using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElAnchorTests : BunitContext
    {
        public ElAnchorTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static RenderFragment Links() => builder =>
        {
            builder.OpenComponent<ElAnchorLink>(0);
            builder.AddAttribute(1, "Href", "#a");
            builder.AddAttribute(2, "Title", "A");
            builder.CloseComponent();
        };

        [Fact]
        public void RendersAnchorWithMarkerByDefault()
        {
            var cut = Render<ElAnchor>(p => p.AddChildContent(Links()));

            Assert.NotNull(cut.Find(".el-anchor"));
            Assert.NotNull(cut.Find(".el-anchor__marker"));
            Assert.NotNull(cut.Find(".el-anchor__list"));
        }

        [Fact]
        public void MarkerFalseHidesMarker()
        {
            var cut = Render<ElAnchor>(p => p
                .Add(x => x.Marker, false)
                .AddChildContent(Links()));

            Assert.Empty(cut.FindAll(".el-anchor__marker"));
        }

        [Fact]
        public void CursorTemplateReplacesDefaultMarker()
        {
            var cut = Render<ElAnchor>(p => p
                .Add(x => x.CursorTemplate, (RenderFragment)(builder =>
                {
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "class", "my-cursor");
                    builder.CloseElement();
                }))
                .AddChildContent(Links()));

            Assert.NotNull(cut.Find(".my-cursor"));
            Assert.Empty(cut.FindAll(".el-anchor__marker"));
        }

        [Fact]
        public void TypeAndDirectionRenderModifierClasses()
        {
            var cut = Render<ElAnchor>(p => p
                .Add(x => x.Type, AnchorType.Underline)
                .Add(x => x.Direction, AnchorDirection.Horizontal)
                .AddChildContent(Links()));

            var cls = cut.Find(".el-anchor").GetAttribute("class");
            Assert.Contains("el-anchor--underline", cls);
            Assert.Contains("el-anchor--horizontal", cls);
        }

        [Fact]
        public void IdAndAriaLabelAreRendered()
        {
            var cut = Render<ElAnchor>(p => p
                .Add(x => x.Id, "anchor-1")
                .Add(x => x.AriaLabel, "目录")
                .AddChildContent(Links()));

            Assert.Equal("anchor-1", cut.Find(".el-anchor").GetAttribute("id"));
            Assert.Equal("目录", cut.Find(".el-anchor").GetAttribute("aria-label"));
        }

        [Fact]
        public void SetActiveHrefUpdatesStateAndRaisesChange()
        {
            var changes = new List<string>();
            var cut = Render<ElAnchor>(p => p
                .Add(x => x.OnChange, (string h) => changes.Add(h))
                .AddChildContent(Links()));

            cut.InvokeAsync(() => cut.Instance.SetActiveHref("#a"));

            Assert.Equal("#a", cut.Instance.ActiveHref);
            Assert.Single(changes);
            Assert.Equal("#a", changes[0]);
        }

        [Fact]
        public void ModelValueActivatesMatchingLinkOnFirstRender()
        {
            var cut = Render<ElAnchor>(p => p
                .Add(x => x.ModelValue, "#a")
                .AddChildContent(Links()));

            Assert.Contains("is-active", cut.Find(".el-anchor__item").GetAttribute("class"));
            Assert.Equal("#a", cut.Instance.ActiveHref);
        }

        [Fact]
        public void AnchorLinkRendersTargetAttribute()
        {
            var cut = Render<ElAnchor>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElAnchorLink>(0);
                builder.AddAttribute(1, "Href", "https://example.com");
                builder.AddAttribute(2, "Title", "外链");
                builder.AddAttribute(3, "Target", "_blank");
                builder.CloseComponent();
            }));

            Assert.Equal("_blank", cut.Find(".el-anchor__link").GetAttribute("target"));
        }
    }
}
