using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElTable 的表头标记契约与样式参数渲染契约。
    ///
    /// 表头标记按 EP 2.14.0 实抓对齐：packages/components/table/src/table-header/index.ts:267-291 ——
    /// .cell 的子项先是 column.label（直接文本），随后才是只装两个 sort-caret 的 caret-wrapper。
    /// 本仓旧写法把 label 放进了 caret-wrapper 里，而这个盒在 CSS 里是固定宽度
    /// （wwwroot/css/index.css 的 <c>.el-table .caret-wrapper</c>；高度改前是打包段的 element-ui 遗留 34px，
    /// 批次⑧已按 EP 改回 14px，见 <c>Guard_SortCaretBoxMatchesEpGeometry</c>），
    /// 窄列表头于是被挤成"一字一行"，再被 .cell{overflow:hidden} 裁掉。
    ///
    /// 样式这组是另一个坑：Blazor 的 <c>style="@dict"</c> 会把 Dictionary.ToString() 原样写进 DOM
    /// （实测 th 的 style 值为 "System.Collections.Generic.Dictionary`2[System.String,System.String]"），
    /// 浏览器把整条声明当非法 CSS 丢弃 —— 按字典传的 RowStyle / HeaderRowStyle / HeaderCellStyle /
    /// CellStyle 于是全部不生效，而且把垃圾串留在属性里。
    /// </summary>
    public class ElTableHeaderCellTests : BunitContext
    {
        public ElTableHeaderCellTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        public class Row
        {
            public string Name { get; set; }
            public int Stock { get; set; }
        }

        private static List<Row> Rows() => new()
        {
            new Row { Name = "A", Stock = 300 },
            new Row { Name = "B", Stock = 10 },
        };

        private IRenderedComponent<ElTable> RenderTable(
            bool sortable = true,
            Func<object, int, Dictionary<string, string>> headerRowStyle = null,
            Func<object, TableHeader, int, int, Dictionary<string, string>> headerCellStyle = null,
            Func<object, int, Dictionary<string, string>> rowStyle = null)
        {
            var child = new RenderFragment(b =>
            {
                var seq = 0;
                b.OpenComponent<ElTableColumn>(seq++);
                b.AddAttribute(seq++, "Property", "Name");
                b.AddAttribute(seq++, "Text", "名称");
                b.CloseComponent();

                b.OpenComponent<ElTableColumn>(seq++);
                b.AddAttribute(seq++, "Property", "Stock");
                b.AddAttribute(seq++, "Text", "库存（可排序）");
                b.AddAttribute(seq++, "Sortable", sortable);
                b.CloseComponent();
            });

            return Render<ElTable>(p => p
                .Add(x => x.DataSource, Rows())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.EnablePagination, false)
                .Add(x => x.HeaderRowStyle, headerRowStyle)
                .Add(x => x.HeaderCellStyle, headerCellStyle)
                .Add(x => x.RowStyle, rowStyle)
                .AddChildContent(child));
        }

        /// <summary>可排序列的表头文字必须是 .cell 的直接子文本，caret-wrapper 里只剩两个箭头。</summary>
        [Fact]
        public void SortableHeaderKeepsLabelOutOfCaretWrapper()
        {
            var cut = RenderTable();

            var caret = cut.Find(".caret-wrapper");
            var cell = caret.Closest(".cell");

            Assert.Matches(new Regex(@"^\s*库存"), cell.InnerHtml);
            Assert.False(Regex.IsMatch(caret.TextContent ?? string.Empty, @"\S"),
                "caret-wrapper 里不该有文字，实测：" + caret.TextContent);
            Assert.Equal(2, caret.QuerySelectorAll(".sort-caret").Length);
        }

        /// <summary>不排序的列同样把文字直接放在 .cell 里（两条分支渲染位置一致）。</summary>
        [Fact]
        public void PlainHeaderRendersLabelAsCellText()
        {
            var cut = RenderTable(sortable: false);

            var nameTh = cut.FindAll(".el-table__header th").First(th => th.TextContent.Contains("名称"));
            Assert.Matches(new Regex(@"^\s*名称"), nameTh.QuerySelector(".cell").InnerHtml);
            Assert.Empty(cut.FindAll(".caret-wrapper"));
        }

        /// <summary>HeaderRowStyle 给字典要渲染成 CSS 声明，不能把字典本身写进属性。</summary>
        [Fact]
        public void HeaderRowStyleDictionaryRendersAsCssDeclarations()
        {
            var cut = RenderTable(headerRowStyle: (row, index) =>
                new Dictionary<string, string> { { "box-shadow", "inset 0 -3px 0 #409eff" } });

            var tr = cut.Find(".el-table__header tr");
            Assert.Equal("box-shadow:inset 0 -3px 0 #409eff", tr.GetAttribute("style"));
            Assert.DoesNotContain("System.Collections", cut.Markup);
        }

        /// <summary>HeaderCellStyle 给字典要落到该列的 th 上（按属性名认列，不靠表头文字）。</summary>
        [Fact]
        public void HeaderCellStyleDictionaryRendersIntoThatColumn()
        {
            var cut = RenderTable(headerCellStyle: (row, header, rowIndex, columnIndex) =>
                header?.Property?.Name == "Name"
                    ? new Dictionary<string, string> { { "background", "#fff7e6" }, { "color", "#d9480f" } }
                    : null);

            var nameTh = cut.FindAll(".el-table__header th").First(th => th.TextContent.Contains("名称"));
            var style = nameTh.GetAttribute("style") ?? string.Empty;
            Assert.Contains("background:#fff7e6", style);
            Assert.Contains("color:#d9480f", style);

            var stockTh = cut.FindAll(".el-table__header th").First(th => th.TextContent.Contains("库存"));
            Assert.Null(stockTh.GetAttribute("style"));
        }

        /// <summary>RowStyle 给字典要落到表体行上；返回空字典则不渲染 style 属性。</summary>
        [Fact]
        public void RowStyleDictionaryRendersIntoBodyRows()
        {
            var cut = RenderTable(rowStyle: (row, index) => new Dictionary<string, string> { { "color", "#f00" } });
            Assert.Equal("color:#f00", cut.Find(".el-table__body tbody tr").GetAttribute("style"));

            var empty = RenderTable(rowStyle: (row, index) => new Dictionary<string, string>());
            Assert.Null(empty.Find(".el-table__body tbody tr").GetAttribute("style"));
        }

        /// <summary>搭一列（可给 Width / MinWidth / Sortable / 筛选），返回表头与表体该列的 &lt;col width&gt;。</summary>
        private (string Header, string Body, string TableStyle) RenderOneColumn(
            string width = null,
            string minWidth = null,
            bool sortable = false,
            bool advancedFilter = false,
            bool withFilters = false)
        {
            var child = new RenderFragment(b =>
            {
                var seq = 0;
                b.OpenComponent<ElTableColumn>(seq++);
                b.AddAttribute(seq++, "Property", "Name");
                b.AddAttribute(seq++, "Text", "名称");
                if (width != null)
                {
                    b.AddAttribute(seq++, "Width", width);
                }
                if (minWidth != null)
                {
                    b.AddAttribute(seq++, "MinWidth", minWidth);
                }
                if (sortable)
                {
                    b.AddAttribute(seq++, "Sortable", true);
                }
                if (advancedFilter)
                {
                    b.AddAttribute(seq++, "AdvancedFilter", true);
                }
                if (withFilters)
                {
                    b.AddAttribute(seq++, "Filters", new List<TableFilterOption>
                    {
                        new TableFilterOption { Text = "甲", Value = "甲" },
                    });
                }
                b.CloseComponent();
            });

            var cut = Render<ElTable>(p => p
                .Add(x => x.DataSource, Rows())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.EnablePagination, false)
                .AddChildContent(child));

            var headerCol = cut.Find(".el-table__header colgroup col[name='el-table_1_column_1']");
            var bodyCol = cut.Find(".el-table__body colgroup col[name='el-table_1_column_1']");
            return (
                headerCol.GetAttribute("width"),
                bodyCol.GetAttribute("width"),
                cut.Find(".el-table__header").GetAttribute("style"));
        }

        /// <summary>
        /// 没有图标盒的列：&lt;col width&gt; 就是声明值，表头表体同源。
        /// 这条是加量的对照组 —— 加量算错时它也会跟着变，能立刻看出偏移。
        /// </summary>
        [Fact]
        public void PlainColumnColWidthIsDeclaredValue()
        {
            var (header, body, tableStyle) = RenderOneColumn(width: "110");

            Assert.Equal("110", header);
            Assert.Equal(header, body);
            Assert.Contains("max(100%,110px)", tableStyle);
        }

        /// <summary>排序命中盒（EP <c>.caret-wrapper{width:24px}</c>）在列宽之外另算。</summary>
        [Fact]
        public void SortableColumnAddsCaretBoxOutsideDeclaredWidth()
        {
            var (header, body, tableStyle) = RenderOneColumn(width: "110", sortable: true);

            Assert.Equal("134", header);
            Assert.Equal(header, body);
            // 求和口径同步抬升：表格靠横向滚动让列保持可读，而不是把列压窄
            Assert.Contains("max(100%,134px)", tableStyle);
        }

        /// <summary>筛选漏斗（图标 12px + <c>margin-left:6px</c>）同样另算，条件面板与选项面板一致。</summary>
        [Fact]
        public void FilterableColumnAddsTriggerBoxOutsideDeclaredWidth()
        {
            var advanced = RenderOneColumn(width: "110", advancedFilter: true);
            Assert.Equal("128", advanced.Header);
            Assert.Equal(advanced.Header, advanced.Body);

            var options = RenderOneColumn(width: "110", withFilters: true);
            Assert.Equal("128", options.Header);

            var none = RenderOneColumn(width: "110", withFilters: false);
            Assert.Equal("110", none.Header);
        }

        /// <summary>排序 + 筛选两个盒同时存在时都算：110 + 24 + 18。</summary>
        [Fact]
        public void SortableAndFilterableColumnAddsBothBoxes()
        {
            var (header, body, _) = RenderOneColumn(width: "110", sortable: true, advancedFilter: true);

            Assert.Equal("152", header);
            Assert.Equal(header, body);
        }

        /// <summary>MinWidth 兜底（去掉 px 后缀取数）也按同一口径加量：90 + 排序盒 24。</summary>
        [Fact]
        public void MinWidthColumnGetsAllowance()
        {
            Assert.Equal("114", RenderOneColumn(minWidth: "90px", sortable: true).Header);
        }

        /// <summary>
        /// 批次㊸：既没给 <c>Width</c> 也没给 <c>MinWidth</c> 的列是 <b>auto 列</b> ——
        /// 表头与表体的 <c>&lt;col&gt;</c> 一律不写 width 属性，<c>table-layout:fixed</c> 会把表格的
        /// 剩余宽度摊给它，这就是 Element Plus 里不写 width 的那一列的行为。
        /// 改前这里兜底成 <c>width="80"</c>，于是「声明列宽之和 &lt; 表格宽度」时富余被按比例摊给每一列，
        /// 钉死的宽度一个都保不住（zyweb 部门维护表 160/160/200/60 在 1106px 容器里摊成 305/305/381/114，
        /// 操作列怎么调都调不窄）。
        /// </summary>
        [Fact]
        public void ColumnWithoutWidthEmitsAutoCol()
        {
            var (header, body, _) = RenderOneColumn();

            Assert.Null(header);
            Assert.Null(body);
        }

        /// <summary>
        /// auto 列的可读性下限换由表格最小宽度兜：80px（外加该列的表头图标盒）照常进 <c>max(100%,Σ)</c> 求和，
        /// 所以窄容器仍是横向滚动而不是把列压成竖排字母 —— 批次⑦ 那条回归不会回来。
        /// </summary>
        [Fact]
        public void AutoColumnStillCountsFloorInTableWidth()
        {
            Assert.Contains("max(100%,80px)", RenderOneColumn().TableStyle);
            Assert.Contains("max(100%,104px)", RenderOneColumn(sortable: true).TableStyle);
            Assert.Contains("max(100%,98px)", RenderOneColumn(advancedFilter: true).TableStyle);
        }

        /// <summary>
        /// 宿主普遍把列宽写成 <c>Width="160px"</c>。改前 <c>ColumnsTotalWidth</c> 用 <c>int.TryParse</c>
        /// 折像素，带后缀就解析失败、静默按 80 记账，整张表的 <c>max(100%,Σ)</c> 因此失真
        /// （zyweb 部门维护表四列共 580px 被记成 320px）。批次㊸ 剥掉 <c>px</c> 后缀再算；
        /// <c>&lt;col width&gt;</c> 本身保持宿主写的原样，不动它的字面值。
        /// </summary>
        [Fact]
        public void PxSuffixWidthCountsItsRealPixels()
        {
            var (header, body, tableStyle) = RenderOneColumn(width: "160px");

            Assert.Equal("160px", header);
            Assert.Equal(header, body);
            Assert.Contains("max(100%,160px)", tableStyle);
        }

        /// <summary>
        /// 百分比列宽不加数字：<c>&lt;col width="50% + 24px"&gt;</c> 是非法属性值，浏览器会整条丢掉
        /// 这一列的宽度。百分比列保持声明值，宽度由 <c>table-layout</c> 自己分配。
        /// </summary>
        [Fact]
        public void PercentColumnWidthIsPassedThroughUnchanged()
        {
            var (header, body, _) = RenderOneColumn(width: "30%", sortable: true);

            Assert.Equal("30%", header);
            Assert.Equal(header, body);
        }

        #region 批次⑧：排序箭头盒几何（EP 2.14.0 口径）

        /// <summary>
        /// 排序箭头盒的 CSS 文本契约。bUnit 没有布局引擎，量不到命中盒矩形，
        /// 所以钉住「手写段必须自己把几何写全」这一条：
        /// EP 2.14.0 theme-chalk/src/table.scss:515-544 是 盒 24×14 + 两个 absolute 三角形
        /// 共用 left:7px（ascending top:-5px / descending bottom:-3px）。
        /// 改前手写段只写了颜色与 display，几何由打包压缩段里的 element-ui 遗留值决定
        /// （盒 24×34 → 可排序列的表头行被撑到 51px），再叠上手写的
        /// margin-left:4px / -5px → 两个三角形横向错开。浏览器实测见 Temp/verify-batch8.log 的 C 段。
        /// </summary>
        [Fact]
        public void Guard_SortCaretBoxMatchesEpGeometry()
        {
            var css = CaretCssSection();

            Assert.Matches(@"\.el-table \.caret-wrapper\s*\{[^}]*height:\s*14px", css);
            Assert.Matches(@"\.el-table \.caret-wrapper\s*\{[^}]*width:\s*24px", css);
            Assert.Matches(@"\.el-table \.caret-wrapper\s*\{[^}]*flex-direction:\s*column", css);
            Assert.Matches(@"\.el-table \.sort-caret\s*\{[^}]*position:\s*absolute", css);
            Assert.Matches(@"\.el-table \.sort-caret\s*\{[^}]*left:\s*7px", css);
            Assert.Matches(@"\.el-table \.sort-caret\.ascending\s*\{[^}]*top:\s*-5px", css);
            Assert.Matches(@"\.el-table \.sort-caret\.descending\s*\{[^}]*bottom:\s*-3px", css);

            // 正向对照：上面 7 条能同时成立，就证明这段确实解析到了这些声明
            // （同一段里没写 height 时第 1 条必红），下面的负向断言因此不是空转的绿。
            Assert.DoesNotMatch(@"\.el-table \.sort-caret[^.]*\{[^}]*margin-left", css);
        }

        /// <summary>取手写段里从「排序箭头盒」注释到 .sort-caret.descending 规则结束的片段（已剥注释）。</summary>
        private static string CaretCssSection()
        {
            const string marker = "排序箭头盒：对齐 EP 2.14.0";
            const string tail = ".el-table .sort-caret.descending";

            var path = Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "css", "index.css");
            var text = File.ReadAllText(path);
            var at = text.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(at >= 0, "index.css 中未找到排序箭头盒样式段");

            var start = text.LastIndexOf("/*", at, StringComparison.Ordinal);
            var end = text.IndexOf(tail, at, StringComparison.Ordinal);
            Assert.True(start >= 0 && end > start, "排序箭头盒样式段不完整");
            var open = text.IndexOf('{', end);
            var stop = open >= 0 ? text.IndexOf('}', open) : -1;
            Assert.True(stop > end, "排序箭头盒样式段结尾规则没有闭合");

            return Regex.Replace(text.Substring(start, stop - start + 1), @"/\*[\s\S]*?\*/", string.Empty);
        }

        private static string RepoRoot([CallerFilePath] string file = null)
            => new FileInfo(file).Directory.Parent.Parent.FullName;

        #endregion
    }
}
