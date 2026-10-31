using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElTable 分页器总数口径（EffectiveTotal）。
    /// <para>
    /// 缺陷实抓：表体切片按 <c>rows.Count</c> 走（<c>ElTable.razor.cs</c> 的 <c>PageRows</c>），
    /// 分页器却只读宿主的 <c>Total</c> 参数（<c>ElTable.razor</c> 分页块），而 <c>Total</c> 默认 0、
    /// 全库没有任何地方按数据源回填（<c>ElTable.razor.cs</c> 里 <c>Total</c> 只有声明处）。
    /// 于是本地数据不显式传 <c>Total</c> 时：12 行 / PageSize=3，表体只渲染 3 行，页码恒 0 页，翻页点不动。
    /// </para>
    /// <para>
    /// 现口径：宿主给了 <c>Total</c>（远程分页）就用它，否则回落当前（已筛选排序的）行数，树形按顶层节点数。
    /// </para>
    /// </summary>
    public class ElTablePaginationTotalTests : BunitContext
    {
        public ElTablePaginationTotalTests()
        {
            Services.AddElementPlusServices();
            Services.AddSingleton(new HttpClient());
        }

        public class Row
        {
            public string Name { get; set; }
        }

        public class Org : TreeItemBase
        {
            public string Name { get; set; }
        }

        private static List<Row> Rows(int n) =>
            Enumerable.Range(1, n).Select(i => new Row { Name = "R" + i }).ToList();

        private static RenderFragment Columns() => new(b =>
        {
            b.OpenComponent<ElTableColumn>(0);
            b.AddAttribute(1, "Property", "Name");
            b.AddAttribute(2, "Text", "名称");
            b.CloseComponent();
        });

        private IRenderedComponent<ElTable> RenderPaged(List<Row> data, int pageSize, int total = 0) =>
            Render<ElTable>(p => p
                .Add(x => x.DataSource, data)
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.EnablePagination, true)
                .Add(x => x.PageSize, pageSize)
                .Add(x => x.Total, total)
                .AddChildContent(Columns()));

        /// <summary>页码条上的页号（按 DOM 序，省略号等无文字的 li 被丢掉）。</summary>
        private static List<string> PagerPages(IRenderedComponent<ElTable> cut) =>
            cut.FindAll(".el-pager li").Select(li => li.TextContent.Trim()).Where(t => t.Length > 0).ToList();

        /// <summary>当前页渲染的行名。</summary>
        private static List<string> BodyNames(IRenderedComponent<ElTable> cut) =>
            cut.FindAll(".el-table__body tbody tr")
                .Select(tr => tr.QuerySelector(".el-table__cell .cell")?.TextContent?.Trim() ?? string.Empty)
                .ToList();

        [Fact]
        public void 本地数据未传Total_分页器按行数出页码()
        {
            var cut = RenderPaged(Rows(12), pageSize: 3);

            // 翻面断言：修复前 EffectiveTotal 恒为宿主的 0，这里一个页码都渲染不出来
            Assert.Equal(new[] { "1", "2", "3", "4" }, PagerPages(cut));
            Assert.Equal(new[] { "R1", "R2", "R3" }, BodyNames(cut));
        }

        [Fact]
        public void 翻页事件发到宿主_宿主回写CurrentPage后表体换行()
        {
            var changed = 0;
            var cut = Render<ElTable>(p => p
                .Add(x => x.DataSource, Rows(12))
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.EnablePagination, true)
                .Add(x => x.PageSize, 3)
                .Add(x => x.CurrentPage, 1)
                .Add(x => x.CurrentPageChanged, EventCallback.Factory.Create<int>(this, page => changed = page))
                .AddChildContent(Columns()));

            Assert.Equal(new[] { "R1", "R2", "R3" }, BodyNames(cut));

            cut.FindAll(".el-pager li")[1].Click();
            Assert.Equal(2, changed);

            // @bind-CurrentPage 的等价动作：宿主把页码写回来，表体才换页
            cut.Render(p => p.Add(x => x.CurrentPage, 2));
            Assert.Equal(new[] { "R4", "R5", "R6" }, BodyNames(cut));
        }

        [Fact]
        public void 宿主显式传Total_优先于行数()
        {
            // 远程分页语义不变：DataSource 只给当页 3 行，Total 给全量 100
            var cut = RenderPaged(Rows(3), pageSize: 3, total: 100);

            Assert.Contains("34", PagerPages(cut));
            Assert.Equal(new[] { "R1", "R2", "R3" }, BodyNames(cut));
        }

        [Fact]
        public void 行数不足一页_未传Total_分页器整体隐藏()
        {
            var cut = RenderPaged(Rows(2), pageSize: 3);

            // NoPaginationOnSinglePage 默认 true（EP hide-on-single-page 口径）：回落后的 2 行不足一页，
            // 分页器不出现；同前提的对照见 本地数据未传Total_分页器按行数出页码
            Assert.Empty(PagerPages(cut));
            Assert.Equal(new[] { "R1", "R2" }, BodyNames(cut));
        }

        [Fact]
        public void 树形数据未传Total_按顶层节点数出页码()
        {
            var data = new List<Org>
            {
                new Org { Id = 1, ParentId = 0, Level = 0, Name = "总院" },
                new Org { Id = 2, ParentId = 1, Level = 1, Name = "内科" },
                new Org { Id = 3, ParentId = 2, Level = 2, Name = "心血管" },
                new Org { Id = 4, ParentId = 0, Level = 0, Name = "东院区" },
                new Org { Id = 5, ParentId = 4, Level = 1, Name = "急诊" },
                new Org { Id = 6, ParentId = 0, Level = 0, Name = "南院区" },
            };

            var cut = Render<ElTable>(p => p
                .Add(x => x.DataSource, data)
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.EnablePagination, true)
                .Add(x => x.PageSize, 2));

            // 6 个节点、3 个顶层：PageSize=2 按顶层节点分页 ⇒ 2 页（PageRows 与分页器同一个基数）
            Assert.Equal(new[] { "1", "2" }, PagerPages(cut));
        }
    }
}
