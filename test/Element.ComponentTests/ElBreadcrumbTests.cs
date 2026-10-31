using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElBreadcrumbTests : BunitContext
    {
        public ElBreadcrumbTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static RenderFragment TwoItems() => builder =>
        {
            builder.OpenComponent<ElBreadcrumbItem>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "首页")));
            builder.CloseComponent();
            builder.OpenComponent<ElBreadcrumbItem>(2);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "详情")));
            builder.CloseComponent();
        };

        [Fact]
        public void RendersNavigationRoleAndDefaultAriaLabel()
        {
            var cut = Render<ElBreadcrumb>(p => p.AddChildContent(TwoItems()));

            Assert.Equal("navigation", cut.Find(".el-breadcrumb").GetAttribute("role"));
            Assert.Equal("Breadcrumb", cut.Find(".el-breadcrumb").GetAttribute("aria-label"));
        }

        [Fact]
        public void DefaultSeparatorIsSlash()
        {
            var cut = Render<ElBreadcrumb>(p => p.AddChildContent(TwoItems()));

            var separators = cut.FindAll(".el-breadcrumb__separator");
            Assert.NotEmpty(separators);
            Assert.Equal("/", separators[0].TextContent.Trim());
        }

        [Fact]
        public void CustomSeparatorIsUsed()
        {
            var cut = Render<ElBreadcrumb>(p => p
                .Add(x => x.Separator, ">")
                .AddChildContent(TwoItems()));

            Assert.Equal(">", cut.FindAll(".el-breadcrumb__separator")[0].TextContent.Trim());
        }

        [Fact]
        public void SeparatorIconRendersIconElement()
        {
            var cut = Render<ElBreadcrumb>(p => p
                .Add(x => x.SeparatorIcon, "el-icon-arrow-right")
                .AddChildContent(TwoItems()));

            Assert.Contains("el-icon-arrow-right", cut.FindAll(".el-breadcrumb__separator")[0].GetAttribute("class"));
        }

        [Fact]
        public void SeparatorTemplateWinsOverSeparator()
        {
            var cut = Render<ElBreadcrumb>(p => p
                .Add(x => x.Separator, "/")
                .Add(x => x.SeparatorTemplate, (RenderFragment)(builder =>
                {
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "class", "my-sep");
                    builder.CloseElement();
                }))
                .AddChildContent(TwoItems()));

            Assert.NotNull(cut.Find(".my-sep"));
        }

        [Fact]
        public void ItemWithToRendersAnchor()
        {
            var cut = Render<ElBreadcrumb>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElBreadcrumbItem>(0);
                builder.AddAttribute(1, "To", "/home");
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "首页")));
                builder.CloseComponent();
            }));

            var link = cut.Find("a.el-breadcrumb__inner");
            Assert.Equal("/home", link.GetAttribute("href"));
            Assert.Contains("is-link", link.GetAttribute("class"));
        }
    }
}
