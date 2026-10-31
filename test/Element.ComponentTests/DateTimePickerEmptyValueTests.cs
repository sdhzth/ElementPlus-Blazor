using Bunit;
using ElementPlus;
using ElementPlus.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElDateTimePicker 在<b>初值为空</b>时的解锁与提交契约 —— 用户报的 /table 单元格就是这个场景：
    /// 单元格原值为 null，只能靠点日期解锁「确定」。现有 ElDateTimePickerTests 全部从非空 Value 起步，
    /// 等于一直没走这条路。
    /// <para>
    /// <b>这套用例的边界要写清楚</b>：读弹层用的是 <c>Render(option.OptionContent)</c>，也就是测试自己
    /// 强制重渲染一次弹层内容。因此它能验"选中值有没有进 pending、点确定能不能提交"，
    /// 但<b>遮住了</b>"宿主没被通知刷新"那一类缺陷 —— footer 实际由应用根的 <c>ElPopup</c> 渲染，
    /// 而 ElPopup 在 bUnit 里独立渲染会在 <c>RenderDropDownAsync</c> 抛 NRE，搭不起真宿主。
    /// 那一面由浏览器探针覆盖：<c>demo/tools/probes/diag-dt-table-probe.cjs</c>
    /// （看选完日期后 <c>confirmDisabled</c> 是否从 true 变 false）。
    /// </para>
    /// </summary>
    public class DateTimePickerEmptyValueTests : BunitContext
    {
        public DateTimePickerEmptyValueTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddElementServices();
        }

        private DropDownOption GetDropdown()
        {
            return Services.GetRequiredService<PopupService>().SelectDropDownOptions.Single();
        }

        private AngleSharp.Dom.IElement PanelFind(string selector)
        {
            return Render(GetDropdown().OptionContent).Find(selector);
        }

        private IReadOnlyList<AngleSharp.Dom.IElement> PanelFindAll(string selector)
        {
            return Render(GetDropdown().OptionContent).FindAll(selector);
        }

        private void PickDay(int day)
        {
            var cell = PanelFindAll(".el-date-table td.available")
                .First(td => td.QuerySelector("span")?.TextContent.Trim() == day.ToString());
            cell.Click();
        }

        [Fact]
        public void ConfirmButtonStartsDisabledWhenValueIsEmpty()
        {
            var cut = Render<ElDateTimePicker>(p => p.Add(x => x.Value, (DateTime?)null));

            cut.Find(".el-date-editor").Click();

            Assert.True(PanelFind(".el-date-time-picker__confirm").HasAttribute("disabled"),
                "空值起步时「确定」本应禁用");
        }

        [Fact]
        public void ConfirmButtonEnablesAfterPickingDateWithEmptyInitialValue()
        {
            var cut = Render<ElDateTimePicker>(p => p.Add(x => x.Value, (DateTime?)null));

            cut.Find(".el-date-editor").Click();
            PickDay(15);

            Assert.False(PanelFind(".el-date-time-picker__confirm").HasAttribute("disabled"),
                "选中日期后「确定」仍禁用 —— 说明 pendingSingleValue 没被设置");
        }

        [Fact]
        public void ConfirmCommitsPickedDateWhenStartedEmpty()
        {
            DateTime? committed = null;
            var cut = Render<ElDateTimePicker>(p => p
                .Add(x => x.Value, (DateTime?)null)
                .Add(x => x.ValueChanged, v => committed = v));

            cut.Find(".el-date-editor").Click();
            PickDay(15);
            PanelFind(".el-date-time-picker__confirm").Click();

            Assert.NotNull(committed);
            Assert.Equal(15, committed.Value.Day);
        }
    }
}
