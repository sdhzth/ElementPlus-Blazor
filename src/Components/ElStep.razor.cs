using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    public partial class ElStep : ElementComponentBase
    {
        [CascadingParameter]
        public ElSteps Steps { get; set; }

        /// <summary>
        /// 父级的渲染令牌（见 <see cref="ElSteps.RenderToken"/>）：父级每次渲染都会换新实例，
        /// 仅用于让本组件跟随父级状态一起重渲染 —— 本组件参数恒定不变，
        /// 不声明该级联参数就永远不会被重新渲染，父级 Active 变化时外观不会更新。
        /// </summary>
        [CascadingParameter]
        public ElSteps.RenderToken RenderToken { get; set; }

        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public string Description { get; set; }

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public StepStatus? Status { get; set; }

        /// <summary>
        /// 单步骤自己的宽度/间距（官方 Step 的 space 属性）。
        /// 未设置时回落到 <see cref="ElSteps.Space"/>。
        /// </summary>
        [Parameter]
        public string Space { get; set; }

        protected string EffectiveSpace => !string.IsNullOrWhiteSpace(Space) ? Space : Steps?.Space;

        [Parameter]
        public RenderFragment TitleContent { get; set; }

        [Parameter]
        public RenderFragment DescriptionContent { get; set; }

        [Parameter]
        public RenderFragment IconContent { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        internal int Index { get; private set; }

        protected int DisplayIndex => Index + 1;

        protected StepDirection Direction => Steps?.Direction ?? StepDirection.Horizontal;

        protected bool IsSimple => Steps?.Simple == true;

        protected StepStatus EffectiveStatus => Steps?.GetStepStatus(Index, Status) ?? (Status ?? StepStatus.Wait);

        protected string StatusName => EffectiveStatus.ToString().ToLowerInvariant();

        protected bool HasDescription => DescriptionContent != null || !string.IsNullOrWhiteSpace(Description);

        protected string StepClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-step", Cls)
            .Add($"is-{Direction.ToString().ToLowerInvariant()}")
            .AddIf(Steps?.AlignCenter == true, "is-center")
            .AddIf(IsSimple, "is-simple")
            .AddIf(Steps?.ShouldFlex == true, "is-flex")
            .ToString();

        protected string StepStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(Steps?.Direction == StepDirection.Horizontal && !string.IsNullOrWhiteSpace(EffectiveSpace), $"flex-basis:{ElementCssUtility.NormalizeCssSize(EffectiveSpace)}")
            .AddIf(Steps?.Direction == StepDirection.Horizontal && !string.IsNullOrWhiteSpace(EffectiveSpace), $"max-width:{ElementCssUtility.NormalizeCssSize(EffectiveSpace)}")
            .AddIf(Steps?.ShouldFlex == true, "flex:1")
            .Add(Style)
            .ToString();

        protected string HeadClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-step__head")
            .Add($"is-{StatusName}")
            .ToString();

        protected string TitleClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-step__title")
            .Add($"is-{StatusName}")
            .ToString();

        protected string DescriptionClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-step__description")
            .Add($"is-{StatusName}")
            .ToString();

        protected string IconBoxClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-step__icon")
            .AddIf(HasIcon, "is-icon")
            .AddIf(!HasIcon, "is-text")
            .ToString();

        protected bool HasIcon => IconContent != null || !string.IsNullOrWhiteSpace(Icon) || !string.IsNullOrWhiteSpace(StatusIcon);

        protected string IconInnerClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .AddIf(!string.IsNullOrWhiteSpace(Icon), "el-step__icon-inner", $"el-icon-{Icon}")
            .AddIf(string.IsNullOrWhiteSpace(Icon) && !string.IsNullOrWhiteSpace(StatusIcon), "el-step__icon-inner", "is-status", StatusIcon)
            .ToString();

        private string StatusIcon => EffectiveStatus switch
        {
            StepStatus.Success => "el-icon-check",
            StepStatus.Error => "el-icon-close",
            StepStatus.Finish => "el-icon-check",
            _ => null
        };

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Index = Steps?.RegisterStep() ?? 0;
        }
    }
}
