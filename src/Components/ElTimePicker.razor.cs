using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace ElementPlus
{
    public partial class ElTimePicker
    {
        private static long dropDownIdSeed;
        private readonly string DropDownId = $"el-time-picker-dropdown-{Interlocked.Increment(ref dropDownIdSeed)}";
        private DropDownOption dropDownOption;
        private ElementReference pickerElement;
        private ElementReference inputElement;
        private bool effectiveDisabled;
        private InputSize effectiveSize = InputSize.Normal;
        private bool isFocus;
        private bool isHovering;
        private TimeSpan? pendingSingleValue;
        private TimeSpan? pendingRangeStart;
        private TimeSpan? pendingRangeEnd;

        // 滚轮居中用的元素引用 + JS 互操作引用（与 ElDateTimePicker 共用一套 dom.js 助手）
        private ElementReference timePanelElement;
        private DotNetObjectReference<ElTimePicker> timeSpinnerRef;
        private bool timeSpinnerBound;

        // 点弹层外面关闭（与 ElDateTimePicker 同一套助手）
        private DotNetObjectReference<ElTimePicker> outsideClickRef;
        private bool outsideClickBound;

        [Inject]
        internal PopupService PopupService { get; set; }

                /// <summary>原生 tabindex</summary>
        [Parameter] public int? Tabindex { get; set; }

        /// <summary>ARIA 标签</summary>
        [Parameter] public string AriaLabel { get; set; }

[Parameter]
        public TimeSpan? Value { get; set; }

        [Parameter]
        public TimeSpan? ModelValue
        {
            get => Value;
            set => Value = value;
        }

        [Parameter]
        public EventCallback<TimeSpan?> ValueChanged { get; set; }

        [Parameter]
        public EventCallback<TimeSpan?> ModelValueChanged { get; set; }

        [Parameter]
        public TimeSpan? Time
        {
            get => Value;
            set => Value = value;
        }

        [Parameter]
        public EventCallback<TimeSpan?> TimeChanged { get; set; }

        [Parameter]
        public IList<TimeSpan?> RangeValue { get; set; } = new List<TimeSpan?>();

        [Parameter]
        public IList<TimeSpan?> ModelRangeValue
        {
            get => RangeValue;
            set => RangeValue = value;
        }

        [Parameter]
        public EventCallback<IList<TimeSpan?>> RangeValueChanged { get; set; }

        [Parameter]
        public EventCallback<IList<TimeSpan?>> ModelRangeValueChanged { get; set; }

        [Parameter]
        public string StringValue { get; set; }

        [Parameter]
        public EventCallback<string> StringValueChanged { get; set; }

        [Parameter]
        public IList<string> StringRangeValue { get; set; } = new List<string>();

        [Parameter]
        public EventCallback<IList<string>> StringRangeValueChanged { get; set; }

        [Parameter]
        public TimePickerType Type { get; set; } = TimePickerType.Time;

        [Parameter]
        public bool IsRange { get; set; }

        [Parameter]
        public string Format { get; set; } = "HH:mm:ss";

        [Parameter]
        public string ValueFormat { get; set; }

        [Parameter]
        public TimeSpan? DefaultValue { get; set; }

        [Parameter]
        public TimeSpan? DefaultStartTime { get; set; }

        [Parameter]
        public TimeSpan? DefaultEndTime { get; set; }

        [Parameter]
        public string Placeholder { get; set; } = "请选择时间";

        [Parameter]
        public string StartPlaceholder { get; set; } = "开始时间";

        [Parameter]
        public string EndPlaceholder { get; set; } = "结束时间";

        [Parameter]
        public string RangeSeparator { get; set; } = "-";

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
        public bool Editable { get; set; } = true;

        [Parameter]
        public bool Clearable { get; set; } = true;

        [Parameter]
        public InputSize Size { get; set; } = InputSize.Normal;

        [Parameter]
        public string PrefixIcon { get; set; } = "el-icon-time";

        [Parameter]
        public string ClearIcon { get; set; } = "el-icon-circle-close";

        [Parameter]
        public string PopperClass { get; set; }

        [Parameter]
        public string PopperStyle { get; set; }

        [Parameter]
        public int PopperMaxHeight { get; set; } = 336;

        [Parameter]
        public bool ValidateEvent { get; set; } = true;

        [Parameter]
        public EventCallback<TimeSpan?> OnChange { get; set; }

        [Parameter]
        public EventCallback<IList<TimeSpan?>> OnRangeChange { get; set; }

        [Parameter]
        public EventCallback OnClear { get; set; }

        [Parameter]
        public EventCallback<bool> OnVisibleChange { get; set; }

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
            SyncValueFromFormattedParameters();

            if (FormItem != null && !FormItem.OriginValueHasRendered)
            {
                FormItem.OriginValueHasRendered = true;
                if (FormItem.Form?.Values.Any() ?? false)
                {
                    Value = ConvertToTime(FormItem.OriginValue);
                }
                SetFieldValue(Value, false);
            }
        }

        protected override void FormItem_OnReset(object value, bool requireRerender)
        {
            Value = ConvertToTime(value);
            ClearPendingValues();
            if (ValueChanged.HasDelegate)
            {
                _ = ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                _ = ModelValueChanged.InvokeAsync(Value);
            }
            if (TimeChanged.HasDelegate)
            {
                _ = TimeChanged.InvokeAsync(Value);
            }
            else
            {
                StateHasChanged();
            }
        }

        private async Task OnPickerClickAsync(MouseEventArgs e)
        {
            if (effectiveDisabled || Readonly)
            {
                return;
            }

            await OpenDropDownAsync();
        }

        private async Task OnInputFocusAsync(FocusEventArgs e)
        {
            isFocus = true;
            if (OnFocus.HasDelegate)
            {
                await OnFocus.InvokeAsync(e);
            }
            await OpenDropDownAsync();
        }

        private async Task OnInputBlurAsync(FocusEventArgs e)
        {
            isFocus = false;
            // 关闭交给「点弹层外面」的 document 级监听（elementPickerOutsideAttach）：
            // 滚轮列里没有可聚焦元素，失焦时 activeElement 会变成 body，
            // 靠 blur 判断会把「点在面板里」也当成点了外面，面板一点就没了。
            if (OnBlur.HasDelegate)
            {
                await OnBlur.InvokeAsync(e);
            }
        }

        private async Task OnSingleInputChangedAsync(ChangeEventArgs e)
        {
            if (!Editable || effectiveDisabled || Readonly)
            {
                return;
            }

            var text = Convert.ToString(e.Value, CultureInfo.CurrentCulture);
            if (!TryParseTime(text, Format, out var parsed))
            {
                return;
            }

            await CommitSingleValueAsync(parsed, ValidateEvent, notifyChange: true, close: false);
        }

        private async Task OnRangeStartChangedAsync(ChangeEventArgs e)
        {
            await CommitRangeTextAsync(Convert.ToString(e.Value, CultureInfo.CurrentCulture), EndDisplayValue);
        }

        private async Task OnRangeEndChangedAsync(ChangeEventArgs e)
        {
            await CommitRangeTextAsync(StartDisplayValue, Convert.ToString(e.Value, CultureInfo.CurrentCulture));
        }

        private async Task CommitRangeTextAsync(string startText, string endText)
        {
            if (!Editable || effectiveDisabled || Readonly)
            {
                return;
            }
            if (!TryParseTime(startText, Format, out var start) || !TryParseTime(endText, Format, out var end))
            {
                return;
            }

            await CommitRangeValueAsync(NormalizeRange(new TimeSpan?[] { start, end }), ValidateEvent, notifyChange: true, close: false);
        }

        private async Task OnKeyDownAsync(KeyboardEventArgs e)
        {
            if (e.Key == "Escape")
            {
                await CloseDropDownAsync();
            }
            else if (e.Key == "Enter" || e.Key == "ArrowDown")
            {
                await OpenDropDownAsync();
            }
        }

        private async Task OnClearClickAsync(MouseEventArgs e)
        {
            if (!Clearable || effectiveDisabled || Readonly)
            {
                return;
            }

            if (IsRangeMode)
            {
                await CommitRangeValueAsync(new List<TimeSpan?>(), ValidateEvent, notifyChange: true, close: true);
            }
            else
            {
                await CommitSingleValueAsync(null, ValidateEvent, notifyChange: true, close: true);
            }
            if (OnClear.HasDelegate)
            {
                await OnClear.InvokeAsync();
            }
        }

        private async Task OpenDropDownAsync()
        {
            if (effectiveDisabled || Readonly || dropDownOption != null)
            {
                return;
            }

            InitializePendingValues();
            dropDownOption = new DropDownOption
            {
                Target = pickerElement,
                OptionContent = BuildDropDownContent,
                PopperClass = string.Join(" ", new[] { "el-time-panel__popover", PopperClass }.Where(x => !string.IsNullOrWhiteSpace(x))),
                PopperStyle = PopperStyle,
                MaxHeight = PopperMaxHeight,
                FitInputWidth = false,
                AutoWidth = false,
                Width = IsRangeMode ? 430 : 220,
                DropDownId = DropDownId,
                Refresh = () => InvokeAsync(StateHasChanged),
                OnClosed = () => NotifyVisibleChangeAsync(false),
                IsShow = true
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
                ClearPendingValues();
            }

            if (OnVisibleChange.HasDelegate)
            {
                return OnVisibleChange.InvokeAsync(visible);
            }

            StateHasChanged();
            return Task.CompletedTask;
        }

        private void BuildDropDownContent(RenderTreeBuilder builder)
        {
            var seq = 0;
            builder.OpenElement(seq++, "li");
            builder.AddAttribute(seq++, "class", "el-date-picker__panel-item");
            builder.AddAttribute(seq++, "role", "presentation");
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", DropDownPanelClass);
            builder.OpenElement(seq++, "div");
            // 范围模式的壳是 el-time-range-picker__content（EP panel-time-range），
            // 选中带宿主下移到每个 cell 自己的 __body（见 BuildRangePanel）；
            // 单选模式壳本身就是宿主。壳只要包住所有 .el-time-spinner__wrapper 即可
            // （dom.js 按后代查列），所以两种模式都在壳上抓 timePanelElement。
            // 注意：AddElementReferenceCapture 之后不能再 AddAttribute（Blazor 渲染树限制）。
            builder.AddAttribute(seq++, "class", IsRangeMode
                ? "el-time-range-picker__content"
                : ShowSeconds ? "el-time-panel__content has-seconds" : "el-time-panel__content");
            builder.AddElementReferenceCapture(seq++, el => timePanelElement = el);

            if (IsRangeMode)
            {
                BuildRangePanel(builder, ref seq, true);
                BuildRangePanel(builder, ref seq, false);
            }
            else
            {
                BuildTimePanel(builder, ref seq, "single", PendingSingleTimeValue, SetSinglePendingPartAsync);
            }

            builder.CloseElement();
            BuildFooter(builder, ref seq);
            builder.CloseElement();
            builder.CloseElement();
        }

        /// <summary>
        /// 范围模式的半个面板，DOM 与 EP <c>panel-time-range</c> 一致：
        /// <code>
        /// div.el-time-range-picker__cell
        ///   ├ div.el-time-range-picker__header          开始时间 / 结束时间
        ///   └ div.el-time-range-picker__body.el-time-panel__content(.has-seconds)
        /// </code>
        /// <para>
        /// 关键：选中带（<c>.el-time-panel__content::before/::after</c>）的宿主必须是每个 cell 自己的
        /// <c>__body</c>，而不是两组共用的外层。以前共用外层时，那条带画在「两个 33px 表头 + 滚轮」
        /// 整体的 50% 处，比滚轮容器中心低约 19px（实测 <c>Temp/diag-batch9-timepicker_explore2.log</c>
        /// 的 D 段：contentCenter 601.9 vs activeCenter 621.8），
        /// 表现就是「当前选中的没停在两条横线中间」。
        /// </para>
        /// </summary>
        private void BuildRangePanel(RenderTreeBuilder builder, ref int seq, bool left)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "el-time-range-picker__cell");
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "el-time-range-picker__header");
            builder.AddContent(seq++, left ? StartPlaceholder : EndPlaceholder);
            builder.CloseElement();
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(
                seq++,
                "class",
                ShowSeconds
                    ? "el-time-range-picker__body el-time-panel__content has-seconds"
                    : "el-time-range-picker__body el-time-panel__content");
            BuildTimePanel(builder, ref seq, left ? "start" : "end", left ? PendingStartTimeValue : PendingEndTimeValue, left ? SetRangeStartPendingPartAsync : SetRangeEndPendingPartAsync);
            builder.CloseElement();
            builder.CloseElement();
        }

        /// <summary>
        /// 与 <see cref="ElDateTimePicker"/> 共用同一份滚轮渲染器：
        /// DOM 结构、<c>data-time-*</c> 测试钩子、无障碍属性只在 <see cref="TimeSpinnerRenderer"/> 维护一处。
        /// </summary>
        private void BuildTimePanel(RenderTreeBuilder builder, ref int seq, string panel, TimeSpan value, Func<TimePart, int, Task> onSelect)
        {
            TimeSpinnerRenderer.BuildSpinner(
                builder,
                ref seq,
                this,
                panel,
                value,
                ShowMinutes,
                ShowSeconds,
                (spinnerPart, selected) => onSelect(ToTimePart(spinnerPart), selected));
        }

        private static TimePart ToTimePart(TimeSpinnerPart part)
        {
            return part switch
            {
                TimeSpinnerPart.Minute => TimePart.Minute,
                TimeSpinnerPart.Second => TimePart.Second,
                _ => TimePart.Hour
            };
        }

        private void BuildFooter(RenderTreeBuilder builder, ref int seq)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "el-time-panel__footer");
            builder.OpenElement(seq++, "button");
            builder.AddAttribute(seq++, "type", "button");
            builder.AddAttribute(seq++, "class", "el-time-panel__btn cancel");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, CloseDropDownAsync));
            builder.AddContent(seq++, "取消");
            builder.CloseElement();
            builder.OpenElement(seq++, "button");
            builder.AddAttribute(seq++, "type", "button");
            builder.AddAttribute(seq++, "class", "el-time-panel__btn confirm");
            builder.AddAttribute(seq++, "disabled", !CanConfirmPending);
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, ConfirmPendingAsync));
            builder.AddContent(seq++, "确认");
            builder.CloseElement();
            builder.CloseElement();
        }

        private Task SetSinglePendingPartAsync(TimePart part, int value)
        {
            pendingSingleValue = UpdatePart(PendingSingleTimeValue, part, value);
            return RefreshPendingAsync();
        }

        private Task SetRangeStartPendingPartAsync(TimePart part, int value)
        {
            pendingRangeStart = UpdatePart(PendingStartTimeValue, part, value);
            return RefreshPendingAsync();
        }

        private Task SetRangeEndPendingPartAsync(TimePart part, int value)
        {
            pendingRangeEnd = UpdatePart(PendingEndTimeValue, part, value);
            return RefreshPendingAsync();
        }

        private Task RefreshPendingAsync()
        {
            dropDownOption?.RequestRender?.Invoke();
            StateHasChanged();
            return Task.CompletedTask;
        }

        private async Task ConfirmPendingAsync()
        {
            if (IsRangeMode)
            {
                if (!pendingRangeStart.HasValue || !pendingRangeEnd.HasValue)
                {
                    return;
                }

                await CommitRangeValueAsync(NormalizeRange(new[] { pendingRangeStart, pendingRangeEnd }), ValidateEvent, notifyChange: true, close: true);
                return;
            }

            if (!pendingSingleValue.HasValue)
            {
                return;
            }

            await CommitSingleValueAsync(pendingSingleValue, ValidateEvent, notifyChange: true, close: true);
        }

        private async Task CommitSingleValueAsync(TimeSpan? value, bool validate, bool notifyChange, bool close)
        {
            Value = NormalizeTime(value);
            StringValue = FormatBindingValue(Value);
            SetFieldValue(Value, validate);
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(Value);
            }
            if (TimeChanged.HasDelegate)
            {
                await TimeChanged.InvokeAsync(Value);
            }
            if (StringValueChanged.HasDelegate)
            {
                await StringValueChanged.InvokeAsync(StringValue);
            }
            if (notifyChange && OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(Value);
            }
            if (close)
            {
                await CloseDropDownAsync();
            }
        }

        private async Task CommitRangeValueAsync(IList<TimeSpan?> range, bool validate, bool notifyChange, bool close)
        {
            RangeValue = NormalizeRange(range);
            await NotifyRangeValueChangedAsync(validate, notifyChange);
            if (close)
            {
                await CloseDropDownAsync();
            }
        }

        private async Task NotifyRangeValueChangedAsync(bool validate, bool notifyChange)
        {
            StringRangeValue = RangeValue.Select(FormatBindingValue).ToList();
            SetFieldValue(RangeStart, validate);
            if (RangeValueChanged.HasDelegate)
            {
                await RangeValueChanged.InvokeAsync(RangeValue);
            }
            if (ModelRangeValueChanged.HasDelegate)
            {
                await ModelRangeValueChanged.InvokeAsync(RangeValue);
            }
            if (StringRangeValueChanged.HasDelegate)
            {
                await StringRangeValueChanged.InvokeAsync(StringRangeValue);
            }
            if (notifyChange && OnRangeChange.HasDelegate)
            {
                await OnRangeChange.InvokeAsync(RangeValue);
            }
        }

        public ValueTask FocusAsync()
        {
            return inputElement.Dom(JSRuntime).FocusAsync();
        }

        public ValueTask BlurAsync()
        {
            return inputElement.Dom(JSRuntime).BlurAsync();
        }

        public Task ShowAsync()
        {
            return OpenDropDownAsync();
        }

        public Task HideAsync()
        {
            return CloseDropDownAsync();
        }

        public Task ClearAsync()
        {
            return OnClearClickAsync(null);
        }

        /// <summary>
        /// 滚轮居中（EP 行为）：弹层打开后由 JS 把当前时/分/秒停在中间选中带，
        /// 滚动停止时吸附并回传新值——否则「显示停在哪一项」与真实值会脱节。
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            try
            {
                if (IsDropDownOpen)
                {
                    timeSpinnerRef ??= DotNetObjectReference.Create(this);
                    if (!timeSpinnerBound)
                    {
                        await JSRuntime.InvokeVoidAsync("elementTimeSpinnerAttach", timePanelElement, timeSpinnerRef, "single");
                        timeSpinnerBound = true;
                    }
                    else
                    {
                        await JSRuntime.InvokeVoidAsync("elementTimeSpinnerAlign", timePanelElement);
                    }
                }
                else if (timeSpinnerBound)
                {
                    await JSRuntime.InvokeVoidAsync("elementTimeSpinnerDispose", timePanelElement);
                    timeSpinnerBound = false;
                }

                if (IsDropDownOpen && !outsideClickBound)
                {
                    outsideClickRef ??= DotNetObjectReference.Create(this);
                    await JSRuntime.InvokeVoidAsync(
                        "elementPickerOutsideAttach",
                        pickerElement,
                        ".el-time-panel__popover",
                        outsideClickRef,
                        nameof(OnOutsideClickAsync));
                    outsideClickBound = true;
                }
                else if (!IsDropDownOpen && outsideClickBound)
                {
                    await JSRuntime.InvokeVoidAsync("elementPickerOutsideDetach", pickerElement);
                    outsideClickBound = false;
                }
            }
            catch (JSException)
            {
                // 预渲染 / 无 JS 环境：退化为纯点击选择
            }
        }

        /// <summary>点弹层外面（document 级 mousedown）时关闭。</summary>
        [JSInvokable]
        public Task OnOutsideClickAsync()
        {
            return CloseDropDownAsync();
        }

        /// <summary>JS 回调：滚动停止后取最接近选中带的那一项（panel 区分范围模式的 start / end）。</summary>
        [JSInvokable]
        public Task OnTimeSpinnerSelect(string panel, string part, int value)
        {
            var spinnerPart = part switch
            {
                "minute" => TimePart.Minute,
                "second" => TimePart.Second,
                _ => TimePart.Hour
            };

            return panel switch
            {
                "start" => SetRangeStartPendingPartAsync(spinnerPart, value),
                "end" => SetRangeEndPendingPartAsync(spinnerPart, value),
                _ => SetSinglePendingPartAsync(spinnerPart, value)
            };
        }

        public override void Dispose()
        {
            if (dropDownOption != null)
            {
                PopupService.SelectDropDownOptionsCore.Remove(dropDownOption);
            }

            if (timeSpinnerBound)
            {
                _ = JSRuntime.InvokeVoidAsync("elementTimeSpinnerDispose", timePanelElement);
                timeSpinnerBound = false;
            }

            if (outsideClickBound)
            {
                _ = JSRuntime.InvokeVoidAsync("elementPickerOutsideDetach", pickerElement);
                outsideClickBound = false;
            }

            timeSpinnerRef?.Dispose();
            timeSpinnerRef = null;
            outsideClickRef?.Dispose();
            outsideClickRef = null;

            base.Dispose();
        }

        private void InitializePendingValues()
        {
            pendingSingleValue = Value ?? DefaultValue ?? TimeSpan.Zero;
            pendingRangeStart = RangeStart ?? DefaultStartTime ?? DefaultValue ?? TimeSpan.Zero;
            pendingRangeEnd = RangeEnd ?? DefaultEndTime ?? DefaultValue ?? TimeSpan.Zero;
        }

        private void ClearPendingValues()
        {
            pendingSingleValue = null;
            pendingRangeStart = null;
            pendingRangeEnd = null;
        }

        private void SyncValueFromFormattedParameters()
        {
            if (!string.IsNullOrWhiteSpace(ValueFormat) && !Value.HasValue && !string.IsNullOrWhiteSpace(StringValue) && TryParseTime(StringValue, ValueFormat, out var parsed))
            {
                Value = parsed;
            }
            if (!string.IsNullOrWhiteSpace(ValueFormat) && IsRangeMode && !HasAnyRangeValue && StringRangeValue?.Count >= 2)
            {
                var parsedValues = StringRangeValue.Take(2)
                    .Select(x => TryParseTime(x, ValueFormat, out var parsedTime) ? (TimeSpan?)parsedTime : null)
                    .ToList();
                if (parsedValues.Any(x => x.HasValue))
                {
                    RangeValue = NormalizeRange(parsedValues);
                }
            }
        }

        private IList<TimeSpan?> NormalizeRange(IEnumerable<TimeSpan?> values)
        {
            var range = values?.Take(2).Select(NormalizeTime).ToList() ?? new List<TimeSpan?>();
            while (range.Count < 2)
            {
                range.Add(null);
            }
            if (range[0].HasValue && range[1].HasValue && range[0] > range[1])
            {
                return new List<TimeSpan?> { range[1], range[0] };
            }
            return range;
        }

        private static TimeSpan? NormalizeTime(TimeSpan? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            var ticks = value.Value.Ticks % TimeSpan.FromDays(1).Ticks;
            if (ticks < 0)
            {
                ticks += TimeSpan.FromDays(1).Ticks;
            }
            return TimeSpan.FromTicks(ticks);
        }

        private static TimeSpan UpdatePart(TimeSpan source, TimePart part, int value)
        {
            var hour = source.Hours;
            var minute = source.Minutes;
            var second = source.Seconds;
            switch (part)
            {
                case TimePart.Hour:
                    hour = value;
                    break;
                case TimePart.Minute:
                    minute = value;
                    break;
                case TimePart.Second:
                    second = value;
                    break;
            }

            return new TimeSpan(hour, minute, second);
        }

        private string FormatDisplayValue(TimeSpan? value)
        {
            if (!value.HasValue)
            {
                return string.Empty;
            }
            return FormatTime(value.Value, Format);
        }

        private string FormatBindingValue(TimeSpan? value)
        {
            if (!value.HasValue)
            {
                return string.Empty;
            }
            return string.IsNullOrWhiteSpace(ValueFormat) ? null : FormatTime(value.Value, ValueFormat);
        }

        private static string FormatTime(TimeSpan value, string format)
        {
            var normalized = NormalizeTime(value) ?? TimeSpan.Zero;
            return DateTime.Today.Add(normalized).ToString(ToDotNetTimeFormat(format), CultureInfo.CurrentCulture);
        }

        private static bool TryParseTime(string value, string format, out TimeSpan result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (DateTime.TryParseExact(value, ToDotNetTimeFormat(format), CultureInfo.CurrentCulture, DateTimeStyles.None, out var exact))
            {
                result = exact.TimeOfDay;
                return true;
            }
            if (TimeSpan.TryParse(value, CultureInfo.CurrentCulture, out result)
                || TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out result))
            {
                result = NormalizeTime(result) ?? TimeSpan.Zero;
                return true;
            }
            if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out var current)
                || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out current))
            {
                result = current.TimeOfDay;
                return true;
            }
            return false;
        }

        private static TimeSpan? ConvertToTime(object value)
        {
            if (value == null)
            {
                return null;
            }
            if (value is TimeSpan time)
            {
                return NormalizeTime(time);
            }
            if (value is DateTime dateTime)
            {
                return dateTime.TimeOfDay;
            }
            return TryParseTime(Convert.ToString(value, CultureInfo.CurrentCulture), "HH:mm:ss", out var parsed)
                ? parsed
                : null;
        }

        private static string ToDotNetTimeFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return "HH:mm:ss";
            }

            return format
                .Replace("A", "tt", StringComparison.Ordinal)
                .Replace("a", "tt", StringComparison.Ordinal);
        }

        private static string GetPartName(TimePart part) => part switch
        {
            TimePart.Hour => "hour",
            TimePart.Minute => "minute",
            _ => "second"
        };

        private bool IsRangeMode => IsRange || Type == TimePickerType.TimeRange;

        private bool HasAnyRangeValue => RangeValue?.Any(x => x.HasValue) == true;

        private TimeSpan? RangeStart => RangeValue?.ElementAtOrDefault(0);

        private TimeSpan? RangeEnd => RangeValue?.ElementAtOrDefault(1);

        private string StartDisplayValue => FormatDisplayValue(RangeStart);

        private string EndDisplayValue => FormatDisplayValue(RangeEnd);

        private string SingleDisplayValue => FormatDisplayValue(Value);

        private TimeSpan PendingSingleTimeValue => pendingSingleValue ?? Value ?? DefaultValue ?? TimeSpan.Zero;

        private TimeSpan PendingStartTimeValue => pendingRangeStart ?? RangeStart ?? DefaultStartTime ?? DefaultValue ?? TimeSpan.Zero;

        private TimeSpan PendingEndTimeValue => pendingRangeEnd ?? RangeEnd ?? DefaultEndTime ?? DefaultValue ?? TimeSpan.Zero;

        private bool ShowMinutes => FormatIncludes("m");

        private bool ShowSeconds => FormatIncludes("s");

        private bool FormatIncludes(string token)
        {
            return (!string.IsNullOrWhiteSpace(Format) && Format.Contains(token, StringComparison.Ordinal))
                || (!string.IsNullOrWhiteSpace(ValueFormat) && ValueFormat.Contains(token, StringComparison.Ordinal));
        }

        private string DropDownPanelClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add(IsRangeMode ? "el-time-panel el-time-range-picker" : "el-time-panel")
            .ToString();

        private string SingleWrapperClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-input", "el-date-editor", "el-input--prefix", "el-input--suffix", "el-date-editor--time", Cls)
            .AddIf(SizeCssValue != null, $"el-input--{SizeCssValue}")
            .AddIf(effectiveDisabled, "is-disabled")
            .AddIf(isFocus || IsDropDownOpen, "is-focus")
            .ToString();

        private string RangeWrapperClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-date-editor", "el-range-editor", "el-input__inner", "el-date-editor--timerange", Cls)
            .AddIf(SizeCssValue != null, $"el-range-editor--{SizeCssValue}")
            .AddIf(effectiveDisabled, "is-disabled")
            .AddIf(isFocus || IsDropDownOpen, "is-active")
            .ToString();

        private string SizeCssValue => effectiveSize switch
        {
            InputSize.Large => "large",
            InputSize.Small => "small",
            _ => null
        };

        private string SuffixIconClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-input__icon")
            .Add(Clearable && HasValue && isHovering && !effectiveDisabled && !Readonly ? ClearIcon : "el-icon-arrow-up")
            .AddIf(IsDropDownOpen, "is-reverse")
            .ToString();

        private string RangeCloseIconClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-input__icon", "el-range__close-icon")
            .Add(Clearable && HasValue && isHovering && !effectiveDisabled && !Readonly ? ClearIcon : string.Empty)
            .ToString();

        private bool HasValue => IsRangeMode ? HasAnyRangeValue : Value.HasValue;

        private bool IsDropDownOpen => dropDownOption != null && dropDownOption.IsShow;

        private bool IsInputReadonly => Readonly || !Editable;

        private bool CanConfirmPending => IsRangeMode ? pendingRangeStart.HasValue && pendingRangeEnd.HasValue : pendingSingleValue.HasValue;

        private enum TimePart
        {
            Hour,
            Minute,
            Second
        }
    }
}
