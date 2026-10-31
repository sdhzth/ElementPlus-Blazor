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
    /// 同名列回归：一张表里两列标题相同（zyweb 物料条码表 u_barcode / u_color 都写「颜色」）时整表必须照常渲染。
    /// 改动前 <c>ElTable.razor</c> 的 col/th/td 用 <c>@key="header.Text"</c> 记账，浏览器里点进「物料条码」页签
    /// 触发一次重渲染，Blazor 就抛
    /// <c>InvalidOperationException: More than one sibling of element 'col' has the same key value, '颜色'</c>，
    /// 用户侧表现为「未处理错误黄条 + 表格不再更新」。
    ///
    /// 关键一点（否则这条测试没有牙）：Blazor 的重复键检查只在<strong>有旧帧可比的更新</strong>里做，
    /// 首帧渲染不查。实测只 Render() 一次时 5 条断言全绿（假绿），所以这里的判据统一走
    /// <see cref="RerenderDup"/> —— 先首帧、再换一份 DataSource 重渲染，异常从这一帧才浮出来。
    /// 键的唯一性判据本来就该由列序号给：标题是给人看的文本，不是身份。
    /// </summary>
    public class ElTableDuplicateColumnTitleTests : BunitContext
    {
        public ElTableDuplicateColumnTitleTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private class Barcode
        {
            public string u_barcode { get; set; }
            public string u_color { get; set; }
            public string u_spec { get; set; }
        }

        private static List<Barcode> Rows() => new List<Barcode>
        {
            new Barcode { u_barcode = "BC-1", u_color = "红色", u_spec = "M" },
            new Barcode { u_barcode = "BC-2", u_color = "蓝色", u_spec = "L" },
        };

        private static RenderFragment Cols(params string[] propsAndTitles)
        {
            // 成对传入：属性名, 标题, 属性名, 标题...
            return builder =>
            {
                var seq = 0;
                for (var i = 0; i < propsAndTitles.Length; i += 2)
                {
                    builder.OpenComponent<ElTableColumn>(seq++);
                    builder.AddAttribute(seq++, "Property", propsAndTitles[i]);
                    builder.AddAttribute(seq++, "Text", propsAndTitles[i + 1]);
                    builder.CloseComponent();
                }
            };
        }

        private static readonly string[] DupPair = { "u_barcode", "颜色", "u_color", "颜色", "u_spec", "规格" };

        private IRenderedComponent<ElTable> RenderFirst(string[] pair) => Render<ElTable>(p => p
            .Add(x => x.DataSource, Rows())
            .Add(x => x.AutoGenerateColumns, false)
            .Add(x => x.HasSelectionColumn, false)
            .Add(x => x.EnablePagination, false)
            .AddChildContent(Cols(pair)));

        /// <summary>首帧 + 一次真实更新（<c>Render()</c> 重渲染，走有旧帧可比的 diff）。返回更新阶段抛出的异常，没抛就是 null。</summary>
        private Exception RerenderDup(string[] pair = null)
        {
            var cut = RenderFirst(pair ?? DupPair);
            return Record.Exception(() => cut.Render());
        }

        /// <summary>表头各格的文字（跳过 gutter 那种空格子），用来核对列没因为换键而错位。</summary>
        private static List<string> HeaderTexts(IRenderedComponent<ElTable> cut) =>
            cut.FindAll("thead th").Select(th => th.QuerySelector(".cell")?.TextContent?.Trim() ?? "")
               .Where(t => t.Length > 0).ToList();

        [Fact]
        public void DuplicateColumnTitles_RerenderDoesNotThrow()
        {
            var ex = RerenderDup();
            Assert.Null(ex);
        }

        [Fact]
        public void ThreeIdenticalTitles_RerenderDoesNotThrow()
        {
            var ex = RerenderDup(new[] { "u_barcode", "颜色", "u_color", "颜色", "u_spec", "颜色" });
            Assert.Null(ex);
        }

        [Fact]
        public void DuplicateColumnTitles_HeaderAndBodyStayInTheirOwnColumns()
        {
            var cut = RenderFirst(DupPair);
            Assert.Equal(new[] { "颜色", "颜色", "规格" }, HeaderTexts(cut));
            var cells = cut.FindAll("tbody tr")[0].QuerySelectorAll("td .cell")
                           .Select(x => x.TextContent.Trim()).ToList();
            Assert.Equal(new[] { "BC-1", "红色", "M" }, cells);
        }

        [Fact]
        public void DistinctColumnTitles_StillRender()
        {
            var cut = RenderFirst(new[] { "u_barcode", "条码", "u_color", "颜色" });
            Assert.Equal(new[] { "条码", "颜色" }, HeaderTexts(cut));
            var ex = Record.Exception(() => cut.Render());
            Assert.Null(ex);
        }
    }
}
