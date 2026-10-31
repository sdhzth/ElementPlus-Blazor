using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElDialogTests : BunitContext
    {
        public ElDialogTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void CenterAddsIsCenterClass()
        {
            var cut = Render<ElDialog>(parameters => parameters
                .Add(x => x.ModelValue, false)
                .Add(x => x.Center, true)
                .Add(x => x.LockScroll, false)
                .Add(x => x.Title, "标题"));

            var cls = cut.Find(".el-dialog").GetAttribute("class");
            Assert.Contains("is-center", cls);
        }

        [Fact]
        public void RendersTitleHeaderBodyAndFooter()
        {
            var cut = Render<ElDialog>(parameters => parameters
                .Add(x => x.ModelValue, false)
                .Add(x => x.LockScroll, false)
                .Add(x => x.Title, "患者信息")
                .AddChildContent("正文内容")
                .Add(x => x.Footer, (RenderFragment)(b => b.AddContent(0, "底部"))));

            Assert.Equal("患者信息", cut.Find(".el-dialog__title").TextContent);
            Assert.Contains("正文内容", cut.Find(".el-dialog__body").TextContent);
            Assert.Contains("底部", cut.Find(".el-dialog__footer").TextContent);
        }

        [Fact]
        public void ClosedDialogWrapperIsHidden()
        {
            var cut = Render<ElDialog>(parameters => parameters
                .Add(x => x.ModelValue, false)
                .Add(x => x.LockScroll, false));

            var wrapper = cut.Find(".el-dialog__wrapper");
            Assert.Contains("display:none", wrapper.GetAttribute("style"));
        }

        [Fact]
        public void OpenDialogWrapperIsVisible()
        {
            var cut = Render<ElDialog>(parameters => parameters
                .Add(x => x.ModelValue, true)
                .Add(x => x.LockScroll, false));

            var wrapper = cut.Find(".el-dialog__wrapper");
            Assert.DoesNotContain("display:none", wrapper.GetAttribute("style"));
        }

        /// <summary>ContentClass / ContentStyle 必须透传到正文容器，用于自定义正文位置。</summary>
        [Fact]
        public void ContentClassAndStylePassThroughToBody()
        {
            var cut = Render<ElDialog>(parameters => parameters
                .Add(x => x.ModelValue, true)
                .Add(x => x.LockScroll, false)
                .Add(x => x.ContentClass, "my-body")
                .Add(x => x.ContentStyle, "padding:0 20px 20px")
                .AddChildContent("正文"));

            var body = cut.Find(".el-dialog__body");
            Assert.Contains("el-dialog__body", body.GetAttribute("class"));
            Assert.Contains("my-body", body.GetAttribute("class"));
            Assert.Equal("padding:0 20px 20px", body.GetAttribute("style"));
        }

        /// <summary>未设置 ContentStyle 时不渲染 style 属性（避免多余空属性干扰样式排查）。</summary>
        [Fact]
        public void BodyHasNoStyleAttributeWhenContentStyleUnset()
        {
            var cut = Render<ElDialog>(parameters => parameters
                .Add(x => x.ModelValue, true)
                .Add(x => x.LockScroll, false));

            Assert.Null(cut.Find(".el-dialog__body").GetAttribute("style"));
        }

        /// <summary>Draggable 时给 .el-dialog 打上可拖拽标记（CSS 据此把标题栏光标改成 move）。</summary>
        [Fact]
        public void Draggable_AddsDraggableClass()
        {
            var cut = Render<ElDialog>(parameters => parameters
                .Add(x => x.ModelValue, true)
                .Add(x => x.LockScroll, false)
                .Add(x => x.Draggable, true));

            Assert.Contains("is-draggable", cut.Find(".el-dialog").GetAttribute("class"));
        }

        [Fact]
        public void NotDraggable_NoDraggableClass()
        {
            var cut = Render<ElDialog>(parameters => parameters
                .Add(x => x.ModelValue, true)
                .Add(x => x.LockScroll, false));

            Assert.DoesNotContain("is-draggable", cut.Find(".el-dialog").GetAttribute("class"));
        }

        /// <summary>
        /// 拖拽偏移回写刻意不触发重渲染（拖拽由 JS 直接改 DOM 保证跟手、避免打断正文里的表单输入），
        /// 位置在下次因其它原因渲染时随 style 里的 transform 还原。
        /// </summary>
        [Fact]
        public async Task Draggable_JsDragCallback_RestoresTransformOnNextRender()
        {
            var cut = Render<ElDialog>(parameters => parameters
                .Add(x => x.ModelValue, true)
                .Add(x => x.LockScroll, false)
                .Add(x => x.Draggable, true));

            Assert.DoesNotContain("transform", cut.Find(".el-dialog").GetAttribute("style") ?? string.Empty);

            await cut.InvokeAsync(() => cut.Instance.JsDialogDrag(60, 24));
            Assert.DoesNotContain("translate(60px,24px)", cut.Find(".el-dialog").GetAttribute("style"));

            cut.Render();
            Assert.Contains("translate(60px,24px)", cut.Find(".el-dialog").GetAttribute("style"));
        }

        /// <summary>重新打开时拖拽位置归零（对齐 Element Plus 的 resetPosition）。</summary>
        [Fact]
        public async Task Draggable_ResetsOffsetOnReopen()
        {
            var cut = Render<ElDialog>(parameters => parameters
                .Add(x => x.ModelValue, true)
                .Add(x => x.LockScroll, false)
                .Add(x => x.Draggable, true));

            await cut.InvokeAsync(() => cut.Instance.JsDialogDrag(60, 24));
            cut.Render();
            Assert.Contains("translate(60px,24px)", cut.Find(".el-dialog").GetAttribute("style"));

            cut.Render(p => p.Add(x => x.ModelValue, false).Add(x => x.LockScroll, false).Add(x => x.Draggable, true));
            cut.Render(p => p.Add(x => x.ModelValue, true).Add(x => x.LockScroll, false).Add(x => x.Draggable, true));

            Assert.DoesNotContain("transform", cut.Find(".el-dialog").GetAttribute("style") ?? string.Empty);
        }
    }
}
