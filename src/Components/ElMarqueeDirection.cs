using System;

namespace ElementPlus
{
    /// <summary>
    /// <see cref="ElMarqueeGrow"/> 的滚动方向。
    /// </summary>
    public enum ElMarqueeDirection
    {
        /// <summary>从右向左（默认）。</summary>
        RightToLeft,

        /// <summary>从左向右。</summary>
        LeftToRight,

        /// <summary>从下向上。</summary>
        BottomToTop,

        /// <summary>从上向下。</summary>
        TopToBottom
    }

    /// <summary>
    /// <see cref="ElMarqueeGrow"/> 的圆角预设。实际像素可通过 <see cref="ElMarqueeGrow.RoundRadius"/> 覆盖。
    /// </summary>
    public enum ElMarqueeRound
    {
        /// <summary>直角（0）。</summary>
        None,

        /// <summary>小圆角（4px，对齐 Element Plus <c>--el-border-radius-base</c>）。</summary>
        Small,

        /// <summary>大圆角（8px）。</summary>
        Large,

        /// <summary>胶囊形（999px）。</summary>
        Pill
    }
}
