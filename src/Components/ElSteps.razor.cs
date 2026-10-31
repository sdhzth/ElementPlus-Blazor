using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    public partial class ElSteps : ElementComponentBase
    {
        /// <summary>
        /// 子步骤的重渲染令牌：<see cref="ElSteps"/> 每次渲染都会经级联下发一个<b>新实例</b>，
        /// 借级联值变更通知强制子 <see cref="ElStep"/> 重渲染。
        /// </summary>
        /// <remarks>
        /// 为什么必须是「每帧新实例」：子步骤的 <c>is-process / is-finish / is-wait</c> 外观完全由父级
        /// <see cref="Active"/>（以及 <see cref="Direction"/> / <see cref="Simple"/> / <see cref="AlignCenter"/> /
        /// <see cref="FinishStatus"/> / <see cref="ProcessStatus"/>）在渲染时推导，而 ElStep 自身参数恒定不变。
        /// 渲染器会跳过「参数值未变」的子组件，于是父级 Active 变化时子步骤不再重算，UI 冻结在首帧
        /// （demo <c>/steps</c> 点「下一步」没反应）。级联值变化走独立通知路径，可绕过该跳过。
        /// </remarks>
        public sealed class RenderToken
        {
        }

        private int nextStepIndex;

        /// <summary>
        /// 每次渲染读取本属性都会得到新实例（Razor 每次渲染只读一次），
        /// 使级联值必然发生变化，从而通知子步骤重渲染。
        /// </summary>
        internal RenderToken CurrentRenderToken => new RenderToken();

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public int Active { get; set; }

        [Parameter]
        public StepDirection Direction { get; set; }

        [Parameter]
        public bool AlignCenter { get; set; }

        [Parameter]
        public bool Simple { get; set; }

        [Parameter]
        public StepStatus FinishStatus { get; set; } = StepStatus.Finish;

        [Parameter]
        public StepStatus ProcessStatus { get; set; } = StepStatus.Process;

        [Parameter]
        public string Space { get; set; }

        protected string StepsClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-steps", Cls)
            .Add($"el-steps--{Direction.ToString().ToLowerInvariant()}")
            .AddIf(Simple, "el-steps--simple")
            .ToString();

        protected string StepsStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add(Style)
            .ToString();

        internal int RegisterStep()
        {
            return nextStepIndex++;
        }

        internal StepStatus GetStepStatus(int index, StepStatus? status)
        {
            if (status.HasValue)
            {
                return status.Value;
            }

            if (index < Active)
            {
                return FinishStatus;
            }

            return index == Active ? ProcessStatus : StepStatus.Wait;
        }

        internal bool ShouldFlex => Direction == StepDirection.Horizontal && string.IsNullOrWhiteSpace(Space);
    }
}
