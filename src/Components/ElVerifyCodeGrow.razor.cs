using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace ElementPlus
{
    /// <summary>
    /// 验证码输入组件（自渲染、零第三方依赖，按 Element Plus 观感渲染）。
    /// <para>
    /// 左侧（或右侧）为程序生成的算术验证码图片（SVG），点击图片或按 Enter/Space 即可刷新；
    /// 右侧（或左侧）为可双向绑定的输入框（<see cref="Value"/> / <see cref="ValueChanged"/>，支持 <c>@bind-Value</c>）。
    /// 支持定时器自动刷新（<see cref="AutoRefreshInterval"/>）、刷新倒计时进度条（<see cref="ShowProgressBar"/> /
    /// <see cref="ProgressColor"/> / <see cref="ProgressWidth"/> / <see cref="ProgressHeight"/>——自动刷新时图片底部会有一条
    /// 宽度随时间递减的进度条，每次刷新重置）、噪点复杂度（<see cref="NoiseComplexity"/>）、
    /// 左右位置切换（<see cref="Position"/>）、边框显隐（<see cref="ShowBorder"/>）与圆角（<see cref="Round"/> / <see cref="RoundRadius"/>）。
    /// 每次刷新会通过 <see cref="OnRefresh"/> 暴露新的验证码信息，校验请用 <see cref="ValidateAsync"/>。
    /// </para>
    /// <para>
    /// <b>安全提示</b>：内置 <see cref="VerifyCodeService"/> 把答案存放在进程内存中，仅适合演示与前端自校验；
    /// 生产环境应将「生成」与「校验」下沉到服务端，并使用不可逆存储（如带过期时间的哈希）。
    /// </para>
    /// </summary>
    public partial class ElVerifyCodeGrow : ElementComponentBase, IDisposable
    {
        private readonly VerifyCodeService _service = new VerifyCodeService();
        private Timer _timer;
        private int _activeInterval;

        /// <summary>用户输入的验证码答案（支持 <c>@bind-Value</c>）。</summary>
        [Parameter]
        public string Value { get; set; }

        /// <summary><see cref="Value"/> 变更回调，供 <c>@bind-Value</c> 使用。</summary>
        [Parameter]
        public EventCallback<string> ValueChanged { get; set; }

        /// <summary>验证码图片与输入框的相对位置。默认 <see cref="ElVerifyCodePosition.Left"/>（图左框右）。</summary>
        [Parameter]
        public ElVerifyCodePosition Position { get; set; } = ElVerifyCodePosition.Left;

        /// <summary>噪点复杂度（默认 5，即基准噪点量；0 = 无噪点）。</summary>
        [Parameter]
        public int NoiseComplexity { get; set; } = 5;

        /// <summary>自动刷新间隔（毫秒）；≤0 表示不自动刷新。默认 0。</summary>
        [Parameter]
        public int AutoRefreshInterval { get; set; }

        /// <summary>
        /// 自动刷新时是否在验证码图片底部显示倒计时进度条（宽度随时间递减，刷新即重置）。
        /// 默认 true；仅当 <see cref="AutoRefreshInterval"/> &gt; 0 时可见。
        /// </summary>
        [Parameter]
        public bool ShowProgressBar { get; set; } = true;

        /// <summary>倒计时进度条颜色。默认 <see cref="DefaultProgressColor"/>。</summary>
        [Parameter]
        public string ProgressColor { get; set; } = DefaultProgressColor;

        /// <summary>倒计时进度条宽度（像素）；不设置则与验证码图片等宽。</summary>
        [Parameter]
        public int? ProgressWidth { get; set; }

        /// <summary>倒计时进度条高度（像素）。默认 3。</summary>
        [Parameter]
        public int ProgressHeight { get; set; } = 3;

        /// <summary>是否显示外边框。默认 true。</summary>
        [Parameter]
        public bool ShowBorder { get; set; } = true;

        /// <summary>是否使用 4px 圆角（对齐 Element Plus <c>--el-border-radius-base</c>）。默认 false。</summary>
        [Parameter]
        public bool Round { get; set; }

        /// <summary>自定义圆角像素值；设置后覆盖 <see cref="Round"/>。</summary>
        [Parameter]
        public int? RoundRadius { get; set; }

        /// <summary>验证码图片宽度（像素）。默认 200。</summary>
        [Parameter]
        public int CaptchaWidth { get; set; } = 200;

        /// <summary>验证码图片高度（像素）。默认 60。</summary>
        [Parameter]
        public int CaptchaHeight { get; set; } = 60;

        /// <summary>输入框宽度（像素）；不设置则自适应剩余宽度。</summary>
        [Parameter]
        public int? InputWidth { get; set; }

        /// <summary>输入框字体大小（像素）。默认 14。</summary>
        [Parameter]
        public int FontSize { get; set; } = 14;

        /// <summary>占位符字体大小（像素）；不设置则沿用 <see cref="FontSize"/>。可与输入框字符大小分别设置。</summary>
        [Parameter]
        public int? PlaceholderFontSize { get; set; }

        /// <summary>输入框占位符。</summary>
        [Parameter]
        public string Placeholder { get; set; } = "请输入计算结果";

        /// <summary>是否禁用。</summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>刷新事件：每次重新生成验证码后触发，携带最新的验证码信息。</summary>
        [Parameter]
        public EventCallback<VerifyCodeGeneratedEventArgs> OnRefresh { get; set; }

        /// <summary>当前验证码唯一标识（用于服务端校验）。</summary>
        public string CaptchaId { get; private set; }

        /// <summary>当前验证码图片的 SVG data URL（可直接用于 <c>&lt;img src&gt;</c>）。</summary>
        public string ImageData { get; private set; }

        /// <summary>
        /// 当前正确答案。出于演示便利而暴露；<b>切勿用于生产校验</b>——客户端可读。
        /// 正式校验请使用 <see cref="ValidateAsync"/>（或把 <see cref="CaptchaId"/> 交服务端校验）。
        /// </summary>
        public string Answer { get; private set; }

        /// <summary>当前题面文本（如 <c>12 加 3 = </c>）。</summary>
        public string Question { get; private set; }

        /// <summary>倒计时进度条是否渲染（开启定时刷新且未显式关闭时才显示）。</summary>
        public bool IsCountdownVisible => ShowProgressBar && AutoRefreshInterval > 0;

        /// <summary>
        /// 倒计时动画的重置键：验证码 Id 或间隔变化即变化，配合 <c>@key</c> 让进度条元素被重建，
        /// 从而重播 CSS 动画（视觉上就是「刷新后进度条重置」）。
        /// </summary>
        public string CountdownKey => CaptchaId + "|" + AutoRefreshInterval;

        /// <summary>解析后的圆角像素值。</summary>
        public string EffectiveRadius
        {
            get
            {
                if (RoundRadius.HasValue)
                {
                    return Math.Max(0, RoundRadius.Value) + "px";
                }

                return Round ? "4px" : "0px";
            }
        }

        /// <inheritdoc/>
        protected override async Task OnInitializedAsync()
        {
            await GenerateAsync();
        }

        /// <inheritdoc/>
        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            // 定时器由 OnParametersSet 统一管理：勾选定时刷新后参数变化也会重启/停止
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override Task OnParametersSetAsync()
        {
            if (AutoRefreshInterval != _activeInterval)
            {
                RestartTimer(AutoRefreshInterval);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 用户主动刷新（点击图片 / 键盘 Enter、Space）。除重建验证码外还会<b>重新计时</b>，
        /// 使进度条动画与下一次自动刷新对齐——否则进度条虽已重置，却会在走到一半时被旧的定时器提前截断。
        /// </summary>
        private async Task ManualRefreshAsync()
        {
            await RefreshAsync();

            if (AutoRefreshInterval > 0)
            {
                RestartTimer(AutoRefreshInterval);
            }
        }

        private void RestartTimer(int interval)
        {
            _activeInterval = interval;

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Elapsed -= OnTimerElapsed;
                _timer.Dispose();
                _timer = null;
            }

            if (interval > 0)
            {
                _timer = new Timer(interval);
                _timer.Elapsed += OnTimerElapsed;
                _timer.AutoReset = true;
                _timer.Start();
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _ = RefreshAsync();
        }

        /// <summary>重新生成验证码（新图片 + 新答案），并触发 <see cref="OnRefresh"/>。</summary>
        public async Task RefreshAsync()
        {
            await GenerateAsync();
            await InvokeAsync(StateHasChanged);
        }

        private async Task GenerateAsync()
        {
            var result = await _service.GenerateMathCaptchaAsync(NoiseComplexity, CaptchaWidth, CaptchaHeight);
            CaptchaId = result.CaptchaId;
            ImageData = result.ImageData;
            Answer = result.Answer;
            Question = result.Question;

            if (OnRefresh.HasDelegate)
            {
                await OnRefresh.InvokeAsync(new VerifyCodeGeneratedEventArgs(CaptchaId, ImageData, Question, Answer));
            }
        }

        /// <summary>
        /// 校验用户输入是否与当前验证码答案一致；一致则立即作废该验证码（一次性）。
        /// </summary>
        /// <param name="userInput">用户在输入框中填写的内容。</param>
        /// <returns>校验是否通过。</returns>
        public async Task<bool> ValidateAsync(string userInput)
        {
            if (string.IsNullOrEmpty(CaptchaId))
            {
                return false;
            }

            return await _service.ValidateCaptchaAsync(userInput ?? string.Empty, CaptchaId);
        }

        /// <summary>输入框内容变化（@bind-Value 内部使用）。</summary>
        private async Task OnInput(ChangeEventArgs e)
        {
            Value = e.Value?.ToString() ?? string.Empty;
            await ValueChanged.InvokeAsync(Value);
        }

        /// <summary>键盘刷新：聚焦验证码后按 Enter / Space 刷新。</summary>
        private async Task OnCaptchaKey(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" || e.Key == " " || e.Key == "Spacebar")
            {
                await ManualRefreshAsync();
            }
        }

        /// <inheritdoc/>
        public new void Dispose()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Elapsed -= OnTimerElapsed;
                _timer.Dispose();
                _timer = null;
            }

            base.Dispose();
        }

        private string WrapperClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-verify-code-grow")
            .AddIf(ShowBorder, "is-bordered")
            .AddIf(Position == ElVerifyCodePosition.Right, "is-reverse")
            .Add(Cls)
            .ToString();

        private string WrapperStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add($"height:{CaptchaHeight}px")
            .Add($"border-radius:{EffectiveRadius}")
            .AddIf(ShowBorder, $"border:1px solid {BorderColor}")
            .Add(Style)
            .ToString();

        private string CaptchaStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add($"width:{CaptchaWidth}px")
            .Add($"height:{CaptchaHeight}px")
            .ToString();

        private string InputStyle
        {
            get
            {
                var b = HtmlPropertyBuilder.CreateCssStyleBuilder();
                if (FontSize != 14)
                {
                    b.Add($"font-size:{FontSize}px");
                }

                if (PlaceholderFontSize.HasValue)
                {
                    // 通过 CSS 自定义属性驱动 ::placeholder 字号，使占位符字号与输入框字符大小相互独立。
                    b.Add($"--evcg-placeholder-font-size:{Math.Max(1, PlaceholderFontSize.Value)}px");
                }

                if (InputWidth.HasValue)
                {
                    b.Add($"width:{InputWidth.Value}px");
                }

                return b.ToString();
            }
        }

        /// <summary>解析后的进度条颜色：为空时回落到 <see cref="DefaultProgressColor"/>。</summary>
        public string EffectiveProgressColor
            => string.IsNullOrWhiteSpace(ProgressColor) ? DefaultProgressColor : ProgressColor;

        /// <summary>
        /// 倒计时进度条「轨道」样式：宽度默认与验证码图片等宽，可用 <see cref="ProgressWidth"/> 覆盖。
        /// 轨道只是长度基准，本身透明；真正着色并动画的是内部 <c>__progress-bar</c>。
        /// </summary>
        private string ProgressTrackStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add($"width:{(ProgressWidth.HasValue ? Math.Max(0, ProgressWidth.Value) : CaptchaWidth)}px")
            .Add($"height:{Math.Max(1, ProgressHeight)}px")
            .ToString();

        /// <summary>
        /// 倒计时进度条样式：颜色来自 <see cref="ProgressColor"/>，宽度在 <see cref="ProgressTrackStyle"/>
        /// 限定的轨道内由 keyframes 从 100% 线性递减到 0%（<c>forwards</c> 保证走完后停在空）。
        /// </summary>
        private string ProgressBarStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add($"background:{EffectiveProgressColor}")
            .Add($"animation:el-verify-code-grow-countdown {AutoRefreshInterval}ms linear forwards")
            .ToString();

        /// <summary>进度条默认色（对齐 Element Plus <c>--el-color-primary</c>）。</summary>
        public const string DefaultProgressColor = "#409eff";

        /// <summary>边框默认色（对齐 Element Plus <c>--el-border-color</c>）。</summary>
        public const string BorderColor = "#dcdfe6";
    }

    /// <summary>验证码刷新事件参数。</summary>
    public class VerifyCodeGeneratedEventArgs : EventArgs
    {
        /// <summary>验证码唯一标识。</summary>
        public string CaptchaId { get; }

        /// <summary>图片 SVG data URL。</summary>
        public string ImageData { get; }

        /// <summary>题面文本。</summary>
        public string Question { get; }

        /// <summary>正确答案（演示用）。</summary>
        public string Answer { get; }

        /// <summary>构造事件参数。</summary>
        public VerifyCodeGeneratedEventArgs(string captchaId, string imageData, string question, string answer)
        {
            CaptchaId = captchaId;
            ImageData = imageData;
            Question = question;
            Answer = answer;
        }
    }
}
