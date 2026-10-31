using Bunit;
using ElementPlus;
using ElementPlus.Core;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 批次㉘：ElDateTimePicker「点日期」这一步的提交与收起口径。
    /// <para>
    /// <b>对齐 EP 2.14.0 的那一档（默认）</b>：点日期走 <c>onPick(date, visible = true)</c>
    /// （Temp/b28-ep-use-common-picker.ts）—— 立刻 <c>emitInput</c> 写 <c>modelValue</c>，
    /// 但面板保持打开，好让用户接着拨时间；<c>change</c> 不在这一刻发，而是等面板关闭时由
    /// <c>picker.vue</c> 的 <c>watch(pickerVisible)</c> 比较 <c>valueOnOpen</c> 才补发。
    /// </para>
    /// <para>
    /// <b>本库扩展</b>：<c>CloseOnDatePick</c>。EP 的 <c>datePickerProps</c> 里没有等价属性
    /// （取证见 Temp/b28-ep-dp-props.ts、Temp/b28-ep-tp-common-props.ts），置 true 时点日期即提交并收起面板，
    /// change 在这一次点击就发且<b>只发一次</b>。
    /// </para>
    /// <para>
    /// 两档判据成对写（默认档「面板仍开」↔ 参数档「面板收起」），任一处被改反都会红。
    /// 浏览器实测面在 <c>demo/tools/probes/verify-datetime-close-on-date-pick.cjs</c>。
    /// </para>
    /// </summary>
    public class DateTimePickerCloseOnDatePickTests : BunitContext
    {
        public DateTimePickerCloseOnDatePickTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddElementPlusServices();
        }

        private IReadOnlyList<DropDownOption> Dropdowns => Services.GetRequiredService<PopupService>().SelectDropDownOptions;

        private bool IsPanelOpen => Dropdowns.Count > 0;

        private DropDownOption Panel() => Dropdowns.Single();

        private AngleSharp.Dom.IElement PanelFind(string selector)
        {
            return Render(Panel().OptionContent).Find(selector);
        }

        private IReadOnlyList<AngleSharp.Dom.IElement> PanelFindAll(string selector)
        {
            return Render(Panel().OptionContent).FindAll(selector);
        }

        private void PickDay(int day)
        {
            PanelFindAll(".el-date-table td.available")
                .First(td => td.QuerySelector("span")?.TextContent.Trim() == day.ToString())
                .Click();
        }

        /// <summary>bUnit 里没有「点面板外」，用 Escape 走同一条关闭路径（OnKeyDownAsync → CloseDropDownAsync）。</summary>
        private static void PressEscape(IRenderedComponent<ElDateTimePicker> cut)
        {
            cut.Find("input.el-input__inner").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        }

        private IRenderedComponent<ElDateTimePicker> RenderPicker(
            DateTime? value,
            Action<DateTime?> onValueChanged,
            Action<DateTime?> onChange,
            Action<string> onStringChanged = null,
            bool closeOnDatePick = false,
            TimeSpan? defaultTime = null)
        {
            return Render<ElDateTimePicker>(p => p
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, onValueChanged)
                .Add(x => x.OnChange, onChange)
                .Add(x => x.ValueFormat, "YYYY-MM-DD HH:mm:ss")
                .Add(x => x.DefaultValue, new DateTime(2026, 5, 1))
                .Add(x => x.DefaultTime, defaultTime)
                .Add(x => x.CloseOnDatePick, closeOnDatePick)
                .Add(x => x.StringValueChanged, onStringChanged ?? (_ => { })));
        }

        [Fact]
        public void DateClickWritesValueImmediatelyButKeepsPanelOpen()
        {
            DateTime? value = new DateTime(2026, 5, 1, 8, 30, 45);
            var stringValue = string.Empty;
            var changes = new List<DateTime?>();
            var cut = RenderPicker(value, next => value = next, next => changes.Add(next),
                next => stringValue = next);

            cut.Find(".el-date-editor").Click();
            PickDay(15);

            // EP 语义 ①：点日期即 update:modelValue，并且沿用原来的时分秒
            Assert.Equal(new DateTime(2026, 5, 15, 8, 30, 45), value);
            Assert.Equal("2026-05-15 08:30:45", stringValue);
            Assert.Equal("2026-05-15 08:30:45", cut.Find("input").GetAttribute("value"));

            // EP 语义 ②：面板不关（好让用户接着拨时间）
            Assert.True(IsPanelOpen, "默认档点日期后面板应仍打开（EP 的 onPick(date, visible=true)）");

            // EP 语义 ③：change 不在这一刻发 —— 正向对照见 ChangeOnCloseAfterDatePick
            Assert.Empty(changes);
        }

        [Fact]
        public void CloseOnDatePickClickCommitsAndClosesPanelAndFiresChangeOnce()
        {
            DateTime? value = new DateTime(2026, 5, 1, 8, 30, 45);
            var changes = new List<DateTime?>();
            var cut = RenderPicker(value, next => value = next, next => changes.Add(next),
                closeOnDatePick: true);

            cut.Find(".el-date-editor").Click();
            PickDay(15);

            Assert.Equal(new DateTime(2026, 5, 15, 8, 30, 45), value);
            Assert.False(IsPanelOpen, "CloseOnDatePick 档点日期后面板应收起");
            Assert.Equal(new DateTime(2026, 5, 15, 8, 30, 45), Assert.Single(changes));
        }

        [Fact]
        public void EmptyStartUsesDefaultTimeWhenDateIsPicked()
        {
            DateTime? value = null;
            var cut = RenderPicker(null, next => value = next, _ => { },
                defaultTime: new TimeSpan(10, 20, 30));

            cut.Find(".el-date-editor").Click();
            PickDay(15);

            Assert.Equal(new DateTime(2026, 5, 15, 10, 20, 30), value);
        }

        [Fact]
        public void EmptyStartWithoutDefaultTimeFallsBackToMidnight()
        {
            DateTime? value = null;
            var cut = RenderPicker(null, next => value = next, _ => { });

            cut.Find(".el-date-editor").Click();
            PickDay(15);

            // 上一条的兜底分支要有反向证人：DefaultTime 缺席时是 00:00:00，不是「现在几点」也不是不写值
            Assert.Equal(new DateTime(2026, 5, 15, 0, 0, 0), value);
        }

        [Fact]
        public void ChangeOnCloseAfterDatePick()
        {
            DateTime? value = new DateTime(2026, 5, 1, 8, 0, 0);
            var changes = new List<DateTime?>();
            var cut = RenderPicker(value, next => value = next, next => changes.Add(next));

            cut.Find(".el-date-editor").Click();
            PickDay(15);
            Assert.Empty(changes);

            // 「点日期 → 面板外关闭」这条路径在改动前一次 change 都不会发；EP 在关闭时补发
            PressEscape(cut);

            Assert.Equal(new DateTime(2026, 5, 15, 8, 0, 0), Assert.Single(changes));
        }

        [Fact]
        public void ClosingPanelWithoutPickingFiresNoChange()
        {
            DateTime? value = new DateTime(2026, 5, 1, 8, 0, 0);
            var changes = new List<DateTime?>();
            var cut = RenderPicker(value, next => value = next, next => changes.Add(next));

            cut.Find(".el-date-editor").Click();
            PressEscape(cut);

            // 上一条断言「变了就发一次」，这条断言「没变就不发」：缺了它，无条件补发的写法也能全绿
            Assert.Empty(changes);
        }

        [Fact]
        public void ConfirmAfterDatePickFiresChangeOnlyOnce()
        {
            DateTime? value = new DateTime(2026, 5, 1, 8, 0, 0);
            var changes = new List<DateTime?>();
            var cut = RenderPicker(value, next => value = next, next => changes.Add(next));

            cut.Find(".el-date-editor").Click();
            PickDay(15);
            PanelFind(".el-date-time-picker__confirm").Click();

            Assert.Equal(new DateTime(2026, 5, 15, 8, 0, 0), value);
            Assert.False(IsPanelOpen);
            Assert.Equal(1, changes.Count);
        }

        [Fact]
        public void RangeModeIgnoresCloseOnDatePick()
        {
            IList<DateTime?> range = new List<DateTime?>();
            var changes = new List<DateTime?>();
            var cut = Render<ElDateTimePicker>(p => p
                .Add(x => x.Type, DateTimePickerType.DateTimeRange)
                .Add(x => x.SinglePanel, true)
                .Add(x => x.DefaultValue, new DateTime(2026, 5, 1))
                .Add(x => x.CloseOnDatePick, true)
                .Add(x => x.RangeValueChanged, next => range = next)
                .Add(x => x.OnRangeChange, next => changes.Add(next[0])));

            cut.Find(".el-date-editor").Click();
            PanelFindAll(".el-date-table td.available")
                .First(td => td.QuerySelector("span")?.TextContent.Trim() == "20").Click();

            // 范围档要两端都选完才有意义：参数不该让它一半就提交、一半就关
            Assert.True(IsPanelOpen, "范围档点第一个日期后面板应仍打开");
            Assert.Empty(range);
            Assert.Empty(changes);
        }

        /// <summary>
        /// 列上的 <c>EditorCloseOnDatePick</c> 要一路透到单元格里那个真 ElDateTimePicker，
        /// 并且当场把值落进行编辑缓冲、把弹层收起。
        /// </summary>
        [Fact]
        public void TableColumnForwardsCloseOnDatePickToEditorAndClosesOnDayPick()
        {
            var header = RenderEditCellTable(closeOnDatePick: true);
            var popup = Dropdowns.Last();

            Assert.True(header.EditorCloseOnDatePick);
            Assert.True(header.EditorRenderConfig.CloseOnDatePick);

            Render(popup.OptionContent).FindAll(".el-date-table td.available")
                .First(td => td.QuerySelector("span")?.TextContent.Trim() == "15")
                .Click();

            Assert.Equal(new DateTime(2026, 1, 15, 9, 30, 0), (DateTime?)header.EditorRenderConfig.EditingValue);
            Assert.False(IsPanelOpen, "列上开了参数，单元格编辑器点日期后弹层应收起");
        }

        /// <summary>
        /// 反向对照：列上不开参数时，单元格里点日期同样即写值，但弹层留着（可以接着拨时间）。
        /// 少了这条，上面那条的「弹层收起」可能只是弹层生命周期在测别的东西。
        /// </summary>
        [Fact]
        public void TableCellDatePickWritesValueAndKeepsPanelOpenByDefault()
        {
            var header = RenderEditCellTable(closeOnDatePick: false);
            var popup = Dropdowns.Last();

            Assert.False(header.EditorRenderConfig.CloseOnDatePick);

            Render(popup.OptionContent).FindAll(".el-date-table td.available")
                .First(td => td.QuerySelector("span")?.TextContent.Trim() == "15")
                .Click();

            Assert.Equal(new DateTime(2026, 1, 15, 9, 30, 0), (DateTime?)header.EditorRenderConfig.EditingValue);
            Assert.True(IsPanelOpen, "默认档单元格点日期后弹层应仍打开");
        }

        private TableHeader RenderEditCellTable(bool closeOnDatePick)
        {
            var rows = new List<Row> { new Row { CreatedAt = new DateTime(2026, 1, 1, 9, 30, 0) } };
            var cut = Render<ElTable>(p => p
                .Add(x => x.DataSource, rows)
                .Add(x => x.IsEditable, true)
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .AddCascadingValue("Page", (ElementComponentBase)new ElementDialogBase())
                .Add(x => x.ChildContent, builder =>
                {
                    builder.OpenComponent<ElTableColumn>(0);
                    builder.AddAttribute(1, nameof(ElTableColumn.Property), nameof(Row.CreatedAt));
                    builder.AddAttribute(2, nameof(ElTableColumn.Text), "上架时间");
                    builder.AddAttribute(3, nameof(ElTableColumn.IsEditable), true);
                    builder.AddAttribute(4, nameof(ElTableColumn.Editor), typeof(ElDateTimePicker));
                    builder.AddAttribute(5, nameof(ElTableColumn.EditorCloseOnDatePick), closeOnDatePick);
                    builder.CloseComponent();
                }));

            cut.InvokeAsync(() => cut.Instance.BeginEditRowAsync(rows[0])).Wait();
            cut.Find(".el-table tbody tr td .el-date-editor").Click();

            return cut.Instance.Columns.Single(h => h.Property?.Name == nameof(Row.CreatedAt));
        }

        public class Row
        {
            public DateTime? CreatedAt { get; set; }
        }
    }
}
