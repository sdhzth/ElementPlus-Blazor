using Bunit;
using ElementPlus;
using System.Collections.Generic;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 空态占位的**几何契约**（不是文字契约，文字见 LangDropdownTextTests）。
    ///
    /// 树：EP 的 DOM 是 <c>.el-tree &gt; .el-tree__empty-block &gt; .el-tree__empty-text</c>，
    /// 而 <c>.el-tree__empty-text</c> 是 <c>position:absolute; top:50%; left:50%; transform:translate(-50%,-50%)</c>，
    /// 居中基准是那个带 <c>min-height:60px</c> 的 block。本仓此前把 text 直接挂在 <c>.el-tree</c> 下面，
    /// 于是基准退化成高度为 0 的 <c>.el-tree</c> 本身 —— 消费侧（zyweb 员工维护的部门树，过滤到 0 个节点）
    /// 实测空态文字压在树搜索框的下边框上（文字 rect 顶比树容器顶还高 10px）。
    ///
    /// 表：无 Height/MaxHeight 时 <c>EmptyBlockStyle</c> 此前写死 <c>height:60px</c>，
    /// 宿主给的是自定义 <c>&lt;Empty&gt;</c> 片段（zyweb 用 133px 的 ElEmpty）时被裁掉半截
    /// （实测表格框底 392，空态文字在 395）。EP 那条规则本来就是 <c>min-height</c>，让内容能撑开。
    /// </summary>
    public class EmptyStateGeometryTests : BunitContext
    {
        public EmptyStateGeometryTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private sealed class Row
        {
            public string Name { get; set; }
        }

        [Fact]
        public void TreeWithEmptyDataSourceWrapsEmptyTextInBlock()
        {
            var cut = Render<ElTree>(p => p.Add(x => x.DataSource, new List<TreeItemBase>()));

            cut.WaitForAssertion(() =>
            {
                var block = cut.Find(".el-tree__empty-block");
                Assert.Equal("无数据", block.QuerySelector(".el-tree__empty-text")?.TextContent.Trim());
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public void TreeWithoutDataSourceWrapsEmptyTextInBlock()
        {
            var cut = Render<ElTree>();

            cut.WaitForAssertion(() =>
            {
                var block = cut.Find(".el-tree__empty-block");
                Assert.Equal("无数据", block.QuerySelector(".el-tree__empty-text")?.TextContent.Trim());
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public void TreeFilteredToNothingWrapsEmptyTextInBlock()
        {
            var cut = Render<ElTree>(p => p
                .Add(x => x.AutoExpandAll, true)
                .Add(x => x.DataSource, new List<TreeItemBase>
                {
                    new TreeItemBase { Id = 1, ParentId = 0, Text = "0001" },
                    new TreeItemBase { Id = 2, ParentId = 1, Text = "23232" }
                }));

            cut.InvokeAsync(() => cut.Instance.Filter("234"));

            // 正向对照：过滤掉全部节点后，空态块要出现，而节点要没了
            cut.WaitForAssertion(() =>
            {
                Assert.Empty(cut.FindAll(".el-tree-node"));
                Assert.Single(cut.FindAll(".el-tree__empty-block .el-tree__empty-text"));
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public void TableWithoutHeightLetsCustomEmptyGrowTheBlock()
        {
            var cut = Render<ElTable>(p => p
                .Add(x => x.DataSource, new List<Row>())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.ChildContent, builder =>
                {
                    builder.OpenComponent<ElTableColumn>(0);
                    builder.AddAttribute(1, nameof(ElTableColumn.Property), nameof(Row.Name));
                    builder.AddAttribute(2, nameof(ElTableColumn.Text), "名称");
                    builder.CloseComponent();
                })
                .Add(x => x.Empty, builder => builder.AddMarkupContent(0, "<div class=\"my-empty\" style=\"height:133px\">自定义空态</div>")));

            cut.WaitForAssertion(() =>
            {
                var style = cut.Find(".el-table__empty-block").GetAttribute("style") ?? "";
                // 用 StartsWith 而不是 DoesNotContain("height:60px")：后者会被 "min-height:60px" 里的子串命中，是假红
                Assert.StartsWith("min-height:", style);
                Assert.NotNull(cut.Find(".el-table__empty-block .my-empty"));
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public void TableWithHeightStillPinsEmptyBlockHeight()
        {
            var cut = Render<ElTable>(p => p
                .Add(x => x.DataSource, new List<Row>())
                .Add(x => x.Height, 300)
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.ChildContent, builder =>
                {
                    builder.OpenComponent<ElTableColumn>(0);
                    builder.AddAttribute(1, nameof(ElTableColumn.Property), nameof(Row.Name));
                    builder.AddAttribute(2, nameof(ElTableColumn.Text), "名称");
                    builder.CloseComponent();
                }));

            // 定高表格仍要按高度撑满正文区，空态才落在可见区中间（这条不能顺手改成 min-height）
            cut.WaitForAssertion(() =>
            {
                var style = cut.Find(".el-table__empty-block").GetAttribute("style") ?? "";
                Assert.StartsWith("height:", style);
                Assert.EndsWith("px", style);
            }, timeout: TestWait.Timeout);
        }
    }
}
