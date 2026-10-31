using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ElementPlus
{
    /// <summary>
    /// 自写的 Markdown 解析器（CommonMark 常用子集 + GFM 表格/删除线/任务列表）。
    /// 零外部依赖：不使用 Markdig、也不引入参考实现的任何代码。
    /// <para>块级：ATX/setext 标题、围栏与缩进代码块、引用块、有序/无序/嵌套列表、GFM 管道表格、分隔线、HTML 块、段落、引用式链接定义。</para>
    /// <para>行内：转义、代码跨、斜体/加粗/删除线、行内链接与图片、引用式链接、自动链接、裸 URL、硬/软换行、emoji 短代码、HTML 行内、占位标记。</para>
    /// </summary>
    public static class ElMarkdownGrowParser
    {
        /// <summary>占位标记的起始字符（Unicode 私用区，正常文本不会出现）。</summary>
        public const char OutletStart = '\uE000';

        /// <summary>占位标记的结束字符。</summary>
        public const char OutletEnd = '\uE001';

        private static readonly Regex AtxHeading = new Regex(@"^( {0,3})(#{1,6})(?:[ \t]+(.*?))?[ \t]*$", RegexOptions.Compiled);
        private static readonly Regex SetextUnderline = new Regex(@"^ {0,3}(=+|-+)[ \t]*$", RegexOptions.Compiled);
        private static readonly Regex Fence = new Regex(@"^( {0,3})(`{3,}|~{3,})[ \t]*(.*)$", RegexOptions.Compiled);
        private static readonly Regex ListMarker = new Regex(@"^( {0,3})([-+*]|\d{1,9}[.)])([ \t]+|$)(.*)$", RegexOptions.Compiled);
        private static readonly Regex TaskMarker = new Regex(@"^\[([ xX])\][ \t]+(.*)$", RegexOptions.Compiled);
        private static readonly Regex LinkDefinition = new Regex(@"^ {0,3}\[([^\]]+)\]:[ \t]*(\S+)(?:[ \t]+[""'(]([^""')]*)[""')])?[ \t]*$", RegexOptions.Compiled);
        private static readonly Regex OutletLine = new Regex("^" + OutletStart + @"(\d+)" + OutletEnd + "$", RegexOptions.Compiled);
        private static readonly Regex EmojiShortcode = new Regex(@"^:([a-zA-Z0-9_+-]{1,40}):", RegexOptions.Compiled);
        private static readonly Regex UrlScheme = new Regex(@"^([a-zA-Z][a-zA-Z0-9+.\-]{1,31}):([^\s<>]*)$", RegexOptions.Compiled);
        private static readonly Regex EmailAddress = new Regex(@"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);
        private static readonly Regex BareUrl = new Regex(@"(?<![\w""'/])(https?://[^\s<>""']+|www\.[^\s<>""']+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex HtmlTagName = new Regex(@"^</?([a-zA-Z][a-zA-Z0-9]*)([ \t/>]|$)", RegexOptions.Compiled);

        private static readonly char[] TrailingUrlTrim = { '.', ',', ';', ':', '!', '?', ')', ']', '}', '>', '"', '\'', '。', '，', '；', '：', '！', '？', '）', '】', '、' };

        /// <summary>块级 HTML 标签（用于识别 HTML 块起始）。</summary>
        private static readonly HashSet<string> BlockTags = new HashSet<string>(StringComparer.Ordinal)
        {
            "address", "article", "aside", "blockquote", "details", "dialog", "dd", "div", "dl", "dt",
            "fieldset", "figcaption", "figure", "footer", "form", "h1", "h2", "h3", "h4", "h5", "h6",
            "header", "hr", "iframe", "legend", "li", "main", "menu", "nav", "ol", "p", "pre", "script",
            "section", "style", "summary", "table", "tbody", "td", "tfoot", "th", "thead", "tr", "ul",
            "center", "dir", "noscript", "object", "embed", "applet", "frame", "frameset", "title", "textarea", "head", "body", "html"
        };

        /// <summary>解析 Markdown 文本。</summary>
        /// <param name="markdown">Markdown 源文本。</param>
        /// <param name="options">解析选项（用于决定是否保留 HTML）。</param>
        public static MdDocument Parse(string markdown, ElMarkdownGrowOptions options = null)
        {
            options = options ?? new ElMarkdownGrowOptions();

            var document = new MdDocument();

            if (string.IsNullOrEmpty(markdown))
            {
                return document;
            }

            var lines = SplitLines(markdown);
            var context = new Context(options);
            CollectLinkDefinitions(lines, context);

            document.Blocks.AddRange(ParseBlocks(lines, 0, lines.Count, context));

            return document;
        }

        /// <summary>解析行内 Markdown（供外部单独调用，便于测试）。</summary>
        public static List<MdInline> ParseInlines(string text, ElMarkdownGrowOptions options = null)
        {
            options = options ?? new ElMarkdownGrowOptions();
            var context = new Context(options);

            return ParseInlines(text ?? string.Empty, context, true);
        }

        /// <summary>生成占位标记字符串。</summary>
        public static string CreateOutletMarker(int id)
        {
            return OutletStart + id.ToString(CultureInfo.InvariantCulture) + OutletEnd;
        }

        /// <summary>判断某一行是否为独占一行的占位标记。</summary>
        public static bool TryParseOutletLine(string line, out int id)
        {
            id = -1;

            if (string.IsNullOrEmpty(line))
            {
                return false;
            }

            var match = OutletLine.Match(line.Trim());

            if (!match.Success)
            {
                return false;
            }

            return int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id);
        }

        #region 块级解析

        private static List<MdBlock> ParseBlocks(List<string> lines, int start, int end, Context context)
        {
            var blocks = new List<MdBlock>();
            var index = start;

            while (index < end)
            {
                var line = lines[index];

                if (IsBlank(line))
                {
                    index++;
                    continue;
                }

                if (TryParseOutletLine(line, out var outletId))
                {
                    blocks.Add(new MdOutletBlock { Id = outletId });
                    index++;
                    continue;
                }

                // 围栏代码块
                var fence = Fence.Match(line);
                if (fence.Success)
                {
                    var fenceText = fence.Groups[2].Value;
                    var fenceChar = fenceText[0];
                    var fenceLength = fenceText.Length;
                    var info = fence.Groups[3].Value.Trim();
                    var code = new StringBuilder();
                    var closed = false;
                    index++;

                    while (index < end)
                    {
                        var candidate = lines[index];
                        var closing = Fence.Match(candidate);

                        if (closing.Success
                            && closing.Groups[2].Value[0] == fenceChar
                            && closing.Groups[2].Value.Length >= fenceLength
                            && string.IsNullOrWhiteSpace(closing.Groups[3].Value))
                        {
                            closed = true;
                            index++;
                            break;
                        }

                        if (code.Length > 0)
                        {
                            code.Append('\n');
                        }

                        code.Append(candidate);
                        index++;
                    }

                    _ = closed;
                    blocks.Add(new MdCodeBlock { Fenced = true, Info = info, Code = code.ToString() });
                    continue;
                }

                // ATX 标题
                var atx = AtxHeading.Match(line);
                if (atx.Success)
                {
                    var content = atx.Groups[3].Success ? atx.Groups[3].Value : string.Empty;
                    content = StripClosingHashes(content);
                    blocks.Add(CreateHeading(atx.Groups[2].Value.Length, content, context));
                    index++;
                    continue;
                }

                // 分隔线
                if (IsThematicBreak(line))
                {
                    blocks.Add(new MdThematicBreak());
                    index++;
                    continue;
                }

                // 引用块
                if (IsBlockQuoteLine(line))
                {
                    var inner = new List<string>();
                    var lastWasQuote = false;

                    while (index < end)
                    {
                        var candidate = lines[index];

                        if (IsBlockQuoteLine(candidate))
                        {
                            inner.Add(StripBlockQuoteMarker(candidate));
                            lastWasQuote = true;
                            index++;
                            continue;
                        }

                        if (IsBlank(candidate) && lastWasQuote)
                        {
                            inner.Add(string.Empty);
                            lastWasQuote = false;
                            index++;
                            continue;
                        }

                        // 惰性续行
                        if (lastWasQuote && !IsBlank(candidate) && !StartsNewBlock(candidate, context.Options.AllowHtml))
                        {
                            inner.Add(candidate);
                            index++;
                            continue;
                        }

                        break;
                    }

                    var quote = new MdBlockQuote();
                    quote.Blocks.AddRange(ParseBlocks(inner, 0, inner.Count, context));
                    blocks.Add(quote);
                    continue;
                }

                // 列表
                if (ListMarker.IsMatch(line))
                {
                    var list = ParseList(lines, ref index, end, context);
                    blocks.Add(list);
                    continue;
                }

                // 表格
                if (index + 1 < end && line.IndexOf('|') >= 0 && IsTableDelimiter(lines[index + 1]))
                {
                    var table = ParseTable(lines, ref index, end, context);
                    blocks.Add(table);
                    continue;
                }

                // 缩进代码块
                if (CountIndent(line) >= 4)
                {
                    var code = new StringBuilder();
                    code.Append(StripIndent(line, 4));
                    index++;

                    while (index < end && (CountIndent(lines[index]) >= 4 || IsBlank(lines[index])))
                    {
                        if (IsBlank(lines[index]))
                        {
                            // 后跟非缩进行则结束
                            var lookAhead = index + 1;

                            while (lookAhead < end && IsBlank(lines[lookAhead]))
                            {
                                lookAhead++;
                            }

                            if (lookAhead >= end || CountIndent(lines[lookAhead]) < 4)
                            {
                                break;
                            }

                            code.Append('\n');
                            index++;
                            continue;
                        }

                        if (code.Length > 0)
                        {
                            code.Append('\n');
                        }

                        code.Append(StripIndent(lines[index], 4));
                        index++;
                    }

                    blocks.Add(new MdCodeBlock { Fenced = false, Info = string.Empty, Code = code.ToString() });
                    continue;
                }

                // HTML 块
                if (context.Options.AllowHtml && TryGetHtmlBlockTag(line, out var htmlTag))
                {
                    var html = new StringBuilder(line.TrimStart());
                    index++;

                    while (index < end)
                    {
                        var candidate = lines[index];

                        if (IsBlank(candidate))
                        {
                            break;
                        }

                        html.Append('\n');
                        html.Append(candidate);
                        index++;
                    }

                    _ = htmlTag;
                    blocks.Add(new MdHtmlBlock { Html = html.ToString() });
                    continue;
                }

                // 段落（含 setext 标题判定）
                blocks.Add(ParseParagraph(lines, ref index, end, context));
            }

            return blocks;
        }

        private static MdBlock ParseParagraph(List<string> lines, ref int index, int end, Context context)
        {
            var buffer = new List<string>();
            var setextLevel = 0;

            while (index < end)
            {
                var line = lines[index];

                if (IsBlank(line))
                {
                    break;
                }

                // setext 下划线（仅在已有内容时生效）
                if (buffer.Count > 0 && SetextUnderline.IsMatch(line))
                {
                    setextLevel = line.Trim()[0] == '=' ? 1 : 2;
                    index++;
                    break;
                }

                if (buffer.Count > 0 && StartsNewBlock(line, context.Options.AllowHtml))
                {
                    break;
                }

                // 保留行尾空格（硬换行判定依赖它），只去掉行首缩进
                buffer.Add(line.TrimStart());
                index++;
            }

            var text = string.Join("\n", buffer);

            if (setextLevel > 0)
            {
                return CreateHeading(setextLevel, text, context);
            }

            var paragraph = new MdParagraph();
            paragraph.Inlines.AddRange(ParseInlines(text, context, true));

            return paragraph;
        }

        private static MdHeading CreateHeading(int level, string content, Context context)
        {
            var heading = new MdHeading { Level = level < 1 ? 1 : (level > 6 ? 6 : level) };
            heading.Inlines.AddRange(ParseInlines(content, context, true));
            heading.Text = MdAst.ToPlainText(heading.Inlines);

            if (context.Options.AutoLinkHeadingDepth >= heading.Level)
            {
                heading.Anchor = CreateAnchor(heading.Text);
            }

            return heading;
        }

        private static MdList ParseList(List<string> lines, ref int index, int end, Context context)
        {
            var first = ListMarker.Match(lines[index]);
            var ordered = char.IsDigit(first.Groups[2].Value[0]);
            var bullet = ordered ? '\0' : first.Groups[2].Value[0];
            var startNumber = ordered
                ? int.Parse(first.Groups[2].Value.TrimEnd('.', ')'), CultureInfo.InvariantCulture)
                : 1;
            var listIndent = first.Groups[1].Value.Length;

            var list = new MdList { Ordered = ordered, Start = startNumber, Tight = true };
            var sawBlank = false;

            while (index < end)
            {
                var line = lines[index];

                if (IsBlank(line))
                {
                    // 列表项之间的空行
                    sawBlank = true;
                    index++;

                    // 空行后若不再是本列表的项则结束
                    if (index >= end || !ListMarker.IsMatch(lines[index]))
                    {
                        break;
                    }

                    continue;
                }

                var marker = ListMarker.Match(line);

                if (!marker.Success)
                {
                    break;
                }

                var itemOrdered = char.IsDigit(marker.Groups[2].Value[0]);
                var itemIndent = marker.Groups[1].Value.Length;
                var itemBullet = itemOrdered ? '\0' : marker.Groups[2].Value[0];

                if (itemOrdered != ordered || (!ordered && itemBullet != bullet) || itemIndent > listIndent + 1)
                {
                    break;
                }

                var markerLength = marker.Groups[2].Value.Length;
                var spacing = marker.Groups[3].Value.Length;
                var contentIndent = itemIndent + markerLength + (spacing > 4 || spacing == 0 ? 1 : spacing);
                var contentLines = new List<string> { marker.Groups[4].Value };
                index++;

                var blankRun = 0;

                while (index < end)
                {
                    var candidate = lines[index];

                    if (IsBlank(candidate))
                    {
                        var lookAhead = index + 1;

                        while (lookAhead < end && IsBlank(lines[lookAhead]))
                        {
                            lookAhead++;
                        }

                        if (lookAhead >= end)
                        {
                            index = end;
                            break;
                        }

                        var next = lines[lookAhead];

                        // 下一个列表项，或继续缩进的续行
                        if (ListMarker.IsMatch(next) && CountIndent(next) <= itemIndent)
                        {
                            break;
                        }

                        if (CountIndent(next) < contentIndent)
                        {
                            break;
                        }

                        blankRun++;
                        contentLines.Add(string.Empty);
                        index++;
                        continue;
                    }

                    if (ListMarker.IsMatch(candidate) && CountIndent(candidate) <= itemIndent)
                    {
                        break;
                    }

                    if (CountIndent(candidate) >= contentIndent)
                    {
                        contentLines.Add(StripIndent(candidate, contentIndent));
                        index++;
                        continue;
                    }

                    // 惰性续行
                    if (!StartsNewBlock(candidate, context.Options.AllowHtml))
                    {
                        contentLines.Add(candidate.Trim());
                        index++;
                        continue;
                    }

                    break;
                }

                if (blankRun > 0)
                {
                    sawBlank = true;
                }

                var item = new MdListItem();

                // 任务列表
                if (contentLines.Count > 0)
                {
                    var task = TaskMarker.Match(contentLines[0]);

                    if (task.Success)
                    {
                        item.Checked = task.Groups[1].Value != " ";
                        contentLines[0] = task.Groups[2].Value;
                    }
                }

                item.Blocks.AddRange(ParseBlocks(contentLines, 0, contentLines.Count, context));
                list.Blocks.Add(item);
            }

            // 只有列表项之间存在空行时才是宽松列表
            list.Tight = !sawBlank;

            return list;
        }

        private static MdTable ParseTable(List<string> lines, ref int index, int end, Context context)
        {
            var table = new MdTable();

            foreach (var part in SplitTableRow(lines[index + 1]))
            {
                var trimmed = part.Trim();

                if (trimmed.StartsWith(":", StringComparison.Ordinal) && trimmed.EndsWith(":", StringComparison.Ordinal))
                {
                    table.Aligns.Add(MdTableAlign.Center);
                }
                else if (trimmed.EndsWith(":", StringComparison.Ordinal))
                {
                    table.Aligns.Add(MdTableAlign.Right);
                }
                else if (trimmed.StartsWith(":", StringComparison.Ordinal))
                {
                    table.Aligns.Add(MdTableAlign.Left);
                }
                else
                {
                    table.Aligns.Add(MdTableAlign.None);
                }
            }

            var header = new MdTableRow { IsHeader = true };
            AddTableCells(header, lines[index], table.Aligns.Count, context);
            table.Rows.Add(header);

            index += 2;

            while (index < end)
            {
                var line = lines[index];

                if (IsBlank(line) || line.IndexOf('|') < 0)
                {
                    break;
                }

                var row = new MdTableRow();
                AddTableCells(row, line, table.Aligns.Count, context);
                table.Rows.Add(row);
                index++;
            }

            return table;
        }

        private static void AddTableCells(MdTableRow row, string line, int columnCount, Context context)
        {
            var cells = SplitTableRow(line);

            for (var i = 0; i < columnCount; i++)
            {
                var text = i < cells.Count ? cells[i].Trim() : string.Empty;
                row.Cells.Add(ParseInlines(text, context, true));
            }
        }

        private static List<string> SplitTableRow(string line)
        {
            var text = line.Trim();

            if (text.StartsWith("|", StringComparison.Ordinal))
            {
                text = text.Substring(1);
            }

            if (text.EndsWith("|", StringComparison.Ordinal) && !text.EndsWith("\\|", StringComparison.Ordinal))
            {
                text = text.Substring(0, text.Length - 1);
            }

            var cells = new List<string>();
            var current = new StringBuilder();

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (c == '\\' && i + 1 < text.Length && text[i + 1] == '|')
                {
                    current.Append('|');
                    i++;
                    continue;
                }

                if (c == '|')
                {
                    cells.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(c);
            }

            cells.Add(current.ToString());

            return cells;
        }

        private static bool IsTableDelimiter(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            var text = line.Trim();

            if (text.IndexOf('|') < 0)
            {
                return false;
            }

            foreach (var part in SplitTableRow(text))
            {
                var cell = part.Trim();

                if (cell.Length == 0)
                {
                    continue;
                }

                var body = cell.Trim(':');

                if (body.Length == 0)
                {
                    return false;
                }

                foreach (var c in body)
                {
                    if (c != '-')
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool TryGetHtmlBlockTag(string line, out string tag)
        {
            tag = null;

            if (CountIndent(line) >= 4)
            {
                return false;
            }

            var text = line.TrimStart();

            if (text.Length < 2 || text[0] != '<')
            {
                return false;
            }

            if (text.StartsWith("<!--", StringComparison.Ordinal)
                || text.StartsWith("<!", StringComparison.Ordinal)
                || text.StartsWith("<?", StringComparison.Ordinal))
            {
                return false;
            }

            var match = HtmlTagName.Match(text);

            if (!match.Success)
            {
                return false;
            }

            var name = match.Groups[1].Value.ToLowerInvariant();

            if (!BlockTags.Contains(name))
            {
                return false;
            }

            tag = name;

            return true;
        }

        private static bool StartsNewBlock(string line, bool allowHtml)
        {
            if (IsBlank(line))
            {
                return true;
            }

            if (Fence.IsMatch(line) || AtxHeading.IsMatch(line) || IsThematicBreak(line))
            {
                return true;
            }

            if (IsBlockQuoteLine(line) || ListMarker.IsMatch(line))
            {
                return true;
            }

            return allowHtml && TryGetHtmlBlockTag(line, out _);
        }

        private static bool IsThematicBreak(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            var text = line.Trim();
            var marker = '\0';
            var count = 0;

            foreach (var c in text)
            {
                if (c == ' ' || c == '\t')
                {
                    continue;
                }

                if (marker == '\0')
                {
                    if (c != '*' && c != '-' && c != '_')
                    {
                        return false;
                    }

                    marker = c;
                }
                else if (c != marker)
                {
                    return false;
                }

                count++;
            }

            return count >= 3;
        }

        private static bool IsBlockQuoteLine(string line)
        {
            var index = 0;

            while (index < line.Length && index < 3 && line[index] == ' ')
            {
                index++;
            }

            return index < line.Length && line[index] == '>';
        }

        private static string StripBlockQuoteMarker(string line)
        {
            var index = 0;

            while (index < line.Length && index < 3 && line[index] == ' ')
            {
                index++;
            }

            if (index < line.Length && line[index] == '>')
            {
                index++;

                if (index < line.Length && line[index] == ' ')
                {
                    index++;
                }
            }

            return line.Substring(index);
        }

        private static string StripClosingHashes(string content)
        {
            var trimmed = content.TrimEnd();

            if (!trimmed.EndsWith("#", StringComparison.Ordinal))
            {
                return content.Trim();
            }

            var index = trimmed.Length;

            while (index > 0 && trimmed[index - 1] == '#')
            {
                index--;
            }

            if (index > 0 && (trimmed[index - 1] == ' ' || trimmed[index - 1] == '\t'))
            {
                return trimmed.Substring(0, index).Trim();
            }

            return content.Trim();
        }

        private static bool IsBlank(string line)
        {
            return string.IsNullOrWhiteSpace(line);
        }

        private static int CountIndent(string line)
        {
            var count = 0;

            while (count < line.Length && line[count] == ' ')
            {
                count++;
            }

            return count;
        }

        private static string StripIndent(string line, int count)
        {
            var removed = 0;

            while (removed < line.Length && removed < count && line[removed] == ' ')
            {
                removed++;
            }

            return line.Substring(removed);
        }

        private static List<string> SplitLines(string text)
        {
            var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\t", "    ");

            return new List<string>(normalized.Split('\n'));
        }

        private static void CollectLinkDefinitions(List<string> lines, Context context)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                var match = LinkDefinition.Match(lines[i]);

                if (!match.Success)
                {
                    continue;
                }

                var label = NormalizeLabel(match.Groups[1].Value);

                if (label.Length == 0 || context.Definitions.ContainsKey(label))
                {
                    continue;
                }

                var title = match.Groups[3].Success ? match.Groups[3].Value : null;
                context.Definitions[label] = (match.Groups[2].Value, title);

                // 定义行不产生输出
                lines[i] = string.Empty;
            }
        }

        private static string NormalizeLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(label.Length);
            var previousWasSpace = false;

            foreach (var c in label.Trim())
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!previousWasSpace)
                    {
                        builder.Append(' ');
                        previousWasSpace = true;
                    }

                    continue;
                }

                builder.Append(char.ToLowerInvariant(c));
                previousWasSpace = false;
            }

            return builder.ToString();
        }

        #endregion

        #region 行内解析

        private static List<MdInline> ParseInlines(string text, Context context, bool allowAutoLink)
        {
            var nodes = new List<MdInline>();

            if (string.IsNullOrEmpty(text))
            {
                return nodes;
            }

            var buffer = new StringBuilder();
            var index = 0;

            void Flush()
            {
                if (buffer.Length == 0)
                {
                    return;
                }

                var value = buffer.ToString();
                buffer.Clear();

                if (allowAutoLink && context.Options.EnableAutoLinkUrl)
                {
                    AppendTextWithAutoLinks(nodes, value);
                }
                else
                {
                    nodes.Add(new MdText(value));
                }
            }

            while (index < text.Length)
            {
                var c = text[index];

                // 转义
                if (c == '\\' && index + 1 < text.Length && IsEscapable(text[index + 1]))
                {
                    buffer.Append(text[index + 1]);
                    index += 2;
                    continue;
                }

                // 硬换行（行尾反斜杠）
                if (c == '\\' && index + 1 < text.Length && text[index + 1] == '\n')
                {
                    Flush();
                    nodes.Add(new MdLineBreak { Hard = true });
                    index += 2;
                    continue;
                }

                // 占位标记
                if (c == OutletStart)
                {
                    var end = text.IndexOf(OutletEnd, index + 1);

                    if (end > index && int.TryParse(text.Substring(index + 1, end - index - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var outletId))
                    {
                        Flush();
                        nodes.Add(new MdInlineOutlet { Id = outletId });
                        index = end + 1;
                        continue;
                    }
                }

                // 换行
                if (c == '\n')
                {
                    var hard = false;

                    if (buffer.Length > 0)
                    {
                        while (buffer.Length > 0 && buffer[buffer.Length - 1] == ' ')
                        {
                            buffer.Length--;
                        }

                        hard = EndsWithTwoSpaces(text, index);
                    }
                    else
                    {
                        hard = EndsWithTwoSpaces(text, index);
                    }

                    Flush();
                    nodes.Add(new MdLineBreak { Hard = hard || context.Options.SoftBreakAsHard });
                    index++;
                    continue;
                }

                // 代码跨
                if (c == '`')
                {
                    if (TryParseCodeSpan(text, index, out var code, out var next))
                    {
                        Flush();
                        nodes.Add(new MdCodeSpan { Code = code });
                        index = next;
                        continue;
                    }
                }

                // 图片 / 链接
                if (c == '!' && index + 1 < text.Length && text[index + 1] == '[')
                {
                    if (TryParseImage(text, index, context, out var image, out var next))
                    {
                        Flush();
                        nodes.Add(image);
                        index = next;
                        continue;
                    }
                }

                if (c == '[')
                {
                    if (TryParseLink(text, index, context, out var link, out var next))
                    {
                        Flush();
                        nodes.Add(link);
                        index = next;
                        continue;
                    }
                }

                // 自动链接 / HTML 行内
                if (c == '<')
                {
                    if (TryParseAutoLink(text, index, out var autoLink, out var autoNext))
                    {
                        Flush();
                        nodes.Add(autoLink);
                        index = autoNext;
                        continue;
                    }

                    if (context.Options.AllowHtml && TryParseHtmlInline(text, index, out var html, out var htmlNext))
                    {
                        Flush();
                        nodes.Add(new MdHtmlInline { Html = html });
                        index = htmlNext;
                        continue;
                    }
                }

                // 强调 / 加粗
                if (c == '*' || c == '_')
                {
                    if (TryParseEmphasis(text, index, context, out var emphasis, out var emphasisNext))
                    {
                        Flush();
                        nodes.Add(emphasis);
                        index = emphasisNext;
                        continue;
                    }
                }

                // 删除线
                if (c == '~' && index + 1 < text.Length && text[index + 1] == '~')
                {
                    if (TryParseStrike(text, index, context, out var strike, out var strikeNext))
                    {
                        Flush();
                        nodes.Add(strike);
                        index = strikeNext;
                        continue;
                    }
                }

                // emoji 短代码
                if (c == ':' && context.Options.EnableEmoji)
                {
                    var match = EmojiShortcode.Match(text.Substring(index));

                    if (match.Success && ElMarkdownGrowEmoji.TryGet(match.Groups[1].Value, out var glyph))
                    {
                        Flush();
                        nodes.Add(new MdEmoji { Shortcode = match.Groups[1].Value, Glyph = glyph });
                        index += match.Length;
                        continue;
                    }
                }

                buffer.Append(c);
                index++;
            }

            Flush();

            return nodes;
        }

        private static bool EndsWithTwoSpaces(string text, int newlineIndex)
        {
            var spaces = 0;
            var index = newlineIndex - 1;

            while (index >= 0 && text[index] == ' ')
            {
                spaces++;
                index--;
            }

            return spaces >= 2;
        }

        private static bool IsEscapable(char c)
        {
            switch (c)
            {
                case '\\':
                case '`':
                case '*':
                case '_':
                case '{':
                case '}':
                case '[':
                case ']':
                case '(':
                case ')':
                case '#':
                case '+':
                case '-':
                case '.':
                case '!':
                case '~':
                case '|':
                case '>':
                case '<':
                case '&':
                case '"':
                case '\'':
                case '$':
                case '%':
                case '/':
                case ':':
                case ';':
                case '=':
                case '?':
                case '@':
                case '^':
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryParseCodeSpan(string text, int start, out string code, out int next)
        {
            code = null;
            next = start;

            var runLength = CountRun(text, start, '`');

            if (runLength == 0)
            {
                return false;
            }

            var search = start + runLength;

            while (search < text.Length)
            {
                var position = text.IndexOf('`', search);

                if (position < 0)
                {
                    return false;
                }

                var closingLength = CountRun(text, position, '`');

                if (closingLength == runLength)
                {
                    var content = text.Substring(start + runLength, position - start - runLength);
                    content = content.Replace("\n", " ");

                    if (content.Length > 2 && content[0] == ' ' && content[content.Length - 1] == ' ' && content.Trim().Length > 0)
                    {
                        content = content.Substring(1, content.Length - 2);
                    }

                    code = content;
                    next = position + closingLength;

                    return true;
                }

                search = position + closingLength;
            }

            return false;
        }

        private static bool TryParseEmphasis(string text, int start, Context context, out MdInline node, out int next)
        {
            node = null;
            next = start;

            var marker = text[start];
            var runLength = CountRun(text, start, marker);

            if (runLength == 0)
            {
                return false;
            }

            // 下划线不参与词内强调
            if (marker == '_' && start > 0 && IsWordChar(text[start - 1]))
            {
                return false;
            }

            if (start + runLength >= text.Length)
            {
                return false;
            }

            if (char.IsWhiteSpace(text[start + runLength]))
            {
                return false;
            }

            // 优先尝试较长的分隔符（***/---）
            var candidates = new List<int>();

            if (runLength >= 3)
            {
                candidates.Add(3);
            }

            if (runLength >= 2)
            {
                candidates.Add(2);
            }

            candidates.Add(1);

            foreach (var use in candidates)
            {
                if (use > runLength)
                {
                    continue;
                }

                var search = start + use;

                while (search < text.Length)
                {
                    var position = text.IndexOf(marker, search);

                    if (position < 0)
                    {
                        break;
                    }

                    var closingLength = CountRun(text, position, marker);

                    if (closingLength < use || position == start + use)
                    {
                        search = position + closingLength;
                        continue;
                    }

                    if (char.IsWhiteSpace(text[position - 1]))
                    {
                        search = position + closingLength;
                        continue;
                    }

                    if (marker == '_' && position + closingLength < text.Length && IsWordChar(text[position + closingLength]))
                    {
                        search = position + closingLength;
                        continue;
                    }

                    var content = text.Substring(start + use, position - start - use);
                    var children = ParseInlines(content, context, true);

                    if (use == 3)
                    {
                        var strong = new MdStrong();
                        strong.Inlines.AddRange(children);
                        var outer = new MdEmphasis();
                        outer.Inlines.Add(strong);
                        node = outer;
                    }
                    else if (use == 2)
                    {
                        var strong = new MdStrong();
                        strong.Inlines.AddRange(children);
                        node = strong;
                    }
                    else
                    {
                        var emphasis = new MdEmphasis();
                        emphasis.Inlines.AddRange(children);
                        node = emphasis;
                    }

                    next = position + use;

                    return true;
                }
            }

            return false;
        }

        private static bool TryParseStrike(string text, int start, Context context, out MdInline node, out int next)
        {
            node = null;
            next = start;

            if (start + 2 >= text.Length || char.IsWhiteSpace(text[start + 2]))
            {
                return false;
            }

            var search = start + 2;

            while (search < text.Length)
            {
                var position = text.IndexOf("~~", search, StringComparison.Ordinal);

                if (position < 0)
                {
                    return false;
                }

                if (position == start + 2 || char.IsWhiteSpace(text[position - 1]))
                {
                    search = position + 2;
                    continue;
                }

                var content = text.Substring(start + 2, position - start - 2);
                var strike = new MdStrike();
                strike.Inlines.AddRange(ParseInlines(content, context, true));
                node = strike;
                next = position + 2;

                return true;
            }

            return false;
        }

        private static bool TryParseLink(string text, int start, Context context, out MdLink link, out int next)
        {
            link = null;
            next = start;

            if (!TryFindLabelEnd(text, start, out var labelEnd))
            {
                return false;
            }

            var labelText = text.Substring(start + 1, labelEnd - start - 1);
            var after = labelEnd + 1;

            // 行内链接 [text](url "title")
            if (after < text.Length && text[after] == '(')
            {
                if (TryParseInlineDestination(text, after, out var url, out var title, out var end))
                {
                    link = new MdLink { Url = url, Title = title };
                    link.Inlines.AddRange(ParseInlines(labelText, context, false));
                    next = end;

                    return true;
                }
            }

            // 引用式链接 [text][ref] / [text][] / [text]
            var referenceLabel = (string)null;

            if (after < text.Length && text[after] == '[')
            {
                if (TryFindLabelEnd(text, after, out var refEnd))
                {
                    var explicitLabel = text.Substring(after + 1, refEnd - after - 1);
                    referenceLabel = string.IsNullOrWhiteSpace(explicitLabel) ? labelText : explicitLabel;
                    next = refEnd + 1;
                }
            }
            else
            {
                referenceLabel = labelText;
                next = after;
            }

            if (referenceLabel == null)
            {
                return false;
            }

            var key = NormalizeLabel(referenceLabel);

            if (!context.Definitions.TryGetValue(key, out var definition))
            {
                return false;
            }

            link = new MdLink { Url = definition.Url, Title = definition.Title };
            link.Inlines.AddRange(ParseInlines(labelText, context, false));

            return true;
        }

        private static bool TryParseImage(string text, int start, Context context, out MdImage image, out int next)
        {
            image = null;
            next = start;

            var bracket = start + 1;

            if (bracket >= text.Length || text[bracket] != '[')
            {
                return false;
            }

            if (!TryFindLabelEnd(text, bracket, out var labelEnd))
            {
                return false;
            }

            var alt = text.Substring(bracket + 1, labelEnd - bracket - 1);
            var after = labelEnd + 1;

            if (after < text.Length && text[after] == '(')
            {
                if (TryParseInlineDestination(text, after, out var url, out var title, out var end))
                {
                    image = new MdImage { Url = url, Title = title, Alt = StripMarkup(alt) };
                    next = end;

                    return true;
                }

                return false;
            }

            // 引用式图片
            var referenceLabel = alt;
            var nextIndex = after;

            if (after < text.Length && text[after] == '[')
            {
                if (!TryFindLabelEnd(text, after, out var refEnd))
                {
                    return false;
                }

                var explicitLabel = text.Substring(after + 1, refEnd - after - 1);
                referenceLabel = string.IsNullOrWhiteSpace(explicitLabel) ? alt : explicitLabel;
                nextIndex = refEnd + 1;
            }

            var key = NormalizeLabel(referenceLabel);

            if (!context.Definitions.TryGetValue(key, out var definition))
            {
                return false;
            }

            image = new MdImage { Url = definition.Url, Title = definition.Title, Alt = StripMarkup(alt) };
            next = nextIndex;

            return true;
        }

        private static bool TryParseInlineDestination(string text, int openParen, out string url, out string title, out int end)
        {
            url = null;
            title = null;
            end = openParen;

            var index = openParen + 1;

            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            if (index >= text.Length)
            {
                return false;
            }

            var builder = new StringBuilder();

            if (text[index] == '<')
            {
                index++;

                while (index < text.Length && text[index] != '>' && text[index] != '\n')
                {
                    builder.Append(text[index]);
                    index++;
                }

                if (index >= text.Length || text[index] != '>')
                {
                    return false;
                }

                index++;
            }
            else
            {
                var depth = 0;

                while (index < text.Length)
                {
                    var c = text[index];

                    if (c == '\\' && index + 1 < text.Length)
                    {
                        builder.Append(text[index + 1]);
                        index += 2;
                        continue;
                    }

                    if (c == '(')
                    {
                        depth++;
                    }
                    else if (c == ')')
                    {
                        if (depth == 0)
                        {
                            break;
                        }

                        depth--;
                    }
                    else if (char.IsWhiteSpace(c))
                    {
                        break;
                    }

                    builder.Append(c);
                    index++;
                }
            }

            url = builder.ToString();

            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            if (index < text.Length && text[index] != ')')
            {
                var quote = text[index];
                var closing = quote == '(' ? ')' : quote;

                if (quote == '"' || quote == '\'' || quote == '(')
                {
                    var titleStart = ++index;
                    var titleBuilder = new StringBuilder();

                    while (index < text.Length && text[index] != closing)
                    {
                        titleBuilder.Append(text[index]);
                        index++;
                    }

                    if (index >= text.Length)
                    {
                        return false;
                    }

                    title = titleBuilder.ToString();
                    index++;

                    while (index < text.Length && char.IsWhiteSpace(text[index]))
                    {
                        index++;
                    }

                    _ = titleStart;
                }
            }

            if (index >= text.Length || text[index] != ')')
            {
                return false;
            }

            end = index + 1;

            return true;
        }

        private static bool TryFindLabelEnd(string text, int openBracket, out int labelEnd)
        {
            labelEnd = -1;

            if (openBracket >= text.Length || text[openBracket] != '[')
            {
                return false;
            }

            var depth = 0;

            for (var i = openBracket; i < text.Length; i++)
            {
                var c = text[i];

                if (c == '\\')
                {
                    i++;
                    continue;
                }

                if (c == '[')
                {
                    depth++;
                }
                else if (c == ']')
                {
                    depth--;

                    if (depth == 0)
                    {
                        labelEnd = i;

                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryParseAutoLink(string text, int start, out MdLink link, out int next)
        {
            link = null;
            next = start;

            var end = text.IndexOf('>', start + 1);

            if (end < 0)
            {
                return false;
            }

            var inner = text.Substring(start + 1, end - start - 1);

            if (inner.Length == 0 || inner.IndexOf('<') >= 0 || inner.IndexOf(' ') >= 0)
            {
                return false;
            }

            var scheme = UrlScheme.Match(inner);

            if (scheme.Success)
            {
                var protocol = scheme.Groups[1].Value.ToLowerInvariant();

                if (protocol == "javascript" || protocol == "vbscript" || protocol == "data")
                {
                    return false;
                }

                link = new MdLink { Url = inner };
                link.Inlines.Add(new MdText(inner));
                next = end + 1;

                return true;
            }

            if (EmailAddress.IsMatch(inner))
            {
                link = new MdLink { Url = "mailto:" + inner };
                link.Inlines.Add(new MdText(inner));
                next = end + 1;

                return true;
            }

            return false;
        }

        private static bool TryParseHtmlInline(string text, int start, out string html, out int next)
        {
            html = null;
            next = start;

            // 注释
            if (text.Length > start + 3 && text.Substring(start, 4) == "<!--")
            {
                var commentEnd = text.IndexOf("-->", start + 4, StringComparison.Ordinal);

                if (commentEnd < 0)
                {
                    return false;
                }

                html = text.Substring(start, commentEnd + 3 - start);
                next = commentEnd + 3;

                return true;
            }

            // 声明
            if (text.Length > start + 1 && (text[start + 1] == '!' || text[start + 1] == '?'))
            {
                var declarationEnd = text.IndexOf('>', start + 1);

                if (declarationEnd < 0)
                {
                    return false;
                }

                html = text.Substring(start, declarationEnd + 1 - start);
                next = declarationEnd + 1;

                return true;
            }

            var match = HtmlTagName.Match(text.Substring(start));

            if (!match.Success)
            {
                return false;
            }

            var end = FindTagEnd(text, start);

            if (end < 0)
            {
                return false;
            }

            html = text.Substring(start, end + 1 - start);
            next = end + 1;

            return true;
        }

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

        private static void AppendTextWithAutoLinks(List<MdInline> nodes, string value)
        {
            var index = 0;

            while (index < value.Length)
            {
                var match = BareUrl.Match(value, index);

                if (!match.Success)
                {
                    nodes.Add(new MdText(value.Substring(index)));
                    return;
                }

                if (match.Index > index)
                {
                    nodes.Add(new MdText(value.Substring(index, match.Index - index)));
                }

                var url = match.Value.TrimEnd(TrailingUrlTrim);

                if (url.Length == 0)
                {
                    nodes.Add(new MdText(match.Value));
                    index = match.Index + match.Value.Length;
                    continue;
                }

                var href = url.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? "http://" + url : url;
                var link = new MdLink { Url = href };
                link.Inlines.Add(new MdText(url));
                nodes.Add(link);

                index = match.Index + url.Length;
            }
        }

        private static string StripMarkup(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(text.Length);

            foreach (var c in text)
            {
                if (c == '*' || c == '_' || c == '`' || c == '~')
                {
                    continue;
                }

                builder.Append(c);
            }

            return builder.ToString();
        }

        private static int CountRun(string text, int start, char c)
        {
            var count = 0;

            while (start + count < text.Length && text[start + count] == c)
            {
                count++;
            }

            return count;
        }

        private static bool IsWordChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        #endregion

        /// <summary>生成标题锚点（对齐参考实现：去除 <c>\w \s -</c> 之外的字符 → 空格转连字符 → 小写）。</summary>
        public static string CreateAnchor(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(text.Length);

            foreach (var c in text)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || char.IsWhiteSpace(c))
                {
                    builder.Append(c == ' ' ? '-' : c);
                }
            }

            return builder.ToString().ToLowerInvariant().Trim();
        }

        private class Context
        {
            public Context(ElMarkdownGrowOptions options)
            {
                Options = options;
            }

            public ElMarkdownGrowOptions Options { get; }

            public Dictionary<string, (string Url, string Title)> Definitions { get; } = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
        }
    }
}
