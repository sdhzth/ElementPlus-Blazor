using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 复现模板项目 Home.razor 的场景：ElDialog（关闭态，DestroyOnClose 默认 false ⇒ 子内容已渲染）
    /// 里 ElForm/ElFormItem 包裹的 ElSelect，绑定值在 <b>首渲染之后</b> 才赋值
    /// （OnAfterRenderAsync(firstRender) 中 Model.CLRY = UserList[0].Value + StateHasChanged）。
    /// <para>
    /// 疑似根因：ElSelect.OnParametersSet 在 FormItem 分支里靠
    /// <c>FormItem.OriginValueHasRendered</c> 守卫一次性回显，首帧之后所有参数更新直接 return，
    /// Label 不再随新 Value 重算 ⇒ 触发器一直是占位符。
    /// </para>
    /// 对照档：同样结构在 OnInitialized（首渲染前）赋值，应正常回显。
    /// </summary>
    public class ElSelectFormItemLateValueTests : BunitContext
    {
        public ElSelectFormItemLateValueTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static string TriggerText(IRenderedComponent<FormHost> cut) =>
            cut.Find(".el-select input.el-input__inner").GetAttribute("value") ?? string.Empty;

        [Fact]
        public void EarlyAssignmentInOnInitializedEchoesOptionText()
        {
            var cut = Render<FormHost>(parameters => parameters.Add(x => x.AssignMode, "early"));
            cut.WaitForAssertion(() => Assert.Equal("张三", TriggerText(cut)), timeout: TestWait.Timeout);
        }

        [Fact]
        public void LateAssignmentAfterFirstRender_EchoesOptionText_InFormItem()
        {
            var cut = Render<FormHost>(parameters => parameters.Add(x => x.AssignMode, "late"));
            cut.WaitForAssertion(() => Assert.Equal("1", cut.Instance.CLRY), timeout: TestWait.Timeout);
            // 修复后：首帧之后赋的值也要回显（模板项目 Home.razor 场景）
            cut.WaitForAssertion(() => Assert.Equal("张三", TriggerText(cut)), timeout: TestWait.Timeout);
        }

        private sealed class FormHost : ComponentBase
        {
            [Parameter]
            public string AssignMode { get; set; }

            public string CLRY { get; set; }

            private static readonly string[] Names = { "张三", "李四", "王五", "赵六" };

            protected override void OnInitialized()
            {
                if (AssignMode == "early")
                    CLRY = "1";
            }

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                if (firstRender && AssignMode == "late")
                {
                    // 与模板 Home.razor 相同的时机：首渲染后才赋值
                    if (string.IsNullOrWhiteSpace(CLRY))
                        CLRY = "1";
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
                    b.AddAttribute(1, nameof(ElForm.Model), this);
                    b.AddAttribute(2, nameof(ElForm.ChildContent), (RenderFragment)(b2 =>
                    {
                        b2.OpenComponent<ElFormItem<string>>(0);
                        b2.AddAttribute(1, nameof(ElFormItem<string>.Name), "处理人员");
                        b2.AddAttribute(2, nameof(ElFormItem<string>.Label), "处理人员");
                        b2.AddAttribute(3, nameof(ElFormItem<string>.ChildContent), (RenderFragment)(b3 =>
                        {
                            b3.OpenComponent<ElSelect<string>>(0);
                            b3.AddAttribute(1, nameof(ElSelect<string>.Value), CLRY);
                            b3.AddAttribute(3, nameof(ElSelect<string>.Placeholder), "请选择");
                            b3.AddAttribute(4, nameof(ElSelect<string>.ChildContent), (RenderFragment)(b4 =>
                            {
                                var seq = 0;
                                for (var i = 0; i < Names.Length; i++)
                                {
                                    var value = (i + 1).ToString();
                                    b4.OpenComponent<ElOption<string>>(seq++);
                                    b4.AddAttribute(seq++, nameof(ElOption<string>.Value), value);
                                    b4.AddAttribute(seq++, nameof(ElOption<string>.Text), Names[i]);
                                    b4.CloseComponent();
                                }
                            }));
                            b3.CloseComponent();
                        }));
                        b2.CloseComponent();
                    }));
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }
    }
}
