using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;
using AngleSharp.Dom;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElAnchor「指示条跟随激活项」口径守卫（批次⑬ #103 / #113）。
    /// <para>
    /// 用户反馈原文：<c>http://localhost:5199/#u-3</c> 「章节A B C都点击了，都变成了激活色（整个页面都这样），
    /// 这样对吗，红色的线离字太近了」。这一批定位出三个各自独立的真身，全部有 EP 2.14.0 源码 +
    /// 同批浏览器探针（<c>Temp/ep2140-anchor.scss</c>、<c>Temp/ep2140-anchor.vue_vue_type_script_setup_true_lang.mjs</c>、
    /// <c>Temp/b13-anchor-marker-after.log</c> 15/15、<c>Temp/b13-anchor-cursor-active.log</c>）为证：
    /// </para>
    /// <list type="number">
    /// <item>marker 的 <c>top/left/width</c> 官方是 JS 按命中链接算出来的行内 style（<c>updateMarkerStyle</c>），
    /// 本库 CSS 只写死一条 <c>top:8px</c> 且 JS 从不写 style —— 线永远停在第一条链接旁边。</item>
    /// <item>点击先激活、随后自己发起的平滑滚动把激活项算回上一条（官方有 <c>isScrolling</c> 闸）。
    /// 容器滚到底时目标被 max 夹住（实测 <c>#c-2</c> 只到 82px，阈值 <c>offset+bound=75</c>）就永远算不回来。</item>
    /// <item>指示条与文字零间距：官方把整块列表缩进 <c>--el-anchor-padding-indent</c>（14px）、
    /// 指示条贴容器左边（<c>left:0</c>），本库以前是 marker <c>left:-12px</c> 且列表不缩进。</item>
    /// </list>
    /// <para>
    /// 顺带两个同日发现，一并钉住：<c>history.replaceState(null, "", href)</c> 在 Blazor
    /// <c>&lt;base href="/"&gt;</c> 下会把 <c>#c-1</c> 解析到站根（实测 URL 从 <code>/anchor</code> 变成
    /// <code>http://127.0.0.1:5199/#c-1</code>，刷新掉回首页），官方从不改地址栏；
    /// <c>dom.js</c> 里 <c>window.elementSetScrollTop</c> 曾被定义两次，后定义的 ElTreeV2 两参版本覆盖了
    /// 带 <c>behavior</c> 的三参版本，导致 ElAnchor / ElBacktop 的 <c>Duration</c>+smooth 静默退化成瞬间跳转。
    /// </para>
    /// <para>
    /// bUnit 没有布局引擎（见 <c>docs/testing-guards.md</c>），所以几何只能靠浏览器探针；本文件守的是
    /// 「公式/口径/选择器契约」这些静态事实，探针红了先在这里查口径有没有被改回去。
    /// </para>
    /// </summary>
    public class AnchorMarkerCaliberGuardTests : BunitContext
    {
        private const string AnchorJsStart = "window.elementAnchorScrollTo = function";

        public AnchorMarkerCaliberGuardTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        /// <summary>锚点段在 dom.js 里的结束标记（下一段是分隔条拖拽）。</summary>
        private const string AnchorJsEnd = "// 分隔条拖拽";

        private static string RepoRoot([CallerFilePath] string file = null)
        {
            // .../test/Element.ComponentTests/AnchorMarkerCaliberGuardTests.cs → 上溯三层
            var directory = new FileInfo(file).Directory;
            return directory.Parent.Parent.FullName;
        }

        private static RenderFragment Links() => builder =>
        {
            builder.OpenComponent<ElAnchorLink>(0);
            builder.AddAttribute(1, "Href", "#a");
            builder.AddAttribute(2, "Title", "A");
            builder.CloseComponent();
        };

        /// <summary>取直接父元素的 class（<c>INode.Parent</c> 上没有 GetAttribute，要先转 IElement）。</summary>
        private static string ParentClass(AngleSharp.Dom.INode node) =>
            (node as AngleSharp.Dom.IElement)?.ParentElement?.GetAttribute("class") ?? string.Empty;

        private static string Css() =>
            File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "css", "index.css"));

        private static string Js() =>
            File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "js", "dom.js"));

        private static string AnchorCode() =>
            File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "ElAnchor.razor.cs"));

        /// <summary>
        /// 取某条 CSS 规则的声明体（选择器后第一个 <c>{...}</c>，去注释）。
        /// 选择器组一行一条，只有行首才算这条规则自己的选择器，避免 <c>.el-anchor__marker</c> 命中
        /// <c>.el-anchor--vertical &gt; .el-anchor__marker</c> 的变体。
        /// </summary>
        private static string CssBlock(string selector)
        {
            var css = Css();
            var index = -1;
            var from = 0;
            while (true)
            {
                index = css.IndexOf(selector, from, StringComparison.Ordinal);
                Assert.True(index >= 0, $"index.css 中未找到行首规则：{selector}");

                var lineStart = css.LastIndexOf('\n', index) + 1;
                if (string.IsNullOrEmpty(css.Substring(lineStart, index - lineStart).Trim()))
                {
                    break;
                }

                from = index + 1;
            }

            var open = css.IndexOf('{', index);
            var close = css.IndexOf('}', open);
            var body = css.Substring(open + 1, close - open - 1);
            return Regex.Replace(body, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        }

        private static int Decl(string block, string property)
        {
            var match = Regex.Match(block, property + @"\s*:\s*(\d+)(?:px)?");
            Assert.True(match.Success, $"规则块里找不到 {property}:Npx —— 实际内容：{block.Trim()}");
            return int.Parse(match.Groups[1].Value);
        }

        /// <summary>dom.js 的锚点段（<c>elementAnchorScrollTo</c> 到分隔条拖拽之前），并去掉注释。</summary>
        private static string AnchorJsCode()
        {
            var js = Js();
            var start = js.IndexOf(AnchorJsStart, StringComparison.Ordinal);
            Assert.True(start >= 0, "dom.js 中未找到 window.elementAnchorScrollTo");
            var end = js.IndexOf(AnchorJsEnd, start, StringComparison.Ordinal);
            Assert.True(end > start, "dom.js 锚点段的结束标记（分隔条拖拽）没找到，段边界需要更新");
            return StripComments(js.Substring(start, end - start));
        }

        /// <summary>
        /// 去掉 <c>/* */</c> 与 <c>//</c> 注释。必须先去注释再断言：本仓的注释里会写被禁止的 API 名
        /// （marker 段就写了「这里以前会 history.replaceState(...)」），不剥会误报。
        /// </summary>
        private static string StripComments(string code)
        {
            code = Regex.Replace(code, @"/\*[\s\S]*?\*/", string.Empty);
            return Regex.Replace(code, @"//[^\n]*", string.Empty);
        }

        /// <summary>整份 dom.js 去掉注释，用于统计同名定义。</summary>
        private static string StrippedJs() => StripComments(Js());

        #region CSS 口径

        [Fact]
        public void MarkerBaseIsHiddenEpStyle()
        {
            var block = CssBlock(".el-anchor__marker");
            Assert.DoesNotContain("!important", block);

            // EP theme-chalk/src/anchor.scss：基态不显示，命中的那一条由 JS 写 style.opacity=1
            Assert.Equal(0, Decl(block, "opacity"));
            Assert.Contains("position: absolute", block);
            Assert.Equal(4, Decl(block, "border-radius"));
            Assert.Contains("--el-anchor-marker-bg-color", block);
        }

        [Fact]
        public void VerticalMarkerIsEpGeometryAndTransitionsTop()
        {
            var block = CssBlock(".el-anchor--vertical > .el-anchor__marker");
            Assert.DoesNotContain("!important", block);

            Assert.Equal(4, Decl(block, "width"));
            Assert.Equal(14, Decl(block, "height"));
            Assert.Equal(8, Decl(block, "top"));
            // left:0 + 列表缩进 14px 是「红色的线离字太近」的官方解法，两条都不许回退
            Assert.Equal(0, Decl(block, "left"));
            Assert.Contains("transition", block);
            Assert.Contains("top", Regex.Match(block, @"transition\s*:\s*([^;]+)").Groups[1].Value);
        }

        [Fact]
        public void HorizontalMarkerIsEpGeometryAndTransitionsLeftWidth()
        {
            var block = CssBlock(".el-anchor--horizontal > .el-anchor__marker");
            Assert.DoesNotContain("!important", block);

            Assert.Equal(2, Decl(block, "height"));
            Assert.Equal(20, Decl(block, "width"));
            Assert.Equal(0, Decl(block, "bottom"));
            var transition = Regex.Match(block, @"transition\s*:\s*([^;]+)").Groups[1].Value;
            Assert.Contains("left", transition);
            Assert.Contains("width", transition);
        }

        [Fact]
        public void VerticalRootListIsIndentedSoMarkerDoesNotTouchText()
        {
            var block = CssBlock(".el-anchor--vertical > .el-anchor__list");
            Assert.DoesNotContain("!important", block);

            // EP：$anchor padding-indent 14px，只作用于根列表；嵌套列表的 16px 是另一份既有口径
            Assert.Contains("--el-anchor-padding-indent", block);
            Assert.Matches(@"padding-left\s*:\s*var\(\s*--el-anchor-padding-indent\s*,\s*14px\s*\)", block);
        }

        [Fact]
        public void CursorExtensionAlsoExpectsJsWrittenTop()
        {
            // cursor（本库扩展，EP 2.10.5 / 2.14.0 的 anchor 只有 default 插槽）以前 CSS 等 JS 写 top、
            // JS 却从不写，所以自定义游标一直停在 top:0。改口径时两条要一起动。
            var block = CssBlock(".el-anchor__cursor");
            Assert.DoesNotContain("!important", block);
            Assert.Contains("top", Regex.Match(block, @"transition\s*:\s*([^;]+)").Groups[1].Value);
            Assert.Equal(0, Decl(block, "left"));
        }

        #endregion

        #region JS 公式与契约

        [Fact]
        public void AnchorScrollToNeverRewritesTheUrl()
        {
            var section = AnchorJsCode();

            // 官方 anchor.vue 只在 onMounted 读一次 location.hash，从不写地址栏
            Assert.DoesNotContain("replaceState", section);
            Assert.DoesNotContain("pushState", section);
        }

        [Fact]
        public void AnchorScrollToHonorsDurationWithBehaviorArgument()
        {
            var section = AnchorJsCode();

            // 三参调用（container, top, behavior）：以前被同名覆盖成两参实现，Duration/smooth 静默失效
            var call = Regex.Match(section, @"elementSetScrollTop\(\s*container\s*,\s*nextTop\s*,\s*([^)]+)\)");
            Assert.True(call.Success, $"elementAnchorScrollTo 没有把 behavior 传给 elementSetScrollTop：{section}");
            Assert.Contains("smooth", call.Groups[1].Value);
            Assert.Contains("auto", call.Groups[1].Value);
        }

        [Fact]
        public void ClickScrollOpensTheEpIsScrollingGate()
        {
            var section = AnchorJsCode();

            Assert.Contains("elementAnchorBeginSuppressScroll(container, dur)", section);

            // 官方 isScrolling 的等价闸：程序滚动期间的 scroll 事件不参与当前锚点计算
            var begin = section.IndexOf("window.elementAnchorBeginSuppressScroll", StringComparison.Ordinal);
            Assert.True(begin >= 0, "dom.js 中未找到 elementAnchorBeginSuppressScroll");
            var gate = section.IndexOf("window.elementAnchorScrollSuppressed", StringComparison.Ordinal);
            Assert.True(gate >= 0, "dom.js 中未找到 elementAnchorScrollSuppressed");

            var release = section.Substring(begin, gate - begin);
            // scrollend 精确解除 + 定时器兜底：没滚起来时不能永久挡住计算
            Assert.Contains("scrollend", release);
            Assert.Contains("setTimeout(release", release);
        }

        [Fact]
        public void ScrollWatchRejectsActiveHrefWhileProgrammaticScrollRuns()
        {
            var init = AnchorInitSection();

            // 闸必须在链接几何循环之前，否则回算已经发生过了
            var gate = init.IndexOf("elementAnchorScrollSuppressed(container)", StringComparison.Ordinal);
            var loop = init.IndexOf("links.forEach", StringComparison.Ordinal);
            Assert.True(gate >= 0, "elementAnchorInit.update 没有抑制闸");
            Assert.True(loop >= 0, "elementAnchorInit.update 找不到链接循环");
            Assert.True(gate < loop, "抑制闸必须在遍历链接之前 return");

            // 滚动驱动的激活也要同步指示条（官方 watch 里 setCurrentAnchor + updateMarkerStyle）
            Assert.Contains("elementAnchorUpdateMarker(root, activeHref)", init);
        }

        [Fact]
        public void MarkerUpdateUsesEpFormulaForBothDirections()
        {
            var marker = AnchorJsCode();
            var start = marker.IndexOf("window.elementAnchorUpdateMarker", StringComparison.Ordinal);
            Assert.True(start >= 0, "dom.js 中未找到 window.elementAnchorUpdateMarker");
            var body = marker.Substring(start);

            // 官方在 nextTick 里量几何；这里用 rAF，等 is-active 刷完（underline 的 border-bottom 会让链接高 2px）
            Assert.Contains("requestAnimationFrame", body);

            // 直接子选择器：marker 与 cursor 都是根节点的直接子（markup 契约见 MarkerIsDirectChildOfRoot）
            Assert.Contains(":scope > .el-anchor__marker", body);
            Assert.Contains(":scope > .el-anchor__cursor", body);

            // EP 竖向 top = linkTop - anchorTop + (linkHeight - markerHeight)/2；横向 left + width
            Assert.Contains("marker.style.top", body);
            Assert.Contains("marker.style.left", body);
            Assert.Contains("marker.style.width", body);
            Assert.Contains("marker.style.opacity", body);
            Assert.Contains("el-anchor--horizontal", body);

            // 没命中时回到基态（官方 markerStyle = {}），否则线会留在上一条
            Assert.Contains("marker.style.opacity = \"\"", body);

            // href 必须按属性值比对，不能拼进选择器（href 里有 # 和任意字符）
            Assert.Contains("getAttribute(\"data-anchor-href\") === href", body);
        }

        [Fact]
        public void SetScrollTopIsDefinedExactlyOnce()
        {
            var js = StrippedJs();

            var canonical = Regex.Matches(js, @"window\.elementSetScrollTop\s*=").Count;
            Assert.Equal(1, canonical);

            // ElTreeV2 的两参版本已改名：同名覆盖会让上面那个带 behavior 的实现失效
            var renamed = Regex.Matches(js, @"window\.elementSetElementScrollTop\s*=").Count;
            Assert.Equal(1, renamed);
            Assert.Contains("window.elementSetScrollTop = function (container, top, behavior)", js);
        }

        [Fact]
        public void TreeV2CallsTheRenamedHelper()
        {
            var tree = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "ElTreeV2.razor.cs"));
            Assert.Contains("elementSetElementScrollTop", tree);
            Assert.DoesNotContain("\"elementSetScrollTop\"", tree);
        }

        #endregion

        #region .NET 双侧驱动

        [Fact]
        public void BothPublicPathsActivateAndMoveTheMarker()
        {
            var code = AnchorCode();

            // EP：scrollTo(href) = setCurrentAnchor(href) + 滚动；点击同理。两条路都要刷指示条
            var scrollTo = code.Substring(code.IndexOf("public async Task ScrollToAsync", StringComparison.Ordinal));
            scrollTo = scrollTo.Substring(0, scrollTo.IndexOf("protected override void OnParametersSet", StringComparison.Ordinal));
            Assert.Contains("SetActiveHrefAsync(href, true)", scrollTo);
            Assert.Contains("elementAnchorUpdateMarker", scrollTo);

            var click = code.Substring(code.IndexOf("internal async Task HandleLinkClickAsync", StringComparison.Ordinal));
            click = click.Substring(0, click.IndexOf("[JSInvokable]", StringComparison.Ordinal));
            Assert.Contains("SetActiveHrefAsync(link.Href, true)", click);
            Assert.Contains("elementAnchorUpdateMarker", click);

            Assert.Equal(2, Regex.Matches(code, "elementAnchorUpdateMarker").Count);
        }

        #endregion

        #region DOM 结构契约

        [Fact]
        public void MarkerIsDirectChildOfRoot()
        {
            // JS 用 :scope > .el-anchor__marker 取节点，marker 一旦被挪进 .el-anchor__list 就取不到（表现为线不动）
            var cut = Render<ElAnchor>(p => p.AddChildContent(Links()));
            var marker = cut.Find(".el-anchor__marker");
            Assert.Contains("el-anchor", ParentClass(marker));
            // 不能落在 .el-anchor__list 里（:scope > 取不到；注意 el-anchor__list 本身含 "el-anchor" 子串，弱断言会漏）
            Assert.DoesNotContain("el-anchor__list", ParentClass(marker));
            Assert.Equal(1, cut.FindAll(".el-anchor__marker").Count);
        }

        [Fact]
        public void CursorTemplateIsAlsoDirectChildOfRoot()
        {
            var cut = Render<ElAnchor>(p => p
                .Add(x => x.CursorTemplate, (RenderFragment)(builder =>
                {
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "class", "el-anchor__cursor");
                    builder.CloseElement();
                }))
                .AddChildContent(Links()));

            var cursor = cut.Find(".el-anchor__cursor");
            Assert.Contains("el-anchor", ParentClass(cursor));
            // cursor 插槽替换默认 marker：两者不能同时存在，否则 JS 的 || 取到的是错的那个
            Assert.Equal(0, cut.FindAll(".el-anchor__marker").Count);
        }

        [Fact]
        public void LinksExposeTheirHrefAsDataAttribute()
        {
            var cut = Render<ElAnchor>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElAnchorLink>(0);
                builder.AddAttribute(1, "Href", "#sec-b");
                builder.AddAttribute(2, "Title", "章节B");
                builder.CloseComponent();
            }));

            // marker 按 data-anchor-href 找链接；没有这个属性就永远算不出 top
            var link = cut.Find("[data-anchor-href]");
            Assert.Equal("#sec-b", link.GetAttribute("data-anchor-href"));
        }

        #endregion

        private static string AnchorInitSection()
        {
            var section = AnchorJsCode();
            var start = section.IndexOf("window.elementAnchorInit", StringComparison.Ordinal);
            Assert.True(start >= 0, "dom.js 中未找到 window.elementAnchorInit");
            var end = section.IndexOf("window.elementAnchorDispose", start, StringComparison.Ordinal);
            Assert.True(end > start, "dom.js 中未找到 window.elementAnchorDispose");
            return section.Substring(start, end - start);
        }
    }
}
