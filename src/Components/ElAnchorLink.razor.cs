using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElAnchorLink : ElementComponentBase
    {
        [CascadingParameter]
        public ElAnchor Anchor { get; set; }

        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public string Href { get; set; }

        /// <summary>原生 a 标签的 target（官方 AnchorLink 属性，默认空 = 当前窗口）</summary>
        [Parameter]
        public string Target { get; set; }

        /// <summary>
        /// 单条链接自己的滚动偏移量（官方 2.10.5 新增）。
        /// 未设置时回落到 <see cref="ElAnchor.Offset"/>。
        /// </summary>
        [Parameter]
        public int? Offset { get; set; }

        [Parameter]
        public RenderFragment TitleContent { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        internal string EffectiveTitle => string.IsNullOrWhiteSpace(Title) ? Href : Title;

        internal int? EffectiveOffset => Offset;

        protected string ItemClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-anchor__item", Cls)
            .AddIf(Anchor?.IsActive(Href) == true, "is-active")
            .ToString();

        protected async Task OnClickAsync(MouseEventArgs e)
        {
            if (Anchor != null)
            {
                await Anchor.HandleLinkClickAsync(this, e);
                return;
            }

            if (!string.IsNullOrWhiteSpace(Href))
            {
                NavigationManager.NavigateTo(Href);
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            if (Anchor != null)
            {
                Anchor.ActiveChanged += OnAnchorActiveChanged;
            }
        }

        private void OnAnchorActiveChanged()
        {
            _ = InvokeAsync(StateHasChanged);
        }

        public override void Dispose()
        {
            if (Anchor != null)
            {
                Anchor.ActiveChanged -= OnAnchorActiveChanged;
            }
            base.Dispose();
        }
    }
}
