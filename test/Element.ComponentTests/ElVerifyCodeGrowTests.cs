using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElVerifyCodeGrow 组件测试（仅覆盖本组件）。
    /// 覆盖：默认渲染（图片 + 输入框）、边框开关、位置反转、圆角（Round/RoundRadius）、
    /// 噪点复杂度对 SVG 干扰元素数量的影响、@bind-Value 双向绑定、
    /// 点击刷新（CaptchaId 变化 + OnRefresh 事件）、ValidateAsync 校验与一次性销毁。
    /// </summary>
    public class ElVerifyCodeGrowTests : BunitContext
    {
        public ElVerifyCodeGrowTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static string SvgOf(IRenderedComponent<ElVerifyCodeGrow> cut)
        {
            var src = cut.Find(".el-verify-code-grow__img").GetAttribute("src");
            Assert.StartsWith("data:image/svg+xml;base64,", src);
            var bytes = Convert.FromBase64String(src.Substring("data:image/svg+xml;base64,".Length));
            return Encoding.UTF8.GetString(bytes);
        }

        [Fact]
        public void Default_RendersCaptchaImageAndInput()
        {
            var cut = Render<ElVerifyCodeGrow>();

            Assert.Contains("el-verify-code-grow", cut.Markup);
            Assert.Contains("is-bordered", cut.Markup);
            Assert.StartsWith("data:image/svg+xml;base64,", cut.Find(".el-verify-code-grow__img").GetAttribute("src"));
            Assert.NotNull(cut.Find(".el-verify-code-grow__field input"));
            Assert.False(string.IsNullOrEmpty(cut.Instance.CaptchaId));
            Assert.False(string.IsNullOrEmpty(cut.Instance.Answer));
        }

        [Fact]
        public void ShowBorder_False_RemovesBorderedClass()
        {
            var cut = Render<ElVerifyCodeGrow>(p => p.Add(x => x.ShowBorder, false));

            Assert.DoesNotContain("is-bordered", cut.Markup);
            Assert.DoesNotContain("border:1px", cut.Find(".el-verify-code-grow").GetAttribute("style"));
        }

        [Fact]
        public void Position_Right_AddsReverseClass()
        {
            var cut = Render<ElVerifyCodeGrow>(p => p.Add(x => x.Position, ElVerifyCodePosition.Right));

            Assert.Contains("is-reverse", cut.Markup);
        }

        [Fact]
        public void Round_And_RoundRadius_ControlRadius()
        {
            var round = Render<ElVerifyCodeGrow>(p => p.Add(x => x.Round, true));
            Assert.Contains("border-radius:4px", round.Find(".el-verify-code-grow").GetAttribute("style"));

            var custom = Render<ElVerifyCodeGrow>(p => p.Add(x => x.RoundRadius, 12));
            Assert.Contains("border-radius:12px", custom.Find(".el-verify-code-grow").GetAttribute("style"));
        }

        [Fact]
        public void NoiseComplexity_Zero_ProducesPlainSvg()
        {
            var plain = Render<ElVerifyCodeGrow>(p => p.Add(x => x.NoiseComplexity, 0));
            var plainSvg = SvgOf(plain);
            Assert.DoesNotContain("<line x1", plainSvg);
            Assert.DoesNotContain("<circle", plainSvg);
            Assert.DoesNotContain("<polygon", plainSvg);
            Assert.DoesNotContain("<path d", plainSvg);

            var noisy = Render<ElVerifyCodeGrow>(p => p.Add(x => x.NoiseComplexity, 5));
            var noisySvg = SvgOf(noisy);
            Assert.Contains("<line", noisySvg);
            Assert.Contains("<circle", noisySvg);
        }

        [Fact]
        public void NoiseComplexity_ScalesNoiseCount()
        {
            var low = SvgOf(Render<ElVerifyCodeGrow>(p => p.Add(x => x.NoiseComplexity, 2)));
            var high = SvgOf(Render<ElVerifyCodeGrow>(p => p.Add(x => x.NoiseComplexity, 10)));

            Assert.True(CountTags(low) < CountTags(high),
                $"低复杂度噪点数 {CountTags(low)} 应少于高复杂度 {CountTags(high)}");
        }

        private static int CountTags(string svg)
            => svg.Split("<line").Length - 1
             + svg.Split("<circle").Length - 1
             + svg.Split("<polygon").Length - 1
             + svg.Split("<path").Length - 1;

        [Fact]
        public void BindValue_InputPropagatesToValueChanged()
        {
            var changed = "";
            var cut = Render<ElVerifyCodeGrow>(p => p
                .Add(x => x.Value, "")
                .Add(x => x.ValueChanged, v => changed = v));

            cut.Find(".el-verify-code-grow__field input").Input("18");

            Assert.Equal("18", changed);
            Assert.Equal("18", cut.Instance.Value);
        }

        [Fact]
        public void ClickCaptcha_RaisesOnRefresh_AndChangesCaptchaId()
        {
            var refreshCount = 0;
            string lastId = null;
            var cut = Render<ElVerifyCodeGrow>(p => p
                .Add(x => x.OnRefresh, ctx =>
                {
                    refreshCount++;
                    lastId = ctx.CaptchaId;
                }));

            var oldId = cut.Instance.CaptchaId;
            // 初始化生成已触发过一次 OnRefresh，点击后应再触发一次
            Assert.Equal(1, refreshCount);
            cut.Find(".el-verify-code-grow__captcha").Click();

            Assert.Equal(2, refreshCount);
            Assert.Equal(cut.Instance.CaptchaId, lastId);
            Assert.NotEqual(oldId, cut.Instance.CaptchaId);
        }

        [Fact]
        public async System.Threading.Tasks.Task ValidateAsync_MatchesCurrentAnswer()
        {
            var cut = Render<ElVerifyCodeGrow>();
            var answer = cut.Instance.Answer;

            // 首次用正确答案校验 → 通过（与输入框 @bind-Value 填入的值同理）
            Assert.True(await cut.Instance.ValidateAsync(answer));

            // 校验即销毁（一次性）→ 同一验证码再次校验失败
            Assert.False(await cut.Instance.ValidateAsync(answer));
        }

        [Fact]
        public async System.Threading.Tasks.Task ValidateAsync_WrongAnswerReturnsFalse()
        {
            var cut = Render<ElVerifyCodeGrow>();

            Assert.False(await cut.Instance.ValidateAsync("999999"));
        }

        [Fact]
        public void Disabled_RendersDisabledInput()
        {
            var cut = Render<ElVerifyCodeGrow>(p => p.Add(x => x.Disabled, true));

            Assert.NotNull(cut.Find(".el-verify-code-grow__field input").GetAttribute("disabled"));
        }

        // 题面文本元素：<text x='..' y='..' font-family='Arial, sans-serif' font-size='..' ...>
        private static readonly Regex CaptchaTextRegex = new Regex(
            @"<text x='(?<x>[\d.]+)' y='(?<y>-?[\d.]+)'\s+font-family='Arial, sans-serif'\s+font-size='(?<fs>[\d.]+)'");

        /// <summary>
        /// 标题文本无论验证码宽高如何调整都必须居中：水平中心贴合 SVG 水平中线，垂直中心贴合垂直中线（不靠下）。
        /// </summary>
        [Theory]
        [InlineData(140, 50)]
        [InlineData(200, 60)]
        [InlineData(300, 80)]
        [InlineData(400, 100)]
        public void CaptchaText_IsCenteredForAnySize(int width, int height)
        {
            var cut = Render<ElVerifyCodeGrow>(p => p
                .Add(x => x.CaptchaWidth, width)
                .Add(x => x.CaptchaHeight, height));

            var matches = CaptchaTextRegex.Matches(SvgOf(cut));
            Assert.Equal(cut.Instance.Question.Length, matches.Count);

            // 每个字符以自身 x 为视觉中心（text-anchor=middle + 绕中心缩放），整行水平中心 ≈ (min+max)/2。
            var xs = matches.Select(m => double.Parse(m.Groups["x"].Value, CultureInfo.InvariantCulture)).ToList();
            var horizontalCenter = (xs.Min() + xs.Max()) / 2.0;
            Assert.True(Math.Abs(horizontalCenter - width / 2.0) <= 15,
                $"题面水平中心 {horizontalCenter:F1} 应接近 SVG 水平中线 {width / 2.0}（尺寸 {width}×{height}）");

            // 数字/大写字母的视觉中心约在基线上方 0.35em。
            var verticalCenters = matches.Select(m =>
                double.Parse(m.Groups["y"].Value, CultureInfo.InvariantCulture)
                - 0.35 * double.Parse(m.Groups["fs"].Value, CultureInfo.InvariantCulture)).ToList();
            var verticalCenter = verticalCenters.Average();
            Assert.True(Math.Abs(verticalCenter - height / 2.0) <= 8,
                $"题面垂直中心 {verticalCenter:F1} 应接近 SVG 垂直中线 {height / 2.0}（尺寸 {width}×{height}）");
        }

        [Fact]
        public void PlaceholderFontSize_EmitsCssVariable()
        {
            var cut = Render<ElVerifyCodeGrow>(p => p.Add(x => x.PlaceholderFontSize, 20));
            Assert.Contains("--evcg-placeholder-font-size:20px",
                cut.Find(".el-verify-code-grow__field input").GetAttribute("style"));

            var none = Render<ElVerifyCodeGrow>();
            var style = none.Find(".el-verify-code-grow__field input").GetAttribute("style") ?? string.Empty;
            Assert.DoesNotContain("--evcg-placeholder-font-size", style);
        }

        [Fact]
        public void FontSize_And_PlaceholderFontSize_AreIndependent()
        {
            var cut = Render<ElVerifyCodeGrow>(p => p
                .Add(x => x.FontSize, 12)
                .Add(x => x.PlaceholderFontSize, 20));

            var style = cut.Find(".el-verify-code-grow__field input").GetAttribute("style");
            Assert.Contains("font-size:12px", style);
            Assert.Contains("--evcg-placeholder-font-size:20px", style);
        }

        #region 倒计时进度条（定时刷新时图片底部宽度递减，刷新即重置）

        // 进度条只在 AutoRefreshInterval > 0 时出现；测试用很长的间隔（30s）避免定时器在用例内触发。
        private const int LongInterval = 30000;

        [Fact]
        public void ProgressBar_IsHiddenWithoutAutoRefresh()
        {
            var cut = Render<ElVerifyCodeGrow>();

            Assert.False(cut.Instance.IsCountdownVisible);
            Assert.Empty(cut.FindAll(".el-verify-code-grow__progress"));
        }

        [Fact]
        public void ProgressBar_IsRenderedWithAutoRefresh_UsingDefaults()
        {
            var cut = Render<ElVerifyCodeGrow>(p => p.Add(x => x.AutoRefreshInterval, LongInterval));

            Assert.True(cut.Instance.IsCountdownVisible);

            var trackStyle = cut.Find(".el-verify-code-grow__progress").GetAttribute("style");
            Assert.Contains("width:200px", trackStyle);   // 默认与验证码图片等宽
            Assert.Contains("height:3px", trackStyle);

            var barStyle = cut.Find(".el-verify-code-grow__progress-bar").GetAttribute("style");
            Assert.Contains("background:#409eff", barStyle);   // 默认取 EP --el-color-primary
            Assert.Contains($"animation:el-verify-code-grow-countdown {LongInterval}ms linear forwards", barStyle);
        }

        [Fact]
        public void ProgressBar_Color_Width_And_Height_AreConfigurable()
        {
            var cut = Render<ElVerifyCodeGrow>(p => p
                .Add(x => x.AutoRefreshInterval, LongInterval)
                .Add(x => x.ProgressColor, "#e6a23c")
                .Add(x => x.ProgressWidth, 120)
                .Add(x => x.ProgressHeight, 6));

            var trackStyle = cut.Find(".el-verify-code-grow__progress").GetAttribute("style");
            Assert.Contains("width:120px", trackStyle);
            Assert.Contains("height:6px", trackStyle);
            Assert.Contains("background:#e6a23c",
                cut.Find(".el-verify-code-grow__progress-bar").GetAttribute("style"));
        }

        [Fact]
        public void ProgressBar_WidthFollowsCaptchaWidthWhenNotSpecified()
        {
            var cut = Render<ElVerifyCodeGrow>(p => p
                .Add(x => x.AutoRefreshInterval, LongInterval)
                .Add(x => x.CaptchaWidth, 320));

            Assert.Contains("width:320px", cut.Find(".el-verify-code-grow__progress").GetAttribute("style"));
        }

        [Fact]
        public void ShowProgressBar_False_HidesCountdown()
        {
            var cut = Render<ElVerifyCodeGrow>(p => p
                .Add(x => x.AutoRefreshInterval, LongInterval)
                .Add(x => x.ShowProgressBar, false));

            Assert.Empty(cut.FindAll(".el-verify-code-grow__progress"));
        }

        /// <summary>
        /// 每次刷新都要换掉 CountdownKey——它是进度条元素的 @key，键变即元素重建，
        /// CSS 动画随之从头播放，视觉上就是「进度条重置为满格」。
        /// </summary>
        [Fact]
        public void CountdownKey_ChangesOnRefresh_SoAnimationRestarts()
        {
            var cut = Render<ElVerifyCodeGrow>(p => p.Add(x => x.AutoRefreshInterval, LongInterval));
            var before = cut.Instance.CountdownKey;

            cut.Find(".el-verify-code-grow__captcha").Click();

            Assert.NotEqual(before, cut.Instance.CountdownKey);
            Assert.EndsWith("|" + LongInterval, cut.Instance.CountdownKey);
            Assert.NotNull(cut.Find(".el-verify-code-grow__progress"));
        }

        #endregion
    }
}
