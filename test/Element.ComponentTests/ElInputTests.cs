using System.Threading.Tasks;
using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElInputTests : BunitContext
    {
        public ElInputTests()
        {
            Services.AddElementPlusServices();
        }

        [Fact]
        public void RendersPrefixAndSuffixSlots()
        {
            RenderFragment prefix = builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-prefix");
                builder.AddContent(2, "https://");
                builder.CloseElement();
            };
            RenderFragment suffix = builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-suffix");
                builder.AddContent(2, ".com");
                builder.CloseElement();
            };

            var cut = Render<ElInput<string>>(parameters => parameters
                .Add(x => x.Value, "element-plus")
                .Add(x => x.PrefixIcon, "el-icon-link")
                .Add(x => x.Prefix, prefix)
                .Add(x => x.SuffixIcon, "el-icon-search")
                .Add(x => x.Suffix, suffix));

            var wrapper = cut.Find(".el-input");

            Assert.Contains("el-input--prefix", wrapper.ClassList);
            Assert.Contains("el-input--suffix", wrapper.ClassList);
            Assert.Equal("https://", cut.Find(".custom-prefix").TextContent);
            Assert.Equal(".com", cut.Find(".custom-suffix").TextContent);
            Assert.Contains("el-icon-link", cut.Find(".el-input__prefix .el-input__icon").ClassList);
            Assert.Contains("el-icon-search", cut.Find(".el-input__suffix .el-input__icon").ClassList);
        }

        [Fact]
        public void FormatterAndParserControlDisplayedAndBoundValue()
        {
            decimal? value = 1234;
            decimal? inputValue = null;

            var cut = Render<ElInput<decimal?>>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.Formatter, next => next.HasValue ? $"$ {next.Value:N0}" : string.Empty)
                .Add(x => x.Parser, next => next.Replace("$", string.Empty).Replace(",", string.Empty).Trim())
                .Add(x => x.ValueChanged, next =>
                {
                    value = next;
                    inputValue = next;
                }));

            Assert.Equal("$ 1,234", cut.Find("input").GetAttribute("value"));

            cut.Find("input").Input("$ 2,500");

            Assert.Equal(2500, inputValue);
        }

        [Fact]
        public void ShowsWordLimitAndExceedState()
        {
            var cut = Render<ElInput<string>>(parameters => parameters
                .Add(x => x.Value, "Element")
                .Add(x => x.Maxlength, 5)
                .Add(x => x.ShowWordLimit, true));

            Assert.Contains("is-exceed", cut.Find(".el-input").ClassList);
            Assert.Equal("7 / 5", cut.Find(".el-input__count-inner").TextContent.Trim());
        }

        [Fact]
        public void ShowPasswordAloneMakesPasswordField()
        {
            var cut = Render<ElInput<string>>(parameters => parameters
                .Add(x => x.Value, "secret")
                .Add(x => x.ShowPassword, true));

            Assert.Equal("password", cut.Find("input").GetAttribute("type"));
            Assert.NotNull(cut.Find(".el-input__password"));
        }

        [Fact]
        public void PasswordToggleSwitchesNativeTypeAndRaisesEvent()
        {
            var visible = false;
            var cut = Render<ElInput<string>>(parameters => parameters
                .Add(x => x.Value, "secret")
                .Add(x => x.ShowPassword, true)
                .Add(x => x.PasswordVisibleChanged, v => visible = v));

            cut.Find(".el-input__password").Click();

            Assert.Equal("text", cut.Find("input").GetAttribute("type"));
            Assert.True(visible);
            Assert.True(cut.Instance.PasswordVisible);

            cut.Find(".el-input__password").Click();
            Assert.Equal("password", cut.Find("input").GetAttribute("type"));
            Assert.False(visible);
        }

        [Fact]
        public void PasswordIconIsHiddenWhenEmptyAndNotFocused()
        {
            var cut = Render<ElInput<string>>(parameters => parameters
                .Add(x => x.Value, string.Empty)
                .Add(x => x.ShowPassword, true));

            Assert.Empty(cut.FindAll(".el-input__password"));

            cut.Find("input").TriggerEvent("onfocus", new FocusEventArgs());

            Assert.NotNull(cut.Find(".el-input__password"));
        }

        [Fact]
        public void PasswordIconSlotOverridesDefaultIcon()
        {
            var cut = Render<ElInput<string>>(parameters => parameters
                .Add(x => x.Value, "secret")
                .Add(x => x.ShowPassword, true)
                .Add(x => x.PasswordIcon, (bool visible) => builder =>
                {
                    builder.OpenElement(0, "i");
                    builder.AddAttribute(1, "class", visible ? "my-eye-open" : "my-eye-off");
                    builder.CloseElement();
                }));

            Assert.Contains("my-eye-off", cut.Find(".el-input__password i").GetAttribute("class"));

            cut.Find(".el-input__password").Click();
            Assert.Contains("my-eye-open", cut.Find(".el-input__password i").GetAttribute("class"));
        }

        [Fact]
        public void PasswordFieldUsesNewPasswordAutocompleteByDefault()
        {
            var cut = Render<ElInput<string>>(parameters => parameters
                .Add(x => x.Value, "secret")
                .Add(x => x.ShowPassword, true));

            var input = cut.Find("input");
            Assert.Equal("new-password", input.GetAttribute("autocomplete"));
            Assert.Equal("true", input.GetAttribute("data-lpignore"));
            Assert.Equal("true", input.GetAttribute("data-1p-ignore"));
        }

        [Fact]
        public void ExplicitAutocompleteOrDisabledPreventionKeepsValue()
        {
            var cut = Render<ElInput<string>>(parameters => parameters
                .Add(x => x.Value, "secret")
                .Add(x => x.ShowPassword, true)
                .Add(x => x.Autocomplete, "current-password"));

            Assert.Equal("current-password", cut.Find("input").GetAttribute("autocomplete"));
            Assert.Null(cut.Find("input").GetAttribute("data-lpignore"));

            var cut2 = Render<ElInput<string>>(parameters => parameters
                .Add(x => x.Value, "secret")
                .Add(x => x.ShowPassword, true)
                .Add(x => x.PreventPasswordSave, false));

            Assert.Equal("off", cut2.Find("input").GetAttribute("autocomplete"));
        }

        [Fact]
        public void ClearIconOverridesDefaultClearIcon()
        {
            var cut = Render<ElInput<string>>(parameters => parameters
                .Add(x => x.Value, "abc")
                .Add(x => x.Clearable, true)
                .Add(x => x.ShowClearIcon, true)
                .Add(x => x.ClearIcon, "el-icon-delete"));

            var clear = cut.Find(".el-input__clear");
            Assert.Contains("el-icon-delete", clear.ClassList);
            Assert.DoesNotContain("el-icon-circle-close", clear.ClassList);
        }

        [Fact]
        public void WordLimitOutsideRendersOutsideSuffix()
        {
            var cut = Render<ElInput<string>>(parameters => parameters
                .Add(x => x.Value, "Element")
                .Add(x => x.Maxlength, 10)
                .Add(x => x.ShowWordLimit, true)
                .Add(x => x.WordLimitPosition, "outside"));

            var count = cut.Find(".el-input__count");
            Assert.Contains("is-outside", count.ClassList);
            Assert.DoesNotContain("el-input__suffix-inner", count.ParentElement?.GetAttribute("class") ?? string.Empty);
            Assert.Equal("7 / 10", cut.Find(".el-input__count-inner").TextContent.Trim());
        }

        [Fact]
        public void NativeMinMaxStepAndLabelAliasRendered()
        {
            var cut = Render<ElInput<string>>(parameters => parameters
                .Add(x => x.Type, InputType.Number)
                .Add(x => x.Min, 0)
                .Add(x => x.Max, 100)
                .Add(x => x.Step, 5)
                .Add(x => x.Label, "年龄"));

            var input = cut.Find("input");
            Assert.Equal("0", input.GetAttribute("min"));
            Assert.Equal("100", input.GetAttribute("max"));
            Assert.Equal("5", input.GetAttribute("step"));
            Assert.Equal("年龄", input.GetAttribute("aria-label"));
        }

        [Fact]
        public void CountGraphemesReplacesNativeMaxlength()
        {
            var cut = Render<ElInput<string>>(parameters => parameters
                .Add(x => x.Value, "abc")
                .Add(x => x.Maxlength, 5)
                .Add(x => x.ShowWordLimit, true)
                .Add(x => x.CountGraphemes, text => text.Length * 2));

            Assert.Null(cut.Find("input").GetAttribute("maxlength"));
            Assert.Equal("6 / 5", cut.Find(".el-input__count-inner").TextContent.Trim());
            Assert.Contains("is-exceed", cut.Find(".el-input").ClassList);
        }

        [Fact]
        public async Task CompositionEndFromJsUpdatesValue()
        {
            var cut = Render<ElInput<string>>();
            await cut.Instance.OnCompositionEndFromJs("中文");

            Assert.Equal("中文", cut.Instance.Value);
        }

        [Fact]
        public void MouseEnterAndMouseLeaveEventsFire()
        {
            var entered = 0;
            var left = 0;
            var cut = Render<ElInput<string>>(parameters => parameters
                .Add(x => x.OnMouseEnter, () => entered++)
                .Add(x => x.OnMouseLeave, () => left++));

            cut.Find(".el-input").TriggerEvent("onmouseover", new MouseEventArgs());
            cut.Find(".el-input").TriggerEvent("onmouseout", new MouseEventArgs());

            Assert.Equal(1, entered);
            Assert.Equal(1, left);
        }
    }
}
