namespace ElementPlus
{
    /// <summary>
    /// ElHtmlEditorGrow 表格工具与对话框的可本地化字符串（中文默认值）。
    /// <para>对应参考实现 Radzen 的 <c>HtmlEditorTableStrings</c>，全部为可空覆盖项：为 <c>null</c> 时使用内置默认值。</para>
    /// </summary>
    public class ElHtmlEditorGrowTableStrings
    {
        /// <summary>表格对话框标题。</summary>
        public string DialogTitle { get; set; }

        /// <summary>表头单元格文本格式（<c>{0}</c> 为 1 基列号）。</summary>
        public string DefaultColumnHeader { get; set; }

        /// <summary>“行数”标签。</summary>
        public string Rows { get; set; }

        /// <summary>“列数”标签。</summary>
        public string Columns { get; set; }

        /// <summary>“宽度”标签。</summary>
        public string Width { get; set; }

        /// <summary>“边框”标签。</summary>
        public string Border { get; set; }

        /// <summary>“包含表头行”标签。</summary>
        public string HeaderRow { get; set; }

        /// <summary>“编辑表格”分区标题。</summary>
        public string Edit { get; set; }

        /// <summary>“插入”按钮。</summary>
        public string OK { get; set; }

        /// <summary>“更新”按钮。</summary>
        public string Update { get; set; }

        /// <summary>“取消”按钮。</summary>
        public string Cancel { get; set; }

        /// <summary>“复制单元格”。</summary>
        public string CopyCells { get; set; }

        /// <summary>“粘贴单元格”。</summary>
        public string PasteCells { get; set; }

        /// <summary>“上方插入行”。</summary>
        public string InsertRowAbove { get; set; }

        /// <summary>“下方插入行”。</summary>
        public string InsertRowBelow { get; set; }

        /// <summary>“左侧插入列”。</summary>
        public string InsertColumnLeft { get; set; }

        /// <summary>“右侧插入列”。</summary>
        public string InsertColumnRight { get; set; }

        /// <summary>“删除行”。</summary>
        public string DeleteRow { get; set; }

        /// <summary>“删除列”。</summary>
        public string DeleteColumn { get; set; }

        /// <summary>“向右合并”。</summary>
        public string MergeRight { get; set; }

        /// <summary>“向下合并”。</summary>
        public string MergeDown { get; set; }

        /// <summary>“拆分单元格”。</summary>
        public string SplitCell { get; set; }

        /// <summary>“删除表格”。</summary>
        public string DeleteTable { get; set; }

        /// <summary>“列宽”标签。</summary>
        public string ColumnWidth { get; set; }

        /// <summary>“单元格背景”标签。</summary>
        public string CellBackground { get; set; }

        /// <summary>“单元格内边距”标签。</summary>
        public string CellPadding { get; set; }

        /// <summary>“水平对齐”标签。</summary>
        public string CellTextAlign { get; set; }

        /// <summary>“垂直对齐”标签。</summary>
        public string CellVerticalAlign { get; set; }

        /// <summary>“单元格边框”标签。</summary>
        public string CellBorder { get; set; }

        /// <summary>“列宽(px)”标签。</summary>
        public string ColumnWidthPx { get; set; }

        /// <summary>“内边距(px)”标签。</summary>
        public string CellPaddingPx { get; set; }

        /// <summary>“边框样式”标签。</summary>
        public string BorderStyle { get; set; }

        /// <summary>“边框宽度(px)”标签。</summary>
        public string BorderWidthPx { get; set; }

        /// <summary>“边框颜色”标签。</summary>
        public string BorderColor { get; set; }

        /// <summary>“上边框”标签。</summary>
        public string BorderTop { get; set; }

        /// <summary>“右边框”标签。</summary>
        public string BorderRight { get; set; }

        /// <summary>“下边框”标签。</summary>
        public string BorderBottom { get; set; }

        /// <summary>“左边框”标签。</summary>
        public string BorderLeft { get; set; }

        /// <summary>表格操作被阻止时的提示标题。</summary>
        public string ActionBlocked { get; set; }

        /// <summary>需要把光标放到表格内才能执行时的提示。</summary>
        public string ActionRequiresTable { get; set; }

        /// <summary>复制区域不规则时的提示。</summary>
        public string CopyInvalidSelection { get; set; }

        /// <summary>粘贴会覆盖合并单元格时的提示。</summary>
        public string PasteBlocked { get; set; }

        /// <summary>合并区域不规则时的提示。</summary>
        public string MergeInvalidSelection { get; set; }

        /// <summary>把命令失败代号转换为可读文本；未识别时原样返回。</summary>
        public string Resolve(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }

            switch (message)
            {
                case "table.action.requiresTable":
                    return ActionRequiresTable ?? "请先把光标放入表格内";
                case "table.copy.invalidSelection":
                    return CopyInvalidSelection ?? "请选择规则的矩形区域后再复制";
                case "table.paste.blocked":
                    return PasteBlocked ?? "粘贴区域与合并单元格冲突，已取消粘贴";
                case "table.merge.invalidSelection":
                    return MergeInvalidSelection ?? "所选单元格不是规则的矩形区域，无法合并";
                default:
                    return message;
            }
        }
    }
}
