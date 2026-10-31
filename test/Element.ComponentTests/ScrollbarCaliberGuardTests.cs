using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 全站细滚动条口径守卫（<c>src/Components/wwwroot/css/index.css</c>）。
    /// <para>
    /// 口径（2026-10-10 批次⑩ 加宽横轴、同日批次⑫ 定为 6px）：<b>纵向 4px / 横向 6px</b>。
    /// 横轴单独加宽是用户的实测诉求（「横向的滚动条稍微宽一点，要不不容易拖动」），随后用户直接指定
    /// 「横向滚动 改为 6px」—— 6px 恰好就是 EP <c>theme-chalk/src/scrollbar.scss</c> 里
    /// <c>.el-scrollbar__bar.is-horizontal{height:6px}</c> 的字面值（批次⑩ 的 8px 是把 <c>bottom:2px</c>
    /// 内缩也算进了可压区域）。Chromium 里 <c>::-webkit-scrollbar</c> 的 <c>width</c> 只管纵轴、
    /// <c>height</c> 只管横轴，所以只有 <c>height</c> 能变；纵轴维持 4px 是批次⑧的既有依赖
    /// （grow 面板表头/表体槽宽相等，改纵轴会把列拽歪）。
    /// </para>
    /// <para>
    /// 悬停档（2026-10-21 批次94 #154，用户的字面诉求）：横向滚条 <b>静止 6px → 鼠标进容器 12px（正好一倍）
    /// → 移出收回</b>，只动 <c>height</c> 这一轴，纵向 4px 一动不动；拖曳期间指针本来就在容器内，
    /// <c>:hover</c> 一直成立，所以条不会中途缩回。刻意归零的宿主用 <c>--el-sb-h-hover</c> 钩子退出。
    /// </para>
    /// <para>
    /// 批次96（2026-10-21 #155）：同一天用户对「要不要给 ElDropDownTableGrow 面板重新装回一根可拖横条」
    /// 裁定<b>「需要」</b> ⇒ 组件 &lt;style&gt; 里那条 0×0 换成库口径（横轴静止 6 / 悬停 12），
    /// <b>只反横轴</b>；批次⑧ 的「纵轴槽宽 0」仍是表头/表体列对齐的前提，一动不动。
    /// </para>
    /// <para>
    /// 另外钉住批次⑧按用户批准归零的滚道（<c>.el-pro-tabs-grow__scroll</c>、时间面板）不许被全局规则顶回来，
    /// 以及 <c>Element.Markdown</c> 的 vendor 滚条（<c>editormd.min.css</c> 的
    /// <c>.editormd ::-webkit-scrollbar</c>，特异性高于全局 <c>*</c> 规则）必须被 <c>editor.css</c> 复位。
    /// </para>
    /// </summary>
    public class ScrollbarCaliberGuardTests
    {
        private static string RepoRoot([CallerFilePath] string file = null)
        {
            // .../test/Element.ComponentTests/ScrollbarCaliberGuardTests.cs → 上溯三层
            var directory = new FileInfo(file).Directory;
            return directory.Parent.Parent.FullName;
        }

        private static string Css() =>
            File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "css", "index.css"));

        private static string MarkdownCss() =>
            File.ReadAllText(Path.Combine(RepoRoot(), "src", "Markdown", "wwwroot", "css", "editor.css"));

        private static string MarkdownJs() =>
            File.ReadAllText(Path.Combine(RepoRoot(), "src", "Markdown", "wwwroot", "js", "dom.js"));

        /// <summary>下拉表格选择器组件自带的 &lt;style&gt;：批次96 起，面板内滚条的口径写在这里而不是 index.css
        /// ——组件条的目的就是压住全局那条（特异性 (0,2,0) 且样式表更晚），所以它的静止/悬停档必须同处维护。</summary>
        private static string GrowRazor() =>
            File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "ElDropDownTableGrow.razor"));

        /// <summary>整份 CSS 去注释：注释里会原样引用选择器和声明字面量（本段的 --el-sb-h-hover: 0px 就是），
        /// 不剥掉的话「找第一条规则」会命中注释，量到隔壁规则的声明体。</summary>
        private static string StripComments(string css) =>
            Regex.Replace(css, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

        /// <summary>取某条规则的声明体：选择器（可为选择器组里的一条）后第一个 <c>{...}</c>，并去掉注释。</summary>
        private static string BlockOf(string css, string selector, bool anchorAtLineStart = false)
        {
            var index = -1;
            var from = 0;
            while (true)
            {
                index = css.IndexOf(selector, from, System.StringComparison.Ordinal);
                Assert.True(index >= 0, $"index.css 中未找到规则：{selector}");

                // 选择器组一行一条，行首才算「这条规则自己的选择器」，避免命中 *-thumb / *-track 的变体
                var lineStart = css.LastIndexOf('\n', index) + 1;
                var atLineStart = string.IsNullOrEmpty(css.Substring(lineStart, index - lineStart).Trim());
                if (!anchorAtLineStart || atLineStart)
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
            // 口径值允许两种写法：字面 Npx，或走钩子 var(--el-sb-*, Npx)（批次94 #154 起，
            // 宿主靠改钩子退出某一档，不必再打特异性战争）。钩子回退值就是这条规则的口径。
            var match = Regex.Match(block,
                property + @"\s*:\s*(?:var\(\s*(--[\w-]+)\s*,\s*)?(\d+)(?:px)?\s*\)?");
            Assert.True(match.Success, $"规则块里找不到 {property}:Npx（或 {property}:var(--钩子, Npx)）");
            return int.Parse(match.Groups[2].Value);
        }

        /// <summary>这条规则里到底有没有声明这个属性（不管值）——用来钉「另一轴一动不动」。</summary>
        private static bool HasDecl(string block, string property) =>
            Regex.IsMatch(block, @"(^|[;{\s])" + property + @"\s*:");

        /// <summary>这条声明挂在哪个钩子上（没走 var() 就返回 null）。用来钉「口径同源」：
        /// 组件的静止/悬停档必须读 index.css 那两个变量，而不是各写一份字面数字。</summary>
        private static string HookOf(string block, string property)
        {
            var match = Regex.Match(block, property + @"\s*:\s*var\(\s*(--[\w-]+)\s*,");
            return match.Success ? match.Groups[1].Value : null;
        }

        [Fact]
        public static void Guard_GlobalScrollbarIsThinVertical_WideHorizontal()
        {
            var block = BlockOf(Css(), "*::-webkit-scrollbar", true);

            // 正向：两轴各自的口径（批次⑫：横轴 8px → 6px）
            Assert.Equal(4, Decl(block, "width"));
            Assert.Equal(6, Decl(block, "height"));
        }

        [Fact]
        public static void Guard_ThinScrollbarAndTreeV2Body_UseTheSameCaliber()
        {
            var block = BlockOf(Css(), ".el-tree-v2__body::-webkit-scrollbar", true);

            Assert.Equal(4, Decl(block, "width"));
            Assert.Equal(6, Decl(block, "height"));
        }

        [Fact]
        public static void Guard_HorizontalBarDoublesOnHover_WithoutTouchingTheVerticalAxis()
        {
            var css = StripComments(Css());

            // 选择器带尾空格才算「这条规则自己」：同段里还有 *:hover::-webkit-scrollbar-thumb 这种
            // 前缀完全相同的邻居，不带 { 会先命中它、量到另一条规则的声明体。
            var rest = BlockOf(css, "*::-webkit-scrollbar {", true);
            var hover = BlockOf(css, "*:hover::-webkit-scrollbar {", true);

            var restH = Decl(rest, "height");
            var hoverH = Decl(hover, "height");
            Assert.Equal(6, restH);
            // 用户的字面口径是「高度变大1倍」⇒ 2× 关系当场算出来，不是两个数各看各的
            Assert.Equal(2 * restH, hoverH);
            Assert.Equal(12, hoverH);
            Assert.Contains("--el-sb-h-hover", hover);

            // 纵轴（width 那一轴）不许出现在悬停档：4px 是 grow 面板表头/表体槽宽相等的依赖（批次⑧），
            // 把 width 写进 hover 规则＝鼠标一进就把列挤歪。探针 ⑥ 用横竖都溢出的裸 div 盯同一件事。
            Assert.False(HasDecl(hover, "width"),
                "*:hover::-webkit-scrollbar 里出现了 width 声明：悬停会把纵向 4px 那一轴一起改掉（列对齐依赖它）");

            // EP 自家 .el-thin-scrollbar / 虚拟列表 body 这段特异性 (0,1,0) 与全局那条打平且位置更靠后
            // ⇒ 全局的悬停加高够不着它们，必须在同一段再补一次；漏了就是这两处永远不放大。
            var thinRest = BlockOf(css, ".el-tree-v2__body::-webkit-scrollbar {", true);
            Assert.Equal(4, Decl(thinRest, "width"));
            Assert.Equal(6, Decl(thinRest, "height"));
            var thinHover = BlockOf(css, ".el-tree-v2__body:hover::-webkit-scrollbar {", true);
            Assert.Equal(hoverH, Decl(thinHover, "height"));
            Assert.False(HasDecl(thinHover, "width"),
                ".el-thin-scrollbar/.el-tree-v2__body 的悬停档写了 width：同批次94 #154 口径只许动 height 一轴");

            // 倒咬一口：滚道被用户批准归零的宿主（批次⑧ 的 pro-tabs、EP noShowScrollbar 的 wrap）
            // 必须带退出钩子，否则悬停把 0×0 复活成一条 12px 粗横条。
            var zero = css.IndexOf("--el-sb-h-hover: 0px", System.StringComparison.Ordinal);
            Assert.True(zero >= 0, "退出钩子 --el-sb-h-hover: 0px 整条不见了：悬停加高会复活刻意归零的滚道");
            var open = css.LastIndexOf('{', zero);
            var selStart = css.LastIndexOf('}', open) + 1;
            var selectors = css.Substring(selStart, open - selStart);
            Assert.Contains(".el-scrollbar__wrap--hidden-default", selectors);
            Assert.Contains(".el-pro-tabs-grow__scroll", selectors);
        }

        [Fact]
        public static void Guard_ApprovedZeroedTracks_AreNotWidenedBack()
        {
            // 这两处是用户点名归零的面板，全局横轴口径（6px）不许把它们顶回来
            var tabs = BlockOf(Css(), ".el-pro-tabs-grow__scroll::-webkit-scrollbar", true);
            Assert.Equal(0, Decl(tabs, "width"));
            Assert.Equal(0, Decl(tabs, "height"));

            var time = BlockOf(Css(), ".el-date-time-picker__time-panel .el-time-spinner__wrapper::-webkit-scrollbar", true);
            Assert.Equal(0, Decl(time, "width"));
            Assert.Equal(0, Decl(time, "height"));
        }

        /// <summary>
        /// 批次96 · #155：ElDropDownTableGrow 下拉面板的横条按用户裁定（同日问「要不要给它重新装回一根
        /// 6→12 的可拖横条」→「需要」）装回库口径，<b>只反横轴</b>；批次⑧ 批准的「纵轴槽宽 0」
        /// 是表头/表体列对齐的前提，仍然是 0，谁把它改回 4px 这条就红。
        /// </summary>
        [Fact]
        public static void Guard_GrowDropDownPanel_HorizontalBarUsesLibraryCaliber_VerticalSlotStaysZero()
        {
            var razor = StripComments(GrowRazor());
            var body = BlockOf(razor, ".el-dropdown-table-grow__panel .el-table__body-wrapper::-webkit-scrollbar {", true);
            var head = BlockOf(razor, ".el-dropdown-table-grow__panel .el-table__header-wrapper::-webkit-scrollbar {", true);
            var bodyHover = BlockOf(razor, ".el-dropdown-table-grow__panel .el-table__body-wrapper:hover::-webkit-scrollbar {", true);

            // 纵轴（::-webkit-scrollbar 的 width 只管纵向那条）必须还是 0：批次⑧ 的 headSlot/bodySlot
            // 同为 0 才让 clientW 相等、逐列边界差全 0（Temp/diag-batch8-scrollbar-zero2.log B 段，
            // 常驻尺子 verify-batch8.cjs G1/G2/G3）。
            Assert.Equal(0, Decl(body, "width"));
            Assert.Equal(0, Decl(head, "width"));

            // 横轴（height 只管横向那条）与 index.css 全局那条同源：不是各写一份字面数字，
            // 而是读同两个钩子 —— 全站改口径时这里自动跟上。
            var css = StripComments(Css());
            var globalRest = Decl(BlockOf(css, "*::-webkit-scrollbar {", true), "height");
            var globalHover = Decl(BlockOf(css, "*:hover::-webkit-scrollbar {", true), "height");

            var restH = Decl(body, "height");
            var hoverH = Decl(bodyHover, "height");
            Assert.Equal(globalRest, restH);
            Assert.Equal(globalHover, hoverH);
            Assert.Equal(2 * restH, hoverH);
            Assert.Equal("--el-sb-h", HookOf(body, "height"));
            Assert.Equal("--el-sb-h-hover", HookOf(bodyHover, "height"));

            // 悬停档必须写在组件里：组件静止条的特异性 (0,2,0) + 更晚的样式表压得住全局那条 (0,1,0)
            // 的 *:hover::-webkit-scrollbar —— 少了这条伴生规则，面板横条会永远停在静止档（批次94 ⑬ 量到
            // 的正是这个形状）。它同时也不许碰 width：那是上面钉住的列对齐依赖。
            Assert.False(HasDecl(bodyHover, "width"),
                "面板的悬停加高档写了 width：会把纵轴槽宽一起改掉，表头/表体列边界随之位移");

            // 表头容器不画横条：EP 给 .el-table__header-wrapper 的是 overflow:hidden，横条拖不动，
            // 6px 只会从表头底部白吃一条高度、并让表头/表体的可视高度不相等。
            Assert.Equal(0, Decl(head, "height"));
        }

        [Fact]
        public static void Guard_MarkdownEditorScrollbarFollowsLibraryCaliber()
        {
            var css = MarkdownCss();
            var js = MarkdownJs();

            // vendor 的 editormd.min.css 里是 `.editormd ::-webkit-scrollbar{width:7px;height:10px;
            // background:rgba(0,0,0,.1)}` —— 选择器带 class，特异性 (0,1,x) 高于全局 `*::-webkit-scrollbar`
            // (0,0,x)，会把全站口径整条顶掉（headful 实测 .CodeMirror-scroll 槽宽 6px、.editormd-preview 6px
            // 且静止常显）。editor.css 在 index.html 里排在它之后，用同选择器复位才赢得了解析顺序。
            var bar = BlockOf(css, ".editormd ::-webkit-scrollbar", true);
            Assert.Equal(4, Decl(bar, "width"));
            Assert.Equal(6, Decl(bar, "height"));
            Assert.Contains("background: transparent", bar);

            var thumb = BlockOf(css, ".editormd ::-webkit-scrollbar-thumb", true);
            Assert.Contains("background: transparent", thumb);

            // 静止透明还不够 —— 必须有一条 hover 让它显形，否则是"彻底没有滚动条"，
            // 用户要的是「鼠标移动上去才显示、若隐若现」。
            Assert.Contains(":hover::-webkit-scrollbar-thumb", css);

            // CodeMirror 的编辑栏**不显示** `.CodeMirror-scroll` 的原生滚条：headful 实测该元素右缘 x=850，
            // 而外层 `.CodeMirror{overflow:hidden}` 只到 x=801，那 4px 被裁掉了；用户看得见、拖得动的
            // 是 CM 的替身 `.CodeMirror-vscrollbar`（x 795..800）。替身不是被悬停的那个滚动容器本身，
            // 所以按栏点亮这一级在 2026-10-18（批次83 #100）改成 class 驱动，且**只能挂滚动元素自身**：
            // ① 祖先 :hover 那档在 Edge/Chromium 155 的真实鼠标路径上不重绘（合成事件才重绘）——
            //    只写 :hover 就是「探针绿、真实用户看不见」（Temp/b53-md-hover-diag7.log N1~N6）；
            // ② 祖先形状的滚条伪元素选择器根本不匹配（getComputedStyle 读回 rgba(0,0,0,0)，
            //    Temp/b54-md-repaint-diag-1.log A2、b54-md-native-cursor-3.log EVIDENCE 档）。
            Assert.Contains(".CodeMirror-vscrollbar.md-bar-hover::-webkit-scrollbar-thumb", css);
            Assert.Contains(".CodeMirror-hscrollbar.md-bar-hover::-webkit-scrollbar-thumb", css);
            Assert.Contains(".editormd-preview.md-bar-hover::-webkit-scrollbar-thumb", css);
            // 两条已证死的形状不许回来（回来了就是重新造一个只有探针能看见的显形）
            Assert.DoesNotContain(".CodeMirror:hover .CodeMirror-vscrollbar", css);
            Assert.DoesNotContain(".CodeMirror.md-bar-hover .CodeMirror-vscrollbar", css);

            // 规则在 CSS 里只是一半 —— class 由 dom.js 的 mouseover/mouseleave 挂撤，挂错元素等于没有。
            Assert.Contains("querySelectorAll(\".CodeMirror-vscrollbar, .CodeMirror-hscrollbar\")", js);
            Assert.Contains("closest(\".editormd-preview\")", js);
            Assert.DoesNotContain(".CodeMirror\").classList.add(\"md-bar-hover\")", js);
        }

        [Fact]
        public static void Guard_GlobalSectionStillResetsScrollbarWidthAndColor()
        {
            var css = Css();
            var section = css.Substring(css.IndexOf("全局细滚动条", System.StringComparison.Ordinal));

            // Chromium ≥121：元素上生效的 scrollbar-width / scrollbar-color 不是 initial（含继承）时，
            // ::-webkit-scrollbar 伪元素整体被忽略、回落平台 15px 轨道 —— 宽度就「改不动」。
            Assert.Contains("scrollbar-width: auto;", section);
            Assert.Contains("scrollbar-color: auto;", section);
        }
    }
}
