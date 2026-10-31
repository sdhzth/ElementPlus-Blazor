using System;
using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
        /// <summary>
        /// 本类与 <c>LangOverlayTextTests</c> 都要 <c>Render&lt;ElPopup&gt;()</c> 当弹层宿主，
        /// 曾经"两类并行 20 次红 5 次"，被记成需要锁进同一个 <c>[Collection]</c> 的未定位干扰。
        /// 真身已定位并修掉：<c>DialogService.Dialogs</c> 当时是 static，后初始化的宿主在
        /// <c>OnInitialized</c> 里把这份共享队列换成新实例，先初始化的宿主就再也收不到弹层
        /// （回归用例见 <c>PopupHostIsolationTests</c>）。集合改为按 DI 作用域存放后并行锁已解除，
        /// 不要再加回来 —— 加了就把这个真实缺陷藏起来。
        /// </summary>
    public class ElFeedbackTests : BunitContext
    {
        public ElFeedbackTests()
        {
            Services.AddElementServices();
        }

        /// <summary>开 Prompt 弹层那一刻抓到的 wrapper 元素对象，只给超时现场比对实例用（见 DescribeIdentity）。</summary>
        private AngleSharp.Dom.IElement openedWrapper;

        /// <summary>
        /// 开层那一刻的 <c>ShadowCount</c>（<c>ElPopup.razor.cs:755</c> 每次 OnAfterRender 按当前模型重算）。
        /// 它是「残留那一刻渲染到底跑完过没有」这条推断的前提：只有开层时它确实=1、红时=0，
        /// 才说得上「有一次渲染对着空模型跑完」。开层就没到过 1 的话，红时的 0 只是初值，那条推断整条作废。
        /// </summary>
        private int? openedShadowCount;

        /// <summary>
        /// 开层那一刻的 <c>RenderCount</c>（bUnit 公开属性，不用反射）。它给的是**没有歧义**的那一问：
        /// 「取消之后到底有没有又跑过渲染」。帧读数 `已用格=0/0` 证不了这一问 —— 第二个 0 是**数组容量**，
        /// 池化数组归还之后容量就是 0，与"这份帧表从来没填过"同形（实测绿路"关干净"也是 0/0，见 docs/testing-guards.md 第 61 条）。
        /// 所以红现场两个一起读：`RenderCount` 涨了而帧里没有 wrapper ⇒ 移除**进了树**、停在落地那一步。
        /// </summary>
        private int? openedRenderCount;


        [Fact]
        public void AlertRendersTypeDescriptionIconAndCloses()
        {
            var closed = false;
            var cut = Render<ElAlert>(parameters => parameters
                .Add(x => x.Title, "Saved")
                .Add(x => x.Description, "The record was saved.")
                .Add(x => x.Type, MessageType.Success)
                .Add(x => x.ShowIcon, true)
                .Add(x => x.OnClose, () => closed = true));

            Assert.Contains("el-alert--success", cut.Find(".el-alert").ClassList);
            Assert.Contains("is-description", cut.Find(".el-alert").ClassList);
            Assert.Contains("el-icon-success", cut.Find(".el-alert__icon").ClassList);

            cut.Find(".el-alert__closebtn").Click();

            Assert.True(closed);
            Assert.Empty(cut.FindAll(".el-alert"));
        }

        [Fact]
        public void DrawerBindsVisibilityAndRespectsBeforeClose()
        {
            var visible = true;
            var closed = 0;
            var allowClose = false;
            var cut = Render<ElDrawer>(parameters => parameters
                .Add(x => x.ModelValue, visible)
                .Add(x => x.ModelValueChanged, value => visible = value)
                .Add(x => x.Title, "Settings")
                .Add(x => x.Direction, DrawerDirection.Ltr)
                .Add(x => x.Size, "320px")
                .Add(x => x.BeforeClose, () => Task.FromResult(allowClose))
                .Add(x => x.OnClosed, () => closed++)
                .AddChildContent("Content"));

            Assert.Contains("el-drawer__open", cut.Find(".el-drawer__wrapper").ClassList);
            Assert.Contains("ltr", cut.Find(".el-drawer").ClassList);
            Assert.Contains("width:320px", cut.Find(".el-drawer").GetAttribute("style"));

            cut.Find(".el-drawer__close-btn").Click();
            Assert.True(visible);
            Assert.Equal(0, closed);

            allowClose = true;
            cut.Find(".el-drawer__close-btn").Click();

            Assert.False(visible);
            Assert.Equal(1, closed);
        }

        [Fact]
        public void TooltipPopoverAndPopconfirmToggleAndEmitActions()
        {
            var tooltip = Render<ElTooltip>(parameters => parameters
                .Add(x => x.Content, "Helpful text")
                .AddChildContent("<button>Hover</button>"));

            // EP 的 trigger 用的是 onMouseenter/onMouseleave；本库同理（mouseover/out 会冒泡，
            // 触发器内部节点每次过渡都各发一次 show/hide，带延迟的两条互相抢占，面板收不掉）。
            tooltip.Find(".el-tooltip__trigger").MouseEnter();
            Assert.Contains("Helpful text", tooltip.Markup);
            tooltip.Find(".el-tooltip__trigger").MouseLeave();
            Assert.DoesNotContain("Helpful text", tooltip.Markup);

            var popover = Render<ElPopover>(parameters => parameters
                .Add(x => x.Title, "Details")
                .Add(x => x.Content, "More information")
                .Add(x => x.Trigger, PopperTrigger.Click)
                .Add(x => x.Reference, (RenderFragment)(builder => builder.AddContent(0, "Open"))));

            // click / contextmenu 只绑在「参考元素」上（面板内联在 wrapper 里，绑 wrapper 会让
            // 面板内的每次点击都冒泡回来再 toggle 一次），所以这里点的是 .el-popover__reference。
            popover.Find(".el-popover__reference").Click();
            Assert.Contains("Details", popover.Markup);
            Assert.Contains("More information", popover.Markup);

            // EP 口径：ElPopover 的 trigger 默认是 hover（tooltip/src/trigger2.mjs 里 default: "hover"），
            // 不写 Trigger 时必须是「悬停开、移开收」，点击不该把它打开。
            var hoverPopover = Render<ElPopover>(parameters => parameters
                .Add(x => x.Title, "Hover only")
                .Add(x => x.Content, "Hover content")
                .Add(x => x.Reference, (RenderFragment)(builder => builder.AddContent(0, "Open"))));

            Assert.DoesNotContain("Hover content", hoverPopover.Markup);
            hoverPopover.Find(".el-popover__reference").Click();
            Assert.DoesNotContain("Hover content", hoverPopover.Markup);
            hoverPopover.Find(".el-popover__reference-wrapper").MouseEnter();
            Assert.Contains("Hover content", hoverPopover.Markup);
            hoverPopover.Find(".el-popover__reference-wrapper").MouseLeave();
            // 关闭走 HideAfter（默认 200ms）延迟，所以要等而不是当场断
            hoverPopover.WaitForState(
                () => !hoverPopover.Markup.Contains("Hover content"),
                timeout: TestWait.Timeout);
            Assert.DoesNotContain("Hover content", hoverPopover.Markup);

            var confirmed = 0;
            var cancelled = 0;
            var popconfirm = Render<ElPopconfirm>(parameters => parameters
                .Add(x => x.Title, "Delete?")
                .Add(x => x.OnConfirm, () => confirmed++)
                .Add(x => x.OnCancel, () => cancelled++)
                .AddChildContent("<button>Delete</button>"));

            popconfirm.Find(".el-popconfirm__reference-wrapper").Click();
            Assert.Contains("Delete?", popconfirm.Markup);

            popconfirm.FindAll(".el-popconfirm__action button").Last().Click();
            Assert.Equal(1, confirmed);
            Assert.Empty(popconfirm.FindAll(".el-popconfirm"));

            popconfirm.Find(".el-popconfirm__reference-wrapper").Click();
            popconfirm.FindAll(".el-popconfirm__action button").First().Click();
            Assert.Equal(1, cancelled);
        }

        [Fact]
        public void MessageAndNotificationServicesRenderThroughPopup()
        {
            JSInterop.SetupVoid("RegisterAnimationBegin", _ => true).SetVoidResult();
            var cut = Render<ElPopup>();
            var messageService = Services.GetRequiredService<MessageService>();
            var notificationService = Services.GetRequiredService<NotificationService>();

            messageService.Show(new MessageInfo
            {
                Message = "Saved",
                Type = MessageType.Success,
                ShowClose = true,
                Plain = true,
                Grouping = true
            });
            messageService.Show(new MessageInfo
            {
                Message = "Saved",
                Type = MessageType.Success,
                Grouping = true
            });
            notificationService.Show(new NotificationOption
            {
                Title = "Deploy",
                Message = "Finished",
                Type = MessageType.Success,
                Duration = 0,
                Position = NotificationPosition.BottomLeft
            });

            cut.Render();

            Assert.Contains("Saved", cut.Markup);
            Assert.Contains("el-message__badge", cut.Markup);
            Assert.Contains("Deploy", cut.Markup);
            Assert.Contains("Finished", cut.Markup);
            Assert.Contains("bottom", cut.Find(".el-notification").GetAttribute("style"));
        }

        [Fact]
        public void LoadingComponentWrapsContentAndCanHideMask()
        {
            var cut = Render<ElLoading>(parameters => parameters
                .Add(x => x.Loading, true)
                .Add(x => x.Text, "Loading")
                .Add(x => x.Background, "rgba(255,255,255,.5)")
                .AddChildContent("<div>Body</div>"));

            Assert.Contains("is-loading", cut.Find(".el-loading-parent").ClassList);
            Assert.Contains("Body", cut.Markup);
            Assert.Equal("Loading", cut.Find(".el-loading-text").TextContent.Trim());

            cut.Render(parameters => parameters
                .Add(x => x.Loading, false)
                .AddChildContent("<div>Body</div>"));

            Assert.Empty(cut.FindAll(".el-loading-mask"));
            Assert.Contains("Body", cut.Markup);
        }

        /// <summary>排干渲染器调度队列：一次 InvokeAsync 往返即可保证之前的渲染批已应用到 DOM。</summary>
        private static Task DrainAsync(IRenderedComponent<ElPopup> popup) => popup.InvokeAsync(() => { });

        /// <summary>
        /// 等弹层真的渲染出来。ElPopup 是在 <c>OnAfterRender</c> 里二次渲染弹层的，
        /// 所以"排干一次队列"不够，必须允许 渲染→OnAfterRender→再渲染 这一串跑完；
        /// 而 bUnit 的 WaitForAssertion 只在"下一次渲染"求值，那一串若先跑完就永远等不到。
        /// 这个门自己泵（每轮排干队列 + 让出一次 timer），直到 <c>TestWait.Timeout</c> 截止线。
        /// </summary>
        private async Task WaitUntilAsync(IRenderedComponent<ElPopup> popup,
            System.Func<bool> condition, string because)
        {
            // 预算与仓内其它等待同档（TestWait.Timeout=20s）：每轮都主动排干一次调度队列，
            // 而不是等"下次渲染"来叫醒。1 秒预算在高压并行下会自己先超时（实测如此）。
            // 轮次与实际耗时要打出来：满载机器上 await Task.Delay(25) 自己就能被饿到几秒，
            // 「等满 20s 没等到」和「只泵了两轮就到线」是两种完全不同的红，不分清楚下次还得再查。
            var deadline = DateTime.UtcNow + TestWait.Timeout;
            var waited = System.Diagnostics.Stopwatch.StartNew();
            var rounds = 0;
            while (DateTime.UtcNow < deadline)
            {
                rounds++;
                await popup.InvokeAsync(() => { });
                if (condition())
                {
                    return;
                }
                await Task.Delay(25);
            }

            // 超出现场：把「模型里还有几层」和「DOM 里有几个 wrapper」一起打出来。
            // 「弹层关不掉」有两种完全不同的真身（宿主没收到移除事件 / 收到了但渲染批没落到 DOM），
            // 只报"没关掉"下次还得再查一遍；负载复现本来就稀有，红一次就得把分叉信息带出来。
            var scene = await popup.InvokeAsync(() => CloseScene(popup));
            var forced = await ForceRenderSceneAsync(popup);
            var pipeline = await PipelineAliveSceneAsync(popup);
            Assert.True(false, $"等待超时（{TestWait.Timeout.TotalSeconds}s）：{because}"
                + $"｜实际等待 {waited.ElapsedMilliseconds}ms 泵了 {rounds} 轮｜超时现场 {scene}"
                + $"｜{forced}"
                + $"｜{pipeline}"
                + $"｜{PeekRendererFault()}"
                + $"｜{PeekRendererBatching()}");
        }

        /// <summary>
        /// 最后一个分叉实验：在红这一刻**再开一层**（它必然要求新增编辑）。分三种形状（口径见方法体里的注释）：
        /// 第二层渲得出来 ⇒ 管道活着，停的只有移除那一批；DOM 没多但本宿主 <c>DialogOptions</c> 涨了 ⇒ 渲染不落地；
        /// DOM 与 <c>DialogOptions</c> 都没涨 ⇒ 这个宿主根本没接到这一层，那属队列/服务侧，不是渲染死。
        /// 先前只数 DOM 就把结果写成「渲染整体不再落到 DOM」是把②③两支混成一支了（本批第一条红就是这么误读的），
        /// 所以现在把模型侧与 <c>queueId</c> 一起打出来。
        /// 只跑 2 秒预算（绿路实测加一层只需几轮，见同文件 <c>WaitUntilAsync</c> 用的同一套泵法），
        /// 且只在超时路径跑：此时断言已经失败，这一步不改判定，只把答案带出来。
        /// </summary>
        private async Task<string> PipelineAliveSceneAsync(IRenderedComponent<ElPopup> popup)
        {
            try
            {
                var before = popup.FindAll(".el-message-box__wrapper");
                var beforeCount = before.Count;
                var beforeFirst = beforeCount > 0 ? before[0] : null;
                // 只数"个数"判不了「这层的编辑没应用」与「旧那份被就地改写/换新」这两支（第 59 条同一种形状）：
                // message-box 分支没有 @key（ElPopup.razor:240，只有 dialog 分支 :180 有），
                // 所以这里在实验前后各打一次 aria-label，并补「实验前那份还在不在 Nodes 里」与实例比对。
                // 绿路实测（Temp/b17-frameprobe.log）：健康宿主加一层时**实例是会换的**，
                // 所以实例读数只作旁证，判定用"个数 + 标题"。
                var beforeLabels = string.Join("|", before.Select(w => w.GetAttribute("aria-label") ?? "?"));
                var hostBefore = popup.Instance.DialogOptions.Count;
                var messageBox = Services.GetRequiredService<MessageBox>();
                Task<MessageBoxPromptResult> extra = null;
                await popup.InvokeAsync(() =>
                {
                    extra = messageBox.PromptAsync(new MessageBoxOption
                    {
                        Message = "诊断用第二层",
                        Title = "诊断用第二层"
                    });
                });

                var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
                var rounds = 0;
                // 循环条件同时看"个数"和"标题"：只看个数会把「旧那份被换成了新层那一份」当成"DOM 没动"，
                // 而绿路实测（Temp/b17-frameprobe.log）加一层时**元素实例本身是会换的**，所以标题才是稳的判据。
                while (DateTime.UtcNow < deadline
                       && popup.FindAll(".el-message-box__wrapper").Count <= beforeCount
                       && !string.Join("|", popup.FindAll(".el-message-box__wrapper")
                               .Select(w => w.GetAttribute("aria-label") ?? "?")).Contains("诊断用第二层"))
                {
                    rounds++;
                    await popup.InvokeAsync(() => { });
                    await Task.Delay(25);
                }

                var after = popup.FindAll(".el-message-box__wrapper");
                var afterCount = after.Count;
                var afterLabels = string.Join("|", after.Select(w => w.GetAttribute("aria-label") ?? "?"));
                var sameAsBefore = beforeFirst is null
                    ? "实验前没有残留（无从判定）"
                    : afterCount == 0
                        ? "实验后一个 wrapper 都没有（该项不作结论）"
                        : ReferenceEquals(beforeFirst, after[0]) ? "是（同一份元素）" : "否（换了实例）";
                // 实验前抓的那一份现在还在不在宿主子树里：绿路实测加一层时它会**被换掉**（Temp/b17-frameprobe.log），
                // 所以这条不能单独当"红＝被摘过"的证据，要和标题一起读。
                var oldStillInTree = beforeFirst is null
                    ? "无从判定"
                    : popup.Nodes.Cast<AngleSharp.Dom.INode>().Contains(beforeFirst) ? "在" : "不在（这份已被摘掉）";
                var newLayerInDom = afterLabels.Contains("诊断用第二层");
                var hostAfter = popup.Instance.DialogOptions.Count;
                var queue = Services.GetRequiredService<DialogService>().Dialogs;
                // 三种形状必须分开，只数 DOM 分不出来（实测：本批第一条红打出的就是第三种，
                // 但「DOM 没多」同时兼容下面两支）：
                //   ① DOM 多了 ⇒ 管道活着，停的只有 Remove 那一批编辑；
                //   ② DOM 没多、本宿主 DialogOptions 多了 ⇒ 模型收到了这层而 DOM 的**个数与标题**都没跟上＝渲染侧；
                //   ③ DOM 没多、DialogOptions 也没多 ⇒ 本宿主根本没收到这层（与队列脱钩／服务上的集合不是它订阅的那个），
                //      这一支属"第二宿主换队列"那条路（ElPopup.razor.cs:190-222），不是渲染死。
                var verdict = afterCount > beforeCount || newLayerInDom
                    ? "第二层渲得出来（渲染管道还活着，停的只有 Remove 那一批编辑）"
                    : hostAfter > hostBefore
                        ? "② 本宿主收到了第二层（DialogOptions 涨了）但 DOM 的个数与标题都没跟上 ⇒ 这层的编辑没应用"
                        : "③ 本宿主没收到第二层（DialogOptions 没涨）⇒ 不是渲染死，是它没接到这层（队列／服务侧），该项要单独定性";
                return $"再开一层实验：加之前={beforeCount} 加之后={afterCount} 泵了{rounds}轮 ⇒ " + verdict +
                       $" 模型侧 host.DialogOptions={hostBefore}→{hostAfter} service.Dialogs={queue.Count}" +
                       $" queueId={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(queue)}" +
                       $" 实验前label=[{beforeLabels}] 实验后label=[{afterLabels}]" +
                       $" 实验后第一份与实验前同一实例={sameAsBefore} 实验前那份现在在Nodes里={oldStillInTree}" +
                       $" nodes序列={DescribeNodes(popup.Nodes)}";
            }
            catch (System.Exception e)
            {
                return $"再开一层实验没跑成（{e.GetType().Name}，该项不作结论）";
            }
        }

        /// <summary>
        /// 把渲染器上的「渲染批合流」状态打出来。Blazor 在事件分派 / 渲染期间会给组件挂一个批作用域，
        /// 批没退到 0 之前 <c>StateHasChanged()</c> 只置「有待渲染」而不派发批，
        /// 于是表现成「模型两侧都清空了、DOM 却一动不动，怎么泵都不出来」——
        /// 这正是本仓那条负载偶发红的形状（见 <c>docs/message-box-alignment.md</c> 负载偶发段）。
        /// 字段名随运行时版本会变，所以这里按名字模式扫，扫不到就如实说「该项不作结论」。
        /// </summary>
        private string PeekRendererBatching()
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            try
            {
                var hits = new List<string>();
                for (var t = Renderer.GetType(); t is not null && t != typeof(object); t = t.BaseType)
                {
                    foreach (var f in t.GetFields(flags))
                    {
                        if (!f.Name.Contains("batch", StringComparison.OrdinalIgnoreCase)
                            && !f.Name.Contains("pending", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        object v;
                        try { v = f.GetValue(Renderer); }
                        catch (System.Exception e) { v = $"<读不到 {e.GetType().Name}>"; }
                        hits.Add($"{t.Name}.{f.Name}={Render(v)}");
                    }
                }
                return hits.Count == 0
                    ? "批状态：渲染器上没有 batch/pending 字段（该项不作结论）"
                    : "批状态 " + string.Join(" ", hits);
            }
            catch (System.Exception e)
            {
                return $"批状态：没扫成（{e.GetType().Name}，该项不作结论）";
            }

            static string Render(object v) => v switch
            {
                null => "null",
                System.Collections.ICollection c => $"[{c.Count}]",
                _ => v.ToString() ?? "?"
            };
        }

        /// <summary>
        /// 把渲染器里「已经发生但没人来取」的未处理异常翻出来。上面这个门是裸 <c>InvokeAsync</c> 泵，
        /// 不会触发 bUnit 在事件分派 / WaitFor 超时里做的未处理异常检查，所以渲染批里被吞掉的异常
        /// 只会表现成「模型状态已经清空、DOM 却没更新」这种假 stall。
        /// bUnit 2.7.2 里 <c>BunitRenderer.AssertNoUnhandledExceptions</c> 不是公开方法（公开的是
        /// <c>Renderer</c> 属性本身），所以只能反射调；这段只在超时路径上跑，任何取不到都只降级成
        /// 一句说明，不会把测试本身弄坏。
        /// </summary>
        private string PeekRendererFault()
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            try
            {
                var method = Renderer.GetType().GetMethod("AssertNoUnhandledExceptions", flags);
                if (method is null) return "渲染器：这个 bUnit 版本没找到未处理异常检查入口（该项不作结论）";
                try
                {
                    method.Invoke(Renderer, null);
                    return "渲染器：没有未处理异常 ⇒ 是「那一批渲染没发生」，不是「渲染报错被吞」";
                }
                catch (System.Reflection.TargetInvocationException tie)
                {
                    var inner = tie.InnerException ?? tie;
                    return $"渲染器：有未处理异常 ⇒ {inner.GetType().Name}: {inner.Message}";
                }
            }
            catch (System.Exception e)
            {
                return $"渲染器：未处理异常检查没跑成（{e.GetType().Name}，该项不作结论）";
            }
        }

        private string CloseScene(IRenderedComponent<ElPopup> popup)
        {
            var queue = Services.GetRequiredService<DialogService>().Dialogs;
            // 第二宿主是库自己写明会让浮层「既不会更新也关不掉」的路径（ElPopup.razor.cs:190-222：
            // 每个宿主初始化时把服务上的集合整个换成新集合，前一个宿主从此收不到任何集合变化，
            // 而新宿主自己的 DialogOptions 是空的）。若真是这样，超时现场就该正好是
            // 「本宿主 DialogOptions=0（新宿主）＋ DOM 还剩 1 个（老宿主留下的孤儿浮层）＋ 渲染器无异常 ＋ 没在合流批里」，
            // 所以宿主数必须打进现场；它同时是这条理论与「渲染根本没派发」两种解释的分叉点。
            // 读数口径要先讲清：`PopupHostCount` 只在宿主 OnInitialized 时 ++、Dispose 不 --（ElPopup.razor.cs:194），
            // 所以 >1 表示「本作用域内先后初始化过 ≥2 个宿主」，不直接等于「红这一刻同时活着两个」。
            var hostCount = Services.GetService<PopupService>()?.PopupHostCount ?? -1;
            // 前两种形状（合流批攒住、第二个宿主接管队列）已被常驻读数排除；第三种「渲染没落到 DOM」原先想换尺子量，
            // 但树根取证实测（Temp/b17-threadprobe.log [取证·Parent 链]）把这把尺子否掉了：这棵树从 wrapper 往上只有
            // HtmlDivElement < HtmlBodyElement，body 的 Parent 是 null —— 根是**游离的 body 元素**不是 IDocument，
            // 而且 popup.Nodes 就挂在这同一棵树上，所以「组件口径」与「树根口径」数的是同一棵树，分不出「节点表陈旧」与「DOM 里真有」。
            // 改用两组能分的读数：
            // ① 宿主根节点的型式+class 序列：一个 message-box 选项在根上占「wrapper + v-modal」两个元素
            //    （ElPopup.razor:240 与 :275-277）。残留是**成对的**＝这一项的移除编辑压根没落；只剩半个＝移除落了一半（帧错位）。
            // ② 残留元素与「开弹层那一刻」抓到的元素是否同一实例：同一＝从没被摘过；不同＝有一次渲染按非空模型又生成了它。
            //    实测本批第一条红给的是「否」，于是"残留是不是本宿主自己渲出来的"变成新的必答问题：
            //    `popup.FindAll` 与 `popup.Nodes` 是否同一棵树先前证过（同根），但**元素身份**还得单独核，
            //    所以这里同时打「残留在不在这份 Nodes 里」和「开层那份样本在不在这份 Nodes 里」两个成员判定。
            var wrappers = popup.FindAll(".el-message-box__wrapper");
            var nodesList = popup.Nodes.Cast<AngleSharp.Dom.INode>().ToList();
            var markupWrapperCount = CountMarkupWrappers(popup);

            return $"host.DialogOptions={popup.Instance.DialogOptions.Count} " +
                   $"dom.wrapper={wrappers.Count} markup.wrapper={markupWrapperCount} " +
                   $"nodes.count={nodesList.Count} " +
                   $"nodes序列={DescribeNodes(popup.Nodes)} " +
                   $"残留在本宿主Nodes里={(wrappers.Count == 0 ? "无从判定" : (nodesList.Contains(wrappers[0]) ? "是" : "否（FindAll 与 Nodes 不是同一份，尺子要先修）"))} " +
                   $"开层样本在本宿主Nodes里={(openedWrapper is null ? "没取到（该项不作结论）" : (nodesList.Contains(openedWrapper) ? "是（这一层从没被重建过）" : "否（开层那份已经不在树里＝被重建/摘除过）"))} " +
                   $"service.Dialogs={queue.Count} " +
                   $"queueId={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(queue)} " +
                   $"hostId={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(popup.Instance)} " +
                   $"popupHostCount={hostCount} " +
                   $"hostShadowCount={popup.Instance.ShadowCount} " +
                   $"开层时ShadowCount={(openedShadowCount?.ToString() ?? "没取到（该项不作结论）")} " +
                   $"RenderCount={popup.RenderCount} 开层时RenderCount={(openedRenderCount?.ToString() ?? "没取到（该项不作结论）")} " +
                   $"残留wrapper={Describe(wrappers)} " +
                   $"残留是否开层那一份={DescribeIdentity(wrappers)} " +
                   $"host内部状态={PeekComponentPending(popup.Instance)} " +
                   $"｜{PeekComponentRegistration(popup.Instance)}" +
                   $"{PeekRendererFrames(popup)}";
        }

        /// <summary>
        /// markup 口径数 wrapper。为什么判定必须走它（批次㉒ #117 钉死，见证 Temp/b22-117-witness.log 6/8 轮复现）：
        /// bUnit 2.7.2（v2.11.3 同款）<c>RenderedComponent.Nodes</c> 是 <c>latestRenderNodes ??= Parse(Markup)</c>
        /// 的缓存，而 <c>UpdateMarkup</c> 先清缓存、再慢慢 GetHtml、最后才写 markup —— 任何线程在这个窗口里
        /// 读 Nodes 会拿到旧 markup 的解析树并写回缓存，缓存非空就永不重解析；此后 markup（渲染器本帧输出，
        /// Volatile.Read）与 FindAll/Nodes（走缓存）永久分叉。 witness 实测：markup.wrapper=0 而 dom.wrapper=1，
        /// 渲染器侧读数与健康关闭完全一致。 markup 与解析树同源，分叉只可能是缓存脏，不可能是渲染没落地。
        /// </summary>
        private static int CountMarkupWrappers(IRenderedComponent<ElPopup> popup)
        {
            var markup = popup.Markup ?? string.Empty;
            var count = 0;
            for (var idx = markup.IndexOf("el-message-box__wrapper", StringComparison.Ordinal);
                 idx >= 0;
                 idx = markup.IndexOf("el-message-box__wrapper", idx + 1, StringComparison.Ordinal))
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// 判别力由临时探针的绿路对照给出（读数原文 `Temp/b17-threadprobe.log`）：打开时这两个 class 必须成对出现在序列里，
        /// 关闭后必须都不在 —— 没做过这条对照就不能拿「残留是成对的还是半个」当结论。
        /// </summary>
        internal static string DescribeNodes(AngleSharp.Dom.INodeList nodes)
        {
            try
            {
                return string.Join(" | ", nodes.Select(n => n is AngleSharp.Dom.IElement e
                    ? $"{e.TagName.ToLowerInvariant()}.{(e.GetAttribute("class") ?? "无class")}"
                    : $"{n.NodeType}"));
            }
            catch (System.Exception ex)
            {
                return $"没量成（{ex.GetType().Name}，该项不作结论）";
            }
        }

        /// <summary>
        /// 残留的 wrapper 是不是「开弹层那一刻」抓到的同一个实例。
        /// 前提（同一个 DOM 节点两次查询必须判「是」）由临时探针的型式取证验过（读数原文 `Temp/b17-threadprobe.log`）；
        /// 取不到样本时明说「不作结论」，不给出误导性的「否」。
        /// </summary>
        private string DescribeIdentity(System.Collections.Generic.IReadOnlyList<AngleSharp.Dom.IElement> wrappers)
        {
            if (wrappers.Count == 0)
            {
                return "没有残留 wrapper，无从比对";
            }

            if (openedWrapper is null)
            {
                return "开层时没抓到样本（该项不作结论）";
            }

            var same = ReferenceEquals(openedWrapper, wrappers[0]);
            return $"同一实例={(same ? "是（这一层从没被摘过）" : "否（残留是后来新生成的）")} " +
                   $"开层样本Id={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(openedWrapper)} " +
                   $"残留Id={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(wrappers[0])}";
        }

        /// <summary>
        /// 把残留的那一两个 wrapper 的 <c>aria-label</c> 与 <c>style</c> 打出来：
        /// 「关不掉的到底是不是刚关的那一层」只有这两项能分辨（同一个作用域里可能有多个浮层）。
        /// </summary>
        private static string Describe(System.Collections.Generic.IEnumerable<AngleSharp.Dom.IElement> wrappers)
        {
            var parts = wrappers.Select(w =>
                $"[label={w.GetAttribute("aria-label") ?? "-"} z={w.GetAttribute("style") ?? "-"}]");
            return string.Join(" ", parts);
        }

        /// <summary>
        /// 「模型是 0、DOM 还剩 1」的分叉实验：反射调 <c>ComponentBase.StateHasChanged</c> 强行给这个宿主排一次渲染，
        /// 再泵到 DOM 稳定（上限 40 轮，只在超时路径跑，不改任何断言）。
        /// 实测已经否掉「那次渲染请求没发出/没派发」这一支：强渲加 40 轮泵之后 wrapper 仍是 1。
        /// 剩下的用 <c>hostShadowCount</c> 分辨 —— 它是 <c>OnAfterRenderAsync</c> 第一行按当前模型算出来的
        /// （<c>ElPopup.razor.cs:755</c>），是「这次渲染到底跑完了没有」的证物：
        /// 强渲后 ShadowCount 变 0 而 wrapper 仍 1 ⇒ 渲染确实跑完了，是**渲染帧没把这一层从 DOM 摘掉**；
        /// 强渲后 ShadowCount 仍 1 ⇒ 这个组件的渲染根本没完成（`StateHasChanged` 落不到渲染器），换方向查。
        /// </summary>
        private async Task<string> ForceRenderSceneAsync(IRenderedComponent<ElPopup> popup)
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic;
            try
            {
                var stateHasChanged = typeof(ComponentBase).GetMethod("StateHasChanged", flags);
                if (stateHasChanged is null) return "强制渲染：ComponentBase 上没找到 StateHasChanged（该项不作结论）";
                await popup.InvokeAsync(() => stateHasChanged.Invoke(popup.Instance, Array.Empty<object>()));
                var rounds = 0;
                while (rounds < 40 && popup.FindAll(".el-message-box__wrapper").Count > 0)
                {
                    rounds++;
                    await popup.InvokeAsync(() => { });
                    await Task.Delay(25);
                }

                await popup.InvokeAsync(() => { });
                return $"强制渲染后（泵 {rounds} 轮）host.DialogOptions={popup.Instance.DialogOptions.Count} " +
                       $"dom.wrapper={popup.FindAll(".el-message-box__wrapper").Count} " +
                       $"nodes.count={popup.Nodes.Count()} " +
                       $"hostShadowCount={popup.Instance.ShadowCount} " +
                       $"host内部状态={PeekComponentPending(popup.Instance)}";
            }
            catch (System.Exception e)
            {
                return $"强制渲染：没跑成（{e.GetType().Name}，该项不作结论）";
            }
        }

        /// <summary>
        /// 组件侧的「待渲染 / 已释放」标记：<c>ComponentBase</c> 上的批父作用域与非公开 pending / disposed 字段。
        /// 和 <see cref="PeekRendererBatching"/> 是一对 —— 一个说「这台渲染器还开着几层批」，
        /// 一个说「这个组件的 StateHasChanged 是被合流掉了、根本没调，还是它已经disposed」。取不到只降级成说明。
        /// </summary>
        private static string PeekComponentPending(ElPopup popup)
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic;
            try
            {
                var hits = new List<string>();
                for (var t = popup.GetType(); t is not null && t != typeof(object); t = t.BaseType)
                {
                    foreach (var f in t.GetFields(flags))
                    {
                        if (!f.Name.Contains("batch", StringComparison.OrdinalIgnoreCase)
                            && !f.Name.Contains("pending", StringComparison.OrdinalIgnoreCase)
                            && !f.Name.Contains("rendered", StringComparison.OrdinalIgnoreCase)
                            && !f.Name.Contains("disposed", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        object v;
                        try { v = f.GetValue(popup); }
                        catch { continue; }
                        hits.Add($"{f.Name}={(v is null ? "null" : v is System.Collections.ICollection c ? $"[{c.Count}]" : v.ToString())}");
                    }
                }
                return hits.Count == 0 ? "没找到批/待渲染/释放字段（该项不作结论）" : string.Join(" ", hits);
            }
            catch (System.Exception e)
            {
                return $"没扫成（{e.GetType().Name}，该项不作结论）";
            }
        }

        /// <summary>
        /// 「强制渲染都渲不动」的两种解释分开量：
        /// ① 组件已经从渲染器上摘掉了（disposed / componentId 不再注册）⇒ 之后任何 <c>StateHasChanged</c> 都是空转，
        ///   这正好表现为「模型两侧都成 0、DOM 一动不动、怎么泵都不出来、渲染器无异常、批没开着」；
        /// ② 组件还注册着却渲不动 ⇒ 那是渲染帧 diff 或 bUnit 节点表的问题，不是生命周期。
        /// 读法：<c>componentId</c> 取自 <c>ComponentBase._renderHandle</c> 的 <c>_componentId</c>；
        /// 再把渲染器上所有 <c>IDictionary</c> 实例字段列出来，逐个查这个 id 在不在键里 ——
        /// 字段名随运行时版本会变，所以按类型扫不点名。取不到只降级成一句说明。
        /// </summary>
        private string PeekComponentRegistration(ElPopup popup)
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            try
            {
                var handleField = typeof(ComponentBase).GetField("_renderHandle", flags);
                if (handleField is null) return "生命周期：没找到 _renderHandle（该项不作结论）";
                var handle = handleField.GetValue(popup);
                if (handle is null) return "生命周期：_renderHandle 是 null（还没挂上渲染器）";
                var idField = handle.GetType().GetField("_componentId", flags);
                var id = idField?.GetValue(handle);
                var hits = new List<string>();
                for (var t = Renderer.GetType(); t is not null && t != typeof(object); t = t.BaseType)
                {
                    foreach (var f in t.GetFields(flags))
                    {
                        object table;
                        try { table = f.GetValue(Renderer); }
                        catch { continue; }
                        if (table is null) continue;
                        // 两种字典都要接住：非泛型 IDictionary（Dictionary<,> 实现它），
                        // 和只实现泛型 IDictionary<,> 的并发字典（ConcurrentDictionary 不实现非泛型那份）。
                        var keys = KeysOf(table);
                        if (keys is null) continue;
                        var contains = id is not null && keys.Contains(id);
                        var count = (table as System.Collections.ICollection)?.Count ?? -1;
                        hits.Add($"{t.Name}.{f.Name}=[{count}] 含{id}={(contains ? "是" : "否")}");
                    }
                }

                return $"生命周期 componentId={id} " + (hits.Count == 0
                    ? "渲染器上没有 id→组件 的字典字段（该项不作结论）"
                    : string.Join(" ", hits));
            }
            catch (System.Exception e)
            {
                return $"生命周期：没扫成（{e.GetType().Name}，该项不作结论）";
            }

            // 只接「键是 int（组件 id）」的表，别的字典打了没意义。
            // 键的类型从第一项实际取，不从属性元数据推 —— Dictionary<,>.Keys 的实现类型是嵌套结构，推不出来。
            // 空表要照样报出来（注册表空＝组件全被摘掉了，本身就是结论），所以这里返回空序列而不是 null。
            static IEnumerable<object> KeysOf(object table)
            {
                var raw = (table as System.Collections.IDictionary)?.Keys
                    ?? table.GetType().GetProperty("Keys")?.GetValue(table);
                if (raw is not System.Collections.IEnumerable enumerable) return null;
                foreach (var key in enumerable)
                {
                    return key is int ? enumerable.Cast<object>() : null;
                }

                return Array.Empty<object>();
            }
        }

        /// <summary>
        /// 打「渲染器自己记着的那份渲染帧」，与 DOM 逐格对照。红的那一刻的形状是「模型两侧都空了、DOM 却挂着旧层那一对」，
        /// 这一对读数是这条形状的最后一处含糊：
        ///   帧里的 wrapper=0 而 dom.wrapper=1 ⇒ 渲染器认为已经删掉了、DOM 没跟上＝编辑没应用到 DOM；
        ///   帧里的 wrapper=1 ⇒ 渲染器认为它该在＝"没删"发生在帧/模型这一侧，不是 DOM 侧。
        /// 内部字段名随运行时版本会变，所以按「元素类型为 RenderTreeFrame 的数组」扫，取不到只降级成一句说明。
        /// 按第 55 条，这两格都要先在成功路径上验它真的会变（开层＝1、关干净＝0）才能拿来当结论。
        /// </summary>
        // BL0006：这里就是为了读渲染器内部的帧才碰 RenderTree 类型的，只在超时现场跑一次；
        // 不压掉的话基线警告数会从 125 变成 129，猎取时的"警告数＝基线"这条可比性就废了。
#pragma warning disable BL0006
        private string PeekRendererFrames(IRenderedComponent<ElPopup> popup)
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            try
            {
                var handle = typeof(ComponentBase).GetField("_renderHandle", flags)?.GetValue(popup.Instance);
                if (handle is null) return "｜帧：_renderHandle 是 null（还没挂上渲染器，该项不作结论）";
                if (handle.GetType().GetField("_componentId", flags)?.GetValue(handle) is not int componentId)
                {
                    return "｜帧：没拿到 componentId（该项不作结论）";
                }

                // 实测第一版这里拿错了表：bUnit 自己也挂一张 int→东西 的字典，
                // 于是 state 取到了 bUnit 的 RenderedFragment（字段=renderer/instance/htmlParser/markup），
                // 那份对象里没有渲染帧。所以只认值型名字是 ComponentState 的表（渲染器内部那张 id→ComponentState）。
                object state = null;
                string stateTableName = null;
                var tableShapes = new List<string>();
                for (var t = Renderer.GetType(); t is not null && t != typeof(object) && state is null; t = t.BaseType)
                {
                    foreach (var f in t.GetFields(flags))
                    {
                        object table;
                        try { table = f.GetValue(Renderer); }
                        catch { continue; }
                        if (table is not System.Collections.IDictionary dict) continue;
                        if (!dict.Contains(componentId)) continue;
                        var candidate = dict[componentId];
                        // 先留下形状：这一格能不能当结论取决于值的真实型名，而型名随运行时版本会变。
                        tableShapes.Add($"{t.Name}.{f.Name}:{table.GetType().Name}:值={candidate?.GetType().FullName ?? "null"}");
                        if (!DerivesFromComponentState(candidate)) continue;
                        state = candidate;
                        stateTableName = f.Name;
                        break;
                    }
                }

                if (state is null)
                {
                    return $"｜帧：渲染器里没有一张表把 componentId={componentId} 映射到 ComponentState 的子类" +
                           $"（含该 id 的字典={string.Join(" ", tableShapes)}，该项不作结论）";
                }

                // 实测形状（Temp/b17-renderer-shape.log）：状态对象是 bUnit 的 RenderedComponent<T>（继承 ComponentState），
                // 帧挂在基类私有字段上，GetFields 不返回基类私有字段 —— 第一版在这里扫了个空。
                var found = FindRenderFrames(state, 3);
                if (found.Frames is null)
                {
                    var shape = string.Join(" ", TypeFields(state.GetType()).Select(f => $"{state.GetType().Name}.{f.Name}:{f.FieldType.Name}"));
                    return $"｜帧：组件状态（表={stateTableName} 型式={state.GetType().Name}）里没找到渲染帧数组（字段={shape}，该项不作结论）";
                }

                var frames = found.Frames;
                // 计数为 0 是合法读数（池化数组长度可以比在用的长，关掉之后在用的就是 0），
                // 所以只在**没读到计数字段**（-1）时才退回数组长度；用 `>0` 判断会把 0 格误当成 128 格。
                var used = found.Count >= 0 ? Math.Min(found.Count, frames.Length) : frames.Length;

                ScanFrames(frames, used, out var elementFrames, out var divFrames, out var wrapperFrames, out var modalFrames, out var labelsText);

                // 剩下那一问是「渲染器算 diff 用的基准树是哪一份」：红现场 `CurrentRenderTree` 报 0 格、
                // DOM 却挂着这一层，而「再开一层」那次是**就地换掉**残留（个数 1→1）——就地换只有在
                // 基准树里还有那一份时才成立。所以把「本状态 + 父状态链」每人自己的 Current/Next 两张帧表逐份打出来，
                // 谁带着 wrapper 谁就是候选基准树。
                // 深度只给 3、且沿 `ParentComponentState` 一跳一跳走，不做整图深走：实测 depth 6 的通用递归
                // 会走进出口对象图的 DOM 一侧，关干净那一面整轮扫直接抛 `ElementRemovedFromDomException`
                // （第一版就栽在这儿，读数原文 `Temp/b17-frameprobe3-deep.txt` 第 3-4 行）；depth 3 沿状态链走两态都能出数。
                string allText;
                try
                {
                    var parts = new List<string>();
                    var saidSources = new HashSet<string>(StringComparer.Ordinal);
                    var cur = state;
                    for (var hop = 0; hop <= 6 && cur is not null; hop++)
                    {
                        var tables = new List<(System.Array Frames, int Count, string Source)>();
                        CollectRenderFrames(cur, 3, tables, new HashSet<object>(), $"状态^{hop}");
                        foreach (var c in tables)
                        {
                            if (c.Frames is null || !saidSources.Add(c.Source)) continue;
                            var u = c.Count >= 0 ? Math.Min(c.Count, c.Frames.Length) : c.Frames.Length;
                            ScanFrames(c.Frames, u, out _, out _, out var w, out var m, out var lb);
                            parts.Add($"{c.Source} 已用={u}/{c.Frames.Length} wrapper={w} v-modal={m} label={lb}");
                        }

                        cur = ParentComponentStateOf(cur);
                    }

                    allText = parts.Count == 0 ? "没扫到任何帧表（该项不作结论）" : string.Join(" ; ", parts);
                }
                catch (System.Exception ex)
                {
                    allText = $"整轮扫描没跑成（{ex.GetType().Name}，该项不作结论）";
                }

                return $"｜帧 componentId={componentId} 帧来源={found.Source} 已用格={used}/{frames.Length} " +
                       $"候选帧容器=[{string.Join(" ", FrameCandidates(state, 2))}] " +
                       $"Element帧={elementFrames} div帧={divFrames} " +
                       $"帧里的wrapper={wrapperFrames} 帧里的v-modal={modalFrames} 帧里的aria-label={labelsText} " +
                       $"｜父链帧表（每人深度3）=[{allText}]";
            }
            catch (System.Exception e)
            {
                return $"｜帧：没读成（{e.GetType().Name}，该项不作结论）";
            }
        }
#pragma warning restore BL0006

        /// <summary>
        /// 数一份帧数组里的 Element/div/wrapper/v-modal 帧与 aria-label。
        /// RenderTreeFrame 的公开面**没有** `Attributes`（实测编译报 CS1061）：属性是紧跟在元素帧之后的
        /// `Attribute` 帧，所以这里按 FrameType 分派、逐格用反射读（型式身份在运行时可能有两份，见 FindRenderFrames 注释②）。
        /// </summary>
        private static void ScanFrames(
            System.Array frames,
            int used,
            out int elementFrames,
            out int divFrames,
            out int wrapperFrames,
            out int modalFrames,
            out string labelsText)
        {
            elementFrames = 0;
            divFrames = 0;
            wrapperFrames = 0;
            modalFrames = 0;
            var labels = new List<string>();
            for (var i = 0; i < used; i++)
            {
                var frame = frames.GetValue(i);
                var kind = FrameText(frame, "FrameType");
                if (kind == "Element")
                {
                    elementFrames++;
                    if (FrameText(frame, "ElementName") == "div") divFrames++;
                }
                else if (kind == "Attribute")
                {
                    var name = FrameText(frame, "AttributeName");
                    var value = FrameText(frame, "AttributeValue");
                    if (name == "class")
                    {
                        if (value.Contains("el-message-box__wrapper", StringComparison.Ordinal)) wrapperFrames++;
                        if (value.Contains("v-modal", StringComparison.Ordinal)) modalFrames++;
                    }
                    else if (name == "aria-label" && value.Length > 0 && labels.Count < 12)
                    {
                        labels.Add(value);
                    }
                }
            }

            labelsText = $"[{string.Join(" | ", labels)}]";
        }

        /// <summary>
        /// 取组件状态对象上的父状态（<code>ComponentState.ParentComponentState</code>，实测字段名就长这样）。
        /// 只按名字/型式名认，读不到回 null（这条只在超时现场用，取不到就少打一层，不影响主读数）。
        /// </summary>
        private static object ParentComponentStateOf(object state)
        {
            try
            {
                foreach (var f in TypeFields(state.GetType()))
                {
                    if (!f.Name.Contains("ParentComponentState", StringComparison.Ordinal)) continue;
                    if (!DerivesFromComponentState(f.GetValue(state))) continue;
                    return f.GetValue(state);
                }
            }
            catch
            {
                // 读不到就当没有父状态：这一节只是多打几层，不能让现场整条挂掉。
            }

            return null;
        }

        /// <summary>沿继承链判断这个对象是不是渲染器的组件状态（bUnit 换成自己的子类，型名不是 ComponentState，见 Temp/b17-renderer-shape.log）。</summary>
        private static bool DerivesFromComponentState(object candidate)
        {
            for (var t = candidate?.GetType(); t is not null && t != typeof(object); t = t.BaseType)
            {
                if (t.Name == "ComponentState") return true;
            }

            return false;
        }

        /// <summary>
        /// 逐层取继承链上的实例字段。私有字段**不在**派生类的 GetFields 结果里（实测就是这里踩的），
        /// 所以要自己从派生走到基类。
        /// </summary>
        private static IEnumerable<System.Reflection.FieldInfo> TypeFields(Type type)
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            for (var t = type; t is not null && t != typeof(object); t = t.BaseType)
            {
                foreach (var f in t.GetFields(flags))
                {
                    yield return f;
                }
            }
        }

        /// <summary>
        /// 找「渲染器自己给这个组件记着的那份当前树」。实测形状（Temp/b17-renderer-shape.log）与直觉有三处不同：
        /// ① 组件状态对象是 bUnit 的 RenderedComponent&lt;T&gt;（它**继承** ComponentState），
        ///    而 GetFields 不返回基类的私有字段，所以要沿继承链逐层扫；
        /// ② .NET 10 的 ComponentState 已经没有 `_renderFrames`：当前树在
        ///    `CurrentRenderTree._entries._items`，同对象上的 `_itemsInUse` 才是已用格数
        ///    （`_items` 是池化数组，可能比在用的长），另一份 `_nextRenderTree._entries._items` 是下一次渲染的暂存，
        ///    空闲时实测长度 0 —— 按「第一个非空」取就会拿错它；
        /// ③ 数组一律按 System.Array + 反射逐格读：实测 `is RenderTreeFrame[]` 对这些数组为假
        ///    （编译期的型式身份与运行时不是同一份），只有按元素型式名认才认得出。
        /// 所以这里点名要路径里含 CurrentRenderTree 的那一份，并把完整路径一起打进日志（帧来源=状态.CurrentRenderTree…），
        /// 红的那一刻要能自证读的是哪一份。
        /// </summary>
        private static (System.Array Frames, int Count, string Source) FindRenderFrames(object root, int depth)
        {
            var candidates = new List<(System.Array Frames, int Count, string Source)>();
            CollectRenderFrames(root, depth, candidates, new HashSet<object>(), "状态");
            var usable = candidates.Where(c => c.Frames is not null).ToList();
            if (usable.Count == 0) return (null, -1, null);

            var current = usable.FirstOrDefault(c => c.Source.Contains("CurrentRenderTree", StringComparison.Ordinal));
            if (current.Frames is not null) return current;
            var nonEmpty = usable.FirstOrDefault(c => c.Frames.Length > 0 || c.Count > 0);
            return nonEmpty.Frames is null ? usable[0] : nonEmpty;
        }

        /// <summary>按型式名认渲染帧数组（不靠编译期型式身份，见 FindRenderFrames 注释②）。</summary>
        private static bool IsFrameArray(object v, out System.Array frames)
        {
            frames = null;
            if (v is not Array array) return false;
            if (array.GetType().GetElementType()?.Name != "RenderTreeFrame") return false;
            frames = array;
            return true;
        }

        /// <summary>反射读一格帧的属性（逐格只在超时现场跑，不追求快）。</summary>
        private static string FrameText(object frame, string propertyName)
        {
            try
            {
                return frame?.GetType().GetProperty(propertyName)?.GetValue(frame)?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void CollectRenderFrames(
            object root,
            int depth,
            List<(System.Array Frames, int Count, string Source)> into,
            HashSet<object> seen,
            string path)
        {
            if (root is null || depth <= 0) return;
            // 只给引用类型去重：装箱出来的结构每次都是新对象，按引用比等于没去重，反过来用值相等又会误剪。
            if (!root.GetType().IsValueType && !seen.Add(root)) return;

            foreach (var f in TypeFields(root.GetType()))
            {
                object v;
                try { v = f.GetValue(root); }
                catch { continue; }
                if (v is null) continue;

                if (IsFrameArray(v, out var frames))
                {
                    // 同一个持有者上的 int 计数（实测是 `_itemsInUse`）就是已用格数：
                    // `_items` 是池化数组，比在用的长，不截就会把上一轮残帧数进来。
                    // 池状态要一起打：`已用格=0/0` 单看分不清「帧表真的空」与「池已归还、读到的是空壳」
                    // （实测绿路"关干净"也是 0/0，见 docs/testing-guards.md 第 61 条），`_disposed` 才是能分的那一位。
                    into.Add((frames, FindCountField(root, f.Name),
                        $"{path}.{f.Name}〔池已归还={PeekBool(root, "_disposed")} 池最小容量={PeekInt(root, "_minCapacity")}〕"));
                    continue;
                }

                if (!v.GetType().IsGenericType) continue;
                var generic = v.GetType().GetGenericTypeDefinition().Name;
                if (generic.StartsWith("ArrayBuilder", StringComparison.Ordinal)
                    && v.GetType().GetGenericArguments()[0].Name == "RenderTreeFrame")
                {
                    var inner = new List<(System.Array Frames, int Count, string Source)>();
                    CollectRenderFrames(v, depth - 1, inner, seen, $"{path}.{CleanFieldName(f.Name)}");
                    var data = inner.FirstOrDefault(c => c.Frames is not null);
                    // 计数直接沿用内层算好的那份（内层持有者就是那个数组池，字段名 `_items` 配 `_itemsInUse`）；
                    // 拿外层字段名（`_entries`）去配对是配不上的，只能退回数组长度，残帧就会混进来。
                    // 池状态要一起打：`已用格=0/0` 单看分不清「帧表真的空」与「池已归还、读到的是空壳」
                    // （实测绿路"关干净"也是 0/0，见 docs/testing-guards.md 第 61 条），`_disposed` 才是能分的那一位。
                    into.Add((data.Frames, data.Count,
                        $"{path}.{CleanFieldName(f.Name)}.{data.Source}" +
                        $"〔池已归还={PeekBool(v, "_disposed")} 池最小容量={PeekInt(v, "_minCapacity")}〕"));
                }
            }

            foreach (var f in TypeFields(root.GetType()))
            {
                object v;
                try { v = f.GetValue(root); }
                catch { continue; }
                if (v is null || v is string || v.GetType().IsPrimitive || v is Enum) continue;
                // 别往渲染器/服务/集合里走：那里没有帧，只会绕回这张状态表（循环）并把探查变成遍历整个对象图。
                var typeName = v.GetType().Name;
                if (v is System.Collections.IEnumerable) continue;
                if (typeName.Contains("Renderer", StringComparison.Ordinal)
                    || typeName.Contains("ServiceProvider", StringComparison.Ordinal)
                    || typeName.Contains("Dispatcher", StringComparison.Ordinal))
                {
                    continue;
                }

                CollectRenderFrames(v, depth - 1, into, seen, $"{path}.{CleanFieldName(f.Name)}");
            }
        }

        /// <summary>Backed 属性字段名长成 &lt;CurrentRenderTree&gt;k__BackingField，日志里只留本体名字。</summary>
        private static string CleanFieldName(string fieldName)
        {
            if (!fieldName.StartsWith("<", StringComparison.Ordinal)) return fieldName;
            var end = fieldName.IndexOf('>');
            return end > 1 ? fieldName[1..end] : fieldName;
        }

        /// <summary>
        /// 把「这个对象图里所有可能装渲染帧的容器」都列出来（名字:型式:长度）。
        /// 取到 0 格时靠这行判断是「拿错了容器」还是「这份真的空的」，两种解释的下一步完全不同。
        /// </summary>
        private static IEnumerable<string> FrameCandidates(object root, int depth)
        {
            if (root is null || depth <= 0) yield break;

            foreach (var f in TypeFields(root.GetType()))
            {
                object v;
                try { v = f.GetValue(root); }
                catch { continue; }
                if (v is null) continue;

                if (IsFrameArray(v, out var direct))
                {
                    yield return $"{root.GetType().Name}.{f.Name}:数组[{direct.Length}]";
                    continue;
                }

                var type = v.GetType();
                if (type.IsGenericType
                    && type.GetGenericTypeDefinition().Name.StartsWith("ArrayBuilder", StringComparison.Ordinal))
                {
                    var elementName = type.GetGenericArguments()[0].Name;
                    var innerLength = -1;
                    string innerArrayFieldName = null;
                    foreach (var inner in TypeFields(type))
                    {
                        object iv;
                        try { iv = inner.GetValue(v); }
                        catch { continue; }
                        if (IsFrameArray(iv, out var arr))
                        {
                            innerLength = arr.Length;
                            innerArrayFieldName = inner.Name;
                            break;
                        }
                    }

                    yield return $"{root.GetType().Name}.{f.Name}:ArrayBuilder<{elementName}>[内层数组={innerLength} 计数={(innerArrayFieldName is null ? -1 : FindCountField(v, innerArrayFieldName))}]";
                    continue;
                }

                if (v is string || type.IsPrimitive || v is Enum || v is System.Collections.IEnumerable) continue;
                var name = type.Name;
                if (name.Contains("Renderer", StringComparison.Ordinal)
                    || name.Contains("ServiceProvider", StringComparison.Ordinal)
                    || name.Contains("Dispatcher", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var nested in FrameCandidates(v, depth - 1))
                {
                    yield return $"{root.GetType().Name}.{f.Name}>{nested}";
                }
            }
        }

        /// <summary>
        /// 取持有者上的"已用格数"：优先取与那份数组同名的字段（实测是 `_items` 配 `_itemsInUse`），
        /// 再退到名字含 count/length/used 的 int 字段；都没有回 -1（调用侧改用数组长度）。
        /// </summary>
        private static int FindCountField(object holder, string arrayFieldName)
        {
            var fields = TypeFields(holder.GetType()).ToList();
            var pair = fields.FirstOrDefault(f => f.FieldType == typeof(int)
                && f.Name.StartsWith(arrayFieldName, StringComparison.Ordinal)
                && !string.Equals(f.Name, arrayFieldName, StringComparison.Ordinal));
            if (pair is not null)
            {
                try { return (int)pair.GetValue(holder); }
                catch { return -1; }
            }

            foreach (var f in fields)
            {
                if (f.FieldType != typeof(int)) continue;
                if (!f.Name.Contains("count", StringComparison.OrdinalIgnoreCase)
                    && !f.Name.Contains("length", StringComparison.OrdinalIgnoreCase)
                    && !f.Name.Contains("used", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try { return (int)f.GetValue(holder); }
                catch { return -1; }
            }

            return -1;
        }

        /// <summary>反射读一个 bool 字段，读不到就写「该项不作结论」（这条只在超时现场用，不追求快）。</summary>
        private static string PeekBool(object holder, string fieldName)
        {
            try
            {
                var f = TypeFields(holder.GetType()).FirstOrDefault(x => x.Name == fieldName && x.FieldType == typeof(bool));
                return f is null ? "该字段没找到" : (bool)f.GetValue(holder) ? "是" : "否";
            }
            catch (System.Exception e)
            {
                return $"读不到（{e.GetType().Name}）";
            }
        }

        /// <summary>反射读一个 int 字段，读不到回 -1。</summary>
        private static int PeekInt(object holder, string fieldName)
        {
            try
            {
                var f = TypeFields(holder.GetType()).FirstOrDefault(x => x.Name == fieldName && x.FieldType == typeof(int));
                return f is null ? -1 : (int)f.GetValue(holder);
            }
            catch
            {
                return -1;
            }
        }

        [Fact]
        public async Task MessageBoxPromptCreatesInputAndBeforeCloseCanBlock()
        {
            var messageBox = Services.GetRequiredService<MessageBox>();
            var popup = Render<ElPopup>();
            var attemptedActions = new List<MessageBoxAction>();
            Task<MessageBoxPromptResult> pendingBox = null;
            var option = new MessageBoxOption
            {
                Message = "Name",
                ShowInput = true,
                InputValue = "Alice",
                BeforeClose = action =>
                {
                    attemptedActions.Add(action);
                    return Task.FromResult(action == MessageBoxAction.Confirm);
                }
            };

            // 只发起、既不 await 也不 Wait：bUnit 的调度上下文只被 WaitFor* 系列泵动，
            // 在测试线程上等这个 Task 本身就可能不返回（并行全量时表现为整 20 秒超时）。
            // "弹层是否已经渲染出来"交给下面的 WaitForAssertion 去泵。
            // 这条老 flaky 的真身是"等待机制用错了"，一共三层，逐层写清楚：
            // ① `_ = popup.InvokeAsync(...)` 丢弃调度任务 → 工作项只是排队，高压下 lambda 根本不执行；
            // ② bUnit 的 WaitForAssertion **只在"下一次渲染"时重新求值**：渲染若发生在它挂上监听之前，
            //    它就永远不再检查（日志里 Check count: 1、render count: 1 就是这个），于是白等 20 秒；
            // ③ 点击后拿到的元素可能属于旧渲染批次，handler id 已失效 → 点击静默丢失。
            // 确定性做法：每步之后**排干调度队列**（再走一次 InvokeAsync 往返），然后直接断言，
            // 不依赖"下次渲染"；只有"结果 Task 完成"这种不引发渲染的条件才用 Task.WhenAny 等。
            await popup.InvokeAsync(() => { _ = pendingBox = messageBox.PromptAsync(option); });
            await WaitUntilAsync(popup, () => popup.FindAll(".el-message-box__btns button").Count > 0,
                "弹层没渲染出来（ElPopup 在 OnAfterRender 里二次渲染，需要多泵一轮）");

            Assert.Contains("Name", popup.Markup);
            Assert.Equal("Alice", popup.Find(".el-message-box__input input").GetAttribute("value"));

            // Find 与 Click 必须在同一次调度批次里：分开写时中间可能已经重渲染，
            // 拿到的旧元素 handler id 失效 → 点击静默丢失（本仓已记过这条，这次没照做才绕了这么久）
            await popup.InvokeAsync(() => popup.FindAll(".el-message-box__btns button").First().Click());
            await WaitUntilAsync(popup, () => attemptedActions.Contains(MessageBoxAction.Cancel),
                "点「取消」没送达 BeforeClose");
            Assert.Contains(MessageBoxAction.Cancel, attemptedActions);

            // 重渲染之后必须重新查询再点，否则点到旧批次的元素上
            await popup.InvokeAsync(() => popup.FindAll(".el-message-box__btns button").Last().Click());
            await WaitUntilAsync(popup, () => attemptedActions.Contains(MessageBoxAction.Confirm),
                "点「确定」没送达 BeforeClose");
            Assert.Contains(MessageBoxAction.Confirm, attemptedActions);

            var finished = await Task.WhenAny(pendingBox, Task.Delay(TestWait.Timeout)) == pendingBox;
            Assert.True(finished, "点了「确定」且 BeforeClose 已放行，但 PromptAsync 的结果 Task 没完成");

            var result = await pendingBox;
            Assert.Equal(MessageBoxResult.Ok, result.Result);
            Assert.Equal("Alice", result.Value);
        }

        /// <summary>
        /// 演示页 <c>/feedback</c>「Prompt 输入病历号」用的那条校验：空值就报错。
        /// </summary>
        private static Func<string, string> RequireRecordNumber => value =>
            string.IsNullOrWhiteSpace(value) ? "病历号不能为空" : null;

        private async Task<(IRenderedComponent<ElPopup> Popup, Task<MessageBoxPromptResult> Result)> OpenPromptAsync(MessageBoxOption option)
        {
            var messageBox = Services.GetRequiredService<MessageBox>();
            var popup = Render<ElPopup>();
            Task<MessageBoxPromptResult> pending = null;
            await popup.InvokeAsync(() => { pending = messageBox.PromptAsync(option); });
            await WaitUntilAsync(popup, () => popup.FindAll(".el-message-box__btns button").Count > 0,
                "Prompt 弹层没渲染出来");
            // 记下「开这一层时 DOM 里的那个元素对象」和当时的 ShadowCount，红时用来判残留是不是同一实例、
            // 以及「渲染跑完过」这条推断的前提（见 DescribeIdentity 与 openedShadowCount 的注释）。
            openedWrapper = popup.FindAll(".el-message-box__wrapper").FirstOrDefault();
            openedShadowCount = popup.Instance.ShadowCount;
            openedRenderCount = popup.RenderCount;
            return (popup, pending);
        }

        /// <summary>
        /// 等 <c>PromptAsync</c> 的结果 Task 完成，**用截止线而不是猜一个固定预算**。
        /// 这条链是「测试线程点按钮 → 调度器跑 handler → TCS 完成」，被断言的性质是
        /// 「这一下点下去会不会回结果」，不是「多快回结果」：实测把预算写死成 300ms/2s 时，
        /// 14 个满载进程 + 全量并行会让它自己先打不住（<c>Temp/b17-loaded-fullraw.log</c> RUN2
        /// 两条同档红，报的都是"没关"而不是"被拦住"），那属于给机器速度下断言，不是给契约下断言。
        /// 超时线取仓内统一的 <c>TestWait.Timeout</c>；真被校验拦住时这条等待会走满并给出下面的文案。
        /// </summary>
        private static async Task<(bool Settled, long ElapsedMs)> SettleAsync(Task<MessageBoxPromptResult> pending)
        {
            var sw = Stopwatch.StartNew();
            var settled = await Task.WhenAny(pending, Task.Delay(TestWait.Timeout)) == pending;
            return (settled, sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// 「点取消关不掉，必须先输入病历号」的真身：CloseAsync 里 InputValidator 不分 action，
        /// 取消 / × 也被校验拦住。EP 只在 <c>action === "confirm"</c> 时校验
        /// （<c>Temp/ep2140_messagebox_script.mjs:181</c>），× 走 <c>handleAction("close")</c>（同文件 218 行）。
        /// 这条钉取消侧：空值点「取消」必须真的关掉，且结果是 Cancel。
        /// </summary>
        [Fact]
        public async Task PromptCancelClosesEvenWhenInputValidatorRejects()
        {
            var option = new MessageBoxOption
            {
                Message = "请输入病历号",
                Title = "请输入病历号",
                ShowInput = true,
                InputValue = "",
                InputValidator = RequireRecordNumber
            };
            var (popup, pending) = await OpenPromptAsync(option);

            // 前提：此刻校验器确实是「拒绝空值」的，而且错误位还没被点亮（不是"已经报过错所以放行"）
            await popup.InvokeAsync(() =>
            {
                Assert.Equal("", popup.Find(".el-message-box__input input").GetAttribute("value"));
            });
            var errorMsg = popup.Find(".el-message-box__errormsg");
            Assert.Contains("visibility: hidden", errorMsg.GetAttribute("style") ?? "");

            await popup.InvokeAsync(() => popup.FindAll(".el-message-box__btns button").First().Click());

            var (settled, elapsed) = await SettleAsync(pending);
            Assert.True(settled, $"空值点「取消」被 InputValidator 拦住了 —— 弹层没关（这就是用户反馈的现象）。"
                + $"等 {elapsed}ms 结果 Task 仍未完成");
            var result = await pending;
            Assert.Equal(MessageBoxResult.Cancel, result.Result);
            // 库里的关闭是「先完成结果 Task，再 Task.Delay(100) 才从队列移除」（ElPopup.razor.cs
            // #DialogAnimationEndAsync），所以这里必须等，直接断言会读到还没撤掉的弹层。
            // 判据走 markup 口径：FindAll 走 bUnit Nodes 缓存，负载下有投毒竞态（见 CountMarkupWrappers）。
            await WaitUntilAsync(popup, () => CountMarkupWrappers(popup) == 0,
                "点了「取消」，结果已回传，但弹层一直没从 DOM 移除");
        }

        /// <summary>× 属「不提交这次输入」，同样不该被校验拦住（EP 的 close 分支）。</summary>
        [Fact]
        public async Task PromptHeaderCloseButtonIsNotBlockedByInputValidator()
        {
            var option = new MessageBoxOption
            {
                Message = "请输入病历号",
                ShowInput = true,
                InputValue = "",
                InputValidator = RequireRecordNumber
            };
            var (popup, pending) = await OpenPromptAsync(option);

            Assert.NotEmpty(popup.FindAll(".el-message-box__headerbtn"));
            await popup.InvokeAsync(() => popup.Find(".el-message-box__headerbtn").Click());

            var (settled, elapsed) = await SettleAsync(pending);
            Assert.True(settled, $"空值点 × 被 InputValidator 拦住了 —— 弹层没关。等 {elapsed}ms 结果 Task 仍未完成");
            // markup 口径判据（bUnit Nodes 缓存竞态见 CountMarkupWrappers）。
            await WaitUntilAsync(popup, () => CountMarkupWrappers(popup) == 0,
                "点了 ×，结果已回传，但弹层一直没从 DOM 移除");
        }

        /// <summary>
        /// 另一半：确定侧的校验必须**还在**（把校验整个删掉也能让上面两条绿，那才是过度修复）。
        /// 这里不用「Task 永不完成」当判据（那要挂死），而是钉三个可观测事实：
        /// 弹层仍在 DOM、错误文案已显示、错误位的 <c>visibility:hidden</c> 已被摘掉。
        /// </summary>
        [Fact]
        public async Task PromptConfirmWithInvalidInputStaysOpenAndShowsError()
        {
            var option = new MessageBoxOption
            {
                Message = "请输入病历号",
                ShowInput = true,
                InputValue = "",
                InputValidator = RequireRecordNumber
            };
            var (popup, pending) = await OpenPromptAsync(option);

            await popup.InvokeAsync(() => popup.FindAll(".el-message-box__btns button").Last().Click());
            await DrainAsync(popup);

            Assert.NotEmpty(popup.FindAll(".el-message-box__wrapper"));
            var errorMsg = popup.Find(".el-message-box__errormsg");
            Assert.Equal("病历号不能为空", errorMsg.TextContent.Trim());
            Assert.DoesNotContain("visibility: hidden", errorMsg.GetAttribute("style") ?? "");
            // 上面三条已经证明「这一下点下去了、校验器也确实拒了」（错误位被写脏、弹层还在）。
            // 这里只补一个**即时状态**：结果 Task 此刻没完成。不写成"300ms 内没完成"那种
            // 拿机器速度当下限的负向计时断言 —— 高压下它绿得没有意义（慢也算绿），
            // 而 IsCompleted 这一眼在任何负载下都只可能有一个真值。
            Assert.False(pending.IsCompleted, "空值点「确定」竟然放行了 —— InputValidator 没起作用");
        }

        /// <summary>
        /// 同一条 Prompt 的正向对照（与上一条同前提）：把值补对再点确定必须放行，
        /// 且 <c>Value</c> 回传的是输入的那串 —— 否则上一条的 <c>Assert.False(settled)</c>
        /// 只是把弹层钉死成"永远关不上"。
        /// </summary>
        [Fact]
        public async Task PromptConfirmAfterFixingInputClosesAndReturnsValue()
        {
            var option = new MessageBoxOption
            {
                Message = "请输入病历号",
                ShowInput = true,
                InputValue = "",
                InputValidator = RequireRecordNumber
            };
            var (popup, pending) = await OpenPromptAsync(option);

            // 先点确定被拦（前提），再改值点确定放行（结论）——两条放在同一台弹层上跑，
            // 保证「放行」真的是补值补出来的，不是本来就没校验。
            await popup.InvokeAsync(() => popup.FindAll(".el-message-box__btns button").Last().Click());
            await DrainAsync(popup);
            Assert.NotEmpty(popup.FindAll(".el-message-box__wrapper"));

            await popup.InvokeAsync(() => popup.Find(".el-message-box__input input").Change("EMR-2026-0001"));
            await DrainAsync(popup);
            await popup.InvokeAsync(() => popup.FindAll(".el-message-box__btns button").Last().Click());

            var (settled, elapsed) = await SettleAsync(pending);
            Assert.True(settled, $"补了合法病历号之后点「确定」仍然没关 —— 确定侧被修死了。等 {elapsed}ms 结果 Task 仍未完成");
            var result = await pending;
            Assert.Equal(MessageBoxResult.Ok, result.Result);
            Assert.Equal("EMR-2026-0001", result.Value);
            // markup 口径判据（bUnit Nodes 缓存竞态见 CountMarkupWrappers）。
            await WaitUntilAsync(popup, () => CountMarkupWrappers(popup) == 0,
                "补值后点了「确定」，结果已回传，但弹层一直没从 DOM 移除");
        }
    }
}
