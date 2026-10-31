namespace ElementPlus
{
    /// <summary>进度条颜色渐变的一个停靠点（对齐 EP <c>color</c> 数组形态 <c>{ color, percentage }</c>）。</summary>
    public sealed class ProgressColorStop
    {
        /// <summary>该停靠点的颜色（任意 CSS 颜色字符串）。</summary>
        public string Color { get; set; }

        /// <summary>该停靠点所处的进度百分比（0~100）。</summary>
        public double Percentage { get; set; }
    }
}
