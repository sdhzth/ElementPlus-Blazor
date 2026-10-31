using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElFormItem 的级联 Form 是可选的：脱离 &lt;ElForm&gt; 单独排版（对话框面板里 EditForm + ElFormItem）
    /// 也必须能渲染。0.10.2 及以前 OnParametersSet/渲染体裸解引用 Form，直接 NullReferenceException。
    /// </summary>
    public class ElFormItemWithoutFormTests : BunitContext
    {
        public ElFormItemWithoutFormTests()
        {
            Services.AddElementServices();
        }

        private IRenderedComponent<ElFormItem<string>> RenderStandaloneItem()
        {
            return Render<ElFormItem<string>>(parameters => parameters
                .Add(x => x.Label, "输入密码")
                .Add(x => x.Required, true)
                .AddChildContent("<span>field</span>"));
        }

        [Fact]
        public void StandaloneFormItemRendersWithoutFormAncestor()
        {
            var cut = RenderStandaloneItem();
            // 二次渲染走 diff 路径：只在首帧不崩不算过
            cut.Render();
            cut.Render();

            var item = cut.Find(".el-form-item");
            Assert.Contains("is-required", item.ClassList);
            Assert.Contains("输入密码", cut.Find(".el-form-item__label").InnerHtml);
            Assert.True(string.IsNullOrEmpty(cut.Find(".el-form-item__label").GetAttribute("for")));
        }

        [Fact]
        public void FormItemInsideFormStillRenders()
        {
            var cut = Render<ElForm>(parameters => parameters
                .AddChildContent<ElFormItem<string>>(item => item
                    .Add(x => x.Label, "输入密码")
                    .AddChildContent("<span>field</span>")));

            cut.Render();
            Assert.Single(cut.FindAll(".el-form-item"));
        }
    }
}
