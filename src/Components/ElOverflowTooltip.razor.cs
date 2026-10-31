using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 「文字被裁掉之后悬停看全文」的宿主组件，本库内部共用（<see cref="ElText"/>、<see cref="ElTable"/>、
    /// <see cref="ElTableV2"/> 都走这里），不是 Element Plus 的组件。
    /// </summary>
    /// <remarks>
    /// 两种模式互斥，不会叠成两层浮层：
    /// <list type="bullet">
    /// <item><see cref="Enabled"/> 为 true —— 手动 <c>ElTooltip</c>，进入文字时实测被裁才浮（本库扩展）。</item>
    /// <item><see cref="BindTitle"/> 为 true —— 按 EP 的 <c>text.vue:42-67</c> 只给原生 <c>title</c>。</item>
    /// </list>
    /// </remarks>
    public partial class ElOverflowTooltip : ElementComponentBase
    {
        /// <summary>被裁的那段内容（由宿主给 RenderFragment，本件不关心它里面是什么）。</summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>是否挂悬停浮层。false 时不加任何包装元素。</summary>
        [Parameter]
        public bool Enabled { get; set; }

        /// <summary>关掉浮层时是否改绑 EP 的原生 <c>title</c>。</summary>
        [Parameter]
        public bool BindTitle { get; set; }

        [Parameter]
        public string Tag { get; set; } = "div";

        [Parameter]
        public string Class { get; set; }

        /// <summary>
        /// 被裁那个元素的样式。名字带 Clip 前缀是因为基类 <see cref="ElementComponentBase"/> 已经有一个
        /// <c>Style</c> 参数，同名会撞在「参数名大小写不敏感且必须唯一」上（运行时抛
        /// InvalidOperationException，bUnit 里表现为整棵渲染树失败）；基类那条默认值是空串，
        /// 直接复用还会给每个元素多渲染一个 <c>style=""</c> 属性。
        /// </summary>
        [Parameter]
        public string ClipStyle { get; set; }

        /// <summary>要落到那个元素上的其它特性（宿主自己写的 class/id/role 等，走特性收集）。</summary>
        [Parameter]
        public IDictionary<string, object> HostAttributes { get; set; }

        [Parameter]
        public string Placement { get; set; } = "top";

        [Parameter]
        public string Effect { get; set; } = "dark";

        /// <summary>触发 span 自己的样式：放进 flex 容器时必须带 <c>min-width:0</c>，否则 flex item 的
        /// 自动最小尺寸＝min-content，包装层会把整行文字重新撑出去、截断失效。</summary>
        [Parameter]
        public string TriggerStyle { get; set; }

        /// <summary>是否承认"高度方向被裁"（多行 line-clamp / 固定行高的单元格）。见 <see cref="TextClip"/> 的注释。</summary>
        [Parameter]
        public bool Vertical { get; set; }

        /// <summary>
        /// 关掉浮层时只渲染内容、连被裁的那个元素都不输出。<see cref="ElTableV2"/> 用它：那里被裁的盒子
        /// （<c>.el-table-v2__cell</c>）由宿主自己写，本件只负责往里塞一层「触发 span + 文字 span」。
        /// 与 <see cref="BindTitle"/> 互斥：没有可测可写 title 的元素，原生 title 请宿主自己绑。
        /// </summary>
        [Parameter]
        public bool ContentOnlyWhenDisabled { get; set; }

        private ElementReference clipElement;
        private string ellipsisTooltipText;
        private bool ellipsisTooltipVisible;
        private string popperStyle;
        private string popperPlacement;

        protected bool EllipsisTooltipVisible => ellipsisTooltipVisible;

        protected string EllipsisTooltipText => ellipsisTooltipText;

        /// <summary>钉成 fixed 后的浮层内联样式（见 <see cref="TextClip.BuildFixedPopperStyle"/>）。未挂浮层时为 null。</summary>
        protected string PopperStyleValue => popperStyle;

        /// <summary>实际用的 placement：上方放不下时翻到下方，一并交给 <c>ElTooltip</c>，
        /// 这样 <c>x-placement</c> 上的箭头与外边距跟着翻，不会浮在下边却带个朝上的箭头。</summary>
        protected string PopperPlacementValue => popperPlacement ?? Placement;

        private Task OnElementCaptured(ElementReference reference)
        {
            clipElement = reference;
            return Task.CompletedTask;
        }

        private async Task OnMouseEnterAsync(MouseEventArgs args)
        {
            var state = await TextClip.MeasureClippedStateAsync(JSRuntime, clipElement, Vertical);
            ellipsisTooltipText = state == null ? null : TextClip.ExtractText(state);
            if (ellipsisTooltipText == null)
            {
                ellipsisTooltipVisible = false;
                popperStyle = null;
                popperPlacement = null;
                return;
            }

            ellipsisTooltipVisible = true;
            popperStyle = TextClip.BuildFixedPopperStyle(state, Placement, out var placement);
            popperPlacement = placement;
        }

        private void OnMouseLeave(MouseEventArgs args)
        {
            ellipsisTooltipVisible = false;
        }

        /// <summary>
        /// 挂浮层时把进入/离开事件也交给被裁的那个元素（走特性收集，与 <c>ElTable.GetEditingCellAttributes</c>
        /// 同一形状）。宿主自己写了同名事件就不抢（字典按大小写不敏感比对）。
        /// </summary>
        private IDictionary<string, object> ClipAttributes
        {
            get
            {
                if (!Enabled)
                {
                    return HostAttributes;
                }

                var copy = HostAttributes == null
                    ? new Dictionary<string, object>()
                    : new Dictionary<string, object>(HostAttributes, StringComparer.OrdinalIgnoreCase);
                if (!copy.ContainsKey("onmouseenter"))
                {
                    copy["onmouseenter"] = EventCallback.Factory.Create<MouseEventArgs>(this, OnMouseEnterAsync);
                }
                if (!copy.ContainsKey("onmouseleave"))
                {
                    copy["onmouseleave"] = EventCallback.Factory.Create<MouseEventArgs>(this, OnMouseLeave);
                }

                return copy;
            }
        }

        /// <summary>
        /// EP 的原生 title 口径：只在真的被裁时写，宿主自己给了 title 就不动（<c>text.vue:43-45</c>）。
        /// 与 EP 的差别：EP 每次更新都重算，这里只在首帧之后量一次（Blazor 侧每帧重算要多一次 JS 往返）。
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender || Enabled || !BindTitle)
            {
                return;
            }

            var text = await TextClip.MeasureClippedTextAsync(JSRuntime, clipElement, Vertical);
            if (text != null)
            {
                await JSRuntime.InvokeVoidAsync("elementSetEllipsisTitle", clipElement, text);
            }
        }
    }
}
