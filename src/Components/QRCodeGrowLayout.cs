using System;
using System.Globalization;

namespace ElementPlus
{
    /// <summary>
    /// 二维码布局结果。包含符号矩阵、静区尺寸与中心 Logo 的几何信息，
    /// 供 <see cref="ElQRCodeGrow"/> 组件渲染与 <see cref="ElQRCodeGrowEncoder.RenderSvg"/> 字符串导出共用。
    /// </summary>
    public sealed class QRCodeGrowLayout
    {
        /// <summary>
        /// 初始化二维码布局结果。
        /// </summary>
        /// <param name="matrix">符号矩阵。</param>
        /// <param name="quietZone">静区宽度（模块数）。</param>
        /// <param name="hasLogo">中心是否叠加 Logo。</param>
        /// <param name="logoBoxSize">Logo 图片边长（模块数）。</param>
        /// <param name="logoCutoutSize">Logo 白色镂空边长（模块数，含内边距）。</param>
        /// <param name="logoCornerRadius">Logo 镂空圆角半径（模块数）。</param>
        public QRCodeGrowLayout(
            QRCodeGrowMatrix matrix,
            int quietZone,
            bool hasLogo,
            double logoBoxSize,
            double logoCutoutSize,
            double logoCornerRadius)
        {
            Matrix = matrix;
            QuietZone = Math.Max(0, quietZone);
            HasLogo = hasLogo;
            LogoBoxSize = logoBoxSize;
            LogoCutoutSize = logoCutoutSize;
            LogoCornerRadius = logoCornerRadius;
            ViewBoxSize = (matrix?.Size ?? 0) + 2 * QuietZone;
        }

        /// <summary>
        /// 符号矩阵。
        /// </summary>
        public QRCodeGrowMatrix Matrix { get; }

        /// <summary>
        /// 静区宽度（模块数）。
        /// </summary>
        public int QuietZone { get; }

        /// <summary>
        /// 中心是否叠加 Logo。
        /// </summary>
        public bool HasLogo { get; }

        /// <summary>
        /// Logo 图片边长（模块数）。
        /// </summary>
        public double LogoBoxSize { get; }

        /// <summary>
        /// Logo 白色镂空边长（模块数，含内边距）。
        /// </summary>
        public double LogoCutoutSize { get; }

        /// <summary>
        /// Logo 镂空圆角半径（模块数）。
        /// </summary>
        public double LogoCornerRadius { get; }

        /// <summary>
        /// viewBox 边长（含静区，用户单位）。
        /// </summary>
        public double ViewBoxSize { get; }

        /// <summary>
        /// 矩阵边长（模块数，不含静区）。
        /// </summary>
        public int ModuleCount => Matrix?.Size ?? 0;

        /// <summary>
        /// Logo 图片左上角 X 坐标。
        /// </summary>
        public double LogoX => (ViewBoxSize - LogoBoxSize) / 2.0;

        /// <summary>
        /// Logo 图片左上角 Y 坐标。
        /// </summary>
        public double LogoY => (ViewBoxSize - LogoBoxSize) / 2.0;

        /// <summary>
        /// Logo 镂空左上角 X 坐标。
        /// </summary>
        public double LogoCutoutX => (ViewBoxSize - LogoCutoutSize) / 2.0;

        /// <summary>
        /// Logo 镂空左上角 Y 坐标。
        /// </summary>
        public double LogoCutoutY => (ViewBoxSize - LogoCutoutSize) / 2.0;

        /// <summary>
        /// viewBox 属性值，形如 <c>0 0 29 29</c>。
        /// </summary>
        public string ViewBox => $"0 0 {Format(ViewBoxSize)} {Format(ViewBoxSize)}";

        /// <summary>
        /// 读取指定行列的模块明暗值。
        /// </summary>
        /// <param name="row">行索引。</param>
        /// <param name="column">列索引。</param>
        /// <returns>深色返回 <c>true</c>。</returns>
        public bool IsDark(int row, int column) => Matrix != null && Matrix.IsDark(row, column);

        /// <summary>
        /// 判断指定模块是否位于三个定位图案（7×7）之内。这些模块由独立的定位图形绘制。
        /// </summary>
        /// <param name="row">行索引。</param>
        /// <param name="column">列索引。</param>
        /// <returns>属于定位图案时返回 <c>true</c>。</returns>
        public bool IsFinderCell(int row, int column) => IsFinderCell(row, column, ModuleCount);

        /// <summary>
        /// 判断指定模块是否位于三个定位图案（7×7）之内。
        /// </summary>
        /// <param name="row">行索引。</param>
        /// <param name="column">列索引。</param>
        /// <param name="size">符号边长。</param>
        /// <returns>属于定位图案时返回 <c>true</c>。</returns>
        public static bool IsFinderCell(int row, int column, int size)
        {
            var topLeft = row < 7 && column < 7;
            var topRight = row < 7 && column >= size - 7;
            var bottomLeft = row >= size - 7 && column < 7;

            return topLeft || topRight || bottomLeft;
        }

        /// <summary>
        /// 以不变区域格式化数值，保证 SVG 坐标不受当前区域设置影响。
        /// </summary>
        /// <param name="value">待格式化的数值。</param>
        /// <returns>数值文本。</returns>
        public static string Format(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);
    }
}
