using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// <see cref="ElProTabsGrow"/> 页签导航（ProTabs 风格）单元测试。
    /// <para>
    /// 覆盖：渲染结构（圆点/图标/标题/关闭按钮）、激活态与 roving tabindex、
    /// 关闭按钮两种显示时机、激活色（属性 + 运行时方法）、
    /// 点击/关闭/「更多」交互与事件、键盘操作、空 Path 过滤，
    /// 以及 index.css / dom.js 的兼容性守卫。
    /// </para>
    /// </summary>
    public class ElProTabsGrowTests : BunitContext
    {
        public ElProTabsGrowTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        #region 辅助

        private static List<ElProTabsGrowItem> Tabs(params string[] paths) =>
            paths.Select((p, i) => new ElProTabsGrowItem { Path = p, Title = $"Tab{i + 1}" }).ToList();

        private static string RepoRoot([CallerFilePath] string file = null)
        {
            // .../test/Element.ComponentTests/ElProTabsGrowTests.cs → 上溯三层
            var directory = new FileInfo(file).Directory;

            return directory.Parent.Parent.FullName;
        }

        /// <summary>读取 index.css 中本组件的样式段（截到下一个「段分隔注释」为止）。</summary>
        private static string CssSection()
        {
            var path = Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "css", "index.css");
            var text = File.ReadAllText(path);
            var marker = text.IndexOf("ElProTabsGrow 页签导航", StringComparison.Ordinal);

            Assert.True(marker >= 0, "index.css 中未找到 ElProTabsGrow 样式段");

            var start = text.LastIndexOf("/*", marker, StringComparison.Ordinal);
            var next = text.IndexOf("\n/* =", marker + 10, StringComparison.Ordinal);
            var section = next >= 0 ? text.Substring(start, next - start) : text.Substring(start);

            // 去掉注释，避免注释里的说明文字命中禁用词
            return Regex.Replace(section, @"/\*[\s\S]*?\*/", string.Empty);
        }

        /// <summary>
        /// 取出样式段里选择器（逗号分组也算）恰好命中 <paramref name="selector"/> 的规则体。
        /// 按整条选择器列表逐段 Trim 后比较，避免 `.el-pro-tabs-grow__refresh` 误命中 `__refresh-icon`。
        /// </summary>
        private static List<string> RuleBodies(string css, string selector)
        {
            var bodies = new List<string>();

            foreach (var chunk in css.Split('}'))
            {
                var brace = chunk.IndexOf('{');

                if (brace < 0)
                {
                    continue;
                }

                var selectors = chunk.Substring(0, brace).Split(',')
                    .Select(x => x.Trim()).ToList();

                if (selectors.Contains(selector))
                {
                    bodies.Add(chunk.Substring(brace + 1));
                }
            }

            return bodies;
        }

        /// <summary>读取 dom.js 中本组件的 JS 通道段。</summary>
        private static string JsSection()
        {
            var path = Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "js", "dom.js");
            var text = File.ReadAllText(path);
            var marker = text.IndexOf("ElProTabsGrow 页签导航：溢出测量", StringComparison.Ordinal);

            Assert.True(marker >= 0, "dom.js 中未找到 ElProTabsGrow 通道");

            // 截到文件末尾（本通道是 dom.js 的最后一段）
            return text.Substring(marker);
        }

        #endregion

        #region 渲染结构

        [Fact]
        public void RendersItemsWithDotTitleAndCloseButton()
        {
            var cut = Render<ElProTabsGrow>(p => p.Add(x => x.Items, Tabs("/a", "/b", "/c")));

            var items = cut.FindAll(".el-pro-tabs-grow__item");
            Assert.Equal(3, items.Count);
            Assert.Equal("Tab1", cut.FindAll(".el-pro-tabs-grow__label")[0].TextContent.Trim());
            // 没给 Icon → 每个标签都有圆点
            Assert.Equal(3, cut.FindAll(".el-pro-tabs-grow__dot").Count);
            // 默认可关闭 → 每个标签都有 X
            Assert.Equal(3, cut.FindAll(".el-pro-tabs-grow__close").Count);
            // 根节点带 aria 语义
            Assert.Equal("tablist", cut.Find(".el-pro-tabs-grow").GetAttribute("role"));
        }

        [Fact]
        public void ActiveItemGetsIsActiveAndRovingTabIndex()
        {
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a", "/b", "/c"))
                .Add(x => x.ActivePath, "/b"));

            var items = cut.FindAll(".el-pro-tabs-grow__item");
            Assert.DoesNotContain("is-active", items[0].ClassList);
            Assert.Contains("is-active", items[1].ClassList);
            Assert.DoesNotContain("is-active", items[2].ClassList);

            Assert.Equal("-1", items[0].GetAttribute("tabindex"));
            Assert.Equal("0", items[1].GetAttribute("tabindex"));
            Assert.Equal("false", items[0].GetAttribute("aria-selected"));
            Assert.Equal("true", items[1].GetAttribute("aria-selected"));
        }

        [Fact]
        public void ActivePathMatchIsCaseInsensitive()
        {
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/Basic", "/Table"))
                .Add(x => x.ActivePath, "/basic"));

            Assert.Contains("is-active", cut.FindAll(".el-pro-tabs-grow__item")[0].ClassList);
        }

        [Fact]
        public void ItemsWithEmptyPathAreIgnored()
        {
            var items = Tabs("/a");
            items.Add(new ElProTabsGrowItem { Path = null, Title = "空" });
            items.Add(new ElProTabsGrowItem { Path = "  ", Title = "空白" });
            items.Add(null);

            var cut = Render<ElProTabsGrow>(p => p.Add(x => x.Items, items));

            Assert.Single(cut.FindAll(".el-pro-tabs-grow__item"));
        }

        [Fact]
        public void IconTakesPrecedenceOverDotAndCanBeSwitchedOff()
        {
            var items = new List<ElProTabsGrowItem>
            {
                new() { Path = "/a", Title = "A", Icon = "s-home" },
                new() { Path = "/b", Title = "B" }
            };

            var cut = Render<ElProTabsGrow>(p => p.Add(x => x.Items, items));

            // 有图标 → 只有 1 个圆点（第二项），图标自动补 el-icon- 前缀
            Assert.Single(cut.FindAll(".el-pro-tabs-grow__dot"));
            Assert.Equal("el-pro-tabs-grow__icon el-icon-s-home",
                cut.Find(".el-pro-tabs-grow__icon").GetAttribute("class"));

            // 关掉图标 → 两项都回落到圆点
            cut.Render(p => p.Add(x => x.Items, items).Add(x => x.ShowIcon, false));
            Assert.Equal(2, cut.FindAll(".el-pro-tabs-grow__dot").Count);
            Assert.Empty(cut.FindAll(".el-pro-tabs-grow__icon"));

            // 关掉圆点且无图标 → 两者都不渲染
            cut.Render(p => p.Add(x => x.Items, items).Add(x => x.ShowIcon, false).Add(x => x.ShowDot, false));
            Assert.Empty(cut.FindAll(".el-pro-tabs-grow__dot"));
        }

        #endregion

        #region 关闭按钮

        [Fact]
        public void ItemClosableFalseHidesItsCloseButton()
        {
            var items = Tabs("/a", "/b");
            items[1].Closable = false;

            var cut = Render<ElProTabsGrow>(p => p.Add(x => x.Items, items));

            Assert.Equal(2, cut.FindAll(".el-pro-tabs-grow__item").Count);
            Assert.Single(cut.FindAll(".el-pro-tabs-grow__close"));
            Assert.Empty(cut.FindAll(".el-pro-tabs-grow__item:nth-child(2) .el-pro-tabs-grow__close"));
        }

        [Fact]
        public void GlobalClosableFalseRemovesEveryCloseButton()
        {
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a", "/b"))
                .Add(x => x.Closable, false));

            Assert.Empty(cut.FindAll(".el-pro-tabs-grow__close"));
        }

        [Fact]
        public void DisabledItemHidesCloseButton()
        {
            var items = Tabs("/a", "/b");
            items[0].Disabled = true;

            var cut = Render<ElProTabsGrow>(p => p.Add(x => x.Items, items));

            var nodes = cut.FindAll(".el-pro-tabs-grow__item");
            Assert.Contains("is-disabled", nodes[0].ClassList);
            Assert.Equal("true", nodes[0].GetAttribute("aria-disabled"));
            Assert.Equal("-1", nodes[0].GetAttribute("tabindex"));
            Assert.Single(cut.FindAll(".el-pro-tabs-grow__close"));
        }

        [Fact]
        public void CloseButtonDisplaySwitchesModifierClass()
        {
            // 默认 Hover：不加 is-close-always，X 交给 CSS 的 :hover 显现
            var cut = Render<ElProTabsGrow>(p => p.Add(x => x.Items, Tabs("/a")));
            Assert.DoesNotContain("is-close-always", cut.Find(".el-pro-tabs-grow").ClassList);

            cut.Render(p => p
                .Add(x => x.Items, Tabs("/a"))
                .Add(x => x.CloseButtonDisplay, ElProTabsGrowCloseButtonDisplay.Always));
            Assert.Contains("is-close-always", cut.Find(".el-pro-tabs-grow").ClassList);

            // 默认值必须就是 Hover（用户要求：只有鼠标移到该按钮上才显示）
            Assert.Equal(ElProTabsGrowCloseButtonDisplay.Hover, new ElProTabsGrow().CloseButtonDisplay);
            Assert.Equal(0, (int)ElProTabsGrowCloseButtonDisplay.Hover);
        }

        #endregion

        #region 激活色（属性 + 快捷方法）

        [Fact]
        public void ActiveTextColorEmitsCssVariables()
        {
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a"))
                .Add(x => x.ActiveTextColor, "#7c8ee8")
                .Add(x => x.ActiveDotColor, "#b9c3f5"));

            var style = cut.Find(".el-pro-tabs-grow").GetAttribute("style");
            Assert.Contains("--eptg-active-color:#7c8ee8", style);
            Assert.Contains("--eptg-active-dot-color:#b9c3f5", style);
        }

        [Fact]
        public void NoColorParametersEmitNoVariables()
        {
            var cut = Render<ElProTabsGrow>(p => p.Add(x => x.Items, Tabs("/a")));

            var style = cut.Find(".el-pro-tabs-grow").GetAttribute("style");
            Assert.DoesNotContain("--eptg-active-color", style);
            Assert.DoesNotContain("--eptg-active-dot-color", style);
        }

        [Fact]
        public void SetActiveTextColorOverridesParameterAndResetRestores()
        {
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a"))
                .Add(x => x.ActiveTextColor, "#111111"));

            Assert.Contains("--eptg-active-color:#111111", cut.Find(".el-pro-tabs-grow").GetAttribute("style"));

            cut.InvokeAsync(() => cut.Instance.SetActiveTextColor("#e6a23c"));
            Assert.Contains("--eptg-active-color:#e6a23c", cut.Find(".el-pro-tabs-grow").GetAttribute("style"));

            cut.InvokeAsync(() => cut.Instance.ResetActiveTextColor());
            Assert.Contains("--eptg-active-color:#111111", cut.Find(".el-pro-tabs-grow").GetAttribute("style"));
        }

        #endregion

        #region 交互与事件

        [Fact]
        public async System.Threading.Tasks.Task ClickingItemRaisesSelectAndUpdatesActivePath()
        {
            ElProTabsGrowItem selected = null;
            var changed = new List<string>();

            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a", "/b"))
                .Add(x => x.ActivePath, "/a")
                .Add(x => x.OnSelect, (ElProTabsGrowItem i) => selected = i)
                .Add(x => x.ActivePathChanged, (string v) => changed.Add(v)));

            await cut.FindAll(".el-pro-tabs-grow__item")[1].ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            Assert.Equal("/b", selected?.Path);
            Assert.Equal(new[] { "/b" }, changed);
            Assert.Contains("is-active", cut.FindAll(".el-pro-tabs-grow__item")[1].ClassList);
        }

        [Fact]
        public async System.Threading.Tasks.Task ClickingActiveItemStillRaisesSelectButNotActivePathChanged()
        {
            var selectCount = 0;
            var changedCount = 0;

            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a", "/b"))
                .Add(x => x.ActivePath, "/a")
                .Add(x => x.OnSelect, (ElProTabsGrowItem i) => selectCount++)
                .Add(x => x.ActivePathChanged, (string v) => changedCount++));

            await cut.FindAll(".el-pro-tabs-grow__item")[0].ClickAsync(new MouseEventArgs());

            Assert.Equal(1, selectCount);
            Assert.Equal(0, changedCount);
        }

        [Fact]
        public async System.Threading.Tasks.Task ClickingCloseRaisesOnCloseOnly()
        {
            ElProTabsGrowItem closed = null;
            var selectCount = 0;

            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a", "/b"))
                .Add(x => x.OnClose, (ElProTabsGrowItem i) => closed = i)
                .Add(x => x.OnSelect, (ElProTabsGrowItem i) => selectCount++));

            await cut.FindAll(".el-pro-tabs-grow__close")[1].ClickAsync(new MouseEventArgs());

            Assert.Equal("/b", closed?.Path);
            // X 上挂了 stopPropagation，不应把点击也当成选中页签
            Assert.Equal(0, selectCount);
        }

        [Fact]
        public async System.Threading.Tasks.Task DisabledItemCannotBeSelected()
        {
            var items = Tabs("/a", "/b");
            items[1].Disabled = true;

            var selectCount = 0;
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, items)
                .Add(x => x.OnSelect, (ElProTabsGrowItem i) => selectCount++));

            await cut.FindAll(".el-pro-tabs-grow__item")[1].ClickAsync(new MouseEventArgs());

            Assert.Equal(0, selectCount);
        }

        #endregion

        #region 标签刷新图标（ShowRefresh）

        /// <summary>逐个页签统计某种图标的数量，用来断言「哪一项有、哪一项没有」而不是只看总数。</summary>
        private static List<int> PerItemCount(IReadOnlyList<AngleSharp.Dom.IElement> items, string selector) =>
            items.Select(x => x.QuerySelectorAll(selector).Length).ToList();

        [Fact]
        public void ShowRefreshOffByDefaultAndOnRendersOneIconPerTab()
        {
            var items = Tabs("/a", "/b", "/c");

            // 默认关：既有宿主观感不变。同一次渲染里页签和关闭按钮都在，
            // 证明 0 个刷新图标是「真的没渲染图标」，不是渲染了空表。
            var off = Render<ElProTabsGrow>(p => p.Add(x => x.Items, items));
            Assert.Equal(3, off.FindAll(".el-pro-tabs-grow__item").Count);
            Assert.Equal(3, off.FindAll(".el-pro-tabs-grow__close").Count);
            Assert.Empty(off.FindAll(".el-pro-tabs-grow__refresh"));

            // 正向对照：同一批 Items 只切 ShowRefresh
            var on = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, items)
                .Add(x => x.ShowRefresh, true));
            Assert.Equal(3, on.FindAll(".el-pro-tabs-grow__refresh").Count);
        }

        [Fact]
        public void ItemRefreshableFalseExitsOnlyItsOwnIcon()
        {
            var items = Tabs("/a", "/b", "/c");
            items[1].Refreshable = false;

            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, items)
                .Add(x => x.ShowRefresh, true));

            var rendered = cut.FindAll(".el-pro-tabs-grow__item");
            Assert.Equal(new[] { 1, 0, 1 }, PerItemCount(rendered, ".el-pro-tabs-grow__refresh"));
            // 退出刷新不动关闭：三项都还可关
            Assert.Equal(new[] { 1, 1, 1 }, PerItemCount(rendered, ".el-pro-tabs-grow__close"));
        }

        [Fact]
        public void PinnedTabGetsRefreshIconEvenWithoutCloseButton()
        {
            var items = Tabs("/", "/b");
            items[0].Closable = false;   // 置钉首页：不可关，但要能刷

            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, items)
                .Add(x => x.ShowRefresh, true));

            var rendered = cut.FindAll(".el-pro-tabs-grow__item");
            Assert.Equal(new[] { 0, 1 }, PerItemCount(rendered, ".el-pro-tabs-grow__close"));
            Assert.Equal(new[] { 1, 1 }, PerItemCount(rendered, ".el-pro-tabs-grow__refresh"));
        }

        [Fact]
        public void DisabledItemGetsNoRefreshIcon()
        {
            var items = Tabs("/a", "/b");
            items[1].Disabled = true;

            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, items)
                .Add(x => x.ShowRefresh, true));

            Assert.Equal(new[] { 1, 0 }, PerItemCount(cut.FindAll(".el-pro-tabs-grow__item"), ".el-pro-tabs-grow__refresh"));
        }

        [Fact]
        public async System.Threading.Tasks.Task ClickingRefreshIconRaisesOnRefreshTabOnly()
        {
            ElProTabsGrowItem refreshed = null;
            var refreshCount = 0;
            var selectCount = 0;

            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a", "/b"))
                .Add(x => x.ShowRefresh, true)
                .Add(x => x.OnRefreshTab, (ElProTabsGrowItem i) =>
                {
                    refreshed = i;
                    refreshCount++;
                })
                .Add(x => x.OnSelect, (ElProTabsGrowItem i) => selectCount++));

            await cut.FindAll(".el-pro-tabs-grow__refresh")[1].ClickAsync(new MouseEventArgs());

            Assert.Equal("/b", refreshed?.Path);
            Assert.Equal(1, refreshCount);
            // 图标上挂了 stopPropagation，点它不该被当成「选中该页签」
            Assert.Equal(0, selectCount);

            // 正向对照：点整项确实会冒泡出 OnSelect，证明上一条的 0 不是「事件根本没接上」
            await cut.FindAll(".el-pro-tabs-grow__item")[0].ClickAsync(new MouseEventArgs());
            Assert.Equal(1, selectCount);
        }

        [Fact]
        public void RefreshIconCarriesAccessibleLabelAndIsNotATabStop()
        {
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a", "/b"))
                .Add(x => x.ShowRefresh, true));

            var icon = cut.FindAll(".el-pro-tabs-grow__refresh")[1];

            Assert.Equal("button", icon.GetAttribute("role"));
            Assert.Equal("刷新 Tab2", icon.GetAttribute("aria-label"));
            Assert.Equal("刷新", icon.GetAttribute("title"));
            // tabindex=-1：键盘走页签时不停在图标上（ ENTER 已由整项处理）
            Assert.Equal("-1", icon.GetAttribute("tabindex"));
            Assert.NotNull(icon.QuerySelector(".el-pro-tabs-grow__refresh-icon"));
        }

        [Fact]
        public void RefreshIconClosesOpenMorePanel()
        {
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a", "/b"))
                .Add(x => x.ShowRefresh, true));

            cut.Find(".el-pro-tabs-grow__more").Click();
            Assert.Single(cut.FindAll(".el-pro-tabs-grow__more-panel"));

            cut.FindAll(".el-pro-tabs-grow__refresh")[0].Click();

            Assert.Empty(cut.FindAll(".el-pro-tabs-grow__more-panel"));
        }

        #endregion

        #region 「更多」下拉

        [Fact]
        public void MoreButtonOpensPanelListingEveryTabAndActions()
        {
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a", "/b", "/c"))
                .Add(x => x.ActivePath, "/c"));

            Assert.Single(cut.FindAll(".el-pro-tabs-grow__more"));
            Assert.Empty(cut.FindAll(".el-pro-tabs-grow__more-panel"));

            cut.Find(".el-pro-tabs-grow__more").Click();

            Assert.Single(cut.FindAll(".el-pro-tabs-grow__more-panel"));
            Assert.Equal(3, cut.FindAll(".el-pro-tabs-grow__more-item").Count);
            Assert.Equal(2, cut.FindAll(".el-pro-tabs-grow__more-btn").Count);
            Assert.Contains("已打开 3 个标签", cut.Find(".el-pro-tabs-grow__more-head").TextContent);
            Assert.Contains("is-active", cut.FindAll(".el-pro-tabs-grow__more-item")[2].ClassList);

            // 再点一次收起
            cut.Find(".el-pro-tabs-grow__more").Click();
            Assert.Empty(cut.FindAll(".el-pro-tabs-grow__more-panel"));
        }

        [Fact]
        public void ShowMoreFalseHidesButton()
        {
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a"))
                .Add(x => x.ShowMore, false)
                .Add(x => x.MoreText, "更多"));

            Assert.Empty(cut.FindAll(".el-pro-tabs-grow__more"));
        }

        [Fact]
        public void MoreTextIsConfigurable()
        {
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a"))
                .Add(x => x.MoreText, "全部标签"));

            Assert.Equal("全部标签", cut.Find(".el-pro-tabs-grow__more-text").TextContent.Trim());
        }

        [Fact]
        public async System.Threading.Tasks.Task MoreActionsRaiseCloseOthersAndCloseAll()
        {
            ElProTabsGrowItem othersCurrent = null;
            var closeAllCount = 0;

            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a", "/b"))
                .Add(x => x.ActivePath, "/b")
                .Add(x => x.OnCloseOthers, (ElProTabsGrowItem i) => othersCurrent = i)
                .Add(x => x.OnCloseAll, () => closeAllCount++));

            cut.Find(".el-pro-tabs-grow__more").Click();
            await cut.FindAll(".el-pro-tabs-grow__more-btn")[0].ClickAsync(new MouseEventArgs());

            Assert.Equal("/b", othersCurrent?.Path);
            // 动作执行后菜单自动收起
            Assert.Empty(cut.FindAll(".el-pro-tabs-grow__more-panel"));

            cut.Find(".el-pro-tabs-grow__more").Click();
            await cut.FindAll(".el-pro-tabs-grow__more-btn")[1].ClickAsync(new MouseEventArgs());

            Assert.Equal(1, closeAllCount);
            Assert.Empty(cut.FindAll(".el-pro-tabs-grow__more-panel"));
        }

        [Fact]
        public async System.Threading.Tasks.Task SelectingFromMorePanelClosesIt()
        {
            ElProTabsGrowItem selected = null;

            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a", "/b"))
                .Add(x => x.OnSelect, (ElProTabsGrowItem i) => selected = i));

            cut.Find(".el-pro-tabs-grow__more").Click();
            await cut.FindAll(".el-pro-tabs-grow__more-item")[1].ClickAsync(new MouseEventArgs());

            Assert.Equal("/b", selected?.Path);
            Assert.Empty(cut.FindAll(".el-pro-tabs-grow__more-panel"));
        }

        [Fact]
        public async System.Threading.Tasks.Task OutsideClickClosesMorePanel()
        {
            var cut = Render<ElProTabsGrow>(p => p.Add(x => x.Items, Tabs("/a", "/b")));

            cut.Find(".el-pro-tabs-grow__more").Click();
            Assert.True(cut.Instance.IsMoreOpened);

            // JS 通道上报「点了组件外面」
            await cut.Instance.OnProTabsOutsideClick();

            Assert.False(cut.Instance.IsMoreOpened);
            Assert.Empty(cut.FindAll(".el-pro-tabs-grow__more-panel"));
        }

        #endregion

        #region 右键菜单

        private static MouseEventArgs RightClickAt(double x, double y) => new() { ClientX = x, ClientY = y, Button = 2 };

        private static List<string> CtxTexts(IRenderedComponent<ElProTabsGrow> cut) =>
            cut.FindAll(".el-pro-tabs-grow__ctx-item").Select(x => x.TextContent.Trim()).ToList();

        [Fact]
        public void ContextMenuOpensOnRightClickWithPositionalDisabledStates()
        {
            var items = Tabs("/a", "/b", "/c");
            items[0].Closable = false;   // 置钉首页：不可关
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, items)
                .Add(x => x.ActivePath, "/b"));

            // 右键中间页签：左侧有可关（无）、右侧有可关 → 关闭左侧禁用
            cut.FindAll(".el-pro-tabs-grow__item")[1].TriggerEvent("oncontextmenu", RightClickAt(120, 40));

            Assert.True(cut.Instance.IsContextMenuOpened);
            var panel = cut.Find(".el-pro-tabs-grow__ctx");
            Assert.NotNull(panel);
            Assert.Equal(new[] { "刷新", "关闭", "关闭左侧", "关闭右侧", "关闭其他", "关闭全部" }, CtxTexts(cut));

            var states = cut.FindAll(".el-pro-tabs-grow__ctx-item")
                .Select(x => x.ClassList.Contains("is-disabled")).ToList();
            Assert.Equal(new[] { false, false, true, false, false, false }, states);
        }

        [Fact]
        public void ContextMenuOnPinnedOnlyTabDisablesAllCloseActions()
        {
            var items = Tabs("/");
            items[0].Closable = false;
            var cut = Render<ElProTabsGrow>(p => p.Add(x => x.Items, items));

            cut.FindAll(".el-pro-tabs-grow__item")[0].TriggerEvent("oncontextmenu", RightClickAt(10, 10));

            var states = cut.FindAll(".el-pro-tabs-grow__ctx-item")
                .Select(x => x.ClassList.Contains("is-disabled")).ToList();
            // 刷新可用；关闭/关左/关右/关其他/关全部全禁用
            Assert.Equal(new[] { false, true, true, true, true, true }, states);
        }

        [Fact]
        public void ContextMenuRefreshRaisesOnRefreshTab()
        {
            ElProTabsGrowItem refreshed = null;
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a", "/b"))
                .Add(x => x.OnRefreshTab, (ElProTabsGrowItem i) => refreshed = i));

            cut.FindAll(".el-pro-tabs-grow__item")[1].TriggerEvent("oncontextmenu", RightClickAt(0, 0));
            cut.FindAll(".el-pro-tabs-grow__ctx-item")[0].Click();

            Assert.Equal("/b", refreshed?.Path);
            Assert.False(cut.Instance.IsContextMenuOpened);
        }

        [Fact]
        public async System.Threading.Tasks.Task ContextMenuCloseLeftRightRaiseEventsWithAnchor()
        {
            var items = Tabs("/a", "/b", "/c");
            ElProTabsGrowItem leftAnchor = null;
            ElProTabsGrowItem rightAnchor = null;

            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, items)
                .Add(x => x.OnCloseLeft, (ElProTabsGrowItem i) => leftAnchor = i)
                .Add(x => x.OnCloseRight, (ElProTabsGrowItem i) => rightAnchor = i));

            // 右键 /b → 关闭左侧（/a 可关）
            cut.FindAll(".el-pro-tabs-grow__item")[1].TriggerEvent("oncontextmenu", RightClickAt(0, 0));
            await cut.FindAll(".el-pro-tabs-grow__ctx-item")[2].ClickAsync(new MouseEventArgs());
            Assert.Equal("/b", leftAnchor?.Path);

            // 右键 /b → 关闭右侧（/c 可关）
            cut.FindAll(".el-pro-tabs-grow__item")[1].TriggerEvent("oncontextmenu", RightClickAt(0, 0));
            await cut.FindAll(".el-pro-tabs-grow__ctx-item")[3].ClickAsync(new MouseEventArgs());
            Assert.Equal("/b", rightAnchor?.Path);
        }

        [Fact]
        public async System.Threading.Tasks.Task ContextMenuCloseOthersAnchorsRightClickedTabNotActive()
        {
            var items = Tabs("/a", "/b", "/c");
            ElProTabsGrowItem anchor = null;

            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, items)
                .Add(x => x.ActivePath, "/a")
                .Add(x => x.OnCloseOthers, (ElProTabsGrowItem i) => anchor = i));

            // 右键 /c（激活的是 /a）→ 关闭其他应以 /c 为锚
            cut.FindAll(".el-pro-tabs-grow__item")[2].TriggerEvent("oncontextmenu", RightClickAt(0, 0));
            await cut.FindAll(".el-pro-tabs-grow__ctx-item")[4].ClickAsync(new MouseEventArgs());

            Assert.Equal("/c", anchor?.Path);
            Assert.False(cut.Instance.IsContextMenuOpened);
        }

        [Fact]
        public async System.Threading.Tasks.Task OutsideClickAndTabClickCloseContextMenu()
        {
            var cut = Render<ElProTabsGrow>(p => p.Add(x => x.Items, Tabs("/a", "/b")));

            cut.FindAll(".el-pro-tabs-grow__item")[0].TriggerEvent("oncontextmenu", RightClickAt(0, 0));
            Assert.True(cut.Instance.IsContextMenuOpened);

            await cut.Instance.OnProTabsOutsideClick();
            Assert.False(cut.Instance.IsContextMenuOpened);
            Assert.Empty(cut.FindAll(".el-pro-tabs-grow__ctx"));

            cut.FindAll(".el-pro-tabs-grow__item")[0].TriggerEvent("oncontextmenu", RightClickAt(0, 0));
            // 右键打开菜单会重渲染，点击前重新取节点
            cut.FindAll(".el-pro-tabs-grow__item")[1].Click();
            Assert.False(cut.Instance.IsContextMenuOpened);
        }

        [Fact]
        public void ShowContextMenuFalseIgnoresRightClick()
        {
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a"))
                .Add(x => x.ShowContextMenu, false));

            cut.FindAll(".el-pro-tabs-grow__item")[0].TriggerEvent("oncontextmenu", RightClickAt(0, 0));

            Assert.False(cut.Instance.IsContextMenuOpened);
            Assert.Empty(cut.FindAll(".el-pro-tabs-grow__ctx"));
        }

        #endregion

        #region 拖拽排序

        [Fact]
        public void SortableMarksClosableItemsDraggableAndPinnedNot()
        {
            var items = Tabs("/a", "/b");
            items[0].Closable = false;   // 置钉项是顺序锚点

            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, items)
                .Add(x => x.Sortable, true));

            var nodes = cut.FindAll(".el-pro-tabs-grow__item");
            // Blazor 对 false 布尔属性直接省略；true 必须是字面 "true"（draggable="" 是 auto，div 拖不动）
            Assert.Null(nodes[0].GetAttribute("draggable"));
            Assert.Equal("true", nodes[1].GetAttribute("draggable"));

            // 默认关闭 → 无 draggable 属性
            var cut2 = Render<ElProTabsGrow>(p => p.Add(x => x.Items, Tabs("/a")));
            Assert.Null(cut2.Find(".el-pro-tabs-grow__item").GetAttribute("draggable"));
        }

        [Fact]
        public async System.Threading.Tasks.Task DropReordersItemsAndRaisesOnReorder()
        {
            string[] order = null;
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a", "/b", "/c"))
                .Add(x => x.Sortable, true)
                .Add(x => x.OnReorder, (string[] o) => order = o));

            // 每次触发事件都会重渲染（is-dragging/is-drop-target），必须重新 FindAll 取最新节点
            var nodes = cut.FindAll(".el-pro-tabs-grow__item");
            await nodes[0].TriggerEventAsync("ondragstart", new DragEventArgs());

            nodes = cut.FindAll(".el-pro-tabs-grow__item");
            await nodes[2].TriggerEventAsync("ondragover", new DragEventArgs());

            nodes = cut.FindAll(".el-pro-tabs-grow__item");
            Assert.Contains("is-drop-target", nodes[2].ClassList);
            await nodes[2].TriggerEventAsync("ondrop", new DragEventArgs());

            // /a 被移到 /c 的位置
            Assert.NotNull(order);
            Assert.Equal(new[] { "/b", "/c", "/a" }, order);

            // 拖拽状态清理
            nodes = cut.FindAll(".el-pro-tabs-grow__item");
            Assert.DoesNotContain("is-dragging", nodes[2].ClassList);
            Assert.DoesNotContain("is-drop-target", nodes[2].ClassList);
        }

        [Fact]
        public async System.Threading.Tasks.Task DragEndWithoutDropClearsState()
        {
            var reorderCount = 0;
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a", "/b"))
                .Add(x => x.Sortable, true)
                .Add(x => x.OnReorder, (string[] o) => reorderCount++));

            var nodes = cut.FindAll(".el-pro-tabs-grow__item");
            await nodes[0].TriggerEventAsync("ondragstart", new DragEventArgs());

            nodes = cut.FindAll(".el-pro-tabs-grow__item");
            await nodes[1].TriggerEventAsync("ondragover", new DragEventArgs());

            nodes = cut.FindAll(".el-pro-tabs-grow__item");
            await nodes[0].TriggerEventAsync("ondragend", new DragEventArgs());

            Assert.Equal(0, reorderCount);

            nodes = cut.FindAll(".el-pro-tabs-grow__item");
            Assert.DoesNotContain("is-drop-target", nodes[1].ClassList);

            // 之后 drop 不应再触发（dragPath 已清）
            await nodes[1].TriggerEventAsync("ondrop", new DragEventArgs());
            Assert.Equal(0, reorderCount);
        }

        #endregion

        #region 滚动状态（JS 回填）

        [Fact]
        public async System.Threading.Tasks.Task ScrollStateControlsArrowVisibilityAndDisabledState()
        {
            var cut = Render<ElProTabsGrow>(p => p.Add(x => x.Items, Tabs("/a", "/b")));

            // 未测量（bUnit 环境无 JS）时默认不显示箭头
            Assert.Empty(cut.FindAll(".el-pro-tabs-grow__arrow"));

            await cut.Instance.OnProTabsScrollState(true, true, false);

            var arrows = cut.FindAll(".el-pro-tabs-grow__arrow");
            Assert.Equal(2, arrows.Count);
            Assert.NotNull(arrows[0].GetAttribute("disabled"));   // 已在最左 → 左箭头禁用
            Assert.Null(arrows[1].GetAttribute("disabled"));      // 还能往右 → 右箭头可用
            Assert.True(cut.Instance.HasScroll);

            await cut.Instance.OnProTabsScrollState(true, false, true);
            arrows = cut.FindAll(".el-pro-tabs-grow__arrow");
            Assert.Null(arrows[0].GetAttribute("disabled"));
            Assert.NotNull(arrows[1].GetAttribute("disabled"));

            await cut.Instance.OnProTabsScrollState(false, true, true);
            Assert.Empty(cut.FindAll(".el-pro-tabs-grow__arrow"));
        }

        [Fact]
        public async System.Threading.Tasks.Task ShowScrollButtonsFalseKeepsArrowsHidden()
        {
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a", "/b"))
                .Add(x => x.ShowScrollButtons, false));

            await cut.Instance.OnProTabsScrollState(true, true, false);

            Assert.Empty(cut.FindAll(".el-pro-tabs-grow__arrow"));
        }

        #endregion

        #region 键盘

        [Fact]
        public void KeyboardEnterSelectsAndArrowMovesToNeighbour()
        {
            var changed = new List<string>();

            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a", "/b", "/c"))
                .Add(x => x.ActivePath, "/a")
                .Add(x => x.ActivePathChanged, (string v) => changed.Add(v)));

            cut.FindAll(".el-pro-tabs-grow__item")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
            Assert.Equal(new[] { "/b" }, changed);

            cut.FindAll(".el-pro-tabs-grow__item")[1].KeyDown(new KeyboardEventArgs { Key = "End" });
            Assert.Equal(new[] { "/b", "/c" }, changed);

            cut.FindAll(".el-pro-tabs-grow__item")[2].KeyDown(new KeyboardEventArgs { Key = "Home" });
            Assert.Equal(new[] { "/b", "/c", "/a" }, changed);

            // 环绕：从第 1 个往左 → 最后一个
            cut.FindAll(".el-pro-tabs-grow__item")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
            Assert.Equal(new[] { "/b", "/c", "/a", "/c" }, changed);
        }

        [Fact]
        public void KeyboardDeleteClosesClosableTab()
        {
            ElProTabsGrowItem closed = null;

            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a", "/b"))
                .Add(x => x.OnClose, (ElProTabsGrowItem i) => closed = i));

            cut.FindAll(".el-pro-tabs-grow__item")[0].KeyDown(new KeyboardEventArgs { Key = "Delete" });

            Assert.Equal("/a", closed?.Path);
        }

        [Fact]
        public void KeyboardDeleteOnNonClosableTabDoesNothing()
        {
            var items = Tabs("/a");
            items[0].Closable = false;

            var closeCount = 0;
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, items)
                .Add(x => x.OnClose, (ElProTabsGrowItem i) => closeCount++));

            cut.FindAll(".el-pro-tabs-grow__item")[0].KeyDown(new KeyboardEventArgs { Key = "Delete" });

            Assert.Equal(0, closeCount);
        }

        #endregion

        #region JS 通道 / 生命周期

        [Fact]
        public void AttachesAndDetachesScrollChannel()
        {
            var cut = Render<ElProTabsGrow>(p => p.Add(x => x.Items, Tabs("/a", "/b")));

            var attach = JSInterop.Invocations.Where(x => x.Identifier == "elementProTabsAttach").ToList();
            Assert.Single(attach);
            Assert.Equal(3, attach[0].Arguments.Count);

            // 数量变化 → 重新测量
            cut.Render(p => p.Add(x => x.Items, Tabs("/a", "/b", "/c")));
            Assert.Equal(2, JSInterop.Invocations.Count(x => x.Identifier == "elementProTabsAttach"));

            cut.Instance.Dispose();

            Assert.Contains(JSInterop.Invocations, x => x.Identifier == "elementProTabsDetach");
        }

        [Fact]
        public void OpeningMorePanelUsesOutsideClickChannel()
        {
            var cut = Render<ElProTabsGrow>(p => p.Add(x => x.Items, Tabs("/a")));

            cut.Find(".el-pro-tabs-grow__more").Click();

            Assert.Contains(JSInterop.Invocations, x => x.Identifier == "elementProTabsMoreOpen");

            cut.Find(".el-pro-tabs-grow__more").Click();

            Assert.Contains(JSInterop.Invocations, x => x.Identifier == "elementProTabsMoreClose");
        }

        [Fact]
        public void ScrollHelpersInvokeJsChannel()
        {
            var cut = Render<ElProTabsGrow>(p => p
                .Add(x => x.Items, Tabs("/a", "/b"))
                .Add(x => x.ActivePath, "/b"));

            cut.InvokeAsync(() => cut.Instance.ScrollToActiveAsync());
            cut.InvokeAsync(() => cut.Instance.CloseMoreAsync());

            Assert.Contains(JSInterop.Invocations, x => x.Identifier == "elementProTabsScrollToTab");
        }

        #endregion

        #region 守卫：浏览器兼容 / 作用域

        [Fact]
        public void CssSection_AvoidsModernOnlySyntax()
        {
            var css = CssSection();

            foreach (var forbidden in new[]
            {
                ":has(", ":is(", ":where(", "clamp(", "inset-block", "inset-inline",
                ":focus-visible", "accent-color", "aspect-ratio", "scrollbar-gutter", "text-decoration-thickness"
            })
            {
                Assert.DoesNotContain(forbidden, css, StringComparison.OrdinalIgnoreCase);
            }

            Assert.DoesNotMatch(new Regex(@"(?<![-\w])gap\s*:"), css);
            Assert.DoesNotMatch(new Regex(@"(?<![-\w])(margin|padding|border)-(inline|block)\s*:"), css);
            Assert.DoesNotMatch(new Regex(@":not\([^)]*,"), css);
        }

        [Fact]
        public void CssSection_IsScopedAndProvidesExpectedClasses()
        {
            var css = CssSection();
            var offenders = new List<string>();

            foreach (var chunk in css.Split('}'))
            {
                var brace = chunk.IndexOf('{');

                if (brace < 0)
                {
                    continue;
                }

                var selector = chunk.Substring(0, brace).Trim();

                if (selector.Length == 0)
                {
                    continue;
                }

                foreach (var part in selector.Split(','))
                {
                    var single = part.Trim();

                    if (single.Length > 0 && !single.StartsWith(".el-pro-tabs-grow", StringComparison.Ordinal))
                    {
                        offenders.Add(single);
                    }
                }
            }

            Assert.Empty(offenders);

            foreach (var cls in new[]
            {
                ".el-pro-tabs-grow {", ".el-pro-tabs-grow__scroll", ".el-pro-tabs-grow__list",
                ".el-pro-tabs-grow__item", ".el-pro-tabs-grow__dot", ".el-pro-tabs-grow__label",
                ".el-pro-tabs-grow__close", ".el-pro-tabs-grow__refresh", ".el-pro-tabs-grow__arrow",
                ".el-pro-tabs-grow__more",
                ".el-pro-tabs-grow__more-panel", ".el-pro-tabs-grow__more-item", ".el-pro-tabs-grow__more-btn"
            })
            {
                Assert.Contains(cls, css);
            }
        }

        /// <summary>关闭按钮「悬停才显示」是纯 CSS 行为，必须真的用 visibility 隐藏（否则会留下能点的幽灵按钮）。</summary>
        [Fact]
        public void CssSection_HidesCloseButtonUntilHover()
        {
            var css = CssSection();

            Assert.Contains("visibility: hidden", css);
            Assert.Contains(".el-pro-tabs-grow__item:hover .el-pro-tabs-grow__close", css);
            Assert.Contains(".el-pro-tabs-grow__item:focus-within .el-pro-tabs-grow__close", css);
            Assert.Contains(".el-pro-tabs-grow.is-close-always .el-pro-tabs-grow__close", css);
        }

        /// <summary>
        /// 刷新图标与关闭按钮相反：宿主把「刷新当前页」的入口从页面里搬到页签上，
        /// 悬停才显形就等于没有入口，所以它的规则里不许出现 visibility 隐藏 / opacity: 0。
        /// </summary>
        [Fact]
        public void CssSection_KeepsRefreshIconVisibleAtRest()
        {
            var css = CssSection();
            var bodies = RuleBodies(css, ".el-pro-tabs-grow__refresh");

            Assert.NotEmpty(bodies);

            foreach (var body in bodies)
            {
                Assert.DoesNotContain("visibility", body, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotMatch(new Regex(@"opacity\s*:\s*0(?![.\d])"), body);
            }

            // 尺寸盒要齐（和关闭按钮同规格，排布才不乱）
            Assert.Contains("width: 16px", bodies[0]);
            Assert.Contains("margin-left: 5px", bodies[0]);

            // 悬停/激活只换颜色，不会把它再藏回去
            Assert.NotEmpty(RuleBodies(css, ".el-pro-tabs-grow__refresh:hover"));
            Assert.NotEmpty(RuleBodies(css, ".el-pro-tabs-grow__item.is-active .el-pro-tabs-grow__refresh"));

            // SVG 本体不吃鼠标事件：点击的落点始终是 __refresh（stopPropagation 才挂在正确的节点上）
            var iconBodies = RuleBodies(css, ".el-pro-tabs-grow__refresh-icon");
            Assert.Single(iconBodies);
            Assert.Contains("pointer-events: none", iconBodies[0]);
        }

        [Fact]
        public void JsSection_ExposesScrollAndMoreChannels()
        {
            var js = JsSection();

            foreach (var fn in new[]
            {
                "window.elementProTabsAttach", "window.elementProTabsDetach", "window.elementProTabsReport",
                "window.elementProTabsScrollBy", "window.elementProTabsScrollToTab",
                "window.elementProTabsMoreOpen", "window.elementProTabsMoreClose"
            })
            {
                Assert.Contains(fn, js);
            }

            Assert.Contains("'OnProTabsScrollState'", js);
            Assert.Contains("'OnProTabsOutsideClick'", js);
            Assert.Contains("ResizeObserver", js);
            Assert.Contains("if (!root", js);
        }

        #endregion
    }
}
