using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElAffixTests : BunitContext
    {
        public ElAffixTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void RendersAffixRootAndContent()
        {
            var cut = Render<ElAffix>(p => p
                .AddChildContent("<span class='affix-inner'>内容</span>"));

            Assert.NotNull(cut.Find(".el-affix"));
            Assert.NotNull(cut.Find(".el-affix__content"));
            Assert.NotNull(cut.Find(".affix-inner"));
        }

        [Fact]
        public void ZIndexIsAlwaysWrittenToContent()
        {
            var cut = Render<ElAffix>(p => p
                .Add(x => x.ZIndex, 999)
                .AddChildContent("x"));

            Assert.Contains("z-index:999", cut.Find(".el-affix__content").GetAttribute("style"));
        }

        [Fact]
        public void OffsetMapsByPosition()
        {
            var top = Render<ElAffix>(p => p
                .Add(x => x.Position, AffixPosition.Top)
                .Add(x => x.Offset, 30));

            Assert.Equal(30, top.Instance.OffsetTop);
            Assert.Equal(0, top.Instance.OffsetBottom);

            var bottom = Render<ElAffix>(p => p
                .Add(x => x.Position, AffixPosition.Bottom)
                .Add(x => x.Offset, 40));

            Assert.Equal(40, bottom.Instance.OffsetBottom);
            Assert.Equal(0, bottom.Instance.OffsetTop);
        }

        [Fact]
        public void SetFixedFlipsIsFixedAndRaisesChange()
        {
            var changed = 0;
            var lastFixed = false;
            var cut = Render<ElAffix>(p => p
                .Add(x => x.OnChange, (bool v) =>
                {
                    changed++;
                    lastFixed = v;
                })
                .AddChildContent("x"));

            Assert.False(cut.Instance.IsFixed);

            cut.InvokeAsync(() => cut.Instance.SetFixed(true, 120));

            Assert.True(cut.Instance.IsFixed);
            Assert.Equal(1, changed);
            Assert.True(lastFixed);
        }

        [Fact]
        public void ScrollEventAlwaysFires()
        {
            var scrolls = 0;
            var cut = Render<ElAffix>(p => p
                .Add(x => x.OnScroll, (AffixScrollEventArgs e) => scrolls++)
                .AddChildContent("x"));

            cut.InvokeAsync(() => cut.Instance.SetFixed(true, 10));
            cut.InvokeAsync(() => cut.Instance.SetFixed(true, 20));

            Assert.Equal(2, scrolls);
        }

        [Fact]
        public void IsFixedClassAppearsWhenFixed()
        {
            var cut = Render<ElAffix>(p => p.AddChildContent("x"));

            Assert.DoesNotContain("is-fixed", cut.Find(".el-affix").ClassList);

            cut.InvokeAsync(() => cut.Instance.SetFixed(true, 10));

            Assert.Contains("is-fixed", cut.Find(".el-affix").ClassList);
        }
    }
}
