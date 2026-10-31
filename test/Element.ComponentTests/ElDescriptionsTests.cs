using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElDescriptionsTests : BunitContext
    {
        public ElDescriptionsTests()
        {
            Services.AddElementServices();
        }

        private sealed record ItemSpec(
            string Label,
            string Content,
            int Span = 1,
            int RowSpan = 1,
            DescriptionsAlign? Align = null,
            string LabelWidth = null,
            bool? Colon = null);

        private static RenderFragment Items(params ItemSpec[] data) => builder =>
        {
            int seq = 0;
            foreach (var d in data)
            {
                builder.OpenComponent<ElDescriptionsItem>(seq++);
                builder.AddAttribute(seq++, "Label", d.Label);
                if (d.Span != 1) builder.AddAttribute(seq++, "Span", d.Span);
                if (d.RowSpan != 1) builder.AddAttribute(seq++, "RowSpan", d.RowSpan);
                if (d.Align.HasValue) builder.AddAttribute(seq++, "Align", d.Align.Value);
                if (d.LabelWidth != null) builder.AddAttribute(seq++, "LabelWidth", d.LabelWidth);
                if (d.Colon.HasValue) builder.AddAttribute(seq++, "Colon", d.Colon.Value);
                builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(inner => inner.AddContent(0, d.Content)));
                builder.CloseComponent();
            }
        };

        [Fact]
        public void RendersTitleAndItems()
        {
            var cut = Render<ElDescriptions>(p => p
                .Add(x => x.Title, "User Info")
                .Add(x => x.ChildContent, Items(
                    new ItemSpec("Username", "kooriookami"),
                    new ItemSpec("Telephone", "18100000000"),
                    new ItemSpec("Place", "Suzhou"))));

            Assert.Equal("User Info", cut.Find(".el-descriptions__title").TextContent.Trim());
            Assert.Equal(3, cut.FindAll(".el-descriptions__cell").Count);
            Assert.Equal(3, cut.FindAll(".el-descriptions__label").Count);
        }

        [Fact]
        public void BorderRendersBorderedTableCells()
        {
            var cut = Render<ElDescriptions>(p => p
                .Add(x => x.Border, true)
                .Add(x => x.ChildContent, Items(
                    new ItemSpec("A", "1"), new ItemSpec("B", "2"), new ItemSpec("C", "3"), new ItemSpec("D", "4"))));

            Assert.Contains("is-bordered", cut.Find(".el-descriptions__table").ClassName);
            Assert.Equal(4, cut.FindAll("th.el-descriptions__label.is-bordered-label").Count);
            Assert.Equal(4, cut.FindAll("td.el-descriptions__content.is-bordered-content").Count);
        }

        [Fact]
        public void VerticalProducesLabelAndContentRows()
        {
            var cut = Render<ElDescriptions>(p => p
                .Add(x => x.Direction, DescriptionsDirection.Vertical)
                .Add(x => x.ChildContent, Items(
                    new ItemSpec("A", "1"), new ItemSpec("B", "2"), new ItemSpec("C", "3"))));

            Assert.Equal(2, cut.FindAll(".el-descriptions__row").Count);
            Assert.Equal(3, cut.FindAll("th.el-descriptions__label.is-vertical-label").Count);
            Assert.Equal(3, cut.FindAll("td.el-descriptions__content.is-vertical-content").Count);
        }

        [Fact]
        public void ColumnControlsItemsPerRow()
        {
            var cut = Render<ElDescriptions>(p => p
                .Add(x => x.Column, 2)
                .Add(x => x.ChildContent, Items(
                    new ItemSpec("A", "1"), new ItemSpec("B", "2"), new ItemSpec("C", "3"), new ItemSpec("D", "4"))));

            Assert.Equal(2, cut.FindAll(".el-descriptions__row").Count);
            Assert.Equal(4, cut.FindAll(".el-descriptions__cell").Count);
        }

        [Fact]
        public void SpanOccupiesTwoColumns()
        {
            var cut = Render<ElDescriptions>(p => p
                .Add(x => x.Border, true)
                .Add(x => x.ChildContent, Items(
                    new ItemSpec("Photo", "x"),
                    new ItemSpec("Username", "kooriookami", Span: 2))));

            var contentCells = cut.FindAll("td.el-descriptions__content.is-bordered-content");
            Assert.Equal(2, contentCells.Count);
            Assert.Equal("3", contentCells[1].GetAttribute("colspan"));
        }

        [Fact]
        public void RowSpanSetsRowSpanAttribute()
        {
            var cut = Render<ElDescriptions>(p => p
                .Add(x => x.Border, true)
                .Add(x => x.ChildContent, Items(
                    new ItemSpec("Photo", "x", RowSpan: 2),
                    new ItemSpec("Username", "kooriookami"))));

            var labelCells = cut.FindAll("th.el-descriptions__label.is-bordered-label");
            Assert.Equal("2", labelCells[0].GetAttribute("rowspan"));
        }

        [Fact]
        public void AlignAddsIsCenterModifier()
        {
            var cut = Render<ElDescriptions>(p => p
                .Add(x => x.ChildContent, Items(
                    new ItemSpec("A", "1", Align: DescriptionsAlign.Center))));

            Assert.Contains("is-center", cut.Find(".el-descriptions__cell").ClassName);
        }

        [Fact]
        public void LabelWidthSetsLabelCellStyle()
        {
            var cut = Render<ElDescriptions>(p => p
                .Add(x => x.Border, true)
                .Add(x => x.ChildContent, Items(
                    new ItemSpec("Username", "x", LabelWidth: "120px"))));

            var labelCell = cut.Find("th.el-descriptions__label.is-bordered-label");
            Assert.Contains("width:120px", labelCell.GetAttribute("style"));
        }

        [Fact]
        public void TitleContentSlotRenders()
        {
            var cut = Render<ElDescriptions>(p => p
                .Add(x => x.TitleContent, (RenderFragment)(b => b.AddMarkupContent(0, "<strong>自定义标题</strong>")))
                .Add(x => x.ChildContent, Items(new ItemSpec("A", "1"))));

            Assert.Contains("自定义标题", cut.Find(".el-descriptions__title").TextContent);
        }

        [Fact]
        public void ColonRendersColonSpan()
        {
            var cut = Render<ElDescriptions>(p => p
                .Add(x => x.Colon, true)
                .Add(x => x.ChildContent, Items(new ItemSpec("Username", "kooriookami"))));

            Assert.Single(cut.FindAll(".el-descriptions__colon"));
        }
    }
}
