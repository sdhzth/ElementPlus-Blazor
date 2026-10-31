using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElNotification（NotificationService + ElPopup 宿主）对齐 Element Plus Notification 的回归用例。
    /// 参考：https://element-plus.org/zh-CN/component/notification
    /// </summary>
    public class ElNotificationTests : BunitContext
    {
        public ElNotificationTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private (IRenderedComponent<ElPopup> cut, NotificationService service) RenderHost()
        {
            var cut = Render<ElPopup>();
            return (cut, Services.GetRequiredService<NotificationService>());
        }

        [Fact]
        public void DefaultOption_HasNoType_AndRendersNoIcon()
        {
            var (cut, service) = RenderHost();

            // EP：type 默认为空串 ⇒ 不显示图标、不带类型色
            service.Show(new NotificationOption { Title = "T", Message = "M", Duration = 0 });
            cut.Render();

            var el = cut.Find(".el-notification");
            Assert.Contains("right", el.ClassList);                 // 默认 top-right
            Assert.Contains("top:0px", el.GetAttribute("style"));   // offset 默认 0
            Assert.Equal("alert", el.GetAttribute("role"));
            Assert.Empty(cut.FindAll(".el-notification__icon"));
        }

        [Fact]
        public void TypeModifierClass_IsOnIconElementNotRoot()
        {
            var (cut, service) = RenderHost();

            service.Show(new NotificationOption { Message = "M", Type = MessageType.Success, Duration = 0 });
            cut.Render();

            var icon = cut.Find(".el-notification__icon");
            Assert.Contains("el-notification--success", icon.ClassList);
            Assert.Contains("el-icon-success", icon.ClassList);
            Assert.DoesNotContain("el-notification--success", cut.Find(".el-notification").ClassList);
        }

        [Fact]
        public void PrimaryType_UsesInfoGlyph()
        {
            var (cut, service) = RenderHost();

            service.Primary("T", "M").Duration = 0;
            cut.Render();

            var icon = cut.Find(".el-notification__icon");
            Assert.Contains("el-notification--primary", icon.ClassList);
            Assert.Contains("el-icon-info", icon.ClassList);
        }

        [Fact]
        public void Message_IsWrappedInParagraph_AndTitleControlsContentMargin()
        {
            var (cut, service) = RenderHost();

            service.Show(new NotificationOption { Title = "T", Message = "M", Duration = 0 });
            cut.Render();
            Assert.Contains("<p>M</p>", cut.Markup);
            Assert.DoesNotContain("margin:0", cut.Find(".el-notification__content").GetAttribute("style") ?? "");

            service.CloseAll();
            service.Show(new NotificationOption { Message = "M2", Duration = 0 });
            cut.Render();
            // EP：无标题时正文 margin 归零
            Assert.Equal("margin:0", cut.Find(".el-notification__content").GetAttribute("style"));
        }

        [Fact]
        public void DangerouslyUseHTMLString_RendersRawHtml()
        {
            var (cut, service) = RenderHost();

            service.Show(new NotificationOption
            {
                Message = "<b>bold</b>",
                DangerouslyUseHTMLString = true,
                Duration = 0
            });
            cut.Render();

            Assert.Contains("<b>bold</b>", cut.Markup);
            Assert.DoesNotContain("&lt;b&gt;", cut.Markup);
        }

        [Fact]
        public void Progress_BarOnlyWhenDurationIsPositive()
        {
            var (cut, service) = RenderHost();

            service.Show(new NotificationOption { Message = "A", Duration = 2000, Progress = true });
            service.Show(new NotificationOption { Message = "B", Duration = 0, Progress = true });
            cut.Render();

            // EP：showProgressBar = !!progress && duration > 0
            Assert.Single(cut.FindAll(".el-notification__progress"));
            Assert.Contains("width:100%", cut.Find(".el-notification__progress-bar").GetAttribute("style"));
            service.CloseAll();
        }

        [Fact]
        public void ShowClose_False_HidesButton_AndCloseIconIsConfigurable()
        {
            var (cut, service) = RenderHost();

            service.Show(new NotificationOption { Message = "A", Duration = 0, ShowClose = false });
            cut.Render();
            Assert.Empty(cut.FindAll(".el-notification__closeBtn"));

            service.CloseAll();
            service.Show(new NotificationOption { Message = "B", Duration = 0, CloseIcon = "el-icon-circle-close" });
            cut.Render();
            Assert.Contains("el-icon-circle-close", cut.Find(".el-notification__closeBtn").ClassList);
        }

        [Fact]
        public async Task Click_FiresOnClick_AndCloseOnClickDismissesAsync()
        {
            var (cut, service) = RenderHost();
            var clicked = 0;

            service.Show(new NotificationOption
            {
                Message = "A",
                Duration = 0,
                CloseOnClick = true,
                OnClick = () => { clicked++; return Task.CompletedTask; }
            });
            cut.Render();

            await cut.InvokeAsync(() => cut.Find(".el-notification").Click());

            Assert.Equal(1, clicked);
            Assert.Empty(cut.FindAll(".el-notification"));
        }

        [Fact]
        public async Task PauseOnHover_KeepsNotificationAlive_AndResumesAfterLeaveAsync()
        {
            var (cut, service) = RenderHost();

            service.Show(new NotificationOption { Message = "A", Duration = 600, PauseOnHover = true });
            cut.Render();
            cut.Find(".el-notification").TriggerEvent("onmouseenter", new System.EventArgs());

            // 不要用 Thread.Sleep：bUnit 里阻塞渲染器线程会连带卡住计时器的调度
            await Task.Delay(900);
            Assert.Single(cut.FindAll(".el-notification"));

            cut.Find(".el-notification").TriggerEvent("onmouseleave", new System.EventArgs());
            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".el-notification")), timeout: System.TimeSpan.FromSeconds(3));
        }

        [Fact]
        public void PauseOnHover_False_ClosesEvenWhileHovering()
        {
            var (cut, service) = RenderHost();

            service.Show(new NotificationOption { Message = "A", Duration = 300, PauseOnHover = false });
            cut.Render();
            cut.Find(".el-notification").TriggerEvent("onmouseenter", new System.EventArgs());

            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".el-notification")), timeout: System.TimeSpan.FromSeconds(3));
        }

        [Fact]
        public void Position_DrivesHorizontalClassAndVerticalStyle()
        {
            var (cut, service) = RenderHost();

            service.Show(new NotificationOption { Message = "A", Duration = 0, Position = NotificationPosition.TopLeft, Offset = 24 });
            service.Show(new NotificationOption { Message = "B", Duration = 0, Position = NotificationPosition.BottomRight, Offset = 40 });
            cut.Render();

            var items = cut.FindAll(".el-notification");
            Assert.Contains("left", items[0].ClassList);
            Assert.Contains("top:24px", items[0].GetAttribute("style"));
            Assert.Contains("right", items[1].ClassList);
            Assert.Contains("bottom:40px", items[1].GetAttribute("style"));
        }

        [Fact]
        public void ExplicitZIndex_IsUsedVerbatim()
        {
            var (cut, service) = RenderHost();

            service.Show(new NotificationOption { Message = "A", Duration = 0, ZIndex = 4321 });
            cut.Render();

            Assert.Contains("z-index:4321", cut.Find(".el-notification").GetAttribute("style"));
        }

        [Fact]
        public void Grouping_MergesIdenticalNotifications()
        {
            var (cut, service) = RenderHost();

            var first = service.Show(new NotificationOption { Title = "T", Message = "M", Duration = 0, Grouping = true });
            var second = service.Show(new NotificationOption { Title = "T", Message = "M", Duration = 0, Grouping = true });
            cut.Render();

            Assert.Same(first, second);
            Assert.Equal(2, first.RepeatCount);
            Assert.Single(cut.FindAll(".el-notification"));
        }

        [Fact]
        public void UpdateOffsets_RepositionsOnlyThatDirection()
        {
            var (cut, service) = RenderHost();

            var top = service.Show(new NotificationOption { Message = "A", Duration = 0, Position = NotificationPosition.TopRight });
            var bottom = service.Show(new NotificationOption { Message = "B", Duration = 0, Position = NotificationPosition.BottomRight });
            cut.Render();

            Assert.Contains("top:0px", cut.FindAll(".el-notification")[0].GetAttribute("style"));
            Assert.Contains("bottom:0px", cut.FindAll(".el-notification")[1].GetAttribute("style"));

            top.Offset = 80;
            bottom.Offset = 80;
            service.UpdateOffsets(NotificationPosition.TopRight);
            cut.Render();

            Assert.Contains("top:80px", cut.FindAll(".el-notification")[0].GetAttribute("style"));
            // EP 的 updateOffsets(position) 只重排该方向（notify.ts 仅遍历 notifications[position]），
            // 另一个方向保持原样；要重排它必须再调一次 UpdateOffsets(BottomRight)。
            Assert.Contains("bottom:0px", cut.FindAll(".el-notification")[1].GetAttribute("style"));

            service.UpdateOffsets(NotificationPosition.BottomRight);
            cut.Render();
            Assert.Contains("bottom:80px", cut.FindAll(".el-notification")[1].GetAttribute("style"));
        }

        [Fact]
        public void Max_LimitsSimultaneousNotifications()
        {
            var (cut, service) = RenderHost();
            service.Max = 2;

            service.Show(new NotificationOption { Message = "A", Duration = 0 });
            service.Show(new NotificationOption { Message = "B", Duration = 0 });
            service.Show(new NotificationOption { Message = "C", Duration = 0 });
            cut.Render();

            Assert.Equal(2, cut.FindAll(".el-notification").Count);
            Assert.Contains("C", cut.Markup);
            Assert.DoesNotContain(">A<", cut.Markup);
        }

        [Fact]
        public async Task Max_Eviction_RemovesOldest_AndDoesNotHang()
        {
            // 回归：Show 超出 Max 时必须真的从集合里移除最早项。
            // 曾只调 CloseCore 而不 Remove → while 条件恒成立 → 同步死循环，浏览器整页卡死。
            // 这里用后台线程 + 超时，把「死循环」转成可读的失败，避免整个测试进程被拖死。
            var service = new NotificationService { Max = 2 };
            var closed = 0;

            var evicting = Task.Run(() =>
            {
                service.Show(new NotificationOption
                {
                    Message = "A",
                    Duration = 0,
                    OnClose = () => { closed++; return Task.CompletedTask; }
                });
                service.Show(new NotificationOption { Message = "B", Duration = 0 });
                service.Show(new NotificationOption { Message = "C", Duration = 0 });
            });

            var finished = await Task.WhenAny(evicting, Task.Delay(3000));
            Assert.True(ReferenceEquals(finished, evicting),
                "Show() 超出 Max 时未从集合移除最早项，发生死循环（页面会卡死）。");

            Assert.Equal(2, service.Notifications.Count);
            Assert.Equal(1, closed);                                   // 被淘汰的 A 触发了 OnClose
            Assert.DoesNotContain(service.Notifications, x => x.Message == "A");
            Assert.Contains(service.Notifications, x => x.Message == "C");
        }

        [Fact]
        public void CustomClassAndIcon_AreApplied()
        {
            var (cut, service) = RenderHost();

            service.Show(new NotificationOption { Message = "A", Duration = 0, CustomClass = "my-notif", Icon = "el-icon-star-on" });
            cut.Render();

            Assert.Contains("my-notif", cut.Find(".el-notification").ClassList);
            Assert.Contains("el-icon-star-on", cut.Find(".el-notification__icon").ClassList);
        }

        [Fact]
        public void CloseAll_ClearsEveryNotification()
        {
            var (cut, service) = RenderHost();

            service.Show(new NotificationOption { Message = "A", Duration = 0 });
            service.Show(new NotificationOption { Message = "B", Duration = 0 });
            cut.Render();
            Assert.Equal(2, cut.FindAll(".el-notification").Count);

            service.CloseAll();
            cut.Render();
            Assert.Empty(cut.FindAll(".el-notification"));
        }
    }
}
