using Bunit;
using ElementPlus;
using ElementPlus.ControlConfigs;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElSwitchTests : BunitContext
    {
        public ElSwitchTests()
        {
            Services.AddElementPlusServices();
        }

        [Fact]
        public void DefaultBoolSwitchTogglesWithoutExplicitValues()
        {
            var value = false;
            var cut = Render<ElSwitch<bool>>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.Id, "enabled")
                .Add(x => x.ActiveText, "On")
                .Add(x => x.InactiveText, "Off"));

            var root = cut.Find(".el-switch");
            var input = cut.Find("input");

            Assert.Equal("switch", root.GetAttribute("role"));
            Assert.Equal("false", root.GetAttribute("aria-checked"));
            Assert.Equal("Off", cut.Find(".el-switch__label--left").TextContent.Trim());
            Assert.Equal("On", cut.Find(".el-switch__label--right").TextContent.Trim());
            Assert.Contains("is-active", cut.Find(".el-switch__label--left").ClassList);
            Assert.Equal("enabled", input.Id);
            Assert.Equal("False", input.GetAttribute("value"));
            Assert.False(input.HasAttribute("checked"));

            cut.Find(".el-switch__core").Click();

            Assert.True(value);
            Assert.Equal("true", cut.Find(".el-switch").GetAttribute("aria-checked"));
            Assert.Contains("is-checked", cut.Find(".el-switch").ClassList);
            Assert.True(cut.Find("input").HasAttribute("checked"));
            Assert.Contains("is-active", cut.Find(".el-switch__label--right").ClassList);
        }

        [Fact]
        public void CustomActiveInactiveValuesToggleAndEmitEvents()
        {
            var value = "off";
            var modelValue = string.Empty;
            var changed = 0;
            ElementChangeEventArgs<string> changing = null;
            var cut = Render<ElSwitch<string>>(parameters => parameters
                .Add(x => x.ActiveValue, "on")
                .Add(x => x.InactiveValue, "off")
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.ModelValueChanged, next => modelValue = next)
                .Add(x => x.ValueChanging, args => changing = args)
                .Add(x => x.OnChanged, _ => changed++));

            cut.Find(".el-switch__core").Click();

            Assert.Equal("on", value);
            Assert.Equal("on", modelValue);
            Assert.Equal(1, changed);
            Assert.NotNull(changing);
            Assert.Equal("off", changing.OldValue);
            Assert.Equal("on", changing.NewValue);
            Assert.Equal("on", cut.Find("input").GetAttribute("value"));
            Assert.Equal("on", cut.Find(".el-switch").GetAttribute("aria-valuetext"));
        }

        [Fact]
        public void LoadingSwitchDoesNotChangeValue()
        {
            var value = false;
            var cut = Render<ElSwitch<bool>>(parameters => parameters
                .Add(x => x.ActiveValue, true)
                .Add(x => x.InactiveValue, false)
                .Add(x => x.Value, value)
                .Add(x => x.Loading, true)
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-switch__core").Click();

            Assert.False(value);
            Assert.Contains("is-loading", cut.Find(".el-switch").ClassList);
            Assert.Equal("true", cut.Find(".el-switch").GetAttribute("aria-disabled"));
            Assert.Equal("true", cut.Find(".el-switch").GetAttribute("aria-busy"));
            Assert.Contains("el-icon-loading", cut.Find(".el-switch__loading").ClassList);
        }

        [Fact]
        public void BeforeChangeCanCancelSwitch()
        {
            var value = false;
            var cut = Render<ElSwitch<bool>>(parameters => parameters
                .Add(x => x.ActiveValue, true)
                .Add(x => x.InactiveValue, false)
                .Add(x => x.Value, value)
                .Add(x => x.BeforeChange, (_, _) => Task.FromResult(false))
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-switch__core").Click();

            Assert.False(value);
        }

        [Fact]
        public void BeforeChangeReceivesOldAndNewValuesBeforeCommit()
        {
            var value = false;
            bool? oldValue = null;
            bool? newValue = null;
            var cut = Render<ElSwitch<bool>>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.BeforeChange, (old, next) =>
                {
                    oldValue = old;
                    newValue = next;
                    return Task.FromResult(true);
                })
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-switch__core").Click();

            Assert.False(oldValue);
            Assert.True(newValue);
            Assert.True(value);
        }

        [Fact]
        public void KeyboardActivatesSwitch()
        {
            var value = false;
            var cut = Render<ElSwitch<bool>>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-switch").KeyDown(new KeyboardEventArgs { Key = "Enter" });

            Assert.True(value);

            cut.Find(".el-switch").KeyDown(new KeyboardEventArgs { Key = " " });

            Assert.False(value);
        }

        [Fact]
        public void ValueChangingCanCancelSwitch()
        {
            var value = false;
            var cut = Render<ElSwitch<bool>>(parameters => parameters
                .Add(x => x.ActiveValue, true)
                .Add(x => x.InactiveValue, false)
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanging, args => args.DisallowChange = true)
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-switch__core").Click();

            Assert.False(value);
        }

        [Fact]
        public void GeneratedSwitchAppliesAttributeConfiguration()
        {
            var model = new GeneratedSwitchModel
            {
                Enabled = false,
                State = "N"
            };

            var cut = Render<ElForm>(parameters => parameters
                .Add(x => x.EntityType, typeof(GeneratedSwitchModel))
                .Add(x => x.Value, model));

            var switches = cut.FindAll(".el-switch");
            Assert.Equal(2, switches.Count);
            Assert.Contains("No", cut.Markup);
            Assert.Contains("Yes", cut.Markup);
            Assert.Contains("Loading", cut.Markup);
            Assert.Contains("Ready", cut.Markup);
            Assert.Contains("is-loading", switches[0].ClassList);
            Assert.Contains("custom-loading", cut.Find(".el-switch__loading").ClassList);

            switches[0].KeyDown(new KeyboardEventArgs { Key = "Enter" });
            cut.FindAll(".el-switch__core")[1].Click();

            Assert.False(model.Enabled);
            Assert.Equal("Y", model.State);
        }

        #region 批次⑧：一个停靠点 + EP 焦点环

        /// <summary>
        /// 键盘可达性契约：根节点是唯一 Tab 停靠点（键盘处理挂在根节点上），
        /// 0×0 透明复选框必须退出 Tab 序 —— 否则一个控件两个停靠点，
        /// 浏览器还会给那个 0×0 的 input 画原生黑色焦点环（用户报的「外边的黑框」）。
        /// 判据成对写：只断言 input 不可达是空转的绿，必须同时证明根节点真的可达。
        /// </summary>
        [Fact]
        public void RootNodeIsTheOnlyTabStop()
        {
            var value = false;
            var cut = Render<ElSwitch<bool>>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next));

            Assert.Equal("0", cut.Find(".el-switch").GetAttribute("tabindex"));
            Assert.Equal("-1", cut.Find(".el-switch__input").GetAttribute("tabindex"));
        }

        /// <summary>禁用态：根节点退出 Tab 序（input 本来就在 -1），整控件不留停靠点。</summary>
        [Fact]
        public void DisabledSwitchLeavesNoTabStop()
        {
            var value = false;
            var cut = Render<ElSwitch<bool>>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.Disabled, true)
                .Add(x => x.ValueChanged, next => value = next));

            Assert.Equal("-1", cut.Find(".el-switch").GetAttribute("tabindex"));
            Assert.Equal("-1", cut.Find(".el-switch__input").GetAttribute("tabindex"));
        }

        /// <summary>
        /// 焦点环 CSS 契约（bUnit 没有布局引擎，量不到 computed outline，只能钉文本契约）：
        /// 消除原生黑环必须与替代环**成对**出现 —— 只写 outline:none 等于删掉可达性提示。
        /// EP 2.14.0 theme-chalk/src/switch.scss 的环是 2px solid switch-on-color + 1px offset，
        /// 画在 .el-switch__core 上（根节点没有任何 outline 规则）。
        /// </summary>
        [Fact]
        public void Guard_SwitchFocusRingSuppressesNativeRingAndPaintsEpRing()
        {
            var css = IndexCssSection("ElSwitch 焦点环对齐 EP", ".el-switch__input:focus-visible ~ .el-switch__core");

            Assert.Matches(@"\.el-switch:focus,\s*\.el-switch__input:focus\s*\{\s*outline:\s*none\s*;?\s*\}", css);
            Assert.Matches(@"\.el-switch:focus-visible \.el-switch__core[^{]*\{[^}]*outline:\s*2px solid var\(--el-switch-on-color", css);
            Assert.Matches(@"\.el-switch:focus-visible \.el-switch__core[^{]*\{[^}]*outline-offset:\s*1px", css);
        }

        /// <summary>
        /// 取 index.css 里从「marker」起始注释到「tail」那条规则闭合处为止的一段（已剥注释）。
        /// 必须**有界**截取：整段扫到文件末尾会把别处无关的 .el-switch 声明也算进来。
        /// </summary>
        private static string IndexCssSection(string marker, string tail)
        {
            var path = Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "css", "index.css");
            var text = File.ReadAllText(path);
            var at = text.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(at >= 0, $"index.css 中未找到「{marker}」样式段");

            var commentStart = text.LastIndexOf("/*", at, StringComparison.Ordinal);
            var anchor = text.IndexOf(tail, at, StringComparison.Ordinal);
            Assert.True(commentStart >= 0 && anchor > at, $"「{marker}」样式段不完整（找不到 {tail}）");
            var open = text.IndexOf('{', anchor);
            var stop = open >= 0 ? text.IndexOf('}', open) : -1;
            Assert.True(stop > anchor, $"「{marker}」样式段结尾规则没有闭合");

            return Regex.Replace(text.Substring(commentStart, stop - commentStart + 1), @"/\*[\s\S]*?\*/", string.Empty);
        }

        private static string RepoRoot([CallerFilePath] string file = null)
            => new FileInfo(file).Directory.Parent.Parent.FullName;

        #endregion

        #region 批次⑨：内联文字（InlinePrompt）轨道要容得下文字并让开圆钮

        /// <summary>
        /// DOM 契约（只钉「属性名」这一半，几何那一半在浏览器探针里量）：
        /// 内联文字态下轨道宽度必须写成 min-width（EP 2.14.0 switch.scss:151 同口径），
        /// 轨道才能随「OFF」这类长文字撑开；钉死 width:40px 时 24px 的文字右对齐后左缘压进
        /// 16px 圆钮底下（实测越界 8px，见 Temp/diag-batch9-switch5.log D1 —— 用户报的「OFF 被遮挡」）。
        /// 判据成对写：同时断言非内联态仍是 width，否则「属性名换了」这条变异就没人管。
        /// bUnit 没有布局引擎，这里再强调一次：min-width 写错成 width 在 bUnit 里全绿。
        /// </summary>
        [Fact]
        public void InlinePromptCoreUsesMinWidthWhilePlainCoreKeepsWidth()
        {
            var value = false;
            var inline = Render<ElSwitch<bool>>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.InlinePrompt, true)
                .Add(x => x.ActiveText, "ON")
                .Add(x => x.InactiveText, "OFF"));

            Assert.Contains("is-inline-prompt", inline.Find(".el-switch").ClassName);
            var inlineStyle = inline.Find(".el-switch__core").GetAttribute("style");
            Assert.Contains("min-width: 40px", inlineStyle);
            // 「min-width: 40px」里含「width: 40px」子串，必须用否定后视把属性名本身钉住
            Assert.DoesNotMatch(@"(?<![-\w])width:\s*40px", inlineStyle);
            // 文字必须待在 .el-switch__inner 里 —— 让位内边距就是写在这个元素上的
            Assert.Equal("OFF", inline.Find(".el-switch__inner").TextContent.Trim());

            var plain = Render<ElSwitch<bool>>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.ActiveText, "ON")
                .Add(x => x.InactiveText, "OFF"));

            var plainStyle = plain.Find(".el-switch__core").GetAttribute("style");
            Assert.DoesNotContain("is-inline-prompt", plain.Find(".el-switch").ClassName);
            Assert.Matches(@"(?<![-\w])width:\s*40px", plainStyle);
            Assert.DoesNotContain("min-width", plainStyle);
            Assert.Empty(plain.FindAll(".el-switch__inner"));
        }

        /// <summary>
        /// CSS 文本契约：让位内边距必须**成对**写全（未选让左 18px / 已选让右 18px），
        /// 且轨道自身不再留 6px、宽度改回 auto —— 否则文字与圆钮又开始争同一块盒。
        /// 18 = 圆钮 button-size 16 + 1px 内偏移 + 1px 余量。
        /// 末尾那条负向判据（段内不得再有 flex-end/flex-start 推文字）的正向对照是探针 D1：
        /// 把旧的 flex-end 注回页面，OFF 行立刻越界 8px 转红。
        /// </summary>
        [Fact]
        public void Guard_SwitchInlinePromptLeavesRoomForKnobOnBothSides()
        {
            var css = InlinePromptCssSection();

            Assert.Matches(@"\.el-switch\.is-inline-prompt \.el-switch__core\{[^}]*width:\s*auto", css);
            Assert.Matches(@"\.el-switch\.is-inline-prompt \.el-switch__core\{[^}]*padding:\s*0(?![.\d])", css);
            Assert.Matches(@"\.el-switch\.is-inline-prompt:not\(\.is-checked\) \.el-switch__inner\{[^}]*padding:\s*0 6px 0 18px", css);
            Assert.Matches(@"\.el-switch\.is-inline-prompt\.is-checked \.el-switch__inner\{[^}]*padding:\s*0 18px 0 6px", css);
            Assert.DoesNotMatch(@"justify-content:\s*flex-(end|start)", css);
        }

        /// <summary>
        /// 取「开关内联文字 (InlinePrompt)」这一段：从该标记起到下一段（图片预览层）的起始注释为止，
        /// 再剥掉注释。必须**有界** —— 扫到文件末尾会把别处无关的 .el-switch 声明也算进来，
        /// 那条负向判据就成了随时会被别人踩红的写法。
        /// </summary>
        private static string InlinePromptCssSection()
        {
            var path = Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "css", "index.css");
            var text = File.ReadAllText(path);
            const string marker = "开关内联文字 (InlinePrompt)";
            const string nextSection = "/* ==== 图片预览层 teleport";

            var at = text.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(at >= 0, "index.css 中未找到「开关内联文字 (InlinePrompt)」样式段");
            // 必须从该段**起始注释的「/*」**切起：标记词本身在注释体里，
            // 从标记词处切开就丢了开括号，剥注释的正则匹配不上，注释里的旧写法会被当成代码命中判据。
            var start = text.LastIndexOf("/*", at, StringComparison.Ordinal);
            Assert.True(start >= 0, "「开关内联文字」段没有起始注释，无法定界");
            var stop = text.IndexOf(nextSection, at, StringComparison.Ordinal);
            Assert.True(stop > at, "「开关内联文字」段后面没有「图片预览层 teleport」段，段界不成立");

            return Regex.Replace(text.Substring(start, stop - start), @"/\*[\s\S]*?\*/", string.Empty);
        }

        #endregion

        private class GeneratedSwitchModel
        {
            [Switch(ActiveText = "Yes", InactiveText = "No", Loading = true, LoadingIcon = "custom-loading")]
            public bool Enabled { get; set; }

            [Switch(ActiveText = "Loading", InactiveText = "Ready", ActiveValue = "Y", InactiveValue = "N")]
            public string State { get; set; }
        }
    }
}
