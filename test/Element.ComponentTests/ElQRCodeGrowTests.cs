using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// <see cref="ElQRCodeGrow"/> 组件渲染与导出行为测试。
    /// </summary>
    public class ElQRCodeGrowTests : BunitContext
    {
        public ElQRCodeGrowTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void Default_RendersSvgSymbolWithQuietZone()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters.Add(x => x.Value, "AB"));

            // 版本 1：21 个模块 + 两侧各 4 个静区 = 29。
            Assert.Contains("viewBox=\"0 0 29 29\"", cut.Markup);

            var svg = cut.Find("svg");
            Assert.Contains("el-qrcode", svg.GetAttribute("class"));
            Assert.Equal("img", svg.GetAttribute("role"));
            Assert.Equal("AB", svg.GetAttribute("aria-label"));
            Assert.Equal("crispEdges", svg.GetAttribute("shape-rendering"));
        }

        [Fact]
        public void Default_DrawsThreeFinderEyes()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters.Add(x => x.Value, "AB"));

            var eyes = cut.FindAll("[data-eye]");

            Assert.Equal(6, eyes.Count);

            foreach (var index in new[] { "0", "1", "2" })
            {
                Assert.Single(cut.FindAll($".el-qrcode__eye[data-eye=\"{index}\"]"));
                Assert.Single(cut.FindAll($".el-qrcode__eye-pupil[data-eye=\"{index}\"]"));
            }
        }

        [Fact]
        public void Default_RendersBackgroundAndModules()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters.Add(x => x.Value, "AB"));

            var background = cut.Find(".el-qrcode__background");
            Assert.Equal("#FFFFFF", background.GetAttribute("fill"));
            Assert.Equal("29", background.GetAttribute("width"));

            var modules = cut.FindAll(".el-qrcode__module");
            Assert.NotEmpty(modules);
            Assert.Equal("#000000", modules[0].GetAttribute("fill"));
        }

        [Fact]
        public void SymbolMetadata_IsExposed()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "https://element-plus.org")
                .Add(x => x.Ecc, ElQRCodeGrowEcc.Medium));

            Assert.False(cut.Instance.HasError);
            Assert.InRange(cut.Instance.Version, 1, 40);
            Assert.InRange(cut.Instance.MaskPattern, 0, 7);
            Assert.Equal(cut.Instance.Version, cut.Instance.Matrix.Version);
            Assert.Equal(ElQRCodeGrowEcc.Medium, cut.Instance.Matrix.Ecc);
            Assert.True(cut.Instance.SymbolSize > 0);
            Assert.Contains("版本", cut.Instance.Matrix.ToString());
        }

        [Fact]
        public void CustomColors_AreAppliedToModulesAndEyes()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.Foreground, "#303133")
                .Add(x => x.EyeColor, "#409eff"));

            Assert.Equal("#303133", cut.Find(".el-qrcode__module").GetAttribute("fill"));

            foreach (var eye in cut.FindAll(".el-qrcode__eye"))
            {
                Assert.Equal("#409eff", eye.GetAttribute("fill"));
            }

            foreach (var pupil in cut.FindAll(".el-qrcode__eye-pupil"))
            {
                Assert.Equal("#409eff", pupil.GetAttribute("fill"));
            }
        }

        [Fact]
        public void PerCornerEyeColors_OverrideSharedEyeColor()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.EyeColor, "#00ff00")
                .Add(x => x.EyeColorTopLeft, "#ff0000")
                .Add(x => x.EyeColorBottomLeft, "#0000ff"));

            Assert.Equal("#ff0000", cut.Find(".el-qrcode__eye[data-eye=\"0\"]").GetAttribute("fill"));
            Assert.Equal("#00ff00", cut.Find(".el-qrcode__eye[data-eye=\"1\"]").GetAttribute("fill"));
            Assert.Equal("#0000ff", cut.Find(".el-qrcode__eye[data-eye=\"2\"]").GetAttribute("fill"));
        }

        [Fact]
        public void Background_Transparent_ProducesNoFill()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.Background, "transparent"));

            var background = cut.Find(".el-qrcode__background");

            Assert.Equal("none", background.GetAttribute("fill"));
            Assert.Equal("0", background.GetAttribute("fill-opacity"));
        }

        [Fact]
        public void EightDigitHexColor_IsSplitIntoFillAndOpacity()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.Foreground, "#33669980"));

            var module = cut.Find(".el-qrcode__module");

            Assert.Equal("#336699", module.GetAttribute("fill"));
            Assert.Equal("0.502", module.GetAttribute("fill-opacity"));
        }

        [Fact]
        public void Size_IsAppliedToWidthAndHeight()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.Size, "180px"));

            var svg = cut.Find("svg");

            Assert.Equal("180px", svg.GetAttribute("width"));
            Assert.Equal("180px", svg.GetAttribute("height"));
        }

        [Fact]
        public void QuietZone_Zero_RemovesMargin()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.QuietZone, 0));

            Assert.Contains("viewBox=\"0 0 21 21\"", cut.Markup);

            // 定位图形紧贴左上角。
            Assert.Contains("M 0 0", cut.Markup);
        }

        [Fact]
        public void DifferentValues_ProduceDifferentSymbols()
        {
            var first = Render<ElQRCodeGrow>(parameters => parameters.Add(x => x.Value, "Hello"));
            var second = Render<ElQRCodeGrow>(parameters => parameters.Add(x => x.Value, "World"));

            Assert.NotEqual(first.Markup, second.Markup);
        }

        [Fact]
        public void HigherEccLevel_ProducesEqualOrLargerSymbol()
        {
            const string payload = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            var low = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, payload)
                .Add(x => x.Ecc, ElQRCodeGrowEcc.Low));

            var high = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, payload)
                .Add(x => x.Ecc, ElQRCodeGrowEcc.High));

            Assert.True(
                high.Instance.SymbolSize >= low.Instance.SymbolSize,
                "更高纠错等级不应产生更小的符号。");
        }

        [Fact]
        public void NumericPayload_UsesSmallerVersionThanByteMode()
        {
            var payload = new string('8', 60);

            var optimized = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, payload)
                .Add(x => x.Ecc, ElQRCodeGrowEcc.Medium));

            var single = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, payload)
                .Add(x => x.Ecc, ElQRCodeGrowEcc.Medium)
                .Add(x => x.OptimizeSegments, false));

            // 60 位数字走数字模式只需 2 版（v1-M 数字容量仅 34 位），
            // 若只能走字节模式则需 4 版，因此符号边长从 25 降到 33 之前的 25。
            Assert.Equal(2, optimized.Instance.Version);
            Assert.Equal(QRCodeGrowMode.Numeric, optimized.Instance.Matrix.Segments[0].Mode);
            Assert.Equal(25, optimized.Instance.SymbolSize);
            Assert.True(optimized.Instance.SymbolSize < 33);

            // 关闭多段优化只是「不拆分」，仍会为纯数字内容选数字模式，版本不变。
            Assert.Equal(optimized.Instance.Version, single.Instance.Version);
        }

        [Fact]
        public void MergeModules_ReducesNumberOfElements()
        {
            const string payload = "https://element-plus.org/zh-CN/component/qrcode";

            var merged = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, payload)
                .Add(x => x.Ecc, ElQRCodeGrowEcc.Quartile));

            var separate = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, payload)
                .Add(x => x.Ecc, ElQRCodeGrowEcc.Quartile)
                .Add(x => x.MergeModules, false));

            var mergedCount = merged.FindAll(".el-qrcode__module").Count;
            var separateCount = separate.FindAll(".el-qrcode__module").Count;

            Assert.True(separateCount > 50);
            Assert.True(mergedCount < separateCount, $"合并后元素数应更少：{mergedCount} vs {separateCount}");
        }

        [Fact]
        public void SquareModules_HaveUnitOrMergedWidth_AndKeepCrispEdges()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.MergeModules, false));

            foreach (var module in cut.FindAll(".el-qrcode__module"))
            {
                Assert.Equal("1", module.GetAttribute("height"));
                Assert.Equal("1", module.GetAttribute("width"));
            }
        }

        [Fact]
        public void CircleModuleShape_RendersCirclesAndSwitchesShapeRendering()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.ModuleShape, QRCodeGrowModuleShape.Circle));

            var circles = cut.FindAll("circle");

            Assert.True(circles.Count > 30, $"圆形模块数量异常：{circles.Count}");
            Assert.Empty(cut.FindAll("rect.el-qrcode__module"));
            Assert.Equal("auto", cut.Find("svg").GetAttribute("shape-rendering"));
        }

        [Fact]
        public void RoundedModuleShape_RendersRoundedRects()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.ModuleShape, QRCodeGrowModuleShape.Rounded));

            var modules = cut.FindAll(".el-qrcode__module");

            Assert.NotEmpty(modules);
            Assert.Equal("0.25", modules[0].GetAttribute("rx"));
        }

        [Fact]
        public void DiamondModuleShape_RendersPaths()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.ModuleShape, QRCodeGrowModuleShape.Diamond));

            var modules = cut.FindAll("path.el-qrcode__module");

            Assert.NotEmpty(modules);
            Assert.Contains("Z", modules[0].GetAttribute("d"));
        }

        [Theory]
        [InlineData(QRCodeGrowEyeShape.Square)]
        [InlineData(QRCodeGrowEyeShape.Rounded)]
        [InlineData(QRCodeGrowEyeShape.Framed)]
        [InlineData(QRCodeGrowEyeShape.Circle)]
        public void EyeShapes_AreRenderedWithExpectedPathCommands(QRCodeGrowEyeShape shape)
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.EyeShape, shape));

            var eye = cut.Find(".el-qrcode__eye");
            var path = eye.GetAttribute("d");

            Assert.False(string.IsNullOrWhiteSpace(path));
            Assert.Equal("evenodd", eye.GetAttribute("fill-rule"));

            if (shape == QRCodeGrowEyeShape.Circle)
            {
                Assert.Contains("a 3.5 3.5", path);
            }
            else if (shape == QRCodeGrowEyeShape.Rounded)
            {
                Assert.Contains("A 1.25 1.25", path);
            }
            else
            {
                Assert.Contains("H", path);
            }
        }

        [Fact]
        public void Logo_RendersCutoutAndImage()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "https://element-plus.org")
                .Add(x => x.Ecc, ElQRCodeGrowEcc.High)
                .Add(x => x.Logo, "data:image/png;base64,AAAA"));

            var image = cut.Find("image");

            Assert.Equal("data:image/png;base64,AAAA", image.GetAttribute("href"));
            Assert.Equal("xMidYMid meet", image.GetAttribute("preserveAspectRatio"));

            var cutout = cut.Find(".el-qrcode__logo-background");
            Assert.Equal("#FFFFFF", cutout.GetAttribute("fill"));

            // 镂空外扩 1 个模块：图片边长 + 2。
            var box = double.Parse(image.GetAttribute("width"), System.Globalization.CultureInfo.InvariantCulture);
            var cutoutSize = double.Parse(cutout.GetAttribute("width"), System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(box + 2, cutoutSize);
        }

        [Fact]
        public void Logo_WithoutLogoParameter_RendersNoImage()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters.Add(x => x.Value, "AB"));

            Assert.Empty(cut.FindAll("image"));
            Assert.Empty(cut.FindAll(".el-qrcode__logo-background"));
        }

        [Fact]
        public void LogoSizePercent_IsScaledAgainstSymbolSize()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.Logo, "logo.png")
                .Add(x => x.LogoSizePercent, 50));

            var image = cut.Find("image");
            var width = double.Parse(image.GetAttribute("width"), System.Globalization.CultureInfo.InvariantCulture);

            // 21 个模块 × 50% = 10.5，四舍五入为 11。
            Assert.Equal(11, width);
        }

        [Fact]
        public void TooLongContent_SetsErrorState()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, new string('a', 2000))
                .Add(x => x.Ecc, ElQRCodeGrowEcc.High));

            Assert.True(cut.Instance.HasError);
            Assert.Contains("过长", cut.Instance.ErrorMessage);
            Assert.Contains("el-qrcode__error", cut.Markup);
            Assert.Contains("is-error", cut.Find("svg").GetAttribute("class"));
            Assert.Equal(0, cut.Instance.Version);
            Assert.Equal(string.Empty, cut.Instance.ToSvg());
        }

        [Fact]
        public void ErrorText_OverridesDefaultMessage()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, new string('a', 2000))
                .Add(x => x.Ecc, ElQRCodeGrowEcc.High)
                .Add(x => x.ErrorText, "内容超出二维码容量"));

            Assert.Contains("内容超出二维码容量", cut.Markup);
        }

        [Fact]
        public void ShowError_False_HidesErrorMessage()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, new string('a', 2000))
                .Add(x => x.Ecc, ElQRCodeGrowEcc.High)
                .Add(x => x.ShowError, false));

            Assert.True(cut.Instance.HasError);
            Assert.DoesNotContain("el-qrcode__error", cut.Markup);
        }

        [Fact]
        public void EncodingError_DoesNotBreakRendering()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, new string('a', 2000))
                .Add(x => x.Ecc, ElQRCodeGrowEcc.High));

            // 仍然渲染出占位方形，页面不会因异常中断。
            Assert.Contains("viewBox=\"0 0 29 29\"", cut.Markup);
        }

        [Fact]
        public void ToSvg_ReturnsStandaloneSvgWithNamespace()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters.Add(x => x.Value, "https://element-plus.org"));

            var svg = cut.Instance.ToSvg();

            Assert.StartsWith("<svg xmlns=\"http://www.w3.org/2000/svg\"", svg);
            Assert.EndsWith("</svg>", svg);
            Assert.Contains("viewBox=\"", svg);
            Assert.DoesNotContain("el-qrcode\"", svg);
            Assert.DoesNotContain("<svg:text", svg);
        }

        [Fact]
        public void ToSvg_ContainsSameContentAsRenderedMarkup()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters.Add(x => x.Value, "AB"));

            var svg = cut.Instance.ToSvg();
            var content = svg.Substring(svg.IndexOf('>') + 1);
            content = content.Substring(0, content.Length - "</svg>".Length);

            Assert.Contains("<rect x=\"0\" y=\"0\"", content);
            Assert.Contains(content, cut.Markup);
        }

        [Fact]
        public async Task DownloadPngAsync_PassesSvgAndOptionsToInterop()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters.Add(x => x.Value, "AB"));

            await cut.Instance.DownloadPngAsync("code.png", 512, 512);

            var invocation = JSInterop.Invocations.Single(x => x.Identifier == "elementSvgDownloadPng");

            Assert.Contains("viewBox=\"0 0 29 29\"", (string)invocation.Arguments[0]);
            Assert.Equal("code.png", invocation.Arguments[1]);

            // 导出尺寸会对齐到「含静区的模块数」整数倍，保证栅格化后每个模块都是整数像素、边缘不发虚，
            // 因此实际像素值会与请求值相差不到一个模块。
            var expected = cut.Instance.ResolveExportSize(512);
            Assert.Equal(0, expected % 29);
            Assert.True(Math.Abs(expected - 512) <= 29, $"导出尺寸 {expected} 应贴近请求值 512。");
            Assert.Equal(expected, invocation.Arguments[2]);
            Assert.Equal(expected, invocation.Arguments[3]);
            Assert.Equal("#FFFFFF", invocation.Arguments[4]);
        }

        [Fact]
        public async Task DownloadPngAsync_DefaultSize_AlignsToModuleGrid()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters.Add(x => x.Value, "AB"));

            await cut.Instance.DownloadPngAsync();

            var invocation = JSInterop.Invocations.Single(x => x.Identifier == "elementSvgDownloadPng");
            var size = (int)invocation.Arguments[2];

            // 29 个模块（含静区）的整数倍，且不小于 256。
            Assert.True(size >= 256);
            Assert.Equal(0, size % 29);
        }

        [Fact]
        public async Task ToPngDataUrlAsync_ReturnsInteropResult()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters.Add(x => x.Value, "AB"));

            JSInterop.Setup<string>("elementSvgToPngDataUrl", _ => true).SetResult("data:image/png;base64,ZZZ");

            var dataUrl = await cut.Instance.ToPngDataUrlAsync(300, 300);

            Assert.Equal("data:image/png;base64,ZZZ", dataUrl);

            var invocation = JSInterop.Invocations.Single(x => x.Identifier == "elementSvgToPngDataUrl");

            // 同 DownloadPngAsync：尺寸对齐到模块整数倍。
            var expected = cut.Instance.ResolveExportSize(300);
            Assert.Equal(0, expected % 29);
            Assert.Equal(expected, invocation.Arguments[1]);
        }

        [Fact]
        public async Task ToPngDataUrlAsync_OnEncodingError_ReturnsEmpty()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, new string('a', 2000))
                .Add(x => x.Ecc, ElQRCodeGrowEcc.High));

            var dataUrl = await cut.Instance.ToPngDataUrlAsync();

            Assert.Equal(string.Empty, dataUrl);
            Assert.DoesNotContain(JSInterop.Invocations, x => x.Identifier == "elementSvgToPngDataUrl");
        }

        [Fact]
        public void ResolveExportSize_UsesIntegerModuleMultiple()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "https://element-plus.org/zh-CN/component/qrcode"));

            var viewBox = (int)cut.Instance.Layout.ViewBoxSize;
            var size = cut.Instance.ResolveExportSize(1000);

            Assert.Equal(0, size % viewBox);
            Assert.InRange(size, 1000 - viewBox, 1000 + viewBox);
        }

        [Fact]
        public void Cls_IsMergedIntoClassAttribute()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.Cls, "report-qrcode"));

            var className = cut.Find("svg").GetAttribute("class");

            Assert.Contains("el-qrcode", className);
            Assert.Contains("report-qrcode", className);
        }

        [Fact]
        public void AriaLabel_IsTruncatedForLongContent()
        {
            var payload = new string('9', 200);

            var cut = Render<ElQRCodeGrow>(parameters => parameters.Add(x => x.Value, payload));

            var label = cut.Find("svg").GetAttribute("aria-label");

            Assert.Equal(64, label.Length);
        }

        [Fact]
        public void EmptyValue_RendersFinderPatternsOnly()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters.Add(x => x.Value, string.Empty));

            Assert.False(cut.Instance.HasError);
            Assert.Equal(1, cut.Instance.Version);
            Assert.Equal("qrcode", cut.Find("svg").GetAttribute("aria-label"));
            Assert.Equal(3, cut.FindAll(".el-qrcode__eye").Count);
        }

        [Fact]
        public void ValueChange_RecomputesSymbol()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters.Add(x => x.Value, "1"));
            var firstSize = cut.Instance.SymbolSize;

            cut.Render(parameters => parameters.Add(x => x.Value, new string('7', 400)));

            Assert.True(cut.Instance.SymbolSize > firstSize);
            Assert.Single(cut.Instance.Matrix.Segments);
            Assert.Equal(QRCodeGrowMode.Numeric, cut.Instance.Matrix.Segments[0].Mode);
            Assert.Equal(400, cut.Instance.Matrix.Segments[0].Length);
        }

        [Fact]
        public void VersionRange_IsRespected()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "1")
                .Add(x => x.MinVersion, 5)
                .Add(x => x.MaxVersion, 5));

            Assert.Equal(5, cut.Instance.Version);
            Assert.Equal(37, cut.Instance.SymbolSize);
        }

        [Fact]
        public void ChineseContent_IsEncodedAndRendered()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "某某市人民医院")
                .Add(x => x.Ecc, ElQRCodeGrowEcc.Medium));

            Assert.False(cut.Instance.HasError);
            Assert.Equal("某某市人民医院", cut.Find("svg").GetAttribute("aria-label"));

            var decoded = QRCodeGrowTestDecoder.Decode(cut.Instance.Matrix.Modules);
            Assert.Equal("某某市人民医院", decoded.Text);
        }

        [Fact]
        public void UseEci_WritesEciIndicator()
        {
            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, "检验单")
                .Add(x => x.UseEci, true));

            var decoded = QRCodeGrowTestDecoder.Decode(cut.Instance.Matrix.Modules);

            Assert.True(decoded.HasUtf8Eci);
            Assert.Equal("检验单", decoded.Text);
        }

        [Fact]
        public void RenderedSymbol_IsDecodableBySpecCompliantReader()
        {
            var payload = "https://element-plus.org/zh-CN/component/qrcode?ref=test";

            var cut = Render<ElQRCodeGrow>(parameters => parameters
                .Add(x => x.Value, payload)
                .Add(x => x.Ecc, ElQRCodeGrowEcc.Quartile));

            var decoded = QRCodeGrowTestDecoder.Decode(cut.Instance.Matrix.Modules);

            Assert.True(decoded.FormatFirstCopyValid);
            Assert.True(decoded.SyndromesAllZero);
            Assert.Equal(payload, decoded.Text);
        }
    }
}
