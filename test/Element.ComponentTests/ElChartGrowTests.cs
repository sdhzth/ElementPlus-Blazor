using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElChartGrow 组件测试（仅覆盖本组件，不跑全量）。
    /// 通过直接操作 cut.Instance.Series / ComputeLayout() / GetSvg() 断言几何体与 SVG 输出，
    /// 另含一个级联注册集成测试验证子组件 ElChartSeries 正确注册到父图表。
    /// </summary>
    public class ElChartGrowTests : BunitContext
    {
        public ElChartGrowTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private class Row
        {
            public string Cat { get; set; }
            public double Val { get; set; }
            public double X { get; set; }
            public double Size { get; set; }
        }

        private static List<Row> MakeRows() => new List<Row>
        {
            new Row { Cat = "Q1", Val = 10 },
            new Row { Cat = "Q2", Val = 20 },
            new Row { Cat = "Q3", Val = 30 },
            new Row { Cat = "Q4", Val = 40 },
        };

        private static ElChartSeries Series(IEnumerable<Row> rows, ElChartType type, string title = "S", string xField = null, string sizeField = null) => new ElChartSeries
        {
            Items = rows,
            ChartType = type,
            CategoryField = "Cat",
            ValueField = "Val",
            Title = title,
            XValueField = xField,
            SizeField = sizeField
        };

        private IRenderedComponent<ElChartGrow> RenderEmpty() => Render<ElChartGrow>();

        [Fact]
        public void EmptyData_RendersEmptyText()
        {
            var cut = RenderEmpty();
            var layout = cut.Instance.ComputeLayout();
            Assert.True(layout.IsEmpty);
            Assert.Contains("暂无数据", cut.Instance.GetSvg());
        }

        [Fact]
        public void ColumnChart_ComputesBarsAndValueTicks()
        {
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(MakeRows(), ElChartType.Column, "Sales"));
            var layout = cut.Instance.ComputeLayout();

            var s = layout.Series.Single();
            Assert.Equal(ElChartType.Column, s.Type);
            Assert.Equal(4, s.Bars.Count);

            // 数值轴：0..40，步长 10
            Assert.Equal(0, layout.ValueTicks.First().Value);
            Assert.Equal(40, layout.ValueTicks.Last().Value);

            // 值越大，柱越高（Y 越小、高度越大）
            Assert.True(s.Bars[3].Y < s.Bars[0].Y);
            Assert.True(s.Bars[3].H > s.Bars[0].H);

            Assert.Contains("<rect", cut.Instance.GetSvg());
        }

        [Fact]
        public void ValueAxisAutoScale_UsesNiceTicks()
        {
            // 参考 RadzenChart 示例：收入 234000..294000 → 自动取整为 0..300000，步长 100000
            var rows = new List<Row>
            {
                new Row { Cat = "Q1", Val = 234000 },
                new Row { Cat = "Q2", Val = 284000 },
                new Row { Cat = "Q3", Val = 274000 },
                new Row { Cat = "Q4", Val = 294000 },
            };
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(rows, ElChartType.Column, "Revenue"));
            var layout = cut.Instance.ComputeLayout();

            Assert.Equal(0, layout.ValueTicks.First().Value);
            Assert.Equal(300000, layout.ValueTicks.Last().Value);
            var step = layout.ValueTicks[1].Value - layout.ValueTicks[0].Value;
            Assert.Equal(100000, step);
        }

        [Fact]
        public void LineChart_ComputesPointsAndPathAndMarkers()
        {
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(MakeRows(), ElChartType.Line, "Trend"));
            var layout = cut.Instance.ComputeLayout();

            var s = layout.Series.Single();
            Assert.Equal(ElChartType.Line, s.Type);
            Assert.Equal(4, s.Points.Count);
            Assert.StartsWith("M", s.Path);

            var svg = cut.Instance.GetSvg();
            Assert.Contains("<path", svg);
            Assert.Contains("<circle", svg); // 折线标记
        }

        [Fact]
        public void StackedColumn_AccumulatesVertically()
        {
            var rows = new List<Row>
            {
                new Row { Cat = "Q1", Val = 10 },
                new Row { Cat = "Q2", Val = 20 },
            };
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(rows, ElChartType.StackedColumn, "Base"));
            cut.Instance.Series.Add(Series(rows, ElChartType.StackedColumn, "Top"));

            var layout = cut.Instance.ComputeLayout();
            var baseBars = layout.Series[0].Bars;
            var topBars = layout.Series[1].Bars;

            // 顶层序列堆叠在底层之上（Y 更小）
            Assert.True(topBars[0].Y < baseBars[0].Y);
        }

        [Fact]
        public void PieChart_SlicesSumTo360()
        {
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(MakeRows(), ElChartType.Pie, "Share"));
            var layout = cut.Instance.ComputeLayout();

            var s = layout.Series.Single();
            Assert.Equal(4, s.Slices.Count);
            var total = s.Slices.Sum(sl => sl.EndDeg - sl.StartDeg);
            Assert.True(System.Math.Abs(total - 360) < 1e-6);
        }

        [Fact]
        public void DonutChart_HasInnerRadius()
        {
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(MakeRows(), ElChartType.Donut, "Share"));
            var layout = cut.Instance.ComputeLayout();

            var s = layout.Series.Single();
            Assert.True(s.Slices[0].RInner > 0);
            Assert.True(s.Slices[0].RInner < s.Slices[0].ROuter);
            Assert.Contains("<path", cut.Instance.GetSvg());
        }

        [Fact]
        public void PieChart_SlicesTakePaletteColors()
        {
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(MakeRows(), ElChartType.Pie, "Share"));
            var layout = cut.Instance.ComputeLayout();

            var s = layout.Series.Single();
            // 四块 = 调色板前四色，互不相同：这是「男女同色」那个缺陷的直接反证。
            var colors = s.Slices.Select(sl => sl.Color).ToList();
            Assert.Equal(4, colors.Count);
            Assert.Equal(4, colors.Distinct().Count());
            Assert.Equal(cut.Instance.BuildModel().Palette.Take(4).ToList(), colors);

            var svg = cut.Instance.GetSvg();
            foreach (var c in colors)
            {
                Assert.Contains("fill=\"" + c + "\"", svg);
            }
        }

        [Fact]
        public void PieChart_ExplicitSeriesColor_KeepsSingleColorSlices()
        {
            var cut = RenderEmpty();
            var series = Series(MakeRows(), ElChartType.Pie, "Share");
            series.Color = "#123456";
            cut.Instance.Series.Add(series);
            var layout = cut.Instance.ComputeLayout();

            // 调用方写了 Color 就是有意单色，不能被调色板覆盖。
            Assert.All(layout.Series.Single().Slices, sl => Assert.Equal("#123456", sl.Color));
        }

        [Fact]
        public void Legend_PieListsSliceCategoriesWithSliceColors()
        {
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(MakeRows(), ElChartType.Pie, "Share"));
            var layout = cut.Instance.ComputeLayout();

            // 单系列饼图按系列列图例只会有一条「Share」，色块与类别名对不上；改为逐块列类别。
            Assert.Equal(4, layout.LegendItems.Count);
            Assert.Equal(new List<string> { "Q1", "Q2", "Q3", "Q4" }, layout.LegendItems.Select(it => it.Title).ToList());
            var sliceColors = layout.Series.Single().Slices.Select(sl => sl.Color).ToList();
            Assert.Equal(sliceColors, layout.LegendItems.Select(it => it.Color).ToList());
            Assert.Contains(">Q4<", cut.Instance.GetSvg());
        }

        [Fact]
        public void DonutChart_CenterText_DrawsAtSliceCenter()
        {
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(MakeRows(), ElChartType.Donut, "Share"));
            cut.Instance.CenterText = "一览";
            var layout = cut.Instance.ComputeLayout();

            var svg = cut.Instance.GetSvg();
            Assert.Contains("el-chart-grow__center", svg);
            Assert.Contains(">一览<", svg);
            var c0 = layout.Series.Single().Slices[0];
            // 圆心文字跟着几何圆心走（宿主用 HTML 叠层冒充时，图例一开圆心就移、字就飘出洞口）。
            Assert.Contains("<text x=\"" + ElChartGrowEngine.Fmt(c0.Cx) + "\" y=\"" + ElChartGrowEngine.Fmt(c0.Cy + 4)
                            + "\" text-anchor=\"middle\" class=\"el-chart-grow__center\"", svg);
        }

        [Fact]
        public void Legend_ItemsRenderedForEachSeries()
        {
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(MakeRows(), ElChartType.Column, "A"));
            cut.Instance.Series.Add(Series(MakeRows(), ElChartType.Line, "B"));
            var layout = cut.Instance.ComputeLayout();

            Assert.Equal(2, layout.LegendItems.Count);
            Assert.Contains("el-chart-grow__legend", cut.Instance.GetSvg());
        }

        [Fact]
        public void CustomAxisMinMaxStep_Respected()
        {
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(MakeRows(), ElChartType.Column, "Sales"));
            cut.Instance.Axes.Add(new ElChartAxis { Type = ElAxisType.Value, Min = 0, Max = 50, Step = 10, Title = "金额" });
            var layout = cut.Instance.ComputeLayout();

            Assert.Equal(0, layout.ValueTicks.First().Value);
            Assert.Equal(50, layout.ValueTicks.Last().Value);
            var step = layout.ValueTicks[1].Value - layout.ValueTicks[0].Value;
            Assert.Equal(10, step);
        }

        [Fact]
        public void BarChart_HorizontalBars()
        {
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(MakeRows(), ElChartType.Bar, "Sales"));
            var layout = cut.Instance.ComputeLayout();

            var s = layout.Series.Single();
            Assert.Equal(4, s.Bars.Count);
            // 条形图：每个条为水平（宽度 > 0，高度为正）
            foreach (var b in s.Bars)
            {
                Assert.True(b.W > 0);
                Assert.True(b.H > 0);
            }
        }

        [Fact]
        public void NegativeValues_ScaleIncludesNegative()
        {
            var rows = new List<Row>
            {
                new Row { Cat = "A", Val = -10 },
                new Row { Cat = "B", Val = 20 },
            };
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(rows, ElChartType.Column, "Delta"));
            var layout = cut.Instance.ComputeLayout();

            Assert.True(layout.ValueTicks.First().Value <= 0);
            Assert.Equal(-10, layout.ValueTicks.First().Value);
        }

        [Fact]
        public void RenderSvg_ReturnsSvgElement()
        {
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(MakeRows(), ElChartType.Area, "Area"));
            var svg = cut.Instance.GetSvg();
            Assert.StartsWith("<svg", svg);
            Assert.Contains("el-chart-grow__svg", svg);
        }

        [Fact]
        public void ChildSeries_RegistersViaCascade()
        {
            RenderFragment childContent = builder =>
            {
                builder.OpenComponent<ElChartSeries>(0);
                builder.AddComponentParameter(1, nameof(ElChartSeries.Items), MakeRows());
                builder.AddComponentParameter(2, nameof(ElChartSeries.ChartType), ElChartType.Column);
                builder.AddComponentParameter(3, nameof(ElChartSeries.CategoryField), "Cat");
                builder.AddComponentParameter(4, nameof(ElChartSeries.ValueField), "Val");
                builder.AddComponentParameter(5, nameof(ElChartSeries.Title), "Sales");
                builder.CloseComponent();
            };

            var cut = Render<ElChartGrow>(p => p.Add(x => x.ChildContent, childContent));

            Assert.Single(cut.Instance.Series);
            var layout = cut.Instance.ComputeLayout();
            Assert.Equal(4, layout.Series.Single().Bars.Count);
            Assert.Contains("<rect", cut.Instance.GetSvg());
        }

        [Fact]
        public void ScatterChart_PlotsMarkersWithoutLine()
        {
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(MakeRows(), ElChartType.Scatter, "Points"));
            var layout = cut.Instance.ComputeLayout();

            var s = layout.Series.Single();
            Assert.Equal(ElChartType.Scatter, s.Type);
            Assert.Equal(4, s.Points.Count);
            // 散点图不应有折线 path（仅标记）
            Assert.DoesNotContain("<path", cut.Instance.GetSvg());
            Assert.Contains("<circle", cut.Instance.GetSvg());
        }

        [Fact]
        public void BubbleChart_PlotsSizedCircles()
        {
            var rows = new List<Row>
            {
                new Row { Cat = "A", Val = 10, Size = 1 },
                new Row { Cat = "B", Val = 20, Size = 2 },
                new Row { Cat = "C", Val = 30, Size = 3 },
                new Row { Cat = "D", Val = 40, Size = 4 },
            };
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(rows, ElChartType.Bubble, "Bubbles", sizeField: "Size"));
            var layout = cut.Instance.ComputeLayout();

            var s = layout.Series.Single();
            Assert.Equal(ElChartType.Bubble, s.Type);
            Assert.Equal(4, s.Points.Count);
            // 尺寸越大半径越大
            Assert.True(s.Points[3].Size > s.Points[0].Size);
            Assert.Contains("<circle", cut.Instance.GetSvg());
        }

        [Fact]
        public void Scatter_NumericXAxis_Respected()
        {
            var rows = new List<Row>
            {
                new Row { Cat = "A", Val = 10, X = 1 },
                new Row { Cat = "B", Val = 20, X = 5 },
                new Row { Cat = "C", Val = 30, X = 10 },
            };
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(rows, ElChartType.Scatter, "XY", xField: "X"));
            var layout = cut.Instance.ComputeLayout();

            // 配置了 XValueField → 使用数值 X 轴（隐藏类别轴）
            Assert.True(layout.XTicks.Count > 0);
            Assert.Empty(layout.CategoryTicks);
            Assert.False(layout.IsRadar);
            // X 越大，像素 X 越靠右
            Assert.True(layout.Series.Single().Points[2].X > layout.Series.Single().Points[0].X);
        }

        [Fact]
        public void RadarChart_ComputesPolygonVertices()
        {
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(MakeRows(), ElChartType.Radar, "R1"));
            cut.Instance.Series.Add(Series(MakeRows(), ElChartType.Radar, "R2"));
            var layout = cut.Instance.ComputeLayout();

            Assert.True(layout.IsRadar);
            Assert.Equal(2, layout.Series.Count);
            foreach (var s in layout.Series)
            {
                // 每个类别一个顶点
                Assert.Equal(4, s.Points.Count);
                Assert.StartsWith("M", s.Path);
                Assert.EndsWith("Z", s.Path);
            }

            var svg = cut.Instance.GetSvg();
            Assert.Contains("<polygon", svg);    // 网格环 / 序列多边形
            Assert.Contains("el-chart-grow__ring", svg);
            Assert.Contains("el-chart-grow__spoke", svg);
        }

        [Fact]
        public void RadarChart_SingleSeries_RendersPolygon()
        {
            var cut = RenderEmpty();
            cut.Instance.Series.Add(Series(MakeRows(), ElChartType.Radar, "R"));
            var layout = cut.Instance.ComputeLayout();

            var s = layout.Series.Single();
            Assert.Equal(4, s.Points.Count);
            Assert.True(layout.RadarR > 0);
            Assert.Contains("<polygon", cut.Instance.GetSvg());
        }

        // ── 左/右图例列的预留宽按实测文字（库批次107 · 消费方批次110zzz-w）──────
        // 死值 120 在窄图表上把三分之二版面划给图例，径向几何夹到 rOuter=1 就画成一条线。

        private const double LegendFont = 12;

        private static double LegendTextW(string title)
            => ElWatermark.EstimateTextWidth(title, LegendFont, string.Empty, string.Empty);

        /// <summary>
        /// 180×150 的环图 + 指定图例位；图例文字 = 扇区类别名，所以预留宽跟着类别名长短走。
        /// 陪衬扇区固定用「乙」（与 1 字的 category 同宽），预留宽就只由 category 那一档决定。
        /// </summary>
        private ElChartLayout LegendLayout(string category, ElLegendPosition position, bool showLegend = true, double width = 180)
        {
            var cut = Render<ElChartGrow>();
            cut.Instance.Width = width;
            cut.Instance.Height = 150;
            cut.Instance.ShowLegend = showLegend;
            cut.Instance.LegendPosition = position;
            cut.Instance.Series.Add(Series(new List<Row>
            {
                new Row { Cat = category, Val = 10 },
                new Row { Cat = "乙", Val = 30 },
            }, ElChartType.Donut, "Share"));
            return cut.Instance.ComputeLayout();
        }

        [Fact]
        public void LegendRight_ShortTitle_ReserveIsMeasuredNotTheDead120()
        {
            var layout = LegendLayout("男", ElLegendPosition.Right);
            var reserve = layout.Width - layout.PlotRight - 8;

            // 色块距绘图区 12 + 色块 12 + 文字前隙 6 = 30，末尾留 4，再扣基础外边 8 ⇒ 26 + 实测文字宽
            Assert.Equal(26 + LegendTextW("男"), reserve, 6);
            // 有牙对照：死值 120 时绘图区右缘钉在 52，实测档必须明显更宽
            Assert.True(layout.PlotRight > 52, $"PlotRight={layout.PlotRight}");
        }

        [Fact]
        public void LegendRight_LongTitle_ReserveGrowsSoTextEndsInsideSvg()
        {
            const string longCat = "病案状态未归档待审核";   // 10 个全角 ≈ 126px
            var layout = LegendLayout(longCat, ElLegendPosition.Right, width: 420);
            var item = layout.LegendItems[0];

            Assert.True(layout.Width - layout.PlotRight - 8 > 120, "长图例的预留宽应超过旧的死值");
            // 文字右缘钉在 SVG 右边 −4：既不裁字，也不白留一整列
            Assert.Equal(layout.Width - 4, item.X + 18 + LegendTextW(item.Title), 6);
        }

        [Fact]
        public void LegendLeft_ShortTitle_PlotStartsClearOfLegendText()
        {
            var layout = LegendLayout("男", ElLegendPosition.Left);
            var item = layout.LegendItems.First(it => it.Title == "男");

            Assert.Equal(8 + 26 + LegendTextW("男"), layout.PlotLeft, 6);
            // Left 档色块钉在 x=12（与预留宽无关），富余的 4px 落在文字与绘图区之间
            Assert.Equal(4, layout.PlotLeft - (item.X + 18 + LegendTextW(item.Title)), 6);
        }

        [Fact]
        public void LegendTopBottom_And_NoLegend_LegendTextLengthMovesNothing()
        {
            var shortTop = LegendLayout("男", ElLegendPosition.Top);
            var longTop = LegendLayout("病案状态未归档待审核", ElLegendPosition.Top);

            // 换口径只动左/右那一列：上下两档的绘图盒必须与文字长短无关
            Assert.Equal(shortTop.PlotLeft, longTop.PlotLeft, 6);
            Assert.Equal(shortTop.PlotRight, longTop.PlotRight, 6);
            Assert.Equal(shortTop.PlotTop, longTop.PlotTop, 6);
            Assert.Equal(shortTop.PlotBottom, longTop.PlotBottom, 6);

            var noLegend = LegendLayout("男", ElLegendPosition.Right, showLegend: false);
            Assert.Equal(noLegend.Width - 8, noLegend.PlotRight, 6);
        }

        [Fact]
        public void DonutOnNarrowChart_ShortLegendKeepsTheRingRound()
        {
            var shortLegend = LegendLayout("男", ElLegendPosition.Right);
            var rOuter = shortLegend.Series.Single().Slices[0].ROuter;

            // 死值 120 只剩 44px 绘图宽 ⇒ rOuter 16（环退化）；实测档要能把环画出来
            Assert.True(rOuter > 40, $"rOuter={rOuter}");
            var longLegend = LegendLayout("病案状态未归档待审核", ElLegendPosition.Right);
            Assert.True(rOuter > longLegend.Series.Single().Slices[0].ROuter + 20, "图例越长，环必须越小");
        }

        [Fact]
        public void LegendRight_ReserveFollowsWidestItemNotTheFirstOne()
        {
            // 首条 1 字、次条 5 字：预留宽必须按最长那条算，否则长的那条会被裁掉。
            var narrow = LegendLayout("甲", ElLegendPosition.Right, width: 420);
            Assert.Equal("甲", narrow.LegendItems[0].Title);
            var wideLayout = Render<ElChartGrow>();
            wideLayout.Instance.Width = 420;
            wideLayout.Instance.Height = 150;
            wideLayout.Instance.Series.Add(Series(new List<Row>
            {
                new Row { Cat = "甲", Val = 10 },
                new Row { Cat = "未归档病案", Val = 30 },
            }, ElChartType.Donut, "Share"));
            var wideLegend = wideLayout.Instance.ComputeLayout();

            Assert.Equal("甲", wideLegend.LegendItems[0].Title);   // 最长那条排在第二位
            Assert.Equal(26 + LegendTextW("乙"), narrow.Width - narrow.PlotRight - 8, 6);
            Assert.Equal(26 + LegendTextW("未归档病案"), wideLegend.Width - wideLegend.PlotRight - 8, 6);
            // 绘图区让出来的那一段，逐像素等于两条图例文字的宽度差
            Assert.Equal(LegendTextW("未归档病案") - LegendTextW("乙"), narrow.PlotRight - wideLegend.PlotRight, 6);
        }
    }
}
