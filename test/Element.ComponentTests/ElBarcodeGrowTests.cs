using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElBarcodeGrowTests : BunitContext
    {
        public ElBarcodeGrowTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void Code128_Default_RendersBarsBackgroundAndValue()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters.Add(x => x.Value, "ABC"));

            // Code 128 "ABC"：起始符 + 3 字符 + 校验位 = 5 个码字共 55 模块，
            // 停止符 15 模块，左右静区各 10 → 宽度 90；高度 = 条高 50 + 间距 6 + 字号 12 = 68。
            Assert.Contains("viewBox=\"0 0 90 68\"", cut.Markup);

            var bars = cut.FindAll("rect").Where(x => x.GetAttribute("fill") == "#000000").ToList();
            Assert.Equal(19, bars.Count);

            Assert.Contains("el-barcode", cut.Find("svg").GetAttribute("class"));
            Assert.Contains("<text", cut.Markup);
            Assert.DoesNotContain("<svg:text", cut.Markup);
            Assert.Contains("ABC 1", cut.Markup);
        }

        [Fact]
        public void Code39_RendersExpectedViewBox()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.Type, ElBarcodeGrowType.Code39));

            // 4 个字符 × 12 模块 + 3 个字符间隔 = 51，加左右静区 20 → 71。
            Assert.Contains("viewBox=\"0 0 71 68\"", cut.Markup);
            Assert.False(cut.Instance.HasError);
        }

        [Fact]
        public void ShowValue_False_RemovesTextAndShrinksHeight()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.ShowValue, false));

            Assert.Contains("viewBox=\"0 0 79 50\"", cut.Markup);
            Assert.DoesNotContain("<text", cut.Markup);
        }

        [Fact]
        public void ShowChecksum_IsIgnoredWhenValueHidden()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters
                .Add(x => x.Value, "ABC")
                .Add(x => x.ShowValue, false)
                .Add(x => x.ShowChecksum, true));

            Assert.DoesNotContain("<text", cut.Markup);
        }

        [Fact]
        public void QuietZone_ControlsQuietSpaceWidth()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters
                .Add(x => x.Value, "ABC")
                .Add(x => x.QuietZone, 0));

            Assert.Contains("viewBox=\"0 0 70 68\"", cut.Markup);
        }

        [Fact]
        public void BarHeight_ScalesBarsAndViewBox()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.BarHeight, 80)
                .Add(x => x.ShowValue, false));

            var bar = cut.FindAll("rect").First(x => x.GetAttribute("fill") == "#000000");
            Assert.Equal("80", bar.GetAttribute("height"));
            Assert.Contains("viewBox=\"0 0 79 80\"", cut.Markup);
        }

        [Fact]
        public void CustomColors_ApplyToBarsBackgroundAndText()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.Color, "#FF0000")
                .Add(x => x.Background, "#EEEEFF"));

            Assert.Contains("fill=\"#EEEEFF\"", cut.Markup);
            Assert.Contains("fill=\"#FF0000\"", cut.Markup);
            Assert.DoesNotContain("fill=\"#000000\"", cut.Markup);
        }

        [Fact]
        public void CustomDimensions_AndCls_AreApplied()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.Width, "300px")
                .Add(x => x.Height, "100px")
                .Add(x => x.Cls, "my-barcode"));

            var svg = cut.Find("svg");
            Assert.Equal("300px", svg.GetAttribute("width"));
            Assert.Equal("100px", svg.GetAttribute("height"));
            Assert.Contains("my-barcode", svg.GetAttribute("class"));
        }

        [Fact]
        public void ValueStyle_IsAppliedToValueText()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters
                .Add(x => x.Value, "AB")
                .Add(x => x.ValueStyle, "letter-spacing:2px"));

            Assert.Contains("el-barcode__value", cut.Markup);
            Assert.Contains("letter-spacing:2px", cut.Markup);
        }

        [Fact]
        public void AriaLabel_ExposesValue()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters.Add(x => x.Value, "ELEMENT-1"));

            Assert.Equal("ELEMENT-1", cut.Find("svg").GetAttribute("aria-label"));
            Assert.Equal("img", cut.Find("svg").GetAttribute("role"));
        }

        [Fact]
        public void InvalidValue_RendersErrorInsteadOfBars()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters
                .Add(x => x.Value, "12")
                .Add(x => x.Type, ElBarcodeGrowType.Ean13));

            Assert.True(cut.Instance.HasError);
            Assert.NotEmpty(cut.Instance.ErrorMessage);
            Assert.Contains("is-error", cut.Find("svg").GetAttribute("class"));
            Assert.Contains("el-barcode__error", cut.Markup);
            Assert.DoesNotContain(cut.FindAll("rect"), x => x.GetAttribute("fill") == "#000000");
        }

        [Fact]
        public void ErrorText_OverridesEncoderMessage()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters
                .Add(x => x.Value, "中文")
                .Add(x => x.ErrorText, "条码内容无效"));

            Assert.True(cut.Instance.HasError);
            Assert.Contains("条码内容无效", cut.Markup);
            Assert.Contains("el-barcode__error", cut.Markup);
        }

        [Fact]
        public void ShowError_False_HidesErrorText()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters
                .Add(x => x.Value, "12")
                .Add(x => x.Type, ElBarcodeGrowType.Ean13)
                .Add(x => x.ShowError, false));

            Assert.True(cut.Instance.HasError);
            Assert.DoesNotContain("<text", cut.Markup);
        }

        [Theory]
        [InlineData(ElBarcodeGrowType.Code128, "ELEMENT-2026")]
        [InlineData(ElBarcodeGrowType.Code39, "ABC-123")]
        [InlineData(ElBarcodeGrowType.Ean13, "400638133393")]
        [InlineData(ElBarcodeGrowType.Ean8, "9638507")]
        [InlineData(ElBarcodeGrowType.UpcA, "01234567890")]
        [InlineData(ElBarcodeGrowType.Itf, "1234567890")]
        [InlineData(ElBarcodeGrowType.Postnet, "12345")]
        [InlineData(ElBarcodeGrowType.Rm4scc, "EC1A1BB")]
        [InlineData(ElBarcodeGrowType.Codabar, "1234")]
        [InlineData(ElBarcodeGrowType.Pharmacode, "12345")]
        [InlineData(ElBarcodeGrowType.Isbn, "9780132350884")]
        [InlineData(ElBarcodeGrowType.Issn, "12345678")]
        [InlineData(ElBarcodeGrowType.Msi, "12345")]
        [InlineData(ElBarcodeGrowType.Telepen, "HELLO")]
        public void AllSymbologies_ProduceBarsWithoutError(ElBarcodeGrowType type, string value)
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters
                .Add(x => x.Type, type)
                .Add(x => x.Value, value));

            Assert.False(cut.Instance.HasError, cut.Instance.ErrorMessage);
            Assert.NotEmpty(cut.Instance.Layout.Bars);
            Assert.True(cut.Instance.Layout.ViewBoxWidth > 0);
            Assert.NotEmpty(cut.FindAll("rect"));
        }

        [Fact]
        public void HasChecksum_IsReportedPerSymbology()
        {
            Assert.True(ElBarcodeGrow.HasChecksum(ElBarcodeGrowType.Code128));
            Assert.True(ElBarcodeGrow.HasChecksum(ElBarcodeGrowType.Ean13));
            Assert.True(ElBarcodeGrow.HasChecksum(ElBarcodeGrowType.Msi));
            Assert.False(ElBarcodeGrow.HasChecksum(ElBarcodeGrowType.Code39));
            Assert.False(ElBarcodeGrow.HasChecksum(ElBarcodeGrowType.Itf));
            Assert.False(ElBarcodeGrow.HasChecksum(ElBarcodeGrowType.Codabar));
        }

        [Fact]
        public void Ean13_AppendsCheckDigitWhenMissing()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters
                .Add(x => x.Value, "400638133393")
                .Add(x => x.Type, ElBarcodeGrowType.Ean13));

            Assert.False(cut.Instance.HasError);
            Assert.Contains("400638133393 1", cut.Markup);
        }

        [Fact]
        public void Ean13_RejectsInvalidCheckDigit()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters
                .Add(x => x.Value, "4006381333939")
                .Add(x => x.Type, ElBarcodeGrowType.Ean13));

            Assert.True(cut.Instance.HasError);
        }

        [Fact]
        public void Code128_RejectsNonAsciiContent()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters.Add(x => x.Value, "中文"));

            Assert.True(cut.Instance.HasError);
        }

        [Fact]
        public void EmptyValue_RendersPlaceholderWithoutError()
        {
            var cut = Render<ElBarcodeGrow>();

            Assert.False(cut.Instance.HasError);
            Assert.Empty(cut.Instance.Layout.Bars);
            Assert.Contains("viewBox=\"0 0 1 50\"", cut.Markup);
        }

        [Fact]
        public void ToSvg_ReturnsStandaloneMarkupMatchingScreen()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters.Add(x => x.Value, "ABC"));

            var svg = cut.Instance.ToSvg();

            Assert.StartsWith("<svg xmlns=\"http://www.w3.org/2000/svg\"", svg);
            Assert.Contains("viewBox=\"0 0 90 68\"", svg);
            Assert.Contains("width=\"90\"", svg);
            Assert.Contains("ABC 1", svg);
            Assert.EndsWith("</svg>", svg);
        }

        [Fact]
        public void ToSvg_ReturnsEmptyWhenEncodingFails()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters
                .Add(x => x.Value, "12")
                .Add(x => x.Type, ElBarcodeGrowType.Ean13));

            Assert.Equal(string.Empty, cut.Instance.ToSvg());
        }

        [Fact]
        public async Task DownloadPngAsync_PassesSvgAndOptionsToInterop()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters.Add(x => x.Value, "ABC"));

            await cut.Instance.DownloadPngAsync("code.png", 400, 120);

            var invocation = JSInterop.Invocations.Single(x => x.Identifier == "elementBarcodeDownloadPng");

            Assert.Contains("viewBox=\"0 0 90 68\"", (string)invocation.Arguments[0]);
            Assert.Equal("code.png", invocation.Arguments[1]);
            Assert.Equal(400, invocation.Arguments[2]);
            Assert.Equal(120, invocation.Arguments[3]);
        }

        [Fact]
        public async Task DownloadPngAsync_DoesNothingWhenEncodingFails()
        {
            var cut = Render<ElBarcodeGrow>(parameters => parameters
                .Add(x => x.Value, "12")
                .Add(x => x.Type, ElBarcodeGrowType.Ean13));

            await cut.Instance.DownloadPngAsync();

            Assert.DoesNotContain(JSInterop.Invocations, x => x.Identifier == "elementBarcodeDownloadPng");
        }
    }
}
