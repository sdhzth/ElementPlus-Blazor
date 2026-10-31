using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElDropdownTests : BunitContext
    {
        public ElDropdownTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static void Build(ComponentParameterCollectionBuilder<ElDropdown> p)
        {
            p.Add(x => x.Trigger, (RenderFragment)(b => b.AddContent(0, "触发器")))
             .Add(x => x.Items, (RenderFragment)(b => b.AddContent(0, "菜单")));
        }

        [Fact]
        public void RendersTriggerWithRoleAndTabindex()
        {
            var cut = Render<ElDropdown>(Build);

            var root = cut.Find(".el-dropdown");
            Assert.Equal("menu", root.GetAttribute("aria-haspopup"));
            Assert.Equal("0", root.GetAttribute("tabindex"));
        }

        [Fact]
        public void CustomRoleAndTabindexAndId()
        {
            var cut = Render<ElDropdown>(p =>
            {
                Build(p);
                p.Add(x => x.Role, "listbox")
                 .Add(x => x.Tabindex, 3)
                 .Add(x => x.Id, "dd-1")
                 .Add(x => x.AriaLabel, "操作");
            });

            var root = cut.Find(".el-dropdown");
            Assert.Equal("listbox", root.GetAttribute("aria-haspopup"));
            Assert.Equal("3", root.GetAttribute("tabindex"));
            Assert.Equal("dd-1", root.GetAttribute("id"));
            Assert.Equal("操作", root.GetAttribute("aria-label"));
        }

        [Fact]
        public void DisabledSetsMinusOneTabindexAndClass()
        {
            var cut = Render<ElDropdown>(p =>
            {
                Build(p);
                p.Add(x => x.Disabled, true);
            });

            var root = cut.Find(".el-dropdown");
            Assert.Equal("-1", root.GetAttribute("tabindex"));
            Assert.Contains("is-disabled", root.ClassList);
        }

        [Fact]
        public void ShowTimeoutDefaultsToOfficial250()
        {
            var cut = Render<ElDropdown>(Build);
            Assert.Equal(250, cut.Instance.ShowTimeout);
        }

        [Fact]
        public void PlacementAndMaxHeightDefault()
        {
            var cut = Render<ElDropdown>(p =>
            {
                Build(p);
                p.Add(x => x.Placement, "top-start")
                 .Add(x => x.MaxHeight, 120);
            });

            Assert.Equal("top-start", cut.Instance.Placement);
            Assert.Equal(120, cut.Instance.MaxHeight);
        }

        [Fact]
        public void ShowDropDownRaisesVisibleChangeAndOpen()
        {
            var visibleChanges = 0;
            var opened = 0;
            var cut = Render<ElDropdown>(p =>
            {
                Build(p);
                p.Add(x => x.OnVisibleChange, (bool v) => { if (v) visibleChanges++; })
                 .Add(x => x.OnOpen, () => opened++);
            });

            cut.InvokeAsync(() => cut.Instance.ShowDropDownAsync());

            Assert.Equal(1, visibleChanges);
            Assert.Equal(1, opened);
        }

        [Fact]
        public void DisabledDropdownDoesNotOpen()
        {
            var visibleChanges = 0;
            var cut = Render<ElDropdown>(p =>
            {
                Build(p);
                p.Add(x => x.Disabled, true)
                 .Add(x => x.OnVisibleChange, (bool v) => visibleChanges++);
            });

            cut.InvokeAsync(() => cut.Instance.ShowDropDownAsync());

            Assert.Equal(0, visibleChanges);
        }

        // ---- 回归：hover 触发的菜单不得铺「点击外部关闭」的全屏遮罩 ----
        // 遮罩铺满整屏且层级高于触发器，一旦它出现在指针下方，浏览器重算命中后会给触发器补发
        // mouseout，菜单随即被 HideTimeout 关闭（表现为「弹出后马上消失 / 反复闪烁」）。
        // 用同动作对照：只差 TriggerMode 的两个实例比弹层 DOM。
        [Fact]
        public async Task HoverTriggerDoesNotRenderOutsideClickShadow()
        {
            var cut = Render<DropdownHost>(p => p.Add(x => x.Mode, DropdownTrigger.Hover));

            await cut.InvokeAsync(() => cut.Instance.Dropdown.ShowDropDownAsync());

            Assert.Empty(cut.FindAll(".v-modal"));
        }

        [Fact]
        public async Task ClickTriggerKeepsOutsideClickShadow()
        {
            var cut = Render<DropdownHost>(p => p.Add(x => x.Mode, DropdownTrigger.Click));

            await cut.InvokeAsync(() => cut.Instance.Dropdown.ShowDropDownAsync());

            Assert.Single(cut.FindAll(".v-modal"));
        }

        // ---- 回归：分裂按钮的两半必须由 el-button-group 包裹（EP 结构）----
        [Fact]
        public void SplitButtonRendersAttachedButtonGroup()
        {
            var cut = Render<DropdownHost>(p => p.Add(x => x.Split, true));

            var group = cut.Find(".el-button-group");
            Assert.Equal(2, group.QuerySelectorAll("button").Length);
            Assert.NotNull(group.QuerySelector(".el-dropdown__caret-button"));
            // 两半不再直接挂在 .el-dropdown 下（否则会叠加 .el-button+.el-button 的 12px margin）
            Assert.Empty(cut.FindAll(".el-dropdown > button"));
        }

        private sealed class DropdownHost : ComponentBase
        {
            [Parameter] public DropdownTrigger Mode { get; set; } = DropdownTrigger.Click;

            [Parameter] public bool Split { get; set; }

            public ElDropdown Dropdown { get; private set; }

            protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
            {
                builder.OpenComponent<ElDropdown>(0);
                builder.AddAttribute(1, nameof(ElDropdown.TriggerMode), Mode);
                builder.AddAttribute(2, nameof(ElDropdown.SplitButton), Split);
                builder.AddAttribute(3, nameof(ElDropdown.Trigger), (RenderFragment)(b => b.AddContent(0, "触发器")));
                builder.AddAttribute(4, nameof(ElDropdown.ButtonContent), (RenderFragment)(b => b.AddContent(0, "主要操作")));
                builder.AddAttribute(5, nameof(ElDropdown.Items), (RenderFragment)(b => b.AddContent(0, "菜单")));
                builder.AddComponentReferenceCapture(6, o => Dropdown = (ElDropdown)o);
                builder.CloseComponent();

                builder.OpenComponent<ElPopup>(7);
                builder.CloseComponent();
            }
        }
    }
}
