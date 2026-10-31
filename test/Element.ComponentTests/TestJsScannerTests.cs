using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 注释剥离器自身的回归：它被三组兼容性守卫依赖（ES5 语法、只用浏览器内建 API、不含第三方库名），
    /// 一旦被改坏就会「假绿」——把真代码也剥没了，守卫就再也抓不到违规。
    /// 因此这里同时锁住两件事：该剥的剥掉、该留的一定不能少。
    /// </summary>
    public class TestJsScannerTests
    {
        [Fact]
        public void DropsLineAndBlockComments()
        {
            var js = "var a = 1; // 说明里写 scrollHeight-clientHeight 和 => 与 Radzen\n"
                   + "/* 块注释\n   里面也有 `反引号` 和 => \n*/\nvar b = 2;";

            var code = TestJsScanner.StripComments(js);

            Assert.DoesNotContain("Radzen", code);
            Assert.DoesNotContain("反引号", code);
            Assert.DoesNotContain("=>", code);
            Assert.DoesNotContain("scrollHeight", code);
            Assert.Contains("var a = 1;", code);
            Assert.Contains("var b = 2;", code);
        }

        [Fact]
        public void KeepsStringsIntactSoPositiveAssertionsStillWork()
        {
            var js = "case \"inserttable\": url = \"https://cdn.example.com/x\"; break;";

            var code = TestJsScanner.StripComments(js);

            Assert.Contains("case \"inserttable\"", code);
            // URL 里的 // 不得被当成行注释，否则后面的真代码会被整段吞掉
            Assert.Contains("https://cdn.example.com/x", code);
            Assert.Contains("break;", code);
        }

        [Fact]
        public void KeepsTheBacktickItselfSoTemplateLiteralsAreStillCaught()
        {
            var js = "var s = `x ${y => z}`;";

            var code = TestJsScanner.StripComments(js);

            Assert.Contains("`", code);                                  // 反引号本身必须留下
            Assert.DoesNotContain("=>", code);                            // 但其内部不算代码
        }

        [Fact]
        public void RealViolationInCodeSurvivesStripping()
        {
            var js = "// 注释\nvar f = (a) => a + 1;";

            var code = TestJsScanner.StripComments(js);

            Assert.Matches(new Regex(@"=>"), code);
        }

        [Fact]
        public void PreservesLineCount()
        {
            var js = "line1\n/* multi\nline\nblock */\nline5\n/// doc\nline7";

            var code = TestJsScanner.StripComments(js);

            Assert.Equal(js.Split('\n').Length, code.Split('\n').Length);
        }

        [Fact]
        public void StrippingRealDomJsKeepsExecutableContent()
        {
            var path = Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "js", "dom.js");
            Assert.True(File.Exists(path), "dom.js 不存在");

            var raw = File.ReadAllText(path);
            var marker = raw.IndexOf("ElHtmlEditorGrow：自写富文本编辑器浏览器通道", StringComparison.Ordinal);
            Assert.True(marker >= 0, "dom.js 中未找到 ElHtmlEditorGrow 通道段");

            var stripped = TestJsScanner.StripComments(raw.Substring(marker));

            // 正向内容不能被剥掉：守卫既不能误报，也不能瞎到看不见真代码
            Assert.Contains("document.execCommand", stripped);
            Assert.Contains("window.getSelection", stripped);
            Assert.Contains("XMLHttpRequest", stripped);
            // 注意 inserttable 是在 switch 之前单独 if 处理的（不在 case 列表里），
            // 所以这里断言的是「带引号的命令名」而不是 case 分支。
            Assert.Contains("\"inserttable\"", stripped);
            Assert.Contains("var ", stripped);
            Assert.True(stripped.Length > 2000, "剥离后代码量异常偏小，可能把真代码也剥没了：" + stripped.Length);
        }

        private static string RepoRoot([CallerFilePath]string callerFilePath = null)
        {
            var dir = new FileInfo(callerFilePath ?? string.Empty).Directory;
            return dir.Parent.Parent.FullName;
        }
    }
}
