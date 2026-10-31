using System.Text.Json;

namespace ElementPlus
{
    /// <summary>
    /// 表格命令参数（插入/更新表格、设置列宽与单元格样式）。
    /// <para>对应参考实现 Radzen 的 <c>HtmlEditorTableCommandArgs</c>，为纯 C# 模型。</para>
    /// </summary>
    public class ElHtmlEditorGrowTableArgs
    {
        /// <summary>表格体行数。</summary>
        public int Rows { get; set; }

        /// <summary>列数。</summary>
        public int Columns { get; set; }

        /// <summary>表格宽度（如 <c>100%</c> 或 <c>600px</c>）。</summary>
        public string Width { get; set; }

        /// <summary>表格边框宽度，默认 <c>1</c>。</summary>
        public int Border { get; set; } = 1;

        /// <summary>是否包含表头行。</summary>
        public bool HeaderRow { get; set; }

        /// <summary>列宽（CSS 长度）。</summary>
        public string ColumnWidth { get; set; }

        /// <summary>列宽（像素）。</summary>
        public int? ColumnWidthPx { get; set; }

        /// <summary>单元格背景色。</summary>
        public string CellBackground { get; set; }

        /// <summary>单元格内边距（CSS 长度）。</summary>
        public string CellPadding { get; set; }

        /// <summary>单元格内边距（像素）。</summary>
        public int? CellPaddingPx { get; set; }

        /// <summary>单元格水平对齐（left/center/right/justify）。</summary>
        public string CellTextAlign { get; set; }

        /// <summary>单元格垂直对齐（top/middle/bottom）。</summary>
        public string CellVerticalAlign { get; set; }

        /// <summary>单元格边框（完整 CSS 值，优先级高于 <see cref="BorderStyle"/> 组合）。</summary>
        public string CellBorder { get; set; }

        /// <summary>边框样式预设（solid/dashed/dotted/double/none）。</summary>
        public string BorderStyle { get; set; }

        /// <summary>边框宽度（像素）。</summary>
        public int? BorderWidthPx { get; set; }

        /// <summary>边框颜色。</summary>
        public string BorderColor { get; set; }

        /// <summary>是否应用上边框。</summary>
        public bool BorderTop { get; set; } = true;

        /// <summary>是否应用右边框。</summary>
        public bool BorderRight { get; set; } = true;

        /// <summary>是否应用下边框。</summary>
        public bool BorderBottom { get; set; } = true;

        /// <summary>是否应用左边框。</summary>
        public bool BorderLeft { get; set; } = true;

        /// <summary>表头单元格文本格式（<c>{0}</c> 会被替换为 1 基列号）。</summary>
        public string DefaultColumnHeader { get; set; }

        /// <summary>由 <see cref="ElHtmlEditorGrowHtmlBuilder"/> 生成的表格 HTML（插入表格时使用）。</summary>
        public string Html { get; set; }

        /// <summary>当前选中的行（0 基，仅用于 <c>updateTable</c> 时的行样式）。</summary>
        public int RowIndex { get; set; } = -1;

        /// <summary>当前选中的列（0 基）。</summary>
        public int ColumnIndex { get; set; } = -1;

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>序列化为浏览器端约定的 JSON。</summary>
        public string ToJson()
        {
            return JsonSerializer.Serialize(this, Options);
        }

        /// <summary>从 JSON 反序列化（空／非法输入返回默认实例）。</summary>
        public static ElHtmlEditorGrowTableArgs Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new ElHtmlEditorGrowTableArgs();
            }

            try
            {
                return JsonSerializer.Deserialize<ElHtmlEditorGrowTableArgs>(json, Options)
                    ?? new ElHtmlEditorGrowTableArgs();
            }
            catch (JsonException)
            {
                return new ElHtmlEditorGrowTableArgs();
            }
        }
    }
}
