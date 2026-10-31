using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElAffix : ElementComponentBase, IAsyncDisposable
    {
        private DotNetObjectReference<ElAffix> objectReference;
        private string optionsKey;
        private bool isFixed;
        private bool _disposed;

        #region 参数

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>吸顶偏移量（Position=Top 时生效，默认 0）</summary>
        [Parameter]
        public int OffsetTop { get; set; }

        /// <summary>吸底偏移量（Position=Bottom 时生效，默认 0）</summary>
        [Parameter]
        public int OffsetBottom { get; set; }

        /// <summary>兼容旧版：当 Position=Top 等同 OffsetTop，Position=Bottom 等同 OffsetBottom</summary>
        [Parameter]
        public int Offset
        {
            get => Position == AffixPosition.Bottom ? OffsetBottom : OffsetTop;
            set
            {
                if (Position == AffixPosition.Bottom) OffsetBottom = value;
                else OffsetTop = value;
            }
        }

        [Parameter]
        public AffixPosition Position { get; set; } = AffixPosition.Top;

        /// <summary>滚动容器 CSS 选择器（为空 = window）</summary>
        [Parameter]
        public string Target { get; set; }

        [Parameter]
        public int ZIndex { get; set; } = 100;

        [Parameter]
        public string Id { get; set; }

        #endregion

        #region 事件

        [Parameter]
        public EventCallback<bool> OnChange { get; set; }

        [Parameter]
        public EventCallback<AffixScrollEventArgs> OnScroll { get; set; }

        #endregion

        public ElementReference RootElement { get; set; }

        /// <summary>当前是否处于固钉态（只读，供调用方/动画读取）</summary>
        public bool IsFixed => isFixed;

        /// <summary>当前生效的偏移量（根据 Position 取对应值）</summary>
        private int EffectiveOffset => Position == AffixPosition.Bottom ? OffsetBottom : OffsetTop;

        protected string AffixClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-affix", Cls)
            .AddIf(isFixed, "is-fixed")
            .ToString();

        protected string AffixContentClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-affix__content")
            .AddIf(isFixed, "el-affix--fixed")
            .ToString();

        /// <summary>
        /// 注意：这里 <b>不能</b> 依赖 <see cref="isFixed"/> 与否而改变取值。
        /// 固钉的 position/left/top/width 是由 JS（elementAffixInit）直接写在
        /// <c>.el-affix__content</c> 的内联样式上的；Blazor 只会「值变了才写 DOM」，
        /// 一旦本属性随 isFixed 翻转（例如空串 → z-index:100），Blazor 就会整体重写
        /// 该元素的 style 属性，把 JS 刚写的定位全部抹掉，固钉会静默失效。
        /// 因此这里恒定输出同一个值（z-index 对 static 元素无副作用，不影响非固钉态）。
        /// </summary>
        protected string AffixContentStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add($"z-index:{ZIndex}")
            .ToString();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            var nextOptionsKey = $"{EffectiveOffset}|{Position}|{Target}|{ZIndex}";
            if (optionsKey == nextOptionsKey)
            {
                return;
            }

            // 清理旧的 JS 监听（参数变化时）
            if (!firstRender && RootElement.Id != null)
            {
                try { await JSRuntime.InvokeVoidAsync("elementAffixDispose", RootElement); } catch { }
            }

            optionsKey = nextOptionsKey;
            objectReference ??= DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("elementAffixInit", RootElement, objectReference, new
            {
                offset = EffectiveOffset,
                position = Position.ToString().ToLowerInvariant(),
                target = Target,
                zIndex = ZIndex
            });
        }

        [JSInvokable]
        public async Task SetFixed(bool nextFixed, int scrollTop)
        {
            var changed = isFixed != nextFixed;
            isFixed = nextFixed;

            if (OnScroll.HasDelegate)
            {
                await OnScroll.InvokeAsync(new AffixScrollEventArgs
                {
                    Fixed = nextFixed,
                    ScrollTop = scrollTop
                });
            }

            if (changed && OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(nextFixed);
            }

            if (changed)
            {
                await InvokeAsync(StateHasChanged);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                await JSRuntime.InvokeVoidAsync("elementAffixDispose", RootElement);
            }
            catch { }
            objectReference?.Dispose();
            objectReference = null;
        }
    }
}