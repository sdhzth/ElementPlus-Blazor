using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// <c>ElPopconfirm.FixedPopper</c>：气泡钉成 position:fixed + 弹出时实测的触发元素视口坐标。
    /// 与 <see cref="ElTooltipFixedPopperTests"/> 同一套基建（<see cref="TextClip"/>），但默认开 ——
    /// 气泡是触发 span 的子孙，absolute 会被表格单元格那条 overflow 链裁掉/挤压（消费方实拍：
    /// 气泡与按钮挤成一行）。bUnit 没有布局引擎，这里钉的是坐标算得对不对、退回路径是否完好。
    /// </summary>
    public class ElPopconfirmFixedPopperTests : BunitContext
    {
        public ElPopconfirmFixedPopperTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddElementPlusServices();
        }

        /// <summary>喂给 JS 的触发元素矩形。56×28 是小按钮的量级，中心点 (228, 514)。</summary>
        private void SetupRect(double top, double left, double width, double height)
        {
            JSInterop.Setup<TextClipState>(TextClip.MeasureFunction, _ => true)
                .SetResult(new TextClipState
                {
                    ScrollWidth = 56,
                    ClientWidth = 56,
                    ScrollHeight = 28,
                    ClientHeight = 28,
                    Text = "确定要删除该条记录吗?",
                    RectTop = top,
                    RectLeft = left,
                    RectWidth = width,
                    RectHeight = height,
                });
        }

        private IRenderedComponent<ElPopconfirm> Open(string placement, bool fixedPopper, int offset = 8)
        {
            var cut = Render<ElPopconfirm>(p => p
                .Add(x => x.Title, "确定要删除该条记录吗?")
                .Add(x => x.Placement, placement)
                .Add(x => x.FixedPopper, fixedPopper)
                .Add(x => x.Offset, offset)
                .AddChildContent("<button>删除</button>"));
            cut.Find(".el-popconfirm__reference-wrapper").Click();
            cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".el-popconfirm")), timeout: TestWait.Timeout);
            return cut;
        }

        private static string Style(IRenderedComponent<ElPopconfirm> cut) =>
            cut.Find(".el-popconfirm").GetAttribute("style");

        [Fact]
        public void FixedPopper_Top_AnchorsToMeasuredViewportRect()
        {
            SetupRect(top: 500, left: 200, width: 56, height: 28);
            var cut = Open("top", true);
            var style = Style(cut);

            Assert.Contains("position:fixed", style);
            Assert.DoesNotContain("position:absolute", style);
            // top 档：水平居中 = 200 + 56/2 = 228；气泡 top = 触发顶 500 - 8 间距 = 492，再 translate(-100%) 抬上去
            Assert.Contains("left:228px", style);
            Assert.Contains("top:492px", style);
            Assert.Contains("transform:translate(-50%, -100%)", style);
            // absolute 那套相对锚点必须让位，否则 left/top 与 bottom:100% 同时生效会顶飞
            Assert.DoesNotContain("bottom:100%", style);
            Assert.Equal("top", cut.Find(".el-popconfirm").GetAttribute("x-placement"));
        }

        [Fact]
        public void FixedPopper_FlipsBelowWhenNoRoomAbove()
        {
            SetupRect(top: 10, left: 200, width: 56, height: 28);
            var cut = Open("top", true);

            // 上方只 10px，放不下 ≈31px 的单行气泡 ⇒ 翻到下方，x-placement 跟着翻（箭头才不指反）
            Assert.Equal("bottom", cut.Find(".el-popconfirm").GetAttribute("x-placement"));
            Assert.Contains("top:46px", Style(cut));
            Assert.Contains("transform:translate(-50%, 0)", Style(cut));
        }

        [Fact]
        public void FixedPopper_UsesOffsetAsGap()
        {
            SetupRect(top: 500, left: 200, width: 56, height: 28);
            // top + offset 20 ⇒ top = 500 - 20 = 480
            Assert.Contains("top:480px", Style(Open("top", true, offset: 20)));
        }

        /// <summary>显式关掉时仍是 absolute + 相对锚点 —— 退路完好，行为与本参数加入前逐字节一致。</summary>
        [Fact]
        public void FixedPopperOff_KeepsAbsoluteAnchors()
        {
            SetupRect(top: 500, left: 200, width: 56, height: 28);
            var cut = Open("top", false);
            var style = Style(cut);

            Assert.Contains("position:absolute", style);
            Assert.DoesNotContain("position:fixed", style);
            Assert.Contains("bottom:100%", style);
            Assert.Contains("transform:translate(-50%, -8px)", style);
            Assert.Equal("top", cut.Find(".el-popconfirm").GetAttribute("x-placement"));
        }

        /// <summary>量不到矩形（bUnit 未打桩 / 元素已移除）时静默退回 absolute，不能让一次点击炸掉渲染。</summary>
        [Fact]
        public void FixedPopper_WithoutJsReading_FallsBackToAbsolute()
        {
            var cut = Render<ElPopconfirm>(p => p
                .Add(x => x.Title, "确定要删除该条记录吗?")
                .AddChildContent("<button>删除</button>"));
            cut.Find(".el-popconfirm__reference-wrapper").Click();
            cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".el-popconfirm")), timeout: TestWait.Timeout);

            Assert.Contains("position:absolute", Style(cut));
        }

        /// <summary>关闭后 fixed 坐标要清掉，下次再弹出重新量 —— 否则挪过位置后气泡钉在旧坐标上。</summary>
        [Fact]
        public void FixedPopper_ClearedOnClose_ReMeasuredOnReopen()
        {
            SetupRect(top: 500, left: 200, width: 56, height: 28);
            var cut = Open("top", true);
            Assert.Contains("position:fixed", Style(cut));

            cut.Find(".el-popconfirm__reference-wrapper").Click();
            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".el-popconfirm")), timeout: TestWait.Timeout);

            // 挪到别处再弹，坐标必须跟着新矩形走
            SetupRect(top: 800, left: 400, width: 56, height: 28);
            cut.Find(".el-popconfirm__reference-wrapper").Click();
            cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".el-popconfirm")), timeout: TestWait.Timeout);
            var style = Style(cut);
            Assert.Contains("position:fixed", style);
            Assert.Contains("left:428px", style);
            Assert.Contains("top:792px", style);
        }
    }
}
