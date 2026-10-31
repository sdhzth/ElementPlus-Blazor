using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 二维码组件。把文本按 ISO/IEC 18004 编码为二维码并以内联 SVG 自渲染，
    /// 支持四种纠错等级、多种模块与定位图形形状、中心 Logo 叠加以及 SVG / PNG 导出。
    /// <para>
    /// 全部编码算法（模式选择、数据段优化、纠错分块、Reed-Solomon 纠错、模块排布、掩码评估、
    /// 格式与版本信息）均由本库自行实现，不依赖任何第三方二维码库或图形库，
    /// 因此可在服务端预渲染、单元测试与离线内网环境中直接使用。
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;ElQRCodeGrow Value="https://element-plus.org" Size="160px" /&gt;
    /// &lt;ElQRCodeGrow Value="01234567890123456789" Ecc="ElQRCodeGrowEcc.High"
    ///               Foreground="#1f2d3d" EyeColor="#409eff" /&gt;
    /// </code>
    /// </example>
    public partial class ElQRCodeGrow : ElementComponentBase
    {
        private static readonly QRCodeGrowMatrix PlaceholderMatrix =
            new QRCodeGrowMatrix(new bool[21, 21], 1, ElQRCodeGrowEcc.Quartile, 0, Array.Empty<QRCodeGrowSegment>());

        private QRCodeGrowMatrix matrix;

        private QRCodeGrowLayout layout =
            ElQRCodeGrowEncoder.ComputeLayout(PlaceholderMatrix, 4);

        private string errorMessage;

        /// <summary>
        /// 待编码的内容。支持任意文本，中文等多字节字符按 UTF-8 编码。
        /// </summary>
        [Parameter]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 纠错等级，默认 <see cref="ElQRCodeGrowEcc.Quartile"/>。中心叠加 Logo 时建议使用
        /// <see cref="ElQRCodeGrowEcc.Quartile"/> 或 <see cref="ElQRCodeGrowEcc.High"/>。
        /// </summary>
        [Parameter]
        public ElQRCodeGrowEcc Ecc { get; set; } = ElQRCodeGrowEcc.Quartile;

        /// <summary>
        /// 渲染尺寸（宽高相同），接受 CSS 单位（如 <c>160px</c>、<c>100%</c>）。默认 <c>100%</c>。
        /// </summary>
        [Parameter]
        public string Size { get; set; } = "100%";

        /// <summary>
        /// 数据模块（深色点）颜色。支持 <c>#RGB</c>、<c>#RRGGBB</c>、<c>#RRGGBBAA</c> 及任意 CSS 颜色写法。
        /// </summary>
        [Parameter]
        public string Foreground { get; set; } = "#000000";

        /// <summary>
        /// 背景色。设为 <c>transparent</c> 时输出透明背景。
        /// </summary>
        [Parameter]
        public string Background { get; set; } = "#FFFFFF";

        /// <summary>
        /// 静区宽度（模块数）。静区是识读的必要条件，规范推荐 4。默认 4。
        /// </summary>
        [Parameter]
        public int QuietZone { get; set; } = 4;

        /// <summary>
        /// 数据模块的形状，默认 <see cref="QRCodeGrowModuleShape.Square"/>。
        /// </summary>
        [Parameter]
        public QRCodeGrowModuleShape ModuleShape { get; set; } = QRCodeGrowModuleShape.Square;

        /// <summary>
        /// 定位图形（三个角上的“回”字形定位点）的形状，默认 <see cref="QRCodeGrowEyeShape.Square"/>。
        /// </summary>
        [Parameter]
        public QRCodeGrowEyeShape EyeShape { get; set; } = QRCodeGrowEyeShape.Square;

        /// <summary>
        /// 左上定位图形的形状。未设置时取 <see cref="EyeShape"/>。
        /// </summary>
        [Parameter]
        public QRCodeGrowEyeShape? EyeShapeTopLeft { get; set; }

        /// <summary>
        /// 右上定位图形的形状。未设置时取 <see cref="EyeShape"/>。
        /// </summary>
        [Parameter]
        public QRCodeGrowEyeShape? EyeShapeTopRight { get; set; }

        /// <summary>
        /// 左下定位图形的形状。未设置时取 <see cref="EyeShape"/>。
        /// </summary>
        [Parameter]
        public QRCodeGrowEyeShape? EyeShapeBottomLeft { get; set; }

        /// <summary>
        /// 定位图形的统一颜色。未设置时取 <see cref="Foreground"/>。
        /// </summary>
        [Parameter]
        public string EyeColor { get; set; }

        /// <summary>
        /// 左上定位图形的颜色。未设置时取 <see cref="EyeColor"/>。
        /// </summary>
        [Parameter]
        public string EyeColorTopLeft { get; set; }

        /// <summary>
        /// 右上定位图形的颜色。未设置时取 <see cref="EyeColor"/>。
        /// </summary>
        [Parameter]
        public string EyeColorTopRight { get; set; }

        /// <summary>
        /// 左下定位图形的颜色。未设置时取 <see cref="EyeColor"/>。
        /// </summary>
        [Parameter]
        public string EyeColorBottomLeft { get; set; }

        /// <summary>
        /// 中心 Logo 的图片地址（URL、data URI 或 base64 内容）。
        /// </summary>
        [Parameter]
        public string Logo { get; set; }

        /// <summary>
        /// Logo 边长占符号内部边长的百分比，取值被限制在 5-60。默认 20，建议不超过 25。
        /// </summary>
        [Parameter]
        public double LogoSizePercent { get; set; } = 20;

        /// <summary>
        /// Logo 白色镂空相对图片的外扩边距（模块数），默认 1。
        /// </summary>
        [Parameter]
        public double LogoPadding { get; set; } = 1.0;

        /// <summary>
        /// Logo 镂空的圆角半径（模块数），默认 0.75。
        /// </summary>
        [Parameter]
        public double LogoCornerRadius { get; set; } = 0.75;

        /// <summary>
        /// Logo 镂空底色，默认白色。
        /// </summary>
        [Parameter]
        public string LogoBackground { get; set; } = "#FFFFFF";

        /// <summary>
        /// Logo 镂空底色的不透明度（0-1），默认 1。
        /// </summary>
        [Parameter]
        public double LogoBackgroundOpacity { get; set; } = 1.0;

        /// <summary>
        /// 方块形状下是否把同一行相邻深色模块合并为单个矩形以精简输出，默认 <c>true</c>。
        /// 输出体积可减少一半以上，且不影响外观与识读。
        /// </summary>
        [Parameter]
        public bool MergeModules { get; set; } = true;

        /// <summary>
        /// 是否自动在数字 / 字母数字 / 字节模式间划分数据段以压缩符号尺寸，默认 <c>true</c>。
        /// </summary>
        [Parameter]
        public bool OptimizeSegments { get; set; } = true;

        /// <summary>
        /// 是否在字节段前写入 UTF-8 的 ECI 指示符（指派值 26）。默认 <c>false</c>，
        /// 以获得最好的老旧扫码器兼容性；内容包含中文且扫码器支持 ECI 时可开启。
        /// </summary>
        [Parameter]
        public bool UseEci { get; set; }

        /// <summary>
        /// 允许使用的最小版本（1-40），默认 1。
        /// </summary>
        [Parameter]
        public int MinVersion { get; set; } = ElQRCodeGrowEncoder.MinSupportedVersion;

        /// <summary>
        /// 允许使用的最大版本（1-40），默认 40。
        /// </summary>
        [Parameter]
        public int MaxVersion { get; set; } = ElQRCodeGrowEncoder.MaxSupportedVersion;

        /// <summary>
        /// 编码失败时是否在图中显示错误提示。
        /// </summary>
        [Parameter]
        public bool ShowError { get; set; } = true;

        /// <summary>
        /// 自定义错误提示文案。未设置时显示编码器给出的具体原因。
        /// </summary>
        [Parameter]
        public string ErrorText { get; set; }

        /// <summary>
        /// 错误提示文本的颜色，默认使用 Element Plus 的 danger 色。
        /// </summary>
        [Parameter]
        public string ErrorColor { get; set; } = "#f56c6c";

        /// <summary>
        /// 当前符号矩阵。编码失败时为占位矩阵。
        /// </summary>
        public QRCodeGrowMatrix Matrix => matrix;

        /// <summary>
        /// 当前布局结果（静区尺寸与中心 Logo 几何）。
        /// </summary>
        public QRCodeGrowLayout Layout => layout;

        /// <summary>
        /// 当前内容是否编码失败。
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(errorMessage);

        /// <summary>
        /// 编码失败的原因。编码成功时为 <c>null</c>。
        /// </summary>
        public string ErrorMessage => errorMessage;

        /// <summary>
        /// 当前符号的版本号；编码失败时为 0。
        /// </summary>
        public int Version => HasError ? 0 : matrix?.Version ?? 0;

        /// <summary>
        /// 当前符号边长（模块数，不含静区）；编码失败时为 0。
        /// </summary>
        public int SymbolSize => HasError ? 0 : matrix?.Size ?? 0;

        /// <summary>
        /// 当前符号实际使用的掩码编号；编码失败时为 -1。
        /// </summary>
        public int MaskPattern => HasError ? -1 : matrix?.MaskPattern ?? -1;

        /// <summary>
        /// 组件的根 CSS 类。
        /// </summary>
        protected string QrCodeClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-qrcode", Cls)
            .AddIf(HasError, "is-error")
            .ToString();

        /// <summary>
        /// SVG 的 <c>shape-rendering</c> 取值。
        /// </summary>
        protected string ShapeRendering =>
            ElQRCodeGrowEncoder.ResolveShapeRendering(ModuleShape, MergeModules);

        /// <summary>
        /// 无障碍标签，便于读屏软件播报二维码内容。
        /// </summary>
        protected string AriaLabel
        {
            get
            {
                if (HasError || string.IsNullOrEmpty(Value))
                {
                    return "qrcode";
                }

                return Value.Length > 64 ? Value.Substring(0, 64) : Value;
            }
        }

        /// <summary>
        /// SVG 内部标记（背景、数据模块、定位图形与中心 Logo）。
        /// 由编码器生成，与 <see cref="ToSvg"/> 共用同一实现。
        /// </summary>
        protected string InnerMarkup => HasError && ShowError
            ? BuildErrorMarkup()
            : ElQRCodeGrowEncoder.RenderContent(
                layout,
                Foreground,
                Background,
                ModuleShape,
                EyeShape,
                EyeColor,
                EyeColorTopLeft,
                EyeColorTopRight,
                EyeColorBottomLeft,
                Logo,
                LogoBackground,
                LogoBackgroundOpacity,
                MergeModules);

        private string BuildErrorMarkup()
        {
            var text = string.IsNullOrEmpty(ErrorText) ? errorMessage : ErrorText;
            var side = Layout.ViewBoxSize;

            var builder = new StringBuilder(320);
            builder.Append($"<rect x=\"0\" y=\"0\" width=\"{F(side)}\" height=\"{F(side)}\" fill=\"{ElQRCodeGrowEncoder.Escape(Background)}\" class=\"el-qrcode__background\"/>");
            builder.Append($"<text x=\"50%\" y=\"50%\" text-anchor=\"middle\" dominant-baseline=\"middle\" class=\"el-qrcode__error\" fill=\"{ElQRCodeGrowEncoder.Escape(ErrorColor)}\" font-size=\"4\">{ElQRCodeGrowEncoder.Escape(text)}</text>");
            return builder.ToString();
        }

        /// <summary>
        /// 返回当前二维码的独立 SVG 字符串。
        /// <para>
        /// 与屏幕渲染使用同一份标记，但不包含组件的 CSS 类与自定义属性，
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

            var content = InnerMarkup;
            var builder = new StringBuilder(content.Length + 256);
            builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\"");
            builder.Append($" viewBox=\"{Layout.ViewBox}\"");
            builder.Append($" width=\"{F(Layout.ViewBoxSize)}\" height=\"{F(Layout.ViewBoxSize)}\"");
            builder.Append($" shape-rendering=\"{ShapeRendering}\">");
            builder.Append(content);
            builder.Append("</svg>");
            return builder.ToString();
        }

        /// <summary>
        /// 把当前二维码渲染为 PNG 并在浏览器中下载。
        /// </summary>
        /// <param name="fileName">下载的文件名，默认 <c>qrcode.png</c>。</param>
        /// <param name="width">PNG 宽度（像素）。省略时按 SVG 自身尺寸乘以设备像素比。</param>
        /// <param name="height">PNG 高度（像素）。省略时按宽度等比换算。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task DownloadPngAsync(string fileName = "qrcode.png", int? width = null, int? height = null)
        {
            var svg = ToSvg();
            if (string.IsNullOrEmpty(svg) || JSRuntime == null)
            {
                return;
            }

            var target = ResolveExportSize(width ?? height ?? 0);
            await JSRuntime.InvokeVoidAsync("elementSvgDownloadPng", svg, fileName, target, target, Background);
        }

        /// <summary>
        /// 把当前二维码栅格化为 PNG，并以 data URL 形式返回。
        /// <para>
        /// 便于写入打印模板、报表或直接作为 <c>&lt;img&gt;</c> 的 <c>src</c>，无需落盘。
        /// </para>
        /// </summary>
        /// <param name="width">PNG 宽度（像素）。小于等于 0 时按 SVG 自身尺寸乘以设备像素比。</param>
        /// <param name="height">PNG 高度（像素）。小于等于 0 时按宽度等比换算。</param>
        /// <returns>PNG 的 data URL；编码失败或运行环境不可用时返回空字符串。</returns>
        public async Task<string> ToPngDataUrlAsync(int width = 0, int height = 0)
        {
            var svg = ToSvg();
            if (string.IsNullOrEmpty(svg) || JSRuntime == null)
            {
                return string.Empty;
            }

            var target = ResolveExportSize(width > 0 ? width : height > 0 ? height : 0);
            return await JSRuntime.InvokeAsync<string>("elementSvgToPngDataUrl", svg, target, target, Background);
        }

        /// <summary>
        /// 计算导出用的像素尺寸。未指定时按符号边长自动选择，并把结果对齐到静区后的模块尺寸整数倍，
        /// 保证导出图中每个模块都是整数像素，边缘不会发虚。
        /// </summary>
        /// <param name="requested">调用方指定的目标宽度（像素）。小于等于 0 表示自动。</param>
        /// <returns>导出像素尺寸（宽高相同）。</returns>
        public int ResolveExportSize(int requested = 0)
        {
            var viewBox = (int)Math.Round(Layout.ViewBoxSize);
            if (viewBox <= 0)
            {
                viewBox = 29;
            }

            var target = requested > 0
                ? requested
                : Math.Max(256, (int)Math.Round(Math.Max(SymbolSize, 21) * 12.0));

            var module = Math.Max(1, (int)Math.Round(target / (double)viewBox, MidpointRounding.AwayFromZero));
            return module * viewBox;
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
                matrix = ElQRCodeGrowEncoder.Encode(
                    Value,
                    Ecc,
                    MinVersion,
                    MaxVersion,
                    OptimizeSegments,
                    UseEci);

                layout = ElQRCodeGrowEncoder.ComputeLayout(
                    matrix,
                    QuietZone,
                    Logo,
                    LogoSizePercent,
                    LogoPadding,
                    LogoCornerRadius);

                errorMessage = null;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                matrix = null;

                // 失败时退化为占位布局，保留方形占位区，避免页面因异常中断渲染。
                layout = ElQRCodeGrowEncoder.ComputeLayout(PlaceholderMatrix, QuietZone);
            }
        }

        /// <summary>
        /// 以不变区域格式化数值，保证 SVG 坐标不受当前区域设置影响。
        /// </summary>
        /// <param name="value">待格式化的数值。</param>
        /// <returns>数值文本。</returns>
        protected static string F(double value) => ElQRCodeGrowEncoder.Format(value);
    }
}
