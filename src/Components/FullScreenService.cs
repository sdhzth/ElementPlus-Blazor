using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 浏览器全屏服务，替代 BootstrapBlazor 的 <c>FullScreenService</c>。
    /// JS 通道在 <c>wwwroot/js/dom.js</c>：<c>elementFullscreenEnter</c> / <c>elementFullscreenExit</c> /
    /// <c>elementFullscreenToggle</c> / <c>elementIsFullscreen</c>。
    /// </summary>
    public class FullScreenService
    {
        private readonly IJSRuntime jsRuntime;

        public FullScreenService(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        /// <summary>当前是否处于全屏。</summary>
        public async Task<bool> IsFullScreenAsync()
        {
            return await jsRuntime.InvokeAsync<bool>("elementIsFullscreen");
        }

        /// <summary>整个文档进入全屏。</summary>
        public async Task<bool> EnterAsync()
        {
            return await jsRuntime.InvokeAsync<bool>("elementFullscreenEnter");
        }

        /// <summary>指定元素进入全屏。</summary>
        public async Task<bool> EnterAsync(ElementReference target)
        {
            return await jsRuntime.InvokeAsync<bool>("elementFullscreenEnter", target);
        }

        /// <summary>退出全屏。</summary>
        public async Task<bool> ExitAsync()
        {
            return await jsRuntime.InvokeAsync<bool>("elementFullscreenExit");
        }

        /// <summary>切换全屏，返回切换后的全屏状态。</summary>
        public async Task<bool> ToggleAsync()
        {
            return await jsRuntime.InvokeAsync<bool>("elementFullscreenToggle");
        }

        /// <summary>切换指定元素的全屏，返回切换后的全屏状态。</summary>
        public async Task<bool> ToggleAsync(ElementReference target)
        {
            return await jsRuntime.InvokeAsync<bool>("elementFullscreenToggle", target);
        }
    }
}
