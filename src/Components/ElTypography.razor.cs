using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// ElTypography — 排版根容器，对齐 Element Plus Typography 官方 API。
    /// 用于包裹 ElTypographyTitle / ElText / ElTypographyParagraph，统一排版上下文。
    /// </summary>
    public partial class ElTypography : ElementComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public string Tag { get; set; } = "div";

        protected string EffectiveTag => string.IsNullOrWhiteSpace(Tag) ? "div" : Tag;

        protected string TypographyClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-typography", Cls)
            .ToString();

        protected IDictionary<string, object> TypographyParameters => new Dictionary<string, object>
        {
            [nameof(ElementTagRenderer.Tag)] = EffectiveTag,
            [nameof(ElementTagRenderer.ChildContent)] = ChildContent,
            [nameof(ElementTagRenderer.Attributes)] = Attributes,
            [nameof(ElementTagRenderer.Class)] = TypographyClass,
            [nameof(ElementTagRenderer.Style)] = Style
        };
    }
}
