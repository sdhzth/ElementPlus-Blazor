using System;
using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    /// <summary>
    /// <see cref="ElPivotDataGridGrow{TItem}"/> 的行 / 列 / 值字段的公共基类。
    /// <para>
    /// 对应参考实现的 <c>RadzenPivotField&lt;TItem&gt;</c>。参数表刻意与之保持同名同义，
    /// 便于从 Radzen 迁移时逐项对照。
    /// </para>
    /// <para>
    /// <b>交互状态不在这里</b>：排序方向、筛选值这类「用户点出来的」状态由网格统一持有
    /// （见 <see cref="ElPivotDataGridGrow{TItem}"/> 的 <c>GetEffectiveSortOrder</c> /
    /// <c>GetEffectiveFilter</c>）。本类只承载**声明式初始配置**，因此字段组件是无状态且可自由重渲染的，
    /// 参考实现里那套「用 <c>SetParametersAsync</c> 检测参数变化 + 内部影子字段」的复杂写法不再需要。
    /// </para>
    /// </summary>
    /// <typeparam name="TItem">记录类型。</typeparam>
    public abstract class ElPivotFieldGrow<TItem> : ComponentBase, IDisposable
    {
        /// <summary>属性路径。支持 <c>.</c> 分隔的嵌套路径，也支持字典键访问。</summary>
        [Parameter]
        public string Property { get; set; }

        /// <summary>显示标题；为 <c>null</c> 时回退到 <see cref="Property"/>。</summary>
        [Parameter]
        public string Title { get; set; }

        /// <summary>
        /// 属性类型。仅当无法从 <see cref="Property"/> 反射出类型时（例如动态字典数据）才需要显式指定；
        /// 显式指定优先。
        /// </summary>
        [Parameter]
        public Type Type { get; set; }

        /// <summary>初始排序方向；<c>null</c> 表示按首次出现顺序。</summary>
        [Parameter]
        public ElPivotSortOrder? SortOrder { get; set; }

        /// <summary>
        /// 交互式排序时循环切换的顺序序列，默认为「升序 → 降序 → 不排序」。
        /// </summary>
        [Parameter]
        public ElPivotSortOrder?[] SortOrderSequence { get; set; } = new ElPivotSortOrder?[]
        {
            ElPivotSortOrder.Ascending,
            ElPivotSortOrder.Descending,
            null
        };

        /// <summary>是否允许点击表头排序。</summary>
        [Parameter]
        public bool Sortable { get; set; } = true;

        /// <summary>
        /// 初始是否处于启用状态（出现在行/列/值区域中）。
        /// 为 <c>false</c> 时该字段只出现在字段选择器里，不参与初始计算。
        /// </summary>
        [Parameter]
        public bool Selected { get; set; } = true;

        /// <summary>是否允许该字段筛选。</summary>
        [Parameter]
        public bool Filterable { get; set; } = true;

        /// <summary>初始的第一个筛选值。</summary>
        [Parameter]
        public object FilterValue { get; set; }

        /// <summary>初始的第二个筛选值。</summary>
        [Parameter]
        public object SecondFilterValue { get; set; }

        /// <summary>初始的第一个筛选运算符。</summary>
        [Parameter]
        public ElPivotFilterOperator FilterOperator { get; set; } = ElPivotFilterOperator.Equals;

        /// <summary>初始的第二个筛选运算符。</summary>
        [Parameter]
        public ElPivotFilterOperator SecondFilterOperator { get; set; } = ElPivotFilterOperator.Equals;

        /// <summary>连接两个初始筛选条件的逻辑运算符。</summary>
        [Parameter]
        public ElPivotLogicalOperator LogicalFilterOperator { get; set; } = ElPivotLogicalOperator.And;

        /// <summary>筛选面板的整体替换模板。</summary>
        [Parameter]
        public RenderFragment<ElPivotFilterContext> FilterTemplate { get; set; }

        /// <summary>第一个筛选值输入控件的替换模板。</summary>
        [Parameter]
        public RenderFragment<ElPivotFilterContext> FilterValueTemplate { get; set; }

        /// <summary>第二个筛选值输入控件的替换模板。</summary>
        [Parameter]
        public RenderFragment<ElPivotFilterContext> SecondFilterValueTemplate { get; set; }

        /// <summary>父网格（由 <see cref="ElPivotDataGridGrow{TItem}"/> 级联提供）。</summary>
        [CascadingParameter]
        public ElPivotDataGridGrow<TItem> PivotGrid { get; set; }

        /// <summary>
        /// 解析后的显示标题。
        /// </summary>
        public string GetTitle()
        {
            return string.IsNullOrEmpty(Title) ? Property : Title;
        }

        /// <summary>
        /// 用于筛选/聚合的属性类型：<see cref="Type"/> 优先，其次按 <see cref="Property"/> 反射。
        /// </summary>
        public Type FilterPropertyType
        {
            get
            {
                if (Type != null)
                {
                    return Type;
                }

                return string.IsNullOrEmpty(Property)
                    ? typeof(string)
                    : ElPivotPropertyAccess.GetPropertyType(typeof(TItem), Property) ?? typeof(string);
            }
        }

        /// <summary>
        /// 该字段属性类型可用的筛选运算符集合。
        /// </summary>
        public System.Collections.Generic.List<ElPivotFilterOperator> GetFilterOperators()
        {
            return ElPivotFilterOperators.For(FilterPropertyType);
        }

        /// <summary>
        /// 按 <see cref="SortOrderSequence"/> 从当前排序方向推出下一个排序方向。
        /// </summary>
        public ElPivotSortOrder? NextSortOrder(ElPivotSortOrder? current)
        {
            var sequence = SortOrderSequence;

            if (sequence == null || sequence.Length == 0)
            {
                return current == ElPivotSortOrder.Ascending ? ElPivotSortOrder.Descending : ElPivotSortOrder.Ascending;
            }

            for (var i = 0; i < sequence.Length; i++)
            {
                if (sequence[i] == current)
                {
                    return sequence[(i + 1) % sequence.Length];
                }
            }

            return sequence[0];
        }

        /// <summary>
        /// 由声明式参数构造初始筛选条件；没有任何有效条件时返回 <c>null</c>。
        /// </summary>
        public ElPivotFilterDescriptor BuildInitialFilter()
        {
            var hasValue = FilterValue != null || !ElPivotFilterOperators.RequiresValue(FilterOperator);
            var hasSecond = SecondFilterValue != null || !ElPivotFilterOperators.RequiresValue(SecondFilterOperator);

            if (!hasValue && !hasSecond)
            {
                return null;
            }

            return new ElPivotFilterDescriptor
            {
                Property = Property,
                Operator = FilterOperator,
                Value = FilterValue,
                SecondOperator = SecondFilterOperator,
                SecondValue = SecondFilterValue,
                LogicalOperator = LogicalFilterOperator,
                Type = FilterPropertyType
            };
        }

        /// <summary>
        /// 注册到父网格（子类分别注册为行字段 / 列字段 / 值字段）。
        /// </summary>
        internal abstract void Register(ElPivotDataGridGrow<TItem> grid);

        /// <summary>
        /// 从父网格注销。
        /// </summary>
        internal abstract void Unregister(ElPivotDataGridGrow<TItem> grid);

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            if (PivotGrid != null)
            {
                Register(PivotGrid);
            }
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            if (PivotGrid != null)
            {
                Unregister(PivotGrid);
            }

            GC.SuppressFinalize(this);
        }
    }
}
