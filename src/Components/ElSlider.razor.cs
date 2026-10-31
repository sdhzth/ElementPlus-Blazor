using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElSlider : ElementFieldComponentBase<double>
    {
        private static long inputIdSeed;
        private readonly string generatedInputId = $"el-slider-{Interlocked.Increment(ref inputIdSeed)}";
        private HtmlPropertyBuilder wrapperClsBuilder;
        private HtmlPropertyBuilder runwayClsBuilder;
        private HtmlPropertyBuilder tooltipClsBuilder;
        private bool effectiveDisabled;
        private int activeThumbIndex = -1;
        private bool inputSizeSpecified;

        /// <summary>
        /// EP <c>initData.firstValue / secondValue</c> 的同构：每个钮自己的原始值。
        /// 拖动过程中允许两个钮互相越过（EP 也允许），对外发布的区间另外排序
        /// （<see cref="CurrentRange"/>）。为 null 时按 <see cref="RangeValue"/> 现算。
        /// </summary>
        private double[] thumbValues;

        /// <summary>EP <c>initData.dragging</c>：按住钮或跑道期间为 true，期间不把模型值回灌到钮上。</summary>
        private bool thumbsDragging;

        /// <summary>本次跑道按下正在驱动哪个钮；-1 表示当前不是跑道拖动。</summary>
        private int runwayThumbIndex = -1;

        /// <summary>跑道元素：量视口矩形算百分位、按住指针都靠它。</summary>
        private ElementReference runwayRef;

        /// <summary>本次手势的跑道视口矩形（一次按下量一次，供整段拖动复用）。</summary>
        private ElementViewportRect runwayRect;

        /// <summary>与 <see cref="TableFilterPopper"/> 用同一个 dom.js 帮助函数（摊平过的 DOMRect）。</summary>
        private const string MeasureFunction = "elementViewportRect";

        private const string PointerCaptureFunction = "elementPointerCapture";

        [Parameter]
        public double Value { get; set; }

        [Parameter]
        public double ModelValue
        {
            get => Value;
            set => Value = value;
        }

        [Parameter]
        public EventCallback<double> ValueChanged { get; set; }

        [Parameter]
        public EventCallback<double> ModelValueChanged { get; set; }

        [Parameter]
        public double Min { get; set; }

        [Parameter]
        public double Max { get; set; } = 100;

        /// <summary>
        /// EP <c>step</c> 的数值形态：步长。
        /// 与 <see cref="StepToMark"/>（EP 的 <c>step="mark"</c>）互斥，<see cref="StepToMark"/> 优先级更高。
        /// </summary>
        [Parameter]
        public double Step { get; set; } = 1;

        /// <summary>
        /// EP 2.13.6 的 <c>step="mark"</c>：将取值限制为 <see cref="Marks"/> 的刻度，此时必须提供 <see cref="Marks"/>。
        /// 开启后 <see cref="ShowStops"/> 与 <see cref="ShowInput"/> 自动失效（对齐 EP <c>renderInput</c> 判定）。
        /// </summary>
        [Parameter]
        public bool StepToMark { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool IsDisabled
        {
            get => Disabled;
            set => Disabled = value;
        }

        [Parameter]
        public bool ShowStops { get; set; }

        [Parameter]
        public bool ShowInput { get; set; }

        [Parameter]
        public IEnumerable<SliderMark> Marks { get; set; }

        [Parameter]
        public bool ShowTooltip { get; set; } = true;

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public bool Range { get; set; }

        [Parameter]
        public IList<double> RangeValue { get; set; } = new List<double>();

        [Parameter]
        public EventCallback<IList<double>> RangeValueChanged { get; set; }

        [Parameter]
        public Func<double, string> FormatTooltip { get; set; }

        [Parameter]
        public bool ValidateEvent { get; set; } = true;

        [Parameter]
        public EventCallback<double> OnInput { get; set; }

        [Parameter]
        public EventCallback<double> OnChange { get; set; }

        /// <summary>垂直模式。</summary>
        [Parameter]
        public bool Vertical { get; set; }

        /// <summary>垂直模式下的高度（px）。EP 原为 string，本库保留 int 以便 DI 使用。</summary>
        [Parameter]
        public int Height { get; set; } = 200;

        /// <summary>
        /// EP <c>placement</c>：Tooltip 出现的位置（top / top-start / bottom / left …）。默认 top。
        /// </summary>
        [Parameter]
        public string Placement { get; set; } = "top";

        /// <summary>兼容旧名，等价于 <see cref="Placement"/>。</summary>
        [Parameter]
        public string TooltipPlacement
        {
            get => Placement;
            set => Placement = value;
        }

        /// <summary>
        /// EP <c>size</c>：滑块包装器尺寸（EP 2.x 的 large / default / small）。垂直模式下该属性不生效（对齐 EP 说明）。
        /// </summary>
        [Parameter]
        public InputSize Size { get; set; } = InputSize.Normal;

        /// <summary>
        /// EP <c>input-size</c>：输入框的大小。未显式赋值时自动取 <see cref="Size"/>（对齐 EP 的 <c>inputSize || sliderWrapperSize</c>）。
        /// </summary>
        [Parameter]
        public InputSize InputSize { get; set; } = InputSize.Normal;

        /// <summary>EP <c>show-input-controls</c>：ShowInput 时数字输入框是否显示控制按钮。</summary>
        [Parameter]
        public bool ShowInputControls { get; set; } = true;

        /// <summary>
        /// EP 2.9.5 <c>persistent</c>：为 false 时，非活动状态的 tooltip 会被销毁（不进 DOM）。
        /// <see cref="ShowTooltip"/> 为 false 时恒为 false（对齐 EP 的 <c>tooltipPersistent</c>）。
        /// </summary>
        [Parameter]
        public bool Persistent { get; set; } = true;

        /// <summary>EP <c>tooltip-class</c>：tooltip 的自定义类名。</summary>
        [Parameter]
        public string TooltipClass { get; set; }

        /// <summary>EP <c>range-start-label</c>：range 模式下首个滑块的屏幕阅读器标签。缺省回落到 <see cref="AriaLabel"/>。</summary>
        [Parameter]
        public string RangeStartLabel { get; set; }

        /// <summary>EP <c>range-end-label</c>：range 模式下末尾滑块的屏幕阅读器标签。缺省回落到 <see cref="AriaLabel"/>。</summary>
        [Parameter]
        public string RangeEndLabel { get; set; }

        /// <summary>
        /// EP <c>format-value-text</c>：屏幕阅读器 <c>aria-valuetext</c> 的格式化函数。
        /// 缺省依次回落到 <see cref="FormatTooltip"/>、裸值（EP 默认也是裸值）。
        /// </summary>
        [Parameter]
        public Func<double, string> FormatValueText { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        [Parameter]
        public string AriaLabelledBy { get; set; }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            inputSizeSpecified = false;
            foreach (var supplied in parameters)
            {
                if (string.Equals(supplied.Name, nameof(InputSize), StringComparison.Ordinal))
                {
                    inputSizeSpecified = true;
                    break;
                }
            }

            return base.SetParametersAsync(parameters);
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            effectiveDisabled = Disabled || (FormItem?.Form?.Disabled ?? false);
            if (Max <= Min)
            {
                Max = Min + 1;
            }
            Step = Step <= 0 ? 1 : Step;
            if (Range)
            {
                NormalizeRange();
            }
            else
            {
                Value = Normalize(Value);
            }
            Id = string.IsNullOrWhiteSpace(Id) ? ResolveAttributeId() ?? generatedInputId : Id;
            if (FormItem?.Form != null)
            {
                FormItem.Form.RegisterInput(FormItem.Name, InputId, this, FormItem);
            }
            wrapperClsBuilder = HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-slider", Cls)
                .Add($"el-slider--{SizeCss}")
                .AddIf(effectiveDisabled, "is-disabled")
                .AddIf(Range, "is-range")
                .AddIf(Vertical, "is-vertical")
                .AddIf(RenderInput, "el-slider--with-input");
            runwayClsBuilder = HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-slider__runway")
                .AddIf(effectiveDisabled, "is-disabled")
                .AddIf(RenderInput, "show-input");
            tooltipClsBuilder = HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-slider__tooltip", "el-tooltip__popper", TooltipClass)
                .Add($"el-tooltip--{EffectivePlacement}");

            if (FormItem != null && !FormItem.OriginValueHasRendered)
            {
                FormItem.OriginValueHasRendered = true;
                if (FormItem.Form?.Values.Any() ?? false)
                {
                    if (Range)
                    {
                        RangeValue = NormalizeRangeValue(FormItem.OriginValue);
                    }
                    else
                    {
                        Value = Convert.ToDouble(FormItem.OriginValue, CultureInfo.CurrentCulture);
                    }
                }
                SetFieldValue(Value, false);
            }
        }

        protected override void FormItem_OnReset(object value, bool requireRerender)
        {
            if (Range)
            {
                RangeValue = NormalizeRangeValue(value);
                Value = RangeValue.LastOrDefault();
                if (RangeValueChanged.HasDelegate)
                {
                    _ = RangeValueChanged.InvokeAsync(RangeValue);
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
                return;
            }

            Value = value == null ? Min : Convert.ToDouble(value, CultureInfo.CurrentCulture);
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
            if (IsSliderDisabled)
            {
                return;
            }

            var next = ParseValue(e.Value);
            await CommitValueAsync(next, validate: false, notifyChange: false);
            if (OnInput.HasDelegate)
            {
                await OnInput.InvokeAsync(Value);
            }
        }

        private async Task OnChangeAsync(ChangeEventArgs e)
        {
            if (IsSliderDisabled)
            {
                return;
            }

            await CommitValueAsync(ParseValue(e.Value), ValidateEvent, notifyChange: true);
        }

        private async Task OnRangeInputAsync(int index, ChangeEventArgs e)
        {
            if (IsSliderDisabled)
            {
                return;
            }

            // 原生 input 拖动：input 事件一路发、change 只在松手时发一次。
            // 这段时间必须标记成「正在拖」，否则自己发布出去的有序区间会被 OnParametersSet
            // 回灌到两个钮上，把用户正按着的那个钮拽回去（浏览器实测见 Temp/diag-batch9-slider-sync3.log 的 D1 段：
            // [20,60] 一越过就塌成 [0,1]，两个钮一起黏到最小端）。
            thumbsDragging = true;
            activeThumbIndex = index;
            await CommitRangeValueAsync(index, ParseValue(e.Value), validate: false, notifyChange: false);
            if (OnInput.HasDelegate)
            {
                await OnInput.InvokeAsync(Value);
            }
        }

        private async Task OnRangeChangeAsync(int index, ChangeEventArgs e)
        {
            if (IsSliderDisabled)
            {
                return;
            }

            activeThumbIndex = index;
            await CommitRangeValueAsync(index, ParseValue(e.Value), ValidateEvent, notifyChange: true);
            EndThumbsDrag();
        }

        private async Task OnInputNumberChangedAsync(decimal? value)
        {
            if (IsSliderDisabled)
            {
                return;
            }

            await CommitValueAsync(value.HasValue ? (double)value.Value : Min, ValidateEvent, notifyChange: true);
        }

        /// <summary>
        /// EP <c>onSliderDown</c>（Temp/ep214_slider_use-slide.ts.txt:174-180）：按住跑道就把
        /// **最近的那个钮**移到按住的位置，并接着把这一按变成连续拖动
        /// （EP 在 window 上挂 mousemove/mouseup，这里靠 <c>elementPointerCapture</c> 把指针钉在跑道上）。
        /// 只接管 range 变体：单钮变体那条透明 input 本来就铺满整条跑道（index.css:1445-1457），
        /// 点轨道、按住拖都归它，实测跟手且 0 帧滞后（Temp/diag-batch9-slider-drag.log 的 B、E 段、
        /// Temp/diag-batch9-slider2.log 的 B/C/E 段），再叠一套机制只会两边打架。
        /// range 变体的 input 只有它的 ::-webkit-slider-thumb 可命中（index.css:1459/1463），
        /// 按住钮时事件在 input 上就被 stopPropagation 掉了，不会走到这里。
        /// </summary>
        private async Task OnRunwayPointerDownAsync(PointerEventArgs e)
        {
            if (!Range || IsSliderDisabled)
            {
                return;
            }

            runwayRect = null;
            var percent = await GetRunwayPercentAsync(e.ClientX, e.ClientY);
            if (percent == null)
            {
                return;
            }

            thumbsDragging = true;
            runwayThumbIndex = NearestThumbIndex(percent.Value);
            await TryPointerCaptureAsync(e.PointerId, true);
            await ApplyThumbPercentAsync(runwayThumbIndex, percent.Value);
        }

        private async Task OnRunwayPointerMoveAsync(PointerEventArgs e)
        {
            if (!Range || runwayThumbIndex < 0 || IsSliderDisabled)
            {
                return;
            }

            var percent = await GetRunwayPercentAsync(e.ClientX, e.ClientY);
            if (percent == null)
            {
                return;
            }

            await ApplyThumbPercentAsync(runwayThumbIndex, percent.Value);
        }

        private async Task OnRunwayPointerUpAsync(PointerEventArgs e)
        {
            if (!Range || runwayThumbIndex < 0)
            {
                return;
            }

            var index = runwayThumbIndex;
            runwayThumbIndex = -1;
            runwayRect = null;
            await TryPointerCaptureAsync(e.PointerId, false);
            // EP 的 onDragEnd 补一次 change，并让钮回到有序（watch(dragging) → setValues()）
            await CommitRangeValueAsync(index, ThumbValues[index], ValidateEvent, notifyChange: true);
            EndThumbsDrag();
        }

        private async Task OnRunwayPointerCancelAsync(PointerEventArgs e)
        {
            if (!Range || runwayThumbIndex < 0)
            {
                return;
            }

            runwayThumbIndex = -1;
            runwayRect = null;
            await TryPointerCaptureAsync(e.PointerId, false);
            EndThumbsDrag();
        }

        /// <summary>松手 / 取消：结束「正在拖」，并把对外的有序区间回灌到两个钮（EP setValues 同构）。</summary>
        private void EndThumbsDrag()
        {
            thumbsDragging = false;
            NormalizeRange();
        }

        /// <summary>EP <c>setPosition</c> 的入口：把百分位换算成该钮自己的值，并对外发布排序后的区间。</summary>
        private async Task ApplyThumbPercentAsync(int index, double percent)
        {
            await CommitRangeValueAsync(index, Min + percent / 100 * (Max - Min), validate: false, notifyChange: false);
            if (OnInput.HasDelegate)
            {
                await OnInput.InvokeAsync(Value);
            }
        }

        /// <summary>
        /// EP <c>handleSliderPointerEvent</c>（Temp/ep214_slider_use-slide.ts.txt:142-163）的同构：
        /// 横向 <c>(clientX - 跑道左) / 跑道宽</c>，纵向 <c>(跑道底 - clientY) / 跑道高</c>（纵向从 bottom 起算）。
        /// 越界（&lt;0 或 &gt;100）与量不到矩形都返回 null —— EP 在这里直接 return，不做「钳制后移动」。
        /// </summary>
        private async Task<double?> GetRunwayPercentAsync(double clientX, double clientY)
        {
            // EP 每次 onDragging 都 resetSize()（use-slide.ts:280），那是同进程取值、代价为零；
            // 这里量的是一次 WASM 往返，所以只在按下的那一次量，整段拖动复用同一个矩形。
            runwayRect ??= await MeasureRunwayAsync();
            var rect = runwayRect;
            if (rect == null || rect.Width <= 0 || rect.Height <= 0)
            {
                return null;
            }

            var percent = Vertical
                ? (rect.Bottom - clientY) / rect.Height * 100
                : (clientX - rect.Left) / rect.Width * 100;
            return percent < 0 || percent > 100 ? null : percent;
        }

        private async Task<ElementViewportRect> MeasureRunwayAsync()
        {
            if (JSRuntime == null)
            {
                return null;
            }

            try
            {
                return await JSRuntime.InvokeAsync<ElementViewportRect>(MeasureFunction, runwayRef);
            }
            catch
            {
                // 未挂载 / 无宿主 JS：与 ElTour.ScrollActiveTargetIntoView 同口径静默
                return null;
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
                await JSRuntime.InvokeAsync<bool>(PointerCaptureFunction, runwayRef, pointerId, capture);
            }
            catch
            {
                // 拿不到指针捕获时退化成「按下即跳到该位置」，仍比原来点跑道毫无反应强
            }
        }

        /// <summary>
        /// EP <c>getButtonRefByPercent</c>（Temp/ep214_slider_use-slide.ts.txt:81-104）：目标值离哪个钮近就驱动哪个钮，
        /// 等距时交给「取到较大值的那个钮」（EP 用的是严格 &lt; 判据）。
        /// </summary>
        private int NearestThumbIndex(double percent)
        {
            var target = Min + percent * (Max - Min) / 100;
            var thumbs = ThumbValues;
            var lowerFirst = thumbs[0] <= thumbs[1];
            var minIndex = lowerFirst ? 0 : 1;
            var maxIndex = lowerFirst ? 1 : 0;
            return Math.Abs(thumbs[minIndex] - target) < Math.Abs(thumbs[maxIndex] - target) ? minIndex : maxIndex;
        }

        private async Task CommitValueAsync(double next, bool validate, bool notifyChange)
        {
            Value = Normalize(next);
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

        private async Task CommitRangeValueAsync(int index, double next, bool validate, bool notifyChange)
        {
            var range = SetRangeThumbValue(index, next);
            RangeValue = range;
            Value = range.LastOrDefault();
            SetFieldValue(Value, validate);
            if (RangeValueChanged.HasDelegate)
            {
                await RangeValueChanged.InvokeAsync(RangeValue);
            }
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

        private double Normalize(double value)
        {
            if (StepToMark && MarkList.Count > 0)
            {
                return MarkList
                    .OrderBy(m => Math.Abs(m.Value - value))
                    .ThenBy(m => m.Value)
                    .First()
                    .Value;
            }

            var clamped = Math.Max(Min, Math.Min(Max, value));
            var steps = Math.Round((clamped - Min) / Step, 0, MidpointRounding.AwayFromZero);
            return Math.Max(Min, Math.Min(Max, Min + steps * Step));
        }

        private double ParseValue(object value)
        {
            return double.TryParse(Convert.ToString(value), NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                ? result
                : Value;
        }

        private IList<double> SetRangeThumbValue(int index, double next)
        {
            var thumbs = ThumbValues.ToArray();
            if (index < 0 || index >= thumbs.Length)
            {
                index = 0;
            }

            // EP 口径：钮各拿各的值，拖动过程中允许越过另一个钮（initData.firstValue / secondValue
            // 从不排序），排序只发生在「对外发布」和「条形/断点」这两处（minValue / maxValue）。
            thumbs[index] = Normalize(next);
            thumbValues = thumbs;
            activeThumbIndex = index;
            return CurrentRange.ToList();
        }

        private double GetPercent(double value)
        {
            return (Math.Max(Min, Math.Min(Max, value)) - Min) / (Max - Min) * 100;
        }

        /// <summary>
        /// EP <c>useStops</c> 同构：只渲染「当前已选区间之外」的断点（<c>range</c> 时取并集之外的两侧）。
        /// <c>step="mark"</c>（<see cref="StepToMark"/>）下不渲染断点。
        /// </summary>
        private IEnumerable<double> Stops
        {
            get
            {
                if (!ShowStops || StepToMark || Min > Max)
                {
                    yield break;
                }

                var stepCount = (int)Math.Ceiling((Max - Min) / Step);
                var stepWidth = 100 * Step / (Max - Min);
                var start = GetPercent(Range ? CurrentRange[0] : Value);
                var end = GetPercent(Range ? CurrentRange[1] : Value);

                for (var i = 1; i < stepCount; i++)
                {
                    var position = i * stepWidth;
                    if (Range ? (position < start || position > end) : position > start)
                    {
                        yield return position;
                    }
                }
            }
        }

        private IReadOnlyList<SliderMark> MarkList
        {
            get
            {
                if (Marks == null)
                {
                    return Array.Empty<SliderMark>();
                }

                return Marks
                    .Where(x => x != null)
                    .Select(x => new SliderMark { Value = x.Value, Label = x.Label, Style = x.Style })
                    .OrderBy(x => x.Value)
                    .ToList();
            }
        }

        /// <summary>EP <c>getStopStyle</c>：横向用 <c>left</c>，纵向用 <c>bottom</c>。</summary>
        private string GetStopStyle(double percent) => Vertical ? $"bottom:{percent}%;" : $"left:{percent}%;";

        private string GetMarkPositionStyle(SliderMark mark)
        {
            var percent = GetPercent(Normalize(mark.Value));
            return GetStopStyle(percent) + (string.IsNullOrWhiteSpace(mark.Style) ? string.Empty : mark.Style);
        }

        private string BarStyle
        {
            get
            {
                if (Range)
                {
                    return RangeBarStyle;
                }

                return Vertical
                    ? $"bottom:0;height:{GetPercent(Value)}%;"
                    : $"width:{GetPercent(Value)}%;";
            }
        }

        private string ButtonStyle => Vertical
            ? $"bottom:{GetPercent(Value)}%;"
            : $"left:{GetPercent(Value)}%;";

        /// <summary>
        /// EP <c>minValue / maxValue</c>（Temp/ep214_slider_use-slide.ts.txt:34-40）：
        /// 条形宽度、断点、对外发布的区间都取这一对，它不改动钮自己的原始值。
        /// </summary>
        private IReadOnlyList<double> CurrentRange
        {
            get
            {
                var thumbs = ThumbValues;
                return thumbs[0] <= thumbs[1]
                    ? new[] { thumbs[0], thumbs[1] }
                    : new[] { thumbs[1], thumbs[0] };
            }
        }

        /// <summary>
        /// 每个钮自己的原始值（<c>value</c>、圆钮位置、<c>aria-valuenow</c>、气泡文本都读这一份）：
        /// 拖动过程中允许互相越过，松手后由 <see cref="EndThumbsDrag"/> 回到有序。
        /// </summary>
        private IReadOnlyList<double> ThumbValues => thumbValues ??= SortRange(RangeValue);

        /// <summary>把对外的区间（可能缺项、可能反序）整理成两个钮的初值：<c>[min, max]</c>，并按步进规整。</summary>
        private double[] SortRange(IList<double> source)
        {
            var start = source != null && source.Count > 0 ? source[0] : Min;
            var end = source != null && source.Count > 1 ? source[1] : Max;
            var pair = new[] { Normalize(start), Normalize(end) };
            if (pair[0] > pair[1])
            {
                var swap = pair[0];
                pair[0] = pair[1];
                pair[1] = swap;
            }
            return pair;
        }

        private string RangeBarStyle => Vertical
            ? $"bottom:{GetPercent(CurrentRange[0])}%;height:{GetPercent(CurrentRange[1]) - GetPercent(CurrentRange[0])}%;"
            : $"left:{GetPercent(CurrentRange[0])}%;width:{GetPercent(CurrentRange[1]) - GetPercent(CurrentRange[0])}%;";

        /// <summary>圆钮位置读钮自己的原始值（ThumbValues），拖动过程中越过另一个钮时才不会被排序掰回去。</summary>
        private string GetRangeButtonStyle(int index) => Vertical
            ? $"bottom:{GetPercent(ThumbValues[index])}%;"
            : $"left:{GetPercent(ThumbValues[index])}%;";

        /// <summary>EP <c>buildModel</c>：横向从 min 起算，纵向从 bottom 起算（<c>bottom + height</c>）。</summary>
        private string GetRangeButtonAriaValueMin(int index) => FormatInvariant(index == 0 ? Min : CurrentRange[0]);

        private string GetRangeButtonAriaValueMax(int index) => FormatInvariant(index == 0 ? CurrentRange[1] : Max);

        private string GetTooltipText(double value)
        {
            return FormatTooltip != null
                ? FormatTooltip(value)
                : value.ToString(CultureInfo.CurrentCulture);
        }

        private string GetValueText(double value)
        {
            if (FormatValueText != null)
            {
                return FormatValueText(value);
            }

            if (FormatTooltip != null)
            {
                return FormatTooltip(value);
            }

            return FormatInvariant(value);
        }

        private string GetRangeThumbAriaLabel(int index)
        {
            var explicitLabel = (index == 0 ? RangeStartLabel : RangeEndLabel) ?? AriaLabel;

            // 宿主给过任何显式标签就沿用它（改动前的行为），两者都没给才补词条，补的是"完全没名字"的缺口
            return Lang.Text(
                index == 0 ? "el.slider.defaultRangeStartLabel" : "el.slider.defaultRangeEndLabel",
                explicitLabel,
                null);
        }

        private bool IsThumbTooltipVisible(int index)
        {
            if (!ShowTooltip)
            {
                return false;
            }

            return Persistent || activeThumbIndex == index;
        }

        private string ResolveAttributeId()
        {
            if (Attributes == null)
            {
                return null;
            }

            return Attributes.TryGetValue("id", out var id) ? Convert.ToString(id, CultureInfo.InvariantCulture) : null;
        }

        private string GetInputId(int index) => index == 0 ? InputId : $"{InputId}-{index + 1}";

        private string InputId => Id;

        private string SizeCss
        {
            get
            {
                switch (Size)
                {
                    case InputSize.Large:
                        return "large";
                    case InputSize.Small:
                        return "small";
                    default:
                        return "default";
                }
            }
        }

        private string EffectivePlacement => string.IsNullOrWhiteSpace(Placement) ? "top" : Placement;

        /// <summary>EP <c>sliderInputSize</c>：显式给了 input-size 就用它，否则继承 <c>size</c>。</summary>
        private InputSize EffectiveInputSize => inputSizeSpecified ? InputSize : Size;

        private string RunwayStyle => Vertical ? $"height:{Height}px;" : null;

        private string RootRole => Range ? "group" : null;

        private string RootAriaLabel => Range && string.IsNullOrWhiteSpace(AriaLabelledBy)
            ? Lang.Template("el.slider.defaultLabel", AriaLabel, null, ("min", Min), ("max", Max))
            : null;

        /// <summary>单滑块的可访问名称：EP 也是用带 min/max 的 defaultLabel 兜底，此前本仓没给名字。</summary>
        private string SingleThumbAriaLabel => Lang.Template(
            "el.slider.defaultLabel", AriaLabel, null, ("min", Min), ("max", Max));

        private string RootAriaLabelledBy => Range ? AriaLabelledBy : null;

        private string AriaOrientation => Vertical ? "vertical" : "horizontal";

        private string AriaDisabled => IsSliderDisabled ? "true" : "false";

        private string AriaInvalid => IsAriaInvalid ? "true" : "false";

        private bool RenderInput => ShowInput && !Range && !StepToMark;

        private static string FormatInvariant(double value) => value.ToString(CultureInfo.InvariantCulture);

        private decimal? DecimalValue => (decimal)Value;

        private bool IsSliderDisabled => effectiveDisabled;

        /// <summary>
        /// EP <c>useWatch.setValues</c> 的同构：把对外的 <c>[min, max]</c> 灌回两个钮。
        /// 拖动进行中（<see cref="thumbsDragging"/>）必须整段跳过 —— EP 的两条 watch 也都以
        /// <c>initData.dragging</c> 短路（Temp/ep214_slider_use-watch.ts.txt:78-104）。
        /// 少了这道闸，自己发布出去的有序区间会被回灌到用户正按着的那个原生 input 上，
        /// 两个钮就会一起黏到端点（浏览器实测：[20,60] 一越过塌成 [0,1]，
        /// 见 Temp/diag-batch9-slider-sync3.log 的 D1 段）。
        /// </summary>
        private void NormalizeRange()
        {
            if (thumbsDragging)
            {
                return;
            }

            var sorted = SortRange(RangeValue);
            thumbValues = sorted;
            RangeValue = sorted.ToList();
            Value = RangeValue.LastOrDefault();
        }

        private IList<double> NormalizeRangeValue(object value)
        {
            if (value is IEnumerable<double> doubleValues)
            {
                return doubleValues.Select(Normalize).OrderBy(x => x).Take(2).ToList();
            }
            if (value is System.Collections.IEnumerable enumerable && value is not string)
            {
                return enumerable.Cast<object>()
                    .Select(x => Convert.ToDouble(x, CultureInfo.CurrentCulture))
                    .Select(Normalize)
                    .OrderBy(x => x)
                    .Take(2)
                    .ToList();
            }
            return new List<double> { Min, Max };
        }
    }
}
