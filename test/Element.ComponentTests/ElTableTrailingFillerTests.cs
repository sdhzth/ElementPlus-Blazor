using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElTable 尾部填充列（<see cref="ElTable.TrailingFiller"/>）契约（批次91，2026-10-21）。
    ///
    /// 背景：<c>table-layout:fixed</c> 只有两种吃富余的办法 —— 有一列不写宽度（它独吞），
    /// 或每列都写（富余按声明比例摊回每一列，写多少都不算数）。宿主此前只能二选一，
    /// 于是 <c>/dictionary/docelement</c> 的「编辑时间」被摊成 634px（内容只需 91.6）。
    /// 本参数把「独吞」交给一根没有内容的尾列，数据列一律精确等于声明值。
    ///
    /// 这里钉住的约定：① 默认关 ⇒ 一个字节都不改既有渲染（auto 列仍不带宽度、尾列不存在）；
    /// ② 尾列的 <c>&lt;col&gt;</c> 必须**不带**宽度且表头/表体各一根，顺序在数据列之后；
    /// ③ 开了尾列后 auto 数据列必须换成按记账钉死（80 + 图标盒），否则富余被两列均分；
    /// ④ 尾列不进 <c>Headers</c> ⇒ 表宽记账 <c>max(100%,Σ)</c> 与关着时同一个数；
    /// ⑤ 表头尾格带 <c>is-leaf</c> —— 本仓表头那条下划线只挂在 <c>th.is-leaf</c> 上，漏了它横线会断。
    /// </summary>
    public class ElTableTrailingFillerTests : BunitContext
    {
        public ElTableTrailingFillerTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        public class Row
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public int Qty { get; set; }
        }

        private static List<Row> Rows() => new()
        {
            new Row { Name = "A", Code = "01", Qty = 3 },
            new Row { Name = "B", Code = "02", Qty = 1 },
        };

        private static RenderFragment Column(string property, string text, string width = null, bool sortable = false) => b =>
        {
            var seq = 0;
            b.OpenComponent<ElTableColumn>(seq++);
            b.AddAttribute(seq++, "Property", property);
            b.AddAttribute(seq++, "Text", text);
            if (width != null)
            {
                b.AddAttribute(seq++, "Width", width);
            }
            if (sortable)
            {
                b.AddAttribute(seq++, "Sortable", true);
            }
            b.CloseComponent();
        };

        private static RenderFragment Group(string text, params (string Property, string Text)[] children) => b =>
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
                    cb.AddAttribute(s++, "Width", "120");
                    cb.CloseComponent();
                }
            }));
            b.CloseComponent();
        };

        private IRenderedComponent<ElTable> RenderTable(bool trailingFiller, params RenderFragment[] columns)
            => Render<ElTable>(p => p
                .Add(x => x.DataSource, Rows())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.EnablePagination, false)
                .Add(x => x.TrailingFiller, trailingFiller)
                .AddChildContent(b =>
                {
                    foreach (var column in columns)
                    {
                        column(b);
                    }
                }));

        private static string RepoRoot([CallerFilePath] string file = null)
        {
            // .../test/Element.ComponentTests/ElTableTrailingFillerTests.cs → 上溯三层
            var directory = new FileInfo(file).Directory;

            return directory.Parent.Parent.FullName;
        }

        /// <summary>两根 colgroup 里尾列那根 <c>&lt;col&gt;</c>（表头一张、表体一张）。</summary>
        private static IReadOnlyList<AngleSharp.Dom.IElement> FillerCols(IRenderedComponent<ElTable> cut)
            => cut.FindAll("colgroup col[name='el-table_1_column_filler']");

        /// <summary>按 <c>&lt;col&gt;</c> 的 name 取它的 width 属性（没写属性 ⇒ null）。</summary>
        private static string ColWidth(IRenderedComponent<ElTable> cut, string name)
            => cut.Find($"colgroup col[name='{name}']").GetAttribute("width");

        /// <summary><c>TableWidthStyle</c> 里 <c>max(100%,Npx)</c> 的 N —— 横向滚动记账用的总宽。</summary>
        private static string TableWidthStyle(IRenderedComponent<ElTable> cut)
            => cut.Find("table.el-table__header").GetAttribute("style");

        /// <summary>默认关：尾列不存在，不写宽度的列仍是真 auto（零回归契约）。</summary>
        [Fact]
        public void OffByDefaultKeepsAutoColumnAndEmitsNoFiller()
        {
            var cut = RenderTable(false, Column("Name", "名称"), Column("Code", "编码", "120"));

            Assert.Empty(FillerCols(cut));
            Assert.Empty(cut.FindAll(".el-table-column--filler"));
            Assert.Null(ColWidth(cut, "el-table_1_column_1"));
            Assert.Equal("120", ColWidth(cut, "el-table_1_column_2"));
        }

        /// <summary>尾列两根 col 都在、都不带宽度（带了就抢不回富余），且排在数据列之后。</summary>
        [Fact]
        public void FillerColIsUnpinnedAndPresentInBothColgroups()
        {
            var cut = RenderTable(true, Column("Name", "名称", "200"), Column("Code", "编码", "120"));
            var cols = FillerCols(cut);

            Assert.Equal(2, cols.Count);
            Assert.All(cols, c => Assert.Null(c.GetAttribute("width")));

            // 顺序：数据列在前，尾列在后（对不上列序整行会左移一格）。
            // 表头那张 table 的 colgroup 末尾还有一根 gutter col，所以尾列是倒数第二根；表体那张没有 gutter。
            var headerCols = cut.FindAll("table.el-table__header colgroup col")
                .Select(c => c.GetAttribute("name")).ToArray();
            var bodyCols = cut.FindAll("table.el-table__body colgroup col")
                .Select(c => c.GetAttribute("name")).ToArray();
            Assert.Equal("el-table_1_column_filler", headerCols[^2]);
            Assert.Equal("gutter", headerCols[^1]);
            Assert.Equal("el-table_1_column_filler", bodyCols[^1]);
        }

        /// <summary>开了尾列后 auto 列按记账钉死 80；带排序的钉 80+24，和表宽求和同一口径。</summary>
        [Fact]
        public void AutoColumnsArePinnedToTheAccountingFloor()
        {
            var pinned = RenderTable(true, Column("Name", "名称"), Column("Code", "编码", "120"));
            Assert.Equal("80", ColWidth(pinned, "el-table_1_column_1"));

            var sortable = RenderTable(true, Column("Name", "名称", sortable: true), Column("Code", "编码", "120"));
            Assert.Equal("104", ColWidth(sortable, "el-table_1_column_1"));

            // 正向对照（同一条断言在关着时必须读到 null，否则上面只是「恰好等于某个数」碰对）
            var off = RenderTable(false, Column("Name", "名称", sortable: true), Column("Code", "编码", "120"));
            Assert.Null(ColWidth(off, "el-table_1_column_1"));
        }

        /// <summary>表头一格、表体每行一格，格子里没有内容。</summary>
        [Fact]
        public void FillerCellsAreAddedToHeaderAndEveryBodyRow()
        {
            var cut = RenderTable(true, Column("Name", "名称", "200"), Column("Code", "编码", "120"));

            Assert.Single(cut.FindAll(".el-table__header th.el-table-column--filler"));
            Assert.Equal(Rows().Count, cut.FindAll(".el-table__body td.el-table-column--filler").Count);
            Assert.All(cut.FindAll(".el-table-column--filler"), c => Assert.Equal(string.Empty, c.TextContent.Trim()));
        }

        /// <summary>按空格拆 class，避免 <c>is-leaf</c> 被子串命中（这里要的是整枚 token）。</summary>
        private static string[] Classes(AngleSharp.Dom.IElement element)
            => (element.GetAttribute("class") ?? string.Empty).Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

        /// <summary>
        /// 表头尾格必须挂 <c>is-leaf</c>：本仓表头那条 1px 下划线只写在 <c>th.is-leaf</c> 上
        /// （<c>td.el-table__cell</c> 自己有一条，所以尾格 td 不会犯同一个错）。
        /// 宿主实测漏它的症状是横线在 1125 处断掉、右边 454px 一片没有底线。
        /// </summary>
        [Fact]
        public void FillerHeaderCellCarriesTheLeafClassThatDrawsTheHairline()
        {
            var cut = RenderTable(true, Column("Name", "名称", "200"), Column("Code", "编码", "120"));
            var th = cut.Find(".el-table__header th.el-table-column--filler");

            Assert.Contains("is-leaf", Classes(th));
            Assert.Contains("el-table__cell", Classes(th));

            // 正向对照：表体尾格本来就带 el-table__cell，下划线由那条 td 规则给，不该依赖 is-leaf。
            var td = cut.Find(".el-table__body td.el-table-column--filler");
            Assert.Contains("el-table__cell", Classes(td));
        }

        /// <summary>分组表头：尾格挂第一行、rowspan=2（第二行补格会和 colgroup 错位），同样要 is-leaf。</summary>
        [Fact]
        public void GroupedHeaderFillerSpansBothRowsAndIsLeaf()
        {
            var cut = RenderTable(true, Group("数量", ("Qty", "数量")), Column("Code", "编码", "120"));
            var th = cut.Find(".el-table__header th.el-table-column--filler");

            Assert.Equal("2", th.GetAttribute("rowspan"));
            Assert.Contains("is-leaf", Classes(th));
        }

        /// <summary>
        /// 尾列不是 <c>Headers</c> 里的一项 ⇒ 表宽记账不变：开着/关着两版 <c>max(100%,Σ)</c> 必须是同一个数。
        /// 名称列开着被钉成 80、关着按 80 记账，所以这一条同时钉住「钉死值 = 记账值」。
        /// </summary>
        [Fact]
        public void TableWidthAccountingIsIdenticalWithAndWithoutFiller()
        {
            var off = TableWidthStyle(RenderTable(false, Column("Name", "名称"), Column("Code", "编码", "120")));
            var on = TableWidthStyle(RenderTable(true, Column("Name", "名称"), Column("Code", "编码", "120")));

            Assert.Equal(off, on);
            Assert.Matches(@"max\(100%,200px\)", on);
        }

        /// <summary>
        /// 整表一列都没声明宽度时不开尾列：那种表本来每列均分富余，插尾列只会把「均分 N 份」
        /// 变成「均分 N+1 份」，把每一列都挤窄。这是本参数的「不许误报」夹具。
        /// </summary>
        [Fact]
        public void AllAutoTableGetsNoFillerEvenWhenRequested()
        {
            var cut = RenderTable(true, Column("Name", "名称"), Column("Code", "编码"));

            Assert.Empty(FillerCols(cut));
            Assert.Empty(cut.FindAll(".el-table-column--filler"));
            Assert.Null(ColWidth(cut, "el-table_1_column_1"));
        }

        /// <summary>MinWidth 一档也算「声明了宽度」，同样触发尾列（判据与 <see cref="UsesTrailingFiller"/> 同源）。</summary>
        [Fact]
        public void MinWidthDeclarationActivatesTheFiller()
        {
            var b = Render<ElTable>(p => p
                .Add(x => x.DataSource, Rows())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.EnablePagination, false)
                .Add(x => x.TrailingFiller, true)
                .AddChildContent(b2 =>
                {
                    b2.OpenComponent<ElTableColumn>(0);
                    b2.AddAttribute(1, "Property", "Name");
                    b2.AddAttribute(2, "Text", "名称");
                    b2.AddAttribute(3, "MinWidth", "150px");
                    b2.CloseComponent();
                }));

            Assert.Equal(2, FillerCols(b).Count);
        }

        /// <summary>
        /// CSS 侧同源约定（bUnit 不加载样式表，只能读源码钉）：
        /// ① 尾格内衬/右边框归零 —— 窄容器（Σ&gt;容器）时它塌成 0 宽，留着内衬会把每行撑出十几个像素、
        /// 凭空多一条滚不动也看不见的滚动条；② 归零的那条必须写到 <c>(0,4,1)</c> 才压得住
        /// vendor 老主题的 <c>.el-table th&gt;.cell{padding-left:10px}</c>；③ 下划线仍挂在 <c>th.is-leaf</c>。
        /// </summary>
        [Fact]
        public void CssZeroesTheFillerPaddingAndStillDrawsTheLeafHairline()
        {
            var css = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "css", "index.css"));
            var rule = "\n.el-table td.el-table__cell.el-table-column--filler,\n.el-table th.el-table__cell.el-table-column--filler {";
            var index = css.IndexOf(rule, System.StringComparison.Ordinal);

            Assert.True(index >= 0, "index.css 中未找到尾列归零规则（选择器必须整两行、行首对齐）");

            var body = css.Substring(index, css.IndexOf('}', index) - index);
            Assert.Contains("border-right-width: 0", body);
            Assert.Contains("padding-left: 0", body);
            Assert.Contains("padding-right: 0", body);

            Assert.Contains(".el-table th.el-table__cell.el-table-column--filler > .cell {", css);
            Assert.Matches(@"\.el-table th\.is-leaf,[\s\S]{0,200}border-bottom: var\(--el-table-border\)", css);
        }
    }
}
