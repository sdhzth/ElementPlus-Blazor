using System;

namespace ElementPlus
{
    /// <summary>
    /// <see cref="ElViewerGrow"/> 支持的文档类型。
    /// <para>
    /// 参考实现（jit-viewer Blazor 封装）把类型判断整体交给外部 JS SDK，本实现把类型判断收敛为
    /// 一张纯 C# 的「扩展名 / MIME → 类型」映射表（见 <see cref="ElViewerGrowEngine"/>），
    /// 因此组件的可用性不依赖任何外部脚本。
    /// </para>
    /// </summary>
    public enum ElViewerDocumentKind
    {
        /// <summary>无法识别的类型。</summary>
        Unknown,

        /// <summary>PDF 文档，使用浏览器内建的 PDF 能力呈现。</summary>
        Pdf,

        /// <summary>Word 文档（doc / docx）。</summary>
        Word,

        /// <summary>Excel 工作簿（xls / xlsx）。</summary>
        Excel,

        /// <summary>PowerPoint 演示文稿（ppt / pptx）。</summary>
        PowerPoint,

        /// <summary>OFD 版式文档。</summary>
        Ofd,

        /// <summary>纯文本。</summary>
        Text,

        /// <summary>Markdown 文本。</summary>
        Markdown,

        /// <summary>CSV / TSV 表格文本。</summary>
        Csv,

        /// <summary>JSON 文本。</summary>
        Json,

        /// <summary>图片。</summary>
        Image,

        /// <summary>视频。</summary>
        Video,

        /// <summary>音频。</summary>
        Audio,

        /// <summary>CAD 图纸（dxf / dwg）。</summary>
        Cad,

        /// <summary>三维模型（glb / gltf / stl）。</summary>
        Model
    }

    /// <summary>
    /// 预览器的当前状态。
    /// </summary>
    public enum ElViewerStatus
    {
        /// <summary>没有文档。</summary>
        Empty,

        /// <summary>正在加载（宿主通过 <see cref="ElViewerGrow.Loading"/> 驱动）。</summary>
        Loading,

        /// <summary>已就绪，可以呈现。</summary>
        Ready,

        /// <summary>加载或解析出错。</summary>
        Error,

        /// <summary>类型可识别，但内建渲染器不支持、且没有可用的外链预览地址。</summary>
        Unsupported
    }

    /// <summary>
    /// 预览器主题。
    /// </summary>
    public enum ElViewerTheme
    {
        /// <summary>浅色。</summary>
        Light,

        /// <summary>深色。</summary>
        Dark
    }

    /// <summary>
    /// 内建语言。
    /// </summary>
    public enum ElViewerLocale
    {
        /// <summary>简体中文。</summary>
        ZhCn,

        /// <summary>英语。</summary>
        En
    }

    /// <summary>
    /// 工具栏动作。参考实现只把「文件 / URL / 缩放 / 旋转 / 打印 / 下载 / 关闭」写死在标记里，
    /// 这里改为数据驱动的描述表（<see cref="ElViewerGrowToolbar"/>），动作集合可整体替换或逐项隐藏。
    /// </summary>
    public enum ElViewerToolbarAction
    {
        /// <summary>分隔符（不渲染按钮）。</summary>
        Separator,

        /// <summary>打开本地文件。</summary>
        OpenFile,

        /// <summary>展开 / 收起 URL 输入条。</summary>
        OpenUrl,

        /// <summary>缩小。</summary>
        ZoomOut,

        /// <summary>放大。</summary>
        ZoomIn,

        /// <summary>重置缩放。</summary>
        ZoomReset,

        /// <summary>逆时针旋转 90°。</summary>
        RotateLeft,

        /// <summary>顺时针旋转 90°。</summary>
        RotateRight,

        /// <summary>页面内最大化 / 还原。</summary>
        Maximize,

        /// <summary>在新标签页打开原始文档。</summary>
        OpenInNewTab,

        /// <summary>下载原始文档。</summary>
        Download,

        /// <summary>
        /// 打印当前文档。
        /// <para>
        /// 这是唯一需要借道 JS 的工具栏动作（<c>window.print()</c> 没有等价的原生 HTML 触发器）。
        /// 已渲染的内容走页面自身的 <c>@media print</c> 规则直接打印；PDF / 外链预览这类
        /// iframe 形态则把源地址交给隐藏 iframe 调它的 <c>print()</c>，跨域失败时回退到新窗口打开。
        /// </para>
        /// </summary>
        Print,

        /// <summary>重新解析当前文档。</summary>
        Reload,

        /// <summary>关闭当前文档，回到空态。</summary>
        Close,

        /// <summary>上一页。</summary>
        PrevPage,

        /// <summary>下一页。</summary>
        NextPage
    }
}
