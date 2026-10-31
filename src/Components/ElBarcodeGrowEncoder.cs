using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ElementPlus
{
    /// <summary>
    /// 一维条码编码器。把文本编码为条码几何（矩形列表）或完整 SVG 字符串。
    /// <para>
    /// 全部编码算法均在本库内独立实现，不依赖任何第三方条码库，也不依赖浏览器脚本或画布 API，
    /// 因此在服务端渲染、单元测试与离线打包场景下同样可用。
    /// </para>
    /// </summary>
    public static class ElBarcodeGrowEncoder
    {
        /// <summary>
        /// Code 128 码字模式表（0..106）。每项为 6 位数字，依次表示 条/空/条/空/条/空 的模块宽度。
        /// 停止符（106）按规范为 7 位（末位为终止条）。
        /// </summary>
        private static readonly string[] Code128Patterns =
        {
            "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
            "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
            "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
            "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
            "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
            "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
            "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
            "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
            "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
            "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
            "114131", "311141", "411131", "211412", "211214", "211232", "2331112"
        };

        /// <summary>Code 128 的起始符 B 码字值。</summary>
        private const int Code128StartB = 104;

        /// <summary>Code 128 的停止符码字值。</summary>
        private const int Code128Stop = 106;

        /// <summary>Code 39 字符模式表。每个模式为 9 个元素（条/空交替），n=窄(1)，w=宽(2)。</summary>
        private static readonly Dictionary<char, string> Code39Patterns = new Dictionary<char, string>
        {
            ['0'] = "nnnwwnwnn",
            ['1'] = "wnnwnnnnw",
            ['2'] = "nnwwnnnnw",
            ['3'] = "wnwwnnnnn",
            ['4'] = "nnnwwnnnw",
            ['5'] = "wnnwwnnnn",
            ['6'] = "nnwwwnnnn",
            ['7'] = "nnnwnnwnw",
            ['8'] = "wnnwnnwnn",
            ['9'] = "nnwwnnwnn",
            ['A'] = "wnnnnwnnw",
            ['B'] = "nnwnnwnnw",
            ['C'] = "wnwnnwnnn",
            ['D'] = "nnnnwwnnw",
            ['E'] = "wnnnwwnnn",
            ['F'] = "nnwnwwnnn",
            ['G'] = "nnnnnwwnw",
            ['H'] = "wnnnnwwnn",
            ['I'] = "nnwnnwwnn",
            ['J'] = "nnnnwwwnn",
            ['K'] = "wnnnnnnww",
            ['L'] = "nnwnnnnww",
            ['M'] = "wnwnnnnwn",
            ['N'] = "nnnnwnnww",
            ['O'] = "wnnnwnnwn",
            ['P'] = "nnwnwnnwn",
            ['Q'] = "nnnnnnwww",
            ['R'] = "wnnnnnwwn",
            ['S'] = "nnwnnnwwn",
            ['T'] = "nnnnwnwwn",
            ['U'] = "wwnnnnnnw",
            ['V'] = "nwwnnnnnw",
            ['W'] = "wwwnnnnnn",
            ['X'] = "nwnnwnnnw",
            ['Y'] = "wwnnwnnnn",
            ['Z'] = "nwwnwnnnn",
            ['-'] = "nwnnnnwnw",
            ['.'] = "wwnnnnwnn",
            [' '] = "nwwnnnwnn",
            ['$'] = "nwnwnwnnn",
            ['/'] = "nwnwnnnwn",
            ['+'] = "nwnnnwnwn",
            ['%'] = "nnnwnwnwn",
            ['*'] = "nwnnwnwnn"
        };

        /// <summary>ITF（交叉二五码）数字模式表。n=窄，w=宽，各 5 个元素。</summary>
        private static readonly Dictionary<char, string> ItfPatterns = new Dictionary<char, string>
        {
            ['0'] = "nnwwn",
            ['1'] = "wnnnw",
            ['2'] = "nwnnw",
            ['3'] = "wwnnn",
            ['4'] = "nnwnw",
            ['5'] = "wnwnn",
            ['6'] = "nwwnn",
            ['7'] = "nnnww",
            ['8'] = "wnnwn",
            ['9'] = "nwnwn"
        };

        /// <summary>
        /// Codabar 字符模式表。<c>Bars</c> 为 4 位（1=宽），<c>Spaces</c> 为 3 位（0=宽）。
        /// 渲染顺序为 条/空/条/空/条/空/条。
        /// </summary>
        private static readonly Dictionary<char, (string Bars, string Spaces)> CodabarPatterns =
            new Dictionary<char, (string Bars, string Spaces)>
            {
                ['0'] = ("0001", "001"),
                ['1'] = ("0010", "001"),
                ['2'] = ("0001", "010"),
                ['3'] = ("1000", "100"),
                ['4'] = ("0100", "001"),
                ['5'] = ("1000", "001"),
                ['6'] = ("0001", "100"),
                ['7'] = ("0010", "100"),
                ['8'] = ("0100", "100"),
                ['9'] = ("1000", "010"),
                ['-'] = ("0010", "010"),
                ['$'] = ("0100", "010"),
                ['.'] = ("0001", "000"),
                ['/'] = ("0010", "000"),
                [':'] = ("0100", "000"),
                ['+'] = ("1000", "000"),
                ['A'] = ("0100", "011"),
                ['B'] = ("0001", "110"),
                ['C'] = ("0001", "011"),
                ['D'] = ("0010", "011")
            };

        /// <summary>EAN/UPC 左侧奇校验（L）编码表。</summary>
        private static readonly string[] EanLeftOdd =
        {
            "0001101", "0011001", "0010011", "0111101", "0100011", "0110001", "0101111", "0111011", "0110111", "0001011"
        };

        /// <summary>EAN/UPC 左侧偶校验（G）编码表。</summary>
        private static readonly string[] EanLeftEven =
        {
            "0100111", "0110011", "0011011", "0100001", "0011101", "0111001", "0000101", "0010001", "0001001", "0010111"
        };

        /// <summary>EAN/UPC 右侧（R）编码表。</summary>
        private static readonly string[] EanRight =
        {
            "1110010", "1100110", "1101100", "1000010", "1011100", "1001110", "1010000", "1000100", "1001000", "1110100"
        };

        /// <summary>EAN-13 首位数字决定的左侧 6 位奇偶模式表（L=奇校验，G=偶校验）。</summary>
        private static readonly string[] Ean13ParityPatterns =
        {
            "LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG", "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL"
        };

        /// <summary>MSI（Modified Plessey）数字位图表。</summary>
        private static readonly Dictionary<char, string> MsiPatterns = new Dictionary<char, string>
        {
            ['0'] = "100100100100",
            ['1'] = "100100100110",
            ['2'] = "100100110100",
            ['3'] = "100100110110",
            ['4'] = "100110100100",
            ['5'] = "100110100110",
            ['6'] = "100110110100",
            ['7'] = "100110110110",
            ['8'] = "110100100100",
            ['9'] = "110100100110"
        };

        /// <summary>POSTNET 数字位图（权重 7、4、2、1、0；1=全高条，0=半高条）。</summary>
        private static readonly Dictionary<char, string> PostnetPatterns = new Dictionary<char, string>
        {
            ['0'] = "11000",
            ['1'] = "00011",
            ['2'] = "00101",
            ['3'] = "00110",
            ['4'] = "01001",
            ['5'] = "01010",
            ['6'] = "01100",
            ['7'] = "10001",
            ['8'] = "10010",
            ['9'] = "10100"
        };

        /// <summary>RM4SCC 上行/下行四状态模式表（值 1..6 依次对应）。</summary>
        private static readonly string[] Rm4sccPatterns = { "0011", "0101", "0110", "1001", "1010", "1100" };

        /// <summary>RM4SCC 校验字符矩阵，行=上行值-1，列=下行值-1。</summary>
        private static readonly char[,] Rm4sccMatrix =
        {
            { '0', '1', '2', '3', '4', '5' },
            { '6', '7', '8', '9', 'A', 'B' },
            { 'C', 'D', 'E', 'F', 'G', 'H' },
            { 'I', 'J', 'K', 'L', 'M', 'N' },
            { 'O', 'P', 'Q', 'R', 'S', 'T' },
            { 'U', 'V', 'W', 'X', 'Y', 'Z' }
        };

        /// <summary>RM4SCC 字符到上行/下行位串的映射（由矩阵构建）。</summary>
        private static readonly Dictionary<char, (string Top, string Bottom)> Rm4sccCharPatterns = BuildRm4sccCharPatterns();

        private static Dictionary<char, (string Top, string Bottom)> BuildRm4sccCharPatterns()
        {
            var map = new Dictionary<char, (string Top, string Bottom)>();
            for (var row = 0; row < Rm4sccPatterns.Length; row++)
            {
                for (var column = 0; column < Rm4sccPatterns.Length; column++)
                {
                    map[Rm4sccMatrix[row, column]] = (Rm4sccPatterns[row], Rm4sccPatterns[column]);
                }
            }

            return map;
        }

        /// <summary>
        /// 判断指定码制是否具有可显示的校验值。
        /// </summary>
        /// <param name="type">条码码制。</param>
        /// <returns>存在校验值时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public static bool HasChecksum(ElBarcodeGrowType type)
        {
            switch (type)
            {
                case ElBarcodeGrowType.Code128:
                case ElBarcodeGrowType.Ean13:
                case ElBarcodeGrowType.Ean8:
                case ElBarcodeGrowType.UpcA:
                case ElBarcodeGrowType.Postnet:
                case ElBarcodeGrowType.Rm4scc:
                case ElBarcodeGrowType.Isbn:
                case ElBarcodeGrowType.Issn:
                case ElBarcodeGrowType.Msi:
                case ElBarcodeGrowType.Telepen:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 把文本编码为 Code 128（B 子集）的模块宽度序列。
        /// </summary>
        /// <param name="value">待编码文本，仅支持 ASCII 32..127。</param>
        /// <returns>条/空交替的模块宽度序列，以条开始。</returns>
        public static IReadOnlyList<int> EncodeCode128B(string value)
        {
            return EncodeCode128B(value, out _);
        }

        /// <summary>
        /// 把文本编码为 Code 128（B 子集）的模块宽度序列，并输出校验值。
        /// </summary>
        /// <param name="value">待编码文本，仅支持 ASCII 32..127。</param>
        /// <param name="checksum">输出的校验码字值（0..102）。</param>
        /// <returns>条/空交替的模块宽度序列，以条开始。</returns>
        public static IReadOnlyList<int> EncodeCode128B(string value, out int checksum)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var codewords = new List<int>(value.Length + 3) { Code128StartB };

            foreach (var ch in value)
            {
                var ascii = (int)ch;
                if (ascii < 32 || ascii > 127)
                {
                    throw new ArgumentException($"Code 128（B 子集）仅支持 ASCII 32..127，非法字符：U+{ascii:X4}。", nameof(value));
                }

                // B 子集中码字值 = ASCII - 32
                codewords.Add(ascii - 32);
            }

            var sum = codewords[0];
            for (var i = 1; i < codewords.Count; i++)
            {
                sum += codewords[i] * i;
            }

            checksum = sum % 103;
            codewords.Add(checksum);
            codewords.Add(Code128Stop);

            var widths = new List<int>(codewords.Count * 6);
            foreach (var codeword in codewords)
            {
                var pattern = Code128Patterns[codeword];
                foreach (var digit in pattern)
                {
                    widths.Add(digit - '0');
                }
            }

            // Code 128 规范要求停止符后追加 2 个模块的终止条。
            // 该终止条与停止符的最后一个条同宽相连，因此直接加宽最后一个模块。
            if (widths.Count > 0)
            {
                widths[widths.Count - 1] += 2;
            }

            return widths;
        }

        /// <summary>
        /// 把文本编码为 Code 39 的模块宽度序列（自动补首尾起始/终止符 <c>*</c>）。
        /// </summary>
        /// <param name="value">待编码文本，支持大写字母、数字与 <c>- . $ / + %</c> 及空格。</param>
        /// <returns>条/空交替的模块宽度序列，以条开始。</returns>
        public static IReadOnlyList<int> EncodeCode39(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var text = value.ToUpperInvariant();
            foreach (var ch in text)
            {
                if (!Code39Patterns.ContainsKey(ch))
                {
                    throw new ArgumentException($"Code 39 不支持字符“{ch}”。", nameof(value));
                }
            }

            var payload = "*" + text + "*";
            var widths = new List<int>(payload.Length * 10);

            for (var index = 0; index < payload.Length; index++)
            {
                var pattern = Code39Patterns[payload[index]];
                foreach (var element in pattern)
                {
                    widths.Add(element == 'w' ? 2 : 1);
                }

                // 字符间以窄空白分隔（末字符之后不再追加）
                if (index != payload.Length - 1)
                {
                    widths.Add(1);
                }
            }

            return widths;
        }

        /// <summary>
        /// 把数字编码为 ITF（交叉二五码）的模块宽度序列。位数为奇数时自动前补 0。
        /// </summary>
        /// <param name="value">待编码内容，非数字字符会被忽略。</param>
        /// <returns>条/空交替的模块宽度序列，以条开始。</returns>
        public static IReadOnlyList<int> EncodeItf(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (digits.Length == 0)
            {
                throw new ArgumentException("ITF 需要数字内容。", nameof(value));
            }

            if (digits.Length % 2 != 0)
            {
                digits = "0" + digits;
            }

            const int narrow = 1;
            const int wide = 3;

            var widths = new List<int>(digits.Length * 10 + 16);

            // 起始符：窄条 窄空 窄条 窄空
            widths.Add(narrow);
            widths.Add(narrow);
            widths.Add(narrow);
            widths.Add(narrow);

            for (var i = 0; i < digits.Length; i += 2)
            {
                var bars = ItfPatterns[digits[i]];
                var spaces = ItfPatterns[digits[i + 1]];

                for (var j = 0; j < 5; j++)
                {
                    widths.Add(bars[j] == 'w' ? wide : narrow);
                    widths.Add(spaces[j] == 'w' ? wide : narrow);
                }
            }

            // 终止符：宽条 窄空 窄条
            widths.Add(wide);
            widths.Add(narrow);
            widths.Add(narrow);

            return widths;
        }

        /// <summary>
        /// 把文本编码为 Codabar 的模块宽度序列。缺少起止符时自动补 <c>A</c>…<c>B</c>。
        /// </summary>
        /// <param name="value">待编码文本，支持数字与 <c>- $ . / : +</c>，起止符为 A/B/C/D。</param>
        /// <returns>条/空交替的模块宽度序列，以条开始。</returns>
        public static IReadOnlyList<int> EncodeCodabar(string value)
        {
            var raw = (value ?? string.Empty).Trim().ToUpperInvariant();
            if (raw.Length == 0)
            {
                throw new ArgumentException("Codabar 需要非空内容。", nameof(value));
            }

            var text = HasCodabarStartStop(raw) ? raw : "A" + raw + "B";

            const int narrow = 1;
            const int wide = 3;
            var widths = new List<int>(text.Length * 8);

            for (var index = 0; index < text.Length; index++)
            {
                if (!CodabarPatterns.TryGetValue(text[index], out var pattern))
                {
                    throw new ArgumentException($"Codabar 不支持字符“{text[index]}”。", nameof(value));
                }

                // 条：4 位，1=宽
                for (var i = 0; i < 4; i++)
                {
                    widths.Add(pattern.Bars[i] == '1' ? wide : narrow);

                    // 每个条之后跟随一个空（第 4 个条除外）
                    if (i < 3)
                    {
                        // 空：3 位，0=宽
                        widths.Add(pattern.Spaces[i] == '0' ? wide : narrow);
                    }
                }

                // 字符间以窄空白分隔（末字符之后不再追加）
                if (index != text.Length - 1)
                {
                    widths.Add(narrow);
                }
            }

            return widths;
        }

        private static bool HasCodabarStartStop(string text)
        {
            if (text.Length < 2)
            {
                return false;
            }

            return IsCodabarGuard(text[0]) && IsCodabarGuard(text[text.Length - 1]);
        }

        private static bool IsCodabarGuard(char ch)
        {
            return ch == 'A' || ch == 'B' || ch == 'C' || ch == 'D';
        }

        /// <summary>
        /// 计算 EAN/UPC 系列校验位。
        /// </summary>
        /// <param name="digits">不含校验位的数字串。</param>
        /// <returns>校验位数字。</returns>
        public static int ComputeEanCheckDigit(string digits)
        {
            if (digits == null)
            {
                throw new ArgumentNullException(nameof(digits));
            }

            var sum = 0;
            var weightThree = true;

            for (var i = digits.Length - 1; i >= 0; i--)
            {
                var digit = digits[i] - '0';
                sum += weightThree ? digit * 3 : digit;
                weightThree = !weightThree;
            }

            return (10 - sum % 10) % 10;
        }

        /// <summary>
        /// 把内容编码为 EAN-13 位串（1=条，0=空）。输入 12 位时自动补校验位。
        /// </summary>
        /// <param name="value">12 位或 13 位数字。</param>
        /// <param name="checksumText">输出的校验位文本。</param>
        /// <returns>EAN-13 位串。</returns>
        public static string EncodeEan13(string value, out string checksumText)
        {
            var digits = ExtractDigits(value, "EAN-13");

            if (digits.Length != 12 && digits.Length != 13)
            {
                throw new ArgumentException("EAN-13 需要 12 位或 13 位数字。", nameof(value));
            }

            digits = EnsureCheckDigit(digits, 12, "EAN-13");
            checksumText = digits[digits.Length - 1].ToString();

            var parity = Ean13ParityPatterns[digits[0] - '0'];
            var bits = new StringBuilder(95);

            // 起始符
            bits.Append("101");

            // 左侧 6 位数据（第 2..7 位），按首位决定的奇偶模式取 L 或 G
            for (var i = 1; i <= 6; i++)
            {
                var digit = digits[i] - '0';
                bits.Append(parity[i - 1] == 'G' ? EanLeftEven[digit] : EanLeftOdd[digit]);
            }

            // 中间分隔符
            bits.Append("01010");

            // 右侧 6 位数据（第 8..13 位），统一取 R
            for (var i = 7; i <= 12; i++)
            {
                bits.Append(EanRight[digits[i] - '0']);
            }

            // 终止符
            bits.Append("101");
            return bits.ToString();
        }

        /// <summary>
        /// 把内容编码为 EAN-8 位串。输入 7 位时自动补校验位。
        /// </summary>
        /// <param name="value">7 位或 8 位数字。</param>
        /// <param name="checksumText">输出的校验位文本。</param>
        /// <returns>EAN-8 位串。</returns>
        public static string EncodeEan8(string value, out string checksumText)
        {
            var digits = ExtractDigits(value, "EAN-8");

            if (digits.Length != 7 && digits.Length != 8)
            {
                throw new ArgumentException("EAN-8 需要 7 位或 8 位数字。", nameof(value));
            }

            digits = EnsureCheckDigit(digits, 7, "EAN-8");
            checksumText = digits[digits.Length - 1].ToString();

            var bits = new StringBuilder(67);
            bits.Append("101");

            for (var i = 0; i < 4; i++)
            {
                bits.Append(EanLeftOdd[digits[i] - '0']);
            }

            bits.Append("01010");

            for (var i = 4; i < 8; i++)
            {
                bits.Append(EanRight[digits[i] - '0']);
            }

            bits.Append("101");
            return bits.ToString();
        }

        /// <summary>
        /// 把内容编码为 UPC-A 位串。输入 11 位时自动补校验位。
        /// </summary>
        /// <param name="value">11 位或 12 位数字。</param>
        /// <param name="checksumText">输出的校验位文本。</param>
        /// <returns>UPC-A 位串。</returns>
        public static string EncodeUpcA(string value, out string checksumText)
        {
            var digits = ExtractDigits(value, "UPC-A");

            if (digits.Length != 11 && digits.Length != 12)
            {
                throw new ArgumentException("UPC-A 需要 11 位或 12 位数字。", nameof(value));
            }

            digits = EnsureCheckDigit(digits, 11, "UPC-A");
            checksumText = digits[digits.Length - 1].ToString();

            var bits = new StringBuilder(95);
            bits.Append("101");

            for (var i = 0; i < 6; i++)
            {
                bits.Append(EanLeftOdd[digits[i] - '0']);
            }

            bits.Append("01010");

            for (var i = 6; i < 12; i++)
            {
                bits.Append(EanRight[digits[i] - '0']);
            }

            bits.Append("101");
            return bits.ToString();
        }

        /// <summary>
        /// 把 ISBN 编码为 EAN-13 位串（ISBN-10 补 978 前缀，ISBN-13 直接使用）。
        /// </summary>
        /// <param name="value">10 位或 13 位 ISBN。</param>
        /// <param name="checksumText">输出的校验位文本。</param>
        /// <returns>EAN-13 位串。</returns>
        public static string EncodeIsbnAsEan13(string value, out string checksumText)
        {
            var raw = ExtractLettersOrDigits(value, "ISBN").ToUpperInvariant();

            if (raw.Length == 10)
            {
                var core = raw.Substring(0, 9);
                if (!core.All(char.IsDigit))
                {
                    throw new ArgumentException("ISBN-10 格式无效。", nameof(value));
                }

                return EncodeEan13("978" + core, out checksumText);
            }

            if (raw.Length == 13)
            {
                if (!raw.All(char.IsDigit))
                {
                    throw new ArgumentException("ISBN-13 格式无效。", nameof(value));
                }

                return EncodeEan13(raw, out checksumText);
            }

            throw new ArgumentException("ISBN 需要 10 位或 13 位字符。", nameof(value));
        }

        /// <summary>
        /// 把 ISSN 编码为 EAN-13 位串（977 前缀 + 前 7 位 + 00）。
        /// </summary>
        /// <param name="value">8 位 ISSN。</param>
        /// <param name="checksumText">输出的校验位文本。</param>
        /// <returns>EAN-13 位串。</returns>
        public static string EncodeIssnAsEan13(string value, out string checksumText)
        {
            var raw = ExtractLettersOrDigits(value, "ISSN").ToUpperInvariant();
            if (raw.Length != 8)
            {
                throw new ArgumentException("ISSN 需要 8 位字符。", nameof(value));
            }

            var core = raw.Substring(0, 7);
            if (!core.All(char.IsDigit))
            {
                throw new ArgumentException("ISSN 格式无效。", nameof(value));
            }

            return EncodeEan13("977" + core + "00", out checksumText);
        }

        /// <summary>
        /// 把数字编码为 MSI（Modified Plessey）位串，并追加 Mod 10（Luhn）校验位。
        /// </summary>
        /// <param name="value">数字内容。</param>
        /// <param name="checksumText">输出的校验位文本。</param>
        /// <returns>MSI 位串。</returns>
        public static string EncodeMsiPlessey(string value, out string checksumText)
        {
            var digits = ExtractDigits(value, "MSI");

            if (digits.Length == 0)
            {
                throw new ArgumentException("MSI 需要数字内容。", nameof(value));
            }

            var check = ComputeLuhnCheckDigit(digits);
            checksumText = check.ToString(CultureInfo.InvariantCulture);
            digits += checksumText;

            var bits = new StringBuilder(digits.Length * 12 + 7);
            bits.Append("110");

            foreach (var digit in digits)
            {
                bits.Append(MsiPatterns[digit]);
            }

            bits.Append("1001");
            return bits.ToString();
        }

        /// <summary>
        /// 计算 Mod 10（Luhn）校验位。
        /// </summary>
        /// <param name="digits">不含校验位的数字串。</param>
        /// <returns>校验位数字。</returns>
        public static int ComputeLuhnCheckDigit(string digits)
        {
            if (digits == null)
            {
                throw new ArgumentNullException(nameof(digits));
            }

            var sum = 0;
            var doubleUp = true;

            for (var i = digits.Length - 1; i >= 0; i--)
            {
                var digit = digits[i] - '0';

                if (doubleUp)
                {
                    digit *= 2;
                    if (digit > 9)
                    {
                        digit -= 9;
                    }
                }

                sum += digit;
                doubleUp = !doubleUp;
            }

            return (10 - sum % 10) % 10;
        }

        /// <summary>
        /// 把文本编码为 Telepen 的模块宽度序列。
        /// </summary>
        /// <param name="value">待编码 ASCII 文本。</param>
        /// <param name="checksumText">输出的校验值文本。</param>
        /// <returns>条/空交替的模块宽度序列，以条开始。</returns>
        public static IReadOnlyList<int> EncodeTelepen(string value, out string checksumText)
        {
            var bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);

            var sum = 0;
            foreach (var b in bytes)
            {
                if (b > 0x7F)
                {
                    throw new ArgumentException("Telepen 仅支持 ASCII 字符。", nameof(value));
                }

                sum = (sum + b) % 127;
            }

            var check = (127 - sum) % 127;
            checksumText = check.ToString(CultureInfo.InvariantCulture);

            // 载荷：起始符 '_' + 数据 + 校验字节 + 终止符 'z'
            var payload = new List<byte>(bytes.Length + 3) { (byte)'_' };
            payload.AddRange(bytes);
            payload.Add((byte)check);
            payload.Add((byte)'z');

            // 生成比特流：低比特优先，最高位为偶校验位
            var bitStream = new List<int>(payload.Count * 8);
            foreach (var raw in payload)
            {
                var b = raw & 0x7F;
                var parityBit = CountSetBits(b) % 2 == 0 ? 0 : 1;
                var withParity = b | (parityBit << 7);

                for (var i = 0; i < 8; i++)
                {
                    bitStream.Add((withParity >> i) & 1);
                }
            }

            const int narrow = 1;
            const int wide = 3;
            var widths = new List<int>();

            var index = 0;
            while (index < bitStream.Count)
            {
                // "1" → 窄条 + 窄空
                if (bitStream[index] == 1)
                {
                    widths.Add(narrow);
                    widths.Add(narrow);
                    index += 1;
                    continue;
                }

                // "00" → 宽条 + 窄空
                if (index + 1 < bitStream.Count && bitStream[index + 1] == 0)
                {
                    widths.Add(wide);
                    widths.Add(narrow);
                    index += 2;
                    continue;
                }

                // "010" → 宽条 + 宽空
                if (index + 2 < bitStream.Count && bitStream[index + 1] == 1 && bitStream[index + 2] == 0)
                {
                    widths.Add(wide);
                    widths.Add(wide);
                    index += 3;
                    continue;
                }

                // 一般形式 0 1^k 0（k >= 2）
                if (index + 3 >= bitStream.Count || bitStream[index] != 0 || bitStream[index + 1] != 1)
                {
                    throw new ArgumentException("Telepen 比特流无效。", nameof(value));
                }

                var end = index + 1;
                while (end < bitStream.Count && bitStream[end] == 1)
                {
                    end++;
                }

                if (end >= bitStream.Count || bitStream[end] != 0)
                {
                    throw new ArgumentException("Telepen 比特流无效。", nameof(value));
                }

                var ones = end - (index + 1);
                if (ones < 2)
                {
                    throw new ArgumentException("Telepen 比特流无效。", nameof(value));
                }

                // 前导 "01" → 窄条 + 宽空
                widths.Add(narrow);
                widths.Add(wide);

                // 中间多余的 1 → 窄条 + 窄空
                for (var m = 0; m < ones - 2; m++)
                {
                    widths.Add(narrow);
                    widths.Add(narrow);
                }

                // 结尾 "10" → 窄条 + 宽空
                widths.Add(narrow);
                widths.Add(wide);

                index = end + 1;
            }

            return widths;
        }

        /// <summary>
        /// 编码 Pharmacode（单轨），返回条几何。支持 3..131070。
        /// </summary>
        /// <param name="value">数字内容。</param>
        /// <param name="barHeight">条高（viewBox 单位）。</param>
        /// <param name="quietZone">静区（模块数）。</param>
        /// <returns>条矩形列表与 viewBox 宽度。</returns>
        public static (IReadOnlyList<BarcodeBar> Bars, double ViewBoxWidth) EncodePharmacode(string value, double barHeight, int quietZone)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                throw new ArgumentException("Pharmacode 需要数字内容。", nameof(value));
            }

            if (number < 3 || number > 131070)
            {
                throw new ArgumentException("Pharmacode 取值必须在 3..131070 之间。", nameof(value));
            }

            // 由低位到高位推导宽窄序列，再反转
            var widths = new List<int>();
            while (number > 0)
            {
                if (number % 2 == 0)
                {
                    widths.Add(2);
                    number = (number - 2) / 2;
                }
                else
                {
                    widths.Add(1);
                    number = (number - 1) / 2;
                }
            }

            widths.Reverse();

            var x = Math.Max(0, quietZone);
            var bars = new List<BarcodeBar>(widths.Count);

            foreach (var width in widths)
            {
                bars.Add(new BarcodeBar(x, 0, width, barHeight));
                x += width + 1;
            }

            return (bars, Sanitize(x + Math.Max(0, quietZone)));
        }

        /// <summary>
        /// 编码 POSTNET，返回条几何。支持 5 / 9 / 11 位数字。
        /// </summary>
        /// <param name="value">数字内容。</param>
        /// <param name="barHeight">条高（viewBox 单位）。</param>
        /// <param name="quietZone">静区（模块数）。</param>
        /// <param name="checksumText">输出的校验位文本。</param>
        /// <returns>条矩形列表与 viewBox 宽度。</returns>
        public static (IReadOnlyList<BarcodeBar> Bars, double ViewBoxWidth) EncodePostnet(
            string value, double barHeight, int quietZone, out string checksumText)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (digits.Length != 5 && digits.Length != 9 && digits.Length != 11)
            {
                throw new ArgumentException("POSTNET 需要 5、9 或 11 位数字。", nameof(value));
            }

            var sum = digits.Sum(ch => ch - '0');
            var check = (10 - sum % 10) % 10;
            checksumText = check.ToString(CultureInfo.InvariantCulture);

            var payload = digits + checksumText;
            var fullHeight = barHeight;
            var halfHeight = barHeight / 2.0;
            var halfY = fullHeight - halfHeight;

            var x = Math.Max(0, quietZone);
            var bars = new List<BarcodeBar>();

            // 起始框条（全高）
            bars.Add(new BarcodeBar(x, 0, 1, fullHeight));
            x += 2;

            foreach (var digit in payload)
            {
                var pattern = PostnetPatterns[digit];
                for (var i = 0; i < 5; i++)
                {
                    bars.Add(pattern[i] == '1'
                        ? new BarcodeBar(x, 0, 1, fullHeight)
                        : new BarcodeBar(x, halfY, 1, halfHeight));

                    x += 2;
                }
            }

            // 终止框条（全高）
            bars.Add(new BarcodeBar(x, 0, 1, fullHeight));
            x += 1;

            return (bars, Sanitize(x + Math.Max(0, quietZone)));
        }

        /// <summary>
        /// 编码 RM4SCC，返回条几何。支持 0-9 与 A-Z。
        /// </summary>
        /// <param name="value">字母数字内容。</param>
        /// <param name="barHeight">条高（viewBox 单位）。</param>
        /// <param name="quietZone">静区（模块数）。</param>
        /// <param name="checksumText">输出的校验字符文本。</param>
        /// <returns>条矩形列表与 viewBox 宽度。</returns>
        public static (IReadOnlyList<BarcodeBar> Bars, double ViewBoxWidth) EncodeRm4scc(
            string value, double barHeight, int quietZone, out string checksumText)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var text = new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            if (text.Length == 0)
            {
                throw new ArgumentException("RM4SCC 需要字母数字内容。", nameof(value));
            }

            foreach (var ch in text)
            {
                if (!Rm4sccCharPatterns.ContainsKey(ch))
                {
                    throw new ArgumentException($"RM4SCC 不支持字符“{ch}”。", nameof(value));
                }
            }

            // 校验：分别累计上行与下行值，模 6 后取 6 代替 0
            var sumTop = 0;
            var sumBottom = 0;

            foreach (var ch in text)
            {
                var pattern = Rm4sccCharPatterns[ch];
                sumTop += Array.IndexOf(Rm4sccPatterns, pattern.Top) + 1;
                sumBottom += Array.IndexOf(Rm4sccPatterns, pattern.Bottom) + 1;
            }

            var topValue = sumTop % 6;
            topValue = topValue == 0 ? 6 : topValue;

            var bottomValue = sumBottom % 6;
            bottomValue = bottomValue == 0 ? 6 : bottomValue;

            var checkChar = Rm4sccMatrix[topValue - 1, bottomValue - 1];
            checksumText = checkChar.ToString();

            var third = barHeight / 3.0;
            var trackerY = third;
            var trackerHeight = third;

            BarcodeBar CreateBar(double x, bool top, bool bottom)
            {
                if (top && bottom)
                {
                    return new BarcodeBar(x, 0, 1, barHeight);
                }

                if (top)
                {
                    return new BarcodeBar(x, 0, 1, trackerY + trackerHeight);
                }

                if (bottom)
                {
                    return new BarcodeBar(x, trackerY, 1, barHeight - trackerY);
                }

                return new BarcodeBar(x, trackerY, 1, trackerHeight);
            }

            var position = Math.Max(0, quietZone);
            var bars = new List<BarcodeBar>();

            // 起始条（升序）
            bars.Add(CreateBar(position, top: true, bottom: false));
            position += 2;

            void AddSymbol(char ch)
            {
                var pattern = Rm4sccCharPatterns[ch];
                for (var i = 0; i < 4; i++)
                {
                    bars.Add(CreateBar(position, top: pattern.Top[i] == '1', bottom: pattern.Bottom[i] == '1'));
                    position += 2;
                }
            }

            foreach (var ch in text)
            {
                AddSymbol(ch);
            }

            AddSymbol(checkChar);

            // 终止条（降序）
            bars.Add(CreateBar(position, top: false, bottom: true));
            position += 1;

            return (bars, Sanitize(position + Math.Max(0, quietZone)));
        }

        /// <summary>
        /// 按指定码制编码内容，返回条几何、viewBox 宽度与校验值文本。
        /// </summary>
        /// <param name="type">条码码制。</param>
        /// <param name="value">待编码内容。</param>
        /// <param name="checksumText">输出的校验值文本，无校验值时为 <c>null</c>。</param>
        /// <param name="barHeight">条高（viewBox 单位）。</param>
        /// <param name="quietZone">静区（模块数）。</param>
        /// <returns>条矩形列表与 viewBox 宽度。</returns>
        public static (IReadOnlyList<BarcodeBar> Bars, double ViewBoxWidth) Encode(
            ElBarcodeGrowType type, string value, out string checksumText, double barHeight = 50, int quietZone = 10)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            checksumText = null;

            switch (type)
            {
                case ElBarcodeGrowType.Code128:
                {
                    var widths = EncodeCode128B(value, out var checksum);
                    checksumText = checksum.ToString(CultureInfo.InvariantCulture);
                    return FromWidths(widths, barHeight, quietZone);
                }

                case ElBarcodeGrowType.Code39:
                    return FromWidths(EncodeCode39(value), barHeight, quietZone);

                case ElBarcodeGrowType.Codabar:
                    return FromWidths(EncodeCodabar(value), barHeight, quietZone);

                case ElBarcodeGrowType.Itf:
                    return FromWidths(EncodeItf(value), barHeight, quietZone);

                case ElBarcodeGrowType.Ean13:
                    return FromBits(EncodeEan13(value, out checksumText), barHeight, quietZone);

                case ElBarcodeGrowType.Ean8:
                    return FromBits(EncodeEan8(value, out checksumText), barHeight, quietZone);

                case ElBarcodeGrowType.UpcA:
                    return FromBits(EncodeUpcA(value, out checksumText), barHeight, quietZone);

                case ElBarcodeGrowType.Isbn:
                    return FromBits(EncodeIsbnAsEan13(value, out checksumText), barHeight, quietZone);

                case ElBarcodeGrowType.Issn:
                    return FromBits(EncodeIssnAsEan13(value, out checksumText), barHeight, quietZone);

                case ElBarcodeGrowType.Pharmacode:
                    return EncodePharmacode(value, barHeight, quietZone);

                case ElBarcodeGrowType.Postnet:
                    return EncodePostnet(value, barHeight, quietZone, out checksumText);

                case ElBarcodeGrowType.Rm4scc:
                    return EncodeRm4scc(value, barHeight, quietZone, out checksumText);

                case ElBarcodeGrowType.Msi:
                    return FromBits(EncodeMsiPlessey(value, out checksumText), barHeight, quietZone);

                case ElBarcodeGrowType.Telepen:
                {
                    var widths = EncodeTelepen(value, out checksumText);
                    return FromWidths(widths, barHeight, quietZone);
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "不支持的条码码制。");
            }
        }

        /// <summary>
        /// 把条码渲染为完整的 SVG 字符串（不依赖浏览器，可在服务端直接生成）。
        /// </summary>
        /// <param name="type">条码码制。</param>
        /// <param name="value">待编码内容。</param>
        /// <param name="barHeight">条高（viewBox 单位）。</param>
        /// <param name="quietZone">静区（模块数）。</param>
        /// <param name="color">条与文字颜色。</param>
        /// <param name="background">背景色。</param>
        /// <param name="showValue">是否显示值文本。</param>
        /// <param name="showChecksum">是否显示校验值。</param>
        /// <param name="fontSize">值文本字号（viewBox 单位）。</param>
        /// <param name="textMarginTop">条与文本之间的间距。</param>
        /// <param name="valueStyle">值文本的内联样式。</param>
        /// <returns>SVG 字符串。</returns>
        public static string RenderSvg(
            ElBarcodeGrowType type,
            string value,
            double barHeight = 50,
            int quietZone = 10,
            string color = "#000000",
            string background = "#FFFFFF",
            bool showValue = true,
            bool showChecksum = true,
            double fontSize = 12,
            double textMarginTop = 6,
            string valueStyle = null)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var layout = ComputeLayout(type, value, barHeight, quietZone, showValue, showChecksum, fontSize, textMarginTop);
            var content = RenderContent(layout, color, background, fontSize, valueStyle);

            var builder = new StringBuilder(content.Length + 256);
            builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\"");
            builder.Append($" viewBox=\"{layout.ViewBox}\"");
            builder.Append($" width=\"{Format(layout.ViewBoxWidth)}\" height=\"{Format(layout.ViewBoxHeight)}\"");
            builder.Append(" shape-rendering=\"crispEdges\">");
            builder.Append(content);
            builder.Append("</svg>");
            return builder.ToString();
        }

        /// <summary>
        /// 生成条码的内层 SVG 标记（背景、条与值文本），不含外层 <c>&lt;svg&gt;</c> 元素。
        /// <para>
        /// <see cref="ElBarcodeGrow"/> 组件与 <see cref="RenderSvg"/> 共用此方法，因此屏幕渲染与导出结果完全一致。
        /// 另外，Razor 无法直接输出 SVG 的 <c>text</c> 元素（<c>&lt;text&gt;</c> 会被 Razor 当作文本标记、
        /// 带前缀的写法又会把前缀原样输出），所以组件在 SVG 内部同样使用本方法生成的标记。
        /// </para>
        /// </summary>
        /// <param name="layout">条码布局结果。</param>
        /// <param name="color">条与文字颜色。</param>
        /// <param name="background">背景色。</param>
        /// <param name="fontSize">值文本字号（viewBox 单位）。</param>
        /// <param name="valueStyle">值文本的内联样式。</param>
        /// <returns>内层 SVG 标记。</returns>
        public static string RenderContent(
            BarcodeLayout layout,
            string color = "#000000",
            string background = "#FFFFFF",
            double fontSize = 12,
            string valueStyle = null)
        {
            var bars = layout.Bars;
            var builder = new StringBuilder((bars?.Count ?? 0) * 80 + 256);

            builder.Append($"<rect x=\"0\" y=\"0\" width=\"{Format(layout.ViewBoxWidth)}\" height=\"{Format(layout.ViewBoxHeight)}\" fill=\"{Escape(background)}\"/>");

            if (bars != null)
            {
                foreach (var bar in bars)
                {
                    builder.Append($"<rect x=\"{Format(bar.X)}\" y=\"{Format(bar.Y)}\" width=\"{Format(bar.Width)}\" height=\"{Format(bar.Height)}\" fill=\"{Escape(color)}\"/>");
                }
            }

            if (layout.ShowText)
            {
                builder.Append($"<text x=\"50%\" y=\"{Format(layout.TextBaseline)}\" text-anchor=\"middle\" class=\"el-barcode__value\" fill=\"{Escape(color)}\" font-size=\"{Format(Math.Max(0, fontSize))}\"");

                if (!string.IsNullOrEmpty(valueStyle))
                {
                    builder.Append($" style=\"{Escape(valueStyle)}\"");
                }

                builder.Append($">{Escape(layout.Text)}</text>");
            }

            return builder.ToString();
        }

        /// <summary>
        /// 计算条码布局：条几何、viewBox 尺寸与文本排版信息。
        /// <see cref="ElBarcodeGrow"/> 组件与 <see cref="RenderSvg"/> 共用此方法，保证屏幕渲染与导出结果完全一致。
        /// </summary>
        /// <param name="type">条码码制。</param>
        /// <param name="value">待编码内容。</param>
        /// <param name="barHeight">条高（viewBox 单位）。</param>
        /// <param name="quietZone">静区（模块数）。</param>
        /// <param name="showValue">是否显示值文本。</param>
        /// <param name="showChecksum">是否显示校验值。</param>
        /// <param name="fontSize">值文本字号（viewBox 单位）。</param>
        /// <param name="textMarginTop">条与文本之间的间距。</param>
        /// <returns>条码布局结果。</returns>
        public static BarcodeLayout ComputeLayout(
            ElBarcodeGrowType type,
            string value,
            double barHeight = 50,
            int quietZone = 10,
            bool showValue = true,
            bool showChecksum = true,
            double fontSize = 12,
            double textMarginTop = 6)
        {
            value = value ?? string.Empty;

            // 空值：保留条高的垂直空间，宽度退化为 1，避免 viewBox 出现 0。
            if (value.Length == 0)
            {
                var emptyHeight = Math.Max(0, barHeight);
                return new BarcodeLayout(
                    Array.Empty<BarcodeBar>(),
                    1,
                    emptyHeight <= 0 ? 1 : emptyHeight,
                    emptyHeight,
                    0,
                    false,
                    string.Empty,
                    null);
            }

            var (bars, viewBoxWidth) = Encode(type, value, out var checksum, barHeight, quietZone);

            var barsHeight = MeasureBarsHeight(bars, barHeight);
            var checksumVisible = showValue && showChecksum && !string.IsNullOrEmpty(checksum);
            var valueVisible = showValue;
            var hasText = valueVisible || checksumVisible;

            var textHeight = hasText ? Math.Max(0, fontSize) : 0;
            var textMargin = hasText ? Math.Max(0, textMarginTop) : 0;
            var viewBoxHeight = barsHeight + (hasText ? textMargin + textHeight : 0);

            if (viewBoxHeight <= 0)
            {
                viewBoxHeight = 1;
            }

            var text = valueVisible && checksumVisible
                ? value + " " + checksum
                : (valueVisible ? value : checksum);

            return new BarcodeLayout(
                bars,
                viewBoxWidth,
                viewBoxHeight,
                barsHeight,
                barsHeight + textMargin + textHeight,
                hasText,
                hasText ? text : string.Empty,
                checksum);
        }

        /// <summary>
        /// 把文本按 XML 规则转义，避免值中的特殊字符破坏 SVG 结构。
        /// </summary>
        /// <param name="text">原始文本。</param>
        /// <returns>转义后的文本。</returns>
        public static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        /// <summary>
        /// 计算条几何的最高点，用于确定条码区域高度。
        /// </summary>
        /// <param name="bars">条矩形列表。</param>
        /// <param name="fallbackHeight">无条时的回退高度。</param>
        /// <returns>条区域高度。</returns>
        public static double MeasureBarsHeight(IReadOnlyList<BarcodeBar> bars, double fallbackHeight)
        {
            if (bars == null || bars.Count == 0)
            {
                return Math.Max(0, fallbackHeight);
            }

            var max = 0.0;
            for (var i = 0; i < bars.Count; i++)
            {
                max = Math.Max(max, bars[i].Y + bars[i].Height);
            }

            return Math.Max(0, max);
        }

        private static (IReadOnlyList<BarcodeBar> Bars, double ViewBoxWidth) FromWidths(
            IReadOnlyList<int> widths, double barHeight, int quietZone)
        {
            var quiet = Math.Max(0, quietZone);
            var bars = new List<BarcodeBar>();
            var x = (double)quiet;
            var isBar = true;

            foreach (var width in widths)
            {
                if (isBar && width > 0)
                {
                    bars.Add(new BarcodeBar(x, 0, width, barHeight));
                }

                x += width;
                isBar = !isBar;
            }

            return (bars, Sanitize(x + quiet));
        }

        private static (IReadOnlyList<BarcodeBar> Bars, double ViewBoxWidth) FromBits(
            string bits, double barHeight, int quietZone)
        {
            var quiet = Math.Max(0, quietZone);
            if (string.IsNullOrEmpty(bits))
            {
                return (Array.Empty<BarcodeBar>(), 1);
            }

            var bars = new List<BarcodeBar>();
            var index = 0;

            while (index < bits.Length)
            {
                if (bits[index] != '1')
                {
                    index++;
                    continue;
                }

                var end = index + 1;
                while (end < bits.Length && bits[end] == '1')
                {
                    end++;
                }

                bars.Add(new BarcodeBar(quiet + index, 0, end - index, barHeight));
                index = end;
            }

            return (bars, Sanitize(quiet + bits.Length + quiet));
        }

        private static string ExtractDigits(string value, string symbology)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (digits.Length == 0)
            {
                throw new ArgumentException($"{symbology} 需要数字内容。", nameof(value));
            }

            return digits;
        }

        private static string ExtractLettersOrDigits(string value, string symbology)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return new string(value.Where(char.IsLetterOrDigit).ToArray());
        }

        private static string EnsureCheckDigit(string digits, int payloadLength, string symbology)
        {
            if (digits.Length == payloadLength)
            {
                return digits + ComputeEanCheckDigit(digits).ToString(CultureInfo.InvariantCulture);
            }

            var expected = ComputeEanCheckDigit(digits.Substring(0, payloadLength));
            if (digits[payloadLength] - '0' != expected)
            {
                throw new ArgumentException($"{symbology} 校验位无效。");
            }

            return digits;
        }

        private static double Sanitize(double value)
        {
            return value <= 0 ? 1 : value;
        }

        private static int CountSetBits(int value)
        {
            var count = 0;
            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }

            return count;
        }

        /// <summary>
        /// 按不变区域（InvariantCulture）格式化数值，保证 SVG 输出不受当前区域设置影响。
        /// </summary>
        /// <param name="value">待格式化的数值。</param>
        /// <returns>数值文本。</returns>
        public static string Format(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
