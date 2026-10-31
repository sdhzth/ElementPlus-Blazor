using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// JS 函数 <c>elementTextOverflowState</c>（<c>wwwroot/js/dom.js</c>）的返回值，仅本库内部使用。
    /// 两个轴的读数都给：单行 ellipsis 用宽，<c>-webkit-line-clamp</c> 多行截断用高。
    /// </summary>
    internal sealed class TextClipState
    {
        public int ScrollWidth { get; set; }

        public int ClientWidth { get; set; }

        public int ScrollHeight { get; set; }

        public int ClientHeight { get; set; }

        public string Text { get; set; }

        /// <summary>被裁元素在视口中的矩形（<c>getBoundingClientRect</c>）。只有需要把浮层钉成
        /// <c>position:fixed</c> 的调用方会读（<see cref="ElOverflowTooltip"/>），余者忽略。</summary>
        public double RectTop { get; set; }

        public double RectLeft { get; set; }

        public double RectWidth { get; set; }

        public double RectHeight { get; set; }
    }

    /// <summary>
    /// 「文字被裁掉之后悬停看全文」的公共测算逻辑（不是组件）：菜单项 / 子菜单标题 / 文本 / 表格单元格共用。
    /// 判定口径：<c>scrollWidth &gt; clientWidth + 1</c>（单行）或在允许纵向时 <c>scrollHeight &gt; clientHeight + 1</c>
    /// （多行 line-clamp）才算真的被截断，未截断返回 <c>null</c>，调用方据此不弹 tooltip ——
    /// 否则每个元素悬停都会浮出一层。
    /// </summary>
    /// <remarks>
    /// 纵向判据必须按调用方开关给：<c>.el-text.is-truncated</c> 是 <c>white-space:nowrap</c> 的单行框，
    /// 它的高度差来自 <c>line-height</c> 取整而不是截断，无条件开纵向判据会让没截断的文字也弹出来。
    /// </remarks>
    internal static class TextClip
    {
        /// <summary>JS 函数名（bUnit 打桩与探针共用同一个口径）。</summary>
        internal const string MeasureFunction = "elementTextOverflowState";

        /// <summary>
        /// 测算元素文字是否被裁掉，是则返回读数（含视口矩形），否则返回 <c>null</c>。
        /// <paramref name="vertical"/> 为 true 时额外承认"高度方向被裁"（line-clamp / 单元格固定行高）。
        /// </summary>
        public static async Task<TextClipState> MeasureClippedStateAsync(IJSRuntime jsRuntime, ElementReference element, bool vertical = false)
        {
            // bUnit 的 Loose 模式对未打桩的调用返回 default(T)，即 null —— 等价于「未截断」，不会抛异常。
            var state = await jsRuntime.InvokeAsync<TextClipState>(MeasureFunction, element);
            if (state == null)
            {
                return null;
            }

            var clipped = state.ScrollWidth > state.ClientWidth + 1
                          || (vertical && state.ScrollHeight > state.ClientHeight + 1);
            return clipped ? state : null;
        }

        /// <summary>
        /// 只量矩形、不做「是否被裁」判据：给「一定要浮、但浮层所在盒子会被祖先裁掉」的调用方用
        /// （<see cref="ElTooltip.FixedPopper"/>，行内图标按钮的悬停提示就是这种——按钮根本没被裁，
        /// 走 <see cref="MeasureClippedStateAsync"/> 会直接拿到 null 而永远不浮）。
        /// </summary>
        public static async Task<TextClipState> MeasureRectAsync(IJSRuntime jsRuntime, ElementReference element)
        {
            return await jsRuntime.InvokeAsync<TextClipState>(MeasureFunction, element);
        }

        /// <summary>
        /// 测算元素文字是否被裁掉，是则返回完整文字。
        /// <paramref name="vertical"/> 为 true 时额外承认"高度方向被裁"（line-clamp / 单元格固定行高）。
        /// </summary>
        public static async Task<string> MeasureClippedTextAsync(IJSRuntime jsRuntime, ElementReference element, bool vertical = false)
        {
            var state = await MeasureClippedStateAsync(jsRuntime, element, vertical);
            return state == null ? null : ExtractText(state);
        }

        /// <summary>从读数里取完整文字；空白视为没内容。</summary>
        public static string ExtractText(TextClipState state)
        {
            var text = (state.Text ?? string.Empty).Trim();
            return text.Length == 0 ? null : text;
        }

        /// <summary>浮层与被裁元素之间的间距。与 <c>ElTooltip.Offset</c> 的默认值同口径，
        /// 坐标由这里算，两边必须是同一个数。</summary>
        internal const int PopperGap = 8;

        /// <summary>「上方至少要有这么高才放得下单行浮层」的经验阈值：<c>.el-tooltip__popper</c> 是
        /// 12px 字号 × 1.2 行高 + 上下 8px padding ≈ 31px，再加边框与一点余量。多行内容仍可能超出，
        /// 这时浮层会压到上方行上而不是消失 —— 阈值只管"整条跑到视口外"那种彻底看不见的情况。</summary>
        internal const int PopperMinRoomAbove = 40;

        /// <summary>
        /// 把浮层钉成 <c>position:fixed</c> + 悬停时实测的视口坐标；上方放不下时翻到下方，翻面结果从
        /// <paramref name="placement"/> 出来（要一并交给 ElTooltip，箭头与外边距才跟着翻）。
        /// 间距由 <paramref name="gap"/> 给：默认沿用 <see cref="PopperGap"/>（<c>ElOverflowTooltip</c>
        /// 的既有口径一字不变），<c>ElTooltip.FixedPopper</c> 传的是它自己的 <c>Offset</c> ——
        /// 否则用户调了 <c>Offset</c> 却发现浮层纹丝不动。
        /// </summary>
        /// <remarks>
        /// 为什么不能用 ElTooltip 默认的 absolute：浮层是触发 span 的子孙，absolute 的包含块与裁剪都来自
        /// 「最近的非 visible overflow 祖先」那条链（表格是 <c>td → .el-table__body-wrapper(overflow:auto)
        /// → .el-table(overflow:hidden)</c>，侧栏菜单是 <c>.demo-aside-scroll(overflow-y:auto)</c>），
        /// 于是靠边的浮层整条被裁掉（浏览器实测：浮层矩形 1127..1159 对上容器上缘 1156，中心命中测试拿到的
        /// 是被裁容器里的元素而不是浮层本身）。fixed 的包含块是视口，只要祖先没有
        /// <c>transform/filter/contain</c> 就不受 overflow 裁剪。
        /// EP 不遇到这个问题：表格侧整张表只渲染一个挂在滚动容器外的虚拟 tooltip
        /// （<c>use-tooltip/index.ts</c>），菜单侧的浮层挂在 body 下；本仓按单元格/菜单项各挂各的，
        /// 就只能把坐标钉到视口。
        /// </remarks>
        public static string BuildFixedPopperStyle(TextClipState state, string requestedPlacement, out string placement, int gap = PopperGap)
        {
            var requested = string.IsNullOrWhiteSpace(requestedPlacement) ? "bottom" : requestedPlacement.ToLowerInvariant();
            var isTop = requested.StartsWith("top", StringComparison.Ordinal);
            var isLeft = requested.StartsWith("left", StringComparison.Ordinal);
            var isRight = requested.StartsWith("right", StringComparison.Ordinal);

            // 上方没地方就翻到下方；只在真翻的时候改 placement，其它情况原样透传。
            var flipped = isTop && state.RectTop < PopperMinRoomAbove;
            placement = flipped ? "bottom" : requested;

            var centerX = state.RectLeft + state.RectWidth / 2;
            var centerY = state.RectTop + state.RectHeight / 2;

            string left;
            string top;
            string transform;
            if (isTop && !flipped)
            {
                left = Px(centerX);
                top = Px(state.RectTop - gap);
                transform = "translate(-50%, -100%)";
            }
            else if (isRight)
            {
                left = Px(state.RectLeft + state.RectWidth + gap);
                top = Px(centerY);
                transform = "translate(0, -50%)";
            }
            else if (isLeft)
            {
                left = Px(state.RectLeft - gap);
                top = Px(centerY);
                transform = "translate(-100%, -50%)";
            }
            else
            {
                left = Px(centerX);
                top = Px(state.RectTop + state.RectHeight + gap);
                transform = "translate(-50%, 0)";
            }

            // ElTooltip 的 placement 规则里带了 bottom/right 锚点，PopperStyle 是最后拼上去的，
            // 所以要显式让位（同名属性后声明的赢），否则 left/top 与 bottom:100% 同时生效会把浮层顶出去。
            return $"position:fixed;left:{left};top:{top};right:auto;bottom:auto;transform:{transform}";
        }

        private static string Px(double value)
        {
            return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "px";
        }
    }
}
