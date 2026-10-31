namespace ElementPlus
{
    public enum PaginationLayoutPart
    {
        Prev = 0,
        Pager = 1,
        Next = 2,
        Jumper = 3,
        Total = 4,
        Sizes = 5,
        /// <summary>`->` 布局标记：其后的所有布局元素整体靠右对齐（EP layout 分隔符）。</summary>
        RightWrapper = 6,
        /// <summary>`slot` 布局标记：渲染自定义内容（EP 的 default 插槽，对应 SlotContent）。</summary>
        Slot = 7
    }
}
