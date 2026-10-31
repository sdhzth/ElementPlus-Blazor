using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    public partial class ElCard
    {
        [Parameter]
        public string BodyStyle { get; set; }

        [Parameter]
        public RenderFragment ChildContent
        {
            get => Body;
            set => Body = value;
        }

        [Parameter]
        public RenderFragment Footer { get; set; }

        /// <summary>
        /// EP <c>footer</c> 属性（2.4.3）：当未提供 <see cref="Footer"/> 插槽时，作为页脚纯文本渲染。
        /// 若同时提供两者，插槽优先级更高（与 EP 行为一致）。
        /// </summary>
        [Parameter]
        public string FooterText { get; set; }

        /// <summary>
        /// Footer 上的 Class。
        /// </summary>
        [Parameter]
        public string FooterCls { get; set; }

        /// <summary>
        /// Footer 上的自定义样式（内联，优先级高于 <see cref="FooterHeight"/> / <see cref="FooterBackgroundColor"/>）。
        /// </summary>
        [Parameter]
        public string FooterStyle { get; set; }

        /// <summary>
        /// 是否显示 Header 底部的分隔线。默认 <c>true</c>。设为 <c>false</c> 等价于分隔线粗细为 0。
        /// </summary>
        [Parameter]
        public bool ShowHeaderDivider { get; set; } = true;

        /// <summary>
        /// 是否显示 Footer 顶部的分隔线。默认 <c>true</c>。
        /// </summary>
        [Parameter]
        public bool ShowFooterDivider { get; set; } = true;

        /// <summary>
        /// Header 底部分隔线的颜色（任意 CSS 颜色）；不设置时回落 <c>--el-border-color-lighter</c>（#ebeef5）。
        /// </summary>
        [Parameter]
        public string HeaderDividerColor { get; set; }

        /// <summary>
        /// Footer 顶部分隔线的颜色；不设置时回落 <c>--el-border-color-lighter</c>（#ebeef5）。
        /// </summary>
        [Parameter]
        public string FooterDividerColor { get; set; }

        /// <summary>
        /// Header 底部分隔线的粗细（像素）。不设置时为 <c>1</c>，设为 <c>0</c> 等同隐藏。
        /// </summary>
        [Parameter]
        public int? HeaderDividerWidth { get; set; }

        /// <summary>
        /// Footer 顶部分隔线的粗细（像素）。不设置时为 <c>1</c>，设为 <c>0</c> 等同隐藏。
        /// </summary>
        [Parameter]
        public int? FooterDividerWidth { get; set; }

        /// <summary>
        /// Header 最小高度（CSS 长度，如 <c>"60px"</c> / <c>"4rem"</c>；纯数字自动按 px 处理）。不设置则不限制。
        /// </summary>
        [Parameter]
        public string HeaderHeight { get; set; }

        /// <summary>
        /// Body 最小高度（CSS 长度；纯数字自动按 px 处理）。不设置则不限制。
        /// </summary>
        [Parameter]
        public string BodyHeight { get; set; }

        /// <summary>
        /// Footer 最小高度（CSS 长度；纯数字自动按 px 处理）。不设置则不限制。
        /// </summary>
        [Parameter]
        public string FooterHeight { get; set; }

        /// <summary>
        /// Header 背景色（任意 CSS 颜色）。不设置则透明（继承卡片底色）。
        /// </summary>
        [Parameter]
        public string HeaderBackgroundColor { get; set; }

        /// <summary>
        /// Body 背景色。不设置则透明（继承卡片底色）。
        /// </summary>
        [Parameter]
        public string BodyBackgroundColor { get; set; }

        /// <summary>
        /// Footer 背景色。不设置则透明（继承卡片底色）。
        /// </summary>
        [Parameter]
        public string FooterBackgroundColor { get; set; }

        /// <summary>
        /// 根元素内联样式：把「分隔线 / 最小高度 / 背景色」逐实例外观参数以 CSS 自定义变量下发到卡片根，
        /// 由 <c>index.css</c> 的 ElCard 段消费（样式真源始终在 index.css，C# 只给变量值）。
        /// 未设置任何新参数时不产生额外内联变量，保持零侵入。
        /// </summary>
        private string CardStyle
        {
            get
            {
                var headerHeight = ToCssLength(HeaderHeight);
                var bodyHeight = ToCssLength(BodyHeight);
                var footerHeight = ToCssLength(FooterHeight);
                // 先算出最终线粗，再做条件下发：AddIf 的插值参数会先求值，不能把 ?.Value 写在参数里。
                var headerDividerWidth = ShowHeaderDivider ? HeaderDividerWidth ?? 1 : 0;
                var footerDividerWidth = ShowFooterDivider ? FooterDividerWidth ?? 1 : 0;
                return HtmlPropertyBuilder.CreateCssStyleBuilder()
                    .AddIf(!ShowHeaderDivider || HeaderDividerWidth.HasValue, $"--el-card-header-divider-width:{headerDividerWidth}px")
                    .AddIf(!string.IsNullOrWhiteSpace(HeaderDividerColor), $"--el-card-header-divider-color:{HeaderDividerColor}")
                    .AddIf(headerHeight != null, $"--el-card-header-height:{headerHeight}")
                    .AddIf(!string.IsNullOrWhiteSpace(HeaderBackgroundColor), $"--el-card-header-bg:{HeaderBackgroundColor}")
                    .AddIf(!ShowFooterDivider || FooterDividerWidth.HasValue, $"--el-card-footer-divider-width:{footerDividerWidth}px")
                    .AddIf(!string.IsNullOrWhiteSpace(FooterDividerColor), $"--el-card-footer-divider-color:{FooterDividerColor}")
                    .AddIf(footerHeight != null, $"--el-card-footer-height:{footerHeight}")
                    .AddIf(!string.IsNullOrWhiteSpace(FooterBackgroundColor), $"--el-card-footer-bg:{FooterBackgroundColor}")
                    .AddIf(bodyHeight != null, $"--el-card-body-height:{bodyHeight}")
                    .AddIf(!string.IsNullOrWhiteSpace(BodyBackgroundColor), $"--el-card-body-bg:{BodyBackgroundColor}")
                    .Add(Style)
                    .ToString();
            }
        }

        private string FooterCss => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add(FooterStyle)
            .ToString();

        /// <summary>
        /// 把「纯数字」补成 px 的长度写法；非数字（含单位 / 函数式写法）原样返回；空白返回 <c>null</c>。
        /// </summary>
        private static string ToCssLength(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }
            var trimmed = value.Trim();
            var numeric = true;
            for (var i = 0; i < trimmed.Length; i++)
            {
                var c = trimmed[i];
                if (!char.IsDigit(c) && c != '.' && c != '-' && c != '+')
                {
                    numeric = false;
                    break;
                }
            }
            return numeric ? trimmed + "px" : trimmed;
        }
    }
}
