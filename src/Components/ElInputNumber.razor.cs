using ElementPlus.Lang;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElInputNumber : ElementFieldComponentBase<decimal?>
    {
        private static long inputIdSeed;
        private readonly string generatedInputId = $"el-input-number-{Interlocked.Increment(ref inputIdSeed)}";
        private HtmlPropertyBuilder wrapperClsBuilder;
        private InputSize effectiveSize = InputSize.Normal;
        private bool effectiveDisabled;
        private string inputText;
        private bool hasInvalidInput;

        [Parameter]
        public decimal? Value { get; set; }

        [Parameter]
        public decimal? ModelValue
        {
            get => Value;
            set => Value = value;
        }

        [Parameter]
        public EventCallback<decimal?> ValueChanged { get; set; }

        [Parameter]
        public EventCallback<decimal?> ModelValueChanged { get; set; }

        [Parameter]
        public decimal? Min { get; set; }

        [Parameter]
        public decimal? Max { get; set; }

        [Parameter]
        public decimal Step { get; set; } = 1;

        [Parameter]
        public bool StepStrictly { get; set; }

        [Parameter]
        public int? Precision { get; set; }

        /// <summary>是否显示步进按钮。默认 <c>false</c>（0.10.31 起收敛：
        /// 业务表单以键盘输入为主，步进按钮在窄列里会挤爆输入框；对齐官方 demo 等
        /// 需要步进视觉的场景显式传 <c>true</c> 即可，键盘上下键步进不受影响。</summary>
        [Parameter]
        public bool Controls { get; set; } = false;

        [Parameter]
        public string ControlsPosition { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool IsDisabled
        {
            get => Disabled;
            set => Disabled = value;
        }

        [Parameter]
        public bool Readonly { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public InputSize Size { get; set; } = InputSize.Normal;

        [Parameter]
        public string Placeholder { get; set; }

        [Parameter]
        public string Autocomplete { get; set; } = "off";

        [Parameter]
        public string Inputmode { get; set; } = "decimal";

        [Parameter]
        public object Tabindex { get; set; } = 0;

        [Parameter]
        public string AriaLabel { get; set; }

        [Parameter]
        public string AriaLabelledby { get; set; }

        [Parameter]
        public string AriaDescribedby { get; set; }

        [Parameter]
        public bool ValidateEvent { get; set; } = true;

        /// <summary>是否可清除：悬停时浮现清除图标（对齐 el-input 的 clearable 惯例）(2.16.0)</summary>
        [Parameter]
        public bool Clearable { get; set; }

        /// <summary>
        /// 常驻显示清除图标。<b>不依赖 <see cref="Clearable"/></b> —— 单独设 true 就必须生效
        /// （旧实现把它 AND 在 Clearable 之后，导致这个参数单独设置时完全无效）。
        /// </summary>
        [Parameter]
        public bool ShowClearIcon { get; set; }

        /// <summary>
        /// 递减按钮的图标插槽（对齐 EP <c>decrease-icon</c>）：给定时整体替换默认图标，
        /// 不给定时保持原有 <c>el-icon-minus</c> / <c>el-icon-arrow-down</c>。
        /// </summary>
        [Parameter]
        public RenderFragment DecreaseIcon { get; set; }

        /// <summary>递增按钮的图标插槽（对齐 EP <c>increase-icon</c>）。</summary>
        [Parameter]
        public RenderFragment IncreaseIcon { get; set; }

        /// <summary>
        /// 递减按钮的可读名称。取值优先级：<b>本参数 &gt; 库级词条 <c>el.inputNumber.decrease</c> &gt; 默认 <c>decrease</c></b>。
        /// EP 侧是 <c>:aria-label="t('el.inputNumber.decrease')"</c>，本仓用注入的 <c>ILang</c> 兑现同一意图，
        /// 词条值与 EP 权威 locale 一致（zh-CN 为「减少数值」，en-US 为 "decrease number"）。
        /// 传 null/空白表示「不覆盖」，继续走词条与默认值 —— 空的 aria-label 是无障碍反模式。
        /// </summary>
        [Parameter]
        public string DecreaseAriaLabel { get; set; }

        /// <summary>递增按钮的可读名称，规则同 <see cref="DecreaseAriaLabel"/>（词条键 <c>el.inputNumber.increase</c>，默认 <c>increase</c>）。</summary>
        [Parameter]
        public string IncreaseAriaLabel { get; set; }

        private const string DefaultDecreaseAriaLabel = "decrease";
        private const string DefaultIncreaseAriaLabel = "increase";

        // 词条键沿用 EP 权威 locale 的同名键（zh-cn.ts / en.ts 里就是 el.inputNumber.decrease / increase）
        private const string DecreaseAriaLabelKey = "el.inputNumber.decrease";
        private const string IncreaseAriaLabelKey = "el.inputNumber.increase";

        // 优先级：宿主显式参数 > 已载入的库级词条 > 接入 i18n 之前就有的英文字面量。
        // 最后一级很关键 —— 词条 404、还没载入、宿主没注册 HttpClient 时，DOM 必须和改动前一字不变。
        // 这套优先级统一在 Lang.Text 里（与 EP 的 props.x || t('el.y.x') 同形）。
        private string EffectiveDecreaseAriaLabel =>
            this.Lang.Text(DecreaseAriaLabelKey, DecreaseAriaLabel, DefaultDecreaseAriaLabel);

        private string EffectiveIncreaseAriaLabel =>
            this.Lang.Text(IncreaseAriaLabelKey, IncreaseAriaLabel, DefaultIncreaseAriaLabel);

        /// <summary>
        /// 显示格式化（对齐 EP <c>formatter</c>，签名同为「字符串进字符串出」）：
        /// 只在把值渲染成文本时套用；用户正在输入的原文不被二次格式化（否则光标与内容会错乱）。
        /// 回调抛异常时退回未格式化的文本，绝不让整页渲染崩掉。
        /// </summary>
        [Parameter]
        public Func<string, string> Formatter { get; set; }

        /// <summary>
        /// 输入解析（对齐 EP <c>parser</c>）：把带格式的文本还原成可解析的数字字符串，再走既有解析链路。
        /// 解析结果仍非法时与 EP 一致：保持上一次的值与显示，不写入非法值。
        /// </summary>
        [Parameter]
        public Func<string, string> Parser { get; set; }

        private ElementReference inputElement;

        /// <summary>
        /// 获取输入框焦点（对齐 EP 暴露的 <c>focus()</c>）。
        /// </summary>
        public async Task FocusAsync() => await inputElement.Dom(JSRuntime).FocusAsync();

        /// <summary>
        /// 释放输入框焦点（对齐 EP 暴露的 <c>blur()</c>）。
        /// </summary>
        public async Task BlurAsync() => await inputElement.Dom(JSRuntime).BlurAsync();

        /// <summary>自定义清除图标 CSS class (2.16.0)</summary>
        [Parameter]
        public string ClearIcon { get; set; } = "el-icon-circle-close";

        /// <summary>
        /// 清空后落到的值（对齐 EP <c>value-on-clear</c>）：<c>null</c>（默认）清成 null；
        /// <c>"min"</c> / <c>"max"</c> 清到对应边界（未设该边界时退回 null）；其余按数字解析。
        /// </summary>
        [Parameter]
        public string ValueOnClear { get; set; }

        /// <summary>
        /// 数字的水平对齐（对齐 EP <c>align</c>）：<c>left</c> / <c>right</c> / <c>center</c>，
        /// 默认 <c>center</c> —— 与 EP 一致，也与本仓基底 CSS 里
        /// <c>.el-input-number .el-input__inner{text-align:center}</c> 的既有观感一致，
        /// 因此不传时零视觉变化。非法值按 <c>center</c> 处理（不抛、不静默丢样式）。
        /// </summary>
        [Parameter]
        public string Align { get; set; } = "center";

        /// <summary>归一化后的对齐值，用于 <c>is-{align}</c> class。</summary>
        private string EffectiveAlign => Align?.Trim().ToLowerInvariant() switch
        {
            "left" => "left",
            "right" => "right",
            _ => "center",
        };

        /// <summary>是否显示清除图标：常驻（ShowClearIcon）与悬停（Clearable）是两条独立通路</summary>
        public bool ShouldShowClearIcon => !IsDisabled && !Readonly && Value.HasValue
            && (ShowClearIcon || (Clearable && isHovering));

        private bool isHovering;

        private const int HoverLeaveDelayMs = 60;

        private CancellationTokenSource hoverLeaveCts;

        [Parameter]
        public EventCallback<decimal?> OnChange { get; set; }

        [Parameter]
        public EventCallback<decimal?> OnInput { get; set; }

        [Parameter]
        public EventCallback<FocusEventArgs> OnFocus { get; set; }

        [Parameter]
        public EventCallback<FocusEventArgs> OnBlur { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            effectiveDisabled = Disabled || (FormItem?.Form?.Disabled ?? false);
            effectiveSize = Size == InputSize.Normal
                ? FormItem?.Size ?? FormItem?.Form?.EffectiveSize ?? ResolveInputSize(InputSize.Normal)
                : Size;
            Id = string.IsNullOrWhiteSpace(Id) ? ResolveAttributeId() ?? generatedInputId : Id;
            if (FormItem?.Form != null)
            {
                FormItem.Form.RegisterInput(FormItem.Name, Id, this, FormItem);
            }
            var sizeCssValue = GetSizeCssValue(effectiveSize);
            wrapperClsBuilder = HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-input-number", Cls)
                .AddIf(effectiveDisabled, "is-disabled")
                .AddIf(IsWithoutControls, "is-without-controls")
                .AddIf(ControlsPosition == "right", "is-controls-right")
                // EP：根节点 is-{align}（left|right|center），覆盖 .el-input__inner 的 text-align
                .Add($"is-{EffectiveAlign}")
                .AddIf(sizeCssValue != null, $"el-input-number--{sizeCssValue}");

            if (FormItem != null && !FormItem.OriginValueHasRendered)
            {
                FormItem.OriginValueHasRendered = true;
                if (FormItem.Form?.Values.Any() ?? false)
                {
                    Value = ConvertToDecimal(FormItem.OriginValue);
                }
                SetFieldValue(Value, false);
            }

            if (!hasInvalidInput)
            {
                inputText = DisplayTextFor(Value);
            }
        }

        protected override void FormItem_OnReset(object value, bool requireRerender)
        {
            Value = ConvertToDecimal(value);
            inputText = DisplayTextFor(Value);
            hasInvalidInput = false;
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

        private async Task OnInputAsync(ChangeEventArgs e)
        {
            inputText = Convert.ToString(e.Value);
            if (!TryParseInput(inputText, out var parsed))
            {
                hasInvalidInput = true;
                return;
            }

            hasInvalidInput = false;
            Value = parsed;
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
        }

        private async Task OnChangeAsync(ChangeEventArgs e)
        {
            // 0.10.28 起：Readonly/Disabled 时不提交文本。readonly 输入框聚焦后失焦，
            // 浏览器不派发 change；但这里统一兜底，杜绝任何路径把空值/零值套用 Min/Max
            // 回写（典型：只读单价 0 被 Normalize 夹成 Min=1 后写回 ValueChanged）。
            if (effectiveDisabled || Readonly)
            {
                return;
            }
            await CommitTextAsync(Convert.ToString(e.Value), ValidateEvent, notifyChange: true);
        }

        private async Task OnFocusAsync(FocusEventArgs e)
        {
            if (OnFocus.HasDelegate)
            {
                await OnFocus.InvokeAsync(e);
            }
        }

        private async Task OnBlurAsync(FocusEventArgs e)
        {
            // 0.10.28 起：Readonly/Disabled 时失焦不触发 CommitTextAsync —— 只读字段聚焦后
            // 移走鼠标，inputText 会被 CommitTextAsync → Normalize 把 0 夹到 Min 值并回写
            // （单价 Readonly + Min=1 时「点击后鼠标移走变 1」的根因）。只读/禁用字段保持原值。
            if (effectiveDisabled || Readonly)
            {
                if (OnBlur.HasDelegate)
                {
                    await OnBlur.InvokeAsync(e);
                }
                return;
            }
            await CommitTextAsync(inputText, ValidateEvent, notifyChange: false);
            if (OnBlur.HasDelegate)
            {
                await OnBlur.InvokeAsync(e);
            }
        }

        private async Task OnKeyDownAsync(KeyboardEventArgs e)
        {
            if (effectiveDisabled || Readonly)
            {
                return;
            }

            if (HasModifierKey(e))
            {
                return;
            }

            if (e.Key == "ArrowUp")
            {
                await IncreaseAsync();
            }
            else if (e.Key == "ArrowDown")
            {
                await DecreaseAsync();
            }
            else if (e.Key == "PageUp")
            {
                await StepValueAsync(GetPageStep());
            }
            else if (e.Key == "PageDown")
            {
                await StepValueAsync(-GetPageStep());
            }
            else if (e.Key == "Home" && Min.HasValue)
            {
                await CommitValueAsync(Min.Value, ValidateEvent, notifyChange: true);
            }
            else if (e.Key == "End" && Max.HasValue)
            {
                await CommitValueAsync(Max.Value, ValidateEvent, notifyChange: true);
            }
            else if (e.Key == "Enter")
            {
                await CommitTextAsync(inputText, ValidateEvent, notifyChange: true);
            }
        }

        private static bool HasModifierKey(KeyboardEventArgs e)
        {
            return e.AltKey || e.CtrlKey || e.MetaKey || e.ShiftKey;
        }

        private async Task OnControlKeyDownAsync(KeyboardEventArgs e, Func<Task> action)
        {
            if (e.Key != "Enter" && e.Key != " ")
            {
                return;
            }

            await action();
        }

        private async Task OnWrapperMouseOverAsync()
        {
            CancelPendingMouseLeave();
            if (!isHovering)
            {
                isHovering = true;
                StateHasChanged();
            }
            await Task.CompletedTask;
        }

        private async Task OnWrapperMouseOutAsync()
        {
            CancelPendingMouseLeave();
            var cts = new CancellationTokenSource();
            hoverLeaveCts = cts;
            // Blazor 只有会冒泡的 mouseover / mouseout：鼠标从输入区移进清除图标时，
            // 会先冒泡 mouseout（离开输入区）再冒泡 mouseover（进入图标）。若立刻置 false，
            // 清除图标会被卸载又重建 —— 表现为「鼠标停在图标上疯狂闪烁、点不中」。
            // 延迟一拍再判定：期间若又移入（含移入内部图标）就取消，等价于 EP 的 mouseenter/mouseleave。
            // 复用 ElInput 已验证的同款实现（见 ElInput.razor.cs.OnWrapperMouseLeaveAsync）。
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
            if (isHovering)
            {
                isHovering = false;
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

        // ---- 按住连发（对齐 EP v-repeat-click；常量取自 directives/repeat-click/index.ts） ----
        /// <summary>EP <c>REPEAT_DELAY</c>：按住多久之后开始连发。</summary>
        private const int RepeatDelayMs = 600;

        /// <summary>EP <c>REPEAT_INTERVAL</c>：连发间隔。</summary>
        private const int RepeatIntervalMs = 100;

        private CancellationTokenSource repeatCts;

        /// <summary>本轮按住是否已经真的连发过；连发过就要吞掉随后到来的 click，避免末尾多走一步。</summary>
        private bool repeatEngaged;

        /// <summary>
        /// 按下步进按钮：EP 语义是 mousedown 先立即走一步、按住超过 <see cref="RepeatDelayMs"/> 后每
        /// <see cref="RepeatIntervalMs"/> 连发。这里把"立即那一步"仍交给 click（Blazor 拿不到
        /// document 级 mouseup，靠 click 更稳），本方法只负责"按住才连发"，因此快速点击的净效果仍是一步。
        /// </summary>
        private async Task BeginHoldAsync(Func<Task> step)
        {
            EndHold();
            var cts = new CancellationTokenSource();
            repeatCts = cts;
            repeatEngaged = false;
            try
            {
                await Task.Delay(RepeatDelayMs, cts.Token);
                while (!cts.Token.IsCancellationRequested)
                {
                    repeatEngaged = true;
                    await step();
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(RepeatIntervalMs, cts.Token);
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        /// <summary>松开/移出按钮即停止连发（EP 用 document mouseup，这里由 mouseup + mouseleave 覆盖）。</summary>
        private void EndHold()
        {
            if (repeatCts != null)
            {
                repeatCts.Cancel();
                repeatCts.Dispose();
                repeatCts = null;
            }
        }

        /// <summary>EP 同款：只响应鼠标左键（<c>evt.button !== 0 → return</c>）。</summary>
        private Task OnControlMouseDownAsync(MouseEventArgs e, Func<Task> step)
            => e.Button == 0 ? BeginHoldAsync(step) : Task.CompletedTask;

        /// <summary>
        /// 步进按钮的 click 入口：若刚才已经因按住而连发过，就吞掉这一次 click（否则会多走一步），否则正常走一步。
        /// </summary>
        private async Task StepOnClickAsync(Func<Task> step)
        {
            if (repeatEngaged)
            {
                repeatEngaged = false;
                return;
            }
            await step();
        }

        public override void Dispose()
        {
            EndHold();
            base.Dispose();
        }

        private async Task ClearAsync()
        {
            if (effectiveDisabled || Readonly)
            {
                return;
            }

            // 对齐 EP value-on-clear：清空未必清成 null，可以落到 min / max / 指定数字
            var cleared = ResolveClearedValue();
            Value = cleared;
            inputText = DisplayTextFor(cleared);
            hasInvalidInput = false;
            SetFieldValue(cleared, false);
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(cleared);
            }
            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(cleared);
            }
            if (OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(cleared);
            }
        }

        /// <summary>
        /// 解析 <see cref="ValueOnClear"/>：空 → null；"min" / "max" → 对应边界（未设该边界则退回 null）；
        /// 其余按当前区域设置解析为数字（越界值由既有 Normalize 夹回）。
        /// </summary>
        private decimal? ResolveClearedValue()
        {
            var raw = ValueOnClear?.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                return null;
            }
            if (string.Equals(raw, "min", StringComparison.OrdinalIgnoreCase))
            {
                return Min;
            }
            if (string.Equals(raw, "max", StringComparison.OrdinalIgnoreCase))
            {
                return Max;
            }
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed))
            {
                return parsed;
            }
            return null;
        }

        private Task IncreaseAsync()
        {
            return StepValueAsync(Step);
        }

        private Task DecreaseAsync()
        {
            return StepValueAsync(-Step);
        }

        private async Task StepValueAsync(decimal step)
        {
            if (effectiveDisabled || Readonly || (step < 0 && IsDecreaseDisabled) || (step > 0 && IsIncreaseDisabled))
            {
                return;
            }

            var source = Value ?? ResolveStepStartValue(step);
            await CommitValueAsync(source + step, ValidateEvent, notifyChange: true);
        }

        private async Task CommitValueAsync(decimal? value, bool validate, bool notifyChange)
        {
            var normalized = Normalize(value);
            if (TypeHelper.Equal(Value, normalized) && inputText == FormatValue(normalized))
            {
                hasInvalidInput = false;
                SetFieldValue(normalized, validate);
                return;
            }

            Value = normalized;
            inputText = DisplayTextFor(Value);
            hasInvalidInput = false;
            SetFieldValue(Value, validate);
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(Value);
            }
            if (notifyChange && OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(Value);
            }
        }

        private decimal? Normalize(decimal? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            var result = value.Value;
            if (StepStrictly && Step > 0)
            {
                result = Math.Round(result / Step, 0, MidpointRounding.AwayFromZero) * Step;
            }
            if (Precision.HasValue)
            {
                result = Math.Round(result, Precision.Value, MidpointRounding.AwayFromZero);
            }
            if (Min.HasValue && result < Min.Value)
            {
                result = Min.Value;
            }
            if (Max.HasValue && result > Max.Value)
            {
                result = Max.Value;
            }
            return result;
        }

        private async Task CommitTextAsync(string text, bool validate, bool notifyChange)
        {
            if (!TryParseInput(text, out var parsed))
            {
                inputText = DisplayTextFor(Value);
                hasInvalidInput = false;
                SetFieldValue(Value, validate);
                return;
            }

            await CommitValueAsync(parsed, validate, notifyChange);
        }

        private bool TryParseInput(string value, out decimal? result)
        {
            if (Parser != null && !string.IsNullOrWhiteSpace(value))
            {
                try
                {
                    value = Parser(value) ?? string.Empty;
                }
                catch
                {
                    result = null;
                    return false;   // 解析回调异常按「非法输入」处理：保持上一次的显示与值
                }
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                result = null;
                return true;
            }
            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantValue))
            {
                result = invariantValue;
                return true;
            }
            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var currentValue))
            {
                result = currentValue;
                return true;
            }
            result = null;
            return false;
        }

        private decimal ResolveStepStartValue(decimal step)
        {
            if (step > 0 && Min.HasValue)
            {
                return Min.Value;
            }

            if (step < 0 && Max.HasValue)
            {
                return Max.Value;
            }

            return 0;
        }

        private decimal GetPageStep()
        {
            var normalizedStep = Step == 0 ? 1 : Math.Abs(Step);
            return normalizedStep * 10;
        }

        private static decimal? ConvertToDecimal(object value)
        {
            if (value == null)
            {
                return null;
            }
            if (value is decimal decimalValue)
            {
                return decimalValue;
            }
            return Convert.ToDecimal(value, CultureInfo.CurrentCulture);
        }

        private string FormatValue(decimal? value)
        {
            if (!value.HasValue)
            {
                return string.Empty;
            }
            if (Precision.HasValue)
            {
                return value.Value.ToString($"F{Precision.Value}", CultureInfo.InvariantCulture);
            }
            return value.Value.ToString(CultureInfo.InvariantCulture);
        }

        private string FormattedValue => inputText ?? DisplayTextFor(Value);

        /// <summary>把值渲染成显示文本：先按 Precision 归一，再套用户给的 Formatter（异常时退回未格式化文本）。</summary>
        private string DisplayTextFor(decimal? value)
        {
            var text = FormatValue(value);
            if (Formatter == null)
            {
                return text;
            }
            try
            {
                return Formatter(text) ?? string.Empty;
            }
            catch
            {
                return text;
            }
        }

        private bool IsInputNumberDisabled => effectiveDisabled;

        private bool IsWithoutControls => !Controls;

        private bool IsDecreaseDisabled => effectiveDisabled || Readonly || (Min.HasValue && (Value ?? 0) <= Min.Value);

        private bool IsIncreaseDisabled => effectiveDisabled || Readonly || (Max.HasValue && (Value ?? 0) >= Max.Value);

        private string DecreaseButtonClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-input-number__decrease")
            .AddIf(IsDecreaseDisabled, "is-disabled")
            .ToString();

        private string IncreaseButtonClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-input-number__increase")
            .AddIf(IsIncreaseDisabled, "is-disabled")
            .ToString();

        private string AriaDisabled => GetAriaBoolean(IsInputNumberDisabled);

        private string AriaReadonly => Readonly ? "true" : null;

        private string AriaInvalid => IsAriaInvalid ? "true" : "false";

        private string AriaValueMin => Min?.ToString(CultureInfo.InvariantCulture);

        private string AriaValueMax => Max?.ToString(CultureInfo.InvariantCulture);

        private string AriaValueNow => hasInvalidInput ? null : Value?.ToString(CultureInfo.InvariantCulture);

        private string AriaValueText => string.IsNullOrWhiteSpace(FormattedValue) || hasInvalidInput ? null : FormattedValue;

        private string InputAriaDescribedBy => JoinAriaIds(AriaDescribedby, AriaDescribedBy);

        private string ResolveAttributeId()
        {
            if (Attributes == null)
            {
                return null;
            }

            return Attributes.TryGetValue("id", out var id) ? Convert.ToString(id, CultureInfo.InvariantCulture) : null;
        }

        private int GetControlTabIndex(bool disabled)
        {
            return disabled ? -1 : 0;
        }

        private static string JoinAriaIds(params string[] ids)
        {
            var value = string.Join(" ", ids.Where(id => !string.IsNullOrWhiteSpace(id)));
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static string GetAriaBoolean(bool value) => value ? "true" : "false";

        private static string GetSizeCssValue(InputSize size) => size switch
        {
            InputSize.Large => "large",
            InputSize.Small => "small",
            _ => null
        };
    }
}
