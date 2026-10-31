using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;

namespace ElementPlus
{
    public partial class ElText : ElementComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public TextType Type { get; set; } = TextType.Default;

        [Parameter]
        public TextSize Size { get; set; } = TextSize.Default;

        [Parameter]
        public bool Truncated { get; set; }

        [Parameter]
        public int? LineClamp { get; set; }

        [Parameter]
        public string Tag { get; set; } = "span";

        /// <summary>
        /// 文字被截断时悬停用 elTooltip 浮出完整内容（本库扩展，默认开；EP 的 <c>ElText</c> 没有浮层，
        /// 只在真的被裁时绑原生 <c>title</c>，见 <c>text.vue:42-71</c>）。
        /// 设成 <c>false</c> 就回到 EP 的原生 title 口径，两者互斥、不会叠成两层浮层。
        /// </summary>
        [Parameter]
        public bool EllipsisTooltip { get; set; } = true;

        protected string EffectiveTag => string.IsNullOrWhiteSpace(Tag) ? "span" : Tag;

        /// <summary>
        /// 未匹配特性里的 class（Blazor 把 &lt;ElText class="x"&gt; 收进 Attributes，不合并会被组件的 class 覆盖丢失）。
        /// </summary>
        protected string AttributeClass => GetUnmatchedValue("class");

        /// <summary>
        /// 未匹配特性里的 style。
        /// </summary>
        protected string AttributeStyle => GetUnmatchedValue("style");

        private string GetUnmatchedValue(string key)
        {
            if (Attributes == null) return null;
            foreach (var kv in Attributes)
            {
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value?.ToString();
            }
            return null;
        }

        protected string TextClass
        {
            get
            {
                var cb = HtmlPropertyBuilder.CreateCssClassBuilder();
                cb.Add("el-text");
                if (!string.IsNullOrWhiteSpace(AttributeClass)) cb.Add(AttributeClass.Trim());
                if (!string.IsNullOrWhiteSpace(Cls)) cb.Add(Cls);
                if (Type != TextType.Default) cb.Add("el-text--" + Type.ToString().ToLowerInvariant());
                if (Size != TextSize.Default) cb.Add("el-text--" + Size.ToString().ToLowerInvariant());
                if (Truncated) cb.Add("is-truncated");
                if (LineClamp.HasValue && LineClamp.Value > 0) cb.Add("is-line-clamp");
                return cb.ToString();
            }
        }

        protected string TextStyle
        {
            get
            {
                var sb = new System.Text.StringBuilder();
                if (LineClamp.HasValue && LineClamp.Value > 0)
                    sb.Append($"--el-text-line-clamp:{LineClamp.Value};");
                if (!string.IsNullOrWhiteSpace(AttributeStyle))
                {
                    var s = AttributeStyle.Trim();
                    if (!s.EndsWith(";", StringComparison.Ordinal)) s += ";";
                    sb.Append(s);
                }
                if (!string.IsNullOrWhiteSpace(Style))
                    sb.Append(Style);
                return sb.ToString();
            }
        }

        private bool Clipping => Truncated || (LineClamp.HasValue && LineClamp.Value > 0);

        /// <summary>只有开了截断（<see cref="Truncated"/> 或 <see cref="LineClamp"/>）才可能挂浮层。</summary>
        protected bool ShowEllipsisTooltip => EllipsisTooltip && Clipping;

        /// <summary>关掉本库浮层扩展时，才走 EP 的原生 title。</summary>
        protected bool BindEllipsisTitle => !EllipsisTooltip && Clipping;

        /// <summary>
        /// <see cref="LineClamp"/> 生效时 CSS 让文字先换行再纵向裁（<c>.is-truncated.is-line-clamp{white-space:normal}</c>），
        /// 判据必须换成高度；单行 nowrap 框的高度差来自 line-height 取整而不是截断。
        /// </summary>
        protected bool MeasuresVertical => LineClamp.HasValue && LineClamp.Value > 0;
    }
}
