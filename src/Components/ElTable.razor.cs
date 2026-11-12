using ElementPlus.ControlRender;
using ElementPlus.ControlRenders;
using ElementPlus.DisplayRenders;
using ElementPlus.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElTable : ElementComponentBase, IContainerComponent, IAsyncDisposable
    {
        private SaveAction saveAction = SaveAction.Create;
        private TaskCompletionSource<int> editingTaskCompletionSource;
        private TaskCompletionSource<int> loadTaskCompletionSource;
        private bool dataSourceUpdated = false;
        private ElButton saveAddButton;
        private bool renderCompleted = false;
        private ElementReference editingRowEl;
        internal ElCheckbox<bool> chkAll;
        internal ElementReference headerElement;
        internal ElementReference bodyWrapperElement;
        private readonly string tableDomId = "eltbl-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        private const string EditingCellClass = "el-table__cell--editing";
        private static readonly Dictionary<string, object> EmptyCellAttributes = new Dictionary<string, object>();
        private object editingRowField;
        private object editingRow
        {
            get => editingRowField;
            set
            {
                editingRowField = value;
                if (value == null)
                {
                    // 退出编辑状态时清空活动单元格
                    editingCellIndex = -1;
                    activeEditingHeader = null;
                    pendingFocusCell = null;
                }
            }
        }
        private int editingCellIndex = -1;
        private TableHeader activeEditingHeader;
        private (object Row, TableHeader Header, bool SelectText)? pendingFocusCell;

        /// <summary>
        /// 主键字段
        /// </summary>
        [Parameter]
        public string Key { get; set; }

        [Inject]
        private DisplayRenderFactory displayRender { get; set; }
        internal List<TableHeader> Headers { get; set; } = new List<TableHeader>();

        internal bool headerInitilized = false;
        internal bool headerRendered = false;

        /// <summary>
        /// 已注册列的稳定签名集合，用于 <see cref="AddColumn(ElTableColumn)"/> 去重。
        /// 表格因 Refresh/MarkAsRequireRender 重渲染时，列组件的 OnInitialized 可能被再次触发并重复 AddColumn，
        /// 导致 Headers 出现重复表头，进而让 &lt;col&gt;/&lt;th&gt;/&lt;td&gt; 上 @key="header.Text" 触发 “same key value” 渲染异常。
        /// 以“组件类型 + 属性 + 文本 + 是否模板列”作为签名，同一列无论实例是否被重建都只注册一次。
        /// </summary>
        private HashSet<string> registeredColumnSignatures = new HashSet<string>();
        private IDictionary<ElCheckbox<bool>, object> rowCheckBoxses = new Dictionary<ElCheckbox<bool>, object>();
        internal int headerHeight = 49;
        /// <summary>AutoFitHeight 由 JS 测量回传的高度（null = 尚未测量 / 未启用）。</summary>
        private int? autoFitHeight;
        private bool autoFitBound;
        private DotNetObjectReference<ElTable> autoFitDotNetRef;

        /// <summary>
        /// 实际生效的固定高度：手动 Height 优先；否则在 AutoFitHeight 且无 MaxHeight 时用 JS 实测值。
        /// 所有样式判定都走这里，保证手动设定与自动撑满互不打架。
        /// </summary>
        internal int EffectiveHeight =>
            Height > 0 ? Height :
            (AutoFitHeight && MaxHeight <= 0 && autoFitHeight.HasValue ? autoFitHeight.Value : 0);
        internal List<object> rows = new List<object>();
        internal List<object> hiddenRows = new List<object>();
        private List<object> sourceRows = new List<object>();
        private TableHeader activeSortHeader;
        /// <summary>当前展开筛选面板的列，同一时刻只有一列（为空表示没有面板开着）。</summary>
        private TableHeader openFilterHeader;
        private string filterPanelStyle;
        /// <summary>每次重新打开面板自增，让 <see cref="ElTableFilterPanel"/> 以新 @key 重建、草稿从列的现值重取。</summary>
        private int filterPanelRenderKey;

        internal void AddColumn(ElTableColumn column)
        {
            AddColumnInternal(column, null);
        }

        /// <summary>注册一个分组表头（两层表头的第一行），它本身不进 <see cref="Headers"/>。</summary>
        internal TableHeader AddGroupColumn(ElTableColumn column)
        {
            var signature = $"group|{column.GetType().FullName}|{column.Text}";
            var existing = groupHeaders.FirstOrDefault(x => x.Text == column.Text);
            registeredColumnSignatures.Add(signature);
            if (existing != null)
            {
                return existing;
            }
            var group = new TableHeader
            {
                Text = column.Text,
                Align = column.Align,
                HeaderAlign = column.HeaderAlign,
                IsGroup = true
            };
            groupHeaders.Add(group);
            return group;
        }

        /// <summary>把叶子列登记到某个分组表头下：仍进 <see cref="Headers"/>（colgroup 与表体按叶子算），另外挂到分组上。</summary>
        internal void AddColumnToGroup(ElTableColumn column, TableHeader group)
        {
            AddColumnInternal(column, group);
        }

        /// <summary>已登记的分组表头，只用于「同标题分组复用」和清理；两行表头的渲染顺序由 <see cref="TopLevelHeaders"/> 从 <see cref="Headers"/> 推导。</summary>
        private readonly List<TableHeader> groupHeaders = new List<TableHeader>();

        /// <summary>是否存在分组表头。没有分组时表头沿用原来的单行渲染路径。</summary>
        protected internal bool HasColumnGroups => Headers.Any(x => x.Parent != null);

        /// <summary>叶子列在 <see cref="Headers"/> 中的 1-based 序号，供表头类名/样式与固定列偏移复用。</summary>
        protected internal int LeafColumnIndex(TableHeader leaf)
        {
            return Headers.IndexOf(leaf) + 1;
        }

        /// <summary>
        /// 两行表头第一行的单元格：顶层列与分组表头按 <see cref="Headers"/> 的叶子顺序各出现一次。
        /// 从叶子顺序反推（而不是按声明顺序记）能保证第一行的 colspan 与 colgroup／表体的列顺序始终对齐。
        /// </summary>
        protected internal List<TableHeader> TopLevelHeaders()
        {
            var result = new List<TableHeader>();
            var addedGroups = new HashSet<TableHeader>();
            foreach (var leaf in Headers)
            {
                if (leaf.Ignore)
                {
                    continue;
                }
                var top = leaf.Parent;
                if (top == null)
                {
                    result.Add(leaf);
                    continue;
                }
                if (addedGroups.Add(top))
                {
                    result.Add(top);
                }
            }
            return result;
        }

        private void AddColumnInternal(ElTableColumn column, TableHeader group)
        {
            // 去重：表格重渲染（Refresh / MarkAsRequireRender 会把 headerInitilized 置 false，
            // 触发列组件 OnInitialized 再次执行）时同一列会重复 AddColumn。以稳定签名拦截，避免 Headers 出现重复表头。
            // 分组下的子列标题会在各分组间重复（交叉报表的「数量/金额」），签名必须带上所属分组标题，否则第二个分组之后的子列会被当成重复而丢掉。
            var signature = $"{column.GetType().FullName}|{column.Property}|{column.Text}|{column.Type}|{column is ElTableTemplateColumn}"
                + (group == null ? string.Empty : $"|group:{group.Text}");
            if (registeredColumnSignatures.Contains(signature))
            {
                return;
            }

            // 显式声明的 <ElTableColumn /> 优先：首个声明列注册时丢弃反射自动生成的列，
            // 否则会出现“自动列 + 声明列”两份表头（例如 Name 与 名称 同时出现在一张表里）。
            if (Headers.Any(x => x.IsAutoGenerated))
            {
                Headers.RemoveAll(x => x.IsAutoGenerated);
            }

            if (DataType == null)
            {
                throw new ElementException($"风格 {GetType().Name} 没有设置 {nameof(DataSource)} 属性或该属性为空");
            }
            // 模板列（ElTableTemplateColumn 或带 ChildContent 的列）不强制要求 Property：
            // 官方 EP 的 <el-table-column> 允许只写插槽不写 prop（典型场景：操作列 / 纯自定义渲染列）。
            var isTemplateColumn = column is ElTableTemplateColumn || column.ChildContent != null;
            if (column.Property == null && !isTemplateColumn
                && column.Type != "index" && column.Type != "selection")
            {
                throw new ElementException($"列 {column.Text} 没有设置 {nameof(ElTableColumn.Property)} 属性");
            }

            PropertyInfo property = null;
            if (!string.IsNullOrWhiteSpace(column.Property))
            {
                property = DataType.GetProperty(column.Property);
                if (property == null)
                {
                    throw new ElementException($"属性 {column.Property} 在 {DataType.Name} 中不存在");
                }
            }

            var columnConfig = new TableHeader
            {
                EvalRaw = row => property?.GetValue(row),
                Property = property,
                Text = column.Text,
                SortNo = column.SortNo,
                Width = column.Width,
                IsCheckBox = column.IsCheckBox,
                Template = column.ChildContent,
                Format = column.Format,
                IsTree = column.IsTree,
                IsEditable = column.IsEditable,
                EnterMovesToNextCell = column.EnterMovesToNextCell,
                Sortable = column.Sortable,
                SortOrder = column.SortOrder,
                Fixed = column.Fixed,
                Filters = column.Filters,
                FilterMethod = column.FilterMethod,
                FilterMultiple = column.FilterMultiple,
                AdvancedFilter = column.AdvancedFilter,
                Type = column.Type,
                MinWidth = column.MinWidth,
                Align = column.Align,
                HeaderAlign = column.HeaderAlign,
                ShowOverflowTooltip = column.ShowOverflowTooltip,
                ClassName = column.ClassName,
                LabelClassName = column.LabelClassName,
                Formatter = column.Formatter,
                Selectable = column.Selectable,
                ReserveSelection = column.ReserveSelection,
                IndexMethod = column.IndexMethod,
                HeaderContent = column.Header,
                EditorType = column.Editor,
                EditorDataSource = column.EditorDataSource,
                EditorValueField = column.EditorValueField,
                EditorTextField = column.EditorTextField,
                EditorColumns = column.EditorColumns,
                EditorCloseOnDatePick = column.EditorCloseOnDatePick,
                // 列级 Resizable 优先于表级（bool? 归并成 bool）；RHS 裸名 Resizable 同名但处于
                // 初始化器表达式上下文，解析到的是 ElTable 自身的表级参数。
                Resizable = column.Resizable ?? Resizable,
                OnHeaderClick = column.OnHeaderClick
            };
            if (columnConfig.Property != null)
            {
                columnConfig.Eval = displayRender.CreateRenderFactory(columnConfig)?.CreateRender(columnConfig);
            }
            Headers.Add(columnConfig);
            registeredColumnSignatures.Add(signature);
            if (group != null)
            {
                columnConfig.Parent = group;
                group.Children.Add(columnConfig);
            }
        }

        /// <summary>
        /// 当前已注册的列（只读快照）。动态增删列后可用于核对结果。
        /// </summary>
        public IReadOnlyList<TableHeader> Columns => Headers;

        /// <summary>
        /// 动态添加一列，列定义由调用方提供，列值按 <see cref="TableHeader.Eval"/> 立即求值显示，无需重新加载数据。
        /// </summary>
        /// <param name="header">列定义，至少应设置 Text 与 Eval（或 Template）。</param>
        public void AddColumn(TableHeader header)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            Headers.Add(header);
            ApplyColumnChanges();
        }

        /// <summary>
        /// 动态添加多列，按顺序追加到列集合末尾。
        /// </summary>
        public void AddColumns(params TableHeader[] headers)
        {
            if (headers == null)
            {
                return;
            }

            foreach (var header in headers)
            {
                if (header != null)
                {
                    Headers.Add(header);
                }
            }

            ApplyColumnChanges();
        }

        /// <summary>
        /// 按属性名动态添加一列（属性类型取自 <see cref="DataType"/> 反射取属性），立即生效。
        /// </summary>
        public void AddColumn(string property, string text = null, int sortNo = 0)
        {
            if (DataType == null)
            {
                throw new ElementException($"风格 {GetType().Name} 没有设置 {nameof(DataSource)} 属性或该属性为空");
            }

            var propertyInfo = DataType.GetProperty(property);
            if (propertyInfo == null)
            {
                throw new ElementException($"属性 {property} 在 {DataType.Name} 中不存在");
            }

            // 同一属性只保留一列，避免运行期重复调用造成列重复
            if (Headers.Any(x => x.Property?.Name == property))
            {
                return;
            }

            var columnConfig = new TableHeader
            {
                EvalRaw = row => propertyInfo.GetValue(row),
                Property = propertyInfo,
                Text = text ?? property,
                SortNo = sortNo
            };
            columnConfig.Eval = displayRender.CreateRenderFactory(columnConfig)?.CreateRender(columnConfig);
            AddColumn(columnConfig);
        }

        /// <summary>
        /// 动态添加一列，列值由委托实时取值。适用于与实体属性无关的计算列。
        /// </summary>
        public void AddColumn(string text, Func<object, object> eval, int sortNo = 0)
        {
            if (eval == null)
            {
                throw new ArgumentNullException(nameof(eval));
            }

            AddColumn(new TableHeader
            {
                Text = text,
                Eval = eval,
                EvalRaw = eval,
                SortNo = sortNo
            });
        }

        /// <summary>
        /// 动态添加一列，列内容由模板渲染。
        /// </summary>
        public void AddColumn(string text, RenderFragment<object> template, int sortNo = 0)
        {
            AddColumn(new TableHeader
            {
                Text = text,
                Template = template,
                SortNo = sortNo
            });
        }

        /// <summary>
        /// 移除满足条件的第一个列并立即重新渲染。
        /// </summary>
        public bool RemoveColumn(Func<TableHeader, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            var target = Headers.FirstOrDefault(predicate);
            if (target == null)
            {
                return false;
            }

            Headers.Remove(target);
            // 叶子列被移除后要从所属分组摘掉，否则两行表头的第一行会按旧的孩子数算 colspan 而错位。
            target.Parent?.Children.Remove(target);
            ApplyColumnChanges();
            return true;
        }

        /// <summary>
        /// 按列标题移除列并立即重新渲染。
        /// </summary>
        public bool RemoveColumn(string text)
        {
            return RemoveColumn(x => string.Equals(x.Text, text, StringComparison.Ordinal));
        }

        /// <summary>丢弃全部分组表头：列集合重建时分组的孩子必须重取，否则会残留已删除的叶子列。</summary>
        private void ClearGroups()
        {
            foreach (var group in groupHeaders)
            {
                group.Children.Clear();
            }
            groupHeaders.Clear();
        }

        /// <summary>
        /// 清空所有列（含声明式列），用于完全重建列集合。
        /// </summary>
        public void ClearColumns()
        {
            Headers.Clear();
            registeredColumnSignatures.Clear();
            ClearGroups();
            ApplyColumnChanges();
        }

        /// <summary>
        /// 重新从 <see cref="ChildContent"/> 收集列定义。用于以 <c>@@foreach</c> 声明式动态增删列的场景：
        /// 修改列集合后调用本方法，新列才会被实例化并注册。
        /// </summary>
        public void ReloadColumns()
        {
            Headers.Clear();
            registeredColumnSignatures.Clear();
            ClearGroups();
            headerInitilized = false;
            headerRendered = false;
            renderCompleted = false;
            ApplyColumnChanges();
        }

        /// <summary>
        /// 按当前 <see cref="DataSource"/> 重新取数、重算行，但不动列定义。
        /// 用于往同一个集合实例里原地增删行的场景：<see cref="DataSource"/> 的引用没变，
        /// 表格不会自己重新查询，光靠 <see cref="ElementComponentBase.Refresh"/> 只是重渲染，行会保持旧的。
        /// 列集合本身变了请用 <see cref="ReloadColumns"/>（它顺带也会重走一次取数）。
        /// </summary>
        public void ReloadData()
        {
            dataSourceUpdated = true;
            Refresh();
        }

        /// <summary>
        /// 列集合变化后统一应用：按 SortNo 归位并触发重新渲染。
        /// 不触碰 headerRendered，避免重新渲染 ChildContent 造成列重复注册。
        /// </summary>
        private void ApplyColumnChanges()
        {
            Headers = Headers.OrderBy(x => x.SortNo).ToList();
            RequireRender = true;
            _ = InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// 自动展开所有节点
        /// </summary>
        [Parameter]
        public bool AutoExpandAll { get; set; } = true;

        /// <summary>
        /// 节点并行加载
        /// </summary>
        [Parameter]
        public bool EnableParallel { get; set; } = true;

        [Parameter]
        public EventCallback<object> OnRowDblClick { get; set; }

        [Inject]
        private TableEditorMap map { get; set; }

        [Inject]
        private IServiceProvider provider { get; set; }

        [Inject]
        private HttpClient httpClient { get; set; }

        public async Task RefreshNodeAsync(TreeItemBase item)
        {
            await ExpandAsync(item, true);
        }

        /// <summary>
        /// 刷新根节点
        /// </summary>
        public async Task RefreshRootNodeAsync()
        {
            if (!rows.Any())
            {
                return;
            }
            await RefreshNodeAsync((TreeItemBase)rows[0]);
        }

        /// <summary>
        /// 本地树形数据（未设置 <see cref="Url"/>）：折叠所有节点，仅显示根节点（ParentId == 0）。
        /// 常用于树形表格首次进入时只展示顶层，用户点击展开图标再逐层下钻。
        /// 异步加载（设置了 <see cref="Url"/>）的场景请用 <see cref="ExpandAsync"/> / <see cref="RefreshRootNodeAsync"/>。
        /// </summary>
        /// <returns>若表格含树形数据并已执行折叠则返回 <c>true</c>；表格尚未就绪（无行）返回 <c>false</c>。</returns>
        public bool CollapseAllTree()
        {
            if (!rows.Any(x => x is TreeItemBase))
            {
                return false;
            }

            var treeRows = rows.Cast<TreeItemBase>().ToList();
            foreach (var item in treeRows)
            {
                item.Expanded = false;
                item.Direction = "up";
            }
            // 隐藏集交给推导，避免和 Expanded 各写各的（非根节点此时全部落进 hiddenRows）
            SyncTreeHiddenRows();

            Refresh();
            return true;
        }

        /// <summary>
        /// 本地树形数据（未设置 <see cref="Url"/>）：展开全部节点（含所有层级），还原成「整棵树平铺可见」状态。
        /// </summary>
        public void ExpandAllTree()
        {
            if (!rows.Any(x => x is TreeItemBase))
            {
                return;
            }

            hiddenRows.Clear();
            foreach (var item in rows.Cast<TreeItemBase>())
            {
                item.Expanded = true;
                item.Direction = "right";
            }

            Refresh();
        }

        /// <summary>
        /// 展开某个节点，如果这个节点当前没有子节点，则动态加载子节点
        /// </summary>
        /// <param name="item"></param>
        /// <param name="forceLoad">是否强制加载新数据</param>
        public async Task ExpandAsync(TreeItemBase item, bool forceLoad = false)
        {
            if (string.IsNullOrWhiteSpace(Url))
            {
                ExceptionHelper.Throw(ExceptionHelper.ExpandOnlyHasUrl, "展开某个节点只支持异步加载");
            }
            ResetSelectAllStatus();
            item.Expanded = true;
            var finalChildren = GetAllChildren(item);
            if (forceLoad || !finalChildren.Any())
            {
                if (!forceLoad)
                {
                    item.HasChildren = true;
                }
                if (finalChildren.Count != rows.RemoveAll(finalChildren.Contains))
                {
                    ExceptionHelper.Throw(ExceptionHelper.ClearChildFailure, "清空子节点失败");
                }
                DataSource = rows;
                await LoadChildrenAsync(item, AutoExpandAll);
                Refresh();
                return;
            }

            LocalExpand(item, finalChildren);
            Refresh();
        }

        private async Task ForceExpandAsync(TreeItemBase item)
        {
            if (string.IsNullOrWhiteSpace(Url))
            {
                ExceptionHelper.Throw(ExceptionHelper.ExpandOnlyHasUrl, "展开某个节点只支持异步加载");
            }
            item.Expanded = true;
            var finalChildren = GetAllChildren(item);
            item.HasChildren = true;
            rows.RemoveAll(finalChildren.Contains);
            await LoadChildrenAsync(item, AutoExpandAll);
            Refresh();
        }

        /// <summary>
        /// 当保存数据时触发
        /// </summary>
        [Parameter]
        public EventCallback<TableSaveEventArgs> OnSave { get; set; }

        /// <summary>
        /// 不可编辑
        /// </summary>
        [Parameter]
        public bool IsEditable { get; set; }

        /// <summary>
        /// 自动操作列宽度（仅在 IsEditable=true 时生效，默认 180）
        /// </summary>
        [Parameter]
        public string OperationColumnWidth { get; set; } = "180";

        /// <summary>
        /// 失焦自动保存：当编辑中的行失去焦点（光标离开当前行，移动到表格外的其它控件，
        /// 例如工具栏按钮），且不是移动到弹出层（下拉/日期选择器等）时，自动提交当前编辑行，
        /// 无需手动点击「更新/保存」。默认关闭，避免影响既有行为。
        /// </summary>
        [Parameter]
        public bool AutoSaveOnLeave { get; set; }

        /// <summary>
        /// 编辑状态下，在单元格内按回车是否自动跳到下一个单元格并聚焦。
        /// Shift + 回车跳到上一个单元格。设为 false 时完全关闭回车跳格（只保留回车保存）
        /// 跳转方向由 <see cref="EnterNavigationMode"/> 决定。
        /// </summary>
        [Parameter]
        public bool EnterMovesToNextCell { get; set; } = true;

        /// <summary>
        /// 回车跳转方式，默认 <see cref="TableEnterNavigationMode.NextCell"/>（本行下一个可编辑单元格）。
        /// 设为 <see cref="TableEnterNavigationMode.SameColumnNextRow"/> 时，回车跳到下一行的同一列，
        /// Shift + 回车跳到上一行的同一列，适合按列逐条录入。最后一行按回车改为跳到下一列、落在该列的第一行
        /// （已是最后一列时不动），这样一列录完能自然接上下一列而不用先把行加出来。
        /// 换列时因为要换编辑行，上一行未保存的编辑值会按 <see cref="SwitchEditingRow(int)"/> 的口径丢弃。
        /// </summary>
        [Parameter]
        public TableEnterNavigationMode EnterNavigationMode { get; set; } = TableEnterNavigationMode.NextCell;

        /// <summary>
        /// 在最后一个（第一个）可编辑单元格按回车时，是否跨行跳到下一行（上一行）的可编辑单元格。
        /// 默认 false，即停在当前行的边界单元格。注意：跨行跳转会丢弃上一行未保存的编辑值。
        /// 仅在 <see cref="EnterNavigationMode"/> 为 <see cref="TableEnterNavigationMode.NextCell"/> 时生效
        /// 跳到下一行的第一个可编辑单元格。
        /// </summary>
        [Parameter]
        public bool EnterWrapsToNextRow { get; set; }

        /// <summary>
        /// 给「每格常驻编辑器」的表格（<see cref="IsEditable"/> 为 false，模板列直接把输入框渲染在每个单元格里）
        /// 开回车跳格：焦点在当前输入框上按回车 → 跳到下一行的同一列；已经是最后一行 → 跳到右边一列、落在该列的第一行
        /// （和 <see cref="EnterNavigationMode"/> 为 <see cref="TableEnterNavigationMode.SameColumnNextRow"/> 的口径一致）。
        /// 右边相邻列整列都没有编辑器时继续往右找，仍从找到的那一列首行落点。
        /// Shift + 回车反向走一行，到首行不再往左换列。目标格内没有可聚焦编辑器时保持不动。
        /// 默认 false（不影响任何既有表格）。
        /// </summary>
        [Parameter]
        public bool EnterNavigatesEditors { get; set; }

        /// <summary>
        /// 配合 <see cref="EnterNavigatesEditors"/>：跳格时把**来源格编辑器的内容**带进目标格（目标格原有内容被覆盖），
        /// 用于按列连续录入、相邻列数值相近的场景。
        /// 只有目标格是「可输入的 input/textarea」时才写值（下拉/日期这类只读触发框不写，写了也不是合法选项）；
        /// 来源格为空时不写，避免一次回车把目标格里已有的数据抹掉。
        /// 写值会补发 <c>input</c> 事件，所以宿主单元格里的 <c>ElInput</c> / <c>ElInputNumber</c> 走正常的
        /// ValueChanged 链路，模型与画面同步。默认 false（不影响任何既有表格）。
        /// </summary>
        [Parameter]
        public bool EnterCarriesValue { get; set; }

        /// <summary>
        /// 忽略的字段名称
        /// </summary>
        [Parameter]
        public string[] IgnoreProperties { get; set; } = { };

        /// <summary>
        /// 数据获取地址
        /// </summary>
        [Parameter]
        public string Url { get; set; }

        /// <summary>
        /// 数据源
        /// </summary>
        [Parameter]
        public object DataSource { get; set; }

        /// <summary>
        /// 当数据源发生变化时触发
        /// </summary>
        [Parameter]
        public EventCallback<object> DataSourceChanged { get; set; }
        /// <summary>
        /// 动态加载数据时设置实体类型
        /// </summary>
        [Parameter]
        public Type DataType { get; set; }

        /// <summary>
        /// 是否自动生成列
        /// </summary>
        [Parameter]
        public bool AutoGenerateColumns { get; set; } = true;

        /// <summary>
        /// 是否在第一列显示复选框列
        /// </summary>
        [Parameter]
        public bool HasSelectionColumn { get; set; } = true;

        /// <summary>
        /// 选择列宽度（像素）。默认 55；表里只有一颗勾选框、想给业务列腾宽度时可收小（如 40）。
        /// <see cref="ColumnsTotalWidth"/> 的横向滚动记账与表头/表体两根 <c>&lt;col&gt;</c> 都用这一个值。
        /// </summary>
        [Parameter]
        public int SelectionColumnWidth { get; set; } = 55;

        /// <summary>
        /// 选择列是否允许多选。false 时勾新行会取消旧行，且不渲染表头全选框
        /// （对齐 Radzen 的 SelectionMode="Single"）。
        /// </summary>
        [Parameter]
        public bool SelectionMultiple { get; set; } = true;

        /// <summary>
        /// 数据为空时显示的提示信息。
        /// </summary>
        [Parameter]
        public string EmptyMessage { get; set; }

        [Parameter]
        public string EmptyText
        {
            get => EmptyMessage;
            set => EmptyMessage = value;
        }

        [Parameter]
        public RenderFragment Empty { get; set; }
        /// <summary>
        /// 总数据条数
        /// </summary>
        [Parameter]
        public int Total { get; set; }

        /// <summary>
        /// 每页条数
        /// </summary>
        [Parameter]
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// 当前页数
        /// </summary>
        [Parameter]
        public int CurrentPage
        {
            get
            {
                return currentPage;
            }
            set
            {
                currentPage = value;
            }
        }

        private int currentPage = 1;

        /// <summary>
        /// 若实体实现了 <seealso cref="INotifyPropertyChanged"/> 接口，此参数控制多少毫秒没有新通知后刷新数据
        /// </summary>
        [Parameter]
        public int BatchUpdateDelayTime { get; set; } = 1000;

        /// <summary>
        /// 最大显示的页码数
        /// </summary>
        [Parameter]
        public int ShowPageCount { get; set; } = 7;

        /// <summary>
        /// 当前最大显示的页码数变化时触发
        /// </summary>
        [Parameter]
        public EventCallback<int> ShowPageCountChanged { get; set; }

        /// <summary>
        /// 当页码变化时触发
        /// </summary>
        [Parameter]
        public EventCallback<int> CurrentPageChanged { get; set; }


        /// <summary>
        /// 当只有一页时，不显示分页
        /// </summary>
        [Parameter]
        public bool NoPaginationOnSinglePage { get; set; } = true;

        /// <summary>
        /// 选中的记录
        /// </summary>
        [Parameter]
        public HashSet<object> SelectedRows { get; set; } = new HashSet<object>();

        /// <summary>
        /// 选中的记录变化时触发
        /// </summary>
        [Parameter]
        public EventCallback<HashSet<object>> SelectedRowsChanged { get; set; }
        internal Status selectAllStatus;
        private CancellationTokenSource delayCancelToken;
        private Task delayTask;

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// 启用分页
        /// </summary>
        [Parameter]
        public bool EnablePagination { get; set; } = true;

        /// <summary>
        /// 表格固定高度（像素）。手动设定时优先级最高，AutoFitHeight 自动让位。
        /// </summary>
        [Parameter]
        public int Height { get; set; }

        [Parameter]
        public int MaxHeight { get; set; }

        /// <summary>
        /// 自动把表格高度撑满视口剩余空间（表格内部滚动，页面不滚动）。
        /// 仅在未手动设定 Height / MaxHeight 时生效；窗口 resize 与容器尺寸变化会自动重算。
        /// </summary>
        [Parameter]
        public bool AutoFitHeight { get; set; }

        /// <summary>自动撑满时距视口底部预留的空白（像素），默认 12。</summary>
        [Parameter]
        public int FitBottomPadding { get; set; } = 12;

        /// <summary>自动撑满时的最小高度（像素），默认 120，避免窗口过矮时表格被压没了。</summary>
        [Parameter]
        public int FitMinHeight { get; set; } = 120;

        /// <summary>自动撑满时的最大高度（像素），0 表示不限制。</summary>
        [Parameter]
        public int FitMaxHeight { get; set; }

        [Parameter]
        public bool EnableMarkupString { get; set; }

        /// <summary>
        /// 启用斑马纹
        /// </summary>
        [Parameter]
        public bool Stripe { get; set; }

        [Parameter]
        public bool IsStripe
        {
            get => Stripe;
            set => Stripe = value;
        }

        [Parameter]
        public bool Border { get; set; }

        [Parameter]
        public bool IsBordered
        {
            get => Border;
            set => Border = value;
        }

        [Parameter]
        public bool Fit { get; set; } = true;

        /// <summary>
        /// 富余宽度交给<b>尾部填充列</b>，而不是交给某一列数据（批次91，2026-10-21）。
        /// <para>
        /// 默认 <c>false</c> 时是本库既有口径：不写 <c>Width</c> 的那一列（auto 列）吸收全部富余，
        /// 其余列才按声明宽度算 —— 见 <c>docs/table-auto-column-width.md</c>（批次㊸）。
        /// 这条口径有个反复咬人的前提：<b>宿主必须知道该留哪一列当 auto</b>。留错了就是消费方每天报的两种病：
        /// 末列是日期时它被摊成半屏空白（<c>/dictionary/docelement</c> 的「编辑时间」容器 1307 实测 634，内容只需 91.6），
        /// 一列都不留时 <c>table-layout:fixed</c> 又把富余<b>按声明比例摊回每一列</b>，
        /// 实测 773 的声明之和在 1307 容器里整体 ×1.69（操作 128→216.4、日期 100→169.1），写多少都不算数。
        /// </para>
        /// <para>
        /// 打开本参数后：① 表头/表体各多渲染一根<b>无内容的</b>尾列（<c>&lt;col&gt;</c> 不带宽度 + 一个空格子），
        /// 富余全部归它，于是每一列都精确等于声明值；② 没写 <c>Width</c> 的列不再当 auto，
        /// 而是按 <c>MinWidth &gt; 80px</c> 兜底钉死（与 <see cref="ColumnsTotalWidth"/> 的记账同一口径），
        /// 避免尾列和它<b>均分</b>富余（<c>table-layout:fixed</c> 对多个无宽列是平摊，不是给最后一个）。
        /// 表格总宽仍是 <c>max(100%, Σ)</c>：窄容器（Σ &gt; 容器）时尾列自然塌到 0、照旧横向滚动，
        /// 所以它的内衬与边框在 CSS 里归零，否则 0 宽的格子会把行撑出 1 像素滚动条。
        /// </para>
        /// <para>
        /// 尾列<b>不是</b> <c>Headers</c> 里的一项：排序、筛选、span-method、编辑取列、合计行下标都不受影响。
        /// 关掉它（默认）时本参数一个字节都不改既有渲染。
        /// </para>
        /// </summary>
        [Parameter]
        public bool TrailingFiller { get; set; }

        /// <summary>
        /// 尾部填充列是否要渲染。宿主开了 <see cref="TrailingFiller"/> 但整表一列都没有声明宽度时不开
        /// （那种表本来每列均分富余，插一根尾列只会把「均分 N 份」变成「均分 N+1 份」，没有意义）。
        /// </summary>
        protected bool UsesTrailingFiller => TrailingFiller
            && Headers.Any(x => !x.Ignore && (x.Width.HasValue || !string.IsNullOrWhiteSpace(x.MinWidth)));

        [Parameter]
        public bool Flexible { get; set; }

        [Parameter]
        public bool ScrollbarAlwaysOn { get; set; }

        [Parameter]
        public bool ShowHeader { get; set; } = true;

        [Parameter]
        public InputSize Size { get; set; } = InputSize.Normal;

        protected InputSize EffectiveSize => ResolveInputSize(Size);

        [Parameter]
        public bool Loading { get; set; }

        [Parameter]
        public object CurrentRow { get; set; }

        [Parameter]
        public EventCallback<object> CurrentRowChanged { get; set; }

        /// <summary>
        /// 加载中状态背景颜色
        /// </summary>
        [Parameter]
        public string LoadingBackground { get; set; }

        /// <summary>
        /// 加载中状态样式类
        /// </summary>
        [Parameter]
        public string LoadingIconClass { get; set; }

        /// <summary>
        /// 加载中状态文字
        /// </summary>
        [Parameter]
        public string LoadingText { get; set; }

        // ============ Element Plus 官方属性补齐 ============

        /// <summary>行数据的 Key，优化渲染；树形数据/保留选中时必填</summary>
        [Parameter] public string RowKey { get; set; }

        /// <summary>是否高亮当前行（点击行时）</summary>
        [Parameter] public bool HighlightCurrentRow { get; set; }

        /// <summary>自定义行 className：函数 ({ row, rowIndex }) => string 或固定 string</summary>
        [Parameter] public Func<object, int, string> RowClassName { get; set; }
        [Parameter] public string RowClassNameString { get; set; }

        /// <summary>自定义行 style</summary>
        [Parameter] public Func<object, int, Dictionary<string, string>> RowStyle { get; set; }
        [Parameter] public Dictionary<string, string> RowStyleObject { get; set; }

        /// <summary>自定义单元格 className：({ row, column, rowIndex, columnIndex }) => string</summary>
        [Parameter] public Func<object, TableHeader, int, int, string> CellClassName { get; set; }
        [Parameter] public string CellClassNameString { get; set; }

        /// <summary>自定义单元格 style</summary>
        [Parameter] public Func<object, TableHeader, int, int, Dictionary<string, string>>CellStyle { get; set; }
        [Parameter] public Dictionary<string, string> CellStyleObject { get; set; }

        /// <summary>表头行 className / style</summary>
        [Parameter] public Func<object, int, string> HeaderRowClassName { get; set; }
        [Parameter] public Func<object, int, Dictionary<string, string>> HeaderRowStyle { get; set; }

        /// <summary>表头单元格 className / style</summary>
        [Parameter] public Func<object, TableHeader, int, int, string> HeaderCellClassName { get; set; }
        [Parameter] public Func<object, TableHeader, int, int, Dictionary<string, string>> HeaderCellStyle { get; set; }

        /// <summary>全局 show-overflow-tooltip：单元格内容超长时隐藏并在 hover 显示 tooltip</summary>
        [Parameter] public bool ShowOverflowTooltip { get; set; }

        /// <summary>空数据时显示的文本（覆盖 EmptyText）</summary>
        [Parameter] public string EmptyTextOverride { get; set; }

        /// <summary>展开行（lazy 模式或子行展开）</summary>
        [Parameter] public HashSet<object> ExpandRowKeys { get; set; } = new();
        [Parameter] public bool DefaultExpandAllRows { get; set; }

        /// <summary>summary 合计行：({ columns, data }) => 每个列的合计值</summary>
        [Parameter] public Func<TableHeader[], IEnumerable<object>, object[]> SummaryMethod { get; set; }

        /// <summary>Table 的 table-layout CSS 值：fixed / auto</summary>
        [Parameter] public string TableLayout { get; set; }

        /// <summary>是否显示表头（覆盖 ShowHeader）</summary>
        [Parameter] public bool HideHeader { get; set; }

        /// <summary>懒加载子节点数据（树形）</summary>
        [Parameter] public Func<object, Task<IEnumerable>> Load { get; set; }

        /// <summary>
        /// 本库扩展（消费方批次110uu）：表级列宽拖拽开关。true 时所有未单独关闭的列都渲染拖拽手柄；
        /// 列级 <see cref="ElTableColumn.Resizable"/>（bool?）可逐列覆盖。EP 的 <c>resizable</c> 对齐口径：
        /// 拖拽全程纯 JS（dom.js 改两张表的 &lt;col&gt;），松手才回调 C# 记覆盖宽。
        /// </summary>
        [Parameter] public bool Resizable { get; set; }

        /// <summary>列宽拖拽落定（EP <c>header-dragend</c> 对齐口径）。只在松手时触发一次，拖拽过程中不触发。</summary>
        [Parameter] public EventCallback<TableColumnWidthChangeEventArgs> OnColumnWidthChange { get; set; }

        // ============ 事件回调（Element Plus Events） ============

        /// <summary>行点击</summary>
        [Parameter] public EventCallback<object> RowClick { get; set; }
        /// <summary>行双击（OnRowDblClick 已存在）</summary>
        /// <summary>单元格点击</summary>
        [Parameter] public EventCallback<(object Row, TableHeader Column, int RowIndex, int ColumnIndex)> CellClick { get; set; }
        /// <summary>表头单元格点击</summary>
        [Parameter] public EventCallback<(TableHeader Column, int ColumnIndex)> HeaderCellClick { get; set; }
        /// <summary>排序变化</summary>
        [Parameter] public EventCallback<(string Prop, TableSortOrder Order)> SortChange { get; set; }
        /// <summary>筛选变化</summary>
        [Parameter] public EventCallback<(string Prop, object[] Values)> FilterChange { get; set; }
        /// <summary>选择变化（SelectedRowsChanged 已存在）</summary>
        /// <summary>当前行变化（CurrentRowChanged 已存在）</summary>
        /// <summary>展开行变化</summary>
        [Parameter] public EventCallback<HashSet<object>> ExpandChange { get; set; }

        // ============ Element Plus 实例方法 / 进阶属性补齐 ============

        /// <summary>单元格合并方法（对齐 EP span-method）：返回 (rowspan, colspan)。
        /// 返回 (0,0) 表示该单元格被前面的单元格合并覆盖、不渲染；返回 (1,1) 为默认不合并。
        /// 参数依次为：行数据、列定义、行下标(0起)、列下标(0起，不含选择列)。</summary>
        [Parameter]
        public Func<object, TableHeader, int, int, (int Rowspan, int Colspan)> SpanMethod { get; set; }

        /// <summary>初始排序列的 prop（属性名或列文本）。设置后首次加载即按此列排序。</summary>
        [Parameter] public string DefaultSortProp { get; set; }

        /// <summary>初始排序方向，配合 <see cref="DefaultSortProp"/>，默认升序。</summary>
        [Parameter] public TableSortOrder DefaultSortOrder { get; set; } = TableSortOrder.Ascending;

        /// <summary>计算某单元格的合并信息；无 SpanMethod 时返回 (1,1)。</summary>
        internal (int Rowspan, int Colspan) GetSpan(object row, TableHeader header, int rowIndex, int columnIndex)
        {
            if (SpanMethod == null || header == null)
            {
                return (1, 1);
            }
            return SpanMethod(row, header, rowIndex, columnIndex);
        }

        /// <summary>合计行：每一列的值（对齐 EP summary-method）。
        /// 提供了 <see cref="SummaryMethod"/> 时按它计算；否则数值列求和，首列显示 <see cref="SumText"/>。</summary>
        internal IReadOnlyDictionary<TableHeader, object> GetSummaryCells()
        {
            var rendered = Headers.Where(x => !x.Ignore).ToList();
            var dict = new Dictionary<TableHeader, object>();
            if (SummaryMethod != null)
            {
                var array = SummaryMethod(rendered.ToArray(), rows);
                for (int k = 0; k < rendered.Count && k < (array?.Length ?? 0); k++)
                {
                    dict[rendered[k]] = array[k];
                }
                return dict;
            }
            for (int k = 0; k < rendered.Count; k++)
            {
                var header = rendered[k];
                if (k == 0)
                {
                    dict[header] = SumText;
                    continue;
                }
                if (header.EvalRaw == null)
                {
                    dict[header] = string.Empty;
                    continue;
                }
                var sum = 0m;
                var hasValue = false;
                foreach (var row in rows)
                {
                    var v = header.EvalRaw(row);
                    if (v == null) continue;
                    if (decimal.TryParse(Convert.ToString(v), out var d))
                    {
                        sum += d;
                        hasValue = true;
                    }
                }
                dict[header] = hasValue ? sum : (object)string.Empty;
            }
            return dict;
        }

        // ----- 实例方法（对齐 EP TableInstance ref 方法） -----

        /// <summary>设置当前行（高亮），等价于 EP setCurrentRow。</summary>
        public async Task SetCurrentRow(object row)
        {
            await SetCurrentRowAsync(row);
        }

        /// <summary>清空所有选中行，等价于 EP clearSelection。</summary>
        public async Task ClearSelectionAsync()
        {
            SelectedRows = new HashSet<object>();
            chkAll?.MarkAsRequireRender();
            foreach (var chk in rowCheckBoxses.Keys)
            {
                chk.MarkAsRequireRender();
            }
            ResetSelectAllStatus();
            if (SelectedRowsChanged.HasDelegate)
            {
                await SelectedRowsChanged.InvokeAsync(SelectedRows);
            }
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>切换某行选中状态；selected 为 null 时取反，等价于 EP toggleRowSelection(row[, selected])。</summary>
        public async Task ToggleRowSelectionAsync(object row, bool? selected = null)
        {
            if (row == null) return;
            var shouldSelect = selected ?? !SelectedRows.Contains(row);
            if (shouldSelect) SelectedRows.Add(row);
            else SelectedRows.Remove(row);
            foreach (var chk in rowCheckBoxses.Keys)
            {
                chk.MarkAsRequireRender();
            }
            ResetSelectAllStatus();
            if (SelectedRowsChanged.HasDelegate)
            {
                await SelectedRowsChanged.InvokeAsync(SelectedRows);
            }
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>清空所有列筛选，等价于 EP clearFilter()。条件式面板的已应用条件一并清空。</summary>
        public async Task ClearFilterAsync()
        {
            foreach (var header in Headers)
            {
                header?.FilterValues?.Clear();
                if (header?.AdvancedConditions != null)
                {
                    header.AdvancedConditions = new List<TableFilterCondition>();
                }
            }
            CloseFilterPanel();
            ApplyTableQuery();
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>重新布局（重算列宽/高度），等价于 EP doLayout。无虚拟测量时退化为一次重渲染。</summary>
        public async Task DoLayoutAsync()
        {
            RequireRender = true;
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>将表体滚动到指定像素位置，等价于 EP setScrollTop。</summary>
        public async Task SetScrollTopAsync(int top)
        {
            if (string.IsNullOrWhiteSpace(bodyWrapperElement.Id))
            {
                return;
            }
            try
            {
                await JSRuntime.InvokeVoidAsync("elementSetScrollTop", bodyWrapperElement, top, "auto");
            }
            catch
            {
                // 无 JS 环境（bUnit）忽略
            }
        }

        [Parameter]
        public Func<int, Task<PagerResult>> OnLoadDataSource { get; set; }
        public ElementReference Container { get; set; }
        [Inject]
        public LoadingService LoadingService { get; set; }

        /// <summary>
        /// 无显式宽度列的兜底宽度，对齐 Element Plus el-table-column 的默认 min-width: 80。
        /// 早期实现输出 &lt;col width="auto"&gt;，而 .el-table__header 是 table-layout:fixed，
        /// auto 列会被平均切成极窄列（10 列时仅 25px），再叠加 .el-table .cell{word-break:break-all}
        /// 就变成逐字符竖排，视觉上像乱码。
        /// </summary>
        private const int DefaultColumnWidth = 80;

        /// <summary>
        /// 排序箭头命中盒在列宽之外另算的加量：EP 压缩块给 <c>.caret-wrapper</c> 写死
        /// <c>width:24px / height:34px / flex-direction:column</c>，它是 .cell 里跟文字并排的一块。
        /// </summary>
        private const int SortIconBoxWidth = 24;

        /// <summary>
        /// 筛选漏斗在列宽之外另算的加量：图标 12px（<c>.el-table__column-filter-trigger i{font-size:12px}</c>，
        /// <c>transform:scale(.75)</c> 只缩视觉不缩布局盒）+ 本库覆盖的 <c>margin-left:6px</c>。
        /// </summary>
        private const int FilterIconBoxWidth = 18;

        /// <summary>
        /// 解析列在 <c>&lt;col width&gt;</c> 上使用的宽度：显式 Width &gt; MinWidth &gt; <b>auto（返回 null，属性整个省略）</b>，
        /// 再把该列表头带的图标盒（排序 24 / 筛选 18，两者都有 42）<b>加在这条宽度之外</b>。
        /// 百分比列原样返回：<c>width</c> 属性写不了 <c>calc()</c>，硬拼数字会把百分比变成固定像素。
        /// </summary>
        private string ResolveColumnWidth(TableHeader header)
        {
            // 拖拽落定的覆盖宽优先（本库扩展，消费方批次110uu）：dom.js 松手时回调
            // JsTableColumnResizeEnd 把最终 px 记进这里，之后的重渲染（排序 / 刷新 / 分页）
            // 全部沿用，页面刷新后新表格实例没有覆盖值 ⇒「刷新即复位」。
            if (header.ResizeWidthOverride is int overridden && overridden > 0)
            {
                return overridden.ToString();
            }

            var baseWidth = ResolveBaseColumnWidth(header);
            if (baseWidth == null)
            {
                // auto 列不吃图标盒加量：它没有钉死的宽度可加，可读性由 ColumnsTotalWidth 里那份
                // 80+加量的记账兜住（表格宁可横向滚动也不把它压窄）。
                if (!UsesTrailingFiller)
                {
                    return null;
                }

                // 开了尾部填充列：富余已经归尾列，这里再把 auto 留在场上会变成「尾列和它均分」，
                // 所以按 ColumnsTotalWidth 那份记账（80 + 图标盒）钉死，两边同一个数才对得齐。
                return (DefaultColumnWidth + HeaderIconAllowance(header)).ToString();
            }

            var allowance = HeaderIconAllowance(header);
            if (allowance == 0)
            {
                return baseWidth;
            }

            var trimmed = baseWidth.Trim();
            var digits = new string(trimmed.TakeWhile(char.IsAsciiDigit).ToArray());
            if (digits.Length == 0 || trimmed.EndsWith("%", StringComparison.Ordinal))
            {
                return baseWidth;
            }
            return (int.Parse(digits) + allowance).ToString();
        }

        /// <summary>
        /// 列宽本体（不含表头图标盒）：显式 Width &gt; MinWidth &gt; null。
        /// null 表示「auto 列」：<c>table-layout:fixed</c> 下不带 width 属性的 <c>&lt;col&gt;</c>
        /// 会<b>均分表格的剩余宽度</b>，这正是 Element Plus 里不写 width 的那一列的行为。
        /// 批次㊸ 之前这里兜底成 80px 并写进属性，于是「声明列宽之和 &lt; 表格宽度」时浏览器把富余
        /// <b>按比例摊给每一列</b>（zyweb 部门维护表 160/160/200/60 在 1106px 容器里实测摊成
        /// 305/305/381/114），操作列永远钉不住、长文本列也不会吸收富余。
        /// 80px 兜底仍然留在 <see cref="ColumnsTotalWidth"/> 的记账里，所以窄容器依旧靠横向滚动
        /// 保住可读性（批次⑦ 那条「十个无宽列挤成竖排字母」的回归不会回来）。
        /// </summary>
        private static string ResolveBaseColumnWidth(TableHeader header)
        {
            if (header.Width.HasValue)
            {
                return header.Width.ToString();
            }

            if (!string.IsNullOrWhiteSpace(header.MinWidth))
            {
                var raw = header.MinWidth.Trim();
                if (raw.EndsWith("px", StringComparison.OrdinalIgnoreCase))
                {
                    raw = raw.Substring(0, raw.Length - 2).Trim();
                }
                if (int.TryParse(raw, out var min) && min > 0)
                {
                    return min.ToString();
                }
            }

            return null;
        }

        /// <summary>把解析出的宽度串折成整数：剥掉宿主常写的 <c>px</c> 后缀，非法值才回落 80。</summary>
        private static int ParseColumnWidthPixels(string width)
        {
            var raw = width.Trim();
            if (raw.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            {
                raw = raw.Substring(0, raw.Length - 2).Trim();
            }
            return int.TryParse(raw, out var value) ? value : DefaultColumnWidth;
        }

        /// <summary>
        /// 该列表头是否渲染筛选漏斗。<see cref="ElTable"/> 的表头模板与列宽计算必须共用这一个判据，
        /// 否则「渲染了漏斗却没给它留宽度」又会把文字挤没。
        /// </summary>
        internal static bool HasFilterTrigger(TableHeader header)
        {
            return header.AdvancedFilter || (header.Filters != null && header.Filters.Any());
        }

        /// <summary>
        /// 表头图标盒的加量。<b>Why:</b> EP 有 realWidth 布局引擎，把 24px 的 caret 盒和漏斗排进
        /// <c>.cell</c> 的换行流里；本仓的列宽由 <c>&lt;col width&gt;</c> 直接钉死、没有布局引擎，
        /// 图标盒若算在声明宽度里，留给文字的净宽就少了 24/18/42px（实测演示页
        /// <c>Width="110"</c> 的「单价(元)」带排序时只剩 66px 给文字），
        /// 表头于是被 <c>.cell{overflow:hidden}</c> 裁掉。
        /// </summary>
        private static int HeaderIconAllowance(TableHeader header)
        {
            var allowance = 0;
            if (header.Sortable)
            {
                allowance += SortIconBoxWidth;
            }
            if (HasFilterTrigger(header))
            {
                allowance += FilterIconBoxWidth;
            }
            return allowance;
        }

        /// <summary>
        /// 列宽折算成像素，只用于 <see cref="ColumnsTotalWidth"/> 的求和：
        /// auto 列按 80 + 图标盒记账（属性上不写宽度，但表格最小宽度要给它留住这条底），
        /// 百分比列返回 0 不参与像素求和，带 <c>px</c> 后缀的声明要剥掉后缀再算。
        /// </summary>
        private int ResolveColumnWidthAsInt(TableHeader header)
        {
            var raw = ResolveColumnWidth(header);
            if (raw == null)
            {
                return DefaultColumnWidth + HeaderIconAllowance(header);
            }
            if (raw.EndsWith("%", StringComparison.Ordinal))
            {
                return 0;
            }
            return ParseColumnWidthPixels(raw);
        }

        /// <summary>所有列宽之和（含选择列），用于让表格横向滚动而不是把列压到不可读。</summary>
        protected int ColumnsTotalWidth
        {
            get
            {
                var total = HasSelectionColumn ? Math.Max(0, SelectionColumnWidth) : 0;
                foreach (var header in Headers.Where(x => !x.Ignore))
                {
                    total += ResolveColumnWidthAsInt(header);
                }
                return total;
            }
        }

        /// <summary>
        /// 分页器的总条数。宿主显式给了 <see cref="Total"/>（远程分页）就用它；
        /// 没给则回落到本地实际行数（树形按顶层节点数），与 <see cref="PageRows"/> 的切片基数一致。
        /// 早期实现分页器只读宿主 Total，本地数据不传 Total 时页码恒为 0 页，翻页形同装饰。
        /// </summary>
        protected int EffectiveTotal
        {
            get
            {
                if (Total > 0)
                {
                    return Total;
                }

                var treeRoots = rows.OfType<TreeItemBase>().Where(x => x.ParentId == 0).ToList();
                return treeRoots.Count > 0 ? treeRoots.Count : rows.Count;
            }
        }

        /// <summary>
        /// 当前页实际渲染的行。EnablePagination 打开且行数超过 PageSize 时只取当前页，
        /// 让 ElPagination 真正生效（早期实现只渲染分页器、不切片数据，分页器形同装饰）。
        /// </summary>
        protected IList<object> PageRows
        {
            get
            {
                if (!EnablePagination || PageSize <= 0 || rows.Count <= PageSize)
                {
                    return rows;
                }

                // 树形数据（本地）：按顶层节点（ParentId == 0）分页，每个顶层节点连同其
                // 已展开（未收起）的子节点整体落在同一页，避免父子被打散到不同页。
                // 此时 PageSize 表示「每页包含的顶层节点数」，分页器 Total 应传入顶层节点总数。
                var treeRows = rows.OfType<TreeItemBase>().ToList();
                if (treeRows.Count > 0)
                {
                    var roots = treeRows.Where(x => x.ParentId == 0).ToList();
                    if (roots.Count > 0)
                    {
                        var page = CurrentPage < 1 ? 1 : CurrentPage;
                        var maxPage = (roots.Count + PageSize - 1) / PageSize;
                        if (page > maxPage) page = maxPage;
                        if (page < 1) page = 1;

                        var pageRoots = roots.Skip((page - 1) * PageSize).Take(PageSize).ToList();
                        var visible = new HashSet<object>();
                        foreach (var root in pageRoots)
                        {
                            visible.Add(root);
                            foreach (var child in GetAllChildren(root))
                            {
                                if (!hiddenRows.Contains(child))
                                {
                                    visible.Add(child);
                                }
                            }
                        }

                        // 保持 rows 原有顺序（父在前、子在后的文档序），保证树形缩进与相邻导航正确
                        return rows.Where(r => visible.Contains(r)).ToList();
                    }
                }

                // 普通数据：按行切片
                var p = CurrentPage < 1 ? 1 : CurrentPage;
                var m = (rows.Count + PageSize - 1) / PageSize;
                if (p > m) p = m;
                return rows.Skip((p - 1) * PageSize).Take(PageSize).ToList();
            }
        }

        /// <summary>
        /// 当前页实际渲染的行数（已考虑分页与树形展开/折叠后的可见节点）。
        /// 只读，供外部（如 demo 状态条）显示「本页可见 N 行」。
        /// </summary>
        public int VisibleRowCount => PageRows?.Count ?? 0;

        /// <summary>
        /// 当前实际渲染的行，顺序与屏幕显示一致（已应用排序 / 筛选 / 分页 / 树形展开）。只读，请勿改写。
        /// <c>SetCurrentRow</c> 一类别拿数据源下标取行：默认排序会把「数据第 1 行」挪到表格中部，
        /// 高亮落在视口外、看着就像按钮没反应。
        /// </summary>
        public IList<object> VisibleRows => PageRows;

        protected string TableWidthStyle
        {
            get
            {
                var total = ColumnsTotalWidth;
                if (total <= 0)
                {
                    return Fit ? "width:100%" : "min-width:100%";
                }
                // 先给一条老浏览器也能解析的声明，再用 max() 覆盖；
                // 不支持 max() 的浏览器会忽略第二条，退化为原来的 100% 行为。
                return Fit
                    ? $"width:100%;width:max(100%,{total}px)"
                    : $"min-width:100%;min-width:max(100%,{total}px)";
            }
        }

        protected bool IsEmpty => rows.Count <= 0 && !IsEditable;

        protected string EmptyBlockStyle
        {
            get
            {
                if (EffectiveHeight <= 0 && MaxHeight <= 0)
                {
                    // 定高会把自定义 <Empty> 片段裁掉半截（宿主常给 100+px 的 ElEmpty），
                    // 而 EP 这条规则本来就是 min-height —— 正文区没有固定高度时让内容自己撑开。
                    return "min-height:60px";
                }

                // 定高/限高表格仍按正文高度给死值，空态才会落在可见区中间
                var height = Math.Max(60, (EffectiveHeight > 0 ? EffectiveHeight : MaxHeight) - (ShowHeader ? headerHeight : 0));
                return $"height:{height}px";
            }
        }

        protected string ScrollClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("is-scrolling-none")
            .AddIf(ScrollbarAlwaysOn, "is-scrollbar-always-on")
            .ToString();

        /// <summary>上一次绑定滚动同步时的元素标识，用于元素被重建后重新绑定。</summary>
        private string boundScrollSyncKey;

        /// <summary>
        /// 把表头 / 表体的横向滚动同步交给 dom.js（<c>elementTableSyncScroll</c>）纯 JS 镜像。
        /// <para>此前用 Blazor 的 <c>@onscroll</c>：每滚一次都要走事件分发 + 无条件 StateHasChanged，
        /// 整表（全部行）重渲染一遍 —— 行数一多就是「滚起来先空白停滞一下」。改纯 JS 后滚动期间
        /// 完全不进 Blazor 渲染管线。无 JS 环境（bUnit）静默降级为不同步。</para>
        /// </summary>
        private async Task SyncScrollBindingAsync()
        {
            var key = (headerElement.Id ?? string.Empty) + "|" + (bodyWrapperElement.Id ?? string.Empty);
            if (string.IsNullOrWhiteSpace(headerElement.Id) || string.IsNullOrWhiteSpace(bodyWrapperElement.Id)
                || key == boundScrollSyncKey)
            {
                return;
            }

            try
            {
                await JSRuntime.InvokeVoidAsync("elementTableSyncScroll", headerElement, bodyWrapperElement);
                boundScrollSyncKey = key;
            }
            catch
            {
                // bUnit 等无 JS 环境：忽略，仅失去横向滚动同步。
            }
        }

        /// <summary>列宽拖拽回调的 .NET 引用，首次绑定时创建、DisposeAsync 释放（ElTableV2 同款模式）。</summary>
        private DotNetObjectReference<ElTable> resizeDotNetRef;

        /// <summary>
        /// 把列宽拖拽交给 dom.js（<c>elementTableColumnResizeInit</c>）。
        /// 手柄 span 由 .razor 渲染在可拖列的 th 里，这里只把表头/表体两张表的引用和 .NET 回调递过去；
        /// JS 侧按手柄幂等绑定（__elResizeBound），重渲染重复调用无副作用。无 JS 环境（bUnit）静默降级。
        /// </summary>
        private async Task BindColumnResizeAsync()
        {
            if (string.IsNullOrWhiteSpace(headerElement.Id) || string.IsNullOrWhiteSpace(bodyWrapperElement.Id))
            {
                return;
            }
            if (!Headers.Any(x => x.Resizable && !x.Ignore))
            {
                return;
            }

            try
            {
                resizeDotNetRef ??= DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("elementTableColumnResizeInit", headerElement, bodyWrapperElement, resizeDotNetRef);
            }
            catch
            {
                // bUnit 等无 JS 环境：忽略，仅失去列宽拖拽。
            }
        }

        /// <summary>
        /// 把「自动撑满剩余高度」交给 dom.js 的 elementTableAutoFitHeightInit：
        /// JS 量出表格顶部到视口底部的可用高度，窗口/容器变化时回传 JsTableAutoFitHeight。
        /// 手动 Height / MaxHeight 存在时完全不绑定；无 JS 环境（bUnit）静默降级。
        /// </summary>
        private async Task BindAutoFitHeightAsync()
        {
            if (!AutoFitHeight || Height > 0 || MaxHeight > 0 || autoFitBound)
            {
                return;
            }

            try
            {
                autoFitDotNetRef ??= DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("elementTableAutoFitHeightInit", Container, autoFitDotNetRef, FitBottomPadding);
                autoFitBound = true;
            }
            catch
            {
                // bUnit 等无 JS 环境：忽略，保持 Height=0 的自然高度行为。
            }
        }

        /// <summary>
        /// dom.js 自动测量的回调：把可用高度 clamp 到 FitMinHeight / FitMaxHeight 后写入 EffectiveHeight。
        /// 手动设定 Height / MaxHeight 时忽略回传，保证「自己设定高度」优先。
        /// </summary>
        [JSInvokable]
        public Task JsTableAutoFitHeight(int height)
        {
            if (!AutoFitHeight || Height > 0 || MaxHeight > 0)
            {
                return Task.CompletedTask;
            }

            var value = Math.Max(FitMinHeight, height);
            if (FitMaxHeight > 0)
            {
                value = Math.Min(value, FitMaxHeight);
            }

            if (value != autoFitHeight)
            {
                autoFitHeight = value;
                StateHasChanged();
            }
            return Task.CompletedTask;
        }

        /// <summary>这一列的表头是否渲染拖拽手柄（登记时已把列级/表级 Resizable 归并进 TableHeader.Resizable）。</summary>
        internal bool IsResizableColumn(TableHeader header)
        {
            return header != null && header.Resizable && !header.Ignore;
        }

        /// <summary>
        /// dom.js 拖拽松手的回调（消费方批次110uu）：把落定宽度记进 <see cref="TableHeader.ResizeWidthOverride"/>
        /// 并重渲染（渲染树从此携带覆盖宽，排序/刷新不会打回去），再通知宿主。
        /// </summary>
        /// <param name="colIndex">col 名里的序号（与 .razor 渲染循环的 i 同一套，忽略 Ignored 列后对齐）。</param>
        /// <param name="width">落定列宽（px）。</param>
        /// <param name="oldWidth">拖拽前列宽（px）。</param>
        [JSInvokable]
        public async Task JsTableColumnResizeEnd(int colIndex, int width, int oldWidth)
        {
            // 0 是选择列：宽度由 SelectionColumnWidth 钉死，不参与拖拽覆盖
            if (colIndex <= 0 || width <= 0)
            {
                return;
            }

            TableHeader target = null;
            var i = 0;
            foreach (var header in Headers)
            {
                i++;
                if (header.Ignore)
                {
                    continue;
                }
                if (i == colIndex)
                {
                    target = header;
                    break;
                }
            }
            if (target == null || width == oldWidth)
            {
                return;
            }

            target.ResizeWidthOverride = width;
            await InvokeAsync(StateHasChanged);
            if (OnColumnWidthChange.HasDelegate)
            {
                await OnColumnWidthChange.InvokeAsync(new TableColumnWidthChangeEventArgs
                {
                    Column = target,
                    ColIndex = colIndex,
                    Width = width,
                    OldWidth = oldWidth
                });
            }
        }

        /// <summary>Blazor 优先走异步释放（实现 IAsyncDisposable 后不再调同步 Dispose）：顺手释放 .NET 回调引用。</summary>
        public ValueTask DisposeAsync()
        {
            resizeDotNetRef?.Dispose();
            resizeDotNetRef = null;
            autoFitDotNetRef?.Dispose();
            autoFitDotNetRef = null;
            Dispose(true);
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        /// <summary>手动同步一次（表体 → 表头），供调用方在编程滚动后按需调用。</summary>
        protected async Task SyncHeaderScrollAsync()
        {
            if (string.IsNullOrWhiteSpace(headerElement.Id) || string.IsNullOrWhiteSpace(bodyWrapperElement.Id))
            {
                return;
            }
            var scrollLeft = await bodyWrapperElement.Dom(JSRuntime).GetScrollLeftAsync();
            await headerElement.Dom(JSRuntime).SetScrollLeftAsync(scrollLeft);
        }

        /// <summary>手动同步一次（表头 → 表体），供调用方在编程滚动后按需调用。</summary>
        protected async Task SyncBodyScrollFromHeaderAsync()
        {
            if (string.IsNullOrWhiteSpace(headerElement.Id) || string.IsNullOrWhiteSpace(bodyWrapperElement.Id))
            {
                return;
            }
            var scrollLeft = await headerElement.Dom(JSRuntime).GetScrollLeftAsync();
            await bodyWrapperElement.Dom(JSRuntime).SetScrollLeftAsync(scrollLeft);
        }

        protected async Task SetCurrentRowAsync(object row)
        {
            CurrentRow = row;
            if (CurrentRowChanged.HasDelegate)
            {
                await CurrentRowChanged.InvokeAsync(row);
            }
            // 强制自身重渲染，确保 current-row 高亮立即生效（对齐 EP setCurrentRow）
            await InvokeAsync(StateHasChanged);
        }

        internal Task OnRowCheckBoxRenderCompleted(object row, ElCheckbox<bool> chk)
        {
            lock (rowCheckBoxses)
            {
                rowCheckBoxses.Add(chk, row);
            }
            return Task.CompletedTask;
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            SelectedRows = new HashSet<object>();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            await SyncScrollBindingAsync();
            await BindColumnResizeAsync();
            await BindAutoFitHeightAsync();
            await FocusPendingEditingCellAsync();
            if (renderCompleted)
            {
                if (dataSourceUpdated)
                {
                    dataSourceUpdated = false;
                    await RefreshDataSourceAsync(AutoExpandAll);
                }
                return;
            }
            if (headerRendered)
            {
                renderCompleted = true;
                await (OnRenderCompleted?.Invoke(this) ?? Task.CompletedTask);
                return;
            }
            await RefreshDataSourceAsync(AutoExpandAll);
            headerRendered = true;
            RequireRender = true;
            StateHasChanged();
        }

        internal async Task RefreshDataSourceAsync(bool autoExpand)
        {
            if (!string.IsNullOrWhiteSpace(Url))
            {
                DataSource = await httpClient.GetFromJsonAsync(Url, typeof(List<>).MakeGenericType(DataType ?? typeof(object)));
                if (DataSourceChanged.HasDelegate)
                {
                    await DataSourceChanged.InvokeAsync(DataSource);
                }

            }
            await InitlizeDataSourceAsync(0, "up", autoExpand);
        }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);
            if (parameters.GetValueOrDefault<object>(nameof(DataSource)) != null)
            {
                await InitlizeDataSourceAsync(0, string.Empty, true);
                return;
            }
        }

        private async Task InitlizeDataSourceAsync(int level, string direction, bool autoExpand)
        {
            if (DataSource != null)
            {
                sourceRows = (DataSource as IEnumerable).Cast<object>().ToList();
                rows = sourceRows.ToList();
                foreach (var item in rows)
                {
                    var propertyChanged = item as INotifyPropertyChanged;
                    if (propertyChanged == null)
                    {
                        break;
                    }
                    propertyChanged.PropertyChanged -= PropertyChanged_PropertyChanged;
                    propertyChanged.PropertyChanged += PropertyChanged_PropertyChanged;
                }
                    if (string.IsNullOrWhiteSpace(Url))
                    {
                        if (rows.Any(x => x is TreeItemBase))
                        {
                            var rootLevels = rows.Cast<TreeItemBase>().Where(x => x.ParentId == 0).ToList();
                            CalculateLevel(rootLevels, null, rows.Cast<TreeItemBase>().ToArray());
                            // DefaultExpandAllRows：本地树（未设置懒加载 Url）首次初始化时把每个节点 Expanded=true，
                            // 还原成「整棵树平铺可见」状态（此前该参数只声明未消费，会退化成手动 Expanded 才生效）
                            if (DefaultExpandAllRows)
                            {
                                foreach (var treeItem in rows.Cast<TreeItemBase>())
                                {
                                    treeItem.Expanded = true;
                                    treeItem.Direction = "right";
                                }
                            }
                        }
                        // 泛型集合取元素类型；数组 / IEnumerable 等非泛型数据源回退用首行运行时类型
                        var genericArguments = DataSource.GetType().GetGenericArguments();
                        if (genericArguments.Length > 0)
                        {
                            DataType = genericArguments[0];
                        }
                        else if (rows.Count > 0 && rows[0] != null)
                        {
                            DataType = rows[0].GetType();
                        }
                    }
                else
                {
                    rows.Clear();
                    foreach (TreeItemBase item in (DataSource as IEnumerable))
                    {
                        if (item.Level == null)
                        {
                            item.Level = level;
                            item.Direction = direction;
                        }
                        rows.Add(item);
                        if (AutoExpandAll && autoExpand && item.HasChildren && !item.Expanded)
                        {
                            var task = ForceExpandAsync(item);
                            if (!EnableParallel)
                            {
                                await task;
                            }
                            _ = ReadyExecuteRefreshAsync();
                        }
                    }

                }
            }
            else
            {
                // 未声明 DataType 时（画廊孤立渲染 / 无数据源场景）用 object 兜底，
                // 避免 MakeGenericType(null) 抛 ArgumentNullException；后续 InitilizeHeaders 会因
                // DataType==null 跳过自动列生成，组件渲染空状态（无数据）而非崩溃。
                var elementType = DataType ?? typeof(object);
                DataSource = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
            }
            InitilizeHeaders();
            ApplyTableQuery();
        }

        private void InitilizeHeaders()
        {
            if (AutoGenerateColumns && !headerInitilized && DataType != null)
            {
                headerInitilized = true;
                DataType.GetProperties().Where(p => !IgnoreProperties.Contains(p.Name)).Reverse().ToList().ForEach(property =>
                {
                    if (Headers.Any(x => x.Property?.Name == property.Name))
                    {
                        return;
                    }
                    var attrs = property.GetCustomAttributes(true);
                    if (attrs.OfType<TableIgnoreAttribute>().Any())
                    {
                        Headers.Add(new TableHeader()
                        {
                            IsAutoGenerated = true,
                            Ignore = true,
                            Property = property
                        });
                        return;
                    }
                    var columnConfig = attrs.OfType<TableColumnAttribute>().FirstOrDefault() ?? new TableColumnAttribute()
                    {
                        Text = property.Name
                    };

                    if (columnConfig.Ignore)
                    {
                        Headers.Add(new TableHeader()
                        {
                            IsAutoGenerated = true,
                            Ignore = true,
                            Property = property
                        });
                        return;
                    }
                    var editorConfig = attrs.OfType<EditorGeneratorAttribute>().FirstOrDefault() ?? new EditorGeneratorAttribute()
                    {
                        Control = typeof(ElInput<string>)
                    };

                    var formConfig = attrs.OfType<FormControlAttribute>().FirstOrDefault();
                    var propertyConfig = attrs.OfType<PropertyAttribute>().FirstOrDefault();

                    var tableHeader = new TableHeader()
                    {
                        EvalRaw = row =>
                        {
                            object value = property.GetValue(row);
                            return value;
                        },
                        SortNo = columnConfig.SortNo,
                        IsCheckBox = property.PropertyType == typeof(bool) || Nullable.GetUnderlyingType(property.PropertyType) == typeof(bool),
                        Property = property,
                        IsAutoGenerated = true,
                        Text = columnConfig.Text,
                        Width = columnConfig.Width,
                        IsEditable = columnConfig.IsEditable
                    };
                    // CreateRenderFactory 对未提供显示渲染器的类型会返回 null（见上方 AddColumn / InitilizeHeaderEditor 的 `?.` 用法），
                    // 此处必须同样做空值保护，否则自动生成列时会抛 NullReferenceException。
                    tableHeader.Eval = displayRender.CreateRenderFactory(tableHeader)?.CreateRender(tableHeader);
                    if (IsEditable && columnConfig.IsEditable)
                    {
                        InitilizeHeaderEditor(property, editorConfig, tableHeader);
                    }
                    Headers.Insert(0, tableHeader);
                }
                 );
                if (IsEditable)
                {
                    CreateOperationColumn();
                }
                chkAll?.MarkAsRequireRender();
                ResetSelectAllStatus();
            }
            else if (!AutoGenerateColumns && !headerInitilized && Headers.Any())
            {
                headerInitilized = true;
                foreach (var header in Headers)
                {
                    if (!CanEdit(header))
                    {
                        continue;
                    }
                    InitilizeHeaderEditor(header.Property, header.Property.GetCustomAttribute<EditorGeneratorAttribute>() ?? new EditorGeneratorAttribute(), header);
                }
                if (IsEditable)
                {
                    CreateOperationColumn();
                }
                Refresh();
            }
        }

        private void CreateOperationColumn()
        {
            Headers.RemoveAll(x => x.IsOperation);
            var lastIndex = Headers.Count;
            var operationHeader = new TableHeader()
            {
                Text = "操作",
                IsOperation = true,
                Width = OperationColumnWidth ?? "180"
            };
            ElButton btnUpdate = null, btnDelete = null;
            operationHeader.Template = row =>
            {
                return (builder) =>
                {
                    builder.OpenComponent<ElButtonGroup>(0);
                    builder.AddAttribute(1, nameof(ElButtonGroup.ChildContent), (RenderFragment)(builder =>
                    {
                        builder.OpenComponent<ElButton>(1);
                        builder.AddAttribute(2, nameof(ElButton.Type), ButtonType.Primary);
                        builder.AddAttribute(3, nameof(ElButton.Size), ButtonSize.Small);
                        builder.AddAttribute(4, nameof(ElButton.ChildContent), (RenderFragment)(builder =>
                        {
                            if (row == editingRow)
                            {
                                builder.AddMarkupContent(5, "保存");
                            }
                            else
                            {
                                builder.AddMarkupContent(5, "更新");
                            }
                        }));
                        if (Page == null)
                        {
                            throw new ElementException(1, "使用可编辑功能必须提供一个值不为 null 的 CascadingValue，值为 ElementDialogBase(this)参数改为 Page");
                        }
                        builder.AddAttribute(6, nameof(ElButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(Page, async (e) =>
                        {
                            if (editingTaskCompletionSource == null)
                            {
                                editingTaskCompletionSource = new TaskCompletionSource<int>();
                                editingRow = row;
                                btnUpdate?.MarkAsRequireRender();
                                Refresh();
                                return;
                            }
                            if (btnUpdate != null) { btnUpdate.IsLoading = true; }
                            Refresh();
                            _ = SaveDataAsync(SaveAction.Update);
                            var result = await editingTaskCompletionSource.Task;
                            editingTaskCompletionSource = null;
                            if (btnUpdate != null) { btnUpdate.IsLoading = false; }
                            if (result != 0)
                            {
                                Refresh();
                                editingTaskCompletionSource = new TaskCompletionSource<int>();
                                return;
                            }
                            foreach (var item in Headers)
                            {
                                item.EditingValue = (string)(item.EditorRenderConfig == null ? null : item.EditorRenderConfig.EditingValue = null);
                            }
                            btnUpdate?.MarkAsRequireRender();
                            Refresh();
                        }));
                        builder.AddComponentReferenceCapture(7, btn =>
                        {
                            if (row == editingRow || editingRow == null)
                            {
                                btnUpdate = (ElButton)btn;
                            }
                        });
                        builder.CloseComponent();
                        builder.OpenComponent<ElButton>(8);
                        builder.AddAttribute(9, nameof(ElButton.Type), ButtonType.Danger);
                        builder.AddAttribute(10, nameof(ElButton.Size), ButtonSize.Small);
                        builder.AddAttribute(11, nameof(ElButton.ChildContent), (RenderFragment)(builder =>
                        {
                            builder.AddMarkupContent(12, "删除");
                        }));
                        builder.AddAttribute(13, nameof(ElButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(Page, async (e) =>
                        {
                            var confirmResult = await ConfirmAsync("确认要删除吗？");
                            if (confirmResult != MessageBoxResult.Ok)
                            {
                                return;
                            }
                            foreach (var item in Headers)
                            {
                                item.RawValue = row;
                            }
                            editingTaskCompletionSource = new TaskCompletionSource<int>();
                            if (btnDelete != null) { btnDelete.IsLoading = true; }
                            btnDelete?.Refresh();
                            _ = SaveDataAsync(SaveAction.Delete);
                            var result = await editingTaskCompletionSource.Task;
                            if (btnDelete != null) { btnDelete.IsLoading = false; }
                            editingTaskCompletionSource = null;
                            if (result != 0)
                            {
                                btnDelete?.Refresh();
                                editingTaskCompletionSource = new TaskCompletionSource<int>();
                                return;
                            }
                            editingTaskCompletionSource = null;
                            editingRow = null;
                            Toast("删除成功");
                        }));
                        builder.AddComponentReferenceCapture(14, btn => btnDelete = (ElButton)btn);
                        builder.CloseComponent();
                    }));
                    builder.CloseComponent();
                };
            };
            Headers.Insert(lastIndex++, operationHeader);
        }

        private void InitilizeHeaderEditor(PropertyInfo property, EditorGeneratorAttribute editorConfig, TableHeader tableHeader)
        {
            // 编辑进行中（editingRow != null）或该列已有未提交的编辑值（RawInfoHasSet）时，
            // 禁止重建 EditorRenderConfig：否则渲染期 initblock 会把 EditingValue 覆写为原始行值，
            // 导致刚选中的值（如 ElSelect 选项）被吞掉。复用现有配置即可保留 EditingValue / RawValue。
            var existingConfig = tableHeader.EditorRenderConfig;
            if (existingConfig != null && (existingConfig.RawInfoHasSet || editingRow != null))
            {
                return;
            }
            // A column may declare its editor explicitly (Editor="typeof(ElSelect<string>)" etc.).
            // Open generics such as ElSelect<> are closed with the property type first.
            var controlType = tableHeader.EditorType;
            Type renderType = null;
            Type dataSourceLoader = null;
            if (controlType != null)
            {
                if (controlType.IsGenericTypeDefinition && controlType.GetGenericArguments().Length == 1)
                {
                    controlType = controlType.MakeGenericType(property.PropertyType);
                }
                renderType = map.GetRenderType(controlType);
                if (renderType == null)
                {
                    throw new ElementException($"The editor declared for column [{tableHeader.Text}] has no registered renderer.");
                }
            }
            else
            {
                var controlInfo = map.GetControl(property);
                controlType = controlInfo.ControlType;
                renderType = controlInfo.RenderType;
                dataSourceLoader = controlInfo.DataSourceLoader;
            }
            tableHeader.EditorRender = (IControlRender)provider.GetRequiredService(renderType);
            tableHeader.EditorRenderConfig = new RenderConfig()
            {
                InputControlType = controlType,
                IsRequired = editorConfig.IsRequired,
                RequiredMessage = editorConfig.RequiredMessage ?? $"{tableHeader.Text}不能为空",
                Placeholder = editorConfig.Placeholder,
                Property = property,
                DataSourceLoader = dataSourceLoader,
                DataSource = tableHeader.EditorDataSource,
                ValueField = tableHeader.EditorValueField,
                TextField = tableHeader.EditorTextField,
                EditorColumns = tableHeader.EditorColumns,
                CloseOnDatePick = tableHeader.EditorCloseOnDatePick,
                Page = Page,
                // 绑定回调的接收者用表格本身而不是页面：否则每敲一个字重渲染整页（见 RenderConfig.Owner 注释）
                Owner = this
            };
            // 编辑器（ElSelect / ElDateTimePicker 等）把选中值写回 EditingValue 后不会自己重绘，
            // 这里挂一次重渲染，界面才会在选择后立刻显示新值。
            // 必须走 InvokeAsync 排队：ElTable.razor 会在渲染过程中给 EditingValue 赋初值，
            // 同步调用 StateHasChanged 会打断当前这一轮渲染（实测整行只渲染出前两列）。
            //
            // 文本/数字框自己就显示用户输入，这次通知是<b>重复</b>的（实测跳过它之后输入耗时没变：
            // 真正的瓶颈在 RenderBase.CreateBind 用 config.Page 当 EventCallback 接收者，
            // 每敲一个字 Blazor 会重渲染整个页面，见 docs/table-edit-issues.md）。
            // 跳过它仍然值得：省掉一次整表 diff，且不影响任何显示行为。
            // 只给"选中后不会自己改显示"的控件（ElSelect / ElDropDownTableGrow / ElSwitch / ElDatePicker）挂。
            if (!IsSelfDisplayingEditor(controlType))
            {
                tableHeader.EditorRenderConfig.ValueChanged = () => { _ = InvokeAsync(StateHasChanged); };
            }
        }

        /// <summary>
        /// 是否属于"自己就会显示用户输入"的编辑器（文本框 / 数字框）。
        /// 这类控件的值变化不需要整表重渲染；<c>ElSelect</c> / <c>ElDropDownTableGrow</c> /
        /// <c>ElSwitch</c> / <c>ElDatePicker</c> 等仍需那次通知，不能一并跳过。
        /// </summary>
        private static bool IsSelfDisplayingEditor(Type controlType)
        {
            if (controlType == null)
            {
                return false;
            }

            var definition = controlType.IsGenericType ? controlType.GetGenericTypeDefinition() : controlType;
            return definition == typeof(ElInput<>) || definition == typeof(ElInputNumber);
        }

        private async Task ReadyExecuteRefreshAsync()
        {
            if (loadTaskCompletionSource == null)
            {
                loadTaskCompletionSource = new TaskCompletionSource<int>();
            }
            else
            {
                loadTaskCompletionSource.TrySetResult(0);
                loadTaskCompletionSource.TrySetCanceled();
                loadTaskCompletionSource = null;
                await ReadyExecuteRefreshAsync();
                return;
            }
            var task = Task.Delay(500);
            var resultTask = await Task.WhenAny(task, loadTaskCompletionSource.Task);
            if (resultTask == loadTaskCompletionSource.Task)
            {
                return;
            }
            await InvokeAsync(Refresh);
        }

        private void PropertyChanged_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (delayTask == null)
            {
                delayCancelToken = new CancellationTokenSource();
                delayTask = Task.Delay(BatchUpdateDelayTime, delayCancelToken.Token).ContinueWith(t =>
                {
                    if (t.IsCanceled)
                    {
                        return;
                    }
                    dataSourceUpdated = true;
                    InvokeAsync(Refresh);
                    delayTask = null;
                });
            }
            else if (delayTask.Status == TaskStatus.Running)
            {
                delayCancelToken.Dispose();
            }

        }

        private void CalculateLevel(List<TreeItemBase> treeItems, TreeItemBase parentNode, TreeItemBase[] allTreeItems)
        {
            foreach (var treeRow in treeItems)
            {
                if (treeRow.Level != null)
                {
                    return;
                }
                treeRow.Level = (parentNode?.Level ?? -1) + 1;
                treeRow.TextPaths = parentNode?.TextPaths == null ? new Dictionary<int, string>() : new Dictionary<int, string>(parentNode.TextPaths);
                if (!treeRow.TextPaths.ContainsKey(treeRow.Id))
                {
                    treeRow.TextPaths.Add(treeRow.Id, treeRow.Text);
                }
                var children = allTreeItems.Where(x => x.ParentId == treeRow.Id).ToList();
                CalculateLevel(children, treeRow, allTreeItems);
            }
        }

        private async Task SaveTableAsync(MouseEventArgs e)
        {
#pragma warning disable BL0005 // Component parameter should not be set outside of its component.
            saveAddButton.IsLoading = true;
            saveAddButton.Refresh();
            await SaveDataAsync(SaveAction.Create);
            saveAddButton.IsLoading = false;
            saveAddButton.MarkAsRequireRender();
            MarkAsRequireRender();
#pragma warning restore BL0005 // Component parameter should not be set outside of its component.
        }

        private async Task SaveTableAsync(KeyboardEventArgs e)
        {
            if (e.Code != "Enter")
            {
                return;
            }
            var keyValueModels = DataSource as List<KeyValueModel>;
            if (string.IsNullOrWhiteSpace(Url) && (DataSource == null || keyValueModels != null))
            {
                var keyHeader = Headers.FirstOrDefault(x => x.Property.Name == "Key");
                var txtKey = keyHeader.EditingValue?.Trim();
                if (string.IsNullOrWhiteSpace(txtKey))
                {
                    return;
                }
                var valueHeader = Headers.FirstOrDefault(x => x.Property.Name == "Value");
                var txtValue = valueHeader.EditingValue?.Trim();
                txtKey = txtKey.Trim();
                if (keyValueModels == null)
                {
                    DataSource = keyValueModels = new List<KeyValueModel>();
                }
                var existKeyValueModel = keyValueModels.FirstOrDefault(x => x.Key == txtKey);
                if (existKeyValueModel == null)
                {
                    keyValueModels.Add(new KeyValueModel()
                    {
                        Key = txtKey,
                        Value = txtValue
                    });
                }
                else
                {
                    existKeyValueModel.Value = txtValue;
                }
                keyHeader.EditingValue = string.Empty;
                valueHeader.EditingValue = null;
                await InitlizeDataSourceAsync(0, string.Empty, true);
                if (editingRow != null)
                {
                    editingRow = null;
                }
                if (DataSourceChanged.HasDelegate)
                {
                    await DataSourceChanged.InvokeAsync(DataSource);
                }
                return;
            }
            await SaveDataAsync(saveAction);
        }

        private async Task SaveDataAsync(SaveAction saveAction)
        {
            if (DataType == null)
            {
                if (DataSource == null)
                {
                    throw new ElementException(2, "DataType 或 DataSource 必须设置一个");
                }
                DataType = DataSource.GetType().GetGenericArguments()[0];
            }
            var row = Activator.CreateInstance(DataType);
            foreach (var header in Headers)
            {
                if ((!header.IsEditable && header.EditorRenderConfig == null) || (saveAction != SaveAction.Create && !CanEdit(header)))
                {
                    if (header.Property != null && (header.Property.Name.Contains("Id") || header.Property.Name.Contains("Key")) && saveAction != SaveAction.Create)
                    {
                        header.Property.SetValue(row, header.Property.GetValue(header.RawValue));
                    }
                    Clear(header);
                    continue;
                }
                if (saveAction != SaveAction.Delete)
                {
                    // 编辑值为空（如新增行中未填写的列）时，值类型不能直接 Convert.ChangeType(null)，
                    // 否则抛 InvalidCast_CannotCastNullToValueType；此处回退为该类型的默认值（可空类型仍为 null）。
                    object rawValue = header.EditorRenderConfig.EditingValue;
                    object cell;
                    if (rawValue == null)
                    {
                        var targetType = header.Property.PropertyType;
                        cell = targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null
                            ? Activator.CreateInstance(targetType)
                            : null;
                    }
                    else
                    {
                        // Convert.ChangeType cannot target Nullable<T> (it throws InvalidCastException),
                        // so always convert against the underlying type instead.
                        var convertType = Nullable.GetUnderlyingType(header.Property.PropertyType) ?? header.Property.PropertyType;
                        cell = convertType.IsInstanceOfType(rawValue) ? rawValue : Convert.ChangeType(rawValue, convertType);
                    }
                    if (header.EditorRenderConfig.IsRequired && cell == null)
                    {
                        Toast(header.EditorRenderConfig.RequiredMessage);
                        editingTaskCompletionSource?.TrySetResult(-1);
                        return;
                    }
                    header.Property.SetValue(row, cell);
                }
                Clear(header);
            }
            var tableSaveArg = new TableSaveEventArgs()
            {
                Action = saveAction,
                Data = row,
                Table = this,
            };
            if (!OnSave.HasDelegate)
            {
                if (DataType == typeof(KeyValueModel))
                {
                    OnSave = EventCallback.Factory.Create<TableSaveEventArgs>(this, TableRender.DefaultSaverAsync);
                }
                else if (!string.IsNullOrWhiteSpace(Key))
                {
                    OnSave = EventCallback.Factory.Create<TableSaveEventArgs>(this, TableRender.DefaultSaverAsync);
                }
                else
                {
                    Toast("OnSave 事件没有注册或数据源类型不是 KeyValueModel 或没指定 Key");
                    editingTaskCompletionSource?.TrySetResult(-1);
                    return;
                }
            }
            tableSaveArg.Key = Key;
            tableSaveArg.DataType = DataType;
            if (editingTaskCompletionSource != null)
            {
                await OnSave.InvokeAsync(tableSaveArg);
                if (tableSaveArg.Cancel)
                {
                    editingTaskCompletionSource.TrySetResult(-1);
                    return;
                }
                editingRow = null;
                dataSourceUpdated = true;
                await RefreshDataSourceAsync(false);
                editingTaskCompletionSource.TrySetResult(0);
            }
            else
            {
                if (OnSave.HasDelegate)
                {
                    await OnSave.InvokeAsync(tableSaveArg);
                    if (tableSaveArg.Cancel)
                    {
                        editingTaskCompletionSource?.TrySetResult(-1);
                        return;
                    }
                    editingRow = null;
                    dataSourceUpdated = true;
                    await RefreshDataSourceAsync(false);
                    editingTaskCompletionSource?.TrySetResult(0);
                    return;
                }
                editingTaskCompletionSource?.TrySetResult(-1);
            }
        }

        private void Clear(TableHeader header)
        {
            header.RawValue = null;
            header.EditingValue = null;
            if (header.EditorRenderConfig == null)
            {
                return;
            }
            header.EditorRenderConfig.RawValue = null;
            header.EditorRenderConfig.EditingValue = null;
            header.EditorRenderConfig.RawLabel = null;
            header.EditorRenderConfig.RawInfoHasSet = false;
        }

        private bool CanEdit(TableHeader header)
        {
            return header.IsEditable && header.Eval != null;
        }

        /// <summary>
        /// 当前处于编辑状态的行；未编辑时为 null。
        /// </summary>
        public object EditingRow => editingRow;

        /// <summary>
        /// 当前活动编辑单元格在 <see cref="EditableColumns"/> 中的下标；-1 表示没有活动单元格。
        /// </summary>
        public int ActiveEditingCellIndex => editingCellIndex;

        /// <summary>
        /// 当前活动编辑单元格对应的列；没有活动单元格时为 null。
        /// </summary>
        public TableHeader ActiveEditingColumn => activeEditingHeader;

        /// <summary>
        /// 可参与回车跳转的列（按显示顺序）。
        /// </summary>
        public IReadOnlyList<TableHeader> EditableColumns => GetEditingColumns();

        /// <summary>
        /// 从外部（按钮、失焦等）触发当前编辑行的保存。没有正在编辑的行时直接返回 false。
        /// 用于 <see cref="AutoSaveOnLeave"/> 失焦自动保存，以及页面「离开即保存」类交互。
        /// </summary>
        public async Task<bool> SaveEditingRowAsync()
        {
            if (editingRow == null || editingTaskCompletionSource == null)
            {
                return false;
            }

            _ = SaveDataAsync(SaveAction.Update);
            var result = await editingTaskCompletionSource.Task;
            editingTaskCompletionSource = null;
            if (result != 0)
            {
                editingTaskCompletionSource = new TaskCompletionSource<int>();
                return false;
            }

            foreach (var item in Headers)
            {
                item.EditingValue = null;
                if (item.EditorRenderConfig != null)
                {
                    item.EditorRenderConfig.EditingValue = null;
                }
            }

            Refresh();
            return true;
        }

        /// <summary>
        /// 从外部让指定行进入编辑状态（等价于双击该行）。供「进入编辑」按钮调用。
        /// </summary>
        public Task BeginEditRowAsync(object row) => BeginEditAsync(row);

        /// <summary>
        /// 表格根容器失焦时，若焦点离开当前编辑行且不是进入弹出层，则自动保存当前编辑行。
        /// </summary>
        private async Task OnTableFocusOutAsync()
        {
            if (!AutoSaveOnLeave || editingRow == null || editingTaskCompletionSource == null)
            {
                return;
            }

            // 交给 JS 用 document.activeElement 判断：焦点落在表格外的普通控件（如工具栏按钮）、
            // 或切到别的行 → 离开当前行，自动保存；焦点仍在当前编辑行内（同行动格）或落入弹层 → 不保存。
            var shouldSave = await JSRuntime.InvokeAsync<bool>("elementShouldAutoSaveOnBlur", Container);
            if (shouldSave)
            {
                await SaveEditingRowAsync();
            }
        }

        /// <summary>
        /// 可参与回车跳转的列：显示中、非复选框列、可编辑、且已初始化编辑器。
        /// </summary>
        private List<TableHeader> GetEditingColumns()
        {
            return Headers.Where(x => !x.Ignore
                && !x.IsCheckBox
                && x.EnterMovesToNextCell
                && CanEdit(x)
                && x.EditorRender != null).ToList();
        }

        /// <summary>
        /// 判断指定行的指定列是否为当前活动编辑单元格。
        /// </summary>
        private bool IsEditingCell(object row, TableHeader header)
        {
            return editingRow != null
                && ReferenceEquals(row, editingRow)
                && ReferenceEquals(header, activeEditingHeader);
        }

        /// <summary>
        /// 活动编辑单元格的 DOM id（同一实例内稳定，供 JS 定位并聚焦）。
        /// </summary>
        private string GetEditingCellId(object row, TableHeader header)
        {
            if (!IsEditingCell(row, header))
            {
                return null;
            }
            var rowIndex = rows.IndexOf(row);
            var columnIndex = Headers.IndexOf(header);
            if (rowIndex < 0 || columnIndex < 0)
            {
                return null;
            }
            return $"{tableDomId}_r{rowIndex}_c{columnIndex}";
        }

        /// <summary>
        /// 编辑行的单元格附加属性：活动单元格带 id，编辑行内的可编辑单元格带点击切换事件。
        /// 非编辑行返回空字典，避免给所有单元格注册事件、写 DOM 属性。
        /// </summary>
        /// <summary>
        /// 行的键盘事件（keydown / keyup）只在 <see cref="IsEditable"/> 时挂载。
        /// <para>原因（实测）：Blazor 在 UI 事件处理结束后会无条件调用 <c>StateHasChanged</c>，
        /// 而这两个处理器在非可编辑表里一开始就 <c>return</c>。挂着它们的后果是「每敲一个键
        /// 整表重渲染一次」，单元格里单向 <c>Value=</c> 的编辑器随即被按旧值打回 —— 表现为
        /// 输入框里打不进字。现改为按需挂载，非可编辑表不再有任何按键期重渲染。</para>
        /// </summary>
        protected IDictionary<string, object> GetRowKeyboardAttributes(object row)
        {
            // ⚠️ 即使非编辑态也必须挂载 onkeydown/onkeyup：OnEditingRowKeyDownAsync / OnRowKeyUpAsync
            // 内部已对 !IsEditable 做 early-return（no-op），但若此处不挂处理器，bUnit 的
            // Rows()[i].KeyDown() 会判「元素无 onkeydown 处理器」直接抛 MissingEventHandlerException
            // （即 NonEditableTable_EnterIsIgnored 测试失败的根因）。挂载后非编辑态回车=安全忽略。
            return new Dictionary<string, object>
            {
                ["onkeydown"] = EventCallback.Factory.Create<KeyboardEventArgs>(this, e => OnEditingRowKeyDownAsync(e, row)),
                ["onkeyup"] = EventCallback.Factory.Create<KeyboardEventArgs>(this, e => OnRowKeyUpAsync(e, row))
            };
        }

        protected IDictionary<string, object> GetEditingCellAttributes(object row, TableHeader header)
        {
            if (!IsEditable || editingRow == null || !ReferenceEquals(row, editingRow) || !CanEdit(header) || header.IsCheckBox)
            {
                return EmptyCellAttributes;
            }
            var attributes = new Dictionary<string, object>();
            if (IsEditingCell(row, header))
            {
                var id = GetEditingCellId(row, header);
                if (!string.IsNullOrWhiteSpace(id))
                {
                    attributes["id"] = id;
                }
            }
            attributes["onclick"] = EventCallback.Factory.Create<MouseEventArgs>(this, () => ActiveEditingCellByClickAsync(row, header));
            return attributes;
        }

        /// <summary>
        /// 点击编辑行内的单元格时，把活动单元格切换到该列。
        /// </summary>
        private Task ActiveEditingCellByClickAsync(object row, TableHeader header)
        {
            if (!IsEditable || editingRow == null || !ReferenceEquals(row, editingRow) || ReferenceEquals(header, activeEditingHeader))
            {
                return Task.CompletedTask;
            }
            var columns = GetEditingColumns();
            var index = columns.IndexOf(header);
            if (index < 0)
            {
                return Task.CompletedTask;
            }
            SetActiveEditingCell(header, index, false);
            StateHasChanged();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 设置活动单元格，并在本次渲染结束后把焦点移进去。
        /// </summary>
        private void SetActiveEditingCell(TableHeader header, int index, bool selectText)
        {
            activeEditingHeader = header;
            editingCellIndex = index;
            pendingFocusCell = editingRow == null ? null : (editingRow, header, selectText);
            MarkAsRequireRender();
        }

        /// <summary>
        /// 编辑状态下把活动单元格按当前 <see cref="EnterNavigationMode"/> 移动 <paramref name="direction"/> 个位置
        /// （-1 上一个，1 下一个）并聚焦。水平模式移动列，垂直模式移动行。返回是否发生了移动。
        /// </summary>
        public async Task<bool> MoveEditingCellAsync(int direction)
        {
            if (!IsEditable || editingRow == null || direction == 0)
            {
                return false;
            }
            var columns = GetEditingColumns();
            if (columns.Count == 0)
            {
                return false;
            }
            if (EnterNavigationMode == TableEnterNavigationMode.SameColumnNextRow)
            {
                return await MoveEditingCellSameColumnAsync(columns, direction);
            }
            var current = editingCellIndex;
            if (current < 0)
            {
                current = direction > 0 ? -1 : 1;
            }
            var next = current + direction;
            if (next < 0 || next >= columns.Count)
            {
                if (!EnterWrapsToNextRow)
                {
                    return false;
                }
                var rowIndex = rows.IndexOf(editingRow);
                if (rowIndex < 0)
                {
                    return false;
                }
                var targetRowIndex = FindVisibleRowIndex(rowIndex, direction);
                if (targetRowIndex < 0)
                {
                    return false;
                }
                SwitchEditingRow(targetRowIndex);
                next = direction > 0 ? 0 : columns.Count - 1;
            }
            SetActiveEditingCell(columns[next], next, true);
            StateHasChanged();
            await Task.CompletedTask;
            return true;
        }

        /// <summary>
        /// 竖直方向移动时当前列不变，仅在可见行的同一列内聚焦移动。
        /// 末行按回车（direction &gt; 0）改为跳到下一列、落在**该列的第一行**（批次㊾ 之前是留在当前行），仍是编辑态；
        /// 已是最后一列时不动（返回 false）。首行 Shift+Enter 保持原语义：不动（返回 false）。
        /// 与 dom.js 的 <c>elementTableEnterNextEditor</c>（常驻编辑器表格那条路）同一口径。
        /// </summary>
        private async Task<bool> MoveEditingCellSameColumnAsync(List<TableHeader> columns, int direction)
        {
            var columnIndex = editingCellIndex >= 0 && editingCellIndex < columns.Count ? editingCellIndex : 0;
            var rowIndex = rows.IndexOf(editingRow);
            if (rowIndex < 0)
            {
                return false;
            }
            var targetRowIndex = FindVisibleRowIndex(rowIndex, direction);
            if (targetRowIndex < 0)
            {
                if (direction < 0)
                {
                    return false;
                }
                var nextColumnIndex = columnIndex + 1;
                if (nextColumnIndex >= columns.Count)
                {
                    return false;
                }
                var firstRowIndex = FindVisibleRowIndex(-1, 1);
                if (firstRowIndex >= 0 && firstRowIndex != rowIndex)
                {
                    SwitchEditingRow(firstRowIndex);
                }
                SetActiveEditingCell(columns[nextColumnIndex], nextColumnIndex, true);
                StateHasChanged();
                await Task.CompletedTask;
                return true;
            }
            SwitchEditingRow(targetRowIndex);
            SetActiveEditingCell(columns[columnIndex], columnIndex, true);
            StateHasChanged();
            await Task.CompletedTask;
            return true;
        }

        /// <summary>
        /// 从 <paramref name="fromIndex"/> 出发按方向找最近一个可见行下标；越界返回 -1。
        /// </summary>
        private int FindVisibleRowIndex(int fromIndex, int direction)
        {
            var index = fromIndex + direction;
            while (index >= 0 && index < rows.Count && hiddenRows.Contains(rows[index]))
            {
                index += direction;
            }
            return index >= 0 && index < rows.Count ? index : -1;
        }

        /// <summary>
        /// 切换编辑行：重新初始化各列编辑器，避免新行沿用上一行未保存的编辑值。
        /// </summary>
        private void SwitchEditingRow(int targetRowIndex)
        {
            foreach (var item in Headers)
            {
                Clear(item);
            }
            editingRow = rows[targetRowIndex];
        }

        /// <summary>
        /// 编辑行内按回车 → 跳到下一个可编辑单元格（Shift + 回车 → 上一个）。
        /// </summary>
        private async Task OnEditingRowKeyDownAsync(KeyboardEventArgs e, object row)
        {
            if (!IsEnterKey(e))
            {
                return;
            }
            if (!IsEditable)
            {
                await MoveFocusToNextEditorAsync(e.ShiftKey);
                return;
            }
            if (!EnterMovesToNextCell || editingRow == null || !ReferenceEquals(row, editingRow))
            {
                return;
            }
            await MoveEditingCellAsync(e.ShiftKey ? -1 : 1);
        }

        /// <summary>
        /// 常驻编辑器表格的回车跳格：把「找下一格并聚焦」整段交给 JS（DOM 顺序就是行列顺序，
        /// 这里不参与列的反射/编辑态判断），失败或没装 dom.js 都只当没跳。
        /// </summary>
        private async Task MoveFocusToNextEditorAsync(bool shift)
        {
            if (!EnterNavigatesEditors || !EnterMovesToNextCell)
            {
                return;
            }
            try
            {
                await JSRuntime.InvokeAsync<bool>("elementTableEnterNextEditor", shift, EnterCarriesValue);
            }
            catch (Exception)
            {
                // 宿主页面未引用 dom.js 时忽略：只影响回车跳格，不影响表格其它功能
            }
        }

        /// <summary>
        /// 行上的 keyup：编辑行内的回车已经由 keydown 处理成“跳到下一个单元格”，这里不再触发保存
        /// 只有点到单元格、或已经处于编辑中时，回车也不再触发保存（避免与当前 saveAction 逻辑重复保存一次）。 saveAction 误保存一次）。
        /// </summary>
        private async Task OnRowKeyUpAsync(KeyboardEventArgs e, object row)
        {
            if (IsEditable && EnterMovesToNextCell && editingRow != null && ReferenceEquals(row, editingRow) && IsEnterKey(e))
            {
                return;
            }
            if (!IsEditable || editingRow != null)
            {
                return;
            }
            await SaveTableAsync(e);
        }

        private static bool IsEnterKey(KeyboardEventArgs e)
        {
            return e.Key == "Enter" || e.Code == "Enter" || e.Code == "NumpadEnter";
        }

        /// <summary>
        /// 渲染结束后聚焦挂起的单元格（跨渲染周期，保证 DOM 已更新）。
        /// </summary>
        private async Task FocusPendingEditingCellAsync()
        {
            var pending = pendingFocusCell;
            if (pending == null)
            {
                return;
            }
            pendingFocusCell = null;
            if (editingRow == null || !ReferenceEquals(pending.Value.Row, editingRow))
            {
                return;
            }
            var cellId = GetEditingCellId(pending.Value.Row, pending.Value.Header);
            if (string.IsNullOrWhiteSpace(cellId))
            {
                return;
            }
            try
            {
                await JSRuntime.InvokeVoidAsync("elementTableFocusCell", cellId, pending.Value.SelectText);
            }
            catch (Exception)
            {
                // 宿主页面未引用 dom.js 时忽略：只影响自动聚焦，不影响表格其它功能
            }
        }

        /// <summary>
        /// 双击进入编辑时，取鼠标位置所在的列作为起始编辑单元格；取不到时从第一个可编辑列开始。
        /// </summary>
        private async Task<TableHeader> GetHeaderByMousePositionAsync(MouseEventArgs e)
        {
            if (e == null)
            {
                return null;
            }
            try
            {
                var ordinal = await JSRuntime.InvokeAsync<int>("elementTableColumnIndexAt", e.ClientX, e.ClientY);
                return GetHeaderByDomOrdinal(ordinal);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 按单元格在行内的 DOM 序号（含选择列）反查列定义。
        /// </summary>
        private TableHeader GetHeaderByDomOrdinal(int ordinal)
        {
            var index = ordinal - (HasSelectionColumn ? 1 : 0);
            if (index < 0)
            {
                return null;
            }
            var rendered = Headers.Where(x => !x.Ignore).ToList();
            return index < rendered.Count ? rendered[index] : null;
        }

        private async Task BeginEditAsync(object row, MouseEventArgs e = null)
        {
            if (OnRowDblClick.HasDelegate)
            {
                await OnRowDblClick.InvokeAsync(row);
            }
            if (!IsEditable)
            {
                return;
            }
            saveAction = SaveAction.Create;
            var rowChanged = !ReferenceEquals(editingRow, row);
            if (rowChanged)
            {
                // 退出编辑状态时清空活动单元格
                foreach (var item in Headers)
                {
                    Clear(item);
                }
            }
            editingRow = row;
            // 双击进入编辑时必须建立这个信号，否则操作列按钮（此时已显示为「保存」）
            // 第一次点击会走「进入编辑」分支而什么都不保存，要点第二次才真正提交。
            if (editingTaskCompletionSource == null)
            {
                editingTaskCompletionSource = new TaskCompletionSource<int>();
            }
            var startHeader = rowChanged ? await GetHeaderByMousePositionAsync(e) : null;
            var columns = GetEditingColumns();
            if (columns.Count > 0)
            {
                var index = startHeader == null ? 0 : columns.IndexOf(startHeader);
                if (index < 0)
                {
                    index = 0;
                }
                SetActiveEditingCell(columns[index], index, true);
            }
            MarkAsRequireRender();
            StateHasChanged();
        }

        private async Task ToggleTreeAsync(object row)
        {
            var treeRow = row as TreeItemBase;
            if (treeRow == null || treeRow.IsLoading)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(Url))
            {
                // EP 口径（2.14.0 store/tree.ts toggleTreeExpansion + table-body/render-helper.ts 的
                // display = parent.display && parent.expanded）：只翻转被点这一个节点自己的 expanded，
                // 子孙的标志一律不动。旧写法是「展开＝把整棵子树从 hiddenRows 里摘掉」，点父级一次就把
                // 孙子曾孙全摊开，而里层节点自己的 Expanded 仍是 false —— 再点它的箭头第一下走的是「展开」
                // 分支、行本来就在，等于什么也没变，第二下才轮到「折叠」，即用户说的「有时要点两次」。
                treeRow.Expanded = !treeRow.Expanded;
                treeRow.Direction = treeRow.Expanded ? "right" : "up";
                SyncTreeHiddenRows();
            }
            else
            {
                var finalChildren = GetAllChildren(treeRow);
                if (treeRow.Expanded)
                {
                    treeRow.Expanded = false;
                    rows.RemoveAll(finalChildren.Contains);
                    DataSource = rows;
                    await InitlizeDataSourceAsync(0, "up", false);
                    if (DataSourceChanged.HasDelegate)
                    {
                        await DataSourceChanged.InvokeAsync(DataSource);
                    }
                    treeRow.Direction = "up";
                }
                else
                {
                    await LoadChildrenAsync(treeRow, false);
                }
            }
            MarkAsRequireRender();
        }

        private void LocalExpand(TreeItemBase treeRow, List<TreeItemBase> finalChildren)
        {
            hiddenRows.RemoveAll(finalChildren.Contains);
            treeRow.Direction = "right";
            treeRow.Expanded = true;
        }

        /// <summary>
        /// 本地树形数据（未设置 <see cref="Url"/>）的可见性真源：<c>hiddenRows</c> 一律由各节点的
        /// <see cref="TreeItemBase.Expanded"/> 推导 —— 一行可见 ⇔ 它往上每一级祖先都处于展开状态，
        /// 与 EP 2.14.0 table-body/render-helper.ts:249 的 <c>display: parent.display &amp;&amp; parent.expanded</c> 同构。
        /// 之所以要整表重算而不是增量增删：<c>hiddenRows</c> 一旦与 <c>Expanded</c> 脱钩，就会出现
        /// 「点了箭头行没变化、再点一次才动」——见 <see cref="ToggleTreeAsync"/> 里的说明。
        /// </summary>
        private void SyncTreeHiddenRows()
        {
            hiddenRows.Clear();
            var treeRows = rows.OfType<TreeItemBase>().ToList();
            if (treeRows.Count == 0)
            {
                return;
            }

            var byId = new Dictionary<int, TreeItemBase>();
            foreach (var item in treeRows)
            {
                byId[item.Id] = item;
            }

            foreach (var item in treeRows)
            {
                var parentId = item.ParentId;
                // visited 兼作环保护：数据源里出现父子互指时不能死循环（FindChildren 有同类防护）
                var visited = new HashSet<int>();
                while (parentId != 0 && visited.Add(parentId))
                {
                    if (!byId.TryGetValue(parentId, out var parent))
                    {
                        // 祖先不在当前行集（筛选 / 分页把父级滤掉了）：按顶层处理，别把它藏起来
                        break;
                    }
                    if (!parent.Expanded)
                    {
                        hiddenRows.Add(item);
                        break;
                    }
                    parentId = parent.ParentId;
                }
            }
        }

        private List<TreeItemBase> GetAllChildren(TreeItemBase treeRow)
        {
            var treeRows = rows.Cast<TreeItemBase>().ToArray();
            var children = treeRows.Where(x => x.ParentId == treeRow.Id).ToList();
            var finalChildren = children.ToList();
            foreach (var item in children)
            {
                finalChildren.AddRange(FindChildren(item));
            }

            return finalChildren;
        }

        private async Task LoadChildrenAsync(TreeItemBase row, bool autoExpand)
        {
            row.Expanded = true;
            row.IsLoading = true;
            Refresh();
            var newDataSource = await FetchChildrenAsync(row);
            row.IsLoading = false;
            var enumerbale = DataSource as IEnumerable;
            var startIndex = 0;
            foreach (var item in enumerbale)
            {
                if (item == row)
                {
                    break;
                }
                startIndex++;
            }
            var oldDataSource = enumerbale.Cast<object>().ToList();
            oldDataSource.InsertRange(startIndex + 1, (newDataSource as IEnumerable).Cast<object>());
            DataSource = oldDataSource;
            await InitlizeDataSourceAsync(row.Level.Value + 1, "up", autoExpand);
            if (DataSourceChanged.HasDelegate)
            {
                await DataSourceChanged.InvokeAsync(DataSource);
            }
        }

        private async Task<IEnumerable> FetchChildrenAsync(TreeItemBase treeRow)
        {
            treeRow.Direction = "right";
            if (!Url.EndsWith("/"))
            {
                Url += "/";
            }

            var newDataSource = (IEnumerable)await httpClient.GetFromJsonAsync(Url + treeRow.Id, typeof(List<>).MakeGenericType(DataType));
            if (!newDataSource.Cast<object>().Any())
            {
                treeRow.HasChildren = false;
            }
            return newDataSource;
        }

        public override void MarkAsRequireRender()
        {
            base.MarkAsRequireRender();
            if (editingRow == null && !renderCompleted)
            {
                headerRendered = false;
                headerInitilized = false;
                // 注意：这里不能清 registeredColumnSignatures——重置 headerInitilized 会让列组件
                // OnInitialized 重跑并再次 AddColumn，清掉签名后去重失效，Headers 会出现重复表头，
                // 进而触发 <col @key="header.Text"> 同 key 渲染崩溃。签名仅在 ClearColumns/ReloadColumns
                // （同步清空 Headers）时一并清空。
            }
        }

        private List<TreeItemBase> FindChildren(TreeItemBase child, HashSet<TreeItemBase> visited = null)
        {
            visited ??= new HashSet<TreeItemBase>();
            var results = new List<TreeItemBase>();
            if (child == null || !visited.Add(child))
            {
                // 数据源存在环 / 重复引用时直接返回，避免无限递归
                return results;
            }

            var treeItems = rows.Cast<TreeItemBase>().Where(x => x != null && x.ParentId == child.Id).ToArray();
            foreach (var childTreeItem in treeItems)
            {
                results.Add(childTreeItem);
                results.AddRange(FindChildren(childTreeItem, visited));
            }
            return results;
        }

        internal async Task CurrentPageChangedAsync(int page)
        {
            CurrentPage = page;
            if (CurrentPageChanged.HasDelegate)
            {
                RequireRender = true;
                var option = new LoadingOption()
                {
                    Background = LoadingBackground,
                    Target = Container,
                    IconClass = LoadingIconClass,
                    Text = LoadingText
                };
                LoadingService.LoadingOptions.Add(option);
                SelectedRows.Clear();
                await CurrentPageChanged.InvokeAsync(page);
                LoadingService.LoadingOptions.Remove(option);
            }
        }

        void ResetSelectAllStatus()
        {
            if (rows.Count == 0 || SelectedRows == null || SelectedRows.Count == 0)
            {
                selectAllStatus = Status.UnChecked;
            }
            else if (rows.Count > SelectedRows.Count)
            {
                selectAllStatus = Status.Indeterminate;
            }
            else
            {
                selectAllStatus = Status.Checked;
            }
        }

        protected void ChangeAllStatus(Status status)
        {
            RequireRender = true;
            if (status == Status.Checked)
            {
                SelectedRows = new HashSet<object>(rows);
            }
            else
            {
                SelectedRows = new HashSet<object>();
            }

            foreach (var item in rowCheckBoxses.Keys)
            {
                item.MarkAsRequireRender();
            }
            if (SelectedRowsChanged.HasDelegate)
            {
                _ = SelectedRowsChanged.InvokeAsync(SelectedRows);
            }
            ResetSelectAllStatus();
        }

        protected void ChangeRowStatus(Status status, object row)
        {
            RequireRender = true;
            chkAll?.MarkAsRequireRender();
            if (status == Status.Checked)
            {
                if (!SelectionMultiple)
                {
                    SelectedRows = new HashSet<object> { row };
                    foreach (var item in rowCheckBoxses.Keys)
                    {
                        item.MarkAsRequireRender();
                    }
                }
                else
                {
                    SelectedRows.Add(row);
                }
            }
            else
            {
                SelectedRows.Remove(row);
            }
            if (SelectedRowsChanged.HasDelegate)
            {
                _ = SelectedRowsChanged.InvokeAsync(SelectedRows);
            }
            ResetSelectAllStatus();
        }

        protected async Task ToggleSortAsync(TableHeader header)
        {
            if (header == null || !header.Sortable || header.Property == null)
            {
                return;
            }
            if (activeSortHeader != null && activeSortHeader != header)
            {
                activeSortHeader.SortOrder = TableSortOrder.None;
            }
            activeSortHeader = header;
            header.SortOrder = header.SortOrder switch
            {
                TableSortOrder.None => TableSortOrder.Ascending,
                TableSortOrder.Ascending => TableSortOrder.Descending,
                _ => TableSortOrder.None
            };
            if (header.SortOrder == TableSortOrder.None)
            {
                activeSortHeader = null;
            }
            ApplyTableQuery();
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// 表头单元格点击入口：依次触发表级 <see cref="HeaderCellClick"/>（参数为 (列, 列索引)）
        /// 与列级 <see cref="TableHeader.OnHeaderClick"/>（参数为该列 <see cref="TableHeader"/>）。
        /// 由表头 &lt;th&gt; 的 @onclick 调用。点击排序箭头 / 筛选项时因已 stopPropagation 不会重复触发。
        /// </summary>
        protected async Task OnHeaderCellClickAsync(TableHeader header, int columnIndex)
        {
            if (header == null)
            {
                return;
            }
            if (HeaderCellClick.HasDelegate)
            {
                await HeaderCellClick.InvokeAsync((header, columnIndex));
            }
            if (header.OnHeaderClick.HasDelegate)
            {
                await header.OnHeaderClick.InvokeAsync(header);
            }
        }

        /// <summary>面板是否正开在这一列上（表头漏斗据此加 <c>is-open</c> 与 <c>aria-expanded</c>）。</summary>
        protected bool IsFilterPanelOpen(TableHeader header)
            => header != null && ReferenceEquals(openFilterHeader, header);

        /// <summary>
        /// 点击表头漏斗：收起已开着的同一列，否则量一次漏斗的视口矩形、把面板坐标定下来再渲染。
        /// 一次只开一列，所以点另一列等于「关掉上一列、打开这一列」。
        /// </summary>
        protected async Task ToggleFilterPanelAsync(TableHeader header)
        {
            if (header == null)
            {
                return;
            }
            if (ReferenceEquals(openFilterHeader, header))
            {
                CloseFilterPanel();
                return;
            }
            openFilterHeader = header;
            filterPanelRenderKey++;
            filterPanelStyle = await MeasureFilterPanelStyleAsync(header);
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// 用漏斗的视口矩形算面板内联样式。量不到（元素未渲染 / JS 未就绪）就返回 <c>null</c>，
        /// CSS 里的默认 <c>left/top</c> 兜底 —— 面板宁位置略偏也不能不出现。
        /// </summary>
        private async Task<string> MeasureFilterPanelStyleAsync(TableHeader header)
        {
            try
            {
                var rect = await JSRuntime.InvokeAsync<ElementViewportRect>(TableFilterPopper.MeasureFunction, header.FilterTriggerElement);
                return TableFilterPopper.BuildPanelStyle(rect, header.AdvancedFilter);
            }
            catch (JSException)
            {
                // 宿主用的是旧版 dom.js（缓存未刷新）时这里会抛，等价于「量不到」。
                return null;
            }
        }

        /// <summary>收起面板（点遮罩、滚轮、以及再次点同一列的漏斗都走这里）。</summary>
        protected void CloseFilterPanel()
        {
            if (openFilterHeader == null)
            {
                return;
            }
            openFilterHeader = null;
            filterPanelStyle = null;
            InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// 面板把草稿落到了列上：重算过滤、刷新，并按 EP 的 filter-change 口径抛 <see cref="FilterChange"/>。
        /// 条件式面板（<c>AdvancedFilter</c>）抛的是各条件的值，运算符仍以面板状态为准。
        /// </summary>
        protected async Task OnFilterPanelCommittedAsync(TableHeader header)
        {
            if (header == null)
            {
                return;
            }
            ApplyTableQuery();
            await InvokeAsync(StateHasChanged);
            if (FilterChange.HasDelegate)
            {
                var values = header.AdvancedFilter
                    ? header.AdvancedConditions.Where(x => x.IsActive).Select(x => x.Value).ToArray()
                    : header.FilterValues.ToArray();
                await FilterChange.InvokeAsync((header.Property?.Name ?? header.Text, values));
            }
        }

        private void ApplyTableQuery()
        {
            if (sourceRows == null || sourceRows.Count == 0)
            {
                return;
            }
            IEnumerable<object> query = sourceRows;
            foreach (var header in Headers.Where(x => x.HasActiveFilter))
            {
                query = query.Where(row => MatchesFilter(row, header));
            }
            if (activeSortHeader == null && !string.IsNullOrWhiteSpace(DefaultSortProp))
            {
                var dsHeader = Headers.FirstOrDefault(x => (x.Property?.Name == DefaultSortProp) || x.Text == DefaultSortProp);
                if (dsHeader != null)
                {
                    dsHeader.SortOrder = DefaultSortOrder;
                    activeSortHeader = dsHeader;
                }
            }
            var sortHeader = activeSortHeader ?? Headers.FirstOrDefault(x => x.SortOrder != TableSortOrder.None);
            if (sortHeader != null && sortHeader.Property != null && sortHeader.SortOrder != TableSortOrder.None)
            {
                query = sortHeader.SortOrder == TableSortOrder.Ascending
                    ? query.OrderBy(row => sortHeader.EvalRaw?.Invoke(row))
                    : query.OrderByDescending(row => sortHeader.EvalRaw?.Invoke(row));
            }
            rows = query.ToList();
            ResetSelectAllStatus();
        }

        private bool MatchesFilter(object row, TableHeader header)
        {
            if (!header.HasActiveFilter)
            {
                return true;
            }
            var cellValue = header.EvalRaw?.Invoke(row);
            // 条件式面板接管这一列时不再看选项勾选（面板也不再渲染选项列表），两套判据不叠加。
            if (header.AdvancedFilter)
            {
                return MatchesAdvancedFilter(cellValue, header);
            }
            foreach (var filterValue in header.FilterValues)
            {
                if (header.FilterMethod != null && header.FilterMethod(filterValue, row))
                {
                    return true;
                }
                if (Equals(cellValue, filterValue) || string.Equals(Convert.ToString(cellValue), Convert.ToString(filterValue), StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 条件链的求值：同列多条按各自 <c>Relation</c> 串起来（首条的关系位忽略），
        /// 一条都没有时不设限（面板「清除」后即整列放行）。
        /// </summary>
        private static bool MatchesAdvancedFilter(object cellValue, TableHeader header)
        {
            bool? result = null;
            foreach (var condition in header.AdvancedConditions.Where(x => x != null && x.IsActive))
            {
                var matched = condition.Matches(cellValue);
                if (result == null)
                {
                    result = matched;
                }
                else
                {
                    result = condition.Relation == TableFilterRelation.Or
                        ? (result.Value || matched)
                        : (result.Value && matched);
                }
            }
            return result ?? true;
        }

        protected string GetHeaderCellClass(TableHeader header, int columnIndex)
        {
            return GetCellClass(header, columnIndex, true, "is-leaf")
                + (header.Sortable ? " is-sortable" : string.Empty)
                + (header.SortOrder == TableSortOrder.Ascending ? " ascending" : string.Empty)
                + (header.SortOrder == TableSortOrder.Descending ? " descending" : string.Empty)
                + (header.HasActiveFilter ? " filtered" : string.Empty);
        }

        protected string GetBodyCellClass(TableHeader header, int columnIndex, object row = null)
        {
            return GetCellClass(header, columnIndex, false, IsEditingCell(row, header) ? EditingCellClass : null);
        }

        /// <summary>
        /// 分组表头单元格的 class：与叶子列同名族（el-table_1_column_N，序号取组内第一个可见子列），
        /// 但不带 is-leaf —— EP/element-ui 给有子列的表头是 is-group，is-leaf 的边框只属于最后一行表头。
        /// </summary>
        protected string GetGroupHeaderCellClass(TableHeader group, int columnIndex)
        {
            return GetCellClass(group, columnIndex, true) + " is-group";
        }

        private string GetCellClass(TableHeader header, int columnIndex, bool isHeader, params string[] extraClasses)
        {
            // EP aligns a column with the is-center / is-right class on th/td (index.css).
            // HeaderAlign wins for header cells, Align is used for body cells.
            var align = GetCellAlign(header, isHeader)?.Trim().ToLowerInvariant();
            var builder = HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add($"el-table_1_column_{columnIndex}", "el-table__cell")
                .Add(extraClasses)
                .AddIf(header?.Fixed == TableColumnFixed.Left, "is-fixed-left")
                .AddIf(header?.Fixed == TableColumnFixed.Right, "is-fixed-right")
                .AddIf(align == "center", "is-center")
                .AddIf(align == "right", "is-right");
            return builder.ToString();
        }
        // ============ Element Plus 渲染辅助方法 ============

        /// <summary>计算行 class（highlight-current-row + row-class-name 函数/字符串 + 当前行标识）</summary>
        internal string GetRowClass(object row, int rowIndex)
        {
            var cls = new System.Text.StringBuilder();
            // 斑马纹
            if (Stripe && rowIndex % 2 == 1) cls.Append("el-table__row--striped ");
            // 高亮当前行
            if (HighlightCurrentRow && CurrentRow != null && object.Equals(CurrentRow, row))
                cls.Append("current-row ");
            // 展开行
            if (ExpandRowKeys != null && ExpandRowKeys.Contains(row)) cls.Append("el-table__expanded-cell ");
            // row-class-name 回调
            var customCls = RowClassName?.Invoke(row, rowIndex) ?? RowClassNameString;
            if (!string.IsNullOrEmpty(customCls)) cls.Append(customCls).Append(' ');
            return cls.ToString().Trim();
        }

        /// <summary>计算行 style</summary>
        internal Dictionary<string, string> GetRowStyle(object row, int rowIndex)
        {
            var dict = RowStyle?.Invoke(row, rowIndex) ?? RowStyleObject;
            return dict;
        }

        /// <summary>表头行 class（对齐 EP header-row-class-name）</summary>
        internal string GetHeaderRowClass()
        {
            var cls = HeaderRowClassName?.Invoke(null, 0);
            return string.IsNullOrEmpty(cls) ? string.Empty : cls;
        }

        /// <summary>表头行 style（对齐 EP header-row-style）</summary>
        internal Dictionary<string, string> GetHeaderRowStyle()
        {
            return HeaderRowStyle?.Invoke(null, 0);
        }

        /// <summary>单元格点击入口：触发 CellClick（载荷为 (行, 列, 行下标, 列下标)）。
        /// 由数据单元格 &lt;td&gt; 的 @onclick 调用；事件冒泡到 &lt;tr&gt; 会继续触发 RowClick（与 EP 一致）。</summary>
        protected async Task OnCellClickAsync(object row, TableHeader header, int rowIndex, int columnIndex)
        {
            if (CellClick.HasDelegate)
            {
                await CellClick.InvokeAsync((row, header, rowIndex, columnIndex));
            }
        }

        /// <summary>切换某行的展开/收起（扩展行），并在变更后触发 ExpandChange（载荷为当前展开行键集合副本，对齐 EP expand-change）。</summary>
        public async Task ToggleRowExpandAsync(object row)
        {
            if (row == null) return;
            if (ExpandRowKeys.Contains(row))
            {
                ExpandRowKeys.Remove(row);
            }
            else
            {
                ExpandRowKeys.Add(row);
            }
            await InvokeAsync(StateHasChanged);
            if (ExpandChange.HasDelegate)
            {
                await ExpandChange.InvokeAsync(new HashSet<object>(ExpandRowKeys));
            }
        }

        /// <summary>单元格对齐：column.Align 优先于 header.Align</summary>
        internal string GetCellAlign(TableHeader header, bool isHeader = false)
        {
            if (header == null) return "left";
            if (isHeader && !string.IsNullOrEmpty(header.HeaderAlign)) return header.HeaderAlign;
            if (!string.IsNullOrEmpty(header.Align)) return header.Align;
            return "left";
        }

        /// <summary>全局覆盖：全局 ShowOverflowTooltip 或列级别 ShowOverflowTooltip</summary>
        internal bool GetShowOverflowTooltip(TableHeader header)
        {
            if (header != null && header.ShowOverflowTooltip) return true;
            return ShowOverflowTooltip;
        }

        /// <summary>这一列的单元格是否挂「悬停看全文」。EP 的 <c>show-overflow-tooltip</c>，默认关。</summary>
        protected bool CellOverflowEnabled(TableHeader header) => GetShowOverflowTooltip(header);

        /// <summary>
        /// 单元格走不走 <see cref="TableHeader.Formatter"/>。EP 2.14.0 的口径是「插槽优先，其次 formatter，最后默认渲染」
        /// （<c>table-column/render-helper.ts:171-178</c> 有 <c>slots.default</c> 就不调 <c>defaultRenderCell</c>，
        /// 而 <c>config.ts:202-207</c> 里 formatter 一命中就 <c>return</c>，不再 <c>value.toString()</c>）。
        /// <para>
        /// 这里只判「有没有 formatter」就够了：<c>ElTable.razor</c> 的两处单元格都是先 <c>index</c> → 先勾选列 →
        /// 先模板，落到 formatter 这一档时那三种都已经出局（实测：把这里的 <c>Template == null</c> /
        /// <c>Type != "index"</c> / <c>!IsCheckBox</c> 全删掉，全量 2836 例仍全绿 —— 见
        /// Temp/b105-mut-M6-1.log；真正钉住优先级的是 <c>TemplateColumn_WinsOverFormatter</c> 与
        /// <c>IndexTypeColumn_DoesNotRunFormatter</c> 这两支改呈现树顺序才会红的用例）。
        /// </para>
        /// </summary>
        protected bool UseCellFormatter(TableHeader header) =>
            header != null && header.Formatter != null;

        /// <summary>
        /// 用列上的 <c>Formatter</c> 算出单元格内容并包成片段。参数按 EP <c>config.ts:204-205</c> 的
        /// <c>formatter(row, column, value, $index)</c>：<b>第三个参数是原始值</b>（不是默认渲染器 <c>ToString()</c>
        /// 之后的文本），第四个是当前页内 0 基下标。
        /// <para>
        /// 三档落地（<c>RenderFragment</c> / <c>string</c> + <see cref="EnableMarkupString"/> / 其余 <c>ToString()</c>）
        /// 只能写在这里，不能图省事在 <c>.razor</c> 里 <c>@((object)v)</c>：Razor 对 <c>object</c> 静态类型走
        /// <c>RenderTreeBuilder.AddContent(int, object)</c>，那个重载只认 <c>string</c>，其余一律 <c>ToString()</c>
        /// （实测 <c>MarkupString</c> 被转义成 <c>&amp;lt;b&amp;gt;</c>、<c>RenderFragment</c> 直接打成类名）。
        /// </para>
        /// </summary>
        protected RenderFragment FormattedCellFragment(TableHeader header, object row, int index) => builder =>
        {
            var value = FormattedCellValue(header, row, index);
            var seq = 0;
            if (value is RenderFragment fragment)
            {
                builder.AddContent(seq++, fragment);
            }
            else if (EnableMarkupString && value is string text)
            {
                builder.AddMarkupContent(seq++, text);
            }
            else
            {
                builder.AddContent(seq++, value?.ToString() ?? string.Empty);
            }
        };

        /// <summary>取列上 <c>Formatter</c> 的原始返回值（未渲染）。第三个参数按 EP 传属性原值。</summary>
        protected object FormattedCellValue(TableHeader header, object row, int index)
        {
            var raw = header.EvalRaw != null ? header.EvalRaw(row)
                : header.Property != null ? header.Property.GetValue(row) : null;
            return header.Formatter(row, header, raw, index);
        }

        /// <summary>
        /// 单元格的类名。开着时多一个 <c>el-tooltip</c> 类：EP 的 <c>table.scss:197-199</c> 靠它把
        /// <c>.cell</c> 从「换行显示全」换成「不换行 + 省略号」，同时 <c>table-column/render-helper.ts:193-200</c>
        /// 也用它作为悬停判据的开关。
        /// </summary>
        protected string CellOverflowClass(TableHeader header) => GetShowOverflowTooltip(header) ? "cell el-tooltip" : "cell";

        /// <summary>
        /// EP <c>table-column/render-helper.ts:196-197</c> 的内联宽度：<c>(realWidth || Number(width)) - 1 px</c>。
        /// 本仓没有 EP 那套布局引擎算 realWidth，用与 <c>&lt;colgroup&gt;</c> 同源的列宽代替；
        /// 百分比列宽折算不出像素（<see cref="ResolveColumnWidthAsInt"/> 返回 0）时不设宽度，交给
        /// <c>table-layout:fixed</c> 或宿主自己的列宽约束 —— 这一条差异记在 docs/table-overflow-tooltip.md。
        /// </summary>
        protected string CellOverflowStyle(TableHeader header)
        {
            if (!GetShowOverflowTooltip(header))
            {
                return null;
            }

            // auto 列（批次㊸：无 width 属性、由表格摊剩余宽度）折不出真实像素，
            // 走下面那条分支会把 .cell 钉成 79px，文字在很宽的列里提前打点。百分比列同理由
            // ResolveColumnWidthAsInt 返回 0、被下面的 width > 1 挡掉。
            if (ResolveColumnWidth(header) == null)
            {
                return null;
            }

            var width = ResolveColumnWidthAsInt(header);
            return width > 1 ? "width:" + (width - 1) + "px" : null;
        }

        /// <summary>计算全局 EmptyText 覆盖</summary>
        /// <summary>
        /// 排序箭头的可访问名称。此前只有 <c>aria-sort</c> 与两个 caret，读屏说不出"按哪一列排序"；
        /// EP 用 <c>t('el.table.sortLabel', { column })</c>。列名为空时返回 null（属性整体不渲染）。
        /// </summary>
        private string SortLabelOf(TableHeader column)
        {
            var name = column?.Text;
            return string.IsNullOrWhiteSpace(name)
                ? null
                : Lang.T("el.table.sortLabel", "Sort by {column}", ("column", name));
        }

        /// <summary>筛选漏斗的可访问名称，同上（EP：<c>el.table.filterLabel</c>）。</summary>
        private string FilterLabelOf(TableHeader column)
        {
            var name = column?.Text;
            return string.IsNullOrWhiteSpace(name)
                ? null
                : Lang.T("el.table.filterLabel", "Filter by {column}", ("column", name));
        }

        internal string EffectiveEmptyText =>
            !string.IsNullOrEmpty(EmptyTextOverride) ? EmptyTextOverride :
            (!string.IsNullOrEmpty(EmptyText) ? EmptyText : Lang.T("el.table.emptyText", "暂无数据"));

        /// <summary>把样式字典序列化成 style 属性字符串。
        /// Blazor 的 <c>style="@dict"</c> 会把字典 ToString() 原样写进 DOM（实测 th 的 style 值为
        /// "System.Collections.Generic.Dictionary`2[System.String,System.String]"），浏览器把整条声明当非法
        /// CSS 丢掉 —— 于是按字典传的 RowStyle / HeaderRowStyle / HeaderCellStyle / CellStyle 全部不生效。
        /// 空字典返回 null，Blazor 便不渲染这个属性。</summary>
        internal static string StyleToString(Dictionary<string, string> dict)
        {
            if (dict == null || dict.Count == 0) return null;
            var builder = HtmlPropertyBuilder.CreateCssStyleBuilder();
            foreach (var kv in dict)
            {
                if (string.IsNullOrWhiteSpace(kv.Key) || string.IsNullOrWhiteSpace(kv.Value)) continue;
                builder.Add(kv.Key.Trim() + ":" + kv.Value.Trim());
            }
            var text = builder.ToString();
            return string.IsNullOrEmpty(text) ? null : text;
        }

        /// <summary>合并 CellClassName（全局 + 列级别）</summary>
        internal string MergeCellClass(TableHeader header, object row, int rowIndex, int columnIndex, string existing = null)
        {
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(existing)) sb.Append(existing).Append(' ');
            if (header != null && !string.IsNullOrEmpty(header.ClassName)) sb.Append(header.ClassName).Append(' ');
            var globalCls = CellClassName?.Invoke(row, header, rowIndex, columnIndex) ?? CellClassNameString;
            if (!string.IsNullOrEmpty(globalCls)) sb.Append(globalCls).Append(' ');
            return sb.ToString().Trim();
        }

        /// <summary>计算单元格 style 合并</summary>
        internal Dictionary<string, string> MergeCellStyle(TableHeader header, object row, int rowIndex, int columnIndex)
        {
            var dict = new Dictionary<string, string>();
            if (CellStyleObject != null)
                foreach (var kv in CellStyleObject) dict[kv.Key] = kv.Value;
            var fn = CellStyle?.Invoke(row, header, rowIndex, columnIndex);
            if (fn != null)
                foreach (var kv in fn) dict[kv.Key] = kv.Value;
            // 表头单元格 style（对齐 EP header-cell-style）：表头行的 row 参数为 null
            if (row == null)
            {
                var hfn = HeaderCellStyle?.Invoke(null, header, rowIndex, columnIndex);
                if (hfn != null)
                    foreach (var kv in hfn) dict[kv.Key] = kv.Value;
            }
            return dict.Count == 0 ? null : dict;
        }

        /// <summary>表头单元格 class 合并</summary>
        internal string MergeHeaderCellClass(TableHeader header, int columnIndex, string existing = null)
        {
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(existing)) sb.Append(existing).Append(' ');
            if (header != null && !string.IsNullOrEmpty(header.LabelClassName)) sb.Append(header.LabelClassName).Append(' ');
            var globalCls = HeaderCellClassName?.Invoke(null, header, 0, columnIndex);
            if (!string.IsNullOrEmpty(globalCls)) sb.Append(globalCls).Append(' ');
            return sb.ToString().Trim();
        }

        /// <summary>触发排序变化事件（替换原 ToggleSortAsync 中的逻辑）</summary>
        internal async Task EmitSortChangeAsync(TableHeader header)
        {
            if (header == null || !header.Sortable) return;
            await SortChange.InvokeAsync((header.Property?.Name ?? header.Text, header.SortOrder));
        }
    }
}