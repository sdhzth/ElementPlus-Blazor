using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElTable 编辑状态下按回车进入下一个可编辑单元格（并自动聚焦）的测试。
    /// </summary>
    public class ElTableEnterCellNavigationTests : BunitContext
    {
        public ElTableEnterCellNavigationTests()
        {
            Services.AddElementServices();
            Services.AddSingleton(new HttpClient());
            // 双击定位起始单元格、以及渲染结束后聚焦单元格都会调用 JS
            JSInterop.SetupVoid("elementTableFocusCell").SetVoidResult();
            JSInterop.Setup<int>("elementTableColumnIndexAt");
            JSInterop.Setup<bool>("elementTableEnterNextEditor").SetResult(false);
        }

        public class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string Dept { get; set; }
        }

        private static List<Person> People()
        {
            return new List<Person>
            {
                new Person { Name = "张三", Age = 30, Dept = "内科" },
                new Person { Name = "李四", Age = 41, Dept = "外科" }
            };
        }

        private static RenderFragment DeclareColumns(IEnumerable<string> properties)
        {
            return builder =>
            {
                var seq = 0;
                foreach (var property in properties)
                {
                    builder.OpenComponent<ElTableColumn>(seq++);
                    builder.AddAttribute(seq++, "Property", property);
                    builder.AddAttribute(seq++, "Text", property);
                    builder.CloseComponent();
                }
            };
        }

        private IRenderedComponent<ElTable> RenderTable(bool editable = true,
            bool enterMovesToNextCell = true,
            bool enterWrapsToNextRow = false,
            TableEnterNavigationMode enterNavigationMode = TableEnterNavigationMode.NextCell,
            bool enterNavigatesEditors = false,
            bool enterCarriesValue = false,
            int rowCount = 2,
            EventCallback<TableSaveEventArgs> onSave = default)
        {
            return Render<ElTable>(parameters => parameters
                .Add(x => x.DataSource, People().Take(rowCount).ToList())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.IsEditable, editable)
                .Add(x => x.EnterMovesToNextCell, enterMovesToNextCell)
                .Add(x => x.EnterWrapsToNextRow, enterWrapsToNextRow)
                .Add(x => x.EnterNavigationMode, enterNavigationMode)
                .Add(x => x.EnterNavigatesEditors, enterNavigatesEditors)
                .Add(x => x.EnterCarriesValue, enterCarriesValue)
                .Add(x => x.OnSave, onSave)
                .AddCascadingValue("Page", (ElementComponentBase)new ElementDialogBase())
                .AddChildContent(DeclareColumns(new[] { "Name", "Age", "Dept" })));
        }

        private static IReadOnlyList<AngleSharp.Dom.IElement> Rows(IRenderedComponent<ElTable> cut)
        {
            return cut.FindAll("tbody tr");
        }

        private static AngleSharp.Dom.IElement ActiveCell(IRenderedComponent<ElTable> cut)
        {
            return cut.Find("td.el-table__cell--editing");
        }

        /// <summary>
        /// 模拟在单元格内按回车：浏览器会先在输入框上触发 keydown，再冒泡到行上触发 keyup。
        /// keydown 会触发重新渲染（事件处理 id 变化），所以 keyup 前要重新查询行元素。
        /// </summary>
        private static void PressEnter(IRenderedComponent<ElTable> cut, int rowIndex = 0, bool shift = false)
        {
            var args = new KeyboardEventArgs { Key = "Enter", Code = "Enter", ShiftKey = shift };
            Rows(cut)[rowIndex].KeyDown(args);
            Rows(cut)[rowIndex].KeyUp(args);
        }

        private List<JSRuntimeInvocation> FocusInvocations()
        {
            return JSInterop.Invocations
                .Where(x => x.Identifier == "elementTableFocusCell")
                .ToList();
        }

        /// <summary>
        /// 取那一次 DOM 跳格调用的参数：[0] = shift，[1] = 是否把来源格内容带进目标格（批次 80）。
        /// </summary>
        private IReadOnlyList<object> EnterEditorArgs()
        {
            var invocations = JSInterop.Invocations
                .Where(x => x.Identifier == "elementTableEnterNextEditor")
                .ToList();
            var invocation = Assert.Single(invocations);
            Assert.Equal(2, invocation.Arguments.Count);
            return invocation.Arguments;
        }

        [Fact]
        public void DoubleClickRow_StartsEditing_AtFirstEditableCell()
        {
            var cut = RenderTable();

            Rows(cut)[0].DoubleClick();

            Assert.NotNull(cut.Instance.EditingRow);
            Assert.Equal(0, cut.Instance.ActiveEditingCellIndex);
            Assert.Equal("Name", cut.Instance.ActiveEditingColumn.Text);
            Assert.Equal(3, cut.Instance.EditableColumns.Count);

            var activeCell = ActiveCell(cut);
            Assert.Contains("el-table__cell--editing", activeCell.ClassList);
            Assert.False(string.IsNullOrWhiteSpace(activeCell.Id));

            cut.WaitForAssertion(() =>
            {
                var focus = FocusInvocations();
                Assert.NotEmpty(focus);
                Assert.Equal(activeCell.Id, focus.Last().Arguments[0]);
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public void Enter_MovesToNextEditableCell_AndFocusesIt()
        {
            var cut = RenderTable();
            Rows(cut)[0].DoubleClick();
            var firstCellId = ActiveCell(cut).Id;

            PressEnter(cut);

            Assert.Equal(1, cut.Instance.ActiveEditingCellIndex);
            Assert.Equal("Age", cut.Instance.ActiveEditingColumn.Text);

            var activeCell = ActiveCell(cut);
            Assert.NotEqual(firstCellId, activeCell.Id);
            Assert.Single(cut.FindAll("td.el-table__cell--editing"));

            cut.WaitForAssertion(() =>
            {
                var focus = FocusInvocations();
                Assert.Equal(activeCell.Id, focus.Last().Arguments[0]);
                Assert.True((bool)focus.Last().Arguments[1]);
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public void Enter_OnLastCell_StaysWhenWrapDisabled()
        {
            var cut = RenderTable();
            Rows(cut)[0].DoubleClick();
            PressEnter(cut);
            PressEnter(cut);
            Assert.Equal(2, cut.Instance.ActiveEditingCellIndex);

            var editingRow = cut.Instance.EditingRow;
            PressEnter(cut);

            Assert.Equal(2, cut.Instance.ActiveEditingCellIndex);
            Assert.Equal("Dept", cut.Instance.ActiveEditingColumn.Text);
            Assert.Same(editingRow, cut.Instance.EditingRow);
        }

        [Fact]
        public void Enter_OnLastCell_WrapsToNextRow_WhenEnabled()
        {
            var cut = RenderTable(enterWrapsToNextRow: true);
            var people = (List<Person>)cut.Instance.DataSource;
            Rows(cut)[0].DoubleClick();
            PressEnter(cut);
            PressEnter(cut);
            Assert.Equal(2, cut.Instance.ActiveEditingCellIndex);

            PressEnter(cut);

            Assert.Equal(0, cut.Instance.ActiveEditingCellIndex);
            Assert.Equal("Name", cut.Instance.ActiveEditingColumn.Text);
            Assert.Same(people[1], cut.Instance.EditingRow);
        }

        [Fact]
        public void ShiftEnter_MovesToPreviousEditableCell()
        {
            var cut = RenderTable();
            Rows(cut)[0].DoubleClick();
            PressEnter(cut);
            Assert.Equal(1, cut.Instance.ActiveEditingCellIndex);

            PressEnter(cut, shift: true);

            Assert.Equal(0, cut.Instance.ActiveEditingCellIndex);
            Assert.Equal("Name", cut.Instance.ActiveEditingColumn.Text);
        }

        [Fact]
        public void Enter_DoesNotTriggerSave()
        {
            var saveCount = 0;
            var cut = RenderTable(onSave: EventCallback.Factory.Create<TableSaveEventArgs>(this, _ => saveCount++));
            Rows(cut)[0].DoubleClick();

            PressEnter(cut);
            PressEnter(cut);

            Assert.Equal(0, saveCount);
            Assert.NotNull(cut.Instance.EditingRow);
            Assert.Equal(2, cut.Instance.ActiveEditingCellIndex);
        }

        [Fact]
        public void EnterMovesToNextCellDisabled_KeepsCell_AndDoesNotSave()
        {
            var saveCount = 0;
            var cut = RenderTable(enterMovesToNextCell: false,
                onSave: EventCallback.Factory.Create<TableSaveEventArgs>(this, _ => saveCount++));
            Rows(cut)[0].DoubleClick();
            var before = cut.Instance.ActiveEditingCellIndex;

            PressEnter(cut);

            Assert.Equal(before, cut.Instance.ActiveEditingCellIndex);
            Assert.Equal("Name", cut.Instance.ActiveEditingColumn.Text);
            Assert.Equal(0, saveCount);
        }

        [Fact]
        public void NonEditableTable_EnterIsIgnored()
        {
            var cut = RenderTable(editable: false);
            Assert.Null(cut.Instance.EditingRow);

            var exception = Record.Exception(() => PressEnter(cut));

            Assert.Null(exception);
            Assert.Null(cut.Instance.EditingRow);
            Assert.Equal(-1, cut.Instance.ActiveEditingCellIndex);
        }

        /// <summary>
        /// 常驻编辑器表格（IsEditable=false）开了 <c>EnterNavigatesEditors</c> 后，回车要把「找下一格编辑器」
        /// 交给 DOM（elementTableEnterNextEditor），因为这类表格没有编辑行/活动单元格概念，
        /// C# 侧的 MoveEditingCell* 一条都跑不到。
        /// </summary>
        [Fact]
        public void AlwaysOnEditors_Enter_CallsDomNavigation()
        {
            var cut = RenderTable(editable: false, enterNavigatesEditors: true);

            PressEnter(cut);

            var args = EnterEditorArgs();
            Assert.Equal(false, args[0]);
            Assert.Equal(false, args[1]);
        }

        [Fact]
        public void AlwaysOnEditors_ShiftEnter_CallsDomNavigationReversed()
        {
            var cut = RenderTable(editable: false, enterNavigatesEditors: true);

            PressEnter(cut, shift: true);

            var args = EnterEditorArgs();
            Assert.Equal(true, args[0]);
            Assert.Equal(false, args[1]);
        }

        /// <summary>
        /// 批次 80：EnterCarriesValue 只是把「带值」这个开关交给 DOM（值本身在 DOM 里写，
        /// C# 侧不参与，因为这条链路是纯 JS 的行列游走）。
        /// </summary>
        [Fact]
        public void AlwaysOnEditors_EnterCarriesValue_PassesFlagToDom()
        {
            var cut = RenderTable(editable: false, enterNavigatesEditors: true, enterCarriesValue: true);

            PressEnter(cut);

            var args = EnterEditorArgs();
            Assert.Equal(false, args[0]);
            Assert.Equal(true, args[1]);
        }

        /// <summary>
        /// 正向对照：EnterCarriesValue 单独开（没有 EnterNavigatesEditors）一个 JS 调用都不发，
        /// 它只是跳格的一个修饰开关，不能自己把 DOM 导航拉起来。
        /// </summary>
        [Fact]
        public void AlwaysOnEditors_CarryValueWithoutNavigation_DoNotCallDomNavigation()
        {
            var cut = RenderTable(editable: false, enterCarriesValue: true);

            PressEnter(cut);

            Assert.Empty(JSInterop.Invocations.Where(x => x.Identifier == "elementTableEnterNextEditor"));
        }

        /// <summary>
        /// 正向对照：不开属性的非编辑表格一个 JS 调用都不发（默认行为不变）。
        /// </summary>
        [Fact]
        public void AlwaysOnEditors_WithoutFlag_DoNotCallDomNavigation()
        {
            var cut = RenderTable(editable: false);

            PressEnter(cut);

            Assert.Empty(JSInterop.Invocations.Where(x => x.Identifier == "elementTableEnterNextEditor"));
        }

        /// <summary>
        /// 编辑表格走的是编辑单元格那条路，属性开着也不该去问 DOM。
        /// </summary>
        [Fact]
        public void EditableTable_IgnoresAlwaysOnEditorNavigation()
        {
            var cut = RenderTable(enterNavigatesEditors: true);
            Rows(cut)[0].DoubleClick();

            PressEnter(cut);

            Assert.Empty(JSInterop.Invocations.Where(x => x.Identifier == "elementTableEnterNextEditor"));
            Assert.Equal(1, cut.Instance.ActiveEditingCellIndex);
        }

        [Fact]
        public void ClickAnotherCell_MakesItActive()
        {
            var cut = RenderTable();
            Rows(cut)[0].DoubleClick();
            Assert.Equal(0, cut.Instance.ActiveEditingCellIndex);

            var cells = Rows(cut)[0].QuerySelectorAll("td");
            cells[2].Click();

            Assert.Equal(2, cut.Instance.ActiveEditingCellIndex);
            Assert.Equal("Dept", cut.Instance.ActiveEditingColumn.Text);
        }

        [Fact]
        public async Task MoveEditingCellAsync_NavigatesProgrammatically()
        {
            var cut = RenderTable();
            Rows(cut)[0].DoubleClick();

            var moved = await cut.InvokeAsync(() => cut.Instance.MoveEditingCellAsync(1));

            Assert.True(moved);
            Assert.Equal(1, cut.Instance.ActiveEditingCellIndex);

            var notMoved = await cut.InvokeAsync(() => cut.Instance.MoveEditingCellAsync(5));

            Assert.False(notMoved);
            Assert.Equal(1, cut.Instance.ActiveEditingCellIndex);
        }

        // ---------------- 垂直模式：回车进入下一行的同一列 ----------------

        [Fact]
        public void DefaultMode_IsNextCell()
        {
            var cut = RenderTable();

            Assert.Equal(TableEnterNavigationMode.NextCell, cut.Instance.EnterNavigationMode);

            var people = (List<Person>)cut.Instance.DataSource;
            Rows(cut)[0].DoubleClick();
            PressEnter(cut);

            Assert.Equal(1, cut.Instance.ActiveEditingCellIndex);
            Assert.Same(people[0], cut.Instance.EditingRow);
        }

        [Fact]
        public void SameColumnMode_Enter_MovesToNextRowSameColumn()
        {
            var cut = RenderTable(enterNavigationMode: TableEnterNavigationMode.SameColumnNextRow);
            var people = (List<Person>)cut.Instance.DataSource;
            Rows(cut)[0].DoubleClick();
            Assert.Equal(0, cut.Instance.ActiveEditingCellIndex);

            PressEnter(cut);

            Assert.Same(people[1], cut.Instance.EditingRow);
            Assert.Equal(0, cut.Instance.ActiveEditingCellIndex);
            Assert.Equal("Name", cut.Instance.ActiveEditingColumn.Text);
            Assert.Contains("_r1_c0", ActiveCell(cut).Id);
        }

        [Fact]
        public void SameColumnMode_Enter_KeepsActiveColumn()
        {
            var cut = RenderTable(enterNavigationMode: TableEnterNavigationMode.SameColumnNextRow);
            Rows(cut)[0].DoubleClick();
            Rows(cut)[0].QuerySelectorAll("td")[2].Click();
            Assert.Equal(2, cut.Instance.ActiveEditingCellIndex);

            PressEnter(cut);

            Assert.Equal(2, cut.Instance.ActiveEditingCellIndex);
            Assert.Equal("Dept", cut.Instance.ActiveEditingColumn.Text);
            Assert.Contains("_r1_c2", ActiveCell(cut).Id);
        }

        [Fact]
        public void SameColumnMode_ShiftEnter_MovesToPreviousRowSameColumn()
        {
            var cut = RenderTable(enterNavigationMode: TableEnterNavigationMode.SameColumnNextRow);
            var people = (List<Person>)cut.Instance.DataSource;
            Rows(cut)[1].DoubleClick();
            Assert.Same(people[1], cut.Instance.EditingRow);

            PressEnter(cut, rowIndex: 1, shift: true);

            Assert.Same(people[0], cut.Instance.EditingRow);
            Assert.Equal(0, cut.Instance.ActiveEditingCellIndex);
        }

        [Fact]
        public void SameColumnMode_Enter_OnLastRow_MovesToNextColumnFirstRow()
        {
            var cut = RenderTable(enterNavigationMode: TableEnterNavigationMode.SameColumnNextRow,
                enterWrapsToNextRow: true);
            // tbody 末尾还有一行“新增行”（只挂了 onkeyup），所以最后一个 tr 不是数据行
            var people = (List<Person>)cut.Instance.DataSource;
            var lastRow = people.Count - 1;
            Rows(cut)[lastRow].DoubleClick();
            Assert.Same(people[lastRow], cut.Instance.EditingRow);
            Assert.Equal(0, cut.Instance.ActiveEditingCellIndex);

            PressEnter(cut, rowIndex: lastRow);

            // 没有下一行了：跳到下一列，并且落在该列的第一行（批次㊾；之前是留在当前行），仍是编辑态
            Assert.Same(people[0], cut.Instance.EditingRow);
            Assert.Equal(1, cut.Instance.ActiveEditingCellIndex);
            Assert.Equal("Age", cut.Instance.ActiveEditingColumn.Text);
            Assert.Contains("_r0_c1", ActiveCell(cut).Id);
        }

        /// <summary>
        /// 正向对照：只有一行数据时，末行就是首行，换列后编辑行不变（不会因为批次㊾ 的“回到第一行”而跳走）。
        /// </summary>
        [Fact]
        public void SameColumnMode_Enter_OnSingleRow_MovesToNextColumnSameRow()
        {
            var cut = RenderTable(enterNavigationMode: TableEnterNavigationMode.SameColumnNextRow, rowCount: 1);
            var people = (List<Person>)cut.Instance.DataSource;
            Rows(cut)[0].DoubleClick();
            var editingRow = cut.Instance.EditingRow;
            Assert.Same(people[0], editingRow);

            PressEnter(cut, rowIndex: 0);

            Assert.Same(editingRow, cut.Instance.EditingRow);
            Assert.Equal(1, cut.Instance.ActiveEditingCellIndex);
            Assert.Contains("_r0_c1", ActiveCell(cut).Id);
        }

        [Fact]
        public void SameColumnMode_Enter_OnLastRowLastColumn_Stays()
        {
            var cut = RenderTable(enterNavigationMode: TableEnterNavigationMode.SameColumnNextRow);
            var people = (List<Person>)cut.Instance.DataSource;
            var lastRow = people.Count - 1;
            Rows(cut)[lastRow].DoubleClick();
            Rows(cut)[lastRow].QuerySelectorAll("td")[2].Click();
            var editingRow = cut.Instance.EditingRow;
            Assert.Equal(2, cut.Instance.ActiveEditingCellIndex);

            PressEnter(cut, rowIndex: lastRow);

            // 既是最后一行也是最后一列：不动
            Assert.Same(editingRow, cut.Instance.EditingRow);
            Assert.Equal(2, cut.Instance.ActiveEditingCellIndex);
        }

        [Fact]
        public void SameColumnMode_ShiftEnter_OnFirstRow_Stays()
        {
            var cut = RenderTable(enterNavigationMode: TableEnterNavigationMode.SameColumnNextRow);
            Rows(cut)[0].DoubleClick();
            var editingRow = cut.Instance.EditingRow;

            PressEnter(cut, shift: true);

            Assert.Same(editingRow, cut.Instance.EditingRow);
            Assert.Equal(0, cut.Instance.ActiveEditingCellIndex);
        }

        [Fact]
        public void SameColumnMode_EnterMovesToNextCellDisabled_DoesNothing()
        {
            var saveCount = 0;
            var cut = RenderTable(enterMovesToNextCell: false,
                enterNavigationMode: TableEnterNavigationMode.SameColumnNextRow,
                onSave: EventCallback.Factory.Create<TableSaveEventArgs>(this, _ => saveCount++));
            Rows(cut)[0].DoubleClick();
            var editingRow = cut.Instance.EditingRow;

            PressEnter(cut);

            Assert.Same(editingRow, cut.Instance.EditingRow);
            Assert.Equal(0, saveCount);
        }

        [Fact]
        public async Task SameColumnMode_MoveEditingCellAsync_MovesRows()
        {
            var cut = RenderTable(enterNavigationMode: TableEnterNavigationMode.SameColumnNextRow);
            var people = (List<Person>)cut.Instance.DataSource;
            Rows(cut)[0].DoubleClick();

            var moved = await cut.InvokeAsync(() => cut.Instance.MoveEditingCellAsync(1));

            Assert.True(moved);
            Assert.Same(people[1], cut.Instance.EditingRow);
            Assert.Equal(0, cut.Instance.ActiveEditingCellIndex);

            var back = await cut.InvokeAsync(() => cut.Instance.MoveEditingCellAsync(-1));

            Assert.True(back);
            Assert.Same(people[0], cut.Instance.EditingRow);
            Assert.Equal(0, cut.Instance.ActiveEditingCellIndex);
        }
    }
}
