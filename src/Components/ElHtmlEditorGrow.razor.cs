using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ElementPlus
{
    /// <summary>
    /// ElHtmlEditorGrow —— 富文本（HTML）编辑器，Element Plus 自渲染版。
    /// <para>
    /// 对应参考实现 Radzen 的 <c>RadzenHtmlEditor</c>（含其一整套 <c>RadzenHtmlEditor*</c> 工具栏子组件），
    /// 但**完全不引用 Radzen 的任何东西**，也没有引入任何第三方编辑器内核（Quill / ProseMirror / TinyMCE 等）。
    /// 浏览器侧只用一个**本仓库自写**的极简 <c>dom.js</c> 通道（<c>elementHtmlEditor*</c>），
    /// 基于原生 <c>contenteditable</c> + <c>document.execCommand</c> 实现所见即所得编辑。
    /// </para>
    /// <para>
    /// 设计上的分工（与仓库内其它 Grow 组件一致，“纯 C# 单一真源”）：
    /// <list type="bullet">
    /// <item>工具栏结构 = <see cref="ElHtmlEditorGrowToolbar.Default"/> 的**数据描述**，组件只按描述渲染；</item>
    /// <item>插入用的 HTML（表格 / 图片 / 链接）= <see cref="ElHtmlEditorGrowHtmlBuilder"/> 纯 C# 生成；</item>
    /// <item>粘贴过滤 = <see cref="ElHtmlEditorGrowSanitizer"/> 纯 C# 消毒；</item>
    /// <item>命令状态 / 表格选区 = JSON 回传后由 <see cref="ElHtmlEditorGrowState"/> / <see cref="ElHtmlEditorGrowTableSelection"/> 解析；</item>
    /// <item>图标 = <see cref="ElHtmlEditorGrowIcons"/> 自绘内联 SVG，不依赖图标字体或第三方图标包。</item>
    /// </list>
    /// 因此核心逻辑全部可在无浏览器环境（单元测试 / SSR / 预渲染）下验证。
    /// </para>
    /// <para>
    /// 浏览器兼容性：CSS 仅使用 Chrome 49+ 支持的特性（不使用 <c>:has()</c>/<c>:is()</c>/<c>:where()</c>/逻辑属性/<c>gap</c>/<c>clamp()</c>/CSS 嵌套），
    /// 配套 JS 为 ES5 语法，且 <c>contenteditable</c> / <c>document.execCommand</c> 均为早已广泛支持的能力。
    /// </para>
    /// </summary>
    public partial class ElHtmlEditorGrow : ElementComponentBase
    {
        private static readonly string[] BlockFormats = { "p", "h1", "h2", "h3", "h4", "h5", "h6" };

        private ElementReference contentEditable;
        private ElementReference fileInput;
        private DotNetObjectReference<ElHtmlEditorGrow> selfReference;

        private readonly Dictionary<string, Func<Task>> shortcuts = new Dictionary<string, Func<Task>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> colorValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private string html = string.Empty;
        private string sourceText = string.Empty;
        private string lastValue;
        private string activePanel;
        private bool requiresUpdate;
        private bool scriptAttached;
        private ElHtmlEditorGrowMode mode = ElHtmlEditorGrowMode.Design;
        private IReadOnlyList<ElHtmlEditorGrowToolDescriptor> resolvedTools;

        // 面板草稿
        private string linkUrl;
        private string linkText;
        private bool linkNewWindow;
        private string imageUrl;
        private string imageAlt;
        private string imageWidth;
        private string imageHeight;
        private string customColor = "#000000";
        private bool tableUpdating;
        private ElHtmlEditorGrowTableArgs tableDraft = new ElHtmlEditorGrowTableArgs();

        private int idSeed = System.Threading.Interlocked.Increment(ref idCounter);
        private static int idCounter;

        #region 参数

        /// <summary>编辑器内容（HTML）。支持 <c>@bind-Value</c>。</summary>
        [Parameter]
        public string Value { get; set; }

        /// <summary>内容双向绑定回调。</summary>
        [Parameter]
        public EventCallback<string> ValueChanged { get; set; }

        /// <summary>内容变更事件（失焦、命令执行后触发，与参考实现的 <c>Change</c> 对应）。</summary>
        [Parameter]
        public EventCallback<string> Change { get; set; }

        /// <summary>内容输入事件（每次输入即触发）。</summary>
        [Parameter]
        public EventCallback<string> Input { get; set; }

        /// <summary>编辑器模式（设计 / 源码）。</summary>
        [Parameter]
        public ElHtmlEditorGrowMode Mode { get; set; } = ElHtmlEditorGrowMode.Design;

        /// <summary>模式变更回调。</summary>
        [Parameter]
        public EventCallback<ElHtmlEditorGrowMode> ModeChanged { get; set; }

        /// <summary>是否显示工具栏，默认 <c>true</c>。</summary>
        [Parameter]
        public bool ShowToolbar { get; set; } = true;

        /// <summary>是否使用内置默认工具栏；为 <c>false</c> 时仅渲染 <see cref="ChildContent"/>。</summary>
        [Parameter]
        public bool EnableDefaultToolbar { get; set; } = true;

        /// <summary>自定义工具栏内容；提供后覆盖默认工具栏。</summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>自定义工具描述列表；为 <c>null</c> 时使用 <see cref="ElHtmlEditorGrowToolbar.Default"/>。</summary>
        [Parameter]
        public IEnumerable<ElHtmlEditorGrowToolDescriptor> Tools { get; set; }

        /// <summary>需要在工具栏中隐藏的工具名。</summary>
        [Parameter]
        public IEnumerable<string> HiddenTools { get; set; }

        /// <summary>是否只读（不可编辑但可选中/复制）。</summary>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>是否禁用。</summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>占位提示文本。</summary>
        [Parameter]
        public string Placeholder { get; set; } = "请输入内容";

        /// <summary>编辑区高度，默认 <c>240px</c>。</summary>
        [Parameter]
        public string Height { get; set; } = "240px";

        /// <summary>编辑区最小高度。</summary>
        [Parameter]
        public string MinHeight { get; set; }

        /// <summary>首次渲染后是否自动聚焦。</summary>
        [Parameter]
        public bool AutoFocus { get; set; }

        /// <summary>图片上传地址；未设置时图片工具只能通过 URL 插入。</summary>
        [Parameter]
        public string UploadUrl { get; set; }

        /// <summary>上传时附加的请求头。</summary>
        [Parameter]
        public IDictionary<string, string> UploadHeaders { get; set; }

        /// <summary>上传表单字段名，默认 <c>file</c>。</summary>
        [Parameter]
        public string UploadFieldName { get; set; } = "file";

        /// <summary>文件选择框的可接受类型，默认 <c>image/*</c>。</summary>
        [Parameter]
        public string Accept { get; set; } = "image/*";

        /// <summary>是否对粘贴内容做消毒过滤（默认 <c>true</c>）。</summary>
        [Parameter]
        public bool SanitizePaste { get; set; } = true;

        /// <summary>粘贴消毒选项；为 <c>null</c> 时使用默认白名单。</summary>
        [Parameter]
        public ElHtmlEditorGrowSanitizeOptions SanitizeOptions { get; set; }

        /// <summary>粘贴事件；可改写 <c>args.Html</c> 进一步过滤内容。</summary>
        [Parameter]
        public EventCallback<ElHtmlEditorGrowPasteArgs> Paste { get; set; }

        /// <summary>命令执行事件（含自定义工具的命令名）。</summary>
        [Parameter]
        public EventCallback<ElHtmlEditorGrowExecuteArgs> Execute { get; set; }

        /// <summary>图片上传完成事件。</summary>
        [Parameter]
        public EventCallback<ElHtmlEditorGrowUploadArgs> UploadComplete { get; set; }

        /// <summary>图片上传失败事件。</summary>
        [Parameter]
        public EventCallback<ElHtmlEditorGrowUploadArgs> UploadError { get; set; }

        /// <summary>表格工具与对话框的本地化字符串。</summary>
        [Parameter]
        public ElHtmlEditorGrowTableStrings TableStrings { get; set; } = new ElHtmlEditorGrowTableStrings();

        /// <summary>工具栏附加类名。</summary>
        [Parameter]
        public string ToolbarCls { get; set; }

        #endregion

        #region 公开状态（供绑定与测试断言）

        /// <summary>当前 HTML 内容。</summary>
        public string Html => html ?? string.Empty;

        /// <summary>当前生效的模式。</summary>
        public ElHtmlEditorGrowMode CurrentMode => mode;

        /// <summary>解析后的命令状态。</summary>
        public ElHtmlEditorGrowState State { get; private set; } = new ElHtmlEditorGrowState();

        /// <summary>当前光标所在的表格信息。</summary>
        public ElHtmlEditorGrowTableSelection TableSelection { get; private set; } = new ElHtmlEditorGrowTableSelection();

        /// <summary>最近一次被拒绝的表格命令提示（已本地化）；无则为 <c>null</c>。</summary>
        public string TableMessage { get; private set; }

        /// <summary>工具栏渲染实例的标识（自动生成，用于面板与 aria 关联）。</summary>
        public string EditorId => "el-html-editor-grow-" + idSeed.ToString(CultureInfo.InvariantCulture);

        /// <summary>当前打开的面板名（<c>link</c> / <c>image</c> / <c>table</c> / <c>color:color</c> …）；未打开为 <c>null</c>。</summary>
        public string ActivePanel => activePanel;

        /// <summary>是否可编辑。</summary>
        public bool CanEdit => !Disabled && !ReadOnly;

        /// <summary>
        /// 当前工具栏中渲染的工具（只按 <see cref="HiddenTools"/> 过滤）。
        /// <para>
        /// <see cref="ElHtmlEditorGrowToolDescriptor.EnabledModes"/> **不参与过滤**：不在生效模式内的工具照样渲染，
        /// 只是置灰不可点（由 <see cref="IsToolEnabled"/> 判定，对齐参考实现 <c>EditorButton.EnabledModes</c> 的 disabled 语义）。
        /// 这样切到源码视图时工具栏仍是完整的一套图标、布局不跳动；若按模式把工具整块抽掉，
        /// 源码视图里就只剩下一排孤立的分隔符，看上去像一排没有图标的空按钮。
        /// </para>
        /// <para>同时收敛「悬空分隔符」：行首 / 行尾 / 连续出现的分隔符一律不渲染，见 <see cref="CollapseSeparators"/>。</para>
        /// </summary>
        public IReadOnlyList<ElHtmlEditorGrowToolDescriptor> VisibleTools
        {
            get
            {
                var all = ResolveTools();
                var hidden = HiddenTools == null ? null : new HashSet<string>(HiddenTools, StringComparer.OrdinalIgnoreCase);
                var kept = new List<ElHtmlEditorGrowToolDescriptor>(all.Count);

                foreach (var tool in all)
                {
                    if (hidden != null && tool.Name != null && hidden.Contains(tool.Name))
                    {
                        continue;
                    }

                    kept.Add(tool);
                }

                return CollapseSeparators(kept);
            }
        }

        /// <summary>
        /// 去掉悬空分隔符：行首、行尾以及连续出现的分隔符都不渲染。
        /// <para>
        /// 默认工具栏本身是规整的；但 <see cref="HiddenTools"/> 隐藏掉一整段工具后，两侧的两个分隔符会贴在一起，
        /// 而把某一段工具整段隐藏（或某段工具全被隐藏后只剩分隔符）会留下一排**没有图标的分隔符** ——
        /// 视觉上就是「一排点不动的空按钮」。这里统一：连续多个分隔符合并成一个，行首 / 行尾的分隔符直接丢弃。
        /// </para>
        /// </summary>
        private static IReadOnlyList<ElHtmlEditorGrowToolDescriptor> CollapseSeparators(List<ElHtmlEditorGrowToolDescriptor> tools)
        {
            var result = new List<ElHtmlEditorGrowToolDescriptor>(tools.Count);
            ElHtmlEditorGrowToolDescriptor pending = null;

            foreach (var tool in tools)
            {
                if (tool.Kind == ElHtmlEditorGrowToolKind.Separator)
                {
                    // 行首的分隔符直接丢弃；其余先挂起，等下一个真实工具来了再补上，
                    // 于是「连续多个分隔符」自然合并成一个。
                    if (result.Count > 0)
                    {
                        pending = tool;
                    }

                    continue;
                }

                if (pending != null)
                {
                    result.Add(pending);
                    pending = null;
                }

                result.Add(tool);
            }

            // 行尾挂起的分隔符不补，直接丢弃。
            return result;
        }

        /// <summary>全量工具描述（未按模式过滤）。</summary>
        public IReadOnlyList<ElHtmlEditorGrowToolDescriptor> ResolveTools()
        {
            return resolvedTools ?? (resolvedTools = Tools == null
                ? ElHtmlEditorGrowToolbar.Default
                : new List<ElHtmlEditorGrowToolDescriptor>(Tools));
        }

        #endregion

        #region 生命周期

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            html = Value ?? string.Empty;
            sourceText = html;
            lastValue = Value;
            mode = Mode;

            // 编辑区是一个空的 contenteditable div，Value 只是 C# 侧的状态，
            // 必须显式推一次才会出现在 DOM 里。此前只在「Value 相对上次发生变化」时
            // 才置 requiresUpdate（见 OnParametersSetAsync），于是**初次绑定的内容永远不显示**：
            // 编辑器一片空白，切到源码视图还会把这份空白从 DOM 回读回来，
            // 顺手把 sourceText 也清掉。首帧就置位，OnAfterRenderAsync 在挂好脚本后推一次。
            requiresUpdate = html.Length > 0;

            BuildShortcuts();

            base.OnInitialized();
        }

        /// <inheritdoc />
        protected override async Task OnParametersSetAsync()
        {
            if (Mode != mode)
            {
                await SetModeAsync(Mode);
            }

            // 仅当外部传入的 Value 相对上一次传入发生变化时，才把内容推回编辑区，
            // 避免用户正在输入时被父组件的重渲染覆盖。
            if (!string.Equals(Value, lastValue, StringComparison.Ordinal))
            {
                lastValue = Value;
                var next = Value ?? string.Empty;

                if (!string.Equals(next, html, StringComparison.Ordinal))
                {
                    html = next;
                    sourceText = next;
                    requiresUpdate = true;
                }
            }

            await base.OnParametersSetAsync();
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                if (CanEdit && !scriptAttached)
                {
                    scriptAttached = await AttachScriptAsync();
                }

                if (AutoFocus)
                {
                    await FocusAsync();
                }
            }

            if (requiresUpdate)
            {
                requiresUpdate = false;
                await SetEditorHtmlAsync(html);
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DetachScript();
                selfReference?.Dispose();
                selfReference = null;
            }

            base.Dispose(disposing);
        }

        #endregion

        #region 命令执行

        /// <summary>执行一个富文本命令（见 <see cref="ElHtmlEditorGrowCommands"/>）。</summary>
        public async Task ExecuteCommandAsync(string name, string value = null)
        {
            if (string.IsNullOrEmpty(name) || !CanEdit)
            {
                return;
            }

            if (ElHtmlEditorGrowCommands.IsTableCommand(name))
            {
                await ExecuteTableCommandAsync(name);
                return;
            }

            var execCommand = ElHtmlEditorGrowCommands.ToExecCommand(name);

            if (execCommand == null)
            {
                return;
            }

            var json = await SafeInvokeStringAsync("elementHtmlEditorExec", contentEditable, execCommand, value);

            ApplyStateJson(json);

            await RaiseExecuteAsync(name, value);
            await SyncHtmlAsync();

            if (State.InTable || ElHtmlEditorGrowCommands.IsTableCommand(name))
            {
                await UpdateTableSelectionAsync();
            }

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>执行表格命令（插入/更新表格、行列增删、合并拆分等）。</summary>
        public async Task ExecuteTableCommandAsync(string name, ElHtmlEditorGrowTableArgs args = null)
        {
            if (string.IsNullOrEmpty(name) || !CanEdit)
            {
                return;
            }

            var payload = args ?? new ElHtmlEditorGrowTableArgs();

            if (string.IsNullOrWhiteSpace(payload.DefaultColumnHeader))
            {
                payload.DefaultColumnHeader = TableStrings?.DefaultColumnHeader
                    ?? ElHtmlEditorGrowHtmlBuilder.DefaultColumnHeaderFormat;
            }

            if (string.Equals(name, ElHtmlEditorGrowCommands.InsertTable, StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(payload.Html))
            {
                payload.Html = ElHtmlEditorGrowHtmlBuilder.BuildTable(payload, TableStrings);
            }

            var json = await SafeInvokeStringAsync("elementHtmlEditorTableCommand", contentEditable, name, payload.ToJson());

            ElHtmlEditorGrowState result = null;

            if (!string.IsNullOrWhiteSpace(json))
            {
                result = ElHtmlEditorGrowState.Parse(json);
            }

            if (result != null && !result.Success)
            {
                TableMessage = TableStrings?.Resolve(result.Message) ?? result.Message;
                await UpdateTableSelectionAsync();
                await InvokeAsync(StateHasChanged);
                return;
            }

            TableMessage = null;
            ApplyStateJson(json);

            await RaiseExecuteAsync(name, payload.Html);
            await SyncHtmlAsync();
            await UpdateTableSelectionAsync();
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>构造“插入表格”所需的 HTML（纯 C#，便于测试）。</summary>
        public string BuildInsertTableHtml(ElHtmlEditorGrowTableArgs args)
        {
            return ElHtmlEditorGrowHtmlBuilder.BuildTable(args ?? new ElHtmlEditorGrowTableArgs(), TableStrings);
        }

        /// <summary>刷新工具栏命令状态（选区变化时由 JS 回调触发）。</summary>
        public async Task UpdateStateAsync()
        {
            var json = await SafeInvokeStringAsync("elementHtmlEditorQueryState", contentEditable);

            ApplyStateJson(json);
            await UpdateTableSelectionAsync();
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>刷新当前表格选区信息。</summary>
        public async Task UpdateTableSelectionAsync()
        {
            var json = await SafeInvokeStringAsync("elementHtmlEditorGetTableSelection", contentEditable);
            var selection = ElHtmlEditorGrowTableSelection.Parse(json);

            if (!string.IsNullOrWhiteSpace(json))
            {
                TableSelection = selection;
                State.InTable = selection.InTable;
            }

            if (!string.IsNullOrWhiteSpace(json))
            {
                // 回填表格面板草稿，便于“更新表格”时复用当前表格设置。
                tableDraft.Rows = selection.Rows > 0 ? selection.Rows : tableDraft.Rows;
                tableDraft.Columns = selection.Columns > 0 ? selection.Columns : tableDraft.Columns;
                tableDraft.HeaderRow = selection.HeaderRow;
            }
        }

        /// <summary>保存当前选区（点击面板/下拉后仍能恢复到原位置）。</summary>
        public async Task SaveSelectionAsync()
        {
            await SafeInvokeVoidAsync("elementHtmlEditorSaveSelection", contentEditable);
        }

        /// <summary>恢复最近保存的选区。</summary>
        public async Task RestoreSelectionAsync()
        {
            await SafeInvokeVoidAsync("elementHtmlEditorRestoreSelection", contentEditable);
        }

        /// <summary>读取编辑区当前 HTML（来自 DOM，含用户尚未回传的编辑）。</summary>
        public async Task<string> GetHtmlAsync()
        {
            var current = await SafeInvokeStringAsync("elementHtmlEditorGetHtml", contentEditable);

            return current ?? html;
        }

        /// <summary>直接把 HTML 写入编辑区。</summary>
        public async Task SetHtmlAsync(string value)
        {
            html = value ?? string.Empty;
            sourceText = html;
            lastValue = html;

            await SetEditorHtmlAsync(html);
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>聚焦编辑器。</summary>
        public async Task FocusAsync()
        {
            try
            {
                if (mode == ElHtmlEditorGrowMode.Source)
                {
                    return;
                }

                await contentEditable.FocusAsync();
            }
            catch (Exception)
            {
                // 无 DOM 环境（单元测试 / SSR）下忽略。
            }
        }

        /// <summary>在设计 / 源码模式之间切换。</summary>
        public async Task ToggleModeAsync()
        {
            await SetModeAsync(mode == ElHtmlEditorGrowMode.Design ? ElHtmlEditorGrowMode.Source : ElHtmlEditorGrowMode.Design);
        }

        /// <summary>切换到指定模式。</summary>
        public async Task SetModeAsync(ElHtmlEditorGrowMode value)
        {
            if (value == mode)
            {
                return;
            }

            var previous = mode;
            mode = value;

            if (value == ElHtmlEditorGrowMode.Source)
            {
                // 进入源码视图前，先把 DOM 里的最新内容取回来。
                var current = await GetHtmlAsync();
                html = current ?? html;
                sourceText = html;
            }
            else if (previous == ElHtmlEditorGrowMode.Source)
            {
                // 回到设计视图时，把源码文本推回 DOM。
                requiresUpdate = true;
            }

            if (ModeChanged.HasDelegate)
            {
                await ModeChanged.InvokeAsync(mode);
            }

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>注册快捷键（供自定义工具子组件使用，对齐参考实现的 <c>RegisterShortcut</c>）。</summary>
        public void RegisterShortcut(string key, Func<Task> action)
        {
            if (!string.IsNullOrEmpty(key) && action != null)
            {
                shortcuts[key] = action;
            }
        }

        /// <summary>注销快捷键。</summary>
        public void UnregisterShortcut(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                shortcuts.Remove(key);
            }
        }

        #endregion

        #region 工具栏交互

        /// <summary>工具是否可用。</summary>
        public bool IsToolEnabled(ElHtmlEditorGrowToolDescriptor tool)
        {
            if (tool == null || Disabled || ReadOnly)
            {
                return false;
            }

            // 不在当前模式内的工具（例如源码视图中所有设计类命令）仍然渲染在工具栏上，但一律置灰不可点。
            if ((tool.EnabledModes & mode) == 0)
            {
                return false;
            }

            if (tool.RequiresTable && !TableSelection.InTable)
            {
                return false;
            }

            if (tool.RequiresTable && tool.Command != null && !TableSelection.CanExecute(tool.Command))
            {
                return false;
            }

            return true;
        }

        /// <summary>工具当前是否为激活（选中）状态。</summary>
        public bool IsToolActive(ElHtmlEditorGrowToolDescriptor tool)
        {
            if (tool == null)
            {
                return false;
            }

            if (string.Equals(tool.Name, "source", StringComparison.OrdinalIgnoreCase))
            {
                return mode == ElHtmlEditorGrowMode.Source;
            }

            if (tool.Kind == ElHtmlEditorGrowToolKind.Color || string.IsNullOrEmpty(tool.Command))
            {
                return false;
            }

            return ElHtmlEditorGrowCommands.IsActive(State, tool.Command);
        }

        /// <summary>下拉工具当前的回显值。</summary>
        public string GetSelectValue(ElHtmlEditorGrowToolDescriptor tool)
        {
            if (tool == null)
            {
                return string.Empty;
            }

            switch (tool.StateProperty)
            {
                case "FontName":
                    return StripQuotes(State?.FontName);
                case "FontSize":
                    return StripQuotes(State?.FontSize);
                case "FormatBlock":
                    return NormalizeBlockFormat(State?.FormatBlock);
                default:
                    return string.Empty;
            }
        }

        /// <summary>颜色工具当前使用的颜色。</summary>
        public string GetColorValue(ElHtmlEditorGrowToolDescriptor tool)
        {
            if (tool == null)
            {
                return "#000000";
            }

            return colorValues.TryGetValue(tool.Name, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : (tool.Value ?? "#000000");
        }

        /// <summary>颜色面板中可选的颜色。</summary>
        public static IReadOnlyList<string> Palette => ElHtmlEditorGrowToolbar.Palette;

        private async Task OnToolClickAsync(ElHtmlEditorGrowToolDescriptor tool)
        {
            if (tool == null || !IsToolEnabled(tool))
            {
                return;
            }

            switch (tool.Name)
            {
                case "source":
                    activePanel = null;
                    await ToggleModeAsync();
                    return;

                case "link":
                    await OpenLinkPanelAsync();
                    return;

                case "image":
                    await OpenImagePanelAsync();
                    return;

                case "table":
                    await OpenTablePanelAsync();
                    return;
            }

            if (tool.Kind == ElHtmlEditorGrowToolKind.Color)
            {
                await SaveSelectionAsync();
                activePanel = "color:" + tool.Name;
                await InvokeAsync(StateHasChanged);
                return;
            }

            activePanel = null;
            await ExecuteCommandAsync(tool.Command);
        }

        private async Task OnSelectChangedAsync(ElHtmlEditorGrowToolDescriptor tool, ChangeEventArgs args)
        {
            if (tool == null || !IsToolEnabled(tool))
            {
                return;
            }

            var value = args?.Value as string;

            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            await ExecuteCommandAsync(tool.Command, value);
        }

        /// <summary>点击颜色面板中的色块。</summary>
        public async Task ApplyColorAsync(string panelName, string color)
        {
            var tool = ElHtmlEditorGrowToolbar.Find(panelName) ?? FindToolByName(panelName);

            if (tool == null || string.IsNullOrWhiteSpace(color))
            {
                return;
            }

            colorValues[panelName] = color;
            activePanel = null;

            await RestoreSelectionAsync();
            await ExecuteCommandAsync(tool.Command, color);
        }

        /// <summary>关闭当前面板。</summary>
        public Task ClosePanelAsync()
        {
            activePanel = null;

            return InvokeAsync(StateHasChanged);
        }

        #endregion

        #region 面板

        /// <summary>打开插入链接面板。</summary>
        public async Task OpenLinkPanelAsync()
        {
            await SaveSelectionAsync();

            var selected = await SafeInvokeStringAsync("elementHtmlEditorSelectionText", contentEditable);
            linkText = string.IsNullOrEmpty(selected) ? string.Empty : selected;
            linkUrl = string.Empty;
            linkNewWindow = false;
            activePanel = "link";

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>打开插入图片面板。</summary>
        public async Task OpenImagePanelAsync()
        {
            await SaveSelectionAsync();

            imageUrl = string.Empty;
            imageAlt = string.Empty;
            imageWidth = string.Empty;
            imageHeight = string.Empty;
            activePanel = "image";

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>打开表格面板（光标在表格内时进入“更新表格”状态）。</summary>
        public async Task OpenTablePanelAsync()
        {
            await SaveSelectionAsync();
            await UpdateTableSelectionAsync();

            tableUpdating = TableSelection.InTable;

            if (tableUpdating)
            {
                tableDraft.Rows = TableSelection.Rows > 0 ? TableSelection.Rows : tableDraft.Rows;
                tableDraft.Columns = TableSelection.Columns > 0 ? TableSelection.Columns : tableDraft.Columns;
                tableDraft.HeaderRow = TableSelection.HeaderRow;
                tableDraft.Width = TableSelection.Width;
                tableDraft.Border = TableSelection.Border > 0 ? TableSelection.Border : tableDraft.Border;
                tableDraft.RowIndex = TableSelection.RowIndex;
                tableDraft.ColumnIndex = TableSelection.ColumnIndex;
                tableDraft.ColumnWidth = TableSelection.ColumnWidth;
                tableDraft.CellBackground = TableSelection.CellBackground;
                tableDraft.CellTextAlign = TableSelection.CellTextAlign;
                tableDraft.CellVerticalAlign = TableSelection.CellVerticalAlign;
                tableDraft.BorderStyle = TableSelection.BorderStyle;
                tableDraft.BorderWidthPx = TableSelection.BorderWidthPx;
                tableDraft.BorderColor = TableSelection.BorderColor;
            }
            else
            {
                tableDraft = new ElHtmlEditorGrowTableArgs
                {
                    Rows = tableDraft.Rows > 0 ? tableDraft.Rows : DefaultTableRows,
                    Columns = tableDraft.Columns > 0 ? tableDraft.Columns : DefaultTableColumns,
                    Width = string.IsNullOrWhiteSpace(tableDraft.Width) ? DefaultTableWidth : tableDraft.Width,
                    Border = tableDraft.Border > 0 ? tableDraft.Border : 1,
                    HeaderRow = tableDraft.HeaderRow
                };
            }

            activePanel = "table";

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>表格面板的草稿参数。</summary>
        public ElHtmlEditorGrowTableArgs TableDraft => tableDraft;

        /// <summary>表格面板当前是否为“更新已有表格”。</summary>
        public bool TableUpdating => tableUpdating;

        /// <summary>提交表格面板。</summary>
        public async Task SubmitTablePanelAsync()
        {
            var args = tableDraft;

            activePanel = null;

            await RestoreSelectionAsync();

            if (tableUpdating)
            {
                await ExecuteTableCommandAsync(ElHtmlEditorGrowCommands.UpdateTable, args);
            }
            else
            {
                await ExecuteTableCommandAsync(ElHtmlEditorGrowCommands.InsertTable, args);
            }
        }

        /// <summary>提交链接面板。</summary>
        public async Task SubmitLinkPanelAsync()
        {
            var markup = ElHtmlEditorGrowHtmlBuilder.BuildLink(linkUrl, linkText, linkNewWindow);

            activePanel = null;

            if (markup == null)
            {
                await InvokeAsync(StateHasChanged);
                return;
            }

            await RestoreSelectionAsync();
            await ExecuteCommandAsync(ElHtmlEditorGrowCommands.InsertHtml, markup);

            linkUrl = string.Empty;
            linkText = string.Empty;
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>提交图片面板（按 URL 插入）。</summary>
        public async Task SubmitImagePanelAsync()
        {
            var markup = ElHtmlEditorGrowHtmlBuilder.BuildImage(imageUrl, imageAlt, imageWidth, imageHeight);

            if (markup == null)
            {
                return;
            }

            activePanel = null;

            await RestoreSelectionAsync();
            await ExecuteCommandAsync(ElHtmlEditorGrowCommands.InsertHtml, markup);

            imageUrl = string.Empty;
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>颜色面板中的自定义颜色。</summary>
        public string CustomColor
        {
            get => customColor;
            set => customColor = value;
        }

        /// <summary>打开文件选择对话框（图片上传）。</summary>
        public async Task PickFileAsync()
        {
            if (!CanEdit || string.IsNullOrWhiteSpace(UploadUrl))
            {
                return;
            }

            await SafeInvokeVoidAsync("elementHtmlEditorPickFile", contentEditable);
        }

        /// <summary>链接面板的地址。</summary>
        public string LinkUrl
        {
            get => linkUrl;
            set => linkUrl = value;
        }

        /// <summary>链接面板的显示文本。</summary>
        public string LinkText
        {
            get => linkText;
            set => linkText = value;
        }

        /// <summary>链接是否在新窗口打开。</summary>
        public bool LinkNewWindow
        {
            get => linkNewWindow;
            set => linkNewWindow = value;
        }

        /// <summary>图片面板的地址。</summary>
        public string ImageUrl
        {
            get => imageUrl;
            set => imageUrl = value;
        }

        /// <summary>图片替代文本。</summary>
        public string ImageAlt
        {
            get => imageAlt;
            set => imageAlt = value;
        }

        /// <summary>图片宽度。</summary>
        public string ImageWidth
        {
            get => imageWidth;
            set => imageWidth = value;
        }

        /// <summary>图片高度。</summary>
        public string ImageHeight
        {
            get => imageHeight;
            set => imageHeight = value;
        }

        /// <summary>表格默认行数。</summary>
        [Parameter]
        public int DefaultTableRows { get; set; } = 2;

        /// <summary>表格默认列数。</summary>
        [Parameter]
        public int DefaultTableColumns { get; set; } = 2;

        /// <summary>表格默认宽度。</summary>
        [Parameter]
        public string DefaultTableWidth { get; set; } = "100%";

        #endregion

        #region JS 回调

        /// <summary>编辑区内容变化（由自写 JS 回调）。</summary>
        [JSInvokable("OnEditorInputAsync")]
        public async Task OnEditorInputAsync(string value)
        {
            if (!CanEdit)
            {
                return;
            }

            var next = value ?? string.Empty;

            if (string.Equals(next, html, StringComparison.Ordinal))
            {
                return;
            }

            html = next;
            sourceText = next;
            lastValue = next;

            if (Input.HasDelegate)
            {
                await Input.InvokeAsync(html);
            }

            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(html);
            }

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>选区变化（由自写 JS 回调），用于刷新工具栏状态。</summary>
        [JSInvokable("OnSelectionChangedAsync")]
        public async Task OnSelectionChangedAsync()
        {
            await UpdateStateAsync();
        }

        /// <summary>失焦（由自写 JS 回调），触发 <see cref="Change"/>。</summary>
        [JSInvokable("OnEditorBlurAsync")]
        public async Task OnEditorBlurAsync()
        {
            if (Change.HasDelegate)
            {
                await Change.InvokeAsync(html);
            }
        }

        /// <summary>
        /// 过滤粘贴内容（由自写 JS 回调）：先按 <see cref="SanitizePaste"/> 消毒，再交给 <see cref="Paste"/> 事件，
        /// 返回最终要插入编辑区的 HTML。
        /// </summary>
        [JSInvokable("OnPasteFilterAsync")]
        public async Task<string> OnPasteFilterAsync(string pastedHtml)
        {
            var result = SanitizePaste
                ? ElHtmlEditorGrowSanitizer.Sanitize(pastedHtml, SanitizeOptions)
                : (pastedHtml ?? string.Empty);

            if (Paste.HasDelegate)
            {
                var args = new ElHtmlEditorGrowPasteArgs { Html = result };
                await Paste.InvokeAsync(args);
                result = args.Html ?? string.Empty;
            }

            return result;
        }

        /// <summary>图片上传完成（由自写 JS 回调）。</summary>
        [JSInvokable("OnUploadCompletedAsync")]
        public async Task OnUploadCompletedAsync(string payload)
        {
            var args = ParseUploadPayload(payload);

            if (!string.IsNullOrWhiteSpace(args.Url))
            {
                var markup = ElHtmlEditorGrowHtmlBuilder.BuildImage(args.Url, imageAlt, imageWidth, imageHeight);

                if (!string.IsNullOrWhiteSpace(markup))
                {
                    activePanel = null;
                    await RestoreSelectionAsync();
                    await ExecuteCommandAsync(ElHtmlEditorGrowCommands.InsertHtml, markup);
                }
            }

            if (args.Success)
            {
                if (UploadComplete.HasDelegate)
                {
                    await UploadComplete.InvokeAsync(args);
                }
            }
            else if (UploadError.HasDelegate)
            {
                await UploadError.InvokeAsync(args);
            }

            imageUrl = string.Empty;
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>解析上传回调负载（纯 C#，便于测试）。</summary>
        public static ElHtmlEditorGrowUploadArgs ParseUploadPayload(string payload)
        {
            var args = new ElHtmlEditorGrowUploadArgs { RawResponse = payload, Success = true };

            if (string.IsNullOrWhiteSpace(payload))
            {
                args.Success = false;
                args.Error = "empty response";
                return args;
            }

            var trimmed = payload.Trim();

            if (trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                try
                {
                    using var document = System.Text.Json.JsonDocument.Parse(trimmed);
                    var root = document.RootElement;

                    if (root.TryGetProperty("url", out var url) || root.TryGetProperty("Url", out url))
                    {
                        args.Url = url.GetString();
                    }

                    if (root.TryGetProperty("success", out var success) || root.TryGetProperty("Success", out success))
                    {
                        args.Success = success.ValueKind != System.Text.Json.JsonValueKind.False;
                    }
                    else
                    {
                        args.Success = !string.IsNullOrWhiteSpace(args.Url);
                    }

                    if (root.TryGetProperty("error", out var error) || root.TryGetProperty("Error", out error))
                    {
                        args.Error = error.GetString();
                    }

                    if (!string.IsNullOrWhiteSpace(args.Url))
                    {
                        args.Success = true;
                    }

                    return args;
                }
                catch (System.Text.Json.JsonException)
                {
                    // 响应不是合法 JSON：退回“响应文本就是地址”的处理方式。
                }
            }

            args.Url = trimmed.Trim('"');
            args.Success = !string.IsNullOrWhiteSpace(args.Url) && ElHtmlEditorGrowSanitizer.IsSafeUrl(args.Url);

            if (!args.Success)
            {
                args.Error = "invalid response";
            }

            return args;
        }

        /// <summary>源码视图内容变化。</summary>
        protected async Task OnSourceInputAsync(ChangeEventArgs args)
        {
            if (!CanEdit)
            {
                return;
            }

            sourceText = args?.Value as string ?? string.Empty;
            html = sourceText;
            lastValue = html;

            if (Input.HasDelegate)
            {
                await Input.InvokeAsync(html);
            }

            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(html);
            }
        }

        /// <summary>源码视图失焦触发 <see cref="Change"/>。</summary>
        protected async Task OnSourceBlurAsync()
        {
            if (Change.HasDelegate)
            {
                await Change.InvokeAsync(html);
            }
        }

        /// <summary>编辑器键盘：命中已注册快捷键时执行对应命令。</summary>
        protected async Task OnKeyDownAsync(KeyboardEventArgs e)
        {
            if (!CanEdit || e == null)
            {
                return;
            }

            var shortcut = BuildShortcutKey(e);

            if (shortcut != null && shortcuts.TryGetValue(shortcut, out var action))
            {
                await action();
            }
        }

        #endregion

        #region 内部工具

        private const string CtrlToken = "Ctrl";

        private static string BuildShortcutKey(KeyboardEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Key))
            {
                return null;
            }

            var key = e.Key.Length == 1 ? e.Key.ToUpperInvariant() : e.Key;
            var builder = new StringBuilder();

            if (e.CtrlKey || e.MetaKey)
            {
                builder.Append(CtrlToken).Append('+');
            }

            if (e.AltKey)
            {
                builder.Append("Alt+");
            }

            if (e.ShiftKey)
            {
                builder.Append("Shift+");
            }

            builder.Append(key);

            return builder.ToString();
        }

        private void BuildShortcuts()
        {
            shortcuts.Clear();

            foreach (var tool in ResolveTools())
            {
                if (string.IsNullOrEmpty(tool.Shortcut) || string.IsNullOrEmpty(tool.Command))
                {
                    continue;
                }

                var name = tool.Command;
                shortcuts[tool.Shortcut] = () => ExecuteCommandAsync(name);
            }
        }

        private ElHtmlEditorGrowToolDescriptor FindToolByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return ElHtmlEditorGrowToolbar.Find(name);
        }

        private async Task RaiseExecuteAsync(string name, string value)
        {
            if (Execute.HasDelegate)
            {
                await Execute.InvokeAsync(new ElHtmlEditorGrowExecuteArgs
                {
                    Editor = this,
                    CommandName = name,
                    Value = value
                });
            }
        }

        private void ApplyStateJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var parsed = ElHtmlEditorGrowState.Parse(json);
            parsed.Html = parsed.Html ?? string.Empty;
            State = parsed;
        }

        private async Task SyncHtmlAsync()
        {
            if (State?.Html == null || string.Equals(State.Html, html, StringComparison.Ordinal))
            {
                return;
            }

            html = State.Html;
            sourceText = html;
            lastValue = html;

            await NotifyValueChangedAsync();
        }

        private async Task NotifyValueChangedAsync()
        {
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(html);
            }

            if (Change.HasDelegate)
            {
                await Change.InvokeAsync(html);
            }
        }

        private async Task SetEditorHtmlAsync(string value)
        {
            await SafeInvokeVoidAsync("elementHtmlEditorSetHtml", contentEditable, value ?? string.Empty);
        }

        private async Task<bool> AttachScriptAsync()
        {
            try
            {
                selfReference?.Dispose();
                selfReference = DotNetObjectReference.Create(this);

                await JSRuntime.InvokeVoidAsync(
                    "elementHtmlEditorAttach",
                    contentEditable,
                    selfReference,
                    fileInput,
                    UploadUrl,
                    SerializeHeaders(),
                    UploadFieldName);

                return true;
            }
            catch (Exception)
            {
                // 宿主未引入 dom.js（或处于单元测试环境）时静默降级：组件仍可渲染与绑定值。
                return false;
            }
        }

        private void DetachScript()
        {
            if (!scriptAttached)
            {
                return;
            }

            scriptAttached = false;

            try
            {
                _ = JSRuntime.InvokeVoidAsync("elementHtmlEditorDetach", contentEditable);
            }
            catch (Exception)
            {
                // 忽略：断开连接时无需保证。
            }
        }

        private string SerializeHeaders()
        {
            if (UploadHeaders == null || UploadHeaders.Count == 0)
            {
                return "{}";
            }

            var builder = new StringBuilder("{");
            var first = true;

            foreach (var pair in UploadHeaders)
            {
                if (!first)
                {
                    builder.Append(',');
                }

                first = false;
                builder.Append('"').Append(EscapeJson(pair.Key)).Append("\":\"").Append(EscapeJson(pair.Value)).Append('"');
            }

            builder.Append('}');

            return builder.ToString();
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private async Task<string> SafeInvokeStringAsync(string identifier, params object[] args)
        {
            if (!scriptAttached)
            {
                return null;
            }

            try
            {
                return await JSRuntime.InvokeAsync<string>(identifier, args);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task SafeInvokeVoidAsync(string identifier, params object[] args)
        {
            if (!scriptAttached)
            {
                return;
            }

            try
            {
                await JSRuntime.InvokeVoidAsync(identifier, args);
            }
            catch (Exception)
            {
                // 忽略：无 JS 环境下这些调用本就不存在。
            }
        }

        private static string StripQuotes(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Trim().Trim('"', '\'').Trim();
        }

        /// <summary>把浏览器回传的块格式（可能为 <c>&lt;h1&gt;</c> 或 <c>H1</c>）规范为 <c>h1</c>。</summary>
        public static string NormalizeBlockFormat(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Trim().Trim('<', '>').Trim().ToLowerInvariant();

            foreach (var format in BlockFormats)
            {
                if (normalized == format)
                {
                    return format;
                }
            }

            if (normalized == "div" || normalized == "body" || normalized == "normal")
            {
                return "p";
            }

            return normalized;
        }

        /// <summary>构造工具按钮的 CSS 类。</summary>
        public string BuildToolCssClass(ElHtmlEditorGrowToolDescriptor tool)
        {
            var builder = HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-html-editor-grow__btn");

            if (IsToolActive(tool))
            {
                builder.Add("is-active");
            }

            if (!IsToolEnabled(tool))
            {
                builder.Add("is-disabled");
            }

            if (tool != null && IsPanelOpenFor(tool))
            {
                builder.Add("is-open");
            }

            return builder.ToString();
        }

        private bool IsPanelOpenFor(ElHtmlEditorGrowToolDescriptor tool)
        {
            if (tool == null || string.IsNullOrEmpty(activePanel))
            {
                return false;
            }

            switch (tool.Name)
            {
                case "link": return activePanel == "link";
                case "image": return activePanel == "image";
                case "table": return activePanel == "table";
                default: return activePanel == "color:" + tool.Name;
            }
        }

        /// <summary>编辑区容器的行内样式。</summary>
        public string BuildBodyStyle()
        {
            var builder = HtmlPropertyBuilder.CreateCssStyleBuilder()
                .Add("height", string.IsNullOrWhiteSpace(Height) ? null : Height)
                .Add("min-height", string.IsNullOrWhiteSpace(MinHeight) ? null : MinHeight);

            return builder.ToString();
        }

        /// <summary>工具栏的 CSS 类。</summary>
        public string BuildToolbarCssClass()
        {
            var builder = HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-html-editor-grow__toolbar")
                .Add(ToolbarCls);

            return builder.ToString();
        }

        /// <summary>外壳的 CSS 类。</summary>
        public string BuildWrapperCssClass()
        {
            var builder = HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-html-editor-grow")
                .AddIf(Disabled, "is-disabled")
                .AddIf(ReadOnly, "is-readonly")
                .AddIf(!string.IsNullOrWhiteSpace(Cls), Cls);

            return builder.ToString();
        }

        /// <summary>外壳的行内样式。</summary>
        public string BuildWrapperStyle()
        {
            return HtmlPropertyBuilder.CreateCssStyleBuilder().Add(Style).ToString();
        }

        #endregion
    }
}
