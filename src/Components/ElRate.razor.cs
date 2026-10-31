using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElRate : ElementFieldComponentBase<double?>
    {
        private HtmlPropertyBuilder wrapperClsBuilder;
        private bool effectiveDisabled;
        private double? hoverValue;

        [Parameter]
        public double? Value { get; set; }

        [Parameter]
        public double? ModelValue
        {
            get => Value;
            set => Value = value;
        }

        [Parameter]
        public EventCallback<double?> ValueChanged { get; set; }

        [Parameter]
        public EventCallback<double?> ModelValueChanged { get; set; }

        [Parameter]
        public int Max { get; set; } = 5;

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool IsDisabled
        {
            get => Disabled;
            set => Disabled = value;
        }

        [Parameter]
        public bool AllowHalf { get; set; }

        [Parameter]
        public bool Clearable { get; set; } = true;

        [Parameter]
        public bool ShowText { get; set; }

        [Parameter]
        public bool ShowScore { get; set; }

        [Parameter]
        public string ScoreTemplate { get; set; } = "{0}";

        [Parameter]
        public string[] Texts { get; set; } = new[] { "极差", "失望", "一般", "满意", "惊喜" };

        [Parameter]
        public string[] Colors { get; set; } = new[] { "#f7ba2a", "#f7ba2a", "#f7ba2a" };

        [Parameter]
        public string VoidColor { get; set; } = "#c6d1de";

        [Parameter]
        public string DisabledVoidColor { get; set; } = "#eff2f7";

        [Parameter]
        public string Icon { get; set; } = "el-icon-star-on";

        [Parameter]
        public string VoidIcon { get; set; } = "el-icon-star-off";

        [Parameter]
        public RenderFragment<RateIconContext> IconTemplate { get; set; }

        [Parameter]
        public RenderFragment<RateIconContext> ItemTemplate { get; set; }

        [Parameter]
        public bool ValidateEvent { get; set; } = true;

        [Parameter]
        public EventCallback<double?> OnChange { get; set; }

        /// <summary>尺寸</summary>
        [Parameter]
        public InputSize Size { get; set; } = InputSize.Normal;

        /// <summary>文字模板（与 ShowText 配合）</summary>
        [Parameter]
        public string TextTemplate { get; set; } = "{value}";

        /// <summary>只读（独立于 Disabled）</summary>
        [Parameter]
        public bool Readonly { get; set; }

        /// <summary>aria-label</summary>
        [Parameter]
        public string AriaLabel { get; set; }

        /// <summary>hover 变化事件（参数是当前 hover 的值，非 hover 时为 null）</summary>
        [Parameter]
        public EventCallback<double?> OnHoverChange { get; set; }

        /// <summary>focus 事件</summary>
        [Parameter]
        public EventCallback OnFocus { get; set; }

        /// <summary>blur 事件</summary>
        [Parameter]
        public EventCallback OnBlur { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            effectiveDisabled = Disabled || (FormItem?.Form?.Disabled ?? false);
            Max = Math.Max(1, Max);
            var sizeVal = Size switch { InputSize.Large => "large", InputSize.Small => "small", _ => null };
            wrapperClsBuilder = HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-rate", Cls)
                .AddIf(effectiveDisabled, "is-disabled")
                .AddIf(sizeVal != null, $"el-rate--{sizeVal}");

            if (FormItem != null && !FormItem.OriginValueHasRendered)
            {
                FormItem.OriginValueHasRendered = true;
                if (FormItem.Form?.Values.Any() ?? false)
                {
                    Value = ConvertToDouble(FormItem.OriginValue);
                }
                SetFieldValue(Value, false);
            }
        }

        protected override void FormItem_OnReset(object value, bool requireRerender)
        {
            Value = ConvertToDouble(value);
            hoverValue = null;
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

        private async Task SelectRateAsync(int index)
        {
            if (effectiveDisabled || Readonly)
            {
                return;
            }

            var next = hoverValue ?? index;
            if (Clearable && Value.HasValue && Math.Abs(Value.Value - next) < 0.001)
            {
                next = 0;
            }
            await CommitValueAsync(next <= 0 ? null : next);
        }

                private async void HoverRate(int index, MouseEventArgs e)
        {
            var half = AllowHalf && e.OffsetX < 12; // 半个星大约 12px
            hoverValue = half ? index - 0.5 : index;
            if (OnHoverChange.HasDelegate) await OnHoverChange.InvokeAsync(hoverValue);
        }

        private async void ClearHover()
        {
            hoverValue = null;
            if (OnHoverChange.HasDelegate) await OnHoverChange.InvokeAsync(null);
        }

        private async Task OnFocusAsync()
        {
            if (OnFocus.HasDelegate)
            {
                await OnFocus.InvokeAsync();
            }
        }

        private async Task OnBlurAsync()
        {
            if (OnBlur.HasDelegate)
            {
                await OnBlur.InvokeAsync();
            }
        }

        private async Task CommitValueAsync(double? value)
        {
            Value = value;
            SetFieldValue(Value, ValidateEvent);
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(Value);
            }
            if (OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(Value);
            }
        }

        private string GetIconClass(int index)
        {
            return IsActive(index) ? $"el-rate__icon {Icon}" : $"el-rate__icon {VoidIcon}";
        }

        private string GetIconStyle(int index)
        {
            var color = ResolveIconColor(index);
            return $"color:{color}";
        }

        private string ResolveIconColor(int index)
        {
            return IsActive(index) ? ResolveColor(CurrentValue) : (effectiveDisabled ? DisabledVoidColor : VoidColor);
        }

        private bool IsActive(int index)
        {
            return CurrentValue >= index || (AllowHalf && CurrentValue > index - 1 && CurrentValue < index);
        }

        private string ResolveColor(double? value)
        {
            var colors = Colors == null || Colors.Length == 0 ? new[] { "#f7ba2a" } : Colors;
            if (!value.HasValue)
            {
                return colors[0];
            }
            if (colors.Length == 1)
            {
                return colors[0];
            }
            var ratio = value.Value / Max;
            if (colors.Length >= 3)
            {
                if (ratio <= 0.4)
                {
                    return colors[0];
                }
                if (ratio <= 0.8)
                {
                    return colors[1];
                }
                return colors[2];
            }
            return ratio <= 0.5 ? colors[0] : colors[1];
        }

        private string DisplayText
        {
            get
            {
                if (ShowScore)
                {
                    return string.Format(CultureInfo.CurrentCulture, ScoreTemplate, CurrentValue?.ToString("0.0", CultureInfo.CurrentCulture) ?? "0");
                }
                // 自定义 TextTemplate（与 ShowText 配合）：用当前分值替换 {value}，覆盖默认 Texts 文案。
                // 未显式设置（仍是默认值 "{value}"）时退化为原有 Texts[index] 行为，保持向后兼容。
                if (ShowText && !string.IsNullOrEmpty(TextTemplate) && TextTemplate != "{value}")
                {
                    return TextTemplate.Replace("{value}", CurrentValue?.ToString("0.0", CultureInfo.CurrentCulture) ?? "0");
                }
                var index = Math.Max(0, Math.Min((int)Math.Ceiling(CurrentValue ?? 0) - 1, (Texts?.Length ?? 0) - 1));
                return Texts != null && Texts.Length > 0 ? Texts[index] : string.Empty;
            }
        }

        private double? CurrentValue => hoverValue ?? Value;

        private bool IsRateDisabled => effectiveDisabled;

        private string AriaDisabled => effectiveDisabled ? "true" : "false";

        private RenderFragment<RateIconContext> ResolvedIconTemplate => IconTemplate ?? ItemTemplate;

        private RateIconContext CreateIconContext(int index)
        {
            var active = IsActive(index);
            var half = AllowHalf && CurrentValue > index - 1 && CurrentValue < index;
            var color = ResolveIconColor(index);
            return new RateIconContext
            {
                Index = index,
                Max = Max,
                Value = Value,
                CurrentValue = CurrentValue,
                HoverValue = hoverValue,
                Active = active,
                Half = half,
                Disabled = effectiveDisabled,
                Empty = !active,
                Color = color,
                Icon = active ? Icon : VoidIcon,
                IconClass = GetIconClass(index),
                IconStyle = $"color:{color}"
            };
        }

        private static double? ConvertToDouble(object value)
        {
            if (value == null)
            {
                return null;
            }
            return Convert.ToDouble(value, CultureInfo.CurrentCulture);
        }
    }

    public class RateIconContext
    {
        public int Index { get; set; }

        public int Max { get; set; }

        public double? Value { get; set; }

        public double? CurrentValue { get; set; }

        public double? HoverValue { get; set; }

        public bool Active { get; set; }

        public bool Half { get; set; }

        public bool Disabled { get; set; }

        public bool Empty { get; set; }

        public string Color { get; set; }

        public string Icon { get; set; }

        public string IconClass { get; set; }

        public string IconStyle { get; set; }
    }
}
