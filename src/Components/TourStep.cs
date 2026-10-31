using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// 官方 Element Plus el-tour-step 对齐
    /// </summary>
    public class TourStep
    {
        /// <summary>目标元素 CSS 选择器或 ElementReference id</summary>
        public string Target { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public RenderFragment Content { get; set; }

        /// <summary>弹出位置：top/bottom/left/right/top-start/top-end/...。
        /// 留空则由 <see cref="ElTour.Placement"/> 组件级兜底（默认 bottom）。</summary>
        public string Placement { get; set; }

        /// <summary>light / dark（默认 light）</summary>
        public string Effect { get; set; } = "light";

        /// <summary>是否显示箭头（默认 true）</summary>
        public bool? ShowArrow { get; set; }

        /// <summary>弹层距离目标的偏移（像素）</summary>
        public int Offset { get; set; } = 10;

        /// <summary>是否显示关闭按钮</summary>
        public bool? ShowClose { get; set; }

        /// <summary>是否显示遮罩</summary>
        public bool? Mask { get; set; }

        /// <summary>遮罩额外 style</summary>
        public string MaskExtraStyle { get; set; }

        /// <summary>自定义 z-index（覆盖组件级 <see cref="ElTour.ZIndex"/>：面板与遮罩同时生效）</summary>
        public int? ZIndex { get; set; }

        /// <summary>是否可点击遮罩关闭</summary>
        public bool? CloseOnClickMask { get; set; }

        /// <summary>按钮主题：default（默认）/ primary（对应官方 step type）</summary>
        public string Type { get; set; } = "default";

        /// <summary>面板内容区自定义 style（官方 step content-style）</summary>
        public string ContentStyle { get; set; }

        /// <summary>自定义头部插槽（官方 step header）</summary>
        public RenderFragment<TourStep> StepHeader { get; set; }

        /// <summary>下一步按钮附加属性（官方 next-button-props）</summary>
        public IReadOnlyDictionary<string, object> NextButtonProps { get; set; }

        /// <summary>上一步按钮附加属性（官方 prev-button-props）</summary>
        public IReadOnlyDictionary<string, object> PrevButtonProps { get; set; }
    }

    /// <summary>
    /// 遮罩镂空与目标之间的空隙（官方 el-tour gap）。字段与官方 <c>types.ts</c> 的
    /// <c>TourGap{ offset?: number | [number, number], radius?: number }</c> 一一对应，
    /// 默认值取官方 <c>tour.vue</c> 的 <c>gap: () =&gt; ({ offset: 6, radius: 2 })</c>。
    /// 官方没有 padding 字段（改动前那个 <c>Padding</c> 被当成遮罩的 <c>inset</c> 用，
    /// 结果是整块遮罩凭空缩小 4px、镂空反而一点没有）。
    /// </summary>
    public class TourGap
    {
        /// <summary>镂空矩形从目标矩形四周各外扩的像素（官方 gap.offset，默认 6）。</summary>
        public int Offset { get; set; } = 6;

        /// <summary>镂空的圆角半径（官方 gap.radius，默认 2）。</summary>
        public int Radius { get; set; } = 2;
    }
}