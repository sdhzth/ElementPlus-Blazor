using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElIconTests : BunitContext
    {
        public ElIconTests()
        {
            Services.AddElementServices();
        }

        // ==================== Icon 枚举 → CSS 类名 ====================

        [Fact]
        public void IconEnum_Close_RendersCorrectCssClass()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.Close));
            var i = cut.Find("i");
            Assert.Contains("el-icon", i.ClassList);
            Assert.Contains("el-icon-close", i.ClassList);
        }

        [Fact]
        public void IconEnum_CirclePlusOutline_ToKebabCase()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.CirclePlusOutline));
            Assert.Contains("el-icon-circle-plus-outline", cut.Find("i").ClassList);
        }

        [Fact]
        public void IconEnum_ArrowLeft_ToKebabCase()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.ArrowLeft));
            Assert.Contains("el-icon-arrow-left", cut.Find("i").ClassList);
        }

        [Fact]
        public void DefaultIcon_RendersNoGlyphClass()
        {
            // Icon 枚举默认值 0 必须是 None：未指定 Icon/Name 时不应渲染出任何字形
            // （修复前 0 是 PlatformEleme，导致 <ElIcon /> 默认会显示 eleme logo）
            var cut = Render<ElIcon>();
            var i = cut.Find("i");
            Assert.Contains("el-icon", i.ClassList);
            var glyph = i.ClassList.FirstOrDefault(c => c.StartsWith("el-icon-", StringComparison.Ordinal));
            Assert.Null(glyph);
        }

        [Fact]
        public void IconEnum_PlatformEleme_StillAvailable()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.PlatformEleme));
            Assert.Contains("el-icon-platform-eleme", cut.Find("i").ClassList);
        }

        [Fact]
        public void IconEnum_UserFilled_FallsBackToExistingGlyph()
        {
            // 字体里没有 el-icon-user-filled，必须兜底到 el-icon-user-solid，避免渲染成空白
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.UserFilled));
            var i = cut.Find("i");
            Assert.Contains("el-icon-user-solid", i.ClassList);
            Assert.DoesNotContain("el-icon-user-filled", i.ClassList);
        }

        [Fact]
        public void IconEnum_LastEnumValue_Works()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.RefreshRightBold));
            // 字体里没有 refresh-right-bold，兜底到语义相同的 refresh-right
            Assert.Contains("el-icon-refresh-right", cut.Find("i").ClassList);
        }

        // ==================== Name 参数（字符串） ====================

        [Fact]
        public void Name_PlainString_NoPrefix()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Name, "close"));
            Assert.Contains("el-icon-close", cut.Find("i").ClassList);
        }

        [Fact]
        public void Name_WithPrefix_StripsElIcon()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Name, "el-icon-delete"));
            Assert.Contains("el-icon-delete", cut.Find("i").ClassList);
            Assert.DoesNotContain("el-icon-el-icon-delete", cut.Find("i").ClassList);
        }

        [Fact]
        public void Name_PascalCase_ToKebabCase()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Name, "CirclePlusOutline"));
            Assert.Contains("el-icon-circle-plus-outline", cut.Find("i").ClassList);
        }

        [Fact]
        public void Name_PascalCase_WithPrefix()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Name, "el-icon-CirclePlusOutline"));
            Assert.Contains("el-icon-circle-plus-outline", cut.Find("i").ClassList);
        }

        [Fact]
        public void Name_TakesPriorityOverIcon()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Name, "close").Add(x => x.Icon, Icon.Delete));
            Assert.Contains("el-icon-close", cut.Find("i").ClassList);
            Assert.DoesNotContain("el-icon-delete", cut.Find("i").ClassList);
        }

        [Fact]
        public void NameEdgeCase_ExtraSpaces()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Name, "  el-icon-delete  "));
            Assert.Contains("el-icon-delete", cut.Find("i").ClassList);
        }

        // ==================== Size 参数 ====================

        [Fact]
        public void Size_WithPxUnit()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.Close).Add(x => x.Size, "16px"));
            Assert.Contains("font-size:16px", cut.Find("i").GetAttribute("style"));
        }

        [Fact]
        public void Size_UnitlessNumber_AppendsPx()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.Close).Add(x => x.Size, "24"));
            Assert.Contains("font-size:24px", cut.Find("i").GetAttribute("style"));
        }

        [Fact]
        public void Size_WithRemUnit()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.Close).Add(x => x.Size, "1.5rem"));
            Assert.Contains("font-size:1.5rem", cut.Find("i").GetAttribute("style"));
        }

        // ==================== Color 参数 ====================

        [Fact]
        public void Color_HexValue()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.Close).Add(x => x.Color, "#ff0000"));
            Assert.Contains("color:#ff0000", cut.Find("i").GetAttribute("style"));
        }

        [Fact]
        public void Color_NamedColor()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.Close).Add(x => x.Color, "red"));
            Assert.Contains("color:red", cut.Find("i").GetAttribute("style"));
        }

        // ==================== Spin / is-loading ====================

        [Fact]
        public void Spin_AddsIsLoadingClass()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.Loading).Add(x => x.Spin, true));
            Assert.Contains("is-loading", cut.Find("i").ClassList);
        }

        [Fact]
        public void Spin_DefaultFalse_NoIsLoading()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.Close));
            Assert.DoesNotContain("is-loading", cut.Find("i").ClassList);
        }

        // ==================== ChildContent ====================

        [Fact]
        public void ChildContent_CustomSvg()
        {
            var cut = Render<ElIcon>(p => p.AddChildContent("<svg viewBox=\"0 0 1024 1024\"><path d=\"M512\"/></svg>"));
            var i = cut.Find("i");
            Assert.Contains("el-icon", i.ClassList);
            Assert.Contains("<svg", i.InnerHtml);
        }

        // ==================== OnClick ====================

        [Fact]
        public async Task OnClick_CallbackInvoked()
        {
            var clicks = new List<MouseEventArgs>();
            var cut = Render<ElIcon>(p => p
                .Add(x => x.Icon, Icon.Close)
                .Add(x => x.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, e => clicks.Add(e))));

            await cut.InvokeAsync(() => cut.Find("i").Click());
            Assert.Single(clicks);
        }

        [Fact]
        public async Task OnClick_NoCallback_DoesNotThrow()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.Close));
            await cut.InvokeAsync(() => cut.Find("i").Click());
        }

        // ==================== 无障碍 ====================

        [Fact]
        public void HasRoleImg()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.Close));
            Assert.Equal("img", cut.Find("i").GetAttribute("role"));
        }

        [Fact]
        public void AriaLabel_RenderedWhenProvided()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.Close).Add(x => x.AriaLabel, "关闭"));
            Assert.Equal("关闭", cut.Find("i").GetAttribute("aria-label"));
        }

        [Fact]
        public void Title_RenderedWhenProvided()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.Close).Add(x => x.Title, "关闭按钮"));
            Assert.Equal("关闭按钮", cut.Find("i").GetAttribute("title"));
        }

        [Fact]
        public void TitleAndAriaLabel_Empty_NotRendered()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.Close));
            var i = cut.Find("i");
            Assert.Null(i.GetAttribute("aria-label"));
            Assert.Null(i.GetAttribute("title"));
        }

        // ==================== Cls / Style / Attributes 透传 ====================

        [Fact]
        public void Cls_AppendedToIconClass()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.Close).Add(x => x.Cls, "my-icon"));
            Assert.Contains("my-icon", cut.Find("i").ClassList);
        }

        [Fact]
        public void Style_AppendedToInlineStyle()
        {
            var cut = Render<ElIcon>(p => p
                .Add(x => x.Icon, Icon.Close)
                .Add(x => x.Size, "20px")
                .Add(x => x.Style, "margin-left:8px"));

            var style = cut.Find("i").GetAttribute("style");
            Assert.Contains("font-size:20px", style);
            Assert.Contains("margin-left:8px", style);
        }

        [Fact]
        public void Attributes_CapturedAndRendered()
        {
            var cut = Render<ElIcon>(p => p.Add(x => x.Icon, Icon.Close).AddUnmatched("data-testid", "icon-close"));
            Assert.Equal("icon-close", cut.Find("i").GetAttribute("data-testid"));
        }

        // ==================== 端到端 ====================

        [Fact]
        public void FullFeature_AllPropertiesCombined()
        {
            var cut = Render<ElIcon>(p => p
                .Add(x => x.Icon, Icon.Loading)
                .Add(x => x.Size, "24px")
                .Add(x => x.Color, "#409EFF")
                .Add(x => x.Spin, true)
                .Add(x => x.AriaLabel, "加载中")
                .Add(x => x.Title, "正在加载，请稍候")
                .Add(x => x.Cls, "my-spinner"));

            var i = cut.Find("i");
            Assert.Contains("el-icon", i.ClassList);
            Assert.Contains("el-icon-loading", i.ClassList);
            Assert.Contains("is-loading", i.ClassList);
            Assert.Contains("my-spinner", i.ClassList);
            Assert.Contains("font-size:24px", i.GetAttribute("style"));
            Assert.Contains("color:#409EFF", i.GetAttribute("style"));
            Assert.Equal("img", i.GetAttribute("role"));
            Assert.Equal("加载中", i.GetAttribute("aria-label"));
            Assert.Equal("正在加载，请稍候", i.GetAttribute("title"));
        }
    }
}