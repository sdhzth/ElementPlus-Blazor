using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElPageHeaderTests : BunitContext
    {
        public ElPageHeaderTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void RendersTitleAndContent()
        {
            var cut = Render<ElPageHeader>(p => p
                .Add(x => x.Title, "返回")
                .Add(x => x.Content, "详情页"));

            Assert.Contains("返回", cut.Find(".el-page-header__title").TextContent);
            Assert.Contains("详情页", cut.Find(".el-page-header__content").TextContent);
        }

        [Fact]
        public void DefaultIconIsBack()
        {
            var cut = Render<ElPageHeader>();

            Assert.NotNull(cut.Find(".el-page-header__left i"));
        }

        [Fact]
        public void TitleSlotOverridesTitle()
        {
            var cut = Render<ElPageHeader>(p => p
                .Add(x => x.Title, "原标题")
                .Add(x => x.TitleContent, (RenderFragment)(b =>
                {
                    b.OpenElement(0, "span");
                    b.AddAttribute(1, "class", "my-title");
                    b.AddContent(2, "自定义标题");
                    b.CloseElement();
                })));

            Assert.NotNull(cut.Find(".my-title"));
            Assert.DoesNotContain("原标题", cut.Markup);
        }

        [Fact]
        public void ExtraAndBreadcrumbSlotsRenderOnlyWhenProvided()
        {
            var without = Render<ElPageHeader>();
            Assert.Empty(without.FindAll(".el-page-header__extra"));
            Assert.Empty(without.FindAll(".el-page-header__breadcrumb"));

            var with = Render<ElPageHeader>(p => p
                .Add(x => x.ExtraContent, (RenderFragment)(b => b.AddContent(0, "额外")))
                .Add(x => x.BreadcrumbContent, (RenderFragment)(b => b.AddContent(0, "面包屑"))));

            Assert.NotNull(with.Find(".el-page-header__extra"));
            Assert.NotNull(with.Find(".el-page-header__breadcrumb"));
        }

        [Fact]
        public void BackEventFiresOnLeftClick()
        {
            var backs = 0;
            var cut = Render<ElPageHeader>(p => p
                .Add(x => x.OnBack, (Microsoft.AspNetCore.Components.Web.MouseEventArgs e) => backs++));

            cut.Find(".el-page-header__left").Click();

            Assert.Equal(1, backs);
        }

        [Fact]
        public void IdAndAriaLabelAreRendered()
        {
            var cut = Render<ElPageHeader>(p => p
                .Add(x => x.Id, "ph-1")
                .Add(x => x.AriaLabel, "页头"));

            Assert.Equal("ph-1", cut.Find(".el-page-header").GetAttribute("id"));
            Assert.Equal("页头", cut.Find(".el-page-header").GetAttribute("aria-label"));
        }
    }
}
