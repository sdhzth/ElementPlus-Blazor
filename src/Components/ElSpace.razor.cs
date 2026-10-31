using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    public partial class ElSpace : ElementComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public SpaceDirection Direction { get; set; } = SpaceDirection.Horizontal;

        [Parameter]
        public SpaceAlignment Alignment { get; set; } = SpaceAlignment.Center;

        [Parameter]
        public SpaceSize Size { get; set; } = SpaceSize.Default;

        [Parameter]
        public int? Spacer { get; set; }

        [Parameter]
        public bool Wrap { get; set; }

        [Parameter]
        public bool Fill { get; set; }

        /// <summary>
        /// 本仓 Grow 口径（Element Plus 没有这个属性，官方用父级 v-if）：
        /// <c>false</c> 时整条间距容器连子项一起不进渲染树（不是 <c>display:none</c>，gap 与占位都不留）。
        /// 默认 <c>true</c>，加参数前的行为逐字节不变。
        /// </summary>
        [Parameter] public bool Visible { get; set; } = true;

        [Parameter]
                public string FillRatio { get; set; }

        /// <summary>在子元素间插入的分隔符（渲染在每个子元素之间）</summary>
        [Parameter]
                public RenderFragment Separator { get; set; }

        /// <summary>给 space-items 的类名前缀（对应官方 prefix-cls）</summary>
        [Parameter]
        public string PrefixCls { get; set; } = "el-space";

        protected string SpaceClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-space", Cls)
            .Add($"el-space--{Direction.ToString().ToLowerInvariant()}")
            .AddIf(Wrap, "is-wrap")
            .AddIf(Fill, "is-fill")
            .ToString();

        protected string SpaceStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add($"align-items:{AlignmentValue}")
            .Add($"gap:{GapValue}")
            .AddIf(!string.IsNullOrWhiteSpace(FillRatio), $"--el-space-fill-ratio:{FillRatio}")
            .Add(Style)
            .ToString();

        private string GapValue => Spacer.HasValue
            ? $"{Spacer.Value}px"
            : Size switch
            {
                SpaceSize.Small => "8px",
                SpaceSize.Large => "16px",
                _ => "12px"
            };

                /// <summary>获取 item 类名前缀（供 ElSpaceItem Cascading 调用）</summary>
        public string GetPrefixCls() => PrefixCls;

        private string AlignmentValue => Alignment switch
        {
            SpaceAlignment.Start => "flex-start",
            SpaceAlignment.End => "flex-end",
            SpaceAlignment.Baseline => "baseline",
            SpaceAlignment.Stretch => "stretch",
            _ => "center"
        };
    }
}
