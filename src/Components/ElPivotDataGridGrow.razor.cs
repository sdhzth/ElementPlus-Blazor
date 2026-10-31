using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ElementPlus
{
    /// <summary>
    /// 交叉表（透视表）组件（Element Plus 自渲染版）。
    /// <para>
    /// 对应参考实现 Radzen 的 <c>RadzenPivotDataGrid&lt;TItem&gt;</c>，但**完全不依赖 Radzen**：
    /// 分组、聚合、筛选、排序、下钻、合计全部由自写的纯 C# 引擎（<see cref="ElPivotEngine"/>）完成，
    /// 无 <c>System.Linq.Dynamic.Core</c>、无 <c>PropertyAccess</c>、无 <c>RadzenPopup</c>、无图标字体、
    /// **且不使用任何 JavaScript**（服务端渲染 / 预渲染 / 单元测试均可用）。
    /// </para>
    /// <para>
    /// 用法：
    /// <code>
    /// &lt;ElPivotDataGridGrow @data="orders" TItem="Order" AllowPaging="true" ShowColumnsTotals="true"&gt;
    ///     &lt;Columns&gt;
    ///         &lt;ElPivotColumnGrow TItem="Order" Property="Region" Title="区域" /&gt;
    ///         &lt;ElPivotColumnGrow TItem="Order" Property="Year" Title="年份" /&gt;
    ///     &lt;/Columns&gt;
    ///     &lt;Rows&gt;
    ///         &lt;ElPivotRowGrow TItem="Order" Property="Category" Title="品类" /&gt;
    ///         &lt;ElPivotRowGrow TItem="Order" Property="Product" Title="商品" /&gt;
    ///     &lt;/Rows&gt;
    ///     &lt;Aggregates&gt;
    ///         &lt;ElPivotAggregateGrow TItem="Order" Property="Amount" Title="金额" Aggregate="ElPivotAggregateFunction.Sum" FormatString="N2" /&gt;
    ///     &lt;/Aggregates&gt;
    /// &lt;/ElPivotDataGridGrow&gt;
    /// </code>
    /// </para>
    /// <para>
    /// 浏览器兼容性：本组件不输出任何 JS，配套样式只使用 Chrome 49+ 的特性
    /// （规避 <c>:has()</c>/<c>:is()</c>/<c>:where()</c>/逻辑属性/<c>gap</c>/<c>clamp()</c>/CSS 嵌套）。
    /// </para>
    /// </summary>
    /// <typeparam name="TItem">记录类型。</typeparam>
    [CascadingTypeParameter(nameof(TItem))]
    public partial class ElPivotDataGridGrow<TItem> : ElementComponentBase
    {
        // ---------------------------------------------------------------- 参数：数据

        /// <summary>数据源。</summary>
        [Parameter]
        public IEnumerable<TItem> Data { get; set; }

        // ---------------------------------------------------------------- 参数：字段声明

        /// <summary>列字段声明片段。</summary>
        [Parameter]
        public RenderFragment Columns { get; set; }

        /// <summary>行字段声明片段。</summary>
        [Parameter]
        public RenderFragment Rows { get; set; }

        /// <summary>值字段声明片段。</summary>
        [Parameter]
        public RenderFragment Aggregates { get; set; }

        // ---------------------------------------------------------------- 参数：字段选择器

        /// <summary>是否显示字段选择器。默认 <c>false</c>（参考实现的文档注释亦声明默认关闭）。</summary>
        [Parameter]
        public bool AllowFieldsPicking { get; set; }

        /// <summary>字段选择器的标题。</summary>
        [Parameter]
        public string FieldsPickerHeaderText { get; set; }

        /// <summary>字段选择器标题的替换模板。</summary>
        [Parameter]
        public RenderFragment FieldsPickerHeaderTemplate { get; set; }

        /// <summary>字段选择器初始是否展开。</summary>
        [Parameter]
        public bool FieldsPickerExpanded { get; set; }

        /// <summary>行区域初始选中的属性（按顺序）。为 <c>null</c> 时按字段的 <c>Selected</c> 参数推导。</summary>
        [Parameter]
        public IReadOnlyList<string> SelectedRows { get; set; }

        /// <summary>列区域初始选中的属性（按顺序）。</summary>
        [Parameter]
        public IReadOnlyList<string> SelectedColumns { get; set; }

        /// <summary>值区域初始选中的属性（按顺序）。</summary>
        [Parameter]
        public IReadOnlyList<string> SelectedAggregates { get; set; }

        /// <summary>行区域选中变化回调。</summary>
        [Parameter]
        public EventCallback<IReadOnlyList<string>> SelectedRowsChanged { get; set; }

        /// <summary>列区域选中变化回调。</summary>
        [Parameter]
        public EventCallback<IReadOnlyList<string>> SelectedColumnsChanged { get; set; }

        /// <summary>值区域选中变化回调。</summary>
        [Parameter]
        public EventCallback<IReadOnlyList<string>> SelectedAggregatesChanged { get; set; }

        /// <summary>任一部分的字段选择发生变化时触发。</summary>
        [Parameter]
        public EventCallback<ElPivotFieldSelectionEventArgs> FieldSelectionChanged { get; set; }

        // ---------------------------------------------------------------- 参数：行为

        /// <summary>是否允许排序（点击表头循环切换升序/降序/不排序）。</summary>
        [Parameter]
        public bool AllowSorting { get; set; } = true;

        /// <summary>是否允许筛选。</summary>
        [Parameter]
        public bool AllowFiltering { get; set; } = true;

        /// <summary>是否允许下钻（分组折叠 / 展开）。</summary>
        [Parameter]
        public bool AllowDrillDown { get; set; } = true;

        /// <summary>分组初始是否全部展开。默认 <c>false</c>（与参考实现一致：初始只显示最外层）。</summary>
        [Parameter]
        public bool ExpandGroupsByDefault { get; set; }

        /// <summary>筛选时字符串比较的默认大小写敏感性。</summary>
        [Parameter]
        public ElPivotFilterCaseSensitivity FilterCaseSensitivity { get; set; } = ElPivotFilterCaseSensitivity.CaseInsensitive;

        /// <summary>多个字段之间筛选条件的组合方式。</summary>
        [Parameter]
        public ElPivotLogicalOperator LogicalFilterOperator { get; set; } = ElPivotLogicalOperator.And;

        /// <summary>网格级筛选条件（对所有字段生效）。</summary>
        [Parameter]
        public ElPivotFilterDescriptor Filter { get; set; }

        // ---------------------------------------------------------------- 参数：外观

        /// <summary>是否显示行合计（行末按值字段汇总）。</summary>
        [Parameter]
        public bool ShowRowsTotals { get; set; }

        /// <summary>是否显示列合计与右下角总计。</summary>
        [Parameter]
        public bool ShowColumnsTotals { get; set; }

        /// <summary>是否显示「值字段名」表头行。默认 <c>true</c>；值字段只有一个时可置 <c>false</c>。</summary>
        [Parameter]
        public bool ShowAggregateHeaderRow { get; set; } = true;

        /// <summary>是否使用斑马纹。</summary>
        [Parameter]
        public bool AllowAlternatingRows { get; set; } = true;

        /// <summary>网格线样式。</summary>
        [Parameter]
        public ElPivotGridLines GridLines { get; set; } = ElPivotGridLines.Default;

        /// <summary>表格区域最大高度（CSS 长度，例如 <c>420px</c>）；超出时纵向滚动且表头吸顶。</summary>
        [Parameter]
        public string MaxHeight { get; set; }

        /// <summary>是否正在加载（显示遮罩）。</summary>
        [Parameter]
        public bool IsLoading { get; set; }

        // ---------------------------------------------------------------- 参数：分页

        /// <summary>是否分页。**注意：分页作用于交叉表的「结果行」，而不是源数据**（见文档「与参考实现的差异」）。</summary>
        [Parameter]
        public bool AllowPaging { get; set; }

        /// <summary>每页行数（默认 10）。</summary>
        [Parameter]
        public int PageSize { get; set; } = 10;

        /// <summary>当前页（从 1 开始）。</summary>
        [Parameter]
        public int PageIndex { get; set; } = 1;

        /// <summary>当前页变化回调。</summary>
        [Parameter]
        public EventCallback<int> PageIndexChanged { get; set; }

        /// <summary>每页行数变化回调。</summary>
        [Parameter]
        public EventCallback<int> PageSizeChanged { get; set; }

        /// <summary>分页器位置（默认只在表格下方显示）。</summary>
        [Parameter]
        public ElPivotPagerPosition PagerPosition { get; set; } = ElPivotPagerPosition.Bottom;

        /// <summary>每页行数可选项。</summary>
        [Parameter]
        public int[] PageSizeOptions { get; set; } = new[] { 10, 20, 50, 100 };

        /// <summary>是否显示分页摘要。</summary>
        [Parameter]
        public bool ShowPagerSummary { get; set; } = true;

        /// <summary>数字按钮窗口大小（默认 5）。</summary>
        [Parameter]
        public int PageNumbersCount { get; set; } = 5;

        // ---------------------------------------------------------------- 参数：文案与模板

        /// <summary>文案集合；为 <c>null</c> 时使用中文默认值。</summary>
        [Parameter]
        public ElPivotDataGridGrowStrings Strings { get; set; }

        /// <summary>无数据时的提示文本（覆盖 <see cref="Strings"/> 中的同名项）。</summary>
        [Parameter]
        public string EmptyText { get; set; }

        /// <summary>无数据时的替换模板。</summary>
        [Parameter]
        public RenderFragment EmptyTemplate { get; set; }

        /// <summary>加载中的替换模板。</summary>
        [Parameter]
        public RenderFragment LoadingTemplate { get; set; }

        // ---------------------------------------------------------------- 参数：事件

        /// <summary>值单元格 / 合计单元格被点击。</summary>
        [Parameter]
        public EventCallback<ElPivotCellEventArgs<TItem>> CellClicked { get; set; }

        /// <summary>排序状态变化。</summary>
        [Parameter]
        public EventCallback<ElPivotSortChangedEventArgs> SortChanged { get; set; }

        /// <summary>筛选条件变化。</summary>
        [Parameter]
        public EventCallback<ElPivotFilterChangedEventArgs> FilterChanged { get; set; }

        /// <summary>分组展开 / 折叠。</summary>
        [Parameter]
        public EventCallback<ElPivotDrillDownEventArgs> DrillDownToggled { get; set; }

        /// <summary>分组折叠状态集合变化（可用于持久化视图状态）。</summary>
        [Parameter]
        public EventCallback<ElPivotGroupStateChangedEventArgs> GroupStateChanged { get; set; }

        // ---------------------------------------------------------------- 内部状态

        private readonly List<ElPivotColumnGrow<TItem>> allColumns = new List<ElPivotColumnGrow<TItem>>();
        private readonly List<ElPivotRowGrow<TItem>> allRows = new List<ElPivotRowGrow<TItem>>();
        private readonly List<ElPivotAggregateGrow<TItem>> allAggregates = new List<ElPivotAggregateGrow<TItem>>();

        private readonly List<ElPivotFieldGrow<TItem>> candidateFields = new List<ElPivotFieldGrow<TItem>>();

        private readonly Dictionary<string, ElPivotSortOrder?> sortOverrides = new Dictionary<string, ElPivotSortOrder?>(StringComparer.Ordinal);
        private readonly Dictionary<string, ElPivotFilterDescriptor> filterOverrides = new Dictionary<string, ElPivotFilterDescriptor>(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> groupState = new Dictionary<string, bool>(StringComparer.Ordinal);

        private List<string> selectedRows;
        private List<string> selectedColumns;
        private List<ElPivotSelectedField> selectedAggregateFields;

        private ElPivotLayout<TItem> layout;
        private bool layoutDirty = true;

        /// <summary>
        /// 字段集合是否在本轮渲染中发生了变化。
        /// <para>
        /// 关键时序：布局在渲染树**顶层**的代码块里构建，而字段组件是在父组件的渲染树被 diff 时才实例化并注册的
        /// （即注册永远晚于本次布局计算）。因此首次渲染必然拿到「零字段」的布局，必须在渲染完成后补一次渲染，
        /// 否则界面会停留在「未设置字段」的空状态。
        /// </para>
        /// </summary>
        private bool fieldsDirty;

        private int currentPageIndex = 1;
        private int currentPageSize;

        private ElPivotFieldGrow<TItem> filterField;
        private ElPivotFilterDescriptor workingFilter;
        private bool filterPanelOpen;
        private bool pickerExpanded;

        // ---------------------------------------------------------------- 字段集合

        /// <summary>当前参与计算的列字段（按层级顺序）。</summary>
        public IReadOnlyList<ElPivotColumnGrow<TItem>> ColumnsCollection
        {
            get { return BuildActiveColumns(); }
        }

        /// <summary>当前参与计算的行字段（按层级顺序）。</summary>
        public IReadOnlyList<ElPivotRowGrow<TItem>> RowsCollection
        {
            get { return BuildActiveRows(); }
        }

        /// <summary>当前参与计算的值字段（按显示顺序）。</summary>
        public IReadOnlyList<ElPivotAggregateGrow<TItem>> AggregatesCollection
        {
            get { return BuildActiveAggregates(); }
        }

        /// <summary>全部已声明的候选字段（字段选择器的数据源，按声明顺序去重）。</summary>
        public IReadOnlyList<ElPivotFieldGrow<TItem>> CandidateFields
        {
            get { return candidateFields; }
        }

        /// <summary>当前解析后的文案集合。</summary>
        public ElPivotDataGridGrowStrings ResolvedStrings
        {
            get { return Strings ?? ElPivotDataGridGrowStrings.Default; }
        }

        /// <summary>无数据提示文本：优先用 <see cref="EmptyText"/>，未设置时回退到 <see cref="Strings"/>。</summary>
        public string ResolvedEmptyText
        {
            get { return string.IsNullOrEmpty(emptyTextOverride) ? ResolvedStrings.EmptyText : emptyTextOverride; }
        }

        private string emptyTextOverride;

        // ---------------------------------------------------------------- 注册

        internal void RegisterColumn(ElPivotColumnGrow<TItem> column)
        {
            if (column == null || allColumns.Contains(column))
            {
                return;
            }

            allColumns.Add(column);
            AddCandidate(column);
            Invalidate();
            ResetDerivedSelection();
            fieldsDirty = true;
        }

        internal void UnregisterColumn(ElPivotColumnGrow<TItem> column)
        {
            if (column != null && allColumns.Remove(column))
            {
                candidateFields.Remove(column);
                Invalidate();
                ResetDerivedSelection();
                fieldsDirty = true;
            }
        }

        internal void RegisterRow(ElPivotRowGrow<TItem> row)
        {
            if (row == null || allRows.Contains(row))
            {
                return;
            }

            allRows.Add(row);
            AddCandidate(row);
            Invalidate();
            ResetDerivedSelection();
            fieldsDirty = true;
        }

        internal void UnregisterRow(ElPivotRowGrow<TItem> row)
        {
            if (row != null && allRows.Remove(row))
            {
                candidateFields.Remove(row);
                Invalidate();
                ResetDerivedSelection();
                fieldsDirty = true;
            }
        }

        internal void RegisterAggregate(ElPivotAggregateGrow<TItem> aggregate)
        {
            if (aggregate == null || allAggregates.Contains(aggregate))
            {
                return;
            }

            allAggregates.Add(aggregate);
            AddCandidate(aggregate);
            Invalidate();
            ResetDerivedSelection();
            fieldsDirty = true;
        }

        internal void UnregisterAggregate(ElPivotAggregateGrow<TItem> aggregate)
        {
            if (aggregate != null && allAggregates.Remove(aggregate))
            {
                candidateFields.Remove(aggregate);
                Invalidate();
                ResetDerivedSelection();
                fieldsDirty = true;
            }
        }

        private void AddCandidate(ElPivotFieldGrow<TItem> field)
        {
            foreach (var existing in candidateFields)
            {
                if (string.Equals(existing.Property, field.Property, StringComparison.Ordinal))
                {
                    return;
                }
            }

            candidateFields.Add(field);
        }

        /// <summary>
        /// 标记布局需要重新计算。
        /// </summary>
        public void Invalidate()
        {
            layoutDirty = true;
        }

        /// <summary>
        /// 字段集合变化时丢弃「由字段推导出来的」选择结果。
        /// <para>
        /// 为什么必须丢弃：<c>ResolveSelectedRows/Columns/Aggregates</c> 在首帧就会被调用，
        /// 而此时字段组件还没注册（注册发生在父组件渲染树被 diff 的阶段，晚于顶层布局计算），
        /// 于是推导结果会是一份**空列表**并被缓存下来，界面将永远停在「未设置字段」。
        /// 这里在注册 / 注销后把缓存置空，让下次解析重新从已注册字段推导。
        /// </para>
        /// <para>
        /// 显式传入 <c>SelectedRows/SelectedColumns/SelectedAggregates</c> 时属于受控参数，
        /// 不能丢弃（否则会覆盖调用方的选择）。
        /// </para>
        /// </summary>
        private void ResetDerivedSelection()
        {
            if (SelectedRows == null)
            {
                selectedRows = null;
            }

            if (SelectedColumns == null)
            {
                selectedColumns = null;
            }

            if (SelectedAggregates == null)
            {
                selectedAggregateFields = null;
            }
        }

        // ---------------------------------------------------------------- 活动字段解析

        private List<string> ResolveSelectedRows()
        {
            if (selectedRows != null)
            {
                return selectedRows;
            }

            if (SelectedRows != null)
            {
                selectedRows = new List<string>(SelectedRows);
                return selectedRows;
            }

            selectedRows = new List<string>();

            foreach (var row in allRows)
            {
                if (row.Selected && !string.IsNullOrEmpty(row.Property))
                {
                    selectedRows.Add(row.Property);
                }
            }

            return selectedRows;
        }

        private List<string> ResolveSelectedColumns()
        {
            if (selectedColumns != null)
            {
                return selectedColumns;
            }

            if (SelectedColumns != null)
            {
                selectedColumns = new List<string>(SelectedColumns);
                return selectedColumns;
            }

            selectedColumns = new List<string>();

            foreach (var column in allColumns)
            {
                if (column.Selected && !string.IsNullOrEmpty(column.Property))
                {
                    selectedColumns.Add(column.Property);
                }
            }

            return selectedColumns;
        }

        private List<ElPivotSelectedField> ResolveSelectedAggregates()
        {
            if (selectedAggregateFields != null)
            {
                return selectedAggregateFields;
            }

            selectedAggregateFields = new List<ElPivotSelectedField>();

            if (SelectedAggregates != null)
            {
                foreach (var property in SelectedAggregates)
                {
                    selectedAggregateFields.Add(new ElPivotSelectedField
                    {
                        Property = property,
                        Function = ResolveFunction(property)
                    });
                }

                return selectedAggregateFields;
            }

            foreach (var aggregate in allAggregates)
            {
                if (aggregate.Selected && !string.IsNullOrEmpty(aggregate.Property))
                {
                    selectedAggregateFields.Add(new ElPivotSelectedField
                    {
                        Property = aggregate.Property,
                        Function = aggregate.Aggregate,
                        PropertyType = aggregate.FilterPropertyType
                    });
                }
            }

            return selectedAggregateFields;
        }

        private ElPivotAggregateFunction ResolveFunction(string property)
        {
            var declared = FindDeclaredAggregate(property);

            if (declared != null)
            {
                return declared.Aggregate;
            }

            var type = string.IsNullOrEmpty(property) ? typeof(string) : ElPivotPropertyAccess.GetPropertyType(typeof(TItem), property);

            return ElPivotAggregateFunctions.DefaultFor(type);
        }

        private ElPivotAggregateGrow<TItem> FindDeclaredAggregate(string property)
        {
            foreach (var aggregate in allAggregates)
            {
                if (string.Equals(aggregate.Property, property, StringComparison.Ordinal))
                {
                    return aggregate;
                }
            }

            return null;
        }

        private ElPivotColumnGrow<TItem> FindDeclaredColumn(string property)
        {
            foreach (var column in allColumns)
            {
                if (string.Equals(column.Property, property, StringComparison.Ordinal))
                {
                    return column;
                }
            }

            return null;
        }

        private ElPivotRowGrow<TItem> FindDeclaredRow(string property)
        {
            foreach (var row in allRows)
            {
                if (string.Equals(row.Property, property, StringComparison.Ordinal))
                {
                    return row;
                }
            }

            return null;
        }

        private List<ElPivotColumnGrow<TItem>> BuildActiveColumns()
        {
            var result = new List<ElPivotColumnGrow<TItem>>();

            foreach (var property in ResolveSelectedColumns())
            {
                var declared = FindDeclaredColumn(property);

                if (declared != null)
                {
                    result.Add(declared);
                    continue;
                }

                // 选择里出现了未声明的属性：合成一个只读字段，保证仍然可以参与计算。
                result.Add(new ElPivotColumnGrow<TItem>
                {
                    Property = property,
                    Title = property
                });
            }

            return result;
        }

        private List<ElPivotRowGrow<TItem>> BuildActiveRows()
        {
            var result = new List<ElPivotRowGrow<TItem>>();

            foreach (var property in ResolveSelectedRows())
            {
                var declared = FindDeclaredRow(property);

                if (declared != null)
                {
                    result.Add(declared);
                    continue;
                }

                result.Add(new ElPivotRowGrow<TItem>
                {
                    Property = property,
                    Title = property
                });
            }

            return result;
        }

        private List<ElPivotAggregateGrow<TItem>> BuildActiveAggregates()
        {
            var result = new List<ElPivotAggregateGrow<TItem>>();

            foreach (var selection in ResolveSelectedAggregates())
            {
                var declared = FindDeclaredAggregate(selection.Property);

                if (declared != null)
                {
                    result.Add(declared);
                    continue;
                }

                result.Add(new ElPivotAggregateGrow<TItem>
                {
                    Property = selection.Property,
                    Title = selection.Property,
                    Aggregate = selection.Function
                });
            }

            return result;
        }

        // ---------------------------------------------------------------- 规格构造

        private ElPivotSortOrder? GetEffectiveSortOrder(string property)
        {
            if (sortOverrides.TryGetValue(property, out var overridden))
            {
                return overridden;
            }

            var field = FindDeclaredRow(property) as ElPivotFieldGrow<TItem> ?? FindDeclaredColumn(property);

            return field == null ? null : field.SortOrder;
        }

        private ElPivotFilterDescriptor GetEffectiveFilter(string property)
        {
            if (filterOverrides.TryGetValue(property, out var overridden))
            {
                return overridden == null || overridden.IsEmpty ? null : overridden;
            }

            // 注意：?? 右结合，三种不同类型不能写成一条链，必须分步赋值。
            ElPivotFieldGrow<TItem> field = FindDeclaredRow(property);
            field = field ?? FindDeclaredColumn(property);
            field = field ?? FindDeclaredAggregate(property);

            return field == null ? null : field.BuildInitialFilter();
        }

        private List<ElPivotFieldSpec> BuildFieldSpecs(IReadOnlyList<ElPivotFieldGrow<TItem>> fields)
        {
            var specs = new List<ElPivotFieldSpec>(fields.Count);

            foreach (var field in fields)
            {
                var spec = new ElPivotFieldSpec
                {
                    Property = field.Property,
                    Title = field.GetTitle(),
                    SortOrder = GetEffectiveSortOrder(field.Property),
                    Filter = GetEffectiveFilter(field.Property),
                    PropertyType = field.FilterPropertyType
                };

                var column = field as ElPivotColumnGrow<TItem>;

                if (column != null)
                {
                    spec.Width = column.Width;
                    spec.MaxGroups = column.MaxGroups;
                    spec.OthersLabel = column.OthersLabel;
                }

                var row = field as ElPivotRowGrow<TItem>;

                if (row != null)
                {
                    spec.MaxGroups = row.MaxGroups;
                    spec.OthersLabel = row.OthersLabel;
                }

                specs.Add(spec);
            }

            return specs;
        }

        private List<ElPivotAggregateSpec> BuildAggregateSpecs()
        {
            var fields = BuildActiveAggregates();
            var specs = new List<ElPivotAggregateSpec>(fields.Count);

            foreach (var field in fields)
            {
                specs.Add(new ElPivotAggregateSpec
                {
                    Property = field.Property,
                    Title = field.GetTitle(),
                    Function = field.Aggregate,
                    PropertyType = field.FilterPropertyType,
                    SortOrder = field.SortOrder,
                    Filter = GetEffectiveFilter(field.Property)
                });
            }

            return specs;
        }

        /// <summary>
        /// 当前有效的引擎选项（对测试与自定义渲染开放）。
        /// </summary>
        public ElPivotEngineOptions BuildEngineOptions()
        {
            return new ElPivotEngineOptions
            {
                AllowDrillDown = AllowDrillDown,
                ExpandGroupsByDefault = ExpandGroupsByDefault,
                GroupState = groupState,
                OthersText = ResolvedStrings.OthersText,
                NullGroupText = ResolvedStrings.NullGroupText,
                ShowRowsTotals = ShowRowsTotals,
                ShowColumnsTotals = ShowColumnsTotals,
                LogicalOperator = LogicalFilterOperator,
                FilterCaseSensitivity = FilterCaseSensitivity,
                Filter = Filter
            };
        }

        // ---------------------------------------------------------------- 布局

        /// <summary>
        /// 取当前交叉表布局（计算结果，渲染的单一真源）。首次访问或失效后自动重算。
        /// </summary>
        public ElPivotLayout<TItem> GetLayout()
        {
            if (layout != null && !layoutDirty)
            {
                return layout;
            }

            layout = ElPivotEngine.Build(
                Data,
                BuildFieldSpecs(BuildActiveRows()),
                BuildFieldSpecs(BuildActiveColumns()),
                BuildAggregateSpecs(),
                BuildEngineOptions());

            layoutDirty = false;

            return layout;
        }

        /// <summary>
        /// 重新计算并刷新界面。
        /// </summary>
        public Task ReloadAsync()
        {
            Invalidate();

            return RefreshAsync();
        }

        private Task RefreshAsync()
        {
            try
            {
                return InvokeAsync(StateHasChanged);
            }
            catch (InvalidOperationException)
            {
                // 未挂载到渲染器（例如纯单元测试直接调用）：无需刷新。
                return Task.CompletedTask;
            }
        }

        /// <summary>结果总行数（交叉表数据行，不是源记录数）。</summary>
        public int TotalRows
        {
            get { return GetLayout().BodyRows.Count; }
        }

        /// <summary>总页数。</summary>
        public int PageCount
        {
            get
            {
                var size = EffectivePageSize;

                if (size <= 0 || TotalRows == 0)
                {
                    return 1;
                }

                return (int)Math.Ceiling(TotalRows / (double)size);
            }
        }

        /// <summary>当前页（钳制在合法范围内，从 1 开始）。</summary>
        public int CurrentPage
        {
            get
            {
                var requested = currentPageIndex > 0 ? currentPageIndex : PageIndex;
                var count = PageCount;
                return requested < 1 ? 1 : (requested > count ? count : requested);
            }
        }

        /// <summary>实际生效的每页行数。</summary>
        public int EffectivePageSize
        {
            get
            {
                var size = currentPageSize > 0 ? currentPageSize : PageSize;
                return size <= 0 ? 10 : size;
            }
        }

        /// <summary>当前页的数据行。</summary>
        public IReadOnlyList<ElPivotBodyRow<TItem>> GetPagedRows()
        {
            var rows = GetLayout().BodyRows;

            if (!AllowPaging)
            {
                return rows;
            }

            var size = EffectivePageSize;
            var start = (CurrentPage - 1) * size;

            if (start >= rows.Count)
            {
                return Array.Empty<ElPivotBodyRow<TItem>>();
            }

            var take = Math.Min(size, rows.Count - start);
            return rows.GetRange(start, take);
        }

        // ---------------------------------------------------------------- 渲染模型

        /// <summary>
        /// 构造当前页的渲染模型（已按本页重新计算跨行数，避免分页导致 <c>rowspan</c> 溢出）。
        /// </summary>
        public List<ElPivotRenderRow<TItem>> GetRenderRows()
        {
            var rows = GetPagedRows();
            var result = new List<ElPivotRenderRow<TItem>>(rows.Count);
            var identities = new List<string[]>(rows.Count);

            // 行头（左上角固定列）的总列数：表头与合计行都按它占列，数据行必须对齐到同一列数。
            var leadingColumns = FrozenRowHeaderCount;

            for (var i = 0; i < rows.Count; i++)
            {
                identities.Add(BuildIdentities(rows[i]));
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var model = new ElPivotRenderRow<TItem> { Row = rows[i], Index = i };
                var identity = identities[i];

                for (var position = 0; position < rows[i].HeaderCells.Count; position++)
                {
                    var cell = rows[i].HeaderCells[position];
                    var key = position < identity.Length ? identity[position] : null;

                    if (cell.IsPlaceholder || key == null)
                    {
                        model.Cells.Add(new ElPivotRenderCell
                        {
                            Source = cell,
                            Position = position,
                            Skip = true
                        });

                        continue;
                    }

                    var continued = false;

                    if (i > 0)
                    {
                        var previous = identities[i - 1];
                        continued = position < previous.Length && string.Equals(previous[position], key, StringComparison.Ordinal);
                    }

                    if (continued)
                    {
                        model.Cells.Add(new ElPivotRenderCell
                        {
                            Source = cell,
                            Position = position,
                            Skip = true,
                            Identity = key
                        });

                        continue;
                    }

                    var span = 1;

                    for (var k = i + 1; k < rows.Count; k++)
                    {
                        var next = identities[k];

                        if (position < next.Length && string.Equals(next[position], key, StringComparison.Ordinal))
                        {
                            span++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    model.Cells.Add(new ElPivotRenderCell
                    {
                        Source = cell,
                        Position = position,
                        RowSpan = span,
                        Identity = key
                    });
                }

                // 行头总列数取「全部数据行里的最大深度」（即表头左上角的固定列数）。
                // 深度较浅的行（同级分组未展开）末尾的占位单元格不渲染，会让该行少占若干列，
                // 导致数据单元格整体左移 —— 这里让本行最后一个渲染出来的行头单元格横向吃掉这些占位列。
                if (leadingColumns > 0)
                {
                    var lastRendered = -1;

                    for (var c = 0; c < model.Cells.Count; c++)
                    {
                        if (!model.Cells[c].Skip)
                        {
                            lastRendered = c;
                        }
                    }

                    if (lastRendered >= 0)
                    {
                        var span = leadingColumns - model.Cells[lastRendered].Position;

                        if (span > 1)
                        {
                            model.Cells[lastRendered].ColSpan = span;
                        }
                    }
                }

                result.Add(model);
            }

            return result;
        }

        private static string[] BuildIdentities(ElPivotBodyRow<TItem> row)
        {
            var identities = new string[row.HeaderCells.Count];
            var path = row.PathValues;

            for (var position = 0; position < identities.Length; position++)
            {
                if (row.HeaderCells[position].IsPlaceholder)
                {
                    identities[position] = null;
                    continue;
                }

                if (path != null && position < path.Count)
                {
                    var prefix = new List<object>(position + 1);

                    for (var k = 0; k <= position; k++)
                    {
                        prefix.Add(path[k]);
                    }

                    identities[position] = ElPivotEngine.PathKey(prefix);
                }
                else
                {
                    var cell = row.HeaderCells[position];
                    identities[position] = cell.PathKey ?? ElPivotEngine.Normalize(cell.Value);
                }
            }

            return identities;
        }

        // ---------------------------------------------------------------- 表头辅助

        /// <summary>左上角固定行头单元格对应的行字段标题（按下标）。</summary>
        public string GetRowHeaderTitle(int index)
        {
            var fields = BuildActiveRows();
            return index >= 0 && index < fields.Count ? fields[index].GetTitle() : string.Empty;
        }

        /// <summary>左上角固定行头单元格数量。</summary>
        public int FrozenRowHeaderCount
        {
            get
            {
                var depth = GetLayout().RowHeaderDepth;
                return depth > 0 ? depth : BuildActiveRows().Count;
            }
        }

        /// <summary>数据区表头行的跨行数（表头行数 + 1 行值字段名）。</summary>
        public int HeaderRowSpan
        {
            get
            {
                var layoutValue = GetLayout();
                var rows = layoutValue.HeaderRowCount;

                if (ShowAggregateHeaderRowEffective)
                {
                    rows++;
                }

                return rows < 1 ? 1 : rows;
            }
        }

        /// <summary>是否显示值字段名表头行。</summary>
        public bool ShowAggregateHeaderRowEffective
        {
            get { return ShowAggregateHeaderRow && BuildActiveAggregates().Count > 0; }
        }

        /// <summary>网格线对应的 CSS 类。</summary>
        public string GridLinesClass
        {
            get
            {
                switch (GridLines)
                {
                    case ElPivotGridLines.Both: return "el-pivot-grow__table--both";
                    case ElPivotGridLines.None: return "el-pivot-grow__table--none";
                    case ElPivotGridLines.Horizontal: return "el-pivot-grow__table--horizontal";
                    case ElPivotGridLines.Vertical: return "el-pivot-grow__table--vertical";
                    default: return string.Empty;
                }
            }
        }

        /// <summary>值字段文本对齐对应的 CSS 类。</summary>
        public static string GetAlignClass(ElPivotTextAlign align)
        {
            switch (align)
            {
                case ElPivotTextAlign.Left: return "is-left";
                case ElPivotTextAlign.Center: return "is-center";
                default: return "is-right";
            }
        }

        // ---------------------------------------------------------------- 排序

        /// <summary>取某属性的当前排序方向。</summary>
        public ElPivotSortOrder? GetSortOrder(string property)
        {
            return GetEffectiveSortOrder(property);
        }

        /// <summary>取某属性排序图标名。</summary>
        public string GetSortIcon(string property)
        {
            var order = GetEffectiveSortOrder(property);

            if (order == ElPivotSortOrder.Ascending)
            {
                return "sort-asc";
            }

            return order == ElPivotSortOrder.Descending ? "sort-desc" : "sort";
        }

        /// <summary>取某属性的排序无障碍文本。</summary>
        public string GetSortTitle(string property)
        {
            var order = GetEffectiveSortOrder(property);

            if (order == ElPivotSortOrder.Ascending)
            {
                return ResolvedStrings.SortAscendingText;
            }

            if (order == ElPivotSortOrder.Descending)
            {
                return ResolvedStrings.SortDescendingText;
            }

            return ResolvedStrings.SortNoneText;
        }

        /// <summary>该字段当前是否可排序。</summary>
        public bool IsFieldSortable(ElPivotFieldGrow<TItem> field)
        {
            return field != null && AllowSorting && field.Sortable;
        }

        /// <summary>该字段当前是否可筛选。</summary>
        public bool IsFieldFilterable(ElPivotFieldGrow<TItem> field)
        {
            return field != null && AllowFiltering && field.Filterable;
        }

        /// <summary>该属性当前是否已有生效的筛选。</summary>
        public bool HasActiveFilter(string property)
        {
            var filter = GetEffectiveFilter(property);
            return filter != null && !filter.IsEmpty;
        }

        /// <summary>
        /// 点击表头：按 <c>SortOrderSequence</c> 循环切换排序方向。
        /// </summary>
        public async Task ToggleSortAsync(ElPivotFieldGrow<TItem> field)
        {
            if (!IsFieldSortable(field) || string.IsNullOrEmpty(field.Property))
            {
                return;
            }

            var next = field.NextSortOrder(GetEffectiveSortOrder(field.Property));
            sortOverrides[field.Property] = next;

            Invalidate();

            if (SortChanged.HasDelegate)
            {
                await SortChanged.InvokeAsync(new ElPivotSortChangedEventArgs
                {
                    Scope = ResolveScope(field),
                    Property = field.Property,
                    SortOrder = next
                });
            }

            await RefreshAsync();
        }

        private static string ResolveScope(ElPivotFieldGrow<TItem> field)
        {
            if (field is ElPivotColumnGrow<TItem>)
            {
                return "column";
            }

            return field is ElPivotAggregateGrow<TItem> ? "aggregate" : "row";
        }

        // ---------------------------------------------------------------- 筛选面板

        /// <summary>筛选面板是否打开。</summary>
        public bool IsFilterPanelOpen
        {
            get { return filterPanelOpen; }
        }

        /// <summary>当前正在编辑筛选的字段。</summary>
        public ElPivotFieldGrow<TItem> FilterField
        {
            get { return filterField; }
        }

        /// <summary>筛选面板的工作上下文（供模板使用）。</summary>
        public ElPivotFilterContext FilterContext
        {
            get
            {
                if (filterField == null || workingFilter == null)
                {
                    return null;
                }

                return new ElPivotFilterContext
                {
                    Property = workingFilter.Property,
                    Title = filterField.GetTitle(),
                    Type = workingFilter.Type,
                    Operator = workingFilter.Operator,
                    Value = workingFilter.Value,
                    SecondOperator = workingFilter.SecondOperator,
                    SecondValue = workingFilter.SecondValue,
                    LogicalOperator = workingFilter.LogicalOperator,
                    Operators = filterField.GetFilterOperators(),
                    LogicalOperators = new[]
                    {
                        ElPivotLogicalOperator.And,
                        ElPivotLogicalOperator.Or
                    },
                    SetOperator = op =>
                    {
                        workingFilter.Operator = op;

                        if (!ElPivotFilterOperators.RequiresValue(op))
                        {
                            workingFilter.Value = null;
                        }
                    },
                    SetValue = value => workingFilter.Value = value,
                    SetSecondOperator = op =>
                    {
                        workingFilter.SecondOperator = op;

                        if (!ElPivotFilterOperators.RequiresValue(op))
                        {
                            workingFilter.SecondValue = null;
                        }
                    },
                    SetSecondValue = value => workingFilter.SecondValue = value,
                    SetLogicalOperator = op => workingFilter.LogicalOperator = op,
                    Apply = ApplyFilterAsync,
                    Clear = ClearFilterAsync
                };
            }
        }

        /// <summary>打开某字段的筛选面板。</summary>
        public Task OpenFilterAsync(ElPivotFieldGrow<TItem> field)
        {
            if (!IsFieldFilterable(field) || string.IsNullOrEmpty(field.Property))
            {
                return Task.CompletedTask;
            }

            filterField = field;

            var existing = GetEffectiveFilter(field.Property);

            workingFilter = existing != null
                ? existing.Clone()
                : new ElPivotFilterDescriptor
                {
                    Property = field.Property,
                    Type = field.FilterPropertyType,
                    CaseSensitivity = FilterCaseSensitivity
                };

            if (workingFilter.Type == null)
            {
                workingFilter.Type = field.FilterPropertyType;
            }

            filterPanelOpen = true;

            return RefreshAsync();
        }

        /// <summary>关闭筛选面板（不改变已生效的筛选）。</summary>
        public Task CloseFilterPanelAsync()
        {
            filterPanelOpen = false;
            filterField = null;
            workingFilter = null;

            return RefreshAsync();
        }

        /// <summary>把筛选面板中的条件应用到当前字段。</summary>
        public async Task ApplyFilterAsync()
        {
            if (filterField == null || workingFilter == null)
            {
                return;
            }

            var property = workingFilter.Property;
            var descriptor = workingFilter.Clone();

            if (descriptor.IsEmpty)
            {
                filterOverrides.Remove(property);
            }
            else
            {
                filterOverrides[property] = descriptor;
            }

            filterPanelOpen = false;
            filterField = null;
            workingFilter = null;

            Invalidate();
            ResetPage();

            if (FilterChanged.HasDelegate)
            {
                await FilterChanged.InvokeAsync(new ElPivotFilterChangedEventArgs
                {
                    Property = property,
                    Filter = descriptor.IsEmpty ? null : descriptor,
                    IsCleared = descriptor.IsEmpty
                });
            }

            await RefreshAsync();
        }

        /// <summary>清除当前字段的筛选并应用。</summary>
        public async Task ClearFilterAsync()
        {
            if (filterField == null)
            {
                return;
            }

            var property = filterField.Property;
            filterOverrides.Remove(property);
            filterPanelOpen = false;
            filterField = null;
            workingFilter = null;

            Invalidate();
            ResetPage();

            if (FilterChanged.HasDelegate)
            {
                await FilterChanged.InvokeAsync(new ElPivotFilterChangedEventArgs
                {
                    Property = property,
                    Filter = null,
                    IsCleared = true
                });
            }

            await RefreshAsync();
        }

        /// <summary>设置第一个筛选条件的运算符（供外部/测试驱动）。</summary>
        public Task SetFilterOperatorAsync(ElPivotFilterOperator op)
        {
            if (workingFilter == null)
            {
                return Task.CompletedTask;
            }

            workingFilter.Operator = op;

            if (!ElPivotFilterOperators.RequiresValue(op))
            {
                workingFilter.Value = null;
            }

            return RefreshAsync();
        }

        /// <summary>设置第一个筛选条件的比较值。</summary>
        public Task SetFilterValueAsync(object value)
        {
            if (workingFilter == null)
            {
                return Task.CompletedTask;
            }

            workingFilter.Value = value;

            return RefreshAsync();
        }

        /// <summary>设置第二个筛选条件的运算符。</summary>
        public Task SetSecondFilterOperatorAsync(ElPivotFilterOperator op)
        {
            if (workingFilter == null)
            {
                return Task.CompletedTask;
            }

            workingFilter.SecondOperator = op;

            if (!ElPivotFilterOperators.RequiresValue(op))
            {
                workingFilter.SecondValue = null;
            }

            return RefreshAsync();
        }

        /// <summary>设置第二个筛选条件的比较值。</summary>
        public Task SetSecondFilterValueAsync(object value)
        {
            if (workingFilter == null)
            {
                return Task.CompletedTask;
            }

            workingFilter.SecondValue = value;

            return RefreshAsync();
        }

        /// <summary>设置两个筛选条件之间的逻辑运算符。</summary>
        public Task SetLogicalFilterOperatorAsync(ElPivotLogicalOperator op)
        {
            if (workingFilter == null)
            {
                return Task.CompletedTask;
            }

            workingFilter.LogicalOperator = op;

            return RefreshAsync();
        }

        // ---------------------------------------------------------------- 下钻

        /// <summary>
        /// 切换某个分组的展开 / 折叠状态。
        /// </summary>
        /// <param name="pathKey">分组路径键。</param>
        /// <param name="scope"><c>row</c> 或 <c>column</c>。</param>
        public async Task ToggleDrillDownAsync(string pathKey, string scope)
        {
            if (!AllowDrillDown || string.IsNullOrEmpty(pathKey))
            {
                return;
            }

            var current = groupState.TryGetValue(pathKey, out var collapsed)
                ? collapsed
                : !ExpandGroupsByDefault;

            var next = !current;
            groupState[pathKey] = next;

            Invalidate();

            if (DrillDownToggled.HasDelegate)
            {
                await DrillDownToggled.InvokeAsync(new ElPivotDrillDownEventArgs
                {
                    PathKey = pathKey,
                    Scope = scope,
                    Collapsed = next
                });
            }

            if (GroupStateChanged.HasDelegate)
            {
                await GroupStateChanged.InvokeAsync(new ElPivotGroupStateChangedEventArgs
                {
                    GroupState = new Dictionary<string, bool>(groupState, StringComparer.Ordinal)
                });
            }

            await RefreshAsync();
        }

        /// <summary>取某分组的当前折叠状态。</summary>
        public bool IsGroupCollapsed(string pathKey)
        {
            if (!AllowDrillDown || string.IsNullOrEmpty(pathKey))
            {
                return false;
            }

            if (groupState.TryGetValue(pathKey, out var collapsed))
            {
                return collapsed;
            }

            return !ExpandGroupsByDefault;
        }

        /// <summary>展开 / 折叠全部分组（<paramref name="collapsed"/> 为 <c>true</c> 时全部折叠）。</summary>
        public async Task SetAllGroupsAsync(bool collapsed)
        {
            groupState.Clear();

            var layoutValue = GetLayout();

            foreach (var row in layoutValue.HeaderRows)
            {
                foreach (var cell in row.Cells)
                {
                    if (!string.IsNullOrEmpty(cell.PathKey))
                    {
                        groupState[cell.PathKey] = collapsed;
                    }
                }
            }

            Invalidate();

            await RefreshAsync();
        }

        // ---------------------------------------------------------------- 分页

        /// <summary>跳转到指定页。</summary>
        public async Task GoToPageAsync(int page)
        {
            var count = PageCount;
            var target = page < 1 ? 1 : (page > count ? count : page);

            if (target == CurrentPage && currentPageIndex == target)
            {
                return;
            }

            currentPageIndex = target;

            if (PageIndexChanged.HasDelegate)
            {
                await PageIndexChanged.InvokeAsync(target);
            }

            await RefreshAsync();
        }

        /// <summary>改变每页行数并回到第一页。</summary>
        public async Task ChangePageSizeAsync(int size)
        {
            if (size <= 0)
            {
                return;
            }

            currentPageSize = size;
            currentPageIndex = 1;

            if (PageSizeChanged.HasDelegate)
            {
                await PageSizeChanged.InvokeAsync(size);
            }

            await RefreshAsync();
        }

        private void ResetPage()
        {
            currentPageIndex = 1;
        }

        /// <summary>分页器当前应显示的页码（最多 <see cref="PageNumbersCount"/> 个）。</summary>
        public IReadOnlyList<int> GetPageNumbers()
        {
            var count = PageCount;
            var window = PageNumbersCount <= 0 ? 5 : PageNumbersCount;

            if (count <= window)
            {
                var all = new List<int>(count);

                for (var i = 1; i <= count; i++)
                {
                    all.Add(i);
                }

                return all;
            }

            var start = CurrentPage - (window / 2);

            if (start < 1)
            {
                start = 1;
            }

            if (start + window - 1 > count)
            {
                start = count - window + 1;
            }

            var pages = new List<int>(window);

            for (var i = 0; i < window; i++)
            {
                pages.Add(start + i);
            }

            return pages;
        }

        /// <summary>分页摘要文本。</summary>
        public string GetPagerSummary()
        {
            var format = ResolvedStrings.PagerSummaryFormat;

            try
            {
                return string.Format(CultureInfo.CurrentCulture, format, TotalRows, GetPagedRows().Count);
            }
            catch (FormatException)
            {
                return string.Format(CultureInfo.CurrentCulture, "共 {0} 行", TotalRows);
            }
        }

        // ---------------------------------------------------------------- 字段选择

        /// <summary>字段选择器是否展开。</summary>
        public bool IsFieldsPickerExpanded
        {
            get { return pickerExpanded || FieldsPickerExpanded; }
        }

        /// <summary>切换字段选择器展开状态。</summary>
        public Task ToggleFieldsPickerAsync()
        {
            pickerExpanded = !IsFieldsPickerExpanded;
            return RefreshAsync();
        }

        /// <summary>某字段是否在行区域中。</summary>
        public bool IsRowSelected(ElPivotFieldGrow<TItem> field)
        {
            return field != null && ResolveSelectedRows().Contains(field.Property);
        }

        /// <summary>某字段是否在列区域中。</summary>
        public bool IsColumnSelected(ElPivotFieldGrow<TItem> field)
        {
            return field != null && ResolveSelectedColumns().Contains(field.Property);
        }

        /// <summary>某字段是否在值区域中。</summary>
        public bool IsAggregateSelected(ElPivotFieldGrow<TItem> field)
        {
            if (field == null)
            {
                return false;
            }

            foreach (var selection in ResolveSelectedAggregates())
            {
                if (string.Equals(selection.Property, field.Property, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>某字段在值区域中使用的聚合函数。</summary>
        public ElPivotAggregateFunction GetAggregateFunction(string property)
        {
            foreach (var selection in ResolveSelectedAggregates())
            {
                if (string.Equals(selection.Property, property, StringComparison.Ordinal))
                {
                    return selection.Function;
                }
            }

            return ElPivotAggregateFunction.Count;
        }

        /// <summary>某属性类型可用的聚合函数。</summary>
        public IReadOnlyList<ElPivotAggregateFunction> GetAvailableAggregates(ElPivotFieldGrow<TItem> field)
        {
            return ElPivotAggregateFunctions.Available(field == null ? typeof(string) : field.FilterPropertyType);
        }

        /// <summary>在行 / 列 / 值区域中切换某个字段的选中状态。</summary>
        public async Task ToggleFieldSelectionAsync(ElPivotFieldGrow<TItem> field, string scope, bool selected)
        {
            if (field == null || string.IsNullOrEmpty(field.Property))
            {
                return;
            }

            var property = field.Property;

            if (string.Equals(scope, "row", StringComparison.Ordinal))
            {
                var list = ResolveSelectedRows();
                Toggle(list, property, selected);

                if (SelectedRowsChanged.HasDelegate)
                {
                    await SelectedRowsChanged.InvokeAsync(new List<string>(list));
                }

                Invalidate();
                ResetPage();
                await RaiseFieldSelectionAsync("row");
            }
            else if (string.Equals(scope, "column", StringComparison.Ordinal))
            {
                var list = ResolveSelectedColumns();
                Toggle(list, property, selected);

                if (SelectedColumnsChanged.HasDelegate)
                {
                    await SelectedColumnsChanged.InvokeAsync(new List<string>(list));
                }

                Invalidate();
                ResetPage();
                await RaiseFieldSelectionAsync("column");
            }
            else
            {
                var list = ResolveSelectedAggregates();
                var existing = -1;

                for (var i = 0; i < list.Count; i++)
                {
                    if (string.Equals(list[i].Property, property, StringComparison.Ordinal))
                    {
                        existing = i;
                        break;
                    }
                }

                if (selected && existing < 0)
                {
                    list.Add(new ElPivotSelectedField
                    {
                        Property = property,
                        Function = ResolveFunction(property),
                        PropertyType = field.FilterPropertyType
                    });
                }
                else if (!selected && existing >= 0)
                {
                    list.RemoveAt(existing);
                }

                if (SelectedAggregatesChanged.HasDelegate)
                {
                    await SelectedAggregatesChanged.InvokeAsync(BuildSelectedAggregateProperties(list));
                }

                Invalidate();
                ResetPage();
                await RaiseFieldSelectionAsync("aggregate");
            }

            await RefreshAsync();
        }

        private static void Toggle(List<string> list, string property, bool selected)
        {
            var index = list.IndexOf(property);

            if (selected)
            {
                if (index < 0)
                {
                    list.Add(property);
                }
            }
            else if (index >= 0)
            {
                list.RemoveAt(index);
            }
        }

        private static IReadOnlyList<string> BuildSelectedAggregateProperties(List<ElPivotSelectedField> fields)
        {
            var result = new List<string>(fields.Count);

            foreach (var field in fields)
            {
                result.Add(field.Property);
            }

            return result;
        }

        private async Task RaiseFieldSelectionAsync(string scope)
        {
            if (!FieldSelectionChanged.HasDelegate)
            {
                return;
            }

            var properties = string.Equals(scope, "row", StringComparison.Ordinal)
                ? (IReadOnlyList<string>)new List<string>(ResolveSelectedRows())
                : string.Equals(scope, "column", StringComparison.Ordinal)
                    ? new List<string>(ResolveSelectedColumns())
                    : BuildSelectedAggregateProperties(ResolveSelectedAggregates());

            await FieldSelectionChanged.InvokeAsync(new ElPivotFieldSelectionEventArgs
            {
                Scope = scope,
                Properties = properties,
                Aggregates = string.Equals(scope, "aggregate", StringComparison.Ordinal)
                    ? new List<ElPivotSelectedField>(ResolveSelectedAggregates())
                    : Array.Empty<ElPivotSelectedField>()
            });
        }

        /// <summary>改变某个值字段的聚合函数。</summary>
        public async Task ChangeAggregateFunctionAsync(ElPivotFieldGrow<TItem> field, ElPivotAggregateFunction function)
        {
            if (field == null)
            {
                return;
            }

            foreach (var selection in ResolveSelectedAggregates())
            {
                if (string.Equals(selection.Property, field.Property, StringComparison.Ordinal))
                {
                    selection.Function = function;
                }
            }

            Invalidate();

            if (SelectedAggregatesChanged.HasDelegate)
            {
                await SelectedAggregatesChanged.InvokeAsync(BuildSelectedAggregateProperties(ResolveSelectedAggregates()));
            }

            await RaiseFieldSelectionAsync("aggregate");
            await RefreshAsync();
        }

        // ---------------------------------------------------------------- 值单元格

        /// <summary>取某行的某个值单元格的聚合值。</summary>
        public object GetCellValue(ElPivotBodyRow<TItem> row, int leafIndex, int aggregateIndex)
        {
            if (row == null)
            {
                return null;
            }

            var count = BuildActiveAggregates().Count;

            if (count <= 0)
            {
                return null;
            }

            var index = (leafIndex * count) + aggregateIndex;

            return index >= 0 && index < row.ValueCells.Count ? row.ValueCells[index] : null;
        }

        /// <summary>取某行某个值字段的行合计值。</summary>
        public object GetRowTotal(ElPivotBodyRow<TItem> row, int aggregateIndex)
        {
            if (row == null || aggregateIndex < 0 || aggregateIndex >= row.RowTotals.Count)
            {
                return null;
            }

            return row.RowTotals[aggregateIndex];
        }

        /// <summary>取某列叶子上某个值字段的列合计值。</summary>
        public object GetColumnTotal(IReadOnlyList<object> leaf, int aggregateIndex)
        {
            var total = GetLayout().ColumnTotals;

            if (!total.TryGetValue(ElPivotEngine.PathKey(leaf), out var values) || values == null)
            {
                return null;
            }

            return aggregateIndex >= 0 && aggregateIndex < values.Count ? values[aggregateIndex] : null;
        }

        /// <summary>取右下角总计值。</summary>
        public object GetGrandTotal(int aggregateIndex)
        {
            var totals = GetLayout().GrandTotals;

            return aggregateIndex >= 0 && aggregateIndex < totals.Count ? totals[aggregateIndex] : null;
        }

        /// <summary>
        /// 空分组值的占位符。
        /// <para>
        /// 分组键为 <c>null</c>（或空字符串）时，拼出来的路径会出现一个看不见的空段
        /// （例如「华东 /  / 药品」），既不好读、也无法与「只有两级」区分开，因此统一补一个占位符。
        /// </para>
        /// </summary>
        public const string EmptyPathSegmentText = "（空）";

        /// <summary>取某列叶子的分组路径文本（用于无障碍标签、CSV 与测试断言）。</summary>
        public static string DescribePath(IReadOnlyList<object> path)
        {
            if (path == null || path.Count == 0)
            {
                return string.Empty;
            }

            var segments = new List<string>(path.Count);

            foreach (var value in path)
            {
                var text = ElPivotEngine.Normalize(value).Replace("|", " / ");

                segments.Add(text.Length == 0 ? EmptyPathSegmentText : text);
            }

            return string.Join(" / ", segments);
        }

        /// <summary>值单元格被点击。</summary>
        public async Task RaiseCellClickedAsync(ElPivotCellEventArgs<TItem> args)
        {
            if (args != null && CellClicked.HasDelegate)
            {
                await CellClicked.InvokeAsync(args);
            }
        }

        /// <summary>把 <c>change</c> 事件的原始值安全地转成整数（分页大小、页码等）。</summary>
        public static int ToInt(object value)
        {
            if (value is int number)
            {
                return number;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);

            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
        }

        // ---------------------------------------------------------------- 标记回调（表头 / 字段选择 / 单元格）

        /// <summary>字段选择器：行字段勾选变化。</summary>
        public Task OnRowFieldSelectedAsync(ElPivotFieldGrow<TItem> field, ChangeEventArgs args)
        {
            return ToggleFieldSelectionAsync(field, "row", ToBool(args == null ? null : args.Value));
        }

        /// <summary>字段选择器：列字段勾选变化。</summary>
        public Task OnColumnFieldSelectedAsync(ElPivotFieldGrow<TItem> field, ChangeEventArgs args)
        {
            return ToggleFieldSelectionAsync(field, "column", ToBool(args == null ? null : args.Value));
        }

        /// <summary>字段选择器：值字段勾选变化。</summary>
        public Task OnAggregateFieldSelectedAsync(ElPivotFieldGrow<TItem> field, ChangeEventArgs args)
        {
            return ToggleFieldSelectionAsync(field, "aggregate", ToBool(args == null ? null : args.Value));
        }

        /// <summary>列分组表头下钻/折叠。</summary>
        public Task ToggleColumnDrillDownAsync(string pathKey)
        {
            return ToggleDrillDownAsync(pathKey, "column");
        }

        /// <summary>行分组表头下钻/折叠。</summary>
        public Task ToggleRowDrillDownAsync(string pathKey)
        {
            return ToggleDrillDownAsync(pathKey, "row");
        }

        /// <summary>
        /// 解析「行分组 × 列分组叶子」桶内的明细记录（仅在单元格被点击时计算，不影响渲染路径）。
        /// <para>
        /// 当列叶子是 <c>MaxGroups</c> 产生的「其他」分组、或路径无法匹配时，退化为整行记录，
        /// 保证事件参数永不为 <c>null</c>。
        /// </para>
        /// </summary>
        public IReadOnlyList<TItem> ResolveCellItems(ElPivotBodyRow<TItem> row, IReadOnlyList<object> leaf)
        {
            if (row == null)
            {
                return Array.Empty<TItem>();
            }

            var columns = BuildActiveColumns();

            if (columns.Count == 0 || leaf == null || leaf.Count == 0)
            {
                return row.Items;
            }

            var getters = new List<Func<object, object>>(columns.Count);

            foreach (var column in columns)
            {
                getters.Add(ElPivotPropertyAccess.ValueGetter(typeof(TItem), column.Property, null));
            }

            var wanted = ElPivotEngine.PathKey(leaf);
            var matched = new List<TItem>();
            var path = new List<object>(getters.Count);

            foreach (var item in row.Items)
            {
                path.Clear();

                foreach (var getter in getters)
                {
                    path.Add(getter(item));
                }

                if (string.Equals(ElPivotEngine.PathKey(path), wanted, StringComparison.Ordinal))
                {
                    matched.Add(item);
                }
            }

            return matched.Count > 0 ? matched : row.Items;
        }

        /// <summary>值单元格点击（自动带上该桶的明细记录）。</summary>
        public Task RaiseLeafCellClickedAsync(ElPivotBodyRow<TItem> row, IReadOnlyList<object> leaf, int leafIndex, int aggregateIndex, ElPivotAggregateGrow<TItem> aggregate, object value)
        {
            return RaiseCellClickedAsync(new ElPivotCellEventArgs<TItem>
            {
                Row = row,
                RowPath = row == null ? Array.Empty<object>() : row.PathValues,
                ColumnPath = leaf ?? Array.Empty<object>(),
                LeafIndex = leafIndex,
                AggregateIndex = aggregateIndex,
                Aggregate = aggregate,
                Value = value,
                Items = ResolveCellItems(row, leaf)
            });
        }

        /// <summary>行合计单元格点击。</summary>
        public Task RaiseRowTotalClickedAsync(ElPivotBodyRow<TItem> row, int aggregateIndex, ElPivotAggregateGrow<TItem> aggregate, object value)
        {
            return RaiseCellClickedAsync(new ElPivotCellEventArgs<TItem>
            {
                Row = row,
                RowPath = row == null ? Array.Empty<object>() : row.PathValues,
                AggregateIndex = aggregateIndex,
                Aggregate = aggregate,
                Value = value,
                Items = row == null ? Array.Empty<TItem>() : row.Items,
                IsRowTotal = true
            });
        }

        /// <summary>列合计单元格点击。</summary>
        public Task RaiseColumnTotalClickedAsync(IReadOnlyList<object> leaf, int aggregateIndex, ElPivotAggregateGrow<TItem> aggregate, object value)
        {
            var items = new List<TItem>();
            var layout = GetLayout();

            foreach (var row in layout.BodyRows)
            {
                items.AddRange(ResolveCellItems(row, leaf));
            }

            return RaiseCellClickedAsync(new ElPivotCellEventArgs<TItem>
            {
                ColumnPath = leaf ?? Array.Empty<object>(),
                AggregateIndex = aggregateIndex,
                Aggregate = aggregate,
                Value = value,
                Items = items,
                IsColumnTotal = true
            });
        }

        /// <summary>右下角总计单元格点击。</summary>
        public Task RaiseGrandTotalClickedAsync(int aggregateIndex, ElPivotAggregateGrow<TItem> aggregate, object value)
        {
            return RaiseCellClickedAsync(new ElPivotCellEventArgs<TItem>
            {
                AggregateIndex = aggregateIndex,
                Aggregate = aggregate,
                Value = value,
                Items = GetLayout().Items,
                IsGrandTotal = true
            });
        }

        /// <summary>键盘事件：Enter / Space 视为点击（无障碍）。</summary>
        public static bool IsActivationKey(KeyboardEventArgs args)
        {
            if (args == null)
            {
                return false;
            }

            var key = args.Code ?? args.Key;

            return key == "Enter" || key == " " || key == "Space";
        }

        // ---------------------------------------------------------------- 标记回调解析

        /// <summary>把 <c>change</c> 事件的原始值安全地转成布尔（复选框勾选状态）。</summary>
        public static bool ToBool(object value)
        {
            if (value is bool flag)
            {
                return flag;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);

            return string.Equals(text, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "on", StringComparison.OrdinalIgnoreCase)
                || text == "1";
        }

        /// <summary>把 <c>change</c> 事件的原始值解析成聚合函数；无法识别时回退到 <see cref="ElPivotAggregateFunction.Sum"/>。</summary>
        public static ElPivotAggregateFunction ParseAggregateFunction(object value)
        {
            if (value is ElPivotAggregateFunction function)
            {
                return function;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);

            return Enum.TryParse(text, true, out ElPivotAggregateFunction parsed)
                ? parsed
                : ElPivotAggregateFunction.Sum;
        }

        /// <summary>把 <c>change</c> 事件的原始值解析成筛选运算符；无法识别时回退到 <see cref="ElPivotFilterOperator.Contains"/>。</summary>
        public static ElPivotFilterOperator ParseFilterOperator(object value)
        {
            if (value is ElPivotFilterOperator op)
            {
                return op;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);

            return Enum.TryParse(text, true, out ElPivotFilterOperator parsed)
                ? parsed
                : ElPivotFilterOperator.Contains;
        }

        /// <summary>把 <c>change</c> 事件的原始值解析成逻辑运算符；无法识别时回退到「与」。</summary>
        public static ElPivotLogicalOperator ParseLogicalOperator(object value)
        {
            if (value is ElPivotLogicalOperator op)
            {
                return op;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);

            return Enum.TryParse(text, true, out ElPivotLogicalOperator parsed)
                ? parsed
                : ElPivotLogicalOperator.And;
        }

        /// <summary>判断类型是否为枚举（自动剥离 <see cref="Nullable{T}"/>）。</summary>
        public static bool IsEnumType(Type type)
        {
            if (type == null)
            {
                return false;
            }

            return ElPivotPropertyAccess.UnwrapNullable(type).IsEnum;
        }

        /// <summary>取枚举类型的成员名集合（用于内置筛选面板的下拉选项）。</summary>
        public static IReadOnlyList<string> GetEnumOptions(Type type)
        {
            if (!IsEnumType(type))
            {
                return Array.Empty<string>();
            }

            return Enum.GetNames(ElPivotPropertyAccess.UnwrapNullable(type));
        }

        // ---------------------------------------------------------------- 生命周期

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (!string.IsNullOrEmpty(EmptyText) && !string.Equals(EmptyText, emptyTextOverride, StringComparison.Ordinal))
            {
                emptyTextOverride = EmptyText;
            }

            if (PageIndex > 0 && PageIndex != currentPageIndex)
            {
                currentPageIndex = PageIndex;
            }

            if (PageSize > 0 && PageSize != currentPageSize)
            {
                currentPageSize = PageSize;
            }
        }

        /// <inheritdoc />
        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            // 字段在本轮渲染中注册完成 → 布局已失效，补一次渲染让表格真正出现。
            // 注册方法会对已注册的字段提前返回，因此这里不会形成渲染循环。
            if (fieldsDirty)
            {
                fieldsDirty = false;
                Invalidate();
                StateHasChanged();
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
