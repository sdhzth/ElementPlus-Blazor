using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElDataDisplayExistingTests : BunitContext
    {
        public ElDataDisplayExistingTests()
        {
            Services.AddElementPlusServices();
        }

        [Fact]
        public void TagSupportsEffectRoundHitDisableTransitionsAndCloseAlias()
        {
            var closed = 0;
            var cut = Render<ElTag>(parameters => parameters
                .Add(x => x.Type, TagType.Warning)
                .Add(x => x.Effect, TagEffect.Plain)
                .Add(x => x.Round, true)
                .Add(x => x.Hit, true)
                .Add(x => x.DisableTransitions, true)
                .Add(x => x.Closable, true)
                .Add(x => x.OnClose, () => closed++)
                .AddChildContent("Alpha"));

            var tag = cut.Find(".el-tag");
            Assert.Contains("el-tag--plain", tag.ClassList);
            Assert.Contains("is-round", tag.ClassList);
            Assert.Contains("is-hit", tag.ClassList);
            Assert.Contains("el-tag--no-transition", tag.ClassList);

            cut.Find(".el-tag__close").Click();

            Assert.Equal(1, closed);
            Assert.Empty(cut.Markup.Trim());
        }

        [Fact]
        public async Task PaginationSupportsLayoutSizesAndJumper()
        {
            var current = 2;
            var pageSize = 10;
            var cut = Render<ElPagination>(parameters => parameters
                .Add(x => x.Total, 95)
                .Add(x => x.PageSize, pageSize)
                .Add(x => x.CurrentPage, current)
                .Add(x => x.Layout, "total, sizes, prev, pager, next, jumper")
                .Add(x => x.PageSizes, new[] { 10, 20, 50 })
                .Add(x => x.CurrentPageChanged, page =>
                {
                    current = page;
                    return Task.CompletedTask;
                })
                .Add(x => x.PageSizeChanged, size => pageSize = size));

            Assert.Equal("共 95 条", cut.Find(".el-pagination__total").TextContent.Trim());
            // 每页条数 = ElSelect（EP 原生下拉），不再是裸 <select>
            var sizeSelect = cut.Find(".el-pagination__sizes .el-pagination__sizes-select");
            Assert.Contains("el-select", sizeSelect.ClassList);
            Assert.Empty(cut.FindAll(".el-pagination__sizes select"));
            Assert.Contains("10 条/页", cut.Find(".el-pagination__sizes input").GetAttribute("value"));
            Assert.Equal(10, pageSize);
            Assert.NotEmpty(cut.FindAll(".el-pagination__jump"));

            // 跳页框 = ElInputNumber（ControlsPosition="right"，右侧竖直 ± 步进按钮），取内层 input 提交
            var jumperBox = cut.Find(".el-pagination__jump .el-pagination__editor");
            Assert.Contains("el-input-number", jumperBox.ClassList);
            Assert.Contains("is-controls-right", jumperBox.ClassList);
            Assert.Equal(2, jumperBox.QuerySelectorAll(".el-input-number__increase, .el-input-number__decrease").Length);

            cut.Find(".el-pagination__editor input").Change("4");

            Assert.Equal(4, current);
            Assert.Contains("active", cut.FindAll(".el-pager .number").First(x => x.TextContent.Trim() == "4").ClassList);
        }
    }
}
