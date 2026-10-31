using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// <c>ElTableColumn.Formatter</c> 的接线（EP 2.14.0 <c>table/src/config.ts:193-208</c>）。
    /// 这批之前它只是被抄进 <see cref="TableHeader"/> 再没人调用 ⇒ 数值列一律裸 <c>ToString()</c>。
    /// <para>
    /// 判据一律走 DOM（AngleSharp 查询 / <c>TextContent</c>），不比对 <c>cut.Markup</c> 子串：
    /// Blazor 调试构建会在节点之间插入 <c>&lt;!--!--&gt;</c> 序列注释，子串比对会假红。
    /// </para>
    /// </summary>
    public class ElTableFormatterColumnTests : BunitContext
    {
        public ElTableFormatterColumnTests()
        {
            Services.AddElementPlusServices();
            Services.AddSingleton(new HttpClient());
        }

        public class Row
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public decimal Amount { get; set; }
            public List<string> Tags { get; set; }
        }

        private static List<Row> Rows()
        {
            return new List<Row>
            {
                new Row { Name = "Alpha", Age = 21, Amount = 1234.5m, Tags = new List<string> { "a", "b" } },
                new Row { Name = "Bravo", Age = 22, Amount = 6789.0m, Tags = new List<string> { "c" } },
                new Row { Name = null, Age = 23, Amount = 0m, Tags = new List<string>() },
            };
        }

        /// <summary>一列的调用台账。ElTable 在 bUnit 里一轮渲染建两遍呈现树（实测 RenderCount=2），
        /// 所以调用次数按「格数 × 渲染轮数」记，不按「格数」记。</summary>
        private sealed class Ledger
        {
            public int Calls;
            public readonly List<object> Rows = new List<object>();
            public readonly List<TableHeader> Columns = new List<TableHeader>();
            public readonly List<object> Values = new List<object>();
            public readonly List<int> Indexes = new List<int>();

            /// <summary>按列标题分组的原值（多列表里逐列断言参数是什么类型）。</summary>
            public List<object> ValuesOf(string columnText)
                => Columns.Select((c, i) => (c.Text, Values[i]))
                    .Where(x => x.Text == columnText)
                    .Select(x => x.Item2)
                    .ToList();
        }

        private static Func<object, object, object, int, object> Counting(Ledger ledger, Func<object, object, object, int, object> inner)
        {
            return (row, column, value, index) =>
            {
                ledger.Calls++;
                ledger.Rows.Add(row);
                ledger.Columns.Add((TableHeader)column);
                ledger.Values.Add(value);
                ledger.Indexes.Add(index);
                return inner != null ? inner(row, column, value, index) : null;
            };
        }

        private IRenderedComponent<ElTable> RenderTable(
            IEnumerable<Action<Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder>> columns,
            bool enableMarkupString = false,
            bool pagination = false,
            int currentPage = 1,
            List<Row> dataSource = null)
        {
            var data = dataSource ?? Rows();
            var fragment = new RenderFragment(child =>
            {
                foreach (var column in columns)
                {
                    column(child);
                }
            });

            return Render<ElTable>(parameters => parameters
                .Add(x => x.DataSource, data)
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.EnableMarkupString, enableMarkupString)
                .Add(x => x.EnablePagination, pagination)
                .Add(x => x.PageSize, 2)
                .Add(x => x.CurrentPage, currentPage)
                .AddChildContent(fragment));
        }

        private static Action<Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder> PlainColumn(
            string property, string text, Func<object, object, object, int, object> formatter = null)
        {
            return child =>
            {
                var seq = 0;
                child.OpenComponent<ElTableColumn>(seq++);
                child.AddAttribute(seq++, "Property", property);
                child.AddAttribute(seq++, "Text", text);
                if (formatter != null)
                {
                    child.AddAttribute(seq++, "Formatter", formatter);
                }
                child.CloseComponent();
            };
        }

        private static Action<Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder> TemplateColumn(
            string property, string text, Func<object, object, object, int, object> formatter)
        {
            return child =>
            {
                var seq = 0;
                child.OpenComponent<ElTableColumn>(seq++);
                child.AddAttribute(seq++, "Property", property);
                child.AddAttribute(seq++, "Text", text);
                if (formatter != null)
                {
                    child.AddAttribute(seq++, "Formatter", formatter);
                }
                child.AddAttribute(seq++, "ChildContent",
                    (RenderFragment<object>)(row => inner =>
                    {
                        var innerSeq = 0;
                        inner.OpenElement(innerSeq++, "em");
                        inner.AddContent(innerSeq++, "slot:" + ((Row)row).Name);
                        inner.CloseElement();
                    }));
                child.CloseComponent();
            };
        }

        private static Action<Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder> IndexColumn(
            string text, Func<object, object, object, int, object> formatter)
        {
            return child =>
            {
                var seq = 0;
                child.OpenComponent<ElTableColumn>(seq++);
                child.AddAttribute(seq++, "Type", "index");
                child.AddAttribute(seq++, "Text", text);
                if (formatter != null)
                {
                    child.AddAttribute(seq++, "Formatter", formatter);
                }
                child.CloseComponent();
            };
        }

        /// <summary>按列序号取 tbody 每行那一格的文本（第 0 列 = 第一个注册的列）。</summary>
        private static List<string> CellText(IRenderedComponent<ElTable> cut, int columnOrdinal)
        {
            return cut.FindAll("tbody tr.el-table__row")
                .Select(tr => tr.QuerySelectorAll("td")[columnOrdinal]?.QuerySelector(".cell")?.TextContent?.Trim() ?? "")
                .ToList();
        }

        /// <summary>按列序号取 tbody 每行那一格里的 HTML（元素存在 ⇨ 真的按标记语言渲染了）。</summary>
        private static List<AngleSharp.Dom.IElement> CellHtml(IRenderedComponent<ElTable> cut, int columnOrdinal, string selector)
        {
            return cut.FindAll("tbody tr.el-table__row")
                .Select(tr => tr.QuerySelectorAll("td")[columnOrdinal]?.QuerySelector(".cell " + selector))
                .Where(node => node != null)
                .Cast<AngleSharp.Dom.IElement>()
                .ToList();
        }

        [Fact]
        public void Formatter_ReplacesDefaultText()
        {
            var cut = RenderTable(new[]
            {
                PlainColumn("Name", "名称", (row, column, value, index) => "#" + value),
            });

            // 改前这一档是裸 ToString()（Alpha/Bravo/空），接线后整体替换
            Assert.Equal(new[] { "#Alpha", "#Bravo", "#" }, CellText(cut, 0));
        }

        [Fact]
        public void Formatter_ReceivesRawValueNotRenderedText()
        {
            var ledger = new Ledger();
            var cut = RenderTable(new[]
            {
                PlainColumn("Age", "年龄", Counting(ledger, (row, column, value, index) => ((int)value).ToString("000"))),
                PlainColumn("Amount", "金额", Counting(ledger, (row, column, value, index) => ((decimal)value).ToString("N2"))),
            });

            // EP config.ts:203 取的是属性原值（int / decimal），不是 DisplayRender 的文本
            Assert.All(ledger.ValuesOf("年龄"), value => Assert.Equal("Int32", value.GetType().Name));
            Assert.All(ledger.ValuesOf("金额"), value => Assert.Equal("Decimal", value.GetType().Name));
            Assert.Equal(new[] { "021", "022", "023" }, CellText(cut, 0));
            Assert.Equal(new[] { "1,234.50", "6,789.00", "0.00" }, CellText(cut, 1));
        }

        [Fact]
        public void Formatter_ReceivesRowColumnAndPageIndex()
        {
            var ledger = new Ledger();
            var data = Rows();
            var cut = RenderTable(new[]
            {
                PlainColumn("Name", "名称", Counting(ledger, (row, column, value, index) => index.ToString())),
            }, dataSource: data);

            // 一轮呈现树每格一次；ElTable 实测渲染两轮 ⇒ 3 格 × RenderCount
            Assert.Equal(3 * cut.RenderCount, ledger.Calls);
            Assert.Equal(new[] { 0, 1, 2 }, ledger.Indexes.Distinct().OrderBy(x => x).ToArray());
            Assert.Equal(data.Cast<object>(), ledger.Rows.Distinct().ToList());
            Assert.All(ledger.Rows, row => Assert.Contains(row, data));
            var header = Assert.IsType<TableHeader>(ledger.Columns[0]);
            Assert.Equal("Name", header.Property.Name);
            Assert.All(ledger.Columns, column => Assert.Same(header, column));
            Assert.Equal(new[] { "0", "1", "2" }, CellText(cut, 0));
        }

        [Fact]
        public void Formatter_IndexIsPageLocal()
        {
            var ledger = new Ledger();
            var data = Rows();
            // 3 行、PageSize=2、当前第 2 页 ⇒ 只渲染第三行
            var cut = RenderTable(new[]
            {
                PlainColumn("Name", "名称", Counting(ledger, (row, column, value, index) => index.ToString())),
            }, pagination: true, currentPage: 2, dataSource: data);

            // EP 传的是当前渲染序列的下标（table-body 的 $index），换页重新从 0 计
            Assert.Equal(cut.RenderCount, ledger.Calls);
            Assert.All(ledger.Indexes, index => Assert.Equal(0, index));
            Assert.All(ledger.Rows, row => Assert.Same(data[2], row));
            Assert.Equal(new[] { "0" }, CellText(cut, 0));
        }

        [Fact]
        public void Formatter_RunsEvenWhenColumnTypeHasNoDisplayRender()
        {
            // List<string> 不在 DisplayRenderFactory 的映射表里 ⇒ Eval==null ⇒ 改前整格空白
            var cut = RenderTable(new[]
            {
                PlainColumn("Tags", "标签"),
                PlainColumn("Tags", "标签格式化", (row, column, value, index) =>
                    string.Join("|", (List<string>)value)),
                PlainColumn("Name", "名称"),
            });

            // 同一次渲染里前一列（无 formatter）仍是空白 —— 证明「空白」是类型没渲染器，不是选择器读不到
            Assert.Equal(new[] { "", "", "" }, CellText(cut, 0));
            Assert.Equal(new[] { "a|b", "c", "" }, CellText(cut, 1));
            Assert.Equal(new[] { "Alpha", "Bravo", "" }, CellText(cut, 2));
        }

        [Fact]
        public void NullCellValue_StillCallsFormatter()
        {
            var ledger = new Ledger();
            var cut = RenderTable(new[]
            {
                PlainColumn("Name", "名称", Counting(ledger, (row, column, value, index) => value == null ? "—" : (string)value)),
            });

            Assert.Equal(3 * cut.RenderCount, ledger.Calls);
            Assert.Contains(null, ledger.Values);
            Assert.Equal(new[] { "Alpha", "Bravo", "—" }, CellText(cut, 0));
        }

        [Fact]
        public void TemplateColumn_WinsOverFormatter()
        {
            var slotLedger = new Ledger();
            var plainLedger = new Ledger();
            var cut = RenderTable(new[]
            {
                TemplateColumn("Name", "名称", Counting(slotLedger, (row, column, value, index) => "should-not-appear")),
                PlainColumn("Name", "名称2", Counting(plainLedger, (row, column, value, index) => "formatted")),
            });

            // EP render-helper.ts:171-178：有默认插槽就不调 defaultRenderCell ⇒ formatter 根本不跑
            Assert.Equal(0, slotLedger.Calls);
            Assert.Equal(3 * cut.RenderCount, plainLedger.Calls);
            Assert.Equal(3, CellHtml(cut, 0, "em").Count);
            Assert.Equal(new[] { "slot:Alpha", "slot:Bravo", "slot:" }, CellText(cut, 0));
            Assert.DoesNotContain("should-not-appear", string.Join("|", CellText(cut, 1).Concat(CellText(cut, 0))));
            Assert.Equal(new[] { "formatted", "formatted", "formatted" }, CellText(cut, 1));
        }

        [Fact]
        public void IndexTypeColumn_DoesNotRunFormatter()
        {
            var indexLedger = new Ledger();
            var nameLedger = new Ledger();
            var cut = RenderTable(new[]
            {
                IndexColumn("#", Counting(indexLedger, (row, column, value, index) => "should-not-appear")),
                PlainColumn("Name", "名称", Counting(nameLedger, (row, column, value, index) => value)),
            });

            Assert.Equal(0, indexLedger.Calls);
            Assert.Equal(3 * cut.RenderCount, nameLedger.Calls);
            Assert.Equal(new[] { "1", "2", "3" }, CellText(cut, 0));
        }

        [Fact]
        public void WithoutFormatter_RenderingIsUnchanged()
        {
            // 反向对照：不挂 formatter 时三格仍是裸 ToString()，本批没动默认渲染
            var cut = RenderTable(new[]
            {
                PlainColumn("Name", "名称"),
                PlainColumn("Age", "年龄"),
            });

            Assert.Equal(new[] { "Alpha", "Bravo", "" }, CellText(cut, 0));
            Assert.Equal(new[] { "21", "22", "23" }, CellText(cut, 1));
        }

        [Fact]
        public void Formatter_ReturningRenderFragment_IsRendered()
        {
            var cut = RenderTable(new[]
            {
                PlainColumn("Name", "名称", (row, column, value, index) =>
                    (RenderFragment)(inner =>
                    {
                        var seq = 0;
                        inner.OpenElement(seq++, "b");
                        inner.AddContent(seq++, (string)value);
                        inner.CloseElement();
                    })),
            });

            // EP 口径 formatter 可回 VNode；这里对应 RenderFragment —— 必须是真节点，不是类名字符串
            Assert.Equal(3, CellHtml(cut, 0, "b").Count);
            Assert.Equal(new[] { "Alpha", "Bravo", "" }, CellText(cut, 0));
        }

        [Fact]
        public void EnableMarkupString_DecidesWhetherFormatterStringIsHtml()
        {
            Func<object, object, object, int, object> formatter = (row, column, value, index) => "<b>" + value + "</b>";

            var on = RenderTable(new[] { PlainColumn("Name", "名称", formatter) }, enableMarkupString: true);
            Assert.Equal(3, CellHtml(on, 0, "b").Count);
            Assert.Equal(new[] { "Alpha", "Bravo", "" }, CellText(on, 0));

            var off = RenderTable(new[] { PlainColumn("Name", "名称", formatter) }, enableMarkupString: false);
            Assert.Empty(CellHtml(off, 0, "b"));
            // 关着时 formatter 文本原样转义显示（跟未走 formatter 那一档同口径）
            Assert.Equal(new[] { "<b>Alpha</b>", "<b>Bravo</b>", "<b></b>" }, CellText(off, 0));
        }

        [Fact]
        public void Formatter_Exception_PropagatesInsteadOfBlanking()
        {
            var ledger = new Ledger();
            var boom = Counting(ledger, (row, column, value, index) =>
                throw new InvalidOperationException("formatter-boom"));

            // 单行数据：单元格内容属于 ElOverflowTooltip 的 ChildContent，formatter 是在那个子件渲染时才跑的，
            // 一行一格 ⇒ 调用次数没有「渲染几轮」的干扰，能钉成 1。
            var oneRow = new List<Row> { new Row { Name = "Alpha" } };
            var ex = Record.Exception(() => RenderTable(new[] { PlainColumn("Name", "名称", boom) }, dataSource: oneRow));

            // EP 里 formatter 抛错就是渲染错误；本库不静默画空格（否则用户看不出格式化函数坏了）
            Assert.NotNull(ex);
            Assert.Equal(1, ledger.Calls);
            Assert.Contains("formatter-boom", ex.ToString());
        }
    }
}
