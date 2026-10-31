namespace ElementPlus
{
    /// <summary>
    /// 二维码分块纠错参数。描述某个版本与纠错等级下，数据码字如何切分为纠错块。
    /// </summary>
    public readonly struct QRCodeGrowBlockPlan
    {
        /// <summary>
        /// 初始化分块纠错参数。
        /// </summary>
        /// <param name="ecPerBlock">每个纠错块附加的纠错码字数。</param>
        /// <param name="group1Blocks">第 1 组块数。</param>
        /// <param name="group1DataCodewords">第 1 组每块的数据码字数。</param>
        /// <param name="group2Blocks">第 2 组块数（无第 2 组时为 0）。</param>
        /// <param name="group2DataCodewords">第 2 组每块的数据码字数（无第 2 组时为 0）。</param>
        public QRCodeGrowBlockPlan(
            int ecPerBlock,
            int group1Blocks,
            int group1DataCodewords,
            int group2Blocks,
            int group2DataCodewords)
        {
            EcPerBlock = ecPerBlock;
            Group1Blocks = group1Blocks;
            Group1DataCodewords = group1DataCodewords;
            Group2Blocks = group2Blocks;
            Group2DataCodewords = group2DataCodewords;
        }

        /// <summary>
        /// 每个纠错块附加的纠错码字数。
        /// </summary>
        public int EcPerBlock { get; }

        /// <summary>
        /// 第 1 组块数。
        /// </summary>
        public int Group1Blocks { get; }

        /// <summary>
        /// 第 1 组每块的数据码字数。
        /// </summary>
        public int Group1DataCodewords { get; }

        /// <summary>
        /// 第 2 组块数。
        /// </summary>
        public int Group2Blocks { get; }

        /// <summary>
        /// 第 2 组每块的数据码字数。
        /// </summary>
        public int Group2DataCodewords { get; }

        /// <summary>
        /// 纠错块总数。
        /// </summary>
        public int TotalBlocks => Group1Blocks + Group2Blocks;

        /// <summary>
        /// 数据码字总数。
        /// </summary>
        public int TotalDataCodewords => Group1Blocks * Group1DataCodewords + Group2Blocks * Group2DataCodewords;

        /// <summary>
        /// 纠错码字总数。
        /// </summary>
        public int TotalEcCodewords => EcPerBlock * TotalBlocks;

        /// <summary>
        /// 码字总数（数据 + 纠错）。
        /// </summary>
        public int TotalCodewords => TotalDataCodewords + TotalEcCodewords;
    }
}
