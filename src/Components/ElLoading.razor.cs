using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElLoading : ElementComponentBase, IAsyncDisposable
    {
        private bool _disposed;
        private bool _lockApplied;

        [Parameter]
        public bool Loading { get; set; } = true;

        [Parameter]
        public string IconClass { get; set; }

        [Parameter]
        public string Text { get; set; } = "拼命加载中";

        [Parameter]
        public string Background { get; set; }

        [Parameter]
        public bool Fullscreen { get; set; }

        [Parameter]
        public bool Lock { get; set; }

        [Parameter]
        public RenderFragment Spinner { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>mask 层自定义 class（对应官方 custom-class）</summary>
        [Parameter]
        public string CustomClass { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            // Fullscreen + Lock → 锁 body scroll
            if (Loading && Fullscreen && Lock && !_lockApplied)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("elementLoadingLock");
                    _lockApplied = true;
                }
                catch { }
            }
            else if ((!Loading || !Fullscreen || !Lock) && _lockApplied)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("elementLoadingUnlock");
                    _lockApplied = false;
                }
                catch { }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            if (_lockApplied)
            {
                try { await JSRuntime.InvokeVoidAsync("elementLoadingUnlock"); } catch { }
                _lockApplied = false;
            }
        }

        private string WrapperClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-loading-parent")
            .AddIf(Loading, "is-loading")
            .Add(Cls)
            .ToString();

        private string WrapperStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add("position:relative")
            .Add(Style)
            .ToString();

        private string MaskClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-loading-mask")
            .AddIf(Fullscreen, "is-fullscreen")
            .Add(CustomClass)
            .ToString();

        private string MaskStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(!string.IsNullOrWhiteSpace(Background), $"background-color:{Background}")
            .ToString();
    }
}