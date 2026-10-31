using Bunit;
using ElementPlus;
using ElementPlus.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 字符串型 <see cref="ElSelect{TValue}"/> 放进 <c>&lt;ElForm Model&gt; + &lt;ElFormItem Name&gt;</c> 后，
    /// 绑定值必须是选项的<b>键</b>，不许被内层显示框的<b>文本</b>反写。
    /// <para>
    /// 缺陷原样（消费方实测取证，2026-10-22，E:\电子病历\netetl\logs\m5-select-label-writeback-before.log）：
    /// <c>ElSelect.razor:54</c> 的内层显示框是 <c>&lt;ElInput TValue="string" Value="@DisplayLabel"&gt;</c>，
    /// 而它落在 <c>ElFormItem.razor:102</c> 的 <c>&lt;CascadingValue Value="this"&gt;</c> 子树里 ——
    /// <c>TValue=string</c> 时内层 <c>ElInput&lt;string&gt;</c> 的 <c>[CascadingParameter] ElFormItem&lt;string&gt;</c>
    /// 命中的正是外层那一个字段项（整型/枚举下拉级联类型不匹配，所以这个洞一直没暴露）。
    /// 于是 <c>ElInput.OnParametersSet</c> 的「父级改了 Value」分支把显示文本当成字段值写进链路：
    /// <code>
    /// Element.FormModelPath.TrySetValue
    ///  &lt;= Element.ElForm.NotifyFieldValueChanged
    ///  &lt;= Element.ElFormItemObject.NotifyValueChanged
    ///  &lt;= Element.ElFormItem`1[string].set_Value
    ///  &lt;= Element.ElInput`1[string].OnParametersSet        ← 显示框在写业务模型
    /// </code>
    /// 现场两条读数：① 筛选栏选「正常」→ 模型先拿到正确的 "0"，250ms 后一轮渲染被改写成「正常」，
    /// 请求发成 <c>status=%E6%AD%A3%E5%B8%B8</c>、返回 0 行；② 编辑弹窗里 <c>edit.Sex="0"</c> 一渲染成「男」，
    /// 点确定会把「男」POST 进库 —— 不只是筛选失真，是写坏数据。
    /// </para>
    /// <para>
    /// 修法：<see cref="ElInput{TValue}.SuppressFormWriteBack"/>（默认 false，直用 ElInput 的行为逐字节不变），
    /// ElSelect 的内层显示框置 true —— 该输入框只负责显示，字段同步交回 <c>ElSelect.SelectValueAsync</c> 一条路；
    /// 它的 id 仍然注册给表单（<c>label for</c> 与 ScrollToField 不塌）。
    /// </para>
    /// <para>判据成对：负向（模型里必须是键 / 输入框文本不许进模型）都配同前提正向（显示框里必须是文本、
    /// 普通 ElInput 的表单写回必须照常工作），否则「一律不写」也能全绿。</para>
    /// </summary>
    public class ElSelectFormItemWriteBackTests : BunitContext
    {
        public ElSelectFormItemWriteBackTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private class Query
        {
            public string Status { get; set; }
            public string UserName { get; set; }
        }

        private static RenderFragment StatusOptions() => builder =>
        {
            builder.OpenComponent<ElOption<string>>(0);
            builder.AddAttribute(1, nameof(ElOption<string>.Value), "0");
            builder.AddAttribute(2, nameof(ElOption<string>.Text), "正常");
            builder.CloseComponent();

            builder.OpenComponent<ElOption<string>>(3);
            builder.AddAttribute(4, nameof(ElOption<string>.Value), "1");
            builder.AddAttribute(5, nameof(ElOption<string>.Text), "停用");
            builder.CloseComponent();
        };

        private class SelectInFormHost : ComponentBase
        {
            [Parameter] public string InitialStatus { get; set; }
            [Parameter] public bool Filterable { get; set; }
            public Query Model { get; } = new Query();

            protected override void OnInitialized() => Model.Status = InitialStatus;

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<ElForm>(0);
                builder.AddAttribute(1, nameof(ElForm.Model), Model);
                builder.AddAttribute(2, "Inline", true);
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(inner =>
                {
                    inner.OpenComponent<ElFormItem<string>>(0);
                    inner.AddAttribute(1, nameof(ElFormItem<string>.Name), "Status");
                    inner.AddAttribute(2, nameof(ElFormItem<string>.Label), "状态");
                    inner.AddAttribute(3, "ChildContent", (RenderFragment)(content =>
                    {
                        content.OpenComponent<ElSelect<string>>(0);
                        content.AddAttribute(1, nameof(ElSelect<string>.Value), Model.Status);
                        content.AddAttribute(2, nameof(ElSelect<string>.ValueChanged),
                            EventCallback.Factory.Create<string>(this, v => Model.Status = v));
                        content.AddAttribute(3, nameof(ElSelect<string>.Filterable), Filterable);
                        content.AddAttribute(4, nameof(ElSelect<string>.ChildContent), StatusOptions());
                        content.CloseComponent();
                    }));
                    inner.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }

        private class InputInFormHost : ComponentBase
        {
            public Query Model { get; } = new Query();

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<ElForm>(0);
                builder.AddAttribute(1, nameof(ElForm.Model), Model);
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(inner =>
                {
                    inner.OpenComponent<ElFormItem<string>>(0);
                    inner.AddAttribute(1, nameof(ElFormItem<string>.Name), "UserName");
                    inner.AddAttribute(2, nameof(ElFormItem<string>.Label), "用户账号");
                    inner.AddAttribute(3, "ChildContent", (RenderFragment)(content =>
                    {
                        content.OpenComponent<ElInput<string>>(0);
                        content.AddAttribute(1, nameof(ElInput<string>.Value), Model.UserName);
                        content.AddAttribute(2, nameof(ElInput<string>.ValueChanged),
                            EventCallback.Factory.Create<string>(this, v => Model.UserName = v));
                        content.CloseComponent();
                    }));
                    inner.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }

        private static string TriggerValue(IRenderedComponent<SelectInFormHost> cut) =>
            cut.Find(".el-select input.el-input__inner").GetAttribute("value");

        [Fact]
        public void SelectingOptionKeepsTheKeyInModelAcrossRenders()
        {
            var cut = Render<SelectInFormHost>(p => p.Add(x => x.InitialStatus, null));
            var select = cut.FindComponent<ElSelect<string>>().Instance;

            // 等价于用户点开下拉点了「正常」：走 ElOption 点击的同一条链路（OnInternalSelectAsync）。
            cut.InvokeAsync(() => select.OnInternalSelectAsync(new SelectResultModel<string>
            {
                Key = "0",
                Text = "正常"
            }));

            Assert.Equal("0", cut.Instance.Model.Status);

            // 缺陷就出在这一次重渲染之后：内层显示框拿到的 Value 从空变成「正常」，
            // 「父级改了 Value」分支把它当成字段值反写回模型。多渲染几轮，钉住它不许改写。
            cut.Render();
            cut.Render();
            Assert.Equal("0", cut.Instance.Model.Status);

            // 同前提正向对照：不写模型，但显示必须是文本 —— 否则「显示框根本没渲染」也能绿。
            Assert.Equal("正常", TriggerValue(cut));
        }

        [Fact]
        public void PresetKeyIsNotReplacedByItsOwnLabelOnFirstRender()
        {
            // 编辑弹窗那一档：模型里本来就是键 "0"，下拉只是把首屏文本扫成「正常」，
            // 谁也没点过，模型不许在这一轮渲染里变成「正常」。
            var cut = Render<SelectInFormHost>(p => p.Add(x => x.InitialStatus, "0"));

            cut.Render();
            cut.Render();

            Assert.Equal("0", cut.Instance.Model.Status);
            Assert.Equal("正常", TriggerValue(cut));
        }

        [Fact]
        public void TypedFilterTextStaysOutOfTheModel()
        {
            // Filterable 档同一个洞的另一半：键入的原文进的是显示框自己的 Value，
            // 没选中任何候选项之前不许写进字段（否则半截输入直接污染查询条件）。
            var cut = Render<SelectInFormHost>(p => p
                .Add(x => x.InitialStatus, null)
                .Add(x => x.Filterable, true));

            // 键入走 @oninput（ElInput.razor 上 @oninput="OnChangeEventArgs"、@onchange 只在
            // ValidateEvent 时才写值），所以用 .Input() 而不是 .Change()，否则测的是「没触发写」的空转绿。
            cut.Find(".el-select input.el-input__inner").Input("正常");
            cut.Render();

            Assert.NotEqual("正常", cut.Instance.Model.Status);
        }

        [Fact]
        public void PlainElInputInsideSameFormItemStillWritesTheModel()
        {
            // 尺子的牙：SuppressFormWriteBack 默认 false，普通 ElInput 的表单写回必须照常。
            // 这条绿而前面几条也绿，才说明改动只掐掉了「显示框反写」而不是整个字段链路。
            var cut = Render<InputInFormHost>();

            cut.Find("input.el-input__inner").Input("admin");
            cut.Render();

            Assert.Equal("admin", cut.Instance.Model.UserName);
        }
    }
}
