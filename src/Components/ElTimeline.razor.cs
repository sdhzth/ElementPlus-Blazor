using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    public partial class ElTimeline : ElementComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>最后一项是否为"正在进行中"（显示 pending 占位）</summary>
        [Parameter]
        public bool Pending { get; set; }

        /// <summary>水平模式（默认垂直）</summary>
        [Parameter]
        public bool Horizontal { get; set; }

        /// <summary>
        /// 是否逆序（自 Element Plus 2.11.9 起）。为 true 时最后一个节点显示在最上方。
        /// 通过 CSS 列反转实现，保持原有 <see cref="ChildContent"/> 声明顺序不变。
        /// </summary>
        [Parameter]
        public bool Reverse { get; set; }

        /// <summary>
        /// 时间线与内容的相对位置（对齐 Element Plus 2.13.1 的 <c>mode</c>）。
        /// Start=内容在右（默认）、End=内容在左、Alternate/AlternateReverse=左右交替。
        /// </summary>
        [Parameter]
        public TimelineMode Mode { get; set; } = TimelineMode.Start;

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        protected string TimelineClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-timeline", Cls)
            .AddIf(Horizontal, "el-timeline--horizontal")
            .AddIf(Pending, "el-timeline--pending")
            .AddIf(Reverse, "el-timeline--reverse")
            .AddIf(Mode != TimelineMode.Start, $"el-timeline--{TimelineModeClass(Mode)}")
            .ToString();

        private static string TimelineModeClass(TimelineMode mode) => mode switch
        {
            TimelineMode.End => "end",
            TimelineMode.Alternate => "alternate",
            TimelineMode.AlternateReverse => "alternate-reverse",
            _ => "start"
        };
    }
}