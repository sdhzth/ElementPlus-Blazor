using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ElementPlus
{
    /// <summary>
    /// 二维码编码器。把文本按 ISO/IEC 18004 编码为二维码模块矩阵。
    /// <para>
    /// 全部算法（模式选择、数据段优化、纠错分块、Reed-Solomon 纠错、模块排布、掩码评估、
    /// 格式与版本信息）均由本库自行实现，不依赖任何第三方二维码库、图形库或浏览器 API，
    /// 因此可在服务端预渲染、单元测试与离线内网环境中直接使用。
    /// </para>
    /// <para>
    /// 与只支持单一字节模式的常见实现相比，本编码器会自动在数字 / 字母数字 / 字节三种模式间
    /// 动态划分数据段，使纯数字内容（如药品监管码、检验单号）所需版本显著减小。
    /// </para>
    /// </summary>
    public static class ElQRCodeGrowEncoder
    {
        /// <summary>
        /// 支持的最小版本。
        /// </summary>
        public const int MinSupportedVersion = 1;

        /// <summary>
        /// 支持的最大版本。
        /// </summary>
        public const int MaxSupportedVersion = 40;

        /// <summary>
        /// 字节段的 ECI 指派值（UTF-8）。
        /// </summary>
        private const int Utf8EciDesignator = 26;

        /// <summary>
        /// 格式信息 BCH(15,5) 生成多项式。
        /// </summary>
        private const int FormatGenerator = 0x537;

        /// <summary>
        /// 格式信息掩码，按规范与 0x5412 异或。
        /// </summary>
        private const int FormatMask = 0x5412;

        /// <summary>
        /// 版本信息 BCH(18,6) 生成多项式。
        /// </summary>
        private const int VersionGenerator = 0x1F25;

        /// <summary>
        /// 数字模式字符集。
        /// </summary>
        private const string NumericCharset = "0123456789";

        /// <summary>
        /// 字母数字模式字符集（索引即编码值）。
        /// </summary>
        private const string AlphanumericCharset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

        // 每版本 4 个纠错等级（L/M/Q/H）× 5 项：
        // { 每块纠错码字数, 第 1 组块数, 第 1 组每块数据码字数, 第 2 组块数, 第 2 组每块数据码字数 }
        // 数据来源：ISO/IEC 18004 表 9（各版本纠错特性）。
        private static readonly int[] EccTable =
        {
            /* v1  */   7,  1,  19,  0,  0,   10,  1,  16,  0,  0,   13,  1,  13,  0,  0,   17,  1,   9,  0,  0,
            /* v2  */  10,  1,  34,  0,  0,   16,  1,  28,  0,  0,   22,  1,  22,  0,  0,   28,  1,  16,  0,  0,
            /* v3  */  15,  1,  55,  0,  0,   26,  1,  44,  0,  0,   18,  2,  17,  0,  0,   22,  2,  13,  0,  0,
            /* v4  */  20,  1,  80,  0,  0,   18,  2,  32,  0,  0,   26,  2,  24,  0,  0,   16,  4,   9,  0,  0,
            /* v5  */  26,  1, 108,  0,  0,   24,  2,  43,  0,  0,   18,  2,  15,  2, 16,   22,  2,  11,  2, 12,
            /* v6  */  18,  2,  68,  0,  0,   16,  4,  27,  0,  0,   24,  4,  19,  0,  0,   28,  4,  15,  0,  0,
            /* v7  */  20,  2,  78,  0,  0,   18,  4,  31,  0,  0,   18,  2,  14,  4, 15,   26,  4,  13,  1, 14,
            /* v8  */  24,  2,  97,  0,  0,   22,  2,  38,  2, 39,   22,  4,  18,  2, 19,   26,  4,  14,  2, 15,
            /* v9  */  30,  2, 116,  0,  0,   22,  3,  36,  2, 37,   20,  4,  16,  4, 17,   24,  4,  12,  4, 13,
            /* v10 */  18,  2,  68,  2, 69,   26,  4,  43,  1, 44,   24,  6,  19,  2, 20,   28,  6,  15,  2, 16,
            /* v11 */  20,  4,  81,  0,  0,   30,  1,  50,  4, 51,   28,  4,  22,  4, 23,   24,  3,  12,  8, 13,
            /* v12 */  24,  2,  92,  2, 93,   22,  6,  36,  2, 37,   26,  4,  20,  6, 21,   28,  7,  14,  4, 15,
            /* v13 */  26,  4, 107,  0,  0,   22,  8,  37,  1, 38,   24,  8,  20,  4, 21,   22, 12,  11,  4, 12,
            /* v14 */  30,  3, 115,  1,116,   24,  4,  40,  5, 41,   20, 11,  16,  5, 17,   24, 11,  12,  5, 13,
            /* v15 */  22,  5,  87,  1, 88,   24,  5,  41,  5, 42,   30,  5,  24,  7, 25,   24, 11,  12,  7, 13,
            /* v16 */  24,  5,  98,  1, 99,   28,  7,  45,  3, 46,   24, 15,  19,  2, 20,   30,  3,  15, 13, 16,
            /* v17 */  28,  1, 107,  5,108,   28, 10,  46,  1, 47,   28,  1,  22, 15, 23,   28,  2,  14, 17, 15,
            /* v18 */  30,  5, 120,  1,121,   26,  9,  43,  4, 44,   28, 17,  22,  1, 23,   28,  2,  14, 19, 15,
            /* v19 */  28,  3, 113,  4,114,   26,  3,  44, 11, 45,   26, 17,  21,  4, 22,   26,  9,  13, 16, 14,
            /* v20 */  28,  3, 107,  5,108,   26,  3,  41, 13, 42,   30, 15,  24,  5, 25,   28, 15,  15, 10, 16,
            /* v21 */  28,  4, 116,  4,117,   26, 17,  42,  0,  0,   28, 17,  22,  6, 23,   30, 19,  16,  6, 17,
            /* v22 */  28,  2, 111,  7,112,   28, 17,  46,  0,  0,   30,  7,  24, 16, 25,   24, 34,  13,  0,  0,
            /* v23 */  30,  4, 121,  5,122,   28,  4,  47, 14, 48,   30, 11,  24, 14, 25,   30, 16,  15, 14, 16,
            /* v24 */  30,  6, 117,  4,118,   28,  6,  45, 14, 46,   30, 11,  24, 16, 25,   30, 30,  16,  2, 17,
            /* v25 */  26,  8, 106,  4,107,   28,  8,  47, 13, 48,   30,  7,  24, 22, 25,   30, 22,  15, 13, 16,
            /* v26 */  28, 10, 114,  2,115,   28, 19,  46,  4, 47,   28, 28,  22,  6, 23,   30, 33,  16,  4, 17,
            /* v27 */  30,  8, 122,  4,123,   28, 22,  45,  3, 46,   30,  8,  23, 26, 24,   30, 12,  15, 28, 16,
            /* v28 */  30,  3, 117, 10,118,   28,  3,  45, 23, 46,   30,  4,  24, 31, 25,   30, 11,  15, 31, 16,
            /* v29 */  30,  7, 116,  7,117,   28, 21,  45,  7, 46,   30,  1,  23, 37, 24,   30, 19,  15, 26, 16,
            /* v30 */  30,  5, 115, 10,116,   28, 19,  47, 10, 48,   30, 15,  24, 25, 25,   30, 23,  15, 25, 16,
            /* v31 */  30, 13, 115,  3,116,   28,  2,  46, 29, 47,   30, 42,  24,  1, 25,   30, 23,  15, 28, 16,
            /* v32 */  30, 17, 115,  0,  0,   28, 10,  46, 23, 47,   30, 10,  24, 35, 25,   30, 19,  15, 35, 16,
            /* v33 */  30, 17, 115,  1,116,   28, 14,  46, 21, 47,   30, 29,  24, 19, 25,   30, 11,  15, 46, 16,
            /* v34 */  30, 13, 115,  6,116,   28, 14,  46, 23, 47,   30, 44,  24,  7, 25,   30, 59,  16,  1, 17,
            /* v35 */  30, 12, 121,  7,122,   28, 12,  47, 26, 48,   30, 39,  24, 14, 25,   30, 22,  15, 41, 16,
            /* v36 */  30,  6, 121, 14,122,   28,  6,  47, 34, 48,   30, 46,  24, 10, 25,   30,  2,  15, 64, 16,
            /* v37 */  30, 17, 122,  4,123,   28, 29,  46, 14, 47,   30, 49,  24, 10, 25,   30, 24,  15, 46, 16,
            /* v38 */  30,  4, 122, 18,123,   28, 13,  46, 32, 47,   30, 48,  24, 14, 25,   30, 42,  15, 32, 16,
            /* v39 */  30, 20, 117,  4,118,   28, 40,  47,  7, 48,   30, 43,  24, 22, 25,   30, 10,  15, 67, 16,
            /* v40 */  30, 19, 118,  6,119,   28, 18,  47, 31, 48,   30, 34,  24, 34, 25,   30, 20,  15, 61, 16
        };

        // 校正图案中心坐标，下标为「版本号 - 1」（v1 无校正图案）。
        private static readonly int[][] AlignmentCenters =
        {
            /* v1  */ Array.Empty<int>(),
            /* v2  */ new[] { 6, 18 },
            /* v3  */ new[] { 6, 22 },
            /* v4  */ new[] { 6, 26 },
            /* v5  */ new[] { 6, 30 },
            /* v6  */ new[] { 6, 34 },
            /* v7  */ new[] { 6, 22, 38 },
            /* v8  */ new[] { 6, 24, 42 },
            /* v9  */ new[] { 6, 26, 46 },
            /* v10 */ new[] { 6, 28, 50 },
            /* v11 */ new[] { 6, 30, 54 },
            /* v12 */ new[] { 6, 32, 58 },
            /* v13 */ new[] { 6, 34, 62 },
            /* v14 */ new[] { 6, 26, 46, 66 },
            /* v15 */ new[] { 6, 26, 48, 70 },
            /* v16 */ new[] { 6, 26, 50, 74 },
            /* v17 */ new[] { 6, 30, 54, 78 },
            /* v18 */ new[] { 6, 30, 56, 82 },
            /* v19 */ new[] { 6, 30, 58, 86 },
            /* v20 */ new[] { 6, 34, 62, 90 },
            /* v21 */ new[] { 6, 28, 50, 72, 94 },
            /* v22 */ new[] { 6, 26, 50, 74, 98 },
            /* v23 */ new[] { 6, 30, 54, 78, 102 },
            /* v24 */ new[] { 6, 28, 54, 80, 106 },
            /* v25 */ new[] { 6, 32, 58, 84, 110 },
            /* v26 */ new[] { 6, 30, 58, 86, 114 },
            /* v27 */ new[] { 6, 34, 62, 90, 118 },
            /* v28 */ new[] { 6, 26, 50, 74, 98, 122 },
            /* v29 */ new[] { 6, 30, 54, 78, 102, 126 },
            /* v30 */ new[] { 6, 26, 52, 78, 104, 130 },
            /* v31 */ new[] { 6, 30, 56, 82, 108, 134 },
            /* v32 */ new[] { 6, 34, 60, 86, 112, 138 },
            /* v33 */ new[] { 6, 30, 58, 86, 114, 142 },
            /* v34 */ new[] { 6, 34, 62, 90, 118, 146 },
            /* v35 */ new[] { 6, 30, 54, 78, 102, 126, 150 },
            /* v36 */ new[] { 6, 24, 50, 76, 102, 128, 154 },
            /* v37 */ new[] { 6, 28, 54, 80, 106, 132, 158 },
            /* v38 */ new[] { 6, 32, 58, 84, 110, 136, 162 },
            /* v39 */ new[] { 6, 26, 54, 82, 110, 138, 166 },
            /* v40 */ new[] { 6, 30, 58, 86, 114, 142, 170 }
        };

        /// <summary>
        /// 符号边长（模块数）。版本 <paramref name="version"/> 的边长为 <c>17 + 4 * version</c>。
        /// </summary>
        /// <param name="version">符号版本（1-40）。</param>
        /// <returns>边长（模块数）。</returns>
        public static int GetSymbolSize(int version) => 17 + 4 * version;

        /// <summary>
        /// 获取指定版本与纠错等级下的分块纠错参数。
        /// </summary>
        /// <param name="version">符号版本（1-40）。</param>
        /// <param name="ecc">纠错等级。</param>
        /// <returns>分块纠错参数。</returns>
        public static QRCodeGrowBlockPlan GetBlockPlan(int version, ElQRCodeGrowEcc ecc)
        {
            if (version < MinSupportedVersion || version > MaxSupportedVersion)
            {
                throw new ArgumentOutOfRangeException(nameof(version), version, "版本必须在 1-40 之间。");
            }

            var offset = ((version - 1) * 4 + (int)ecc) * 5;
            return new QRCodeGrowBlockPlan(
                EccTable[offset],
                EccTable[offset + 1],
                EccTable[offset + 2],
                EccTable[offset + 3],
                EccTable[offset + 4]);
        }

        /// <summary>
        /// 获取指定版本的数据码字总数。
        /// </summary>
        /// <param name="version">符号版本（1-40）。</param>
        /// <param name="ecc">纠错等级。</param>
        /// <returns>数据码字总数。</returns>
        public static int GetDataCodewords(int version, ElQRCodeGrowEcc ecc) => GetBlockPlan(version, ecc).TotalDataCodewords;

        /// <summary>
        /// 获取指定版本的码字总数（数据 + 纠错）。该值与纠错等级无关。
        /// </summary>
        /// <param name="version">符号版本（1-40）。</param>
        /// <returns>码字总数。</returns>
        public static int GetTotalCodewords(int version) => GetBlockPlan(version, ElQRCodeGrowEcc.Low).TotalCodewords;

        /// <summary>
        /// 获取校正图案的中心坐标（v1 返回空数组）。
        /// </summary>
        /// <param name="version">符号版本（1-40）。</param>
        /// <returns>校正图案中心坐标列表。</returns>
        public static IReadOnlyList<int> GetAlignmentCenters(int version)
        {
            if (version < MinSupportedVersion || version > MaxSupportedVersion)
            {
                throw new ArgumentOutOfRangeException(nameof(version), version, "版本必须在 1-40 之间。");
            }

            return AlignmentCenters[version - 1];
        }

        /// <summary>
        /// 计算某一版本与纠错等级下，单一模式下可存放的最大字符数。
        /// 字节模式按 UTF-8 字节数计。
        /// </summary>
        /// <param name="version">符号版本（1-40）。</param>
        /// <param name="ecc">纠错等级。</param>
        /// <param name="mode">编码模式。</param>
        /// <returns>该模式可容纳的最大字符数（或字节数）。</returns>
        public static int GetCapacity(int version, ElQRCodeGrowEcc ecc, QRCodeGrowMode mode)
        {
            var available = GetDataCodewords(version, ecc) * 8 - 4 - GetCountFieldBits(version, mode);

            if (available <= 0)
            {
                return 0;
            }

            switch (mode)
            {
                case QRCodeGrowMode.Numeric:
                    {
                        var groups = available / 10;
                        var rest = available % 10;
                        return groups * 3 + (rest >= 7 ? 2 : rest >= 4 ? 1 : 0);
                    }

                case QRCodeGrowMode.Alphanumeric:
                    {
                        var pairs = available / 11;
                        return pairs * 2 + (available % 11 >= 6 ? 1 : 0);
                    }

                default:
                    return available / 8;
            }
        }

        /// <summary>
        /// 获取指定版本下某模式的字符计数字段位数。
        /// </summary>
        /// <param name="version">符号版本（1-40）。</param>
        /// <param name="mode">编码模式。</param>
        /// <returns>计数字段位数。</returns>
        public static int GetCountFieldBits(int version, QRCodeGrowMode mode)
        {
            switch (mode)
            {
                case QRCodeGrowMode.Numeric:
                    return version <= 9 ? 10 : version <= 26 ? 12 : 14;

                case QRCodeGrowMode.Alphanumeric:
                    return version <= 9 ? 9 : version <= 26 ? 11 : 13;

                default:
                    return version <= 9 ? 8 : 16;
            }
        }

        /// <summary>
        /// 判断字符是否属于数字模式字符集。
        /// </summary>
        /// <param name="value">待判断字符。</param>
        /// <returns>属于时返回 <c>true</c>。</returns>
        public static bool IsNumericChar(char value) => value >= '0' && value <= '9';

        /// <summary>
        /// 判断字符是否属于字母数字模式字符集。
        /// </summary>
        /// <param name="value">待判断字符。</param>
        /// <returns>属于时返回 <c>true</c>。</returns>
        public static bool IsAlphanumericChar(char value) => AlphanumericCharset.IndexOf(value) >= 0;

        /// <summary>
        /// 按字符集判断单个字符最省的编码模式。
        /// </summary>
        /// <param name="value">待判断字符。</param>
        /// <returns>最省的编码模式。</returns>
        public static QRCodeGrowMode ClassifyChar(char value)
        {
            if (IsNumericChar(value))
            {
                return QRCodeGrowMode.Numeric;
            }

            return IsAlphanumericChar(value) ? QRCodeGrowMode.Alphanumeric : QRCodeGrowMode.Byte;
        }

        /// <summary>
        /// 规划数据段。
        /// </summary>
        /// <param name="text">待编码文本。</param>
        /// <param name="version">当前尝试的版本。</param>
        /// <param name="optimize">是否启用多段优化。</param>
        /// <returns>数据段列表；无法在该版本容纳时返回 <c>null</c>。</returns>
        private static List<PlannedSegment> PlanSegments(string text, int version, bool optimize)
        {
            if (text.Length == 0)
            {
                // 空内容仍写入一个长度为 0 的字节段，保证位流结构完整。
                return new List<PlannedSegment>
                {
                    new PlannedSegment(QRCodeGrowMode.Byte, 0, 0, 0)
                };
            }

            var runs = BuildRuns(text);

            if (!optimize || runs.Count == 1)
            {
                var mode = runs[0].Mode;
                for (var i = 1; i < runs.Count && mode != QRCodeGrowMode.Byte; i++)
                {
                    mode = MergeMode(mode, runs[i].Mode);
                }

                var units = 0;
                var length = 0;
                foreach (var run in runs)
                {
                    units += run.Units;
                    length += run.CharLength;
                }

                if (units > MaxCountValue(version, mode))
                {
                    return null;
                }

                return new List<PlannedSegment> { new PlannedSegment(mode, 0, length, units) };
            }

            // 最优划分一定落在游程边界上，因此在游程数量级别上做动态规划即可得到全局最优解。
            var count = runs.Count;
            var best = new long[count + 1];
            var previous = new int[count + 1];
            var chosenMode = new QRCodeGrowMode[count + 1];
            var chosenCharLength = new int[count + 1];
            var chosenUnits = new int[count + 1];

            for (var i = 1; i <= count; i++)
            {
                best[i] = long.MaxValue;
            }

            for (var end = 1; end <= count; end++)
            {
                var mode = runs[end - 1].Mode;
                var units = 0;
                var charLength = 0;

                for (var start = end - 1; start >= 0; start--)
                {
                    mode = MergeMode(mode, runs[start].Mode);
                    units += runs[start].Units;
                    charLength += runs[start].CharLength;

                    if (best[start] == long.MaxValue)
                    {
                        continue;
                    }

                    if (units > MaxCountValue(version, mode))
                    {
                        // 计数位宽有限，再往前合并只会更长，直接结束本层内循环。
                        break;
                    }

                    var cost = best[start] + SegmentBits(mode, units, version);
                    if (cost <= best[end])
                    {
                        best[end] = cost;
                        previous[end] = start;
                        chosenMode[end] = mode;
                        chosenCharLength[end] = charLength;
                        chosenUnits[end] = units;
                    }
                }
            }

            if (best[count] == long.MaxValue)
            {
                return null;
            }

            var segments = new List<PlannedSegment>();
            var cursor = count;
            while (cursor > 0)
            {
                var start = previous[cursor];
                segments.Insert(0, new PlannedSegment(chosenMode[cursor], runs[start].Start, chosenCharLength[cursor], chosenUnits[cursor]));
                cursor = start;
            }

            return segments;
        }

        /// <summary>
        /// 按字符集把文本切分为最省的游程。
        /// </summary>
        /// <param name="text">待编码文本。</param>
        /// <returns>游程列表。</returns>
        private static List<PlannedSegment> BuildRuns(string text)
        {
            var runs = new List<PlannedSegment>();
            var index = 0;

            while (index < text.Length)
            {
                var mode = ClassifyChar(text[index]);
                var end = index + 1;

                while (end < text.Length && ClassifyChar(text[end]) == mode)
                {
                    end++;
                }

                var charLength = end - index;
                var units = mode == QRCodeGrowMode.Byte
                    ? Encoding.UTF8.GetByteCount(text, index, charLength)
                    : charLength;

                runs.Add(new PlannedSegment(mode, index, charLength, units));
                index = end;
            }

            return runs;
        }

        /// <summary>
        /// 合并两个模式：结果必须能同时容纳两种字符集。
        /// </summary>
        /// <param name="left">左侧模式。</param>
        /// <param name="right">右侧模式。</param>
        /// <returns>合并后的模式。</returns>
        private static QRCodeGrowMode MergeMode(QRCodeGrowMode left, QRCodeGrowMode right)
        {
            if (left == right)
            {
                return left;
            }

            if (left == QRCodeGrowMode.Byte || right == QRCodeGrowMode.Byte)
            {
                return QRCodeGrowMode.Byte;
            }

            return QRCodeGrowMode.Alphanumeric;
        }

        /// <summary>
        /// 计数字段可表示的最大值。
        /// </summary>
        /// <param name="version">符号版本。</param>
        /// <param name="mode">编码模式。</param>
        /// <returns>计数字段最大值。</returns>
        private static int MaxCountValue(int version, QRCodeGrowMode mode) => (1 << GetCountFieldBits(version, mode)) - 1;

        /// <summary>
        /// 计算单个数据段占用的位数（含模式指示符与计数字段）。
        /// </summary>
        /// <param name="mode">编码模式。</param>
        /// <param name="units">计数字段值（数字/字母数字为字符数，字节为 UTF-8 字节数）。</param>
        /// <param name="version">符号版本。</param>
        /// <returns>占用的位数。</returns>
        private static long SegmentBits(QRCodeGrowMode mode, int units, int version)
        {
            var header = 4 + GetCountFieldBits(version, mode);

            switch (mode)
            {
                case QRCodeGrowMode.Numeric:
                    return header + 10L * (units / 3) + (units % 3 == 2 ? 7 : units % 3 == 1 ? 4 : 0);

                case QRCodeGrowMode.Alphanumeric:
                    return header + 11L * (units / 2) + 6L * (units % 2);

                default:
                    return header + 8L * units;
            }
        }

        /// <summary>
        /// 数据段规划结果。
        /// </summary>
        private sealed class PlannedSegment
        {
            /// <summary>
            /// 初始化游程。
            /// </summary>
            /// <param name="mode">编码模式。</param>
            /// <param name="start">起始字符索引。</param>
            /// <param name="charLength">字符个数。</param>
            /// <param name="units">计数字段值。</param>
            public PlannedSegment(QRCodeGrowMode mode, int start, int charLength, int units)
            {
                Mode = mode;
                Start = start;
                CharLength = charLength;
                Units = units;
            }

            /// <summary>
            /// 编码模式。
            /// </summary>
            public QRCodeGrowMode Mode { get; }

            /// <summary>
            /// 起始字符索引。
            /// </summary>
            public int Start { get; }

            /// <summary>
            /// 字符个数。
            /// </summary>
            public int CharLength { get; }

            /// <summary>
            /// 计数字段值。
            /// </summary>
            public int Units { get; }
        }

        /// <summary>
        /// 编码入口：把文本编码为二维码符号矩阵。
        /// </summary>
        /// <param name="value">待编码内容。为 <c>null</c> 时按空字符串处理。</param>
        /// <param name="ecc">纠错等级，默认 <see cref="ElQRCodeGrowEcc.Quartile"/>。</param>
        /// <param name="minVersion">允许使用的最小版本，默认 1。</param>
        /// <param name="maxVersion">允许使用的最大版本，默认 40。</param>
        /// <param name="optimizeSegments">
        /// 是否自动在数字 / 字母数字 / 字节模式间划分数据段以压缩符号尺寸，默认 <c>true</c>。
        /// 关闭后整段内容使用同一种模式。
        /// </param>
        /// <param name="useEci">是否在字节段前写入 UTF-8 的 ECI 指示符（指派值 26），默认 <c>false</c>。</param>
        /// <returns>二维码符号矩阵。</returns>
        /// <exception cref="ArgumentException">内容超过指定版本范围所能容纳的容量。</exception>
        public static QRCodeGrowMatrix Encode(
            string value,
            ElQRCodeGrowEcc ecc = ElQRCodeGrowEcc.Quartile,
            int minVersion = MinSupportedVersion,
            int maxVersion = MaxSupportedVersion,
            bool optimizeSegments = true,
            bool useEci = false)
        {
            value ??= string.Empty;

            if (minVersion < MinSupportedVersion || maxVersion > MaxSupportedVersion || minVersion > maxVersion)
            {
                throw new ArgumentOutOfRangeException(nameof(minVersion), $"版本范围必须在 {MinSupportedVersion}-{MaxSupportedVersion} 之间且起始不大于结束。");
            }

            for (var version = minVersion; version <= maxVersion; version++)
            {
                var segments = PlanSegments(value, version, optimizeSegments);
                if (segments == null)
                {
                    continue;
                }

                var dataBits = GetDataCodewords(version, ecc) * 8L;
                var usedBits = useEci && HasByteSegment(segments) ? 12L : 0L;
                var fits = true;

                foreach (var segment in segments)
                {
                    usedBits += SegmentBits(segment.Mode, segment.Units, version);
                    if (usedBits > dataBits)
                    {
                        fits = false;
                        break;
                    }
                }

                if (!fits)
                {
                    continue;
                }

                // 终止符最多 4 位，且允许在容量不足时截短或省略。
                var padded = usedBits + Math.Min(4, dataBits - usedBits);
                padded += (8 - padded % 8) % 8;

                if (padded > dataBits)
                {
                    continue;
                }

                return BuildSymbol(version, ecc, value, segments, useEci);
            }

            throw new ArgumentException(
                $"内容过长：{value.Length} 个字符在 {ecc} 级纠错下超过版本 {maxVersion} 的容量。请缩短内容或降低纠错等级。",
                nameof(value));
        }

        /// <summary>
        /// 生成指定版本的数据码字，便于容量核算与问题排查。
        /// </summary>
        /// <param name="value">待编码内容。</param>
        /// <param name="ecc">纠错等级。</param>
        /// <param name="version">符号版本。</param>
        /// <param name="optimizeSegments">是否启用多段优化。</param>
        /// <param name="useEci">是否写入 UTF-8 的 ECI 指示符。</param>
        /// <returns>数据码字数组。</returns>
        /// <exception cref="ArgumentException">内容无法在该版本容纳。</exception>
        public static byte[] GetDataCodewords(
            string value,
            ElQRCodeGrowEcc ecc,
            int version,
            bool optimizeSegments = true,
            bool useEci = false)
        {
            value ??= string.Empty;

            var segments = PlanSegments(value, version, optimizeSegments)
                ?? throw new ArgumentException($"内容在版本 {version} 的 {ecc} 级纠错下无法容纳。", nameof(value));

            return BuildDataCodewords(value, segments, version, GetDataCodewords(version, ecc), useEci);
        }

        /// <summary>
        /// 生成指定版本的完整码字序列（数据码字 + 交织后的纠错码字），便于与规范示例逐字节比对。
        /// </summary>
        /// <param name="value">待编码内容。</param>
        /// <param name="ecc">纠错等级。</param>
        /// <param name="version">符号版本。</param>
        /// <param name="optimizeSegments">是否启用多段优化。</param>
        /// <param name="useEci">是否写入 UTF-8 的 ECI 指示符。</param>
        /// <returns>交织后的码字序列。</returns>
        /// <exception cref="ArgumentException">内容无法在该版本容纳。</exception>
        public static byte[] GetCodewords(
            string value,
            ElQRCodeGrowEcc ecc,
            int version,
            bool optimizeSegments = true,
            bool useEci = false)
        {
            var plan = GetBlockPlan(version, ecc);
            var data = GetDataCodewords(value, ecc, version, optimizeSegments, useEci);
            return AddErrorCorrection(data, plan);
        }

        /// <summary>
        /// 生成最终符号。
        /// </summary>
        /// <param name="version">符号版本。</param>
        /// <param name="ecc">纠错等级。</param>
        /// <param name="text">原始文本。</param>
        /// <param name="segments">数据段规划结果。</param>
        /// <param name="useEci">是否写入 ECI 指示符。</param>
        /// <returns>二维码符号矩阵。</returns>
        private static QRCodeGrowMatrix BuildSymbol(
            int version,
            ElQRCodeGrowEcc ecc,
            string text,
            List<PlannedSegment> segments,
            bool useEci)
        {
            var plan = GetBlockPlan(version, ecc);
            var dataCodewords = BuildDataCodewords(text, segments, version, plan.TotalDataCodewords, useEci);
            var interleaved = AddErrorCorrection(dataCodewords, plan);

            var modules = BuildSymbolModules(version, ecc, interleaved, out var mask);

            var publicSegments = new QRCodeGrowSegment[segments.Count];
            for (var i = 0; i < segments.Count; i++)
            {
                publicSegments[i] = new QRCodeGrowSegment(segments[i].Mode, segments[i].Start, segments[i].CharLength);
            }

            return new QRCodeGrowMatrix(modules, version, ecc, mask, publicSegments);
        }

        /// <summary>
        /// 判断数据段中是否存在字节段。
        /// </summary>
        /// <param name="segments">数据段列表。</param>
        /// <returns>存在字节段时返回 <c>true</c>。</returns>
        private static bool HasByteSegment(List<PlannedSegment> segments)
        {
            foreach (var segment in segments)
            {
                if (segment.Mode == QRCodeGrowMode.Byte)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 位流缓冲。按位追加，写入时高位在前。
        /// </summary>
        private sealed class BitBuffer
        {
            private readonly List<byte> bytes = new List<byte>(256);
            private int bitLength;

            /// <summary>
            /// 已写入的位数。
            /// </summary>
            public int Length => bitLength;

            /// <summary>
            /// 追加若干位。
            /// </summary>
            /// <param name="value">位值。</param>
            /// <param name="count">位数。</param>
            public void AppendBits(int value, int count)
            {
                for (var i = count - 1; i >= 0; i--)
                {
                    var bit = ((value >> i) & 1) != 0;

                    if ((bitLength & 7) == 0)
                    {
                        bytes.Add(0);
                    }

                    if (bit)
                    {
                        bytes[bytes.Count - 1] |= (byte)(1 << (7 - (bitLength & 7)));
                    }

                    bitLength++;
                }
            }

            /// <summary>
            /// 导出为字节数组，末尾不足 8 位的部分以 0 补齐。
            /// </summary>
            /// <returns>字节数组。</returns>
            public byte[] ToArray() => bytes.ToArray();
        }

        /// <summary>
        /// 生成数据码字：模式指示符、计数字段、数据位，随后补齐终止符与填充码字。
        /// </summary>
        /// <param name="text">原始文本。</param>
        /// <param name="segments">数据段规划结果。</param>
        /// <param name="version">符号版本。</param>
        /// <param name="dataCodewords">数据码字总数。</param>
        /// <param name="useEci">是否写入 ECI 指示符。</param>
        /// <returns>数据码字数组。</returns>
        private static byte[] BuildDataCodewords(
            string text,
            List<PlannedSegment> segments,
            int version,
            int dataCodewords,
            bool useEci)
        {
            var totalBits = dataCodewords * 8;
            var buffer = new BitBuffer();

            if (useEci && HasByteSegment(segments))
            {
                buffer.AppendBits(0b0111, 4);
                buffer.AppendBits(Utf8EciDesignator, 8);
            }

            foreach (var segment in segments)
            {
                switch (segment.Mode)
                {
                    case QRCodeGrowMode.Numeric:
                        WriteNumericSegment(buffer, text, segment, version);
                        break;

                    case QRCodeGrowMode.Alphanumeric:
                        WriteAlphanumericSegment(buffer, text, segment, version);
                        break;

                    default:
                        WriteByteSegment(buffer, text, segment, version);
                        break;
                }
            }

            // 终止符：最多 4 位 0。
            buffer.AppendBits(0, Math.Min(4, Math.Max(0, totalBits - buffer.Length)));

            // 补齐到字节边界。
            if ((buffer.Length & 7) != 0)
            {
                buffer.AppendBits(0, 8 - (buffer.Length & 7));
            }

            var bytes = new List<byte>(buffer.ToArray());

            // 交替填充 0xEC / 0x11。
            for (var i = 0; bytes.Count < dataCodewords; i++)
            {
                bytes.Add((i & 1) == 0 ? (byte)0xEC : (byte)0x11);
            }

            if (bytes.Count > dataCodewords)
            {
                bytes.RemoveRange(dataCodewords, bytes.Count - dataCodewords);
            }

            return bytes.ToArray();
        }

        /// <summary>
        /// 写入数字段数据位：每 3 个字符 10 bit，余 2 字符 7 bit，余 1 字符 4 bit。
        /// </summary>
        /// <param name="buffer">位流缓冲。</param>
        /// <param name="text">原始文本。</param>
        /// <param name="segment">数据段。</param>
        /// <param name="version">符号版本。</param>
        private static void WriteNumericSegment(BitBuffer buffer, string text, PlannedSegment segment, int version)
        {
            buffer.AppendBits((int)QRCodeGrowMode.Numeric, 4);
            buffer.AppendBits(segment.Units, GetCountFieldBits(version, QRCodeGrowMode.Numeric));

            var index = segment.Start;
            var remaining = segment.CharLength;

            while (remaining >= 3)
            {
                var value = (text[index] - '0') * 100 + (text[index + 1] - '0') * 10 + (text[index + 2] - '0');
                buffer.AppendBits(value, 10);
                index += 3;
                remaining -= 3;
            }

            if (remaining == 2)
            {
                buffer.AppendBits((text[index] - '0') * 10 + (text[index + 1] - '0'), 7);
            }
            else if (remaining == 1)
            {
                buffer.AppendBits(text[index] - '0', 4);
            }
        }

        /// <summary>
        /// 写入字母数字段数据位：每 2 个字符 11 bit，余 1 字符 6 bit。
        /// </summary>
        /// <param name="buffer">位流缓冲。</param>
        /// <param name="text">原始文本。</param>
        /// <param name="segment">数据段。</param>
        /// <param name="version">符号版本。</param>
        private static void WriteAlphanumericSegment(BitBuffer buffer, string text, PlannedSegment segment, int version)
        {
            buffer.AppendBits((int)QRCodeGrowMode.Alphanumeric, 4);
            buffer.AppendBits(segment.Units, GetCountFieldBits(version, QRCodeGrowMode.Alphanumeric));

            var index = segment.Start;
            var remaining = segment.CharLength;

            while (remaining >= 2)
            {
                buffer.AppendBits(
                    AlphanumericCharset.IndexOf(text[index]) * 45 + AlphanumericCharset.IndexOf(text[index + 1]),
                    11);
                index += 2;
                remaining -= 2;
            }

            if (remaining == 1)
            {
                buffer.AppendBits(AlphanumericCharset.IndexOf(text[index]), 6);
            }
        }

        /// <summary>
        /// 写入字节段数据位：按 UTF-8 编码，每字节 8 bit。
        /// </summary>
        /// <param name="buffer">位流缓冲。</param>
        /// <param name="text">原始文本。</param>
        /// <param name="segment">数据段。</param>
        /// <param name="version">符号版本。</param>
        private static void WriteByteSegment(BitBuffer buffer, string text, PlannedSegment segment, int version)
        {
            buffer.AppendBits((int)QRCodeGrowMode.Byte, 4);
            buffer.AppendBits(segment.Units, GetCountFieldBits(version, QRCodeGrowMode.Byte));

            var payload = segment.CharLength == 0
                ? Array.Empty<byte>()
                : Encoding.UTF8.GetBytes(text.Substring(segment.Start, segment.CharLength));

            foreach (var value in payload)
            {
                buffer.AppendBits(value, 8);
            }
        }

        #region GF(256) 与 Reed-Solomon 纠错

        private static readonly byte[] GfExp = new byte[512];
        private static readonly byte[] GfLog = new byte[256];
        private static readonly Dictionary<int, byte[]> GeneratorCache = new Dictionary<int, byte[]>();
        private static readonly object GeneratorLock = new object();

        static ElQRCodeGrowEncoder()
        {
            BuildGfTables();
        }

        /// <summary>
        /// 构建 GF(256) 的指数与对数表。本原多项式为 x^8 + x^4 + x^3 + x^2 + 1（0x11D）。
        /// </summary>
        private static void BuildGfTables()
        {
            var value = 1;

            for (var i = 0; i < 255; i++)
            {
                GfExp[i] = (byte)value;
                GfLog[value] = (byte)i;

                value <<= 1;
                if ((value & 0x100) != 0)
                {
                    value ^= 0x11D;
                }
            }

            for (var i = 255; i < GfExp.Length; i++)
            {
                GfExp[i] = GfExp[i - 255];
            }
        }

        /// <summary>
        /// GF(256) 乘法。
        /// </summary>
        /// <param name="left">乘数。</param>
        /// <param name="right">乘数。</param>
        /// <returns>乘积。</returns>
        private static byte GfMultiply(byte left, byte right)
        {
            if (left == 0 || right == 0)
            {
                return 0;
            }

            return GfExp[GfLog[left] + GfLog[right]];
        }

        /// <summary>
        /// 构建生成多项式 (x - α^0)(x - α^1)...(x - α^(ecCount-1))，
        /// 返回去掉首项 1 后的系数（最高次在前）。
        /// </summary>
        /// <param name="ecCount">纠错码字数。</param>
        /// <returns>生成多项式系数。</returns>
        private static byte[] GetGenerator(int ecCount)
        {
            lock (GeneratorLock)
            {
                if (GeneratorCache.TryGetValue(ecCount, out var cached))
                {
                    return cached;
                }

                var poly = new byte[] { 1 };

                for (var i = 0; i < ecCount; i++)
                {
                    var next = new byte[poly.Length + 1];

                    for (var j = 0; j < poly.Length; j++)
                    {
                        next[j] ^= poly[j];
                        next[j + 1] ^= GfMultiply(poly[j], GfExp[i]);
                    }

                    poly = next;
                }

                var generator = new byte[ecCount];
                Array.Copy(poly, 1, generator, 0, ecCount);

                GeneratorCache[ecCount] = generator;
                return generator;
            }
        }

        /// <summary>
        /// 计算 Reed-Solomon 纠错码字（多项式除法取余）。
        /// </summary>
        /// <param name="data">数据码字。</param>
        /// <param name="ecCount">纠错码字数。</param>
        /// <returns>纠错码字数组。</returns>
        private static byte[] ComputeErrorCorrection(byte[] data, int ecCount)
        {
            var generator = GetGenerator(ecCount);
            var remainder = new byte[ecCount];

            foreach (var value in data)
            {
                var factor = (byte)(value ^ remainder[0]);

                if (ecCount > 1)
                {
                    Array.Copy(remainder, 1, remainder, 0, ecCount - 1);
                }

                remainder[ecCount - 1] = 0;

                if (factor == 0)
                {
                    continue;
                }

                for (var i = 0; i < ecCount; i++)
                {
                    remainder[i] ^= GfMultiply(generator[i], factor);
                }
            }

            return remainder;
        }

        /// <summary>
        /// 按分块参数切分数据码字、逐块计算纠错码字，并按规范交织为最终码字序列。
        /// </summary>
        /// <param name="dataCodewords">数据码字。</param>
        /// <param name="plan">分块纠错参数。</param>
        /// <returns>交织后的码字序列。</returns>
        private static byte[] AddErrorCorrection(byte[] dataCodewords, QRCodeGrowBlockPlan plan)
        {
            var blockCount = plan.TotalBlocks;
            var blocks = new byte[blockCount][];
            var corrections = new byte[blockCount][];

            var offset = 0;

            for (var i = 0; i < blockCount; i++)
            {
                var length = i < plan.Group1Blocks ? plan.Group1DataCodewords : plan.Group2DataCodewords;
                var block = new byte[length];
                Array.Copy(dataCodewords, offset, block, 0, length);
                offset += length;

                blocks[i] = block;
                corrections[i] = ComputeErrorCorrection(block, plan.EcPerBlock);
            }

            var result = new byte[dataCodewords.Length + plan.TotalEcCodewords];
            var position = 0;

            var maxDataLength = plan.Group2Blocks > 0 ? plan.Group2DataCodewords : plan.Group1DataCodewords;
            for (var i = 0; i < maxDataLength; i++)
            {
                for (var b = 0; b < blockCount; b++)
                {
                    if (i < blocks[b].Length)
                    {
                        result[position++] = blocks[b][i];
                    }
                }
            }

            for (var i = 0; i < plan.EcPerBlock; i++)
            {
                for (var b = 0; b < blockCount; b++)
                {
                    result[position++] = corrections[b][i];
                }
            }

            return result;
        }

        #endregion
        #region 符号排布

        /// <summary>
        /// 构建基础矩阵：定位图案、分隔符、定时图案、校正图案、固定深色模块，并预留格式与版本信息区。
        /// </summary>
        /// <param name="version">符号版本。</param>
        /// <param name="modules">模块矩阵（输出）。</param>
        /// <param name="reserved">功能模块标记（输出）。<c>true</c> 表示不参与数据排布与掩码。</param>
        private static void BuildBaseMatrix(int version, bool[,] modules, bool[,] reserved)
        {
            var size = GetSymbolSize(version);

            DrawFinder(modules, reserved, 0, 0);
            DrawFinder(modules, reserved, size - 7, 0);
            DrawFinder(modules, reserved, 0, size - 7);

            // 定时图案：第 6 行与第 6 列，从索引 8 起交替。
            for (var i = 8; i < size - 8; i++)
            {
                SetFunction(modules, reserved, 6, i, (i & 1) == 0);
                SetFunction(modules, reserved, i, 6, (i & 1) == 0);
            }

            // 校正图案：跳过与定位图案重叠的三个角。
            var centers = AlignmentCenters[version - 1];
            foreach (var row in centers)
            {
                foreach (var column in centers)
                {
                    var overlapsFinder =
                        (column <= 8 && row <= 8) ||
                        (column >= size - 9 && row <= 8) ||
                        (column <= 8 && row >= size - 9);

                    if (!overlapsFinder)
                    {
                        DrawAlignment(modules, reserved, column, row);
                    }
                }
            }

            // 固定深色模块。
            SetFunction(modules, reserved, size - 8, 8, true);

            ReserveFormatAreas(reserved, size);

            if (version >= 7)
            {
                ReserveVersionAreas(reserved, size);
            }
        }

        /// <summary>
        /// 绘制定位图案（7×7）及其右侧与下方的 1 模块宽分隔符。
        /// </summary>
        /// <param name="modules">模块矩阵。</param>
        /// <param name="reserved">功能模块标记。</param>
        /// <param name="left">左上角列索引。</param>
        /// <param name="top">左上角行索引。</param>
        private static void DrawFinder(bool[,] modules, bool[,] reserved, int left, int top)
        {
            var size = modules.GetLength(0);

            for (var dr = -1; dr <= 7; dr++)
            {
                for (var dc = -1; dc <= 7; dc++)
                {
                    var row = top + dr;
                    var column = left + dc;

                    if (row < 0 || column < 0 || row >= size || column >= size)
                    {
                        continue;
                    }

                    var inside = dr >= 0 && dr < 7 && dc >= 0 && dc < 7;

                    if (inside)
                    {
                        var dark = dr == 0 || dr == 6 || dc == 0 || dc == 6 ||
                                   (dr >= 2 && dr <= 4 && dc >= 2 && dc <= 4);
                        SetFunction(modules, reserved, row, column, dark);
                    }
                    else
                    {
                        // 分隔符为浅色，仅标记为功能模块。
                        reserved[row, column] = true;
                    }
                }
            }
        }

        /// <summary>
        /// 绘制校正图案（5×5，外圈深色、内圈浅色、中心深色）。
        /// </summary>
        /// <param name="modules">模块矩阵。</param>
        /// <param name="reserved">功能模块标记。</param>
        /// <param name="centerColumn">中心列索引。</param>
        /// <param name="centerRow">中心行索引。</param>
        private static void DrawAlignment(bool[,] modules, bool[,] reserved, int centerColumn, int centerRow)
        {
            for (var dr = -2; dr <= 2; dr++)
            {
                for (var dc = -2; dc <= 2; dc++)
                {
                    var dark = Math.Max(Math.Abs(dr), Math.Abs(dc)) != 1;
                    SetFunction(modules, reserved, centerRow + dr, centerColumn + dc, dark);
                }
            }
        }

        /// <summary>
        /// 预留格式信息区域（两份，各 15 个模块）。
        /// </summary>
        /// <param name="reserved">功能模块标记。</param>
        /// <param name="size">符号边长。</param>
        private static void ReserveFormatAreas(bool[,] reserved, int size)
        {
            for (var column = 0; column <= 5; column++)
            {
                reserved[8, column] = true;
            }

            reserved[8, 7] = true;
            reserved[8, 8] = true;
            reserved[7, 8] = true;

            for (var row = 0; row <= 5; row++)
            {
                reserved[row, 8] = true;
            }

            for (var i = 0; i < 7; i++)
            {
                reserved[size - 1 - i, 8] = true;
            }

            for (var i = 0; i < 8; i++)
            {
                reserved[8, size - 8 + i] = true;
            }
        }

        /// <summary>
        /// 预留版本信息区域（v7 及以上，两块共 36 个模块）。
        /// </summary>
        /// <param name="reserved">功能模块标记。</param>
        /// <param name="size">符号边长。</param>
        private static void ReserveVersionAreas(bool[,] reserved, int size)
        {
            for (var row = size - 11; row <= size - 9; row++)
            {
                for (var column = 0; column <= 5; column++)
                {
                    reserved[row, column] = true;
                }
            }

            for (var row = 0; row <= 5; row++)
            {
                for (var column = size - 11; column <= size - 9; column++)
                {
                    reserved[row, column] = true;
                }
            }
        }

        /// <summary>
        /// 写入功能模块并标记为已预留。
        /// </summary>
        /// <param name="modules">模块矩阵。</param>
        /// <param name="reserved">功能模块标记。</param>
        /// <param name="row">行索引。</param>
        /// <param name="column">列索引。</param>
        /// <param name="dark">是否深色。</param>
        private static void SetFunction(bool[,] modules, bool[,] reserved, int row, int column, bool dark)
        {
            modules[row, column] = dark;
            reserved[row, column] = true;
        }

        /// <summary>
        /// 按规范顺序把码字位流排布到矩阵中：
        /// 自右向左两列一组，组内自下而上 / 自上而下交替，跳过第 6 列与功能模块。
        /// </summary>
        /// <param name="modules">模块矩阵。</param>
        /// <param name="reserved">功能模块标记。</param>
        /// <param name="codewords">交织后的码字序列。</param>
        private static void PlaceData(bool[,] modules, bool[,] reserved, byte[] codewords)
        {
            var size = modules.GetLength(0);
            var totalBits = codewords.Length * 8;
            var bitIndex = 0;
            var upward = true;

            for (var column = size - 1; column > 0; column -= 2)
            {
                if (column == 6)
                {
                    // 跳过竖向定时图案所在列。
                    column = 5;
                }

                for (var i = 0; i < size; i++)
                {
                    var row = upward ? size - 1 - i : i;

                    for (var k = 0; k < 2; k++)
                    {
                        var target = column - k;

                        if (reserved[row, target])
                        {
                            continue;
                        }

                        var dark = false;

                        if (bitIndex < totalBits)
                        {
                            dark = ((codewords[bitIndex >> 3] >> (7 - (bitIndex & 7))) & 1) != 0;
                            bitIndex++;
                        }

                        // 剩余位（余数位）保持浅色。
                        modules[row, target] = dark;
                    }
                }

                upward = !upward;
            }
        }

        /// <summary>
        /// 按掩码编号判断某坐标是否取反。
        /// </summary>
        /// <param name="mask">掩码编号（0-7）。</param>
        /// <param name="row">行索引。</param>
        /// <param name="column">列索引。</param>
        /// <returns>需要取反时返回 <c>true</c>。</returns>
        private static bool MaskApplies(int mask, int row, int column)
        {
            switch (mask)
            {
                case 0:
                    return ((row + column) & 1) == 0;

                case 1:
                    return (row & 1) == 0;

                case 2:
                    return column % 3 == 0;

                case 3:
                    return (row + column) % 3 == 0;

                case 4:
                    return ((row / 2) + (column / 3)) % 2 == 0;

                case 5:
                    return (row * column) % 2 + (row * column) % 3 == 0;

                case 6:
                    return ((row * column) % 2 + (row * column) % 3) % 2 == 0;

                case 7:
                    return ((row + column) % 2 + (row * column) % 3) % 2 == 0;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 对非功能模块取反。
        /// </summary>
        /// <param name="modules">模块矩阵。</param>
        /// <param name="reserved">功能模块标记。</param>
        /// <param name="mask">掩码编号。</param>
        private static void ApplyMask(bool[,] modules, bool[,] reserved, int mask)
        {
            var size = modules.GetLength(0);

            for (var row = 0; row < size; row++)
            {
                for (var column = 0; column < size; column++)
                {
                    if (!reserved[row, column] && MaskApplies(mask, row, column))
                    {
                        modules[row, column] = !modules[row, column];
                    }
                }
            }
        }

        /// <summary>
        /// 构建符号矩阵并自动选择最优掩码。
        /// </summary>
        /// <param name="version">符号版本。</param>
        /// <param name="ecc">纠错等级。</param>
        /// <param name="codewords">交织后的码字序列。</param>
        /// <param name="mask">选中的掩码编号（输出）。</param>
        /// <returns>最终模块矩阵。</returns>
        private static bool[,] BuildSymbolModules(int version, ElQRCodeGrowEcc ecc, byte[] codewords, out int mask)
        {
            var size = GetSymbolSize(version);
            var modules = new bool[size, size];
            var reserved = new bool[size, size];

            BuildBaseMatrix(version, modules, reserved);
            PlaceData(modules, reserved, codewords);

            if (version >= 7)
            {
                WriteVersionInfo(modules, reserved, version);
            }

            var bestMask = 0;
            var bestPenalty = long.MaxValue;
            bool[,] bestModules = null;

            for (var candidate = 0; candidate < 8; candidate++)
            {
                var trialModules = (bool[,])modules.Clone();
                var trialReserved = (bool[,])reserved.Clone();

                ApplyMask(trialModules, trialReserved, candidate);

                // 格式信息随掩码变化，评估罚分时一并写入，结果与规范一致。
                WriteFormatInfo(trialModules, trialReserved, ecc, candidate);

                var penalty = ComputePenalty(trialModules);

                if (penalty < bestPenalty)
                {
                    bestPenalty = penalty;
                    bestMask = candidate;
                    bestModules = trialModules;
                }
            }

            mask = bestMask;
            return bestModules ?? modules;
        }

        /// <summary>
        /// 计算掩码罚分（规范 8.8.2 的四条规则，罚分越低越好）。
        /// </summary>
        /// <param name="modules">模块矩阵。</param>
        /// <returns>总罚分。</returns>
        private static long ComputePenalty(bool[,] modules)
        {
            var size = modules.GetLength(0);
            long penalty = 0;

            // 规则 1：同色连续 5 个及以上，长度 n 罚 3 + (n - 5)。
            for (var row = 0; row < size; row++)
            {
                var runLength = 1;

                for (var column = 1; column < size; column++)
                {
                    if (modules[row, column] == modules[row, column - 1])
                    {
                        runLength++;
                    }
                    else
                    {
                        if (runLength >= 5)
                        {
                            penalty += 3 + (runLength - 5);
                        }

                        runLength = 1;
                    }
                }

                if (runLength >= 5)
                {
                    penalty += 3 + (runLength - 5);
                }
            }

            for (var column = 0; column < size; column++)
            {
                var runLength = 1;

                for (var row = 1; row < size; row++)
                {
                    if (modules[row, column] == modules[row - 1, column])
                    {
                        runLength++;
                    }
                    else
                    {
                        if (runLength >= 5)
                        {
                            penalty += 3 + (runLength - 5);
                        }

                        runLength = 1;
                    }
                }

                if (runLength >= 5)
                {
                    penalty += 3 + (runLength - 5);
                }
            }

            // 规则 2：2×2 同色块，每处罚 3。
            for (var row = 0; row < size - 1; row++)
            {
                for (var column = 0; column < size - 1; column++)
                {
                    var value = modules[row, column];

                    if (value == modules[row, column + 1] &&
                        value == modules[row + 1, column] &&
                        value == modules[row + 1, column + 1])
                    {
                        penalty += 3;
                    }
                }
            }

            // 规则 3：出现 1:1:3:1:1 比例且两侧留有 4 个浅色模块，每处罚 40。
            var patternA = new[] { true, false, true, true, true, false, true, false, false, false, false };
            var patternB = new[] { false, false, false, false, true, false, true, true, true, false, true };

            for (var row = 0; row < size; row++)
            {
                for (var column = 0; column <= size - 11; column++)
                {
                    if (MatchesRow(modules, row, column, patternA) || MatchesRow(modules, row, column, patternB))
                    {
                        penalty += 40;
                    }
                }
            }

            for (var column = 0; column < size; column++)
            {
                for (var row = 0; row <= size - 11; row++)
                {
                    if (MatchesColumn(modules, row, column, patternA) || MatchesColumn(modules, row, column, patternB))
                    {
                        penalty += 40;
                    }
                }
            }

            // 规则 4：深色比例偏离 50%，每偏离 5% 罚 10。
            var dark = 0;

            for (var row = 0; row < size; row++)
            {
                for (var column = 0; column < size; column++)
                {
                    if (modules[row, column])
                    {
                        dark++;
                    }
                }
            }

            var total = size * size;
            var percent = dark * 100 / total;
            penalty += Math.Abs(percent - 50) / 5 * 10;

            return penalty;
        }

        /// <summary>
        /// 判断某行指定位置是否匹配给定图案。
        /// </summary>
        /// <param name="modules">模块矩阵。</param>
        /// <param name="row">行索引。</param>
        /// <param name="column">起始列索引。</param>
        /// <param name="pattern">图案。</param>
        /// <returns>匹配时返回 <c>true</c>。</returns>
        private static bool MatchesRow(bool[,] modules, int row, int column, bool[] pattern)
        {
            for (var i = 0; i < pattern.Length; i++)
            {
                if (modules[row, column + i] != pattern[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断某列指定位置是否匹配给定图案。
        /// </summary>
        /// <param name="modules">模块矩阵。</param>
        /// <param name="row">起始行索引。</param>
        /// <param name="column">列索引。</param>
        /// <param name="pattern">图案。</param>
        /// <returns>匹配时返回 <c>true</c>。</returns>
        private static bool MatchesColumn(bool[,] modules, int row, int column, bool[] pattern)
        {
            for (var i = 0; i < pattern.Length; i++)
            {
                if (modules[row + i, column] != pattern[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 写入格式信息（纠错等级 + 掩码编号，BCH(15,5) 编码后与 0x5412 异或），两份副本。
        /// </summary>
        /// <param name="modules">模块矩阵。</param>
        /// <param name="reserved">功能模块标记。</param>
        /// <param name="ecc">纠错等级。</param>
        /// <param name="mask">掩码编号。</param>
        private static void WriteFormatInfo(bool[,] modules, bool[,] reserved, ElQRCodeGrowEcc ecc, int mask)
        {
            // 纠错等级指示位：L=01、M=00、Q=11、H=10。
            var eccBits = ecc switch
            {
                ElQRCodeGrowEcc.Low => 1,
                ElQRCodeGrowEcc.Medium => 0,
                ElQRCodeGrowEcc.Quartile => 3,
                _ => 2
            };

            var format = BchEncode((eccBits << 3) | (mask & 7), FormatGenerator, 15, 5) ^ FormatMask;
            var size = modules.GetLength(0);

            // 位序：format 的最高位为第 0 位。
            bool Bit(int index) => ((format >> (14 - index)) & 1) != 0;

            for (var i = 0; i <= 5; i++)
            {
                SetFunction(modules, reserved, 8, i, Bit(i));
            }

            SetFunction(modules, reserved, 8, 7, Bit(6));
            SetFunction(modules, reserved, 8, 8, Bit(7));
            SetFunction(modules, reserved, 7, 8, Bit(8));

            for (var i = 9; i <= 14; i++)
            {
                SetFunction(modules, reserved, 14 - i, 8, Bit(i));
            }

            for (var i = 0; i <= 6; i++)
            {
                SetFunction(modules, reserved, size - 1 - i, 8, Bit(i));
            }

            for (var i = 7; i <= 14; i++)
            {
                SetFunction(modules, reserved, 8, size - 15 + i, Bit(i));
            }
        }

        /// <summary>
        /// 写入版本信息（v7 及以上，BCH(18,6) 编码），两份副本。
        /// </summary>
        /// <param name="modules">模块矩阵。</param>
        /// <param name="reserved">功能模块标记。</param>
        /// <param name="version">符号版本。</param>
        private static void WriteVersionInfo(bool[,] modules, bool[,] reserved, int version)
        {
            var bits = BchEncode(version, VersionGenerator, 18, 6);
            var size = modules.GetLength(0);

            bool Bit(int index) => ((bits >> index) & 1) != 0;

            // 左下块：3 行 × 6 列，位序为列优先向下。
            for (var column = 0; column < 6; column++)
            {
                for (var row = 0; row < 3; row++)
                {
                    SetFunction(modules, reserved, size - 11 + row, column, Bit(row + column * 3));
                }
            }

            // 右上块：6 行 × 3 列，位序为行优先向右。
            for (var row = 0; row < 6; row++)
            {
                for (var column = 0; column < 3; column++)
                {
                    SetFunction(modules, reserved, row, size - 11 + column, Bit(row * 3 + column));
                }
            }
        }

        /// <summary>
        /// BCH 编码：把 <paramref name="dataBits"/> 位数据左移校验位宽后做多项式取余，
        /// 返回“数据 + 校验位”的完整码字。
        /// </summary>
        /// <param name="data">待编码数据。</param>
        /// <param name="generator">生成多项式。</param>
        /// <param name="totalBits">码字总位数。</param>
        /// <param name="dataBits">数据位数。</param>
        /// <returns>编码结果。</returns>
        private static int BchEncode(int data, int generator, int totalBits, int dataBits)
        {
            var checkBits = totalBits - dataBits;
            var value = data << checkBits;

            for (var i = totalBits - 1; i >= checkBits; i--)
            {
                if (((value >> i) & 1) != 0)
                {
                    value ^= generator << (i - checkBits);
                }
            }

            return (data << checkBits) | (value & ((1 << checkBits) - 1));
        }

        #endregion
        #region 布局与渲染

        /// <summary>
        /// 计算二维码布局：静区尺寸与中心 Logo 几何。
        /// </summary>
        /// <param name="matrix">符号矩阵。</param>
        /// <param name="quietZone">静区宽度（模块数），默认 4（规范推荐值）。</param>
        /// <param name="logo">中心 Logo 的图片地址；为空表示不叠加。</param>
        /// <param name="logoSizePercent">Logo 边长占符号内部边长的百分比，取值被限制在 5-60，默认 20。</param>
        /// <param name="logoPadding">Logo 白色镂空相对图片的外扩边距（模块数），默认 1。</param>
        /// <param name="logoCornerRadius">Logo 镂空圆角半径（模块数），默认 0.75。</param>
        /// <returns>布局结果。</returns>
        public static QRCodeGrowLayout ComputeLayout(
            QRCodeGrowMatrix matrix,
            int quietZone = 4,
            string logo = null,
            double logoSizePercent = 20,
            double logoPadding = 1.0,
            double logoCornerRadius = 0.75)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException(nameof(matrix));
            }

            var hasLogo = !string.IsNullOrWhiteSpace(logo);

            if (!hasLogo)
            {
                return new QRCodeGrowLayout(matrix, quietZone, false, 0, 0, 0);
            }

            var percent = Math.Clamp(logoSizePercent, 5, 60);
            var box = Math.Max(5, Math.Round(matrix.Size * (percent / 100.0), MidpointRounding.AwayFromZero));
            var padding = Math.Max(0, logoPadding);

            return new QRCodeGrowLayout(
                matrix,
                quietZone,
                true,
                box,
                box + 2 * padding,
                Math.Max(0, logoCornerRadius));
        }

        /// <summary>
        /// 编码并计算布局。
        /// </summary>
        /// <param name="value">待编码内容。</param>
        /// <param name="ecc">纠错等级。</param>
        /// <param name="quietZone">静区宽度（模块数）。</param>
        /// <param name="logo">中心 Logo 的图片地址。</param>
        /// <param name="logoSizePercent">Logo 边长占符号内部边长的百分比。</param>
        /// <param name="logoPadding">Logo 白色镂空相对图片的外扩边距（模块数）。</param>
        /// <param name="logoCornerRadius">Logo 镂空圆角半径（模块数）。</param>
        /// <param name="minVersion">允许使用的最小版本。</param>
        /// <param name="maxVersion">允许使用的最大版本。</param>
        /// <param name="optimizeSegments">是否启用多段优化。</param>
        /// <param name="useEci">是否写入 UTF-8 的 ECI 指示符。</param>
        /// <returns>布局结果。</returns>
        public static QRCodeGrowLayout ComputeLayout(
            string value,
            ElQRCodeGrowEcc ecc = ElQRCodeGrowEcc.Quartile,
            int quietZone = 4,
            string logo = null,
            double logoSizePercent = 20,
            double logoPadding = 1.0,
            double logoCornerRadius = 0.75,
            int minVersion = MinSupportedVersion,
            int maxVersion = MaxSupportedVersion,
            bool optimizeSegments = true,
            bool useEci = false)
        {
            var matrix = Encode(value, ecc, minVersion, maxVersion, optimizeSegments, useEci);
            return ComputeLayout(matrix, quietZone, logo, logoSizePercent, logoPadding, logoCornerRadius);
        }

        /// <summary>
        /// 依据模块形状决定 SVG 的 <c>shape-rendering</c>：
        /// 方块（可合并）用 <c>crispEdges</c> 保证边缘锐利，其余形状用 <c>auto</c> 保留抗锯齿。
        /// </summary>
        /// <param name="moduleShape">模块形状。</param>
        /// <param name="mergeModules">是否合并同色相邻模块。</param>
        /// <returns>shape-rendering 取值。</returns>
        public static string ResolveShapeRendering(QRCodeGrowModuleShape moduleShape, bool mergeModules) =>
            moduleShape == QRCodeGrowModuleShape.Square && mergeModules ? "crispEdges" : "auto";

        /// <summary>
        /// 生成二维码的内层 SVG 标记（背景、数据模块、定位图形与中心 Logo），
        /// 不含外层 <c>&lt;svg&gt;</c> 元素。
        /// <para>
        /// <see cref="ElQRCodeGrow"/> 组件与 <see cref="RenderSvg"/> 共用此方法，因此屏幕渲染与导出结果完全一致。
        /// </para>
        /// </summary>
        /// <param name="layout">布局结果。</param>
        /// <param name="foreground">数据模块颜色。</param>
        /// <param name="background">背景色。</param>
        /// <param name="moduleShape">模块形状。</param>
        /// <param name="eyeShape">定位图形形状。</param>
        /// <param name="eyeColor">定位图形统一颜色；为空时取数据模块颜色。</param>
        /// <param name="eyeColorTopLeft">左上定位图形颜色；为空时取 <paramref name="eyeColor"/>。</param>
        /// <param name="eyeColorTopRight">右上定位图形颜色；为空时取 <paramref name="eyeColor"/>。</param>
        /// <param name="eyeColorBottomLeft">左下定位图形颜色；为空时取 <paramref name="eyeColor"/>。</param>
        /// <param name="logo">中心 Logo 的图片地址；为空表示不叠加。</param>
        /// <param name="logoBackground">Logo 镂空底色。</param>
        /// <param name="logoBackgroundOpacity">Logo 镂空底色不透明度（0-1）。</param>
        /// <param name="mergeModules">方块形状下是否把同一行相邻深色模块合并为单个矩形以精简输出。</param>
        /// <returns>内层 SVG 标记。</returns>
        public static string RenderContent(
            QRCodeGrowLayout layout,
            string foreground = "#000000",
            string background = "#FFFFFF",
            QRCodeGrowModuleShape moduleShape = QRCodeGrowModuleShape.Square,
            QRCodeGrowEyeShape eyeShape = QRCodeGrowEyeShape.Square,
            string eyeColor = null,
            string eyeColorTopLeft = null,
            string eyeColorTopRight = null,
            string eyeColorBottomLeft = null,
            string logo = null,
            string logoBackground = "#FFFFFF",
            double logoBackgroundOpacity = 1.0,
            bool mergeModules = true)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            var size = layout.ModuleCount;
            var offset = layout.QuietZone;
            var viewBox = layout.ViewBoxSize;
            var builder = new StringBuilder(size * size * 24 + 512);

            // 背景
            builder.Append($"<rect x=\"0\" y=\"0\" width=\"{Format(viewBox)}\" height=\"{Format(viewBox)}\"");
            builder.Append(BuildFillAttributes(background));
            builder.Append(" class=\"el-qrcode__background\"/>");

            // 数据模块
            AppendModules(builder, layout, BuildFillAttributes(foreground), moduleShape, mergeModules);

            // 定位图形
            var baseEyeFill = eyeColor == null ? BuildFillAttributes(foreground) : BuildFillAttributes(eyeColor);
            var eyeFills = new[]
            {
                eyeColorTopLeft == null ? baseEyeFill : BuildFillAttributes(eyeColorTopLeft),
                eyeColorTopRight == null ? baseEyeFill : BuildFillAttributes(eyeColorTopRight),
                eyeColorBottomLeft == null ? baseEyeFill : BuildFillAttributes(eyeColorBottomLeft)
            };

            AppendEye(builder, offset, offset, eyeShape, eyeFills[0], 0);
            AppendEye(builder, viewBox - offset - 7, offset, eyeShape, eyeFills[1], 1);
            AppendEye(builder, offset, viewBox - offset - 7, eyeShape, eyeFills[2], 2);

            // 中心 Logo
            if (layout.HasLogo && !string.IsNullOrWhiteSpace(logo))
            {
                var cutout = new StringBuilder(160);
                cutout.Append($"<rect x=\"{Format(layout.LogoCutoutX)}\" y=\"{Format(layout.LogoCutoutY)}\"");
                cutout.Append($" width=\"{Format(layout.LogoCutoutSize)}\" height=\"{Format(layout.LogoCutoutSize)}\"");
                cutout.Append($" rx=\"{Format(layout.LogoCornerRadius)}\" ry=\"{Format(layout.LogoCornerRadius)}\"");

                var logoFill = ParseColor(logoBackground, out var logoOpacity);
                cutout.Append($" fill=\"{Escape(logoFill)}\"");
                cutout.Append($" fill-opacity=\"{Format(Math.Clamp(logoBackgroundOpacity, 0, 1) * logoOpacity)}\"");
                cutout.Append(" class=\"el-qrcode__logo-background\"/>");
                builder.Append(cutout);

                builder.Append($"<image x=\"{Format(layout.LogoX)}\" y=\"{Format(layout.LogoY)}\"");
                builder.Append($" width=\"{Format(layout.LogoBoxSize)}\" height=\"{Format(layout.LogoBoxSize)}\"");
                builder.Append($" preserveAspectRatio=\"xMidYMid meet\" href=\"{Escape(logo)}\" class=\"el-qrcode__logo\"/>");
            }

            return builder.ToString();
        }

        /// <summary>
        /// 生成二维码的独立 SVG 字符串。
        /// </summary>
        /// <param name="value">待编码内容。</param>
        /// <param name="ecc">纠错等级。</param>
        /// <param name="quietZone">静区宽度（模块数）。</param>
        /// <param name="foreground">数据模块颜色。</param>
        /// <param name="background">背景色。</param>
        /// <param name="moduleShape">模块形状。</param>
        /// <param name="eyeShape">定位图形形状。</param>
        /// <param name="eyeColor">定位图形统一颜色。</param>
        /// <param name="eyeColorTopLeft">左上定位图形颜色。</param>
        /// <param name="eyeColorTopRight">右上定位图形颜色。</param>
        /// <param name="eyeColorBottomLeft">左下定位图形颜色。</param>
        /// <param name="logo">中心 Logo 的图片地址。</param>
        /// <param name="logoSizePercent">Logo 边长占符号内部边长的百分比。</param>
        /// <param name="logoPadding">Logo 白色镂空相对图片的外扩边距（模块数）。</param>
        /// <param name="logoCornerRadius">Logo 镂空圆角半径（模块数）。</param>
        /// <param name="logoBackground">Logo 镂空底色。</param>
        /// <param name="logoBackgroundOpacity">Logo 镂空底色不透明度（0-1）。</param>
        /// <param name="mergeModules">方块形状下是否合并同色相邻模块。</param>
        /// <param name="minVersion">允许使用的最小版本。</param>
        /// <param name="maxVersion">允许使用的最大版本。</param>
        /// <param name="optimizeSegments">是否启用多段优化。</param>
        /// <param name="useEci">是否写入 UTF-8 的 ECI 指示符。</param>
        /// <returns>SVG 字符串。</returns>
        public static string RenderSvg(
            string value,
            ElQRCodeGrowEcc ecc = ElQRCodeGrowEcc.Quartile,
            int quietZone = 4,
            string foreground = "#000000",
            string background = "#FFFFFF",
            QRCodeGrowModuleShape moduleShape = QRCodeGrowModuleShape.Square,
            QRCodeGrowEyeShape eyeShape = QRCodeGrowEyeShape.Square,
            string eyeColor = null,
            string eyeColorTopLeft = null,
            string eyeColorTopRight = null,
            string eyeColorBottomLeft = null,
            string logo = null,
            double logoSizePercent = 20,
            double logoPadding = 1.0,
            double logoCornerRadius = 0.75,
            string logoBackground = "#FFFFFF",
            double logoBackgroundOpacity = 1.0,
            bool mergeModules = true,
            int minVersion = MinSupportedVersion,
            int maxVersion = MaxSupportedVersion,
            bool optimizeSegments = true,
            bool useEci = false)
        {
            var matrix = Encode(value, ecc, minVersion, maxVersion, optimizeSegments, useEci);
            var layout = ComputeLayout(matrix, quietZone, logo, logoSizePercent, logoPadding, logoCornerRadius);
            var content = RenderContent(
                layout,
                foreground,
                background,
                moduleShape,
                eyeShape,
                eyeColor,
                eyeColorTopLeft,
                eyeColorTopRight,
                eyeColorBottomLeft,
                logo,
                logoBackground,
                logoBackgroundOpacity,
                mergeModules);

            var builder = new StringBuilder(content.Length + 256);
            builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\"");
            builder.Append($" viewBox=\"{layout.ViewBox}\"");
            builder.Append($" width=\"{Format(layout.ViewBoxSize)}\" height=\"{Format(layout.ViewBoxSize)}\"");
            builder.Append($" shape-rendering=\"{ResolveShapeRendering(moduleShape, mergeModules)}\">");
            builder.Append(content);
            builder.Append("</svg>");
            return builder.ToString();
        }

        /// <summary>
        /// 生成数据模块标记。
        /// </summary>
        /// <param name="builder">输出缓冲。</param>
        /// <param name="layout">布局结果。</param>
        /// <param name="fillAttributes">填充属性串（形如 <c> fill="#000"</c>）。</param>
        /// <param name="moduleShape">模块形状。</param>
        /// <param name="mergeModules">方块形状下是否合并同一行相邻深色模块。</param>
        private static void AppendModules(
            StringBuilder builder,
            QRCodeGrowLayout layout,
            string fillAttributes,
            QRCodeGrowModuleShape moduleShape,
            bool mergeModules)
        {
            var size = layout.ModuleCount;
            var offset = layout.QuietZone;

            if (moduleShape == QRCodeGrowModuleShape.Square && mergeModules)
            {
                for (var row = 0; row < size; row++)
                {
                    var runStart = -1;

                    for (var column = 0; column < size; column++)
                    {
                        var drawable = layout.IsDark(row, column) && !QRCodeGrowLayout.IsFinderCell(row, column, size);

                        if (drawable)
                        {
                            if (runStart < 0)
                            {
                                runStart = column;
                            }

                            continue;
                        }

                        if (runStart >= 0)
                        {
                            AppendRect(builder, offset + runStart, offset + row, column - runStart, fillAttributes);
                            runStart = -1;
                        }
                    }

                    if (runStart >= 0)
                    {
                        AppendRect(builder, offset + runStart, offset + row, size - runStart, fillAttributes);
                    }
                }

                return;
            }

            for (var row = 0; row < size; row++)
            {
                for (var column = 0; column < size; column++)
                {
                    if (!layout.IsDark(row, column) || QRCodeGrowLayout.IsFinderCell(row, column, size))
                    {
                        continue;
                    }

                    var x = offset + column;
                    var y = offset + row;

                    switch (moduleShape)
                    {
                        case QRCodeGrowModuleShape.Rounded:
                            builder.Append($"<rect x=\"{Format(x)}\" y=\"{Format(y)}\" width=\"1\" height=\"1\" rx=\"0.25\" ry=\"0.25\"");
                            builder.Append(fillAttributes);
                            builder.Append(" class=\"el-qrcode__module\"/>");
                            break;

                        case QRCodeGrowModuleShape.Circle:
                            builder.Append($"<circle cx=\"{Format(x + 0.5)}\" cy=\"{Format(y + 0.5)}\" r=\"0.5\"");
                            builder.Append(fillAttributes);
                            builder.Append(" class=\"el-qrcode__module\"/>");
                            break;

                        case QRCodeGrowModuleShape.Diamond:
                            builder.Append($"<path d=\"M {Format(x + 0.5)} {Format(y)} L {Format(x + 1)} {Format(y + 0.5)} L {Format(x + 0.5)} {Format(y + 1)} L {Format(x)} {Format(y + 0.5)} Z\"");
                            builder.Append(fillAttributes);
                            builder.Append(" class=\"el-qrcode__module\"/>");
                            break;

                        default:
                            AppendRect(builder, x, y, 1, fillAttributes);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 追加一个高 1 模块的数据模块矩形。
        /// </summary>
        /// <param name="builder">输出缓冲。</param>
        /// <param name="x">左上角 X 坐标。</param>
        /// <param name="y">左上角 Y 坐标。</param>
        /// <param name="width">宽度（模块数）。</param>
        /// <param name="fillAttributes">填充属性串。</param>
        private static void AppendRect(StringBuilder builder, double x, double y, double width, string fillAttributes)
        {
            builder.Append($"<rect x=\"{Format(x)}\" y=\"{Format(y)}\" width=\"{Format(width)}\" height=\"1\"");
            builder.Append(fillAttributes);
            builder.Append(" class=\"el-qrcode__module\"/>");
        }

        /// <summary>
        /// 追加一个定位图形（外环 + 中心点）。
        /// </summary>
        /// <param name="builder">输出缓冲。</param>
        /// <param name="x">左上角 X 坐标。</param>
        /// <param name="y">左上角 Y 坐标。</param>
        /// <param name="shape">定位图形形状。</param>
        /// <param name="fillAttributes">填充属性串。</param>
        /// <param name="index">定位图形序号（0 左上、1 右上、2 左下）。</param>
        private static void AppendEye(
            StringBuilder builder,
            double x,
            double y,
            QRCodeGrowEyeShape shape,
            string fillAttributes,
            int index)
        {
            string ring;
            string pupil;

            switch (shape)
            {
                case QRCodeGrowEyeShape.Rounded:
                    ring = SquareRing(x, y, 1, 1.25, 0.25);
                    pupil = RoundedRectPath(x + 2, y + 2, 3, 3, 0.75);
                    break;

                case QRCodeGrowEyeShape.Framed:
                    ring = SquareRing(x, y, 1.2, 0, 0);
                    pupil = SquarePath(x + 2, y + 2, 3, 3);
                    break;

                case QRCodeGrowEyeShape.Circle:
                    ring = CirclePath(x + 3.5, y + 3.5, 3.5) + " " + CirclePath(x + 3.5, y + 3.5, 2.5);
                    pupil = CirclePath(x + 3.5, y + 3.5, 1.5);
                    break;

                default:
                    ring = SquareRing(x, y, 1, 0, 0);
                    pupil = SquarePath(x + 2, y + 2, 3, 3);
                    break;
            }

            builder.Append($"<path d=\"{ring}\" fill-rule=\"evenodd\"");
            builder.Append(fillAttributes);
            builder.Append($" class=\"el-qrcode__eye\" data-eye=\"{index}\"/>");

            builder.Append($"<path d=\"{pupil}\"");
            builder.Append(fillAttributes);
            builder.Append($" class=\"el-qrcode__eye-pupil\" data-eye=\"{index}\"/>");
        }

        /// <summary>
        /// 生成 7×7 定位图形外环路径：外部轮廓减去内部镂空，配合 evenodd 填充规则形成环形。
        /// </summary>
        /// <param name="x">左上角 X 坐标。</param>
        /// <param name="y">左上角 Y 坐标。</param>
        /// <param name="thickness">外环厚度（模块数）。</param>
        /// <param name="outerRadius">外轮廓圆角半径。</param>
        /// <param name="innerRadius">内镂空圆角半径。</param>
        /// <returns>路径数据。</returns>
        private static string SquareRing(double x, double y, double thickness, double outerRadius, double innerRadius)
        {
            var inner = 7 - 2 * thickness;

            var outer = outerRadius > 0
                ? RoundedRectPath(x, y, 7, 7, outerRadius)
                : SquarePath(x, y, 7, 7);

            var hole = innerRadius > 0
                ? RoundedRectPath(x + thickness, y + thickness, inner, inner, innerRadius)
                : SquarePath(x + thickness, y + thickness, inner, inner);

            return outer + " " + hole;
        }

        /// <summary>
        /// 生成矩形路径。
        /// </summary>
        /// <param name="x">左上角 X 坐标。</param>
        /// <param name="y">左上角 Y 坐标。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <returns>路径数据。</returns>
        private static string SquarePath(double x, double y, double width, double height) =>
            $"M {Format(x)} {Format(y)} H {Format(x + width)} V {Format(y + height)} H {Format(x)} Z";

        /// <summary>
        /// 生成圆角矩形路径。
        /// </summary>
        /// <param name="x">左上角 X 坐标。</param>
        /// <param name="y">左上角 Y 坐标。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="radius">圆角半径。</param>
        /// <returns>路径数据。</returns>
        private static string RoundedRectPath(double x, double y, double width, double height, double radius)
        {
            var r = Math.Max(0, Math.Min(radius, Math.Min(width, height) / 2.0));

            if (r <= 0)
            {
                return SquarePath(x, y, width, height);
            }

            var builder = new StringBuilder(192);
            builder.Append($"M {Format(x + r)} {Format(y)}");
            builder.Append($" H {Format(x + width - r)}");
            builder.Append($" A {Format(r)} {Format(r)} 0 0 1 {Format(x + width)} {Format(y + r)}");
            builder.Append($" V {Format(y + height - r)}");
            builder.Append($" A {Format(r)} {Format(r)} 0 0 1 {Format(x + width - r)} {Format(y + height)}");
            builder.Append($" H {Format(x + r)}");
            builder.Append($" A {Format(r)} {Format(r)} 0 0 1 {Format(x)} {Format(y + height - r)}");
            builder.Append($" V {Format(y + r)}");
            builder.Append($" A {Format(r)} {Format(r)} 0 0 1 {Format(x + r)} {Format(y)}");
            builder.Append(" Z");
            return builder.ToString();
        }

        /// <summary>
        /// 生成圆形路径。
        /// </summary>
        /// <param name="centerX">圆心 X 坐标。</param>
        /// <param name="centerY">圆心 Y 坐标。</param>
        /// <param name="radius">半径。</param>
        /// <returns>路径数据。</returns>
        private static string CirclePath(double centerX, double centerY, double radius) =>
            $"M {Format(centerX - radius)} {Format(centerY)} a {Format(radius)} {Format(radius)} 0 1 0 {Format(2 * radius)} 0 a {Format(radius)} {Format(radius)} 0 1 0 {Format(-2 * radius)} 0 Z";

        /// <summary>
        /// 解析颜色，返回用于 <c>fill</c> 的值，并通过 <paramref name="opacity"/> 输出分离出的不透明度。
        /// <para>
        /// 支持 <c>#RGB</c>、<c>#RGBA</c>、<c>#RRGGBB</c>、<c>#RRGGBBAA</c>、<c>transparent</c>
        /// 以及任意浏览器可识别的颜色写法（原样透传）。
        /// </para>
        /// </summary>
        /// <param name="color">颜色文本。</param>
        /// <param name="opacity">分离出的不透明度（0-1）。</param>
        /// <returns>用于 <c>fill</c> 的颜色值。</returns>
        public static string ParseColor(string color, out double opacity)
        {
            opacity = 1;

            if (string.IsNullOrWhiteSpace(color))
            {
                return "none";
            }

            var text = color.Trim();

            if (text.Equals("transparent", StringComparison.OrdinalIgnoreCase))
            {
                opacity = 0;
                return "none";
            }

            if (text[0] != '#' || !IsHexText(text, 1))
            {
                return text;
            }

            var hex = text.Substring(1);

            switch (hex.Length)
            {
                case 3:
                case 4:
                {
                    var expanded = new StringBuilder(7);
                    expanded.Append('#');

                    for (var i = 0; i < 3; i++)
                    {
                        expanded.Append(hex[i]).Append(hex[i]);
                    }

                    if (hex.Length == 4)
                    {
                        opacity = Convert.ToInt32(new string(hex[3], 2), 16) / 255.0;
                    }

                    return expanded.ToString();
                }

                case 6:
                    return text;

                case 8:
                    opacity = Convert.ToInt32(hex.Substring(6, 2), 16) / 255.0;
                    return "#" + hex.Substring(0, 6);

                default:
                    return text;
            }
        }

        /// <summary>
        /// 判断字符串从指定位置起是否全为十六进制字符。
        /// </summary>
        /// <param name="text">文本。</param>
        /// <param name="start">起始索引。</param>
        /// <returns>全为十六进制字符时返回 <c>true</c>。</returns>
        private static bool IsHexText(string text, int start)
        {
            for (var i = start; i < text.Length; i++)
            {
                if (!Uri.IsHexDigit(text[i]))
                {
                    return false;
                }
            }

            return text.Length > start;
        }

        /// <summary>
        /// 生成形如 <c> fill="#000000"</c> 的属性串，必要时附加 <c>fill-opacity</c>。
        /// </summary>
        /// <param name="color">颜色文本。</param>
        /// <returns>属性串。</returns>
        private static string BuildFillAttributes(string color)
        {
            var fill = ParseColor(color, out var opacity);
            var builder = new StringBuilder(48);
            builder.Append($" fill=\"{Escape(fill)}\"");

            if (opacity < 1)
            {
                builder.Append($" fill-opacity=\"{Format(opacity)}\"");
            }

            return builder.ToString();
        }

        /// <summary>
        /// 转义 XML 特殊字符，避免颜色或图片地址破坏 SVG 结构。
        /// </summary>
        /// <param name="text">待转义文本。</param>
        /// <returns>转义后的文本。</returns>
        public static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(text.Length + 16);

            foreach (var value in text)
            {
                switch (value)
                {
                    case '&':
                        builder.Append("&amp;");
                        break;

                    case '<':
                        builder.Append("&lt;");
                        break;

                    case '>':
                        builder.Append("&gt;");
                        break;

                    case '"':
                        builder.Append("&quot;");
                        break;

                    case '\'':
                        builder.Append("&#39;");
                        break;

                    default:
                        builder.Append(value);
                        break;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// 以不变区域格式化数值，保证 SVG 坐标不受当前区域设置影响。
        /// </summary>
        /// <param name="value">待格式化的数值。</param>
        /// <returns>数值文本。</returns>
        public static string Format(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);

        #endregion
    }
}
