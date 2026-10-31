using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElEmptyTests : BunitContext
    {
        public ElEmptyTests()
        {
            Services.AddElementServices();
        }

        [Fact]
        public void RendersRootAndDefaultDescription()
        {
            var cut = Render<ElEmpty>(p => p.Add(x => x.Description, (string)null));

            Assert.Single(cut.FindAll(".el-empty"));
            // 描述回落到本地化默认「暂无数据」
            Assert.Equal("暂无数据", cut.Find(".el-empty__description p").TextContent.Trim());
        }

        [Fact]
        public void DescriptionShowsProvidedText()
        {
            var cut = Render<ElEmpty>(p => p.Add(x => x.Description, "找不到该条件下的数据"));

            Assert.Equal("找不到该条件下的数据", cut.Find(".el-empty__description p").TextContent.Trim());
        }

        [Fact]
        public void ImageUrlRendersImgAndHidesDefaultSvg()
        {
            var cut = Render<ElEmpty>(p => p.Add(x => x.Image, "https://example.com/a.png"));

            var img = cut.Find(".el-empty__image img");
            Assert.Equal("https://example.com/a.png", img.GetAttribute("src"));
            // 给了 Image 后，默认插画 svg 不应出现
            Assert.Empty(cut.FindAll(".el-empty__image svg"));
        }

        [Fact]
        public void ImageSizeSetsWidthStyle()
        {
            var cut = Render<ElEmpty>(p => p.Add(x => x.ImageSize, 120));

            Assert.Contains("width:120px", cut.Find(".el-empty__image").GetAttribute("style"));
        }

        [Fact]
        public void BottomSlotRenders()
        {
            var cut = Render<ElEmpty>(p => p
                .Add(x => x.ChildContent, (RenderFragment)(b => b.AddContent(0, "去逛逛"))));

            Assert.Contains("去逛逛", cut.Find(".el-empty__bottom").TextContent);
        }

        [Fact]
        public void ImageContentSlotReplacesDefaultSvg()
        {
            var cut = Render<ElEmpty>(p => p
                .Add(x => x.ImageContent, (RenderFragment)(b => b.AddMarkupContent(0, "<svg class='custom-img'></svg>"))));

            Assert.Single(cut.FindAll(".el-empty__image svg"));
            Assert.Single(cut.FindAll(".custom-img"));
        }

        [Fact]
        public void DescriptionContentSlotReplacesDefaultParagraph()
        {
            var cut = Render<ElEmpty>(p => p
                .Add(x => x.DescriptionContent, (RenderFragment)(b => b.AddMarkupContent(0, "<span class='custom-desc'>网络异常</span>"))));

            Assert.Single(cut.FindAll(".custom-desc"));
            Assert.Empty(cut.FindAll(".el-empty__description p"));
        }

        [Fact]
        public void DefaultSvgUsesFillColorVariables()
        {
            var cut = Render<ElEmpty>(p => p.Add(x => x.Description, (string)null));
            var markup = cut.Find(".el-empty__image").InnerHtml;

            Assert.Contains("var(--el-empty-fill-color-3)", markup);
            Assert.Contains("var(--el-empty-fill-color-1)", markup);
        }
    }
}
