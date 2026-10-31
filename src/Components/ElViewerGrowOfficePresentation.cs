using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ElementPlus
{
    /// <summary>
    /// pptx（PresentationML）→ HTML。
    /// <para>
    /// 与 Word / Excel 的「流式长文」不同，幻灯片是一张固定尺寸的画布 + 一堆绝对定位的形状，
    /// 因此这里按 <c>p:sldSz</c> 的画布尺寸把每个形状的 EMU 坐标换算成百分比定位，
    /// 字号则换算成容器宽度单位（<c>cqw</c>）—— 这样无论预览容器多宽，版式比例都与原稿一致。
    /// </para>
    /// <para>
    /// 覆盖：按 <c>p:sldIdLst</c> 的真实顺序逐页渲染、文本框（含层级缩进 / 对齐 / 字号 / 粗斜体 /
    /// 颜色）、图片、表格、组合形状（按 <c>chOff</c>/<c>chExt</c> 做坐标映射）、页面尺寸。
    /// 刻意不做：动画与切换、母版主题配色的完整还原（<c>a:schemeClr</c> 需要读 theme1.xml 的色板，
    /// 且用户常自定义主题，硬猜会串色，因此只用显式 RGB 值）。
    /// </para>
    /// </summary>
    internal static class ElViewerGrowOfficePresentation
    {
        private const string PresentationPart = "ppt/presentation.xml";

        private const string PresentationRelsPart = "ppt/_rels/presentation.xml.rels";

        /// <summary>EMU → 点：1 英寸 = 914400 EMU = 72 磅。</summary>
        private const double EmuPerPoint = 914400.0 / 72.0;

        private sealed class Context
        {
            public ZipArchive Archive;

            public int SlideWidth;

            public int SlideHeight;

            /// <summary>当前幻灯片的 rId → 部件路径。</summary>
            public Dictionary<string, string> Parts;

            /// <summary>占位符 → 从 slideLayout 继承来的位置（幻灯片坐标，EMU）。</summary>
            public Dictionary<PhKey, Frame> LayoutBoxes = new Dictionary<PhKey, Frame>();

            /// <summary>占位符 → 从 layout 的 <c>a:lstStyle</c> 继承来的「段落层级 → 字号（磅）」。</summary>
            public Dictionary<PhKey, Dictionary<int, double>> LayoutSizes =
                new Dictionary<PhKey, Dictionary<int, double>>();

            /// <summary>母版 <c>p:txStyles</c> 的「类别 → 段落层级 → 字号（磅）」。</summary>
            public Dictionary<string, Dictionary<int, double>> MasterSizes =
                new Dictionary<string, Dictionary<int, double>>();
        }

        /// <summary>
        /// 占位符标识。ECMA-376 里两个 <c>p:ph</c> 算同一个占位符的前提是
        /// <c>type</c> 与 <c>idx</c> 都相同（两者缺省时分别是 <c>obj</c> / <c>0</c>），
        /// 并且 <c>title</c> 与 <c>ctrTitle</c> 视为同一类 —— 幻灯片用前者、版式用后者的组合很常见。
        /// </summary>
        private struct PhKey : IEquatable<PhKey>
        {
            public string Type;

            public int Index;

            public bool Equals(PhKey other)
            {
                return string.Equals(Type, other.Type, StringComparison.Ordinal) && Index == other.Index;
            }

            public override bool Equals(object obj)
            {
                return obj is PhKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return ((Type ?? string.Empty).GetHashCode() * 397) ^ Index;
            }
        }

        public static string Render(ZipArchive archive)
        {
            var presentation = ElViewerGrowOoxml.ReadXml(archive, PresentationPart);

            if (presentation?.Root == null)
            {
                return null;
            }

            var p = ElViewerGrowOoxml.P;

            var slideList = presentation.Root.Element(p + "sldIdLst");

            if (slideList == null)
            {
                return null;
            }

            var relationships = ElViewerGrowOoxml.ReadRelationships(archive, PresentationRelsPart, "ppt/");

            var size = presentation.Root.Element(p + "sldSz");

            var width = ReadInt((string)size?.Attribute("cx"), 12192000);
            var height = ReadInt((string)size?.Attribute("cy"), 6858000);

            if (width <= 0 || height <= 0)
            {
                width = 12192000;
                height = 6858000;
            }

            var context = new Context
            {
                Archive = archive,
                SlideWidth = width,
                SlideHeight = height
            };

            var builder = new StringBuilder();
            builder.Append("<div class=\"el-viewer-grow__doc-pptx\">");

            var number = 0;

            foreach (var slide in slideList.Elements(p + "sldId"))
            {
                var id = (string)slide.Attribute(ElViewerGrowOoxml.R + "id");

                if (string.IsNullOrEmpty(id) || !relationships.TryGetValue(id, out var path))
                {
                    continue;
                }

                number++;

                var markup = RenderSlide(archive, path, context, number, width, height);

                if (markup != null)
                {
                    builder.Append(markup);
                }
            }

            builder.Append("</div>");

            return number > 0 ? builder.ToString() : null;
        }

        private static string RenderSlide(
            ZipArchive archive, string path, Context context, int number, int width, int height)
        {
            var document = ElViewerGrowOoxml.ReadXml(archive, path);
            var p = ElViewerGrowOoxml.P;

            var tree = document?.Root?.Element(p + "cSld")?.Element(p + "spTree");

            if (tree == null)
            {
                return null;
            }

            // 幻灯片自己的关系表：图片、外部链接都在这里（路径形如 ppt/slides/_rels/slide1.xml.rels）。
            var relsPath = RelsPathOf(path);
            context.Parts = ElViewerGrowOoxml.ReadRelationships(archive, relsPath, DirectoryOf(path));

            CollectInheritance(archive, relsPath, context);

            var ratio = height * 100.0 / width;
            var builder = new StringBuilder();

            builder.Append("<section class=\"el-viewer-grow__doc-slide\">");
            builder.Append("<div class=\"el-viewer-grow__doc-slide-canvas\" style=\"padding-top:")
                .Append(ratio.ToString("0.####", CultureInfo.InvariantCulture))
                .Append("%\">");
            builder.Append("<div class=\"el-viewer-grow__doc-slide-body\">");

            var frame = new Frame { X = 0, Y = 0, Width = width, Height = height };

            foreach (var shape in tree.Elements())
            {
                AppendShape(builder, shape, context, frame, null);
            }

            builder.Append("</div></div>");
            builder.Append("<div class=\"el-viewer-grow__doc-slide-number\">第 ")
                .Append(number.ToString(CultureInfo.InvariantCulture))
                .Append(" 页</div>");
            builder.Append("</section>");

            return builder.ToString();
        }

        #region 占位符继承（幻灯片 → 版式 → 母版）

        /// <summary>
        /// 收集「幻灯片 → slideLayout → slideMaster」这条继承链上可用的缺省值。
        /// <para>
        /// 真实 PPT 里幻灯片上的占位符往往只写内容、不写位置与字号 —— 位置来自版式，
        /// 字号来自母版的 <c>p:txStyles</c>。不读这条链的直接后果是：标题与正文全部堆在左上角、
        /// 且都是同一个字号，看起来「像是解析坏了」，其实只是少读了两级。
        /// </para>
        /// </summary>
        private static void CollectInheritance(ZipArchive archive, string relsPath, Context context)
        {
            context.LayoutBoxes.Clear();
            context.LayoutSizes.Clear();
            context.MasterSizes.Clear();

            var layoutPath = ElViewerGrowOoxml.FindRelationshipByType(archive, relsPath, "slideLayout");

            if (layoutPath == null)
            {
                return;
            }

            var layout = ElViewerGrowOoxml.ReadXml(archive, layoutPath);
            CollectPlaceholders(layout, context.LayoutBoxes, context.LayoutSizes);

            var masterPath = ElViewerGrowOoxml.FindRelationshipByType(
                archive, RelsPathOf(layoutPath), "slideMaster");

            if (masterPath == null)
            {
                return;
            }

            var master = ElViewerGrowOoxml.ReadXml(archive, masterPath);

            CollectTextStyles(master, context.MasterSizes);

            // 母版自己的占位符也算一层（版式把定义继续留给母版的情况很常见），
            // 但优先级低于版式，所以只在版式没给时补位。
            var masterBoxes = new Dictionary<PhKey, Frame>();
            var masterSizes = new Dictionary<PhKey, Dictionary<int, double>>();

            CollectPlaceholders(master, masterBoxes, masterSizes);

            foreach (var pair in masterBoxes)
            {
                if (!context.LayoutBoxes.ContainsKey(pair.Key))
                {
                    context.LayoutBoxes[pair.Key] = pair.Value;
                }
            }

            foreach (var pair in masterSizes)
            {
                if (!context.LayoutSizes.ContainsKey(pair.Key))
                {
                    context.LayoutSizes[pair.Key] = pair.Value;
                }
            }
        }

        /// <summary>取出一个版式 / 母版里所有占位符形状的位置与 <c>a:lstStyle</c> 字号。</summary>
        private static void CollectPlaceholders(
            XDocument document,
            Dictionary<PhKey, Frame> boxes,
            Dictionary<PhKey, Dictionary<int, double>> sizes)
        {
            var p = ElViewerGrowOoxml.P;
            var a = ElViewerGrowOoxml.A;

            var tree = document?.Root?.Element(p + "cSld")?.Element(p + "spTree");

            if (tree == null)
            {
                return;
            }

            foreach (var shape in tree.Elements(p + "sp"))
            {
                var key = ReadPlaceholderKey(shape);

                if (!key.HasValue)
                {
                    continue;
                }

                var transform = shape.Element(p + "spPr")?.Element(a + "xfrm");

                if (transform != null)
                {
                    var offset = transform.Element(a + "off");
                    var extent = transform.Element(a + "ext");

                    var box = new Frame
                    {
                        X = ReadInt((string)offset?.Attribute("x"), 0),
                        Y = ReadInt((string)offset?.Attribute("y"), 0),
                        Width = ReadInt((string)extent?.Attribute("cx"), 0),
                        Height = ReadInt((string)extent?.Attribute("cy"), 0)
                    };

                    if (box.Width > 0 && box.Height > 0)
                    {
                        boxes[key.Value] = box;
                    }
                }

                var styled = ReadListStyleSizes(shape.Element(p + "txBody"));

                if (styled.Count > 0)
                {
                    sizes[key.Value] = styled;
                }
            }
        }

        /// <summary>读 <c>a:txBody/a:lstStyle</c> 里逐层的缺省字号。</summary>
        private static Dictionary<int, double> ReadListStyleSizes(XElement textBody)
        {
            var map = new Dictionary<int, double>();
            var a = ElViewerGrowOoxml.A;

            var listStyle = textBody?.Element(a + "lstStyle");

            if (listStyle == null)
            {
                return map;
            }

            for (var level = 1; level <= 9; level++)
            {
                var size = ReadFontSize(listStyle.Element(a + ("lvl" + level + "pPr"))?.Element(a + "defRPr"));

                if (size.HasValue)
                {
                    map[level - 1] = size.Value;
                }
            }

            return map;
        }

        /// <summary>读母版的 <c>p:txStyles</c>：标题 / 正文 / 其他三套，各自九层。</summary>
        private static void CollectTextStyles(XDocument master, Dictionary<string, Dictionary<int, double>> target)
        {
            var p = ElViewerGrowOoxml.P;
            var a = ElViewerGrowOoxml.A;

            var styles = master?.Root?.Element(p + "txStyles");

            if (styles == null)
            {
                return;
            }

            var categories = new[]
            {
                new KeyValuePair<string, XElement>("title", styles.Element(p + "titleStyle")),
                new KeyValuePair<string, XElement>("body", styles.Element(p + "bodyStyle")),
                new KeyValuePair<string, XElement>("other", styles.Element(p + "otherStyle"))
            };

            foreach (var category in categories)
            {
                if (category.Value == null)
                {
                    continue;
                }

                var map = new Dictionary<int, double>();

                for (var level = 1; level <= 9; level++)
                {
                    var size = ReadFontSize(category.Value.Element(a + ("lvl" + level + "pPr"))?.Element(a + "defRPr"));

                    if (size.HasValue)
                    {
                        map[level - 1] = size.Value;
                    }
                }

                if (map.Count > 0)
                {
                    target[category.Key] = map;
                }
            }
        }

        /// <summary>读 <c>a:defRPr/@sz</c>（单位 1/100 磅）并换算成磅。</summary>
        private static double? ReadFontSize(XElement properties)
        {
            if (properties == null)
            {
                return null;
            }

            if (int.TryParse((string)properties.Attribute("sz"), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var size) && size > 0)
            {
                return size / 100.0;
            }

            return null;
        }

        /// <summary>读形状的占位符标识；不是占位符时返回 <c>null</c>。</summary>
        private static PhKey? ReadPlaceholderKey(XElement shape)
        {
            var p = ElViewerGrowOoxml.P;

            foreach (var child in shape.Elements())
            {
                // p:nvSpPr / p:nvPicPr / p:nvGraphicFramePr：名字里的形状种类不定，直接看有没有 nvPr。
                var placeholder = child.Element(p + "nvPr")?.Element(p + "ph");

                if (placeholder == null)
                {
                    continue;
                }

                var type = (string)placeholder.Attribute("type") ?? "obj";

                // title 与 ctrTitle 是同一个占位符的两种叫法。
                if (type == "ctrTitle")
                {
                    type = "title";
                }
                else if (type == "obj")
                {
                    // 同理：幻灯片上写 <p:ph idx="1"/>（type 缺省 = obj）而版式 / 母版写
                    // type="body" 是极常见的组合，它们指的是同一个占位符，不归一化就永远匹配不上，
                    // 正文的位置与字号也就继承不到（表现为全部挤在左上角）。
                    type = "body";
                }

                return new PhKey
                {
                    Type = type,
                    Index = ReadInt((string)placeholder.Attribute("idx"), 0)
                };
            }

            return null;
        }

        /// <summary>按「版式 → 母版 txStyles」的顺序取某个段落层级的缺省字号（磅）。</summary>
        private static double? ResolveDefaultFontSize(Context context, PhKey? key, int level)
        {
            if (key.HasValue
                && context.LayoutSizes.TryGetValue(key.Value, out var layoutSizes)
                && layoutSizes.TryGetValue(level, out var layoutSize))
            {
                return layoutSize;
            }

            var category = StyleCategoryOf(key);

            if (context.MasterSizes.TryGetValue(category, out var masterSizes)
                && masterSizes.TryGetValue(level, out var masterSize))
            {
                return masterSize;
            }

            return null;
        }

        /// <summary>占位符归到母版三套样式中的哪一套。</summary>
        private static string StyleCategoryOf(PhKey? key)
        {
            if (!key.HasValue)
            {
                return "other";
            }

            switch (key.Value.Type)
            {
                case "title":
                    return "title";
                case "body":
                case "subTitle":
                case "obj":
                    return "body";
                default:
                    return "other";
            }
        }

        #endregion

        #region 形状

        /// <summary>形状在幻灯片里的绝对框（EMU）。</summary>
        private struct Frame
        {
            public double X;

            public double Y;

            public double Width;

            public double Height;
        }

        /// <summary>组合形状的坐标映射参数（子坐标系 → 父坐标系）。</summary>
        private sealed class GroupTransform
        {
            public double OffsetX;

            public double OffsetY;

            public double ExtentWidth;

            public double ExtentHeight;

            public double ChildOffsetX;

            public double ChildOffsetY;

            public double ChildExtentWidth;

            public double ChildExtentHeight;
        }

        private static void AppendShape(
            StringBuilder builder, XElement shape, Context context, Frame frame, GroupTransform group)
        {
            var p = ElViewerGrowOoxml.P;

            if (shape.Name == p + "sp")
            {
                AppendTextBox(builder, shape, context, frame, group);
            }
            else if (shape.Name == p + "pic")
            {
                AppendPicture(builder, shape, context, frame, group);
            }
            else if (shape.Name == p + "graphicFrame")
            {
                AppendTable(builder, shape, context, frame, group);
            }
            else if (shape.Name == p + "grpSp")
            {
                AppendGroup(builder, shape, context, frame, group);
            }
        }

        private static void AppendGroup(
            StringBuilder builder, XElement group, Context context, Frame frame, GroupTransform parent)
        {
            var a = ElViewerGrowOoxml.A;

            var transform = group.Element(ElViewerGrowOoxml.P + "grpSpPr")?.Element(a + "xfrm");

            var box = ResolveBox(transform, frame, parent);

            // 组自身的框可能没写；没写时子形状直接用父坐标系。
            var effective = transform == null
                ? parent
                : BuildGroupTransform(transform, box, frame, parent);

            foreach (var child in group.Elements())
            {
                if (child.Name == ElViewerGrowOoxml.P + "sp"
                    || child.Name == ElViewerGrowOoxml.P + "pic"
                    || child.Name == ElViewerGrowOoxml.P + "graphicFrame"
                    || child.Name == ElViewerGrowOoxml.P + "grpSp")
                {
                    AppendShape(builder, child, context, frame, effective);
                }
            }
        }

        /// <summary>
        /// 由组的 <c>a:xfrm</c> 构造坐标映射：
        /// 子坐标先减去 <c>chOff</c>，再按 <c>ext/chExt</c> 缩放，最后加上组的 <c>off</c>。
        /// </summary>
        private static GroupTransform BuildGroupTransform(
            XElement transform, Frame box, Frame frame, GroupTransform parent)
        {
            var a = ElViewerGrowOoxml.A;
            var childOffset = transform.Element(a + "chOff");
            var childExtent = transform.Element(a + "chExt");

            return new GroupTransform
            {
                OffsetX = box.X,
                OffsetY = box.Y,
                ExtentWidth = box.Width,
                ExtentHeight = box.Height,
                ChildOffsetX = ReadInt((string)childOffset?.Attribute("x"), 0),
                ChildOffsetY = ReadInt((string)childOffset?.Attribute("y"), 0),
                ChildExtentWidth = Math.Max(1, ReadInt((string)childExtent?.Attribute("cx"), (int)box.Width)),
                ChildExtentHeight = Math.Max(1, ReadInt((string)childExtent?.Attribute("cy"), (int)box.Height))
            };
        }

        /// <summary>
        /// 读取 <c>a:xfrm</c> 的位置与尺寸，换算到幻灯片绝对坐标。
        /// 组合内部的子形状要经过 <paramref name="group"/> 的映射。
        /// </summary>
        private static Frame ResolveBox(XElement transform, Frame fallback, GroupTransform group)
        {
            var a = ElViewerGrowOoxml.A;

            var offset = transform?.Element(a + "off");
            var extent = transform?.Element(a + "ext");

            var x = (double)ReadInt((string)offset?.Attribute("x"), 0);
            var y = (double)ReadInt((string)offset?.Attribute("y"), 0);
            var width = (double)ReadInt((string)extent?.Attribute("cx"), 0);
            var height = (double)ReadInt((string)extent?.Attribute("cy"), 0);

            if (group == null)
            {
                return new Frame { X = x, Y = y, Width = width, Height = height };
            }

            var mapped = new Frame
            {
                X = group.OffsetX + (x - group.ChildOffsetX) / group.ChildExtentWidth * group.ExtentWidth,
                Y = group.OffsetY + (y - group.ChildOffsetY) / group.ChildExtentHeight * group.ExtentHeight,
                Width = width / group.ChildExtentWidth * group.ExtentWidth,
                Height = height / group.ChildExtentHeight * group.ExtentHeight
            };

            return mapped;
        }

        private static void AppendTextBox(
            StringBuilder builder, XElement shape, Context context, Frame frame, GroupTransform group)
        {
            var p = ElViewerGrowOoxml.P;
            var a = ElViewerGrowOoxml.A;

            var properties = shape.Element(p + "spPr");
            var transform = properties?.Element(a + "xfrm");
            var key = ReadPlaceholderKey(shape);

            // 位置来源：形状自己的 a:xfrm → 版式里同名占位符的 a:xfrm → 流式兜底。
            var positioned = false;
            var box = default(Frame);

            if (transform != null)
            {
                box = ResolveBox(transform, frame, group);
                positioned = box.Width > 0 && box.Height > 0;
            }
            else if (group == null
                && key.HasValue
                && context.LayoutBoxes.TryGetValue(key.Value, out var inheritedBox))
            {
                box = inheritedBox;
                positioned = true;
            }

            var body = shape.Element(p + "txBody");

            if (body == null)
            {
                return;
            }

            var content = RenderTextBody(body, context, key);

            if (content.Length == 0)
            {
                return;
            }

            var titleLike = key.HasValue && key.Value.Type == "title";
            var filled = properties?.Element(a + "solidFill") != null;

            var css = new StringBuilder();

            if (positioned)
            {
                AppendFrameStyle(css, box, context);
            }
            else
            {
                // 版式里也没有位置（自由文本框极少见）：宁可让它流式排在页面上，
                // 也不能因为「不知道放哪儿」就把文字丢掉。
                css.Append("position:relative;width:100%;");
            }

            // 垂直居中：PPT 的正文占位符默认就是居中的。
            var anchor = (string)body.Element(a + "bodyPr")?.Attribute("anchor");

            if (anchor == "ctr" || (anchor == null && key.HasValue && !titleLike))
            {
                css.Append("display:flex;flex-direction:column;justify-content:center;");
            }
            else if (anchor == "b")
            {
                css.Append("display:flex;flex-direction:column;justify-content:flex-end;");
            }

            if (titleLike)
            {
                css.Append("font-weight:600;");
            }

            builder.Append("<div class=\"el-viewer-grow__doc-shape");

            if (!positioned)
            {
                builder.Append(" is-static");
            }

            if (filled)
            {
                builder.Append(" is-filled");
            }

            if (titleLike)
            {
                builder.Append(" is-title");
            }

            builder.Append("\" style=\"").Append(css).Append("\">");
            builder.Append(content);
            builder.Append("</div>");
        }

        private static void AppendPicture(
            StringBuilder builder, XElement picture, Context context, Frame frame, GroupTransform group)
        {
            var p = ElViewerGrowOoxml.P;
            var a = ElViewerGrowOoxml.A;

            var properties = picture.Element(p + "spPr");
            var transform = properties?.Element(a + "xfrm");
            var key = ReadPlaceholderKey(picture);

            var positioned = false;
            var box = default(Frame);

            if (transform != null)
            {
                box = ResolveBox(transform, frame, group);
                positioned = box.Width > 0 && box.Height > 0;
            }
            else if (group == null
                && key.HasValue
                && context.LayoutBoxes.TryGetValue(key.Value, out var inheritedBox)
                && inheritedBox.Width > 0)
            {
                box = inheritedBox;
                positioned = true;
            }

            var blip = picture.Element(p + "blipFill")?.Element(a + "blip");
            var id = (string)blip?.Attribute(ElViewerGrowOoxml.R + "embed");

            if (string.IsNullOrEmpty(id) || context.Parts == null || !context.Parts.TryGetValue(id, out var part))
            {
                return;
            }

            var uri = ElViewerGrowOoxml.BuildDataUri(context.Archive, part);

            if (uri == null)
            {
                // EMF/WMF 这类浏览器显示不了的矢量图：跳过，不留占位（否则会出现一堆空框）。
                return;
            }

            if (!positioned)
            {
                builder.Append("<img class=\"el-viewer-grow__doc-image\" src=\"").Append(uri).Append("\" alt=\"\" />");
                return;
            }

            var css = new StringBuilder();
            AppendFrameStyle(css, box, context);

            builder.Append("<img class=\"el-viewer-grow__doc-shape el-viewer-grow__doc-shape-image\" style=\"")
                .Append(css)
                .Append("\" src=\"")
                .Append(uri)
                .Append("\" alt=\"\" />");
        }

        private static void AppendTable(
            StringBuilder builder, XElement frameShape, Context context, Frame frame, GroupTransform group)
        {
            var a = ElViewerGrowOoxml.A;
            var p = ElViewerGrowOoxml.P;

            var table = frameShape.Descendants(a + "tbl").FirstOrDefault();

            if (table == null)
            {
                // 图表 / SmartArt 之类：这里的 graphicData 里是另一套部件，本实现不解析。
                return;
            }

            var transform = frameShape.Element(p + "xfrm");
            var key = ReadPlaceholderKey(frameShape);

            var positioned = false;
            var box = default(Frame);

            if (transform != null)
            {
                box = ResolveBox(transform, frame, group);
                positioned = box.Width > 0 && box.Height > 0;
            }
            else if (group == null
                && key.HasValue
                && context.LayoutBoxes.TryGetValue(key.Value, out var inheritedBox)
                && inheritedBox.Width > 0)
            {
                box = inheritedBox;
                positioned = true;
            }

            var css = new StringBuilder();

            if (positioned)
            {
                AppendFrameStyle(css, box, context);
            }
            else
            {
                css.Append("position:relative;width:100%;");
            }

            css.Append("overflow:hidden;");

            builder.Append("<div class=\"el-viewer-grow__doc-shape el-viewer-grow__doc-shape-table\" style=\"")
                .Append(css)
                .Append("\"><table class=\"el-viewer-grow__doc-table\">");

            foreach (var row in table.Elements(a + "tr"))
            {
                builder.Append("<tr class=\"el-viewer-grow__doc-tr\">");

                foreach (var cell in row.Elements(a + "tc"))
                {
                    var body = cell.Element(a + "txBody");
                    var content = body == null ? string.Empty : RenderTextBody(body, context, null);

                    builder.Append("<td class=\"el-viewer-grow__doc-td\">").Append(content).Append("</td>");
                }

                builder.Append("</tr>");
            }

            builder.Append("</table></div>");
        }

        /// <summary>
        /// 把 EMU 框换算成百分比定位的 CSS。
        /// <para>
        /// left/top 必须**无条件**写出来：绝对定位元素若缺这两个属性会退回「静态位置」，
        /// 那个位置取决于前一个兄弟节点的布局结果，等于把版式交给运气。
        /// </para>
        /// </summary>
        private static void AppendFrameStyle(StringBuilder css, Frame box, Context context)
        {
            css.Append("position:absolute;")
                .Append("left:").Append(Pct(box.X, context.SlideWidth)).Append("%;")
                .Append("top:").Append(Pct(box.Y, context.SlideHeight)).Append("%;")
                .Append("width:").Append(Pct(box.Width, context.SlideWidth)).Append("%;")
                .Append("height:").Append(Pct(box.Height, context.SlideHeight)).Append("%;");
        }

        private static string Pct(double value, double total)
        {
            if (total <= 0)
            {
                return "0";
            }

            return (value / total * 100.0).ToString("0.###", CultureInfo.InvariantCulture);
        }

        #endregion

        #region 文本

        /// <summary>渲染 <c>p:txBody</c> / <c>a:txBody</c>：段落 + run 样式 + 层级缩进。</summary>
        private static string RenderTextBody(XElement body, Context context, PhKey? key)
        {
            var a = ElViewerGrowOoxml.A;
            var builder = new StringBuilder();

            foreach (var paragraph in body.Elements(a + "p"))
            {
                var markup = RenderParagraph(paragraph, context, key);

                if (markup.Length == 0)
                {
                    continue;
                }

                builder.Append(markup);
            }

            return builder.ToString();
        }

        private static string RenderParagraph(XElement paragraph, Context context, PhKey? key)
        {
            var a = ElViewerGrowOoxml.A;
            var properties = paragraph.Element(a + "pPr");

            var builder = new StringBuilder();
            var css = new StringBuilder();

            var alignment = (string)properties?.Attribute("algn");

            switch (alignment)
            {
                case "ctr":
                    css.Append("text-align:center;");
                    break;
                case "r":
                    css.Append("text-align:right;");
                    break;
                case "just":
                    css.Append("text-align:justify;");
                    break;
            }

            var level = 0;

            if (int.TryParse((string)properties?.Attribute("lvl"), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var parsedLevel) && parsedLevel > 0)
            {
                level = parsedLevel;
                css.Append("margin-left:").Append((level * 1.4).ToString("0.##", CultureInfo.InvariantCulture)).Append("em;");
            }

            foreach (var child in paragraph.Elements())
            {
                if (child.Name == a + "r" || child.Name == a + "fld")
                {
                    AppendTextRun(builder, child, context, key, level);
                }
                else if (child.Name == a + "br")
                {
                    builder.Append("<br/>");
                }
                else if (child.Name == a + "tab")
                {
                    builder.Append("<span class=\"el-viewer-grow__doc-tab\"></span>");
                }
            }

            if (builder.Length == 0)
            {
                return string.Empty;
            }

            var result = builder.Insert(0, "<p class=\"el-viewer-grow__doc-p\"" +
                (css.Length > 0 ? " style=\"" + css + "\"" : string.Empty) + ">");

            result.Append("</p>");

            return result.ToString();
        }

        private static void AppendTextRun(
            StringBuilder builder, XElement run, Context context, PhKey? key, int level)
        {
            var a = ElViewerGrowOoxml.A;
            var properties = run.Element(a + "rPr");

            var text = new StringBuilder();

            foreach (var child in run.Elements())
            {
                if (child.Name == a + "t")
                {
                    text.Append(ElViewerGrowRenderer.EscapeHtml(child.Value));
                }
                else if (child.Name == a + "br")
                {
                    text.Append("<br/>");
                }
            }

            if (text.Length == 0)
            {
                return;
            }

            var css = new StringBuilder();

            if (properties != null)
            {
                // 注意：DrawingML 的 a:rPr 用「属性」表达粗体斜体（<a:rPr b="1"/>），
                // 与 WordprocessingML 的「子元素」（<w:rPr><w:b/></w:rPr>）不同，别混用同一个判定。
                if (ToggleAttribute(properties, "b"))
                {
                    css.Append("font-weight:700;");
                }

                if (ToggleAttribute(properties, "i"))
                {
                    css.Append("font-style:italic;");
                }

                var underline = (string)properties.Attribute("u");

                if (!string.IsNullOrEmpty(underline) && underline != "none")
                {
                    css.Append("text-decoration:underline;");
                }

                if (ToggleAttribute(properties, "strike"))
                {
                    css.Append("text-decoration:line-through;");
                }

                var color = (string)properties.Element(a + "solidFill")?.Element(a + "srgbClr")?.Attribute("val");

                if (!string.IsNullOrEmpty(color) && color.Length == 6)
                {
                    css.Append("color:#").Append(color).Append(';');
                }
            }

            // 字号：run 自己的 a:sz → 版式 lstStyle → 母版 txStyles。
            // 真实 PPT 里绝大多数文字都靠后面两级，不继承就全挤成默认大小。
            var size = ReadFontSize(properties) ?? ResolveDefaultFontSize(context, key, level);

            if (size.HasValue)
            {
                AppendFontSize(css, size.Value, context);
            }

            if (css.Length == 0)
            {
                builder.Append(text);
                return;
            }

            builder.Append("<span style=\"").Append(css).Append("\">").Append(text).Append("</span>");
        }

        /// <summary>
        /// 字号换算成「占幻灯片宽度的比例」用 <c>cqw</c> 表达，这样字号随预览容器等比缩放；
        /// 同时给一个按 1280px 画布算的 <c>px</c> 兜底，给不支持容器查询单位的浏览器。
        /// </summary>
        private static void AppendFontSize(StringBuilder css, double points, Context context)
        {
            var slideWidthPoints = context.SlideWidth / EmuPerPoint;

            if (slideWidthPoints <= 0)
            {
                return;
            }

            var pixels = points * 96.0 / 72.0 * (1280.0 / (slideWidthPoints * 96.0 / 72.0));

            css.Append("font-size:")
                .Append(pixels.ToString("0.##", CultureInfo.InvariantCulture))
                .Append("px;")
                .Append("font-size:")
                .Append((points / slideWidthPoints * 100.0).ToString("0.####", CultureInfo.InvariantCulture))
                .Append("cqw;");
        }

        /// <summary>DrawingML 的布尔属性：<c>"1"</c> / <c>"true"</c> 为真，缺省或 <c>"0"</c> 为假。</summary>
        private static bool ToggleAttribute(XElement element, string name)
        {
            var value = (string)element.Attribute(name);

            return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region 路径与小工具

        /// <summary><c>ppt/slides/slide1.xml</c> → <c>ppt/slides</c>。</summary>
        private static string DirectoryOf(string path)
        {
            var slash = path.LastIndexOf('/');
            return slash < 0 ? string.Empty : path.Substring(0, slash);
        }

        /// <summary><c>ppt/slides/slide1.xml</c> → <c>ppt/slides/_rels/slide1.xml.rels</c>。</summary>
        private static string RelsPathOf(string path)
        {
            var slash = path.LastIndexOf('/');
            var directory = slash < 0 ? string.Empty : path.Substring(0, slash);
            var file = slash < 0 ? path : path.Substring(slash + 1);

            return (directory.Length == 0 ? string.Empty : directory + "/") + "_rels/" + file + ".rels";
        }

        private static int ReadInt(string value, int fallback)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        #endregion
    }
}
