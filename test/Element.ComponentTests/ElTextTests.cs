using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElText 单元测试 — 对齐 Element Plus Text 官方 API
    /// </summary>
    public class ElTextTests : BunitContext
    {
        public ElTextTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddElementServices();
        }

        // ==================== 基本渲染 ====================

        [Fact]
        public void Default_TagIsSpan()
        {
            var cut = Render<ElText>(p => p.AddChildContent("Hello"));
            var el = cut.Find("span");
            Assert.Equal("Hello", el.TextContent.Trim());
            Assert.Contains("el-text", el.ClassList);
        }

        [Fact]
        public void Default_TypeDefault_NoTypeClass()
        {
            var cut = Render<ElText>(p => p.AddChildContent("x"));
            var el = cut.Find("span");
            Assert.Contains("el-text", el.ClassList);
            Assert.DoesNotContain("el-text--default", el.ClassList);
            Assert.DoesNotContain("el-text--primary", el.ClassList);
        }

        [Fact]
        public void Default_SizeDefault_NoSizeClass()
        {
            var cut = Render<ElText>(p => p.AddChildContent("x"));
            Assert.DoesNotContain("el-text--large", cut.Find("span").ClassList);
            Assert.DoesNotContain("el-text--small", cut.Find("span").ClassList);
        }

        // ==================== Type ====================

        [Fact]
        public void Type_Primary()
        {
            var cut = Render<ElText>(p => p.Add(x => x.Type, TextType.Primary).AddChildContent("x"));
            Assert.Contains("el-text--primary", cut.Find("span").ClassList);
        }

        [Fact]
        public void Type_Success()
        {
            var cut = Render<ElText>(p => p.Add(x => x.Type, TextType.Success).AddChildContent("x"));
            Assert.Contains("el-text--success", cut.Find("span").ClassList);
        }

        [Fact]
        public void Type_Warning()
        {
            var cut = Render<ElText>(p => p.Add(x => x.Type, TextType.Warning).AddChildContent("x"));
            Assert.Contains("el-text--warning", cut.Find("span").ClassList);
        }

        [Fact]
        public void Type_Danger()
        {
            var cut = Render<ElText>(p => p.Add(x => x.Type, TextType.Danger).AddChildContent("x"));
            Assert.Contains("el-text--danger", cut.Find("span").ClassList);
        }

        [Fact]
        public void Type_Info()
        {
            var cut = Render<ElText>(p => p.Add(x => x.Type, TextType.Info).AddChildContent("x"));
            Assert.Contains("el-text--info", cut.Find("span").ClassList);
        }

        // ==================== Size ====================

        [Fact]
        public void Size_Large()
        {
            var cut = Render<ElText>(p => p.Add(x => x.Size, TextSize.Large).AddChildContent("x"));
            Assert.Contains("el-text--large", cut.Find("span").ClassList);
        }

        [Fact]
        public void Size_Small()
        {
            var cut = Render<ElText>(p => p.Add(x => x.Size, TextSize.Small).AddChildContent("x"));
            Assert.Contains("el-text--small", cut.Find("span").ClassList);
        }

        // ==================== Truncated ====================

        [Fact]
        public void Truncated_AddsIsTruncatedClass()
        {
            var cut = Render<ElText>(p => p.Add(x => x.Truncated, true).AddChildContent("Long text"));
            // 截断时文字 span 被挪进 ElTooltip 的触发 span 里（浮层渲染在触发 span 内部，
            // 不能放进带 overflow:hidden 的 .el-text），所以第一个 span 是触发 span，必须按类名取。
            Assert.Contains("is-truncated", cut.Find(".el-text").ClassList);
        }

        [Fact]
        public void Truncated_False_NoClass()
        {
            var cut = Render<ElText>(p => p.AddChildContent("x"));
            Assert.DoesNotContain("is-truncated", cut.Find("span").ClassList);
        }

        // ==================== LineClamp ====================

        [Fact]
        public void LineClamp_PositiveInt_AddsIsLineClamp()
        {
            var cut = Render<ElText>(p => p.Add(x => x.LineClamp, 3).AddChildContent("Multiline"));
            var el = cut.Find(".el-text");
            Assert.Contains("is-line-clamp", el.ClassList);
            Assert.Contains("--el-text-line-clamp:3", el.GetAttribute("style"));
        }

        [Fact]
        public void LineClamp_Null_NoClass()
        {
            var cut = Render<ElText>(p => p.AddChildContent("x"));
            Assert.DoesNotContain("is-line-clamp", cut.Find("span").ClassList);
        }

        [Fact]
        public void LineClamp_Zero_NoClass()
        {
            var cut = Render<ElText>(p => p.Add(x => x.LineClamp, 0).AddChildContent("x"));
            Assert.DoesNotContain("is-line-clamp", cut.Find("span").ClassList);
        }

        [Fact]
        public void LineClamp_Negative_NoClass()
        {
            var cut = Render<ElText>(p => p.Add(x => x.LineClamp, -1).AddChildContent("x"));
            Assert.DoesNotContain("is-line-clamp", cut.Find("span").ClassList);
        }

        // ==================== Tag（自定义元素）====================

        [Fact]
        public void Tag_DefaultIsSpan()
        {
            var cut = Render<ElText>(p => p.AddChildContent("x"));
            Assert.Equal("span", cut.Find("span").TagName.ToLower());
        }

        [Fact]
        public void Tag_CustomParagraph()
        {
            var cut = Render<ElText>(p => p.Add(x => x.Tag, "p").AddChildContent("para"));
            var el = cut.Find("p");
            Assert.Contains("el-text", el.ClassList);
            Assert.Equal("para", el.TextContent.Trim());
        }

        [Fact]
        public void Tag_Bold()
        {
            var cut = Render<ElText>(p => p.Add(x => x.Tag, "b").AddChildContent("Bold"));
            Assert.Equal("b", cut.Find("b").TagName.ToLower());
        }

        [Fact]
        public void Tag_Italic()
        {
            var cut = Render<ElText>(p => p.Add(x => x.Tag, "i").AddChildContent("Italic"));
            Assert.Equal("i", cut.Find("i").TagName.ToLower());
        }

        [Fact]
        public void Tag_EmptyFallsBackToSpan()
        {
            var cut = Render<ElText>(p => p.Add(x => x.Tag, "").AddChildContent("x"));
            Assert.Equal("span", cut.Find("span").TagName.ToLower());
        }

        [Fact]
        public void Tag_CustomWithTypeAndSize()
        {
            var cut = Render<ElText>(p => p
                .Add(x => x.Tag, "mark")
                .Add(x => x.Type, TextType.Danger)
                .Add(x => x.Size, TextSize.Large)
                .AddChildContent("Marked"));

            var el = cut.Find("mark");
            Assert.Contains("el-text", el.ClassList);
            Assert.Contains("el-text--danger", el.ClassList);
            Assert.Contains("el-text--large", el.ClassList);
        }

        // ==================== Cls / Style / Attributes ====================

        [Fact]
        public void Cls_Appended()
        {
            var cut = Render<ElText>(p => p.Add(x => x.Cls, "my-text").AddChildContent("x"));
            Assert.Contains("my-text", cut.Find("span").ClassList);
        }

        [Fact]
        public void Style_Appended()
        {
            var cut = Render<ElText>(p => p.Add(x => x.Style, "font-family:monospace").AddChildContent("x"));
            Assert.Contains("font-family:monospace", cut.Find("span").GetAttribute("style"));
        }

        [Fact]
        public void AttributeClass_MergedNotDropped()
        {
            var cut = Render<ElText>(p => p.AddChildContent("x").AddUnmatched("class", "custom-cls"));
            var el = cut.Find("span");
            Assert.Contains("el-text", el.ClassList);
            Assert.Contains("custom-cls", el.ClassList);
        }

        [Fact]
        public void AttributeStyle_MergedNotDropped()
        {
            var cut = Render<ElText>(p => p.AddChildContent("x").AddUnmatched("style", "color:red"));
            Assert.Contains("color:red", cut.Find("span").GetAttribute("style"));
        }

        [Fact]
        public void AttributeStyle_AndLineClamp_Coexist()
        {
            var cut = Render<ElText>(p => p
                .Add(x => x.LineClamp, 2)
                .AddChildContent("x")
                .AddUnmatched("style", "color:red"));
            // LineClamp 生效时外层多了触发 span，按 .el-text 取真正承载样式的那个元素。
            var style = cut.Find(".el-text").GetAttribute("style");
            Assert.Contains("--el-text-line-clamp:2", style);
            Assert.Contains("color:red", style);
        }

        [Fact]
        public void Attributes_Passthrough()
        {
            var cut = Render<ElText>(p => p
                .AddChildContent("x")
                .AddUnmatched("data-qa", "label-name"));
            Assert.Equal("label-name", cut.Find("span").GetAttribute("data-qa"));
        }

        // ==================== 端到端 ====================

        [Fact]
        public void FullFeature_DangerLargeTruncated()
        {
            var cut = Render<ElText>(p => p
                .Add(x => x.Type, TextType.Danger)
                .Add(x => x.Size, TextSize.Large)
                .Add(x => x.Truncated, true)
                .Add(x => x.LineClamp, 2)
                .AddChildContent("Emergency alert text"));

            var el = cut.Find(".el-text");
            Assert.Contains("el-text", el.ClassList);
            Assert.Contains("el-text--danger", el.ClassList);
            Assert.Contains("el-text--large", el.ClassList);
            Assert.Contains("is-truncated", el.ClassList);
            Assert.Contains("is-line-clamp", el.ClassList);
            Assert.Contains("--el-text-line-clamp:2", el.GetAttribute("style"));
            Assert.Equal("Emergency alert text", el.TextContent.Trim());
        }

        // ==================== 截断悬停看全文（本库扩展） ====================

        /// <summary>
        /// bUnit 里没有布局引擎，"文字是否真被裁"只能由 JS interop 喂进来
        /// （Loose 模式未打桩返回 default＝null，等价于"量不到＝没裁"）。
        /// </summary>
        private void StubTextOverflow(bool clipped, string text = "一段会被裁掉的长文本",
                                      bool clippedVertical = false, int scrollHeight = 0, int clientHeight = 0)
            => JSInterop.Setup<TextClipState>(TextClip.MeasureFunction, _ => true)
                .SetResult(new TextClipState
                {
                    ScrollWidth = clipped ? 640 : 180,
                    ClientWidth = 180,
                    ScrollHeight = clippedVertical ? scrollHeight : (clientHeight > 0 ? clientHeight : 22),
                    ClientHeight = clientHeight > 0 ? clientHeight : 22,
                    Text = text
                });

        /// <summary>负向断言的反空转对照：确认悬停真的走到了 JS 测算那一步。</summary>
        private void WaitForOverflowMeasured<TComponent>(IRenderedComponent<TComponent> cut)
            where TComponent : IComponent
        {
            cut.WaitForAssertion(() => Assert.Contains(
                TextClip.MeasureFunction,
                JSInterop.Invocations.Select(x => x.Identifier)), timeout: TestWait.Timeout);
        }

        [Fact]
        public void TruncatedMountsTooltipTriggerAroundText()
        {
            var cut = Render<ElText>(p => p.Add(x => x.Truncated, true).AddChildContent("长文本"));

            // 浮层渲染在 ElTooltip 触发 span 内部，只能包住 .el-text 这个裁剪盒。
            // 这条 span 必须同时带 min-width:0 与 max-width:100%：EP 的 .el-text.is-truncated 是靠
            // 自己的 max-width:100% 被宿主宽度钳住的，中间插一层 auto 宽的包装就把钳制链断了 ——
            // 浏览器实测（/basic 的 180px 容器）：不带 max-width 时包装层 378px、文字整段冲出、
            // scrollWidth==clientWidth＝根本没裁，截断功能等于没做。几何只能在浏览器量，
            // bUnit 没有布局引擎（见 docs/testing-guards.md §4 第 11 条），这里先把属性钉住。
            var trigger = cut.Find(".el-text").ParentElement;
            Assert.Contains("el-tooltip__trigger", trigger.ClassList);
            Assert.Contains("min-width:0", trigger.GetAttribute("style"));
            Assert.Contains("max-width:100%", trigger.GetAttribute("style"));
        }

        [Fact]
        public void TruncatedShowsFullTextOnHoverWhenClipped()
        {
            StubTextOverflow(clipped: true, text: "完整文字");
            var cut = Render<ElText>(p => p.Add(x => x.Truncated, true).AddChildContent("完整文字"));

            cut.Find(".el-text").TriggerEvent("onmouseenter", new MouseEventArgs());

            cut.WaitForAssertion(() =>
            {
                var popper = cut.Find(".el-tooltip__popper");
                Assert.Equal("完整文字", popper.TextContent.Trim());
                Assert.Equal("tooltip", popper.GetAttribute("role"));
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public void TruncatedKeepsTooltipHiddenWhenNotClipped()
        {
            StubTextOverflow(clipped: false, text: "没被裁的文字");
            var cut = Render<ElText>(p => p.Add(x => x.Truncated, true).AddChildContent("没被裁的文字"));

            cut.Find(".el-text").TriggerEvent("onmouseenter", new MouseEventArgs());

            WaitForOverflowMeasured(cut);
            Assert.Empty(cut.FindAll(".el-tooltip__popper"));
        }

        [Fact]
        public void TruncatedHidesTooltipOnMouseLeave()
        {
            StubTextOverflow(clipped: true, text: "完整文字");
            var cut = Render<ElText>(p => p.Add(x => x.Truncated, true).AddChildContent("完整文字"));

            cut.Find(".el-text").TriggerEvent("onmouseenter", new MouseEventArgs());
            cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".el-tooltip__popper")), timeout: TestWait.Timeout);

            cut.Find(".el-text").TriggerEvent("onmouseleave", new MouseEventArgs());

            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".el-tooltip__popper")), timeout: TestWait.Timeout);
        }

        [Fact]
        public void LineClampAcceptsVerticalClipping()
        {
            // 只纵向被裁（多行 line-clamp 的场景）：横向读数完全不动，判据必须承认高度方向。
            // 去掉 ElText 的 Vertical 传参，这一条就会红 —— 单行 nowrap 框不许开纵向判据。
            StubTextOverflow(clipped: false, text: "三行截断的完整内容",
                clippedVertical: true, scrollHeight: 66, clientHeight: 44);
            var cut = Render<ElText>(p => p
                .Add(x => x.LineClamp, 2)
                .AddChildContent("三行截断的完整内容"));

            cut.Find(".el-text").TriggerEvent("onmouseenter", new MouseEventArgs());

            cut.WaitForAssertion(() => Assert.Equal("三行截断的完整内容",
                cut.Find(".el-tooltip__popper").TextContent.Trim()), timeout: TestWait.Timeout);
        }

        [Fact]
        public void OnePixelOverflowIsNotTreatedAsClipped()
        {
            // 判据是 scrollWidth > clientWidth + 1：line-height/边框取整会带来 1px 噪声，
            // 把它当截断就等于每个元素悬停都浮一层。去掉那个 +1 这条就红。
            JSInterop.Setup<TextClipState>(TextClip.MeasureFunction, _ => true)
                .SetResult(new TextClipState { ScrollWidth = 181, ClientWidth = 180, Text = "刚好一像素" });
            var cut = Render<ElText>(p => p.Add(x => x.Truncated, true).AddChildContent("刚好一像素"));

            cut.Find(".el-text").TriggerEvent("onmouseenter", new MouseEventArgs());

            cut.WaitForAssertion(() => Assert.Contains(TextClip.MeasureFunction,
                JSInterop.Invocations.Select(x => x.Identifier)), timeout: TestWait.Timeout);
            Assert.Empty(cut.FindAll(".el-tooltip__popper"));
        }

        [Fact]
        public void SingleLineTruncationIgnoresVerticalOverflow()
        {
            // .el-text.is-truncated 是单行 nowrap 框：横向没溢出就不该浮，
            // 纵向读数差来自 line-height 取整而不是截断（Vertical 只在 LineClamp 下才开）。
            JSInterop.Setup<TextClipState>(TextClip.MeasureFunction, _ => true)
                .SetResult(new TextClipState { ScrollWidth = 180, ClientWidth = 180, ScrollHeight = 66, ClientHeight = 22, Text = "纵向噪声" });
            var cut = Render<ElText>(p => p.Add(x => x.Truncated, true).AddChildContent("纵向噪声"));

            cut.Find(".el-text").TriggerEvent("onmouseenter", new MouseEventArgs());

            cut.WaitForAssertion(() => Assert.Contains(TextClip.MeasureFunction,
                JSInterop.Invocations.Select(x => x.Identifier)), timeout: TestWait.Timeout);
            Assert.Empty(cut.FindAll(".el-tooltip__popper"));
        }

        [Fact]
        public void EllipsisTooltipFalseFallsBackToEpNativeTitle()
        {
            StubTextOverflow(clipped: true, text: "完整文字");
            var cut = Render<ElText>(p => p
                .Add(x => x.EllipsisTooltip, false)
                .Add(x => x.Truncated, true)
                .AddChildContent("完整文字"));

            // EP text.vue:42-71 的口径：没有浮层，实测被裁才写原生 title。
            cut.WaitForAssertion(() => Assert.Contains("完整文字",
                JSInterop.Invocations
                    .Where(x => x.Identifier == "elementSetEllipsisTitle")
                    .SelectMany(x => x.Arguments.Select(a => a?.ToString() ?? string.Empty)))
                , timeout: TestWait.Timeout);
            Assert.Empty(cut.FindAll(".el-tooltip__trigger"));
        }

        [Fact]
        public void EllipsisTooltipFalseWritesNoTitleWhenNotClipped()
        {
            StubTextOverflow(clipped: false);
            var cut = Render<ElText>(p => p
                .Add(x => x.EllipsisTooltip, false)
                .Add(x => x.Truncated, true)
                .AddChildContent("没被裁"));

            WaitForOverflowMeasured(cut);
            Assert.DoesNotContain("elementSetEllipsisTitle", JSInterop.Invocations.Select(x => x.Identifier));
        }

        [Fact]
        public void NoTruncationMountsNoTooltipAndSkipsJs()
        {
            var cut = Render<ElText>(p => p.AddChildContent("普通文字"));

            // 没开截断＝零 DOM 变化：既没有触发 span，也不该有一次 JS 往返。
            Assert.Empty(cut.FindAll(".el-tooltip__trigger"));
            Assert.Single(cut.FindAll("span"));
            Assert.Equal("普通文字", cut.Find(".el-text").TextContent.Trim());
            Assert.DoesNotContain(TextClip.MeasureFunction, JSInterop.Invocations.Select(x => x.Identifier));
        }
    }
}