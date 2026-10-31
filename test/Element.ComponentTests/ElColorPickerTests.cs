using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElColorPickerTests : BunitContext
    {
        public ElColorPickerTests()
        {
            Services.AddElementServices();
        }

        [Fact]
        public void RendersTriggerWithColorAndSize()
        {
            var cut = Render<ElColorPicker>(parameters => parameters
                .Add(x => x.Value, "rgba(255, 69, 0, 0.68)")
                .Add(x => x.ShowAlpha, true)
                .Add(x => x.Size, InputSize.Small));

            Assert.Contains("el-color-picker--small", cut.Find(".el-color-picker").ClassList);
            Assert.Contains("is-alpha", cut.Find(".el-color-picker__color").ClassList);
            Assert.Contains("rgba(255, 69, 0, 0.68)", cut.Find(".el-color-picker__color-inner").GetAttribute("style"));
        }

        [Fact]
        public void OpensPanelWithAlphaAndPredefinedColors()
        {
            var cut = Render<ElColorPicker>(parameters => parameters
                .Add(x => x.Value, "#ff0000")
                .Add(x => x.ShowAlpha, true)
                .Add(x => x.Predefine, new[] { "#ff0000", "#00ff00" }));

            cut.Find(".el-color-picker").Click();
            var dropdown = GetDropdown();
            var fragment = Render(dropdown.OptionContent);

            Assert.Contains("el-color-picker__dropdown", dropdown.PopperClass);
            Assert.Equal(300, dropdown.Width);
            Assert.NotNull(fragment.Find(".el-color-alpha-slider"));
            Assert.Equal(2, fragment.FindAll(".el-color-predefine__color-selector").Count);
        }

        [Fact]
        public void PanelChangeUpdatesBoundValueAndChangeEvent()
        {
            var value = "#ff0000";
            var changes = new List<string>();
            var cut = Render<ElColorPicker>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.OnChange, next => changes.Add(next)));

            cut.Find(".el-color-picker").Click();
            var fragment = Render(GetDropdown().OptionContent);
            fragment.Find("input[aria-label='hue']").Input("120");

            Assert.Equal("#00ff00", value);
            Assert.Equal(new[] { "#00ff00" }, changes);
        }

        [Fact]
        public void ClearableTriggerClearsValue()
        {
            var value = "#ff0000";
            var cleared = false;
            var cut = Render<ElColorPicker>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.Clearable, true)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.OnClear, _ => cleared = true));

            cut.Find(".el-color-picker").Click();
            var footer = Render(GetDropdown().OptionContent);
            footer.Find(".el-color-dropdown__link-btn").Click();

            Assert.Equal(string.Empty, value);
            Assert.True(cleared);
        }

        /// <summary>
        /// EP 的 clearable 默认就是 true，所以没写参数的色板页脚也该有「清空」。
        /// 翻面对照：显式 Clearable="false" 时它必须消失（见 FooterOmitsClearButtonWhenClearableDisabled）。
        /// </summary>
        [Fact]
        public void FooterHasClearButtonByDefault()
        {
            var cut = Render<ElColorPicker>(parameters => parameters.Add(x => x.Value, "#ff0000"));

            cut.Find(".el-color-picker").Click();
            var footer = Render(GetDropdown().OptionContent);
            var clear = footer.Find(".el-color-dropdown__link-btn");

            Assert.Equal("清空", clear.TextContent.Trim());
            Assert.Contains("el-button", clear.ClassList);
            Assert.False(clear.HasAttribute("disabled"));
        }

        [Fact]
        public void FooterOmitsClearButtonWhenClearableDisabled()
        {
            var cut = Render<ElColorPicker>(parameters => parameters
                .Add(x => x.Value, "#ff0000")
                .Add(x => x.Clearable, false));

            cut.Find(".el-color-picker").Click();
            var footer = Render(GetDropdown().OptionContent);

            Assert.Empty(footer.FindAll(".el-color-dropdown__link-btn"));
            Assert.Equal("确定", footer.Find(".el-color-dropdown__btn").TextContent.Trim());
        }

        /// <summary>
        /// 页脚两个按钮都是 Element 的 .el-button（EP：text/plain + size=small），
        /// 不再靠遗留的 .el-color-dropdown__btn* 尺寸规则——那两条规则已从 index.css 删除。
        /// </summary>
        [Fact]
        public void FooterButtonsRenderAsElementButtons()
        {
            var css = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "css", "index.css"));
            var cut = Render<ElColorPicker>(parameters => parameters.Add(x => x.Value, "#ff0000"));

            cut.Find(".el-color-picker").Click();
            var footer = Render(GetDropdown().OptionContent);
            var buttons = footer.FindAll("button.el-button").ToList();

            Assert.Equal(2, buttons.Count);
            Assert.Contains("el-button--small", buttons[0].ClassName);
            Assert.Contains("is-text", buttons[0].ClassName);
            Assert.Contains("el-button--small", buttons[1].ClassName);
            Assert.Contains("is-plain", buttons[1].ClassName);
            Assert.DoesNotContain(".el-color-dropdown__btn{", css);
            Assert.DoesNotContain(".el-color-dropdown__link-btn{", css);
        }

        /// <summary>
        /// 触发器图标在 EP 里只是显示态，没有点击语义：点图标必须开面板，
        /// 不能被「悬停换 ✕」这个扩展吃掉（原来实测 6/6 首点变成清空）。
        /// </summary>
        [Fact]
        public void TriggerIconClickOpensPanelInsteadOfClearing()
        {
            var value = "#ff0000";
            var cleared = false;
            var cut = Render<ElColorPicker>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.OnClear, _ => cleared = true));

            var icon = cut.Find(".el-color-picker__icon");
            Assert.Contains("el-icon-arrow-down", icon.ClassList);
            Assert.DoesNotContain("el-icon-close", icon.ClassList);

            icon.Click();

            Assert.False(cleared);
            Assert.Equal("#ff0000", value);
            Assert.NotEmpty(Services.GetRequiredService<PopupService>().SelectDropDownOptions);
        }

        /// <summary>
        /// EP 的页脚「清空」只禁到组件禁用（color-picker.vue 里是 :disabled="disabled"），
        /// 没有色值时点清空是幂等空操作，不该灰成「坏了」的样子。
        /// </summary>
        [Fact]
        public void ClearButtonStaysEnabledWithoutAColor()
        {
            var cut = Render<ElColorPicker>(parameters => parameters.Add(x => x.Value, null));

            cut.Find(".el-color-picker").Click();
            var footer = Render(GetDropdown().OptionContent);
            var clear = footer.Find(".el-color-dropdown__link-btn");

            Assert.False(clear.HasAttribute("disabled"));

            // 正向对照：同一份页脚里「确定」也不该被禁，否则两条断言可能只是「按钮没渲染出来」的假绿
            Assert.False(footer.Find(".el-color-dropdown__btn").HasAttribute("disabled"));
        }

        [Fact]
        public void DisabledPickerDoesNotOpen()
        {
            var cut = Render<ElColorPicker>(parameters => parameters
                .Add(x => x.Value, "#ff0000")
                .Add(x => x.Disabled, true));

            cut.Find(".el-color-picker").Click();

            Assert.Empty(Services.GetRequiredService<PopupService>().SelectDropDownOptions);
            Assert.Contains("is-disabled", cut.Find(".el-color-picker").ClassList);
        }

        private DropDownOption GetDropdown()
        {
            return Services.GetRequiredService<PopupService>().SelectDropDownOptions.Single();
        }

        private static string RepoRoot([CallerFilePath] string file = null)
        {
            // .../test/Element.ComponentTests/ElColorPickerTests.cs → 上溯三层
            var directory = new FileInfo(file).Directory;
            return directory.Parent.Parent.FullName;
        }
    }
}
