using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using System;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElInfiniteScrollTests : BunitContext
    {
        public ElInfiniteScrollTests()
        {
            Services.AddElementPlusServices();
        }

        [Fact]
        public void RendersContainerWithChildContent()
        {
            var cut = Render<ElInfiniteScroll>(p => p.AddChildContent("<div class=\"child\">Rows</div>"));
            var root = cut.Find(".el-infinite-scroll");
            Assert.NotNull(root);
            Assert.Contains("child", root.InnerHtml);
        }

        [Fact]
        public void LoadingShowsIndicatorAndSpinner()
        {
            var cut = Render<ElInfiniteScroll>(p => p
                .Add(x => x.Loading, true)
                .AddChildContent("<div>Rows</div>"));

            Assert.Contains("is-loading", cut.Find(".el-infinite-scroll").ClassList);
            Assert.Equal("加载中", cut.Find(".el-infinite-scroll__loading").TextContent.Trim());
            Assert.NotNull(cut.Find(".el-infinite-scroll__spinner"));
        }

        [Fact]
        public void DisabledShowsDisabledText()
        {
            var cut = Render<ElInfiniteScroll>(p => p
                .Add(x => x.Disabled, true)
                .AddChildContent("<div>Rows</div>"));

            Assert.Contains("is-disabled", cut.Find(".el-infinite-scroll").ClassList);
            Assert.Equal("没有更多了", cut.Find(".el-infinite-scroll__disabled").TextContent.Trim());
        }

        [Fact]
        public void ImmediateFalseDoesNotAutoLoad()
        {
            var loads = new System.Collections.Generic.List<int>();
            Render<ElInfiniteScroll>(p => p
                .Add(x => x.Immediate, false)
                .Add(x => x.OnLoad, args => loads.Add(args.LoadCount))
                .AddChildContent("<div>Rows</div>"));

            Assert.Empty(loads);
        }

        [Fact]
        public void DisabledStopsLoadOnScroll()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            JSInterop.Setup<decimal>("Element.getScrollDistance", _ => true).SetResult(1m);

            var loads = new System.Collections.Generic.List<int>();
            var cut = Render<ElInfiniteScroll>(p => p
                .Add(x => x.Disabled, true)
                .Add(x => x.Delay, 0)
                .Add(x => x.OnLoad, args => loads.Add(args.LoadCount))
                .AddChildContent("<div>Rows</div>"));

            cut.Find(".el-infinite-scroll").TriggerEvent("onscroll", EventArgs.Empty);
            Assert.Empty(loads);
        }

        [Fact]
        public void ScrollTriggersLoadWhenNearBottom()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            JSInterop.Setup<decimal>("Element.getScrollDistance", _ => true).SetResult(1m);

            var loads = new System.Collections.Generic.List<int>();
            var cut = Render<ElInfiniteScroll>(p => p
                .Add(x => x.Delay, 0)
                .Add(x => x.OnLoad, args => loads.Add(args.LoadCount))
                .AddChildContent("<div>Rows</div>"));

            cut.Find(".el-infinite-scroll").TriggerEvent("onscroll", EventArgs.Empty);
            Assert.True(loads.Count >= 1, $"expected at least 1 load, got {loads.Count}");
        }

        [Fact]
        public void HeightAppliesOverflowStyle()
        {
            var cut = Render<ElInfiniteScroll>(p => p
                .Add(x => x.Height, "200")
                .AddChildContent("<div>Rows</div>"));

            var style = cut.Find(".el-infinite-scroll").GetAttribute("style") ?? string.Empty;
            Assert.Contains("height:200px", style);
            Assert.Contains("overflow:auto", style);
        }
    }
}
