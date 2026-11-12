using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElTableColumn.Align / HeaderAlign: EP renders alignment as is-center / is-right
    /// on the th/td (see index.css). Before this fix the parameters were copied into
    /// TableHeader but never used by GetCellClass, so every column stayed left.
    /// </summary>
    public class ElTableColumnAlignTests : BunitContext
    {
        public ElTableColumnAlignTests()
        {
            Services.AddElementPlusServices();
            Services.AddSingleton(new System.Net.Http.HttpClient());
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        public class Row
        {
            public string Name { get; set; }
        }

        private static List<Row> Rows() => new() { new Row { Name = "A" }, new Row { Name = "B" } };

        private IRenderedComponent<ElTable> RenderTable(params (string text, string align, string headerAlign)[] columns)
        {
            var child = new RenderFragment(b =>
            {
                var seq = 0;
                foreach (var c in columns)
                {
                    b.OpenComponent<ElTableColumn>(seq++);
                    b.AddAttribute(seq++, "Property", "Name");
                    b.AddAttribute(seq++, "Text", c.text);
                    if (c.align != null) b.AddAttribute(seq++, "Align", c.align);
                    if (c.headerAlign != null) b.AddAttribute(seq++, "HeaderAlign", c.headerAlign);
                    b.CloseComponent();
                }
            });

            return Render<ElTable>(p => p
                .Add(x => x.DataSource, Rows())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.EnablePagination, false)
                .AddChildContent(child));
        }

        private static string HeadClass(IRenderedComponent<ElTable> cut, int i) => cut.FindAll("thead th")[i].ClassName ?? "";

        private static string BodyClass(IRenderedComponent<ElTable> cut, int i) => cut.FindAll("tbody td")[i].ClassName ?? "";

        [Fact]
        public void DefaultAlign_StaysLeft()
        {
            var cut = RenderTable(("Name", null, null));

            Assert.DoesNotContain("is-center", HeadClass(cut, 0));
            Assert.DoesNotContain("is-right", HeadClass(cut, 0));
            Assert.DoesNotContain("is-center", BodyClass(cut, 0));
            Assert.DoesNotContain("is-right", BodyClass(cut, 0));
        }

        [Fact]
        public void AlignCenter_AppliesToHeaderAndBody()
        {
            var cut = RenderTable(("No.", "center", null));

            Assert.Contains("is-center", HeadClass(cut, 0));
            Assert.Contains("is-center", BodyClass(cut, 0));
            Assert.DoesNotContain("is-right", HeadClass(cut, 0));
        }

        [Fact]
        public void AlignRight_AppliesToHeaderAndBody()
        {
            var cut = RenderTable(("Amount", "right", null));

            Assert.Contains("is-right", HeadClass(cut, 0));
            Assert.Contains("is-right", BodyClass(cut, 0));
            Assert.DoesNotContain("is-center", HeadClass(cut, 0));
        }

        [Fact]
        public void HeaderAlign_OverridesAlignForHeaderOnly()
        {
            var cut = RenderTable(("Amount", "center", "right"));

            Assert.Contains("is-right", HeadClass(cut, 0));
            Assert.DoesNotContain("is-center", HeadClass(cut, 0));
            Assert.Contains("is-center", BodyClass(cut, 0));
        }

        [Fact]
        public void Align_PerColumnIndependent()
        {
            var cut = RenderTable(("A", null, null), ("B", "center", null), ("C", "right", null));

            Assert.DoesNotContain("is-center", HeadClass(cut, 0));
            Assert.Contains("is-center", HeadClass(cut, 1));
            Assert.Contains("is-right", HeadClass(cut, 2));
            Assert.Contains("is-center", BodyClass(cut, 1));
            Assert.Contains("is-right", BodyClass(cut, 2));
        }
    }
}
