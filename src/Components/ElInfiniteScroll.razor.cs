using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElInfiniteScroll : ElementComponentBase
    {
        private DateTime lastLoadTime = DateTime.MinValue;
        private int loadCount;

        private ElementReference containerRef;
        private bool lastScrollNearBottom = false;

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public EventCallback<InfiniteScrollEventArgs> OnLoad { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool Loading { get; set; }

        [Parameter]
        public int Delay { get; set; } = 200;

        [Parameter]
        public string Height { get; set; }

        [Parameter]
        public string MaxHeight { get; set; }

        /// <summary>距底部多少 px 触发加载 (默认 0，对齐 Element Plus infinite-scroll-distance)</summary>
        [Parameter]
        public int Distance { get; set; } = 0;

        /// <summary>立即加载一次</summary>
        [Parameter]
        public bool Immediate { get; set; } = true;

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        [Parameter]
        public string LoadingText { get; set; } = "加载中";

        [Parameter]
        public string DisabledText { get; set; } = "没有更多了";

        [Parameter]
        public RenderFragment LoadingContent { get; set; }

        [Parameter]
        public RenderFragment DisabledContent { get; set; }

        protected string ScrollClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-infinite-scroll", Cls)
            .AddIf(Disabled, "is-disabled")
            .AddIf(Loading, "is-loading")
            .ToString();

        protected string ScrollStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(!string.IsNullOrWhiteSpace(Height), $"height:{ElementCssUtility.NormalizeCssSize(Height)}")
            .AddIf(!string.IsNullOrWhiteSpace(MaxHeight), $"max-height:{ElementCssUtility.NormalizeCssSize(MaxHeight)}")
            .AddIf(!string.IsNullOrWhiteSpace(Height) || !string.IsNullOrWhiteSpace(MaxHeight), "overflow:auto")
            .Add(Style)
            .ToString();

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        protected ElementReference ContainerRef => containerRef;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender && Immediate && OnLoad.HasDelegate && !Disabled && !Loading)
            {
                await TryTriggerLoadAsync(null);
            }
        }

        protected async Task OnScrollAsync(EventArgs e)
        {
            if (Disabled || Loading || !OnLoad.HasDelegate)
            {
                return;
            }

            var nearBottom = await IsNearBottomAsync();
            if (!nearBottom)
            {
                lastScrollNearBottom = false;
                return;
            }

            // 边沿触发：从未到底→到了底时触发，避免反复触发
            if (lastScrollNearBottom)
            {
                return;
            }
            lastScrollNearBottom = true;

            var now = DateTime.UtcNow;
            if ((now - lastLoadTime).TotalMilliseconds < Delay)
            {
                return;
            }

            await TryTriggerLoadAsync(e);
        }

        private async Task TryTriggerLoadAsync(EventArgs e)
        {
            var now = DateTime.UtcNow;
            if ((now - lastLoadTime).TotalMilliseconds < Delay)
            {
                return;
            }

            lastLoadTime = now;
            loadCount++;
            await OnLoad.InvokeAsync(new InfiniteScrollEventArgs
            {
                ScrollEventArgs = e,
                LoadCount = loadCount
            });
        }

        private async Task<bool> IsNearBottomAsync()
        {
            try
            {
                var result = await JSRuntime.InvokeAsync<decimal>("Element.getScrollDistance", containerRef, Distance);
                return result == 1;
            }
            catch
            {
                return false;
            }
        }
    }
}
