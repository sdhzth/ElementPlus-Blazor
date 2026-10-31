namespace ElementPlus
{
    /// <summary>
    /// Element Plus MessageConfigContext — Message 全局配置
    /// </summary>
    public class MessageConfigContext
    {
        /// <summary>
        /// 同时显示的消息最大数量
        /// </summary>
        public int Max { get; set; } = 3;

        /// <summary>
        /// 默认持续时间（毫秒），0 表示不自动关闭
        /// </summary>
        public int Duration { get; set; } = 3000;

        /// <summary>
        /// 是否启用分组（相同内容只显示一个）
        /// </summary>
        public bool Grouping { get; set; } = false;

        public MessageConfigContext Clone() => (MessageConfigContext)MemberwiseClone();
    }
}