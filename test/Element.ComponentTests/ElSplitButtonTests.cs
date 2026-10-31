using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 批次97：ElSplitButton / ElSplitButtonItem。
    /// 行为对齐 RadzenSplitButton（主按钮 Click(null)、菜单项 Click(item)、AlwaysOpenPopup、
    /// IsBusy、方向键/Home/End/Enter/Escape/Tab/首字母定位、Close()），
    /// 渲染对齐 Element Plus 的 el-button-group + el-dropdown 组合（caret 挂
    /// el-dropdown__caret-button，默认类型补 el-button--default 以出分隔线）。
    /// <para>
    /// 回归用例 <see cref="CaretClickDuringClosingWindowReopens"/> 锁住「关闭动画窗口期内的
    /// 箭头点击不得被吞」：toggle 判据必须是 IsOpen（含 IsShow），且 OnClosed 按 option 识别；
    /// 修反任何一处，这条都会红。浏览器实测面在 <c>demo/tools/probes/verify-split-button.cjs</c>。
    /// </para>
    /// </summary>
    public class ElSplitButtonTests : BunitContext
    {
        public ElSplitButtonTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
            // SplitHost 里渲染了 ElPopup：打开菜单会走 RenderDropDownAsync 量触发器矩形，
            // 不 stub 的话 Loose 模式返回 null，在 rect.Height 上 NRE
            JSInterop.Setup<BoundingClientRect>("getBoundingClientRect", _ => true)
                .SetResult(new BoundingClientRect { Top = 0, Left = 0, Width = 120, Height = 32, Bottom = 32, Right = 120 });
        }

        private IReadOnlyList<DropDownOption> Menus => Services.GetRequiredService<PopupService>().DropDownMenuOptions;

        private static ElSplitButton Button(IRenderedComponent<SplitHost> cut) => cut.Instance.Instance;

        private static KeyboardEventArgs Key(string key, string code = null) =>
            new KeyboardEventArgs { Key = key, Code = code ?? key };

        private IRenderedComponent<SplitHost> RenderHost(Action<SplitHostBuilder> configure = null)
        {
            var b = new SplitHostBuilder();
            configure?.Invoke(b);
            return Render<SplitHost>(p =>
            {
                p.Add(x => x.Text, b.Text);
                p.Add(x => x.Type, b.Type);
                p.Add(x => x.Size, b.Size);
                p.Add(x => x.Disabled, b.Disabled);
                p.Add(x => x.Busy, b.Busy);
                p.Add(x => x.BusyText, b.BusyText);
                p.Add(x => x.AlwaysOpenPopup, b.AlwaysOpenPopup);
                p.Add(x => x.Visible, b.Visible);
                p.Add(x => x.Id, b.Id);
                p.Add(x => x.Icon, b.Icon);
                p.Add(x => x.IconColor, b.IconColor);
                p.Add(x => x.DropDownIcon, b.DropDownIcon);
                p.Add(x => x.ButtonContent, b.ButtonContent);
                p.Add(x => x.ItemCount, b.ItemCount);
                p.Add(x => x.LastItemDisabled, b.LastItemDisabled);
            });
        }

        /// <summary>
        /// 开菜单并等开启动画尾巴（ShowStatus Begin→End，10ms 真实定时器）落地。
        /// 这笔尾巴的重渲染若落在后续派发之间，缓存节点上的 keydown/click 会被
        /// bUnit 静默丢弃（handler 不执行、无异常、产品状态无任何可见变化）——
        /// NavigationKeysSkipDisabledItemsAndClamp 负载偶发红的真身（1/200 循环复现）。
        /// scaleY(1) 只出现在尾巴渲染的输出里（ShowStatus 初值是 Begin，首帧是 scaleY(0)），
        /// 作为「尾巴已落、再无待处理渲染」的完成证人。
        /// </summary>
        private void OpenAndSettleMenu(IRenderedComponent<SplitHost> cut)
        {
            cut.InvokeAsync(() => Button(cut).ShowDropDownAsync()).Wait();
            cut.WaitForAssertion(
                () => Assert.Contains("scaleY(1)", cut.Markup),
                timeout: TestWait.Timeout);
        }

        private sealed class SplitHostBuilder
        {
            public string Text { get; set; } = "操作";
            public ButtonType Type { get; set; } = ButtonType.Primary;
            public ButtonSize Size { get; set; } = ButtonSize.Default;
            public bool Disabled { get; set; }
            public bool Busy { get; set; }
            public string BusyText { get; set; }
            public bool AlwaysOpenPopup { get; set; }
            public bool Visible { get; set; } = true;
            public string Id { get; set; }
            public string Icon { get; set; }
            public string IconColor { get; set; }
            public string DropDownIcon { get; set; }
            public RenderFragment ButtonContent { get; set; }
            public int ItemCount { get; set; } = -1;
            public bool LastItemDisabled { get; set; } = true;
        }

        // ---- 结构 / ARIA ----

        [Fact]
        public void RendersButtonGroupWithCaretHookAndDefaultSeparatorClass()
        {
            var cut = RenderHost(p => p.Id = "sb-1");
            var group = cut.Find("#sb-1 .el-button-group");
            var buttons = group.QuerySelectorAll("button");
            Assert.Equal(2, buttons.Length);

            var main = cut.Find("#sb-1-main");
            var caret = cut.Find("#sb-1-toggle");
            Assert.Contains("el-button--primary", main.ClassName);
            Assert.Contains("el-dropdown__caret-button", caret.ClassName);
            Assert.Contains("el-button--primary", caret.ClassName);
        }

        [Fact]
        public void DefaultTypeCaretKeepsSeparatorClass()
        {
            var cut = RenderHost(p => { p.Id = "sb-d"; p.Type = ButtonType.Default; });
            Assert.Contains("el-button--default", cut.Find("#sb-d-toggle").ClassName);
        }

        [Fact]
        public void SizeAndTypeCascadeToBothHalves()
        {
            var cut = RenderHost(p => { p.Id = "sb-s"; p.Type = ButtonType.Danger; p.Size = ButtonSize.Small; });
            Assert.Contains("el-button--danger", cut.Find("#sb-s-main").ClassName);
            Assert.Contains("el-button--small", cut.Find("#sb-s-main").ClassName);
            Assert.Contains("el-button--danger", cut.Find("#sb-s-toggle").ClassName);
            Assert.Contains("el-button--small", cut.Find("#sb-s-toggle").ClassName);
        }

        [Fact]
        public void CaretAriaInitialState()
        {
            var cut = RenderHost(p => p.Id = "sb-a");
            var caret = cut.Find("#sb-a-toggle");
            Assert.Equal("false", caret.GetAttribute("aria-expanded"));
            Assert.Equal("menu", caret.GetAttribute("aria-haspopup"));
            Assert.Equal("sb-a-menu", caret.GetAttribute("aria-controls"));
            Assert.Equal("dropdown trigger", caret.GetAttribute("aria-label"));
        }

        [Fact]
        public void MainClickFiresClickWithNullAndDoesNotOpen()
        {
            var cut = RenderHost(p => p.Id = "sb-m");
            cut.Find("#sb-m-main").Click();

            Assert.Null(Assert.Single(cut.Instance.Clicked));
            Assert.Empty(Menus);
        }

        // ---- 打开 / 条目 / option 接线 ----

        [Fact]
        public async Task ShowDropDownRegistersMenuWithStableItemIds()
        {
            var cut = RenderHost(p => p.Id = "sb-o");
            await cut.InvokeAsync(() => Button(cut).ShowDropDownAsync());

            var option = Assert.Single(Menus);
            Assert.Equal("sb-o-menu", option.DropDownId);
            Assert.True(option.IsShow);

            var items = Button(cut).Items;
            Assert.Equal(4, items.Count);
            Assert.Equal(new[] { "提交并关闭", "提交并新建", "打开", "导入" },
                items.Select(x => x.Text).ToArray());
            Assert.Equal("提交并关闭", cut.Find("#sb-o-item-0").TextContent.Trim());
            Assert.Equal("sb-o-item-3", cut.Find("#sb-o-item-3").GetAttribute("id"));
            Assert.Equal("true", cut.Find("#sb-o-item-3").GetAttribute("aria-disabled"));
            Assert.Equal("menu", cut.Find("#sb-o-menu").GetAttribute("role"));
        }

        [Fact]
        public void ShowDropDownWiresPopupContract()
        {
            var cut = RenderHost(p => p.Id = "sb-w");
            cut.InvokeAsync(() => Button(cut).ShowDropDownAsync()).Wait();

            var option = Assert.Single(Menus);
            Assert.Equal("bottom-start", option.Placement);
            Assert.False(option.ShowShadow);
            Assert.Equal("menu", option.AriaRole);
            Assert.Equal("dropdown trigger", option.AriaLabel);
            Assert.True(option.PreventKeyDown);
            Assert.True(option.StopKeyDownPropagation);
        }

        [Fact]
        public void OpenSetsAriaExpandedTrue()
        {
            var cut = RenderHost(p => p.Id = "sb-e");
            cut.InvokeAsync(() => Button(cut).ShowDropDownAsync()).Wait();

            var caret = cut.Find("#sb-e-toggle");
            Assert.Equal("true", caret.GetAttribute("aria-expanded"));
        }

        // ---- 点击条目：回调、收起、Value ----

        [Fact]
        public void ItemClickFiresClickWithItemAndCloses()
        {
            var cut = RenderHost(p => p.Id = "sb-c");
            OpenAndSettleMenu(cut);

            cut.Find("#sb-c-item-1").Click();

            var clicked = Assert.Single(cut.Instance.Clicked);
            Assert.Equal("提交并新建", clicked.Text);
            Assert.Equal("new", clicked.Value);
            Assert.False(Button(cut).IsOpen);
            cut.WaitForAssertion(() => Assert.Empty(Menus), timeout: TestWait.Timeout);
        }

        [Fact]
        public void DisabledItemClickIsNoOpAndKeepsMenuOpen()
        {
            var cut = RenderHost(p => p.Id = "sb-x");
            OpenAndSettleMenu(cut);

            cut.Find("#sb-x-item-3").Click();

            Assert.Empty(cut.Instance.Clicked);
            Assert.True(Button(cut).IsOpen, "禁用项点击后菜单应保持展开");
            Assert.Single(Menus);
        }

        // ---- 键盘 ----

        [Fact]
        public void ArrowDownOpensFromCaretAndAutoFocusesFirstEnabledItem()
        {
            var cut = RenderHost(p => p.Id = "sb-k");
            cut.Find("#sb-k-toggle").KeyDown(Key("ArrowDown"));

            Assert.Single(Menus);
            Assert.Equal(0, Button(cut).FocusedIndex);
        }

        [Fact]
        public void ArrowUpOpensFocusingLastEnabledItem()
        {
            var cut = RenderHost(p => p.Id = "sb-ku");
            cut.Find("#sb-ku-toggle").KeyDown(Key("ArrowUp"));

            Assert.Single(Menus);
            Assert.Equal(2, Button(cut).FocusedIndex);
        }

        [Fact]
        public void NavigationKeysSkipDisabledItemsAndClamp()
        {
            var cut = RenderHost(p => p.Id = "sb-n");
            OpenAndSettleMenu(cut);
            var menu = cut.Find("#sb-n-menu");

            menu.KeyDown(Key("ArrowDown"));
            Assert.Equal(1, Button(cut).FocusedIndex);
            menu.KeyDown(Key("ArrowDown"));
            Assert.Equal(2, Button(cut).FocusedIndex);
            // 末个启用项再 ↓：不落到禁用项、不越界
            menu.KeyDown(Key("ArrowDown"));
            Assert.Equal(2, Button(cut).FocusedIndex);
            menu.KeyDown(Key("End"));
            Assert.Equal(2, Button(cut).FocusedIndex);
            menu.KeyDown(Key("Home"));
            Assert.Equal(0, Button(cut).FocusedIndex);
        }

        [Fact]
        public void EnterActivatesFocusedItemClosesAndFiresClick()
        {
            var cut = RenderHost(p => p.Id = "sb-en");
            OpenAndSettleMenu(cut);
            var menu = cut.Find("#sb-en-menu");

            menu.KeyDown(Key("ArrowDown"));
            menu.KeyDown(Key("Enter"));

            var clicked = Assert.Single(cut.Instance.Clicked);
            Assert.Equal("提交并新建", clicked.Text);
            Assert.False(Button(cut).IsOpen);
            cut.WaitForAssertion(() => Assert.Empty(Menus), timeout: TestWait.Timeout);
        }

        [Fact]
        public void EscapeClosesMenu()
        {
            var cut = RenderHost(p => p.Id = "sb-es");
            OpenAndSettleMenu(cut);
            cut.Find("#sb-es-menu").KeyDown(Key("Escape"));

            Assert.False(Button(cut).IsOpen);
            cut.WaitForAssertion(() => Assert.Empty(Menus), timeout: TestWait.Timeout);
        }

        [Fact]
        public void TabClosesMenu()
        {
            var cut = RenderHost(p => p.Id = "sb-t");
            OpenAndSettleMenu(cut);
            cut.Find("#sb-t-menu").KeyDown(Key("Tab"));

            Assert.False(Button(cut).IsOpen);
            cut.WaitForAssertion(() => Assert.Empty(Menus), timeout: TestWait.Timeout);
        }

        [Fact]
        public void TypeAheadJumpsToItemByFirstLetterSkippingDisabled()
        {
            var cut = RenderHost(p => p.Id = "sb-ty");
            OpenAndSettleMenu(cut);
            var menu = cut.Find("#sb-ty-menu");

            menu.KeyDown(Key("End"));
            Assert.Equal(2, Button(cut).FocusedIndex);
            // 从末项按「提」：跳过禁用项回到「提交并关闭」
            menu.KeyDown(new KeyboardEventArgs { Key = "提" });
            Assert.Equal(0, Button(cut).FocusedIndex);
        }

        // ---- 回归：关闭动画窗口期内点箭头不得吞点击 ----

        /// <summary>
        /// 修复前两处都会让这条红：
        /// ① toggle 判据用 <c>dropDownOption != null</c> —— 关闭尾巴（210ms）未落地时
        ///    点击被当成「关闭中」吞掉；② OnClosed 不按 option 识别 —— 旧尾巴落地时会把
        ///    重新打开的新 option 引用一并清空。
        /// </summary>
        [Fact]
        public async Task CaretClickDuringClosingWindowReopens()
        {
            var cut = RenderHost(p => p.Id = "sb-z");
            await cut.InvokeAsync(() => Button(cut).ShowDropDownAsync());
            Assert.True(Button(cut).IsOpen);

            // 只发起关闭、不等尾巴：IsShow 已 false，dropDownOption 还挂着
            Task closeTask = Task.CompletedTask;
            await cut.InvokeAsync(() => closeTask = Button(cut).CloseAsync());
            Assert.False(Button(cut).IsOpen);

            cut.Find("#sb-z-toggle").Click();

            Assert.True(Button(cut).IsOpen, "关闭动画窗口内点箭头应重新展开菜单");
            var option = Assert.Single(Menus);
            Assert.True(option.IsShow);
            Assert.Single(cut.FindAll("ul.el-dropdown-menu"));

            // 旧尾巴跑完：不得清掉新 option
            await closeTask;
            Assert.True(Button(cut).IsOpen, "旧关闭尾巴落地后不得影响重新打开的菜单");
            Assert.Single(Menus);
        }

        // ---- 禁用 / 忙碌 / AlwaysOpenPopup ----

        [Fact]
        public void DisabledBlocksBothHalvesAndOpening()
        {
            var cut = RenderHost(p => { p.Id = "sb-di"; p.Disabled = true; });

            var root = cut.Find("#sb-di");
            Assert.Contains("is-disabled", root.ClassName);
            Assert.True(cut.Find("#sb-di-main").HasAttribute("disabled"));
            Assert.True(cut.Find("#sb-di-toggle").HasAttribute("disabled"));
            Assert.Equal("-1", cut.Find("#sb-di-main").GetAttribute("tabindex"));
            Assert.Equal("-1", cut.Find("#sb-di-toggle").GetAttribute("tabindex"));

            cut.Find("#sb-di-toggle").KeyDown(Key("ArrowDown"));
            Assert.Empty(Menus);
        }

        [Fact]
        public void BusyShowsLoadingTextAndBlocksCaret()
        {
            var cut = RenderHost(p => { p.Id = "sb-b"; p.Busy = true; p.BusyText = "保存中…"; });

            var main = cut.Find("#sb-b-main");
            Assert.Contains("is-loading", main.ClassName);
            Assert.Equal("true", main.GetAttribute("aria-busy"));
            Assert.Contains("保存中…", main.TextContent);
            Assert.True(main.HasAttribute("disabled"));
            Assert.True(cut.Find("#sb-b-toggle").HasAttribute("disabled"));

            cut.Find("#sb-b-toggle").KeyDown(Key("ArrowDown"));
            Assert.Empty(Menus);
        }

        [Fact]
        public void AlwaysOpenPopupTogglesMenuWithoutFiringClick()
        {
            var cut = RenderHost(p => { p.Id = "sb-al"; p.AlwaysOpenPopup = true; });

            cut.Find("#sb-al-main").Click();
            Assert.Single(Menus);
            Assert.True(Button(cut).IsOpen);

            cut.Find("#sb-al-main").Click();
            Assert.False(Button(cut).IsOpen);
            cut.WaitForAssertion(() => Assert.Empty(Menus), timeout: TestWait.Timeout);

            Assert.Empty(cut.Instance.Clicked);
        }

        // ---- 程序化 Close / Visible / 图标 / 空菜单 ----

        [Fact]
        public void ProgrammaticCloseRemovesMenu()
        {
            var cut = RenderHost(p => p.Id = "sb-pc");
            cut.InvokeAsync(() => Button(cut).ShowDropDownAsync()).Wait();

            // Close 内部走 ElPopup.CloseDropDownAsync（StateHasChanged + 210ms 动画尾巴），
            // 必须在渲染 dispatcher 上触发：直接在测试线程调用会撞 Dispatcher.AssertAccess
            cut.InvokeAsync(() => Button(cut).Close());

            cut.WaitForAssertion(() => Assert.Empty(Menus), timeout: TestWait.Timeout);
        }

        [Fact]
        public void VisibleFalseRendersNothing()
        {
            var cut = RenderHost(p => p.Visible = false);
            Assert.Empty(cut.FindAll(".el-split-button"));
        }

        [Fact]
        public void IconsNormalizeAndIconColorApplies()
        {
            var cut = RenderHost(p =>
            {
                p.Id = "sb-i";
                p.Icon = "search";
                p.IconColor = "red";
                p.DropDownIcon = "more";
            });

            var mainIcon = cut.Find("#sb-i-main i.el-icon-search");
            Assert.NotNull(mainIcon);
            Assert.Contains("color:red", mainIcon.GetAttribute("style"));
            Assert.NotNull(cut.Find("#sb-i-toggle i.el-icon-more"));
        }

        [Fact]
        public void MenuItemsRenderIcons()
        {
            var cut = Render<SplitHost>(p =>
            {
                p.Add(x => x.Text, "操作");
                p.Add(x => x.ItemCount, 2);
                p.Add(x => x.LastItemDisabled, false);
                p.Add(x => x.ChildContent, (RenderFragment)(builder =>
                {
                    builder.OpenComponent<ElSplitButtonItem>(0);
                    builder.AddAttribute(1, nameof(ElSplitButtonItem.Text), "用户");
                    builder.AddAttribute(2, nameof(ElSplitButtonItem.Icon), "user");
                    builder.AddAttribute(3, nameof(ElSplitButtonItem.IconColor), "rgb(245, 108, 108)");
                    builder.CloseComponent();
                    builder.OpenComponent<ElSplitButtonItem>(4);
                    builder.AddAttribute(5, nameof(ElSplitButtonItem.Text), "文档");
                    builder.AddAttribute(6, nameof(ElSplitButtonItem.Icon), "el-icon-document");
                    builder.CloseComponent();
                }));
            });

            cut.InvokeAsync(() => Button(cut).ShowDropDownAsync()).Wait();

            var items = cut.FindAll("li.el-dropdown-menu__item");
            Assert.Equal(2, items.Count);
            Assert.NotNull(items[0].QuerySelector("i.el-icon-user"));
            Assert.NotNull(items[1].QuerySelector("i.el-icon-document"));
        }

        [Fact]
        public void EmptyMenuOpensWithoutCrashAndNoFocusTarget()
        {
            var cut = RenderHost(p => { p.Id = "sb-empty"; p.ItemCount = 0; });
            OpenAndSettleMenu(cut);

            Assert.Single(Menus);
            Assert.Empty(Button(cut).Items);
            Assert.Equal(-1, Button(cut).FocusedIndex);

            cut.Find("#sb-empty-menu").KeyDown(Key("ArrowDown"));
            Assert.Equal(-1, Button(cut).FocusedIndex);
        }

        [Fact]
        public void ButtonContentTakesPrecedenceOverText()
        {
            var cut = Render<SplitHost>(p =>
            {
                p.Add(x => x.Text, "默认文字");
                p.Add(x => x.Id, "sb-bc");
                p.Add(x => x.ButtonContent, (RenderFragment)(b => b.AddContent(0, "自定义")));
            });

            Assert.Contains("自定义", cut.Find("#sb-bc-main").TextContent);
            Assert.DoesNotContain("默认文字", cut.Find("#sb-bc-main").TextContent);
        }

        public sealed class SplitHost : ComponentBase
        {
            [Parameter] public string Text { get; set; }
            [Parameter] public ButtonType Type { get; set; }
            [Parameter] public ButtonSize Size { get; set; }
            [Parameter] public bool Disabled { get; set; }
            [Parameter] public bool Busy { get; set; }
            [Parameter] public string BusyText { get; set; }
            [Parameter] public bool AlwaysOpenPopup { get; set; }
            [Parameter] public bool Visible { get; set; } = true;
            [Parameter] public string Id { get; set; }
            [Parameter] public string Icon { get; set; }
            [Parameter] public string IconColor { get; set; }
            [Parameter] public string DropDownIcon { get; set; }
            [Parameter] public RenderFragment ButtonContent { get; set; }
            [Parameter] public RenderFragment ChildContent { get; set; }
            [Parameter] public int ItemCount { get; set; } = -1;
            [Parameter] public bool LastItemDisabled { get; set; } = true;

            public ElSplitButton Instance { get; private set; }
            public List<ElSplitButtonItem> Clicked { get; } = new();

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<ElSplitButton>(0);
                builder.AddAttribute(1, nameof(ElSplitButton.Text), Text);
                builder.AddAttribute(2, nameof(ElSplitButton.Type), Type);
                builder.AddAttribute(3, nameof(ElSplitButton.Size), Size);
                builder.AddAttribute(4, nameof(ElSplitButton.Disabled), Disabled);
                builder.AddAttribute(5, nameof(ElSplitButton.IsBusy), Busy);
                builder.AddAttribute(6, nameof(ElSplitButton.BusyText), BusyText);
                builder.AddAttribute(7, nameof(ElSplitButton.AlwaysOpenPopup), AlwaysOpenPopup);
                builder.AddAttribute(8, nameof(ElSplitButton.Visible), Visible);
                builder.AddAttribute(9, nameof(ElSplitButton.Id), Id);
                builder.AddAttribute(10, nameof(ElSplitButton.Icon), Icon);
                builder.AddAttribute(11, nameof(ElSplitButton.IconColor), IconColor);
                builder.AddAttribute(12, nameof(ElSplitButton.DropDownIcon), DropDownIcon);
                builder.AddAttribute(13, nameof(ElSplitButton.ButtonContent), ButtonContent);
                builder.AddAttribute(14, nameof(ElSplitButton.Click),
                    EventCallback.Factory.Create<ElSplitButtonItem>(this, item => Clicked.Add(item)));
                builder.AddAttribute(15, nameof(ElSplitButton.ChildContent), (RenderFragment)(b =>
                {
                    if (ChildContent != null)
                    {
                        b.AddContent(0, ChildContent);
                        return;
                    }

                    if (ItemCount == 0)
                    {
                        return;
                    }

                    AddItem(b, 0, "提交并关闭");
                    AddItem(b, 1, "提交并新建", value: "new");
                    AddItem(b, 2, "打开");
                    AddItem(b, 3, "导入", disabled: LastItemDisabled);
                }));
                builder.AddComponentReferenceCapture(16, o => Instance = (ElSplitButton)o);
                builder.CloseComponent();

                builder.OpenComponent<ElPopup>(17);
                builder.CloseComponent();
            }

            private static void AddItem(RenderTreeBuilder b, int seq, string text, string value = null, bool disabled = false)
            {
                b.OpenComponent<ElSplitButtonItem>(seq * 10);
                b.AddAttribute(seq * 10 + 1, nameof(ElSplitButtonItem.Text), text);
                if (value != null)
                {
                    b.AddAttribute(seq * 10 + 2, nameof(ElSplitButtonItem.Value), value);
                }
                b.AddAttribute(seq * 10 + 3, nameof(ElSplitButtonItem.Disabled), disabled);
                b.CloseComponent();
            }
        }
    }
}
