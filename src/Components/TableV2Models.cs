using System;
using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>排序方向（对齐 EP SortOrder：asc / desc）</summary>
    public enum TableV2SortOrder
    {
        None = 0,
        Asc = 1,
        Desc = 2
    }

    /// <summary>排序模型（对齐 EP sortBy：{ key, order }）</summary>
    public class TableV2SortBy
    {
        /// <summary>排序字段（列的 Key 或 Property）</summary>
        public string Key { get; set; }

        /// <summary>排序方向</summary>
        public TableV2SortOrder Order { get; set; } = TableV2SortOrder.None;

        public TableV2SortBy() { }

        public TableV2SortBy(string key, TableV2SortOrder order)
        {
            Key = key;
            Order = order;
        }
    }

    /// <summary>列排序事件参数（对齐 EP column-sort 回调入参）</summary>
    public class TableV2ColumnSortArgs
    {
        /// <summary>被点击的列</summary>
        public TableV2Column Column { get; set; }

        /// <summary>排序键（列的 Key 或 Property）</summary>
        public string Key { get; set; }

        /// <summary>点击后的排序方向（None→Asc→Desc→Asc 循环）</summary>
        public TableV2SortOrder Order { get; set; }
    }

    /// <summary>表头 class 上下文（对齐 EP headerClass 函数入参）</summary>
    public class TableV2HeaderClassContext
    {
        public IReadOnlyList<TableV2Column> Columns { get; set; }

        /// <summary>表头行下标（从 0 开始）</summary>
        public int HeaderIndex { get; set; }
    }

    /// <summary>行 class 上下文（对齐 EP rowClass 函数入参）</summary>
    public class TableV2RowClassContext
    {
        public IReadOnlyList<TableV2Column> Columns { get; set; }

        /// <summary>行数据</summary>
        public object Row { get; set; }

        /// <summary>行下标（扁平化后的绝对下标，从 0 开始）</summary>
        public int RowIndex { get; set; }
    }

    /// <summary>
    /// 扁平化后的行模型：树形数据（children）会被展开/收起展开成线性序列，
    /// 普通数据 Depth 恒为 0。
    /// </summary>
    public class TableV2FlatRow
    {
        /// <summary>原始行数据</summary>
        public object Row { get; set; }

        /// <summary>树层级（0 = 顶层）</summary>
        public int Depth { get; set; }

        /// <summary>行键（rowKey 属性值，缺省回落到行对象本身）</summary>
        public object Key { get; set; }

        /// <summary>是否可展开（有 children 或组件提供了 ExpandContent）</summary>
        public bool Expandable { get; set; }

        /// <summary>当前是否已展开</summary>
        public bool IsExpanded { get; set; }

        /// <summary>在扁平序列中的绝对下标</summary>
        public int Index { get; set; }
    }
}
