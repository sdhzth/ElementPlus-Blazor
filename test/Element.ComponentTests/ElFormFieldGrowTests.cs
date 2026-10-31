using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElFormFieldGrow 组件测试（仅覆盖本组件，不跑全量）。
    /// 覆盖：类名构建、三种浮动模式、四种变体、三种尺寸、禁用/只读/校验失败状态、
    /// label for 关联、必填星号、辅助/错误文本优先级、前后缀插槽、宽度归一化、
    /// 级联上下文下发给子组件，以及**浏览器兼容性守卫**（样式段禁用新特性清单）。
    /// </summary>
    public class ElFormFieldGrowTests : BunitContext
    {
        public ElFormFieldGrowTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        // ------------------------------------------------------------------ 测试夹具

        /// <summary>探针子组件：捕获级联到内容区的表单字段上下文。</summary>
        public class ContextProbe : ComponentBase
        {
            [CascadingParameter]
            public IElFormFieldContext FieldContext { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "probe");
                builder.AddContent(2, FieldContext == null
                    ? "null"
                    : (FieldContext.AllowFloatingLabel ? "allow" : "deny"));
                builder.CloseElement();
            }
        }

        private static readonly RenderFragment ProbeFragment = builder =>
        {
            builder.OpenComponent<ContextProbe>(0);
            builder.CloseComponent();
        };

        private static RenderFragment InputFragment(string placeholder = " ") => builder =>
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "class", "el-form-field__control");
            builder.AddAttribute(2, "placeholder", placeholder);
            builder.CloseElement();
        };

        private static RenderFragment SpanFragment(string text) => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddContent(1, text);
            builder.CloseElement();
        };

        // ------------------------------------------------------------------ 基础结构

        [Fact]
        public void EmptyField_RendersRootAndContentWithoutLabel()
        {
            var cut = Render<ElFormFieldGrow>();

            Assert.Contains("el-form-field", cut.Markup);
            Assert.Contains("el-form-field__content", cut.Markup);
            Assert.Empty(cut.FindAll(".el-form-field__label"));
            Assert.Empty(cut.FindAll(".el-form-field__helper"));
        }

        [Fact]
        public void Text_RendersLabelText()
        {
            var cut = Render<ElFormFieldGrow>(p => p.Add(c => c.Text, "电子邮箱"));

            var label = cut.Find(".el-form-field__label");
            Assert.Contains("电子邮箱", label.TextContent);
            Assert.Equal("电子邮箱", cut.Instance.EffectiveText);
        }

        [Fact]
        public void LabelAlias_UsedWhenTextIsEmpty()
        {
            var cut = Render<ElFormFieldGrow>(p => p.Add(c => c.Label, "别名标签"));

            Assert.Equal("别名标签", cut.Instance.EffectiveText);
            Assert.Contains("别名标签", cut.Find(".el-form-field__label").TextContent);
        }

        [Fact]
        public void Text_TakesPrecedenceOverLabelAlias()
        {
            var cut = Render<ElFormFieldGrow>(p => p
                .Add(c => c.Text, "主标签")
                .Add(c => c.Label, "别名标签"));

            Assert.Equal("主标签", cut.Instance.EffectiveText);
        }

        [Fact]
        public void TextTemplate_ReplacesPlainText()
        {
            var cut = Render<ElFormFieldGrow>(p => p
                .Add(c => c.Text, "被替换的文本")
                .Add(c => c.TextTemplate, SpanFragment("模板内容")));

            var label = cut.Find(".el-form-field__label");
            Assert.Contains("模板内容", label.TextContent);
            Assert.DoesNotContain("被替换的文本", label.TextContent);
        }

        [Fact]
        public void Component_RendersLabelForAttribute()
        {
            var cut = Render<ElFormFieldGrow>(p => p
                .Add(c => c.Text, "姓名")
                .Add(c => c.Component, "  name-input  "));

            Assert.Equal("name-input", cut.Find(".el-form-field__label").GetAttribute("for"));
        }

        [Fact]
        public void WithoutComponent_OmitsLabelForAttribute()
        {
            var cut = Render<ElFormFieldGrow>(p => p.Add(c => c.Text, "姓名"));

            Assert.Null(cut.Find(".el-form-field__label").GetAttribute("for"));
        }

        [Fact]
        public void Required_AppendsAsterisk()
        {
            var cut = Render<ElFormFieldGrow>(p => p
                .Add(c => c.Text, "姓名")
                .Add(c => c.Required, true));

            var star = cut.Find(".el-form-field__required");
            Assert.Equal("*", star.TextContent);
            Assert.Contains("el-form-field--required", cut.Find("div.el-form-field").ClassList);
        }

        // ------------------------------------------------------------------ 变体与尺寸

        [Theory]
        [InlineData(ElFormFieldVariant.Outlined, "el-form-field--outlined")]
        [InlineData(ElFormFieldVariant.Filled, "el-form-field--filled")]
        [InlineData(ElFormFieldVariant.Flat, "el-form-field--flat")]
        [InlineData(ElFormFieldVariant.Text, "el-form-field--text")]
        public void Variant_MapsToModifierClass(ElFormFieldVariant variant, string expected)
        {
            var cut = Render<ElFormFieldGrow>(p => p.Add(c => c.Variant, variant));

            Assert.Contains(expected, cut.Instance.BuildCssClass());
        }

        [Fact]
        public void Variant_DefaultsToOutlined()
        {
            var cut = Render<ElFormFieldGrow>();

            Assert.Contains("el-form-field--outlined", cut.Instance.BuildCssClass());
        }

        [Theory]
        [InlineData(ElFormFieldSize.Small, "el-form-field--size-small")]
        [InlineData(ElFormFieldSize.Default, "el-form-field--size-default")]
        [InlineData(ElFormFieldSize.Large, "el-form-field--size-large")]
        public void Size_MapsToModifierClass(ElFormFieldSize size, string expected)
        {
            var cut = Render<ElFormFieldGrow>(p => p.Add(c => c.Size, size));

            Assert.Contains(expected, cut.Instance.BuildCssClass());
        }

        [Fact]
        public void Inline_AddsInlineClass()
        {
            var cut = Render<ElFormFieldGrow>(p => p.Add(c => c.Inline, true));

            Assert.Contains("el-form-field--inline", cut.Find("div.el-form-field").ClassList);
        }

        [Fact]
        public void KeepControlChrome_AddsKeepControlClass()
        {
            var cut = Render<ElFormFieldGrow>(p => p.Add(c => c.KeepControlChrome, true));

            Assert.Contains("el-form-field--keep-control", cut.Find("div.el-form-field").ClassList);
        }

        // ------------------------------------------------------------------ 浮动模式

        [Fact]
        public void DefaultFloat_IsAuto()
        {
            var cut = Render<ElFormFieldGrow>(p => p.Add(c => c.Text, "标签"));

            Assert.Equal(ElFormFieldFloat.Auto, cut.Instance.EffectiveFloat);
            Assert.Contains("el-form-field--float-auto", cut.Instance.BuildCssClass());
        }

        [Fact]
        public void HasValue_ForcesAlwaysFloat()
        {
            var cut = Render<ElFormFieldGrow>(p => p
                .Add(c => c.Text, "标签")
                .Add(c => c.HasValue, true));

            Assert.Equal(ElFormFieldFloat.Always, cut.Instance.EffectiveFloat);
            Assert.Contains("el-form-field--float-always", cut.Instance.BuildCssClass());
        }

        [Fact]
        public void AllowFloatingLabelFalse_ForcesAlwaysFloat()
        {
            var cut = Render<ElFormFieldGrow>(p => p
                .Add(c => c.Text, "标签")
                .Add(c => c.AllowFloatingLabel, false));

            Assert.Equal(ElFormFieldFloat.Always, cut.Instance.EffectiveFloat);
        }

        [Fact]
        public void FloatAlways_WinsOverAllowFloatingLabelFalse()
        {
            var cut = Render<ElFormFieldGrow>(p => p
                .Add(c => c.AllowFloatingLabel, false)
                .Add(c => c.Float, ElFormFieldFloat.Always));

            Assert.Equal(ElFormFieldFloat.Always, cut.Instance.EffectiveFloat);
        }

        [Fact]
        public void FloatNever_BeatsBothHasValueAndAllowFloatingLabel()
        {
            var cut = Render<ElFormFieldGrow>(p => p
                .Add(c => c.Text, "标签")
                .Add(c => c.HasValue, true)
                .Add(c => c.AllowFloatingLabel, false)
                .Add(c => c.Float, ElFormFieldFloat.Never));

            Assert.Equal(ElFormFieldFloat.Never, cut.Instance.EffectiveFloat);
            Assert.Contains("el-form-field--float-never", cut.Instance.BuildCssClass());
        }

        [Fact]
        public void FloatModes_AreMutuallyExclusiveModifierClasses()
        {
            var auto = Render<ElFormFieldGrow>(p => p.Add(c => c.Float, ElFormFieldFloat.Auto));
            var always = Render<ElFormFieldGrow>(p => p.Add(c => c.Float, ElFormFieldFloat.Always));
            var never = Render<ElFormFieldGrow>(p => p.Add(c => c.Float, ElFormFieldFloat.Never));

            Assert.Contains("el-form-field--float-auto", auto.Instance.BuildCssClass());
            Assert.DoesNotContain("--float-always", auto.Instance.BuildCssClass());
            Assert.Contains("el-form-field--float-always", always.Instance.BuildCssClass());
            Assert.DoesNotContain("--float-auto", always.Instance.BuildCssClass());
            Assert.Contains("el-form-field--float-never", never.Instance.BuildCssClass());
            Assert.DoesNotContain("--float-auto", never.Instance.BuildCssClass());
        }

        // ------------------------------------------------------------------ 状态

        [Fact]
        public void Disabled_AddsDisabledClass()
        {
            var cut = Render<ElFormFieldGrow>(p => p.Add(c => c.Disabled, true));

            Assert.True(cut.Instance.IsDisabled);
            Assert.Contains("el-form-field--disabled", cut.Find("div.el-form-field").ClassList);
        }

        [Fact]
        public void ChildReportsDisabled_FlipsFieldToDisabled()
        {
            var cut = Render<ElFormFieldGrow>();

            Assert.False(cut.Instance.IsDisabled);

            cut.Instance.Context.DisabledChanged(true);
            cut.WaitForAssertion(() => Assert.True(cut.Instance.IsDisabled), timeout: TestWait.Timeout);

            Assert.Contains("el-form-field--disabled", cut.Find("div.el-form-field").ClassList);
            Assert.True(cut.Instance.Context.Disabled);
        }

        [Fact]
        public void ReadOnly_AddsReadonlyClass()
        {
            var cut = Render<ElFormFieldGrow>(p => p.Add(c => c.ReadOnly, true));

            Assert.Contains("el-form-field--readonly", cut.Find("div.el-form-field").ClassList);
        }

        [Fact]
        public void Invalid_AddsInvalidClassAndErrorHelper()
        {
            var cut = Render<ElFormFieldGrow>(p => p
                .Add(c => c.Text, "邮箱")
                .Add(c => c.Invalid, true)
                .Add(c => c.ErrorText, "邮箱格式不正确"));

            Assert.Contains("el-form-field--invalid", cut.Find("div.el-form-field").ClassList);
            Assert.True(cut.Instance.ShowError);
            Assert.Equal("邮箱格式不正确", cut.Instance.EffectiveHelperText);
            Assert.Contains("el-form-field__helper--error", cut.Find(".el-form-field__helper").ClassList);
            Assert.Contains("邮箱格式不正确", cut.Markup);
        }

        [Fact]
        public void ErrorText_HiddenWhenNotInvalid()
        {
            var cut = Render<ElFormFieldGrow>(p => p
                .Add(c => c.ErrorText, "邮箱格式不正确"));

            Assert.False(cut.Instance.ShowError);
            Assert.False(cut.Instance.HasHelperContent);
            Assert.Empty(cut.FindAll(".el-form-field__helper"));
        }

        [Fact]
        public void HelperText_RendersWhenNoError()
        {
            var cut = Render<ElFormFieldGrow>(p => p
                .Add(c => c.Text, "邮箱")
                .Add(c => c.HelperText, "用于接收通知"));

            var helper = cut.Find(".el-form-field__helper");
            Assert.Contains("用于接收通知", helper.TextContent);
            Assert.DoesNotContain("el-form-field__helper--error", helper.ClassList);
        }

        [Fact]
        public void ErrorText_TakesPrecedenceOverHelperText()
        {
            var cut = Render<ElFormFieldGrow>(p => p
                .Add(c => c.HelperText, "用于接收通知")
                .Add(c => c.Invalid, true)
                .Add(c => c.ErrorText, "必填"));

            Assert.Equal("必填", cut.Instance.EffectiveHelperText);
            Assert.Contains("必填", cut.Markup);
            Assert.DoesNotContain("用于接收通知", cut.Markup);
        }

        [Fact]
        public void HelperFragment_TakesPrecedenceOverTexts()
        {
            var cut = Render<ElFormFieldGrow>(p => p
                .Add(c => c.HelperText, "普通说明")
                .Add(c => c.ErrorText, "错误说明")
                .Add(c => c.Invalid, true)
                .Add(c => c.Helper, SpanFragment("自定义辅助区")));

            Assert.Contains("自定义辅助区", cut.Markup);
            Assert.DoesNotContain("普通说明", cut.Markup);
            Assert.DoesNotContain("错误说明", cut.Markup);
        }

        [Fact]
        public void Helper_RoleIsAlertOnlyWhenError()
        {
            var error = Render<ElFormFieldGrow>(p => p
                .Add(c => c.Invalid, true)
                .Add(c => c.ErrorText, "必填"));

            var plain = Render<ElFormFieldGrow>(p => p.Add(c => c.HelperText, "说明"));

            Assert.Equal("alert", error.Find(".el-form-field__helper").GetAttribute("role"));
            Assert.Null(plain.Find(".el-form-field__helper").GetAttribute("role"));
        }

        // ------------------------------------------------------------------ 插槽

        [Fact]
        public void StartAndEnd_RenderSlotsAndModifierClasses()
        {
            var cut = Render<ElFormFieldGrow>(p => p
                .Add(c => c.Start, SpanFragment("前置"))
                .Add(c => c.End, SpanFragment("后置"))
                .Add(c => c.ChildContent, InputFragment()));

            Assert.Contains("前置", cut.Find(".el-form-field__start").TextContent);
            Assert.Contains("后置", cut.Find(".el-form-field__end").TextContent);
            Assert.Contains("el-form-field--has-start", cut.Instance.BuildCssClass());
            Assert.Contains("el-form-field--has-end", cut.Instance.BuildCssClass());
        }

        [Fact]
        public void ChildContent_RendersInsideContentBeforeLabel()
        {
            var cut = Render<ElFormFieldGrow>(p => p
                .Add(c => c.Text, "姓名")
                .Add(c => c.ChildContent, InputFragment()));

            var input = cut.Find("input.el-form-field__control");
            Assert.Equal("el-form-field__content", input.ParentElement.ClassName);
        }

        // ------------------------------------------------------------------ 宽度与自定义样式

        [Fact]
        public void Width_NumericGetsPixelSuffix()
        {
            var cut = Render<ElFormFieldGrow>(p => p.Add(c => c.Width, "320"));

            Assert.Contains("width:320px", cut.Instance.BuildStyle());
        }

        [Fact]
        public void Width_CssLengthKeptAsIs()
        {
            var cut = Render<ElFormFieldGrow>(p => p.Add(c => c.Width, "50%"));

            Assert.Contains("width:50%", cut.Instance.BuildStyle());
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("   ", "")]
        [InlineData("300", "300px")]
        [InlineData(" 12.5 ", "12.5px")]
        [InlineData("18rem", "18rem")]
        [InlineData("calc(100% - 20px)", "calc(100% - 20px)")]
        public void NormalizeLength_Works(string input, string expected)
        {
            Assert.Equal(expected, ElFormFieldGrow.NormalizeLength(input));
        }

        [Fact]
        public void ClsAndStyle_AreMergedIntoRoot()
        {
            var cut = Render<ElFormFieldGrow>(p => p
                .Add(c => c.Cls, "my-field")
                .Add(c => c.Style, "margin-top:4px")
                .Add(c => c.Width, "200px"));

            var root = cut.Find("div.el-form-field");
            Assert.Contains("my-field", root.ClassList);
            Assert.Contains("margin-top:4px", root.GetAttribute("style"));
            Assert.Contains("width:200px", root.GetAttribute("style"));
        }

        [Fact]
        public void Id_PropagatesToRootAndHelper()
        {
            var cut = Render<ElFormFieldGrow>(p => p
                .Add(c => c.Id, "email-field")
                .Add(c => c.HelperText, "说明"));

            Assert.Equal("email-field", cut.Find("div.el-form-field").GetAttribute("id"));
            Assert.Equal("email-field-helper", cut.Find(".el-form-field__helper").GetAttribute("id"));
        }

        [Fact]
        public void WithoutId_HelperHasNoId()
        {
            var cut = Render<ElFormFieldGrow>(p => p.Add(c => c.HelperText, "说明"));

            Assert.Null(cut.Find(".el-form-field__helper").GetAttribute("id"));
        }

        // ------------------------------------------------------------------ 级联上下文

        [Fact]
        public void CascadingContext_FlowsToChildComponent()
        {
            var cut = Render<ElFormFieldGrow>(p => p.Add(c => c.ChildContent, ProbeFragment));

            Assert.Equal("allow", cut.Find(".probe").TextContent);
        }

        [Fact]
        public void CascadingContext_ReflectsAllowFloatingLabel()
        {
            var cut = Render<ElFormFieldGrow>(p => p
                .Add(c => c.AllowFloatingLabel, false)
                .Add(c => c.ChildContent, ProbeFragment));

            Assert.Equal("deny", cut.Find(".probe").TextContent);
        }

        // ------------------------------------------------------------------ 浏览器兼容性守卫

        [Fact]
        public void Css_FormFieldSection_AvoidsUnsupportedCssFeatures()
        {
            var css = FormFieldCss();

            // 明确禁用的新特性：一旦引入即在老浏览器（Chromium 75 / Chrome 90）上整条规则失效。
            Assert.DoesNotContain(":has(", css);
            Assert.DoesNotContain(":is(", css);
            Assert.DoesNotContain(":where(", css);
            Assert.DoesNotContain("clamp(", css);
            Assert.DoesNotContain("inset-block", css);
            Assert.DoesNotContain("inset-inline", css);
            Assert.DoesNotContain("margin-block", css);
            Assert.DoesNotContain("padding-block", css);
            Assert.DoesNotContain("border-block", css);
            Assert.DoesNotMatch(@"(?<![-\w])(margin-inline|padding-inline|border-inline)\s*:", css);
            Assert.DoesNotMatch(@"(?<![-\w])gap\s*:", css);
            // :not() 只允许单参数（多参数形式是 Chrome 88+）
            Assert.DoesNotMatch(@":not\([^)]*,", css);
        }

        [Fact]
        public void Css_FormFieldSection_UsesFocusWithinInsteadOfJs()
        {
            var css = FormFieldCss();

            // 这是本组件“零 JS”的关键：聚焦态完全由 CSS :focus-within（Chrome 60+）驱动，
            // 不需要 Radzen 那样的 focusin/focusout JS 互操作。
            Assert.Contains(":focus-within", css);
            Assert.Contains(":not(:placeholder-shown)", css);
        }

        [Fact]
        public void Css_FormFieldSection_HasNoRadzenClasses()
        {
            Assert.DoesNotContain("rz-", FormFieldCss());
        }

        /// <summary>
        /// 消费方批次110r（用户 2026-10-23 三句连着的口径：「未显示全的，能不能显示完整，但是不要调整宽度，
        /// 如果超过了formfield的宽度才显示省略号」）钉住浮动标签的宽度预算档位。
        /// 三档都实测过：旧口径 <c>calc(100% - 24px)</c> 配 14px 字，在 84px 窄字段下裁掉四颗四字标签；
        /// 0.10.28 一刀切 <c>max-width:none + overflow:visible</c> 反过来让超宽标签画到邻颗控件上
        /// （消费方探针实测越出本字段 53px、压到邻颗 33px）；0.10.29 取中间档 —— 预算 = 内容盒宽 − 左偏移 12，
        /// 标签右沿正好落在 formfield 内边界，84px 字段可容 62px 文字（四字标签占 48）⇒ 一字不裁、真超宽才出省略号。
        /// 这条守卫防的是有人再把预算整个关掉（那正是 0.10.28 翻车的那一档）。
        /// </summary>
        [Fact]
        public void Css_FormFieldLabel_KeepsMiddleBudgetWithEllipsis()
        {
            var block = CssRuleBlock(".el-form-field__label {");

            Assert.Equal("calc(100% - 12px)", Decl(block, "max-width"));
            Assert.Equal("hidden", Decl(block, "overflow"));
            Assert.Equal("ellipsis", Decl(block, "text-overflow"));
            Assert.Equal("nowrap", Decl(block, "white-space"));
            Assert.Equal("12px", Decl(block, "left"));
            Assert.DoesNotMatch(@"max-width\s*:\s*none", block);
            Assert.DoesNotMatch(@"overflow\s*:\s*visible", block);
        }

        [Fact]
        public void SourceFiles_HaveNoRadzenDependency()
        {
            foreach (var file in new[]
            {
                "ElFormFieldGrow.razor",
                "ElFormFieldGrow.razor.cs",
                "ElFormFieldGrowEnums.cs",
                "ElFormFieldGrowContext.cs",
            })
            {
                var text = File.ReadAllText(Path.Combine(ComponentDir(), file));

                Assert.DoesNotContain("using Radzen", text);
                Assert.DoesNotContain("@using Radzen", text);
                Assert.DoesNotContain("<Radzen", text);
            }
        }

        // ------------------------------------------------------------------ 辅助

        private static string RepositoryRoot([CallerFilePath] string callerFilePath = null)
        {
            // 首选：用编译期源文件路径定位仓库根（本文件位于 <repo>/test/Element.ComponentTests/）。
            // 这样无论测试程序集在仓库内还是仓库外的最小工程里运行，都能找到真实仓库。
            var from = callerFilePath;

            if (!string.IsNullOrEmpty(from))
            {
                from = Path.GetDirectoryName(from);
            }

            var root = FindRoot(from) ?? FindRoot(AppContext.BaseDirectory);

            if (root == null)
            {
                throw new DirectoryNotFoundException("未能定位仓库根目录（含 src/Components/wwwroot/css）");
            }

            return root;
        }

        private static string FindRoot(string start)
        {
            var dir = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start);

            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "src", "Components", "wwwroot", "css")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            return null;
        }

        private static string ComponentDir()
            => Path.Combine(RepositoryRoot(), "src", "Components");

        /// <summary>取出 index.css 补丁段（原 fix.css，2026-09-26 并入）中 ElFormFieldGrow 样式段（已剥离注释）用于兼容性断言。</summary>
        private static string FormFieldCss()
        {
            var path = Path.Combine(ComponentDir(), "wwwroot", "css", "index.css");
            var text = File.ReadAllText(path);
            var marker = text.IndexOf("ElFormFieldGrow", StringComparison.Ordinal);

            Assert.True(marker >= 0, "index.css 中未找到 ElFormFieldGrow 样式段");

            // 标记词落在样式段的起始注释里，必须从该注释的 "/*" 开始截取，
            // 否则注释剥离正则匹配不到（注释开头被切掉了）。
            var commentStart = text.LastIndexOf("/*", marker, StringComparison.Ordinal);
            var section = commentStart >= 0 ? text.Substring(commentStart) : text.Substring(marker);

            // 注释里会列举被禁用的特性名（如 :has()），必须剥离后再检查实际声明。
            return Regex.Replace(section, @"/\*[\s\S]*?\*/", string.Empty);
        }

        /// <summary>
        /// 取本仓手写规则（行首选择器）的声明体。选择器必须整行开头：index.css 第 1 行是整份 EP 压缩产物，
        /// 任何「子串命中」都会先撞上那份压缩规则，行首判定把这类假命中筛掉。注释一并剥掉。
        /// </summary>
        private static string CssRuleBlock(string selector)
        {
            var text = File.ReadAllText(Path.Combine(ComponentDir(), "wwwroot", "css", "index.css"));
            var from = 0;

            while (true)
            {
                var index = text.IndexOf(selector, from, StringComparison.Ordinal);
                Assert.True(index >= 0, $"index.css 中未找到行首规则：{selector}");

                var lineStart = text.LastIndexOf('\n', index) + 1;
                if (text.Substring(lineStart, index - lineStart).Trim().Length == 0)
                {
                    var open = text.IndexOf('{', index);
                    var close = text.IndexOf('}', open);

                    return Regex.Replace(text.Substring(open + 1, close - open - 1), @"/\*.*?\*/", string.Empty, RegexOptions.Singleline)
                        .Replace("\n", " ").Replace("\r", " ");
                }

                from = index + 1;
            }
        }

        /// <summary>读一条声明的值；属性不存在即失败（把「规则被删了」和「值被改了」都判红）。</summary>
        private static string Decl(string block, string property)
        {
            var match = Regex.Match(block, Regex.Escape(property) + @"\s*:\s*([^;]+)");
            Assert.True(match.Success, $"规则体里没有 {property} 声明（实测块体：{block.Trim()}）");

            return match.Groups[1].Value.Trim();
        }
    }
}
