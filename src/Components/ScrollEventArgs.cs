using System;

namespace ElementPlus
{
    /// <summary>
    /// ElScrollbar OnScroll 事件参数 — 返回当前滚动距离
    /// </summary>
    public class ScrollEventArgs : EventArgs
    {
        public decimal ScrollTop { get; set; }
        public decimal ScrollLeft { get; set; }
        public decimal ScrollHeight { get; set; }
        public decimal ScrollWidth { get; set; }
        public decimal ClientHeight { get; set; }
        public decimal ClientWidth { get; set; }

        /// <summary>距离顶部的百分比（0~100）</summary>
        public decimal TopPercentage { get; set; }
        /// <summary>距离左侧的百分比（0~100）</summary>
        public decimal LeftPercentage { get; set; }
    }
}