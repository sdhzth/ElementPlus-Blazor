using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>日历头部控制器类型，对齐 EP 2.13.1 的 <c>controller-type</c>。</summary>
    public enum CalendarControllerType
    {
        /// <summary>上一月 / 今天 / 下一月 按钮组（默认）。</summary>
        Button = 0,
        /// <summary>年 / 月下拉选择 + 今天按钮。</summary>
        Select = 1
    }

    public partial class ElCalendar : ElementComponentBase
    {

        [Parameter]
        public DateTime? Value { get; set; }

        [Parameter]
        public EventCallback<DateTime?> ValueChanged { get; set; }

        /// <summary>选中某天回调（EP 无此事件，本库扩展，便于拿原始点击日期）。</summary>
        [Parameter]
        public EventCallback<DateTime> OnPick { get; set; }

        /// <summary>自定义头部插槽，参数为当前视图参照日期 <see cref="DateTime"/>。</summary>
        [Parameter]
        public RenderFragment<DateTime> Header { get; set; }

        /// <summary>自定义单元格插槽，参数为 <see cref="CalendarDateContext"/>（对齐 EP <c>date-cell</c>）。</summary>
        [Parameter]
        public RenderFragment<CalendarDateContext> DateCell { get; set; }

        /// <summary>
        /// 自定义周标题（7 项）。未提供时按语言取 <c>el.datepicker.weeks.*</c> 词条，
        /// 没有词条就用内置表（简体：日/一/二/三/四/五/六，周日为首列）。
        /// </summary>
        [Parameter]
        public string[] WeekDays { get; set; }

        [Parameter]
        public CultureInfo Culture { get; set; }

        /// <summary>周起始日 (0=周日...6=周六)。</summary>
        [Parameter]
        public int FirstDayOfWeek { get; set; } = 0;

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        /// <summary>
        /// 时间范围，对齐 EP <c>range</c>：长度须为 2，分别为起始与结束日期。
        /// 起始日期应为周起始日、结束日期应为周结束日，跨度不超过两个月。
        /// </summary>
        [Parameter]
        public DateTime[] Range { get; set; }

        /// <summary>头部控制器类型，对齐 EP 2.13.1 的 <c>controller-type</c>（仅非 range 模式生效）。</summary>
        [Parameter]
        public CalendarControllerType ControllerType { get; set; } = CalendarControllerType.Button;

        /// <summary>
        /// 当 <see cref="ControllerType"/> 为 <see cref="CalendarControllerType.Select"/> 时自定义下拉标签。
        /// 签名对齐 EP：<c>(value, type)</c>，<c>type</c> 为 <c>"year"</c> 或 <c>"month"</c>。
        /// </summary>
        [Parameter]
        public Func<int, string, string> Formatter { get; set; }

        /// <summary>非 range 模式下，当前显示的月份（每月 1 号）。</summary>
        protected DateTime ViewDate { get; set; }

        /// <summary>range 模式下，第一个分段所代表的月份 1 号（用于标题）。</summary>
        protected DateTime RangeTitleDate { get; set; }

        /// <summary>当前视图参照日期：标题与头部插槽据此取值。</summary>
        protected DateTime ReferenceDate =>
            IsRangeMode && RangeSegments.Count > 0 ? RangeTitleDate : ViewDate;

        /// <summary>
        /// 标题：EP 的挂法是 <c>{year} {el.datepicker.year} {el.datepicker.monthM}</c>，
        /// 取词顺序同样是<b>词条 &gt; 内置表</b>。
        /// <para>
        /// 兜底值仍按 <see cref="Culture"/> 的数字写法格式化，所以简体中文下渲染结果与改动前
        /// 的 <c>"yyyy 年 M 月"</c> 完全一致；<see cref="Culture"/> 的作用也因此保留。
        /// </para>
        /// </summary>
        protected string Title
        {
            get
            {
                var date = ReferenceDate;
                var culture = Culture ?? CultureInfo.CurrentCulture;
                return PickerLocale.FormatYear(ConfigLocale, date.Year, Lang, culture)
                    + " "
                    + PickerLocale.FormatMonth(ConfigLocale, date.Month, Lang);
            }
        }

        protected bool IsRangeMode { get; private set; }

        /// <summary>range 模式下的各月分段（每个分段是一张 6 周表格的数据）。</summary>
        protected IReadOnlyList<CalendarRangeSegment> RangeSegments { get; private set; } = Array.Empty<CalendarRangeSegment>();

        /// <summary>下拉控制器年份候选项（当前年 ±10）。</summary>
        protected IReadOnlyList<SelectOption> YearOptions { get; private set; } = Array.Empty<SelectOption>();

        /// <summary>下拉控制器月份候选项（1-12）。</summary>
        protected IReadOnlyList<SelectOption> MonthOptions { get; private set; } = Array.Empty<SelectOption>();

        protected DateTime SelectedDay => Value?.Date ?? DateTime.MinValue;

        // EP 的 calendar 控制器就是这三个 el.datepicker.* 词条，兜底值取改动前的写死文字。
        protected string PrevMonthLabel => PickerLocale.Entry(ConfigLocale, Lang, "el.datepicker.prevMonth", "上个月");

        protected string TodayLabel => PickerLocale.Entry(ConfigLocale, Lang, "el.datepicker.today", "今天");

        protected string NextMonthLabel => PickerLocale.Entry(ConfigLocale, Lang, "el.datepicker.nextMonth", "下个月");

        protected IReadOnlyList<IReadOnlyList<CalendarDateContext>> Weeks
        {
            get
            {
                var firstOfMonth = new DateTime(ViewDate.Year, ViewDate.Month, 1);
                var start = firstOfMonth.AddDays(-GetOffset(firstOfMonth.DayOfWeek));

                var days = Enumerable.Range(0, 42)
                    .Select(i =>
                    {
                        var date = start.AddDays(i);
                        var isCurrent = date.Month == ViewDate.Month;
                        return new CalendarDateContext
                        {
                            Date = date,
                            Type = isCurrent ? "current-month" : (date < firstOfMonth ? "prev-month" : "next-month"),
                            IsCurrentMonth = isCurrent,
                            IsSelected = Value.HasValue && date.Date == Value.Value.Date,
                            IsToday = date.Date == DateTime.Today,
                            Day = date.ToString("yyyy-MM-dd")
                        };
                    })
                    .ToList();

                return Enumerable.Range(0, 6)
                    .Select(i => (IReadOnlyList<CalendarDateContext>)days.Skip(i * 7).Take(7).ToList())
                    .ToList();
            }
        }

        private int GetOffset(DayOfWeek dow)
        {
            var dowInt = (int)dow;
            var offset = dowInt - FirstDayOfWeek;
            if (offset < 0) offset += 7;
            return offset;
        }

        protected string[] ResolvedWeekDays
        {
            get
            {
                if (WeekDays != null && WeekDays.Length == 7) return WeekDays;
                // EP 的 calendar 表头同样是 el.datepicker.weeks.*（周日为首列），无词条时回内置表，与改动前一致
                var names = PickerLocale.WeekHeaders(ConfigLocale, Lang);
                var result = new string[7];
                for (int i = 0; i < 7; i++)
                    result[i] = names[(i + FirstDayOfWeek) % 7];
                return result;
            }
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            // range 模式
            IsRangeMode = Range != null && Range.Length == 2 && Range[0] <= Range[1];
            if (IsRangeMode)
            {
                BuildRangeSegments(Range[0].Date, Range[1].Date);
            }
            else
            {
                RangeSegments = Array.Empty<CalendarRangeSegment>();
                var value = (Value ?? DateTime.Today).Date;
                if (ViewDate == default)
                {
                    ViewDate = new DateTime(value.Year, value.Month, 1);
                }
            }

            // 下拉控制器候选项
            if (ControllerType == CalendarControllerType.Select && !IsRangeMode)
            {
                var year = ViewDate.Year;
                var years = new List<SelectOption>(20);
                for (int i = -10; i < 10; i++)
                {
                    var y = year + i;
                    if (y > 0)
                        years.Add(new SelectOption { Value = y, Label = FormatLabel(y, "year") });
                }
                YearOptions = years;

                var months = new List<SelectOption>(12);
                for (int m = 1; m <= 12; m++)
                    months.Add(new SelectOption { Value = m, Label = FormatLabel(m, "month") });
                MonthOptions = months;
            }
        }

        private string FormatLabel(int value, string type) =>
            Formatter != null ? Formatter(value, type) : (type == "month" ? value.ToString() : value.ToString());

        private void BuildRangeSegments(DateTime rawStart, DateTime rawEnd)
        {
            // 跨度不超过两个月（相邻月允许）
            var monthDiff = (rawEnd.Year - rawStart.Year) * 12 + (rawEnd.Month - rawStart.Month);
            if (monthDiff > 1)
            {
                IsRangeMode = false;
                RangeSegments = Array.Empty<CalendarRangeSegment>();
                return;
            }

            var segments = new List<CalendarRangeSegment>();
            int total = monthDiff; // 0 = 单月, 1 = 两个月
            for (int k = 0; k <= total; k++)
            {
                var monthFirst = new DateTime(rawStart.Year, rawStart.Month, 1).AddMonths(k);
                var monthLast = monthFirst.AddMonths(1).AddDays(-1);

                var segStart = AlignToWeekStart(rawStart > monthFirst ? rawStart : monthFirst);
                var segEnd = AlignToWeekEnd(rawEnd < monthLast ? rawEnd : monthLast);

                var days = new List<CalendarDateContext>();
                for (var d = segStart; d <= segEnd; d = d.AddDays(1))
                {
                    bool inRange = d >= rawStart && d <= rawEnd;
                    days.Add(new CalendarDateContext
                    {
                        Date = d,
                        Type = inRange ? "current-month" : (d < rawStart ? "prev-month" : "next-month"),
                        IsCurrentMonth = inRange,
                        IsSelected = Value.HasValue && d.Date == Value.Value.Date,
                        IsToday = d.Date == DateTime.Today,
                        Day = d.ToString("yyyy-MM-dd"),
                        IsInRange = inRange,
                        IsRangeStart = d.Date == rawStart.Date,
                        IsRangeEnd = d.Date == rawEnd.Date
                    });
                }

                var weeks = Enumerable.Range(0, days.Count / 7)
                    .Select(i => (IReadOnlyList<CalendarDateContext>)days.Skip(i * 7).Take(7).ToList())
                    .ToList();

                if (k == 0) RangeTitleDate = monthFirst;
                segments.Add(new CalendarRangeSegment { TitleDate = monthFirst, Weeks = weeks });
            }

            RangeSegments = segments;
        }

        private DateTime AlignToWeekStart(DateTime d)
        {
            var off = GetOffset(d.DayOfWeek);
            return d.AddDays(-off).Date;
        }

        private DateTime AlignToWeekEnd(DateTime d)
        {
            var off = (7 - GetOffset(d.DayOfWeek)) % 7;
            return d.AddDays(off == 0 ? 6 : off - 1).Date; // 补齐到本周最后一天
        }

        /// <summary>对齐 EP 暴露方法：选中指定日期。</summary>
        public void PickDay(DateTime day)
        {
            Value = day.Date;
            ViewDate = new DateTime(day.Year, day.Month, 1);
            _ = ValueChanged.InvokeAsync(Value);
            _ = OnPick.InvokeAsync(day.Date);
            StateHasChanged();
        }

        /// <summary>对齐 EP 暴露方法：跳转到上一月 / 下一月 / 上一年 / 下一年 / 今天。</summary>
        public void SelectDate(string type)
        {
            switch (type)
            {
                case "prev-month": ViewDate = ViewDate.AddMonths(-1); break;
                case "next-month": ViewDate = ViewDate.AddMonths(1); break;
                case "prev-year": ViewDate = ViewDate.AddYears(-1); break;
                case "next-year": ViewDate = ViewDate.AddYears(1); break;
                case "today":
                    ViewDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    PickDay(DateTime.Today);
                    return;
                default: return;
            }
            StateHasChanged();
        }

        /// <summary>对齐 EP 暴露方法：根据起始与结束日期计算验证后的显示区间（按周对齐、按月分段）。</summary>
        public IReadOnlyList<(DateTime Start, DateTime End)> CalculateValidatedRange(DateTime start, DateTime end)
        {
            if (start > end) return Array.Empty<(DateTime, DateTime)>();
            var monthDiff = (end.Year - start.Year) * 12 + (end.Month - start.Month);
            if (monthDiff > 1) return Array.Empty<(DateTime, DateTime)>();

            var list = new List<(DateTime, DateTime)>();
            for (int k = 0; k <= monthDiff; k++)
            {
                var monthFirst = new DateTime(start.Year, start.Month, 1).AddMonths(k);
                var monthLast = monthFirst.AddMonths(1).AddDays(-1);
                var segStart = AlignToWeekStart(start > monthFirst ? start : monthFirst);
                var segEnd = AlignToWeekEnd(end < monthLast ? end : monthLast);
                list.Add((segStart, segEnd));
            }
            return list;
        }

        protected string CalendarClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-calendar", Cls)
            .ToString();

        protected string GetCellClass(CalendarDateContext context)
        {
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add(context.Type.Replace("-month", string.Empty))
                .AddIf(context.IsSelected, "is-selected")
                .AddIf(context.IsToday, "is-today")
                .ToString();
        }

        protected string GetRangeCellClass(CalendarDateContext context)
        {
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add(context.Type.Replace("-month", string.Empty))
                .AddIf(context.IsSelected, "is-selected")
                .AddIf(context.IsToday, "is-today")
                .AddIf(context.IsInRange, "is-in-range")
                .AddIf(context.IsRangeStart, "is-range-start")
                .AddIf(context.IsRangeEnd, "is-range-end")
                .ToString();
        }

        protected Task SelectDateAsync(DateTime date)
        {
            PickDay(date);
            return Task.CompletedTask;
        }

        protected Task SelectPreviousMonthAsync()
        {
            ViewDate = ViewDate.AddMonths(-1);
            return Task.CompletedTask;
        }

        protected Task SelectTodayAsync()
        {
            SelectDate("today");
            return Task.CompletedTask;
        }

        protected Task SelectNextMonthAsync()
        {
            ViewDate = ViewDate.AddMonths(1);
            return Task.CompletedTask;
        }

        protected void OnYearChanged(int year)
        {
            ViewDate = new DateTime(year, ViewDate.Month, 1);
        }

        protected void OnMonthChanged(int month)
        {
            ViewDate = new DateTime(ViewDate.Year, month, 1);
        }
    }

    /// <summary>range 模式下的一个月份分段。</summary>
    public sealed class CalendarRangeSegment
    {
        public DateTime TitleDate { get; set; }
        public IReadOnlyList<IReadOnlyList<CalendarDateContext>> Weeks { get; set; } = Array.Empty<IReadOnlyList<CalendarDateContext>>();
    }

    /// <summary>下拉控制器候选项。</summary>
    public sealed class SelectOption
    {
        public int Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }
}
