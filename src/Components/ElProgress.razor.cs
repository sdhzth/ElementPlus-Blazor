using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ElementPlus
{
    public partial class ElProgress : ElementComponentBase
    {
        [Parameter]
        public double Percentage { get; set; }

        [Parameter]
        public ProgressType Type { get; set; } = ProgressType.Line;

        [Parameter]
        public ProgressStatus Status { get; set; } = ProgressStatus.None;

        [Parameter]
        public double StrokeWidth { get; set; } = 6;

        [Parameter]
        public bool TextInside { get; set; }

        [Parameter]
        public bool ShowText { get; set; } = true;

        [Parameter]
        public int Width { get; set; } = 126;

        [Parameter]
        public string Color { get; set; }

        [Parameter]
        public string TrackColor { get; set; } = "#e5e9f2";

        [Parameter]
        public string Format { get; set; }

        [Parameter]
        public string StrokeLinecap { get; set; } = "round";

        [Parameter]
        public RenderFragment<double> Content { get; set; }

        /// <summary>不确定模式（持续循环动画）</summary>
        [Parameter]
        public bool Indeterminate { get; set; }

        /// <summary>动画持续时间（秒）——控制 indeterminate 动画速度与 striped-flow 流动速度（对齐 EP <c>duration</c>）。</summary>
        [Parameter]
        public int Duration { get; set; } = 3;

        /// <summary>条纹进度条（对齐 EP <c>striped</c>，2.3.4+）。与 <see cref="StripedFlow"/> 配合可让条纹流动。</summary>
        [Parameter]
        public bool Striped { get; set; }

        /// <summary>让条纹流动起来（对齐 EP <c>striped-flow</c>，2.3.4+）。须与 <see cref="Striped"/> 同时为真。</summary>
        [Parameter]
        public bool StripedFlow { get; set; }

        /// <summary>颜色函数（对齐 EP <c>color</c> 的函数形态）：入参当前百分比、返回颜色字符串，优先级高于 <see cref="Color"/> 字符串。</summary>
        [Parameter]
        public Func<double, string> ColorFunction { get; set; }

        /// <summary>颜色渐变停靠点（对齐 EP <c>color</c> 的数组形态）：按百分比生成线性渐变（直线型）或取当前百分比对应颜色（环形/仪表盘）。
        /// 设置后优先级最高，覆盖 <see cref="Color"/> 与状态色。</summary>
        [Parameter]
        public IEnumerable<ProgressColorStop> ColorStops { get; set; }

        /// <summary>文字格式化函数（对齐 EP <c>format</c>）：入参当前百分比、返回显示文本，优先级高于 <see cref="Format"/> 字符串。</summary>
        [Parameter]
        public Func<double, string> FormatFunc { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        /// <summary>
        /// 本仓 Grow 口径（Element Plus 没有这个属性，官方用父级 v-if）：
        /// <c>false</c> 时进度条整块不进渲染树（不是 <c>display:none</c>，不留一条空轨道）。
        /// 默认 <c>true</c>，加参数前的行为逐字节不变。
        /// </summary>
        [Parameter] public bool Visible { get; set; } = true;

        protected double SafePercentage => Math.Max(0, Math.Min(100, Percentage));

        protected string ProgressClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-progress", $"el-progress--{Type.ToString().ToLower()}", Cls)
            .AddIf(Status != ProgressStatus.None, $"is-{Status.ToString().ToLower()}")
            .AddIf(!ShowText, "el-progress--without-text")
            .AddIf(TextInside, "el-progress--text-inside")
            .ToString();

        protected string OuterStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add($"height:{StrokeWidth.ToString("0.###", CultureInfo.InvariantCulture)}px")
            .AddIf(!string.IsNullOrWhiteSpace(TrackColor), $"background-color:{TrackColor}")
            .ToString();

        /// <summary>直线型进度条内层样式：宽度 +（可选）颜色/渐变 +（可选）动画时长。
        /// 条纹(striped/striped-flow)与不确定(indeterminate)的动画类由 <see cref="InnerModifierClass"/> 提供。</summary>
        protected string InnerStyle
        {
            get
            {
                if (Indeterminate)
                {
                    // 动画接管宽度/位移，内联宽度仅作占位；duration 控制动画速度
                    return $"width:40%;animation-duration:{Duration.ToString(CultureInfo.InvariantCulture)}s";
                }

                var sb = new System.Text.StringBuilder();
                sb.Append($"width:{SafePercentage.ToString("0.###", CultureInfo.InvariantCulture)}%");
                if (ColorStops != null && ColorStops.Any())
                {
                    sb.Append($";background-image:{BuildGradient()}");
                }
                else
                {
                    var c = ColorFunction != null
                        ? ColorFunction(SafePercentage)
                        : (!string.IsNullOrWhiteSpace(Color) ? Color : null);
                    if (c != null)
                    {
                        sb.Append($";background-color:{c}");
                    }
                }

                if (Striped && StripedFlow)
                {
                    sb.Append($";animation-duration:{Duration.ToString(CultureInfo.InvariantCulture)}s");
                }

                return sb.ToString();
            }
        }

        /// <summary>直线/环形内层附加修饰类：条纹、条纹流动、不确定动画（对齐 EP <c>is-striped</c>/<c>is-striped-flow</c>/<c>is-indeterminate</c>）。</summary>
        protected string InnerModifierClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .AddIf(Striped, "is-striped")
            .AddIf(Striped && StripedFlow, "is-striped-flow")
            .AddIf(Indeterminate, "is-indeterminate")
            .ToString();

        /// <summary>环形/仪表盘路径描边色：颜色数组取当前百分比对应停靠色，函数/字符串次之，最后回落状态色或默认主色。</summary>
        protected string ResolvedStroke => ColorStops != null && ColorStops.Any()
            ? ColorAtPercentage(SafePercentage)
            : (ColorFunction != null
                ? ColorFunction(SafePercentage)
                : (!string.IsNullOrWhiteSpace(Color) ? Color : StrokeColorValue));

        private string BuildGradient()
        {
            var stops = (ColorStops ?? Array.Empty<ProgressColorStop>())
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Color))
                .OrderBy(s => s.Percentage)
                .ToList();
            if (stops.Count == 0)
            {
                return "none";
            }

            if (stops.Count == 1)
            {
                return stops[0].Color;
            }

            var parts = stops.Select(s => $"{s.Color} {s.Percentage.ToString("0.###", CultureInfo.InvariantCulture)}%");
            return $"linear-gradient(to right, {string.Join(", ", parts)})";
        }

        private string ColorAtPercentage(double p)
        {
            var stops = (ColorStops ?? Array.Empty<ProgressColorStop>())
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Color))
                .OrderBy(s => s.Percentage)
                .ToList();
            if (stops.Count == 0)
            {
                return StrokeColorValue;
            }

            if (p <= stops[0].Percentage)
            {
                return stops[0].Color;
            }

            if (p >= stops[stops.Count - 1].Percentage)
            {
                return stops[stops.Count - 1].Color;
            }

            for (var i = 0; i < stops.Count - 1; i++)
            {
                if (p >= stops[i].Percentage && p <= stops[i + 1].Percentage)
                {
                    return stops[i + 1].Color;
                }
            }

            return stops[stops.Count - 1].Color;
        }

        protected string CircleStyle => $"height:{Width}px;width:{Width}px";

        protected string TextStyle => Type == ProgressType.Line
            ? string.Empty
            : $"font-size:{Math.Max(12, Width * 0.111111).ToString("0.###", CultureInfo.InvariantCulture)}px";

        protected string StrokeColorValue => !string.IsNullOrWhiteSpace(Color)
            ? Color
            : Status switch
            {
                ProgressStatus.Success => "#67c23a",
                ProgressStatus.Exception => "#f56c6c",
                ProgressStatus.Warning => "#e6a23c",
                _ => "#409eff"
            };

        protected string TrackPath
        {
            get
            {
                if (Type == ProgressType.Dashboard)
                {
                    return "M 50 50 m 0 47 a 47 47 0 1 1 0 -94 a 47 47 0 1 1 0 94";
                }

                return "M 50 50 m 0 -47 a 47 47 0 1 1 0 94 a 47 47 0 1 1 0 -94";
            }
        }

        protected string CirclePathStyle
        {
            get
            {
                var perimeter = Math.PI * 2 * 47;
                var rate = Type == ProgressType.Dashboard ? 0.75 : 1;
                var dashArray = $"{(perimeter * rate).ToString("0.###", CultureInfo.InvariantCulture)}px, {perimeter.ToString("0.###", CultureInfo.InvariantCulture)}px";
                var dashOffset = ((1 - SafePercentage / 100) * perimeter * rate).ToString("0.###", CultureInfo.InvariantCulture);
                return $"stroke-dasharray:{dashArray};stroke-dashoffset:{dashOffset}px";
            }
        }

        protected RenderFragment TextContent
        {
            get
            {
                if (Content != null)
                {
                    return builder => builder.AddContent(0, Content(SafePercentage));
                }

                string text;
                if (FormatFunc != null)
                {
                    text = FormatFunc(SafePercentage);
                }
                else if (!string.IsNullOrWhiteSpace(Format))
                {
                    text = string.Format(CultureInfo.CurrentCulture, Format, SafePercentage);
                }
                else
                {
                    text = $"{SafePercentage.ToString("0.###", CultureInfo.InvariantCulture)}%";
                }

                return builder => builder.AddContent(0, text);
            }
        }
    }
}
