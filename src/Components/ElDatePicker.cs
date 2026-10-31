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
    public partial class ElDatePicker
    {
        private static long dropDownIdSeed;
        private readonly string DropDownId = $"el-date-picker-dropdown-{Interlocked.Increment(ref dropDownIdSeed)}";
        private DropDownOption dropDownOption;
        private ElementReference pickerElement;
        private ElementReference inputElement;
        private bool effectiveDisabled;
        private InputSize effectiveSize = InputSize.Normal;
        private bool isFocus;
        private bool isHovering;
        private DateTime? pendingRangeStart;
        // 双面板范围选择时左右两个面板共用的「左面板月份」。
        // null 表示还没翻过页 —— 由 RangeStart / DefaultValue 推算出锚点月份。
        private DateTime? rangeDisplayMonth;
        // unlink-panels 模式：右面板独立维护的显示月份锚点。
        // 默认 linked 时始终为 null，右月由 RangeLeftMonth 推算（始终保持相邻一个月）。
        private DateTime? rangeDisplayMonthRight;

        [Inject]
        internal PopupService PopupService { get; set; }

                /// <summary>原生 tabindex</summary>
        [Parameter] public int? Tabindex { get; set; }

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
        public DatePickerType Type { get; set; } = DatePickerType.Date;

        [Parameter]
        public string Format { get; set; } = "yyyy-MM-dd";

        [Parameter]
        public string ValueFormat { get; set; }

        [Parameter]
        public DateTime? DefaultValue { get; set; }

        [Parameter]
        public string Placeholder { get; set; } = "请选择日期";

        [Parameter]
        public string StartPlaceholder { get; set; } = "开始日期";

        [Parameter]
        public string EndPlaceholder { get; set; } = "结束日期";

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
        public string PrefixIcon { get; set; } = "el-icon-date";

        [Parameter]
        public string ClearIcon { get; set; } = "el-icon-circle-close";

        [Parameter]
        public string PopperClass { get; set; }

        [Parameter]
        public string PopperStyle { get; set; }

        [Parameter]
        public int PopperMaxHeight { get; set; } = 420;

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

        /// <summary>是否显示周数（EP <c>show-week-number</c>，仅 date 类型生效）。</summary>
        [Parameter]
        public bool ShowWeekNumber { get; set; }

        /// <summary>是否显示底部「确定」按钮（EP <c>show-confirm</c>，daterange 等场景常用）。</summary>
        [Parameter]
        public bool ShowConfirm { get; set; }

        /// <summary>底部自定义内容插槽（EP <c>footer</c> slot）。优先级高于 <see cref="ShowConfirm"/> 的默认确定按钮。</summary>
        [Parameter]
        public RenderFragment Footer { get; set; }

        /// <summary>点击清除按钮时回写的值（EP <c>value-on-clear</c>，默认 null）。仅单选 date 类型生效，范围选择始终清空为 [null,null]。</summary>
        [Parameter]
        public DateTime? ValueOnClear { get; set; }

        /// <summary>范围选择时左右日期面板是否取消联动（EP <c>unlink-panels</c>，2.5.0+）。
        /// 默认 false（联动：翻动任一侧，另一侧跟随以保证两面板始终相邻一个月）。
        /// 为 true 时左右面板独立翻页，互不影响。</summary>
        [Parameter]
        public bool UnlinkPanels { get; set; }

        [Parameter]
        public Func<DateTime, bool> DisabledDate { get; set; }

        [Parameter]
        public IEnumerable<DatePickerShortcut> Shortcuts { get; set; }

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
            pendingRangeStart = null;
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
            _ = CloseDropDownAfterBlurAsync();
            if (OnBlur.HasDelegate)
            {
                await OnBlur.InvokeAsync(e);
            }
        }

        private async Task CloseDropDownAfterBlurAsync()
        {
            await Task.Delay(50);
            if (dropDownOption == null || !IsDropDownOpen) return;

            var js = "function(){var a=document.activeElement;if(!a)return true;var pan=document.querySelector('.el-date-picker__dropdown .el-picker-panel');if(!pan)return true;return !pan.contains(a);}()";
            var shouldClose = await JSRuntime.InvokeAsync<bool>("eval", new object[] { js });
            if (shouldClose && dropDownOption != null)
            {
                await CloseDropDownAsync();
            }
        }

        private async Task OnSingleInputChangedAsync(ChangeEventArgs e)
        {
            if (!Editable || effectiveDisabled || Readonly)
            {
                return;
            }

            var text = Convert.ToString(e.Value, CultureInfo.CurrentCulture);
            if (!TryParseDate(text, Format, out var parsed))
            {
                return;
            }

            await CommitSingleValueAsync(NormalizeValue(parsed), ValidateEvent, notifyChange: true, close: false);
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
            if (!TryParseDate(startText, Format, out var start) || !TryParseDate(endText, Format, out var end))
            {
                return;
            }

            await CommitRangeValueAsync(NormalizeRange(new DateTime?[] { NormalizeValue(start), NormalizeValue(end) }), ValidateEvent, notifyChange: true, close: false);
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

            // 每次打开都重新锚定范围面板的月份（跟着当前已选范围走，而不是上次翻到的地方）。
            rangeDisplayMonth = null;
            rangeDisplayMonthRight = null;
            // unlink-panels：固化右面板初始月份（左月 + 偏移），使其独立于左面板，
            // 之后任一侧翻页只更新各自锚点、互不跟随。
            if (UnlinkPanels)
            {
                rangeDisplayMonthRight = Type == DatePickerType.YearRange
                    ? RangeLeftMonth.AddYears(10)
                    : RangeLeftMonth.AddMonths(1);
            }

            dropDownOption = new DropDownOption
            {
                Target = pickerElement,
                OptionContent = BuildDropDownContent,
                PopperClass = string.Join(" ", new[] { "el-date-picker__dropdown", PopperClass }.Where(x => !string.IsNullOrWhiteSpace(x))),
                PopperStyle = PopperStyle,
                MaxHeight = PopperMaxHeight,
                FitInputWidth = false,
                AutoWidth = false,
                Width = DropDownWidth,
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
                pendingRangeStart = null;
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
            builder.CloseElement();
            builder.CloseElement();
        }

        private void BuildSinglePanel(RenderTreeBuilder builder, ref int seq)
        {
            builder.OpenComponent<ElDatePickerPanel>(seq++);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.Value), IsRange ? pendingRangeStart ?? RangeStart : Value);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.Type), PanelType);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.Locale), ConfigLocale);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.DefaultValue), DefaultValue ?? Value ?? RangeStart ?? DateTime.Today);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.Compact), Compact);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.DisabledDate), DisabledDate);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.Border), false);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.ValidateEvent), false);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.ShowWeekNumber), ShowWeekNumber);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.FooterContent), Footer);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.ShowConfirm), ShowConfirm);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.OnPick), EventCallback.Factory.Create<DatePickerPanelSelection>(this, OnPanelPickAsync));
            builder.CloseComponent();
        }

        private void BuildRangePanel(RenderTreeBuilder builder, ref int seq, bool left)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", left ? "el-date-range-picker__content is-left" : "el-date-range-picker__content is-right");
            builder.OpenComponent<ElDatePickerPanel>(seq++);
            // 注意：**不传 Value** —— 范围面板的显示月份由父级统一驱动（DefaultValue），
            // 传了 Value 面板就会跟着已选日期跑，左右两侧的月份会失联。
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.Type), PanelType);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.Locale), ConfigLocale);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.DefaultValue), left ? RangeLeftMonth : RangeRightMonth);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.RangeRole), left ? DatePickerRangeRole.Start : DatePickerRangeRole.End);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.RangeStart), RangeHighlightStart);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.RangeEnd), RangeHighlightEnd);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.DisabledDate), DisabledDate);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.Compact), Compact);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.Border), false);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.ValidateEvent), false);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.FooterContent), Footer);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.ShowConfirm), ShowConfirm);
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.OnPick), EventCallback.Factory.Create<DatePickerPanelSelection>(this, OnPanelPickAsync));
            builder.AddAttribute(seq++, nameof(ElDatePickerPanel.OnPanelChange), EventCallback.Factory.Create<DateTime>(this, month => OnRangePanelChangeAsync(left, month)));
            builder.CloseComponent();
            builder.CloseElement();
        }

        /// <summary>
        /// 双面板联动：任一侧翻页后重新锚定两侧显示的月份。
        /// <para>linked（默认）：左面板报上来的月份就是左月；右面板报上来的是「左月 + 1」（年范围是 +10 年），反推回去，两侧始终相邻。</para>
        /// <para>unlink-panels：翻动一侧只更新该侧自己的锚点（<see cref="rangeDisplayMonth"/> / <see cref="rangeDisplayMonthRight"/>），对侧保持不变。</para>
        /// </summary>
        private Task OnRangePanelChangeAsync(bool left, DateTime displayMonth)
        {
            if (left)
            {
                rangeDisplayMonth = new DateTime(displayMonth.Year, displayMonth.Month, 1);
            }
            else if (UnlinkPanels)
            {
                // 右面板独立翻页：只更新右侧锚点，左面板不动。
                rangeDisplayMonthRight = new DateTime(displayMonth.Year, displayMonth.Month, 1);
            }
            else
            {
                var month = Type == DatePickerType.YearRange ? displayMonth.AddYears(-10) : displayMonth.AddMonths(-1);
                rangeDisplayMonth = new DateTime(month.Year, month.Month, 1);
            }

            // 弹层内容是由 ElPopup 宿主渲染的（不在本组件的渲染树里）。
            // 被点击的那一侧自己能刷新，但对侧必须靠宿主重绘才会拿到新的 DefaultValue —— 
            // 少了这一步就只有右面板翻页、左面板停在原月份。
            dropDownOption?.RequestRender?.Invoke();

            return InvokeAsync(StateHasChanged);
        }

        /// <summary>左面板显示的月份（范围锚点）。</summary>
        private DateTime RangeLeftMonth
        {
            get
            {
                if (rangeDisplayMonth.HasValue)
                {
                    return rangeDisplayMonth.Value;
                }
                var anchor = RangeHighlightStart ?? DefaultValue ?? Value ?? DateTime.Today;
                return new DateTime(anchor.Year, anchor.Month, 1);
            }
        }

        /// <summary>右面板显示的月份。
        /// <para>unlink-panels 模式且右面板已独立翻过页时，返回固定的 <see cref="rangeDisplayMonthRight"/>；
        /// 否则返回「左月 + 1 月」（年范围 +10 年），保证两面板相邻一个月。</para></summary>
        private DateTime RangeRightMonth
        {
            get
            {
                if (UnlinkPanels && rangeDisplayMonthRight.HasValue)
                {
                    return rangeDisplayMonthRight.Value;
                }
                return Type == DatePickerType.YearRange ? RangeLeftMonth.AddYears(10) : RangeLeftMonth.AddMonths(1);
            }
        }

        private async Task OnPanelPickAsync(DatePickerPanelSelection selection)
        {
            if (selection == null)
            {
                return;
            }

            if (!IsRange)
            {
                await CommitSingleValueAsync(NormalizeValue(selection.Value), ValidateEvent, notifyChange: true, close: true);
                return;
            }

            var picked = NormalizeValue(selection.Value);
            if (!pendingRangeStart.HasValue)
            {
                // 第一次点击 = 记下起点。
                // ⚠️ 不能只改 RangeValue 就完事：RangeValue 是**双向绑定参数**，
                // 父组件下一轮渲染会把旧值写回来，高亮会闪回旧区间。
                // 所以起点单独存在 pendingRangeStart 里（父级碰不到），高亮也以它为准；
                // 同时要显式请求弹层宿主重绘，否则 DOM 里根本看不到这次点击。
                pendingRangeStart = picked;
                RangeValue = new List<DateTime?> { picked, null };
                await NotifyRangeValueChangedAsync(validate: false, notifyChange: false);
                dropDownOption?.RequestRender?.Invoke();
                StateHasChanged();
                return;
            }

            var range = NormalizeRange(new[] { pendingRangeStart, picked });
            pendingRangeStart = null;
            await CommitRangeValueAsync(range, ValidateEvent, notifyChange: true, close: true);
        }

        private async Task SelectShortcutAsync(DatePickerShortcut shortcut)
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

            var value = shortcut.ResolveValue();
            await CommitSingleValueAsync(value.HasValue ? NormalizeValue(value.Value) : null, ValidateEvent, notifyChange: true, close: true);
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
            if (notifyChange && OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(Value);
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
            base.Dispose();
        }

        private void SyncValueFromFormattedParameters()
        {
            if (!string.IsNullOrWhiteSpace(ValueFormat) && !Value.HasValue && !string.IsNullOrWhiteSpace(StringValue) && TryParseDate(StringValue, ValueFormat, out var parsed))
            {
                Value = NormalizeValue(parsed);
            }
            if (!string.IsNullOrWhiteSpace(ValueFormat) && IsRange && !HasAnyRangeValue && StringRangeValue?.Count >= 2)
            {
                var parsedValues = StringRangeValue.Take(2)
                    .Select(x => TryParseDate(x, ValueFormat, out var parsedDate) ? (DateTime?)NormalizeValue(parsedDate) : null)
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

        private DateTime NormalizeValue(DateTime value)
        {
            return Type switch
            {
                DatePickerType.Year or DatePickerType.YearRange => new DateTime(value.Year, 1, 1),
                DatePickerType.Month or DatePickerType.MonthRange => new DateTime(value.Year, value.Month, 1),
                _ => value.Date
            };
        }

        private string FormatDisplayValue(DateTime? value)
        {
            if (!value.HasValue)
            {
                return string.Empty;
            }
            return FormatDate(value.Value, Format);
        }

        private string FormatBindingValue(DateTime? value)
        {
            if (!value.HasValue)
            {
                return string.Empty;
            }
            return string.IsNullOrWhiteSpace(ValueFormat) ? null : FormatDate(value.Value, ValueFormat);
        }

        private string FormatDate(DateTime value, string format)
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

        private bool TryParseDate(string value, string format, out DateTime result)
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
                return "yyyy-MM-dd";
            }
            return format
                .Replace("YYYY", "yyyy", StringComparison.Ordinal)
                .Replace("YY", "yy", StringComparison.Ordinal)
                .Replace("DD", "dd", StringComparison.Ordinal);
        }

        private bool IsRange => Type is DatePickerType.DateRange or DatePickerType.MonthRange or DatePickerType.YearRange;

        private bool HasAnyRangeValue => RangeValue?.Any(x => x.HasValue) == true;

        private DateTime? RangeStart => RangeValue?.ElementAtOrDefault(0);

        private DateTime? RangeEnd => RangeValue?.ElementAtOrDefault(1);

        /// <summary>高亮用的起点：优先未完成选择的起点（父级改不动它）。</summary>
        private DateTime? RangeHighlightStart => pendingRangeStart ?? RangeStart;

        /// <summary>高亮用的终点：已点了起点、还没点终点时为空（EP 的「半选」态）。</summary>
        private DateTime? RangeHighlightEnd => pendingRangeStart.HasValue ? null : RangeEnd;

        private string StartDisplayValue => FormatDisplayValue(RangeStart);

        private string EndDisplayValue => FormatDisplayValue(RangeEnd);

        private string SingleDisplayValue => FormatDisplayValue(Value);

        private IReadOnlyList<DatePickerShortcut> ShortcutList => Shortcuts?.Where(x => x != null).ToList() ?? new List<DatePickerShortcut>();

        private DatePickerPanelType PanelType => Type switch
        {
            DatePickerType.Month or DatePickerType.MonthRange => DatePickerPanelType.Month,
            DatePickerType.Year or DatePickerType.YearRange => DatePickerPanelType.Year,
            _ => DatePickerPanelType.Date
        };

        private string DropDownPanelClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add(IsRange && !SinglePanel ? "el-picker-panel el-date-range-picker" : "el-picker-panel el-date-picker")
            .AddIf(ShortcutList.Any(), "has-sidebar")
            .AddIf(Compact, "is-compact")
            .ToString();

        /// <summary>
        /// 弹层宽度（px）。紧凑模式按 content 224px + 左右 margin 15px×2 + 边框 推出：
        /// 单面板 256、双面板 514。与 index.css 补丁层（原 fix.css）「日期面板紧凑模式」段里的内容区宽度 224px 成对存在，改一处必须改另一处。
        /// <para>
        /// 带快捷选项时要再让出侧栏那一列：EP 2.14.0 的口径是
        /// <c>.el-date-picker.has-sidebar{width:438px}</c>、<c>.el-date-range-picker.has-sidebar{width:756px}</c>
        /// （实抓 <c>Temp/ep2140-full.css</c>，即单面板 +116、双面板 +110；紧凑档官方没有对应值，按同族增量给）。
        /// 少这一截时弹层仍是 322，面板被补丁层的 <c>max-width:100%</c> 夹住、<c>__body</c> 只剩 210 宽，
        /// 307 宽的内容横向溢出 ⇒ 面板底部冒横向滚动条、右侧日期列被裁（批次㉕ #124，/datepicker「快捷选项」）。
        /// </para>
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
            .Add("el-input", "el-date-editor", "el-input--prefix", "el-input--suffix", $"el-date-editor--{TypeCssValue}", Cls)
            .AddIf(SizeCssValue != null, $"el-input--{SizeCssValue}")
            .AddIf(effectiveDisabled, "is-disabled")
            .AddIf(isFocus || IsDropDownOpen, "is-focus")
            .ToString();

        private string RangeWrapperClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-date-editor", "el-range-editor", "el-input__inner", $"el-date-editor--{TypeCssValue}", Cls)
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

        private string TypeCssValue => Type switch
        {
            DatePickerType.Month => "month",
            DatePickerType.Year => "year",
            DatePickerType.DateRange => "daterange",
            DatePickerType.MonthRange => "monthrange",
            DatePickerType.YearRange => "yearrange",
            _ => "date"
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
    }
}
