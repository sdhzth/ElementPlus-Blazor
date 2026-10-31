using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElStepsTests : BunitContext
    {
        public ElStepsTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        /// <summary>
        /// 宿主：模拟 demo 页面的写法（<c>&lt;ElSteps Active="@Active"&gt;</c> + 子 ElStep），
        /// 用于验证父级 Active 变化后子步骤是否重算状态。
        /// </summary>
        private sealed class StepsHost : ComponentBase
        {
            public int Active { get; set; } = 1;

            public void SetActive(int value)
            {
                Active = value;
                StateHasChanged();
            }

            protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
            {
                builder.OpenComponent<ElSteps>(0);
                builder.AddAttribute(1, nameof(ElSteps.Active), Active);
                builder.AddAttribute(2, nameof(ElSteps.ChildContent), ThreeSteps());
                builder.CloseComponent();
            }
        }

        [Fact]
        public async Task RecomputesStatusWhenActiveChanges()
        {
            var cut = Render<StepsHost>();

            var before = cut.FindAll(".el-step__head");
            Assert.Contains("is-finish", before[0].GetAttribute("class"));
            Assert.Contains("is-process", before[1].GetAttribute("class"));
            Assert.Contains("is-wait", before[2].GetAttribute("class"));

            await cut.InvokeAsync(() => cut.Instance.SetActive(2));

            var after = cut.FindAll(".el-step__head");
            Assert.Contains("is-finish", after[0].GetAttribute("class"));
            Assert.Contains("is-finish", after[1].GetAttribute("class"));
            Assert.Contains("is-process", after[2].GetAttribute("class"));
        }

        // ---- 钉住修复所依赖的机制：级联值变化可以强制「参数未变」的子组件重渲染 ----
        private sealed class TokenChild : ComponentBase
        {
            public static int RenderCount;

            /// <summary>刻意声明成与父级令牌同类型：这正是 ElStep 跟随 ElSteps 刷新的通道。</summary>
            [CascadingParameter] public ElSteps.RenderToken Token { get; set; }

            [Parameter] public string Title { get; set; }

            protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
            {
                RenderCount++;
                builder.AddContent(0, Title);
            }
        }

        private sealed class TokenCascadeParent : ComponentBase
        {
            public int Tick { get; private set; }

            public void Bump()
            {
                Tick++;
                StateHasChanged();
            }

            protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
            {
                builder.OpenComponent<CascadingValue<ElSteps.RenderToken>>(0);
                builder.AddAttribute(1, nameof(CascadingValue<ElSteps.RenderToken>.Value), new ElSteps.RenderToken());
                builder.AddAttribute(2, nameof(CascadingValue<ElSteps.RenderToken>.IsFixed), false);
                builder.AddAttribute(3, nameof(CascadingValue<ElSteps.RenderToken>.ChildContent), (RenderFragment)(x =>
                {
                    x.OpenComponent<TokenChild>(0);
                    // 子组件参数恒不变化 —— 仅靠级联令牌变化就必须让它重渲染
                    x.AddAttribute(1, nameof(TokenChild.Title), "same");
                    x.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }

        /// <summary>
        /// ElStep 的外观由父级 ElSteps 的状态推导，自身参数恒定不变；若只靠参数变更检测，
        /// 父级 Active 变化时子步骤会被跳过（UI 冻结在首帧）。本用例钉住修复所依赖的机制：
        /// 级联值变化必须能把「参数未变」的子组件拉起来重渲染。
        /// </summary>
        [Fact]
        public async Task CascadingTokenChangeForcesChildRerender()
        {
            TokenChild.RenderCount = 0;
            var cut = Render<TokenCascadeParent>();
            Assert.Equal(1, TokenChild.RenderCount);

            await cut.InvokeAsync(() => cut.Instance.Bump());

            Assert.Equal(2, TokenChild.RenderCount);
        }

        private static RenderFragment ThreeSteps() => builder =>
        {
            builder.OpenComponent<ElStep>(0);
            builder.AddAttribute(1, "Title", "步骤 1");
            builder.CloseComponent();
            builder.OpenComponent<ElStep>(2);
            builder.AddAttribute(3, "Title", "步骤 2");
            builder.CloseComponent();
            builder.OpenComponent<ElStep>(4);
            builder.AddAttribute(5, "Title", "步骤 3");
            builder.CloseComponent();
        };

        [Fact]
        public void RendersThreeStepsInOrder()
        {
            var cut = Render<ElSteps>(p => p.AddChildContent(ThreeSteps()));

            Assert.Equal(3, cut.FindAll(".el-step").Count);
            Assert.Contains("步骤 1", cut.FindAll(".el-step__title")[0].TextContent);
            Assert.Contains("步骤 3", cut.FindAll(".el-step__title")[2].TextContent);
        }

        private static RenderFragment FourSteps() => builder =>
        {
            for (var i = 0; i < 4; i++)
            {
                builder.OpenComponent<ElStep>(i * 2);
                builder.AddAttribute(i * 2 + 1, "Title", $"步骤 {i + 1}");
                builder.CloseComponent();
            }
        };

        /// <summary>
        /// 最后一步（<c>Active</c> == 末位下标 3）应处于 <c>is-process</c>。
        /// 这是 Element Plus 的语义：process 用「深色数字圆点 + 深色加粗标题」表示当前步骤，
        /// 不是没刷新——本项目曾因为该外观与 finish 的蓝色差异过大而被误判成"状态没变"。
        /// </summary>
        [Fact]
        public void LastStepIsProcessWhenActiveIsLastIndex()
        {
            var cut = Render<ElSteps>(p => p.Add(x => x.Active, 3).AddChildContent(FourSteps()));

            var heads = cut.FindAll(".el-step__head");
            Assert.Equal(4, heads.Count);
            Assert.Contains("is-finish", heads[0].GetAttribute("class"));
            Assert.Contains("is-finish", heads[1].GetAttribute("class"));
            Assert.Contains("is-finish", heads[2].GetAttribute("class"));
            Assert.Contains("is-process", heads[3].GetAttribute("class"));

            // process 不显示对勾，而是显示步骤序号
            Assert.Equal("4", cut.FindAll(".el-step__icon-inner")[3].TextContent.Trim());
        }

        /// <summary>
        /// <c>Active</c> 等于步骤总数（超出最后一个下标）时，所有步骤都是 <c>is-finish</c>（全部完成）。
        /// </summary>
        [Fact]
        public void AllStepsFinishWhenActiveEqualsStepCount()
        {
            var cut = Render<ElSteps>(p => p.Add(x => x.Active, 4).AddChildContent(FourSteps()));

            foreach (var head in cut.FindAll(".el-step__head"))
            {
                Assert.Contains("is-finish", head.GetAttribute("class"));
            }

            Assert.Equal(4, cut.FindAll(".el-step__icon-inner.is-status").Count);
        }

        [Fact]
        public void StatusDerivedFromActive()
        {
            var cut = Render<ElSteps>(p => p
                .Add(x => x.Active, 1)
                .AddChildContent(ThreeSteps()));

            var heads = cut.FindAll(".el-step__head");
            Assert.Contains("is-finish", heads[0].GetAttribute("class"));
            Assert.Contains("is-process", heads[1].GetAttribute("class"));
            Assert.Contains("is-wait", heads[2].GetAttribute("class"));
        }

        [Fact]
        public void SimpleAndAlignCenterAddClasses()
        {
            var cut = Render<ElSteps>(p => p
                .Add(x => x.Simple, true)
                .Add(x => x.AlignCenter, true)
                .AddChildContent(ThreeSteps()));

            Assert.Contains("el-steps--simple", cut.Find(".el-steps").GetAttribute("class"));
            Assert.Contains("is-center", cut.FindAll(".el-step")[0].GetAttribute("class"));
        }

        [Fact]
        public void VerticalDirectionAddsClass()
        {
            var cut = Render<ElSteps>(p => p
                .Add(x => x.Direction, StepDirection.Vertical)
                .AddChildContent(ThreeSteps()));

            Assert.Contains("el-steps--vertical", cut.Find(".el-steps").GetAttribute("class"));
        }

        [Fact]
        public void StepSpaceOverridesGlobalSpace()
        {
            var cut = Render<ElSteps>(p => p
                .Add(x => x.Space, "100px")
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElStep>(0);
                    builder.AddAttribute(1, "Title", "窄");
                    builder.AddAttribute(2, "Space", "260px");
                    builder.CloseComponent();
                    builder.OpenComponent<ElStep>(3);
                    builder.AddAttribute(4, "Title", "默认");
                    builder.CloseComponent();
                }));

            var styles = cut.FindAll(".el-step")[0].GetAttribute("style");
            Assert.Contains("260px", styles);

            var defaultStyle = cut.FindAll(".el-step")[1].GetAttribute("style");
            Assert.Contains("100px", defaultStyle);
        }

        [Fact]
        public void IconAndStatusSlotsRender()
        {
            var cut = Render<ElSteps>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElStep>(0);
                builder.AddAttribute(1, "Status", StepStatus.Error);
                builder.AddAttribute(2, "IconContent", (RenderFragment)(b =>
                {
                    b.OpenElement(0, "i");
                    b.AddAttribute(1, "class", "my-icon");
                    b.CloseElement();
                }));
                builder.CloseComponent();
            }));

            Assert.NotNull(cut.Find(".my-icon"));
            Assert.Contains("is-error", cut.Find(".el-step__head").GetAttribute("class"));
        }
    }
}
