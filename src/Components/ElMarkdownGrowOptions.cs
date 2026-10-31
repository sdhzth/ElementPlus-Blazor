using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// <see cref="ElMarkdownGrow"/> 的解析与渲染选项。
    /// 与参考实现（Radzen 的 <c>BlazorMarkdownRendererOptions</c>）对齐，并补充若干自渲染场景下的开关。
    /// </summary>
    public class ElMarkdownGrowOptions
    {
        /// <summary>
        /// Markdown 中内嵌的 HTML 标签是否按 HTML 渲染。
        /// <para><c>true</c>（默认）：允许白名单内的安全标签，危险标签（script/iframe/object/style 等）连同内容一并剔除。</para>
        /// <para><c>false</c>：所有 HTML 一律作为纯文本显示（转义后原样输出）。</para>
        /// </summary>
        public bool AllowHtml { get; set; } = true;

        /// <summary>
        /// 允许的 HTML 标签白名单。为 <c>null</c> 时使用 <see cref="ElMarkdownGrowSanitizer.DefaultAllowedTags"/>。
        /// </summary>
        public IEnumerable<string> AllowedHtmlTags { get; set; }

        /// <summary>
        /// 允许的 HTML 属性白名单。为 <c>null</c> 时使用 <see cref="ElMarkdownGrowSanitizer.DefaultAllowedAttributes"/>。
        /// </summary>
        public IEnumerable<string> AllowedHtmlAttributes { get; set; }

        /// <summary>
        /// 自动为标题生成锚点的最大标题层级（0 表示关闭）。
        /// 例如为 3 时，h1/h2/h3 会带 <c>id</c> 与可点击锚点链接。
        /// </summary>
        public int AutoLinkHeadingDepth { get; set; }

        /// <summary>把 Markdown 的软换行（单个换行）也渲染成 <c>&lt;br /&gt;</c>。</summary>
        public bool SoftBreakAsHard { get; set; }

        /// <summary>是否把 <c>:smile:</c> 这类 emoji 短代码转换为字符（默认开启，与参考实现一致）。</summary>
        public bool EnableEmoji { get; set; } = true;

        /// <summary>是否把裸露的 <c>http(s)://</c> 与 <c>www.</c> 文本自动转换为链接（默认开启，GFM 行为）。</summary>
        public bool EnableAutoLinkUrl { get; set; } = true;

        /// <summary>根节点与内部元素使用的 CSS 类名前缀。</summary>
        public string ClassPrefix { get; set; } = "el-markdown-grow";

        /// <summary>空内容时显示的文本。</summary>
        public string EmptyText { get; set; } = "暂无内容";

        /// <summary>由当前选项创建一份副本。</summary>
        public ElMarkdownGrowOptions Clone()
        {
            return new ElMarkdownGrowOptions
            {
                AllowHtml = AllowHtml,
                AllowedHtmlTags = AllowedHtmlTags,
                AllowedHtmlAttributes = AllowedHtmlAttributes,
                AutoLinkHeadingDepth = AutoLinkHeadingDepth,
                SoftBreakAsHard = SoftBreakAsHard,
                EnableEmoji = EnableEmoji,
                EnableAutoLinkUrl = EnableAutoLinkUrl,
                ClassPrefix = ClassPrefix,
                EmptyText = EmptyText
            };
        }
    }
}
