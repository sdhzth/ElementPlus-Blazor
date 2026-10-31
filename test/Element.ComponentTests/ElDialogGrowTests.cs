using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElDialogGrowTests : BunitContext
    {
        public ElDialogGrowTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void HiddenByDefault_WrapperHasDisplayNone()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters.Add(x => x.Title, "标题"));

            var wrapper = cut.Find(".el-dialog-grow__wrapper");
            Assert.Contains("display:none", wrapper.GetAttribute("style"));
            // DestroyOnClose 未开启时内容仍渲染（仅隐藏）。
            Assert.NotEmpty(cut.FindAll(".el-dialog__body"));
        }

        [Fact]
        public void Visible_RendersTitleBodyAndFooter()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.Title, "我的标题")
                .Add(x => x.ChildContent, "正文内容")
                .Add(x => x.Footer, "页脚内容"));

            Assert.Contains("我的标题", cut.Markup);
            Assert.Contains("正文内容", cut.Markup);
            Assert.Contains("页脚内容", cut.Markup);
            Assert.DoesNotContain("display:none", cut.Find(".el-dialog-grow__wrapper").GetAttribute("style"));
            Assert.Equal("我的标题", cut.Find(".el-dialog__title").TextContent);
        }

        [Fact]
        public void DefaultWidth_Is50Percent()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters.Add(x => x.Visible, true));
            Assert.Contains("width:50%", cut.Find(".el-dialog-grow").GetAttribute("style"));
        }

        [Fact]
        public void CustomWidth_IsApplied()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.Width, "80%"));
            Assert.Contains("width:80%", cut.Find(".el-dialog-grow").GetAttribute("style"));
        }

        [Fact]
        public void Fullscreen_AddsClassAndIgnoresWidth()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.Fullscreen, true)
                .Add(x => x.Width, "80%"));
            Assert.Contains("is-fullscreen", cut.Find(".el-dialog-grow").ClassName);
            Assert.DoesNotContain("width:", cut.Find(".el-dialog-grow").GetAttribute("style"));
        }

        [Fact]
        public void Top_Default15vh_Applied()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters.Add(x => x.Visible, true));
            Assert.Contains("margin-top:15vh", cut.Find(".el-dialog-grow").GetAttribute("style"));
        }

        [Fact]
        public void AlignCenter_RemovesMarginTop()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.AlignCenter, true));
            var style = cut.Find(".el-dialog-grow").GetAttribute("style");
            Assert.DoesNotContain("margin-top", style);
            Assert.Contains("is-align-center", cut.Find(".el-dialog-grow__wrapper").ClassName);
        }

        [Fact]
        public void ModalFalse_NoOverlayRendered()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.Modal, false));
            Assert.Empty(cut.FindAll(".v-modal"));
        }

        [Fact]
        public async Task CloseOnClickModal_True_ClosesDialog()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters.Add(x => x.Visible, true));
            await cut.Find(".v-modal").ClickAsync();
            Assert.False(cut.Instance.Visible);
            Assert.Contains("display:none", cut.Find(".el-dialog-grow__wrapper").GetAttribute("style"));
        }

        [Fact]
        public async Task CloseOnClickModal_False_KeepsOpen()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.CloseOnClickModal, false));
            await cut.Find(".v-modal").ClickAsync();
            Assert.True(cut.Instance.Visible);
        }

        [Fact]
        public async Task EscapeKey_ClosesDialog()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters.Add(x => x.Visible, true));
            await cut.Find(".el-dialog-grow__wrapper").TriggerEventAsync("onkeydown",
                new KeyboardEventArgs { Key = "Escape" });
            Assert.False(cut.Instance.Visible);
        }

        [Fact]
        public async Task EscapeKey_Disabled_KeepsOpen()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.CloseOnPressEscape, false));
            await cut.Find(".el-dialog-grow__wrapper").TriggerEventAsync("onkeydown",
                new KeyboardEventArgs { Key = "Escape" });
            Assert.True(cut.Instance.Visible);
        }

        [Fact]
        public void ShowClose_False_HidesCloseButton()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.ShowClose, false));
            Assert.Empty(cut.FindAll(".el-dialog__headerbtn"));
        }

        [Fact]
        public async Task ClickCloseButton_ClosesDialog()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters.Add(x => x.Visible, true));
            await cut.Find(".el-dialog__headerbtn").ClickAsync();
            Assert.False(cut.Instance.Visible);
        }

        [Fact]
        public void Center_AddsCenterClass()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.Center, true));
            Assert.Contains("el-dialog--center", cut.Find(".el-dialog-grow").ClassName);
        }

        [Fact]
        public void Draggable_AddsDraggableClass()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.Draggable, true));
            Assert.Contains("is-draggable", cut.Find(".el-dialog-grow").ClassName);
            Assert.Contains("el-dialog-grow__draggable-header", cut.Find(".el-dialog__header").ClassName);
        }

        [Fact]
        public async Task Draggable_JsDragCallback_RestoresTransformOnNextRender()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.Draggable, true));

            // 拖拽事件源在 JS 侧（document 级 mousemove），.NET 只接偏移；
            // 回写本身不触发重渲染（拖拽由 JS 直接改 DOM），位置在下次渲染时随 style 还原。
            await cut.InvokeAsync(() => cut.Instance.JsDialogDrag(40, 20));
            Assert.DoesNotContain("translate(40px, 20px)", cut.Find(".el-dialog-grow").GetAttribute("style"));

            cut.Render();
            Assert.Contains("translate(40px, 20px)", cut.Find(".el-dialog-grow").GetAttribute("style"));
        }

        [Fact]
        public void Draggable_DoesNotRenderTransformBeforeAnyDrag()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.Draggable, true));

            Assert.DoesNotContain("transform", cut.Find(".el-dialog-grow").GetAttribute("style") ?? string.Empty);
        }

        [Fact]
        public void HeaderSlot_OverridesDefaultTitle()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.Header, (RenderFragment)(b => b.AddContent(0, "自定义标题插槽"))));
            Assert.Contains("自定义标题插槽", cut.Markup);
            Assert.Empty(cut.FindAll(".el-dialog__title"));
        }

        [Fact]
        public async Task DestroyOnClose_RemovesContentWhenClosed()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.DestroyOnClose, true)
                .Add(x => x.ChildContent, "正文内容"));
            Assert.NotEmpty(cut.FindAll(".el-dialog__body"));

            await cut.Instance.CloseAsync();
            Assert.Empty(cut.FindAll(".el-dialog__body"));
        }

        [Fact]
        public async Task BeforeClose_False_PreventsClose()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.BeforeClose, () => Task.FromResult(false)));
            await cut.Find(".v-modal").ClickAsync();
            Assert.True(cut.Instance.Visible);
        }

        [Fact]
        public async Task OnBeforeClose_Cancel_PreventsClose()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.OnBeforeClose,
                    EventCallback.Factory.Create<ElementClosingEventArgs<ElDialogGrow>>(this, args => args.Cancel = true)));
            await cut.Find(".v-modal").ClickAsync();
            Assert.True(cut.Instance.Visible);
        }

        [Fact]
        public async Task OnOpenAndOnClose_Fired()
        {
            var opened = false;
            var closed = false;
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.OnOpen, EventCallback.Factory.Create(this, () => { opened = true; }))
                .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => { closed = true; })));

            Assert.True(opened);

            await cut.Instance.CloseAsync();
            Assert.True(closed);
            Assert.False(cut.Instance.Visible);
        }

        [Fact]
        public void CustomZIndex_AppliedToWrapperAndOverlay()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.ZIndex, 5000));
            Assert.Contains("z-index:5000", cut.Find(".el-dialog-grow__wrapper").GetAttribute("style"));
            Assert.Contains("z-index:4999", cut.Find(".v-modal").GetAttribute("style"));
        }

        [Fact]
        public void ModalClass_AppendedToOverlay()
        {
            var cut = Render<ElDialogGrow>(parameters => parameters
                .Add(x => x.Visible, true)
                .Add(x => x.ModalClass, "my-modal"));
            Assert.Contains("my-modal", cut.Find(".v-modal").ClassName);
        }
    }
}
