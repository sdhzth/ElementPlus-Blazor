using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ElementPlus
{
    /// <summary>
    /// 热力图布局与渲染引擎（Element Plus 自渲染版，零 Radzen 依赖、零 JS、零第三方图表库）。
    /// <para>
    /// 参考 Radzen.Blazor 的 <c>RadzenHeatmap</c>（类别网格）与 <c>RadzenHeatmapSeries</c>（轴单位网格 + 分段配色）两种形态，
    /// 但所有几何与 SVG 标记均在本文件内以纯 C# 计算：<see cref="ComputeLayout"/> 产出结构化布局（供测试断言与渲染共用），
    /// <see cref="RenderSvg"/> 产出完整 SVG 字符串，屏幕渲染与导出因此共享唯一的布局真源。
    /// </para>
    /// </summary>
    public static class ElHeatmapGrowEngine
    {
        /// <summary>格内数值可绘制的最小单元格宽度（像素）。</summary>
        public const double MinLabelWidth = 20;

        /// <summary>格内数值可绘制的最小单元格高度（像素）。</summary>
        public const double MinLabelHeight = 14;

        /// <summary>线性色阶图例的档数。</summary>
        public const int GradientLegendSteps = 5;

        // ---------------------------------------------------------------- 布局

        /// <summary>
        /// 计算热力图布局：解析轴序列、单元格像素矩形、刻度位置与色阶图例。
        /// </summary>
        /// <param name="model">热力图模型。</param>
        /// <returns>结构化布局结果。</returns>
        public static ElHeatmapLayout ComputeLayout(ElHeatmapModel model)
        {
            var m = model ?? new ElHeatmapModel();

            var layout = new ElHeatmapLayout
            {
                Width = m.Width > 0 ? m.Width : 600,
                Height = m.Height > 0 ? m.Height : 360,
                EmptyText = string.IsNullOrEmpty(m.EmptyText) ? "暂无数据" : m.EmptyText,
                IsNumeric = m.NumericAxes
            };

            var source = (m.Cells ?? new List<ElHeatmapCell>())
                .Where(c => c != null && c.IsDefined)
                .ToList();

            if (source.Count == 0)
            {
                layout.IsEmpty = true;
                layout.PlotLeft = 0;
                layout.PlotTop = 0;
                layout.PlotRight = layout.Width;
                layout.PlotBottom = layout.Height;
                return layout;
            }

            // 强度范围（决定颜色映射）
            var minValue = source.Min(c => c.Value);
            var maxValue = source.Max(c => c.Value);
            layout.MinValue = minValue;
            layout.MaxValue = maxValue;
            layout.ColorMin = minValue;
            layout.ColorMax = maxValue;

            foreach (var cell in source)
            {
                var copy = Clone(cell);
                copy.Color = ElHeatmapGrowColorScale.Resolve(m, copy.Value, minValue, maxValue);
                copy.ValueText = ElHeatmapGrowColorScale.FormatValue(m, copy.Value);
                copy.LightText = ElHeatmapGrowColorScale.NeedsLightText(copy.Color);
                layout.Cells.Add(copy);
            }

            // 轴序列与坐标范围
            double xLo;
            double xHi;
            double yLo;
            double yHi;

            if (!m.NumericAxes)
            {
                layout.XCategories = DistinctOrdered(layout.Cells.Select(c => c.XLabel));
                layout.YCategories = DistinctOrdered(layout.Cells.Select(c => c.YLabel));

                foreach (var cell in layout.Cells)
                {
                    cell.XIndex = layout.XCategories.IndexOf(cell.XLabel);
                    cell.YIndex = layout.YCategories.IndexOf(cell.YLabel);
                }

                xLo = 0;
                xHi = Math.Max(1, layout.XCategories.Count);
                yLo = 0;
                yHi = Math.Max(1, layout.YCategories.Count);
            }
            else
            {
                layout.XValues = DistinctSorted(layout.Cells.Select(c => c.X));
                layout.YValues = DistinctSorted(layout.Cells.Select(c => c.Y));

                var stepX = m.CellWidth > 0 ? m.CellWidth : InferStep(layout.XValues);
                var stepY = m.CellHeight > 0 ? m.CellHeight : InferStep(layout.YValues);
                layout.CellStepX = stepX;
                layout.CellStepY = stepY;

                // 数值坐标：以单元格中心定位，两端各扩展半格，保证最外侧单元格完整可见。
                xLo = layout.XValues[0] - stepX / 2;
                xHi = layout.XValues[layout.XValues.Count - 1] + stepX / 2;
                yLo = layout.YValues[0] - stepY / 2;
                yHi = layout.YValues[layout.YValues.Count - 1] + stepY / 2;
            }

            // 边距（按标签/标题/色阶的实际需要预留）
            var top = string.IsNullOrEmpty(m.Title) ? 12.0 : 34.0;
            var left = (m.ShowYLabels ? 64.0 : 12.0) + (string.IsNullOrEmpty(m.YTitle) ? 0.0 : 16.0);
            var right = m.ShowColorScale ? 92.0 : 16.0;
            var bottom = (m.ShowXLabels ? 40.0 : 12.0) + (string.IsNullOrEmpty(m.XTitle) ? 0.0 : 16.0);

            layout.PlotLeft = left;
            layout.PlotTop = top;
            layout.PlotRight = Math.Max(left + 10, layout.Width - right);
            layout.PlotBottom = Math.Max(top + 10, layout.Height - bottom);

            var plotWidth = layout.PlotRight - layout.PlotLeft;
            var plotHeight = layout.PlotBottom - layout.PlotTop;

            // 单元格像素几何
            if (!m.NumericAxes)
            {
                var bandX = plotWidth / Math.Max(1, layout.XCategories.Count);
                var bandY = plotHeight / Math.Max(1, layout.YCategories.Count);
                layout.CellStepX = bandX;
                layout.CellStepY = bandY;

                var pad = Math.Max(0, m.CellPadding);

                foreach (var cell in layout.Cells)
                {
                    var originX = layout.PlotLeft + Math.Max(0, cell.XIndex) * bandX;
                    var originY = layout.PlotTop + Math.Max(0, cell.YIndex) * bandY;

                    cell.Left = originX + pad;
                    cell.Top = originY + pad;
                    cell.Width = Math.Max(0, bandX - 2 * pad);
                    cell.Height = Math.Max(0, bandY - 2 * pad);
                    cell.CenterX = originX + bandX / 2;
                    cell.CenterY = originY + bandY / 2;
                }
            }
            else
            {
                var spanX = xHi - xLo;
                var spanY = yHi - yLo;

                foreach (var cell in layout.Cells)
                {
                    var x1 = ToPixelX(cell.X - layout.CellStepX / 2, layout.PlotLeft, xLo, spanX, plotWidth);
                    var x2 = ToPixelX(cell.X + layout.CellStepX / 2, layout.PlotLeft, xLo, spanX, plotWidth);
                    // 数值 Y 轴向上：值越大越高。
                    var y1 = ToPixelY(cell.Y + layout.CellStepY / 2, layout.PlotTop, yLo, spanY, plotHeight);
                    var y2 = ToPixelY(cell.Y - layout.CellStepY / 2, layout.PlotTop, yLo, spanY, plotHeight);

                    cell.Left = Math.Min(x1, x2);
                    cell.Width = Math.Abs(x2 - x1);
                    cell.Top = Math.Min(y1, y2);
                    cell.Height = Math.Abs(y2 - y1);
                    cell.CenterX = (x1 + x2) / 2;
                    cell.CenterY = (y1 + y2) / 2;
                }
            }

            // 格内数值仅在格子足够大时绘制
            foreach (var cell in layout.Cells)
            {
                cell.ShowValue = m.ShowValues
                    && cell.Width > MinLabelWidth
                    && cell.Height > MinLabelHeight;
            }

            // 轴刻度
            if (!m.NumericAxes)
            {
                for (var i = 0; i < layout.XCategories.Count; i++)
                {
                    layout.XTicks.Add(new ElHeatmapTick
                    {
                        Label = layout.XCategories[i],
                        Value = i,
                        X = layout.PlotLeft + (i + 0.5) * layout.CellStepX,
                        Y = layout.PlotBottom + 18
                    });
                }

                for (var i = 0; i < layout.YCategories.Count; i++)
                {
                    layout.YTicks.Add(new ElHeatmapTick
                    {
                        Label = layout.YCategories[i],
                        Value = i,
                        X = layout.PlotLeft - 8,
                        Y = layout.PlotTop + (i + 0.5) * layout.CellStepY
                    });
                }
            }
            else
            {
                foreach (var value in layout.XValues)
                {
                    layout.XTicks.Add(new ElHeatmapTick
                    {
                        Label = Fmt(value),
                        Value = value,
                        X = ToPixelX(value, layout.PlotLeft, xLo, xHi - xLo, plotWidth),
                        Y = layout.PlotBottom + 18
                    });
                }

                foreach (var value in layout.YValues)
                {
                    layout.YTicks.Add(new ElHeatmapTick
                    {
                        Label = Fmt(value),
                        Value = value,
                        X = layout.PlotLeft - 8,
                        Y = ToPixelY(value, layout.PlotTop, yLo, yHi - yLo, plotHeight)
                    });
                }
            }

            // 色阶图例（分段优先，否则线性等分色阶）
            if (m.ShowColorScale)
            {
                var items = m.ColorRanges != null && m.ColorRanges.Count > 0
                    ? m.ColorRanges
                        .Where(r => r != null)
                        .Select(r => new ElHeatmapLegendItem
                        {
                            Label = ElHeatmapGrowColorScale.FormatRangeLabel(r),
                            Color = r.Color ?? string.Empty
                        })
                        .ToList()
                    : BuildGradientLegend(m, minValue, maxValue);

                layout.LegendItems = items;

                var size = 14.0;
                var gap = 4.0;
                var y = layout.PlotTop;
                foreach (var item in items)
                {
                    item.X = layout.PlotRight + 12;
                    item.Y = y;
                    item.Size = size;
                    y += size + gap;
                }
            }

            return layout;
        }

        /// <summary>构建线性色阶图例（<see cref="GradientLegendSteps"/> 档等分；取值范围退化时只给 1 档）。</summary>
        private static List<ElHeatmapLegendItem> BuildGradientLegend(ElHeatmapModel m, double min, double max)
        {
            var items = new List<ElHeatmapLegendItem>();
            var range = max - min;

            if (range <= 0)
            {
                items.Add(new ElHeatmapLegendItem
                {
                    Label = ElHeatmapGrowColorScale.FormatValue(m, min),
                    Color = ElHeatmapGrowColorScale.Resolve(m, min, min, max)
                });
                return items;
            }

            for (var i = 0; i < GradientLegendSteps; i++)
            {
                var lo = min + range * i / GradientLegendSteps;
                var hi = min + range * (i + 1) / GradientLegendSteps;
                // 颜色取样点均匀铺满 [0,1]，使首档恰为 MinColor、末档恰为 MaxColor。
                var t = GradientLegendSteps > 1 ? (double)i / (GradientLegendSteps - 1) : 0;

                items.Add(new ElHeatmapLegendItem
                {
                    Label = $"{ElHeatmapGrowColorScale.FormatValue(m, lo)} – {ElHeatmapGrowColorScale.FormatValue(m, hi)}",
                    Color = ElHeatmapGrowColorScale.Interpolate(
                        string.IsNullOrEmpty(m.MinColor) ? ElHeatmapGrowColorScale.DefaultMinColor : m.MinColor,
                        string.IsNullOrEmpty(m.MaxColor) ? ElHeatmapGrowColorScale.DefaultMaxColor : m.MaxColor,
                        t)
                });
            }

            return items;
        }

        /// <summary>推断单元格步长：相邻唯一值的最小正间隔；不足 2 个唯一值时返回 1（对齐参考实现）。</summary>
        /// <param name="values">坐标值序列。</param>
        /// <returns>步长。</returns>
        public static double InferStep(IEnumerable<double> values)
        {
            if (values == null)
            {
                return 1;
            }

            var distinct = values.Distinct().OrderBy(v => v).ToList();
            if (distinct.Count < 2)
            {
                return 1;
            }

            var step = double.MaxValue;
            for (var i = 1; i < distinct.Count; i++)
            {
                var gap = distinct[i] - distinct[i - 1];
                if (gap > 0 && gap < step)
                {
                    step = gap;
                }
            }

            return step == double.MaxValue ? 1 : step;
        }

        private static double ToPixelX(double value, double plotLeft, double lo, double span, double plotWidth)
            => span > 0 ? plotLeft + (value - lo) / span * plotWidth : plotLeft;

        /// <summary>数值 Y 轴向上：值越大，像素 Y 越小（越靠上）。</summary>
        private static double ToPixelY(double value, double plotTop, double lo, double span, double plotHeight)
            => span > 0 ? plotTop + (lo + span - value) / span * plotHeight : plotTop;

        private static List<string> DistinctOrdered(IEnumerable<string> values)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var value in values)
            {
                var text = value ?? string.Empty;
                if (seen.Add(text))
                {
                    result.Add(text);
                }
            }

            return result;
        }

        private static List<double> DistinctSorted(IEnumerable<double> values)
            => values.Distinct().OrderBy(v => v).ToList();

        private static ElHeatmapCell Clone(ElHeatmapCell source) => new ElHeatmapCell
        {
            XLabel = source.XLabel ?? string.Empty,
            YLabel = source.YLabel ?? string.Empty,
            X = source.X,
            Y = source.Y,
            Value = source.Value,
            IsDefined = source.IsDefined,
            XIndex = source.XIndex,
            YIndex = source.YIndex
        };

        // ---------------------------------------------------------------- 渲染

        /// <summary>把模型渲染为完整 SVG 字符串（含外层 &lt;svg&gt;）。</summary>
        /// <param name="model">热力图模型。</param>
        /// <returns>SVG 标记。</returns>
        public static string RenderSvg(ElHeatmapModel model)
        {
            var layout = ComputeLayout(model);
            var inner = RenderContent(layout, model);

            var sb = new StringBuilder(inner.Length + 220);
            sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\"");
            sb.Append($" viewBox=\"0 0 {Fmt(layout.Width)} {Fmt(layout.Height)}\"");
            sb.Append($" width=\"{Fmt(layout.Width)}\" height=\"{Fmt(layout.Height)}\"");
            sb.Append(" class=\"el-heatmap-grow__svg\" role=\"img\"");

            if (!string.IsNullOrEmpty(model?.Title))
            {
                sb.Append($" aria-label=\"{Escape(model.Title)}\"");
            }

            sb.Append('>');
            sb.Append(inner);
            sb.Append("</svg>");
            return sb.ToString();
        }

        /// <summary>生成内层 SVG 标记（不含外层 &lt;svg&gt;），供组件以 MarkupString 注入。</summary>
        /// <param name="layout">已计算的布局。</param>
        /// <param name="model">热力图模型。</param>
        /// <returns>SVG 片段。</returns>
        public static string RenderContent(ElHeatmapLayout layout, ElHeatmapModel model)
        {
            var m = model ?? new ElHeatmapModel();
            var sb = new StringBuilder(1024);

            sb.Append($"<rect x=\"0\" y=\"0\" width=\"{Fmt(layout.Width)}\" height=\"{Fmt(layout.Height)}\" fill=\"#ffffff\"/>");

            if (layout.IsEmpty)
            {
                sb.Append($"<text x=\"{Fmt(layout.Width / 2)}\" y=\"{Fmt(layout.Height / 2)}\" text-anchor=\"middle\" class=\"el-heatmap-grow__empty\" fill=\"#909399\" font-size=\"14\">{Escape(layout.EmptyText)}</text>");
                return sb.ToString();
            }

            if (!string.IsNullOrEmpty(m.Title))
            {
                sb.Append($"<text x=\"{Fmt(layout.Width / 2)}\" y=\"18\" text-anchor=\"middle\" class=\"el-heatmap-grow__title\" fill=\"#303133\" font-size=\"15\">{Escape(m.Title)}</text>");
            }

            RenderCells(sb, layout, m);
            RenderValueLabels(sb, layout, m);
            RenderAxisLabels(sb, layout, m);
            RenderColorScale(sb, layout, m);

            return sb.ToString();
        }

        private static void RenderCells(StringBuilder sb, ElHeatmapLayout layout, ElHeatmapModel m)
        {
            var hasStroke = !string.IsNullOrEmpty(m.Stroke) && m.StrokeWidth > 0;

            sb.Append("<g class=\"el-heatmap-grow__cells\">");

            foreach (var cell in layout.Cells)
            {
                if (cell.Width <= 0 || cell.Height <= 0)
                {
                    continue;
                }

                sb.Append($"<rect x=\"{Fmt(cell.Left)}\" y=\"{Fmt(cell.Top)}\" width=\"{Fmt(cell.Width)}\" height=\"{Fmt(cell.Height)}\" rx=\"2\" ry=\"2\" fill=\"{Escape(cell.Color)}\"");

                if (hasStroke)
                {
                    sb.Append($" stroke=\"{Escape(m.Stroke)}\" stroke-width=\"{Fmt(m.StrokeWidth)}\"");
                }

                sb.Append(" class=\"el-heatmap-grow__cell\">");
                sb.Append($"<title>{Escape(BuildTooltip(cell))}</title>");
                sb.Append("</rect>");
            }

            sb.Append("</g>");
        }

        private static void RenderValueLabels(StringBuilder sb, ElHeatmapLayout layout, ElHeatmapModel m)
        {
            if (!m.ShowValues || !layout.Cells.Any(c => c.ShowValue))
            {
                return;
            }

            sb.Append("<g class=\"el-heatmap-grow__values\">");

            foreach (var cell in layout.Cells)
            {
                if (!cell.ShowValue)
                {
                    continue;
                }

                var fill = cell.LightText ? "#ffffff" : "#303133";
                sb.Append($"<text x=\"{Fmt(cell.CenterX)}\" y=\"{Fmt(cell.CenterY)}\" text-anchor=\"middle\" dominant-baseline=\"middle\" fill=\"{fill}\" font-size=\"11\" class=\"el-heatmap-grow__value\">{Escape(cell.ValueText)}</text>");
            }

            sb.Append("</g>");
        }

        private static void RenderAxisLabels(StringBuilder sb, ElHeatmapLayout layout, ElHeatmapModel m)
        {
            if (m.ShowXLabels && layout.XTicks.Count > 0)
            {
                sb.Append("<g class=\"el-heatmap-grow__x-labels\">");

                foreach (var tick in layout.XTicks)
                {
                    sb.Append($"<text x=\"{Fmt(tick.X)}\" y=\"{Fmt(tick.Y)}\" text-anchor=\"middle\" dominant-baseline=\"middle\" fill=\"#606266\" font-size=\"12\" class=\"el-heatmap-grow__label\">{Escape(tick.Label)}</text>");
                }

                sb.Append("</g>");
            }

            if (m.ShowYLabels && layout.YTicks.Count > 0)
            {
                sb.Append("<g class=\"el-heatmap-grow__y-labels\">");

                foreach (var tick in layout.YTicks)
                {
                    sb.Append($"<text x=\"{Fmt(tick.X)}\" y=\"{Fmt(tick.Y)}\" text-anchor=\"end\" dominant-baseline=\"middle\" fill=\"#606266\" font-size=\"12\" class=\"el-heatmap-grow__label\">{Escape(tick.Label)}</text>");
                }

                sb.Append("</g>");
            }

            if (!string.IsNullOrEmpty(m.XTitle))
            {
                var x = (layout.PlotLeft + layout.PlotRight) / 2;
                sb.Append($"<text x=\"{Fmt(x)}\" y=\"{Fmt(layout.Height - 6)}\" text-anchor=\"middle\" fill=\"#303133\" font-size=\"12\" class=\"el-heatmap-grow__axis-title\">{Escape(m.XTitle)}</text>");
            }

            if (!string.IsNullOrEmpty(m.YTitle))
            {
                var y = (layout.PlotTop + layout.PlotBottom) / 2;
                sb.Append($"<text x=\"14\" y=\"{Fmt(y)}\" text-anchor=\"middle\" transform=\"rotate(-90 14 {Fmt(y)})\" fill=\"#303133\" font-size=\"12\" class=\"el-heatmap-grow__axis-title\">{Escape(m.YTitle)}</text>");
            }
        }

        private static void RenderColorScale(StringBuilder sb, ElHeatmapLayout layout, ElHeatmapModel m)
        {
            if (!m.ShowColorScale || layout.LegendItems.Count == 0)
            {
                return;
            }

            sb.Append("<g class=\"el-heatmap-grow__scale\">");

            foreach (var item in layout.LegendItems)
            {
                sb.Append($"<rect x=\"{Fmt(item.X)}\" y=\"{Fmt(item.Y)}\" width=\"{Fmt(item.Size)}\" height=\"{Fmt(item.Size)}\" rx=\"2\" fill=\"{Escape(item.Color)}\" class=\"el-heatmap-grow__scale-swatch\"/>");
                sb.Append($"<text x=\"{Fmt(item.X + item.Size + 6)}\" y=\"{Fmt(item.Y + item.Size / 2)}\" text-anchor=\"start\" dominant-baseline=\"middle\" fill=\"#606266\" font-size=\"11\" class=\"el-heatmap-grow__scale-label\">{Escape(item.Label)}</text>");
            }

            sb.Append("</g>");
        }

        /// <summary>单元格悬停提示文本（形如 <c>Mon, W1: 12</c>）。</summary>
        /// <param name="cell">单元格。</param>
        /// <returns>提示文本。</returns>
        public static string BuildTooltip(ElHeatmapCell cell)
        {
            if (cell == null)
            {
                return string.Empty;
            }

            var hasX = !string.IsNullOrEmpty(cell.XLabel);
            var hasY = !string.IsNullOrEmpty(cell.YLabel);
            var prefix = hasX && hasY ? $"{cell.XLabel}, {cell.YLabel}" : (hasX ? cell.XLabel : cell.YLabel);

            return string.IsNullOrEmpty(prefix) ? cell.ValueText : $"{prefix}: {cell.ValueText}";
        }

        /// <summary>按不变区域格式化数值，保证 SVG 输出与区域设置无关。</summary>
        /// <param name="value">数值。</param>
        /// <returns>文本。</returns>
        public static string Fmt(double value) => value.ToString(CultureInfo.InvariantCulture);

        /// <summary>XML 转义。</summary>
        /// <param name="text">原始文本。</param>
        /// <returns>转义后的文本。</returns>
        public static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}
