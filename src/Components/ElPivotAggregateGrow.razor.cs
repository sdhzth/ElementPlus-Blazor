using System;
using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    /// <summary>
    /// 值字段（度量）。放在 <see cref="ElPivotDataGridGrow{TItem}"/> 的 <c>Aggregates</c> 片段中。
    /// <para>对应参考实现的 <c>RadzenPivotAggregate&lt;TItem&gt;</c>。</para>
    /// </summary>
    /// <typeparam name="TItem">记录类型。</typeparam>
    public partial class ElPivotAggregateGrow<TItem> : ElPivotFieldGrow<TItem>
    {
        /// <summary>聚合函数，默认 <see cref="ElPivotAggregateFunction.Sum"/>（与参考实现一致）。</summary>
        [Parameter]
        public ElPivotAggregateFunction Aggregate { get; set; } = ElPivotAggregateFunction.Sum;

        /// <summary>值单元格的文本对齐方式，默认右对齐（与参考实现一致）。</summary>
        [Parameter]
        public ElPivotTextAlign TextAlign { get; set; } = ElPivotTextAlign.Right;

        /// <summary>值格式化字符串，例如 <c>N2</c> 或 <c>P0</c>。</summary>
        [Parameter]
        public string FormatString { get; set; }

        /// <summary>格式化使用的区域信息；为 <c>null</c> 时使用 <c>CultureInfo.CurrentCulture</c>。</summary>
        [Parameter]
        public IFormatProvider FormatProvider { get; set; }

        /// <summary>值单元格的内容替换模板。参数为聚合后的值。</summary>
        [Parameter]
        public RenderFragment<object> Template { get; set; }

        /// <summary>列合计单元格的内容替换模板。参数为合计值。</summary>
        [Parameter]
        public RenderFragment<object> ColumnTotalTemplate { get; set; }

        /// <summary>行合计单元格的内容替换模板。参数为行合计上下文。</summary>
        [Parameter]
        public RenderFragment<ElPivotAggregateGrowContext<TItem>> RowTotalTemplate { get; set; }

        /// <summary>值列表头的替换模板（表头行中该值字段的名字）。</summary>
        [Parameter]
        public RenderFragment HeaderTemplate { get; set; }

        /// <summary>
        /// 把聚合值格式化为显示文本（<see cref="FormatString"/> 优先，否则直接 <c>ToString()</c>）。
        /// </summary>
        public string FormatValue(object value)
        {
            return ElPivotAggregator.FormatValue(value, FormatString, FormatProvider);
        }

        /// <summary>
        /// 该值字段在表头/字段选择器中使用的默认标题（<c>标题 (函数名)</c>）。
        /// </summary>
        public string GetAggregateTitle()
        {
            var title = GetTitle();

            if (string.IsNullOrEmpty(title))
            {
                return ElPivotAggregateFunctions.DisplayName(Aggregate);
            }

            return string.Concat(title, " (", ElPivotAggregateFunctions.DisplayName(Aggregate), ")");
        }

        internal override void Register(ElPivotDataGridGrow<TItem> grid)
        {
            if (grid != null)
            {
                grid.RegisterAggregate(this);
            }
        }

        internal override void Unregister(ElPivotDataGridGrow<TItem> grid)
        {
            if (grid != null)
            {
                grid.UnregisterAggregate(this);
            }
        }
    }
}
