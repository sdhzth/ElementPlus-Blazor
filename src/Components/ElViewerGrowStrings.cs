using System;

namespace ElementPlus
{
    /// <summary>
    /// <see cref="ElViewerGrow"/> 的文案集合。
    /// <para>
    /// 参考实现把中文硬编码在 Razor 标记里（<c>请选择文件或输入 URL</c>），无法本地化也无法测试；
    /// 这里收敛为单个 <c>Strings</c> 参数，可整体替换或逐项覆盖。
    /// </para>
    /// </summary>
    public sealed class ElViewerGrowStrings
    {
        /// <summary>空态主文案。</summary>
        public string EmptyText { get; set; } = "暂无文档";

        /// <summary>空态副文案。</summary>
        public string EmptyHintText { get; set; } = "选择本地文件，或在工具栏输入文档地址";

        /// <summary>加载中。</summary>
        public string LoadingText { get; set; } = "加载中...";

        /// <summary>加载失败前缀。</summary>
        public string ErrorText { get; set; } = "加载失败";

        /// <summary>类型不受内建渲染器支持。</summary>
        public string UnsupportedText { get; set; } = "暂不支持预览此类型";

        /// <summary>不受支持时的补充说明。</summary>
        public string UnsupportedHintText { get; set; } = "可配置 ExternalViewerUrl 交给外部预览服务，或用「新标签页打开」";

        /// <summary>
        /// 旧版二进制 Office 格式（.doc / .xls / .ppt）的专用提示。
        /// <para>
        /// 这三种是 OLE 复合文档，不是 ZIP 包，解析方式与 <c>.docx</c> / <c>.xlsx</c> / <c>.pptx</c>
        /// 完全不同；含糊地回一句「暂不支持此类型」会让人以为是能力缺失，
        /// 所以这里把「换个格式另存」这条可执行的出路直接写出来。
        /// </para>
        /// </summary>
        public string LegacyOfficeText { get; set; } = "该文件是旧版二进制格式，请另存为 docx / xlsx / pptx 后再预览";

        /// <summary>未通过 URL 安全检查。</summary>
        public string UnsafeUrlText { get; set; } = "地址不被允许";

        /// <summary>文件超出大小上限。</summary>
        public string FileTooLargeText { get; set; } = "文件超出大小上限";

        /// <summary>未知文件名时的占位。</summary>
        public string UnknownFileNameText { get; set; } = "未命名文档";

        /// <summary>就绪状态。</summary>
        public string ReadyText { get; set; } = "就绪";

        /// <summary>未加载状态。</summary>
        public string IdleText { get; set; } = "未加载";

        /// <summary>打开文件。</summary>
        public string OpenFileText { get; set; } = "打开文件";

        /// <summary>输入地址。</summary>
        public string OpenUrlText { get; set; } = "输入地址";

        /// <summary>URL 输入框占位符。</summary>
        public string UrlPlaceholderText { get; set; } = "https://example.com/doc.pdf";

        /// <summary>URL 输入框的提交按钮。</summary>
        public string UrlSubmitText { get; set; } = "加载";

        /// <summary>放大。</summary>
        public string ZoomInText { get; set; } = "放大";

        /// <summary>缩小。</summary>
        public string ZoomOutText { get; set; } = "缩小";

        /// <summary>重置缩放。</summary>
        public string ZoomResetText { get; set; } = "重置缩放";

        /// <summary>逆时针旋转。</summary>
        public string RotateLeftText { get; set; } = "逆时针旋转";

        /// <summary>顺时针旋转。</summary>
        public string RotateRightText { get; set; } = "顺时针旋转";

        /// <summary>最大化。</summary>
        public string MaximizeText { get; set; } = "最大化";

        /// <summary>退出最大化。</summary>
        public string RestoreText { get; set; } = "退出最大化";

        /// <summary>新标签页打开。</summary>
        public string OpenInNewTabText { get; set; } = "新标签页打开";

        /// <summary>下载。</summary>
        public string DownloadText { get; set; } = "下载";

        /// <summary>打印。</summary>
        public string PrintText { get; set; } = "打印";

        /// <summary>重新加载。</summary>
        public string ReloadText { get; set; } = "重新加载";

        /// <summary>关闭文档。</summary>
        public string CloseText { get; set; } = "关闭";

        /// <summary>上一页。</summary>
        public string PrevPageText { get; set; } = "上一页";

        /// <summary>下一页。</summary>
        public string NextPageText { get; set; } = "下一页";

        /// <summary>注释卡片的作者行标签。</summary>
        public string AnnotationAuthorLabel { get; set; } = "作者";

        /// <summary>注释卡片的时间行标签。</summary>
        public string AnnotationTimeLabel { get; set; } = "时间";

        /// <summary>简体中文。</summary>
        public static ElViewerGrowStrings ZhCn
        {
            get { return new ElViewerGrowStrings(); }
        }

        /// <summary>英语。</summary>
        public static ElViewerGrowStrings En
        {
            get
            {
                return new ElViewerGrowStrings
                {
                    EmptyText = "No document",
                    EmptyHintText = "Pick a local file, or type a document URL in the toolbar",
                    LoadingText = "Loading...",
                    ErrorText = "Failed to load",
                    UnsupportedText = "This type cannot be previewed",
                    UnsupportedHintText = "Configure ExternalViewerUrl, or open the document in a new tab",
                    LegacyOfficeText = "This is a legacy binary file. Save it as docx / xlsx / pptx to preview it",
                    UnsafeUrlText = "The address is not allowed",
                    FileTooLargeText = "The file is too large",
                    UnknownFileNameText = "Untitled document",
                    ReadyText = "Ready",
                    IdleText = "Idle",
                    OpenFileText = "Open file",
                    OpenUrlText = "Open URL",
                    UrlPlaceholderText = "https://example.com/doc.pdf",
                    UrlSubmitText = "Load",
                    ZoomInText = "Zoom in",
                    ZoomOutText = "Zoom out",
                    ZoomResetText = "Reset zoom",
                    RotateLeftText = "Rotate left",
                    RotateRightText = "Rotate right",
                    MaximizeText = "Maximize",
                    RestoreText = "Restore",
                    OpenInNewTabText = "Open in new tab",
                    DownloadText = "Download",
                    PrintText = "Print",
                    ReloadText = "Reload",
                    CloseText = "Close",
                    PrevPageText = "Previous page",
                    NextPageText = "Next page",
                    AnnotationAuthorLabel = "Author",
                    AnnotationTimeLabel = "Time"
                };
            }
        }

        /// <summary>按语言取一份默认文案。</summary>
        public static ElViewerGrowStrings Create(ElViewerLocale locale)
        {
            return locale == ElViewerLocale.En ? En : ZhCn;
        }

        /// <summary>按动作取对应文案。</summary>
        public string Resolve(ElViewerToolbarAction action)
        {
            switch (action)
            {
                case ElViewerToolbarAction.OpenFile:
                    return OpenFileText;
                case ElViewerToolbarAction.OpenUrl:
                    return OpenUrlText;
                case ElViewerToolbarAction.ZoomIn:
                    return ZoomInText;
                case ElViewerToolbarAction.ZoomOut:
                    return ZoomOutText;
                case ElViewerToolbarAction.ZoomReset:
                    return ZoomResetText;
                case ElViewerToolbarAction.RotateLeft:
                    return RotateLeftText;
                case ElViewerToolbarAction.RotateRight:
                    return RotateRightText;
                case ElViewerToolbarAction.Maximize:
                    return MaximizeText;
                case ElViewerToolbarAction.OpenInNewTab:
                    return OpenInNewTabText;
                case ElViewerToolbarAction.Download:
                    return DownloadText;
                case ElViewerToolbarAction.Print:
                    return PrintText;
                case ElViewerToolbarAction.Reload:
                    return ReloadText;
                case ElViewerToolbarAction.Close:
                    return CloseText;
                case ElViewerToolbarAction.PrevPage:
                    return PrevPageText;
                case ElViewerToolbarAction.NextPage:
                    return NextPageText;
                default:
                    return string.Empty;
            }
        }
    }
}
