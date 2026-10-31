using Microsoft.AspNetCore.Components;
using System;

namespace ElementPlus
{
    public partial class ElSkeleton : ElementComponentBase
    {
        [Parameter]
        public bool Loading { get; set; } = true;

        [Parameter]
        public bool Animated { get; set; }

        [Parameter]
        public bool Throttle { get; set; }

        [Parameter]
        public int Count { get; set; } = 1;

        [Parameter]
        public RenderFragment Template { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public bool Round { get; set; }

        [Parameter]
        public bool Bordered { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        protected int SafeCount => Math.Max(1, Count);

        protected string SkeletonClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-skeleton", Cls)
            .AddIf(Animated, "is-animated")
            .ToString();
    }
}
