using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ElementPlus
{
    /// <summary>
    /// Markdown 渲染输出的分段。
    /// 当 Markdown 来自 <c>ChildContent</c> 且其中嵌入了元素/组件时，输出会被拆成若干段：
    /// HTML 段与占位段交替出现，占位段由组件填充对应的渲染片段。
    /// </summary>
    public class ElMarkdownGrowSegment
    {
        /// <summary>HTML 片段（占位段时为空字符串）。</summary>
        public string Html { get; set; } = string.Empty;

        /// <summary>占位编号；<c>-1</c> 表示这是一段纯 HTML。</summary>
        public int OutletId { get; set; } = -1;

        /// <summary>是否为占位段。</summary>
        public bool IsOutlet => OutletId >= 0;
    }

    /// <summary>
    /// 把 Markdown AST 渲染为 HTML 字符串（自渲染，无任何 JS 依赖）。
    /// 渲染器与页面显示共用同一份输出：组件页面上的内容与 <see cref="ElMarkdownGrow.RenderHtml"/> 完全一致。
    /// </summary>
    public static class ElMarkdownGrowHtmlRenderer
    {
        /// <summary>便捷入口：直接把 Markdown 文本渲染为 HTML 片段。</summary>
        public static string RenderHtml(string markdown, ElMarkdownGrowOptions options = null)
        {
            options = options ?? new ElMarkdownGrowOptions();

            return Render(ElMarkdownGrowParser.Parse(markdown, options), options);
        }

        /// <summary>把文档渲染为 HTML 片段。</summary>
        public static string Render(MdDocument document, ElMarkdownGrowOptions options = null)
        {
            options = options ?? new ElMarkdownGrowOptions();

            if (document == null || document.Blocks.Count == 0)
            {
                var prefix = options.ClassPrefix;
                var emptyText = options.EmptyText ?? string.Empty;

                return "<div class=\"" + prefix + "__empty\">" + EscapeText(emptyText) + "</div>";
            }

            var builder = new StringBuilder(1024);
            var context = new RenderContext(options);

            RenderBlocks(builder, document.Blocks, context, false);

            return builder.ToString();
        }

        /// <summary>把文档渲染为分段集合（用于 <c>ChildContent</c> 中嵌入元素/组件的场景）。</summary>
        public static IReadOnlyList<ElMarkdownGrowSegment> RenderSegments(MdDocument document, ElMarkdownGrowOptions options = null)
        {
            options = options ?? new ElMarkdownGrowOptions();

            var html = Render(document, options);
            var segments = new List<ElMarkdownGrowSegment>();
            var index = 0;

            while (index < html.Length)
            {
                var start = html.IndexOf(ElMarkdownGrowParser.OutletStart, index);

                if (start < 0)
                {
                    segments.Add(new ElMarkdownGrowSegment { Html = html.Substring(index), OutletId = -1 });
                    break;
                }

                if (start > index)
                {
                    segments.Add(new ElMarkdownGrowSegment { Html = html.Substring(index, start - index), OutletId = -1 });
                }

                var end = html.IndexOf(ElMarkdownGrowParser.OutletEnd, start + 1);

                if (end < 0)
                {
                    segments.Add(new ElMarkdownGrowSegment { Html = html.Substring(start), OutletId = -1 });
                    break;
                }

                var text = html.Substring(start + 1, end - start - 1);

                if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var outletId))
                {
                    segments.Add(new ElMarkdownGrowSegment { Html = string.Empty, OutletId = outletId });
                }
                else
                {
                    segments.Add(new ElMarkdownGrowSegment { Html = html.Substring(start, end - start + 1), OutletId = -1 });
                }

                index = end + 1;
            }

            if (segments.Count == 0)
            {
                segments.Add(new ElMarkdownGrowSegment { Html = html, OutletId = -1 });
            }

            return segments;
        }

        private static void RenderBlocks(StringBuilder builder, List<MdBlock> blocks, RenderContext context, bool tight)
        {
            foreach (var block in blocks)
            {
                RenderBlock(builder, block, context, tight);
            }
        }

        private static void RenderBlock(StringBuilder builder, MdBlock block, RenderContext context, bool tight)
        {
            var prefix = context.Options.ClassPrefix;

            switch (block)
            {
                case MdHeading heading:
                    RenderHeading(builder, heading, context);
                    break;

                case MdParagraph paragraph:
                    if (tight)
                    {
                        // 紧凑列表项内的段落不包 <p>
                        RenderInlines(builder, paragraph.Inlines, context);
                    }
                    else
                    {
                        builder.Append("<p>");
                        RenderInlines(builder, paragraph.Inlines, context);
                        builder.Append("</p>");
                    }

                    break;

                case MdCodeBlock code:
                    builder.Append("<pre class=\"").Append(prefix).Append("__pre\">");
                    builder.Append("<code");

                    var language = GetLanguage(code.Info);

                    if (language.Length > 0)
                    {
                        builder.Append(" class=\"language-").Append(EscapeAttribute(language)).Append('"');
                    }

                    builder.Append('>').Append(EscapeText(code.Code)).Append("</code></pre>");
                    break;

                case MdBlockQuote quote:
                    builder.Append("<blockquote class=\"").Append(prefix).Append("__quote\">");
                    RenderBlocks(builder, quote.Blocks, context, false);
                    builder.Append("</blockquote>");
                    break;

                case MdList list:
                    RenderList(builder, list, context);
                    break;

                case MdTable table:
                    RenderTable(builder, table, context);
                    break;

                case MdThematicBreak _:
                    builder.Append("<hr class=\"").Append(prefix).Append("__hr\" />");
                    break;

                case MdHtmlBlock html:
                    builder.Append(ElMarkdownGrowSanitizer.Sanitize(html.Html, context.Options));
                    break;

                case MdOutletBlock outlet:
                    builder.Append(ElMarkdownGrowParser.CreateOutletMarker(outlet.Id));
                    break;
            }
        }

        private static void RenderHeading(StringBuilder builder, MdHeading heading, RenderContext context)
        {
            var prefix = context.Options.ClassPrefix;
            var level = heading.Level < 1 ? 1 : (heading.Level > 6 ? 6 : heading.Level);

            builder.Append("<h").Append(level.ToString(CultureInfo.InvariantCulture));
            builder.Append(" class=\"").Append(prefix).Append("__heading ").Append(prefix).Append("__h").Append(level.ToString(CultureInfo.InvariantCulture)).Append('"');

            if (!string.IsNullOrEmpty(heading.Anchor))
            {
                builder.Append(" id=\"").Append(EscapeAttribute(heading.Anchor)).Append('"');
            }

            builder.Append('>');
            RenderInlines(builder, heading.Inlines, context);

            if (!string.IsNullOrEmpty(heading.Anchor))
            {
                builder.Append("<a class=\"").Append(prefix).Append("__anchor\" href=\"#")
                    .Append(EscapeAttribute(heading.Anchor)).Append("\" aria-hidden=\"true\" tabindex=\"-1\">#</a>");
            }

            builder.Append("</h").Append(level.ToString(CultureInfo.InvariantCulture)).Append('>');
        }

        private static void RenderList(StringBuilder builder, MdList list, RenderContext context)
        {
            var prefix = context.Options.ClassPrefix;
            var tag = list.Ordered ? "ol" : "ul";
            var hasTask = false;

            foreach (var item in list.Blocks)
            {
                if (item is MdListItem listItem && listItem.Checked.HasValue)
                {
                    hasTask = true;
                    break;
                }
            }

            builder.Append('<').Append(tag);
            builder.Append(" class=\"").Append(prefix).Append("__list");

            if (list.Tight)
            {
                builder.Append(' ').Append(prefix).Append("__list--tight");
            }

            if (hasTask)
            {
                builder.Append(' ').Append(prefix).Append("__task-list");
            }

            builder.Append('"');

            if (list.Ordered && list.Start != 1)
            {
                builder.Append(" start=\"").Append(list.Start.ToString(CultureInfo.InvariantCulture)).Append('"');
            }

            builder.Append('>');

            foreach (var block in list.Blocks)
            {
                if (!(block is MdListItem item))
                {
                    continue;
                }

                builder.Append("<li class=\"").Append(prefix).Append("__item");

                if (item.Checked.HasValue)
                {
                    builder.Append(' ').Append(prefix).Append("__task-item");
                }

                builder.Append("\">");

                if (item.Checked.HasValue)
                {
                    builder.Append("<input class=\"").Append(prefix).Append("__task-checkbox\" type=\"checkbox\" disabled=\"disabled\"");

                    if (item.Checked.Value)
                    {
                        builder.Append(" checked=\"checked\"");
                    }

                    builder.Append(" /> ");
                }

                RenderBlocks(builder, item.Blocks, context, list.Tight);
                builder.Append("</li>");
            }

            builder.Append("</").Append(tag).Append('>');
        }

        private static void RenderTable(StringBuilder builder, MdTable table, RenderContext context)
        {
            var prefix = context.Options.ClassPrefix;

            builder.Append("<table class=\"").Append(prefix).Append("__table\">");

            var firstRow = true;

            foreach (var row in table.Rows)
            {
                if (row.IsHeader)
                {
                    builder.Append("<thead><tr>");

                    for (var i = 0; i < row.Cells.Count; i++)
                    {
                        builder.Append("<th").Append(AlignStyle(table, i)).Append('>');
                        RenderInlines(builder, row.Cells[i], context);
                        builder.Append("</th>");
                    }

                    builder.Append("</tr></thead><tbody>");
                    firstRow = false;
                    continue;
                }

                _ = firstRow;
                builder.Append("<tr>");

                for (var i = 0; i < row.Cells.Count; i++)
                {
                    builder.Append("<td").Append(AlignStyle(table, i)).Append('>');
                    RenderInlines(builder, row.Cells[i], context);
                    builder.Append("</td>");
                }

                builder.Append("</tr>");
            }

            builder.Append("</tbody></table>");
        }

        private static string AlignStyle(MdTable table, int index)
        {
            if (index >= table.Aligns.Count)
            {
                return string.Empty;
            }

            switch (table.Aligns[index])
            {
                case MdTableAlign.Left:
                    return " style=\"text-align:left\"";
                case MdTableAlign.Center:
                    return " style=\"text-align:center\"";
                case MdTableAlign.Right:
                    return " style=\"text-align:right\"";
                default:
                    return string.Empty;
            }
        }

        private static void RenderInlines(StringBuilder builder, List<MdInline> inlines, RenderContext context)
        {
            var prefix = context.Options.ClassPrefix;

            foreach (var inline in inlines)
            {
                switch (inline)
                {
                    case MdText text:
                        builder.Append(EscapeText(text.Text));
                        break;

                    case MdCodeSpan code:
                        builder.Append("<code class=\"").Append(prefix).Append("__code-inline\">")
                            .Append(EscapeText(code.Code)).Append("</code>");
                        break;

                    case MdEmphasis emphasis:
                        builder.Append("<em>");
                        RenderInlines(builder, emphasis.Inlines, context);
                        builder.Append("</em>");
                        break;

                    case MdStrong strong:
                        builder.Append("<strong>");
                        RenderInlines(builder, strong.Inlines, context);
                        builder.Append("</strong>");
                        break;

                    case MdStrike strike:
                        builder.Append("<del>");
                        RenderInlines(builder, strike.Inlines, context);
                        builder.Append("</del>");
                        break;

                    case MdLink link:
                        RenderLink(builder, link, context);
                        break;

                    case MdImage image:
                        RenderImage(builder, image, context);
                        break;

                    case MdHtmlInline html:
                        builder.Append(ElMarkdownGrowSanitizer.Sanitize(html.Html, context.Options));
                        break;

                    case MdLineBreak lineBreak:
                        builder.Append(lineBreak.Hard ? "<br />" : "\n");
                        break;

                    case MdEmoji emoji:
                        builder.Append(EscapeText(emoji.Glyph));
                        break;

                    case MdInlineOutlet outlet:
                        builder.Append(ElMarkdownGrowParser.CreateOutletMarker(outlet.Id));
                        break;
                }
            }
        }

        private static void RenderLink(StringBuilder builder, MdLink link, RenderContext context)
        {
            if (!ElMarkdownGrowSanitizer.IsSafeUrl(link.Url))
            {
                // 危险协议：只保留可见文本
                RenderInlines(builder, link.Inlines, context);

                return;
            }

            builder.Append("<a href=\"").Append(EscapeAttribute(link.Url ?? string.Empty)).Append('"');

            if (!string.IsNullOrEmpty(link.Title))
            {
                builder.Append(" title=\"").Append(EscapeAttribute(link.Title)).Append('"');
            }

            builder.Append('>');
            RenderInlines(builder, link.Inlines, context);
            builder.Append("</a>");
        }

        private static void RenderImage(StringBuilder builder, MdImage image, RenderContext context)
        {
            var prefix = context.Options.ClassPrefix;

            if (!ElMarkdownGrowSanitizer.IsSafeUrl(image.Url))
            {
                builder.Append(EscapeText(image.Alt));

                return;
            }

            builder.Append("<img class=\"").Append(prefix).Append("__img\" src=\"")
                .Append(EscapeAttribute(image.Url ?? string.Empty)).Append("\" alt=\"")
                .Append(EscapeAttribute(image.Alt ?? string.Empty)).Append('"');

            if (!string.IsNullOrEmpty(image.Title))
            {
                builder.Append(" title=\"").Append(EscapeAttribute(image.Title)).Append('"');
            }

            builder.Append(" />");
        }

        private static string GetLanguage(string info)
        {
            if (string.IsNullOrWhiteSpace(info))
            {
                return string.Empty;
            }

            var trimmed = info.Trim();
            var space = trimmed.IndexOf(' ');

            if (space > 0)
            {
                trimmed = trimmed.Substring(0, space);
            }

            // 只保留安全的字符，避免注入
            var builder = new StringBuilder(trimmed.Length);

            foreach (var c in trimmed)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '+' || c == '#')
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        /// <summary>转义文本内容：保留已有的合法实体，转义 <c>&lt; &gt;</c> 与非法 <c>&amp;</c>，并屏蔽占位标记用字符。</summary>
        public static string EscapeText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(text.Length + 16);

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                switch (c)
                {
                    case '&':
                        var end = text.IndexOf(';', i + 1);

                        if (end > i && end - i <= 12 && ElMarkdownGrowSanitizer.IsEntity(text.Substring(i + 1, end - i - 1)))
                        {
                            builder.Append(text, i, end - i + 1);
                            i = end;
                        }
                        else
                        {
                            builder.Append("&amp;");
                        }

                        break;

                    case '<':
                        builder.Append("&lt;");
                        break;

                    case '>':
                        builder.Append("&gt;");
                        break;

                    case ElMarkdownGrowParser.OutletStart:
                        builder.Append("&#xE000;");
                        break;

                    case ElMarkdownGrowParser.OutletEnd:
                        builder.Append("&#xE001;");
                        break;

                    default:
                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }

        /// <summary>转义属性值。</summary>
        public static string EscapeAttribute(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(text.Length + 8);

            foreach (var c in text)
            {
                switch (c)
                {
                    case '&':
                        builder.Append("&amp;");
                        break;
                    case '"':
                        builder.Append("&quot;");
                        break;
                    case '\'':
                        builder.Append("&#39;");
                        break;
                    case '<':
                        builder.Append("&lt;");
                        break;
                    case '>':
                        builder.Append("&gt;");
                        break;
                    case '\r':
                    case '\n':
                    case '\t':
                        builder.Append(' ');
                        break;
                    case ElMarkdownGrowParser.OutletStart:
                        builder.Append("&#xE000;");
                        break;
                    case ElMarkdownGrowParser.OutletEnd:
                        builder.Append("&#xE001;");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }

        private class RenderContext
        {
            public RenderContext(ElMarkdownGrowOptions options)
            {
                Options = options;
            }

            public ElMarkdownGrowOptions Options { get; }
        }
    }
}
