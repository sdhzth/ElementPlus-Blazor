using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ElementPlus;

namespace ElementPlus
{
    /// <summary>
    /// 图表（Element Plus 自渲染版，零 Radzen 依赖）。
    /// 参考 Radzen.Blazor 的 RadzenChart：用子组件 <see cref="ElChartSeries"/> / <see cref="ElChartAxis"/> 声明序列与坐标轴，
    /// 但本组件不引入任何 Radzen 代码——全部几何与 SVG 标记均在 C# 内计算（见 <see cref="ElChartGrowEngine"/>），
    /// 屏幕渲染与 <c>RenderSvg</c> 导出共用同一套布局真源。
    /// </summary>
    public partial class ElChartGrow : ElementComponentBase
    {
        /// <summary>画布宽度（像素）。默认 600。</summary>
        [Parameter] public double Width { get; set; } = 600;

        /// <summary>画布高度（像素）。默认 360。</summary>
        [Parameter] public double Height { get; set; } = 360;

        /// <summary>图表标题（可选）。</summary>
        [Parameter] public string Title { get; set; }

        /// <summary>是否显示图例。默认 true。</summary>
        [Parameter] public bool ShowLegend { get; set; } = true;

        /// <summary>图例位置。默认 Right。</summary>
        [Parameter] public ElLegendPosition LegendPosition { get; set; } = ElLegendPosition.Right;

        /// <summary>饼/环图圆心文字（环图常用来写「一览」「总计」）。画在 SVG 内、跟着圆心走，空则不画。</summary>
        [Parameter] public string CenterText { get; set; }

        /// <summary>是否显示类别轴。默认 true（径向图忽略）。</summary>
        [Parameter] public bool ShowCategoryAxis { get; set; } = true;

        /// <summary>是否显示数值轴。默认 true。</summary>
        [Parameter] public bool ShowValueAxis { get; set; } = true;

        /// <summary>是否显示网格线。默认 true。</summary>
        [Parameter] public bool ShowGridLines { get; set; } = true;

        /// <summary>自定义调色板（序列颜色不足时循环使用）。</summary>
        [Parameter] public IEnumerable<string> Palette { get; set; }

        /// <summary>空数据提示文本。</summary>
        [Parameter] public string EmptyText { get; set; } = "暂无数据";

        /// <summary>子内容（ElChartSeries / ElChartAxis 集合）。</summary>
        [Parameter] public RenderFragment ChildContent { get; set; }

        /// <summary>参考线集合（目标线/警戒线，横向按数值、纵向按类别）。</summary>
        [Parameter] public IEnumerable<ElChartReferenceLine> ReferenceLines { get; set; }

        /// <summary>参考带集合（区间底色，如正常范围）。</summary>
        [Parameter] public IEnumerable<ElChartReferenceBand> ReferenceBands { get; set; }

        /// <summary>标注集合（在指定坐标画标记 + 文本）。</summary>
        [Parameter] public IEnumerable<ElChartAnnotation> Annotations { get; set; }

        /// <summary>是否启用悬停提示与命中高亮。默认 true。</summary>
        [Parameter] public bool ShowTooltip { get; set; } = true;

        /// <summary>悬停命中的最大容差（像素）。默认 26。</summary>
        [Parameter] public double TooltipTolerance { get; set; } = 26;

        /// <summary>已注册的序列（供测试/编程直接操作）。</summary>
        public List<ElChartSeries> Series { get; } = new List<ElChartSeries>();

        /// <summary>已注册的坐标轴（供测试/编程直接操作）。</summary>
        public List<ElChartAxis> Axes { get; } = new List<ElChartAxis>();

        internal void AddSeries(ElChartSeries series)
        {
            if (series != null && !Series.Contains(series))
            {
                Series.Add(series);
            }
        }

        internal void RemoveSeries(ElChartSeries series)
        {
            if (series != null)
            {
                Series.Remove(series);
            }
        }

        internal void AddAxis(ElChartAxis axis)
        {
            if (axis != null && !Axes.Contains(axis))
            {
                Axes.Add(axis);
            }
        }

        internal void RemoveAxis(ElChartAxis axis)
        {
            if (axis != null)
            {
                Axes.Remove(axis);
            }
        }

        /// <summary>由当前参数与已注册子组件构建纯数据模型（供引擎计算）。</summary>
        public ElChartModel BuildModel()
        {
            var model = new ElChartModel
            {
                Width = Width,
                Height = Height,
                Title = Title,
                ShowLegend = ShowLegend,
                LegendPosition = LegendPosition,
                ShowCategoryAxis = ShowCategoryAxis,
                ShowValueAxis = ShowValueAxis,
                ShowGridLines = ShowGridLines,
                EmptyText = EmptyText ?? "暂无数据",
                CenterText = CenterText ?? string.Empty,
                CollectHits = ShowTooltip
            };

            if (ReferenceLines != null)
            {
                model.ReferenceLines = ReferenceLines.ToList();
            }

            if (ReferenceBands != null)
            {
                model.ReferenceBands = ReferenceBands.ToList();
            }

            if (Annotations != null)
            {
                model.Annotations = Annotations.ToList();
            }

            if (Palette != null)
            {
                model.Palette = Palette.ToList();
            }

            foreach (var ax in Axes)
            {
                model.Axes.Add(new ElChartAxisModel
                {
                    Type = ax.Type,
                    Name = ax.Name ?? string.Empty,
                    OnRight = ax.Type == ElAxisType.Value && !string.IsNullOrWhiteSpace(ax.Name),
                    Title = ax.Title ?? string.Empty,
                    Min = ax.Min,
                    Max = ax.Max,
                    Step = ax.Step,
                    Formatter = ax.Formatter
                });
            }

            var palette = model.Palette;
            var idx = 0;
            foreach (var s in Series)
            {
                var color = string.IsNullOrEmpty(s.Color) && palette.Count > 0
                    ? palette[idx % palette.Count]
                    : (s.Color ?? string.Empty);

                var slots = ResolveSlots(s);

                model.Series.Add(new ElChartSeriesModel
                {
                    Type = s.ChartType,
                    Title = s.Title ?? string.Empty,
                    Color = color,
                    // 饼/环要按这个标志决定「整图一色」还是「逐块调色板」：调用方写了 Color 就是有意单色。
                    ColorExplicit = !string.IsNullOrEmpty(s.Color),
                    ShowDataLabels = s.ShowDataLabels,
                    LineStyle = s.LineStyle,
                    MarkerType = s.MarkerType,
                    Points = ResolvePoints(s),
                    XValueField = s.XValueField ?? string.Empty,
                    SizeField = s.SizeField ?? string.Empty,
                    ValueField2 = slots.F2,
                    ValueField3 = slots.F3,
                    ValueField4 = slots.F4,
                    ValueField5 = slots.F5,
                    GroupField = s.GroupField ?? string.Empty,
                    ValueAxisName = s.ValueAxisName ?? string.Empty,
                    TrendLine = s.TrendLine,
                    TrendLinePeriod = s.TrendLinePeriod,
                    TrendLineStyle = s.TrendLineStyle,
                    TrendLineColor = s.TrendLineColor ?? string.Empty,
                    RisingColor = string.IsNullOrWhiteSpace(s.RisingColor) ? "#f56c6c" : s.RisingColor,
                    FallingColor = string.IsNullOrWhiteSpace(s.FallingColor) ? "#67c23a" : s.FallingColor,
                    ValueFormat = s.ValueFormat ?? string.Empty
                });

                idx++;
            }

            return model;
        }

        /// <summary>计算图表布局（坐标/刻度/各序列几何），与渲染共用，便于测试断言。</summary>
        public ElChartLayout ComputeLayout() => ElChartGrowEngine.ComputeLayout(BuildModel());

        // ————— 交互层（悬停提示 + 命中高亮） —————

        /// <summary>是否绘制悬停十字准线。默认 true。</summary>
        [Parameter] public bool ShowTooltipCrosshair { get; set; } = true;

        private ElChartHit _hover;
        private ElChartLayout _layoutCache;

        /// <summary>当前缓存的布局（交互与覆盖层共用）。</summary>
        public ElChartLayout Layout => _layoutCache ??= ComputeLayout();

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            _layoutCache = null;
            if (!ShowTooltip)
            {
                _hover = null;
            }
        }

        /// <summary>悬停位置对应的命中目标（测试可读）。</summary>
        public ElChartHit HoveredHit => _hover;

        private string TooltipStyle
        {
            get
            {
                if (_hover == null)
                {
                    return string.Empty;
                }

                const double tipW = 190;
                var left = Math.Min(Math.Max(4, _hover.X + 14), Math.Max(4, Width - tipW - 4));
                var top = Math.Max(4, _hover.Y - 14);
                return "left:" + left.ToString(CultureInfo.InvariantCulture) + "px;top:"
                       + top.ToString(CultureInfo.InvariantCulture) + "px;";
            }
        }

        /// <summary>鼠标在画布内移动时更新命中目标（坐标与 viewBox 1:1）。</summary>
        public Task HandleMouseMoveAsync(MouseEventArgs e)
        {
            if (!ShowTooltip)
            {
                return Task.CompletedTask;
            }

            var hit = ElChartGrowEngine.FindNearest(Layout, e.OffsetX, e.OffsetY, TooltipTolerance);
            var changed = !ReferenceEquals(hit, _hover)
                          && !(hit != null && _hover != null && hit.SeriesIndex == _hover.SeriesIndex
                               && hit.PointIndex == _hover.PointIndex && hit.Category == _hover.Category);
            if (changed)
            {
                _hover = hit;
                StateHasChanged();
            }

            return Task.CompletedTask;
        }

        /// <summary>鼠标离开画布时清除命中目标。</summary>
        public void HandleMouseLeave()
        {
            if (_hover != null)
            {
                _hover = null;
                StateHasChanged();
            }
        }

        private static string Fmt(double v) => ElChartGrowEngine.Fmt(v);

        /// <summary>生成完整 SVG 字符串（含外层 svg）。</summary>
        public string GetSvg() => ElChartGrowEngine.RenderSvg(BuildModel());

        /// <summary>内层 SVG 标记（供 Razor 以 MarkupString 注入）。</summary>
        public string InnerMarkup => GetSvg();

        /// <summary>
        /// 按类型语义解析 5 个数值槽的字段名（语义别名优先，缺省回落到通用 ValueFieldN）。
        /// 同一个结果既用于取值，也回写到 <see cref="ElChartSeriesModel"/> 的 ValueField2..5，
        /// 使引擎能据「该槽是否真的提供过字段」决定可选几何（如 HighLow 的收盘短横线）。
        /// </summary>
        private static (string F1, string F2, string F3, string F4, string F5) ResolveSlots(ElChartSeries s)
        {
            static string First(string a, string b) => string.IsNullOrWhiteSpace(a) ? (b ?? string.Empty) : a;

            string f1, f2, f3, f4, f5;
            switch (s.ChartType)
            {
                case ElChartType.Candlestick:
                case ElChartType.Ohlc:
                    f1 = First(s.OpenField, s.ValueField);
                    f2 = First(s.HighField, s.ValueField2);
                    f3 = First(s.LowField, s.ValueField3);
                    f4 = First(s.CloseField, s.ValueField4);
                    f5 = s.ValueField5;
                    break;

                case ElChartType.HighLow:
                    f1 = First(s.HighField, s.ValueField);
                    f2 = First(s.LowField, s.ValueField2);
                    f3 = First(s.CloseField, s.ValueField3);
                    f4 = s.ValueField4;
                    f5 = s.ValueField5;
                    break;

                case ElChartType.BoxPlot:
                    f1 = First(s.MinField, s.ValueField);
                    f2 = First(s.Q1Field, s.ValueField2);
                    f3 = First(s.MedianField, s.ValueField3);
                    f4 = First(s.Q3Field, s.ValueField4);
                    f5 = First(s.MaxField, s.ValueField5);
                    break;

                case ElChartType.RangeArea:
                case ElChartType.RangeBar:
                case ElChartType.RangeColumn:
                    f1 = First(s.LowField, s.ValueField);
                    f2 = First(s.HighField, s.ValueField2);
                    f3 = s.ValueField3;
                    f4 = s.ValueField4;
                    f5 = s.ValueField5;
                    break;

                case ElChartType.Bullet:
                    f1 = s.ValueField ?? string.Empty;
                    f2 = First(s.TargetField, s.ValueField2);
                    f3 = s.ValueField3;
                    f4 = s.ValueField4;
                    f5 = s.ValueField5;
                    break;

                case ElChartType.Waterfall:
                case ElChartType.HorizontalWaterfall:
                    f1 = s.ValueField ?? string.Empty;
                    f2 = First(s.IsTotalField, s.ValueField2);
                    f3 = s.ValueField3;
                    f4 = s.ValueField4;
                    f5 = s.ValueField5;
                    break;

                default:
                    f1 = s.ValueField ?? string.Empty;
                    f2 = s.ValueField2;
                    f3 = s.ValueField3;
                    f4 = s.ValueField4;
                    f5 = s.ValueField5;
                    break;
            }

            return (f1, f2, f3, f4, f5);
        }

        private List<ElChartPoint> ResolvePoints(ElChartSeries s)
        {
            var points = new List<ElChartPoint>();
            if (s?.Items == null)
            {
                return points;
            }

            var (f1, f2, f3, f4, f5) = ResolveSlots(s);

            foreach (var item in s.Items.Cast<object>())
            {
                if (item == null)
                {
                    continue;
                }

                var category = GetFieldValue(item, s.CategoryField)?.ToString() ?? string.Empty;

                double x = 0;
                if (!string.IsNullOrWhiteSpace(s.XValueField))
                {
                    x = ToDouble(GetFieldValue(item, s.XValueField));
                }

                double size = 0;
                if (!string.IsNullOrWhiteSpace(s.SizeField))
                {
                    size = ToDouble(GetFieldValue(item, s.SizeField));
                }

                var point = new ElChartPoint
                {
                    Category = category,
                    X = x,
                    Size = size,
                    Value = ToDouble(GetFieldValue(item, f1)),
                    Value2 = ToDouble(GetFieldValue(item, f2)),
                    Value3 = ToDouble(GetFieldValue(item, f3)),
                    Value4 = ToDouble(GetFieldValue(item, f4)),
                    Value5 = ToDouble(GetFieldValue(item, f5)),
                    Group = string.IsNullOrWhiteSpace(s.GroupField)
                        ? string.Empty
                        : (GetFieldValue(item, s.GroupField)?.ToString() ?? string.Empty)
                };

                point.IsTotal = !string.IsNullOrWhiteSpace(s.IsTotalField)
                    ? ToDouble(GetFieldValue(item, s.IsTotalField)) != 0
                    : !string.IsNullOrWhiteSpace(s.ValueField2) && ToDouble(GetFieldValue(item, s.ValueField2)) != 0;

                points.Add(point);
            }

            return points;
        }

        private static double ToDouble(object value)
        {
            if (value == null)
            {
                return 0;
            }

            if (value is double d)
            {
                return d;
            }

            if (value is int or long or float or decimal)
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }

            return double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : 0;
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

            var prop = row.GetType().GetProperty(field, BindingFlags.Public | BindingFlags.Instance);
            return prop?.GetValue(row);
        }
    }
}
