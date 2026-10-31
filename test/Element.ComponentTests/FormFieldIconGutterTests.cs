using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 字段壳「图标槽位」守卫（批次㊽）。
    /// <para>
    /// 宿主 <c>zyweb</c> 员工编辑「入厂日期」= <see cref="ElFormFieldGrow"/> 里放 <c>ElDatePicker</c>，
    /// 用户反馈原文：<c>http://localhost:5171/baseinfo/employeeinfo</c> 「点击编辑进去，2026 下面是不是还有东西」。
    /// 那个「东西」是日历前缀图标压在日期文本上（<c>Temp/b57/date_val_clip.png</c> 改前 3× 裁图，
    /// 图标整个盖在「2026-10-10」的 0 上；<c>Temp/b57/date_post_clip.png</c> 改后让到文本左侧）。
    /// </para>
    /// <para>
    /// 真身在 CSS 特异度：<c>.el-form-field:not(.el-form-field--keep-control) &gt; .el-form-field__content .el-input__inner</c>
    /// 这条「内层控件去壳」规则写 <c>padding: 0</c>，特异度 (0,4,0)；而 Element 给图标让位的两条
    /// <c>.el-input--prefix .el-input__inner{padding-left:30px}</c> / <c>.el-input--suffix .el-input__inner{padding-right:30px}</c>
    /// 只有 (0,2,0)，于是被打掉。<c>.el-input__prefix</c> / <c>.el-input__suffix</c> 是 <c>position:absolute</c>
    /// 浮在 input 之上（<c>left:5px</c> / <c>right:5px</c>，图标字形 25px 宽 ⇒ 槽位正好 30px），
    /// 文本于是从 x=0 起画、压在图标底下。
    /// </para>
    /// <para>
    /// bUnit 没有布局引擎（见 <c>docs/testing-guards.md</c>），量不到重叠；本文件守的是静态事实：
    /// 补槽的两条规则在、值对、特异度严格高于那条 <c>padding: 0</c>、且写在它后面。
    /// 几何与「文本压没压图标」由宿主侧浏览器探针为证（<c>_tmp_probe/b57_gutter_pre_run.log</c> /
    /// <c>b57_gutter_post2_run.log</c>：改前 17/37 个控件压图标，改后 0 个；纯文本格槽位仍 0/0）。
    /// </para>
    /// </summary>
    public class FormFieldIconGutterTests
    {
        private static string RepoRoot([CallerFilePath] string file = null)
        {
            // .../test/Element.ComponentTests/FormFieldIconGutterTests.cs → 上溯三层
            var directory = new FileInfo(file).Directory;
            return directory.Parent.Parent.FullName;
        }

        private static string Css() =>
            File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "css", "index.css"));

        /// <summary>去注释后的 CSS：本仓注释里会写出被禁止的选择器/属性名，不剥会误报。</summary>
        private static string CssNoComments() =>
            Regex.Replace(Css(), @"/\*[\s\S]*?\*/", string.Empty);

        /// <summary>
        /// 取某条规则的选择器 + 声明体。<paramref name="selector"/> 必须在行首（选择器组一行一条），
        /// 否则会命中另一条规则里的同名子串。
        /// </summary>
        private static (string Selector, string Body) Rule(string selector)
        {
            var css = CssNoComments();
            var index = -1;
            var from = 0;
            var lineStart = 0;
            while (true)
            {
                index = css.IndexOf(selector, from, System.StringComparison.Ordinal);
                Assert.True(index >= 0, $"index.css 中未找到行首规则：{selector}");

                lineStart = css.LastIndexOf('\n', index) + 1;
                if (System.String.IsNullOrEmpty(css.Substring(lineStart, index - lineStart).Trim()))
                {
                    break;
                }

                from = index + 1;
            }

            var open = css.IndexOf('{', index);
            var close = css.IndexOf('}', open);
            var selectorText = css.Substring(lineStart, index - lineStart + selector.Length).Trim();
            return (selectorText, css.Substring(open + 1, close - open - 1));
        }

        /// <summary>特异度的 class 位：每个 <c>.foo</c> 计 1，<c>:not(...)</c> 内部同样按 class 计。</summary>
        private static int ClassSpecificity(string selector) =>
            Regex.Matches(selector, @"\.[A-Za-z_-][\w-]*").Count;

        private const string DeShell =
            ".el-form-field:not(.el-form-field--keep-control) > .el-form-field__content .el-input__inner";

        private const string PrefixGutter =
            ".el-form-field:not(.el-form-field--keep-control) > .el-form-field__content .el-input--prefix > .el-input__inner";

        private const string SuffixGutter =
            ".el-form-field:not(.el-form-field--keep-control) > .el-form-field__content .el-input--suffix > .el-input__inner";

        [Fact]
        public void DeShellRuleStillZeroesPadding()
        {
            // 前提：去壳这条还在（批次㊽ 不打算撤销它，只是把图标槽补回来）。它要是被删了，下面两条就失去意义。
            var (selector, body) = Rule(DeShell);
            Assert.Matches(@"padding\s*:\s*0", body);
            Assert.Equal(4, ClassSpecificity(selector));
        }

        [Theory]
        [InlineData(PrefixGutter, "padding-left")]
        [InlineData(SuffixGutter, "padding-right")]
        public void IconGutterRestoresEpCaliberAndBeatsDeShell(string selectorText, string property)
        {
            var (selector, body) = Rule(selectorText);

            // Element 原口径：图标字形 25px + left/right:5px ⇒ 30px 槽位不多不少
            Assert.Equal(30, int.Parse(Regex.Match(body, property + @"\s*:\s*(\d+)px").Groups[1].Value));

            // 必须严格赢过那条 padding:0：特异度更高，且写在它后面（同特异度时后来者胜，这里是双保险）
            Assert.True(ClassSpecificity(selector) > ClassSpecificity(DeShell),
                $"补槽规则的特异度必须高于 padding:0 那条：{selector}");
            Assert.Contains(".el-input--" + (property == "padding-left" ? "prefix" : "suffix"), selector);
            var css = CssNoComments();
            Assert.True(css.IndexOf(selectorText, System.StringComparison.Ordinal) >
                        css.IndexOf(DeShell, System.StringComparison.Ordinal),
                        "补槽规则必须写在 padding:0 之后");
        }

        [Fact]
        public void GutterIsScopedToIconBearingControlsOnly()
        {
            // 去壳的本意：纯文本格左右零槽。所以补槽只能挂在 --prefix/--suffix 上，不能回到无条件 padding。
            var (prefixSelector, _) = Rule(PrefixGutter);
            var (suffixSelector, _) = Rule(SuffixGutter);
            Assert.Contains(".el-input--prefix > ", prefixSelector);
            Assert.Contains(".el-input--suffix > ", suffixSelector);

            // 仍然只在字段壳内生效：KeepControl（保留控件自身外观）那条路不能被补槽规则命中
            Assert.StartsWith(".el-form-field:not(.el-form-field--keep-control) > .el-form-field__content", prefixSelector);
            Assert.StartsWith(".el-form-field:not(.el-form-field--keep-control) > .el-form-field__content", suffixSelector);
        }

        [Fact]
        public void ElementBaseGutterRulesAreStillThere()
        {
            // 批次㊽ 是「补回」而不是「另起一套」：库自己那两条 (0,2,0) 的原始规则必须原样在，
            // 否则字段壳之外（KeepControl、裸 .el-input）的日期/下拉格会跟着丢槽。
            Assert.Matches(@"padding-left\s*:\s*30px", Rule(".el-input--prefix .el-input__inner").Body);
            Assert.Matches(@"padding-right\s*:\s*30px", Rule(".el-input--suffix .el-input__inner").Body);
        }
    }
}
