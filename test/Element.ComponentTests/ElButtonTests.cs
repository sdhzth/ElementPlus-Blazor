using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElButtonTests : BunitContext
    {
        public ElButtonTests()
        {
            Services.AddElementPlusServices();
        }

        [Fact]
        public void Icon_LeftPosition_AddsElIconLeftClass()
        {
            var cut = Render<ElButton>(parameters => parameters
                .Add(x => x.Icon, "el-icon-search")
                .AddChildContent("搜索"));

            var i = cut.Find("i");
            Assert.Contains("el-icon--left", i.ClassList);
            Assert.DoesNotContain("el-icon--right", i.ClassList);
        }

        [Fact]
        public void Icon_RightPosition_AddsElIconRightClass()
        {
            var cut = Render<ElButton>(parameters => parameters
                .Add(x => x.Icon, "el-icon-edit")
                .Add(x => x.IconPosition, ElButton.IconPositionMode.Right)
                .AddChildContent("编辑"));

            var i = cut.Find("i");
            Assert.Contains("el-icon--right", i.ClassList);
            Assert.DoesNotContain("el-icon--left", i.ClassList);
        }

        [Fact]
        public void IconOnlyButton_NoSideClass_ToAvoidCenterOffset()
        {
            var cut = Render<ElButton>(parameters => parameters
                .Add(x => x.Icon, "el-icon-search"));

            var i = cut.Find("i");
            Assert.DoesNotContain("el-icon--left", i.ClassList);
            Assert.DoesNotContain("el-icon--right", i.ClassList);
        }

        [Fact]
        public void Text_Link_Dashed_RenderExpectedClasses()
        {
            var text = Render<ElButton>(p => p.Add(x => x.Text, true).AddChildContent("t"));
            var link = Render<ElButton>(p => p.Add(x => x.Link, true).AddChildContent("l"));
            var dashed = Render<ElButton>(p => p.Add(x => x.Dashed, true).AddChildContent("d"));

            Assert.Contains("is-text", text.Find("button").ClassList);
            Assert.Contains("is-link", link.Find("button").ClassList);
            Assert.Contains("is-dashed", dashed.Find("button").ClassList);
        }

        [Fact]
        public void ButtonGroup_CascadesTypeAndSizeToChildren()
        {
            var cut = Render<ElButtonGroup>(parameters => parameters
                .Add(x => x.Type, ButtonType.Primary)
                .Add(x => x.Size, ButtonSize.Small)
                .AddChildContent<ElButton>(btn => btn.AddChildContent("A"))
                .AddChildContent<ElButton>(btn => btn.AddChildContent("B")));

            var buttons = cut.FindAll("button.el-button");
            Assert.Equal(2, buttons.Count);
            foreach (var b in buttons)
            {
                Assert.Contains("el-button--primary", b.ClassList);
                Assert.Contains("el-button--small", b.ClassList);
            }
        }

        [Fact]
        public void ButtonGroup_Disabled_CascadesToChildren()
        {
            var cut = Render<ElButtonGroup>(parameters => parameters
                .Add(x => x.Disabled, true)
                .AddChildContent<ElButton>(btn => btn.AddChildContent("A")));

            var b = cut.Find("button.el-button");
            Assert.Contains("is-disabled", b.ClassList);
            Assert.True(b.HasAttribute("disabled"));
            Assert.Equal("true", b.GetAttribute("aria-disabled"));
        }

        [Fact]
        public void ButtonGroup_Vertical_AddsIsVerticalClass()
        {
            var cut = Render<ElButtonGroup>(parameters => parameters
                .Add(x => x.Direction, "vertical")
                .AddChildContent<ElButton>(btn => btn.AddChildContent("A")));

            var group = cut.Find(".el-button-group");
            Assert.Contains("is-vertical", group.ClassList);
        }

        [Fact]
        public void FocusAsync_OnUnrenderedReference_DoesNotThrow()
        {
            var cut = Render<ElButton>(p => p.AddChildContent("x"));
            var btn = cut.Instance;
            // ButtonReference 尚未通过 DOM 捕获时应为 default，FocusAsync 应安全跳过而非抛异常
            var task = btn.FocusAsync();
            Assert.True(task.IsCompleted || task.IsCompletedSuccessfully);
        }

        // ============ EP Exposes（size / type / disabled / shouldAddSpace）============

        [Theory]
        [InlineData("确定", true)]
        [InlineData("取消", true)]
        [InlineData("OK", false)]          // 含非中文字符 → 不插空格
        [InlineData("确定X", false)]       // 长度≠2 → 不插空格
        [InlineData("三", false)]          // 长度≠2 → 不插空格
        public void Exposes_ShouldAddSpace_ReflectsTwoChineseChars(string text, bool expected)
        {
            var cut = Render<ElButton>(p => p
                .Add(x => x.AutoInsertSpace, true)
                .Add(x => x.Content, text));

            Assert.Equal(expected, cut.Instance.ShouldAddSpace);
        }

        [Fact]
        public void Exposes_ShouldAddSpace_FalseWhenAutoInsertSpaceOff()
        {
            var cut = Render<ElButton>(p => p
                .Add(x => x.AutoInsertSpace, false)
                .Add(x => x.Content, "确定"));

            Assert.False(cut.Instance.ShouldAddSpace);
        }

        [Fact]
        public void Exposes_ResolvedType_Size_Disabled_ReflectEffectiveState()
        {
            var cut = Render<ElButton>(p => p
                .Add(x => x.Type, ButtonType.Success)
                .Add(x => x.Size, ButtonSize.Small)
                .Add(x => x.Disabled, true)
                .AddChildContent("x"));

            Assert.Equal(ButtonType.Success, cut.Instance.ResolvedType);
            Assert.Equal(ButtonSize.Small, cut.Instance.ResolvedSize);
            Assert.True(cut.Instance.ResolvedDisabled);
        }

        [Fact]
        public void Exposes_ResolvedDisabled_TrueWhenLoading()
        {
            var cut = Render<ElButton>(p => p
                .Add(x => x.Loading, true)
                .AddChildContent("x"));

            Assert.True(cut.Instance.ResolvedDisabled);
            Assert.Equal("true", cut.Find("button").GetAttribute("aria-busy"));
        }

        [Fact]
        public void Exposes_ResolvedType_Size_InheritsFromButtonGroup()
        {
            var group = Render<ElButtonGroup>(parameters => parameters
                .Add(x => x.Type, ButtonType.Primary)
                .Add(x => x.Size, ButtonSize.Small)
                .AddChildContent<ElButton>(btn => btn.AddChildContent("A")));

            var child = group.FindComponent<ElButton>();
            Assert.Equal(ButtonType.Primary, child.Instance.ResolvedType);
            Assert.Equal(ButtonSize.Small, child.Instance.ResolvedSize);
        }
    }
}
