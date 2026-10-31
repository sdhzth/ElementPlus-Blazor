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
    /// 找出不给结果的 <c>JSInterop.SetupVoid(...)</c> 注册。规则与背景见 <see cref="VoidInteropGuardTests"/>。
    /// </summary>
    internal static class VoidInteropGuard
    {
        private static readonly Regex SetupCall = new Regex(@"JSInterop\.SetupVoid\s*\(", RegexOptions.Compiled);

        /// <summary>返回「同一语句里没有 .SetVoidResult()」的 SetupVoid 调用位置。</summary>
        public static List<string> FindDanglingSetups(string code, string label = null)
        {
            var hits = new List<string>();
            // 与 TimingGuard 同一条口径：先在「注释与字符串抹成空格」的掩码文本上找，
            // 否则被注释掉的注册、以及标识符字符串里的 "SetupVoid" 都会造成误报。
            var mask = TimingGuard.Mask(code ?? string.Empty);
            foreach (Match m in SetupCall.Matches(mask))
            {
                int open = mask.IndexOf('(', m.Index);
                int close = MatchParen(mask, open);
                if (close < 0)
                {
                    continue;
                }

                // 语句终点：右括号之后到第一个分号。链式写法 .SetVoidResult() 必须落在这段里。
                int semicolon = mask.IndexOf(';', close);
                if (semicolon < 0)
                {
                    semicolon = mask.Length;
                }
                var statement = mask.Substring(m.Index, semicolon - m.Index + 1);
                if (!statement.Contains("SetVoidResult"))
                {
                    hits.Add(string.Format("{0}L{1}: {2}",
                        label == null ? string.Empty : label + " ",
                        mask.Substring(0, m.Index).Split('\n').Length,
                        code.Substring(m.Index, Math.Min(90, semicolon - m.Index + 1)).Replace("\r", " ").Replace("\n", " ").Trim()));
                }
            }
            return hits;
        }

        /// <summary>字符串/字符/逐字串/注释感知的括号配平（复用 TimingGuard 的口径，不另起一套）。</summary>
        private static int MatchParen(string code, int openIndex)
        {
            int depth = 0;
            for (int i = openIndex; i < code.Length; i++)
            {
                if (code[i] == '(')
                {
                    depth++;
                }
                else if (code[i] == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
    }

    /// <summary>
    /// 「<c>SetupVoid</c> 必须马上 <c>SetVoidResult()</c>」守卫。
    ///
    /// 为什么要它：<c>JSInterop.SetupVoid("execFocus", _ =&gt; true);</c> 只登记了「这次调用由我接管」，
    /// 没给结果 —— 于是组件里那句 <c>await InvokeVoidAsync("execFocus", …)</c> 永远悬着。
    /// bUnit 约 1 秒后把它置成 <c>JSRuntimeInvocationNotSetException</c> 并记成未处理渲染异常，
    /// 在之后任意一次派发/等待里重新抛出，<b>把「断言其实已经成立」盖成红</b>。
    /// 后果是「单类复跑必过、满载偶发红」的假失败，而且错误文案是 JSInterop 而不是断言本身，
    /// 极易被误判成组件缺陷（本轮 <c>ElRadioTests</c> 两条即如此，取证见
    /// <c>docs/testing-guards.md</c> 第 51 条：悬空档 3/3 在 1007/1018/1020ms 抛 JS 异常，
    /// 补结果档 3/3 在 6006/6014/6010ms 才抛真实的等待超时）。
    /// </summary>
    public class VoidInteropGuardTests
    {
        private static readonly string[] TestSources = Discover();

        private static string[] Discover()
        {
            var dir = Path.Combine(RepoRoot(), "test", "Element.ComponentTests");
            return Directory.GetFiles(dir, "*.cs")
                .Where(p => !Path.GetFileName(p).StartsWith("VoidInteropGuardTests", StringComparison.Ordinal))
                .ToArray();
        }

        [Fact]
        public void Guard_EverySetupVoidCompletesItsResult()
        {
            var report = new StringBuilder();
            int hits = 0;

            foreach (var file in TestSources)
            {
                foreach (var hit in VoidInteropGuard.FindDanglingSetups(Read(file), Path.GetFileName(file)))
                {
                    hits++;
                    report.AppendLine("   " + hit);
                }
            }

            Assert.True(hits == 0, "有 " + hits + " 处 JSInterop.SetupVoid 没写 .SetVoidResult()"
                + "（那次 interop 会永远悬着，bUnit ~1s 后抛 JSRuntimeInvocationNotSetException 盖掉真实断言结果）\n" + report);
        }

        /// <summary>防「假绿」：扫描器必须真的看到足量 SetupVoid 注册，否则「0 命中」毫无意义。</summary>
        [Fact]
        public void Guard_ScannerSeesTheSetupVoidCalls()
        {
            int seen = TestSources.Sum(f => Regex.Count(Read(f), @"JSInterop\.SetupVoid\s*\("));
            Assert.True(seen >= 25, "全测试工程只识别到 " + seen + " 处 SetupVoid 注册，扫描器可能已失效");
        }

        // ---------- 夹具：两侧都锁 ----------

        [Fact]
        public void Guard_Fixture_FlagsDanglingSetup()
        {
            var code = "class T { T() { JSInterop.SetupVoid(\"execFocus\", _ => true); } }";
            Assert.NotEmpty(VoidInteropGuard.FindDanglingSetups(code));
        }

        [Fact]
        public void Guard_Fixture_AllowsCompletedSetup()
        {
            var code = "class T { T() { JSInterop.SetupVoid(\"execFocus\", _ => true).SetVoidResult(); } }";
            Assert.Empty(VoidInteropGuard.FindDanglingSetups(code));
        }

        /// <summary>被注释掉的注册不能被当成违规（和 TimingGuard 同一坑）。</summary>
        [Fact]
        public void Guard_Fixture_IgnoresCommentedOutSetup()
        {
            var code = "class T { T() { /* JSInterop.SetupVoid(\"execFocus\", _ => true); */ } }";
            Assert.Empty(VoidInteropGuard.FindDanglingSetups(code));
        }

        private static string Read(string path)
        {
            var bytes = File.ReadAllBytes(path);
            try
            {
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return Encoding.UTF8.GetString(bytes);
            }
        }

        private static string RepoRoot([CallerFilePath] string callerFilePath = null)
        {
            var dir = new FileInfo(callerFilePath ?? string.Empty).Directory;
            return dir.Parent.Parent.FullName;
        }
    }
}
