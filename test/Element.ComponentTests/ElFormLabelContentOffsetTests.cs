using Bunit;
using ElementPlus;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElFormLabelContentOffsetTests : BunitContext
    {
        public ElFormLabelContentOffsetTests()
        {
            Services.AddElementPlusServices();
        }

        private class OffsetModel
        {
            public string Name { get; set; }
        }

        private static (string labelStyle, string contentStyle) RenderForm(
            BunitContext ctx,
            bool? formOffset = null,
            bool? itemOffset = null,
            string formLabelWidth = "120px",
            string itemLabelWidth = null,
            string labelPosition = null,
            bool inline = false)
        {
            var cut = ctx.Render<ElForm>(p =>
            {
                p.Add(x => x.Model, new OffsetModel());
                p.Add(x => x.LabelWidth, (object)formLabelWidth);
                if (formOffset.HasValue)
                {
                    p.Add(x => x.LabelContentOffset, formOffset.Value);
                }
                if (labelPosition != null)
                {
                    p.Add(x => x.LabelPosition, (object)labelPosition);
                }
                p.Add(x => x.Inline, inline);
                p.AddChildContent<ElFormItem<string>>(item =>
                {
                    item.Add(x => x.Prop, "Name");
                    item.Add(x => x.Label, "Name");
                    if (itemOffset.HasValue)
                    {
                        item.Add(x => x.LabelContentOffset, (bool?)itemOffset.Value);
                    }
                    if (itemLabelWidth != null)
                    {
                        item.Add(x => x.LabelWidth, (object)itemLabelWidth);
                    }
                    item.AddChildContent<ElInput<string>>();
                });
            });

            return (
                cut.Find(".el-form-item__label").GetAttribute("style") ?? string.Empty,
                cut.Find(".el-form-item__content").GetAttribute("style") ?? string.Empty);
        }

        [Fact]
        public void DefaultAddsMarginLeft()
        {
            var (label, content) = RenderForm(this);
            Assert.Contains("width:120px", label);
            Assert.Contains("margin-left:120px", content);
        }

        [Fact]
        public void FormFalseRemovesMarginLeftButKeepsLabelWidth()
        {
            var (label, content) = RenderForm(this, formOffset: false);
            Assert.Contains("width:120px", label);
            Assert.DoesNotContain("margin-left", content);
        }

        [Fact]
        public void ItemTrueOverridesFormFalse()
        {
            var (_, content) = RenderForm(this, formOffset: false, itemOffset: true);
            Assert.Contains("margin-left:120px", content);
        }

        [Fact]
        public void ItemFalseOverridesFormDefault()
        {
            var (_, content) = RenderForm(this, itemOffset: false);
            Assert.DoesNotContain("margin-left", content);
        }

        [Fact]
        public void TopPositionNeverAddsMarginLeft()
        {
            var (label, content) = RenderForm(this, labelPosition: "top");
            Assert.Contains("width:120px", label);
            Assert.DoesNotContain("margin-left", content);
        }

        [Fact]
        public void TopPositionWinsEvenWhenOffsetIsForcedOn()
        {
            var (_, content) = RenderForm(this, formOffset: true, itemOffset: true, labelPosition: "top");
            Assert.DoesNotContain("margin-left", content);
        }

        [Fact]
        public void EmptyLabelWidthStillHasNoStyles()
        {
            var (label, content) = RenderForm(this, formLabelWidth: string.Empty, formOffset: true);
            Assert.DoesNotContain("width", label);
            Assert.DoesNotContain("margin-left", content);
        }

        [Fact]
        public void InlineFormStillHasNoStyles()
        {
            var (label, content) = RenderForm(this, inline: true);
            Assert.DoesNotContain("width", label);
            Assert.DoesNotContain("margin-left", content);
        }

        [Fact]
        public void EmptyLabelPositionFallsBackToLeftAndKeepsOffset()
        {
            var (_, content) = RenderForm(this, labelPosition: string.Empty);
            Assert.Contains("margin-left:120px", content);
        }

        [Fact]
        public void CustomItemLabelWidthDoesNotReintroduceMarginLeftWhenOffsetIsOff()
        {
            var (label, content) = RenderForm(this, formOffset: false, itemLabelWidth: "300px");
            Assert.Contains("width:300px", label);
            Assert.DoesNotContain("margin-left", content);
        }
    }
}
