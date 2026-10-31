using System;
using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// <see cref="ElPivotDataGridGrow{TItem}"/> 的内联 SVG 图标集。
    /// <para>
    /// 与参考实现依赖图标字体（<c>rzi-*</c> class）不同：这里全部是**手绘的内联 SVG**，
    /// 不加载字体、不引入图标包，任意宿主都能正确显示。
    /// </para>
    /// <para>
    /// ⚠️ 本仓库既有的 Razor/SVG 陷阱：Razor 无法直接输出 SVG 的 <c>&lt;text&gt;</c> 元素。
    /// 因此本图标集**只使用 path/rect/circle**，且整段标记由 C# 拼接后用
    /// <see cref="Microsoft.AspNetCore.Components.MarkupString"/> 输出，不经过 Razor 的元素解析。
    /// </para>
    /// </summary>
    public static class ElPivotDataGridGrowIcons
    {
        /// <summary>
        /// 根 <c>&lt;svg&gt;</c> 开始标签。尺寸用 <c>1em</c> 跟随字号，颜色用 <c>currentColor</c> 跟随文本色。
        /// </summary>
        public const string OpenTag =
            "<svg class=\"el-pivot-grow__icon\" viewBox=\"0 0 24 24\" width=\"1em\" height=\"1em\" " +
            "xmlns=\"http://www.w3.org/2000/svg\" aria-hidden=\"true\" focusable=\"false\" role=\"img\">";

        private const string Stroke = "fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\"";

        private static readonly Dictionary<string, string> paths = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // 未排序：上下两个箭头
            ["sort"] =
                "<path d=\"M8 10l4-4 4 4\" " + Stroke + "/>" +
                "<path d=\"M8 14l4 4 4-4\" " + Stroke + "/>",

            // 升序：向上箭头 + 底部基线
            ["sort-asc"] =
                "<path d=\"M8 14l4-5 4 5\" " + Stroke + "/>" +
                "<path d=\"M6 19h12\" " + Stroke + "/>",

            // 降序：向下箭头 + 顶部基线
            ["sort-desc"] =
                "<path d=\"M8 10l4 5 4-5\" " + Stroke + "/>" +
                "<path d=\"M6 5h12\" " + Stroke + "/>",

            // 筛选：漏斗
            ["filter"] =
                "<path d=\"M4 5h16l-6.2 7.4v5.9l-3.6 2.1v-8z\" " + Stroke + "/>",

            // 展开（折叠状态下）
            ["caret-right"] =
                "<path d=\"M10 7l5 5-5 5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>",

            // 收起（展开状态下）
            ["caret-down"] =
                "<path d=\"M7 10l5 5 5-5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>",

            // 下拉箭头（字段选择器等）
            ["chevron-down"] =
                "<path d=\"M6 9l6 6 6-6\" " + Stroke + "/>",

            // 关闭
            ["close"] =
                "<path d=\"M7 7l10 10\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\"/>" +
                "<path d=\"M17 7L7 17\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\"/>",

            // 字段选择：表格轮廓
            ["fields"] =
                "<rect x=\"4\" y=\"5\" width=\"16\" height=\"14\" rx=\"1.5\" " + Stroke + "/>" +
                "<path d=\"M4 10h16\" " + Stroke + "/>" +
                "<path d=\"M10 10v9\" " + Stroke + "/>",

            // 兜底
            ["dot"] =
                "<circle cx=\"12\" cy=\"12\" r=\"3\" fill=\"currentColor\"/>"
        };

        /// <summary>兜底图标名。</summary>
        public const string Fallback = "dot";

        /// <summary>全部图标名（按声明顺序）。</summary>
        public static IReadOnlyList<string> Names
        {
            get
            {
                var list = new List<string>(paths.Count);

                foreach (var pair in paths)
                {
                    list.Add(pair.Key);
                }

                return list;
            }
        }

        /// <summary>是否声明了指定图标。</summary>
        public static bool Has(string name)
        {
            return !string.IsNullOrEmpty(name) && paths.ContainsKey(name);
        }

        /// <summary>
        /// 取图标内部标记（不含 <c>&lt;svg&gt;</c> 外壳）；未知图标回退到 <see cref="Fallback"/>。
        /// </summary>
        public static string GetInner(string name)
        {
            if (!string.IsNullOrEmpty(name) && paths.TryGetValue(name, out var inner))
            {
                return inner;
            }

            return paths[Fallback];
        }

        /// <summary>
        /// 取完整的 <c>&lt;svg&gt;…&lt;/svg&gt;</c> 标记；未知图标回退到 <see cref="Fallback"/>。
        /// </summary>
        public static string Get(string name)
        {
            return OpenTag + GetInner(name) + "</svg>";
        }
    }
}
