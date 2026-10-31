using Bunit;
using ElementPlus;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElTable 选择列宽度（<see cref="ElTable.SelectionColumnWidth"/>）契约。
    ///
    /// 背景：选择列宽原先硬编码 55 写在三处（表头 colgroup、表体 colgroup、横向滚动记账），
    /// 宿主想给业务列腾宽度时收不动。批次82 把它参数化，默认 55 —— 全仓老表格渲染结果不变。
    /// 这里的约定是「三处必须同源」：表头与表体两根 col 的 width 相同，
    /// 记账（TableWidthStyle 的 max(100%,Npx)）也用同一个值，否则横向滚动边界和列宽会错位。
    /// </summary>
    public class ElTableSelectionColumnWidthTests : BunitContext
    {
        public ElTableSelectionColumnWidthTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        public class Row
        {
            public string Name { get; set; }
        }

        private static readonly Row[] Rows = { new Row { Name = "A" }, new Row { Name = "B" } };

        private IRenderedComponent<ElTable> RenderTable(int selectionWidth)
        {
            return Render<ElTable>(p => p
                .Add(x => x.DataSource, Rows)
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, true)
                .Add(x => x.EnablePagination, false)
                .Add(x => x.SelectionColumnWidth, selectionWidth)
                .AddChildContent(b =>
                {
                    b.OpenComponent<ElTableColumn>(0);
                    b.AddAttribute(1, "Property", "Name");
                    b.AddAttribute(2, "Text", "名称");
                    b.CloseComponent();
                }));
        }

        /// <summary>colgroup 里选择列那根 col 的 width（表头与表体各一根）。</summary>
        private static string[] SelectionColWidths(IRenderedComponent<ElTable> cut)
            => cut.FindAll("colgroup col[name='el-table_1_column_0']")
                .Select(c => c.GetAttribute("width"))
                .ToArray();

        /// <summary>TableWidthStyle 里 max(100%,Npx) 的 N——横向滚动记账用的总宽。</summary>
        private static int AccountingTotal(IRenderedComponent<ElTable> cut)
        {
            // el-table__header 是 table 元素自身的 class（不是包裹层），样式就挂在这张表上。
            var style = cut.Find("table.el-table__header").GetAttribute("style");
            return int.Parse(Regex.Match(style, @"max\(100%,(\d+)px\)").Groups[1].Value);
        }

        /// <summary>默认 55：不传参数的老表格，col 与记账都还是原值（零回归契约）。</summary>
        [Fact]
        public void DefaultIsFiftyFiveInBothColgroupsAndAccounting()
        {
            var cut = RenderTable(55);

            // 表头、表体两根 col 都在且同值；无 Width 的名称列按 DefaultColumnWidth(80) 记账。
            Assert.Equal(new[] { "55", "55" }, SelectionColWidths(cut));
            Assert.Equal(55 + 80, AccountingTotal(cut));
        }

        /// <summary>收窄到 40：两根 col 与记账同步变——三处不同源就会在这里裂开。</summary>
        [Fact]
        public void CustomWidthPropagatesToBothColgroupsAndAccounting()
        {
            var cut = RenderTable(40);

            Assert.Equal(new[] { "40", "40" }, SelectionColWidths(cut));
            Assert.Equal(40 + 80, AccountingTotal(cut));
        }

        /// <summary>关掉选择列：col 不渲染、记账也不计选择列（正向对照：不是「恰好等于列宽」碰对的）。</summary>
        [Fact]
        public void NoSelectionColumnExcludesItFromAccounting()
        {
            var cut = Render<ElTable>(p => p
                .Add(x => x.DataSource, Rows)
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.EnablePagination, false)
                .Add(x => x.SelectionColumnWidth, 40)
                .AddChildContent(b =>
                {
                    b.OpenComponent<ElTableColumn>(0);
                    b.AddAttribute(1, "Property", "Name");
                    b.AddAttribute(2, "Text", "名称");
                    b.CloseComponent();
                }));

            Assert.Empty(cut.FindAll("colgroup col[name='el-table_1_column_0']"));
            Assert.Equal(80, AccountingTotal(cut));
        }
    }
}
