using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElTypography / ElTypographyTitle 单元测试 — 对齐 Element Plus Typography 官方 API
    /// </summary>
    public class ElTypographyTests : BunitContext
    {
        public ElTypographyTests()
        {
            Services.AddElementServices();
        }

        // ==================== ElTypographyTitle：层级 / 标签 ====================

        [Fact]
        public void Title_Level1_RendersH1()
        {
            var cut = Render<ElTypographyTitle>(p => p
                .Add(x => x.Level, 1)
                .Add(x => x.Content, "标题"));

            var title = cut.Find(".el-typography-title");
            Assert.Equal("H1", title.TagName);
            Assert.Contains("el-typography-title--1", title.ClassList);
            Assert.Equal("标题", title.TextContent.Trim());
        }

        [Fact]
        public void Title_Level3_RendersH3()
        {
            var cut = Render<ElTypographyTitle>(p => p
                .Add(x => x.Level, 3)
                .Add(x => x.Content, "三级标题"));

            var title = cut.Find(".el-typography-title");
            Assert.Equal("H3", title.TagName);
            Assert.Contains("el-typography-title--3", title.ClassList);
        }

        [Fact]
        public void Title_LevelClamped_Above5_Becomes5()
        {
            // Level 超过 5 应被 Math.Clamp 到 5（渲染 h5）
            var cut = Render<ElTypographyTitle>(p => p
                .Add(x => x.Level, 9)
                .Add(x => x.Content, "超限"));

            var title = cut.Find(".el-typography-title");
            Assert.Equal("H5", title.TagName);
            Assert.Contains("el-typography-title--5", title.ClassList);
        }

        [Fact]
        public void Title_LevelClamped_Below1_Becomes1()
        {
            var cut = Render<ElTypographyTitle>(p => p
                .Add(x => x.Level, 0)
                .Add(x => x.Content, "下限"));

            Assert.Equal("H1", cut.Find(".el-typography-title").TagName);
        }

        [Fact]
        public void Title_Type_Primary_AddsTypeClass()
        {
            var cut = Render<ElTypographyTitle>(p => p
                .Add(x => x.Level, 2)
                .Add(x => x.Type, TextType.Primary)
                .Add(x => x.Content, "主色"));

            Assert.Contains("el-typography-title--primary", cut.Find(".el-typography-title").ClassList);
        }

        // ==================== ElTypographyTitle：复制 ====================

        [Fact]
        public void Title_Copyable_RendersCopyIcon()
        {
            var cut = Render<ElTypographyTitle>(p => p
                .Add(x => x.Level, 3)
                .Add(x => x.Copyable, true)
                .Add(x => x.Content, "可复制文本"));

            var copy = cut.Find(".el-typography-title__copy");
            Assert.NotNull(copy);
            // 复制图标字形
            Assert.Contains("el-icon-document-copy", cut.Find(".el-typography-title__copy i").ClassName);
        }

        [Fact]
        public void Title_NotCopyable_HasNoCopyIcon()
        {
            var cut = Render<ElTypographyTitle>(p => p
                .Add(x => x.Level, 3)
                .Add(x => x.Copyable, false)
                .Add(x => x.Content, "不可复制"));

            Assert.Empty(cut.FindAll(".el-typography-title__copy"));
        }

        // ==================== ElTypographyTitle：截断 ====================

        [Fact]
        public void Title_Truncated_AddsTruncatedClass()
        {
            var cut = Render<ElTypographyTitle>(p => p
                .Add(x => x.Level, 4)
                .Add(x => x.Truncated, true)
                .Add(x => x.Content, "单行截断内容"));

            Assert.Contains("is-truncated", cut.Find(".el-typography-title").ClassList);
        }

        [Fact]
        public void Title_LineClamp_AddsLineClampClass_AndStyleVar()
        {
            var cut = Render<ElTypographyTitle>(p => p
                .Add(x => x.Level, 4)
                .Add(x => x.LineClamp, 2)
                .Add(x => x.Content, "多行截断内容"));

            var title = cut.Find(".el-typography-title");
            Assert.Contains("is-line-clamp", title.ClassList);
            Assert.Contains("--el-text-line-clamp:2", title.GetAttribute("style"));
        }

        // ==================== ElTypography：根容器 ====================

        [Fact]
        public void Typography_Root_DefaultTagIsDiv()
        {
            var cut = Render<ElTypography>(p => p
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElTypographyTitle>(0);
                    builder.AddAttribute(1, nameof(ElTypographyTitle.Level), 1);
                    builder.AddAttribute(2, nameof(ElTypographyTitle.Content), "根内标题");
                    builder.CloseComponent();
                }));

            var root = cut.Find(".el-typography");
            Assert.Equal("DIV", root.TagName);
            // 子标题在根容器内渲染
            Assert.Equal("H1", cut.Find(".el-typography-title").TagName);
        }

        [Fact]
        public void Typography_CustomTag_Applied()
        {
            var cut = Render<ElTypography>(p => p
                .Add(x => x.Tag, "section")
                .AddChildContent("段落"));

            Assert.Equal("SECTION", cut.Find(".el-typography").TagName);
        }
    }
}
