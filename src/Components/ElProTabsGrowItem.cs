namespace ElementPlus
{
    /// <summary>
    /// <see cref="ElProTabsGrow"/> 的单个页签项。
    /// 纯数据模型：组件只负责渲染与抛事件，标签的增删/持久化由宿主决定。
    /// </summary>
    public class ElProTabsGrowItem
    {
        /// <summary>
        /// 页签唯一标识（必填）。既作为去重键，也作为 <see cref="ElProTabsGrow.ActivePath"/> 的比较值。
        /// 在 admin 布局里通常就是路由路径（如 <c>/table</c>）。
        /// </summary>
        public string Path { get; set; }

        /// <summary>页签显示文字。</summary>
        public string Title { get; set; }

        /// <summary>
        /// 图标 CSS 类名，如 <c>el-icon-s-home</c>（可省略 <c>el-icon-</c> 前缀）。
        /// 留空时回落到圆点（<see cref="ElProTabsGrow.ShowDot"/>）。
        /// </summary>
        public string Icon { get; set; }

        /// <summary>本项是否可关闭，默认 <c>true</c>。首页/固定页设为 <c>false</c>（不渲染关闭按钮）。</summary>
        public bool Closable { get; set; } = true;

        /// <summary>
        /// 本项是否显示标签上的刷新图标，默认 <c>true</c>；总开关是 <see cref="ElProTabsGrow.ShowRefresh"/>，
        /// 两者都开才渲染。与 <see cref="Closable"/> 相互独立：固定页（不可关闭）照样可以刷新。
        /// </summary>
        public bool Refreshable { get; set; } = true;

        /// <summary>是否禁用（不可点击、不可关闭）。</summary>
        public bool Disabled { get; set; }

        /// <summary>鼠标悬停提示（title）；留空则用 <see cref="Title"/>。</summary>
        public string Tooltip { get; set; }

        /// <summary>本项附加 CSS 类。</summary>
        public string Cls { get; set; }

        /// <summary>本项附加内联样式。</summary>
        public string Style { get; set; }
    }
}
