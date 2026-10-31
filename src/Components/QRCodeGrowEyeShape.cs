namespace ElementPlus
{
    /// <summary>
    /// 二维码定位图形（三个角上的“回”字形定位点，俗称“眼睛”）的形状。
    /// </summary>
    public enum QRCodeGrowEyeShape
    {
        /// <summary>
        /// 方形。标准外观，是默认形状。
        /// </summary>
        Square = 0,

        /// <summary>
        /// 圆角方环。外观柔和。
        /// </summary>
        Rounded = 1,

        /// <summary>
        /// 加粗方框。外框更厚、内部留白更多。
        /// </summary>
        Framed = 2,

        /// <summary>
        /// 圆环。中心为圆点。
        /// </summary>
        Circle = 3
    }
}
