using Bunit;
using ElementPlus;
using ElementPlus.ControlConfigs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElTimePickerTests : BunitContext
    {
        public ElTimePickerTests()
        {
            // 滚轮与 ElDateTimePicker 共用同一套 JS 助手（居中 / 吸附），需要 Loose 模式。
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddElementServices();
        }

        [Fact]
        public void RendersFormattedSingleValue()
        {
            var cut = Render<ElTimePicker>(parameters => parameters
                .Add(x => x.Value, new TimeSpan(14, 30, 45))
                .Add(x => x.Format, "HH:mm"));

            Assert.Equal("14:30", cut.Find("input").GetAttribute("value"));
            Assert.Contains("el-date-editor--time", cut.Find(".el-date-editor").ClassList);
        }

        [Fact]
        public void SelectsTimeFromPanelAndEmitsValueFormat()
        {
            TimeSpan? value = null;
            var stringValue = string.Empty;
            var changes = new List<TimeSpan?>();
            var cut = Render<ElTimePicker>(parameters => parameters
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.ValueFormat, "HH:mm:ss")
                .Add(x => x.StringValueChanged, next => stringValue = next)
                .Add(x => x.OnChange, next => changes.Add(next)));

            cut.Find(".el-date-editor").Click();
            SelectTimePart("single", "hour", 9);
            SelectTimePart("single", "minute", 45);
            SelectTimePart("single", "second", 30);
            Render(GetDropdown().OptionContent).Find(".el-time-panel__btn.confirm").Click();

            Assert.Equal(new TimeSpan(9, 45, 30), value);
            Assert.Equal("09:45:30", stringValue);
            Assert.Equal(new TimeSpan(9, 45, 30), changes[0]);
        }

        [Fact]
        public void EditableInputParsesFormattedValue()
        {
            TimeSpan? value = null;
            var cut = Render<ElTimePicker>(parameters => parameters
                .Add(x => x.Format, "HH:mm")
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find("input").Change("08:15");

            Assert.Equal(new TimeSpan(8, 15, 0), value);
        }

        [Fact]
        public void RangeSelectionOrdersValuesAndEmitsFormattedRange()
        {
            IList<TimeSpan?> range = new List<TimeSpan?>();
            IList<string> stringRange = new List<string>();
            var cut = Render<ElTimePicker>(parameters => parameters
                .Add(x => x.Type, TimePickerType.TimeRange)
                .Add(x => x.DefaultStartTime, new TimeSpan(20, 0, 0))
                .Add(x => x.DefaultEndTime, new TimeSpan(8, 0, 0))
                .Add(x => x.RangeValueChanged, next => range = next)
                .Add(x => x.ValueFormat, "HH:mm:ss")
                .Add(x => x.StringRangeValueChanged, next => stringRange = next));

            cut.Find(".el-date-editor").Click();
            SelectTimePart("start", "hour", 20);
            SelectTimePart("start", "minute", 30);
            SelectTimePart("end", "hour", 8);
            SelectTimePart("end", "minute", 15);
            Render(GetDropdown().OptionContent).Find(".el-time-panel__btn.confirm").Click();

            Assert.Equal(new TimeSpan(8, 15, 0), range[0]);
            Assert.Equal(new TimeSpan(20, 30, 0), range[1]);
            Assert.Equal(new[] { "08:15:00", "20:30:00" }, stringRange);
        }

        [Fact]
        public void ClearableClearsSingleValue()
        {
            TimeSpan? value = new TimeSpan(14, 30, 45);
            var cleared = false;
            var cut = Render<ElTimePicker>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.OnClear, () => cleared = true));

            cut.Find(".el-date-editor").MouseOver();
            cut.Find(".el-input__suffix .el-input__icon").Click();

            Assert.Null(value);
            Assert.True(cleared);
        }

        [Fact]
        public void GeneratedFormUsesTimePickerAndUpdatesModel()
        {
            var model = new GeneratedTimePickerModel
            {
                StartAt = new TimeSpan(9, 15, 0)
            };

            var cut = Render<ElForm>(parameters => parameters
                .Add(x => x.EntityType, typeof(GeneratedTimePickerModel))
                .Add(x => x.Value, model));

            var wrapper = cut.Find(".el-date-editor");
            var input = cut.Find("input");

            Assert.Contains("el-date-editor--time", wrapper.ClassList);
            Assert.Contains("el-input--large", wrapper.ClassList);
            Assert.Equal("Start", input.GetAttribute("placeholder"));
            Assert.Equal("09:15", input.GetAttribute("value"));

            wrapper.Click();
            SelectTimePart("single", "hour", 10);
            SelectTimePart("single", "minute", 45);
            Render(GetDropdown().OptionContent).Find(".el-time-panel__btn.confirm").Click();

            Assert.Equal(new TimeSpan(10, 45, 0), model.StartAt);
        }

        [Fact]
        public void TabIndexRendersOnInput()
        {
            var cut = Render<ElTimePicker>(parameters => parameters
                .Add(x => x.Value, new TimeSpan(14, 30, 45))
                .Add(x => x.Tabindex, 5));

            Assert.Equal("5", cut.Find("input").GetAttribute("tabindex"));
        }

        [Fact]
        public void RangePanelGivesEachCellItsOwnSelectionBandHost()
        {
            var cut = Render<ElTimePicker>(parameters => parameters
                .Add(x => x.Type, TimePickerType.TimeRange)
                .Add(x => x.ValueFormat, "HH:mm:ss"));

            cut.Find(".el-date-editor").Click();
            var panel = Render(GetDropdown().OptionContent);

            // EP panel-time-range：壳是 __content，带线宿主下移到每个 cell 的 __body。
            // 若把两条 __body 合回共用的 .el-time-panel__content，宿主中心会被两个 33px 表头压低约 19px，
            // 选中项就不再停在两条横线中间（浏览器实测见 Temp/diag-batch9-timepicker*.log）。
            var cells = panel.FindAll(".el-time-range-picker__cell");
            Assert.Equal(2, cells.Count);
            // 壳只有一层；旧的 is-left / is-right 两栏结构（共用带线宿主）不再出现
            Assert.Single(panel.FindAll(".el-time-range-picker__content"));
            Assert.Empty(panel.FindAll(".el-time-range-picker__content.is-left"));
            Assert.Empty(panel.FindAll(".el-time-range-picker__content.is-right"));

            var bands = panel.FindAll(".el-time-panel__content");
            Assert.Equal(2, bands.Count);
            foreach (var band in bands)
            {
                Assert.Contains("el-time-range-picker__body", band.ClassList);
                Assert.Contains("has-seconds", band.ClassList);
                Assert.Equal("div", band.ParentElement.LocalName);
                Assert.Contains("el-time-range-picker__cell", band.ParentElement.ClassList);
                Assert.Equal(1, band.ChildElementCount);
            }

            Assert.Equal(6, panel.FindAll(".el-time-spinner__wrapper").Count);
            Assert.Equal(3, panel.FindAll(".el-time-spinner__wrapper[data-time-panel='start']").Count);
            Assert.Equal(3, panel.FindAll(".el-time-spinner__wrapper[data-time-panel='end']").Count);

            // 壳必须仍是全部列的祖先：dom.js 的 elementTimeSpinnerAttach 按后代查列
            var shell = panel.Find(".el-time-range-picker__content");
            Assert.Equal(6, shell.QuerySelectorAll(".el-time-spinner__wrapper").Length);
        }

        [Fact]
        public void SinglePanelKeepsOneSharedSelectionBandHost()
        {
            var cut = Render<ElTimePicker>(parameters => parameters
                .Add(x => x.Value, new TimeSpan(14, 30, 45))
                .Add(x => x.ValueFormat, "HH:mm:ss"));

            cut.Find(".el-date-editor").Click();
            var panel = Render(GetDropdown().OptionContent);

            var band = panel.Find(".el-time-panel__content");
            Assert.Contains("has-seconds", band.ClassList);
            Assert.Equal(1, panel.FindAll(".el-time-panel__content").Count);
            Assert.Empty(panel.FindAll(".el-time-range-picker__cell"));
            Assert.Equal(3, panel.FindAll(".el-time-spinner__wrapper").Count);
        }

        [Fact]
        public void SinglePanelOmitsHasSecondsWithoutSecondsFormat()
        {
            var cut = Render<ElTimePicker>(parameters => parameters
                .Add(x => x.Value, new TimeSpan(14, 30, 45))
                .Add(x => x.Format, "HH:mm"));

            cut.Find(".el-date-editor").Click();
            var panel = Render(GetDropdown().OptionContent);

            var band = panel.Find(".el-time-panel__content");
            Assert.DoesNotContain("has-seconds", band.ClassList);
            Assert.Equal(2, panel.FindAll(".el-time-spinner__wrapper").Count);
        }

        [Fact]
        public void RangeSeparatorCarriesConfiguredText()
        {
            var cut = Render<ElTimePicker>(parameters => parameters
                .Add(x => x.Type, TimePickerType.TimeRange)
                .Add(x => x.RangeSeparator, "至"));

            var separator = cut.Find(".el-date-editor--timerange .el-range-separator");

            Assert.Equal("至", separator.TextContent);
            // 裁切是几何问题（浏览器探针判），bUnit 只钉住「分隔符是 range-editor 的直接子级」这条前提：
            // index.css 的 flex:1 居中规则要靠它所在的 inline-flex 宿主才生效。
            Assert.Contains("el-range-editor", separator.ParentElement.ClassList);
        }

        private void SelectTimePart(string panel, string part, int value)
        {
            Render(GetDropdown().OptionContent)
                .Find($"li[data-time-panel='{panel}'][data-time-part='{part}'][data-time-value='{value}']")
                .Click();
        }

        private DropDownOption GetDropdown()
        {
            return Services.GetRequiredService<PopupService>().SelectDropDownOptions.Single();
        }

        private class GeneratedTimePickerModel
        {
            [TimePicker(Format = "HH:mm", Placeholder = "Start", Size = InputSize.Large)]
            public TimeSpan StartAt { get; set; }
        }
    }
}
