using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace ElementPlus
{
    /// <summary>
    /// <see cref="ElViewerGrow"/> 的内建 Office 渲染器：把 docx / xlsx / pptx 的字节直接解析成 HTML。
    /// <para>
    /// 参考实现（jit-viewer Blazor 封装）把这件事外包给 10 MB 的 <c>jit-viewer.min.js</c>
    /// （内含 SheetJS / JSZip / ExcelJS 等一整套第三方解析器）；本实现改用 .NET 自带的
    /// <see cref="ZipArchive"/> 与 <see cref="XDocument"/> 自己读 OOXML ——
    /// 三者本质上都是「ZIP + 若干 XML」，**不需要任何第三方依赖，也不需要下载任何脚本**。
    /// </para>
    /// <para>
    /// 边界（刻意如此）：只解析 ZIP 包裹的 OOXML（<c>.docx</c> / <c>.xlsx</c> / <c>.pptx</c>）。
    /// <c>.doc</c> / <c>.xls</c> / <c>.ppt</c> 是 OLE 复合二进制文档，不在本类能力范围内，
    /// 由组件给出「请另存为新格式」的明确提示，而不是含糊地说「不支持」。
    /// </para>
    /// <para>
    /// 产出为 HTML 片段（单一真源，渲染与单元测试共用），所有文本都过
    /// <see cref="ElViewerGrowRenderer.EscapeHtml"/>，图片一律转成内联 data URI，
    /// 因此结果可以直接交给 <c>MarkupString</c> 渲染，不会成为注入通道。
    /// </para>
    /// </summary>
    public static class ElViewerGrowOffice
    {
        /// <summary>该类型 + 该字节内容是否可以被内建解析器处理。</summary>
        public static bool CanParse(ElViewerDocumentKind kind, byte[] content)
        {
            if (!ElViewerGrowOoxml.IsZipPackage(content))
            {
                return false;
            }

            var entry = MainEntryOf(kind);

            if (entry == null)
            {
                return false;
            }

            try
            {
                using (var archive = ElViewerGrowOoxml.Open(content))
                {
                    return archive != null && archive.GetEntry(entry) != null;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 把 Office 文档渲染成 HTML 片段。无法处理时返回 <c>null</c>
        /// （由调用方回退到「不支持」或外链预览），解析中途失败也不会抛出去。
        /// </summary>
        public static string Render(ElViewerDocumentKind kind, byte[] content, string fileName)
        {
            if (!CanParse(kind, content))
            {
                return null;
            }

            try
            {
                using (var archive = ElViewerGrowOoxml.Open(content))
                {
                    if (archive == null)
                    {
                        return null;
                    }

                    switch (kind)
                    {
                        case ElViewerDocumentKind.Word:
                            return ElViewerGrowOfficeWord.Render(archive);
                        case ElViewerDocumentKind.Excel:
                            return ElViewerGrowOfficeExcel.Render(archive);
                        case ElViewerDocumentKind.PowerPoint:
                            return ElViewerGrowOfficePresentation.Render(archive);
                        default:
                            return null;
                    }
                }
            }
            catch
            {
                // 半损坏的文档不该把预览器拖崩：交给上层回退。
                return null;
            }
        }

        /// <summary>该类型是否是本类可以处理的 OOXML 类型。</summary>
        public static bool IsOfficeKind(ElViewerDocumentKind kind)
        {
            return kind == ElViewerDocumentKind.Word
                || kind == ElViewerDocumentKind.Excel
                || kind == ElViewerDocumentKind.PowerPoint;
        }

        /// <summary>该类型的入口部件路径；不是本类负责的类型时返回 <c>null</c>。</summary>
        private static string MainEntryOf(ElViewerDocumentKind kind)
        {
            switch (kind)
            {
                case ElViewerDocumentKind.Word:
                    return "word/document.xml";
                case ElViewerDocumentKind.Excel:
                    return "xl/workbook.xml";
                case ElViewerDocumentKind.PowerPoint:
                    return "ppt/presentation.xml";
                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// OOXML 的底层读取工具：ZIP 打开、XML 读取、关系（.rels）解析、部件路径规范化、图片转 data URI。
    /// <para>
    /// 单独抽出来是因为三套解析器（Word / Excel / PowerPoint）都要用同一套命名空间与路径规则，
    /// 复用一份实现可以保证三者的行为一致 —— 也便于单独对「路径规范化」这类纯函数做断言。
    /// </para>
    /// </summary>
    internal static class ElViewerGrowOoxml
    {
        /// <summary>WordprocessingML。</summary>
        public static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        /// <summary>SpreadsheetML。</summary>
        public static readonly XNamespace Xl = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        /// <summary>PresentationML。</summary>
        public static readonly XNamespace P = "http://schemas.openxmlformats.org/presentationml/2006/main";

        /// <summary>DrawingML。</summary>
        public static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";

        /// <summary>Office 文档内部的关系引用命名空间（<c>r:id</c> / <c>r:embed</c>）。</summary>
        public static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        /// <summary>OPC 包的关系部件命名空间（.rels 文件内部）。</summary>
        public static readonly XNamespace Rel = "http://schemas.openxmlformats.org/package/2006/relationships";

        /// <summary>VML（旧版 Office 的图片载体，<c>v:imagedata</c>）。</summary>
        public static readonly XNamespace V = "urn:schemas-microsoft-com:vml";

        /// <summary>
        /// 是不是一个 ZIP 包（PK\x03\x04）。OOXML 三者都是 ZIP，
        /// 旧版 <c>.doc</c>/<c>.xls</c>/<c>.ppt</c> 则是 <c>D0 CF 11 E0</c> 开头的 OLE 容器 —— 靠这个字节前缀区分。
        /// </summary>
        public static bool IsZipPackage(byte[] content)
        {
            return content != null
                && content.Length >= 4
                && content[0] == 0x50
                && content[1] == 0x4B
                && (content[2] == 0x03 || content[2] == 0x05 || content[2] == 0x07);
        }

        /// <summary>是不是旧版 OLE 复合文档（.doc / .xls / .ppt）。</summary>
        public static bool IsOleCompound(byte[] content)
        {
            return content != null
                && content.Length >= 8
                && content[0] == 0xD0
                && content[1] == 0xCF
                && content[2] == 0x11
                && content[3] == 0xE0;
        }

        /// <summary>打开 ZIP 归档；不是 ZIP 或打不开时返回 <c>null</c>。</summary>
        public static ZipArchive Open(byte[] content)
        {
            if (!IsZipPackage(content))
            {
                return null;
            }

            try
            {
                return new ZipArchive(new MemoryStream(content, false), ZipArchiveMode.Read, false);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>读取一个 XML 部件；不存在或不是合法 XML 时返回 <c>null</c>。</summary>
        public static XDocument ReadXml(ZipArchive archive, string path)
        {
            var entry = EntryOf(archive, path);

            if (entry == null)
            {
                return null;
            }

            try
            {
                using (var stream = entry.Open())
                {
                    return XDocument.Load(stream, LoadOptions.None);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>读取一个部件的原始字节；不存在时返回 <c>null</c>。</summary>
        public static byte[] ReadBytes(ZipArchive archive, string path)
        {
            var entry = EntryOf(archive, path);

            if (entry == null)
            {
                return null;
            }

            try
            {
                using (var stream = entry.Open())
                using (var buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);
                    return buffer.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 按路径取条目。ZIP 内的名字大小写敏感、且可能带前导 <c>/</c>，
        /// 这里统一做一次「去前导斜杠 + 忽略大小写 + 退化为按文件名匹配」的宽容查找。
        /// </summary>
        public static ZipArchiveEntry EntryOf(ZipArchive archive, string path)
        {
            if (archive == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            var normalized = Normalize(path.TrimStart('/'));

            var exact = archive.GetEntry(normalized);

            if (exact != null)
            {
                return exact;
            }

            foreach (var entry in archive.Entries)
            {
                if (string.Equals(Normalize(entry.FullName), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// 读取 <c>_rels</c> 关系表，返回 <c>rId → 目标部件路径</c>（已按 <paramref name="baseDir"/> 规范化为包内绝对路径）。
        /// </summary>
        /// <param name="baseDir">关系所在部件的目录，例如 <c>word/</c> 或 <c>ppt/slides/</c>。</param>
        public static Dictionary<string, string> ReadRelationships(ZipArchive archive, string relsPath, string baseDir)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            var document = ReadXml(archive, relsPath);

            if (document == null || document.Root == null)
            {
                return map;
            }

            foreach (var relationship in document.Root.Elements(Rel + "Relationship"))
            {
                var id = (string)relationship.Attribute("Id");
                var target = (string)relationship.Attribute("Target");

                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(target))
                {
                    continue;
                }

                // 外链关系（TargetMode="External"）不是包内部件，不能当路径用。
                var mode = (string)relationship.Attribute("TargetMode");

                if (!string.IsNullOrEmpty(mode) && mode.Equals("External", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                map[id] = ResolvePart(baseDir, target);
            }

            return map;
        }

        /// <summary>
        /// 读取 <c>_rels</c> 里的**外链**关系（<c>TargetMode="External"</c>），
        /// 返回 <c>rId → 原始 Target</c>。超链接就靠这个还原：它是 URL，不是包内部件路径，
        /// 不能和 <see cref="ReadRelationships"/> 混在一起。
        /// </summary>
        public static Dictionary<string, string> ReadExternalRelationships(ZipArchive archive, string relsPath)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            var document = ReadXml(archive, relsPath);

            if (document == null || document.Root == null)
            {
                return map;
            }

            foreach (var relationship in document.Root.Elements(Rel + "Relationship"))
            {
                var mode = (string)relationship.Attribute("TargetMode");

                if (string.IsNullOrEmpty(mode) || !mode.Equals("External", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var id = (string)relationship.Attribute("Id");
                var target = (string)relationship.Attribute("Target");

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(target))
                {
                    map[id] = target;
                }
            }

            return map;
        }

        /// <summary>
        /// 按关系的 <c>Type</c> 后缀定位一个部件（例如 <c>"slideLayout"</c> / <c>"slideMaster"</c>），
        /// 返回包内绝对路径；找不到时返回 <c>null</c>。
        /// <para>
        /// 幻灯片 → 版式 → 母版这条链只能靠关系类型走，因为部件名没有规律
        /// （<c>slideLayout1.xml</c> 可能是任意一个版式），猜文件名会张冠李戴。
        /// </para>
        /// </summary>
        public static string FindRelationshipByType(ZipArchive archive, string relsPath, string typeSuffix)
        {
            var document = ReadXml(archive, relsPath);

            if (document?.Root == null)
            {
                return null;
            }

            var baseDir = DirectoryOf(relsPath, true);

            foreach (var relationship in document.Root.Elements(Rel + "Relationship"))
            {
                var type = (string)relationship.Attribute("Type");
                var target = (string)relationship.Attribute("Target");

                if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(target))
                {
                    continue;
                }

                // Type 是 .../relationships/slideLayout 这种形式，用结尾匹配即可。
                if (!type.EndsWith("/" + typeSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return ResolvePart(baseDir, target);
            }

            return null;
        }

        /// <summary>
        /// 由 <c>_rels</c> 路径反推它所属部件的目录。
        /// <c>ppt/slides/_rels/slide1.xml.rels</c> → <c>ppt/slides/</c>。
        /// </summary>
        private static string DirectoryOf(string relsPath, bool fromRels)
        {
            if (!fromRels || string.IsNullOrEmpty(relsPath))
            {
                return string.Empty;
            }

            var marker = relsPath.LastIndexOf("_rels/", StringComparison.OrdinalIgnoreCase);

            return marker <= 0 ? string.Empty : relsPath.Substring(0, marker);
        }

        /// <summary>
        /// 相对部件路径规范化为包内绝对路径：处理前导 <c>/</c>、<c>.</c> 与 <c>..</c>。
        /// <para>刻意不用 <see cref="Path"/> —— WebAssembly 下的路径分隔符与真实文件系统无关，自己算更可控也更可测。</para>
        /// </summary>
        public static string ResolvePart(string baseDir, string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                return string.Empty;
            }

            var raw = target.Replace('\\', '/');
            var segments = new List<string>();

            if (raw.StartsWith("/", StringComparison.Ordinal))
            {
                // 以 / 开头 = 从包根算起，忽略 baseDir。
                raw = raw.TrimStart('/');
            }
            else if (!string.IsNullOrEmpty(baseDir))
            {
                foreach (var segment in baseDir.Replace('\\', '/').Split('/'))
                {
                    if (segment.Length > 0)
                    {
                        segments.Add(segment);
                    }
                }
            }

            foreach (var segment in raw.Split('/'))
            {
                if (segment.Length == 0 || segment == ".")
                {
                    continue;
                }

                if (segment == "..")
                {
                    if (segments.Count > 0)
                    {
                        segments.RemoveAt(segments.Count - 1);
                    }

                    continue;
                }

                segments.Add(segment);
            }

            return string.Join("/", segments);
        }

        /// <summary>把一个媒体部件转成内联 data URI；读不到或类型不支持时返回 <c>null</c>。</summary>
        public static string BuildDataUri(ZipArchive archive, string partPath)
        {
            var media = ContentTypeOf(partPath);

            if (media == null)
            {
                return null;
            }

            var bytes = ReadBytes(archive, partPath);

            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            return "data:" + media + ";base64," + Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// 按扩展名给出浏览器能直接显示的内联 MIME。
        /// EMF/WMF/TIFF 这类 Word/PPT 里常见的矢量格式浏览器无法显示，返回 <c>null</c> 让调用方跳过。
        /// </summary>
        public static string ContentTypeOf(string path)
        {
            var clean = path ?? string.Empty;
            var dot = clean.LastIndexOf('.');

            if (dot < 0)
            {
                return null;
            }

            switch (clean.Substring(dot).ToLowerInvariant())
            {
                case ".png":
                    return "image/png";
                case ".jpg":
                case ".jpeg":
                case ".jpe":
                    return "image/jpeg";
                case ".gif":
                    return "image/gif";
                case ".bmp":
                    return "image/bmp";
                case ".webp":
                    return "image/webp";
                case ".svg":
                    return "image/svg+xml";
                case ".ico":
                    return "image/x-icon";
                case ".avif":
                    return "image/avif";
                default:
                    return null;
            }
        }

        /// <summary>取元素下某个直接子元素的文本（<c>w:t</c> / <c>a:t</c> 这类）。</summary>
        public static string TextOf(XElement element)
        {
            if (element == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            foreach (var text in element.Descendants())
            {
                if (text.Name.LocalName == "t")
                {
                    builder.Append(text.Value);
                }
            }

            return builder.ToString();
        }

        /// <summary>读取一个布尔型开关（<c>w:b</c> / <c>w:i</c> 等）：自带 <c>val="0"</c> / <c>"false"</c> 表示关闭。</summary>
        public static bool Toggle(XElement parent, XNamespace ns, string name)
        {
            if (parent == null)
            {
                return false;
            }

            var child = parent.Element(ns + name);

            if (child == null)
            {
                return false;
            }

            var value = (string)child.Attribute(ns + "val")
                ?? (string)child.Attribute("val");

            if (string.IsNullOrEmpty(value))
            {
                // 只有元素、没有值 = 打开。
                return true;
            }

            return !(value == "0"
                || value.Equals("false", StringComparison.OrdinalIgnoreCase)
                || value.Equals("off", StringComparison.OrdinalIgnoreCase));
        }

        private static string Normalize(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/');
        }
    }
}
