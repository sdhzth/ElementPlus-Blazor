namespace ElementPlus
{
    /// <summary>
    /// Element Plus DialogConfigContext — Dialog 全局配置
    /// </summary>
    public class DialogConfigContext
    {
        /// <summary>
        /// 是否允许点击遮罩层关闭
        /// </summary>
        public bool CloseOnClickModal { get; set; } = true;

        /// <summary>
        /// 是否允许按 ESC 关闭
        /// </summary>
        public bool CloseOnPressEscape { get; set; } = true;

        /// <summary>
        /// 是否显示关闭按钮
        /// </summary>
        public bool ShowClose { get; set; } = true;

        public DialogConfigContext Clone() => (DialogConfigContext)MemberwiseClone();
    }
}