using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElPopover : ElementComponentBase, IDisposable
    {
        private static int nextId;
        private bool internalVisible;
        private CancellationTokenSource delayCancellationTokenSource;
        private ElementReference wrapperRef;
        private ElementReference referenceRef;
        private string fixedPopperStyle;
        private string fixedPlacement;
        private DotNetObjectReference<ElPopover> outsideRef;
        private bool outsideBound;

        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public RenderFragment TitleContent { get; set; }

        [Parameter]
        public string Content { get; set; }

        [Parameter]
        public RenderFragment ContentTemplate { get; set; }

        [Parameter]
        public RenderFragment Footer { get; set; }

        [Parameter]
        public RenderFragment Reference { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public string Placement { get; set; } = "bottom";

        /// <summary>触发方式。EP 口径：默认 hover（tooltip/src/trigger2.mjs 里 trigger 的 default 就是 "hover"）。</summary>
        [Parameter]
        public PopperTrigger Trigger { get; set; } = PopperTrigger.Hover;

        [Parameter]
        public bool ModelValue { get; set; }

        [Parameter]
        public EventCallback<bool> ModelValueChanged { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public int Width { get; set; } = 150;

        [Parameter]
        public bool ShowArrow { get; set; } = true;

        /// <summary>tooltip 主题（EP effect，默认 light；dark 为深色）</summary>
        [Parameter]
        public string Effect { get; set; } = "light";

        [Parameter]
        public int OpenDelay { get; set; }

        [Parameter]
        public int HideAfter { get; set; } = 200;

        /// <summary>关闭延迟（HideAfter 别名）</summary>
        [Parameter]
        public int CloseDelay { get; set; }

        /// <summary>placement 偏移量（默认 8px）</summary>
        [Parameter]
        public int Offset { get; set; } = 8;

        /// <summary>
        /// 把面板钉成 <c>position:fixed</c>，坐标取打开时实测的参考元素视口矩形（<see cref="TextClip"/>）。
        /// 默认 <c>false</c>：absolute，行为与本参数加入前逐字节一致。
        /// <para>
        /// 为什么需要它：与 <see cref="ElTooltip.FixedPopper"/>（批次㊶）同一个根因——本库的面板内联在
        /// <c>.el-popover__reference-wrapper</c> 里，absolute 的裁剪来自「最近的非 visible overflow 祖先」那条链。
        /// 浏览器实测（对话框里的「操作说明」面板）：面板矩形 936,108..1282,260，而裁剪层
        /// <c>.el-tabs__content</c>(overflow:hidden) 的上缘是 253 ⇒ 152px 高只剩 7px，
        /// <c>elementFromPoint(面板中心)</c> 命中的是 <c>.el-dialog__header</c> 而不是面板。
        /// 页签容器、表格 <c>.cell</c>、滚动正文都是这类盒子；<c>ZIndex</c> 救不了裁剪。
        /// fixed 的包含块是视口，只要祖先没有 <c>transform/filter/contain</c> 就不被裁。
        /// </para>
        /// <para>
        /// 与 <see cref="Offset"/> 共用同一个间距；<c>Placement="top"</c> 时上方放不下会自动翻到下方，
        /// 翻面结果一并落到 <c>x-placement</c> 上，箭头跟着翻。JS 量不到（bUnit、元素已被移除）时
        /// 静默退回 absolute，不抛异常。水平方向不做视口夹取（与批次㊶ 同口径，见 docs/popover-fixed-popper.md）。
        /// </para>
        /// </summary>
        [Parameter]
        public bool FixedPopper { get; set; }

        /// <summary>鼠标移出后是否保持显示（需手动 HideAsync 关闭）</summary>
        [Parameter]
        public bool Persistent { get; set; }

        [Parameter]
        public string PopperClass { get; set; }

        [Parameter]
        public string PopperStyle { get; set; }

        [Parameter]
        public string Role { get; set; } = "tooltip";

        [Parameter]
        public int Tabindex { get; set; }

        [Parameter]
        public EventCallback OnShow { get; set; }

        [Parameter]
        public EventCallback OnHide { get; set; }

        [Parameter]
        public EventCallback<bool> OnVisibleChange { get; set; }

        protected string PopoverId { get; } = $"el-popover-{Interlocked.Increment(ref nextId)}";

        protected bool PreventContextMenu => Trigger == PopperTrigger.ContextMenu;

        protected bool ShouldShowPopover => !Disabled && (Trigger == PopperTrigger.Manual ? ModelValue : internalVisible);

        /// <summary>实际生效的 placement。<see cref="FixedPopper"/> 翻面时（上方放不下改下方）箭头要跟着翻，
        /// 所以 <c>x-placement</c> 读这一条而不是 <see cref="Placement"/>；未开 fixed 时两者恒等。</summary>
        protected string EffectivePlacement => fixedPlacement ?? Placement;

        protected string ReferenceStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add("position:relative", "display:inline-block")
            .Add(Style)
            .ToString();

        protected string PopoverClassValue => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-popover", "el-popper")
            .AddIf(!string.IsNullOrWhiteSpace(Effect), $"is-{Effect}")
            .Add(PopperClass)
            .Add(Cls)
            .ToString();

        protected string PopoverStyleValue => BuildPopoverStyle();

        private string BuildPopoverStyle()
        {
            var builder = HtmlPropertyBuilder.CreateCssStyleBuilder()
                .Add("z-index:var(--el-index-popper, 2000)")
                .AddIf(Width > 0, $"width:{Width}px");

            if (FixedPopper && fixedPopperStyle != null)
            {
                // 不能再拼 absolute 那套相对锚点（top:100%/bottom:100%/left:50%）：fixed 的包含块是视口，
                // 留着会把面板算到屏幕外。坐标整条由 fixedPopperStyle 给（自带 position:fixed）。
                builder.Add(fixedPopperStyle);
            }
            else
            {
                builder.Add("position:absolute").Add(GetPlacementStyle());
            }

            return builder.Add(PopperStyle).ToString();
        }

        /// <summary>
        /// 打开前量一次参考元素的视口矩形，算出 fixed 坐标与翻面结果。
        /// 与 <see cref="ElTooltip"/> 的同一逻辑同口径（批次㊶ 落地的 <see cref="TextClip"/> 原语）。
        /// </summary>
        private async Task UpdateFixedPopperAsync(bool visible)
        {
            if (!FixedPopper)
            {
                return;
            }

            fixedPopperStyle = null;
            fixedPlacement = null;
            if (!visible)
            {
                return;
            }

            TextClipState state;
            try
            {
                state = await TextClip.MeasureRectAsync(JSRuntime, referenceRef);
            }
            catch (Exception)
            {
                // 点击与渲染之间元素被移除（对话框关闭、宿主整块重建）时 JS 互操作会抛；
                // 这里退回 absolute 让面板照常出现，不能让一次点击把整页渲染带崩。
                return;
            }

            if (state == null || (state.RectWidth <= 0 && state.RectHeight <= 0))
            {
                // bUnit 未打桩时拿到 default，等价于「量不到」，同样退回 absolute
                return;
            }

            fixedPopperStyle = TextClip.BuildFixedPopperStyle(state, Placement, out fixedPlacement, Offset);
        }

        public Task ShowAsync()
        {
            return SetVisibleAsync(true);
        }

        public Task HideAsync()
        {
            return SetVisibleAsync(false);
        }

        protected Task OnMouseOverAsync(MouseEventArgs e)
        {
            return Trigger == PopperTrigger.Hover ? SetVisibleAsync(true, OpenDelay) : Task.CompletedTask;
        }

        protected Task OnMouseOutAsync(MouseEventArgs e)
        {
            if (Trigger == PopperTrigger.Hover && !Persistent)
            {
                return SetVisibleAsync(false, CloseDelay > 0 ? CloseDelay : HideAfter);
            }
            return Task.CompletedTask;
        }

        protected Task OnClickAsync(MouseEventArgs e)
        {
            return Trigger == PopperTrigger.Click ? SetVisibleAsync(!internalVisible) : Task.CompletedTask;
        }

        protected Task OnFocusInAsync(FocusEventArgs e)
        {
            return Trigger == PopperTrigger.Focus ? SetVisibleAsync(true, OpenDelay) : Task.CompletedTask;
        }

        protected Task OnFocusOutAsync(FocusEventArgs e)
        {
            if (Trigger == PopperTrigger.Focus && !Persistent)
            {
                return SetVisibleAsync(false, CloseDelay > 0 ? CloseDelay : HideAfter);
            }
            return Task.CompletedTask;
        }

        protected Task OnContextMenuAsync(MouseEventArgs e)
        {
            return Trigger == PopperTrigger.ContextMenu ? SetVisibleAsync(!internalVisible) : Task.CompletedTask;
        }

        protected async Task SetVisibleAsync(bool visible, int delay = 0)
        {
            if (Disabled)
            {
                visible = false;
            }

            delayCancellationTokenSource?.Cancel();
            delayCancellationTokenSource?.Dispose();
            delayCancellationTokenSource = new CancellationTokenSource();
            var token = delayCancellationTokenSource.Token;

            if (delay > 0)
            {
                try
                {
                    await Task.Delay(delay, token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }

            if (Trigger == PopperTrigger.Manual)
            {
                await UpdateFixedPopperAsync(visible);
                if (ModelValueChanged.HasDelegate)
                {
                    await ModelValueChanged.InvokeAsync(visible);
                }
            }
            else
            {
                if (internalVisible == visible)
                {
                    return;
                }

                await UpdateFixedPopperAsync(visible);
                internalVisible = visible;
            }

            await UpdateOutsideBindingAsync();

            if (visible && OnShow.HasDelegate)
            {
                await OnShow.InvokeAsync(null);
            }
            if (!visible && OnHide.HasDelegate)
            {
                await OnHide.InvokeAsync(null);
            }
            if (OnVisibleChange.HasDelegate)
            {
                await OnVisibleChange.InvokeAsync(visible);
            }
        }

        /// <summary>
        /// EP 口径：tooltip/src/content2.mjs 在面板打开期间注册 onClickOutside，回调里
        /// 只在 <c>trigger !== "hover"</c> 时 onClose。本库面板内联在 wrapper 内，点面板
        /// 等于点 root 内部，助手会放过，所以点选项/按钮不会误关；点别处才收。
        /// 缺了这一段，click / focus 触发的面板就「能开不能关」——只能再点一次参考元素。
        /// </summary>
        private async Task UpdateOutsideBindingAsync()
        {
            if (wrapperRef.Equals(default(ElementReference)))
            {
                return;
            }

            var shouldBind = ShouldShowPopover
                && Trigger != PopperTrigger.Hover
                && Trigger != PopperTrigger.Manual;
            try
            {
                if (shouldBind && !outsideBound)
                {
                    outsideRef ??= DotNetObjectReference.Create(this);
                    await JSRuntime.InvokeVoidAsync(
                        "elementPickerOutsideAttach",
                        wrapperRef,
                        ".el-popover",
                        outsideRef,
                        nameof(CloseFromOutsideAsync));
                    outsideBound = true;
                }
                else if (!shouldBind && outsideBound)
                {
                    outsideBound = false;
                    await JSRuntime.InvokeVoidAsync("elementPickerOutsideDetach", wrapperRef);
                }
            }
            catch (Exception)
            {
                // 宿主没引 dom.js 时只是不能点外关闭，hover / click 开合本身不受影响
            }
        }

        /// <summary>document 捕获阶段收到「面板与参考元素之外」的 mousedown：收起面板（由 JS 回调）。</summary>
        [JSInvokable]
        public async Task CloseFromOutsideAsync()
        {
            if (Trigger == PopperTrigger.Hover || Trigger == PopperTrigger.Manual || !internalVisible)
            {
                return;
            }

            await SetVisibleAsync(false);
        }

        private string GetPlacementStyle()
        {
            var placement = string.IsNullOrWhiteSpace(Placement) ? "bottom" : Placement.ToLowerInvariant();
            // 同 ElTooltip：少了 $ 会输出非法 transform，整条声明被浏览器丢弃。
            if (placement.StartsWith("top", StringComparison.Ordinal))
            {
                return $"bottom:100%;left:50%;transform:translate(-50%, -{Offset}px)";
            }
            if (placement.StartsWith("left", StringComparison.Ordinal))
            {
                return $"right:100%;top:50%;transform:translate(-{Offset}px, -50%)";
            }
            if (placement.StartsWith("right", StringComparison.Ordinal))
            {
                return $"left:100%;top:50%;transform:translate({Offset}px, -50%)";
            }

            return $"top:100%;left:50%;transform:translate(-50%, {Offset}px)";
        }

        public override void Dispose()
        {
            delayCancellationTokenSource?.Cancel();
            delayCancellationTokenSource?.Dispose();
            if (outsideBound)
            {
                outsideBound = false;
                _ = JSRuntime.InvokeVoidAsync("elementPickerOutsideDetach", wrapperRef);
            }
            outsideRef?.Dispose();
            outsideRef = null;
            base.Dispose();
        }
    }
}
