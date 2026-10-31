namespace ElementPlus
{
    /// <summary>
    /// 二维码数据段编码模式。枚举值与 QR 规范中的模式指示符一致
    /// （数字 0001、字母数字 0010、字节 0100）。
    /// </summary>
    public enum QRCodeGrowMode
    {
        /// <summary>
        /// 数字模式。字符集 0-9，每 3 个字符用 10 bit，密度最高。
        /// </summary>
        Numeric = 1,

        /// <summary>
        /// 字母数字模式。字符集 0-9、A-Z、空格与 $ % * + - . / :，每 2 个字符用 11 bit。
        /// </summary>
        Alphanumeric = 2,

        /// <summary>
        /// 字节模式。任意数据按 UTF-8 编码，每字节 8 bit，兼容性最好。
        /// </summary>
        Byte = 4
    }
}
