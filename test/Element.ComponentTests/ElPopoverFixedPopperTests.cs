using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// <c>ElPopover.FixedPopper</c>（批次㊹）：面板钉成 position:fixed + 打开时实测的视口坐标。
    /// 根因与 <see cref="ElTooltipFixedPopperTests"/>（批次㊶）同一条：面板内联在
    /// <c>.el-popover__reference-wrapper</c> 里，absolute 被「最近的非 visible overflow 祖先」裁掉。
    /// 浏览器实测的场景是对话框页签里的「操作说明」：面板矩形 936,108..1282,260，
    /// 裁剪层 <c>.el-tabs__content</c>(overflow:hidden) 上缘 253 ⇒ 152px 高只剩 7px。
    /// 「到底裁没裁」只有浏览器能判（bUnit 没有布局引擎）；这里钉的是坐标算得对不对、
    /// fixed 分支有没有保住 <c>width</c>/<c>z-index</c>、以及**默认关着时行为一字不变**。
    /// </summary>
    public class ElPopoverFixedPopperTests : BunitContext
    {
        public ElPopoverFixedPopperTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddElementServices();
        }

        /// <summary>喂给 JS 的参考元素矩形。40×32 是「?」小方钮的量级。</summary>
        private void SetupRect(double top, double left, double width, double height)
        {
            JSInterop.Setup<TextClipState>(TextClip.MeasureFunction, _ => true)
                .SetResult(new TextClipState
                {
                    ScrollWidth = (int)width,
                    ClientWidth = (int)width,
                    ScrollHeight = (int)height,
                    ClientHeight = (int)height,
                    Text = "?",
                    RectTop = top,
                    RectLeft = left,
                    RectWidth = width,
                    RectHeight = height,
                });
        }

        private IRenderedComponent<ElPopover> Open(string placement, bool fixedPopper, int offset = 8, int width = 320)
        {
            var cut = Render<ElPopover>(p => p
                .Add(x => x.Title, "操作说明")
                .Add(x => x.Content, "用户权限等级A的可以看等级B的用户增加的数据。")
                .Add(x => x.Width, width)
                .Add(x => x.Placement, placement)
                .Add(x => x.FixedPopper, fixedPopper)
                .Add(x => x.Offset, offset)
                .Add(x => x.Trigger, PopperTrigger.Click)
                .AddChildContent("<button>?</button>"));
            cut.Find(".el-popover__reference").Click();
            cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".el-popover")), timeout: TestWait.Timeout);
            return cut;
        }

        private static string Style(IRenderedComponent<ElPopover> cut) =>
            cut.Find(".el-popover").GetAttribute("style");

        [Fact]
        public void FixedPopper_Top_AnchorsToMeasuredViewportRect()
        {
            // 参考元素：273,1096 起，40×32 ⇒ 中心 x=1116，面板贴在它上方 8px
            SetupRect(top: 273, left: 1096, width: 40, height: 32);
            var cut = Open("top", true);
            var style = Style(cut);

            Assert.Contains("position:fixed", style);
            Assert.DoesNotContain("position:absolute", style);
            Assert.Contains("left:1116px", style);
            Assert.Contains("top:265px", style);
            Assert.Contains("transform:translate(-50%, -100%)", style);
            // absolute 那套相对锚点必须让位，否则 left/top 与 bottom:100% 同时生效会顶飞
            Assert.DoesNotContain("bottom:100%", style);
            Assert.Equal("top", cut.Find(".el-popover").GetAttribute("x-placement"));
        }

        [Fact]
        public void FixedPopper_FlipsBelowWhenNoRoomAbove()
        {
            SetupRect(top: 10, left: 200, width: 40, height: 32);
            var cut = Open("top", true);

            // 上方只 10px ⇒ 翻到下方（10+32+8=50），x-placement 跟着翻，箭头才不指反
            Assert.Equal("bottom", cut.Find(".el-popover").GetAttribute("x-placement"));
            Assert.Contains("top:50px", Style(cut));
            Assert.Contains("transform:translate(-50%, 0)", Style(cut));
        }

        [Fact]
        public void FixedPopper_UsesOffsetAsGap()
        {
            SetupRect(top: 273, left: 1096, width: 40, height: 32);
            Assert.Contains("top:253px", Style(Open("top", true, offset: 20)));
        }

        /// <summary>fixed 分支不能把 absolute 分支自带的 <c>width</c> 与 <c>z-index</c> 弄丢。</summary>
        [Fact]
        public void FixedPopper_KeepsWidthAndZIndex()
        {
            SetupRect(top: 273, left: 1096, width: 40, height: 32);
            var style = Style(Open("top", true, width: 320));

            Assert.Contains("width:320px", style);
            Assert.Contains("z-index:var(--el-index-popper, 2000)", style);
        }

        /// <summary>正向对照：不开 fixed 时仍是 absolute + 相对锚点。这条证明上面几条不是把判据放宽到恒过。</summary>
        [Fact]
        public void FixedPopperOff_KeepsAbsoluteAnchors()
        {
            SetupRect(top: 273, left: 1096, width: 40, height: 32);
            var style = Style(Open("top", false));

            Assert.Contains("position:absolute", style);
            Assert.DoesNotContain("position:fixed", style);
            Assert.Contains("bottom:100%", style);
            Assert.Contains("left:50%", style);
            Assert.Contains("transform:translate(-50%, -8px)", style);
        }

        /// <summary>量不到矩形（bUnit 未打桩 / 元素已移除）时静默退回 absolute，不能让一次点击炸掉渲染。</summary>
        [Fact]
        public void FixedPopper_WithoutJsReading_FallsBackToAbsolute()
        {
            var cut = Render<ElPopover>(p => p
                .Add(x => x.Title, "操作说明")
                .Add(x => x.Content, "说明文字")
                .Add(x => x.FixedPopper, true)
                .Add(x => x.Trigger, PopperTrigger.Click)
                .AddChildContent("<button>?</button>"));
            cut.Find(".el-popover__reference").Click();
            cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".el-popover")), timeout: TestWait.Timeout);

            Assert.Contains("position:absolute", Style(cut));
        }

        /// <summary>关闭时把 fixed 读数清掉：再开一次要重新量，不能拿上一次的坐标。</summary>
        [Fact]
        public void FixedPopper_ClearsStyleOnHide()
        {
            SetupRect(top: 273, left: 1096, width: 40, height: 32);
            var cut = Open("top", true);
            Assert.Contains("position:fixed", Style(cut));

            cut.Find(".el-popover__reference").Click();
            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".el-popover")), timeout: TestWait.Timeout);

            SetupRect(top: 600, left: 300, width: 40, height: 32);
            cut.Find(".el-popover__reference").Click();
            cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".el-popover")), timeout: TestWait.Timeout);
            Assert.Contains("top:592px", Style(cut));
            Assert.DoesNotContain("top:265px", Style(cut));
        }
    }
}
