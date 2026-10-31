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
    /// 找出不写 <c>timeout:</c> 的 bUnit 等待调用。规则与背景见 <see cref="TimingGuardTests"/>。
    /// </summary>
    internal static class TimingGuard
    {
        private static readonly Regex WaitCall = new Regex(@"\.WaitFor(Assertion|State|Element|Failed|Changed)\s*\(", RegexOptions.Compiled);

        public static List<string> FindBareWaitCalls(string code, string label = null)
        {
            var hits = new List<string>();
            // 🔥 先在「注释与字符串内容被抹成空格」的掩码文本上找调用，再回到原文取实参：
            // 否则被注释掉的 WaitFor（存档写法）会被误报 —— 本仓的兼容性守卫刚因为
            // 「拿原文断言」误报过三条，这条规则现在统一遵守。
            var mask = Mask(code ?? string.Empty);
            foreach (Match m in WaitCall.Matches(mask))
            {
                int open = mask.IndexOf('(', m.Index);
                int close = MatchParen(mask, open);
                if (close < 0)
                {
                    continue;
                }
                var args = code.Substring(open + 1, close - open - 1);
                if (!args.Contains("timeout"))
                {
                    hits.Add(string.Format("{0}L{1}: {2}",
                        label == null ? string.Empty : label + " ",
                        mask.Substring(0, m.Index).Split('\n').Length,
                        args.Replace("\r", " ").Replace("\n", " ").Trim().Substring(0, Math.Min(70, args.Trim().Length))));
                }
            }
            return hits;
        }

        /// <summary>把注释与字符串/字符内容替换成等长空格（偏移与行号保持不变），只留可执行代码。</summary>
        public static string Mask(string source)
        {
            var buf = new char[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                buf[i] = source[i] == '\n' ? '\n' : source[i];
            }

            int state = 0;   // 0 普通 / 1 行注释 / 2 块注释 / 3 普通串 / 4 逐字串 / 5 字符
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                char n = i + 1 < source.Length ? source[i + 1] : '\0';

                if (state == 0)
                {
                    if (c == '/' && n == '/') { state = 1; buf[i] = ' '; continue; }
                    if (c == '/' && n == '*') { state = 2; buf[i] = ' '; continue; }
                    if (c == '@' && n == '"') { state = 4; buf[i] = ' '; continue; }
                    if (c == '$' && (n == '"' || n == '@' || n == '\'')) { buf[i] = ' '; continue; }
                    if (c == '"') { state = 3; buf[i] = ' '; continue; }
                    if (c == '\'') { state = 5; buf[i] = ' '; continue; }
                    continue;
                }

                if (c == '\n')
                {
                    buf[i] = '\n';
                    if (state == 1) { state = 0; }
                    else if (state == 3 || state == 5) { state = 0; }
                    continue;
                }

                if (state == 1) { buf[i] = ' '; continue; }
                if (state == 2)
                {
                    buf[i] = ' ';
                    if (c == '*' && n == '/') { buf[i + 1] = ' '; i++; state = 0; }
                    continue;
                }
                if (state == 3)
                {
                    buf[i] = ' ';
                    if (c == '\\') { if (i + 1 < source.Length) { buf[i + 1] = ' '; i++; } }
                    else if (c == '"') { state = 0; }
                    continue;
                }
                if (state == 4)
                {
                    buf[i] = ' ';
                    if (c == '"' && n == '"') { buf[i + 1] = ' '; i++; continue; }
                    if (c == '"') { state = 0; }
                    continue;
                }
                if (state == 5)
                {
                    buf[i] = ' ';
                    if (c == '\\') { if (i + 1 < source.Length) { buf[i + 1] = ' '; i++; } }
                    else if (c == '\'') { state = 0; }
                    continue;
                }
            }

            return new string(buf);
        }

        /// <summary>字符串/字符/逐字串/注释感知的括号配平（否则 "外部引用改的标题" 这类文本会干扰）。</summary>
        private static int MatchParen(string code, int openIndex)
        {
            int depth = 0;
            for (int i = openIndex; i < code.Length; i++)
            {
                int jumped = SkipLiteral(code, i);
                if (jumped > i)
                {
                    i = jumped - 1;
                    continue;
                }
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

        private static int SkipLiteral(string code, int i)
        {
            char c = code[i];
            char n = i + 1 < code.Length ? code[i + 1] : '\0';

            if (c == '/' && n == '/')
            {
                int j = code.IndexOf('\n', i);
                return j < 0 ? code.Length : j;
            }
            if (c == '/' && n == '*')
            {
                int j = code.IndexOf("*/", i + 2, StringComparison.Ordinal);
                return j < 0 ? code.Length : j + 2;
            }
            if (c == '@' && n == '"')
            {
                int j = i + 2;
                while (j < code.Length)
                {
                    if (code[j] == '"')
                    {
                        if (j + 1 < code.Length && code[j + 1] == '"')
                        {
                            j += 2;
                            continue;
                        }
                        return j + 1;
                    }
                    j++;
                }
                return code.Length;
            }
            if (c == '$' && (n == '"' || n == '@' || n == '\''))
            {
                return i + 1;
            }
            if (c == '"' || c == '\'')
            {
                char quote = c;
                int j = i + 1;
                while (j < code.Length)
                {
                    if (code[j] == '\\')
                    {
                        j += 2;
                        continue;
                    }
                    if (code[j] == quote)
                    {
                        return j + 1;
                    }
                    if (code[j] == '\n' && quote == '"')
                    {
                        return j;   // 普通字符串不跨行（逐字串已在上一个分支处理）
                    }
                    j++;
                }
                return code.Length;
            }
            return i;
        }
    }

    /// <summary>
    /// 「等待必须显式给超时」守卫。
    ///
    /// 为什么要它：bUnit 的 WaitForAssertion / WaitForState 默认只等 <b>1 秒</b>，而全量套件是并行跑的，
    /// CPU 争抢时 1 秒不够 —— 于是出现「单类复跑必过、全量偶发红」的假失败。本仓为此白白记过一轮
    /// 「flaky 12↔13 属抖动」的噪音，真缺陷反倒被淹掉。<see cref="TestWait.Timeout"/> 是统一超时，
    /// 这条守卫保证以后新写的用例不会再退回裸调用。
    /// </summary>
    public class TimingGuardTests
    {
        private static readonly string[] TestSources = Discover();

        private static string[] Discover()
        {
            var dir = Path.Combine(RepoRoot(), "test", "Element.ComponentTests");
            return Directory.GetFiles(dir, "*.cs")
                .Where(p => !Path.GetFileName(p).StartsWith("TimingGuardTests", StringComparison.Ordinal))
                .ToArray();
        }

        [Fact]
        public void Guard_EveryWaitCallSpecifiesTimeout()
        {
            var report = new StringBuilder();
            int hits = 0;

            foreach (var file in TestSources)
            {
                foreach (var hit in TimingGuard.FindBareWaitCalls(Read(file), Path.GetFileName(file)))
                {
                    hits++;
                    report.AppendLine("   " + hit);
                }
            }

            Assert.True(hits == 0, "有 " + hits + " 处 WaitFor* 调用没写 timeout:（默认只等 1 秒，并行跑套件时会假失败）\n" + report);
        }

        /// <summary>防「假绿」：扫描器必须真的看到足量 WaitFor 调用，否则「0 命中」毫无意义。</summary>
        [Fact]
        public void Guard_ScannerSeesTheWaitCalls()
        {
            int seen = TestSources.Sum(f => Regex.Count(Read(f), @"\.WaitFor(Assertion|State|Element)\s*\("));
            Assert.True(seen >= 60, "全测试工程只识别到 " + seen + " 处 WaitFor 调用，扫描器可能已失效");
        }

        // ---------- 夹具：两侧都锁 ----------

        [Fact]
        public void Guard_Fixture_FlagsBareWaitCall()
        {
            var code = "class T { void M() { cut.WaitForAssertion(() => Assert.True(x)); } }";
            Assert.NotEmpty(TimingGuard.FindBareWaitCalls(code));
        }

        [Fact]
        public void Guard_Fixture_AllowsExplicitTimeout()
        {
            var code = "class T { void M() { cut.WaitForAssertion(() => Assert.True(x), timeout: TestWait.Timeout); } }";
            Assert.Empty(TimingGuard.FindBareWaitCalls(code));
        }

        /// <summary>实参字符串里带中文括号/引号时不能误配平（本仓夹具里就有「外部引用改的标题」这类文本）。</summary>
        [Fact]
        public void Guard_Fixture_IgnoresBracketsInsideStringsAndComments()
        {
            var withText = "class T { void M() { cut.WaitForState(() => cut.Find(\".x\").TextContent == \"标题(外)（内部）\", timeout: TestWait.Timeout); } }";
            Assert.Empty(TimingGuard.FindBareWaitCalls(withText));

            var commented = "class T { void M() { /* cut.WaitForAssertion(() => x); */ } }";
            Assert.Empty(TimingGuard.FindBareWaitCalls(commented));
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
