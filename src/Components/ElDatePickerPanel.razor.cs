using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElDatePickerPanel : ElementFieldComponentBase<DateTime?>
    {
        private static readonly string[] FallbackWeekHeaders = new[] { "日", "一", "二", "三", "四", "五", "六" };
        private DateTime displayDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DatePickerPanelType currentView;
        private bool effectiveDisabled;

        [Parameter]
        public DateTime? Value { get; set; }

        [Parameter]
        public DateTime? ModelValue
        {
            get => Value;
            set => Value = value;
        }

        [Parameter]
        public EventCallback<DateTime?> ValueChanged { get; set; }

        [Parameter]
        public EventCallback<DateTime?> ModelValueChanged { get; set; }

        [Parameter]
        public DatePickerPanelType Type { get; set; } = DatePickerPanelType.Date;

        [Parameter]
        public DateTime? DefaultValue { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool IsDisabled
        {
            get => Disabled;
            set => Disabled = value;
        }

        [Parameter]
        public bool Border { get; set; } = true;

        /// <summary>
        /// 界面语言（BCP-47，如 <c>en-US</c>）。为空时回退到 <see cref="ElementComponentBase.ConfigLocale"/>。
        /// <para>
        /// ⚠️ 必须由宿主（ElDatePicker）显式下发：面板在**弹层里**渲染，而弹层内容由 ElPopup 宿主
        /// 负责出树，面板拿到的 <c>ElementConfiguration</c> 级联值是**弹层所在位置**的（全局默认），
        /// 不是 <c>ElConfigProvider</c> 包裹选择器时的那一份 —— 光靠级联切不到 en-US。
        /// </para>
        /// </summary>
        [Parameter]
        public string Locale { get; set; }

        // ===== 范围（daterange）面板 =====

        /// <summary>面板在范围选择里的角色；<see cref="DatePickerRangeRole.None"/> 即普通单面板。</summary>
        [Parameter]
        public DatePickerRangeRole RangeRole { get; set; } = DatePickerRangeRole.None;

        /// <summary>范围起始日（仅用于高亮，不参与显示月份的计算）。</summary>
        [Parameter]
        public DateTime? RangeStart { get; set; }

        /// <summary>范围结束日（仅用于高亮）。</summary>
        [Parameter]
        public DateTime? RangeEnd { get; set; }

        [Parameter]
        public Func<DateTime, bool> DisabledDate { get; set; }

        [Parameter]
        public RenderFragment FooterContent { get; set; }

        [Parameter]
        public bool ValidateEvent { get; set; } = true;

        [Parameter]
        public EventCallback<DateTime?> OnChange { get; set; }

        [Parameter]
        public EventCallback<DateTime> OnPanelChange { get; set; }

        [Parameter]
        public EventCallback<DatePickerPanelSelection> OnPick { get; set; }

        // ===== Element Plus 对齐新增属性 =====

        /// <summary>是否显示清除按钮</summary>
        [Parameter]
        public bool Clearable { get; set; } = true;

        /// <summary>显示周数（除 week type 外）</summary>
        [Parameter]
        public bool ShowWeekNumber { get; set; } = false;

        /// <summary>是否显示底部区域</summary>
        [Parameter]
        public bool ShowFooter { get; set; } = false;

        /// <summary>是否显示确定按钮</summary>
        [Parameter]
        public bool ShowConfirm { get; set; } = false;

        /// <summary>文本框可输入 (2.13.0, 仅提示性)</summary>
        [Parameter]
        public bool Editable { get; set; } = true;

        /// <summary>自定义单元格类名函数</summary>
        [Parameter]
        public Func<DateTime, string> CellClassName { get; set; }

        /// <summary>
        /// 紧凑模式：整体收窄日期/月/年面板（单元格、表头、内外边距同步缩小），
        /// 供密集表单、表格内联编辑等空间受限的场景使用。默认 false（常规尺寸）。
        /// 几何数值的统一真源是 index.css 补丁层（原 fix.css）的「日期面板紧凑模式」段。
        /// </summary>
        [Parameter]
        public bool Compact { get; set; }

        /// <summary>快捷选项数组（面板场景下仅作扩展入口）</summary>
        [Parameter]
        public IEnumerable<DatePickerShortcut> Shortcuts { get; set; }

        // ===== Slots =====
        [Parameter]
        public RenderFragment PrevMonthIcon { get; set; }
        [Parameter]
        public RenderFragment NextMonthIcon { get; set; }
        [Parameter]
        public RenderFragment PrevYearIcon { get; set; }
        [Parameter]
        public RenderFragment NextYearIcon { get; set; }

        // ===== Events =====
        [Parameter]
        public EventCallback OnClear { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            effectiveDisabled = Disabled || (FormItem?.Form?.Disabled ?? false);
            currentView = currentView == default ? Type : currentView;

            if (FormItem != null && !FormItem.OriginValueHasRendered)
            {
                FormItem.OriginValueHasRendered = true;
                if (FormItem.Form?.Values.Any() ?? false)
                {
                    Value = ConvertToDateTime(FormItem.OriginValue);
                }
                SetFieldValue(Value, false);
            }

            // 范围面板的显示月份一律由父级通过 DefaultValue 下发（左右面板要严格相差一个月），
            // 不能跟着自己选中的那一天跳，否则两侧月份会失联。
            displayDate = RangeRole == DatePickerRangeRole.None
                ? NormalizeDisplayDate(Value ?? DefaultValue ?? displayDate)
                : NormalizeDisplayDate(DefaultValue ?? displayDate);
        }

        protected override void FormItem_OnReset(object value, bool requireRerender)
        {
            Value = ConvertToDateTime(value);
            displayDate = NormalizeDisplayDate(Value ?? DateTime.Today);
            currentView = Type;
            if (ValueChanged.HasDelegate)
            {
                _ = ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                _ = ModelValueChanged.InvokeAsync(Value);
            }
            else
            {
                StateHasChanged();
            }
        }

        private Task GoPrevMonthAsync()
        {
            return SetDisplayDateAsync(displayDate.AddMonths(-1));
        }

        private Task GoNextMonthAsync()
        {
            return SetDisplayDateAsync(displayDate.AddMonths(1));
        }

        private Task GoPrevYearAsync()
        {
            var years = CurrentView == DatePickerPanelType.Year ? -10 : -1;
            return SetDisplayDateAsync(displayDate.AddYears(years));
        }

        private Task GoNextYearAsync()
        {
            var years = CurrentView == DatePickerPanelType.Year ? 10 : 1;
            return SetDisplayDateAsync(displayDate.AddYears(years));
        }

        private void ShowYearView()
        {
            if (!effectiveDisabled)
            {
                currentView = DatePickerPanelType.Year;
            }
        }

        private void ShowMonthView()
        {
            if (!effectiveDisabled)
            {
                currentView = DatePickerPanelType.Month;
            }
        }

        private async Task SelectDateAsync(DateTime value)
        {
            if (effectiveDisabled || !Editable || IsDisabledDate(value))
            {
                return;
            }

            await CommitValueAsync(value.Date, DatePickerPanelType.Date);
        }

        private async Task SelectMonthAsync(int month)
        {
            if (effectiveDisabled || !Editable)
            {
                return;
            }

            var next = new DateTime(displayDate.Year, month, 1);
            if (Type == DatePickerPanelType.Month)
            {
                await CommitValueAsync(next, DatePickerPanelType.Month);
                return;
            }

            displayDate = next;
            currentView = DatePickerPanelType.Date;
            await NotifyPanelChangeAsync();
        }

        private async Task SelectYearAsync(int year)
        {
            if (effectiveDisabled || !Editable)
            {
                return;
            }

            var next = new DateTime(year, displayDate.Month, 1);
            if (Type == DatePickerPanelType.Year)
            {
                await CommitValueAsync(next, DatePickerPanelType.Year);
                return;
            }

            displayDate = next;
            currentView = DatePickerPanelType.Month;
            await NotifyPanelChangeAsync();
        }

        private async Task CommitValueAsync(DateTime value, DatePickerPanelType selectionType)
        {
            Value = NormalizeValue(value, selectionType);
            // 范围面板的显示月份由父级（ElDatePicker）统一驱动，这里不能跟着选中日跳走，
            // 否则左右两个面板会各自跑到不同的月份上去。
            if (RangeRole == DatePickerRangeRole.None)
            {
                displayDate = NormalizeDisplayDate(Value.Value);
            }
            SetFieldValue(Value, ValidateEvent);
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(Value);
            }
            if (OnPick.HasDelegate)
            {
                await OnPick.InvokeAsync(new DatePickerPanelSelection(Value.Value, selectionType));
            }
            if (OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(Value);
            }
        }

        private async Task SetDisplayDateAsync(DateTime value)
        {
            if (effectiveDisabled)
            {
                return;
            }

            displayDate = NormalizeDisplayDate(value);
            await NotifyPanelChangeAsync();
        }

        private async Task NotifyPanelChangeAsync()
        {
            if (OnPanelChange.HasDelegate)
            {
                await OnPanelChange.InvokeAsync(displayDate);
            }
        }

        private RenderFragment BuildDateTable()
        {
            return builder =>
            {
                var seq = 0;
                var firstDay = new DateTime(displayDate.Year, displayDate.Month, 1);
                var gridStart = firstDay.AddDays(-(int)firstDay.DayOfWeek);

                builder.OpenElement(seq++, "table");
                builder.AddAttribute(seq++, "cellspacing", "0");
                builder.AddAttribute(seq++, "cellpadding", "0");
                builder.AddAttribute(seq++, "class", ShowWeekNumber ? "el-date-table is-week-mode" : "el-date-table");
                builder.AddAttribute(seq++, "role", "grid");
                builder.AddAttribute(seq++, "aria-label", DateTableAriaLabel);
                builder.OpenElement(seq++, "tbody");
                builder.OpenElement(seq++, "tr");
                if (ShowWeekNumber)
                {
                    builder.OpenElement(seq++, "th");
                    builder.AddContent(seq++, "");
                    builder.CloseElement();
                }
                var weekIndex = 0;
                foreach (var header in EffectiveWeekHeaders)
                {
                    builder.OpenElement(seq++, "th");
                    builder.AddAttribute(seq++, "aria-label", PickerLocale.WeekHeaderFullName(PanelLocale, Lang, weekIndex));
                    builder.AddContent(seq++, header);
                    builder.CloseElement();
                    weekIndex++;
                }
                builder.CloseElement();

                for (var week = 0; week < 6; week++)
                {
                    builder.OpenElement(seq++, "tr");
                    builder.AddAttribute(seq++, "class", "el-date-table__row");
                    if (ShowWeekNumber)
                    {
                        var weekNo = GetIsoWeek(gridStart.AddDays(week * 7));
                        builder.OpenElement(seq++, "td");
                        builder.AddAttribute(seq++, "class", "el-date-table__week");
                        builder.OpenElement(seq++, "span");
                        builder.AddContent(seq++, weekNo);
                        builder.CloseElement();
                        builder.CloseElement();
                    }
                    for (var day = 0; day < 7; day++)
                    {
                        var current = gridStart.AddDays(week * 7 + day);
                        builder.OpenElement(seq++, "td");
                        builder.AddAttribute(seq++, "class", GetDayClass(current));
                        builder.AddAttribute(seq++, "role", "gridcell");
                        builder.AddAttribute(seq++, "aria-selected", IsSelectedDate(current) || IsInSelectedRange(current) ? "true" : "false");
                        builder.AddAttribute(seq++, "aria-disabled", IsDisabledDate(current) ? "true" : "false");
                        builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => SelectDateAsync(current)));
                        builder.OpenElement(seq++, "div");
                        builder.OpenElement(seq++, "span");
                        builder.AddContent(seq++, current.Day);
                        builder.CloseElement();
                        builder.CloseElement();
                        builder.CloseElement();
                    }
                    builder.CloseElement();
                }

                builder.CloseElement();
                builder.CloseElement();
            };
        }

        private RenderFragment BuildMonthTable()
        {
            return builder =>
            {
                var seq = 0;
                builder.OpenElement(seq++, "table");
                builder.AddAttribute(seq++, "class", "el-month-table");
                builder.AddAttribute(seq++, "role", "grid");
                builder.AddAttribute(seq++, "aria-label", MonthTableAriaLabel);
                builder.OpenElement(seq++, "tbody");
                for (var row = 0; row < 3; row++)
                {
                    builder.OpenElement(seq++, "tr");
                    for (var col = 0; col < 4; col++)
                    {
                        var month = row * 4 + col + 1;
                        var current = new DateTime(displayDate.Year, month, 1);
                        builder.OpenElement(seq++, "td");
                        builder.AddAttribute(seq++, "class", GetMonthClass(month));
                        builder.AddAttribute(seq++, "aria-label", PickerLocale.MonthNumberEntry(PanelLocale, month, Lang));
                        builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => SelectMonthAsync(month)));
                        builder.OpenElement(seq++, "div");
                        builder.OpenElement(seq++, "a");
                        builder.AddAttribute(seq++, "class", "cell");
                        builder.AddContent(seq++, PickerLocale.MonthName(PanelLocale, month, Lang));
                        builder.CloseElement();
                        builder.CloseElement();
                        builder.CloseElement();
                    }
                    builder.CloseElement();
                }
                builder.CloseElement();
                builder.CloseElement();
            };
        }

        private RenderFragment BuildYearTable()
        {
            return builder =>
            {
                var seq = 0;
                var start = YearRangeStart;
                builder.OpenElement(seq++, "table");
                builder.AddAttribute(seq++, "class", "el-year-table");
                builder.AddAttribute(seq++, "role", "grid");
                builder.AddAttribute(seq++, "aria-label", YearTableAriaLabel);
                builder.OpenElement(seq++, "tbody");
                for (var row = 0; row < 3; row++)
                {
                    builder.OpenElement(seq++, "tr");
                    for (var col = 0; col < 4; col++)
                    {
                        var index = row * 4 + col;
                        if (index >= 10)
                        {
                            builder.OpenElement(seq++, "td");
                            builder.CloseElement();
                            continue;
                        }

                        var year = start + index;
                        builder.OpenElement(seq++, "td");
                        builder.AddAttribute(seq++, "class", GetYearClass(year));
                        builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => SelectYearAsync(year)));
                        builder.OpenElement(seq++, "a");
                        builder.AddAttribute(seq++, "class", "cell");
                        builder.AddContent(seq++, year);
                        builder.CloseElement();
                        builder.CloseElement();
                    }
                    builder.CloseElement();
                }
                builder.CloseElement();
                builder.CloseElement();
            };
        }

        private string GetDayClass(DateTime value)
        {
            var custom = CellClassName?.Invoke(value) ?? string.Empty;
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add(value.Month == displayDate.Month ? "available" : value < new DateTime(displayDate.Year, displayDate.Month, 1) ? "prev-month" : "available next-month")
                .AddIf(value.Date == DateTime.Today, "today")
                .AddIf(IsSelectedDate(value), "current")
                // 范围高亮（对齐 EP date-table 的 in-range / start-date / end-date）：
                // in-range 覆盖起止两端，再由 start-date / end-date 负责两端的圆角与实心块。
                .AddIf(IsRangePanel && IsInSelectedRange(value), "in-range")
                .AddIf(IsRangePanel && IsRangeBoundary(value, RangeStart), "start-date")
                .AddIf(IsRangePanel && IsRangeBoundary(value, RangeEnd), "end-date")
                .AddIf(IsDisabledDate(value), "disabled")
                .Add(custom)
                .ToString();
        }

        private string GetMonthClass(int month)
        {
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .AddIf(Value.HasValue && Value.Value.Year == displayDate.Year && Value.Value.Month == month, "current")
                .AddIf(DateTime.Today.Year == displayDate.Year && DateTime.Today.Month == month, "today")
                .ToString();
        }

        private string GetYearClass(int year)
        {
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .AddIf(Value.HasValue && Value.Value.Year == year, "current")
                .AddIf(DateTime.Today.Year == year, "today")
                .ToString();
        }

        private bool IsSelectedDate(DateTime value)
        {
            return Value.HasValue && Value.Value.Date == value.Date;
        }

        private bool IsDisabledDate(DateTime value)
        {
            return DisabledDate?.Invoke(value.Date) == true;
        }

        private DateTime NormalizeValue(DateTime value, DatePickerPanelType selectionType)
        {
            return selectionType switch
            {
                DatePickerPanelType.Year => new DateTime(value.Year, 1, 1),
                DatePickerPanelType.Month => new DateTime(value.Year, value.Month, 1),
                _ => value.Date
            };
        }

        private DateTime NormalizeDisplayDate(DateTime value)
        {
            return new DateTime(value.Year, value.Month, 1);
        }

        private static DateTime? ConvertToDateTime(object value)
        {
            if (value == null)
            {
                return null;
            }
            if (value is DateTime dateTime)
            {
                return dateTime;
            }
            if (DateTime.TryParse(Convert.ToString(value, CultureInfo.CurrentCulture), CultureInfo.CurrentCulture, DateTimeStyles.None, out var current))
            {
                return current;
            }
            if (DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.None, out var invariant))
            {
                return invariant;
            }
            return null;
        }

        private string PanelClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-picker-panel", "el-date-picker", "el-date-picker-panel", Cls)
            .AddIf(!Border, "is-borderless")
            .AddIf(Compact, "is-compact")
            .AddIf(effectiveDisabled, "is-disabled")
            .AddIf(ShortcutList.Any(), "has-sidebar")
            .ToString();

        private string HeaderClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-date-picker__header")
            .AddIf(CurrentView != DatePickerPanelType.Date, "el-date-picker__header--bordered")
            .ToString();

        private string RangeHeaderClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-date-range-picker__header", "el-date-range-header")
            .AddIf(IsRangeStart, "is-left")
            .AddIf(IsRangeEnd, "is-right")
            .ToString();

        private DatePickerPanelType CurrentView => currentView;

        private int DisplayedYear => displayDate.Year;

        private int DisplayedMonth => displayDate.Month;

        private int YearRangeStart => displayDate.Year / 10 * 10;

        private string YearRangeText => $"{YearRangeStart} - {YearRangeStart + 9}";

        private bool IsDatePickerPanelDisabled => effectiveDisabled;

        // ===== 范围（daterange）面板 =====

        /// <summary>是否以「范围面板」形态渲染。</summary>
        private bool IsRangePanel => RangeRole != DatePickerRangeRole.None;

        private bool IsRangeStart => RangeRole == DatePickerRangeRole.Start;

        private bool IsRangeEnd => RangeRole == DatePickerRangeRole.End;

        /// <summary>范围面板的合并年月标签（EP 的 <c>el-date-range-picker__header &gt; div</c>）。</summary>
        private string RangeHeaderLabel => PickerLocale.FormatYearMonth(PanelLocale, DisplayedYear, DisplayedMonth, Lang);

        // ===== 本地化（周标题 / 月名 / 年月标签 / 无障碍名称）=====

        /// <summary>星期表头，随 <see cref="ElementConfig.Locale"/> 切换；无配置时默认简体中文。</summary>
        private string[] EffectiveWeekHeaders
        {
            get
            {
                var headers = PickerLocale.WeekHeaders(PanelLocale, Lang);
                return headers != null && headers.Length == 7 ? headers : FallbackWeekHeaders;
            }
        }

        private string YearLabel => PickerLocale.FormatYear(PanelLocale, DisplayedYear, Lang);

        private string MonthLabel => PickerLocale.FormatMonth(PanelLocale, DisplayedMonth, Lang);

        private string PrevYearAriaLabel => PickerLocale.Entry(PanelLocale, Lang, "el.datepicker.prevYear", null);

        private string NextYearAriaLabel => PickerLocale.Entry(PanelLocale, Lang, "el.datepicker.nextYear", null);

        private string PrevMonthAriaLabel => PickerLocale.Entry(PanelLocale, Lang, "el.datepicker.prevMonth", null);

        private string NextMonthAriaLabel => PickerLocale.Entry(PanelLocale, Lang, "el.datepicker.nextMonth", null);

        private string ClearAriaLabel => PickerLocale.Entry(PanelLocale, Lang, "el.datepicker.clear", null);

        /// <summary>三个视图下表格的 <c>aria-label</c>（EP 的 basic-*-table 就是这么命名的，此前本仓表格没有名称）。</summary>
        private string DateTableAriaLabel => PickerLocale.Entry(PanelLocale, Lang, "el.datepicker.dateTablePrompt", null);

        private string MonthTableAriaLabel => PickerLocale.Entry(PanelLocale, Lang, "el.datepicker.monthTablePrompt", null);

        private string YearTableAriaLabel => PickerLocale.Entry(PanelLocale, Lang, "el.datepicker.yearTablePrompt", null);

        /// <summary>页脚确认按钮文字：EP 的 <c>el.datepicker.confirm</c>，没词条时保持改动前的「确定」。</summary>
        private string ConfirmLabel => PickerLocale.Entry(PanelLocale, Lang, "el.datepicker.confirm", "确定");

        /// <summary>
        /// 生效语言：宿主（ElDatePicker）下发的 <see cref="Locale"/> 优先；
        /// 未下发时回退到本组件级联到的 <see cref="ElementComponentBase.ConfigLocale"/>（页面内联使用面板的场景）。
        /// </summary>
        private string PanelLocale => string.IsNullOrWhiteSpace(Locale) ? ConfigLocale : Locale;

        /// <summary>某一天的归属范围：用于 <c>in-range</c> / <c>start-date</c> / <c>end-date</c> 高亮。</summary>
        private bool IsInSelectedRange(DateTime value)
        {
            var start = RangeStart?.Date;
            var end = RangeEnd?.Date;
            if (!start.HasValue || !end.HasValue)
            {
                return false;
            }

            var min = start.Value <= end.Value ? start.Value : end.Value;
            var max = start.Value <= end.Value ? end.Value : start.Value;
            return value.Date >= min && value.Date <= max;
        }

        private bool IsRangeBoundary(DateTime value, DateTime? boundary)
        {
            return boundary.HasValue && boundary.Value.Date == value.Date;
        }

        /// <summary>清除当前值</summary>
        public async Task ClearAsync()
        {
            Value = null;
            SetFieldValue(null, ValidateEvent);
            if (ValueChanged.HasDelegate) await ValueChanged.InvokeAsync(null);
            if (ModelValueChanged.HasDelegate) await ModelValueChanged.InvokeAsync(null);
            if (OnClear.HasDelegate) await OnClear.InvokeAsync();
            StateHasChanged();
        }

        /// <summary>ShowConfirm 确定按钮 — 触发 OnChange</summary>
        public Task OnConfirmAsync()
        {
            if (Value.HasValue && OnChange.HasDelegate)
            {
                return OnChange.InvokeAsync(Value);
            }
            return Task.CompletedTask;
        }

        /// <summary>非空快捷项列表（过滤 null）。面板场景下的独立扩展入口：在面板左侧渲染快捷栏。</summary>
        private IReadOnlyList<DatePickerShortcut> ShortcutList =>
            Shortcuts?.Where(x => x != null).ToList() ?? new List<DatePickerShortcut>();

        /// <summary>点击快捷项：单值项直接提交；范围项面板仅持单一 Value，取起点提交（完整范围由宿主 ElDatePicker 处理）。</summary>
        private async Task SelectShortcutAsync(DatePickerShortcut shortcut)
        {
            if (shortcut == null || !Editable || effectiveDisabled)
            {
                return;
            }

            var range = shortcut.ResolveRangeValue();
            if (range != null && range.Count >= 1)
            {
                var start = range[0] ?? DateTime.Today;
                await CommitValueAsync(start.Date, DatePickerPanelType.Date);
                return;
            }

            var value = shortcut.ResolveValue();
            if (value.HasValue)
            {
                await CommitValueAsync(value.Value.Date, DatePickerPanelType.Date);
            }
        }

        /// <summary>ISO 周数</summary>
        internal static int GetIsoWeek(DateTime value) =>
            CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(value,
                CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }
}
