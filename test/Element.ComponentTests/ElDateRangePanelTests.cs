using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 日期范围「双面板」与日历文案本地化（2026-09-22 新增能力）。
    /// <para>
    /// 断言的是 EP daterange 的两个关键点：
    /// ① 左右面板各自只画自己那一侧的箭头（« ‹ / › »），标签用合并的年月文案；
    /// ② 区间高亮用 in-range + start-date / end-date 表达，非范围面板不画。
    /// </para>
    /// </summary>
    public class ElDateRangePanelTests : BunitContext
    {
        public ElDateRangePanelTests()
        {
            Services.AddElementServices();
        }

        private static void AddRangePanel(ComponentParameterCollectionBuilder<ElDatePickerPanel> parameters,
            DatePickerRangeRole role, string locale = null)
        {
            parameters
                .Add(x => x.RangeRole, role)
                .Add(x => x.RangeStart, new DateTime(2026, 9, 12))
                .Add(x => x.RangeEnd, new DateTime(2026, 9, 25))
                .Add(x => x.Border, false)
                .Add(x => x.ValidateEvent, false)
                .Add(x => x.DefaultValue, new DateTime(2026, 9, 1));

            if (locale != null)
            {
                parameters.Add(x => x.Locale, locale);
            }
        }

        [Fact]
        public void StartPanel_RendersOnlyPrevArrows()
        {
            var cut = Render<ElDatePickerPanel>(p => AddRangePanel(p, DatePickerRangeRole.Start));

            Assert.Contains("el-date-range-header", cut.Markup);
            Assert.Contains("el-date-range-header__btns is-left", cut.Markup);
            Assert.Contains("el-date-range-header__label", cut.Markup);
            Assert.Contains("2026 年 9 月", cut.Markup);

            // 左面板没有「下一月 / 下一年」
            Assert.Empty(cut.FindAll(".el-icon-arrow-right"));
            Assert.Empty(cut.FindAll(".el-icon-d-arrow-right"));
            // 范围面板不出现清除按钮
            Assert.Empty(cut.FindAll(".el-date-picker__clear-btn"));
            // 单面板的「年 / 月」可点击块不该出现
            Assert.Empty(cut.FindAll(".el-date-picker__header-label"));
        }

        [Fact]
        public void EndPanel_RendersOnlyNextArrows()
        {
            var cut = Render<ElDatePickerPanel>(p => AddRangePanel(p, DatePickerRangeRole.End));

            Assert.Contains("el-date-range-header__btns is-right", cut.Markup);
            Assert.Equal(2, cut.FindAll(".el-date-range-header__btn").Count);
            Assert.Empty(cut.FindAll(".el-icon-arrow-left"));
            Assert.Empty(cut.FindAll(".el-icon-d-arrow-left"));
        }

        [Fact]
        public void RangePanel_HighlightsStartEndAndInRange()
        {
            var cut = Render<ElDatePickerPanel>(p => AddRangePanel(p, DatePickerRangeRole.Start));

            Assert.Equal("12", cut.Find("td.start-date span").TextContent.Trim());
            Assert.Equal("25", cut.Find("td.end-date span").TextContent.Trim());
            // 9/12 ~ 9/25 共 14 天
            Assert.Equal(14, cut.FindAll("td.in-range").Count);
        }

        [Fact]
        public void PlainPanel_DoesNotRenderRangeHighlight()
        {
            var cut = Render<ElDatePickerPanel>(p => p
                .Add(x => x.DefaultValue, new DateTime(2026, 9, 1))
                .Add(x => x.RangeStart, new DateTime(2026, 9, 12))
                .Add(x => x.RangeEnd, new DateTime(2026, 9, 25)));

            Assert.Empty(cut.FindAll(".el-date-range-header"));
            Assert.Empty(cut.FindAll("td.in-range"));
            Assert.Empty(cut.FindAll("td.start-date"));
            Assert.Contains("2026 年", cut.Markup);
            Assert.Contains("9 月", cut.Markup);
        }

        [Fact]
        public void LocaleEnglish_SwitchesWeekHeadersAndMonthLabel()
        {
            var cut = Render<ElDatePickerPanel>(p => AddRangePanel(p, DatePickerRangeRole.Start, "en-US"));

            Assert.Contains("2026 September", cut.Markup);
            var headers = cut.FindAll("th").Select(x => x.TextContent.Trim()).Take(7).ToArray();
            Assert.Equal(new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" }, headers);
        }

        [Fact]
        public void LocaleEmpty_FallsBackToSimplifiedChinese()
        {
            var cut = Render<ElDatePickerPanel>(p => AddRangePanel(p, DatePickerRangeRole.Start));

            Assert.Contains("2026 年 9 月", cut.Markup);
            var headers = cut.FindAll("th").Select(x => x.TextContent.Trim()).Take(7).ToArray();
            Assert.Equal(new[] { "日", "一", "二", "三", "四", "五", "六" }, headers);
        }

        [Fact]
        public void PickerLocale_FormatHelpers()
        {
            Assert.Equal("2026 年 9 月", PickerLocale.FormatYearMonth("zh-CN", 2026, 9));
            Assert.Equal("2026 September", PickerLocale.FormatYearMonth("en-US", 2026, 9));
            Assert.Equal("2026 年", PickerLocale.FormatYear(null, 2026));
            Assert.Equal("9 月", PickerLocale.FormatMonth(null, 9));
            Assert.Equal("January", PickerLocale.MonthName("en", 1));
            Assert.Equal("十二月", PickerLocale.MonthName("zh-cn", 12));
            // 越界月份夹取到 1 / 12，不抛
            Assert.Equal("January", PickerLocale.MonthName("en", 0));
            Assert.Equal("December", PickerLocale.MonthName("en", 99));
            // 未识别语言回退中文
            Assert.Equal("2026 年", PickerLocale.FormatYear("fr-FR", 2026));
        }

        [Fact]
        public void RangeRoleEnum_CoversThreeStates()
        {
            Assert.Equal(0, (int)DatePickerRangeRole.None);
            Assert.Equal(1, (int)DatePickerRangeRole.Start);
            Assert.Equal(2, (int)DatePickerRangeRole.End);
        }
    }
}
