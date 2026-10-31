using AngleSharp.Dom;
using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElTable 列筛选：点击展开的面板（EP filter-panel.vue 两态）+ 本库扩展的条件式面板。
    /// EP 口径实抓（packages/components/table/src/filter-panel.vue）：
    /// filterMultiple 为真 → <c>__multiple</c> 勾选组 +「筛选 / 重置」（confirmFilter 才 commit('filterChange')）；
    /// 为假 → <c>__list</c> 单选 radiogroup，首项是 clearFilter（EP 中文值是「全部」，不是「清除」），
    /// handleSelect 里 confirmFilter + hidden()（点完即收起）。
    /// </summary>
    public class ElTableFilterPanelTests : BunitContext
    {
        public ElTableFilterPanelTests()
        {
            Services.AddElementServices();
            Services.AddSingleton(new HttpClient());
        }

        public class Row
        {
            public string Name { get; set; }
            public string Category { get; set; }
            public int Stock { get; set; }
        }

        private static List<Row> Rows() => new()
        {
            new Row { Name = "阿莫西林", Category = "西药", Stock = 300 },
            new Row { Name = "板蓝根", Category = "中成药", Stock = 10 },
            new Row { Name = "维生素C", Category = "保健品", Stock = 90 },
        };

        private static IList<TableFilterOption> CategoryFilters() => new List<TableFilterOption>
        {
            new TableFilterOption { Text = "西药", Value = "西药" },
            new TableFilterOption { Text = "保健品", Value = "保健品" },
        };

        /// <summary>
        /// 面板坐标来自漏斗的视口矩形（一次 JS 往返）。bUnit 没有布局引擎，这里给一个固定读数：
        /// 左侧够宽（1200）、下方只剩 100px（低于 TableFilterPopper.MinRoomBelow）→ 应当翻到上方。
        /// </summary>
        private void StubTriggerRect(double top = 120, double left = 40, double height = 24, double viewportHeight = 200)
        {
            JSInterop.Setup<ElementViewportRect>(TableFilterPopper.MeasureFunction, _ => true)
                .SetResult(new ElementViewportRect
                {
                    Left = left,
                    Top = top,
                    Right = left + 24,
                    Bottom = top + height,
                    Width = 24,
                    Height = height,
                    Vw = 1200,
                    Vh = viewportHeight
                });
        }

        private IRenderedComponent<ElTable> RenderTable(
            bool advancedNameColumn = false,
            bool filterMultiple = true,
            EventCallback<(string Prop, object[] Values)> filterChange = default)
        {
            var child = new RenderFragment(b =>
            {
                var seq = 0;
                b.OpenComponent<ElTableColumn>(seq++);
                b.AddAttribute(seq++, "Property", "Name");
                b.AddAttribute(seq++, "Text", "名称");
                b.AddAttribute(seq++, "AdvancedFilter", advancedNameColumn);
                b.CloseComponent();

                b.OpenComponent<ElTableColumn>(seq++);
                b.AddAttribute(seq++, "Property", "Category");
                b.AddAttribute(seq++, "Text", "类别");
                b.AddAttribute(seq++, "Filters", CategoryFilters());
                b.AddAttribute(seq++, "FilterMultiple", filterMultiple);
                b.CloseComponent();

                b.OpenComponent<ElTableColumn>(seq++);
                b.AddAttribute(seq++, "Property", "Stock");
                b.AddAttribute(seq++, "Text", "库存");
                b.CloseComponent();
            });

            return Render<ElTable>(p =>
            {
                p.Add(x => x.DataSource, Rows());
                p.Add(x => x.AutoGenerateColumns, false);
                p.Add(x => x.HasSelectionColumn, false);
                p.Add(x => x.EnablePagination, false);
                p.AddChildContent(child);
                if (filterChange.HasDelegate)
                {
                    p.Add(x => x.FilterChange, filterChange);
                }
            });
        }

        /// <summary>表体每行第一列（名称列）的文字，用来核对筛选后的行集。</summary>
        private static IReadOnlyList<string> BodyNames(IRenderedComponent<ElTable> cut) =>
            cut.FindAll(".el-table__body tbody tr")
                .Select(tr => tr.QuerySelector("td .cell")?.TextContent.Trim())
                .ToList();

        /// <summary>按表头文字找到该列的漏斗（列顺序是演示里的事实，写死下标一改就错）。</summary>
        private static IElement TriggerOf(IRenderedComponent<ElTable> cut, string text)
        {
            var th = cut.FindAll(".el-table__header th").First(x => x.TextContent.Contains(text));
            return th.QuerySelector(".el-table__column-filter-trigger");
        }

        /// <summary>勾选草稿：bUnit 里点 label 不会触发 input 的 change，直接 Change 原生 input。</summary>
        private static void CheckOption(IRenderedComponent<ElTable> cut, int index = 0) =>
            cut.FindAll(".el-table-filter__checkbox-group .el-checkbox__original")[index].Change(true);

        [Fact]
        public async Task 点漏斗_面板出现且为_fixed_带遮罩()
        {
            StubTriggerRect();
            var cut = RenderTable();

            await TriggerOf(cut, "类别").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            var panel = cut.WaitForElement(".el-table-filter", timeout: TestWait.Timeout);
            var style = panel.GetAttribute("style") ?? string.Empty;
            // 定标证据：坐标来自打桩的矩形（left 40），翻面后锚 bottom（视口 200 - 顶部 120 + 间距 4）
            Assert.Contains("position:fixed", style);
            Assert.Contains("left:40px", style);
            Assert.Contains("bottom:84px", style);
            Assert.Contains("top:auto", style);
            Assert.Single(cut.FindAll(".el-table-filter__backdrop"));
            // 面板不再嵌在触发器里：嵌回去就会被 .cell 的 overflow:hidden 裁掉，正是「点不开」的旧结构
            Assert.Null(panel.Closest(".el-table__column-filter-trigger"));
        }

        [Fact]
        public async Task 再点同一漏斗_面板收起()
        {
            StubTriggerRect();
            var cut = RenderTable();

            await TriggerOf(cut, "类别").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            cut.WaitForElement(".el-table-filter", timeout: TestWait.Timeout);

            await TriggerOf(cut, "类别").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            Assert.Empty(cut.FindAll(".el-table-filter"));
            Assert.Empty(cut.FindAll(".el-table-filter__backdrop"));
        }

        [Fact]
        public async Task 点遮罩_面板收起()
        {
            StubTriggerRect();
            var cut = RenderTable();

            await TriggerOf(cut, "类别").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            await cut.Find(".el-table-filter__backdrop").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            Assert.Empty(cut.FindAll(".el-table-filter"));
        }

        [Fact]
        public async Task 勾选组_未点筛选不改行_点筛选才落值并收起()
        {
            StubTriggerRect();
            (string Prop, object[] Values)? payload = null;
            var cut = RenderTable(filterChange: EventCallback.Factory.Create<(string, object[])>(this, v => payload = v));

            await TriggerOf(cut, "类别").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            Assert.Equal(new[] { "西药", "保健品" },
                cut.FindAll(".el-table-filter__checkbox-group .el-checkbox__label").Select(x => x.TextContent.Trim()));
            Assert.Equal(new[] { "阿莫西林", "板蓝根", "维生素C" }, BodyNames(cut));

            CheckOption(cut);
            // EP：勾选项只写进 filteredValue，行集要等 confirmFilter → store.commit('filterChange')
            Assert.Equal(3, BodyNames(cut).Count);

            await cut.Find(".el-table-filter__bottom button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            Assert.Equal(new[] { "阿莫西林" }, BodyNames(cut));
            Assert.Empty(cut.FindAll(".el-table-filter"));
            Assert.Equal("Category", payload?.Prop);
            Assert.Equal(new[] { "西药" }, payload?.Values);
            // 生效中的列要给表头筛选态：漏斗高亮与 th.filtered 都靠它
            Assert.Contains("filtered", (TriggerOf(cut, "类别").Closest("th").GetAttribute("class") ?? ""));
        }

        [Fact]
        public async Task 重置_清空草稿与已生效的筛选()
        {
            StubTriggerRect();
            var cut = RenderTable();
            await TriggerOf(cut, "类别").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            CheckOption(cut);
            await cut.Find(".el-table-filter__bottom button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            Assert.Single(BodyNames(cut));

            await TriggerOf(cut, "类别").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            await cut.FindAll(".el-table-filter__bottom button")[1].ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            Assert.Equal(3, BodyNames(cut).Count);
            Assert.DoesNotContain("filtered", (TriggerOf(cut, "类别").Closest("th").GetAttribute("class") ?? ""));
        }

        [Fact]
        public async Task 单选列表_点一项即生效并收起()
        {
            StubTriggerRect();
            var cut = RenderTable(filterMultiple: false);

            await TriggerOf(cut, "类别").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            // EP 首项固定是 clearFilter（中文「全部」/英文 All），其余按 filters 顺序
            var items = cut.FindAll(".el-table-filter__list-item");
            Assert.Equal(3, items.Count);
            Assert.Equal("全部", items[0].TextContent.Trim());

            await items[2].ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            Assert.Equal(new[] { "维生素C" }, BodyNames(cut));
            Assert.Empty(cut.FindAll(".el-table-filter"));
        }

        [Fact]
        public async Task 单选列表_点全部_恢复全部()
        {
            StubTriggerRect();
            var cut = RenderTable(filterMultiple: false);
            await TriggerOf(cut, "类别").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            await cut.FindAll(".el-table-filter__list-item")[2].ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            Assert.Single(BodyNames(cut));

            await TriggerOf(cut, "类别").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            await cut.Find(".el-table-filter__list-item").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            Assert.Equal(3, BodyNames(cut).Count);
        }

        [Fact]
        public async Task 条件式面板_渲染两条规则_应用才生效()
        {
            StubTriggerRect();
            var cut = RenderTable(advancedNameColumn: true);
            var nameHeader = cut.Instance.Columns.First(x => x.Text == "名称");
            nameHeader.AdvancedConditions = new List<TableFilterCondition>
            {
                new TableFilterCondition { Operator = TableFilterOperator.Contains, Value = "板蓝" }
            };

            await TriggerOf(cut, "名称").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            Assert.Single(cut.FindAll(".el-table-filter--advanced"));
            Assert.Equal(2, cut.FindAll(".el-table-filter__rule").Count);
            Assert.Single(cut.FindAll(".el-table-filter__relation"));
            // 打开面板本身不改行集
            Assert.Equal(3, BodyNames(cut).Count);

            await cut.Find(".el-table-filter__bottom button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            Assert.Equal(new[] { "板蓝根" }, BodyNames(cut));
            Assert.Empty(cut.FindAll(".el-table-filter"));
        }

        [Fact]
        public async Task 条件面板_运算符与关系显示当前值_不把C代码印进DOM()
        {
            StubTriggerRect();
            var cut = RenderTable(advancedNameColumn: true);
            cut.Instance.Columns.First(x => x.Text == "名称").AdvancedConditions = new List<TableFilterCondition>
            {
                new TableFilterCondition { Operator = TableFilterOperator.StartsWith, Value = "维" },
                new TableFilterCondition { Operator = TableFilterOperator.Equal, Value = "C" }
            };

            await TriggerOf(cut, "名称").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            var panel = cut.Find(".el-table-filter--advanced");
            // @for 体里出现过标记之后 Razor 已切到标记上下文，裸 if 会被当正文整句印进 DOM
            // （浏览器实测：右值框被这段匿名文字挤到 11px 宽）。单测与浏览器同一份编译产物，这里拦得住。
            Assert.DoesNotContain("NeedsValue", panel.TextContent);
            Assert.DoesNotContain("index > 0", panel.TextContent);
            // ElOption 只在下拉首次展开时才挂载注册：不显式给 Label，输入框在点开之前一直是空白
            Assert.Equal(new[] { "开头是", "等于" },
                cut.FindAll(".el-table-filter__rule-op input").Select(x => x.GetAttribute("value")).ToArray());
            Assert.Equal("且", cut.Find(".el-table-filter__relation input").GetAttribute("value"));
        }

        [Fact]
        public async Task 条件式面板_清除_立刻放行全部并保持打开()
        {
            StubTriggerRect();
            var cut = RenderTable(advancedNameColumn: true);
            var nameHeader = cut.Instance.Columns.First(x => x.Text == "名称");
            nameHeader.AdvancedConditions = new List<TableFilterCondition>
            {
                new TableFilterCondition { Operator = TableFilterOperator.StartsWith, Value = "维" }
            };
            await TriggerOf(cut, "名称").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            await cut.Find(".el-table-filter__bottom button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            Assert.Single(BodyNames(cut));

            await TriggerOf(cut, "名称").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            await cut.FindAll(".el-table-filter__bottom button")[1].ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            Assert.Equal(3, BodyNames(cut).Count);
            Assert.Empty(cut.Instance.Columns.First(x => x.Text == "名称").AdvancedConditions);
            // 「清除」是重新开始，不是关掉面板（对齐截图里 Clear 之后仍可继续填）
            Assert.Single(cut.FindAll(".el-table-filter--advanced"));
        }

        [Fact]
        public async Task 条件链_或_与_且_求值不同()
        {
            StubTriggerRect();
            var cut = RenderTable(advancedNameColumn: true);
            void Apply(TableFilterRelation relation)
            {
                var header = cut.Instance.Columns.First(x => x.Text == "名称");
                header.AdvancedConditions = new List<TableFilterCondition>
                {
                    new TableFilterCondition { Operator = TableFilterOperator.Contains, Value = "阿莫" },
                    new TableFilterCondition { Operator = TableFilterOperator.Contains, Value = "维生素", Relation = relation }
                };
            }

            Apply(TableFilterRelation.Or);
            await TriggerOf(cut, "名称").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            await cut.Find(".el-table-filter__bottom button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            Assert.Equal(new[] { "阿莫西林", "维生素C" }, BodyNames(cut));

            Apply(TableFilterRelation.And);
            await TriggerOf(cut, "名称").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            await cut.Find(".el-table-filter__bottom button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            // 「或」两条命中不同行 → 2 行；「且」要求同一行同时命中 → 0 行
            Assert.Empty(BodyNames(cut));
            Assert.Single(cut.FindAll(".el-table__empty-block"));
        }

        [Fact]
        public async Task ClearFilterAsync_同时清空选项与条件()
        {
            StubTriggerRect();
            var cut = RenderTable(advancedNameColumn: true);
            await TriggerOf(cut, "类别").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            CheckOption(cut);
            await cut.Find(".el-table-filter__bottom button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            var nameHeader = cut.Instance.Columns.First(x => x.Text == "名称");
            nameHeader.AdvancedConditions = new List<TableFilterCondition>
            {
                new TableFilterCondition { Operator = TableFilterOperator.NotEmpty }
            };
            Assert.Single(BodyNames(cut));

            await cut.InvokeAsync(() => cut.Instance.ClearFilterAsync());

            Assert.Equal(3, BodyNames(cut).Count);
            Assert.Empty(nameHeader.AdvancedConditions);
            Assert.All(cut.Instance.Columns, header => Assert.False(header.HasActiveFilter));
        }

        [Fact]
        public void 面板定位_右侧放不下时左钳_下方够宽不翻面_条件面板给定宽()
        {
            var roomBelow = TableFilterPopper.BuildPanelStyle(
                new ElementViewportRect { Left = 40, Top = 100, Bottom = 124, Vw = 1200, Vh = 800 }, advanced: false);
            Assert.Contains("top:128px", roomBelow);
            Assert.Contains("bottom:auto", roomBelow);
            Assert.DoesNotContain("width:", roomBelow);

            var nearRight = TableFilterPopper.BuildPanelStyle(
                new ElementViewportRect { Left = 1150, Top = 100, Bottom = 124, Vw = 1200, Vh = 800 }, advanced: false);
            Assert.Contains($"left:{1200 - TableFilterPopper.EdgeMargin - TableFilterPopper.OptionPanelWidth:0.##}px", nearRight);

            var advanced = TableFilterPopper.BuildPanelStyle(
                new ElementViewportRect { Left = 20, Top = 100, Bottom = 124, Vw = 1200, Vh = 800 }, advanced: true);
            Assert.Contains($"width:{TableFilterPopper.AdvancedPanelWidth:0.##}px", advanced);
            // 条件面板不设 max-height，否则运算符下拉会被面板自己裁掉
            Assert.DoesNotContain("max-height", advanced);
        }

        [Fact]
        public void 面板定位_量不到矩形时返回_null_由_CSS_兜底()
        {
            Assert.Null(TableFilterPopper.BuildPanelStyle(null, advanced: false));
            Assert.Null(TableFilterPopper.BuildPanelStyle(new ElementViewportRect { Left = 10, Bottom = 30 }, advanced: true));
        }
    }
}
