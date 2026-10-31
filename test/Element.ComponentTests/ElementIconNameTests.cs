using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 批次92 · ElementIconName：图标名归一从 ElIcon 的私有实现提成一份公共口径，并让
    /// ElMenuItem / ElSubMenu 用上它（原先它们只补个 el-icon- 前缀，不认大小写也不走字体别名）。
    /// 触发点是宿主 emrelecrecordplusgroup 的左侧菜单：u_icon 这个数据库字段直接进组件，
    /// 库里认不出的写法（"User"、"avatar"）在菜单里是空白格，在 &lt;ElIcon&gt; 里却有图。
    /// </summary>
    public class ElementIconNameTests : BunitContext
    {
        public ElementIconNameTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static string RepoRoot([CallerFilePath] string file = null)
        {
            // .../test/Element.ComponentTests/ElementIconNameTests.cs → 上溯三层
            var directory = new FileInfo(file).Directory;
            return directory.Parent.Parent.FullName;
        }

        private static RenderFragment Item(string index, string icon)
            => builder =>
            {
                builder.OpenComponent<ElMenuItem>(0);
                builder.AddAttribute(1, "Index", index);
                if (icon != null) builder.AddAttribute(2, "Icon", icon);
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "导航一")));
                builder.CloseComponent();
            };

        private static RenderFragment Sub(string index, string icon, RenderFragment children)
            => builder =>
            {
                builder.OpenComponent<ElSubMenu>(0);
                builder.AddAttribute(1, "Index", index);
                builder.AddAttribute(2, "Label", "分组一");
                if (icon != null) builder.AddAttribute(3, "Icon", icon);
                builder.AddAttribute(4, "ChildContent", children);
                builder.CloseComponent();
            };

        #region 归一口径

        [Theory]
        [InlineData("user", "user")]
        [InlineData("   user   ", "user")]
        [InlineData("el-icon-user", "user")]
        [InlineData("User", "user")]
        [InlineData("UserFilled", "user-solid")]
        [InlineData("avatar", "user")]
        [InlineData("clock", "time")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void Normalize_SupportsEverySpelling(string raw, string expected)
        {
            Assert.Equal(expected, ElementIconName.Normalize(raw));
        }

        [Theory]
        [InlineData("user", "el-icon-user")]
        [InlineData("UserFilled", "el-icon-user-solid")]
        [InlineData("el-icon-house", "el-icon-house")]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void ToClass_AddsPrefixOrGivesNullForEmpty(string raw, string expected)
        {
            Assert.Equal(expected, ElementIconName.ToClass(raw));
        }

        [Theory]
        [InlineData("user", true)]
        [InlineData("el-icon-user-solid", true)]
        [InlineData("avatar", true)]
        [InlineData("user123", false)]
        [InlineData("nope-nope", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsKnown_AnswersWhetherAGlyphWillRemain(string raw, bool expected)
        {
            Assert.Equal(expected, ElementIconName.IsKnown(raw));
        }

        #endregion

        #region 两份清单与事实同源（改 CSS / 加枚举却不补别名时当场翻脸）

        [Fact]
        public void GlyphNames_AreExactlyTheFontRulesInIndexCss()
        {
            var css = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "css", "index.css"));
            var inCss = Regex.Matches(css, @"\.el-icon-([A-Za-z0-9_-]+):before\{content:")
                .Select(m => m.Groups[1].Value).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
            var embedded = ElementIconName.GlyphNames.OrderBy(x => x, StringComparer.Ordinal).ToList();

            // 尺子先自证命中过：正则抓空了这条用例就永远绿，那是假绿
            Assert.True(inCss.Count > 200, $"index.css 里只抓到 {inCss.Count} 条字形规则，正则或 CSS 结构变了");
            Assert.Equal(inCss, embedded);
        }

        [Fact]
        public void EveryEnumIcon_ResolvesToAGlyphInTheFont()
        {
            foreach (Icon icon in Enum.GetValues(typeof(Icon)))
            {
                if (icon == Icon.None)
                {
                    Assert.Equal(string.Empty, ElementIconName.FromEnum(icon));
                    continue;
                }

                var name = ElementIconName.FromEnum(icon);
                Assert.True(ElementIconName.IsKnown(name),
                    $"Icon.{icon} 归一成 {name}，但字体里没有这个字形，也没配别名 —— 用它会渲染成空白格");
            }
        }

        #endregion

        #region 菜单与 ElIcon 同一个口径

        [Fact]
        public void MenuItem_BareName_RendersPrefixedGlyph()
        {
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent, Item("1", "user")));

            Assert.Equal("el-icon-user", cut.Find("li.el-menu-item i").GetAttribute("class"));
        }

        [Fact]
        public void MenuItem_AliasName_RendersFontGlyphInsteadOfBlank()
        {
            // 改前：只补前缀 ⇒ el-icon-avatar，字体里没这个字形，图标位空白
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent, Item("1", "avatar")));

            Assert.Equal("el-icon-user", cut.Find("li.el-menu-item i").GetAttribute("class"));
        }

        [Fact]
        public void MenuItem_PascalName_RendersKebabGlyph()
        {
            // 改前：el-icon-UserFilled（大小写敏感的类名，等于没图）
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent, Item("1", "UserFilled")));

            Assert.Equal("el-icon-user-solid", cut.Find("li.el-menu-item i").GetAttribute("class"));
        }

        [Fact]
        public void SubMenu_UsesTheSameCaliberAsMenuItem()
        {
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent,
                Sub("g1", "UserFilled", Item("1-1", "user"))));

            var classes = cut.FindAll("i").Select(i => i.GetAttribute("class")).ToList();
            Assert.Contains("el-icon-user-solid", classes);
            Assert.Contains("el-icon-user", classes);
        }

        [Fact]
        public void MenuItem_EmptyIcon_RendersNoGlyphElement()
        {
            var cut = Render<ElMenu>(p => p.Add(x => x.ChildContent, Item("1", null)));

            Assert.Empty(cut.FindAll("li.el-menu-item i"));
        }

        [Fact]
        public void ElIcon_KeepsNormalizingNameAfterTheExtraction()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Name, "avatar"));

            Assert.Contains("el-icon-user", cut.Find("i").ClassList);
        }

        #endregion
    }
}
