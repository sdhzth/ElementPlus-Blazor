using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 一维条码组件。把文本编码为条码并以内联 SVG 自渲染，支持 Code 128、Code 39、EAN-13、EAN-8、
    /// UPC-A、ITF、POSTNET、RM4SCC、Codabar、Pharmacode、ISBN、ISSN、MSI、Telepen 共 14 种码制。
    /// <para>
    /// 全部编码算法由本库自行实现，不依赖任何第三方条码库或浏览器画布 API；
    /// 因此可在服务端预渲染、单元测试与离线部署环境中直接使用。
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;ElBarcodeGrow Value="ELEMENT-2026" Type="ElBarcodeGrowType.Code128" Height="80px" /&gt;
    /// </code>
    /// </example>
    public partial class ElBarcodeGrow : ElementComponentBase
    {
        private BarcodeLayout layout = new BarcodeLayout(
            Array.Empty<BarcodeBar>(), 1, 1, 0, 0, false, string.Empty, null);

        private string errorMessage;

        /// <summary>
        /// 待编码的内容。
        /// </summary>
        [Parameter]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 条码码制，默认 <see cref="ElBarcodeGrowType.Code128"/>。
        /// </summary>
        [Parameter]
        public ElBarcodeGrowType Type { get; set; } = ElBarcodeGrowType.Code128;

        /// <summary>
        /// SVG 的渲染宽度，接受 CSS 单位（如 <c>300px</c>、<c>100%</c>）。
        /// </summary>
        [Parameter]
        public string Width { get; set; } = "100%";

        /// <summary>
        /// SVG 的渲染高度，接受 CSS 单位（如 <c>80px</c>）。值文本绘制在该高度之内。
        /// </summary>
        [Parameter]
        public string Height { get; set; } = "80px";

        /// <summary>
        /// 条与值文本的颜色。
        /// </summary>
        [Parameter]
        public string Color { get; set; } = "#000000";

        /// <summary>
        /// 背景色。
        /// </summary>
        [Parameter]
        public string Background { get; set; } = "#FFFFFF";

        /// <summary>
        /// 编码失败时错误提示文本的颜色，默认使用 Element Plus 的 danger 色。
        /// </summary>
        [Parameter]
        public string ErrorColor { get; set; } = "#f56c6c";

        /// <summary>
        /// 左右静区宽度（模块数）。静区是条码可被识读的必要条件，不建议小于 10。
        /// </summary>
        [Parameter]
        public int QuietZone { get; set; } = 10;

        /// <summary>
        /// 条的高度（viewBox 用户单位）。默认 50。
        /// </summary>
        [Parameter]
        public double BarHeight { get; set; } = 50;

        /// <summary>
        /// 是否在条码下方显示原始值。
        /// </summary>
        [Parameter]
        public bool ShowValue { get; set; } = true;

        /// <summary>
        /// 是否在条码下方显示校验值（仅对该码制存在校验值时有效）。值文本隐藏时校验值同样不显示。
        /// </summary>
        [Parameter]
        public bool ShowChecksum { get; set; } = true;

        /// <summary>
        /// 编码失败时是否在图中显示错误提示。
        /// </summary>
        [Parameter]
        public bool ShowError { get; set; } = true;

        /// <summary>
        /// 值文本的字号（viewBox 用户单位）。
        /// </summary>
        [Parameter]
        public double FontSize { get; set; } = 12;

        /// <summary>
        /// 条与值文本之间的间距（viewBox 用户单位）。
        /// </summary>
        [Parameter]
        public double TextMarginTop { get; set; } = 6;

        /// <summary>
        /// 值文本的内联样式，用于覆盖字号、字重、字距等排版细节。
        /// </summary>
        [Parameter]
        public string ValueStyle { get; set; }

        /// <summary>
        /// 自定义错误提示文案。未设置时显示编码器给出的具体原因。
        /// </summary>
        [Parameter]
        public string ErrorText { get; set; }

        /// <summary>
        /// 当前条码的布局结果（条几何、viewBox 尺寸与文本排版信息）。
        /// </summary>
        public BarcodeLayout Layout => layout;

        /// <summary>
        /// 当前内容是否编码失败。
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(errorMessage);

        /// <summary>
        /// 编码失败的原因。编码成功时为 <c>null</c>。
        /// </summary>
        public string ErrorMessage => errorMessage;

        /// <summary>
        /// 组件的根 CSS 类。
        /// </summary>
        protected string BarcodeClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-barcode", Cls)
            .AddIf(HasError, "is-error")
            .ToString();

        /// <summary>
        /// 无障碍标签，便于读屏软件播报条码内容。
        /// </summary>
        protected string AriaLabel => string.IsNullOrEmpty(Value) ? "barcode" : Value;

        /// <summary>
        /// SVG 内部标记（背景、条与文本）。由编码器生成，与 <see cref="ToSvg"/> 共用同一实现。
        /// </summary>
        protected string InnerMarkup => HasError && ShowError
            ? BuildErrorMarkup()
            : ElBarcodeGrowEncoder.RenderContent(Layout, Color, Background, FontSize, ValueStyle);

        private string BuildErrorMarkup()
        {
            var text = string.IsNullOrEmpty(ErrorText) ? errorMessage : ErrorText;
            var baseline = Layout.ViewBoxHeight / 2.0;

            var builder = new StringBuilder(256);
            builder.Append($"<rect x=\"0\" y=\"0\" width=\"{F(Layout.ViewBoxWidth)}\" height=\"{F(Layout.ViewBoxHeight)}\" fill=\"{ElBarcodeGrowEncoder.Escape(Background)}\"/>");
            builder.Append($"<text x=\"50%\" y=\"{F(baseline)}\" text-anchor=\"middle\" class=\"el-barcode__error\" fill=\"{ElBarcodeGrowEncoder.Escape(ErrorColor)}\" font-size=\"{F(FontSize)}\">{ElBarcodeGrowEncoder.Escape(text)}</text>");
            return builder.ToString();
        }

        /// <summary>
        /// 判断指定码制是否具备可显示的校验值。
        /// </summary>
        /// <param name="type">条码码制。</param>
        /// <returns>存在校验值时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public static bool HasChecksum(ElBarcodeGrowType type) => ElBarcodeGrowEncoder.HasChecksum(type);

        /// <summary>
        /// 返回当前条码的独立 SVG 字符串。
        /// <para>
        /// 与屏幕渲染使用同一套布局计算，但不包含组件的 CSS 类与自定义属性，
        /// 可直接保存为 <c>.svg</c> 文件、写入打印模板或嵌入文档。
        /// </para>
        /// </summary>
        /// <returns>SVG 字符串；当前内容编码失败时返回空字符串。</returns>
        public string ToSvg()
        {
            if (HasError)
            {
                return string.Empty;
            }

            return ElBarcodeGrowEncoder.RenderSvg(
                Type, Value, BarHeight, QuietZone, Color, Background, ShowValue, ShowChecksum, FontSize, TextMarginTop, ValueStyle);
        }

        /// <summary>
        /// 把当前条码渲染为 PNG 并在浏览器中下载。
        /// </summary>
        /// <param name="fileName">下载的文件名，默认 <c>barcode.png</c>。</param>
        /// <param name="width">PNG 宽度（像素）。省略时按 SVG 原始宽度乘以设备像素比。</param>
        /// <param name="height">PNG 高度（像素）。省略时按宽度等比换算。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task DownloadPngAsync(string fileName = "barcode.png", int? width = null, int? height = null)
        {
            var svg = ToSvg();
            if (string.IsNullOrEmpty(svg) || JSRuntime == null)
            {
                return;
            }

            await JSRuntime.InvokeVoidAsync("elementBarcodeDownloadPng", svg, fileName, width, height);
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            Rebuild();
        }

        private void Rebuild()
        {
            try
            {
                layout = ElBarcodeGrowEncoder.ComputeLayout(
                    Type, Value, BarHeight, QuietZone, ShowValue, ShowChecksum, FontSize, TextMarginTop);
                errorMessage = null;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;

                // 失败时退化为空布局，保留条高占位，避免页面因异常中断渲染。
                layout = ElBarcodeGrowEncoder.ComputeLayout(
                    Type, string.Empty, BarHeight, QuietZone, false, false, FontSize, TextMarginTop);
            }
        }

        /// <summary>
        /// 以不变区域格式化数值，保证 SVG 坐标不受当前区域设置影响。
        /// </summary>
        /// <param name="value">待格式化的数值。</param>
        /// <returns>数值文本。</returns>
        protected static string F(double value) => ElBarcodeGrowEncoder.Format(value);
    }
}
