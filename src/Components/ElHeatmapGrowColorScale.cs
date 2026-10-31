using System;
using System.Collections.Generic;
using System.Globalization;

namespace ElementPlus
{
    /// <summary>
    /// 热力图颜色刻度（Element Plus 自渲染版，零 Radzen 依赖）。
    /// 提供颜色解析、两端线性插值、分段区间命中、格内文字对比色判定与区间标签格式化，
    /// 全部为纯函数，便于单元测试直接断言。
    /// </summary>
    public static class ElHeatmapGrowColorScale
    {
        /// <summary>线性映射默认最小值颜色。</summary>
        public const string DefaultMinColor = "#f0f0f0";

        /// <summary>线性映射默认最大值颜色。</summary>
        public const string DefaultMaxColor = "#1a9641";

        /// <summary>无法解析颜色时的回退 RGB（与参考实现的默认灰一致）。</summary>
        public const int FallbackChannel = 240;

        /// <summary>分段配色未命中且无回退色时使用的填充。</summary>
        public const string Transparent = "transparent";

        /// <summary>
        /// 解析 <c>#RGB</c> / <c>#RRGGBB</c> / 无 <c>#</c> 前缀的十六进制颜色。
        /// 解析失败时返回回退灰 <c>(240,240,240)</c>。
        /// </summary>
        /// <param name="color">颜色文本。</param>
        /// <returns>RGB 三元组。</returns>
        public static (int R, int G, int B) Parse(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return (FallbackChannel, FallbackChannel, FallbackChannel);
            }

            var text = color.Trim().TrimStart('#');

            if (text.Length == 3)
            {
                text = new string(new[] { text[0], text[0], text[1], text[1], text[2], text[2] });
            }

            if (text.Length == 6 && int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            {
                return ((value >> 16) & 0xFF, (value >> 8) & 0xFF, value & 0xFF);
            }

            return (FallbackChannel, FallbackChannel, FallbackChannel);
        }

        /// <summary>把 <paramref name="t"/> 限制在 <c>[0,1]</c>。</summary>
        /// <param name="t">归一化强度。</param>
        /// <returns>裁剪后的值。</returns>
        public static double Clamp01(double t) => t < 0 ? 0 : (t > 1 ? 1 : t);

        /// <summary>
        /// 在 <paramref name="minColor"/> 与 <paramref name="maxColor"/> 之间按 <paramref name="t"/> 线性插值（通道值截断取整，与参考实现一致）。
        /// </summary>
        /// <param name="minColor">t=0 时的颜色。</param>
        /// <param name="maxColor">t=1 时的颜色。</param>
        /// <param name="t">归一化强度，自动裁剪到 <c>[0,1]</c>。</param>
        /// <returns><c>#RRGGBB</c> 颜色。</returns>
        public static string Interpolate(string minColor, string maxColor, double t)
        {
            t = Clamp01(t);
            var (r1, g1, b1) = Parse(minColor);
            var (r2, g2, b2) = Parse(maxColor);

            var r = (int)(r1 + (r2 - r1) * t);
            var g = (int)(g1 + (g2 - g1) * t);
            var b = (int)(b1 + (b2 - b1) * t);

            return $"#{r:X2}{g:X2}{b:X2}";
        }

        /// <summary>按声明顺序查找命中的分段区间（闭区间，首个命中生效）。</summary>
        /// <param name="ranges">区间列表。</param>
        /// <param name="value">强度值。</param>
        /// <returns>命中的颜色；无命中返回 <c>null</c>。</returns>
        public static string Pick(IList<ElHeatmapColorRange> ranges, double value)
        {
            if (ranges == null)
            {
                return null;
            }

            for (var i = 0; i < ranges.Count; i++)
            {
                var range = ranges[i];
                if (range == null)
                {
                    continue;
                }

                if (value >= range.Min && value <= range.Max)
                {
                    return range.Color;
                }
            }

            return null;
        }

        /// <summary>
        /// 解析单元格的最终填充色：存在分段区间时优先分段（未命中回退 <c>Fill</c> 或透明），
        /// 否则按 <c>MinColor → MaxColor</c> 线性插值（取值范围退化时取中点色）。
        /// </summary>
        /// <param name="model">热力图模型。</param>
        /// <param name="value">强度值。</param>
        /// <param name="min">强度最小值。</param>
        /// <param name="max">强度最大值。</param>
        /// <returns>填充色。</returns>
        public static string Resolve(ElHeatmapModel model, double value, double min, double max)
        {
            if (model == null)
            {
                return Transparent;
            }

            if (model.ColorRanges != null && model.ColorRanges.Count > 0)
            {
                var hit = Pick(model.ColorRanges, value);
                if (!string.IsNullOrEmpty(hit))
                {
                    return hit;
                }

                return string.IsNullOrEmpty(model.Fill) ? Transparent : model.Fill;
            }

            var range = max - min;
            var t = range > 0 ? (value - min) / range : 0.5;

            return Interpolate(
                string.IsNullOrEmpty(model.MinColor) ? DefaultMinColor : model.MinColor,
                string.IsNullOrEmpty(model.MaxColor) ? DefaultMaxColor : model.MaxColor,
                t);
        }

        /// <summary>判断格内数值文本是否需要使用浅色（深色底用白字）。</summary>
        /// <param name="color">单元格填充色。</param>
        /// <returns>需要浅色文字返回 <c>true</c>。</returns>
        public static bool NeedsLightText(string color)
        {
            var (r, g, b) = Parse(color);
            var luminance = 0.299 * r + 0.587 * g + 0.114 * b;
            return luminance < 150;
        }

        /// <summary>格式化分段区间标签（<c>-∞</c> / <c>+∞</c> 特殊显示）。</summary>
        /// <param name="range">区间。</param>
        /// <returns>形如 <c>0 – 10</c> 的文本。</returns>
        public static string FormatRangeLabel(ElHeatmapColorRange range)
        {
            if (range == null)
            {
                return string.Empty;
            }

            var min = double.IsNegativeInfinity(range.Min)
                ? "-∞"
                : range.Min.ToString("R", CultureInfo.InvariantCulture);
            var max = double.IsPositiveInfinity(range.Max)
                ? "+∞"
                : range.Max.ToString("R", CultureInfo.InvariantCulture);

            return $"{min} – {max}";
        }

        /// <summary>按模型格式串格式化强度值（保证与区域设置无关）。</summary>
        /// <param name="model">热力图模型。</param>
        /// <param name="value">强度值。</param>
        /// <returns>文本。</returns>
        public static string FormatValue(ElHeatmapModel model, double value)
        {
            var format = model?.FormatString;

            if (!string.IsNullOrEmpty(format))
            {
                try
                {
                    // 同时支持 "{0:0.0}" 与 "N1" 两种写法。
                    return format.Contains("{0")
                        ? string.Format(CultureInfo.InvariantCulture, format, value)
                        : value.ToString(format, CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    // 非法格式串回退默认格式，避免渲染期抛异常。
                }
            }

            return value.ToString("G", CultureInfo.InvariantCulture);
        }
    }
}
