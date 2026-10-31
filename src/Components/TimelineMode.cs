namespace ElementPlus
{
    /// <summary>
    /// 时间线整体模式（对齐 Element Plus <c>el-timeline</c> 的 <c>mode</c> 属性，自 2.13.1 起）。
    /// 控制时间线（轴线）与内容相对位置；<see cref="TimelineMode.Alternate"/> / <see cref="TimelineMode.AlternateReverse"/> 会左右交替排布。
    /// </summary>
    public enum TimelineMode
    {
        /// <summary>内容在轴线右侧（默认，等同经典时间线布局）。</summary>
        Start = 0,

        /// <summary>内容在轴线左侧（轴线在右）。</summary>
        End = 1,

        /// <summary>内容左右交替，奇数项在右、偶数项在左。</summary>
        Alternate = 2,

        /// <summary>内容左右交替，但与 <see cref="TimelineMode.Alternate"/> 方向相反（奇数项在左、偶数项在右）。</summary>
        AlternateReverse = 3
    }
}
