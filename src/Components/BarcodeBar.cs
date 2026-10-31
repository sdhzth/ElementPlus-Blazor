namespace ElementPlus
{
    /// <summary>
    /// 条码渲染中的一个矩形（一个条或一段空白），坐标为 SVG viewBox 用户单位。
    /// </summary>
    public readonly struct BarcodeBar
    {
        /// <summary>
        /// 使用位置与尺寸初始化一个条码矩形。
        /// </summary>
        /// <param name="x">左边界 X 坐标。</param>
        /// <param name="y">上边界 Y 坐标。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        public BarcodeBar(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// 左边界 X 坐标。
        /// </summary>
        public double X { get; }

        /// <summary>
        /// 上边界 Y 坐标。
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// 宽度。
        /// </summary>
        public double Width { get; }

        /// <summary>
        /// 高度。
        /// </summary>
        public double Height { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"BarcodeBar({X}, {Y}, {Width}, {Height})";
        }
    }
}
