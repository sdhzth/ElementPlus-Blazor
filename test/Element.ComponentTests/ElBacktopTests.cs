using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElBacktopTests : BunitContext
    {
        public ElBacktopTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void RendersDefaultCaretIcon()
        {
            var cut = Render<ElBacktop>();

            Assert.NotNull(cut.Find(".el-backtop"));
            Assert.NotNull(cut.Find(".el-icon-caret-top"));
            Assert.Equal("button", cut.Find(".el-backtop").GetAttribute("role"));
        }

        [Fact]
        public void CustomSlotReplacesIcon()
        {
            var cut = Render<ElBacktop>(p => p
                .AddChildContent("<span class='my-top'>TOP</span>"));

            Assert.NotNull(cut.Find(".my-top"));
            Assert.Empty(cut.FindAll(".el-icon-caret-top"));
        }

        [Fact]
        public void RightAndBottomAreWrittenToStyle()
        {
            var cut = Render<ElBacktop>(p => p
                .Add(x => x.Right, 80)
                .Add(x => x.Bottom, 90));

            var style = cut.Find(".el-backtop").GetAttribute("style");
            Assert.Contains("right:80px", style);
            Assert.Contains("bottom:90px", style);
        }

        [Fact]
        public void HiddenByDefaultAndShownByJs()
        {
            var cut = Render<ElBacktop>();

            Assert.Contains("display:none", cut.Find(".el-backtop").GetAttribute("style"));
            Assert.False(cut.Instance.Visible);

            cut.InvokeAsync(() => cut.Instance.SetVisible(true));

            Assert.True(cut.Instance.Visible);
            Assert.Contains("is-visible", cut.Find(".el-backtop").ClassList);
            Assert.DoesNotContain("display:none", cut.Find(".el-backtop").GetAttribute("style"));
        }

        [Fact]
        public void IdAndAriaLabelAreRendered()
        {
            var cut = Render<ElBacktop>(p => p
                .Add(x => x.Id, "bt-1")
                .Add(x => x.AriaLabel, "回到顶部"));

            Assert.Equal("bt-1", cut.Find(".el-backtop").GetAttribute("id"));
            Assert.Equal("回到顶部", cut.Find(".el-backtop").GetAttribute("aria-label"));
        }

        [Fact]
        public void ClickRaisesOnClick()
        {
            var clicked = 0;
            var cut = Render<ElBacktop>(p => p
                .Add(x => x.OnClick, (Microsoft.AspNetCore.Components.Web.MouseEventArgs e) => clicked++));

            cut.Find(".el-backtop").Click();

            Assert.Equal(1, clicked);
        }
    }
}
