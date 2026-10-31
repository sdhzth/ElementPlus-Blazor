namespace ElementPlus
{
    /// <summary>
    /// 二维码纠错等级。等级越高可恢复的破损比例越大，但同样内容所需的版本（尺寸）也越大。
    /// </summary>
    public enum ElQRCodeGrowEcc
    {
        /// <summary>
        /// L 级，可恢复约 7% 的码字，容量最大。
        /// </summary>
        Low = 0,

        /// <summary>
        /// M 级，可恢复约 15% 的码字。
        /// </summary>
        Medium = 1,

        /// <summary>
        /// Q 级，可恢复约 25% 的码字。默认等级，中心叠加 Logo 时建议至少使用该等级。
        /// </summary>
        Quartile = 2,

        /// <summary>
        /// H 级，可恢复约 30% 的码字，容错最强、容量最小。
        /// </summary>
        High = 3
    }
}
