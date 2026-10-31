using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ElementPlus;

namespace ElementPlus
{
    /// <summary>
    /// 下拉表格选择器（Element Plus 自渲染版）。
    /// 参考 Radzen.Blazor 的 RadzenDropDownDataGrid：弹层内用表格展示数据集并支持行选中，
    /// 但本组件不引入任何 Radzen 代码——直接复用仓库已有的 <see cref="ElTable"/> / <see cref="ElTableColumn"/> 渲染表格，
    /// 自己实现 EP 风格触发器、弹层定位、顶部搜索过滤与单选/多选值映射。
    /// </summary>
    public partial class ElDropDownTableGrow : ElementComponentBase
    {
        /// <summary>数据源（行集合，元素为任意对象）。</summary>
        [Parameter] public IEnumerable Items { get; set; }

        /// <summary>取值字段名（从行对象读取 Value 的属性）。为空则 Value 直接为行对象本身。</summary>
        [Parameter] public string ValueField { get; set; }

        /// <summary>显示字段名（从行对象读取展示文本的属性）。为空则用行对象 ToString()。</summary>
        [Parameter] public string TextField { get; set; }

        /// <summary>单选当前值。</summary>
        [Parameter] public object Value { get; set; }

        /// <summary>单选值变化回调。</summary>
        [Parameter] public EventCallback<object> ValueChanged { get; set; }

        /// <summary>多选当前值集合（取值字段的值集合）。</summary>
        [Parameter] public IEnumerable Values { get; set; }

        /// <summary>多选值变化回调。</summary>
        [Parameter] public EventCallback<IEnumerable> ValuesChanged { get; set; }

        /// <summary>是否多选。默认单选。</summary>
        [Parameter] public bool Multiple { get; set; }

        /// <summary>占位符文本。</summary>
        [Parameter] public string Placeholder { get; set; } = "请选择";

        /// <summary>是否禁用。</summary>
        [Parameter] public bool Disabled { get; set; }

        /// <summary>无边框模式（适合嵌入表格单元格内）。</summary>
        [Parameter] public bool Borderless { get; set; }

        /// <summary>是否可清空。</summary>
        [Parameter] public bool Clearable { get; set; }

        /// <summary>是否显示顶部搜索框。默认 true。</summary>
        [Parameter] public bool AllowSearch { get; set; } = true;

        /// <summary>搜索框占位符。</summary>
        [Parameter] public string SearchPlaceholder { get; set; } = "搜索";

        /// <summary>每页行数。默认 10。</summary>
        [Parameter] public int PageSize { get; set; } = 10;

        /// <summary>是否显示分页。默认 true。</summary>
        [Parameter] public bool ShowPagination { get; set; } = true;

        /// <summary>斑马纹。</summary>
        [Parameter] public bool Stripe { get; set; }

        /// <summary>是否显示边框。</summary>
        [Parameter] public bool Border { get; set; }

        /// <summary>弹层内表格的最大高度（像素）：数据少就按内容收，超出才出滚动区。默认 320。</summary>
        [Parameter] public int Height { get; set; } = 320;

        /// <summary>触发器宽度（如 "300px" / "100%"）。为空则由内容决定。</summary>
        [Parameter] public string Width { get; set; }

        /// <summary>下拉面板宽度（如 "640px" / "70vw" / "100%"）。为空时回落到 <see cref="Width"/>。
        /// 单独设置可让面板比触发器更宽，例如触发器 240px、面板 640px。</summary>
        [Parameter] public string PanelWidth { get; set; }

        /// <summary>下拉面板最小宽度（如 "520px"）。为避免面板过窄，留空且未设 <see cref="PanelWidth"/> 时默认等于触发器宽度。</summary>
        [Parameter] public string PanelMinWidth { get; set; }

        /// <summary>下拉面板最大宽度（如 "90vw"）。用于给超宽面板兜底，避免在小屏上溢出视口。</summary>
        [Parameter] public string PanelMaxWidth { get; set; }

        /// <summary>表格尺寸：<see cref="InputSize.Small"/> = 紧凑，<see cref="InputSize.Normal"/> = 正常（默认），
        /// <see cref="InputSize.Large"/> = 宽松。同时作用于触发器与面板内表格。</summary>
        [Parameter] public InputSize Size { get; set; } = InputSize.Normal;

        /// <summary>面板紧凑模式（默认 false）。只收紧<b>面板内</b>的表格与搜索框
        /// （行高约 30px、单元格留白 4px/8px、字号 12px、面板留白 6px），
        /// <b>不影响</b>触发器尺寸——需要触发器也紧凑请用 <see cref="Size"/> = <see cref="InputSize.Small"/>。</summary>
        [Parameter] public bool Compact { get; set; }

        /// <summary>键盘选择（默认 <c>true</c>，与 Element Plus 的 el-select 一致）。启用后：
        /// <list type="bullet">
        /// <item>面板展开时自动聚焦顶部搜索框；</item>
        /// <item>搜索框内 <c>↑</c> / <c>↓</c> 移动高亮行（首次 ↓ 落在第一行、首次 ↑ 落在最后一行），<c>Enter</c> 确认高亮行；</item>
        /// <item>没有搜索框时（<see cref="AllowSearch"/> = false）在触发器上直接按 ↑/↓/Enter 同样有效；</item>
        /// <item>多选模式 Enter 为「切换该行勾选」，不改动面板开关；单选模式 Enter 与点击行等价。</item>
        /// </list>
        /// 显式设为 <c>false</c> 可回到「搜索框只做过滤、↑/↓/Enter 不参与选择」的旧行为。</summary>
        [Parameter] public bool KeyboardSelect { get; set; } = true;

        /// <summary>空数据提示文本。</summary>
        [Parameter] public string EmptyText { get; set; }

        /// <summary>单选时选中后是否自动关闭弹层。默认 true。</summary>
        [Parameter] public bool AutoCloseOnSelect { get; set; } = true;

        /// <summary>参与搜索的字段名，可给多个（如 <c>new[] { "Name", "Category" }</c>）——命中任一字段即算匹配。
        /// 为空（默认）则检索行对象的所有公开属性。</summary>
        [Parameter] public IEnumerable<string> SearchProperties { get; set; }

        /// <summary>表格列定义（ElTableColumn 集合），作为 ChildContent 传入。</summary>
        [Parameter] public RenderFragment ChildContent { get; set; }

        /// <summary>选择变化回调：单选时为选中的行对象；多选时为选中的行对象 HashSet。</summary>
        [Parameter] public EventCallback<object> OnChange { get; set; }

        /// <summary>清除回调。</summary>
        [Parameter] public EventCallback OnClear { get; set; }

        /// <summary>弹层是否展开。</summary>
        protected bool Visible { get; private set; }

        /// <summary>键盘导航当前高亮行的下标（相对面板内实际渲染的行；-1 = 未高亮）。</summary>
        public int ActiveIndex => activeIndex;

        private string searchText;
        private ElTable tableRef;
        private int activeIndex = -1;
        private ElementReference searchInputRef;
        private bool pendingSearchFocus;

        private ElementReference wrapperRef;
        private ElementReference panelRef;
        private readonly string _panelUid = "ddtg_" + Guid.NewGuid().ToString("N");
        private bool _panelAttached;
        private bool _outsideBound;
        private DotNetObjectReference<ElDropDownTableGrow> _outsideRef;

        private List<object> itemList => (Items as IEnumerable)?.Cast<object>().ToList() ?? new List<object>();

        private List<object> filteredItems =>
            string.IsNullOrWhiteSpace(searchText) ? itemList : itemList.Where(MatchesSearch).ToList();

        /// <summary>当前过滤后的行数（便于调试/测试断言）。</summary>
        public int FilteredCount => filteredItems.Count;

        /// <summary>行元素实际类型（用于给内嵌 ElTable 提供强类型 DataSource，避免列按 object 解析属性失败）。</summary>
        private Type RowType
        {
            get
            {
                if (Items == null)
                {
                    return null;
                }

                var t = Items.GetType();
                if (t.IsGenericType)
                {
                    return t.GetGenericArguments()[0];
                }

                return Items.Cast<object>().FirstOrDefault()?.GetType();
            }
        }

        /// <summary>传给内嵌 ElTable 的 DataSource：无搜索时直接传原始（强类型）集合；有搜索时构造同元素类型的强类型列表，
        /// 确保 ElTable 推断出的 DataType 正确，列 Property 能解析到真实属性。</summary>
        private object FilteredDataSource
        {
            get
            {
                if (Items == null)
                {
                    return new List<object>();
                }

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    return Items;
                }

                var list = filteredItems;
                var rowType = RowType;
                if (rowType == null || rowType == typeof(object))
                {
                    return list;
                }

                var listType = typeof(List<>).MakeGenericType(rowType);
                var typed = (IList)Activator.CreateInstance(listType);
                foreach (var r in list)
                {
                    typed.Add(r);
                }

                return typed;
            }
        }

        /// <summary>当前过滤后行数（供 ElTable 分页计算 Total 用）。</summary>
        public int FilteredTotal => (FilteredDataSource as IEnumerable)?.Cast<object>().Count() ?? 0;

        /// <summary>面板内实际渲染的行（与内嵌 ElTable 的 PageRows 同口径：不开分页 = 全部过滤结果，
        /// 开分页 = 第一页切片）。键盘导航的行下标都相对它。</summary>
        private List<object> DisplayedRows
        {
            get
            {
                var list = filteredItems;
                if (ShowPagination && PageSize > 0 && list.Count > PageSize)
                {
                    return list.Take(PageSize).ToList();
                }

                return list;
            }
        }

        private object selectedItem =>
            Multiple ? null : itemList.FirstOrDefault(r => Equals(GetFieldValue(r, ValueField), Value));

        private List<object> selectedItems =>
            Multiple
                ? itemList.Where(r => Values != null && Values.Cast<object>().Contains(GetFieldValue(r, ValueField))).ToList()
                : new List<object>();

        private HashSet<object> selectedRowSet => new HashSet<object>(selectedItems, ReferenceEqualityComparer.Instance);

        private bool MatchesSearch(object row)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            if (SearchProperties != null && SearchProperties.Any())
            {
                return SearchProperties.Any(p => ContainsText(GetFieldValue(row, p), searchText));
            }

            return row.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Any(p => ContainsText(p.GetValue(row), searchText));
        }

        private static bool ContainsText(object value, string text) =>
            value != null && value.ToString().IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;

        private static object GetFieldValue(object row, string field)
        {
            if (row == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(field))
            {
                return row;
            }

            var prop = row.GetType().GetProperty(field, BindingFlags.Public | BindingFlags.Instance);
            return prop?.GetValue(row);
        }

        /// <summary>展开弹层。</summary>
        public async Task ShowAsync()
        {
            if (Disabled || Visible)
            {
                return;
            }

            Visible = true;
            activeIndex = -1;
            // 键盘选择模式：展开后自动把焦点放进搜索框（渲染完成后再聚焦，见 OnAfterRenderAsync）
            pendingSearchFocus = KeyboardSelect && AllowSearch;
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>关闭弹层（并清空搜索关键字）。</summary>
        public async Task CloseAsync()
        {
            if (!Visible)
            {
                return;
            }

            Visible = false;
            searchText = null;
            activeIndex = -1;
            pendingSearchFocus = false;
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>document 捕获阶段收到「控件之外」的 mousedown：收起弹层（由 JS 回调）。</summary>
        [JSInvokable]
        public Task OnOutsideClickAsync() => CloseAsync();

        public override void Dispose()
        {
            if (_outsideBound)
            {
                _ = JSRuntime.InvokeVoidAsync("elementPickerOutsideDetach", wrapperRef);
                _outsideBound = false;
            }
            _outsideRef?.Dispose();
            _outsideRef = null;
            base.Dispose();
        }

        /// <summary>切换弹层展开状态。</summary>
        public async Task ToggleAsync()
        {
            if (Disabled)
            {
                return;
            }

            if (Visible)
            {
                await CloseAsync();
            }
            else
            {
                await ShowAsync();
            }
        }

        /// <summary>触发器上的键盘处理。默认（<see cref="KeyboardSelect"/> = false）只支持「Esc 收起 / Enter 展开」；
        /// 打开键盘选择后，面板展开时 ↑/↓ 移动高亮行、Enter 确认高亮行（无搜索框时靠这里工作）。</summary>
        public async Task OnKeyDownAsync(KeyboardEventArgs e)
        {
            if (e.Key == "Escape" && Visible)
            {
                await CloseAsync();
                return;
            }

            if (KeyboardSelect && Visible)
            {
                switch (e.Key)
                {
                    case "ArrowDown":
                        await MoveActiveAsync(1);
                        return;
                    case "ArrowUp":
                        await MoveActiveAsync(-1);
                        return;
                    case "Enter":
                        await ConfirmActiveAsync();
                        return;
                    default:
                        return;
                }
            }

            if (e.Key == "Enter" && !Visible)
            {
                await ShowAsync();
            }
        }

        /// <summary>搜索框内键盘处理（仅 <see cref="KeyboardSelect"/> 打开时参与选择；关闭时这里什么都不做，
        /// 事件仍按原样冒泡给触发器，保持「只过滤不选择」的原行为）。</summary>
        /// <remarks>输入框上的 <c>@onkeydown:stopPropagation</c> 绑定到 <see cref="KeyboardSelect"/>，
        /// 因此本模式下来自搜索框的事件不会再次触发触发器上的处理（否则 Enter 确认后会被重新展开）。</remarks>
        public async Task OnSearchKeyDownAsync(KeyboardEventArgs e)
        {
            if (!KeyboardSelect)
            {
                return;
            }

            switch (e.Key)
            {
                case "ArrowDown":
                    await MoveActiveAsync(1);
                    break;
                case "ArrowUp":
                    await MoveActiveAsync(-1);
                    break;
                case "Enter":
                    await ConfirmActiveAsync();
                    break;
                case "Escape":
                    await CloseAsync();
                    break;
            }
        }

        /// <summary>移动高亮行：未高亮时 ↓ 落到第一行、↑ 落到最后一行；到底 / 到顶后夹住不再越界。</summary>
        private async Task MoveActiveAsync(int delta)
        {
            var count = DisplayedRows.Count;
            if (count == 0)
            {
                activeIndex = -1;
            }
            else if (activeIndex < 0)
            {
                activeIndex = delta > 0 ? 0 : count - 1;
            }
            else
            {
                activeIndex = Math.Min(count - 1, Math.Max(0, activeIndex + delta));
            }

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>确认高亮行：还没高亮时按第一行处理（「输入关键字后直接回车」即可选中首条）。
        /// 单选 = 与点击行等价（设值 + 按 <see cref="AutoCloseOnSelect"/> 收起）；多选 = 切换该行勾选。</summary>
        public async Task ConfirmActiveAsync()
        {
            var rows = DisplayedRows;
            if (rows.Count == 0)
            {
                return;
            }

            var row = rows[activeIndex < 0 ? 0 : Math.Min(activeIndex, rows.Count - 1)];
            if (Multiple)
            {
                await ToggleRowSelectionAsync(row);
            }
            else
            {
                await OnTableRowClickAsync(row);
            }
        }

        /// <summary>多选模式下切换某一行的勾选状态（键盘 Enter 用，与勾选框行为一致）。</summary>
        private async Task ToggleRowSelectionAsync(object row)
        {
            var key = GetFieldValue(row, ValueField);
            var list = (Values ?? Enumerable.Empty<object>()).Cast<object>().ToList();
            if (list.Any(v => Equals(v, key)))
            {
                list.RemoveAll(v => Equals(v, key));
            }
            else
            {
                list.Add(key);
            }

            Values = list;
            if (ValuesChanged.HasDelegate)
            {
                await ValuesChanged.InvokeAsync(Values);
            }

            if (OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(selectedRowSet);
            }

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>高亮行的 class：只给面板内当前行加 <c>is-keyboard-active</c>；
        /// <see cref="KeyboardSelect"/> 关闭时返回 null（不改变默认渲染）。</summary>
        /// <remarks>每次读取都新建闭包并捕获当时的 <c>activeIndex</c>，保证子组件收到的参数判定为「已变化」，
        /// 高亮才能随按键即时刷新（沿用同一委托实例时 Blazor 可能跳过子组件重渲染）。</remarks>
        private Func<object, int, string> KeyboardRowClassName
        {
            get
            {
                if (!KeyboardSelect)
                {
                    return null;
                }

                var index = activeIndex;
                return (row, rowIndex) => rowIndex == index ? "is-keyboard-active" : null;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (!Visible)
            {
                // 弹层已收起：解绑 JS 滚动/缩放监听，复位附加状态
                if (_panelAttached)
                {
                    _panelAttached = false;
                    await JSRuntime.InvokeVoidAsync("elementDropDownTablePanelDetach", _panelUid);
                }
                if (_outsideBound)
                {
                    _outsideBound = false;
                    await JSRuntime.InvokeVoidAsync("elementPickerOutsideDetach", wrapperRef);
                }

                if (!pendingSearchFocus)
                {
                    return;
                }
            }
            else if (panelRef.Context != null)
            {
                // 弹层已渲染：把面板交给 JS 做 fixed 定位（逃逸祖先滚动容器的裁剪 + 滚轮穿透隔离）。
                // 每次渲染后都重定位一遍：搜索/分页会重渲染面板，避免 Blazor 重设 style/class 冲掉 JS 写入的 top/left。
                if (!_panelAttached)
                {
                    _panelAttached = true;
                    await JSRuntime.InvokeVoidAsync("elementDropDownTablePanelAttach", _panelUid, wrapperRef, panelRef);
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("elementDropDownTablePanelReposition", _panelUid);
                }

                // 「点外面收起」挂在 document 的捕获阶段，而不是铺一层全屏 backdrop：
                // backdrop 是 fixed 覆盖层，滚轮在它身上走到「视口」这条滚动链，开着面板时整页滚不动。
                if (!_outsideBound)
                {
                    _outsideRef ??= DotNetObjectReference.Create(this);
                    await JSRuntime.InvokeVoidAsync(
                        "elementPickerOutsideAttach",
                        wrapperRef,
                        ".el-dropdown-table-grow__panel",
                        _outsideRef,
                        nameof(OnOutsideClickAsync));
                    _outsideBound = true;
                }
            }

            if (!pendingSearchFocus)
            {
                return;
            }

            pendingSearchFocus = false;
            if (!KeyboardSelect || !Visible || searchInputRef.Context == null)
            {
                return;
            }

            await searchInputRef.FocusAsync();
        }

        public async Task OnSearchInputAsync(ChangeEventArgs e)
        {
            searchText = e?.Value?.ToString();
            // 过滤结果变了，旧的高亮下标已无意义
            activeIndex = -1;
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>行点击事件（来自内嵌 ElTable 的 RowClick）。单选模式据此设值并关闭；多选模式忽略（由复选框列驱动）。</summary>
        public async Task OnTableRowClickAsync(object row)
        {
            if (row == null)
            {
                return;
            }

            // 鼠标点的行就是当前高亮行，键盘接续操作时不会跳回旧位置
            activeIndex = DisplayedRows.IndexOf(row);

            if (Multiple)
            {
                return;
            }

            Value = GetFieldValue(row, ValueField);
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }

            if (OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(row);
            }

            if (AutoCloseOnSelect)
            {
                await CloseAsync();
            }
        }

        /// <summary>多选变化事件（来自内嵌 ElTable 的 SelectedRowsChanged）。</summary>
        public async Task OnTableSelectionChangedAsync(HashSet<object> rows)
        {
            if (!Multiple || rows == null)
            {
                return;
            }

            Values = rows.Select(r => GetFieldValue(r, ValueField)).ToList();
            if (ValuesChanged.HasDelegate)
            {
                await ValuesChanged.InvokeAsync(Values);
            }

            if (OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(rows);
            }
        }

        /// <summary>清空选择。</summary>
        public async Task ClearAsync()
        {
            if (Multiple)
            {
                Values = null;
                if (ValuesChanged.HasDelegate)
                {
                    await ValuesChanged.InvokeAsync(Values);
                }
            }
            else
            {
                Value = null;
                if (ValueChanged.HasDelegate)
                {
                    await ValueChanged.InvokeAsync(Value);
                }
            }

            if (OnClear.HasDelegate)
            {
                await OnClear.InvokeAsync();
            }

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>移除某个已选标签（多选）。</summary>
        public async Task RemoveItemAsync(object item, MouseEventArgs e)
        {
            if (!Multiple || Values == null)
            {
                return;
            }

            var list = Values.Cast<object>().ToList();
            list.Remove(GetFieldValue(item, ValueField));
            Values = list;
            if (ValuesChanged.HasDelegate)
            {
                await ValuesChanged.InvokeAsync(Values);
            }

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>行对象展示文本。</summary>
        protected string DisplayText(object item) =>
            item == null ? string.Empty : (GetFieldValue(item, TextField)?.ToString() ?? item.ToString());

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            public new bool Equals(object x, object y) => ReferenceEquals(x, y);

            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
