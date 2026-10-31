using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public class NotificationService
    {
        internal ObservableCollection<NotificationOption> Notifications { get; set; }

        /// <summary>最大同时显示数量（超出时自动淘汰最早的）</summary>
        public int Max { get; set; } = 4;

        public NotificationService()
            : this(null)
        {
        }

        internal NotificationService(NotificationService inner)
        {
            Notifications = inner?.Notifications ?? new ObservableCollection<NotificationOption>();
        }

        #region 便捷方法

        public NotificationOption Show(string message) => Show(new NotificationOption { Message = message });

        public NotificationOption Show(string title, string message) => Show(new NotificationOption { Title = title, Message = message });

        public NotificationOption Show(string title, string message, MessageType type) => Show(new NotificationOption { Title = title, Message = message, Type = type });

        public NotificationOption Success(string title, string message) => Show(title, message, MessageType.Success);

        public NotificationOption Warning(string title, string message) => Show(title, message, MessageType.Warning);

        public NotificationOption Error(string title, string message) => Show(title, message, MessageType.Error);

        public NotificationOption Info(string title, string message) => Show(title, message, MessageType.Info);

        /// <summary>EP 2.9.11 新增的第五种类型（primary，蓝色系）。</summary>
        public NotificationOption Primary(string title, string message) => Show(title, message, MessageType.Primary);

        #endregion

        /// <summary>
        /// 请求宿主重新计算某个方向上所有通知的偏移量（对应 EP 2.10.5 的 <c>ElNotification.updateOffsets(position)</c>）。
        /// 宿主（ElPopup）订阅后重排；未挂载宿主时为空操作。
        /// </summary>
        public void UpdateOffsets(NotificationPosition position)
        {
            UpdateOffsetsRequested?.Invoke(position);
        }

        /// <summary>宿主重排钩子，由 ElPopup 在初始化时挂载。</summary>
        internal event Action<NotificationPosition> UpdateOffsetsRequested;

        public NotificationOption Show(NotificationOption option)
        {
            if (option == null) throw new ArgumentNullException(nameof(option));

            if (option.Grouping)
            {
                var existing = Notifications.FirstOrDefault(x =>
                    x.Grouping && x.Type == option.Type &&
                    x.Title == option.Title && x.Message == option.Message);
                if (existing != null)
                {
                    existing.RepeatCount++;
                    return existing;
                }
            }

            // Max 限制：超出时淘汰最早的一条。
            // 注意必须真的从集合里 Remove —— 只调 CloseCore 的话集合长度不变，
            // while 条件恒成立 → 同步死循环，直接卡死 Blazor 的 UI 线程（页面无响应）。
            while (Notifications.Count >= Max)
            {
                var oldest = Notifications[0];
                Notifications.RemoveAt(0);
                oldest.OnCloseSync?.Invoke();
                if (oldest.OnClose != null)
                {
                    _ = oldest.OnClose();
                }
            }

            Notifications.Add(option);
            return option;
        }

        public async Task CloseAsync(NotificationOption option)
        {
            if (option == null || !Notifications.Contains(option)) return;
            Notifications.Remove(option);
            option.OnCloseSync?.Invoke();
            if (option.OnClose != null) await option.OnClose();
        }

        public void CloseAll()
        {
            foreach (var n in Notifications.ToList()) CloseCore(n);
            Notifications.Clear();
        }

        private void CloseCore(NotificationOption option)
        {
            option.OnCloseSync?.Invoke();
            try { option.OnClose?.Invoke(); } catch { }
        }
    }
}