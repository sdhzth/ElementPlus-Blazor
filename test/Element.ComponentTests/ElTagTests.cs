using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElTagTests : BunitContext
    {
        public ElTagTests()
        {
            Services.AddElementServices();
        }

        [Fact]
        public void ClosableRendersCloseIconWithGlyphClass()
        {
            var cut = Render<ElTag>(parameters => parameters
                .Add(x => x.Closable, true)
                .AddChildContent("标签"));

            var close = cut.Find("i.el-tag__close");
            Assert.NotNull(close);
            Assert.Contains("el-icon-close", close.GetAttribute("class"));
        }

        [Fact]
        public void NonClosableOmitsCloseIcon()
        {
            var cut = Render<ElTag>(parameters => parameters.AddChildContent("标签"));
            Assert.Empty(cut.FindAll("i.el-tag__close"));
        }

        [Fact]
        public void CustomCloseIconClassIsUsed()
        {
            var cut = Render<ElTag>(parameters => parameters
                .Add(x => x.Closable, true)
                .Add(x => x.CloseIcon, "el-icon-circle-close")
                .AddChildContent("标签"));

            var close = cut.Find("i.el-tag__close");
            Assert.Contains("el-icon-circle-close", close.GetAttribute("class"));
        }

        [Fact]
        public void RendersTypeThemeAndSizeClasses()
        {
            var cut = Render<ElTag>(parameters => parameters
                .Add(x => x.Type, TagType.Success)
                .Add(x => x.Theme, TagTheme.Dark)
                .Add(x => x.Size, TagSize.Small)
                .AddChildContent("标签"));

            var cls = cut.Find("span.el-tag").GetAttribute("class");
            Assert.Contains("el-tag--success", cls);
            Assert.Contains("el-tag--dark", cls);
            Assert.Contains("el-tag--small", cls);
        }

        [Fact]
        public void RendersLeadingIcon()
        {
            var cut = Render<ElTag>(parameters => parameters
                .Add(x => x.Icon, "el-icon-star-on")
                .AddChildContent("标签"));

            Assert.NotNull(cut.Find("i.el-icon-star-on"));
        }

        [Fact]
        public void CloseClickRemovesTag()
        {
            var cut = Render<ElTag>(parameters => parameters
                .Add(x => x.Closable, true)
                .AddChildContent("标签"));

            Assert.NotNull(cut.Find("span.el-tag"));
            cut.Find("i.el-tag__close").Click();
            Assert.Empty(cut.FindAll("span.el-tag"));
        }
    }
}
