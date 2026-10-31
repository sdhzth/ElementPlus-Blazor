using System;
using System.Globalization;

namespace ElementPlus
{
    /// <summary>
    /// JS 函数 <c>elementViewportRect</c>（<c>wwwroot/js/dom.js</c>）的返回值，仅本库内部使用。
    /// 矩形是被测元素的视口坐标，<see cref="Vw"/> / <see cref="Vh"/> 是视口尺寸，
    /// 翻面与钳制要同时用到两者。
    /// </summary>
    internal sealed class ElementViewportRect
    {
        public double Left { get; set; }

        public double Top { get; set; }

        public double Right { get; set; }

        public double Bottom { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public double Vw { get; set; }

        public double Vh { get; set; }
    }

    /// <summary>
    /// 表头漏斗面板（<see cref="ElTableFilterPanel"/>）的定位测算（不是组件）。
    /// </summary>
    /// <remarks>
    /// 为什么是 <c>position:fixed</c> 而不是 EP 那样的 popper 挂载：面板是触发 span 的子孙，
    /// absolute 的裁剪来自「最近的非 visible overflow 祖先」那条链
    /// （<c>.cell(overflow:hidden) → th → .el-table__header-wrapper(hidden) → .el-table(hidden)</c>），
    /// 于是面板刚冒头就被整条裁掉，看着就是「点了没反应」（浏览器实测：cell 只有 23px 高）。
    /// fixed 的包含块是视口，只要祖先没有 transform/filter/contain 就不受 overflow 裁剪。
    /// EP 不会遇到这个问题，它把面板挂到 body 下再用 popper.js 跟随；本仓按列各挂各的，
    /// 只能在打开时用一次 JS 往返量出触发器的视口矩形，把坐标自己钉上去。
    /// </remarks>
    internal static class TableFilterPopper
    {
        /// <summary>JS 函数名（bUnit 打桩与浏览器探针共用同一个口径）。</summary>
        internal const string MeasureFunction = "elementViewportRect";

        /// <summary>面板与漏斗之间的间距，与 <see cref="ElDropDownTableGrow"/> 的弹层同口径。</summary>
        internal const double Gap = 4;

        /// <summary>面板距视口左右/上下边缘的最小留白。</summary>
        internal const double EdgeMargin = 8;

        /// <summary>下方放不下这么高、且上方更宽裕时，改为向上展开（同 EP 下拉的判据口径）。</summary>
        internal const double MinRoomBelow = 140;

        /// <summary>选项列表面板的保底宽度：标签是 nowrap 的，靠内容撑不开时也不能细成一条。</summary>
        internal const double OptionPanelWidth = 150;

        /// <summary>条件式面板要并列放下「运算符下拉 + 值输入框」，宽度由内容撑不稳，直接给定值。</summary>
        internal const double AdvancedPanelWidth = 320;

        /// <summary>
        /// 按触发器的视口矩形算出面板的内联样式。量不到矩形（元素还没渲染）时返回 <c>null</c>，
        /// 由 CSS 的默认定位兜底，不让面板凭空消失。
        /// </summary>
        public static string BuildPanelStyle(ElementViewportRect rect, bool advanced)
        {
            if (rect == null || rect.Vw <= 0 || rect.Vh <= 0)
            {
                return null;
            }

            var panelWidth = advanced ? AdvancedPanelWidth : OptionPanelWidth;
            var left = rect.Left;
            if (left + panelWidth > rect.Vw - EdgeMargin)
            {
                left = Math.Max(EdgeMargin, rect.Vw - EdgeMargin - panelWidth);
            }

            var below = rect.Vh - rect.Bottom - Gap - EdgeMargin;
            var above = rect.Top - Gap - EdgeMargin;
            var goUp = below < MinRoomBelow && above > below;

            // max-height 只给选项列表：条件面板两条规则是定高内容，而它的运算符下拉要能在面板外展开，
            // 给面板加 max-height + overflow 会把下拉整条裁掉。
            var width = advanced ? $"width:{Px(panelWidth)};" : string.Empty;
            var maxHeight = advanced ? string.Empty : $"max-height:{Px(Math.Max(0, goUp ? above : below))};";
            var vertical = goUp
                ? $"bottom:{Px(rect.Vh - rect.Top + Gap)};top:auto;"
                : $"top:{Px(rect.Bottom + Gap)};bottom:auto;";
            return $"position:fixed;left:{Px(left)};{vertical}right:auto;{width}{maxHeight}";
        }

        private static string Px(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture) + "px";
        }
    }
}
