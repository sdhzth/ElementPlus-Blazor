using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    public partial class ElBreadcrumb : ElementComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public string Separator { get; set; } = "/";

        [Parameter]
        public string SeparatorIcon { get; set; }

        /// <summary>自定义分隔符模板（扩展：优先级高于 Separator / SeparatorIcon）</summary>
        [Parameter]
        public RenderFragment SeparatorTemplate { get; set; }

        [Parameter]
        public string Id { get; set; }

        /// <summary>无障碍标签；未设置时回落到官方默认 "Breadcrumb"</summary>
        [Parameter]
        public string AriaLabel { get; set; }

        protected string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) ? "Breadcrumb" : AriaLabel;

        protected override bool ShouldRender()
        {
            return true;
        }
    }
}
