using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace ElementPlus
{
    /// <summary>
    /// docx（WordprocessingML）→ HTML。
    /// <para>
    /// 只覆盖「读得出来、看得懂」的部分：段落、标题、粗体 / 斜体 / 下划线 / 删除线、上标下标、
    /// 换行与分页、超链接、表格（含横向合并与纵向合并）、内嵌图片。
    /// 刻意不做分页还原与页码（Word 的分页取决于渲染引擎的字体度量，任何非 Word 实现都只是近似，
    /// 假装还原反而会误导），因此文档以「流式长文」呈现。
    /// </para>
    /// </summary>
    internal static class ElViewerGrowOfficeWord
    {
        private const string DocumentPart = "word/document.xml";

        private const string RelsPart = "word/_rels/document.xml.rels";

        /// <summary>解析一份 Word 文档时要用到的东西：归档、部件关系、外链关系。</summary>
        private sealed class Context
        {
            public ZipArchive Archive;

            /// <summary>rId → 包内部件路径（图片等）。</summary>
            public Dictionary<string, string> Parts;

            /// <summary>rId → 外部 URL（超链接）。</summary>
            public Dictionary<string, string> Links;
        }

        public static string Render(ZipArchive archive)
        {
            var document = ElViewerGrowOoxml.ReadXml(archive, DocumentPart);

            if (document == null || document.Root == null)
            {
                return null;
            }

            var w = ElViewerGrowOoxml.W;
            var body = document.Root.Element(w + "body");

            if (body == null)
            {
                return null;
            }

            var context = new Context
            {
                Archive = archive,
                Parts = ElViewerGrowOoxml.ReadRelationships(archive, RelsPart, "word/"),
                Links = ElViewerGrowOoxml.ReadExternalRelationships(archive, RelsPart)
            };

            var builder = new StringBuilder();

            builder.Append("<div class=\"el-viewer-grow__doc-word\">");

            AppendBlocks(builder, body, context);

            builder.Append("</div>");

            return builder.ToString();
        }

        /// <summary>铺开一层块级内容：段落、表格、内容控件。</summary>
        private static void AppendBlocks(StringBuilder builder, XElement container, Context context)
        {
            var w = ElViewerGrowOoxml.W;

            foreach (var node in container.Elements())
            {
                if (node.Name == w + "p")
                {
                    AppendParagraph(builder, node, context);
                }
                else if (node.Name == w + "tbl")
                {
                    AppendTable(builder, node, context);
                }
                else if (node.Name == w + "sdt")
                {
                    // 内容控件（目录、文本框的容器）：把里面的块级内容原样铺开。
                    var content = node.Element(w + "sdtContent");

                    if (content != null)
                    {
                        AppendBlocks(builder, content, context);
                    }
                }
            }
        }

        #region 段落

        private static void AppendParagraph(StringBuilder builder, XElement paragraph, Context context)
        {
            var w = ElViewerGrowOoxml.W;
            var properties = paragraph.Element(w + "pPr");

            var inner = RenderInline(paragraph, context, out var hasContent);

            if (!hasContent && inner.Length == 0)
            {
                // 完全空的段落 = 一个空行，保留它，否则文档的段落间距会全部塌掉。
                builder.Append("<p class=\"el-viewer-grow__doc-p\"></p>");
                return;
            }

            var heading = ResolveHeadingLevel(properties);
            var tag = heading > 0 ? "h" + heading.ToString(CultureInfo.InvariantCulture) : "p";
            var css = heading > 0 ? "el-viewer-grow__doc-heading" : "el-viewer-grow__doc-p";

            builder.Append('<').Append(tag).Append(" class=\"").Append(css).Append('"');

            var style = ResolveParagraphStyle(properties);

            if (style.Length > 0)
            {
                builder.Append(" style=\"").Append(style).Append('"');
            }

            builder.Append('>');
            builder.Append(inner);
            builder.Append("</").Append(tag).Append('>');
        }

        /// <summary>段落对齐与缩进。</summary>
        private static string ResolveParagraphStyle(XElement properties)
        {
            if (properties == null)
            {
                return string.Empty;
            }

            var w = ElViewerGrowOoxml.W;
            var builder = new StringBuilder();

            var alignment = (string)properties.Element(w + "jc")?.Attribute(w + "val");

            switch (alignment)
            {
                case "center":
                    builder.Append("text-align:center;");
                    break;
                case "right":
                case "end":
                    builder.Append("text-align:right;");
                    break;
                case "both":
                case "distribute":
                    builder.Append("text-align:justify;");
                    break;
            }

            var indentation = properties.Element(w + "ind");

            if (indentation != null)
            {
                var left = ResolveTwips((string)indentation.Attribute(w + "left")
                    ?? (string)indentation.Attribute(w + "start"));

                if (left > 0)
                {
                    builder.Append("margin-left:").Append(left.ToString(CultureInfo.InvariantCulture)).Append("px;");
                }

                var firstLine = ResolveTwips((string)indentation.Attribute(w + "firstLine"));

                if (firstLine > 0)
                {
                    builder.Append("text-indent:").Append(firstLine.ToString(CultureInfo.InvariantCulture)).Append("px;");
                }
            }

            return builder.ToString();
        }

        /// <summary>把 <c>w:pStyle/@w:val</c> 映射成标题级别；不是标题返回 0。</summary>
        private static int ResolveHeadingLevel(XElement properties)
        {
            if (properties == null)
            {
                return 0;
            }

            var w = ElViewerGrowOoxml.W;
            var style = (string)properties.Element(w + "pStyle")?.Attribute(w + "val");

            if (string.IsNullOrEmpty(style))
            {
                return 0;
            }

            // "Heading2" / "heading 2" / "标题 2" / "标题2" / "2级标题"
            var compact = style.Replace(" ", string.Empty).ToLowerInvariant();

            for (var level = 1; level <= 6; level++)
            {
                var digit = level.ToString(CultureInfo.InvariantCulture);

                if (compact == "heading" + digit
                    || compact == "标题" + digit
                    || compact == digit + "级标题")
                {
                    return level;
                }
            }

            return 0;
        }

        #endregion

        #region 行内内容

        /// <summary>
        /// 渲染一段容器（<c>w:p</c> / <c>w:hyperlink</c> / <c>w:ins</c>）里的所有行内元素。
        /// <paramref name="hasContent"/> 表示是否产出了可见内容（区分「空段落」与「纯图片段落」）。
        /// </summary>
        private static string RenderInline(XElement container, Context context, out bool hasContent)
        {
            var w = ElViewerGrowOoxml.W;
            var builder = new StringBuilder();

            hasContent = false;

            foreach (var child in container.Elements())
            {
                if (child.Name == w + "r")
                {
                    if (AppendRun(builder, child, context))
                    {
                        hasContent = true;
                    }
                }
                else if (child.Name == w + "hyperlink")
                {
                    var inner = RenderInline(child, context, out var linkHasContent);
                    var target = ResolveHyperlink(child, context);

                    if (linkHasContent)
                    {
                        hasContent = true;
                    }

                    if (string.IsNullOrEmpty(target) || inner.Length == 0)
                    {
                        builder.Append(inner);
                    }
                    else
                    {
                        builder.Append("<a class=\"el-viewer-grow__doc-link\" href=\"")
                            .Append(ElViewerGrowRenderer.EscapeHtml(target))
                            .Append("\" target=\"_blank\" rel=\"noopener noreferrer\">")
                            .Append(inner)
                            .Append("</a>");
                    }
                }
                else if (child.Name == w + "ins")
                {
                    // 修订插入：按正文渲染。
                    var inner = RenderInline(child, context, out var insHasContent);

                    if (insHasContent)
                    {
                        hasContent = true;
                    }

                    builder.Append(inner);
                }
                else if (child.Name == w + "sdt")
                {
                    var content = child.Element(w + "sdtContent");

                    if (content == null)
                    {
                        continue;
                    }

                    var inner = RenderInline(content, context, out var sdtHasContent);

                    if (sdtHasContent)
                    {
                        hasContent = true;
                    }

                    builder.Append(inner);
                }
            }

            return builder.ToString();
        }

        /// <summary>渲染单个 run（<c>w:r</c>），返回它是否产出了可见内容。</summary>
        private static bool AppendRun(StringBuilder builder, XElement run, Context context)
        {
            var w = ElViewerGrowOoxml.W;
            var properties = run.Element(w + "rPr");

            var text = new StringBuilder();
            var images = new StringBuilder();

            // 先把 run 的内容拆成「文本」与「图片」，样式只作用于文本。
            foreach (var child in run.Elements())
            {
                if (child.Name == w + "t")
                {
                    text.Append(ElViewerGrowRenderer.EscapeHtml(child.Value));
                }
                else if (child.Name == w + "delText")
                {
                    // 修订删除：不显示。
                    continue;
                }
                else if (child.Name == w + "br")
                {
                    if ((string)child.Attribute(w + "type") == "page")
                    {
                        text.Append("<span class=\"el-viewer-grow__doc-page-break\"></span>");
                    }
                    else
                    {
                        text.Append("<br/>");
                    }
                }
                else if (child.Name == w + "tab")
                {
                    text.Append("<span class=\"el-viewer-grow__doc-tab\"></span>");
                }
                else if (child.Name == w + "drawing" || child.Name == w + "pict" || child.Name == w + "object")
                {
                    AppendImages(images, child, context);
                }
            }

            var produced = false;

            if (text.Length > 0)
            {
                var markup = text.ToString();
                var style = ResolveRunStyle(properties);

                if (style.Length > 0)
                {
                    markup = "<span style=\"" + style + "\">" + markup + "</span>";
                }

                if (IsInlineCode(properties))
                {
                    markup = "<code class=\"el-viewer-grow__doc-inline-code\">" + markup + "</code>";
                }

                builder.Append(markup);
                produced = true;
            }

            if (images.Length > 0)
            {
                builder.Append(images);
                produced = true;
            }

            return produced;
        }

        private static string ResolveRunStyle(XElement properties)
        {
            if (properties == null)
            {
                return string.Empty;
            }

            var w = ElViewerGrowOoxml.W;
            var builder = new StringBuilder();

            if (ElViewerGrowOoxml.Toggle(properties, w, "b"))
            {
                builder.Append("font-weight:700;");
            }

            if (ElViewerGrowOoxml.Toggle(properties, w, "i"))
            {
                builder.Append("font-style:italic;");
            }

            var underline = properties.Element(w + "u");

            if (underline != null && (string)underline.Attribute(w + "val") != "none")
            {
                builder.Append("text-decoration:underline;");
            }

            if (ElViewerGrowOoxml.Toggle(properties, w, "strike") || ElViewerGrowOoxml.Toggle(properties, w, "dstrike"))
            {
                builder.Append("text-decoration:line-through;");
            }

            var color = (string)properties.Element(w + "color")?.Attribute(w + "val");

            if (!string.IsNullOrEmpty(color) && color != "auto" && IsHexColor(color))
            {
                builder.Append("color:#").Append(color).Append(';');
            }

            var highlight = (string)properties.Element(w + "highlight")?.Attribute(w + "val");

            if (!string.IsNullOrEmpty(highlight) && highlight != "none")
            {
                var background = ResolveHighlight(highlight);

                if (background != null)
                {
                    builder.Append("background-color:").Append(background).Append(';');
                }
            }

            var size = (string)properties.Element(w + "sz")?.Attribute(w + "val");

            if (double.TryParse(size, NumberStyles.Float, CultureInfo.InvariantCulture, out var halfPoints)
                && halfPoints > 0)
            {
                // w:sz 的单位是半磅。
                builder.Append("font-size:")
                    .Append((halfPoints / 2.0).ToString("0.##", CultureInfo.InvariantCulture))
                    .Append("pt;");
            }

            var vertical = (string)properties.Element(w + "vertAlign")?.Attribute(w + "val");

            if (vertical == "superscript")
            {
                builder.Append("vertical-align:super;font-size:.75em;");
            }
            else if (vertical == "subscript")
            {
                builder.Append("vertical-align:sub;font-size:.75em;");
            }

            return builder.ToString();
        }

        /// <summary>等宽字体（Consolas / Courier New 等）视为行内代码。</summary>
        private static bool IsInlineCode(XElement properties)
        {
            if (properties == null)
            {
                return false;
            }

            var w = ElViewerGrowOoxml.W;
            var fonts = properties.Element(w + "rFonts");

            if (fonts == null)
            {
                return false;
            }

            foreach (var name in new[] { "ascii", "hAnsi", "cs" })
            {
                var value = (string)fonts.Attribute(w + name);

                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                foreach (var candidate in new[] { "Consolas", "Courier New", "Cascadia", "Menlo", "Monaco", "Source Code" })
                {
                    if (value.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string ResolveHighlight(string value)
        {
            switch (value)
            {
                case "yellow":
                    return "#ffff00";
                case "green":
                    return "#00ff00";
                case "cyan":
                    return "#00ffff";
                case "magenta":
                    return "#ff00ff";
                case "blue":
                    return "#0000ff";
                case "red":
                    return "#ff0000";
                case "darkBlue":
                    return "#000080";
                case "darkCyan":
                    return "#008080";
                case "darkGreen":
                    return "#008000";
                case "darkMagenta":
                    return "#800080";
                case "darkRed":
                    return "#800000";
                case "darkYellow":
                    return "#808000";
                case "darkGray":
                    return "#808080";
                case "lightGray":
                    return "#c0c0c0";
                case "black":
                    return "#000000";
                default:
                    return null;
            }
        }

        /// <summary>超链接的 rId → 外部 URL（只放行 http/https/mailto）。</summary>
        private static string ResolveHyperlink(XElement hyperlink, Context context)
        {
            var id = (string)hyperlink.Attribute(ElViewerGrowOoxml.R + "id");

            if (string.IsNullOrEmpty(id) || context.Links == null || !context.Links.TryGetValue(id, out var target))
            {
                return null;
            }

            if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            {
                return target;
            }

            return null;
        }

        #endregion

        #region 图片

        private static void AppendImages(StringBuilder builder, XElement node, Context context)
        {
            var a = ElViewerGrowOoxml.A;
            var r = ElViewerGrowOoxml.R;

            foreach (var blip in node.Descendants(a + "blip"))
            {
                AppendImage(builder, (string)blip.Attribute(r + "embed") ?? (string)blip.Attribute(r + "link"), context);
            }

            // VML 旧写法：<v:imagedata r:id="..."/>
            foreach (var imageData in node.Descendants(ElViewerGrowOoxml.V + "imagedata"))
            {
                AppendImage(builder, (string)imageData.Attribute(r + "id"), context);
            }
        }

        private static void AppendImage(StringBuilder builder, string id, Context context)
        {
            if (string.IsNullOrEmpty(id) || !context.Parts.TryGetValue(id, out var part))
            {
                return;
            }

            // EMF/WMF 等浏览器无法显示的格式，BuildDataUri 返回 null，这里自然跳过。
            var uri = ElViewerGrowOoxml.BuildDataUri(context.Archive, part);

            if (uri == null)
            {
                return;
            }

            builder.Append("<img class=\"el-viewer-grow__doc-image\" src=\"").Append(uri).Append("\" alt=\"\" />");
        }

        #endregion

        #region 表格

        private sealed class Cell
        {
            public XElement Element;

            public int ColumnSpan = 1;

            public bool MergeContinue;

            public bool MergeRestart;

            public int RowSpan = 1;
        }

        private static void AppendTable(StringBuilder builder, XElement table, Context context)
        {
            var w = ElViewerGrowOoxml.W;
            var rows = new List<List<Cell>>();

            foreach (var row in table.Elements(w + "tr"))
            {
                var cells = new List<Cell>();

                foreach (var cell in row.Elements(w + "tc"))
                {
                    var properties = cell.Element(w + "tcPr");
                    var info = new Cell { Element = cell };

                    var span = (string)properties?.Element(w + "gridSpan")?.Attribute(w + "val");

                    if (int.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out var columnSpan)
                        && columnSpan > 1)
                    {
                        info.ColumnSpan = columnSpan;
                    }

                    var merge = properties?.Element(w + "vMerge");

                    if (merge != null)
                    {
                        info.MergeRestart = (string)merge.Attribute(w + "val") == "restart";
                        info.MergeContinue = !info.MergeRestart;
                    }

                    cells.Add(info);
                }

                if (cells.Count > 0)
                {
                    rows.Add(cells);
                }
            }

            if (rows.Count == 0)
            {
                return;
            }

            ResolveRowSpans(rows);

            builder.Append("<div class=\"el-viewer-grow__doc-table-wrap\"><table class=\"el-viewer-grow__doc-table\">");

            for (var r = 0; r < rows.Count; r++)
            {
                builder.Append("<tr class=\"el-viewer-grow__doc-tr\">");

                foreach (var cell in rows[r])
                {
                    if (cell.MergeContinue)
                    {
                        // 已被上面的 rowspan 覆盖，不再输出占位单元格。
                        continue;
                    }

                    var tag = r == 0 ? "th" : "td";
                    var css = r == 0 ? "el-viewer-grow__doc-th" : "el-viewer-grow__doc-td";

                    builder.Append('<').Append(tag).Append(" class=\"").Append(css).Append('"');

                    if (cell.ColumnSpan > 1)
                    {
                        builder.Append(" colspan=\"").Append(cell.ColumnSpan.ToString(CultureInfo.InvariantCulture)).Append('"');
                    }

                    if (cell.RowSpan > 1)
                    {
                        builder.Append(" rowspan=\"").Append(cell.RowSpan.ToString(CultureInfo.InvariantCulture)).Append('"');
                    }

                    builder.Append('>');

                    AppendCellContent(builder, cell.Element, context);

                    builder.Append("</").Append(tag).Append('>');
                }

                builder.Append("</tr>");
            }

            builder.Append("</table></div>");
        }

        /// <summary>
        /// 纵向合并（<c>w:vMerge</c>）在 OOXML 里是「起始格 val="restart" + 后续格 val 缺省」，
        /// 后续格本身不带跨行数，必须按列向下数出来，否则表头合并会全丢。
        /// </summary>
        private static void ResolveRowSpans(List<List<Cell>> rows)
        {
            for (var r = 0; r < rows.Count; r++)
            {
                var column = 0;

                foreach (var cell in rows[r])
                {
                    if (cell.MergeRestart)
                    {
                        var span = 1;
                        var next = r + 1;

                        while (next < rows.Count)
                        {
                            var below = CellAtColumn(rows[next], column);

                            if (below == null || !below.MergeContinue)
                            {
                                break;
                            }

                            span++;
                            next++;
                        }

                        cell.RowSpan = span;
                    }

                    column += cell.ColumnSpan;
                }
            }
        }

        /// <summary>取某一行里起始列等于 <paramref name="column"/> 的单元格。</summary>
        private static Cell CellAtColumn(List<Cell> row, int column)
        {
            var current = 0;

            foreach (var cell in row)
            {
                if (current == column)
                {
                    return cell;
                }

                current += cell.ColumnSpan;
            }

            return null;
        }

        private static void AppendCellContent(StringBuilder builder, XElement cell, Context context)
        {
            var w = ElViewerGrowOoxml.W;
            var paragraphs = new List<XElement>(cell.Elements(w + "p"));

            if (paragraphs.Count == 0)
            {
                return;
            }

            if (paragraphs.Count == 1)
            {
                builder.Append(RenderInline(paragraphs[0], context, out _));
                return;
            }

            // 单元格里的多段：段间用 <br/> 分隔，避免在 td 里塞块级元素。
            for (var i = 0; i < paragraphs.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("<br/>");
                }

                builder.Append(RenderInline(paragraphs[i], context, out _));
            }
        }

        #endregion

        /// <summary>缇（twip，1/20 磅）→ 像素（按 96 DPI）。</summary>
        private static int ResolveTwips(string value)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var twips) || twips <= 0)
            {
                return 0;
            }

            return (int)Math.Round(twips / 20.0 * 96.0 / 72.0);
        }

        private static bool IsHexColor(string value)
        {
            if (value.Length != 6)
            {
                return false;
            }

            foreach (var ch in value)
            {
                if (!Uri.IsHexDigit(ch))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
