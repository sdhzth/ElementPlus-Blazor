using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElAnchor : ElementComponentBase, IAsyncDisposable
    {
        private DotNetObjectReference<ElAnchor> objectReference;
        private string activeHref;
        private string optionsKey;
        private bool _disposed;

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>自定义游标/指示器（官方 2.10.5 cursor 插槽），未设置时使用默认 marker 竖线</summary>
        [Parameter]
        public RenderFragment CursorTemplate { get; set; }

        [Parameter]
        public string Container { get; set; }

        [Parameter]
        public int Offset { get; set; }

        [Parameter]
        public int Bound { get; set; } = 15;

        [Parameter]
        public int Duration { get; set; } = 300;

        [Parameter]
        public bool Marker { get; set; } = true;

        [Parameter]
        public AnchorType Type { get; set; }

        [Parameter]
        public AnchorDirection Direction { get; set; }

        /// <summary>
        /// 点击锚点链接时，是否把目标元素滚动到容器顶部（忽略 offset 定位），
        /// 等价于官方 2.10.5 的 <c>select-scroll-top</c>。默认 false。
        /// </summary>
        [Parameter]
        public bool SelectScrollTop { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        [Parameter]
        public string ModelValue { get; set; }

        [Parameter]
        public EventCallback<string> ModelValueChanged { get; set; }

        [Parameter]
        public string Value
        {
            get => ModelValue;
            set => ModelValue = value;
        }

        [Parameter]
        public EventCallback<string> ValueChanged
        {
            get => ModelValueChanged;
            set => ModelValueChanged = value;
        }

        [Parameter]
        public EventCallback<string> OnChange { get; set; }

        [Parameter]
        public EventCallback<AnchorClickEventArgs> OnClick { get; set; }

        public ElementReference RootElement { get; set; }

        /// <summary>当前激活的锚点 href（官方 exposes 之外的本库扩展，只读）</summary>
        public string ActiveHref => activeHref;

        /// <summary>
        /// 激活项变化通知。ElAnchorLink 的 <c>is-active</c> 读的是这里的父级状态，不是它自己的参数；
        /// 参数没变时 Blazor 会跳过子组件重渲染，于是点过的每一条都把 <c>is-active</c> 留在 DOM 上，
        /// 表现为「A、B、C 都点过 → 三个都是激活色」（官方是 <c>ns.is('active', currentAnchor === href)</c>，
        /// 任意时刻只有一条命中）。订阅口径与本仓 ElOption 的 HoverChanged / FilterChanged 一致。
        /// </summary>
        internal event Action ActiveChanged;

        /// <summary>
        /// 以编程方式滚动到指定 href 对应的锚点目标。
        /// 官方 <c>anchor.vue</c> expose 的 <c>scrollTo(href)</c> 是「先 setCurrentAnchor(href)，再滚动」
        /// （取证文件 Temp/ep2140-anchor.vue_vue_type_script_setup_true_lang.mjs 的 scrollTo），
        /// 所以这里同样要把激活项一起改过来，而不是只滚页面。
        /// </summary>
        public async Task ScrollToAsync(string href)
        {
            if (!string.IsNullOrWhiteSpace(href))
            {
                await SetActiveHrefAsync(href, true);
            }
            await JSRuntime.InvokeVoidAsync("elementAnchorScrollTo", href, Container, Offset, Duration, SelectScrollTop);
            await JSRuntime.InvokeVoidAsync("elementAnchorUpdateMarker", RootElement, href);
        }

        protected string AnchorClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-anchor", Cls)
            .Add($"el-anchor--{Direction.ToString().ToLowerInvariant()}")
            .AddIf(Type == AnchorType.Underline, "el-anchor--underline")
            .ToString();

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            if (!string.IsNullOrWhiteSpace(ModelValue) && activeHref != ModelValue)
            {
                activeHref = ModelValue;
                // 外部直接改 ModelValue（@bind-ModelValue / ModelValue="..."）也要把每一条链接刷一遍，
                // 否则链接组件参数没变不重渲染，旧的 is-active 还挂在 DOM 上。
                ActiveChanged?.Invoke();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            var nextOptionsKey = $"{Container}|{Offset}|{Bound}|{Duration}";
            if (optionsKey == nextOptionsKey)
            {
                return;
            }

            // 参数变化时先清理旧的 JS 监听
            if (!firstRender)
            {
                try { await JSRuntime.InvokeVoidAsync("elementAnchorDispose", RootElement); } catch { }
            }

            optionsKey = nextOptionsKey;
            objectReference ??= DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("elementAnchorInit", RootElement, objectReference, new
            {
                container = Container,
                offset = Offset,
                bound = Bound,
                duration = Duration
            });
        }

        internal bool IsActive(string href)
        {
            return !string.IsNullOrWhiteSpace(href) && href == activeHref;
        }

        internal async Task HandleLinkClickAsync(ElAnchorLink link, MouseEventArgs mouseEventArgs)
        {
            // 官方口径：click 事件先 emit，然后 scrollTo = setCurrentAnchor(href) + 滚动。
            // 激活项必须由点击直接定，不能等滚动落定后由 JS 回算：容器滚到底被 max 夹住时
            // 回算永远算不到被点的那条（实测 /anchor 的 #c-2 目标只能到 82px，阈值 offset+bound=75）。
            await SetActiveHrefAsync(link.Href, true);
            if (OnClick.HasDelegate)
            {
                await OnClick.InvokeAsync(new AnchorClickEventArgs
                {
                    Href = link.Href,
                    Title = link.EffectiveTitle,
                    MouseEventArgs = mouseEventArgs
                });
            }

            await JSRuntime.InvokeVoidAsync("elementAnchorScrollTo",
                link.Href, Container, link.EffectiveOffset ?? Offset, Duration, SelectScrollTop);
            await JSRuntime.InvokeVoidAsync("elementAnchorUpdateMarker", RootElement, link.Href);
        }

        [JSInvokable]
        public Task SetActiveHref(string href)
        {
            return SetActiveHrefAsync(href, true);
        }

        private async Task SetActiveHrefAsync(string href, bool notify)
        {
            if (activeHref == href)
            {
                return;
            }

            activeHref = href;
            ModelValue = href;
            ActiveChanged?.Invoke();
            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(href);
            }
            if (notify && OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(href);
            }
            await InvokeAsync(StateHasChanged);
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                await JSRuntime.InvokeVoidAsync("elementAnchorDispose", RootElement);
            }
            catch { }
            objectReference?.Dispose();
            objectReference = null;
        }
    }
}
