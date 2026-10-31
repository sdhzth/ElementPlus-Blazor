namespace ElementPlus
{
    /// <summary>
    /// 二维码数据段。描述原始字符串中一段使用同一编码模式的连续字符区间。
    /// </summary>
    public readonly struct QRCodeGrowSegment
    {
        /// <summary>
        /// 初始化数据段。
        /// </summary>
        /// <param name="mode">编码模式。</param>
        /// <param name="start">在原始字符串中的起始字符索引。</param>
        /// <param name="length">字符个数。字节模式下写入位流的是该区间对应子串的 UTF-8 字节数。</param>
        public QRCodeGrowSegment(QRCodeGrowMode mode, int start, int length)
        {
            Mode = mode;
            Start = start;
            Length = length;
        }

        /// <summary>
        /// 编码模式。
        /// </summary>
        public QRCodeGrowMode Mode { get; }

        /// <summary>
        /// 在原始字符串中的起始字符索引。
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// 字符个数。
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// 模式名称（Numeric / Alphanumeric / Byte）。
        /// </summary>
        public string ModeName => Mode switch
        {
            QRCodeGrowMode.Numeric => "Numeric",
            QRCodeGrowMode.Alphanumeric => "Alphanumeric",
            _ => "Byte"
        };

        /// <summary>
        /// 返回形如 <c>Numeric[0,4)</c> 的文本。
        /// </summary>
        /// <returns>数据段文本。</returns>
        public override string ToString() => $"{ModeName}[{Start},{Length})";
    }
}
