using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// <c>:shortcode:</c> 形式的 emoji 映射表。
    /// 仅收录常见短代码（参考实现内置了数百条），未收录的短代码会原样保留为文本。
    /// </summary>
    public static class ElMarkdownGrowEmoji
    {
        /// <summary>短代码 → 字符（不区分大小写）。</summary>
        public static readonly IReadOnlyDictionary<string, string> Map = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            // 表情
            { "smile", "\U0001F604" },
            { "smiley", "\U0001F603" },
            { "grinning", "\U0001F600" },
            { "grin", "\U0001F601" },
            { "laughing", "\U0001F606" },
            { "satisfied", "\U0001F606" },
            { "joy", "\U0001F602" },
            { "rofl", "\U0001F923" },
            { "blush", "\U0001F60A" },
            { "wink", "\U0001F609" },
            { "heart_eyes", "\U0001F60D" },
            { "kissing_heart", "\U0001F618" },
            { "thinking", "\U0001F914" },
            { "neutral_face", "\U0001F610" },
            { "expressionless", "\U0001F611" },
            { "worried", "\U0001F61F" },
            { "cry", "\U0001F622" },
            { "sob", "\U0001F62D" },
            { "angry", "\U0001F620" },
            { "rage", "\U0001F621" },
            { "scream", "\U0001F631" },
            { "sunglasses", "\U0001F60E" },
            { "sleeping", "\U0001F634" },
            { "poop", "\U0001F4A9" },
            { "ghost", "\U0001F47B" },
            { "skull", "\U0001F480" },
            { "robot", "\U0001F916" },
            { "alien", "\U0001F47D" },
            { "clown_face", "\U0001F921" },

            // 手势与人物
            { "thumbsup", "\U0001F44D" },
            { "+1", "\U0001F44D" },
            { "thumbsdown", "\U0001F44E" },
            { "-1", "\U0001F44E" },
            { "ok_hand", "\U0001F44C" },
            { "clap", "\U0001F44F" },
            { "pray", "\U0001F64F" },
            { "wave", "\U0001F44B" },
            { "muscle", "\U0001F4AA" },
            { "point_right", "\U0001F449" },
            { "point_left", "\U0001F448" },
            { "point_up", "\U0001F446" },
            { "point_down", "\U0001F447" },
            { "handshake", "\U0001F91D" },
            { "eyes", "\U0001F440" },

            // 心与符号
            { "heart", "\u2764\uFE0F" },
            { "broken_heart", "\U0001F494" },
            { "sparkling_heart", "\U0001F496" },
            { "star", "\u2B50" },
            { "sparkles", "\u2728" },
            { "fire", "\U0001F525" },
            { "boom", "\U0001F4A5" },
            { "zap", "\u26A1" },
            { "bulb", "\U0001F4A1" },
            { "warning", "\u26A0\uFE0F" },
            { "white_check_mark", "\u2705" },
            { "heavy_check_mark", "\u2714\uFE0F" },
            { "x", "\u274C" },
            { "no_entry", "\u26D4" },
            { "question", "\u2753" },
            { "exclamation", "\u2757" },
            { "100", "\U0001F4AF" },
            { "heavy_plus_sign", "\u2795" },
            { "heavy_minus_sign", "\u2796" },

            // 自然与物体
            { "sunny", "\u2600\uFE0F" },
            { "cloud", "\u2601\uFE0F" },
            { "umbrella", "\u2614" },
            { "snowflake", "\u2744\uFE0F" },
            { "moon", "\U0001F319" },
            { "earth_asia", "\U0001F30F" },
            { "rocket", "\U0001F680" },
            { "tada", "\U0001F389" },
            { "gift", "\U0001F381" },
            { "trophy", "\U0001F3C6" },
            { "medal", "\U0001F3C5" },
            { "lock", "\U0001F512" },
            { "unlock", "\U0001F513" },
            { "key", "\U0001F511" },
            { "bell", "\U0001F514" },
            { "memo", "\U0001F4DD" },
            { "book", "\U0001F4D6" },
            { "books", "\U0001F4DA" },
            { "paperclip", "\U0001F4CE" },
            { "link", "\U0001F517" },
            { "package", "\U0001F4E6" },
            { "wrench", "\U0001F527" },
            { "hammer", "\U0001F528" },
            { "bug", "\U0001F41B" },
            { "mag", "\U0001F50D" },
            { "chart_with_upwards_trend", "\U0001F4C8" },
            { "bar_chart", "\U0001F4CA" },
            { "clipboard", "\U0001F4CB" },
            { "computer", "\U0001F4BB" },
            { "iphone", "\U0001F4F1" },
            { "email", "\U0001F4E7" },
            { "phone", "\u260E\uFE0F" },
            { "calendar", "\U0001F4C5" },
            { "clock1", "\U0001F550" },
            { "hourglass", "\u231B" },
            { "coffee", "\u2615" },
            { "beer", "\U0001F37B" },
            { "pizza", "\U0001F355" },
            { "apple", "\U0001F34E" },
            { "car", "\U0001F697" },
            { "airplane", "\u2708\uFE0F" },
            { "house", "\U0001F3E0" },
            { "office", "\U0001F3E2" },
            { "hospital", "\U0001F3E5" },
            { "page_facing_up", "\U0001F4C4" },
            { "inbox_tray", "\U0001F4E5" },
            { "outbox_tray", "\U0001F4E4" },
            { "arrow_right", "\u27A1\uFE0F" },
            { "arrow_left", "\u2B05\uFE0F" },
            { "arrow_up", "\u2B06\uFE0F" },
            { "arrow_down", "\u2B07\uFE0F" },
            { "recycle", "\u267B\uFE0F" },
            { "construction", "\U0001F6A7" },
            { "red_circle", "\U0001F534" },
            { "large_blue_circle", "\U0001F535" },
            { "white_circle", "\u26AA" },
            { "black_circle", "\u26AB" },
        };

        /// <summary>尝试解析短代码。</summary>
        public static bool TryGet(string shortcode, out string glyph)
        {
            glyph = null;

            if (string.IsNullOrEmpty(shortcode))
            {
                return false;
            }

            return Map.TryGetValue(shortcode, out glyph);
        }

        /// <summary>映射表条目数。</summary>
        public static int Count => Map.Count;
    }
}
