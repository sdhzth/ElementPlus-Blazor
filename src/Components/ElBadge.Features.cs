using Microsoft.AspNetCore.Components;
using System;
using System.Globalization;

namespace ElementPlus
{
    public partial class ElBadge
    {
        [Parameter]
        public string Text { get; set; }

        [Parameter]
        public int? Max { get; set; }

        [Parameter]
        public bool IsDot { get; set; }

        [Parameter]
        public bool Dot
        {
            get => IsDot;
            set => IsDot = value;
        }

        [Parameter]
        public bool Hidden { get; set; }

        protected bool IsHidden => Hidden || (!ShowZero && !IsDot && Value == 0 && string.IsNullOrWhiteSpace(Text));

        [Parameter]
        public int? OffsetX { get; set; }

        [Parameter]
        public int? OffsetY { get; set; }

        [Parameter]
        public string Offset { get; set; }

        /// <summary>自定义背景色</summary>
        [Parameter]
        public string Color { get; set; }

        /// <summary>位置 (top-right/top-left/bottom-right/bottom-left)</summary>
        [Parameter]
        public BadgePosition Position { get; set; } = BadgePosition.TopRight;

        /// <summary>Value 为 0 时是否显示</summary>
        [Parameter]
        public bool ShowZero { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        protected string DisplayValue => !string.IsNullOrWhiteSpace(Text)
            ? Text
            : Max.HasValue && Value > Max.Value
                ? $"{Max.Value}+"
                : Value.ToString(CultureInfo.InvariantCulture);

        protected string BadgeClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-badge", Cls)
            .ToString();

        protected string ContentClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-badge__content", $"el-badge__content--{TypeName}", "is-fixed")
            .AddIf(IsDot, "is-dot")
            .AddIf(Position == BadgePosition.TopLeft, "el-badge__content--top-left")
            .AddIf(Position == BadgePosition.BottomRight, "el-badge__content--bottom-right")
            .AddIf(Position == BadgePosition.BottomLeft, "el-badge__content--bottom-left")
            .ToString();

        protected string ContentStyle
        {
            get
            {
                var builder = HtmlPropertyBuilder.CreateCssStyleBuilder();
                var (x, y) = ResolveOffset();
                if (x.HasValue)
                {
                    builder.Add($"right:{-x.Value}px");
                }

                if (y.HasValue)
                {
                    builder.Add($"margin-top:{y.Value}px");
                }

                if (!string.IsNullOrWhiteSpace(Color))
                {
                    builder.Add($"background-color:{Color}");
                }

                return builder.ToString();
            }
        }

        private string TypeName => Convert.ToInt32(Type, CultureInfo.InvariantCulture) == 3
            ? "danger"
            : Type.ToString().ToLowerInvariant();

        private (int? x, int? y) ResolveOffset()
        {
            if (OffsetX.HasValue || OffsetY.HasValue)
            {
                return (OffsetX, OffsetY);
            }

            if (string.IsNullOrWhiteSpace(Offset))
            {
                return (null, null);
            }

            var parts = Offset.Split(',', ';', ' ');
            int? x = null;
            int? y = null;
            if (parts.Length > 0 && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedX))
            {
                x = parsedX;
            }

            if (parts.Length > 1 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedY))
            {
                y = parsedY;
            }

            return (x, y);
        }
    }
}
