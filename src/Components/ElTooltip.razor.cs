using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElTooltip : ElementComponentBase, IDisposable
    {
        private static int nextId;
        private bool internalVisible;
        private CancellationTokenSource delayCancellationTokenSource;
        private CancellationTokenSource autoCloseCancellationTokenSource;
        private ElementReference triggerRef;
        private string fixedPopperStyle;
        private string fixedPlacement;

        [Parameter]
        public string Content { get; set; }

        [Parameter]
        public RenderFragment ContentTemplate { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public string Placement { get; set; } = "bottom";

        [Parameter]
        public PopperTrigger Trigger { get; set; } = PopperTrigger.Hover;

        [Parameter]
        public bool ModelValue { get; set; }

        [Parameter]
        public EventCallback<bool> ModelValueChanged { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public string Effect { get; set; } = "dark";

        [Parameter]
        public bool ShowArrow { get; set; } = true;

        [Parameter]
        public int OpenDelay { get; set; }

        [Parameter]
        public int HideAfter { get; set; }

        /// <summary>关闭延迟（HideAfter 别名）</summary>
        [Parameter]
        public int CloseDelay { get; set; }

        /// <summary>placement 偏移量（默认 8px）</summary>
        [Parameter]
        public int Offset { get; set; } = 8;

        /// <summary>鼠标移出后是否保持显示（需手动 HideAsync 关闭）</summary>
        [Parameter]
        public bool Persistent { get; set; }

        /// <summary>鼠标是否可进入 tooltip 浮层（EP enterable，默认 true；false 时移入触发器即关闭）</summary>
        [Parameter]
        public bool Enterable { get; set; } = true;

        /// <summary>
        /// 把浮层钉成 <c>position:fixed</c>，坐标取悬停时实测的触发元素视口矩形（<see cref="TextClip"/>）。
        /// 默认 <c>false</c>：absolute，行为与本参数加入前逐字节一致。
        /// <para>
        /// 为什么需要它：浮层渲染在触发 span <b>内部</b>（本仓没有 EP 那套 teleport 到 body 的传送门），
        /// 而 absolute 的裁剪来自「最近的非 visible overflow 祖先」那条链 —— 表格单元格是
        /// <c>.cell(overflow:hidden) → .el-table__body-wrapper(auto) → .el-table(hidden)</c>，
        /// 侧栏菜单是 <c>.demo-aside-scroll(auto)</c>。挂在这类盒子里的浮层一露头就被裁没，
        /// 表现为「hover 什么都不出来」。fixed 的包含块是视口，只要祖先没有
        /// <c>transform/filter/contain</c> 就不受 overflow 裁剪。
        /// </para>
        /// <para>
        /// 与 <see cref="Offset"/> 共用同一个间距；<c>Placement="top"</c> 时上方放不下会自动翻到下方，
        /// 翻面结果一并落到 <c>x-placement</c> 上，箭头跟着翻。JS 量不到（bUnit、元素已被移除）时
        /// 静默退回 absolute，不抛异常。
        /// </para>
        /// </summary>
        [Parameter]
        public bool FixedPopper { get; set; }

        /// <summary>显示后自动关闭的毫秒数（EP auto-close，0 = 不自动关闭）</summary>
        [Parameter]
        public int AutoClose { get; set; }

        [Parameter]
        public string PopperClass { get; set; }

        [Parameter]
        public string PopperStyle { get; set; }

        [Parameter]
        public EventCallback OnShow { get; set; }

        [Parameter]
        public EventCallback OnHide { get; set; }

        [Parameter]
        public EventCallback<bool> OnVisibleChange { get; set; }

        protected string PopperId { get; } = $"el-tooltip-{Interlocked.Increment(ref nextId)}";

        protected bool PreventContextMenu => Trigger == PopperTrigger.ContextMenu;

        protected bool ShouldShowPopper => !Disabled && (Trigger == PopperTrigger.Manual ? ModelValue : internalVisible);

        /// <summary>实际生效的 placement。<see cref="FixedPopper"/> 翻面时（上方放不下改下方）箭头要跟着翻，
        /// 所以 <c>x-placement</c> 读这一条而不是 <see cref="Placement"/>；未开 fixed 时两者恒等。</summary>
        protected string EffectivePlacement => fixedPlacement ?? Placement;

        protected string TriggerStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add("position:relative", "display:inline-block")
            .Add(Style)
            .ToString();

        protected string PopperClassValue => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-tooltip__popper", "el-popper")
            .AddIf(!string.IsNullOrWhiteSpace(Effect), $"is-{Effect}")
            .Add(PopperClass)
            .ToString();

        protected string PopperStyleValue => BuildPopperStyle();

        public Task ShowAsync()
        {
            return SetVisibleAsync(true);
        }

        public Task HideAsync()
        {
            return SetVisibleAsync(false);
        }

        // 触发器绑的是 mouseenter / mouseleave，不是 mouseover / mouseout：
        // 后者会冒泡，鼠标从触发按钮移进它自己的内层 <span>/图标 时也会触发一次 mouseout，
        // 而 HideAfter 默认 0 → 立刻隐藏并断开弹层，紧跟的那次 mouseover 又可能因为重渲染丢掉，
        // 实测就是「hover 没反应、只有点击才出得来」（与 ElInput / ElDropdown 修过的同一个根因）。
        protected Task OnMouseOverAsync(MouseEventArgs e)
        {
            if (Disabled || Trigger != PopperTrigger.Hover) return Task.CompletedTask;
            return SetVisibleAsync(true, OpenDelay);
        }

        protected Task OnMouseOutAsync(MouseEventArgs e)
        {
            if (Disabled || Trigger != PopperTrigger.Hover || Persistent) return Task.CompletedTask;
            var delay = Enterable ? (CloseDelay > 0 ? CloseDelay : HideAfter) : 0;
            return SetVisibleAsync(false, delay);
        }

        /// <summary>
        /// 弹层是触发器 wrapper 的子节点，所以 mouseleave 天然覆盖"移进弹层"这段路（Enterable 默认 true）。
        /// Enterable=false 时要反过来：一旦进弹层就当作已经离开触发器，立即关闭。
        /// </summary>
        protected Task OnPopperMouseEnterAsync(MouseEventArgs e)
        {
            if (Disabled || Trigger != PopperTrigger.Hover || Persistent || Enterable) return Task.CompletedTask;
            return SetVisibleAsync(false);
        }

        protected Task OnClickAsync(MouseEventArgs e)
        {
            if (Disabled || Trigger != PopperTrigger.Click) return Task.CompletedTask;
            return SetVisibleAsync(!internalVisible);
        }

        protected Task OnFocusInAsync(FocusEventArgs e)
        {
            if (Disabled || Trigger != PopperTrigger.Focus) return Task.CompletedTask;
            return SetVisibleAsync(true, OpenDelay);
        }

        protected Task OnFocusOutAsync(FocusEventArgs e)
        {
            if (Disabled || Trigger != PopperTrigger.Focus || Persistent) return Task.CompletedTask;
            return SetVisibleAsync(false, CloseDelay > 0 ? CloseDelay : HideAfter);
        }

        protected Task OnContextMenuAsync(MouseEventArgs e)
        {
            if (Disabled || Trigger != PopperTrigger.ContextMenu) return Task.CompletedTask;
            return SetVisibleAsync(!internalVisible);
        }

        private async Task SetVisibleAsync(bool visible, int delay = 0)
        {
            if (Disabled)
            {
                visible = false;
            }

            // Delay CTS：每次调用都重建（用于 OpenDelay / CloseDelay）
            delayCancellationTokenSource?.Cancel();
            delayCancellationTokenSource?.Dispose();
            delayCancellationTokenSource = new CancellationTokenSource();
            var delayToken = delayCancellationTokenSource.Token;

            if (delay > 0)
            {
                try
                {
                    await Task.Delay(delay, delayToken);
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
                    // 状态没变：不重建 AutoClose CTS，避免鼠标微动不断重置定时器
                    return;
                }

                await UpdateFixedPopperAsync(visible);
                internalVisible = visible;
            }

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

            // EP auto-close：显示后到点自动关闭。用独立 CTS，避免被 delay 重建取消。
            if (visible && AutoClose > 0)
            {
                autoCloseCancellationTokenSource?.Cancel();
                autoCloseCancellationTokenSource?.Dispose();
                autoCloseCancellationTokenSource = new CancellationTokenSource();
                _ = ScheduleAutoCloseAsync(autoCloseCancellationTokenSource.Token);
            }
            else if (!visible)
            {
                autoCloseCancellationTokenSource?.Cancel();
                autoCloseCancellationTokenSource?.Dispose();
                autoCloseCancellationTokenSource = null;
            }
        }

        private async Task ScheduleAutoCloseAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(AutoClose, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
            // AutoClose 触发时强制关闭，即使鼠标还在上面（EP 行为）
            internalVisible = false;
            if (OnHide.HasDelegate) await OnHide.InvokeAsync(null);
            if (OnVisibleChange.HasDelegate) await OnVisibleChange.InvokeAsync(false);
            autoCloseCancellationTokenSource?.Dispose();
            autoCloseCancellationTokenSource = null;
            StateHasChanged();
        }

        private async Task UpdateFixedPopperAsync(bool visible)
        {
            if (!FixedPopper)
            {
                return;
            }

            if (!visible)
            {
                fixedPopperStyle = null;
                fixedPlacement = null;
                return;
            }

            fixedPopperStyle = null;
            fixedPlacement = null;
            TextClipState state;
            try
            {
                state = await TextClip.MeasureRectAsync(JSRuntime, triggerRef);
            }
            catch (Exception)
            {
                // 悬停与渲染之间元素被移除（表格刷新、行折叠、宿主整块重建）时 JS 互操作会抛；
                // 这里退回 absolute 让浮层照常出现，不能让一次悬停把整页渲染带崩。
                return;
            }

            if (state == null || (state.RectWidth <= 0 && state.RectHeight <= 0))
            {
                // bUnit 未打桩时拿到 default，等价于「量不到」，同样退回 absolute
                return;
            }

            fixedPopperStyle = TextClip.BuildFixedPopperStyle(state, Placement, out fixedPlacement, Offset);
        }

        private string BuildPopperStyle()
        {
            var builder = HtmlPropertyBuilder.CreateCssStyleBuilder()
                .Add("z-index:var(--el-index-popper, 2000)")
                .Add("min-width:max-content");

            if (FixedPopper && fixedPopperStyle != null)
            {
                // 不能再带 absolute 那套 top:100%/left:50% 锚点：它们是相对触发元素的，
                // fixed 的包含块是视口，留着会把浮层算到屏幕外。坐标整条由 fixedPopperStyle 给。
                builder.Add(fixedPopperStyle);
            }
            else
            {
                builder.Add("position:absolute").Add(GetPlacementStyle());
            }

            return builder.Add(PopperStyle).ToString();
        }

        private string GetPlacementStyle()
        {
            var placement = string.IsNullOrWhiteSpace(Placement) ? "bottom" : Placement.ToLowerInvariant();
            // Offset 必须真正插值：少了 $ 就会输出 "translate(-50%, -{Offset}px)" 这种
            // 非法值，浏览器把整条 transform 声明丢掉，popper 就贴回参考元素上。
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
            base.Dispose();
        }
    }
}
