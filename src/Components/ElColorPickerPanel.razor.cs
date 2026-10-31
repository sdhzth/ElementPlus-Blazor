using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElColorPickerPanel : ElementFieldComponentBase<string>
    {
        private ElementColor currentColor = ElementColor.Parse("#409eff");
        private string inputValue;
        private bool effectiveDisabled;
        private IReadOnlyList<PredefinedColor> predefinedColors = Array.Empty<PredefinedColor>();

        /// <summary>三块可按住拖动的区域：饱和度面板、色相条、alpha 条。</summary>
        private const string SvRegion = "sv";
        private const string HueRegion = "hue";
        private const string AlphaRegion = "alpha";

        private ElementReference svPanelRef;
        private ElementReference hueSliderRef;
        private ElementReference alphaSliderRef;

        /// <summary>三块区域各自的透明 range：拖动走指针通道后它们只服务键盘与读屏，按下时把焦点交给它们。</summary>
        private ElementReference saturationInputRef;
        private ElementReference hueInputRef;
        private ElementReference alphaInputRef;

        /// <summary>当前按住拖动的是哪块区域；null 表示没按住。</summary>
        private string activeRegion;

        /// <summary>本次手势的区域元素与它的视口矩形（一次按下量一次，整段拖动复用）。</summary>
        private ElementReference gestureReference;
        private ElementViewportRect gestureRect;

        /// <summary>按下已经由指针通道写过值：紧跟其后的 click 不再重复写一次。</summary>
        private string handledClickRegion;

        /// <summary>与 ElSlider 跑道用同一对 dom.js 帮助函数（<see cref="ElSlider"/>:44-46）。</summary>
        private const string MeasureFunction = "elementViewportRect";
        private const string PointerCaptureFunction = "elementPointerCapture";
        private const string FocusFunction = "execFocus";

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
        public bool Border { get; set; } = true;

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool IsDisabled
        {
            get => Disabled;
            set => Disabled = value;
        }

        [Parameter]
        public bool ShowAlpha { get; set; }

        [Parameter]
        public string ColorFormat { get; set; }

        [Parameter]
        public IEnumerable<string> Predefine { get; set; }

        [Parameter]
        public bool ValidateEvent { get; set; } = true;

        [Parameter]
        public string HueSliderClass { get; set; }

        [Parameter]
        public string HueSliderStyle { get; set; }

        [Parameter]
        public RenderFragment FooterContent { get; set; }

        [Parameter]
        public RenderFragment Footer { get; set; }

        [Parameter]
        public EventCallback<string> OnChange { get; set; }

        [Parameter]
        public EventCallback<string> OnActiveChange { get; set; }

        public ElementColor Color => currentColor?.Clone();

        public ElementReference InputRef { get; private set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            effectiveDisabled = Disabled || (FormItem?.Form?.Disabled ?? false);

            if (FormItem != null && !FormItem.OriginValueHasRendered)
            {
                FormItem.OriginValueHasRendered = true;
                if (FormItem.Form?.Values.Any() ?? false)
                {
                    Value = Convert.ToString(FormItem.OriginValue, CultureInfo.CurrentCulture);
                }
                SetFieldValue(Value, false);
            }

            /* 只有外部真的换了颜色才重新解析：同一块颜色的字符串回环会丢通道信息
               （黑 #000000 解析回来饱和度塌成 0、白 #ffffff 会把色相丢掉），
               小圆点因此会在角落里横跳。EP 的面板自持 Color 对象，不从自己发布的字符串重导。 */
            if (ElementColor.TryParse(Value, out var parsed) && !parsed.ColorEquals(currentColor))
            {
                currentColor = parsed;
            }
            inputValue = FormatColor(currentColor);
            predefinedColors = (Predefine ?? Enumerable.Empty<string>())
                .Select(x => new PredefinedColor(x, ElementColor.TryParse(x, out var color) ? color : null))
                .Where(x => x.Color != null)
                .ToArray();
        }

        protected override void FormItem_OnReset(object value, bool requireRerender)
        {
            Value = Convert.ToString(value, CultureInfo.CurrentCulture);
            if (ElementColor.TryParse(Value, out var parsed))
            {
                currentColor = parsed;
            }
            inputValue = FormatColor(currentColor);

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

        public Task Update()
        {
            inputValue = FormatColor(currentColor);
            StateHasChanged();
            return Task.CompletedTask;
        }

        private async Task OnHueInputAsync(ChangeEventArgs e)
        {
            if (effectiveDisabled)
            {
                return;
            }

            var next = currentColor.Clone();
            next.Hue = ParseDouble(e.Value, next.Hue);
            await CommitColorAsync(next, ValidateEvent, notifyChange: true);
        }

        private async Task OnSaturationInputAsync(ChangeEventArgs e)
        {
            if (effectiveDisabled)
            {
                return;
            }

            var next = currentColor.Clone();
            next.Saturation = Clamp(ParseDouble(e.Value, next.Saturation), 0, 100);
            await CommitColorAsync(next, ValidateEvent, notifyChange: true);
        }

        private async Task OnValueInputAsync(ChangeEventArgs e)
        {
            if (effectiveDisabled)
            {
                return;
            }

            var next = currentColor.Clone();
            next.Value = Clamp(ParseDouble(e.Value, next.Value), 0, 100);
            await CommitColorAsync(next, ValidateEvent, notifyChange: true);
        }

        private async Task OnAlphaInputAsync(ChangeEventArgs e)
        {
            if (effectiveDisabled)
            {
                return;
            }

            var next = currentColor.Clone();
            next.Alpha = Clamp(ParseDouble(e.Value, next.Alpha * 100), 0, 100) / 100;
            await CommitColorAsync(next, ValidateEvent, notifyChange: true);
        }

        /// <summary>
        /// EP 的 <c>draggable(rootEl, { drag, end })</c>（Temp/ep2140-cp-draggable.mjs:7-38；
        /// SV 面板在 use-sv-panel 的 onMounted 绑根元素，Temp/ep2140-cp-use-sv-panel.mjs:99-109；
        /// 色相/alpha 条在 use-slider 的 onMounted 同时绑 bar 和 thumb，Temp/ep2140-cp-use-slider.mjs:112-125）：
        /// 按住就把值跳到按住点，随后连续跟随。EP 在 document 上挂 mousemove/mouseup，
        /// 这里靠 <c>elementPointerCapture</c> 把指针钉在本区域上（同 ElSlider 跑道，ElSlider.razor.cs:387-405）。
        /// </summary>
        private async Task OnRegionPointerDownAsync(PointerEventArgs e, string region, ElementReference reference)
        {
            gestureReference = reference;
            gestureRect = null;
            activeRegion = null;
            handledClickRegion = null;
            if (effectiveDisabled)
            {
                return;
            }

            if (await ApplyRegionPointAsync(e, region))
            {
                activeRegion = region;
                handledClickRegion = region;
                await TryPointerCaptureAsync(e.PointerId, true);
            }
        }

        private async Task OnRegionPointerMoveAsync(PointerEventArgs e, string region)
        {
            if (activeRegion != region || effectiveDisabled)
            {
                return;
            }

            await ApplyRegionPointAsync(e, region);
        }

        /// <summary>
        /// EP <c>draggable</c> 的 end 回调：EP 在 end 里再走一次 handleDrag，但每次 move 都已经把坐标钳进区域内，
        /// 松手点的钳制结果与最后一次 move 相同，这里不再重复取值；只按 EP 对外口径补一次带校验、带 change
        /// 的提交（拖动途中只发 active-change，同 ElSlider 的 OnRunwayPointerUpAsync）。
        /// </summary>
        private async Task OnRegionPointerUpAsync(PointerEventArgs e, string region)
        {
            if (activeRegion != region)
            {
                return;
            }

            activeRegion = null;
            gestureRect = null;
            await TryPointerCaptureAsync(e.PointerId, false);
            if (effectiveDisabled)
            {
                return;
            }

            await CommitColorAsync(currentColor.Clone(), ValidateEvent, notifyChange: true);
        }

        /// <summary>
        /// 按指针位置写入该区域对应的通道。取到矩形才写；量不到（无宿主 JS / 未挂载）返回 false，
        /// 这次手势就此放弃，交给紧随其后的 click 走 OffsetX 兜底路径。
        /// </summary>
        private async Task<bool> ApplyRegionPointAsync(PointerEventArgs e, string region)
        {
            gestureRect ??= await MeasureAsync(gestureReference);
            var rect = gestureRect;
            if (rect == null || rect.Width <= 0 || rect.Height <= 0)
            {
                return false;
            }

            var next = currentColor.Clone();
            if (region == SvRegion)
            {
                /* EP use-sv-panel handleDrag（Temp/ep2140-cp-use-sv-panel.mjs:23-39）：
                   坐标先钳进面板内，再 saturation = x / width * 100、value = 100 - y / height * 100，不取整。 */
                next.Saturation = Clamp(e.ClientX - rect.Left, 0, rect.Width) / rect.Width * 100;
                next.Value = 100 - Clamp(e.ClientY - rect.Top, 0, rect.Height) / rect.Height * 100;
            }
            else
            {
                /* EP use-slider handleDrag（Temp/ep2140-cp-use-slider.mjs:19-36）：横向先把 x 钳进
                   [thumbW/2, width - thumbW/2]，再 value = round((x - thumbW/2) / (width - thumbW) * maxValue)。
                   色相 maxValue=360（Temp/ep2140-cp-hue-slider.mjs:14-15），alpha maxValue=100
                   （Temp/ep2140-cp-alpha-slider.mjs:14-15），本仓 alpha 存 0~1，所以再除以 100。 */
                var span = rect.Width - SliderThumbWidth;
                var offset = Clamp(e.ClientX - rect.Left, SliderThumbWidth / 2, span + SliderThumbWidth / 2) - SliderThumbWidth / 2;
                if (region == HueRegion)
                {
                    next.Hue = Math.Round(offset / span * 360);
                }
                else
                {
                    next.Alpha = Math.Round(offset / span * 100) / 100;
                }
            }

            await CommitColorAsync(next, validate: false, notifyChange: false);
            return true;
        }

        private async Task<ElementViewportRect> MeasureAsync(ElementReference reference)
        {
            if (JSRuntime == null)
            {
                return null;
            }

            try
            {
                return await JSRuntime.InvokeAsync<ElementViewportRect>(MeasureFunction, reference);
            }
            catch
            {
                /* 未挂载 / 无宿主 JS：与 ElSlider.MeasureRunwayAsync 同口径静默 */
                return null;
            }
        }

        private async Task FocusRegionInputAsync(string region)
        {
            if (JSRuntime == null)
            {
                return;
            }

            var target = region == SvRegion ? saturationInputRef : region == HueRegion ? hueInputRef : alphaInputRef;
            try
            {
                await JSRuntime.InvokeVoidAsync(FocusFunction, target);
            }
            catch
            {
                /* 聚焦失败不影响拖动 */
            }
        }

        private async Task TryPointerCaptureAsync(long pointerId, bool capture)
        {
            if (JSRuntime == null)
            {
                return;
            }

            try
            {
                await JSRuntime.InvokeAsync<bool>(PointerCaptureFunction, gestureReference, pointerId, capture);
            }
            catch
            {
                /* 拿不到指针捕获：退化成「按下即跳到该位置」，click 兜底路径仍在 */
            }
        }

        /// <summary>EP 的滑块 thumb 宽 4px（index.css：.el-color-hue-slider__thumb / .el-color-alpha-slider__thumb 的 width:4px）。</summary>
        private const double SliderThumbWidth = 4;

        /* 下面三个尺寸是 index.css 写死的值（.el-color-svpanel{width:280px;height:180px}、
           .el-color-hue-slider / .el-color-alpha-slider{width:280px}）。
           只有 click 兜底路径（量不到视口矩形时）才用它们；指针通道一律用实测矩形。 */
        private const double SvPanelWidth = 280;
        private const double SvPanelHeight = 180;
        private const double SliderWidth = 280;

        /// <summary>指针通道已经写过这个点了，随后的 click 直接吃掉，避免同一次点击重复提交。</summary>
        private bool ConsumedClick(string region)
        {
            if (handledClickRegion != region)
            {
                return false;
            }

            handledClickRegion = null;
            return true;
        }

        /// <summary>
        /// EP 的 <c>handleClick</c>（Temp/ep2140-cp-use-sv-panel.mjs:18-22、
        /// Temp/ep2140-cp-use-slider.mjs:13-17）：点击做两件事——把焦点交给滑块，
        /// 并在命中的不是滑块本体时按位置写一次。本仓按下时已经写过同一个点
        /// （<see cref="ConsumedClick"/> 挡住重复的那次写），所以这里通常只剩聚焦。
        /// 聚焦必须放在 click 而不是按下：按下时的原生默认动作会把焦点抢回 body。
        /// </summary>
        private async Task OnSvPanelClickAsync(MouseEventArgs e)
        {
            if (effectiveDisabled)
            {
                return;
            }

            if (!ConsumedClick(SvRegion))
            {
                var next = currentColor.Clone();
                next.Saturation = Clamp(e.OffsetX / SvPanelWidth * 100, 0, 100);
                next.Value = Clamp(100 - e.OffsetY / SvPanelHeight * 100, 0, 100);
                await CommitColorAsync(next, ValidateEvent, notifyChange: true);
            }

            await FocusRegionInputAsync(SvRegion);
        }

        private async Task OnHueSliderClickAsync(MouseEventArgs e)
        {
            if (effectiveDisabled)
            {
                return;
            }

            if (!ConsumedClick(HueRegion))
            {
                var next = currentColor.Clone();
                next.Hue = Clamp(e.OffsetX / SliderWidth * 360, 0, 360);
                await CommitColorAsync(next, ValidateEvent, notifyChange: true);
            }

            await FocusRegionInputAsync(HueRegion);
        }

        private async Task OnAlphaSliderClickAsync(MouseEventArgs e)
        {
            if (effectiveDisabled)
            {
                return;
            }

            if (!ConsumedClick(AlphaRegion))
            {
                var next = currentColor.Clone();
                next.Alpha = Clamp(e.OffsetX / SliderWidth, 0, 1);
                await CommitColorAsync(next, ValidateEvent, notifyChange: true);
            }

            await FocusRegionInputAsync(AlphaRegion);
        }

        private Task OnColorTextInputAsync(ChangeEventArgs e)
        {
            inputValue = Convert.ToString(e.Value, CultureInfo.CurrentCulture);
            return Task.CompletedTask;
        }

        private async Task OnColorTextChangeAsync(ChangeEventArgs e)
        {
            if (effectiveDisabled)
            {
                return;
            }

            var text = Convert.ToString(e.Value, CultureInfo.CurrentCulture);
            inputValue = text;
            if (!ElementColor.TryParse(text, out var parsed))
            {
                return;
            }

            await CommitColorAsync(parsed, ValidateEvent, notifyChange: true);
        }

        private Task SelectPredefineAsync(PredefinedColor item)
        {
            if (effectiveDisabled || item?.Color == null)
            {
                return Task.CompletedTask;
            }

            return CommitColorAsync(item.Color.Clone(), ValidateEvent, notifyChange: true);
        }

        private async Task CommitColorAsync(ElementColor color, bool validate, bool notifyChange)
        {
            currentColor = color;
            Value = FormatColor(currentColor);
            inputValue = Value;
            SetFieldValue(Value, validate);

            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(Value);
            }
            if (OnActiveChange.HasDelegate)
            {
                await OnActiveChange.InvokeAsync(Value);
            }
            if (notifyChange && OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(Value);
            }
        }

        private string FormatColor(ElementColor color)
        {
            return color?.ToCssString(ElementColor.ParseFormat(ColorFormat), ShowAlpha) ?? string.Empty;
        }

        private string PanelClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-color-picker__panel", "el-color-picker-panel", Cls)
            .AddIf(!Border, "is-borderless")
            .AddIf(effectiveDisabled, "is-disabled")
            .ToString();

        private string PanelStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add("position:relative")
            .Add(Style)
            .AddIf(!Border, "border-color:transparent", "box-shadow:none")
            .AddIf(effectiveDisabled, "pointer-events:none", "opacity:.6")
            .ToString();

        private string SvPanelStyle => $"background-color:{HueColor}";

        private string SvCursorStyle => string.Format(CultureInfo.InvariantCulture, "left:{0}%;top:{1}%;", currentColor.Saturation, 100 - currentColor.Value);

        private string HueSliderMergedStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add(HueSliderStyle)
            .ToString();

        private string HueThumbStyle => string.Format(CultureInfo.InvariantCulture, "left:{0}%;", currentColor.Hue / 360 * 100);

        private string AlphaThumbStyle => string.Format(CultureInfo.InvariantCulture, "left:{0}%;", currentColor.Alpha * 100);

        private string AlphaBarStyle => $"background:linear-gradient(to right, rgba({currentColor.Red}, {currentColor.Green}, {currentColor.Blue}, 0) 0%, {currentColor.ToAlphaGradientColor()} 100%)";

        /* 这两张透明 range 只服务键盘与读屏：EP 的拖动句柄是面板/滑块本身
           （Temp/ep2140-cp-use-sv-panel.mjs:99-109、Temp/ep2140-cp-use-slider.mjs:112-125），
           它们一旦吃指针，饱和度面板的二维拖动就会被读成一维亮度拖动
           （实测：Temp/b20-drag-diag-1.log —— elementFromPoint 命中的是 value-input，
           横向拖动把 val 从 29 拖到 76，sat 全程冻在 73~75）。指针一律交给区域元素自己的
           OnRegionPointer* 通道，Tab / 方向键不受影响。 */
        private string SvRangeInputStyle => "position:absolute;left:0;top:0;right:0;width:100%;height:100%;opacity:0;pointer-events:none";

        private string SliderRangeInputStyle => "position:absolute;left:0;top:0;width:100%;height:100%;opacity:0;pointer-events:none";

        private string HueColor => new ElementColor(currentColor.Hue, 100, 100).ToOpaqueRgbString();

        private string InputValue => inputValue ?? FormatColor(currentColor);

        private int HueValue => (int)Math.Round(currentColor.Hue, MidpointRounding.AwayFromZero);

        private int SaturationValue => (int)Math.Round(currentColor.Saturation, MidpointRounding.AwayFromZero);

        private int BrightnessValue => (int)Math.Round(currentColor.Value, MidpointRounding.AwayFromZero);

        private int AlphaValue => (int)Math.Round(currentColor.Alpha * 100, MidpointRounding.AwayFromZero);

        private bool IsColorPickerPanelDisabled => effectiveDisabled;

        private IReadOnlyList<PredefinedColor> PredefinedColors => predefinedColors;

        // ===== 无障碍名称（el.colorpicker.*，挂法对齐 EP 面板：…Label 进 aria-label，…Description 进 aria-valuetext）=====
        // …Label 的兜底取改动前那个写死字面量（属性本来就在）；…Description 兜底为 null ——
        // aria-valuetext 是这次新加的属性，只有词条存在时才渲染，保证「没词条＝零 DOM 变化」。

        private string CurrentColorText => InputValue ?? string.Empty;

        private string SvPanelAriaLabel => Lang.T("el.colorpicker.svLabel", "color saturation and value");

        private string SvPanelAriaValueText => Lang.T(
            "el.colorpicker.svDescription",
            null,
            ("saturation", SaturationValue), ("brightness", BrightnessValue), ("color", CurrentColorText));

        private string HueAriaLabel => Lang.T("el.colorpicker.hueLabel", "hue");

        private string HueAriaValueText => Lang.T(
            "el.colorpicker.hueDescription",
            null,
            ("hue", HueValue), ("color", CurrentColorText));

        private string AlphaAriaLabel => Lang.T("el.colorpicker.alphaLabel", "alpha");

        private string AlphaAriaValueText => Lang.T(
            "el.colorpicker.alphaDescription",
            null,
            ("alpha", AlphaValue), ("color", CurrentColorText));

        private string GetPredefineAriaLabel(PredefinedColor item)
        {
            return Lang.T("el.colorpicker.predefineDescription", "select color {value}", ("value", item.Value));
        }

        private string GetPredefineClass(PredefinedColor item)
        {
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-color-predefine__color-selector")
                .AddIf(item.Color.Alpha < 1, "is-alpha")
                .AddIf(currentColor.ColorEquals(item.Color, ShowAlpha), "selected")
                .ToString();
        }

        private string GetPredefineStyle(PredefinedColor item)
        {
            return $"background-color:{item.Color.ToCssString(ElementColorFormat.Rgb, true)}";
        }

        private static double ParseDouble(object value, double fallback)
        {
            return double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                ? result
                : fallback;
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private class PredefinedColor
        {
            public PredefinedColor(string value, ElementColor color)
            {
                Value = value;
                Color = color;
            }

            public string Value { get; }

            public ElementColor Color { get; }
        }
    }
}
