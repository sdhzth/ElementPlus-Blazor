using Bunit;
using ElementPlus;
using ElementPlus.Lang;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 第四批取词契约：取色器（el.colorpicker.*）、滑块（el.slider.*）、穿梭框（el.transfer.*）
    /// 与日历族（el.datepicker.* + <see cref="PickerLocale"/>）。
    /// <para>
    /// 每族都验三态：<b>没词条</b>＝改动前那个字面量（新属性则干脆不渲染，DOM 零变化）；
    /// <b>有词条</b>＝词条生效，且 <c>{name}</c> 插值出真实数值；<b>显式参数</b>＝压制词条。
    /// </para>
    /// <para>
    /// 夹具里的值统一带 "ZH" 尾巴：兜底值与 EP 权威值几乎同形，不加标记就证明不了取词真的走了词条。
    /// 日历族还有一条独有的判据 —— 本仓有两条语言开关（<c>ElConfigProvider.Locale</c> 与词条语言档），
    /// 两者不一致时必须让 Locale 赢，见 <see cref="PanelFollowsLocaleInsteadOfLangFileWhenTheyDiffer"/>。
    /// </para>
    /// </summary>
    public class LangPickerTextTests : BunitContext
    {
        public LangPickerTextTests()
        {
            Services.AddElementPlusServices();
        }

        private const string ZhJson = @"{
  ""el"": {
    ""colorpicker"": {
      ""defaultLabel"": ""颜色选择器ZH"",
      ""description"": ""当前颜色 {color}ZH，按 Enter 键选择新颜色"",
      ""svLabel"": ""选择饱和度与明度的值ZH"",
      ""svDescription"": ""饱和度 {saturation}, 明度 {brightness}, 当前颜色 {color}ZH"",
      ""hueLabel"": ""选择色相值ZH"",
      ""hueDescription"": ""色相 {hue}, 当前颜色 {color}ZH"",
      ""alphaLabel"": ""选择透明度的值ZH"",
      ""alphaDescription"": ""透明度 {alpha}, 当前颜色 {color}ZH"",
      ""predefineDescription"": ""选择 {value} 作为颜色ZH""
    },
    ""slider"": {
      ""defaultLabel"": ""滑块介于 {min} 至 {max}ZH"",
      ""defaultRangeStartLabel"": ""选择起始值ZH"",
      ""defaultRangeEndLabel"": ""选择结束值ZH""
    },
    ""transfer"": {
      ""noData"": ""无数据ZH"",
      ""noMatch"": ""无匹配数据ZH"",
      ""filterPlaceholder"": ""请输入搜索内容ZH""
    },
    ""datepicker"": {
      ""today"": ""今天ZH"",
      ""cancel"": ""取消ZH"",
      ""clear"": ""清空ZH"",
      ""confirm"": ""确定ZH"",
      ""dateTablePrompt"": ""使用方向键与 Enter 键可选择日期ZH"",
      ""monthTablePrompt"": ""使用方向键与 Enter 键可选择月份ZH"",
      ""yearTablePrompt"": ""使用方向键与 Enter 键可选择年份ZH"",
      ""prevYear"": ""前一年ZH"",
      ""nextYear"": ""后一年ZH"",
      ""prevMonth"": ""上个月ZH"",
      ""nextMonth"": ""下个月ZH"",
      ""year"": ""{year} 年ZH"",
      ""month1"": ""1 月ZH"",
      ""month5"": ""5 月ZH"",
      ""month7"": ""7 月ZH"",
      ""weeks"": { ""sun"": ""日ZH"", ""mon"": ""一ZH"", ""tue"": ""二ZH"", ""wed"": ""三ZH"", ""thu"": ""四ZH"", ""fri"": ""五ZH"", ""sat"": ""六ZH"" },
      ""weeksFull"": { ""sun"": ""星期日ZH"", ""mon"": ""星期一ZH"", ""tue"": ""星期二ZH"", ""wed"": ""星期三ZH"", ""thu"": ""星期四ZH"", ""fri"": ""星期五ZH"", ""sat"": ""星期六ZH"" },
      ""months"": { ""jan"": ""一月ZH"", ""feb"": ""二月ZH"", ""mar"": ""三月ZH"", ""apr"": ""四月ZH"", ""may"": ""五月ZH"", ""jun"": ""六月ZH"", ""jul"": ""七月ZH"", ""aug"": ""八月ZH"", ""sep"": ""九月ZH"", ""oct"": ""十月ZH"", ""nov"": ""十一月ZH"", ""dec"": ""十二月ZH"" }
    }
  }
}";

        private void UseZhLang()
        {
            Services.AddSingleton<ILang>(TestLang.Loaded(ZhJson, "zh-CN"));
        }

        private static AngleSharp.Dom.IElement Trigger<TComponent>(Bunit.IRenderedComponent<TComponent> cut)
            where TComponent : Microsoft.AspNetCore.Components.IComponent
            => cut.Find("div[role='combobox']");

        // ===== 取色器触发器 =====

        [Fact]
        public void ColorPickerTriggerKeepsLiteralWithoutEntries()
        {
            var cut = Render<ElColorPicker>(p => p.Add(x => x.Value, "#409EFF"));

            var trigger = Trigger(cut);
            Assert.Equal("color picker", trigger.GetAttribute("aria-label"));
            // aria-description 是这次新加的属性：没词条就不渲染，DOM 与改动前一致
            Assert.Null(trigger.GetAttribute("aria-description"));
        }

        [Fact]
        public void ColorPickerTriggerUsesEntryAndInterpolatesCurrentColor()
        {
            UseZhLang();
            var cut = Render<ElColorPicker>(p => p.Add(x => x.Value, "#409EFF"));

            var trigger = Trigger(cut);
            Assert.Equal("颜色选择器ZH", trigger.GetAttribute("aria-label"));
            Assert.Equal("当前颜色 #409EFFZH，按 Enter 键选择新颜色", trigger.GetAttribute("aria-description"));
        }

        [Fact]
        public void ColorPickerAriaLabelParameterWinsOverEntry()
        {
            UseZhLang();
            var cut = Render<ElColorPicker>(p => p
                .Add(x => x.Value, "#409EFF")
                .Add(x => x.AriaLabel, "患者肤色"));

            Assert.Equal("患者肤色", Trigger(cut).GetAttribute("aria-label"));
        }

        // ===== 取色器面板 =====

        [Fact]
        public void ColorPickerPanelKeepsLiteralsAndAddsNoValueTextWithoutEntries()
        {
            var cut = Render<ElColorPickerPanel>(p => p
                .Add(x => x.Value, "#ff7800")
                .Add(x => x.ShowAlpha, true)
                .Add(x => x.Predefine, new[] { "#ff0000" }));

            Assert.Equal("color saturation and value", cut.Find(".el-color-svpanel").GetAttribute("aria-label"));
            Assert.Null(cut.Find(".el-color-svpanel").GetAttribute("aria-valuetext"));
            Assert.Equal("hue", cut.Find(".el-color-hue-slider").GetAttribute("aria-label"));
            Assert.Equal("alpha", cut.Find(".el-color-alpha-slider").GetAttribute("aria-label"));
            Assert.Equal("select color #ff0000", cut.Find(".el-color-predefine__color-selector").GetAttribute("aria-label"));
        }

        [Fact]
        public void ColorPickerPanelValueTextInterpolatesRealChannelValues()
        {
            UseZhLang();
            var cut = Render<ElColorPickerPanel>(p => p
                .Add(x => x.Value, "#ff7800")
                .Add(x => x.ShowAlpha, true)
                .Add(x => x.Predefine, new[] { "#ff0000" }));

            // #ff7800 → 饱和度 100、明度 100、色相 28（四舍五入到整数百分比/度数，与 EP 一致）
            var sv = cut.Find(".el-color-svpanel").GetAttribute("aria-valuetext");
            Assert.Equal("饱和度 100, 明度 100, 当前颜色 #ff7800ZH", sv);

            var hue = cut.Find(".el-color-hue-slider").GetAttribute("aria-valuetext");
            Assert.StartsWith("色相 ", hue);
            Assert.EndsWith(" 当前颜色 #ff7800ZH", hue);
            Assert.DoesNotContain("{hue}", hue);

            Assert.Equal("透明度 100, 当前颜色 #ff7800ZH",
                cut.Find(".el-color-alpha-slider").GetAttribute("aria-valuetext"));

            Assert.Equal("选择 #ff0000 作为颜色ZH",
                cut.Find(".el-color-predefine__color-selector").GetAttribute("aria-label"));

            // …Label 一族同样换成词条
            Assert.Equal("选择饱和度与明度的值ZH", cut.Find(".el-color-svpanel").GetAttribute("aria-label"));
            Assert.Equal("选择色相值ZH", cut.Find(".el-color-hue-slider").GetAttribute("aria-label"));
            Assert.Equal("选择透明度的值ZH", cut.Find(".el-color-alpha-slider").GetAttribute("aria-label"));
        }

        // ===== 滑块 =====

        [Fact]
        public void SliderKeepsUnlabelledThumbsWithoutEntries()
        {
            var cut = Render<ElSlider>(p => p
                .Add(x => x.Value, 30)
                .Add(x => x.Min, 10)
                .Add(x => x.Max, 90));

            Assert.Null(cut.Find(".el-slider__input").GetAttribute("aria-label"));
        }

        [Fact]
        public void SliderSingleThumbUsesEntryWithRealMinMax()
        {
            UseZhLang();
            var cut = Render<ElSlider>(p => p
                .Add(x => x.Value, 30)
                .Add(x => x.Min, 10)
                .Add(x => x.Max, 90));

            Assert.Equal("滑块介于 10 至 90ZH", cut.Find(".el-slider__input").GetAttribute("aria-label"));
        }

        [Fact]
        public void SliderRangeGroupAndBothThumbsUseEntries()
        {
            UseZhLang();
            var cut = Render<ElSlider>(p => p
                .Add(x => x.Range, true)
                .Add(x => x.RangeValue, new List<double> { 20, 80 })
                .Add(x => x.Min, 5)
                .Add(x => x.Max, 95));

            Assert.Equal("滑块介于 5 至 95ZH", cut.Find("div[role='group']").GetAttribute("aria-label"));

            var thumbs = cut.FindAll(".el-slider__input--range");
            Assert.Equal(2, thumbs.Count);
            Assert.Equal("选择起始值ZH", thumbs[0].GetAttribute("aria-label"));
            Assert.Equal("选择结束值ZH", thumbs[1].GetAttribute("aria-label"));
        }

        [Fact]
        public void SliderExplicitAriaLabelWinsOverEntry()
        {
            UseZhLang();
            var cut = Render<ElSlider>(p => p
                .Add(x => x.Value, 30)
                .Add(x => x.AriaLabel, "音量"));

            Assert.Equal("音量", cut.Find(".el-slider__input").GetAttribute("aria-label"));
        }

        [Fact]
        public void SliderRangeStartLabelParameterWinsOverEntry()
        {
            UseZhLang();
            var cut = Render<ElSlider>(p => p
                .Add(x => x.Range, true)
                .Add(x => x.RangeValue, new List<double> { 20, 80 })
                .Add(x => x.RangeStartLabel, "下界"));

            var thumbs = cut.FindAll(".el-slider__input--range");
            Assert.Equal("下界", thumbs[0].GetAttribute("aria-label"));
            Assert.Equal("选择结束值ZH", thumbs[1].GetAttribute("aria-label"));
        }

        // ===== 穿梭框 =====

        [Fact]
        public void TransferKeepsEnglishLiteralsWithoutEntries()
        {
            var cut = Render<ElTransfer>(p => p
                .Add(x => x.EnableSearch, true)
                .Add(x => x.List1, Items())
                .Add(x => x.List2, new List<TransferItem>()));

            // 只有空掉的那一列渲染占位文字，左列有数据所以整页只有一处
            Assert.Equal("No data", cut.Find(".el-transfer-panel__empty").TextContent.Trim());
            Assert.Equal("Enter keyword", cut.Find(".el-transfer-panel__filter input").GetAttribute("placeholder"));
        }

        [Fact]
        public void TransferEmptyAndPlaceholderUseEntries()
        {
            UseZhLang();
            var cut = Render<ElTransfer>(p => p
                .Add(x => x.EnableSearch, true)
                .Add(x => x.List1, Items())
                .Add(x => x.List2, new List<TransferItem>()));

            // 左列有数据但一条都匹配不上 → noMatch；右列本来就空 → noData
            cut.Find(".el-transfer-panel__filter input").Input("zzz");

            Assert.Equal("请输入搜索内容ZH", cut.Find(".el-transfer-panel__filter input").GetAttribute("placeholder"));
            Assert.Equal("无匹配数据ZH", cut.FindAll(".el-transfer-panel__empty")[0].TextContent.Trim());
            Assert.Equal("无数据ZH", cut.FindAll(".el-transfer-panel__empty")[1].TextContent.Trim());
        }

        [Fact]
        public void TransferTextParametersWinOverEntries()
        {
            UseZhLang();
            var cut = Render<ElTransfer>(p => p
                .Add(x => x.EnableSearch, true)
                .Add(x => x.EmptyText, "左边空的")
                .Add(x => x.List1SearchPlaceHolder, "筛左边")
                .Add(x => x.List1, new List<TransferItem>())
                .Add(x => x.List2, Items()));

            Assert.Equal("左边空的", cut.Find(".el-transfer-panel__empty").TextContent.Trim());
            var placeholders = cut.FindAll(".el-transfer-panel__filter input").Select(x => x.GetAttribute("placeholder")).ToList();
            Assert.Equal("筛左边", placeholders[0]);
            Assert.Equal("请输入搜索内容ZH", placeholders[1]);
        }

        // ===== 日期面板（el.datepicker.*）=====

        [Fact]
        public void DatePickerPanelWithoutEntriesKeepsMarkupUnchanged()
        {
            var cut = Render<ElDatePickerPanel>(p => p.Add(x => x.Value, new DateTime(2026, 5, 27)));

            Assert.Contains("2026 年", cut.Markup);
            Assert.Contains("5 月", cut.Markup);
            Assert.Equal("日", cut.FindAll(".el-date-table th")[0].TextContent.Trim());
            // 表格名称与单元格/表头的完整星期名都是这次新加的：没词条就一个都不渲染
            Assert.Null(cut.Find(".el-date-table").GetAttribute("aria-label"));
            Assert.Null(cut.FindAll(".el-date-table th")[0].GetAttribute("aria-label"));
            Assert.Null(cut.Find(".el-picker-panel__icon-btn").GetAttribute("aria-label"));
        }

        [Fact]
        public void DatePickerPanelTablesAndHeaderGetEntryNames()
        {
            UseZhLang();
            var cut = Render<ElDatePickerPanel>(p => p.Add(x => x.Value, new DateTime(2026, 5, 27)));

            var table = cut.Find(".el-date-table");
            Assert.Equal("grid", table.GetAttribute("role"));
            Assert.Equal("使用方向键与 Enter 键可选择日期ZH", table.GetAttribute("aria-label"));

            var firstTh = cut.FindAll(".el-date-table th")[0];
            Assert.Equal("日ZH", firstTh.TextContent.Trim());
            Assert.Equal("星期日ZH", firstTh.GetAttribute("aria-label"));

            // 年月标签：词条是 {year} 模板，插值后要落回改动前那个「2026 年」形态
            Assert.Contains("2026 年ZH", cut.Markup);
            Assert.Contains("5 月ZH", cut.Markup);

            var icons = cut.FindAll(".el-picker-panel__icon-btn");
            Assert.Equal("前一年ZH", FirstAriaLabelByClass(icons, "el-icon-d-arrow-left"));
            Assert.Equal("后一年ZH", FirstAriaLabelByClass(icons, "el-icon-d-arrow-right"));
            Assert.Equal("上个月ZH", FirstAriaLabelByClass(icons, "el-icon-arrow-left"));
            Assert.Equal("下个月ZH", FirstAriaLabelByClass(icons, "el-icon-arrow-right"));
            // Clearable 默认开且当前有值 → 清空按钮存在，名称来自 el.datepicker.clear
            Assert.Equal("清空ZH", FirstAriaLabelByClass(icons, "el-icon-close"));
        }

        [Fact]
        public void MonthAndYearPanelUseTheirOwnPromptsAndMonthEntries()
        {
            UseZhLang();

            var monthPanel = Render<ElDatePickerPanel>(p => p
                .Add(x => x.Type, DatePickerPanelType.Month)
                .Add(x => x.Value, new DateTime(2026, 5, 27)));

            var monthTable = monthPanel.Find(".el-month-table");
            Assert.Equal("grid", monthTable.GetAttribute("role"));
            Assert.Equal("使用方向键与 Enter 键可选择月份ZH", monthTable.GetAttribute("aria-label"));

            var cells = monthPanel.FindAll(".el-month-table td");
            Assert.Equal("五月ZH", cells[4].TextContent.Trim());
            // 可见文字用 months.*，aria-label 用 monthN —— 与 EP 的 basic-month-table 同一挂法
            Assert.Equal("5 月ZH", cells[4].GetAttribute("aria-label"));
            Assert.Equal("一月ZH", cells[0].TextContent.Trim());

            var yearPanel = Render<ElDatePickerPanel>(p => p
                .Add(x => x.Type, DatePickerPanelType.Year)
                .Add(x => x.Value, new DateTime(2026, 5, 27)));

            Assert.Equal("使用方向键与 Enter 键可选择年份ZH", yearPanel.Find(".el-year-table").GetAttribute("aria-label"));
        }

        [Fact]
        public void PanelFooterConfirmFallsBackToPreviousLiteral()
        {
            var cut = Render<ElDatePickerPanel>(p => p
                .Add(x => x.Value, new DateTime(2026, 5, 27))
                .Add(x => x.ShowConfirm, true));

            Assert.Equal("确定", cut.Find(".el-picker-panel__footer button").TextContent.Trim());
        }

        [Fact]
        public void PanelFooterConfirmUsesEntry()
        {
            UseZhLang();
            var cut = Render<ElDatePickerPanel>(p => p
                .Add(x => x.Value, new DateTime(2026, 5, 27))
                .Add(x => x.ShowConfirm, true));

            Assert.Equal("确定ZH", cut.Find(".el-picker-panel__footer button").TextContent.Trim());
        }

        [Fact]
        public void PanelFollowsLocaleInsteadOfLangFileWhenTheyDiffer()
        {
            // 词条是 zh-CN，宿主却把 picker 的语言指定成英文：必须让 Locale 赢，回内置英文表
            UseZhLang();
            var cut = Render<ElDatePickerPanel>(p => p
                .Add(x => x.Locale, "en")
                .Add(x => x.Value, new DateTime(2026, 5, 27)));

            Assert.Contains("2026", cut.Markup);
            Assert.DoesNotContain("年ZH", cut.Markup);
            Assert.DoesNotContain("月ZH", cut.Markup);
            Assert.Equal("Sun", cut.FindAll(".el-date-table th")[0].TextContent.Trim());
            Assert.Null(cut.Find(".el-date-table").GetAttribute("aria-label"));
            Assert.Null(cut.FindAll(".el-date-table th")[0].GetAttribute("aria-label"));
        }

        [Fact]
        public void PickerLocaleResolvesParameterThenEntryThenTable()
        {
            var lang = TestLang.Loaded(ZhJson, "zh-CN");

            // 内置表兜底（改动前的行为）
            Assert.Equal("确认", PickerLocale.Resolve("zh-cn", PickerText.Confirm, null));
            // 词条生效（面板页脚因此从「确认」变 EP 的「确定」）
            Assert.Equal("确定ZH", PickerLocale.Resolve("zh-cn", PickerText.Confirm, null, lang));
            // 显式参数压制词条
            Assert.Equal("好的", PickerLocale.Resolve("zh-cn", PickerText.Confirm, "好的", lang));
            // 语言档不一致 → 回表，不串语言
            Assert.Equal("OK", PickerLocale.Resolve("en", PickerText.Confirm, null, lang));
            // EP 没有「开始/结束/时间」这三个裸标签键，所以它们永远走表（不给它们造键）
            Assert.Equal("Start", PickerLocale.Resolve("en", PickerText.Start, null, lang));
        }

        // ===== 日历（ElCalendar）=====

        [Fact]
        public void CalendarWithoutEntriesKeepsTitleButtonsAndWeeks()
        {
            var cut = Render<ElCalendar>(p => p.Add(x => x.Value, new DateTime(2026, 5, 12)));

            Assert.Equal("2026 年 5 月", cut.Find(".el-calendar__title").TextContent.Trim());
            Assert.Equal("日", cut.FindAll("th")[0].TextContent.Trim());

            var buttons = cut.FindAll(".el-calendar__header button");
            Assert.Equal("上个月", buttons[0].TextContent.Trim());
            Assert.Equal("今天", buttons[1].TextContent.Trim());
            Assert.Equal("下个月", buttons[2].TextContent.Trim());
        }

        [Fact]
        public void CalendarTitleButtonsAndWeeksUseEntries()
        {
            UseZhLang();
            var cut = Render<ElCalendar>(p => p.Add(x => x.Value, new DateTime(2026, 5, 12)));

            // EP 的标题组成为 {year} + el.datepicker.year + el.datepicker.monthM
            Assert.Equal("2026 年ZH 5 月ZH", cut.Find(".el-calendar__title").TextContent.Trim());
            Assert.Equal("日ZH", cut.FindAll("th")[0].TextContent.Trim());
            Assert.Equal("六ZH", cut.FindAll("th")[6].TextContent.Trim());

            var buttons = cut.FindAll(".el-calendar__header button");
            Assert.Equal("上个月ZH", buttons[0].TextContent.Trim());
            Assert.Equal("今天ZH", buttons[1].TextContent.Trim());
            Assert.Equal("下个月ZH", buttons[2].TextContent.Trim());
        }

        [Fact]
        public void CalendarWeekDaysParameterWinsOverEntries()
        {
            UseZhLang();
            var cut = Render<ElCalendar>(p => p
                .Add(x => x.Value, new DateTime(2026, 5, 12))
                .Add(x => x.WeekDays, new[] { "A", "B", "C", "D", "E", "F", "G" }));

            Assert.Equal("A", cut.FindAll("th")[0].TextContent.Trim());
        }

        private static List<TransferItem> Items()
        {
            return new List<TransferItem>
            {
                new TransferItem { Id = "a", Label = "Alpha" },
                new TransferItem { Id = "b", Label = "Beta" }
            };
        }

        private static string FirstAriaLabelByClass(IEnumerable<AngleSharp.Dom.IElement> elements, string cssClass)
        {
            return elements.First(x => (x.GetAttribute("class") ?? string.Empty).Contains(cssClass, StringComparison.Ordinal))
                .GetAttribute("aria-label");
        }
    }
}
