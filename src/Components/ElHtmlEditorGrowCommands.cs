using System;
using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// ElHtmlEditorGrow 支持的富文本命令。
    /// <para>
    /// 命名与参考实现 Radzen 的 <c>HtmlEditorCommands</c> 保持一致，便于代码迁移；
    /// 全部为**本仓库自写常量**，不引用任何第三方库。
    /// </para>
    /// <para>
    /// <see cref="ToExecCommand"/> 负责把公开命令名映射到浏览器 <c>document.execCommand</c> 的原生命令，
    /// 其中 <see cref="InsertHtml"/> 会被映射为标准写法 <c>insertHTML</c>，
    /// <see cref="InsertTable"/> / <see cref="UpdateTable"/> 等表格命令则由自写 JS 处理、不经过 execCommand。
    /// </para>
    /// </summary>
    public static class ElHtmlEditorGrowCommands
    {
        /// <summary>在光标处插入 HTML 片段。</summary>
        public const string InsertHtml = "insertHtml";

        /// <summary>居中。</summary>
        public const string AlignCenter = "justifyCenter";

        /// <summary>左对齐。</summary>
        public const string AlignLeft = "justifyLeft";

        /// <summary>右对齐。</summary>
        public const string AlignRight = "justifyRight";

        /// <summary>两端对齐。</summary>
        public const string Justify = "justifyFull";

        /// <summary>设置所选文字的背景色。</summary>
        public const string Background = "backColor";

        /// <summary>加粗。</summary>
        public const string Bold = "bold";

        /// <summary>设置所选文字的字体颜色。</summary>
        public const string Color = "foreColor";

        /// <summary>设置字体名称。</summary>
        public const string FontName = "fontName";

        /// <summary>设置字号。</summary>
        public const string FontSize = "fontSize";

        /// <summary>设置块级格式（p / h1..h6）。</summary>
        public const string FormatBlock = "formatBlock";

        /// <summary>增加缩进。</summary>
        public const string Indent = "indent";

        /// <summary>斜体。</summary>
        public const string Italic = "italic";

        /// <summary>有序列表。</summary>
        public const string OrderedList = "insertOrderedList";

        /// <summary>减少缩进。</summary>
        public const string Outdent = "outdent";

        /// <summary>重做。</summary>
        public const string Redo = "redo";

        /// <summary>清除格式。</summary>
        public const string RemoveFormat = "removeFormat";

        /// <summary>删除线。</summary>
        public const string StrikeThrough = "strikeThrough";

        /// <summary>下标。</summary>
        public const string Subscript = "subscript";

        /// <summary>上标。</summary>
        public const string Superscript = "superscript";

        /// <summary>下划线。</summary>
        public const string Underline = "underline";

        /// <summary>撤销。</summary>
        public const string Undo = "undo";

        /// <summary>取消链接。</summary>
        public const string Unlink = "unlink";

        /// <summary>无序列表。</summary>
        public const string UnorderedList = "insertUnorderedList";

        /// <summary>插入水平分割线。</summary>
        public const string HorizontalRule = "insertHorizontalRule";

        /// <summary>插入表格（由自写 JS 处理，不使用 execCommand）。</summary>
        public const string InsertTable = "insertTable";

        /// <summary>更新当前表格（由自写 JS 处理）。</summary>
        public const string UpdateTable = "updateTable";

        /// <summary>在当前行上方插入行。</summary>
        public const string AddRowBefore = "addRowBefore";

        /// <summary>在当前行下方插入行。</summary>
        public const string AddRowAfter = "addRowAfter";

        /// <summary>在当前列左侧插入列。</summary>
        public const string AddColumnBefore = "addColumnBefore";

        /// <summary>在当前列右侧插入列。</summary>
        public const string AddColumnAfter = "addColumnAfter";

        /// <summary>删除当前行。</summary>
        public const string DeleteRow = "deleteRow";

        /// <summary>删除当前列。</summary>
        public const string DeleteColumn = "deleteColumn";

        /// <summary>删除当前表格。</summary>
        public const string DeleteTable = "deleteTable";

        /// <summary>与右侧单元格合并。</summary>
        public const string MergeCellRight = "mergeCellRight";

        /// <summary>与下方单元格合并。</summary>
        public const string MergeCellDown = "mergeCellDown";

        /// <summary>拆分当前合并单元格。</summary>
        public const string SplitCell = "splitCell";

        /// <summary>复制选中单元格内容。</summary>
        public const string CopyCells = "copyCells";

        /// <summary>粘贴单元格内容。</summary>
        public const string PasteCells = "pasteCells";

        private static readonly Dictionary<string, string> ExecCommandAliases =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { InsertHtml, "insertHTML" },
                { HorizontalRule, "insertHorizontalRule" }
            };

        private static readonly HashSet<string> TableCommands =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                InsertTable, UpdateTable, AddRowBefore, AddRowAfter,
                AddColumnBefore, AddColumnAfter, DeleteRow, DeleteColumn, DeleteTable,
                MergeCellRight, MergeCellDown, SplitCell, CopyCells, PasteCells
            };

        /// <summary>该命令是否需要参数（颜色、字体名、字号、块格式、插入的 HTML 等）。</summary>
        public static bool RequiresValue(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return false;
            }

            return string.Equals(command, InsertHtml, StringComparison.OrdinalIgnoreCase)
                || string.Equals(command, Color, StringComparison.OrdinalIgnoreCase)
                || string.Equals(command, Background, StringComparison.OrdinalIgnoreCase)
                || string.Equals(command, FontName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(command, FontSize, StringComparison.OrdinalIgnoreCase)
                || string.Equals(command, FormatBlock, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>是否为表格类命令（走自写 JS 的表格通道）。</summary>
        public static bool IsTableCommand(string command)
        {
            return !string.IsNullOrEmpty(command) && TableCommands.Contains(command);
        }

        /// <summary>是否为“开关型”命令（可呈选中态）。</summary>
        public static bool IsToggle(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return false;
            }

            switch (command.ToLowerInvariant())
            {
                case "bold":
                case "italic":
                case "underline":
                case "strikethrough":
                case "subscript":
                case "superscript":
                case "justifyleft":
                case "justifycenter":
                case "justifyright":
                case "justifyfull":
                case "insertorderedlist":
                case "insertunorderedlist":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 把公开命令名映射为浏览器原生 <c>document.execCommand</c> 命令名。
        /// 表格类命令返回 <c>null</c>（由自写 JS 单独处理）。
        /// </summary>
        public static string ToExecCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return null;
            }

            if (IsTableCommand(command))
            {
                return null;
            }

            return ExecCommandAliases.TryGetValue(command, out var mapped) ? mapped : command;
        }

        /// <summary>在浏览器里查询某个开关型命令当前是否处于激活状态。</summary>
        public static bool IsActive(ElHtmlEditorGrowState state, string command)
        {
            if (state == null || string.IsNullOrEmpty(command))
            {
                return false;
            }

            switch (command.ToLowerInvariant())
            {
                case "bold": return state.Bold;
                case "italic": return state.Italic;
                case "underline": return state.Underline;
                case "strikethrough": return state.StrikeThrough;
                case "subscript": return state.Subscript;
                case "superscript": return state.Superscript;
                case "justifyleft": return state.JustifyLeft;
                case "justifycenter": return state.JustifyCenter;
                case "justifyright": return state.JustifyRight;
                case "justifyfull": return state.JustifyFull;
                case "unlink": return state.Unlink;
                case "insertorderedlist": return false;
                case "insertunorderedlist": return false;
                default: return false;
            }
        }
    }
}
