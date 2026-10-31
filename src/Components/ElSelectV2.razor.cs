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
    public partial class ElSelectV2<TValue> : ElementFieldComponentBase<TValue>
    {
        private readonly string dropdownId = $"el-select-v2-dropdown-{Guid.NewGuid():N}";
        private readonly List<SelectV2Option> selectedOptions = new List<SelectV2Option>();
        private HtmlPropertyBuilder wrapperClsBuilder;
        private bool effectiveDisabled;
        private InputSize effectiveSize = InputSize.Normal;
        private bool dropdownVisible;
        private string filterText;
        private string activeOptionId;

        /// <summary>ElSelectV2 根节点引用，供 dom.js 挂载键盘导航拦截（见 OnAfterRenderAsync）。</summary>
        protected ElementReference RootElement;

        private DotNetObjectReference<ElSelectV2<TValue>> outsideRef;
        private bool outsideBound;

        [Parameter]
        public TValue Value { get; set; }

        [Parameter]
        public TValue ModelValue
        {
            get => Value;
            set => Value = value;
        }

        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }

        [Parameter]
        public EventCallback<TValue> ModelValueChanged { get; set; }

        [Parameter]
        public IEnumerable<SelectV2Option> Options { get; set; } = Enumerable.Empty<SelectV2Option>();

        /// <summary>占位文案。留空时取词条 <c>el.select.placeholder</c>，再回落「请选择」。</summary>
        [Parameter]
        public string Placeholder { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool IsDisabled
        {
            get => Disabled;
            set => Disabled = value;
        }

        [Parameter]
        public bool Clearable { get; set; } = true;

        [Parameter]
        public bool IsClearable
        {
            get => Clearable;
            set => Clearable = value;
        }

        [Parameter]
        public bool Filterable { get; set; }

        [Parameter]
        public bool Multiple { get; set; }

        [Parameter]
        public ICollection<TValue> Values { get; set; }

        [Parameter]
        public EventCallback<ICollection<TValue>> ValuesChanged { get; set; }

        [Parameter]
        public bool CollapseTags { get; set; }

        [Parameter]
        public int MaxCollapseTags { get; set; } = 1;

        [Parameter]
        public bool Remote { get; set; }

        [Parameter]
        public EventCallback<string> RemoteMethod { get; set; }

        [Parameter]
        public EventCallback<string> OnFilter { get; set; }

        [Parameter]
        public bool Loading { get; set; }

        /// <summary>加载中提示。留空时取词条 <c>el.select.loading</c>，再回落「加载中」。</summary>
        [Parameter]
        public string LoadingText { get; set; }

        /// <summary>无数据提示。留空时取词条 <c>el.select.noData</c>，再回落「无数据」。</summary>
        [Parameter]
        public string NoDataText { get; set; }

        /// <summary>过滤后无匹配提示。留空时取词条 <c>el.select.noMatch</c>，再回落「无匹配数据」。</summary>
        [Parameter]
        public string NoMatchText { get; set; }

        [Parameter]
        public InputSize Size { get; set; } = InputSize.Normal;

        [Parameter]
        public int ItemHeight { get; set; } = 34;

        [Parameter]
        public int Height { get; set; } = 274;

        [Parameter]
        public int OverscanCount { get; set; } = 3;

        [Parameter]
        public bool ValidateEvent { get; set; } = true;

        [Parameter]
        public EventCallback<TValue> OnChange { get; set; }

        [Parameter]
        public EventCallback<ElementChangeEventArgs<SelectV2Option>> OnChanging { get; set; }

        [Parameter]
        public EventCallback<ElementChangeEventArgs<SelectV2Option>> OnSelectedOptionChange { get; set; }

        /// <summary>多选 tag 类型 (success/info/warning/danger)</summary>
        [Parameter]
        public string TagType { get; set; } = "info";

        /// <summary>多选 tag 效果 (light/dark/plain)</summary>
        [Parameter]
        public string TagEffect { get; set; } = "light";

        /// <summary>CollapseTags 时显示 tooltip</summary>
        [Parameter]
        public bool CollapseTagsTooltip { get; set; }

        /// <summary>aria-label</summary>
        [Parameter]
        public string AriaLabel { get; set; }

        /// <summary>选中后清空搜索关键字</summary>
        [Parameter]
        public bool ClearAfterSelect { get; set; }

        /// <summary>回车默认选中第一项</summary>
        [Parameter]
        public bool DefaultFirstOption { get; set; }

        /// <summary>允许 Filterable 创建新项</summary>
        [Parameter]
        public bool AllowCreate { get; set; }

        [Parameter]
        public EventCallback<string> OnCreateOption { get; set; }

        /// <summary>DropDown 的自定义 class</summary>
        [Parameter]
        public string PopperClass { get; set; }

        /// <summary>DropDown 的自定义 style</summary>
        [Parameter]
        public string PopperStyle { get; set; }

        /// <summary>
        /// 下拉面板顶部固定区域（对齐 EP 2.5.2+ 的 <c>header</c> 插槽）。
        /// 官方的 el-select-v2 同样没有「面板内搜索框」属性 —— 搜索就是 <see cref="Filterable"/>
        /// 让触发器里的输入框可编辑；要在面板里再放独立搜索框 / 工具条，官方做法即本插槽。
        /// </summary>
        [Parameter]
        public RenderFragment HeaderContent { get; set; }

        /// <summary>下拉面板底部固定区域（对齐 EP 2.5.2+ 的 <c>footer</c> 插槽）。</summary>
        [Parameter]
        public RenderFragment FooterContent { get; set; }

        /// <summary>空事件处理器：仅用于给插槽容器挂 <c>@onclick:stopPropagation</c>（该指令必须与 @onclick 同时出现）。</summary>
        private void NoOp()
        {
        }

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
            var sizeCssValue = GetSizeCssValue(effectiveSize);
            wrapperClsBuilder = HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-select", "el-select-v2", Cls)
                .AddIf(effectiveDisabled, "is-disabled")
                .AddIf(Filterable, "is-filterable")
                .AddIf(Clearable, "is-clearable")
                .AddIf(Multiple, "is-multiple")
                .AddIf(dropdownVisible, "is-focus")
                .AddIf(sizeCssValue != null, $"el-select--{sizeCssValue}");

            if (FormItem != null && !FormItem.OriginValueHasRendered)
            {
                FormItem.OriginValueHasRendered = true;
                if (FormItem.Form?.Values.Any() ?? false)
                {
                    Value = FormItem.OriginValue == null
                        ? default
                        : (TValue)TypeHelper.ChangeType(FormItem.OriginValue, typeof(TValue));
                }
                SetFieldValue(Value, false);
            }

            SyncSelectedOptions();
            EnsureActiveOption();
        }

        /// <summary>下拉键盘导航时，『按上下键页面跟着动』的根因是原生方向键的默认行为会滚动
        /// 最近的可滚动祖先。Blazor 侧没法按键动态 preventDefault（@onkeydown:preventDefault 是
        /// 渲染期静态值，一旦开启连 Tab 都会被吞掉），所以交给 dom.js 按 key 精确拦截。
        /// 与 ElSelect 用同一个助手，行为保持一致。</summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("elementSelectLockNavScroll", RootElement);
                }
                catch (Exception)
                {
                    // 宿主页面未引用 dom.js 时忽略：只影响键盘导航时页面是否跟着滚动
                }
            }

            // EP useSelect.mjs#handleClickOutside：点控件之外 → expanded=false。
            // 本组件的下拉内联在根节点里（不像 ElSelect 走全局弹层宿主），原先没有任何机制
            // 会替我们收起面板 —— 面板能开不能关，就是「下拉框不关闭」的真身。
            // 用与 ElDropDownTableGrow / ElTimePicker 同一个 document 捕获阶段 mousedown 助手：
            // 目标落在根节点内（含面板本身）会被放过，所以点选项照常选中。
            if (dropdownVisible)
            {
                if (!outsideBound && RootElement.Context != null)
                {
                    outsideRef ??= DotNetObjectReference.Create(this);
                    await JSRuntime.InvokeVoidAsync(
                        "elementPickerOutsideAttach",
                        RootElement,
                        ".el-select-v2__popper",
                        outsideRef,
                        nameof(CloseFromOutsideAsync));
                    outsideBound = true;
                }
            }
            else if (outsideBound)
            {
                outsideBound = false;
                await JSRuntime.InvokeVoidAsync("elementPickerOutsideDetach", RootElement);
            }
        }

        /// <summary>document 捕获阶段收到「控件之外」的 mousedown：收起面板（由 JS 回调）。</summary>
        [JSInvokable]
        public Task CloseFromOutsideAsync()
        {
            if (!dropdownVisible)
            {
                return Task.CompletedTask;
            }
            dropdownVisible = false;
            if (OnVisibleChange.HasDelegate) _ = OnVisibleChange.InvokeAsync(false);
            StateHasChanged();
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            if (outsideBound)
            {
                _ = JSRuntime.InvokeVoidAsync("elementPickerOutsideDetach", RootElement);
                outsideBound = false;
            }
            outsideRef?.Dispose();
            outsideRef = null;
            base.Dispose();
        }

        protected override void FormItem_OnReset(object value, bool requireRerender)
        {
            Value = value == null ? default : (TValue)TypeHelper.ChangeType(value, typeof(TValue));
            filterText = null;
            dropdownVisible = false;
            activeOptionId = null;
            if (OnVisibleChange.HasDelegate) _ = OnVisibleChange.InvokeAsync(false);
            if (Multiple && Values == null)
            {
                // 非受控多选的真相在 selectedOptions 里（见 SyncSelectedOptions 注释），
                // 重置时 Values 仍是 null，SyncSelectedOptions 会原样保留 —— 所以这里显式清。
                selectedOptions.Clear();
            }
            SyncSelectedOptions();
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

        private void OpenDropdown()
        {
            if (effectiveDisabled)
            {
                return;
            }
            dropdownVisible = true;
            EnsureActiveOption();
            if (OnVisibleChange.HasDelegate)
            {
                _ = OnVisibleChange.InvokeAsync(true);
            }
        }

        private Task OnInputFocusAsync(FocusEventArgs _)
        {
            if (OnFocus.HasDelegate) return OnFocus.InvokeAsync(_);
            return Task.CompletedTask;
        }

        private Task OnInputBlurAsync(FocusEventArgs _)
        {
            if (OnBlur.HasDelegate) return OnBlur.InvokeAsync(_);
            return Task.CompletedTask;
        }

        private async Task OnFilterInputAsync(string value)
        {
            filterText = value;
            dropdownVisible = true;
            EnsureActiveOption();
            if (Remote && RemoteMethod.HasDelegate)
            {
                await RemoteMethod.InvokeAsync(value);
            }
            if (OnFilter.HasDelegate)
            {
                await OnFilter.InvokeAsync(value);
            }
        }

        private void OpenDropdownFromKeyboard(KeyboardEventArgs e)
        {
            if (!effectiveDisabled)
            {
                dropdownVisible = true;
                EnsureActiveOption();
            }
        }

        private async Task OnRootKeyDownAsync(KeyboardEventArgs e)
        {
            if (effectiveDisabled)
            {
                return;
            }

            switch (e.Key)
            {
                case "ArrowDown":
                    dropdownVisible = true;
                    MoveActiveOption(1);
                    break;
                case "ArrowUp":
                    dropdownVisible = true;
                    MoveActiveOption(-1);
                    break;
                case "Home":
                    dropdownVisible = true;
                    activeOptionId = SelectableRows.FirstOrDefault()?.Id;
                    break;
                case "End":
                    dropdownVisible = true;
                    activeOptionId = SelectableRows.LastOrDefault()?.Id;
                    break;
                case "Enter":
                    if (!dropdownVisible)
                    {
                        dropdownVisible = true;
                        EnsureActiveOption();
                        return;
                    }
                    if (DefaultFirstOption)
                    {
                        var firstOption = SelectableRows.FirstOrDefault()?.Option;
                        if (firstOption != null)
                        {
                            await SelectOptionAsync(firstOption);
                        }
                    }
                    else
                    {
                        await SelectActiveOptionAsync();
                    }
                    break;
                case "Escape":
                    dropdownVisible = false;
                    break;
                case "Backspace":
                    if (Multiple && SelectedOptions.Any() && string.IsNullOrWhiteSpace(filterText))
                    {
                        await RemoveTagAsync(SelectedOptions.Last());
                    }
                    break;
            }
        }

        private async Task SelectActiveOptionAsync()
        {
            var option = SelectableRows.FirstOrDefault(x => x.Id == activeOptionId)?.Option;
            if (option != null)
            {
                await SelectOptionAsync(option);
            }
        }

        private async Task SelectOptionAsync(SelectV2Option option)
        {
            if (option == null || option.Disabled || effectiveDisabled)
            {
                return;
            }

            // AllowCreate：「创建新项」哨兵行——不按普通选项选中，而是把输入文本交给 OnCreateOption，
            // 随后清空搜索框（让调用方通过回调加入的新项立即可见），并保持下拉展开。
            if (option.Value is string createValue && createValue.StartsWith(CreateOptionSentinel))
            {
                var created = createValue.Substring(CreateOptionSentinel.Length);
                if (OnCreateOption.HasDelegate)
                {
                    await OnCreateOption.InvokeAsync(created);
                }
                filterText = null;
                EnsureActiveOption();
                return;
            }

            var args = new ElementChangeEventArgs<SelectV2Option>
            {
                OldValue = SelectedOption,
                NewValue = option
            };
            if (OnChanging.HasDelegate)
            {
                await OnChanging.InvokeAsync(args);
                if (args.DisallowChange)
                {
                    return;
                }
            }

            if (Multiple)
            {
                await ToggleMultipleOptionAsync(option);
            }
            else
            {
                Value = ConvertOptionValue(option.Value);
                filterText = null;
                dropdownVisible = false;
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

            if (OnSelectedOptionChange.HasDelegate)
            {
                await OnSelectedOptionChange.InvokeAsync(args);
            }

            EnsureActiveOption();
        }

        private async Task ToggleMultipleOptionAsync(SelectV2Option option)
        {
            var existing = selectedOptions.FirstOrDefault(x => TypeHelper.Equal(ConvertOptionValue(x.Value), ConvertOptionValue(option.Value)));
            if (existing == null)
            {
                selectedOptions.Add(option);
            }
            else
            {
                selectedOptions.Remove(existing);
            }

            var values = selectedOptions.Select(x => ConvertOptionValue(x.Value)).ToList();
            Values = values;
            Value = values.LastOrDefault();
            // ClearAfterSelect：多选模式下选中后清空搜索关键字（单选分支已在 SelectOptionAsync 内清空）。
            if (ClearAfterSelect)
            {
                filterText = null;
            }
            SetFieldValue(Value, ValidateEvent);
            if (ValuesChanged.HasDelegate)
            {
                await ValuesChanged.InvokeAsync(Values);
            }
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

        private async Task RemoveTagAsync(SelectV2Option option)
        {
            if (option == null || effectiveDisabled)
            {
                return;
            }

            selectedOptions.RemoveAll(x => TypeHelper.Equal(ConvertOptionValue(x.Value), ConvertOptionValue(option.Value)));
            var values = selectedOptions.Select(x => ConvertOptionValue(x.Value)).ToList();
            Values = values;
            Value = values.LastOrDefault();
            SetFieldValue(Value, ValidateEvent);
            if (ValuesChanged.HasDelegate)
            {
                await ValuesChanged.InvokeAsync(Values);
            }
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(Value);
            }
        }

        private async Task ClearAsync(MouseEventArgs e)
        {
            if (!Clearable || effectiveDisabled)
            {
                return;
            }
            Value = default;
            filterText = null;
            dropdownVisible = false;
            activeOptionId = null;
            if (OnVisibleChange.HasDelegate) _ = OnVisibleChange.InvokeAsync(false);
            selectedOptions.Clear();
            if (Multiple)
            {
                Values = Array.Empty<TValue>();
                if (ValuesChanged.HasDelegate)
                {
                    await ValuesChanged.InvokeAsync(Values);
                }
            }
            SetFieldValue(Value, ValidateEvent);
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(Value);
            }
        }

        private TValue ConvertOptionValue(object value)
        {
            if (value == null)
            {
                return default;
            }
            if (value is TValue typedValue)
            {
                return typedValue;
            }
            return (TValue)TypeHelper.ChangeType(value, typeof(TValue));
        }

        private void SyncSelectedOptions()
        {
            if (Multiple)
            {
                var values = Values ?? ExtractValues(Value);
                if (Values == null && !values.Any())
                {
                    // 非受控多选（消费者只绑 Value，或根本不绑）：Values 恒为 null，
                    // 此时 selectedOptions 是本组件唯一的真相，必须原样保留。
                    // 早先这里是无条件 Clear() 再从 Values/Value 重算，而 Value 只有一个标量，
                    // 于是每选一项 → ValueChanged → 父组件重渲染 → OnParametersSet → 刚选的标签
                    // 全被抹掉，表现即「多选点了不显示选中的值」。
                    // 绑了 Values 且它是空集合（受控清空）才走下面的重建分支把标签清掉。
                    return;
                }

                selectedOptions.Clear();
                foreach (var value in values)
                {
                    var option = FlatOptions.FirstOrDefault(x => TypeHelper.Equal(ConvertOptionValue(x.Value), value));
                    if (option != null)
                    {
                        selectedOptions.Add(option);
                    }
                }
                return;
            }

            selectedOptions.Clear();
            var selected = FlatOptions.FirstOrDefault(x => TypeHelper.Equal(ConvertOptionValue(x.Value), Value));
            if (selected != null)
            {
                selectedOptions.Add(selected);
            }
        }

        private IEnumerable<TValue> ExtractValues(object value)
        {
            if (value is IEnumerable<TValue> typed && value is not string)
            {
                return typed;
            }
            return Enumerable.Empty<TValue>();
        }

        private void EnsureActiveOption()
        {
            if (!SelectableRows.Any())
            {
                activeOptionId = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(activeOptionId) || SelectableRows.All(x => x.Id != activeOptionId))
            {
                var selected = SelectableRows.FirstOrDefault(x => IsSelected(x.Option));
                activeOptionId = (selected ?? SelectableRows.First()).Id;
            }
        }

        private void MoveActiveOption(int step)
        {
            var rows = SelectableRows.ToList();
            if (!rows.Any())
            {
                activeOptionId = null;
                return;
            }

            var index = rows.FindIndex(x => x.Id == activeOptionId);
            if (index < 0)
            {
                index = 0;
            }
            else
            {
                index = (index + step + rows.Count) % rows.Count;
            }
            activeOptionId = rows[index].Id;
        }

        private bool IsSelected(SelectV2Option option)
        {
            if (option == null)
            {
                return false;
            }
            return Multiple
                ? selectedOptions.Any(x => TypeHelper.Equal(ConvertOptionValue(x.Value), ConvertOptionValue(option.Value)))
                : TypeHelper.Equal(ConvertOptionValue(option.Value), Value);
        }

        private IEnumerable<SelectV2Option> FilteredOptions
        {
            get
            {
                var options = Options ?? Enumerable.Empty<SelectV2Option>();
                if (!Filterable || Remote || string.IsNullOrWhiteSpace(filterText))
                {
                    return options.Where(x => x != null).ToList();
                }
                return options.Where(x => x != null && (x.Label ?? string.Empty).IndexOf(filterText, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
            }
        }

        private List<SelectV2Row> VirtualRows
        {
            get
            {
                var rows = new List<SelectV2Row>();
                string currentGroup = null;
                foreach (var option in FilteredOptions)
                {
                    if (!string.IsNullOrWhiteSpace(option.Group) && !string.Equals(currentGroup, option.Group, StringComparison.Ordinal))
                    {
                        currentGroup = option.Group;
                        rows.Add(new SelectV2Row
                        {
                            Id = $"{dropdownId}-group-{rows.Count}",
                            Group = option.Group,
                            IsGroup = true
                        });
                    }

                    rows.Add(new SelectV2Row
                    {
                        Id = GetOptionId(option),
                        Option = option,
                        Group = option.Group,
                        IsGroup = false
                    });
                }

                if (ShowCreateOption)
                {
                    var createOption = new SelectV2Option
                    {
                        Label = $"创建 \"{filterText}\"",
                        Value = CreateOptionSentinel + filterText
                    };
                    rows.Insert(0, new SelectV2Row
                    {
                        Id = $"{dropdownId}-create",
                        Option = createOption,
                        IsCreate = true,
                        IsGroup = false
                    });
                }
                return rows;
            }
        }

        /// <summary>
        /// 是否展示「创建新项」行：Filterable + AllowCreate + 非远程 + 已有输入，
        /// 且当前输入与任意现有选项标签都不相同（EP 行为）。
        /// </summary>
        private bool ShowCreateOption =>
            Filterable && AllowCreate && !Remote && !string.IsNullOrWhiteSpace(filterText)
            && !FlatOptions.Any(o => string.Equals(o.Label, filterText, StringComparison.CurrentCultureIgnoreCase));

        private IEnumerable<SelectV2Row> SelectableRows => VirtualRows.Where(x => !x.IsGroup && x.Option != null && !x.Option.Disabled);

        private IEnumerable<SelectV2Option> FlatOptions => (Options ?? Enumerable.Empty<SelectV2Option>()).Where(x => x != null);

        private SelectV2Option SelectedOption => FlatOptions.FirstOrDefault(IsSelected);

        private string DisplayLabel => Multiple
            ? (Filterable && dropdownVisible ? filterText : string.Empty)
            : (Filterable && dropdownVisible ? filterText : SelectedOption?.Label);

        private string EffectivePlaceholder => Multiple && SelectedOptions.Any()
            ? string.Empty
            : (string.IsNullOrEmpty(DisplayLabel) ? Lang.Text("el.select.placeholder", Placeholder, DefaultPlaceholder) : null);

        private bool ShowClear => Clearable && !effectiveDisabled && (Multiple ? SelectedOptions.Any() : SelectedOption != null);

        private bool IsSelectV2Disabled => effectiveDisabled;

        private bool IsLoading => Loading;

        // 「显式参数 > 词条 > 改动前字面量」，键名对齐 EP useSelect.ts（SelectV2 与 Select 同一族文案）
        private const string DefaultPlaceholder = "请选择";
        private const string DefaultLoadingText = "加载中";
        private const string DefaultNoDataText = "无数据";
        private const string DefaultNoMatchText = "无匹配数据";

        private string EffectiveLoadingText => Lang.Text("el.select.loading", LoadingText, DefaultLoadingText);

        private string EmptyText => Filterable && !Remote && !string.IsNullOrWhiteSpace(filterText) && !FilteredOptions.Any()
            ? Lang.Text("el.select.noMatch", NoMatchText, DefaultNoMatchText)
            : Lang.Text("el.select.noData", NoDataText, DefaultNoDataText);

        private int EffectiveItemHeight => Math.Max(24, ItemHeight);

        private int EffectiveHeight => Math.Max(EffectiveItemHeight * 2, Height);

        private int EffectiveOverscanCount => Math.Max(0, OverscanCount);

        private IReadOnlyList<SelectV2Option> SelectedOptions => selectedOptions;

        private IEnumerable<SelectV2Option> VisibleSelectedOptions => CollapseTags
            ? selectedOptions.Take(Math.Max(1, MaxCollapseTags))
            : selectedOptions;

        private int CollapsedTagCount => CollapseTags
            ? Math.Max(0, selectedOptions.Count - Math.Max(1, MaxCollapseTags))
            : 0;

        private string ActiveDescendantId => dropdownVisible ? activeOptionId : null;

        private string GetOptionId(SelectV2Option option)
        {
            var value = option?.Value == null ? "null" : Convert.ToString(option.Value, CultureInfo.InvariantCulture)?.Replace(' ', '-');
            return $"{dropdownId}-option-{value}";
        }

        private static string GetSizeCssValue(InputSize size) => size switch
        {
            InputSize.Large => "large",
            InputSize.Small => "small",
            _ => null
        };

        private sealed class SelectV2Row
        {
            public string Id { get; set; }

            public bool IsGroup { get; set; }

            public string Group { get; set; }

            public SelectV2Option Option { get; set; }

            public bool IsCreate { get; set; }
        }

        /// <summary>AllowCreate 创建项的哨兵前缀：选中该 Value 时走「创建新项」分支而非普通选中。</summary>
        private const string CreateOptionSentinel = "__EL_SELECT_V2_CREATE__::";
    }
}
