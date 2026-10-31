using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElTableDynamicColumnTests : BunitContext
    {
        public ElTableDynamicColumnTests()
        {
            Services.AddElementServices();
            Services.AddSingleton(new HttpClient());
        }

        public class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string Dept { get; set; }
        }

        private static List<Person> People()
        {
            return new List<Person>
            {
                new Person { Name = "张三", Age = 30, Dept = "内科" },
                new Person { Name = "李四", Age = 41, Dept = "外科" }
            };
        }

        private static RenderFragment DeclareColumns(IEnumerable<string> properties)
        {
            return builder =>
            {
                var seq = 0;
                foreach (var property in properties)
                {
                    builder.OpenComponent<ElTableColumn>(seq++);
                    builder.AddAttribute(seq++, "Property", property);
                    builder.AddAttribute(seq++, "Text", property);
                    builder.CloseComponent();
                }
            };
        }

        private IRenderedComponent<ElTable> RenderTable(IEnumerable<string> properties = null)
        {
            var props = (properties ?? new[] { "Name" }).ToList();
            return Render<ElTable>(parameters => parameters
                .Add(x => x.DataSource, People())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .AddChildContent(DeclareColumns(props)));
        }

        private static IReadOnlyList<string> HeaderTexts(IRenderedComponent<ElTable> cut)
        {
            return cut.FindAll("thead th")
                .Where(th => !th.ClassList.Contains("gutter"))
                .Select(th => th.TextContent.Trim())
                .ToList();
        }

        private static IReadOnlyList<string> FirstRowCells(IRenderedComponent<ElTable> cut)
        {
            var row = cut.Find("tbody tr");
            return row.QuerySelectorAll("td")
                .Select(td => td.TextContent.Trim())
                .ToList();
        }

        [Fact]
        public void AddColumnByProperty_OnPopulatedTable_ShowsImmediately()
        {
            var cut = RenderTable(new[] { "Name" });

            Assert.Single(HeaderTexts(cut));

            cut.Instance.AddColumn("Dept", "部门");

            Assert.Equal(new[] { "Name", "部门" }, HeaderTexts(cut));
            Assert.Contains("内科", FirstRowCells(cut));
            Assert.Contains("外科", cut.FindAll("tbody tr")[1].TextContent);
        }

        [Fact]
        public void AddComputedColumn_EvaluatesExistingRowsImmediately()
        {
            var cut = RenderTable(new[] { "Name" });

            cut.Instance.AddColumn("明年年龄", row => ((Person)row).Age + 1);

            Assert.Contains("明年年龄", HeaderTexts(cut));
            Assert.Contains("31", FirstRowCells(cut));
        }

        [Fact]
        public void AddColumn_WithSortNo_IsInsertedAtExpectedPosition()
        {
            var cut = RenderTable(new[] { "Name" });

            cut.Instance.AddColumn("Dept", "部门", -1);

            Assert.Equal("部门", HeaderTexts(cut).First());
        }

        [Fact]
        public void RemoveColumn_DropsHeaderAndCells()
        {
            var cut = RenderTable(new[] { "Name" });
            cut.Instance.AddColumn("Dept", "部门");
            Assert.Equal(2, HeaderTexts(cut).Count);

            Assert.True(cut.Instance.RemoveColumn("部门"));

            Assert.Single(HeaderTexts(cut));
            Assert.DoesNotContain("内科", FirstRowCells(cut));
        }

        [Fact]
        public void RemoveColumn_MissingColumn_ReturnsFalse()
        {
            var cut = RenderTable(new[] { "Name" });

            Assert.False(cut.Instance.RemoveColumn("不存在的列"));
        }

        [Fact]
        public void ReloadColumns_PicksUpDeclarativeColumnChanges()
        {
            var properties = new List<string> { "Name" };
            var cut = Render<ElTable>(parameters => parameters
                .Add(x => x.DataSource, People())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .AddChildContent(DeclareColumns(properties)));

            Assert.Single(HeaderTexts(cut));

            properties.Add("Dept");
            cut.Instance.ReloadColumns();

            Assert.Equal(2, HeaderTexts(cut).Count);
            Assert.Contains("内科", FirstRowCells(cut));
        }

        [Fact]
        public void Columns_ReflectsRegisteredHeaders()
        {
            var cut = RenderTable(new[] { "Name" });

            Assert.Single(cut.Instance.Columns);

            cut.Instance.AddColumn("Dept", "部门");

            Assert.Equal(2, cut.Instance.Columns.Count);
            Assert.Contains(cut.Instance.Columns, x => x.Text == "部门" && x.Property != null);
        }
    }
}
