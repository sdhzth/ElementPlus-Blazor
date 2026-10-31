using System;
using System.Collections.Generic;
using System.Text;
using ElementPlus;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 测试专用二维码解码器。
    /// <para>
    /// 按 ISO/IEC 18004 规定的读取顺序独立实现：校验功能图形、读取两份格式信息并做 BCH 校验、
    /// 按掩码还原、按 Z 字形读取码字、反交织、用 Reed-Solomon 伴随式验证纠错码字、
    /// 最后解析位流还原原始文本。编码器只要在任一环节偏离规范，本解码器都会断言失败。
    /// </para>
    /// </summary>
    internal static class QRCodeGrowTestDecoder
    {
        /// <summary>
        /// 解码结果。
        /// </summary>
        internal sealed class Result
        {
            /// <summary>符号版本。</summary>
            public int Version { get; set; }

            /// <summary>符号边长。</summary>
            public int Size { get; set; }

            /// <summary>从格式信息读出的纠错等级。</summary>
            public ElQRCodeGrowEcc Ecc { get; set; }

            /// <summary>从格式信息读出的掩码编号。</summary>
            public int Mask { get; set; }

            /// <summary>第一份格式信息是否为合法的 BCH 码字。</summary>
            public bool FormatFirstCopyValid { get; set; }

            /// <summary>两份格式信息是否一致。</summary>
            public bool FormatCopiesMatch { get; set; }

            /// <summary>所有纠错块的 Reed-Solomon 伴随式是否全为零。</summary>
            public bool SyndromesAllZero { get; set; }

            /// <summary>是否出现 UTF-8 的 ECI 指示符。</summary>
            public bool HasUtf8Eci { get; set; }

            /// <summary>解析出的数据段。</summary>
            public List<SegmentResult> Segments { get; } = new List<SegmentResult>();

            /// <summary>还原出的文本。</summary>
            public string Text { get; set; }
        }

        /// <summary>
        /// 解析出的数据段。
        /// </summary>
        internal sealed class SegmentResult
        {
            /// <summary>编码模式。</summary>
            public QRCodeGrowMode Mode { get; set; }

            /// <summary>该段还原出的文本。</summary>
            public string Payload { get; set; }
        }

        /// <summary>
        /// 解码给定模块矩阵。
        /// </summary>
        /// <param name="modules">模块矩阵，<c>true</c> 表示深色。</param>
        /// <returns>解码结果。</returns>
        public static Result Decode(bool[,] modules)
        {
            var size = modules.GetLength(0);

            if (size < 21 || (size - 17) % 4 != 0)
            {
                throw new InvalidOperationException($"非法符号尺寸：{size}");
            }

            var version = (size - 17) / 4;
            AssertFunctionPatterns(modules, size);

            var first = ReadFormatCopyFirst(modules);
            var second = ReadFormatCopySecond(modules);
            var (ecc, mask, valid) = ParseFormat(first);

            var result = new Result
            {
                Version = version,
                Size = size,
                Ecc = ecc,
                Mask = mask,
                FormatFirstCopyValid = valid,
                FormatCopiesMatch = first == second
            };

            var reserved = BuildReservedMap(version, size);
            var codewords = ReadCodewords(modules, reserved, mask);

            var plan = ElQRCodeGrowEncoder.GetBlockPlan(version, ecc);
            var data = DeinterleaveAndVerify(codewords, plan, out var syndromesZero);
            result.SyndromesAllZero = syndromesZero;

            ParseBitStream(data, version, result);

            return result;
        }

        /// <summary>
        /// 校验定位图案、分隔符、定时图案与固定深色模块。
        /// </summary>
        /// <param name="modules">模块矩阵。</param>
        /// <param name="size">符号边长。</param>
        private static void AssertFunctionPatterns(bool[,] modules, int size)
        {
            AssertFinder(modules, 0, 0);
            AssertFinder(modules, size - 7, 0);
            AssertFinder(modules, 0, size - 7);

            for (var i = 8; i < size - 8; i++)
            {
                var expected = (i & 1) == 0;

                if (modules[6, i] != expected)
                {
                    throw new InvalidOperationException($"定时图案（第 6 行第 {i} 列）不符合规范。");
                }

                if (modules[i, 6] != expected)
                {
                    throw new InvalidOperationException($"定时图案（第 {i} 行第 6 列）不符合规范。");
                }
            }

            if (!modules[size - 8, 8])
            {
                throw new InvalidOperationException("固定深色模块位置不正确。");
            }
        }

        /// <summary>
        /// 校验单个定位图案及其分隔符。
        /// </summary>
        /// <param name="modules">模块矩阵。</param>
        /// <param name="left">左上角列索引。</param>
        /// <param name="top">左上角行索引。</param>
        private static void AssertFinder(bool[,] modules, int left, int top)
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

                    var expected = inside &&
                                   (dr == 0 || dr == 6 || dc == 0 || dc == 6 ||
                                    (dr >= 2 && dr <= 4 && dc >= 2 && dc <= 4));

                    if (modules[row, column] != expected)
                    {
                        throw new InvalidOperationException($"定位图案（{top},{left}）偏移（{dr},{dc}）不符合规范。");
                    }
                }
            }
        }

        /// <summary>
        /// 读取第一份格式信息（左上角两侧），返回 15 位原始值。
        /// </summary>
        /// <param name="modules">模块矩阵。</param>
        /// <returns>15 位格式信息原始值。</returns>
        private static int ReadFormatCopyFirst(bool[,] modules)
        {
            var bits = new bool[15];

            for (var i = 0; i <= 5; i++)
            {
                bits[i] = modules[8, i];
            }

            bits[6] = modules[8, 7];
            bits[7] = modules[8, 8];
            bits[8] = modules[7, 8];

            for (var i = 9; i <= 14; i++)
            {
                bits[i] = modules[14 - i, 8];
            }

            return Compose(bits);
        }

        /// <summary>
        /// 读取第二份格式信息（左下与右上），返回 15 位原始值。
        /// </summary>
        /// <param name="modules">模块矩阵。</param>
        /// <returns>15 位格式信息原始值。</returns>
        private static int ReadFormatCopySecond(bool[,] modules)
        {
            var size = modules.GetLength(0);
            var bits = new bool[15];

            for (var i = 0; i <= 6; i++)
            {
                bits[i] = modules[size - 1 - i, 8];
            }

            for (var i = 7; i <= 14; i++)
            {
                bits[i] = modules[8, size - 15 + i];
            }

            return Compose(bits);
        }

        /// <summary>
        /// 把位数组（索引 0 为最高位）组合为整数。
        /// </summary>
        /// <param name="bits">位数组。</param>
        /// <returns>组合结果。</returns>
        private static int Compose(bool[] bits)
        {
            var value = 0;

            for (var i = 0; i < bits.Length; i++)
            {
                value = (value << 1) | (bits[i] ? 1 : 0);
            }

            return value;
        }

        /// <summary>
        /// 解析格式信息：异或固定掩码、校验 BCH、取出纠错等级与掩码编号。
        /// </summary>
        /// <param name="raw">15 位原始值。</param>
        /// <returns>纠错等级、掩码编号与 BCH 校验结果。</returns>
        private static (ElQRCodeGrowEcc Ecc, int Mask, bool Valid) ParseFormat(int raw)
        {
            var format = raw ^ 0x5412;
            var data = format >> 10;

            var recomputed = BchEncode(data, 0x537, 15, 5);
            var valid = recomputed == format;

            var eclBits = (data >> 3) & 0x3;
            var mask = data & 0x7;

            var ecc = eclBits switch
            {
                1 => ElQRCodeGrowEcc.Low,
                0 => ElQRCodeGrowEcc.Medium,
                3 => ElQRCodeGrowEcc.Quartile,
                _ => ElQRCodeGrowEcc.High
            };

            return (ecc, mask, valid);
        }

        /// <summary>
        /// BCH 编码，用于校验格式信息。
        /// </summary>
        /// <param name="data">数据位。</param>
        /// <param name="generator">生成多项式。</param>
        /// <param name="totalBits">总位数。</param>
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

        /// <summary>
        /// 构建功能模块标记表。
        /// </summary>
        /// <param name="version">符号版本。</param>
        /// <param name="size">符号边长。</param>
        /// <returns>功能模块标记。</returns>
        private static bool[,] BuildReservedMap(int version, int size)
        {
            var reserved = new bool[size, size];

            for (var dr = -1; dr <= 7; dr++)
            {
                for (var dc = -1; dc <= 7; dc++)
                {
                    Mark(reserved, size, dr, dc);
                    Mark(reserved, size, dr, size - 7 + dc);
                    Mark(reserved, size, size - 7 + dr, dc);
                }
            }

            for (var i = 0; i < size; i++)
            {
                reserved[6, i] = true;
                reserved[i, 6] = true;
            }

            var centers = ElQRCodeGrowEncoder.GetAlignmentCenters(version);
            foreach (var row in centers)
            {
                foreach (var column in centers)
                {
                    var overlapsFinder =
                        (column <= 8 && row <= 8) ||
                        (column >= size - 9 && row <= 8) ||
                        (column <= 8 && row >= size - 9);

                    if (overlapsFinder)
                    {
                        continue;
                    }

                    for (var dr = -2; dr <= 2; dr++)
                    {
                        for (var dc = -2; dc <= 2; dc++)
                        {
                            reserved[row + dr, column + dc] = true;
                        }
                    }
                }
            }

            reserved[size - 8, 8] = true;

            for (var i = 0; i <= 5; i++)
            {
                reserved[8, i] = true;
                reserved[i, 8] = true;
            }

            reserved[8, 7] = true;
            reserved[8, 8] = true;
            reserved[7, 8] = true;

            for (var i = 0; i < 7; i++)
            {
                reserved[size - 1 - i, 8] = true;
            }

            for (var i = 0; i < 8; i++)
            {
                reserved[8, size - 8 + i] = true;
            }

            if (version >= 7)
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

            return reserved;
        }

        /// <summary>
        /// 在标记表上打点（越界忽略）。
        /// </summary>
        /// <param name="reserved">标记表。</param>
        /// <param name="size">符号边长。</param>
        /// <param name="row">行索引。</param>
        /// <param name="column">列索引。</param>
        private static void Mark(bool[,] reserved, int size, int row, int column)
        {
            if (row >= 0 && column >= 0 && row < size && column < size)
            {
                reserved[row, column] = true;
            }
        }

        /// <summary>
        /// 按规范顺序读取码字：先用掩码还原数据区，再按 Z 字形取出。
        /// </summary>
        /// <param name="modules">模块矩阵。</param>
        /// <param name="reserved">功能模块标记。</param>
        /// <param name="mask">掩码编号。</param>
        /// <returns>码字序列。</returns>
        private static byte[] ReadCodewords(bool[,] modules, bool[,] reserved, int mask)
        {
            var size = modules.GetLength(0);
            var unmasked = new bool[size, size];

            for (var row = 0; row < size; row++)
            {
                for (var column = 0; column < size; column++)
                {
                    var value = modules[row, column];

                    if (!reserved[row, column] && MaskApplies(mask, row, column))
                    {
                        value = !value;
                    }

                    unmasked[row, column] = value;
                }
            }

            var bits = new List<bool>(size * size);

            for (var column = size - 1; column > 0; column -= 2)
            {
                if (column == 6)
                {
                    column = 5;
                }

                var upward = ((column + 1) & 2) == 0;

                for (var i = 0; i < size; i++)
                {
                    var row = upward ? size - 1 - i : i;

                    for (var k = 0; k < 2; k++)
                    {
                        var target = column - k;

                        if (!reserved[row, target])
                        {
                            bits.Add(unmasked[row, target]);
                        }
                    }
                }
            }

            var codewords = new byte[bits.Count / 8];

            for (var i = 0; i < codewords.Length; i++)
            {
                var value = 0;

                for (var j = 0; j < 8; j++)
                {
                    value = (value << 1) | (bits[i * 8 + j] ? 1 : 0);
                }

                codewords[i] = (byte)value;
            }

            return codewords;
        }

        /// <summary>
        /// 掩码判定。
        /// </summary>
        /// <param name="mask">掩码编号。</param>
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

                default:
                    return ((row + column) % 2 + (row * column) % 3) % 2 == 0;
            }
        }

        /// <summary>
        /// 反交织并校验每个纠错块的 Reed-Solomon 伴随式。
        /// </summary>
        /// <param name="codewords">码字序列。</param>
        /// <param name="plan">分块纠错参数。</param>
        /// <param name="syndromesZero">所有伴随式是否为零（输出）。</param>
        /// <returns>数据码字（按块顺序拼接）。</returns>
        private static byte[] DeinterleaveAndVerify(byte[] codewords, QRCodeGrowBlockPlan plan, out bool syndromesZero)
        {
            var blockCount = plan.TotalBlocks;
            var lengths = new int[blockCount];

            for (var i = 0; i < blockCount; i++)
            {
                lengths[i] = i < plan.Group1Blocks ? plan.Group1DataCodewords : plan.Group2DataCodewords;
            }

            var blocks = new byte[blockCount][];
            var corrections = new byte[blockCount][];

            for (var i = 0; i < blockCount; i++)
            {
                blocks[i] = new byte[lengths[i]];
                corrections[i] = new byte[plan.EcPerBlock];
            }

            var position = 0;
            var maxLength = 0;

            for (var i = 0; i < blockCount; i++)
            {
                maxLength = Math.Max(maxLength, lengths[i]);
            }

            for (var i = 0; i < maxLength; i++)
            {
                for (var b = 0; b < blockCount; b++)
                {
                    if (i < lengths[b])
                    {
                        blocks[b][i] = codewords[position++];
                    }
                }
            }

            for (var i = 0; i < plan.EcPerBlock; i++)
            {
                for (var b = 0; b < blockCount; b++)
                {
                    corrections[b][i] = codewords[position++];
                }
            }

            syndromesZero = true;

            for (var b = 0; b < blockCount; b++)
            {
                var full = new byte[lengths[b] + plan.EcPerBlock];
                Array.Copy(blocks[b], 0, full, 0, lengths[b]);
                Array.Copy(corrections[b], 0, full, lengths[b], plan.EcPerBlock);

                for (var j = 0; j < plan.EcPerBlock; j++)
                {
                    if (Evaluate(full, GfPow(2, j)) != 0)
                    {
                        syndromesZero = false;
                    }
                }
            }

            var data = new byte[plan.TotalDataCodewords];
            position = 0;

            for (var b = 0; b < blockCount; b++)
            {
                Array.Copy(blocks[b], 0, data, position, blocks[b].Length);
                position += blocks[b].Length;
            }

            return data;
        }

        /// <summary>
        /// 以给定值对码字多项式求值（Horner 方法）。
        /// </summary>
        /// <param name="codewords">码字。</param>
        /// <param name="value">自变量。</param>
        /// <returns>求值结果。</returns>
        private static byte Evaluate(byte[] codewords, byte value)
        {
            byte result = 0;

            foreach (var codeword in codewords)
            {
                result = (byte)(GfMultiply(result, value) ^ codeword);
            }

            return result;
        }

        private static readonly byte[] Exp = new byte[512];
        private static readonly byte[] Log = new byte[256];

        static QRCodeGrowTestDecoder()
        {
            var value = 1;

            for (var i = 0; i < 255; i++)
            {
                Exp[i] = (byte)value;
                Log[value] = (byte)i;

                value <<= 1;
                if ((value & 0x100) != 0)
                {
                    value ^= 0x11D;
                }
            }

            for (var i = 255; i < Exp.Length; i++)
            {
                Exp[i] = Exp[i - 255];
            }
        }

        private static byte GfMultiply(byte left, byte right)
        {
            if (left == 0 || right == 0)
            {
                return 0;
            }

            return Exp[Log[left] + Log[right]];
        }

        private static byte GfPow(byte value, int exponent)
        {
            byte result = 1;

            for (var i = 0; i < exponent; i++)
            {
                result = GfMultiply(result, value);
            }

            return result;
        }

        /// <summary>
        /// 解析数据位流，还原各数据段与原始文本。
        /// </summary>
        /// <param name="data">数据码字。</param>
        /// <param name="version">符号版本。</param>
        /// <param name="result">解码结果（输出）。</param>
        private static void ParseBitStream(byte[] data, int version, Result result)
        {
            var totalBits = data.Length * 8;
            var position = 0;
            var builder = new StringBuilder();

            int ReadBits(int count)
            {
                var value = 0;

                for (var i = 0; i < count; i++)
                {
                    var bit = position < totalBits && ((data[position >> 3] >> (7 - (position & 7))) & 1) != 0;
                    value = (value << 1) | (bit ? 1 : 0);
                    position++;
                }

                return value;
            }

            while (position + 4 <= totalBits)
            {
                var indicator = ReadBits(4);

                if (indicator == 0)
                {
                    break;
                }

                if (indicator == 7)
                {
                    var designator = ReadBits(8);
                    result.HasUtf8Eci = designator == 26;
                    continue;
                }

                var mode = (QRCodeGrowMode)indicator;
                var count = ReadBits(ElQRCodeGrowEncoder.GetCountFieldBits(version, mode));
                var payload = new StringBuilder();

                switch (mode)
                {
                    case QRCodeGrowMode.Numeric:
                        {
                            var remaining = count;

                            while (remaining >= 3)
                            {
                                payload.Append(ReadBits(10).ToString("000"));
                                remaining -= 3;
                            }

                            if (remaining == 2)
                            {
                                payload.Append(ReadBits(7).ToString("00"));
                            }
                            else if (remaining == 1)
                            {
                                payload.Append(ReadBits(4).ToString("0"));
                            }

                            break;
                        }

                    case QRCodeGrowMode.Alphanumeric:
                        {
                            const string charset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";
                            var remaining = count;

                            while (remaining >= 2)
                            {
                                var value = ReadBits(11);
                                payload.Append(charset[value / 45]).Append(charset[value % 45]);
                                remaining -= 2;
                            }

                            if (remaining == 1)
                            {
                                payload.Append(charset[ReadBits(6)]);
                            }

                            break;
                        }

                    default:
                        {
                            var bytes = new byte[count];

                            for (var i = 0; i < count; i++)
                            {
                                bytes[i] = (byte)ReadBits(8);
                            }

                            payload.Append(Encoding.UTF8.GetString(bytes));
                            break;
                        }
                }

                result.Segments.Add(new SegmentResult { Mode = mode, Payload = payload.ToString() });
                builder.Append(payload);
            }

            result.Text = builder.ToString();
        }
    }
}
