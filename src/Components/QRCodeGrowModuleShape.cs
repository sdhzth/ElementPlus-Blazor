namespace ElementPlus
{
    /// <summary>
    /// 二维码数据模块（深色点）的形状。
    /// </summary>
    public enum QRCodeGrowModuleShape
    {
        /// <summary>
        /// 方块。标准外观，识读率最高，是默认形状。
        /// </summary>
        Square = 0,

        /// <summary>
        /// 圆角方块。外观柔和，仍保持较高识读率。
        /// </summary>
        Rounded = 1,

        /// <summary>
        /// 圆形点阵。外观最柔和，识读率略低于方块。
        /// </summary>
        Circle = 2,

        /// <summary>
        /// 菱形。装饰性最强，大尺寸下识读良好。
        /// </summary>
        Diamond = 3
    }
}
