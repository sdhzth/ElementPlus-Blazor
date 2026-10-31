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
    public partial class ElDateTimePicker
    {
        private static long dropDownIdSeed;
        private readonly string DropDownId = $"el-date-time-picker-dropdown-{Interlocked.Increment(ref dropDownIdSeed)}";
        private DropDownOption dropDownOption;
        private ElementReference pickerElement;
        private ElementReference inputElement;
        private bool effectiveDisabled;
        private InputSize effectiveSize = InputSize.Normal;
        private bool isFocus;
        private bool isHovering;
        private DateTime? pendingSingleValue;
        private DateTime? pendingRangeStart;
        private DateTime? pendingRangeEnd;

        /// <summary>当前展开滚轮面板的时间编辑器："single" / "start" / "end"，null 表示都收起。</summary>
        private string openTimePanel;

        /// <summary>打开滚轮面板时的时间快照，供面板「取消」回滚。</summary>
        private TimeSpan? timePanelSnapshot;

        /// <summary>弹层打开时的值：EP 的 change 只在弹层关闭时发，且要求"与打开时不同"（picker.vue 的 watch(pickerVisible) + valueOnOpen）。</summary>
        private DateTime? valueOnOpen;

        // 时间滚轮面板：EP 是「滚动选中」，必须靠 JS 把当前项停在中间那条选中带上，
        // 以及「点击弹层外部关闭」——两者都需要元素引用 + JS 互操作引用。
        private ElementReference timePanelElement;
        private DotNetObjectReference<ElDateTimePicker> timeSpinnerRef;
        private DotNetObjectReference<ElDateTimePicker> outsideClickRef;
        private bool timeSpinnerBound;
        private bool outsideClickBound;

        [Inject]
        internal PopupService PopupService { get; set; }

        [Parameter]
        public DateTime? Value { get; set; }

        [Parameter]
        public DateTime? ModelValue
        {
            get => Value;
            set => Value = value;
        }

        [Parameter]
        public EventCallback<DateTime?> ValueChanged { get; set; }

        [Parameter]
        public EventCallback<DateTime?> ModelValueChanged { get; set; }

        [Parameter]
        public DateTime? Date
        {
            get => Value;
            set => Value = value;
        }

        [Parameter]
        public EventCallback<DateTime?> DateChanged { get; set; }

        [Parameter]
        public IList<DateTime?> RangeValue { get; set; } = new List<DateTime?>();

        [Parameter]
        public IList<DateTime?> ModelRangeValue
        {
            get => RangeValue;
            set => RangeValue = value;
        }

        [Parameter]
        public EventCallback<IList<DateTime?>> RangeValueChanged { get; set; }

        [Parameter]
        public EventCallback<IList<DateTime?>> ModelRangeValueChanged { get; set; }

        [Parameter]
        public string StringValue { get; set; }

        [Parameter]
        public EventCallback<string> StringValueChanged { get; set; }

        [Parameter]
        public IList<string> StringRangeValue { get; set; } = new List<string>();

        [Parameter]
        public EventCallback<IList<string>> StringRangeValueChanged { get; set; }

        [Parameter]
        public DateTimePickerType Type { get; set; } = DateTimePickerType.DateTime;

        [Parameter]
        public string Format { get; set; } = "yyyy-MM-dd HH:mm:ss";

        [Parameter]
        public string ValueFormat { get; set; }

        [Parameter]
        public DateTime? DefaultValue { get; set; }

        [Parameter]
        public TimeSpan? DefaultTime { get; set; }

        [Parameter]
        public TimeSpan? DefaultStartTime { get; set; }

        [Parameter]
        public TimeSpan? DefaultEndTime { get; set; }

        [Parameter]
        public string Placeholder { get; set; } = "请选择日期时间";

        [Parameter]
        public string StartPlaceholder { get; set; } = "开始日期时间";

        [Parameter]
        public string EndPlaceholder { get; set; } = "结束日期时间";

        [Parameter]
        public string RangeSeparator { get; set; } = "-";

        /// <summary>面板「时间」标签文字；留空时按当前语言取（默认中文「时间」）</summary>
        [Parameter]
        public string TimeLabelText { get; set; }

        /// <summary>面板「开始」标签文字；留空时按当前语言取（默认中文「开始」）</summary>
        [Parameter]
        public string StartTimeLabelText { get; set; }

        /// <summary>面板「结束」标签文字；留空时按当前语言取（默认中文「结束」）</summary>
        [Parameter]
        public string EndTimeLabelText { get; set; }

        /// <summary>面板确认按钮文字（时间滚轮面板与页脚共用）；留空时按当前语言取（默认中文「确认」）</summary>
        [Parameter]
        public string ConfirmText { get; set; }

        /// <summary>时间滚轮面板取消按钮文字；留空时按当前语言取（默认中文「取消」）</summary>
        [Parameter]
        public string CancelText { get; set; }

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

        /// <summary>
        /// <b>本库扩展</b>（EP 2.14.0 的 <c>datePickerProps</c> 里没有任何控制"点日期即关闭"的属性）。
        /// 默认 <c>false</c> ＝ EP 行为：点日期立刻写值，但面板留着好继续选时间。
        /// 置 <c>true</c> 时点日期即提交并收起面板，时间沿用当前值、没有当前值时走 <c>DefaultTime</c>。
        /// 只作用于单值档（<see cref="DateTimePickerType.DateTime"/>）；范围档要两端都选完才有意义，不受该参数影响。
        /// </summary>
        [Parameter]
        public bool CloseOnDatePick { get; set; }

        [Parameter]
        public InputSize Size { get; set; } = InputSize.Normal;

        [Parameter]
        public string PrefixIcon { get; set; } = "el-icon-time";

        [Parameter]
        public string ClearIcon { get; set; } = "el-icon-circle-close";

        /// <summary>点击面板外部是否保持打开（EP persistent 语义，default: false 即点外部关闭）。</summary>
        [Parameter]
        public bool Persistent { get; set; } = false;

        /// <summary>挂载到哪个 DOM 元素 (2.10.5)</summary>
        [Parameter]
        public string AppendTo { get; set; }

        /// <summary>原生 tabindex (2.13.4)</summary>
        [Parameter]
        public int? Tabindex { get; set; }

        /// <summary>自定义单元格类名函数（透传到 DatePickerPanel）</summary>
        [Parameter]
        public Func<DateTime, string> CellClassName { get; set; }

        /// <summary>清空后回写的值（对齐 EP value-on-clear，默认 null 即清空为 null）。</summary>
        [Parameter]
        public DateTime? ValueOnClear { get; set; }

        [Parameter]
        public string PopperClass { get; set; }

        [Parameter]
        public string PopperStyle { get; set; }

        [Parameter]
        public int PopperMaxHeight { get; set; } = 460;

        /// <summary>
        /// 紧凑模式：弹出的日期面板整体收窄（单元格、表头、内外边距同步缩小），弹层宽度也随之变窄。
        /// 默认 false（常规尺寸）。适合密集表单、表格内联编辑。
        /// 面板几何的统一真源是 index.css 补丁层（原 fix.css）的「日期面板紧凑模式」段；本属性只负责挂 is-compact 类与收窄弹层宽度。
        /// </summary>
        [Parameter]
        public bool Compact { get; set; }

        [Parameter]
        public bool SinglePanel { get; set; }

        [Parameter]
        public bool ValidateEvent { get; set; } = true;

        [Parameter]
        public Func<DateTime, bool> DisabledDate { get; set; }

        [Parameter]
        public IEnumerable<DateTimePickerShortcut> Shortcuts { get; set; }

        [Parameter]
        public EventCallback<DateTime?> OnChange { get; set; }

        [Parameter]
        public EventCallback<IList<DateTime?>> OnRangeChange { get; set; }

        [Parameter]
        public EventCallback OnClear { get; set; }

        [Parameter]
        public EventCallback<bool> OnVisibleChange { get; set; }

        [Parameter]
        public EventCallback<FocusEventArgs> OnFocus { get; set; }

        [Parameter]
        public EventCallback<FocusEventArgs> OnBlur { get; set; }

        /// <summary>面板切换（翻月 / 翻年 / 选月 / 选年）事件，携带切换后显示的日期（对齐 EP onPanel-change，只暴露日期部分；视图类型由面板内部处理）。</summary>
        [Parameter]
        public EventCallback<DateTime?> OnPanelChange { get; set; }

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
                    Value = ConvertToDateTime(FormItem.OriginValue);
                }
                SetFieldValue(Value, false);
            }
        }

        protected override void FormItem_OnReset(object value, bool requireRerender)
        {
            Value = ConvertToDateTime(value);
            ClearPendingValues();
            if (ValueChanged.HasDelegate)
            {
                _ = ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                _ = ModelValueChanged.InvokeAsync(Value);
            }
            if (DateChanged.HasDelegate)
            {
                _ = DateChanged.InvokeAsync(Value);
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
            if (ValidateEvent)
            {
                SetFieldValue(Value, true);
            }
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
            if (!TryParseDateTime(text, Format, out var parsed))
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
            if (!TryParseDateTime(startText, Format, out var start) || !TryParseDateTime(endText, Format, out var end))
            {
                return;
            }

            await CommitRangeValueAsync(NormalizeRange(new DateTime?[] { start, end }), ValidateEvent, notifyChange: true, close: false);
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

            if (IsRange)
            {
                await CommitRangeValueAsync(new List<DateTime?>(), ValidateEvent, notifyChange: true, close: true);
            }
            else
            {
                // EP value-on-clear：清除时回写自定义值（默认 null）。
                await CommitSingleValueAsync(ValueOnClear, ValidateEvent, notifyChange: true, close: true);
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
            valueOnOpen = Value;
            dropDownOption = new DropDownOption
            {
                Target = pickerElement,
                OptionContent = BuildDropDownContent,
                PopperClass = string.Join(" ", new[] { "el-date-picker__dropdown", "el-date-time-picker__dropdown", PopperClass }.Where(x => !string.IsNullOrWhiteSpace(x))),
                PopperStyle = PopperStyle,
                MaxHeight = PopperMaxHeight,
                FitInputWidth = false,
                AutoWidth = false,
                Width = DropDownWidth,
                DropDownId = DropDownId,
                Refresh = () => InvokeAsync(StateHasChanged),
                OnClosed = () => NotifyVisibleChangeAsync(false),
                IsShow = true,
                Persistent = Persistent,
                AppendTo = AppendTo
            };
            PopupService.SelectDropDownOptionsCore.Add(dropDownOption);
            await NotifyVisibleChangeAsync(true);
        }

        private async Task CloseDropDownAsync()
        {
            // 弹层关闭即收起时间滚轮：否则下次打开时滚轮还挂在面板上（EP 不会这样）。
            openTimePanel = null;
            timePanelSnapshot = null;

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

        private async Task NotifyVisibleChangeAsync(bool visible)
        {
            if (!visible)
            {
                dropDownOption = null;
                // EP 的 change 时机：面板关闭那一刻才发，且只在"与打开时不同"（picker.vue 的 watch(pickerVisible) + valueOnOpen）。
                // 点日期改成即写值之后，「点日期 → 点面板外」这条路径以前一次 change 都不会发。
                var notifyCloseChange = !IsRange && OnChange.HasDelegate && !Equals(Value, valueOnOpen);
                ClearPendingValues();
                if (notifyCloseChange)
                {
                    valueOnOpen = Value;
                    await OnChange.InvokeAsync(Value);
                }
            }

            if (OnVisibleChange.HasDelegate)
            {
                await OnVisibleChange.InvokeAsync(visible);
                return;
            }

            StateHasChanged();
        }

        private void BuildDropDownContent(RenderTreeBuilder builder)
        {
            var seq = 0;
            builder.OpenElement(seq++, "li");
            builder.AddAttribute(seq++, "class", "el-date-picker__panel-item");
            builder.AddAttribute(seq++, "role", "presentation");
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", DropDownPanelClass);
            if (ShortcutList.Any())
            {
                builder.OpenElement(seq++, "div");
                builder.AddAttribute(seq++, "class", "el-picker-panel__sidebar");
                foreach (var shortcut in ShortcutList)
                {
                    builder.OpenElement(seq++, "button");
                    builder.AddAttribute(seq++, "type", "button");
                    builder.AddAttribute(seq++, "class", "el-picker-panel__shortcut");
                    builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => SelectShortcutAsync(shortcut)));
                    builder.AddContent(seq++, shortcut.Text);
                    builder.CloseElement();
                }
                builder.CloseElement();
            }

            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", ShortcutList.Any() ? "el-picker-panel__body" : "el-picker-panel__body-wrapper");
            if (IsRange && !SinglePanel)
            {
                BuildRangePanel(builder, ref seq, true);
                BuildRangePanel(builder, ref seq, false);
            }
            else
            {
                BuildSinglePanel(builder, ref seq);
            }
            builder.CloseElement();
            BuildFooter(builder, ref seq);
            builder.CloseElement();
            builder.CloseElement();
        }

        private void BuildSinglePanel(RenderTreeBuilder builder, ref int seq)
        {
            builder.OpenComponent<ElDatePickerPanel>(seq++);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.Value), IsRange ? pendingRangeStart ?? RangeStart : pendingSingleValue ?? Value);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.Type), DatePickerPanelType.Date);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.DefaultValue), DefaultValue ?? pendingSingleValue ?? Value ?? RangeStart ?? DateTime.Today);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.DisabledDate), DisabledDate);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.CellClassName), CellClassName);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.Border), false);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.Compact), Compact);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.ValidateEvent), false);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.OnPick), EventCallback.Factory.Create<DatePickerPanelSelection>(this, OnPanelPickAsync));
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.OnPanelChange), EventCallback.Factory.Create<DateTime>(this, OnInnerPanelChangeAsync));
            builder.CloseComponent();
        }

        private void BuildRangePanel(RenderTreeBuilder builder, ref int seq, bool left)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", left ? "el-date-range-picker__content is-left" : "el-date-range-picker__content is-right");
            builder.OpenComponent<ElDatePickerPanel>(seq++);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.Value), left ? pendingRangeStart ?? RangeStart : pendingRangeEnd ?? RangeEnd);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.Type), DatePickerPanelType.Date);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.DefaultValue), GetRangePanelDefaultValue(left));
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.DisabledDate), DisabledDate);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.CellClassName), CellClassName);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.Border), false);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.Compact), Compact);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.ValidateEvent), false);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.OnPick), EventCallback.Factory.Create<DatePickerPanelSelection>(this, OnPanelPickAsync));
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.OnPanelChange), EventCallback.Factory.Create<DateTime>(this, OnInnerPanelChangeAsync));
            builder.CloseComponent();
            builder.CloseElement();
        }

        private void BuildFooter(RenderTreeBuilder builder, ref int seq)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "el-picker-panel__footer el-date-time-picker__footer");
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "el-date-time-picker__time-wrapper");
            if (IsRange)
            {
                BuildTimeField(builder, ref seq, "start", EffectiveStartTimeLabel, PendingStartTimeOfDay);
                BuildTimeField(builder, ref seq, "end", EffectiveEndTimeLabel, PendingEndTimeOfDay);
            }
            else
            {
                BuildTimeField(builder, ref seq, "single", EffectiveTimeLabel, PendingSingleTimeOfDay);
            }
            builder.CloseElement();
            builder.OpenElement(seq++, "button");
            builder.AddAttribute(seq++, "type", "button");
            builder.AddAttribute(seq++, "class", "el-button el-button--text el-picker-panel__link-btn el-date-time-picker__confirm");
            builder.AddAttribute(seq++, "disabled", !CanConfirmPending);
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, ConfirmPendingAsync));
            builder.OpenElement(seq++, "span");
            builder.AddContent(seq++, EffectiveConfirmText);
            builder.CloseElement();
            builder.CloseElement();
            builder.CloseElement();
        }

        /// <summary>
        /// 时间编辑器：只读输入直接显示 <c>HH:mm:ss</c>，点击在其上方展开 EP 风格的时分秒滚轮面板。
        /// <para>
        /// 旧实现是原生 <c>&lt;input type="time"&gt;</c>，而 EP 的 <c>.el-picker-panel__footer</c> 带
        /// <c>font-size:0</c>（用于消除 inline-block 间隙），输入框继承后**文字字号为 0 → 时间完全看不见**；
        /// 这里显式给 12px 字号，并改用滚轮选择（对齐 EP TimePicker 面板）。
        /// </para>
        /// </summary>
        private void BuildTimeField(RenderTreeBuilder builder, ref int seq, string panel, string label, TimeSpan time)
        {
            var isOpen = openTimePanel == panel;
            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "class", "el-date-time-picker__time-field");
            builder.AddAttribute(seq++, "data-time-panel-host", panel);
            if (!string.IsNullOrEmpty(label))
            {
                builder.OpenElement(seq++, "span");
                builder.AddAttribute(seq++, "class", "el-date-time-picker__time-label");
                builder.AddContent(seq++, label);
                builder.CloseElement();
            }
            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "class", "el-date-time-picker__time-editor");
            builder.OpenElement(seq++, "input");
            builder.AddAttribute(seq++, "class", $"el-input__inner el-date-time-picker__time-input el-date-time-picker__{panel}-time");
            builder.AddAttribute(seq++, "type", "text");
            builder.AddAttribute(seq++, "readonly", true);
            builder.AddAttribute(seq++, "value", FormatTime(time));
            builder.AddAttribute(seq++, "disabled", effectiveDisabled);
            builder.AddAttribute(seq++, "aria-label", label);
            builder.AddAttribute(seq++, "aria-expanded", isOpen ? "true" : "false");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => ToggleTimePanelAsync(panel)));
            builder.CloseElement();
            builder.OpenElement(seq++, "i");
            builder.AddAttribute(seq++, "class", "el-input__icon el-icon-time el-date-time-picker__time-icon");
            builder.AddAttribute(seq++, "aria-hidden", true);
            builder.CloseElement();
            if (isOpen)
            {
                BuildTimePopover(builder, ref seq, panel, time);
            }
            builder.CloseElement();
            builder.CloseElement();
        }

        /// <summary>EP TimePicker 面板：时 / 分 / 秒三列滚轮 + 取消 / 确认（文案随语言，默认中文）。</summary>
        private void BuildTimePopover(RenderTreeBuilder builder, ref int seq, string panel, TimeSpan time)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "el-time-panel el-date-time-picker__time-panel");
            builder.AddAttribute(seq++, "data-time-panel", panel);
            builder.AddElementReferenceCapture(seq++, el => timePanelElement = el);
            builder.OpenElement(seq++, "div");
            // has-seconds：EP 用它决定中间选中带的横线跨越几列（index.css）
            builder.AddAttribute(seq++, "class", "el-time-panel__content has-seconds");
            TimeSpinnerRenderer.BuildSpinner(
                builder,
                ref seq,
                this,
                panel,
                time,
                showMinutes: true,
                showSeconds: true,
                onSelect: (part, value) => SetTimePartAsync(panel, part, value));
            builder.CloseElement();
            TimeSpinnerRenderer.BuildFooter(
                builder,
                ref seq,
                EffectiveCancelText,
                EffectiveConfirmText,
                confirmDisabled: false,
                onCancel: EventCallback.Factory.Create<MouseEventArgs>(this, CancelTimePanelAsync),
                onConfirm: EventCallback.Factory.Create<MouseEventArgs>(this, ConfirmTimePanelAsync));
            builder.CloseElement();
        }

        private async Task OnPanelPickAsync(DatePickerPanelSelection selection)
        {
            if (selection == null)
            {
                return;
            }

            if (!IsRange)
            {
                pendingSingleValue = CombineDateAndTime(selection.Value, pendingSingleValue?.TimeOfDay ?? Value?.TimeOfDay ?? DefaultTime ?? TimeSpan.Zero);
                // EP 点日期即 update:modelValue（emitInput），面板保持打开好继续选时间；change 与表单校验都等面板关闭才走，
                // 所以不关面板时 notifyChange / validate 都给 false —— CloseOnDatePick 档这一次点击就是"选完即关"，两项都要发。
                await CommitSingleValueAsync(pendingSingleValue,
                    validate: ValidateEvent && CloseOnDatePick,
                    notifyChange: CloseOnDatePick,
                    close: CloseOnDatePick);
                if (!CloseOnDatePick)
                {
                    // 弹层内容由 ElPopup 宿主渲染，只 StateHasChanged() 刷不到 footer 的 disabled
                    await RefreshPendingAsync();
                }
                return;
            }

            var pickedDate = selection.Value.Date;
            if (!pendingRangeStart.HasValue || HasCompletePendingRange)
            {
                pendingRangeStart = CombineDateAndTime(pickedDate, pendingRangeStart?.TimeOfDay ?? RangeStart?.TimeOfDay ?? DefaultStartTime ?? TimeSpan.Zero);
                pendingRangeEnd = null;
                // 弹层内容由 ElPopup 宿主渲染，只 StateHasChanged() 刷不到 footer 的 disabled
                await RefreshPendingAsync();
                return;
            }

            var startDate = pendingRangeStart.Value.Date;
            var startTime = pendingRangeStart.Value.TimeOfDay;
            var endTime = pendingRangeEnd?.TimeOfDay ?? RangeEnd?.TimeOfDay ?? DefaultEndTime ?? TimeSpan.Zero;
            if (pickedDate < startDate)
            {
                pendingRangeEnd = CombineDateAndTime(startDate, endTime);
                pendingRangeStart = CombineDateAndTime(pickedDate, startTime);
            }
            else
            {
                pendingRangeEnd = CombineDateAndTime(pickedDate, endTime);
            }
            await RefreshPendingAsync();
        }

        /// <summary>面板翻页（翻月 / 翻年 / 选月 / 选年）时把切换后显示的日期上抛给 <see cref="OnPanelChange"/>（对齐 EP onPanel-change）。</summary>
        private Task OnInnerPanelChangeAsync(DateTime displayDate)
        {
            if (OnPanelChange.HasDelegate)
            {
                return OnPanelChange.InvokeAsync(displayDate);
            }
            return Task.CompletedTask;
        }

        /// <summary>滚轮面板中点击某一列后写回待提交的时间。</summary>
        private Task SetTimePartAsync(string panel, TimeSpinnerPart part, int value)
        {
            var current = panel switch
            {
                "start" => PendingStartTimeOfDay,
                "end" => PendingEndTimeOfDay,
                _ => PendingSingleTimeOfDay
            };
            ApplyTimeOfDay(panel, TimeSpinnerRenderer.UpdatePart(current, part, value));
            return RefreshPendingAsync();
        }

        /// <summary>点击时间输入框：同一面板再点一次收起，否则切换过去（同时记录快照供「取消」回滚）。</summary>
        private async Task ToggleTimePanelAsync(string panel)
        {
            if (openTimePanel == panel)
            {
                openTimePanel = null;
                await RefreshPendingAsync();
                return;
            }

            openTimePanel = panel;
            timePanelSnapshot = panel switch
            {
                "start" => PendingStartTimeOfDay,
                "end" => PendingEndTimeOfDay,
                _ => PendingSingleTimeOfDay
            };
            await RefreshPendingAsync();
        }

        private async Task ConfirmTimePanelAsync()
        {
            timePanelSnapshot = null;
            openTimePanel = null;
            await RefreshPendingAsync();
        }

        /// <summary>取消：回滚到打开面板时的时间，再收起。</summary>
        private async Task CancelTimePanelAsync()
        {
            if (timePanelSnapshot.HasValue && openTimePanel != null)
            {
                ApplyTimeOfDay(openTimePanel, timePanelSnapshot.Value);
            }
            timePanelSnapshot = null;
            openTimePanel = null;
            await RefreshPendingAsync();
        }

        private void ApplyTimeOfDay(string panel, TimeSpan time)
        {
            switch (panel)
            {
                case "start":
                    pendingRangeStart = CombineDateAndTime((pendingRangeStart ?? RangeStart ?? DefaultValue ?? DateTime.Today).Date, time);
                    break;
                case "end":
                    pendingRangeEnd = CombineDateAndTime((pendingRangeEnd ?? RangeEnd ?? DefaultValue ?? pendingRangeStart ?? DateTime.Today).Date, time);
                    break;
                default:
                    pendingSingleValue = CombineDateAndTime((pendingSingleValue ?? Value ?? DefaultValue ?? DateTime.Today).Date, time);
                    break;
            }
        }

        private Task RefreshPendingAsync()
        {
            dropDownOption?.RequestRender?.Invoke();
            StateHasChanged();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 时间滚轮 + 关外点（EP 行为）。
        /// <para>
        /// 1. 滚轮：EP 默认「鼠标滚轮滚动选中」，选中项是**停在中线**的那一项，
        ///    所以必须由 JS 把当前值滚到中间带——否则会出现「时间是 14:00:00，
        ///    分 / 秒列却停在 02」这类显示与真实值脱节的问题。
        /// 2. 关外点：EP 的 TimePicker / DatePicker 点击弹层外部即关闭；
        ///    关闭时一并清掉 <see cref="openTimePanel"/>，避免再次打开时滚轮还挂在上面。
        /// </para>
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            try
            {
                if (openTimePanel != null)
                {
                    timeSpinnerRef ??= DotNetObjectReference.Create(this);
                    if (!timeSpinnerBound)
                    {
                        await JSRuntime.InvokeVoidAsync("elementTimeSpinnerAttach", timePanelElement, timeSpinnerRef, openTimePanel);
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
                        ".el-date-time-picker__dropdown",
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
                // 预渲染 / 无 JS 环境：退化为纯点击选择（Blazor 的 @onclick 仍然有效）
            }
        }

        /// <summary>JS 回调：滚动停止后，EP 取最接近中间选中带的那一项作为新值。</summary>
        [JSInvokable]
        public Task OnTimeSpinnerSelect(string panel, string part, int value)
        {
            var target = panel ?? openTimePanel;
            if (target == null)
            {
                return Task.CompletedTask;
            }

            var spinnerPart = part switch
            {
                "minute" => TimeSpinnerPart.Minute,
                "second" => TimeSpinnerPart.Second,
                _ => TimeSpinnerPart.Hour
            };

            return SetTimePartAsync(target, spinnerPart, value);
        }

        /// <summary>JS 回调：点击输入框 / 弹层外部 → 收起滚轮并关闭弹层。</summary>
        [JSInvokable]
        public Task OnOutsideClickAsync()
        {
            return CloseDropDownAsync();
        }

        private async Task ConfirmPendingAsync()
        {
            if (IsRange)
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

        private async Task SelectShortcutAsync(DateTimePickerShortcut shortcut)
        {
            if (shortcut == null)
            {
                return;
            }

            if (IsRange)
            {
                await CommitRangeValueAsync(NormalizeRange(shortcut.ResolveRangeValue()), ValidateEvent, notifyChange: true, close: true);
                return;
            }

            await CommitSingleValueAsync(shortcut.ResolveValue(), ValidateEvent, notifyChange: true, close: true);
        }

        private async Task CommitSingleValueAsync(DateTime? value, bool validate, bool notifyChange, bool close)
        {
            Value = value;
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
            if (DateChanged.HasDelegate)
            {
                await DateChanged.InvokeAsync(Value);
            }
            if (StringValueChanged.HasDelegate)
            {
                await StringValueChanged.InvokeAsync(StringValue);
            }
            if (notifyChange)
            {
                // 这一条路径已经通知过了：面板关闭时那条判据（NotifyVisibleChangeAsync）就不该再发一次。
                valueOnOpen = Value;
                if (OnChange.HasDelegate)
                {
                    await OnChange.InvokeAsync(Value);
                }
            }
            if (close)
            {
                await CloseDropDownAsync();
            }
        }

        private async Task CommitRangeValueAsync(IList<DateTime?> range, bool validate, bool notifyChange, bool close)
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
            var strings = RangeValue.Select(FormatBindingValue).ToList();
            StringRangeValue = strings;
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

        public override void Dispose()
        {
            if (dropDownOption != null)
            {
                PopupService.SelectDropDownOptionsCore.Remove(dropDownOption);
            }

            if (outsideClickBound)
            {
                _ = JSRuntime.InvokeVoidAsync("elementPickerOutsideDetach", pickerElement);
                outsideClickBound = false;
            }

            if (timeSpinnerBound)
            {
                _ = JSRuntime.InvokeVoidAsync("elementTimeSpinnerDispose", timePanelElement);
                timeSpinnerBound = false;
            }

            timeSpinnerRef?.Dispose();
            timeSpinnerRef = null;
            outsideClickRef?.Dispose();
            outsideClickRef = null;

            base.Dispose();
        }

        private void InitializePendingValues()
        {
            pendingSingleValue = Value;
            pendingRangeStart = RangeStart;
            pendingRangeEnd = RangeEnd;
        }

        private void ClearPendingValues()
        {
            pendingSingleValue = null;
            pendingRangeStart = null;
            pendingRangeEnd = null;
        }

        private void SyncValueFromFormattedParameters()
        {
            if (!string.IsNullOrWhiteSpace(ValueFormat) && !Value.HasValue && !string.IsNullOrWhiteSpace(StringValue) && TryParseDateTime(StringValue, ValueFormat, out var parsed))
            {
                Value = parsed;
            }
            if (!string.IsNullOrWhiteSpace(ValueFormat) && IsRange && !HasAnyRangeValue && StringRangeValue?.Count >= 2)
            {
                var parsedValues = StringRangeValue.Take(2)
                    .Select(x => TryParseDateTime(x, ValueFormat, out var parsedDate) ? (DateTime?)parsedDate : null)
                    .ToList();
                if (parsedValues.Any(x => x.HasValue))
                {
                    RangeValue = NormalizeRange(parsedValues);
                }
            }
        }

        private IList<DateTime?> NormalizeRange(IEnumerable<DateTime?> values)
        {
            var range = values?.Take(2).ToList() ?? new List<DateTime?>();
            while (range.Count < 2)
            {
                range.Add(null);
            }
            if (range[0].HasValue && range[1].HasValue && range[0] > range[1])
            {
                return new List<DateTime?> { range[1], range[0] };
            }
            return range;
        }

        private DateTime? GetRangePanelDefaultValue(bool left)
        {
            var fallback = DefaultValue ?? pendingRangeStart ?? RangeStart ?? Value ?? DateTime.Today;
            if (left)
            {
                return fallback;
            }
            return fallback.AddMonths(1);
        }

        private string FormatDisplayValue(DateTime? value)
        {
            if (!value.HasValue)
            {
                return string.Empty;
            }
            return FormatDateTime(value.Value, Format);
        }

        private string FormatBindingValue(DateTime? value)
        {
            if (!value.HasValue)
            {
                return string.Empty;
            }
            return string.IsNullOrWhiteSpace(ValueFormat) ? null : FormatDateTime(value.Value, ValueFormat);
        }

        private string FormatDateTime(DateTime value, string format)
        {
            if (string.Equals(format, "x", StringComparison.Ordinal))
            {
                return new DateTimeOffset(value).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
            }
            if (string.Equals(format, "X", StringComparison.Ordinal))
            {
                return new DateTimeOffset(value).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
            }
            return value.ToString(ToDotNetDateFormat(format), CultureInfo.CurrentCulture);
        }

        private bool TryParseDateTime(string value, string format, out DateTime result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            if (string.Equals(format, "x", StringComparison.Ordinal) && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds))
            {
                result = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).LocalDateTime;
                return true;
            }
            if (string.Equals(format, "X", StringComparison.Ordinal) && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
            {
                result = DateTimeOffset.FromUnixTimeSeconds(seconds).LocalDateTime;
                return true;
            }

            return DateTime.TryParseExact(value, ToDotNetDateFormat(format), CultureInfo.CurrentCulture, DateTimeStyles.None, out result)
                || DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out result)
                || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
        }

        private static bool TryParseTime(string value, out TimeSpan result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            return TimeSpan.TryParse(value, CultureInfo.CurrentCulture, out result)
                || TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out result);
        }

        private static DateTime CombineDateAndTime(DateTime date, TimeSpan time)
        {
            return date.Date.Add(time);
        }

        private static DateTime? ConvertToDateTime(object value)
        {
            if (value == null)
            {
                return null;
            }
            if (value is DateTime dateTime)
            {
                return dateTime;
            }
            if (DateTime.TryParse(Convert.ToString(value, CultureInfo.CurrentCulture), CultureInfo.CurrentCulture, DateTimeStyles.None, out var current))
            {
                return current;
            }
            if (DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.None, out var invariant))
            {
                return invariant;
            }
            return null;
        }

        private static string ToDotNetDateFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return "yyyy-MM-dd HH:mm:ss";
            }
            return format
                .Replace("YYYY", "yyyy", StringComparison.Ordinal)
                .Replace("YY", "yy", StringComparison.Ordinal)
                .Replace("DD", "dd", StringComparison.Ordinal);
        }

        private bool IsRange => Type == DateTimePickerType.DateTimeRange;

        private bool HasAnyRangeValue => RangeValue?.Any(x => x.HasValue) == true;

        private bool HasCompletePendingRange => pendingRangeStart.HasValue && pendingRangeEnd.HasValue;

        private DateTime? RangeStart => RangeValue?.ElementAtOrDefault(0);

        private DateTime? RangeEnd => RangeValue?.ElementAtOrDefault(1);

        private string StartDisplayValue => FormatDisplayValue(RangeStart);

        private string EndDisplayValue => FormatDisplayValue(RangeEnd);

        private string SingleDisplayValue => FormatDisplayValue(Value);

        private TimeSpan PendingSingleTimeOfDay => pendingSingleValue?.TimeOfDay ?? Value?.TimeOfDay ?? DefaultTime ?? TimeSpan.Zero;

        private TimeSpan PendingStartTimeOfDay => pendingRangeStart?.TimeOfDay ?? RangeStart?.TimeOfDay ?? DefaultStartTime ?? TimeSpan.Zero;

        private TimeSpan PendingEndTimeOfDay => pendingRangeEnd?.TimeOfDay ?? RangeEnd?.TimeOfDay ?? DefaultEndTime ?? TimeSpan.Zero;

        /// <summary>「时间」标签：组件参数优先，否则按当前语言（默认中文）。</summary>
        private string EffectiveTimeLabel => PickerLocale.Resolve(ConfigLocale, PickerText.Time, TimeLabelText);

        /// <summary>「开始」标签：组件参数优先，否则按当前语言（默认中文）。</summary>
        private string EffectiveStartTimeLabel => PickerLocale.Resolve(ConfigLocale, PickerText.Start, StartTimeLabelText);

        /// <summary>「结束」标签：组件参数优先，否则按当前语言（默认中文）。</summary>
        private string EffectiveEndTimeLabel => PickerLocale.Resolve(ConfigLocale, PickerText.End, EndTimeLabelText);

        /// <summary>确认按钮文字：<b>组件参数 &gt; <c>el.datepicker.confirm</c> 词条 &gt; 内置表</b>。</summary>
        private string EffectiveConfirmText => PickerLocale.Resolve(ConfigLocale, PickerText.Confirm, ConfirmText, Lang);

        /// <summary>取消按钮文字：<b>组件参数 &gt; <c>el.datepicker.cancel</c> 词条 &gt; 内置表</b>。</summary>
        private string EffectiveCancelText => PickerLocale.Resolve(ConfigLocale, PickerText.Cancel, CancelText, Lang);

        private static string FormatTime(TimeSpan value)
        {
            return value.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
        }

        private IReadOnlyList<DateTimePickerShortcut> ShortcutList => Shortcuts?.Where(x => x != null).ToList() ?? new List<DateTimePickerShortcut>();

        private string DropDownPanelClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add(IsRange && !SinglePanel ? "el-picker-panel el-date-range-picker el-date-time-range-picker" : "el-picker-panel el-date-picker el-date-time-picker")
            .AddIf(ShortcutList.Any(), "has-sidebar")
            .AddIf(Compact, "is-compact")
            .ToString();

        /// <summary>
        /// 弹层宽度（px）。紧凑模式按 <c>content 224px + 左右 margin 15px×2 + 边框</c> 推出单面板 256 /
        /// 双面板 514。<para>与 index.css 补丁层（原 fix.css）「日期面板紧凑模式」段里的内容区宽度 224px 成对存在。</para>
        /// 带快捷选项时按 EP 2.14.0 的 <c>.el-date-picker.has-sidebar{width:438px}</c> /
        /// <c>.el-date-time-picker.has-sidebar</c> 同族口径让出侧栏（单面板 +116、双面板 +110，实抓 <c>Temp/ep2140-full.css</c>），
        /// 与 <c>ElDatePicker.DropDownWidth</c> 同源同改，否则面板被弹层夹住后内容横向溢出。
        /// </summary>
        private int DropDownWidth
        {
            get
            {
                var isDualPanel = IsRange && !SinglePanel;
                var width = (isDualPanel, Compact) switch
                {
                    (true, true) => 514,
                    (true, false) => 646,
                    (false, true) => 256,
                    _ => 322
                };
                return ShortcutList.Any() ? width + (isDualPanel ? 110 : 116) : width;
            }
        }

        private string SingleWrapperClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-input", "el-date-editor", "el-input--prefix", "el-input--suffix", "el-date-editor--datetime", Cls)
            .AddIf(SizeCssValue != null, $"el-input--{SizeCssValue}")
            .AddIf(effectiveDisabled, "is-disabled")
            .AddIf(isFocus || IsDropDownOpen, "is-focus")
            .ToString();

        private string RangeWrapperClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-date-editor", "el-range-editor", "el-input__inner", "el-date-editor--datetimerange", Cls)
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

        private bool HasValue => IsRange ? HasAnyRangeValue : Value.HasValue;

        private bool IsDropDownOpen => dropDownOption != null && dropDownOption.IsShow;

        private bool IsInputReadonly => Readonly || !Editable;

        private bool CanConfirmPending => IsRange ? pendingRangeStart.HasValue && pendingRangeEnd.HasValue : pendingSingleValue.HasValue;
    }
}
