using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ElementPlus;
using ElementPlus.X;
using Microsoft.AspNetCore.Components;

namespace ElementPlus.Blazor.Demo.Shared
{
    /// <summary>
    /// 通过反射扫描 Element / Element.X 程序集，找出所有 Blazor 组件，
    /// 并归类到演示目录。供侧边栏导航与“全部组件”自动画廊共用。
    /// </summary>
    public class CatalogEntry
    {
        public Type Type { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsGeneric { get; set; }
        public string Assembly { get; set; } = string.Empty;
    }

    public static class ComponentCatalog
    {
        public static IReadOnlyList<CatalogEntry> All { get; } = Build();

        public static ILookup<string, CatalogEntry> ByCategory { get; } =
            All.ToLookup(e => e.Category, e => e);

        private static List<CatalogEntry> Build()
        {
            var assemblies = new[] { typeof(ElementPlus.Extension).Assembly, typeof(ElementPlus.X.ExtensionBuilder).Assembly };
            var icomponent = typeof(IComponent);
            var seen = new HashSet<string>();
            var list = new List<CatalogEntry>();

            foreach (var asm in assemblies)
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }

                foreach (var t in types)
                {
                    if (t == null || !t.IsPublic || t.IsAbstract || !t.IsClass) continue;
                    if (!icomponent.IsAssignableFrom(t)) continue;
                    // 跳过布局/演示自身所在程序集之外的误入（本 demo 程序集不在此扫描范围）
                    var name = t.Name;
                    if (seen.Contains(name)) continue;
                    seen.Add(name);

                    list.Add(new CatalogEntry
                    {
                        Type = t,
                        Name = name,
                        Category = Categorize(t),
                        IsGeneric = t.IsGenericTypeDefinition,
                        Assembly = asm == typeof(ElementPlus.Extension).Assembly ? "ElementPlus" : "ElementPlus.X",
                    });
                }
            }

            list.Sort((a, b) =>
            {
                var c = string.CompareOrdinal(a.Category, b.Category);
                return c != 0 ? c : string.CompareOrdinal(a.Name, b.Name);
            });
            return list;
        }

        private static string Categorize(Type t)
        {
            if (t.Namespace != null && t.Namespace.Contains(".X")) return "AI / X 组件";
            var n = t.Name;
            if (n.StartsWith("ElX")) return "AI / X 组件";

            // 自研 / Grow 组件
            if (n.Contains("Pivot") || n.Contains("Viewer") || n.Contains("Barcode") || n.Contains("QRCode")
                || n.Contains("Chart") || n.Contains("Heatmap") || n.Contains("DropDownTable")
                || n.Contains("FormField") || n.Contains("Markdown") || n.Contains("DialogGrow")
                // 富文本编辑器也是本仓库自渲染的（工具栏 45 项 + 自绘 SVG 图标），
                // 不归到“表单组件”里，否则会被误认为有第三方编辑器依赖。
                || n.Contains("HtmlEditor"))
                return "自研 / Grow 组件";

            // 导航
            if (n.Contains("Menu") || n.Contains("Breadcrumb") || n.Contains("Tabs") || n.Contains("Dropdown")
                || n.Contains("Pagination") || n.Contains("Anchor") || n.Contains("Backtop") || n.Contains("PageHeader"))
                return "导航组件";

            // 反馈
            if (n.Contains("Alert") || n.Contains("Dialog") || n.Contains("Loading") || n.Contains("Tooltip")
                || n.Contains("Popover") || n.Contains("Message") || n.Contains("Notification") || n.Contains("Result")
                || n.Contains("Progress") || n.Contains("Badge"))
                return "反馈组件";

            // 表单
            if (n.Contains("Input") || n.Contains("Select") || n.Contains("Checkbox") || n.Contains("Radio")
                || n.Contains("Switch") || n.Contains("Rate") || n.Contains("Slider") || n.Contains("ColorPicker")
                || n.Contains("DatePicker") || n.Contains("TimePicker") || n.Contains("Cascader") || n.Contains("Transfer")
                || n.Contains("Autocomplete") || n.Contains("Mention") || n.Contains("Form") || n.Contains("DateTimePicker"))
                return "表单组件";

            // 数据展示
            if (n.Contains("Table") || n.Contains("Tree") || n.Contains("Card") || n.Contains("Avatar")
                || n.Contains("Calendar") || n.Contains("Carousel") || n.Contains("Collapse") || n.Contains("Empty")
                || n.Contains("Skeleton") || n.Contains("Splitter") || n.Contains("Steps") || n.Contains("Descriptions")
                || n.Contains("Statistic") || n.Contains("Timeline") || n.Contains("Image") || n.Contains("List"))
                return "数据展示组件";

            // 基础
            return "基础组件";
        }

        /// <summary>
        /// 为给定组件类型生成安全的默认参数字典（仅填充可安全推导的基础类型，
        /// 跳过 EventCallback / RenderFragment / object / 集合 / 泛型等复杂参数）。
        /// 返回 null 表示该组件无法以自动方式安全实例化（通常是泛型或强依赖父子关系）。
        /// </summary>
        public static Dictionary<string, object>? BuildSafeParameters(Type type)
        {
            if (type.IsGenericTypeDefinition) return null;

            var dict = new Dictionary<string, object>();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in props)
            {
                var pa = p.GetCustomAttribute<ParameterAttribute>();
                if (pa == null) continue;
                if (!p.CanWrite) continue;

                // 跳过会触发“首屏远程拉取”的 URL 类参数。
                // 默认会给所有 string 参数填 “示例文本”，若填进 Url 就会变成“请求一个不存在的地址”，
                // 导致 ElTable / ElTree / ElTableV2 / ElTreeV2 在孤立渲染时去拉取假地址并抛 JSON/网络异常。
                // 这类组件“无数据时应优雅渲染空状态”，而不是发请求；真实用法由调用方显式传 Url。
                if (p.PropertyType == typeof(string)
                    && p.Name.Equals("Url", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var v = SafeDefault(p.PropertyType);
                if (v.Set) dict[p.Name] = v.Value!;
            }
            return dict;
        }

        private static (bool Set, object? Value) SafeDefault(Type type)
        {
            // 跳过无法安全推断的复杂参数
            if (type == typeof(object)) return (false, null);
            if (typeof(System.Delegate).IsAssignableFrom(type)) return (false, null);
            if (typeof(Microsoft.AspNetCore.Components.RenderFragment).IsAssignableFrom(type)) return (false, null);
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>)) return (false, null);
            if (typeof(IEnumerable<object>).IsAssignableFrom(type) && type != typeof(string)) return (false, null);
            if (type.IsArray) return (false, null);

            var nv = Nullable.GetUnderlyingType(type) ?? type;

            if (nv == typeof(string)) return (true, "示例文本");
            if (nv == typeof(bool)) return (true, false);
            // 注意：必须返回与属性类型完全匹配的装箱值，否则 Blazor 属性赋值会抛 InvalidCastException
            if (nv == typeof(int)) return (true, 1);
            if (nv == typeof(short)) return (true, (short)1);
            if (nv == typeof(long)) return (true, 1L);
            if (nv == typeof(byte)) return (true, (byte)1);
            if (nv == typeof(sbyte)) return (true, (sbyte)1);
            if (nv == typeof(uint)) return (true, 1u);
            if (nv == typeof(ushort)) return (true, (ushort)1);
            if (nv == typeof(ulong)) return (true, 1ul);
            if (nv == typeof(float)) return (true, 1f);
            if (nv == typeof(double)) return (true, 1.0);
            if (nv == typeof(decimal)) return (true, 1.0m);
            if (nv.IsEnum)
            {
                var vals = Enum.GetValues(nv);
                if (vals.Length > 0) return (true, vals.GetValue(0)!);
            }
            return (false, null);
        }
    }
}
