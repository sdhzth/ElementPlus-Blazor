using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElTable.AutoFitHeight：自动撑满视口剩余高度，但手动 Height / MaxHeight 永远优先。
    /// JS 测量在 bUnit 里没有真实浏览器，这里直接调用 [JSInvokable] 回传点验证渲染结果。
    /// </summary>
    public class ElTableAutoFitHeightTests : BunitContext
    {
        public ElTableAutoFitHeightTests()
        {
            Services.AddElementPlusServices();
            Services.AddSingleton(new HttpClient());
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        public class Row
        {
            public string Name { get; set; }
        }

        private static List<Row> Rows() => new() { new Row { Name = "A" }, new Row { Name = "B" } };

        private IRenderedComponent<ElTable> RenderTable(
            bool autoFit = false, int height = 0, int maxHeight = 0,
            int minHeight = 120, int maxFitHeight = 0)
        {
            var child = new RenderFragment(b =>
            {
                b.OpenComponent<ElTableColumn>(0);
                b.AddAttribute(1, "Property", "Name");
                b.AddAttribute(2, "Text", "名称");
                b.CloseComponent();
            });

            return Render<ElTable>(p => p
                .Add(x => x.DataSource, Rows())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.EnablePagination, false)
                .Add(x => x.AutoFitHeight, autoFit)
                .Add(x => x.Height, height)
                .Add(x => x.MaxHeight, maxHeight)
                .Add(x => x.FitMinHeight, minHeight)
                .Add(x => x.FitMaxHeight, maxFitHeight)
                .AddChildContent(child));
        }

        private static string RootStyle(IRenderedComponent<ElTable> cut) =>
            cut.Find("div.el-table").GetAttribute("style") ?? string.Empty;

        private static string BodyStyle(IRenderedComponent<ElTable> cut) =>
            cut.Find("div.el-table__body-wrapper").GetAttribute("style") ?? string.Empty;

        [Fact]
        public async Task 默认不启用_不产生定高_回传也被忽略()
        {
            var cut = RenderTable();
            Assert.DoesNotContain("height:", RootStyle(cut).Replace("min-height:", ""));

            await cut.InvokeAsync(() => cut.Instance.JsTableAutoFitHeight(600));

            Assert.DoesNotContain("height:600px", RootStyle(cut));
        }

        [Fact]
        public async Task 自动回传高度_外层和表体都按实测值定高()
        {
            var cut = RenderTable(autoFit: true);

            await cut.InvokeAsync(() => cut.Instance.JsTableAutoFitHeight(600));

            Assert.Contains("height:600px", RootStyle(cut));
            // 表体 = 实测高 - 表头 49px，和手动 Height 完全同一套滚动逻辑
            Assert.Contains("height:551px", BodyStyle(cut));
        }

        [Fact]
        public async Task 手动Height优先_自动回传不覆盖()
        {
            var cut = RenderTable(autoFit: true, height: 320);

            await cut.InvokeAsync(() => cut.Instance.JsTableAutoFitHeight(600));

            Assert.Contains("height:320px", RootStyle(cut));
            Assert.DoesNotContain("height:600px", RootStyle(cut));
            Assert.Contains("height:271px", BodyStyle(cut));
        }

        [Fact]
        public async Task 手动MaxHeight优先_自动回传不覆盖()
        {
            var cut = RenderTable(autoFit: true, maxHeight: 400);

            await cut.InvokeAsync(() => cut.Instance.JsTableAutoFitHeight(600));

            Assert.Contains("max-height:400px", RootStyle(cut));
            Assert.DoesNotContain("height:600px", RootStyle(cut));
            Assert.Contains("max-height:351px", BodyStyle(cut));
        }

        [Fact]
        public async Task FitMinHeight_与_FitMaxHeight_夹住自动高度()
        {
            var cut = RenderTable(autoFit: true, minHeight: 200, maxFitHeight: 500);

            await cut.InvokeAsync(() => cut.Instance.JsTableAutoFitHeight(80));
            Assert.Contains("height:200px", RootStyle(cut));

            await cut.InvokeAsync(() => cut.Instance.JsTableAutoFitHeight(900));
            Assert.Contains("height:500px", RootStyle(cut));
            Assert.DoesNotContain("height:200px", RootStyle(cut));

            await cut.InvokeAsync(() => cut.Instance.JsTableAutoFitHeight(360));
            Assert.Contains("height:360px", RootStyle(cut));
        }

        [Fact]
        public async Task 同一高度重复回传_不产生多余状态变更()
        {
            var cut = RenderTable(autoFit: true);
            var before = cut.RenderCount;

            await cut.InvokeAsync(() => cut.Instance.JsTableAutoFitHeight(600));
            var afterFirst = cut.RenderCount;
            await cut.InvokeAsync(() => cut.Instance.JsTableAutoFitHeight(600));

            Assert.True(afterFirst > before);
            Assert.Equal(afterFirst, cut.RenderCount);
        }
    }
}
