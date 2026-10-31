using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 字段组件放进「脱离 &lt;ElForm&gt; 的 ElFormItem」必须能渲染。
    ///
    /// 触发场景：本项目对话框编辑页（如 BaseInfo/YwClassEdit.razor、StoreHouseEdit.razor）用
    /// Blazor 原生 <c>EditForm</c> 做验证、外层只写 <c>ElFormItem</c> 排版，全页没有 <c>&lt;ElForm&gt;</c>。
    /// 0.10.3 只修了 <see cref="ElFormItem{TValue}"/> 自己，字段组件仍在 <c>OnParametersSet</c> 首帧里
    /// 裸解引用 <c>FormItem.Form</c>。真实服务端实测（Blazor Server 静态预渲染，证据
    /// E:\电子档案\_probe_formitem\probe_result_0103.log / probe_result_0104.log）：
    /// <code>
    /// /probe/f/input   500 → 200   NRE at Element.ElInput`1.get_HasValidationStatusIcon() -> get_HasSuffix() -> OnParametersSet()
    /// /probe/f/number  500 → 200   NRE at Element.ElInputNumber.OnParametersSet()
    /// /probe/f/switch  500 → 200   NRE at Element.ElSwitch`1.OnParametersSet()
    /// /probe/f/date    500 → 200   NRE at Element.ElDatePicker.OnParametersSet()
    /// /probe/f/select  500 → 200   NRE at Element.ElSelect`1.OnParametersSet()
    /// /probe/f/checkbox 200 → 200  一直安全（ElCheckbox 不碰 Form，见 ElCheckbox.razor.cs:106）——本类的对照组
    /// </code>
    /// 修复：库里 23 个文件 / 24 处 <c>FormItem.Form.Values.Any()</c>、<c>FormItem.Form.StatusIcon</c>
    /// 统一改成 <c>FormItem.Form?.. ?? false</c>（逐站点 diff 见 _guard_formitem_diff.log）。
    /// <c>FormItem.Form.RegisterInput(...)</c> 五处保持原样，它们本来就在 <c>if (FormItem?.Form != null)</c> 里。
    /// </summary>
    public class ElFieldInStandaloneFormItemTests : BunitContext
    {
        public ElFieldInStandaloneFormItemTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static RenderFragment Field(Type componentType, params (string Name, object Value)[] attrs)
        {
            return builder =>
            {
                var seq = 0;
                builder.OpenComponent(seq++, componentType);
                foreach (var a in attrs)
                {
                    builder.AddAttribute(seq++, a.Name, a.Value);
                }
                builder.CloseComponent();
            };
        }

        /// <summary>首帧 + 两次重渲染：只在首帧不崩不算过（OnParametersSet 每帧都走）。</summary>
        private void AssertStandaloneFieldRenders(RenderFragment field)
        {
            var cut = Render<ElFormItem<string>>(p => p
                .Add(x => x.Label, "测试字段")
                .Add(x => x.Required, true)
                .AddChildContent(field));

            cut.Render();
            cut.Render();
            Assert.NotNull(cut.Find(".el-form-item"));
            Assert.Single(cut.FindAll(".el-form-item"));
        }

        [Fact]
        public void ElInputInStandaloneFormItemRenders()
        {
            AssertStandaloneFieldRenders(Field(typeof(ElInput<string>), ("Value", (object)"abc")));
        }

        [Fact]
        public void ElInputNumberInStandaloneFormItemRenders()
        {
            AssertStandaloneFieldRenders(Field(typeof(ElInputNumber),
                ("Value", (object)(decimal?)5),
                ("Min", (object)(decimal?)0),
                ("Max", (object)(decimal?)100)));
        }

        [Fact]
        public void ElSwitchInStandaloneFormItemRenders()
        {
            AssertStandaloneFieldRenders(Field(typeof(ElSwitch<bool>), ("Value", (object)true)));
        }

        [Fact]
        public void ElDatePickerInStandaloneFormItemRenders()
        {
            AssertStandaloneFieldRenders(Field(typeof(ElDatePicker),
                ("Value", (object)(DateTime?)new DateTime(2026, 10, 17))));
        }

        [Fact]
        public void ElSelectInStandaloneFormItemRenders()
        {
            var options = (RenderFragment)(b =>
            {
                var seq = 0;
                foreach (var pair in new[] { "a|甲", "b|乙" })
                {
                    var bits = pair.Split('|');
                    b.OpenComponent(seq++, typeof(ElOption<string>));
                    b.AddAttribute(seq++, "Value", bits[0]);
                    b.AddAttribute(seq++, "Label", bits[1]);
                    b.CloseComponent();
                }
            });

            AssertStandaloneFieldRenders(Field(typeof(ElSelect<string>),
                ("Value", (object)"b"),
                ("ChildContent", options)));
        }

        /// <summary>对照组：ElCheckbox 本来就不碰 Form，修复前后都该绿；它绿而其它红，说明崩点确在 Form 解引用。</summary>
        [Fact]
        public void ElCheckboxInStandaloneFormItemRenders()
        {
            AssertStandaloneFieldRenders(Field(typeof(ElCheckbox<bool>), ("Value", (object)true)));
        }

        /// <summary>对照组：字段在 &lt;ElForm&gt; 里时走的是原有分支，行为不能变。</summary>
        [Fact]
        public void ElInputInsideFormStillRenders()
        {
            var cut = Render<ElForm>(p => p
                .AddChildContent<ElFormItem<string>>(item => item
                    .Add(x => x.Label, "测试字段")
                    .AddChildContent(Field(typeof(ElInput<string>), ("Value", (object)"abc")))));

            cut.Render();
            Assert.Single(cut.FindAll(".el-form-item"));
            Assert.Equal("abc", cut.Find("input").GetAttribute("value"));
        }
    }
}
