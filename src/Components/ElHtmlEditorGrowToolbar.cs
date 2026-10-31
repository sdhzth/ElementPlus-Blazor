using System;
using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>工具栏工具的呈现类型。</summary>
    public enum ElHtmlEditorGrowToolKind
    {
        /// <summary>竖线分隔符。</summary>
        Separator,

        /// <summary>图标按钮（执行一个命令）。</summary>
        Button,

        /// <summary>下拉选择（字体 / 字号 / 块格式）。</summary>
        Select,

        /// <summary>颜色选择（文字颜色 / 背景色）。</summary>
        Color,

        /// <summary>表格操作按钮（仅在光标位于表格内时可用）。</summary>
        TableCommand
    }

    /// <summary>下拉选项。</summary>
    public class ElHtmlEditorGrowToolOption
    {
        /// <summary>选项显示文本。</summary>
        public string Text { get; set; }

        /// <summary>选项值（传给命令）。</summary>
        public string Value { get; set; }

        /// <summary>选项自身的预览样式（如字体名、字号）。</summary>
        public string Style { get; set; }

        /// <summary>构造选项。</summary>
        public static ElHtmlEditorGrowToolOption Create(string text, string value, string style = null)
        {
            return new ElHtmlEditorGrowToolOption { Text = text, Value = value, Style = style };
        }
    }

    /// <summary>
    /// 一个工具栏工具的完整描述。工具栏由**数据驱动**：<see cref="ElHtmlEditorGrowToolbar.Default"/> 决定默认工具栏的
    /// 组成、顺序、图标、快捷键与生效模式，组件只负责按描述渲染，因此工具栏结构可在单元测试中直接断言。
    /// </summary>
    public class ElHtmlEditorGrowToolDescriptor
    {
        /// <summary>工具名（唯一标识）。</summary>
        public string Name { get; set; }

        /// <summary>呈现类型。</summary>
        public ElHtmlEditorGrowToolKind Kind { get; set; } = ElHtmlEditorGrowToolKind.Button;

        /// <summary>点击后执行的命令（<see cref="ElHtmlEditorGrowCommands"/> 中的常量）。</summary>
        public string Command { get; set; }

        /// <summary>悬停提示文本。</summary>
        public string Title { get; set; }

        /// <summary>图标名（<see cref="ElHtmlEditorGrowIcons"/>）。</summary>
        public string Icon { get; set; }

        /// <summary>颜色工具的默认值。</summary>
        public string Value { get; set; }

        /// <summary>下拉选项。</summary>
        public IReadOnlyList<ElHtmlEditorGrowToolOption> Options { get; set; }

        /// <summary>状态字段名（用于下拉回显与按钮激活判定，如 <c>FormatBlock</c>）。</summary>
        public string StateProperty { get; set; }

        /// <summary>快捷键（如 <c>Ctrl+B</c>）。</summary>
        public string Shortcut { get; set; }

        /// <summary>
        /// 生效模式，默认仅设计模式。
        /// <para>
        /// 语义是「**可点 / 置灰**」而不是「渲染 / 不渲染」：不在生效模式内的工具仍然会渲染在工具栏上，
        /// 只是带上 <c>disabled</c> 与 <c>is-disabled</c>（对齐参考实现的 <c>EditorButton.EnabledModes</c>）。
        /// 于是源码视图下工具栏依旧是一整套带图标的按钮，只是设计类命令变成灰的，布局不会跳动。
        /// 想让某个工具彻底不出现，用组件的 <c>HiddenTools</c>。
        /// </para>
        /// </summary>
        public ElHtmlEditorGrowMode EnabledModes { get; set; } = ElHtmlEditorGrowMode.Design;

        /// <summary>是否需要光标位于表格内才可用。</summary>
        public bool RequiresTable { get; set; }

        /// <summary>下拉框的宽度（CSS）。</summary>
        public string Width { get; set; }
    }

    /// <summary>
    /// ElHtmlEditorGrow 的默认工具栏定义。
    /// <para>全部数据为**纯 C# 静态数据**，不依赖任何第三方库；参考实现的工具栏由一组 <c>RadzenHtmlEditor*</c> 子组件拼装，这里等价地用一个描述符列表表达，便于测试与自定义。</para>
    /// </summary>
    public static class ElHtmlEditorGrowToolbar
    {
        /// <summary>字号下拉（对齐参考实现：字号值 1–7 对应 10/13/16/18/24/32/48px）。</summary>
        public static IReadOnlyList<ElHtmlEditorGrowToolOption> FontSizeOptions { get; } = new List<ElHtmlEditorGrowToolOption>
        {
            ElHtmlEditorGrowToolOption.Create("10px", "1", "font-size:10px"),
            ElHtmlEditorGrowToolOption.Create("13px", "2", "font-size:13px"),
            ElHtmlEditorGrowToolOption.Create("16px", "3", "font-size:16px"),
            ElHtmlEditorGrowToolOption.Create("18px", "4", "font-size:18px"),
            ElHtmlEditorGrowToolOption.Create("24px", "5", "font-size:24px"),
            ElHtmlEditorGrowToolOption.Create("32px", "6", "font-size:32px"),
            ElHtmlEditorGrowToolOption.Create("48px", "7", "font-size:48px")
        };

        /// <summary>字体下拉。</summary>
        public static IReadOnlyList<ElHtmlEditorGrowToolOption> FontNameOptions { get; } = new List<ElHtmlEditorGrowToolOption>
        {
            ElHtmlEditorGrowToolOption.Create("Arial", "Arial", "font-family:Arial"),
            ElHtmlEditorGrowToolOption.Create("Georgia", "Georgia", "font-family:Georgia"),
            ElHtmlEditorGrowToolOption.Create("Helvetica", "Helvetica", "font-family:Helvetica"),
            ElHtmlEditorGrowToolOption.Create("等宽", "monospace", "font-family:monospace"),
            ElHtmlEditorGrowToolOption.Create("Segoe UI", "\"Segoe UI\"", "font-family:'Segoe UI'"),
            ElHtmlEditorGrowToolOption.Create("Tahoma", "Tahoma", "font-family:Tahoma"),
            ElHtmlEditorGrowToolOption.Create("Times New Roman", "\"Times New Roman\"", "font-family:'Times New Roman'"),
            ElHtmlEditorGrowToolOption.Create("Verdana", "Verdana", "font-family:Verdana"),
            ElHtmlEditorGrowToolOption.Create("宋体", "SimSun", "font-family:SimSun"),
            ElHtmlEditorGrowToolOption.Create("微软雅黑", "\"Microsoft YaHei\"", "font-family:'Microsoft YaHei'")
        };

        /// <summary>块级格式下拉。</summary>
        public static IReadOnlyList<ElHtmlEditorGrowToolOption> FormatBlockOptions { get; } = new List<ElHtmlEditorGrowToolOption>
        {
            ElHtmlEditorGrowToolOption.Create("正文", "p", "font-size:14px"),
            ElHtmlEditorGrowToolOption.Create("标题 1", "h1", "font-size:22px;font-weight:600"),
            ElHtmlEditorGrowToolOption.Create("标题 2", "h2", "font-size:19px;font-weight:600"),
            ElHtmlEditorGrowToolOption.Create("标题 3", "h3", "font-size:17px;font-weight:600"),
            ElHtmlEditorGrowToolOption.Create("标题 4", "h4", "font-size:15px;font-weight:600"),
            ElHtmlEditorGrowToolOption.Create("标题 5", "h5", "font-size:14px;font-weight:600"),
            ElHtmlEditorGrowToolOption.Create("标题 6", "h6", "font-size:13px;font-weight:600")
        };

        /// <summary>默认颜色面板（10 色相 × 5 明度，末行为灰阶）。</summary>
        public static IReadOnlyList<string> Palette { get; } = new List<string>
        {
            "#f56c6c", "#e6a23c", "#f7ba2a", "#67c23a", "#20c997", "#409eff", "#5b8ff9", "#722ed1", "#eb2f96", "#909399",
            "#f89898", "#f3d19e", "#f9e79f", "#b3e19d", "#96dcc9", "#a0cfff", "#adc6ff", "#d3adf7", "#fab6d4", "#c8c9cc",
            "#c45656", "#b88230", "#c7a227", "#529b2e", "#1a9b74", "#337ecc", "#4059b8", "#5b2ea6", "#b31d76", "#606266",
            "#fab6b6", "#fceed0", "#fdf3c0", "#d9f0c6", "#c4ece0", "#d9ecff", "#dbe4ff", "#e9dcfb", "#fbd6ea", "#e9e9eb",
            "#303133", "#606266", "#909399", "#a8abb2", "#c0c4cc", "#dcdfe6", "#e4e7ed", "#ebeef5", "#f2f3f5", "#ffffff"
        };

        private static IReadOnlyList<ElHtmlEditorGrowToolDescriptor> defaultTools;

        /// <summary>默认工具栏（顺序与参考实现一致）。</summary>
        public static IReadOnlyList<ElHtmlEditorGrowToolDescriptor> Default =>
            defaultTools ?? (defaultTools = Build());

        /// <summary>按名称查找工具描述。</summary>
        public static ElHtmlEditorGrowToolDescriptor Find(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            foreach (var tool in Default)
            {
                if (string.Equals(tool.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return tool;
                }
            }

            return null;
        }

        private static IReadOnlyList<ElHtmlEditorGrowToolDescriptor> Build()
        {
            return new List<ElHtmlEditorGrowToolDescriptor>
            {
                Button("undo", ElHtmlEditorGrowCommands.Undo, "撤销", "undo", "Ctrl+Z"),
                Button("redo", ElHtmlEditorGrowCommands.Redo, "重做", "redo", "Ctrl+Y"),

                Separator(),

                Button("bold", ElHtmlEditorGrowCommands.Bold, "加粗", "bold", "Ctrl+B"),
                Button("italic", ElHtmlEditorGrowCommands.Italic, "斜体", "italic", "Ctrl+I"),
                Button("underline", ElHtmlEditorGrowCommands.Underline, "下划线", "underline", "Ctrl+U"),
                Button("strike", ElHtmlEditorGrowCommands.StrikeThrough, "删除线", "strike"),

                Separator(),

                Button("align-left", ElHtmlEditorGrowCommands.AlignLeft, "左对齐", "align-left"),
                Button("align-center", ElHtmlEditorGrowCommands.AlignCenter, "居中", "align-center"),
                Button("align-right", ElHtmlEditorGrowCommands.AlignRight, "右对齐", "align-right"),
                Button("align-justify", ElHtmlEditorGrowCommands.Justify, "两端对齐", "align-justify"),

                Separator(),

                Button("indent", ElHtmlEditorGrowCommands.Indent, "增加缩进", "indent", "Tab"),
                Button("outdent", ElHtmlEditorGrowCommands.Outdent, "减少缩进", "outdent", "Shift+Tab"),
                Button("list-unordered", ElHtmlEditorGrowCommands.UnorderedList, "无序列表", "list-unordered"),
                Button("list-ordered", ElHtmlEditorGrowCommands.OrderedList, "有序列表", "list-ordered"),

                Separator(),

                Color("color", ElHtmlEditorGrowCommands.Color, "文字颜色", "color", "#f56c6c"),
                Color("highlight", ElHtmlEditorGrowCommands.Background, "背景颜色", "highlight", "#f7ba2a"),
                Button("remove-format", ElHtmlEditorGrowCommands.RemoveFormat, "清除格式", "remove-format"),

                Separator(),

                Button("subscript", ElHtmlEditorGrowCommands.Subscript, "下标", "subscript"),
                Button("superscript", ElHtmlEditorGrowCommands.Superscript, "上标", "superscript"),

                Separator(),

                Button("link", "link", "插入链接", "link", "Ctrl+K"),
                Button("unlink", ElHtmlEditorGrowCommands.Unlink, "取消链接", "unlink"),
                Button("image", "image", "插入图片", "image"),
                Button("table", ElHtmlEditorGrowCommands.InsertTable, "插入表格", "table"),

                Select("font-name", ElHtmlEditorGrowCommands.FontName, "字体", "FontName", FontNameOptions, "110px"),
                Select("font-size", ElHtmlEditorGrowCommands.FontSize, "字号", "FontSize", FontSizeOptions, "76px"),
                Select("format-block", ElHtmlEditorGrowCommands.FormatBlock, "段落样式", "FormatBlock", FormatBlockOptions, "104px"),

                Separator(),

                TableCommand("row-above", ElHtmlEditorGrowCommands.AddRowBefore, "上方插入行", "row-above"),
                TableCommand("row-below", ElHtmlEditorGrowCommands.AddRowAfter, "下方插入行", "row-below"),
                TableCommand("column-left", ElHtmlEditorGrowCommands.AddColumnBefore, "左侧插入列", "column-left"),
                TableCommand("column-right", ElHtmlEditorGrowCommands.AddColumnAfter, "右侧插入列", "column-right"),
                TableCommand("delete-row", ElHtmlEditorGrowCommands.DeleteRow, "删除行", "delete-row"),
                TableCommand("delete-column", ElHtmlEditorGrowCommands.DeleteColumn, "删除列", "delete-column"),
                TableCommand("merge-right", ElHtmlEditorGrowCommands.MergeCellRight, "向右合并", "merge-right"),
                TableCommand("merge-down", ElHtmlEditorGrowCommands.MergeCellDown, "向下合并", "merge-down"),
                TableCommand("split-cell", ElHtmlEditorGrowCommands.SplitCell, "拆分单元格", "split-cell"),
                TableCommand("delete-table", ElHtmlEditorGrowCommands.DeleteTable, "删除表格", "delete-table"),

                Separator(),

                SourceTool()
            };
        }

        private static ElHtmlEditorGrowToolDescriptor Button(string name, string command, string title, string icon, string shortcut = null)
        {
            return new ElHtmlEditorGrowToolDescriptor
            {
                Name = name,
                Kind = ElHtmlEditorGrowToolKind.Button,
                Command = command,
                Title = title,
                Icon = icon,
                Shortcut = shortcut
            };
        }

        private static ElHtmlEditorGrowToolDescriptor Separator()
        {
            return new ElHtmlEditorGrowToolDescriptor
            {
                Name = "separator",
                Kind = ElHtmlEditorGrowToolKind.Separator,
                // 分隔符自身不跟模式走，它是「两段工具之间的那条竖线」；
                // 是否真的渲染由组件按「左右是否都有工具」决定（行首 / 行尾 / 连续的分隔符一律不渲染）。
                EnabledModes = ElHtmlEditorGrowMode.All
            };
        }

        private static ElHtmlEditorGrowToolDescriptor Color(string name, string command, string title, string icon, string value)
        {
            return new ElHtmlEditorGrowToolDescriptor
            {
                Name = name,
                Kind = ElHtmlEditorGrowToolKind.Color,
                Command = command,
                Title = title,
                Icon = icon,
                Value = value
            };
        }

        private static ElHtmlEditorGrowToolDescriptor Select(
            string name, string command, string title, string stateProperty,
            IReadOnlyList<ElHtmlEditorGrowToolOption> options, string width)
        {
            return new ElHtmlEditorGrowToolDescriptor
            {
                Name = name,
                Kind = ElHtmlEditorGrowToolKind.Select,
                Command = command,
                Title = title,
                StateProperty = stateProperty,
                Options = options,
                Width = width
            };
        }

        private static ElHtmlEditorGrowToolDescriptor TableCommand(string name, string command, string title, string icon)
        {
            return new ElHtmlEditorGrowToolDescriptor
            {
                Name = name,
                Kind = ElHtmlEditorGrowToolKind.TableCommand,
                Command = command,
                Title = title,
                Icon = icon,
                RequiresTable = true
            };
        }

        private static ElHtmlEditorGrowToolDescriptor SourceTool()
        {
            return new ElHtmlEditorGrowToolDescriptor
            {
                Name = "source",
                Kind = ElHtmlEditorGrowToolKind.Button,
                Command = null,
                Title = "源码 / 设计视图",
                Icon = "source",
                EnabledModes = ElHtmlEditorGrowMode.All
            };
        }
    }
}
