using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using System;

namespace ElementPlus
{
    public partial class ElAvatar : ElementComponentBase
    {
        [Parameter]
        public string Src { get; set; }

        [Parameter]
        public string Alt { get; set; }

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public RenderFragment IconContent { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public ElementSize Size { get; set; } = ElementSize.Default;

        [Parameter]
        public string CustomSize { get; set; }

        [Parameter]
        public AvatarShape Shape { get; set; } = AvatarShape.Circle;

        [Parameter]
        public string Fit { get; set; } = "cover";

        /// <summary>
        /// 自定义背景色（任意 CSS 颜色：<c>#409eff</c>、<c>var(--el-color-success)</c>、<c>rgb(64,158,255)</c> 等）。
        /// 不传时沿用 Element Plus 默认灰 <c>#c0c4cc</c>。比 <see cref="ElementComponentBase.Style" /> 里写的 background 优先级更高。
        /// </summary>
        [Parameter]
        public string Color { get; set; }

        /// <summary>
        /// 自定义文字色（文字头像 / 图标头像的前景色）。
        /// 不传时沿用 Element Plus 默认白 <c>#fff</c>。比 <see cref="ElementComponentBase.Style" /> 里写的 color 优先级更高。
        /// </summary>
        [Parameter]
        public string TextColor { get; set; }

        /// <summary>图片加载失败时的图标</summary>
        [Parameter]
        public string ErrorIcon { get; set; } = "el-icon-picture-outline";

        /// <summary>响应式 srcset</summary>
        [Parameter]
        public string SrcSet { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        /// <summary>图片加载失败事件</summary>
        [Parameter]
        public EventCallback OnError { get; set; }

        private bool isImageError;

        protected string AvatarClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-avatar", $"el-avatar--{Shape.ToString().ToLower()}", Cls)
            .AddIf(Size == ElementSize.Large, "el-avatar--large")
            .AddIf(Size == ElementSize.Small, "el-avatar--small")
            .AddIf(!string.IsNullOrWhiteSpace(Icon) || IconContent != null, "el-avatar--icon")
            .ToString();

        protected string AvatarStyle
        {
            get
            {
                var builder = HtmlPropertyBuilder.CreateCssStyleBuilder()
                    .Add(Style);

                if (!string.IsNullOrWhiteSpace(CustomSize))
                {
                    var size = ElementCssUtility.NormalizeCssSize(CustomSize);
                    builder.Add($"width:{size}", $"height:{size}", $"line-height:{size}");
                }

                // 颜色参数渲染在内联样式里（而非新增 CSS 类），
                // 这样任意 CSS 颜色都能直接用，且不需要在 index.css 补丁层（原 fix.css）里为每种颜色开一条规则。
                if (!string.IsNullOrWhiteSpace(Color))
                {
                    builder.Add($"background-color:{Color.Trim()}");
                }

                if (!string.IsNullOrWhiteSpace(TextColor))
                {
                    builder.Add($"color:{TextColor.Trim()}");
                }

                return builder.ToString();
            }
        }

        protected string ImageStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add("width:100%")
            .AddIf(!string.IsNullOrWhiteSpace(Fit), $"object-fit:{Fit}")
            .ToString();

        protected string IconClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add(NormalizeIcon(Icon))
            .ToString();

        protected string ErrorIconClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add(NormalizeIcon(ErrorIcon))
            .ToString();

        protected async Task OnImageErrorAsync()
        {
            isImageError = true;
            StateHasChanged();
            if (OnError.HasDelegate)
            {
                await OnError.InvokeAsync();
            }
        }

        protected string DisplayText => GetInitialsFromAlt(Alt);

        private static string GetInitialsFromAlt(string alt)
        {
            if (string.IsNullOrWhiteSpace(alt)) return string.Empty;
            var trimmed = alt.Trim();
            // 取每个单词首字母，或中文字符串首字
            var words = trimmed.Split(new[] { ' ', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2)
            {
                return char.ToUpperInvariant(words[0][0]).ToString() + char.ToUpperInvariant(words[^1][0]).ToString();
            }
            return trimmed.Length > 0 ? char.ToUpperInvariant(trimmed[0]).ToString() : string.Empty;
        }

        private static string NormalizeIcon(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var trimmed = value.Trim();
            return trimmed.StartsWith("el-icon-") ? trimmed : $"el-icon-{trimmed}";
        }
    }
}
