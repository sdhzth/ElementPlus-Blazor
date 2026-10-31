using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElWaitingPopupGrow 组件测试（仅覆盖本组件）。
    /// 覆盖：默认隐藏、Visible 渲染遮罩+卡片、Show/Hide 命令式显隐与事件广播、
    /// MaskClosable（点遮罩取消 / 默认不取消）、取消按钮显隐与「先 OnCancel 再关闭」、
    /// Round / RoundRadius 圆角、BackgroundColor 背景色、
    /// TitleColor / MessageColor / SpinnerColor 三色 CSS 变量、ZIndex 分层、ChildContent 插槽，
    /// 以及取消按钮（本就是一个 ElButton）的 Type / Size / Plain / Round / Color / Dark 透传与位置、文字。
    /// 以及 ShowDelay 阈值：到点才画 / 阈值内被 Hide 则整块不画 / 关闭不受阈值影响 / 命令式与声明式两条路都吃阈值（各配同前提正向对照）。
    /// </summary>
    public class ElWaitingPopupGrowTests : BunitContext
    {
        public ElWaitingPopupGrowTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void Default_IsHidden()
        {
            var cut = Render<ElWaitingPopupGrow>();

            Assert.False(cut.Instance.IsVisible);
            Assert.Empty(cut.FindAll(".el-waiting-popup-grow__wrapper"));
            Assert.Empty(cut.FindAll(".el-overlay"));
        }

        [Fact]
        public void Visible_True_RendersOverlaySpinnerTitleMessageAndCancel()
        {
            var cut = Render<ElWaitingPopupGrow>(p => p.Add(x => x.Visible, true));

            Assert.Single(cut.FindAll(".el-overlay"));
            Assert.Single(cut.FindAll(".el-waiting-popup-grow__wrapper"));
            Assert.Single(cut.FindAll(".el-waiting-popup-grow__spinner .ewpg-circular"));
            Assert.Equal("处理中...", cut.Find(".el-waiting-popup-grow__title").TextContent);
            Assert.Equal("请稍候", cut.Find(".el-waiting-popup-grow__message").TextContent);
            Assert.NotNull(cut.Find(".el-waiting-popup-grow__actions button"));
        }

        [Fact]
        public void ShowCancelButton_False_HidesActions()
        {
            var cut = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.ShowCancelButton, false));

            Assert.Empty(cut.FindAll(".el-waiting-popup-grow__actions"));
        }

        [Fact]
        public async Task ShowAndHide_ToggleVisibilityAndBroadcastEvents()
        {
            bool? last = null;
            var cut = Render<ElWaitingPopupGrow>(p => p.Add(x => x.OnVisibleChange, v => last = v));

            await cut.Instance.Show();
            Assert.True(cut.Instance.IsVisible);
            cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".el-waiting-popup-grow__wrapper")), timeout: TestWait.Timeout);
            Assert.True(last);

            await cut.Instance.Hide();
            Assert.False(cut.Instance.IsVisible);
            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".el-waiting-popup-grow__wrapper")), timeout: TestWait.Timeout);
            Assert.False(last);
        }

        [Fact]
        public async Task VisibleChanged_IsRaisedOnImperativeShow()
        {
            bool? changed = null;
            var cut = Render<ElWaitingPopupGrow>(p => p.Add(x => x.VisibleChanged, v => changed = v));

            await cut.Instance.Show();

            Assert.True(changed);
        }

        [Fact]
        public async Task MaskClick_DoesNotCloseByDefault()
        {
            var cancelCount = 0;
            var cut = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.OnCancel, () => cancelCount++));

            cut.Find(".el-overlay").Click();

            Assert.Equal(0, cancelCount);
            Assert.True(cut.Instance.IsVisible);
        }

        [Fact]
        public async Task MaskClosable_MaskClickRaisesCancelThenCloses()
        {
            var cancelCount = 0;
            var cut = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.MaskClosable, true)
                .Add(x => x.OnCancel, () => cancelCount++));

            cut.Find(".el-overlay").Click();

            Assert.Equal(1, cancelCount);
            cut.WaitForAssertion(() => Assert.False(cut.Instance.IsVisible), timeout: TestWait.Timeout);
        }

        [Fact]
        public async Task CancelButton_RaisesOnCancelThenCloses()
        {
            var cancelCount = 0;
            var cut = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.OnCancel, () => cancelCount++));

            cut.Find(".el-waiting-popup-grow__actions button").Click();

            Assert.Equal(1, cancelCount);
            cut.WaitForAssertion(() => Assert.False(cut.Instance.IsVisible), timeout: TestWait.Timeout);
        }

        [Fact]
        public void Round_And_RoundRadius_ControlCardRadius()
        {
            var square = Render<ElWaitingPopupGrow>(p => p.Add(x => x.Visible, true).Add(x => x.Round, false));
            Assert.Contains("border-radius:0px", square.Find(".el-waiting-popup-grow__content").GetAttribute("style"));

            var rounded = Render<ElWaitingPopupGrow>(p => p.Add(x => x.Visible, true));
            Assert.Contains("border-radius:4px", rounded.Find(".el-waiting-popup-grow__content").GetAttribute("style"));

            var custom = Render<ElWaitingPopupGrow>(p => p.Add(x => x.Visible, true).Add(x => x.RoundRadius, 16));
            Assert.Contains("border-radius:16px", custom.Find(".el-waiting-popup-grow__content").GetAttribute("style"));
        }

        [Fact]
        public void BackgroundColor_AppliesToCard()
        {
            var cut = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.BackgroundColor, "#123456"));

            Assert.Contains("background:#123456", cut.Find(".el-waiting-popup-grow__content").GetAttribute("style"));
        }

        [Fact]
        public void TitleColor_MessageColor_SpinnerColor_EmitCssVariables()
        {
            var cut = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.TitleColor, "#ffffff")
                .Add(x => x.MessageColor, "#e6f0ff")
                .Add(x => x.SpinnerColor, "#ffd666"));

            var style = cut.Find(".el-waiting-popup-grow__content").GetAttribute("style");
            Assert.Contains("--ewpg-title-color:#ffffff", style);
            Assert.Contains("--ewpg-message-color:#e6f0ff", style);
            Assert.Contains("--ewpg-spinner-color:#ffd666", style);
        }

        [Fact]
        public void ColorParameters_Unset_EmitNoCssVariables()
        {
            var cut = Render<ElWaitingPopupGrow>(p => p.Add(x => x.Visible, true));

            var style = cut.Find(".el-waiting-popup-grow__content").GetAttribute("style");
            Assert.DoesNotContain("--ewpg-title-color", style);
            Assert.DoesNotContain("--ewpg-message-color", style);
            Assert.DoesNotContain("--ewpg-spinner-color", style);
        }

        [Fact]
        public void ShowCancelButton_ToggledByParameter()
        {
            var hidden = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.ShowCancelButton, false));
            Assert.Empty(hidden.FindAll(".el-waiting-popup-grow__actions"));

            var shown = Render<ElWaitingPopupGrow>(p => p.Add(x => x.Visible, true));
            Assert.Single(shown.FindAll(".el-waiting-popup-grow__actions"));
        }

        [Fact]
        public void ZIndex_MaskAndCardAreLayered()
        {
            var cut = Render<ElWaitingPopupGrow>(p => p.Add(x => x.Visible, true).Add(x => x.ZIndex, 3000));

            Assert.Contains("z-index:3000", cut.Find(".el-overlay").GetAttribute("style"));
            Assert.Contains("z-index:3001", cut.Find(".el-waiting-popup-grow__wrapper").GetAttribute("style"));
        }

        [Fact]
        public void ChildContent_RendersExtraSlot()
        {
            var cut = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.ChildContent, "<span class=\"probe-extra\">进度 50%</span>"));

            Assert.Single(cut.FindAll(".el-waiting-popup-grow__extra"));
            Assert.Contains("probe-extra", cut.Markup);
        }

        [Fact]
        public void CustomTexts_AreRendered()
        {
            var cut = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.Title, "正在导出")
                .Add(x => x.Message, "大约需要 10 秒")
                .Add(x => x.CancelText, "中止"));

            Assert.Equal("正在导出", cut.Find(".el-waiting-popup-grow__title").TextContent);
            Assert.Equal("大约需要 10 秒", cut.Find(".el-waiting-popup-grow__message").TextContent);
            Assert.Contains("中止", cut.Find(".el-waiting-popup-grow__actions button").TextContent);
        }

        [Fact]
        public void EmptyTitleOrMessage_AreSkipped()
        {
            var cut = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.Title, "")
                .Add(x => x.Message, null));

            Assert.Empty(cut.FindAll(".el-waiting-popup-grow__title"));
            Assert.Empty(cut.FindAll(".el-waiting-popup-grow__message"));
        }

        #region 取消按钮：就是一个 ElButton，可设颜色 / 尺寸 / 位置 / 文字

        [Fact]
        public void CancelButton_IsAnElButtonWithDefaults()
        {
            var cut = Render<ElWaitingPopupGrow>(p => p.Add(x => x.Visible, true));

            var button = cut.Find(".el-waiting-popup-grow__actions button");
            var cls = button.GetAttribute("class");

            Assert.Contains("el-button", cls);
            Assert.DoesNotContain("el-button--", cls);   // Default 类型不输出 el-button--xxx
            Assert.Equal("取消", button.TextContent.Trim());
        }

        [Fact]
        public void CancelButtonType_And_Size_MapToButtonClasses()
        {
            var cut = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.CancelButtonType, ButtonType.Primary)
                .Add(x => x.CancelButtonSize, ButtonSize.Small));

            var cls = cut.Find(".el-waiting-popup-grow__actions button").GetAttribute("class");

            Assert.Contains("el-button--primary", cls);
            Assert.Contains("el-button--small", cls);

            var large = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.CancelButtonSize, ButtonSize.Large));
            Assert.Contains("el-button--large",
                large.Find(".el-waiting-popup-grow__actions button").GetAttribute("class"));
        }

        [Fact]
        public void CancelButtonPlain_And_Round_MapToButtonClasses()
        {
            var cut = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.CancelButtonPlain, true)
                .Add(x => x.CancelButtonRound, true));

            var cls = cut.Find(".el-waiting-popup-grow__actions button").GetAttribute("class");

            Assert.Contains("is-plain", cls);
            Assert.Contains("is-round", cls);
        }

        /// <summary>
        /// 自定义颜色透传给 ElButton.Color —— 渲染结果是按钮上的 CSS 变量（浅底 + 彩色字），
        /// 与 ElButton 自身完全一致；<c>Dark</c> 切换成实心底 + 白字。
        /// </summary>
        [Fact]
        public void CancelButtonColor_FlowsThroughToElButtonCssVariables()
        {
            var light = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.CancelButtonColor, "#f56c6c"));
            var lightStyle = light.Find(".el-waiting-popup-grow__actions button").GetAttribute("style");

            Assert.Contains("--el-button-text-color:#f56c6c", lightStyle);
            Assert.Contains("--el-button-border-color:#f56c6c", lightStyle);

            var dark = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.CancelButtonColor, "#f56c6c")
                .Add(x => x.CancelButtonDark, true));
            var darkStyle = dark.Find(".el-waiting-popup-grow__actions button").GetAttribute("style");

            Assert.Contains("--el-button-bg-color:#f56c6c", darkStyle);
            Assert.Contains("--el-button-text-color:var(--el-color-white)", darkStyle);

            // 不设颜色时不输出任何自定义色变量，走 Type 的主题配色
            var none = Render<ElWaitingPopupGrow>(p => p.Add(x => x.Visible, true));
            var noneStyle = none.Find(".el-waiting-popup-grow__actions button").GetAttribute("style") ?? string.Empty;
            Assert.DoesNotContain("--el-button-text-color", noneStyle);
        }

        [Fact]
        public void CancelButtonPosition_EmitsAlignVariable()
        {
            var center = Render<ElWaitingPopupGrow>(p => p.Add(x => x.Visible, true));
            var centerStyle = center.Find(".el-waiting-popup-grow__actions").GetAttribute("style") ?? string.Empty;
            Assert.DoesNotContain("--ewpg-actions-align", centerStyle);   // 默认居中不输出变量

            var left = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.CancelButtonPosition, ElWaitingPopupGrowActionAlign.Left));
            Assert.Contains("--ewpg-actions-align:left",
                left.Find(".el-waiting-popup-grow__actions").GetAttribute("style"));

            var right = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.CancelButtonPosition, ElWaitingPopupGrowActionAlign.Right));
            Assert.Contains("--ewpg-actions-align:right",
                right.Find(".el-waiting-popup-grow__actions").GetAttribute("style"));
        }

        [Fact]
        public void CancelButton_Text_IsConfigurableAndClickStillCancels()
        {
            var cancelled = 0;
            var cut = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.CancelText, "停止导出")
                .Add(x => x.OnCancel, () => cancelled++));

            var button = cut.Find(".el-waiting-popup-grow__actions button");
            Assert.Contains("停止导出", button.TextContent);

            button.Click();

            Assert.Equal(1, cancelled);
            Assert.False(cut.Instance.IsVisible);
        }

        #endregion

        #region ShowDelay：短于一档延迟的请求，整块一个节点都不画

        /// <summary>
        /// 正向对照：<c>ShowDelay</c> 默认 0（= 本参数加入前的行为），Show 之后同步就有浮层。
        /// 没有这一条，下面「延迟期间没浮层」的负向断言可能只是因为压根没接上计时器。
        /// </summary>
        [Fact]
        public async Task ShowDelay_Unset_PaintsSynchronously()
        {
            var cut = Render<ElWaitingPopupGrow>();

            await cut.Instance.Show();

            Assert.True(cut.Instance.IsVisible);
            Assert.True(cut.Instance.IsPainted);
            Assert.Single(cut.FindAll(".el-waiting-popup-grow__wrapper"));
            Assert.Single(cut.FindAll(".el-overlay"));
        }

        /// <summary>
        /// 到点才画：请求活得比 ShowDelay 长时，浮层照旧出现（这一条把「没出现＝尺子坏了」和
        /// 「没出现＝阈值生效」区分开）。
        /// </summary>
        [Fact]
        public async Task ShowDelay_ElapsedThenPaints()
        {
            var cut = Render<ElWaitingPopupGrow>(p => p.Add(x => x.ShowDelay, 50));

            await cut.Instance.Show();
            Assert.False(cut.Instance.IsPainted);
            Assert.Empty(cut.FindAll(".el-waiting-popup-grow__wrapper"));

            cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".el-waiting-popup-grow__wrapper")), timeout: TestWait.Timeout);
            Assert.True(cut.Instance.IsPainted);
        }

        /// <summary>阈值内被 Hide ⇒ 一次都没画过（遮罩也不出现），不是「画了又收」。</summary>
        [Fact]
        public async Task ShowDelay_HideBeforeElapsed_NeverPaints()
        {
            var cut = Render<ElWaitingPopupGrow>(p => p.Add(x => x.ShowDelay, 300));

            await cut.Instance.Show();
            await cut.Instance.Hide();

            await Task.Delay(500);

            Assert.False(cut.Instance.IsVisible);
            Assert.False(cut.Instance.IsPainted);
            Assert.Empty(cut.FindAll(".el-waiting-popup-grow__wrapper"));
            Assert.Empty(cut.FindAll(".el-overlay"));
        }

        /// <summary>取消计时后重开：旧那把计时器不能再来补画一次，只有新那把说了算。</summary>
        [Fact]
        public async Task ShowDelay_RestartAfterHide_UsesFreshTimerOnly()
        {
            var cut = Render<ElWaitingPopupGrow>(p => p.Add(x => x.ShowDelay, 300));

            await cut.Instance.Show();
            await cut.Instance.Hide();
            await cut.Instance.Show();
            await Task.Delay(120);            // 旧计时（若没取消）此刻早已到点并补画 ⇒ 这一句是它的探针
            Assert.Empty(cut.FindAll(".el-waiting-popup-grow__wrapper"));

            cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".el-waiting-popup-grow__wrapper")), timeout: TestWait.Timeout);

            await cut.Instance.Hide();
            Assert.Empty(cut.FindAll(".el-waiting-popup-grow__wrapper"));
        }

        /// <summary>已经浮出来的再关闭是立即关闭，ShowDelay 只管「出不出来」，不管「收不收」。</summary>
        [Fact]
        public async Task ShowDelay_HideAfterPaint_IsImmediate()
        {
            var cut = Render<ElWaitingPopupGrow>(p => p.Add(x => x.ShowDelay, 50));

            await cut.Instance.Show();
            cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".el-waiting-popup-grow__wrapper")), timeout: TestWait.Timeout);

            await cut.Instance.Hide();

            Assert.False(cut.Instance.IsPainted);
            Assert.Empty(cut.FindAll(".el-waiting-popup-grow__wrapper"));
        }

        /// <summary>声明式 <c>@bind-Visible</c> 走的是 OnParametersSetAsync 那条路，也要吃同一个阈值。</summary>
        [Fact]
        public void ShowDelay_DeclarativeVisible_HonoursThreshold()
        {
            var cut = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.ShowDelay, 300)
                .Add(x => x.Visible, true));

            Assert.True(cut.Instance.IsVisible);
            Assert.Empty(cut.FindAll(".el-waiting-popup-grow__wrapper"));
        }

        /// <summary>声明式的对照：同一条路径 ShowDelay=0 时首帧就有浮层（防「这条路根本没接阈值」的假绿）。</summary>
        [Fact]
        public void ShowDelay_DeclarativeVisible_ZeroPaintsAtOnce()
        {
            var cut = Render<ElWaitingPopupGrow>(p => p
                .Add(x => x.ShowDelay, 0)
                .Add(x => x.Visible, true));

            Assert.Single(cut.FindAll(".el-waiting-popup-grow__wrapper"));
        }

        #endregion
    }
}
