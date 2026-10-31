using ElementPlus;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;

namespace ElementPlus.X
{
    public partial class ElXBubble : ElementComponentBase
    {
        [Parameter]
        public XMessageRole Role { get; set; } = XMessageRole.Assistant;

        [Parameter]
        public string Placement { get; set; }

        [Parameter]
        public string Header { get; set; }

        /// <summary>
        /// 时间文本（如 <c>10:24</c> / <c>09-26 10:24</c>），显示在作者名右侧。
        /// 为空则不渲染、不占位；用户侧（<c>Role=User</c>）同样显示在名字右侧。
        /// </summary>
        [Parameter]
        public string Time { get; set; }

        [Parameter]
        public string Content { get; set; }

        [Parameter]
        public string Footer { get; set; }

        [Parameter]
        public string Avatar { get; set; }

        [Parameter]
        public string AvatarText { get; set; }

        [Parameter]
        public string AvatarIcon { get; set; }

        [Parameter]
        public bool Loading { get; set; }

        [Parameter]
        public bool Typing { get; set; }

        [Parameter]
        public int TypingSpeed { get; set; } = 20;

        [Parameter]
        public bool ShowAvatar { get; set; } = true;

        [Parameter]
        public IEnumerable<XAttachmentItem> Attachments { get; set; }

        [Parameter]
        public RenderFragment HeaderContent { get; set; }

        [Parameter]
        public RenderFragment FooterContent { get; set; }

        [Parameter]
        public RenderFragment AvatarContent { get; set; }

        [Parameter]
        public RenderFragment Actions { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        protected string BubbleClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-x-bubble", Cls)
            .Add($"el-x-bubble--{Role.ToString().ToLowerInvariant()}")
            .AddIf(IsEndPlacement, "is-end")
            .AddIf(Loading, "is-loading")
            .ToString();

        protected string AvatarTextValue => !string.IsNullOrWhiteSpace(AvatarText)
            ? AvatarText
            : Role == XMessageRole.User ? "U" : "AI";

        private bool IsEndPlacement => string.Equals(Placement, "end", System.StringComparison.OrdinalIgnoreCase)
            || Role == XMessageRole.User;
    }
}
