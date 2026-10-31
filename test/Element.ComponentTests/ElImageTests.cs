using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElImageTests : BunitContext
    {
        public ElImageTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddElementServices();
        }

        [Fact]
        public void InitialIndexSelectsStartingPreviewImage()
        {
            var urls = new[]
            {
                "https://example.com/a.png",
                "https://example.com/b.png",
                "https://example.com/c.png"
            };
            var cut = Render<ElImage>(parameters => parameters
                .Add(x => x.Src, urls[0])
                .Add(x => x.PreviewSrcList, urls)
                .Add(x => x.InitialIndex, 2)
                .Add(x => x.PreviewTeleported, false));

            cut.Find("img.el-image__inner").Click();
            cut.Render();

            var previewImg = cut.Find(".el-image-viewer__canvas img");
            Assert.Equal(urls[2], previewImg.GetAttribute("src"));
        }

        [Fact]
        public void PreviewFalse_DoesNotOpenViewerOnClick()
        {
            var cut = Render<ElImage>(parameters => parameters
                .Add(x => x.Src, "https://example.com/avatar.png")
                .Add(x => x.Preview, false));

            cut.Find("img.el-image__inner").Click();
            cut.Render();

            Assert.Empty(cut.FindAll(".el-image-viewer__canvas"));
            Assert.Empty(cut.FindAll(".el-image-viewer__mask"));
        }

        /// <summary>
        /// 正向对照：<c>Preview</c> 默认 true，且本库在不传 <c>PreviewSrcList</c> 时回落到预览 <c>Src</c>。
        /// 少了这条，上一条的「查不到查看器」可能只是因为选择器或点击本身没生效。
        /// </summary>
        [Fact]
        public void PreviewDefault_StillOpensViewerFromSrcFallback()
        {
            var cut = Render<ElImage>(parameters => parameters
                .Add(x => x.Src, "https://example.com/avatar.png"));

            cut.Find("img.el-image__inner").Click();
            cut.Render();

            var previewImg = cut.Find(".el-image-viewer__canvas img");
            Assert.Equal("https://example.com/avatar.png", previewImg.GetAttribute("src"));
        }

        [Fact]
        public void PreviewFalse_WinsOverExplicitPreviewSrcList()
        {
            var cut = Render<ElImage>(parameters => parameters
                .Add(x => x.Src, "https://example.com/a.png")
                .Add(x => x.PreviewSrcList, new[] { "https://example.com/a.png", "https://example.com/b.png" })
                .Add(x => x.Preview, false));

            cut.Find("img.el-image__inner").Click();
            cut.Render();

            Assert.Empty(cut.FindAll(".el-image-viewer__canvas"));
        }

        /// <summary>关掉预览不等于关掉点击：宿主仍要能拿到 OnClick（头像点开个人资料这类用法）。</summary>
        [Fact]
        public void PreviewFalse_StillInvokesOnClick()
        {
            var clicked = 0;
            var cut = Render<ElImage>(parameters => parameters
                .Add(x => x.Src, "https://example.com/avatar.png")
                .Add(x => x.Preview, false)
                .Add(x => x.OnClick, EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(
                    this, () => clicked++)));

            cut.Find("img.el-image__inner").Click();

            Assert.Equal(1, clicked);
        }
    }
}
