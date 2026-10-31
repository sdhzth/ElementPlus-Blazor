using System;

namespace ElementPlus
{
    /// <summary>
    /// ElScrollbar EndReached 事件参数 — 滚动到边缘时触发
    /// </summary>
    public class ScrollEndEventArgs : EventArgs
    {
        public ScrollbarDirection Direction { get; set; }
    }
}