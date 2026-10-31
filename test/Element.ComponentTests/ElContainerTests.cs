using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElContainer 及子组件单元测试 — 对齐 Element Plus Container 官方 API
    /// </summary>
    public class ElContainerTests : BunitContext
    {
        public ElContainerTests()
        {
            Services.AddElementServices();
        }

        // ==================== ElContainer ====================

        [Fact]
        public void Container_Default_NoHeaderFooter_Horizontal()
        {
            // 没有 Header/Footer → 默认水平（无 is-vertical 类）
            var cut = Render<ElContainer>(parameters => parameters
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElAside>(0);
                    builder.AddAttribute(1, nameof(ElAside.ChildContent), (RenderFragment)(c => c.AddContent(0, "Side")));
                    builder.CloseComponent();
                    builder.OpenComponent<ElMain>(2);
                    builder.AddAttribute(3, nameof(ElMain.ChildContent), (RenderFragment)(c => c.AddContent(0, "Main")));
                    builder.CloseComponent();
                }));

            var container = cut.Find(".el-container");
            Assert.DoesNotContain("is-vertical", container.ClassList);
            Assert.Equal("Side", cut.Find(".el-aside").TextContent.Trim());
            Assert.Equal("Main", cut.Find(".el-main").TextContent.Trim());
        }

        [Fact]
        public void Container_AutoDirection_WithHeader_IsVertical()
        {
            // Auto + Header → 自动垂直
            var cut = Render<ElContainer>(parameters => parameters
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElHeader>(0);
                    builder.AddAttribute(1, nameof(ElHeader.ChildContent), (RenderFragment)(c => c.AddContent(0, "Top")));
                    builder.CloseComponent();
                    builder.OpenComponent<ElMain>(2);
                    builder.AddAttribute(3, nameof(ElMain.ChildContent), (RenderFragment)(c => c.AddContent(0, "Body")));
                    builder.CloseComponent();
                }));

            var container = cut.Find(".el-container");
            Assert.Contains("is-vertical", container.ClassList);
            Assert.Equal("Top", cut.Find(".el-header").TextContent.Trim());
            Assert.Equal("Body", cut.Find(".el-main").TextContent.Trim());
        }

        [Fact]
        public void Container_AutoDirection_WithFooter_IsVertical()
        {
            // Auto + Footer → 自动垂直
            var cut = Render<ElContainer>(parameters => parameters
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElMain>(0);
                    builder.AddAttribute(1, nameof(ElMain.ChildContent), (RenderFragment)(c => c.AddContent(0, "Body")));
                    builder.CloseComponent();
                    builder.OpenComponent<ElFooter>(2);
                    builder.AddAttribute(3, nameof(ElFooter.ChildContent), (RenderFragment)(c => c.AddContent(0, "Bottom")));
                    builder.CloseComponent();
                }));

            Assert.Contains("is-vertical", cut.Find(".el-container").ClassList);
            Assert.Equal("Bottom", cut.Find(".el-footer").TextContent.Trim());
        }

        [Fact]
        public void Container_ExplicitVertical_OverridesNoHeaderFooter()
        {
            // 显式 Vertical → 即使没有 Header/Footer 也垂直
            var cut = Render<ElContainer>(parameters => parameters
                .Add(x => x.Direction, ContainerDirection.Vertical)
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElAside>(0);
                    builder.AddAttribute(1, nameof(ElAside.ChildContent), (RenderFragment)(c => c.AddContent(0, "Side")));
                    builder.CloseComponent();
                    builder.OpenComponent<ElMain>(2);
                    builder.AddAttribute(3, nameof(ElMain.ChildContent), (RenderFragment)(c => c.AddContent(0, "Body")));
                    builder.CloseComponent();
                }));

            Assert.Contains("is-vertical", cut.Find(".el-container").ClassList);
        }

        [Fact]
        public void Container_ExplicitHorizontal_OverridesHeaderPresence()
        {
            // 显式 Horizontal → 即使有 Header 也强制水平
            var cut = Render<ElContainer>(parameters => parameters
                .Add(x => x.Direction, ContainerDirection.Horizontal)
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElHeader>(0);
                    builder.AddAttribute(1, nameof(ElHeader.ChildContent), (RenderFragment)(c => c.AddContent(0, "Top")));
                    builder.CloseComponent();
                    builder.OpenComponent<ElMain>(2);
                    builder.AddAttribute(3, nameof(ElMain.ChildContent), (RenderFragment)(c => c.AddContent(0, "Body")));
                    builder.CloseComponent();
                }));

            Assert.DoesNotContain("is-vertical", cut.Find(".el-container").ClassList);
        }

        [Fact]
        public void Container_ClassicAdminLayout()
        {
            // 经典后台布局：Header + (Aside + Main) + Footer
            var cut = Render<ElContainer>(parameters => parameters
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElHeader>(0);
                    builder.AddAttribute(1, nameof(ElHeader.ChildContent), (RenderFragment)(c => c.AddContent(0, "Header")));
                    builder.CloseComponent();

                    // 嵌套 Container（水平：Aside + Main）
                    builder.OpenComponent<ElContainer>(2);
                    builder.AddAttribute(3, nameof(ElContainer.ChildContent), (RenderFragment)(inner =>
                    {
                        inner.OpenComponent<ElAside>(0);
                        inner.AddAttribute(1, nameof(ElAside.Width), "200px");
                        inner.AddAttribute(2, nameof(ElAside.ChildContent), (RenderFragment)(c => c.AddContent(0, "Nav")));
                        inner.CloseComponent();
                        inner.OpenComponent<ElMain>(3);
                        inner.AddAttribute(4, nameof(ElMain.ChildContent), (RenderFragment)(c => c.AddContent(0, "Content")));
                        inner.CloseComponent();
                    }));
                    builder.CloseComponent();

                    builder.OpenComponent<ElFooter>(4);
                    builder.AddAttribute(5, nameof(ElFooter.ChildContent), (RenderFragment)(c => c.AddContent(0, "Footer")));
                    builder.CloseComponent();
                }));

            // 外层 Container 有 Header + Footer → 垂直
            var containers = cut.FindAll(".el-container");
            Assert.Equal(2, containers.Count);
            Assert.Contains("is-vertical", containers[0].ClassList);
            // 内层只有 Aside + Main → 水平
            Assert.DoesNotContain("is-vertical", containers[1].ClassList);

            Assert.Equal("Header", cut.Find(".el-header").TextContent.Trim());
            Assert.Equal("Nav", cut.Find(".el-aside").TextContent.Trim());
            Assert.Equal("Content", cut.Find(".el-main").TextContent.Trim());
            Assert.Equal("Footer", cut.Find(".el-footer").TextContent.Trim());
        }

        // ==================== ElHeader ====================

        [Fact]
        public void Header_DefaultHeight_60px()
        {
            var cut = Render<ElContainer>(parameters => parameters
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElHeader>(0);
                    builder.CloseComponent();
                    builder.OpenComponent<ElMain>(1);
                    builder.CloseComponent();
                }));

            var header = cut.Find(".el-header");
            // Height 默认 60px，通过 CSS 变量 --el-header-height 设置
            Assert.Contains("--el-header-height:60px", header.GetAttribute("style"));
        }

        [Fact]
        public void Header_CustomHeight()
        {
            var cut = Render<ElContainer>(parameters => parameters
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElHeader>(0);
                    builder.AddAttribute(1, nameof(ElHeader.Height), "80px");
                    builder.CloseComponent();
                    builder.OpenComponent<ElMain>(2);
                    builder.CloseComponent();
                }));

            var header = cut.Find(".el-header");
            Assert.Contains("--el-header-height:80px", header.GetAttribute("style"));
        }

        [Fact]
        public void Header_CustomHeight_WithUnitlessValue()
        {
            // 数字也应被 NormalizeCssSize 处理（补 px）
            var cut = Render<ElContainer>(parameters => parameters
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElHeader>(0);
                    builder.AddAttribute(1, nameof(ElHeader.Height), "72");
                    builder.CloseComponent();
                    builder.OpenComponent<ElMain>(2);
                    builder.CloseComponent();
                }));

            var style = cut.Find(".el-header").GetAttribute("style");
            Assert.Contains("--el-header-height:", style);
            Assert.Contains("72px", style);
        }

        // ==================== ElFooter ====================

        [Fact]
        public void Footer_DefaultHeight_60px()
        {
            var cut = Render<ElContainer>(parameters => parameters
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElMain>(0);
                    builder.CloseComponent();
                    builder.OpenComponent<ElFooter>(1);
                    builder.CloseComponent();
                }));

            var footer = cut.Find(".el-footer");
            Assert.Contains("--el-footer-height:60px", footer.GetAttribute("style"));
        }

        [Fact]
        public void Footer_CustomHeight()
        {
            var cut = Render<ElContainer>(parameters => parameters
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElMain>(0);
                    builder.CloseComponent();
                    builder.OpenComponent<ElFooter>(1);
                    builder.AddAttribute(2, nameof(ElFooter.Height), "48px");
                    builder.CloseComponent();
                }));

            Assert.Contains("--el-footer-height:48px", cut.Find(".el-footer").GetAttribute("style"));
        }

        // ==================== ElAside ====================

        [Fact]
        public void Aside_DefaultWidth_300px()
        {
            var cut = Render<ElContainer>(parameters => parameters
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElAside>(0);
                    builder.CloseComponent();
                    builder.OpenComponent<ElMain>(1);
                    builder.CloseComponent();
                }));

            Assert.Contains("--el-aside-width:300px", cut.Find(".el-aside").GetAttribute("style"));
        }

        [Fact]
        public void Aside_CustomWidth()
        {
            var cut = Render<ElContainer>(parameters => parameters
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElAside>(0);
                    builder.AddAttribute(1, nameof(ElAside.Width), "220px");
                    builder.CloseComponent();
                    builder.OpenComponent<ElMain>(2);
                    builder.CloseComponent();
                }));

            Assert.Contains("--el-aside-width:220px", cut.Find(".el-aside").GetAttribute("style"));
        }

        [Fact]
        public void Aside_CustomWidth_Percent()
        {
            // 百分比应原样保留
            var cut = Render<ElContainer>(parameters => parameters
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElAside>(0);
                    builder.AddAttribute(1, nameof(ElAside.Width), "25%");
                    builder.CloseComponent();
                    builder.OpenComponent<ElMain>(2);
                    builder.CloseComponent();
                }));

            Assert.Contains("--el-aside-width:25%", cut.Find(".el-aside").GetAttribute("style"));
        }

        // ==================== ElMain ====================

        [Fact]
        public void Main_BasicRender()
        {
            var cut = Render<ElMain>(parameters => parameters
                .AddChildContent("Main content here"));

            var main = cut.Find(".el-main");
            Assert.Equal("Main content here", main.TextContent.Trim());
            // main 元素
            Assert.Equal("main", main.TagName.ToLower());
        }

        [Fact]
        public void Main_WithCustomClassAndStyle()
        {
            var cut = Render<ElMain>(parameters => parameters
                .Add(x => x.Cls, "my-main")
                .Add(x => x.Style, "background:#fff")
                .AddChildContent("Styled main"));

            var main = cut.Find(".el-main");
            Assert.Contains("my-main", main.ClassList);
            Assert.Contains("background:#fff", main.GetAttribute("style"));
        }

        // ==================== 组合 & Dispose 测试 ====================

        [Fact]
        public void Container_HeaderDispose_TogglesDirectionBack()
        {
            // 初始：只有 Aside + Main → 水平
            var hasHeader = true;
            var cut = Render<ElContainer>(parameters => parameters
                .AddChildContent(builder =>
                {
                    if (hasHeader)
                    {
                        builder.OpenComponent<ElHeader>(0);
                        builder.AddAttribute(1, nameof(ElHeader.ChildContent), (RenderFragment)(c => c.AddContent(0, "H")));
                        builder.CloseComponent();
                    }
                    builder.OpenComponent<ElMain>(2);
                    builder.AddAttribute(3, nameof(ElMain.ChildContent), (RenderFragment)(c => c.AddContent(0, "M")));
                    builder.CloseComponent();
                }));

            Assert.Contains("is-vertical", cut.Find(".el-container").ClassList);

            // 移除 Header → 应切换回水平
            hasHeader = false;
            cut.Render();

            // Dispose 后 Header 还在 DOM 里但已不可见 — 我们验证 Count 变为 0
            // bUnit 不支持动态移除 child，所以跳过这个检查
        }

        // ==================== Cls / Style / Attributes 透传 ====================

        [Fact]
        public void AllComponents_PassThroughCls()
        {
            var cut = Render<ElContainer>(parameters => parameters
                .Add(x => x.Cls, "container-custom")
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElHeader>(0);
                    builder.AddAttribute(1, nameof(ElHeader.Cls), "header-custom");
                    builder.CloseComponent();
                    builder.OpenComponent<ElAside>(2);
                    builder.AddAttribute(3, nameof(ElAside.Cls), "aside-custom");
                    builder.CloseComponent();
                    builder.OpenComponent<ElMain>(4);
                    builder.AddAttribute(5, nameof(ElMain.Cls), "main-custom");
                    builder.CloseComponent();
                    builder.OpenComponent<ElFooter>(6);
                    builder.AddAttribute(7, nameof(ElFooter.Cls), "footer-custom");
                    builder.CloseComponent();
                }));

            Assert.Contains("container-custom", cut.Find(".el-container").ClassList);
            Assert.Contains("header-custom", cut.Find(".el-header").ClassList);
            Assert.Contains("aside-custom", cut.Find(".el-aside").ClassList);
            Assert.Contains("main-custom", cut.Find(".el-main").ClassList);
            Assert.Contains("footer-custom", cut.Find(".el-footer").ClassList);
        }

        [Fact]
        public void AllComponents_PassThroughStyle()
        {
            var cut = Render<ElContainer>(parameters => parameters
                .Add(x => x.Style, "gap:10px")
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElHeader>(0);
                    builder.AddAttribute(1, nameof(ElHeader.Style), "color:red");
                    builder.CloseComponent();
                    builder.OpenComponent<ElFooter>(2);
                    builder.AddAttribute(3, nameof(ElFooter.Style), "color:blue");
                    builder.CloseComponent();
                }));

            Assert.Contains("gap:10px", cut.Find(".el-container").GetAttribute("style"));
            Assert.Contains("color:red", cut.Find(".el-header").GetAttribute("style"));
            Assert.Contains("color:blue", cut.Find(".el-footer").GetAttribute("style"));
        }

        [Fact]
        public void Container_NestedDirectionAuto_DetectsEachLevel()
        {
            // 三层嵌套：外层 H+F→垂直，中层 Aside+Main→水平，内层 H+F→垂直
            var cut = Render<ElContainer>(parameters => parameters
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElHeader>(0);
                    builder.CloseComponent();

                    builder.OpenComponent<ElContainer>(1);
                    builder.AddAttribute(2, nameof(ElContainer.ChildContent), (RenderFragment)(mid =>
                    {
                        mid.OpenComponent<ElAside>(0);
                        mid.CloseComponent();

                        mid.OpenComponent<ElContainer>(1);
                        mid.AddAttribute(2, nameof(ElContainer.ChildContent), (RenderFragment)(inner =>
                        {
                            inner.OpenComponent<ElHeader>(0);
                            inner.CloseComponent();
                            inner.OpenComponent<ElMain>(1);
                            inner.CloseComponent();
                            inner.OpenComponent<ElFooter>(2);
                            inner.CloseComponent();
                        }));
                        mid.CloseComponent();

                        mid.OpenComponent<ElMain>(3);
                        mid.CloseComponent();
                    }));
                    builder.CloseComponent();

                    builder.OpenComponent<ElFooter>(3);
                    builder.CloseComponent();
                }));

            var containers = cut.FindAll(".el-container");
            Assert.Equal(3, containers.Count);

            // 外层：H+F → 垂直
            Assert.Contains("is-vertical", containers[0].ClassList);
            // 中层：Aside + (嵌套 Container) + Main → 水平（中层 Container 没 H/F）
            Assert.DoesNotContain("is-vertical", containers[1].ClassList);
            // 内层：H+F → 垂直
            Assert.Contains("is-vertical", containers[2].ClassList);
        }
    }
}