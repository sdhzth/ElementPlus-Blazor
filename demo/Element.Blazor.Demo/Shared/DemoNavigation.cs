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

    /// <summary>侧栏里的一个分组（对应 <c>ElSubMenu</c> 或 <c>ElMenuItemGroup</c>）。</summary>
    public sealed class DemoNavSection
    {
        public string Key { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string Icon { get; init; } = "el-icon-folder";

        public IReadOnlyList<DemoNavEntry> Entries { get; init; } = Array.Empty<DemoNavEntry>();
    }

    /// <summary>
    /// 演示站的导航真源：侧栏菜单、页签标题、面包屑都从这里取，
    /// 避免「菜单改名了但页签还显示旧名」这类漂移。
    /// </summary>
    public static class DemoNavigation
    {
        /// <summary>侧栏顶部的固定入口（首页固定为不可关闭页签）。</summary>
        public static IReadOnlyList<DemoNavEntry> Fixed { get; } = new[]
        {
            new DemoNavEntry { Path = "/", Title = "首页", Icon = "el-icon-s-home", Closable = false },
            new DemoNavEntry { Path = "/all", Title = "全部组件", Icon = "el-icon-s-grid", Section = "组件中心" },
            new DemoNavEntry { Path = "/login", Title = "登录演示", Icon = "el-icon-lock", Section = "组件中心" }
        };

        /// <summary>「分类演示（Mock 数据）」分组。</summary>
        public static DemoNavSection MockSection { get; } = new DemoNavSection
        {
            Key = "mock",
            Title = MockSectionTitle,
            Icon = "el-icon-s-data",
            Entries = new[]
            {
                Mock("/basic", "基础组件", "el-icon-s-operation"),
                Mock("/form", "表单组件", "el-icon-edit-outline"),
                Mock("/form2", "表单进阶", "el-icon-document-checked"),
                Mock("/data", "数据展示", "el-icon-pie-chart"),
                Mock("/table", "表格进阶", "el-icon-s-grid"),
                Mock("/layout", "布局与容器", "el-icon-crop"),
                Mock("/misc", "交互与虚拟化", "el-icon-mouse"),
                Mock("/feedback", "反馈组件", "el-icon-chat-line-round"),
                Mock("/nav", "导航组件", "el-icon-guide"),
                Mock("/watermark", "水印整页", "el-icon-picture-outline"),
                Mock("/affix", "固钉 Affix", "el-icon-aim"),
                Mock("/alert", "警告提示 Alert", "el-icon-warning"),
                Mock("/anchor", "锚点 Anchor", "el-icon-connection"),
                Mock("/autocomplete", "自动补全 Autocomplete", "el-icon-edit"),
                Mock("/avatar", "头像 Avatar", "el-icon-user"),
                Mock("/backtop", "回到顶部 Backtop", "el-icon-top"),
                Mock("/badge", "徽标 Badge", "el-icon-bell"),
                Mock("/border", "边框 / Border", "el-icon-minus"),
                Mock("/breadcrumb", "面包屑 Breadcrumb", "el-icon-guide"),
                Mock("/button", "按钮 / Button", "el-icon-box"),
                Mock("/calendar", "日历 Calendar", "el-icon-date"),
                Mock("/card", "卡片 Card", "el-icon-postcard"),
                Mock("/carousel", "走马灯 Carousel", "el-icon-picture"),
                Mock("/cascader", "级联选择器 Cascader", "el-icon-menu"),
                Mock("/checkbox", "复选框 Checkbox", "el-icon-circle-check"),
                Mock("/collapse", "折叠面板 Collapse", "el-icon-caret-top"),
                Mock("/colorpicker", "颜色选择器 / ColorPicker", "el-icon-picture"),
                Mock("/colorpickerpanel", "颜色面板 ColorPickerPanel", "el-icon-picture"),
                Mock("/config-provider", "全局配置 / ConfigProvider", "el-icon-setting"),
                Mock("/container", "布局容器 / Container", "el-icon-s-fold"),
                Mock("/datepicker", "日期选择器 DatePicker", "el-icon-date"),
                Mock("/datepickerpanel", "日期面板 DatePickerPanel", "el-icon-date"),
                Mock("/datetimepicker", "日期时间选择器 DateTimePicker", "el-icon-time"),
                Mock("/descriptions", "描述列表 Descriptions", "el-icon-tickets"),
                Mock("/dialog", "对话框 Dialog", "el-icon-document"),
                Mock("/divider", "分割线 Divider", "el-icon-minus"),
                Mock("/drawer", "抽屉 Drawer", "el-icon-right"),
                Mock("/dropdown", "下拉菜单 Dropdown", "el-icon-arrow-down"),
                Mock("/empty", "空状态 Empty", "el-icon-box"),
                Mock("/grow", "自研 / Grow", "el-icon-magic-stick"),
                Mock("/icon", "图标 / Icon", "el-icon-star-on"),
                Mock("/image", "图片 Image", "el-icon-picture"),
                Mock("/infinite-scroll", "无限滚动 InfiniteScroll", "el-icon-loading"),
                Mock("/input", "输入框 Input", "el-icon-edit-outline"),
                Mock("/input-number", "数字输入框 InputNumber", "el-icon-sort"),
                Mock("/input-otp", "OTP 输入 InputOtp", "el-icon-key"),
                Mock("/input-tag", "标签输入 InputTag", "el-icon-collection-tag"),
                Mock("/light", "指示灯 Light", "el-icon-star-on"),
                Mock("/link", "链接 / Link", "el-icon-share"),
                Mock("/loading", "加载 Loading", "el-icon-loading"),
                Mock("/mention", "提及 Mention", "el-icon-chat-dot-round"),
                Mock("/menu", "菜单 Menu", "el-icon-menu"),
                Mock("/message", "全局提示 Message", "el-icon-message"),
                Mock("/message-box", "弹框 MessageBox", "el-icon-question"),
                Mock("/notification", "通知 Notification", "el-icon-bell"),
                Mock("/page-header", "页头 PageHeader", "el-icon-document"),
                Mock("/pagination", "分页 Pagination", "el-icon-more"),
                Mock("/popconfirm", "气泡确认 Popconfirm", "el-icon-question"),
                Mock("/popover", "弹出框 Popover", "el-icon-more"),
                Mock("/progress", "进度条 Progress", "el-icon-loading"),
                Mock("/radio", "单选框 Radio", "el-icon-circle-check"),
                Mock("/rate", "评分 Rate", "el-icon-star-on"),
                Mock("/result", "结果 Result", "el-icon-success"),
                Mock("/grid", "栅格 / Row·Col", "el-icon-crop"),
                Mock("/scrollbar", "滚动条 / Scrollbar", "el-icon-more"),
                Mock("/segmented", "分段控制器 Segmented", "el-icon-s-grid"),
                Mock("/select", "选择器 Select", "el-icon-arrow-down"),
                Mock("/select-v2", "虚拟列表 SelectV2", "el-icon-arrow-down"),
                Mock("/slider", "滑块 Slider", "el-icon-minus"),
                Mock("/space", "间距 / Space", "el-icon-s-grid"),
                Mock("/splitter", "分割面板 / Splitter", "el-icon-menu"),
                Mock("/steps", "步骤条 Steps", "el-icon-sort"),
                Mock("/statistic", "统计数值 Statistic", "el-icon-data-line"),
                Mock("/switch", "开关 Switch", "el-icon-switch"),
                Mock("/tablev2", "虚拟化表格 TableV2", "el-icon-s-grid"),
                Mock("/tabs", "标签页 Tabs", "el-icon-folder-opened"),
                Mock("/tag", "标签 Tag", "el-icon-price-tag"),
                Mock("/text", "文本 / Text", "el-icon-document"),
                Mock("/timeline", "时间线 Timeline", "el-icon-time"),
                Mock("/timepicker", "时间选择器 TimePicker", "el-icon-time"),
                Mock("/timeselect", "时间选择 TimeSelect", "el-icon-time"),
                Mock("/tooltip", "文字提示 Tooltip", "el-icon-chat-dot-round"),
                Mock("/tour", "引导 Tour", "el-icon-guide"),
                Mock("/transfer", "穿梭框 Transfer", "el-icon-d-caret"),
                Mock("/treeselect", "树形选择 TreeSelect", "el-icon-share"),
                Mock("/typography", "排版 / Typography", "el-icon-tickets"),
                Mock("/upload", "上传 Upload", "el-icon-upload"),
                Mock("/video-player-grow", "视频播放器 / VideoPlayer", "el-icon-video-camera"),
                Mock("/x", "AI / X 组件", "el-icon-cpu")
            }
        };

        private const string MockSectionTitle = "分类演示（Mock 数据）";

        private static DemoNavEntry Mock(string path, string title, string icon) =>
            new() { Path = path, Title = title, Icon = icon, Section = MockSectionTitle };

        /// <summary>全部条目（顺序即侧栏顺序），用于「更多」与查找。</summary>
        public static IReadOnlyList<DemoNavEntry> All { get; } =
            Fixed.Concat(MockSection.Entries).ToArray();

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
