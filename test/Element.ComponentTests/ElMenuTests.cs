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
    /// <summary>
    /// ElMenu / ElSubMenu / ElMenuItem / ElMenuItemGroup 四件套。
    /// 覆盖：EP 的 DOM 与类名（el-menu / el-sub-menu / el-menu-item / el-menu-item-group）、
    /// 激活态（default-active / updateActiveIndex）、展开态（default-openeds / unique-opened / open / close）、
    /// 事件（select / open / close / menu-item click）、逐级缩进 --el-menu-level，以及零第三方依赖守卫。
    /// 弹层（horizontal / collapse 的 hover 打开）依赖真实浏览器布局，由 demo/verify-menu.cjs 覆盖。
    /// </summary>
    public class ElMenuTests : BunitContext
    {
        public ElMenuTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        #region 片段构造

        private static RenderFragment Frag(params RenderFragment[] items)
            => builder =>
            {
                for (var i = 0; i < items.Length; i++)
                {
                    items[i](builder);
                }
            };

        private static RenderFragment Item(string index, string text = null, bool disabled = false, string title = null)
            => builder =>
            {
                builder.OpenComponent<ElMenuItem>(0);
                builder.AddAttribute(1, "Index", index);
                if (disabled) builder.AddAttribute(2, "Disabled", true);
                if (text != null) builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, text)));
                if (title != null) builder.AddAttribute(4, "Title", title);
                builder.CloseComponent();
            };

        private static RenderFragment Sub(string index, string label, RenderFragment children)
            => builder =>
            {
                builder.OpenComponent<ElSubMenu>(0);
                builder.AddAttribute(1, "Index", index);
                builder.AddAttribute(2, "Label", label);
                builder.AddAttribute(3, "ChildContent", children);
                builder.CloseComponent();
            };

        private static RenderFragment Group(string title, RenderFragment children)
            => builder =>
            {
                builder.OpenComponent<ElMenuItemGroup>(0);
                builder.AddAttribute(1, "Title", title);
                builder.AddAttribute(2, "ChildContent", children);
                builder.CloseComponent();
            };

        /// <summary>两级菜单：子菜单「g1」包含「1-1」「1-2」，另有顶层项「2」。</summary>
        private static RenderFragment NestedMenu()
            => Frag(
                Sub("g1", "分组一", Frag(Item("1-1", "子项一"), Item("1-2", "子项二"))),
                Item("2", "顶层项"));

        #endregion

        #region DOM / 类名

        [Fact]
        public void Menu_RendersUlWithRoleAndModeClass()
        {
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent, Item("1", "导航一")));

            var ul = cut.Find("ul.el-menu");

            Assert.Equal("menubar", ul.GetAttribute("role"));
            Assert.Contains("el-menu--vertical", ul.GetAttribute("class"));
        }

        [Fact]
        public void HorizontalMode_RendersHorizontalClass()
        {
            var cut = Render<ElMenu>(p => p
                .Add(x => x.Mode, MenuMode.Horizontal)
                .Add(x => x.ChildContent, Item("1", "导航一")));

            Assert.Contains("el-menu--horizontal", cut.Find("ul.el-menu").GetAttribute("class"));
        }

        [Fact]
        public void Collapse_RendersCollapseClassAndHidesTitle()
        {
            var cut = Render<ElMenu>(p => p
                .Add(x => x.Collapse, true)
                .Add(x => x.ChildContent, Item("1", "导航一", title: "导航一")));

            Assert.Contains("el-menu--collapse", cut.Find("ul.el-menu").GetAttribute("class"));
            // 折叠时标题文本仍在 DOM 里（EP 由 CSS 隐藏），并作为原生 tooltip
            Assert.Equal("导航一", cut.Find("li.el-menu-item").GetAttribute("title") ?? string.Empty);
        }

        [Fact]
        public void MenuItem_RendersLiWithRoleMenuitem()
        {
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent, Item("1", "导航一")));

            var li = cut.Find("li.el-menu-item");

            Assert.Equal("menuitem", li.GetAttribute("role"));
            Assert.Contains("导航一", li.TextContent);
        }

        [Fact]
        public void DefaultActive_MarksItemActive()
        {
            var cut = Render<ElMenu>(p => p
                .Add(x => x.DefaultActive, "2")
                .Add(x => x.ChildContent, Frag(Item("1", "一"), Item("2", "二"))));

            var active = cut.FindAll("li.el-menu-item.is-active");

            Assert.Single(active);
            Assert.Contains("二", active[0].TextContent);
        }

        [Fact]
        public void SubMenu_RendersEpDomWithArrowDown()
        {
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent, NestedMenu()));

            var li = cut.Find("li.el-sub-menu");
            var title = cut.Find("div.el-sub-menu__title");
            var arrow = cut.Find("i.el-sub-menu__icon-arrow");

            Assert.Equal("menuitem", li.GetAttribute("role"));
            Assert.Equal("true", li.GetAttribute("aria-haspopup"));
            Assert.Contains("分组一", title.TextContent);
            // 展开态（vertical 非折叠）用 arrow-down
            Assert.Contains("el-icon-arrow-down", arrow.GetAttribute("class"));
            // 内层 ul 用 el-menu--inline
            Assert.NotNull(cut.Find("ul.el-menu--inline"));
        }

        [Fact]
        public void CollapsedSubMenu_UsesArrowRight()
        {
            var cut = Render<ElMenu>(p => p
                .Add(x => x.Collapse, true)
                .Add(x => x.ChildContent, NestedMenu()));

            Assert.Contains("el-icon-arrow-right", cut.Find("i.el-sub-menu__icon-arrow").GetAttribute("class"));
            // 折叠（弹层）态不渲染内联 ul
            Assert.Empty(cut.FindAll("ul.el-menu--inline"));
        }

        [Fact]
        public void CustomExpandIcons_AreUsed()
        {
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent, builder =>
            {
                builder.OpenComponent<ElSubMenu>(0);
                builder.AddAttribute(1, "Index", "g1");
                builder.AddAttribute(2, "Label", "分组一");
                builder.AddAttribute(3, "ExpandCloseIcon", "el-icon-circle-plus");
                builder.AddAttribute(4, "ExpandOpenIcon", "el-icon-remove");
                builder.AddAttribute(5, "ChildContent", (RenderFragment)(b => { }));
                builder.CloseComponent();
            }));

            Assert.Contains("el-icon-circle-plus", cut.Find("i.el-sub-menu__icon-arrow").GetAttribute("class"));
            // 自定义图标不叠加旋转
            Assert.Equal("transform:none", cut.Find("i.el-sub-menu__icon-arrow").GetAttribute("style"));
        }

        [Fact]
        public void MenuItemGroup_RendersTitleAndList()
        {
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent, Group("默认分组", Item("1", "一"))));

            var group = cut.Find("li.el-menu-item-group");
            var title = cut.Find("div.el-menu-item-group__title");

            Assert.Contains("默认分组", title.TextContent);
            Assert.NotNull(group.QuerySelector("ul li.el-menu-item"));
        }

        [Fact]
        public void LevelIndent_UsesMenuLevelCssVar()
        {
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent, NestedMenu()));

            var inline = cut.Find("ul.el-menu--inline");
            var item = cut.Find("li.el-sub-menu .el-menu-item");

            Assert.Contains("--el-menu-level:1", inline.GetAttribute("style"));
            Assert.Contains("--el-menu-level:1", item.GetAttribute("style"));
        }

        #endregion

        #region 激活 / 选中

        [Fact]
        public void ClickItem_SetsActiveAndRaisesSelect()
        {
            var selected = new List<MenuSelectEventArgs>();
            var cut = Render<ElMenu>(p => p
                .Add(x => x.OnSelect, (MenuSelectEventArgs e) => selected.Add(e))
                .Add(x => x.ChildContent, Frag(Item("1", "一"), Item("2", "二"))));

            cut.FindAll("li.el-menu-item")[1].Click();

            cut.WaitForAssertion(() =>
            {
                var active = cut.FindAll("li.el-menu-item.is-active");
                Assert.Single(active);
                Assert.Contains("二", active[0].TextContent);
            }, timeout: TestWait.Timeout);

            Assert.Single(selected);
            Assert.Equal("2", selected[0].Index);
            Assert.Equal(new[] { "2" }, selected[0].IndexPath);
        }

        [Fact]
        public void ClickItem_IndexPathContainsAncestors()
        {
            var selected = new List<MenuSelectEventArgs>();
            var cut = Render<ElMenu>(p => p
                .Add(x => x.OnSelect, (MenuSelectEventArgs e) => selected.Add(e))
                .Add(x => x.ChildContent, NestedMenu()));

            cut.Find("li.el-sub-menu .el-menu-item").Click();

            Assert.Single(selected);
            Assert.Equal("1-1", selected[0].Index);
            Assert.Equal(new[] { "g1", "1-1" }, selected[0].IndexPath);
        }

        [Fact]
        public void MenuItemClick_RaisesItemLevelClickEvent()
        {
            var clicked = new List<MenuSelectEventArgs>();
            var receiver = new object();
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent, builder =>
            {
                builder.OpenComponent<ElMenuItem>(0);
                builder.AddAttribute(1, "Index", "1");
                builder.AddAttribute(2, "OnClick",
                    EventCallback.Factory.Create<MenuSelectEventArgs>(receiver, e => clicked.Add(e)));
                builder.CloseComponent();
            }));

            cut.Find("li.el-menu-item").Click();

            Assert.Single(clicked);
            Assert.Equal("1", clicked[0].Index);
        }

        [Fact]
        public void DisabledItem_ClickDoesNothing()
        {
            var selected = new List<MenuSelectEventArgs>();
            var cut = Render<ElMenu>(p => p
                .Add(x => x.OnSelect, (MenuSelectEventArgs e) => selected.Add(e))
                .Add(x => x.ChildContent, Item("1", "一", disabled: true)));

            cut.Find("li.el-menu-item").Click();

            Assert.Contains("is-disabled", cut.Find("li.el-menu-item").GetAttribute("class"));
            Assert.Empty(selected);
        }

        [Fact]
        public void UpdateActiveIndex_ActivatesByIndex()
        {
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent, Frag(Item("1", "一"), Item("2", "二"))));

            cut.InvokeAsync(() => cut.Instance.UpdateActiveIndex("2"));

            cut.WaitForAssertion(() =>
                Assert.Contains("二", cut.Find("li.el-menu-item.is-active").TextContent), timeout: TestWait.Timeout);
        }

        [Fact]
        public void SubMenu_IsActiveWhenDescendantActive()
        {
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent, NestedMenu()));

            Assert.DoesNotContain("is-active", cut.Find("li.el-sub-menu").GetAttribute("class"));

            cut.Find("li.el-sub-menu .el-menu-item").Click();

            cut.WaitForAssertion(() =>
                Assert.Contains("is-active", cut.Find("li.el-sub-menu").GetAttribute("class")), timeout: TestWait.Timeout);
        }

        #endregion

        #region 展开 / 收起

        [Fact]
        public void ClickSubMenuTitle_TogglesAndRaisesOpenClose()
        {
            var opened = new List<MenuOpenEventArgs>();
            var closed = new List<MenuOpenEventArgs>();
            var cut = Render<ElMenu>(p => p
                .Add(x => x.OnOpen, (MenuOpenEventArgs e) => opened.Add(e))
                .Add(x => x.OnClose, (MenuOpenEventArgs e) => closed.Add(e))
                .Add(x => x.ChildContent, NestedMenu()));

            cut.Find("div.el-sub-menu__title").Click();

            cut.WaitForAssertion(() =>
                Assert.Contains("is-opened", cut.Find("li.el-sub-menu").GetAttribute("class")), timeout: TestWait.Timeout);

            Assert.Single(opened);
            Assert.Equal("g1", opened[0].Index);
            Assert.Equal(new[] { "g1" }, opened[0].IndexPath);

            cut.Find("div.el-sub-menu__title").Click();

            cut.WaitForAssertion(() =>
                Assert.DoesNotContain("is-opened", cut.Find("li.el-sub-menu").GetAttribute("class")), timeout: TestWait.Timeout);

            Assert.Single(closed);
            Assert.Equal("g1", closed[0].Index);
        }

        [Fact]
        public void DefaultOpeneds_OpenOnFirstRender()
        {
            var cut = Render<ElMenu>(p => p
                .Add(x => x.DefaultOpeneds, new[] { "g1" })
                .Add(x => x.ChildContent, NestedMenu()));

            cut.WaitForAssertion(() =>
                Assert.Contains("is-opened", cut.Find("li.el-sub-menu").GetAttribute("class")), timeout: TestWait.Timeout);
        }

        [Fact]
        public void UniqueOpened_ClosesOtherSubMenus()
        {
            var cut = Render<ElMenu>(p => p
                .Add(x => x.UniqueOpened, true)
                .Add(x => x.ChildContent, Frag(
                    Sub("g1", "一", Item("1-1", "a")),
                    Sub("g2", "二", Item("2-1", "b")))));

            cut.FindAll("div.el-sub-menu__title")[0].Click();
            cut.WaitForAssertion(() =>
                Assert.Contains("is-opened", cut.FindAll("li.el-sub-menu")[0].GetAttribute("class")), timeout: TestWait.Timeout);

            cut.FindAll("div.el-sub-menu__title")[1].Click();
            cut.WaitForAssertion(() =>
            {
                Assert.DoesNotContain("is-opened", cut.FindAll("li.el-sub-menu")[0].GetAttribute("class"));
                Assert.Contains("is-opened", cut.FindAll("li.el-sub-menu")[1].GetAttribute("class"));
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public void OpenAndClose_ApiTogglesSubMenu()
        {
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent, NestedMenu()));

            cut.InvokeAsync(() => cut.Instance.OpenAsync("g1"));
            cut.WaitForAssertion(() =>
                Assert.Contains("is-opened", cut.Find("li.el-sub-menu").GetAttribute("class")), timeout: TestWait.Timeout);

            cut.InvokeAsync(() => cut.Instance.CloseAsync("g1"));
            cut.WaitForAssertion(() =>
                Assert.DoesNotContain("is-opened", cut.Find("li.el-sub-menu").GetAttribute("class")), timeout: TestWait.Timeout);
        }

        [Fact]
        public void Collapse_ClosesAllOpenedSubMenus()
        {
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent, NestedMenu()));

            cut.Find("div.el-sub-menu__title").Click();
            cut.WaitForAssertion(() =>
                Assert.Contains("is-opened", cut.Find("li.el-sub-menu").GetAttribute("class")), timeout: TestWait.Timeout);

            cut.Render(p => p.Add(x => x.Collapse, true));

            cut.WaitForAssertion(() =>
                Assert.DoesNotContain("is-opened", cut.Find("li.el-sub-menu").GetAttribute("class")), timeout: TestWait.Timeout);
        }

        [Fact]
        public void HorizontalHoverTrigger_SubMenuClickDoesNotToggle()
        {
            var cut = Render<ElMenu>(p => p
                .Add(x => x.Mode, MenuMode.Horizontal)
                .Add(x => x.MenuTrigger, MenuTrigger.Hover)
                .Add(x => x.ChildContent, NestedMenu()));

            cut.Find("div.el-sub-menu__title").Click();

            Assert.DoesNotContain("is-opened", cut.Find("li.el-sub-menu").GetAttribute("class"));
        }

        [Fact]
        public void Timeouts_DefaultToEpValues()
        {
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent, Item("1", "一")));

            Assert.Equal(300, cut.Instance.ShowTimeout);
            Assert.Equal(300, cut.Instance.HideTimeout);
            Assert.Equal(6, cut.Instance.PopperOffset);
        }

        [Fact]
        public void VerticalNonPopup_MouseLeaveDoesNotCloseOpenSubMenu()
        {
            // 垂直非折叠模式下子菜单是「内联展开」；标题上的 mouseleave 绝不能收起它，
            // 否则鼠标从标题移向子项时会经过一次 mouseleave，把刚展开的内联子菜单用 display:none 挡住。
            // 旧实现用 @onmouseout（会在标题内部跨子元素冒泡触发）曾导致「展开即关、子项点不到」。
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent, NestedMenu()));

            cut.Find("div.el-sub-menu__title").Click();
            cut.WaitForAssertion(() =>
                Assert.Contains("is-opened", cut.Find("li.el-sub-menu").GetAttribute("class")), timeout: TestWait.Timeout);

            cut.Find("div.el-sub-menu__title").TriggerEvent("onmouseleave", new MouseEventArgs());

            // 鼠标移出标题（非弹层模式）不应收起内联子菜单
            cut.WaitForAssertion(() =>
                Assert.Contains("is-opened", cut.Find("li.el-sub-menu").GetAttribute("class")), timeout: TestWait.Timeout);
        }

        [Fact]
        public void Refresh_BackfillsIsOpenedFromRootOpenedMenus()
        {
            // 展开态唯一真源是根菜单的 OpenedMenus；若实例 IsOpened 因故漂移（历史双真源 bug），
            // Refresh 必须把根菜单的展开态补齐回实例，否则子菜单虽在 OpenedMenus 中却 display:none。
            var cut = Render<ElMenu>(p => p
                .Add(x => x.DefaultOpeneds, new[] { "g1" })
                .Add(x => x.ChildContent, NestedMenu()));

            cut.WaitForAssertion(() =>
                Assert.Contains("is-opened", cut.Find("li.el-sub-menu").GetAttribute("class")), timeout: TestWait.Timeout);

            // 模拟实例态漂移：根 OpenedMenus 仍含 "g1"，但实例 IsOpened 被置否
            var sub = cut.FindComponent<ElSubMenu>().Instance;
            sub.ApplyOpenedState(false);
            cut.Render(); // 普通重渲染不会自动调用 Refresh，漂移态会表现为关闭
            cut.WaitForAssertion(() =>
                Assert.DoesNotContain("is-opened", cut.Find("li.el-sub-menu").GetAttribute("class")), timeout: TestWait.Timeout);

            // Refresh 应把根菜单的展开态补齐回实例
            cut.Instance.RefreshItems();
            cut.WaitForAssertion(() =>
                Assert.Contains("is-opened", cut.Find("li.el-sub-menu").GetAttribute("class")), timeout: TestWait.Timeout);
        }

        [Fact]
        public async Task PopupSubMenu_OpensWhileAnotherPopupIsRegistered()
        {
            // 回归：ShowPopupCoreAsync 曾在添加弹层前执行
            //   while (PopupService.SubMenuOptionsCore.Any()) await Task.Delay(50);
            // —— 只要已有任一弹层在世（horizontal 下父弹层必然在世），后续/嵌套子菜单就永远打不开，
            //    表现为「点开父菜单后，子菜单怎么都打不开」。
            var cut = Render<ElMenu>(p => p
                .Add(x => x.Mode, MenuMode.Horizontal)
                .Add(x => x.ShowTimeout, 0)
                .Add(x => x.ChildContent, Frag(
                    Sub("h2", "Workspace", Frag(Item("h2-1", "one"))),
                    Sub("h4", "Orders", Frag(Item("h4-1", "one"))))));

            var popupService = Services.GetRequiredService<PopupService>();
            var subMenus = cut.FindComponents<ElSubMenu>();
            Assert.Equal(2, subMenus.Count);
            Assert.Empty(popupService.SubMenuOptionsCore);

            await cut.InvokeAsync(() => { _ = subMenus[0].Instance.ToggleByKeyboardAsync(); });
            Assert.True(await WaitForAsync(() => popupService.SubMenuOptionsCore.Count == 1), "第一个子菜单弹层未注册");

            await cut.InvokeAsync(() => { _ = subMenus[1].Instance.ToggleByKeyboardAsync(); });
            Assert.True(await WaitForAsync(() => popupService.SubMenuOptionsCore.Count == 2),
                "已有弹层在世时第二个子菜单弹层打不开（回归 bug）");
        }

        [Fact]
        public void SubMenuPlacement_IsBottomOnlyForHorizontalFirstLevel()
        {
            // EP sub-menu.ts currentPlacement：
            //   horizontal 的一级子菜单 → bottom-start；嵌套层级 / vertical+collapse → right-start。
            // 旧实现一律 bottom-start，嵌套子菜单的弹层会跑到触发项下方而不是右侧。
            var horizontal = new ElMenu { Mode = MenuMode.Horizontal };
            var firstLevel = new ElSubMenu { Index = "h2", TopMenu = horizontal };
            var nested = new ElSubMenu { Index = "h2-3", ParentMenu = firstLevel, TopMenu = horizontal };

            var collapsed = new ElMenu { Mode = MenuMode.Vertical, Collapse = true };
            var collapsedFirst = new ElSubMenu { Index = "v2", TopMenu = collapsed };

            Assert.Equal("bottom-start", ElPopup.ResolveSubMenuPlacement(new SubMenuOption { SubMenu = firstLevel }));
            Assert.Equal("right-start", ElPopup.ResolveSubMenuPlacement(new SubMenuOption { SubMenu = nested }));
            Assert.Equal("right-start", ElPopup.ResolveSubMenuPlacement(new SubMenuOption { SubMenu = collapsedFirst }));
        }

        [Fact]
        public void SubMenuOption_DescendantDetection_UsesIndexPathPrefix()
        {
            // 关闭父弹层时按 index 路径前缀级联关闭后代弹层（实例会随渲染重建，不能比引用）。
            var firstLevel = new ElSubMenu { Index = "h2" };
            var nested = new ElSubMenu { Index = "h2-3", ParentMenu = firstLevel };
            var grandChild = new ElSubMenu { Index = "h2-3-1", ParentMenu = nested };
            var sibling = new ElSubMenu { Index = "h4" };
            var lookalike = new ElSubMenu { Index = "h2-x" };

            var firstOption = new SubMenuOption { SubMenu = firstLevel };

            Assert.True(ElPopup.IsDescendantOption(new SubMenuOption { SubMenu = nested }, firstOption));
            Assert.True(ElPopup.IsDescendantOption(new SubMenuOption { SubMenu = grandChild }, firstOption));
            Assert.False(ElPopup.IsDescendantOption(firstOption, new SubMenuOption { SubMenu = nested }));
            Assert.False(ElPopup.IsDescendantOption(new SubMenuOption { SubMenu = sibling }, firstOption));
            Assert.False(ElPopup.IsDescendantOption(new SubMenuOption { SubMenu = lookalike }, firstOption));
        }

        /// <summary>轮询等待条件成立（弹层打开是异步续体，不能在渲染帧内同步断言）。</summary>
        private static async Task<bool> WaitForAsync(Func<bool> condition, int timeoutMs = 3000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                if (condition())
                {
                    return true;
                }
                await Task.Delay(20);
            }
            return condition();
        }

        #endregion

        #region 守卫

        [Fact]
        public void Guard_NoThirdPartyDependency()
        {
            foreach (var file in new[]
            {
                "ElMenu.razor", "ElMenu.razor.cs", "ElMenuContainer.cs",
                "ElSubMenu.razor", "ElSubMenu.razor.cs",
                "ElMenuItem.razor", "ElMenuItem.razor.cs",
                "ElMenuItemGroup.razor", "ElMenuItemGroup.razor.cs"
            })
            {
                var text = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", file));
                var stripped = Regex.Replace(text, @"/\*[\s\S]*?\*/", string.Empty);
                stripped = Regex.Replace(stripped, @"//[^\r\n]*", string.Empty);

                Assert.DoesNotContain("using Radzen", stripped);
                Assert.DoesNotContain("Radzen.Blazor", stripped);
                Assert.DoesNotContain("<Radzen", stripped);
            }
        }

        [Fact]
        public void Guard_MarkupHasNoScriptOrSelfWrittenJs()
        {
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent, NestedMenu()));

            Assert.DoesNotContain("<script", cut.Markup);
        }

        [Fact]
        public void Guard_MenuCssSectionUsesNoModernSyntax()
        {
            // theme.css 已折叠入 index.css，原 theme 段由 /* ==== THEME_CSS_START ==== */ / END 标记包裹。
            var css = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "css", "index.css"));
            const string startMarker = "/* ==== THEME_CSS_START ==== */";
            var themeStart = css.IndexOf(startMarker, StringComparison.Ordinal);
            Assert.True(themeStart >= 0, "index.css 中未找到折叠后的 theme 段标记(THEME_CSS_START)");

            // 锚定到 theme 块内的 .el-menu 段（与合并前 theme.css 守卫范围一致），
            // 避免把 theme 块里合法的 gap: 4px 等纳入断言。
            var s = css.IndexOf(".el-menu {", themeStart, StringComparison.Ordinal);
            var e = css.IndexOf("\n.el-table__body-wrapper {", s, StringComparison.Ordinal);
            Assert.True(s >= 0 && e > s, "theme 块内未找到 .el-menu 段");

            var section = Regex.Replace(css.Substring(s, e - s), @"/\*[\s\S]*?\*/", string.Empty);

            Assert.DoesNotContain(":has(", section);
            Assert.DoesNotContain(":is(", section);
            Assert.DoesNotContain(":where(", section);
            Assert.DoesNotContain("clamp(", section);
            Assert.DoesNotMatch(@"(?<![-\w])gap\s*:", section);
            Assert.DoesNotMatch(@"(?<![-\w])(margin-inline|padding-inline|border-inline|inset-block|inset-inline)\s*:", section);
        }

        private static string RepoRoot([CallerFilePath] string file = null)
            => new FileInfo(file).Directory.Parent.Parent.FullName;

        #endregion

        #region 配色 CSS 变量（EP use-menu-css-var / use-menu-color 口径）

        /// <summary>
        /// 深色 + 自定义配色的回归口径：EP 的 <c>use-menu-color.ts</c> 把悬浮底派生成
        /// <c>background-color</c> 加深 20%（<c>TinyColor.shade(20)</c> = 与纯黑按 20% 线性混合），
        /// <c>use-menu-css-var.ts</c> 把悬浮文字色跟到 <c>text-color</c>。
        /// 这里原先写死过 <c>HoverColor = "#ecf5ff"</c> 并无条件内联输出，压过 <c>.el-menu--dark</c>
        /// 的 #343842，于是深色菜单悬浮时是"近白底 + 白字"，标题文字直接看不见（demo /menu 第三节）。
        /// </summary>
        [Fact]
        public void DarkMenuWithCustomColorsDerivesHoverFromBackground()
        {
            var cut = Render<ElMenu>(p => p
                .Add(x => x.Theme, MenuTheme.Dark)
                .Add(x => x.BackgroundColor, "#545c64")
                .Add(x => x.TextColor, "#fff")
                .Add(x => x.ActiveTextColor, "#ffd04b")
                .AddChildContent(MenuWithOneItem()));

            var menu = cut.Find("ul.el-menu");
            var style = menu.GetAttribute("style");

            Assert.Contains("--el-menu-bg-color:#545c64", style);
            Assert.Contains("--el-menu-hover-bg-color:color-mix(in srgb, #545c64 80%, var(--el-color-black))", style);
            Assert.Contains("--el-menu-hover-text-color:#fff", style);
            Assert.Contains("el-menu--dark", menu.GetAttribute("class"));
            // 那个把深色主题悬浮打成白底的具体默认值不许再出现
            Assert.DoesNotContain("#ecf5ff", style);
        }

        [Fact]
        public void MenuWithoutColorsEmitsNoHoverVars()
        {
            var cut = Render<ElMenu>(p => p.AddChildContent(MenuWithOneItem()));

            var style = cut.Find("ul.el-menu").GetAttribute("style");

            // 一条配色都没给时，内联不许出现任何 hover 变量 —— 类级默认（.el-menu / .el-menu--dark）才拿得回去
            Assert.DoesNotContain("--el-menu-hover-bg-color", style);
            Assert.DoesNotContain("--el-menu-hover-text-color", style);
            // 正向对照：style 确实读到了内容，上面两条不是空转
            Assert.Contains("--el-menu-level:0", style);
        }

        [Fact]
        public void ExplicitHoverColorWinsOverDerivedOne()
        {
            var cut = Render<ElMenu>(p => p
                .Add(x => x.BackgroundColor, "#545c64")
                .Add(x => x.HoverColor, "#eef2fb")
                .AddChildContent(MenuWithOneItem()));

            var style = cut.Find("ul.el-menu").GetAttribute("style");

            Assert.Contains("--el-menu-hover-bg-color:#eef2fb", style);
            Assert.DoesNotContain("color-mix", style);
        }

        [Fact]
        public void TextColorOnlyFollowsHoverTextButNotHoverBg()
        {
            var cut = Render<ElMenu>(p => p
                .Add(x => x.TextColor, "#303133")
                .AddChildContent(MenuWithOneItem()));

            var style = cut.Find("ul.el-menu").GetAttribute("style");

            Assert.Contains("--el-menu-text-color:#303133", style);
            Assert.Contains("--el-menu-hover-text-color:#303133", style);
            Assert.DoesNotContain("--el-menu-hover-bg-color", style);
        }

        #endregion

        #region 官方属性补齐（Id / AriaLabel / Ellipsis / exposes）

        [Fact]
        public void IdAndAriaLabelAreRendered()
        {
            var cut = Render<ElMenu>(p => p
                .Add(x => x.Id, "menu-1")
                .Add(x => x.AriaLabel, "主菜单")
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElMenuItem>(0);
                    builder.AddAttribute(1, "Index", "1");
                    builder.CloseComponent();
                }));

            Assert.Equal("menu-1", cut.Find("ul.el-menu").GetAttribute("id"));
            Assert.Equal("主菜单", cut.Find("ul.el-menu").GetAttribute("aria-label"));
        }

        [Fact]
        public void EllipsisIsOnByDefaultAndEmitsTextClass()
        {
            // EP 2.14.0 menu.ts:124 的 ellipsis 默认值就是 true。本库曾默认 false，于是「文字超长」在
            // 任何没显式开这个属性的菜单里都直接溢出容器 —— 用户报的「ellipsis 没起作用」正是这一条。
            var cut = Render<ElMenu>(p => p.AddChildContent(MenuWithOneItem()));

            Assert.Single(cut.FindAll(".el-menu-item .el-menu-item__ellipsis-text"));
        }

        [Fact]
        public void EllipsisAddsTextClassOnItem()
        {
            var cut = Render<ElMenu>(p => p
                .Add(x => x.Ellipsis, true)
                .AddChildContent(MenuWithOneItem()));

            Assert.NotNull(cut.Find(".el-menu-item__ellipsis-text"));
        }

        [Fact]
        public void EllipsisFalseOmitsTextClass()
        {
            var cut = Render<ElMenu>(p => p
                .Add(x => x.Ellipsis, false)
                .AddChildContent(MenuWithOneItem()));

            Assert.Empty(cut.FindAll(".el-menu-item__ellipsis-text"));
            // 正向对照：span 本身一直在，上面那条断言不是"没有 span"造成的空转
            Assert.Single(cut.FindAll(".el-menu-item > span"));
        }

        [Fact]
        public void EllipsisTurnsOnWhenParameterChangesAfterInit()
        {
            // Options 在 OnInitialized 里一次性建好，之后每次 OnParametersSet 只走 SyncOptions；
            // 那里曾漏掉 Ellipsis（其余字段都同步），所以动态改这个参数会永久停在初始值上。
            var cut = Render<ElMenu>(p => p
                .Add(x => x.Ellipsis, false)
                .AddChildContent(MenuWithOneItem()));

            Assert.Empty(cut.FindAll(".el-menu-item__ellipsis-text"));

            cut.Render(p => p
                .Add(x => x.Ellipsis, true)
                .Add(x => x.ChildContent, MenuWithOneItem()));

            Assert.Single(cut.FindAll(".el-menu-item__ellipsis-text"));
        }

        [Fact]
        public void SubMenuTitleTruncatesToo()
        {
            // 只截断 ElMenuItem 会留下半条功能：同级子菜单标题照样溢出（实测 li scrollWidth 401 vs 239）。
            var cut = Render<ElMenu>(p => p.AddChildContent(NestedMenu()));

            Assert.Single(cut.FindAll(".el-sub-menu__title .el-menu-item__ellipsis-text"));
        }

        [Fact]
        public void SubMenuTitleOmitsClassWhenEllipsisFalse()
        {
            var cut = Render<ElMenu>(p => p
                .Add(x => x.Ellipsis, false)
                .AddChildContent(NestedMenu()));

            Assert.Empty(cut.FindAll(".el-menu-item__ellipsis-text"));
            // 正向对照：标题的 span 在，只是没带截断类
            Assert.Single(cut.FindAll(".el-sub-menu__title > span"));
        }

        #endregion

        #region ellipsis 截断悬停 tooltip（本库扩展）

        /// <summary>
        /// 打桩「文字是否真被截断」的 JS 测算：菜单在 bUnit 里没有布局，
        /// scrollWidth / clientWidth 只能由 JS interop 喂进来（Loose 模式未打桩时返回 null＝视为未截断）。
        /// </summary>
        private void StubTextOverflow(bool clipped, string text = "一个很长很长很长的菜单项用于验证 ellipsis 截断")
            => JSInterop.Setup<TextClipState>(TextClip.MeasureFunction, _ => true)
                .SetResult(new TextClipState
                {
                    ScrollWidth = clipped ? 401 : 200,
                    ClientWidth = 200,
                    Text = text
                });

        /// <summary>确认悬停处理确实跑到了 JS 测算这一步（负向断言的反空转对照）。</summary>
        private void WaitForOverflowMeasured(IRenderedComponent<ElMenu> cut)
        {
            cut.WaitForAssertion(() => Assert.Contains(
                TextClip.MeasureFunction,
                JSInterop.Invocations.Select(x => x.Identifier)), timeout: TestWait.Timeout);
        }

        [Fact]
        public void EllipsisTooltipWrapsTextSpanInTooltipTrigger()
        {
            var cut = Render<ElMenu>(p => p.AddChildContent(MenuWithOneItem()));

            // 截断 tooltip 挂上后，文字 span 从 li 的直接子节点挪进 ElTooltip 的触发 span ——
            // 浮层渲染在触发 span 内部，绝不能放进带 overflow:hidden 的文字 span 里，否则浮层一起被裁掉。
            Assert.Single(cut.FindAll(".el-menu-item > span.el-tooltip__trigger > span.el-menu-item__ellipsis-text"));
        }

        [Fact]
        public void EllipsisTooltipShowsFullTextOnHoverWhenClipped()
        {
            StubTextOverflow(clipped: true, text: "完整文字");
            var cut = Render<ElMenu>(p => p.AddChildContent(MenuWithOneItem()));

            cut.Find("li.el-menu-item").TriggerEvent("onmouseenter", new MouseEventArgs());

            cut.WaitForAssertion(() =>
            {
                var popper = cut.Find("li.el-menu-item .el-tooltip__popper");
                Assert.Equal("完整文字", popper.TextContent.Trim());
                Assert.Equal("tooltip", popper.GetAttribute("role"));
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public void EllipsisTooltipStaysHiddenWhenTextNotClipped()
        {
            StubTextOverflow(clipped: false);
            var cut = Render<ElMenu>(p => p.AddChildContent(MenuWithOneItem()));

            cut.Find("li.el-menu-item").TriggerEvent("onmouseenter", new MouseEventArgs());

            WaitForOverflowMeasured(cut);
            Assert.Empty(cut.FindAll(".el-tooltip__popper"));
        }

        [Fact]
        public void EllipsisTooltipHidesOnMouseLeave()
        {
            StubTextOverflow(clipped: true, text: "完整文字");
            var cut = Render<ElMenu>(p => p.AddChildContent(MenuWithOneItem()));

            cut.Find("li.el-menu-item").TriggerEvent("onmouseenter", new MouseEventArgs());
            cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".el-tooltip__popper")), timeout: TestWait.Timeout);

            cut.Find("li.el-menu-item").TriggerEvent("onmouseleave", new MouseEventArgs());

            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".el-tooltip__popper")), timeout: TestWait.Timeout);
        }

        [Fact]
        public void EllipsisTooltipToleratesMissingJsState()
        {
            // 不打桩：Loose 模式返回 default(T)＝null，等价于「量不到」—— 不许抛，也不许凭空弹一层
            var cut = Render<ElMenu>(p => p.AddChildContent(MenuWithOneItem()));

            cut.Find("li.el-menu-item").TriggerEvent("onmouseenter", new MouseEventArgs());

            WaitForOverflowMeasured(cut);
            Assert.Empty(cut.FindAll(".el-tooltip__popper"));
        }

        [Fact]
        public void EllipsisFalseMountsNoTooltip()
        {
            var cut = Render<ElMenu>(p => p
                .Add(x => x.Ellipsis, false)
                .AddChildContent(MenuWithOneItem()));

            Assert.Empty(cut.FindAll(".el-tooltip__trigger"));
            // 正向对照：文字 span 仍以 li 直接子节点存在（退回旧 DOM 形状）
            Assert.Single(cut.FindAll(".el-menu-item > span"));
        }

        [Fact]
        public void CollapsedMenuMountsNoEllipsisTooltip()
        {
            // 折叠态由原生 title 负责（EP menu-item.vue 的 tooltip 条件是 collapse && slots.title），
            // 且折叠 CSS 把 li 的直接子 span 收成 0×0，浮层会被一起裁掉，所以这里根本不挂。
            var cut = Render<ElMenu>(p => p
                .Add(x => x.Collapse, true)
                .AddChildContent(Item("1", "导航一", title: "导航一")));

            Assert.Empty(cut.FindAll(".el-tooltip__trigger"));
            Assert.Equal("导航一", cut.Find("li.el-menu-item").GetAttribute("title"));
        }

        [Fact]
        public void HorizontalSubMenuTitleMountsNoTooltipButItemsDo()
        {
            // 横向子菜单悬停本身要打开弹层，标题再挂 tooltip 就是两层浮层互相盖；
            // 同级的普通菜单项没有这个问题，照旧挂。
            var cut = Render<ElMenu>(p => p
                .Add(x => x.Mode, MenuMode.Horizontal)
                .AddChildContent(NestedMenu()));

            Assert.Empty(cut.FindAll("div.el-sub-menu__title .el-tooltip__trigger"));
            Assert.Single(cut.FindAll("li.el-menu-item > .el-tooltip__trigger"));
        }

        [Fact]
        public void VerticalSubMenuTitleShowsFullTextOnHover()
        {
            StubTextOverflow(clipped: true, text: "一个很长很长很长的子菜单标题");
            var cut = Render<ElMenu>(p => p.AddChildContent(NestedMenu()));

            Assert.Single(cut.FindAll("div.el-sub-menu__title > span.el-tooltip__trigger"));

            cut.Find("div.el-sub-menu__title").TriggerEvent("onmouseenter", new MouseEventArgs());

            cut.WaitForAssertion(() => Assert.Equal("一个很长很长很长的子菜单标题",
                cut.Find("div.el-sub-menu__title .el-tooltip__popper").TextContent.Trim()), timeout: TestWait.Timeout);
        }

        #endregion

        #region 官方属性补齐（Id / AriaLabel / Ellipsis / exposes）续：exposes

        [Fact]
        public void MenuExposesOpenCloseAndResize()
        {
            var cut = Render<ElMenu>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElSubMenu>(0);
                builder.AddAttribute(1, "Index", "s1");
                builder.AddAttribute(2, "Label", "分组");
                builder.AddAttribute(3, "ChildContent", MenuWithOneItem());
                builder.CloseComponent();
            }));

            Assert.False(cut.Instance.IsSubMenuOpened("s1"));

            cut.InvokeAsync(() => cut.Instance.OpenAsync("s1"));
            Assert.True(cut.Instance.IsSubMenuOpened("s1"));

            cut.InvokeAsync(() => cut.Instance.CloseAsync("s1"));
            Assert.False(cut.Instance.IsSubMenuOpened("s1"));

            cut.InvokeAsync(() => cut.Instance.HandleResizeAsync());
        }

        [Fact]
        public void UniqueOpenedClosesSibling()
        {
            var cut = Render<ElMenu>(p => p
                .Add(x => x.UniqueOpened, true)
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElSubMenu>(0);
                    builder.AddAttribute(1, "Index", "u1");
                    builder.AddAttribute(2, "Label", "其一");
                    builder.AddAttribute(3, "ChildContent", MenuWithOneItem());
                    builder.CloseComponent();

                    builder.OpenComponent<ElSubMenu>(4);
                    builder.AddAttribute(5, "Index", "u2");
                    builder.AddAttribute(6, "Label", "其二");
                    builder.AddAttribute(7, "ChildContent", MenuWithOneItem());
                    builder.CloseComponent();
                }));

            cut.InvokeAsync(() => cut.Instance.OpenAsync("u1"));
            cut.InvokeAsync(() => cut.Instance.OpenAsync("u2"));

            Assert.True(cut.Instance.IsSubMenuOpened("u2"));
            Assert.False(cut.Instance.IsSubMenuOpened("u1"));
        }

        private static RenderFragment MenuWithOneItem() => builder =>
        {
            builder.OpenComponent<ElMenuItem>(0);
            builder.AddAttribute(1, "Index", "1");
            builder.AddAttribute(2, "ChildContent",
                (RenderFragment)(b => b.AddContent(0, "item one")));
            builder.CloseComponent();
        };

        #endregion
    }
}
