using System;
using System.Globalization;
using System.Text;

namespace ElementPlus
{
    /// <summary>
    /// ElHtmlEditorGrow 的 HTML 片段构建器（**纯 C# 单一真源**）。
    /// <para>
    /// 表格、图片、链接等需要被插入编辑区的 HTML 全部在这里生成，屏幕渲染与测试断言使用同一份逻辑，
    /// 浏览器端只负责把生成好的字符串插入光标位置，不参与任何结构拼装。
    /// </para>
    /// </summary>
    public static class ElHtmlEditorGrowHtmlBuilder
    {
        /// <summary>默认边框颜色（与 Element Plus 边框色一致）。</summary>
        public const string DefaultBorderColor = "#dcdfe6";

        /// <summary>默认表头背景色（与 Element Plus 表头一致）。</summary>
        public const string DefaultHeaderBackground = "#f5f7fa";

        /// <summary>默认表头文本格式。</summary>
        public const string DefaultColumnHeaderFormat = "列 {0}";

        /// <summary>把裸数字补成像素值；已是带单位长度或百分比时原样返回。</summary>
        public static string NormalizeLength(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();

            if (trimmed.EndsWith("%", StringComparison.Ordinal))
            {
                return trimmed;
            }

            if (trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase)
                || trimmed.EndsWith("em", StringComparison.OrdinalIgnoreCase)
                || trimmed.EndsWith("rem", StringComparison.OrdinalIgnoreCase)
                || trimmed.EndsWith("vw", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            {
                return number.ToString(CultureInfo.InvariantCulture) + "px";
            }

            return trimmed;
        }

        /// <summary>按参数解析表格边框 CSS 值。</summary>
        public static string ResolveBorder(ElHtmlEditorGrowTableArgs args)
        {
            var style = string.IsNullOrWhiteSpace(args?.BorderStyle) ? "solid" : args.BorderStyle.Trim();
            var color = string.IsNullOrWhiteSpace(args?.BorderColor) ? DefaultBorderColor : args.BorderColor.Trim();
            var width = args?.BorderWidthPx ?? Math.Max(0, args?.Border ?? 1);

            return width.ToString(CultureInfo.InvariantCulture) + "px " + style + " " + color;
        }

        /// <summary>按参数解析单元格内边距 CSS 值。</summary>
        public static string ResolveCellPadding(ElHtmlEditorGrowTableArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args?.CellPaddingPx?.ToString()))
            {
                return args.CellPaddingPx.Value.ToString(CultureInfo.InvariantCulture) + "px";
            }

            return string.IsNullOrWhiteSpace(args?.CellPadding) ? "4px 8px" : args.CellPadding.Trim();
        }

        /// <summary>构造单元格的行内样式。</summary>
        public static string BuildCellStyle(ElHtmlEditorGrowTableArgs args, bool isHeader)
        {
            var builder = new StringBuilder();

            builder.Append("border:").Append(ResolveBorder(args)).Append(';');
            builder.Append("padding:").Append(ResolveCellPadding(args)).Append(';');

            if (isHeader)
            {
                builder.Append("background:").Append(
                    string.IsNullOrWhiteSpace(args?.CellBackground) ? DefaultHeaderBackground : args.CellBackground.Trim()).Append(';');
                builder.Append("text-align:").Append(
                    string.IsNullOrWhiteSpace(args?.CellTextAlign) ? "left" : args.CellTextAlign.Trim()).Append(';');
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(args?.CellBackground))
                {
                    builder.Append("background:").Append(args.CellBackground.Trim()).Append(';');
                }

                if (!string.IsNullOrWhiteSpace(args?.CellTextAlign))
                {
                    builder.Append("text-align:").Append(args.CellTextAlign.Trim()).Append(';');
                }
            }

            if (!string.IsNullOrWhiteSpace(args?.CellVerticalAlign))
            {
                builder.Append("vertical-align:").Append(args.CellVerticalAlign.Trim()).Append(';');
            }

            return builder.ToString();
        }

        /// <summary>
        /// 生成插入用的表格 HTML。<paramref name="args"/> 中的 <see cref="ElHtmlEditorGrowTableArgs.Html"/> 会被本方法的返回值覆盖。
        /// </summary>
        public static string BuildTable(ElHtmlEditorGrowTableArgs args, ElHtmlEditorGrowTableStrings strings = null)
        {
            args ??= new ElHtmlEditorGrowTableArgs();

            var rows = args.Rows;
            var columns = args.Columns;

            if (rows <= 0)
            {
                rows = 2;
            }

            if (columns <= 0)
            {
                columns = 2;
            }

            // 行数上限防止误操作生成超大表格（与参考实现一致的防御性约束）。
            rows = Math.Min(rows, 100);
            columns = Math.Min(columns, 50);

            var width = NormalizeLength(args.Width) ?? "100%";
            var border = ResolveBorder(args);
            var cellStyle = BuildCellStyle(args, false);
            var headerStyle = BuildCellStyle(args, true);
            var format = args.DefaultColumnHeader;
            if (string.IsNullOrWhiteSpace(format))
            {
                format = strings?.DefaultColumnHeader;
            }

            if (string.IsNullOrWhiteSpace(format))
            {
                format = DefaultColumnHeaderFormat;
            }

            var html = new StringBuilder();
            html.Append("<table style=\"width:").Append(EscapeAttribute(width))
                .Append(";border-collapse:collapse;border:").Append(EscapeAttribute(border)).Append(";\">");

            if (args.HeaderRow)
            {
                html.Append("<thead><tr>");

                for (var column = 0; column < columns; column++)
                {
                    var text = string.Format(CultureInfo.InvariantCulture, format, column + 1);

                    html.Append("<th style=\"").Append(EscapeAttribute(headerStyle)).Append("\">")
                        .Append(EscapeText(text))
                        .Append("</th>");
                }

                html.Append("</tr></thead>");
            }

            html.Append("<tbody>");

            for (var row = 0; row < rows; row++)
            {
                html.Append("<tr>");

                for (var column = 0; column < columns; column++)
                {
                    // 用 <br> 而非 &nbsp; 占位：既保证空单元格可点击，也避免产生不可见的不换行空格污染内容。
                    html.Append("<td style=\"").Append(EscapeAttribute(cellStyle)).Append("\"><br></td>");
                }

                html.Append("</tr>");
            }

            html.Append("</tbody></table><p><br></p>");

            return html.ToString();
        }

        /// <summary>生成图片标签；<paramref name="src"/> 为空或协议不安全时返回 <c>null</c>。</summary>
        public static string BuildImage(string src, string alt = null, string width = null, string height = null)
        {
            if (string.IsNullOrWhiteSpace(src) || !ElHtmlEditorGrowSanitizer.IsSafeUrl(src))
            {
                return null;
            }

            var html = new StringBuilder();
            html.Append("<img src=\"").Append(EscapeAttribute(src.Trim())).Append('"');

            if (!string.IsNullOrWhiteSpace(alt))
            {
                html.Append(" alt=\"").Append(EscapeAttribute(alt)).Append('"');
            }

            var normalizedWidth = NormalizeLength(width);
            if (!string.IsNullOrWhiteSpace(normalizedWidth))
            {
                html.Append(" width=\"").Append(EscapeAttribute(normalizedWidth)).Append('"');
            }

            var normalizedHeight = NormalizeLength(height);
            if (!string.IsNullOrWhiteSpace(normalizedHeight))
            {
                html.Append(" height=\"").Append(EscapeAttribute(normalizedHeight)).Append('"');
            }

            html.Append('>');

            return html.ToString();
        }

        /// <summary>生成链接标签；<paramref name="href"/> 为空或协议不安全时返回 <c>null</c>。</summary>
        public static string BuildLink(string href, string text, bool openInNewWindow = false)
        {
            if (string.IsNullOrWhiteSpace(href) || !ElHtmlEditorGrowSanitizer.IsSafeUrl(href))
            {
                return null;
            }

            var inner = string.IsNullOrEmpty(text) ? EscapeText(href.Trim()) : EscapeText(text);

            var html = new StringBuilder();
            html.Append("<a href=\"").Append(EscapeAttribute(href.Trim())).Append('"');

            if (openInNewWindow)
            {
                html.Append(" target=\"_blank\" rel=\"noopener noreferrer\"");
            }

            html.Append('>').Append(inner).Append("</a>");

            return html.ToString();
        }

        /// <summary>生成水平分割线。</summary>
        public static string BuildHorizontalRule()
        {
            return "<hr>";
        }

        /// <summary>转义 HTML 属性值（含单引号）。</summary>
        public static string EscapeAttribute(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("&", "&amp;")
                        .Replace("<", "&lt;")
                        .Replace(">", "&gt;")
                        .Replace("\"", "&quot;")
                        .Replace("'", "&#39;");
        }

        /// <summary>转义 HTML 文本内容。</summary>
        public static string EscapeText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("&", "&amp;")
                        .Replace("<", "&lt;")
                        .Replace(">", "&gt;");
        }
    }
}
