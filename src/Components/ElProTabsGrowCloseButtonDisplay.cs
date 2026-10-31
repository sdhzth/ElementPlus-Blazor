namespace ElementPlus
{
    /// <summary>
    /// <see cref="ElProTabsGrow"/> 关闭按钮（X）的显示时机。
    /// </summary>
    public enum ElProTabsGrowCloseButtonDisplay
    {
        /// <summary>
        /// 鼠标移到该页签上、或该页签获得键盘焦点时才显示（默认）。
        /// 未显示时用 <c>visibility:hidden</c> 真正移出命中区，避免出现「看不见却能点到」的幽灵按钮。
        /// </summary>
        Hover = 0,

        /// <summary>始终显示，仅悬停时给 X 加一层灰色底衬。</summary>
        Always = 1
    }
}
