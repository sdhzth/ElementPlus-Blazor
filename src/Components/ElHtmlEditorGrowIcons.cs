using System;
using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// ElHtmlEditorGrow 工具栏图标（**自绘内联 SVG**）。
    /// <para>
    /// 本仓库自带的 <c>element-icons</c> 字体只有 link / picture / edit 等少量图形，缺少加粗、斜体、对齐、列表等排版图标；
    /// 为避免引入任何第三方图标包（如 Radzen 的 Material Icons），这里按统一的 24×24 视图盒、2px 圆头描边规则手工绘制。
    /// </para>
    /// <para>
    /// 注意（本仓库既有坑）：Razor 无法直接输出 SVG 的 <c>&lt;text&gt;</c> 元素，因此所有图标**只用 path/circle/rect/line**，
    /// 不含任何 <c>&lt;text&gt;</c> 节点 —— 该约束由单元测试守卫。
    /// </para>
    /// </summary>
    public static class ElHtmlEditorGrowIcons
    {
        /// <summary>默认图标（未知名称时回退）。</summary>
        public const string Fallback = "dot";

        private const string OpenTag =
            "<svg class=\"el-html-editor-grow__icon\" viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" " +
            "fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" " +
            "stroke-linejoin=\"round\" aria-hidden=\"true\" focusable=\"false\">";

        private static readonly Dictionary<string, string> Paths =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // ---- 行内格式 ----
                { "bold", "<path d=\"M7 5h5.2a3.4 3.4 0 0 1 0 6.8H7z\"/><path d=\"M7 11.8h6a3.6 3.6 0 0 1 0 7.2H7z\"/>" },
                { "italic", "<path d=\"M10 5h7\"/><path d=\"M7 19h7\"/><path d=\"M14.5 5l-5 14\"/>" },
                { "underline", "<path d=\"M7 4v7a5 5 0 0 0 10 0V4\"/><path d=\"M6 20h12\"/>" },
                { "strike", "<path d=\"M5 12h14\"/><path d=\"M16.5 7.6A5.2 5.2 0 0 0 12 5c-2.4 0-4 1.1-4 2.8 0 1 .7 1.9 2.4 2.5\"/><path d=\"M7.5 16.4A5.2 5.2 0 0 0 12 19c2.4 0 4-1.1 4-2.8 0-.9-.6-1.8-2.2-2.4\"/>" },
                { "subscript", "<path d=\"M4 6l6 8\"/><path d=\"M10 6l-6 8\"/><path d=\"M15 17h5v4h-5z\"/>" },
                { "superscript", "<path d=\"M4 8l6 8\"/><path d=\"M10 8l-6 8\"/><path d=\"M15 3h5v4h-5z\"/>" },
                { "remove-format", "<path d=\"M5 6h14\"/><path d=\"M12 6v13\"/><path d=\"M4 3l17 18\"/>" },

                // ---- 对齐 ----
                { "align-left", "<path d=\"M4 6h16\"/><path d=\"M4 11h10\"/><path d=\"M4 16h16\"/><path d=\"M4 21h10\"/>" },
                { "align-center", "<path d=\"M4 6h16\"/><path d=\"M7 11h10\"/><path d=\"M4 16h16\"/><path d=\"M7 21h10\"/>" },
                { "align-right", "<path d=\"M4 6h16\"/><path d=\"M10 11h10\"/><path d=\"M4 16h16\"/><path d=\"M10 21h10\"/>" },
                { "align-justify", "<path d=\"M4 6h16\"/><path d=\"M4 11h16\"/><path d=\"M4 16h16\"/><path d=\"M4 21h16\"/>" },

                // ---- 缩进 ----
                { "indent", "<path d=\"M10 6h10\"/><path d=\"M10 12h10\"/><path d=\"M10 18h10\"/><path d=\"M3 8l4 4-4 4z\"/>" },
                { "outdent", "<path d=\"M10 6h10\"/><path d=\"M10 12h10\"/><path d=\"M10 18h10\"/><path d=\"M7 8l-4 4 4 4z\"/>" },

                // ---- 列表 ----
                { "list-unordered", "<path d=\"M9 6h11\"/><path d=\"M9 12h11\"/><path d=\"M9 18h11\"/><path d=\"M4 6h.01\"/><path d=\"M4 12h.01\"/><path d=\"M4 18h.01\"/>" },
                { "list-ordered", "<path d=\"M9 6h11\"/><path d=\"M9 12h11\"/><path d=\"M9 18h11\"/><path d=\"M3.5 5.5L5 4.5V8\"/><path d=\"M3.4 10.6a1.5 1.5 0 1 1 2.3 1.6L3.4 14H6\"/><path d=\"M3.6 16a1.3 1.3 0 1 1 1.3 1.4A1.3 1.3 0 0 1 3.6 19h2.4\"/>" },

                // ---- 撤销 / 重做 ----
                { "undo", "<path d=\"M3 5v5h5\"/><path d=\"M4.2 14a8 8 0 1 0 1.4-8.4L3 10\"/>" },
                { "redo", "<path d=\"M21 5v5h-5\"/><path d=\"M19.8 14a8 8 0 1 1-1.4-8.4L21 10\"/>" },

                // ---- 颜色 / 背景 ----
                { "color", "<path d=\"M6.5 16L12 4l5.5 12\"/><path d=\"M8.8 12.5h6.4\"/><path d=\"M4 21h16\"/>" },
                { "highlight", "<path d=\"M4 20h16\"/><path d=\"M8.5 16.5l6.5-6.5 3 3-6.5 6.5z\"/><path d=\"M15 4.5l4.5 4.5\"/>" },

                // ---- 链接 / 图片 / 表格 / 源码 ----
                { "link", "<path d=\"M10 13.8a4 4 0 0 1 0-5.6l2.2-2.2a4 4 0 0 1 5.6 5.6l-1.4 1.4\"/><path d=\"M14 10.2a4 4 0 0 1 0 5.6l-2.2 2.2a4 4 0 0 1-5.6-5.6l1.4-1.4\"/>" },
                { "unlink", "<path d=\"M10 13.8a4 4 0 0 1 0-5.6l2.2-2.2a4 4 0 0 1 5.6 5.6\"/><path d=\"M14.4 18a4 4 0 0 1-5.6-.2\"/><path d=\"M4 4l16 16\"/>" },
                { "image", "<path d=\"M4 5h16v14H4z\"/><path d=\"M8.5 10.5a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3\"/><path d=\"M4 16.5l4.8-4.8 4 4L16.5 12l3.5 3.5\"/>" },
                { "table", "<path d=\"M4 5h16v14H4z\"/><path d=\"M4 10h16\"/><path d=\"M4 15h16\"/><path d=\"M10 5v14\"/><path d=\"M15 5v14\"/>" },
                { "source", "<path d=\"M9.5 6l-5.5 6 5.5 6\"/><path d=\"M14.5 6l5.5 6-5.5 6\"/>" },

                // ---- 表格行 / 列操作 ----
                { "row-above", "<path d=\"M4 3h16\"/><path d=\"M12 9v12\"/><path d=\"M8 13l4-4 4 4\"/>" },
                { "row-below", "<path d=\"M4 21h16\"/><path d=\"M12 3v12\"/><path d=\"M8 11l4 4 4-4\"/>" },
                { "column-left", "<path d=\"M3 4v16\"/><path d=\"M9 12h12\"/><path d=\"M13 8l-4 4 4 4\"/>" },
                { "column-right", "<path d=\"M21 4v16\"/><path d=\"M3 12h12\"/><path d=\"M11 8l4 4-4 4\"/>" },
                { "delete-row", "<path d=\"M3 12h13\"/><path d=\"M19 9l3 3-3 3\"/><path d=\"M14 6v12\"/>" },
                { "delete-column", "<path d=\"M12 3v13\"/><path d=\"M9 19l3 3 3-3\"/><path d=\"M6 14h12\"/>" },
                { "delete-table", "<path d=\"M4 5h16v14H4z\"/><path d=\"M4 11h16\"/><path d=\"M8 8l8 8\"/><path d=\"M16 8l-8 8\"/>" },
                { "merge-right", "<path d=\"M3 5h7v14H3z\"/><path d=\"M12 12h9\"/><path d=\"M17 9l4 3-4 3\"/>" },
                { "merge-down", "<path d=\"M5 3h14v7H5z\"/><path d=\"M12 12v9\"/><path d=\"M9 17l3 4 3-4\"/>" },
                { "split-cell", "<path d=\"M12 3v18\"/><path d=\"M4 6h5v12H4z\"/><path d=\"M15 6h5v12h-5z\"/>" },
                { "copy-cells", "<path d=\"M8 8h12v12H8z\"/><path d=\"M4 16V4h12\"/>" },
                { "paste-cells", "<path d=\"M9 4h6v3H9z\"/><path d=\"M6 6h12v15H6z\"/><path d=\"M10 12h6\"/><path d=\"M10 16h6\"/>" },

                // ---- 其它 ----
                { "settings", "<path d=\"M12 15.2a3.2 3.2 0 1 0 0-6.4 3.2 3.2 0 0 0 0 6.4\"/><path d=\"M19.4 15a1.7 1.7 0 0 0 .34 1.87l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06A1.7 1.7 0 0 0 15 19.4a1.7 1.7 0 0 0-1 1.55V21a2 2 0 1 1-4 0v-.05A1.7 1.7 0 0 0 9 19.4a1.7 1.7 0 0 0-1.87.34l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06A1.7 1.7 0 0 0 4.6 15a1.7 1.7 0 0 0-1.55-1H3a2 2 0 1 1 0-4h.05A1.7 1.7 0 0 0 4.6 9a1.7 1.7 0 0 0-.34-1.87l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06A1.7 1.7 0 0 0 9 4.6a1.7 1.7 0 0 0 1-1.55V3a2 2 0 1 1 4 0v.05A1.7 1.7 0 0 0 15 4.6a1.7 1.7 0 0 0 1.87-.34l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06A1.7 1.7 0 0 0 19.4 9v.05a1.7 1.7 0 0 0 1.55 1H21a2 2 0 1 1 0 4h-.05a1.7 1.7 0 0 0-1.55 1z\"/>" },
                { "dot", "<path d=\"M12 12h.01\"/>" },
                { "chevron-down", "<path d=\"M6 9l6 6 6-6\"/>" },
                { "check", "<path d=\"M5 13l4.5 4.5L19 7\"/>" },
                { "close", "<path d=\"M6 6l12 12\"/><path d=\"M18 6L6 18\"/>" }
            };

        /// <summary>全部已绘制图标名称。</summary>
        public static IReadOnlyCollection<string> Names => Paths.Keys;

        /// <summary>该图标是否已绘制。</summary>
        public static bool Has(string name)
        {
            return !string.IsNullOrEmpty(name) && Paths.ContainsKey(name);
        }

        /// <summary>取得图标的内部标记（不含外层 <c>&lt;svg&gt;</c>；未知名称返回 <c>null</c>）。</summary>
        public static string GetInner(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return Paths.TryGetValue(name, out var inner) ? inner : null;
        }

        /// <summary>取得完整的内联 SVG 标记（未知名称回退到 <see cref="Fallback"/>）。</summary>
        public static string Get(string name)
        {
            var inner = GetInner(string.IsNullOrEmpty(name) ? Fallback : name) ?? GetInner(Fallback);

            return OpenTag + inner + "</svg>";
        }
    }
}
