using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElDateTimePickerTests : BunitContext
    {
        public ElDateTimePickerTests()
        {
            // 时间滚轮要调 JS（居中 + 关外点），bUnit 默认 Strict 会对未注册的调用抛异常。
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddElementServices();
        }

        [Fact]
        public void RendersFormattedSingleValue()
        {
            var cut = Render<ElDateTimePicker>(parameters => parameters
                .Add(x => x.Value, new DateTime(2026, 5, 27, 14, 30, 45))
                .Add(x => x.Format, "YYYY/MM/DD HH:mm:ss"));

            Assert.Equal("2026/05/27 14:30:45", cut.Find("input").GetAttribute("value"));
            Assert.Contains("el-date-editor--datetime", cut.Find(".el-date-editor").ClassList);
        }

        [Fact]
        public void SelectsDateTimeFromPopupAndEmitsValueFormat()
        {
            DateTime? value = new DateTime(2026, 5, 1, 8, 0, 0);
            var stringValue = string.Empty;
            var changes = new List<DateTime?>();
            var cut = Render<ElDateTimePicker>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.ValueFormat, "YYYY-MM-DD HH:mm:ss")
                .Add(x => x.StringValueChanged, next => stringValue = next)
                .Add(x => x.OnChange, next => changes.Add(next)));

            cut.Find(".el-date-editor").Click();
            PanelFindAll("td.available")
                .First(x => x.QuerySelector("span")?.TextContent.Trim() == "15")
                .Click();

            OpenTimePanel();
            ClickTimePart("single", "hour", 9);
            ClickTimePart("single", "minute", 45);
            ClickTimePart("single", "second", 0);

            PanelFind(".el-date-time-picker__confirm").Click();

            Assert.Equal(new DateTime(2026, 5, 15, 9, 45, 0), value);
            Assert.Equal("2026-05-15 09:45:00", stringValue);
            Assert.Equal(new DateTime(2026, 5, 15, 9, 45, 0), changes[0]);
        }

        [Fact]
        public void ShortcutSelectsSingleValue()
        {
            DateTime? value = null;
            var cut = Render<ElDateTimePicker>(parameters => parameters
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.Shortcuts, new[]
                {
                    new DateTimePickerShortcut { Text = "Morning", Value = new DateTime(2026, 5, 27, 8, 30, 0) }
                }));

            cut.Find(".el-date-editor").Click();
            PanelFind(".el-picker-panel__shortcut").Click();

            Assert.Equal(new DateTime(2026, 5, 27, 8, 30, 0), value);
        }

        [Fact]
        public void DisabledDatePreventsSelection()
        {
            DateTime? value = new DateTime(2026, 5, 1, 8, 0, 0);
            var cut = Render<ElDateTimePicker>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.DisabledDate, day => day.Day == 15));

            cut.Find(".el-date-editor").Click();
            PanelFindAll("td.disabled")
                .First(x => x.QuerySelector("span")?.TextContent.Trim() == "15")
                .Click();
            PanelFind(".el-date-time-picker__confirm").Click();

            Assert.Equal(new DateTime(2026, 5, 1, 8, 0, 0), value);
        }

        [Fact]
        public void RangeSelectionOrdersValuesAndEmitsFormattedRange()
        {
            IList<DateTime?> range = new List<DateTime?>();
            IList<string> stringRange = new List<string>();
            var cut = Render<ElDateTimePicker>(parameters => parameters
                .Add(x => x.Type, DateTimePickerType.DateTimeRange)
                .Add(x => x.SinglePanel, true)
                .Add(x => x.DefaultValue, new DateTime(2026, 5, 1))
                .Add(x => x.DefaultStartTime, new TimeSpan(8, 0, 0))
                .Add(x => x.DefaultEndTime, new TimeSpan(18, 0, 0))
                .Add(x => x.RangeValueChanged, next => range = next)
                .Add(x => x.ValueFormat, "YYYY-MM-DD HH:mm:ss")
                .Add(x => x.StringRangeValueChanged, next => stringRange = next));

            cut.Find(".el-date-editor").Click();
            PanelFindAll("td.available").First(x => x.QuerySelector("span")?.TextContent.Trim() == "20").Click();

            OpenTimePanel("start");
            ClickTimePart("start", "hour", 9);
            ClickTimePart("start", "minute", 0);

            PanelFindAll("td.available").First(x => x.QuerySelector("span")?.TextContent.Trim() == "10").Click();

            OpenTimePanel("end");
            ClickTimePart("end", "hour", 17);
            ClickTimePart("end", "minute", 30);

            PanelFind(".el-date-time-picker__confirm").Click();

            Assert.Equal(new DateTime(2026, 5, 10, 9, 0, 0), range[0]);
            Assert.Equal(new DateTime(2026, 5, 20, 17, 30, 0), range[1]);
            Assert.Equal(new[] { "2026-05-10 09:00:00", "2026-05-20 17:30:00" }, stringRange);
        }

        [Fact]
        public void ClearableClearsSingleValue()
        {
            DateTime? value = new DateTime(2026, 5, 27, 14, 30, 45);
            var cleared = false;
            var cut = Render<ElDateTimePicker>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.OnClear, _ => cleared = true));

            cut.Find(".el-date-editor").MouseOver();
            cut.Find(".el-input__suffix .el-input__icon").Click();

            Assert.Null(value);
            Assert.True(cleared);
        }

        // #148 B5：对齐 EP onPanel-change —— 翻月时把切换后显示的日期上抛（只暴露日期部分）。
        [Fact]
        public void OnPanelChange_FiresWhenNavigatingMonth()
        {
            DateTime? fired = null;
            var cut = Render<ElDateTimePicker>(parameters => parameters
                .Add(x => x.DefaultValue, new DateTime(2026, 5, 1))
                .Add(x => x.OnPanelChange, next => fired = next));

            cut.Find(".el-date-editor").Click();

            Assert.Null(fired);

            // 下一月按钮（el-icon-arrow-right 区分于「翻年」的 d-arrow-right）。
            PanelFind(".el-date-picker__next-btn.el-icon-arrow-right").Click();

            Assert.Equal(new DateTime(2026, 6, 1), fired);
        }

        // #148 B5：对齐 EP value-on-clear —— 清除时回写自定义值（而非默认 null）。
        [Fact]
        public void ValueOnClear_WritesCustomValueOnClear()
        {
            DateTime? value = new DateTime(2026, 5, 27, 14, 30, 45);
            var cut = Render<ElDateTimePicker>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.ValueOnClear, new DateTime(2026, 1, 1, 0, 0, 0)));

            cut.Find(".el-date-editor").MouseOver();
            cut.Find(".el-input__suffix .el-input__icon").Click();

            Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0), value);
        }

        #region 时间区域：文字可见 + 滚轮面板（2026-09-21）

        // 回归：旧实现用原生 <input type="time">，而 .el-picker-panel__footer 带 font-size:0，
        // 输入框继承后字号为 0 → 时间文字完全看不见（用户截图：红框里只有「时间」标签，右侧一片空白）。
        [Fact]
        public void TimeField_ShowsPendingTimeTextAndHasNoNativeTimeInput()
        {
            var cut = Render<ElDateTimePicker>(parameters => parameters
                .Add(x => x.Value, new DateTime(2026, 5, 27, 14, 30, 45)));

            cut.Find(".el-date-editor").Click();

            var input = PanelFind(".el-date-time-picker__time-input");
            Assert.Equal("14:30:45", input.GetAttribute("value"));
            Assert.True(input.HasAttribute("readonly"));
            Assert.Equal("时间", PanelFind(".el-date-time-picker__time-label").TextContent.Trim());

            // 原生 type=time 控件已彻底移除（它正是「看不见」的元凶）
            Assert.Empty(PanelFindAll("input[type='time']"));
        }

        [Fact]
        public void TimePanel_OpensWithThreeSpinnerColumns()
        {
            var cut = Render<ElDateTimePicker>(parameters => parameters
                .Add(x => x.Value, new DateTime(2026, 5, 27, 14, 30, 45)));

            cut.Find(".el-date-editor").Click();
            Assert.Empty(PanelFindAll(".el-date-time-picker__time-panel"));

            OpenTimePanel();

            Assert.Single(PanelFindAll(".el-date-time-picker__time-panel"));
            var columns = PanelFindAll(".el-time-spinner__wrapper").ToList();
            Assert.Equal(3, columns.Count);
            Assert.Equal(24, columns[0].QuerySelectorAll("li").Length);
            Assert.Equal(60, columns[1].QuerySelectorAll("li").Length);
            Assert.Equal(60, columns[2].QuerySelectorAll("li").Length);

            // 当前值所在项高亮
            Assert.Contains("active", PanelFind("li[data-time-part='hour'][data-time-value='14']").ClassList);
            Assert.Contains("active", PanelFind("li[data-time-part='minute'][data-time-value='30']").ClassList);
        }

        // 滚轮必须是 EP 的三层结构：wrapper(滚动容器) > list(80px 占位) > item。
        // 旧实现把 ul 同时当 wrapper 和 list（还自己加 padding:80px 0），
        // 中间选中带（.el-time-panel__content::before/::after）会跟着一起滚，
        // 于是「显示停在哪一项」和真实值脱节 —— 14:00:00 时分/秒列却停在 02。
        [Fact]
        public void TimePanel_UsesElementPlusSpinnerDom()
        {
            var cut = Render<ElDateTimePicker>(parameters => parameters
                .Add(x => x.Value, new DateTime(2026, 5, 27, 14, 30, 45)));

            cut.Find(".el-date-editor").Click();
            OpenTimePanel();

            // 三列 → 中间选中带要跨三列（index.css 的 .has-seconds 规则）
            Assert.Contains("has-seconds", PanelFind(".el-time-panel__content").ClassList);
            Assert.Contains("has-seconds", PanelFind(".el-time-spinner").ClassList);

            var columns = PanelFindAll(".el-time-spinner__wrapper").ToList();
            Assert.Equal(3, columns.Count);

            foreach (var column in columns)
            {
                // 滚动容器是 div，不是 ul：只有这样选中带才固定不动
                Assert.Equal("DIV", column.TagName);
                Assert.Contains("el-time-spinner__wrapper", column.ClassList);

                var list = column.QuerySelector(".el-time-spinner__list");
                Assert.NotNull(list);
                Assert.Equal("UL", list.TagName);

                // 所有选项都在 list 里（::before/::after 提供 80px 占位，靠 CSS 而非内联 padding）
                var items = list.QuerySelectorAll(".el-time-spinner__item");
                Assert.True(items.Length >= 24, "列内至少要有 24 个选项");
                Assert.All(items, x => Assert.Equal("LI", x.TagName));
            }

            // 当前值所在项带 active（浏览器里的「黑色加粗 + 停在中间带」由此驱动）
            Assert.Contains("active", PanelFind("li[data-time-part='hour'][data-time-value='14']").ClassList);
            Assert.Contains("active", PanelFind("li[data-time-part='minute'][data-time-value='30']").ClassList);
            Assert.Contains("active", PanelFind("li[data-time-part='second'][data-time-value='45']").ClassList);
        }

        // 用户要求：三个被选中项要「显目」——黑色 + 加粗。
        [Fact]
        public void Guard_ActiveSpinnerItemIsBlackAndBold()
        {
            var css = CssSection();
            Assert.Matches(
                new Regex(@"\.el-date-time-picker__time-panel\s+\.el-time-spinner__item\.active[^{]*\{[^}]*color:\s*#000"),
                css);
            Assert.Matches(
                new Regex(@"\.el-date-time-picker__time-panel\s+\.el-time-spinner__item\.active[^{]*\{[^}]*font-weight:\s*700"),
                css);
        }

        [Fact]
        public void TimeField_AriaExpandedUsesStringValues()
        {
            var cut = Render<ElDateTimePicker>(parameters => parameters
                .Add(x => x.Value, new DateTime(2026, 5, 27, 14, 30, 45)));

            // 传 bool 给 aria-* 会被 Blazor 渲染成「无值属性」（aria-expanded=""），辅助技术读不到状态；
            // 必须显式给 "true"/"false"（与 ElButton / ElInputTag / ElSelect 的既有写法一致）。
            var trigger = cut.Find(".el-date-editor");
            Assert.Equal("false", trigger.GetAttribute("aria-expanded"));
            Assert.Equal("false", trigger.GetAttribute("aria-disabled"));

            trigger.Click();
            Assert.Equal("true", cut.Find(".el-date-editor").GetAttribute("aria-expanded"));
            Assert.Equal("false", PanelFind(".el-date-time-picker__time-input").GetAttribute("aria-expanded"));

            OpenTimePanel();
            Assert.Equal("true", PanelFind(".el-date-time-picker__time-input").GetAttribute("aria-expanded"));
        }

        [Fact]
        public void TimePanel_SpinnerAriaSelectedUsesStringValues()
        {
            var cut = Render<ElDateTimePicker>(parameters => parameters
                .Add(x => x.Value, new DateTime(2026, 5, 27, 14, 30, 45)));

            cut.Find(".el-date-editor").Click();
            OpenTimePanel();

            var items = PanelFindAll(".el-time-spinner__item").ToList();

            // 每项都必须带可读取的 "true"/"false"（无值属性会让辅助技术读不到状态）。
            Assert.All(items, x => Assert.Contains(x.GetAttribute("aria-selected"), new[] { "true", "false" }));

            // 时/分/秒三列各恰好一项为 true。
            Assert.Equal(3, items.Count(x => x.GetAttribute("aria-selected") == "true"));
            var columns = PanelFindAll(".el-time-spinner__wrapper").ToList();
            Assert.Equal(3, columns.Count);
            Assert.All(columns, c => Assert.Single(c.QuerySelectorAll("li[aria-selected='true']")));

            Assert.Equal("true", PanelFind("li[data-time-part='hour'][data-time-value='14']").GetAttribute("aria-selected"));
            Assert.Equal("false", PanelFind("li[data-time-part='hour'][data-time-value='15']").GetAttribute("aria-selected"));
        }

        [Fact]
        public void TimePanel_SelectsPartsAndWritesVisibleText()
        {

            var cut = Render<ElDateTimePicker>(parameters => parameters
                .Add(x => x.Value, new DateTime(2026, 5, 27, 14, 30, 45)));

            cut.Find(".el-date-editor").Click();
            OpenTimePanel();
            ClickTimePart("single", "hour", 9);
            ClickTimePart("single", "minute", 5);
            ClickTimePart("single", "second", 7);

            Assert.Equal("09:05:07", PanelFind(".el-date-time-picker__time-input").GetAttribute("value"));
        }

        [Fact]
        public void TimePanel_ConfirmClosesPanelAndKeepsTime()
        {
            DateTime? value = null;
            var cut = Render<ElDateTimePicker>(parameters => parameters
                .Add(x => x.Value, new DateTime(2026, 5, 27, 14, 30, 45))
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-date-editor").Click();
            OpenTimePanel();
            ClickTimePart("single", "hour", 9);
            PanelFind(".el-time-panel__btn.confirm").Click();

            Assert.Empty(PanelFindAll(".el-date-time-picker__time-panel"));
            Assert.Equal("09:30:45", PanelFind(".el-date-time-picker__time-input").GetAttribute("value"));

            PanelFind(".el-date-time-picker__confirm").Click();

            Assert.Equal(new DateTime(2026, 5, 27, 9, 30, 45), value);
        }

        [Fact]
        public void TimePanel_CancelRevertsPendingTime()
        {
            var cut = Render<ElDateTimePicker>(parameters => parameters
                .Add(x => x.Value, new DateTime(2026, 5, 27, 14, 30, 45)));

            cut.Find(".el-date-editor").Click();
            OpenTimePanel();
            ClickTimePart("single", "hour", 9);
            Assert.Equal("09:30:45", PanelFind(".el-date-time-picker__time-input").GetAttribute("value"));

            PanelFind(".el-time-panel__btn.cancel").Click();

            Assert.Empty(PanelFindAll(".el-date-time-picker__time-panel"));
            Assert.Equal("14:30:45", PanelFind(".el-date-time-picker__time-input").GetAttribute("value"));
        }

        [Fact]
        public void TimePanel_ClickingTimeFieldAgainCollapsesIt()
        {
            var cut = Render<ElDateTimePicker>(parameters => parameters
                .Add(x => x.Value, new DateTime(2026, 5, 27, 14, 30, 45)));

            cut.Find(".el-date-editor").Click();
            OpenTimePanel();
            Assert.Single(PanelFindAll(".el-date-time-picker__time-panel"));

            OpenTimePanel();

            Assert.Empty(PanelFindAll(".el-date-time-picker__time-panel"));
        }

        [Fact]
        public void RangeMode_ShowsTwoTimeFieldsWithStartAndEndLabels()
        {
            var cut = Render<ElDateTimePicker>(parameters => parameters
                .Add(x => x.Type, DateTimePickerType.DateTimeRange)
                .Add(x => x.SinglePanel, true)
                .Add(x => x.DefaultValue, new DateTime(2026, 5, 1))
                .Add(x => x.DefaultStartTime, new TimeSpan(8, 0, 0))
                .Add(x => x.DefaultEndTime, new TimeSpan(18, 0, 0)));

            cut.Find(".el-date-editor").Click();

            var labels = PanelFindAll(".el-date-time-picker__time-label").Select(x => x.TextContent.Trim()).ToList();
            Assert.Equal(new[] { "开始", "结束" }, labels);
            Assert.Equal("08:00:00", PanelFind(".el-date-time-picker__start-time").GetAttribute("value"));
            Assert.Equal("18:00:00", PanelFind(".el-date-time-picker__end-time").GetAttribute("value"));
        }

        #endregion

        #region 多语言（默认中文）

        [Fact]
        public void Locale_English_ShowsEnglishTexts()
        {
            OpenPanelWithLocale("en-US");

            Assert.Equal("Time", PanelFind(".el-date-time-picker__time-label").TextContent.Trim());
            Assert.Equal("OK", PanelFind(".el-date-time-picker__confirm").TextContent.Trim());

            OpenTimePanel();

            Assert.Equal("Cancel", PanelFind(".el-time-panel__btn.cancel").TextContent.Trim());
            Assert.Equal("OK", PanelFind(".el-time-panel__btn.confirm").TextContent.Trim());
        }

        [Fact]
        public void Locale_TraditionalChinese_ShowsTraditionalTexts()
        {
            OpenPanelWithLocale("zh-TW");

            Assert.Equal("時間", PanelFind(".el-date-time-picker__time-label").TextContent.Trim());
            Assert.Equal("確認", PanelFind(".el-date-time-picker__confirm").TextContent.Trim());
        }

        [Fact]
        public void Locale_TraditionalChinese_StartAndEndLabelsUseTraditional()
        {
            var cut = Render<ElConfigProvider>(parameters => parameters
                .Add(x => x.Locale, "zh-Hant")
                .AddChildContent<ElDateTimePicker>(p => p
                    .Add(x => x.Type, DateTimePickerType.DateTimeRange)
                    .Add(x => x.SinglePanel, true)
                    .Add(x => x.DefaultValue, new DateTime(2026, 5, 1))));

            cut.Find(".el-date-editor").Click();

            Assert.Equal(
                new[] { "開始", "結束" },
                PanelFindAll(".el-date-time-picker__time-label").Select(x => x.TextContent.Trim()).ToArray());
        }

        [Fact]
        public void Locale_UnsupportedLanguage_FallsBackToSimplifiedChinese()
        {
            OpenPanelWithLocale("ja-JP");

            Assert.Equal("时间", PanelFind(".el-date-time-picker__time-label").TextContent.Trim());
            Assert.Equal("确认", PanelFind(".el-date-time-picker__confirm").TextContent.Trim());
        }

        [Fact]
        public void TextParameters_OverrideLocale()
        {
            var cut = Render<ElConfigProvider>(parameters => parameters
                .Add(x => x.Locale, "en-US")
                .AddChildContent<ElDateTimePicker>(p => p
                    .Add(x => x.Value, new DateTime(2026, 5, 27, 14, 30, 45))
                    .Add(x => x.TimeLabelText, "时刻")
                    .Add(x => x.ConfirmText, "应用")
                    .Add(x => x.CancelText, "返回")));

            cut.Find(".el-date-editor").Click();

            Assert.Equal("时刻", PanelFind(".el-date-time-picker__time-label").TextContent.Trim());
            Assert.Equal("应用", PanelFind(".el-date-time-picker__confirm").TextContent.Trim());

            OpenTimePanel();

            Assert.Equal("返回", PanelFind(".el-time-panel__btn.cancel").TextContent.Trim());
            Assert.Equal("应用", PanelFind(".el-time-panel__btn.confirm").TextContent.Trim());
        }

        [Theory]
        [InlineData(null, "zh-cn")]
        [InlineData("", "zh-cn")]
        [InlineData("zh-CN", "zh-cn")]
        [InlineData("zh_CN", "zh-cn")]
        [InlineData("zh-Hans-CN", "zh-cn")]
        [InlineData("en-US", "en")]
        [InlineData("EN", "en")]
        [InlineData("zh-TW", "zh-tw")]
        [InlineData("zh-HK", "zh-tw")]
        [InlineData("zh-Hant", "zh-tw")]
        [InlineData("de-DE", "zh-cn")]
        public void Locale_Normalization(string locale, string expected)
        {
            Assert.Equal(expected, PickerLocale.Normalize(locale));
        }

        #endregion

        #region 守卫：CSS 不裁剪确认按钮 / 不使用新语法

        // 面板宽 322px 塞进 320px 的滚动内容盒 → 横向溢出被 .el-scrollbar{overflow:hidden} 裁掉，
        // 页脚右侧的确认按钮被切（用户截图：按钮「有一部分在框外」）。
        [Fact]
        public void Guard_PanelIsPinnedInsideDropdownToAvoidClipping()
        {
            Assert.Matches(
                new Regex(@"\.el-date-time-picker__dropdown\s+\.el-picker-panel\s*\{[^}]*max-width:\s*100%"),
                CssSection());
        }

        // 页脚 font-size 为 0，时间输入框必须显式声明字号，否则文字不可见。
        [Fact]
        public void Guard_TimeInputDeclaresExplicitFontSize()
        {
            var match = Regex.Match(
                CssSection(),
                @"\.el-date-time-picker__time-input\s*\{[^}]*?font-size:\s*([^;}]+)");

            Assert.True(match.Success, "时间输入框未显式声明 font-size —— 页脚 font-size 为 0，继承后时间文字不可见");

            // 允许字面量，也允许主题变量；但必须是 12px（或带 12px 回落的变量），且不得继承 / 归零。
            var value = match.Groups[1].Value.Trim();
            Assert.DoesNotContain("inherit", value);
            Assert.Contains("12px", value);
        }

        [Fact]
        public void Guard_TimePanelCssUsesNoModernSyntax()
        {
            var section = Regex.Replace(CssSection(), @"/\*[\s\S]*?\*/", string.Empty);

            Assert.DoesNotContain(":has(", section);
            Assert.DoesNotContain(":is(", section);
            Assert.DoesNotContain(":where(", section);
            Assert.DoesNotContain("clamp(", section);
            Assert.DoesNotMatch(@"(?<![-\w])gap\s*:", section);
            Assert.DoesNotMatch(@"(?<![-\w])(margin-inline|padding-inline|border-inline|inset-block|inset-inline)\s*:", section);
        }

        #endregion

        private void OpenPanelWithLocale(string locale)
        {
            var cut = Render<ElConfigProvider>(parameters => parameters
                .Add(x => x.Locale, locale)
                .AddChildContent<ElDateTimePicker>(p => p.Add(x => x.Value, new DateTime(2026, 5, 27, 14, 30, 45))));

            cut.Find(".el-date-editor").Click();
        }

        /// <summary>点击时间输入框：展开（已展开则收起）滚轮面板。</summary>
        private void OpenTimePanel(string panel = "single")
        {
            PanelFind($".el-date-time-picker__{panel}-time").Click();
        }

        private void ClickTimePart(string panel, string part, int value)
        {
            PanelFind($"li[data-time-panel='{panel}'][data-time-part='{part}'][data-time-value='{value}']").Click();
        }

        /// <summary>在当前弹层内容里查一个元素（每次都是最新状态）。</summary>
        private AngleSharp.Dom.IElement PanelFind(string selector)
        {
            return Render(GetDropdown().OptionContent).Find(selector);
        }

        /// <summary>在当前弹层内容里查一组元素（每次都是最新状态）。</summary>
        private IReadOnlyList<AngleSharp.Dom.IElement> PanelFindAll(string selector)
        {
            return Render(GetDropdown().OptionContent).FindAll(selector);
        }

        private DropDownOption GetDropdown()
        {
            return Services.GetRequiredService<PopupService>().SelectDropDownOptions.Single();
        }

        private static string CssSection()
        {
            var text = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "css", "index.css"));
            var marker = text.IndexOf("ElDateTimePicker：时间编辑器", StringComparison.Ordinal);

            Assert.True(marker >= 0, "index.css 中未找到 ElDateTimePicker 时间面板样式段");

            var start = text.LastIndexOf("/*", marker, StringComparison.Ordinal);

            // 只取本组件的段落：截到下一个「段分隔注释」为止，避免把后面追加的其它组件样式算进来。
            var next = text.IndexOf("/* =", marker + 10, StringComparison.Ordinal);

            return next > start ? text.Substring(start, next - start) : text.Substring(start);
        }

        private static string RepoRoot([CallerFilePath] string file = null)
        {
            return new FileInfo(file).Directory.Parent.Parent.FullName;
        }
    }
}
