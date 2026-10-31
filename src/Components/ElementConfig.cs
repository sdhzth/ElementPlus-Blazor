using System.Globalization;

namespace ElementPlus
{
    public class ElementConfig
    {
        public const string DefaultNamespace = "el";
        public const int DefaultZIndex = 2000;

        public ElementSize Size { get; set; } = ElementSize.Default;

        public string Namespace { get; set; } = DefaultNamespace;

        public int ZIndex { get; set; } = DefaultZIndex;

        public string Locale { get; set; } = CultureInfo.CurrentUICulture.Name;

        // ===== 子配置（ConfigContexts） =====

        /// <summary>Button 全局配置</summary>
        public ButtonConfigContext Button { get; set; } = new();

        /// <summary>Link 全局配置</summary>
        public LinkConfigContext Link { get; set; } = new();

        /// <summary>Dialog 全局配置</summary>
        public DialogConfigContext Dialog { get; set; } = new();

        /// <summary>Message 全局配置</summary>
        public MessageConfigContext Message { get; set; } = new();

        /// <summary>Table 全局配置 (2.13.3)</summary>
        public TableConfigContext Table { get; set; } = new();

        public ElementConfig Clone()
        {
            return new ElementConfig
            {
                Size = Size,
                Namespace = Namespace,
                ZIndex = ZIndex,
                Locale = Locale,
                Button = Button?.Clone() ?? new(),
                Link = Link?.Clone() ?? new(),
                Dialog = Dialog?.Clone() ?? new(),
                Message = Message?.Clone() ?? new(),
                Table = Table?.Clone() ?? new()
            };
        }
    }
}