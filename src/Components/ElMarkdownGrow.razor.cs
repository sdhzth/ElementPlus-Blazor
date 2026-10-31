using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace ElementPlus
{
    /// <summary>
    /// Markdown 渲染组件（Element Plus 自渲染版）。
    /// <para>
    /// 对应参考实现 Radzen 的 <c>RadzenMarkdown</c>，但**完全不依赖 Radzen**，也**不使用 Markdig 等任何第三方解析库**：
    /// 解析（<see cref="ElMarkdownGrowParser"/>）、消毒（<see cref="ElMarkdownGrowSanitizer"/>）、渲染（<see cref="ElMarkdownGrowHtmlRenderer"/>）
    /// 全部为纯 C# 自写实现，零 JS 互操作，服务端渲染 / 单元测试 / 离线打包均可用。
    /// </para>
    /// <para>
    /// 浏览器兼容性：输出的 HTML 只使用 Chromium 75 起就支持的元素与属性，配套样式也仅使用 Chrome 49+ 的特性，
    /// 不使用 <c>:has()</c>/<c>:is()</c>/<c>:where()</c>/逻辑属性/<c>gap</c>/<c>clamp()</c>/CSS 嵌套等较新特性。
    /// </para>
    /// </summary>
    public partial class ElMarkdownGrow : ElementComponentBase
    {
        /// <summary>Markdown 源文本（优先于 <see cref="ChildContent"/>）。</summary>
        [Parameter]
        public string Text { get; set; }

        /// <summary>直接写在标签内部的 Markdown 文本；其中嵌入的 HTML 元素/Blazor 组件会原样保留在输出中。</summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>是否渲染 Markdown 中内嵌的 HTML（默认 <c>true</c>）。为 <c>false</c> 时所有 HTML 作为纯文本显示。</summary>
        [Parameter]
        public bool AllowHtml { get; set; } = true;

        /// <summary>允许的 HTML 标签白名单；为 <c>null</c> 时使用默认白名单。</summary>
        [Parameter]
        public IEnumerable<string> AllowedHtmlTags { get; set; }

        /// <summary>允许的 HTML 属性白名单；为 <c>null</c> 时使用默认白名单。</summary>
        [Parameter]
        public IEnumerable<string> AllowedHtmlAttributes { get; set; }

        /// <summary>自动生成标题锚点的最大层级（0 表示关闭）。</summary>
        [Parameter]
        public int AutoLinkHeadingDepth { get; set; }

        /// <summary>软换行（单个换行）是否渲染为 <c>&lt;br /&gt;</c>。</summary>
        [Parameter]
        public bool SoftBreakAsHard { get; set; }

        /// <summary>是否把 <c>:smile:</c> 这类 emoji 短代码转换为字符。</summary>
        [Parameter]
        public bool EnableEmoji { get; set; } = true;

        /// <summary>是否把裸露的 URL 自动转换为链接。</summary>
        [Parameter]
        public bool EnableAutoLinkUrl { get; set; } = true;

        /// <summary>内容为空时显示的文本。</summary>
        [Parameter]
        public string EmptyText { get; set; } = "暂无内容";

        /// <summary>内容为空时是否隐藏该提示。</summary>
        [Parameter]
        public bool HideEmptyText { get; set; }

        private MdDocument document;
        private readonly List<ElMarkdownGrowSegment> segments = new List<ElMarkdownGrowSegment>();
        private readonly Dictionary<int, RenderFragment> outlets = new Dictionary<int, RenderFragment>();

        /// <summary>解析后的 AST（供展示与测试断言）。</summary>
        public MdDocument Document => document ?? (document = ElMarkdownGrowParser.Parse(ResolveMarkdown(), BuildOptions()));

        /// <summary>渲染输出的分段集合（HTML 段与占位段交替）。</summary>
        public IReadOnlyList<ElMarkdownGrowSegment> Segments => segments;

        /// <summary>是否存在来自 <c>ChildContent</c> 的嵌入元素/组件占位。</summary>
        public bool HasOutlets => outlets.Count > 0;

        /// <summary>页面渲染用的 HTML 片段。</summary>
        public string InnerMarkup => RenderHtml();

        /// <summary>根据当前参数构造解析/渲染选项。</summary>
        public ElMarkdownGrowOptions BuildOptions()
        {
            return new ElMarkdownGrowOptions
            {
                AllowHtml = AllowHtml,
                AllowedHtmlTags = AllowedHtmlTags,
                AllowedHtmlAttributes = AllowedHtmlAttributes,
                AutoLinkHeadingDepth = AutoLinkHeadingDepth,
                SoftBreakAsHard = SoftBreakAsHard,
                EnableEmoji = EnableEmoji,
                EnableAutoLinkUrl = EnableAutoLinkUrl,
                EmptyText = HideEmptyText ? string.Empty : (EmptyText ?? string.Empty)
            };
        }

        /// <summary>把当前 Markdown 渲染为 HTML 片段（与页面显示内容完全一致）。</summary>
        public string RenderHtml()
        {
            return ElMarkdownGrowHtmlRenderer.Render(Document, BuildOptions());
        }

        /// <summary>取出指定占位的渲染片段。</summary>
        public bool TryGetOutlet(int id, out RenderFragment fragment)
        {
            return outlets.TryGetValue(id, out fragment);
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            document = null;
            segments.Clear();
            outlets.Clear();

            var options = BuildOptions();
            document = ElMarkdownGrowParser.Parse(ResolveMarkdown(), options);

            segments.AddRange(ElMarkdownGrowHtmlRenderer.RenderSegments(document, options));
        }

        private string ResolveMarkdown()
        {
            if (!string.IsNullOrEmpty(Text))
            {
                return Text;
            }

            if (ChildContent == null)
            {
                return string.Empty;
            }

            return ExtractMarkdownFromChildContent();
        }

        private string ExtractMarkdownFromChildContent()
        {
#pragma warning disable BL0006 // 使用 RenderTree 类型以支持“标签内直接写 Markdown，同时保留嵌入的元素/组件”
            var builder = new RenderTreeBuilder();
            ChildContent(builder);

            var frames = builder.GetFrames();

            var markdown = new StringBuilder();
            var markerId = 0;
            var index = 0;

            while (index < frames.Count)
            {
                var frame = frames.Array[index];

                switch (frame.FrameType)
                {
                    case RenderTreeFrameType.Text:
                        markdown.Append(frame.TextContent);
                        index++;
                        break;

                    case RenderTreeFrameType.Markup:
                        markdown.Append(frame.MarkupContent);
                        index++;
                        break;

                    case RenderTreeFrameType.Element:
                        {
                            var id = markerId++;
                            markdown.Append(ElMarkdownGrowParser.CreateOutletMarker(id));
                            outlets[id] = CreateFragment(frames, index, index + frame.ElementSubtreeLength);
                            index += frame.ElementSubtreeLength;
                            break;
                        }

                    case RenderTreeFrameType.Component:
                        {
                            var id = markerId++;
                            markdown.Append(ElMarkdownGrowParser.CreateOutletMarker(id));
                            outlets[id] = CreateFragment(frames, index, index + frame.ComponentSubtreeLength);
                            index += frame.ComponentSubtreeLength;
                            break;
                        }

                    case RenderTreeFrameType.Region:
                        CopyRegionChildren(markdown, frames, index, ref markerId);
                        index += frame.RegionSubtreeLength;
                        break;

                    default:
                        index++;
                        break;
                }
            }

#pragma warning restore BL0006
            return markdown.ToString();
        }

#pragma warning disable BL0006
        private void CopyRegionChildren(StringBuilder markdown, ArrayRange<RenderTreeFrame> frames, int regionIndex, ref int markerId)
        {
            var length = frames.Array[regionIndex].RegionSubtreeLength;
            var index = regionIndex + 1;
            var end = regionIndex + length;

            while (index < end)
            {
                var frame = frames.Array[index];

                switch (frame.FrameType)
                {
                    case RenderTreeFrameType.Text:
                        markdown.Append(frame.TextContent);
                        index++;
                        break;

                    case RenderTreeFrameType.Markup:
                        markdown.Append(frame.MarkupContent);
                        index++;
                        break;

                    case RenderTreeFrameType.Element:
                        {
                            var id = markerId++;
                            markdown.Append(ElMarkdownGrowParser.CreateOutletMarker(id));
                            outlets[id] = CreateFragment(frames, index, index + frame.ElementSubtreeLength);
                            index += frame.ElementSubtreeLength;
                            break;
                        }

                    case RenderTreeFrameType.Component:
                        {
                            var id = markerId++;
                            markdown.Append(ElMarkdownGrowParser.CreateOutletMarker(id));
                            outlets[id] = CreateFragment(frames, index, index + frame.ComponentSubtreeLength);
                            index += frame.ComponentSubtreeLength;
                            break;
                        }

                    default:
                        index++;
                        break;
                }
            }
#pragma warning restore BL0006
        }

#pragma warning disable BL0006
        private static RenderFragment CreateFragment(ArrayRange<RenderTreeFrame> frames, int start, int end)
        {
            return builder => CopyFrames(builder, frames, start, end);
        }

        private static void CopyFrames(RenderTreeBuilder builder, ArrayRange<RenderTreeFrame> frames, int start, int end)
        {
            while (start < end)
            {
                var frame = frames.Array[start];

                switch (frame.FrameType)
                {
                    case RenderTreeFrameType.Element:
                        builder.OpenElement(frame.Sequence, frame.ElementName);
                        CopyFrames(builder, frames, start + 1, start + frame.ElementSubtreeLength);
                        builder.CloseElement();
                        start += frame.ElementSubtreeLength;
                        break;

                    case RenderTreeFrameType.Component:
                        builder.OpenComponent(frame.Sequence, frame.ComponentType);
                        CopyFrames(builder, frames, start + 1, start + frame.ComponentSubtreeLength);
                        builder.CloseComponent();
                        start += frame.ComponentSubtreeLength;
                        break;

                    case RenderTreeFrameType.Region:
                        CopyFrames(builder, frames, start + 1, start + frame.RegionSubtreeLength);
                        start += frame.RegionSubtreeLength;
                        break;

                    case RenderTreeFrameType.Attribute:
                        builder.AddAttribute(frame.Sequence, frame.AttributeName, frame.AttributeValue);
                        start++;
                        break;

                    case RenderTreeFrameType.ElementReferenceCapture:
                        builder.AddElementReferenceCapture(frame.Sequence, frame.ElementReferenceCaptureAction);
                        start++;
                        break;

                    case RenderTreeFrameType.ComponentReferenceCapture:
                        builder.AddComponentReferenceCapture(frame.Sequence, frame.ComponentReferenceCaptureAction);
                        start++;
                        break;

                    case RenderTreeFrameType.Text:
                        if (!string.IsNullOrEmpty(frame.TextContent))
                        {
                            builder.AddContent(frame.Sequence, frame.TextContent);
                        }

                        start++;
                        break;

                    case RenderTreeFrameType.Markup:
                        if (!string.IsNullOrEmpty(frame.MarkupContent))
                        {
                            builder.AddMarkupContent(frame.Sequence, frame.MarkupContent);
                        }

                        start++;
                        break;

                    default:
                        start++;
                        break;
                }
            }
        }
#pragma warning restore BL0006
    }
}
