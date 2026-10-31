using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Globalization;

namespace ElementPlus
{
    public partial class ElRow : ElementComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public int Gutter { get; set; }

        [Parameter]
        public RowJustify Justify { get; set; } = RowJustify.Start;

        [Parameter]
        public RowAlign Align { get; set; } = RowAlign.Top;

        [Parameter]
        public string Tag { get; set; } = "div";

        /// <summary>
        /// 本仓 Grow 口径（Element Plus 没有这个属性，官方用父级 v-if）：
        /// <c>false</c> 时整行不进渲染树（<c>el-row</c> 壳与 <see cref="ElCol"/> 子树一起消失，不是
        /// <c>display:none</c>），用于按状态收起整块版式。默认 <c>true</c>，加参数前的行为逐字节不变。
        /// </summary>
        [Parameter] public bool Visible { get; set; } = true;

        protected IDictionary<string, object> RowParameters => new Dictionary<string, object>
        {
            [nameof(ElementTagRenderer.Tag)] = string.IsNullOrWhiteSpace(Tag) ? "div" : Tag,
            [nameof(ElementTagRenderer.ChildContent)] = ChildContent,
            [nameof(ElementTagRenderer.Attributes)] = Attributes,
            [nameof(ElementTagRenderer.Class)] = RowClass,
            [nameof(ElementTagRenderer.Style)] = RowStyle
        };

        protected string RowClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-row", Cls)
            .AddIf(Justify != RowJustify.Start, $"is-justify-{CssValue(Justify)}")
            .AddIf(Align != RowAlign.Top, $"is-align-{CssValue(Align)}")
            .ToString();

        protected string RowStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(Gutter > 0, $"margin-left:-{(Gutter / 2m).ToString("0.###", CultureInfo.InvariantCulture)}px", $"margin-right:-{(Gutter / 2m).ToString("0.###", CultureInfo.InvariantCulture)}px")
            .Add(Style)
            .ToString();

        private static string CssValue(RowJustify value) => value switch
        {
            RowJustify.SpaceAround => "space-around",
            RowJustify.SpaceBetween => "space-between",
            RowJustify.SpaceEvenly => "space-evenly",
            _ => value.ToString().ToLowerInvariant()
        };

        private static string CssValue(RowAlign value) => value switch
        {
            RowAlign.Middle => "middle",
            RowAlign.Bottom => "bottom",
            _ => "top"
        };
    }
}
