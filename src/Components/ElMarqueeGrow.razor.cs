using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ElementPlus
{
    /// <summary>
    /// 滚动字幕（自渲染版，零 JS、零第三方依赖，按 Element Plus 观感渲染）。
    /// <para>
    /// 能力参照 BootstrapBlazor 的 <c>Marquee</c>（Text / Color / BackgroundColor / FontSize / Duration / Direction），
    /// 但实现与渲染均为自研：不使用 BootstrapBlazor 的任何代码或样式，动画由纯 CSS keyframes + 本组件的内联样式驱动。
    /// </para>
    /// <para>
    /// 相比参考实现的三点增强：① 支持任意 <see cref="ChildContent"/>（不只是纯文本）；
    /// ② 提供 <see cref="Round"/> / <see cref="RoundRadius"/> 圆角控制与 <see cref="Bordered"/> 边框；
    /// ③ 纵向滚动按容器高度直角偏移（参考实现用百分比 padding，纵向会解析成容器宽度而留出空档）。
    /// </para>
    /// <para>
    /// 无缝原理：内容元素带一个等于容器尺寸的 <c>padding</c> 偏移，动画位移恰为该元素自身宽/高的 100%，
    /// 于是内容从一侧边缘进入、从另一侧边缘退出，循环点正好衔接，**无需 JS 测量内容宽度、任意长度内容都不留空档**。
    /// </para>
    /// </summary>
    public partial class ElMarqueeGrow : ElementComponentBase
    {
        /// <summary>边框默认色（对齐 Element Plus <c>--el-border-color</c>）。</summary>
        public const string DefaultBorderColor = "#dcdfe6";

        private static readonly Dictionary<string, object> EmptyAttrs = new Dictionary<string, object>();

        private static readonly Dictionary<string, object> HiddenAttrs = new Dictionary<string, object>
        {
            { "aria-hidden", "true" }
        };

        /// <summary>滚动文本内容；未设置 <see cref="ChildContent"/> 时使用。</summary>
        [Parameter]
        public string Text { get; set; }

        /// <summary>自定义滚动内容；优先级高于 <see cref="Text"/>。</summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>滚动方向。默认 <see cref="ElMarqueeDirection.RightToLeft"/>。</summary>
        [Parameter]
        public ElMarqueeDirection Direction { get; set; } = ElMarqueeDirection.RightToLeft;

        /// <summary>滚动一个完整循环的时长（秒），值越小越快。默认 14。</summary>
        [Parameter]
        public int Duration { get; set; } = 14;

        /// <summary>字号（像素）。默认 14。</summary>
        [Parameter]
        public int FontSize { get; set; } = 14;

        /// <summary>文本颜色。默认 #303133（Element Plus <c>--el-text-color-regular</c>）。</summary>
        [Parameter]
        public string Color { get; set; } = "#303133";

        /// <summary>背景颜色。默认 #ffffff。</summary>
        [Parameter]
        public string BackgroundColor { get; set; } = "#ffffff";

        /// <summary>容器宽度。默认 100%。</summary>
        [Parameter]
        public string Width { get; set; } = "100%";

        /// <summary>容器高度。默认 40px；纵向滚动时建议使用 px 值（决定初始偏移量）。</summary>
        [Parameter]
        public string Height { get; set; } = "40px";

        /// <summary>圆角预设。默认 <see cref="ElMarqueeRound.None"/>（直角）。</summary>
        [Parameter]
        public ElMarqueeRound Round { get; set; } = ElMarqueeRound.None;

        /// <summary>自定义圆角像素值；设置后覆盖 <see cref="Round"/>。</summary>
        [Parameter]
        public int? RoundRadius { get; set; }

        /// <summary>是否显示边框。默认 false。</summary>
        [Parameter]
        public bool Bordered { get; set; }

        /// <summary>边框颜色；为空时使用 <see cref="DefaultBorderColor"/>。</summary>
        [Parameter]
        public string BorderColor { get; set; }

        /// <summary>边框宽度（像素）。默认 1。</summary>
        [Parameter]
        public int BorderWidth { get; set; } = 1;

        /// <summary>
        /// 内容在一次滚动循环内的重复次数。默认 1（单份内容即无缝）。
        /// 增大可让整条字幕更密集（重复份数越多，单圈位移越大）。
        /// </summary>
        [Parameter]
        public int Repeat { get; set; } = 1;

        /// <summary>相邻两份内容之间的间隔（像素）。默认 32。</summary>
        [Parameter]
        public int Spacing { get; set; } = 32;

        /// <summary>鼠标悬停时是否暂停滚动。默认 true。</summary>
        [Parameter]
        public bool PauseOnHover { get; set; } = true;

        /// <summary>无障碍标签。</summary>
        [Parameter]
        public string AriaLabel { get; set; } = "滚动字幕";

        /// <summary>是否横向滚动（上下两类方向为纵向）。</summary>
        public bool IsHorizontal =>
            Direction == ElMarqueeDirection.RightToLeft || Direction == ElMarqueeDirection.LeftToRight;

        /// <summary>实际渲染的内容份数（至少 1）。</summary>
        public int EffectiveRepeat => Repeat < 1 ? 1 : Repeat;

        /// <summary>解析后的边框颜色。</summary>
        public string EffectiveBorderColor =>
            string.IsNullOrWhiteSpace(BorderColor) ? DefaultBorderColor : BorderColor;

        /// <summary>
        /// 解析后的圆角：<see cref="RoundRadius"/> 优先，其次是 <see cref="Round"/> 预设。
        /// </summary>
        public string EffectiveRadius
        {
            get
            {
                if (RoundRadius.HasValue)
                {
                    return Math.Max(0, RoundRadius.Value) + "px";
                }

                switch (Round)
                {
                    case ElMarqueeRound.Small:
                        return "4px";
                    case ElMarqueeRound.Large:
                        return "8px";
                    case ElMarqueeRound.Pill:
                        return "999px";
                    default:
                        return "0px";
                }
            }
        }

        /// <summary>当前方向对应的 CSS 动画名。</summary>
        public string AnimationName
        {
            get
            {
                switch (Direction)
                {
                    case ElMarqueeDirection.LeftToRight:
                        return "el-marquee-grow-ltr";
                    case ElMarqueeDirection.BottomToTop:
                        return "el-marquee-grow-btt";
                    case ElMarqueeDirection.TopToBottom:
                        return "el-marquee-grow-ttb";
                    default:
                        return "el-marquee-grow-rtl";
                }
            }
        }

        /// <summary>
        /// <see cref="Height"/> 的像素值；非 px 写法（如 <c>100%</c>）时为 null。
        /// </summary>
        public double? HeightInPixels
        {
            get
            {
                var text = (Height ?? string.Empty).Trim();
                if (text.EndsWith("px", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Substring(0, text.Length - 2).Trim();
                }

                return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                    ? value
                    : (double?)null;
            }
        }

        /// <summary>
        /// 内容元素的初始偏移量：横向为容器内容宽度的 <c>100%</c>（百分比 padding 恰好按内容宽度解析）；
        /// 纵向取容器内容高度的像素值——纵向的百分比 padding 会按容器「宽度」解析，故必须用 px，
        /// 且 <c>box-sizing:border-box</c> 下需扣除上下边框，偏移量才与可见区域严格相等。
        /// </summary>
        public string ContentOffset
        {
            get
            {
                if (IsHorizontal || !HeightInPixels.HasValue)
                {
                    return "100%";
                }

                var borderHeight = Bordered ? Math.Max(1, BorderWidth) * 2 : 0;
                var innerHeight = HeightInPixels.Value - borderHeight;
                if (innerHeight < 0)
                {
                    innerHeight = 0;
                }

                return innerHeight.ToString("0.###", CultureInfo.InvariantCulture) + "px";
            }
        }

        /// <summary>实际渲染的内容片段：<see cref="ChildContent"/> 优先，否则渲染 <see cref="Text"/>。</summary>
        protected RenderFragment ContentFragment => ChildContent ?? DefaultTextFragment;

        private RenderFragment DefaultTextFragment => builder =>
        {
            builder.AddContent(0, Text);
        };

        private string WrapperClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-marquee-grow")
            .AddIf(IsHorizontal, "el-marquee-grow--horizontal")
            .AddIf(!IsHorizontal, "el-marquee-grow--vertical")
            .AddIf(Bordered, "is-bordered")
            .Add(Cls)
            .ToString();

        private string WrapperStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add($"width:{Width}")
            .Add($"height:{Height}")
            .Add($"background-color:{BackgroundColor}")
            .Add($"color:{Color}")
            .Add($"font-size:{FontSize}px")
            .Add($"border-radius:{EffectiveRadius}")
            .AddIf(Bordered, $"border:{Math.Max(1, BorderWidth)}px solid {EffectiveBorderColor}")
            .Add(Style)
            .ToString();

        private string ItemClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-marquee-grow__item")
            .AddIf(PauseOnHover, "is-pausable")
            .ToString();

        private string ItemStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add($"animation-name:{AnimationName}")
            .Add($"animation-duration:{Math.Max(0, Duration)}s")
            .AddIf(IsHorizontal, $"padding-left:{ContentOffset}")
            .AddIf(!IsHorizontal, $"padding-top:{ContentOffset}")
            .ToString();

        /// <summary>单份内容的样式：仅在前一份与后一份之间插入 <see cref="Spacing"/> 间隔。</summary>
        /// <param name="index">内容序号。</param>
        /// <returns>内联样式。</returns>
        protected string CopyStyle(int index)
        {
            if (index >= EffectiveRepeat - 1)
            {
                return string.Empty;
            }

            return HtmlPropertyBuilder.CreateCssStyleBuilder()
                .AddIf(IsHorizontal, $"padding-right:{Math.Max(0, Spacing)}px")
                .AddIf(!IsHorizontal, $"padding-bottom:{Math.Max(0, Spacing)}px")
                .ToString();
        }

        /// <summary>为重复内容添加 aria-hidden（首份内容保留给读屏软件）。</summary>
        /// <param name="index">内容序号。</param>
        /// <returns>附加属性。</returns>
        protected IDictionary<string, object> CopyAttributes(int index) =>
            index == 0 ? EmptyAttrs : HiddenAttrs;
    }
}
