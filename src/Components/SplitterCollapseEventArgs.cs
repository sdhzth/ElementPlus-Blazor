using System;

namespace ElementPlus
{
    /// <summary>
    /// ElSplitter collapse 事件参数
    /// </summary>
    public class SplitterCollapseEventArgs : EventArgs
    {
        /// <summary>被折叠面板所在的拖拽条索引</summary>
        public int Index { get; set; }

        /// <summary>是否折叠（true=折叠，false=展开）</summary>
        public bool Collapsed { get; set; }
    }
}