using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// <see cref="ElDialogGrowService"/> 服务化对话框测试：打开组件 / 内联内容、回传结果、
    /// Confirm / Alert、CanClose 拦截、动态改选项。
    /// </summary>
    public class ElDialogGrowServiceTests : BunitContext
    {
        public ElDialogGrowServiceTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private ElDialogGrowService Service => Services.GetRequiredService<ElDialogGrowService>();

        private IRenderedComponent<ElDialogGrowHost> RenderHost()
        {
            return Render<ElDialogGrowHost>();
        }

        [Fact]
        public void OpenAsync_RendersDialogWithTitleAndComponent()
        {
            var host = RenderHost();
            var task = Service.OpenAsync<ProbeContent, string>("标题A", new Dictionary<string, object> { ["Text"] = "内容A" });

            Assert.Equal(1, Service.Count);
            var dialog = host.Find(".el-dialog-grow");
            Assert.Contains("标题A", dialog.TextContent);
            Assert.Contains("内容A", host.Markup);
        }

        [Fact]
        public async Task OpenAsync_ComponentClosesWithResult_TaskCompletes()
        {
            var host = RenderHost();
            var task = Service.OpenAsync<ProbeContent, string>("标题B");

            var probe = host.FindComponent<ProbeContent>();
            await host.InvokeAsync(() => probe.Instance.CloseWith("我回传的值"));

            Assert.Equal("我回传的值", await task);
            Assert.Equal(0, Service.Count);
            // 关闭后对话框节点必须从 DOM 摘除（否则 Task 已完成但对话框还挂在页面上）。
            Assert.Empty(host.FindAll(".el-dialog-grow"));
        }

        [Fact]
        public async Task OpenAsync_CancelReturnsDefault()
        {
            var host = RenderHost();
            var task = Service.OpenAsync<ProbeContent, string>("标题C");

            await host.InvokeAsync(() => host.FindComponent<ProbeContent>().Instance.CloseWith(null));

            Assert.Null(await task);
        }

        [Fact]
        public async Task OpenAsync_InlineContent_CascadedRefReturnsValue()
        {
            var host = RenderHost();
            var task = Service.OpenAsync<string>("内联标题", dlg => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddContent(1, "内联内容");
                builder.CloseElement();
                builder.OpenComponent<InlineProbe>(2);
                builder.CloseComponent();
            });

            Assert.Contains("内联内容", host.Markup);

            // 内联 fragment 的级联 ref 由 ElDialogGrow 下发，通过探针组件验证链路。
            var probe = host.FindComponent<InlineProbe>();
            await host.InvokeAsync(() => probe.Instance.DialogRef.CloseAsync("inline-result"));

            Assert.Equal("inline-result", await task);
        }

        [Fact]
        public void Options_MappedOntoDialog()
        {
            var host = RenderHost();
            Service.OpenAsync<string>("选项映射", dlg => b => b.AddContent(0, "x"),
                new ElDialogGrowOptions
                {
                    Width = "640px",
                    Height = "320px",
                    ShowClose = false,
                    Role = "alertdialog",
                    ModalClass = "my-modal",
                    WrapperCssClass = "my-wrapper",
                    ContentCssClass = "my-body"
                });

            var dialog = host.Find(".el-dialog-grow");
            var style = dialog.GetAttribute("style");
            Assert.Contains("width:640px", style);
            Assert.Contains("height:320px", style);
            Assert.Empty(host.FindAll(".el-dialog__headerbtn"));
            Assert.Equal("alertdialog", dialog.GetAttribute("role"));
            Assert.Contains("my-modal", host.Find(".v-modal").ClassName);
            Assert.Contains("my-wrapper", host.Find(".el-dialog-grow__wrapper").ClassName);
            Assert.Contains("my-body", host.Find(".el-dialog__body").ClassName);
        }

        [Fact]
        public async Task Options_NotifyChanged_TriggersRerender()
        {
            var host = RenderHost();
            var options = new ElDialogGrowOptions { Title = "会被覆盖", Width = "50%" };
            Service.OpenAsync<string>("初始", dlg => b => b.AddContent(0, "x"), options);

            // OpenAsync 的 title 参数优先于 options.Title（按设计）。
            Assert.Equal("初始", host.Find(".el-dialog__title").TextContent);

            options.Title = "新标题";
            options.Width = "80%";
            // 选项变更 → 宿主 InvokeAsync(StateHasChanged)（排队），需让 bUnit 把队列跑完。
            await host.InvokeAsync(() => { });

            var style = host.Find(".el-dialog-grow").GetAttribute("style");
            Assert.Equal("新标题", host.Find(".el-dialog__title").TextContent);
            Assert.Contains("width:80%", style);
        }

        [Fact]
        public async Task CanClose_False_BlocksUserClose_ButServiceCloseWorks()
        {
            var host = RenderHost();
            Service.OpenAsync<string>("关不掉", dlg => b => b.AddContent(0, "x"),
                new ElDialogGrowOptions { CanClose = () => Task.FromResult(false) });

            var dialog = host.FindComponent<ElDialogGrow>();
            await host.InvokeAsync(() => dialog.Instance.CloseAsync("用户关闭"));

            Assert.Equal(1, Service.Count);
            Assert.True(dialog.Instance.Visible);

            // 编程式关闭（TryCloseAsync）同样走 CanClose，被拦下。
            Assert.False(await Service.TryCloseAsync("x"));
            Assert.Equal(1, Service.Count);
        }

        [Fact]
        public async Task ConfirmAsync_ReturnsTrueOnOk()
        {
            var host = RenderHost();
            var task = Service.ConfirmAsync("确认内容", "确认标题");

            Assert.Contains("确认内容", host.Markup);
            Assert.Contains("确认标题", host.Find(".el-dialog__title").TextContent);

            var buttons = host.FindAll(".el-dialog-grow__confirm-buttons button");
            Assert.Equal(2, buttons.Count);

            await cutInvokeConfirm(host, true);
            Assert.True(await task);
        }

        [Fact]
        public async Task ConfirmAsync_ReturnsFalseOnCancel()
        {
            var host = RenderHost();
            var task = Service.ConfirmAsync("确认内容2");

            await cutInvokeConfirm(host, false);
            Assert.False(await task);
        }

        [Fact]
        public async Task AlertAsync_SingleOkButton()
        {
            var host = RenderHost();
            var task = Service.AlertAsync("提示内容");

            Assert.Contains("提示内容", host.Markup);
            var buttons = host.FindAll(".el-dialog-grow__confirm-buttons button");
            Assert.Single(buttons);

            await buttons[0].ClickAsync(new MouseEventArgs());
            await task;
            Assert.Equal(0, Service.Count);
        }

        /// <summary>
        /// 确认框按 EP MessageBox 口径取 420px 宽（对话框默认 50% 会把一行提示拉成横幅），
        /// 并给正文区挂 el-dialog-grow__confirm-body，让内容贴标题而不是被上下等距内边距夹成垂直居中。
        /// </summary>
        [Fact]
        public async Task ConfirmAsync_UsesMessageBoxWidthAndTopAlignedBody()
        {
            var host = RenderHost();
            var task = Service.ConfirmAsync("确认内容3", "确认标题3");

            Assert.Contains("width:420px", host.Find(".el-dialog-grow").GetAttribute("style"));
            Assert.Contains("el-dialog-grow__confirm-body", host.Find(".el-dialog__body").GetAttribute("class"));

            await cutInvokeConfirm(host, true);
            Assert.True(await task);
        }

        /// <summary>调用方显式给过 Width 时不覆盖，只补正文类。</summary>
        [Fact]
        public async Task ConfirmAsync_KeepsExplicitWidth()
        {
            var host = RenderHost();
            var task = Service.ConfirmAsync("确认内容4", "确认标题4", new ElDialogGrowConfirmOptions { Width = "30%" });

            Assert.Contains("width:30%", host.Find(".el-dialog-grow").GetAttribute("style"));
            Assert.Contains("el-dialog-grow__confirm-body", host.Find(".el-dialog__body").GetAttribute("class"));

            await cutInvokeConfirm(host, false);
            Assert.False(await task);
        }

        /// <summary>
        /// 官方 Dialog 口径：按钮区是 .el-dialog__footer 的子节点（不是塞在正文里居中），
        /// 次按钮在前、主按钮在最后，主按钮带 autofocus 以保住「Enter = 确定」。
        /// </summary>
        [Fact]
        public async Task ConfirmAsync_ButtonsLiveInFooterWithPrimaryLast()
        {
            var host = RenderHost();
            var task = Service.ConfirmAsync("确认内容5", "确认标题5");

            var footer = host.Find(".el-dialog__footer");
            var buttons = footer.QuerySelectorAll(".el-dialog-grow__confirm-buttons button");
            Assert.Equal(2, buttons.Count);
            Assert.Equal("取消", buttons[0].TextContent.Trim());
            Assert.Equal("确定", buttons[1].TextContent.Trim());
            Assert.Contains("el-button--primary", buttons[1].GetAttribute("class"));
            Assert.NotNull(buttons[1].GetAttribute("autofocus"));
            // 正文里不该再有一份按钮条（旧口径把按钮画在 body 中间）
            Assert.Empty(host.Find(".el-dialog__body").QuerySelectorAll("button"));

            await cutInvokeConfirm(host, true);
            Assert.True(await task);
        }

        /// <summary>警告框只有一个主按钮，同样落在页脚，并保持 autofocus。</summary>
        [Fact]
        public async Task AlertAsync_SingleOkButtonLivesInFooter()
        {
            var host = RenderHost();
            var task = Service.AlertAsync("提示内容5");

            var buttons = host.Find(".el-dialog__footer").QuerySelectorAll(".el-dialog-grow__confirm-buttons button");
            Assert.Single(buttons);
            Assert.Contains("el-button--primary", buttons[0].GetAttribute("class"));
            Assert.NotNull(buttons[0].GetAttribute("autofocus"));

            await buttons[0].ClickAsync(new MouseEventArgs());
            await task;
        }

        [Fact]
        public async Task Close_CompletesTopDialog()
        {
            var host = RenderHost();
            var first = Service.OpenAsync<string>("第一个");
            var second = Service.OpenAsync<string>("第二个");
            Assert.Equal(2, Service.Count);

            Service.Close("第二个的结果");

            Assert.Equal("第二个的结果", await second);
            Assert.Equal(1, Service.Count);
            Assert.False(first.IsCompleted);

            Service.Close(null);
            await first;
        }

        [Fact]
        public void Refresh_DoesNotThrow()
        {
            var host = RenderHost();
            Service.OpenAsync<string>("刷新", dlg => b => b.AddContent(0, "x"));

            Service.Refresh();
            Assert.Contains("刷新", host.Markup);
        }

        /// <summary>点击 Confirm 对话框的按钮（官方口径：次按钮在前、主按钮在最后，所以 1=确定 / 0=取消）。</summary>
        private static Task cutInvokeConfirm(IRenderedComponent<ElDialogGrowHost> host, bool ok)
        {
            var buttons = host.FindAll(".el-dialog-grow__confirm-buttons button");
            return buttons[ok ? 1 : 0].ClickAsync(new MouseEventArgs());
        }

        /// <summary>服务化对话框的示例内容组件：把参数显示出来，并可回传结果。</summary>
        private class ProbeContent : ComponentBase
        {
            [Parameter] public string Text { get; set; }
            [CascadingParameter] public ElDialogGrowRef Dialog { get; set; }

            public Task CloseWith(object result) => Dialog.CloseAsync(result);

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "probe-content");
                builder.AddContent(2, Text ?? "probe");
                builder.CloseElement();
            }
        }

        /// <summary>内联 fragment 的探针：拿到级联 ref 以验证 CloseAsync 链路。</summary>
        private class InlineProbe : ComponentBase
        {
            [CascadingParameter] public ElDialogGrowRef DialogRef { get; set; }
        }
    }
}