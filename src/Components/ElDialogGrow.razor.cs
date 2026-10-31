using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace ElementPlus
{
    /// <summary>
    /// ElDialogGrow 是参照 Element Plus <c>el-dialog</c> 规范、从零自渲染的模态对话框组件。
    /// 不依赖任何第三方 UI 库（包括 Radzen），所有渲染、定位、拖拽、缩放、关闭逻辑均为本仓库内部实现。
    /// 通过 <c>Visible</c>（可配合 <c>@bind-Visible</c>）控制显隐，支持标题/内容/页脚插槽、
    /// 全屏、居中、拖拽、缩放、遮罩点击关闭、Esc 关闭、关闭前拦截、焦点陷阱等能力。
    ///
    /// 服务化用法（打开任意组件并回传结果）见 <see cref="ElDialogGrowService"/> 与
    /// <see cref="ElDialogGrowHost"/>；此时对话框内容通过级联 <see cref="ElDialogGrowRef"/> 拿到
    /// <c>CloseAsync(result)</c> 等能力。
    /// </summary>
    public partial class ElDialogGrow : ElementComponentBase
    {
        private ElementReference wrapperElement;
        private ElementReference headerElement;
        private ElementReference resizerElement;
        private ElementReference dialogElement;
        private bool hasOpenedRender;
        private bool resizeRegistered;
        private DotNetObjectReference<ElDialogGrow> dotNetRef;
        private ElDialogGrowRef dialogRef;

        // 拖拽状态：事件源在 JS 侧（document 级 mousemove），这里只保存偏移用于重渲染还原。
        private int dragOffsetX;
        private int dragOffsetY;
        private string dragTransform;
        private bool dragBound;

        /// <summary>是否显示对话框（与 <see cref="Visible"/> 同源，保留以兼容 <c>v-model</c> 风格）。</summary>
        [Parameter]
        public bool ModelValue { get; set; }

        /// <summary>是否显示对话框的双向绑定回调（与 <see cref="VisibleChanged"/> 同源）。</summary>
        [Parameter]
        public EventCallback<bool> ModelValueChanged { get; set; }

        /// <summary>是否显示对话框。推荐使用 <c>@bind-Visible</c> 进行双向绑定。</summary>
        [Parameter]
        public bool Visible
        {
            get => ModelValue;
            set => ModelValue = value;
        }

        /// <summary>是否显示对话框的双向绑定回调。</summary>
        [Parameter]
        public EventCallback<bool> VisibleChanged { get; set; }

        /// <summary>对话框标题。提供 <see cref="Header"/> 插槽时本项被忽略。</summary>
        [Parameter]
        public string Title { get; set; }

        /// <summary>对话框宽度，默认 <c>50%</c>。全屏时忽略。</summary>
        [Parameter]
        public string Width { get; set; } = "50%";

        /// <summary>对话框高度，默认空（自适应内容）。全屏时忽略。</summary>
        [Parameter]
        public string Height { get; set; }

        /// <summary>是否为全屏对话框。</summary>
        [Parameter]
        public bool Fullscreen { get; set; }

        /// <summary>对话框距顶部距离，默认 <c>15vh</c>。全屏或竖直居中（<see cref="AlignCenter"/>）时忽略。</summary>
        [Parameter]
        public string Top { get; set; } = "15vh";

        /// <summary>是否显示遮罩层，默认 <c>true</c>。</summary>
        [Parameter]
        public bool Modal { get; set; } = true;

        /// <summary>附加到遮罩层的自定义类名。</summary>
        [Parameter]
        public string ModalClass { get; set; }

        /// <summary>附加到全屏包装层的自定义类名。</summary>
        [Parameter]
        public string WrapperClass { get; set; }

        /// <summary>附加到正文容器的自定义类名。</summary>
        [Parameter]
        public string ContentClass { get; set; }

        /// <summary>
        /// 附加到正文容器（<c>.el-dialog__body</c>）的自定义内联样式，
        /// 用于自定义正文相对标题的位置与间距（正文默认内边距来自 EP 原生 30px 20px）。
        /// </summary>
        [Parameter]
        public string ContentStyle { get; set; }

        /// <summary>点击遮罩是否关闭对话框，默认 <c>true</c>。</summary>
        [Parameter]
        public bool CloseOnClickModal { get; set; } = true;

        /// <summary>按下 Esc 键是否关闭对话框，默认 <c>true</c>。</summary>
        [Parameter]
        public bool CloseOnPressEscape { get; set; } = true;

        /// <summary>是否显示标题栏（含默认标题与右上角关闭按钮），默认 <c>true</c>。</summary>
        [Parameter]
        public bool ShowTitle { get; set; } = true;

        /// <summary>是否显示右上角关闭按钮，默认 <c>true</c>。</summary>
        [Parameter]
        public bool ShowClose { get; set; } = true;

        /// <summary>是否让标题与页脚水平居中。</summary>
        [Parameter]
        public bool Center { get; set; }

        /// <summary>是否在视口中竖直居中（覆盖 <see cref="Top"/>）。</summary>
        [Parameter]
        public bool AlignCenter { get; set; }

        /// <summary>是否允许通过拖拽标题栏移动对话框。</summary>
        [Parameter]
        public bool Draggable { get; set; }

        /// <summary>是否显示右下角缩放手柄（对齐 Element Plus 2.x <c>resizable</c>）。</summary>
        [Parameter]
        public bool Resizable { get; set; }

        /// <summary>缩放时的最小宽度（像素）。</summary>
        [Parameter]
        public int ResizeMinWidth { get; set; } = 120;

        /// <summary>缩放时的最小高度（像素）。</summary>
        [Parameter]
        public int ResizeMinHeight { get; set; } = 80;

        /// <summary>缩放过程中触发（参数为当前像素宽高）。</summary>
        [Parameter]
        public EventCallback<ElDialogGrowSize> OnResize { get; set; }

        /// <summary>关闭时是否销毁内部内容（再次打开时重新渲染），默认 <c>false</c>。</summary>
        [Parameter]
        public bool DestroyOnClose { get; set; }

        /// <summary>显示时是否锁定 body 滚动，默认 <c>true</c>。</summary>
        [Parameter]
        public bool LockScroll { get; set; } = true;

        /// <summary>
        /// 是否渲染到 body 根部。本组件通过 <c>position:fixed</c> 定位，天然处于视口层级，
        /// 因此该参数仅用于 API 对齐，实际始终按视口定位渲染。
        /// </summary>
        [Parameter]
        public bool AppendToBody { get; set; } = true;

        /// <summary>自定义层级 <c>z-index</c>；未指定时由组件内部递增分配。</summary>
        [Parameter]
        public int? ZIndex { get; set; }

        /// <summary>ARIA role，默认 <c>dialog</c>；确认类对话框可传 <c>alertdialog</c>。</summary>
        [Parameter]
        public string Role { get; set; }

        /// <summary>无标题时的 aria-label；留空则回退到 <see cref="Title"/>。</summary>
        [Parameter]
        public string AriaLabel { get; set; }

        /// <summary>关闭按钮的 aria-label，默认 <c>Close</c>。</summary>
        [Parameter]
        public string CloseAriaLabel { get; set; }

        /// <summary>关闭按钮 tabindex，默认 <c>0</c>（可 Tab 到）。</summary>
        [Parameter]
        public int CloseTabIndex { get; set; }

        /// <summary>打开后是否自动聚焦第一个可聚焦元素，默认 <c>false</c>。</summary>
        [Parameter]
        public bool AutoFocusFirstElement { get; set; }

        /// <summary>是否把 Tab / Shift+Tab 焦点锁在对话框内，默认 <c>false</c>。</summary>
        [Parameter]
        public bool FocusTrap { get; set; }

        /// <summary>
        /// 对话框级联引用。设置后本组件会把自己的 <see cref="ElDialogGrowRef"/> 写进去，
        /// 使正文内容可以 <c>CloseAsync(result)</c> / 改标题 / 触发重渲染（服务化打开时由宿主注入）。
        /// </summary>
        [Parameter]
        public ElDialogGrowRef DialogRef { get; set; }

        /// <summary>
        /// 服务化打开时的选项对象。传入后，级联引用 <see cref="ElDialogGrowRef"/> 的
        /// <c>Title</c> / <c>Width</c> / <c>Height</c> 赋值会直接落到本对象上（并触发宿主重渲染）。
        /// </summary>
        [Parameter]
        public ElDialogGrowOptions Options { get; set; }

        /// <summary>关闭前拦截回调；返回 <c>false</c> 可阻止关闭。</summary>
        [Parameter]
        public Func<Task<bool>> BeforeClose { get; set; }

        /// <summary>关闭前拦截事件；设置 <see cref="ElementClosingEventArgs{ElDialogGrow}.Cancel"/> 为 <c>true</c> 可阻止关闭。</summary>
        [Parameter]
        public EventCallback<ElementClosingEventArgs<ElDialogGrow>> OnBeforeClose { get; set; }

        /// <summary>标题栏插槽。提供后覆盖 <see cref="Title"/> 的默认渲染。</summary>
        [Parameter]
        public RenderFragment Header { get; set; }

        /// <summary>对话框正文插槽。</summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>对话框页脚插槽（如操作按钮）。</summary>
        [Parameter]
        public RenderFragment Footer { get; set; }

        /// <summary>打开动画开始前触发。</summary>
        [Parameter]
        public EventCallback OnOpen { get; set; }

        /// <summary>打开动画结束后触发（首次渲染并聚焦后）。</summary>
        [Parameter]
        public EventCallback OnOpened { get; set; }

        /// <summary>关闭动画开始前触发。</summary>
        [Parameter]
        public EventCallback OnClose { get; set; }

        /// <summary>关闭动画结束后触发。</summary>
        [Parameter]
        public EventCallback OnClosed { get; set; }

        /// <summary>关闭并回传结果时触发（参数为内容组件回传的对象，可能为 <c>null</c>）。</summary>
        [Parameter]
        public EventCallback<object> OnCloseWithResult { get; set; }

        /// <summary>显隐状态变更时触发，参数为当前是否可见。</summary>
        [Parameter]
        public EventCallback<bool> OnVisibleChange { get; set; }

        private bool wasOpen;
        private string mutableTitle;
        private string mutableWidth;
        private string mutableHeight;
        private ElDialogGrowRef boundRef;

        /// <summary>
        /// 供级联引用 <see cref="ElDialogGrowRef"/> 改标题用（服务化 / 内联用法）。
        /// 非 <c>null</c> 时覆盖 <see cref="Title"/> 参数。
        /// </summary>
        internal string MutableTitle
        {
            get => mutableTitle ?? Title;
            set
            {
                mutableTitle = value;
                InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>供级联引用改宽度用（非 <c>null</c> 时覆盖 <see cref="Width"/>）。</summary>
        internal string MutableWidth
        {
            get => mutableWidth ?? Width;
            set
            {
                mutableWidth = value;
                InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>供级联引用改高度用（非 <c>null</c> 时覆盖 <see cref="Height"/>）。</summary>
        internal string MutableHeight
        {
            get => mutableHeight ?? Height;
            set
            {
                mutableHeight = value;
                InvokeAsync(StateHasChanged);
            }
        }

        protected int ResolvedZIndex { get; private set; }

        /// <summary>实际用于渲染的标题（级联引用改过 <see cref="MutableTitle"/> 时优先生效）。</summary>
        protected string EffectiveTitle => mutableTitle ?? Title;

        /// <summary>实际用于渲染的宽度。</summary>
        protected string EffectiveWidth => mutableWidth ?? Width;

        /// <summary>实际用于渲染的高度。</summary>
        protected string EffectiveHeight => mutableHeight ?? Height;
        protected int ResolvedModalZIndex => Modal ? ResolvedZIndex - 1 : ResolvedZIndex;

        /// <summary>
        /// 本对话框对外暴露的级联引用。优先复用调用方传入的 <see cref="DialogRef"/> 实例
        /// （服务化打开时由 <c>ElDialogGrowHostItem</c> 注入，内容组件拿到的就是它）。
        /// </summary>
        internal ElDialogGrowRef ActiveDialogRef
        {
            get
            {
                var reference = DialogRef ?? (dialogRef ??= CreateDialogRef());
                if (!ReferenceEquals(boundRef, reference))
                {
                    // 关键：把关闭 / 刷新 / 改属性的能力接到这个引用上。
                    // 否则宿主传进来的空 ref 会让内容组件的 CloseAsync 静默无效。
                    reference.Bind(this, Options);
                    boundRef = reference;
                }

                return reference;
            }
        }

        /// <summary>是否需要渲染对话框容器（关闭且 <see cref="DestroyOnClose"/> 为真时整体不渲染）。</summary>
        protected bool ShouldRenderDialog => ModelValue || !DestroyOnClose;

        /// <summary>是否渲染对话框内部内容（<see cref="DestroyOnClose"/> 且已关闭时不渲染）。</summary>
        protected bool ShowContent => !(DestroyOnClose && !ModelValue);

        /// <summary>以编程方式打开对话框。</summary>
        public async Task ShowAsync()
        {
            if (ModelValue)
            {
                return;
            }

            // 状态变更、StateHasChanged 与所有 EventCallback 都要在 Dispatcher 上跑：
            // 既支持真实 UI 事件调用，也支持测试线程直接 await 调用。
            await InvokeAsync(async () =>
            {
                ModelValue = true;
                wasOpen = true;
                StateHasChanged();

                if (VisibleChanged.HasDelegate)
                {
                    await VisibleChanged.InvokeAsync(true);
                }

                if (ModelValueChanged.HasDelegate)
                {
                    await ModelValueChanged.InvokeAsync(true);
                }

                await NotifyOpeningAsync();
            });
        }

        /// <summary>以编程方式关闭对话框（会经过 <see cref="BeforeClose"/> / <see cref="OnBeforeClose"/> 拦截）。</summary>
        public Task CloseAsync(MouseEventArgs e = null)
        {
            return CloseCoreAsync(null);
        }

        /// <summary>关闭对话框并回传结果（服务化打开的返回值来源）。</summary>
        public Task CloseAsync(object result)
        {
            return CloseCoreAsync(result);
        }

        /// <summary>关闭对话框并回传强类型结果。</summary>
        public Task CloseAsync<T>(T result)
        {
            return CloseCoreAsync(result);
        }

        /// <summary>
        /// 绕过 <see cref="BeforeClose"/> / <see cref="OnBeforeClose"/> 拦截的强制关闭。
        /// 供服务化用法里 <see cref="ElDialogGrowService.Close"/> 的编程式关闭使用
        /// （对齐 Radzen：<c>DialogService.Close()</c> 不受内容组件的 CanClose 限制），
        /// 其余流程（过渡、解锁滚动、回传结果）与普通关闭完全一致。
        /// </summary>
        public Task ForceCloseAsync(object result = null)
        {
            return CloseCoreAsync(result, skipCloseGuard: true);
        }

        private Task CloseCoreAsync(object result)
        {
            return CloseCoreAsync(result, skipCloseGuard: false);
        }

        private async Task CloseCoreAsync(object result, bool skipCloseGuard)
        {
            if (!ModelValue)
            {
                return;
            }

            if (!skipCloseGuard && !await CanCloseAsync())
            {
                return;
            }

            // 整个关闭流程（含所有 EventCallback）都必须跑在 Dispatcher 上下文：
            // 回调接收方是组件，InvokeAsync 内部会触发它的 StateHasChanged；
            // 若从测试线程 / 事件线程直接调 CloseAsync，会抛
            //「The current thread is not associated with the Dispatcher」。
            await InvokeAsync(async () =>
            {
                ModelValue = false;
                wasOpen = false;
                StateHasChanged();

                if (VisibleChanged.HasDelegate)
                {
                    await VisibleChanged.InvokeAsync(false);
                }

                if (ModelValueChanged.HasDelegate)
                {
                    await ModelValueChanged.InvokeAsync(false);
                }

                await NotifyClosingAsync();

                // 先触发结果回调（宿主据此从渲染列表摘除本对话框），再抛 OnClosed ——
                // 否则 await OpenAsync(...) 的续体可能看到对话框还挂在页面上。
                if (OnCloseWithResult.HasDelegate)
                {
                    await OnCloseWithResult.InvokeAsync(result);
                }
            });
        }

        /// <inheritdoc />
        protected override async Task OnParametersSetAsync()
        {
            if (ModelValue && !wasOpen)
            {
                wasOpen = true;
                hasOpenedRender = false;
                ResolvedZIndex = ZIndex ?? ElPopup.AllocateOverlayZIndex() + 1 + ConfigZIndex - ElementConfig.DefaultZIndex;
                await NotifyOpeningAsync();
            }
            else if (!ModelValue && wasOpen)
            {
                wasOpen = false;
                await NotifyClosingAsync();
            }
        }

        /// <summary>打开时的副作用：锁定滚动、触发 <see cref="OnOpen"/> 与 <see cref="OnVisibleChange"/>。
        /// 既供 <see cref="OnParametersSetAsync"/> 在参数驱动打开时调用，也供 <see cref="ShowAsync"/> 编程打开时调用。</summary>
        private async Task NotifyOpeningAsync()
        {
            // 每次打开都清空「运行期改过的尺寸与位置」，回到参数（或选项）定义的值：
            // 同一个组件实例第二次打开（声明式 keep-alive、服务化复用宿主项）不该带着
            // 上一次缩放/拖拽的结果。
            mutableWidth = null;
            mutableHeight = null;
            dragTransform = null;
            dragOffsetX = 0;
            dragOffsetY = 0;

            if (LockScroll && Modal)
            {
                await JSRuntime.InvokeVoidAsync("elementLockBodyScroll", true);
            }

            if (OnOpen.HasDelegate)
            {
                await OnOpen.InvokeAsync();
            }

            if (OnVisibleChange.HasDelegate)
            {
                await OnVisibleChange.InvokeAsync(true);
            }
        }

        /// <summary>关闭时的副作用：解锁滚动、触发 <see cref="OnClose"/> / <see cref="OnVisibleChange"/> / <see cref="OnClosed"/>。
        /// 既供 <see cref="OnParametersSetAsync"/> 在参数驱动关闭时调用，也供 <c>CloseAsync</c> 编程关闭时调用。</summary>
        private async Task NotifyClosingAsync()
        {
            if (LockScroll && Modal)
            {
                await JSRuntime.InvokeVoidAsync("elementLockBodyScroll", false);
            }

            if (OnClose.HasDelegate)
            {
                await OnClose.InvokeAsync();
            }

            if (OnVisibleChange.HasDelegate)
            {
                await OnVisibleChange.InvokeAsync(false);
            }

            if (OnClosed.HasDelegate)
            {
                await OnClosed.InvokeAsync();
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (ShouldRenderDialog && Resizable && !resizeRegistered)
            {
                await RegisterResizerAsync();
            }
            else if ((!Resizable || !ShouldRenderDialog) && resizeRegistered)
            {
                await UnregisterResizerAsync();
            }

            await SyncDraggableAsync();

            if (FocusTrap)
            {
                await UpdateFocusTrapAsync();
            }

            if (!ModelValue || hasOpenedRender)
            {
                return;
            }

            hasOpenedRender = true;

            if (AutoFocusFirstElement)
            {
                // 对齐 Radzen 的 AutoFocusFirstElement：优先落到第一个可聚焦控件（表单对话框体验更好）。
                var focused = false;
                try
                {
                    focused = await JSRuntime.InvokeAsync<bool>("elementDialogFocusFirst", dialogElement);
                }
                catch (Exception)
                {
                    // 无 JS 环境时退回聚焦包装层
                }

                if (!focused)
                {
                    await wrapperElement.Dom(JSRuntime).FocusAsync();
                }
            }
            else
            {
                await wrapperElement.Dom(JSRuntime).FocusAsync();
            }

            if (OnOpened.HasDelegate)
            {
                await OnOpened.InvokeAsync();
            }
        }

        private bool focusTrapRegistered;

        private async Task UpdateFocusTrapAsync()
        {
            var shouldRegister = FocusTrap && ModelValue;
            if (shouldRegister == focusTrapRegistered)
            {
                return;
            }

            try
            {
                if (shouldRegister)
                {
                    dotNetRef ??= DotNetObjectReference.Create(this);
                    await JSRuntime.InvokeVoidAsync("elementDialogBindFocusTrap", dialogElement, dotNetRef);
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("elementDialogUnbindFocusTrap", dialogElement);
                }

                focusTrapRegistered = shouldRegister;
            }
            catch (Exception)
            {
                // 无 JS 环境（如 bUnit 单元测试）静默降级
            }
        }

        private async Task RegisterResizerAsync()
        {
            dotNetRef ??= DotNetObjectReference.Create(this);
            try
            {
                await JSRuntime.InvokeVoidAsync("elementDialogEnableResize", dialogElement, resizerElement, dotNetRef);
                resizeRegistered = true;
            }
            catch (Exception)
            {
                // 无效interop 时静默降级
            }
        }

        private async Task UnregisterResizerAsync()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("elementDialogDisableResize", resizerElement);
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

        /// <summary>缩放过程中由 JS 回调（每次 mousemove）。</summary>
        [JSInvokable]
        public async Task JsResize(double width, double height)
        {
            if (!ModelValue)
            {
                return;
            }

            // 写 internal 可变字段而非 Width/Height 参数：
            // ① 直接覆盖 [Parameter] 会被渲染器当成「父组件未更新」，父组件稍后重渲染
            //    （典型：内容组件改了标题 → 宿主刷新）时会把 Width 重新赋回原值 → 缩放白做；
            // ② MutableWidth/Height 优先级高于参数（EffectiveWidth => mutableWidth ?? Width），
            //    缩放结果因此能稳定保留到对话框关闭。
            MutableWidth = $"{Math.Round(width)}px";
            MutableHeight = $"{Math.Round(height)}px";

            if (OnResize.HasDelegate)
            {
                await OnResize.InvokeAsync(new ElDialogGrowSize(width, height));
            }
        }

        /// <summary>点击遮罩层关闭。</summary>
        protected async Task CloseOnModalClickAsync(MouseEventArgs e)
        {
            if (!CloseOnClickModal)
            {
                return;
            }

            await CloseAsync(e);
        }

        /// <summary>按下 Esc 键关闭。</summary>
        protected async Task OnKeyDownAsync(KeyboardEventArgs e)
        {
            if (!CloseOnPressEscape || e.Key != "Escape")
            {
                return;
            }

            await CloseAsync();
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

            var args = new ElementClosingEventArgs<ElDialogGrow> { Target = this };
            await OnBeforeClose.InvokeAsync(args);
            return !args.Cancel;
        }

        private ElDialogGrowRef CreateDialogRef()
        {
            var reference = new ElDialogGrowRef();
            reference.Bind(this, Options);
            boundRef = reference;
            return reference;
        }

        // ---- 拖拽（事件源在 JS 侧：document 级 mousemove，见 dom.js 的 elementDialogEnableDrag） ----

        /// <summary>
        /// 拖拽偏移回写（由 JS 节流调用）。刻意**不**触发重渲染：拖拽过程由 JS 直接改 DOM 的
        /// transform 保证跟手，这里只保存偏移，供下一次因其它原因渲染时还原位置。
        /// </summary>
        [JSInvokable]
        public Task JsDialogDrag(int offsetX, int offsetY)
        {
            dragOffsetX = offsetX;
            dragOffsetY = offsetY;
            dragTransform = $"translate({offsetX}px, {offsetY}px)";
            return Task.CompletedTask;
        }

        /// <summary>按 <see cref="Draggable"/> 与打开状态同步拖拽绑定（幂等）。</summary>
        private async Task SyncDraggableAsync()
        {
            var wanted = ModelValue && Draggable;
            if (wanted == dragBound)
            {
                return;
            }

            try
            {
                if (wanted)
                {
                    dotNetRef ??= DotNetObjectReference.Create(this);
                    await JSRuntime.InvokeVoidAsync("elementDialogEnableDrag", dialogElement, dotNetRef);
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("elementDialogDisableDrag", dialogElement);
                }
            }
            catch (Exception)
            {
                // 无 JS 环境（如 bUnit 单元测试）或元素已摘除时静默降级
            }

            dragBound = wanted;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                base.Dispose(disposing);
                return;
            }

            dotNetRef?.Dispose();
            dotNetRef = null;
            dragBound = false;

            base.Dispose(disposing);
        }
    }
}