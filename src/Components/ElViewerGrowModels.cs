using System;

namespace ElementPlus
{
    /// <summary>
    /// <see cref="ElViewerGrow"/> 的文档描述：URL、字节、文本三种来源统一成一个对象。
    /// <para>
    /// 参考实现把「容器 id + 文件输入 id + URL 字符串」散落在组件字段里，服务层再靠 id 去 JS 里找实例；
    /// 本实现把文档本身建模成对象，组件只持有它，因此状态可以在 C# 侧完整断言，也支持双向绑定。
    /// </para>
    /// </summary>
    public sealed class ElViewerDocument
    {
        private string sourceUrl;

        private string renderedOfficeHtml;

        private bool renderedOfficeHtmlComputed;

        /// <summary>文档的远程或相对地址。</summary>
        public string Url { get; set; }

        /// <summary>文件名（影响类型识别、下载名与标题显示）。</summary>
        public string FileName { get; set; }

        /// <summary>MIME 类型；为空时由 <see cref="Kind"/> 反推。</summary>
        public string ContentType { get; set; }

        /// <summary>文档类型。</summary>
        public ElViewerDocumentKind Kind { get; set; }

        /// <summary>字节长度；URL 来源的文档未知时为 0。</summary>
        public long Size { get; set; }

        /// <summary>文档字节（本地文件 / 文本编码后的结果）。</summary>
        public byte[] Content { get; set; }

        /// <summary>文本型文档解码后的内容。</summary>
        public string Text { get; set; }

        /// <summary>是否带有字节内容。</summary>
        public bool HasContent
        {
            get { return Content != null && Content.Length > 0; }
        }

        /// <summary>标题：优先文件名，其次地址，最后退回类型名。</summary>
        public string Title
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(FileName))
                {
                    return FileName;
                }

                if (!string.IsNullOrWhiteSpace(Url))
                {
                    return Url;
                }

                return Kind.ToString();
            }
        }

        /// <summary>扩展名（含点）。</summary>
        public string Extension
        {
            get { return ElViewerGrowEngine.GetExtension(FileName); }
        }

        /// <summary>实际使用的 MIME：显式指定的优先，否则按类型反推。</summary>
        public string ResolvedContentType
        {
            get
            {
                return string.IsNullOrWhiteSpace(ContentType)
                    ? ElViewerGrowEngine.ResolveContentType(FileName, Kind)
                    : ContentType;
            }
        }

        /// <summary>
        /// 用于 <c>src</c> / <c>href</c> 的地址：有 URL 用它，否则把字节转成内联 data URI。
        /// 结果会被缓存（字节内容默认视为不可变）。
        /// </summary>
        public string SourceUrl
        {
            get
            {
                if (sourceUrl != null)
                {
                    return sourceUrl.Length == 0 ? null : sourceUrl;
                }

                if (!string.IsNullOrWhiteSpace(Url))
                {
                    sourceUrl = Url;
                }
                else if (HasContent)
                {
                    string uri;
                    sourceUrl = ElViewerGrowEngine.TryBuildDataUri(Content, ResolvedContentType, out uri) ? uri : string.Empty;
                }
                else
                {
                    sourceUrl = string.Empty;
                }

                return sourceUrl.Length == 0 ? null : sourceUrl;
            }
        }

        /// <summary>地址是否通过了安全校验（没有地址时视为安全）。</summary>
        public bool IsSafe
        {
            get { return string.IsNullOrWhiteSpace(Url) || ElViewerGrowEngine.IsSafeUrl(Url); }
        }

        /// <summary>
        /// 用内建 OOXML 解析器渲染出来的 HTML；不是 Office 文档、没有字节、或解析失败时为 <c>null</c>。
        /// <para>
        /// <b>惰性且带缓存</b>：解析一份 docx / xlsx / pptx 需要解压整个 ZIP 并遍历若干 XML，
        /// 而这个属性在每次组件重渲染（缩放、翻页、悬停工具栏）时都会被读到 ——
        /// 不缓存的话，动一下缩放就会把整份文档重新解析一遍。
        /// </para>
        /// </summary>
        public string RenderedOfficeHtml
        {
            get
            {
                if (!renderedOfficeHtmlComputed)
                {
                    renderedOfficeHtmlComputed = true;
                    renderedOfficeHtml = HasContent && ElViewerGrowOffice.IsOfficeKind(Kind)
                        ? ElViewerGrowOffice.Render(Kind, Content, FileName)
                        : null;
                }

                return renderedOfficeHtml;
            }
        }

        /// <summary>创建一个来自 URL 的文档。</summary>
        public static ElViewerDocument FromUrl(string url, string fileName = null, string contentType = null)
        {
            var document = new ElViewerDocument
            {
                Url = url,
                FileName = string.IsNullOrWhiteSpace(fileName) ? ElViewerGrowEngine.GuessFileName(url) : fileName,
                ContentType = contentType
            };

            return document.Resolve();
        }

        /// <summary>创建一个来自字节的文档。</summary>
        public static ElViewerDocument FromBytes(byte[] content, string fileName, string contentType = null)
        {
            var document = new ElViewerDocument
            {
                Content = content,
                FileName = fileName,
                ContentType = contentType
            };

            return document.Resolve();
        }

        /// <summary>创建一个来自文本的文档（按 UTF-8 编码为字节）。</summary>
        public static ElViewerDocument FromText(string text, string fileName, string contentType = null)
        {
            var document = new ElViewerDocument
            {
                Text = ElViewerGrowEngine.NormalizeNewLines(text),
                FileName = fileName,
                ContentType = contentType
            };

            document.Content = ElViewerGrowEngine.EncodeText(document.Text);

            return document.Resolve();
        }

        /// <summary>补齐派生的类型 / MIME / 长度 / 文本，返回自身便于链式调用。</summary>
        public ElViewerDocument Resolve()
        {
            Kind = ElViewerGrowEngine.ResolveKind(FileName, ContentType);

            if (HasContent)
            {
                Size = Content.Length;
            }
            else if (Size < 0)
            {
                Size = 0;
            }

            if (string.IsNullOrEmpty(Text) && ElViewerGrowEngine.IsTextual(Kind) && HasContent)
            {
                string text;

                if (ElViewerGrowEngine.TryDecodeText(Content, out text))
                {
                    Text = text;
                }
            }

            sourceUrl = null;
            renderedOfficeHtml = null;
            renderedOfficeHtmlComputed = false;
            return this;
        }

        /// <summary>浅拷贝（用于「重新解析」时保留来源而重置派生状态）。</summary>
        public ElViewerDocument Clone()
        {
            var clone = new ElViewerDocument
            {
                Url = Url,
                FileName = FileName,
                ContentType = ContentType,
                Kind = Kind,
                Size = Size,
                Content = Content,
                Text = Text
            };

            // 字节没变，渲染结果也就没变，直接沿用缓存，省掉一次整文档解析。
            clone.sourceUrl = sourceUrl;
            clone.renderedOfficeHtml = renderedOfficeHtml;
            clone.renderedOfficeHtmlComputed = renderedOfficeHtmlComputed;

            return clone;
        }
    }

    /// <summary>
    /// 工具栏条目描述符。参考实现把按钮写死在标记里、无法配置；
    /// 这里沿用 <c>ElHtmlEditorGrow</c> 的做法，把工具栏变成一张可以在测试里直接断言的数据表。
    /// </summary>
    public sealed class ElViewerTool
    {
        /// <summary>动作。</summary>
        public ElViewerToolbarAction Action { get; set; } = ElViewerToolbarAction.Separator;

        /// <summary>是否为分隔符（分隔符不渲染按钮）。</summary>
        public bool IsSeparator { get; set; }

        /// <summary>自定义提示文案；为空时按动作从 <see cref="ElViewerGrowStrings"/> 取。</summary>
        public string Title { get; set; }

        /// <summary>自定义 SVG 内部标记；为空时按动作从 <see cref="ElViewerGrowIcons"/> 取。</summary>
        public string Icon { get; set; }

        /// <summary>创建一个动作按钮。</summary>
        public static ElViewerTool Of(ElViewerToolbarAction action)
        {
            return new ElViewerTool { Action = action, IsSeparator = false };
        }

        /// <summary>创建一个分隔符。</summary>
        public static ElViewerTool Separator()
        {
            return new ElViewerTool { Action = ElViewerToolbarAction.Separator, IsSeparator = true };
        }
    }

    /// <summary>
    /// OFD 注释的交互信息（GB/T 48666 的 Remark / Creator / LastModDate），
    /// 由 dom.js 通道从渲染核心的 data 属性上取回，组件据此开注释卡片。
    /// </summary>
    public sealed class ElViewerAnnotationInfo
    {
        /// <summary>注释内容（<c>Remark</c>）。</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>作者（<c>Creator</c>）。</summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>最后修改时间（<c>LastModDate</c>）。</summary>
        public string Time { get; set; } = string.Empty;

        /// <summary>注释类型（<c>Type</c>）。</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>注释 ID（<c>ID</c>）。</summary>
        public string Id { get; set; } = string.Empty;
    }
}
