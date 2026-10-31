using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Collections.Generic;

namespace ElementPlus
{
    public sealed class ElementTagRenderer : ComponentBase
    {
        [Parameter]
        public string Tag { get; set; } = "span";

        [Parameter]
        public string Class { get; set; }

        [Parameter]
        public string Style { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter(CaptureUnmatchedValues = true)]
        public IDictionary<string, object> Attributes { get; set; }

        /// <summary>
        /// 宿主需要拿到真正渲染出来的那个元素（例如量文字有没有被裁）时用它回传 <see cref="ElementReference"/>。
        /// 不传则一个帧都不多发射，渲染输出与引入本参数之前逐字节一致。
        /// </summary>
        [Parameter]
        public EventCallback<ElementReference> ElementCaptured { get; set; }

        // 引用捕获帧之前的帧必须按槽位固定编号：可选帧增减若让捕获帧的序列号平移，
        // Blazor diff 会抛 NotImplementedException: Unexpected frame type during RemoveOldFrame:
        // ElementReferenceCapture（同 ElButton.razor.cs:321-330 记的那次事故，守卫 R1 扫的就是这个形状）。
        private const int SeqOpen = 0;
        private const int SeqSpread = 1;
        private const int SeqClass = 2;
        private const int SeqStyle = 3;
        private const int SeqContent = 4;
        private const int SeqCapture = 5;

        private static readonly IDictionary<string, object> EmptyAttributes = new Dictionary<string, object>();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(SeqOpen, string.IsNullOrWhiteSpace(Tag) ? "span" : Tag.Trim().ToLowerInvariant());
            builder.AddMultipleAttributes(SeqSpread, Attributes ?? EmptyAttributes);
            builder.AddAttribute(SeqClass, "class", Class);
            builder.AddAttribute(SeqStyle, "style", Style);
            builder.AddContent(SeqContent, ChildContent);
            if (ElementCaptured.HasDelegate)
            {
                builder.AddElementReferenceCapture(SeqCapture, reference => ElementCaptured.InvokeAsync(reference));
            }

            builder.CloseElement();
        }
    }
}
