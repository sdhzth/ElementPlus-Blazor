using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElTreeSelectTests : BunitContext
    {
        public ElTreeSelectTests()
        {
            Services.AddElementServices();
            JSInterop.SetupVoid("setDisabled", _ => true).SetVoidResult();
        }

        [Fact]
        public void TreeSelectRendersTreeDropdownAndSelectsNode()
        {
            int? value = null;
            var selectedNodes = new List<TreeItemBase>();
            var cut = Render<ElTreeSelect>(parameters => parameters
                .Add(x => x.Data, FlatTreeItems())
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.SelectedNodesChanged, next => selectedNodes = next));

            cut.Find(".el-select").Click();
            var dropdown = GetDropdown();
            var fragment = RenderTreeDropdown(dropdown);

            Assert.True(dropdown.IsTree);
            Assert.IsAssignableFrom<ElTreeSelect>(dropdown.Select);
            Assert.Contains("Root", fragment.Markup);
            Assert.Contains("Leaf", fragment.Markup);

            fragment.FindAll(".el-tree-node__content").Last().Click();

            Assert.Equal(2, value);
            Assert.Equal(new[] { 2 }, selectedNodes.Select(x => x.Id).ToArray());
            Assert.Equal("Root > Leaf", cut.Find("input").GetAttribute("value"));
        }

        [Fact]
        public void ValueParameterInitializesDisplayLabelFromTreePath()
        {
            var cut = Render<ValueHost>();

            Assert.Equal("Root > Leaf", cut.Find("input").GetAttribute("value"));

            cut.Find("button").Click();

            Assert.Equal("Root", cut.Find("input").GetAttribute("value"));
        }

        [Fact]
        public void TreeSelectKeepsInheritedSelectParameters()
        {
            var cut = Render<ElTreeSelect>(parameters => parameters
                .Add(x => x.Data, FlatTreeItems())
                .Add(x => x.Filterable, true)
                .Add(x => x.Clearable, true)
                .Add(x => x.Size, InputSize.Small)
                .Add(x => x.Disabled, true));

            var wrapper = cut.Find(".el-select");

            Assert.Contains("is-filterable", wrapper.ClassList);
            Assert.Contains("is-clearable", wrapper.ClassList);
            Assert.Contains("is-disabled", wrapper.ClassList);
            Assert.Contains("el-select--small", wrapper.ClassList);
            Assert.Equal("true", wrapper.GetAttribute("aria-disabled"));
        }

        [Fact]
        public void TreeSelectSupportsDeclarativeTreeContent()
        {
            int? value = null;
            var cut = Render<ElTreeSelect>(parameters => parameters
                .Add(x => x.TreeContent, DeclarativeTreeContent())
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-select").Click();
            var fragment = RenderTreeDropdown(GetDropdown());

            fragment.WaitForAssertion(() => Assert.Contains("Declarative leaf", fragment.Markup), timeout: TestWait.Timeout);

            fragment.FindAll(".el-tree-node__content").Last().Click();

            Assert.Equal(20, value);
            Assert.Equal("Declarative root > Declarative leaf", cut.Find("input").GetAttribute("value"));
        }

        [Fact]
        public void LegacyTreeSingleSelectUsesTreeSelectPipeline()
        {
            var cut = Render<ElTreeSingleSelect>(parameters => parameters
                .Add(x => x.Data, FlatTreeItems()));

            cut.Find(".el-select").Click();
            var dropdown = GetDropdown();
            var fragment = RenderTreeDropdown(dropdown);

            Assert.True(dropdown.IsTree);
            Assert.IsAssignableFrom<ElTreeSelect>(dropdown.Select);
            Assert.Contains("Leaf", fragment.Markup);
        }

        private DropDownOption GetDropdown()
        {
            return Services.GetRequiredService<PopupService>().SelectDropDownOptions.Single();
        }

        private IRenderedComponent<CascadingValue<ElTreeSelect>> RenderTreeDropdown(DropDownOption dropdown)
        {
            return Render<CascadingValue<ElTreeSelect>>(parameters => parameters
                .Add(x => x.Value, (ElTreeSelect)dropdown.Select)
                .Add(x => x.ChildContent, (RenderFragment)(content => content.AddContent(0, dropdown.OptionContent))));
        }

        private static List<TreeItemBase> FlatTreeItems()
        {
            return new List<TreeItemBase>
            {
                new TreeItemBase { Id = 1, ParentId = 0, Text = "Root" },
                new TreeItemBase { Id = 2, ParentId = 1, Text = "Leaf" }
            };
        }

        private static RenderFragment DeclarativeTreeContent()
        {
            return builder =>
            {
                builder.OpenComponent<ElTreeItem>(0);
                builder.AddAttribute(1, nameof(ElTreeItem.Id), 10);
                builder.AddAttribute(2, nameof(ElTreeItem.Text), "Declarative root");
                builder.AddAttribute(3, nameof(ElTreeItem.ChildContent), (RenderFragment)(child =>
                {
                    child.OpenComponent<ElTreeItem>(0);
                    child.AddAttribute(1, nameof(ElTreeItem.Id), 20);
                    child.AddAttribute(2, nameof(ElTreeItem.Text), "Declarative leaf");
                    child.CloseComponent();
                }));
                builder.CloseComponent();
            };
        }

        private class ValueHost : ComponentBase
        {
            private int? value = 2;

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<ElTreeSelect>(0);
                builder.AddAttribute(1, nameof(ElTreeSelect.Data), FlatTreeItems());
                builder.AddAttribute(2, nameof(ElTreeSelect.Value), value);
                builder.CloseComponent();

                builder.OpenElement(3, "button");
                builder.AddAttribute(4, "onclick", EventCallback.Factory.Create(this, () => value = 1));
                builder.AddContent(5, "Change");
                builder.CloseElement();
            }
        }

        [Fact]
        public void EmptyTextShowsWhenNoData()
        {
            var cut = Render<ElTreeSelect>(parameters => parameters
                .Add(x => x.Data, new List<TreeItemBase>())
                .Add(x => x.EmptyText, "无数据提示"));

            cut.Find(".el-select").Click();
            var fragment = RenderTreeDropdown(GetDropdown());

            Assert.Contains("无数据提示", fragment.Markup);
        }

        [Fact]
        public void TreeSelectFilterableFiltersTreeNodes()
        {
            // 回归：Filterable 触发器输入应把关键字下发给内嵌 ElTree.Filter，命中节点保留、非命中隐藏、祖先展开。
            var cut = Render<ElTreeSelect>(parameters => parameters
                .Add(x => x.Data, FilterTreeItems())
                .Add(x => x.Filterable, true));

            cut.Find(".el-select").Click();
            var fragment = RenderTreeDropdown(GetDropdown());

            var before = fragment.FindAll(".el-tree-node").Count;
            Assert.True(before > 0);

            cut.Find("input").Input("B");

            fragment.WaitForAssertion(() =>
            {
                var after = fragment.FindAll(".el-tree-node").Count;
                Assert.True(after < before, $"过滤后可见节点应减少：before={before} after={after}");
                Assert.Contains("B", fragment.Markup);
            }, timeout: TestWait.Timeout);
        }

        private static List<TreeItemBase> FilterTreeItems()
        {
            return new List<TreeItemBase>
            {
                new TreeItemBase { Id = 1, ParentId = 0, Text = "一级节点 A" },
                new TreeItemBase { Id = 2, ParentId = 1, Text = "二级节点 A1" },
                new TreeItemBase { Id = 3, ParentId = 0, Text = "一级节点 B" },
                new TreeItemBase { Id = 4, ParentId = 3, Text = "二级节点 B1" }
            };
        }

        // ========== 批次⑲ / #111：勾选模式下面板与输入框必须同源 ==========
        //
        // 数据形状刻意做成「A 有两个孩子、B 只有一个孩子」：
        // A 用来验 EP 的半选（子节点没全选完），B 用来验 EP reInitChecked 的
        // 「子节点全部选中 ⇒ 父节点转为选中（不是半选）」那条独子链折算。
        // 渲染顺序（深度优先）：A(0)、A-1(1)、A-2(2)、B(3)、B-1(4)。
        //
        // 用例一律用 input 的 change 事件驱动复选框（本仓 ElCheckbox 用例的既有口径）：
        // bUnit 不跑真实布局，点 label 是否连带触发内部 input 的 change 在这里不可靠。
        // 「点框的 click 会不会冒泡到节点文字那条 onclick 而被翻两次」是浏览器行为，
        // bUnit 量不到，由 demo/tools/probes/verify-treeselect-b19.cjs 在真实 DOM 上守。

        [Fact]
        public void CheckBoxModeSelectingLeafChecksOnlyThatBoxAndEchoMatches()
        {
            int? value = null;
            var cut = Render<ElTreeSelect>(parameters => parameters
                .Add(x => x.Data, CheckBoxTreeItems())
                .Add(x => x.ShowCheckbox, true)
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-select").Click();
            var fragment = RenderTreeDropdown(GetDropdown());

            Assert.Equal(5, fragment.FindAll(".el-checkbox").Count);
            Assert.Equal(0, fragment.FindAll(".el-checkbox.is-checked").Count);

            fragment.FindAll("input.el-checkbox__original")[1].Change(true);

            Assert.Equal(2, value);
            var boxes = fragment.FindAll(".el-checkbox");
            Assert.Equal(new[] { false, true, false, false, false },
                boxes.Select(x => x.ClassList.Contains("is-checked")).ToArray());
            Assert.Contains("is-indeterminate", boxes[0].ClassList);
            Assert.Equal("mixed", boxes[0].GetAttribute("aria-checked"));
            Assert.Equal("一级节点 A > 二级节点 A-1", cut.Find("input").GetAttribute("value"));
        }

        [Fact]
        public void CheckBoxModeSecondCheckReplacesTheFirstInsteadOfAccumulating()
        {
            // 改前的正身：勾选只是把 node.Checked 置真、从不清除上一个，
            // 于是面板同时挂着两个勾、输入框只写着一个。
            int? value = null;
            var cut = Render<ElTreeSelect>(parameters => parameters
                .Add(x => x.Data, CheckBoxTreeItems())
                .Add(x => x.ShowCheckbox, true)
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-select").Click();
            var fragment = RenderTreeDropdown(GetDropdown());
            var boxes = fragment.FindAll("input.el-checkbox__original");

            boxes[1].Change(true);
            fragment.FindAll("input.el-checkbox__original")[2].Change(true);

            Assert.Equal(5, value);
            Assert.Single(fragment.FindAll(".el-checkbox.is-checked"));
            // 复选框本体没有文字（节点文字在 .el-tree-node__label 里），所以按行定位再查框的状态。
            var checkedRow = fragment.FindAll(".el-tree-node__content")
                .Single(x => x.TextContent.Contains("二级节点 A-2"))
                .QuerySelector(".el-checkbox");
            Assert.Contains("is-checked", checkedRow.ClassList);
            var earlierRow = fragment.FindAll(".el-tree-node__content")
                .Single(x => x.TextContent.Contains("二级节点 A-1"))
                .QuerySelector(".el-checkbox");
            Assert.DoesNotContain("is-checked", earlierRow.ClassList);
            Assert.Equal("一级节点 A > 二级节点 A-2", cut.Find("input").GetAttribute("value"));
        }

        [Fact]
        public void CheckBoxModeParentCheckCollapsesToFirstLeafAndStaysIndeterminate()
        {
            int? value = null;
            var cut = Render<ElTreeSelect>(parameters => parameters
                .Add(x => x.Data, CheckBoxTreeItems())
                .Add(x => x.ShowCheckbox, true)
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-select").Click();
            var fragment = RenderTreeDropdown(GetDropdown());

            fragment.FindAll("input.el-checkbox__original")[0].Change(true);

            // EP：勾父节点写入的值是「该子树第一个可用叶子」，父节点自身保持半选。
            Assert.Equal(2, value);
            var boxes = fragment.FindAll(".el-checkbox");
            Assert.Contains("is-indeterminate", boxes[0].ClassList);
            Assert.DoesNotContain("is-checked", boxes[0].ClassList);
            Assert.Single(fragment.FindAll(".el-checkbox.is-checked"));
            Assert.Equal("一级节点 A > 二级节点 A-1", cut.Find("input").GetAttribute("value"));
        }

        [Fact]
        public void CheckBoxModeReCheckingSameParentClearsValueAndEcho()
        {
            int? value = null;
            var cut = Render<ElTreeSelect>(parameters => parameters
                .Add(x => x.Data, CheckBoxTreeItems())
                .Add(x => x.ShowCheckbox, true)
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-select").Click();
            var fragment = RenderTreeDropdown(GetDropdown());
            var leaf = fragment.FindAll("input.el-checkbox__original")[0];

            leaf.Change(true);
            Assert.Equal(2, value);

            fragment.FindAll("input.el-checkbox__original")[0].Change(true);

            Assert.Null(value);
            Assert.Equal(0, fragment.FindAll(".el-checkbox.is-checked").Count);
            Assert.Equal(0, fragment.FindAll(".el-checkbox.is-indeterminate").Count);
            Assert.Equal(string.Empty, cut.Find("input").GetAttribute("value"));
        }

        [Fact]
        public void CheckBoxModeOnlyChildParentRendersFullyCheckedNotIndeterminate()
        {
            int? value = null;
            var cut = Render<ElTreeSelect>(parameters => parameters
                .Add(x => x.Data, CheckBoxTreeItems())
                .Add(x => x.ShowCheckbox, true)
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-select").Click();
            var fragment = RenderTreeDropdown(GetDropdown());

            fragment.FindAll("input.el-checkbox__original")[4].Change(true);

            // B 只有 B-1 一个孩子 ⇒ EP 的 reInitChecked 见 all=true 就把 B 转为「选中」（不是半选），且没有半选残留
            // （Temp/ep2140-tree-model-node.mjs:37-40）。
            Assert.Equal(4, value);
            var boxes = fragment.FindAll(".el-checkbox");
            Assert.Contains("is-checked", boxes[3].ClassList);
            Assert.DoesNotContain("is-indeterminate", boxes[3].ClassList);
            Assert.Equal(0, fragment.FindAll(".el-checkbox.is-indeterminate").Count);
            Assert.Equal("一级节点 B > 二级节点 B-1", cut.Find("input").GetAttribute("value"));
        }

        [Fact]
        public void CheckBoxModeClickingNodeTextDoesNotWriteValueButExpands()
        {
            // EP 的 onNodeClick：showCheckbox 时点文字不调 handleOptionSelect，只做「设为当前节点 + 展开」。
            int? value = null;
            var cut = Render<ElTreeSelect>(parameters => parameters
                .Add(x => x.Data, CheckBoxTreeItems())
                .Add(x => x.ShowCheckbox, true)
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-select").Click();
            var fragment = RenderTreeDropdown(GetDropdown());

            Assert.Equal(5, fragment.FindAll(".el-tree-node").Count);

            fragment.FindAll(".el-tree-node__content")[0].Click();

            Assert.Null(value);
            Assert.Equal(0, fragment.FindAll(".el-checkbox.is-checked").Count);
            Assert.Equal(string.Empty, cut.Find("input").GetAttribute("value"));
            // ExpandOnClickNode 默认值对齐 EP（true）：点父节点文字应把它的子树收起。
            Assert.Equal(3, fragment.FindAll(".el-tree-node").Count);
        }

        [Fact]
        public void CheckBoxModeStrictlyUsesOwnIdAndNeverShowsIndeterminate()
        {
            int? value = null;
            var cut = Render<ElTreeSelect>(parameters => parameters
                .Add(x => x.Data, CheckBoxTreeItems())
                .Add(x => x.ShowCheckbox, true)
                .Add(x => x.CheckStrictly, true)
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-select").Click();
            var fragment = RenderTreeDropdown(GetDropdown());

            // EP 下发给树的 expandOnClickNode 是 `!checkStrictly && props.expandOnClickNode`
            // （Temp/ep2140-treeselect-tree.mjs:66-68）：严格模式下点父节点文字既不该写值，也不该顺带收起子树。
            fragment.FindAll(".el-tree-node__content")[0].Click();
            Assert.Null(value);
            Assert.Equal(5, fragment.FindAll(".el-tree-node").Count);

            fragment.FindAll("input.el-checkbox__original")[0].Change(true);

            Assert.Equal(1, value);
            Assert.Equal("一级节点 A", cut.Find("input").GetAttribute("value"));
            var boxes = fragment.FindAll(".el-checkbox");
            Assert.Contains("is-checked", boxes[0].ClassList);
            Assert.Equal(0, fragment.FindAll(".el-checkbox.is-indeterminate").Count);
            Assert.Equal(5, fragment.FindAll(".el-tree-node").Count);
        }

        [Fact]
        public void CheckBoxModeInitialValueRendersMatchingCheckInPanel()
        {
            // EP 的 watch([modelValue, tree]) 一开局就把 setCheckedKeys(值) 下发给树：
            // 预置值必须能在面板里看到对应的勾，而不是「输入框有字、面板全空」。
            var cut = Render<ElTreeSelect>(parameters => parameters
                .Add(x => x.Data, CheckBoxTreeItems())
                .Add(x => x.ShowCheckbox, true)
                .Add(x => x.Value, (int?)5));

            cut.Find(".el-select").Click();
            var fragment = RenderTreeDropdown(GetDropdown());

            var boxes = fragment.FindAll(".el-checkbox");
            Assert.Single(fragment.FindAll(".el-checkbox.is-checked"));
            Assert.Contains("is-indeterminate", boxes[0].ClassList);
            Assert.Equal("一级节点 A > 二级节点 A-2", cut.Find("input").GetAttribute("value"));

            fragment.FindAll("input.el-checkbox__original")[2].Change(true);

            Assert.Equal(0, fragment.FindAll(".el-checkbox.is-checked").Count);
            Assert.Equal(string.Empty, cut.Find("input").GetAttribute("value"));
        }

        private static List<TreeItemBase> CheckBoxTreeItems()
        {
            return new List<TreeItemBase>
            {
                new TreeItemBase { Id = 1, ParentId = 0, Text = "一级节点 A" },
                new TreeItemBase { Id = 2, ParentId = 1, Text = "二级节点 A-1" },
                new TreeItemBase { Id = 5, ParentId = 1, Text = "二级节点 A-2" },
                new TreeItemBase { Id = 3, ParentId = 0, Text = "一级节点 B" },
                new TreeItemBase { Id = 4, ParentId = 3, Text = "二级节点 B-1" }
            };
        }
    }
}
