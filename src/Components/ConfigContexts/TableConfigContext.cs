namespace ElementPlus
{
    /// <summary>
    /// Element Plus TableConfigContext — Table 全局配置 (2.13.3)
    /// </summary>
    public class TableConfigContext
    {
        /// <summary>
        /// Table 布局方式 (fixed/auto)
        /// </summary>
        public string Layout { get; set; } = "fixed";

        /// <summary>
        /// 是否在 hover 时高亮行
        /// </summary>
        public bool Hover { get; set; } = true;

        public TableConfigContext Clone() => (TableConfigContext)MemberwiseClone();
    }
}