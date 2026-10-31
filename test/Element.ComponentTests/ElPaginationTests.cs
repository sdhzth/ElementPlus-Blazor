using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElPaginationTests : BunitContext
    {
        public ElPaginationTests()
        {
            Services.AddElementServices();
        }

        [Fact]
        public void RendersTotalText()
        {
            var cut = Render<ElPagination>(p => p
                .Add(x => x.Total, 95)
                .Add(x => x.Layout, "total, prev, pager, next"));
            Assert.Equal("共 95 条", cut.Find(".el-pagination__total").TextContent.Trim());
        }

        [Fact]
        public void PageCountTakesPriorityOverTotal()
        {
            // EP：page-count 优先；设置后无需 total 即可显示页码
            var cut = Render<ElPagination>(p => p
                .Add(x => x.PageCount, 20)
                .Add(x => x.PageSize, 10)
                .Add(x => x.CurrentPage, 1)
                .Add(x => x.Layout, "prev, pager, next"));
            var numbers = cut.FindAll(".el-pager li.number");
            Assert.NotEmpty(numbers);
            Assert.Equal("20", numbers.Last().TextContent.Trim());
        }

        [Fact]
        public void HideOnSinglePage_HidesWhenOnlyOnePage()
        {
            var cut = Render<ElPagination>(p => p
                .Add(x => x.Total, 5)
                .Add(x => x.PageSize, 10)
                .Add(x => x.HideOnSinglePage, true)
                .Add(x => x.Layout, "prev, pager, next, jumper"));
            Assert.Empty(cut.FindAll("div.el-pagination"));
        }

        [Fact]
        public void HideOnSinglePage_ShowsWhenDisabled()
        {
            var cut = Render<ElPagination>(p => p
                .Add(x => x.Total, 5)
                .Add(x => x.PageSize, 10)
                .Add(x => x.HideOnSinglePage, false)
                .Add(x => x.Layout, "prev, pager, next, jumper"));
            Assert.NotEmpty(cut.FindAll("div.el-pagination"));
        }

        [Fact]
        public void LayoutArrowPushesSubsequentPartsToRightWrapper()
        {
            var cut = Render<ElPagination>(p => p
                .Add(x => x.Total, 95)
                .Add(x => x.Layout, "prev, pager, next, ->, total"));
            // -> 之后的 total 应被包进 .el-pagination__rightwrapper
            Assert.NotEmpty(cut.FindAll(".el-pagination__rightwrapper"));
            var totalInside = cut.Find(".el-pagination__rightwrapper .el-pagination__total");
            Assert.Equal("共 95 条", totalInside.TextContent.Trim());
        }

        [Fact]
        public void SlotTokenRendersSlotContent()
        {
            var cut = Render<ElPagination>(p => p
                .Add(x => x.Total, 95)
                .Add(x => x.Layout, "prev, pager, next, slot")
                .Add(x => x.SlotContent, (RenderFragment)(b => b.AddContent(0, "自定义内容"))));
            var slot = cut.Find(".el-pagination__slot");
            Assert.Contains("自定义内容", slot.TextContent);
        }

        [Fact]
        public void PrevButtonRendersDefaultArrowWhenNoTextOrIcon()
        {
            var cut = Render<ElPagination>(p => p
                .Add(x => x.Total, 100)
                .Add(x => x.Layout, "prev, pager, next"));
            var prevBtn = cut.FindAll("button.el-button").First();
            Assert.NotNull(prevBtn.QuerySelector("i.el-icon-arrow-left"));
        }

        [Fact]
        public void PrevTextReplacesArrowIcon()
        {
            var cut = Render<ElPagination>(p => p
                .Add(x => x.Total, 100)
                .Add(x => x.PrevText, "上一页")
                .Add(x => x.Layout, "prev, pager, next"));
            var prevBtn = cut.FindAll("button.el-button").First();
            Assert.Contains("上一页", prevBtn.TextContent);
            Assert.Null(prevBtn.QuerySelector("i.el-icon-arrow-left"));
        }

        [Fact]
        public async Task NextClickFiresEvents()
        {
            int nextPage = 0;
            int current = 0;
            (int, int) change = (0, 0);
            var cut = Render<ElPagination>(p => p
                .Add(x => x.Total, 100)
                .Add(x => x.PageSize, 10)
                .Add(x => x.CurrentPage, 1)
                .Add(x => x.Layout, "prev, pager, next")
                .Add(x => x.NextClick, (int pg) => nextPage = pg)
                .Add(x => x.CurrentPageChanged, (int pg) => current = pg)
                .Add(x => x.Change, (e) => change = e));
            var nextIcon = cut.Find("i.el-icon-arrow-right");
            nextIcon.Closest("button").Click();
            Assert.Equal(2, nextPage);
            Assert.Equal(2, current);
            Assert.Equal(2, change.Item1);
            Assert.Equal(10, change.Item2);
        }
    }
}
