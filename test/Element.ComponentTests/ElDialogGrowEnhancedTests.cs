using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElDialogGrow 增强能力测试：新增参数（Height / Resizable / FocusTrap / ShowTitle / Role ...）
    /// 与关闭回传（CloseAsync(result) + OnCloseWithResult）、级联 <see cref="ElDialogGrowRef"/>。
    /// </summary>
    public class ElDialogGrowEnhancedTests : BunitContext
    {
        public ElDialogGrowEnhancedTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void Height_AppliedWhenProvided()
        {
            var cut = Render<ElDialogGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.Height, "300px"));

            Assert.Contains("height:300px", cut.Find(".el-dialog-grow").GetAttribute("style"));
        }

        [Fact]
        public void Fullscreen_IgnoresHeight()
        {
            var cut = Render<ElDialogGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.Fullscreen, true)
                .Add(x => x.Height, "300px"));

            Assert.DoesNotContain("height:", cut.Find(".el-dialog-grow").GetAttribute("style"));
        }

        [Fact]
        public void Resizable_RendersResizerHandle()
        {
            var cut = Render<ElDialogGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.Resizable, true));

            Assert.Contains("is-resizable", cut.Find(".el-dialog-grow").ClassName);
            Assert.Single(cut.FindAll(".el-dialog-grow__resizer"));
        }

        [Fact]
        public void Resizable_False_NoResizerHandle()
        {
            var cut = Render<ElDialogGrow>(p => p.Add(x => x.Visible, true));
            Assert.Empty(cut.FindAll(".el-dialog-grow__resizer"));
        }

        [Fact]
        public void ShowTitle_False_HidesWholeHeader()
        {
            var cut = Render<ElDialogGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.Title, "标题")
                .Add(x => x.ShowTitle, false));

            Assert.Empty(cut.FindAll(".el-dialog__header"));
            Assert.Empty(cut.FindAll(".el-dialog__headerbtn"));
        }

        [Fact]
        public void Role_AppliedToDialogElement()
        {
            var cut = Render<ElDialogGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.Role, "alertdialog"));

            Assert.Equal("alertdialog", cut.Find(".el-dialog-grow").GetAttribute("role"));
        }

        [Fact]
        public void AriaLabel_FallsBackToTitle()
        {
            var cut = Render<ElDialogGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.Title, "我的标题"));

            Assert.Equal("我的标题", cut.Find(".el-dialog-grow").GetAttribute("aria-label"));
        }

        [Fact]
        public void CloseAriaLabelAndTabIndex_Applied()
        {
            var cut = Render<ElDialogGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.CloseAriaLabel, "关闭此窗口")
                .Add(x => x.CloseTabIndex, -1));

            var btn = cut.Find(".el-dialog__headerbtn");
            Assert.Equal("关闭此窗口", btn.GetAttribute("aria-label"));
            Assert.Equal("-1", btn.GetAttribute("tabindex"));
        }

        [Fact]
        public void WrapperClassAndContentClass_Applied()
        {
            var cut = Render<ElDialogGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.WrapperClass, "my-wrapper")
                .Add(x => x.ContentClass, "my-body"));

            Assert.Contains("my-wrapper", cut.Find(".el-dialog-grow__wrapper").ClassName);
            Assert.Contains("my-body", cut.Find(".el-dialog__body").ClassName);
        }

        [Fact]
        public async Task CloseAsync_WithResult_TriggersOnCloseWithResult()
        {
            object received = null;
            var cut = Render<ElDialogGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.OnCloseWithResult, EventCallback.Factory.Create<object>(this, r => received = r)));

            await cut.InvokeAsync(() => cut.Instance.CloseAsync("返回值"));

            Assert.Equal("返回值", received);
            Assert.False(cut.Instance.Visible);
        }

        [Fact]
        public async Task CloseAsync_TypedResult_IsPassedThrough()
        {
            object received = null;
            var cut = Render<ElDialogGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.OnCloseWithResult, EventCallback.Factory.Create<object>(this, r => received = r)));

            await cut.InvokeAsync(() => cut.Instance.CloseAsync(42));

            Assert.Equal(42, received);
        }

        [Fact]
        public async Task CloseAsync_BlockedByBeforeClose_DoesNotFireResult()
        {
            var fired = false;
            var cut = Render<ElDialogGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.BeforeClose, () => Task.FromResult(false))
                .Add(x => x.OnCloseWithResult, EventCallback.Factory.Create<object>(this, _ => fired = true)));

            await cut.InvokeAsync(() => cut.Instance.CloseAsync("不该回传"));

            Assert.False(fired);
            Assert.True(cut.Instance.Visible);
        }

        [Fact]
        public void DialogRef_ReusedAsActiveRef()
        {
            var dialogRef = new ElDialogGrowRef();
            var cut = Render<ElDialogGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.DialogRef, dialogRef));

            Assert.Same(dialogRef, cut.Instance.ActiveDialogRef);
        }

        [Fact]
        public async Task CascadedRef_InsideContent_CanCloseWithResult()
        {
            object received = null;
            var cut = Render<ElDialogGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.OnCloseWithResult, EventCallback.Factory.Create<object>(this, r => received = r))
                .Add(x => x.ChildContent, (RenderFragment)(b =>
                {
                    b.OpenComponent<RefProbe>(0);
                    b.CloseComponent();
                })));

            var probe = cut.FindComponent<RefProbe>();
            Assert.NotNull(probe.Instance.Dialog);
            Assert.True(probe.Instance.Dialog.IsOpen);

            await cut.InvokeAsync(() => probe.Instance.Dialog.CloseAsync("级联回传"));

            Assert.Equal("级联回传", received);
            Assert.False(cut.Instance.Visible);
        }

        /// <summary>
        /// 缩放结果必须写进 <c>MutableWidth</c> / <c>MutableHeight</c>（而非覆盖 <c>Width</c> / <c>Height</c> 参数）：
        /// 父组件随后因别的原因重渲染（典型：内容组件改了标题触发宿主刷新）时，
        /// 参数仍是旧值，尺寸不能被它打回原样。
        /// </summary>
        [Fact]
        public async Task ResizedSizeSurvivesLaterRerender()
        {
            var cut = Render<ElDialogGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.Width, "500px")
                .Add(x => x.Height, "300px")
                .Add(x => x.Resizable, true)
                .Add(x => x.Title, "原标题"));

            await cut.InvokeAsync(() => cut.Instance.JsResize(820, 460));

            var style = cut.Find(".el-dialog").GetAttribute("style");
            Assert.Contains("width:820px", style);
            Assert.Contains("height:460px", style);

            // 模拟「内容组件改了标题 → 父组件重渲染」，Width/Height 参数值未变。
            cut.Render(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.Width, "500px")
                .Add(x => x.Height, "300px")
                .Add(x => x.Resizable, true)
                .Add(x => x.Title, "新标题"));

            Assert.Contains("新标题", cut.Markup);
            style = cut.Find(".el-dialog").GetAttribute("style");
            Assert.Contains("width:820px", style);
            Assert.Contains("height:460px", style);
        }

        /// <summary>验证级联 ElDialogGrowRef 真的下发给正文内容的探针组件。</summary>
        private class RefProbe : ComponentBase
        {
            [CascadingParameter] public ElDialogGrowRef Dialog { get; set; }
        }
    }
}