using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace ElementPlus
{
    /// <summary>
    /// xlsx（SpreadsheetML）→ HTML。
    /// <para>
    /// 覆盖：多工作表（按工作簿里的顺序，含隐藏表跳过）、共享字符串表、内联字符串、
    /// 公式（显示缓存结果）、布尔与日期（按单元格样式的数字格式判断）、合并单元格、
    /// 稀疏行与空列的对齐补齐。
    /// </para>
    /// <para>
    /// 刻意不做：公式重算（没有表达式引擎，也不该假装有）、图表与数据透视表（它们在包里是
    /// 另一套 drawing 部件，属于「另一件事」的规模），条件格式的配色还原。
    /// </para>
    /// </summary>
    internal static class ElViewerGrowOfficeExcel
    {
        private const string WorkbookPart = "xl/workbook.xml";

        private const string WorkbookRelsPart = "xl/_rels/workbook.xml.rels";

        private const string SharedStringsPart = "xl/sharedStrings.xml";

        private const string StylesPart = "xl/styles.xml";

        /// <summary>内置的数字格式里属于日期 / 时间的编号。</summary>
        private static readonly HashSet<int> BuiltInDateFormats = new HashSet<int>
        {
            14, 15, 16, 17, 18, 19, 20, 21, 22,
            27, 28, 29, 30, 31, 32, 33, 34, 35, 36,
            45, 46, 47, 50, 51, 52, 53, 54, 55, 56, 57, 58
        };

        /// <summary>一张工作表最多渲染这么多行 —— 防止一个十万行的工作簿把页面撑爆。</summary>
        private const int MaxRows = 2000;

        public static string Render(ZipArchive archive)
        {
            var workbook = ElViewerGrowOoxml.ReadXml(archive, WorkbookPart);

            if (workbook == null || workbook.Root == null)
            {
                return null;
            }

            var xl = ElViewerGrowOoxml.Xl;
            var sheetsElement = workbook.Root.Element(xl + "sheets");

            if (sheetsElement == null)
            {
                return null;
            }

            var relationships = ElViewerGrowOoxml.ReadRelationships(archive, WorkbookRelsPart, "xl/");
            var sharedStrings = ReadSharedStrings(archive);
            var dateStyles = ReadDateStyles(archive);

            var builder = new StringBuilder();
            builder.Append("<div class=\"el-viewer-grow__doc-excel\">");

            var rendered = 0;

            foreach (var sheet in sheetsElement.Elements(xl + "sheet"))
            {
                // 隐藏工作表不显示，与 Excel 打开时的观感一致。
                var state = (string)sheet.Attribute("state");

                if (!string.IsNullOrEmpty(state) && state != "visible")
                {
                    continue;
                }

                var name = (string)sheet.Attribute("name") ?? "Sheet";
                var id = (string)sheet.Attribute(ElViewerGrowOoxml.R + "id");

                if (string.IsNullOrEmpty(id) || !relationships.TryGetValue(id, out var path))
                {
                    continue;
                }

                var markup = RenderSheet(archive, name, path, sharedStrings, dateStyles);

                if (markup == null)
                {
                    continue;
                }

                builder.Append(markup);
                rendered++;
            }

            builder.Append("</div>");

            return rendered > 0 ? builder.ToString() : null;
        }

        private static string RenderSheet(
            ZipArchive archive,
            string name,
            string path,
            List<string> sharedStrings,
            HashSet<int> dateStyles)
        {
            var document = ElViewerGrowOoxml.ReadXml(archive, path);

            var data = document?.Root?.Element(ElViewerGrowOoxml.Xl + "sheetData");

            if (data == null)
            {
                return null;
            }

            var xl = ElViewerGrowOoxml.Xl;
            var merges = ReadMergeMap(document);

            var rows = new List<List<string>>();
            var rowIndex = 0;

            foreach (var row in data.Elements(xl + "row"))
            {
                if (rowIndex >= MaxRows)
                {
                    break;
                }

                var values = new List<string>();
                var column = 0;

                foreach (var cell in row.Elements(xl + "c"))
                {
                    var reference = (string)cell.Attribute("r");

                    if (!string.IsNullOrEmpty(reference))
                    {
                        var parsed = ColumnIndexOf(reference);

                        // 空列补齐：Excel 里 <c> 是稀疏的，不补会把后面的列整体左移。
                        while (column < parsed)
                        {
                            values.Add(string.Empty);
                            column++;
                        }
                    }

                    values.Add(ReadCell(cell, sharedStrings, dateStyles));
                    column++;
                }

                while (values.Count > 0 && values[values.Count - 1].Length == 0)
                {
                    values.RemoveAt(values.Count - 1);
                }

                rows.Add(values);
                rowIndex++;
            }

            while (rows.Count > 0 && rows[rows.Count - 1].Count == 0)
            {
                rows.RemoveAt(rows.Count - 1);
            }

            if (rows.Count == 0)
            {
                return null;
            }

            var width = 0;

            foreach (var row in rows)
            {
                width = Math.Max(width, row.Count);
            }

            var builder = new StringBuilder();

            builder.Append("<section class=\"el-viewer-grow__doc-sheet\">");
            builder.Append("<h4 class=\"el-viewer-grow__doc-sheet-name\">")
                .Append(ElViewerGrowRenderer.EscapeHtml(name))
                .Append("</h4>");
            builder.Append("<div class=\"el-viewer-grow__doc-table-wrap\"><table class=\"el-viewer-grow__doc-table\">");

            for (var r = 0; r < rows.Count; r++)
            {
                builder.Append("<tr class=\"el-viewer-grow__doc-tr\">");

                for (var c = 0; c < width; c++)
                {
                    var value = c < rows[r].Count ? rows[r][c] : string.Empty;
                    var text = value.Length == 0 ? "&nbsp;" : ElViewerGrowRenderer.EscapeHtml(value);
                    var tag = r == 0 ? "th" : "td";
                    var css = r == 0 ? "el-viewer-grow__doc-th" : "el-viewer-grow__doc-td";

                    builder.Append('<').Append(tag).Append(" class=\"").Append(css).Append('"');

                    var key = new CellKey { Row = r, Column = c };

                    if (merges.TryGetValue(key, out var span) && (span.RowSpan > 1 || span.ColumnSpan > 1))
                    {
                        if (span.ColumnSpan > 1)
                        {
                            builder.Append(" colspan=\"").Append(span.ColumnSpan.ToString(CultureInfo.InvariantCulture)).Append('"');
                        }

                        if (span.RowSpan > 1)
                        {
                            builder.Append(" rowspan=\"").Append(span.RowSpan.ToString(CultureInfo.InvariantCulture)).Append('"');
                        }
                    }
                    else if (IsMergedAway(merges, r, c))
                    {
                        // 被合并区域覆盖的格子直接跳过，交给 colspan/rowspan 占位。
                        continue;
                    }

                    builder.Append('>').Append(text).Append("</").Append(tag).Append('>');
                }

                builder.Append("</tr>");
            }

            builder.Append("</table></div></section>");

            return builder.ToString();
        }

        #region 单元格

        private static string ReadCell(XElement cell, List<string> sharedStrings, HashSet<int> dateStyles)
        {
            var xl = ElViewerGrowOoxml.Xl;
            var type = (string)cell.Attribute("t");

            if (type == "inlineStr")
            {
                var inline = cell.Element(xl + "is");
                return inline == null ? string.Empty : ElViewerGrowOoxml.TextOf(inline);
            }

            var value = cell.Element(xl + "v");
            var raw = value?.Value;

            if (raw == null)
            {
                // 没有缓存结果：退而显示公式本身，总比空白强（但明确包一层，避免被误读成计算值）。
                var formula = cell.Element(xl + "f");

                if (formula != null && !string.IsNullOrWhiteSpace(formula.Value))
                {
                    return "=" + formula.Value;
                }

                return string.Empty;
            }

            if (type == "s")
            {
                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
                    && index >= 0
                    && index < sharedStrings.Count)
                {
                    return sharedStrings[index];
                }

                return string.Empty;
            }

            if (type == "b")
            {
                return raw == "1" ? "TRUE" : "FALSE";
            }

            if (type == "str" || type == "e")
            {
                return raw;
            }

            // 数值：可能是日期，也可能是普通数字。
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                if (IsDateStyle(cell, dateStyles) && number >= 1 && number < 2958466)
                {
                    return FormatSerialDate(number);
                }

                return FormatNumber(number);
            }

            return raw;
        }

        /// <summary>去掉 double 打印时的浮点尾巴（0.30000000000000004 → 0.3）。</summary>
        private static string FormatNumber(double value)
        {
            if (value == Math.Floor(value) && Math.Abs(value) < 1e15)
            {
                return ((long)value).ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString("0.###############", CultureInfo.InvariantCulture);
        }

        /// <summary>OLE 自动化日期序列号 → 可读日期（保留时间部分，纯日期不显示 00:00:00）。</summary>
        private static string FormatSerialDate(double serial)
        {
            try
            {
                var date = DateTime.FromOADate(serial);

                if (date.TimeOfDay == TimeSpan.Zero)
                {
                    return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }

                return date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
            catch
            {
                return FormatNumber(serial);
            }
        }

        /// <summary>该单元格的样式（<c>s</c> 属性 → cellXfs 下标）是否指向日期格式。</summary>
        private static bool IsDateStyle(XElement cell, HashSet<int> dateStyles)
        {
            var style = (string)cell.Attribute("s");

            if (string.IsNullOrEmpty(style)
                || !int.TryParse(style, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
            {
                return false;
            }

            return dateStyles.Contains(index);
        }

        #endregion

        #region 共享字符串与样式

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            var list = new List<string>();
            var document = ElViewerGrowOoxml.ReadXml(archive, SharedStringsPart);

            if (document?.Root == null)
            {
                return list;
            }

            var xl = ElViewerGrowOoxml.Xl;

            foreach (var item in document.Root.Elements(xl + "si"))
            {
                list.Add(ElViewerGrowOoxml.TextOf(item));
            }

            return list;
        }

        /// <summary>
        /// 收集「cellXfs 下标」里属于日期 / 时间的那些。
        /// 判断依据是两跳：cellXfs 的 xf → numFmtId；numFmtId 要么命中内置日期表，
        /// 要么在 numFmts 里查到自定义格式串、且格式串里的日期占位符多于数字占位符。
        /// </summary>
        private static HashSet<int> ReadDateStyles(ZipArchive archive)
        {
            var result = new HashSet<int>();
            var document = ElViewerGrowOoxml.ReadXml(archive, StylesPart);

            if (document?.Root == null)
            {
                return result;
            }

            var xl = ElViewerGrowOoxml.Xl;

            var customFormats = new Dictionary<int, string>();

            var numFmts = document.Root.Element(xl + "numFmts");

            if (numFmts != null)
            {
                foreach (var format in numFmts.Elements(xl + "numFmt"))
                {
                    var id = (string)format.Attribute("numFmtId");
                    var code = (string)format.Attribute("formatCode");

                    if (int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                        && code != null)
                    {
                        customFormats[parsed] = code;
                    }
                }
            }

            var cellXfs = document.Root.Element(xl + "cellXfs");

            if (cellXfs == null)
            {
                return result;
            }

            var index = 0;

            foreach (var xf in cellXfs.Elements(xl + "xf"))
            {
                var id = (string)xf.Attribute("numFmtId");

                if (int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numFmtId)
                    && IsDateFormat(numFmtId, customFormats))
                {
                    result.Add(index);
                }

                index++;
            }

            return result;
        }

        private static bool IsDateFormat(int numFmtId, Dictionary<int, string> customFormats)
        {
            if (BuiltInDateFormats.Contains(numFmtId))
            {
                return true;
            }

            if (!customFormats.TryGetValue(numFmtId, out var code) || string.IsNullOrEmpty(code))
            {
                return false;
            }

            // 格式串里去掉引号包裹的字面量与方括号里的条件段，再看日期占位符是否占多数。
            var stripped = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < code.Length; i++)
            {
                var ch = code[i];

                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (inQuotes)
                {
                    continue;
                }

                if (ch == '[')
                {
                    while (i < code.Length && code[i] != ']')
                    {
                        i++;
                    }

                    continue;
                }

                if (ch == '\\')
                {
                    i++;
                    continue;
                }

                stripped.Append(ch);
            }

            var probe = stripped.ToString().ToLowerInvariant();
            var dateTokens = 0;
            var numericTokens = 0;

            foreach (var ch in probe)
            {
                if ("ymdhs".IndexOf(ch) >= 0)
                {
                    dateTokens++;
                }
                else if ("0#?".IndexOf(ch) >= 0)
                {
                    numericTokens++;
                }
            }

            return dateTokens >= 2 && dateTokens > numericTokens;
        }

        #endregion

        #region 合并单元格

        private sealed class Span
        {
            public int ColumnSpan = 1;

            public int RowSpan = 1;
        }

        private struct CellKey : IEquatable<CellKey>
        {
            public int Row;

            public int Column;

            public bool Equals(CellKey other)
            {
                return Row == other.Row && Column == other.Column;
            }

            public override bool Equals(object obj)
            {
                return obj is CellKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (Row * 397) ^ Column;
            }
        }

        /// <summary>把 <c>A1:B3</c> 这样的合并区域展开成「锚点 → 跨行跨列」表。</summary>
        private static Dictionary<CellKey, Span> ReadMergeMap(XDocument document)
        {
            var map = new Dictionary<CellKey, Span>();
            var xl = ElViewerGrowOoxml.Xl;

            var merges = document?.Root?.Element(xl + "mergeCells");

            if (merges == null)
            {
                return map;
            }

            foreach (var merge in merges.Elements(xl + "mergeCell"))
            {
                var reference = (string)merge.Attribute("ref");

                if (string.IsNullOrEmpty(reference))
                {
                    continue;
                }

                var parts = reference.Split(':');

                if (parts.Length != 2)
                {
                    continue;
                }

                var startRow = 0;
                var startColumn = 0;
                var endRow = 0;
                var endColumn = 0;

                if (!TryParseReference(parts[0], out startRow, out startColumn)
                    || !TryParseReference(parts[1], out endRow, out endColumn))
                {
                    continue;
                }

                map[new CellKey { Row = startRow, Column = startColumn }] = new Span
                {
                    RowSpan = Math.Max(1, endRow - startRow + 1),
                    ColumnSpan = Math.Max(1, endColumn - startColumn + 1)
                };
            }

            return map;
        }

        /// <summary>该格是否落在某个合并区域内部（不是锚点），需要跳过不输出。</summary>
        private static bool IsMergedAway(Dictionary<CellKey, Span> merges, int row, int column)
        {
            foreach (var pair in merges)
            {
                var key = pair.Key;

                if (row >= key.Row && row < key.Row + pair.Value.RowSpan
                    && column >= key.Column && column < key.Column + pair.Value.ColumnSpan
                    && !(row == key.Row && column == key.Column))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region 坐标

        /// <summary>把 <c>A1</c> / <c>AA10</c> 解析成 0 基的行列号。</summary>
        private static bool TryParseReference(string reference, out int row, out int column)
        {
            row = 0;
            column = 0;

            if (string.IsNullOrEmpty(reference))
            {
                return false;
            }

            var index = 0;

            while (index < reference.Length && char.IsLetter(reference[index]))
            {
                column = column * 26 + (char.ToUpperInvariant(reference[index]) - 'A' + 1);
                index++;
            }

            if (index == 0 || index >= reference.Length)
            {
                return false;
            }

            if (!int.TryParse(reference.Substring(index), NumberStyles.Integer, CultureInfo.InvariantCulture, out row))
            {
                return false;
            }

            column -= 1;
            row -= 1;

            return column >= 0 && row >= 0;
        }

        /// <summary><c>A1</c> → 0，<c>C5</c> → 2。</summary>
        private static int ColumnIndexOf(string reference)
        {
            TryParseReference(reference, out _, out var column);
            return column;
        }

        #endregion
    }
}
