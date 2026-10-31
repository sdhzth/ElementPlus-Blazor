using System;
using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// 行合计 / 列合计 / 总计单元格的模板上下文。
    /// <para>
    /// 对应参考实现里的 <c>RadzenPivotAggreateContext&lt;T&gt;</c>（注意 Radzen 里该类型名有拼写错误），
    /// 这里改用正确拼写并补充了「是哪一种合计」「行列路径」等信息，便于模板中区分场景。
    /// </para>
    /// </summary>
    /// <typeparam name="TItem">记录类型。</typeparam>
    public sealed class ElPivotAggregateGrowContext<TItem>
    {
        /// <summary>参与该合计计算的记录。</summary>
        public IReadOnlyList<TItem> Items { get; internal set; } = Array.Empty<TItem>();

        /// <summary>对应的值字段。</summary>
        public ElPivotAggregateGrow<TItem> Aggregate { get; internal set; }

        /// <summary>合计值。</summary>
        public object Value { get; internal set; }

        /// <summary>该合计所在的数据行下标（列合计/总计为 <c>null</c>）。</summary>
        public int? Index { get; internal set; }

        /// <summary>该合计所在的行分组路径。</summary>
        public IReadOnlyList<object> RowPath { get; internal set; } = Array.Empty<object>();

        /// <summary>该合计所在的列分组路径（总计为空）。</summary>
        public IReadOnlyList<object> ColumnPath { get; internal set; } = Array.Empty<object>();

        /// <summary>是否为行合计（与行首分组同行的合计）。</summary>
        public bool IsRowTotal { get; internal set; }

        /// <summary>是否为列合计（表格底部 foot 中按列聚合的合计）。</summary>
        public bool IsColumnTotal { get; internal set; }

        /// <summary>是否为右下角总计。</summary>
        public bool IsGrandTotal { get; internal set; }
    }
}
