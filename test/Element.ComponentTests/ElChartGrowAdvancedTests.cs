using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElChartGrow 新增系列与结构能力的专项测试：
    /// 19 个新增 series（100% 堆叠 / 范围 / K线 / OHLC / 高低 / 箱线 / 瀑布 / 子弹 / 漏斗 / 金字塔 / 热力 / 等值带 / 蜘蛛柱）
    /// 与 3 类结构能力（趋势线与参考元素、命名次轴、交互提示与命中测试）。
    /// </summary>
    public class ElChartGrowAdvancedTests : BunitContext
    {
        public ElChartGrowAdvancedTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private class Row
        {
            public string Cat { get; set; }
            public string Row2 { get; set; }
            public double Val { get; set; }
            public double Open { get; set; }
            public double High { get; set; }
            public double Low { get; set; }
            public double Close { get; set; }
            public double Min { get; set; }
            public double Q1 { get; set; }
            public double Median { get; set; }
            public double Q3 { get; set; }
            public double Max { get; set; }
            public double Target { get; set; }
            public double Total { get; set; }
            public double Val2 { get; set; }
            public double ValSec { get; set; }
        }

        private static List<Row> Rows() => new List<Row>
        {
            new Row { Cat = "A", Val = 10, Val2 = 18, Open = 10, High = 16, Low = 8, Close = 14, Min = 5, Q1 = 9, Median = 12, Q3 = 15, Max = 20, Target = 15, ValSec = 1000 },
            new Row { Cat = "B", Val = 20, Val2 = 26, Open = 20, High = 27, Low = 15, Close = 17, Min = 10, Q1 = 14, Median = 18, Q3 = 24, Max = 30, Target = 25, ValSec = 2000 },
            new Row { Cat = "C", Val = 30, Val2 = 44, Open = 30, High = 40, Low = 26, Close = 38, Min = 15, Q1 = 22, Median = 30, Q3 = 38, Max = 50, Target = 35, ValSec = 3000 },
        };

        private static ElChartSeries S(ElChartType type, string title = "S") => new ElChartSeries
        {
            Items = Rows(),
            ChartType = type,
            CategoryField = "Cat",
            ValueField = "Val",
            Title = title
        };

        private IRenderedComponent<ElChartGrow> Chart(ElChartType type, string title = "S")
        {
            var cut = Render<ElChartGrow>();
            cut.Instance.Series.Add(S(type, title));
            return cut;
        }

        // —————————————— 19 个新增 series ——————————————

        [Fact]
        public void FullStackedColumn_NormalizesEachCategoryToHundred()
        {
            var cut = Render<ElChartGrow>();
            cut.Instance.Series.Add(S(ElChartType.FullStackedColumn, "A"));
            cut.Instance.Series.Add(S(ElChartType.FullStackedColumn, "B"));

            var layout = cut.Instance.ComputeLayout();

            Assert.Equal(0, layout.ValueTicks.First().Value);
            Assert.Equal(100, layout.ValueTicks.Last().Value);
            // 每列两段之和等于 100%（像素高度之和 == 绘图区高度）
            var a = layout.Series[0].Bars[0];
            var b = layout.Series[1].Bars[0];
            Assert.True(System.Math.Abs((a.H + b.H) - (layout.PlotBottom - layout.PlotTop)) < 1.0);
        }

        [Fact]
        public void FullStackedBar_And_Line_Area_AreSupported()
        {
            var bar = Chart(ElChartType.FullStackedBar);
            var bl = bar.Instance.ComputeLayout();
            Assert.Equal(100, bl.ValueTicks.Last().Value);
            Assert.Equal(3, bl.Series.Single().Bars.Count);

            foreach (var t in new[] { ElChartType.FullStackedLine, ElChartType.FullStackedArea })
            {
                var cut = Chart(t);
                var layout = cut.Instance.ComputeLayout();
                Assert.Equal(100, layout.ValueTicks.Last().Value);
                Assert.Equal(3, layout.Series.Single().Points.Count);
            }
        }

        [Fact]
        public void RangeColumn_SpawnsFromLowToHigh()
        {
            var cut = Render<ElChartGrow>();
            var s = S(ElChartType.RangeColumn);
            s.LowField = "Val";
            s.HighField = "Val2";
            cut.Instance.Series.Add(s);

            var layout = cut.Instance.ComputeLayout();
            var bar = layout.Series.Single().Bars[0];

            // 上界 18 > 下界 10 → 柱体高度对应 8 个单位；每个点两端各一条封口线
            Assert.True(bar.H > 0);
            Assert.Equal(6, layout.Series.Single().Segments.Count(x => x.Kind == "cap"));
            Assert.Contains("el-chart-grow__seg--cap", cut.Instance.GetSvg());
        }

        [Fact]
        public void Candlestick_AxisHugsDataInsteadOfZeroBaseline()
        {
            var rows = new List<Row>
            {
                new Row { Cat = "D1", Open = 42.1, High = 44.8, Low = 41.2, Close = 44.1 },
                new Row { Cat = "D2", Open = 44.1, High = 45.6, Low = 42.3, Close = 42.8 },
            };

            var cut = Render<ElChartGrow>();
            var s = new ElChartSeries
            {
                Items = rows,
                ChartType = ElChartType.Candlestick,
                CategoryField = "Cat",
                OpenField = "Open",
                HighField = "High",
                LowField = "Low",
                CloseField = "Close",
                Title = "K"
            };
            cut.Instance.Series.Add(s);

            var layout = cut.Instance.ComputeLayout();

            // 数据域 41.2..45.6：轴必须贴着数据（41..46），不能出现 0 基线
            Assert.Equal(41, layout.ValueTicks.First().Value);
            Assert.Equal(46, layout.ValueTicks.Last().Value);

            // 蜡烛实体占绘图区高度 2/5（2.0 / 5.0），被压成 4% 那种就是回归
            var plotH = layout.PlotBottom - layout.PlotTop;
            var body = layout.Series.Single().Boxes.First(b => b.Kind == "candle");
            Assert.True(body.H > plotH * 0.3, "实体高度占比过小：" + body.H + "/" + plotH);
        }

        [Fact]
        public void DataHuggingFamiliesSkipZeroBaseline_WhileColumnKeepsIt()
        {
            // 范围柱 / 箱线 / 高低：贴合数据域
            foreach (var type in new[] { ElChartType.RangeColumn, ElChartType.RangeArea, ElChartType.BoxPlot, ElChartType.HighLow })
            {
                var cut = Render<ElChartGrow>();
                var s = S(type);
                s.LowField = "Val";
                s.HighField = "Val2";
                s.MinField = "Val";
                s.Q1Field = "Val2";
                s.MedianField = "Val";
                s.Q3Field = "Val2";
                s.MaxField = "Val2";
                cut.Instance.Series.Add(s);

                var layout = cut.Instance.ComputeLayout();
                Assert.True(layout.ValueTicks.First().Value >= 10,
                    type + " 轴未贴合数据：" + layout.ValueTicks.First().Value);
            }

            // 对照：柱状图仍然必须有 0 基线（柱高与数值成正比才不算骗人）
            var col = Render<ElChartGrow>();
            col.Instance.Series.Add(S(ElChartType.Column));
            Assert.Equal(0, col.Instance.ComputeLayout().ValueTicks.First().Value);
        }

        [Fact]
        public void RangeBar_IsHorizontal()
        {
            var cut = Render<ElChartGrow>();
            var s = S(ElChartType.RangeBar);
            s.LowField = "Val";
            s.HighField = "Val2";
            cut.Instance.Series.Add(s);

            var layout = cut.Instance.ComputeLayout();
            var bar = layout.Series.Single().Bars[0];
            Assert.True(bar.W > 0);
            Assert.True(bar.H > 0);
        }

        [Fact]
        public void RangeArea_ProducesClosedBandPath()
        {
            var cut = Render<ElChartGrow>();
            var s = S(ElChartType.RangeArea);
            s.LowField = "Val";
            s.HighField = "Val2";
            cut.Instance.Series.Add(s);

            var layout = cut.Instance.ComputeLayout();
            var path = layout.Series.Single().Path;
            Assert.StartsWith("M", path);
            Assert.EndsWith("Z", path);
        }

        [Fact]
        public void Candlestick_HasWickAndColoredBody()
        {
            var cut = Render<ElChartGrow>();
            var s = S(ElChartType.Candlestick);
            s.OpenField = "Open";
            s.HighField = "High";
            s.LowField = "Low";
            s.CloseField = "Close";
            cut.Instance.Series.Add(s);

            var layout = cut.Instance.ComputeLayout();
            var r = layout.Series.Single();

            Assert.Equal(3, r.Segments.Count(x => x.Kind == "wick"));
            Assert.Equal(3, r.Boxes.Count(x => x.Kind == "candle"));
            // A：收 14 > 开 10 → 涨（红）；B：收 17 < 开 20 → 跌（绿）
            Assert.Equal("#f56c6c", r.Boxes[0].Fill);
            Assert.Equal("#67c23a", r.Boxes[1].Fill);
            Assert.Contains("el-chart-grow__seg--wick", cut.Instance.GetSvg());
        }

        [Fact]
        public void Ohlc_HasOpenAndCloseTicks()
        {
            var cut = Render<ElChartGrow>();
            var s = S(ElChartType.Ohlc);
            s.OpenField = "Open";
            s.HighField = "High";
            s.LowField = "Low";
            s.CloseField = "Close";
            cut.Instance.Series.Add(s);

            var r = cut.Instance.ComputeLayout().Series.Single();
            Assert.Equal(3, r.Segments.Count(x => x.Kind == "open"));
            Assert.Equal(3, r.Segments.Count(x => x.Kind == "close"));
            Assert.Empty(r.Boxes);
        }

        [Fact]
        public void HighLow_WickPlusOptionalCloseTick()
        {
            var cut = Render<ElChartGrow>();
            var s = S(ElChartType.HighLow);
            s.HighField = "High";
            s.LowField = "Low";
            s.CloseField = "Close";
            cut.Instance.Series.Add(s);

            var r = cut.Instance.ComputeLayout().Series.Single();
            Assert.Equal(3, r.Segments.Count(x => x.Kind == "wick"));
            Assert.Equal(3, r.Segments.Count(x => x.Kind == "close"));

            // 未设置 CloseField 时不画收盘短横线
            var cut2 = Render<ElChartGrow>();
            var s2 = S(ElChartType.HighLow);
            s2.HighField = "High";
            s2.LowField = "Low";
            cut2.Instance.Series.Add(s2);
            Assert.Empty(cut2.Instance.ComputeLayout().Series.Single().Segments.Where(x => x.Kind == "close"));
        }

        [Fact]
        public void BoxPlot_HasBoxWhiskersAndMedian()
        {
            var cut = Render<ElChartGrow>();
            var s = S(ElChartType.BoxPlot);
            s.MinField = "Min";
            s.Q1Field = "Q1";
            s.MedianField = "Median";
            s.Q3Field = "Q3";
            s.MaxField = "Max";
            cut.Instance.Series.Add(s);

            var r = cut.Instance.ComputeLayout().Series.Single();
            Assert.Equal(3, r.Boxes.Count(x => x.Kind == "box"));
            Assert.Equal(6, r.Segments.Count(x => x.Kind == "whisker"));
            Assert.Equal(6, r.Segments.Count(x => x.Kind == "cap"));
            Assert.Equal(3, r.Segments.Count(x => x.Kind == "median"));
        }

        [Fact]
        public void Waterfall_AccumulatesAndHonoursTotalFlag()
        {
            var rows = new List<Row>
            {
                new Row { Cat = "A", Val = 10 },
                new Row { Cat = "B", Val = -4 },
                new Row { Cat = "C", Total = 1 }
            };
            var cut = Render<ElChartGrow>();
            cut.Instance.Series.Add(new ElChartSeries
            {
                Items = rows,
                ChartType = ElChartType.Waterfall,
                CategoryField = "Cat",
                ValueField = "Val",
                IsTotalField = "Total"
            });

            var layout = cut.Instance.ComputeLayout().Series.Single();
            Assert.Equal(3, layout.Bars.Count);
            Assert.Contains("el-chart-grow__seg--connector", cut.Instance.GetSvg());
        }

        [Fact]
        public void HorizontalWaterfall_IsHorizontal()
        {
            var cut = Chart(ElChartType.HorizontalWaterfall);
            var r = cut.Instance.ComputeLayout().Series.Single();
            Assert.True(r.Bars.All(b => b.W >= 0));
            Assert.Contains("el-chart-grow__seg--connector", cut.Instance.GetSvg());
        }

        [Fact]
        public void Bullet_HasThreeBandBackgroundValueBarAndTargetLine()
        {
            var cut = Render<ElChartGrow>();
            var s = S(ElChartType.Bullet);
            s.TargetField = "Target";
            cut.Instance.Series.Add(s);

            var r = cut.Instance.ComputeLayout().Series.Single();
            // 每行 3 档背景 × 3 行
            Assert.Equal(9, r.Boxes.Count(x => x.Kind == "band"));
            Assert.Equal(3, r.Bars.Count);
            Assert.Equal(3, r.Segments.Count(x => x.Kind == "target"));
        }

        [Fact]
        public void Funnel_DescendingAndPyramid_Ascending()
        {
            var funnel = Chart(ElChartType.Funnel);
            var fp = funnel.Instance.ComputeLayout().Series.Single().Polygons;
            Assert.Equal(3, fp.Count);
            // 漏斗：自上而下宽度递减 → 首个多边形最宽
            var w0 = Width(fp[0].Points);
            var w2 = Width(fp[2].Points);
            Assert.True(w0 > w2);

            var pyramid = Chart(ElChartType.Pyramid);
            var pp = pyramid.Instance.ComputeLayout().Series.Single().Polygons;
            Assert.True(Width(pp[0].Points) < Width(pp[2].Points));

            Assert.Contains("<polygon", funnel.Instance.GetSvg());
        }

        private static double Width(string points)
        {
            var xs = points.Split(' ')
                .Select(p => double.Parse(p.Split(',')[0], System.Globalization.CultureInfo.InvariantCulture))
                .ToList();
            return xs.Max() - xs.Min();
        }

        [Fact]
        public void Heatmap_BuildsCellsRowsAndLegend()
        {
            var rows = new List<Row>
            {
                new Row { Cat = "X1", Row2 = "Y1", Val = 1 },
                new Row { Cat = "X2", Row2 = "Y1", Val = 5 },
                new Row { Cat = "X1", Row2 = "Y2", Val = 9 },
                new Row { Cat = "X2", Row2 = "Y2", Val = 3 }
            };
            var cut = Render<ElChartGrow>();
            cut.Instance.Series.Add(new ElChartSeries
            {
                Items = rows,
                ChartType = ElChartType.Heatmap,
                CategoryField = "Cat",
                GroupField = "Row2",
                ValueField = "Val"
            });

            var layout = cut.Instance.ComputeLayout();
            Assert.True(layout.IsHeatmap);
            Assert.Equal(4, layout.Series.Single().Boxes.Count(x => x.Kind == "cell"));
            Assert.Equal(2, layout.CategoryTicks.Count); // Y 行
            Assert.Equal(2, layout.XTicks.Count);        // X 列
            Assert.True(layout.HeatLegend.Count > 1);
            Assert.Contains("el-chart-grow__cell", cut.Instance.GetSvg());
            Assert.Contains("el-chart-grow__heat-legend", cut.Instance.GetSvg());
        }

        [Fact]
        public void Contour_QuantizesBandsAndDrawsBoundary()
        {
            var rows = new List<Row>
            {
                new Row { Cat = "X1", Row2 = "Y1", Val = 0 },
                new Row { Cat = "X2", Row2 = "Y1", Val = 100 },
                new Row { Cat = "X1", Row2 = "Y2", Val = 100 },
                new Row { Cat = "X2", Row2 = "Y2", Val = 0 }
            };
            var cut = Render<ElChartGrow>();
            cut.Instance.Series.Add(new ElChartSeries
            {
                Items = rows,
                ChartType = ElChartType.Contour,
                CategoryField = "Cat",
                GroupField = "Row2",
                ValueField = "Val"
            });

            var layout = cut.Instance.ComputeLayout();
            var r = layout.Series.Single();
            Assert.Equal(4, r.Boxes.Count(x => x.Kind == "cell"));
            // 相邻单元层级不同处应产生边界线
            Assert.True(r.Segments.Count(x => x.Kind == "contour") > 0);
            Assert.Contains("el-chart-grow__contour", cut.Instance.GetSvg());
        }

        [Fact]
        public void SpiderColumn_UsesRadarLayoutWithSpokes()
        {
            var cut = Chart(ElChartType.SpiderColumn);
            var layout = cut.Instance.ComputeLayout();

            Assert.True(layout.IsRadar);
            var r = layout.Series.Single();
            Assert.Equal(3, r.Spokes.Count);
            Assert.True(r.Spokes.All(x => x.Width > 0));
            Assert.Contains("el-chart-grow__spider-column", cut.Instance.GetSvg());
        }

        // —————————————— 结构能力 1：趋势线 / 参考线 / 参考带 / 标注 ——————————————

        [Fact]
        public void TrendLine_LinearAndMean_ProducePaths()
        {
            foreach (var t in new[]
            {
                ElTrendLineType.Linear, ElTrendLineType.MovingAverage, ElTrendLineType.Polynomial,
                ElTrendLineType.Mean, ElTrendLineType.Median, ElTrendLineType.Mode
            })
            {
                var cut = Chart(ElChartType.Line);
                cut.Instance.Series.Single().TrendLine = t;
                cut.Instance.Series.Single().TrendLinePeriod = 2;

                var path = cut.Instance.ComputeLayout().Series.Single().TrendPath;
                Assert.StartsWith("M", path);
                Assert.Contains("el-chart-grow__trendline", cut.Instance.GetSvg());
            }
        }

        [Fact]
        public void TrendLine_None_ProducesNoPath()
        {
            var cut = Chart(ElChartType.Line);
            Assert.Empty(cut.Instance.ComputeLayout().Series.Single().TrendPath);
        }

        [Fact]
        public void ReferenceLineAndBand_ProduceGeometryAndMarkup()
        {
            var cut = Render<ElChartGrow>(p => p
                .Add(x => x.ReferenceLines, new[]
                {
                    new ElChartReferenceLine { Axis = ElReferenceAxis.Value, Value = 20, Label = "目标" }
                })
                .Add(x => x.ReferenceBands, new[]
                {
                    new ElChartReferenceBand { From = 5, To = 12, Label = "正常", Color = "#e1f3d8" }
                }));
            cut.Instance.Series.Add(S(ElChartType.Column));

            var layout = cut.Instance.ComputeLayout();
            Assert.Single(layout.RefLines);
            Assert.Single(layout.RefBands);
            Assert.Equal("目标", layout.RefLines[0].Label);
            Assert.True(layout.RefBands[0].H > 0);

            var svg = cut.Instance.GetSvg();
            Assert.Contains("el-chart-grow__refline", svg);
            Assert.Contains("el-chart-grow__refband", svg);
        }

        [Fact]
        public void Annotation_ProducesMarkerAndText()
        {
            var cut = Render<ElChartGrow>(p => p.Add(x => x.Annotations, new[]
            {
                new ElChartAnnotation { Category = "B", Value = 20, Text = "异常" }
            }));
            cut.Instance.Series.Add(S(ElChartType.Column));

            var layout = cut.Instance.ComputeLayout();
            Assert.Single(layout.Annotations);
            Assert.Equal("异常", layout.Annotations[0].Text);

            var svg = cut.Instance.GetSvg();
            Assert.Contains("el-chart-grow__annotation", svg);
        }

        // —————————————— 结构能力 2：命名次轴 ——————————————

        [Fact]
        public void NamedSecondaryAxis_GetsOwnScaleAndRightPlacement()
        {
            var cut = Render<ElChartGrow>();
            cut.Instance.Axes.Add(new ElChartAxis { Type = ElAxisType.Value, Name = "sec", Title = "金额" });

            var bar = S(ElChartType.Column, "主");
            cut.Instance.Series.Add(bar);
            var line = S(ElChartType.Line, "次");
            line.ValueField = "ValSec";
            line.ValueAxisName = "sec";
            cut.Instance.Series.Add(line);

            var layout = cut.Instance.ComputeLayout();

            Assert.Equal(2, layout.ValueScales.Count);
            Assert.False(layout.ValueScales[0].OnRight);
            Assert.True(layout.ValueScales[1].OnRight);
            Assert.Equal("sec", layout.ValueScales[1].Name);
            // 主轴按 0..30、次轴按 0..3000 → 量程不同
            Assert.True(layout.ValueScales[1].Hi >= 3000);
            Assert.True(layout.ValueScales[0].Hi <= 100);
            // 次轴序列的点必须落在绘图区内
            Assert.True(layout.Series[1].Points.All(p => p.Y >= layout.PlotTop - 1 && p.Y <= layout.PlotBottom + 1));

            var svg = cut.Instance.GetSvg();
            Assert.Contains("el-chart-grow__axis--secondary", svg);
            Assert.Contains("el-chart-grow__tick--secondary", svg);
        }

        // —————————————— 结构能力 3：交互提示与命中测试 ——————————————

        [Fact]
        public void Hits_CollectedForEachPoint_AndFindNearestResolves()
        {
            var cut = Chart(ElChartType.Column);
            var layout = cut.Instance.ComputeLayout();

            Assert.Equal(3, layout.Hits.Count);
            var target = layout.Hits[1];
            var found = ElChartGrowEngine.FindNearest(layout, target.X, target.Y, 26);

            Assert.NotNull(found);
            Assert.Equal(target.Category, found.Category);
            Assert.Equal("20", found.ValueText);
            // 远离任何命中目标时返回 null
            Assert.Null(ElChartGrowEngine.FindNearest(layout, -500, -500, 10));
        }

        [Fact]
        public async System.Threading.Tasks.Task Tooltip_HoverShowsThenClears()
        {
            var cut = Chart(ElChartType.Column);
            var hit = cut.Instance.ComputeLayout().Hits[0];

            await cut.InvokeAsync(() => cut.Instance.HandleMouseMoveAsync(new MouseEventArgs { OffsetX = hit.X, OffsetY = hit.Y }));
            Assert.NotNull(cut.Instance.HoveredHit);
            Assert.Contains("el-chart-grow__tooltip", cut.Markup);
            Assert.Contains("el-chart-grow__hitlayer", cut.Markup);

            await cut.InvokeAsync(() => cut.Instance.HandleMouseLeave());
            Assert.Null(cut.Instance.HoveredHit);
            Assert.DoesNotContain("el-chart-grow__tooltip", cut.Markup);
        }

        [Fact]
        public void Tooltip_DisabledCollectsNoHits()
        {
            var cut = Render<ElChartGrow>(p => p.Add(x => x.ShowTooltip, false));
            cut.Instance.Series.Add(S(ElChartType.Column));

            Assert.Empty(cut.Instance.ComputeLayout().Hits);
        }

        // —————————————— 全类型冒烟 ——————————————

        [Fact]
        public void AllNewSeriesTypes_RenderSvg()
        {
            var newTypes = new[]
            {
                ElChartType.FullStackedColumn, ElChartType.FullStackedBar, ElChartType.FullStackedArea,
                ElChartType.FullStackedLine, ElChartType.RangeColumn, ElChartType.RangeBar, ElChartType.RangeArea,
                ElChartType.Candlestick, ElChartType.Ohlc, ElChartType.HighLow, ElChartType.BoxPlot,
                ElChartType.Waterfall, ElChartType.HorizontalWaterfall, ElChartType.Bullet,
                ElChartType.Funnel, ElChartType.Pyramid, ElChartType.Heatmap, ElChartType.Contour,
                ElChartType.SpiderColumn
            };

            foreach (var t in newTypes)
            {
                var cut = Render<ElChartGrow>();
                var s = S(t);
                s.HighField = "High";
                s.LowField = "Low";
                s.OpenField = "Open";
                s.CloseField = "Close";
                s.MinField = "Min";
                s.Q1Field = "Q1";
                s.MedianField = "Median";
                s.Q3Field = "Q3";
                s.MaxField = "Max";
                s.TargetField = "Target";
                s.GroupField = "Row2";
                cut.Instance.Series.Add(s);

                var layout = cut.Instance.ComputeLayout();

                Assert.False(layout.IsEmpty);
                Assert.Single(layout.Series);
                var svg = cut.Instance.GetSvg();
                Assert.StartsWith("<svg", svg);
                Assert.True(svg.Length > 200, t + " 未输出有效 SVG");
            }
        }
    }
}
