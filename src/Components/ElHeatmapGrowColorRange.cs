using System;

namespace ElementPlus
{
    /// <summary>
    /// 热力图的分段配色区间（Element Plus 自渲染版，零 Radzen 依赖）。
    /// 语义对齐 Radzen.Blazor 的 <c>SeriesColorRange</c>：强度落入 <see cref="Min"/> ~ <see cref="Max"/> 闭区间的单元格使用 <see cref="Color"/>。
    /// 区间按声明顺序匹配，首个命中者生效；两端支持 ±∞。
    /// </summary>
    public sealed class ElHeatmapColorRange
    {
        /// <summary>区间下界（含）。默认负无穷。</summary>
        public double Min { get; set; } = double.NegativeInfinity;

        /// <summary>区间上界（含）。默认正无穷。</summary>
        public double Max { get; set; } = double.PositiveInfinity;

        /// <summary>命中该区间时使用的填充色（<c>#RGB</c> 或 <c>#RRGGBB</c>）。</summary>
        public string Color { get; set; } = string.Empty;

        /// <summary>创建空的配色区间。</summary>
        public ElHeatmapColorRange()
        {
        }

        /// <summary>创建配色区间。</summary>
        /// <param name="min">区间下界（含）。</param>
        /// <param name="max">区间上界（含）。</param>
        /// <param name="color">填充色。</param>
        public ElHeatmapColorRange(double min, double max, string color)
        {
            Min = min;
            Max = max;
            Color = color;
        }
    }
}
