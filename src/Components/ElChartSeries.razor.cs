using System;
using System.Collections;
using Microsoft.AspNetCore.Components;
using ElementPlus;

namespace ElementPlus
{
    /// <summary>
    /// 图表序列（Element Plus 自渲染版）。
    /// 作为 <see cref="ElChartGrow"/> 的子组件声明一条数据序列，并在初始化时注册到父图表。
    /// 自身不渲染任何可见内容（几何由 <see cref="ElChartGrowEngine"/> 统一计算）。
    /// <para>多值系列的字段映射（语义由 <see cref="ElChartType"/> 决定）：</para>
    /// <list type="bullet">
    /// <item>Candlestick / Ohlc：<c>OpenField</c> <c>HighField</c> <c>LowField</c> <c>CloseField</c></item>
    /// <item>HighLow：<c>HighField</c> <c>LowField</c>（可选 <c>CloseField</c> 画收盘短横线）</item>
    /// <item>BoxPlot：<c>MinField</c> <c>Q1Field</c> <c>MedianField</c> <c>Q3Field</c> <c>MaxField</c></item>
    /// <item>RangeArea / RangeBar / RangeColumn：<c>LowField</c> <c>HighField</c></item>
    /// <item>Bullet：<c>ValueField</c>（实际值）+ <c>TargetField</c>（目标值）</item>
    /// <item>Waterfall：<c>ValueField</c>（变化量）+ <c>IsTotalField</c>（非 0 视为小计）</item>
    /// <item>Heatmap / Contour：<c>CategoryField</c>（X 列）+ <c>GroupField</c>（Y 行）+ <c>ValueField</c></item>
    /// <item>其它：只需 <c>CategoryField</c> + <c>ValueField</c></item>
    /// </list>
    /// 也可直接用通用槽位 <c>ValueField2..ValueField5</c> 代替上述语义别名。
    /// </summary>
    public partial class ElChartSeries : ComponentBase
    {
        /// <summary>父图表（级联参数）。</summary>
        [CascadingParameter] public ElChartGrow Chart { get; set; }

        /// <summary>数据源（行集合）。</summary>
        [Parameter] public IEnumerable Items { get; set; }

        /// <summary>图表类型（含线/面/柱/条/散点/气泡/饼/环/雷达/堆叠/K线/箱线/范围/瀑布/子弹/漏斗/金字塔/热力/等值带/蜘蛛柱）。</summary>
        [Parameter] public ElChartType ChartType { get; set; } = ElChartType.Column;

        /// <summary>类别字段名（饼/环为扇区名；热力图为 X 列；雷达为辐射轴名）。</summary>
        [Parameter] public string CategoryField { get; set; }

        /// <summary>数值字段名（Y 轴/半径/扇区值；多值系列见类型说明）。</summary>
        [Parameter] public string ValueField { get; set; }

        /// <summary>数值槽 2 字段名（通用写法，等价于 HighField/Q1Field/TargetField 等语义别名）。</summary>
        [Parameter] public string ValueField2 { get; set; }

        /// <summary>数值槽 3 字段名。</summary>
        [Parameter] public string ValueField3 { get; set; }

        /// <summary>数值槽 4 字段名。</summary>
        [Parameter] public string ValueField4 { get; set; }

        /// <summary>数值槽 5 字段名。</summary>
        [Parameter] public string ValueField5 { get; set; }

        /// <summary>开盘字段名（Candlestick / Ohlc）。等价于 ValueField。</summary>
        [Parameter] public string OpenField { get; set; }

        /// <summary>最高字段名（Candlestick / Ohlc / HighLow / Range 上界）。等价于 ValueField2。</summary>
        [Parameter] public string HighField { get; set; }

        /// <summary>最低字段名（Candlestick / Ohlc / HighLow / Range 下界）。</summary>
        [Parameter] public string LowField { get; set; }

        /// <summary>收盘字段名（Candlestick / Ohlc / HighLow 可选）。</summary>
        [Parameter] public string CloseField { get; set; }

        /// <summary>最小字段名（BoxPlot）。</summary>
        [Parameter] public string MinField { get; set; }

        /// <summary>第一四分位字段名（BoxPlot）。</summary>
        [Parameter] public string Q1Field { get; set; }

        /// <summary>中位数字段名（BoxPlot）。</summary>
        [Parameter] public string MedianField { get; set; }

        /// <summary>第三四分位字段名（BoxPlot）。</summary>
        [Parameter] public string Q3Field { get; set; }

        /// <summary>最大字段名（BoxPlot）。</summary>
        [Parameter] public string MaxField { get; set; }

        /// <summary>目标值字段名（Bullet）。</summary>
        [Parameter] public string TargetField { get; set; }

        /// <summary>小计标记字段名（Waterfall；值非 0 视为小计行）。</summary>
        [Parameter] public string IsTotalField { get; set; }

        /// <summary>第二类别维度字段名（Heatmap / Contour 的 Y 行）。</summary>
        [Parameter] public string GroupField { get; set; }

        /// <summary>数值 X 字段名（散点/气泡专用；提供后改用数值 X 轴而非类别位置）。</summary>
        [Parameter] public string XValueField { get; set; }

        /// <summary>气泡尺寸字段名（Bubble 专用；提供后按数值决定圆点半径）。</summary>
        [Parameter] public string SizeField { get; set; }

        /// <summary>序列标题（图例文本）。</summary>
        [Parameter] public string Title { get; set; }

        /// <summary>序列颜色；为空时由父图表调色板按序分配。</summary>
        [Parameter] public string Color { get; set; }

        /// <summary>是否显示数据标签。默认 false。</summary>
        [Parameter] public bool ShowDataLabels { get; set; }

        /// <summary>线型（折线/面积图生效）。</summary>
        [Parameter] public ElLineStyle LineStyle { get; set; } = ElLineStyle.Solid;

        /// <summary>标记形状（折线/面积图生效）。</summary>
        [Parameter] public ElMarkerType MarkerType { get; set; } = ElMarkerType.Circle;

        /// <summary>该序列绑定的数值轴名称（与 <c>ElChartAxis.Name</c> 匹配即为该次轴；空=主轴）。</summary>
        [Parameter] public string ValueAxisName { get; set; }

        /// <summary>趋势线类型（对齐 Radzen 的 TrendLine / MovingAverage / Polynomial / Mean / Median / Mode）。默认 None。</summary>
        [Parameter] public ElTrendLineType TrendLine { get; set; } = ElTrendLineType.None;

        /// <summary>移动平均窗口（TrendLine=MovingAverage 时生效）。默认 3。</summary>
        [Parameter] public int TrendLinePeriod { get; set; } = 3;

        /// <summary>趋势线线型。默认虚线。</summary>
        [Parameter] public ElLineStyle TrendLineStyle { get; set; } = ElLineStyle.Dashed;

        /// <summary>趋势线颜色（空=跟随序列颜色）。</summary>
        [Parameter] public string TrendLineColor { get; set; }

        /// <summary>K 线/瀑布上涨色。默认中国习惯「涨红」<c>#f56c6c</c>。</summary>
        [Parameter] public string RisingColor { get; set; } = "#f56c6c";

        /// <summary>K 线/瀑布下跌色。默认中国习惯「跌绿」<c>#67c23a</c>。</summary>
        [Parameter] public string FallingColor { get; set; } = "#67c23a";

        /// <summary>数值/提示的格式（如 <c>N2</c>、<c>0.0%</c>；空=自动去尾零）。</summary>
        [Parameter] public string ValueFormat { get; set; }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            Chart?.AddSeries(this);
        }
    }
}
