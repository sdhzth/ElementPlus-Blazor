using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 通知栏配置项（对齐 Element Plus Notification 的 Options）。
    /// 参考：https://element-plus.org/zh-CN/component/notification
    /// </summary>
    public class NotificationOption
    {
        /// <summary>通知的根元素 id（EP 的 <c>id</c>），缺省不输出。</summary>
        public string Id { get; set; }

        /// <summary>标题。EP 默认空串，为空时不渲染 <c>h2</c>（且正文去掉上间距）。</summary>
        public string Title { get; set; }

        /// <summary>正文内容。</summary>
        public string Message { get; set; }

        /// <summary>正文的自定义内容（RenderFragment，优先级高于 Message）。EP 的 message 支持 VNode/Function。</summary>
        public RenderFragment MessageContent { get; set; }

        /// <summary>
        /// 类型。<see langword="null"/> 表示**无类型**——EP 的 <c>type</c> 默认是空串，
        /// 此时不显示图标、不带类型色（除 primary 外四种类型才渲染图标）。
        /// </summary>
        public MessageType? Type { get; set; }

        /// <summary>自定义图标 class。EP：若设置了 <c>type</c>，则 type 的图标优先；仅自定义图标时用它。</summary>
        public string Icon { get; set; }

        /// <summary>自定义 class。</summary>
        public string CustomClass { get; set; }

        /// <summary>自定义 style（EP 无此项，本库扩展）。</summary>
        public string CustomStyle { get; set; }

        /// <summary>显示时长（ms），0 = 不自动关闭。EP 默认 4500。</summary>
        public int Duration { get; set; } = 4500;

        /// <summary>弹出位置。EP 默认 <c>top-right</c>。</summary>
        public NotificationPosition Position { get; set; } = NotificationPosition.TopRight;

        /// <summary>
        /// 相对屏幕顶/底部的偏移量。EP 默认 0。
        /// 注意 EP 原文：同一时刻所有实例应当具有相同的偏移量（本库按「同位置分组的第一条」取值，见 <c>RepositionNotifications</c>）。
        /// </summary>
        public int Offset { get; set; }

        /// <summary>是否显示关闭按钮。EP 默认 true。</summary>
        public bool ShowClose { get; set; } = true;

        /// <summary>点击通知本身即关闭（EP 无此项，本库扩展；与 OnClick 可同时生效）。</summary>
        public bool CloseOnClick { get; set; }

        /// <summary>把 Message 当作 HTML 渲染（XSS 风险，仅用于可信内容）。</summary>
        public bool DangerouslyUseHTMLString { get; set; }

        /// <summary>分组：同类型同标题同正文合并为一条并累加次数（EP 无此项，本库扩展）。</summary>
        public bool Grouping { get; set; }

        /// <summary>分组合并的次数（内部维护，1 = 未重复）。</summary>
        public int RepeatCount { get; set; } = 1;

        /// <summary>悬停于通知上时是否暂停计时器（EP 2.14.4，默认 true）。</summary>
        public bool PauseOnHover { get; set; } = true;

        /// <summary>
        /// 显示进度条指示自动关闭倒计时（EP 2.14.4，默认 false）。
        /// 仅在 <see cref="Duration"/> &gt; 0 时生效（EP：<c>!!progress &amp;&amp; duration &gt; 0</c>）。
        /// </summary>
        public bool Progress { get; set; }

        /// <summary>
        /// 进度条颜色（EP 的 <c>progress.color</c>，会覆盖基于 type 的状态色）。
        /// 缺省时按类型取色：success 绿 / warning 橙 / error 红 / 其余主色蓝。
        /// </summary>
        public string ProgressColor { get; set; }

        /// <summary>自定义关闭图标 class（EP 2.9.8 的 <c>closeIcon</c>，默认 <c>el-icon-close</c>）。</summary>
        public string CloseIcon { get; set; } = "el-icon-close";

        /// <summary>
        /// 初始 z-index（EP 的 <c>zIndex</c>，默认 0）。0 = 由宿主按顺序自动分配。
        /// </summary>
        public int ZIndex { get; set; }

        /// <summary>关闭时的回调。</summary>
        public Func<Task> OnClose { get; set; }

        /// <summary>关闭时的同步回调。</summary>
        public Action OnCloseSync { get; set; }

        /// <summary>点击通知时的回调（EP 的 <c>onClick</c>）。</summary>
        public Func<Task> OnClick { get; set; }

        /// <summary>点击通知时的同步回调。</summary>
        public Action OnClickSync { get; set; }

        internal bool IsNew { get; set; }

        internal bool Closing { get; set; }

        /// <summary>宿主最终使用的 z-index（<see cref="ZIndex"/> 非 0 时用它，否则自动分配）。</summary>
        internal int ResolvedZIndex { get; set; }

        internal int Top { get; set; }

        internal int Bottom { get; set; }

        /// <summary>已累计的计时毫秒数（暂停时把本段时长累加进来）。</summary>
        internal double ElapsedMs { get; set; }

        /// <summary>本段计时的起点（Environment.TickCount64），0 = 未在计时。</summary>
        internal long StartedAt { get; set; }

        /// <summary>是否处于暂停（悬停）状态。</summary>
        internal bool Paused { get; set; }

        /// <summary>进度条剩余百分比（100 → 0）。</summary>
        internal double ProgressPercent { get; set; } = 100;

        /// <summary>
        /// 是否显示进度条（对齐 EP：<c>!!progress &amp;&amp; duration &gt; 0</c>）。
        /// </summary>
        internal bool ShowProgress => Progress && Duration > 0;

        /// <summary>本条是否已经到点（供宿主判断，不含暂停语义）。</summary>
        internal bool IsExpired(int duration) => duration > 0 && ElapsedMs >= duration;
    }
}
