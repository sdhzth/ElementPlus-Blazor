using System;
using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// 值单元格被点击时的事件参数。
    /// </summary>
    /// <typeparam name="TItem">记录类型。</typeparam>
    public sealed class ElPivotCellEventArgs<TItem>
    {
        /// <summary>该单元格所在的数据行。</summary>
        public ElPivotBodyRow<TItem> Row { get; set; }

        /// <summary>该单元格的行分组路径。</summary>
        public IReadOnlyList<object> RowPath { get; set; } = Array.Empty<object>();

        /// <summary>该单元格的列分组路径。</summary>
        public IReadOnlyList<object> ColumnPath { get; set; } = Array.Empty<object>();

        /// <summary>列叶子下标。</summary>
        public int LeafIndex { get; set; }

        /// <summary>值字段下标。</summary>
        public int AggregateIndex { get; set; }

        /// <summary>对应的值字段。</summary>
        public ElPivotAggregateGrow<TItem> Aggregate { get; set; }

        /// <summary>聚合值。</summary>
        public object Value { get; set; }

        /// <summary>参与该单元格聚合的记录（可用于下钻到明细）。</summary>
        public IReadOnlyList<TItem> Items { get; set; } = Array.Empty<TItem>();

        /// <summary>是否在行合计单元格上触发。</summary>
        public bool IsRowTotal { get; set; }

        /// <summary>是否在列合计单元格上触发。</summary>
        public bool IsColumnTotal { get; set; }

        /// <summary>是否在右下角总计单元格上触发。</summary>
        public bool IsGrandTotal { get; set; }
    }

    /// <summary>
    /// 分组展开 / 折叠时的事件参数。
    /// </summary>
    public sealed class ElPivotDrillDownEventArgs
    {
        /// <summary>分组路径键。</summary>
        public string PathKey { get; set; }

        /// <summary>作用范围：<c>row</c>（行分组）或 <c>column</c>（列分组）。</summary>
        public string Scope { get; set; }

        /// <summary>切换后的状态：<c>true</c> 表示已折叠。</summary>
        public bool Collapsed { get; set; }
    }

    /// <summary>
    /// 字段选择发生变化时的事件参数（行 / 列 / 值区域）。
    /// </summary>
    public sealed class ElPivotFieldSelectionEventArgs
    {
        /// <summary>作用范围：<c>row</c> / <c>column</c> / <c>aggregate</c>。</summary>
        public string Scope { get; set; }

        /// <summary>当前选中的属性路径（按显示顺序）。</summary>
        public IReadOnlyList<string> Properties { get; set; } = Array.Empty<string>();

        /// <summary>当 <see cref="Scope"/> 为 <c>aggregate</c> 时，附带每个值字段的聚合函数。</summary>
        public IReadOnlyList<ElPivotSelectedField> Aggregates { get; set; } = Array.Empty<ElPivotSelectedField>();
    }

    /// <summary>
    /// 排序发生变化时的事件参数。
    /// </summary>
    public sealed class ElPivotSortChangedEventArgs
    {
        /// <summary>作用范围：<c>row</c> / <c>column</c> / <c>aggregate</c>。</summary>
        public string Scope { get; set; }

        /// <summary>属性路径。</summary>
        public string Property { get; set; }

        /// <summary>新的排序方向；<c>null</c> 表示取消排序。</summary>
        public ElPivotSortOrder? SortOrder { get; set; }
    }

    /// <summary>
    /// 筛选发生变化时的事件参数。
    /// </summary>
    public sealed class ElPivotFilterChangedEventArgs
    {
        /// <summary>属性路径。</summary>
        public string Property { get; set; }

        /// <summary>新的筛选条件；为 <c>null</c> 表示已清除。</summary>
        public ElPivotFilterDescriptor Filter { get; set; }

        /// <summary>是否为「清除筛选」操作。</summary>
        public bool IsCleared { get; set; }
    }

    /// <summary>
    /// 分组折叠状态集合发生变化时的事件参数。
    /// </summary>
    public sealed class ElPivotGroupStateChangedEventArgs
    {
        /// <summary>折叠状态快照：键为分组路径键，值为是否折叠。</summary>
        public IReadOnlyDictionary<string, bool> GroupState { get; set; } = new Dictionary<string, bool>(StringComparer.Ordinal);
    }
}
