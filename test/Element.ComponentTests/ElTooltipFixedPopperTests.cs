using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// <c>ElTooltip.FixedPopper</c>（批次㊶）：浮层钉成 position:fixed + 悬停时实测的视口坐标。
    /// 加这一条是因为浮层渲染在触发 span <b>内部</b>，absolute 会被「最近的非 visible overflow 祖先」裁掉
    /// —— 表格单元格（<c>.cell</c> hidden → <c>.el-table__body-wrapper</c> auto → <c>.el-table</c> hidden）
    /// 里的行内图标按钮 hover 什么都不出来。
    /// 「真的被裁掉了没有」只能浏览器实测（bUnit 没有布局引擎，docs/testing-guards.md §4 第 11 条）；
    /// 这里钉的是坐标算得对不对、有没有覆盖掉默认的 absolute、以及**默认关着时行为一字不变**。
    /// </summary>
    public class ElTooltipFixedPopperTests : BunitContext
    {
        public ElTooltipFixedPopperTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddElementServices();
        }

        /// <summary>喂给 JS 的触发元素矩形。48×32 是行内小圆钮的量级，中心点 (224, 516)。</summary>
        private void SetupRect(double top, double left, double width, double height)
        {
            JSInterop.Setup<TextClipState>(TextClip.MeasureFunction, _ => true)
                .SetResult(new TextClipState
                {
                    ScrollWidth = 48,
                    ClientWidth = 48,
                    ScrollHeight = 32,
                    ClientHeight = 32,
                    Text = "新增同级",
                    RectTop = top,
                    RectLeft = left,
                    RectWidth = width,
                    RectHeight = height,
                });
        }

        private IRenderedComponent<ElTooltip> Hover(string placement, bool fixedPopper, int offset = 8)
        {
            var cut = Render<ElTooltip>(p => p
                .Add(x => x.Content, "新增同级")
                .Add(x => x.Placement, placement)
                .Add(x => x.FixedPopper, fixedPopper)
                .Add(x => x.Offset, offset)
                .AddChildContent("<button>+</button>"));
            cut.Find(".el-tooltip__trigger").MouseEnter();
            cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".el-tooltip__popper")), timeout: TestWait.Timeout);
            return cut;
        }

        private static string Style(IRenderedComponent<ElTooltip> cut) =>
            cut.Find(".el-tooltip__popper").GetAttribute("style");

        [Fact]
        public void FixedPopper_Top_AnchorsToMeasuredViewportRect()
        {
            SetupRect(top: 500, left: 200, width: 48, height: 32);
            var cut = Hover("top", true);
            var style = Style(cut);

            Assert.Contains("position:fixed", style);
            Assert.DoesNotContain("position:absolute", style);
            // top 档：水平居中 = 200 + 48/2 = 224；上缘 = 500 - 8（Offset 即间距）
            Assert.Contains("left:224px", style);
            Assert.Contains("top:492px", style);
            Assert.Contains("transform:translate(-50%, -100%)", style);
            // absolute 那套相对锚点必须让位，否则 left/top 与 bottom:100% 同时生效会顶飞
            Assert.DoesNotContain("bottom:100%", style);
            Assert.Equal("top", cut.Find(".el-tooltip__popper").GetAttribute("x-placement"));
        }

        [Fact]
        public void FixedPopper_FlipsBelowWhenNoRoomAbove()
        {
            SetupRect(top: 10, left: 200, width: 48, height: 32);
            var cut = Hover("top", true);
            var style = Style(cut);

            // 上方只 10px，放不下 ≈31px 高的单行浮层 ⇒ 翻到下方，x-placement 跟着翻（箭头才不指反）
            Assert.Equal("bottom", cut.Find(".el-tooltip__popper").GetAttribute("x-placement"));
            Assert.Contains("top:50px", style);
            Assert.Contains("transform:translate(-50%, 0)", style);
        }

        [Fact]
        public void FixedPopper_UsesOffsetAsGap()
        {
            SetupRect(top: 500, left: 200, width: 48, height: 32);
            Assert.Contains("top:480px", Style(Hover("top", true, offset: 20)));
        }

        [Fact]
        public void FixedPopper_Right_PlacementAnchorsBesideTrigger()
        {
            SetupRect(top: 500, left: 200, width: 48, height: 32);
            var style = Style(Hover("right", true));
            Assert.Contains("left:256px", style);
            Assert.Contains("top:516px", style);
            Assert.Contains("transform:translate(0, -50%)", style);
        }

        /// <summary>正向对照：不开 fixed 时仍是 absolute + 相对锚点。这条证明上面几条不是把判据放宽到恒过。</summary>
        [Fact]
        public void FixedPopperOff_KeepsAbsoluteAnchors()
        {
            SetupRect(top: 500, left: 200, width: 48, height: 32);
            var style = Style(Hover("top", false));

            Assert.Contains("position:absolute", style);
            Assert.DoesNotContain("position:fixed", style);
            Assert.Contains("bottom:100%", style);
            Assert.Contains("transform:translate(-50%, -8px)", style);
        }

        /// <summary>量不到矩形（bUnit 未打桩 / 元素已移除）时静默退回 absolute，不能让一次悬停炸掉渲染。</summary>
        [Fact]
        public void FixedPopper_WithoutJsReading_FallsBackToAbsolute()
        {
            var cut = Render<ElTooltip>(p => p
                .Add(x => x.Content, "新增同级")
                .Add(x => x.FixedPopper, true)
                .AddChildContent("<button>+</button>"));
            cut.Find(".el-tooltip__trigger").MouseEnter();
            cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".el-tooltip__popper")), timeout: TestWait.Timeout);

            Assert.Contains("position:absolute", Style(cut));
        }
    }
}
