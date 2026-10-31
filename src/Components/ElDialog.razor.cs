using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElDialog : ElementComponentBase
    {
        private ElementReference wrapperElement;
        private ElementReference dialogElement;
        private DotNetObjectReference<ElDialog> dragDotNetRef;
        private bool dragBound;
        private int dragOffsetX;
        private int dragOffsetY;
        private bool hasOpenedRender;

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
        /// 关闭按钮的可访问名称。此前<b>完全没有 aria-label</b>（EP 的 dialog-content.vue 用的是
        /// <c>t('el.dialog.close')</c>），所以这里连 fallback 都给成 EP 的英文值 —— 这是补一个真实无障碍缺口，
        /// 不接词条时也会多出这个属性（纯新增，不改任何可见文字）。
        /// </summary>
        private string CloseButtonAriaLabel => Lang.T("el.dialog.close", "Close this dialog");

        [Parameter]
        public string Width { get; set; } = "50%";

        [Parameter]
        public bool Fullscreen { get; set; }

        [Parameter]
        public string Top { get; set; } = "15vh";

        [Parameter]
        public bool Modal { get; set; } = true;

        [Parameter]
        public bool CloseOnClickModal { get; set; } = true;

        [Parameter]
        public bool CloseOnPressEscape { get; set; } = true;

        [Parameter]
        public bool ShowClose { get; set; } = true;

        [Parameter]
        public bool Draggable { get; set; }

        /// <summary>对话框垂直居中（EP align-center，默认 false）</summary>
        [Parameter]
        public bool AlignCenter { get; set; }

        /// <summary>标题的 aria-level（EP header-aria-level，默认 2）</summary>
        [Parameter]
        public string HeaderAriaLevel { get; set; } = "2";

        [Parameter]
        public bool AppendToBody { get; set; }

        [Parameter]
        public bool LockScroll { get; set; } = true;

        [Parameter]
        public bool DestroyOnClose { get; set; }

        [Parameter]
        public int? ZIndex { get; set; }

        /// <summary>标题和底部按钮居中</summary>
        [Parameter]
        public bool Center { get; set; }

        /// <summary>遮罩层自定义 class</summary>
        [Parameter]
        public string ModalClass { get; set; }

        /// <summary>
        /// 正文容器（<c>.el-dialog__body</c>）自定义 class。
        /// 正文与标题的间距由 EP 原生的 <c>.el-dialog__body{padding:30px 20px}</c> 决定，
        /// 需要调整时用本参数或 <see cref="ContentStyle"/> 覆盖，不要改全局 CSS。
        /// </summary>
        [Parameter]
        public string ContentClass { get; set; }

        /// <summary>
        /// 正文容器（<c>.el-dialog__body</c>）自定义内联样式。
        /// 例：<c>ContentStyle="padding-top:0"</c> 让正文紧贴标题；
        /// <c>ContentStyle="min-height:240px;display:flex;align-items:center"</c> 让正文在正文区里垂直居中。
        /// </summary>
        [Parameter]
        public string ContentStyle { get; set; }

        [Parameter]
        public Func<Task<bool>> BeforeClose { get; set; }

        [Parameter]
        public EventCallback<ElementClosingEventArgs<ElDialog>> OnBeforeClose { get; set; }

        [Parameter]
        public RenderFragment Header { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public RenderFragment Footer { get; set; }

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

        private bool wasOpen;
        protected int ResolvedZIndex { get; private set; }
        protected int ResolvedModalZIndex => Modal ? ResolvedZIndex - 1 : ResolvedZIndex;

        protected bool ShouldRenderDialog => ModelValue || !DestroyOnClose;

        protected override async Task OnParametersSetAsync()
        {
            if (ModelValue && !wasOpen)
            {
                wasOpen = true;
                hasOpenedRender = false;
                // 每次打开都把拖拽位置归零（对齐 Element Plus 的 resetPosition）。
                dragOffsetX = 0;
                dragOffsetY = 0;
                ResolvedZIndex = ZIndex ?? ElPopup.AllocateOverlayZIndex() + 1 + ConfigZIndex - ElementConfig.DefaultZIndex;
                if (OnOpen.HasDelegate)
                {
                    await OnOpen.InvokeAsync();
                }
                if (OnVisibleChange.HasDelegate)
                {
                    await OnVisibleChange.InvokeAsync(true);
                }
            }
            else if (!ModelValue)
            {
                wasOpen = false;
            }

            if (LockScroll)
            {
                await JSRuntime.InvokeVoidAsync("elementLockBodyScroll", ModelValue);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            await SyncDraggableAsync();
            if (!ModelValue || hasOpenedRender)
            {
                return;
            }
            hasOpenedRender = true;
            await wrapperElement.Dom(JSRuntime).FocusAsync();
            if (OnOpened.HasDelegate)
            {
                await OnOpened.InvokeAsync();
            }
        }

        protected async Task CloseAsync(MouseEventArgs e = null)
        {
            if (!ModelValue)
            {
                return;
            }
            if (!await CanCloseAsync())
            {
                return;
            }
            ModelValue = false;
            wasOpen = false;
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

        protected async Task CloseOnModalClickAsync(MouseEventArgs e)
        {
            if (!CloseOnClickModal)
            {
                return;
            }
            await CloseAsync(e);
        }

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

            var args = new ElementClosingEventArgs<ElDialog> { Target = this };
            await OnBeforeClose.InvokeAsync(args);
            return !args.Cancel;
        }

        /// <summary>
        /// 拖拽偏移回写（由 JS 侧 <c>elementDialogEnableDrag</c> 节流调用，纯 JS 会话内的 1:1 拖动）。
        /// 刻意**不**触发重渲染：拖拽过程中重渲染会重建 <see cref="ChildContent"/>，
        /// 可能打断对话框里的表单输入；偏移值只在下次因其它原因渲染时随 style 一起落到 DOM，
        /// 位置因此同样不会丢。
        /// </summary>
        [JSInvokable]
        public Task JsDialogDrag(int offsetX, int offsetY)
        {
            dragOffsetX = offsetX;
            dragOffsetY = offsetY;
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
                    dragDotNetRef ??= DotNetObjectReference.Create(this);
                    await JSRuntime.InvokeVoidAsync("elementDialogEnableDrag", dialogElement, dragDotNetRef);
                }
                else if (dragDotNetRef != null)
                {
                    await JSRuntime.InvokeVoidAsync("elementDialogDisableDrag", dialogElement);
                }
            }
            catch (JSException)
            {
                /* 元素已随关闭被摘除（DestroyOnClose）时忽略 */
            }
            catch (InvalidOperationException)
            {
                /* 预渲染阶段无可用的 JS 运行时 */
            }

            dragBound = wanted;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                dragDotNetRef?.Dispose();
                dragDotNetRef = null;
                dragBound = false;
            }

            base.Dispose(disposing);
        }
    }
}
