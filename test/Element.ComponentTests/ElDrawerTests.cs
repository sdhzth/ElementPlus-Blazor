using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElDrawer 组件测试：覆盖「右上角 × 是否显示」「点击遮罩是否关闭」「动态 title」
    /// 以及「抽屉里放表单，由表单反向改当前抽屉标题（级联 ElDrawerRef）」四项能力。
    /// </summary>
    public class ElDrawerTests : BunitContext
    {
        public ElDrawerTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        /// <summary>抽屉里的「内容组件」替身：靠级联 ElDrawerRef 反向改标题 / 关闭抽屉。</summary>
        private class DrawerContentProbe : ComponentBase
        {
            [CascadingParameter]
            public ElDrawerRef Drawer { get; set; }

            internal static string NextTitle = "表单改过的标题";

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "probe-rename");
                builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, () =>
                {
                    if (Drawer != null)
                    {
                        Drawer.Title = NextTitle;
                    }
                }));
                builder.AddContent(3, "改标题");
                builder.CloseElement();
            }
        }

        private static void AddOpen(ComponentParameterCollectionBuilder<ElDrawer> p)
        {
            p.Add(x => x.ModelValue, true);
        }

        // ===== 基础渲染 =====

        [Fact]
        public void ClosedByDefault_NothingRendered()
        {
            var cut = Render<ElDrawer>(p => p.Add(x => x.Title, "标题"));

            // 懒渲染：首次打开之前不渲染任何抽屉 DOM。
            Assert.Empty(cut.FindAll(".el-drawer__wrapper"));
        }

        [Fact]
        public void Open_RendersTitleAndBody()
        {
            var cut = Render<ElDrawer>(p =>
            {
                AddOpen(p);
                p.Add(x => x.Title, "抽屉标题");
                p.Add(x => x.ChildContent, "正文内容");
            });

            Assert.Equal("抽屉标题", cut.Find(".el-drawer__title").TextContent);
            Assert.Contains("正文内容", cut.Find(".el-drawer__body").TextContent);
        }

        [Fact]
        public void Direction_AppliesClassAndSize()
        {
            var cut = Render<ElDrawer>(p =>
            {
                AddOpen(p);
                p.Add(x => x.Direction, DrawerDirection.Ttb);
                p.Add(x => x.Size, "40%");
            });

            Assert.Contains("ttb", cut.Find(".el-drawer").ClassName);
            Assert.Contains("height:40%", cut.Find(".el-drawer").GetAttribute("style"));
        }

        // ===== 1. 是否显示右上角 × =====

        [Fact]
        public void ShowClose_Default_True_RendersCloseButton()
        {
            var cut = Render<ElDrawer>(p => AddOpen(p));
            Assert.NotEmpty(cut.FindAll(".el-drawer__close-btn"));
        }

        [Fact]
        public void ShowClose_False_NoCloseButton()
        {
            var cut = Render<ElDrawer>(p =>
            {
                AddOpen(p);
                p.Add(x => x.ShowClose, false);
            });

            Assert.Empty(cut.FindAll(".el-drawer__close-btn"));
        }

        [Fact]
        public async Task CloseButton_ClosesDrawer()
        {
            var cut = Render<ElDrawer>(p => AddOpen(p));
            await cut.Find(".el-drawer__close-btn").ClickAsync();

            Assert.False(cut.Instance.ModelValue);
        }

        [Fact]
        public void WithHeader_False_NoHeaderAtAll()
        {
            var cut = Render<ElDrawer>(p =>
            {
                AddOpen(p);
                p.Add(x => x.WithHeader, false);
            });

            Assert.Empty(cut.FindAll(".el-drawer__header"));
        }

        // ===== 2. 点击遮罩层是否关闭 =====

        [Fact]
        public async Task CloseOnClickModal_Default_True_Closes()
        {
            var cut = Render<ElDrawer>(p => AddOpen(p));
            await cut.Find(".v-modal").ClickAsync();

            Assert.False(cut.Instance.ModelValue);
        }

        [Fact]
        public async Task CloseOnClickModal_False_KeepsOpen()
        {
            var cut = Render<ElDrawer>(p =>
            {
                AddOpen(p);
                p.Add(x => x.CloseOnClickModal, false);
            });

            await cut.Find(".v-modal").ClickAsync();
            Assert.True(cut.Instance.ModelValue);
        }

        // ===== 3. 动态设置 title =====

        [Fact]
        public void Title_ChangedByParameter_UpdatesHeader()
        {
            var cut = Render<ElDrawer>(p =>
            {
                AddOpen(p);
                p.Add(x => x.Title, "旧标题");
            });

            cut.Render(p => p.Add(x => x.Title, "新标题"));
            Assert.Equal("新标题", cut.Find(".el-drawer__title").TextContent);
        }

        // ===== 4. 抽屉里的表单改当前抽屉标题（级联 ElDrawerRef）=====

        [Fact]
        public async Task ContentComponent_CanChangeDrawerTitle()
        {
            var cut = Render<ElDrawer>(p =>
            {
                AddOpen(p);
                p.Add(x => x.Title, "原始标题");
                p.Add(x => x.ChildContent, (Microsoft.AspNetCore.Components.RenderFragment)(b =>
                {
                    b.OpenComponent<DrawerContentProbe>(0);
                    b.CloseComponent();
                }));
            });

            Assert.Equal("原始标题", cut.Find(".el-drawer__title").TextContent);

            await cut.Find(".probe-rename").ClickAsync();

            Assert.Equal("表单改过的标题", cut.Find(".el-drawer__title").TextContent);
            Assert.Equal("表单改过的标题", cut.Find(".el-drawer").GetAttribute("aria-label"));
        }

        [Fact]
        public async Task DrawerRef_PassedIn_IsBoundAndCanChangeTitle()
        {
            var reference = new ElDrawerRef();
            var cut = Render<ElDrawer>(p =>
            {
                AddOpen(p);
                p.Add(x => x.Title, "原始标题");
                p.Add(x => x.DrawerRef, reference);
            });

            Assert.Equal("原始标题", reference.Title);

            reference.Title = "外部引用改的标题";
            cut.WaitForState(() => cut.Find(".el-drawer__title").TextContent == "外部引用改的标题", timeout: TestWait.Timeout);
            Assert.Equal("外部引用改的标题", cut.Find(".el-drawer__title").TextContent);
            Assert.True(reference.IsOpen);
        }

        [Fact]
        public async Task Reopen_ResetsTitleBackToParameter()
        {
            var cut = Render<ElDrawer>(p =>
            {
                AddOpen(p);
                p.Add(x => x.Title, "原始标题");
                p.Add(x => x.ChildContent, (Microsoft.AspNetCore.Components.RenderFragment)(b =>
                {
                    b.OpenComponent<DrawerContentProbe>(0);
                    b.CloseComponent();
                }));
            });

            await cut.Find(".probe-rename").ClickAsync();
            Assert.Equal("表单改过的标题", cut.Find(".el-drawer__title").TextContent);

            // 关闭 → 再打开：标题回到 Title 参数值，不残留上一次表单改过的标题。
            await cut.Instance.HandleCloseAsync();
            cut.Render(p => p.Add(x => x.ModelValue, true));
            Assert.Equal("原始标题", cut.Find(".el-drawer__title").TextContent);
        }

        [Fact]
        public async Task BeforeClose_False_KeepsDrawerOpen()
        {
            var cut = Render<ElDrawer>(p =>
            {
                AddOpen(p);
                p.Add(x => x.BeforeClose, () => Task.FromResult(false));
            });

            await cut.Instance.HandleCloseAsync();
            Assert.True(cut.Instance.ModelValue);
        }
    }
}
