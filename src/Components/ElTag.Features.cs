using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    public partial class ElTag
    {
        [Parameter]
        public TagEffect Effect
        {
            get => Theme switch
            {
                TagTheme.Dark => TagEffect.Dark,
                TagTheme.Plain => TagEffect.Plain,
                _ => TagEffect.Light
            };
            set => Theme = value switch
            {
                TagEffect.Dark => TagTheme.Dark,
                TagEffect.Plain => TagTheme.Plain,
                _ => TagTheme.Light
            };
        }

        [Parameter]
        public bool Round { get; set; }

        [Parameter]
        public bool Hit { get; set; }

        [Parameter]
        public bool DisableTransitions { get; set; }

        [Parameter]
        public EventCallback OnClose
        {
#pragma warning disable CS0618 // OnAfteClose 已过时，此处仅作内部存储转发
            get => OnAfteClose;
            set => OnAfteClose = value;
#pragma warning restore CS0618
        }

        private string FeatureClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add(Cls)
            .AddIf(Round, "is-round")
            .AddIf(Hit, "is-hit")
            .AddIf(DisableTransitions, "el-tag--no-transition")
            .ToString();
    }
}
