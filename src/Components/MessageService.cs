using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public class MessageService
    {
        internal ObservableCollection<MessageInfo> Messages = new ObservableCollection<MessageInfo>();

        /// <summary>
        /// 分组提示被合并时触发。合并只改 RepeatCount，既没有新增也没有移除，
        /// 集合事件不会发，宿主（ElPopup）必须靠这个通知才会把计数值刷出来。
        /// </summary>
        internal event Action<MessageInfo> GroupingMerged;

        /// <summary>最大同时显示数量（超出时自动淘汰最早的）</summary>
        public int Max { get; set; } = 3;

        #region 便捷方法

        public MessageInfo Show(string text) => Show(new MessageInfo { Message = text });

        public MessageInfo Show(string text, MessageType type) => Show(new MessageInfo { Message = text, Type = type });

        public MessageInfo Show(string text, MessageType type, int duration) => Show(new MessageInfo { Message = text, Type = type, Duration = duration });

        /// <summary>快捷：成功</summary>
        public MessageInfo Success(string text) => Show(text, MessageType.Success);

        /// <summary>快捷：警告</summary>
        public MessageInfo Warning(string text) => Show(text, MessageType.Warning);

        /// <summary>快捷：错误</summary>
        public MessageInfo Error(string text) => Show(text, MessageType.Error);

        /// <summary>快捷：信息</summary>
        public MessageInfo Info(string text) => Show(text, MessageType.Info);

        /// <summary>快捷：主要（EP 2.9+ 第五种类型，蓝色系）</summary>
        public MessageInfo Primary(string text) => Show(text, MessageType.Primary);

        #endregion

        public MessageInfo Show(MessageInfo info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));

            // 全局默认值
            if (info.Duration <= 0 && info.Duration != 0) info.Duration = 3000;

            if (info.Grouping)
            {
                var existing = Messages.FirstOrDefault(x => x.Grouping && x.Type == info.Type && x.Message == info.Message);
                if (existing != null)
                {
                    existing.RepeatCount++;
                    GroupingMerged?.Invoke(existing);
                    return existing;
                }
            }

            // Max 限制：超出时淘汰最早的一条。
            // 注意必须真的从集合里 Remove —— 只调 CloseCore 的话集合长度不变，
            // 「最多同时 N 条」不成立，弹层还会把多的那几条一起画出来。
            while (Messages.Count >= Max)
            {
                var oldest = Messages[0];
                Messages.RemoveAt(0);
                oldest.OnCloseSync?.Invoke();
                if (oldest.OnClose != null)
                {
                    _ = oldest.OnClose();
                }
            }

            Messages.Add(info);
            return info;
        }

        public Task CloseAsync(MessageInfo info)
        {
            if (info == null || !Messages.Contains(info)) return Task.CompletedTask;
            Messages.Remove(info);
            info.OnCloseSync?.Invoke();
            return info.OnClose == null ? Task.CompletedTask : info.OnClose();
        }

        public void CloseAll()
        {
            foreach (var m in Messages.ToList()) CloseCore(m);
            Messages.Clear();
        }

        private void CloseCore(MessageInfo info)
        {
            info.OnCloseSync?.Invoke();
            try { info.OnClose?.Invoke(); } catch { }
        }
    }
}