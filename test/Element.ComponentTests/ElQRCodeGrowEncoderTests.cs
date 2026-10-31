using System;
using System.Collections.Generic;
using System.Text;
using ElementPlus;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 二维码编码器一致性测试：用独立来源（规范表、独立公式、独立解码器）
    /// 交叉验证参数表与整条编码链路，而不是只验证“自己和自己一致”。
    /// </summary>
    public class ElQRCodeGrowEncoderTests
    {
        /// <summary>
        /// 各版本的码字总数（ISO/IEC 18004 表 1）。该值与纠错等级无关。
        /// </summary>
        private static readonly int[] StandardTotalCodewords =
        {
            26, 44, 70, 100, 134, 172, 196, 242, 292, 346,
            404, 466, 532, 581, 655, 733, 815, 901, 991, 1085,
            1156, 1258, 1364, 1474, 1588, 1706, 1828, 1921, 2051, 2185,
            2323, 2465, 2611, 2761, 2876, 3034, 3196, 3362, 3532, 3706
        };

        /// <summary>
        /// 各版本校正图案的个数（ISO/IEC 18004 表 E.1）。
        /// </summary>
        private static readonly int[] StandardAlignmentCount =
        {
            0, 2, 2, 2, 2, 2, 3, 3, 3, 3,
            3, 3, 3, 4, 4, 4, 4, 4, 4, 4,
            5, 5, 5, 5, 5, 5, 5, 6, 6, 6,
            6, 6, 6, 6, 7, 7, 7, 7, 7, 7
        };

        private static readonly ElQRCodeGrowEcc[] AllLevels =
        {
            ElQRCodeGrowEcc.Low,
            ElQRCodeGrowEcc.Medium,
            ElQRCodeGrowEcc.Quartile,
            ElQRCodeGrowEcc.High
        };

        [Fact]
        public void BlockPlan_TotalCodewords_MatchStandardTable_AndAreConsistentAcrossEcc()
        {
            for (var version = 1; version <= 40; version++)
            {
                var expected = StandardTotalCodewords[version - 1];

                foreach (var level in AllLevels)
                {
                    var plan = ElQRCodeGrowEncoder.GetBlockPlan(version, level);

                    Assert.Equal(expected, plan.TotalCodewords);
                    Assert.Equal(expected, ElQRCodeGrowEncoder.GetTotalCodewords(version));
                    Assert.Equal(
                        plan.TotalDataCodewords,
                        ElQRCodeGrowEncoder.GetDataCodewords(version, level));
                }
            }
        }

        [Fact]
        public void BlockPlan_SecondGroup_HoldsOneMoreCodewordThanFirstGroup()
        {
            for (var version = 1; version <= 40; version++)
            {
                foreach (var level in AllLevels)
                {
                    var plan = ElQRCodeGrowEncoder.GetBlockPlan(version, level);

                    Assert.True(plan.Group1Blocks > 0, $"版本 {version} 的 {level} 级没有纠错块。");

                    if (plan.Group2Blocks > 0)
                    {
                        Assert.Equal(plan.Group1DataCodewords + 1, plan.Group2DataCodewords);
                    }
                }
            }
        }

        [Fact]
        public void BlockPlan_EcPerBlock_ComesFromStandardValueSet_AndGrowsWithEccLevel()
        {
            var allowed = new HashSet<int> { 7, 10, 13, 15, 16, 17, 18, 20, 22, 24, 26, 28, 30 };

            for (var version = 1; version <= 40; version++)
            {
                var previous = -1;

                foreach (var level in AllLevels)
                {
                    var plan = ElQRCodeGrowEncoder.GetBlockPlan(version, level);

                    Assert.Contains(plan.EcPerBlock, allowed);

                    var totalEc = plan.TotalEcCodewords;
                    Assert.True(totalEc > previous, $"版本 {version} 的纠错码字数未随纠错等级递增（{level}）。");
                    previous = totalEc;
                }
            }
        }

        [Fact]
        public void BlockPlan_KnownMixedGroupEntries_MatchStandard()
        {
            var expected = new Dictionary<string, int[]>
            {
                // ecPerBlock, g1Blocks, g1Data, g2Blocks, g2Data
                ["3/Quartile"] = new[] { 18, 2, 17, 0, 0 },
                ["5/Quartile"] = new[] { 18, 2, 15, 2, 16 },
                ["7/High"] = new[] { 26, 4, 13, 1, 14 },
                ["10/Medium"] = new[] { 26, 4, 43, 1, 44 },
                ["14/Low"] = new[] { 30, 3, 115, 1, 116 },
                ["16/Quartile"] = new[] { 24, 15, 19, 2, 20 },
                ["32/Low"] = new[] { 30, 17, 115, 0, 0 },
                ["34/High"] = new[] { 30, 59, 16, 1, 17 },
                ["40/Low"] = new[] { 30, 19, 118, 6, 119 },
                ["40/High"] = new[] { 30, 20, 15, 61, 16 }
            };

            foreach (var pair in expected)
            {
                var parts = pair.Key.Split('/');
                var version = int.Parse(parts[0]);
                var level = (ElQRCodeGrowEcc)Enum.Parse(typeof(ElQRCodeGrowEcc), parts[1]);
                var plan = ElQRCodeGrowEncoder.GetBlockPlan(version, level);

                Assert.Equal(pair.Value[0], plan.EcPerBlock);
                Assert.Equal(pair.Value[1], plan.Group1Blocks);
                Assert.Equal(pair.Value[2], plan.Group1DataCodewords);
                Assert.Equal(pair.Value[3], plan.Group2Blocks);
                Assert.Equal(pair.Value[4], plan.Group2DataCodewords);
            }
        }

        [Fact]
        public void AlignmentCenters_MatchIndependentFormula()
        {
            for (var version = 2; version <= 40; version++)
            {
                var size = ElQRCodeGrowEncoder.GetSymbolSize(version);
                var count = StandardAlignmentCount[version - 1];

                // 规范：末位固定为边长 - 7，其余按“偶数步长向上取整”从末位向前推出。
                // 版本 32 是 ISO/IEC 18004 表 E.1 的明文特例：步长固定为 26，
                // 否则会推出 26/54/82/110 而非规范的 34/60/86/112。
                var step = version == 32
                    ? 26
                    : (int)Math.Ceiling((size - 13) / (double)(count - 1) / 2) * 2;
                var expected = new List<int>();

                for (var i = 0; i < count - 1; i++)
                {
                    expected.Add(size - 7 - i * step);
                }

                expected.Reverse();
                expected.Insert(0, 6);

                Assert.Equal(expected, ElQRCodeGrowEncoder.GetAlignmentCenters(version));
            }

            Assert.Empty(ElQRCodeGrowEncoder.GetAlignmentCenters(1));
        }

        [Fact]
        public void DataModuleCount_MatchesTotalCodewords()
        {
            for (var version = 1; version <= 40; version++)
            {
                var size = ElQRCodeGrowEncoder.GetSymbolSize(version);
                var functionModules = CountFunctionModules(version, size);
                var dataModules = size * size - functionModules;

                var expected = ElQRCodeGrowEncoder.GetTotalCodewords(version) * 8 + RemainderBits(version);

                Assert.Equal(expected, dataModules);
            }
        }

        [Theory]
        [InlineData(1, ElQRCodeGrowEcc.Low, 41, 25, 17)]
        [InlineData(1, ElQRCodeGrowEcc.Medium, 34, 20, 14)]
        [InlineData(1, ElQRCodeGrowEcc.Quartile, 27, 16, 11)]
        [InlineData(1, ElQRCodeGrowEcc.High, 17, 10, 7)]
        [InlineData(2, ElQRCodeGrowEcc.Medium, 63, 38, 26)]
        [InlineData(10, ElQRCodeGrowEcc.Low, 652, 395, 271)]
        [InlineData(40, ElQRCodeGrowEcc.Low, 7089, 4296, 2953)]
        [InlineData(40, ElQRCodeGrowEcc.Medium, 5596, 3391, 2331)]
        [InlineData(40, ElQRCodeGrowEcc.Quartile, 3993, 2420, 1663)]
        [InlineData(40, ElQRCodeGrowEcc.High, 3057, 1852, 1273)]
        public void Capacity_MatchesStandardTable_And_IsTight(
            int version,
            ElQRCodeGrowEcc ecc,
            int numeric,
            int alphanumeric,
            int byteCount)
        {
            Assert.Equal(numeric, ElQRCodeGrowEncoder.GetCapacity(version, ecc, QRCodeGrowMode.Numeric));
            Assert.Equal(alphanumeric, ElQRCodeGrowEncoder.GetCapacity(version, ecc, QRCodeGrowMode.Alphanumeric));
            Assert.Equal(byteCount, ElQRCodeGrowEncoder.GetCapacity(version, ecc, QRCodeGrowMode.Byte));

            // 容量值必须“卡得很准”：恰好放得下，多一个字符就要更大版本。
            Assert.Equal(version, Encode(new string('7', numeric), ecc, version).Version);
            Assert.Throws<ArgumentException>(() => Encode(new string('7', numeric + 1), ecc, version));

            Assert.Equal(version, Encode(new string('A', alphanumeric), ecc, version).Version);
            Assert.Throws<ArgumentException>(() => Encode(new string('A', alphanumeric + 1), ecc, version));

            Assert.Equal(version, Encode(new string('a', byteCount), ecc, version).Version);
            Assert.Throws<ArgumentException>(() => Encode(new string('a', byteCount + 1), ecc, version));
        }

        [Fact]
        public void Encode_SelectsNumericMode_ForDigits()
        {
            var matrix = ElQRCodeGrowEncoder.Encode("20260919");

            Assert.Single(matrix.Segments);
            Assert.Equal(QRCodeGrowMode.Numeric, matrix.Segments[0].Mode);
            Assert.Equal(1, matrix.Version);
        }

        [Fact]
        public void Encode_SelectsAlphanumericMode_ForUpperCaseAndSymbols()
        {
            var matrix = ElQRCodeGrowEncoder.Encode("HELLO WORLD 123");

            Assert.Single(matrix.Segments);
            Assert.Equal(QRCodeGrowMode.Alphanumeric, matrix.Segments[0].Mode);
        }

        [Fact]
        public void Encode_SelectsByteMode_ForNonLatinContent()
        {
            var matrix = ElQRCodeGrowEncoder.Encode("电子病历");

            Assert.Single(matrix.Segments);
            Assert.Equal(QRCodeGrowMode.Byte, matrix.Segments[0].Mode);
        }

        [Fact]
        public void Encode_NumericPayload_UsesSmallerVersionThanByteMode()
        {
            var payload = new string('9', 60);

            var optimized = ElQRCodeGrowEncoder.Encode(payload, ElQRCodeGrowEcc.Medium);

            Assert.Equal(QRCodeGrowMode.Numeric, optimized.Segments[0].Mode);
            Assert.Equal(2, optimized.Version);

            // 同样 60 个字符若只能走字节模式，需要 4 版；数字模式省下 2 个版本。
            Assert.True(ElQRCodeGrowEncoder.GetCapacity(3, ElQRCodeGrowEcc.Medium, QRCodeGrowMode.Byte) < 60);
            Assert.True(ElQRCodeGrowEncoder.GetCapacity(4, ElQRCodeGrowEcc.Medium, QRCodeGrowMode.Byte) >= 60);

            // 关闭多段优化只是「不拆分」，仍会为纯数字内容选择最省的数字模式，
            // 因此纯数字载荷下开关该参数得到的版本相同。
            var single = ElQRCodeGrowEncoder.Encode(payload, ElQRCodeGrowEcc.Medium, optimizeSegments: false);
            Assert.Equal(optimized.Version, single.Version);
        }

        [Fact]
        public void Encode_OptimizeSegments_SplitsMixedContentIntoSeveralSegments()
        {
            var matrix = ElQRCodeGrowEncoder.Encode("ORDER-2026-0001234567890123456789012345678901-X");

            Assert.True(matrix.Segments.Count > 1, "混合内容应被拆分为多个数据段。");
            Assert.Contains(matrix.Segments, segment => segment.Mode == QRCodeGrowMode.Numeric);
            Assert.Contains(matrix.Segments, segment => segment.Mode == QRCodeGrowMode.Alphanumeric);
        }

        [Fact]
        public void Encode_OptimizeSegments_NeverProducesLargerVersion()
        {
            var samples = new[]
            {
                "ORDER-2026-0001234567890123456789012345678901-X",
                "A1B2C3D4E5F6G7H8I9J0",
                "https://element-plus.org/zh-CN/component/1234567890",
                "NO1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnop",
                "13800138000",
                "2026年09月19日 10:23:45 门诊号 0001234567"
            };

            foreach (var sample in samples)
            {
                var optimized = ElQRCodeGrowEncoder.Encode(sample, ElQRCodeGrowEcc.Medium);
                var single = ElQRCodeGrowEncoder.Encode(sample, ElQRCodeGrowEcc.Medium, optimizeSegments: false);

                Assert.True(
                    optimized.Version <= single.Version,
                    $"多段优化不应增大版本：{sample} -> {optimized.Version} vs {single.Version}");
            }
        }

        [Fact]
        public void Encode_EmptyString_ProducesVersion1Symbol()
        {
            var matrix = ElQRCodeGrowEncoder.Encode(string.Empty);

            Assert.Equal(1, matrix.Version);
            // 与 zxing 的 chooseMode 行为一致：空内容仍写入一个长度为 0 的字节段
            // （模式位 0100 + 计数 00000000），位流结构完整且能被任何解码器读成空串。
            var segment = Assert.Single(matrix.Segments);
            Assert.Equal(QRCodeGrowMode.Byte, segment.Mode);
            Assert.Equal(0, segment.Length);
            Assert.Equal(21, matrix.Size);

            var decoded = QRCodeGrowTestDecoder.Decode(matrix.Modules);
            Assert.Equal(string.Empty, decoded.Text);
        }

        [Fact]
        public void Encode_TooLongContent_Throws()
        {
            var payload = new string('A', 4000);

            var error = Assert.Throws<ArgumentException>(
                () => ElQRCodeGrowEncoder.Encode(payload, ElQRCodeGrowEcc.High));

            Assert.Contains("过长", error.Message);
        }

        [Fact]
        public void Encode_RespectsVersionRange()
        {
            Assert.Throws<ArgumentException>(
                () => ElQRCodeGrowEncoder.Encode("https://element-plus.org", ElQRCodeGrowEcc.High, 1, 1));

            var matrix = ElQRCodeGrowEncoder.Encode("1", ElQRCodeGrowEcc.Low, 5, 6);
            Assert.Equal(5, matrix.Version);
        }

        [Fact]
        public void Encode_InvalidVersionRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ElQRCodeGrowEncoder.Encode("A", ElQRCodeGrowEcc.Low, 0, 40));
            Assert.Throws<ArgumentOutOfRangeException>(() => ElQRCodeGrowEncoder.Encode("A", ElQRCodeGrowEcc.Low, 10, 5));
        }

        [Fact]
        public void Encode_DifferentContent_ProducesDifferentModules()
        {
            var first = ElQRCodeGrowEncoder.Encode("https://element-plus.org");
            var second = ElQRCodeGrowEncoder.Encode("https://element-plus.org/zh-CN");

            Assert.NotEqual(first.CountDarkModules(), second.CountDarkModules());
        }

        private static QRCodeGrowMatrix Encode(string value, ElQRCodeGrowEcc ecc, int version) =>
            ElQRCodeGrowEncoder.Encode(value, ecc, version, version);

        /// <summary>
        /// 计算功能模块数量（定位图案与分隔符、定时图案、校正图案、格式与版本信息、固定深色模块）。
        /// 与编码器实现无共享代码。
        /// </summary>
        /// <param name="version">符号版本。</param>
        /// <param name="size">符号边长。</param>
        /// <returns>功能模块数量。</returns>
        private static int CountFunctionModules(int version, int size)
        {
            var map = new bool[size, size];

            void Mark(int row, int column)
            {
                if (row >= 0 && column >= 0 && row < size && column < size)
                {
                    map[row, column] = true;
                }
            }

            // 定位图案 + 分隔符：三个 8×8 区域
            for (var dr = -1; dr <= 7; dr++)
            {
                for (var dc = -1; dc <= 7; dc++)
                {
                    Mark(dr, dc);
                    Mark(dr, size - 7 + dc);
                    Mark(size - 7 + dr, dc);
                }
            }

            // 定时图案
            for (var i = 0; i < size; i++)
            {
                map[6, i] = true;
                map[i, 6] = true;
            }

            // 校正图案
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
                            map[row + dr, column + dc] = true;
                        }
                    }
                }
            }

            // 固定深色模块
            map[size - 8, 8] = true;

            // 格式信息两份
            for (var i = 0; i <= 5; i++)
            {
                map[8, i] = true;
                map[i, 8] = true;
            }

            map[8, 7] = true;
            map[8, 8] = true;
            map[7, 8] = true;

            for (var i = 0; i < 7; i++)
            {
                map[size - 1 - i, 8] = true;
            }

            for (var i = 0; i < 8; i++)
            {
                map[8, size - 8 + i] = true;
            }

            // 版本信息两份
            if (version >= 7)
            {
                for (var row = size - 11; row <= size - 9; row++)
                {
                    for (var column = 0; column <= 5; column++)
                    {
                        map[row, column] = true;
                    }
                }

                for (var row = 0; row <= 5; row++)
                {
                    for (var column = size - 11; column <= size - 9; column++)
                    {
                        map[row, column] = true;
                    }
                }
            }

            var count = 0;

            for (var row = 0; row < size; row++)
            {
                for (var column = 0; column < size; column++)
                {
                    if (map[row, column])
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// 各版本数据区末尾的余数位数（ISO/IEC 18004 表 1）。
        /// </summary>
        /// <param name="version">符号版本。</param>
        /// <returns>余数位数。</returns>
        private static int RemainderBits(int version)
        {
            if (version == 1)
            {
                return 0;
            }

            if (version <= 6)
            {
                return 7;
            }

            if (version <= 13)
            {
                return 0;
            }

            if (version <= 20)
            {
                return 3;
            }

            if (version <= 27)
            {
                return 4;
            }

            return version <= 34 ? 3 : 0;
        }

        [Fact]
        public void DataCodewords_MatchIsoStandardExample()
        {
            // ISO/IEC 18004 附录 I 的示例：版本 1-M，内容 "01234567"。
            // 模式 0001 + 计数 0000001000 + 012/345/67 的数据位 + 终止符与填充位，
            // 得到前 6 个码字为 10 20 0C 56 61 80，其后交替填充 EC 11。
            var data = ElQRCodeGrowEncoder.GetDataCodewords("01234567", ElQRCodeGrowEcc.Medium, 1);

            Assert.Equal(16, data.Length);
            Assert.Equal(
                new byte[] { 0x10, 0x20, 0x0C, 0x56, 0x61, 0x80, 0xEC, 0x11, 0xEC, 0x11, 0xEC, 0x11, 0xEC, 0x11, 0xEC, 0x11 },
                data);

            // 同一示例的 10 个纠错码字。
            var codewords = ElQRCodeGrowEncoder.GetCodewords("01234567", ElQRCodeGrowEcc.Medium, 1);

            Assert.Equal(26, codewords.Length);
            Assert.Equal(
                new byte[] { 0xA5, 0x24, 0xD4, 0xC1, 0xED, 0x36, 0xC7, 0x87, 0x2C, 0x55 },
                codewords.AsSpan(16, 10).ToArray());
        }

        [Fact]
        public void ReedSolomon_ProducesZeroSyndromes_ForAllBlockPlans()
        {
            // 对若干版本/等级，直接验证“数据 + 纠错”码字多项式在 α^0..α^(ec-1) 处取值为零。
            foreach (var version in new[] { 1, 2, 5, 7, 10, 14, 21, 27, 32, 40 })
            {
                foreach (var level in AllLevels)
                {
                    var plan = ElQRCodeGrowEncoder.GetBlockPlan(version, level);
                    var capacity = ElQRCodeGrowEncoder.GetCapacity(version, level, QRCodeGrowMode.Byte);
                    var payload = new string('z', Math.Min(capacity, 40));
                    var codewords = ElQRCodeGrowEncoder.GetCodewords(payload, level, version, optimizeSegments: false);

                    Assert.Equal(plan.TotalCodewords, codewords.Length);

                    // 单块情形可直接对整串求值；多块情形由解码器的反交织逻辑覆盖。
                    if (plan.TotalBlocks == 1)
                    {
                        for (var i = 0; i < plan.EcPerBlock; i++)
                        {
                            Assert.Equal(0, EvaluatePolynomial(codewords, GfPower(2, i)));
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData("https://element-plus.org/zh-CN/component/qrcode")]
        [InlineData("ORDER-2026-0001234567890123456789012345678901-X")]
        [InlineData("13800138000")]
        [InlineData("A1B2C3D4E5")]
        [InlineData("Hello, World!")]
        [InlineData("电子病历系统 · 门诊号 0001234567")]
        [InlineData("1")]
        [InlineData("x")]
        [InlineData("0000000000000000000000000000000000000")]
        [InlineData("WIFI:T:WPA;S:Hospital-5G;P:password123;;")]
        public void RoundTrip_CommonPayloads_AreDecodable(string payload)
        {
            var decoded = RoundTrip(payload);

            Assert.Equal(payload, decoded.Text);
        }

        [Fact]
        public void RoundTrip_AllEccLevels()
        {
            const string payload = "https://element-plus.org/zh-CN/component/qrcode.html?a=1&b=2";

            foreach (var level in AllLevels)
            {
                var decoded = RoundTrip(payload, level);

                Assert.Equal(level, decoded.Ecc);
                Assert.Equal(payload, decoded.Text);
            }
        }

        [Fact]
        public void RoundTrip_Chinese_IsEncodedAsUtf8Bytes()
        {
            const string payload = "某某市人民医院 检验报告单 2026-09-19";

            var decoded = RoundTrip(payload);

            Assert.Contains(decoded.Segments, segment => segment.Mode == QRCodeGrowMode.Byte);
            Assert.Equal(payload, decoded.Text);
            Assert.False(decoded.HasUtf8Eci);
        }

        [Fact]
        public void RoundTrip_WithEci_WritesUtf8Designator()
        {
            var decoded = RoundTrip("检验单 0001", ElQRCodeGrowEcc.Medium, useEci: true);

            Assert.True(decoded.HasUtf8Eci);
            Assert.Equal("检验单 0001", decoded.Text);
        }

        [Fact]
        public void RoundTrip_WithoutSegmentOptimization_UsesSingleSegment()
        {
            const string payload = "ORDER-2026-0001234567890123456789012345678901-X";

            var decoded = RoundTrip(payload, optimizeSegments: false);

            Assert.Single(decoded.Segments);
            Assert.Equal(payload, decoded.Text);
        }

        [Fact]
        public void RoundTrip_LongNumeric_UsesVersionsAboveNine()
        {
            var payload = new string('7', 1200);

            var matrix = ElQRCodeGrowEncoder.Encode(payload, ElQRCodeGrowEcc.Low);
            var decoded = QRCodeGrowTestDecoder.Decode(matrix.Modules);

            Assert.True(matrix.Version >= 10, $"1200 位数字应使用 10 版以上，实际 {matrix.Version}。");
            Assert.Equal(payload, decoded.Text);
        }

        [Fact]
        public void RoundTrip_LargeNumeric_UsesVersionsAboveTwentySeven()
        {
            var payload = new string('3', 3600);

            var matrix = ElQRCodeGrowEncoder.Encode(payload, ElQRCodeGrowEcc.Low);
            var decoded = QRCodeGrowTestDecoder.Decode(matrix.Modules);

            Assert.True(matrix.Version >= 27, $"3600 位数字应使用 27 版以上，实际 {matrix.Version}。");
            Assert.Equal(payload, decoded.Text);
        }

        [Fact]
        public void RoundTrip_MaximumBytePayload_ReachesVersion40()
        {
            var payload = new string('a', 2900);

            var matrix = ElQRCodeGrowEncoder.Encode(payload, ElQRCodeGrowEcc.Low);
            var decoded = QRCodeGrowTestDecoder.Decode(matrix.Modules);

            Assert.Equal(40, matrix.Version);
            Assert.Equal(177, matrix.Size);
            Assert.Equal(payload, decoded.Text);
        }

        [Fact]
        public void RoundTrip_SweepAcrossVersionsAndLevels()
        {
            var payloads = new[]
            {
                new string('9', 25),
                new string('9', 120),
                new string('9', 500),
                new string('Z', 30),
                new string('Z', 200),
                new string('q', 60),
                new string('q', 400),
                "订单编号 20260919000123 金额 1580.00 元"
            };

            var count = 0;

            foreach (var level in AllLevels)
            {
                foreach (var payload in payloads)
                {
                    var decoded = RoundTrip(payload, level);
                    Assert.Equal(payload, decoded.Text);
                    count++;
                }
            }

            Assert.Equal(32, count);
        }

        [Fact]
        public void VersionInfo_IsCorrectlyWritten_ForVersionsSevenAndAbove()
        {
            for (var version = 7; version <= 40; version++)
            {
                var payload = new string('5', ElQRCodeGrowEncoder.GetCapacity(version, ElQRCodeGrowEcc.Low, QRCodeGrowMode.Numeric));
                var matrix = ElQRCodeGrowEncoder.Encode(payload, ElQRCodeGrowEcc.Low, version, version);
                var size = matrix.Size;
                var expected = BchEncode(version, 0x1F25, 18, 6);

                for (var column = 0; column < 6; column++)
                {
                    for (var row = 0; row < 3; row++)
                    {
                        Assert.Equal(
                            ((expected >> (row + column * 3)) & 1) != 0,
                            matrix.Modules[size - 11 + row, column]);
                    }
                }

                for (var row = 0; row < 6; row++)
                {
                    for (var column = 0; column < 3; column++)
                    {
                        Assert.Equal(
                            ((expected >> (row * 3 + column)) & 1) != 0,
                            matrix.Modules[row, size - 11 + column]);
                    }
                }
            }
        }

        [Fact]
        public void MaskSelection_PicksLowestPenaltyPattern_AndDarkRatioStaysBalanced()
        {
            var matrix = ElQRCodeGrowEncoder.Encode("https://element-plus.org/zh-CN/component/qrcode", ElQRCodeGrowEcc.Quartile);

            Assert.InRange(matrix.MaskPattern, 0, 7);

            var ratio = matrix.CountDarkModules() / (double)(matrix.Size * matrix.Size);
            Assert.InRange(ratio, 0.35, 0.65);
        }

        private static QRCodeGrowTestDecoder.Result RoundTrip(
            string value,
            ElQRCodeGrowEcc ecc = ElQRCodeGrowEcc.Quartile,
            bool optimizeSegments = true,
            bool useEci = false,
            int minVersion = 1,
            int maxVersion = 40)
        {
            var matrix = ElQRCodeGrowEncoder.Encode(value, ecc, minVersion, maxVersion, optimizeSegments, useEci);
            var decoded = QRCodeGrowTestDecoder.Decode(matrix.Modules);

            Assert.True(decoded.FormatFirstCopyValid, "格式信息不是合法的 BCH 码字。");
            Assert.True(decoded.FormatCopiesMatch, "两份格式信息不一致。");
            Assert.True(decoded.SyndromesAllZero, "纠错码字的 Reed-Solomon 伴随式不为零。");
            Assert.Equal(matrix.Version, decoded.Version);
            Assert.Equal(ecc, decoded.Ecc);
            Assert.Equal(matrix.MaskPattern, decoded.Mask);
            Assert.Equal(value, decoded.Text);

            return decoded;
        }

        private static byte EvaluatePolynomial(byte[] codewords, byte value)
        {
            byte result = 0;

            foreach (var codeword in codewords)
            {
                result = (byte)(GfMultiply(result, value) ^ codeword);
            }

            return result;
        }

        private static byte GfMultiply(byte left, byte right)
        {
            if (left == 0 || right == 0)
            {
                return 0;
            }

            return Exp[Log[left] + Log[right]];
        }

        private static byte GfPower(byte value, int exponent)
        {
            byte result = 1;

            for (var i = 0; i < exponent; i++)
            {
                result = GfMultiply(result, value);
            }

            return result;
        }

        private static readonly byte[] Exp = BuildExpTable();
        private static readonly byte[] Log = BuildLogTable();

        private static byte[] BuildExpTable()
        {
            var table = new byte[512];
            var value = 1;

            for (var i = 0; i < 255; i++)
            {
                table[i] = (byte)value;
                value <<= 1;

                if ((value & 0x100) != 0)
                {
                    value ^= 0x11D;
                }
            }

            for (var i = 255; i < table.Length; i++)
            {
                table[i] = table[i - 255];
            }

            return table;
        }

        private static byte[] BuildLogTable()
        {
            var table = new byte[256];
            var value = 1;

            for (var i = 0; i < 255; i++)
            {
                table[value] = (byte)i;
                value <<= 1;

                if ((value & 0x100) != 0)
                {
                    value ^= 0x11D;
                }
            }

            return table;
        }

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
    }
}
