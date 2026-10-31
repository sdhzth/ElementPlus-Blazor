using Bunit;
using ElementPlus;
using ElementPlus.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElTreeTests : BunitContext
    {
        public ElTreeTests()
        {
            Services.AddElementPlusServices();
        }

        private static List<TreeItemBase> FlatTree() => new List<TreeItemBase>
        {
            new TreeItemBase { Id = 1, ParentId = 0, Text = "Root" },
            new TreeItemBase { Id = 2, ParentId = 0, Text = "Child" }
        };

        [Fact]
        public void CheckOnClickLeafTogglesCheckbox()
        {
            var cut = Render<ElTree>(p => p
                .Add(x => x.ShowCheckbox, true)
                .Add(x => x.CheckOnClickLeaf, true)
                .Add(x => x.AutoExpandAll, false)
                .Add(x => x.DataSource, FlatTree()));

            cut.Find(".el-tree-node__content").Click();

            cut.WaitForAssertion(() =>
                Assert.Contains(cut.Instance.GetCheckedNodes(), n => n.Id == 1), timeout: TestWait.Timeout);
        }

        [Fact]
        public void AccordionCollapsesSiblings()
        {
            var data = new List<TreeItemBase>
            {
                new TreeItemBase { Id = 1, ParentId = 0, Text = "A", Expanded = false, HasChildren = true },
                new TreeItemBase { Id = 2, ParentId = 0, Text = "B", Expanded = false, HasChildren = true },
                new TreeItemBase { Id = 3, ParentId = 1, Text = "A1" },
                new TreeItemBase { Id = 4, ParentId = 2, Text = "B1" }
            };
            var cut = Render<ElTree>(p => p
                .Add(x => x.Accordion, true)
                .Add(x => x.AutoExpandAll, false)
                .Add(x => x.DataSource, data));

            // 展开 A：A 展开、兄弟 B 保持收起
            cut.InvokeAsync(() => cut.Instance.ExpandNode(1));
            Assert.True(cut.Instance.GetNode(1).Expanded);
            Assert.False(cut.Instance.GetNode(2).Expanded);

            // 再展开 B：accordion 模式下 A 应收起、B 保持展开
            cut.InvokeAsync(() => cut.Instance.ExpandNode(2));
            Assert.True(cut.Instance.GetNode(2).Expanded);
            Assert.False(cut.Instance.GetNode(1).Expanded);
        }

        [Fact]
        public void SetCheckedKeysChecksOnlyGivenNodes()
        {
            var cut = Render<ElTree>(p => p
                .Add(x => x.ShowCheckbox, true)
                .Add(x => x.AutoExpandAll, false)
                .Add(x => x.DataSource, FlatTree()));

            cut.InvokeAsync(() => cut.Instance.SetCheckedKeys(new List<int> { 2 }));

            var checkedKeys = cut.Instance.GetCheckedKeys();
            Assert.Contains(2, checkedKeys);
            Assert.DoesNotContain(1, checkedKeys);
        }

        [Fact]
        public void FilterShowsOnlyMatching()
        {
            var data = new List<TreeItemBase>
            {
                new TreeItemBase { Id = 1, ParentId = 0, Text = "Root" },
                new TreeItemBase { Id = 2, ParentId = 1, Text = "Alpha" },
                new TreeItemBase { Id = 3, ParentId = 1, Text = "Beta" },
                new TreeItemBase { Id = 4, ParentId = 2, Text = "AlphaChild" },
                new TreeItemBase { Id = 5, ParentId = 3, Text = "BetaChild" }
            };
            var cut = Render<ElTree>(p => p
                .Add(x => x.AutoExpandAll, true)
                .Add(x => x.DataSource, data));

            cut.InvokeAsync(() => cut.Instance.Filter("Alpha"));

            cut.WaitForAssertion(() =>
            {
                Assert.Contains("Alpha", cut.Markup);
                Assert.DoesNotContain("Beta", cut.Markup);
            }, timeout: TestWait.Timeout);
        }

        /// <summary>
        /// EP 的 el-tree 只有一个 current-node-key：逐个点过之后，高亮必须只剩最后点的那一个。
        /// 回归来自 zyweb 员工维护的部门树（点一遍后所有点过的节点都还亮着）。
        /// </summary>
        [Fact]
        public void ClickingSeveralNodesHighlightsOnlyTheLastOne()
        {
            List<TreeItemBase> selected = null;
            var cut = Render<ElTree>(p => p
                .Add(x => x.DataSource, FlatTree())
                .Add(x => x.SelectedNodesChanged, next => selected = next.ToList()));

            // 每次点击都会重渲染整棵树，元素上的事件 ID 随之失效 ⇒ 必须重新 FindAll 再点（bUnit 口径）
            cut.InvokeAsync(() => cut.FindAll(".el-tree-node__content")[0].Click());
            cut.WaitForAssertion(() => Assert.Single(
                cut.FindAll(".el-tree-node").Where(n => n.ClassList.Contains("is-current"))), timeout: TestWait.Timeout);
            cut.InvokeAsync(() => cut.FindAll(".el-tree-node__content")[1].Click());

            cut.WaitForAssertion(() =>
            {
                var current = cut.FindAll(".el-tree-node")
                    .Where(n => n.ClassList.Contains("is-current"))
                    .ToList();
                Assert.Single(current);
                Assert.NotNull(selected);
                Assert.Equal(new[] { 2 }, selected.Select(x => x.Id).ToArray());
            }, timeout: TestWait.Timeout);
        }
    }
}
