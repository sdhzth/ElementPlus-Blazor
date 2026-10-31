using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElCheckboxTests : BunitContext
    {
        public ElCheckboxTests()
        {
            Services.AddElementPlusServices();
        }

        [Fact]
        public void RendersIndeterminateState()
        {
            var cut = Render<ElCheckbox<bool>>(parameters => parameters
                .Add(x => x.Value, true)
                .Add(x => x.Indeterminate, true)
                .AddChildContent("All"));

            var label = cut.Find("label");
            Assert.Equal("mixed", label.GetAttribute("aria-checked"));
            Assert.Contains("is-indeterminate", label.ClassList);
            Assert.Contains("is-indeterminate", cut.Find(".el-checkbox__input").ClassList);
            Assert.False(cut.Find("input").HasAttribute("checked"));
        }

        [Fact]
        public async Task StandaloneBoolCheckbox_ToggleEmitsTrueThenFalse()
        {
            // 独立 bool 复选框（无 TrueValue）走 @bind-Value 的场景：
            // 勾选必须回传 true（旧实现回传旧值 false，宿主状态永远翻不过来），
            // 取消勾选回传 false。
            var values = new List<bool>();
            var cut = Render<ElCheckbox<bool>>(parameters => parameters
                .Add(x => x.Value, false)
                .Add(x => x.ValueChanged, v => values.Add(v))
                .AddChildContent("启用"));

            await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = true });
            await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = false });

            Assert.Equal(2, values.Count);
            Assert.True(values[0]);
            Assert.False(values[1]);
        }

        [Fact]
        public void GroupMaxDisablesUncheckedOptions()
        {
            var cut = Render<ElCheckboxGroup<string>>(parameters => parameters
                .Add(x => x.Value, new[] { "a" })
                .Add(x => x.Max, 1)
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElCheckbox<string>>(0);
                    builder.AddAttribute(1, "Value", "a");
                    builder.AddAttribute(2, "ChildContent", Text("A"));
                    builder.CloseComponent();
                    builder.OpenComponent<ElCheckbox<string>>(3);
                    builder.AddAttribute(4, "Value", "b");
                    builder.AddAttribute(5, "ChildContent", Text("B"));
                    builder.CloseComponent();
                }));

            var inputs = cut.FindAll("input");
            Assert.False(inputs[0].HasAttribute("disabled"));
            Assert.True(inputs[1].HasAttribute("disabled"));
        }

        [Fact]
        public void GroupMinPreventsUncheckingLastRequiredOption()
        {
            var selected = new List<string> { "a" };
            var cut = Render<ElCheckboxGroup<string>>(parameters => parameters
                .Add(x => x.Value, selected)
                .Add(x => x.ValueChanged, values => selected = values.ToList())
                .Add(x => x.Min, 1)
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElCheckbox<string>>(0);
                    builder.AddAttribute(1, "Value", "a");
                    builder.AddAttribute(2, "ChildContent", Text("A"));
                    builder.CloseComponent();
                }));

            var input = cut.Find("input");
            Assert.True(input.HasAttribute("disabled"));
            input.Change(false);

            Assert.Equal(new[] { "a" }, selected);
        }

        [Fact]
        public void GroupSizePropagatesToCheckboxes()
        {
            var cut = Render<ElCheckboxGroup<string>>(parameters => parameters
                .Add(x => x.Size, InputSize.Small)
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElCheckbox<string>>(0);
                    builder.AddAttribute(1, "Value", "a");
                    builder.AddAttribute(2, "ChildContent", Text("A"));
                    builder.CloseComponent();
                }));

            Assert.Contains("el-checkbox--small", cut.Find("label").ClassList);
        }

        [Fact]
        public void CheckboxButtonUsesGroupSizeAndLimitDisabledState()
        {
            var cut = Render<ElCheckboxGroup<string>>(parameters => parameters
                .Add(x => x.Value, new[] { "a" })
                .Add(x => x.Max, 1)
                .Add(x => x.Size, InputSize.Large)
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElCheckboxButton<string>>(0);
                    builder.AddAttribute(1, "Value", "a");
                    builder.AddAttribute(2, "ChildContent", Text("A"));
                    builder.CloseComponent();
                    builder.OpenComponent<ElCheckboxButton<string>>(3);
                    builder.AddAttribute(4, "Value", "b");
                    builder.AddAttribute(5, "ChildContent", Text("B"));
                    builder.CloseComponent();
                }));

            var labels = cut.FindAll("label");
            Assert.Contains("el-checkbox-button--large", labels[0].ClassList);
            Assert.Contains("el-checkbox-button--large", labels[1].ClassList);
            Assert.Contains("is-disabled", labels[1].ClassList);
            Assert.True(labels[1].QuerySelector("input").HasAttribute("disabled"));
        }

        [Fact]
        public async Task ValidateEventFalseDoesNotClearFormErrorOnToggle()
        {
            // 对照实验：同一勾选动作，只有 ValidateEvent 一个变量不同
            var validateOn = RenderToggleCheckbox(validateEvent: true);
            var validateOff = RenderToggleCheckbox(validateEvent: false);

            Assert.Contains("is-success", validateOn.Find(".el-form-item").ClassList);
            Assert.DoesNotContain("is-success", validateOff.Find(".el-form-item").ClassList);
            Assert.DoesNotContain("is-error", validateOff.Find(".el-form-item").ClassList);
        }

        private Bunit.IRenderedComponent<ElForm> RenderToggleCheckbox(bool validateEvent)
        {
            var cut = Render<ElForm>(parameters => parameters
                .AddChildContent<ElFormItem<bool>>(item => item
                    .Add(x => x.Name, "agree")
                    .Add(x => x.Required, true)
                    .AddChildContent<ElCheckbox<bool>>(cb => cb
                        .Add(x => x.ValidateEvent, validateEvent))));

            cut.Find("input").Change(true);
            cut.Render();
            return cut;
        }

        [Fact]
        public void ValidateEventTrueTriggersFormValidationOnToggle()
        {
            bool value = false;
            var cut = Render<ElForm>(parameters => parameters
                .AddChildContent<ElFormItem<bool>>(item => item
                    .Add(x => x.Name, "agree")
                    .Add(x => x.Required, true)
                    .AddChildContent<ElCheckbox<bool>>(cb => cb
                        .Add(x => x.Value, value)
                        .Add(x => x.ValueChanged, v => value = v))));

            cut.Find("input").Change(true);
            cut.Render();

            // 值确实更新了（勾选态反映到原生 input）
            Assert.NotNull(cut.Find("input").GetAttribute("checked"));
            // 默认 ValidateEvent=true：勾选触发表单重校验并通过，form-item 进入 success 态
            Assert.Contains("is-success", cut.Find(".el-form-item").ClassList);
        }

        private static Microsoft.AspNetCore.Components.RenderFragment Text(string value)
        {
            return builder => builder.AddContent(0, value);
        }
    }
}
