using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    /// <summary>
    /// 热力图（Element Plus 自渲染版，零 Radzen 依赖、零 JS）。
    /// <para>
    /// 参考 Radzen.Blazor 的 <c>RadzenHeatmap</c>（X/Y 类别网格 + 两端线性配色）与 <c>RadzenHeatmapSeries</c>
    /// （数值坐标 + 轴单位单元格 + 分段配色）两种形态，但本组件不引入任何 Radzen 代码：
    /// 全部几何与 SVG 标记均由 <see cref="ElHeatmapGrowEngine"/> 以纯 C# 计算，屏幕渲染与 <c>GetSvg()</c> 导出共用同一布局真源。
    /// </para>
    /// </summary>
    public partial class ElHeatmapGrow : ElementComponentBase
    {
        /// <summary>数据源（每行提供一个 X 维度、一个 Y 维度与一个强度值）。</summary>
        [Parameter] public IEnumerable<object> Data { get; set; }

        /// <summary>X 维度字段名（类别模式下为类别文本；数值模式下须可解析为数字）。</summary>
        [Parameter] public string XProperty { get; set; }

        /// <summary>Y 维度字段名。</summary>
        [Parameter] public string YProperty { get; set; }

        /// <summary>强度字段名（决定单元格颜色）。</summary>
        [Parameter] public string ValueProperty { get; set; }

        /// <summary>画布宽度（像素）。默认 600。</summary>
        [Parameter] public double Width { get; set; } = 600;

        /// <summary>画布高度（像素）。默认 360。</summary>
        [Parameter] public double Height { get; set; } = 360;

        /// <summary>标题（可选）。</summary>
        [Parameter] public string Title { get; set; }

        /// <summary>
        /// 是否使用数值坐标。默认 <c>false</c>（类别网格，单元格等分铺满绘图区，对齐 <c>RadzenHeatmap</c>）；
        /// 设为 <c>true</c> 时按轴单位定位单元格（对齐 <c>RadzenHeatmapSeries</c>，数值支持小数与不等间距）。
        /// </summary>
        [Parameter] public bool NumericAxes { get; set; }

        /// <summary>数值模式下单元格宽度（轴单位）；0 表示按相邻唯一 X 值的最小间隔自动推断。</summary>
        [Parameter] public double CellWidth { get; set; }

        /// <summary>数值模式下单元格高度（轴单位）；0 表示自动推断。</summary>
        [Parameter] public double CellHeight { get; set; }

        /// <summary>类别模式下单元格内缩边距（像素）。默认 2。</summary>
        [Parameter] public double CellPadding { get; set; } = 2;

        /// <summary>最小值颜色（两端线性映射）。默认 <c>#f0f0f0</c>。</summary>
        [Parameter] public string MinColor { get; set; } = ElHeatmapGrowColorScale.DefaultMinColor;

        /// <summary>最大值颜色（两端线性映射）。默认 <c>#1a9641</c>。</summary>
        [Parameter] public string MaxColor { get; set; } = ElHeatmapGrowColorScale.DefaultMaxColor;

        /// <summary>分段配色区间（非空时优先于线性映射；未命中回退 <see cref="Fill"/>）。</summary>
        [Parameter] public IEnumerable<ElHeatmapColorRange> ColorRanges { get; set; }

        /// <summary>分段配色未命中时的回退填充色（为空则透明）。</summary>
        [Parameter] public string Fill { get; set; }

        /// <summary>单元格描边颜色（可选）。</summary>
        [Parameter] public string Stroke { get; set; }

        /// <summary>单元格描边宽度（像素，配合 <see cref="Stroke"/> 生效）。</summary>
        [Parameter] public double StrokeWidth { get; set; }

        /// <summary>是否在格内显示强度数值（仅在单元格宽 &gt; 20 且高 &gt; 14 像素时绘制）。</summary>
        [Parameter] public bool ShowValues { get; set; }

        /// <summary>数值格式串，支持 <c>N1</c> 或 <c>{0:0.0}</c> 两种写法。</summary>
        [Parameter] public string FormatString { get; set; }

        /// <summary>是否显示 X 轴标签。默认 true。</summary>
        [Parameter] public bool ShowXLabels { get; set; } = true;

        /// <summary>是否显示 Y 轴标签。默认 true。</summary>
        [Parameter] public bool ShowYLabels { get; set; } = true;

        /// <summary>是否显示右侧色阶图例。默认 true。</summary>
        [Parameter] public bool ShowColorScale { get; set; } = true;

        /// <summary>X 轴标题（可选）。</summary>
        [Parameter] public string XTitle { get; set; }

        /// <summary>Y 轴标题（可选）。</summary>
        [Parameter] public string YTitle { get; set; }

        /// <summary>空数据提示文本。</summary>
        [Parameter] public string EmptyText { get; set; } = "暂无数据";

        /// <summary>由当前参数构建纯数据模型（把 <see cref="Data"/> 反射解析为单元格）。</summary>
        /// <returns>热力图模型。</returns>
        public ElHeatmapModel BuildModel()
        {
            var model = new ElHeatmapModel
            {
                Width = Width,
                Height = Height,
                Title = Title ?? string.Empty,
                EmptyText = string.IsNullOrEmpty(EmptyText) ? "暂无数据" : EmptyText,
                NumericAxes = NumericAxes,
                CellWidth = CellWidth,
                CellHeight = CellHeight,
                CellPadding = CellPadding,
                MinColor = string.IsNullOrEmpty(MinColor) ? ElHeatmapGrowColorScale.DefaultMinColor : MinColor,
                MaxColor = string.IsNullOrEmpty(MaxColor) ? ElHeatmapGrowColorScale.DefaultMaxColor : MaxColor,
                Fill = Fill ?? string.Empty,
                Stroke = Stroke ?? string.Empty,
                StrokeWidth = StrokeWidth,
                ShowValues = ShowValues,
                FormatString = FormatString ?? string.Empty,
                ShowXLabels = ShowXLabels,
                ShowYLabels = ShowYLabels,
                ShowColorScale = ShowColorScale,
                XTitle = XTitle ?? string.Empty,
                YTitle = YTitle ?? string.Empty,
                Cells = ResolveCells()
            };

            if (ColorRanges != null)
            {
                model.ColorRanges = ColorRanges.Where(r => r != null).ToList();
            }

            return model;
        }

        /// <summary>计算热力图布局（单元格像素矩形、轴刻度、色阶），与渲染共用，便于测试断言。</summary>
        /// <returns>结构化布局。</returns>
        public ElHeatmapLayout ComputeLayout() => ElHeatmapGrowEngine.ComputeLayout(BuildModel());

        /// <summary>生成完整 SVG 字符串（含外层 svg）。</summary>
        /// <returns>SVG 标记。</returns>
        public string GetSvg() => ElHeatmapGrowEngine.RenderSvg(BuildModel());

        /// <summary>内层 SVG 标记（供 Razor 以 MarkupString 注入）。</summary>
        public string InnerMarkup => GetSvg();

        /// <summary>把数据源反射解析为单元格列表。</summary>
        /// <returns>单元格列表。</returns>
        public List<ElHeatmapCell> ResolveCells()
        {
            var cells = new List<ElHeatmapCell>();

            if (Data == null)
            {
                return cells;
            }

            foreach (var item in Data.Cast<object>())
            {
                if (item == null)
                {
                    continue;
                }

                var defined = TryToDouble(GetFieldValue(item, ValueProperty), out var value);

                var x = 0.0;
                var y = 0.0;
                if (NumericAxes)
                {
                    TryToDouble(GetFieldValue(item, XProperty), out x);
                    TryToDouble(GetFieldValue(item, YProperty), out y);
                }

                cells.Add(new ElHeatmapCell
                {
                    XLabel = GetFieldValue(item, XProperty)?.ToString() ?? string.Empty,
                    YLabel = GetFieldValue(item, YProperty)?.ToString() ?? string.Empty,
                    X = x,
                    Y = y,
                    Value = value,
                    IsDefined = defined
                });
            }

            return cells;
        }

        private static bool TryToDouble(object value, out double result)
        {
            result = 0;

            if (value == null)
            {
                return false;
            }

            switch (value)
            {
                case double d:
                    result = d;
                    return true;
                case float f:
                    result = f;
                    return true;
                case decimal m:
                    result = (double)m;
                    return true;
                case int i:
                    result = i;
                    return true;
                case long l:
                    result = l;
                    return true;
                case short s:
                    result = s;
                    return true;
                case byte b:
                    result = b;
                    return true;
                case string str:
                    return double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
                default:
                    return double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
            }
        }

        private static object GetFieldValue(object row, string field)
        {
            if (row == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(field))
            {
                return row;
            }

            var property = row.GetType().GetProperty(field, BindingFlags.Public | BindingFlags.Instance);
            return property?.GetValue(row);
        }
    }
}
