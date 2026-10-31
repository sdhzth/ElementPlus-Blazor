using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElTableV2Tests : BunitContext
    {
        private class Product
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
        }

        private class TreeNode
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public List<TreeNode> Children { get; set; }
        }

        private static List<Product> Data() => Enumerable.Range(1, 50)
            .Select(i => new Product { Id = i, Name = $"P{i}", Price = i * 10 })
            .ToList();

        public ElTableV2Tests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddElementPlusServices();
        }

        private static IList<TableV2Column> ThreeColumns() => new List<TableV2Column>
        {
            new TableV2Column { Key = "Id", Property = "Id", Title = "编号", Width = "80" },
            new TableV2Column { Key = "Name", Property = "Name", Title = "名称", Width = "120" },
            new TableV2Column { Key = "Price", Property = "Price", Title = "价格", Width = "100" }
        };

        [Fact]
        public void DefaultRowAndHeaderHeightAlignedWithEp()
        {
            var cut = Render<ElTableV2>();
            Assert.Equal(50, cut.Instance.RowHeight);
            Assert.Equal(50, cut.Instance.HeaderHeight);
        }

        [Fact]
        public void DataAliasPopulatesRows()
        {
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.Data, Data());
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
            });
            // Rows 是 protected；用渲染出的 DOM 行数（虚拟窗口）验证数据被吃进来了
            Assert.True(cut.FindAll(".el-table-v2__body .el-table-v2__row").Count > 0);
            Assert.Contains("P1", cut.Markup);
        }

        [Fact]
        public void AutoGenerateColumnsCreatesFromFirstRow()
        {
            var cut = Render<ElTableV2>(p => p.Add(x => x.DataSource, Data()));
            Assert.Equal(3, cut.Instance.Columns.Count);
            Assert.All(cut.Instance.Columns, c => Assert.False(string.IsNullOrEmpty(c.Key)));
        }

        [Fact]
        public void ColumnWidthLocksFlexBasis()
        {
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
            });
            // 修复：显式 Width 必须锁定 flex-basis，否则 flex:1 0 120px 会让列宽失效
            Assert.Contains("flex:0 0 80px", cut.Markup);
            Assert.Contains("width:80px", cut.Markup);
        }

        [Fact]
        public void ColumnFlexGrowApplies()
        {
            var columns = new List<TableV2Column>
            {
                new TableV2Column { Key = "Name", Property = "Name", Title = "名称", FlexGrow = 2, FlexShrink = 0 }
            };
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, columns);
                p.Add(x => x.AutoGenerateColumns, false);
            });
            Assert.Contains("flex:2 0 auto", cut.Markup);
        }

        [Fact]
        public void VirtualWindowRendersOnlyVisibleRows()
        {
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.StartIndex, 10);
                p.Add(x => x.ItemCount, 5);
            });
            Assert.Equal(5, cut.FindAll(".el-table-v2__body .el-table-v2__row").Count);
            Assert.Contains("P11", cut.Markup);
        }

        [Fact]
        public void SortableHeader_Click_EmitsColumnSortAndCyclesOrder()
        {
            TableV2ColumnSortArgs received = null;
            var columns = ThreeColumns();
            columns[1].Sortable = true;
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, columns);
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.ColumnSort, EventCallback.Factory.Create<TableV2ColumnSortArgs>(this, args => received = args));
            });
            var header = cut.Find(".el-table-v2__header-cell-sortable");
            header.Click();
            Assert.NotNull(received);
            Assert.Equal("Name", received.Key);
            Assert.Equal(TableV2SortOrder.Asc, received.Order);
            Assert.Contains("sort-asc", cut.Markup);

            header = cut.Find(".el-table-v2__header-cell-sortable");
            header.Click();
            Assert.Equal(TableV2SortOrder.Desc, received.Order);
            Assert.Contains("sort-desc", cut.Markup);
        }

        [Fact]
        public void SortByControlled_SortByChangedFiresWithoutInternalMutation()
        {
            TableV2SortBy changed = null;
            var columns = ThreeColumns();
            columns[0].Sortable = true;
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, columns);
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.SortBy, new TableV2SortBy("Id", TableV2SortOrder.Desc));
                p.Add(x => x.SortByChanged, EventCallback.Factory.Create<TableV2SortBy>(this, v => changed = v));
            });
            cut.Find(".el-table-v2__header-cell-sortable").Click();
            Assert.NotNull(changed);
            Assert.Equal(TableV2SortOrder.Asc, changed.Order);
            // 受控：组件内部不改写 SortBy（仍为 Desc，由调用方决定是否更新）
            Assert.Equal(TableV2SortOrder.Desc, cut.Instance.SortBy.Order);
        }

        [Fact]
        public void SortStateResolvesOrder()
        {
            var columns = ThreeColumns();
            columns[2].Sortable = true;
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, columns);
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.SortState, new Dictionary<string, TableV2SortOrder> { ["Price"] = TableV2SortOrder.Desc });
            });
            Assert.Contains("sort-desc", cut.Markup);
        }

        [Fact]
        public void NonSortableHeader_DoesNotRenderSortIcon()
        {
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
            });
            Assert.DoesNotContain("el-table-v2__sort-icon", cut.Markup);
        }

        [Fact]
        public void TreeData_FlattensAndIndentsByDepth()
        {
            var data = new List<TreeNode>
            {
                new TreeNode
                {
                    Id = 1, Name = "节点1",
                    Children = new List<TreeNode>
                    {
                        new TreeNode { Id = 11, Name = "子节点11" },
                        new TreeNode { Id = 12, Name = "子节点12" }
                    }
                },
                new TreeNode { Id = 2, Name = "节点2" }
            };
            var columns = new List<TableV2Column>
            {
                new TableV2Column { Key = "Name", Property = "Name", Title = "名称" }
            };
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, data);
                p.Add(x => x.Columns, columns);
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.RowKey, "Id");
                p.Add(x => x.ExpandColumnKey, "Name");
                p.Add(x => x.DefaultExpandAllRows, true);
            });
            // 展开后 4 行：节点1 + 2 子 + 节点2
            Assert.Equal(4, cut.FindAll(".el-table-v2__body .el-table-v2__row").Count);
            Assert.Contains("子节点11", cut.Markup);
            // 子行缩进占位 = depth(1) * IndentSize(12)（叶子行同样缩进对齐）
            Assert.Contains("width:12px", cut.Markup);
            // 父行箭头展开态
            Assert.Contains("el-table-v2__expand-icon is-expanded", cut.Markup);
        }

        [Fact]
        public void TreeData_CollapsedByDefaultAndToggleEmitsChange()
        {
            var data = new List<TreeNode>
            {
                new TreeNode
                {
                    Id = 1, Name = "节点1",
                    Children = new List<TreeNode> { new TreeNode { Id = 11, Name = "子节点11" } }
                }
            };
            var columns = new List<TableV2Column> { new TableV2Column { Key = "Name", Property = "Name", Title = "名称" } };
            IEnumerable<object> changed = null;
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, data);
                p.Add(x => x.Columns, columns);
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.RowKey, "Id");
                p.Add(x => x.ExpandColumnKey, "Name");
                p.Add(x => x.ExpandedRowsChange, EventCallback.Factory.Create<IEnumerable<object>>(this, v => changed = v));
            });
            Assert.DoesNotContain("子节点11", cut.Markup);
            cut.Find(".el-table-v2__expand-icon").Click();
            Assert.Contains("子节点11", cut.Markup);
            Assert.NotNull(changed);
            Assert.Equal(1, changed.Count());
        }

        [Fact]
        public void DefaultExpandRowKeysSeedsOnce()
        {
            var data = new List<TreeNode>
            {
                new TreeNode
                {
                    Id = 1, Name = "节点1",
                    Children = new List<TreeNode> { new TreeNode { Id = 11, Name = "子节点11" } }
                }
            };
            var columns = new List<TableV2Column> { new TableV2Column { Key = "Name", Property = "Name", Title = "名称" } };
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, data);
                p.Add(x => x.Columns, columns);
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.RowKey, "Id");
                p.Add(x => x.ExpandColumnKey, "Name");
                p.Add(x => x.DefaultExpandRowKeys, new object[] { 1 });
            });
            Assert.Contains("子节点11", cut.Markup);
        }

        [Fact]
        public void ExpandContent_RendersDedicatedColumnAndExpandedRow()
        {
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.RowKey, "Id");
                p.Add(x => x.ExpandContent, row => builder =>
                {
                    builder.AddContent(0, $"详情-{((Product)row).Name}");
                });
                p.Add(x => x.ExpandedRows, new HashSet<object> { 1 });
            });
            // 旧版扩展：独立展开列
            Assert.Contains("el-table-v2__expand-column", cut.Markup);
            Assert.Contains("详情-P1", cut.Markup);
            Assert.Equal(1, cut.FindAll(".el-table-v2__expanded-row").Count);
        }

        [Fact]
        public void ExpandColumnKey_RendersArrowInsideCell()
        {
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.RowKey, "Id");
                p.Add(x => x.ExpandColumnKey, "Name");
                p.Add(x => x.ExpandContent, row => builder => builder.AddContent(0, "x"));
                p.Add(x => x.StartIndex, 0);
                p.Add(x => x.ItemCount, 50);
            });
            // EP 方式：箭头在 expand-column-key 列的单元格内，不再有独立展开列
            Assert.DoesNotContain("el-table-v2__expand-column", cut.Markup);
            Assert.Equal(50, cut.FindAll(".el-table-v2__expand-icon").Count);
        }

        [Fact]
        public void FixedData_RendersStickyRows()
        {
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.FixedData, new[] { new Product { Id = 999, Name = "置顶行", Price = 1 } });
            });
            Assert.Equal(1, cut.FindAll(".el-table-v2__fixed-row").Count);
            Assert.Contains("置顶行", cut.Markup);
            Assert.Contains("position:sticky", cut.Markup);
        }

        [Fact]
        public void FooterContent_RendersWithHeight()
        {
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.FooterContent, builder => builder.AddContent(0, "共 50 条"));
            });
            var footer = cut.Find(".el-table-v2__footer");
            Assert.Contains("共 50 条", footer.TextContent);
            Assert.Contains("height:50px", footer.GetAttribute("style"));
        }

        [Fact]
        public void FooterHeightZero_RendersNoFooter()
        {
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
            });
            Assert.Empty(cut.FindAll(".el-table-v2__footer"));
        }

        [Fact]
        public void OverlayContent_RendersOverlay()
        {
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.OverlayContent, builder => builder.AddContent(0, "自定义遮罩"));
            });
            Assert.Equal(1, cut.FindAll(".el-table-v2__overlay").Count);
            Assert.Contains("自定义遮罩", cut.Markup);
        }

        [Fact]
        public void EmptyState_RendersEmptyClassAndCustomContent()
        {
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, new List<Product>());
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.EmptyContent, builder => builder.AddContent(0, "没有数据"));
            });
            Assert.Equal(1, cut.FindAll(".el-table-v2__empty").Count);
            Assert.Contains("没有数据", cut.Markup);
        }

        [Fact]
        public void SummaryRow_SumsNumericColumn()
        {
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, new List<Product>
                {
                    new Product { Id = 1, Name = "A", Price = 10 },
                    new Product { Id = 2, Name = "B", Price = 15 }
                });
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.ShowSummary, true);
                p.Add(x => x.SumText, "总计");
            });
            var summary = cut.Find(".el-table-v2__summary-row");
            Assert.Contains("总计", summary.TextContent);
            Assert.Contains("25", summary.TextContent);
        }

        [Fact]
        public void StripeBorderAndHighlightClasses()
        {
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.Stripe, true);
                p.Add(x => x.Border, true);
                p.Add(x => x.HighlightCurrentRow, true);
                p.Add(x => x.CurrentRowIndex, 2);
            });
            var root = cut.Find(".el-table-v2");
            Assert.Contains("el-table--border", root.GetAttribute("class"));
            Assert.Contains("el-table--striped", root.GetAttribute("class"));
            Assert.Contains("el-table--highlight-current-row", root.GetAttribute("class"));
            Assert.Contains("current-row", cut.Markup);
            Assert.Contains("el-table__row--striped", cut.Markup);
        }

        [Fact]
        public void RowPropsAndRowClassGetterApply()
        {
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.RowProps, row => new Dictionary<string, object> { ["data-test"] = $"r{((Product)row).Id}" });
                p.Add(x => x.RowClassGetter, ctx => ctx.RowIndex % 2 == 0 ? "even-row" : null);
            });
            Assert.Contains("data-test=\"r1\"", cut.Markup);
            Assert.Contains("even-row", cut.Markup);
        }

        [Fact]
        public void HeaderClassAppliesToHeaderRow()
        {
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.HeaderClass, "my-header");
            });
            Assert.Contains("el-table-v2__header-row my-header", cut.Markup);
        }

        [Fact]
        public void RowEventsFire()
        {
            object clicked = null;
            object dbl = null;
            object ctx = null;
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.OnRowClick, EventCallback.Factory.Create<object>(this, v => clicked = v));
                p.Add(x => x.OnRowDoubleClick, EventCallback.Factory.Create<object>(this, v => dbl = v));
                p.Add(x => x.OnRowContextClick, EventCallback.Factory.Create<object>(this, v => ctx = v));
            });
            var row = cut.Find(".el-table-v2__body .el-table-v2__row");
            row.Click();
            row.DoubleClick();
            row.ContextMenu();
            Assert.Equal(1, ((Product)clicked).Id);
            Assert.Equal(1, ((Product)dbl).Id);
            Assert.Equal(1, ((Product)ctx).Id);
        }

        [Fact]
        public void ScrollToRow_InvokesJsHelper()
        {
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
            });
            cut.Instance.ScrollToRowAsync(5);
            var invocation = JSInterop.Invocations.Single(x => x.Identifier == "elementTableV2ScrollTo");
            Assert.Equal(250.0, invocation.Arguments[1]);
        }

        [Fact]
        public void ScrollToTopAndLeft_InvokeJsHelper()
        {
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
            });
            cut.Instance.ScrollToTopAsync();
            cut.Instance.ScrollToLeftAsync(120.0);
            var calls = JSInterop.Invocations.Where(x => x.Identifier == "elementTableV2ScrollTo").ToList();
            Assert.Equal(2, calls.Count);
            Assert.Equal(0.0, calls[0].Arguments[1]);
            Assert.Equal(120.0, calls[1].Arguments[2]);
        }

        [Fact]
        public void HeightWithPx_ParsesContainerHeight()
        {
            var cut = Render<ElTableV2>(p =>
            {
                p.Add(x => x.DataSource, Data());
                p.Add(x => x.Columns, ThreeColumns());
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.Height, "320px");
            });
            // 修复："320px" 不再回落 500
            Assert.Equal(320, cut.Instance.GetType().GetMethod("GetContainerHeight", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(cut.Instance, null));
        }

        // ==================== 截断单元格：EP 原生 title + 本库悬停浮层扩展 ====================

        /// <summary>
        /// bUnit 没有布局引擎，"文字是否真被裁"只能由 JS interop 喂进来
        /// （Loose 模式未打桩返回 default＝null，等价于"量不到＝没裁"）。
        /// </summary>
        private void StubTextOverflow(bool clipped, string text = "被裁掉的完整单元格文字")
            => JSInterop.Setup<TextClipState>(TextClip.MeasureFunction, _ => true)
                .SetResult(new TextClipState
                {
                    ScrollWidth = clipped ? 640 : 120,
                    ClientWidth = 120,
                    Text = text
                });

        private void RenderClippedColumns(ComponentParameterCollectionBuilder<ElTableV2> p, bool tableSwitch, bool? nameSwitch)
        {
            var columns = ThreeColumns();
            columns[1].EllipsisTooltip = nameSwitch;
            p.Add(x => x.DataSource, Data());
            p.Add(x => x.Columns, columns);
            p.Add(x => x.AutoGenerateColumns, false);
            p.Add(x => x.EllipsisTooltip, tableSwitch);
        }

        [Fact]
        public void CellsBindEpNativeTitleWhenTooltipOff()
        {
            // EP table-v2 的口径就是原生 title（components/cell.tsx:11-14），默认开着、零 DOM 形状变化。
            var cut = Render<ElTableV2>(p => RenderClippedColumns(p, tableSwitch: false, nameSwitch: null));

            Assert.Equal("1", cut.Find(".el-table-v2__body .el-table-v2__row [role=cell]").GetAttribute("title"));
            Assert.Empty(cut.FindAll(".el-tooltip__trigger"));
            Assert.Empty(cut.FindAll(".el-table-v2__cell-text"));
            Assert.DoesNotContain("is-ellipsis-tooltip", cut.Markup);
        }

        [Fact]
        public void EllipsisTooltipWrapsCellTextAndOpensCellClipping()
        {
            var cut = Render<ElTableV2>(p => RenderClippedColumns(p, tableSwitch: true, nameSwitch: null));

            // 浮层渲染在触发 span 内部，所以裁剪要从单元格让给内层文字 span：
            // 单元格加 is-ellipsis-tooltip（CSS 里换成 overflow:visible），否则浮层被单元格一起裁掉。
            var cell = cut.Find(".el-table-v2__body .el-table-v2__cell-text").ParentElement.ParentElement;
            Assert.Contains("is-ellipsis-tooltip", cell.ClassList);
            Assert.Contains("el-tooltip__trigger", cell.FirstElementChild.ClassList);
            // 挂了浮层就不给原生 title，免得浏览器提示与 elTooltip 叠成两层
            Assert.Null(cell.GetAttribute("title"));
        }

        [Fact]
        public void EllipsisTooltipShowsFullTextOnHoverWhenClipped()
        {
            StubTextOverflow(clipped: true, text: "P1 完整文字");
            var cut = Render<ElTableV2>(p => RenderClippedColumns(p, tableSwitch: true, nameSwitch: null));

            cut.Find(".el-table-v2__cell-text").TriggerEvent("onmouseenter", new MouseEventArgs());

            cut.WaitForAssertion(() =>
            {
                var popper = cut.Find(".el-tooltip__popper");
                Assert.Equal("P1 完整文字", popper.TextContent.Trim());
                Assert.Equal("tooltip", popper.GetAttribute("role"));
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public void EllipsisTooltipKeepsPopperHiddenWhenNotClipped()
        {
            StubTextOverflow(clipped: false);
            var cut = Render<ElTableV2>(p => RenderClippedColumns(p, tableSwitch: true, nameSwitch: null));

            cut.Find(".el-table-v2__cell-text").TriggerEvent("onmouseenter", new MouseEventArgs());

            // 反空转对照：确实去 JS 侧量过，只是结论是"没裁"
            cut.WaitForAssertion(() => Assert.Contains(TextClip.MeasureFunction,
                JSInterop.Invocations.Select(x => x.Identifier)), timeout: TestWait.Timeout);
            Assert.Empty(cut.FindAll(".el-tooltip__popper"));
        }

        [Fact]
        public void EllipsisTooltipHidesOnMouseLeave()
        {
            StubTextOverflow(clipped: true, text: "P1 完整文字");
            var cut = Render<ElTableV2>(p => RenderClippedColumns(p, tableSwitch: true, nameSwitch: null));
            var text = cut.Find(".el-table-v2__cell-text");

            text.TriggerEvent("onmouseenter", new MouseEventArgs());
            cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".el-tooltip__popper")), timeout: TestWait.Timeout);

            cut.Find(".el-table-v2__cell-text").TriggerEvent("onmouseleave", new MouseEventArgs());

            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".el-tooltip__popper")), timeout: TestWait.Timeout);
        }

        [Fact]
        public void ColumnSwitchOverridesTableSwitch()
        {
            var on = Render<ElTableV2>(p => RenderClippedColumns(p, tableSwitch: false, nameSwitch: true));
            // 只开了 Name 一列：每个渲染行的 Name 单元格各一层文字 span，别的列一概不动
            Assert.Equal(on.FindAll(".el-table-v2__body .el-table-v2__row").Count,
                on.FindAll(".el-table-v2__cell-text").Count);

            var off = Render<ElTableV2>(p => RenderClippedColumns(p, tableSwitch: true, nameSwitch: false));
            // 表级开着、Name 列点名关掉：少且只少 Name 那一列（三列 → 每行两包）
            var rows = off.FindAll(".el-table-v2__body .el-table-v2__row").Count;
            Assert.Equal(rows * 2, off.FindAll(".el-table-v2__cell-text").Count);
            var firstRowCells = off.Find(".el-table-v2__body .el-table-v2__row").Children
                .OfType<AngleSharp.Dom.IElement>().ToList();
            Assert.Equal("P1", firstRowCells[1].GetAttribute("title"));
            Assert.Null(firstRowCells[0].GetAttribute("title"));
        }

        [Fact]
        public void SummaryCellBindsEpNativeTitle()
        {
            var cut = Render<ElTableV2>(p =>
            {
                RenderClippedColumns(p, tableSwitch: false, nameSwitch: null);
                p.Add(x => x.ShowSummary, true);
            });

            var summaryCells = cut.FindAll(".el-table-v2__summary-row [role=cell]");
            Assert.Equal(3, summaryCells.Count);
            Assert.Equal("合计", summaryCells[0].GetAttribute("title"));
            // 价格列汇总出数字 → 有 title；名称列汇总不出数字（空串）→ 不给空 title
            Assert.Equal("12750", summaryCells[2].GetAttribute("title"));
            Assert.Null(summaryCells[1].GetAttribute("title"));
        }
    }
}
