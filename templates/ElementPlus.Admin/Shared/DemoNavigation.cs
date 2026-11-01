using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementPlus.Blazor.Demo.Shared
{
    /// <summary>侧栏导航中的一条可点击项（也是可打开的一个页签）。</summary>
    public sealed class DemoNavEntry
    {
        public string Path { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        /// <summary>图标 CSS 类名（element-icons 字形，已确认字体里真实存在）。</summary>
        public string Icon { get; init; } = "el-icon-document";

        /// <summary>所属分组标题，用于面包屑第二级。</summary>
        public string Section { get; init; } = string.Empty;

        /// <summary>是否可关闭；首页固定不可关。</summary>
        public bool Closable { get; init; } = true;
    }

    /// <summary>侧栏里的一个分组（对应 <c>ElSubMenu</c>）。</summary>
    public sealed class DemoNavSection
    {
        public string Key { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string Icon { get; init; } = "el-icon-folder";

        public IReadOnlyList<DemoNavEntry> Entries { get; init; } = Array.Empty<DemoNavEntry>();
    }

    /// <summary>
    /// 中台模板的导航真源：侧栏菜单、页签标题、面包屑都从这里取，避免漂移。
    /// 使用者新增业务页时，只需在 <see cref="TemplateSection"/> 里加一条 <see cref="DemoNavEntry"/>。
    /// </summary>
    public static class DemoNavigation
    {
        /// <summary>侧栏顶部的固定入口（首页固定为不可关闭页签）。</summary>
        public static IReadOnlyList<DemoNavEntry> Fixed { get; } = new[]
        {
            new DemoNavEntry { Path = "/", Title = "首页", Icon = "el-icon-s-home", Closable = false }
        };

        /// <summary>「中台模板」分组：登录 / 控制台 / 用户·角色·权限·菜单管理。</summary>
        public static DemoNavSection TemplateSection { get; } = new DemoNavSection
        {
            Key = "template",
            Title = TemplateSectionTitle,
            Icon = "el-icon-menu",
            Entries = new[]
            {
                Template("/login", "登录页 Login（独立布局）", "el-icon-lock"),
                Template("/dashboard", "控制台 Dashboard", "el-icon-s-data"),
                Template("/admin/users", "用户管理", "el-icon-user"),
                Template("/admin/roles", "角色管理", "el-icon-s-custom"),
                Template("/admin/permissions", "权限管理", "el-icon-key"),
                Template("/admin/menus", "菜单管理", "el-icon-menu")
            }
        };

        /// <summary>所有侧栏分组，供 SidebarMenu 遍历渲染。</summary>
        public static IReadOnlyList<DemoNavSection> Sections { get; } = new[]
        {
            TemplateSection
        };

        private const string TemplateSectionTitle = "中台模板";

        /// <summary>「中台模板」分组的条目构造器：Section 固定为 TemplateSectionTitle，面包屑第二级即「中台模板」。</summary>
        private static DemoNavEntry Template(string path, string title, string icon) =>
            new() { Path = path, Title = title, Icon = icon, Section = TemplateSectionTitle };

        /// <summary>全部条目（顺序即侧栏顺序），用于「更多」与查找。</summary>
        public static IReadOnlyList<DemoNavEntry> All { get; } =
            Fixed.Concat(Sections.SelectMany(s => s.Entries)).ToArray();

        private static readonly Dictionary<string, DemoNavEntry> Index =
            All.GroupBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
               .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        /// <summary>去掉查询串与结尾斜杠后的规范化路径。</summary>
        public static string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "/";

            var clean = path;
            var query = clean.IndexOf('?');
            if (query >= 0) clean = clean.Substring(0, query);
            var hash = clean.IndexOf('#');
            if (hash >= 0) clean = clean.Substring(0, hash);
            if (!clean.StartsWith('/')) clean = "/" + clean;
            if (clean.Length > 1 && clean.EndsWith('/')) clean = clean.TrimEnd('/');
            return clean.Length == 0 ? "/" : clean;
        }

        /// <summary>按路径取条目；未登记的路径返回 <c>null</c>。</summary>
        public static DemoNavEntry Find(string path)
        {
            var key = Normalize(path);
            return Index.TryGetValue(key, out var entry) ? entry : null;
        }

        /// <summary>按路径取显示标题；未登记的路径回落到路径本身。</summary>
        public static string TitleOf(string path)
        {
            var entry = Find(path);
            return entry != null ? entry.Title : Normalize(path);
        }

        /// <summary>图标查表；未登记返回默认文档图标。</summary>
        public static string IconOf(string path)
        {
            var entry = Find(path);
            return entry == null ? "el-icon-document" : entry.Icon;
        }

        /// <summary>面包屑：首页 → 分组 → 当前页。</summary>
        public static IReadOnlyList<string> Breadcrumb(string path)
        {
            var key = Normalize(path);
            if (key == "/") return new[] { "首页" };

            var entry = Find(key);
            var crumbs = new List<string> { "首页" };

            if (entry != null)
            {
                if (!string.IsNullOrEmpty(entry.Section)) crumbs.Add(entry.Section);
                crumbs.Add(entry.Title);
                return crumbs;
            }

            crumbs.Add(TitleOf(key));
            return crumbs;
        }
    }
}
