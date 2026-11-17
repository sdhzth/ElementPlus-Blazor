using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 全站细滚动条口径守卫（<c>src/Components/wwwroot/css/index.css</c>）。
    /// <para>
    /// 口径（2026-10-10 批次⑩ 加宽横轴、同日批次⑫ 定为 6px；2026-11-17 改为布局占位恒定）：
    /// <b>纵向 4px / 横向轨道布局占位恒 12px</b>。Chromium 里 <c>::-webkit-scrollbar</c> 的
    /// <c>width</c> 只管纵轴、<c>height</c> 只管横轴；纵轴维持 4px 是批次⑧的既有依赖
    /// （grow 面板表头/表体槽宽相等，改纵轴会把列拽歪）。
    /// </para>
    /// <para>
    /// 【2026-11-17 布局占位恒定】用户实拍「ElTable 下方滚动条显示或隐藏时整表抖动」。实测根因两条：
    /// ① 横向轨道「静止 6px / 悬停 12px」两档切换（<c>*:hover::-webkit-scrollbar</c>），厚度占布局；
    /// ② dom.js 显形重建无条件 <c>auto↔scroll</c> 互换 + 钉 <c>scrollbar-gutter:stable</c>，
    ///    把没溢出轴的轨道渲染/预留出来（实测首次 hover clientW 638→634 且不复原）。
    /// 修复：横向轨道布局占位恒 <c>var(--el-sb-h-hover, 12px)</c>、删除悬停加高规则（静止拇指透明，
    /// 视觉与 6px 时代一致）；dom.js 只互换真实溢出的轴（溢出轴两取值几何相同）+ 不再钉 gutter。
    /// <c>--el-sb-h</c>(6px) 保留为「静止视觉厚度」标记：常显拇指组件用两钩子差算透明 border 维持 6px 视觉。
    /// </para>
    /// <para>
    /// 批次96（2026-10-21 #155）：ElDropDownTableGrow 面板横条按用户裁定装回库口径，<b>只反横轴</b>；
    /// 批次⑧ 的「纵轴槽宽 0」仍是表头/表体列对齐的前提，一动不动。
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
        public static void Guard_GlobalScrollbarIsThinVertical_HorizontalTrackConstant()
        {
            var block = BlockOf(Css(), "*::-webkit-scrollbar", true);

            // 正向：纵轴 4px（批次⑧ 依赖）；横轴布局占位恒 12px（2026-11-17 布局占位恒定）
            Assert.Equal(4, Decl(block, "width"));
            Assert.Equal(12, Decl(block, "height"));
            Assert.Equal("--el-sb-h-hover", HookOf(block, "height"));
        }

        [Fact]
        public static void Guard_ThinScrollbarAndTreeV2Body_UseTheSameCaliber()
        {
            var block = BlockOf(Css(), ".el-tree-v2__body::-webkit-scrollbar", true);

            Assert.Equal(4, Decl(block, "width"));
            Assert.Equal(12, Decl(block, "height"));
            Assert.Equal("--el-sb-h-hover", HookOf(block, "height"));
        }

        [Fact]
        public static void Guard_HorizontalTrackLayoutNeverToggles_NoHoverHeightRule()
        {
            var css = StripComments(Css());

            // 2026-11-17 起：任何 *:hover::-webkit-scrollbar（轨道本体）的 height 规则都不许存在 ——
            // 轨道厚度切换占布局，就是用户实拍的「滚动条显隐时表格抖动」（thumb 的 :hover 上色不在禁列）。
            Assert.DoesNotContain("*:hover::-webkit-scrollbar {", css);
            // 带尾随 " {" 防止误命中 :hover::-webkit-scrollbar-thumb（拇指上色规则合法且必须存在）
            Assert.DoesNotContain(".el-thin-scrollbar:hover::-webkit-scrollbar {", css);
            Assert.DoesNotContain(".el-tree-v2__body:hover::-webkit-scrollbar {", css);

            // 拇指显形/加深的两条 hover 规则必须还在（显形机制不变）
            Assert.Contains("*:hover::-webkit-scrollbar-thumb", css);
            Assert.Contains("*::-webkit-scrollbar-thumb:hover", css);

            // 拇指常显组件的「静止 6px 视觉」border 内缩钩子必须在（两钩子差的一半）
            Assert.Contains("::-webkit-scrollbar-thumb:horizontal", css);
            Assert.Contains("background-clip: padding-box", css);

            // 倒咬一口：滚道被用户批准归零的宿主（批次⑧ 的 pro-tabs、EP noShowScrollbar 的 wrap）
            // 必须带退出钩子 —— 现在它直接决定轨道布局占位（0 = 整条归零）。
            var zero = css.IndexOf("--el-sb-h-hover: 0px", System.StringComparison.Ordinal);
            Assert.True(zero >= 0, "退出钩子 --el-sb-h-hover: 0px 整条不见了：刻意归零的滚道会被 12px 占位顶回来");
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
        /// 批次96 · #155：ElDropDownTableGrow 下拉面板的横条按用户裁定装回库口径，<b>只反横轴</b>；
        /// 批次⑧ 批准的「纵轴槽宽 0」是表头/表体列对齐的前提，仍然是 0，谁把它改回 4px 这条就红。
        /// 2026-11-17 起横轴轨道布局占位恒 12px（--el-sb-h-hover 钩子），组件里不许再有 :hover 高度档；
        /// 静止拇指常显 → 必须带 :horizontal 透明 border（两钩子差的一半）维持 6px 静止视觉。
        /// </summary>
        [Fact]
        public static void Guard_GrowDropDownPanel_HorizontalBarUsesLibraryCaliber_VerticalSlotStaysZero()
        {
            var razor = StripComments(GrowRazor());
            var body = BlockOf(razor, ".el-dropdown-table-grow__panel .el-table__body-wrapper::-webkit-scrollbar {", true);
            var head = BlockOf(razor, ".el-dropdown-table-grow__panel .el-table__header-wrapper::-webkit-scrollbar {", true);

            // 纵轴（::-webkit-scrollbar 的 width 只管纵向那条）必须还是 0：批次⑧ 的 headSlot/bodySlot
            // 同为 0 才让 clientW 相等、逐列边界差全 0（Temp/diag-batch8-scrollbar-zero2.log B 段，
            // 常驻尺子 verify-batch8.cjs G1/G2/G3）。
            Assert.Equal(0, Decl(body, "width"));
            Assert.Equal(0, Decl(head, "width"));

            // 横轴（height 只管横向那条）与 index.css 全局那条同源：轨道布局占位恒 12px，读 --el-sb-h-hover。
            var css = StripComments(Css());
            var globalTrack = BlockOf(css, "*::-webkit-scrollbar {", true);

            var trackH = Decl(body, "height");
            Assert.Equal(Decl(globalTrack, "height"), trackH);
            Assert.Equal(12, trackH);
            Assert.Equal("--el-sb-h-hover", HookOf(body, "height"));

            // 组件里不许再有 :hover 高度档（旧批次94 #154 形状）：那是布局抖动的元凶，2026-11-17 移除
            Assert.DoesNotContain(".el-dropdown-table-grow__panel .el-table__body-wrapper:hover::-webkit-scrollbar {", razor);

            // 静止拇指常显的 6px 视觉靠 :horizontal 透明 border 内缩，悬停放开 —— 两条都必须在
            var thumbRest = BlockOf(razor, ".el-dropdown-table-grow__panel .el-table__body-wrapper::-webkit-scrollbar-thumb:horizontal {", true);
            Assert.Contains("background-clip: padding-box", thumbRest);
            Assert.Contains("var(--el-sb-h", thumbRest);
            var thumbHover = BlockOf(razor, ".el-dropdown-table-grow__panel .el-table__body-wrapper:hover::-webkit-scrollbar-thumb:horizontal {", true);
            Assert.Contains("border-top-width: 0", thumbHover);
            Assert.Contains("border-bottom-width: 0", thumbHover);

            // 表头容器不画横条：EP 给 .el-table__header-wrapper 的是 overflow:hidden，横条拖不动，
            // 轨道只会从表头底部白吃一条高度、并让表头/表体的可视高度不相等。
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
