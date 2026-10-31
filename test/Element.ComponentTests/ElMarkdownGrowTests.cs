using Bunit;
using ElementPlus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElMarkdownGrow 组件测试（仅覆盖本组件，不跑全量）。
    /// 覆盖块级语法（标题/代码块/引用/列表/表格/分隔线/HTML 块/段落）、行内语法（强调/代码跨/链接/图片/换行/emoji/转义）、
    /// 安全消毒（危险标签与协议、白名单）、标题锚点、ChildContent 与占位片段，
    /// 以及浏览器兼容性与“零 JS 依赖”两条守卫。
    /// </summary>
    public class ElMarkdownGrowTests : BunitContext
    {
        public ElMarkdownGrowTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        #region 辅助

        private static string Html(string markdown, ElMarkdownGrowOptions options = null)
            => ElMarkdownGrowHtmlRenderer.RenderHtml(markdown, options);

        private static ElMarkdownGrowOptions Options(Action<ElMarkdownGrowOptions> configure = null)
        {
            var options = new ElMarkdownGrowOptions();
            configure?.Invoke(options);

            return options;
        }

        private IRenderedComponent<ElMarkdownGrow> Md(string markdown)
            => Render<ElMarkdownGrow>(p => p.Add(x => x.Text, markdown));

        private static string RepoRoot([CallerFilePath] string file = null)
        {
            // .../test/Element.ComponentTests/ElMarkdownGrowTests.cs → 上溯三层
            var directory = new FileInfo(file).Directory;

            return directory.Parent.Parent.FullName;
        }

        /// <summary>读取 index.css 补丁段（原 fix.css，2026-09-26 并入）中 ElMarkdownGrow 的样式段（已剥离注释）。</summary>
        private static string MarkdownCssSection()
        {
            var path = Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "css", "index.css");
            var text = File.ReadAllText(path);
            var marker = text.IndexOf("ElMarkdownGrow", StringComparison.Ordinal);

            Assert.True(marker >= 0, "index.css 中未找到 ElMarkdownGrow 样式段");

            var start = text.LastIndexOf("/*", marker, StringComparison.Ordinal);
            var section = text.Substring(start);

            return Regex.Replace(section, @"/\*[\s\S]*?\*/", string.Empty);
        }

        #endregion

        #region 空状态与基本结构

        [Fact]
        public void EmptyText_RendersEmptyState()
        {
            var html = Html(string.Empty);

            Assert.Contains("el-markdown-grow__empty", html);
            Assert.Contains("暂无内容", html);
        }

        [Fact]
        public void EmptyText_UsesCustomEmptyText()
        {
            var html = Html("   ", Options(o => o.EmptyText = "nothing here"));

            Assert.Contains("nothing here", html);
        }

        [Fact]
        public void Paragraph_RendersParagraph()
        {
            Assert.Equal("<p>hello world</p>", Html("hello world"));
        }

        [Fact]
        public void Wrapper_AppliesClassStyleAndAttributes()
        {
            var cut = Render<ElMarkdownGrow>(p => p
                .Add(x => x.Text, "hi")
                .Add(x => x.Cls, "my-md")
                .Add(x => x.Style, "color:red")
                .AddUnmatched("data-test", "md1"));

            var markup = cut.Markup;

            Assert.Contains("el-markdown-grow", markup);
            Assert.Contains("my-md", markup);
            Assert.Contains("color:red", markup);
            Assert.Contains("data-test=\"md1\"", markup);
            Assert.Contains("<p>hi</p>", markup);
        }

        #endregion

        #region 块级语法

        [Fact]
        public void AtxHeadings_AllSixLevels()
        {
            var html = Html("# a\n\n## b\n\n### c\n\n#### d\n\n##### e\n\n###### f");

            for (var level = 1; level <= 6; level++)
            {
                Assert.Contains("<h" + level + " class=\"el-markdown-grow__heading el-markdown-grow__h" + level + "\">", html);
                Assert.Contains("</h" + level + ">", html);
            }
        }

        [Fact]
        public void AtxHeading_StripsClosingHashes()
        {
            var html = Html("## 标题 ##");

            Assert.Contains(">标题</h2>", html);
            Assert.DoesNotContain("#", html.Replace("el-markdown-grow__anchor", string.Empty).Replace("href=\"#", string.Empty).Replace("\">#</a>", string.Empty));
        }

        [Fact]
        public void SetextHeadings_LevelOneAndTwo()
        {
            var html = Html("Title\n===\n\nSub\n---");

            Assert.Contains("<h1", html);
            Assert.Contains(">Title</h1>", html);
            Assert.Contains("<h2", html);
            Assert.Contains(">Sub</h2>", html);
        }

        [Fact]
        public void ThematicBreak_RendersHr()
        {
            var html = Html("***");

            Assert.Contains("<hr class=\"el-markdown-grow__hr\" />", html);
        }

        [Fact]
        public void FencedCode_EscapesContentAndAddsLanguageClass()
        {
            var html = Html("```csharp\nvar x = 1 < 2 && 3 > 2;\n```");

            Assert.Contains("<pre class=\"el-markdown-grow__pre\">", html);
            Assert.Contains("class=\"language-csharp\"", html);
            Assert.Contains("1 &lt; 2 &amp;&amp; 3 &gt; 2;", html);
            Assert.DoesNotContain("<var", html);
        }

        [Fact]
        public void FencedCode_WithoutInfo_OmitsLanguageClass()
        {
            var html = Html("```\nplain\n```");

            Assert.Contains("<code>plain</code>", html);
            Assert.DoesNotContain("language-", html);
        }

        [Fact]
        public void FencedCode_TildeFence()
        {
            var html = Html("~~~\ntilde\n~~~");

            Assert.Contains("<code>tilde</code>", html);
        }

        [Fact]
        public void IndentedCodeBlock_RendersPre()
        {
            var html = Html("    indented line");

            Assert.Contains("<pre class=\"el-markdown-grow__pre\">", html);
            Assert.Contains("indented line", html);
        }

        [Fact]
        public void BlockQuote_RendersAndNests()
        {
            var html = Html("> outer\n> > inner");

            Assert.Contains("<blockquote class=\"el-markdown-grow__quote\">", html);
            Assert.Equal(2, Regex.Matches(html, "<blockquote").Count);
            Assert.Contains("outer", html);
            Assert.Contains("inner", html);
        }

        [Fact]
        public void UnorderedList_DashMarker()
        {
            var html = Html("- a\n- b");

            Assert.Contains("<ul class=\"el-markdown-grow__list el-markdown-grow__list--tight\">", html);
            Assert.Equal(2, Regex.Matches(html, "<li class=\"el-markdown-grow__item\">").Count);
        }

        [Fact]
        public void UnorderedList_StarAndPlusMarkers()
        {
            Assert.Contains("<ul", Html("* a\n* b"));
            Assert.Contains("<ul", Html("+ a\n+ b"));
        }

        [Fact]
        public void OrderedList_StartsAtOneWithoutStartAttribute()
        {
            var html = Html("1. a\n2. b");

            Assert.Contains("<ol class=\"el-markdown-grow__list el-markdown-grow__list--tight\">", html);
            Assert.DoesNotContain("start=\"", html);
        }

        [Fact]
        public void OrderedList_CustomStart_RendersStartAttribute()
        {
            var html = Html("3. c\n4. d");

            Assert.Contains("start=\"3\"", html);
        }

        [Fact]
        public void NestedList_RendersNestedList()
        {
            var html = Html("- a\n  - b\n  - c\n- d");

            Assert.Equal(2, Regex.Matches(html, "<ul").Count);
            Assert.Contains(">b</li>", html);
        }

        [Fact]
        public void LooseList_WrapsItemsInParagraph()
        {
            var html = Html("- a\n\n- b");

            Assert.Contains("<p>a</p>", html);
            Assert.Contains("<p>b</p>", html);
        }

        [Fact]
        public void TaskList_RendersDisabledCheckboxes()
        {
            var html = Html("- [x] done\n- [ ] todo");

            Assert.Contains("el-markdown-grow__task-list", html);
            Assert.Contains("type=\"checkbox\"", html);
            Assert.Contains("checked=\"checked\"", html);
            Assert.Contains("disabled=\"disabled\"", html);
            Assert.Contains("done", html);
            Assert.Contains("todo", html);
        }

        [Fact]
        public void Table_RendersHeaderAndAlignment()
        {
            var html = Html("| L | C | R |\n| :-- | :-: | --: |\n| 1 | 2 | 3 |");

            Assert.Contains("<table class=\"el-markdown-grow__table\">", html);
            Assert.Contains("<thead><tr>", html);
            Assert.Contains("style=\"text-align:left\"", html);
            Assert.Contains("style=\"text-align:center\"", html);
            Assert.Contains("style=\"text-align:right\"", html);
            Assert.Equal(3, Regex.Matches(html, "<td").Count);
        }

        [Fact]
        public void Table_SupportsEscapedPipeInCell()
        {
            var html = Html("| a \\| b |\n| --- |\n| c |");

            Assert.Contains("a | b", html);
        }

        [Fact]
        public void SoftBreak_KeptAsNewline_AndOptionTurnsItIntoBr()
        {
            Assert.Contains("line1\nline2", Html("line1\nline2"));
            Assert.Contains("<br />", Html("line1\nline2", Options(o => o.SoftBreakAsHard = true)));
        }

        #endregion

        #region 行内语法

        [Fact]
        public void BoldAndItalic_RenderStrongAndEm()
        {
            Assert.Contains("<strong>x</strong>", Html("**x**"));
            Assert.Contains("<strong>x</strong>", Html("__x__"));
            Assert.Contains("<em>x</em>", Html("*x*"));
            Assert.Contains("<em>x</em>", Html("_x_"));
        }

        [Fact]
        public void TripleMarker_RendersNestedEmphasisAndStrong()
        {
            var html = Html("***x***");

            Assert.Contains("<em><strong>x</strong></em>", html);
        }

        [Fact]
        public void IntrawordUnderscore_IsNotEmphasized()
        {
            var html = Html("a_b_c");

            Assert.Contains("a_b_c", html);
            Assert.DoesNotContain("<em>", html);
        }

        [Fact]
        public void Strikethrough_RendersDel()
        {
            Assert.Contains("<del>x</del>", Html("~~x~~"));

            // 单个波浪线不构成删除线
            Assert.DoesNotContain("<del>", Html("~x~"));
        }

        [Fact]
        public void CodeSpan_RendersCodeAndEscapes()
        {
            var html = Html("`a < b`");

            Assert.Contains("<code class=\"el-markdown-grow__code-inline\">a &lt; b</code>", html);
        }

        [Fact]
        public void CodeSpan_WithBacktickRun()
        {
            var html = Html("``a ` b``");

            Assert.Contains("a ` b", html);
        }

        [Fact]
        public void BackslashEscape_ProducesLiteralCharacters()
        {
            var html = Html("\\*not em\\*");

            Assert.Contains("*not em*", html);
            Assert.DoesNotContain("<em>", html);
        }

        [Fact]
        public void InlineLink_RendersHrefAndTitle()
        {
            var html = Html("[x](https://a.com \"t\")");

            Assert.Contains("<a href=\"https://a.com\" title=\"t\">x</a>", html);
        }

        [Fact]
        public void InlineLink_DangerousProtocol_KeepsTextOnly()
        {
            var html = Html("[x](javascript:alert(1))");

            Assert.Contains("x", html);
            Assert.DoesNotContain("<a", html);
            Assert.DoesNotContain("javascript", html);
        }

        [Fact]
        public void ReferenceLink_ResolvesDefinitionAndHidesIt()
        {
            var html = Html("[x][ref]\n\n[ref]: https://a.com \"T\"");

            Assert.Contains("<a href=\"https://a.com\" title=\"T\">x</a>", html);
            Assert.DoesNotContain("[ref]", html);
        }

        [Fact]
        public void ShortcutReferenceLink_UsesLabelAsText()
        {
            var html = Html("[ref]\n\n[ref]: https://a.com");

            Assert.Contains("<a href=\"https://a.com\">ref</a>", html);
        }

        [Fact]
        public void Image_RendersImgWithAltAndTitle()
        {
            var html = Html("![alt text](https://a.com/i.png \"T\")");

            Assert.Contains("<img class=\"el-markdown-grow__img\"", html);
            Assert.Contains("src=\"https://a.com/i.png\"", html);
            Assert.Contains("alt=\"alt text\"", html);
            Assert.Contains("title=\"T\"", html);
        }

        [Fact]
        public void Image_UnsafeSource_FallsBackToAltText()
        {
            var html = Html("![alt](javascript:alert(1))");

            Assert.Contains("alt", html);
            Assert.DoesNotContain("<img", html);
        }

        [Fact]
        public void Autolink_UrlAndEmail()
        {
            Assert.Contains("<a href=\"https://a.com\">https://a.com</a>", Html("<https://a.com>"));
            Assert.Contains("href=\"mailto:a@b.com\"", Html("<a@b.com>"));
        }

        [Fact]
        public void Autolink_DangerousScheme_IsNotLinked()
        {
            var html = Html("<javascript:alert(1)>");

            Assert.DoesNotContain("<a", html);
        }

        [Fact]
        public void BareUrl_IsAutoLinkedAndTrailingPunctuationTrimmed()
        {
            var html = Html("see https://a.com/x. now");

            Assert.Contains("<a href=\"https://a.com/x\">https://a.com/x</a>", html);
        }

        [Fact]
        public void BareUrl_DisabledByOption()
        {
            var html = Html("see https://a.com/x", Options(o => o.EnableAutoLinkUrl = false));

            Assert.DoesNotContain("<a", html);
            Assert.Contains("https://a.com/x", html);
        }

        [Fact]
        public void HardBreak_TwoSpacesAndBackslash()
        {
            Assert.Contains("<br />", Html("a  \nb"));
            Assert.Contains("<br />", Html("a\\\nb"));
        }

        [Fact]
        public void Emoji_ShortcodeAndOptionAndUnknown()
        {
            Assert.Contains(ElMarkdownGrowEmoji.Map["smile"], Html(":smile:"));
            Assert.Contains(":smile:", Html(":smile:", Options(o => o.EnableEmoji = false)));
            Assert.Contains(":definitely_not_an_emoji:", Html(":definitely_not_an_emoji:"));
        }

        [Fact]
        public void Entity_PreservedAndBareAmpersandEscaped()
        {
            Assert.Contains("&amp;", Html("A &amp; B"));
            Assert.Contains("A &amp; B", Html("A & B"));
            Assert.Contains("&#169;", Html("&#169;"));
        }

        #endregion

        #region 安全消毒

        [Fact]
        public void Html_EscapedWhenAllowHtmlIsFalse()
        {
            var html = Html("<b>x</b>", Options(o => o.AllowHtml = false));

            Assert.Contains("&lt;b&gt;x&lt;/b&gt;", html);
            Assert.DoesNotContain("<b>", html);
        }

        [Fact]
        public void HtmlBlock_SanitizedWhenAllowHtmlIsTrue()
        {
            var html = Html("<div class=\"a\"><span>hi</span></div>");

            Assert.Contains("<div class=\"a\">", html);
            Assert.Contains("<span>hi</span>", html);
        }

        [Fact]
        public void ScriptTag_RemovedTogetherWithItsContent()
        {
            var html = Html("<script>alert(1)</script>");

            Assert.DoesNotContain("alert(1)", html);
            Assert.DoesNotContain("<script", html);
        }

        [Fact]
        public void IframeAndStyleContainer_Removed()
        {
            Assert.DoesNotContain("iframe", Html("<iframe src=\"https://a.com\"></iframe>"));
            Assert.DoesNotContain("color:red", Html("<style>body { color: red }</style>"));
        }

        [Fact]
        public void EventHandlerAttributes_AlwaysRemoved()
        {
            var html = Html("<a href=\"#\" onclick=\"evil()\">t</a>");

            Assert.DoesNotContain("onclick", html);
            Assert.Contains("href=\"#\"", html);
        }

        [Fact]
        public void DangerousHref_StrippedButTagKept()
        {
            var html = Html("<a href=\"javascript:alert(1)\">t</a>");

            Assert.DoesNotContain("javascript", html);
            Assert.Contains("<a>t</a>", html);
        }

        [Fact]
        public void StyleAttribute_ExpressionRemoved_SafeStyleKept()
        {
            Assert.DoesNotContain("style=", Html("<div style=\"width:expression(alert(1))\">x</div>"));
            Assert.Contains("style=\"color:red\"", Html("<div style=\"color:red\">x</div>"));
        }

        [Fact]
        public void HtmlComment_Removed()
        {
            var html = Html("before <!-- hidden --> after");

            Assert.DoesNotContain("hidden", html);
            Assert.Contains("before", html);
            Assert.Contains("after", html);
        }

        [Fact]
        public void AllowedHtmlTagsWhitelist_RestrictsTags()
        {
            var html = Html("<b>x</b><i>y</i>", Options(o => o.AllowedHtmlTags = new[] { "b" }));

            Assert.Contains("<b>x</b>", html);
            Assert.Contains("y", html);
            Assert.DoesNotContain("<i>", html);
        }

        [Fact]
        public void AllowedHtmlAttributesWhitelist_RestrictsAttributes()
        {
            var html = Html("<a href=\"#\" title=\"t\">x</a>", Options(o => o.AllowedHtmlAttributes = new[] { "href" }));

            Assert.Contains("href=\"#\"", html);
            Assert.DoesNotContain("title=", html);
        }

        [Fact]
        public void Sanitizer_IsSafeUrlCoversProtocols()
        {
            Assert.True(ElMarkdownGrowSanitizer.IsSafeUrl("https://a.com"));
            Assert.True(ElMarkdownGrowSanitizer.IsSafeUrl("/relative/path"));
            Assert.True(ElMarkdownGrowSanitizer.IsSafeUrl("#anchor"));
            Assert.True(ElMarkdownGrowSanitizer.IsSafeUrl("mailto:a@b.com"));
            Assert.False(ElMarkdownGrowSanitizer.IsSafeUrl("javascript:alert(1)"));
            Assert.False(ElMarkdownGrowSanitizer.IsSafeUrl("JaVaScRiPt:alert(1)"));
            Assert.False(ElMarkdownGrowSanitizer.IsSafeUrl("java\tscript:alert(1)"));
            Assert.False(ElMarkdownGrowSanitizer.IsSafeUrl("&#106;avascript:alert(1)"));
            Assert.False(ElMarkdownGrowSanitizer.IsSafeUrl("data:text/html;base64,PHNjcmlwdD4="));
            Assert.False(ElMarkdownGrowSanitizer.IsSafeUrl("vbscript:msgbox(1)"));
        }

        #endregion

        #region 标题锚点

        [Fact]
        public void Anchor_DisabledByDefault()
        {
            var html = Html("## Hello");

            Assert.DoesNotContain(" id=", html);
            Assert.DoesNotContain("__anchor", html);
        }

        [Fact]
        public void Anchor_GeneratedUpToConfiguredDepth()
        {
            var html = Html("# A\n\n## B\n\n### C", Options(o => o.AutoLinkHeadingDepth = 2));

            Assert.Contains("id=\"a\"", html);
            Assert.Contains("id=\"b\"", html);
            Assert.DoesNotContain("id=\"c\"", html);
            Assert.Equal(2, Regex.Matches(html, "el-markdown-grow__anchor").Count);
            Assert.Contains("href=\"#a\"", html);
        }

        [Fact]
        public void Anchor_SlugMatchesReferenceAlgorithm()
        {
            Assert.Equal("hello-world", ElMarkdownGrowParser.CreateAnchor("Hello World!"));
            Assert.Equal("中文-标题", ElMarkdownGrowParser.CreateAnchor("中文 标题"));
            Assert.Equal("a-b_c", ElMarkdownGrowParser.CreateAnchor("A B_C"));

            var html = Html("## Hello World!", Options(o => o.AutoLinkHeadingDepth = 6));

            Assert.Contains("id=\"hello-world\"", html);
        }

        [Fact]
        public void Anchor_IgnoresInlineMarkupInText()
        {
            var html = Html("## **Bold** Head", Options(o => o.AutoLinkHeadingDepth = 6));

            Assert.Contains("id=\"bold-head\"", html);
        }

        #endregion

        #region AST 与公开 API

        [Fact]
        public void Document_ExposesParsedAst()
        {
            var cut = Md("# Title\n\nparagraph\n\n- item");

            Assert.Equal(3, cut.Instance.Document.Blocks.Count);
            Assert.IsType<MdHeading>(cut.Instance.Document.Blocks[0]);
            Assert.IsType<MdParagraph>(cut.Instance.Document.Blocks[1]);
            Assert.IsType<MdList>(cut.Instance.Document.Blocks[2]);
        }

        [Fact]
        public void RenderHtml_MatchesPageMarkup()
        {
            var cut = Md("# Title\n\n**bold**");

            Assert.Contains("<h1", cut.Instance.RenderHtml());
            Assert.Contains("<h1", cut.Instance.InnerMarkup);
            Assert.Contains("<strong>bold</strong>", cut.Instance.RenderHtml());
            Assert.Contains("<strong>bold</strong>", cut.Markup);
        }

        [Fact]
        public void Parser_ExposesCodeBlockAndTableDetails()
        {
            var document = ElMarkdownGrowParser.Parse("```json\n{}\n```\n\n| a | b |\n| :-: | --- |\n| 1 | 2 |");

            var code = Assert.IsType<MdCodeBlock>(document.Blocks[0]);
            Assert.True(code.Fenced);
            Assert.Equal("json", code.Info);

            var table = Assert.IsType<MdTable>(document.Blocks[1]);
            Assert.Equal(MdTableAlign.Center, table.Aligns[0]);
            Assert.Equal(MdTableAlign.None, table.Aligns[1]);
            Assert.Equal(2, table.Rows.Count);
            Assert.True(table.Rows[0].IsHeader);
        }

        [Fact]
        public void Parser_InlineApiExposesNodes()
        {
            var inlines = ElMarkdownGrowParser.ParseInlines("a**b**`c`");

            Assert.Equal(3, inlines.Count);
            Assert.IsType<MdText>(inlines[0]);
            Assert.IsType<MdStrong>(inlines[1]);
            Assert.IsType<MdCodeSpan>(inlines[2]);
        }

        [Fact]
        public void Options_CloneCopiesAllSettings()
        {
            var options = Options(o =>
            {
                o.AllowHtml = false;
                o.AutoLinkHeadingDepth = 3;
                o.SoftBreakAsHard = true;
                o.EnableEmoji = false;
                o.EnableAutoLinkUrl = false;
                o.EmptyText = "x";
            });

            var clone = options.Clone();

            Assert.False(clone.AllowHtml);
            Assert.Equal(3, clone.AutoLinkHeadingDepth);
            Assert.True(clone.SoftBreakAsHard);
            Assert.False(clone.EnableEmoji);
            Assert.False(clone.EnableAutoLinkUrl);
            Assert.Equal("x", clone.EmptyText);
        }

        #endregion

        #region ChildContent 与占位片段

        [Fact]
        public void ChildContent_TextIsParsedAsMarkdown()
        {
            var cut = Render<ElMarkdownGrow>(p => p.AddChildContent("# Hi\n\nplain"));

            Assert.Contains("<h1", cut.Markup);
            Assert.Contains(">Hi</h1>", cut.Markup);
            Assert.Contains("<p>plain</p>", cut.Markup);
            Assert.False(cut.Instance.HasOutlets);
        }

        [Fact]
        public void ChildContent_MarkupFrameIsParsedAsMarkdown()
        {
            var cut = Render<ElMarkdownGrow>(p => p.AddChildContent(builder => builder.AddMarkupContent(0, "**bold** text")));

            Assert.Contains("<strong>bold</strong>", cut.Markup);
        }

        [Fact]
        public void ChildContent_TextParameterTakesPrecedence()
        {
            var cut = Render<ElMarkdownGrow>(p => p
                .Add(x => x.Text, "from text")
                .AddChildContent("from child"));

            Assert.Contains("from text", cut.Markup);
            Assert.DoesNotContain("from child", cut.Markup);
        }

        [Fact]
        public void ChildContent_EmbeddedElementBecomesOutletOutsideParagraph()
        {
            var cut = Render<ElMarkdownGrow>(p => p.AddChildContent(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "mark");
                builder.AddContent(2, "X");
                builder.CloseElement();
            }));

            Assert.True(cut.Instance.HasOutlets);
            Assert.Contains("class=\"mark\"", cut.Markup);
            Assert.Contains(">X</span>", cut.Markup);

            // 占位标记不得泄漏到输出
            Assert.DoesNotContain("\uE000", cut.Markup);
            Assert.DoesNotContain("\uE001", cut.Markup);
        }

        [Fact]
        public void ChildContent_ElementBetweenMarkdownBlocks_KeepsOrder()
        {
            var cut = Render<ElMarkdownGrow>(p => p.AddChildContent(builder =>
            {
                builder.AddMarkupContent(0, "# Title\n");
                builder.OpenElement(1, "div");
                builder.AddAttribute(2, "class", "widget");
                builder.AddContent(3, "W");
                builder.CloseElement();
                builder.AddMarkupContent(4, "\npara");
            }));

            var markup = cut.Markup;

            Assert.Contains("<h1", markup);
            Assert.Contains("class=\"widget\"", markup);
            Assert.Contains("<p>para</p>", markup);
            Assert.True(markup.IndexOf("<h1", StringComparison.Ordinal) < markup.IndexOf("widget", StringComparison.Ordinal));
            Assert.True(markup.IndexOf("widget", StringComparison.Ordinal) < markup.IndexOf("<p>para</p>", StringComparison.Ordinal));
        }

        [Fact]
        public void Segments_AreSingleHtmlSegmentForPlainText()
        {
            var cut = Md("# A");

            Assert.Single(cut.Instance.Segments);
            Assert.False(cut.Instance.Segments[0].IsOutlet);
        }

        [Fact]
        public void Segments_SplitAtOutletBoundaries()
        {
            var document = ElMarkdownGrowParser.Parse("a " + ElMarkdownGrowParser.CreateOutletMarker(0) + " b");
            var segments = ElMarkdownGrowHtmlRenderer.RenderSegments(document);

            Assert.Equal(3, segments.Count);
            Assert.False(segments[0].IsOutlet);
            Assert.True(segments[1].IsOutlet);
            Assert.Equal(0, segments[1].OutletId);
            Assert.False(segments[2].IsOutlet);
        }

        [Fact]
        public void OutletMarkerLine_BecomesOutletBlock()
        {
            var document = ElMarkdownGrowParser.Parse(ElMarkdownGrowParser.CreateOutletMarker(7));

            var outlet = Assert.IsType<MdOutletBlock>(Assert.Single(document.Blocks));
            Assert.Equal(7, outlet.Id);
        }

        #endregion

        #region 浏览器兼容性 / 零 JS 守卫

        [Fact]
        public void CssUsesOnlyWidelySupportedFeatures()
        {
            var section = MarkdownCssSection();

            var banned = new[]
            {
                ":has(", ":is(", ":where(", "clamp(", "inset-block", "inset-inline",
                ":focus-visible", "accent-color", "aspect-ratio", "scrollbar-gutter", "text-decoration-thickness"
            };

            foreach (var feature in banned)
            {
                Assert.DoesNotContain(feature, section);
            }

            // gap / 逻辑属性 / 多参数 :not() 需要带边界判断，避免误伤变量名
            Assert.DoesNotMatch(new Regex(@"(?<![-\w])gap\s*:"), section);
            Assert.DoesNotMatch(new Regex(@"(?<![-\w])(margin|padding|border)-(inline|block)\s*:"), section);
            Assert.DoesNotMatch(new Regex(@":not\([^)]*,"), section);
        }

        [Fact]
        public void CssProvidesExpectedClasses()
        {
            var section = MarkdownCssSection();

            foreach (var selector in new[]
            {
                ".el-markdown-grow {", ".el-markdown-grow__heading", ".el-markdown-grow__anchor",
                ".el-markdown-grow__quote", ".el-markdown-grow__pre", ".el-markdown-grow__code-inline",
                ".el-markdown-grow__list", ".el-markdown-grow__item", ".el-markdown-grow__task-checkbox",
                ".el-markdown-grow__table", ".el-markdown-grow__hr", ".el-markdown-grow__img",
                ".el-markdown-grow__empty"
            })
            {
                Assert.Contains(selector, section);
            }
        }

        [Fact]
        public void ComponentUsesNoJavaScriptInterop()
        {
            var root = Path.Combine(RepoRoot(), "src", "Components");
            var files = new[]
            {
                "ElMarkdownGrow.razor",
                "ElMarkdownGrow.razor.cs",
                "ElMarkdownGrowOptions.cs",
                "ElMarkdownGrowAst.cs",
                "ElMarkdownGrowParser.cs",
                "ElMarkdownGrowSanitizer.cs",
                "ElMarkdownGrowHtmlRenderer.cs",
                "ElMarkdownGrowEmoji.cs"
            };

            foreach (var file in files)
            {
                var path = Path.Combine(root, file);

                Assert.True(File.Exists(path), "缺少源文件：" + file);

                var text = File.ReadAllText(path);

                Assert.DoesNotContain("IJSRuntime", text);
                Assert.DoesNotContain("JSRuntime", text);
                Assert.DoesNotContain("InvokeVoidAsync", text);
                Assert.DoesNotContain("InvokeAsync<", text);
                Assert.DoesNotContain("eval(", text);
            }

            Assert.Empty(Directory.GetFiles(Path.Combine(root, "wwwroot", "js"), "ElMarkdownGrow*"));
        }

        [Fact]
        public void ParserAndSanitizerHaveNoExternalDependencies()
        {
            var root = Path.Combine(RepoRoot(), "src", "Components");

            foreach (var file in new[] { "ElMarkdownGrowParser.cs", "ElMarkdownGrowSanitizer.cs", "ElMarkdownGrowHtmlRenderer.cs" })
            {
                var text = File.ReadAllText(Path.Combine(root, file));

                // 只检查“是否真的引用了第三方库”，不检查 XML 注释里对第三方库的说明文字
                Assert.DoesNotContain("using Markdig", text);
                Assert.DoesNotContain("Markdig.Markdown", text);
                Assert.DoesNotContain("using Radzen", text);
                Assert.DoesNotContain("Radzen.Blazor", text);
                Assert.DoesNotContain("System.Web", text);
                Assert.DoesNotContain("HttpUtility", text);
            }

            var csproj = File.ReadAllText(Path.Combine(root, "Element.csproj"));

            Assert.DoesNotContain("Markdig", csproj);
            Assert.DoesNotContain("Radzen", csproj);
        }

        #endregion
    }
}
