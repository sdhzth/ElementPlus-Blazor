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
    public class ElTableIndexColumnTests : BunitContext
    {
        public ElTableIndexColumnTests()
        {
            Services.AddElementPlusServices();
            Services.AddSingleton(new HttpClient());
        }

        public class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        private static List<Person> People()
        {
            return Enumerable.Range(1, 6)
                .Select(i => new Person { Name = "P" + i, Age = 20 + i })
                .ToList();
        }

        private IRenderedComponent<ElTable> RenderTable(
            int? pageSize = null,
            int? currentPage = null,
            Func<int, object> indexMethod = null)
        {
            var builder = new RenderFragment(child =>
            {
                var seq = 0;
                child.OpenComponent<ElTableColumn>(seq++);
                child.AddAttribute(seq++, "Type", "index");
                child.AddAttribute(seq++, "Text", "#");
                if (indexMethod != null) child.AddAttribute(seq++, "IndexMethod", indexMethod);
                child.CloseComponent();

                child.OpenComponent<ElTableColumn>(seq++);
                child.AddAttribute(seq++, "Property", "Name");
                child.AddAttribute(seq++, "Text", "名称");
                child.CloseComponent();
            });

            return Render<ElTable>(parameters => parameters
                .Add(x => x.DataSource, People())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.EnablePagination, true)
                .Add(p => p.PageSize, pageSize ?? 3)
                .Add(p => p.CurrentPage, currentPage ?? 1)
                .AddChildContent(builder));
        }

        private static List<string> IndexCells(IRenderedComponent<ElTable> cut)
        {
            return cut.FindAll("tbody tr.el-table__row")
                .Select(tr => tr.QuerySelector("td .cell")?.TextContent?.Trim() ?? "")
                .ToList();
        }

        [Fact]
        public void IndexColumn_ShowsPageLocalNumbers()
        {
            var cut = RenderTable(pageSize: 3, currentPage: 1);
            var cells = IndexCells(cut);
            Assert.Equal(new[] { "1", "2", "3" }, cells);
        }

        [Fact]
        public void IndexColumn_PageLocalResetsOnPageChange()
        {
            // 第二页默认序号仍从 1 重新开始（与 Element Plus 默认行为一致）
            var cut = RenderTable(pageSize: 3, currentPage: 2);
            var cells = IndexCells(cut);
            Assert.Equal(new[] { "1", "2", "3" }, cells);
        }

        [Fact]
        public void IndexMethod_ContinuousAcrossPages()
        {
            int page = 2;
            int size = 3;
            var cut = RenderTable(pageSize: size, currentPage: page,
                indexMethod: i => i + (page - 1) * size + 1);
            var cells = IndexCells(cut);
            // 第二页连续序号应为 4 / 5 / 6
            Assert.Equal(new[] { "4", "5", "6" }, cells);
        }
    }
}
