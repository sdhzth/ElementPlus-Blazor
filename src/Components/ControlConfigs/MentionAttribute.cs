namespace ElementPlus.ControlConfigs
{
    using System;

    public class MentionAttribute : BaseAttribute
    {
        public string Placeholder { get; set; }

        public string Prefix { get; set; } = "@";

        public string[] Prefixes { get; set; }

        public int Rows { get; set; } = 2;

        public string Autocomplete { get; set; } = "off";

        public int Tabindex { get; set; } = 0;

        public string AriaLabel { get; set; }

        public string InputStyle { get; set; }

        public bool IsDisabled { get; set; }

        public bool Readonly { get; set; }

        public InputSize Size { get; set; } = InputSize.Normal;

        public bool Clearable { get; set; }

        /// <summary>
        /// 候选刷新防抖毫秒数，透传给 <see cref="ElMention.Debounce"/>。
        /// 默认 300 与组件一致；即时联想（或自动化测试）设 0 可让下拉候选随输入同步刷新。
        /// </summary>
        public int Debounce { get; set; } = 300;

        public string Display { get; set; } = "Label";

        public string Value { get; set; } = "Value";

        public Type DataSourceLoader { get; set; }
    }
}
