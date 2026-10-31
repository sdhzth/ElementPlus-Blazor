using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ElementPlus
{
    /// <summary>
    /// <see cref="ElViewerGrow"/> 的内建文档渲染器：把文本 / Markdown / CSV / 图片 / 音视频 / PDF
    /// 转成 HTML 字符串。
    /// <para>
    /// 全部为纯 C# 字符串构建（单一真源，渲染与单元测试共用），不引入 Markdown 解析库、
    /// 不引入任何 JS 渲染引擎。所有用户输入都先经过 <see cref="EscapeHtml"/>，
    /// 链接地址必须过 <see cref="ElViewerGrowEngine.IsSafeUrl"/> 才会输出 <c>href</c>。
    /// </para>
    /// </summary>
    public static class ElViewerGrowRenderer
    {
        private static readonly Regex CodeSpan = new Regex("`([^`]+)`", RegexOptions.Compiled);

        private static readonly Regex Link = new Regex(@"\[([^\]]*)\]\(([^)\s]*)\)", RegexOptions.Compiled);

        private static readonly Regex StrongStar = new Regex(@"\*\*(.+?)\*\*", RegexOptions.Compiled);

        private static readonly Regex StrongUnderscore = new Regex(@"(?<![\w])__(.+?)__(?![\w])", RegexOptions.Compiled);

        private static readonly Regex EmStar = new Regex(@"(?<![\w*])\*([^*\n]+?)\*(?![\w*])", RegexOptions.Compiled);

        private static readonly Regex EmUnderscore = new Regex(@"(?<![\w_])_([^_\n]+?)_(?![\w_])", RegexOptions.Compiled);

        #region 转义

        /// <summary>
        /// HTML 转义。覆盖 <c>&amp; &lt; &gt; &quot; &#39;</c>，足以阻断标签注入与属性注入。
        /// </summary>
        public static string EscapeHtml(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);

            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '&':
                        builder.Append("&amp;");
                        break;
                    case '<':
                        builder.Append("&lt;");
                        break;
                    case '>':
                        builder.Append("&gt;");
                        break;
                    case '"':
                        builder.Append("&quot;");
                        break;
                    case '\'':
                        builder.Append("&#39;");
                        break;
                    default:
                        builder.Append(ch);
                        break;
                }
            }

            return builder.ToString();
        }

        #endregion

        #region 可插拔渲染器

        private static readonly Dictionary<ElViewerDocumentKind, Func<byte[], string>> ExternalRenderers =
            new Dictionary<ElViewerDocumentKind, Func<byte[], string>>();

        private static readonly Regex DangerousBlock = new Regex(
            @"<\s*(script|iframe|object|embed|frame|form|meta|base)\b[^>]*>[\s\S]*?<\s*/\s*\1\s*>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex DangerousVoid = new Regex(
            @"<\s*(script|iframe|object|embed|frame|form|meta|base)\b[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex EventAttribute = new Regex(
            @"\son[a-z]+\s*=\s*(?:""[^""]*""|'[^']*'|[^\s>]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex UriAttribute = new Regex(
            @"\s(?:href|src|xlink:href|action|formaction)\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s>]+))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 注册「文档字节 → HTML 片段」的外部渲染器，用来补上浏览器与内建渲染器都不覆盖的类型
        /// —— 最典型的就是 OFD 版式文档，其次还有 Word / Excel / PowerPoint / CAD。
        /// <para>
        /// 组件自身仍然零第三方依赖：注册什么完全由宿主决定（把自己项目的 OFD 库包一层委托传进来即可），
        /// 没有注册的类型依旧落到「不支持」状态，不会假装能渲染。委托返回空或抛异常时自动回退到内建路径。
        /// </para>
        /// </summary>
        /// <param name="kind">要补上的文档类型。</param>
        /// <param name="renderer">入参为该文档的原始字节，返回可直接内联的 HTML 片段（通常是 SVG）。传 null 等于注销。</param>
        public static void RegisterRenderer(ElViewerDocumentKind kind, Func<byte[], string> renderer)
        {
            lock (ExternalRenderers)
            {
                if (renderer == null)
                {
                    ExternalRenderers.Remove(kind);
                }
                else
                {
                    ExternalRenderers[kind] = renderer;
                }
            }
        }

        /// <summary>注销某个类型的外部渲染器。</summary>
        public static void UnregisterRenderer(ElViewerDocumentKind kind)
        {
            lock (ExternalRenderers)
            {
                ExternalRenderers.Remove(kind);
            }
        }

        /// <summary>该类型是否已经注册了外部渲染器。</summary>
        public static bool HasRenderer(ElViewerDocumentKind kind)
        {
            lock (ExternalRenderers)
            {
                return ExternalRenderers.ContainsKey(kind);
            }
        }

        /// <summary>
        /// 调用外部渲染器。未注册、入参为空、返回空或抛出异常时统一返回 <c>null</c>，
        /// 由调用方回退到内建路径 —— 一个坏掉的渲染器不会把预览器拖崩。
        /// </summary>
        public static string InvokeRenderer(ElViewerDocumentKind kind, byte[] content)
        {
            if (content == null || content.Length == 0)
            {
                return null;
            }

            Func<byte[], string> renderer;

            lock (ExternalRenderers)
            {
                if (!ExternalRenderers.TryGetValue(kind, out renderer))
                {
                    return null;
                }
            }

            try
            {
                var markup = renderer(content);

                return string.IsNullOrWhiteSpace(markup) ? null : markup;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 净化外部渲染器产出的片段：整体剔除危险容器及其内容、剔除所有 <c>on*</c> 事件属性、
        /// 剔除 <c>javascript:</c> / <c>vbscript:</c> / <c>data:text/html</c> 这类 URI 属性。
        /// 注入的渲染器由宿主负责，但预览器仍守住最后一道门，避免「打开一个文档文件」变成 XSS 通道。
        /// </summary>
        public static string SanitizeMarkup(string markup)
        {
            if (string.IsNullOrEmpty(markup))
            {
                return string.Empty;
            }

            var result = DangerousBlock.Replace(markup, string.Empty);
            result = DangerousVoid.Replace(result, string.Empty);
            result = EventAttribute.Replace(result, string.Empty);
            result = UriAttribute.Replace(result, match =>
            {
                var value = match.Groups[1].Success
                    ? match.Groups[1].Value
                    : (match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value);

                return IsDangerousUriValue(value) ? string.Empty : match.Value;
            });

            return result;
        }

        /// <summary>判断一个属性值是否是危险 URI（允许空白与数字实体绕过写法）。</summary>
        private static bool IsDangerousUriValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var probe = value.Replace(" ", string.Empty)
                .Replace("\t", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("&#x3A;", ":")
                .Replace("&#58;", ":")
                .Replace("&#x3a;", ":")
                .ToLowerInvariant();

            return probe.StartsWith("javascript:", StringComparison.Ordinal)
                || probe.StartsWith("vbscript:", StringComparison.Ordinal)
                || probe.StartsWith("data:text/html", StringComparison.Ordinal);
        }

        #endregion

        #region 入口

        /// <summary>内建渲染器是否能呈现该文档（不含外链预览）。</summary>
        /// <param name="allowOffice">
        /// 是否允许用内建 OOXML 解析器渲染 docx / xlsx / pptx。宿主想把 Office 文档交给
        /// 自己的渲染器或外链服务时，可以传 <c>false</c> 让这些类型退回「不支持 → 走外链」。
        /// </param>
        public static bool CanRender(ElViewerDocument document, bool allowOffice = true)
        {
            if (document == null)
            {
                return false;
            }

            if (ElViewerGrowEngine.IsTextual(document.Kind))
            {
                return true;
            }

            if (!document.IsSafe)
            {
                return false;
            }

            // 宿主注入了渲染器（例如 OFD → SVG）时，只要有字节就能呈现，
            // 且优先于 PDF 的 iframe 回退路径。
            if (document.HasContent && HasRenderer(document.Kind))
            {
                return true;
            }

            // 内建 OOXML 解析器：docx / xlsx / pptx 只要拿到字节就能自己渲染，
            // 既不依赖第三方库，也不需要宿主注册渲染器。
            if (allowOffice && document.RenderedOfficeHtml != null)
            {
                return true;
            }

            return !string.IsNullOrEmpty(document.SourceUrl)
                && (document.Kind == ElViewerDocumentKind.Image
                    || document.Kind == ElViewerDocumentKind.Video
                    || document.Kind == ElViewerDocumentKind.Audio
                    || document.Kind == ElViewerDocumentKind.Pdf);
        }

        /// <summary>按文档类型分派渲染；无法渲染时返回空串（由组件落到「不支持」状态）。</summary>
        /// <param name="allowOffice">是否允许用内建 OOXML 解析器渲染 docx / xlsx / pptx。</param>
        public static string RenderContent(ElViewerDocument document, bool allowOffice = true)
        {
            if (document == null)
            {
                return string.Empty;
            }

            if (ElViewerGrowEngine.IsTextual(document.Kind))
            {
                var text = document.Text ?? string.Empty;

                if (document.Kind == ElViewerDocumentKind.Markdown)
                {
                    return RenderMarkdown(text);
                }

                if (document.Kind == ElViewerDocumentKind.Csv)
                {
                    return RenderCsv(text, null, true);
                }

                return RenderText(text);
            }

            if (!document.IsSafe)
            {
                return string.Empty;
            }

            // 外部渲染器优先；产出为空时继续往下走内建路径，不会把「渲染器没输出」变成空白页。
            if (document.HasContent)
            {
                var external = InvokeRenderer(document.Kind, document.Content);

                if (external != null)
                {
                    return "<div class=\"el-viewer-grow__doc-external\">" + SanitizeMarkup(external) + "</div>";
                }
            }

            // 内建 OOXML 解析（排在 external 之后 = 显式注册的渲染器始终优先）。
            if (allowOffice)
            {
                var office = document.RenderedOfficeHtml;

                if (!string.IsNullOrEmpty(office))
                {
                    return office;
                }
            }

            var source = document.SourceUrl;

            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            switch (document.Kind)
            {
                case ElViewerDocumentKind.Image:
                    return RenderImage(source, document.Title);
                case ElViewerDocumentKind.Video:
                    return RenderVideo(source, document.ResolvedContentType);
                case ElViewerDocumentKind.Audio:
                    return RenderAudio(source);
                case ElViewerDocumentKind.Pdf:
                    return RenderEmbed(source, document.Title);
                default:
                    return string.Empty;
            }
        }

        #endregion

        #region 纯文本

        /// <summary>纯文本 / JSON：整段放进 <c>pre</c>。</summary>
        public static string RenderText(string text)
        {
            return "<pre class=\"el-viewer-grow__doc-pre\">" + EscapeHtml(ElViewerGrowEngine.NormalizeNewLines(text)) + "</pre>";
        }

        #endregion

        #region Markdown

        /// <summary>
        /// 渲染 Markdown 的常用子集：ATX 标题、围栏代码块、分隔线、引用、无序/有序列表、
        /// 段落，以及行内的代码、粗体、斜体、链接。
        /// <para>刻意不支持内联 HTML —— 所有内容都会被转义，避免预览成为 XSS 通道。</para>
        /// </summary>
        public static string RenderMarkdown(string markdown)
        {
            var text = ElViewerGrowEngine.NormalizeNewLines(markdown);

            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var lines = text.Split('\n');
            var builder = new StringBuilder();
            var index = 0;

            builder.Append("<div class=\"el-viewer-grow__doc-md\">");

            while (index < lines.Length)
            {
                var line = lines[index];

                if (IsBlank(line))
                {
                    index++;
                    continue;
                }

                var fence = MatchFence(line);

                if (fence != null)
                {
                    index++;
                    var code = new StringBuilder();

                    while (index < lines.Length)
                    {
                        var current = lines[index];

                        if (current.TrimStart().StartsWith(fence, StringComparison.Ordinal))
                        {
                            index++;
                            break;
                        }

                        if (code.Length > 0)
                        {
                            code.Append('\n');
                        }

                        code.Append(current);
                        index++;
                    }

                    builder.Append("<pre class=\"el-viewer-grow__doc-pre\"><code class=\"el-viewer-grow__doc-code\">");
                    builder.Append(EscapeHtml(code.ToString()));
                    builder.Append("</code></pre>");
                    continue;
                }

                if (IsHorizontalRule(line))
                {
                    builder.Append("<hr class=\"el-viewer-grow__doc-hr\" />");
                    index++;
                    continue;
                }

                var level = HeadingLevel(line);

                if (level > 0)
                {
                    var heading = line.Substring(level).Trim().TrimEnd('#').Trim();

                    builder.Append("<h").Append(level).Append('>');
                    builder.Append(RenderInline(heading));
                    builder.Append("</h").Append(level).Append('>');
                    index++;
                    continue;
                }

                if (IsQuote(line))
                {
                    var quote = new StringBuilder();

                    while (index < lines.Length && IsQuote(lines[index]))
                    {
                        var current = lines[index].TrimStart().Substring(1);

                        if (current.StartsWith(" ", StringComparison.Ordinal))
                        {
                            current = current.Substring(1);
                        }

                        if (quote.Length > 0)
                        {
                            quote.Append('\n');
                        }

                        quote.Append(current);
                        index++;
                    }

                    builder.Append("<blockquote class=\"el-viewer-grow__doc-quote\">");
                    builder.Append(RenderInline(quote.ToString()));
                    builder.Append("</blockquote>");
                    continue;
                }

                if (IsUnorderedItem(line))
                {
                    builder.Append("<ul class=\"el-viewer-grow__doc-list\">");
                    AppendListItems(builder, lines, ref index, false);
                    builder.Append("</ul>");
                    continue;
                }

                if (IsOrderedItem(line))
                {
                    builder.Append("<ol class=\"el-viewer-grow__doc-list\">");
                    AppendListItems(builder, lines, ref index, true);
                    builder.Append("</ol>");
                    continue;
                }

                var paragraph = new StringBuilder();

                while (index < lines.Length && !IsBlank(lines[index]) && !IsBlockStart(lines[index]))
                {
                    if (paragraph.Length > 0)
                    {
                        paragraph.Append(' ');
                    }

                    paragraph.Append(lines[index].Trim());
                    index++;
                }

                if (paragraph.Length == 0)
                {
                    // 理论上不会走到这里（IsBlockStart 已覆盖所有块级起始），防御性退出避免死循环。
                    index++;
                    continue;
                }

                builder.Append("<p class=\"el-viewer-grow__doc-p\">");
                builder.Append(RenderInline(paragraph.ToString()));
                builder.Append("</p>");
            }

            builder.Append("</div>");

            return builder.ToString();
        }

        private static void AppendListItems(StringBuilder builder, string[] lines, ref int index, bool ordered)
        {
            while (index < lines.Length)
            {
                var line = lines[index];
                var matched = ordered ? IsOrderedItem(line) : IsUnorderedItem(line);

                if (!matched)
                {
                    // 缩进的续行属于上一个列表项，简单并入即可。
                    if (line.StartsWith(" ", StringComparison.Ordinal) && !IsBlank(line) && !IsBlockStart(line))
                    {
                        builder.Append(' ').Append(RenderInline(line.Trim()));
                        index++;
                        continue;
                    }

                    break;
                }

                var content = ordered ? StripOrderedMarker(line) : StripUnorderedMarker(line);

                builder.Append("<li class=\"el-viewer-grow__doc-item\">");
                builder.Append(RenderInline(content));
                builder.Append("</li>");
                index++;
            }
        }

        /// <summary>行内标记渲染：代码、链接、粗体、斜体。</summary>
        public static string RenderInline(string text)
        {
            var value = EscapeHtml(text ?? string.Empty);

            value = CodeSpan.Replace(value, "<code class=\"el-viewer-grow__doc-inline-code\">$1</code>");

            value = Link.Replace(value, match =>
            {
                var url = match.Groups[2].Value;

                if (!ElViewerGrowEngine.IsSafeUrl(ElViewerGrowEngine.DecodeEntities(url)))
                {
                    // 危险协议：只保留文字，不产出链接。
                    return match.Groups[1].Value;
                }

                return "<a class=\"el-viewer-grow__doc-link\" href=\"" + url
                    + "\" target=\"_blank\" rel=\"noopener noreferrer\">" + match.Groups[1].Value + "</a>";
            });

            value = StrongStar.Replace(value, "<strong>$1</strong>");
            value = StrongUnderscore.Replace(value, "<strong>$1</strong>");
            value = EmStar.Replace(value, "<em>$1</em>");
            value = EmUnderscore.Replace(value, "<em>$1</em>");

            return value;
        }

        private static bool IsBlank(string line)
        {
            return string.IsNullOrWhiteSpace(line);
        }

        private static bool IsBlockStart(string line)
        {
            return MatchFence(line) != null
                || IsHorizontalRule(line)
                || HeadingLevel(line) > 0
                || IsQuote(line)
                || IsUnorderedItem(line)
                || IsOrderedItem(line);
        }

        private static string MatchFence(string line)
        {
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                return "```";
            }

            return trimmed.StartsWith("~~~", StringComparison.Ordinal) ? "~~~" : null;
        }

        private static bool IsHorizontalRule(string line)
        {
            var trimmed = line.Trim();

            if (trimmed.Length < 3)
            {
                return false;
            }

            var first = trimmed[0];

            if (first != '-' && first != '*' && first != '_')
            {
                return false;
            }

            foreach (var ch in trimmed)
            {
                if (ch != first)
                {
                    return false;
                }
            }

            return true;
        }

        private static int HeadingLevel(string line)
        {
            var level = 0;

            while (level < line.Length && level < 6 && line[level] == '#')
            {
                level++;
            }

            if (level == 0 || level >= line.Length)
            {
                return 0;
            }

            return line[level] == ' ' ? level : 0;
        }

        private static bool IsQuote(string line)
        {
            return line.TrimStart().StartsWith(">", StringComparison.Ordinal);
        }

        private static bool IsUnorderedItem(string line)
        {
            var trimmed = line.TrimStart();

            if (trimmed.Length < 2)
            {
                return false;
            }

            var marker = trimmed[0];

            return (marker == '-' || marker == '*' || marker == '+') && trimmed[1] == ' ';
        }

        private static bool IsOrderedItem(string line)
        {
            var trimmed = line.TrimStart();
            var index = 0;

            while (index < trimmed.Length && char.IsDigit(trimmed[index]))
            {
                index++;
            }

            if (index == 0 || index + 1 >= trimmed.Length)
            {
                return false;
            }

            var marker = trimmed[index];

            return (marker == '.' || marker == ')') && trimmed[index + 1] == ' ';
        }

        private static string StripUnorderedMarker(string line)
        {
            return line.TrimStart().Substring(2).Trim();
        }

        private static string StripOrderedMarker(string line)
        {
            var trimmed = line.TrimStart();
            var index = 0;

            while (index < trimmed.Length && char.IsDigit(trimmed[index]))
            {
                index++;
            }

            return trimmed.Substring(index + 1).Trim();
        }

        #endregion

        #region CSV

        /// <summary>猜测分隔符：在首行里数 , ; \t | 的出现次数，取最多者（都为 0 时用逗号）。</summary>
        public static char DetectDelimiter(string text)
        {
            var normalized = ElViewerGrowEngine.NormalizeNewLines(text);

            if (string.IsNullOrEmpty(normalized))
            {
                return ',';
            }

            var firstLine = normalized.Split('\n')[0];
            var best = ',';
            var bestCount = 0;

            foreach (var candidate in new[] { ',', ';', '\t', '|' })
            {
                var count = 0;

                foreach (var ch in firstLine)
                {
                    if (ch == candidate)
                    {
                        count++;
                    }
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    best = candidate;
                }
            }

            return best;
        }

        /// <summary>按 RFC 4180 的规则解析（支持引号包裹、引号内换行与双写引号转义）。</summary>
        public static List<List<string>> ParseCsv(string text, char delimiter)
        {
            var rows = new List<List<string>>();
            var normalized = ElViewerGrowEngine.NormalizeNewLines(text ?? string.Empty);
            var row = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;
            var index = 0;

            while (index < normalized.Length)
            {
                var ch = normalized[index];

                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (index + 1 < normalized.Length && normalized[index + 1] == '"')
                        {
                            field.Append('"');
                            index += 2;
                            continue;
                        }

                        inQuotes = false;
                        index++;
                        continue;
                    }

                    field.Append(ch);
                    index++;
                    continue;
                }

                if (ch == '"' && field.Length == 0)
                {
                    inQuotes = true;
                    index++;
                    continue;
                }

                if (ch == delimiter)
                {
                    row.Add(field.ToString());
                    field.Length = 0;
                    index++;
                    continue;
                }

                if (ch == '\n')
                {
                    row.Add(field.ToString());
                    field.Length = 0;
                    rows.Add(row);
                    row = new List<string>();
                    index++;
                    continue;
                }

                field.Append(ch);
                index++;
            }

            row.Add(field.ToString());

            if (row.Count > 1 || (row.Count == 1 && row[0].Length > 0))
            {
                rows.Add(row);
            }

            return rows;
        }

        /// <summary>渲染 CSV / TSV 为表格；<paramref name="delimiter"/> 为空时自动探测。</summary>
        public static string RenderCsv(string text, char? delimiter, bool firstRowIsHeader)
        {
            var separator = delimiter ?? DetectDelimiter(text);
            var rows = ParseCsv(text, separator);

            if (rows.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            builder.Append("<div class=\"el-viewer-grow__doc-table-wrap\"><table class=\"el-viewer-grow__doc-table\">");

            var bodyStart = 0;

            if (firstRowIsHeader && rows.Count > 1)
            {
                builder.Append("<thead><tr>");

                foreach (var cell in rows[0])
                {
                    builder.Append("<th class=\"el-viewer-grow__doc-th\">").Append(EscapeHtml(cell)).Append("</th>");
                }

                builder.Append("</tr></thead>");
                bodyStart = 1;
            }

            builder.Append("<tbody>");

            for (var r = bodyStart; r < rows.Count; r++)
            {
                builder.Append("<tr class=\"el-viewer-grow__doc-tr\">");

                foreach (var cell in rows[r])
                {
                    builder.Append("<td class=\"el-viewer-grow__doc-td\">").Append(EscapeHtml(cell)).Append("</td>");
                }

                builder.Append("</tr>");
            }

            builder.Append("</tbody></table></div>");

            return builder.ToString();
        }

        #endregion

        #region 媒体与嵌入

        /// <summary>图片。</summary>
        public static string RenderImage(string source, string title)
        {
            return "<img class=\"el-viewer-grow__doc-image\" src=\"" + EscapeHtml(source)
                + "\" alt=\"" + EscapeHtml(title) + "\" />";
        }

        /// <summary>视频。</summary>
        public static string RenderVideo(string source, string contentType)
        {
            return "<video class=\"el-viewer-grow__doc-video\" src=\"" + EscapeHtml(source)
                + "\" controls preload=\"metadata\"></video>";
        }

        /// <summary>音频。</summary>
        public static string RenderAudio(string source)
        {
            return "<audio class=\"el-viewer-grow__doc-audio\" src=\"" + EscapeHtml(source)
                + "\" controls preload=\"metadata\"></audio>";
        }

        /// <summary>PDF 等交给浏览器内建能力的文档：使用 iframe 嵌入。</summary>
        public static string RenderEmbed(string source, string title)
        {
            return "<iframe class=\"el-viewer-grow__doc-frame\" src=\"" + EscapeHtml(source)
                + "\" title=\"" + EscapeHtml(title) + "\"></iframe>";
        }

        #endregion
    }
}
