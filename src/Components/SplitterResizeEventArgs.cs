using System;
using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// ElSplitter resize / resize-start / resize-end 事件参数
    /// </summary>
    public class SplitterResizeEventArgs : EventArgs
    {
        /// <summary>拖拽条的索引（即发生 resize 的 bar 位置，从 0 开始）</summary>
        public int Index { get; set; }

        /// <summary>所有面板的当前大小（像素）</summary>
        public List<decimal> Sizes { get; set; } = new List<decimal>();
    }
}