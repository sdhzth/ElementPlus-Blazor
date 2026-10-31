using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElDropDownTableGrowTests : BunitContext
    {
        public ElDropDownTableGrowTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private class DemoRow
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string City { get; set; }
        }

        private static List<DemoRow> MakeRows() => new List<DemoRow>
        {
            new DemoRow { Id = 1, Name = "Alice", City = "Beijing" },
            new DemoRow { Id = 2, Name = "Bob", City = "Shanghai" },
            new DemoRow { Id = 3, Name = "Carol", City = "Beijing" },
            new DemoRow { Id = 4, Name = "Dave", City = "Guangzhou" },
        };

        private static RenderFragment ColsFragment() => builder =>
        {
            builder.OpenComponent<ElTableColumn>(0);
            builder.AddAttribute(1, "Property", "Name");
            builder.AddAttribute(2, "Label", "名称");
            builder.CloseComponent();
            builder.OpenComponent<ElTableColumn>(3);
            builder.AddAttribute(4, "Property", "City");
            builder.AddAttribute(5, "Label", "城市");
            builder.CloseComponent();
        };

        [Fact]
        public void HiddenByDefault_PanelNotRendered()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.TextField, "Name"));

            Assert.Empty(cut.FindAll(".el-dropdown-table-grow__panel"));
        }

        [Fact]
        public void Placeholder_ShownWhenEmpty()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Placeholder, "请选择城市")
                .Add(x => x.Items, MakeRows()));

            var ph = cut.Find(".el-select__placeholder");
            Assert.Equal("请选择城市", ph.TextContent);
        }

        [Fact]
        public void ClickTrigger_OpensPanelWithElTable()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.ChildContent, ColsFragment()));

            cut.Find(".el-select__wrapper").Click();

            Assert.NotEmpty(cut.FindAll(".el-dropdown-table-grow__panel"));
            Assert.NotEmpty(cut.FindAll(".el-table"));
        }

        [Fact]
        public void AllowSearchFalse_NoSearchBox()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.AllowSearch, false));

            cut.Find(".el-select__wrapper").Click();

            Assert.Empty(cut.FindAll(".el-dropdown-table-grow__search"));
        }

        [Fact]
        public async Task Search_FiltersRows()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.ValueField, "Name"));

            Assert.Equal(4, cut.Instance.FilteredCount);

            await cut.Instance.OnSearchInputAsync(new ChangeEventArgs { Value = "Alice" });
            Assert.Equal(1, cut.Instance.FilteredCount);

            await cut.Instance.OnSearchInputAsync(new ChangeEventArgs { Value = "Beijing" });
            Assert.Equal(2, cut.Instance.FilteredCount);
        }

        [Fact]
        public async Task SingleSelect_RowClick_SetsValueAndCloses()
        {
            var rows = MakeRows();
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, rows)
                .Add(x => x.ValueField, "Id")
                .Add(x => x.TextField, "Name"));

            await cut.Instance.OnTableRowClickAsync(rows[1]);

            Assert.Equal(rows[1].Id, cut.Instance.Value);
            Assert.Empty(cut.FindAll(".el-dropdown-table-grow__panel"));
        }

        [Fact]
        public async Task MultiSelect_SelectionChanged_SetsValues()
        {
            var rows = MakeRows();
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, rows)
                .Add(x => x.Multiple, true)
                .Add(x => x.ValueField, "Id"));

            await cut.Instance.OnTableSelectionChangedAsync(new HashSet<object> { rows[0], rows[2] });

            Assert.NotNull(cut.Instance.Values);
            var values = cut.Instance.Values.Cast<object>().ToList();
            Assert.Contains(rows[0].Id, values);
            Assert.Contains(rows[2].Id, values);
        }

        [Fact]
        public async Task Clearable_ClearsValue()
        {
            var rows = MakeRows();
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, rows)
                .Add(x => x.ValueField, "Id")
                .Add(x => x.Clearable, true)
                .Add(x => x.Value, rows[0].Id));

            await cut.Instance.ClearAsync();

            Assert.Null(cut.Instance.Value);
        }

        [Fact]
        public void Disabled_NotOpen()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.Disabled, true));

            cut.Find(".el-select__wrapper").Click();

            Assert.Empty(cut.FindAll(".el-dropdown-table-grow__panel"));
        }

        [Fact]
        public void CustomWidth_AppliedToPanel()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.Width, "420px"));

            cut.Find(".el-select__wrapper").Click();

            var panel = cut.Find(".el-dropdown-table-grow__panel");
            Assert.Contains("width:420px", panel.GetAttribute("style"));
        }

        [Fact]
        public async Task Escape_ClosesPanel()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows()));

            await cut.Instance.ShowAsync();
            Assert.NotEmpty(cut.FindAll(".el-dropdown-table-grow__panel"));

            await cut.Instance.OnKeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
            Assert.Empty(cut.FindAll(".el-dropdown-table-grow__panel"));
        }

        // ---------------- SearchProperties：按指定字段（可多个）搜索 ----------------

        [Fact]
        public async Task SearchProperties_SingleField_OnlyThatFieldIsSearched()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.SearchProperties, new[] { "Name" }));

            // 命中 Name 字段
            await cut.Instance.OnSearchInputAsync(new ChangeEventArgs { Value = "Alice" });
            Assert.Equal(1, cut.Instance.FilteredCount);

            // Beijing 只在 City 字段里，Name 受限后不应命中
            await cut.Instance.OnSearchInputAsync(new ChangeEventArgs { Value = "Beijing" });
            Assert.Equal(0, cut.Instance.FilteredCount);
        }

        [Fact]
        public async Task SearchProperties_MultipleFields_AnyFieldMatches()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.SearchProperties, new[] { "Name", "City" }));

            await cut.Instance.OnSearchInputAsync(new ChangeEventArgs { Value = "Beijing" });
            Assert.Equal(2, cut.Instance.FilteredCount);

            await cut.Instance.OnSearchInputAsync(new ChangeEventArgs { Value = "Carol" });
            Assert.Equal(1, cut.Instance.FilteredCount);
        }

        [Fact]
        public async Task SearchProperties_Empty_SearchesAllProperties()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows()));

            // 不限定字段时 City 也参与检索
            await cut.Instance.OnSearchInputAsync(new ChangeEventArgs { Value = "Beijing" });
            Assert.Equal(2, cut.Instance.FilteredCount);
        }

        // ---------------- 面板宽度：可与触发器宽度解耦 ----------------

        [Fact]
        public void PanelWidth_OverridesTriggerWidthForPanelOnly()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.Width, "240px")
                .Add(x => x.PanelWidth, "640px"));

            cut.Find(".el-select__wrapper").Click();

            Assert.Contains("width:240px", cut.Find(".el-dropdown-table-grow").GetAttribute("style"));
            Assert.Contains("width:640px", cut.Find(".el-dropdown-table-grow__panel").GetAttribute("style"));
        }

        [Fact]
        public void PanelMinWidthApplied_WhenPanelWidthNotSet()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.Width, "200px")
                .Add(x => x.PanelMinWidth, "520px"));

            cut.Find(".el-select__wrapper").Click();

            // 未给 PanelWidth 时宽度回落 Width，但 min-width 更高 —— 面板实际以 520px 呈现。
            var style = cut.Find(".el-dropdown-table-grow__panel").GetAttribute("style");
            Assert.Contains("width:200px", style);
            Assert.Contains("min-width:520px", style);
        }

        [Fact]
        public void NoWidthAtAll_PanelIsAutoWithTriggerMinWidth()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.PanelMinWidth, "520px"));

            cut.Find(".el-select__wrapper").Click();

            var style = cut.Find(".el-dropdown-table-grow__panel").GetAttribute("style");
            Assert.Contains("min-width:520px", style);
            Assert.Contains("width:auto", style);
        }

        [Fact]
        public void PanelMaxWidth_Applied()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.PanelWidth, "640px")
                .Add(x => x.PanelMaxWidth, "90vw"));

            cut.Find(".el-select__wrapper").Click();

            Assert.Contains("max-width:90vw", cut.Find(".el-dropdown-table-grow__panel").GetAttribute("style"));
        }

        [Fact]
        public void NoWidthAtAll_PanelKeepsAtLeastTriggerWidth()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows()));

            cut.Find(".el-select__wrapper").Click();

            var style = cut.Find(".el-dropdown-table-grow__panel").GetAttribute("style");
            Assert.Contains("min-width:100%", style);
        }

        // ---------------- Size：表格紧凑 / 正常 ----------------

        [Fact]
        public void Size_Small_AddsCompactClassesToWrapperAndTable()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.ChildContent, ColsFragment())
                .Add(x => x.Size, InputSize.Small));

            Assert.Contains("el-dropdown-table-grow--small", cut.Find(".el-dropdown-table-grow").GetAttribute("class"));

            cut.Find(".el-select__wrapper").Click();
            Assert.Contains("el-table--small", cut.Find(".el-table").GetAttribute("class"));
        }

        [Fact]
        public void Size_Default_IsNormalSize()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.ChildContent, ColsFragment()));

            var wrapperCls = cut.Find(".el-dropdown-table-grow").GetAttribute("class");
            Assert.DoesNotContain("el-dropdown-table-grow--small", wrapperCls);
            Assert.DoesNotContain("el-dropdown-table-grow--large", wrapperCls);

            cut.Find(".el-select__wrapper").Click();
            var tableCls = cut.Find(".el-table").GetAttribute("class");
            Assert.DoesNotContain("el-table--small", tableCls);
            Assert.DoesNotContain("el-table--large", tableCls);
        }

        [Fact]
        public void Size_Large_AddsLargeClasses()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.ChildContent, ColsFragment())
                .Add(x => x.Size, InputSize.Large));

            Assert.Contains("el-dropdown-table-grow--large", cut.Find(".el-dropdown-table-grow").GetAttribute("class"));

            cut.Find(".el-select__wrapper").Click();
            Assert.Contains("el-table--large", cut.Find(".el-table").GetAttribute("class"));
        }

        // ---------------- 弹层定位 + 滚轮：不铺 backdrop，点外面交给 document 捕获阶段 ----------------

        /// <summary>
        /// 面板开着时不能再渲染全屏 backdrop：它是 position:fixed 覆盖层，Blink 的滚轮滚动链会走到「视口」
        /// 而不是页面的真实滚动容器（如 .demo-main），开着面板时整页就滚不动了。
        /// 负向断言配正向对照（面板确实渲染了），否则「选择器写错导致永远查不到」也会假绿。
        /// </summary>
        [Fact]
        public void OpenPanel_UsesOutsideHandlerInsteadOfBackdrop()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.ChildContent, ColsFragment()));

            cut.Find(".el-select__wrapper").Click();

            Assert.NotEmpty(cut.FindAll(".el-dropdown-table-grow__panel"));
            Assert.Empty(cut.FindAll(".el-dropdown-table-grow__backdrop"));
        }

        /// <summary>展开时把「点外面收起」挂到 document 捕获阶段，且必须带上面板选择器与 [JSInvokable] 方法名。</summary>
        [Fact]
        public void OpenPanel_AttachesOutsideHandlerWithSelectorAndMethod()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.ChildContent, ColsFragment()));

            cut.Find(".el-select__wrapper").Click();

            var attach = JSInterop.Invocations
                .Where(i => i.Identifier == "elementPickerOutsideAttach")
                .ToList();
            var invocation = Assert.Single(attach);
            // 参数形如 (root, dropdownSelector, dotNetRef, methodName)：漏一个或写错字符串只有运行时才炸，
            // 这里把它钉在编译期可跑的断言里。
            Assert.Contains(".el-dropdown-table-grow__panel", invocation.Arguments);
            Assert.Contains("OnOutsideClickAsync", invocation.Arguments);
            Assert.True(invocation.Arguments.Count >= 4,
                $"elementPickerOutsideAttach 需要 (root, selector, dotNetRef, methodName)，实测 {invocation.Arguments.Count} 个参数");
        }

        /// <summary>收起时解绑 document 监听，否则离开页面后监听器会一直挂在 document 上。</summary>
        [Fact]
        public void ClosePanel_DetachesOutsideHandler()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.ChildContent, ColsFragment()));

            cut.Find(".el-select__wrapper").Click();
            cut.Find(".el-select__wrapper").Click();

            Assert.Empty(cut.FindAll(".el-dropdown-table-grow__panel"));
            Assert.Contains(JSInterop.Invocations, i => i.Identifier == "elementPickerOutsideDetach");
        }

        /// <summary>JS 回调（点在控件外）走的入口：收到回调就收起弹层。</summary>
        [Fact]
        public async Task OnOutsideClickAsync_ClosesPanel()
        {
            var cut = Render<ElDropDownTableGrow>(p => p
                .Add(x => x.Items, MakeRows())
                .Add(x => x.ChildContent, ColsFragment()));

            cut.Find(".el-select__wrapper").Click();
            Assert.NotEmpty(cut.FindAll(".el-dropdown-table-grow__panel"));

            await cut.Instance.OnOutsideClickAsync();

            Assert.Empty(cut.FindAll(".el-dropdown-table-grow__panel"));
        }

        /// <summary>
        /// 面板滚条槽契约：表体与表头的 <c>::-webkit-scrollbar</c> 的 <c>width</c>（Chromium 里这一轴只管<b>纵向</b>那条）
        /// 必须由组件自己**逐个显式钉成同一个值**（批次⑨ 起为 0，即去掉滚动条）。两个容器都是
        /// <c>overflow-y:scroll; scrollbar-gutter:stable</c>，槽宽差多少，两个表格的可视宽度就差多少，
        /// 列边界于是越往右偏得越多（改前表体 8px、表头只被全局 4px 规则命中 → 实测偏移 0/0.9/2.9/4px，见 Temp/verify-batch8.log 的 G 段）。
        /// 契约测的是「两侧都被本地钉住、且同值、且各自只有一处声明」这个不变量，不是「必须写在同一条规则里」：
        /// 批次96 按用户裁定把面板的<b>横轴</b>（<c>height</c>）装回库口径 6→12，而两个容器的横轴取值本来就不一样
        /// （表体 6px、表头 0），于是拆成了两条规则 —— 纵轴那一侧仍然各自钉死 0，本条继续钉它。
        /// 值从 4 改到 0 时这里跟着改；把任意一侧的 <c>width</c> 删掉（退回全局 4px）或改成别的数，本条红。
        /// 横轴的口径在 <c>ScrollbarCaliberGuardTests.Guard_GrowDropDownPanel_HorizontalBarUsesLibraryCaliber_VerticalSlotStaysZero</c>。
        /// </summary>
        [Fact]
        public void Guard_PanelScrollbarGutterIsOneSharedEqualWidthRule()
        {
            var css = ComponentStyleBlock();
            const string body = ".el-dropdown-table-grow__panel .el-table__body-wrapper::-webkit-scrollbar";
            const string header = ".el-dropdown-table-grow__panel .el-table__header-wrapper::-webkit-scrollbar";

            // 只认「整条规则的每个选择器都以 ::-webkit-scrollbar 结尾」的那些
            // （-thumb / -button 规则不算，它们不决定槽宽；:hover 那条的选择器不等于 body/header，也不算）。
            var declared = new Dictionary<string, List<string>>();
            foreach (Match m in Regex.Matches(css, @"([^{}]+)\{([^{}]*)\}"))
            {
                var parts = Regex.Replace(m.Groups[1].Value, @"\s+", " ").Split(',')
                    .Select(s => s.Trim()).ToList();
                if (parts.Count == 0 || !parts.All(p => p.EndsWith("::-webkit-scrollbar", StringComparison.Ordinal)))
                    continue;
                foreach (var p in parts)
                {
                    if (p != body && p != header) continue;
                    // 这条规则没声明 width 就跳过：那一侧的 width 退回全局，正是改前那种错位的形状。
                    var w = Regex.Match(m.Groups[2].Value, @"width:\s*(\d+)(?:px)?");
                    if (w.Success)
                    {
                        if (!declared.TryGetValue(p, out var list)) declared[p] = list = new List<string>();
                        list.Add(w.Groups[1].Value);
                    }
                }
            }

            // 有牙的地方：某一侧整条删掉 → 对应那条红；只改一侧的数值 → 下面第三条红；同一侧写两处 → 第二条红。
            Assert.Contains(body, declared.Keys);
            Assert.Contains(header, declared.Keys);
            Assert.Single(declared[body]);
            Assert.Single(declared[header]);
            // 批次⑨ 按用户「能去就去」的批准把面板滚道去掉，批次96 明确**不反转纵轴**（列对齐的前提）：
            // 两侧同为 0（实测槽宽 0/0、clientW 316/316、列宽差值全 0、滚轮照旧，见 Temp/diag-batch8-scrollbar-zero2.log 的 B 段，
            // 常驻尺子 verify-batch8.cjs 的 G1/G2/G3）。
            Assert.Equal("0", declared[body][0]);
            Assert.Equal(declared[body][0], declared[header][0]);
        }

        /// <summary>取组件内联 &lt;style&gt; 段（已剥注释）。</summary>
        private static string ComponentStyleBlock()
        {
            var path = Path.Combine(RepoRoot(), "src", "Components", "ElDropDownTableGrow.razor");
            var text = File.ReadAllText(path);
            var start = text.IndexOf("<style>", StringComparison.Ordinal);
            var stop = text.IndexOf("</style>", StringComparison.Ordinal);
            Assert.True(start >= 0 && stop > start, "ElDropDownTableGrow.razor 中没有 <style> 段");
            var css = text.Substring(start + 7, stop - start - 7);
            return Regex.Replace(css, @"/\*[\s\S]*?\*/", string.Empty);
        }

        private static string RepoRoot([CallerFilePath] string file = null)
            => new FileInfo(file).Directory.Parent.Parent.FullName;
    }
}
