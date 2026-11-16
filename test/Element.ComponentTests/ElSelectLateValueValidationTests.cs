using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 复现用户真实项目场景：ElForm.Model 属性带 [Required]（英文名报错
    /// "The 报修科室 field is required"），ElFormItem.Name 与属性同名，
    /// ElSelect 绑定值在首渲染之后才赋值（OnAfterRenderAsync + StateHasChanged）。
    /// 验证点：值填充后 Form.Validate() 是否仍误报 required。
    /// </summary>
    public class ElSelectLateValueValidationTests : BunitContext
    {
        public ElSelectLateValueValidationTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static string TriggerText(IRenderedComponent<FormHost> cut) =>
            cut.Find(".el-select input.el-input__inner").GetAttribute("value") ?? string.Empty;

        [Fact]
        public async Task LateAssignmentThenValidate_ShouldPass()
        {
            var cut = Render<FormHost>();
            cut.WaitForAssertion(() => Assert.Equal("内科", TriggerText(cut)), timeout: TestWait.Timeout);
            var ok = await cut.InvokeAsync(() => cut.Instance.Validate());
            Assert.True(ok, "值已填充后再校验不应再报 required");
        }

        [Fact]
        public async Task ErrorShownBeforeFill_ShouldClearAfterLateAssignment()
        {
            var cut = Render<FormHost>(parameters => parameters.Add(x => x.AssignMode, "manual"));
            // 值为空时先报一次错（模拟用户先点提交，红字出现）
            var first = await cut.InvokeAsync(() => cut.Instance.Validate());
            Assert.False(first);
            Assert.NotNull(cut.Find(".el-form-item__error"));
            Assert.Contains("报修科室", cut.Find(".el-form-item__error").TextContent);

            // 值异步填充 + 重渲染（模板 Home.razor 时序）
            await cut.InvokeAsync(() => cut.Instance.Assign());
            cut.WaitForAssertion(() => Assert.Equal("内科", TriggerText(cut)), timeout: TestWait.Timeout);

            // 回显同步落库后，旧错误必须自动消失（Element UI 语义：值变化即重校验）
            cut.WaitForAssertion(() =>
                Assert.Empty(cut.FindAll(".el-form-item__error")), timeout: TestWait.Timeout);
        }

        public class RepairModel
        {
            [Required]
            public string 报修科室 { get; set; }
        }

        private sealed class FormHost : ComponentBase
        {
            [Parameter]
            public string AssignMode { get; set; }

            public RepairModel Model { get; } = new RepairModel();
            private ElForm form;
            public bool ValidateResult;

            public bool Validate()
            {
                ValidateResult = form.Validate();
                return ValidateResult;
            }

            public void Assign()
            {
                Model.报修科室 = "k01";
                StateHasChanged();
            }

            private static readonly (string Value, string Name)[] Departments =
            {
                ("k01", "内科"), ("k02", "外科"), ("k03", "儿科"),
            };

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                if (firstRender && AssignMode != "manual")
                {
                    if (string.IsNullOrWhiteSpace(Model.报修科室))
                        Model.报修科室 = "k01";
                    await InvokeAsync(StateHasChanged);
                }
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<ElDialog>(0);
                builder.AddAttribute(1, nameof(ElDialog.ModelValue), false);
                builder.AddAttribute(2, nameof(ElDialog.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenComponent<ElForm>(0);
                    b.AddAttribute(1, nameof(ElForm.Model), Model);
                    b.AddAttribute(3, nameof(ElForm.ChildContent), (RenderFragment)(b2 =>
                    {
                        b2.OpenComponent<ElFormItem<string>>(0);
                        b2.AddAttribute(1, nameof(ElFormItem<string>.Name), "报修科室");
                        b2.AddAttribute(2, nameof(ElFormItem<string>.Label), "报修科室");
                        b2.AddAttribute(3, nameof(ElFormItem<string>.ChildContent), (RenderFragment)(b3 =>
                        {
                            b3.OpenComponent<ElSelect<string>>(0);
                            b3.AddAttribute(1, nameof(ElSelect<string>.Value), Model.报修科室);
                            b3.AddAttribute(2, nameof(ElSelect<string>.ValueChanged),
                                EventCallback.Factory.Create<string>(this, v => { Model.报修科室 = v; StateHasChanged(); }));
                            b3.AddAttribute(3, nameof(ElSelect<string>.Placeholder), "请选择");
                            b3.AddAttribute(4, nameof(ElSelect<string>.ChildContent), (RenderFragment)(b4 =>
                            {
                                var seq = 0;
                                foreach (var (value, name) in Departments)
                                {
                                    b4.OpenComponent<ElOption<string>>(seq++);
                                    b4.AddAttribute(seq++, nameof(ElOption<string>.Value), value);
                                    b4.AddAttribute(seq++, nameof(ElOption<string>.Text), name);
                                    b4.CloseComponent();
                                }
                            }));
                            b3.CloseComponent();
                        }));
                        b2.CloseComponent();
                    }));
                    b.AddComponentReferenceCapture(4, v => form = (ElForm)v);
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }
    }
}
