using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElStatisticTests : BunitContext
    {
        public ElStatisticTests()
        {
            Services.AddElementPlusServices();
        }

        [Fact]
        public void DecimalSeparatorReplacesDot()
        {
            var cut = Render<ElStatistic>(p => p
                .Add(x => x.Value, 1234.5)
                .Add(x => x.Precision, 1)
                .Add(x => x.GroupSeparator, false)
                .Add(x => x.DecimalSeparator, ","));

            Assert.Contains("1234,5", cut.Find(".el-statistic__number").TextContent);
        }

        [Fact]
        public void GroupSeparatorUsesComma()
        {
            var cut = Render<ElStatistic>(p => p
                .Add(x => x.Value, 1234567)
                .Add(x => x.Precision, 0));

            Assert.Contains("1,234,567", cut.Find(".el-statistic__number").TextContent);
        }

        [Fact]
        public void DisplayValueTextExposed()
        {
            var cut = Render<ElStatistic>(p => p
                .Add(x => x.Value, 9.25)
                .Add(x => x.Precision, 2));

            Assert.Equal("9.25", cut.Instance.DisplayValueText);
        }

        [Fact]
        public void FormatterOverridesDefaultFormatting()
        {
            var cut = Render<ElStatistic>(p => p
                .Add(x => x.Value, 0.5)
                .Add(x => x.Formatter, v => "pct:" + (v * 100).ToString("0")));

            Assert.Contains("pct:50", cut.Find(".el-statistic__number").TextContent);
        }

        // ── 批次㉕ #129：displayValue 改按 EP 2.14.0 的算法（实抓
        //    packages/components/statistic/src/statistic.vue 的 displayValue computed）──

        [Fact]
        public void PrecisionTruncatesInsteadOfRounding()
        {
            // EP 是 padEnd 补零后 slice 直接砍：12345.6789 取 2 位得 12,345.67（旧写法 N2 会四舍五入成 .68）。
            var cut = Render<ElStatistic>(p => p
                .Add(x => x.Value, 12345.6789)
                .Add(x => x.Precision, 2));

            Assert.Equal("12,345.67", cut.Find(".el-statistic__number").TextContent.Trim());
        }

        [Fact]
        public void PrecisionZeroDropsTheWholeFraction()
        {
            // 同一条 slice：小数段被砍成空串时连小数点都不补（旧写法 F0 会把 1234.9 报成 1,235）。
            var cut = Render<ElStatistic>(p => p
                .Add(x => x.Value, 1234.9)
                .Add(x => x.Precision, 0));

            Assert.Equal("1,234", cut.Find(".el-statistic__number").TextContent.Trim());
        }

        [Fact]
        public void PrecisionPadsMissingDecimalsWithZero()
        {
            var cut = Render<ElStatistic>(p => p
                .Add(x => x.Value, 9.2)
                .Add(x => x.Precision, 4));

            Assert.Equal("9.2000", cut.Find(".el-statistic__number").TextContent.Trim());
        }

        [Fact]
        public void DecimalSeparatorDoesNotTouchGroupSeparator()
        {
            // 欧式写法：千分位 "." + 小数点 ","。EP 把两者分别只作用在整数段和两段接缝上，
            // 旧写法先 N2 再对整串 Replace，会把千分位一起换成逗号。
            var cut = Render<ElStatistic>(p => p
                .Add(x => x.Value, 1234567.891)
                .Add(x => x.Precision, 2)
                .Add(x => x.ThousandsSeparator, ".")
                .Add(x => x.DecimalSeparator, ","));

            Assert.Equal("1.234.567,89", cut.Find(".el-statistic__number").TextContent.Trim());
        }

        [Fact]
        public void GroupSeparatorFalseLeavesPlainIntegerPart()
        {
            var cut = Render<ElStatistic>(p => p
                .Add(x => x.Value, 1234567)
                .Add(x => x.GroupSeparator, false));

            Assert.Equal("1234567", cut.Find(".el-statistic__number").TextContent.Trim());
        }

        [Fact]
        public void NegativeValueKeepsSignBeforeGrouping()
        {
            // 与 EP 同一条正则的 \B：负号与首个数字之间是单词边界，不会插千分位符。
            var cut = Render<ElStatistic>(p => p
                .Add(x => x.Value, -4580.5)
                .Add(x => x.Precision, 1));

            Assert.Equal("-4,580.5", cut.Find(".el-statistic__number").TextContent.Trim());
        }

        [Fact]
        public void ValueStyleLandsOnNumberSpan()
        {
            var cut = Render<ElStatistic>(p => p
                .Add(x => x.Value, 72000)
                .Add(x => x.ValueStyle, "color: var(--el-color-primary, #409eff); font-weight: 700;"));

            Assert.Equal("color: var(--el-color-primary, #409eff); font-weight: 700;",
                cut.Find(".el-statistic__number").GetAttribute("style"));
        }

        // ── 批次㉕ #128：ElCountdown 的 format 按 EP 的 formatTime
        //    （实抓 packages/components/countdown/src/utils.ts）──

        [Fact]
        public void CountdownStripsBracketLiterals()
        {
            // 官方示例用的就是 "DD [days] HH:mm:ss"：方括号里是字面量，不参与替换，最后整对剥掉。
            var cut = Render<ElCountdown>(p => p
                .Add(x => x.Value, DateTime.Now.Add(new TimeSpan(2, 5, 5, 0)))
                .Add(x => x.Format, "DD [天] HH:mm:ss"));

            var text = cut.Find(".el-statistic__number").TextContent.Trim();
            Assert.DoesNotContain("[", text);
            Assert.DoesNotContain("]", text);
            Assert.StartsWith("02 天 05:", text);
        }

        [Fact]
        public void CountdownHoursAreRemainderWhenDaysArePresent()
        {
            // 逐级吃余数：2 天 5 小时在这里是「02 天 + 05 时」，而不是 EP 之外那种总小时 53。
            var cut = Render<ElCountdown>(p => p
                .Add(x => x.Value, DateTime.Now.Add(new TimeSpan(2, 5, 5, 0)))
                .Add(x => x.Format, "DD 天 HH:mm:ss"));

            Assert.StartsWith("02 天 05:", cut.Find(".el-statistic__number").TextContent.Trim());
        }

        [Fact]
        public void CountdownHoursAreTotalWhenNoDayToken()
        {
            // 官方口径的另一半：格式里没有 DD 时 HH 是总小时数（这里 50h − 一帧误差 → 49）。
            var cut = Render<ElCountdown>(p => p
                .Add(x => x.Value, DateTime.Now.AddHours(50))
                .Add(x => x.Format, "HH:mm:ss"));

            Assert.StartsWith("49:", cut.Find(".el-statistic__number").TextContent.Trim());
        }

        [Fact]
        public void CountdownPadsByTokenLength()
        {
            // 补零长度跟 token 连续长度走（EP: padStart(match.length,'0')）："H:mm" 的单位 H 不补零。
            var cut = Render<ElCountdown>(p => p
                .Add(x => x.Value, DateTime.Now.AddMinutes(5))
                .Add(x => x.Format, "H:mm"));

            Assert.Equal("0:04", cut.Find(".el-statistic__number").TextContent.Trim());
        }

        [Fact]
        public void CountdownValueStyleLandsOnNumberSpan()
        {
            var cut = Render<ElCountdown>(p => p
                .Add(x => x.Value, DateTime.Now.AddHours(1))
                .Add(x => x.ValueStyle, "color: red"));

            Assert.Equal("color: red", cut.Find(".el-statistic__number").GetAttribute("style"));
        }
    }
}
