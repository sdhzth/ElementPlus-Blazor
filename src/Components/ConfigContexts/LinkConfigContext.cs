namespace ElementPlus
{
    /// <summary>
    /// Element Plus LinkConfigContext — 链接全局配置
    /// </summary>
    public class LinkConfigContext
    {
        /// <summary>
        /// 链接类型 (primary/success/info/warning/danger/default)
        /// </summary>
        public string Type { get; set; } = "primary";

        /// <summary>
        /// 下划线显示策略 (always/never/hover)
        /// </summary>
        public string Underline { get; set; } = "always";

        public LinkConfigContext Clone() => (LinkConfigContext)MemberwiseClone();
    }
}