using Bunit;
using ElementPlus;
using ElementPlus.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElTreeV2Tests : BunitContext
    {
        public ElTreeV2Tests()
        {
            Services.AddElementServices();
        }

        private static List<TreeItemBase> FlatTree() => new List<TreeItemBase>
        {
            new TreeItemBase { Id = 1, ParentId = 0, Text = "Apple" },
            new TreeItemBase { Id = 2, ParentId = 0, Text = "Banana" }
        };

        [Fact]
        public void ScrollbarAlwaysOnAddsClass()
        {
            var cut = Render<ElTreeV2>(p => p
                .Add(x => x.ScrollbarAlwaysOn, true)
                .Add(x => x.DataSource, FlatTree()));

            Assert.Contains("is-scrollbar-always-on", cut.Find(".el-tree-v2").ClassList);
        }

        [Fact]
        public void FilterMethodHidesNonMatching()
        {
            var cut = Render<ElTreeV2>(p => p
                .Add(x => x.DataSource, FlatTree())
                .Add(x => x.FilterMethod, (node, text) => (node.Text ?? "").Contains(text)));

            cut.InvokeAsync(() => cut.Instance.Filter("Apple"));

            cut.WaitForAssertion(() =>
            {
                Assert.Contains("Apple", cut.Markup);
                Assert.DoesNotContain("Banana", cut.Markup);
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public void SetExpandedKeysExpandsGivenNodes()
        {
            var data = new List<TreeItemBase>
            {
                new TreeItemBase { Id = 1, ParentId = 0, Text = "Root", HasChildren = true },
                new TreeItemBase { Id = 2, ParentId = 1, Text = "Leaf" }
            };
            var cut = Render<ElTreeV2>(p => p
                .Add(x => x.AutoExpandAll, false)
                .Add(x => x.DataSource, data));

            cut.InvokeAsync(() => cut.Instance.SetExpandedKeys(new List<int> { 1 }));

            Assert.True(cut.Instance.GetNode(1).Expanded);
            Assert.Contains("Leaf", cut.Markup);
        }

        [Fact]
        public void CheckOnClickLeafTogglesCheckbox()
        {
            var cut = Render<ElTreeV2>(p => p
                .Add(x => x.ShowCheckbox, true)
                .Add(x => x.CheckOnClickLeaf, true)
                .Add(x => x.AutoExpandAll, false)
                .Add(x => x.DataSource, FlatTree()));

            cut.Find(".el-tree-node__content").Click();

            cut.WaitForAssertion(() =>
                Assert.Contains(cut.Instance.GetCheckedNodes(), n => n.Id == 1), timeout: TestWait.Timeout);
        }
    }
}
