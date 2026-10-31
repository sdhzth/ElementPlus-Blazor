using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 词条资源守卫：把「接了 i18n 但取不到文案」这类假接入挡在编译期之后。
    /// <para>
    /// 三条规则都跑在<b>仓库真实文件</b>上：① 中英两份键集必须互为相等（缺键＝切语言时静默掉回 fallback）；
    /// ② 源码里引用到的每个 <c>el.*</c> 键必须在两份文件里都存在且非空白；③ 词条值不得为空白。
    /// </para>
    /// <para>
    /// 判据全部走 <see cref="Audit"/> 这个纯函数，因此可以按「四步配方」两侧夹验：
    /// 真数据必须 0 违规，人造的坏数据必须被抓到，人造的好数据必须不被误报。
    /// </para>
    /// </summary>
    public class LangResourceGuardTests
    {
        private static readonly Regex ReferencedKey = new Regex(
            "\"(el[.:][A-Za-z0-9_:.-]+)\"", RegexOptions.CultureInvariant);

        private static string RepoRoot()
        {
            var dir = new Uri(typeof(LangResourceGuardTests).Assembly.Location).LocalPath;
            for (var i = 0; i < 10 && !string.IsNullOrEmpty(dir); i++)
            {
                if (File.Exists(Path.Combine(dir, "src", "Components", "Element.csproj")))
                {
                    return dir;
                }

                dir = Path.GetDirectoryName(dir);
            }

            throw new DirectoryNotFoundException("找不到仓库根（src/Components/Element.csproj）");
        }

        private static Dictionary<string, string> LoadLocaleFile(string path)
        {
            using (var doc = JsonDocument.Parse(File.ReadAllText(path)))
            {
                var map = new Dictionary<string, string>(StringComparer.Ordinal);
                Walk(doc.RootElement, string.Empty, map);
                return map;
            }
        }

        private static void Walk(JsonElement node, string prefix, Dictionary<string, string> map)
        {
            foreach (var property in node.EnumerateObject())
            {
                var key = prefix.Length == 0 ? property.Name : prefix + ":" + property.Name;
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    Walk(property.Value, key, map);
                }
                else if (property.Value.ValueKind == JsonValueKind.String)
                {
                    map[key] = property.Value.GetString();
                }
            }
        }

        private static Dictionary<string, IReadOnlyList<string>> CollectReferencedStatements(string root)
        {
            var hits = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "src", "Components"), "*.*", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(file);
                if (extension != ".cs" && extension != ".razor")
                {
                    continue;
                }

                if (file.Replace('\\', '/').Contains("/obj/") || file.Replace('\\', '/').Contains("/bin/"))
                {
                    continue;
                }

                var masked = MaskComments(File.ReadAllText(file));
                foreach (Match match in ReferencedKey.Matches(masked))
                {
                    var key = Normalize(match.Groups[1].Value);

                    // 片段取到本条语句结束（下一个 ';'）：插值实参常写在键之后的同一/下一行里
                    var semicolon = masked.IndexOf(';', match.Index);
                    var stop = semicolon < 0 ? Math.Min(masked.Length, match.Index + 300) : Math.Min(masked.Length, semicolon + 1);
                    var statement = masked.Substring(match.Index, stop - match.Index);

                    if (!hits.TryGetValue(key, out var list))
                    {
                        list = new List<string>();
                        hits[key] = list;
                    }

                    list.Add(statement);
                }
            }

            return hits
                .OrderBy(p => p.Key, StringComparer.Ordinal)
                .ToDictionary(
                    p => p.Key,
                    p => (IReadOnlyList<string>)p.Value.ToList(),
                    StringComparer.Ordinal);
        }

        /// <summary>
        /// 掩掉注释：整行 <c>//</c>、<c>///</c> 换成等长空格（行号不变），razor 的 <c>@* … *@@</c> 块同理。
        /// 局限：不处理字符串里恰好出现的 <c>//</c>（键名正则只吃 <c>"el…"</c> 形态，影响不到本判据）。
        /// </summary>
        internal static string MaskComments(string source)
        {
            var blanked = Regex.Replace(source, "@\\*.*?\\*@", m => new string(' ', m.Length), RegexOptions.Singleline);
            var lines = blanked.Replace("\r\n", "\n").Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//", StringComparison.Ordinal))
                {
                    lines[i] = new string(' ', lines[i].Length);
                }
            }

            return string.Join("\n", lines);
        }

        private static string Normalize(string key)
        {
            return key.Replace('.', ':');
        }

        /// <summary>纯判据：返回违规清单，空清单代表通过。</summary>
        internal static string[] Audit(IEnumerable<string> referencedKeys, Dictionary<string, string> zh, Dictionary<string, string> en)
        {
            return Audit(referencedKeys.ToDictionary(k => k, _ => (IReadOnlyList<string>)null), zh, en);
        }

        /// <summary>
        /// 判据主体。<paramref name="referencedStatements"/> 是"引用键 → 该键在源码里的各处调用片段"，
        /// 给了之后额外查两条插值规则：
        /// ① 同一个键在两份语言包里的占位符集合必须相同（不同＝切语言时会漏出 <c>{pager}</c> 这种字面量）；
        /// ② 引用了含占位符的词条却没传对应参数（挡住"加了插值词条但忘了传参"）。
        /// 只要有一处调用漏了参数就算违规 —— 否则同一个键在 A 页正常、在 B 页露出花括号，更难复现。
        /// </summary>
        internal static string[] Audit(IReadOnlyDictionary<string, IReadOnlyList<string>> referencedStatements, Dictionary<string, string> zh, Dictionary<string, string> en)
        {
            var problems = new List<string>();

            foreach (var key in zh.Keys.Where(k => !en.ContainsKey(k)).OrderBy(k => k, StringComparer.Ordinal))
            {
                problems.Add("只在 zh-CN 存在：" + key);
            }

            foreach (var key in en.Keys.Where(k => !zh.ContainsKey(k)).OrderBy(k => k, StringComparer.Ordinal))
            {
                problems.Add("只在 en-US 存在：" + key);
            }

            foreach (var pair in zh.OrderBy(p => p.Key, StringComparer.Ordinal).Concat(en.OrderBy(p => p.Key, StringComparer.Ordinal)))
            {
                if (string.IsNullOrWhiteSpace(pair.Value))
                {
                    problems.Add("词条值为空白：" + pair.Key);
                }
            }

            foreach (var key in zh.Keys.Where(en.ContainsKey).OrderBy(k => k, StringComparer.Ordinal))
            {
                var a = Placeholders(zh[key]);
                var b = Placeholders(en[key]);
                if (!a.SequenceEqual(b))
                {
                    problems.Add("占位符不一致：" + key + " zh={" + string.Join(",", a) + "} en={" + string.Join(",", b) + "}");
                }
            }

            var expectedPlaceholders = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            foreach (var pair in referencedStatements)
            {
                var names = Placeholders(zh.TryGetValue(pair.Key, out var zv) ? zv : string.Empty)
                    .Concat(Placeholders(en.TryGetValue(pair.Key, out var ev) ? ev : string.Empty))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                expectedPlaceholders[pair.Key] = names;

                if (!zh.ContainsKey(pair.Key))
                {
                    problems.Add("源码引用但 zh-CN 缺键：" + pair.Key);
                }

                if (!en.ContainsKey(pair.Key))
                {
                    problems.Add("源码引用但 en-US 缺键：" + pair.Key);
                }

                if (pair.Value == null || names.Count == 0)
                {
                    continue;   // 夹具只查键存在性；无占位符的键不需要参数
                }

                foreach (var statement in pair.Value)
                {
                    foreach (var name in names)
                    {
                        if (statement.IndexOf("\"" + name + "\"", StringComparison.Ordinal) < 0)
                        {
                            problems.Add("引用了带 {" + name + "} 的词条却没传该参数：" + pair.Key);
                        }
                    }
                }
            }

            return problems.ToArray();
        }

        private static readonly Regex Placeholder = new Regex(@"\{(\w+)\}", RegexOptions.CultureInvariant);

        /// <summary>按出现顺序去重（错误信息里的顺序要稳定，否则两次跑输出不一样）。</summary>
        private static List<string> Placeholders(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<string>();
            }

            return Placeholder.Matches(text).Select(m => m.Groups[1].Value).Distinct(StringComparer.Ordinal).ToList();
        }


        [Fact]
        public void BothLocaleFilesParseAndHaveParityWithSourceReferences()
        {
            var root = RepoRoot();
            var zh = LoadLocaleFile(Path.Combine(root, "src", "Components", "wwwroot", "lang", "zh-CN.json"));
            var en = LoadLocaleFile(Path.Combine(root, "src", "Components", "wwwroot", "lang", "en-US.json"));
            var referenced = CollectReferencedStatements(root);

            // 覆盖断言：正则与文件定位真的看到了东西，否则 0 违规只是失效的同义词
            Assert.True(referenced.Count >= 12, "只扫到 " + referenced.Count + " 个 el.* 引用点，守卫可能已失效：" + string.Join(",", referenced.Keys));
            Assert.True(zh.Count >= 30 && en.Count >= 30, "词条数异常（zh=" + zh.Count + ", en=" + en.Count + "）");
            Assert.Contains("el:inputNumber:decrease", referenced.Keys);
            Assert.Contains("el:carousel:leftArrow", referenced.Keys);

            // 插值规则不能是"零输入的空转"：至少要有一条被引用的键带占位符
            var interpolated = referenced.Keys
                .Where(k => zh.TryGetValue(k, out var z) && Placeholders(z).Count > 0)
                .ToList();
            Assert.True(interpolated.Count >= 3,
                "只找到 " + interpolated.Count + " 条带占位符的被引用键，插值判据实际没在被检验：" + string.Join(",", interpolated));

            var problems = Audit(referenced, zh, en);
            Assert.True(problems.Length == 0, "词条资源体检失败：\n   " + string.Join("\n   ", problems));
        }

        [Fact]
        public void AuditCatchesEveryRealViolationShape()
        {
            // 反向注入：三种真会出问题的形态，一种都不许漏
            var zh = new Dictionary<string, string>
            {
                { "el.inputNumber.decrease", "减少数值" },
                { "el.inputNumber.increase", "增加数值" },
                { "el.onlyZh", "中文独有" },
                { "el.blank", "   " },
            };
            var en = new Dictionary<string, string>
            {
                { "el.inputNumber.decrease", "decrease number" },
                { "el.inputNumber.increase", "increase number" },
                { "el.onlyEn", "english only" },
                { "el.blank", "" },
            };

            var problems = Audit(new[] { "el.missing" }, zh, en);

            Assert.Contains(problems, p => p.Contains("只在 zh-CN 存在：el.onlyZh"));
            Assert.Contains(problems, p => p.Contains("只在 en-US 存在：el.onlyEn"));
            Assert.Contains(problems, p => p.Contains("源码引用但 zh-CN 缺键：el.missing"));
            Assert.Contains(problems, p => p.Contains("源码引用但 en-US 缺键：el.missing"));
            // 2 个单边键 + 2 个空白值（zh 的 "   " 与 en 的 ""各一次）+ 2 个引用缺键
            Assert.Equal(6, problems.Length);
        }

        [Fact]
        public void AuditDoesNotFlagAWellFormedFixture()
        {
            // 不误报：点写法的引用与冒号写法的键集必须能对上
            var zh = new Dictionary<string, string> { { "el:inputNumber:decrease", "减少数值" } };
            var en = new Dictionary<string, string> { { "el:inputNumber:decrease", "decrease number" } };

            Assert.Empty(Audit(new[] { "el:inputNumber:decrease" }, zh, en));
        }

        /// <summary>
        /// 掩码两侧的夹具：文档注释与行注释里的假键必须被抹掉（否则每个写文档的人都会误报一条），
        /// 真代码里的引用必须保留，且掩码不能改变行数（行号漂移会让"哪份文件哪一行"这条线索失效）。
        /// </summary>
        /// <summary>
        /// 插值两条新规则的"必抓到"侧：两份语言包占位符不一致、以及引用了带占位符的词条却没传参。
        /// </summary>
        [Fact]
        public void AuditCatchesInterpolationMistakes()
        {
            var zh = new Dictionary<string, string>
            {
                { "el.pagination.total", "共 {total} 条" },
                { "el.pagination.currentPage", "第 {pager} 页" },
            };
            var en = new Dictionary<string, string>
            {
                { "el.pagination.total", "Total {total}" },
                // 少了一个占位符：切到 en-US 时 "page" 会露出 {page}
                { "el.pagination.currentPage", "page" },
            };

            var referenced = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                // 忘了传 ("pager", ...)
                { "el.pagination.currentPage", new[] { "Lang.T(\"el.pagination.currentPage\", \"页码\")" } },
                { "el.pagination.total", new[] { "Lang.T(\"el.pagination.total\", \"共 {total} 条\", (\"total\", Total))" } },
            };

            var problems = Audit(referenced, zh, en);

            Assert.Contains(problems, p => p.Contains("占位符不一致：el.pagination.currentPage"));
            Assert.Contains(problems, p => p.Contains("引用了带 {pager} 的词条却没传该参数：el.pagination.currentPage"));
            Assert.DoesNotContain(problems, p => p.Contains("el.pagination.total"));
        }

        /// <summary>插值规则的"不误报"侧：参数名写在下一行（跨行调用）也必须认。</summary>
        [Fact]
        public void AuditAcceptsMultiLineInterpolationCalls()
        {
            var zh = new Dictionary<string, string> { { "el.input.characters", "{count} / {max} 个字符" } };
            var en = new Dictionary<string, string> { { "el.input.characters", "{count} / {max} characters" } };

            var statement =
                "Lang.T(\r\n    \"el.input.characters\", \"{count} / {max}\",\r\n    (\"count\", TextLength), (\"max\", Maxlength));";

            var referenced = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                { "el.input.characters", new[] { statement } },
            };

            Assert.Empty(Audit(referenced, zh, en));
        }

        [Fact]
        public void MaskCommentsHidesDocExamplesButKeepsRealReferences()
        {
            const string Source =
                "public class X\n" +
                "{\n" +
                "    /// 用法示例 <c>Lang.Text(\"el.fake.doc\", 参数, \"字面量\")</c>\n" +
                "    // 行注释里的 \"el.fake.line\" 也不算\n" +
                "    private const string K = \"el.select.noData\"; // 尾注释里的 \"el.fake.trailing\"\n" +
                "}\n";

            var masked = MaskComments(Source);

            Assert.DoesNotContain("el.fake.doc", masked);
            Assert.DoesNotContain("el.fake.line", masked);
            Assert.Contains("el.select.noData", masked);
            Assert.Equal(Source.Split('\n').Length, masked.Split('\n').Length);
        }

        /// <summary>razor 块注释 @* … *@ 里的假键同样不算引用。</summary>
        [Fact]
        public void MaskCommentsHidesRazorBlockComments()
        {
            const string Source = "@* 这里写 \"el.fake.razor\" 只是说明 *@\n<span>@Lang.T(\"el.select.loading\", \"加载中\")</span>\n";
            var masked = MaskComments(Source);

            Assert.DoesNotContain("el.fake.razor", masked);
            Assert.Contains("el.select.loading", masked);
        }
    }
}
