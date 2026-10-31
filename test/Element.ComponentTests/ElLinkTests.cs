using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElLink 单元测试 — 对齐 Element Plus Link 官方 API
    /// </summary>
    public class ElLinkTests : BunitContext
    {
        public ElLinkTests() => Services.AddElementServices();

        // ==================== 基本渲染 ====================

        [Fact]
        public void Default_RenderAnchor()
        {
            var cut = Render<ElLink>(p => p.AddChildContent("click me"));
            var a = cut.Find("a");
            Assert.Equal("click me", a.TextContent.Trim());
            Assert.Contains("el-link", a.ClassList);
            Assert.Contains("is-underline", a.ClassList);
            // Default 是默认类型，不加 el-link--default 冗余类
        }

        [Fact]
        public void Default_Target_Self()
        {
            var cut = Render<ElLink>(p => p.AddChildContent("x"));
            Assert.Equal("_self", cut.Find("a").GetAttribute("target"));
        }

        // ==================== LinkType ====================

        [Fact]
        public void Type_Default_NoExtraTypeClass()
        {
            // Default 是默认类型，不加 el-link--default 类
            var cut = Render<ElLink>(p => p.AddChildContent("x"));
            Assert.Contains("el-link", cut.Find("a").ClassList);
            Assert.DoesNotContain("el-link--default", cut.Find("a").ClassList);
        }

        [Fact]
        public void Type_Primary()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Type, LinkType.Primary).AddChildContent("x"));
            Assert.Contains("el-link--primary", cut.Find("a").ClassList);
        }

        [Fact]
        public void Type_Success()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Type, LinkType.Success).AddChildContent("x"));
            Assert.Contains("el-link--success", cut.Find("a").ClassList);
        }

        [Fact]
        public void Type_Warning()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Type, LinkType.Warning).AddChildContent("x"));
            Assert.Contains("el-link--warning", cut.Find("a").ClassList);
        }

        [Fact]
        public void Type_Danger()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Type, LinkType.Danger).AddChildContent("x"));
            Assert.Contains("el-link--danger", cut.Find("a").ClassList);
        }

        [Fact]
        public void Type_Info()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Type, LinkType.Info).AddChildContent("x"));
            Assert.Contains("el-link--info", cut.Find("a").ClassList);
        }

        // ==================== Href / Target ====================

        [Fact]
        public void Href_Rendered()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Href, "https://element-plus.org").AddChildContent("x"));
            Assert.Equal("https://element-plus.org", cut.Find("a").GetAttribute("href"));
        }

        [Fact]
        public void Href_Disabled_Removed()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Href, "https://x.com").Add(x => x.Disabled, true).AddChildContent("x"));
            Assert.Null(cut.Find("a").GetAttribute("href"));
        }

        [Fact]
        public void Target_CustomBlank()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Target, "_blank").AddChildContent("x"));
            Assert.Equal("_blank", cut.Find("a").GetAttribute("target"));
        }

        // ==================== Rel 自动安全处理 ====================

        [Fact]
        public void Rel_Auto_NoTarget_NoRel()
        {
            var cut = Render<ElLink>(p => p.AddChildContent("x"));
            // 默认 target=_self，不自动加 rel
            Assert.Null(cut.Find("a").GetAttribute("rel"));
        }

        [Fact]
        public void Rel_Auto_TargetBlank_NoreferrerNoopener()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Target, "_blank").AddChildContent("x"));
            Assert.Equal("noreferrer noopener", cut.Find("a").GetAttribute("rel"));
        }

        [Fact]
        public void Rel_Explicit_OverridesAuto()
        {
            var cut = Render<ElLink>(p => p
                .Add(x => x.Target, "_blank")
                .Add(x => x.Rel, "noopener")
                .AddChildContent("x"));
            Assert.Equal("noopener", cut.Find("a").GetAttribute("rel"));
        }

        // ==================== Disabled ====================

        [Fact]
        public void Disabled_AddsIsDisabledClass()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Disabled, true).AddChildContent("x"));
            Assert.Contains("is-disabled", cut.Find("a").ClassList);
        }

        [Fact]
        public void Disabled_AriaDisabledTrue()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Disabled, true).AddChildContent("x"));
            Assert.Equal("true", cut.Find("a").GetAttribute("aria-disabled"));
        }

        [Fact]
        public void Disabled_TabindexMinus1()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Disabled, true).AddChildContent("x"));
            Assert.Equal("-1", cut.Find("a").GetAttribute("tabindex"));
        }

        [Fact]
        public void Disabled_NoHref()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Disabled, true).Add(x => x.Href, "#").AddChildContent("x"));
            Assert.Null(cut.Find("a").GetAttribute("href"));
        }

        [Fact]
        public void NotDisabled_AriaDisabledFalse()
        {
            var cut = Render<ElLink>(p => p.AddChildContent("x"));
            Assert.Equal("false", cut.Find("a").GetAttribute("aria-disabled"));
        }

        // ==================== Underline（Element Plus 2.9.9+）====================

        [Fact]
        public void Underline_DefaultHover_HasIsUnderline()
        {
            var cut = Render<ElLink>(p => p.AddChildContent("x"));
            Assert.Contains("is-underline", cut.Find("a").ClassList);
            Assert.DoesNotContain("is-underline-always", cut.Find("a").ClassList);
            Assert.DoesNotContain("is-underline-never", cut.Find("a").ClassList);
        }

        [Fact]
        public void Underline_Always()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Underline, LinkUnderline.Always).AddChildContent("x"));
            Assert.Contains("is-underline-always", cut.Find("a").ClassList);
            Assert.DoesNotContain("is-underline", cut.Find("a").ClassList);
        }

        [Fact]
        public void Underline_Never()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Underline, LinkUnderline.Never).AddChildContent("x"));
            Assert.Contains("is-underline-never", cut.Find("a").ClassList);
            Assert.DoesNotContain("is-underline", cut.Find("a").ClassList);
        }

        [Fact]
        public void Underline_Disabled_NoUnderlineClass()
        {
            // Disabled 时不应有任何 underline class
            var cut = Render<ElLink>(p => p.Add(x => x.Disabled, true).Add(x => x.Underline, LinkUnderline.Always).AddChildContent("x"));
            var classes = cut.Find("a").ClassList;
            Assert.DoesNotContain("is-underline", classes);
            Assert.DoesNotContain("is-underline-always", classes);
            Assert.DoesNotContain("is-underline-never", classes);
        }

        [Fact]
        public void Underline_DisabledDefault_NoUnderline()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Disabled, true).AddChildContent("x"));
            Assert.DoesNotContain("is-underline", cut.Find("a").ClassList);
        }

        // ==================== Icon（字符串）====================

        [Fact]
        public void Icon_String_RenderedAsIconClass()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Icon, "el-icon-edit").AddChildContent("Edit"));
            var i = cut.Find("i");
            Assert.Contains("el-icon-edit", i.ClassList);
            Assert.Equal("true", i.GetAttribute("aria-hidden"));
        }

        [Fact]
        public void Icon_String_PlainName()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Icon, "close").AddChildContent("Close"));
            // 纯名称应归一化成 el-icon-*，否则渲染出的是没有样式的 <i class="close">
            Assert.Contains("el-icon-close", cut.Find("i").ClassList);
        }

        [Fact]
        public void Icon_String_ThirdPartyClassPreserved()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Icon, "fa fa-search").AddChildContent("Search"));
            var cls = cut.Find("i").GetAttribute("class");
            Assert.Contains("fa", cls);
            Assert.Contains("fa-search", cls);
            Assert.DoesNotContain("el-icon-", cls);
        }

        [Fact]
        public void Icon_String_PrefixedNotDoublePrefixed()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Icon, "el-icon-view").AddChildContent("View"));
            Assert.DoesNotContain("el-icon-el-icon-view", cut.Find("i").ClassList);
        }

        // ==================== IconContent（RenderFragment slot）====================

        [Fact]
        public void IconContent_Rendered()
        {
            var cut = Render<ElLink>(p => p
                .Add(x => x.IconContent, (RenderFragment)(b => b.AddContent(0, "★")))
                .AddChildContent("Star"));

            Assert.Contains("★", cut.Find("a").InnerHtml);
        }

        [Fact]
        public void IconContent_TakesPriorityOverIcon()
        {
            var cut = Render<ElLink>(p => p
                .Add(x => x.Icon, "el-icon-edit")
                .Add(x => x.IconContent, (RenderFragment)(b => b.AddContent(0, "CUSTOM")))
                .AddChildContent("Edit"));

            Assert.DoesNotContain("<i", cut.Find("a").InnerHtml);
            Assert.Contains("CUSTOM", cut.Find("a").InnerHtml);
        }

        // ==================== OnClick ====================

        [Fact]
        public async Task OnClick_NotDisabled_Invoked()
        {
            var clicks = new List<MouseEventArgs>();
            var cut = Render<ElLink>(p => p
                .Add(x => x.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, e => clicks.Add(e)))
                .AddChildContent("x"));

            await cut.InvokeAsync(() => cut.Find("a").Click());
            Assert.Single(clicks);
        }

        [Fact]
        public async Task OnClick_Disabled_NotInvoked()
        {
            var clicks = new List<MouseEventArgs>();
            var cut = Render<ElLink>(p => p
                .Add(x => x.Disabled, true)
                .Add(x => x.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, e => clicks.Add(e)))
                .AddChildContent("x"));

            await cut.InvokeAsync(() => cut.Find("a").Click());
            Assert.Empty(clicks);
        }

        // ==================== ChildContent wrapper ====================

        [Fact]
        public void ChildContent_WrappedInInnerSpan()
        {
            var cut = Render<ElLink>(p => p.AddChildContent("Hello"));
            var span = cut.Find(".el-link__inner");
            Assert.Equal("Hello", span.TextContent.Trim());
        }

        [Fact]
        public void NoChildContent_NoInnerSpan()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Icon, "el-icon-close"));
            var spans = cut.FindAll(".el-link__inner");
            Assert.Empty(spans);
        }

        // ==================== Cls / Style / Attributes ====================

        [Fact]
        public void Cls_Appended()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Cls, "my-link").AddChildContent("x"));
            Assert.Contains("my-link", cut.Find("a").ClassList);
        }

        [Fact]
        public void Style_Appended()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Style, "font-weight:bold").AddChildContent("x"));
            Assert.Contains("font-weight:bold", cut.Find("a").GetAttribute("style"));
        }

        [Fact]
        public void Attributes_Passthrough()
        {
            var cut = Render<ElLink>(p => p.AddChildContent("x").AddUnmatched("data-ga", "link-click"));
            Assert.Equal("link-click", cut.Find("a").GetAttribute("data-ga"));
        }

        [Fact]
        public void Tabindex_Custom()
        {
            var cut = Render<ElLink>(p => p.Add(x => x.Tabindex, 5).AddChildContent("x"));
            Assert.Equal("5", cut.Find("a").GetAttribute("tabindex"));
        }

        [Fact]
        public void Tabindex_Disabled_OverriddenToMinus1()
        {
            var cut = Render<ElLink>(p => p
                .Add(x => x.Tabindex, 5)
                .Add(x => x.Disabled, true)
                .AddChildContent("x"));
            Assert.Equal("-1", cut.Find("a").GetAttribute("tabindex"));
        }

        // ==================== 端到端 ====================

        [Fact]
        public void FullFeature_PrimaryLinkWithIconTargetBlank()
        {
            var cut = Render<ElLink>(p => p
                .Add(x => x.Type, LinkType.Primary)
                .Add(x => x.Href, "https://element-plus.org")
                .Add(x => x.Target, "_blank")
                .Add(x => x.Icon, "el-icon-view")
                .Add(x => x.Underline, LinkUnderline.Always)
                .AddChildContent("View Docs"));

            var a = cut.Find("a");
            Assert.Contains("el-link", a.ClassList);
            Assert.Contains("el-link--primary", a.ClassList);
            Assert.Contains("is-underline-always", a.ClassList);
            Assert.Equal("https://element-plus.org", a.GetAttribute("href"));
            Assert.Equal("_blank", a.GetAttribute("target"));
            Assert.Equal("noreferrer noopener", a.GetAttribute("rel"));
            Assert.Contains("el-icon-view", cut.Find("i").ClassList);
            Assert.Equal("View Docs", cut.Find(".el-link__inner").TextContent.Trim());
        }
    }
}