using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElTable 的 <c>show-overflow-tooltip</c>（EP 契约）。改动前这条属性在本仓是「死属性」：
    /// 参数、<c>TableHeader</c> 字段与 <c>GetShowOverflowTooltip</c> 都在，但没有任何渲染点用它，
    /// <c>ElAccountTableGrow</c> 里 <c>ShowOverflowTooltip="true"</c> 的那一列同样形同装饰。
    /// 这里钉住 EP 2.14.0 的三件事：单元格类名多 <c>el-tooltip</c>（table.scss:197-200 靠它换成不换行）、
    /// 内联宽度 <c>realWidth-1</c>（render-helper.ts:193-199）、悬停实测被裁才浮全文（table-body/events-helper.ts）。
    /// </summary>
    public class ElTableOverflowTooltipTests : BunitContext
    {
        public ElTableOverflowTooltipTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private class Person
        {
            public string Name { get; set; }
            public string City { get; set; }
        }

        private static List<Person> People() => Enumerable.Range(1, 3)
            .Select(i => new Person { Name = "姓名" + i, City = "城市" + i })
            .ToList();

        /// <summary>两列：第一列按需开 show-overflow-tooltip，第二列永远不开（负向断言的对照组）。</summary>
        private static RenderFragment Columns(bool firstFlagged, string firstWidth = "170") => builder =>
        {
            var seq = 0;
            builder.OpenComponent<ElTableColumn>(seq++);
            builder.AddAttribute(seq++, "Property", "Name");
            builder.AddAttribute(seq++, "Text", "姓名");
            builder.AddAttribute(seq++, "Width", firstWidth);
            builder.AddAttribute(seq++, "ShowOverflowTooltip", firstFlagged);
            builder.CloseComponent();

            builder.OpenComponent<ElTableColumn>(seq++);
            builder.AddAttribute(seq++, "Property", "City");
            builder.AddAttribute(seq++, "Text", "城市");
            builder.AddAttribute(seq++, "Width", "120");
            builder.CloseComponent();
        };

        private IRenderedComponent<ElTable> RenderTable(bool tableFlag, bool columnFlag = false,
                                                       string columnWidth = "170")
            => Render<ElTable>(p => p
                .Add(x => x.DataSource, People())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.ShowOverflowTooltip, tableFlag)
                .AddChildContent(Columns(columnFlag, columnWidth)));

        /// <summary>bUnit 没有布局引擎：是否真被裁只能由 JS interop 喂进来。</summary>
        private void StubTextOverflow(bool clipped, string text = "姓名1 的完整内容")
            => JSInterop.Setup<TextClipState>(TextClip.MeasureFunction, _ => true)
                .SetResult(new TextClipState
                {
                    ScrollWidth = clipped ? 640 : 169,
                    ClientWidth = 169,
                    Text = text
                });

        private static IReadOnlyList<AngleSharp.Dom.IElement> BodyCells(IRenderedComponent<ElTable> cut)
            => cut.FindAll("tbody tr.el-table__row td .cell");

        [Fact]
        public void OffByDefaultKeepsPlainCellMarkup()
        {
            var cut = RenderTable(tableFlag: false);

            // 没开＝零 DOM 变化：没有触发 span，类名仍是单纯的 cell（EP 默认也是关的）
            Assert.Empty(cut.FindAll(".el-tooltip__trigger"));
            Assert.All(BodyCells(cut), cell => Assert.Equal("cell", cell.GetAttribute("class")));
        }

        [Fact]
        public void ColumnFlagAddsElTooltipClassInsideBlockTrigger()
        {
            var cut = RenderTable(tableFlag: false, columnFlag: true);

            var flagged = BodyCells(cut).First();
            Assert.Contains("cell", flagged.ClassList);
            Assert.Contains("el-tooltip", flagged.ClassList);

            // 触发 span 必须是块级：inline-block 的收缩宽度会撑破 td，让列宽约束失效
            var trigger = flagged.ParentElement;
            Assert.Contains("el-tooltip__trigger", trigger.ClassList);
            Assert.Contains("display:block", trigger.GetAttribute("style"));

            // 对照：没开的那一列连包装都不加，父节点仍是 td
            var plain = BodyCells(cut).Skip(1).First();
            Assert.Equal("cell", plain.GetAttribute("class"));
            Assert.Equal("td", plain.ParentElement.TagName.ToLowerInvariant());
        }

        [Fact]
        public void ColumnFlagSetsEpCellWidthOnePixelNarrower()
        {
            var cut = RenderTable(tableFlag: false, columnFlag: true, columnWidth: "170");

            // EP render-helper.ts:195：width = (realWidth || Number(width)) - 1
            Assert.Equal("width:169px", BodyCells(cut).First().GetAttribute("style"));
        }

        [Fact]
        public void PercentageColumnSkipsInlineWidth()
        {
            var cut = RenderTable(tableFlag: false, columnFlag: true, columnWidth: "40%");

            // 百分比列折不出像素（本仓没有 EP 的布局引擎），交给块级触发 span 撑满 td
            Assert.Null(BodyCells(cut).First().GetAttribute("style"));
        }

        /// <summary>
        /// 批次㊸ 的 auto 列（<c>Width</c> 为 null → <c>&lt;col&gt;</c> 不写宽度）同理不能钉内联宽度：
        /// 它折出来是兜底的 80，会把 .cell 钉成 79px，文字在摊到 700px 的列里提前打点。
        /// 正向对照是上面那条 <c>ColumnFlagSetsEpCellWidthOnePixelNarrower</c>（声明 170 → 169px）。
        /// </summary>
        [Fact]
        public void AutoColumnSkipsInlineWidth()
        {
            var cut = RenderTable(tableFlag: false, columnFlag: true, columnWidth: null);

            Assert.Null(BodyCells(cut).First().GetAttribute("style"));
        }

        [Fact]
        public void TableFlagAppliesToEveryColumn()
        {
            var cut = RenderTable(tableFlag: true);

            Assert.All(BodyCells(cut), cell => Assert.Contains("el-tooltip", cell.ClassList));
        }

        [Fact]
        public void HoverShowsFullTextWhenCellIsClipped()
        {
            StubTextOverflow(clipped: true, text: "姓名1 的完整内容");
            var cut = RenderTable(tableFlag: false, columnFlag: true);

            BodyCells(cut).First().TriggerEvent("onmouseenter", new MouseEventArgs());

            cut.WaitForAssertion(() =>
            {
                var popper = cut.Find(".el-tooltip__popper");
                Assert.Equal("姓名1 的完整内容", popper.TextContent.Trim());
                Assert.Equal("tooltip", popper.GetAttribute("role"));
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public void HoverKeepsPopperHiddenWhenCellNotClipped()
        {
            StubTextOverflow(clipped: false);
            var cut = RenderTable(tableFlag: false, columnFlag: true);

            BodyCells(cut).First().TriggerEvent("onmouseenter", new MouseEventArgs());

            // 反空转对照：确实去过 JS 侧量，只是结论是「没裁」
            cut.WaitForAssertion(() => Assert.Contains(TextClip.MeasureFunction,
                JSInterop.Invocations.Select(x => x.Identifier)), timeout: TestWait.Timeout);
            Assert.Empty(cut.FindAll(".el-tooltip__popper"));
        }

        [Fact]
        public void HoverHidesPopperOnMouseLeave()
        {
            StubTextOverflow(clipped: true, text: "姓名1 的完整内容");
            var cut = RenderTable(tableFlag: false, columnFlag: true);
            var cell = BodyCells(cut).First();

            cell.TriggerEvent("onmouseenter", new MouseEventArgs());
            cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".el-tooltip__popper")), timeout: TestWait.Timeout);

            BodyCells(cut).First().TriggerEvent("onmouseleave", new MouseEventArgs());

            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".el-tooltip__popper")), timeout: TestWait.Timeout);
        }

        [Fact]
        public void IndexColumnStillCountsPerPageUnderTooltip()
        {
            // 单元格内容改成延迟委托后，@for 的计数器不能再被子片段直接引用，
            // 否则每行的序号都会取到最后一行（这正是 Index 列曾经实测三行全渲染成 "4" 的形状）。
            var cut = Render<ElTable>(p => p
                .Add(x => x.DataSource, People())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.EnablePagination, true)
                .Add(x => x.PageSize, 3)
                .Add(x => x.ShowOverflowTooltip, true)
                .AddChildContent(builder =>
                {
                    var seq = 0;
                    builder.OpenComponent<ElTableColumn>(seq++);
                    builder.AddAttribute(seq++, "Type", "index");
                    builder.AddAttribute(seq++, "Text", "#");
                    builder.CloseComponent();
                }));

            Assert.Equal(new[] { "1", "2", "3" },
                BodyCells(cut).Select(cell => cell.TextContent.Trim()).ToArray());
        }

        [Fact]
        public void GrowPassesColumnFlagThroughToTable()
        {
            // ElAccountTableGrow 的「科目名称」列就是这条链路（ElDropDownTableGrow → ElTable）；
            // 属性此前一路传到 TableHeader 却没人用，这里钉住它真的落到单元格上。
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, People())
                .Add(x => x.TextField, "Name")
                .AddChildContent(Columns(firstFlagged: true, firstWidth: "90")));

            cut.Find(".el-select__wrapper").Click();

            var flagged = cut.FindAll(".el-dropdown-table-grow__panel tbody td .cell.el-tooltip");
            var plain = cut.FindAll(".el-dropdown-table-grow__panel tbody td .cell:not(.el-tooltip)");
            Assert.NotEmpty(flagged);
            Assert.NotEmpty(plain);
            Assert.All(flagged, cell => Assert.Equal("width:89px", cell.GetAttribute("style")));
            Assert.All(plain, cell => Assert.Null(cell.GetAttribute("style")));
        }
    }
}
