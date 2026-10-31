using System;
using Bunit;
using ElementPlus;
using ElementPlus.Lang;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 第三批：弹层族（Dialog / Drawer / Popconfirm / MessageBox / DialogGrow）文案的词条契约。
    /// <para>
    /// 与组件族的一条关键差别：MessageBox 与 ElDialogGrowService 是 <b>服务 + DTO</b>，没有渲染生命周期，
    /// 所以 DTO 属性留 null、由渲染处（<c>ElPopup</c> 或按钮的 RenderFragment）在渲染那一刻取词 ——
    /// 这样"弹层开着时切语言"也能跟着换。已打开弹层的<b>标题</b>例外（它在打开时就定型写进 DialogOption）。
    /// </para>
    /// </summary>
        /// <summary>
        /// 与 <c>ElFeedbackTests</c> 同用 <c>Render&lt;ElPopup&gt;()</c> 当弹层宿主。
        /// 两类并行时的红是真缺陷（static 的 <c>DialogService.Dialogs</c> 被后初始化的宿主换走），
        /// 已定位并修，见 <c>PopupHostIsolationTests</c> 与 <c>ElFeedbackTests</c> 上的说明；
        /// 这里不再需要 <c>[Collection]</c> 并行锁。
        /// </summary>
    public class LangOverlayTextTests : BunitContext
    {
        public LangOverlayTextTests()
        {
            // ElDialog 打开时会 execFocus；没配 Loose 会抛 JSRuntimeUnhandledInvocationException（与 ElDialogTests 同一写法）
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddElementServices();
        }

        private const string ZhJson = @"{
  ""el"": {
    ""dialog"": { ""close"": ""关闭此对话框"" },
    ""drawer"": { ""close"": ""关闭此抽屉"" },
    ""messagebox"": { ""title"": ""提示ZH"", ""confirm"": ""确定ZH"", ""cancel"": ""取消ZH"", ""close"": ""关闭此框ZH"" },
    ""popconfirm"": { ""confirmButtonText"": ""是ZH"", ""cancelButtonText"": ""否ZH"" }
  }
}";

        private const string EnJson = @"{
  ""el"": {
    ""dialog"": { ""close"": ""Close this dialog"" },
    ""drawer"": { ""close"": ""Close this dialog"" },
    ""messagebox"": { ""title"": ""Message"", ""confirm"": ""OK"", ""cancel"": ""Cancel"", ""close"": ""Close this dialog"" },
    ""popconfirm"": { ""confirmButtonText"": ""Yes"", ""cancelButtonText"": ""No"" }
  }
}";

        private void UseLocale(string json, string locale)
        {
            Services.AddSingleton<ILang>(TestLang.Loaded(json, locale));
        }

        // ===== ElDialog / ElDrawer：关闭按钮的可访问名称 =====

        [Fact]
        public void DialogCloseButtonGetsAriaLabelWithoutEntries()
        {
            var cut = Render<ElDialog>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.LockScroll, false)
                .Add(x => x.Title, "患者信息"));

            var close = cut.Find(".el-dialog__headerbtn");

            // 这一处是补缺口：改动前这个按钮只有图标、没有任何可访问名称，
            // 所以连 fallback 都给了 EP 的英文值（纯新增属性，不改任何可见文字）
            Assert.Equal("Close this dialog", close.GetAttribute("aria-label"));
        }

        [Fact]
        public void DialogCloseButtonAriaLabelFollowsEntries()
        {
            UseLocale(ZhJson, "zh-CN");

            var cut = Render<ElDialog>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.LockScroll, false)
                .Add(x => x.Title, "患者信息"));

            Assert.Equal("关闭此对话框", cut.Find(".el-dialog__headerbtn").GetAttribute("aria-label"));
        }

        [Fact]
        public void DrawerCloseButtonKeepsEnglishFallbackWithoutEntries()
        {
            var cut = Render<ElDrawer>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Title, "抽屉标题"));

            // 改动前硬编码就是 "Close"，不接词条时必须一字不变
            Assert.Equal("Close", cut.Find(".el-drawer__close-btn").GetAttribute("aria-label"));
        }

        [Fact]
        public void DrawerCloseButtonAriaLabelFollowsEntries()
        {
            UseLocale(ZhJson, "zh-CN");

            var cut = Render<ElDrawer>(p => p
                .Add(x => x.ModelValue, true)
                .Add(x => x.Title, "抽屉标题"));

            Assert.Equal("关闭此抽屉", cut.Find(".el-drawer__close-btn").GetAttribute("aria-label"));
        }

        // ===== ElPopconfirm：EP 的 en-US 值是 Yes / No（不是 OK / Cancel）=====

        [Fact]
        public void PopconfirmButtonsKeepChineseFallbackWithoutEntries()
        {
            var cut = RenderPopconfirm(null, null);

            Assert.Contains("取消", cut.Markup);
            Assert.Contains("确定", cut.Markup);
        }

        [Fact]
        public void PopconfirmButtonsFollowEntriesUsingEpWording()
        {
            UseLocale(EnJson, "en-US");

            var cut = RenderPopconfirm(null, null);

            Assert.Contains(">No<", cut.Markup);
            Assert.Contains(">Yes<", cut.Markup);
            Assert.DoesNotContain("确定", cut.Markup);
        }

        [Fact]
        public void PopconfirmExplicitButtonsWinOverEntries()
        {
            UseLocale(EnJson, "en-US");

            var cut = RenderPopconfirm("好", "算了");

            Assert.Contains(">算了<", cut.Markup);
            Assert.Contains(">好<", cut.Markup);
            Assert.DoesNotContain(">Yes<", cut.Markup);
        }

        private IRenderedComponent<ElPopconfirm> RenderPopconfirm(string confirmText, string cancelText)
        {
            return Render<ElPopconfirm>(p =>
            {
                // 面板默认不渲染：Trigger=Manual + ModelValue=true 才能稳定拿到按钮，不必模拟点击
                p.Add(x => x.Trigger, PopperTrigger.Manual);
                p.Add(x => x.ModelValue, true);
                p.Add(x => x.Title, "删除这条记录？");
                p.Add(x => x.ChildContent, builder => builder.AddContent(0, "<span class=\"trigger\">触发</span>"));
                if (confirmText != null)
                {
                    p.Add(x => x.ConfirmButtonText, confirmText);
                }

                if (cancelText != null)
                {
                    p.Add(x => x.CancelButtonText, cancelText);
                }
            });
        }

        // ===== MessageBox：服务 + DTO 的形状，取词发生在渲染那一刻 =====
        //
        // 注意 ConfirmAsync/PromptAsync 返回的 Task 要等用户点按钮才完成，
        // 所以这里一律「发起但不 await」—— 一旦 Wait()/await 就会把测试线程挂死
        // （本仓 ElFeedbackTests 也是这个写法）。收尾时点掉按钮让 Task 完成，不留悬挂。

        [Fact]
        public async Task MessageBoxKeepsHeaderAndChineseTextsWithoutEntries()
        {
            var messageBox = Services.GetRequiredService<MessageBox>();
            var popup = Render<ElPopup>();

            // await 这次调度本身（lambda 里 `_ =` 丢弃要点按钮才完成的 Task）：
            // 丢弃 InvokeAsync 返回的 Task 会让工作项只是排队，高压并行下弹层根本没出现。
            await popup.InvokeAsync(() => { _ = messageBox.ConfirmAsync("要删除吗"); });

            // 默认标题从「提示」改成 null 之后，最容易坏的就是 header 整块消失（渲染条件是标题非空）
            await WaitUntilAsync(popup, () => popup.FindAll(".el-message-box__header").Count == 1,
                "弹层 header 没渲染出来");
            Assert.Single(popup.FindAll(".el-message-box__header"));
            Assert.Equal("提示", popup.Find(".el-message-box__title span").TextContent.Trim());

            var buttons = popup.FindAll(".el-message-box__btns button").Select(b => b.TextContent.Trim()).ToList();
            Assert.Contains("取消", buttons);
            Assert.Contains("确定", buttons);

            // 关闭按钮此前只有图标、没有可访问名称
            Assert.Equal("Close this dialog", popup.Find(".el-message-box__headerbtn").GetAttribute("aria-label"));

            await Dismiss(popup);
        }

        [Fact]
        public async Task MessageBoxTextsFollowEntriesAndRefreshWhileOpen()
        {
            var lang = TestLang.Loaded(new Dictionary<string, string>
            {
                { "zh-CN", ZhJson },
                { "en-US", EnJson },
            }, "zh-CN");
            Services.AddSingleton<ILang>(lang);
            var messageBox = Services.GetRequiredService<MessageBox>();
            var popup = Render<ElPopup>();

            // await 这次调度本身（lambda 里 `_ =` 丢弃要点按钮才完成的 Task）：
            // 丢弃 InvokeAsync 返回的 Task 会让工作项只是排队，高压并行下弹层根本没出现。
            await popup.InvokeAsync(() => { _ = messageBox.ConfirmAsync("要删除吗"); });

            await WaitUntilAsync(popup, () => ButtonTexts(popup).Contains("确定ZH"), "弹层按钮文字没渲染出来");
            Assert.Equal("提示ZH", popup.Find(".el-message-box__title span").TextContent.Trim());
            Assert.Equal("关闭此框ZH", popup.Find(".el-message-box__headerbtn").GetAttribute("aria-label"));
            Assert.Contains("确定ZH", ButtonTexts(popup));

            // 关键差异点：弹层开着的时候切语言，按钮与标题也要跟着换。
            // 这要求取词写在 RenderFragment 里（渲染期求值）而不是服务调用时定死成字符串。
            // 切语言要在渲染器的调度队列里做：直接在线程池上等它会绕开这次批处理，
            // 全量并行时就会变成"改了但没渲染"的假红。
            await popup.InvokeAsync(() => lang.SetLangAsync("en-US"));
            // 事件回调把重渲染排进队列；下面的 DrainAsync 会等它真正落地，不靠"下次渲染"碰运气
            popup.Render();

            await WaitUntilAsync(popup, () => ButtonTexts(popup).Contains("OK"),
                "切语言后按钮文字未更新");
            Assert.Equal("Message", popup.Find(".el-message-box__title span").TextContent.Trim());
            Assert.Equal("Close this dialog", popup.Find(".el-message-box__headerbtn").GetAttribute("aria-label"));

            await Dismiss(popup);
        }

        [Fact]
        public async Task MessageBoxExplicitTextsWinOverEntries()
        {
            UseLocale(EnJson, "en-US");
            var messageBox = Services.GetRequiredService<MessageBox>();
            var popup = Render<ElPopup>();

            // 只发起弹层、不等它自己的 Task（点了按钮才完成），但要 await 这次调度本身
            await popup.InvokeAsync(() =>
            {
                _ = messageBox.ConfirmAsync(new MessageBoxOption
                {
                    Message = "要删除吗",
                    Title = "危险操作",
                    ConfirmButtonText = "就删",
                });
            });

            await WaitUntilAsync(popup, () => popup.FindAll(".el-message-box__title span").Count == 1,
                "弹层标题没渲染出来");
            Assert.Equal("危险操作", popup.Find(".el-message-box__title span").TextContent.Trim());

            var buttons = ButtonTexts(popup);
            Assert.Contains("就删", buttons);       // 显式给的压过词条
            Assert.Contains("Cancel", buttons);     // 没给的那颗仍走词条
            Assert.DoesNotContain("OK", buttons);

            await Dismiss(popup);
        }

        [Fact]
        public async Task PromptResolvesTitleThroughEntriesNotServiceDefault()
        {
            var lang = TestLang.Loaded(ZhJson, "zh-CN");
            Services.AddSingleton<ILang>(lang);
            var messageBox = Services.GetRequiredService<MessageBox>();
            var popup = Render<ElPopup>();

            // PromptAsync(message) 不传标题：以前在服务里写死「提示」，现在由渲染处取词条
            await popup.InvokeAsync(() => { _ = messageBox.PromptAsync("名字"); });
            popup.Render();

            await WaitUntilAsync(popup, () => popup.FindAll(".el-message-box__input").Count > 0,
                "Prompt 输入框没渲染出来");
            Assert.Equal("提示ZH", popup.Find(".el-message-box__title span").TextContent.Trim());
            Assert.NotEmpty(popup.FindAll(".el-message-box__input"));

            await Dismiss(popup);
        }

        /// <summary>排干渲染器调度队列：一次 InvokeAsync 往返即可保证之前的渲染批已应用到 DOM。</summary>
        /// <summary>
        /// 等弹层真的渲染出来。ElPopup 是在 <c>OnAfterRender</c> 里二次渲染弹层的，
        /// 所以"排干一次队列"不够，必须允许 渲染→OnAfterRender→再渲染 这一串跑完；
        /// 而 bUnit 的 WaitForAssertion 只在"下一次渲染"求值，那一串若先跑完就永远等不到。
        /// 这个门自己泵（每轮排干队列 + 让出一次 timer），条件不满足最多重试 40 轮（约 1s）。
        /// </summary>
        private static async Task WaitUntilAsync(IRenderedComponent<ElPopup> popup,
            System.Func<bool> condition, string because)
        {
            // 预算与仓内其它等待同档（TestWait.Timeout=20s）：每轮都主动排干一次调度队列，
            // 而不是等"下次渲染"来叫醒。1 秒预算在高压并行下会自己先超时（实测如此）。
            var deadline = DateTime.UtcNow + TestWait.Timeout;
            while (DateTime.UtcNow < deadline)
            {
                await popup.InvokeAsync(() => { });
                if (condition())
                {
                    return;
                }
                await Task.Delay(25);
            }

            Assert.True(false, "等待超时（" + TestWait.Timeout.TotalSeconds + "s）：" + because);
        }

        private static Task DrainAsync(IRenderedComponent<ElPopup> popup) => popup.InvokeAsync(() => { });

        private static List<string> ButtonTexts(IRenderedComponent<ElPopup> popup)
        {
            return popup.FindAll(".el-message-box__btns button").Select(b => b.TextContent.Trim()).ToList();
        }

        /// <summary>点掉取消，让挂起的 MessageBox Task 完成，不把未结束的 await 留在测试里。</summary>
        private static async Task Dismiss(IRenderedComponent<ElPopup> popup)
        {
            // 同样：查询与点击放同一次调度批次，避免中间重渲染导致 handler id 失效
            await popup.InvokeAsync(() =>
            {
                var buttons = popup.FindAll(".el-message-box__btns button");
                if (buttons.Count > 0)
                {
                    buttons.First().Click();
                }
            });
        }
    }
}

