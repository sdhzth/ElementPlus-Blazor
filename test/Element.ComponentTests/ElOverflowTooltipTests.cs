using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElOverflowTooltip 的浮层定位单元测试。坐标来自悬停时 JS 量到的视口矩形，所以 bUnit 能把读数喂全，
    /// 这里钉的是「坐标算得对不对、有没有真的覆盖掉 ElTooltip 默认的 absolute」。
    /// 「浮层被 .el-table__body-wrapper 这类滚动容器裁掉」本身只能浏览器实测，见
    /// demo/tools/probes/verify-ellipsis-tooltip.cjs（docs/testing-guards.md §4 第 11 条：bUnit 没有布局引擎）。
    /// </summary>
    public class ElOverflowTooltipTests : BunitContext
    {
        public ElOverflowTooltipTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddElementServices();
        }

        private IRenderedComponent<ElOverflowTooltip> RenderClipped(string placement,
            double rectTop, double rectLeft, double rectWidth, double rectHeight)
        {
            JSInterop.Setup<TextClipState>(TextClip.MeasureFunction, _ => true)
                .SetResult(new TextClipState
                {
                    ScrollWidth = 640,
                    ClientWidth = 180,
                    ScrollHeight = 24,
                    ClientHeight = 24,
                    Text = "完整文字",
                    RectTop = rectTop,
                    RectLeft = rectLeft,
                    RectWidth = rectWidth,
                    RectHeight = rectHeight
                });

            var cut = Render<ElOverflowTooltip>(p => p
                .Add(x => x.Enabled, true)
                .Add(x => x.Tag, "div")
                .Add(x => x.Class, "clip-box")
                .Add(x => x.Placement, placement)
                .AddChildContent("完整文字"));

            cut.Find(".clip-box").TriggerEvent("onmouseenter", new MouseEventArgs());
            cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".el-tooltip__popper")), timeout: TestWait.Timeout);
            return cut;
        }

        private static string PopperStyle(IRenderedComponent<ElOverflowTooltip> cut)
        {
            return cut.Find(".el-tooltip__popper").GetAttribute("style");
        }

        [Fact]
        public void ClippedPopperIsPinnedFixedToTheMeasuredRect()
        {
            var cut = RenderClipped("top", rectTop: 300, rectLeft: 100, rectWidth: 180, rectHeight: 24);
            var style = PopperStyle(cut);

            // fixed 必须排在 ElTooltip 默认那条 absolute 之后才算把它覆盖掉（同名属性后声明的赢）。
            Assert.True(style.LastIndexOf("position:fixed", StringComparison.Ordinal)
                        > style.IndexOf("position:absolute", StringComparison.Ordinal), style);
            Assert.Contains("left:190px", style);
            Assert.Contains("top:292px", style);
            Assert.Contains("transform:translate(-50%, -100%)", style);
            // ElTooltip 的 top 规则自带 bottom:100%、left:50%，不显式让位就会和这里的 left/top 打架
            Assert.Contains("bottom:auto", style);
            Assert.Contains("right:auto", style);
            Assert.Equal("top", cut.Find(".el-tooltip__popper").GetAttribute("x-placement"));
        }

        [Fact]
        public void ClippedPopperFlipsBelowWhenThereIsNoRoomAbove()
        {
            // 上方只剩 20px（阈值 40px）：整块浮层会跑到视口外，用户看到的还是被裁的那截，所以必须翻到下方。
            var cut = RenderClipped("top", rectTop: 20, rectLeft: 100, rectWidth: 180, rectHeight: 24);
            var style = PopperStyle(cut);

            Assert.Contains("top:52px", style);
            Assert.Contains("transform:translate(-50%, 0)", style);
            Assert.Equal("bottom", cut.Find(".el-tooltip__popper").GetAttribute("x-placement"));
        }

        [Fact]
        public void ClippedPopperHonoursRightPlacement()
        {
            // 左/右两支：垂直居中、贴着裁剪盒的外侧 8px。Placement 是公开参数，不能只认上下。
            var cut = RenderClipped("right", rectTop: 300, rectLeft: 100, rectWidth: 180, rectHeight: 24);
            var style = PopperStyle(cut);

            Assert.Contains("left:288px", style);
            Assert.Contains("top:312px", style);
            Assert.Contains("transform:translate(0, -50%)", style);
            Assert.Equal("right", cut.Find(".el-tooltip__popper").GetAttribute("x-placement"));
        }

        [Fact]
        public void NotClippedPinsNothing()
        {
            // 反空转对照：没被裁时既没有浮层，也不许留下上一次悬停算出来的坐标。
            JSInterop.Setup<TextClipState>(TextClip.MeasureFunction, _ => true)
                .SetResult(new TextClipState { ScrollWidth = 180, ClientWidth = 180, Text = "放得下的文字", RectTop = 300, RectLeft = 100, RectWidth = 180, RectHeight = 24 });
            var cut = Render<ElOverflowTooltip>(p => p
                .Add(x => x.Enabled, true)
                .Add(x => x.Tag, "div")
                .Add(x => x.Class, "clip-box")
                .AddChildContent("放得下的文字"));

            cut.Find(".clip-box").TriggerEvent("onmouseenter", new MouseEventArgs());

            cut.WaitForAssertion(() => Assert.Contains(TextClip.MeasureFunction,
                JSInterop.Invocations.Select(x => x.Identifier)), timeout: TestWait.Timeout);
            Assert.Empty(cut.FindAll(".el-tooltip__popper"));
        }
    }
}
