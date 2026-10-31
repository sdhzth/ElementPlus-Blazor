using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElSpace 单元测试 — 对齐 Element Plus Space 官方 API
    /// </summary>
    public class ElSpaceTests : BunitContext
    {
        public ElSpaceTests() => Services.AddElementServices();

        // ==================== 基础渲染 ====================

        [Fact]
        public void Default_RenderDiv()
        {
            var cut = Render<ElSpace>(p => p.AddChildContent("content"));
            var el = cut.Find("div");
            Assert.Contains("el-space", el.ClassList);
            Assert.Equal("content", el.TextContent.Trim());
        }

        [Fact]
        public void Default_HorizontalDirection()
        {
            var cut = Render<ElSpace>(p => p.AddChildContent("x"));
            Assert.Contains("el-space--horizontal", cut.Find("div").ClassList);
        }

        [Fact]
        public void Default_CenterAlignment()
        {
            var cut = Render<ElSpace>(p => p.AddChildContent("x"));
            Assert.Contains("align-items:center", cut.Find("div").GetAttribute("style"));
        }

        [Fact]
        public void Default_Size12px()
        {
            var cut = Render<ElSpace>(p => p.AddChildContent("x"));
            Assert.Contains("gap:12px", cut.Find("div").GetAttribute("style"));
        }

        // ==================== Direction ====================

        [Fact]
        public void Direction_Vertical()
        {
            var cut = Render<ElSpace>(p => p
                .Add(x => x.Direction, SpaceDirection.Vertical)
                .AddChildContent("x"));
            Assert.Contains("el-space--vertical", cut.Find("div").ClassList);
        }

        [Fact]
        public void Direction_HorizontalExplicit()
        {
            var cut = Render<ElSpace>(p => p
                .Add(x => x.Direction, SpaceDirection.Horizontal)
                .AddChildContent("x"));
            Assert.Contains("el-space--horizontal", cut.Find("div").ClassList);
        }

        // ==================== Alignment ====================

        [Fact]
        public void Alignment_Center()
        {
            var cut = Render<ElSpace>(p => p
                .Add(x => x.Alignment, SpaceAlignment.Center)
                .AddChildContent("x"));
            Assert.Contains("align-items:center", cut.Find("div").GetAttribute("style"));
        }

        [Fact]
        public void Alignment_Start()
        {
            var cut = Render<ElSpace>(p => p
                .Add(x => x.Alignment, SpaceAlignment.Start)
                .AddChildContent("x"));
            Assert.Contains("align-items:flex-start", cut.Find("div").GetAttribute("style"));
        }

        [Fact]
        public void Alignment_End()
        {
            var cut = Render<ElSpace>(p => p
                .Add(x => x.Alignment, SpaceAlignment.End)
                .AddChildContent("x"));
            Assert.Contains("align-items:flex-end", cut.Find("div").GetAttribute("style"));
        }

        [Fact]
        public void Alignment_Baseline()
        {
            var cut = Render<ElSpace>(p => p
                .Add(x => x.Alignment, SpaceAlignment.Baseline)
                .AddChildContent("x"));
            Assert.Contains("align-items:baseline", cut.Find("div").GetAttribute("style"));
        }

        [Fact]
        public void Alignment_Stretch()
        {
            var cut = Render<ElSpace>(p => p
                .Add(x => x.Alignment, SpaceAlignment.Stretch)
                .AddChildContent("x"));
            Assert.Contains("align-items:stretch", cut.Find("div").GetAttribute("style"));
        }

        // ==================== Size ====================

        [Fact]
        public void Size_Small_8px()
        {
            var cut = Render<ElSpace>(p => p
                .Add(x => x.Size, SpaceSize.Small)
                .AddChildContent("x"));
            Assert.Contains("gap:8px", cut.Find("div").GetAttribute("style"));
        }

        [Fact]
        public void Size_Default_12px()
        {
            var cut = Render<ElSpace>(p => p
                .Add(x => x.Size, SpaceSize.Default)
                .AddChildContent("x"));
            Assert.Contains("gap:12px", cut.Find("div").GetAttribute("style"));
        }

        [Fact]
        public void Size_Large_16px()
        {
            var cut = Render<ElSpace>(p => p
                .Add(x => x.Size, SpaceSize.Large)
                .AddChildContent("x"));
            Assert.Contains("gap:16px", cut.Find("div").GetAttribute("style"));
        }

        // ==================== Spacer（覆盖 Size）====================

        [Fact]
        public void Spacer_OverridesSize()
        {
            var cut = Render<ElSpace>(p => p
                .Add(x => x.Size, SpaceSize.Large)
                .Add(x => x.Spacer, 25)
                .AddChildContent("x"));
            Assert.Contains("gap:25px", cut.Find("div").GetAttribute("style"));
        }

        [Fact]
        public void Spacer_Zero()
        {
            var cut = Render<ElSpace>(p => p
                .Add(x => x.Spacer, 0)
                .AddChildContent("x"));
            Assert.Contains("gap:0px", cut.Find("div").GetAttribute("style"));
        }

        // ==================== Wrap ====================

        [Fact]
        public void Wrap_AddsIsWrapClass()
        {
            var cut = Render<ElSpace>(p => p
                .Add(x => x.Wrap, true)
                .AddChildContent("x"));
            Assert.Contains("is-wrap", cut.Find("div").ClassList);
        }

        [Fact]
        public void NotWrap_NoIsWrapClass()
        {
            var cut = Render<ElSpace>(p => p.AddChildContent("x"));
            Assert.DoesNotContain("is-wrap", cut.Find("div").ClassList);
        }

        // ==================== Fill ====================

        [Fact]
        public void Fill_AddsIsFillClass()
        {
            var cut = Render<ElSpace>(p => p
                .Add(x => x.Fill, true)
                .AddChildContent("x"));
            Assert.Contains("is-fill", cut.Find("div").ClassList);
        }

        [Fact]
        public void NotFill_NoIsFillClass()
        {
            var cut = Render<ElSpace>(p => p.AddChildContent("x"));
            Assert.DoesNotContain("is-fill", cut.Find("div").ClassList);
        }

        [Fact]
        public void Fill_FillRatio_Custom()
        {
            var cut = Render<ElSpace>(p => p
                .Add(x => x.Fill, true)
                .Add(x => x.FillRatio, "30%")
                .AddChildContent("x"));
            Assert.Contains("--el-space-fill-ratio:30%", cut.Find("div").GetAttribute("style"));
        }

        // ==================== PrefixCls（官方 prefix-cls）====================

        [Fact]
        public void PrefixCls_DefaultElSpace()
        {
            var cut = Render<ElSpace>(p => p.AddChildContent("x"));
            Assert.Equal("el-space", cut.Instance.PrefixCls);
        }

        [Fact]
        public void PrefixCls_Custom()
        {
            var cut = Render<ElSpace>(p => p.Add(x => x.PrefixCls, "my-space").AddChildContent("x"));
            Assert.Equal("my-space", cut.Instance.PrefixCls);
        }

        [Fact]
        public void GetPrefixCls_ReturnsValue()
        {
            var cut = Render<ElSpace>(p => p.Add(x => x.PrefixCls, "custom").AddChildContent("x"));
            Assert.Equal("custom", cut.Instance.GetPrefixCls());
        }

        // ==================== Separator 参数存在 ====================

        [Fact]
        public void Separator_DefaultNull()
        {
            var cut = Render<ElSpace>(p => p.AddChildContent("x"));
            Assert.Null(cut.Instance.Separator);
        }

        [Fact]
        public void Separator_CanBeSet()
        {
            var frag = new RenderFragment(builder => builder.AddContent(0, "|"));
            var cut = Render<ElSpace>(p => p.Add(x => x.Separator, frag).AddChildContent("x"));
            Assert.NotNull(cut.Instance.Separator);
        }

        // ==================== ElSpaceItem（CascadingParameter）====================

        [Fact]
        public void SpaceItem_DefaultPrefixCls()
        {
            var cut = Render<ElSpace>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent(0, typeof(ElSpaceItem));
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "item")));
                builder.CloseComponent();
            }));
            var item = cut.Find(".el-space__item");
            Assert.NotNull(item);
            Assert.Equal("item", item.TextContent.Trim());
        }

        [Fact]
        public void SpaceItem_CustomPrefixCls()
        {
            var cut = Render<ElSpace>(p => p
                .Add(x => x.PrefixCls, "my-space")
                .AddChildContent(builder =>
            {
                builder.OpenComponent(0, typeof(ElSpaceItem));
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "item")));
                builder.CloseComponent();
            }));
            Assert.NotNull(cut.Find(".my-space__item"));
        }

        [Fact]
        public void SpaceItem_ClsAppended()
        {
            var cut = Render<ElSpace>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent(0, typeof(ElSpaceItem));
                builder.AddAttribute(1, "Cls", "custom-item");
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "item")));
                builder.CloseComponent();
            }));
            Assert.Contains("custom-item", cut.Find(".el-space__item").ClassList);
        }

        [Fact]
        public void SpaceItem_Standalone_RendersDefaultPrefix()
        {
            // 单独使用 ElSpaceItem（无父级）也能渲染
            var cut = Render<ElSpaceItem>(p => p.AddChildContent("standalone"));
            Assert.NotNull(cut.Find(".el-space__item"));
            Assert.Equal("standalone", cut.Find(".el-space__item").TextContent.Trim());
        }

        // ==================== Cls / Style / Attributes ====================

        [Fact]
        public void Cls_Appended()
        {
            var cut = Render<ElSpace>(p => p.Add(x => x.Cls, "my-space").AddChildContent("x"));
            Assert.Contains("my-space", cut.Find("div").ClassList);
        }

        [Fact]
        public void Style_Appended()
        {
            var cut = Render<ElSpace>(p => p.Add(x => x.Style, "margin:8px").AddChildContent("x"));
            Assert.Contains("margin:8px", cut.Find("div").GetAttribute("style"));
        }

        [Fact]
        public void Attributes_Passthrough()
        {
            var cut = Render<ElSpace>(p => p
                .AddChildContent("x")
                .AddUnmatched("data-component", "space"));
            Assert.Equal("space", cut.Find("div").GetAttribute("data-component"));
        }

        // ==================== 枚举值验证 ====================

        [Fact]
        public void SpaceDirection_Values()
        {
            Assert.Equal(0, (int)SpaceDirection.Horizontal);
            Assert.Equal(1, (int)SpaceDirection.Vertical);
        }

        [Fact]
        public void SpaceAlignment_Values()
        {
            Assert.Equal(0, (int)SpaceAlignment.Center);
            Assert.Equal(1, (int)SpaceAlignment.Start);
            Assert.Equal(2, (int)SpaceAlignment.End);
            Assert.Equal(3, (int)SpaceAlignment.Baseline);
            Assert.Equal(4, (int)SpaceAlignment.Stretch);
        }

        [Fact]
        public void SpaceSize_Values()
        {
            Assert.Equal(0, (int)SpaceSize.Small);
            Assert.Equal(1, (int)SpaceSize.Default);
            Assert.Equal(2, (int)SpaceSize.Large);
        }

        // ==================== 端到端 ====================

        [Fact]
        public void FullFeature_VerticalLargeFillStart()
        {
            var cut = Render<ElSpace>(p => p
                .Add(x => x.Direction, SpaceDirection.Vertical)
                .Add(x => x.Size, SpaceSize.Large)
                .Add(x => x.Alignment, SpaceAlignment.Start)
                .Add(x => x.Fill, true)
                .Add(x => x.FillRatio, "20%")
                .Add(x => x.Wrap, true)
                .AddChildContent("<div>a</div><div>b</div>"));

            var el = cut.Find("div");
            Assert.Contains("el-space", el.ClassList);
            Assert.Contains("el-space--vertical", el.ClassList);
            Assert.Contains("is-wrap", el.ClassList);
            Assert.Contains("is-fill", el.ClassList);
            var style = el.GetAttribute("style");
            Assert.Contains("align-items:flex-start", style);
            Assert.Contains("gap:16px", style);
            Assert.Contains("--el-space-fill-ratio:20%", style);
        }

        [Fact]
        public void SpacerOverridesSize_EvenLarge()
        {
            var cut = Render<ElSpace>(p => p
                .Add(x => x.Size, SpaceSize.Large)
                .Add(x => x.Spacer, 42)
                .AddChildContent("x"));
            Assert.Contains("gap:42px", cut.Find("div").GetAttribute("style"));
        }
    }
}