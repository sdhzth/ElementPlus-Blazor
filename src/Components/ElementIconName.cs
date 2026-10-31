using System;
using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// 图标名归一：把「数据库里存的」「宿主手写的」各种写法收敛成 element-icons 字体里真实存在的那个字形名。
    /// 认的写法有 el-icon-user、user、User、UserFilled、user-filled 五类，口径只在这里有一份。
    ///
    /// 为什么要单独拿出来：这份逻辑原先只在 ElIcon 里（NormalizeIconName），而 ElMenuItem/ElSubMenu 这类
    /// 「字符串 Icon 参数」的组件各自只补了个前缀，于是同一个名字 <ElIcon Name="avatar"/> 有图、
    /// <ElMenuItem Icon="avatar"/> 是空白格。菜单图标要直接吃数据库字段（宿主批次92），这种不一致必须先抹平。
    ///
    /// 两份清单的来源（改 CSS 或加别名时会由 ElementIconNameTests 当场翻脸，不靠人记）：
    /// FontGlyphs = wwwroot/css/index.css 里所有 .el-icon-x:before 的 x；FontAliases = 枚举里有、字体里没有的退路。
    /// </summary>
    public static class ElementIconName
    {
        /// <summary>CSS 类名前缀。</summary>
        public const string Prefix = "el-icon-";

        /// <summary>
        /// Element UI 的 element-icons 字体只有 280 个字形，Icon 枚举里有 59 个名字在字体里没有对应字形，
        /// 直接输出类名会渲染成空白。这里把它们退到语义最接近的已有字形。Key = kebab-case 图标名。
        /// </summary>
        static readonly Dictionary<string, string> FontAliases = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["user-filled"] = "user-solid",
            ["avatar"] = "user",
            ["loading2"] = "loading",
            ["info-filled"] = "info",
            ["success-filled"] = "success",
            ["warning-filled"] = "warning",
            ["question-filled"] = "question",
            ["close-bold"] = "close",
            ["check-bold"] = "check",
            ["circle-close-filled"] = "circle-close",
            ["circle-check-filled"] = "circle-check",
            ["edit-pen"] = "edit-outline",
            ["histogram"] = "s-data",
            ["trend-charts"] = "s-marketing",
            ["video-camera-filled"] = "video-camera-solid",
            ["picture-filled"] = "picture",
            ["camera-filled"] = "camera-solid",
            ["microphone-filled"] = "microphone",
            ["bell-filled"] = "bell",
            ["d-arrow-up"] = "top",
            ["d-arrow-down"] = "bottom",
            ["filter"] = "s-operation",
            ["filter-filled"] = "s-operation",
            ["menu-fold"] = "s-fold",
            ["menu-unfold"] = "s-unfold",
            ["expand"] = "full-screen",
            ["fold"] = "s-fold",
            ["select"] = "mouse",
            ["select-filled"] = "mouse",
            ["tools"] = "setting",
            ["operation"] = "s-operation",
            ["unlink"] = "link",
            ["promotion"] = "s-promotion",
            ["credit-card"] = "bank-card",
            ["delete-filled"] = "delete-solid",
            ["delete-outline"] = "delete",
            ["clock"] = "time",
            ["calendar"] = "date",
            ["calendar-filled"] = "date",
            ["message-filled"] = "message-solid",
            ["iphone"] = "mobile-phone",
            ["location-filled"] = "location",
            ["house-filled"] = "house",
            ["shop"] = "s-shop",
            ["shop-filled"] = "s-shop",
            ["shopping-cart"] = "shopping-cart-2",
            ["shopping-bag"] = "shopping-bag-1",
            ["reading-active"] = "reading",
            ["notebooks"] = "notebook-2",
            ["notebook"] = "notebook-1",
            ["monitor-filled"] = "monitor",
            ["keyboard"] = "key",
            ["keyboard-filled"] = "key",
            ["magic-stick-filled"] = "magic-stick",
            ["stamp"] = "postcard",
            ["stamp-filled"] = "postcard",
            ["no-data"] = "sold-out",
            ["remove-filled"] = "remove",
            ["refresh-right-bold"] = "refresh-right"
        };

        /// <summary>字体里真有字形的名字（不含前缀）。</summary>
        static readonly HashSet<string> FontGlyphs = new HashSet<string>(StringComparer.Ordinal)
        {
            "add-location", "aim", "alarm-clock", "apple", "arrow-down", "arrow-left",
            "arrow-right", "arrow-up", "attract", "back", "bangzhu", "bank-card",
            "baseball", "basketball", "bell", "bicycle", "bottom", "bottom-left",
            "bottom-right", "box", "brush", "burger", "c-scale-to-original", "camera",
            "camera-solid", "caret-bottom", "caret-left", "caret-right", "caret-top", "chat-dot-round",
            "chat-dot-square", "chat-line-round", "chat-line-square", "chat-round", "chat-square", "check",
            "cherry", "chicken", "circle-check", "circle-close", "circle-plus", "circle-plus-outline",
            "close", "close-notification", "cloudy", "cloudy-and-sunny", "coffee", "coffee-cup",
            "coin", "cold-drink", "collection", "collection-tag", "connection", "coordinate",
            "copy-document", "cpu", "crop", "d-arrow-left", "d-arrow-right", "d-caret",
            "data-analysis", "data-board", "data-line", "date", "delete", "delete-location",
            "delete-solid", "dessert", "discount", "discover", "dish", "dish-1",
            "document", "document-add", "document-checked", "document-copy", "document-delete", "document-remove",
            "download", "edit", "edit-outline", "eleme", "error", "female",
            "files", "film", "finished", "first-aid-kit", "folder", "folder-add",
            "folder-checked", "folder-delete", "folder-opened", "folder-remove", "food", "football",
            "fork-spoon", "full-screen", "goblet", "goblet-full", "goblet-square", "goblet-square-full",
            "goods", "grape", "guide", "headset", "heavy-rain", "help",
            "hot-water", "house", "ice-cream", "ice-cream-round", "ice-cream-square", "ice-drink",
            "ice-tea", "info", "key", "knife-fork", "light-rain", "lightning",
            "link", "loading", "location", "location-information", "location-outline", "lock",
            "lollipop", "magic-stick", "male", "map-location", "medal", "medal-1",
            "menu", "message", "message-solid", "mic", "microphone", "milk-tea",
            "minus", "mobile", "mobile-phone", "money", "monitor", "moon",
            "moon-night", "more", "more-outline", "mouse", "news", "no-smoking",
            "notebook-1", "notebook-2", "odometer", "office-building", "open", "orange",
            "paperclip", "partly-cloudy", "pear", "phone", "phone-outline", "picture",
            "picture-outline", "picture-outline-round", "pie-chart", "place", "platform-eleme", "plus",
            "position", "postcard", "potato-strips", "present", "price-tag", "printer",
            "question", "rank", "reading", "receiving", "refresh", "refresh-left",
            "refresh-right", "refrigerator", "remove", "remove-outline", "right", "s-check",
            "s-claim", "s-comment", "s-cooperation", "s-custom", "s-data", "s-finance",
            "s-flag", "s-fold", "s-goods", "s-grid", "s-help", "s-home",
            "s-management", "s-marketing", "s-open", "s-operation", "s-opportunity", "s-order",
            "s-platform", "s-promotion", "s-release", "s-shop", "s-ticket", "s-tools",
            "s-unfold", "school", "scissors", "search", "sell", "service",
            "set-up", "setting", "share", "ship", "shopping-bag-1", "shopping-bag-2",
            "shopping-cart-1", "shopping-cart-2", "shopping-cart-full", "smoking", "soccer", "sold-out",
            "sort", "sort-down", "sort-up", "star-off", "star-on", "stopwatch",
            "success", "sugar", "suitcase", "suitcase-1", "sunny", "sunrise",
            "sunrise-1", "sunset", "switch-button", "table-lamp", "tableware", "takeaway-box",
            "thumb", "tickets", "time", "timer", "toilet-paper", "top",
            "top-left", "top-right", "trophy", "trophy-1", "truck", "turn-off",
            "turn-off-microphone", "umbrella", "unlock", "upload", "upload2", "user",
            "user-solid", "video-camera", "video-camera-solid", "video-pause", "video-play", "view",
            "wallet", "warning", "warning-outline", "watch", "watch-1", "water-cup",
            "watermelon", "wind-power", "zoom-in", "zoom-out"
        };

        /// <summary>字体里真有字形的名字（不含前缀）。宿主可拿它做强校验或做图标选择器；
        /// ElementIconNameTests 会拿 index.css 逐名对账，这份清单和 CSS 不可能悄悄分家。</summary>
        public static IReadOnlyCollection<string> GlyphNames => FontGlyphs;

        /// <summary>归一成不带前缀的字形名；空输入给空串。未知名字不猜，交给调用方决定退路。</summary>
        public static string Normalize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var value = raw.Trim();
            if (value.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(Prefix.Length);
            }

            return ApplyAlias(ToKebabCase(value));
        }

        /// <summary>归一并补成 CSS 类名；空输入给 null（组件据此决定「不渲染这个 i」）。</summary>
        public static string ToClass(string raw)
        {
            var name = Normalize(raw);
            return name.Length == 0 ? null : Prefix + name;
        }

        /// <summary>这个名字最终有没有字形（先走别名再查字形清单）。宿主用它决定要不要退回默认图标。</summary>
        public static bool IsKnown(string raw)
        {
            var name = Normalize(raw);
            return name.Length > 0 && FontGlyphs.Contains(name);
        }

        /// <summary>枚举值 → 不带前缀的字形名；Icon.None 给空串（未指定图标时不该输出任何 el-icon-* 类）。</summary>
        public static string FromEnum(Icon icon)
        {
            return icon == Icon.None ? string.Empty : ApplyAlias(ToKebabCase(icon.ToString()));
        }

        static string ApplyAlias(string name)
        {
            return string.IsNullOrEmpty(name) || !FontAliases.TryGetValue(name, out var alias) ? name : alias;
        }

        /// <summary>PascalCase / camelCase / 空格 → kebab-case：CirclePlusOutline → circle-plus-outline。</summary>
        static string ToKebabCase(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var sb = new System.Text.StringBuilder(value.Length + 4);
            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (char.IsUpper(c))
                {
                    if (i > 0)
                    {
                        sb.Append('-');
                    }
                    sb.Append(char.ToLowerInvariant(c));
                }
                else if (char.IsWhiteSpace(c))
                {
                    sb.Append('-');
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
