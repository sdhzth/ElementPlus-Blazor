using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElScrollbar 单元测试 — 对齐 Element Plus Scrollbar 官方 API
    /// </summary>
    public class ElScrollbarTests : BunitContext
    {
        public ElScrollbarTests() => Services.AddElementServices();

        // ==================== 基础渲染 ====================

        [Fact]
        public void Default_RenderStructure()
        {
            var cut = Render<ElScrollbar>(p => p.AddChildContent("content"));
            // 外层 el-scrollbar
            var root = cut.Find(".el-scrollbar");
            Assert.Contains("el-scrollbar", root.ClassList);
            // wrap 层
            var wrap = cut.Find(".el-scrollbar__wrap");
            Assert.Contains("el-scrollbar__wrap", wrap.ClassList);
            Assert.Contains("el-scrollbar__wrap--hidden-default", wrap.ClassList);
            // view 层
            var view = cut.Find(".el-scrollbar__view");
            Assert.Contains("el-scrollbar__view", view.ClassList);
            Assert.Contains("content", view.TextContent);
            // 自定义滚动条（非 Native）
            Assert.NotEmpty(cut.FindAll(".el-scrollbar__bar.is-horizontal"));
            Assert.NotEmpty(cut.FindAll(".el-scrollbar__bar.is-vertical"));
        }

        [Fact]
        public void Default_ViewTagIsDiv()
        {
            var cut = Render<ElScrollbar>(p => p.AddChildContent("x"));
            Assert.Equal("div", cut.Find(".el-scrollbar__view").TagName.ToLower());
        }

        [Fact]
        public void CustomTag_Section()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.Tag, "section").AddChildContent("x"));
            Assert.Equal("section", cut.Find(".el-scrollbar__view").TagName.ToLower());
        }

        [Fact]
        public void CustomTag_Article()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.Tag, "article").AddChildContent("x"));
            Assert.Equal("article", cut.Find(".el-scrollbar__view").TagName.ToLower());
        }

        [Fact]
        public void CustomTag_EmptyFallbackToDiv()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.Tag, "").AddChildContent("x"));
            Assert.Equal("div", cut.Find(".el-scrollbar__view").TagName.ToLower());
        }

        // ==================== Height / MaxHeight ====================

        [Fact]
        public void Height_Applied()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.Height, "400px").AddChildContent("x"));
            var style = cut.Find(".el-scrollbar").GetAttribute("style");
            Assert.Contains("height:400px", style);
        }

        [Fact]
        public void Height_UnitlessNumber_AppendsPx()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.Height, "400").AddChildContent("x"));
            Assert.Contains("height:400px", cut.Find(".el-scrollbar").GetAttribute("style"));
        }

        [Fact]
        public void MaxHeight_Applied()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.MaxHeight, "500px").AddChildContent("x"));
            Assert.Contains("max-height:500px", cut.Find(".el-scrollbar").GetAttribute("style"));
        }

        [Fact]
        public void HeightAndMaxHeight_Combined()
        {
            var cut = Render<ElScrollbar>(p => p
                .Add(x => x.Height, "300px")
                .Add(x => x.MaxHeight, "500px")
                .AddChildContent("x"));
            var style = cut.Find(".el-scrollbar").GetAttribute("style");
            Assert.Contains("height:300px", style);
            Assert.Contains("max-height:500px", style);
        }

        [Fact]
        public void NoHeight_NoInlineStyle()
        {
            var cut = Render<ElScrollbar>(p => p.AddChildContent("x"));
            Assert.True(string.IsNullOrEmpty(cut.Find(".el-scrollbar").GetAttribute("style")) ||
                !cut.Find(".el-scrollbar").GetAttribute("style").Contains("height"));
        }

        // ==================== Always ====================

        [Fact]
        public void Always_AddsIsAlwaysClass()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.Always, true).AddChildContent("x"));
            Assert.Contains("is-always", cut.Find(".el-scrollbar").ClassList);
        }

        [Fact]
        public void AlwaysBar_VisibilityStyle()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.Always, true).AddChildContent("x"));
            var bar = cut.Find(".el-scrollbar__bar.is-horizontal");
            Assert.Contains("opacity:1", bar.GetAttribute("style"));
        }

        [Fact]
        public void NotAlwaysBar_NoOpacity1()
        {
            var cut = Render<ElScrollbar>(p => p.AddChildContent("x"));
            var bar = cut.Find(".el-scrollbar__bar.is-horizontal");
            Assert.Null(bar.GetAttribute("style"));
        }

        // ==================== Native ====================

        [Fact]
        public void Native_AddsIsNativeClass()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.Native, true).AddChildContent("x"));
            Assert.Contains("is-native", cut.Find(".el-scrollbar").ClassList);
        }

        [Fact]
        public void Native_NoCustomBarElements()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.Native, true).AddChildContent("x"));
            var bars = cut.FindAll(".el-scrollbar__bar");
            Assert.Empty(bars);
        }

        [Fact]
        public void Native_WrapNoHiddenDefault()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.Native, true).AddChildContent("x"));
            Assert.DoesNotContain("el-scrollbar__wrap--hidden-default", cut.Find(".el-scrollbar__wrap").ClassList);
        }

        // ==================== Noresize ====================

        [Fact]
        public void Noresize_NotAffectsClass()
        {
            // Noresize 只是 hint 参数，不影响渲染
            var cut = Render<ElScrollbar>(p => p.Add(x => x.Noresize, true).AddChildContent("x"));
            Assert.Contains("el-scrollbar", cut.Find(".el-scrollbar").ClassList);
        }

        // ==================== MinSize ====================

        [Fact]
        public void MinSize_ThumbWidthStyle()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.MinSize, 30).AddChildContent("x"));
            var bar = cut.Find(".el-scrollbar__bar.is-horizontal .el-scrollbar__thumb");
            Assert.Contains("width:30", bar.GetAttribute("style"));
        }

        [Fact]
        public void MinSizeZero_UsesDefault20()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.MinSize, 0).AddChildContent("x"));
            var bar = cut.Find(".el-scrollbar__bar.is-horizontal .el-scrollbar__thumb");
            Assert.Contains("width:20", bar.GetAttribute("style"));
        }

        [Fact]
        public void MinSizeNegative_UsesDefault20()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.MinSize, -5).AddChildContent("x"));
            var bar = cut.Find(".el-scrollbar__bar.is-horizontal .el-scrollbar__thumb");
            Assert.Contains("width:20", bar.GetAttribute("style"));
        }

        // ==================== WrapClassName / ViewClassName ====================

        [Fact]
        public void WrapClassName_Appended()
        {
            var cut = Render<ElScrollbar>(p => p
                .Add(x => x.WrapClassName, "my-wrap")
                .AddChildContent("x"));
            Assert.Contains("my-wrap", cut.Find(".el-scrollbar__wrap").ClassList);
        }

        [Fact]
        public void ViewClassName_Appended()
        {
            var cut = Render<ElScrollbar>(p => p
                .Add(x => x.ViewClassName, "my-view")
                .AddChildContent("x"));
            Assert.Contains("my-view", cut.Find(".el-scrollbar__view").ClassList);
        }

        // ==================== a11y（Id / Role / AriaLabel / Tabindex）====================

        [Fact]
        public void Id_OnView()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.Id, "main-scroll").AddChildContent("x"));
            Assert.Equal("main-scroll", cut.Find(".el-scrollbar__view").GetAttribute("id"));
        }

        [Fact]
        public void Role_OnRoot()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.Role, "region").AddChildContent("x"));
            Assert.Equal("region", cut.Find(".el-scrollbar").GetAttribute("role"));
        }

        [Fact]
        public void AriaLabel_OnView()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.AriaLabel, "内容列表").AddChildContent("x"));
            Assert.Equal("内容列表", cut.Find(".el-scrollbar__view").GetAttribute("aria-label"));
        }

        [Fact]
        public void AriaOrientation_OnView()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.AriaOrientation, "vertical").AddChildContent("x"));
            Assert.Equal("vertical", cut.Find(".el-scrollbar__view").GetAttribute("aria-orientation"));
        }

        [Fact]
        public void Tabindex_OnRoot()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.Tabindex, 0).AddChildContent("x"));
            Assert.Equal("0", cut.Find(".el-scrollbar").GetAttribute("tabindex"));
        }

        // ==================== Distance ====================

        [Fact]
        public void Distance_DefaultZero()
        {
            // Distance 是纯逻辑参数，不影响 CSS
            var cut = Render<ElScrollbar>(p => p.AddChildContent("x"));
            Assert.Contains("el-scrollbar", cut.Find(".el-scrollbar").ClassList);
        }

        // ==================== Cls / Style / Attributes ====================

        [Fact]
        public void Cls_Appended()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.Cls, "my-scrollbar").AddChildContent("x"));
            Assert.Contains("my-scrollbar", cut.Find(".el-scrollbar").ClassList);
        }

        [Fact]
        public void Style_Appended()
        {
            var cut = Render<ElScrollbar>(p => p.Add(x => x.Style, "margin:10px").AddChildContent("x"));
            Assert.Contains("margin:10px", cut.Find(".el-scrollbar").GetAttribute("style"));
        }

        [Fact]
        public void Attributes_Passthrough()
        {
            var cut = Render<ElScrollbar>(p => p
                .AddChildContent("x")
                .AddUnmatched("data-testid", "main-scrollbar"));
            Assert.Equal("main-scrollbar", cut.Find(".el-scrollbar").GetAttribute("data-testid"));
        }

        // ==================== 端到端 ====================

        [Fact]
        public void FullFeature_HeightAlwaysCustomTag()
        {
            var cut = Render<ElScrollbar>(p => p
                .Add(x => x.Height, "400px")
                .Add(x => x.Always, true)
                .Add(x => x.Tag, "ul")
                .Add(x => x.WrapClassName, "wrap-inner")
                .Add(x => x.ViewClassName, "view-inner")
                .Add(x => x.AriaLabel, "列表")
                .AddChildContent("<li>item 1</li><li>item 2</li>"));

            var root = cut.Find(".el-scrollbar");
            Assert.Contains("el-scrollbar", root.ClassList);
            Assert.Contains("is-always", root.ClassList);
            Assert.Contains("height:400px", root.GetAttribute("style"));

            var wrap = cut.Find(".el-scrollbar__wrap");
            Assert.Contains("wrap-inner", wrap.ClassList);

            var view = cut.Find(".el-scrollbar__view");
            Assert.Equal("ul", view.TagName.ToLower());
            Assert.Contains("view-inner", view.ClassList);
            Assert.Equal("列表", view.GetAttribute("aria-label"));
        }

        // ==================== 枚举值验证 ====================

        [Fact]
        public void ScrollbarDirection_AllValues()
        {
            Assert.Equal(0, (int)ScrollbarDirection.Top);
            Assert.Equal(1, (int)ScrollbarDirection.Bottom);
            Assert.Equal(2, (int)ScrollbarDirection.Left);
            Assert.Equal(3, (int)ScrollbarDirection.Right);
        }
    }
}