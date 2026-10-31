using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ElementPlus
{
    /// <summary>
    /// <see cref="ElViewerGrow"/> 的纯 C# 引擎：类型识别、URL 安全、文件名推断、体积格式化、
    /// 缩放阶梯、旋转归一、内联 data URI、外链预览地址拼装。
    /// <para>
    /// 这一层不引用任何 Blazor 类型，也不依赖 JavaScript —— 参考实现把这些能力全部外包给外部
    /// JS SDK（10MB 的 <c>jit-viewer.min.js</c>），本实现改为自带，因此可以在单元测试里直接断言。
    /// </para>
    /// </summary>
    public static class ElViewerGrowEngine
    {
        /// <summary>缩放阶梯：放大/缩小在档位之间跳动，避免参考实现「±0.2 无限累加出 1.0000000002」的问题。</summary>
        public static readonly double[] ZoomLadder = { 0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 2.0, 3.0, 4.0 };

        /// <summary>内联 data URI 的体积上限（超过则不再把字节内联到页面上，避免超大 DOM）。</summary>
        public const long MaxDataUriBytes = 8L * 1024 * 1024;

        private static readonly Dictionary<string, ElViewerDocumentKind> ExtensionMap = CreateExtensionMap();

        private static readonly Dictionary<string, ElViewerDocumentKind> ContentTypeMap = CreateContentTypeMap();

        private static readonly Dictionary<ElViewerDocumentKind, string> DefaultContentTypes = CreateDefaultContentTypes();

        private static readonly string[] DataUriWhitelist =
        {
            "data:image/", "data:video/", "data:audio/", "data:application/pdf",
            "data:text/plain", "data:text/csv", "data:text/markdown", "data:application/json"
        };

        private static readonly string[] UrlSchemeWhitelist = { "http", "https", "blob" };

        #region 类型识别

        private static Dictionary<string, ElViewerDocumentKind> CreateExtensionMap()
        {
            var map = new Dictionary<string, ElViewerDocumentKind>(StringComparer.OrdinalIgnoreCase)
            {
                [".pdf"] = ElViewerDocumentKind.Pdf,
                [".doc"] = ElViewerDocumentKind.Word,
                [".docx"] = ElViewerDocumentKind.Word,
                [".rtf"] = ElViewerDocumentKind.Word,
                [".xls"] = ElViewerDocumentKind.Excel,
                [".xlsx"] = ElViewerDocumentKind.Excel,
                [".ppt"] = ElViewerDocumentKind.PowerPoint,
                [".pptx"] = ElViewerDocumentKind.PowerPoint,
                [".ofd"] = ElViewerDocumentKind.Ofd,
                [".txt"] = ElViewerDocumentKind.Text,
                [".log"] = ElViewerDocumentKind.Text,
                [".ini"] = ElViewerDocumentKind.Text,
                [".json"] = ElViewerDocumentKind.Json,
                [".md"] = ElViewerDocumentKind.Markdown,
                [".markdown"] = ElViewerDocumentKind.Markdown,
                [".csv"] = ElViewerDocumentKind.Csv,
                [".tsv"] = ElViewerDocumentKind.Csv,
                [".png"] = ElViewerDocumentKind.Image,
                [".jpg"] = ElViewerDocumentKind.Image,
                [".jpeg"] = ElViewerDocumentKind.Image,
                [".gif"] = ElViewerDocumentKind.Image,
                [".bmp"] = ElViewerDocumentKind.Image,
                [".webp"] = ElViewerDocumentKind.Image,
                [".svg"] = ElViewerDocumentKind.Image,
                [".ico"] = ElViewerDocumentKind.Image,
                [".mp4"] = ElViewerDocumentKind.Video,
                [".webm"] = ElViewerDocumentKind.Video,
                [".ogg"] = ElViewerDocumentKind.Video,
                [".ogv"] = ElViewerDocumentKind.Video,
                [".mov"] = ElViewerDocumentKind.Video,
                [".mp3"] = ElViewerDocumentKind.Audio,
                [".wav"] = ElViewerDocumentKind.Audio,
                [".m4a"] = ElViewerDocumentKind.Audio,
                [".flac"] = ElViewerDocumentKind.Audio,
                [".dxf"] = ElViewerDocumentKind.Cad,
                [".dwg"] = ElViewerDocumentKind.Cad,
                [".glb"] = ElViewerDocumentKind.Model,
                [".gltf"] = ElViewerDocumentKind.Model,
                [".stl"] = ElViewerDocumentKind.Model
            };

            return map;
        }

        private static Dictionary<string, ElViewerDocumentKind> CreateContentTypeMap()
        {
            var map = new Dictionary<string, ElViewerDocumentKind>(StringComparer.OrdinalIgnoreCase)
            {
                ["application/pdf"] = ElViewerDocumentKind.Pdf,
                ["application/x-pdf"] = ElViewerDocumentKind.Pdf,
                ["application/msword"] = ElViewerDocumentKind.Word,
                ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = ElViewerDocumentKind.Word,
                ["application/vnd.ms-excel"] = ElViewerDocumentKind.Excel,
                ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = ElViewerDocumentKind.Excel,
                ["application/vnd.ms-powerpoint"] = ElViewerDocumentKind.PowerPoint,
                ["application/vnd.openxmlformats-officedocument.presentationml.presentation"] = ElViewerDocumentKind.PowerPoint,
                ["application/ofd"] = ElViewerDocumentKind.Ofd,
                ["text/plain"] = ElViewerDocumentKind.Text,
                ["text/markdown"] = ElViewerDocumentKind.Markdown,
                ["text/x-markdown"] = ElViewerDocumentKind.Markdown,
                ["text/csv"] = ElViewerDocumentKind.Csv,
                ["text/tab-separated-values"] = ElViewerDocumentKind.Csv,
                ["application/json"] = ElViewerDocumentKind.Json,
                ["model/gltf-binary"] = ElViewerDocumentKind.Model,
                ["model/gltf+json"] = ElViewerDocumentKind.Model
            };

            return map;
        }

        private static Dictionary<ElViewerDocumentKind, string> CreateDefaultContentTypes()
        {
            return new Dictionary<ElViewerDocumentKind, string>
            {
                [ElViewerDocumentKind.Pdf] = "application/pdf",
                [ElViewerDocumentKind.Word] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                [ElViewerDocumentKind.Excel] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                [ElViewerDocumentKind.PowerPoint] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                [ElViewerDocumentKind.Ofd] = "application/ofd",
                [ElViewerDocumentKind.Text] = "text/plain",
                [ElViewerDocumentKind.Markdown] = "text/markdown",
                [ElViewerDocumentKind.Csv] = "text/csv",
                [ElViewerDocumentKind.Json] = "application/json",
                [ElViewerDocumentKind.Image] = "image/png",
                [ElViewerDocumentKind.Video] = "video/mp4",
                [ElViewerDocumentKind.Audio] = "audio/mpeg",
                [ElViewerDocumentKind.Cad] = "application/octet-stream",
                [ElViewerDocumentKind.Model] = "model/gltf-binary",
                [ElViewerDocumentKind.Unknown] = "application/octet-stream"
            };
        }

        /// <summary>取文件的扩展名（含点）；取不到返回 <c>null</c>。</summary>
        public static string GetExtension(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            var index = fileName.LastIndexOf('.');

            if (index < 0 || index == fileName.Length - 1)
            {
                return null;
            }

            return fileName.Substring(index);
        }

        /// <summary>
        /// 识别文档类型：先按扩展名，再按 MIME。两者冲突时以扩展名为准（更贴近用户直觉），
        /// 都没有时返回 <see cref="ElViewerDocumentKind.Unknown"/>。
        /// </summary>
        public static ElViewerDocumentKind ResolveKind(string fileName, string contentType)
        {
            var extension = GetExtension(fileName);

            if (extension != null && ExtensionMap.TryGetValue(extension, out var byExtension))
            {
                return byExtension;
            }

            if (!string.IsNullOrWhiteSpace(contentType))
            {
                var mime = contentType.Split(';')[0].Trim();

                if (mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    return ElViewerDocumentKind.Image;
                }

                if (mime.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
                {
                    return ElViewerDocumentKind.Video;
                }

                if (mime.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
                {
                    return ElViewerDocumentKind.Audio;
                }

                if (ContentTypeMap.TryGetValue(mime, out var byMime))
                {
                    return byMime;
                }
            }

            return ElViewerDocumentKind.Unknown;
        }

        private static readonly Dictionary<string, string> MediaContentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".bmp"] = "image/bmp",
            [".webp"] = "image/webp",
            [".svg"] = "image/svg+xml",
            [".ico"] = "image/x-icon",
            [".mp4"] = "video/mp4",
            [".webm"] = "video/webm",
            [".ogg"] = "video/ogg",
            [".ogv"] = "video/ogg",
            [".mov"] = "video/quicktime",
            [".mp3"] = "audio/mpeg",
            [".wav"] = "audio/wav",
            [".m4a"] = "audio/mp4",
            [".flac"] = "audio/flac"
        };

        /// <summary>取该类型默认的 MIME；识别不了时返回 <c>application/octet-stream</c>。</summary>
        public static string ResolveContentType(ElViewerDocumentKind kind)
        {
            return DefaultContentTypes.TryGetValue(kind, out var value) ? value : "application/octet-stream";
        }

        /// <summary>
        /// 取 MIME：图片 / 音视频按扩展名给出精确类型（<c>image/*</c> 这种通配值浏览器不认，
        /// 会让内联 data URI 变成空白），其余按类型给默认值。
        /// </summary>
        public static string ResolveContentType(string fileName, ElViewerDocumentKind kind)
        {
            var extension = GetExtension(fileName);

            if (extension != null && MediaContentTypes.TryGetValue(extension, out var mime))
            {
                return mime;
            }

            return ResolveContentType(kind);
        }

        /// <summary>是否为文本型文档（可以解码成字符串交给内建渲染器）。</summary>
        public static bool IsTextual(ElViewerDocumentKind kind)
        {
            return kind == ElViewerDocumentKind.Text
                || kind == ElViewerDocumentKind.Markdown
                || kind == ElViewerDocumentKind.Csv
                || kind == ElViewerDocumentKind.Json;
        }

        /// <summary>内建渲染器能否直接呈现（不需要任何外部服务）。</summary>
        public static bool IsBuiltInSupported(ElViewerDocumentKind kind)
        {
            return IsTextual(kind)
                || kind == ElViewerDocumentKind.Image
                || kind == ElViewerDocumentKind.Video
                || kind == ElViewerDocumentKind.Audio
                || kind == ElViewerDocumentKind.Pdf;
        }

        /// <summary>该类型是否只能通过「外部预览服务 / 浏览器内建能力」呈现。</summary>
        public static bool NeedsExternalViewer(ElViewerDocumentKind kind)
        {
            return !IsBuiltInSupported(kind) && kind != ElViewerDocumentKind.Unknown;
        }

        #endregion

        #region URL 与文件名

        /// <summary>
        /// 从 URL 推断文件名：去掉查询串与片段，取最后一段并做一次 URL 解码。
        /// 参考实现直接 <c>InputUrl.Split('/').Last()</c>，遇到带查询串或百分号编码的链接会拿到脏名字。
        /// </summary>
        public static string GuessFileName(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            var value = url.Trim();

            var hash = value.IndexOf('#');

            if (hash >= 0)
            {
                value = value.Substring(0, hash);
            }

            var query = value.IndexOf('?');

            if (query >= 0)
            {
                value = value.Substring(0, query);
            }

            var slash = value.LastIndexOf('/');
            var backslash = value.LastIndexOf('\\');
            var cut = Math.Max(slash, backslash);

            if (cut >= 0)
            {
                value = value.Substring(cut + 1);
            }

            if (value.Length == 0)
            {
                return null;
            }

            try
            {
                var decoded = Uri.UnescapeDataString(value);
                return decoded.Length == 0 ? value : decoded;
            }
            catch (Exception)
            {
                return value;
            }
        }

        /// <summary>
        /// URL 安全性判断：只放行 http/https/blob 与白名单内的 data URI 以及相对地址，
        /// 其它一律拒绝（<c>javascript:</c>、<c>vbscript:</c>、<c>file:</c>、<c>data:text/html</c> 等）。
        /// <para>
        /// 会先做一次实体解码再判断，避免 <c>&amp;#106;avascript:</c> / <c>javascript&amp;colon;</c>
        /// 这类「写进属性、浏览器解析后才变成危险协议」的绕过。
        /// </para>
        /// </summary>
        public static bool IsSafeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            var value = StripControlCharacters(DecodeEntities(url)).Trim();

            if (value.Length == 0)
            {
                return false;
            }

            if (value.StartsWith("/", StringComparison.Ordinal)
                || value.StartsWith("./", StringComparison.Ordinal)
                || value.StartsWith("../", StringComparison.Ordinal))
            {
                return true;
            }

            var colon = value.IndexOf(':');

            if (colon <= 0)
            {
                // 没有协议 → 相对地址，安全。
                return true;
            }

            var scheme = value.Substring(0, colon).ToLowerInvariant();

            foreach (var allowed in UrlSchemeWhitelist)
            {
                if (scheme == allowed)
                {
                    return true;
                }
            }

            if (scheme == "data")
            {
                foreach (var prefix in DataUriWhitelist)
                {
                    if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>解码数字字符实体与 <c>&amp;colon;</c> 等少量具名实体（仅为安全判断用）。</summary>
        public static string DecodeEntities(string value)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOf('&') < 0)
            {
                return value ?? string.Empty;
            }

            var builder = new StringBuilder(value.Length);

            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];

                if (ch != '&')
                {
                    builder.Append(ch);
                    continue;
                }

                var semicolon = value.IndexOf(';', i + 1);

                if (semicolon < 0 || semicolon - i > 12)
                {
                    builder.Append(ch);
                    continue;
                }

                var body = value.Substring(i + 1, semicolon - i - 1);
                var decoded = DecodeEntityBody(body);

                if (decoded == null)
                {
                    builder.Append(ch);
                    continue;
                }

                builder.Append(decoded);
                i = semicolon;
            }

            return builder.ToString();
        }

        private static string DecodeEntityBody(string body)
        {
            if (body.Length == 0)
            {
                return null;
            }

            if (body[0] == '#')
            {
                var text = body.Substring(1);
                int code;

                if (text.Length > 1 && (text[0] == 'x' || text[0] == 'X'))
                {
                    if (!int.TryParse(text.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                    {
                        return null;
                    }
                }
                else if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out code))
                {
                    return null;
                }

                if (code < 0 || code > 0x10FFFF)
                {
                    return null;
                }

                return ((char)code).ToString();
            }

            switch (body.ToLowerInvariant())
            {
                case "colon":
                    return ":";
                case "amp":
                    return "&";
                case "lt":
                    return "<";
                case "gt":
                    return ">";
                case "quot":
                    return "\"";
                case "apos":
                    return "'";
                default:
                    return null;
            }
        }

        private static string StripControlCharacters(string value)
        {
            var builder = new StringBuilder(value.Length);

            foreach (var ch in value)
            {
                if (ch < ' ' || ch == '\u007F')
                {
                    continue;
                }

                builder.Append(ch);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 拼装外部预览地址。模板里支持 <c>{url}</c>、<c>{name}</c>、<c>{ext}</c>、<c>{kind}</c> 四个占位符，
        /// <c>{url}</c> 会做 URL 编码。模板为空或地址不安全时返回 <c>null</c>。
        /// </summary>
        public static string BuildExternalUrl(string template, ElViewerDocument document)
        {
            if (string.IsNullOrWhiteSpace(template) || document == null)
            {
                return null;
            }

            var url = document.Url;

            if (string.IsNullOrWhiteSpace(url) || !IsSafeUrl(url))
            {
                return null;
            }

            var name = document.FileName ?? string.Empty;
            var extension = GetExtension(name) ?? string.Empty;

            return template
                .Replace("{url}", Uri.EscapeDataString(url))
                .Replace("{name}", Uri.EscapeDataString(name))
                .Replace("{ext}", Uri.EscapeDataString(extension))
                .Replace("{kind}", document.Kind.ToString());
        }

        #endregion

        #region 字节与文本

        /// <summary>把字节解码为文本：自动识别 UTF-8 / UTF-16 的 BOM，随后统一换行符。</summary>
        public static bool TryDecodeText(byte[] content, out string text)
        {
            text = null;

            if (content == null || content.Length == 0)
            {
                return false;
            }

            Encoding encoding = Encoding.UTF8;
            var offset = 0;

            if (content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF)
            {
                offset = 3;
            }
            else if (content.Length >= 2 && content[0] == 0xFF && content[1] == 0xFE)
            {
                encoding = Encoding.Unicode;
                offset = 2;
            }
            else if (content.Length >= 2 && content[0] == 0xFE && content[1] == 0xFF)
            {
                encoding = Encoding.BigEndianUnicode;
                offset = 2;
            }

            try
            {
                text = NormalizeNewLines(encoding.GetString(content, offset, content.Length - offset));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>把文本编码为 UTF-8 字节（不带 BOM）。</summary>
        public static byte[] EncodeText(string text)
        {
            return new UTF8Encoding(false).GetBytes(text ?? string.Empty);
        }

        /// <summary>统一换行符为 <c>\n</c>。</summary>
        public static string NormalizeNewLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text ?? string.Empty;
            }

            return text.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        /// <summary>
        /// 生成内联 data URI。超过 <see cref="MaxDataUriBytes"/> 或类型不在白名单内时返回 <c>false</c>，
        /// 避免把几十兆的字节塞进 DOM。
        /// </summary>
        public static bool TryBuildDataUri(byte[] content, string contentType, out string uri)
        {
            uri = null;

            if (content == null || content.Length == 0 || content.Length > MaxDataUriBytes)
            {
                return false;
            }

            var mime = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Split(';')[0].Trim();
            var candidate = "data:" + mime + ";base64," + Convert.ToBase64String(content);

            if (!IsSafeUrl(candidate))
            {
                return false;
            }

            uri = candidate;
            return true;
        }

        /// <summary>体积格式化（1024 进制，固定小数位，与文化无关）。</summary>
        public static string FormatSize(long bytes)
        {
            if (bytes <= 0)
            {
                return null;
            }

            string[] units = { "B", "KB", "MB", "GB", "TB" };
            var value = (double)bytes;
            var index = 0;

            while (value >= 1024 && index < units.Length - 1)
            {
                value /= 1024;
                index++;
            }

            return index == 0
                ? bytes.ToString(CultureInfo.InvariantCulture) + " " + units[index]
                : value.ToString("0.##", CultureInfo.InvariantCulture) + " " + units[index];
        }

        #endregion

        #region 缩放与旋转

        /// <summary>把缩放限制在 [min, max] 内；min/max 非法时退化为 [0.1, 10]。</summary>
        public static double ClampZoom(double value, double min, double max)
        {
            var low = min > 0 ? min : 0.1;
            var high = max > 0 ? max : 10.0;

            if (high < low)
            {
                var swap = low;
                low = high;
                high = swap;
            }

            if (double.IsNaN(value))
            {
                return 1.0;
            }

            if (value < low)
            {
                return low;
            }

            return value > high ? high : value;
        }

        /// <summary>
        /// 沿缩放阶梯跳一档。<paramref name="direction"/> 大于 0 为放大，小于 0 为缩小。
        /// 当前值不在阶梯上时回退为「±0.25 再夹紧」，保证任何初始值都能动。
        /// </summary>
        public static double StepZoom(double current, int direction, double min, double max)
        {
            var start = ClampZoom(current, min, max);
            var target = 0.0;

            if (direction > 0)
            {
                for (var i = 0; i < ZoomLadder.Length; i++)
                {
                    if (ZoomLadder[i] > start + 0.0001)
                    {
                        target = ZoomLadder[i];
                        break;
                    }
                }

                if (target <= 0)
                {
                    target = start + 0.25;
                }
            }
            else if (direction < 0)
            {
                for (var i = ZoomLadder.Length - 1; i >= 0; i--)
                {
                    if (ZoomLadder[i] < start - 0.0001)
                    {
                        target = ZoomLadder[i];
                        break;
                    }
                }

                if (target <= 0)
                {
                    target = start - 0.25;
                }
            }
            else
            {
                return start;
            }

            return ClampZoom(target, min, max);
        }

        /// <summary>把角度归一到 0 / 90 / 180 / 270。</summary>
        public static int NormalizeRotation(int degree)
        {
            if (degree == 0)
            {
                return 0;
            }

            var quarters = (int)Math.Round(degree / 90.0, MidpointRounding.AwayFromZero);
            var normalized = quarters % 4;

            if (normalized < 0)
            {
                normalized += 4;
            }

            return normalized * 90;
        }

        /// <summary>生成画布的 CSS transform；无需变换时返回空串（避免多余的样式属性）。</summary>
        public static string BuildTransform(double zoom, int rotation)
        {
            var scale = double.IsNaN(zoom) ? 1.0 : zoom;
            var angle = NormalizeRotation(rotation);

            if (Math.Abs(scale - 1.0) < 0.0001 && angle == 0)
            {
                return string.Empty;
            }

            var parts = new List<string>(2);

            if (Math.Abs(scale - 1.0) >= 0.0001)
            {
                parts.Add("scale(" + scale.ToString("0.###", CultureInfo.InvariantCulture) + ")");
            }

            if (angle != 0)
            {
                parts.Add("rotate(" + angle.ToString(CultureInfo.InvariantCulture) + "deg)");
            }

            return string.Join(" ", parts);
        }

        /// <summary>缩放百分比文本（与文化无关）。</summary>
        public static string FormatZoom(double zoom)
        {
            var value = double.IsNaN(zoom) ? 1.0 : zoom;

            return (int)Math.Round(value * 100) + "%";
        }

        /// <summary>页码文本：没有总页数时只显示当前页。</summary>
        public static string BuildPageLabel(int page, int count)
        {
            if (page <= 0 && count <= 0)
            {
                return string.Empty;
            }

            if (count <= 0)
            {
                return page.ToString(CultureInfo.InvariantCulture);
            }

            var current = page <= 0 ? 1 : page;

            if (current > count)
            {
                current = count;
            }

            return current.ToString(CultureInfo.InvariantCulture) + " / " + count.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>把页码夹到 [1, count]；count 小于等于 0 时不做限制。</summary>
        public static int ClampPage(int page, int count)
        {
            if (count <= 0)
            {
                return page < 1 ? 1 : page;
            }

            if (page < 1)
            {
                return 1;
            }

            return page > count ? count : page;
        }

        #endregion

        #region 类型显示名

        /// <summary>
        /// 类型的中性显示名（PDF / Word / Excel…）。
        /// 刻意不做本地化：这些是专有名词，翻译反而更难辨认；真正需要本地化的文案都在
        /// <see cref="ElViewerGrowStrings"/> 里。
        /// </summary>
        public static string GetKindDisplayName(ElViewerDocumentKind kind)
        {
            switch (kind)
            {
                case ElViewerDocumentKind.Pdf:
                    return "PDF";
                case ElViewerDocumentKind.Word:
                    return "Word";
                case ElViewerDocumentKind.Excel:
                    return "Excel";
                case ElViewerDocumentKind.PowerPoint:
                    return "PowerPoint";
                case ElViewerDocumentKind.Ofd:
                    return "OFD";
                case ElViewerDocumentKind.Text:
                    return "Text";
                case ElViewerDocumentKind.Markdown:
                    return "Markdown";
                case ElViewerDocumentKind.Csv:
                    return "CSV";
                case ElViewerDocumentKind.Json:
                    return "JSON";
                case ElViewerDocumentKind.Image:
                    return "Image";
                case ElViewerDocumentKind.Video:
                    return "Video";
                case ElViewerDocumentKind.Audio:
                    return "Audio";
                case ElViewerDocumentKind.Cad:
                    return "CAD";
                case ElViewerDocumentKind.Model:
                    return "3D";
                default:
                    return "Unknown";
            }
        }

        #endregion

        #region 默认 accept

        /// <summary>
        /// 文件选择框的默认 accept：只包含内建渲染器真正支持的类型，
        /// 参考实现把 dxf/glb/stl 一并列出但 Blazor 侧根本没有对应能力，属于误导。
        /// </summary>
        public static string DefaultAccept
        {
            get { return ".pdf,.txt,.md,.markdown,.csv,.tsv,.json,.log,.png,.jpg,.jpeg,.gif,.bmp,.webp,.svg,.mp4,.webm,.ogg,.mp3,.wav"; }
        }

        #endregion
    }
}
