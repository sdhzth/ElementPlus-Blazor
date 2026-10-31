using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// 渲染用的行头单元格：在布局计算结果之上补充**「本页内」**的跨行数。
    /// <para>
    /// 为什么不直接用 <see cref="ElPivotRowHeaderCell.RowSpan"/>：分页时一个分组可能被切到两页，
    /// 引擎算出的跨行数会超出本页行数，造成 <c>rowspan</c> 溢出（表格错位）。
    /// 因此渲染前统一按「当前页」重新计算跨行数，未分页时结果与引擎一致。
    /// </para>
    /// </summary>
    public sealed class ElPivotRenderCell
    {
        /// <summary>来源行头单元格。</summary>
        public ElPivotRowHeaderCell Source { get; set; }

        /// <summary>在行头中的列位置（从 0 开始）。</summary>
        public int Position { get; set; }

        /// <summary>本页内的跨行数（仅为 <c>1</c> 时表示单行分组）。</summary>
        public int RowSpan { get; set; } = 1;

        /// <summary>
        /// 横向跨越的列数。
        /// <para>
        /// 行分组树是「部分下钻」的：把某个分组展开后，行头列数取**全部数据行里的最大深度**，
        /// 深度较浅的行（未展开的同级分组）末尾会留下占位单元格，而占位单元格不渲染。
        /// 若不给这些行补上横向跨度，该行就比表头少占若干列，**数据单元格会整体左移**
        /// （表现为某个值跑到左边一列的列头下面）。
        /// 因此由本行最后一个实际渲染的行头单元格横向吃掉这些占位列，把数据区对齐回表头。
        /// </para>
        /// </summary>
        public int ColSpan { get; set; } = 1;

        /// <summary>是否被上方单元格的 <c>rowspan</c> 覆盖，因而不需要输出。</summary>
        public bool Skip { get; set; }

        /// <summary>该单元格在行分组树中的稳定标识（用于判断相邻行是否属于同一分组）。</summary>
        public string Identity { get; set; }
    }

    /// <summary>
    /// 渲染用的数据行。
    /// </summary>
    /// <typeparam name="TItem">记录类型。</typeparam>
    public sealed class ElPivotRenderRow<TItem>
    {
        /// <summary>对应的布局数据行。</summary>
        public ElPivotBodyRow<TItem> Row { get; set; }

        /// <summary>在本页中的下标（从 0 开始），用于斑马纹。</summary>
        public int Index { get; set; }

        /// <summary>行头单元格（含被覆盖的占位项，渲染时需要按 <see cref="ElPivotRenderCell.Skip"/> 跳过）。</summary>
        public List<ElPivotRenderCell> Cells { get; } = new List<ElPivotRenderCell>();
    }
}
