using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElDataDisplayRemainingTests : BunitContext
    {
        public ElDataDisplayRemainingTests()
        {
            Services.AddElementPlusServices();
        }

        [Fact]
        public void InfiniteScrollRaisesLoadOnScrollAndShowsStates()
        {
            // 组件靠 JS 的 Element.getScrollDistance 判断「是否到底」；bUnit 里没有真实 DOM，
            // 不注册这个 mock 会走 catch 分支恒为 false，滚动永远不触发加载（首屏那 1 次是 Immediate 加载）。
            JSInterop.Mode = JSRuntimeMode.Loose;
            JSInterop.Setup<decimal>("Element.getScrollDistance", _ => true).SetResult(1m);

            var loads = new List<int>();
            var cut = Render<ElInfiniteScroll>(parameters => parameters
                .Add(x => x.Height, "120")
                .Add(x => x.Delay, 0)
                .Add(x => x.OnLoad, args => loads.Add(args.LoadCount))
                .AddChildContent("<div>Rows</div>"));

            Assert.Contains("height:120px", cut.Find(".el-infinite-scroll").GetAttribute("style"));
            cut.Find(".el-infinite-scroll").TriggerEvent("onscroll", EventArgs.Empty);
            cut.Find(".el-infinite-scroll").TriggerEvent("onscroll", EventArgs.Empty);

            Assert.Equal(new[] { 1, 2 }, loads);

            cut = Render<ElInfiniteScroll>(parameters => parameters
                .Add(x => x.Loading, true)
                .Add(x => x.Disabled, true)
                .AddChildContent("<div>Rows</div>"));

            Assert.Contains("is-loading", cut.Find(".el-infinite-scroll").ClassList);
            Assert.Equal("加载中", cut.Find(".el-infinite-scroll__loading").TextContent.Trim());
            Assert.Equal("没有更多了", cut.Find(".el-infinite-scroll__disabled").TextContent.Trim());
        }

        [Fact]
        public void TourStepsNavigateAndClose()
        {
            var open = true;
            var current = 0;
            var closed = 0;
            var changes = new List<int>();
            var steps = new List<TourStep>
            {
                new TourStep { Title = "Step 1", Description = "First", Placement = "right" },
                new TourStep { Title = "Step 2", Description = "Second" }
            };

            var cut = Render<ElTour>(parameters => parameters
                .Add(x => x.ModelValue, open)
                .Add(x => x.ModelValueChanged, next => open = next)
                .Add(x => x.Current, current)
                .Add(x => x.CurrentChanged, next => current = next)
                .Add(x => x.OnChange, next => changes.Add(next))
                .Add(x => x.OnClose, () => closed++)
                .Add(x => x.Steps, steps));

            Assert.Equal("Step 1", cut.Find(".el-tour__title").TextContent.Trim());
            Assert.Contains("is-right", cut.Find(".el-tour__content").ClassList);

            cut.FindAll(".el-tour__actions button")[1].Click();

            Assert.Equal(1, current);
            Assert.Equal(1, changes.Single());
            Assert.Equal("Step 2", cut.Find(".el-tour__title").TextContent.Trim());

            cut.FindAll(".el-tour__actions button")[1].Click();

            Assert.False(open);
            Assert.Equal(1, closed);
            Assert.Empty(cut.FindAll(".el-tour"));
        }

        [Fact]
        public void TreeFiltersTemplatesChecksLazyLoadsAndDrags()
        {
            var checkedNodes = new List<TreeItemBase>();
            TreeDragEventArgs dropped = null;
            var data = new List<TreeItemBase>
            {
                new TreeItemBase { Id = 1, ParentId = 0, Text = "Root", Expanded = true },
                new TreeItemBase { Id = 2, ParentId = 1, Text = "Alpha" },
                new TreeItemBase { Id = 3, ParentId = 1, Text = "Beta", HasChildren = true }
            };

            var cut = Render<ElTree>(parameters => parameters
                .Add(x => x.DataSource, data)
                .Add(x => x.FilterText, "Alpha")
                .Add(x => x.ShowCheckbox, true)
                .Add(x => x.NodeTemplate, node => builder =>
                {
                    builder.OpenElement(0, "strong");
                    builder.AddContent(1, node.Text);
                    builder.CloseElement();
                })
                .Add(x => x.CheckedNodesChanged, next => checkedNodes = next)
                .Add(x => x.Draggable, true)
                .Add(x => x.OnNodeDrop, args => dropped = args));

            Assert.Contains("Root", cut.Markup);
            Assert.Contains("Alpha", cut.Markup);
            Assert.DoesNotContain("Beta", cut.Markup);
            Assert.NotEmpty(cut.FindAll(".el-tree-node__label strong"));

            cut.FindAll(".el-checkbox__original")[0].Change(true);

            Assert.Contains(1, checkedNodes.Select(x => x.Id));
            Assert.Contains(2, checkedNodes.Select(x => x.Id));

            cut.FindAll(".el-tree-node")[1].DragStart(new Microsoft.AspNetCore.Components.Web.DragEventArgs());
            cut.FindAll(".el-tree-node")[0].Drop(new Microsoft.AspNetCore.Components.Web.DragEventArgs());

            Assert.NotNull(dropped);
            Assert.Equal(2, dropped.DraggingNode.Id);
            Assert.Equal(1, dropped.DropNode.Id);

            var loaded = false;
            var lazyData = new List<TreeItemBase>
            {
                new TreeItemBase { Id = 10, ParentId = 0, Text = "Lazy Root", Expanded = true },
                new TreeItemBase { Id = 11, ParentId = 10, Text = "Lazy Parent", HasChildren = true }
            };

            cut = Render<ElTree>(parameters => parameters
                .Add(x => x.DataSource, lazyData)
                .Add(x => x.FilterText, null)
                .Add(x => x.Lazy, true)
                .Add(x => x.LazyLoad, node =>
                {
                    loaded = true;
                    return Task.FromResult<IEnumerable<TreeItemBase>>(new[]
                    {
                        new TreeItemBase { Id = 4, Text = "Loaded" }
                    });
                }));

            // 必须按节点「自身 label」定位：子节点现在正确嵌套在父节点的 div 之内，
            // 用 TextContent 匹配会把父节点（Lazy Root 的文本里含 "Lazy Parent"）也命中，
            // 于是点到了父节点的箭头，惰性加载自然不触发。
            cut.FindAll(".el-tree-node")
                .First(x => (x.QuerySelector(".el-tree-node__label")?.TextContent ?? string.Empty).Trim() == "Lazy Parent")
                .QuerySelector(".el-tree-node__expand-icon")
                .Click();

            Assert.True(loaded);
            Assert.Contains("Loaded", cut.Markup);
        }

        [Fact]
        public void TableV2VirtualizesExpandsAndSummarizesRows()
        {
            var rows = Enumerable.Range(1, 8)
                .Select(i => new ProductRow { Name = $"Item {i}", Amount = i })
                .ToList();
            var expanded = new HashSet<object>();

            var cut = Render<ElTableV2>(parameters => parameters
                .Add(x => x.DataSource, rows)
                .Add(x => x.StartIndex, 2)
                .Add(x => x.ItemCount, 3)
                .Add(x => x.ShowSummary, true)
                .Add(x => x.ExpandedRows, expanded)
                .Add(x => x.ExpandContent, row => builder =>
                {
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "class", "expanded-product");
                    builder.AddContent(2, ((ProductRow)row).Name);
                    builder.CloseElement();
                }));

            Assert.DoesNotContain("Item 1", cut.Markup);
            Assert.Contains("Item 3", cut.Markup);
            Assert.Contains("Item 5", cut.Markup);
            Assert.Contains("36", cut.Find(".el-table-v2__summary-row").TextContent);

            // 展开箭头契约类名是 el-table-v2__expand-icon（+ is-expanded），不是旧版 el-table 的
            // el-table__expand-icon / el-table__expand-icon--expanded —— 依据：
            //   EP theme-chalk/table-v2.scss → .el-table-v2 .el-table-v2__row .el-table-v2__expand-icon
            //                                  .el-table-v2__expand-icon.is-expanded svg{transform:rotate(90deg)}
            //   本仓 index.css（.el-table-v2 .el-table-v2__expand-icon / .is-expanded）
            //   与 docs/table-v2-alignment.md 的 DOM 树（span.el-table-v2__expand-icon(.is-expanded)）
            var expander = cut.Find(".el-table-v2__expand-icon");

            expander.Click();

            Assert.Contains("expanded-product", cut.Markup);
            Assert.Contains("is-expanded", cut.Find(".el-table-v2__expand-icon").ClassList);

            // 再点一次必须收回去（展开态是可逆的，不能只测开不测关）
            cut.Find(".el-table-v2__expand-icon").Click();

            Assert.DoesNotContain("expanded-product", cut.Markup);
            Assert.DoesNotContain("is-expanded", cut.Find(".el-table-v2__expand-icon").ClassList);
        }

        [Fact]
        public void TreeV2UsesTreePipeline()
        {
            var cut = Render<ElTreeV2>(parameters => parameters
                .Add(x => x.DataSource, new List<TreeItemBase>
                {
                    new TreeItemBase { Id = 1, ParentId = 0, Text = "Root" }
                }));

            Assert.Contains("el-tree", cut.Find("[role='tree']").ClassList);
            Assert.Contains("Root", cut.Markup);
        }

        private class ProductRow
        {
            public string Name { get; set; }

            public int Amount { get; set; }
        }
    }
}
