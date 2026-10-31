using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElSplitter + ElSplitterPanel 单元测试 — 对齐 Element Plus Splitter 官方 API
    /// </summary>
    public class ElSplitterTests : BunitContext
    {
        public ElSplitterTests() => Services.AddElementServices();

        // ==================== ElSplitter 基础 ====================

        [Fact]
        public void Default_RenderHorizontal()
        {
            var cut = Render<ElSplitter>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent(0, typeof(ElSplitterPanel));
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p1")));
                builder.CloseComponent();
            }));
            var el = cut.Find(".el-splitter");
            Assert.Contains("el-splitter", el.ClassList);
            Assert.Contains("el-splitter--horizontal", el.ClassList);
        }

        [Fact]
        public void Direction_Vertical()
        {
            var cut = Render<ElSplitter>(p => p
                .Add(x => x.Direction, SplitterDirection.Vertical)
                .AddChildContent(builder =>
                {
                    builder.OpenComponent(0, typeof(ElSplitterPanel));
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p")));
                    builder.CloseComponent();
                }));
            Assert.Contains("el-splitter--vertical", cut.Find(".el-splitter").ClassList);
        }

        [Fact]
        public void Lazy_AddsIsLazyClass()
        {
            var cut = Render<ElSplitter>(p => p
                .Add(x => x.Lazy, true)
                .AddChildContent(builder =>
                {
                    builder.OpenComponent(0, typeof(ElSplitterPanel));
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p")));
                    builder.CloseComponent();
                }));
            Assert.Contains("is-lazy", cut.Find(".el-splitter").ClassList);
        }

        [Fact]
        public void NotLazy_NoIsLazyClass()
        {
            var cut = Render<ElSplitter>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent(0, typeof(ElSplitterPanel));
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p")));
                builder.CloseComponent();
            }));
            Assert.DoesNotContain("is-lazy", cut.Find(".el-splitter").ClassList);
        }

        [Fact]
        public void Cls_Appended()
        {
            var cut = Render<ElSplitter>(p => p
                .Add(x => x.Cls, "my-splitter")
                .AddChildContent(builder =>
                {
                    builder.OpenComponent(0, typeof(ElSplitterPanel));
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p")));
                    builder.CloseComponent();
                }));
            Assert.Contains("my-splitter", cut.Find(".el-splitter").ClassList);
        }

        [Fact]
        public void Attributes_Passthrough()
        {
            var cut = Render<ElSplitter>(p => p
                .AddUnmatched("data-test", "splitter-1")
                .AddChildContent(builder =>
                {
                    builder.OpenComponent(0, typeof(ElSplitterPanel));
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p")));
                    builder.CloseComponent();
                }));
            Assert.Equal("splitter-1", cut.Find(".el-splitter").GetAttribute("data-test"));
        }

        // ==================== ElSplitterPanel 基础 ====================

        [Fact]
        public void Panel_DefaultResizableAddsBar()
        {
            var cut = Render<ElSplitter>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent(0, typeof(ElSplitterPanel));
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p1")));
                builder.CloseComponent();
                builder.OpenComponent(2, typeof(ElSplitterPanel));
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p2")));
                builder.CloseComponent();
            }));
            var bars = cut.FindAll(".el-splitter__bar");
            Assert.Single(bars); // 最后一个 bar 被 CSS 隐藏，但 DOM 中只有 1 个
        }

        [Fact]
        public void Panel_ResizableFalse_NoBar()
        {
            var cut = Render<ElSplitter>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent(0, typeof(ElSplitterPanel));
                builder.AddAttribute(1, "Resizable", false);
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p")));
                builder.CloseComponent();
            }));
            Assert.Empty(cut.FindAll(".el-splitter__bar"));
        }

        [Fact]
        public void Panel_Collapsible_AddsBarClass()
        {
            var cut = Render<ElSplitter>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent(0, typeof(ElSplitterPanel));
                builder.AddAttribute(1, "Collapsible", true);
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p")));
                builder.CloseComponent();
                builder.OpenComponent(3, typeof(ElSplitterPanel));
                builder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p")));
                builder.CloseComponent();
            }));
            var bar = cut.Find(".el-splitter__bar");
            Assert.Contains("is-collapsible", bar.ClassList);
        }

        [Fact]
        public void Panel_CollapsibleFalse_NoIsCollapsibleOnBar()
        {
            var cut = Render<ElSplitter>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent(0, typeof(ElSplitterPanel));
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p")));
                builder.CloseComponent();
                builder.OpenComponent(2, typeof(ElSplitterPanel));
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p")));
                builder.CloseComponent();
            }));
            Assert.DoesNotContain("is-collapsible", cut.Find(".el-splitter__bar").ClassList);
        }

        [Fact]
        public void Panel_Size_AppliedToStyle()
        {
            var cut = Render<ElSplitter>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent(0, typeof(ElSplitterPanel));
                builder.AddAttribute(1, "Size", "30%");
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p1")));
                builder.CloseComponent();
                builder.OpenComponent(3, typeof(ElSplitterPanel));
                builder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p2")));
                builder.CloseComponent();
            }));
            var panel = cut.FindAll(".el-splitter-panel")[0];
            Assert.Contains("30%", panel.GetAttribute("style"));
        }

        [Fact]
        public void Panel_MinMax_InStyle()
        {
            var cut = Render<ElSplitter>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent(0, typeof(ElSplitterPanel));
                builder.AddAttribute(1, "Min", "50px");
                builder.AddAttribute(2, "Max", "300px");
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p1")));
                builder.CloseComponent();
                builder.OpenComponent(4, typeof(ElSplitterPanel));
                builder.AddAttribute(5, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p2")));
                builder.CloseComponent();
            }));
            var panel = cut.FindAll(".el-splitter-panel")[0];
            var style = panel.GetAttribute("style");
            Assert.Contains("min-width:50px", style);
            Assert.Contains("max-width:300px", style);
        }

        [Fact]
        public void Panel_VerticalDirection_MinMaxHeight()
        {
            var cut = Render<ElSplitter>(p => p
                .Add(x => x.Direction, SplitterDirection.Vertical)
                .AddChildContent(builder =>
                {
                    builder.OpenComponent(0, typeof(ElSplitterPanel));
                    builder.AddAttribute(1, "Min", "50px");
                    builder.AddAttribute(2, "Max", "300px");
                    builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p1")));
                    builder.CloseComponent();
                    builder.OpenComponent(4, typeof(ElSplitterPanel));
                    builder.AddAttribute(5, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p2")));
                    builder.CloseComponent();
                }));
            var panel = cut.FindAll(".el-splitter-panel")[0];
            var style = panel.GetAttribute("style");
            Assert.Contains("min-height:50px", style);
            Assert.Contains("max-height:300px", style);
        }

        [Fact]
        public void Panel_AriaOrientation_Horizontal()
        {
            var cut = Render<ElSplitter>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent(0, typeof(ElSplitterPanel));
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p1")));
                builder.CloseComponent();
                builder.OpenComponent(2, typeof(ElSplitterPanel));
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p2")));
                builder.CloseComponent();
            }));
            Assert.Equal("vertical", cut.Find(".el-splitter__bar").GetAttribute("aria-orientation"));
        }

        [Fact]
        public void Panel_AriaOrientation_VerticalSplitter()
        {
            var cut = Render<ElSplitter>(p => p
                .Add(x => x.Direction, SplitterDirection.Vertical)
                .AddChildContent(builder =>
                {
                    builder.OpenComponent(0, typeof(ElSplitterPanel));
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p1")));
                    builder.CloseComponent();
                    builder.OpenComponent(2, typeof(ElSplitterPanel));
                    builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p2")));
                    builder.CloseComponent();
                }));
            Assert.Equal("horizontal", cut.Find(".el-splitter__bar").GetAttribute("aria-orientation"));
        }

        [Fact]
        public void Panel_ClsAppended()
        {
            var cut = Render<ElSplitter>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent(0, typeof(ElSplitterPanel));
                builder.AddAttribute(1, "Cls", "custom-panel");
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p")));
                builder.CloseComponent();
            }));
            Assert.Contains("custom-panel", cut.Find(".el-splitter-panel").ClassList);
        }

        // ==================== Bar 拖拽 ====================

        [Fact]
        public void Bar_DefaultIsVisibleOnlyWhenResizable()
        {
            var cut = Render<ElSplitter>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent(0, typeof(ElSplitterPanel));
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p1")));
                builder.CloseComponent();
                builder.OpenComponent(2, typeof(ElSplitterPanel));
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p2")));
                builder.CloseComponent();
            }));
            var bars = cut.FindAll(".el-splitter__bar");
            Assert.Equal(1, bars.Count);
            Assert.Contains("el-splitter__bar-dragger", bars[0].InnerHtml);
        }

        // ==================== Lazy 模式 ====================

        [Fact]
        public void Lazy_OnSplitter_AddsClass()
        {
            var cut = Render<ElSplitter>(p => p
                .Add(x => x.Lazy, true)
                .AddChildContent(builder =>
                {
                    builder.OpenComponent(0, typeof(ElSplitterPanel));
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p")));
                    builder.CloseComponent();
                }));
            Assert.Contains("is-lazy", cut.Find(".el-splitter").ClassList);
        }

        // ==================== 事件参数类验证 ====================

        [Fact]
        public void SplitterResizeEventArgs_Defaults()
        {
            var ea = new SplitterResizeEventArgs();
            Assert.Equal(0, ea.Index);
            Assert.NotNull(ea.Sizes);
            Assert.Empty(ea.Sizes);
        }

        [Fact]
        public void SplitterCollapseEventArgs_Defaults()
        {
            var ea = new SplitterCollapseEventArgs();
            Assert.Equal(0, ea.Index);
            Assert.False(ea.Collapsed);
        }

        // ==================== 多 Panel ====================

        [Fact]
        public void MultiplePanels_OnlyOneBarBetweenEach()
        {
            var cut = Render<ElSplitter>(p => p.AddChildContent(builder =>
            {
                for (int i = 0; i < 4; i++)
                {
                    builder.OpenComponent(i * 2, typeof(ElSplitterPanel));
                    builder.AddAttribute(i * 2 + 1, "ChildContent", (RenderFragment)(b => b.AddContent(0, $"p{i}")));
                    builder.CloseComponent();
                }
            }));
            var bars = cut.FindAll(".el-splitter__bar");
            Assert.Equal(3, bars.Count); // 4 panels → 3 bars
        }

        [Fact]
        public void LastBar_OnlyOneBetweenTwoPanels()
        {
            // 2 panels → 1 bar
            var cut = Render<ElSplitter>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent(0, typeof(ElSplitterPanel));
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p1")));
                builder.CloseComponent();
                builder.OpenComponent(2, typeof(ElSplitterPanel));
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p2")));
                builder.CloseComponent();
            }));
            Assert.Single(cut.FindAll(".el-splitter__bar"));
        }

        // ==================== 分隔条 DOM（对齐 Element Plus：bar 不占布局宽） ====================

        [Fact]
        public void Bar_HasDataIndexForJsDrag()
        {
            // 3 panels → 2 bars；JS 靠 data-index 把拖拽回传到对应面板。
            var cut = Render<ElSplitter>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent(0, typeof(ElSplitterPanel));
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p1")));
                builder.CloseComponent();
                builder.OpenComponent(2, typeof(ElSplitterPanel));
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p2")));
                builder.CloseComponent();
                builder.OpenComponent(4, typeof(ElSplitterPanel));
                builder.AddAttribute(5, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p3")));
                builder.CloseComponent();
            }));

            var bars = cut.FindAll(".el-splitter__bar");
            Assert.Equal(2, bars.Count);
            Assert.Equal("0", bars[0].GetAttribute("data-index"));
            Assert.Equal("1", bars[1].GetAttribute("data-index"));
        }

        [Fact]
        public void Bar_HasNoInlineWidth_DraggerRendersVisibleLine()
        {
            var cut = Render<ElSplitter>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent(0, typeof(ElSplitterPanel));
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p1")));
                builder.CloseComponent();
                builder.OpenComponent(2, typeof(ElSplitterPanel));
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p2")));
                builder.CloseComponent();
            }));

            var bar = cut.Find(".el-splitter__bar");
            var style = bar.GetAttribute("style");
            // 对齐官方：bar 自身不写宽高（.el-splitter__bar{flex:0 0 0}），可见的线是 dragger 的伪元素。
            Assert.DoesNotContain("width:", style ?? string.Empty);
            Assert.Single(cut.FindAll(".el-splitter__bar-dragger"));
        }

        [Fact]
        public void Dragger_GetsIsLazyWhenSplitterLazy()
        {
            var cut = Render<ElSplitter>(p => p
                .Add(x => x.Lazy, true)
                .AddChildContent(builder =>
                {
                    builder.OpenComponent(0, typeof(ElSplitterPanel));
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p1")));
                    builder.CloseComponent();
                    builder.OpenComponent(2, typeof(ElSplitterPanel));
                    builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p2")));
                    builder.CloseComponent();
                }));

            Assert.Contains("is-lazy", cut.Find(".el-splitter__bar-dragger").ClassName);
        }

        [Fact]
        public void LazyOffset_ExposedAsCssVariable()
        {
            var cut = Render<ElSplitter>(p => p
                .Add(x => x.Lazy, true)
                .AddChildContent(builder =>
                {
                    builder.OpenComponent(0, typeof(ElSplitterPanel));
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p1")));
                    builder.CloseComponent();
                    builder.OpenComponent(2, typeof(ElSplitterPanel));
                    builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "p2")));
                    builder.CloseComponent();
                }));

            Assert.Contains("--el-splitter-bar-offset", cut.Find(".el-splitter").GetAttribute("style"));
        }

        // ==================== SplitterDirection 枚举 ====================

        [Fact]
        public void SplitterDirection_Values()
        {
            Assert.Equal(0, (int)SplitterDirection.Horizontal);
            Assert.Equal(1, (int)SplitterDirection.Vertical);
        }
    }
}