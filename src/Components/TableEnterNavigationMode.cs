namespace ElementPlus
{
    /// <summary>
    /// ElTable 编辑状态下按回车键的跳转方式。
    /// </summary>
    public enum TableEnterNavigationMode
    {
        /// <summary>
        /// 默认：跳到本行下一个可编辑单元格（行内水平移动），列顺序由 <see cref="TableHeader.SortNo"/> 决定。
        /// </summary>
        NextCell = 0,

        /// <summary>
        /// 跳到下一行的同一列（列内垂直移动），适合按列逐条录入的场景。
        /// </summary>
        SameColumnNextRow = 1,
    }
}
