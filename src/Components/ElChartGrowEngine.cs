using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ElementPlus
{
    /// <summary>
    /// 图表类型（对齐 Radzen.Blazor 的 RadzenChart 常用 series：线/面积/柱/条/散点/气泡/饼/环/雷达 + 堆叠变体）。
    /// </summary>
    public enum ElChartType
    {
        /// <summary>折线图（类别在 X 轴）。</summary>
        Line,
        /// <summary>面积图（折线下方填充）。</summary>
        Area,
        /// <summary>柱状图（垂直柱，类别在 X 轴）。</summary>
        Column,
        /// <summary>条形图（水平条，类别在 Y 轴）。</summary>
        Bar,
        /// <summary>散点图（仅标记，无连线；配置 XValueField 时改用数值 X 轴）。</summary>
        Scatter,
        /// <summary>气泡图（散点 + 由 SizeField 决定半径的第三维度）。</summary>
        Bubble,
        /// <summary>饼图（径向，单序列）。</summary>
        Pie,
        /// <summary>环图（径向，单序列）。</summary>
        Donut,
        /// <summary>雷达图（多序列在辐射坐标系上叠加，类别即辐射轴）。</summary>
        Radar,
        /// <summary>堆叠柱状图。</summary>
        StackedColumn,
        /// <summary>堆叠条形图。</summary>
        StackedBar,
        /// <summary>堆叠面积图。</summary>
        StackedArea,
        /// <summary>堆叠折线图。</summary>
        StackedLine,
        /// <summary>100% 堆叠柱状图（每类别归一化到 100）。</summary>
        FullStackedColumn,
        /// <summary>100% 堆叠条形图。</summary>
        FullStackedBar,
        /// <summary>100% 堆叠面积图。</summary>
        FullStackedArea,
        /// <summary>100% 堆叠折线图（仅折线按百分比定位，不填充）。</summary>
        FullStackedLine,
        /// <summary>范围柱状图（每类别由 ValueField 到 ValueField2 的浮动柱）。</summary>
        RangeColumn,
        /// <summary>范围条形图（水平）。</summary>
        RangeBar,
        /// <summary>范围面积图（上下界之间的带状区域）。</summary>
        RangeArea,
        /// <summary>K 线图（蜡烛）：Value=开 Value2=高 Value3=低 Value4=收。</summary>
        Candlestick,
        /// <summary>OHLC 图：Value=开 Value2=高 Value3=低 Value4=收（以竖线 + 左右刻度表示）。</summary>
        Ohlc,
        /// <summary>高低图：Value=高 Value2=低 Value3=收（收为可选的短横线）。</summary>
        HighLow,
        /// <summary>箱线图：Value=最小 Value2=Q1 Value3=中位 Value4=Q3 Value5=最大。</summary>
        BoxPlot,
        /// <summary>瀑布图（垂直，逐项累计）。</summary>
        Waterfall,
        /// <summary>瀑布图（水平）。</summary>
        HorizontalWaterfall,
        /// <summary>子弹图：Value=实际值 Value2=目标值（带三档背景带）。</summary>
        Bullet,
        /// <summary>漏斗图（垂直居中梯形带，按数值递减）。</summary>
        Funnel,
        /// <summary>金字塔图（垂直居中梯形带，按数值递增，与漏斗相反）。</summary>
        Pyramid,
        /// <summary>热力图（CategoryField=X 列，GroupField=Y 行，ValueField=值；独立图表类型）。</summary>
        Heatmap,
        /// <summary>等值带图（与 Heatmap 同数据模型，但色阶量化并绘制层级边界线）。</summary>
        Contour,
        /// <summary>蜘蛛柱状图（雷达坐标系上沿辐射轴从圆心向外画柱）。</summary>
        SpiderColumn
    }

    /// <summary>图例位置。</summary>
    public enum ElLegendPosition { Top, Bottom, Left, Right }

    /// <summary>坐标轴类型。</summary>
    public enum ElAxisType { Category, Value }

    /// <summary>趋势线类型（对齐 Radzen 的 TrendLine / MovingAverage / Polynomial / Mean / Median / Mode）。</summary>
    public enum ElTrendLineType
    {
        /// <summary>不绘制。</summary>
        None,
        /// <summary>最小二乘线性回归。</summary>
        Linear,
        /// <summary>移动平均（窗口由 TrendLinePeriod 决定）。</summary>
        MovingAverage,
        /// <summary>二次多项式拟合（抛物线）。</summary>
        Polynomial,
        /// <summary>水平均值线。</summary>
        Mean,
        /// <summary>水平中位数线。</summary>
        Median,
        /// <summary>水平众数线（按 2 位小数归并后取出现最多者）。</summary>
        Mode
    }

    /// <summary>参考线/参考带依附的轴。</summary>
    public enum ElReferenceAxis { Value, Category }

    /// <summary>线型（用于折线/面积图的描边）。</summary>
    public enum ElLineStyle { Solid, Dashed, Dotted }

    /// <summary>标记形状（折线/面积/散点图的数据点）。</summary>
    public enum ElMarkerType { None, Circle, Square, Diamond }

    /// <summary>单个数据点的模型（已从原始 Items 解析为「类别标签 + 数值」，散点/气泡可带数值 X 与尺寸）。</summary>
    public sealed class ElChartPoint
    {
        /// <summary>类别标签（饼/环为扇区名；直角坐标为 X 轴类别；雷达为辐射轴名）。</summary>
        public string Category { get; set; }

        /// <summary>数值（Y 轴 / 半径 / 扇区值）。</summary>
        public double Value { get; set; }

        /// <summary>数值 X 坐标（散点/气泡配置了 XValueField 时填入；否则为 0，表示用类别位置）。</summary>
        public double X { get; set; }

        /// <summary>气泡尺寸（Bubble 配置了 SizeField 时填入；否则为 0，表示用默认半径）。</summary>
        public double Size { get; set; }

        /// <summary>
        /// 多值槽 2。按类型语义：
        /// Ohlc/Candlestick=<c>High</c>；HighLow=<c>Low</c>；BoxPlot=<c>Q1</c>；
        /// RangeArea/RangeBar/RangeColumn=<c>上界</c>；Bullet=<c>目标值</c>；Waterfall=<c>小计标记（&gt;0 视为小计）</c>。
        /// </summary>
        public double Value2 { get; set; }

        /// <summary>多值槽 3。Ohlc/Candlestick=<c>Low</c>；HighLow=<c>Close（可选）</c>；BoxPlot=<c>Median</c>。</summary>
        public double Value3 { get; set; }

        /// <summary>多值槽 4。Ohlc/Candlestick=<c>Close</c>；BoxPlot=<c>Q3</c>。</summary>
        public double Value4 { get; set; }

        /// <summary>多值槽 5。仅 BoxPlot=<c>Max</c>。</summary>
        public double Value5 { get; set; }

        /// <summary>第二类别维度（Heatmap/Contour 的 Y 行分组；其它类型留空）。</summary>
        public string Group { get; set; } = string.Empty;

        /// <summary>是否为小计/合计行（Waterfall/HorizontalWaterfall）。</summary>
        public bool IsTotal { get; set; }
    }

    /// <summary>参考线（横向/纵向的阈值线，如目标线、警戒线）。</summary>
    public sealed class ElChartReferenceLine
    {
        /// <summary>依附的轴（Value=按数值定位；Category=按类别定位）。</summary>
        public ElReferenceAxis Axis { get; set; } = ElReferenceAxis.Value;

        /// <summary>数值（Axis=Value 时使用）。</summary>
        public double Value { get; set; }

        /// <summary>类别标签（Axis=Category 时使用）。</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>线旁标签文本。</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>颜色。</summary>
        public string Color { get; set; } = "#e6a23c";

        /// <summary>线型。</summary>
        public ElLineStyle LineStyle { get; set; } = ElLineStyle.Dashed;
    }

    /// <summary>参考带（区间底色，如正常范围）。</summary>
    public sealed class ElChartReferenceBand
    {
        /// <summary>依附的轴。</summary>
        public ElReferenceAxis Axis { get; set; } = ElReferenceAxis.Value;

        /// <summary>区间起点数值（Axis=Value）。</summary>
        public double From { get; set; }

        /// <summary>区间终点数值（Axis=Value）。</summary>
        public double To { get; set; }

        /// <summary>区间起点类别序号（Axis=Category，支持 0.5 表示半格）。</summary>
        public double FromIndex { get; set; }

        /// <summary>区间终点类别序号（Axis=Category）。</summary>
        public double ToIndex { get; set; }

        /// <summary>带内标签文本。</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>颜色。</summary>
        public string Color { get; set; } = "#f4f4f5";

        /// <summary>不透明度。默认 0.6。</summary>
        public double Opacity { get; set; } = 0.6;
    }

    /// <summary>数据点标注（在指定坐标画标记 + 文本，如异常点说明）。</summary>
    public sealed class ElChartAnnotation
    {
        /// <summary>类别标签（未提供 X 时按类别定位）。</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>数值 X（图表使用数值 X 轴时优先于 Category）。</summary>
        public double? X { get; set; }

        /// <summary>数值 Y。</summary>
        public double Value { get; set; }

        /// <summary>标注文本。</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>颜色。默认危险色。</summary>
        public string Color { get; set; } = "#f56c6c";

        /// <summary>标记形状（None 则不画标记，只画文本）。</summary>
        public ElMarkerType Marker { get; set; } = ElMarkerType.Circle;
    }

    /// <summary>序列模型（与 UI 组件解耦，便于纯算法与单元测试）。</summary>
    public sealed class ElChartSeriesModel
    {
        /// <summary>图表类型。</summary>
        public ElChartType Type { get; set; } = ElChartType.Column;

        /// <summary>序列标题（图例文本）。</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>序列颜色（已解析为具体色值；为空则由调色板分配）。</summary>
        public string Color { get; set; } = string.Empty;

        /// <summary>调用方是否显式给过序列色。饼/环据此决定扇区逐块取调色板，还是整图沿用该色。</summary>
        public bool ColorExplicit { get; set; }

        /// <summary>是否显示数据标签。</summary>
        public bool ShowDataLabels { get; set; }

        /// <summary>线型。</summary>
        public ElLineStyle LineStyle { get; set; } = ElLineStyle.Solid;

        /// <summary>标记形状。</summary>
        public ElMarkerType MarkerType { get; set; } = ElMarkerType.Circle;

        /// <summary>已解析的数据点。</summary>
        public List<ElChartPoint> Points { get; set; } = new List<ElChartPoint>();

        /// <summary>数值 X 字段名（散点/气泡使用，提供时改用数值 X 轴而非类别位置）。</summary>
        public string XValueField { get; set; } = string.Empty;

        /// <summary>气泡尺寸字段名（Bubble 使用）。</summary>
        public string SizeField { get; set; } = string.Empty;

        /// <summary>多值槽 2 的字段名（语义见 <see cref="ElChartPoint.Value2"/>）。</summary>
        public string ValueField2 { get; set; } = string.Empty;

        /// <summary>多值槽 3 的字段名。</summary>
        public string ValueField3 { get; set; } = string.Empty;

        /// <summary>多值槽 4 的字段名。</summary>
        public string ValueField4 { get; set; } = string.Empty;

        /// <summary>多值槽 5 的字段名。</summary>
        public string ValueField5 { get; set; } = string.Empty;

        /// <summary>第二类别维度字段名（Heatmap/Contour 的 Y 行）。</summary>
        public string GroupField { get; set; } = string.Empty;

        /// <summary>该序列绑定的数值轴名称（空=主轴；与 <see cref="ElChartAxisModel.Name"/> 匹配即为次轴）。</summary>
        public string ValueAxisName { get; set; } = string.Empty;

        /// <summary>趋势线类型。默认 None。</summary>
        public ElTrendLineType TrendLine { get; set; } = ElTrendLineType.None;

        /// <summary>移动平均窗口（TrendLine=MovingAverage 时生效）。默认 3。</summary>
        public int TrendLinePeriod { get; set; } = 3;

        /// <summary>趋势线线型。默认虚线。</summary>
        public ElLineStyle TrendLineStyle { get; set; } = ElLineStyle.Dashed;

        /// <summary>趋势线颜色（空=跟随序列颜色）。</summary>
        public string TrendLineColor { get; set; } = string.Empty;

        /// <summary>K 线/H 柱上涨色。默认中国习惯「涨红」。</summary>
        public string RisingColor { get; set; } = "#f56c6c";

        /// <summary>K 线/H 柱下跌色。默认中国习惯「跌绿」。</summary>
        public string FallingColor { get; set; } = "#67c23a";

        /// <summary>数据标签/提示的数值格式（如 <c>N2</c>、<c>0.0%</c>；空=默认数字格式）。</summary>
        public string ValueFormat { get; set; } = string.Empty;
    }

    /// <summary>坐标轴模型。</summary>
    public sealed class ElChartAxisModel
    {
        /// <summary>坐标轴类型。</summary>
        public ElAxisType Type { get; set; } = ElAxisType.Value;

        /// <summary>坐标轴名称（与 <see cref="ElChartSeriesModel.ValueAxisName"/> 匹配以绑定次轴；空=主轴）。</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>是否将本次轴绘制在右侧（次轴默认右侧，可由组件参数覆盖）。</summary>
        public bool OnRight { get; set; }

        /// <summary>坐标轴标题。</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>最小值（不设置则由数据自动推断）。</summary>
        public double? Min { get; set; }

        /// <summary>最大值（不设置则由数据自动推断）。</summary>
        public double? Max { get; set; }

        /// <summary>刻度步长（不设置则由 nice 算法推断）。</summary>
        public double? Step { get; set; }

        /// <summary>刻度/标签格式化回调。</summary>
        public Func<object, string> Formatter { get; set; }
    }

    /// <summary>图表整体模型。</summary>
    public sealed class ElChartModel
    {
        /// <summary>画布宽度（像素，viewBox 单位）。</summary>
        public double Width { get; set; } = 600;

        /// <summary>画布高度（像素，viewBox 单位）。</summary>
        public double Height { get; set; } = 360;

        /// <summary>图表标题（可选）。</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>是否显示图例。默认 true。</summary>
        public bool ShowLegend { get; set; } = true;

        /// <summary>图例位置。默认 Right。</summary>
        public ElLegendPosition LegendPosition { get; set; } = ElLegendPosition.Right;

        /// <summary>饼/环图圆心文字（环图常用「总计/一览」这类占位）。为空则不画。</summary>
        public string CenterText { get; set; } = string.Empty;

        /// <summary>是否显示类别轴。默认 true（径向图/雷达图/数值 X 轴时忽略）。</summary>
        public bool ShowCategoryAxis { get; set; } = true;

        /// <summary>是否显示数值轴。默认 true（雷达图忽略标准左轴，改用辐射刻度）。</summary>
        public bool ShowValueAxis { get; set; } = true;

        /// <summary>是否显示网格线。默认 true。</summary>
        public bool ShowGridLines { get; set; } = true;

        /// <summary>序列集合。</summary>
        public List<ElChartSeriesModel> Series { get; set; } = new List<ElChartSeriesModel>();

        /// <summary>坐标轴集合（类别轴/数值轴可分别配置，未提供时使用默认轴）。</summary>
        public List<ElChartAxisModel> Axes { get; set; } = new List<ElChartAxisModel>();

        /// <summary>调色板（系列颜色不足时循环使用）。</summary>
        public List<string> Palette { get; set; } = new List<string>
        {
            "#5470c6", "#91cc75", "#fac858", "#ee6666", "#73c0de",
            "#3ba272", "#fc8452", "#9a60b4", "#ea7ccc", "#4e79a7"
        };

        /// <summary>空数据提示文本。</summary>
        public string EmptyText { get; set; } = "暂无数据";

        /// <summary>参考线集合（目标线/警戒线）。</summary>
        public List<ElChartReferenceLine> ReferenceLines { get; set; } = new List<ElChartReferenceLine>();

        /// <summary>参考带集合（区间底色）。</summary>
        public List<ElChartReferenceBand> ReferenceBands { get; set; } = new List<ElChartReferenceBand>();

        /// <summary>标注集合。</summary>
        public List<ElChartAnnotation> Annotations { get; set; } = new List<ElChartAnnotation>();

        /// <summary>是否在悬停时输出命中目标（供交互提示使用）。默认 true。</summary>
        public bool CollectHits { get; set; } = true;
    }

    /// <summary>单条序列的渲染几何结果。</summary>
    public sealed class ElSeriesRender
    {
        /// <summary>图表类型。</summary>
        public ElChartType Type { get; set; }

        /// <summary>序列标题。</summary>
        public string Title { get; set; }

        /// <summary>颜色。</summary>
        public string Color { get; set; }

        /// <summary>线型（折线/面积）。</summary>
        public ElLineStyle LineStyle { get; set; }

        /// <summary>标记形状。</summary>
        public ElMarkerType MarkerType { get; set; }

        /// <summary>是否显示数据标签。</summary>
        public bool ShowDataLabels { get; set; }

        /// <summary>折线/面积/散点/气泡/雷达的顶点（X/Y 为画布坐标，Size 为气泡半径）。</summary>
        public List<(double X, double Y, double Size)> Points { get; set; } = new List<(double X, double Y, double Size)>();

        /// <summary>折线/面积图的轮廓 path（已包含区域闭合，供直接输出）。</summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>柱/条矩形的几何（X/Y/W/H 为画布坐标，Value 为原始数值）。</summary>
        public List<(double X, double Y, double W, double H, double Value)> Bars { get; set; } = new List<(double X, double Y, double W, double H, double Value)>();

        /// <summary>饼/环扇区几何（Color 为该块实际填色：逐块取调色板，或整图沿用序列色）。</summary>
        public List<(double Cx, double Cy, double ROuter, double RInner, double StartDeg, double EndDeg, double Value, string Label, string Color)> Slices { get; set; }
            = new List<(double Cx, double Cy, double ROuter, double RInner, double StartDeg, double EndDeg, double Value, string Label, string Color)>();

        // —— 以下为新增系列的几何槽（前 4 类系列不使用，保持为空） ——

        /// <summary>线段几何：K 线影线 / OHLC 高低线 / 箱线须与中位线 / 子弹目标线 / 参考线。
        /// Kind ∈ wick|open|close|whisker|median|cap|target|refline。</summary>
        public List<(double X1, double Y1, double X2, double Y2, string Kind)> Segments { get; set; }
            = new List<(double X1, double Y1, double X2, double Y2, string Kind)>();

        /// <summary>矩形几何：蜡烛实体 / 箱体 / 热力单元 / 子弹背景档。
        /// Kind ∈ candle|box|cell|band。</summary>
        public List<(double X, double Y, double W, double H, string Fill, string Kind)> Boxes { get; set; }
            = new List<(double X, double Y, double W, double H, string Fill, string Kind)>();

        /// <summary>趋势线/均值线路径（已含 M/L 指令；空=不绘制）。</summary>
        public string TrendPath { get; set; } = string.Empty;

        /// <summary>趋势线描边颜色。</summary>
        public string TrendColor { get; set; } = string.Empty;

        /// <summary>趋势线线型。</summary>
        public ElLineStyle TrendStyle { get; set; } = ElLineStyle.Dashed;

        /// <summary>漏斗/金字塔梯形（Points 为 SVG polygon 点串）。</summary>
        public List<(string Points, double Value, string Label)> Polygons { get; set; }
            = new List<(string Points, double Value, string Label)>();

        /// <summary>蜘蛛柱（沿辐射轴的粗线段；Width 为描边宽度）。</summary>
        public List<(double X1, double Y1, double X2, double Y2, double Width, double Value)> Spokes { get; set; }
            = new List<(double X1, double Y1, double X2, double Y2, double Width, double Value)>();

        /// <summary>显式数据标签（由新系列计算时填充；X/Y=画布坐标，Anchor ∈ start|middle|end）。</summary>
        public List<(double X, double Y, string Text, string Anchor)> Labels { get; set; }
            = new List<(double X, double Y, string Text, string Anchor)>();
    }

    /// <summary>命中目标（供悬停提示与交互高亮；已解析为画布坐标与展示文本）。</summary>
    public sealed class ElChartHit
    {
        /// <summary>序列序号。</summary>
        public int SeriesIndex { get; set; }

        /// <summary>数据点序号。</summary>
        public int PointIndex { get; set; }

        /// <summary>序列标题。</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>系列颜色。</summary>
        public string Color { get; set; } = string.Empty;

        /// <summary>类别标签。</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>数值文本（已按 ValueFormat 格式化；K 线类为「开/高/低/收」组合文本）。</summary>
        public string ValueText { get; set; } = string.Empty;

        /// <summary>锚点画布坐标。</summary>
        public double X, Y;

        /// <summary>命中半径（矩形类用矩形判定，径向类用角度+半径判定）。</summary>
        public double Radius = 12;

        /// <summary>矩形命中区（W/H &gt; 0 时优先使用矩形判定）。</summary>
        public double W, H;
    }

    /// <summary>数值轴刻度（支持主轴 + 多个命名次轴）。</summary>
    public sealed class ElAxisScale
    {
        /// <summary>轴名称（空=主轴）。</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>轴标题。</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>轴范围与步长。</summary>
        public double Lo, Hi, Step;

        /// <summary>是否绘制在右侧。</summary>
        public bool OnRight;

        /// <summary>是否由用户显式指定过 Min/Max/Step（0..3 位掩码：1=Min 2=Max 4=Step）。</summary>
        public int Forced;

        /// <summary>刻度（数值/画布 Y/标签）。</summary>
        public List<(double Value, double Y, string Label)> Ticks { get; set; }
            = new List<(double Value, double Y, string Label)>();

        /// <summary>是否存在负值（用于决定柱体基线）。</summary>
        public bool HasNegative;
    }

    /// <summary>图表布局结果（坐标/刻度/各序列几何），屏幕渲染与导出共用。</summary>
    public sealed class ElChartLayout
    {
        /// <summary>画布尺寸。</summary>
        public double Width, Height;

        /// <summary>绘图区矩形（含四边界）。</summary>
        public double PlotLeft, PlotTop, PlotRight, PlotBottom;

        /// <summary>数值轴刻度（Value/Y、标签）。</summary>
        public List<(double Value, double Y, string Label)> ValueTicks { get; set; } = new List<(double Value, double Y, string Label)>();

        /// <summary>类别轴刻度（Label/X）。</summary>
        public List<(string Label, double X)> CategoryTicks { get; set; } = new List<(string Label, double X)>();

        /// <summary>数值 X 轴刻度（Value/X、标签），仅散点/气泡配置 XValueField 时使用。</summary>
        public List<(double Value, double X, string Label)> XTicks { get; set; } = new List<(double Value, double X, string Label)>();

        /// <summary>各序列渲染几何。</summary>
        public List<ElSeriesRender> Series { get; set; } = new List<ElSeriesRender>();

        /// <summary>图例项（标题/颜色/标记锚点坐标）。</summary>
        public List<(string Title, string Color, double X, double Y)> LegendItems { get; set; } = new List<(string Title, string Color, double X, double Y)>();

        /// <summary>是否无有效数据（仅渲染空提示）。</summary>
        public bool IsEmpty { get; set; }

        /// <summary>是否为径向（饼/环）图表。</summary>
        public bool IsRadial { get; set; }

        /// <summary>是否为雷达图。</summary>
        public bool IsRadar { get; set; }

        // —— 雷达图布局辅助字段 ——
        /// <summary>雷达图中心。</summary>
        public double RadarCx, RadarCy, RadarR, RadarMaxValue;
        /// <summary>雷达图每个辐射轴的角度（度）。</summary>
        public List<double> RadarAngles { get; set; } = new List<double>();
        /// <summary>雷达图辐射轴名称（与 RadarAngles 一一对应）。</summary>
        public List<string> RadarCategories { get; set; } = new List<string>();

        // —— 以下为后续新增能力（次轴 / 参考线 / 标注 / 交互命中） ——

        /// <summary>全部数值轴刻度集（[0] 恒为主轴，其余为命名次轴）。</summary>
        public List<ElAxisScale> ValueScales { get; set; } = new List<ElAxisScale>();

        /// <summary>主轴范围（用于 值↔Y 双向映射；雷达图时为辐射最大值/0）。</summary>
        public double ValueLo, ValueHi;

        /// <summary>参考线几何（X1/Y1→X2/Y2 + 标签 + 颜色 + 线型）。</summary>
        public List<(double X1, double Y1, double X2, double Y2, string Label, string Color, ElLineStyle Style)> RefLines { get; set; }
            = new List<(double X1, double Y1, double X2, double Y2, string Label, string Color, ElLineStyle Style)>();

        /// <summary>参考带几何（矩形 + 标签 + 颜色 + 不透明度）。</summary>
        public List<(double X, double Y, double W, double H, string Label, string Color, double Opacity)> RefBands { get; set; }
            = new List<(double X, double Y, double W, double H, string Label, string Color, double Opacity)>();

        /// <summary>标注几何（锚点 + 文本 + 颜色 + 标记形状）。</summary>
        public List<(double X, double Y, string Text, string Color, ElMarkerType Marker)> Annotations { get; set; }
            = new List<(double X, double Y, string Text, string Color, ElMarkerType Marker)>();

        /// <summary>命中目标集合（供悬停提示；按序列/点序排列）。</summary>
        public List<ElChartHit> Hits { get; set; } = new List<ElChartHit>();

        /// <summary>是否为热力/等值带图（独立布局，使用单元格而非直角坐标系列）。</summary>
        public bool IsHeatmap { get; set; }

        /// <summary>是否收集命中目标（由模型 CollectHits 决定；关闭可省去交互开销）。</summary>
        public bool CollectHitsAllowed { get; set; } = true;

        /// <summary>热力图色阶图例项（值文本 + 颜色），从低到高。</summary>
        public List<(string Label, string Color)> HeatLegend { get; set; } = new List<(string Label, string Color)>();

        /// <summary>饼/环圆心文字（透传自模型；空=不画）。</summary>
        public string CenterText { get; set; } = string.Empty;
    }

    /// <summary>
    /// Element Plus 自渲染版图表引擎（零 Radzen、零 JS 依赖）。
    /// <para>全部几何与 SVG 标记均在 C# 内计算，因此服务端渲染、单元测试与离线打包均可用；
    /// 屏幕渲染（<see cref="ElChartGrow"/>）与 <see cref="RenderSvg"/> 导出共用 <see cref="ComputeLayout"/> 单一真源。</para>
    /// </summary>
    public static class ElChartGrowEngine
    {
        private const double Deg2Rad = Math.PI / 180.0;

        /// <summary>计算图表布局（坐标、刻度、各序列几何）。</summary>
        public static ElChartLayout ComputeLayout(ElChartModel m)
        {
            if (m == null)
            {
                throw new ArgumentNullException(nameof(m));
            }

            var W = Math.Max(1, m.Width);
            var H = Math.Max(1, m.Height);

            var isRadial = m.Series.Any(s => s.Type == ElChartType.Pie || s.Type == ElChartType.Donut);
            var isRadar = m.Series.Any(s => s.Type == ElChartType.Radar || s.Type == ElChartType.SpiderColumn);
            var isHeatmap = m.Series.Any(s => s.Type == ElChartType.Heatmap || s.Type == ElChartType.Contour);
            var useNumericX = m.Series.Any(s => (s.Type == ElChartType.Scatter || s.Type == ElChartType.Bubble)
                && !string.IsNullOrEmpty(s.XValueField));

            // 无有效数据的图：退化为空
            var hasData = m.Series.Any(s => s.Points != null && s.Points.Count > 0);
            if (!hasData)
            {
                return new ElChartLayout
                {
                    Width = W,
                    Height = H,
                    PlotLeft = 8,
                    PlotTop = 8,
                    PlotRight = W - 8,
                    PlotBottom = H - 8,
                    IsEmpty = true,
                    IsRadial = isRadial,
                    IsRadar = isRadar,
                    IsHeatmap = isHeatmap
                };
            }

            double marginLeft = 8, marginRight = 8, marginTop = 8, marginBottom = 8;

            if (!string.IsNullOrEmpty(m.Title))
            {
                marginTop += 26;
            }

            var valueAxis = m.Axes.FirstOrDefault(a => a.Type == ElAxisType.Value);
            var categoryAxis = m.Axes.FirstOrDefault(a => a.Type == ElAxisType.Category);

            // 命名次轴（ValueAxisName 匹配到的命名轴，最多额外绘制 2 条以控版面）
            var secondaryAxes = m.Axes
                .Where(a => a.Type == ElAxisType.Value && !string.IsNullOrWhiteSpace(a.Name))
                .ToList();

            if (m.ShowValueAxis && !isRadial && !isRadar)
            {
                marginLeft += 44 + (string.IsNullOrEmpty(valueAxis?.Title) ? 0 : 16);
            }

            if (isHeatmap)
            {
                // 热力图需要底部类别标签 + 左侧行标签
                marginBottom += 12;
                marginLeft = Math.Max(marginLeft, 64);
            }
            else if (secondaryAxes.Count > 0 && !isRadial && !isRadar)
            {
                marginRight += 44 + (string.IsNullOrEmpty(secondaryAxes[0].Title) ? 0 : 16);
            }

            if (m.ShowCategoryAxis && !isRadial && !isRadar && !useNumericX && !isHeatmap)
            {
                marginBottom += 28 + (string.IsNullOrEmpty(categoryAxis?.Title) ? 0 : 16);
            }

            var legendCount = LegendSliceSeries(m) != null ? Math.Max(1, m.Series[0].Points.Count) : m.Series.Count;
            if (m.ShowLegend)
            {
                // 饼/环的图例逐块列类别（见 LegendItems），版面预留的行数也得按块数算，
                // 不能再按「1 个系列 = 1 行」算——那样四块的环图只留一行高，图例全挤到画布外。
                var legendH = Math.Max(20, legendCount * 22 + 8);
                var legendW = LegendReserveWidth(m);
                switch (m.LegendPosition)
                {
                    case ElLegendPosition.Top:
                        marginTop += legendH;
                        break;
                    case ElLegendPosition.Bottom:
                        marginBottom += legendH;
                        break;
                    case ElLegendPosition.Left:
                        marginLeft += legendW;
                        break;
                    case ElLegendPosition.Right:
                        marginRight += legendW;
                        break;
                }
            }

            var plotLeft = marginLeft;
            var plotTop = marginTop;
            var plotRight = W - marginRight;
            var plotBottom = H - marginBottom;
            var plotW = Math.Max(1, plotRight - plotLeft);
            var plotH = Math.Max(1, plotBottom - plotTop);

            var layout = new ElChartLayout
            {
                Width = W,
                Height = H,
                PlotLeft = plotLeft,
                PlotTop = plotTop,
                PlotRight = plotRight,
                PlotBottom = plotBottom,
                IsRadial = isRadial,
                IsRadar = isRadar,
                IsHeatmap = isHeatmap,
                CenterText = m.CenterText ?? string.Empty,
                CollectHitsAllowed = m.CollectHits
            };

            if (isHeatmap)
            {
                ComputeHeatmap(m, layout, plotLeft, plotTop, plotW, plotH);
            }
            else if (isRadar)
            {
                ComputeRadar(m, layout, plotLeft, plotTop, plotW, plotH, valueAxis);
            }
            else if (isRadial)
            {
                ComputeRadial(m, layout, plotLeft, plotTop, plotW, plotH);
            }
            else
            {
                ComputeCartesian(m, layout, plotLeft, plotTop, plotRight, plotBottom, plotW, plotH, valueAxis, categoryAxis, useNumericX, secondaryAxes);
            }

            if (!isHeatmap)
            {
                ComputeReferenceGeometry(m, layout);
                ComputeAnnotationGeometry(m, layout, useNumericX);
            }

            if (m.ShowLegend)
            {
                ComputeLegend(m, layout, plotLeft, plotTop, plotRight, plotBottom, marginLeft, marginRight, marginTop, marginBottom);
            }

            return layout;
        }

        private static void ComputeCartesian(
            ElChartModel m, ElChartLayout layout,
            double plotLeft, double plotTop, double plotRight, double plotBottom, double plotW, double plotH,
            ElChartAxisModel valueAxis, ElChartAxisModel categoryAxis, bool useNumericX,
            List<ElChartAxisModel> secondaryAxes)
        {
            var cartesian = m.Series.Where(s => !IsNonCartesian(s.Type)).ToList();

            // 类别轴：保持出现顺序去重
            var categories = new List<string>();
            foreach (var s in cartesian)
            {
                foreach (var p in s.Points)
                {
                    if (!categories.Contains(p.Category))
                    {
                        categories.Add(p.Category);
                    }
                }
            }

            var isFullStacked = cartesian.Any(s => s.Type == ElChartType.FullStackedColumn || s.Type == ElChartType.FullStackedBar
                || s.Type == ElChartType.FullStackedArea || s.Type == ElChartType.FullStackedLine);
            var isStacked = cartesian.Any(s => s.Type == ElChartType.StackedColumn || s.Type == ElChartType.StackedBar
                || s.Type == ElChartType.StackedArea || s.Type == ElChartType.StackedLine
                || s.Type == ElChartType.FullStackedColumn || s.Type == ElChartType.FullStackedBar
                || s.Type == ElChartType.FullStackedArea || s.Type == ElChartType.FullStackedLine);

            // 主轴量程只由未绑定命名次轴的序列决定（绑定了次轴的序列有自己的量程）
            var primarySeries = cartesian.Where(x => string.IsNullOrEmpty(x.ValueAxisName)).ToList();

            // 100% 堆叠的每类别总量
            var fsTotal = new Dictionary<string, double>();
            if (isFullStacked)
            {
                foreach (var cat in categories)
                {
                    double sum = 0;
                    foreach (var s in primarySeries.Where(x => IsStackedFamily(x.Type)))
                    {
                        var pt = s.Points.FirstOrDefault(p => p.Category == cat);
                        if (pt != null)
                        {
                            sum += Math.Max(0, pt.Value);
                        }
                    }
                    fsTotal[cat] = sum;
                }
            }

            // —— 数值范围：覆盖全部系列族（含多值槽与累计型） ——
            double rawMax = 0, rawMin = 0;
            var hasValue = false;
            void Consider(double v)
            {
                if (double.IsNaN(v) || double.IsInfinity(v))
                {
                    return;
                }

                // 首个有效值直接对 lo/hi 打底（此前 rawMin 恒为 0，会让「贴数据」的图形被强行拉到 0）
                if (!hasValue)
                {
                    rawMax = v;
                    rawMin = v;
                    hasValue = true;
                    return;
                }

                if (v > rawMax) rawMax = v;
                if (v < rawMin) rawMin = v;
            }

            foreach (var s in primarySeries)
            {
                foreach (var p in s.Points)
                {
                    switch (s.Type)
                    {
                        case ElChartType.Candlestick:
                        case ElChartType.Ohlc:
                            Consider(p.Value); Consider(p.Value2); Consider(p.Value3); Consider(p.Value4);
                            break;
                        case ElChartType.HighLow:
                            Consider(p.Value); Consider(p.Value2);
                            if (!string.IsNullOrEmpty(s.ValueField3)) { Consider(p.Value3); }
                            break;
                        case ElChartType.BoxPlot:
                            Consider(p.Value); Consider(p.Value2); Consider(p.Value3); Consider(p.Value4); Consider(p.Value5);
                            break;
                        case ElChartType.RangeColumn:
                        case ElChartType.RangeBar:
                        case ElChartType.RangeArea:
                        case ElChartType.Bullet:
                            Consider(p.Value); Consider(p.Value2);
                            break;
                        default:
                            if (!isStacked)
                            {
                                Consider(p.Value);
                            }
                            break;
                    }
                }
            }

            // 瀑布图按累计值定范围
            foreach (var s in primarySeries.Where(x => x.Type == ElChartType.Waterfall || x.Type == ElChartType.HorizontalWaterfall))
            {
                double cum = 0;
                foreach (var p in s.Points)
                {
                    if (p.IsTotal)
                    {
                        Consider(0); Consider(cum);
                    }
                    else
                    {
                        var next = cum + p.Value;
                        Consider(cum); Consider(next);
                        cum = next;
                    }
                }
            }

            if (isStacked)
            {
                foreach (var cat in categories)
                {
                    double sum = 0;
                    foreach (var s in primarySeries.Where(x => IsStackedFamily(x.Type)))
                    {
                        var pt = s.Points.FirstOrDefault(p => p.Category == cat);
                        if (pt != null)
                        {
                            sum += pt.Value;
                        }
                    }
                    Consider(isFullStacked && sum != 0 ? 100 : sum);
                }
            }

            var anyNegative = rawMin < 0;
            var dataMax = rawMax;

            double lo, hi, step;
            if (isFullStacked)
            {
                lo = valueAxis?.Min ?? 0;
                hi = valueAxis?.Max ?? 100;
                step = valueAxis?.Step ?? 20;
            }
            else if (valueAxis != null && (valueAxis.Min.HasValue || valueAxis.Max.HasValue))
            {
                lo = valueAxis.Min ?? (anyNegative ? dataMax : 0);
                hi = valueAxis.Max ?? dataMax;
                step = valueAxis.Step ?? (hi > lo ? (hi - lo) / 5.0 : 1);
            }
            else
            {
                // 零基线规则（对齐 Radzen 的取舍）：
                // 柱 / 条 / 堆叠 / 瀑布 / 子弹这类「从基线长出来」的图形必须有 0 基线，否则柱高会骗人；
                // K 线 / OHLC / 高低 / 箱线 / 范围这类「量值本身有意义」的图形则必须贴合数据 ——
                // 强行从 0 起会把 41~47 的股价压成顶部一条缝（Radzen 的 Scale 只取数据域）。
                var zeroBased = primarySeries.Any(s => NeedsZeroBaseline(s.Type));
                var lower = anyNegative ? rawMin : (zeroBased || !hasValue ? 0 : rawMin);
                var (niceLo, niceHi, niceStep) = NiceScale(lower, dataMax, 5);
                lo = niceLo;
                hi = niceHi;
                step = niceStep;
            }

            if (hi <= lo)
            {
                hi = lo + 1;
            }

            // 数值刻度（含 0 边界）+ 命名次轴刻度
            var span = hi - lo;
            var primary = MakeScale(string.Empty, valueAxis?.Title ?? string.Empty, false, valueAxis, lo, hi, step, plotBottom, plotH, anyNegative);
            layout.ValueTicks = primary.Ticks;
            layout.ValueScales.Add(primary);
            layout.ValueLo = lo;
            layout.ValueHi = hi;

            foreach (var ax in secondaryAxes)
            {
                var bound = cartesian.Where(s => string.Equals(s.ValueAxisName, ax.Name, StringComparison.Ordinal)).ToList();
                if (bound.Count == 0)
                {
                    continue;
                }

                double sMax = 0, sMin = 0;
                foreach (var s in bound)
                {
                    foreach (var p in s.Points)
                    {
                        foreach (var vv in new[] { p.Value, p.Value2, p.Value3, p.Value4, p.Value5 })
                        {
                            if (double.IsNaN(vv) || double.IsInfinity(vv)) { continue; }
                            if (vv > sMax) { sMax = vv; }
                            if (vv < sMin) { sMin = vv; }
                        }
                    }
                }

                double slo, shi, sstep;
                if (ax.Min.HasValue || ax.Max.HasValue)
                {
                    slo = ax.Min ?? (sMin < 0 ? sMin : 0);
                    shi = ax.Max ?? sMax;
                    sstep = ax.Step ?? (shi > slo ? (shi - slo) / 5.0 : 1);
                }
                else
                {
                    var (na, nb, nc) = NiceScale(sMin < 0 ? sMin : 0, sMax, 5);
                    slo = na; shi = nb; sstep = nc;
                }

                if (shi <= slo)
                {
                    shi = slo + 1;
                }

                layout.ValueScales.Add(MakeScale(ax.Name, ax.Title ?? string.Empty, true, ax, slo, shi, sstep, plotBottom, plotH, sMin < 0));
            }

            // 数值 X 轴刻度（散点/气泡启用 XValueField 时）
            if (useNumericX)
            {
                var xSeries = cartesian.Where(s => (s.Type == ElChartType.Scatter || s.Type == ElChartType.Bubble)).ToList();
                var xMin = xSeries.SelectMany(s => s.Points).Min(p => p.X);
                var xMax = xSeries.SelectMany(s => s.Points).Max(p => p.X);
                var (nxLo, nxHi, nxStep) = NiceScale(xMin, xMax, 5);
                var nxSpan = nxHi - nxLo;
                var xTicks = new List<(double Value, double X, string Label)>();
                for (var v = nxLo; v <= nxHi + 1e-9; v += nxStep)
                {
                    var px = plotLeft + (v - nxLo) / nxSpan * plotW;
                    xTicks.Add((v, px, FormatTick(v, null)));
                }
                layout.XTicks = xTicks;
                layout.RadarMaxValue = nxLo; // 借用字段存储 xLo，供渲染映射
                layout.RadarR = nxHi;        // 借用字段存储 xHi
                layout.RadarCx = nxSpan;     // 借用字段存储 xSpan
            }

            // 类别刻度（数值 X 轴时不存在类别刻度）
            var catTicks = new List<(string Label, double X)>();
            var band = categories.Count > 0 ? plotW / categories.Count : plotW;
            if (!useNumericX)
            {
                for (var i = 0; i < categories.Count; i++)
                {
                    var cx = plotLeft + (i + 0.5) * band;
                    catTicks.Add((categories[i], cx));
                }
            }
            layout.CategoryTicks = catTicks;

            // 气泡半径缩放基准
            double maxBubble = 0;
            if (cartesian.Any(s => s.Type == ElChartType.Bubble))
            {
                maxBubble = cartesian.Where(s => s.Type == ElChartType.Bubble).SelectMany(s => s.Points).Select(p => p.Size).DefaultIfEmpty(0).Max();
            }

            // 数值 X 轴映射
            double xLo = useNumericX ? layout.RadarMaxValue : 0;
            double xHi = useNumericX ? layout.RadarR : 1;
            double xSpan = useNumericX ? layout.RadarCx : 1;

            // 各序列几何
            var stackedBase = new Dictionary<string, double>(); // category -> 已累加高度（数值）
            foreach (var s in cartesian)
            {
                var render = new ElSeriesRender
                {
                    Type = s.Type,
                    Title = s.Title,
                    Color = s.Color,
                    LineStyle = s.LineStyle,
                    MarkerType = s.MarkerType,
                    ShowDataLabels = s.ShowDataLabels,
                    TrendStyle = s.TrendLineStyle,
                    TrendColor = string.IsNullOrEmpty(s.TrendLineColor) ? s.Color : s.TrendLineColor
                };

                // 该序列绑定的数值轴（空=主轴）
                var sc = layout.ValueScales.FirstOrDefault(x => string.Equals(x.Name ?? string.Empty, s.ValueAxisName ?? string.Empty, StringComparison.Ordinal))
                         ?? layout.ValueScales[0];
                var seriesIndex = layout.Series.Count;
                var ptIndex = 0;
                var sSpan = Math.Max(1e-9, sc.Hi - sc.Lo);
                double YFor(double v) => plotBottom - (v - sc.Lo) / sSpan * plotH;
                double XFor(double v) => plotLeft + (v - sc.Lo) / sSpan * plotW;
                double ValOf(ElChartPoint p) => isFullStacked ? FullStackedValue(p, fsTotal) : p.Value;

                var isColumn = s.Type == ElChartType.Column || s.Type == ElChartType.StackedColumn || s.Type == ElChartType.FullStackedColumn;
                var isBar = s.Type == ElChartType.Bar || s.Type == ElChartType.StackedBar || s.Type == ElChartType.FullStackedBar;
                var isLine = s.Type == ElChartType.Line || s.Type == ElChartType.StackedLine;
                var isArea = s.Type == ElChartType.Area || s.Type == ElChartType.StackedArea;
                var isAreaLine = isLine || isArea || s.Type == ElChartType.FullStackedLine || s.Type == ElChartType.FullStackedArea;
                var isScatter = s.Type == ElChartType.Scatter;
                var isBubble = s.Type == ElChartType.Bubble;
                var isBullet = s.Type == ElChartType.Bullet;
                var sIsStacked = IsStackedFamily(s.Type);
                var isRangeColumn = s.Type == ElChartType.RangeColumn;
                var isRangeBar = s.Type == ElChartType.RangeBar;
                var isRangeArea = s.Type == ElChartType.RangeArea;
                var isFunnelLike = s.Type == ElChartType.Funnel || s.Type == ElChartType.Pyramid;
                var isWaterfall = s.Type == ElChartType.Waterfall || s.Type == ElChartType.HorizontalWaterfall;

                if (isColumn || isBar)
                {
                    var barW = Math.Min(band * 0.7, 60);
                    for (var i = 0; i < categories.Count; i++)
                    {
                        var cat = categories[i];
                        var pt = s.Points.FirstOrDefault(p => p.Category == cat);
                        if (pt == null)
                        {
                            continue;
                        }

                        double v = ValOf(pt);
                        double vLo, vHi;
                        if (sIsStacked)
                        {
                            var baseV = stackedBase.TryGetValue(cat, out var b) ? b : 0;
                            vLo = Math.Min(baseV, baseV + v);
                            vHi = Math.Max(baseV, baseV + v);
                            stackedBase[cat] = baseV + v;
                        }
                        else
                        {
                            vLo = Math.Min(0, v);
                            vHi = Math.Max(0, v);
                        }

                        var yHi = YFor(vHi);
                        var yLo = YFor(vLo);
                        var h = Math.Max(0.5, yLo - yHi);
                        var cx = plotLeft + (i + 0.5) * band;

                        if (isColumn)
                        {
                            render.Bars.Add((cx - barW / 2, yHi, barW, h, v));
                            AddBarHit(layout, render, s, pt, seriesIndex, ptIndex++, cx, yHi + h / 2, barW, h, pt.Value);
                        }
                        else // bar（水平）
                        {
                            var xHi2 = XFor(vHi);
                            var xLo2 = XFor(vLo);
                            var w = Math.Max(0.5, xHi2 - xLo2);
                            var by = plotTop + (i + 0.15) * band;
                            var bh = band * 0.7;
                            render.Bars.Add((xLo2, by, w, bh, v));
                            AddBarHit(layout, render, s, pt, seriesIndex, ptIndex++, xLo2 + w / 2, by + bh / 2, w, bh, pt.Value);
                        }
                    }
                }
                else if (isScatter || isBubble)
                {
                    var pts = new List<(double X, double Y, double Size)>();
                    foreach (var p in s.Points)
                    {
                        double cx;
                        if (useNumericX)
                        {
                            cx = plotLeft + (p.X - xLo) / xSpan * plotW;
                        }
                        else
                        {
                            var idx = categories.IndexOf(p.Category);
                            if (idx < 0) idx = 0;
                            cx = plotLeft + (idx + 0.5) * band;
                        }

                        double y = YFor(p.Value);
                        double size = 0;
                        if (isBubble)
                        {
                            size = maxBubble <= 0 ? 5 : (3 + (p.Size / maxBubble) * 15);
                        }

                        pts.Add((cx, y, size));
                        AddPointHit(layout, render, s, p, seriesIndex, ptIndex++, cx, y, Math.Max(size, 8));
                    }

                    render.Points = pts;
                }
                else if (isRangeArea)
                {
                    var hiPts = new List<(double X, double Y, double Size)>();
                    var loPts = new List<(double X, double Y, double Size)>();
                    for (var i = 0; i < categories.Count; i++)
                    {
                        var pt = s.Points.FirstOrDefault(p => p.Category == categories[i]);
                        if (pt == null)
                        {
                            continue;
                        }

                        var cx = plotLeft + (i + 0.5) * band;
                        hiPts.Add((cx, YFor(pt.Value2), 0));
                        loPts.Add((cx, YFor(pt.Value), 0));
                        AddPointHit(layout, render, s, pt, seriesIndex, ptIndex++, cx, (YFor(pt.Value2) + YFor(pt.Value)) / 2, 9);
                    }

                    render.Points = hiPts;
                    render.Path = BuildBandPath(hiPts, loPts);
                }
                else if (isFunnelLike)
                {
                    var ordered = s.Points
                        .Select(p => (Point: p, Val: p.Value))
                        .OrderByDescending(x => s.Type == ElChartType.Funnel ? x.Val : -x.Val)
                        .ToList();
                    var n = Math.Max(1, ordered.Count);
                    var rowH = plotH / n;
                    var maxV = ordered.Count > 0 ? ordered.Max(x => Math.Abs(x.Val)) : 0;
                    if (maxV <= 0)
                    {
                        maxV = 1;
                    }

                    double WidthAt(int idx)
                    {
                        if (ordered.Count == 0)
                        {
                            return 0;
                        }
                        var k = Math.Min(idx, ordered.Count - 1);
                        if (k < 0)
                        {
                            k = 0;
                        }
                        return Math.Abs(ordered[k].Val) / maxV * plotW * 0.94;
                    }

                    var midX = plotLeft + plotW / 2.0;
                    for (var i = 0; i < ordered.Count; i++)
                    {
                        var topW = WidthAt(i);
                        var botW = WidthAt(i + 1);
                        if (i == ordered.Count - 1)
                        {
                            botW = topW;
                        }

                        var yTop = plotTop + i * rowH;
                        var yBot = yTop + rowH;
                        var pts = string.Join(" ", new[]
                        {
                            Fmt(midX - topW / 2) + "," + Fmt(yTop),
                            Fmt(midX + topW / 2) + "," + Fmt(yTop),
                            Fmt(midX + botW / 2) + "," + Fmt(yBot),
                            Fmt(midX - botW / 2) + "," + Fmt(yBot)
                        });
                        var labelText = ((ordered[i].Point.Category ?? string.Empty) + "  " + FmtValue(ordered[i].Val, s.ValueFormat)).Trim();
                        var polyFill = MixHex(s.Color, "#ffffff", i * 0.10);
                        render.Polygons.Add((pts, ordered[i].Val, labelText));
                        render.Boxes.Add((midX - Math.Max(topW, botW) / 2, yTop, Math.Max(topW, botW), rowH, polyFill, "band"));
                        layout.Hits.Add(new ElChartHit
                        {
                            SeriesIndex = seriesIndex,
                            PointIndex = i,
                            Title = s.Title,
                            Color = polyFill,
                            Category = ordered[i].Point.Category ?? string.Empty,
                            ValueText = FmtValue(ordered[i].Val, s.ValueFormat),
                            X = midX,
                            Y = yTop + rowH / 2,
                            W = Math.Max(topW, botW),
                            H = rowH
                        });
                    }
                }
                else if (isBullet || isWaterfall || isRangeColumn || isRangeBar
                    || s.Type == ElChartType.Candlestick || s.Type == ElChartType.Ohlc
                    || s.Type == ElChartType.HighLow || s.Type == ElChartType.BoxPlot)
                {
                    ComputeSpecialCartesian(s, render, layout, categories, band, plotLeft, plotTop, plotW, plotH,
                        YFor, XFor, seriesIndex, sSpan);
                }
                else // line / area（含堆叠与 100% 堆叠）
                {
                    var pts = new List<(double X, double Y, double Size)>();
                    for (var i = 0; i < categories.Count; i++)
                    {
                        var cat = categories[i];
                        var pt = s.Points.FirstOrDefault(p => p.Category == cat);
                        if (pt == null)
                        {
                            continue;
                        }

                        double v = ValOf(pt);
                        if (sIsStacked)
                        {
                            var baseV = stackedBase.TryGetValue(cat, out var b) ? b : 0;
                            v = baseV + v;
                            stackedBase[cat] = v;
                        }

                        var y = YFor(v);
                        var cx = plotLeft + (i + 0.5) * band;
                        pts.Add((cx, y, 0));
                        AddPointHit(layout, render, s, pt, seriesIndex, ptIndex++, cx, y, 8);
                    }

                    render.Points = pts;
                    render.Path = BuildLinePath(pts, isArea || s.Type == ElChartType.FullStackedArea, plotBottom);
                }

                // 趋势线（对齐 Radzen 的 TrendLine / MovingAverage / Polynomial / Mean / Median / Mode）
                if (s.TrendLine != ElTrendLineType.None && render.Points.Count > 1)
                {
                    render.TrendPath = BuildTrendPath(render.Points, s);
                }

                layout.Series.Add(render);
            }
        }

        /// <summary>笛卡尔坐标系内的「箱/蜡烛/范围/瀑布/子弹」几何计算。</summary>
        private static void ComputeSpecialCartesian(
            ElChartSeriesModel s, ElSeriesRender render, ElChartLayout layout, List<string> categories, double band,
            double plotLeft, double plotTop, double plotW, double plotH,
            Func<double, double> yFor, Func<double, double> xFor, int seriesIndex, double sSpan)
        {
            var barW = Math.Min(band * 0.6, 48);
            var cum = 0.0;
            var prevTop = double.NaN;

            for (var i = 0; i < categories.Count; i++)
            {
                var cat = categories[i];
                var p = s.Points.FirstOrDefault(x => x.Category == cat);
                if (p == null)
                {
                    continue;
                }

                var cx = plotLeft + (i + 0.5) * band;
                switch (s.Type)
                {
                    case ElChartType.Candlestick:
                    case ElChartType.Ohlc:
                    {
                        var yO = yFor(p.Value);
                        var yH = yFor(p.Value2);
                        var yL = yFor(p.Value3);
                        var yC = yFor(p.Value4);
                        var rising = p.Value4 >= p.Value;
                        var col = rising ? s.RisingColor : s.FallingColor;
                        render.Segments.Add((cx, yH, cx, yL, "wick"));
                        if (s.Type == ElChartType.Candlestick)
                        {
                            var top = Math.Min(yO, yC);
                            var bot = Math.Max(yO, yC);
                            render.Boxes.Add((cx - barW / 2, top, barW, Math.Max(1, bot - top), col, "candle"));
                        }
                        else
                        {
                            render.Segments.Add((cx - barW * 0.6, yO, cx, yO, "open"));
                            render.Segments.Add((cx, yC, cx + barW * 0.6, yC, "close"));
                        }

                        render.Points.Add((cx, (yO + yC) / 2, 0));
                        layout.Hits.Add(new ElChartHit
                        {
                            SeriesIndex = seriesIndex,
                            PointIndex = i,
                            Title = s.Title,
                            Color = col,
                            Category = p.Category ?? string.Empty,
                            ValueText = "开 " + FmtValue(p.Value, s.ValueFormat) + "  高 " + FmtValue(p.Value2, s.ValueFormat)
                                        + "  低 " + FmtValue(p.Value3, s.ValueFormat) + "  收 " + FmtValue(p.Value4, s.ValueFormat),
                            X = cx,
                            Y = (yO + yC) / 2,
                            W = Math.Max(barW, 10),
                            H = Math.Max(6, Math.Abs(yO - yC))
                        });
                        break;
                    }

                    case ElChartType.HighLow:
                    {
                        var yHigh = yFor(p.Value);
                        var yLow = yFor(p.Value2);
                        var col = p.Value2 > p.Value ? s.RisingColor : s.FallingColor;
                        render.Segments.Add((cx, yHigh, cx, yLow, "wick"));
                        if (!string.IsNullOrEmpty(s.ValueField3))
                        {
                            var yClose = yFor(p.Value3);
                            render.Segments.Add((cx - barW * 0.55, yClose, cx + barW * 0.55, yClose, "close"));
                        }

                        render.Points.Add((cx, (yHigh + yLow) / 2, 0));
                        layout.Hits.Add(new ElChartHit
                        {
                            SeriesIndex = seriesIndex,
                            PointIndex = i,
                            Title = s.Title,
                            Color = col,
                            Category = p.Category ?? string.Empty,
                            ValueText = "高 " + FmtValue(p.Value, s.ValueFormat) + "  低 " + FmtValue(p.Value2, s.ValueFormat),
                            X = cx,
                            Y = (yHigh + yLow) / 2,
                            W = 12,
                            H = Math.Max(6, Math.Abs(yHigh - yLow))
                        });
                        break;
                    }

                    case ElChartType.BoxPlot:
                    {
                        var yMin = yFor(p.Value);
                        var yQ1 = yFor(p.Value2);
                        var yMed = yFor(p.Value3);
                        var yQ3 = yFor(p.Value4);
                        var yMax = yFor(p.Value5);
                        render.Boxes.Add((cx - barW / 2, Math.Min(yQ1, yQ3), barW, Math.Max(1, Math.Abs(yQ3 - yQ1)), s.Color, "box"));
                        render.Segments.Add((cx, yQ1, cx, yMax, "whisker"));
                        render.Segments.Add((cx, yMin, cx, yQ3, "whisker"));
                        render.Segments.Add((cx - barW / 4, yMax, cx + barW / 4, yMax, "cap"));
                        render.Segments.Add((cx - barW / 4, yMin, cx + barW / 4, yMin, "cap"));
                        render.Segments.Add((cx - barW / 2, yMed, cx + barW / 2, yMed, "median"));
                        render.Points.Add((cx, yMed, 0));
                        layout.Hits.Add(new ElChartHit
                        {
                            SeriesIndex = seriesIndex,
                            PointIndex = i,
                            Title = s.Title,
                            Color = s.Color,
                            Category = p.Category ?? string.Empty,
                            ValueText = "最小 " + FmtValue(p.Value, s.ValueFormat) + "  Q1 " + FmtValue(p.Value2, s.ValueFormat)
                                        + "  中位 " + FmtValue(p.Value3, s.ValueFormat) + "  Q3 " + FmtValue(p.Value4, s.ValueFormat)
                                        + "  最大 " + FmtValue(p.Value5, s.ValueFormat),
                            X = cx,
                            Y = yMed,
                            W = Math.Max(barW, 10),
                            H = Math.Max(6, Math.Abs(yMax - yMin))
                        });
                        break;
                    }

                    case ElChartType.RangeColumn:
                    case ElChartType.RangeBar:
                    {
                        var isVertical = s.Type == ElChartType.RangeColumn;
                        double v1 = p.Value, v2 = p.Value2;
                        if (isVertical)
                        {
                            var ya = yFor(v1);
                            var yb = yFor(v2);
                            render.Bars.Add((cx - barW / 2, Math.Min(ya, yb), barW, Math.Max(1, Math.Abs(yb - ya)), v2));
                            render.Segments.Add((cx - barW / 2, ya, cx + barW / 2, ya, "cap"));
                            render.Segments.Add((cx - barW / 2, yb, cx + barW / 2, yb, "cap"));
                            layout.Hits.Add(new ElChartHit
                            {
                                SeriesIndex = seriesIndex, PointIndex = i, Title = s.Title, Color = s.Color,
                                Category = p.Category ?? string.Empty,
                                ValueText = FmtValue(v1, s.ValueFormat) + " ~ " + FmtValue(v2, s.ValueFormat),
                                X = cx, Y = (ya + yb) / 2, W = Math.Max(barW, 10), H = Math.Max(6, Math.Abs(yb - ya))
                            });
                        }
                        else
                        {
                            var xa = xFor(v1);
                            var xb = xFor(v2);
                            var by = plotTop + (i + 0.15) * band;
                            var bh = band * 0.6;
                            render.Bars.Add((Math.Min(xa, xb), by, Math.Max(1, Math.Abs(xb - xa)), bh, v2));
                            render.Segments.Add((xa, by, xa, by + bh, "cap"));
                            render.Segments.Add((xb, by, xb, by + bh, "cap"));
                            layout.Hits.Add(new ElChartHit
                            {
                                SeriesIndex = seriesIndex, PointIndex = i, Title = s.Title, Color = s.Color,
                                Category = p.Category ?? string.Empty,
                                ValueText = FmtValue(v1, s.ValueFormat) + " ~ " + FmtValue(v2, s.ValueFormat),
                                X = (xa + xb) / 2, Y = by + bh / 2, W = Math.Max(6, Math.Abs(xb - xa)), H = Math.Max(bh, 10)
                            });
                        }
                        break;
                    }

                    case ElChartType.Waterfall:
                    case ElChartType.HorizontalWaterfall:
                    {
                        var isVertical = s.Type == ElChartType.Waterfall;
                        double vLo, vHi;
                        if (p.IsTotal)
                        {
                            vLo = 0;
                            vHi = cum;
                        }
                        else
                        {
                            vLo = cum;
                            vHi = cum + p.Value;
                            cum = vHi;
                        }

                        var col = p.IsTotal ? s.Color : (p.Value >= 0 ? s.RisingColor : s.FallingColor);
                        if (isVertical)
                        {
                            var ya = yFor(vLo);
                            var yb = yFor(vHi);
                            render.Bars.Add((cx - barW / 2, Math.Min(ya, yb), barW, Math.Max(1, Math.Abs(yb - ya)), p.Value));
                            var topY = Math.Min(ya, yb);
                            if (!double.IsNaN(prevTop))
                            {
                                render.Segments.Add((cx - band, prevTop, cx - barW / 2, topY, "connector"));
                            }
                            prevTop = topY;
                            layout.Hits.Add(new ElChartHit
                            {
                                SeriesIndex = seriesIndex, PointIndex = i, Title = s.Title, Color = col,
                                Category = p.Category ?? string.Empty,
                                ValueText = FmtValue(p.Value, s.ValueFormat) + (p.IsTotal ? "（小计）" : string.Empty),
                                X = cx, Y = topY + Math.Abs(yb - ya) / 2, W = Math.Max(barW, 10), H = Math.Max(6, Math.Abs(yb - ya))
                            });
                        }
                        else
                        {
                            var xa = xFor(vLo);
                            var xb = xFor(vHi);
                            var by = plotTop + (i + 0.15) * band;
                            var bh = band * 0.6;
                            render.Bars.Add((Math.Min(xa, xb), by, Math.Max(1, Math.Abs(xb - xa)), bh, p.Value));
                            var leftX = Math.Min(xa, xb);
                            if (!double.IsNaN(prevTop))
                            {
                                render.Segments.Add((prevTop, by - band + bh, leftX, by, "connector"));
                            }
                            prevTop = leftX;
                            layout.Hits.Add(new ElChartHit
                            {
                                SeriesIndex = seriesIndex, PointIndex = i, Title = s.Title, Color = col,
                                Category = p.Category ?? string.Empty,
                                ValueText = FmtValue(p.Value, s.ValueFormat) + (p.IsTotal ? "（小计）" : string.Empty),
                                X = leftX + Math.Abs(xb - xa) / 2, Y = by + bh / 2, W = Math.Max(6, Math.Abs(xb - xa)), H = Math.Max(bh, 10)
                            });
                        }
                        break;
                    }

                    case ElChartType.Bullet:
                    {
                        var by = plotTop + (i + 0.15) * band;
                        var bh = band * 0.6;
                        var rowTop = plotTop + i * band + band * 0.15;
                        var rowH = band * 0.7;
                        var x1 = xFor(p.Value);
                        var x0 = xFor(0);
                        var barThick = Math.Max(3, rowH * 0.42);
                        // 三档背景（按轴范围三等分）
                        var b1 = plotLeft + plotW / 3.0;
                        var b2 = plotLeft + plotW * 2.0 / 3.0;
                        render.Boxes.Add((plotLeft, rowTop, b1 - plotLeft, rowH, "#f0f2f5", "band"));
                        render.Boxes.Add((b1, rowTop, b2 - b1, rowH, "#e6e8eb", "band"));
                        render.Boxes.Add((b2, rowTop, plotLeft + plotW - b2, rowH, "#d9dce0", "band"));
                        render.Bars.Add((Math.Min(x0, x1), rowTop + (rowH - barThick) / 2, Math.Max(1, Math.Abs(x1 - x0)), barThick, p.Value));
                        // 目标刻度线
                        var xt = xFor(p.Value2);
                        render.Segments.Add((xt, rowTop, xt, rowTop + rowH, "target"));
                        layout.Hits.Add(new ElChartHit
                        {
                            SeriesIndex = seriesIndex, PointIndex = i, Title = s.Title, Color = s.Color,
                            Category = p.Category ?? string.Empty,
                            ValueText = "值 " + FmtValue(p.Value, s.ValueFormat) + "  目标 " + FmtValue(p.Value2, s.ValueFormat),
                            X = plotLeft + plotW / 2, Y = rowTop + rowH / 2, W = plotW, H = rowH
                        });
                        break;
                    }
                }
            }
        }

        // ——————————————————————————————————————————————————————————
        // 新增能力：类型判定 / 刻度 / 命中 / 趋势线 / 色彩 / 热力 / 参考元素
        // ——————————————————————————————————————————————————————————

        /// <summary>是否为非笛卡尔类型（饼/环/雷达/蜘蛛柱/热力/等值带）。</summary>
        public static bool IsNonCartesian(ElChartType t)
            => t == ElChartType.Pie || t == ElChartType.Donut || t == ElChartType.Radar
               || t == ElChartType.SpiderColumn || t == ElChartType.Heatmap || t == ElChartType.Contour;

        /// <summary>是否为堆叠族（含 100% 堆叠）。</summary>
        public static bool IsStackedFamily(ElChartType t)
            => t == ElChartType.StackedColumn || t == ElChartType.StackedBar
               || t == ElChartType.StackedArea || t == ElChartType.StackedLine
               || t == ElChartType.FullStackedColumn || t == ElChartType.FullStackedBar
               || t == ElChartType.FullStackedArea || t == ElChartType.FullStackedLine;

        /// <summary>
        /// 数值轴是否必须包含 0 基线。
        /// 柱 / 条 / 堆叠 / 瀑布 / 子弹这类「从基线长出来」的图形：柱高必须与数值成正比，故必须有 0；
        /// K 线 / OHLC / 高低 / 箱线 / 范围这类「量值本身有意义」的图形：贴合数据域（对齐 Radzen 的 Scale 语义），
        /// 否则 41~47 的股价会被压成绘图区顶部的一条缝。
        /// </summary>
        public static bool NeedsZeroBaseline(ElChartType t)
            => t == ElChartType.Column || t == ElChartType.Bar
               || t == ElChartType.StackedColumn || t == ElChartType.StackedBar
               || t == ElChartType.FullStackedColumn || t == ElChartType.FullStackedBar
               || t == ElChartType.Waterfall || t == ElChartType.HorizontalWaterfall
               || t == ElChartType.Bullet;

        /// <summary>100% 堆叠下的归一化值（按类别总量缩放为 0..100）。</summary>
        private static double FullStackedValue(ElChartPoint p, Dictionary<string, double> totals)
        {
            if (p == null || totals == null)
            {
                return 0;
            }

            if (!totals.TryGetValue(p.Category ?? string.Empty, out var t) || t <= 0)
            {
                return 0;
            }

            return Math.Max(0, p.Value) / t * 100.0;
        }

        /// <summary>构造数值轴刻度集（主轴与次轴共用）。</summary>
        private static ElAxisScale MakeScale(string name, string title, bool onRight, ElChartAxisModel def,
            double lo, double hi, double step, double plotBottom, double plotH, bool hasNeg)
        {
            var scale = new ElAxisScale
            {
                Name = name ?? string.Empty,
                Title = title ?? string.Empty,
                Lo = lo,
                Hi = hi,
                Step = step,
                OnRight = onRight,
                HasNegative = hasNeg
            };

            var span = hi - lo;
            if (span <= 0)
            {
                span = 1;
            }

            for (var v = lo; v <= hi + 1e-9; v += step)
            {
                scale.Ticks.Add((v, plotBottom - (v - lo) / span * plotH, FormatTick(v, def)));
            }

            return scale;
        }

        /// <summary>数值格式化（空格式时自动去掉无意义的小数）。</summary>
        private static string FmtValue(double v, string fmt)
        {
            if (!string.IsNullOrWhiteSpace(fmt))
            {
                try
                {
                    return v.ToString(fmt, CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    // 非法格式回退默认
                }
            }

            return Math.Abs(v - Math.Round(v)) < 1e-9
                ? ((long)Math.Round(v)).ToString(CultureInfo.InvariantCulture)
                : v.ToString("0.###", CultureInfo.InvariantCulture);
        }

        /// <summary>#rrggbb 线性混合（t=0 取 a，t=1 取 b）。</summary>
        private static string MixHex(string a, string b, double t)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            {
                return string.IsNullOrWhiteSpace(a) ? b : a;
            }

            if (t <= 0)
            {
                return a;
            }

            if (t >= 1)
            {
                return b;
            }

            if (!TryParseHex(a, out var r1, out var g1, out var b1) || !TryParseHex(b, out var r2, out var g2, out var b2))
            {
                return a;
            }

            var r = (int)Math.Round(r1 + (r2 - r1) * t);
            var g = (int)Math.Round(g1 + (g2 - g1) * t);
            var bl = (int)Math.Round(b1 + (b2 - b1) * t);
            return "#" + r.ToString("x2") + g.ToString("x2") + bl.ToString("x2");
        }

        private static bool TryParseHex(string hex, out int r, out int g, out int b)
        {
            r = g = b = 0;
            if (string.IsNullOrWhiteSpace(hex))
            {
                return false;
            }

            var s = hex.Trim().TrimStart('#');
            if (s.Length == 3)
            {
                s = string.Concat(s[0], s[0], s[1], s[1], s[2], s[2]);
            }

            if (s.Length != 6)
            {
                return false;
            }

            return int.TryParse(s.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r)
                   && int.TryParse(s.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)
                   && int.TryParse(s.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
        }

        /// <summary>高/低两条折线围成的闭合带（用于范围面积图）。</summary>
        private static string BuildBandPath(List<(double X, double Y, double Size)> upper, List<(double X, double Y, double Size)> lower)
        {
            if (upper.Count == 0 || lower.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.Append('M').Append(Fmt(upper[0].X)).Append(' ').Append(Fmt(upper[0].Y));
            for (var i = 1; i < upper.Count; i++)
            {
                sb.Append('L').Append(Fmt(upper[i].X)).Append(' ').Append(Fmt(upper[i].Y));
            }

            for (var i = lower.Count - 1; i >= 0; i--)
            {
                sb.Append('L').Append(Fmt(lower[i].X)).Append(' ').Append(Fmt(lower[i].Y));
            }

            sb.Append('Z');
            return sb.ToString();
        }

        /// <summary>计算趋势线/均值线路径。</summary>
        private static string BuildTrendPath(List<(double X, double Y, double Size)> pts, ElChartSeriesModel s)
        {
            if (pts == null || pts.Count < 2)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            switch (s.TrendLine)
            {
                case ElTrendLineType.Linear:
                {
                    var (slope, intercept) = LinearFit(pts);
                    sb.Append('M').Append(Fmt(pts[0].X)).Append(' ').Append(Fmt(intercept));
                    var lastX = pts[pts.Count - 1].X;
                    sb.Append('L').Append(Fmt(lastX)).Append(' ').Append(Fmt(slope * (pts.Count - 1) + intercept));
                    break;
                }

                case ElTrendLineType.MovingAverage:
                {
                    var w = Math.Max(2, s.TrendLinePeriod);
                    var started = false;
                    for (var i = w - 1; i < pts.Count; i++)
                    {
                        double sum = 0;
                        for (var k = i - w + 1; k <= i; k++)
                        {
                            sum += pts[k].Y;
                        }

                        var avg = sum / w;
                        sb.Append(started ? "L" : "M").Append(Fmt(pts[i].X)).Append(' ').Append(Fmt(avg));
                        started = true;
                    }
                    break;
                }

                case ElTrendLineType.Polynomial:
                {
                    var (c0, c1, c2) = QuadraticFit(pts);
                    for (var i = 0; i < pts.Count; i++)
                    {
                        var x = pts[i].X;
                        var y = c0 + c1 * i + c2 * i * i;
                        sb.Append(i == 0 ? "M" : "L").Append(Fmt(x)).Append(' ').Append(Fmt(y));
                    }
                    break;
                }

                case ElTrendLineType.Mean:
                case ElTrendLineType.Median:
                case ElTrendLineType.Mode:
                {
                    var ys = pts.Select(p => p.Y).OrderBy(v => v).ToList();
                    double level = s.TrendLine switch
                    {
                        ElTrendLineType.Mean => ys.Average(),
                        ElTrendLineType.Median => ys.Count % 2 == 1
                            ? ys[ys.Count / 2]
                            : (ys[ys.Count / 2 - 1] + ys[ys.Count / 2]) / 2.0,
                        _ => Mode(ys)
                    };
                    sb.Append('M').Append(Fmt(pts[0].X)).Append(' ').Append(Fmt(level));
                    sb.Append('L').Append(Fmt(pts[pts.Count - 1].X)).Append(' ').Append(Fmt(level));
                    break;
                }
            }

            return sb.ToString();
        }

        private static (double Slope, double Intercept) LinearFit(List<(double X, double Y, double Size)> pts)
        {
            var n = pts.Count;
            double sx = 0, sy = 0, sxy = 0, sxx = 0;
            for (var i = 0; i < n; i++)
            {
                sx += i;
                sy += pts[i].Y;
                sxy += i * pts[i].Y;
                sxx += (double)i * i;
            }

            var den = n * sxx - sx * sx;
            if (Math.Abs(den) < 1e-12)
            {
                return (0, sy / n);
            }

            var slope = (n * sxy - sx * sy) / den;
            var intercept = (sy - slope * sx) / n;
            return (slope, intercept);
        }

        private static (double C0, double C1, double C2) QuadraticFit(List<(double X, double Y, double Size)> pts)
        {
            var n = pts.Count;
            double sx = 0, sx2 = 0, sx3 = 0, sx4 = 0, sy = 0, sxy = 0, sx2y = 0;
            for (var i = 0; i < n; i++)
            {
                var x = (double)i;
                var y = pts[i].Y;
                var x2 = x * x;
                sx += x;
                sx2 += x2;
                sx3 += x2 * x;
                sx4 += x2 * x2;
                sy += y;
                sxy += x * y;
                sx2y += x2 * y;
            }

            // 3x3 正规方程（克莱姆法则）
            double Det3(double a11, double a12, double a13, double a21, double a22, double a23, double a31, double a32, double a33)
                => a11 * (a22 * a33 - a23 * a32) - a12 * (a21 * a33 - a23 * a31) + a13 * (a21 * a32 - a22 * a31);

            var d = Det3(n, sx, sx2, sx, sx2, sx3, sx2, sx3, sx4);
            if (Math.Abs(d) < 1e-12)
            {
                var (sl, ic) = LinearFit(pts);
                return (ic, sl, 0);
            }

            var d0 = Det3(sy, sx, sx2, sxy, sx2, sx3, sx2y, sx3, sx4);
            var d1 = Det3(n, sy, sx2, sx, sxy, sx3, sx2, sx2y, sx4);
            var d2 = Det3(n, sx, sy, sx, sx2, sxy, sx2, sx3, sx2y);
            return (d0 / d, d1 / d, d2 / d);
        }

        private static double Mode(List<double> values)
        {
            var groups = values.GroupBy(v => Math.Round(v, 2)).Select(g => (Value: g.Key, Count: g.Count(), First: g.First()))
                .OrderByDescending(g => g.Count).ThenBy(g => g.Value).ToList();
            return groups.Count > 0 ? groups[0].First : 0;
        }

        private static void AddBarHit(ElChartLayout layout, ElSeriesRender render, ElChartSeriesModel s, ElChartPoint p,
            int seriesIndex, int pointIndex, double x, double y, double w, double h, double value)
        {
            if (layout == null || !layout.CollectHitsAllowed)
            {
                return;
            }

            layout.Hits.Add(new ElChartHit
            {
                SeriesIndex = seriesIndex,
                PointIndex = pointIndex,
                Title = s.Title,
                Color = s.Color,
                Category = p?.Category ?? string.Empty,
                ValueText = FmtValue(value, s.ValueFormat),
                X = x,
                Y = y,
                W = Math.Max(w, 6),
                H = Math.Max(h, 6)
            });
        }

        private static void AddPointHit(ElChartLayout layout, ElSeriesRender render, ElChartSeriesModel s, ElChartPoint p,
            int seriesIndex, int pointIndex, double x, double y, double radius)
        {
            if (layout == null || !layout.CollectHitsAllowed)
            {
                return;
            }

            layout.Hits.Add(new ElChartHit
            {
                SeriesIndex = seriesIndex,
                PointIndex = pointIndex,
                Title = s.Title,
                Color = s.Color,
                Category = p?.Category ?? string.Empty,
                ValueText = FmtValue(p?.Value ?? 0, s.ValueFormat),
                X = x,
                Y = y,
                Radius = Math.Max(radius, 6)
            });
        }


        /// <summary>调色板取色（越界循环；调色板为空时退到 EP 主蓝，避免出现无 fill 的隐形扇区）。</summary>
        private static string PaletteColor(ElChartModel m, int index)
        {
            var palette = m.Palette;
            if (palette != null && palette.Count > 0)
            {
                return palette[index % palette.Count];
            }

            return "#409eff";
        }

        /// <summary>
        /// 是否改用「逐扇区图例」：整张图只有一个饼/环系列时，按系列列图例只会有一条「性别」，
        /// 色块与类别名对不上号。混合图（饼 + 柱）仍按系列列，别把柱子的图例条目吃掉。
        /// </summary>
        private static ElChartSeriesModel LegendSliceSeries(ElChartModel m)
        {
            return m.Series.Count == 1 && (m.Series[0].Type == ElChartType.Pie || m.Series[0].Type == ElChartType.Donut)
                ? m.Series[0]
                : null;
        }

        /// <summary>图例条目（标题 + 色块）：径向单系列逐块取类别名与该块实际色，其余按系列。</summary>
        private static List<(string Title, string Color)> LegendItems(ElChartModel m)
        {
            var radial = LegendSliceSeries(m);
            if (radial == null)
            {
                return m.Series.Select(s => (s.Title, s.Color)).ToList();
            }

            return radial.Points
                .Select((p, i) => (p.Category ?? string.Empty,
                    radial.ColorExplicit ? radial.Color : PaletteColor(m, i)))
                .ToList();
        }

        /// <summary>
        /// 左/右图例列的预留宽。原来是死值 120 —— 图例文字短（「男」「女」）时也照扣 120，
        /// 窄图表（消费方首页 180px 的饼/环）被吃掉三分之二版面，径向几何夹到 rOuter=1 就成一条线。
        /// 按该列实际要画的最宽图例文字实测：色块起点距绘图区边缘 12 + 色块 12 + 文字前隙 6 = 30，
        /// 加上文字实测宽再留 4px 内衬，扣掉基础外边 8 后即为 <c>26 + 最宽文字</c>
        /// （<see cref="ComputeLegend"/> 里 Left 的色块钉在 x=12，故左列的 4px 落在文字与绘图区之间）。
        /// </summary>
        private static double LegendReserveWidth(ElChartModel m)
        {
            var widest = LegendItems(m).Max(item => LegendTextWidth(item.Title));
            return 26 + widest;
        }

        /// <summary>图例条目的文字宽，字档与 <see cref="RenderLegend"/> 下发的 font-size=12 同一口径。</summary>
        private static double LegendTextWidth(string text)
        {
            return ElWatermark.EstimateTextWidth(text, 12, string.Empty, string.Empty);
        }

        private static void ComputeRadial(ElChartModel m, ElChartLayout layout, double plotLeft, double plotTop, double plotW, double plotH)
        {
            // 径向图取第一个饼/环序列
            var radial = m.Series.FirstOrDefault(s => s.Type == ElChartType.Pie || s.Type == ElChartType.Donut);
            if (radial == null)
            {
                layout.IsEmpty = true;
                return;
            }

            var cx = plotLeft + plotW / 2.0;
            var cy = plotTop + plotH / 2.0;
            var rOuter = Math.Max(1, Math.Min(plotW, plotH) / 2.0 - 6);
            var rInner = radial.Type == ElChartType.Donut ? rOuter * 0.6 : 0;

            var total = radial.Points.Sum(p => Math.Max(0, p.Value));
            if (total <= 0)
            {
                layout.IsEmpty = true;
                return;
            }

            double angle = 0;
            var sliceIndex = 0;
            var render = new ElSeriesRender
            {
                Type = radial.Type,
                Title = radial.Title,
                Color = radial.Color,
                ShowDataLabels = radial.ShowDataLabels
            };

            foreach (var p in radial.Points)
            {
                // 饼/环的一块 = 一个类别（男/女、各病案状态），整图一色就等于「看不出区别」。
                // 只有调用方显式给过 Color 才保留单色，那是有意的配色；否则按调色板逐块取色。
                var sliceColor = radial.ColorExplicit ? radial.Color : PaletteColor(m, sliceIndex);
                var frac = Math.Max(0, p.Value) / total;
                var sweep = frac * 360.0;
                var start = angle;
                var end = angle + sweep;
                render.Slices.Add((cx, cy, rOuter, rInner, start, end, p.Value, p.Category, sliceColor));
                angle = end;
                sliceIndex++;

                if (layout.CollectHitsAllowed)
                {
                    var mid = Polar(cx, cy, (rOuter + rInner) / 2.0, start + sweep / 2.0);
                    layout.Hits.Add(new ElChartHit
                    {
                        SeriesIndex = layout.Series.Count,
                        PointIndex = 0,
                        Title = radial.Title,
                        Color = sliceColor,
                        Category = p.Category ?? string.Empty,
                        ValueText = FmtValue(p.Value, radial.ValueFormat),
                        X = mid.X,
                        Y = mid.Y,
                        Radius = Math.Max(10, (rOuter - rInner) / 2.0 + 4)
                    });
                }
            }

            layout.ValueLo = 0;
            layout.ValueHi = total;
            layout.Series.Add(render);
        }

        /// <summary>热力图 / 等值带图布局（CategoryField=X 列，GroupField=Y 行，ValueField=值）。</summary>
        private static void ComputeHeatmap(ElChartModel m, ElChartLayout layout, double plotLeft, double plotTop, double plotW, double plotH)
        {
            var series = m.Series.Where(s => s.Type == ElChartType.Heatmap || s.Type == ElChartType.Contour).ToList();
            if (series.Count == 0)
            {
                layout.IsEmpty = true;
                return;
            }

            var xs = new List<string>();
            var ys = new List<string>();
            foreach (var s in series)
            {
                foreach (var p in s.Points)
                {
                    var xk = p.Category ?? string.Empty;
                    if (!xs.Contains(xk))
                    {
                        xs.Add(xk);
                    }

                    var yk = string.IsNullOrEmpty(p.Group) ? "-" : p.Group;
                    if (!ys.Contains(yk))
                    {
                        ys.Add(yk);
                    }
                }
            }

            if (xs.Count == 0 || ys.Count == 0)
            {
                layout.IsEmpty = true;
                return;
            }

            var cellW = plotW / xs.Count;
            var cellH = plotH / ys.Count;

            var all = series.SelectMany(s => s.Points).ToList();
            var vMin = all.Count > 0 ? all.Min(p => p.Value) : 0;
            var vMax = all.Count > 0 ? all.Max(p => p.Value) : 1;
            if (Math.Abs(vMax - vMin) < 1e-12)
            {
                vMax = vMin + 1;
            }

            const int levels = 5;
            var isContour = series.Any(s => s.Type == ElChartType.Contour);

            foreach (var s in series)
            {
                var render = new ElSeriesRender
                {
                    Type = s.Type,
                    Title = s.Title,
                    Color = s.Color,
                    ShowDataLabels = s.ShowDataLabels
                };

                var levelMap = new Dictionary<(int, int), int>();
                foreach (var p in s.Points)
                {
                    var xi = xs.IndexOf(p.Category ?? string.Empty);
                    var yi = ys.IndexOf(string.IsNullOrEmpty(p.Group) ? "-" : p.Group);
                    if (xi < 0 || yi < 0)
                    {
                        continue;
                    }

                    var t = (p.Value - vMin) / (vMax - vMin);
                    var shade = isContour
                        ? Math.Min(levels - 1, (int)Math.Floor(t * levels)) / (double)levels
                        : t;
                    var fill = MixHex(MixHex("#ffffff", s.Color, 0.10), s.Color, Math.Max(0, Math.Min(1, shade)));
                    var cx0 = plotLeft + xi * cellW;
                    var cy0 = plotTop + (ys.Count - 1 - yi) * cellH;
                    render.Boxes.Add((cx0, cy0, cellW, cellH, fill, "cell"));
                    levelMap[(xi, yi)] = isContour ? (int)Math.Floor(t * levels) : (int)Math.Round(t * 255);

                    if (s.ShowDataLabels)
                    {
                        render.Labels.Add((cx0 + cellW / 2, cy0 + cellH / 2 + 4, FmtValue(p.Value, s.ValueFormat), "middle"));
                    }

                    AddBarHit(layout, render, s, p, layout.Series.Count, 0, cx0, cy0, cellW, cellH, p.Value);
                }

                // 等值带边界线（相邻单元层级不同处描边）
                if (isContour)
                {
                    foreach (var kv in levelMap)
                    {
                        var (xi, yi) = kv.Key;
                        var x0 = plotLeft + xi * cellW;
                        var y0 = plotTop + (ys.Count - 1 - yi) * cellH;
                        if (levelMap.TryGetValue((xi + 1, yi), out var right) && right != kv.Value)
                        {
                            render.Segments.Add((x0 + cellW, y0, x0 + cellW, y0 + cellH, "contour"));
                        }

                        if (levelMap.TryGetValue((xi, yi + 1), out var up) && up != kv.Value)
                        {
                            render.Segments.Add((x0, y0, x0 + cellW, y0, "contour"));
                        }
                    }
                }

                layout.Series.Add(render);
            }

            // 行标签（Y）与列标签（X）
            for (var i = 0; i < ys.Count; i++)
            {
                layout.CategoryTicks.Add((ys[i], plotTop + (ys.Count - 1 - i) * cellH + cellH / 2));
            }

            for (var i = 0; i < xs.Count; i++)
            {
                layout.XTicks.Add((i, plotLeft + i * cellW + cellW / 2, xs[i]));
            }

            // 色阶图例
            var legendCount = levels + 1;
            for (var i = 0; i < legendCount; i++)
            {
                var t = i / (double)(legendCount - 1);
                layout.HeatLegend.Add((FmtValue(vMin + (vMax - vMin) * t, string.Empty),
                    MixHex(MixHex("#ffffff", series[0].Color, 0.10), series[0].Color, t)));
            }
        }

        /// <summary>参考线 / 参考带几何。</summary>
        private static void ComputeReferenceGeometry(ElChartModel m, ElChartLayout layout)
        {
            double XOfCategory(string cat)
            {
                var hit = layout.CategoryTicks.FirstOrDefault(t => string.Equals(t.Label, cat, StringComparison.Ordinal));
                if (!string.IsNullOrEmpty(hit.Label))
                {
                    return hit.X;
                }

                return layout.PlotLeft;
            }

            double YOfValue(double v)
            {
                var span = layout.ValueHi - layout.ValueLo;
                if (span <= 0)
                {
                    span = 1;
                }

                return layout.PlotBottom - (v - layout.ValueLo) / span * (layout.PlotBottom - layout.PlotTop);
            }

            foreach (var band in m.ReferenceBands)
            {
                if (band.Axis == ElReferenceAxis.Value)
                {
                    var y1 = YOfValue(band.From);
                    var y2 = YOfValue(band.To);
                    layout.RefBands.Add((layout.PlotLeft, Math.Min(y1, y2), layout.PlotRight - layout.PlotLeft,
                        Math.Max(1, Math.Abs(y2 - y1)), band.Label, band.Color, band.Opacity));
                }
                else
                {
                    var bandW = layout.CategoryTicks.Count > 1
                        ? layout.CategoryTicks[1].X - layout.CategoryTicks[0].X
                        : layout.PlotRight - layout.PlotLeft;
                    var x1 = layout.PlotLeft + band.FromIndex * bandW;
                    var x2 = layout.PlotLeft + band.ToIndex * bandW;
                    layout.RefBands.Add((Math.Min(x1, x2), layout.PlotTop, Math.Max(1, Math.Abs(x2 - x1)),
                        layout.PlotBottom - layout.PlotTop, band.Label, band.Color, band.Opacity));
                }
            }

            foreach (var line in m.ReferenceLines)
            {
                if (line.Axis == ElReferenceAxis.Value)
                {
                    var y = YOfValue(line.Value);
                    layout.RefLines.Add((layout.PlotLeft, y, layout.PlotRight, y, line.Label, line.Color, line.LineStyle));
                }
                else
                {
                    var x = XOfCategory(line.Category);
                    layout.RefLines.Add((x, layout.PlotTop, x, layout.PlotBottom, line.Label, line.Color, line.LineStyle));
                }
            }
        }

        /// <summary>标注几何。</summary>
        private static void ComputeAnnotationGeometry(ElChartModel m, ElChartLayout layout, bool useNumericX)
        {
            if (m.Annotations == null || m.Annotations.Count == 0)
            {
                return;
            }

            var span = layout.ValueHi - layout.ValueLo;
            if (span <= 0)
            {
                span = 1;
            }

            var plotH = layout.PlotBottom - layout.PlotTop;
            var plotW = layout.PlotRight - layout.PlotLeft;

            foreach (var a in m.Annotations)
            {
                double x;
                if (a.X.HasValue && layout.XTicks.Count > 0)
                {
                    var xLo = layout.XTicks[0].Value;
                    var xHi = layout.XTicks[layout.XTicks.Count - 1].Value;
                    var xSpan = xHi - xLo;
                    if (xSpan <= 0)
                    {
                        xSpan = 1;
                    }

                    x = layout.PlotLeft + (a.X.Value - xLo) / xSpan * plotW;
                }
                else if (useNumericX && a.X.HasValue && layout.XTicks.Count == 0)
                {
                    x = layout.PlotLeft + plotW / 2;
                }
                else
                {
                    var tick = layout.CategoryTicks.FirstOrDefault(t => string.Equals(t.Label, a.Category, StringComparison.Ordinal));
                    x = string.IsNullOrEmpty(tick.Label) ? layout.PlotLeft + plotW / 2 : tick.X;
                }

                var y = layout.PlotBottom - (a.Value - layout.ValueLo) / span * plotH;
                layout.Annotations.Add((x, y, a.Text ?? string.Empty, a.Color, a.Marker));
            }
        }

        private static void ComputeRadar(ElChartModel m, ElChartLayout layout, double plotLeft, double plotTop, double plotW, double plotH, ElChartAxisModel valueAxis)
        {
            var radar = m.Series.ToList();

            // 辐射轴 = 各序列类别的并集（保持出现顺序去重）
            var categories = new List<string>();
            foreach (var s in radar)
            {
                foreach (var p in s.Points)
                {
                    if (!categories.Contains(p.Category))
                    {
                        categories.Add(p.Category);
                    }
                }
            }

            if (categories.Count == 0)
            {
                layout.IsEmpty = true;
                return;
            }

            double dataMax = radar.Any() ? radar.Max(s => s.Points.Count > 0 ? s.Points.Max(p => p.Value) : 0) : 0;
            dataMax = Math.Max(dataMax, 0);

            double lo, hi, step;
            if (valueAxis != null && (valueAxis.Min.HasValue || valueAxis.Max.HasValue))
            {
                lo = valueAxis.Min ?? 0;
                hi = valueAxis.Max ?? dataMax;
                step = valueAxis.Step ?? (hi > lo ? (hi - lo) / 5.0 : 1);
            }
            else
            {
                var (nlo, nhi, nstep) = NiceScale(0, dataMax, 5);
                lo = nlo;
                hi = nhi;
                step = nstep;
            }

            if (hi <= lo)
            {
                hi = lo + 1;
            }

            // 雷达的「数值刻度」即各同心环对应的数值（复用于 RenderRadar 绘制环与刻度标签）
            var vTicks = new List<(double Value, double Y, string Label)>();
            for (var v = lo; v <= hi + 1e-9; v += step)
            {
                vTicks.Add((v, 0, FormatTick(v, valueAxis)));
            }
            layout.ValueTicks = vTicks;
            layout.ValueLo = lo;
            layout.ValueHi = hi;
            layout.ValueScales.Add(new ElAxisScale
            {
                Name = string.Empty,
                Lo = lo,
                Hi = hi,
                Step = step,
                Ticks = vTicks,
                HasNegative = false
            });

            var cx = plotLeft + plotW / 2.0;
            var cy = plotTop + plotH / 2.0;
            var R = Math.Max(1, Math.Min(plotW, plotH) / 2.0 - 22);

            var angles = new List<double>();
            for (var i = 0; i < categories.Count; i++)
            {
                angles.Add(-90 + i * (360.0 / categories.Count));
            }

            layout.RadarCx = cx;
            layout.RadarCy = cy;
            layout.RadarR = R;
            layout.RadarMaxValue = hi;
            layout.RadarAngles = angles;
            layout.RadarCategories = categories;

            foreach (var s in radar)
            {
                var render = new ElSeriesRender
                {
                    Type = s.Type,
                    Title = s.Title,
                    Color = s.Color,
                    LineStyle = s.LineStyle,
                    MarkerType = s.MarkerType,
                    ShowDataLabels = s.ShowDataLabels
                };

                var pts = new List<(double X, double Y, double Size)>();
                for (var i = 0; i < categories.Count; i++)
                {
                    var pt = s.Points.FirstOrDefault(p => p.Category == categories[i]);
                    var val = pt == null ? 0 : pt.Value;
                    var r = R * (val / hi);
                    var p = Polar(cx, cy, r, angles[i]);
                    pts.Add((p.X, p.Y, 0));
                }

                render.Points = pts;
                if (pts.Count > 0)
                {
                    var sb = new StringBuilder();
                    sb.Append($"M {Fmt(pts[0].X)} {Fmt(pts[0].Y)}");
                    for (var i = 1; i < pts.Count; i++)
                    {
                        sb.Append($" L {Fmt(pts[i].X)} {Fmt(pts[i].Y)}");
                    }
                    sb.Append(" Z");
                    render.Path = sb.ToString();
                }

                // 蜘蛛柱：沿每条辐射轴从圆心向外画粗线段
                if (s.Type == ElChartType.SpiderColumn)
                {
                    var spokeW = Math.Min(26, Math.Max(6, 2 * Math.PI * R / Math.Max(1, categories.Count) * 0.55));
                    for (var i = 0; i < categories.Count; i++)
                    {
                        var pt = s.Points.FirstOrDefault(p => p.Category == categories[i]);
                        var val = pt == null ? 0 : pt.Value;
                        var rr = R * (val / hi);
                        var from = Polar(cx, cy, 0, angles[i]);
                        var to = Polar(cx, cy, rr, angles[i]);
                        render.Spokes.Add((from.X, from.Y, to.X, to.Y, spokeW, val));
                        render.Segments.Add((cx, cy, to.X, to.Y, "spoke"));
                    }
                }

                // 命中目标（顶点）
                for (var i = 0; i < pts.Count; i++)
                {
                    var cat = categories[i];
                    var pt = s.Points.FirstOrDefault(p => p.Category == cat);
                    if (layout.CollectHitsAllowed)
                    {
                        layout.Hits.Add(new ElChartHit
                        {
                            SeriesIndex = layout.Series.Count,
                            PointIndex = i,
                            Title = s.Title,
                            Color = s.Color,
                            Category = cat,
                            ValueText = FmtValue(pt?.Value ?? 0, s.ValueFormat),
                            X = pts[i].X,
                            Y = pts[i].Y,
                            Radius = 10
                        });
                    }
                }

                layout.Series.Add(render);
            }
        }

        private static void ComputeLegend(
            ElChartModel m, ElChartLayout layout,
            double plotLeft, double plotTop, double plotRight, double plotBottom,
            double marginLeft, double marginRight, double marginTop, double marginBottom)
        {
            var items = LegendItems(m);
            var lineH = 22;
            var blockH = items.Count * lineH;

            double x, y;
            switch (m.LegendPosition)
            {
                case ElLegendPosition.Top:
                    x = plotLeft;
                    y = marginTop - blockH + 4;
                    break;
                case ElLegendPosition.Bottom:
                    x = plotLeft;
                    y = plotBottom + 10;
                    break;
                case ElLegendPosition.Left:
                    x = 12;
                    y = plotTop + 4;
                    break;
                default: // Right
                    x = plotRight + 12;
                    y = plotTop + 4;
                    break;
            }

            for (var i = 0; i < items.Count; i++)
            {
                layout.LegendItems.Add((items[i].Title, items[i].Color, x, y + i * lineH));
            }
        }

        /// <summary>把布局渲染为完整 SVG 字符串（含外层 &lt;svg&gt;）。</summary>
        public static string RenderSvg(ElChartModel m)
        {
            var layout = ComputeLayout(m);
            var inner = RenderContent(layout, m);
            var sb = new StringBuilder(inner.Length + 200);
            sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\"");
            sb.Append($" viewBox=\"0 0 {Fmt(layout.Width)} {Fmt(layout.Height)}\"");
            sb.Append($" width=\"{Fmt(layout.Width)}\" height=\"{Fmt(layout.Height)}\"");
            sb.Append(" class=\"el-chart-grow__svg\" role=\"img\"");
            if (!string.IsNullOrEmpty(m.Title))
            {
                sb.Append($" aria-label=\"{Escape(m.Title)}\"");
            }
            sb.Append(">");
            sb.Append(inner);
            sb.Append("</svg>");
            return sb.ToString();
        }

        /// <summary>生成内层 SVG 标记（不含外层 &lt;svg&gt;）。与 <see cref="RenderSvg"/> 共用，供组件以 MarkupString 注入。</summary>
        public static string RenderContent(ElChartLayout layout, ElChartModel m)
        {
            var sb = new StringBuilder(1024);
            sb.Append($"<rect x=\"0\" y=\"0\" width=\"{Fmt(layout.Width)}\" height=\"{Fmt(layout.Height)}\" fill=\"#ffffff\"/>");

            if (layout.IsEmpty)
            {
                var tx = layout.PlotLeft + (layout.PlotRight - layout.PlotLeft) / 2.0;
                var ty = layout.PlotTop + (layout.PlotBottom - layout.PlotTop) / 2.0;
                sb.Append($"<text x=\"{Fmt(tx)}\" y=\"{Fmt(ty)}\" text-anchor=\"middle\" class=\"el-chart-grow__empty\" fill=\"#909399\" font-size=\"14\">{Escape(m.EmptyText)}</text>");
                return sb.ToString();
            }

            if (!string.IsNullOrEmpty(m.Title))
            {
                sb.Append($"<text x=\"{Fmt(layout.Width / 2.0)}\" y=\"18\" text-anchor=\"middle\" class=\"el-chart-grow__title\" fill=\"#303133\" font-size=\"15\">{Escape(m.Title)}</text>");
            }

            if (layout.IsHeatmap)
            {
                RenderHeatmap(sb, layout, m);
            }
            else if (layout.IsRadar)
            {
                RenderRadar(sb, layout);
            }
            else if (layout.IsRadial)
            {
                RenderRadial(sb, layout);
            }
            else
            {
                RenderCartesian(sb, layout, m);
            }

            RenderLegend(sb, layout);
            return sb.ToString();
        }

        /// <summary>热力图 / 等值带图渲染（单元格 + 行/列标签 + 色阶条）。</summary>
        private static void RenderHeatmap(StringBuilder sb, ElChartLayout layout, ElChartModel m)
        {
            foreach (var s in layout.Series)
            {
                foreach (var bx in s.Boxes.Where(b => b.Kind == "cell"))
                {
                    sb.Append($"<rect x=\"{Fmt(bx.X)}\" y=\"{Fmt(bx.Y)}\" width=\"{Fmt(bx.W)}\" height=\"{Fmt(bx.H)}\" fill=\"{Escape(bx.Fill)}\" stroke=\"#ffffff\" stroke-width=\"1\" class=\"el-chart-grow__cell\"/>");
                }

                foreach (var seg in s.Segments.Where(x => x.Kind == "contour"))
                {
                    sb.Append($"<line x1=\"{Fmt(seg.X1)}\" y1=\"{Fmt(seg.Y1)}\" x2=\"{Fmt(seg.X2)}\" y2=\"{Fmt(seg.Y2)}\" stroke=\"#909399\" stroke-width=\"1\" class=\"el-chart-grow__contour\"/>");
                }

                foreach (var lb in s.Labels)
                {
                    sb.Append($"<text x=\"{Fmt(lb.X)}\" y=\"{Fmt(lb.Y)}\" text-anchor=\"{lb.Anchor}\" class=\"el-chart-grow__datalabel\" fill=\"#303133\" font-size=\"10\">{Escape(lb.Text)}</text>");
                }
            }

            // Y 行标签（左侧）
            foreach (var t in layout.CategoryTicks)
            {
                sb.Append($"<text x=\"{Fmt(layout.PlotLeft - 6)}\" y=\"{Fmt(t.X + 4)}\" text-anchor=\"end\" class=\"el-chart-grow__tick\" fill=\"#606266\" font-size=\"11\">{Escape(t.Label)}</text>");
            }

            // X 列标签（底部）
            foreach (var t in layout.XTicks)
            {
                sb.Append($"<text x=\"{Fmt(t.X)}\" y=\"{Fmt(layout.PlotBottom + 16)}\" text-anchor=\"middle\" class=\"el-chart-grow__tick\" fill=\"#606266\" font-size=\"11\">{Escape(t.Label)}</text>");
            }

            // 色阶条
            if (m.ShowValueAxis && layout.HeatLegend.Count > 1)
            {
                var barW = Math.Min(160, (layout.PlotRight - layout.PlotLeft) * 0.4);
                var barH = 10;
                var bx0 = layout.PlotRight - barW;
                var by0 = layout.PlotBottom + 22;
                var stepW = barW / (layout.HeatLegend.Count - 1);
                for (var i = 0; i < layout.HeatLegend.Count - 1; i++)
                {
                    sb.Append($"<rect x=\"{Fmt(bx0 + i * stepW)}\" y=\"{Fmt(by0)}\" width=\"{Fmt(stepW + 0.5)}\" height=\"{Fmt(barH)}\" fill=\"{Escape(layout.HeatLegend[i].Color)}\" class=\"el-chart-grow__heat-legend\"/>");
                }

                sb.Append($"<text x=\"{Fmt(bx0)}\" y=\"{Fmt(by0 + barH + 12)}\" text-anchor=\"middle\" class=\"el-chart-grow__tick\" fill=\"#606266\" font-size=\"10\">{Escape(layout.HeatLegend[0].Label)}</text>");
                sb.Append($"<text x=\"{Fmt(bx0 + barW)}\" y=\"{Fmt(by0 + barH + 12)}\" text-anchor=\"middle\" class=\"el-chart-grow__tick\" fill=\"#606266\" font-size=\"10\">{Escape(layout.HeatLegend[layout.HeatLegend.Count - 1].Label)}</text>");
            }
        }

        private static void RenderCartesian(StringBuilder sb, ElChartLayout layout, ElChartModel m)
        {
            var valueAxis = m.Axes.FirstOrDefault(a => a.Type == ElAxisType.Value);
            var categoryAxis = m.Axes.FirstOrDefault(a => a.Type == ElAxisType.Category);

            // 参考带（最底层底色）
            foreach (var b in layout.RefBands)
            {
                sb.Append($"<rect x=\"{Fmt(b.X)}\" y=\"{Fmt(b.Y)}\" width=\"{Fmt(b.W)}\" height=\"{Fmt(b.H)}\" fill=\"{Escape(b.Color)}\" fill-opacity=\"{Fmt(b.Opacity)}\" class=\"el-chart-grow__refband\"/>");
                if (!string.IsNullOrEmpty(b.Label))
                {
                    sb.Append($"<text x=\"{Fmt(b.X + b.W - 4)}\" y=\"{Fmt(b.Y + 14)}\" text-anchor=\"end\" class=\"el-chart-grow__refband-label\" fill=\"#909399\" font-size=\"10\">{Escape(b.Label)}</text>");
                }
            }

            // 网格线 + 数值轴刻度
            if (m.ShowGridLines)
            {
                foreach (var t in layout.ValueTicks)
                {
                    sb.Append($"<line x1=\"{Fmt(layout.PlotLeft)}\" y1=\"{Fmt(t.Y)}\" x2=\"{Fmt(layout.PlotRight)}\" y2=\"{Fmt(t.Y)}\" class=\"el-chart-grow__grid\" stroke=\"#ebeef5\" stroke-width=\"1\"/>");
                }
            }

            if (m.ShowValueAxis)
            {
                sb.Append($"<line x1=\"{Fmt(layout.PlotLeft)}\" y1=\"{Fmt(layout.PlotTop)}\" x2=\"{Fmt(layout.PlotLeft)}\" y2=\"{Fmt(layout.PlotBottom)}\" class=\"el-chart-grow__axis\" stroke=\"#dcdfe6\" stroke-width=\"1\"/>");
                foreach (var t in layout.ValueTicks)
                {
                    sb.Append($"<text x=\"{Fmt(layout.PlotLeft - 6)}\" y=\"{Fmt(t.Y + 4)}\" text-anchor=\"end\" class=\"el-chart-grow__tick\" fill=\"#606266\" font-size=\"11\">{Escape(t.Label)}</text>");
                }
                if (!string.IsNullOrEmpty(valueAxis?.Title))
                {
                    sb.Append($"<text x=\"{Fmt(layout.PlotLeft - 30)}\" y=\"{Fmt(layout.PlotTop - 8)}\" text-anchor=\"middle\" class=\"el-chart-grow__axis-title\" fill=\"#909399\" font-size=\"11\">{Escape(valueAxis.Title)}</text>");
                }
            }

            // 命名次轴（绘制在右侧）
            foreach (var scale in layout.ValueScales.Where(x => x.OnRight))
            {
                sb.Append($"<line x1=\"{Fmt(layout.PlotRight)}\" y1=\"{Fmt(layout.PlotTop)}\" x2=\"{Fmt(layout.PlotRight)}\" y2=\"{Fmt(layout.PlotBottom)}\" class=\"el-chart-grow__axis el-chart-grow__axis--secondary\" stroke=\"#dcdfe6\" stroke-width=\"1\"/>");
                foreach (var t in scale.Ticks)
                {
                    sb.Append($"<text x=\"{Fmt(layout.PlotRight + 6)}\" y=\"{Fmt(t.Y + 4)}\" text-anchor=\"start\" class=\"el-chart-grow__tick el-chart-grow__tick--secondary\" fill=\"#606266\" font-size=\"11\">{Escape(t.Label)}</text>");
                }
                if (!string.IsNullOrEmpty(scale.Title))
                {
                    sb.Append($"<text x=\"{Fmt(layout.PlotRight + 30)}\" y=\"{Fmt(layout.PlotTop - 8)}\" text-anchor=\"middle\" class=\"el-chart-grow__axis-title\" fill=\"#909399\" font-size=\"11\">{Escape(scale.Title)}</text>");
                }
            }

            // 数值 X 轴（散点/气泡启用 XValueField 时）
            if (layout.XTicks.Count > 0)
            {
                sb.Append($"<line x1=\"{Fmt(layout.PlotLeft)}\" y1=\"{Fmt(layout.PlotBottom)}\" x2=\"{Fmt(layout.PlotRight)}\" y2=\"{Fmt(layout.PlotBottom)}\" class=\"el-chart-grow__axis\" stroke=\"#dcdfe6\" stroke-width=\"1\"/>");
                foreach (var t in layout.XTicks)
                {
                    if (m.ShowGridLines)
                    {
                        sb.Append($"<line x1=\"{Fmt(t.X)}\" y1=\"{Fmt(layout.PlotTop)}\" x2=\"{Fmt(t.X)}\" y2=\"{Fmt(layout.PlotBottom)}\" class=\"el-chart-grow__grid\" stroke=\"#ebeef5\" stroke-width=\"1\"/>");
                    }
                    sb.Append($"<text x=\"{Fmt(t.X)}\" y=\"{Fmt(layout.PlotBottom + 16)}\" text-anchor=\"middle\" class=\"el-chart-grow__tick\" fill=\"#606266\" font-size=\"11\">{Escape(t.Label)}</text>");
                }
            }
            else if (m.ShowCategoryAxis)
            {
                sb.Append($"<line x1=\"{Fmt(layout.PlotLeft)}\" y1=\"{Fmt(layout.PlotBottom)}\" x2=\"{Fmt(layout.PlotRight)}\" y2=\"{Fmt(layout.PlotBottom)}\" class=\"el-chart-grow__axis\" stroke=\"#dcdfe6\" stroke-width=\"1\"/>");
                foreach (var t in layout.CategoryTicks)
                {
                    sb.Append($"<text x=\"{Fmt(t.X)}\" y=\"{Fmt(layout.PlotBottom + 16)}\" text-anchor=\"middle\" class=\"el-chart-grow__tick\" fill=\"#606266\" font-size=\"11\">{Escape(t.Label)}</text>");
                }
                if (!string.IsNullOrEmpty(categoryAxis?.Title))
                {
                    sb.Append($"<text x=\"{Fmt((layout.PlotLeft + layout.PlotRight) / 2.0)}\" y=\"{Fmt(layout.PlotBottom + 32)}\" text-anchor=\"middle\" class=\"el-chart-grow__axis-title\" fill=\"#909399\" font-size=\"11\">{Escape(categoryAxis.Title)}</text>");
                }
            }

            foreach (var s in layout.Series)
            {
                var isLine = s.Type == ElChartType.Line || s.Type == ElChartType.StackedLine || s.Type == ElChartType.FullStackedLine;
                var isArea = s.Type == ElChartType.Area || s.Type == ElChartType.StackedArea
                    || s.Type == ElChartType.FullStackedArea || s.Type == ElChartType.RangeArea;
                var isColumn = s.Type == ElChartType.Column || s.Type == ElChartType.StackedColumn
                    || s.Type == ElChartType.FullStackedColumn || s.Type == ElChartType.RangeColumn
                    || s.Type == ElChartType.Waterfall || s.Type == ElChartType.Candlestick;
                var isBar = s.Type == ElChartType.Bar || s.Type == ElChartType.StackedBar
                    || s.Type == ElChartType.FullStackedBar || s.Type == ElChartType.RangeBar
                    || s.Type == ElChartType.HorizontalWaterfall || s.Type == ElChartType.Bullet;
                var isScatter = s.Type == ElChartType.Scatter;
                var isBubble = s.Type == ElChartType.Bubble;

                // 背景档（子弹图三档等）
                foreach (var bx in s.Boxes.Where(b => b.Kind == "band"))
                {
                    sb.Append($"<rect x=\"{Fmt(bx.X)}\" y=\"{Fmt(bx.Y)}\" width=\"{Fmt(bx.W)}\" height=\"{Fmt(bx.H)}\" fill=\"{Escape(bx.Fill)}\" class=\"el-chart-grow__band\"/>");
                }

                if (isLine || isArea)
                {
                    if (isArea)
                    {
                        sb.Append($"<path d=\"{s.Path}\" fill=\"{Escape(s.Color)}\" fill-opacity=\"0.25\" stroke=\"none\"><title>{Escape(s.Title)}</title></path>");
                    }
                    var dash = s.LineStyle == ElLineStyle.Dashed ? " stroke-dasharray=\"6 4\"" : s.LineStyle == ElLineStyle.Dotted ? " stroke-dasharray=\"2 3\"" : "";
                    sb.Append($"<path d=\"{s.Path}\" fill=\"none\" stroke=\"{Escape(s.Color)}\" stroke-width=\"2\"{dash}><title>{Escape(s.Title)}</title></path>");
                    foreach (var p in s.Points)
                    {
                        if (s.MarkerType != ElMarkerType.None)
                        {
                            sb.Append(MarkerSvg(p.X, p.Y, s.Color, s.MarkerType));
                        }
                        if (s.ShowDataLabels)
                        {
                            sb.Append($"<text x=\"{Fmt(p.X)}\" y=\"{Fmt(p.Y - 8)}\" text-anchor=\"middle\" class=\"el-chart-grow__datalabel\" fill=\"#606266\" font-size=\"10\">{Fmt(Math.Round(ValueAt(layout, s, p), 2))}</text>");
                        }
                    }
                }
                else if (isColumn || isBar)
                {
                    foreach (var b in s.Bars)
                    {
                        sb.Append($"<rect x=\"{Fmt(b.X)}\" y=\"{Fmt(b.Y)}\" width=\"{Fmt(b.W)}\" height=\"{Fmt(b.H)}\" fill=\"{Escape(s.Color)}\" rx=\"1\"><title>{Escape(s.Title)}: {Fmt(Math.Round(b.Value, 2))}</title></rect>");
                        if (s.ShowDataLabels)
                        {
                            if (isColumn)
                            {
                                sb.Append($"<text x=\"{Fmt(b.X + b.W / 2.0)}\" y=\"{Fmt(b.Y - 4)}\" text-anchor=\"middle\" class=\"el-chart-grow__datalabel\" fill=\"#606266\" font-size=\"10\">{Fmt(Math.Round(b.Value, 2))}</text>");
                            }
                            else
                            {
                                sb.Append($"<text x=\"{Fmt(b.X + b.W + 4)}\" y=\"{Fmt(b.Y + b.H / 2.0 + 4)}\" text-anchor=\"start\" class=\"el-chart-grow__datalabel\" fill=\"#606266\" font-size=\"10\">{Fmt(Math.Round(b.Value, 2))}</text>");
                            }
                        }
                    }
                }
                else if (isScatter || isBubble)
                {
                    foreach (var p in s.Points)
                    {
                        if (isBubble)
                        {
                            var r = p.Size > 0 ? p.Size : 5;
                            sb.Append($"<circle cx=\"{Fmt(p.X)}\" cy=\"{Fmt(p.Y)}\" r=\"{Fmt(r)}\" fill=\"{Escape(s.Color)}\" fill-opacity=\"0.5\" stroke=\"{Escape(s.Color)}\" stroke-width=\"1\"><title>{Escape(s.Title)}: {Fmt(Math.Round(ValueAt(layout, s, p), 2))}</title></circle>");
                        }
                        else
                        {
                            var mt = s.MarkerType == ElMarkerType.None ? ElMarkerType.Circle : s.MarkerType;
                            sb.Append(MarkerSvg(p.X, p.Y, s.Color, mt));
                        }

                        if (s.ShowDataLabels)
                        {
                            sb.Append($"<text x=\"{Fmt(p.X)}\" y=\"{Fmt(p.Y - 8)}\" text-anchor=\"middle\" class=\"el-chart-grow__datalabel\" fill=\"#606266\" font-size=\"10\">{Fmt(Math.Round(ValueAt(layout, s, p), 2))}</text>");
                        }
                    }
                }

                // —— 新增系列专用几何 ——
                foreach (var bx in s.Boxes.Where(b => b.Kind == "cell"))
                {
                    sb.Append($"<rect x=\"{Fmt(bx.X)}\" y=\"{Fmt(bx.Y)}\" width=\"{Fmt(bx.W)}\" height=\"{Fmt(bx.H)}\" fill=\"{Escape(bx.Fill)}\" class=\"el-chart-grow__cell\"/>");
                }

                foreach (var bx in s.Boxes.Where(b => b.Kind == "candle" || b.Kind == "box"))
                {
                    var stroke = bx.Kind == "candle" ? bx.Fill : s.Color;
                    sb.Append($"<rect x=\"{Fmt(bx.X)}\" y=\"{Fmt(bx.Y)}\" width=\"{Fmt(bx.W)}\" height=\"{Fmt(bx.H)}\" fill=\"{Escape(bx.Fill)}\" stroke=\"{Escape(stroke)}\" stroke-width=\"1\"><title>{Escape(s.Title)}</title></rect>");
                }

                foreach (var seg in s.Segments)
                {
                    var w = seg.Kind == "median" || seg.Kind == "target" ? 2 : 1;
                    var dash = seg.Kind == "connector" ? " stroke-dasharray=\"4 3\"" : string.Empty;
                    var col = seg.Kind == "contour" ? "#909399" : seg.Kind == "connector" ? "#c0c4cc" : s.Color;
                    sb.Append($"<line x1=\"{Fmt(seg.X1)}\" y1=\"{Fmt(seg.Y1)}\" x2=\"{Fmt(seg.X2)}\" y2=\"{Fmt(seg.Y2)}\" stroke=\"{Escape(col)}\" stroke-width=\"{Fmt(w)}\"{dash} class=\"el-chart-grow__seg el-chart-grow__seg--{seg.Kind}\"/>");
                }

                foreach (var poly in s.Polygons)
                {
                    sb.Append($"<polygon points=\"{poly.Points}\" fill=\"{Escape(s.Color)}\" fill-opacity=\"0.72\" stroke=\"{Escape(s.Color)}\" stroke-width=\"1\"><title>{Escape(poly.Label)}</title></polygon>");
                }

                foreach (var sp in s.Spokes)
                {
                    sb.Append($"<line x1=\"{Fmt(sp.X1)}\" y1=\"{Fmt(sp.Y1)}\" x2=\"{Fmt(sp.X2)}\" y2=\"{Fmt(sp.Y2)}\" stroke=\"{Escape(s.Color)}\" stroke-width=\"{Fmt(sp.Width)}\" stroke-opacity=\"0.85\" class=\"el-chart-grow__spider-column\"><title>{Escape(s.Title)}: {Fmt(Math.Round(sp.Value, 2))}</title></line>");
                }

                if (!string.IsNullOrEmpty(s.TrendPath))
                {
                    var tdash = s.TrendStyle == ElLineStyle.Dashed ? " stroke-dasharray=\"6 4\"" : s.TrendStyle == ElLineStyle.Dotted ? " stroke-dasharray=\"2 3\"" : string.Empty;
                    var tcol = string.IsNullOrEmpty(s.TrendColor) ? s.Color : s.TrendColor;
                    sb.Append($"<path d=\"{s.TrendPath}\" fill=\"none\" stroke=\"{Escape(tcol)}\" stroke-width=\"2\"{tdash} class=\"el-chart-grow__trendline\"><title>{Escape(s.Title)} 趋势线</title></path>");
                }

                foreach (var lb in s.Labels)
                {
                    sb.Append($"<text x=\"{Fmt(lb.X)}\" y=\"{Fmt(lb.Y)}\" text-anchor=\"{lb.Anchor}\" class=\"el-chart-grow__datalabel\" fill=\"#606266\" font-size=\"10\">{Escape(lb.Text)}</text>");
                }
            }

            foreach (var line in layout.RefLines)
            {
                var dash = line.Style == ElLineStyle.Dashed ? " stroke-dasharray=\"6 4\"" : line.Style == ElLineStyle.Dotted ? " stroke-dasharray=\"2 3\"" : string.Empty;
                sb.Append($"<line x1=\"{Fmt(line.X1)}\" y1=\"{Fmt(line.Y1)}\" x2=\"{Fmt(line.X2)}\" y2=\"{Fmt(line.Y2)}\" stroke=\"{Escape(line.Color)}\" stroke-width=\"1.5\"{dash} class=\"el-chart-grow__refline\"/>");
                if (!string.IsNullOrEmpty(line.Label))
                {
                    var isVertical = Math.Abs(line.X1 - line.X2) < 1e-9;
                    var lx = isVertical ? line.X1 + 4 : line.X2 - 4;
                    var ly = isVertical ? line.Y1 + 12 : line.Y1 - 4;
                    sb.Append($"<text x=\"{Fmt(lx)}\" y=\"{Fmt(ly)}\" text-anchor=\"{(isVertical ? "start" : "end")}\" class=\"el-chart-grow__refline-label\" fill=\"{Escape(line.Color)}\" font-size=\"10\">{Escape(line.Label)}</text>");
                }
            }

            foreach (var an in layout.Annotations)
            {
                if (an.Marker != ElMarkerType.None)
                {
                    sb.Append(MarkerSvg(an.X, an.Y, an.Color, an.Marker));
                }

                if (!string.IsNullOrEmpty(an.Text))
                {
                    sb.Append($"<text x=\"{Fmt(an.X + 6)}\" y=\"{Fmt(an.Y - 6)}\" text-anchor=\"start\" class=\"el-chart-grow__annotation\" fill=\"{Escape(an.Color)}\" font-size=\"10\">{Escape(an.Text)}</text>");
                }
            }
        }

        private static double ValueAt(ElChartLayout layout, ElSeriesRender s, (double X, double Y, double Size) p)
        {
            // 由 Y 反推数值（用于数据标签）
            var plotH = layout.PlotBottom - layout.PlotTop;
            if (plotH <= 0)
            {
                return 0;
            }

            var lo = layout.ValueTicks.Count > 0 ? layout.ValueTicks.First().Value : 0;
            var hi = layout.ValueTicks.Count > 0 ? layout.ValueTicks.Last().Value : 1;
            var span = hi - lo;
            return lo + (layout.PlotBottom - p.Y) / plotH * span;
        }

        private static void RenderRadial(StringBuilder sb, ElChartLayout layout)
        {
            foreach (var s in layout.Series)
            {
                var isDonut = s.Type == ElChartType.Donut;
                foreach (var slice in s.Slices)
                {
                    var span = slice.EndDeg - slice.StartDeg;
                    if (span >= 359.999)
                    {
                        if (isDonut)
                        {
                            sb.Append($"<circle cx=\"{Fmt(slice.Cx)}\" cy=\"{Fmt(slice.Cy)}\" r=\"{Fmt(slice.ROuter)}\" fill=\"none\" stroke=\"{Escape(slice.Color)}\" stroke-width=\"{Fmt(slice.ROuter - slice.RInner)}\"/><title>{Escape(slice.Label)}: {Fmt(Math.Round(slice.Value, 2))}</title>");
                        }
                        else
                        {
                            sb.Append($"<circle cx=\"{Fmt(slice.Cx)}\" cy=\"{Fmt(slice.Cy)}\" r=\"{Fmt(slice.ROuter)}\" fill=\"{Escape(slice.Color)}\"><title>{Escape(slice.Label)}: {Fmt(Math.Round(slice.Value, 2))}</title></circle>");
                        }
                        continue;
                    }

                    var large = span > 180 ? 1 : 0;
                    if (isDonut)
                    {
                        var p1 = Polar(slice.Cx, slice.Cy, slice.ROuter, slice.StartDeg);
                        var p2 = Polar(slice.Cx, slice.Cy, slice.ROuter, slice.EndDeg);
                        var p1i = Polar(slice.Cx, slice.Cy, slice.RInner, slice.StartDeg);
                        var p2i = Polar(slice.Cx, slice.Cy, slice.RInner, slice.EndDeg);
                        var d = $"M {Fmt(p1.X)} {Fmt(p1.Y)} A {Fmt(slice.ROuter)} {Fmt(slice.ROuter)} 0 {large} 1 {Fmt(p2.X)} {Fmt(p2.Y)} " +
                                $"L {Fmt(p2i.X)} {Fmt(p2i.Y)} A {Fmt(slice.RInner)} {Fmt(slice.RInner)} 0 {large} 0 {Fmt(p1i.X)} {Fmt(p1i.Y)} Z";
                        sb.Append($"<path d=\"{d}\" fill=\"{Escape(slice.Color)}\"><title>{Escape(slice.Label)}: {Fmt(Math.Round(slice.Value, 2))}</title></path>");
                    }
                    else
                    {
                        var p1 = Polar(slice.Cx, slice.Cy, slice.ROuter, slice.StartDeg);
                        var p2 = Polar(slice.Cx, slice.Cy, slice.ROuter, slice.EndDeg);
                        var d = $"M {Fmt(slice.Cx)} {Fmt(slice.Cy)} L {Fmt(p1.X)} {Fmt(p1.Y)} A {Fmt(slice.ROuter)} {Fmt(slice.ROuter)} 0 {large} 1 {Fmt(p2.X)} {Fmt(p2.Y)} Z";
                        sb.Append($"<path d=\"{d}\" fill=\"{Escape(slice.Color)}\"><title>{Escape(slice.Label)}: {Fmt(Math.Round(slice.Value, 2))}</title></path>");
                    }

                    if (s.ShowDataLabels)
                    {
                        var mid = (slice.StartDeg + slice.EndDeg) / 2.0;
                        var r = isDonut ? (slice.ROuter + slice.RInner) / 2.0 : slice.ROuter * 0.62;
                        var lp = Polar(slice.Cx, slice.Cy, r, mid);
                        sb.Append($"<text x=\"{Fmt(lp.X)}\" y=\"{Fmt(lp.Y + 4)}\" text-anchor=\"middle\" class=\"el-chart-grow__datalabel\" fill=\"#ffffff\" font-size=\"10\">{Fmt(Math.Round(slice.Value, 2))}</text>");
                    }
                }

                // 圆心文字画在扇区之上，坐标直接取几何中心：宿主页用绝对定位的 HTML 叠层冒充的话，
                // 一旦图例/画布宽度变了就要重新算偏移（首页「一览」就是这么偏出洞口的）。
                if (!string.IsNullOrEmpty(layout.CenterText) && s.Slices.Count > 0)
                {
                    var c0 = s.Slices[0];
                    sb.Append($"<text x=\"{Fmt(c0.Cx)}\" y=\"{Fmt(c0.Cy + 4)}\" text-anchor=\"middle\" class=\"el-chart-grow__center\" fill=\"#909399\" font-size=\"14\">{Escape(layout.CenterText)}</text>");
                }
            }
        }

        private static void RenderRadar(StringBuilder sb, ElChartLayout layout)
        {
            var cx = layout.RadarCx;
            var cy = layout.RadarCy;
            var R = layout.RadarR;
            var hi = layout.RadarMaxValue;
            var angles = layout.RadarAngles;
            var categories = layout.RadarCategories;

            if (angles == null || angles.Count == 0 || hi <= 0)
            {
                return;
            }

            // 同心多边形网格（每环对应一个数值刻度）
            foreach (var t in layout.ValueTicks)
            {
                if (t.Value <= 0)
                {
                    continue;
                }

                var r = R * t.Value / hi;
                var pts = new List<string>();
                for (var i = 0; i < angles.Count; i++)
                {
                    var p = Polar(cx, cy, r, angles[i]);
                    pts.Add($"{Fmt(p.X)} {Fmt(p.Y)}");
                }
                sb.Append($"<polygon points=\"{string.Join(" ", pts)}\" class=\"el-chart-grow__ring\" fill=\"none\" stroke=\"#ebeef5\" stroke-width=\"1\"/>");
            }

            // 辐射轴线 + 类别标签
            for (var i = 0; i < angles.Count; i++)
            {
                var outer = Polar(cx, cy, R, angles[i]);
                sb.Append($"<line x1=\"{Fmt(cx)}\" y1=\"{Fmt(cy)}\" x2=\"{Fmt(outer.X)}\" y2=\"{Fmt(outer.Y)}\" class=\"el-chart-grow__spoke\" stroke=\"#dcdfe6\" stroke-width=\"1\"/>");
                var lbl = Polar(cx, cy, R + 16, angles[i]);
                var anchor = Math.Abs(angles[i] + 90) < 1 ? "middle" : (Math.Cos((angles[i] - 90) * Deg2Rad) >= 0 ? "start" : "end");
                sb.Append($"<text x=\"{Fmt(lbl.X)}\" y=\"{Fmt(lbl.Y + 4)}\" text-anchor=\"{anchor}\" class=\"el-chart-grow__axis-label\" fill=\"#606266\" font-size=\"11\">{Escape(categories[i])}</text>");
            }

            // 数值刻度标签（沿顶部轴线，角度 -90）
            foreach (var t in layout.ValueTicks)
            {
                if (t.Value <= 0)
                {
                    continue;
                }

                var r = R * t.Value / hi;
                var p = Polar(cx, cy, r, -90);
                sb.Append($"<text x=\"{Fmt(p.X - 6)}\" y=\"{Fmt(p.Y + 4)}\" text-anchor=\"end\" class=\"el-chart-grow__tick\" fill=\"#909399\" font-size=\"10\">{Escape(t.Label)}</text>");
            }

            // 各序列多边形叠加（蜘蛛柱改为沿辐射轴的粗线段）
            foreach (var s in layout.Series)
            {
                if (s.Points.Count == 0)
                {
                    continue;
                }

                if (s.Type == ElChartType.SpiderColumn)
                {
                    foreach (var sp in s.Spokes)
                    {
                        sb.Append($"<line x1=\"{Fmt(sp.X1)}\" y1=\"{Fmt(sp.Y1)}\" x2=\"{Fmt(sp.X2)}\" y2=\"{Fmt(sp.Y2)}\" stroke=\"{Escape(s.Color)}\" stroke-width=\"{Fmt(sp.Width)}\" stroke-opacity=\"0.85\" class=\"el-chart-grow__spider-column\"><title>{Escape(s.Title)}: {Fmt(Math.Round(sp.Value, 2))}</title></line>");
                    }

                    foreach (var p in s.Points)
                    {
                        if (s.ShowDataLabels)
                        {
                            sb.Append($"<text x=\"{Fmt(p.X)}\" y=\"{Fmt(p.Y - 8)}\" text-anchor=\"middle\" class=\"el-chart-grow__datalabel\" fill=\"#606266\" font-size=\"10\">{Fmt(Math.Round(ValueAt(layout, s, p), 2))}</text>");
                        }
                    }

                    continue;
                }

                var pts = string.Join(" ", s.Points.Select(p => $"{Fmt(p.X)} {Fmt(p.Y)}"));
                sb.Append($"<polygon points=\"{pts}\" fill=\"{Escape(s.Color)}\" fill-opacity=\"0.15\" stroke=\"{Escape(s.Color)}\" stroke-width=\"2\"><title>{Escape(s.Title)}</title></polygon>");
                foreach (var p in s.Points)
                {
                    sb.Append(MarkerSvg(p.X, p.Y, s.Color, ElMarkerType.Circle));
                    if (s.ShowDataLabels)
                    {
                        sb.Append($"<text x=\"{Fmt(p.X)}\" y=\"{Fmt(p.Y - 8)}\" text-anchor=\"middle\" class=\"el-chart-grow__datalabel\" fill=\"#606266\" font-size=\"10\">{Fmt(Math.Round(ValueAt(layout, s, p), 2))}</text>");
                    }
                }
            }
        }

        private static void RenderLegend(StringBuilder sb, ElChartLayout layout)
        {
            foreach (var item in layout.LegendItems)
            {
                sb.Append($"<rect x=\"{Fmt(item.X)}\" y=\"{Fmt(item.Y)}\" width=\"12\" height=\"12\" rx=\"2\" fill=\"{Escape(item.Color)}\"/>");
                sb.Append($"<text x=\"{Fmt(item.X + 18)}\" y=\"{Fmt(item.Y + 10)}\" class=\"el-chart-grow__legend\" fill=\"#606266\" font-size=\"12\">{Escape(item.Title)}</text>");
            }
        }

        private static string MarkerSvg(double x, double y, string color, ElMarkerType type)
        {
            switch (type)
            {
                case ElMarkerType.Square:
                    return $"<rect x=\"{Fmt(x - 3)}\" y=\"{Fmt(y - 3)}\" width=\"6\" height=\"6\" fill=\"{Escape(color)}\"/>";
                case ElMarkerType.Diamond:
                    return $"<path d=\"M {Fmt(x)} {Fmt(y - 4)} L {Fmt(x + 4)} {Fmt(y)} L {Fmt(x)} {Fmt(y + 4)} L {Fmt(x - 4)} {Fmt(y)} Z\" fill=\"{Escape(color)}\"/>";
                default:
                    return $"<circle cx=\"{Fmt(x)}\" cy=\"{Fmt(y)}\" r=\"3.5\" fill=\"{Escape(color)}\"/>";
            }
        }

        private static (double X, double Y) Polar(double cx, double cy, double r, double deg)
        {
            var rad = (deg - 90) * Deg2Rad;
            return (cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
        }

        private static string BuildLinePath(List<(double X, double Y, double Size)> pts, bool isArea, double plotBottom)
        {
            if (pts.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.Append($"M {Fmt(pts[0].X)} {Fmt(pts[0].Y)}");
            for (var i = 1; i < pts.Count; i++)
            {
                sb.Append($" L {Fmt(pts[i].X)} {Fmt(pts[i].Y)}");
            }

            if (isArea && pts.Count > 0)
            {
                var first = pts[0];
                var last = pts[pts.Count - 1];
                sb.Append($" L {Fmt(last.X)} {Fmt(plotBottom)} L {Fmt(first.X)} {Fmt(plotBottom)} Z");
            }

            return sb.ToString();
        }

        private static string FormatTick(double v, ElChartAxisModel axis)
        {
            if (axis?.Formatter != null)
            {
                return axis.Formatter(v);
            }

            if (Math.Abs(v - Math.Round(v)) < 1e-9)
            {
                return ((long)Math.Round(v)).ToString(CultureInfo.InvariantCulture);
            }

            return v.ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>「漂亮」刻度算法：返回 (最小值, 最大值, 步长)。</summary>
        private static (double Lo, double Hi, double Step) NiceScale(double min, double max, int maxTicks)
        {
            if (max <= min)
            {
                max = min + 1;
            }

            var range = NiceNum(max - min, false);
            var step = NiceNum(range / (maxTicks - 1), true);
            var niceMin = Math.Floor(min / step) * step;
            var niceMax = Math.Ceiling(max / step) * step;
            return (niceMin, niceMax, step);
        }

        private static double NiceNum(double x, bool round)
        {
            if (x == 0)
            {
                return 1;
            }

            var exp = Math.Floor(Math.Log10(Math.Abs(x)));
            var f = x / Math.Pow(10, exp);
            double nf;
            if (round)
            {
                if (f < 1.5) nf = 1;
                else if (f < 3) nf = 2;
                else if (f < 7) nf = 5;
                else nf = 10;
            }
            else
            {
                if (f <= 1) nf = 1;
                else if (f <= 2) nf = 2;
                else if (f <= 5) nf = 5;
                else nf = 10;
            }

            return nf * Math.Pow(10, exp);
        }

        /// <summary>按不变区域格式化数值，保证 SVG 输出与区域设置无关。</summary>
        /// <summary>
        /// 命中测试：按画布坐标查找最近的数据点（供悬停提示使用）。
        /// 返回 null 表示未命中（阈值外）。
        /// </summary>
        public static ElChartHit FindNearest(ElChartLayout layout, double x, double y, double tolerance = 26)
        {
            if (layout?.Hits == null || layout.Hits.Count == 0)
            {
                return null;
            }

            ElChartHit best = null;
            var bestDist = double.MaxValue;

            foreach (var hit in layout.Hits)
            {
                double dist;
                if (hit.W > 0 && hit.H > 0)
                {
                    // 矩形判定：到矩形中心归一化距离
                    var dx = Math.Max(0, Math.Abs(x - hit.X) - hit.W / 2.0);
                    var dy = Math.Max(0, Math.Abs(y - hit.Y) - hit.H / 2.0);
                    dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dx <= 0 && dy <= 0)
                    {
                        dist = -1; // 命中内部优先
                    }
                }
                else
                {
                    var dx = x - hit.X;
                    var dy = y - hit.Y;
                    dist = Math.Sqrt(dx * dx + dy * dy) - hit.Radius * 0.5;
                }

                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = hit;
                }
            }

            return bestDist <= tolerance ? best : null;
        }

        /// <summary>Web 坐标（相对画布左上角）转 SVG viewBox 坐标（画布已按 viewBox 尺寸渲染时两者一致）。</summary>
        public static (double X, double Y) ToCanvas(ElChartLayout layout, double clientX, double clientY)
        {
            if (layout == null || layout.Width <= 0 || layout.Height <= 0)
            {
                return (clientX, clientY);
            }

            return (clientX, clientY);
        }

        public static string Fmt(double value) => value.ToString(CultureInfo.InvariantCulture);

        /// <summary>XML 转义。</summary>
        public static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }
    }
}
