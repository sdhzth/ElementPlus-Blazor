using System;

namespace ElementPlus
{
    /// <summary>
    /// 日历单元格上下文，作为 <see cref="ElCalendar"/> 的 <c>DateCell</c> 插槽参数。
    /// 字段对齐 Element Plus 官方 <c>date-cell</c> scoped-slot 的 <c>data</c>：
    /// <c>type</c> / <c>isSelected</c> / <c>day</c> / <c>date</c>。
    /// </summary>
    public class CalendarDateContext
    {
        /// <summary>单元格对应的真实日期。</summary>
        public DateTime Date { get; set; }

        /// <summary>是否为当前显示月份内的日期（旧别名，兼容保留）。</summary>
        public bool IsCurrentMonth { get; set; }

        /// <summary>是否被选中（等于 model-value）。</summary>
        public bool IsSelected { get; set; }

        /// <summary>是否为今天。</summary>
        public bool IsToday { get; set; }

        /// <summary>
        /// 所属月份，取值对齐 EP：<c>prev-month</c> / <c>current-month</c> / <c>next-month</c>。
        /// 同时用于单元格 CSS 类（取其 <c>-month</c> 前缀即 EP 的 <c>prev</c>/<c>current</c>/<c>next</c>）。
        /// </summary>
        public string Type { get; set; } = "current-month";

        /// <summary>格式化的日期字符串，对齐 EP 的 <c>day</c>：<c>yyyy-MM-dd</c>。</summary>
        public string Day { get; set; } = string.Empty;

        /// <summary>是否落在 range 区间内（仅 range 模式有意义）。</summary>
        public bool IsInRange { get; set; }

        /// <summary>是否为 range 区间起点。</summary>
        public bool IsRangeStart { get; set; }

        /// <summary>是否为 range 区间终点。</summary>
        public bool IsRangeEnd { get; set; }
    }
}
