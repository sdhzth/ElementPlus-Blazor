using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElTable 列宽拖拽（本库扩展，消费方批次110uu）：手柄渲染面（表级 / 列级归并、选择列与分组头排除）
    /// 与落定面（JsTableColumnResizeEnd 记覆盖宽 → 重渲染不打回去 → 事件参数）。
    /// 判据一律走 DOM（AngleSharp 查询），不比对 Markup 子串（Blazor 调试帧的 &lt;!--!--&gt; 会假红）。
    /// </summary>
    public class ElTableResizableColumnTests : BunitContext
    {
        public ElTableResizableColumnTests()
        {
            Services.AddElementServices();
            Services.AddSingleton(new HttpClient());
        }

        public class Row
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        private static List<Row> Rows() => new List<Row>
        {
            new Row { Name = "Alpha", Age = 21 },
            new Row { Name = "Bravo", Age = 22 },
        };

        private static Action<RenderTreeBuilder> PlainColumn(string property, string text, int width, bool? resizable = null)
        {
            return child =>
            {
                var seq = 0;
                child.OpenComponent<ElTableColumn>(seq++);
                child.AddAttribute(seq++, "Property", property);
                child.AddAttribute(seq++, "Text", text);
                child.AddAttribute(seq++, "Width", width.ToString());
                if (resizable != null)
                {
                    child.AddAttribute(seq++, "Resizable", resizable.Value);
                }
                child.CloseComponent();
            };
        }

        private IRenderedComponent<ElTable> RenderTable(
            IEnumerable<Action<RenderTreeBuilder>> columns,
            bool resizable = false,
            bool hasSelection = false)
        {
            var fragment = new RenderFragment(child =>
            {
                foreach (var column in columns)
                {
                    column(child);
                }
            });

            return Render<ElTable>(parameters => parameters
                .Add(x => x.DataSource, Rows())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, hasSelection)
                .Add(x => x.Resizable, resizable)
                .AddChildContent(fragment));
        }

        [Fact]
        public void Default_HasNoHandle_AndNoClass()
        {
            var cut = RenderTable(new[] { PlainColumn("Name", "姓名", 120), PlainColumn("Age", "年龄", 80) });

            Assert.DoesNotContain("is-resizable", cut.Find("div.el-table").ClassName);
            Assert.Empty(cut.FindAll(".el-table__column-resize-handle"));
        }

        [Fact]
        public void TableLevel_RendersHandleOnEveryLeafColumn()
        {
            var cut = RenderTable(new[] { PlainColumn("Name", "姓名", 120), PlainColumn("Age", "年龄", 80) }, resizable: true);

            Assert.Contains("is-resizable", cut.Find("div.el-table").ClassName);
            // 两根叶子列各一颗手柄（bUnit 里 ElTable 渲染两拍，FindAll 是当前帧的 DOM）
            Assert.Equal(2, cut.FindAll(".el-table__column-resize-handle").Count);
            Assert.Equal(2, cut.FindAll("th .el-table__column-resize-handle").Count);
        }

        [Fact]
        public void SelectionColumn_NeverGetsHandle()
        {
            var cut = RenderTable(new[] { PlainColumn("Name", "姓名", 120) }, resizable: true, hasSelection: true);

            var handles = cut.FindAll("th .el-table__column-resize-handle");
            Assert.Single(handles);
            var handleTh = handles[0].Closest("th");
            // 手柄只属于数据列；选择列（el-table_1_column_0）一颗都没有
            Assert.DoesNotContain("el-table-column--selection", handleTh.ClassName);
            Assert.Empty(cut.FindAll("th.el-table-column--selection .el-table__column-resize-handle"));
        }

        [Fact]
        public void ColumnOverride_True_LightsClass_AndOnlyThatColumn()
        {
            var cut = RenderTable(new[] { PlainColumn("Name", "姓名", 120, resizable: true), PlainColumn("Age", "年龄", 80) });

            // 列级 true 点亮表根的 is-resizable（否则手柄没有定位锚点）
            Assert.Contains("is-resizable", cut.Find("div.el-table").ClassName);
            var handles = cut.FindAll("th .el-table__column-resize-handle");
            Assert.Single(handles);
            Assert.Contains("el-table_1_column_1", handles[0].Closest("th").ClassName);
        }

        [Fact]
        public void ColumnOverride_False_ExcludesColumn_FromTableLevel()
        {
            var cut = RenderTable(new[] { PlainColumn("Name", "姓名", 120, resizable: false), PlainColumn("Age", "年龄", 80) }, resizable: true);

            var handles = cut.FindAll("th .el-table__column-resize-handle");
            Assert.Single(handles);
            Assert.Contains("el-table_1_column_2", handles[0].Closest("th").ClassName);
        }

        [Fact]
        public void GroupedHeader_HandleOnLeafRowOnly()
        {
            var fragment = new RenderFragment(child =>
            {
                var seq = 0;
                child.OpenComponent<ElTableColumnGroup>(seq++);
                child.AddAttribute(seq++, "Text", "组一");
                child.AddAttribute(seq++, "ChildColumns", new RenderFragment(builder =>
                {
                    var g = 0;
                    builder.OpenComponent<ElTableColumn>(g++);
                    builder.AddAttribute(g++, "Property", "Name");
                    builder.AddAttribute(g++, "Text", "甲");
                    builder.AddAttribute(g++, "Resizable", true);
                    builder.CloseComponent();
                }));
                child.CloseComponent();
            });

            var cut = Render<ElTable>(parameters => parameters
                .Add(x => x.DataSource, Rows())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .AddChildContent(fragment));

            var handles = cut.FindAll("th .el-table__column-resize-handle");
            Assert.Single(handles);
            // 手柄必须挂在叶子行（is-leaf）的 th 上，分组头（is-group）不挂
            Assert.Contains("is-leaf", handles[0].Closest("th").ClassName);
            Assert.DoesNotContain("is-group", handles[0].Closest("th").ClassName);
        }

        [Fact]
        public async System.Threading.Tasks.Task ResizeEnd_StoresOverride_AndRendersColWidth_AndFiresEvent()
        {
            TableColumnWidthChangeEventArgs fired = null;
            var fragment = new RenderFragment(child =>
            {
                var seq = 0;
                child.OpenComponent<ElTableColumn>(seq++);
                child.AddAttribute(seq++, "Property", "Name");
                child.AddAttribute(seq++, "Text", "姓名");
                child.AddAttribute(seq++, "Width", "100");
                child.AddAttribute(seq++, "Resizable", true);
                child.CloseComponent();
            });

            var cut = Render<ElTable>(parameters => parameters
                .Add(x => x.DataSource, Rows())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.OnColumnWidthChange, EventCallback.Factory.Create<TableColumnWidthChangeEventArgs>(this, e => fired = e))
                .AddChildContent(fragment));

            var col = cut.Find("col[name=\"el-table_1_column_1\"]");
            Assert.Equal("100", col.GetAttribute("width"));

            await cut.Instance.JsTableColumnResizeEnd(1, 220, 100);
            cut.Render();

            // 覆盖宽进渲染树：col 的 width 属性从 100 变 220
            Assert.Equal("220", cut.Find("col[name=\"el-table_1_column_1\"]").GetAttribute("width"));

            Assert.NotNull(fired);
            Assert.Equal("姓名", fired.Column.Text);
            Assert.Equal(1, fired.ColIndex);
            Assert.Equal(220, fired.Width);
            Assert.Equal(100, fired.OldWidth);
        }

        [Fact]
        public async System.Threading.Tasks.Task Override_SurvivesReRender()
        {
            var cut = RenderTable(new[] { PlainColumn("Name", "姓名", 100, resizable: true), PlainColumn("Age", "年龄", 80) });

            await cut.Instance.JsTableColumnResizeEnd(1, 220, 100);
            cut.Render();

            // 落定后再渲染一轮（对应排序 / 刷新数据 / 分页的重渲染路径）：
            // colgroup 从 ResolveColumnWidth 重新出宽度，覆盖宽不许被打回去
            cut.Render();

            Assert.Equal("220", cut.Find("col[name=\"el-table_1_column_1\"]").GetAttribute("width"));
        }

        [Fact]
        public async System.Threading.Tasks.Task ResizeEnd_SelectionIndex_IsIgnored()
        {
            TableColumnWidthChangeEventArgs fired = null;
            var cut = RenderTable(new[] { PlainColumn("Name", "姓名", 100) }, hasSelection: true);

            cut.Render();
            var selectionCol = cut.Find("col[name=\"el-table_1_column_0\"]");
            var before = selectionCol.GetAttribute("width");

            await cut.Instance.JsTableColumnResizeEnd(0, 80, 55);
            cut.Render();

            Assert.Equal(before, cut.Find("col[name=\"el-table_1_column_0\"]").GetAttribute("width"));
            Assert.Null(fired);
        }

        [Fact]
        public async System.Threading.Tasks.Task ResizeEnd_SameWidth_DoesNotFireEvent()
        {
            TableColumnWidthChangeEventArgs fired = null;
            var fragment = new RenderFragment(child =>
            {
                var seq = 0;
                child.OpenComponent<ElTableColumn>(seq++);
                child.AddAttribute(seq++, "Property", "Name");
                child.AddAttribute(seq++, "Text", "姓名");
                child.AddAttribute(seq++, "Width", "100");
                child.AddAttribute(seq++, "Resizable", true);
                child.CloseComponent();
            });

            var cut = Render<ElTable>(parameters => parameters
                .Add(x => x.DataSource, Rows())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.OnColumnWidthChange, EventCallback.Factory.Create<TableColumnWidthChangeEventArgs>(this, e => fired = e))
                .AddChildContent(fragment));

            await cut.Instance.JsTableColumnResizeEnd(1, 100, 100);

            Assert.Null(fired);
        }
    }
}
