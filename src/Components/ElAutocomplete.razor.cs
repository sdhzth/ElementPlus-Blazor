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
    public partial class ElAutocomplete : ElementFieldComponentBase<string>, ISelectDropDownContext
    {
        private static long dropDownIdSeed;
        private readonly string DropDownId = $"el-autocomplete-dropdown-{Interlocked.Increment(ref dropDownIdSeed)}";
        private readonly List<AutocompleteOption> currentSuggestions = new List<AutocompleteOption>();
        private CancellationTokenSource queryCancellationTokenSource;
        private DropDownOption dropDownOption;
        private ElementReference autocompleteElement;
        private ElInput<string> input;
        private HtmlPropertyBuilder wrapperClsBuilder;
        private long queryVersion;
        private int highlightedIndex = -1;
        private bool loading;
        private InputSize effectiveSize = InputSize.Normal;
        private bool effectiveDisabled;

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
        public IEnumerable<AutocompleteOption> Suggestions { get; set; }

        [Parameter]
        public Func<string, IEnumerable<AutocompleteOption>> FetchSuggestions { get; set; }

        [Parameter]
        public Func<string, Task<IEnumerable<AutocompleteOption>>> FetchSuggestionsAsync { get; set; }

        [Parameter]
        public RenderFragment<AutocompleteOption> ItemTemplate { get; set; }

        [Parameter]
        public RenderFragment<AutocompleteOption> SuggestionItemTemplate
        {
            get => ItemTemplate;
            set => ItemTemplate = value;
        }

        [Parameter]
        public string Placeholder { get; set; } = "请输入内容";

        [Parameter]
        public bool Clearable { get; set; }

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
        public InputSize Size { get; set; } = InputSize.Normal;

        [Parameter]
        public bool TriggerOnFocus { get; set; } = true;

        [Parameter]
        public bool SelectWhenUnmatched { get; set; }

        [Parameter]
        public int Debounce { get; set; } = 300;

        [Parameter]
        public bool Loading { get; set; }

        [Parameter]
        public bool HideLoading { get; set; }

        [Parameter]
        public string LoadingText { get; set; } = "加载中";

        [Parameter]
        public string NoDataText { get; set; } = "无数据";

        [Parameter]
        public string PopperClass { get; set; }

        [Parameter]
        public string PopperStyle { get; set; }

        [Parameter]
        public int PopperMaxHeight { get; set; } = 280;

        [Parameter]
        public bool FitInputWidth { get; set; } = true;

        [Parameter]
        public string Placement { get; set; } = "bottom-start";

        [Parameter]
        public string PrefixIcon { get; set; }

        [Parameter]
        public string SuffixIcon { get; set; }

        [Parameter]
        public string AdditionalClearIcon { get; set; }

        [Parameter]
        public string NativeAutocomplete { get; set; } = "off";

        [Parameter]
        public string AriaLabel { get; set; }

        /// <summary>是否默认高亮远程搜索结果的第一项 (官方 default: false)</summary>
        [Parameter]
        public bool HighlightFirstItem { get; set; } = false;

        /// <summary>键盘导航是否从末尾循环到开头 (官方 2.11.4, default: true)</summary>
        [Parameter]
        public bool LoopNavigation { get; set; } = true;


        [Parameter]
        public object Tabindex { get; set; } = 0;

        [Parameter]
        public string InputStyle { get; set; }

        [Parameter]
        public bool ValidateEvent { get; set; } = true;

        [Parameter]
        public EventCallback<string> OnInput { get; set; }

        [Parameter]
        public EventCallback<string> OnChange { get; set; }

        [Parameter]
        public EventCallback<AutocompleteOption> OnSelect { get; set; }

        [Parameter]
        public EventCallback<bool> OnVisibleChange { get; set; }

        [Parameter]
        public EventCallback OnClear { get; set; }

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
            var sizeCssValue = GetSizeCssValue(effectiveSize);
            wrapperClsBuilder = HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-autocomplete", Cls)
                .AddIf(effectiveDisabled, "is-disabled")
                .AddIf(IsDropDownOpen, "is-focus")
                .AddIf(sizeCssValue != null, $"el-autocomplete--{sizeCssValue}");

            if (FormItem == null)
            {
                return;
            }

            if (FormItem.OriginValueHasRendered)
            {
                return;
            }

            FormItem.OriginValueHasRendered = true;
            if (FormItem.Form?.Values.Any() ?? false)
            {
                Value = FormItem.OriginValue;
            }
            SetFieldValue(Value, false);
        }

        protected override void FormItem_OnReset(object value, bool requireRerender)
        {
            Value = value == null ? null : Convert.ToString(value);
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

        private async Task OnInputValueChangedAsync(string value)
        {
            Value = value;
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

            await LoadSuggestionsAsync(Value, debounce: true, openWhenReady: true);
        }

        private async Task OnInputFocusAsync(FocusEventArgs e)
        {
            if (TriggerOnFocus)
            {
                await LoadSuggestionsAsync(Value, debounce: false, openWhenReady: true);
            }
            if (OnFocus.HasDelegate)
            {
                await OnFocus.InvokeAsync(e);
            }
        }

        private async Task OnInputBlurAsync(FocusEventArgs e)
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

        private async Task OnInputClearAsync(MouseEventArgs e)
        {
            Value = string.Empty;
            currentSuggestions.Clear();
            highlightedIndex = -1;
            CancelPendingQuery();
            SetFieldValue(Value, ValidateEvent);
            if (OnClear.HasDelegate)
            {
                await OnClear.InvokeAsync();
            }
            await CloseDropDownAsync();
        }

        private async Task OnKeyDownAsync(KeyboardEventArgs e)
        {
            if (effectiveDisabled || Readonly)
            {
                return;
            }

            switch (e.Key)
            {
                case "ArrowDown":
                    if (!IsDropDownOpen)
                    {
                        await LoadSuggestionsAsync(Value, debounce: false, openWhenReady: true);
                        return;
                    }
                    MoveHighlight(1);
                    break;
                case "ArrowUp":
                    if (!IsDropDownOpen)
                    {
                        await LoadSuggestionsAsync(Value, debounce: false, openWhenReady: true);
                        return;
                    }
                    MoveHighlight(-1);
                    break;
                case "Enter":
                    if (IsDropDownOpen)
                    {
                        await SelectHighlightedOrUnmatchedAsync();
                    }
                    break;
                case "Escape":
                    await CloseDropDownAsync();
                    break;
            }
        }

        private async Task LoadSuggestionsAsync(string query, bool debounce, bool openWhenReady)
        {
            if (effectiveDisabled || Readonly)
            {
                return;
            }

            CancelPendingQuery();
            queryCancellationTokenSource = new CancellationTokenSource();
            var token = queryCancellationTokenSource.Token;
            var currentQueryVersion = Interlocked.Increment(ref queryVersion);

            if (debounce && Debounce > 0)
            {
                try
                {
                    await Task.Delay(Debounce, token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }

            // 定时器到点与后键 CancelPendingQuery 之间没有墙钟保证（超额订阅下实测必现，批次㉒ #116）：
            // 续跑晚于 cancel 时 token 已置位但 Delay 已完成，旧写法会照常把被取代的查询发上远端，
            // 事后的 IsCurrentQuery 只能丢弃结果、收不回请求。EP 侧 lodash debounce 在 cancel 时直接不派发。
            if (!IsCurrentQuery(currentQueryVersion, token))
            {
                return;
            }

            SetLoading(true);
            if (openWhenReady)
            {
                await OpenDropDownAsync();
            }

            IEnumerable<AutocompleteOption> options;
            try
            {
                if (FetchSuggestionsAsync != null)
                {
                    options = await FetchSuggestionsAsync(query ?? string.Empty);
                }
                else if (FetchSuggestions != null)
                {
                    options = FetchSuggestions(query ?? string.Empty);
                }
                else
                {
                    options = Suggestions ?? Enumerable.Empty<AutocompleteOption>();
                }
            }
            catch
            {
                if (!IsCurrentQuery(currentQueryVersion, token))
                {
                    return;
                }

                SetLoading(false);
                throw;
            }

            if (!IsCurrentQuery(currentQueryVersion, token))
            {
                return;
            }

            currentSuggestions.Clear();
            currentSuggestions.AddRange(options?.Where(x => x != null) ?? Enumerable.Empty<AutocompleteOption>());
            highlightedIndex = HighlightFirstItem ? currentSuggestions.FindIndex(x => !x.Disabled) : -1;
            SetLoading(false);

            if (openWhenReady)
            {
                await OpenDropDownAsync();
            }
            RefreshDropDown();
        }

        private async Task OpenDropDownAsync()
        {
            if (effectiveDisabled || Readonly)
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
                Select = this,
                Target = autocompleteElement,
                OptionContent = BuildSuggestionContent,
                PopperClass = string.Join(" ", new[] { "el-autocomplete-suggestion", PopperClass }.Where(x => !string.IsNullOrWhiteSpace(x))),
                PopperStyle = PopperStyle,
                MaxHeight = PopperMaxHeight,
                FitInputWidth = FitInputWidth,
                DropDownId = DropDownId,
                Placement = string.IsNullOrWhiteSpace(Placement) ? "bottom-start" : Placement,
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
                highlightedIndex = -1;
            }

            if (OnVisibleChange.HasDelegate)
            {
                return OnVisibleChange.InvokeAsync(visible);
            }

            StateHasChanged();
            return Task.CompletedTask;
        }

        private void MoveHighlight(int step)
        {
            var enabledIndexes = currentSuggestions
                .Select((item, index) => new { item, index })
                .Where(x => !x.item.Disabled)
                .Select(x => x.index)
                .ToList();
            if (!enabledIndexes.Any())
            {
                highlightedIndex = -1;
                return;
            }

            var currentPosition = enabledIndexes.IndexOf(highlightedIndex);
            if (LoopNavigation)
            {
                currentPosition = (currentPosition + step + enabledIndexes.Count) % enabledIndexes.Count;
            }
            else
            {
                currentPosition = Math.Clamp(currentPosition + step, 0, enabledIndexes.Count - 1);
            }
            highlightedIndex = enabledIndexes[currentPosition];
            RefreshDropDown();
        }

        private async Task SelectHighlightedOrUnmatchedAsync()
        {
            if (highlightedIndex >= 0 && highlightedIndex < currentSuggestions.Count)
            {
                await SelectSuggestionAsync(currentSuggestions[highlightedIndex]);
                return;
            }

            if (!SelectWhenUnmatched)
            {
                return;
            }

            await SelectSuggestionAsync(new AutocompleteOption
            {
                Value = Value,
                Label = Value
            });
        }

        private async Task SelectSuggestionAsync(AutocompleteOption option)
        {
            if (option == null || option.Disabled)
            {
                return;
            }

            Value = option.Value ?? option.Label ?? string.Empty;
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
            if (OnSelect.HasDelegate)
            {
                await OnSelect.InvokeAsync(option);
            }
            await CloseDropDownAsync();
        }

        public ValueTask FocusAsync()
        {
            return input?.FocusAsync() ?? ValueTask.CompletedTask;
        }

        public ValueTask BlurAsync()
        {
            return input?.BlurAsync() ?? ValueTask.CompletedTask;
        }

        public async Task ClearAsync()
        {
            Value = string.Empty;
            currentSuggestions.Clear();
            highlightedIndex = -1;
            CancelPendingQuery();
            SetFieldValue(Value, ValidateEvent);
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(Value);
            }
            await CloseDropDownAsync();
        }

        /// <summary>
        /// 折叠（收起）建议列表（对齐 EP <c>close()</c> exposes）。
        /// 若下拉未打开则为无操作；可安全重复调用。
        /// </summary>
        public async Task CloseAsync()
        {
            await CloseDropDownAsync();
        }

        /// <summary>
        /// 展开（显示）建议列表（对齐 EP <c>open()</c> exposes）。
        /// 会根据 <see cref="Suggestions"/> / <see cref="FetchSuggestions"/> / <see cref="FetchSuggestionsAsync"/> 重新拉取候选并弹出面板；
        /// 已禁用或只读时为无操作。
        /// </summary>
        public async Task ShowAsync()
        {
            await LoadSuggestionsAsync(Value, debounce: false, openWhenReady: true);
        }

        public override void Dispose()
        {
            CancelPendingQuery();
            if (dropDownOption != null)
            {
                PopupService.SelectDropDownOptionsCore.Remove(dropDownOption);
            }
            base.Dispose();
        }

        private void CancelPendingQuery()
        {
            Interlocked.Increment(ref queryVersion);
            queryCancellationTokenSource?.Cancel();
            queryCancellationTokenSource?.Dispose();
            queryCancellationTokenSource = null;
            loading = false;
        }

        private bool IsCurrentQuery(long version, CancellationToken token)
        {
            return !token.IsCancellationRequested && Interlocked.Read(ref queryVersion) == version;
        }

        private void SetLoading(bool value)
        {
            loading = value;
            RefreshDropDown();
        }

        private void RefreshDropDown()
        {
            dropDownOption?.RequestRender?.Invoke();
            StateHasChanged();
        }

        private void BuildSuggestionContent(RenderTreeBuilder builder)
        {
            var seq = 0;
            for (var i = 0; i < currentSuggestions.Count; i++)
            {
                var option = currentSuggestions[i];
                var index = i;
                var itemClsBuilder = HtmlPropertyBuilder.CreateCssClassBuilder()
                    .Add("el-autocomplete-suggestion__item")
                    .AddIf(index == highlightedIndex, "highlighted")
                    .AddIf(option.Disabled, "is-disabled");
                builder.OpenElement(seq++, "li");
                builder.SetKey(option);
                builder.AddAttribute(seq++, "class", itemClsBuilder.ToString());
                builder.AddAttribute(seq++, "role", "option");
                builder.AddAttribute(seq++, "aria-selected", index == highlightedIndex);
                builder.AddAttribute(seq++, "aria-disabled", option.Disabled);
                builder.AddAttribute(seq++, "tabindex", option.Disabled ? -1 : 0);
                builder.AddAttribute(seq++, "onmouseover", EventCallback.Factory.Create<MouseEventArgs>(this, () =>
                {
                    if (!option.Disabled)
                    {
                        highlightedIndex = index;
                    }
                }));
                builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => SelectSuggestionAsync(option)));
                builder.AddEventStopPropagationAttribute(seq++, "onclick", true);
                if (ItemTemplate != null)
                {
                    builder.AddContent(seq++, ItemTemplate(option));
                }
                else
                {
                    builder.AddContent(seq++, option.Label ?? option.Value);
                }
                builder.CloseElement();
            }
        }

        private bool IsDropDownOpen => dropDownOption != null && dropDownOption.IsShow;

        /// <summary>
        /// 当前正在展示的建议项（已按最近一次查询完成本地过滤）。
        /// 与 <see cref="Suggestions"/> 的区别：后者是外部传入的全量数据源，本属性是实际渲染的快照。
        /// </summary>
        public IReadOnlyList<AutocompleteOption> CurrentSuggestions => currentSuggestions;

        /// <summary>
        /// 是否处于**内部**加载态（由 <see cref="FetchSuggestionsAsync"/> 尚未返回导致）；
        /// 外部通过 <see cref="Loading"/> 手动指定的加载态不计入此值。
        /// </summary>
        public bool IsInternalLoading => loading;

        private bool IsAutocompleteDisabled => effectiveDisabled;

        private InputSize EffectiveSize => effectiveSize;

        bool ISelectDropDownContext.Loading => !HideLoading && (Loading || loading);

        string ISelectDropDownContext.LoadingText => LoadingText;

        string ISelectDropDownContext.EmptyText => NoDataText;

        bool ISelectDropDownContext.ShouldShowEmpty => !Loading && !loading && !currentSuggestions.Any();

        bool ISelectDropDownContext.ShouldShowNoMatch => false;

        Task ISelectDropDownContext.OnEndReachedAsync()
        {
            return Task.CompletedTask;
        }

        private static string GetSizeCssValue(InputSize size) => size switch
        {
            InputSize.Large => "large",
            InputSize.Small => "small",
            _ => null
        };
    }
}
