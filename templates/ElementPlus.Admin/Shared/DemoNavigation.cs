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
    /// <list type="bullet">
    ///   <item><see cref="Fixed"/>：左侧菜单实际显示的条目（模板默认仅 4 个管理页）。</item>
    ///   <item><see cref="Registered"/>：已登记但不在左侧菜单显示的页（首页 / 登录 / 控制台），仍可被直接访问并被页签与面包屑引用。</item>
    /// </list>
    /// 使用者新增业务页时，只需在 <see cref="Fixed"/> 里加一条 <see cref="DemoNavEntry"/> 即可。
    /// </summary>
    public static class DemoNavigation
    {
        /// <summary>侧栏实际显示的菜单项（模板默认：用户 / 角色 / 权限 / 菜单管理）。</summary>
        public static IReadOnlyList<DemoNavEntry> Fixed { get; } = new[]
        {
            new DemoNavEntry { Path = "/admin/users", Title = "用户管理", Icon = "el-icon-user", Closable = true },
            new DemoNavEntry { Path = "/admin/roles", Title = "角色管理", Icon = "el-icon-s-custom", Closable = true },
            new DemoNavEntry { Path = "/admin/permissions", Title = "权限管理", Icon = "el-icon-key", Closable = true },
            new DemoNavEntry { Path = "/admin/menus", Title = "菜单管理", Icon = "el-icon-menu", Closable = true }
        };

        /// <summary>
        /// 已登记但不在左侧菜单显示的页：首页（默认落地页 / 固定钉页签）、登录页（独立布局）、控制台。
        /// 保留在此供 <see cref="Find"/> / 页签标题 / 面包屑引用，避免直接访问时标题退化成路径。
        /// </summary>
        public static IReadOnlyList<DemoNavEntry> Registered { get; } = new[]
        {
            new DemoNavEntry { Path = "/", Title = "首页", Icon = "el-icon-s-home", Closable = false },
            new DemoNavEntry { Path = "/login", Title = "登录页 Login（独立布局）", Icon = "el-icon-lock" },
            new DemoNavEntry { Path = "/dashboard", Title = "控制台 Dashboard", Icon = "el-icon-s-data" }
        };

        /// <summary>侧栏分组（当前模板仅 4 个一级管理页、无子分组；保留结构便于后续扩展多级菜单）。</summary>
        public static IReadOnlyList<DemoNavSection> Sections { get; } = Array.Empty<DemoNavSection>();

        /// <summary>全部已登记条目（Fixed + Registered），用于查找、页签与面包屑。</summary>
        public static IReadOnlyList<DemoNavEntry> All { get; } =
            Fixed.Concat(Registered).ToArray();

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
