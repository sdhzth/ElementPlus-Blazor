using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElementPlus
{
    /// <summary>
    /// 当前光标/选区所在的表格信息。
    /// <para>对应参考实现 Radzen 的 <c>HtmlEditorTableSelection</c>，为纯 C# 模型，可在单元测试中直接构造与断言。</para>
    /// </summary>
    public class ElHtmlEditorGrowTableSelection
    {
        /// <summary>光标是否位于表格内。</summary>
        public bool InTable { get; set; }

        /// <summary>光标是否位于表头单元格（<c>th</c>）。</summary>
        public bool InHeader { get; set; }

        /// <summary>所在行序号（0 基，表体外为 -1）。</summary>
        public int RowIndex { get; set; } = -1;

        /// <summary>所在列序号（0 基，不在表格内为 -1）。</summary>
        public int ColumnIndex { get; set; } = -1;

        /// <summary>表格体行数。</summary>
        public int Rows { get; set; }

        /// <summary>表格列数。</summary>
        public int Columns { get; set; }

        /// <summary>表格宽度。</summary>
        public string Width { get; set; }

        /// <summary>表格边框宽度。</summary>
        public int Border { get; set; }

        /// <summary>表格是否包含表头行。</summary>
        public bool HeaderRow { get; set; }

        /// <summary>当前单元格横向合并数。</summary>
        public int ColSpan { get; set; } = 1;

        /// <summary>当前单元格纵向合并数。</summary>
        public int RowSpan { get; set; } = 1;

        /// <summary>是否可与右侧单元格合并。</summary>
        public bool CanMergeRight { get; set; }

        /// <summary>是否可与下方单元格合并。</summary>
        public bool CanMergeDown { get; set; }

        /// <summary>当前单元格是否可拆分。</summary>
        public bool CanSplit { get; set; }

        /// <summary>当前列宽度。</summary>
        public string ColumnWidth { get; set; }

        /// <summary>当前列宽度（像素）。</summary>
        public int? ColumnWidthPx { get; set; }

        /// <summary>当前单元格背景色。</summary>
        public string CellBackground { get; set; }

        /// <summary>当前单元格内边距。</summary>
        public string CellPadding { get; set; }

        /// <summary>当前单元格内边距（像素）。</summary>
        public int? CellPaddingPx { get; set; }

        /// <summary>当前单元格水平对齐。</summary>
        public string CellTextAlign { get; set; }

        /// <summary>当前单元格垂直对齐。</summary>
        public string CellVerticalAlign { get; set; }

        /// <summary>当前单元格边框样式。</summary>
        public string CellBorder { get; set; }

        /// <summary>当前单元格边框样式预设（solid/dashed/dotted/double/none）。</summary>
        public string BorderStyle { get; set; }

        /// <summary>当前单元格边框宽度（像素）。</summary>
        public int? BorderWidthPx { get; set; }

        /// <summary>当前单元格边框颜色。</summary>
        public string BorderColor { get; set; }

        /// <summary>上边框是否存在。</summary>
        public bool BorderTop { get; set; }

        /// <summary>右边框是否存在。</summary>
        public bool BorderRight { get; set; }

        /// <summary>下边框是否存在。</summary>
        public bool BorderBottom { get; set; }

        /// <summary>左边框是否存在。</summary>
        public bool BorderLeft { get; set; }

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        /// <summary>从浏览器回传的 JSON 解析；空／非法输入返回“不在表格内”的默认实例。</summary>
        public static ElHtmlEditorGrowTableSelection Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new ElHtmlEditorGrowTableSelection();
            }

            try
            {
                return JsonSerializer.Deserialize<ElHtmlEditorGrowTableSelection>(json, Options)
                    ?? new ElHtmlEditorGrowTableSelection();
            }
            catch (JsonException)
            {
                return new ElHtmlEditorGrowTableSelection();
            }
        }

        /// <summary>该表格命令在当前选区内是否可执行（用于禁用工具栏按钮）。</summary>
        public bool CanExecute(string command)
        {
            if (!InTable)
            {
                return false;
            }

            switch (command)
            {
                case ElHtmlEditorGrowCommands.MergeCellRight: return CanMergeRight;
                case ElHtmlEditorGrowCommands.MergeCellDown: return CanMergeDown;
                case ElHtmlEditorGrowCommands.SplitCell: return CanSplit;
                case ElHtmlEditorGrowCommands.CopyCells: return true;
                case ElHtmlEditorGrowCommands.PasteCells: return true;
                default: return true;
            }
        }
    }
}
