namespace ElementPlus
{
    /// <summary>
    /// Element Plus ButtonConfigContext — 按钮全局配置
    /// </summary>
    public class ButtonConfigContext
    {
        /// <summary>
        /// 按钮类型 (primary/success/info/warning/danger/default)
        /// </summary>
        public string Type { get; set; } = "default";

        /// <summary>
        /// 两个中文字符之间自动插入空格（仅当文本长度为 2 且所有字符均为中文时生效）
        /// </summary>
        public bool AutoInsertSpace { get; set; } = false;

        /// <summary>
        /// 是否为朴素按钮
        /// </summary>
        public bool Plain { get; set; } = false;

        /// <summary>
        /// 是否为圆角按钮
        /// </summary>
        public bool Round { get; set; } = false;

        /// <summary>
        /// 是否为虚线按钮
        /// </summary>
        public bool Dashed { get; set; } = false;

        /// <summary>
        /// 是否为文字按钮
        /// </summary>
        public bool Text { get; set; } = false;

        public ButtonConfigContext Clone() => (ButtonConfigContext)MemberwiseClone();
    }
}