using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElCascader : ElementFieldComponentBase<IList<string>>, ISelectDropDownContext
    {
        private static long dropDownIdSeed;
        private readonly string DropDownId = $"el-cascader-dropdown-{Interlocked.Increment(ref dropDownIdSeed)}";
        private readonly List<IList<CascaderOption>> menus = new List<IList<CascaderOption>>();
        private readonly List<CascaderOption> activePath = new List<CascaderOption>();
        private CascaderOption hoveredOption;
        private readonly List<IList<string>> checkedValues = new List<IList<string>>();
        private readonly List<CascaderSuggestion> suggestions = new List<CascaderSuggestion>();
        private DropDownOption dropDownOption;
        private ElementReference cascaderElement;
        private HtmlPropertyBuilder wrapperClsBuilder;
        private string filterText;
        private InputSize effectiveSize = InputSize.Normal;
        private bool effectiveDisabled;
        private CancellationTokenSource _filterCts;

        /// <summary>键盘光标所在列（menus 的下标）。-1 表示尚未初始化。</summary>
        private int keyboardMenuIndex = -1;

        /// <summary>键盘光标在当前列中的行号。</summary>
        private int keyboardRowIndex = -1;

        /// <summary>筛选模式下（Filterable 且有 filterText）键盘光标在建议列表中的下标。</summary>
        private int keyboardSuggestionIndex = -1;

        [Inject]
        internal PopupService PopupService { get; set; }

        [Parameter]
        public IList<string> Value { get; set; } = new List<string>();

        [Parameter]
        public IList<string> ModelValue
        {
            get => Value;
            set => Value = value;
        }

        [Parameter]
        public EventCallback<IList<string>> ValueChanged { get; set; }

        [Parameter]
        public EventCallback<IList<string>> ModelValueChanged { get; set; }

        [Parameter]
        public IList<IList<string>> Values { get; set; } = new List<IList<string>>();

        [Parameter]
        public IList<IList<string>> ModelValues
        {
            get => Values;
            set => Values = value;
        }

        [Parameter]
        public EventCallback<IList<IList<string>>> ValuesChanged { get; set; }

        [Parameter]
        public EventCallback<IList<IList<string>>> ModelValuesChanged { get; set; }

        [Parameter]
        public IEnumerable<CascaderOption> Options { get; set; }

        /// <summary>占位文案。留空时取词条 <c>el.cascader.placeholder</c>，再回落「请选择」（与 EP 同键同默认）。</summary>
        [Parameter]
        public string Placeholder { get; set; }

        private string EffectivePlaceholder => Lang.Text("el.cascader.placeholder", Placeholder, "请选择");

        [Parameter]
        public string Separator { get; set; } = " / ";

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
        public InputSize Size { get; set; } = InputSize.Normal;

        [Parameter]
        public bool CheckStrictly { get; set; }

        [Parameter]
        public bool Multiple { get; set; }

        [Parameter]
        public bool Lazy { get; set; }

        [Parameter]
        public Func<CascaderOption, Task<IEnumerable<CascaderOption>>> LazyLoad { get; set; }

        [Parameter]
        public bool Filterable { get; set; }

        [Parameter]
        public Func<CascaderOption, string, bool> FilterMethod { get; set; }

        [Parameter]
        public bool ExpandTriggerHover { get; set; }

        [Parameter]
        public RenderFragment<CascaderOption> NodeTemplate { get; set; }

        [Parameter]
        public RenderFragment<CascaderSuggestion> SuggestionItemTemplate { get; set; }

        [Parameter]
        public string PrefixIcon { get; set; }

        [Parameter]
        public string PopperClass { get; set; }

        [Parameter]
        public string PopperStyle { get; set; }

        [Parameter]
        public int PopperMaxHeight { get; set; } = 204;

        [Parameter]
        public bool FitInputWidth { get; set; }

        [Parameter]
        public bool ValidateEvent { get; set; } = true;

        // ===== Element Plus 对齐新增属性 =====

        /// <summary>是否显示完整路径（false 只显示最后一级）</summary>
        [Parameter]
        public bool ShowAllLevels { get; set; } = true;

        /// <summary>是否高亮当前悬停项</summary>
        [Parameter]
        public bool HighlightCurrentItem { get; set; } = false;

        /// <summary>是否折叠多选标签</summary>
        [Parameter]
        public bool CollapseTags { get; set; } = false;

        /// <summary>折叠标签最大显示数</summary>
        [Parameter]
        public int MaxCollapseTags { get; set; } = 1;

        /// <summary>鼠标悬停折叠文字时是否显示完整路径 tooltip</summary>
        [Parameter]
        public bool CollapseTagsTooltip { get; set; } = false;

        /// <summary>自定义选项 value 属性名（默认 "value"）</summary>
        [Parameter]
        public string ValueKey { get; set; } = "value";

        /// <summary>远程搜索防抖延时（毫秒）</summary>
        [Parameter]
        public int Debounce { get; set; } = 300;

        [Parameter]
        public EventCallback<IList<string>> OnChange { get; set; }

        public EventCallback OnClear { get; set; }

        [Parameter]
        public EventCallback<IList<IList<string>>> OnValuesChange { get; set; }

        [Parameter]
        public EventCallback<CascaderOption> OnExpandChange { get; set; }

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
                .Add("el-cascader", Cls)
                .AddIf(effectiveDisabled, "is-disabled")
                .AddIf(IsDropDownOpen, "is-focus")
                .AddIf(sizeCssValue != null, $"el-cascader--{sizeCssValue}");

            if (FormItem != null && !FormItem.OriginValueHasRendered)
            {
                FormItem.OriginValueHasRendered = true;
                if (FormItem.Form?.Values.Any() ?? false)
                {
                    Value = NormalizeValue(FormItem.OriginValue);
                }
                SetFieldValue(Value, false);
            }

            SyncPathFromValue();
            SyncCheckedValuesFromValues();
        }

        protected override void FormItem_OnReset(object value, bool requireRerender)
        {
            Value = NormalizeValue(value);
            SyncPathFromValue();
            SyncCheckedValuesFromValues();
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

        private async Task OnCascaderClickAsync(MouseEventArgs e)
        {
            if (effectiveDisabled)
            {
                return;
            }

            if (IsDropDownOpen)
            {
                await CloseDropDownAsync();
                return;
            }

            filterText = null;
            suggestions.Clear();
            OpenMenus();
            await OpenDropDownAsync();
        }

        private async Task OnInputClearAsync(MouseEventArgs e)
        {
            Value = new List<string>();
            Values = new List<IList<string>>();
            activePath.Clear();
            checkedValues.Clear();
            filterText = null;
            suggestions.Clear();
            OpenMenus();
            SetFieldValue(Value, ValidateEvent);
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(Value);
            }
            if (ValuesChanged.HasDelegate)
            {
                await ValuesChanged.InvokeAsync(Values);
            }
            if (ModelValuesChanged.HasDelegate)
            {
                await ModelValuesChanged.InvokeAsync(Values);
            }
            if (OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(Value);
            }
            if (OnValuesChange.HasDelegate)
            {
                await OnValuesChange.InvokeAsync(Values);
            }
            if (OnClear.HasDelegate)
            {
                await OnClear.InvokeAsync();
            }
            await CloseDropDownAsync();
        }

        private async Task OnInputFocusAsync(FocusEventArgs e)
        {
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

        private ElInput<string> Input { get; set; }

        /// <summary>对齐 EP exposes：使组件获得焦点（委托内层 ElInput）。</summary>
        public ValueTask FocusAsync() => Input?.FocusAsync() ?? ValueTask.CompletedTask;

        /// <summary>对齐 EP exposes：使组件失去焦点（委托内层 ElInput）。</summary>
        public ValueTask BlurAsync() => Input?.BlurAsync() ?? ValueTask.CompletedTask;

        /// <summary>对齐 EP exposes：清空已选值（同清空按钮逻辑）。</summary>
        public async Task ClearAsync()
        {
            Value = new List<string>();
            Values = new List<IList<string>>();
            activePath.Clear();
            checkedValues.Clear();
            filterText = null;
            suggestions.Clear();
            OpenMenus();
            SetFieldValue(Value, ValidateEvent);
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(Value);
            }
            if (ValuesChanged.HasDelegate)
            {
                await ValuesChanged.InvokeAsync(Values);
            }
            if (ModelValuesChanged.HasDelegate)
            {
                await ModelValuesChanged.InvokeAsync(Values);
            }
            if (OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(Value);
            }
            if (OnValuesChange.HasDelegate)
            {
                await OnValuesChange.InvokeAsync(Values);
            }
            if (OnClear.HasDelegate)
            {
                await OnClear.InvokeAsync();
            }
            await CloseDropDownAsync();
        }

        /// <summary>
        /// 焦点一直停在内层 ElInput 上，↑↓ 的默认行为会滚动最近的可滚动祖先（就是页面），
        /// 表现为「按下键整页跟着动」。与 ElSelect / ElSelectV2 保持一致：交给 dom.js 按 key
        /// 精确拦截（Blazor 侧 @onkeydown:preventDefault 是渲染期静态值，开启会连 Tab 一起吞掉）。
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("elementSelectLockNavScroll", cascaderElement);
                }
                catch (Exception)
                {
                    // 宿主页面未引用 dom.js 时忽略：只影响键盘导航时页面是否跟着滚动
                }
            }
        }

        /// <summary>是否处于筛选建议模式（面板显示扁平建议列表，而非多列菜单）。</summary>
        private bool IsSuggestionMode => Filterable && !string.IsNullOrWhiteSpace(filterText);

        /// <summary>
        /// 键盘导航入口（对齐 Element Plus）：↑↓ 本列上下移、→ 进入子级、← 回到父级、
        /// Enter 展开 / 选中、Home / End 跳首尾、Esc 收起。↑↓ 不会滚动页面（见 elementSelectLockNavScroll）。
        /// </summary>
        private async Task OnRootKeyDownAsync(KeyboardEventArgs e)
        {
            if (effectiveDisabled)
            {
                return;
            }

            switch (e.Key)
            {
                case "ArrowDown":
                    await EnsureNavOpenAsync();
                    if (IsSuggestionMode) { MoveSuggestionCursor(1); } else { MoveRowCursor(1); }
                    break;
                case "ArrowUp":
                    await EnsureNavOpenAsync();
                    if (IsSuggestionMode) { MoveSuggestionCursor(-1); } else { MoveRowCursor(-1); }
                    break;
                case "Home":
                    await EnsureNavOpenAsync();
                    JumpRowCursor(true);
                    break;
                case "End":
                    await EnsureNavOpenAsync();
                    JumpRowCursor(false);
                    break;
                case "ArrowRight":
                    await EnsureNavOpenAsync();
                    await EnterChildMenuAsync();
                    break;
                case "ArrowLeft":
                    await EnsureNavOpenAsync();
                    MoveToParentMenu();
                    break;
                case "Enter":
                    await EnsureNavOpenAsync();
                    await ActivateKeyboardNodeAsync();
                    break;
                case "Escape":
                    await CloseDropDownAsync();
                    break;
            }
        }

        /// <summary>导航键按下时保证下拉已打开，并把光标落到某个有效节点上。</summary>
        private async Task EnsureNavOpenAsync()
        {
            if (!IsDropDownOpen)
            {
                filterText = null;
                suggestions.Clear();
                OpenMenus();
                await OpenDropDownAsync();
            }

            if (IsSuggestionMode)
            {
                if (keyboardSuggestionIndex < 0 || keyboardSuggestionIndex >= suggestions.Count)
                {
                    keyboardSuggestionIndex = suggestions.Any() ? 0 : -1;
                }

                RefreshNav();
                return;
            }

            if (menus.Count == 0)
            {
                OpenMenus();
            }

            ClampCursorColumn();
            if (keyboardMenuIndex < 0)
            {
                return;
            }

            var column = menus[keyboardMenuIndex];
            if (keyboardRowIndex >= 0 && keyboardRowIndex < column.Count)
            {
                return;
            }

            // 已选中过的值可以作为起点（↓ 从它往下走）；没有选中项则保持 -1，
            // 交给 MoveRowCursor：↓ 落到首项、↑ 落到末项，第一次按键不会跳过第一项。
            var preferred = column.FirstOrDefault(IsSelected);
            if (preferred != null)
            {
                keyboardRowIndex = column.IndexOf(preferred);
                RefreshNav();
            }
        }

        /// <summary>把光标列下标夹到 menus 合法范围内（菜单会因展开 / 收起而增删列）。</summary>
        private void ClampCursorColumn()
        {
            if (menus.Count == 0)
            {
                keyboardMenuIndex = -1;
                return;
            }

            if (keyboardMenuIndex < 0)
            {
                keyboardMenuIndex = menus.Count - 1;
            }

            if (keyboardMenuIndex > menus.Count - 1)
            {
                keyboardMenuIndex = menus.Count - 1;
            }
        }

        /// <summary>键盘光标当前所在的列。</summary>
        private IList<CascaderOption> GetCursorColumn()
        {
            if (keyboardMenuIndex < 0 || keyboardMenuIndex >= menus.Count)
            {
                return null;
            }

            return menus[keyboardMenuIndex];
        }

        /// <summary>键盘光标当前所在的节点。</summary>
        private CascaderOption GetKeyboardNode()
        {
            if (IsSuggestionMode)
            {
                return null;
            }

            var column = GetCursorColumn();
            if (column == null || keyboardRowIndex < 0 || keyboardRowIndex >= column.Count)
            {
                return null;
            }

            return column[keyboardRowIndex];
        }

        /// <summary>↑↓：在本列内上下移动，跳过禁用节点。</summary>
        private void MoveRowCursor(int step)
        {
            ClampCursorColumn();
            var column = GetCursorColumn();
            if (column == null || column.Count == 0)
            {
                return;
            }

            var index = keyboardRowIndex;
            if (index < 0)
            {
                // 还没落位：↓ 从首项开始，↑ 从末项开始
                index = step > 0 ? 0 : column.Count - 1;
                if (!column[index].Disabled)
                {
                    keyboardRowIndex = index;
                    RefreshNav();
                    return;
                }
            }

            for (var i = 0; i < column.Count; i++)
            {
                index = (index + step + column.Count) % column.Count;
                if (!column[index].Disabled)
                {
                    keyboardRowIndex = index;
                    RefreshNav();
                    return;
                }
            }
        }

        /// <summary>Home / End：跳到本列第一个 / 最后一个可用节点。</summary>
        private void JumpRowCursor(bool first)
        {
            ClampCursorColumn();
            var column = GetCursorColumn();
            if (column == null || column.Count == 0)
            {
                return;
            }

            var order = Enumerable.Range(0, column.Count);
            IEnumerable<int> candidates = first ? order : order.Reverse();
            foreach (var i in candidates)
            {
                if (!column[i].Disabled)
                {
                    keyboardRowIndex = i;
                    RefreshNav();
                    return;
                }
            }
        }

        /// <summary>→：展开当前节点并进入子列（叶子节点则原地不动）。</summary>
        private async Task EnterChildMenuAsync()
        {
            var node = GetKeyboardNode();
            if (node == null || !node.HasChildren)
            {
                return;
            }

            var parentIndex = keyboardMenuIndex;
            await ExpandOptionAsync(node, parentIndex);

            // 叶子被选中时下拉会关闭，此时 menus 已被重置，不能再挪光标
            if (!IsDropDownOpen)
            {
                return;
            }

            keyboardMenuIndex = Math.Min(parentIndex + 1, menus.Count - 1);
            keyboardRowIndex = 0;
            RefreshNav();
        }

        /// <summary>←：回到父列，光标停在刚展开的那个节点所在行。</summary>
        private void MoveToParentMenu()
        {
            ClampCursorColumn();
            if (keyboardMenuIndex <= 0)
            {
                return;
            }

            keyboardMenuIndex--;
            var column = GetCursorColumn();
            var parent = keyboardMenuIndex < activePath.Count ? activePath[keyboardMenuIndex] : null;
            var index = column != null && parent != null ? column.IndexOf(parent) : -1;
            keyboardRowIndex = index >= 0 ? index : 0;
            RefreshNav();
        }

        /// <summary>Enter：展开当前节点；若是叶子（或 CheckStrictly）则由 ExpandOptionAsync 负责选中。</summary>
        private async Task ActivateKeyboardNodeAsync()
        {
            if (IsSuggestionMode)
            {
                if (keyboardSuggestionIndex >= 0 && keyboardSuggestionIndex < suggestions.Count)
                {
                    await SelectSuggestionAsync(suggestions[keyboardSuggestionIndex]);
                }

                return;
            }

            var node = GetKeyboardNode();
            if (node == null)
            {
                return;
            }

            await ExpandOptionAsync(node, keyboardMenuIndex);
        }

        /// <summary>筛选建议列表内的 ↑↓。</summary>
        private void MoveSuggestionCursor(int step)
        {
            if (!suggestions.Any())
            {
                return;
            }

            keyboardSuggestionIndex = keyboardSuggestionIndex < 0
                ? 0
                : (keyboardSuggestionIndex + step + suggestions.Count) % suggestions.Count;
            RefreshNav();
        }

        /// <summary>
        /// 面板内容由 ElPopup 宿主渲染，不在本组件的渲染树里 —— 必须走 RequestRender 才能刷新，
        /// 只 StateHasChanged 自己是无效的（同前一天玩家 Fix 的 ElSelect / ElDatePicker 弹层规则）。
        /// </summary>
        private void RefreshNav()
        {
            RefreshDropDown();
            StateHasChanged();
        }

        /// <summary>当前节点是否是键盘光标所在项（用于给出高亮视觉）。</summary>
        private bool IsKeyboardCursor(CascaderOption option)
            => !IsSuggestionMode && ReferenceEquals(option, GetKeyboardNode());

        private async Task ExpandOptionAsync(CascaderOption option, int menuIndex)
        {
            if (option == null || option.Disabled)
            {
                return;
            }

            TrimActivePath(menuIndex);
            activePath.Add(option);
            TrimMenus(menuIndex + 1);

            if (Lazy)
            {
                await EnsureLazyLoadedAsync(option);
            }

            if (option.HasChildren)
            {
                menus.Add(option.Children);
                if (OnExpandChange.HasDelegate)
                {
                    await OnExpandChange.InvokeAsync(option);
                }
            }

            if (CheckStrictly || option.Leaf || !option.HasChildren)
            {
                if (Multiple)
                {
                    await ToggleMultipleSelectionAsync(activePath);
                }
                else
                {
                    await CommitSelectionAsync(option);
                }
            }

            RefreshDropDown();
        }

        private async Task OnInputValueChangedAsync(string value)
        {
            if (!Filterable)
            {
                return;
            }

            filterText = value;
            if (!IsDropDownOpen)
            {
                await OpenDropDownAsync();
            }

            if (Debounce <= 0)
            {
                BuildSuggestions();
                RefreshDropDown();
                return;
            }

            _filterCts?.Cancel();
            _filterCts?.Dispose();
            var cts = new CancellationTokenSource();
            _filterCts = cts;
            try
            {
                await Task.Delay(Debounce, cts.Token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (_filterCts != cts)
            {
                return;
            }

            BuildSuggestions();
            RefreshDropDown();
        }

        private async Task CommitSelectionAsync(CascaderOption option)
        {
            Value = activePath.Select(x => x.Value).Where(x => x != null).ToList();
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

            if (!option.HasChildren || !CheckStrictly)
            {
                await CloseDropDownAsync();
            }
        }

        private async Task ToggleMultipleSelectionAsync(IEnumerable<CascaderOption> path)
        {
            var values = path.Select(x => x.Value).Where(x => x != null).ToList();
            if (!values.Any())
            {
                return;
            }

            var existing = checkedValues.FindIndex(x => SamePath(x, values));
            if (existing >= 0)
            {
                checkedValues.RemoveAt(existing);
            }
            else
            {
                checkedValues.Add(values);
            }

            Values = checkedValues.Select(x => (IList<string>)x.ToList()).ToList();
            Value = Values.LastOrDefault()?.ToList() ?? new List<string>();
            SetFieldValue(Value, ValidateEvent);

            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(Value);
            }
            if (ValuesChanged.HasDelegate)
            {
                await ValuesChanged.InvokeAsync(Values);
            }
            if (ModelValuesChanged.HasDelegate)
            {
                await ModelValuesChanged.InvokeAsync(Values);
            }
            if (OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(Value);
            }
            if (OnValuesChange.HasDelegate)
            {
                await OnValuesChange.InvokeAsync(Values);
            }
        }

        private async Task SelectSuggestionAsync(CascaderSuggestion suggestion)
        {
            if (suggestion?.Path == null || !suggestion.Path.Any())
            {
                return;
            }

            activePath.Clear();
            activePath.AddRange(suggestion.Path);
            Value = suggestion.Values.ToList();
            OpenMenus();

            if (Multiple)
            {
                await ToggleMultipleSelectionAsync(suggestion.Path);
                return;
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
            if (OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(Value);
            }
            await CloseDropDownAsync();
        }

        private void OpenMenus()
        {
            menus.Clear();
            menus.Add(RootOptions);
            var currentOptions = RootOptions;
            var nextPath = new List<CascaderOption>();

            foreach (var value in Value ?? Enumerable.Empty<string>())
            {
                var node = currentOptions.FirstOrDefault(x => x.Value == value);
                if (node == null)
                {
                    break;
                }

                nextPath.Add(node);
                if (!node.HasChildren)
                {
                    break;
                }

                currentOptions = node.Children;
                menus.Add(currentOptions);
            }

            activePath.Clear();
            activePath.AddRange(nextPath);
        }

        private void SyncPathFromValue()
        {
            activePath.Clear();
            var currentOptions = RootOptions;
            foreach (var value in Value ?? Enumerable.Empty<string>())
            {
                var option = currentOptions.FirstOrDefault(x => x.Value == value);
                if (option == null)
                {
                    break;
                }

                activePath.Add(option);
                currentOptions = option.Children ?? new List<CascaderOption>();
            }
        }

        private void SyncCheckedValuesFromValues()
        {
            checkedValues.Clear();
            foreach (var value in Values ?? Enumerable.Empty<IList<string>>())
            {
                var normalized = NormalizeValue(value);
                if (normalized.Any())
                {
                    checkedValues.Add(normalized);
                }
            }
        }

        private async Task OpenDropDownAsync()
        {
            if (dropDownOption != null)
            {
                RefreshDropDown();
                return;
            }

            dropDownOption = new DropDownOption
            {
                Select = this,
                Target = cascaderElement,
                OptionContent = BuildPanelContent,
                PopperClass = string.Join(" ", new[] { "el-cascader__dropdown", PopperClass }.Where(x => !string.IsNullOrWhiteSpace(x))),
                PopperStyle = PopperStyle,
                MaxHeight = PopperMaxHeight,
                FitInputWidth = FitInputWidth,
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
                filterText = null;
                suggestions.Clear();
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
            builder.OpenElement(seq++, "div");
            builder.SetKey($"el-cascader-panel-{panelGeneration}");
            builder.AddAttribute(seq++, "class", "el-cascader-panel");
            builder.AddAttribute(seq++, "role", "menu");

            if (Filterable && !string.IsNullOrWhiteSpace(filterText))
            {
                BuildSuggestionContent(builder, ref seq);
                builder.CloseElement();
                return;
            }

            for (var menuIndex = 0; menuIndex < menus.Count; menuIndex++)
            {
                var index = menuIndex;
                builder.OpenElement(seq++, "div");
                builder.AddAttribute(seq++, "class", "el-cascader-menu");
                builder.OpenElement(seq++, "div");
                builder.AddAttribute(seq++, "class", "el-cascader-menu__wrap el-scrollbar__wrap");
                builder.OpenElement(seq++, "ul");
                builder.AddAttribute(seq++, "class", "el-cascader-menu__list");
                foreach (var option in menus[index])
                {
                    var optionClsBuilder = HtmlPropertyBuilder.CreateCssClassBuilder()
                        .Add("el-cascader-node")
                        .AddIf(option.Disabled, "is-disabled")
                        .AddIf(IsInPath(option) || IsKeyboardCursor(option), "in-active-path")
                        // in-active-path 既是「选中路径」也是「键盘光标」的视觉样式，二者会重叠；
                        // 额外打一个标记类，便于无障碍/自动化精确定位键盘光标位置。
                        .AddIf(IsKeyboardCursor(option), "el-cascader-node--key")
                        // EP node.vue:12 —— is-active 绑定 node.checked；多选勾中的叶子同样拿 is-active
                        // （此前只有活动路径末梢拿到，勾完第二项后第一项就失去蓝粗体，即用户报的字体不一致）。
                        .AddIf(IsSelected(option) || IsChecked(option), "is-active")
                        .AddIf(IsChecked(option), "is-checked")
                        // EP node.vue:15 —— in-checked-path 只在 checkStrictly 模式出现，勾中节点与其祖先都在勾选路径上
                        .AddIf(CheckStrictly && IsOnCheckedPath(option), "in-checked-path")
                        .AddIf(HighlightCurrentItem && hoveredOption == option, "is-current")
                        // EP node.vue:9 —— is-selectable 只在 checkStrictly 出现；旧版 CSS 里
                        // .is-selectable.in-active-path 会把颜色压回常规灰（特异性 0,3,0 > is-active 的 0,2,0），
                        // 叶子若带它，键盘光标所在的勾中叶子就失去蓝粗体（即用户截图里「反馈」与其他勾选项字体不一致）。
                        .AddIf(CheckStrictly, "is-selectable");
                    builder.OpenElement(seq++, "li");
                    builder.SetKey(option);
                    builder.AddAttribute(seq++, "class", optionClsBuilder.ToString());
                    builder.AddAttribute(seq++, "role", "menuitem");
                    builder.AddAttribute(seq++, "aria-disabled", option.Disabled);
                    builder.AddAttribute(seq++, "aria-expanded", option.HasChildren ? IsInPath(option) : null);
                    builder.AddAttribute(seq++, "aria-checked", Multiple ? IsChecked(option) : null);
                    builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => ExpandOptionAsync(option, index)));
                    builder.AddAttribute(seq++, "onmouseover", EventCallback.Factory.Create<MouseEventArgs>(this, () =>
                    {
                        if (HighlightCurrentItem && hoveredOption != option)
                        {
                            hoveredOption = option;
                            RefreshDropDown();
                        }
                        if (ExpandTriggerHover)
                        {
                            _ = ExpandOptionAsync(option, index);
                        }
                    }));
                    builder.AddAttribute(seq++, "onmouseleave", EventCallback.Factory.Create<MouseEventArgs>(this, () =>
                    {
                        if (HighlightCurrentItem && hoveredOption != null)
                        {
                            hoveredOption = null;
                            RefreshDropDown();
                        }
                    }));
                    if (Multiple)
                    {
                        builder.OpenElement(seq++, "span");
                        builder.AddAttribute(seq++, "class", "el-checkbox__input" + (IsChecked(option) ? " is-checked" : string.Empty));
                        builder.OpenElement(seq++, "span");
                        builder.AddAttribute(seq++, "class", "el-checkbox__inner");
                        builder.CloseElement();
                        builder.CloseElement();
                    }
                    builder.OpenElement(seq++, "span");
                    builder.AddAttribute(seq++, "class", "el-cascader-node__label");
                    if (NodeTemplate != null)
                    {
                        builder.AddContent(seq++, NodeTemplate(option));
                    }
                    else
                    {
                        builder.AddContent(seq++, option.DisplayLabel);
                    }
                    builder.CloseElement();
                    if (option.Loading)
                    {
                        builder.OpenElement(seq++, "i");
                        builder.AddAttribute(seq++, "class", "el-icon-loading el-cascader-node__postfix");
                        builder.CloseElement();
                    }
                    else if (option.HasChildren || (Lazy && !option.Leaf))
                    {
                        builder.OpenElement(seq++, "i");
                        builder.AddAttribute(seq++, "class", "el-icon-arrow-right el-cascader-node__postfix");
                        builder.CloseElement();
                    }
                    else if (IsSelected(option))
                    {
                        builder.OpenElement(seq++, "i");
                        builder.AddAttribute(seq++, "class", "el-icon-check el-cascader-node__postfix");
                        builder.CloseElement();
                    }
                    builder.CloseElement();
                }
                builder.CloseElement();
                builder.CloseElement();
                builder.CloseElement();
            }
            builder.CloseElement();
        }

        private void BuildSuggestionContent(RenderTreeBuilder builder, ref int seq)
        {
            if (!suggestions.Any())
            {
                builder.OpenElement(seq++, "p");
                builder.AddAttribute(seq++, "class", "el-cascader__empty-text");
                builder.AddContent(seq++, Lang.T("el.cascader.noMatch", "无匹配数据"));
                builder.CloseElement();
                return;
            }

            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "el-cascader-menu");
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "el-cascader-menu__wrap el-scrollbar__wrap");
            builder.OpenElement(seq++, "ul");
            builder.AddAttribute(seq++, "class", "el-cascader-menu__list");
            foreach (var suggestion in suggestions)
            {
                builder.OpenElement(seq++, "li");
                builder.SetKey(suggestion);
                builder.AddAttribute(seq++, "class", "el-cascader-node is-selectable"
                    + (keyboardSuggestionIndex >= 0 && keyboardSuggestionIndex < suggestions.Count
                        && ReferenceEquals(suggestion, suggestions[keyboardSuggestionIndex]) ? " in-active-path" : string.Empty));
                builder.AddAttribute(seq++, "role", "menuitem");
                builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => SelectSuggestionAsync(suggestion)));
                builder.OpenElement(seq++, "span");
                builder.AddAttribute(seq++, "class", "el-cascader-node__label");
                if (SuggestionItemTemplate != null)
                {
                    builder.AddContent(seq++, SuggestionItemTemplate(suggestion));
                }
                else
                {
                    builder.AddContent(seq++, suggestion.Text);
                }
                builder.CloseElement();
                builder.CloseElement();
            }
            builder.CloseElement();
            builder.CloseElement();
            builder.CloseElement();
        }

        public override void Dispose()
        {
            _filterCts?.Cancel();
            _filterCts?.Dispose();
            _filterCts = null;
            if (dropDownOption != null)
            {
                PopupService.SelectDropDownOptionsCore.Remove(dropDownOption);
            }
            base.Dispose();
        }

        /// <summary>
        /// 规范化后的根选项。当选项携带原始数据对象 <see cref="CascaderOption.Data"/> 且
        /// <see cref="ValueKey"/> 非默认的 "value" 时，按 <see cref="ValueKey"/> 从 Data 反射/字典取值，
        /// 回填到 <see cref="CascaderOption.Value"/>（对齐 EP props.value 的语义）。
        /// 未携带 Data 的选项保持原 Value 不变，向后兼容。
        /// </summary>
        private IList<CascaderOption> RootOptions
        {
            get
            {
                var list = Options?.ToList() ?? new List<CascaderOption>();
                if (!string.IsNullOrEmpty(ValueKey) && !string.Equals(ValueKey, "value", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyValueKey(list);
                }
                return list;
            }
        }

        private void ApplyValueKey(IEnumerable<CascaderOption> options)
        {
            foreach (var option in options)
            {
                if (option == null)
                {
                    continue;
                }

                if (option.Data != null)
                {
                    var mapped = ExtractOptionValue(option.Data, ValueKey);
                    if (!string.IsNullOrEmpty(mapped))
                    {
                        option.Value = mapped;
                    }
                }

                if (option.Children != null && option.Children.Any())
                {
                    ApplyValueKey(option.Children);
                }
            }
        }

        private static string ExtractOptionValue(object data, string key)
        {
            if (data == null || string.IsNullOrEmpty(key))
            {
                return null;
            }

            if (data is System.Collections.IDictionary dict && dict.Contains(key))
            {
                return Convert.ToString(dict[key]);
            }

            var prop = data.GetType().GetProperty(key, BindingFlags.Public | BindingFlags.Instance)
                       ?? data.GetType().GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return prop != null ? Convert.ToString(prop.GetValue(data)) : null;
        }

        private bool IsDropDownOpen => dropDownOption != null && dropDownOption.IsShow;

        private bool IsCascaderDisabled => effectiveDisabled;

        /// <summary>多选模式：可见的已选路径（支持 CollapseTags）</summary>
        internal IEnumerable<IList<string>> VisibleCheckedPaths => CollapseTags && MaxCollapseTags > 0
            ? checkedValues.Take(Math.Max(1, MaxCollapseTags))
            : checkedValues;

        internal string GetPathLabel(IList<string> path)
        {
            // 通过 Value 路径找到 CascaderOption 路径，拼接 label
            var options = RootOptions;
            var labels = new List<string>();
            foreach (var v in path)
            {
                var opt = options.FirstOrDefault(x => x.Value == v);
                if (opt == null) { labels.Add(v); break; }
                labels.Add(opt.DisplayLabel);
                options = opt.Children ?? new List<CascaderOption>();
            }
            return ShowAllLevels ? string.Join(Separator, labels) : labels.LastOrDefault() ?? string.Empty;
        }

        internal async Task RemoveCheckedPathAsync(IList<string> path)
        {
            var idx = checkedValues.FindIndex(p => SamePath(p, path));
            if (idx < 0) return;
            checkedValues.RemoveAt(idx);
            Values = checkedValues.Select(x => (IList<string>)x.ToList()).ToList();
            Value = Values.LastOrDefault()?.ToList() ?? new List<string>();
            SetFieldValue(Value, ValidateEvent);
            if (ValueChanged.HasDelegate) await ValueChanged.InvokeAsync(Value);
            if (ModelValueChanged.HasDelegate) await ModelValueChanged.InvokeAsync(Value);
            if (ValuesChanged.HasDelegate) await ValuesChanged.InvokeAsync(Values);
            if (ModelValuesChanged.HasDelegate) await ModelValuesChanged.InvokeAsync(Values);
            if (OnValuesChange.HasDelegate) await OnValuesChange.InvokeAsync(Values);
            StateHasChanged();
        }

        private InputSize EffectiveSize => effectiveSize;
        private bool EffectiveClearable => Clearable && !Multiple;

        private string SuffixIconClass => $"el-icon-arrow-up el-cascader__caret{(IsDropDownOpen ? " is-reverse" : string.Empty)}";

        private string DisplayValue => activePath.Any()
            ? (ShowAllLevels
                ? string.Join(Separator, activePath.Select(x => x.DisplayLabel))
                : activePath.Last().DisplayLabel)
            : string.Empty;

        /// <summary>多选模式：可见的已选路径数量（考虑 CollapseTags）</summary>
        internal int VisibleCheckedCount => CollapseTags && MaxCollapseTags > 0
            ? Math.Min(checkedValues.Count, MaxCollapseTags)
            : checkedValues.Count;

        /// <summary>多选模式：折叠隐藏的已选数量</summary>
        internal int CollapsedCount => CollapseTags && checkedValues.Count > MaxCollapseTags
            ? checkedValues.Count - MaxCollapseTags
            : 0;

        private string InputValue => Filterable && IsDropDownOpen && !string.IsNullOrWhiteSpace(filterText)
            ? filterText
            : DisplayValue;

        bool ISelectDropDownContext.Loading => false;

        string ISelectDropDownContext.LoadingText => string.Empty;

        // EP 的 cascader 空态键是 el.cascader.noData（值「暂无数据」）；本仓此前写的是「无数据」，
        // 所以 fallback 保留本仓原字面量：不接词条时 DOM 一字不变，接了才走 EP 措辞。
        string ISelectDropDownContext.EmptyText => Lang.T("el.cascader.noData", "无数据");

        bool ISelectDropDownContext.ShouldShowEmpty => !RootOptions.Any();

        bool ISelectDropDownContext.ShouldShowNoMatch => false;

        Task ISelectDropDownContext.OnEndReachedAsync()
        {
            return Task.CompletedTask;
        }

        private void RefreshDropDown()
        {
            // 每次刷新换代：BuildPanelContent 的外层 div 带上递增 key，强制 ElPopup diff 时
            // 整棵面板子树重建而不是逐帧配对。实测（浏览器 outerHTML）叶子被勾选、其 li 尾部
            // 新插入 el-icon-check postfix 的那次增量 diff 会把同 li 的 class/role 属性帧丢掉，
            // bUnit 的等价宿主复现不了，只能从结构上免疫。面板节点量级几十个，重建代价可忽略。
            panelGeneration++;
            dropDownOption?.RequestRender?.Invoke();
            StateHasChanged();
        }

        /// <summary>面板换代号：随 RefreshDropDown 递增，作为外层 div 的 key。</summary>
        private int panelGeneration;

        private bool IsInPath(CascaderOption option)
        {
            return activePath.Contains(option);
        }

        private bool IsSelected(CascaderOption option)
        {
            return activePath.LastOrDefault() == option && Value?.LastOrDefault() == option.Value;
        }

        private bool IsChecked(CascaderOption option)
        {
            return checkedValues.Any(path => path.LastOrDefault() == option.Value);
        }

        /// <summary>EP node.vue isInPath：该节点是否位于任一勾选路径上（含勾中节点自身）。</summary>
        private bool IsOnCheckedPath(CascaderOption option)
        {
            return checkedValues.Any(path => path != null && path.Contains(option.Value));
        }

        private async Task EnsureLazyLoadedAsync(CascaderOption option)
        {
            if (!Lazy || LazyLoad == null || option == null || option.Leaf || option.Loaded || option.HasChildren)
            {
                return;
            }

            option.Loading = true;
            RefreshDropDown();
            try
            {
                var children = await LazyLoad(option);
                option.Children = children?.Where(x => x != null).ToList() ?? new List<CascaderOption>();
                option.Loaded = true;
            }
            finally
            {
                option.Loading = false;
            }
        }

        private void BuildSuggestions()
        {
            suggestions.Clear();
            if (string.IsNullOrWhiteSpace(filterText))
            {
                return;
            }

            foreach (var path in EnumeratePaths(RootOptions, new List<CascaderOption>()))
            {
                var option = path.LastOrDefault();
                if (option == null || option.Disabled || (option.HasChildren && !CheckStrictly))
                {
                    continue;
                }

                var matches = FilterMethod != null
                    ? FilterMethod(option, filterText)
                    : path.Any(x => x.DisplayLabel?.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0);
                if (!matches)
                {
                    continue;
                }

                suggestions.Add(new CascaderSuggestion
                {
                    Path = path.ToList(),
                    Text = string.Join(Separator, path.Select(x => x.DisplayLabel))
                });
            }
        }

        private IEnumerable<IList<CascaderOption>> EnumeratePaths(IEnumerable<CascaderOption> options, IList<CascaderOption> prefix, HashSet<CascaderOption> visited = null)
        {
            visited ??= new HashSet<CascaderOption>();
            foreach (var option in options ?? Enumerable.Empty<CascaderOption>())
            {
                // 跳过重复 / 成环的选项，避免无限递归（WASM 下会致命爆栈）
                if (option == null || !visited.Add(option))
                {
                    continue;
                }

                var path = prefix.Concat(new[] { option }).ToList();
                if (option.HasChildren)
                {
                    foreach (var childPath in EnumeratePaths(option.Children, path, visited))
                    {
                        yield return childPath;
                    }
                }

                if (!option.HasChildren || CheckStrictly)
                {
                    yield return path;
                }
            }
        }

        private static bool SamePath(IList<string> first, IList<string> second)
        {
            return first != null && second != null && first.SequenceEqual(second);
        }

        private void TrimMenus(int count)
        {
            while (menus.Count > count)
            {
                menus.RemoveAt(menus.Count - 1);
            }
        }

        private void TrimActivePath(int count)
        {
            while (activePath.Count > count)
            {
                activePath.RemoveAt(activePath.Count - 1);
            }
        }

        private static IList<string> NormalizeValue(object value)
        {
            if (value == null)
            {
                return new List<string>();
            }
            if (value is IList<string> stringList)
            {
                return stringList;
            }
            if (value is IEnumerable<string> stringEnumerable)
            {
                return stringEnumerable.ToList();
            }
            return new List<string> { Convert.ToString(value) };
        }

        private static string GetSizeCssValue(InputSize size) => size switch
        {
            InputSize.Large => "large",
            InputSize.Small => "small",
            _ => null
        };
    }
}
