using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElRow + ElCol 单元测试 — 对齐 Element Plus Layout 官方 API
    /// </summary>
    public class ElRowColTests : BunitContext
    {
        public ElRowColTests() => Services.AddElementServices();

        // ==================== ElRow 基本渲染 ====================

        [Fact]
        public void Row_DefaultTag_IsDiv()
        {
            var cut = Render<ElRow>(p => p.AddChildContent("content"));
            var row = cut.Find(".el-row");
            Assert.Equal("div", row.TagName.ToLower());
        }

        [Fact]
        public void Row_CustomTag_IsSpan()
        {
            var cut = Render<ElRow>(p => p.Add(x => x.Tag, "span").AddChildContent("content"));
            var row = cut.Find(".el-row");
            Assert.Equal("span", row.TagName.ToLower());
        }

        [Fact]
        public void Row_EmptyTag_FallsBackToDiv()
        {
            var cut = Render<ElRow>(p => p.Add(x => x.Tag, "").AddChildContent("content"));
            Assert.Equal("div", cut.Find(".el-row").TagName.ToLower());
        }

        [Fact]
        public void Row_Cls_Appended()
        {
            var cut = Render<ElRow>(p => p.Add(x => x.Cls, "my-row").AddChildContent("x"));
            Assert.Contains("my-row", cut.Find(".el-row").ClassList);
        }

        // ==================== ElRow Justify ====================

        [Fact]
        public void Row_Justify_DefaultStart_NoClass()
        {
            var cut = Render<ElRow>(p => p.AddChildContent("x"));
            Assert.DoesNotContain("is-justify-start", cut.Find(".el-row").ClassList);
        }

        [Fact]
        public void Row_Justify_Center()
        {
            var cut = Render<ElRow>(p => p.Add(x => x.Justify, RowJustify.Center).AddChildContent("x"));
            Assert.Contains("is-justify-center", cut.Find(".el-row").ClassList);
        }

        [Fact]
        public void Row_Justify_End()
        {
            var cut = Render<ElRow>(p => p.Add(x => x.Justify, RowJustify.End).AddChildContent("x"));
            Assert.Contains("is-justify-end", cut.Find(".el-row").ClassList);
        }

        [Fact]
        public void Row_Justify_SpaceBetween()
        {
            var cut = Render<ElRow>(p => p.Add(x => x.Justify, RowJustify.SpaceBetween).AddChildContent("x"));
            Assert.Contains("is-justify-space-between", cut.Find(".el-row").ClassList);
        }

        [Fact]
        public void Row_Justify_SpaceAround()
        {
            var cut = Render<ElRow>(p => p.Add(x => x.Justify, RowJustify.SpaceAround).AddChildContent("x"));
            Assert.Contains("is-justify-space-around", cut.Find(".el-row").ClassList);
        }

        [Fact]
        public void Row_Justify_SpaceEvenly()
        {
            var cut = Render<ElRow>(p => p.Add(x => x.Justify, RowJustify.SpaceEvenly).AddChildContent("x"));
            Assert.Contains("is-justify-space-evenly", cut.Find(".el-row").ClassList);
        }

        // ==================== ElRow Align ====================

        [Fact]
        public void Row_Align_DefaultTop_NoClass()
        {
            var cut = Render<ElRow>(p => p.AddChildContent("x"));
            Assert.DoesNotContain("is-align-top", cut.Find(".el-row").ClassList);
        }

        [Fact]
        public void Row_Align_Middle()
        {
            var cut = Render<ElRow>(p => p.Add(x => x.Align, RowAlign.Middle).AddChildContent("x"));
            Assert.Contains("is-align-middle", cut.Find(".el-row").ClassList);
        }

        [Fact]
        public void Row_Align_Bottom()
        {
            var cut = Render<ElRow>(p => p.Add(x => x.Align, RowAlign.Bottom).AddChildContent("x"));
            Assert.Contains("is-align-bottom", cut.Find(".el-row").ClassList);
        }

        // ==================== ElRow Gutter ====================

        [Fact]
        public void Row_Gutter_NoGutter_NoNegativeMargin()
        {
            var cut = Render<ElRow>(p => p.AddChildContent("x"));
            var row = cut.Find(".el-row");
            var style = row.GetAttribute("style") ?? "";
            Assert.DoesNotContain("margin-left:-", style);
            Assert.DoesNotContain("margin-right:-", style);
        }

        [Fact]
        public void Row_Gutter_Gutter20_NegativeMargin()
        {
            var cut = Render<ElRow>(p => p.Add(x => x.Gutter, 20).AddChildContent("x"));
            var row = cut.Find(".el-row");
            var style = row.GetAttribute("style");
            Assert.Contains("margin-left:-10px", style);
            Assert.Contains("margin-right:-10px", style);
        }

        [Fact]
        public void Row_Gutter_Gutter10_NegativeMargin()
        {
            var cut = Render<ElRow>(p => p.Add(x => x.Gutter, 10).AddChildContent("x"));
            var row = cut.Find(".el-row");
            var style = row.GetAttribute("style");
            Assert.Contains("margin-left:-5px", style);
            Assert.Contains("margin-right:-5px", style);
        }

        [Fact]
        public void Row_Gutter_NoUnusedCssVar()
        {
            // 不应有 --el-row-gutter（已清理）
            var cut = Render<ElRow>(p => p.Add(x => x.Gutter, 20).AddChildContent("x"));
            Assert.DoesNotContain("--el-row-gutter", cut.Find(".el-row").GetAttribute("style"));
        }

        // ==================== ElCol 基本 ====================

        [Fact]
        public void Col_DefaultSpan_24()
        {
            var cut = Render<ElRow>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.ChildContent), (RenderFragment)(c => c.AddContent(0, "c")));
                builder.CloseComponent();
            }));

            var col = cut.Find(".el-col");
            Assert.Contains("el-col-24", col.ClassList);
        }

        [Fact]
        public void Col_DefaultTag_IsDiv()
        {
            var cut = Render<ElRow>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.ChildContent), (RenderFragment)(c => c.AddContent(0, "c")));
                builder.CloseComponent();
            }));

            Assert.Equal("div", cut.Find(".el-col").TagName.ToLower());
        }

        [Fact]
        public void Col_CustomTag_IsSection()
        {
            var cut = Render<ElRow>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.Tag), "section");
                builder.AddAttribute(2, nameof(ElCol.ChildContent), (RenderFragment)(c => c.AddContent(0, "c")));
                builder.CloseComponent();
            }));

            Assert.Equal("section", cut.Find(".el-col").TagName.ToLower());
        }

        [Fact]
        public void Col_Span_12()
        {
            var cut = RenderCol(12);
            Assert.Contains("el-col-12", cut.Find(".el-col").ClassList);
        }

        [Fact]
        public void Col_Span_OutOfRange_NormalizeTo24()
        {
            // Span > 24 → 24
            var cut = RenderCol(100);
            Assert.Contains("el-col-24", cut.Find(".el-col").ClassList);
        }

        [Fact]
        public void Col_Span_Negative_NormalizeTo0()
        {
            var cut = RenderCol(-5);
            Assert.Contains("el-col-0", cut.Find(".el-col").ClassList);
        }

        [Fact]
        public void Col_Cls_Appended()
        {
            var cut = Render<ElRow>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.Span), 6);
                builder.AddAttribute(2, nameof(ElCol.Cls), "special-col");
                builder.AddAttribute(3, nameof(ElCol.ChildContent), (RenderFragment)(c => c.AddContent(0, "x")));
                builder.CloseComponent();
            }));

            Assert.Contains("special-col", cut.Find(".el-col").ClassList);
        }

        private IRenderedComponent<ElRow> RenderCol(int span)
        {
            return Render<ElRow>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.Span), span);
                builder.AddAttribute(2, nameof(ElCol.ChildContent), (RenderFragment)(c => c.AddContent(0, "x")));
                builder.CloseComponent();
            }));
        }

        // ==================== ElCol Offset / Push / Pull ====================

        [Fact]
        public void Col_Offset()
        {
            var cut = Render<ElRow>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.Span), 12);
                builder.AddAttribute(2, nameof(ElCol.Offset), 6);
                builder.CloseComponent();
            }));

            var col = cut.Find(".el-col");
            Assert.Contains("el-col-offset-6", col.ClassList);
            Assert.Contains("el-col-12", col.ClassList);
        }

        [Fact]
        public void Col_Push()
        {
            var cut = Render<ElRow>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.Span), 6);
                builder.AddAttribute(2, nameof(ElCol.Push), 3);
                builder.CloseComponent();
            }));

            Assert.Contains("el-col-push-3", cut.Find(".el-col").ClassList);
        }

        [Fact]
        public void Col_Pull()
        {
            var cut = Render<ElRow>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.Span), 6);
                builder.AddAttribute(2, nameof(ElCol.Pull), 3);
                builder.CloseComponent();
            }));

            Assert.Contains("el-col-pull-3", cut.Find(".el-col").ClassList);
        }

        [Fact]
        public void Col_Offset_Zero_NoClass()
        {
            var cut = Render<ElRow>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.Span), 12);
                builder.CloseComponent();
            }));

            Assert.DoesNotContain("el-col-offset-0", cut.Find(".el-col").ClassList);
        }

        // ==================== ElCol 响应式 ====================

        [Fact]
        public void Col_Responsive_Xs()
        {
            var cut = Render<ElRow>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.Xs), 8);
                builder.CloseComponent();
            }));

            Assert.Contains("el-col-xs-8", cut.Find(".el-col").ClassList);
        }

        [Fact]
        public void Col_Responsive_Sm()
        {
            var cut = Render<ElRow>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.Sm), 6);
                builder.CloseComponent();
            }));

            Assert.Contains("el-col-sm-6", cut.Find(".el-col").ClassList);
        }

        [Fact]
        public void Col_Responsive_Md()
        {
            var cut = Render<ElRow>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.Md), 4);
                builder.CloseComponent();
            }));

            Assert.Contains("el-col-md-4", cut.Find(".el-col").ClassList);
        }

        [Fact]
        public void Col_Responsive_Lg()
        {
            var cut = Render<ElRow>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.Lg), 3);
                builder.CloseComponent();
            }));

            Assert.Contains("el-col-lg-3", cut.Find(".el-col").ClassList);
        }

        [Fact]
        public void Col_Responsive_Xl()
        {
            var cut = Render<ElRow>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.Xl), 2);
                builder.CloseComponent();
            }));

            Assert.Contains("el-col-xl-2", cut.Find(".el-col").ClassList);
        }

        [Fact]
        public void Col_Responsive_AllBreakpoints()
        {
            var cut = Render<ElRow>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.Xs), 8);
                builder.AddAttribute(2, nameof(ElCol.Sm), 6);
                builder.AddAttribute(3, nameof(ElCol.Md), 4);
                builder.AddAttribute(4, nameof(ElCol.Lg), 3);
                builder.AddAttribute(5, nameof(ElCol.Xl), 1);
                builder.CloseComponent();
            }));

            var col = cut.Find(".el-col");
            Assert.Contains("el-col-xs-8", col.ClassList);
            Assert.Contains("el-col-sm-6", col.ClassList);
            Assert.Contains("el-col-md-4", col.ClassList);
            Assert.Contains("el-col-lg-3", col.ClassList);
            Assert.Contains("el-col-xl-1", col.ClassList);
        }

        // ==================== Gutter 传递到 Col ====================

        [Fact]
        public void Col_InsideGutterRow_HasPadding()
        {
            var cut = Render<ElRow>(p => p.Add(x => x.Gutter, 20).AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.Span), 12);
                builder.CloseComponent();
            }));

            var colStyle = cut.Find(".el-col").GetAttribute("style");
            Assert.Contains("padding-left:10px", colStyle);
            Assert.Contains("padding-right:10px", colStyle);
        }

        [Fact]
        public void Col_InsideNoGutterRow_NoPadding()
        {
            var cut = Render<ElRow>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.Span), 12);
                builder.CloseComponent();
            }));

            var colStyle = cut.Find(".el-col").GetAttribute("style");
            Assert.True(string.IsNullOrEmpty(colStyle));
        }

        // ==================== 混合布局（官方示例）====================

        [Fact]
        public void ClassicAdminLayout_16plus8()
        {
            var cut = Render<ElRow>(p => p.Add(x => x.Gutter, 20).AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.Span), 16);
                builder.CloseComponent();
                builder.OpenComponent<ElCol>(2);
                builder.AddAttribute(3, nameof(ElCol.Span), 8);
                builder.CloseComponent();
            }));

            var cols = cut.FindAll(".el-col");
            Assert.Equal(2, cols.Count);
            Assert.Contains("el-col-16", cols[0].ClassList);
            Assert.Contains("el-col-8", cols[1].ClassList);
            Assert.Contains("padding-left:10px", cols[0].GetAttribute("style"));
        }

        [Fact]
        public void ResponsiveLayout_FullChain()
        {
            var cut = Render<ElRow>(p => p.Add(x => x.Gutter, 10).AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.Xs), 8);
                builder.AddAttribute(2, nameof(ElCol.Sm), 6);
                builder.AddAttribute(3, nameof(ElCol.Md), 4);
                builder.AddAttribute(4, nameof(ElCol.Lg), 3);
                builder.AddAttribute(5, nameof(ElCol.Xl), 1);
                builder.CloseComponent();
                builder.OpenComponent<ElCol>(6);
                builder.AddAttribute(7, nameof(ElCol.Xs), 4);
                builder.AddAttribute(8, nameof(ElCol.Sm), 6);
                builder.AddAttribute(9, nameof(ElCol.Md), 8);
                builder.AddAttribute(10, nameof(ElCol.Lg), 9);
                builder.AddAttribute(11, nameof(ElCol.Xl), 11);
                builder.CloseComponent();
            }));

            var cols = cut.FindAll(".el-col");
            Assert.Equal(2, cols.Count);
            Assert.Contains("el-col-xs-8", cols[0].ClassList);
            Assert.Contains("el-col-xs-4", cols[1].ClassList);
        }

        // ==================== Attributes 透传 ====================

        [Fact]
        public void Row_Attributes_Passthrough()
        {
            var cut = Render<ElRow>(p => p
                .AddChildContent("x")
                .AddUnmatched("data-testid", "my-row"));

            Assert.Equal("my-row", cut.Find(".el-row").GetAttribute("data-testid"));
        }

        [Fact]
        public void Col_Attributes_Passthrough()
        {
            var cut = Render<ElRow>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElCol>(0);
                builder.AddAttribute(1, nameof(ElCol.Span), 6);
                builder.AddAttribute(2, "data-col-id", "main-col");
                builder.CloseComponent();
            }));

            Assert.Equal("main-col", cut.Find(".el-col").GetAttribute("data-col-id"));
        }
    }
}