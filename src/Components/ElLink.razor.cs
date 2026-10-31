using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// ElLink 文字超链接 — 对齐 Element Plus Link 官方 API
    /// </summary>
    public partial class ElLink : ElementComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>链接类型：default / primary / success / warning / danger / info</summary>
        [Parameter]
        public LinkType Type { get; set; } = LinkType.Default;

        /// <summary>是否禁用</summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>下划线模式：hover(默认) / always / never</summary>
        [Parameter]
        public LinkUnderline Underline { get; set; } = LinkUnderline.Hover;

        /// <summary>原生 href 属性</summary>
        [Parameter]
        public string Href { get; set; }

        /// <summary>原生 target 属性，默认 _self</summary>
        [Parameter]
        public string Target { get; set; } = "_self";

        /// <summary>原生 rel 属性（会自动处理 target=_blank 的安全值）</summary>
        [Parameter]
        public string Rel { get; set; }

        /// <summary>图标 CSS 类名（如 "el-icon-edit"）或纯名称</summary>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>自定义图标 slot（优先于 Icon 属性）</summary>
        [Parameter]
        public RenderFragment IconContent { get; set; }

        /// <summary>原生 tabindex</summary>
        [Parameter]
        public int? Tabindex { get; set; }

        /// <summary>点击事件（Disabled 时不触发）</summary>
        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        protected string LinkClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-link", Cls)
            .AddIf(Type != LinkType.Default, $"el-link--{Type.ToString().ToLowerInvariant()}")
            .AddIf(Disabled, "is-disabled")
            .AddIf(Underline == LinkUnderline.Hover && !Disabled, "is-underline")
            .AddIf(Underline == LinkUnderline.Always && !Disabled, "is-underline-always")
            .AddIf(Underline == LinkUnderline.Never && !Disabled, "is-underline-never")
            .ToString();

        protected string LinkStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add(Style)
            .ToString();

        /// <summary>
        /// Icon 的 XML 注释声明「可以是完整 CSS 类名，也可以是纯名称」（如 "edit" / "el-icon-edit"），
    /// 但渲染层原本是原样输出，传纯名称会得到 &lt;i class="edit"&gt; — 一个没有任何样式的空标签。
    /// 这里补上归一化：不含连字符且不以 el-icon- 开头的单词自动补前缀；
    /// 含连字符或已带前缀的值（如 "fa fa-search" / "el-icon-close"）保持原样，避免误伤第三方图标库。
    /// </summary>
    protected string EffectiveIconClass
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Icon)) return null;

            var trimmed = Icon.Trim();
            if (trimmed.StartsWith("el-icon-", StringComparison.OrdinalIgnoreCase)) return trimmed;
            return trimmed.IndexOf('-') < 0 ? "el-icon-" + trimmed : trimmed;
        }
    }

        /// <summary>
        /// 计算最终 rel：如果没显式设 + target=_blank → 自动加 noreferrer noopener（安全）
        /// </summary>
        protected string EffectiveRel
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Rel)) return Rel;
                if (string.Equals(Target, "_blank", StringComparison.OrdinalIgnoreCase))
                    return "noreferrer noopener";
                return null;
            }
        }

        private async Task OnLinkClickAsync(MouseEventArgs args)
        {
            if (Disabled || !OnClick.HasDelegate)
            {
                return;
            }

            await OnClick.InvokeAsync(args);
        }
    }
}