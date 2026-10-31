using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// /grow 章节分批建的结构守卫（<c>demo/Element.Blazor.Demo/Pages/GrowDemo.razor</c>）。
    /// <para>
    /// 背景：本页 20 个章节一次性建完会把主线程连续死住约 1 秒（实测见
    /// <c>docs/grow-progressive-section-build.md</c> 第 3 节），所以每节套了一道
    /// <c>@if (builtSections &gt; k)</c> 闸门，由 <c>OnAfterRenderAsync</c> 里的泵每 60ms 建 5 节补齐。
    /// </para>
    /// <para>防的是两类静默回退：</para>
    /// <list type="bullet">
    /// <item>加/删一个 <c>.demo-section</c> 却没同步闸门与 <c>GrowSectionCount</c>
    /// ⇒ 闸门序号 ≥ <c>GrowSectionCount</c> 的那一节<b>永远不渲染</b>：页面不报错、探针也不一定红，内容就是没了。</item>
    /// <item>把 <c>GrowSectionChunk</c>「顺手优化」成 1（看起来更平滑）
    /// ⇒ 实测长任务合计 <b>+70~90%</b>、落定多花约 3 秒：每多渲染一次就要整页重跑一次 Blazor 差分。</item>
    /// </list>
    /// <para>五条判据：G1 三个数相等；G2 闸门索引恰好 0..N-1 且按源码顺序升序；
    /// G3 每道闸门紧跟在 <c>&lt;h3&gt;</c> 之后（标题永不参与闸门）；
    /// G4 泵用独立取消源且 <c>Dispose</c> 里取消；G5 常量关系与实测最优批大小。</para>
    /// </summary>
    public class GrowSectionGateGuardTests
    {
        private static string RepoRoot([CallerFilePath] string file = null)
        {
            // .../test/Element.ComponentTests/GrowSectionGateGuardTests.cs → 上溯三层
            var directory = new FileInfo(file).Directory;
            return directory.Parent.Parent.FullName;
        }

        private static string Page() =>
            File.ReadAllText(Path.Combine(RepoRoot(), "demo", "Element.Blazor.Demo", "Pages", "GrowDemo.razor"));

        private static string[] LinesOf(string text) => text.Replace("\r\n", "\n").Split('\n');

        private static int ConstOf(string text, string name)
        {
            var match = Regex.Match(text, @"private\s+const\s+int\s+" + name + @"\s*=\s*(\d+)\s*;");
            Assert.True(match.Success, $"GrowDemo.razor 里找不到常量 {name}（判据可能已失效）");
            return int.Parse(match.Groups[1].Value);
        }

        /// <summary>每道闸门所在的行号与它守卫的章节序号，按源码顺序。</summary>
        private static List<KeyValuePair<int, int>> Gates(string[] lines)
        {
            var gates = new List<KeyValuePair<int, int>>();
            for (var i = 0; i < lines.Length; i++)
            {
                var match = Regex.Match(lines[i], @"@if\s*\(\s*builtSections\s*>\s*(\d+)\s*\)");
                if (match.Success)
                {
                    gates.Add(new KeyValuePair<int, int>(i, int.Parse(match.Groups[1].Value)));
                }
            }
            return gates;
        }

        [Fact]
        public static void Guard_SectionCountMatchesGateCountMatchesConstant()
        {
            var text = Page();
            var lines = LinesOf(text);

            var sections = Regex.Matches(text, @"<div\s+class=""demo-section""").Count;
            var gates = Gates(lines).Count;
            var declared = ConstOf(text, "GrowSectionCount");

            // 覆盖率哨兵：判据一旦写坏（比如正则改了）就会扫到 0，这里先炸而不是假绿
            Assert.True(sections >= 20, $"只扫到 {sections} 个 .demo-section，判据可能已失效");
            Assert.Contains("@code {", text);

            Assert.Equal(sections, gates);
            Assert.Equal(sections, declared);
        }

        [Fact]
        public static void Guard_GateIndicesAreExactlyZeroToNMinusOneInSourceOrder()
        {
            var text = Page();
            var indices = Gates(LinesOf(text)).Select(g => g.Value).ToArray();

            // 重复或缺口都会让某一节永不渲染，或两节共用一道闸门
            Assert.Equal(Enumerable.Range(0, indices.Length), indices);
        }

        [Fact]
        public static void Guard_EveryGateSitsRightAfterItsSectionHeading()
        {
            var lines = LinesOf(Page());
            var gates = Gates(lines);
            Assert.NotEmpty(gates);

            foreach (var gate in gates)
            {
                // 往上找最近一条非空行：必须是 <h3>。标题在闸门外，
                // 页面骨架、锚点与滚动目标才能一进来就在（这也是分批建不破坏跳转的前提）。
                var previous = gate.Key - 1;
                while (previous >= 0 && string.IsNullOrWhiteSpace(lines[previous]))
                {
                    previous--;
                }

                Assert.True(previous >= 0, $"第 {gate.Value} 道闸门上方没有内容");
                Assert.True(Regex.IsMatch(lines[previous], @"<h3>.*</h3>"),
                    $"第 {gate.Value} 道闸门（行 {gate.Key + 1}）上一条非空行不是 <h3>，而是：{lines[previous].Trim()}");
            }
        }

        [Fact]
        public static void Guard_SectionPumpUsesOwnCancellationTokenSourceAndCancelsOnDispose()
        {
            var text = Page();

            // 本仓异步自动关闭的既定写法：独立 CTS，不复用别处的 delay token
            Assert.Contains("private CancellationTokenSource? sectionPumpCts;", text);
            Assert.Contains("sectionPumpCts = new CancellationTokenSource();", text);
            Assert.Contains("var token = sectionPumpCts.Token;", text);
            Assert.Matches(@"await Task\.Delay\(\d+,\s*token\)", text);
            Assert.Contains("catch (OperationCanceledException)", text);
            Assert.Contains("void IDisposable.Dispose() => sectionPumpCts?.Cancel();", text);

            // 泵只该在首渲染起一次，否则切页签回来会重复排队
            Assert.Contains("if (!firstRender || sectionPumpCts != null || builtSections >= GrowSectionCount)", text);
            Assert.Contains("await InvokeAsync(StateHasChanged);", text);
        }

        [Fact]
        public static void Guard_ChunkSizeStaysAtMeasuredOptimum()
        {
            var text = Page();
            var count = ConstOf(text, "GrowSectionCount");
            var eager = ConstOf(text, "GrowEagerSections");
            var chunk = ConstOf(text, "GrowSectionChunk");

            Assert.InRange(eager, 1, count - 1);   // 首屏至少建 1 节，且不能一次建完（那就等于没分批）
            Assert.InRange(chunk, 1, count - eager);

            // 钉住实测值：同窗口三臂各 4 轮，一批 5 节的最长冻结 408~475ms（改前 958~1033ms），
            // 长任务合计与改前持平；一节一帧的合计 +70~90%。改这个数必须重新量，见文档第 6 节配方。
            Assert.Equal(5, chunk);
        }
    }
}
