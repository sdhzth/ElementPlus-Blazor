using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElResultTests : BunitContext
    {
        public ElResultTests()
        {
            Services.AddElementServices();
        }

        [Fact]
        public void DefaultIconIsInfo()
        {
            var cut = Render<ElResult>(p => p.Add(x => x.Title, "T"));
            var icon = cut.Find("i.el-result-icon");
            Assert.Contains("el-result-icon--info", icon.ClassName);
            Assert.Contains("el-icon-info", icon.ClassName);
        }

        [Fact]
        public void PrimaryRendersPrimaryClassAndInfoGlyph()
        {
            var cut = Render<ElResult>(p => p.Add(x => x.Icon, ResultIcon.Primary));
            var icon = cut.Find("i.el-result-icon");
            Assert.Contains("el-result-icon--primary", icon.ClassName);
            // EP 2.9.11: primary 字形复用 InfoFilled（本库 el-icon-info）
            Assert.Contains("el-icon-info", icon.ClassName);
        }

        [Fact]
        public void SuccessRendersSuccessClassAndGlyph()
        {
            var cut = Render<ElResult>(p => p.Add(x => x.Icon, ResultIcon.Success));
            var icon = cut.Find("i.el-result-icon");
            Assert.Contains("el-result-icon--success", icon.ClassName);
            Assert.Contains("el-icon-success", icon.ClassName);
        }

        [Fact]
        public void WarningRendersWarningClassAndGlyph()
        {
            var cut = Render<ElResult>(p => p.Add(x => x.Icon, ResultIcon.Warning));
            var icon = cut.Find("i.el-result-icon");
            Assert.Contains("el-result-icon--warning", icon.ClassName);
            Assert.Contains("el-icon-warning", icon.ClassName);
        }

        [Fact]
        public void ErrorRendersErrorClassAndGlyph()
        {
            var cut = Render<ElResult>(p => p.Add(x => x.Icon, ResultIcon.Error));
            var icon = cut.Find("i.el-result-icon");
            Assert.Contains("el-result-icon--error", icon.ClassName);
            Assert.Contains("el-icon-error", icon.ClassName);
        }

        [Fact]
        public void TitleRendersWhenProvided()
        {
            var cut = Render<ElResult>(p => p.Add(x => x.Title, "提交成功"));
            var title = cut.Find(".el-result__title");
            Assert.Contains("提交成功", title.TextContent);
        }

        [Fact]
        public void SubTitleRendersWhenProvided()
        {
            var cut = Render<ElResult>(p => p.Add(x => x.SubTitle, "请稍候"));
            var sub = cut.Find(".el-result__subtitle");
            Assert.Contains("请稍候", sub.TextContent);
        }

        [Fact]
        public void IconContentSlotOverridesDefaultIcon()
        {
            var cut = Render<ElResult>(p => p
                .Add(x => x.Icon, ResultIcon.Error)
                .Add(x => x.IconContent, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"custom-result-icon\">X</span>"))));
            Assert.Single(cut.FindAll("span.custom-result-icon"));
            // 默认 <i> 图标不再渲染
            Assert.Empty(cut.FindAll("i.el-result-icon"));
        }

        [Fact]
        public void TitleContentSlotRenders()
        {
            var cut = Render<ElResult>(p => p
                .Add(x => x.TitleContent, (RenderFragment)(b => b.AddMarkupContent(0, "<b>标题插槽</b>"))));
            var title = cut.Find(".el-result__title");
            Assert.Contains("标题插槽", title.TextContent);
        }

        [Fact]
        public void SubTitleContentSlotRenders()
        {
            var cut = Render<ElResult>(p => p
                .Add(x => x.SubTitleContent, (RenderFragment)(b => b.AddMarkupContent(0, "<i>副标题插槽</i>"))));
            var sub = cut.Find(".el-result__subtitle");
            Assert.Contains("副标题插槽", sub.TextContent);
        }

        [Fact]
        public void ExtraSlotRenders()
        {
            var cut = Render<ElResult>(p => p
                .Add(x => x.Extra, (RenderFragment)(b => b.AddMarkupContent(0, "<button class=\"extra-btn\">Back</button>"))));
            Assert.Single(cut.FindAll("div.el-result__extra"));
            Assert.Single(cut.FindAll("button.extra-btn"));
        }

        [Fact]
        public void AriaLabelIsOutput()
        {
            var cut = Render<ElResult>(p => p.Add(x => x.AriaLabel, "操作结果"));
            Assert.Equal("操作结果", cut.Find("div.el-result").GetAttribute("aria-label"));
        }

        [Fact]
        public void RootHasAlertRole()
        {
            var cut = Render<ElResult>(p => p.Add(x => x.Title, "T"));
            Assert.Equal("alert", cut.Find("div.el-result").GetAttribute("role"));
        }
    }
}
