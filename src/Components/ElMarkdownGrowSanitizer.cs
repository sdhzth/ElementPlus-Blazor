using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ElementPlus
{
    /// <summary>
    /// Markdown 中内嵌 HTML 的消毒器。
    /// 策略与参考实现（Radzen 的 <c>HtmlSanitizer</c>）对齐：标签白名单 + 属性白名单 + URI 属性危险协议拦截 + <c>style</c> 表达式拦截；
    /// 并额外把危险容器（script/style/iframe/object/... ）连同其内容整段剔除，避免只删标签导致脚本正文以文本形式泄漏到页面上。
    /// 全部代码自写，无任何外部依赖。
    /// </summary>
    public static class ElMarkdownGrowSanitizer
    {
        /// <summary>默认允许的 HTML 标签（对齐参考实现的白名单，但去除了 html/head/body 这类文档级标签）。</summary>
        public static readonly IReadOnlyCollection<string> DefaultAllowedTags = new HashSet<string>(StringComparer.Ordinal)
        {
            "a", "abbr", "acronym", "address", "area", "b",
            "big", "blockquote", "br", "button", "caption", "center", "cite",
            "code", "col", "colgroup", "dd", "del", "dfn", "dir", "div", "dl", "dt",
            "em", "fieldset", "font", "h1", "h2", "h3", "h4", "h5", "h6",
            "hr", "i", "img", "input", "ins", "kbd", "label", "legend", "li", "map",
            "menu", "ol", "optgroup", "option", "p", "pre", "q", "s", "samp",
            "select", "small", "span", "strike", "strong", "sub", "sup", "table",
            "tbody", "td", "textarea", "tfoot", "th", "thead", "tr", "tt", "u",
            "ul", "var", "section", "nav", "article", "aside", "header", "footer", "main",
            "figure", "figcaption", "data", "time", "mark", "ruby", "rt", "rp", "bdi", "wbr",
            "datalist", "output", "progress", "meter", "details", "summary"
        };

        /// <summary>默认允许的 HTML 属性（对齐参考实现）。</summary>
        public static readonly IReadOnlyCollection<string> DefaultAllowedAttributes = new HashSet<string>(StringComparer.Ordinal)
        {
            "abbr", "accept", "accept-charset", "accesskey",
            "action", "align", "alt", "axis", "bgcolor", "border", "cellpadding",
            "cellspacing", "char", "charoff", "charset", "checked", "cite", "class",
            "clear", "cols", "colspan", "color", "compact", "coords", "datetime",
            "dir", "disabled", "enctype", "for", "frame", "headers", "height",
            "href", "hreflang", "hspace", "id", "ismap", "label", "lang",
            "longdesc", "maxlength", "media", "method", "multiple", "name",
            "nohref", "noshade", "nowrap", "prompt", "readonly", "rel", "rev",
            "rows", "rowspan", "rules", "scope", "selected", "shape", "size",
            "span", "src", "start", "style", "summary", "tabindex", "target", "title",
            "type", "usemap", "valign", "value", "vspace", "width",
            "high", "keytype", "list", "low", "max", "min", "novalidate", "open", "optimum",
            "pattern", "placeholder", "pubdate", "radiogroup", "required", "reversed", "spellcheck", "step",
            "wrap", "challenge", "contenteditable", "draggable", "dropzone", "autocomplete", "autosave"
        };

        /// <summary>需要检查协议的属性。</summary>
        public static readonly IReadOnlyCollection<string> UriAttributes = new HashSet<string>(StringComparer.Ordinal)
        {
            "action", "background", "dynsrc", "href", "lowsrc", "src", "longdesc", "usemap", "cite", "formaction", "poster", "srcset"
        };

        /// <summary>危险标签：连同内容一起剔除。</summary>
        public static readonly IReadOnlyCollection<string> DangerousTags = new HashSet<string>(StringComparer.Ordinal)
        {
            "script", "style", "iframe", "object", "embed", "applet", "frame", "frameset",
            "link", "meta", "base", "template", "svg", "math", "noscript", "title", "head", "body", "html"
        };

        /// <summary>不允许省略赋值的布尔属性（其余属性必须带值才会输出）。</summary>
        private static readonly HashSet<string> BooleanAttributes = new HashSet<string>(StringComparer.Ordinal)
        {
            "checked", "disabled", "readonly", "selected", "multiple", "required", "open",
            "reversed", "novalidate", "compact", "nowrap", "noshade", "ismap", "nohref", "autosave", "pubdate"
        };

        /// <summary>允许的 URL 协议前缀。</summary>
        private static readonly string[] SafeSchemes = { "http:", "https:", "mailto:", "tel:", "ftp:", "ftps:" };

        /// <summary>按给定白名单消毒一段 HTML。</summary>
        /// <param name="html">原始 HTML 片段。</param>
        /// <param name="allowedTags">允许的标签；为 null 时使用默认白名单。</param>
        /// <param name="allowedAttributes">允许的属性；为 null 时使用默认白名单。</param>
        public static string Sanitize(string html, IEnumerable<string> allowedTags = null, IEnumerable<string> allowedAttributes = null)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

            var tags = ToSet(allowedTags, DefaultAllowedTags);
            var attributes = ToSet(allowedAttributes, DefaultAllowedAttributes);

            var stripped = StripDangerousContainers(html);

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

                var end = FindTagEnd(stripped, start);

                if (end < 0)
                {
                    // 未闭合的 '<'，原样输出
                    output.Append(stripped, start, stripped.Length - start);
                    break;
                }

                var inner = stripped.Substring(start + 1, end - start - 1);
                output.Append(SanitizeTag(inner, tags, attributes));
                index = end + 1;
            }

            return output.ToString();
        }

        /// <summary>按选项消毒一段 HTML。</summary>
        public static string Sanitize(string html, ElMarkdownGrowOptions options)
        {
            if (options == null)
            {
                return Sanitize(html);
            }

            return Sanitize(html, options.AllowedHtmlTags, options.AllowedHtmlAttributes);
        }

        /// <summary>剔除危险容器（含其内部内容）。</summary>
        public static string StripDangerousContainers(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }

            var result = html;

            foreach (var tag in DangerousTags)
            {
                // <tag ...> ... </tag>
                result = RemoveContainer(result, tag);
            }

            return result;
        }

        /// <summary>判断 URL 是否为危险协议。</summary>
        public static bool IsDangerousUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            var decoded = NormalizeForScan(url);

            return decoded.StartsWith("javascript:", StringComparison.Ordinal)
                || decoded.StartsWith("vbscript:", StringComparison.Ordinal)
                || decoded.StartsWith("livescript:", StringComparison.Ordinal)
                || decoded.StartsWith("mocha:", StringComparison.Ordinal)
                || decoded.StartsWith("data:text/html", StringComparison.Ordinal)
                || decoded.StartsWith("data:text/javascript", StringComparison.Ordinal)
                || decoded.StartsWith("data:application/xhtml", StringComparison.Ordinal)
                || decoded.Contains("expression(", StringComparison.Ordinal);
        }

        /// <summary>判断 URL 是否可安全输出（相对路径、锚点、以及 http/https/mailto/tel/ftp 等协议视为安全）。</summary>
        public static bool IsSafeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return true;
            }

            if (IsDangerousUrl(url))
            {
                return false;
            }

            var decoded = NormalizeForScan(url);

            // 含协议（scheme:）时必须是安全协议
            var colon = decoded.IndexOf(':');
            var slash = decoded.IndexOf('/');

            if (colon > 0 && (slash < 0 || colon < slash))
            {
                // 形如 "#id:xx" 之类不是协议
                var schemeEnd = colon;
                var isScheme = true;

                for (var i = 0; i < schemeEnd; i++)
                {
                    var c = decoded[i];

                    if (!(char.IsLetterOrDigit(c) || c == '+' || c == '-' || c == '.'))
                    {
                        isScheme = false;
                        break;
                    }
                }

                if (isScheme)
                {
                    foreach (var scheme in SafeSchemes)
                    {
                        if (decoded.StartsWith(scheme, StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            return true;
        }

        /// <summary>判断 style 属性值是否含危险表达式。</summary>
        public static bool IsDangerousStyle(string style)
        {
            if (string.IsNullOrWhiteSpace(style))
            {
                return false;
            }

            var decoded = NormalizeForScan(style);

            return decoded.Contains("expression", StringComparison.Ordinal)
                || decoded.Contains("javascript:", StringComparison.Ordinal)
                || decoded.Contains("vbscript:", StringComparison.Ordinal)
                || decoded.Contains("behavior:", StringComparison.Ordinal)
                || decoded.Contains("-moz-binding", StringComparison.Ordinal);
        }

        /// <summary>解码常见 HTML 实体（含数字实体），用于 URL/style 的安全检测。</summary>
        public static string DecodeEntities(string input)
        {
            if (string.IsNullOrEmpty(input) || input.IndexOf('&') < 0)
            {
                return input ?? string.Empty;
            }

            var output = new StringBuilder(input.Length);
            var index = 0;

            while (index < input.Length)
            {
                var amp = input.IndexOf('&', index);

                if (amp < 0)
                {
                    output.Append(input, index, input.Length - index);
                    break;
                }

                output.Append(input, index, amp - index);

                var semi = input.IndexOf(';', amp + 1);

                if (semi < 0 || semi - amp > 12)
                {
                    output.Append('&');
                    index = amp + 1;
                    continue;
                }

                var entity = input.Substring(amp + 1, semi - amp - 1);
                var decoded = DecodeEntity(entity);

                if (decoded == null)
                {
                    output.Append('&');
                    index = amp + 1;
                    continue;
                }

                output.Append(decoded);
                index = semi + 1;
            }

            return output.ToString();
        }

        /// <summary>是否为合法的命名/数字实体。</summary>
        public static bool IsEntity(string entity)
        {
            return DecodeEntity(entity) != null;
        }

        /// <summary>把标签转换成小写并去掉空白，得到可比较的名字。</summary>
        internal static string NormalizeName(string name)
        {
            return string.IsNullOrEmpty(name) ? string.Empty : name.Trim().ToLowerInvariant();
        }

        private static ISet<string> ToSet(IEnumerable<string> values, IReadOnlyCollection<string> fallback)
        {
            if (values == null)
            {
                return new HashSet<string>(fallback, StringComparer.Ordinal);
            }

            var set = new HashSet<string>(StringComparer.Ordinal);

            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    set.Add(value.Trim().ToLowerInvariant());
                }
            }

            return set;
        }

        private static string RemoveContainer(string html, string tag)
        {
            var open = "<" + tag;
            var close = "</" + tag;
            var result = html;
            var searchFrom = 0;

            while (true)
            {
                var start = result.IndexOf(open, searchFrom, StringComparison.OrdinalIgnoreCase);

                if (start < 0)
                {
                    return result;
                }

                var after = start + open.Length;

                // 必须是标签名边界（后接空白、'/' 或 '>'）
                if (after < result.Length)
                {
                    var c = result[after];

                    if (!(char.IsWhiteSpace(c) || c == '/' || c == '>'))
                    {
                        searchFrom = after;
                        continue;
                    }
                }

                var tagEnd = FindTagEnd(result, start);

                if (tagEnd < 0)
                {
                    return result.Substring(0, start);
                }

                if (result[tagEnd - 1] == '/')
                {
                    // 自闭合，如 <embed ... />
                    result = result.Remove(start, tagEnd - start + 1);
                    searchFrom = start;
                    continue;
                }

                var closeStart = result.IndexOf(close, tagEnd, StringComparison.OrdinalIgnoreCase);

                if (closeStart < 0)
                {
                    // 没有闭合标签，只去掉开始标签
                    result = result.Remove(start, tagEnd - start + 1);
                    searchFrom = start;
                    continue;
                }

                var closeEnd = FindTagEnd(result, closeStart);

                if (closeEnd < 0)
                {
                    result = result.Remove(start, result.Length - start);
                    return result;
                }

                result = result.Remove(start, closeEnd - start + 1);
                searchFrom = start;
            }
        }

        /// <summary>找到标签的结束 '&gt;'（跳过引号内的内容）。返回 -1 表示未闭合。</summary>
        private static int FindTagEnd(string html, int start)
        {
            var quote = '\0';

            for (var i = start + 1; i < html.Length; i++)
            {
                var c = html[i];

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

        private static string SanitizeTag(string inner, ISet<string> allowedTags, ISet<string> allowedAttributes)
        {
            var body = inner;

            if (body.StartsWith("/", StringComparison.Ordinal))
            {
                body = body.Substring(1);
            }

            var selfClosing = body.EndsWith("/", StringComparison.Ordinal);

            if (selfClosing)
            {
                body = body.Substring(0, body.Length - 1);
            }

            var cursor = 0;

            while (cursor < body.Length && char.IsWhiteSpace(body[cursor]))
            {
                cursor++;
            }

            var nameStart = cursor;

            while (cursor < body.Length && (char.IsLetterOrDigit(body[cursor]) || body[cursor] == '-' || body[cursor] == ':'))
            {
                cursor++;
            }

            var tag = NormalizeName(body.Substring(nameStart, cursor - nameStart));

            if (tag.Length == 0 || !allowedTags.Contains(tag))
            {
                return string.Empty;
            }

            var isClosing = inner.Length > 0 && inner[0] == '/';
            var attributes = SanitizeAttributes(body.Substring(cursor), allowedAttributes, tag);
            var suffix = selfClosing && !IsVoidElement(tag) ? " /" : string.Empty;

            return isClosing
                ? "</" + tag + ">"
                : "<" + tag + attributes + suffix + ">";
        }

        private static string SanitizeAttributes(string text, ISet<string> allowedAttributes, string tag)
        {
            var output = new StringBuilder();
            var index = 0;

            while (index < text.Length)
            {
                while (index < text.Length && (char.IsWhiteSpace(text[index]) || text[index] == '/'))
                {
                    index++;
                }

                if (index >= text.Length)
                {
                    break;
                }

                var nameStart = index;

                while (index < text.Length && !char.IsWhiteSpace(text[index]) && text[index] != '=' && text[index] != '/')
                {
                    index++;
                }

                var name = NormalizeName(text.Substring(nameStart, index - nameStart));

                while (index < text.Length && char.IsWhiteSpace(text[index]))
                {
                    index++;
                }

                string value = null;

                if (index < text.Length && text[index] == '=')
                {
                    index++;

                    while (index < text.Length && char.IsWhiteSpace(text[index]))
                    {
                        index++;
                    }

                    if (index < text.Length && (text[index] == '"' || text[index] == '\''))
                    {
                        var quote = text[index];
                        var valueStart = ++index;

                        while (index < text.Length && text[index] != quote)
                        {
                            index++;
                        }

                        value = text.Substring(valueStart, index - valueStart);

                        if (index < text.Length)
                        {
                            index++;
                        }
                    }
                    else
                    {
                        var valueStart = index;

                        while (index < text.Length && !char.IsWhiteSpace(text[index]))
                        {
                            index++;
                        }

                        value = text.Substring(valueStart, index - valueStart);
                    }
                }

                if (name.Length == 0)
                {
                    continue;
                }

                // 事件处理器一律拒绝（即便被放进白名单）
                if (name.StartsWith("on", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!allowedAttributes.Contains(name))
                {
                    continue;
                }

                if (value == null)
                {
                    if (BooleanAttributes.Contains(name))
                    {
                        output.Append(' ').Append(name);
                    }

                    continue;
                }

                if (UriAttributes.Contains(name) && !IsSafeUrl(value))
                {
                    continue;
                }

                if (name == "style" && IsDangerousStyle(value))
                {
                    continue;
                }

                output.Append(' ').Append(name).Append("=\"").Append(EscapeAttribute(value)).Append('"');
            }

            return output.ToString();
        }

        private static bool IsVoidElement(string tag)
        {
            switch (tag)
            {
                case "area":
                case "base":
                case "br":
                case "col":
                case "hr":
                case "img":
                case "input":
                case "link":
                case "meta":
                case "param":
                case "source":
                case "track":
                case "wbr":
                    return true;
                default:
                    return false;
            }
        }

        private static string EscapeAttribute(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var output = new StringBuilder(value.Length);

            foreach (var c in value)
            {
                switch (c)
                {
                    case '&':
                        output.Append("&amp;");
                        break;
                    case '"':
                        output.Append("&quot;");
                        break;
                    case '<':
                        output.Append("&lt;");
                        break;
                    case '>':
                        output.Append("&gt;");
                        break;
                    case '\r':
                    case '\n':
                    case '\t':
                        output.Append(' ');
                        break;
                    default:
                        output.Append(c);
                        break;
                }
            }

            return output.ToString();
        }

        private static string DecodeEntity(string entity)
        {
            if (string.IsNullOrEmpty(entity))
            {
                return null;
            }

            switch (entity.ToLowerInvariant())
            {
                case "amp": return "&";
                case "lt": return "<";
                case "gt": return ">";
                case "quot": return "\"";
                case "apos": return "'";
                case "nbsp": return " ";
                case "tab": return "\t";
                case "newline": return "\n";
            }

            if (entity[0] != '#')
            {
                return null;
            }

            try
            {
                if (entity.Length > 1 && (entity[1] == 'x' || entity[1] == 'X'))
                {
                    if (int.TryParse(entity.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue))
                    {
                        return char.ConvertFromUtf32(hexValue);
                    }

                    return null;
                }

                if (int.TryParse(entity.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                {
                    return char.ConvertFromUtf32(value);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }

            return null;
        }

        /// <summary>解码实体并去掉控制字符/空白，再转小写，用于协议嗅探。</summary>
        private static string NormalizeForScan(string value)
        {
            var decoded = DecodeEntities(value);
            var output = new StringBuilder(decoded.Length);

            foreach (var c in decoded)
            {
                if (char.IsControl(c) || char.IsWhiteSpace(c))
                {
                    continue;
                }

                output.Append(char.ToLowerInvariant(c));
            }

            return output.ToString();
        }
    }
}
