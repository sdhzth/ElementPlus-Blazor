using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 没有 <c>Property</c> 的模板列必须写成 <see cref="ElTableTemplateColumn"/>，
    /// 写成 <see cref="ElTableColumn"/> 会在运行期抛 <see cref="ElementException"/>。
    ///
    /// 为什么值得单独钉一条：这条崩溃<b>编译器完全看不见</b>（RZ/CS 全 0），页面照常编译通过，
    /// 一打开就整页 500。本项目实测（Blazor Server 静态预渲染，证据
    /// E:\电子档案\_probe_formitem\probe_result_0103.log、probe_server_0103.log 里的
    /// <c>at Element.ElTable.AddColumnInternal</c>）：
    /// <code>
    /// /probe/t/bad  （&lt;ElTableColumn Text="操作"&gt;&lt;ChildContent&gt;…）  → HTTP 500
    ///                Element.ElementException: 列 操作 没有设置 Property 属性
    /// /probe/t/good （同一段内容，只把标签换成 &lt;ElTableTemplateColumn&gt;） → HTTP 200，模板照常出单元格
    /// </code>
    /// 两档字节数在 0.10.3 / 0.10.4 之间逐路由一致，说明这条判定与版本无关，是列类型的事。
    /// 守卫在 ElTable.razor.cs:185-190：<c>Property == null</c> 且不是 <c>ElTableTemplateColumn</c>
    /// 且没有 <c>ChildContent</c>（批次91-95 按 EP 口径放宽：官方 <c>el-table-column</c> 允许只写插槽不写 prop）
    /// 且 <c>Type</c> 不是 index/selection 就抛。<c>ElTableTemplateColumn</c> 只是把 <c>ChildContent</c>
    /// 重声明一遍（ElTableTemplateColumn.razor.cs:9-12），渲染路径与 <c>ElTableColumn</c> 完全相同，
    /// 所以改名是零风险的修法，也是本项目统一的写法（见 Pages/BaseInfo/DeptInfo.razor、StoreHouse.razor）。
    /// </summary>
    public class ElTablePropertylessColumnTests : BunitContext
    {
        public ElTablePropertylessColumnTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private class Item
        {
            public string Name { get; set; }
        }

        private static List<Item> Rows() => new List<Item>
        {
            new Item { Name = "A" },
            new Item { Name = "B" },
        };

        private static RenderFragment<object> TemplateCell => _ => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddContent(1, "op-cell");
            builder.CloseElement();
        };

        /// <summary>一列「操作」模板列（列类型由参数决定）+ 一列带 Property 的普通列。</summary>
        private static RenderFragment OpPlusPropertyColumn(Type opColumnType)
        {
            return builder =>
            {
                builder.OpenComponent(0, opColumnType);
                builder.AddAttribute(1, "Text", "操作");
                builder.AddAttribute(2, "Width", "120");
                builder.AddAttribute(3, "ChildContent", TemplateCell);
                builder.CloseComponent();

                builder.OpenComponent<ElTableColumn>(4);
                builder.AddAttribute(5, "Property", "Name");
                builder.AddAttribute(6, "Text", "名称");
                builder.CloseComponent();
            };
        }

        /// <summary>
        /// 真正会踩守卫的坏档：既没有 <c>Property</c>、也没有 <c>ChildContent</c>、
        /// <c>Type</c> 又不是 index/selection 的普通列。
        /// <para>
        /// 为什么不能沿用上面那个带模板的坏档：守卫在批次91-95（提交 61a990d）按 EP 口径放宽成
        /// 「<c>ElTableTemplateColumn</c> <b>或</b>带 <c>ChildContent</c> 的列」都不强制 <c>Property</c>
        /// （官方 <c>el-table-column</c> 允许只写插槽不写 prop）。带模板的列从此不抛，
        /// 用它当坏档只会让 <c>Record.Exception</c> 拿到 null、这条用例长期假红。
        /// </para>
        /// </summary>
        private static RenderFragment PropertylessPlainColumn()
        {
            return builder =>
            {
                builder.OpenComponent<ElTableColumn>(0);
                builder.AddAttribute(1, "Text", "操作");
                builder.AddAttribute(2, "Width", "120");
                builder.CloseComponent();

                builder.OpenComponent<ElTableColumn>(3);
                builder.AddAttribute(4, "Property", "Name");
                builder.AddAttribute(5, "Text", "名称");
                builder.CloseComponent();
            };
        }

        /// <summary>两列都带 Property 的普通表（正向对照用）。</summary>
        private static RenderFragment TwoPropertyColumns()
        {
            return builder =>
            {
                builder.OpenComponent<ElTableColumn>(0);
                builder.AddAttribute(1, "Property", "Name");
                builder.AddAttribute(2, "Text", "名称");
                builder.CloseComponent();
            };
        }

        private IRenderedComponent<ElTable> RenderTable(RenderFragment columns)
        {
            return Render<ElTable>(p => p
                .Add(x => x.DataSource, Rows())
                .Add(x => x.DataType, typeof(Item))
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.EnablePagination, false)
                .AddChildContent(columns));
        }

        private static Exception Unwrap(Exception ex)
        {
            while (ex?.InnerException != null) ex = ex.InnerException;
            return ex;
        }

        [Fact]
        public void PlainColumnWithoutPropertyThrows()
        {
            var ex = Record.Exception(() => RenderTable(PropertylessPlainColumn()));
            var root = Unwrap(ex);

            Assert.IsType<ElementException>(root);
            Assert.Contains("没有设置", root.Message);
            Assert.Contains("Property", root.Message);
        }

        /// <summary>
        /// EP 口径的豁免档：<c>ElTableColumn</c> 只写插槽不写 <c>prop</c> 是合法的（操作列 / 纯自定义渲染列），
        /// 守卫不能误伤。这一支正是把上面那条坏档改窄的原因，单独钉住免得以后又被放宽回去。
        /// </summary>
        [Fact]
        public void PlainColumnWithChildContentDoesNotThrow()
        {
            var cut = RenderTable(OpPlusPropertyColumn(typeof(ElTableColumn)));
            cut.Render();

            Assert.Equal(2, cut.FindAll("tbody tr").Count);
            Assert.Equal(2, cut.FindAll("tbody td .cell").Count(t => t.TextContent.Trim() == "op-cell"));
        }

        [Fact]
        public void TemplateColumnWithoutPropertyRendersTemplate()
        {
            var cut = RenderTable(OpPlusPropertyColumn(typeof(ElTableTemplateColumn)));
            cut.Render();

            Assert.Equal(2, cut.FindAll("tbody tr").Count);
            Assert.Equal(2, cut.FindAll("tbody td .cell").Count(t => t.TextContent.Trim() == "op-cell"));
            Assert.Contains("名称", cut.Find("thead").TextContent);
        }

        /// <summary>正向对照：同一条 Record.Exception 口径下，带 Property 的普通列不抛。</summary>
        [Fact]
        public void ColumnsWithPropertyDoNotThrow()
        {
            var ex = Record.Exception(() =>
            {
                var cut = RenderTable(TwoPropertyColumns());
                cut.Render();
            });
            Assert.Null(ex);
        }

        /// <summary>内置类型列（index/selection）本来就允许没有 Property，守卫不能误伤。</summary>
        [Fact]
        public void IndexColumnWithoutPropertyDoesNotThrow()
        {
            var columns = (RenderFragment)(builder =>
            {
                builder.OpenComponent<ElTableColumn>(0);
                builder.AddAttribute(1, "Type", "index");
                builder.AddAttribute(2, "Text", "#");
                builder.CloseComponent();
            });

            var ex = Record.Exception(() => RenderTable(columns).Render());
            Assert.Null(ex);
        }
    }
}
