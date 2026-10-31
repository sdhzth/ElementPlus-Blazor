using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 探针脚本自身卫生守卫（attic 治理的机器化）。防的是这类缓慢回退：
    /// 新会话图省事又写死端口 → 主目录重新堆满"换端口就跑不动"的脚本；
    /// 或者 attic 里的脚本被文档重新引用 → 它已经是回归资产却还躺在存档目录里。
    ///
    /// 三条规则（现状全部 0 违规，所以是硬断言）：
    /// 🔎 H1 主目录探针若代码里连本地演示站（<c>://localhost</c> / <c>://127.0.0.1</c>），
    ///     就必须能覆盖地址：<c>process.argv[2]</c> 或 <c>process.env.*BASE</c>。
    ///     <b>只写在注释里不算</b>（判据跑在 <see cref="TestJsScanner.StripComments"/> 之后）。
    ///     不连本地站的探针（访问 element-plus.org 的外部基准探针、用 setContent 喂内联 HTML 的）不适用本条。
    /// 🔎 H2 <c>attic/</c> 里的脚本不得被 <c>CHANGELOG.md</c> 或 <c>docs/*.md</c> 点名 —— 一旦被引用就说明它复活成回归资产，应移回主目录。
    /// 🔎 H3 主目录探针不得拿 <c>#app</c> 当就绪判据：Blazor 尚未启动时 <c>#app</c> 就已存在于静态 index.html，等它等于没等。
    ///
    /// 防假绿：要求扫到足量文件（主目录连本地站 ≥120 份、attic ≥40 份），路径或判据失效时本类会先炸。
    /// </summary>
    public class ProbeHygieneGuardTests
    {
        private static readonly string ProbesDir = Path.Combine(RepoRoot(), "demo", "tools", "probes");
        private static readonly string AtticDir = Path.Combine(ProbesDir, "attic");
        private static readonly string[] MainProbes = ListCjs(ProbesDir);
        private static readonly string[] AtticProbes = ListCjs(AtticDir);

        private static readonly Regex BaseOverride = new Regex(
            @"process\.argv\[2\]|process\.env\.\w*BASE", RegexOptions.Compiled);
        private static readonly Regex LocalServer = new Regex(
            @"://(?:localhost|127\.0\.0\.1)", RegexOptions.Compiled);
        private static readonly Regex AppAsReadiness = new Regex(
            @"waitForSelector\(\s*['""]#app|waitForFunction\([^)]*#app|querySelector(All)?\(\s*['""]#app",
            RegexOptions.Compiled);

        // ---------- 判据（纯函数，供夹具直接调用） ----------

        internal static string[] FindHardcodedLocalProbes(Dictionary<string, string> codeByFile)
        {
            var bad = new List<string>();
            foreach (var pair in codeByFile)
            {
                var code = TestJsScanner.StripComments(pair.Value);
                if (!LocalServer.IsMatch(code))
                {
                    continue;   // 不连本地站：外部基准探针 / 内联 HTML 探针
                }
                if (!BaseOverride.IsMatch(code))
                {
                    bad.Add(pair.Key);
                }
            }
            return bad.ToArray();
        }

        internal static string[] FindReferencedAtticFiles(string corpus, IEnumerable<string> atticFiles)
        {
            return atticFiles.Where(f => corpus.IndexOf(f, StringComparison.Ordinal) >= 0).ToArray();
        }

        internal static string[] FindAppAsReadinessProbes(Dictionary<string, string> codeByFile)
        {
            var bad = new List<string>();
            foreach (var pair in codeByFile)
            {
                if (AppAsReadiness.IsMatch(TestJsScanner.StripComments(pair.Value)))
                {
                    bad.Add(pair.Key);
                }
            }
            return bad.ToArray();
        }

        // ---------- 全仓硬断言 ----------

        [Fact]
        public void Guard_EveryActiveProbeCanOverrideBaseUrl()
        {
            var code = new Dictionary<string, string>();
            foreach (var file in MainProbes)
            {
                code[Path.GetFileName(file)] = Read(file);
            }

            var local = code.Count(kv => LocalServer.IsMatch(TestJsScanner.StripComments(kv.Value)));
            var bad = FindHardcodedLocalProbes(code);

            Assert.True(local >= 120, "只扫到 " + local + " 份连本地演示站的探针，疑似目录或判据失效");
            Assert.True(bad.Length == 0, bad.Length + " 份现行探针把本地演示站地址写死了（换端口跑不动）：\n   " + string.Join("\n   ", bad));
        }

        [Fact]
        public void Guard_AtticProbesAreNotReferencedAsRegressionEvidence()
        {
            var bad = FindReferencedAtticFiles(DocumentationCorpus(), AtticProbes.Select(Path.GetFileName));

            Assert.True(bad.Length == 0, "attic 里有 " + bad.Length + " 份已被 CHANGELOG/docs 点名 —— 它们已是回归资产，应移回主目录：\n   "
                + string.Join("\n   ", bad));
        }

        [Fact]
        public void Guard_NoProbeWaitsOnTheStaticAppHost()
        {
            var code = MainProbes.ToDictionary(file => Path.GetFileName(file), Read);
            var bad = FindAppAsReadinessProbes(code);

            Assert.True(bad.Length == 0, "探针拿 #app 当就绪判据等于没在等 Blazor（静态 HTML 里它一直在），要等真渲染出的 class：\n   "
                + string.Join("\n   ", bad));
        }

        /// <summary>防假绿：路径或判据一旦失效，上面三条会"因为没有文件而全部通过"。</summary>
        [Fact]
        public void Guard_ScannerSeesTheProbeDirectories()
        {
            Assert.True(Directory.Exists(ProbesDir), "找不到探针目录：" + ProbesDir);
            Assert.True(Directory.Exists(AtticDir), "找不到 attic 目录：" + AtticDir);
            Assert.True(MainProbes.Length >= 140, "主目录只扫到 " + MainProbes.Length + " 份 .cjs");
            Assert.True(AtticProbes.Length >= 40, "attic 只扫到 " + AtticProbes.Length + " 份 .cjs");
        }

        // ---------- 两侧夹具：该抓的抓到、不许误报 ----------

        [Fact]
        public void Guard_H1_BothSides()
        {
            // 必须抓到
            Assert.Equal(new[] { "bad.cjs" }, FindHardcodedLocalProbes(new Dictionary<string, string>
            {
                ["bad.cjs"] = "await page.goto('http://127.0.0.1:5199/table');",
            }));

            // argv / env 两种覆盖方式都算合格
            Assert.Empty(FindHardcodedLocalProbes(new Dictionary<string, string>
            {
                ["a.cjs"] = "const BASE = process.argv[2] || 'http://127.0.0.1:5199';\npage.goto(BASE + '/table');",
                ["b.cjs"] = "const BASE = process.env.EB_BASE || 'http://127.0.0.1:5199';",
            }));

            // 注释里写"以后支持 argv"不算实现（掩码的价值）
            Assert.Equal(new[] { "fake.cjs" }, FindHardcodedLocalProbes(new Dictionary<string, string>
            {
                ["fake.cjs"] = "// 以后可以支持 process.argv[2]\n/* process.argv[2] */\nawait page.goto('http://localhost:5199/x');",
            }));

            // 不连本地站的探针不适用（外部基准 / 内联 HTML），不许误报
            Assert.Empty(FindHardcodedLocalProbes(new Dictionary<string, string>
            {
                ["ext.cjs"] = "await page.goto('https://element-plus.org/zh-CN/component/button');",
                ["inline.cjs"] = "await page.setContent('<div class=\"el-button\">x</div>');",
            }));
        }

        [Fact]
        public void Guard_H2_BothSides()
        {
            // 文档里真提到了某个 attic 文件名 → 该文件不再是"无人引用"，应移回主目录
            Assert.Equal(new[] { "diag-x.cjs" }, FindReferencedAtticFiles(
                "CHANGELOG：diag-x.cjs 24 断言通过；另附一份截图。", new[] { "diag-x.cjs", "diag-y.cjs" }));
            // 文档没提 → 留在 attic 合理
            Assert.Empty(FindReferencedAtticFiles("attic 里放着 48 份一次性脚本，判据见手册", new[] { "diag-x.cjs", "diag-y.cjs" }));
        }

        [Fact]
        public void Guard_H3_BothSides()
        {
            Assert.Equal(new[] { "w.cjs" }, FindAppAsReadinessProbes(new Dictionary<string, string>
            {
                ["w.cjs"] = "await page.waitForSelector('#app', { timeout: 90000 });",
            }));
            Assert.Empty(FindAppAsReadinessProbes(new Dictionary<string, string>
            {
                ["ok.cjs"] = "// 注意 #app 静态就存在\nawait page.waitForSelector('.demo-section');",
            }));
        }

        // ---------- 支撑 ----------

        private static string[] ListCjs(string dir)
        {
            return Directory.Exists(dir)
                ? Directory.GetFiles(dir, "*.cjs").OrderBy(p => p, StringComparer.Ordinal).ToArray()
                : Array.Empty<string>();
        }

        private static string DocumentationCorpus()
        {
            var builder = new StringBuilder();
            builder.Append(Read(Path.Combine(RepoRoot(), "CHANGELOG.md")));
            foreach (var doc in Directory.GetFiles(Path.Combine(RepoRoot(), "docs"), "*.md"))
            {
                builder.Append(Read(doc));
            }
            return builder.ToString();
        }

        /// <summary>仓库源码与文档编码混用（GBK 无 BOM 与 UTF-8 并存），读文件必须编码回退。</summary>
        private static string Read(string path)
        {
            var bytes = File.ReadAllBytes(path);
            try
            {
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                try
                {
                    return Encoding.GetEncoding("GBK").GetString(bytes);
                }
                catch (Exception)
                {
                    return Encoding.UTF8.GetString(bytes);
                }
            }
        }

        private static string RepoRoot([CallerFilePath] string callerFilePath = null)
        {
            var dir = new FileInfo(callerFilePath ?? string.Empty).Directory;
            return dir.Parent.Parent.FullName;
        }
    }
}
