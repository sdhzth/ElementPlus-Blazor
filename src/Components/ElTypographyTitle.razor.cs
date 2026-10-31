using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// ElTypographyTitle — 标题，对齐 Element Plus Typography Title 官方 API。
    /// 支持 Level 1~5（渲染为 h1~h5）、Type（primary/success/warning/danger/info）、
    /// Content / 默认插槽、Copyable（一键复制）、Truncated / LineClamp。
    /// </summary>
    public partial class ElTypographyTitle : ElementComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>标题文本（优先于 ChildContent）；与官方 content 属性对应。</summary>
        [Parameter]
        public string Content { get; set; }

        /// <summary>标题层级，1~5，渲染为 h1~h5，默认 1。</summary>
        [Parameter]
        public int Level { get; set; } = 1;

        /// <summary>标题类型：default / primary / success / warning / danger / info。</summary>
        [Parameter]
        public TextType Type { get; set; } = TextType.Default;

        /// <summary>是否显示复制按钮（点击复制 Content）。</summary>
        [Parameter]
        public bool Copyable { get; set; }

        /// <summary>单行截断（对应官方 truncated）。</summary>
        [Parameter]
        public bool Truncated { get; set; }

        /// <summary>多行截断行数（对应官方 line-clamp）。</summary>
        [Parameter]
        public int? LineClamp { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        protected int EffectiveLevel => Math.Clamp(Level, 1, 5);

        protected string HeadingTag => "h" + EffectiveLevel;

        protected string TitleClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-typography-title")
            .Add($"el-typography-title--{EffectiveLevel}")
            .AddIf(Type != TextType.Default, $"el-typography-title--{Type.ToString().ToLowerInvariant()}")
            .AddIf(Truncated, "is-truncated")
            .AddIf(LineClamp.HasValue && LineClamp.Value > 0, "is-line-clamp")
            .Add(Cls)
            .ToString();

        protected string TitleStyle
        {
            get
            {
                var clampStyle = LineClamp.HasValue && LineClamp.Value > 0
                    ? $"--el-text-line-clamp:{LineClamp.Value}"
                    : null;
                return HtmlPropertyBuilder.CreateCssStyleBuilder()
                    .AddIf(clampStyle != null, clampStyle)
                    .Add(Style)
                    .ToString();
            }
        }

        protected RenderFragment EffectiveChildContent => builder =>
        {
            if (!string.IsNullOrEmpty(Content))
            {
                builder.AddContent(0, Content);
            }
            else if (ChildContent != null)
            {
                builder.AddContent(1, ChildContent);
            }

            if (Copyable)
            {
                builder.OpenElement(2, "span");
                builder.AddAttribute(3, "class", "el-typography-title__copy");
                builder.AddAttribute(4, "title", "复制");
                builder.AddAttribute(5, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, CopyAsync));
                builder.AddContent(6, (RenderFragment)(b =>
                {
                    b.OpenElement(0, "i");
                    b.AddAttribute(1, "class", "el-icon-document-copy");
                    b.CloseElement();
                }));
                builder.CloseElement();
            }
        };

        protected IDictionary<string, object> TitleParameters => new Dictionary<string, object>
        {
            [nameof(ElementTagRenderer.Tag)] = HeadingTag,
            [nameof(ElementTagRenderer.ChildContent)] = EffectiveChildContent,
            [nameof(ElementTagRenderer.Attributes)] = Attributes,
            [nameof(ElementTagRenderer.Class)] = TitleClass,
            [nameof(ElementTagRenderer.Style)] = TitleStyle
        };

        private async Task CopyAsync(MouseEventArgs e)
        {
            if (string.IsNullOrEmpty(Content))
            {
                return;
            }

            try
            {
                await JSRuntime.InvokeVoidAsync("elementCopyText", Content);
            }
            catch
            {
                // 剪贴板不可用时静默失败，不影响渲染
            }
        }
    }
}
