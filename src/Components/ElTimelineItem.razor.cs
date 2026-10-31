using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    public partial class ElTimelineItem : ElementComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public string Timestamp { get; set; }

        [Parameter]
        public RenderFragment TimestampContent { get; set; }

        [Parameter]
        public TimelinePlacement Placement { get; set; } = TimelinePlacement.Bottom;

        /// <summary>是否隐藏时间戳（对齐 Element Plus <c>hide-timestamp</c>，默认 false）。</summary>
        [Parameter]
        public bool HideTimestamp { get; set; }

        /// <summary>是否垂直居中（对齐 Element Plus <c>center</c>，默认 false）。</summary>
        [Parameter]
        public bool Center { get; set; }

        /// <summary>primary/success/warning/danger/info</summary>
        [Parameter]
        public string Type { get; set; }

        [Parameter]
        public string Color { get; set; }

        /// <summary>large / normal（默认 normal；对齐 Element Plus 默认值，确保节点始终有尺寸）</summary>
        [Parameter]
        public string Size { get; set; } = "normal";

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public RenderFragment Dot { get; set; }

        [Parameter]
        public bool HideTail { get; set; }

        /// <summary>空心节点（与 Solid 互斥，默认 false）</summary>
        [Parameter]
        public bool Hollow { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        [Parameter]
        public EventCallback OnClick { get; set; }

        protected bool HasTimestamp => !HideTimestamp && (TimestampContent != null || !string.IsNullOrWhiteSpace(Timestamp));

        protected string ItemClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-timeline-item", Cls)
            .AddIf(Hollow, "el-timeline-item--hollow")
            .AddIf(Center, "el-timeline-item__center")
            .ToString();

        protected string TailClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-timeline-item__tail")
            .AddIf(HideTail, "is-hidden")
            .ToString();

        protected string NodeClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-timeline-item__node")
            .AddIf(!string.IsNullOrWhiteSpace(Type), $"el-timeline-item__node--{Type}")
            .AddIf(!string.IsNullOrWhiteSpace(Size), $"el-timeline-item__node--{Size}")
            .AddIf(!string.IsNullOrWhiteSpace(Icon), "el-timeline-item__node--icon")
            .AddIf(Hollow, "is-hollow")
            .ToString();

        protected string NodeStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(!string.IsNullOrWhiteSpace(Color), $"background-color:{Color}")
            .ToString();

        protected string TimestampClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-timeline-item__timestamp", $"is-{Placement.ToString().ToLower()}")
            .ToString();

        protected string IconClass => NormalizeIcon(Icon);

        private static string NormalizeIcon(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var trimmed = value.Trim();
            return trimmed.StartsWith("el-icon-") ? trimmed : $"el-icon-{trimmed}";
        }
    }
}