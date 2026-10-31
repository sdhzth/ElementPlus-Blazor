using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ElementPlus
{
    /// <summary>
    /// Element Plus 自渲染的文档预览器。
    /// <para>
    /// 参考实现（jit-viewer 的 Blazor 封装）把「识别 / 渲染 / 缩放 / 旋转 / 打印 / 下载」全部
    /// 交给外部 JS SDK，Blazor 侧只剩一串容器 id 与若干 <c>InvokeVoidAsync</c>；
    /// 本实现把这些能力尽量收回到 C#：
    /// <list type="bullet">
    /// <item><description>类型识别、URL 安全、文件名推断、体积格式化 —— <see cref="ElViewerGrowEngine"/>；</description></item>
    /// <item><description>文本 / Markdown / CSV / 图片 / 音视频 / PDF 的正文渲染 —— <see cref="ElViewerGrowRenderer"/>；</description></item>
    /// <item><description>缩放与旋转 —— 纯 CSS <c>transform</c>，不需要任何脚本；</description></item>
    /// <item><description>下载 —— 原生 <c>&lt;a download&gt;</c>；最大化 —— 纯 CSS 定位；打印 —— <c>@media print</c>。</description></item>
    /// </list>
    /// 因此除 OFD 之外的每条路径在 bUnit / SSR / 预渲染环境下都能完整渲染并断言。
    /// </para>
    /// <para>
    /// <b>OFD 是唯一的例外，且是可选、懒加载的</b>：浏览器没有 OFD 原生能力，C# 侧也没有
    /// 实现 OFD 版面解析，所以 <see cref="EnableOfdPreview"/> 打开时组件会调用
    /// <c>dom.js</c> 的 <c>elementOfdViewer*</c> 通道，由它按需下载
    /// <c>wwwroot/js/ofd/ofd.umd.min.js</c>（约 0.9 MB）完成解析与逐页绘制。
    /// 不打开 OFD 就永远不会下载这段脚本；关掉 <see cref="EnableOfdPreview"/> 则连这条通道都不用。
    /// </para>
    /// </summary>
    public partial class ElViewerGrow : ElementComponentBase
    {
        #region 参数：文档来源

        /// <summary>完整的文档描述；与 <see cref="Src"/> / <see cref="Content"/> / <see cref="Text"/> 三选一使用。</summary>
        [Parameter]
        public ElViewerDocument Document { get; set; }

        /// <summary>文档变化时回调。</summary>
        [Parameter]
        public EventCallback<ElViewerDocument> DocumentChanged { get; set; }

        /// <summary>文档地址。</summary>
        [Parameter]
        public string Src { get; set; }

        /// <summary>文件名（影响类型识别、下载名与标题）。</summary>
        [Parameter]
        public string FileName { get; set; }

        /// <summary>MIME 类型。</summary>
        [Parameter]
        public string ContentType { get; set; }

        /// <summary>文档字节。</summary>
        [Parameter]
        public byte[] Content { get; set; }

        /// <summary>文本文档内容。</summary>
        [Parameter]
        public string Text { get; set; }

        /// <summary>初始化时是否自动加载 <see cref="Src"/> / <see cref="Text"/> / <see cref="Content"/>。</summary>
        [Parameter]
        public bool AutoLoad { get; set; } = true;

        /// <summary>宿主驱动的加载态（远程取流时置 true）。</summary>
        [Parameter]
        public bool Loading { get; set; }

        /// <summary>
        /// 加载框的最短显示时长（毫秒），默认 0（关闭）。
        /// <para>
        /// 打开文件的动作里，「读字节 → 解压 → 解析」往往在几毫秒内就跑完了 ——
        /// 不给一个下限的话，加载框会在同一帧里出现又消失，用户只看到一次闪烁。
        /// 设为正数（demo 用 400）后，门控把「加载中」撑住：同步解析被挪进门控里跑，
        /// 读得快的文档也能看到一次干净的加载反馈；只保留真实耗时的加载态。
        /// </para>
        /// <para>
        /// 默认 0 走原来的同步路径（状态直接算成终态），保证「加载完立刻断言 Ready」
        /// 的既有契约与零延迟的宿主都不受影响 —— 想要加载框的宿主显式打开即可。
        /// </para>
        /// </summary>
        [Parameter]
        public int MinLoadingMilliseconds { get; set; } = 0;

        #endregion

        #region 参数：外观

        /// <summary>主题。</summary>
        [Parameter]
        public ElViewerTheme Theme { get; set; } = ElViewerTheme.Light;

        /// <summary>语言。</summary>
        [Parameter]
        public ElViewerLocale Locale { get; set; } = ElViewerLocale.ZhCn;

        /// <summary>文案集合；为空时按 <see cref="Locale"/> 取默认值。</summary>
        [Parameter]
        public ElViewerGrowStrings Strings { get; set; }

        /// <summary>整体宽度。</summary>
        [Parameter]
        public string Width { get; set; } = "100%";

        /// <summary>预览区高度。</summary>
        [Parameter]
        public string Height { get; set; } = "600px";

        /// <summary>是否显示工具栏。</summary>
        [Parameter]
        public bool ShowToolbar { get; set; } = true;

        /// <summary>是否显示底部状态栏。</summary>
        [Parameter]
        public bool ShowStatusBar { get; set; } = true;

        /// <summary>自定义工具栏；为空时使用 <see cref="ElViewerGrowToolbar.Default"/>。</summary>
        [Parameter]
        public ElViewerTool[] Toolbar { get; set; }

        /// <summary>需要隐藏的动作。</summary>
        [Parameter]
        public ElViewerToolbarAction[] HiddenTools { get; set; }

        /// <summary>是否显示边框。</summary>
        [Parameter]
        public bool Bordered { get; set; } = true;

        #endregion

        #region 参数：视图

        /// <summary>当前缩放。</summary>
        [Parameter]
        public double Zoom { get; set; } = 1.0;

        /// <summary>缩放变化回调。</summary>
        [Parameter]
        public EventCallback<double> ZoomChanged { get; set; }

        /// <summary>当前旋转角度（自动归一到 0 / 90 / 180 / 270）。</summary>
        [Parameter]
        public int Rotation { get; set; }

        /// <summary>旋转变化回调。</summary>
        [Parameter]
        public EventCallback<int> RotationChanged { get; set; }

        /// <summary>最小缩放。</summary>
        [Parameter]
        public double MinZoom { get; set; } = 0.25;

        /// <summary>最大缩放。</summary>
        [Parameter]
        public double MaxZoom { get; set; } = 4.0;

        /// <summary>当前页码（1 起）。</summary>
        [Parameter]
        public int PageNumber { get; set; } = 1;

        /// <summary>页码变化回调。</summary>
        [Parameter]
        public EventCallback<int> PageNumberChanged { get; set; }

        /// <summary>总页数；小于等于 0 时隐藏翻页工具。</summary>
        [Parameter]
        public int PageCount { get; set; }

        /// <summary>是否最大化（页面内铺满）。</summary>
        [Parameter]
        public bool Maximized { get; set; }

        /// <summary>最大化变化回调。</summary>
        [Parameter]
        public EventCallback<bool> MaximizedChanged { get; set; }

        #endregion

        #region 参数：文件与外链

        /// <summary>是否允许选择本地文件。</summary>
        [Parameter]
        public bool AllowFileOpen { get; set; } = true;

        /// <summary>
        /// 是否启用内置的 OFD 渲染通道（默认开）。
        /// <para>
        /// 打开后，<c>.ofd</c> 文档不再落到「不支持」：组件会把字节（或地址）交给
        /// <c>dom.js</c> 的 <c>elementOfdViewer*</c> 通道，由它按需加载 OFD 渲染核心完成
        /// 解析与逐页绘制。宿主没引 <c>dom.js</c>、或核心脚本取不到时，OFD 依旧回到「不支持」，
        /// 不会假装修好了。关掉它则完全不走这条通道（宿主要么自己注册渲染器，要么用外链预览）。
        /// </para>
        /// </summary>
        [Parameter]
        public bool EnableOfdPreview { get; set; } = true;

        /// <summary>
        /// 是否启用内置的 Office 渲染（默认开）：把 <c>.docx</c> / <c>.xlsx</c> / <c>.pptx</c>
        /// 的字节直接解析成 HTML。
        /// <para>
        /// 与 OFD 通道的差别在于：这条通道**不下载任何脚本、也不依赖任何第三方库** ——
        /// OOXML 本质是「ZIP + 若干 XML」，组件用 .NET 自带的 <c>ZipArchive</c> 与 <c>XDocument</c>
        /// 自己读，所以只要拿到文件字节就能渲染（本地选择文件、或宿主用 <see cref="ContentLoader"/> 抓流）。
        /// </para>
        /// <para>
        /// 只有地址、又没配 <see cref="ContentLoader"/> 时仍然落到「不支持」——
        /// 浏览器里跨域读一个 URL 的字节本来就不成立，装成可以反而是骗人。
        /// 关掉它则 Office 文档回到「不支持 → 走外链预览」的原路径。
        /// </para>
        /// </summary>
        [Parameter]
        public bool EnableOfficePreview { get; set; } = true;

        /// <summary>文件选择框的 accept；为空时使用 <see cref="ElViewerGrowEngine.DefaultAccept"/>。</summary>
        [Parameter]
        public string Accept { get; set; }

        /// <summary>单个文件的字节上限。</summary>
        [Parameter]
        public long MaxFileSize { get; set; } = 20L * 1024 * 1024;

        /// <summary>
        /// 外部预览服务地址模板，支持 <c>{url}</c> / <c>{name}</c> / <c>{ext}</c> / <c>{kind}</c>。
        /// 内建渲染器不支持的类型（Word / Excel / PPT / OFD / CAD / 3D）配了它才能预览。
        /// </summary>
        [Parameter]
        public string ExternalViewerUrl { get; set; }

        /// <summary>即使内建渲染器支持，也优先走外部预览服务。</summary>
        [Parameter]
        public bool PreferExternalViewer { get; set; }

        /// <summary>
        /// 字节抓取钩子：文档只有地址、而该类型又需要字节才能渲染时（典型是按 URL 打开 OFD）调用它。
        /// 组件不做任何网络请求 —— 下载由宿主实现，组件只负责拿到字节后交给已注册的渲染器。
        /// 未设置时这类文档依旧落到「不支持」。
        /// </summary>
        [Parameter]
        public Func<ElViewerDocument, Task<byte[]>> ContentLoader { get; set; }

        #endregion

        #region 参数：模板与事件

        /// <summary>自定义正文模板（宿主自己实现渲染器时使用）。</summary>
        [Parameter]
        public RenderFragment<ElViewerDocument> ContentTemplate { get; set; }

        /// <summary>空态模板。</summary>
        [Parameter]
        public RenderFragment EmptyTemplate { get; set; }

        /// <summary>「不支持」状态模板。</summary>
        [Parameter]
        public RenderFragment<ElViewerDocument> UnsupportedTemplate { get; set; }

        /// <summary>文档变化事件。</summary>
        [Parameter]
        public EventCallback<ElViewerDocumentEventArgs> OnDocumentChanged { get; set; }

        /// <summary>状态变化事件。</summary>
        [Parameter]
        public EventCallback<ElViewerStateChangedEventArgs> OnStatusChanged { get; set; }

        /// <summary>出错事件。</summary>
        [Parameter]
        public EventCallback<ElViewerErrorEventArgs> OnError { get; set; }

        #endregion

        #region 运行时状态

        private ElViewerDocument current;
        private ElViewerStatus status = ElViewerStatus.Empty;
        private string errorMessage;
        private string urlInput = string.Empty;
        private bool urlBarVisible;

        private bool contentLoading;
        private bool pendingContentLoad;

        /// <summary>
        /// 打开文档时的加载门控。打开动作在同一个同步调用里就会把状态算成 Ready，
        /// 于是「加载中」这一态从来没有机会被渲染出来；门控把它撑住，让 spinner 真正画一帧。
        /// <para>
        /// 只有 <see cref="MinLoadingMilliseconds"/> &gt; 0 时才启用；为 0 时整套门控退化成
        /// 同步路径（状态直接算成终态），单元测试与不需要加载反馈的宿主因此不受影响。
        /// </para>
        /// </summary>
        private bool loadGate;

        /// <summary>
        /// 当前门控是否由命令式 <see cref="LoadAsync"/> 举起并收尾。
        /// <para>
        /// 门控只有 <see cref="loadGate"/> 一个字段，但有两路驱动都往它上面收尾：参数/渲染路在
        /// <see cref="OnAfterRenderAsync"/> 里补时长，命令式路在 <see cref="LoadAsync"/> 末尾自己补。
        /// 二者共用时，渲染路的收尾可能抢在命令式把 <c>current</c> 赋上之前把门放下
        /// （状态当场算成 Empty，终态一拍被踩掉），超额订阅负载下实测必现 —— 见批次㉒ #115。
        /// 命令式路举门时置真，渲染路据此不再插手；门落下时清回假。
        /// </para>
        /// </summary>
        private bool loadGateImperative;

        private long loadGateStarted;

        private ElViewerStatus loadGatePrevious;

        private ElViewerDocument lastDocument;
        private string lastSrc;
        private string lastFileName;
        private string lastContentType;
        private byte[] lastContent;
        private string lastText;

        private ElementReference ofdHost;

        /// <summary>组件根元素：内容打印时要凭它到页面里找到自己的画布。</summary>
        private ElementReference rootElement;

        /// <summary>内建 Office 解析支持的扩展名（只列 ZIP 包裹的 OOXML 三种）。</summary>
        private static readonly string[] OfficeExtensions = { ".docx", ".xlsx", ".pptx" };
        private bool ofdBridgeKnown;
        private bool ofdBridgeAvailable;
        private bool ofdMounting;
        private bool ofdMounted;
        private bool ofdFailed;
        private string ofdErrorMessage;
        private int ofdPageCount;
        private ElViewerDocument ofdAttemptedDocument;
        private bool ofdAttemptedMaximized;
        private ElViewerAnnotationInfo annotation;
        private bool annotationVisible;

        #endregion

        #region 公开只读状态

        /// <summary>当前状态。</summary>
        public ElViewerStatus Status
        {
            get { return status; }
        }

        /// <summary>当前文档。</summary>
        public ElViewerDocument CurrentDocument
        {
            get { return current; }
        }

        /// <summary>当前错误信息。</summary>
        public string ErrorMessage
        {
            get { return errorMessage; }
        }

        /// <summary>实际生效的工具栏。</summary>
        public ElViewerTool[] ResolvedTools
        {
            get
            {
                var tools = Toolbar != null && Toolbar.Length > 0 ? Toolbar : ElViewerGrowToolbar.Default;

                if (HiddenTools != null && HiddenTools.Length > 0)
                {
                    tools = ElViewerGrowToolbar.Without(tools, HiddenTools);
                }

                if (!AllowFileOpen)
                {
                    tools = ElViewerGrowToolbar.Without(tools, ElViewerToolbarAction.OpenFile);
                }

                // 页数未知（既没有宿主给的值，通道也还没解析出来）时不给翻页按钮。
                if (EffectivePageCount <= 0)
                {
                    tools = ElViewerGrowToolbar.Without(tools, ElViewerToolbarAction.PrevPage, ElViewerToolbarAction.NextPage);
                }

                return tools;
            }
        }

        #endregion

        #region 派生属性

        /// <summary>实际使用的文案。</summary>
        public ElViewerGrowStrings ResolvedStrings
        {
            get { return Strings ?? ElViewerGrowStrings.Create(Locale); }
        }

        /// <summary>文件选择框的 accept。</summary>
        public string ResolvedAccept
        {
            get
            {
                var accept = string.IsNullOrWhiteSpace(Accept) ? ElViewerGrowEngine.DefaultAccept : Accept;

                // 只有真的能打开 OFD 时，文件选择框才应该能选到 .ofd：
                // 要么宿主注入了自己的渲染器，要么用组件内置的 OFD 通道；
                // 两者都没有时列出来等于骗用户（选了也是「不支持」）。
                if ((ElViewerGrowRenderer.HasRenderer(ElViewerDocumentKind.Ofd) || EnableOfdPreview)
                    && accept != null
                    && accept.IndexOf(".ofd", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    accept = accept + ",.ofd";
                }

                // Office 同理：开了内建解析就应该能选到 .docx / .xlsx / .pptx
                //（旧版二进制 .doc/.xls/.ppt 列了也打不开，所以不列）。
                if (EnableOfficePreview && accept != null)
                {
                    foreach (var extension in OfficeExtensions)
                    {
                        if (accept.IndexOf(extension, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            accept = accept + "," + extension;
                        }
                    }
                }

                return accept;
            }
        }

        /// <summary>外部预览地址；无法拼出时为 <c>null</c>。</summary>
        public string ExternalUrl
        {
            get { return ElViewerGrowEngine.BuildExternalUrl(ExternalViewerUrl, current); }
        }

        /// <summary>
        /// 当前文档是否应该走内置的 OFD 渲染通道。
        /// <para>
        /// 宿主自己注册了 OFD 渲染器时让位给宿主（显式注入优先），配了外链预览时也走外链。
        /// 只有「要打开 OFD、开了开关、没有别的渲染途径、而且手上确实有字节或地址」才为真。
        /// </para>
        /// </summary>
        public bool UseBuiltInOfd
        {
            get
            {
                return EnableOfdPreview
                    && current != null
                    && current.Kind == ElViewerDocumentKind.Ofd
                    && current.IsSafe
                    && ContentTemplate == null
                    && !ElViewerGrowRenderer.HasRenderer(ElViewerDocumentKind.Ofd)
                    && (current.HasContent || !string.IsNullOrWhiteSpace(current.SourceUrl))
                    && !UsesEmbed;
            }
        }

        /// <summary>
        /// 内建 OOXML 解析是否适用于当前文档。
        /// <para>
        /// 与 <see cref="UseBuiltInOfd"/> 的区别：OFD 通道是「让 JS 去挂载」，所以这里额外要求
        /// 有字节或地址；而 Office 解析必须在 C# 侧拿到字节，因此只要求「开关开着 + 类型匹配 + 没被模板接管」，
        /// 「有没有字节」由 <see cref="ElViewerDocument.RenderedOfficeHtml"/> 自己判定。
        /// </para>
        /// </summary>
        public bool AllowsBuiltInOffice
        {
            get
            {
                return EnableOfficePreview
                    && current != null
                    && ElViewerGrowOffice.IsOfficeKind(current.Kind)
                    && current.IsSafe
                    && ContentTemplate == null
                    && !UsesEmbed;
            }
        }

        /// <summary>
        /// 当前文档是不是「旧版二进制 Office 格式」（<c>.doc</c> / <c>.xls</c> / <c>.ppt</c>，
        /// 即 OLE 复合文档）。用来给出「另存为新格式」这条可执行的提示，而不是含糊的「不支持」。
        /// </summary>
        public bool IsLegacyOfficeDocument
        {
            get
            {
                return current != null
                    && ElViewerGrowOffice.IsOfficeKind(current.Kind)
                    && current.HasContent
                    && ElViewerGrowOoxml.IsOleCompound(current.Content);
            }
        }

        /// <summary>内置 OFD 通道当前是否已经挂上内容。</summary>
        public bool IsOfdMounted
        {
            get { return ofdMounted; }
        }

        /// <summary>内置 OFD 通道解析出的页数；未挂载时为 0。</summary>
        public int OfdPageCount
        {
            get { return ofdPageCount; }
        }

        /// <summary>
        /// 生效页数：宿主显式给的 <see cref="PageCount"/> 优先；没给就用内置通道算出来的页数。
        /// <para>
        /// 翻页可用性、页码文本、事件回传全部走这个值，而不是直接读 <see cref="PageCount"/>。
        /// 这样组件永远不需要回写宿主的参数 —— 一旦回写，换文档时就会残留上一份的页数
        /// （例如先开 1 页的文档再开 2 页的文档，页数会卡在 1）。
        /// </para>
        /// </summary>
        public int EffectivePageCount
        {
            get { return PageCount > 0 ? PageCount : ofdPageCount; }
        }

        /// <summary>是否使用 iframe 呈现（PDF 或外部预览服务）。</summary>
        public bool UsesEmbed
        {
            get { return EmbedSourceUrl != null; }
        }

        /// <summary>
        /// iframe 形态实际要加载的地址：外部预览服务优先（宿主显式指定，或内建渲染器接不住这个类型时），
        /// 其次是 PDF 自身的地址；两者都不成立时返回 <c>null</c>，表示走普通的内建 HTML 渲染。
        /// <para>
        /// iframe 标记与打印共用这一份判定 —— 打印时必须拿同一个地址，否则会出现
        /// 「画布里是 PDF、打印却去打了渲染出来的空壳」这类口径分叉。
        /// </para>
        /// </summary>
        public string EmbedSourceUrl
        {
            get
            {
                if (current == null || !current.IsSafe || ContentTemplate != null)
                {
                    return null;
                }

                var external = ExternalUrl;

                if (!string.IsNullOrEmpty(external)
                    && (PreferExternalViewer || !ElViewerGrowRenderer.CanRender(current, EnableOfficePreview)))
                {
                    return external;
                }

                return current.Kind == ElViewerDocumentKind.Pdf && !string.IsNullOrEmpty(current.SourceUrl)
                    ? current.SourceUrl
                    : null;
            }
        }

        /// <summary>正文 HTML（纯 C# 产出）。</summary>
        public string ContentHtml
        {
            get
            {
                return current == null
                    ? string.Empty
                    : ElViewerGrowRenderer.RenderContent(current, EnableOfficePreview);
            }
        }

        /// <summary>文档标题。</summary>
        public string DocumentTitle
        {
            get
            {
                if (current == null)
                {
                    return ResolvedStrings.IdleText;
                }

                return string.IsNullOrWhiteSpace(current.FileName) ? ResolvedStrings.UnknownFileNameText : current.FileName;
            }
        }

        /// <summary>文档元信息（类型 + 体积）。</summary>
        public string MetaText
        {
            get
            {
                if (current == null)
                {
                    return string.Empty;
                }

                var kind = ElViewerGrowEngine.GetKindDisplayName(current.Kind);
                var size = ElViewerGrowEngine.FormatSize(current.Size);

                return size == null ? kind : kind + " · " + size;
            }
        }

        /// <summary>状态提示文案（错误 / 不支持时才有内容）。</summary>
        public string MessageText
        {
            get
            {
                if (status == ElViewerStatus.Error)
                {
                    return errorMessage ?? ResolvedStrings.ErrorText;
                }

                if (status == ElViewerStatus.Unsupported)
                {
                    return IsLegacyOfficeDocument
                        ? ResolvedStrings.LegacyOfficeText
                        : ResolvedStrings.UnsupportedText;
                }

                return string.Empty;
            }
        }

        /// <summary>缩放百分比文本。</summary>
        public string ZoomText
        {
            get { return ElViewerGrowEngine.FormatZoom(Zoom); }
        }

        /// <summary>页码文本。</summary>
        public string PageLabel
        {
            get { return ElViewerGrowEngine.BuildPageLabel(PageNumber, EffectivePageCount); }
        }

        /// <summary>下载链接；不可下载时为 <c>null</c>。</summary>
        public string DownloadUrl
        {
            get
            {
                if (current == null || !current.IsSafe)
                {
                    return null;
                }

                return string.IsNullOrWhiteSpace(current.Url) ? current.SourceUrl : current.Url;
            }
        }

        /// <summary>下载时使用的文件名。</summary>
        public string DownloadName
        {
            get
            {
                if (current == null)
                {
                    return ResolvedStrings.UnknownFileNameText;
                }

                return string.IsNullOrWhiteSpace(current.FileName) ? ResolvedStrings.UnknownFileNameText : current.FileName;
            }
        }

        /// <summary>新标签页打开的地址；仅 URL 来源时可用。</summary>
        public string NewTabUrl
        {
            get
            {
                if (current == null || !current.IsSafe || string.IsNullOrWhiteSpace(current.Url))
                {
                    return null;
                }

                return current.Url;
            }
        }

        /// <summary>根元素 class。</summary>
        public string RootClass
        {
            get
            {
                var builder = new System.Text.StringBuilder("el-viewer-grow");

                if (Theme == ElViewerTheme.Dark)
                {
                    builder.Append(" el-viewer-grow--dark");
                }

                if (Bordered)
                {
                    builder.Append(" is-bordered");
                }

                if (Maximized)
                {
                    builder.Append(" is-maximized");
                }

                if (!string.IsNullOrWhiteSpace(Cls))
                {
                    builder.Append(' ').Append(Cls);
                }

                return builder.ToString();
            }
        }

        /// <summary>根元素 style。</summary>
        public string RootStyle
        {
            get
            {
                var builder = new System.Text.StringBuilder();

                // 宽高走 CSS 变量而不是内联 width/height：内联样式在打印时无法被
                // @media print 覆盖（除非用 !important，而本仓库禁用它），
                // 结果就是固定高度把长文档裁成第一屏。写成变量后样式表可以在打印态把它改成 auto。
                if (!string.IsNullOrWhiteSpace(Width))
                {
                    builder.Append("--el-viewer-grow-w:").Append(Width).Append(';');
                }

                if (!string.IsNullOrWhiteSpace(Height))
                {
                    builder.Append("--el-viewer-grow-h:").Append(Height).Append(';');
                }

                if (!string.IsNullOrWhiteSpace(Style))
                {
                    builder.Append(Style);
                }

                return builder.ToString();
            }
        }

        /// <summary>画布 style（缩放与旋转）。</summary>
        public string CanvasStyle
        {
            get
            {
                var scale = double.IsNaN(Zoom) ? 1.0 : Zoom;
                var angle = ElViewerGrowEngine.NormalizeRotation(Rotation);
                var zoomed = Math.Abs(scale - 1.0) >= 0.0001;

                // 缩放的滚动条问题：transform 不参与布局 —— 画布视觉上放大了，
                // 溢出部分却落在视口滚动区之外（中心原点还会把一半溢出甩到左上方向，
                // 那边永远滚不到）。修法分两种形态：
                //   - iframe 形态（PDF）：不用 transform，直接给画布真实宽度，
                //     浏览器 PDF 查看器按盒宽渲染，清晰且天然可滚；
                //   - 内建渲染形态：transform 照旧（文本缩放后仍可选可打），
                //     但缩放且未旋转时把原点定到左上角 —— 溢出全部落在右 / 下方向，
                //     视口的滚动条就能到达每一处；旋转仍用中心原点（左上原点会把
                //     内容甩出视野左上方），先旋转后缩放的组合场景保持原行为。
                if (UsesEmbed)
                {
                    if (zoomed && scale > 1.0)
                    {
                        // 宽高一起给：只给宽度的话，布局盒高度仍是视口 100%，
                        // 外层永远不出纵向滚动条 —— 放大之后下方就是滚不到
                        // （iframe 里的 PDF 插件有自己的滚动，但外层盒子没长高，
                        // 插件的可视高度也不会跟着变大）。
                        var embedScale = scale.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

                        return "width:calc(100% * " + embedScale + ");"
                            + "height:calc(100% * " + embedScale + ");";
                    }

                    if (angle != 0)
                    {
                        return "--el-viewer-grow-transform:" + ElViewerGrowEngine.BuildTransform(1.0, Rotation) + ";";
                    }

                    return string.Empty;
                }

                var transform = ElViewerGrowEngine.BuildTransform(Zoom, Rotation);

                if (string.IsNullOrEmpty(transform))
                {
                    return string.Empty;
                }

                var style = "--el-viewer-grow-transform:" + transform + ";";

                if (zoomed && scale > 1.0 && angle == 0)
                {
                    style += "transform-origin:0 0;";
                }

                return style;
            }
        }

        /// <summary>URL 输入条是否展开。</summary>
        public bool UrlBarVisible
        {
            get { return urlBarVisible; }
        }

        /// <summary>URL 输入框的当前值。</summary>
        public string UrlInput
        {
            get { return urlInput; }
            set { urlInput = value; }
        }

        #endregion

        #region 生命周期

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (AutoLoad)
            {
                current = BuildFromParameters();

                // 首次打开（含宿主用 @key 重建组件实例的场景）同样要给加载框：
                // 这条路径上 SourceChanged() 不会再触发，门控必须在这里举起来，
                // 否则第一帧就是终态。门控统一在 OnAfterRenderAsync 里放下。
                ArmLoadGate(status);
            }

            MarkSourceAsSeen();
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (SourceChanged())
            {
                current = BuildFromParameters();
                MarkSourceAsSeen();

                // 换了文档就该重新给内置 OFD 通道一次机会：否则上一份的失败状态会
                // 粘在新文档上，让它一进来就是「错误」。
                ofdFailed = false;
                ofdErrorMessage = null;

                // 参数驱动的换文档也是同步算状态的：把门控举起来，让第一帧是「加载中」；
                // 门控在 OnAfterRenderAsync 里（取流 / 解析 / 挂载都完成之后）放下。
                ArmLoadGate(status);
            }

            status = ResolveStatus();

            // 参数驱动的加载没有异常可抛，这里把「地址不合法」落成明确的错误文案，
            // 否则状态是 Error 而提示却是泛泛的「加载失败」。
            errorMessage = status == ElViewerStatus.Error
                ? (errorMessage ?? ResolvedStrings.UnsafeUrlText)
                : null;

            // 参数变化是同步的，抓取字节只能放到渲染之后再做一轮。
            pendingContentLoad = NeedsContentLoad();
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (pendingContentLoad)
            {
                pendingContentLoad = false;
                await EnsureContentAsync();
            }

            await EnsureOfdMountedAsync();

            // 参数驱动的加载在这里收尾：重活（取流 / 解析 / 挂载）都已完成，
            // 补足最短显示时长后回到真实状态。门控没举起时这里是空操作。
            // 命令式 LoadAsync 举的门不归这里收尾（#115）：渲染路可能抢在命令式把
            // current 赋上之前把门放下，终态当场算成 Empty 把命令式的 Ready 一拍踩掉。
            if (!loadGateImperative)
            {
                await EndLoadGateAsync();
            }
        }

        /// <summary>当前文档是否必须先抓字节才能呈现（有地址、无字节、且该类型有渲染途径）。</summary>
        private bool NeedsContentLoad()
        {
            if (current == null
                || current.HasContent
                || ContentLoader == null
                || string.IsNullOrWhiteSpace(current.Url)
                || !current.IsSafe)
            {
                return false;
            }

            // 注入的渲染器要字节；内置 OFD 通道自己也取流，但宿主给了 ContentLoader 时
            // 优先让宿主取（带上自己的鉴权头），组件只把字节转交给通道。
            // 内建 Office 解析同样只在 C# 侧有字节时才成立，所以也走这条取流路径。
            return ElViewerGrowRenderer.HasRenderer(current.Kind)
                || UseBuiltInOfd
                || AllowsBuiltInOffice;
        }

        /// <summary>
        /// 用 <see cref="ContentLoader"/> 把远程文档抓成字节，再交给已注册的渲染器。
        /// 失败不静默：走与运行时异常同一条错误通道。
        /// </summary>
        private async Task EnsureContentAsync()
        {
            if (!NeedsContentLoad())
            {
                return;
            }

            var document = current;
            var previous = status;

            contentLoading = true;
            status = ElViewerStatus.Loading;
            await InvokeAsync(StateHasChanged);

            try
            {
                var bytes = await ContentLoader(document);

                if (!ReferenceEquals(current, document))
                {
                    return;
                }

                if (bytes != null && bytes.Length > 0)
                {
                    var resolved = document.Clone();
                    resolved.Content = bytes;
                    current = resolved.Resolve();
                }
                else
                {
                    await FailAsync(ResolvedStrings.ErrorText, null);
                    return;
                }
            }
            catch (Exception exception)
            {
                if (ReferenceEquals(current, document))
                {
                    await FailAsync(exception.Message, exception);
                    return;
                }
            }
            finally
            {
                contentLoading = false;
            }

            status = ResolveStatus();
            await InvokeAsync(StateHasChanged);

            // 门控举起期间终态由 EndLoadGateAsync 统一上报（它还要补最短显示时长），
            // 这里只把状态算好、画一帧，不重复发事件。
            if (!loadGate)
            {
                await RaiseStateChangedAsync(previous);
            }
        }

        /// <summary>
        /// 打开加载门控：让「加载中」这一态真的有机会被画出来。
        /// <para>
        /// <see cref="MinLoadingMilliseconds"/> &lt;= 0 时返回 <c>false</c>，调用方走原来的同步路径，
        /// 状态直接算成终态 —— 这样单元测试里「调用完立刻断言 Ready」的既有契约不受影响。
        /// </para>
        /// </summary>
        private bool TryBeginLoadGate()
        {
            if (MinLoadingMilliseconds <= 0)
            {
                return false;
            }

            loadGate = true;
            loadGateImperative = true;
            loadGateStarted = Environment.TickCount64;

            // 已经处于 Loading（如首帧参数驱动的门控窗口还在计时）时不能把「加载前状态」
            // 抓成 Loading：否则终态上报的 PreviousStatus 全被污染成 Loading（#115 实测）。
            if (status != ElViewerStatus.Loading)
            {
                loadGatePrevious = status;
            }

            status = ElViewerStatus.Loading;

            // 渲染请求派发到调度器：LoadAsync 在 Task.Yield 之后继续跑，续体可能已经
            // 不在调度器线程上（bUnit / 服务端都会 assert 这一点）。
            _ = InvokeAsync(StateHasChanged);
            return true;
        }

        /// <summary>参数驱动换文档时同样是同步的，只在参数阶段把门控「举起」，等渲染后再放下。</summary>
        private void ArmLoadGate(ElViewerStatus previous)
        {
            if (MinLoadingMilliseconds <= 0)
            {
                return;
            }

            loadGate = true;
            loadGateImperative = false;
            loadGateStarted = Environment.TickCount64;
            loadGatePrevious = previous;
        }

        /// <summary>
        /// 结束门控：补足最短显示时长，回到真实状态，画一帧并通知宿主。
        /// 只补差额 —— 真正耗时的文档（大 PDF / 大 OOXML）不会被额外拖慢。
        /// </summary>
        private async Task EndLoadGateAsync()
        {
            if (!loadGate)
            {
                return;
            }

            var elapsed = Environment.TickCount64 - loadGateStarted;
            var remaining = MinLoadingMilliseconds - (int)elapsed;

            if (remaining > 0)
            {
                await Task.Delay(remaining);
            }

            loadGate = false;
            loadGateImperative = false;
            status = ResolveStatus();
            await InvokeAsync(StateHasChanged);
            await RaiseStateChangedAsync(loadGatePrevious);
        }

        /// <summary>
        /// 把内建 OOXML 解析提前到门控内跑一遍。
        /// 解析是惰性的（第一次读 <see cref="ElViewerDocument.RenderedOfficeHtml"/> 时才解压整个 ZIP），
        /// 不在门控里预热的话，这段耗时会落在「内容渲染」那一帧上 ——
        /// 加载框还没来得及画就已经结束了，用户看到的仍是一次卡顿。
        /// </summary>
        private void PrewarmHeavyRender()
        {
            var document = current;

            if (document != null
                && document.HasContent
                && ElViewerGrowOffice.IsOfficeKind(document.Kind))
            {
                // 结果由 ElViewerDocument 自己缓存，渲染阶段再读不会重算。
                var warm = document.RenderedOfficeHtml;
            }
        }

        #endregion

        #region 内置 OFD 通道

        /// <summary>
        /// 把当前 OFD 文档挂到画布上。只在「该文档 + 该最大化状态」上尝试一次：
        /// 挂载是异步的（首次还要下载约 0.9 MB 的渲染核心），重复触发只会得到重复的页面。
        /// 最大化会改变可用宽度，所以换一种尺寸就重挂一次，让页面按新宽度重排。
        /// </summary>
        private async Task EnsureOfdMountedAsync()
        {
            if (ofdMounting)
            {
                return;
            }

            if (!UseBuiltInOfd)
            {
                if (ofdMounted)
                {
                    await DisposeOfdAsync();
                }

                return;
            }

            if ((ofdMounted || ofdFailed)
                && ReferenceEquals(ofdAttemptedDocument, current)
                && ofdAttemptedMaximized == Maximized)
            {
                return;
            }

            // 挂载点（<div class="el-viewer-grow__ofd">）只存在于「画布」分支里：上一帧若落在
            // Error / Unsupported，这一帧文档里根本没有它，ofdHost 还指向随那一帧被移除的元素，
            // 交给 JS 只会收到 null —— 于是报错变成一条莫名其妙的
            // "elementOfdViewer: container is null"（#166 实测：先传一份解析失败的 .ofd，
            // 紧接着传正常文档，第二份当场得这条错误，而且静置 45 秒也不自愈）。
            // 这一轮先不挂：清掉「这一份渲染失败」的粘滞状态，让渲染把挂载点画回来，
            // 下一帧的 OnAfterRenderAsync 会自然走到这里重试。
            if (status == ElViewerStatus.Error || status == ElViewerStatus.Unsupported)
            {
                // 通道本身取不到（宿主没引 dom.js / 核心脚本拿不到）时 ofdErrorMessage 是空的：
                // 那种情况下「不支持」就是终态，重试只会渲染—挂载地空转，不清。
                if (ofdFailed && string.IsNullOrEmpty(ofdErrorMessage))
                {
                    return;
                }

                ofdFailed = false;
                ofdErrorMessage = null;
                status = ResolveStatus();
                StateHasChanged();

                return;
            }

            var document = current;
            var previous = status;

            ofdAttemptedDocument = document;
            ofdAttemptedMaximized = Maximized;
            ofdMounting = true;
            ofdFailed = false;
            StateHasChanged();

            try
            {
                await MountOfdCoreAsync(document);
            }
            catch (Exception exception)
            {
                ofdFailed = true;
                ofdMounted = false;

                // 通道在，但这一份文档没渲染出来 —— 这是真的出错，不能像「通道不存在」
                // 那样静默退回「不支持」，否则宿主的错误监控什么都看不到。
                ofdErrorMessage = exception.Message;
                errorMessage = exception.Message;

                await OnError.InvokeAsync(new ElViewerErrorEventArgs
                {
                    Message = exception.Message,
                    Exception = exception,
                    Document = current
                });
            }
            finally
            {
                ofdMounting = false;
            }

            // 收尾必须无条件执行。早期实现把这几行写在同一个 try 里，而 try 内的
            // return 会直接跳出整个方法 —— 结果「桥接不可用」时状态停在上一轮的
            // Ready：画布空白，状态栏却报「就绪」，那条兜底分支等于没生效。
            status = ResolveStatus();
            StateHasChanged();

            // 门控举起期间终态由 EndLoadGateAsync 统一上报，这里不发重复事件。
            if (!loadGate)
            {
                await RaiseStateChangedAsync(previous);
            }
        }

        /// <summary>
        /// 真正把文档交给通道。<b>本方法的 return 是安全的</b>：它只结束自己，
        /// 调用方的收尾（重算状态 + 通知）照常执行。
        /// </summary>
        private async Task MountOfdCoreAsync(ElViewerDocument document)
        {
            if (JSRuntime == null)
            {
                ofdFailed = true;
                return;
            }

            if (!ofdBridgeKnown)
            {
                ofdBridgeAvailable = await JSRuntime.InvokeAsync<bool>("elementOfdViewerAvailable");
                ofdBridgeKnown = true;
            }

            if (!ofdBridgeAvailable)
            {
                // 宿主没引 dom.js：不是故障，退回「不支持」即可。
                ofdFailed = true;
                return;
            }

            var count = await JSRuntime.InvokeAsync<int>("elementOfdViewerMount", ofdHost, BuildOfdPayload(document));

            if (!ReferenceEquals(current, document))
            {
                // 挂载期间换了文档：这次结果作废，下一轮渲染会按新文档重来。
                return;
            }

            ofdMounted = true;
            ofdPageCount = count;

            // 页数是渲染核心算出来的，只记在自己的字段里（见 EffectivePageCount）。
            // 不回写宿主的 PageCount 参数：回写会粘住上一份文档的页数。
            if (count > 0 && PageNumber > count)
            {
                // 换到更短的文档时把页码收回来，免得状态栏出现「3 / 2」这种页码。
                PageNumber = count;
                await PageNumberChanged.InvokeAsync(count);
            }
        }

        /// <summary>内置通道的挂载参数（JSON 字符串）。字节与地址二选一，有字节时优先字节。</summary>
        private string BuildOfdPayload(ElViewerDocument document)
        {
            var builder = new StringBuilder();

            builder.Append('{');

            if (document.HasContent)
            {
                builder.Append("\"base64\":\"").Append(Convert.ToBase64String(document.Content)).Append('"');
            }
            else
            {
                builder.Append("\"url\":").Append(QuoteJson(document.SourceUrl));
            }

            builder.Append(",\"fileName\":").Append(QuoteJson(document.FileName ?? string.Empty));
            builder.Append(",\"maxWidth\":900");
            builder.Append('}');

            return builder.ToString();
        }

        /// <summary>最小的 JSON 字符串字面量转义（只用于把文件名 / 地址塞进桥接参数）。</summary>
        private static string QuoteJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            var builder = new StringBuilder(value.Length + 2);

            builder.Append('"');

            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (ch < ' ')
                        {
                            builder.Append("\\u").Append(((int)ch).ToString("x4"));
                        }
                        else
                        {
                            builder.Append(ch);
                        }

                        break;
                }
            }

            builder.Append('"');

            return builder.ToString();
        }

        /// <summary>释放画布上的 OFD 挂载状态。</summary>
        private async Task DisposeOfdAsync()
        {
            var host = ofdHost;

            ofdMounted = false;
            ofdPageCount = 0;
            ofdAttemptedDocument = null;
            annotation = null;
            annotationVisible = false;

            if (JSRuntime == null)
            {
                return;
            }

            try
            {
                await JSRuntime.InvokeVoidAsync("elementOfdViewerDispose", host);
            }
            catch
            {
                // 元素可能已经跟随上一轮渲染被移除；JS 侧对空容器也会自己兜底。
            }
        }

        /// <summary>把画布滚到指定页（0 起）。只在内置 OFD 通道挂载成功后才真的调用。</summary>
        private async Task ScrollOfdToPageAsync(int pageIndex)
        {
            if (!ofdMounted || JSRuntime == null)
            {
                return;
            }

            try
            {
                await JSRuntime.InvokeVoidAsync("elementOfdViewerScrollToPage", ofdHost, pageIndex);
            }
            catch
            {
                // 滚动失败不影响翻页状态本身。
            }
        }

        /// <summary>
        /// OFD 角标由 dom.js 通道画进页面，Blazor 没法直接给它挂 <c>@onclick</c>；
        /// 但点击会冒泡到宿主，这里拿视口坐标问通道「命中了哪条注释」，命中才开卡片，
        /// 未命中（通道回 null）就是页面上的普通点击，不产生任何额外渲染。
        /// </summary>
        private async Task OnOfdHostClickAsync(MouseEventArgs args)
        {
            if (JSRuntime == null || !ofdMounted)
            {
                return;
            }

            string json;

            try
            {
                json = await JSRuntime.InvokeAsync<string>("elementOfdViewerAnnotationAt", args.ClientX, args.ClientY);
            }
            catch
            {
                // 通道不在（宿主没引 dom.js）时点击只当没发生。
                return;
            }

            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            try
            {
                using var payload = System.Text.Json.JsonDocument.Parse(json);
                var root = payload.RootElement;

                annotation = new ElViewerAnnotationInfo
                {
                    Content = ReadJsonString(root, "remark"),
                    Author = ReadJsonString(root, "creator"),
                    Time = ReadJsonString(root, "time"),
                    Type = ReadJsonString(root, "type"),
                    Id = ReadJsonString(root, "id")
                };
            }
            catch (System.Text.Json.JsonException)
            {
                return;
            }

            annotationVisible = true;
            StateHasChanged();
        }

        private static string ReadJsonString(System.Text.Json.JsonElement root, string name)
        {
            return root.TryGetProperty(name, out var value) && value.ValueKind == System.Text.Json.JsonValueKind.String
                ? value.GetString()
                : string.Empty;
        }

        /// <summary>关闭注释卡片（关闭按钮与遮罩点击都走这里）。</summary>
        private void CloseAnnotation()
        {
            if (!annotationVisible)
            {
                return;
            }

            annotationVisible = false;
            StateHasChanged();
        }

        #endregion

        #region 加载

        /// <summary>加载一个文档描述。</summary>
        public async Task LoadAsync(ElViewerDocument document)
        {
            var previous = status;
            var gated = TryBeginLoadGate();

            if (gated)
            {
                // 入口同步发一拍（#115 契约）：在任何 await 交出线程之前，宿主已经收到
                // 「加载中」。此前这一拍排在门控收尾之后的续体里，超额订阅负载下续体排队
                // 超过门控窗口、门已落下时状态早不是 Loading，宿主就永远看不到 Loading。
                await RaiseStateChangedAsync(previous);

                // 交出控制权一拍，让「加载中」那一帧真的上屏；随后的重活（解压解析）
                // 全部落在这个加载框的遮盖之下，而不是塞进用户看见卡顿的那一帧。
                await Task.Yield();
            }

            var previousDocument = current;

            current = document == null ? null : document.Resolve();

            // 注释卡片指向的是上一份页面的角标，加载后它要么消失要么重画，先关掉（批次101）。
            annotation = null;
            annotationVisible = false;

            // 换文档就是给内置 OFD 通道一次新的机会，与 OnParametersSet 里同一口径。
            // 上一份的失败态若留着，ResolveStatus 会把新文档一进门就算成 Error，
            // 而紧跟其后的「Error 即地址不合法」分支又把这句错误文案安到新文档头上
            // （#166 实测：先传一份解析失败的 .ofd、再传正常文档，新文档当场被报成
            // 「地址不被允许」；而且错误分支不画挂载点，通道只会收到一个 null 容器）。
            if (!ReferenceEquals(current, previousDocument))
            {
                ofdFailed = false;
                ofdErrorMessage = null;
            }

            errorMessage = null;
            status = ResolveStatus();

            // 地址不合法时不静默吞掉：走与运行时异常同一条错误通道，宿主能收到 OnError。
            if (status == ElViewerStatus.Error)
            {
                await FailAsync(ResolvedStrings.UnsafeUrlText, null);

                if (gated)
                {
                    await EndLoadGateAsync();
                }

                return;
            }

            if (gated)
            {
                // 门控内的同步解析（大 OOXML 的解压耗时）提前热身，结果由文档缓存。
                // 「进入加载」那一拍已在入口同步发出（#115），这里不再重复。
                PrewarmHeavyRender();
            }

            await EnsureContentAsync();

            await DocumentChanged.InvokeAsync(current);
            await OnDocumentChanged.InvokeAsync(new ElViewerDocumentEventArgs { Document = current });

            if (gated)
            {
                // 补足最短显示时长、回到真实状态并由它负责上报终态。
                await EndLoadGateAsync();
            }
            else
            {
                await RaiseStateChangedAsync(previous);
            }

            // 换文档会牵动内置 OFD 通道（重新挂载 / 换页数），而通道的重挂是
            // 「渲染之后」的动作。宿主没绑 Document 时不会自己重渲染，这里主动请求一次。
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>按地址加载。</summary>
        public Task LoadUrlAsync(string url, string fileName = null)
        {
            return LoadAsync(string.IsNullOrWhiteSpace(url) ? null : ElViewerDocument.FromUrl(url, fileName));
        }

        /// <summary>按文本加载（自动按 UTF-8 编码）。</summary>
        public Task LoadTextAsync(string text, string fileName = null)
        {
            return LoadAsync(ElViewerDocument.FromText(text ?? string.Empty, string.IsNullOrWhiteSpace(fileName) ? "document.txt" : fileName));
        }

        /// <summary>按字节加载。</summary>
        public Task LoadBytesAsync(byte[] bytes, string fileName = null)
        {
            return LoadAsync(bytes == null ? null : ElViewerDocument.FromBytes(bytes, fileName));
        }

        /// <summary>关闭当前文档，回到空态。</summary>
        public async Task CloseAsync()
        {
            var previous = status;

            current = null;
            errorMessage = null;
            urlInput = string.Empty;
            status = ElViewerStatus.Empty;

            await DocumentChanged.InvokeAsync(null);
            await OnDocumentChanged.InvokeAsync(new ElViewerDocumentEventArgs { Document = null });
            await RaiseStateChangedAsync(previous);

            // 关掉文档也要把内置 OFD 通道一起放掉（通道的挂载跟着渲染走），
            // 否则再打开同一份文档时会因为「已挂载」而被跳过，留下空画布。
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>重新解析当前文档。</summary>
        public async Task ReloadAsync()
        {
            if (current == null)
            {
                return;
            }

            var previous = status;

            current = current.Clone().Resolve();
            errorMessage = null;
            status = ResolveStatus();

            await RaiseStateChangedAsync(previous);
        }

        /// <summary>
        /// 打印当前文档。
        /// <para>
        /// 已渲染的内容（文本 / Markdown / 表格 / 图片 / Office / OFD）不传地址：
        /// 交给组件自己的 <c>@media print</c> 规则把外壳隐掉、把缩放与定高复位，直接打页面即可 ——
        /// 零额外窗口、零脚本。
        /// </para>
        /// <para>
        /// PDF / 外链预览这类 iframe 形态才把源地址交出去，由隐藏 iframe 调它自己的 <c>print()</c>：
        /// 页面级 <c>window.print()</c> 对 iframe 里的浏览器 PDF 查看器并不可靠（多数内核会把
        /// 插件区域打成空白）。跨域取不到 contentWindow 时由桥接回退到新窗口打开。
        /// </para>
        /// </summary>
        public async Task PrintAsync()
        {
            if (current == null || JSRuntime == null)
            {
                return;
            }

            try
            {
                // 根元素交给桥接：内容形态要在页面里找画布序列化打印。
                await JSRuntime.InvokeVoidAsync("elementPrintDocument", UsesEmbed ? EmbedSourceUrl : null, rootElement);
            }
            catch (Exception exception)
            {
                // JS 桥接缺失（宿主没引 dom.js）或浏览器拒绝打印。这既不是加载失败也不是
                // 能力缺失，不该把画布打成错误态，只上报 OnError 让宿主自己去记。
                await OnError.InvokeAsync(new ElViewerErrorEventArgs
                {
                    Message = exception.Message,
                    Exception = exception,
                    Document = current
                });
            }
        }

        /// <summary>提交 URL 输入条里的地址。</summary>
        public async Task SubmitUrlAsync()
        {
            if (string.IsNullOrWhiteSpace(urlInput))
            {
                return;
            }

            var url = urlInput.Trim();

            if (!ElViewerGrowEngine.IsSafeUrl(url))
            {
                await FailAsync(ResolvedStrings.UnsafeUrlText, null);
                return;
            }

            await LoadUrlAsync(url);
        }

        #endregion

        #region 视图命令

        /// <summary>放大一档。</summary>
        public Task ZoomInAsync()
        {
            return SetZoomAsync(ElViewerGrowEngine.StepZoom(Zoom, 1, MinZoom, MaxZoom));
        }

        /// <summary>缩小一档。</summary>
        public Task ZoomOutAsync()
        {
            return SetZoomAsync(ElViewerGrowEngine.StepZoom(Zoom, -1, MinZoom, MaxZoom));
        }

        /// <summary>把缩放设为指定值（会夹紧到 [MinZoom, MaxZoom]）。</summary>
        public async Task SetZoomAsync(double value)
        {
            var next = ElViewerGrowEngine.ClampZoom(value, MinZoom, MaxZoom);

            if (Math.Abs(next - Zoom) < 0.0001)
            {
                return;
            }

            var previous = status;

            Zoom = next;
            await ZoomChanged.InvokeAsync(next);
            await RaiseStateChangedAsync(previous);
        }

        /// <summary>重置缩放。</summary>
        public Task ResetZoomAsync()
        {
            return SetZoomAsync(1.0);
        }

        /// <summary>逆时针旋转 90°。</summary>
        public Task RotateLeftAsync()
        {
            return SetRotationAsync(Rotation - 90);
        }

        /// <summary>顺时针旋转 90°。</summary>
        public Task RotateRightAsync()
        {
            return SetRotationAsync(Rotation + 90);
        }

        /// <summary>设置旋转角度。</summary>
        public async Task SetRotationAsync(int degree)
        {
            var next = ElViewerGrowEngine.NormalizeRotation(degree);

            if (next == ElViewerGrowEngine.NormalizeRotation(Rotation))
            {
                return;
            }

            var previous = status;

            Rotation = next;
            await RotationChanged.InvokeAsync(next);
            await RaiseStateChangedAsync(previous);
        }

        /// <summary>同时重置缩放与旋转。</summary>
        public async Task ResetViewAsync()
        {
            await SetZoomAsync(1.0);
            await SetRotationAsync(0);
        }

        /// <summary>切换最大化。</summary>
        public async Task ToggleMaximizeAsync()
        {
            var previous = status;

            Maximized = !Maximized;
            await MaximizedChanged.InvokeAsync(Maximized);
            await RaiseStateChangedAsync(previous);

            // 最大化会改变可用宽度，内置 OFD 通道要按新宽度重排（重挂跟渲染走）。
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>跳到指定页。</summary>
        public async Task GoToPageAsync(int page)
        {
            var next = ElViewerGrowEngine.ClampPage(page, EffectivePageCount);

            if (next == PageNumber)
            {
                return;
            }

            var previous = status;

            PageNumber = next;
            await PageNumberChanged.InvokeAsync(next);

            // 内置 OFD 通道的页面是纵向排列的，翻页要跟着滚动；
            // 其余类型（PDF iframe / 文本 / 图片）没有可滚动到第 N 页的概念，这里自然跳过。
            await ScrollOfdToPageAsync(next - 1);

            await RaiseStateChangedAsync(previous);
        }

        /// <summary>上一页。</summary>
        public Task PrevPageAsync()
        {
            return GoToPageAsync(PageNumber - 1);
        }

        /// <summary>下一页。</summary>
        public Task NextPageAsync()
        {
            return GoToPageAsync(PageNumber + 1);
        }

        /// <summary>执行一个工具栏动作（供标记层统一分发）。</summary>
        public Task ExecuteAsync(ElViewerToolbarAction action)
        {
            switch (action)
            {
                case ElViewerToolbarAction.OpenUrl:
                    return ToggleUrlBarAsync();
                case ElViewerToolbarAction.ZoomIn:
                    return ZoomInAsync();
                case ElViewerToolbarAction.ZoomOut:
                    return ZoomOutAsync();
                case ElViewerToolbarAction.ZoomReset:
                    return ResetZoomAsync();
                case ElViewerToolbarAction.RotateLeft:
                    return RotateLeftAsync();
                case ElViewerToolbarAction.RotateRight:
                    return RotateRightAsync();
                case ElViewerToolbarAction.Maximize:
                    return ToggleMaximizeAsync();
                case ElViewerToolbarAction.Reload:
                    return ReloadAsync();
                case ElViewerToolbarAction.Print:
                    return PrintAsync();
                case ElViewerToolbarAction.Close:
                    return CloseAsync();
                case ElViewerToolbarAction.PrevPage:
                    return PrevPageAsync();
                case ElViewerToolbarAction.NextPage:
                    return NextPageAsync();
                default:
                    return Task.CompletedTask;
            }
        }

        /// <summary>展开 / 收起 URL 输入条。</summary>
        public async Task ToggleUrlBarAsync()
        {
            urlBarVisible = !urlBarVisible;
            await InvokeAsync(StateHasChanged);
        }

        #endregion

        #region 文件选择

        /// <summary>处理本地文件选择（由 <c>InputFile</c> 触发）。</summary>
        public async Task OnFileChanged(InputFileChangeEventArgs args)
        {
            if (args == null || args.File == null)
            {
                return;
            }

            var file = args.File;
            var limit = MaxFileSize > 0 ? MaxFileSize : long.MaxValue;

            if (file.Size > limit)
            {
                await FailAsync(ResolvedStrings.FileTooLargeText, null);
                return;
            }

            try
            {
                using (var stream = file.OpenReadStream(limit))
                using (var memory = new MemoryStream())
                {
                    await stream.CopyToAsync(memory);
                    await LoadBytesAsync(memory.ToArray(), file.Name);
                }
            }
            catch (Exception ex)
            {
                await FailAsync(ex.Message, ex);
            }
        }

        #endregion

        #region 标记辅助

        /// <summary>取工具的提示文案。</summary>
        public string ResolveToolTitle(ElViewerTool tool)
        {
            if (tool == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(tool.Title))
            {
                return tool.Title;
            }

            if (tool.Action == ElViewerToolbarAction.Maximize && Maximized)
            {
                return ResolvedStrings.RestoreText;
            }

            return ResolvedStrings.Resolve(tool.Action);
        }

        /// <summary>取工具的图标（最大化状态下显示「还原」图标）。</summary>
        public string ResolveToolIcon(ElViewerTool tool)
        {
            if (tool == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(tool.Icon))
            {
                return tool.Icon;
            }

            if (tool.Action == ElViewerToolbarAction.Maximize && Maximized)
            {
                return MinimizeIcon;
            }

            return ElViewerGrowIcons.Get(tool.Action);
        }

        /// <summary>把图标包装成完整的 svg 标记。</summary>
        public string ResolveToolIconMarkup(ElViewerTool tool)
        {
            return ElViewerGrowIcons.Svg(ResolveToolIcon(tool));
        }

        /// <summary>「还原」图标（最大化按钮的第二态）。</summary>
        public static string MinimizeIcon
        {
            get { return "<path d=\"M4 8.5h4.5V4\"/><path d=\"M20 8.5h-4.5V4\"/><path d=\"M4 15.5h4.5V20\"/><path d=\"M20 15.5h-4.5V20\"/>"; }
        }

        /// <summary>工具是否禁用。</summary>
        public bool IsToolDisabled(ElViewerTool tool)
        {
            if (tool == null || tool.IsSeparator)
            {
                return true;
            }

            if (!ElViewerGrowToolbar.RequiresDocument(tool.Action))
            {
                return false;
            }

            if (current == null)
            {
                return true;
            }

            switch (tool.Action)
            {
                case ElViewerToolbarAction.OpenInNewTab:
                    return NewTabUrl == null;
                case ElViewerToolbarAction.Download:
                    return DownloadUrl == null;
                case ElViewerToolbarAction.PrevPage:
                    return EffectivePageCount <= 0 || PageNumber <= 1;
                case ElViewerToolbarAction.NextPage:
                    return EffectivePageCount <= 0 || PageNumber >= EffectivePageCount;
                case ElViewerToolbarAction.ZoomIn:
                    return Zoom >= MaxZoom;
                case ElViewerToolbarAction.ZoomOut:
                    return Zoom <= MinZoom;
                default:
                    return false;
            }
        }

        /// <summary>工具按钮的 class（禁用态与文件按钮各有一种修饰）。</summary>
        public string ToolClass(ElViewerTool tool)
        {
            var builder = new StringBuilder("el-viewer-grow__button");

            if (tool != null && tool.Action == ElViewerToolbarAction.OpenFile)
            {
                builder.Append(" el-viewer-grow__button--file");
            }

            if (IsToolDisabled(tool))
            {
                builder.Append(" is-disabled");
            }

            return builder.ToString();
        }

        /// <summary>画布 class（iframe 形态需要铺满视口）。</summary>
        public string CanvasClass
        {
            get { return UsesEmbed ? "el-viewer-grow__canvas is-embed" : "el-viewer-grow__canvas"; }
        }

        /// <summary>
        /// iframe 形态的标记：优先用外部预览地址，其次用 PDF 的浏览器内建能力。
        /// 由纯 C# 拼出，与 <see cref="ElViewerGrowRenderer"/> 共用同一套转义。
        /// </summary>
        public string EmbedMarkup
        {
            get
            {
                var source = EmbedSourceUrl;

                return string.IsNullOrEmpty(source)
                    ? string.Empty
                    : ElViewerGrowRenderer.RenderEmbed(source, current.Title);
            }
        }

        /// <summary>URL 输入框回车提交。</summary>
        public async Task OnUrlKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs args)
        {
            if (args != null && args.Key == "Enter")
            {
                await SubmitUrlAsync();
            }
        }

        #endregion

        #region 内部

        private ElViewerDocument BuildFromParameters()
        {
            if (Document != null)
            {
                return Document.Resolve();
            }

            if (!string.IsNullOrWhiteSpace(Src))
            {
                return ElViewerDocument.FromUrl(Src, FileName, ContentType);
            }

            if (!string.IsNullOrEmpty(Text))
            {
                return ElViewerDocument.FromText(Text, string.IsNullOrWhiteSpace(FileName) ? "document.txt" : FileName, ContentType);
            }

            if (Content != null && Content.Length > 0)
            {
                return ElViewerDocument.FromBytes(Content, FileName, ContentType);
            }

            return null;
        }

        private bool SourceChanged()
        {
            return !ReferenceEquals(Document, lastDocument)
                || !string.Equals(Src, lastSrc, StringComparison.Ordinal)
                || !string.Equals(FileName, lastFileName, StringComparison.Ordinal)
                || !string.Equals(ContentType, lastContentType, StringComparison.Ordinal)
                || !ReferenceEquals(Content, lastContent)
                || !ReferenceEquals(Text, lastText);
        }

        private void MarkSourceAsSeen()
        {
            lastDocument = Document;
            lastSrc = Src;
            lastFileName = FileName;
            lastContentType = ContentType;
            lastContent = Content;
            lastText = Text;
        }

        private bool CanPresent
        {
            get
            {
                if (current == null)
                {
                    return false;
                }

                if (ContentTemplate != null)
                {
                    return true;
                }

                if (!string.IsNullOrEmpty(ExternalUrl))
                {
                    return true;
                }

                if (ElViewerGrowRenderer.CanRender(current, EnableOfficePreview))
                {
                    return true;
                }

                // 内置 OFD 通道：通道本身取不到（宿主没引 dom.js / 核心脚本 404）时
                // 主动翻回「不支持」，不留下一个空画布。
                return UseBuiltInOfd && !ofdFailed;
            }
        }

        private ElViewerStatus ResolveStatus()
        {
            // 门控举起期间一律报「加载中」：参数驱动的换文档是同步算状态的，
            // 不借 ResolveStatus 的话，第一帧就是终态，加载框永远画不出来。
            if (loadGate || Loading || contentLoading || ofdMounting)
            {
                return ElViewerStatus.Loading;
            }

            if (current == null)
            {
                return ElViewerStatus.Empty;
            }

            if (!current.IsSafe)
            {
                return ElViewerStatus.Error;
            }

            // 内置 OFD 通道存在、但这一份没渲染出来（解析失败 / 核心取不到）：
            // 报错而不是报「不支持」，否则故障会被当成能力缺失而查无实据。
            if (ofdFailed && !string.IsNullOrEmpty(ofdErrorMessage))
            {
                return ElViewerStatus.Error;
            }

            return CanPresent ? ElViewerStatus.Ready : ElViewerStatus.Unsupported;
        }

        private async Task FailAsync(string message, Exception exception)
        {
            var previous = status;

            errorMessage = message;
            status = ElViewerStatus.Error;

            await OnError.InvokeAsync(new ElViewerErrorEventArgs { Message = message, Exception = exception, Document = current });
            await RaiseStateChangedAsync(previous, message);
        }

        private async Task RaiseStateChangedAsync(ElViewerStatus previous, string message = null)
        {
            await OnStatusChanged.InvokeAsync(new ElViewerStateChangedEventArgs
            {
                Status = status,
                PreviousStatus = previous,
                Document = current,
                Zoom = Zoom,
                Rotation = Rotation,
                PageNumber = PageNumber,
                PageCount = EffectivePageCount,
                Message = message ?? MessageText
            });
        }

        #endregion
    }
}
