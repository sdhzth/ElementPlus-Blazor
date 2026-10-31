using Microsoft.AspNetCore.Components;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 等待 / 处理中浮层（自渲染、零第三方依赖，按 Element Plus 观感渲染）。
    /// <para>
    /// 全屏半透明遮罩 + 居中卡片：Element Plus 风格的 SVG 旋转加载圈 + 标题 + 提示文字，
    /// 可带「取消」按钮。通过 <see cref="Visible"/>（可配合 <c>@bind-Visible</c>）或
    /// <see cref="Show"/> / <see cref="Hide"/> 控制显隐。
    /// </para>
    /// <para>
    /// 默认遮罩不响应点击（等待过程中不应被误关）；把 <see cref="MaskClosable"/> 设为 true 后，
    /// 点击遮罩等同于点击取消（先触发 <see cref="OnCancel"/> 再关闭）。
    /// </para>
    /// <para>
    /// <see cref="ShowDelay"/> 给「一闪而过」的请求消抖：设为 N 毫秒后，请求在 N 毫秒内结束就<b>整块不渲染</b>
    /// （遮罩、卡片、进场动画都不出现），只有真的等过 N 毫秒才浮出来。默认 0 = 请求即出，与本参数加入前完全一致。
    /// </para>
    /// <para>
    /// 外观可调：卡片圆角 <see cref="Round"/> / <see cref="RoundRadius"/>、卡片背景 <see cref="BackgroundColor"/>、
    /// 标题色 <see cref="TitleColor"/>、提示色 <see cref="MessageColor"/>、加载圈颜色 <see cref="SpinnerColor"/>、
    /// 加载圈尺寸 <see cref="SpinnerSize"/>；取消按钮本身就是一个 <see cref="ElButton"/>，
    /// 可设 <see cref="ShowCancelButton"/>（显隐）、<see cref="CancelText"/>（文字）、
    /// <see cref="CancelButtonType"/>（语义配色）、<see cref="CancelButtonSize"/>（尺寸）、
    /// <see cref="CancelButtonColor"/> / <see cref="CancelButtonDark"/>（自定义色与实心外观）、
    /// <see cref="CancelButtonPlain"/> / <see cref="CancelButtonRound"/>（朴素 / 圆角），
    /// 以及 <see cref="CancelButtonPosition"/>（左 / 中 / 右）。
    /// 颜色参数以 CSS 自定义变量（<c>--ewpg-title-color</c> 等）内联下发，样式真源仍是 <c>index.css</c>。
    /// </para>
    /// </summary>
    public partial class ElWaitingPopupGrow : ElementComponentBase
    {
        private bool _visible;
        private bool _lastNotifiedVisible;

        // _visible 记「有没有人请求显示」，_painted 才记「场上到底画没画」。
        // 两者分开是为了 ShowDelay：延迟期间的请求只挂着一个计时器，DOM 里遮罩/卡片一个节点都不生成。
        private bool _painted;
        private CancellationTokenSource _paintCts;

        /// <summary>是否显示等待浮层。推荐使用 <c>@bind-Visible</c> 进行双向绑定。</summary>
        [Parameter]
        public bool Visible
        {
            get => _visible;
            set => _visible = value;
        }

        /// <summary><see cref="Visible"/> 变更回调，供 <c>@bind-Visible</c> 使用。</summary>
        [Parameter]
        public EventCallback<bool> VisibleChanged { get; set; }

        /// <summary>标题文字（默认「处理中...」）。</summary>
        [Parameter]
        public string Title { get; set; } = "处理中...";

        /// <summary>提示文字（默认「请稍候」）。</summary>
        [Parameter]
        public string Message { get; set; } = "请稍候";

        /// <summary>是否显示「取消」按钮（默认 true）。</summary>
        [Parameter]
        public bool ShowCancelButton { get; set; } = true;

        /// <summary>取消按钮文字（默认「取消」）。</summary>
        [Parameter]
        public string CancelText { get; set; } = "取消";

        /// <summary>
        /// 取消按钮语义类型（透传给 <see cref="ElButton.Type"/>）：`Default`（默认）/ `Primary` / `Success` /
        /// `Warning` / `Danger` / `Info`，决定按钮的配色。
        /// </summary>
        [Parameter]
        public ButtonType CancelButtonType { get; set; } = ButtonType.Default;

        /// <summary>取消按钮尺寸（透传给 <see cref="ElButton.Size"/>）：`Default`（默认）/ `Large` / `Small`。</summary>
        [Parameter]
        public ButtonSize CancelButtonSize { get; set; } = ButtonSize.Default;

        /// <summary>取消按钮是否为朴素（浅底）样式（透传给 <see cref="ElButton.Plain"/>）。默认 false。</summary>
        [Parameter]
        public bool CancelButtonPlain { get; set; }

        /// <summary>取消按钮是否为胶囊圆角（透传给 <see cref="ElButton.Round"/>）。默认 false。</summary>
        [Parameter]
        public bool CancelButtonRound { get; set; }

        /// <summary>
        /// 取消按钮自定义颜色（透传给 <see cref="ElButton.Color"/>，取值为<b>任意 CSS 颜色</b>：
        /// `#f56c6c`、`rgb(...)`、`var(--el-color-danger)` 皆可）。为空时走 <see cref="CancelButtonType"/> 的主题配色。
        /// </summary>
        [Parameter]
        public string CancelButtonColor { get; set; }

        /// <summary>
        /// 自定义颜色时是否使用<b>实心</b>外观（透传给 <see cref="ElButton.Dark"/>）：
        /// false（默认）= 浅底 + 彩色文字（hover 才上实色）；true = 实色底 + 白字。
        /// </summary>
        [Parameter]
        public bool CancelButtonDark { get; set; }

        /// <summary>取消按钮所在操作区的水平位置：`Center`（默认）/ `Left` / `Right`。</summary>
        [Parameter]
        public ElWaitingPopupGrowActionAlign CancelButtonPosition { get; set; } = ElWaitingPopupGrowActionAlign.Center;

        /// <summary>点击遮罩是否按取消处理（默认 false：等待浮层默认不响应遮罩点击）。</summary>
        [Parameter]
        public bool MaskClosable { get; set; }

        /// <summary>加载圈尺寸（像素，默认 42）。</summary>
        [Parameter]
        public int SpinnerSize { get; set; } = 42;

        /// <summary>遮罩与浮层的基础 z-index（默认 2000；遮罩用本值，浮层卡片用本值 +1）。</summary>
        [Parameter]
        public int ZIndex { get; set; } = 2000;

        /// <summary>自定义卡片宽度（如 "360px"）；不设置则按内容自适应（卡片 min-width 280px）。</summary>
        [Parameter]
        public string Width { get; set; }

        /// <summary>是否使用 4px 圆角（对齐 Element Plus <c>--el-border-radius-base</c>）。默认 true。</summary>
        [Parameter]
        public bool Round { get; set; } = true;

        /// <summary>自定义圆角像素值；设置后覆盖 <see cref="Round"/>（负数按 0 处理）。</summary>
        [Parameter]
        public int? RoundRadius { get; set; }

        /// <summary>卡片背景色（如 <c>#ffffff</c>）；不设置则用默认白色。</summary>
        [Parameter]
        public string BackgroundColor { get; set; }

        /// <summary>标题文字颜色（如 <c>#ffffff</c>）；不设置则用默认主文本色。</summary>
        [Parameter]
        public string TitleColor { get; set; }

        /// <summary>提示文字颜色（如 <c>#e6f0ff</c>）；不设置则用默认常规文本色。</summary>
        [Parameter]
        public string MessageColor { get; set; }

        /// <summary>加载圈（转圈动画）颜色（如 <c>#ffd666</c>）；不设置则用主题主色。</summary>
        [Parameter]
        public string SpinnerColor { get; set; }

        /// <summary>取消回调：点击取消按钮（或 <see cref="MaskClosable"/> 时点击遮罩）后触发。</summary>
        [Parameter]
        public EventCallback OnCancel { get; set; }

        /// <summary>显隐变化回调（true = 打开 / false = 关闭）。</summary>
        [Parameter]
        public EventCallback<bool> OnVisibleChange { get; set; }

        /// <summary>标题 / 提示之外的额外内容插槽（如进度条、子状态）。</summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// 显示延迟（毫秒，默认 0 = 请求即出，与本参数加入前行为一致）。
        /// <para>
        /// 设成 N 后：<see cref="Show"/> / <c>Visible=true</c> 只起一个 N 毫秒的计时器，这期间若被
        /// <see cref="Hide"/>（或 <c>Visible=false</c>）取消，就<b>一次都不渲染</b>——遮罩、卡片、进场动画全都不出现；
        /// 只有请求真的活了超过 N 毫秒，浮层才浮出来。已经浮出来的再关闭仍是立即关闭，不受本参数影响。
        /// </para>
        /// <para>
        /// 用途：给「一闪而过」的请求消抖。实测一次列表换页从请求到落表约 160–250ms，
        /// 比浮层自己 0.2s 的进场动画还短，浮层等于刚闪出来就收；配 300 后这类请求整块跳过，
        /// 真正的慢请求照旧提示。
        /// </para>
        /// </summary>
        [Parameter]
        public int ShowDelay { get; set; }

        /// <summary>当前是否可见（命令式读取）。</summary>
        public bool IsVisible => _visible;

        /// <summary>浮层是否已经真的画到场上（<see cref="IsVisible"/> 为 true 但 <see cref="ShowDelay"/> 计时未到时为 false）。</summary>
        public bool IsPainted => _painted;

        /// <summary>遮罩 z-index。</summary>
        public int ResolvedMaskZIndex => ZIndex;

        /// <summary>浮层卡片 z-index（位于遮罩之上）。</summary>
        public int ResolvedZIndex => ZIndex + 1;

        /// <summary>解析后的圆角像素值：<see cref="RoundRadius"/> 优先，其次 <see cref="Round"/>（4px / 0px）。</summary>
        public string EffectiveRadius => RoundRadius.HasValue
            ? Math.Max(0, RoundRadius.Value) + "px"
            : (Round ? "4px" : "0px");

        /// <summary>打开等待浮层。</summary>
        public async Task Show()
        {
            if (_visible)
            {
                return;
            }

            _visible = true;
            await NotifyVisibleChanged(true);
            StartPaint();
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>关闭等待浮层（不触发取消回调）。</summary>
        public async Task Hide()
        {
            if (!_visible)
            {
                return;
            }

            _visible = false;
            CancelPendingPaint();
            _painted = false;
            await NotifyVisibleChanged(false);
            await InvokeAsync(StateHasChanged);
        }

        /// <inheritdoc />
        protected override async Task OnParametersSetAsync()
        {
            // 参数驱动的显隐变化也要广播 OnVisibleChange / VisibleChanged（与 Show / Hide 路径一致），
            // 这样纯声明式 @bind-Visible 用法也能收到事件。
            if (_visible != _lastNotifiedVisible)
            {
                await NotifyVisibleChanged(_visible);
                if (_visible)
                {
                    StartPaint();
                }
                else
                {
                    CancelPendingPaint();
                    _painted = false;
                }
            }

            await base.OnParametersSetAsync();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CancelPendingPaint();
            }

            base.Dispose(disposing);
        }

        /// <summary>点击取消按钮：先触发 <see cref="OnCancel"/>，再关闭。</summary>
        private async Task OnCancelClickAsync()
        {
            if (OnCancel.HasDelegate)
            {
                await OnCancel.InvokeAsync();
            }

            await Hide();
        }

        /// <summary>点击遮罩：仅当 <see cref="MaskClosable"/> 为 true 时按取消处理。</summary>
        private async Task OnMaskClickAsync()
        {
            if (MaskClosable)
            {
                await OnCancelClickAsync();
            }
        }

        /// <summary>
        /// 起一次「到点才画」的计时。ShowDelay ≤ 0 时同步置 <c>_painted</c>，
        /// 所以默认路径（延迟关闭）不引入任何一次额外的渲染往返。
        /// </summary>
        private void StartPaint()
        {
            if (ShowDelay <= 0)
            {
                _painted = true;
                return;
            }

            _painted = false;

            // 每次请求一把独立的 CTS（不复用上一把、也不在回调里递归 Show/Hide）：
            // Show→Hide→Show 连着走时，旧那把已被 Cancel+Dispose，回调里的 IsCancellationRequested 会挡住误画。
            var cts = new CancellationTokenSource();
            _paintCts = cts;
            _ = PaintAfterDelayAsync(cts);
        }

        private async Task PaintAfterDelayAsync(CancellationTokenSource cts)
        {
            try
            {
                await Task.Delay(ShowDelay, cts.Token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                // Cancel→Dispose 与 Task.Delay 内部定时器之间有已知竞态：到点前被取消就可能撞见已释放的定时器。
                return;
            }

            if (cts.IsCancellationRequested || !_visible)
            {
                return;
            }

            _painted = true;
            await InvokeAsync(StateHasChanged);
        }

        private void CancelPendingPaint()
        {
            var cts = _paintCts;
            if (cts == null)
            {
                return;
            }

            _paintCts = null;
            try
            {
                cts.Cancel();
            }
            finally
            {
                cts.Dispose();
            }
        }

        private async Task NotifyVisibleChanged(bool value)
        {
            _lastNotifiedVisible = value;

            if (VisibleChanged.HasDelegate)
            {
                await VisibleChanged.InvokeAsync(value);
            }

            if (OnVisibleChange.HasDelegate)
            {
                await OnVisibleChange.InvokeAsync(value);
            }
        }

        private string MaskCls => MaskClosable ? "is-closable" : string.Empty;

        private string WrapperClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-waiting-popup-grow__content")
            .Add(Cls)
            .ToString();

        private string ContentStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(!string.IsNullOrWhiteSpace(Width), $"width:{Width}")
            .AddIf(!string.IsNullOrWhiteSpace(BackgroundColor), $"background:{BackgroundColor}")
            .AddIf(!string.IsNullOrWhiteSpace(TitleColor), $"--ewpg-title-color:{TitleColor}")
            .AddIf(!string.IsNullOrWhiteSpace(MessageColor), $"--ewpg-message-color:{MessageColor}")
            .AddIf(!string.IsNullOrWhiteSpace(SpinnerColor), $"--ewpg-spinner-color:{SpinnerColor}")
            .Add($"border-radius:{EffectiveRadius}")
            .Add(Style)
            .ToString();

        /// <summary>
        /// 操作区样式：只下发「对齐」这个变量，具体 `text-align` 规则由 <c>index.css</c> 消费
        /// （与三色参数同一范式——C# 提供值，样式真源仍在 index.css）。`Center` 为默认且不输出变量。
        /// </summary>
        private string ActionsStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(CancelButtonPosition != ElWaitingPopupGrowActionAlign.Center,
                $"--ewpg-actions-align:{CancelButtonPosition.ToString().ToLowerInvariant()}")
            .ToString();
    }
}
