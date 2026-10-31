using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElTourTests : BunitContext
    {
        public ElTourTests()
        {
            Services.AddElementPlusServices();
            // 切步会调 elementScrollIntoView（官方 scroll-into-view-options）；bUnit 里没有布局可滚，
            // 用 Loose 放行，但下面有用例专门验这次调用真的发生、且参数是 block:center。
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        // 圆点没有可读文本，active 只能按下标找
        private static int ActiveDotIndex(IReadOnlyList<AngleSharp.Dom.IElement> dots)
        {
            for (var i = 0; i < dots.Count; i++)
            {
                if (dots[i].ClassList.Contains("is-active")) return i;
            }
            return -1;
        }

        private static List<TourStep> TwoSteps() => new List<TourStep>
        {
            new TourStep { Title = "Step A", Description = "First" },
            new TourStep { Title = "Step B", Description = "Second" }
        };

        [Fact]
        public void ShowsPanelWhenModelValueTrue()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoSteps()));

            Assert.NotNull(cut.Find(".el-tour__content"));
            Assert.Contains("Step A", cut.Markup);
        }

        [Fact]
        public void NextAdvancesCurrent()
        {
            int current = 0;
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoSteps())
                .Add(x => x.OnChange, c => current = c));

            cut.Find(".el-tour__actions button:last-child").Click();

            cut.WaitForAssertion(() => Assert.Equal(1, current), timeout: TestWait.Timeout);
        }

        [Fact]
        public void IndicatorsSlotReplacesDefaultDots()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoSteps())
                .Add(x => x.Indicators, (RenderFragment<TourStep>)(s => b => b.AddContent(0, "IND"))));

            Assert.Contains("IND", cut.Markup);
            Assert.DoesNotContain("el-tour__counter", cut.Markup);
            // 正向对照：插槽换掉的是官方那组圆点，两者不并存
            Assert.Empty(cut.FindAll(".el-tour-indicator"));
            Assert.Single(cut.FindAll(".el-tour-indicators"));
        }

        [Fact]
        public void CloseButtonHidesTour()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoSteps())
                .Add(x => x.ShowClose, true));

            cut.Find(".el-tour__close").Click();

            cut.WaitForAssertion(() => Assert.False(cut.Instance.ModelValue), timeout: TestWait.Timeout);
        }

        [Fact]
        public void PlacementFallsBackToComponentDefault()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoSteps())
                .Add(x => x.Placement, "left"));

            // step 未显式指定 Placement 时，面板使用组件级 Placement 兜底（is-left）
            Assert.Contains("is-left", cut.Find(".el-tour__content").ClassName);
            Assert.DoesNotContain("is-bottom", cut.Find(".el-tour__content").ClassName);
        }

        [Fact]
        public void StepPlacementOverridesComponentDefault()
        {
            var steps = new List<TourStep>
            {
                new TourStep { Title = "A", Description = "First", Placement = "top" },
                new TourStep { Title = "B", Description = "Second" }
            };
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, steps)
                .Add(x => x.Placement, "left"));

            // 第一步显式 Placement=top 覆盖组件级 left
            Assert.Contains("is-top", cut.Find(".el-tour__content").ClassName);
            Assert.DoesNotContain("is-left", cut.Find(".el-tour__content").ClassName);
        }

        [Fact]
        public void RendersAppendToAttribute()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoSteps())
                .Add(x => x.AppendTo, ".my-tour-target"));

            Assert.Contains("data-append-to=\".my-tour-target\"", cut.Markup);
        }

        // ===== 以下对齐 EP 2.14.0 packages/components/tour（用户三次报「没效果」的三条主因：
        // 遮罩没有洞、箭头没有 CSS、键盘与步序复位）。几何部分见
        // demo/tools/probes/verify-tour-hollow-mask.cjs（bUnit 没有布局引擎）。 =====

        private static List<TourStep> TwoTargetedSteps() => new List<TourStep>
        {
            new TourStep { Target = "#t1", Title = "Step A", Description = "First" },
            new TourStep { Target = "#t2", Title = "Step B", Description = "Second" }
        };

        [Fact]
        public void DefaultIndicatorsAreOfficialDots()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps()));

            // 官方 step.vue 渲染的是 6×6 圆点（el-tour-indicator），不是「1 / N」计数器
            var dots = cut.FindAll(".el-tour-indicator");
            Assert.Equal(2, dots.Count);
            Assert.Equal(0, ActiveDotIndex(dots));
            Assert.DoesNotContain("el-tour__counter", cut.Markup);
        }

        [Fact]
        public void IndicatorActiveFollowsCurrent()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps()));

            cut.Find(".el-tour__actions button:last-child").Click();

            cut.WaitForAssertion(() => Assert.Equal(1, ActiveDotIndex(cut.FindAll(".el-tour-indicator"))), timeout: TestWait.Timeout);
        }

        [Fact]
        public void MaskDrawsHollowPathWhenEnabled()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps()));

            // d 由 dom.js 逐帧写入（这里只验结构：官方 mask.vue 是 <div><svg><path>）
            Assert.NotNull(cut.Find(".el-tour__mask > svg > path.el-tour__hollow"));
        }

        [Fact]
        public void MaskOmittedWhenMaskFalse()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps())
                .Add(x => x.Mask, false));

            Assert.Empty(cut.FindAll(".el-tour__mask"));
            Assert.NotNull(cut.Find(".el-tour__content"));
        }

        [Fact]
        public void ArrowOnlyRenderedForTargetedStep()
        {
            var withTarget = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps()));
            Assert.Single(withTarget.FindAll(".el-tour__content > .el-tour__arrow"));

            // 官方 tour.vue：mergedShowArrow = !!currentTarget && (…) —— 没有目标那一步不出箭头
            var noTarget = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoSteps()));
            Assert.Empty(noTarget.FindAll(".el-tour__arrow"));
        }

        [Fact]
        public void ArrowOmittedWhenShowArrowFalse()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps())
                .Add(x => x.ShowArrow, false));

            Assert.Empty(cut.FindAll(".el-tour__arrow"));
            Assert.NotNull(cut.Find(".el-tour__content"));
        }

        [Fact]
        public void MaskPassesClicksThroughOnlyWhenTargetExists()
        {
            var targeted = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps()));
            // 官方 mask.vue：镂空外层 pointer-events 由「有目标 && targetAreaClickable」决定，
            // 真正吃点击的是 path；这样洞里的目标才点得穿。
            Assert.DoesNotContain("pointer-events:auto", targeted.Find(".el-tour__mask").GetAttribute("style"));

            var noTarget = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoSteps()));
            Assert.Contains("pointer-events:auto", noTarget.Find(".el-tour__mask").GetAttribute("style"));
        }

        [Fact]
        public void MaskBlocksTargetClicksWhenTargetAreaClickableFalse()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps())
                .Add(x => x.TargetAreaClickable, false));

            Assert.Contains("pointer-events:auto", cut.Find(".el-tour__mask").GetAttribute("style"));
        }

        [Fact]
        public void MaskClickDoesNotCloseByDefault()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps()));

            // EP 2.14.0 的 tour 根本没有 close-on-click-mask 这个 prop，path 上也没有 click 监听
            cut.Find(".el-tour__mask").Click();

            Assert.True(cut.Instance.ModelValue);
        }

        [Fact]
        public void MaskClickClosesWhenHostEnablesIt()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps())
                .Add(x => x.CloseOnClickMask, true));

            cut.Find(".el-tour__mask").Click();

            cut.WaitForAssertion(() => Assert.False(cut.Instance.ModelValue), timeout: TestWait.Timeout);
        }

        [Fact]
        public void UnrelatedKeyDownKeepsTourOpen()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps()));

            // 改动前这里不判键名：面板内任意 keydown 都会关掉整条引导
            cut.Find(".el-tour__content").KeyDown(new KeyboardEventArgs { Key = "a" });
            cut.Find(".el-tour__content").KeyDown(new KeyboardEventArgs { Key = "Tab" });

            Assert.True(cut.Instance.ModelValue);
            Assert.Equal(0, cut.Instance.Current);
        }

        [Fact]
        public void EscapeClosesTour()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps()));

            cut.Find(".el-tour__content").KeyDown(new KeyboardEventArgs { Key = "Escape" });

            cut.WaitForAssertion(() => Assert.False(cut.Instance.ModelValue), timeout: TestWait.Timeout);
        }

        [Fact]
        public void EscapeIgnoredWhenCloseOnPressEscapeFalse()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps())
                .Add(x => x.CloseOnPressEscape, false));

            cut.Find(".el-tour__content").KeyDown(new KeyboardEventArgs { Key = "Escape" });

            Assert.True(cut.Instance.ModelValue);
        }

        [Fact]
        public void ArrowKeysSwitchStepsAndRightOnLastStepFinishes()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps()));

            cut.Find(".el-tour__content").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
            cut.WaitForAssertion(() => Assert.Equal(1, cut.Instance.Current), timeout: TestWait.Timeout);

            // 官方 step.vue：右方向键在最后一步等于「完成」
            cut.Find(".el-tour__content").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
            cut.WaitForAssertion(() => Assert.False(cut.Instance.ModelValue), timeout: TestWait.Timeout);
        }

        [Fact]
        public void ArrowLeftGoesBack()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps())
                .Add(x => x.Current, 1));

            cut.Find(".el-tour__content").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

            cut.WaitForAssertion(() => Assert.Equal(0, cut.Instance.Current), timeout: TestWait.Timeout);
        }

        [Fact]
        public void CurrentResetsWhenClosedByCloseButton()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps())
                .Add(x => x.Current, 1));

            cut.Find(".el-tour__close").Click();

            // 官方 tour.vue 的 watch(modelValue)：关掉引导时 current 归零，否则重开停在旧步
            cut.WaitForAssertion(() =>
            {
                Assert.False(cut.Instance.ModelValue);
                Assert.Equal(0, cut.Instance.Current);
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public void CurrentResetsWhenFinished()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps())
                .Add(x => x.Current, 1));

            cut.Find(".el-tour__actions button:last-child").Click();

            cut.WaitForAssertion(() =>
            {
                Assert.False(cut.Instance.ModelValue);
                Assert.Equal(0, cut.Instance.Current);
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public void GapDataAttributesUseOfficialDefaults()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps()));

            // 官方 types.ts 的 gap 只有 offset(6) / radius(2)，且没有 padding
            var panel = cut.Find(".el-tour__content");
            Assert.Equal("6", panel.GetAttribute("data-gap-offset"));
            Assert.Equal("2", panel.GetAttribute("data-gap-radius"));
            Assert.Equal("10", panel.GetAttribute("data-offset"));
        }

        [Fact]
        public void GapOverrideReachesDataAttributes()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps())
                .Add(x => x.Gap, new TourGap { Offset = 12, Radius = 8 }));

            var panel = cut.Find(".el-tour__content");
            Assert.Equal("12", panel.GetAttribute("data-gap-offset"));
            Assert.Equal("8", panel.GetAttribute("data-gap-radius"));
        }

        [Fact]
        public void StepOffsetAndZIndexFeedPanel()
        {
            var steps = new List<TourStep>
            {
                new TourStep { Target = "#t1", Title = "A", Description = "d", Offset = 24, ZIndex = 3000 }
            };
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, steps)
                .Add(x => x.ZIndex, 2001));

            var panel = cut.Find(".el-tour__content");
            Assert.Equal("24", panel.GetAttribute("data-offset"));
            Assert.Contains("z-index:3000", panel.GetAttribute("style"));
            Assert.Contains("z-index:2999", cut.Find(".el-tour__mask").GetAttribute("style"));
        }

        [Fact]
        public void PanelAndMaskStylesCarryNoInventedGeometry()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps())
                .Add(x => x.Gap, new TourGap { Offset = 12, Radius = 8 }));

            // 回归守卫：旧实现把浮层 offset 当面板 padding 写（面板凭空矮 6px），
            // 又把 gap.padding 当遮罩 inset 写（整块暗幕内缩、根本没有洞）。
            var panelStyle = cut.Find(".el-tour__content").GetAttribute("style");
            var maskStyle = cut.Find(".el-tour__mask").GetAttribute("style");
            Assert.Contains("z-index:", panelStyle);
            Assert.DoesNotContain("padding", panelStyle);
            Assert.DoesNotContain("inset", maskStyle);
        }

        [Fact]
        public void HeaderCarriesShowCloseWhenCloseVisible()
        {
            var shown = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps()));
            Assert.Contains("show-close", shown.Find(".el-tour__header").ClassList);

            var hidden = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps())
                .Add(x => x.ShowClose, false));
            Assert.DoesNotContain("show-close", hidden.Find(".el-tour__header").ClassList);
            Assert.Empty(hidden.FindAll(".el-tour__close"));
        }

        [Fact]
        public void StepChangeScrollsTargetIntoViewAtCenter()
        {
            var cut = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps()));

            cut.Find(".el-tour__actions button:last-child").Click();

            // 官方 scroll-into-view-options 默认 { block: 'center' }
            cut.WaitForAssertion(() =>
            {
                var call = JSInterop.Invocations.First(i => i.Identifier == "elementScrollIntoView");
                Assert.Contains("#t2", call.Arguments);
                Assert.Contains("{\"block\":\"center\"}", call.Arguments);
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public void ScrollIntoViewSkippedWhenDisabledOrStepHasNoTarget()
        {
            var disabled = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoTargetedSteps())
                .Add(x => x.ScrollIntoView, false));
            disabled.Find(".el-tour__actions button:last-child").Click();
            Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "elementScrollIntoView");

            // 没有 Target 的一步：官方 useTarget 拿不到 reference，也就没什么可滚
            var noTarget = Render<ElTour>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Steps, TwoSteps()));
            noTarget.Find(".el-tour__actions button:last-child").Click();
            Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "elementScrollIntoView");
        }
    }
}
