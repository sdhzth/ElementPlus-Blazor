using System;
using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    /// <summary>
    /// 列字段。放在 <see cref="ElPivotDataGridGrow{TItem}"/> 的 <c>Columns</c> 片段中。
    /// <para>对应参考实现的 <c>RadzenPivotColumn&lt;TItem&gt;</c>。</para>
    /// </summary>
    /// <typeparam name="TItem">记录类型。</typeparam>
    public partial class ElPivotColumnGrow<TItem> : ElPivotFieldGrow<TItem>
    {
        /// <summary>列宽，原样写入表头单元格的 <c>style="width:…"</c>。</summary>
        [Parameter]
        public string Width { get; set; }

        /// <summary>
        /// 该层级最多显示的分组数。超出部分按「已设置排序的聚合值（否则第一个聚合）」降序取前 N 组，
        /// 其余记录合并为一个「其他」分组。为 <c>null</c> 或 <c>&lt;= 0</c> 表示不限制。
        /// </summary>
        [Parameter]
        public int? MaxGroups { get; set; }

        /// <summary>「其他」分组的标题；为 <c>null</c> 时使用网格的 <c>OthersText</c>。</summary>
        [Parameter]
        public string OthersLabel { get; set; }

        /// <summary>列头单元格的内容替换模板。参数为该分组的上下文。</summary>
        [Parameter]
        public RenderFragment<ElPivotGroupContext> HeaderTemplate { get; set; }

        internal override void Register(ElPivotDataGridGrow<TItem> grid)
        {
            if (grid != null)
            {
                grid.RegisterColumn(this);
            }
        }

        internal override void Unregister(ElPivotDataGridGrow<TItem> grid)
        {
            if (grid != null)
            {
                grid.UnregisterColumn(this);
            }
        }
    }
}
