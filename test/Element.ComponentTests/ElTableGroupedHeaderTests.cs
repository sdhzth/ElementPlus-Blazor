using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElTable 的两层表头（分组列）契约。
    ///
    /// 背景：交叉报表（Radzen 的嵌套 Columns）需要一个父标题横跨若干子列的两行表头，
    /// 本库原先只有单行表头。这里的约定是「分组只影响表头那两行」：
    /// colgroup、表体、取数、排序、筛选、固定列偏移一律继续按 <c>Headers</c> 里的叶子列算，
    /// 分组表头不进 <c>Headers</c>，所以没有分组的老表格渲染结果必须一个字节都不变。
    /// </summary>
    public class ElTableGroupedHeaderTests : BunitContext
    {
        public ElTableGroupedHeaderTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        public class Row
        {
            public string Name { get; set; }
            public int Qty { get; set; }
            public decimal Amount { get; set; }
        }

        private static List<Row> Rows() => new()
        {
            new Row { Name = "A", Qty = 3, Amount = 30m },
            new Row { Name = "B", Qty = 1, Amount = 12m },
        };

        private static RenderFragment LeafColumn(string property, string text) => b =>
        {
            var seq = 0;
            b.OpenComponent<ElTableColumn>(seq++);
            b.AddAttribute(seq++, "Property", property);
            b.AddAttribute(seq++, "Text", text);
            b.CloseComponent();
        };

        /// <summary>分组列：父标题 + 子列片段（片段参数名是 ChildColumns，Columns 已被 ElTableColumns 级联参数占用）。</summary>
        private static RenderFragment GroupColumn(string text, params (string Property, string Text)[] children) => b =>
        {
            var seq = 0;
            b.OpenComponent<ElTableColumnGroup>(seq++);
            b.AddAttribute(seq++, "Text", text);
            b.AddAttribute(seq++, "ChildColumns", new RenderFragment(cb =>
            {
                var s = 0;
                foreach (var child in children)
                {
                    cb.OpenComponent<ElTableColumn>(s++);
                    cb.AddAttribute(s++, "Property", child.Property);
                    cb.AddAttribute(s++, "Text", child.Text);
                    cb.CloseComponent();
                }
            }));
            b.CloseComponent();
        };

        private IRenderedComponent<ElTable> RenderTable(RenderFragment childContent)
        {
            return Render<ElTable>(p => p
                .Add(x => x.DataSource, Rows())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.EnablePagination, false)
                .AddChildContent(childContent));
        }

        /// <summary>没有分组时仍是单行表头——老表格的渲染路径不能被这次改动影响。</summary>
        [Fact]
        public void UngroupedColumnsStillRenderSingleHeaderRow()
        {
            var cut = RenderTable(b =>
            {
                LeafColumn("Name", "名称")(b);
                LeafColumn("Qty", "数量")(b);
            });

            var rows = cut.FindAll(".el-table__header thead tr");
            Assert.Single(rows);
            // 每行末尾还有一个 th.gutter（滚动条占位），所以只数真正的表头单元格 th.el-table__cell
            Assert.Equal(2, HeaderCells(rows[0]).Count);
            Assert.All(HeaderCells(rows[0]), th => Assert.Contains("is-leaf", th.ClassName));
        }

        private static List<AngleSharp.Dom.IElement> HeaderCells(AngleSharp.Dom.IElement row)
            => row.QuerySelectorAll("th.el-table__cell").ToList();

        /// <summary>分组列只出现在表头第一行：父标题 colspan＝可见子列数，顶层列与选择列 rowspan＝2。</summary>
        [Fact]
        public void GroupHeaderSpansItsChildrenAndLeavesSpanTwoRows()
        {
            var cut = RenderTable(b =>
            {
                LeafColumn("Name", "名称")(b);
                GroupColumn("销售", ("Qty", "数量"), ("Amount", "金额"))(b);
            });

            var rows = cut.FindAll(".el-table__header thead tr");
            Assert.Equal(2, rows.Count);

            var first = HeaderCells(rows[0]);
            Assert.Equal(2, first.Count);
            Assert.Equal("1", first[0].GetAttribute("colspan"));
            Assert.Equal("2", first[0].GetAttribute("rowspan"));
            Assert.Equal("名称", first[0].TextContent.Trim());
            Assert.Equal("2", first[1].GetAttribute("colspan"));
            Assert.Equal("1", first[1].GetAttribute("rowspan"));
            Assert.Equal("销售", first[1].TextContent.Trim());
            Assert.Contains("is-group", first[1].ClassName);

            var second = HeaderCells(rows[1]);
            Assert.Equal(2, second.Count);
            Assert.Equal(new[] { "数量", "金额" }, second.Select(th => th.TextContent.Trim()).ToArray());
            Assert.All(second, th => Assert.Contains("is-leaf", th.ClassName));
        }

        /// <summary>分组不进 Headers：colgroup 与表体只按叶子列出一列，父标题不该多占一格。</summary>
        [Fact]
        public void GroupIsNotRegisteredAsLeafColumn()
        {
            var cut = RenderTable(b =>
            {
                LeafColumn("Name", "名称")(b);
                GroupColumn("销售", ("Qty", "数量"), ("Amount", "金额"))(b);
            });

            Assert.Equal(3, cut.Instance.Headers.Count);
            Assert.Equal(new[] { "名称", "数量", "金额" }, cut.Instance.Headers.Select(h => h.Text).ToArray());
            Assert.All(cut.Instance.Headers, h => Assert.False(h.IsGroup));

            var cols = cut.FindAll(".el-table__header colgroup col");
            Assert.Equal(3, cols.Count(c => c.GetAttribute("name") != "gutter"));

            // 表体每行的单元格数也必须等于叶子列数（分组不能多出 td）
            foreach (var tr in cut.FindAll(".el-table__body tbody tr"))
            {
                Assert.Equal(3, tr.QuerySelectorAll("td").Length);
            }
        }

        /// <summary>
        /// 交叉报表的真实形状：多个分组的子列标题会重复（每组都有「数量/金额」）。
        /// 去重签名必须带上所属分组标题，否则第二个分组之后的子列会被当成重复列丢掉。
        /// </summary>
        [Fact]
        public void RepeatedChildTitlesAcrossGroupsAreAllKept()
        {
            var cut = RenderTable(b =>
            {
                LeafColumn("Name", "名称")(b);
                GroupColumn("门店甲", ("Qty", "数量"), ("Amount", "金额"))(b);
                GroupColumn("门店乙", ("Qty", "数量"), ("Amount", "金额"))(b);
            });

            Assert.Equal(5, cut.Instance.Headers.Count);
            var rows = cut.FindAll(".el-table__header thead tr");
            Assert.Equal(2, rows.Count);
            Assert.Equal(new[] { "2", "2" }, HeaderCells(rows[0]).Where(th => th.ClassName.Contains("is-group"))
                .Select(th => th.GetAttribute("colspan")).ToArray());
            Assert.Equal(4, HeaderCells(rows[1]).Count);
        }

        /// <summary>叶子列的类名序号与 colgroup 的列名一一对应，表头与表体才不会错位。</summary>
        [Fact]
        public void LeafHeaderColumnIndexMatchesColgroup()
        {
            var cut = RenderTable(b =>
            {
                LeafColumn("Name", "名称")(b);
                GroupColumn("销售", ("Qty", "数量"), ("Amount", "金额"))(b);
            });

            var leafThs = cut.FindAll(".el-table__header thead th")
                .Where(th => th.ClassName.Contains("is-leaf"))
                .ToList();
            var indexes = leafThs.Select(th => th.ClassName.Split(' ').First(c => c.StartsWith("el-table_1_column_"))).ToList();
            Assert.Equal(new[] { "el-table_1_column_1", "el-table_1_column_2", "el-table_1_column_3" }, indexes);

            var colNames = cut.FindAll(".el-table__header colgroup col")
                .Where(c => c.GetAttribute("name") != "gutter")
                .Select(c => c.GetAttribute("name")).ToArray();
            Assert.Equal(indexes, colNames);
        }

        /// <summary>分组第二行的叶子表头照样能排序：分组分支复制了单行分支的点击/排序/筛选挂接，不能只画个壳。</summary>
        [Fact]
        public void LeafHeaderInSecondRowStillSorts()
        {
            var cut = Render<ElTable>(p => p
                .Add(x => x.DataSource, Rows())
                .Add(x => x.AutoGenerateColumns, false)
                // 选择列默认是开着的，第一格会是复选框列（空文本）；这里要读的是名称列，所以显式关掉。
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.EnablePagination, false)
                .AddChildContent(b =>
                {
                    var seq = 0;
                    b.OpenComponent<ElTableColumn>(seq++);
                    b.AddAttribute(seq++, "Property", "Name");
                    b.AddAttribute(seq++, "Text", "名称");
                    b.CloseComponent();

                    b.OpenComponent<ElTableColumnGroup>(seq++);
                    b.AddAttribute(seq++, "Text", "销售");
                    b.AddAttribute(seq++, "ChildColumns", new RenderFragment(cb =>
                    {
                        var s = 0;
                        cb.OpenComponent<ElTableColumn>(s++);
                        cb.AddAttribute(s++, "Property", "Qty");
                        cb.AddAttribute(s++, "Text", "数量");
                        cb.AddAttribute(s++, "Sortable", true);
                        cb.CloseComponent();
                        cb.OpenComponent<ElTableColumn>(s++);
                        cb.AddAttribute(s++, "Property", "Amount");
                        cb.AddAttribute(s++, "Text", "金额");
                        cb.CloseComponent();
                    }));
                    b.CloseComponent();
                }));

            var qtyTh = HeaderCells(cut.FindAll(".el-table__header thead tr")[1]).First(th => th.TextContent.Contains("数量"));
            Assert.Contains("is-sortable", qtyTh.ClassName);

            // 正向对照：没点之前是数据原序 A,B；不写这一句的话，"空文本"也能让下面的断言看起来像通过。
            Assert.Equal(new[] { "A", "B" }, NameColumnCells(cut));

            // 排序不挂在 th 上：OnHeaderCellClickAsync 只回调 HeaderCellClick/OnHeaderClick，
            // 真正的 ToggleSortAsync 在 .caret-wrapper 上（它 stopPropagation，免得两件事都发生一次）。
            // 单行分支本来就是这个行为，这里要验的是分组分支的第二行没有把这套挂接漏掉。
            var caret = qtyTh.QuerySelector(".caret-wrapper");
            Assert.NotNull(caret);

            caret.Click();

            Assert.Equal(new[] { "B", "A" }, NameColumnCells(cut));
        }

        /// <summary>表体名称列（第一格）的文本，用来观察排序后的行序。</summary>
        private static string[] NameColumnCells(IRenderedComponent<ElTable> cut)
            => cut.FindAll(".el-table__body tbody tr td:first-child")
                .Select(td => td.TextContent.Trim()).ToArray();

        /// <summary>分组表下的选择列要跨两行（rowspan=2），否则第二行会多出一格把子列挤偏。</summary>
        [Fact]
        public void SelectionColumnSpansBothHeaderRows()
        {
            var cut = Render<ElTable>(p => p
                .Add(x => x.DataSource, Rows())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, true)
                .Add(x => x.EnablePagination, false)
                .AddChildContent(b =>
                {
                    LeafColumn("Name", "名称")(b);
                    GroupColumn("销售", ("Qty", "数量"), ("Amount", "金额"))(b);
                }));

            var rows = cut.FindAll(".el-table__header thead tr");
            Assert.Equal(2, rows.Count);
            var first = HeaderCells(rows[0]);
            // 第一行：选择列 + 名称（都是 rowspan=2 的顶层列）+ 一个分组父标题 = 3 格
            Assert.Equal(3, first.Count);
            Assert.Equal("2", first[0].GetAttribute("rowspan"));
            Assert.Equal(1, first[0].QuerySelectorAll(".el-checkbox").Count);
            Assert.Equal("2", first[1].GetAttribute("rowspan"));
            // 第二行只有分组的孩子 2 格；选择列不能在这里重复出现，否则整行会多出一格把子列挤偏。
            var second = HeaderCells(rows[1]);
            Assert.Equal(2, second.Count);
            Assert.Equal(new[] { "数量", "金额" }, second.Select(th => th.TextContent.Trim()).ToArray());
            Assert.Empty(rows[1].QuerySelectorAll(".el-checkbox"));

            // 正向对照：表体每行 = 选择格 + 3 个叶子列 = 4 格，分组父标题不占 td。
            Assert.All(cut.FindAll(".el-table__body tbody tr"), tr => Assert.Equal(4, tr.QuerySelectorAll("td").Length));
        }
    }
}
