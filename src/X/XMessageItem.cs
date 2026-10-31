using ElementPlus;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace ElementPlus.X
{
    public class XMessageItem
    {
        public string Id { get; set; }

        public XMessageRole Role { get; set; } = XMessageRole.Assistant;

        public string Content { get; set; }

        public string Header { get; set; }

        /// <summary>时间文本（如 10:24），显示在作者名右侧；为空则隐藏。</summary>
        public string Time { get; set; }

        public string Footer { get; set; }

        public string Avatar { get; set; }

        public string AvatarText { get; set; }

        public string AvatarIcon { get; set; }

        public string Placement { get; set; }

        public bool Loading { get; set; }

        public bool Typing { get; set; }

        public IEnumerable<XAttachmentItem> Attachments { get; set; }

        public RenderFragment ContentTemplate { get; set; }

        public RenderFragment Actions { get; set; }
    }
}
