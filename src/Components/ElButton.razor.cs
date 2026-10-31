using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElButton : ElementComponentBase
    {
        internal HtmlPropertyBuilder cssClassBuilder;
        internal HtmlPropertyBuilder buttonStyleBuilder;

        [CascadingParameter]
        public ElButtonGroup ButtonGroup { get; set; }

        // ============ 防抖 ============
        [Parameter] public int Debounce { get; set; }
        [Parameter] public bool DebounceLeading { get; set; }
        private CancellationTokenSource debounceCts;
        private DateTime? debounceWindowStart;

        private void CancelPendingClick()
        {
            if (debounceCts == null) return;
            debounceCts.Cancel();
            debounceCts.Dispose();
            debounceCts = null;
        }

        protected async Task OnButtonClickedAsync(MouseEventArgs e)
        {
            if (IsButtonDisabled) return;

            if (Debounce > 0)
            {
                if (DebounceLeading)
                {
                    var now = DateTime.UtcNow;
                    if (debounceWindowStart.HasValue &&
                        (now - debounceWindowStart.Value).TotalMilliseconds < Debounce) return;
                    debounceWindowStart = now;
                }
                else
                {
                    var cts = new CancellationTokenSource();
                    CancelPendingClick();
                    debounceCts = cts;
                    try { await Task.Delay(Debounce, cts.Token); }
                    catch (TaskCanceledException) { return; }
                    if (cts.Token.IsCancellationRequested) return;
                    if (ReferenceEquals(debounceCts, cts)) debounceCts = null;
                    cts.Dispose();
                }
            }

            await InvokeAsync(() => RaiseClickAsync(e));
        }

        private async Task RaiseClickAsync(MouseEventArgs e)
        {
            var oldImg = showingImage;
            if (!string.IsNullOrWhiteSpace(showingImage) && !string.IsNullOrWhiteSpace(ClickImage))
            {
                showingImage = ClickImage;
                StateHasChanged();
                await Task.Delay(100);
            }
            if (OnClick.HasDelegate) await OnClick.InvokeAsync(e);
            if (!string.IsNullOrWhiteSpace(oldImg)) { showingImage = oldImg; StateHasChanged(); }
        }

        public override void Dispose()
        {
            CancelPendingClick();
            base.Dispose();
        }

        private string showingImage;

        // ============ Element Plus 官方属性 ============
        [Parameter] public bool Text { get; set; }
        [Parameter] public string Content { get; set; }
        [Parameter] public string Label { get; set; }
        [Parameter] public string Image { get; set; }
        [Parameter] public string HoverImage { get; set; }
        [Parameter] public string ClickImage { get; set; }
        [Parameter] public bool AppendCustomCls { get; set; } = true;
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
        [Parameter] public ButtonType Type { get; set; } = ButtonType.Default;
        [Parameter] public bool Plain { get; set; }
        [Parameter] public bool IsPlain { get => Plain; set => Plain = value; }
        [Parameter] public bool Round { get; set; }
        [Parameter] public bool IsRound { get => Round; set => Round = value; }
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public bool IsDisabled { get => Disabled; set => Disabled = value; }

        /// <summary>
        /// 本仓 Grow 口径（Element Plus 没有这个属性，官方用父级 v-if）：
        /// <c>false</c> 时整颗按钮不渲染（连 DOM 都不留），用于权限/状态驱动的工具条。
        /// 与 <see cref="Disabled"/> 是两件事——后者是「看得见但点不动」（会级联按钮组的禁用态），
        /// 前者是「此刻不存在」。默认 <c>true</c>，加参数前的行为逐字节不变。
        /// </summary>
        [Parameter] public bool Visible { get; set; } = true;

        [Parameter] public bool Circle { get; set; }
        [Parameter] public bool IsCircle { get => Circle; set => Circle = value; }
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public ButtonSize Size { get; set; }
        [Parameter] public string Icon { get; set; }
        [Parameter] public RenderFragment IconContent { get; set; }
        [Parameter] public bool Loading { get; set; }
        [Parameter] public bool IsLoading { get => Loading; set => Loading = value; }
        [Parameter] public string LoadingIcon { get; set; } = "el-icon-loading";
        [Parameter] public RenderFragment LoadingContent { get; set; }
        [Parameter] public string NativeType { get; set; } = "button";
        [Parameter] public bool Link { get; set; }
        [Parameter] public bool Bg { get; set; }
        [Parameter] public bool Dashed { get; set; }
        [Parameter] public bool Autofocus { get; set; }
        [Parameter] public string Color { get; set; }
        [Parameter] public bool Dark { get; set; }
        [Parameter] public bool AutoInsertSpace { get; set; }
        [Parameter] public string Tag { get; set; }
        [Parameter] public string Href { get; set; }
        [Parameter] public string Target { get; set; }
        [Parameter] public string Rel { get; set; }
        [Parameter] public int? Tabindex { get; set; }

        // ============ Element Plus 2.x 补齐属性 ============
        public enum IconPositionMode { Left, Right }

        /// <summary>图标位置：left（默认）/ right，对应 Element Plus icon-position</summary>
        [Parameter] public IconPositionMode IconPosition { get; set; } = IconPositionMode.Left;

        // ============ 计算属性 ============
        protected string ButtonTextContent => Content ?? Label;
        protected bool IsButtonDisabled => Disabled || Loading || (ButtonGroup?.EffectiveDisabled ?? false);

        // ============ Element Plus 全局配置（ButtonConfigContext）============
        // EP 里 plain / text / round / dashed / type 的默认值是 undefined —— 组件没显式传值时
        // 回落到 ElConfigProvider 的 Button 全局配置。Blazor 的 bool 参数区分不出「没传」和
        // 「传了 false」，因此在 SetParametersAsync 里记录本组件是否真的被赋值。
        private bool _typeSpecified;
        private bool _plainSpecified;
        private bool _roundSpecified;
        private bool _dashedSpecified;
        private bool _textSpecified;

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (var p in parameters)
            {
                switch (p.Name)
                {
                    case nameof(Type): _typeSpecified = true; break;
                    case nameof(Plain):
                    case nameof(IsPlain): _plainSpecified = true; break;
                    case nameof(Round):
                    case nameof(IsRound): _roundSpecified = true; break;
                    case nameof(Dashed): _dashedSpecified = true; break;
                    case nameof(Text): _textSpecified = true; break;
                }
            }
            return base.SetParametersAsync(parameters);
        }

        private bool EffectivePlain => _plainSpecified ? Plain : (GlobalButtonConfig?.Plain ?? false);
        private bool EffectiveRound => _roundSpecified ? Round : (GlobalButtonConfig?.Round ?? false);
        private bool EffectiveDashed => _dashedSpecified ? Dashed : (GlobalButtonConfig?.Dashed ?? false);
        private bool EffectiveText => _textSpecified ? Text : (GlobalButtonConfig?.Text ?? false);

        private ButtonType EffectiveType
        {
            get
            {
                var t = Type;
                // 未显式传 Type 时才吃全局配置（ButtonConfigContext.Type 是字符串）
                if (!_typeSpecified && GlobalButtonConfig != null
                    && Enum.TryParse<ButtonType>(GlobalButtonConfig.Type, true, out var cfgType)
                    && cfgType != ButtonType.Default)
                {
                    t = cfgType;
                }
                // 仍未指定则继承按钮组
                return t == ButtonType.Default && ButtonGroup != null ? ButtonGroup.Type : t;
            }
        }

        private ButtonSize EffectiveSize => Size == ButtonSize.Default && ButtonGroup != null
            ? ButtonGroup.EffectiveSize : ResolveButtonSize(Size);

        // Element Plus 里 type="text"（→ .el-button--text）与 text 布尔属性（→ .is-text）
        // 是两个独立开关，官方不会同时给；这里同样只按 text 属性给 is-text。
        private bool IsTextButton => EffectiveText;
        // Tag 未显式指定时按 Element Plus 的习惯：给了 Href 就渲染成 <a>，否则 <button>。
        private string EffectiveTag => !string.IsNullOrWhiteSpace(Tag)
            ? Tag.Trim().ToLowerInvariant()
            : (string.IsNullOrWhiteSpace(Href) ? "button" : "a");
        private bool IsNativeButton => string.Equals(EffectiveTag, "button", StringComparison.OrdinalIgnoreCase);

        private string SizeCssValue => EffectiveSize switch
        {
            ButtonSize.Large => "large",
            ButtonSize.Small => "small",
            _ => null
        };

        // Element Plus：type="text" 渲染 .el-button--text（蓝色 + 零左右内边距），
        // 与 text 布尔属性（.is-text，保留内边距）是两套类名，这里按官方同时给出。
        private string TypeCssValue => EffectiveType == ButtonType.Default
            ? null : EffectiveType.ToString().ToLowerInvariant();

        /// <summary>
        /// EP Exposes <c>shouldAddSpace</c>：是否需要「两字之间加宽」。
        /// 官方不插真空格，而是给文本 span 加 letter-spacing；仅当文本长度=2 且全为中文时生效。
        /// 通过组件引用可读：<c>myBtn.ShouldAddSpace</c>。
        /// </summary>
        public bool ShouldAddSpace
        {
            get
            {
                var text = ButtonTextContent ?? string.Empty;
                return EffectiveAutoInsertSpace && text.Length == 2 && text.All(IsChineseCharacter);
            }
        }

        // ============ Element Plus Exposes（ref / size / type / disabled / shouldAddSpace）============
        // 以下属性返回「经过按钮组级联 + 全局 ButtonConfigContext 解析」后的最终值，
        // 与 EP 暴露的同名 Exposes 语义一致（读取的是生效态，而非原始 Parameter）。
        // 用法：<ElButton @ref="myBtn" /> 然后 await myBtn.FocusAsync() / 读取 myBtn.ResolvedType 等。

        /// <summary>EP Exposes <c>type</c>：生效后的按钮类型（含按钮组级联与全局配置回落）。</summary>
        public ButtonType ResolvedType => EffectiveType;

        /// <summary>EP Exposes <c>size</c>：生效后的尺寸（含按钮组级联与默认尺寸解析）。</summary>
        public ButtonSize ResolvedSize => EffectiveSize;

        /// <summary>EP Exposes <c>disabled</c>：是否处于禁用态（含 Loading / 按钮组禁用）。</summary>
        public bool ResolvedDisabled => IsButtonDisabled;

        private string RenderedButtonTextContent => ButtonTextContent ?? string.Empty;

        private static bool IsChineseCharacter(char value) => value >= '\u4e00' && value <= '\u9fff';

        /// <summary>从全局 ConfigProvider 读取按钮配置</summary>
        private ButtonConfigContext GlobalButtonConfig => ElementConfiguration?.Button;

        private bool EffectiveAutoInsertSpace =>
            AutoInsertSpace || (GlobalButtonConfig?.AutoInsertSpace ?? false);

        protected override bool ShouldRender() => true;

        private void MouseOver() { if (!string.IsNullOrWhiteSpace(HoverImage)) showingImage = HoverImage; }
        private void MouseOut() { if (!string.IsNullOrWhiteSpace(HoverImage)) showingImage = Image; }

        // ============ OnParametersSet — 统一 CSS 构建 ============
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            if (string.IsNullOrWhiteSpace(showingImage) && !string.IsNullOrWhiteSpace(Image))
                showingImage = Image;

            // Style builder: 自定义 color
            buttonStyleBuilder = HtmlPropertyBuilder.CreateCssStyleBuilder()
                .Add(Style)
                // Dark=true：深色底 + 白字（Element Plus 的 dark 语义）
                .AddIf(!string.IsNullOrWhiteSpace(Color) && !IsTextButton && !Link && Dark,
                    "--el-button-text-color:var(--el-color-white)",
                    $"--el-button-bg-color:{Color}",
                    $"--el-button-border-color:{Color}",
                    $"--el-button-hover-bg-color:color-mix(in srgb, {Color} 82%, var(--el-color-white))",
                    $"--el-button-hover-border-color:color-mix(in srgb, {Color} 82%, var(--el-color-white))",
                    $"--el-button-active-bg-color:color-mix(in srgb, {Color} 82%, var(--el-color-black))",
                    $"--el-button-active-border-color:color-mix(in srgb, {Color} 82%, var(--el-color-black))")
                // Dark=false（默认）：浅色底 + 彩色文字，hover 才上实色 —— Element Plus 的默认语义。
                // 旧内核（无 color-mix）需要能回落到一条普通颜色声明，因此每个变量先写降级值再写 color-mix 版本，
                // 支持的内核用后者覆盖，不支持的忽略后者、保留前者。
                .AddIf(!string.IsNullOrWhiteSpace(Color) && !IsTextButton && !Link && !Dark,
                    $"--el-button-text-color:{Color}",
                    "--el-button-bg-color:var(--el-fill-color-blank)",
                    $"--el-button-border-color:{Color}",
                    $"--el-button-bg-color:color-mix(in srgb, {Color} 12%, var(--el-fill-color-blank))",
                    $"--el-button-border-color:color-mix(in srgb, {Color} 40%, var(--el-fill-color-blank))",
                    $"--el-button-hover-bg-color:{Color}",
                    $"--el-button-hover-border-color:{Color}",
                    "--el-button-hover-text-color:var(--el-color-white)",
                    $"--el-button-active-bg-color:color-mix(in srgb, {Color} 82%, var(--el-color-black))",
                    $"--el-button-active-border-color:color-mix(in srgb, {Color} 82%, var(--el-color-black))",
                    "--el-button-active-text-color:var(--el-color-white)")
                .AddIf(!string.IsNullOrWhiteSpace(Color) && (IsTextButton || Link),
                    $"--el-button-text-color:{Color}",
                    $"--el-button-hover-text-color:color-mix(in srgb, {Color} 82%, var(--el-color-white))",
                    $"--el-button-active-text-color:color-mix(in srgb, {Color} 82%, var(--el-color-black))");

            // CSS class builder — 统一出口
            // 语义对齐 ButtonBase.AppendCustomCls：false = 用 Cls「替换」默认类（而非丢弃 Cls）。
            // 这正是 <ElButton Cls="el-picker-panel__icon-btn el-icon-d-arrow-left" AppendCustomCls="false" />
            // 能渲染出图标字形的关键；早期实现把 Cls 直接丢弃，导致选择器面板按钮只剩空框。
            cssClassBuilder = HtmlPropertyBuilder.CreateCssClassBuilder();
            if (!AppendCustomCls && !string.IsNullOrWhiteSpace(Cls))
            {
                // 替换模式：只输出自定义类，不再附加 el-button / el-button--xxx 等默认类
                cssClassBuilder.Add(Cls);
                return;
            }

            cssClassBuilder.Add("el-button", Cls)
                .AddIf(TypeCssValue != null, $"el-button--{TypeCssValue}")
                .AddIf(SizeCssValue != null, $"el-button--{SizeCssValue}")
                .AddIf(EffectivePlain, "is-plain")
                .AddIf(EffectiveRound, "is-round")
                .AddIf(IsButtonDisabled, "is-disabled")
                .AddIf(Loading, "is-loading")
                .AddIf(Circle, "is-circle")
                .AddIf(IsTextButton, "is-text")
                .AddIf(Link, "is-link")
                .AddIf(Bg, "is-has-bg")
                .AddIf(EffectiveDashed, "is-dashed")
                // Element Plus icon-position=right → el-button--icon-right
                .AddIf(IconPosition == IconPositionMode.Right && !string.IsNullOrWhiteSpace(Icon), "el-button--icon-right")
                // Element Plus: 有文字 + 图标 → el-button--has-icon
                .AddIf(!Circle && !string.IsNullOrWhiteSpace(Icon) && !string.IsNullOrWhiteSpace(RenderedButtonTextContent), "el-button--has-icon");
        }

        // ============ Render ============
        /// <summary>
        /// 🔥 手写渲染树的序列号必须「按槽位固定」，不能用 seq++ 连续分配。
        /// 一旦某个可选帧（aria-busy / autofocus / 图标 / Loading 内容 / 链接属性）增减，
        /// seq++ 会把其后所有帧的序列号整体平移，末帧 AddElementReferenceCapture
        /// 就会与旧树里同序列的「异类型帧」相撞，Blazor diff 直接抛
        /// NotImplementedException: Unexpected frame type during RemoveOldFrame: ElementReferenceCapture
        /// （实测：裸 ElButton 只把 Loading 由 false 切成 true 必崩，任何宿主里的
        ///  提交/发送按钮进入 loading 态都会整页渲染失败）。
        /// 下面每个槽位各占一个固定编号，可选帧跳过时不占用后续编号。
        /// </summary>
        private const int SeqRoot = 0;
        private const int SeqSpread = 1;
        private const int SeqStyle = 2;
        private const int SeqClass = 3;
        private const int SeqAriaDisabled = 4;
        private const int SeqAriaBusy = 5;
        private const int SeqAutofocus = 6;
        private const int SeqType = 7;
        private const int SeqDisabled = 8;
        private const int SeqTabindexNative = 9;
        private const int SeqRole = 10;
        private const int SeqTabindex = 11;
        private const int SeqOnKeydown = 12;
        private const int SeqHref = 13;
        private const int SeqTarget = 14;
        private const int SeqRel = 15;
        private const int SeqOnClick = 16;
        private const int SeqRefCapture = 90;
        // 内容区三段：左侧图标 20..27、文字 40..43、右侧图标 60..67（互不重叠，切位置也不撞）
        private const int SeqIconLeft = 20;
        private const int SeqText = 40;
        private const int SeqIconRight = 60;

        protected RenderFragment ButtonElement => builder =>
        {
            builder.OpenElement(SeqRoot, EffectiveTag);
            if (Attributes != null) builder.AddMultipleAttributes(SeqSpread, Attributes);

            builder.AddAttribute(SeqStyle, "style", buttonStyleBuilder);
            builder.AddAttribute(SeqClass, "class", cssClassBuilder);
            builder.AddAttribute(SeqAriaDisabled, "aria-disabled", IsButtonDisabled ? "true" : "false");
            if (Loading) builder.AddAttribute(SeqAriaBusy, "aria-busy", "true");
            if (Autofocus) builder.AddAttribute(SeqAutofocus, "autofocus", true);

            if (IsNativeButton)
            {
                builder.AddAttribute(SeqType, "type", string.IsNullOrWhiteSpace(NativeType) ? "button" : NativeType);
                builder.AddAttribute(SeqDisabled, "disabled", IsButtonDisabled);
                if (Tabindex.HasValue) builder.AddAttribute(SeqTabindexNative, "tabindex", Tabindex.Value);
            }
            else
            {
                builder.AddAttribute(SeqRole, "role", "button");
                builder.AddAttribute(SeqTabindex, "tabindex", IsButtonDisabled ? -1 : Tabindex ?? 0);
                builder.AddAttribute(SeqOnKeydown, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, OnButtonKeyDownAsync));
                if (string.Equals(EffectiveTag, "a", StringComparison.OrdinalIgnoreCase))
                {
                    builder.AddAttribute(SeqHref, "href", IsButtonDisabled ? null : Href);
                    builder.AddAttribute(SeqTarget, "target", Target);
                    builder.AddAttribute(SeqRel, "rel", Rel);
                }
            }

            builder.AddAttribute(SeqOnClick, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, OnButtonClickedAsync));
            BuildButtonContent(builder);
            builder.AddElementReferenceCapture(SeqRefCapture, __ref => ButtonReference = __ref);
            builder.CloseElement();
        };

        /// <summary>
        /// Element Plus Exposes：通过组件引用拿到原生 DOM 元素（ref），可调用 focus 等原生方法。
        /// 用法：&lt;ElButton @ref="myBtn" /&gt; 然后 await myBtn.FocusAsync();
        /// </summary>
        public ElementReference ButtonReference { get; private set; }

        /// <summary>聚焦到该按钮（对齐 Element Plus 暴露的 ref.focus()）</summary>
        public async Task FocusAsync()
        {
            if (!ButtonReference.Equals(default))
            {
                await ButtonReference.FocusAsync();
            }
        }

        private void BuildButtonContent(RenderTreeBuilder builder)
        {
            bool renderIconFirst = IconPosition != IconPositionMode.Right;
            bool hasIcon = Loading || IconContent != null || !string.IsNullOrWhiteSpace(Icon);
            bool hasText = !string.IsNullOrWhiteSpace(RenderedButtonTextContent) || ChildContent != null;

            // 三段各用独立的固定序列号区间：切 IconPosition（图标从左到右）也不会撞序列
            if (renderIconFirst || !hasText) RenderIcon(builder, SeqIconLeft);

            if (hasText)
            {
                builder.OpenElement(SeqText, "span");
                // Element Plus：两字加宽用 .el-button__text--expand 的 letter-spacing，而非真空格
                if (ShouldAddSpace)
                    builder.AddAttribute(SeqText + 1, "class", "el-button__text el-button__text--expand");
                builder.AddContent(SeqText + 2, RenderedButtonTextContent);
                builder.AddContent(SeqText + 3, ChildContent);
                builder.CloseElement();
            }

            if (!renderIconFirst && hasText) RenderIcon(builder, SeqIconRight);
        }

        /// <summary>
        /// 渲染按钮图标。base 是本图标块的固定序列号起点
        /// （4 种互斥形态各占一格，形态之间切换不会造成后续帧序列漂移）：
        /// base+0 LoadingContent、base+1..3 LoadingIcon 的 &lt;i&gt;、
        /// base+4 IconContent、base+5..7 Icon 的 &lt;i&gt;。
        /// </summary>
        private void RenderIcon(RenderTreeBuilder builder, int baseSeq)
        {
            // Element Plus：图标按位置带 el-icon--left / el-icon--right，
            // 由 index.css 的 5px 间距规则产生「图标与文字」留白（此前漏加 → 图标贴字）。
            var iconSideClass = IconPosition == IconPositionMode.Right ? "el-icon--right" : "el-icon--left";
            // 仅在有文字时加留白类：纯图标（含圆形）按钮不需要，避免圆心偏移。
            var hasText = !string.IsNullOrWhiteSpace(RenderedButtonTextContent) || ChildContent != null;
            if (!hasText) iconSideClass = string.Empty;

            if (Loading)
            {
                if (LoadingContent != null)
                {
                    builder.AddContent(baseSeq + 0, LoadingContent);
                    return;
                }
                if (!string.IsNullOrWhiteSpace(LoadingIcon))
                {
                    builder.OpenElement(baseSeq + 1, "i");
                    builder.AddAttribute(baseSeq + 2, "class", $"{LoadingIcon} {iconSideClass}");
                    builder.AddAttribute(baseSeq + 3, "aria-hidden", "true");
                    builder.CloseElement();
                    return;
                }
            }
            if (IconContent != null) { builder.AddContent(baseSeq + 4, IconContent); return; }
            if (!string.IsNullOrWhiteSpace(Icon))
            {
                builder.OpenElement(baseSeq + 5, "i");
                builder.AddAttribute(baseSeq + 6, "class", $"{Icon} {iconSideClass}");
                builder.AddAttribute(baseSeq + 7, "aria-hidden", "true");
                builder.CloseElement();
            }
        }

        private async Task OnButtonKeyDownAsync(KeyboardEventArgs e)
        {
            if (e.Key != "Enter" && e.Key != " ") return;
            await OnButtonClickedAsync(new MouseEventArgs());
        }
    }
}