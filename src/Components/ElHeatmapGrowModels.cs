using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// 热力图单元格模型（已从原始数据解析；布局阶段会被复制一份并回填像素几何与最终颜色）。
    /// </summary>
    public sealed class ElHeatmapCell
    {
        /// <summary>X 轴标签（类别模式为类别名；数值模式为数值的原始文本）。</summary>
        public string XLabel { get; set; } = string.Empty;

        /// <summary>Y 轴标签。</summary>
        public string YLabel { get; set; } = string.Empty;

        /// <summary>X 坐标（数值模式为轴单位；类别模式为类别序号）。</summary>
        public double X { get; set; }

        /// <summary>Y 坐标（数值模式为轴单位；类别模式为类别序号）。</summary>
        public double Y { get; set; }

        /// <summary>强度值（决定颜色）。</summary>
        public double Value { get; set; }

        /// <summary>该行数据是否解析成功（值为有效数字）。未定义的单元格不绘制。</summary>
        public bool IsDefined { get; set; }

        /// <summary>已在类别序列中的下标（类别模式下有效；数值模式为 -1）。</summary>
        public int XIndex { get; set; } = -1;

        /// <summary>已在类别序列中的下标（类别模式下有效；数值模式为 -1）。</summary>
        public int YIndex { get; set; } = -1;

        /// <summary>布局解析出的填充色。</summary>
        public string Color { get; set; } = string.Empty;

        /// <summary>单元格左边界（像素，已计入内缩）。</summary>
        public double Left { get; set; }

        /// <summary>单元格上边界（像素）。</summary>
        public double Top { get; set; }

        /// <summary>单元格宽度（像素）。</summary>
        public double Width { get; set; }

        /// <summary>单元格高度（像素）。</summary>
        public double Height { get; set; }

        /// <summary>单元格水平中心（像素）。</summary>
        public double CenterX { get; set; }

        /// <summary>单元格垂直中心（像素）。</summary>
        public double CenterY { get; set; }

        /// <summary>是否在格内绘制数值标签（由尺寸阈值与 <c>ShowValues</c> 共同决定）。</summary>
        public bool ShowValue { get; set; }

        /// <summary>格内数值文本是否应使用浅色（深底用白字）。</summary>
        public bool LightText { get; set; }

        /// <summary>格式化后的数值文本（供格内标签与悬停提示复用）。</summary>
        public string ValueText { get; set; } = string.Empty;
    }

    /// <summary>坐标轴刻度（已定位到像素）。</summary>
    public sealed class ElHeatmapTick
    {
        /// <summary>刻度文本。</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>刻度在轴上的值（数值模式为轴单位；类别模式为序号）。</summary>
        public double Value { get; set; }

        /// <summary>水平位置（像素）。</summary>
        public double X { get; set; }

        /// <summary>垂直位置（像素）。</summary>
        public double Y { get; set; }
    }

    /// <summary>色阶图例项（已定位到像素）。</summary>
    public sealed class ElHeatmapLegendItem
    {
        /// <summary>图例文本（分段标签或色阶区间）。</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>色块颜色。</summary>
        public string Color { get; set; } = string.Empty;

        /// <summary>色块左上角 X（像素）。</summary>
        public double X { get; set; }

        /// <summary>色块左上角 Y（像素）。</summary>
        public double Y { get; set; }

        /// <summary>色块边长（像素）。</summary>
        public double Size { get; set; } = 14;
    }

    /// <summary>
    /// 热力图布局结果（纯几何，不含任何 UI 依赖）。屏幕渲染与 <c>RenderSvg</c> 导出共用同一份结果，便于单元测试断言。
    /// </summary>
    public sealed class ElHeatmapLayout
    {
        /// <summary>画布宽度（像素）。</summary>
        public double Width { get; set; }

        /// <summary>画布高度（像素）。</summary>
        public double Height { get; set; }

        /// <summary>绘图区左边界。</summary>
        public double PlotLeft { get; set; }

        /// <summary>绘图区上边界。</summary>
        public double PlotTop { get; set; }

        /// <summary>绘图区右边界。</summary>
        public double PlotRight { get; set; }

        /// <summary>绘图区下边界。</summary>
        public double PlotBottom { get; set; }

        /// <summary>是否处于数值坐标模式。</summary>
        public bool IsNumeric { get; set; }

        /// <summary>是否无数据（渲染空状态提示）。</summary>
        public bool IsEmpty { get; set; }

        /// <summary>空状态提示文本。</summary>
        public string EmptyText { get; set; } = "暂无数据";

        /// <summary>强度最小值（有效单元格）。</summary>
        public double MinValue { get; set; }

        /// <summary>强度最大值（有效单元格）。</summary>
        public double MaxValue { get; set; }

        /// <summary>结构强度最小值（用于颜色映射；与 MinValue 相同，独立保留便于后续扩展）。</summary>
        public double ColorMin { get; set; }

        /// <summary>结构强度最大值（用于颜色映射）。</summary>
        public double ColorMax { get; set; }

        /// <summary>X 方向单元格步长（数值模式为轴单位；类别模式为像素带宽）。</summary>
        public double CellStepX { get; set; }

        /// <summary>Y 方向单元格步长（数值模式为轴单位；类别模式为像素带宽）。</summary>
        public double CellStepY { get; set; }

        /// <summary>X 轴类别序列（按出现顺序去重）。</summary>
        public List<string> XCategories { get; set; } = new List<string>();

        /// <summary>Y 轴类别序列（按出现顺序去重）。</summary>
        public List<string> YCategories { get; set; } = new List<string>();

        /// <summary>X 轴数值序列（去重升序；数值模式）。</summary>
        public List<double> XValues { get; set; } = new List<double>();

        /// <summary>Y 轴数值序列（去重升序；数值模式）。</summary>
        public List<double> YValues { get; set; } = new List<double>();

        /// <summary>已定位的单元格。</summary>
        public List<ElHeatmapCell> Cells { get; set; } = new List<ElHeatmapCell>();

        /// <summary>X 轴刻度。</summary>
        public List<ElHeatmapTick> XTicks { get; set; } = new List<ElHeatmapTick>();

        /// <summary>Y 轴刻度。</summary>
        public List<ElHeatmapTick> YTicks { get; set; } = new List<ElHeatmapTick>();

        /// <summary>色阶图例项。</summary>
        public List<ElHeatmapLegendItem> LegendItems { get; set; } = new List<ElHeatmapLegendItem>();
    }

    /// <summary>
    /// 热力图纯数据模型（与 UI 组件解耦，便于纯算法与单元测试直接构造）。
    /// </summary>
    public sealed class ElHeatmapModel
    {
        /// <summary>画布宽度（像素）。</summary>
        public double Width { get; set; } = 600;

        /// <summary>画布高度（像素）。</summary>
        public double Height { get; set; } = 360;

        /// <summary>标题（可选）。</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>空数据提示文本。</summary>
        public string EmptyText { get; set; } = "暂无数据";

        /// <summary>是否使用数值坐标（false 为类别网格，对齐 RadzenHeatmap；true 为轴单位网格，对齐 RadzenHeatmapSeries）。</summary>
        public bool NumericAxes { get; set; }

        /// <summary>数值模式下单元格宽度（轴单位）；0 表示按相邻唯一 X 值的最小间隔自动推断。</summary>
        public double CellWidth { get; set; }

        /// <summary>数值模式下单元格高度（轴单位）；0 表示自动推断。</summary>
        public double CellHeight { get; set; }

        /// <summary>类别模式下单元格内缩边距（像素）。</summary>
        public double CellPadding { get; set; } = 2;

        /// <summary>是否在格内显示数值标签（仅在格子足够大时绘制）。</summary>
        public bool ShowValues { get; set; }

        /// <summary>数值格式串（如 <c>N1</c>、<c>{0:0.0}</c>）；为空则用不变区域默认格式。</summary>
        public string FormatString { get; set; } = string.Empty;

        /// <summary>是否显示 X 轴标签。默认 true。</summary>
        public bool ShowXLabels { get; set; } = true;

        /// <summary>是否显示 Y 轴标签。默认 true。</summary>
        public bool ShowYLabels { get; set; } = true;

        /// <summary>是否显示右侧色阶图例。默认 true。</summary>
        public bool ShowColorScale { get; set; } = true;

        /// <summary>X 轴标题（可选）。</summary>
        public string XTitle { get; set; } = string.Empty;

        /// <summary>Y 轴标题（可选）。</summary>
        public string YTitle { get; set; } = string.Empty;

        /// <summary>线性映射的最小值颜色。默认 <c>#f0f0f0</c>。</summary>
        public string MinColor { get; set; } = ElHeatmapGrowColorScale.DefaultMinColor;

        /// <summary>线性映射的最大值颜色。默认 <c>#1a9641</c>。</summary>
        public string MaxColor { get; set; } = ElHeatmapGrowColorScale.DefaultMaxColor;

        /// <summary>分段配色区间（非空时优先于线性映射）。</summary>
        public List<ElHeatmapColorRange> ColorRanges { get; set; } = new List<ElHeatmapColorRange>();

        /// <summary>分段配色未命中时的回退填充色（为空则视为透明）。</summary>
        public string Fill { get; set; } = string.Empty;

        /// <summary>单元格描边颜色（可选）。</summary>
        public string Stroke { get; set; } = string.Empty;

        /// <summary>单元格描边宽度（像素）。</summary>
        public double StrokeWidth { get; set; }

        /// <summary>已解析的单元格（由组件层从 Data 反射得到）。</summary>
        public List<ElHeatmapCell> Cells { get; set; } = new List<ElHeatmapCell>();
    }
}
