using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElProgressTests : BunitContext
    {
        public ElProgressTests()
        {
            Services.AddElementServices();
        }

        [Fact]
        public void LineBasicRendersInnerWidth()
        {
            var cut = Render<ElProgress>(p => p.Add(x => x.Percentage, 50));
            var inner = cut.Find(".el-progress-bar__inner");
            Assert.Contains("width:50%", (inner.GetAttribute("style") ?? "").Replace(" ", ""));
        }

        [Fact]
        public void DefaultTypeIsLine()
        {
            var cut = Render<ElProgress>(p => p.Add(x => x.Percentage, 30));
            Assert.Contains("el-progress--line", cut.Find("div.el-progress").ClassName);
        }

        [Fact]
        public void CircleRendersSvgAndClass()
        {
            var cut = Render<ElProgress>(p => p
                .Add(x => x.Type, ProgressType.Circle)
                .Add(x => x.Percentage, 40));
            Assert.Contains("el-progress--circle", cut.Find("div.el-progress").ClassName);
            Assert.NotNull(cut.Find(".el-progress-circle svg"));
            Assert.NotNull(cut.Find("path.el-progress-circle__path"));
        }

        [Fact]
        public void DashboardRendersClass()
        {
            var cut = Render<ElProgress>(p => p
                .Add(x => x.Type, ProgressType.Dashboard)
                .Add(x => x.Percentage, 40));
            Assert.Contains("el-progress--dashboard", cut.Find("div.el-progress").ClassName);
        }

        [Fact]
        public void StatusAddsModifierClass()
        {
            var cut = Render<ElProgress>(p => p
                .Add(x => x.Percentage, 100)
                .Add(x => x.Status, ProgressStatus.Success));
            Assert.Contains("is-success", cut.Find("div.el-progress").ClassName);
        }

        [Fact]
        public void TextInsideAddsModifierClass()
        {
            var cut = Render<ElProgress>(p => p
                .Add(x => x.Percentage, 70)
                .Add(x => x.TextInside, true));
            Assert.Contains("el-progress--text-inside", cut.Find("div.el-progress").ClassName);
            Assert.NotNull(cut.Find(".el-progress-bar__innerText"));
        }

        [Fact]
        public void ShowTextFalseHidesTextAndAddsClass()
        {
            var cut = Render<ElProgress>(p => p
                .Add(x => x.Percentage, 50)
                .Add(x => x.ShowText, false));
            Assert.Contains("el-progress--without-text", cut.Find("div.el-progress").ClassName);
            Assert.Empty(cut.FindAll(".el-progress__text"));
        }

        [Fact]
        public void StripedAddsIsStriped()
        {
            var cut = Render<ElProgress>(p => p
                .Add(x => x.Percentage, 60)
                .Add(x => x.Striped, true));
            Assert.Contains("is-striped", cut.Find(".el-progress-bar__inner").ClassName);
        }

        [Fact]
        public void StripedFlowAddsIsStripedFlow()
        {
            var cut = Render<ElProgress>(p => p
                .Add(x => x.Percentage, 60)
                .Add(x => x.Striped, true)
                .Add(x => x.StripedFlow, true));
            var inner = cut.Find(".el-progress-bar__inner");
            Assert.Contains("is-striped", inner.ClassName);
            Assert.Contains("is-striped-flow", inner.ClassName);
        }

        [Fact]
        public void IndeterminateAddsIsIndeterminate()
        {
            var cut = Render<ElProgress>(p => p
                .Add(x => x.Percentage, 50)
                .Add(x => x.Indeterminate, true));
            Assert.Contains("is-indeterminate", cut.Find(".el-progress-bar__inner").ClassName);
        }

        [Fact]
        public void ColorStringAppliedToInner()
        {
            var cut = Render<ElProgress>(p => p
                .Add(x => x.Percentage, 50)
                .Add(x => x.Color, "#ff0000"));
            Assert.Contains("background-color:#ff0000", (cut.Find(".el-progress-bar__inner").GetAttribute("style") ?? "").Replace(" ", ""));
        }

        [Fact]
        public void ColorStopsProducesGradient()
        {
            var cut = Render<ElProgress>(p => p
                .Add(x => x.Percentage, 60)
                .Add(x => x.ColorStops, new[]
                {
                    new ProgressColorStop { Color = "#e6a23c", Percentage = 0 },
                    new ProgressColorStop { Color = "#f56c6c", Percentage = 100 }
                }));
            Assert.Contains("linear-gradient", (cut.Find(".el-progress-bar__inner").GetAttribute("style") ?? "").Replace(" ", ""));
        }

        [Fact]
        public void ColorStopsPicksColorAtPercentageForCircle()
        {
            var cut = Render<ElProgress>(p => p
                .Add(x => x.Type, ProgressType.Circle)
                .Add(x => x.Percentage, 80)
                .Add(x => x.ColorStops, new[]
                {
                    new ProgressColorStop { Color = "#e6a23c", Percentage = 0 },
                    new ProgressColorStop { Color = "#f56c6c", Percentage = 100 }
                }));
            // 80% 介于 0~100 之间，应取后一停靠点 #f56c6c
            Assert.Equal("#f56c6c", cut.Find("path.el-progress-circle__path").GetAttribute("stroke"));
        }

        [Fact]
        public void FormatFuncOverridesText()
        {
            var cut = Render<ElProgress>(p => p
                .Add(x => x.Percentage, 60)
                .Add(x => x.FormatFunc, (System.Func<double, string>)(p => $"{p}/100")));
            Assert.Equal("60/100", cut.Find(".el-progress__text").TextContent.Trim());
        }

        [Fact]
        public void ContentSlotRendered()
        {
            var cut = Render<ElProgress>(p => p
                .Add(x => x.Type, ProgressType.Circle)
                .Add(x => x.Percentage, 80)
                .Add(x => x.Content, (RenderFragment<double>)(pct => b => b.AddContent(0, $"自定义{pct}"))));
            Assert.Contains("自定义80", cut.Find(".el-progress__text").TextContent);
        }

        [Fact]
        public void PercentageClampedToRange()
        {
            var over = Render<ElProgress>(p => p.Add(x => x.Percentage, 150));
            Assert.Contains("width:100%", (over.Find(".el-progress-bar__inner").GetAttribute("style") ?? "").Replace(" ", ""));

            var under = Render<ElProgress>(p => p.Add(x => x.Percentage, -10));
            Assert.Contains("width:0%", (under.Find(".el-progress-bar__inner").GetAttribute("style") ?? "").Replace(" ", ""));
        }
    }
}
