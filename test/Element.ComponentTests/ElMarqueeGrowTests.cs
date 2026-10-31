using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElMarqueeGrow 组件测试（仅覆盖本组件）。
    /// 覆盖：无缝滚动的内容偏移量、内容份数与无障碍标记、四个方向的动画名映射、
    /// Round 预设与 RoundRadius 覆盖、边框开关、悬停暂停开关、Text 与 ChildContent 的优先级。
    /// </summary>
    public class ElMarqueeGrowTests : BunitContext
    {
        public ElMarqueeGrowTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static int CopyCount(IRenderedComponent<ElMarqueeGrow> cut)
            => cut.FindAll(".el-marquee-grow__copy").Count;

        private static int HiddenCopyCount(IRenderedComponent<ElMarqueeGrow> cut)
            => cut.FindAll(".el-marquee-grow__copy").Count(e => e.HasAttribute("aria-hidden"));

        private static string ItemStyle(IRenderedComponent<ElMarqueeGrow> cut)
            => cut.Find(".el-marquee-grow__item").GetAttribute("style");

        [Fact]
        public void Default_UsesHorizontalRightToLeftWithSingleCopy()
        {
            var cut = Render<ElMarqueeGrow>(p => p.Add(x => x.Text, "门诊通知"));

            Assert.Equal(1, CopyCount(cut));
            Assert.Contains("el-marquee-grow--horizontal", cut.Markup);
            Assert.Contains("el-marquee-grow-rtl", cut.Markup);
            Assert.Contains("门诊通知", cut.Markup);
        }

        [Fact]
        public void HorizontalOffset_IsOneFullContainerWidth()
        {
            var cut = Render<ElMarqueeGrow>(p => p
                .Add(x => x.Text, "A")
                .Add(x => x.Width, "100%"));

            // 横向：padding-left 百分比按容器宽度解析，恰好等于容器宽度
            Assert.Contains("padding-left:100%", ItemStyle(cut));
            Assert.DoesNotContain("padding-top", ItemStyle(cut));
        }

        [Fact]
        public void VerticalOffset_UsesContainerHeightInPixels()
        {
            var cut = Render<ElMarqueeGrow>(p => p
                .Add(x => x.Text, "A")
                .Add(x => x.Direction, ElMarqueeDirection.BottomToTop)
                .Add(x => x.Height, "130px"));

            // 纵向：百分比 padding 会按容器宽度解析，因此必须用容器高度的 px 值
            Assert.Contains("padding-top:130px", ItemStyle(cut));
            Assert.DoesNotContain("padding-left", ItemStyle(cut));
        }

        [Fact]
        public void VerticalOffset_DeductsBordersUnderBorderBoxSizing()
        {
            var cut = Render<ElMarqueeGrow>(p => p
                .Add(x => x.Text, "A")
                .Add(x => x.Direction, ElMarqueeDirection.BottomToTop)
                .Add(x => x.Height, "130px")
                .Add(x => x.Bordered, true)
                .Add(x => x.BorderWidth, 2));

            // 可见内容高度 = 130 - 2*2 = 126，偏移量必须与之严格相等才无缝
            Assert.Contains("padding-top:126px", ItemStyle(cut));
        }

        [Fact]
        public void VerticalOffset_FallsBackToPercentageWhenHeightIsNotPixels()
        {
            var cut = Render<ElMarqueeGrow>(p => p
                .Add(x => x.Text, "A")
                .Add(x => x.Direction, ElMarqueeDirection.TopToBottom)
                .Add(x => x.Height, "40vh"));

            Assert.Contains("padding-top:100%", ItemStyle(cut));
        }

        [Fact]
        public void Repeat_RendersThatManyCopiesAndClampsAtOne()
        {
            var many = Render<ElMarqueeGrow>(p => p
                .Add(x => x.Text, "A")
                .Add(x => x.Repeat, 4));
            Assert.Equal(4, CopyCount(many));
            // 首份内容保留给读屏软件，其余 3 份标记 aria-hidden
            Assert.Equal(3, HiddenCopyCount(many));

            var clamped = Render<ElMarqueeGrow>(p => p
                .Add(x => x.Text, "A")
                .Add(x => x.Repeat, 0));
            Assert.Equal(1, CopyCount(clamped));
        }

        [Fact]
        public void Spacing_AppliesBetweenCopiesOnly()
        {
            var cut = Render<ElMarqueeGrow>(p => p
                .Add(x => x.Text, "A")
                .Add(x => x.Repeat, 3)
                .Add(x => x.Spacing, 20));

            var styles = cut.FindAll(".el-marquee-grow__copy").Select(e => e.GetAttribute("style")).ToList();
            Assert.Contains("padding-right:20px", styles[0]);
            Assert.Contains("padding-right:20px", styles[1]);
            Assert.DoesNotContain("padding-right", styles[2] ?? string.Empty);
        }

        [Theory]
        [InlineData(ElMarqueeDirection.RightToLeft, "el-marquee-grow-rtl", "el-marquee-grow--horizontal")]
        [InlineData(ElMarqueeDirection.LeftToRight, "el-marquee-grow-ltr", "el-marquee-grow--horizontal")]
        [InlineData(ElMarqueeDirection.BottomToTop, "el-marquee-grow-btt", "el-marquee-grow--vertical")]
        [InlineData(ElMarqueeDirection.TopToBottom, "el-marquee-grow-ttb", "el-marquee-grow--vertical")]
        public void Direction_MapsToAnimationNameAndAxisClass(ElMarqueeDirection direction, string animation, string axisClass)
        {
            var cut = Render<ElMarqueeGrow>(p => p
                .Add(x => x.Text, "A")
                .Add(x => x.Direction, direction));

            Assert.Contains(animation, cut.Markup);
            Assert.Contains(axisClass, cut.Markup);
        }

        [Theory]
        [InlineData(ElMarqueeRound.None, "border-radius:0px")]
        [InlineData(ElMarqueeRound.Small, "border-radius:4px")]
        [InlineData(ElMarqueeRound.Large, "border-radius:8px")]
        [InlineData(ElMarqueeRound.Pill, "border-radius:999px")]
        public void Round_MapsToBorderRadius(ElMarqueeRound round, string expected)
        {
            var cut = Render<ElMarqueeGrow>(p => p
                .Add(x => x.Text, "A")
                .Add(x => x.Round, round));

            Assert.Contains(expected, cut.Markup);
        }

        [Fact]
        public void RoundRadius_OverridesRoundPreset()
        {
            var cut = Render<ElMarqueeGrow>(p => p
                .Add(x => x.Text, "A")
                .Add(x => x.Round, ElMarqueeRound.Pill)
                .Add(x => x.RoundRadius, 12));

            Assert.Contains("border-radius:12px", cut.Markup);
            Assert.DoesNotContain("border-radius:999px", cut.Markup);
        }

        [Fact]
        public void RoundRadius_NegativeIsClampedToZero()
        {
            var cut = Render<ElMarqueeGrow>(p => p
                .Add(x => x.Text, "A")
                .Add(x => x.RoundRadius, -5));

            Assert.Contains("border-radius:0px", cut.Markup);
        }

        [Fact]
        public void Bordered_AddsBorderWithDefaultColor()
        {
            var cut = Render<ElMarqueeGrow>(p => p
                .Add(x => x.Text, "A")
                .Add(x => x.Bordered, true));

            Assert.Contains("border:1px solid #dcdfe6", cut.Markup);
        }

        [Fact]
        public void Bordered_UsesCustomColorAndWidth()
        {
            var cut = Render<ElMarqueeGrow>(p => p
                .Add(x => x.Text, "A")
                .Add(x => x.Bordered, true)
                .Add(x => x.BorderColor, "#409eff")
                .Add(x => x.BorderWidth, 2));

            Assert.Contains("border:2px solid #409eff", cut.Markup);
        }

        [Fact]
        public void NotBordered_OmitsBorderDeclaration()
        {
            var cut = Render<ElMarqueeGrow>(p => p
                .Add(x => x.Text, "A")
                .Add(x => x.Bordered, false));

            Assert.DoesNotContain("border:1px solid", cut.Markup);
        }

        [Fact]
        public void PauseOnHover_TogglesItemClass()
        {
            var on = Render<ElMarqueeGrow>(p => p
                .Add(x => x.Text, "A")
                .Add(x => x.PauseOnHover, true));
            Assert.Contains("el-marquee-grow__item is-pausable", on.Markup);

            var off = Render<ElMarqueeGrow>(p => p
                .Add(x => x.Text, "A")
                .Add(x => x.PauseOnHover, false));
            Assert.DoesNotContain("is-pausable", off.Markup);
        }

        [Fact]
        public void ChildContent_TakesPrecedenceOverText()
        {
            var cut = Render<ElMarqueeGrow>(p => p
                .Add(x => x.Text, "纯文本")
                .Add(x => x.ChildContent, "<b>富内容</b>"));

            Assert.Contains("<b>富内容</b>", cut.Markup);
            Assert.DoesNotContain("纯文本", cut.Markup);
        }

        [Fact]
        public void Duration_WrittenAsSecondsOnItem()
        {
            var cut = Render<ElMarqueeGrow>(p => p
                .Add(x => x.Text, "A")
                .Add(x => x.Duration, 20));

            Assert.Contains("animation-duration:20s", cut.Markup);
        }
    }
}
