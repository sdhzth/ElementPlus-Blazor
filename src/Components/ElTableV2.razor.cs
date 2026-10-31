using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace ElementPlus
{
    public partial class ElTableV2 : ElementComponentBase, IAsyncDisposable
    {
        private ElementReference _rootRef;
        private IJSObjectReference _jsModule;
        private DotNetObjectReference<ElTableV2> _dotNetRef;
        private bool _moduleLoaded;
        private int _scrollTop;
        private int _scrollLeft;
        private bool _isScrolling;
        private bool _disposed;
        private bool _defaultsSeeded;
        private TableV2SortBy _sortByInternal;

        private List<object> _rows = new List<object>();
        private List<object> _fixedRows = new List<object>();

        // ===================== 性能缓存 =====================
        // 背景（2026-09-30 实测）：滚动时组件每帧重渲染，原先每次都把「数据源 → 扁平行」
        // 全量重算一遍，且每行都要反射两次（rowKey 属性 + children 字段）。400 行表
        // 实测单帧被阻塞 80~107ms（200 行表 30~48ms，与行数成正比）⇒ 滚动中出现
        // 空白停滞。下面三层缓存把每帧的 O(行数) 工作降为 O(1)：只在数据/展开态
        // 真正变化时重算。

        /// <summary>属性反射缓存：type.FullName|属性名 → PropertyInfo（可为 null，表示不存在）</summary>
        private static readonly Dictionary<string, PropertyInfo> s_propertyCache = new Dictionary<string, PropertyInfo>();

        /// <summary>扁平化结果缓存</summary>
        private List<TableV2FlatRow> _flatCache;

        /// <summary>展开态版本号：每次展开/收起自增，用于让扁平化缓存失效</summary>
        private int _expansionVersion;

        private object _flatCacheDataSource;
        private int _flatCacheRowCount = -1;
        private string _flatCacheChildrenField;
        private string _flatCacheRowKey;
        private bool _flatCacheHasExpandContent;
        private object _flatCacheExpandedRows;
        private int _flatCacheVersion = -1;

        /// <summary>数据源拷贝缓存（避免每次父组件重渲染都重新 ToList 一遍全量数据）</summary>
        private object _rowsSource;
        private int _rowsSourceCount = -1;
        private object _fixedRowsSource;
        private int _fixedRowsSourceCount = -1;

        // ===================== 重渲染抑制 =====================
        // 背景（2026-09-30 实测）：父组件（页面）每一次重渲染都会顺着组件树把子组件
        // 全部重渲染一遍 —— 即使参数一个都没变。本 demo 页有 9 张表，于是「滚动时
        // 更新一个页面级日志」就把 9 张表全部重渲染：实测单次滚动首帧 62~71ms
        // （无 OnScroll 绑定的同类表只要 27ms）。
        // 这里做两件事：
        //   ① 参数未变且组件自身状态未变时 ShouldRender 返回 false（整棵子树跳过）；
        //   ② 仅在「列携带模板」时放弃① —— 因为列对象是调用方持有的引用，
        //      其 Template/CellClassName 可能捕获了调用方的状态，引用比较看不出来。

        /// <summary>本次参数写入是否真的带来了变化</summary>
        private bool _paramsDirty = true;

        /// <summary>是否已完成首次参数写入（首帧必须渲染，哪怕一个参数都没传）</summary>
        private bool _parametersInitialized;

        /// <summary>组件自身状态（滚动/展开/排序）请求的渲染</summary>
        private bool _selfDirty;

        /// <summary>参数属性反射缓存（键前缀 $param$，与行属性缓存共用一张表）</summary>
        private static PropertyInfo ResolveOwnProperty(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var key = "$param$" + name;
            lock (s_propertyCache)
            {
                if (s_propertyCache.TryGetValue(key, out var cached)) return cached;
            }
            var prop = typeof(ElTableV2).GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            lock (s_propertyCache)
            {
                s_propertyCache[key] = prop;
            }
            return prop;
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            // 首帧无条件渲染：此时「当前值全是默认值」，若调用方一个参数都没传，
            // dirty 会算出 false，组件就会一帧都不渲染。
            if (!_parametersInitialized)
            {
                _parametersInitialized = true;
                _paramsDirty = true;
                return base.SetParametersAsync(parameters);
            }

            var dirty = false;
            foreach (var parameter in parameters)
            {
                var prop = ResolveOwnProperty(parameter.Name);
                var current = prop != null && prop.CanRead ? prop.GetValue(this) : null;
                if (!Equals(current, parameter.Value))
                {
                    dirty = true;
                    break;
                }
            }
            _paramsDirty = dirty;
            return base.SetParametersAsync(parameters);
        }

        protected override bool ShouldRender()
        {
            if (_paramsDirty || _selfDirty || HasColumnTemplates())
            {
                _selfDirty = false;
                return true;
            }
            return false;
        }

        /// <summary>列上是否挂了模板/按行委托 —— 这类值可在引用不变的情况下改变输出，故不参与重渲染抑制</summary>
        private bool HasColumnTemplates()
        {
            if (Columns == null) return false;
            foreach (var column in Columns)
            {
                if (column == null) continue;
                if (column.Template != null || column.HeaderTemplate != null
                    || column.CellClassName != null || column.HeaderCellClassName != null)
                {
                    return true;
                }
            }
            return false;
        }

        [Parameter]
        public object DataSource { get; set; }

        /// <summary>数据源（对齐 EP data 属性命名；与 DataSource 同一存储）</summary>
        [Parameter]
        public object Data { get => DataSource; set => DataSource = value; }

        [Parameter]
        public IList<TableV2Column> Columns { get; set; }

        [Parameter]
        public bool AutoGenerateColumns { get; set; } = true;

        /// <summary>行高（虚拟滚动必需，对齐 EP row-height 默认 50）</summary>
        [Parameter]
        public int RowHeight { get; set; } = 50;

        /// <summary>表头高度（对齐 EP header-height 默认 50）</summary>
        [Parameter]
        public int HeaderHeight { get; set; } = 50;

        /// <summary>预渲染行数（对齐 EP cache，减少滚动白屏）</summary>
        [Parameter]
        public int OverscanCount { get; set; } = 5;

        /// <summary>
        /// 滚动进行中额外追加的预渲染行数（默认 10 行 ≈ 500px）。
        /// <para>
        /// 配合 rAF 节流（每帧用最新 scrollTop 渲染）使用：快速滚动时单次帧位移可能达数百像素，
        /// 在静止 overscan 之外再追加缓冲，确保渲染窗口始终覆盖视口、消除空白。
        /// 静止后会回落到 <see cref="OverscanCount"/>，不增加常态渲染成本。
        /// </para>
        /// </summary>
        [Parameter]
        public int ScrollingOverscanBoost { get; set; } = 10;

        /// <summary>预渲染行数（对齐 EP cache 命名；与 OverscanCount 同一存储）</summary>
        [Parameter]
        public int Cache { get => OverscanCount; set => OverscanCount = value; }

        [Parameter]
        public string Height { get; set; }

        [Parameter]
        public string Width { get; set; }

        /// <summary>粘性固定行（对齐 EP fixed-data：始终钉在表头下方）</summary>
        [Parameter]
        public IEnumerable FixedData { get; set; }

        // ===================== 展开行 / 树形数据 =====================

        /// <summary>展开行内容（本库扩展：展开后在行下方渲染）</summary>
        [Parameter]
        public RenderFragment<object> ExpandContent { get; set; }

        /// <summary>展开列键（对齐 EP expand-column-key：展开箭头渲染在该列单元格内）</summary>
        [Parameter]
        public string ExpandColumnKey { get; set; }

        /// <summary>当前展开的行键集合（扩展；ExpandRowKeys 的同一存储）</summary>
        [Parameter]
        public HashSet<object> ExpandedRows { get; set; } = new HashSet<object>();

        /// <summary>展开行键集合（对齐 EP expand-row-keys；与 ExpandedRows 同一存储）</summary>
        [Parameter]
        public HashSet<object> ExpandRowKeys { get => ExpandedRows; set => ExpandedRows = value ?? new HashSet<object>(); }

        /// <summary>初始展开的行键（对齐 EP default-expand-row-keys，仅首次生效）</summary>
        [Parameter]
        public IEnumerable<object> DefaultExpandRowKeys { get; set; }

        /// <summary>默认展开全部可展开行（对齐 EP default-expand-all-rows，仅首次生效）</summary>
        [Parameter]
        public bool DefaultExpandAllRows { get; set; }

        /// <summary>展开状态变化回调（对齐 EP expanded-rows-change 事件）</summary>
        [Parameter]
        public EventCallback<IEnumerable<object>> ExpandedRowsChange { get; set; }

        /// <summary>树形数据的子节点字段名（默认 children，对齐 EP tree-props.children）</summary>
        [Parameter]
        public string TreeChildrenField { get; set; } = "children";

        /// <summary>树形缩进步长 px（对齐 EP indent-size 默认 12）</summary>
        [Parameter]
        public int IndentSize { get; set; } = 12;

        /// <summary>展开图标尺寸 px（对齐 EP icon-size 默认 12）</summary>
        [Parameter]
        public int IconSize { get; set; } = 12;

        /// <summary>当前行 key 属性名（用于回显 ExpandedRows 等场景；缺省回落 id 属性）</summary>
        [Parameter]
        public string RowKey { get; set; }

        // ===================== 排序 =====================

        /// <summary>排序模型（对齐 EP sortBy：{ key, order }；受控传入时组件不内部改写）</summary>
        [Parameter]
        public TableV2SortBy SortBy { get; set; }

        /// <summary>排序模型双向绑定（配合 SortBy 受控使用）</summary>
        [Parameter]
        public EventCallback<TableV2SortBy> SortByChanged { get; set; }

        /// <summary>多列排序状态表（对齐 EP sort-state：key → order）</summary>
        [Parameter]
        public Dictionary<string, TableV2SortOrder> SortState { get; set; }

        /// <summary>列排序事件（对齐 EP column-sort；组件只发事件，数据排序由调用方完成）</summary>
        [Parameter]
        public EventCallback<TableV2ColumnSortArgs> ColumnSort { get; set; }

        // ===================== 汇总行（扩展） =====================

        [Parameter]
        public bool ShowSummary { get; set; }

        [Parameter]
        public string SumText { get; set; } = "合计";

        [Parameter]
        public Func<TableSummaryContext, object> SummaryMethod { get; set; }

        // ===================== 外观与状态 =====================

        [Parameter]
        public bool Stripe { get; set; } = true;

        [Parameter]
        public bool Border { get; set; }

        [Parameter]
        public bool HighlightCurrentRow { get; set; }

        [Parameter]
        public int CurrentRowIndex { get; set; } = -1;

        /// <summary>行 class（扩展：Func&lt;行, string&gt;）</summary>
        [Parameter]
        public Func<object, string> RowClassName { get; set; }

        /// <summary>行 class（对齐 EP row-class 字符串形式）</summary>
        [Parameter]
        public string RowClass { get; set; }

        /// <summary>行 class（对齐 EP row-class 函数形式：columns/row/rowIndex）</summary>
        [Parameter]
        public Func<TableV2RowClassContext, string> RowClassGetter { get; set; }

        /// <summary>行内联样式（扩展）</summary>
        [Parameter]
        public Func<object, string> RowStyle { get; set; }

        /// <summary>行附加 HTML 属性（对齐 EP row-props；返回的键值对会展开到行 div 上）</summary>
        [Parameter]
        public Func<object, Dictionary<string, object>> RowProps { get; set; }

        /// <summary>表头行 class（对齐 EP header-class 字符串形式）</summary>
        [Parameter]
        public string HeaderClass { get; set; }

        /// <summary>表头行 class（对齐 EP header-class 函数形式）</summary>
        [Parameter]
        public Func<TableV2HeaderClassContext, string> HeaderClassGetter { get; set; }

        /// <summary>滚动时给行加 is-scrolling 类（对齐 EP use-is-scrolling）</summary>
        [Parameter]
        public bool UseIsScrolling { get; set; }

        /// <summary>
        /// OnScroll 回调节流毫秒数（0 = 不节流，每次滚动都回调 —— 默认，保持 EP 语义）。
        /// <para>
        /// 为什么需要它：Blazor 的 <see cref="EventCallback"/> 在调用后会令**接收方**
        /// （通常是页面）执行 <c>StateHasChanged</c>。滚动时若页面里还有其它重组件，
        /// 每秒上百次的页面级重渲染会把「滚动 → 显示」拖成秒级卡顿/空白
        /// （2026-09-30 实测：本 demo 页 9 张表，不节流时每次滚动首帧 62~71ms，
        /// 节流后回落到单表重渲染的量级）。设成 ~150ms 即可既保留滚动日志，
        /// 又不掉帧。
        /// </para>
        /// </summary>
        [Parameter]
        public int ScrollThrottleMs { get; set; }

        // ===================== 页脚 / 遮罩 / 空态 =====================

        /// <summary>页脚高度 px（对齐 EP footer-height 默认 0；提供 FooterContent 时缺省取 50）</summary>
        [Parameter]
        public int FooterHeight { get; set; }

        /// <summary>页脚内容（对齐 EP footer 插槽）</summary>
        [Parameter]
        public RenderFragment FooterContent { get; set; }

        /// <summary>遮罩层内容（对齐 EP overlay 插槽，覆盖整个表格）</summary>
        [Parameter]
        public RenderFragment OverlayContent { get; set; }

        [Parameter]
        public bool Loading { get; set; }

        [Parameter]
        public string LoadingText { get; set; } = "加载中...";

        /// <summary>空态文案。留空时取词条 <c>el.table.emptyText</c>，再回落「暂无数据」。</summary>
        [Parameter]
        public string EmptyText { get; set; }

        /// <summary>空态最终取值：显式参数 &gt; 词条 &gt; 改动前的「暂无数据」。</summary>
        private string EffectiveEmptyText => Lang.Text("el.table.emptyText", EmptyText, "暂无数据");

        [Parameter]
        public RenderFragment LoadingContent { get; set; }

        [Parameter]
        public RenderFragment EmptyContent { get; set; }

        // ===================== 事件 =====================

        [Parameter]
        public EventCallback<object> OnRowClick { get; set; }

        [Parameter]
        public EventCallback<object> OnRowDoubleClick { get; set; }

        /// <summary>行右键事件（对齐 EP row-event-handlers.contextmenu）</summary>
        [Parameter]
        public EventCallback<object> OnRowContextClick { get; set; }

        [Parameter]
        public EventCallback<object> OnRowMouseEnter { get; set; }

        [Parameter]
        public EventCallback<object> OnRowMouseLeave { get; set; }

        [Parameter]
        public EventCallback<int> OnScroll { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        // ========== 虚拟滚动窗口（扩展：显式 StartIndex/ItemCount） ==========

        /// <summary>
        /// 虚拟滚动的起始行下标（0 基）。显式设置后**优先于**滚动位置自动推导，
        /// 取值范围会被约束到 [0, 行数-1]。留空则跟随滚动。
        /// </summary>
        [Parameter]
        public int? StartIndex { get; set; }

        /// <summary>
        /// 虚拟滚动一次渲染的行数。仅在与 <see cref="StartIndex"/> 一起显式指定时生效，
        /// 用于替代"按容器高度推算可视行数"的行为。
        /// </summary>
        [Parameter]
        public int? ItemCount { get; set; }

        protected int RowHeightSafe => Math.Max(20, RowHeight);
        protected int OverscanSafe => Math.Max(0, OverscanCount);

        /// <summary>
        /// 被 ellipsis 裁掉的单元格悬停浮出全文（本库扩展，默认关）。EP 的 table-v2 没有这条：
        /// <c>components/cell.tsx:14</c> 只无条件给被裁文本绑原生 <c>title</c>（本仓按 <see cref="CellTitle"/> 对齐）。
        /// 打开会给每个启用列的单元格加两层 span（触发 span + 文字 span），并把单元格的
        /// <c>overflow</c> 让给内层 —— 浮层必须待在裁剪盒外面，否则一起被裁掉（菜单那次踩过，见 docs/menu-alignment.md §9）。
        /// 虚拟滚动每帧都要重建这些实例，行数多的表请按需开。
        /// </summary>
        [Parameter]
        public bool EllipsisTooltip { get; set; }

        /// <summary>这一列的被裁单元格是否挂悬停浮层：列级覆盖表级。</summary>
        protected bool CellOverflowEnabled(TableV2Column column)
        {
            return column != null && (column.EllipsisTooltip ?? EllipsisTooltip);
        }

        /// <summary>
        /// EP <c>components/cell.tsx:11-14</c> 的原生 <c>title</c>：内容取单元格值的字符串形式，空白不给属性。
        /// 挂了浮层就不给 title，免得浏览器原生提示与 elTooltip 叠成两层。
        /// </summary>
        protected string CellTitle(object value, TableV2Column column)
        {
            if (CellOverflowEnabled(column))
            {
                return null;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        /// <summary>表体/固定行单元格的类名：挂浮层时多一个 <c>is-ellipsis-tooltip</c>（把 overflow 让给内层文字 span）。</summary>
        protected string CellClass(TableV2Column column, object row)
        {
            return CellOverflowEnabled(column)
                ? GetCellClass(column, row) + " is-ellipsis-tooltip"
                : GetCellClass(column, row);
        }

        /// <summary>汇总行单元格的类名，与 <see cref="CellClass"/> 同一套规则，但不掺行级 CellClassName。</summary>
        protected string SummaryCellClass(TableV2Column column)
        {
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add(GetColumnClass(column), "el-table-v2__cell")
                .AddIf(CellOverflowEnabled(column), "is-ellipsis-tooltip")
                .ToString();
        }

        /// <summary>实际生效的预渲染行数：滚动中追加 <see cref="ScrollingOverscanBoost"/> 缓冲，静止时回落到基础值。</summary>
        protected int EffectiveOverscan => _isScrolling ? OverscanSafe + Math.Max(0, ScrollingOverscanBoost) : OverscanSafe;

        protected IReadOnlyList<object> Rows => _rows;

        protected IReadOnlyList<object> FixedRows => _fixedRows;

        /// <summary>是否渲染旧版独立展开列（ExpandContent 模式且未指定 ExpandColumnKey）</summary>
        protected bool ShowLegacyExpandColumn => ExpandContent != null && string.IsNullOrEmpty(ExpandColumnKey);

        /// <summary>实际生效的起始行下标（<see cref="StartIndex"/> 优先，否则按滚动位置推导）。</summary>
        protected int EffectiveStartIndex
        {
            get
            {
                if (StartIndex.HasValue)
                {
                    return Math.Clamp(StartIndex.Value, 0, Math.Max(0, _rows.Count - 1));
                }
                var start = _scrollTop / RowHeightSafe;
                return Math.Max(0, start - EffectiveOverscan);
            }
        }

        protected int ComputeStartIndex(int totalCount)
        {
            if (totalCount == 0) return 0;
            if (StartIndex.HasValue)
            {
                return Math.Clamp(StartIndex.Value, 0, totalCount - 1);
            }
            var start = _scrollTop / RowHeightSafe;
            return Math.Max(0, Math.Min(start - EffectiveOverscan, totalCount - 1));
        }

        protected int ComputeEndIndex(int totalCount)
        {
            if (totalCount == 0)
            {
                return -1;
            }
            if (StartIndex.HasValue)
            {
                var count = Math.Max(1, ItemCount ?? 1);
                return Math.Min(totalCount - 1, StartIndex.Value + count - 1);
            }
            var containerH = GetContainerHeight();
            var visible = containerH / RowHeightSafe;
            var end = (_scrollTop / RowHeightSafe) + (int)Math.Ceiling((double)visible) + EffectiveOverscan;
            return Math.Min(totalCount - 1, end);
        }

        protected int TotalBodyHeight => _rows.Count * RowHeightSafe;

        /// <summary>每块占位 padding 把未渲染的行撑出正确高度</summary>
        protected string BuildBodyPaddingStyle(int start, int end, int total)
        {
            var topPad = start * RowHeightSafe;
            var bottomPad = Math.Max(0, (total - 1 - end) * RowHeightSafe);
            return HtmlPropertyBuilder.CreateCssStyleBuilder()
                .Add($"padding-top:{topPad}px")
                .Add($"padding-bottom:{bottomPad}px")
                .ToString();
        }

        protected string TableClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-table-v2", "el-table-v2__root", "el-table", Cls)
            .AddIf(Border, "el-table--border")
            .AddIf(Stripe, "el-table--striped")
            .AddIf(HighlightCurrentRow, "el-table--highlight-current-row")
            .ToString();

        protected string TableStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(!string.IsNullOrWhiteSpace(Height), $"height:{ElementCssUtility.NormalizeCssSize(Height)}")
            .AddIf(!string.IsNullOrWhiteSpace(Width), $"width:{ElementCssUtility.NormalizeCssSize(Width)}")
            .Add(Style)
            .ToString();

        protected string HeaderRowClass
        {
            get
            {
                var fromFunc = HeaderClassGetter?.Invoke(new TableV2HeaderClassContext
                {
                    Columns = Columns != null ? Columns.ToList() : new List<TableV2Column>(),
                    HeaderIndex = 0
                });
                return HtmlPropertyBuilder.CreateCssClassBuilder()
                    .Add("el-table-v2__header-row")
                    .Add(string.IsNullOrWhiteSpace(HeaderClass) ? fromFunc : HeaderClass)
                    .ToString();
            }
        }

        // ========== 生命周期 ==========

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            RebuildRowsCache();
            RebuildFixedRowsCache();

            if ((Columns == null || Columns.Count == 0) && AutoGenerateColumns)
            {
                Columns = CreateColumns().ToList();
            }

            SeedExpandedKeys();
            // 画廊等场景中 AutoGenerateColumns 可能被置为 false 且无显式 Columns，
            // 此处兜底保证 Columns 永不为 null（否则首屏 @foreach (var column in Columns) 抛 NRE）。
            Columns ??= new List<TableV2Column>();
        }

        /// <summary>数据源 → 内部行列表（仅在数据源引用或条数变化时重建）</summary>
        private void RebuildRowsCache()
        {
            // 非 ICollection（惰性查询）拿不到条数 ⇒ 保守地每次重建，避免缓存到过期结果
            var count = DataSource is ICollection collection ? collection.Count : -1;
            if (count >= 0 && ReferenceEquals(_rowsSource, DataSource) && _rowsSourceCount == count)
            {
                return;
            }
            _rowsSource = DataSource;
            _rowsSourceCount = count;
            _rows = DataSource is IEnumerable enumerable
                ? enumerable.Cast<object>().ToList()
                : new List<object>();
        }

        private void RebuildFixedRowsCache()
        {
            var count = FixedData is ICollection collection ? collection.Count : -1;
            if (count >= 0 && ReferenceEquals(_fixedRowsSource, FixedData) && _fixedRowsSourceCount == count)
            {
                return;
            }
            _fixedRowsSource = FixedData;
            _fixedRowsSourceCount = count;
            _fixedRows = FixedData is IEnumerable enumerable
                ? enumerable.Cast<object>().ToList()
                : new List<object>();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender && !_moduleLoaded)
            {
                try
                {
                    _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                        "import", "./_content/Element-Plus/js/dom.js");
                    _moduleLoaded = true;
                    if (_moduleLoaded && _jsModule != null)
                    {
                        // 绑定 rAF 节流的滚动监听（替代原生 @onscroll，消除快速滚动空白/停滞）
                        _dotNetRef = DotNetObjectReference.Create(this);
                        await JSRuntime.InvokeVoidAsync("elementTableV2BindScroll", _rootRef, _dotNetRef);
                    }
                }
                catch { _moduleLoaded = false; }
            }
        }

        private void SeedExpandedKeys()
        {
            if (_defaultsSeeded) return;
            _defaultsSeeded = true;
            if (ExpandedRows == null)
            {
                ExpandedRows = new HashSet<object>();
            }
            if (DefaultExpandAllRows)
            {
                foreach (var row in _rows)
                {
                    var key = GetRowKey(row);
                    if (key != null && (GetChildren(row) != null || ExpandContent != null))
                    {
                        ExpandedRows.Add(key);
                    }
                }
            }
            else if (DefaultExpandRowKeys != null)
            {
                foreach (var key in DefaultExpandRowKeys)
                {
                    if (key != null) ExpandedRows.Add(key);
                }
            }
            _expansionVersion++;
        }

        // ========== 扁平化（树形数据 → 线性序列） ==========

        /// <summary>把数据扁平化：树形 children 按 ExpandedRows 展开为线性序列，普通数据 Depth=0。</summary>
        /// <remarks>结果按「数据源 / 行数 / 树字段 / rowKey / 展开态 / 展开集合引用」缓存，
        /// 滚动重渲染时直接命中缓存，不再逐帧重算（见字段区性能缓存注释）。</remarks>
        protected IReadOnlyList<TableV2FlatRow> BuildFlatRows()
        {
            if (FlatCacheValid())
            {
                return _flatCache;
            }

            var list = new List<TableV2FlatRow>();
            if (ExpandedRows == null) ExpandedRows = new HashSet<object>();
            foreach (var row in _rows)
            {
                FlattenRow(row, 0, list);
            }
            for (var i = 0; i < list.Count; i++)
            {
                list[i].Index = i;
            }

            _flatCache = list;
            _flatCacheDataSource = DataSource;
            _flatCacheRowCount = _rows.Count;
            _flatCacheChildrenField = TreeChildrenField;
            _flatCacheRowKey = RowKey;
            _flatCacheHasExpandContent = ExpandContent != null;
            _flatCacheExpandedRows = ExpandedRows;
            _flatCacheVersion = _expansionVersion;
            return list;
        }

        private bool FlatCacheValid()
        {
            return _flatCache != null
                && ReferenceEquals(_flatCacheDataSource, DataSource)
                && _flatCacheRowCount == _rows.Count
                && string.Equals(_flatCacheChildrenField, TreeChildrenField, StringComparison.Ordinal)
                && string.Equals(_flatCacheRowKey, RowKey, StringComparison.Ordinal)
                && _flatCacheHasExpandContent == (ExpandContent != null)
                && ReferenceEquals(_flatCacheExpandedRows, ExpandedRows)
                && _flatCacheVersion == _expansionVersion;
        }

        private void FlattenRow(object row, int depth, List<TableV2FlatRow> list)
        {
            if (row == null || depth > 32) return;
            var key = GetRowKey(row);
            var children = GetChildren(row);
            var expandable = children != null || ExpandContent != null;
            var expanded = expandable && ExpandedRows.Contains(key);
            list.Add(new TableV2FlatRow
            {
                Row = row,
                Depth = depth,
                Key = key,
                Expandable = expandable,
                IsExpanded = expanded
            });
            if (children != null && expanded)
            {
                foreach (var child in children)
                {
                    FlattenRow(child, depth + 1, list);
                }
            }
        }

        protected object GetRowKey(object row)
        {
            if (row == null) return null;
            var name = string.IsNullOrWhiteSpace(RowKey) ? "id" : RowKey;
            var value = ResolveProperty(row.GetType(), name)?.GetValue(row);
            return value ?? row;
        }

        protected IEnumerable<object> GetChildren(object row)
        {
            if (row == null || string.IsNullOrWhiteSpace(TreeChildrenField)) return null;
            // 先精确匹配，再忽略大小写回退（EP 默认 'children'，C# 属性常为 'Children'）
            var value = ResolveProperty(row.GetType(), TreeChildrenField)?.GetValue(row);
            if (value is IEnumerable children && !(value is string))
            {
                var list = children.Cast<object>().Where(x => x != null).ToList();
                return list.Count > 0 ? list : null;
            }
            return null;
        }

        /// <summary>
        /// 反射取属性（含「精确 → 忽略大小写」回退），结果按类型+属性名永久缓存。
        /// 逐行反射是滚动卡顿的主因之一（400 行 × 2 次/帧），缓存后每类型只算一次。
        /// </summary>
        private static PropertyInfo ResolveProperty(Type type, string name)
        {
            if (type == null || string.IsNullOrWhiteSpace(name)) return null;
            var key = type.FullName + "|" + name;
            lock (s_propertyCache)
            {
                if (s_propertyCache.TryGetValue(key, out var cached))
                {
                    return cached;
                }
            }

            var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public)
                    ?? type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            lock (s_propertyCache)
            {
                s_propertyCache[key] = prop;
            }
            return prop;
        }

        // ========== 排序 ==========

        protected string ColumnKey(TableV2Column column) => column?.Key ?? column?.Property;

        /// <summary>排序方向真源：SortBy（受控或内部）优先，其次 SortState。</summary>
        protected TableV2SortOrder GetColumnOrder(TableV2Column column)
        {
            var key = ColumnKey(column);
            var by = SortBy ?? _sortByInternal;
            if (by != null && by.Key == key && by.Order != TableV2SortOrder.None)
            {
                return by.Order;
            }
            if (SortState != null && key != null && SortState.TryGetValue(key, out var order))
            {
                return order;
            }
            return TableV2SortOrder.None;
        }

        protected string GetSortClass(TableV2Column column)
        {
            return GetColumnOrder(column) switch
            {
                TableV2SortOrder.Asc => "el-table-v2__sort-icon sort-asc",
                TableV2SortOrder.Desc => "el-table-v2__sort-icon sort-desc",
                _ => "el-table-v2__sort-icon"
            };
        }

        /// <summary>表头点击排序：None→Asc→Desc→Asc 循环；只发事件，由调用方排序数据（对齐 EP）。</summary>
        protected async Task OnHeaderSortAsync(TableV2Column column)
        {
            if (column == null || !column.Sortable) return;
            var key = ColumnKey(column);
            var current = GetColumnOrder(column);
            var next = current switch
            {
                TableV2SortOrder.Asc => TableV2SortOrder.Desc,
                TableV2SortOrder.Desc => TableV2SortOrder.Asc,
                _ => TableV2SortOrder.Asc
            };
            if (SortBy != null)
            {
                await SortByChanged.InvokeAsync(new TableV2SortBy(key, next));
            }
            else
            {
                _sortByInternal = new TableV2SortBy(key, next);
                _selfDirty = true;
                StateHasChanged();
            }
            await ColumnSort.InvokeAsync(new TableV2ColumnSortArgs
            {
                Column = column,
                Key = key,
                Order = next
            });
        }

        // ========== 展开 ==========

        protected async Task ToggleExpandAsync(TableV2FlatRow flat)
        {
            if (flat == null || !flat.Expandable) return;
            if (ExpandedRows == null) ExpandedRows = new HashSet<object>();
            if (ExpandedRows.Contains(flat.Key))
            {
                ExpandedRows.Remove(flat.Key);
            }
            else
            {
                ExpandedRows.Add(flat.Key);
            }
            // 展开态变化 ⇒ 扁平化缓存失效（下次渲染重算一次）
            _expansionVersion++;
            _selfDirty = true;
            await ExpandedRowsChange.InvokeAsync(ExpandedRows.ToList());
            StateHasChanged();
        }

        protected bool IsExpandColumn(TableV2Column column)
        {
            if (string.IsNullOrEmpty(ExpandColumnKey) || column == null) return false;
            return column.Key == ExpandColumnKey || column.Property == ExpandColumnKey;
        }

        // ========== 样式辅助 ==========

        protected int GetContainerHeight()
        {
            if (string.IsNullOrWhiteSpace(Height)) return 500;
            var t = Height.Trim();
            if (t.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            {
                t = t.Substring(0, t.Length - 2);
            }
            return int.TryParse(t.Trim(), out var h) ? h : 500;
        }

        private static string FormatNum(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>列样式：显式 Width 时锁定 flex-basis（修复 flex:1 0 120px 吞掉 Width 的问题）</summary>
        /// <param name="forHeader">
        /// true = 表头单元格。表头是 flex 容器，text-align 管不了横向对齐，
        /// 需额外下发 justify-content；表体是块级盒，多这一条也无副作用。
        /// </param>
        protected string GetColumnStyle(TableV2Column column, bool forHeader = false)
        {
            if (column == null) return null;
            var b = HtmlPropertyBuilder.CreateCssStyleBuilder();
            if (!string.IsNullOrWhiteSpace(column.Width))
            {
                var w = ElementCssUtility.NormalizeCssSize(column.Width);
                b.Add($"width:{w}");
                if (column.FlexGrow.HasValue)
                {
                    b.Add($"flex:{FormatNum(column.FlexGrow.Value)} {FormatNum(column.FlexShrink ?? 0)} {w}");
                }
                else
                {
                    b.Add($"flex:0 0 {w}");
                }
            }
            else if (column.FlexGrow.HasValue)
            {
                b.Add($"flex:{FormatNum(column.FlexGrow.Value)} {FormatNum(column.FlexShrink ?? 1)} auto");
            }
            b.AddIf(!string.IsNullOrWhiteSpace(column.MinWidth), $"min-width:{ElementCssUtility.NormalizeCssSize(column.MinWidth)}");
            b.AddIf(!string.IsNullOrWhiteSpace(column.MaxWidth), $"max-width:{ElementCssUtility.NormalizeCssSize(column.MaxWidth)}");
            b.AddIf(!string.IsNullOrWhiteSpace(column.Align), $"text-align:{column.Align}");
            if (forHeader && !string.IsNullOrWhiteSpace(column.Align))
            {
                b.Add($"justify-content:{AlignToJustifyContent(column.Align)}");
            }
            return b.ToString();
        }

        /// <summary>把列对齐（left/center/right）映射成 flex 容器的 justify-content（表头用）</summary>
        private static string AlignToJustifyContent(string align)
        {
            switch (align.Trim().ToLowerInvariant())
            {
                case "center": return "center";
                case "right":
                case "end": return "flex-end";
                default: return "flex-start";
            }
        }

        protected string GetColumnClass(TableV2Column column)
        {
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add(column?.ClassName)
                .AddIf(!string.IsNullOrWhiteSpace(column?.Fixed), $"is-fixed-{column.Fixed}")
                .ToString();
        }

        /// <summary>
        /// 表头单元格 class：与表体单元格共用 .el-table-v2__cell 盒模型
        /// （同一套 flex/width/padding/border），保证表头与表体列宽严格对齐。
        /// </summary>
        protected string GetHeaderCellClass(TableV2Column column)
        {
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-table-v2__cell", "el-table-v2__header-cell")
                .AddIf(column?.Sortable == true, "el-table-v2__header-cell-sortable")
                .Add(column?.HeaderCellClassName)
                .Add(GetColumnClass(column))
                .ToString();
        }

        protected string GetRowClass(TableV2FlatRow flat, int visibleIndex)
        {
            var rowIndex = EffectiveStartIndex + visibleIndex;
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-table-v2__row")
                .AddIf(Stripe && rowIndex % 2 == 1, "el-table__row--striped")
                .AddIf(HighlightCurrentRow && rowIndex == CurrentRowIndex, "el-table__row current-row")
                .AddIf(UseIsScrolling && _isScrolling, "is-scrolling")
                .Add(RowClassName?.Invoke(flat.Row))
                .Add(RowClassGetter?.Invoke(new TableV2RowClassContext
                {
                    Columns = Columns != null ? Columns.ToList() : new List<TableV2Column>(),
                    Row = flat.Row,
                    RowIndex = rowIndex
                }))
                .Add(RowClass)
                .ToString();
        }

        protected string GetRowCellStyle(object row)
        {
            return RowStyle?.Invoke(row);
        }

        protected Dictionary<string, object> GetRowAttributes(object row)
        {
            return RowProps?.Invoke(row) ?? new Dictionary<string, object>();
        }

        protected string GetCellClass(TableV2Column column, object row)
        {
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-table-v2__cell")
                .Add(column?.CellClassName?.Invoke(row))
                .Add(GetColumnClass(column))
                .ToString();
        }

        // ========== 交互 ==========

        /// <summary>
        /// 由 JS 侧 rAF 节流后回调（见 dom.js elementTableV2BindScroll）。
        /// 直接传入该帧最新的 scrollTop/Left，避免每次原生滚动事件都做 JSRuntime 回读 + StateHasChanged，
        /// 从而消除高频滚动时的渲染排队与 _scrollTop 滞后（空白/停滞根因）。
        /// </summary>
        [JSInvokable]
        public void OnTableV2Scroll(double top, double left)
        {
            // 用 double 接收：非整数 devicePixelRatio / 页面缩放下 scrollTop 可能是小数，
            // 若签名写 int，System.Text.Json 反序列化会抛 FormatException 导致整次回调失败
            //（表现：滚动时 DOM 不更新 → 空白/停滞）。此处再统一取整。
            _scrollTop = double.IsNaN(top) || double.IsInfinity(top) ? _scrollTop : (int)Math.Round(top);
            _scrollLeft = double.IsNaN(left) || double.IsInfinity(left) ? _scrollLeft : (int)Math.Round(left);
            // 始终追踪滚动活动态：① 滚动中动态放大 overscan 缓冲（消除极端快速滚动空白）；
            // ② 若调用方开启 UseIsScrolling，则附加 is-scrolling 样式类（门控见 GetRowClass）。
            if (!_isScrolling)
            {
                _isScrolling = true;
                _ = ResetScrollingAsync();
            }
            _selfDirty = true;
            StateHasChanged();
            QueueScrollNotify(_scrollTop);
        }

        // ===== OnScroll 回调（可选节流） =====

        private int _pendingNotifyTop;
        private int _lastNotifiedTop = int.MinValue;
        private bool _notifyLoopRunning;

        private void QueueScrollNotify(int top)
        {
            _pendingNotifyTop = top;
            if (ScrollThrottleMs <= 0)
            {
                _ = OnScroll.InvokeAsync(top);
                return;
            }
            if (_notifyLoopRunning)
            {
                return;
            }
            _notifyLoopRunning = true;
            _ = RunScrollNotifyLoopAsync();
        }

        /// <summary>节流循环：窗口内只保留最后一次位置，静止后补发一次即退出。</summary>
        private async Task RunScrollNotifyLoopAsync()
        {
            try
            {
                while (!_disposed)
                {
                    await Task.Delay(Math.Max(16, ScrollThrottleMs));
                    if (_disposed || _pendingNotifyTop == _lastNotifiedTop)
                    {
                        return;
                    }
                    _lastNotifiedTop = _pendingNotifyTop;
                    await InvokeAsync(() => OnScroll.InvokeAsync(_lastNotifiedTop));
                }
            }
            catch { }
            finally
            {
                _notifyLoopRunning = false;
                if (!_disposed && _pendingNotifyTop != _lastNotifiedTop)
                {
                    QueueScrollNotify(_pendingNotifyTop);
                }
            }
        }

        private async Task ResetScrollingAsync()
        {
            try
            {
                await Task.Delay(500);
                if (_disposed) return;
                _isScrolling = false;
                _selfDirty = true;
                await InvokeAsync(StateHasChanged);
            }
            catch { }
        }

        protected async Task OnRowClickedAsync(object row)
        {
            await OnRowClick.InvokeAsync(row);
        }

        protected async Task OnRowDoubleClickedAsync(object row)
        {
            await OnRowDoubleClick.InvokeAsync(row);
        }

        protected async Task OnRowContextClickedAsync(object row)
        {
            await OnRowContextClick.InvokeAsync(row);
        }

        protected async Task OnRowMouseEnteredAsync(object row)
        {
            await OnRowMouseEnter.InvokeAsync(row);
        }

        protected async Task OnRowMouseLeftAsync(object row)
        {
            await OnRowMouseLeave.InvokeAsync(row);
        }

        // ========== Exposes（对齐 EP scrollTo / scrollToTop / scrollToLeft / scrollToRow） ==========

        /// <summary>滚动到指定位置（对齐 EP scrollTo({ scrollTop, scrollLeft })）</summary>
        public Task ScrollToAsync(double scrollTop, double scrollLeft = 0)
        {
            return ApplyScrollAsync(scrollTop, scrollLeft);
        }

        /// <summary>滚动到顶部（对齐 EP scrollToTop()）</summary>
        public Task ScrollToTopAsync()
        {
            return ApplyScrollAsync(0, null);
        }

        /// <summary>横向滚动到指定位置，纵向保持不变（对齐 EP scrollToLeft()）</summary>
        public Task ScrollToLeftAsync(double scrollLeft)
        {
            return ApplyScrollAsync(null, scrollLeft);
        }

        /// <summary>滚动到指定行（对齐 EP scrollToRow(row)）</summary>
        public Task ScrollToRowAsync(int rowIndex)
        {
            return ApplyScrollAsync(Math.Max(0, rowIndex) * (double)RowHeightSafe, null);
        }

        private async Task ApplyScrollAsync(double? scrollTop, double? scrollLeft)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("elementTableV2ScrollTo", _rootRef, scrollTop, scrollLeft);
                if (scrollTop.HasValue)
                {
                    _scrollTop = (int)scrollTop.Value;
                    QueueScrollNotify(_scrollTop);
                    _selfDirty = true;
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch { /* bUnit / 预渲染下无 JS */ }
        }

        // ========== 汇总 ==========

        protected object GetSummaryCell(TableV2Column column, int index)
        {
            if (index == 0)
            {
                return SumText;
            }

            if (SummaryMethod != null)
            {
                return SummaryMethod(new TableSummaryContext
                {
                    Rows = _rows,
                    Columns = new List<TableHeader>(),
                    ColumnIndex = index
                });
            }

            decimal total = 0;
            var hasValue = false;
            foreach (var row in _rows)
            {
                var value = column.GetValue(row);
                if (decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                {
                    total += number;
                    hasValue = true;
                }
            }

            return hasValue ? total.ToString(CultureInfo.CurrentCulture) : string.Empty;
        }

        private IEnumerable<TableV2Column> CreateColumns()
        {
            var row = _rows.FirstOrDefault();
            var type = row?.GetType();
            if (type == null)
            {
                return Enumerable.Empty<TableV2Column>();
            }

            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => !x.GetCustomAttributes(true).OfType<TableIgnoreAttribute>().Any())
                .Select(x => new TableV2Column
                {
                    Key = x.Name,
                    Title = x.Name,
                    Property = x.Name
                });
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                if (_jsModule != null) await _jsModule.DisposeAsync();
                // 解绑 rAF 滚动监听 + 释放 DotNet 引用（避免内存泄漏 / 悬空回调）
                await JSRuntime.InvokeVoidAsync("elementTableV2UnbindScroll", _rootRef);
                _dotNetRef?.Dispose();
            }
            catch { }
        }
    }
}
