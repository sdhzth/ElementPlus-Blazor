using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElColorPicker : ElementFieldComponentBase<string>
    {
        private static long dropDownIdSeed;
        private readonly string DropDownId = $"el-color-picker-dropdown-{Interlocked.Increment(ref dropDownIdSeed)}";
        private DropDownOption dropDownOption;
        private ElementReference colorPickerElement;
        private bool effectiveDisabled;
        private InputSize effectiveSize = InputSize.Normal;

        [Inject]
        internal PopupService PopupService { get; set; }

        [Parameter]
        public string Value { get; set; }

        [Parameter]
        public string ModelValue
        {
            get => Value;
            set => Value = value;
        }

        [Parameter]
        public EventCallback<string> ValueChanged { get; set; }

        [Parameter]
        public EventCallback<string> ModelValueChanged { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool IsDisabled
        {
            get => Disabled;
            set => Disabled = value;
        }

        [Parameter]
        public InputSize Size { get; set; } = InputSize.Normal;

        [Parameter]
        public bool ShowAlpha { get; set; }

        [Parameter]
        public string ColorFormat { get; set; }

        [Parameter]
        public IEnumerable<string> Predefine { get; set; }

        /// <summary>
        /// 是否可清空。EP 口径 <c>clearable: { type: Boolean, default: true }</c>
        /// （packages/components/color-picker/src/color-picker.ts），所以这里默认开：
        /// 原来默认 false 时，绝大多数色板的面板页脚根本没有「清空」按钮（悬停 ✕ 也一并没有），
        /// 用户看到的「清空不起作用」第一步就是「压根没这个按钮」。
        /// false 时页脚不出「清空」，触发器悬停也不换 ✕。
        /// </summary>
        [Parameter]
        public bool Clearable { get; set; } = true;

        [Parameter]
        public bool ValidateEvent { get; set; } = true;

        [Parameter]
        public string PopperClass { get; set; }

        [Parameter]
        public string PopperStyle { get; set; }

        [Parameter]
        public int PopperMaxHeight { get; set; } = 420;

        /// <summary>点击面板外部是否保持打开（EP persistent 语义，default: false 即点外部关闭）。</summary>
        [Parameter]
        public bool Persistent { get; set; } = false;

        /// <summary>挂载到哪个 DOM 元素 (2.10.5, default: body)</summary>
        [Parameter]
        public string AppendTo { get; set; }

        /// <summary>清空后的值 (2.10.3, default: null → empty string)</summary>
        [Parameter]
        public string ValueOnClear { get; set; }

        [Parameter]
        public object Tabindex { get; set; } = 0;

        /// <summary>触发器的可访问名称。默认 null（=宿主没指定），此时取 <c>el.colorpicker.defaultLabel</c> 词条。</summary>
        [Parameter]
        public string AriaLabel { get; set; }

        [Parameter]
        public EventCallback<string> OnChange { get; set; }

        [Parameter]
        public EventCallback<string> OnActiveChange { get; set; }

        [Parameter]
        public EventCallback<bool> OnVisibleChange { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnClear { get; set; }

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

            if (FormItem != null && !FormItem.OriginValueHasRendered)
            {
                FormItem.OriginValueHasRendered = true;
                if (FormItem.Form?.Values.Any() ?? false)
                {
                    Value = Convert.ToString(FormItem.OriginValue, CultureInfo.CurrentCulture);
                }
                SetFieldValue(Value, false);
            }
        }

        protected override void FormItem_OnReset(object value, bool requireRerender)
        {
            Value = Convert.ToString(value, CultureInfo.CurrentCulture);
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

        private async Task OnColorPickerClickAsync(MouseEventArgs e)
        {
            if (effectiveDisabled)
            {
                return;
            }

            if (IsDropDownOpen)
            {
                await HideAsync();
                return;
            }

            await ShowAsync();
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
            if (ValidateEvent)
            {
                SetFieldValue(Value, true);
            }
            if (OnBlur.HasDelegate)
            {
                await OnBlur.InvokeAsync(e);
            }
        }

        private async Task OnKeyDownAsync(KeyboardEventArgs e)
        {
            if (effectiveDisabled)
            {
                return;
            }

            if (e.Key == "Enter" || e.Key == " " || e.Key == "ArrowDown")
            {
                await ShowAsync();
            }
            else if (e.Key == "Escape")
            {
                await HideAsync();
            }
        }

        public Task ShowAsync()
        {
            return OpenDropDownAsync();
        }

        public Task HideAsync()
        {
            return CloseDropDownAsync();
        }

        public ValueTask FocusAsync()
        {
            return colorPickerElement.Dom(JSRuntime).FocusAsync();
        }

        public ValueTask BlurAsync()
        {
            return colorPickerElement.Dom(JSRuntime).BlurAsync();
        }

        public Task ClearAsync()
        {
            return ClearAsync(null);
        }

        private async Task ClearAsync(MouseEventArgs e)
        {
            if (effectiveDisabled)
            {
                return;
            }

            var clearValue = ValueOnClear ?? string.Empty;
            await CommitValueAsync(clearValue, ValidateEvent, notifyChange: true, notifyActiveChange: true);
            if (OnClear.HasDelegate)
            {
                await OnClear.InvokeAsync(e);
            }
            await HideAsync();
        }

        private async Task OnPanelValueChangedAsync(string value)
        {
            await CommitValueAsync(value, ValidateEvent, notifyChange: true, notifyActiveChange: false);
        }

        private async Task OnPanelActiveChangeAsync(string value)
        {
            if (OnActiveChange.HasDelegate)
            {
                await OnActiveChange.InvokeAsync(value);
            }
        }

        private async Task CommitValueAsync(string value, bool validate, bool notifyChange, bool notifyActiveChange)
        {
            Value = value;
            SetFieldValue(Value, validate);
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(Value);
            }
            if (notifyActiveChange && OnActiveChange.HasDelegate)
            {
                await OnActiveChange.InvokeAsync(Value);
            }
            if (notifyChange && OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(Value);
            }
        }

        private async Task OpenDropDownAsync()
        {
            if (effectiveDisabled)
            {
                return;
            }

            if (dropDownOption != null)
            {
                RefreshDropDown();
                return;
            }

            dropDownOption = new DropDownOption
            {
                Target = colorPickerElement,
                OptionContent = BuildPanelContent,
                PopperClass = string.Join(" ", new[] { "el-color-picker__dropdown", PopperClass }.Where(x => !string.IsNullOrWhiteSpace(x))),
                PopperStyle = PopperStyle,
                MaxHeight = PopperMaxHeight,
                FitInputWidth = false,
                AutoWidth = false,
                DropDownId = DropDownId,
                Refresh = () => InvokeAsync(StateHasChanged),
                OnClosed = () => NotifyVisibleChangeAsync(false),
                IsShow = true,
                Width = 300,
                Persistent = Persistent,
                AppendTo = AppendTo
            };
            PopupService.SelectDropDownOptionsCore.Add(dropDownOption);
            await NotifyVisibleChangeAsync(true);
        }

        private async Task CloseDropDownAsync()
        {
            if (dropDownOption?.Instance != null)
            {
                await dropDownOption.Instance.CloseDropDownAsync(dropDownOption);
                return;
            }

            if (dropDownOption != null)
            {
                PopupService.SelectDropDownOptionsCore.Remove(dropDownOption);
                dropDownOption = null;
                await NotifyVisibleChangeAsync(false);
            }
        }

        private Task NotifyVisibleChangeAsync(bool visible)
        {
            if (!visible)
            {
                dropDownOption = null;
            }

            if (OnVisibleChange.HasDelegate)
            {
                return OnVisibleChange.InvokeAsync(visible);
            }

            StateHasChanged();
            return Task.CompletedTask;
        }

        private void BuildPanelContent(RenderTreeBuilder builder)
        {
            var seq = 0;
            builder.OpenElement(seq++, "li");
            builder.AddAttribute(seq++, "class", "el-color-picker__panel-item");
            builder.AddAttribute(seq++, "role", "presentation");
            builder.OpenComponent<ElColorPickerPanel>(seq++);
            builder.AddAttribute(seq++, nameof(ElColorPickerPanel.Value), Value);
            builder.AddAttribute(seq++, nameof(ElColorPickerPanel.ValueChanged), EventCallback.Factory.Create<string>(this, OnPanelValueChangedAsync));
            builder.AddAttribute(seq++, nameof(ElColorPickerPanel.ShowAlpha), ShowAlpha);
            builder.AddAttribute(seq++, nameof(ElColorPickerPanel.ColorFormat), ColorFormat);
            builder.AddAttribute(seq++, nameof(ElColorPickerPanel.Predefine), Predefine);
            builder.AddAttribute(seq++, nameof(ElColorPickerPanel.Disabled), effectiveDisabled);
            builder.AddAttribute(seq++, nameof(ElColorPickerPanel.ValidateEvent), false);
            builder.AddAttribute(seq++, nameof(ElColorPickerPanel.Border), false);
            builder.AddAttribute(seq++, nameof(ElColorPickerPanel.OnActiveChange), EventCallback.Factory.Create<string>(this, OnPanelActiveChangeAsync));
            builder.AddAttribute(seq++, nameof(ElColorPickerPanel.FooterContent), BuildFooterContent());
            builder.CloseComponent();
            builder.CloseElement();
        }

        /// <summary>
        /// 面板页脚。EP 口径（color-picker.vue 模板）：
        /// <code>
        /// &lt;el-button v-if="clearable" :class="ns.be('footer','link-btn')" text size="small" @click="clear"&gt;
        /// &lt;el-button plain size="small" :class="ns.be('footer','btn')" @click="confirmValue"&gt;
        /// </code>
        /// 原来这里是两个裸 <c>&lt;button&gt;</c>，只挂 element-ui 遗留的
        /// <c>.el-color-dropdown__btn</c>（line-height:24px）/ <c>__link-btn</c>（padding:15px）尺寸规则。
        /// 实测（把这两条规则与裸 button 用 <c>Temp/mut-cp107-guard.cjs</c> 的 CP3+CP5 注回后跑探针）：
        /// 「清空」h=51 对「确定」h=24，日志 <c>Temp/b14-cp-legacy-geometry.log</c>；
        /// 全裸状态下的「确定」实测 h=26 / w=66，日志 <c>Temp/b13-cp-before.log</c> —— 就是用户说的「按钮大小和旁边的不一样」。
        /// EP 的 theme-chalk/color-picker.scss 里没有任何 el-color-dropdown 规则（那两个类只是 DOM 钩子），
        /// 所以遗留尺寸规则一并删掉，改由 .el-button 自己出样式。
        /// 「清空」的 disabled 也按 EP 收掉 <c>!HasColor</c> 那半：EP 只禁到组件禁用，
        /// 没色值时点清空是幂等的空操作，不该长得像「坏了」。
        /// </summary>
        private RenderFragment BuildFooterContent()
        {
            return builder =>
            {
                var seq = 0;
                if (Clearable)
                {
                    builder.OpenComponent<ElButton>(seq++);
                    builder.AddAttribute(seq++, nameof(ElButton.Cls), "el-color-dropdown__link-btn");
                    builder.AddAttribute(seq++, nameof(ElButton.Text), true);
                    builder.AddAttribute(seq++, nameof(ElButton.Size), ButtonSize.Small);
                    builder.AddAttribute(seq++, nameof(ElButton.Disabled), effectiveDisabled);
                    builder.AddAttribute(seq++, nameof(ElButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, e => ClearAsync(e)));
                    builder.AddAttribute(seq++, nameof(ElButton.ChildContent), (RenderFragment)(b => b.AddContent(0, "清空")));
                    builder.CloseComponent();
                }

                builder.OpenComponent<ElButton>(seq++);
                builder.AddAttribute(seq++, nameof(ElButton.Cls), "el-color-dropdown__btn");
                builder.AddAttribute(seq++, nameof(ElButton.Plain), true);
                builder.AddAttribute(seq++, nameof(ElButton.Size), ButtonSize.Small);
                builder.AddAttribute(seq++, nameof(ElButton.Disabled), effectiveDisabled);
                builder.AddAttribute(seq++, nameof(ElButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, _ => HideAsync()));
                builder.AddAttribute(seq++, nameof(ElButton.ChildContent), (RenderFragment)(b => b.AddContent(0, "确定")));
                builder.CloseComponent();
            };
        }

        public override void Dispose()
        {
            if (dropDownOption != null)
            {
                PopupService.SelectDropDownOptionsCore.Remove(dropDownOption);
            }
            base.Dispose();
        }

        private void RefreshDropDown()
        {
            dropDownOption?.RequestRender?.Invoke();
            StateHasChanged();
        }

        private string WrapperClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-color-picker", Cls)
            .AddIf(effectiveDisabled, "is-disabled")
            .AddIf(IsDropDownOpen, "is-focus")
            .AddIf(SizeCssValue != null, $"el-color-picker--{SizeCssValue}")
            .ToString();

        private string ColorBlockClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-color-picker__color")
            .AddIf(ShowAlpha && HasColor, "is-alpha")
            .ToString();

        private string ColorInnerStyle => $"background-color:{DisplayColor}";

        private string DisplayColor
        {
            get
            {
                if (!ElementColor.TryParse(Value, out var color))
                {
                    return "transparent";
                }

                return color.ToCssString(ElementColorFormat.Rgb, ShowAlpha || color.Alpha < 1);
            }
        }

        /// <summary>触发器图标：EP 只有「有色值 → arrow-down」这一档显示态，清空不走这里。</summary>
        private string TriggerIconClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-color-picker__icon")
            .Add("el-icon-arrow-down")
            .ToString();

        private string SizeCssValue => effectiveSize switch
        {
            InputSize.Large => "large",
            InputSize.Small => "small",
            _ => null
        };

        private bool HasColor => ElementColor.TryParse(Value, out _);

        /// <summary>触发器名称：显式参数 &gt; <c>el.colorpicker.defaultLabel</c> &gt; 改动前的字面量。</summary>
        private string TriggerAriaLabel => Lang.Text("el.colorpicker.defaultLabel", AriaLabel, "color picker");

        /// <summary>EP 挂在触发器上的 <c>aria-description</c>，带当前颜色。改动前本仓没这个属性，所以没词条时保持不渲染。</summary>
        private string TriggerAriaDescription => Lang.T(
            "el.colorpicker.description",
            null,
            ("color", Value ?? string.Empty));

        private bool IsDropDownOpen => dropDownOption != null && dropDownOption.IsShow;

        private bool IsColorPickerDisabled => effectiveDisabled;
    }
}
