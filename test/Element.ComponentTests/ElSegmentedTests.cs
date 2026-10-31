using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElSegmentedTests : BunitContext
    {
        public ElSegmentedTests()
        {
            Services.AddElementServices();
        }

        private static List<SegmentedOption> TwoOptions() => new List<SegmentedOption>
        {
            new SegmentedOption { Label = "A", Value = "a" },
            new SegmentedOption { Label = "B", Value = "b" }
        };

        [Fact]
        public void RendersVerticalDirectionClass()
        {
            var cut = Render<ElSegmented<string>>(p => p
                .Add(x => x.Direction, SegmentedDirection.Vertical)
                .Add(x => x.Options, TwoOptions()));

            Assert.Contains("is-vertical", cut.Find(".el-segmented").ClassList);
        }

        [Fact]
        public void PrimaryButtonTypeAddsThemeClass()
        {
            var cut = Render<ElSegmented<string>>(p => p
                .Add(x => x.ButtonType, "primary")
                .Add(x => x.Options, TwoOptions()));

            Assert.Contains("el-segmented--primary", cut.Find(".el-segmented").ClassList);
        }

        [Fact]
        public void ItemTemplateOverridesDefaultContent()
        {
            var cut = Render<ElSegmented<string>>(p => p
                .Add(x => x.Options, TwoOptions())
                .Add(x => x.ItemTemplate, (RenderFragment<SegmentedOption>)(opt => builder =>
                    builder.AddContent(0, "Tpl:" + opt.Label))));

            Assert.Contains("Tpl:A", cut.Markup);
            Assert.Contains("Tpl:B", cut.Markup);
        }

        [Fact]
        public void SelectingOptionRaisesOnChange()
        {
            string selected = null;
            var cut = Render<ElSegmented<string>>(p => p
                .Add(x => x.Options, TwoOptions())
                .Add(x => x.OnChange, v => selected = v));

            cut.FindAll("input[type=radio]")[1].Change("b");

            Assert.Equal("b", selected);
        }

        [Fact]
        public async Task ValidateEventTriggersFormValidationOnSelect()
        {
            var cut = Render<ElForm>(parameters => parameters
                .AddChildContent<ElFormItem<string>>(item => item
                    .Add(x => x.Name, "seg")
                    .Add(x => x.Required, true)
                    .AddChildContent<ElSegmented<string>>(seg => seg
                        .Add(x => x.Options, TwoOptions()))));

            // 初始为空 -> 必填校验失败，form-item 进入错误态
            await cut.InvokeAsync(() => cut.Instance.Validate());
            cut.Render();
            Assert.Contains("is-error", cut.Find(".el-form-item").ClassList);

            // 选中第二项（默认 ValidateEvent=true）应触发表单重校验并清除错误
            cut.FindAll("input[type=radio]")[1].Change("b");
            cut.Render();
            Assert.DoesNotContain("is-error", cut.Find(".el-form-item").ClassList);
        }
    }
}
