using AngleSharp.Dom;
using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElColorPickerPanelTests : BunitContext
    {
        public ElColorPickerPanelTests()
        {
            Services.AddElementServices();
        }

        [Theory]
        [InlineData("#409EFF", "#409eff")]
        [InlineData("rgb(255, 120, 0)", "#ff7800")]
        [InlineData("rgba(255, 69, 0, 0.68)", "rgba(255, 69, 0, 0.68)")]
        [InlineData("hsv(51, 100, 98)", "#fad400")]
        [InlineData("hsla(209, 100%, 56%, 0.73)", "rgba(31, 147, 255, 0.73)")]
        [InlineData("#c7158577", "rgba(199, 21, 133, 0.467)")]
        public void ElementColorParsesSupportedFormats(string value, string expected)
        {
            var color = ElementColor.Parse(value);
            var actual = value.Contains("0.") || value.Length == 9
                ? color.ToCssString(ElementColorFormat.Rgb, true)
                : color.ToCssString(ElementColorFormat.Hex, false);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void RendersAlphaPredefineBorderAndFooter()
        {
            RenderFragment footer = builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "custom-footer");
                builder.AddContent(2, "Apply");
                builder.CloseElement();
            };

            var cut = Render<ElColorPickerPanel>(parameters => parameters
                .Add(x => x.Value, "rgba(255, 69, 0, 0.68)")
                .Add(x => x.ShowAlpha, true)
                .Add(x => x.Border, false)
                .Add(x => x.Predefine, new[]
                {
                    "#ff4500",
                    "rgba(255, 69, 0, 0.68)"
                })
                .Add(x => x.FooterContent, footer));

            Assert.Contains("is-borderless", cut.Find(".el-color-picker-panel").ClassList);
            Assert.NotNull(cut.Find(".el-color-alpha-slider"));
            Assert.Equal(2, cut.FindAll(".el-color-predefine__color-selector").Count);
            Assert.Contains("selected", cut.FindAll(".el-color-predefine__color-selector")[1].ClassList);
            Assert.Contains("custom-footer", cut.Markup);
        }

        [Fact]
        public void HueInputUpdatesBoundValue()
        {
            var value = "#ff0000";
            var changes = new List<string>();
            var cut = Render<ElColorPickerPanel>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.OnChange, next => changes.Add(next)));

            cut.Find("input[aria-label='hue']").Input("120");

            Assert.Equal("#00ff00", value);
            Assert.Equal(new[] { "#00ff00" }, changes);
        }

        [Fact]
        public void TextInputRespectsColorFormat()
        {
            var value = "#409eff";
            var cut = Render<ElColorPickerPanel>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ColorFormat, "rgb")
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find("input[aria-label='color value']").Input("#ff0000");
            cut.Find("input[aria-label='color value']").Change("#ff0000");

            Assert.Equal("rgb(255, 0, 0)", value);
        }

        [Fact]
        public void DisabledPanelIgnoresInput()
        {
            var value = "#ff0000";
            var cut = Render<ElColorPickerPanel>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.Disabled, true)
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find("input[aria-label='hue']").Input("120");

            Assert.Equal("#ff0000", value);
            Assert.True(cut.Find("input[aria-label='hue']").HasAttribute("disabled"));
        }

        // ==== 批次⑳ #118：三块区域的按住拖动 =====================================
        // EP 口径实抓：SV 面板把 draggable(rootEl,{drag,end}) 绑在根元素上
        // （Temp/ep2140-cp-use-sv-panel.mjs:99-109），色相/alpha 条绑在 bar 和 thumb 上
        // （Temp/ep2140-cp-use-slider.mjs:112-125），坐标一律取 getBoundingClientRect，
        // 滑块映射还要扣掉 thumb 宽（:19-36，hue maxValue=360、alpha maxValue=100）。
        // 本仓原来只有 click 通道，且两张透明 range 铺满面板把指针全吃了
        // （实测：Temp/b20-drag-diag-1.log、Temp/b20-drag-diag-2.log）。

        /// <summary>bUnit 没有布局引擎，视口矩形只能桩一个。这里桩成 left=0、top=0、280x180，
        /// 与 index.css 写死的 .el-color-svpanel 尺寸一致，用例里的 clientX/clientY 就能直接读成面板内坐标。</summary>
        private void StubPanelGeometry(double left = 0, double top = 0, double width = 280, double height = 180)
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            JSInterop.Setup<ElementViewportRect>("elementViewportRect", _ => true)
                .SetResult(new ElementViewportRect
                {
                    Left = left,
                    Top = top,
                    Right = left + width,
                    Bottom = top + height,
                    Width = width,
                    Height = height,
                    Vw = 1440,
                    Vh = 1000
                });
            JSInterop.Setup<bool>("elementPointerCapture", _ => true).SetResult(true);
            JSInterop.SetupVoid("execFocus", _ => true).SetVoidResult();
        }

        private static PointerEventArgs Pointer(double clientX, double clientY, long pointerId = 7)
            => new PointerEventArgs { ClientX = clientX, ClientY = clientY, PointerId = pointerId };

        private int CaptureCalls(bool? capture)
            => JSInterop.Invocations.Count(i => i.Identifier == "elementPointerCapture"
                && (capture == null || Convert.ToBoolean(i.Arguments[i.Arguments.Count - 1]) == capture.Value));

        private IElement SvPanel(IRenderedComponent<ElColorPickerPanel> cut) => cut.Find(".el-color-svpanel");
        private IElement HueBar(IRenderedComponent<ElColorPickerPanel> cut) => cut.Find(".el-color-hue-slider");
        private IElement AlphaBar(IRenderedComponent<ElColorPickerPanel> cut) => cut.Find(".el-color-alpha-slider");
        private string RangeValue(IRenderedComponent<ElColorPickerPanel> cut, string selector) => cut.Find(selector).GetAttribute("value");
        private string CursorStyle(IRenderedComponent<ElColorPickerPanel> cut) => cut.Find(".el-color-svpanel__cursor").GetAttribute("style");

        [Fact]
        public async Task SvPanelPressAndDragFollowPointerOnBothAxesAndCommitOnce()
        {
            StubPanelGeometry();
            var value = "#409eff";
            var changes = new List<string>();
            var actives = new List<string>();
            var cut = Render<ElColorPickerPanel>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.OnChange, next => changes.Add(next))
                .Add(x => x.OnActiveChange, next => actives.Add(next)));

            var panel = SvPanel(cut);
            await panel.PointerDownAsync(Pointer(70, 45));
            // 按下即跳位：(70,45) → sat 25%、value 75%
            Assert.Equal("25", RangeValue(cut, ".el-color-picker-panel__saturation-input"));
            Assert.Equal("75", RangeValue(cut, ".el-color-picker-panel__value-input"));
            Assert.Empty(changes);
            Assert.Single(actives);

            await panel.PointerMoveAsync(Pointer(210, 135));
            // 拖动途中就跟随，不等松手
            Assert.Equal("75", RangeValue(cut, ".el-color-picker-panel__saturation-input"));
            Assert.Equal("25", RangeValue(cut, ".el-color-picker-panel__value-input"));
            Assert.Equal("left:75%;top:75%;", CursorStyle(cut));
            Assert.Empty(changes);
            Assert.Equal(2, actives.Count);

            await panel.PointerUpAsync(Pointer(210, 135));
            Assert.Single(changes);
            Assert.Equal(3, actives.Count);

            // 浏览器在 pointerup 之后一定补一个 click；EP 的 handleClick 只做聚焦，
            // 位置已由指针通道写过 —— 这里必须不再重复提交一次。
            await panel.ClickAsync(new MouseEventArgs { ClientX = 210, ClientY = 135, OffsetX = 210, OffsetY = 135 });
            Assert.Single(changes);
            Assert.Equal(3, actives.Count);

            // 一次手势只量一次矩形（每次移动都量就等于把卡顿请回来）
            Assert.Equal(1, JSInterop.Invocations.Count(i => i.Identifier == "elementViewportRect"));
            Assert.Equal(1, CaptureCalls(true));
            Assert.Equal(1, CaptureCalls(false));
        }

        [Fact]
        public async Task SvPanelCornerKeepsSaturationWhenBlackRoundTripsThroughValue()
        {
            StubPanelGeometry();
            var value = "#409eff";
            var cut = Render<ElColorPickerPanel>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next));

            var panel = SvPanel(cut);
            await panel.PointerDownAsync(Pointer(250, 90));
            await panel.PointerMoveAsync(Pointer(400, 400));   // 越出右下角：EP 是钳制，不是丢事件
            await panel.PointerUpAsync(Pointer(400, 400));

            Assert.Equal("#000000", value);
            Assert.Equal("left:100%;top:100%;", CursorStyle(cut));
            Assert.Equal("100", RangeValue(cut, ".el-color-picker-panel__saturation-input"));
            Assert.Equal("0", RangeValue(cut, ".el-color-picker-panel__value-input"));

            // 宿主把同一个颜色写回来：黑 #000000 解析出的饱和度是 0，
            // 不加「同色不重导」这道闸，圆点会当场从右边缘甩到左边缘。
            cut.Render(parameters => parameters.Add(x => x.Value, value));
            Assert.Equal("left:100%;top:100%;", CursorStyle(cut));
            Assert.Equal("100", RangeValue(cut, ".el-color-picker-panel__saturation-input"));

            // 正向对照：真的换了别的颜色就必须重导，圆点跟着走
            // #00ff00 的 HSV 是 hue 120 / sat 100 / val 100 —— 饱和度满格，圆点该贴右边缘
            cut.Render(parameters => parameters.Add(x => x.Value, "#00ff00"));
            Assert.Equal("left:100%;top:0%;", CursorStyle(cut));
        }

        [Theory]
        // EP 公式：offset = clamp(x, thumb/2, width-thumb/2) - thumb/2，value = round(offset/(width-thumb)*360)
        [InlineData(6, "5")]
        [InlineData(70, "89")]
        [InlineData(140, "180")]
        [InlineData(270, "350")]
        [InlineData(279, "360")]
        public async Task HueBarUsesEpThumbWidthMapping(double clientX, string expectedHue)
        {
            StubPanelGeometry();
            var value = "#ff0000";
            var cut = Render<ElColorPickerPanel>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next));

            await HueBar(cut).PointerDownAsync(Pointer(clientX, 5));

            Assert.Equal(expectedHue, RangeValue(cut, ".el-color-picker-panel__hue-input"));
            Assert.NotNull(value);
        }

        [Theory]
        [InlineData(6, "1")]
        [InlineData(70, "25")]
        [InlineData(140, "50")]
        [InlineData(279, "100")]
        public async Task AlphaBarUsesEpThumbWidthMapping(double clientX, string expectedAlpha)
        {
            StubPanelGeometry();
            var value = "rgba(255, 0, 0, 0)";
            var cut = Render<ElColorPickerPanel>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ShowAlpha, true)
                .Add(x => x.ValueChanged, next => value = next));

            await AlphaBar(cut).PointerDownAsync(Pointer(clientX, 5));

            Assert.Equal(expectedAlpha, RangeValue(cut, ".el-color-picker-panel__alpha-input"));
        }

        [Fact]
        public async Task DisabledPanelIgnoresPointerDragButEnabledOneStillMoves()
        {
            StubPanelGeometry();
            var disabledValue = "#ff0000";
            var enabledValue = "#ff0000";
            var disabled = Render<ElColorPickerPanel>(parameters => parameters
                .Add(x => x.Value, disabledValue)
                .Add(x => x.Disabled, true)
                .Add(x => x.ValueChanged, next => disabledValue = next));
            var enabled = Render<ElColorPickerPanel>(parameters => parameters
                .Add(x => x.Value, enabledValue)
                .Add(x => x.ValueChanged, next => enabledValue = next));

            await SvPanel(disabled).PointerDownAsync(Pointer(210, 45));
            await SvPanel(disabled).PointerMoveAsync(Pointer(210, 45));
            await SvPanel(disabled).PointerUpAsync(Pointer(210, 45));
            Assert.Equal("#ff0000", disabledValue);
            Assert.Equal(0, CaptureCalls(null));

            // 正向对照：同一份桩、同一段坐标，没禁用就必须动 —— 否则上面那条红不说明任何事
            await SvPanel(enabled).PointerDownAsync(Pointer(210, 45));
            Assert.Equal("75", RangeValue(enabled, ".el-color-picker-panel__saturation-input"));
            Assert.Equal(1, CaptureCalls(true));
        }

        [Fact]
        public async Task ClickFallbackStillWritesPositionWhenGeometryIsUnavailable()
        {
            // 不桩 elementViewportRect：Loose 模式返回 null，指针通道应当整段弃权，
            // 由紧随其后的 click 走 OffsetX 兜底（bUnit 与无宿主 JS 的那条路）。
            JSInterop.Mode = JSRuntimeMode.Loose;
            var value = "#409eff";
            var changes = new List<string>();
            var cut = Render<ElColorPickerPanel>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.OnChange, next => changes.Add(next)));

            var panel = SvPanel(cut);
            await panel.PointerDownAsync(Pointer(210, 45));
            await panel.PointerUpAsync(Pointer(210, 45));
            Assert.Empty(changes);
            Assert.Equal(0, CaptureCalls(null));

            await panel.ClickAsync(new MouseEventArgs { OffsetX = 210, OffsetY = 45 });
            Assert.Single(changes);
            // y=45 → value 75，圆点 top = 100-value = 25%
            Assert.Equal("left:75%;top:25%;", CursorStyle(cut));
        }

        [Fact]
        public void OverlayRangesHandThePointerToThePanel()
        {
            var cut = Render<ElColorPickerPanel>(parameters => parameters
                .Add(x => x.Value, "rgba(64, 158, 255, 0.5)")
                .Add(x => x.ShowAlpha, true));
            var disabled = Render<ElColorPickerPanel>(parameters => parameters
                .Add(x => x.Value, "rgba(64, 158, 255, 0.5)")
                .Add(x => x.ShowAlpha, true)
                .Add(x => x.Disabled, true));

            // 拖动通道换了宿主：四张透明 range 都不吃指针，但仍留在 Tab 序列里给键盘/读屏用
            foreach (var selector in new[]
            {
                ".el-color-picker-panel__saturation-input",
                ".el-color-picker-panel__value-input",
                ".el-color-picker-panel__hue-input",
                ".el-color-picker-panel__alpha-input"
            })
            {
                var style = cut.Find(selector).GetAttribute("style");
                Assert.Contains("pointer-events:none", style);
                Assert.DoesNotContain("pointer-events:auto", style);
                Assert.False(cut.Find(selector).HasAttribute("disabled"));
                // 正向对照：同一份选择器，禁用位必须真的能上锁 —— 否则上一条 False 不说明任何事
                Assert.True(disabled.Find(selector).HasAttribute("disabled"));
            }
        }
    }
}
