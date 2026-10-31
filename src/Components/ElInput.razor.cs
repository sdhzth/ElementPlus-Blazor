using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Web;

namespace ElementPlus
{
    public partial class ElInput<TValue> : ElementFieldComponentBase<TValue>, IDisposable
    {
        internal HtmlPropertyBuilder wrapperClsBuilder;
        private static long inputIdSeed;
        private readonly string generatedInputId = $"el-input-{Interlocked.Increment(ref inputIdSeed)}";
        private bool passwordVisible;
        private bool isComposing;

        /// <summary>
        /// 聚焦期间用户实际键入的原始文本（未经过 Formatter 规整）。
        /// <para>存在的必要性（实测）：① 数值 / 日期这类框在输入 <c>12.</c>、<c>-</c> 等中间态时，
        /// <c>Formatter(Value)</c> 会把它规整成 <c>12</c>，导致字符刚打进去就被抹掉、根本打不出小数；
        /// ② 单向 <c>Value=</c> 用法下父级重渲染会把半截输入按旧值打回。</para>
        /// 只在聚焦期间生效，失焦即丢弃，之后统一由 Formatter(Value) 呈现规整后的值。
        /// </summary>
        private string rawInputText;

        /// <summary>记录 rawInputText 对应的那个 Value，用于判断外部是否合法改了值。</summary>
        private TValue rawInputValue;
        /// <summary>上一次 OnParametersSet 收到的父级 Value 参数；用于区分「父级真的改了值」与
        /// 「@bind-Value 回写滞后导致的陈旧 Value」，避免重渲染把用户输入覆盖回旧值。</summary>
        private TValue _lastParamValue;
        private InputSize effectiveSize = InputSize.Normal;
        private double autosizeHeight;
        private string lastAutosizeText;
        private bool effectiveDisabled;
        private bool autocompleteExplicit;
        private DotNetObjectReference<ElInput<TValue>> compositionRef;
        private bool compositionAttempted;
        private CancellationTokenSource hoverLeaveCts;
        /// <summary>
        /// 插槽前缀实际占宽（含默认间隙），由 JS 测量后回写为 input 的 padding-left；0 = 无前缀插槽。
        /// </summary>
        private int affixPrefixPadding;
        /// <summary>
        /// 插槽后缀实际占宽（含默认间隙），由 JS 测量后回写为 input 的 padding-right；0 = 无后缀插槽。
        /// </summary>
        private int affixSuffixPadding;

        /// <summary>
        /// 鼠标「离开」判定的延迟毫秒数（用于抵消 mouseout / mouseover 的冒泡抖动）
        /// </summary>
        private const int HoverLeaveDelayMs = 60;
        /// <summary>
        /// 输入框类型
        /// </summary>
        [Parameter]
        public InputType Type { get; set; } = InputType.Text;

        /// <summary>
        /// 当输入值错误时是否抛出异常
        /// </summary>
        protected virtual bool ThrowOnInvalidValue { get; } = false;

        /// <summary>
        /// 在前方加入元素
        /// </summary>
        [Parameter]
        public RenderFragment Prepend { get; set; }

        [Parameter]
        public RenderFragment Append { get; set; }

        [Parameter]
        public EventCallback<bool> EnableClearButtonChanged { get; set; }

        /// <summary>
        /// 按钮背景图片
        /// </summary>
        [Parameter]
        public string Image { get; set; }

        /// <summary>
        /// 输入框尺寸
        /// </summary>
        [Parameter]
        public InputSize Size { get; set; } = InputSize.Normal;
        [Parameter]
        public bool EnableClearButton { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool IsDisabled
        {
            get => Disabled;
            set => Disabled = value;
        }

        [Parameter]
        public bool Clearable { get; set; }

        /// <summary>当可清除时是否始终显示清除图标（默认：聚焦/悬停时显示）</summary>
        [Parameter]
        public bool ShowClearIcon { get; set; }

        [Parameter]
        public bool IsClearable
        {
            get => Clearable;
            set => Clearable = value;
        }

        [Parameter]
        public bool Readonly { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public object Maxlength { get; set; }

        [Parameter]
        public object Minlength { get; set; }

        [Parameter]
        public string Resize { get; set; }

        [Parameter]
        public bool Autosize { get; set; }

        [Parameter]
        public string Autocomplete { get; set; } = "off";

        [Parameter]
        public string Form { get; set; }

        /// <summary>
        /// 是否显示切换密码图标。为 true 时输入框即视为密码框（原生 type 在 password / text 之间切换）。
        /// </summary>
        [Parameter]
        public bool ShowPassword { get; set; }

        /// <summary>
        /// 自定义密码切换图标插槽（EP 2.13.6 password-icon），上下文为「密码当前是否明文可见」。
        /// 未设置时使用内置的睁眼 / 闭眼 SVG 图标。
        /// </summary>
        [Parameter]
        public RenderFragment<bool> PasswordIcon { get; set; }

        /// <summary>
        /// 密码明文状态变化时触发
        /// </summary>
        [Parameter]
        public EventCallback<bool> PasswordVisibleChanged { get; set; }

        /// <summary>
        /// 阻止浏览器弹出「记住 / 保存密码」对话框，仅对密码框生效。
        /// 实现方式：原生 autocomplete 置为 new-password，并附加主流密码管理器的忽略标记。
        /// 调用方显式设置了 Autocomplete 时以显式值为准。默认 true。
        /// </summary>
        [Parameter]
        public bool PreventPasswordSave { get; set; } = true;

        /// <summary>
        /// 密码框改走 CSS 掩码（批次86）：原生 <c>type</c> 恒为 <c>text</c>，打点由
        /// <c>-webkit-text-security: disc</c> 负责。浏览器的密码表单分析只认 <c>type=password</c>，
        /// 因此「保存密码」提示与自动填充都不会再命中这一格——这是 <see cref="PreventPasswordSave"/>
        /// 那套 autocomplete 提示做不到的（MDN：对登录用密码框，浏览器会忽略这些提示）。
        /// 默认 false（保持官方行为）。浏览器不支持该属性时自动回落原生 password，绝不明文。
        /// </summary>
        [Parameter]
        public bool MaskedPassword { get; set; }

        /// <summary>
        /// 自定义清除图标类名（EP 2.11.0 clear-icon），默认 el-icon-circle-close。
        /// </summary>
        [Parameter]
        public string ClearIcon { get; set; }

        /// <summary>
        /// 原生 max 属性（type 为 number / date 等时生效）
        /// </summary>
        [Parameter]
        public object Max { get; set; }

        /// <summary>
        /// 原生 min 属性
        /// </summary>
        [Parameter]
        public object Min { get; set; }

        /// <summary>
        /// 原生 step 属性
        /// </summary>
        [Parameter]
        public object Step { get; set; }

        /// <summary>
        /// 自适应 textarea 的最小行数（对应 EP autosize: { minRows }）
        /// </summary>
        [Parameter]
        public int? AutosizeMinRows { get; set; }

        /// <summary>
        /// 自适应 textarea 的最大行数（对应 EP autosize: { maxRows }），为 null 表示不限制
        /// </summary>
        [Parameter]
        public int? AutosizeMaxRows { get; set; }

        /// <summary>
        /// 等价于原生 input 的 aria-label 属性（EP 官方已标记 deprecated，此处保留以兼容官方命名）
        /// </summary>
        [Parameter]
        public string Label { get; set; }

        /// <summary>
        /// 自定义字素计数（EP 2.13.7 count-graphemes）。设置后将绕过原生 maxlength / minlength 约束，
        /// 其返回值用于字数统计与 is-exceed 判定。
        /// </summary>
        [Parameter]
        public Func<string, int> CountGraphemes { get; set; }

        [Parameter]
        public bool ShowWordLimit { get; set; }

        [Parameter]
        public string WordLimitPosition { get; set; } = "inside";

        [Parameter]
        public string InputStyle { get; set; }

        [Parameter]
        public bool Autofocus { get; set; }

        [Parameter]
        public int Rows { get; set; } = 2;

        [Parameter]
        public string AriaLabel { get; set; }

        [Parameter]
        public string Inputmode { get; set; }

        [Parameter]
        public object Tabindex { get; set; } = 0;

        [Parameter]
        public bool ValidateEvent { get; set; } = true;

        /// <summary>
        /// 置 true 时本输入框不再向 <see cref="ElFormItem{TValue}"/>／表单模型写回任何值，只做显示。
        /// 给「组合件把内部文本框当作显示区」的场景用（<c>ElSelect</c> 的下拉框：<c>Value</c> 传的是选项
        /// <b>文本</b>，而字段值是选项 <c>Key</c>；一旦写回就把汉字灌进模型，反射赋值还会把 int/枚举字段直接判成校验失败）。
        /// 默认 false，直接用 <c>ElInput</c> 的页面行为不变。id 仍然注册给表单，<c>label for</c> 与 ScrollToField 不塌。
        /// </summary>
        [Parameter]
        public bool SuppressFormWriteBack { get; set; }

        [Parameter]
        public EventCallback<FocusEventArgs> OnFocus { get; set; }

        [Parameter]
        public EventCallback<FocusEventArgs> OnBlur { get; set; }

        /// <summary>
        /// 鼠标移入输入框时触发（EP mouseenter）
        /// </summary>
        [Parameter]
        public EventCallback<MouseEventArgs> OnMouseEnter { get; set; }

        /// <summary>
        /// 鼠标移出输入框时触发（EP mouseleave）
        /// </summary>
        [Parameter]
        public EventCallback<MouseEventArgs> OnMouseLeave { get; set; }

        [Parameter]
        public EventCallback<TValue> OnInput { get; set; }

        [Parameter]
        public EventCallback<TValue> OnChange { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnClear { get; set; }

        [Parameter]
        public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

        [Parameter]
        public EventCallback OnCompositionStart { get; set; }

        [Parameter]
        public EventCallback OnCompositionUpdate { get; set; }

        [Parameter]
        public EventCallback<TValue> OnCompositionEnd { get; set; }

        /// <summary>
        /// 如何格式化
        /// </summary>
        [Parameter]
        public virtual Func<TValue, string> Formatter { get; set; } = v => Convert.ToString(v);

        [Parameter]
        public virtual Func<string, string> Parser { get; set; }

        /// <summary>
        /// 输入框的值
        /// </summary>
        [Parameter]
        public TValue Value { get; set; }

        [Parameter]
        public TValue ModelValue
        {
            get => Value;
            set => Value = value;
        }

        /// <summary>
        /// 值改变时触发
        /// </summary>
        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }

        [Parameter]
        public EventCallback<TValue> ModelValueChanged { get; set; }

        /// <summary>
        /// Placeholder
        /// </summary>
        [Parameter]
        public virtual string Placeholder { get; set; } = "请输入内容";

        internal bool IsFocus { get; set; }

        internal bool IsMouseOver { get; set; }

        [Parameter]
        public virtual string PrefixIcon { get; set; }

        [Parameter]
        public RenderFragment Prefix { get; set; }

        [Parameter]
        public RenderFragment Suffix { get; set; }

        [Parameter]
        public virtual EventCallback<MouseEventArgs> OnSuffixIconClick { get; set; }
        [Parameter]
        public virtual EventCallback<MouseEventArgs> OnPrefixIconClick { get; set; }
        [Parameter]
        public virtual EventCallback<MouseEventArgs> OnInputClick { get; set; }
        [Parameter]
        public virtual EventCallback<MouseEventArgs> OnClick { get; set; }
        [Parameter]
        public virtual string SuffixIcon { get; set; }
        [Parameter]
        public virtual string AdditionalClearIcon { get; set; }

        internal ElementReference Content { get; set; }

        /// <summary>
        /// HTML 元素引用
        /// </summary>
        public ElementReference InputElement { get; set; }

        protected async Task ClearOnClick(MouseEventArgs e)
        {
            EnableClearButton = true;
            if (EnableClearButtonChanged.HasDelegate)
            {
                await EnableClearButtonChanged.InvokeAsync(EnableClearButton);
            }
            Value = TypeHelper.ChangeType<TValue>(string.Empty);
            rawInputText = null;
            rawInputValue = Value;
            if (FormItem != null && !SuppressFormWriteBack)
            {
                FormItem.Value = Value;
            }
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(Value);
            }
            if (OnInput.HasDelegate)
            {
                await OnInput.InvokeAsync(Value);
            }
            if (OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(Value);
            }
            if (OnClear.HasDelegate)
            {
                await OnClear.InvokeAsync(e);
            }
            SetFieldValue(Value, ValidateEvent);
        }

        protected virtual async Task OnFocusAsync()
        {
            if (Value == null)
            {
                Value = default;
            }
            IsFocus = true;
        }

        protected async Task OnFocusEventAsync(FocusEventArgs e)
        {
            await OnFocusAsync();
            if (OnFocus.HasDelegate)
            {
                await OnFocus.InvokeAsync(e);
            }
        }

        protected async Task OnBlurEventAsync(FocusEventArgs e)
        {
            IsFocus = false;
            // 失焦即丢弃原始文本：此后由 Formatter(Value) 呈现规整后的值（如 12. → 12.5）。
            rawInputText = null;
            rawInputValue = Value;
            if (ValidateEvent)
            {
                SetFieldValue(Value, true);
            }
            if (OnBlur.HasDelegate)
            {
                await OnBlur.InvokeAsync(e);
            }
        }

        protected virtual async Task OnChangeEventArgs(ChangeEventArgs input)
        {
            if (isComposing)
            {
                return;
            }

            var rawText = input.Value == null ? null : Convert.ToString(input.Value);
            try
            {
                var value = input.Value;
                if (value == null)
                {
                    Value = default;
                }
                else if (Formatter != null && Parser != null)
                {
                    Value = (TValue)TypeHelper.ChangeType(Parser(rawText), typeof(TValue));
                }
                else
                {
                    Value = (TValue)TypeHelper.ChangeType(value, typeof(TValue));
                }
            }
            catch (FormatException)
            {
                if (ThrowOnInvalidValue)
                {
                    throw;
                }
                Value = default;
            }

            // 聚焦期间记住用户键入的原文：下一次渲染直接用它，避免 Formatter 把「12.」这类中间态规整掉。
            // 即便解析失败（Value 被置 default）也照样保留原文，让用户可以继续改到合法为止。
            rawInputText = rawText;
            rawInputValue = Value;

            // 写入表单链路（FormItem → Form → Model）。SetFieldValue 内部已经把值落到 FormItem.Value，
            // 其 setter 会经 NotifyValueChanged 把值写回 Model，因此无需再手动 Notify。
            SetFieldValue(Value, false);

            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(Value);
            }
            if (OnInput.HasDelegate)
            {
                await OnInput.InvokeAsync(Value);
            }

            // 关键：没有 FormItem 时也要自己重渲染，否则 nativeValue 不会更新 → DOM value 永远是老值
            StateHasChanged();
        }

        protected async Task OnNativeChangeAsync(ChangeEventArgs input)
        {
            if (ValidateEvent)
            {
                SetFieldValue(Value, false);
                FormItem?.Validate();
                FormItem?.ShowErrorMessage();
            }
            if (OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(Value);
            }
        }

        /// <summary>
        /// 切换密码明文 / 密文显示（EP 的 show-password 行为）
        /// </summary>
        protected async Task TogglePasswordVisibilityAsync(MouseEventArgs e)
        {
            await SetPasswordVisibleAsync(!passwordVisible);
        }

        /// <summary>
        /// 编程方式切换密码明文显示
        /// </summary>
        public async Task SetPasswordVisibleAsync(bool visible)
        {
            if (passwordVisible == visible)
            {
                return;
            }
            passwordVisible = visible;
            await InvokeAsync(StateHasChanged);
            if (PasswordVisibleChanged.HasDelegate)
            {
                await PasswordVisibleChanged.InvokeAsync(passwordVisible);
            }
        }

        protected async Task OnWrapperMouseEnterAsync(MouseEventArgs e)
        {
            CancelPendingMouseLeave();
            if (!IsMouseOver)
            {
                IsMouseOver = true;
                StateHasChanged();
            }
            if (OnMouseEnter.HasDelegate)
            {
                await OnMouseEnter.InvokeAsync(e);
            }
        }

        protected async Task OnWrapperMouseLeaveAsync(MouseEventArgs e)
        {
            CancelPendingMouseLeave();
            var cts = new CancellationTokenSource();
            hoverLeaveCts = cts;
            if (OnMouseLeave.HasDelegate)
            {
                await OnMouseLeave.InvokeAsync(e);
            }
            // Blazor 只有会冒泡的 mouseover / mouseout：鼠标从输入框移进清除图标时，
            // 会先冒泡 mouseout（离开）再冒泡 mouseover（进入）。若立刻置为 false，
            // 图标会被卸载又重建 —— 表现为「鼠标停在图标上图标疯狂闪烁、点不中」。
            // 这里延迟一拍再判定：期间若又移入（含移入内部图标）就取消，等价于 EP 的 mouseenter/mouseleave。
            try
            {
                await Task.Delay(HoverLeaveDelayMs, cts.Token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
            if (!ReferenceEquals(hoverLeaveCts, cts))
            {
                return;
            }
            hoverLeaveCts = null;
            if (IsMouseOver)
            {
                IsMouseOver = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private void CancelPendingMouseLeave()
        {
            if (hoverLeaveCts != null)
            {
                hoverLeaveCts.Cancel();
                hoverLeaveCts.Dispose();
                hoverLeaveCts = null;
            }
        }

        protected async Task OnCompositionStartAsync()
        {
            isComposing = true;
            if (OnCompositionStart.HasDelegate)
            {
                await OnCompositionStart.InvokeAsync();
            }
        }

        protected async Task OnCompositionUpdateAsync()
        {
            if (OnCompositionUpdate.HasDelegate)
            {
                await OnCompositionUpdate.InvokeAsync();
            }
        }

        protected async Task OnCompositionEndAsync(ChangeEventArgs input)
        {
            isComposing = false;
            await OnChangeEventArgs(input);
            if (OnCompositionEnd.HasDelegate)
            {
                await OnCompositionEnd.InvokeAsync(Value);
            }
        }

        /// <summary>
        /// 输入法组合开始（由 dom.js 的 elementInputBindComposition 回调）
        /// </summary>
        [JSInvokable]
        public async Task OnCompositionStartFromJs()
        {
            isComposing = true;
            if (OnCompositionStart.HasDelegate)
            {
                await OnCompositionStart.InvokeAsync();
            }
        }

        /// <summary>
        /// 输入法组合更新（由 dom.js 回调）
        /// </summary>
        [JSInvokable]
        public async Task OnCompositionUpdateFromJs(string data)
        {
            if (OnCompositionUpdate.HasDelegate)
            {
                await OnCompositionUpdate.InvokeAsync();
            }
        }

        /// <summary>
        /// 输入法组合结束（由 dom.js 回调，携带组合后的文本）
        /// </summary>
        [JSInvokable]
        public async Task OnCompositionEndFromJs(string data)
        {
            isComposing = false;
            // JS 回调不一定运行在渲染器的 Dispatcher 上，必须切回去再改状态。
            await InvokeAsync(() => OnChangeEventArgs(new ChangeEventArgs { Value = data }));
            if (OnCompositionEnd.HasDelegate)
            {
                await OnCompositionEnd.InvokeAsync(Value);
            }
        }

        private bool HasCompositionHandlers => OnCompositionStart.HasDelegate
            || OnCompositionUpdate.HasDelegate
            || OnCompositionEnd.HasDelegate;

        /// <summary>
        /// Blazor 不派发 composition* 原生事件，故委托 dom.js 绑定监听器后再回调本组件。
        /// 没有任何组合事件订阅者时完全不碰 JS，避免无谓开销。
        /// </summary>
        private async Task SyncCompositionAsync()
        {
            if (compositionAttempted || !HasCompositionHandlers)
            {
                return;
            }
            compositionAttempted = true;
            try
            {
                compositionRef ??= DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("elementInputBindComposition", InputElement, compositionRef);
            }
            catch
            {
                // 无 JS 环境（bUnit 等）静默降级：组合事件不生效，其余行为不变。
            }
        }

        public ValueTask FocusAsync()
        {
            return InputElement.Dom(JSRuntime).FocusAsync();
        }

        public ValueTask BlurAsync()
        {
            return InputElement.Dom(JSRuntime).BlurAsync();
        }

        public ValueTask SelectAsync()
        {
            return InputElement.Dom(JSRuntime).SelectAsync();
        }

        public async Task ClearAsync()
        {
            await ClearOnClick(null);
        }

        /// <summary>
        /// 重新计算 textarea 高度（EP expose: resizeTextarea）
        /// </summary>
        public async Task ResizeTextareaAsync()
        {
            lastAutosizeText = null;
            await SyncAutoSizeAsync();
        }

        /// <summary>
        /// 密码当前是否明文可见（EP expose: passwordVisible）
        /// </summary>
        public bool PasswordVisible => passwordVisible;

        /// <summary>
        /// 是否处于输入法组合状态（EP expose: isComposing）
        /// </summary>
        public bool IsComposing => isComposing;

        public override void Dispose()
        {
            FormItem?.Form?.UnregisterInput(this);
            CancelPendingMouseLeave();
            compositionRef?.Dispose();
            compositionRef = null;
            base.Dispose();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            await SyncTextSecurityAsync();
            await SyncAutoSizeAsync();
            await SyncCompositionAsync();
            await SyncAffixPaddingAsync();
            if (effectiveDisabled)
            {
                await InputElement.Dom(JSRuntime).SetDisabledAsync(effectiveDisabled);
            }
        }

        /// <summary>
        /// 掩码档的能力探测：只在开了 MaskedPassword 的实例上跑，且每实例一次。
        /// 探测失败（无 JS 环境如 bUnit）按"不支持"处理 ⇒ 保持原生 password，宁可少掉抑制效果也不明文显示密码。
        /// </summary>
        private async Task SyncTextSecurityAsync()
        {
            if (!MaskedPassword || textSecurityProbed)
            {
                return;
            }

            textSecurityProbed = true;
            try
            {
                maskedPasswordSupported = await JSRuntime.InvokeAsync<bool>("elementSupportsTextSecurity");
            }
            catch
            {
                maskedPasswordSupported = false;
            }

            if (maskedPasswordSupported)
            {
                await InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// 前缀 / 后缀插槽（Prefix、Suffix RenderFragment）是绝对定位渲染的，
        /// 官方只用固定的 30px padding，插槽内容一宽（如 "Http://"）就会压在输入文字上。
        /// 这里让 JS 只负责「量」，量出来的像素回到 C# 写成 input 的内联 padding，
        /// 保证样式仍由 Blazor 单一真源下发。无 JS 环境（bUnit）静默降级为官方默认 padding。
        /// </summary>
        private async Task SyncAffixPaddingAsync()
        {
            if (Prefix is null && Suffix is null)
            {
                return;
            }

            try
            {
                var measured = await JSRuntime.InvokeAsync<int[]>("elementInputMeasureAffix", InputElement);
                var prefixPadding = measured is { Length: 2 } ? Math.Max(measured[0], 0) : 0;
                var suffixPadding = measured is { Length: 2 } ? Math.Max(measured[1], 0) : 0;

                if (prefixPadding != affixPrefixPadding || suffixPadding != affixSuffixPadding)
                {
                    affixPrefixPadding = prefixPadding;
                    affixSuffixPadding = suffixPadding;
                    StateHasChanged();
                }
            }
            catch
            {
                // 无 JS 环境不做补偿，沿用 .el-input--prefix / --suffix 的默认 padding。
            }
        }

        /// <summary>
        /// textarea 自适应高度。只让 JS 负责「量」，量出来的像素回到这里写成 style，
        /// 保证 Blazor 始终是样式的唯一真源（否则下一次 diff 会把行内 height 冲掉）。
        /// 无 JS 环境（bUnit）下静默退化成 Rows 固定高度。
        /// </summary>
        private async Task SyncAutoSizeAsync()
        {
            if (!Autosize || Type != InputType.Textarea)
            {
                return;
            }

            var text = Formatter(Value) ?? string.Empty;
            if (string.Equals(text, lastAutosizeText, StringComparison.Ordinal))
            {
                return;
            }
            lastAutosizeText = text;

            try
            {
                var measured = await JSRuntime.InvokeAsync<double>("elementTextareaAutoHeight", InputElement, AutosizeMinRows ?? Rows, AutosizeMaxRows ?? 0);
                if (measured > 0 && Math.Abs(measured - autosizeHeight) > 0.5)
                {
                    autosizeHeight = measured;
                    StateHasChanged();
                }
            }
            catch
            {
                // 无 JS 环境不做任何补偿，保持行内不含 height。
            }
        }

        protected override void FormItem_OnReset(object value, bool requireRerender)
        {
            if (value == null)
            {
                Value = default;
            }
            else
            {
                Value = (TValue)TypeHelper.ChangeType(value, typeof(TValue));
            }

            if (ValueChanged.HasDelegate)
            {
                _ = ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                _ = ModelValueChanged.InvokeAsync(Value);
            }
            else
            {
                StateHasChanged();
            }
        }

        /// <summary>
        /// 记录调用方是否显式设置了 Autocomplete：只有显式赋值才覆盖密码框防保存的默认值。
        /// </summary>
        public override Task SetParametersAsync(ParameterView parameters)
        {
            autocompleteExplicit = false;
            foreach (var parameter in parameters)
            {
                if (string.Equals(parameter.Name, nameof(Autocomplete), StringComparison.Ordinal))
                {
                    autocompleteExplicit = true;
                    break;
                }
            }
            return base.SetParametersAsync(parameters);
        }

        /// <summary>
        /// 渲染到 DOM 的文本。聚焦且存在原始键入文本时用原文（保住「12.」这类中间态），
        /// 否则用 Formatter(Value) 的规整结果。
        /// </summary>
        private string DisplayText
        {
            get
            {
                if (IsFocus && rawInputText != null)
                {
                    return rawInputText;
                }

                return Formatter != null ? Formatter(Value) ?? string.Empty : Convert.ToString(Value) ?? string.Empty;
            }
        }

        /// <summary>
        /// 批次87：掩码档能力探测提前到首帧之前（WASM 的 IJSInProcessRuntime 同步调用）。
        /// Chrome 对密码格的分类发生在场元素首次入树的时刻并终身记牢，事后改属性不撤销（axis1-7 实测），
        /// 批次86 的「先原生 password、异步探测后再翻面」出生即被分类 ⇒ 离开页的挂锁图标照出。
        /// 所以掩码场必须出生即 type=text + 内联掩码。非 InProcess 运行时（Server / bUnit）投不了
        /// IJSInProcessRuntime ⇒ 保持 false，走 OnAfterRenderAsync 的异步探测，出生仍原生 password（不泄露明文）。
        /// </summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();
            if (MaskedPassword && !textSecurityProbed)
            {
                try
                {
                    maskedPasswordSupported = ((IJSInProcessRuntime)JSRuntime).Invoke<bool>("elementSupportsTextSecurity");
                    textSecurityProbed = true;
                }
                catch
                {
                    // 非 InProcess 运行时：留给 OnAfterRenderAsync 的异步探测。
                }
            }
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            // 值被外部合法改写（清除按钮 / 表单重置 / 调用方赋值 / 数据源刷新）时放弃原始文本；
            // 若新的 Value 正好等于原文本解析出来的值（@bind-Value、ValueChanged 的正常回路），则继续保留。
            // ⚠️ 聚焦期间绝不因外部 Value 与已键入内容不一致而丢弃原文：单向绑定（Value= + OnChange 失焦才保存）的
            // 场景下，父级在键入过程中重渲染会把「尚未保存的旧值」作为 Value 回传，此时 Value 必然不等于已键入内容，
            // 若在此清空 rawInputText 就会让用户键入的字符瞬间被旧值覆盖（即「输入完马上变回原来的东西」的 bug）。
            // 聚焦态下始终以用户键入的原文为准，失焦（OnBlurEventAsync）时再统一丢弃并由 OnChange 写回数据源。
            if (rawInputText != null && !IsFocus && !EqualityComparer<TValue>.Default.Equals(Value, rawInputValue))
            {
                rawInputText = null;
            }
            effectiveDisabled = Disabled || (FormItem?.Form?.Disabled ?? false);
            effectiveSize = Size == InputSize.Normal
                ? FormItem?.Size ?? FormItem?.Form?.EffectiveSize ?? ResolveInputSize(InputSize.Normal)
                : Size;
            Id = string.IsNullOrWhiteSpace(Id) && FormItem != null ? generatedInputId : Id;
            if (FormItem?.Form != null)
            {
                FormItem.Form.RegisterInput(FormItem.Name, Id, this, FormItem);
            }
            var inputSizeCssValue = GetSizeCssValue(effectiveSize);
            wrapperClsBuilder = HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add(Type == InputType.Textarea ? "el-textarea" : "el-input", Cls)
                .AddIf(inputSizeCssValue != null, Type == InputType.Textarea ? $"el-textarea--{inputSizeCssValue}" : $"el-input--{inputSizeCssValue}")
                .AddIf(HasSuffix, "el-input--suffix")
                .AddIf(HasPrefix, "el-input--prefix")
                .AddIf(effectiveDisabled, "is-disabled")
                .AddIf(IsFocus, "is-focus")
                .AddIf(IsExceed, "is-exceed")
                .AddIf(Prepend != null || Append != null, "el-input-group")
                .AddIf(Prepend != null, "el-input-group--prepend")
                .AddIf(Append != null, "el-input-group--append");
            if (FormItem == null)
            {
                return;
            }
            if (SuppressFormWriteBack)
            {
                // 两个方向都要跳过：写回会把显示文本灌进模型，反向采纳 FormItem.Value 又会让框里
                // 显示裸键（"0"）而不是选项文本（"正常"）。_lastParamValue 照常跟住，避免下次
                // 取消抑制时把「本次显示文本的变化」误判成父级赋值。
                _lastParamValue = Value;
                return;
            }
            if (FormItem.OriginValueHasRendered)
            {
                // 后续重渲染：区分「父级真的改了 Value」与「@bind-Value 回写滞后导致的陈旧 Value」。
                // 仅当父级 Value 相对上一次参数确实发生变化（真正的外部赋值 / 重置）时才采纳父级值；
                // 否则（本组件/表单内部自行触发的重渲染，父级 Value 尚未经 ValueChanged 追上）保留表单
                // 已记录的当前值（FormItem.Value），避免把用户刚输入的内容覆盖回初始值（抽屉/对话框
                // 表单字段回滚 bug 的根因：StateHasChanged 触发的重渲染拿到的父级 Value 仍是旧值，
                // 旧分支会把它写回 FormItem.Value → 模型，导致输入被回滚）。
                bool parentValueChanged = !TypeHelper.Equal(Value, _lastParamValue);
                if (parentValueChanged && Value != null && !TypeHelper.Equal(Value, FormItem.Value))
                {
                    FormItem.Value = Value;
                }
                Value = FormItem.Value ?? Value;
                _lastParamValue = Value;
                return;
            }
            FormItem.OriginValueHasRendered = true;
            if (FormItem.Form?.Values.Any() ?? false)
            {
                Value = FormItem.OriginValue;
            }
            SetFieldValue(Value, false);
            _lastParamValue = Value;
        }

        /// <summary>
        /// 抑制开关为真时不向表单同步：见 <see cref="SuppressFormWriteBack"/>。
        /// 覆盖基类的唯一写回入口，键入／失焦／清空三条路径一并拦住。
        /// </summary>
        protected override void SetFieldValue(TValue value, bool validate)
        {
            if (SuppressFormWriteBack)
            {
                return;
            }
            base.SetFieldValue(value, validate);
        }

        /// <summary>
        /// 是否为密码框：ShowPassword 为真即为密码框（无需再显式指定 Type=Password）
        /// </summary>
        protected bool IsPasswordField => ShowPassword || Type == InputType.Password;

        /// <summary>
        /// 本实例的 -webkit-text-security 探测结果。false = 尚未探到或浏览器不支持 ⇒ 按原生 password 渲染。
        /// 刻意不做成静态：静态会被无 JS 环境（bUnit、预渲染）那一次失败永久污染，
        /// 真实浏览器里的后续实例就再也开不了掩码档。
        /// </summary>
        private bool maskedPasswordSupported;

        private bool textSecurityProbed;

        /// <summary>
        /// 本实例是否走掩码档：调用方开了 MaskedPassword、确实是密码框、且浏览器支持该属性。
        /// </summary>
        protected bool UseMaskedPassword => MaskedPassword && IsPasswordField && maskedPasswordSupported;

        protected string NativeInputType => UseMaskedPassword
            ? "text"
            : IsPasswordField
                ? (passwordVisible ? "text" : "password")
                : Type.ToString().ToLower();

        /// <summary>
        /// 密码框且未显式设置 autocomplete 时，用 new-password 抑制浏览器的记住 / 保存密码提示。
        /// 批次87：掩码档（UseMaskedPassword）不再发 new-password —— 该值本身是 Chrome 密码分类的一张票，
        /// 掩码场要走「纯文本 + 内联掩码」的零信号出生形态；原生 password 档（Firefox 等不支持掩码的浏览器）保留。
        /// </summary>
        protected string NativeAutocomplete => IsPasswordField && PreventPasswordSave && !autocompleteExplicit && !UseMaskedPassword
            ? "new-password"
            : Autocomplete;

        protected bool IsInputDisabled => effectiveDisabled;

        protected bool IsWordLimitVisible => ShowWordLimit
            && Maxlength != null
            && (Type == InputType.Text || Type == InputType.Textarea)
            && !effectiveDisabled
            && !Readonly
            && !IsPasswordField;

        /// <summary>
        /// 字数统计是否显示在输入框外侧（EP 2.11.5 word-limit-position="outside"）
        /// </summary>
        protected bool HasWordLimitOutside => IsWordLimitVisible && string.Equals(WordLimitPosition, "outside", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// EP 规则：show-password 且未禁用未只读，并且「有值」或「已聚焦」时才显示切换图标
        /// </summary>
        protected bool ShowPasswordToggle => IsPasswordField
            && !effectiveDisabled
            && !Readonly
            && (IsFocus || !string.IsNullOrEmpty(Formatter(Value)));

        protected bool HasPrefix => Prefix != null || !string.IsNullOrWhiteSpace(PrefixIcon);

        protected bool HasSuffix => Suffix != null || Clearable || !string.IsNullOrWhiteSpace(SuffixIcon) || ShowPasswordToggle || (IsWordLimitVisible && !HasWordLimitOutside) || HasValidationStatusIcon;

        protected object NativeMaxlength => CountGraphemes != null ? null : Maxlength;

        protected object NativeMinlength => CountGraphemes != null ? null : Minlength;

        protected string EffectiveAriaLabel => !string.IsNullOrWhiteSpace(AriaLabel) ? AriaLabel : Label;

        /// <summary>
        /// 密码框防保存时附加的密码管理器忽略标记（LastPass / 1Password）
        /// </summary>
        protected string NativeLpIgnore => IsPasswordField && PreventPasswordSave && !autocompleteExplicit ? "true" : null;

        protected string InputElementClass => Type == InputType.Textarea
            ? HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-textarea__inner")
                .AddIf(IsFocus, "is-focus")
                .AddIf(Clearable, "is-clearable")
                .ToString()
            : HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-input__inner")
                .ToString();

        protected int TextLength => CountGraphemes != null
            ? CountGraphemes(Formatter(Value) ?? string.Empty)
            : (Formatter(Value)?.Length ?? 0);

        protected bool IsExceed => IsWordLimitVisible && int.TryParse(Convert.ToString(Maxlength), out var max) && TextLength > max;

        /// <summary>
        /// 字数统计文案。EP 用 <c>t('el.input.characters')</c>（zh 为 <c>{count} / {max} 个字符</c>）；
        /// 本仓此前只显示 <c>3 / 10</c>，所以 <b>fallback 保持没有后缀的那版模板</b>，接了词条才变成 EP 措辞。
        /// </summary>
        protected string CharactersText => Lang.T(
            "el.input.characters", "{count} / {max}", ("count", TextLength), ("max", Maxlength));

        protected bool HasValidationStatusIcon => FormItem != null && (FormItem.Form?.StatusIcon ?? false) && FormItem.ValidationResult != null;

        protected string ValidationStatusIconClass
        {
            get
            {
                var state = FormItem?.ValidateStatus;
                var icon = state == "success" ? "el-icon-success" : state == "validating" ? "el-icon-loading is-loading" : "el-icon-error";
                return $"el-input__icon el-input__validateIcon {icon}";
            }
        }

        protected override bool ShouldRender()
        {
            return true;
        }

        private static string GetSizeCssValue(InputSize size) => size switch
        {
            InputSize.Large => "large",
            InputSize.Small => "small",
            _ => null
        };
    }
}
