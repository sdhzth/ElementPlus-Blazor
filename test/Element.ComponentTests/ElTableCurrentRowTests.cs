using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElTable 当前行高亮（EP highlight-current-row / setCurrentRow）与 VisibleRows。
    /// EP 口径实抓：table-body/styles-helper.ts:30-37 —— current-row 只在 highlightCurrentRow 为真时进 class；
    /// table-body/events-helper.ts:48-51 —— 点行无条件 commit('setCurrentRow', row)。
    /// </summary>
    public class ElTableCurrentRowTests : BunitContext
    {
        public ElTableCurrentRowTests()
        {
            Services.AddElementServices();
            Services.AddSingleton(new HttpClient());
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
            new Row { Name = "C", Stock = 90 },
        };

        private IRenderedComponent<ElTable> RenderTable(List<Row> data, bool highlight, string defaultSortProp = null)
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
                b.AddAttribute(seq++, "Text", "库存");
                b.CloseComponent();
            });

            return Render<ElTable>(p => p
                .Add(x => x.DataSource, data)
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.EnablePagination, false)
                .Add(x => x.HighlightCurrentRow, highlight)
                .Add(x => x.DefaultSortProp, defaultSortProp ?? string.Empty)
                .AddChildContent(child));
        }

        /// <summary>每行的 class 里 current-row 出现的次数（按显示顺序）。</summary>
        private static List<int> CurrentRowCounts(IRenderedComponent<ElTable> cut) =>
            cut.FindAll(".el-table__body tbody tr")
                .Select(tr => Regex.Matches(tr.GetAttribute("class") ?? string.Empty, "current-row").Count)
                .ToList();

        [Fact]
        public async Task 实例方法_SetCurrentRow_高亮恰一行且不重复()
        {
            var data = Rows();
            var cut = RenderTable(data, highlight: true);
            Assert.Equal(new[] { 0, 0, 0 }, CurrentRowCounts(cut));

            await cut.InvokeAsync(() => cut.Instance.SetCurrentRow(data[1]));

            var counts = CurrentRowCounts(cut);
            Assert.Equal(new[] { 0, 1, 0 }, counts);
        }

        [Fact]
        public void 点行_开关开启_高亮恰一行()
        {
            var cut = RenderTable(Rows(), highlight: true);

            cut.FindAll(".el-table__body tbody tr")[2].Click();

            Assert.Equal(new[] { 0, 0, 1 }, CurrentRowCounts(cut));
        }

        [Fact]
        public void 点行_开关关闭_不高亮但当前行仍然被记录()
        {
            object changed = null;
            var data = Rows();
            var child = new RenderFragment(b =>
            {
                b.OpenComponent<ElTableColumn>(0);
                b.AddAttribute(1, "Property", "Name");
                b.AddAttribute(2, "Text", "名称");
                b.CloseComponent();
            });

            var cut = Render<ElTable>(p => p
                .Add(x => x.DataSource, data)
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.CurrentRowChanged, EventCallback.Factory.Create<object>(this, r => changed = r))
                .AddChildContent(child));

            cut.FindAll(".el-table__body tbody tr")[1].Click();

            // EP：class 由 highlightCurrentRow 决定，但 commit('setCurrentRow') 无条件 —— 事件照发
            Assert.DoesNotContain(1, CurrentRowCounts(cut));
            Assert.Same(data[1], changed);
            Assert.Same(data[1], cut.Instance.CurrentRow);
        }

        [Fact]
        public async Task 父级重渲染_不会抹掉已设置的高亮()
        {
            var data = Rows();
            var cut = RenderTable(data, highlight: true);
            await cut.InvokeAsync(() => cut.Instance.SetCurrentRow(data[2]));
            Assert.Equal(new[] { 0, 0, 1 }, CurrentRowCounts(cut));

            // 父级再渲染一次：DataSource 仍然 supplying，CurrentRow 没有 supplying
            cut.Render();

            Assert.Equal(new[] { 0, 0, 1 }, CurrentRowCounts(cut));
        }

        [Fact]
        public void VisibleRows_跟随默认排序_而不是数据源下标()
        {
            var data = Rows();
            var unsorted = RenderTable(data, highlight: true);
            // 正对照：不排序时 VisibleRows[0] 就是数据源第 1 行
            Assert.Same(data[0], unsorted.Instance.VisibleRows[0]);

            var sorted = RenderTable(data, highlight: true, defaultSortProp: "Stock");
            Assert.Same(data[1], sorted.Instance.VisibleRows[0]);
            Assert.NotSame(unsorted.Instance.VisibleRows[0], sorted.Instance.VisibleRows[0]);
        }

        [Fact]
        public void VisibleRows_计数与_VisibleRowCount_一致()
        {
            var cut = RenderTable(Rows(), highlight: true);
            Assert.Equal(3, cut.Instance.VisibleRows.Count);
            Assert.Equal(cut.Instance.VisibleRowCount, cut.Instance.VisibleRows.Count);
        }
    }
}
