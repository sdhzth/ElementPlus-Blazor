using AngleSharp.Dom;
using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElViewerGrow 组件测试（仅覆盖本组件，不跑全量）。
    /// <list type="number">
    /// <item><description><b>纯 C# 引擎</b>（<see cref="ElViewerGrowEngine"/>）：类型识别、URL 安全、文件名推断、
    /// 体积格式化、缩放阶梯、旋转归一、data URI、外链模板。</description></item>
    /// <item><description><b>纯 C# 渲染器</b>（<see cref="ElViewerGrowRenderer"/>）：转义、Markdown 子集、CSV、媒体与嵌入。</description></item>
    /// <item><description><b>bUnit 渲染与交互</b>：外壳、五种状态、缩放/旋转/最大化/翻页/URL 加载、事件。</description></item>
    /// <item><description><b>内置 OFD 通道</b>：可开关、生效条件、桥接参数、页数回填与不粘页、失败可见、关闭即回收。</description></item>
    /// <item><description><b>守卫</b>：零第三方依赖、JS 互操作只允许 OFD 通道与打印桥接、懒加载不自带 <c>&lt;script&gt;</c>、通道 ES5 合规、浏览器兼容 CSS、样式作用域。</description></item>
    /// </list>
    /// </summary>
    public class ElViewerGrowTests : BunitContext
    {
        public ElViewerGrowTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        #region 辅助

        private IRenderedComponent<ElViewerGrow> Viewer()
        {
            return Render<ElViewerGrow>();
        }

        private IRenderedComponent<ElViewerGrow> Viewer(Action<ComponentParameterCollectionBuilder<ElViewerGrow>> configure)
        {
            return Render<ElViewerGrow>(configure);
        }

        /// <summary>按提示文案定位工具栏按钮（按钮/链接/禁用占位都带 <c>el-viewer-grow__button</c> 类）。</summary>
        private static IElement Button(IRenderedComponent<ElViewerGrow> cut, string title)
        {
            return cut.FindAll(".el-viewer-grow__button").First(e => e.GetAttribute("title") == title);
        }

        private static bool HasButton(IRenderedComponent<ElViewerGrow> cut, string title)
        {
            return cut.FindAll(".el-viewer-grow__button").Any(e => e.GetAttribute("title") == title);
        }

        private static RenderFragment<ElViewerDocument> CustomContent()
        {
            return document => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "my-viewer-content");
                builder.AddContent(2, "custom:" + document.FileName);
                builder.CloseElement();
            };
        }

        private static RenderFragment CustomEmpty()
        {
            return builder =>
            {
                builder.OpenElement(0, "p");
                builder.AddAttribute(1, "class", "my-viewer-empty");
                builder.AddContent(2, "没有文档");
                builder.CloseElement();
            };
        }

        private static byte[] Utf8(string text)
        {
            return new UTF8Encoding(false).GetBytes(text);
        }

        /// <summary>
        /// 仓库根目录。用 <c>[CallerFilePath]</c> 而不是运行目录定位 —— 测试也可能跑在仓库外的
        /// 隔离工程里（见 <c>ce_build/viewer_verify</c>），那时 AppContext.BaseDirectory 上溯不到仓库。
        /// </summary>
        private static string RepoRoot([CallerFilePath] string file = null)
        {
            // .../test/Element.ComponentTests/ElViewerGrowTests.cs → 上溯两层
            var directory = new FileInfo(file).Directory;

            return directory.Parent.Parent.FullName;
        }

        private static string[] SourceFiles()
        {
            return Directory.GetFiles(Path.Combine(RepoRoot(), "src", "Components"), "ElViewer*", SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// 剥离注释，只留可执行代码。实现收敛到 <see cref="TestJsScanner"/>（引号感知，
        /// 不会把字符串里的 <c>"https://…"</c> 误当行注释）。
        /// </summary>
        private static string StripComments(string text) => TestJsScanner.StripComments(text);

        private static string CssSection()
        {
            var text = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "css", "index.css"));
            var marker = text.IndexOf("ElViewerGrow：Element Plus 自渲染文档预览器", StringComparison.Ordinal);

            Assert.True(marker >= 0, "index.css 中未找到 ElViewerGrow 样式段");

            var start = text.LastIndexOf("/*", marker, StringComparison.Ordinal);

            // 只取本组件的段落：截到下一个「段分隔注释」为止。
            // 否则会把样式表里后面追加的其它组件样式也算进来，作用域守卫会误报。
            var next = text.IndexOf("\n/* =", marker + 10, StringComparison.Ordinal);
            var section = next >= 0 ? text.Substring(start, next - start) : text.Substring(start);

            return Regex.Replace(section, @"/\*[\s\S]*?\*/", string.Empty);
        }

        #endregion

        #region 引擎：类型识别

        [Fact]
        public void Engine_ResolveKind_ByExtension()
        {
            Assert.Equal(ElViewerDocumentKind.Pdf, ElViewerGrowEngine.ResolveKind("a.pdf", null));
            Assert.Equal(ElViewerDocumentKind.Word, ElViewerGrowEngine.ResolveKind("a.docx", null));
            Assert.Equal(ElViewerDocumentKind.Word, ElViewerGrowEngine.ResolveKind("a.doc", null));
            Assert.Equal(ElViewerDocumentKind.Excel, ElViewerGrowEngine.ResolveKind("a.xlsx", null));
            Assert.Equal(ElViewerDocumentKind.PowerPoint, ElViewerGrowEngine.ResolveKind("a.pptx", null));
            Assert.Equal(ElViewerDocumentKind.Ofd, ElViewerGrowEngine.ResolveKind("a.ofd", null));
            Assert.Equal(ElViewerDocumentKind.Text, ElViewerGrowEngine.ResolveKind("a.txt", null));
            Assert.Equal(ElViewerDocumentKind.Markdown, ElViewerGrowEngine.ResolveKind("a.md", null));
            Assert.Equal(ElViewerDocumentKind.Markdown, ElViewerGrowEngine.ResolveKind("a.markdown", null));
            Assert.Equal(ElViewerDocumentKind.Csv, ElViewerGrowEngine.ResolveKind("a.csv", null));
            Assert.Equal(ElViewerDocumentKind.Csv, ElViewerGrowEngine.ResolveKind("a.tsv", null));
            Assert.Equal(ElViewerDocumentKind.Json, ElViewerGrowEngine.ResolveKind("a.json", null));
            Assert.Equal(ElViewerDocumentKind.Image, ElViewerGrowEngine.ResolveKind("a.PNG", null));
            Assert.Equal(ElViewerDocumentKind.Video, ElViewerGrowEngine.ResolveKind("a.mp4", null));
            Assert.Equal(ElViewerDocumentKind.Audio, ElViewerGrowEngine.ResolveKind("a.mp3", null));
            Assert.Equal(ElViewerDocumentKind.Cad, ElViewerGrowEngine.ResolveKind("a.dxf", null));
            Assert.Equal(ElViewerDocumentKind.Model, ElViewerGrowEngine.ResolveKind("a.glb", null));
        }

        [Fact]
        public void Engine_ResolveKind_ByContentType()
        {
            Assert.Equal(ElViewerDocumentKind.Pdf, ElViewerGrowEngine.ResolveKind(null, "application/pdf"));
            Assert.Equal(ElViewerDocumentKind.Word, ElViewerGrowEngine.ResolveKind(null, "application/vnd.openxmlformats-officedocument.wordprocessingml.document"));
            Assert.Equal(ElViewerDocumentKind.Excel, ElViewerGrowEngine.ResolveKind(null, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
            Assert.Equal(ElViewerDocumentKind.Ofd, ElViewerGrowEngine.ResolveKind(null, "application/ofd"));
            Assert.Equal(ElViewerDocumentKind.Markdown, ElViewerGrowEngine.ResolveKind(null, "text/markdown"));
            Assert.Equal(ElViewerDocumentKind.Csv, ElViewerGrowEngine.ResolveKind(null, "text/csv"));
            Assert.Equal(ElViewerDocumentKind.Json, ElViewerGrowEngine.ResolveKind(null, "application/json; charset=utf-8"));
        }

        [Fact]
        public void Engine_ResolveKind_UsesPrefixForMediaMime()
        {
            Assert.Equal(ElViewerDocumentKind.Image, ElViewerGrowEngine.ResolveKind(null, "image/webp"));
            Assert.Equal(ElViewerDocumentKind.Video, ElViewerGrowEngine.ResolveKind(null, "video/mp4"));
            Assert.Equal(ElViewerDocumentKind.Audio, ElViewerGrowEngine.ResolveKind(null, "audio/mpeg"));
        }

        [Fact]
        public void Engine_ResolveKind_PrefersExtensionOverContentType()
        {
            // 用户看到的是文件名，扩展名更贴近直觉：扩展名说它是 Markdown，就不要因为 MIME 说成纯文本。
            Assert.Equal(ElViewerDocumentKind.Markdown, ElViewerGrowEngine.ResolveKind("a.md", "text/plain"));
        }

        [Fact]
        public void Engine_ResolveKind_UnknownForMissingAndOddInputs()
        {
            Assert.Equal(ElViewerDocumentKind.Unknown, ElViewerGrowEngine.ResolveKind(null, null));
            Assert.Equal(ElViewerDocumentKind.Unknown, ElViewerGrowEngine.ResolveKind("noext", null));
            Assert.Equal(ElViewerDocumentKind.Unknown, ElViewerGrowEngine.ResolveKind("a.", null));
            Assert.Equal(ElViewerDocumentKind.Unknown, ElViewerGrowEngine.ResolveKind("a.qqq", "application/x-foo"));
        }

        [Fact]
        public void Engine_GetExtension_ReturnsNullWhenAbsent()
        {
            Assert.Equal(".pdf", ElViewerGrowEngine.GetExtension("dir/a.pdf"));
            Assert.Null(ElViewerGrowEngine.GetExtension("noext"));
            Assert.Null(ElViewerGrowEngine.GetExtension("trailing."));
            Assert.Null(ElViewerGrowEngine.GetExtension(string.Empty));
            Assert.Null(ElViewerGrowEngine.GetExtension(null));
        }

        [Fact]
        public void Engine_ResolveContentType_FallsBackToOctetStream()
        {
            Assert.Equal("application/pdf", ElViewerGrowEngine.ResolveContentType(ElViewerDocumentKind.Pdf));
            Assert.Equal("text/markdown", ElViewerGrowEngine.ResolveContentType(ElViewerDocumentKind.Markdown));
            Assert.Equal("application/octet-stream", ElViewerGrowEngine.ResolveContentType(ElViewerDocumentKind.Unknown));
        }

        [Fact]
        public void Engine_CapabilityFlags_AreConsistent()
        {
            Assert.True(ElViewerGrowEngine.IsTextual(ElViewerDocumentKind.Text));
            Assert.True(ElViewerGrowEngine.IsTextual(ElViewerDocumentKind.Markdown));
            Assert.True(ElViewerGrowEngine.IsTextual(ElViewerDocumentKind.Csv));
            Assert.True(ElViewerGrowEngine.IsTextual(ElViewerDocumentKind.Json));
            Assert.False(ElViewerGrowEngine.IsTextual(ElViewerDocumentKind.Pdf));

            Assert.True(ElViewerGrowEngine.IsBuiltInSupported(ElViewerDocumentKind.Pdf));
            Assert.True(ElViewerGrowEngine.IsBuiltInSupported(ElViewerDocumentKind.Image));
            Assert.False(ElViewerGrowEngine.IsBuiltInSupported(ElViewerDocumentKind.Word));
            Assert.False(ElViewerGrowEngine.IsBuiltInSupported(ElViewerDocumentKind.Ofd));

            Assert.True(ElViewerGrowEngine.NeedsExternalViewer(ElViewerDocumentKind.Word));
            Assert.False(ElViewerGrowEngine.NeedsExternalViewer(ElViewerDocumentKind.Pdf));
            Assert.False(ElViewerGrowEngine.NeedsExternalViewer(ElViewerDocumentKind.Unknown));
        }

        [Fact]
        public void Engine_GetKindDisplayName_CoversAllKinds()
        {
            Assert.Equal("PDF", ElViewerGrowEngine.GetKindDisplayName(ElViewerDocumentKind.Pdf));
            Assert.Equal("Word", ElViewerGrowEngine.GetKindDisplayName(ElViewerDocumentKind.Word));
            Assert.Equal("Excel", ElViewerGrowEngine.GetKindDisplayName(ElViewerDocumentKind.Excel));
            Assert.Equal("PowerPoint", ElViewerGrowEngine.GetKindDisplayName(ElViewerDocumentKind.PowerPoint));
            Assert.Equal("OFD", ElViewerGrowEngine.GetKindDisplayName(ElViewerDocumentKind.Ofd));
            Assert.Equal("Markdown", ElViewerGrowEngine.GetKindDisplayName(ElViewerDocumentKind.Markdown));
            Assert.Equal("CSV", ElViewerGrowEngine.GetKindDisplayName(ElViewerDocumentKind.Csv));
            Assert.Equal("3D", ElViewerGrowEngine.GetKindDisplayName(ElViewerDocumentKind.Model));
            Assert.Equal("Unknown", ElViewerGrowEngine.GetKindDisplayName(ElViewerDocumentKind.Unknown));
        }

        #endregion

        #region 引擎：URL 安全

        [Fact]
        public void Engine_IsSafeUrl_AllowsHttpAndHttps()
        {
            Assert.True(ElViewerGrowEngine.IsSafeUrl("https://example.com/a.pdf"));
            Assert.True(ElViewerGrowEngine.IsSafeUrl("http://example.com/a.pdf"));
            Assert.True(ElViewerGrowEngine.IsSafeUrl("HTTPS://EXAMPLE.COM/A.PDF"));
        }

        [Fact]
        public void Engine_IsSafeUrl_AllowsRelativeAddresses()
        {
            Assert.True(ElViewerGrowEngine.IsSafeUrl("/files/a.pdf"));
            Assert.True(ElViewerGrowEngine.IsSafeUrl("./a.pdf"));
            Assert.True(ElViewerGrowEngine.IsSafeUrl("../a.pdf"));
            Assert.True(ElViewerGrowEngine.IsSafeUrl("files/a.pdf"));
            Assert.True(ElViewerGrowEngine.IsSafeUrl("//cdn.example.com/a.pdf"));
        }

        [Fact]
        public void Engine_IsSafeUrl_BlocksDangerousSchemes()
        {
            Assert.False(ElViewerGrowEngine.IsSafeUrl("javascript:alert(1)"));
            Assert.False(ElViewerGrowEngine.IsSafeUrl("JavaScript:alert(1)"));
            Assert.False(ElViewerGrowEngine.IsSafeUrl("vbscript:msgbox"));
            Assert.False(ElViewerGrowEngine.IsSafeUrl("file:///C:/secret.pdf"));
            Assert.False(ElViewerGrowEngine.IsSafeUrl("mailto:a@b.com"));
            Assert.False(ElViewerGrowEngine.IsSafeUrl("ftp://example.com/a.pdf"));
            Assert.False(ElViewerGrowEngine.IsSafeUrl("data:text/html;base64,PHNjcmlwdD4="));
        }

        [Fact]
        public void Engine_IsSafeUrl_BlocksObfuscatedJavascript()
        {
            // 写进属性后浏览器会先做实体解码，所以这里必须同样先解码再判断。
            Assert.False(ElViewerGrowEngine.IsSafeUrl("&#106;avascript:alert(1)"));
            Assert.False(ElViewerGrowEngine.IsSafeUrl("javascript&colon;alert(1)"));
            Assert.False(ElViewerGrowEngine.IsSafeUrl("java\tscript:alert(1)"));
            Assert.False(ElViewerGrowEngine.IsSafeUrl("  javascript:alert(1)  "));
        }

        [Fact]
        public void Engine_IsSafeUrl_AllowsWhitelistedDataAndBlob()
        {
            Assert.True(ElViewerGrowEngine.IsSafeUrl("data:image/png;base64,iVBORw0KGgo="));
            Assert.True(ElViewerGrowEngine.IsSafeUrl("data:application/pdf;base64,JVBERi0="));
            Assert.True(ElViewerGrowEngine.IsSafeUrl("data:text/plain;base64,aGVsbG8="));
            Assert.True(ElViewerGrowEngine.IsSafeUrl("blob:http://example.com/abc"));
        }

        [Fact]
        public void Engine_IsSafeUrl_RejectsEmptyAndWhitespace()
        {
            Assert.False(ElViewerGrowEngine.IsSafeUrl(null));
            Assert.False(ElViewerGrowEngine.IsSafeUrl(string.Empty));
            Assert.False(ElViewerGrowEngine.IsSafeUrl("   "));
        }

        [Fact]
        public void Engine_DecodeEntities_HandlesNumericAndColon()
        {
            Assert.Equal("javascript:", ElViewerGrowEngine.DecodeEntities("&#106;avascript:"));
            Assert.Equal("javascript:", ElViewerGrowEngine.DecodeEntities("javascript&colon;"));
            Assert.Equal("a&b", ElViewerGrowEngine.DecodeEntities("a&amp;b"));
            Assert.Equal("plain", ElViewerGrowEngine.DecodeEntities("plain"));
        }

        #endregion

        #region 引擎：文件名 / 体积 / data URI / 外链

        [Fact]
        public void Engine_GuessFileName_StripsQueryAndFragment()
        {
            Assert.Equal("报告.pdf", ElViewerGrowEngine.GuessFileName("https://example.com/files/报告.pdf?token=1&x=2#page=3"));
            Assert.Equal("a.pdf", ElViewerGrowEngine.GuessFileName("https://example.com/a.pdf"));
            Assert.Equal("a.pdf", ElViewerGrowEngine.GuessFileName("a.pdf"));
            Assert.Equal("a.pdf", ElViewerGrowEngine.GuessFileName("C:\\docs\\a.pdf"));
        }

        [Fact]
        public void Engine_GuessFileName_DecodesPercentEncoding()
        {
            Assert.Equal("报告.pdf", ElViewerGrowEngine.GuessFileName("https://example.com/%E6%8A%A5%E5%91%8A.pdf"));
        }

        [Fact]
        public void Engine_GuessFileName_ReturnsNullForEmptyResults()
        {
            Assert.Null(ElViewerGrowEngine.GuessFileName(null));
            Assert.Null(ElViewerGrowEngine.GuessFileName("  "));
            Assert.Null(ElViewerGrowEngine.GuessFileName("https://example.com/"));
            Assert.Null(ElViewerGrowEngine.GuessFileName("https://example.com/?x=1"));
        }

        [Fact]
        public void Engine_FormatSize_UsesBinaryUnits()
        {
            Assert.Null(ElViewerGrowEngine.FormatSize(0));
            Assert.Null(ElViewerGrowEngine.FormatSize(-1));
            Assert.Equal("512 B", ElViewerGrowEngine.FormatSize(512));
            Assert.Equal("1 KB", ElViewerGrowEngine.FormatSize(1024));
            Assert.Equal("1.5 KB", ElViewerGrowEngine.FormatSize(1536));
            Assert.Equal("2 MB", ElViewerGrowEngine.FormatSize(2 * 1024 * 1024));
        }

        [Fact]
        public void Engine_TryBuildDataUri_RespectsWhitelistAndLimit()
        {
            string uri;

            Assert.True(ElViewerGrowEngine.TryBuildDataUri(Utf8("hi"), "text/plain", out uri));
            Assert.StartsWith("data:text/plain;base64,", uri);

            Assert.False(ElViewerGrowEngine.TryBuildDataUri(Utf8("x"), "text/html", out uri));
            Assert.False(ElViewerGrowEngine.TryBuildDataUri(new byte[ElViewerGrowEngine.MaxDataUriBytes + 1], "text/plain", out uri));
            Assert.False(ElViewerGrowEngine.TryBuildDataUri(null, "text/plain", out uri));
        }

        [Fact]
        public void Engine_BuildExternalUrl_SubstitutesPlaceholders()
        {
            var document = ElViewerDocument.FromUrl("https://example.com/files/a.docx");
            var url = ElViewerGrowEngine.BuildExternalUrl("https://view/?u={url}&n={name}&e={ext}&k={kind}", document);

            Assert.Equal("https://view/?u=https%3A%2F%2Fexample.com%2Ffiles%2Fa.docx&n=a.docx&e=.docx&k=Word", url);
        }

        [Fact]
        public void Engine_BuildExternalUrl_ReturnsNullWhenUnusable()
        {
            var document = ElViewerDocument.FromUrl("https://example.com/a.docx");

            Assert.Null(ElViewerGrowEngine.BuildExternalUrl(null, document));
            Assert.Null(ElViewerGrowEngine.BuildExternalUrl("", document));
            Assert.Null(ElViewerGrowEngine.BuildExternalUrl("https://view/?u={url}", null));
            Assert.Null(ElViewerGrowEngine.BuildExternalUrl("https://view/?u={url}", ElViewerDocument.FromUrl("javascript:alert(1)")));
        }

        [Fact]
        public void Engine_TryDecodeText_HandlesBom()
        {
            string text;

            Assert.True(ElViewerGrowEngine.TryDecodeText(Utf8("你好\n第二行"), out text));
            Assert.Equal("你好\n第二行", text);

            var bom = new List<byte> { 0xEF, 0xBB, 0xBF };
            bom.AddRange(Utf8("abc"));

            Assert.True(ElViewerGrowEngine.TryDecodeText(bom.ToArray(), out text));
            Assert.Equal("abc", text);

            Assert.True(ElViewerGrowEngine.TryDecodeText(Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("ab")).ToArray(), out text));
            Assert.Equal("ab", text);

            Assert.False(ElViewerGrowEngine.TryDecodeText(null, out text));
            Assert.False(ElViewerGrowEngine.TryDecodeText(new byte[0], out text));
        }

        [Fact]
        public void Engine_NormalizeNewLines_UnifiesCrlf()
        {
            Assert.Equal("a\nb\nc", ElViewerGrowEngine.NormalizeNewLines("a\r\nb\rc"));
            Assert.Equal(string.Empty, ElViewerGrowEngine.NormalizeNewLines(null));
        }

        #endregion

        #region 引擎：缩放 / 旋转 / 页码

        [Fact]
        public void Engine_ClampZoom_RespectsLimits()
        {
            Assert.Equal(1.0, ElViewerGrowEngine.ClampZoom(1.0, 0.25, 4.0));
            Assert.Equal(0.25, ElViewerGrowEngine.ClampZoom(0.01, 0.25, 4.0));
            Assert.Equal(4.0, ElViewerGrowEngine.ClampZoom(99, 0.25, 4.0));
            Assert.Equal(1.0, ElViewerGrowEngine.ClampZoom(double.NaN, 0.25, 4.0));
            Assert.Equal(2.0, ElViewerGrowEngine.ClampZoom(2.0, 4.0, 0.25));
        }

        [Fact]
        public void Engine_StepZoom_WalksTheLadder()
        {
            Assert.Equal(1.25, ElViewerGrowEngine.StepZoom(1.0, 1, 0.25, 4.0));
            Assert.Equal(1.5, ElViewerGrowEngine.StepZoom(1.25, 1, 0.25, 4.0));
            Assert.Equal(4.0, ElViewerGrowEngine.StepZoom(4.0, 1, 0.25, 4.0));
            Assert.Equal(0.75, ElViewerGrowEngine.StepZoom(1.0, -1, 0.25, 4.0));
            Assert.Equal(0.25, ElViewerGrowEngine.StepZoom(0.25, -1, 0.25, 4.0));
            Assert.Equal(1.0, ElViewerGrowEngine.StepZoom(1.0, 0, 0.25, 4.0));
        }

        [Fact]
        public void Engine_StepZoom_FallsBackWhenOffLadder()
        {
            Assert.Equal(1.0, ElViewerGrowEngine.StepZoom(0.9, 1, 0.25, 4.0));
            Assert.Equal(0.75, ElViewerGrowEngine.StepZoom(0.9, -1, 0.25, 4.0));
        }

        [Fact]
        public void Engine_NormalizeRotation_SnapsToQuarters()
        {
            Assert.Equal(0, ElViewerGrowEngine.NormalizeRotation(0));
            Assert.Equal(90, ElViewerGrowEngine.NormalizeRotation(90));
            Assert.Equal(180, ElViewerGrowEngine.NormalizeRotation(180));
            Assert.Equal(270, ElViewerGrowEngine.NormalizeRotation(-90));
            Assert.Equal(90, ElViewerGrowEngine.NormalizeRotation(450));
            Assert.Equal(0, ElViewerGrowEngine.NormalizeRotation(360));
        }

        [Fact]
        public void Engine_BuildTransform_OmitsIdentity()
        {
            Assert.Equal(string.Empty, ElViewerGrowEngine.BuildTransform(1.0, 0));
            Assert.Equal("scale(1.5)", ElViewerGrowEngine.BuildTransform(1.5, 0));
            Assert.Equal("rotate(90deg)", ElViewerGrowEngine.BuildTransform(1.0, 90));
            Assert.Equal("scale(2) rotate(180deg)", ElViewerGrowEngine.BuildTransform(2.0, 180));
        }

        [Fact]
        public void Engine_FormatZoom_UsesPercent()
        {
            Assert.Equal("100%", ElViewerGrowEngine.FormatZoom(1.0));
            Assert.Equal("150%", ElViewerGrowEngine.FormatZoom(1.5));
            Assert.Equal("25%", ElViewerGrowEngine.FormatZoom(0.25));
        }

        [Fact]
        public void Engine_BuildPageLabel_HandlesUnknownCount()
        {
            Assert.Equal(string.Empty, ElViewerGrowEngine.BuildPageLabel(0, 0));
            Assert.Equal("3", ElViewerGrowEngine.BuildPageLabel(3, 0));
            Assert.Equal("1 / 5", ElViewerGrowEngine.BuildPageLabel(1, 5));
            Assert.Equal("5 / 5", ElViewerGrowEngine.BuildPageLabel(9, 5));
        }

        [Fact]
        public void Engine_ClampPage_KeepsInsideRange()
        {
            Assert.Equal(1, ElViewerGrowEngine.ClampPage(0, 5));
            Assert.Equal(5, ElViewerGrowEngine.ClampPage(9, 5));
            Assert.Equal(3, ElViewerGrowEngine.ClampPage(3, 5));
            Assert.Equal(1, ElViewerGrowEngine.ClampPage(-2, 0));
        }

        #endregion

        #region 渲染器：转义与纯文本

        [Fact]
        public void Renderer_EscapeHtml_NeutralizesMarkup()
        {
            Assert.Equal("&lt;script&gt;alert(&#39;x&#39;)&lt;/script&gt;", ElViewerGrowRenderer.EscapeHtml("<script>alert('x')</script>"));
            Assert.Equal("a&amp;b", ElViewerGrowRenderer.EscapeHtml("a&b"));
            Assert.Equal("&quot;q&quot;", ElViewerGrowRenderer.EscapeHtml("\"q\""));
            Assert.Equal(string.Empty, ElViewerGrowRenderer.EscapeHtml(null));
        }

        [Fact]
        public void Renderer_RenderText_EscapesAndKeepsNewlines()
        {
            var html = ElViewerGrowRenderer.RenderText("<b>a</b>\nb");

            Assert.StartsWith("<pre class=\"el-viewer-grow__doc-pre\">", html);
            Assert.Contains("&lt;b&gt;a&lt;/b&gt;", html);
            Assert.Contains("\nb", html);
        }

        #endregion

        #region 渲染器：Markdown

        [Fact]
        public void Renderer_RenderMarkdown_Headings()
        {
            var html = ElViewerGrowRenderer.RenderMarkdown("# 一级\n## 二级\n###### 六级");

            Assert.Contains("<h1>一级</h1>", html);
            Assert.Contains("<h2>二级</h2>", html);
            Assert.Contains("<h6>六级</h6>", html);
        }

        [Fact]
        public void Renderer_RenderMarkdown_TreatsHashWithoutSpaceAsText()
        {
            var html = ElViewerGrowRenderer.RenderMarkdown("#标签");

            Assert.DoesNotContain("<h1>", html);
            Assert.Contains("<p class=\"el-viewer-grow__doc-p\">#标签</p>", html);
        }

        [Fact]
        public void Renderer_RenderMarkdown_InlineMarks()
        {
            var html = ElViewerGrowRenderer.RenderMarkdown("**粗** 和 *斜* 和 `代码`");

            Assert.Contains("<strong>粗</strong>", html);
            Assert.Contains("<em>斜</em>", html);
            Assert.Contains("<code class=\"el-viewer-grow__doc-inline-code\">代码</code>", html);
        }

        [Fact]
        public void Renderer_RenderMarkdown_LinksOnlySafeProtocols()
        {
            var safe = ElViewerGrowRenderer.RenderMarkdown("[文档](https://example.com/a.pdf)");

            Assert.Contains("href=\"https://example.com/a.pdf\"", safe);
            Assert.Contains("rel=\"noopener noreferrer\"", safe);

            var unsafeLink = ElViewerGrowRenderer.RenderMarkdown("[坏链](javascript:alert(1))");

            Assert.DoesNotContain("href", unsafeLink);
            Assert.DoesNotContain("javascript", unsafeLink);
        }

        [Fact]
        public void Renderer_RenderMarkdown_FencedCodeIsEscaped()
        {
            var html = ElViewerGrowRenderer.RenderMarkdown("```\nif (a < b) { }\n```");

            Assert.Contains("<pre class=\"el-viewer-grow__doc-pre\"><code class=\"el-viewer-grow__doc-code\">", html);
            Assert.Contains("if (a &lt; b) { }", html);
        }

        [Fact]
        public void Renderer_RenderMarkdown_Blocks()
        {
            Assert.Contains("<hr class=\"el-viewer-grow__doc-hr\" />", ElViewerGrowRenderer.RenderMarkdown("---"));
            Assert.Contains("<blockquote class=\"el-viewer-grow__doc-quote\">引用</blockquote>", ElViewerGrowRenderer.RenderMarkdown("> 引用"));
            Assert.Contains("<ul class=\"el-viewer-grow__doc-list\">", ElViewerGrowRenderer.RenderMarkdown("- a\n- b"));
            Assert.Contains("<ol class=\"el-viewer-grow__doc-list\">", ElViewerGrowRenderer.RenderMarkdown("1. a\n2. b"));
            Assert.Contains("<li class=\"el-viewer-grow__doc-item\">a</li>", ElViewerGrowRenderer.RenderMarkdown("- a"));
        }

        [Fact]
        public void Renderer_RenderMarkdown_EscapesRawHtml()
        {
            var html = ElViewerGrowRenderer.RenderMarkdown("<img src=x onerror=alert(1)>");

            Assert.DoesNotContain("<img", html);
            Assert.Contains("&lt;img", html);
        }

        [Fact]
        public void Renderer_RenderMarkdown_EmptyInput()
        {
            Assert.Equal(string.Empty, ElViewerGrowRenderer.RenderMarkdown(null));
            Assert.Equal(string.Empty, ElViewerGrowRenderer.RenderMarkdown(string.Empty));
        }

        #endregion

        #region 渲染器：CSV

        [Fact]
        public void Renderer_DetectDelimiter_PicksMostFrequent()
        {
            Assert.Equal(',', ElViewerGrowRenderer.DetectDelimiter("a,b,c"));
            Assert.Equal(';', ElViewerGrowRenderer.DetectDelimiter("a;b;c;d"));
            Assert.Equal('\t', ElViewerGrowRenderer.DetectDelimiter("a\tb\tc"));
            Assert.Equal(',', ElViewerGrowRenderer.DetectDelimiter("abc"));
        }

        [Fact]
        public void Renderer_ParseCsv_HandlesQuotesAndEmbeddedBreaks()
        {
            var rows = ElViewerGrowRenderer.ParseCsv("a,\"b,c\"\n1,2", ',');

            Assert.Equal(2, rows.Count);
            Assert.Equal("b,c", rows[0][1]);

            var multiline = ElViewerGrowRenderer.ParseCsv("a,\"x\ny\"\n1,2", ',');

            Assert.Equal("x\ny", multiline[0][1]);

            var escaped = ElViewerGrowRenderer.ParseCsv("a,\"say \"\"hi\"\"\"", ',');

            Assert.Equal("say \"hi\"", escaped[0][1]);
        }

        [Fact]
        public void Renderer_RenderCsv_ProducesHeaderAndBody()
        {
            var html = ElViewerGrowRenderer.RenderCsv("name,age\n张三,30\n李四,40", null, true);

            Assert.Contains("<th class=\"el-viewer-grow__doc-th\">name</th>", html);
            Assert.Contains("<th class=\"el-viewer-grow__doc-th\">age</th>", html);
            Assert.Contains("<td class=\"el-viewer-grow__doc-td\">张三</td>", html);
            Assert.Contains("<tr class=\"el-viewer-grow__doc-tr\">", html);
        }

        [Fact]
        public void Renderer_RenderCsv_EscapesCells()
        {
            var html = ElViewerGrowRenderer.RenderCsv("a,b\n<script>,2", null, true);

            Assert.DoesNotContain("<script>", html);
            Assert.Contains("&lt;script&gt;", html);
        }

        [Fact]
        public void Renderer_RenderCsv_EmptyInput()
        {
            Assert.Equal(string.Empty, ElViewerGrowRenderer.RenderCsv(string.Empty, null, true));
        }

        #endregion

        #region 渲染器：媒体与嵌入

        [Fact]
        public void Renderer_Media_MarkupIsEscaped()
        {
            Assert.Equal("<img class=\"el-viewer-grow__doc-image\" src=\"a.png\" alt=\"a.png\" />",
                ElViewerGrowRenderer.RenderImage("a.png", "a.png"));

            Assert.Contains("<video class=\"el-viewer-grow__doc-video\" src=\"a.mp4\"", ElViewerGrowRenderer.RenderVideo("a.mp4", "video/mp4"));
            Assert.Contains("<audio class=\"el-viewer-grow__doc-audio\" src=\"a.mp3\"", ElViewerGrowRenderer.RenderAudio("a.mp3"));
            Assert.Contains("<iframe class=\"el-viewer-grow__doc-frame\" src=\"a.pdf\"", ElViewerGrowRenderer.RenderEmbed("a.pdf", "a.pdf"));
        }

        [Fact]
        public void Renderer_RenderContent_DispatchesByKind()
        {
            Assert.Contains("<pre", ElViewerGrowRenderer.RenderContent(ElViewerDocument.FromText("abc", "a.txt")));
            Assert.Contains("el-viewer-grow__doc-md", ElViewerGrowRenderer.RenderContent(ElViewerDocument.FromText("# a", "a.md")));
            Assert.Contains("el-viewer-grow__doc-table", ElViewerGrowRenderer.RenderContent(ElViewerDocument.FromText("a,b\n1,2", "a.csv")));
            Assert.Contains("<iframe", ElViewerGrowRenderer.RenderContent(ElViewerDocument.FromUrl("https://x/a.pdf")));
            Assert.Contains("<img", ElViewerGrowRenderer.RenderContent(ElViewerDocument.FromUrl("https://x/a.png")));
            Assert.Equal(string.Empty, ElViewerGrowRenderer.RenderContent(ElViewerDocument.FromUrl("https://x/a.docx")));
            Assert.Equal(string.Empty, ElViewerGrowRenderer.RenderContent(null));
        }

        [Fact]
        public void Renderer_CanRender_MatchesBuiltInCapabilities()
        {
            Assert.True(ElViewerGrowRenderer.CanRender(ElViewerDocument.FromText("a", "a.txt")));
            Assert.True(ElViewerGrowRenderer.CanRender(ElViewerDocument.FromUrl("https://x/a.pdf")));
            Assert.False(ElViewerGrowRenderer.CanRender(ElViewerDocument.FromUrl("https://x/a.docx")));
            Assert.False(ElViewerGrowRenderer.CanRender(ElViewerDocument.FromUrl("javascript:alert(1)")));
            Assert.False(ElViewerGrowRenderer.CanRender(null));
        }

        #endregion

        #region 文档模型

        [Fact]
        public void Document_FromUrl_ResolvesNameAndKind()
        {
            var document = ElViewerDocument.FromUrl("https://example.com/files/report.pdf?x=1");

            Assert.Equal("report.pdf", document.FileName);
            Assert.Equal(ElViewerDocumentKind.Pdf, document.Kind);
            Assert.Equal("application/pdf", document.ResolvedContentType);
            Assert.Equal("https://example.com/files/report.pdf?x=1", document.SourceUrl);
            Assert.True(document.IsSafe);
        }

        [Fact]
        public void Document_FromText_EncodesAndSizes()
        {
            var document = ElViewerDocument.FromText("你好", "a.md");

            Assert.Equal(ElViewerDocumentKind.Markdown, document.Kind);
            Assert.Equal("你好", document.Text);
            Assert.Equal(6, document.Size);
            Assert.True(document.HasContent);
            Assert.NotNull(document.SourceUrl);
        }

        [Fact]
        public void Document_FromBytes_DecodesTextualKinds()
        {
            var document = ElViewerDocument.FromBytes(Utf8("a,b\n1,2"), "a.csv");

            Assert.Equal(ElViewerDocumentKind.Csv, document.Kind);
            Assert.Equal("a,b\n1,2", document.Text);
        }

        [Fact]
        public void Document_SourceUrl_InlinesBytesWhenNoUrl()
        {
            var image = ElViewerDocument.FromBytes(new byte[] { 1, 2, 3 }, "a.png");

            Assert.StartsWith("data:image/png;base64,", image.SourceUrl);

            var huge = new byte[ElViewerGrowEngine.MaxDataUriBytes + 1];

            Assert.Null(ElViewerDocument.FromBytes(huge, "a.bin").SourceUrl);
        }

        [Fact]
        public void Document_IsSafe_FlagsDangerousUrls()
        {
            Assert.True(ElViewerDocument.FromUrl("https://x/a.pdf").IsSafe);
            Assert.False(ElViewerDocument.FromUrl("javascript:alert(1)").IsSafe);
        }

        [Fact]
        public void Document_Title_FallsBackThroughFileNameAndUrl()
        {
            Assert.Equal("a.pdf", ElViewerDocument.FromUrl("https://x/a.pdf").Title);

            // 地址最后一段会被当成文件名（哪怕它没有扩展名）
            Assert.Equal("y", ElViewerDocument.FromUrl("https://x/y").Title);

            // 没有文件名、且地址推断不出名字时才退回地址本身
            Assert.Equal("https://x/", new ElViewerDocument { Url = "https://x/" }.Title);
            Assert.Equal("Unknown", new ElViewerDocument().Title);
        }

        [Fact]
        public void Document_Clone_PreservesSource()
        {
            var document = ElViewerDocument.FromText("abc", "a.txt");
            var clone = document.Clone();

            Assert.Equal(document.FileName, clone.FileName);
            Assert.Equal(document.Text, clone.Text);
            Assert.NotSame(document, clone);
        }

        #endregion

        #region 工具栏与文案

        [Fact]
        public void Toolbar_Default_HasFullCommandSetInOrder()
        {
            var actions = ElViewerGrowToolbar.Default.Select(t => t.Action).ToArray();

            Assert.Equal(20, actions.Length);
            Assert.Equal(ElViewerToolbarAction.OpenFile, actions[0]);
            Assert.Equal(ElViewerToolbarAction.OpenUrl, actions[1]);
            Assert.Equal(ElViewerToolbarAction.Separator, actions[2]);
            Assert.Equal(ElViewerToolbarAction.Print, actions[13]);
            Assert.Equal(ElViewerToolbarAction.Close, actions[16]);
            Assert.Equal(ElViewerToolbarAction.NextPage, actions[19]);
            Assert.Equal(5, actions.Count(a => a == ElViewerToolbarAction.Separator));
        }

        [Fact]
        public void Toolbar_BuildAndWithout_WorkAsExpected()
        {
            var built = ElViewerGrowToolbar.Build(ElViewerToolbarAction.ZoomIn, ElViewerToolbarAction.Separator, ElViewerToolbarAction.Download);

            Assert.Equal(3, built.Length);
            Assert.True(built[1].IsSeparator);

            var trimmed = ElViewerGrowToolbar.Without(ElViewerGrowToolbar.Default, ElViewerToolbarAction.Download, ElViewerToolbarAction.Close);

            Assert.DoesNotContain(ElViewerToolbarAction.Download, trimmed.Select(t => t.Action));
            Assert.DoesNotContain(ElViewerToolbarAction.Close, trimmed.Select(t => t.Action));
            Assert.Equal(18, trimmed.Length);
        }

        [Fact]
        public void Toolbar_RequiresDocument_OnlyForDocumentScopedActions()
        {
            Assert.False(ElViewerGrowToolbar.RequiresDocument(ElViewerToolbarAction.OpenFile));
            Assert.False(ElViewerGrowToolbar.RequiresDocument(ElViewerToolbarAction.OpenUrl));
            Assert.False(ElViewerGrowToolbar.RequiresDocument(ElViewerToolbarAction.Separator));
            Assert.True(ElViewerGrowToolbar.RequiresDocument(ElViewerToolbarAction.Download));
            Assert.True(ElViewerGrowToolbar.RequiresDocument(ElViewerToolbarAction.NextPage));
        }

        [Fact]
        public void Toolbar_IsLink_OnlyForDownloadAndNewTab()
        {
            Assert.True(ElViewerGrowToolbar.IsLink(ElViewerToolbarAction.Download));
            Assert.True(ElViewerGrowToolbar.IsLink(ElViewerToolbarAction.OpenInNewTab));
            Assert.False(ElViewerGrowToolbar.IsLink(ElViewerToolbarAction.ZoomIn));
        }

        [Fact]
        public void Strings_DefaultIsChineseAndEnglishIsTranslated()
        {
            Assert.Equal("暂无文档", ElViewerGrowStrings.ZhCn.EmptyText);
            Assert.Equal("No document", ElViewerGrowStrings.En.EmptyText);
            Assert.Equal("暂无文档", ElViewerGrowStrings.Create(ElViewerLocale.ZhCn).EmptyText);
            Assert.Equal("No document", ElViewerGrowStrings.Create(ElViewerLocale.En).EmptyText);
        }

        [Fact]
        public void Strings_Resolve_CoversEveryAction()
        {
            var strings = ElViewerGrowStrings.ZhCn;

            Assert.Equal("放大", strings.Resolve(ElViewerToolbarAction.ZoomIn));
            Assert.Equal("缩小", strings.Resolve(ElViewerToolbarAction.ZoomOut));
            Assert.Equal("重置缩放", strings.Resolve(ElViewerToolbarAction.ZoomReset));
            Assert.Equal("顺时针旋转", strings.Resolve(ElViewerToolbarAction.RotateRight));
            Assert.Equal("关闭", strings.Resolve(ElViewerToolbarAction.Close));
            Assert.Equal(string.Empty, strings.Resolve(ElViewerToolbarAction.Separator));
        }

        [Fact]
        public void Icons_EveryActionHasNonEmptyMarkup()
        {
            foreach (ElViewerToolbarAction action in Enum.GetValues(typeof(ElViewerToolbarAction)))
            {
                if (action == ElViewerToolbarAction.Separator)
                {
                    continue;
                }

                Assert.False(string.IsNullOrEmpty(ElViewerGrowIcons.Get(action)), action + " 缺少图标");
            }

            Assert.Contains("<svg", ElViewerGrowIcons.Svg(ElViewerGrowIcons.Document));
            Assert.Contains("el-viewer-grow__icon", ElViewerGrowIcons.Svg(ElViewerGrowIcons.Warning));
        }

        #endregion

        #region 渲染：外壳与空态

        [Fact]
        public void Renders_RootWithScopedClassAndSize()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Width, "640px");
                p.Add(x => x.Height, "480px");
            });

            var root = cut.Find(".el-viewer-grow");

            Assert.Contains("el-viewer-grow", root.GetAttribute("class"));
            Assert.Contains("is-bordered", root.GetAttribute("class"));

            // 尺寸写进 CSS 变量而不是裸的 width/height —— 这样 @media print 才能
            // 把定高外壳放开（内联样式是盖不住的，除非用仓库禁用的 !important）。
            Assert.Contains("--el-viewer-grow-w:640px", root.GetAttribute("style"));
            Assert.Contains("--el-viewer-grow-h:480px", root.GetAttribute("style"));
            Assert.DoesNotContain("height:480px", root.GetAttribute("style"));
        }

        [Fact]
        public void Renders_DarkThemeAndCustomClass()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Theme, ElViewerTheme.Dark);
                p.Add(x => x.Cls, "my-viewer");
            });

            Assert.Contains("el-viewer-grow--dark", cut.Find(".el-viewer-grow").GetAttribute("class"));
            Assert.Contains("my-viewer", cut.Find(".el-viewer-grow").GetAttribute("class"));
        }

        [Fact]
        public void Renders_EmptyStateByDefault()
        {
            var cut = Viewer();

            Assert.NotNull(cut.Find(".el-viewer-grow__placeholder"));
            Assert.Equal("暂无文档", cut.Find(".el-viewer-grow__placeholder-text").TextContent);
            Assert.Equal("选择本地文件，或在工具栏输入文档地址", cut.Find(".el-viewer-grow__placeholder-hint").TextContent);
            Assert.Equal(ElViewerStatus.Empty, cut.Instance.Status);
        }

        [Fact]
        public void Renders_EmptyTemplate_WhenProvided()
        {
            var cut = Viewer(p => p.Add(x => x.EmptyTemplate, CustomEmpty()));

            Assert.NotNull(cut.Find(".my-viewer-empty"));
            Assert.Empty(cut.FindAll(".el-viewer-grow__placeholder-text"));
        }

        [Fact]
        public void Renders_ToolbarCanBeHidden()
        {
            Assert.NotNull(Viewer().Find(".el-viewer-grow__toolbar"));
            Assert.Empty(Viewer(p => p.Add(x => x.ShowToolbar, false)).FindAll(".el-viewer-grow__toolbar"));
        }

        [Fact]
        public void Renders_StatusBarCanBeHidden()
        {
            Assert.NotNull(Viewer().Find(".el-viewer-grow__status"));
            Assert.Empty(Viewer(p => p.Add(x => x.ShowStatusBar, false)).FindAll(".el-viewer-grow__status"));
        }

        [Fact]
        public void Renders_StatusBarShowsIdleAndZoom()
        {
            var cut = Viewer();

            Assert.Equal("未加载", cut.Find(".el-viewer-grow__status-name").TextContent.Trim());
            Assert.Equal("100%", cut.Find(".el-viewer-grow__status-zoom").TextContent.Trim());
        }

        [Fact]
        public void Renders_LoadingOverlayWhenHostSetsLoading()
        {
            var cut = Viewer(p => p.Add(x => x.Loading, true));

            Assert.Equal(ElViewerStatus.Loading, cut.Instance.Status);
            Assert.NotNull(cut.Find(".el-viewer-grow__loading"));
            Assert.Equal("加载中...", cut.Find(".el-viewer-grow__loading-text").TextContent);
        }

        [Fact]
        public void Renders_EnglishStringsWhenLocaleIsEn()
        {
            var cut = Viewer(p => p.Add(x => x.Locale, ElViewerLocale.En));

            Assert.Equal("No document", cut.Find(".el-viewer-grow__placeholder-text").TextContent);
            Assert.Equal("Open file", Button(cut, "Open file").GetAttribute("title"));
        }

        [Fact]
        public void Renders_CustomStringsOverrideLocale()
        {
            var cut = Viewer(p => p.Add(x => x.Strings, new ElViewerGrowStrings { EmptyText = "空空如也" }));

            Assert.Equal("空空如也", cut.Find(".el-viewer-grow__placeholder-text").TextContent);
        }

        [Fact]
        public void Renders_FileInputUsesDefaultAccept()
        {
            var cut = Viewer();

            Assert.Contains(".pdf", cut.Find(".el-viewer-grow__file").GetAttribute("accept"));
        }

        [Fact]
        public void Renders_HiddenToolsAreRemoved()
        {
            var cut = Viewer(p => p.Add(x => x.HiddenTools, new[] { ElViewerToolbarAction.Download, ElViewerToolbarAction.Close }));

            Assert.False(HasButton(cut, "下载"));
            Assert.False(HasButton(cut, "关闭"));
            Assert.True(HasButton(cut, "放大"));
        }

        [Fact]
        public void Renders_OpenFileToolIsRemovedWhenDisabled()
        {
            var cut = Viewer(p => p.Add(x => x.AllowFileOpen, false));

            Assert.False(HasButton(cut, "打开文件"));
            Assert.Empty(cut.FindAll(".el-viewer-grow__file"));
        }

        [Fact]
        public void Renders_PageToolsOnlyWhenPageCountIsKnown()
        {
            Assert.False(HasButton(Viewer(), "下一页"));

            var cut = Viewer(p => p.Add(x => x.PageCount, 5));

            Assert.True(HasButton(cut, "下一页"));
            Assert.Equal("1 / 5", cut.Find(".el-viewer-grow__status-page").TextContent.Trim());
        }

        #endregion

        #region 渲染：文档内容

        [Fact]
        public void Renders_PdfAsIframe()
        {
            var cut = Viewer(p => p.Add(x => x.Src, "https://example.com/a.pdf"));

            Assert.Equal(ElViewerStatus.Ready, cut.Instance.Status);
            Assert.Contains("el-viewer-grow__doc-frame", cut.Markup);
            Assert.Contains("src=\"https://example.com/a.pdf\"", cut.Markup);
            Assert.Contains("is-embed", cut.Find(".el-viewer-grow__canvas").GetAttribute("class"));
        }

        [Fact]
        public void Zoom_EmbedDocument_GrowsBothWidthAndHeight()
        {
            // iframe 形态放大必须宽高一起撑：只给宽度的话布局盒高度仍是视口
            // 100%，外层永远不出纵向滚动条 —— 放大后下方就滚不到。
            var cut = Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/a.pdf");
                p.Add(x => x.Zoom, 1.5);
            });

            var style = cut.Find(".el-viewer-grow__canvas").GetAttribute("style");

            Assert.Contains("width:calc(100% * 1.5)", style);
            Assert.Contains("height:calc(100% * 1.5)", style);
            Assert.DoesNotContain("--el-viewer-grow-transform", style);
        }

        [Fact]
        public void Renders_MarkdownText()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Text, "# 标题\n\n正文 **粗**");
                p.Add(x => x.FileName, "readme.md");
            });

            Assert.Contains("el-viewer-grow__doc-md", cut.Markup);
            Assert.Contains("<h1>标题</h1>", cut.Markup);
            Assert.Contains("<strong>粗</strong>", cut.Markup);
            Assert.Equal("Markdown", cut.Instance.CurrentDocument.Kind.ToString());
        }

        [Fact]
        public void Renders_PlainTextAsPre()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Text, "第一行\n<script>");
                p.Add(x => x.FileName, "log.txt");
            });

            Assert.Contains("el-viewer-grow__doc-pre", cut.Markup);
            Assert.Contains("&lt;script&gt;", cut.Markup);
        }

        [Fact]
        public void Renders_CsvAsTable()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Text, "name,age\n张三,30");
                p.Add(x => x.FileName, "data.csv");
            });

            Assert.Contains("el-viewer-grow__doc-table", cut.Markup);
            Assert.Contains("<th class=\"el-viewer-grow__doc-th\">name</th>", cut.Markup);
            Assert.Contains("<td class=\"el-viewer-grow__doc-td\">张三</td>", cut.Markup);
        }

        [Fact]
        public void Renders_ImageVideoAndAudio()
        {
            Assert.Contains("<img", Viewer(p => p.Add(x => x.Src, "https://x/a.png")).Markup);
            Assert.Contains("<video", Viewer(p => p.Add(x => x.Src, "https://x/a.mp4")).Markup);
            Assert.Contains("<audio", Viewer(p => p.Add(x => x.Src, "https://x/a.mp3")).Markup);
        }

        [Fact]
        public void Renders_BytesSourceInlinesDataUri()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Content, new byte[] { 1, 2, 3 });
                p.Add(x => x.FileName, "a.png");
            });

            Assert.Contains("data:image/png;base64,", cut.Markup);
        }

        [Fact]
        public void Renders_UnsupportedStateForOfficeFiles()
        {
            var cut = Viewer(p => p.Add(x => x.Src, "https://example.com/a.docx"));

            Assert.Equal(ElViewerStatus.Unsupported, cut.Instance.Status);
            Assert.Contains("is-warning", cut.Find(".el-viewer-grow__placeholder").GetAttribute("class"));
            Assert.Equal("暂不支持预览此类型", cut.Find(".el-viewer-grow__placeholder-text").TextContent);
            Assert.Equal("暂不支持预览此类型", cut.Find(".el-viewer-grow__status-message").TextContent);
        }

        [Fact]
        public void Renders_ExternalViewerForOfficeFiles()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/a.docx");
                p.Add(x => x.ExternalViewerUrl, "https://view.example.com/?u={url}");
            });

            Assert.Equal(ElViewerStatus.Ready, cut.Instance.Status);
            Assert.Contains("<iframe", cut.Markup);
            Assert.Contains("https://view.example.com/?u=", cut.Markup);
        }

        [Fact]
        public void Renders_PreferExternalViewerEvenForPdf()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/a.pdf");
                p.Add(x => x.ExternalViewerUrl, "https://view.example.com/?u={url}");
                p.Add(x => x.PreferExternalViewer, true);
            });

            Assert.StartsWith("https://view.example.com/?u=", cut.Find(".el-viewer-grow__doc-frame").GetAttribute("src"));

            // 下载按钮仍然指向原始文件——外链只影响预览，不影响下载。
            Assert.Equal("https://example.com/a.pdf", Button(cut, "下载").GetAttribute("href"));
        }

        [Fact]
        public void Renders_ErrorStateForUnsafeUrl()
        {
            var cut = Viewer(p => p.Add(x => x.Src, "javascript:alert(1)"));

            Assert.Equal(ElViewerStatus.Error, cut.Instance.Status);
            Assert.Equal("地址不被允许", cut.Find(".el-viewer-grow__placeholder-text").TextContent);
        }

        [Fact]
        public void Renders_ContentTemplateOverridesBuiltInRenderer()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/a.docx");
                p.Add(x => x.ContentTemplate, CustomContent());
            });

            Assert.Equal(ElViewerStatus.Ready, cut.Instance.Status);
            Assert.NotNull(cut.Find(".my-viewer-content"));
            Assert.Contains("custom:a.docx", cut.Markup);
        }

        [Fact]
        public void Renders_UnsupportedTemplateWhenProvided()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/a.docx");
                p.Add(x => x.UnsupportedTemplate, CustomContent());
            });

            Assert.Equal(ElViewerStatus.Unsupported, cut.Instance.Status);
            Assert.NotNull(cut.Find(".my-viewer-content"));
        }

        [Fact]
        public void Renders_AutoLoadFalseKeepsEmptyState()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/a.pdf");
                p.Add(x => x.AutoLoad, false);
            });

            Assert.Equal(ElViewerStatus.Empty, cut.Instance.Status);
            Assert.Null(cut.Instance.CurrentDocument);
        }

        #endregion

        #region 交互：缩放 / 旋转 / 最大化

        [Fact]
        public void Interaction_ZoomInAndOutWalkTheLadder()
        {
            var cut = Viewer(p => p.Add(x => x.Src, "https://example.com/a.pdf"));

            Button(cut, "放大").Click();
            Assert.Equal(1.25, cut.Instance.Zoom);
            Assert.Equal("125%", cut.Find(".el-viewer-grow__status-zoom").TextContent.Trim());

            Button(cut, "缩小").Click();
            Assert.Equal(1.0, cut.Instance.Zoom);

            Button(cut, "缩小").Click();
            Assert.Equal(0.75, cut.Instance.Zoom);
        }

        [Fact]
        public void Interaction_ZoomResetReturnsToHundredPercent()
        {
            var cut = Viewer(p => p.Add(x => x.Src, "https://example.com/a.pdf"));

            Button(cut, "放大").Click();
            Button(cut, "放大").Click();
            Assert.Equal(1.5, cut.Instance.Zoom);

            Button(cut, "重置缩放").Click();
            Assert.Equal(1.0, cut.Instance.Zoom);
        }

        [Fact]
        public void Interaction_ZoomButtonsDisableAtLimits()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/a.pdf");
                p.Add(x => x.Zoom, 4.0);
            });

            Assert.Contains("is-disabled", Button(cut, "放大").GetAttribute("class"));

            var small = Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/a.pdf");
                p.Add(x => x.Zoom, 0.25);
            });

            Assert.Contains("is-disabled", Button(small, "缩小").GetAttribute("class"));
        }

        [Fact]
        public void Interaction_RotationAppliesCssTransform()
        {
            var cut = Viewer(p => p.Add(x => x.Src, "https://example.com/a.pdf"));

            Button(cut, "顺时针旋转").Click();
            Assert.Equal(90, cut.Instance.Rotation);
            Assert.Contains("rotate(90deg)", cut.Find(".el-viewer-grow__canvas").GetAttribute("style"));

            Button(cut, "逆时针旋转").Click();
            Assert.Equal(0, cut.Instance.Rotation);

            Button(cut, "逆时针旋转").Click();
            Assert.Equal(270, cut.Instance.Rotation);
        }

        [Fact]
        public void Interaction_MaximizeTogglesClassAndTitle()
        {
            var cut = Viewer(p => p.Add(x => x.Src, "https://example.com/a.pdf"));

            Button(cut, "最大化").Click();

            Assert.True(cut.Instance.Maximized);
            Assert.Contains("is-maximized", cut.Find(".el-viewer-grow").GetAttribute("class"));
            Assert.Equal("退出最大化", Button(cut, "退出最大化").GetAttribute("title"));

            Button(cut, "退出最大化").Click();
            Assert.False(cut.Instance.Maximized);
        }

        [Fact]
        public void Interaction_DownloadAndNewTabRenderAsAnchors()
        {
            var cut = Viewer(p => p.Add(x => x.Src, "https://example.com/a.pdf"));

            var download = Button(cut, "下载");

            Assert.Equal("A", download.TagName);
            Assert.Equal("https://example.com/a.pdf", download.GetAttribute("href"));
            Assert.Equal("a.pdf", download.GetAttribute("download"));

            var newTab = Button(cut, "新标签页打开");

            Assert.Equal("_blank", newTab.GetAttribute("target"));
            Assert.Equal("noopener noreferrer", newTab.GetAttribute("rel"));
        }

        [Fact]
        public void Interaction_CloseReturnsToEmptyState()
        {
            var cut = Viewer(p => p.Add(x => x.Src, "https://example.com/a.pdf"));

            Button(cut, "关闭").Click();

            Assert.Equal(ElViewerStatus.Empty, cut.Instance.Status);
            Assert.Null(cut.Instance.CurrentDocument);
            Assert.NotNull(cut.Find(".el-viewer-grow__placeholder"));
        }

        [Fact]
        public void Interaction_ReloadKeepsDocument()
        {
            var cut = Viewer(p => p.Add(x => x.Src, "https://example.com/a.pdf"));

            Button(cut, "重新加载").Click();

            Assert.Equal(ElViewerStatus.Ready, cut.Instance.Status);
            Assert.Equal("a.pdf", cut.Instance.CurrentDocument.FileName);
        }

        [Fact]
        public void Interaction_PagingMovesWithinRange()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/a.pdf");
                p.Add(x => x.PageCount, 5);
                p.Add(x => x.PageNumber, 1);
            });

            Button(cut, "下一页").Click();
            Assert.Equal(2, cut.Instance.PageNumber);
            Assert.Equal("2 / 5", cut.Find(".el-viewer-grow__status-page").TextContent.Trim());

            Button(cut, "上一页").Click();
            Assert.Equal(1, cut.Instance.PageNumber);
        }

        [Fact]
        public void Interaction_PrevIsDisabledOnFirstPage()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/a.pdf");
                p.Add(x => x.PageCount, 5);
                p.Add(x => x.PageNumber, 1);
            });

            Assert.Contains("is-disabled", Button(cut, "上一页").GetAttribute("class"));
            Assert.DoesNotContain("is-disabled", Button(cut, "下一页").GetAttribute("class"));
        }

        #endregion

        #region 交互：URL 输入条与事件

        [Fact]
        public void Interaction_UrlBarToggleAndSubmit()
        {
            var cut = Viewer();

            Button(cut, "输入地址").Click();
            Assert.NotNull(cut.Find(".el-viewer-grow__urlbar"));

            cut.Find(".el-viewer-grow__url-input").Change("https://example.com/report.pdf");
            cut.Find(".el-viewer-grow__url-submit").Click();

            Assert.Equal(ElViewerStatus.Ready, cut.Instance.Status);
            Assert.Equal("report.pdf", cut.Instance.CurrentDocument.FileName);
        }

        [Fact]
        public void Interaction_UrlSubmitRejectsUnsafeAddress()
        {
            var cut = Viewer();

            Button(cut, "输入地址").Click();
            cut.Find(".el-viewer-grow__url-input").Change("javascript:alert(1)");
            cut.Find(".el-viewer-grow__url-submit").Click();

            Assert.Equal(ElViewerStatus.Error, cut.Instance.Status);
            Assert.Equal("地址不被允许", cut.Instance.ErrorMessage);
        }

        [Fact]
        public void Interaction_LoadUrlMethodUpdatesDocument()
        {
            var cut = Viewer();

            cut.InvokeAsync(() => cut.Instance.LoadUrlAsync("https://example.com/b.pdf"));

            Assert.Equal(ElViewerStatus.Ready, cut.Instance.Status);
            Assert.Equal("b.pdf", cut.Instance.CurrentDocument.FileName);
        }

        [Fact]
        public void Interaction_LoadTextAndBytesWork()
        {
            var cut = Viewer();

            cut.InvokeAsync(() => cut.Instance.LoadTextAsync("hello", "note.txt"));
            Assert.Equal("hello", cut.Instance.CurrentDocument.Text);

            cut.InvokeAsync(() => cut.Instance.LoadBytesAsync(new byte[] { 9, 9 }, "blob.bin"));
            Assert.Equal(2, cut.Instance.CurrentDocument.Size);
        }

        [Fact]
        public void Interaction_DocumentChangedEventFires()
        {
            ElViewerDocument seen = null;
            var cut = Viewer(p => p.Add(x => x.OnDocumentChanged, (ElViewerDocumentEventArgs args) => seen = args.Document));

            cut.InvokeAsync(() => cut.Instance.LoadUrlAsync("https://example.com/c.pdf"));

            Assert.NotNull(seen);
            Assert.Equal("c.pdf", seen.FileName);
        }

        [Fact]
        public void Interaction_StatusChangedEventFiresWithTransitions()
        {
            var events = new List<ElViewerStatus>();
            var cut = Viewer(p => p.Add(x => x.OnStatusChanged, (ElViewerStateChangedEventArgs args) => events.Add(args.Status)));

            cut.InvokeAsync(() => cut.Instance.LoadUrlAsync("https://example.com/c.pdf"));

            Assert.Equal(ElViewerStatus.Ready, events.First());

            cut.InvokeAsync(() => cut.Instance.CloseAsync());

            Assert.Equal(ElViewerStatus.Empty, events.Last());
        }

        [Fact]
        public void Interaction_ErrorEventFiresForUnsafeUrl()
        {
            ElViewerErrorEventArgs error = null;
            var cut = Viewer(p => p.Add(x => x.OnError, (ElViewerErrorEventArgs args) => error = args));

            cut.InvokeAsync(() => cut.Instance.LoadUrlAsync("javascript:alert(1)"));

            Assert.NotNull(error);
            Assert.Equal("地址不被允许", error.Message);
        }

        #endregion

        #region 交互：回调与自定义工具条

        [Fact]
        public void Renders_CustomToolbarReplacesDefault()
        {
            var cut = Viewer(p => p.Add(x => x.Toolbar, ElViewerGrowToolbar.Build(
                ElViewerToolbarAction.ZoomIn,
                ElViewerToolbarAction.Separator,
                ElViewerToolbarAction.Download)));

            // 三个工具里有一个是分隔符，只产出 2 个按钮
            Assert.Equal(2, cut.FindAll(".el-viewer-grow__button").Count);
            Assert.Single(cut.FindAll(".el-viewer-grow__divider"));
            Assert.True(HasButton(cut, "放大"));
            Assert.False(HasButton(cut, "关闭"));
        }

        [Fact]
        public void Interaction_ZoomChangedCallbackFires()
        {
            double seen = 0;
            var cut = Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/a.pdf");
                p.Add(x => x.ZoomChanged, (double value) => seen = value);
            });

            Button(cut, "放大").Click();

            Assert.Equal(1.25, seen);
        }

        [Fact]
        public void Interaction_RotationChangedCallbackFires()
        {
            int seen = -1;
            var cut = Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/a.pdf");
                p.Add(x => x.RotationChanged, (int value) => seen = value);
            });

            Button(cut, "顺时针旋转").Click();

            Assert.Equal(90, seen);
        }

        [Fact]
        public void Interaction_MaximizedChangedCallbackFires()
        {
            var seen = new List<bool>();
            var cut = Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/a.pdf");
                p.Add(x => x.MaximizedChanged, (bool value) => seen.Add(value));
            });

            Button(cut, "最大化").Click();
            Button(cut, "退出最大化").Click();

            Assert.Equal(new[] { true, false }, seen.ToArray());
        }

        [Fact]
        public void Interaction_PageNumberChangedCallbackFires()
        {
            int seen = 0;
            var cut = Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/a.pdf");
                p.Add(x => x.PageCount, 3);
                p.Add(x => x.PageNumber, 1);
                p.Add(x => x.PageNumberChanged, (int value) => seen = value);
            });

            Button(cut, "下一页").Click();

            Assert.Equal(2, seen);
        }

        [Fact]
        public void Interaction_ResetViewRestoresZoomAndRotation()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/a.pdf");
                p.Add(x => x.Zoom, 2.0);
                p.Add(x => x.Rotation, 90);
            });

            cut.InvokeAsync(() => cut.Instance.ResetViewAsync());

            Assert.Equal(1.0, cut.Instance.Zoom);
            Assert.Equal(0, cut.Instance.Rotation);
        }

        [Fact]
        public void Renders_DocumentParameterIsUsed()
        {
            var cut = Viewer(p => p.Add(x => x.Document, ElViewerDocument.FromText("# 标题", "a.md")));

            Assert.Equal(ElViewerStatus.Ready, cut.Instance.Status);
            Assert.Contains("<h1>标题</h1>", cut.Markup);
            Assert.Equal("a.md", cut.Find(".el-viewer-grow__status-name").TextContent.Trim());
        }

        [Fact]
        public void Renders_UnsafeMarkupInTextIsEscaped()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Text, "<img src=x onerror=alert(1)>");
                p.Add(x => x.FileName, "a.txt");
            });

            Assert.DoesNotContain("<img", cut.Markup);
            Assert.Contains("&lt;img", cut.Markup);
        }

        [Fact]
        public void Renders_StatusBarShowsKindAndSize()
        {
            var cut = Viewer(p => p.Add(x => x.Text, "abc"));

            Assert.Equal("Text · 3 B", cut.Find(".el-viewer-grow__status-meta").TextContent.Trim());
        }

        [Fact]
        public void Engine_DefaultAcceptListsOnlyRenderableTypes()
        {
            var accept = ElViewerGrowEngine.DefaultAccept;

            foreach (var supported in new[] { ".pdf", ".txt", ".md", ".csv", ".json", ".png", ".mp4", ".mp3" })
            {
                Assert.Contains(supported, accept);
            }

            // 参考实现把 dxf / glb / stl 也列进 accept，但 Blazor 侧根本没有渲染能力 —— 属于误导。
            foreach (var misleading in new[] { ".docx", ".xlsx", ".pptx", ".ofd", ".dxf", ".glb", ".stl" })
            {
                Assert.DoesNotContain(misleading, accept);
            }
        }

        #endregion

        #region 可插拔渲染器（OFD 等）

        /// <summary>不是真 OFD，只用来把类型识别落到 Ofd；渲染结果完全由注入的渲染器决定。</summary>
        private static byte[] OfdBytes()
        {
            return Utf8("PK\x03\x04fake-ofd-payload");
        }

        [Fact]
        public void Renderer_OfdWithoutRenderer_IsNotRenderable()
        {
            var document = ElViewerDocument.FromBytes(OfdBytes(), "report.ofd");

            Assert.Equal(ElViewerDocumentKind.Ofd, document.Kind);
            Assert.False(ElViewerGrowRenderer.HasRenderer(ElViewerDocumentKind.Ofd));
            Assert.False(ElViewerGrowRenderer.CanRender(document));
            Assert.Equal(string.Empty, ElViewerGrowRenderer.RenderContent(document));
        }

        [Fact]
        public void Renderer_RegisteredOfdRenderer_RendersSvgFragment()
        {
            try
            {
                ElViewerGrowRenderer.RegisterRenderer(
                    ElViewerDocumentKind.Ofd,
                    bytes => "<svg viewBox=\"0 0 100 100\"><rect width=\"10\" height=\"10\"/></svg>");

                var document = ElViewerDocument.FromBytes(OfdBytes(), "report.ofd");

                Assert.True(ElViewerGrowRenderer.HasRenderer(ElViewerDocumentKind.Ofd));
                Assert.True(ElViewerGrowRenderer.CanRender(document));

                var html = ElViewerGrowRenderer.RenderContent(document);

                Assert.Contains("el-viewer-grow__doc-external", html);
                Assert.Contains("<svg viewBox=", html);
            }
            finally
            {
                ElViewerGrowRenderer.UnregisterRenderer(ElViewerDocumentKind.Ofd);
            }
        }

        [Fact]
        public void Renderer_UnregisterRenderer_RestoresUnsupportedState()
        {
            ElViewerGrowRenderer.RegisterRenderer(ElViewerDocumentKind.Ofd, bytes => "<svg/>");
            ElViewerGrowRenderer.UnregisterRenderer(ElViewerDocumentKind.Ofd);

            Assert.False(ElViewerGrowRenderer.HasRenderer(ElViewerDocumentKind.Ofd));
            Assert.False(ElViewerGrowRenderer.CanRender(ElViewerDocument.FromBytes(OfdBytes(), "report.ofd")));
        }

        [Fact]
        public void Renderer_RegisterWithNull_ActsAsUnregister()
        {
            ElViewerGrowRenderer.RegisterRenderer(ElViewerDocumentKind.Ofd, bytes => "<svg/>");
            ElViewerGrowRenderer.RegisterRenderer(ElViewerDocumentKind.Ofd, null);

            Assert.False(ElViewerGrowRenderer.HasRenderer(ElViewerDocumentKind.Ofd));
        }

        [Fact]
        public void Renderer_RendererThrows_FallsBackToBuiltInPath()
        {
            try
            {
                ElViewerGrowRenderer.RegisterRenderer(
                    ElViewerDocumentKind.Pdf,
                    bytes => { throw new InvalidOperationException("渲染器炸了"); });

                var document = ElViewerDocument.FromBytes(Utf8("%PDF-1.7"), "a.pdf");

                // 注入的渲染器坏了也不能白屏：回退到浏览器内建的 iframe 路径。
                var html = ElViewerGrowRenderer.RenderContent(document);

                Assert.Contains("<iframe", html);
                Assert.DoesNotContain("el-viewer-grow__doc-external", html);
            }
            finally
            {
                ElViewerGrowRenderer.UnregisterRenderer(ElViewerDocumentKind.Pdf);
            }
        }

        [Fact]
        public void Renderer_RendererReturnsEmpty_FallsBackToBuiltInPath()
        {
            try
            {
                ElViewerGrowRenderer.RegisterRenderer(ElViewerDocumentKind.Image, bytes => "   ");
                ElViewerGrowRenderer.RegisterRenderer(ElViewerDocumentKind.Pdf, bytes => "   ");

                var byUrl = ElViewerDocument.FromUrl("https://example.com/a.pdf");

                // 只有地址、没有字节时渲染器根本没得调用，照旧走浏览器内建的 iframe。
                Assert.Contains("<iframe", ElViewerGrowRenderer.RenderContent(byUrl));

                var byBytes = ElViewerDocument.FromBytes(Utf8("%PDF-1.7"), "a.pdf");

                Assert.Contains("<iframe", ElViewerGrowRenderer.RenderContent(byBytes));
            }
            finally
            {
                ElViewerGrowRenderer.UnregisterRenderer(ElViewerDocumentKind.Image);
                ElViewerGrowRenderer.UnregisterRenderer(ElViewerDocumentKind.Pdf);
            }
        }

        [Fact]
        public void Renderer_SanitizeMarkup_StripsScriptsEventsAndDangerousUris()
        {
            var markup = "<svg onload=\"alert(1)\">"
                + "<script>alert(2)</script>"
                + "<a xlink:href=\"javascript:alert(3)\">x</a>"
                + "<image href=\"https://example.com/y.png\"/>"
                + "</svg>";

            var clean = ElViewerGrowRenderer.SanitizeMarkup(markup);

            Assert.DoesNotContain("onload", clean);
            Assert.DoesNotContain("script", clean);
            Assert.DoesNotContain("javascript:", clean);
            Assert.Contains("https://example.com/y.png", clean);
        }

        [Fact]
        public void Renderer_ExternalMarkupIsSanitizedBeforeInlining()
        {
            try
            {
                ElViewerGrowRenderer.RegisterRenderer(
                    ElViewerDocumentKind.Ofd,
                    bytes => "<svg><script>alert(1)</script><rect onclick=\"boom()\"/></svg>");

                var html = ElViewerGrowRenderer.RenderContent(ElViewerDocument.FromBytes(OfdBytes(), "a.ofd"));

                Assert.DoesNotContain("script", html);
                Assert.DoesNotContain("onclick", html);
                Assert.Contains("<rect", html);
            }
            finally
            {
                ElViewerGrowRenderer.UnregisterRenderer(ElViewerDocumentKind.Ofd);
            }
        }

        [Fact]
        public void Viewer_Ofd_BridgeUnavailable_FallsBackToUnsupported()
        {
            // bUnit 的宽松 JSInterop 把 elementOfdViewerAvailable 当 false 返回，
            // 等价于「宿主没引 dom.js」：这时组件必须落回「不支持」，不能留一个空画布。
            var cut = Viewer(p => p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "report.ofd")));

            cut.WaitForAssertion(() => Assert.Equal(ElViewerStatus.Unsupported, cut.Instance.Status), timeout: TestWait.Timeout);
            Assert.NotNull(cut.Find(".el-viewer-grow__placeholder"));
        }

        [Fact]
        public void Viewer_OfdWithRenderer_RendersInjectedSvg()
        {
            try
            {
                ElViewerGrowRenderer.RegisterRenderer(
                    ElViewerDocumentKind.Ofd,
                    bytes => "<svg viewBox=\"0 0 210 297\"><rect width=\"10\" height=\"10\"/></svg>");

                var cut = Viewer(p => p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "report.ofd")));

                Assert.Equal(ElViewerStatus.Ready, cut.Instance.Status);
                Assert.NotNull(cut.Find(".el-viewer-grow__doc-external"));
                Assert.Contains("<rect", cut.Markup);
            }
            finally
            {
                ElViewerGrowRenderer.UnregisterRenderer(ElViewerDocumentKind.Ofd);
            }
        }

        [Fact]
        public void Viewer_ResolvedAccept_AppendsOfdWhenAnyOfdPathExists()
        {
            // 内置通道默认开，所以 accept 默认就带 .ofd。
            Assert.Contains(".ofd", Viewer().Instance.ResolvedAccept);

            // 关掉内置通道、又没有注入渲染器：选到 .ofd 也开不了，就不该出现在 accept 里。
            Assert.DoesNotContain(".ofd", Viewer(p => p.Add(x => x.EnableOfdPreview, false)).Instance.ResolvedAccept);

            try
            {
                ElViewerGrowRenderer.RegisterRenderer(ElViewerDocumentKind.Ofd, bytes => "<svg/>");

                // 注入渲染器算另一条路：即使内置通道关着，accept 也要带上 .ofd。
                Assert.Contains(".ofd", Viewer(p => p.Add(x => x.EnableOfdPreview, false)).Instance.ResolvedAccept);
            }
            finally
            {
                ElViewerGrowRenderer.UnregisterRenderer(ElViewerDocumentKind.Ofd);
            }
        }

        [Fact]
        public void Viewer_ContentLoader_FetchesBytesForRemoteOfd()
        {
            try
            {
                ElViewerGrowRenderer.RegisterRenderer(
                    ElViewerDocumentKind.Ofd,
                    bytes => "<svg>" + bytes.Length + "</svg>");

                var requested = 0;

                var cut = Viewer(p =>
                {
                    p.Add(x => x.ContentLoader, (Func<ElViewerDocument, Task<byte[]>>)(document =>
                    {
                        requested++;
                        return Task.FromResult(OfdBytes());
                    }));
                });

                cut.InvokeAsync(() => cut.Instance.LoadUrlAsync("https://example.com/report.ofd"));

                Assert.Equal(1, requested);
                Assert.Equal(ElViewerStatus.Ready, cut.Instance.Status);
                Assert.NotNull(cut.Find(".el-viewer-grow__doc-external"));
                Assert.Contains("<svg>" + OfdBytes().Length + "</svg>", cut.Markup);
            }
            finally
            {
                ElViewerGrowRenderer.UnregisterRenderer(ElViewerDocumentKind.Ofd);
            }
        }

        [Fact]
        public void Viewer_ContentLoader_IsCalledForBuiltInOfdChannel()
        {
            // 内置通道自己也能按地址取流，但宿主给了 ContentLoader 时优先让宿主取
            // （宿主可以在里面挂鉴权头），组件再把字节转交给通道。
            var requested = 0;

            var cut = Viewer(p =>
            {
                p.Add(x => x.ContentLoader, (Func<ElViewerDocument, Task<byte[]>>)(document =>
                {
                    requested++;
                    return Task.FromResult(OfdBytes());
                }));
            });

            cut.InvokeAsync(() => cut.Instance.LoadUrlAsync("https://example.com/report.ofd"));

            Assert.Equal(1, requested);
            Assert.True(cut.Instance.CurrentDocument.HasContent);
        }

        [Fact]
        public void Viewer_ContentLoaderIsNotCalled_WhenBothOfdPathsAreOff()
        {
            var requested = 0;

            var cut = Viewer(p =>
            {
                p.Add(x => x.EnableOfdPreview, false);
                p.Add(x => x.ContentLoader, (Func<ElViewerDocument, Task<byte[]>>)(document =>
                {
                    requested++;
                    return Task.FromResult(OfdBytes());
                }));
            });

            cut.InvokeAsync(() => cut.Instance.LoadUrlAsync("https://example.com/report.ofd"));

            // 内置通道关着、又没有注入渲染器：没人要字节，不该白跑一次下载。
            Assert.Equal(0, requested);
            Assert.Equal(ElViewerStatus.Unsupported, cut.Instance.Status);
        }

        #endregion

        #region 内置 OFD 通道

        /// <summary>取 <c>elementOfdViewerMount</c> 的第二个参数（挂载参数 JSON），供谓词按文档区分。</summary>
        private static string PayloadOf(JSRuntimeInvocation invocation)
        {
            return invocation.Arguments.Count > 1 ? invocation.Arguments[1] as string : string.Empty;
        }

        /// <summary>把桥接装成「可用 + 指定页数」，并返回挂载参数（JSON）的取值记录。</summary>
        private (IRenderedComponent<ElViewerGrow> Cut, List<string> Payloads) MountOfd(
            Action<ComponentParameterCollectionBuilder<ElViewerGrow>> configure,
            int pageCount)
        {
            JSInterop.Setup<bool>("elementOfdViewerAvailable").SetResult(true);

            var payloads = new List<string>();

            JSInterop.Setup<int>("elementOfdViewerMount", invocation =>
            {
                payloads.Add(invocation.Arguments.Count > 1 ? invocation.Arguments[1] as string : null);
                return true;
            }).SetResult(pageCount);

            JSInterop.SetupVoid("elementOfdViewerDispose", _ => true).SetVoidResult();
            JSInterop.SetupVoid("elementOfdViewerScrollToPage", _ => true).SetVoidResult();

            var cut = Viewer(configure);

            cut.WaitForAssertion(() => Assert.True(cut.Instance.IsOfdMounted), timeout: TestWait.Timeout);

            return (cut, payloads);
        }

        [Fact]
        public void Viewer_EnableOfdPreview_DefaultsToTrue()
        {
            Assert.True(Viewer().Instance.EnableOfdPreview);
        }

        [Fact]
        public void Viewer_UseBuiltInOfd_IsTrueOnlyUnderTheRightConditions()
        {
            var ofdBytes = ElViewerDocument.FromBytes(OfdBytes(), "report.ofd");
            var pdfBytes = ElViewerDocument.FromBytes(Utf8("%PDF-1.7"), "a.pdf");

            // 开、有字节 → 走内置通道
            Assert.True(Viewer(p => p.Add(x => x.Document, ofdBytes)).Instance.UseBuiltInOfd);

            // 开、只有地址 → 也是内置通道（通道自己按地址取流）
            Assert.True(Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/report.ofd");
                p.Add(x => x.FileName, "report.ofd");
            }).Instance.UseBuiltInOfd);

            // 关掉开关 → 不走
            Assert.False(Viewer(p =>
            {
                p.Add(x => x.Document, ofdBytes);
                p.Add(x => x.EnableOfdPreview, false);
            }).Instance.UseBuiltInOfd);

            // 宿主注入了自己的渲染器 → 让位
            try
            {
                ElViewerGrowRenderer.RegisterRenderer(ElViewerDocumentKind.Ofd, bytes => "<svg/>");

                Assert.False(Viewer(p => p.Add(x => x.Document, ofdBytes)).Instance.UseBuiltInOfd);
            }
            finally
            {
                ElViewerGrowRenderer.UnregisterRenderer(ElViewerDocumentKind.Ofd);
            }

            // 宿主自己接管正文（ContentTemplate）→ 让位
            var templated = Render<ElViewerGrow>(p =>
            {
                p.Add(x => x.Document, ofdBytes);
                p.Add(x => x.ContentTemplate, CustomContent());
            });

            Assert.False(templated.Instance.UseBuiltInOfd);

            // 配了外链预览服务 → 让位
            Assert.False(Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/report.ofd");
                p.Add(x => x.ExternalViewerUrl, "https://view.example.com/?src={url}");
            }).Instance.UseBuiltInOfd);

            // 不是 OFD → 与内置通道无关
            Assert.False(Viewer(p => p.Add(x => x.Document, pdfBytes)).Instance.UseBuiltInOfd);

            // 地址不安全 → 一律不走
            Assert.False(Viewer(p => p.Add(x => x.Src, "javascript:alert(1)")).Instance.UseBuiltInOfd);
        }

        [Fact]
        public void Viewer_OfdMount_PassesBytesAsBase64ToTheBridge()
        {
            var (cut, payloads) = MountOfd(
                p => p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "report.ofd")),
                2);

            Assert.Single(payloads);
            Assert.Contains("\"base64\":\"", payloads[0]);
            Assert.Contains("\"fileName\":\"report.ofd\"", payloads[0]);
            Assert.Contains("\"maxWidth\":900", payloads[0]);

            // 有字节就不该同时塞地址，免得通道又去取一次流。
            Assert.DoesNotContain("\"url\":", payloads[0]);

            // 组件自己不做任何 OFD 解析，页数是通道算出来的。
            Assert.Equal(2, cut.Instance.OfdPageCount);
        }

        [Fact]
        public void Viewer_OfdMount_PassesUrlWhenThereAreNoBytes()
        {
            var (_, payloads) = MountOfd(
                p =>
                {
                    p.Add(x => x.Src, "https://example.com/report.ofd");
                    p.Add(x => x.FileName, "report.ofd");
                },
                1);

            Assert.Single(payloads);
            Assert.Contains("\"url\":\"https://example.com/report.ofd\"", payloads[0]);
            Assert.DoesNotContain("base64", payloads[0]);
        }

        [Fact]
        public void Viewer_OfdMount_BackfillsPageCountWithoutTouchingHostParameter()
        {
            var (cut, _) = MountOfd(
                p => p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "report.ofd")),
                3);

            // 生效页数来自通道，但宿主的 PageCount 参数保持原样 —— 回写就会粘住页数。
            Assert.Equal(3, cut.Instance.EffectivePageCount);
            Assert.Equal(0, cut.Instance.PageCount);
            Assert.Equal("1 / 3", cut.Instance.PageLabel);
            Assert.True(HasButton(cut, "下一页"));
        }

        [Fact]
        public void Viewer_OfdMount_HostPageCountWinsOverBridgePageCount()
        {
            var (cut, _) = MountOfd(
                p =>
                {
                    p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "report.ofd"));
                    p.Add(x => x.PageCount, 5);
                },
                3);

            // 宿主显式给过页数就以宿主为准（宿主可能自己在做分页）。
            Assert.Equal(5, cut.Instance.EffectivePageCount);
            Assert.Equal("1 / 5", cut.Instance.PageLabel);
        }

        [Fact]
        public void Viewer_OfdMount_PageCountDoesNotStickAcrossDocuments()
        {
            // 回归：先开 1 页的文档，再换成 2 页的文档。
            // 早期实现把通道页数回写进 PageCount，第二份文档会一直显示「1 / 1」。
            JSInterop.Setup<bool>("elementOfdViewerAvailable").SetResult(true);

            // 按文件名分别给页数：谓词互斥，不依赖 setup 的匹配顺序。
            JSInterop.Setup<int>("elementOfdViewerMount", i => PayloadOf(i).Contains("first.ofd")).SetResult(1);
            JSInterop.Setup<int>("elementOfdViewerMount", i => PayloadOf(i).Contains("second.ofd")).SetResult(2);
            JSInterop.SetupVoid("elementOfdViewerDispose", _ => true).SetVoidResult();

            var cut = Render<ElViewerGrow>(p => p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "first.ofd")));

            cut.WaitForAssertion(() => Assert.Equal(1, cut.Instance.EffectivePageCount), timeout: TestWait.Timeout);

            cut.Render(p => p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "second.ofd")));

            cut.WaitForAssertion(() => Assert.Equal(2, cut.Instance.EffectivePageCount), timeout: TestWait.Timeout);

            // 换文档时会重新挂载到同一个容器（通道自己清空重画），不是释放。
            Assert.Equal(2, JSInterop.Invocations.Count(i => i.Identifier == "elementOfdViewerMount"));
        }

        [Fact]
        public void Viewer_OfdMount_LongerDocumentToShorterClampsPageNumber()
        {
            JSInterop.Setup<bool>("elementOfdViewerAvailable").SetResult(true);

            JSInterop.Setup<int>("elementOfdViewerMount", i => PayloadOf(i).Contains("five.ofd")).SetResult(5);
            JSInterop.Setup<int>("elementOfdViewerMount", i => PayloadOf(i).Contains("two.ofd")).SetResult(2);
            JSInterop.SetupVoid("elementOfdViewerDispose", _ => true).SetVoidResult();
            JSInterop.SetupVoid("elementOfdViewerScrollToPage", _ => true).SetVoidResult();

            var cut = Render<ElViewerGrow>(p => p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "five.ofd")));

            cut.WaitForAssertion(() => Assert.Equal(5, cut.Instance.EffectivePageCount), timeout: TestWait.Timeout);

            cut.InvokeAsync(() => cut.Instance.GoToPageAsync(5));
            cut.WaitForAssertion(() => Assert.Equal(5, cut.Instance.PageNumber), timeout: TestWait.Timeout);

            cut.Render(p => p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "two.ofd")));

            // 换成更短的文档时页码要收回来，不能出现「5 / 2」。
            cut.WaitForAssertion(() => Assert.Equal(2, cut.Instance.PageNumber), timeout: TestWait.Timeout);
        }

        [Fact]
        public void Viewer_OfdMount_IsAttemptedOncePerDocumentAndSize()
        {
            var (cut, payloads) = MountOfd(
                p => p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "report.ofd")),
                2);

            cut.Render(p => p.Add(x => x.Zoom, 1.5));
            cut.Render(p => p.Add(x => x.Theme, ElViewerTheme.Dark));

            // 缩放 / 主题不影响可用宽度，不该重挂（重挂会得到重复的页）。
            Assert.Single(payloads);

            // 但最大化会改变可用宽度，必须重挂一次。
            cut.InvokeAsync(() => cut.Instance.ToggleMaximizeAsync());

            cut.WaitForAssertion(() => Assert.Equal(2, payloads.Count), timeout: TestWait.Timeout);
        }

        [Fact]
        public void Viewer_OfdMount_TurningOffTheSwitchDisposesTheChannel()
        {
            var (cut, _) = MountOfd(
                p => p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "report.ofd")),
                2);

            cut.Render(p => p.Add(x => x.EnableOfdPreview, false));

            cut.WaitForAssertion(() => Assert.False(cut.Instance.IsOfdMounted), timeout: TestWait.Timeout);
            Assert.Contains(JSInterop.Invocations, i => i.Identifier == "elementOfdViewerDispose");
        }

        [Fact]
        public void Viewer_OfdMount_FailureSurfacesAsError()
        {
            JSInterop.Setup<bool>("elementOfdViewerAvailable").SetResult(true);
            JSInterop.Setup<int>("elementOfdViewerMount", _ => true)
                .SetException(new InvalidOperationException("ofd core failed to load"));

            var errors = new List<string>();

            var cut = Render<ElViewerGrow>(p =>
            {
                p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "report.ofd"));
                p.Add(x => x.OnError, (ElViewerErrorEventArgs args) => errors.Add(args.Message));
            });

            // 通道在、只是这一份没渲染出来 —— 这是真故障，必须报错而不是静默「不支持」。
            cut.WaitForAssertion(() => Assert.Equal(ElViewerStatus.Error, cut.Instance.Status), timeout: TestWait.Timeout);
            Assert.Equal("ofd core failed to load", cut.Instance.ErrorMessage);
            Assert.Single(errors);
        }

        [Fact]
        public void Viewer_OfdMount_BridgeUnavailableIsNotAnError()
        {
            // 「宿主没引 dom.js」是能力缺失，不是故障：不该触发 OnError。
            JSInterop.Setup<bool>("elementOfdViewerAvailable").SetResult(false);

            var errors = new List<string>();

            var cut = Render<ElViewerGrow>(p =>
            {
                p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "report.ofd"));
                p.Add(x => x.OnError, (ElViewerErrorEventArgs args) => errors.Add(args.Message));
            });

            cut.WaitForAssertion(() => Assert.Equal(ElViewerStatus.Unsupported, cut.Instance.Status), timeout: TestWait.Timeout);
            Assert.Empty(errors);
            Assert.Null(cut.Instance.ErrorMessage);
            Assert.Equal(0, JSInterop.Invocations.Count(i => i.Identifier == "elementOfdViewerMount"));
        }

        [Fact]
        public void Viewer_OfdMount_NewDocumentGetsAFreshChanceAfterFailure()
        {
            JSInterop.Setup<bool>("elementOfdViewerAvailable").SetResult(true);

            // 坏的那份炸，好的那份正常。谓词互斥，与 setup 匹配顺序无关。
            JSInterop.Setup<int>("elementOfdViewerMount", i => PayloadOf(i).Contains("broken.ofd"))
                .SetException(new InvalidOperationException("boom"));
            JSInterop.Setup<int>("elementOfdViewerMount", i => PayloadOf(i).Contains("good.ofd")).SetResult(2);

            JSInterop.SetupVoid("elementOfdViewerDispose", _ => true).SetVoidResult();

            var cut = Render<ElViewerGrow>(p => p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "broken.ofd")));

            cut.WaitForAssertion(() => Assert.Equal(ElViewerStatus.Error, cut.Instance.Status), timeout: TestWait.Timeout);

            cut.Render(p => p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "good.ofd")));

            // 上一份的失败状态不能粘在新文档上。
            cut.WaitForAssertion(() => Assert.Equal(ElViewerStatus.Ready, cut.Instance.Status), timeout: TestWait.Timeout);
            Assert.Equal(2, cut.Instance.EffectivePageCount);
            Assert.Null(cut.Instance.ErrorMessage);
        }

        /// <summary>
        /// #166：命令式（文件选择器走 <c>LoadBytesAsync</c>，不是参数驱动）先传一份解析失败的
        /// .ofd、紧接着传正常文档。失败态把状态打成 Error，而 Error 分支根本不画挂载点
        /// （<c>ElViewerGrow.razor</c> 的画布分支），此时 <c>ofdHost</c> 指向上一帧就被移除的元素，
        /// 交给 JS 只会收到 null —— 浏览器里就是用户报的那条
        /// "elementOfdViewer: container is null"，而且这份文档再也不会被画出来。
        /// <para>判据取「每一次挂载调用发生的那一刻，挂载点在不在渲染树里」：bUnit 没有布局引擎，
        /// 但渲染树里有没有那个 div 是 markup 层看得见的事实。修复前 good.ofd 那一次调用记到 false。</para>
        /// </summary>
        [Fact]
        public async Task Viewer_OfdImperativeLoadAfterFailure_MountsOnlyWhileTheHostIsInTheDocument()
        {
            JSInterop.Setup<bool>("elementOfdViewerAvailable").SetResult(true);
            JSInterop.SetupVoid("elementOfdViewerDispose", _ => true).SetVoidResult();
            JSInterop.SetupVoid("elementOfdViewerScrollToPage", _ => true).SetVoidResult();

            var hostPresentAtMount = new List<(string File, bool Present)>();
            IRenderedComponent<ElViewerGrow> cut = null;

            // 谓词里顺手记账（与 MountOfd 同一手法）：记录「这次挂载调用发生的那一刻，
            // 渲染树里有没有挂载点」。不匹配自己文档名的调用原样放行给下一条 setup。
            bool Record(string fileName, JSRuntimeInvocation invocation)
            {
                var payload = PayloadOf(invocation);

                if (payload == null || !payload.Contains(fileName))
                {
                    return false;
                }

                hostPresentAtMount.Add((fileName, cut?.Markup.Contains("el-viewer-grow__ofd") == true));

                return true;
            }

            JSInterop.Setup<int>("elementOfdViewerMount", i => Record("broken.ofd", i))
                .SetException(new InvalidOperationException("boom"));
            JSInterop.Setup<int>("elementOfdViewerMount", i => Record("good.ofd", i)).SetResult(2);

            cut = Viewer();

            await cut.InvokeAsync(() => cut.Instance.LoadBytesAsync(OfdBytes(), "broken.ofd"));
            cut.WaitForAssertion(() => Assert.Equal(ElViewerStatus.Error, cut.Instance.Status), timeout: TestWait.Timeout);

            // 前提：错误分支里确实没有挂载点（这条不成立，下面的判据就是空转）
            Assert.DoesNotContain("el-viewer-grow__ofd", cut.Markup);
            Assert.Contains("broken.ofd", hostPresentAtMount.Select(h => h.File));

            await cut.InvokeAsync(() => cut.Instance.LoadBytesAsync(OfdBytes(), "good.ofd"));
            cut.WaitForAssertion(() => Assert.True(
                cut.Instance.EffectivePageCount == 2,
                $"records=[{string.Join(" | ", hostPresentAtMount)}] status={cut.Instance.Status} "
                + $"error={cut.Instance.ErrorMessage} host={cut.Markup.Contains("el-viewer-grow__ofd")}"),
                timeout: TestWait.Timeout);

            Assert.Equal(ElViewerStatus.Ready, cut.Instance.Status);
            Assert.Null(cut.Instance.ErrorMessage);
            Assert.Contains("el-viewer-grow__ofd", cut.Markup);

            // 每一次挂载调用都必须发生在「挂载点在文档里」的那一帧；good 这一次尤其要看得见。
            Assert.Contains("good.ofd", hostPresentAtMount.Select(h => h.File));
            Assert.All(hostPresentAtMount, h => Assert.True(h.Present, $"{h.File} 的挂载调用发生在容器不在文档里的那一帧"));
        }

        /// <summary>
        /// #166 的另一半：挂载点不在文档里，跟「这一份 OFD 渲染失败」无关。
        /// 宿主给的 <c>ContentLoader</c> 抓取失败 → <c>FailAsync</c> 把状态打成 Error（<c>ofdFailed</c> 仍是 false），
        /// 而 Error 分支不画挂载点；此时 <c>OnAfterRenderAsync</c> 照样会走挂载决策，
        /// 拿到的 <c>ofdHost</c> 指向上一帧就被移除的元素 —— 浏览器里同样是那条
        /// "elementOfdViewer: container is null"。挂载决策必须先确认这一帧真的画出了挂载点，
        /// 否则本轮不挂、让下一帧再挂（内置通道自己会按地址取流，抓取失败不该挡住它）。
        /// </summary>
        [Fact]
        public async Task Viewer_OfdMount_WaitsForTheHostWhenTheFrameHasNoCanvas()
        {
            JSInterop.Setup<bool>("elementOfdViewerAvailable").SetResult(true);
            JSInterop.SetupVoid("elementOfdViewerDispose", _ => true).SetVoidResult();

            var hostPresentAtMount = new List<bool>();
            IRenderedComponent<ElViewerGrow> cut = null;

            JSInterop.Setup<int>("elementOfdViewerMount", _ =>
            {
                hostPresentAtMount.Add(cut?.Markup.Contains("el-viewer-grow__ofd") == true);
                return true;
            }).SetResult(3);

            cut = Viewer(p => p.Add(x => x.ContentLoader,
                (Func<ElViewerDocument, Task<byte[]>>)(_ => Task.FromException<byte[]>(new IOException("抓取失败")))));

            await cut.InvokeAsync(() => cut.Instance.LoadUrlAsync("https://example.com/remote.ofd"));

            cut.WaitForAssertion(() => Assert.Equal(3, cut.Instance.EffectivePageCount), timeout: TestWait.Timeout);

            Assert.NotEmpty(hostPresentAtMount);
            Assert.All(hostPresentAtMount, present => Assert.True(present, "挂载调用发生在容器不在文档里的那一帧"));
        }

        /// <summary>
        /// #166 的第三条来路（也是「上一帧没画挂载点」这条守卫唯一的触发场景）：
        /// 坏文档挂不上 → 状态打成 Error → 画布分支连挂载点一起撤掉；此时宿主改一次可用宽度
        /// （最大化），重挂闸门只认「同一份文档 + 同一个尺寸」，尺寸一变就放行 —— 放行的那一刻
        /// 状态还是 Error，渲染树里没有 <c>el-viewer-grow__ofd</c>，交给 JS 又是一条
        /// "elementOfdViewer: container is null"。
        /// <para>判据仍是「每次挂载调用发生时挂载点在不在渲染树里」；翻面实测：去掉
        /// <c>EnsureOfdMountedAsync</c> 里那条前置分支，第二次调用记到 False（本用例红）。
        /// 顺带钉住两件事：重试是有界的（一份文档 + 一种尺寸只追一次），且重试仍失败时报给宿主的
        /// 还是那句真原因，不会被「容器为空」顶掉。</para>
        /// </summary>
        [Fact]
        public async Task Viewer_OfdRemountAfterMaximizeToggle_NeverMountsIntoAFrameWithoutTheHost()
        {
            JSInterop.Setup<bool>("elementOfdViewerAvailable").SetResult(true);
            JSInterop.SetupVoid("elementOfdViewerDispose", _ => true).SetVoidResult();
            JSInterop.SetupVoid("elementOfdViewerScrollToPage", _ => true).SetVoidResult();

            var hostPresentAtMount = new List<bool>();
            var errors = new List<string>();
            IRenderedComponent<ElViewerGrow> cut = null;

            JSInterop.Setup<int>("elementOfdViewerMount", _ =>
            {
                hostPresentAtMount.Add(cut?.Markup.Contains("el-viewer-grow__ofd") == true);
                return true;
            }).SetException(new InvalidOperationException("boom"));

            // 文档用命令式灌进来：参数驱动的话首帧渲染期 cut 还没赋值，记账只能记到假的 false。
            cut = Viewer(p => p.Add(x => x.OnError, (ElViewerErrorEventArgs args) => errors.Add(args.Message)));

            await cut.InvokeAsync(() => cut.Instance.LoadBytesAsync(OfdBytes(), "broken.ofd"));

            cut.WaitForAssertion(() => Assert.Equal(ElViewerStatus.Error, cut.Instance.Status), timeout: TestWait.Timeout);
            Assert.Single(hostPresentAtMount);
            Assert.True(hostPresentAtMount[0], "第一次挂载就该发生在有挂载点的那一帧");

            // 前提：错误分支确实不画挂载点（这条不成立，下面的判据就是空转）
            Assert.DoesNotContain("el-viewer-grow__ofd", cut.Markup);

            cut.InvokeAsync(() => cut.Instance.ToggleMaximizeAsync());

            cut.WaitForAssertion(
                () => Assert.Equal(2, hostPresentAtMount.Count),
                timeout: TestWait.Timeout);

            Assert.All(hostPresentAtMount, present => Assert.True(present, "挂载调用发生在容器不在文档里的那一帧"));

            // 有界：一份文档 + 一种尺寸只追加重挂一次，不在 Error↔Ready 之间空转。
            Assert.Equal(2, hostPresentAtMount.Count);

            Assert.Equal(ElViewerStatus.Error, cut.Instance.Status);
            Assert.Equal("boom", cut.Instance.ErrorMessage);
            Assert.All(errors, message => Assert.Equal("boom", message));
        }

        /// <summary>
        /// 批次101：注释角标由 dom.js 通道画进页面，Blazor 没法直接给它挂 <c>@onclick</c>；
        /// 点击冒泡到宿主，组件拿视口坐标问通道「命中了哪条注释」，命中才开卡片，
        /// 内容 / 作者 / 时间三行齐全。关闭按钮与遮罩都能关，卡片自己点不关
        /// （stopPropagation，与 ElDialog 同一取舍）。
        /// <para>翻面实测：把 <c>OnOfdHostClickAsync</c> 里开卡片那句去掉，本用例红。</para>
        /// </summary>
        [Fact]
        public void Viewer_OfdAnnotationClick_OpensTheCardWithRemarkAuthorAndTime()
        {
            var (cut, _) = MountOfd(
                p => p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "seal.ofd")),
                1);

            JSInterop.Setup<string>("elementOfdViewerAnnotationAt", _ => true)
                .SetResult("{\"remark\":\"病历质量审核：病历内容完整、书写规范，予以审核通过。\",\"creator\":\"王芳（质控医师）\",\"time\":\"2026-10-24\",\"type\":\"Path\",\"id\":\"11\"}");

            Assert.DoesNotContain("el-viewer-grow__annot-card", cut.Markup);

            cut.Find(".el-viewer-grow__ofd").Click();

            cut.WaitForAssertion(() => Assert.Contains("el-viewer-grow__annot-card", cut.Markup), timeout: TestWait.Timeout);
            Assert.Contains("病历质量审核：病历内容完整、书写规范，予以审核通过。", cut.Markup);
            Assert.Contains("王芳（质控医师）", cut.Markup);
            Assert.Contains("2026-10-24", cut.Markup);
            Assert.Contains("作者", cut.Markup);
            Assert.Contains("时间", cut.Markup);

            // 卡片自身点击不关：卡片只挂 stopPropagation、没有自己的 handler（与 ElDialog 同一取舍），
            // bUnit 对「没有任何 handler 的元素」直接抛 MissingEventHandlerException（模拟不了这次点击），
            // 所以这里钉接线，行为留给 verify-ofd 探针在真浏览器里点。
            Assert.Contains("stoppropagation", cut.Find(".el-viewer-grow__annot-card").OuterHtml, StringComparison.OrdinalIgnoreCase);

            cut.Find(".el-viewer-grow__annot-close").Click();
            Assert.DoesNotContain("el-viewer-grow__annot-card", cut.Markup);

            // 遮罩点击关。
            cut.Find(".el-viewer-grow__ofd").Click();
            cut.WaitForAssertion(() => Assert.Contains("el-viewer-grow__annot-card", cut.Markup), timeout: TestWait.Timeout);

            cut.Find(".el-viewer-grow__annot").Click();
            Assert.DoesNotContain("el-viewer-grow__annot-card", cut.Markup);
        }

        /// <summary>
        /// 批次101 的另一半：通道回 null（点在页面空白处）时不产生任何卡片渲染，
        /// 否则页面上每次点击都会闪一层遮罩。
        /// </summary>
        [Fact]
        public void Viewer_OfdAnnotationClick_MissLeavesTheCanvasWithoutACard()
        {
            var (cut, _) = MountOfd(
                p => p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "seal.ofd")),
                1);

            JSInterop.Setup<string>("elementOfdViewerAnnotationAt", _ => true).SetResult((string)null);

            cut.Find(".el-viewer-grow__ofd").Click();

            Assert.DoesNotContain("el-viewer-grow__annot", cut.Markup);
        }

        /// <summary>
        /// 批次101：卡片指向的是当前页面的角标，换文档后它要么消失要么重画，
        /// 所以 <c>LoadAsync</c> 必须先把卡片关掉；顺带钉住「作者 / 时间为空时不画空行」。
        /// </summary>
        [Fact]
        public void Viewer_OfdAnnotationCard_ClosesWhenAnotherDocumentLoads()
        {
            var (cut, _) = MountOfd(
                p => p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "seal.ofd")),
                1);

            JSInterop.Setup<string>("elementOfdViewerAnnotationAt", _ => true)
                .SetResult("{\"remark\":\"只有内容一行\",\"creator\":\"\",\"time\":\"\",\"type\":\"Path\",\"id\":\"1\"}");

            cut.Find(".el-viewer-grow__ofd").Click();

            cut.WaitForAssertion(() => Assert.Contains("el-viewer-grow__annot-card", cut.Markup), timeout: TestWait.Timeout);
            // 作者 / 时间为空时不画空行：判据用标签元素本身，不用文案子串（内容里可能撞字）。
            Assert.DoesNotContain("el-viewer-grow__annot-label", cut.Markup);

            cut.InvokeAsync(() => cut.Instance.LoadBytesAsync(OfdBytes(), "second.ofd"));

            cut.WaitForAssertion(() => Assert.DoesNotContain("el-viewer-grow__annot-card", cut.Markup), timeout: TestWait.Timeout);
        }

        [Fact]
        public void Viewer_GoToPage_RoutesToTheOfdChannel()
        {
            var (cut, _) = MountOfd(
                p => p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "report.ofd")),
                4);

            cut.InvokeAsync(() => cut.Instance.GoToPageAsync(3));

            // 通道负责滚动；页码换成 0 起的下标传进去。
            cut.WaitForAssertion(() => Assert.Contains(
                JSInterop.Invocations,
                i => i.Identifier == "elementOfdViewerScrollToPage"
                    && i.Arguments.Count == 2
                    && Convert.ToInt32(i.Arguments[1]) == 2), timeout: TestWait.Timeout);
        }

        [Fact]
        public void Viewer_Close_DisposesTheOfdChannel()
        {
            var (cut, _) = MountOfd(
                p => p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "report.ofd")),
                2);

            cut.InvokeAsync(() => cut.Instance.CloseAsync());

            cut.WaitForAssertion(() => Assert.False(cut.Instance.IsOfdMounted), timeout: TestWait.Timeout);
            Assert.Contains(JSInterop.Invocations, i => i.Identifier == "elementOfdViewerDispose");
        }

        [Fact]
        public void Viewer_OfdMarkup_HasHostContainerAndNoInlineScript()
        {
            var (cut, _) = MountOfd(
                p => p.Add(x => x.Document, ElViewerDocument.FromBytes(OfdBytes(), "report.ofd")),
                2);

            Assert.NotEmpty(cut.FindAll(".el-viewer-grow__ofd"));

            // 渲染核心靠桥接按需下载，标记里永远不出现 <script>（否则会阻塞首屏、也没法懒加载）。
            Assert.DoesNotContain("<script", cut.Markup, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region 守卫：零第三方依赖

        [Fact]
        public void Guard_ComponentSources_DoNotReferenceThirdPartyLibraries()
        {
            var offenders = new List<string>();

            foreach (var file in SourceFiles())
            {
                var code = StripComments(File.ReadAllText(file));

                foreach (var token in new[] { "Radzen", "System.Linq.Dynamic", "Dynamic.Core", "Markdig", "ODataEnumerable" })
                {
                    if (code.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        offenders.Add(Path.GetFileName(file) + " → " + token);
                    }
                }
            }

            Assert.NotEmpty(SourceFiles());
            Assert.Empty(offenders);
        }

        [Fact]
        public void Guard_EngineAndRendererAreDecoupledFromBlazor()
        {
            var files = new[]
            {
                "ElViewerGrowEngine.cs",
                "ElViewerGrowRenderer.cs",
                "ElViewerGrowModels.cs",
                "ElViewerGrowEnums.cs",
                "ElViewerGrowStrings.cs",
                "ElViewerGrowToolbar.cs",
                "ElViewerGrowIcons.cs"
            };

            var offenders = new List<string>();

            foreach (var name in files)
            {
                var code = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", name));

                if (code.Contains("Microsoft.AspNetCore.Components"))
                {
                    offenders.Add(name);
                }
            }

            Assert.Empty(offenders);
        }

        [Fact]
        public void Guard_ComponentProject_HasNoNewPackageDependencies()
        {
            var csproj = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "Element.csproj"));

            foreach (var forbidden in new[] { "Radzen", "Dynamic.Core", "Markdig.Blazor", "PdfPig", "itext" })
            {
                Assert.DoesNotContain(forbidden, csproj, StringComparison.OrdinalIgnoreCase);
            }
        }

        #endregion

        #region 守卫：JS 只允许走内置 OFD 通道

        /// <summary>dom.js 里 OFD 通道那一段（从段头注释到文件末尾）。</summary>
        private static string DomJsOfdSection()
        {
            var path = Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "js", "dom.js");

            Assert.True(File.Exists(path), "dom.js 不存在");

            var text = File.ReadAllText(path);
            var marker = text.IndexOf("ElViewerGrow：OFD 预览通道", StringComparison.Ordinal);

            Assert.True(marker >= 0, "dom.js 中未找到 OFD 预览通道段");

            return text.Substring(marker);
        }

        [Fact]
        public void Guard_ComponentSources_ReferenceOnlyTheOfdViewerChannel()
        {
            // 早期版本这个组件是「零 JS」的。内置 OFD 通道是唯一被允许的例外 ——
            // 它必须只借道 dom.js 的 elementOfdViewer* 通道，不能顺手把别的 SDK 引进来。
            var offenders = new List<string>();

            foreach (var file in SourceFiles())
            {
                var code = StripComments(File.ReadAllText(file));

                foreach (var token in new[] { "IJSObjectReference", "elementViewer", "ofd.umd", "ofd-bridge" })
                {
                    if (code.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        offenders.Add(Path.GetFileName(file) + " → " + token);
                    }
                }

                // 组件源码里出现的 JS 函数名必须全部是 elementOfdViewer*（OFD 通道）
                // 或 elementPrintDocument（打印桥接）。
                foreach (Match match in Regex.Matches(code, @"""(element[A-Za-z]+)"))
                {
                    var name = match.Groups[1].Value;

                    if (!name.StartsWith("elementOfdViewer", StringComparison.Ordinal)
                        && name != "elementPrintDocument")
                    {
                        offenders.Add(Path.GetFileName(file) + " → " + name);
                    }
                }
            }

            Assert.NotEmpty(SourceFiles());
            Assert.Empty(offenders);
        }

        [Fact]
        public void Guard_ComponentSources_HaveNoInlineScript()
        {
            var offenders = new List<string>();

            foreach (var file in SourceFiles())
            {
                var code = File.ReadAllText(file);

                if (code.IndexOf("<script", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    offenders.Add(Path.GetFileName(file));
                }
            }

            Assert.Empty(offenders);
        }

        [Fact]
        public void Guard_NonOfdPathsNeverInvokeJavaScript()
        {
            // 除 OFD 之外的每一条路径都必须保持零 JS：缩放 / 旋转 / 最大化 / 翻页 / 重载都不许下发脚本调用。
            var cut = Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/a.pdf");
                p.Add(x => x.PageCount, 5);
                p.Add(x => x.ShowToolbar, true);
            });

            Button(cut, "放大").Click();
            Button(cut, "缩小").Click();
            Button(cut, "顺时针旋转").Click();
            Button(cut, "最大化").Click();
            Button(cut, "下一页").Click();
            Button(cut, "重新加载").Click();

            var custom = JSInterop.Invocations
                .Where(i => !i.Identifier.StartsWith("Blazor._internal", StringComparison.Ordinal))
                .Select(i => i.Identifier)
                .ToList();

            Assert.Empty(custom);
        }

        [Fact]
        public void Guard_DomJsExposesTheOfdViewerChannelAndNothingElse()
        {
            var section = DomJsOfdSection();

            foreach (var exported in new[]
            {
                "elementOfdViewerAvailable", "elementOfdViewerMount",
                "elementOfdViewerPageCount", "elementOfdViewerScrollToPage", "elementOfdViewerDispose"
            })
            {
                Assert.Contains("window." + exported + " =", section);
            }

            // jit-viewer 的通道名（elementViewer*）绝对不能出现：那是外部 SDK 的挂载点，不是本组件的。
            Assert.DoesNotContain("elementViewer", File.ReadAllText(
                Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "js", "dom.js")),
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Guard_OfdViewerChannel_IsEs5Safe()
        {
            // 通道要跑在目标浏览器（Chrome 75 / Chromium 75 级别）上，不允许 ES6+ 语法。
            // 只查代码：本段注释里用反引号包裹标识号（`scrollHeight-clientHeight`）是说明文字，不是模板字符串。
            var section = StripComments(DomJsOfdSection());

            foreach (var forbidden in new[] { "=>", "?.", "??", "`" })
            {
                Assert.DoesNotContain(forbidden, section);
            }

            Assert.DoesNotMatch(@"\blet\s", section);
            Assert.DoesNotMatch(@"\bconst\s", section);
            Assert.DoesNotMatch(@"\bclass\s", section);
        }

        [Fact]
        public void Guard_OfdCore_IsLazyLoadedAndShipped()
        {
            var section = DomJsOfdSection();

            // 核心必须由通道自己按需插入 <script>，而不是在组件标记里预置。
            Assert.Contains("createElement(\"script\")", section);
            Assert.Contains("ofd.umd.min.js", section);

            var core = Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "js", "ofd", "ofd.umd.min.js");

            Assert.True(File.Exists(core), "内置 OFD 渲染核心未随包携带：" + core);
            Assert.True(new FileInfo(core).Length > 0, "内置 OFD 渲染核心是空文件");
        }

        /// <summary>
        /// 站内这份 ofd.js 0.2.6 不是上游原样：它带着本地补丁 P1–P7，专门吃 GB/T 48666—2026 口径
        /// OFD-H 的非常规写法（<c>Signatures</c> 路径写在 <c>Loc</c> 属性里、签名/注释层用默认命名空间、
        /// <c>PathObject</c> 没有 <c>Boundary</c>、<c>SignedValue</c> 的包内相对路径被
        /// <c>replace("/","")</c> 削掉第一个斜杠），外加批次 100 的注释层三条：
        /// <c>Document.xml</c> 的 <c>&lt;ofd:Annotations&gt;</c> 按包内绝对路径写（<c>/Doc_0/…</c>），
        /// 而 JSZip 的条目名不带前导斜杠 —— 原样查表必然落空，整层注释（质控印章、审核意见）
        /// 被静默丢掉；补丁同时把 <c>Creator</c> / <c>LastModDate</c> / <c>Remark</c> 这些
        /// 「交互信息」留在数据袋里并写到 DOM 的 <c>title</c> 上。上游一旦覆盖回来，
        /// 用户的印章又会当场消失，所以这里既钉字节指纹，也钉每条补丁的锚点。
        /// <para>补丁锚点取自 <c>Temp/b99-vendor-patch.py</c>、<c>Temp/b100-vendor-patch.py</c>
        /// 与 <c>Temp/b101-vendor-patch.py</c>（批次101 的 P8 把三条交互信息另写成结构化
        /// data 属性，供 dom.js 的角标与注释卡片取用），改补丁要同步改这里；
        /// 反向对照见 <c>Temp/b99-vendor-patch-*-ship.log</c>（上游原版每条锚点命中 0 次）。</para>
        /// </summary>
        [Fact]
        public void Guard_OfdCore_CarriesTheLocalPatchAndIsBytePinned()
        {
            var core = Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "js", "ofd", "ofd.umd.min.js");
            var bytes = File.ReadAllBytes(core);

            // 指纹：补丁版 = 928111 字节。上游原版是 926580 字节 / bced7f3fc396769578b22a173d48705d。
            Assert.Equal(928111L, (long)bytes.Length);
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                Assert.Equal("9685428c3f13476a7b69022ed27bd104",
                    BitConverter.ToString(md5.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant());
            }

            var text = Encoding.UTF8.GetString(bytes);

            Assert.Contains("Element-Blazor local patch", text);                    // P0 补丁头
            Assert.Contains(@"e=e[""@_Loc""]||e[""@_BaseLoc""]", text);             // P1 签名清单路径在属性里
            Assert.Contains("signature layer skipped", text);                       // P1 签名层不得否决版面
            Assert.Contains(@"""@""===k.charAt(0)", text);                           // P2 默认命名空间补前缀
            Assert.Contains("a||(a={x:0,y:0,w:0,h:0})", text);                      // P3 无 Boundary 的绝对坐标
            Assert.Contains(@"s.toString().replace(/^\/+/,"""")", text);            // P4 SignedValue 包内路径
            Assert.Contains(@"a&&(a=String(a).replace(/^\/+/,"""")", text);         // P5 注释清单的包内绝对路径
            Assert.Contains(@"remark:i[""ofd:Remark""],creator:i[""@_Creator""]", text); // P6 注释的交互信息留在数据袋
            Assert.Contains(@"a.setAttribute(""data-ofd-annot"",""1"")", text);     // P7 注释层写进 DOM，可断言、可悬停
            Assert.Contains(@"a.setAttribute(""data-ofd-annot-remark"",String(e[""remark""]||""""))", text); // P8 交互信息另写成结构化 data 属性

            foreach (var anchor in new[]
            {
                @"e[""@_Loc""]", "signature layer skipped", "Element-Blazor local patch",
                @"a&&(a=String(a).replace(/^\/+/,"""")", @"remark:i[""ofd:Remark""]",
                @"a.setAttribute(""data-ofd-annot"",""1"")",
                @"a.setAttribute(""data-ofd-annot-remark"",String(e[""remark""]||""""))",
            })
            {
                var hits = Regex.Matches(text, Regex.Escape(anchor)).Count;
                Assert.Equal(1, hits);
            }
        }

        [Fact]
        public void Guard_OfdChannel_DoesNotEagerlyLoadTheCore()
        {
            // 不打开 OFD 就永远不该出现核心脚本的 <script>：从空态到 PDF，全程只能看到 dom.js。
            var cut = Viewer(p => p.Add(x => x.Src, "https://example.com/a.pdf"));

            Assert.DoesNotContain("ofd.umd", cut.Markup, StringComparison.OrdinalIgnoreCase);

            cut.InvokeAsync(() => cut.Instance.LoadUrlAsync("https://example.com/b.pdf"));

            Assert.DoesNotContain("ofd.umd", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(JSInterop.Invocations.Where(i => i.Identifier.StartsWith("elementOfdViewer", StringComparison.Ordinal)));
        }

        #endregion

        #region 守卫：浏览器兼容 CSS

        [Fact]
        public void Guard_CssSection_AvoidsModernOnlySyntax()
        {
            var css = CssSection();

            foreach (var forbidden in new[]
            {
                ":has(", ":is(", ":where(", ":focus-visible", "accent-color",
                "clamp(", "inset-", "margin-inline", "padding-inline", "block-size", "inline-size",
                "gap:", "grid-template", "aspect-ratio", "backdrop-filter", "&. "
            })
            {
                Assert.DoesNotContain(forbidden, css, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void Guard_CssSection_IsFullyScoped()
        {
            var css = CssSection();
            var offenders = new List<string>();

            foreach (var chunk in css.Split('}'))
            {
                var brace = chunk.IndexOf('{');

                if (brace < 0)
                {
                    continue;
                }

                var selector = chunk.Substring(0, brace).Trim();

                if (selector.Length == 0)
                {
                    continue;
                }

                foreach (var part in selector.Split(','))
                {
                    var single = part.Trim();

                    if (single.Length == 0)
                    {
                        continue;
                    }

                    if (single.StartsWith(".el-viewer-grow", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    // @media / @keyframes 本身、以及关键帧里的 from / to / 0% 这类位置选择器没有 class。
                    if (single.StartsWith("@", StringComparison.Ordinal) || single.IndexOf('.') < 0)
                    {
                        continue;
                    }

                    offenders.Add(single);
                }
            }

            Assert.Empty(offenders);
        }

        [Fact]
        public void Guard_CssSection_AvoidsImportantAndGlobalSelectors()
        {
            var css = CssSection();

            // !important 只允许出现在 @media print 块里：打印时画布 outerHTML 会把
            // 缩放态的内联 min-width 等真实属性带进打印文档，内联属性只有 !important
            // 压得住。屏幕样式一律不许用。
            var printIndex = css.IndexOf("@media print", StringComparison.Ordinal);
            var screen = printIndex >= 0 ? css.Substring(0, printIndex) : css;

            Assert.DoesNotContain("!important", screen, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotMatch(new Regex(@"(?<![-\w.])body\s*\{"), css);
            Assert.DoesNotMatch(new Regex(@"(?<![-\w.])html\s*\{"), css);
        }

        [Fact]
        public void Guard_CssSection_UsesElementPlusVariablesWithFallbacks()
        {
            var css = CssSection();

            Assert.Contains("var(--el-", css);
            Assert.DoesNotMatch(new Regex(@"var\(--el-[a-z-]+\)"), css);
        }

        [Fact]
        public void Guard_CssSection_CoversKeyParts()
        {
            var css = CssSection();

            foreach (var part in new[]
            {
                ".el-viewer-grow", ".el-viewer-grow--dark", ".el-viewer-grow__toolbar",
                ".el-viewer-grow__button", ".el-viewer-grow__divider", ".el-viewer-grow__urlbar",
                ".el-viewer-grow__viewport", ".el-viewer-grow__canvas", ".el-viewer-grow__placeholder",
                ".el-viewer-grow__loading", ".el-viewer-grow__status", ".el-viewer-grow__doc-pre",
                ".el-viewer-grow__doc-md", ".el-viewer-grow__doc-table", ".el-viewer-grow__doc-frame",
                ".el-viewer-grow__ofd-annot-marker", ".el-viewer-grow__annot",
                "is-bordered", "is-maximized", "is-disabled", "is-embed", "is-warning"
            })
            {
                Assert.Contains(part, css);
            }
        }

        [Fact]
        public void Print_RootSizeAndTransformGoThroughCssVariables()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Width, "800px");
                p.Add(x => x.Height, "600px");
                p.Add(x => x.Zoom, 2.0);
                p.Add(x => x.Rotation, 90);
                p.Add(x => x.Text, "一行文本");
                p.Add(x => x.FileName, "a.txt");
            });

            var root = cut.Find(".el-viewer-grow");
            var canvas = cut.Find(".el-viewer-grow__canvas");

            // 内联样式一旦写了裸的 width/height/transform，@media print 就盖不住它
            // （仓库禁用 !important），打印会保留固定高度与缩放 —— 所以必须走变量。
            Assert.DoesNotContain("width:", root.GetAttribute("style"));
            Assert.DoesNotContain("height:", root.GetAttribute("style"));
            Assert.Contains("--el-viewer-grow-w:800px", root.GetAttribute("style"));
            Assert.Contains("--el-viewer-grow-h:600px", root.GetAttribute("style"));
            // 只能有一个变量声明，且不以裸 transform 打头（注意别用 Contains("transform:") 判，
            // "--el-viewer-grow-transform:scale(2)" 里也含这个子串）。
            Assert.StartsWith("--el-viewer-grow-transform:", canvas.GetAttribute("style"));
        }

        [Fact]
        public void Print_CssSection_ResetsChromeAndKeepsContentPrintable()
        {
            var css = CssSection();
            var index = css.IndexOf("@media print", StringComparison.Ordinal);

            Assert.True(index >= 0, "样式段里缺少 @media print 块");

            var print = css.Substring(index);

            Assert.Contains("overflow: visible", print);      // 外壳不再裁剪长文档
            Assert.Contains("transform: none", print);        // 缩放/旋转在纸上归零
            Assert.Contains(".el-viewer-grow__doc-frame", print);    // PDF 的 iframe 要有高度
            Assert.Contains(".el-viewer-grow__doc-external", print); // OFD / 注入的 SVG 不滚动
            Assert.Contains(".el-viewer-grow--dark", print);         // 深色主题打印反白
            Assert.Contains(".el-viewer-grow__ofd-annot-marker", print); // 注释角标不上纸（批次101）
            Assert.Contains(".el-viewer-grow__annot", print);            // 注释卡片不上纸（批次101）
                                                              // 打印块里压制内联缩放尺寸的 !important 是合法的：
                                                              // 画布 outerHTML 会把 min-width 等真实内联属性带进打印文档。
            Assert.Contains("min-width: 0 !important", print);
        }

        [Fact]
        public void Print_ToolbarButton_InvokesBridgeWithoutUrlForRenderedContent()
        {
            // 已渲染的内容（文本 / Office / OFD…）不传地址：纸张呈现走组件的 @media print。
            var cut = Viewer(p =>
            {
                p.Add(x => x.Text, "一段文本");
                p.Add(x => x.FileName, "a.txt");
            });

            Assert.True(HasButton(cut, "打印"));

            Button(cut, "打印").Click();

            var invocations = JSInterop.Invocations
                .Where(i => i.Identifier == "elementPrintDocument")
                .ToList();

            Assert.Single(invocations);
            Assert.Null(invocations[0].Arguments[0]);
        }

        [Fact]
        public void Print_EmbedDocument_PassesSourceUrlToBridge()
        {
            // iframe 形态（PDF）必须把源地址交出去：页面级 window.print() 打不出 iframe 里的 PDF。
            var cut = Viewer(p => p.Add(x => x.Src, "https://example.com/a.pdf"));

            Button(cut, "打印").Click();

            var invocations = JSInterop.Invocations
                .Where(i => i.Identifier == "elementPrintDocument")
                .ToList();

            Assert.Single(invocations);
            Assert.Equal("https://example.com/a.pdf", invocations[0].Arguments[0]);
        }

        [Fact]
        public async Task LoadingGate_MinDuration_RaisesLoadingThenReady()
        {
            var events = new List<ElViewerStateChangedEventArgs>();
            var cut = Viewer(p =>
            {
                p.Add(x => x.MinLoadingMilliseconds, 200);
                p.Add(x => x.OnStatusChanged, (Action<ElViewerStateChangedEventArgs>)(e => events.Add(e)));
            });

            await cut.Instance.LoadTextAsync("一行文本", "a.txt");

            // 门控把「加载中」撑住了一帧：宿主能收到 Loading -> Ready 两拍。
            // Loading 一拍由 LoadAsync 入口在任何 await 之前同步发出（批次㉒ #115 契约），
            // 负载下的调度器排队夺不走它；旧实现把它排在门控收尾之后的续体里，实测会整拍丢失。
            Assert.Contains(events, e => e.Status == ElViewerStatus.Loading);
            Assert.Equal(ElViewerStatus.Ready, events[events.Count - 1].Status);

            // 终态拍的「加载前状态」必须是举门前真实状态（Empty），不能被首帧参数驱动的
            // Loading 窗口污染（#115 实测：污染后宿主收到的全是 PreviousStatus=Loading）。
            Assert.NotEqual(ElViewerStatus.Loading, events[events.Count - 1].PreviousStatus);
        }

        [Fact]
        public async Task LoadingGate_DefaultZero_StaysSynchronous()
        {
            var events = new List<ElViewerStateChangedEventArgs>();
            var cut = Viewer(p =>
            {
                p.Add(x => x.OnStatusChanged, (Action<ElViewerStateChangedEventArgs>)(e => events.Add(e)));
            });

            await cut.Instance.LoadTextAsync("一行文本", "a.txt");

            // 默认 0 走同步路径：状态直接算成终态，不插入额外的 Loading 拍。
            Assert.DoesNotContain(events, e => e.Status == ElViewerStatus.Loading);
            Assert.Equal(ElViewerStatus.Ready, events[events.Count - 1].Status);
            Assert.Equal(ElViewerStatus.Ready, cut.Instance.Status);
        }

        [Fact]
        public void Zoom_WithoutRotation_UsesTopLeftOriginSoOverflowStaysScrollable()
        {
            // 放大后滚不到左上内容的旧毛病：原点必须在缩放且未旋转时定到左上角。
            var cut = Viewer(p =>
            {
                p.Add(x => x.Text, "一段文本");
                p.Add(x => x.FileName, "a.txt");
                p.Add(x => x.Zoom, 2.0);
            });

            var style = cut.Find(".el-viewer-grow__canvas").GetAttribute("style");

            Assert.Contains("transform-origin:0 0", style);

            // 旋转场景保持中心原点（左上原点会把内容甩出视野）。
            var rotated = Viewer(p =>
            {
                p.Add(x => x.Text, "一段文本");
                p.Add(x => x.FileName, "a.txt");
                p.Add(x => x.Zoom, 2.0);
                p.Add(x => x.Rotation, 90);
            });

            Assert.DoesNotContain("transform-origin", rotated.Find(".el-viewer-grow__canvas").GetAttribute("style"));
        }

        [Fact]
        public void Guard_EveryRenderedClassHasAStyle()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Src, "https://example.com/a.pdf");
                p.Add(x => x.PageCount, 5);
                p.Add(x => x.Theme, ElViewerTheme.Dark);
            });

            Button(cut, "输入地址").Click();
            cut.Find(".el-viewer-grow__url-input").Change("https://example.com/a.txt");
            cut.Find(".el-viewer-grow__url-submit").Click();

            var css = CssSection();

            // 只认 class 属性里的类名：整段 markup 里还有 style 中的 CSS 变量名
            // （--el-viewer-grow-w 之类），按文本扫会把它们误当成没有样式的类名。
            var used = cut.FindAll("*")
                .SelectMany(e => (e.GetAttribute("class") ?? string.Empty).Split(' '))
                .Select(name => name.Trim())
                .Where(name => name.StartsWith("el-viewer-grow", StringComparison.Ordinal))
                .Distinct()
                .Where(name => name != "el-viewer-grow")
                .ToList();

            var missing = used.Where(name => !css.Contains("." + name)).ToList();

            Assert.Empty(missing);
        }

        #endregion

        #region 内建 Office 预览（OOXML 字节解析）

        private static byte[] ReadResource(string name)
        {
            var assembly = typeof(ElViewerGrowTests).Assembly;
            var full = assembly.GetManifestResourceNames()
                .First(n => n.EndsWith(name, StringComparison.OrdinalIgnoreCase));

            using var stream = assembly.GetManifestResourceStream(full);
            using var memory = new MemoryStream();

            stream.CopyTo(memory);

            return memory.ToArray();
        }

        [Theory]
        [InlineData("office-sample.docx", "el-viewer-grow__doc-word", "出院记录")]
        [InlineData("office-sample.xlsx", "el-viewer-grow__doc-excel", "检验结果")]
        [InlineData("office-sample.pptx", "el-viewer-grow__doc-pptx", "门诊病历汇报")]
        public void OfficePreview_RendersFromBytes(string resource, string cssClass, string expectText)
        {
            var bytes = ReadResource(resource);
            var document = ElViewerDocument.FromBytes(bytes, resource);

            Assert.NotNull(document.RenderedOfficeHtml);
            Assert.Contains(cssClass, document.RenderedOfficeHtml);
            Assert.Contains(expectText, document.RenderedOfficeHtml);
        }

        [Fact]
        public void OfficePreview_CanRenderTrueForBytes()
        {
            var docx = ElViewerDocument.FromBytes(ReadResource("office-sample.docx"), "office-sample.docx");
            var xlsx = ElViewerDocument.FromBytes(ReadResource("office-sample.xlsx"), "office-sample.xlsx");
            var pptx = ElViewerDocument.FromBytes(ReadResource("office-sample.pptx"), "office-sample.pptx");

            Assert.True(ElViewerGrowRenderer.CanRender(docx));
            Assert.True(ElViewerGrowRenderer.CanRender(xlsx));
            Assert.True(ElViewerGrowRenderer.CanRender(pptx));
        }

        [Fact]
        public void OfficePreview_SameDocument_CachesRenderedHtml()
        {
            // RenderedOfficeHtml 必须带缓存：缩放 / 翻页重渲染时不应重复解压整份 ZIP。
            var doc = ElViewerDocument.FromBytes(ReadResource("office-sample.docx"), "office-sample.docx");
            var first = doc.RenderedOfficeHtml;
            var second = doc.RenderedOfficeHtml;

            Assert.NotNull(first);
            Assert.Same(first, second);
        }

        [Fact]
        public void OfficePreview_LegacyBinaryDoc_ShowsConvertHint()
        {
            // 旧版 .doc（OLE 复合文档）：内建 OOXML 解析器读不了，给可执行提示而不是含糊的「不支持」。
            var ole = new byte[512];
            ole[0] = 0xD0;
            ole[1] = 0xCF;
            ole[2] = 0x11;
            ole[3] = 0xE0;

            var cut = Viewer(p =>
            {
                p.Add(x => x.Content, ole);
                p.Add(x => x.FileName, "report.doc");
            });

            Assert.Equal(ElViewerStatus.Unsupported, cut.Instance.Status);
            Assert.Contains("另存为 docx", cut.Find(".el-viewer-grow__status-message").TextContent);
        }

        [Fact]
        public void OfficePreview_BUnitRendersWordMarkup()
        {
            var cut = Viewer(p =>
            {
                p.Add(x => x.Content, ReadResource("office-sample.docx"));
                p.Add(x => x.FileName, "office-sample.docx");
            });

            Assert.Equal(ElViewerStatus.Ready, cut.Instance.Status);
            Assert.Contains("el-viewer-grow__doc-word", cut.Markup);
            Assert.DoesNotContain("暂不支持预览此类型", cut.Markup);
        }

        #endregion
    }
}
