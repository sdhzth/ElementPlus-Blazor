using System;
using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    /// <summary>
    /// 行字段。放在 <see cref="ElPivotDataGridGrow{TItem}"/> 的 <c>Rows</c> 片段中。
    /// <para>对应参考实现的 <c>RadzenPivotRow&lt;TItem&gt;</c>。</para>
    /// </summary>
    /// <typeparam name="TItem">记录类型。</typeparam>
    public partial class ElPivotRowGrow<TItem> : ElPivotFieldGrow<TItem>
    {
        /// <summary>
        /// 该层级最多显示的分组数。语义与 <see cref="ElPivotColumnGrow{TItem}.MaxGroups"/> 相同。
        /// </summary>
        [Parameter]
        public int? MaxGroups { get; set; }

        /// <summary>「其他」分组的标题；为 <c>null</c> 时使用网格的 <c>OthersText</c>。</summary>
        [Parameter]
        public string OthersLabel { get; set; }

        internal override void Register(ElPivotDataGridGrow<TItem> grid)
        {
            if (grid != null)
            {
                grid.RegisterRow(this);
            }
        }

        internal override void Unregister(ElPivotDataGridGrow<TItem> grid)
        {
            if (grid != null)
            {
                grid.UnregisterRow(this);
            }
        }
    }
}
