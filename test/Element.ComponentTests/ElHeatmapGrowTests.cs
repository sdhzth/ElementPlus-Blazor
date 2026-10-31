using Bunit;
using ElementPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElHeatmapGrow 组件测试（仅覆盖本组件，不跑全量）。
    /// 覆盖两种形态：类别网格（对齐 RadzenHeatmap）与数值坐标网格（对齐 RadzenHeatmapSeries），
    /// 并覆盖线性配色、分段配色、数值坐标半格扩展、格内数值阈值、色阶图例与 SVG 结构。
    /// </summary>
    public class ElHeatmapGrowTests : BunitContext
    {
        public ElHeatmapGrowTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private class Row
        {
            public string XCat { get; set; }
            public string YCat { get; set; }
            public string VText { get; set; }
            public double XPt { get; set; }
            public double YPt { get; set; }
            public double Val { get; set; }
        }

        /// <summary>3 列 × 2 行网格，强度 10/20/30/40/50/60，X 坐标 0/1/2，Y 坐标 0/1。</summary>
        private static List<Row> Grid() => new List<Row>
        {
            new Row { XCat = "A", YCat = "R1", XPt = 0, YPt = 0, Val = 10, VText = "10" },
            new Row { XCat = "B", YCat = "R1", XPt = 1, YPt = 0, Val = 20, VText = "20" },
            new Row { XCat = "C", YCat = "R1", XPt = 2, YPt = 0, Val = 30, VText = "30" },
            new Row { XCat = "A", YCat = "R2", XPt = 0, YPt = 1, Val = 40, VText = "40" },
            new Row { XCat = "B", YCat = "R2", XPt = 1, YPt = 1, Val = 50, VText = "50" },
            new Row { XCat = "C", YCat = "R2", XPt = 2, YPt = 1, Val = 60, VText = "60" },
        };

        private IRenderedComponent<ElHeatmapGrow> Category()
            => Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, Grid())
                .Add(c => c.XProperty, nameof(Row.XCat))
                .Add(c => c.YProperty, nameof(Row.YCat))
                .Add(c => c.ValueProperty, nameof(Row.Val)));

        private IRenderedComponent<ElHeatmapGrow> Numeric()
            => Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, Grid())
                .Add(c => c.XProperty, nameof(Row.XPt))
                .Add(c => c.YProperty, nameof(Row.YPt))
                .Add(c => c.ValueProperty, nameof(Row.Val))
                .Add(c => c.NumericAxes, true));

        // ------------------------------------------------------------ 空状态

        [Fact]
        public void EmptyData_RendersEmptyText()
        {
            var cut = Render<ElHeatmapGrow>();
            var layout = cut.Instance.ComputeLayout();

            Assert.True(layout.IsEmpty);
            Assert.Contains("暂无数据", cut.Instance.GetSvg());
        }

        [Fact]
        public void CustomEmptyText_IsRendered()
        {
            var cut = Render<ElHeatmapGrow>(p => p.Add(c => c.EmptyText, "无数据"));
            Assert.Contains("无数据", cut.Instance.GetSvg());
        }

        // ------------------------------------------------------------ 类别网格

        [Fact]
        public void CategoryGrid_ComputesEqualCells()
        {
            var layout = Category().Instance.ComputeLayout();

            Assert.False(layout.IsEmpty);
            Assert.Equal(3, layout.XCategories.Count);
            Assert.Equal(2, layout.YCategories.Count);
            Assert.Equal(6, layout.Cells.Count);
            Assert.Equal(new[] { "A", "B", "C" }, layout.XCategories.ToArray());
            Assert.Equal(new[] { "R1", "R2" }, layout.YCategories.ToArray());
            Assert.Equal(3, layout.XTicks.Count);
            Assert.Equal(2, layout.YTicks.Count);

            // 所有单元格尺寸一致（等分带宽减两侧内缩）。
            var widths = layout.Cells.Select(c => c.Width).Distinct().ToList();
            var heights = layout.Cells.Select(c => c.Height).Distinct().ToList();
            Assert.Single(widths);
            Assert.Single(heights);
            Assert.True(widths[0] > 0 && heights[0] > 0);
        }

        [Fact]
        public void CategoryGrid_AppliesCellPadding()
        {
            var layout = Category().Instance.ComputeLayout();
            var first = layout.Cells.First(c => c.XIndex == 0 && c.YIndex == 0);

            // 左上角单元格：左/上边界为绘图区原点内缩一个 padding。
            Assert.Equal(layout.PlotLeft + 2, first.Left, 6);
            Assert.Equal(layout.PlotTop + 2, first.Top, 6);
        }

        [Fact]
        public void CategoryGrid_KeepsCategoryOrderOfFirstAppearance()
        {
            var rows = new List<Row>
            {
                new Row { XCat = "C", YCat = "R2", Val = 1 },
                new Row { XCat = "A", YCat = "R1", Val = 2 },
                new Row { XCat = "C", YCat = "R1", Val = 3 },
            };

            var cut = Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, rows)
                .Add(c => c.XProperty, nameof(Row.XCat))
                .Add(c => c.YProperty, nameof(Row.YCat))
                .Add(c => c.ValueProperty, nameof(Row.Val)));

            var layout = cut.Instance.ComputeLayout();
            Assert.Equal(new[] { "C", "A" }, layout.XCategories.ToArray());
            Assert.Equal(new[] { "R2", "R1" }, layout.YCategories.ToArray());
        }

        [Fact]
        public void UndefinedCells_AreSkipped()
        {
            var rows = new List<Row>
            {
                new Row { XCat = "A", YCat = "R1", VText = "10" },
                new Row { XCat = "B", YCat = "R1", VText = "oops" },
            };

            var cut = Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, rows)
                .Add(c => c.XProperty, nameof(Row.XCat))
                .Add(c => c.YProperty, nameof(Row.YCat))
                .Add(c => c.ValueProperty, nameof(Row.VText)));

            var layout = cut.Instance.ComputeLayout();
            Assert.Single(layout.Cells);
            Assert.True(layout.Cells[0].IsDefined);
            Assert.DoesNotContain("oops", cut.Instance.GetSvg());
        }

        [Fact]
        public void AllValuesUndefined_TreatedAsEmpty()
        {
            var rows = new List<Row> { new Row { XCat = "A", YCat = "R1", VText = "x" } };

            var cut = Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, rows)
                .Add(c => c.XProperty, nameof(Row.XCat))
                .Add(c => c.YProperty, nameof(Row.YCat))
                .Add(c => c.ValueProperty, nameof(Row.VText)));

            Assert.True(cut.Instance.ComputeLayout().IsEmpty);
        }

        // ------------------------------------------------------------ 颜色映射

        [Fact]
        public void LinearColorMapping_UsesBothEndPoints()
        {
            var layout = Category().Instance.ComputeLayout();

            var min = layout.Cells.Single(c => c.Value == 10);
            var max = layout.Cells.Single(c => c.Value == 60);

            Assert.Equal("#F0F0F0", min.Color);
            Assert.Equal("#1A9641", max.Color);
            Assert.Equal(10, layout.MinValue);
            Assert.Equal(60, layout.MaxValue);
        }

        [Fact]
        public void LinearColorMapping_InterpolatesMidpoint()
        {
            var layout = Category().Instance.ComputeLayout();
            var mid = layout.Cells.Single(c => c.Value == 30);

            // t=(30-10)/50=0.4，#f0f0f0 → #1a9641 在 t=0.4 处为 #9ACCAA。
            Assert.Equal("#9ACCAA", mid.Color);
        }

        [Fact]
        public void CustomEndColors_AreRespected()
        {
            var cut = Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, Grid())
                .Add(c => c.XProperty, nameof(Row.XCat))
                .Add(c => c.YProperty, nameof(Row.YCat))
                .Add(c => c.ValueProperty, nameof(Row.Val))
                .Add(c => c.MinColor, "#000000")
                .Add(c => c.MaxColor, "#ffffff"));

            var layout = cut.Instance.ComputeLayout();
            Assert.Equal("#000000", layout.Cells.Single(c => c.Value == 10).Color);
            Assert.Equal("#FFFFFF", layout.Cells.Single(c => c.Value == 60).Color);
            Assert.Equal("#666666", layout.Cells.Single(c => c.Value == 30).Color);
        }

        [Fact]
        public void SingleValue_ColorFallsBackToMidpoint()
        {
            var rows = new List<Row> { new Row { XCat = "A", YCat = "R1", Val = 7 } };

            var cut = Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, rows)
                .Add(c => c.XProperty, nameof(Row.XCat))
                .Add(c => c.YProperty, nameof(Row.YCat))
                .Add(c => c.ValueProperty, nameof(Row.Val)));

            // 取值范围退化时取 t=0.5。
            Assert.Equal("#85C398", cut.Instance.ComputeLayout().Cells[0].Color);
        }

        [Fact]
        public void ColorRanges_TakePrecedenceOverLinearMapping()
        {
            var ranges = new List<ElHeatmapColorRange>
            {
                new ElHeatmapColorRange(0, 30, "#00ff00"),
                new ElHeatmapColorRange(31, 100, "#ff0000"),
            };

            var cut = Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, Grid())
                .Add(c => c.XProperty, nameof(Row.XCat))
                .Add(c => c.YProperty, nameof(Row.YCat))
                .Add(c => c.ValueProperty, nameof(Row.Val))
                .Add(c => c.ColorRanges, ranges));

            var layout = cut.Instance.ComputeLayout();
            Assert.Equal("#00ff00", layout.Cells.Single(c => c.Value == 10).Color);
            Assert.Equal("#00ff00", layout.Cells.Single(c => c.Value == 30).Color);
            Assert.Equal("#ff0000", layout.Cells.Single(c => c.Value == 40).Color);
            Assert.Equal("#ff0000", layout.Cells.Single(c => c.Value == 60).Color);
        }

        [Fact]
        public void ColorRanges_UnmatchedFallsBackToFill()
        {
            var ranges = new List<ElHeatmapColorRange> { new ElHeatmapColorRange(0, 15, "#00ff00") };

            var cut = Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, Grid())
                .Add(c => c.XProperty, nameof(Row.XCat))
                .Add(c => c.YProperty, nameof(Row.YCat))
                .Add(c => c.ValueProperty, nameof(Row.Val))
                .Add(c => c.ColorRanges, ranges)
                .Add(c => c.Fill, "#cccccc"));

            var layout = cut.Instance.ComputeLayout();
            Assert.Equal("#00ff00", layout.Cells.Single(c => c.Value == 10).Color);
            Assert.Equal("#cccccc", layout.Cells.Single(c => c.Value == 60).Color);
        }

        [Fact]
        public void ColorRanges_UnmatchedWithoutFillIsTransparent()
        {
            var ranges = new List<ElHeatmapColorRange> { new ElHeatmapColorRange(0, 15, "#00ff00") };

            var cut = Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, Grid())
                .Add(c => c.XProperty, nameof(Row.XCat))
                .Add(c => c.YProperty, nameof(Row.YCat))
                .Add(c => c.ValueProperty, nameof(Row.Val))
                .Add(c => c.ColorRanges, ranges));

            Assert.Equal("transparent", cut.Instance.ComputeLayout().Cells.Single(c => c.Value == 60).Color);
        }

        // ------------------------------------------------------------ 数值坐标

        [Fact]
        public void NumericAxes_InfersCellStepFromSmallestGap()
        {
            var layout = Numeric().Instance.ComputeLayout();

            Assert.True(layout.IsNumeric);
            Assert.Equal(new double[] { 0, 1, 2 }, layout.XValues.ToArray());
            Assert.Equal(new double[] { 0, 1 }, layout.YValues.ToArray());
            Assert.Equal(1.0, layout.CellStepX, 6);
            Assert.Equal(1.0, layout.CellStepY, 6);
        }

        [Fact]
        public void NumericAxes_ExpandsHalfCellSoEdgesAlignWithPlot()
        {
            var layout = Numeric().Instance.ComputeLayout();

            var leftMost = layout.Cells.Where(c => c.X == 0).OrderBy(c => c.Left).First();
            var rightMost = layout.Cells.Where(c => c.X == 2).First();

            Assert.Equal(layout.PlotLeft, leftMost.Left, 6);
            Assert.Equal(layout.PlotRight, rightMost.Left + rightMost.Width, 6);
        }

        [Fact]
        public void NumericAxes_YAxisPointsUp()
        {
            var layout = Numeric().Instance.ComputeLayout();

            var top = layout.Cells.First(c => c.Y == 1).Top;
            var bottom = layout.Cells.First(c => c.Y == 0).Top;

            Assert.True(top < bottom);
        }

        [Fact]
        public void NumericAxes_ExplicitCellSizeIsRespected()
        {
            var cut = Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, Grid())
                .Add(c => c.XProperty, nameof(Row.XPt))
                .Add(c => c.YProperty, nameof(Row.YPt))
                .Add(c => c.ValueProperty, nameof(Row.Val))
                .Add(c => c.NumericAxes, true)
                .Add(c => c.CellWidth, 0.5)
                .Add(c => c.CellHeight, 4.0));

            var layout = cut.Instance.ComputeLayout();
            Assert.Equal(0.5, layout.CellStepX, 6);
            Assert.Equal(4.0, layout.CellStepY, 6);
        }

        [Fact]
        public void InferStep_HandlesDegenerateInput()
        {
            Assert.Equal(1.0, ElHeatmapGrowEngine.InferStep(new double[] { 5 }), 6);
            Assert.Equal(1.0, ElHeatmapGrowEngine.InferStep(new double[0]), 6);
            Assert.Equal(1.0, ElHeatmapGrowEngine.InferStep(null), 6);
            Assert.Equal(2.5, ElHeatmapGrowEngine.InferStep(new double[] { 0, 2.5, 10 }), 6);
        }

        // ------------------------------------------------------------ 格内数值与阈值

        [Fact]
        public void ShowValues_DrawsValueTextWhenCellIsLargeEnough()
        {
            var cut = Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, Grid())
                .Add(c => c.XProperty, nameof(Row.XCat))
                .Add(c => c.YProperty, nameof(Row.YCat))
                .Add(c => c.ValueProperty, nameof(Row.Val))
                .Add(c => c.ShowValues, true));

            var layout = cut.Instance.ComputeLayout();
            Assert.All(layout.Cells, c => Assert.True(c.ShowValue));
            Assert.Contains(">10<", cut.Instance.GetSvg());
        }

        [Fact]
        public void ShowValues_SkippedWhenCellTooSmall()
        {
            var cut = Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, Grid())
                .Add(c => c.XProperty, nameof(Row.XCat))
                .Add(c => c.YProperty, nameof(Row.YCat))
                .Add(c => c.ValueProperty, nameof(Row.Val))
                .Add(c => c.Width, 60)
                .Add(c => c.ShowValues, true));

            var layout = cut.Instance.ComputeLayout();
            Assert.All(layout.Cells, c => Assert.False(c.ShowValue));
            Assert.DoesNotContain("el-heatmap-grow__value", cut.Instance.GetSvg());
        }

        [Fact]
        public void ShowValues_UsesLightTextOnDarkCells()
        {
            var cut = Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, Grid())
                .Add(c => c.XProperty, nameof(Row.XCat))
                .Add(c => c.YProperty, nameof(Row.YCat))
                .Add(c => c.ValueProperty, nameof(Row.Val))
                .Add(c => c.ShowValues, true));

            var layout = cut.Instance.ComputeLayout();
            Assert.False(layout.Cells.Single(c => c.Value == 10).LightText);
            Assert.True(layout.Cells.Single(c => c.Value == 60).LightText);
            Assert.Contains("fill=\"#ffffff\"", cut.Instance.GetSvg());
        }

        [Fact]
        public void FormatString_IsAppliedToValueText()
        {
            var cut = Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, Grid())
                .Add(c => c.XProperty, nameof(Row.XCat))
                .Add(c => c.YProperty, nameof(Row.YCat))
                .Add(c => c.ValueProperty, nameof(Row.Val))
                .Add(c => c.ShowValues, true)
                .Add(c => c.FormatString, "N1"));

            Assert.Contains("10.0", cut.Instance.GetSvg());

            var model = cut.Instance.BuildModel();
            Assert.Equal("10.00", ElHeatmapGrowColorScale.FormatValue(
                new ElHeatmapModel { FormatString = "{0:0.00}" }, 10));
            Assert.Equal("10.0", ElHeatmapGrowColorScale.FormatValue(model, 10));
        }

        // ------------------------------------------------------------ 色阶图例

        [Fact]
        public void GradientLegend_HasFiveStepsByDefault()
        {
            var layout = Category().Instance.ComputeLayout();

            Assert.Equal(ElHeatmapGrowEngine.GradientLegendSteps, layout.LegendItems.Count);
            Assert.Equal("#F0F0F0", layout.LegendItems[0].Color);
            Assert.Equal("#1A9641", layout.LegendItems[layout.LegendItems.Count - 1].Color);
            Assert.All(layout.LegendItems, i => Assert.False(string.IsNullOrEmpty(i.Label)));
        }

        [Fact]
        public void ColorRanges_LegendUsesRangeLabels()
        {
            var ranges = new List<ElHeatmapColorRange>
            {
                new ElHeatmapColorRange(double.NegativeInfinity, 20, "#00ff00"),
                new ElHeatmapColorRange(20, double.PositiveInfinity, "#ff0000"),
            };

            var cut = Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, Grid())
                .Add(c => c.XProperty, nameof(Row.XCat))
                .Add(c => c.YProperty, nameof(Row.YCat))
                .Add(c => c.ValueProperty, nameof(Row.Val))
                .Add(c => c.ColorRanges, ranges));

            var layout = cut.Instance.ComputeLayout();
            Assert.Equal(2, layout.LegendItems.Count);
            Assert.Equal(ElHeatmapGrowColorScale.FormatRangeLabel(ranges[0]), layout.LegendItems[0].Label);
            Assert.Contains("-∞", layout.LegendItems[0].Label);
            Assert.Contains("+∞", layout.LegendItems[1].Label);
            Assert.Equal("#00ff00", layout.LegendItems[0].Color);
        }

        [Fact]
        public void ShowColorScaleFalse_ProducesNoLegend()
        {
            var cut = Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, Grid())
                .Add(c => c.XProperty, nameof(Row.XCat))
                .Add(c => c.YProperty, nameof(Row.YCat))
                .Add(c => c.ValueProperty, nameof(Row.Val))
                .Add(c => c.ShowColorScale, false));

            Assert.Empty(cut.Instance.ComputeLayout().LegendItems);
            Assert.DoesNotContain("el-heatmap-grow__scale", cut.Instance.GetSvg());
        }

        // ------------------------------------------------------------ 渲染细节

        [Fact]
        public void Stroke_IsRenderedWhenConfigured()
        {
            var cut = Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, Grid())
                .Add(c => c.XProperty, nameof(Row.XCat))
                .Add(c => c.YProperty, nameof(Row.YCat))
                .Add(c => c.ValueProperty, nameof(Row.Val))
                .Add(c => c.Stroke, "#ffffff")
                .Add(c => c.StrokeWidth, 1));

            Assert.Contains("stroke=\"#ffffff\"", cut.Instance.GetSvg());
            Assert.Contains("stroke-width=\"1\"", cut.Instance.GetSvg());
        }

        [Fact]
        public void TitleAndAxisTitles_AreRendered()
        {
            var cut = Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, Grid())
                .Add(c => c.XProperty, nameof(Row.XCat))
                .Add(c => c.YProperty, nameof(Row.YCat))
                .Add(c => c.ValueProperty, nameof(Row.Val))
                .Add(c => c.Title, "负荷分布")
                .Add(c => c.XTitle, "时段")
                .Add(c => c.YTitle, "设备"));

            var svg = cut.Instance.GetSvg();
            Assert.Contains("负荷分布", svg);
            Assert.Contains("时段", svg);
            Assert.Contains("设备", svg);
            Assert.Contains("aria-label=\"负荷分布\"", svg);
        }

        [Fact]
        public void RenderSvg_ReturnsSvgElement()
        {
            var svg = Category().Instance.GetSvg();

            Assert.StartsWith("<svg", svg);
            Assert.EndsWith("</svg>", svg);
            Assert.Contains("viewBox=\"0 0 600 360\"", svg);
            Assert.Contains("el-heatmap-grow__svg", svg);
            Assert.Contains("el-heatmap-grow__cell", svg);
        }

        [Fact]
        public void Tooltip_CombinesLabelsAndValue()
        {
            var layout = Category().Instance.ComputeLayout();
            var cell = layout.Cells.Single(c => c.Value == 60);

            Assert.Equal("C, R2: 60", ElHeatmapGrowEngine.BuildTooltip(cell));
            Assert.Contains("<title>C, R2: 60</title>", Category().Instance.GetSvg());
        }

        [Fact]
        public void Escape_HandlesSpecialCharacters()
        {
            Assert.Equal("&lt;a&gt;&amp;b&quot;c&apos;", ElHeatmapGrowEngine.Escape("<a>&b\"c'"));
            Assert.Equal(string.Empty, ElHeatmapGrowEngine.Escape(null));
        }

        [Fact]
        public void ParseColor_SupportsShorthandAndFallback()
        {
            Assert.Equal((255, 0, 0), ElHeatmapGrowColorScale.Parse("#ff0000"));
            Assert.Equal((255, 0, 0), ElHeatmapGrowColorScale.Parse("ff0000"));
            Assert.Equal((170, 187, 204), ElHeatmapGrowColorScale.Parse("#abc"));
            Assert.Equal((240, 240, 240), ElHeatmapGrowColorScale.Parse("not-a-color"));
            Assert.Equal((240, 240, 240), ElHeatmapGrowColorScale.Parse(null));
        }

        [Fact]
        public void HideAxisLabels_RemovesLabelGroups()
        {
            var cut = Render<ElHeatmapGrow>(p => p
                .Add(c => c.Data, Grid())
                .Add(c => c.XProperty, nameof(Row.XCat))
                .Add(c => c.YProperty, nameof(Row.YCat))
                .Add(c => c.ValueProperty, nameof(Row.Val))
                .Add(c => c.ShowXLabels, false)
                .Add(c => c.ShowYLabels, false));

            var svg = cut.Instance.GetSvg();
            Assert.DoesNotContain("el-heatmap-grow__x-labels", svg);
            Assert.DoesNotContain("el-heatmap-grow__y-labels", svg);
            Assert.Equal(6, cut.Instance.ComputeLayout().Cells.Count);
        }
    }
}
