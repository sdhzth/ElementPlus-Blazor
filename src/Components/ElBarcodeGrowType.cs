namespace ElementPlus
{
    /// <summary>
    /// 条码码制。ElBarcodeGrow 支持的一维条码类型。
    /// </summary>
    public enum ElBarcodeGrowType
    {
        /// <summary>
        /// Code 128（B 子集）。编码 ASCII 32..127，密度高、通用性最好，是默认码制。
        /// </summary>
        Code128 = 0,

        /// <summary>
        /// Code 39。支持大写字母、数字与 - . $ / + % 和空格，无需校验位。
        /// </summary>
        Code39 = 1,

        /// <summary>
        /// EAN-13（GTIN-13）。13 位商品条码，末位为校验位。
        /// </summary>
        Ean13 = 2,

        /// <summary>
        /// EAN-8（GTIN-8）。8 位短商品条码，末位为校验位。
        /// </summary>
        Ean8 = 3,

        /// <summary>
        /// UPC-A。12 位北美商品条码，末位为校验位。
        /// </summary>
        UpcA = 4,

        /// <summary>
        /// ITF（Interleaved 2 of 5，交叉二五码）。纯数字，位数必须为偶数（奇数自动前补 0）。
        /// </summary>
        Itf = 5,

        /// <summary>
        /// POSTNET（美国邮政）。5 / 9 / 11 位数字，使用半高条与全高条。
        /// </summary>
        Postnet = 6,

        /// <summary>
        /// RM4SCC（英国皇家邮政四状态码）。字母数字，使用升序/降序/全高/跟踪条。
        /// </summary>
        Rm4scc = 7,

        /// <summary>
        /// Codabar。支持数字与 - $ . / : +，首尾需为 A/B/C/D 起止符（缺失时自动补 A...B）。
        /// </summary>
        Codabar = 8,

        /// <summary>
        /// Pharmacode（单轨药监码）。取值 3..131070。
        /// </summary>
        Pharmacode = 9,

        /// <summary>
        /// ISBN。以 EAN-13 的 978/979 前缀编码，支持 10 位或 13 位输入。
        /// </summary>
        Isbn = 10,

        /// <summary>
        /// ISSN。以 EAN-13 的 977 前缀编码，8 位输入。
        /// </summary>
        Issn = 11,

        /// <summary>
        /// MSI（Modified Plessey）。纯数字，使用 Mod 10（Luhn）校验位。
        /// </summary>
        Msi = 12,

        /// <summary>
        /// Telepen。仅支持 ASCII，使用偶校验与 mod 127 校验值。
        /// </summary>
        Telepen = 13
    }
}
