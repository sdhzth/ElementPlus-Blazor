using System;
using System.Threading.Tasks;
using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElInputNumberTests : BunitContext
    {
        public ElInputNumberTests()
        {
            Services.AddElementServices();
            JSInterop.SetupVoid("execFocus", _ => true).SetVoidResult();
            JSInterop.SetupVoid("execBlur", _ => true).SetVoidResult();
        }

        [Fact]
        public void KeyboardSupportsStrictSpinbuttonNavigation()
        {
            var cut = Render<InputNumberKeyboardHost>();

            var input = cut.Find("input");

            input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
            Assert.Equal(7, cut.Instance.Value);

            input.KeyDown(new KeyboardEventArgs { Key = "PageUp" });
            Assert.Equal(20, cut.Instance.Value);

            input.KeyDown(new KeyboardEventArgs { Key = "Home" });
            Assert.Equal(0, cut.Instance.Value);

            input.KeyDown(new KeyboardEventArgs { Key = "End" });
            Assert.Equal(20, cut.Instance.Value);

            input.KeyDown(new KeyboardEventArgs { Key = "PageDown" });
            Assert.Equal(0, cut.Instance.Value);
        }

        [Fact]
        public void ModifiedNavigationKeysDoNotSpinValue()
        {
            var cut = Render<InputNumberKeyboardHost>();

            var input = cut.Find("input");

            input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp", ShiftKey = true });
            input.KeyDown(new KeyboardEventArgs { Key = "PageUp", CtrlKey = true });
            input.KeyDown(new KeyboardEventArgs { Key = "Home", MetaKey = true });
            input.KeyDown(new KeyboardEventArgs { Key = "End", AltKey = true });

            Assert.Equal(5, cut.Instance.Value);
        }

        [Fact]
        public void DisabledAndReadonlyKeyboardDoesNotSpinValue()
        {
            decimal? disabledValue = 5;
            var disabled = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, disabledValue)
                .Add(x => x.ValueChanged, next => disabledValue = next)
                .Add(x => x.Disabled, true));

            disabled.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
            Assert.Equal(5, disabledValue);

            decimal? readonlyValue = 5;
            var readOnly = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, readonlyValue)
                .Add(x => x.ValueChanged, next => readonlyValue = next)
                .Add(x => x.Readonly, true));

            readOnly.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
            Assert.Equal(5, readonlyValue);
        }

        [Fact]
        public void EnterCommitsPendingTypedValue()
        {
            decimal? value = 1;
            decimal? changed = null;
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, value)
                .Add(x => x.StepStrictly, true)
                .Add(x => x.Step, 0.5m)
                .Add(x => x.Precision, 1)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.OnChange, next => changed = next));

            var input = cut.Find("input");
            input.Input("1.24");
            input.KeyDown(new KeyboardEventArgs { Key = "Enter" });

            Assert.Equal(1.0m, value);
            Assert.Equal(1.0m, changed);
            Assert.Equal("1.0", cut.Find("input").GetAttribute("value"));
        }

        [Fact]
        public void InvalidTypedValueIsNotCommittedAndResetsOnBlur()
        {
            decimal? value = 3;
            decimal? inputValue = null;
            decimal? changedValue = null;
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.OnInput, next => inputValue = next)
                .Add(x => x.OnChange, next => changedValue = next));

            var input = cut.Find("input");
            input.Input("abc");

            Assert.Equal(3, value);
            Assert.Null(inputValue);
            Assert.Null(input.GetAttribute("aria-valuenow"));
            Assert.Null(input.GetAttribute("aria-valuetext"));

            input.Blur();

            Assert.Equal(3, value);
            Assert.Null(changedValue);
            Assert.Equal("3", cut.Find("input").GetAttribute("value"));
            Assert.Equal("3", cut.Find("input").GetAttribute("aria-valuenow"));
        }

        /// <summary>
        /// 消费方批次110r（用户 2026-10-23：「这个单价，是只读的吧，但是点击上，然后鼠标移走会变成1」）。
        /// 根因不在「只读没生效」：只读的 input 照样能拿焦点，失焦仍走提交通路 ⇒
        /// <c>CommitTextAsync → Normalize</c> 把低于 Min 的原值夹到 Min 再写回 ValueChanged（单价 0.2 + Min 1 → 1）。
        /// 闸门在 <c>ElInputNumber.razor.cs</c> 的 <c>OnBlurAsync</c> / <c>OnChangeAsync</c>（0.10.28 起）。
        /// 下一条是非只读的正向对照：闸门不能顺手把可编辑字段的夹取语义也关掉。
        /// </summary>
        [Fact]
        public void ReadonlyBlurAndChangeNeverCommitBackToMin()
        {
            foreach (var readOnly in new[] { true, false })
            {
                decimal? value = 0.2m;
                var cut = Render<ElInputNumber>(parameters => parameters
                    .Add(x => x.Controls, true)
                    .Add(x => x.Value, value)
                    .Add(x => x.ValueChanged, next => value = next)
                    .Add(x => x.Min, 1m)
                    .Add(x => x.Readonly, readOnly));

                var input = cut.Find("input");
                input.Change("0");
                input.Blur();

                if (readOnly)
                {
                    Assert.Equal(0.2m, value);
                    Assert.Equal("0.2", cut.Find("input").GetAttribute("value"));
                }
                else
                {
                    // 正向对照：同一条通路打在非只读字段上必须真的把 0.2 夹到 Min=1。
                    Assert.Equal(1m, value);
                }
            }
        }

        /// <summary>禁用档与只读同闸门（消费方有 Disabled 的数字框，症状一模一样）。</summary>
        [Fact]
        public void DisabledBlurDoesNotCommitBackToMin()
        {
            decimal? value = 0.2m;
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.Min, 1m)
                .Add(x => x.Disabled, true));

            cut.Find("input").Blur();

            Assert.Equal(0.2m, value);
        }

        [Fact]
        public void RendersSpinbuttonAriaAttributesOnInput()
        {
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 2.5m)
                .Add(x => x.Min, 1m)
                .Add(x => x.Max, 5m)
                .Add(x => x.Step, 0.5m)
                .Add(x => x.Precision, 1)
                .Add(x => x.Readonly, true));

            var input = cut.Find("input");

            Assert.Equal("spinbutton", input.GetAttribute("role"));
            Assert.Equal("1", input.GetAttribute("aria-valuemin"));
            Assert.Equal("5", input.GetAttribute("aria-valuemax"));
            Assert.Equal("2.5", input.GetAttribute("aria-valuenow"));
            Assert.Equal("2.5", input.GetAttribute("aria-valuetext"));
            Assert.Equal("true", input.GetAttribute("aria-readonly"));
        }

        [Fact]
        public void RendersTextInputSemanticsAndConfigurableAria()
        {
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 2m)
                .Add(x => x.Autocomplete, "one-time-code")
                .Add(x => x.Inputmode, "numeric")
                .Add(x => x.Tabindex, 3)
                .Add(x => x.AriaLabel, "Quantity")
                .Add(x => x.AriaLabelledby, "quantity-label")
                .Add(x => x.AriaDescribedby, "quantity-help"));

            var input = cut.Find("input");

            Assert.Equal("text", input.GetAttribute("type"));
            Assert.Equal("one-time-code", input.GetAttribute("autocomplete"));
            Assert.Equal("numeric", input.GetAttribute("inputmode"));
            Assert.Equal("3", input.GetAttribute("tabindex"));
            Assert.Equal("Quantity", input.GetAttribute("aria-label"));
            Assert.Equal("quantity-label", input.GetAttribute("aria-labelledby"));
            Assert.Equal("quantity-help", input.GetAttribute("aria-describedby"));
        }

        [Fact]
        public void EmptyValueOmitsCurrentAriaValue()
        {
            var cut = Render<ElInputNumber>();

            var input = cut.Find("input");

            Assert.Null(input.GetAttribute("aria-valuenow"));
            Assert.Null(input.GetAttribute("aria-valuetext"));
            Assert.Equal("false", input.GetAttribute("aria-invalid"));
            Assert.Equal("false", input.GetAttribute("aria-disabled"));
        }

        [Fact]
        public void FormItemConnectsLabelAndValidationAria()
        {
            var cut = Render<ElForm>(parameters => parameters
                .AddChildContent<ElFormItem<decimal?>>(item => item
                    .Add(x => x.Name, "count")
                    .Add(x => x.Label, "Count")
                    .Add(x => x.Required, true)
                    .AddChildContent<ElInputNumber>(input => input
                        .Add(x => x.Value, (decimal?)null))));

            cut.InvokeAsync(() => cut.Instance.Validate());
            cut.Render();

            var label = cut.Find("label.el-form-item__label");
            var input = cut.Find("input");
            var error = cut.Find(".el-form-item__error");

            Assert.Equal(input.Id, label.GetAttribute("for"));
            Assert.Equal("true", input.GetAttribute("aria-invalid"));
            Assert.Equal(error.Id, input.GetAttribute("aria-describedby"));
            Assert.Equal("spinbutton", input.GetAttribute("role"));
        }

        [Fact]
        public void FormItemMergesCustomAndValidationDescriptions()
        {
            var cut = Render<ElForm>(parameters => parameters
                .AddChildContent<ElFormItem<decimal?>>(item => item
                    .Add(x => x.Name, "count")
                    .Add(x => x.Label, "Count")
                    .Add(x => x.Required, true)
                    .AddChildContent<ElInputNumber>(input => input
                        .Add(x => x.Value, (decimal?)null)
                        .Add(x => x.AriaDescribedby, "count-help"))));

            cut.InvokeAsync(() => cut.Instance.Validate());
            cut.Render();

            Assert.Equal("count-help count-error", cut.Find("input").GetAttribute("aria-describedby"));
        }

        /// <summary>0.10.31 起 Controls 默认收敛为 <c>false</c>：不传即无步进按钮
        /// （业务表单以键盘输入为主，窄列里步进按钮会挤爆输入框）。</summary>
        [Fact]
        public void ControlsDefaultToFalse()
        {
            var cut = Render<ElInputNumber>(parameters => parameters.Add(x => x.Value, 5m));

            Assert.Empty(cut.FindAll(".el-input-number__increase"));
            Assert.Empty(cut.FindAll(".el-input-number__decrease"));
        }

        /// <summary>显式 <c>Controls="true"</c> 仍渲染步进按钮——对齐官方 demo 视觉的逃生阀保留。</summary>
        [Fact]
        public void ControlsTrueRendersButtons()
        {
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 5m));

            Assert.Single(cut.FindAll(".el-input-number__increase"));
            Assert.Single(cut.FindAll(".el-input-number__decrease"));
        }

        [Fact]
        public void ControlsExposeAriaDisabledAndControlTargetInput()
        {
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 10m)
                .Add(x => x.Min, 0m)
                .Add(x => x.Max, 10m));

            var input = cut.Find("input");
            var increase = cut.Find(".el-input-number__increase");
            var decrease = cut.Find(".el-input-number__decrease");

            Assert.Equal(input.Id, increase.GetAttribute("aria-controls"));
            Assert.Equal(input.Id, decrease.GetAttribute("aria-controls"));
            Assert.Equal("true", increase.GetAttribute("aria-disabled"));
            Assert.Equal("false", decrease.GetAttribute("aria-disabled"));
            Assert.Equal("-1", increase.GetAttribute("tabindex"));
            Assert.Equal("0", decrease.GetAttribute("tabindex"));
            Assert.Equal("true", cut.Find(".el-input-number__increase i").GetAttribute("aria-hidden"));
        }

        [Fact]
        public void ControlButtonsSupportKeyboardActivation()
        {
            decimal? value = 1;
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.Min, 0m)
                .Add(x => x.Max, 3m));

            cut.Find(".el-input-number__increase").KeyDown(new KeyboardEventArgs { Key = "Enter" });
            Assert.Equal(2, value);

            cut.Find(".el-input-number__decrease").KeyDown(new KeyboardEventArgs { Key = " " });
            Assert.Equal(1, value);
        }

        private class InputNumberKeyboardHost : ComponentBase
        {
            public decimal? Value { get; set; } = 5m;

            protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
            {
                builder.OpenComponent<ElInputNumber>(0);
                builder.AddAttribute(1, nameof(ElInputNumber.Value), Value);
                builder.AddAttribute(2, nameof(ElInputNumber.ValueChanged), EventCallback.Factory.Create<decimal?>(this, OnValueChanged));
                builder.AddAttribute(3, nameof(ElInputNumber.Min), 0m);
                builder.AddAttribute(4, nameof(ElInputNumber.Max), 20m);
                builder.AddAttribute(5, nameof(ElInputNumber.Step), 2m);
                builder.CloseComponent();
            }

            private void OnValueChanged(decimal? value)
            {
                Value = value;
                StateHasChanged();
            }
        }

        [Fact]
        public void ClearableShowClearIconRendersClearButton()
        {
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 5m)
                .Add(x => x.Clearable, true)
                .Add(x => x.ShowClearIcon, true));

            var clear = cut.Find(".el-input__clear");
            Assert.NotNull(clear);
            Assert.Contains("el-icon-circle-close", clear.GetAttribute("class"));
        }

        [Fact]
        public void ClearableWithoutShowClearIconRendersOnlyOnHover()
        {
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 5m)
                .Add(x => x.Clearable, true));

            Assert.Empty(cut.FindAll(".el-input__clear"));
        }

        [Fact]
        public void ClearButtonWipesValue()
        {
            decimal? value = 5m;
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.Clearable, true)
                .Add(x => x.ShowClearIcon, true));

            cut.Find(".el-input__clear").Click();

            Assert.Null(value);
        }

        /// <summary>
        /// P0-1 护栏：<c>ShowClearIcon</c> 单独设置就必须常驻显示清除图标。
        /// 修复前它被 AND 在 <c>Clearable</c> 之后，这条必红（两份浏览器探针同报同一现象）。
        /// </summary>
        [Fact]
        public void ShowClearIconAloneRendersClearIcon()
        {
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 5m)
                .Add(x => x.ShowClearIcon, true));

            Assert.Single(cut.FindAll(".el-input__clear"));
        }

        /// <summary>P0-2：对齐 EP value-on-clear —— "min" 清到下限而不是清成 null。</summary>
        [Fact]
        public void ValueOnClearMinClearsToMinimum()
        {
            decimal? value = 5m;
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.Min, 2m)
                .Add(x => x.ShowClearIcon, true)
                .Add(x => x.ValueOnClear, "min"));

            cut.Find(".el-input__clear").Click();

            Assert.Equal(2m, value);
        }

        /// <summary>P0-2：value-on-clear 支持指定数字。</summary>
        [Fact]
        public void ValueOnClearNumberClearsToThatNumber()
        {
            decimal? value = 5m;
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.ShowClearIcon, true)
                .Add(x => x.ValueOnClear, "0"));

            cut.Find(".el-input__clear").Click();

            Assert.Equal(0m, value);
        }

        /// <summary>P0-2：不设 value-on-clear 时行为与旧版一字不差（清成 null），防止默认值漂移。</summary>
        [Fact]
        public void ValueOnClearUnsetStillClearsToNull()
        {
            decimal? value = 5m;
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.ShowClearIcon, true));

            cut.Find(".el-input__clear").Click();

            Assert.Null(value);
        }

        /// <summary>P0-3：默认对齐 = center（与 EP 2.14 及本仓基底 CSS 一致，故不传时零视觉变化）。</summary>
        [Fact]
        public void AlignDefaultsToCenterClass()
        {
            var cut = Render<ElInputNumber>(parameters => parameters.Add(x => x.Value, 5m));

            Assert.Contains("is-center", cut.Find(".el-input-number").ClassList);
        }

        [Theory]
        [InlineData("left")]
        [InlineData("right")]
        public void AlignEmitsIsModifierClass(string align)
        {
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 5m)
                .Add(x => x.Align, align));

            var root = cut.Find(".el-input-number");
            Assert.Contains($"is-{align}", root.ClassList);
            Assert.DoesNotContain("is-center", root.ClassList);
        }

        /// <summary>P0-3：非法值退回 center，而不是渲染出一个没有 CSS 对应的 is-xxx。</summary>
        [Fact]
        public void AlignFallsBackToCenterForUnknownValue()
        {
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 5m)
                .Add(x => x.Align, "middle"));

            var classes = cut.Find(".el-input-number").ClassList;
            Assert.Contains("is-center", classes);
            Assert.DoesNotContain("is-middle", classes);
        }

        /// <summary>
        /// P0-4：按住步进按钮要连发（EP <c>v-repeat-click</c>：600ms 后开始，每 100ms 一步），
        /// 松开即停。用 WaitForState 轮询而不是固定延时，避免并行跑套件时 CPU 争抢造成假失败。
        /// </summary>
        [Fact]
        public void HoldingIncreaseRepeatsAndStopsOnMouseUp()
        {
            decimal? value = 0m;
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.Max, 100m));

            cut.Find(".el-input-number__increase").MouseDown();

            cut.WaitForState(() => value >= 3m, timeout: System.TimeSpan.FromSeconds(6));
            Assert.True(value >= 3m, "按住 600ms 后应每 100ms 连发，实测只走到 " + value);

            cut.Find(".el-input-number__increase").MouseUp();
            var afterRelease = value;
            System.Threading.Thread.Sleep(500);
            Assert.Equal(afterRelease, value);   // 松开后不得继续走
        }

        /// <summary>P0-4：快速点击（只有 click，没有按住）净效果仍是一步，不能被连发逻辑多算。</summary>
        [Fact]
        public void SingleClickOnControlStepsExactlyOnce()
        {
            decimal? value = 5m;
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-input-number__decrease").Click();

            Assert.Equal(4m, value);
        }

        /// <summary>
        /// P0-4：真实浏览器里"按住→松开"会紧接着派发一个 click，
        /// 连发过之后必须吞掉它，否则末尾多走一步（EP 靠 mousedown 立即一步 + 无 click 监听规避，
        /// 本组件保留 click 作为快速点击通路，故显式吞掉）。
        /// </summary>
        [Fact]
        public void ClickAfterHoldIsSwallowed()
        {
            decimal? value = 0m;
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.Max, 100m));

            cut.Find(".el-input-number__increase").MouseDown();
            cut.WaitForState(() => value >= 2m, timeout: System.TimeSpan.FromSeconds(6));
            cut.Find(".el-input-number__increase").MouseUp();
            var afterHold = value;

            cut.Find(".el-input-number__increase").Click();

            Assert.Equal(afterHold, value);
        }

        /// <summary>P1：decrease-icon / increase-icon 插槽整体替换默认图标（EP 同名插槽契约）。</summary>
        [Fact]
        public void IconSlotsReplaceDefaultIcons()
        {
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 5m)
                .Add(x => x.DecreaseIcon, builder =>
                {
                    builder.OpenElement(0, "i");
                    builder.AddAttribute(1, "class", "my-minus");
                    builder.CloseElement();
                })
                .Add(x => x.IncreaseIcon, builder =>
                {
                    builder.OpenElement(0, "i");
                    builder.AddAttribute(1, "class", "my-plus");
                    builder.CloseElement();
                }));

            Assert.Single(cut.FindAll(".el-input-number__decrease .my-minus"));
            Assert.Single(cut.FindAll(".el-input-number__increase .my-plus"));
            Assert.Empty(cut.FindAll(".el-input-number__decrease .el-icon-minus"));
            Assert.Empty(cut.FindAll(".el-input-number__increase .el-icon-plus"));
        }

        /// <summary>P1：不传插槽时默认图标一字不变（避免"加了插槽就弄坏现有页面"）。</summary>
        [Fact]
        public void DefaultIconsRenderWhenSlotsAreNotSet()
        {
            var cut = Render<ElInputNumber>(parameters => parameters.Add(x => x.Controls, true).Add(x => x.Value, 5m));

            Assert.Single(cut.FindAll(".el-input-number__decrease .el-icon-minus"));
            Assert.Single(cut.FindAll(".el-input-number__increase .el-icon-plus"));
        }

        /// <summary>P2-3：不传无障碍标签时，DOM 与改动前逐字一致（仍是英文 decrease / increase）。</summary>
        [Fact]
        public void ControlAriaLabelsDefaultToExistingEnglishTokens()
        {
            var cut = Render<ElInputNumber>(parameters => parameters.Add(x => x.Controls, true).Add(x => x.Value, 5m));

            Assert.Equal("decrease", cut.Find(".el-input-number__decrease").GetAttribute("aria-label"));
            Assert.Equal("increase", cut.Find(".el-input-number__increase").GetAttribute("aria-label"));
        }

        /// <summary>P2-3：EP 的 t('el.inputNumber.*') 意图 = 标签可本地化，这里用组件级参数兑现。</summary>
        [Fact]
        public void ControlAriaLabelsCanBeLocalized()
        {
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 5m)
                .Add(x => x.DecreaseAriaLabel, "减小")
                .Add(x => x.IncreaseAriaLabel, "增加"));

            Assert.Equal("减小", cut.Find(".el-input-number__decrease").GetAttribute("aria-label"));
            Assert.Equal("增加", cut.Find(".el-input-number__increase").GetAttribute("aria-label"));
            Assert.Empty(cut.FindAll("[aria-label=\"decrease\"]"));
            Assert.Empty(cut.FindAll("[aria-label=\"increase\"]"));
        }

        /// <summary>P2-3：controls-position="right" 是另一套模板分支，覆盖参数必须同样生效。</summary>
        [Fact]
        public void ControlAriaLabelsApplyInRightControlsPosition()
        {
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 5m)
                .Add(x => x.ControlsPosition, "right")
                .Add(x => x.DecreaseAriaLabel, "减小")
                .Add(x => x.IncreaseAriaLabel, "增加"));

            Assert.Equal("增加", cut.Find(".el-input-number__increase").GetAttribute("aria-label"));
            Assert.Equal("减小", cut.Find(".el-input-number__decrease").GetAttribute("aria-label"));
        }

        /// <summary>P2-3：传 null / 空白回落默认值——空的 aria-label 是无障碍反模式，不能把按钮变成无名元素。</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void BlankControlAriaLabelFallsBackToDefault(string label)
        {
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 5m)
                .Add(x => x.DecreaseAriaLabel, label)
                .Add(x => x.IncreaseAriaLabel, label));

            Assert.Equal("decrease", cut.Find(".el-input-number__decrease").GetAttribute("aria-label"));
            Assert.Equal("increase", cut.Find(".el-input-number__increase").GetAttribute("aria-label"));
        }

        /// <summary>P1：Formatter 只作用于「值→显示」，输入中的原文不被二次格式化。</summary>
        [Fact]
        public void FormatterAppliesToDisplayedValue()
        {
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 1234m)
                .Add(x => x.Formatter, text => string.IsNullOrEmpty(text) ? text : text + " %"));

            Assert.Equal("1234 %", cut.Find("input").GetAttribute("value"));
        }

        /// <summary>P1：Parser 把带格式的文本还原成数字，提交后模型拿到真实数值。</summary>
        [Fact]
        public void ParserTurnsFormattedTextIntoValue()
        {
            decimal? value = 1m;
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.Parser, text => text.Replace(",", string.Empty).Replace(" %", string.Empty))
                .Add(x => x.Formatter, text => string.IsNullOrEmpty(text) ? text : text + " %"));

            cut.Find("input").Change("2,500 %");

            Assert.Equal(2500m, value);
        }

        /// <summary>P1：Parser 给出非法结果时按 EP 语义保持原值，不写入垃圾。</summary>
        [Fact]
        public void InvalidParserResultKeepsPreviousValue()
        {
            decimal? value = 7m;
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.Parser, text => "非数字"));

            cut.Find("input").Change("随便写点什么");

            Assert.Equal(7m, value);
        }

        /// <summary>P1：Formatter 抛异常不得炸掉渲染（EP 没这层保护，我们加上并说明为有意差异）。</summary>
        [Fact]
        public void ThrowingFormatterFallsBackToPlainNumberText()
        {
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 9m)
                .Add(x => x.Formatter, text => throw new InvalidOperationException("bad formatter")));

            Assert.Equal("9", cut.Find("input").GetAttribute("value"));
        }

        /// <summary>P1：不传 Formatter/Parser 时行为一字不变（零回归）。</summary>
        [Fact]
        public void NoFormatterKeepsPlainNumericDisplay()
        {
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 42m)
                .Add(x => x.Precision, 2));

            Assert.Equal("42.00", cut.Find("input").GetAttribute("value"));
        }

        /// <summary>P1：EP 的 InputNumber 只暴露 focus / blur 两个方法，我们此前一个都没有。</summary>
        [Fact]
        public async Task ExposesFocusAndBlurLikeElementPlus()
        {
            var cut = Render<ElInputNumber>(parameters => parameters.Add(x => x.Value, 5m));

            await cut.Instance.FocusAsync();
            Assert.Contains(JSInterop.Invocations, i => i.Identifier == "execFocus");

            await cut.Instance.BlurAsync();
            Assert.Contains(JSInterop.Invocations, i => i.Identifier == "execBlur");
        }

        /// <summary>P1：@ref 不能把原有的属性/事件顺序弄坏（同元素内捕获帧之后不得再加属性）。</summary>
        [Fact]
        public void RefDoesNotDisturbExistingDomContract()
        {
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 5m)
                .Add(x => x.Id, "my-num")
                .Add(x => x.Align, "right"));

            var input = cut.Find("input");
            Assert.Equal("my-num", input.GetAttribute("id"));
            Assert.Equal("spinbutton", input.GetAttribute("role"));
            Assert.Equal("5", input.GetAttribute("value"));
            Assert.Contains("is-right", cut.Find(".el-input-number").ClassList);
        }

        [Fact]
        public void DisabledDoesNotRenderClearButton()
        {
            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 5m)
                .Add(x => x.Clearable, true)
                .Add(x => x.ShowClearIcon, true)
                .Add(x => x.Disabled, true));

            Assert.Empty(cut.FindAll(".el-input__clear"));
        }
    }
}
