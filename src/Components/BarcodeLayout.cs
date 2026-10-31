using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// 条码布局结果。包含条几何、viewBox 尺寸与文本排版信息，
    /// 供 <see cref="ElBarcodeGrow"/> 组件渲染与 <see cref="ElBarcodeGrowEncoder.RenderSvg"/> 字符串导出共用。
    /// </summary>
    public readonly struct BarcodeLayout
    {
        /// <summary>
        /// 初始化条码布局结果。
        /// </summary>
        /// <param name="bars">条矩形列表。</param>
        /// <param name="viewBoxWidth">viewBox 宽度。</param>
        /// <param name="viewBoxHeight">viewBox 高度。</param>
        /// <param name="barsHeight">条区域高度。</param>
        /// <param name="textBaseline">值文本的基线 Y 坐标。</param>
        /// <param name="showText">是否显示文本。</param>
        /// <param name="text">实际显示的文本内容。</param>
        /// <param name="checksumText">校验值文本，无校验值时为 <c>null</c>。</param>
        public BarcodeLayout(
            IReadOnlyList<BarcodeBar> bars,
            double viewBoxWidth,
            double viewBoxHeight,
            double barsHeight,
            double textBaseline,
            bool showText,
            string text,
            string checksumText)
        {
            Bars = bars ?? System.Array.Empty<BarcodeBar>();
            ViewBoxWidth = viewBoxWidth;
            ViewBoxHeight = viewBoxHeight;
            BarsHeight = barsHeight;
            TextBaseline = textBaseline;
            ShowText = showText;
            Text = text ?? string.Empty;
            ChecksumText = checksumText;
        }

        /// <summary>
        /// 条矩形列表。
        /// </summary>
        public IReadOnlyList<BarcodeBar> Bars { get; }

        /// <summary>
        /// viewBox 宽度（用户单位）。已含左右静区。
        /// </summary>
        public double ViewBoxWidth { get; }

        /// <summary>
        /// viewBox 高度（用户单位）。已含文本区域。
        /// </summary>
        public double ViewBoxHeight { get; }

        /// <summary>
        /// 条区域高度（用户单位）。
        /// </summary>
        public double BarsHeight { get; }

        /// <summary>
        /// 值文本的基线 Y 坐标。
        /// </summary>
        public double TextBaseline { get; }

        /// <summary>
        /// 是否需要绘制文本（值或校验值任一可见）。
        /// </summary>
        public bool ShowText { get; }

        /// <summary>
        /// 实际显示的文本内容（值、或值加校验值）。
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// 校验值文本。该码制无校验值时或未启用时为 <c>null</c>。
        /// </summary>
        public string ChecksumText { get; }

        /// <summary>
        /// viewBox 属性值，形如 <c>0 0 90 68</c>。
        /// </summary>
        public string ViewBox => $"0 0 {ElBarcodeGrowEncoder.Format(ViewBoxWidth)} {ElBarcodeGrowEncoder.Format(ViewBoxHeight)}";
    }
}
