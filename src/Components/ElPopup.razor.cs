
using ElementPlus.Lang;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElPopup : ComponentBase, IDisposable
    {
        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        /// <summary>库级词条：弹层标题与关闭按钮名称在渲染时取词，切语言要能跟着换。</summary>
        [Inject]
        private ILang Lang { get; set; }

        private IDisposable langSubscription;

        /// <summary>
        /// 弹层标题：<b>只有 MessageBox 家族</b>才在留空时回落到词条 <c>el.messagebox.title</c>；
        /// 普通 DialogOption 的标题由宿主自己定，库不接管（否则会把别人的标题改成"提示"）。
        /// </summary>
        private string ResolveTitle(DialogOption option)
        {
            return option?.MessageBoxOption == null
                ? option?.Title
                : Lang.Text("el.messagebox.title", option.Title, "提示");
        }

        /// <summary>MessageBox 关闭按钮的可访问名称（此前完全没有；EP 用 t('el.messagebox.close')）。</summary>
        private string MessageBoxCloseAriaLabel => Lang.T("el.messagebox.close", "Close this dialog");

        [Inject]
        private DialogService DialogService { get; set; }

        [Inject]
        private MessageService MessageService { get; set; }

        [Inject]
        private NotificationService NotificationService { get; set; }

        [Inject]
        private LoadingService LoadingService { get; set; }

        [Inject]
        private PopupService PopupService { get; set; }

        internal int ShadowCount { get; set; }
        [Inject]
        private Document Document { get; set; }
        private static int ZIndex = 2000;

        /// <summary>
        /// 为整屏浮层（ElDialog / ElDialogGrow / ElDrawer）分配一对 z-index，
        /// 返回**遮罩层**的层级，容器层取「返回值 + 1」。
        /// 关键点：必须与 MessageBox / Notification / Message 等共用同一个计数器 ——
        /// 否则「先开对话框、再弹 before-close 确认框」时确认框会落在对话框之下点不到
        /// （历史上 ElDialog / ElDialogGrow / ElDrawer 各自维护一个从 2000 起步的独立计数器，
        /// 而 MessageBox 走的是本类的计数器，两边互不知情 → 层级会穿透）。
        /// </summary>
        internal static int AllocateOverlayZIndex()
        {
            return Interlocked.Add(ref ZIndex, 2) - 2;
        }
        internal List<MessageInfo> Messages { get; set; } = new List<MessageInfo>();
        internal List<NotificationOption> Notifications { get; set; } = new List<NotificationOption>();

        protected List<LoadingOption> LoadingOptions = new List<LoadingOption>();
        internal List<DialogOption> DialogOptions = new List<DialogOption>();
        internal List<DateTimePickerOption> DateTimePickerOptions = new List<DateTimePickerOption>();
        internal protected List<DropDownOption> SelectDropDownOptions = new List<DropDownOption>();
        internal protected List<DropDownOption> DropDownMenuOptions = new List<DropDownOption>();
        internal protected List<SubMenuOption> SubMenuOptions = new List<SubMenuOption>();
        internal List<PopupLayerOption> PopupOptions = new List<PopupLayerOption>();

        internal async Task CloseDialogAsync(DialogOption option, DialogResult result)
        {
            option.Result = result;
            if (option.Element == null)
            {
                await DialogAnimationEndAsync(option);
            }
            else
            {
                option.ShadowElement.Resume();
                option.Element.Resume();
            }
            await option.TaskCompletionSource.Task;
        }

        protected async Task CloseDialogOnModalClickAsync(DialogOption option)
        {
            if (!option.IsModal)
            {
                // 非模态层（如 Ribbon 菜单 / 下拉层）点击空白处即关闭，符合弹出菜单交互预期。
                if (!option.CloseOnClickModal)
                {
                    return;
                }
                await CloseDialogAsync(option, new DialogResult());
                return;
            }
            if (!option.CloseOnClickModal)
            {
                return;
            }
            if (option.MessageBoxOption != null)
            {
                await CloseMessageBoxAsync(option, option.MessageBoxOption.DistinguishCancelAndClose ? MessageBoxResult.Close : MessageBoxResult.Cancel, MessageBoxAction.Close);
                return;
            }
            await CloseDialogAsync(option, new DialogResult());
        }

        /// <summary>
        /// MessageBox 内层盒子的点击占位处理：配合 @onclick:stopPropagation 阻断冒泡，
        /// 只用来区分「点框内」与「点框外的遮罩层」，本身不做事。
        /// </summary>
        protected void StopBoxClickBubble()
        {
        }

        protected async Task OnDialogKeyDownAsync(DialogOption option, KeyboardEventArgs e)
        {
            if (!option.CloseOnPressEscape || e.Key != "Escape")
            {
                return;
            }
            if (option.MessageBoxOption != null)
            {
                await CloseMessageBoxAsync(option, option.MessageBoxOption.DistinguishCancelAndClose ? MessageBoxResult.Close : MessageBoxResult.Cancel, MessageBoxAction.Close);
                return;
            }
            await CloseDialogAsync(option, new DialogResult());
        }

        protected async Task CloseMessageBoxAsync(DialogOption option, MessageBoxResult result, MessageBoxAction action)
        {
            if (option.MessageBoxOption == null)
            {
                await CloseDialogAsync(option, new DialogResult { Result = result });
                return;
            }

            if (action == MessageBoxAction.Close && !option.MessageBoxOption.DistinguishCancelAndClose)
            {
                result = MessageBoxResult.Cancel;
            }

            await MessageBox.CloseAsync(option, option.MessageBoxOption, result, action);
        }

        private async Task OnPauseAsync(DialogOption option)
        {
            if (option.OnShow == null)
            {
                return;
            }
            await option.OnShow();
        }

        async Task DialogAnimationEndAsync(DialogOption option)
        {
            option.TaskCompletionSource.TrySetResult(option.Result);
            await Task.Delay(100);
            DialogService.Dialogs.Remove(option);
        }

        protected void SelectDay(DateTimePickerOption option, DateTime day)
        {
            option.CurrentMonth = day;
            option.TaskComplietionSource.SetResult(day);
            CloseDateTimePicker(option);
        }

        protected override void OnInitialized()
        {
            // 弹层里的文字来自应用级词条：ElPopup 不是 ElementComponentBase 的后代，订阅要自己接
            langSubscription?.Dispose();
            langSubscription = Lang.SubscribeLocaleChanged(() => InvokeAsync(StateHasChanged));

            // 一个作用域只允许挂一个宿主。下面这串「解订阅 → 换新集合 → 再订阅」就是"接管浮层队列"的动作，
            // 第二个宿主执行它时，前一个宿主从此收不到任何集合变化（已渲染的浮层关不掉，新浮层也不在它那里）。
            // 这件事静默了很多年（演示站的 LoginLayout 就挂了第二个宿主），所以这里把它打到控制台。
            // 计数挂在 PopupService（Scoped）上，因此不会跨作用域互相干扰。
            var hostCount = ++PopupService.PopupHostCount;
            if (hostCount > 1)
            {
                Console.WriteLine("[Element] <ElPopup /> 在同一作用域内被挂载了第 " + hostCount
                    + " 个：浮层队列现在由最后挂载的宿主接管，先前那个宿主上的浮层既不会更新也关不掉。"
                    + "浮层宿主全应用只应挂一次（放在 Router 之前），见 docs/popup-host-isolation.md。");
            }

            MessageService.Messages.CollectionChanged -= Messages_CollectionChanged;
            MessageService.Messages = new ObservableCollection<MessageInfo>();
            MessageService.Messages.CollectionChanged += Messages_CollectionChanged;

            MessageService.GroupingMerged -= OnMessageGroupingMerged;
            MessageService.GroupingMerged += OnMessageGroupingMerged;

            NotificationService.Notifications.CollectionChanged -= Notifications_CollectionChanged;
            NotificationService.Notifications = new ObservableCollection<NotificationOption>();
            NotificationService.Notifications.CollectionChanged += Notifications_CollectionChanged;

            NotificationService.UpdateOffsetsRequested -= OnNotificationUpdateOffsets;
            NotificationService.UpdateOffsetsRequested += OnNotificationUpdateOffsets;

            LoadingService.LoadingOptions.CollectionChanged -= LoadingOptions_CollectionChanged;
            LoadingService.LoadingOptions = new ObservableCollection<LoadingOption>();
            LoadingService.LoadingOptions.CollectionChanged += LoadingOptions_CollectionChanged;

            DialogService.Dialogs.CollectionChanged -= Dialogs_CollectionChanged;
            DialogService.Dialogs = new ObservableCollection<DialogOption>();
            DialogService.Dialogs.CollectionChanged += Dialogs_CollectionChanged;

            PopupService.DateTimePickerOptionsCore.CollectionChanged -= DateTimePickerOptions_CollectionChanged;
            PopupService.DateTimePickerOptionsCore = new ObservableCollection<DateTimePickerOption>();
            PopupService.DateTimePickerOptionsCore.CollectionChanged += DateTimePickerOptions_CollectionChanged;

            PopupService.SelectDropDownOptionsCore.CollectionChanged -= DropDownOptions_CollectionChanged;
            PopupService.SelectDropDownOptionsCore = new ObservableCollection<DropDownOption>();
            PopupService.SelectDropDownOptionsCore.CollectionChanged += DropDownOptions_CollectionChanged;

            PopupService.SubMenuOptionsCore.CollectionChanged -= SubMenuOptions_CollectionChanged;
            PopupService.SubMenuOptionsCore = new ObservableCollection<SubMenuOption>();
            PopupService.SubMenuOptionsCore.CollectionChanged += SubMenuOptions_CollectionChanged;

            PopupService.DropDownMenuOptionsCore.CollectionChanged -= DropDownMenuOptions_CollectionChanged;
            PopupService.DropDownMenuOptionsCore = new ObservableCollection<DropDownOption>();
            PopupService.DropDownMenuOptionsCore.CollectionChanged += DropDownMenuOptions_CollectionChanged;

            PopupService.PopupLayerOptionsCore.CollectionChanged -= PopupLayerOptions_CollectionChanged;
            PopupService.PopupLayerOptionsCore = new ObservableCollection<PopupLayerOption>();
            PopupService.PopupLayerOptionsCore.CollectionChanged += PopupLayerOptions_CollectionChanged;

            PopupService.DropDownTreeOptionsCore.CollectionChanged -= DropDownTreeOptions_CollectionChanged;
            PopupService.DropDownTreeOptionsCore = new ObservableCollection<PopupLayerOption>();
            PopupService.DropDownTreeOptionsCore.CollectionChanged += DropDownTreeOptions_CollectionChanged;
        }

        private void DropDownTreeOptions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var option = e.NewItems.OfType<DropDownOption>().FirstOrDefault();
                option.IsNew = true;
                option.Instance = this;
                option.RequestRender = () => InvokeAsync(StateHasChanged);
                option.ShadowZIndex = ZIndex++;
                option.ZIndex = ZIndex++;
                SelectDropDownOptions.Add(option);
                InvokeAsync(() =>
                {
                    StateHasChanged();
                });
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var option = e.OldItems.OfType<DropDownOption>().FirstOrDefault();
                SelectDropDownOptions.Remove(option);
                InvokeAsync(() =>
                {
                    StateHasChanged();
                });
            }
        }

        private void PopupLayerOptions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var option = e.NewItems.OfType<PopupLayerOption>().FirstOrDefault();
                option.IsNew = true;
                option.Instance = this;
                option.ShadowZIndex = ZIndex++;
                option.ZIndex = ZIndex++;
                PopupOptions.Add(option);
                InvokeAsync(() =>
                {
                    StateHasChanged();
                });
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var option = e.OldItems.OfType<PopupLayerOption>().FirstOrDefault();
                PopupOptions.Remove(option);
                InvokeAsync(() =>
                {
                    StateHasChanged();
                });
            }
        }

        private void DropDownMenuOptions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var option = e.NewItems.OfType<DropDownOption>().FirstOrDefault();
                option.IsNew = true;
                option.Instance = this;
                option.RequestRender = () => InvokeAsync(StateHasChanged);
                option.ShadowZIndex = ZIndex++;
                option.ZIndex = ZIndex++;
                DropDownMenuOptions.Add(option);
                InvokeAsync(() =>
                {
                    StateHasChanged();
                });
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var option = e.OldItems.OfType<DropDownOption>().FirstOrDefault();
                DropDownMenuOptions.Remove(option);
                InvokeAsync(() =>
                {
                    StateHasChanged();
                });
            }
        }

        private void SubMenuOptions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var option = e.NewItems.OfType<SubMenuOption>().FirstOrDefault();
                option.IsNew = true;
                option.Instance = this;
                option.ShadowZIndex = ZIndex++;
                option.ZIndex = ZIndex++;
                option.Close = CloseSubMenuAsync;
                SubMenuOptions.Add(option);
                InvokeAsync(StateHasChanged);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var option = e.OldItems.OfType<SubMenuOption>().FirstOrDefault();
                if (!SubMenuOptions.Remove(option))
                {
                    System.Diagnostics.Debug.WriteLine("SubMenuOptions_CollectionChanged Remove Failure");
                }
                InvokeAsync(StateHasChanged);
            }
        }

        private void DropDownOptions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var option = e.NewItems.OfType<DropDownOption>().FirstOrDefault();
                option.IsNew = true;
                option.Instance = this;
                option.RequestRender = () => InvokeAsync(StateHasChanged);
                option.ShadowZIndex = ZIndex++;
                option.ZIndex = ZIndex++;
                SelectDropDownOptions.Add(option);
                InvokeAsync(() =>
                {
                    StateHasChanged();
                });
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var option = e.OldItems.OfType<DropDownOption>().FirstOrDefault();
                SelectDropDownOptions.Remove(option);
                InvokeAsync(() =>
                {
                    StateHasChanged();
                });
            }
        }

        private void DateTimePickerOptions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var option = e.NewItems.OfType<DateTimePickerOption>().FirstOrDefault();
                option.IsNew = true;
                option.Instance = this;
                option.ShadowZIndex = ZIndex++;
                option.ZIndex = ZIndex++;
                DateTimePickerOptions.Add(option);
                InvokeAsync(() =>
                {
                    StateHasChanged();
                });
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var option = e.OldItems.OfType<DateTimePickerOption>().FirstOrDefault();
                DateTimePickerOptions.Remove(option);
                InvokeAsync(() =>
                {
                    StateHasChanged();
                });
            }
        }

        private void Dialogs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var option = e.NewItems.OfType<DialogOption>().FirstOrDefault();
                option.IsNew = true;
                option.Instance = this;
                option.ShadowZIndex = ZIndex++;
                option.ZIndex = option.CustomZIndex ?? ZIndex++;
                DialogOptions.Add(option);
                StateHasChanged();
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var option = e.OldItems.OfType<DialogOption>().FirstOrDefault();
                DialogOptions.Remove(option);
                StateHasChanged();
            }
        }

        protected void GoPrevYear(DateTimePickerOption option)
        {
            if (option.ShowYear > 0 && option.ShowYear > 1900)
            {
                option.ShowYear -= 10;
                return;
            }
            if (option.CurrentMonth.Year < 1900)
            {
                return;
            }
            option.CurrentMonth = option.CurrentMonth.AddYears(-1);
        }


        protected void GoNextYear(DateTimePickerOption option)
        {
            if (option.ShowYear > 0)
            {
                option.ShowYear += 10;
                return;
            }
            option.CurrentMonth = option.CurrentMonth.AddYears(1);
        }
        protected void OpenMonth(DateTimePickerOption option, int month)
        {
            option.ShowYear = option.ShowMonth = 0;
            option.CurrentMonth = new DateTime(option.CurrentMonth.Year, month, 1);
        }

        private void LoadingOptions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var loadingOption = e.NewItems.OfType<LoadingOption>().FirstOrDefault();
                loadingOption.IsNew = true;
                loadingOption.ZIndex = ZIndex++;
                LoadingOptions.Add(loadingOption);
                InvokeAsync(() =>
                {
                    StateHasChanged();
                });
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var loadingOption = e.OldItems.OfType<LoadingOption>().FirstOrDefault();
                LoadingOptions.Remove(loadingOption);
                if (string.IsNullOrWhiteSpace(loadingOption.Target.Id))
                {
                    _ = CloseFullScreenLoadingAsync(loadingOption);
                }
                else if (!LoadingOptions.Any(x => x.Target.Id == loadingOption.Target.Id))
                {
                    // 同一目标上没有别的遮罩了才撤定位上下文。EP 用 loading-number 属性计数，
                    // 本库的遮罩队列本来就按 target 去重（LoadingService.Show），拿队列当计数源等价。
                    _ = loadingOption.Target.Dom(JSRuntime).LoadingParentRelativeAsync(false);
                }
                InvokeAsync(() =>
                {
                    StateHasChanged();
                });
            }
        }

        // 提示堆叠间距：每条提示之间固定 60px（与入场动画的 BeginTop 偏移一致），
        // 整摞从首条的 Offset 处向下排，移除后由 RepositionMessagesAsync 平滑收拢。
        private const int MessageSlotGap = 60;

        private void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var message = e.NewItems.OfType<MessageInfo>().FirstOrDefault();
                message.IsNew = true;
                message.ZIndex = ZIndex++;
                lock (Messages)
                {
                    // 位置按在队列中的序号计算，而不是「堆在最后一条下面」——
                    // 这样无论旧提示是否已被移除，新提示都落在固定的槽位，
                    // 不再越弹越往下漂。垂直定位交给外层 .el-message-anchor，
                    // 它带 transition:top，重算 EndTop 后即可平滑收拢。
                    message.Index = Messages.Count;
                    message.EndTop = (Messages.Count == 0 ? message.Offset : Messages[0].Offset)
                        + message.Index * MessageSlotGap;
                    Messages.Add(message);
                }
                InvokeAsync(() =>
                {
                    StateHasChanged();
                });
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                // 从 MessageService 里被移除 = 这条提示已经结束（自动到期 / 被 Max 挤掉 /
                // 业务主动 CloseAsync），这里同步把它从弹层自己的列表里摘掉。
                // 旧实现要等「所有提示同时被移除」才动手，导致被挤掉的提示节点一直挂在
                // DOM 上（Duration=0 的那种会永远留在屏幕上）。
                foreach (var item in e.OldItems.OfType<MessageInfo>())
                {
                    lock (Messages)
                    {
                        Messages.Remove(item);
                    }
                }
                // 摘掉一条后，剩余提示整体向上收拢到新槽位（对齐 Element Plus）：
                // 重算每条的 EndTop，外层 anchor 的 transition:top 会让它们平滑上移。
                RecalculateMessageSlots();
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // MessageService.CloseAll() 走的是 Clear()，只发 Reset；
                // 不跟上的话弹层里那份列表（以及 DOM 节点）会原样留着，
                // 表现为「点了全部关闭，提示还在原地闪到自己的 Duration 结束」。
                lock (Messages)
                {
                    Messages.Clear();
                }
                InvokeAsync(() =>
                {
                    StateHasChanged();
                });
            }
        }

        /// <summary>
        /// 移除/新增后按当前队列顺序重算每条提示的序号与 EndTop。垂直定位由外层
        /// <c>.el-message-anchor</c> 承载（带 transition:top），重算后 StateHasChanged 触发
        /// 重渲染，剩余提示会平滑滑动到新槽位（向上收拢），避免「前面走了、坑位不回填、
        /// 整摞越漂越低」。不使用 JS 直接改 top，以免与 ElTransition 缓存的内联样式冲突。
        /// </summary>
        private void RecalculateMessageSlots()
        {
            if (Messages.Count == 0)
            {
                return;
            }
            var baseOffset = Messages[0].Offset;
            lock (Messages)
            {
                for (var i = 0; i < Messages.Count; i++)
                {
                    Messages[i].Index = i;
                    Messages[i].EndTop = baseOffset + i * MessageSlotGap;
                }
            }
            StateHasChanged();
        }

        private void Notifications_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var notification in e.NewItems.OfType<NotificationOption>())
                {
                    notification.IsNew = true;
                    // EP：zIndex 默认 0 = 由宿主自动分配；显式给了就用给的值
                    notification.ResolvedZIndex = notification.ZIndex > 0 ? notification.ZIndex : ZIndex++;
                    PositionNotification(notification);
                    Notifications.Add(notification);
                    if (notification.Duration > 0)
                    {
                        _ = AutoCloseNotificationAsync(notification);
                    }
                }

                InvokeAsync(StateHasChanged);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var notification in e.OldItems.OfType<NotificationOption>())
                {
                    Notifications.Remove(notification);
                }

                RepositionNotifications();
                InvokeAsync(StateHasChanged);
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                Notifications.Clear();
                InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// 自动关闭计时（对齐 EP 2.14.4 的 pauseOnHover / progress）：
        /// 每 100ms 走一拍，支持悬停暂停（把已走时长累进 <see cref="NotificationOption.ElapsedMs"/>、
        /// 离开时按剩余时长续跑），开启进度条时同步更新剩余百分比。
        /// 未开进度条时不触发重渲染，避免每 100ms 重绘整个弹层宿主。
        /// </summary>
        private async Task AutoCloseNotificationAsync(NotificationOption notification)
        {
            const int tickMs = 100;

            notification.ElapsedMs = 0;
            notification.ProgressPercent = 100;
            notification.Paused = false;
            notification.StartedAt = Environment.TickCount64;

            while (true)
            {
                await Task.Delay(tickMs);

                if (!Notifications.Contains(notification))
                {
                    return;
                }

                if (notification.Paused)
                {
                    continue;
                }

                var elapsed = notification.ElapsedMs + (Environment.TickCount64 - notification.StartedAt);
                if (elapsed >= notification.Duration)
                {
                    notification.ElapsedMs = notification.Duration;
                    await NotificationService.CloseAsync(notification);
                    return;
                }

                if (notification.ShowProgress)
                {
                    notification.ProgressPercent = 100 - (elapsed / (double)notification.Duration) * 100;
                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        /// <summary>点击通知（EP 的 <c>onClick</c>）；<see cref="NotificationOption.CloseOnClick"/> 时顺带关闭。</summary>
        protected async Task OnNotificationClickAsync(NotificationOption notification)
        {
            notification.OnClickSync?.Invoke();
            if (notification.OnClick != null)
            {
                await notification.OnClick();
            }

            if (notification.CloseOnClick)
            {
                await NotificationService.CloseAsync(notification);
            }
        }

        /// <summary>指针移入：pauseOnHover（默认 true）时暂停计时。</summary>
        protected void OnNotificationMouseEnter(NotificationOption notification)
        {
            if (!notification.PauseOnHover || notification.Paused)
            {
                return;
            }

            if (notification.StartedAt > 0)
            {
                notification.ElapsedMs += Environment.TickCount64 - notification.StartedAt;
            }

            notification.StartedAt = 0;
            notification.Paused = true;
        }

        /// <summary>指针移出：恢复计时（从剩余时长续跑）。</summary>
        protected void OnNotificationMouseLeave(NotificationOption notification)
        {
            if (!notification.PauseOnHover || !notification.Paused)
            {
                return;
            }

            notification.StartedAt = Environment.TickCount64;
            notification.Paused = false;
        }

        /// <summary>EP 2.10.5 的 <c>ElNotification.updateOffsets(position)</c>：只重排指定方向。</summary>
        private void OnNotificationUpdateOffsets(NotificationPosition position)
        {
            RepositionNotifications(position);
            InvokeAsync(StateHasChanged);
        }

        private void PositionNotification(NotificationOption notification)
        {
            var samePosition = Notifications.Where(x => x.Position == notification.Position).ToList();
            var offset = notification.Offset;
            foreach (var item in samePosition)
            {
                offset += 84;
            }

            if (notification.Position == NotificationPosition.TopLeft || notification.Position == NotificationPosition.TopRight)
            {
                notification.Top = offset;
            }
            else
            {
                notification.Bottom = offset;
            }
        }

        private void RepositionNotifications(NotificationPosition? only = null)
        {
            foreach (var group in Notifications.GroupBy(x => x.Position))
            {
                if (only.HasValue && group.Key != only.Value)
                {
                    continue;
                }

                var offset = group.First().Offset;
                foreach (var notification in group)
                {
                    if (notification.Position == NotificationPosition.TopLeft || notification.Position == NotificationPosition.TopRight)
                    {
                        notification.Top = offset;
                        notification.Bottom = 0;
                    }
                    else
                    {
                        notification.Bottom = offset;
                        notification.Top = 0;
                    }

                    offset += 84;
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            ShadowCount = DialogOptions.Count(x => x.IsModal);
            await RenderLoadingAsync();
            await RenderDateTimePickerAsync();
            await RenderDropDownAsync(SelectDropDownOptions, true);
            await RenderDropDownAsync(DropDownMenuOptions, false);
            await RenderSubMenuAsync();
        }

        private async Task RenderSubMenuAsync()
        {
            var option = SubMenuOptions.FirstOrDefault(x => x.IsNew);
            if (option == null)
            {
                return;
            }
            option.IsNew = false;
            var targetEl = option.Target.Dom(JSRuntime);
            var rect = await targetEl.GetBoundingClientRectAsync();
            var top = await targetEl.GetTopRelativeBodyAsync();
            var left = await targetEl.GetLeftRelativeBodyAsync();
            var documentWidth = await Document.GetClientWidthAsync();
            var documentHeight = await Document.GetClientHeightAsync();

            // EP sub-menu.ts currentPlacement：
            //   mode === 'horizontal' && isFirstLevel → 'bottom-start'（在触发项正下方展开）
            //   其余（嵌套层级 / vertical + collapse）→ 'right-start'（在触发项右侧展开）
            // 之前一律按 bottom-start 定位，导致嵌套子菜单的弹层落在触发项下方而不是右侧。
            var placement = ResolveSubMenuPlacement(option);
            option.Placement = placement;

            if (placement == "bottom-start")
            {
                option.Left = left;
                option.Top = top + rect.Height + option.Offset;
                if (option.Left + rect.Width > documentWidth)
                {
                    option.Left = Math.Max(10, documentWidth - rect.Width - 10);
                }
                if (rect.Bottom + 200 > documentHeight && rect.Top > 200)
                {
                    option.Top = Math.Max(10, top - 200 - option.Offset);
                    option.Placement = "top-start";
                }
            }
            else
            {
                option.Left = left + rect.Width + option.Offset;
                option.Top = top;
                var popupWidth = await option.Element.Dom(JSRuntime).GetClientWidthAsync();
                if (popupWidth > 0 && option.Left + popupWidth > documentWidth - 10)
                {
                    // 右侧空间不足：翻到触发项左侧（EP fallbackPlacements 含 left-start）
                    var flipped = left - popupWidth - option.Offset;
                    option.Left = flipped >= 10 ? flipped : Math.Max(10, documentWidth - popupWidth - 10);
                    option.Placement = "left-start";
                }
                // 弹层底部溢出视口时上移，避免子项被裁掉
                var popupHeight = await option.Element.Dom(JSRuntime).GetClientHeightAsync();
                if (popupHeight > 0 && option.Top + popupHeight > documentHeight - 10)
                {
                    option.Top = Math.Max(10, documentHeight - popupHeight - 10);
                }
            }

            option.IsShow = true;
            option.ShowStatus = AnimationStatus.Begin;
            StateHasChanged();
            await Task.Delay(10);
            option.ShowStatus = AnimationStatus.End;
            StateHasChanged();
        }

        /// <summary>
        /// EP <c>currentPlacement</c>：只有 horizontal 的一级子菜单向下展开（bottom-start），
        /// 其余（嵌套层级 / vertical + collapse）一律向右展开（right-start）。
        /// </summary>
        internal static string ResolveSubMenuPlacement(SubMenuOption option)
        {
            var subMenu = option?.SubMenu;
            var isBottom = subMenu != null
                && subMenu.IsFirstLevel
                && subMenu.TopMenu != null
                && subMenu.TopMenu.Mode == MenuMode.Horizontal;
            return isBottom ? "bottom-start" : "right-start";
        }
        private async Task RenderDropDownAsync(List<DropDownOption> dropDownOptions, bool autoWidth)
        {
            var option = dropDownOptions.FirstOrDefault(x => x.IsNew);
            if (option == null)
            {
                return;
            }
            option.IsNew = false;
            var targetEl = option.Target.Dom(JSRuntime);
            var rect = await targetEl.GetBoundingClientRectAsync();
            var top = await targetEl.GetTopRelativeBodyAsync();
            var left = await targetEl.GetLeftRelativeBodyAsync();
            option.Left = left;
            option.Top = top + rect.Height;
            if ((autoWidth && option.AutoWidth) || option.FitInputWidth)
            {
                option.WidthPx = rect.Width;
            }
            else if (option.WidthPx <= 0)
            {
                option.MinWidth = rect.Width;
            }
            var dropDownEl = option.Element.Dom(JSRuntime);
            var width = await dropDownEl.GetClientWidthAsync();
            var height = await dropDownEl.GetClientHeightAsync();
            var documentWidth = await Document.GetClientWidthAsync();
            var documentHeight = await Document.GetClientHeightAsync();
            if (option.Left + width >= documentWidth)
            {
                option.Left = documentWidth - width - 10;
            }
            if (option.Left < 0)
            {
                option.Left = 10;
            }
            if (rect.Bottom + height > documentHeight && rect.Top > height)
            {
                option.Top = top - height;
                option.Placement = "top-start";
                option.TransformOrigin = "center bottom 0px";
            }
            else
            {
                option.Placement = "bottom-start";
                option.TransformOrigin = "center top 0px";
            }
            option.IsShow = true;
            if (GlobalElementSettings.DisableAnimation)
            {
                StateHasChanged();
                return;
            }
            option.ShowStatus = AnimationStatus.Begin;
            StateHasChanged();
            await Task.Delay(10);
            option.ShowStatus = AnimationStatus.End;
            StateHasChanged();
        }

        internal async Task CloseDropDownAsync(DropDownOption option)
        {
            if (!option.IsShow)
            {
                return;
            }
            option.IsShow = false;
            if (GlobalElementSettings.DisableAnimation)
            {
                PopupService.SelectDropDownOptionsCore.Remove(option);
                PopupService.DropDownMenuOptionsCore.Remove(option);
                option.Refresh?.Invoke();
                if (option.OnClosed != null)
                {
                    await option.OnClosed();
                }
                return;
            }
            option.HideStatus = AnimationStatus.Begin;
            StateHasChanged();
            await Task.Delay(10);
            option.HideStatus = AnimationStatus.End;
            StateHasChanged();
            await Task.Delay(200);
            PopupService.SelectDropDownOptionsCore.Remove(option);
            PopupService.DropDownMenuOptionsCore.Remove(option);
            option.Refresh?.Invoke();
            if (option.OnClosed != null)
            {
                await option.OnClosed();
            }
        }

        internal async Task CloseSubMenuAsync(SubMenuOption option)
        {
            if (option?.SubMenu == null)
            {
                return;
            }
            var semaphore = option.SubMenu.TopMenu?.SemaphoreSlim;
            if (semaphore == null)
            {
                await CloseSubMenuCoreAsync(option);
                return;
            }
            await semaphore.WaitAsync();
            try
            {
                if (!option.IsShow)
                {
                    return;
                }
                // 先级联关闭后代弹层：嵌套子菜单的弹层不是父弹层的 DOM 子节点，
                // 不主动关闭就会留下孤儿弹层（父层已消失但它仍在页面上）。
                foreach (var descendant in SubMenuOptions
                             .Where(x => !ReferenceEquals(x, option) && IsDescendantOption(x, option))
                             .ToList())
                {
                    await CloseSubMenuCoreAsync(descendant, true);
                }

                await CloseSubMenuCoreAsync(option);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>关闭单个弹层（不再取信号量，供上面的级联关闭复用）。</summary>
        private async Task CloseSubMenuCoreAsync(SubMenuOption option, bool bestEffort = false)
        {
            if (!option.IsShow)
            {
                return;
            }
            option.IsShow = false;

            if (!GlobalElementSettings.DisableAnimation)
            {
                await InvokeAsync(StateHasChanged);
                await Task.Delay(200);
            }

            try
            {
                await InvokeAsync(option.SubMenu.DeActivate);
            }
            catch (Exception) when (bestEffort)
            {
                // 后代弹层可能已随父层卸载：忽略已释放组件上的展开态回调
            }

            CompleteSubMenuOption(option);
            PopupService.SubMenuOptionsCore.Remove(option);
        }

        /// <summary>释放「等待弹层关闭」的续体（<see cref="ElSubMenu.OnOverAsync"/> 正 await 它收尾）。</summary>
        private static void CompleteSubMenuOption(SubMenuOption option)
        {
            try
            {
                option.TaskCompletionSource?.TrySetResult(0);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        /// <summary>
        /// candidate 是否为 ancestor 的后代弹层：按 index 路径前缀判定
        /// （嵌套组件实例会随弹层重渲染而重建，所以不能用实例引用比较）。
        /// </summary>
        internal static bool IsDescendantOption(SubMenuOption candidate, SubMenuOption ancestor)
        {
            var ancestorPath = ancestor.SubMenu?.GetIndexPath();
            var candidatePath = candidate.SubMenu?.GetIndexPath();
            if (ancestorPath == null || candidatePath == null || candidatePath.Count <= ancestorPath.Count)
            {
                return false;
            }
            for (var i = 0; i < ancestorPath.Count; i++)
            {
                if (!string.Equals(candidatePath[i], ancestorPath[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
        }

        internal void KeepShowSubMenu(SubMenuOption option)
        {
            CancelTokenSource(option.ClosingTaskCancellationTokenSource);
        }

        /// <summary>鼠标进入某个弹层：逐级取消「自身 + 全部祖先」尚未到期的关闭计时。</summary>
        internal void KeepSelfAndAncestorsOpen(SubMenuOption option)
        {
            option?.SubMenu?.KeepSelfAndAncestorsOpen();
        }
        void CancelTokenSource(CancellationTokenSource cancellationTokenSource)
        {
            if (cancellationTokenSource == null)
            {
                return;
            }
            try
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }
                cancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {

            }
        }
        void DisposeTokenSource(CancellationTokenSource cancellationTokenSource)
        {
            if (cancellationTokenSource == null)
            {
                return;
            }
            try
            {
                cancellationTokenSource.Dispose();
            }
            catch (ObjectDisposedException)
            {

            }
        }

        internal async Task ReadyCloseSubMenuAsync(SubMenuOption option)
        {
            // 延迟时长遵循 EP 的 hide-timeout（默认 300ms），而不是写死 200ms
            var delay = option.HideTimeoutMs > 0 ? option.HideTimeoutMs : 200;
            option.ClosingTaskCancellationTokenSource = new System.Threading.CancellationTokenSource();
            var task = Task.Delay(delay, option.ClosingTaskCancellationTokenSource.Token).ContinueWith(async task =>
              {
                  if (task.IsCanceled)
                  {
                      DisposeTokenSource(option.ClosingTaskCancellationTokenSource);
                      return;
                  }
                  DisposeTokenSource(option.ClosingTaskCancellationTokenSource);
                  await CloseSubMenuAsync(option);
              });
            option.ClosingTask = await task;
        }

        async Task RenderDateTimePickerAsync()
        {
            var option = DateTimePickerOptions.FirstOrDefault(x => x.IsNew);
            if (option == null)
            {
                return;
            }
            option.IsNew = false;
            var targetEl = option.Target.Dom(JSRuntime);
            var rect = await targetEl.GetBoundingClientRectAsync();
            var top = await targetEl.GetTopRelativeBodyAsync();
            option.Left = rect.Left;
            option.Top = top + rect.Height;
            var style = option.Element.Dom(JSRuntime).Style;
            await style.SetAsync("left", $"{rect.Left}px");
            if (!GlobalElementSettings.DisableAnimation)
            {
                await style.SetAsync("top", $"{option.Top + 10}px");
                await style.ClearAsync("display");
                await Task.Delay(10);
            }
            else
            {
                await style.ClearAsync("display");
            }
            await style.SetAsync("top", $"{option.Top}px");
            await style.SetAsync("opacity", $"1");
        }

        /// <summary>
        /// 鼠标悬停：暂停该条提示的自动关闭计时（对齐 EP message 官方行为——
        /// 悬停时清除关闭定时，移开后继续）。Duration=0 本就不自动关，无需处理。
        /// </summary>
        private void PauseMessageTiming(MessageInfo message)
        {
            if (message.Duration > 0)
            {
                message.TransitionRef?.PauseTiming();
            }
        }

        /// <summary>鼠标移开：继续被悬停暂停的自动关闭计时。</summary>
        private void ResumeMessageTiming(MessageInfo message)
        {
            if (message.Duration > 0)
            {
                message.TransitionRef?.ResumeTiming();
            }
        }

        private void DisposeMessage(MessageInfo item)
        {
            item.IsNew = false;
            lock (MessageService.Messages)
            {
                MessageService.Messages.Remove(item);
            }
            _ = item.OnClose?.Invoke();
        }

        /// <summary>
        /// 分组提示重复出现时（只涨 RepeatCount，集合没动），把计数徽标刷出来。
        /// </summary>
        private void OnMessageGroupingMerged(MessageInfo message)
        {
            InvokeAsync(() =>
            {
                StateHasChanged();
            });
        }

        /// <summary>
        /// 提示动画收尾。Duration=0 的提示只带一段「淡入」Path，动画结束得比正常提示早得多，
        /// 不能当成「到期该关了」——那种提示必须等用户点 × / 业务调 CloseAsync 才走。
        /// </summary>
        private void OnMessageTransitionEnd(MessageInfo message)
        {
            if (message.Duration > 0)
            {
                DisposeMessage(message);
            }
        }

        protected async Task CloseMessageAsync(MessageInfo item)
        {
            if (Messages.Contains(item))
            {
                Messages.Remove(item);
                StateHasChanged();
            }

            await MessageService.CloseAsync(item);
        }

        protected Task CloseNotificationAsync(NotificationOption item)
        {
            return NotificationService.CloseAsync(item);
        }

        protected void CloseDateTimePicker(DateTimePickerOption option)
        {
            PopupService.DateTimePickerOptionsCore.Remove(option);
        }

        async Task ShowFullScreenLoadingAsync(LoadingOption option)
        {
            await Document.AppendAsync(option.Element);
            await Document.DisableScrollAsync();
            var style = option.Element.Dom(JSRuntime).Style;
            await style.SetAsync("display", "none");
            await style.SetAsync("width", "100%");
            await style.SetAsync("height", "100%");
            await style.SetAsync("position", "fixed");
            await style.SetAsync("top", "0");
            await style.SetAsync("bottom", "0");
            await style.SetAsync("display", "block");
        }


        async Task CloseFullScreenLoadingAsync(LoadingOption option)
        {
            await Document.EnableYScrollAsync();
        }
        async Task RenderLoadingAsync()
        {
            var option = LoadingOptions.FirstOrDefault(x => x.IsNew);
            if (option == null)
            {
                return;
            }
            option.IsNew = false;
            if (string.IsNullOrWhiteSpace(option.Target.Id))
            {
                await ShowFullScreenLoadingAsync(option);
                return;
            }
            await option.Target.Dom(JSRuntime).AppendAsync(option.Element);
            // .el-loading-mask 是 position:absolute（EP 原样），定位上下文必须由「被挂遮罩的元素」
            // 提供。目标若是 position:static（演示里 .demo-loading-box 就没有任何 CSS 定义），
            // 绝对定位会一路向上相对初始包含块铺开 —— 表现就是「区域加载变成了全屏 loading」。
            // EP 的做法是在挂遮罩时给父级补 el-loading-parent--relative，这里同口径。
            await option.Target.Dom(JSRuntime).LoadingParentRelativeAsync(true);
            await option.Element.Dom(JSRuntime).Style.SetAsync("display", "block");
        }

        /// <summary>退订词条变更；重复 Dispose 是 no-op（句柄自身保证）。</summary>
        public void Dispose()
        {
            langSubscription?.Dispose();
            langSubscription = null;
        }
    }
}
