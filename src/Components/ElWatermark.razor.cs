using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Text;

namespace ElementPlus
{
    /// <summary>
    /// 水印（对齐 Element Plus 2.x Watermark）。
    /// <para>
    /// 与官方一致的三个关键点：
    /// ① <see cref="Width"/>/<see cref="Height"/> 未显式指定时按 content 自身尺寸自动测量，并且额外加上
    /// <c>space = ceil(|sin(rotate)| * 水印高 / 2)</c> 的旋转补偿，保证旋转后的文字仍完整落在水印框内；
    /// ② 拼贴图不是"一个水印框"，而是"旋转包围盒"（旋转后内容的实际外接矩形）+ gap；
    /// ③ 拼贴图横向放两份、纵向错位半格，形成官方的菱形（交错）排布。
    /// 因此水印内容不会再被裁掉一个角。
    /// </para>
    /// </summary>
    public partial class ElWatermark : ElementComponentBase
    {
        /// <summary>EP 中 width 未指定且无内容时的兜底宽度</summary>
        private const double DefaultBoxWidth = 120;

        /// <summary>EP 中 height 未指定且无内容时的兜底高度</summary>
        private const double DefaultBoxHeight = 64;

        /// <summary>fontBoundingBoxAscent + fontBoundingBoxDescent 的估算系数（EP 用 canvas 实测，这里按 1.16 估算）</summary>
        private const double FontHeightRatio = 1.16;

        private ElementReference maskRef;
        private DotNetObjectReference<ElWatermark> protectRef;
        private bool protectAttached;
        private bool disposed;

        /// <summary>
        /// 水印的宽度。不指定时，content 的默认值是它自己的宽度（与官方一致）。
        /// </summary>
        [Parameter]
        public int? Width { get; set; }

        /// <summary>
        /// 水印的高度。不指定时，content 的默认值是它自己的高度（与官方一致）。
        /// </summary>
        [Parameter]
        public int? Height { get; set; }

        /// <summary>
        /// 水印的旋转角度，单位 °。默认 -22。
        /// </summary>
        [Parameter]
        public double Rotate { get; set; } = -22;

        /// <summary>
        /// 水印元素的 z-index。默认 9。
        /// </summary>
        [Parameter]
        public int ZIndex { get; set; } = 9;

        /// <summary>
        /// 水印整体不透明度（EP alpha，默认 1）。取值 0~1，越小的值越透明。
        /// </summary>
        [Parameter]
        public double Alpha { get; set; } = 1;

        /// <summary>
        /// 水印图片地址，建议使用 2x 或 3x 图像；与官方一致，图片会被拉伸到 width × height。
        /// </summary>
        [Parameter]
        public string Image { get; set; }

        /// <summary>
        /// 水印文本内容。含换行符时按多行处理。
        /// </summary>
        [Parameter]
        public string Content { get; set; } = "Element Plus";

        /// <summary>
        /// 多行水印文本内容（对应官方的 content 数组写法），优先级高于 <see cref="Content"/>。
        /// </summary>
        [Parameter]
        public IEnumerable<string> ContentList { get; set; }

        /// <summary>
        /// 文字样式。
        /// </summary>
        [Parameter]
        public WatermarkFont Font { get; set; } = new WatermarkFont();

        /// <summary>
        /// 水印之间的间距 [x, y]。默认 [100, 100]。
        /// </summary>
        [Parameter]
        public int[] Gap { get; set; } = new[] { 100, 100 };

        /// <summary>
        /// 水印从容器左上角的偏移。默认值为 gap / 2。
        /// </summary>
        [Parameter]
        public int[] Offset { get; set; }

        /// <summary>
        /// 是否开启防篡改：水印元素被删除或样式被外部改写时自动恢复（对应官方内部实现的 MutationObserver）。
        /// 默认 true。
        /// </summary>
        [Parameter]
        public bool Protect { get; set; } = true;

        /// <summary>
        /// 添加水印的容器（对应官方默认插槽）。
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        private WatermarkFont ResolvedFont => Font ?? new WatermarkFont();

        private int GapX => GetPairValue(Gap, 0, 100);

        private int GapY => GetPairValue(Gap, 1, 100);

        private int OffsetX => GetPairValue(Offset, 0, GapX / 2);

        private int OffsetY => GetPairValue(Offset, 1, GapY / 2);

        protected string WatermarkClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-watermark", Cls)
            .ToString();

        protected string WatermarkStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add(Style)
            .ToString();

        protected string MaskStyle
        {
            get
            {
                var geometry = BuildGeometry();
                var positionLeft = (double)OffsetX - (geometry.GapX / 2);
                var positionTop = (double)OffsetY - (geometry.GapY / 2);
                var builder = HtmlPropertyBuilder.CreateCssStyleBuilder().Add($"z-index:{ZIndex}");

                // 官方 getMarkStyle()：偏移量把水印元素本身也往里推，并同步收窄尺寸
                if (positionLeft > 0)
                {
                    builder.Add($"left:{N(positionLeft)}px");
                    builder.Add($"width:calc(100% - {N(positionLeft)}px)");
                    positionLeft = 0;
                }

                if (positionTop > 0)
                {
                    builder.Add($"top:{N(positionTop)}px");
                    builder.Add($"height:calc(100% - {N(positionTop)}px)");
                    positionTop = 0;
                }

                builder.Add("background-repeat:repeat");
                builder.Add($"background-position:{N(positionLeft)}px {N(positionTop)}px");
                builder.Add($"background-image:{BuildBackgroundImage(geometry)}");
                builder.Add($"background-size:{N(geometry.TileWidth)}px");
                return builder.ToString();
            }
        }

        /// <summary>
        /// 依据内容推算水印框、旋转包围盒与拼贴尺寸（对齐官方 getMarkSize + useClips 的几何计算）。
        /// </summary>
        private WatermarkGeometry BuildGeometry()
        {
            var font = ResolvedFont;
            var fontSize = ResolveFontSizeNumber(font.FontSize);
            var fontGap = Math.Max(0, font.FontGap);
            var lines = ResolveTextLines();
            var hasImage = !string.IsNullOrWhiteSpace(Image);
            var angle = Math.PI / 180.0 * Rotate;
            var gapX = (double)GapX;
            var gapY = (double)GapY;

            double boxWidth;
            double boxHeight;
            double inkWidth;
            var space = 0.0;

            if (!hasImage && lines.Count > 0)
            {
                var maxWidth = 0.0;
                foreach (var line in lines)
                {
                    maxWidth = Math.Max(maxWidth, EstimateTextWidth(line, fontSize, font.FontWeight, font.FontStyle));
                }

                var lineHeight = Math.Ceiling(fontSize * FontHeightRatio);
                var defaultHeight = (lineHeight * lines.Count) + ((lines.Count - 1) * fontGap);
                space = Math.Ceiling(Math.Abs(Math.Sin(angle)) * defaultHeight / 2);
                inkWidth = maxWidth;
                boxWidth = Width ?? Math.Ceiling(maxWidth + space);
                boxHeight = Height ?? defaultHeight;
            }
            else
            {
                inkWidth = boxWidth = Width ?? DefaultBoxWidth;
                boxHeight = Height ?? DefaultBoxHeight;
            }

            var halfWidth = boxWidth / 2;
            var halfHeight = boxHeight / 2;
            var lineStep = fontSize + fontGap;
            var blockHeight = lines.Count > 0 ? ((lines.Count - 1) * lineStep) + fontSize : 0;

            var align = ResolveTextAlign(font.TextAlign);
            var anchor = -halfWidth + (boxWidth * align.AnchorRatio) + (space * align.SpaceRatio);
            var inkLeft = anchor - (inkWidth * align.InkRatio);

            // 内容矩形（水印框 与 实际墨迹 的并集）：显式给了过小的 width/height 时也保证内容不被裁掉
            var rectLeft = Math.Min(-halfWidth, inkLeft);
            var rectRight = Math.Max(halfWidth, inkLeft + inkWidth);
            var rectTop = -halfHeight;
            var rectBottom = Math.Max(halfHeight, -halfHeight + (hasImage ? boxHeight : blockHeight));

            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            double left = double.MaxValue, right = double.MinValue, top = double.MaxValue, bottom = double.MinValue;

            RotatePoint(rectLeft, rectTop, cos, sin, ref left, ref right, ref top, ref bottom);
            RotatePoint(rectRight, rectTop, cos, sin, ref left, ref right, ref top, ref bottom);
            RotatePoint(rectRight, rectBottom, cos, sin, ref left, ref right, ref top, ref bottom);
            RotatePoint(rectLeft, rectBottom, cos, sin, ref left, ref right, ref top, ref bottom);

            var cutWidth = right - left;
            var cutHeight = bottom - top;

            return new WatermarkGeometry
            {
                BoxWidth = boxWidth,
                BoxHeight = boxHeight,
                Space = space,
                LineStep = lineStep,
                CutLeft = left,
                CutTop = top,
                CutWidth = cutWidth,
                CutHeight = cutHeight,
                TileWidth = Math.Ceiling((cutWidth + gapX) * 2),
                TileHeight = Math.Ceiling(cutHeight + gapY),
                GapX = gapX,
                GapY = gapY,
                HasImage = hasImage,
                Lines = lines,
                FontSize = fontSize
            };
        }

        private string BuildBackgroundImage(WatermarkGeometry geometry)
        {
            var svg = new StringBuilder();
            svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" ");
            svg.Append(CultureInfo.InvariantCulture, $"width=\"{N(geometry.TileWidth)}\" height=\"{N(geometry.TileHeight)}\" ");
            svg.Append(CultureInfo.InvariantCulture, $"viewBox=\"0 0 {N(geometry.TileWidth)} {N(geometry.TileHeight)}\"");
            // EP alpha：整体不透明度（<1 时才输出，保持默认渲染零差异）
            if (Alpha > 0 && Alpha < 1)
            {
                svg.Append(CultureInfo.InvariantCulture, $" opacity=\"{N(Alpha)}\"");
            }
            svg.Append(">");

            // 官方 useClips：横向两份、纵向错位半格
            AppendTileCopy(svg, geometry, 0, 0);
            AppendTileCopy(svg, geometry, geometry.CutWidth + geometry.GapX, -(geometry.CutHeight + geometry.GapY) / 2);
            AppendTileCopy(svg, geometry, geometry.CutWidth + geometry.GapX, (geometry.CutHeight + geometry.GapY) / 2);

            svg.Append("</svg>");
            return $"url(\"data:image/svg+xml,{Uri.EscapeDataString(svg.ToString())}\")";
        }

        private void AppendTileCopy(StringBuilder svg, WatermarkGeometry geometry, double x, double y)
        {
            // 把"旋转中心"放到 x - CutLeft / y - CutTop，使该副本的旋转包围盒正好落在 (x, y)
            svg.Append(CultureInfo.InvariantCulture, $"<g transform=\"translate({N(x - geometry.CutLeft)} {N(y - geometry.CutTop)}) rotate({N(Rotate)})\">");

            if (geometry.HasImage)
            {
                AppendImage(svg, geometry);
            }
            else
            {
                AppendText(svg, geometry);
            }

            svg.Append("</g>");
        }

        private void AppendImage(StringBuilder svg, WatermarkGeometry geometry)
        {
            svg.Append(CultureInfo.InvariantCulture, $"<image href=\"{EscapeXml(Image)}\" ");
            svg.Append(CultureInfo.InvariantCulture, $"x=\"{N(-geometry.BoxWidth / 2)}\" y=\"{N(-geometry.BoxHeight / 2)}\" ");
            svg.Append(CultureInfo.InvariantCulture, $"width=\"{N(geometry.BoxWidth)}\" height=\"{N(geometry.BoxHeight)}\" preserveAspectRatio=\"none\" />");
        }

        private void AppendText(StringBuilder svg, WatermarkGeometry geometry)
        {
            var font = ResolvedFont;
            var halfWidth = geometry.BoxWidth / 2;
            var halfHeight = geometry.BoxHeight / 2;
            var align = ResolveTextAlign(font.TextAlign);
            var x = -halfWidth + (geometry.BoxWidth * align.AnchorRatio) + (geometry.Space * align.SpaceRatio);
            var baseline = ResolveBaseline(font.TextBaseline);
            var fontSize = NormalizeFontSize(font.FontSize);

            for (var i = 0; i < geometry.Lines.Count; i++)
            {
                svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{N(x)}\" y=\"{N(-halfHeight + (i * geometry.LineStep))}\" ");
                svg.Append(CultureInfo.InvariantCulture, $"fill=\"{EscapeXml(font.Color)}\" font-size=\"{EscapeXml(fontSize)}\" ");
                svg.Append(CultureInfo.InvariantCulture, $"font-weight=\"{EscapeXml(font.FontWeight)}\" font-family=\"{EscapeXml(font.FontFamily)}\" ");
                svg.Append(CultureInfo.InvariantCulture, $"font-style=\"{EscapeXml(font.FontStyle)}\" text-anchor=\"{align.Anchor}\" ");
                svg.Append(CultureInfo.InvariantCulture, $"dominant-baseline=\"{baseline}\">");
                svg.Append(EscapeXml(geometry.Lines[i]));
                svg.Append("</text>");
            }
        }

        private IReadOnlyList<string> ResolveTextLines()
        {
            if (ContentList != null)
            {
                var explicitLines = ContentList.Where(x => !string.IsNullOrEmpty(x)).ToList();
                if (explicitLines.Count > 0)
                {
                    return explicitLines;
                }
            }

            if (string.IsNullOrEmpty(Content))
            {
                return Array.Empty<string>();
            }

            return Content.Split('\n').Select(x => x.TrimEnd('\r')).Where(x => x.Length > 0).ToList();
        }

        private static void RotatePoint(double x, double y, double cos, double sin, ref double left, ref double right, ref double top, ref double bottom)
        {
            var rotatedX = (x * cos) - (y * sin);
            var rotatedY = (x * sin) + (y * cos);
            left = Math.Min(left, rotatedX);
            right = Math.Max(right, rotatedX);
            top = Math.Min(top, rotatedY);
            bottom = Math.Max(bottom, rotatedY);
        }

        /// <summary>
        /// 文字宽度估算（对齐官方 canvas measureText 的语义）。
        /// CJK 等全角字符按 1em 计算（精确），拉丁字符按字宽表估算并略微放大，宁可多留白也不裁切内容。
        /// 需要像素级精确时请显式指定 <see cref="Width"/>。
        /// internal 供 <see cref="ElChartGrowEngine"/> 复用同一份字宽表（图表图例列的预留宽要按实测文字算）。
        /// </summary>
        internal static double EstimateTextWidth(string text, double fontSize, string fontWeight, string fontStyle)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            var ratio = 0.0;
            foreach (var ch in text)
            {
                ratio += CharWidthRatio(ch);
            }

            var width = ratio * fontSize;
            if (IsBold(fontWeight))
            {
                width *= 1.04;
            }

            if (!string.IsNullOrEmpty(fontStyle) && fontStyle.IndexOf("italic", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                width *= 1.02;
            }

            return (width * 1.03) + 2;
        }

        private static double CharWidthRatio(char ch)
        {
            var code = ch;

            if (code < 128)
            {
                if (ch == ' ')
                {
                    return 0.3;
                }

                if (ch >= '0' && ch <= '9')
                {
                    return 0.556;
                }

                if (ch >= 'A' && ch <= 'Z')
                {
                    switch (ch)
                    {
                        case 'W':
                        case 'M':
                            return 0.9;
                        case 'I':
                            return 0.28;
                        case 'J':
                            return 0.5;
                        default:
                            return 0.7;
                    }
                }

                if (ch >= 'a' && ch <= 'z')
                {
                    switch (ch)
                    {
                        case 'i':
                        case 'l':
                            return 0.24;
                        case 'j':
                            return 0.26;
                        case 'f':
                        case 't':
                        case 'r':
                            return 0.34;
                        case 'm':
                            return 0.82;
                        case 'w':
                            return 0.76;
                        default:
                            return 0.53;
                    }
                }

                switch (ch)
                {
                    case '.':
                    case ',':
                    case ':':
                    case ';':
                    case '!':
                    case '|':
                    case '\'':
                    case '"':
                    case '`':
                        return 0.28;
                    case '-':
                    case '(':
                    case ')':
                    case '[':
                    case ']':
                    case '{':
                    case '}':
                    case '/':
                    case '\\':
                        return 0.36;
                    case '@':
                        return 0.95;
                    case '%':
                        return 0.86;
                    case '&':
                        return 0.7;
                    default:
                        return 0.58;
                }
            }

            return IsWideChar(code) ? 1.0 : 0.6;
        }

        private static bool IsWideChar(int code)
        {
            return (code >= 0x1100 && code <= 0x115F)
                || (code >= 0x2E80 && code <= 0x303E)
                || (code >= 0x3041 && code <= 0x33FF)
                || (code >= 0x3400 && code <= 0x4DBF)
                || (code >= 0x4E00 && code <= 0x9FFF)
                || (code >= 0xA000 && code <= 0xA4CF)
                || (code >= 0xAC00 && code <= 0xD7A3)
                || (code >= 0xF900 && code <= 0xFAFF)
                || (code >= 0xFE30 && code <= 0xFE4F)
                || (code >= 0xFF00 && code <= 0xFF60)
                || (code >= 0xFFE0 && code <= 0xFFE6)
                || (code >= 0x1F300 && code <= 0x1F64F)
                || (code >= 0x20000 && code <= 0x3FFFD);
        }

        private static bool IsBold(string fontWeight)
        {
            if (string.IsNullOrWhiteSpace(fontWeight))
            {
                return false;
            }

            if (string.Equals(fontWeight, "bold", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fontWeight, "bolder", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return double.TryParse(fontWeight, NumberStyles.Number, CultureInfo.InvariantCulture, out var numeric) && numeric >= 600;
        }

        private static TextAlignToken ResolveTextAlign(string textAlign)
        {
            switch (textAlign?.Trim().ToLowerInvariant())
            {
                case "left":
                case "start":
                    // 官方 TEXT_ALIGN_RATIO_MAP: left/start => [0, 0.5]，墨迹从左边界开始
                    return new TextAlignToken(0, 0.5, 0, "start");
                case "right":
                case "end":
                    // right/end => [1, -0.5]，墨迹在右边界结束
                    return new TextAlignToken(1, -0.5, 1, "end");
                default:
                    // center => [0.5, 0]，墨迹以中心对齐
                    return new TextAlignToken(0.5, 0, 0.5, "middle");
            }
        }

        private static string ResolveBaseline(string textBaseline)
        {
            switch (textBaseline?.Trim().ToLowerInvariant())
            {
                case "top":
                    return "text-before-edge";
                case "middle":
                    return "central";
                case "alphabetic":
                    return "alphabetic";
                case "ideographic":
                    return "ideographic";
                case "bottom":
                    return "text-after-edge";
                default:
                    return "hanging";
            }
        }

        private static int GetPairValue(IReadOnlyList<int> values, int index, int defaultValue)
        {
            return values != null && values.Count > index ? values[index] : defaultValue;
        }

        private static string NormalizeFontSize(object value)
        {
            if (value == null)
            {
                return "16px";
            }

            if (value is string text)
            {
                return ElementCssUtility.NormalizeCssSize(text);
            }

            if (IsNumeric(value))
            {
                return $"{Convert.ToString(value, CultureInfo.InvariantCulture)}px";
            }

            return value.ToString();
        }

        private static double ResolveFontSizeNumber(object value)
        {
            if (value == null)
            {
                return 16;
            }

            if (IsNumeric(value))
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }

            var text = value.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return 16;
            }

            if (text.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(0, text.Length - 2);
            }

            return double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
                ? result
                : 16;
        }

        private static bool IsNumeric(object value)
        {
            return value is byte
                || value is sbyte
                || value is short
                || value is ushort
                || value is int
                || value is uint
                || value is long
                || value is ulong
                || value is float
                || value is double
                || value is decimal;
        }

        private static string EscapeXml(string value)
        {
            return SecurityElement.Escape(value) ?? string.Empty;
        }

        private static string N(double value)
        {
            var rounded = Math.Round(value, 3, MidpointRounding.AwayFromZero);
            if (rounded == 0)
            {
                rounded = 0;
            }

            return rounded.ToString("0.###", CultureInfo.InvariantCulture);
        }

        protected override async System.Threading.Tasks.Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (!Protect || disposed)
            {
                if (protectAttached && !Protect)
                {
                    await ReleaseProtectAsync();
                }

                return;
            }

            if (protectAttached)
            {
                return;
            }

            try
            {
                protectRef ??= DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("elementWatermarkProtect", maskRef, protectRef);
                protectAttached = true;
            }
            catch (Exception)
            {
                // 无 JS 环境（单元测试 / 预渲染）下静默降级
                protectAttached = false;
            }
        }

        private async System.Threading.Tasks.Task ReleaseProtectAsync()
        {
            if (!protectAttached)
            {
                return;
            }

            protectAttached = false;
            try
            {
                await JSRuntime.InvokeVoidAsync("elementWatermarkRelease", maskRef);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 防篡改回调：水印元素被移除或样式被外部改写时，用当前参数重新写回。
        /// </summary>
        [JSInvokable]
        public async System.Threading.Tasks.Task OnWatermarkTamperedFromJs()
        {
            if (disposed)
            {
                return;
            }

            try
            {
                await JSRuntime.InvokeVoidAsync("elementWatermarkRestore", maskRef, MaskStyle);
            }
            catch (Exception)
            {
            }
        }

        public override void Dispose()
        {
            disposed = true;
            protectAttached = false;

            if (protectRef != null)
            {
                try
                {
                    _ = JSRuntime.InvokeVoidAsync("elementWatermarkRelease", maskRef);
                }
                catch (Exception)
                {
                }

                protectRef.Dispose();
                protectRef = null;
            }

            base.Dispose();
        }

        private sealed class WatermarkGeometry
        {
            public double BoxWidth { get; set; }

            public double BoxHeight { get; set; }

            public double Space { get; set; }

            public double LineStep { get; set; }

            public double CutLeft { get; set; }

            public double CutTop { get; set; }

            public double CutWidth { get; set; }

            public double CutHeight { get; set; }

            public double TileWidth { get; set; }

            public double TileHeight { get; set; }

            public double GapX { get; set; }

            public double GapY { get; set; }

            public double FontSize { get; set; }

            public bool HasImage { get; set; }

            public IReadOnlyList<string> Lines { get; set; }
        }

        private readonly struct TextAlignToken
        {
            public TextAlignToken(double anchorRatio, double spaceRatio, double inkRatio, string anchor)
            {
                AnchorRatio = anchorRatio;
                SpaceRatio = spaceRatio;
                InkRatio = inkRatio;
                Anchor = anchor;
            }

            /// <summary>fillText 的 x 在"水印框"内的相对位置</summary>
            public double AnchorRatio { get; }

            /// <summary>space 补偿施加的方向</summary>
            public double SpaceRatio { get; }

            /// <summary>墨迹左边界相对锚点的位置比例</summary>
            public double InkRatio { get; }

            /// <summary>SVG text-anchor</summary>
            public string Anchor { get; }
        }
    }
}
