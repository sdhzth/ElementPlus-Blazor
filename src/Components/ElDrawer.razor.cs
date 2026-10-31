using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElDrawer : ElementComponentBase, IAsyncDisposable
    {
        private bool wasOpen;
        private bool openedRendered;

        /// <summary>级联引用改过的标题；非 null 时覆盖 <see cref="Title"/> 参数。</summary>
        private string mutableTitle;

        private ElDrawerRef drawerRef;
        private ElDrawerRef boundRef;

        /// <summary>是否曾经打开过：对齐官方「内容懒渲染」语义，首次打开前不渲染内部内容。</summary>
        private bool everOpened;

        /// <summary>受 open-delay 控制的真实显示状态（ModelValue 变为 true 且延时到点后才为 true）。</summary>
        private bool visibleState;

        private CancellationTokenSource openDelayCts;

        private DotNetObjectReference<ElDrawer> dotNetRef;
        private ElementReference drawerElement;
        private ElementReference resizerElement;
        private bool resizeRegistered;
        private bool bodyAppended;
        private bool scrollLocked;

        /// <summary>拖拽调整大小后的像素尺寸；null 表示仍使用 <see cref="Size"/>。</summary>
        private int? resizedSize;

        /// <summary>aria-labelledby 用的标题元素 id，每个实例唯一。</summary>
        private readonly string titleElementId = "el-drawer-title-" + Guid.NewGuid().ToString("N");

        [Parameter]
        public bool ModelValue { get; set; }

        [Parameter]
        public EventCallback<bool> ModelValueChanged { get; set; }

        [Parameter]
        public bool Visible
        {
            get => ModelValue;
            set => ModelValue = value;
        }

        [Parameter]
        public EventCallback<bool> VisibleChanged { get; set; }

        [Parameter]
        public string Title { get; set; }

        /// <summary>
        /// 抽屉级联引用。设置后本组件会把自己的 <see cref="ElDrawerRef"/> 写进去，
        /// 使抽屉正文里渲染的任意组件（例如表单）可以动态改标题、关闭抽屉。
        /// </summary>
        [Parameter]
        public ElDrawerRef DrawerRef { get; set; }

        /// <summary>
        /// 实际用于渲染的标题。级联引用 <see cref="ElDrawerRef.Title"/> 改过之后优先生效，
        /// 因此「抽屉里打开的表单」可以随时改当前抽屉的标题。
        /// </summary>
        internal string EffectiveTitle => mutableTitle ?? Title;

        /// <summary>关闭按钮的可访问名称：词条 <c>el.drawer.close</c>，fallback 是改动前硬编码的 "Close"。</summary>
        private string CloseButtonAriaLabel => Lang.T("el.drawer.close", "Close");

        /// <summary>供级联引用改标题用（赋值后立即重渲染）。</summary>
        internal string MutableTitle
        {
            get => mutableTitle ?? Title;
            set
            {
                mutableTitle = value;
                InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// 本抽屉对外暴露的级联引用。优先复用调用方传入的 <see cref="DrawerRef"/> 实例，
        /// 否则内部自建一个（对象标识在组件生命周期内保持稳定，可安全级联下发）。
        /// </summary>
        internal ElDrawerRef ActiveDrawerRef
        {
            get
            {
                var reference = DrawerRef ?? (drawerRef ??= CreateDrawerRef());
                if (!ReferenceEquals(boundRef, reference))
                {
                    reference.Bind(this);
                    boundRef = reference;
                }

                return reference;
            }
        }

        private ElDrawerRef CreateDrawerRef()
        {
            var reference = new ElDrawerRef();
            reference.Bind(this);
            boundRef = reference;
            return reference;
        }

        [Parameter]
        public RenderFragment Header { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public RenderFragment Footer { get; set; }

        [Parameter]
        public DrawerDirection Direction { get; set; } = DrawerDirection.Rtl;

        [Parameter]
        public string Size { get; set; } = "30%";

        [Parameter]
        public bool WithHeader { get; set; } = true;

        [Parameter]
        public bool Modal { get; set; } = true;

        [Parameter]
        public bool AppendToBody { get; set; }

        [Parameter]
        public bool LockScroll { get; set; } = true;

        [Parameter]
        public bool CloseOnClickModal { get; set; } = true;

        [Parameter]
        public bool CloseOnPressEscape { get; set; } = true;

        [Parameter]
        public bool ShowClose { get; set; } = true;

        [Parameter]
        public bool DestroyOnClose { get; set; }

        [Parameter]
        public int? ZIndex { get; set; }

        [Parameter]
        public Func<Task<bool>> BeforeClose { get; set; }

        [Parameter]
        public EventCallback<ElementClosingEventArgs<ElDrawer>> OnBeforeClose { get; set; }

        [Parameter]
        public EventCallback OnOpen { get; set; }

        [Parameter]
        public EventCallback OnOpened { get; set; }

        [Parameter]
        public EventCallback OnClose { get; set; }

        [Parameter]
        public EventCallback OnClosed { get; set; }

        [Parameter]
        public EventCallback<bool> OnVisibleChange { get; set; }

        // ===== 对齐 Element Plus 官方 Drawer API =====

        /// <summary>body 部分自定义 class（官方 2.9.3）。</summary>
        [Parameter]
        public string BodyClass { get; set; }

        /// <summary>抽屉打开的延时时间，单位毫秒（官方 open-delay）。</summary>
        [Parameter]
        public int OpenDelay { get; set; }

        /// <summary>抽屉关闭的延时时间，单位毫秒（官方 close-delay）。</summary>
        [Parameter]
        public int CloseDelay { get; set; }

        /// <summary>是否允许穿透遮罩层，需配合 <see cref="Modal"/>=false 使用（官方 2.11.7）。</summary>
        [Parameter]
        public bool ModalPenetrable { get; set; }

        /// <summary>header 标题的 aria-level 值（官方 header-aria-level，默认 "2"）。</summary>
        [Parameter]
        public string HeaderAriaLevel { get; set; } = "2";

        /// <summary>为抽屉启用可调整大小的功能（官方 2.11.0）。</summary>
        [Parameter]
        public bool Resizable { get; set; }

        /// <summary>输入焦点聚焦到 Drawer 内容时的回调（官方 open-auto-focus）。</summary>
        [Parameter]
        public EventCallback OnOpenAutoFocus { get; set; }

        /// <summary>输入焦点从 Drawer 内容失焦时的回调（官方 close-auto-focus）。</summary>
        [Parameter]
        public EventCallback OnCloseAutoFocus { get; set; }

        /// <summary>开始调整大小时触发（官方 2.11.8，仅 <see cref="Resizable"/> 时触发），参数为像素尺寸。</summary>
        [Parameter]
        public EventCallback<int> OnResizeStart { get; set; }

        /// <summary>调整大小过程中触发，参数为像素尺寸。</summary>
        [Parameter]
        public EventCallback<int> OnResize { get; set; }

        /// <summary>调整大小结束时触发，参数为像素尺寸。</summary>
        [Parameter]
        public EventCallback<int> OnResizeEnd { get; set; }

        // ===== 自定义 class =====

        /// <summary>遮罩层自定义 class</summary>
        [Parameter]
        public string ModalClass { get; set; }

        /// <summary>抽屉容器自定义 class</summary>
        [Parameter]
        public string DrawerClassParam { get; set; }

        /// <summary>header 自定义 class</summary>
        [Parameter]
        public string HeaderClass { get; set; }

        /// <summary>footer 自定义 class</summary>
        [Parameter]
        public string FooterClass { get; set; }

        protected int ResolvedZIndex { get; private set; }

        /// <summary>
        /// 是否渲染容器：对齐官方「内容懒渲染」——首次打开之前不渲染内部内容；
        /// 打开过之后若未开启 DestroyOnClose 则保留（便于在抽屉里放表单/另一个 Razor 页面时保留状态）。
        /// </summary>
        protected bool ShouldRenderDrawer => everOpened && (ModelValue || !DestroyOnClose);

        private bool IsHorizontal => Direction == DrawerDirection.Rtl || Direction == DrawerDirection.Ltr;

        private string DirectionClass => Direction.ToString().ToLowerInvariant();

        /// <summary>实际显示状态（受 open-delay 影响）。</summary>
        private bool IsVisible => ModelValue && visibleState;

        private string WrapperClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-drawer__wrapper")
            .AddIf(IsVisible, "el-drawer__open")
            .Add(Cls)
            .ToString();

        private string WrapperStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add($"z-index:{ResolvedZIndex}")
            .AddIf(!IsVisible, "display:none")
            .ToString();

        private string ModalStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add($"z-index:{ResolvedZIndex - 1}")
            .AddIf(!IsVisible, "display:none")
            .AddIf(ModalPenetrable, "pointer-events:none")
            .ToString();

        private string DrawerClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-drawer", DirectionClass)
            .AddIf(Resizable, "is-resizable")
            .Add(DrawerClassParam)
            .ToString();

        private string DrawerStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(IsHorizontal, $"width:{EffectiveSize}")
            .AddIf(!IsHorizontal, $"height:{EffectiveSize}")
            .Add(Style)
            .ToString();

        private string EffectiveSize => resizedSize.HasValue ? resizedSize.Value + "px" : Size;

        /// <summary>body 区域是否渲染（懒渲染：首次打开后才渲染内容）。</summary>
        private bool ShouldRenderBody => ShouldRenderDrawer;

        protected override async Task OnParametersSetAsync()
        {
            if (ModelValue && !wasOpen)
            {
                wasOpen = true;
                everOpened = true;
                openedRendered = false;

                /* 每次打开都从 Title 参数重新开始：同一个实例第二次打开时不再带着
                   上一次（可能由抽屉内表单改过的）标题。 */
                mutableTitle = null;
                ResolvedZIndex = ZIndex ?? ElPopup.AllocateOverlayZIndex() + 1 + ConfigZIndex - ElementConfig.DefaultZIndex;

                if (OpenDelay > 0)
                {
                    visibleState = false;
                    BeginOpenDelay();
                }
                else
                {
                    visibleState = true;
                }

                if (OnOpen.HasDelegate)
                {
                    await OnOpen.InvokeAsync(null);
                }
                if (OnVisibleChange.HasDelegate)
                {
                    await OnVisibleChange.InvokeAsync(true);
                }
            }
            else if (!ModelValue)
            {
                wasOpen = false;
                visibleState = false;
                CancelOpenDelay();
            }

            await SyncBodyScrollAsync();
        }

        private void BeginOpenDelay()
        {
            CancelOpenDelay();
            openDelayCts = new CancellationTokenSource();
            var token = openDelayCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(OpenDelay, token);
                    if (token.IsCancellationRequested || !ModelValue)
                    {
                        return;
                    }

                    visibleState = true;
                    openedRendered = false;
                    await InvokeAsync(StateHasChanged);
                    await SyncBodyScrollAsync();
                }
                catch (TaskCanceledException)
                {
                    // 延迟未到即被关闭，忽略
                }
                catch (ObjectDisposedException)
                {
                    // 组件已释放，忽略
                }
            });
        }

        private void CancelOpenDelay()
        {
            if (openDelayCts == null)
            {
                return;
            }

            openDelayCts.Cancel();
            openDelayCts.Dispose();
            openDelayCts = null;
        }

        private async Task SyncBodyScrollAsync()
        {
            if (!LockScroll)
            {
                return;
            }

            var shouldLock = IsVisible;
            if (shouldLock == scrollLocked)
            {
                return;
            }

            try
            {
                await JSRuntime.InvokeVoidAsync("elementLockBodyScroll", shouldLock);
                scrollLocked = shouldLock;
            }
            catch (Exception)
            {
                // JS 不可用时降级：不做滚动锁定
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (ShouldRenderDrawer && AppendToBody && !bodyAppended)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("documentAppendChild", drawerElement);
                    bodyAppended = true;
                }
                catch (Exception)
                {
                    // 降级：保持在原位置渲染
                }
            }

            if (ShouldRenderDrawer && Resizable && !resizeRegistered)
            {
                await RegisterResizerAsync();
            }
            else if (!Resizable && resizeRegistered)
            {
                await UnregisterResizerAsync();
            }

            if (!IsVisible || openedRendered)
            {
                return;
            }

            openedRendered = true;

            // 对齐官方：打开后自动把焦点移入抽屉内容
            await TryFocusContentAsync();

            if (OnOpenAutoFocus.HasDelegate)
            {
                await OnOpenAutoFocus.InvokeAsync(null);
            }

            if (OnOpened.HasDelegate)
            {
                await OnOpened.InvokeAsync(null);
            }
        }

        private async Task TryFocusContentAsync()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("elementDrawerFocusContent", drawerElement);
            }
            catch (Exception)
            {
                // 无 JS 环境时不聚焦
            }
        }

        private async Task RegisterResizerAsync()
        {
            dotNetRef ??= DotNetObjectReference.Create(this);
            try
            {
                await JSRuntime.InvokeVoidAsync(
                    "elementDrawerEnableResize",
                    drawerElement,
                    resizerElement,
                    DirectionClass,
                    dotNetRef);
                resizeRegistered = true;
            }
            catch (Exception)
            {
                // 无效 interop 时静默降级
            }
        }

        private async Task UnregisterResizerAsync()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("elementDrawerDisableResize", resizerElement);
            }
            catch (Exception)
            {
                // 忽略
            }
            finally
            {
                resizeRegistered = false;
            }
        }

        [JSInvokable]
        public async Task JsResizeStart(int size)
        {
            resizedSize = size;
            if (OnResizeStart.HasDelegate)
            {
                await OnResizeStart.InvokeAsync(size);
            }
            await InvokeAsync(StateHasChanged);
        }

        [JSInvokable]
        public async Task JsResize(int size)
        {
            resizedSize = size;
            if (OnResize.HasDelegate)
            {
                await OnResize.InvokeAsync(size);
            }
            await InvokeAsync(StateHasChanged);
        }

        [JSInvokable]
        public async Task JsResizeEnd(int size)
        {
            resizedSize = size;
            if (OnResizeEnd.HasDelegate)
            {
                await OnResizeEnd.InvokeAsync(size);
            }
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// 关闭 Drawer（对应官方暴露的 handleClose）：会先通过 before-close 校验。
        /// </summary>
        public async Task HandleCloseAsync()
        {
            await CloseAsync();
        }

        private async Task CloseAsync()
        {
            if (!ModelValue || !await CanCloseAsync())
            {
                return;
            }

            if (CloseDelay > 0)
            {
                await Task.Delay(CloseDelay);
                if (!ModelValue)
                {
                    return;
                }
            }

            if (OnCloseAutoFocus.HasDelegate)
            {
                await OnCloseAutoFocus.InvokeAsync(null);
            }

            ModelValue = false;
            wasOpen = false;
            visibleState = false;
            CancelOpenDelay();

            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(false);
            }
            if (VisibleChanged.HasDelegate)
            {
                await VisibleChanged.InvokeAsync(false);
            }
            if (OnClose.HasDelegate)
            {
                await OnClose.InvokeAsync(null);
            }
            if (OnVisibleChange.HasDelegate)
            {
                await OnVisibleChange.InvokeAsync(false);
            }
            if (OnClosed.HasDelegate)
            {
                await OnClosed.InvokeAsync(null);
            }

            if (DestroyOnClose)
            {
                resizedSize = null;
            }

            await SyncBodyScrollAsync();
        }

        private async Task<bool> CanCloseAsync()
        {
            if (BeforeClose != null && !await BeforeClose())
            {
                return false;
            }

            if (!OnBeforeClose.HasDelegate)
            {
                return true;
            }

            var args = new ElementClosingEventArgs<ElDrawer> { Target = this };
            await OnBeforeClose.InvokeAsync(args);
            return !args.Cancel;
        }

        private async Task CloseOnModalClickAsync(MouseEventArgs e)
        {
            if (!CloseOnClickModal)
            {
                return;
            }

            await CloseAsync();
        }

        private async Task OnKeyDownAsync(KeyboardEventArgs e)
        {
            if (!CloseOnPressEscape || e.Key != "Escape")
            {
                return;
            }

            await CloseAsync();
        }

        public async ValueTask DisposeAsync()
        {
            CancelOpenDelay();

            if (resizeRegistered)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("elementDrawerDisableResize", resizerElement);
                }
                catch (Exception)
                {
                    // 忽略
                }
                resizeRegistered = false;
            }

            if (scrollLocked)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("elementLockBodyScroll", false);
                }
                catch (Exception)
                {
                    // 忽略
                }
                scrollLocked = false;
            }

            dotNetRef?.Dispose();
            dotNetRef = null;
        }
    }
}
