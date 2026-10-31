using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ElementPlus
{
    /// <summary>
    /// ElHtmlEditorGrow 的粘贴内容消毒选项。
    /// </summary>
    public class ElHtmlEditorGrowSanitizeOptions
    {
        /// <summary>允许的标签白名单；为 <c>null</c> 时使用 <see cref="ElHtmlEditorGrowSanitizer.DefaultAllowedTags"/>。</summary>
        public IEnumerable<string> AllowedTags { get; set; }

        /// <summary>允许的属性白名单；为 <c>null</c> 时使用 <see cref="ElHtmlEditorGrowSanitizer.DefaultAllowedAttributes"/>。</summary>
        public IEnumerable<string> AllowedAttributes { get; set; }

        /// <summary>是否保留 <c>&lt;img&gt;</c>。</summary>
        public bool AllowImages { get; set; } = true;

        /// <summary>是否保留 <c>&lt;a&gt;</c>（为 <c>false</c> 时仅去掉链接标签、保留其中的文字）。</summary>
        public bool AllowLinks { get; set; } = true;

        /// <summary>是否保留 <c>style</c> 属性（仍会拦截 <c>expression()</c> 等危险表达式）。</summary>
        public bool AllowStyles { get; set; } = true;

        /// <summary>是否保留 <c>data-*</c> 属性。</summary>
        public bool AllowDataAttributes { get; set; } = true;

        /// <summary>是否保留 <c>class</c> 属性。</summary>
        public bool AllowClasses { get; set; } = true;

        /// <summary>纯文本模式：丢弃全部标签与样式，仅保留文字与基本换行。</summary>
        public bool StripFormatting { get; set; }
    }

    /// <summary>
    /// ElHtmlEditorGrow 的复制/粘贴内容消毒器（纯 C#，**不依赖任何第三方消毒库**）。
    /// <para>
    /// 策略：标签白名单 + 属性白名单（含 <c>data-*</c> 前缀放行）+ URI 属性危险协议拦截 + <c>style</c> 表达式拦截，
    /// 并把 <c>script/style/iframe/object/…</c> 这类危险容器**连同内容**整段剔除，避免只删标签导致脚本正文以文本形式残留。
    /// </para>
    /// <para>
    /// 安全判定（协议白名单、<c>style</c> 表达式、实体解码、危险容器剔除）与 <see cref="ElMarkdownGrowSanitizer"/>
    /// **共用同一份实现**，避免库内出现两套会各自漂移的安全规则；标签与属性白名单则按编辑器场景单独声明（额外放行 <c>data-*</c>）。
    /// </para>
    /// </summary>
    public static class ElHtmlEditorGrowSanitizer
    {
        /// <summary>默认允许的标签（在 Markdown 白名单基础上补齐表格相关标签）。</summary>
        public static readonly IReadOnlyCollection<string> DefaultAllowedTags = new HashSet<string>(StringComparer.Ordinal)
        {
            "a", "abbr", "address", "article", "aside", "b", "bdi", "big", "blockquote", "br",
            "caption", "center", "cite", "code", "col", "colgroup", "dd", "del", "dfn", "div", "dl", "dt",
            "em", "figcaption", "figure", "font", "footer", "h1", "h2", "h3", "h4", "h5", "h6",
            "header", "hr", "i", "img", "ins", "kbd", "label", "li", "main", "mark",
            "nav", "ol", "p", "pre", "q", "s", "samp", "section", "small", "span", "strike", "strong",
            "sub", "sup", "table", "tbody", "td", "tfoot", "th", "thead", "time", "tr", "tt", "u", "ul", "var", "wbr"
        };

        /// <summary>默认允许的属性。</summary>
        public static readonly IReadOnlyCollection<string> DefaultAllowedAttributes = new HashSet<string>(StringComparer.Ordinal)
        {
            "abbr", "align", "alt", "axis", "bgcolor", "border", "cellpadding", "cellspacing",
            "cite", "class", "color", "colspan", "datetime", "dir", "face", "height", "href",
            "hreflang", "id", "lang", "rel", "rev", "rowspan", "scope", "size", "span", "src",
            "start", "style", "summary", "target", "title", "type", "valign", "value", "width"
        };

        /// <summary>需要做协议检查的属性（与 <see cref="ElMarkdownGrowSanitizer.UriAttributes"/> 对齐）。</summary>
        public static readonly IReadOnlyCollection<string> UriAttributes = new HashSet<string>(StringComparer.Ordinal)
        {
            "action", "background", "cite", "href", "longdesc", "poster", "src", "srcset", "usemap"
        };

        /// <summary>无需闭合标签的空元素。</summary>
        public static readonly IReadOnlyCollection<string> VoidElements = new HashSet<string>(StringComparer.Ordinal)
        {
            "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param",
            "source", "track", "wbr"
        };

        /// <summary>纯文本模式下会被转换为换行的块级标签。</summary>
        private static readonly HashSet<string> BlockTags = new HashSet<string>(StringComparer.Ordinal)
        {
            "address", "article", "aside", "blockquote", "caption", "dd", "div", "dl", "dt",
            "fieldset", "figcaption", "figure", "footer", "form", "h1", "h2", "h3", "h4", "h5", "h6",
            "header", "hr", "li", "main", "nav", "ol", "p", "pre", "section", "table", "tbody",
            "td", "tfoot", "th", "thead", "tr", "ul"
        };

        /// <summary>按给定选项消毒一段 HTML。</summary>
        public static string Sanitize(string html, ElHtmlEditorGrowSanitizeOptions options = null)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

            options ??= new ElHtmlEditorGrowSanitizeOptions();

            if (options.StripFormatting)
            {
                return ToPlainText(html);
            }

            var tags = ToSet(options.AllowedTags, DefaultAllowedTags);
            var attributes = ToSet(options.AllowedAttributes, DefaultAllowedAttributes);

            // 危险容器（含内容）先整段剔除，后续只需处理普通标签。
            var stripped = ElMarkdownGrowSanitizer.StripDangerousContainers(html);

            var output = new StringBuilder(stripped.Length);
            var index = 0;

            while (index < stripped.Length)
            {
                var start = stripped.IndexOf('<', index);

                if (start < 0)
                {
                    output.Append(stripped, index, stripped.Length - index);
                    break;
                }

                output.Append(stripped, index, start - index);

                if (startsComment(stripped, start))
                {
                    var commentEnd = stripped.IndexOf("-->", start + 4, StringComparison.Ordinal);
                    index = commentEnd < 0 ? stripped.Length : commentEnd + 3;
                    continue;
                }

                if (start + 1 < stripped.Length && (stripped[start + 1] == '!' || stripped[start + 1] == '?'))
                {
                    var declarationEnd = stripped.IndexOf('>', start);
                    index = declarationEnd < 0 ? stripped.Length : declarationEnd + 1;
                    continue;
                }

                var end = FindTagEnd(stripped, start);

                if (end < 0)
                {
                    output.Append(stripped, start, stripped.Length - start);
                    break;
                }

                output.Append(RebuildTag(stripped.Substring(start + 1, end - start - 1), tags, attributes, options));
                index = end + 1;
            }

            return output.ToString();
        }

        /// <summary>把 HTML 转为纯文本（保留基本换行），用于“粘贴为纯文本”。</summary>
        public static string ToPlainText(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

            var stripped = ElMarkdownGrowSanitizer.StripDangerousContainers(html);
            var text = new StringBuilder(stripped.Length);
            var index = 0;

            while (index < stripped.Length)
            {
                var start = stripped.IndexOf('<', index);

                if (start < 0)
                {
                    text.Append(stripped, index, stripped.Length - index);
                    break;
                }

                text.Append(stripped, index, start - index);

                if (startsComment(stripped, start))
                {
                    var commentEnd = stripped.IndexOf("-->", start + 4, StringComparison.Ordinal);
                    index = commentEnd < 0 ? stripped.Length : commentEnd + 3;
                    continue;
                }

                var end = FindTagEnd(stripped, start);

                if (end < 0)
                {
                    text.Append(stripped, start, stripped.Length - start);
                    break;
                }

                var inner = stripped.Substring(start + 1, end - start - 1).Trim();
                var isClosing = inner.StartsWith("/", StringComparison.Ordinal);
                var name = ReadTagName(isClosing ? inner.Substring(1) : inner);

                if (name == "br" || (!isClosing && BlockTags.Contains(name)) || (isClosing && BlockTags.Contains(name)))
                {
                    text.Append('\n');
                }

                index = end + 1;
            }

            return CollapseBlankLines(ElMarkdownGrowSanitizer.DecodeEntities(text.ToString())).Trim();
        }

        /// <summary>判断 URL 是否安全（与 Markdown 消毒器共用协议白名单）。</summary>
        public static bool IsSafeUrl(string url)
        {
            return ElMarkdownGrowSanitizer.IsSafeUrl(url);
        }

        /// <summary>判断 URL 是否使用危险协议。</summary>
        public static bool IsDangerousUrl(string url)
        {
            return ElMarkdownGrowSanitizer.IsDangerousUrl(url);
        }

        /// <summary>判断 <c>style</c> 值是否含危险表达式。</summary>
        public static bool IsDangerousStyle(string style)
        {
            return ElMarkdownGrowSanitizer.IsDangerousStyle(style);
        }

        /// <summary>解码常见 HTML 实体（含数字实体）。</summary>
        public static string DecodeEntities(string input)
        {
            return ElMarkdownGrowSanitizer.DecodeEntities(input);
        }

        // ---------- 内部实现 ----------

        private static bool startsComment(string text, int start)
        {
            return start + 3 < text.Length && text[start + 1] == '!' && text[start + 2] == '-' && text[start + 3] == '-';
        }

        /// <summary>找到标签结束的 <c>&gt;</c>，跳过被引号包裹的属性值。</summary>
        private static int FindTagEnd(string text, int start)
        {
            var quote = '\0';

            for (var i = start + 1; i < text.Length; i++)
            {
                var c = text[i];

                if (quote != '\0')
                {
                    if (c == quote)
                    {
                        quote = '\0';
                    }

                    continue;
                }

                if (c == '"' || c == '\'')
                {
                    quote = c;
                    continue;
                }

                if (c == '>')
                {
                    return i;
                }
            }

            return -1;
        }

        private static string RebuildTag(string inner, HashSet<string> tags, HashSet<string> attributes, ElHtmlEditorGrowSanitizeOptions options)
        {
            var isClosing = inner.StartsWith("/", StringComparison.Ordinal);
            var body = isClosing ? inner.Substring(1) : inner;
            var name = ReadTagName(body);

            if (name.Length == 0 || !tags.Contains(name))
            {
                // 不在白名单：丢弃标签本身，保留子内容。
                return string.Empty;
            }

            if (name == "img" && !options.AllowImages)
            {
                return string.Empty;
            }

            if (name == "a" && !options.AllowLinks)
            {
                return string.Empty;
            }

            if (isClosing)
            {
                return VoidElements.Contains(name) ? string.Empty : "</" + name + ">";
            }

            var output = new StringBuilder();
            output.Append('<').Append(name);

            var index = name.Length;

            while (index < body.Length)
            {
                // 跳过空白与结尾的 '/'
                while (index < body.Length && (char.IsWhiteSpace(body[index]) || body[index] == '/'))
                {
                    index++;
                }

                if (index >= body.Length)
                {
                    break;
                }

                var nameStart = index;

                while (index < body.Length && !char.IsWhiteSpace(body[index]) && body[index] != '=' && body[index] != '/')
                {
                    index++;
                }

                var attributeName = body.Substring(nameStart, index - nameStart).ToLowerInvariant();

                var valueStart = index;

                while (valueStart < body.Length && char.IsWhiteSpace(body[valueStart]))
                {
                    valueStart++;
                }

                string attributeValue = null;

                if (valueStart < body.Length && body[valueStart] == '=')
                {
                    valueStart++;

                    while (valueStart < body.Length && char.IsWhiteSpace(body[valueStart]))
                    {
                        valueStart++;
                    }

                    if (valueStart < body.Length && (body[valueStart] == '"' || body[valueStart] == '\''))
                    {
                        var quote = body[valueStart];
                        valueStart++;
                        var valueEnd = body.IndexOf(quote, valueStart);

                        if (valueEnd < 0)
                        {
                            valueEnd = body.Length;
                        }

                        attributeValue = body.Substring(valueStart, valueEnd - valueStart);
                        index = valueEnd + 1;
                    }
                    else
                    {
                        var valueEnd = valueStart;

                        while (valueEnd < body.Length && !char.IsWhiteSpace(body[valueEnd]))
                        {
                            valueEnd++;
                        }

                        attributeValue = body.Substring(valueStart, valueEnd - valueStart);
                        index = valueEnd;
                    }
                }
                else
                {
                    index = valueStart;
                }

                if (attributeName.Length == 0)
                {
                    continue;
                }

                if (!IsAllowedAttribute(attributeName, attributeValue, attributes, options))
                {
                    continue;
                }

                output.Append(' ').Append(attributeName);

                if (attributeValue != null)
                {
                    output.Append("=\"").Append(ElHtmlEditorGrowHtmlBuilder.EscapeAttribute(attributeValue)).Append('"');
                }
            }

            output.Append('>');

            return output.ToString();
        }

        private static bool IsAllowedAttribute(string name, string value, HashSet<string> attributes, ElHtmlEditorGrowSanitizeOptions options)
        {
            // 事件处理器一律不保留（无论白名单怎么写）。
            if (name.StartsWith("on", StringComparison.Ordinal))
            {
                return false;
            }

            if (name.StartsWith("data-", StringComparison.Ordinal))
            {
                return options.AllowDataAttributes;
            }

            if (name == "class" && !options.AllowClasses)
            {
                return false;
            }

            if (name == "style")
            {
                return options.AllowStyles && !IsDangerousStyle(value);
            }

            if (!attributes.Contains(name))
            {
                return false;
            }

            if (UriAttributes.Contains(name) && !IsSafeUrl(value))
            {
                return false;
            }

            return true;
        }

        private static string ReadTagName(string body)
        {
            var index = 0;

            while (index < body.Length)
            {
                var c = body[index];

                if (char.IsLetterOrDigit(c) || c == '-' || c == ':' || c == '_')
                {
                    index++;
                    continue;
                }

                break;
            }

            return body.Substring(0, index).ToLowerInvariant();
        }

        private static HashSet<string> ToSet(IEnumerable<string> values, IReadOnlyCollection<string> fallback)
        {
            var source = values ?? fallback;
            var set = new HashSet<string>(StringComparer.Ordinal);

            foreach (var value in source)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    set.Add(value.Trim().ToLowerInvariant());
                }
            }

            return set;
        }

        private static string CollapseBlankLines(string text)
        {
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var output = new StringBuilder(text.Length);
            var pendingBreak = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.Length == 0)
                {
                    pendingBreak = true;
                    continue;
                }

                if (output.Length > 0 && pendingBreak)
                {
                    output.Append('\n');
                }

                pendingBreak = false;
                output.Append(trimmed).Append('\n');
            }

            return output.ToString();
        }
    }
}
