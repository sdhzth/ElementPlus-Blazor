namespace ElementPlus
{
    /// <summary>
    /// 日期面板在「范围选择」里的角色（对齐 EP <c>el-date-range-picker__content.is-left / is-right</c>）。
    /// <para>
    /// 范围模式下会并排渲染两个日期面板，左右各管一个月份，并且**各自只保留自己那一侧的翻页箭头**：
    /// 左面板 <c>«  ‹</c> 在标签左侧，右面板 <c>›  »</c> 在标签右侧；
    /// 单面板（<see cref="None"/>）保持原有「两侧都有箭头 + 清除按钮」的布局。
    /// </para>
    /// </summary>
    public enum DatePickerRangeRole
    {
        /// <summary>非范围面板（默认）：两侧箭头齐全，可点标签切换年 / 月视图。</summary>
        None = 0,

        /// <summary>范围起始面板：只显示上一月 / 上一年箭头，位于标签左侧。</summary>
        Start = 1,

        /// <summary>范围结束面板：只显示下一月 / 下一年箭头，位于标签右侧。</summary>
        End = 2
    }
}
