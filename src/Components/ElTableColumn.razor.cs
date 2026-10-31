using Microsoft.AspNetCore.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElTableColumn : ElementComponentBase
    {
        internal virtual bool IsCheckBox { get; set; }
        [Parameter]
        public virtual string Width { get; set; }

        internal virtual bool IsTree { get; set; }
        /// <summary>
        /// 排序编号
        /// </summary>
        [Parameter]
        public int SortNo { get; set; }
        /// <summary>
        /// 当前列关联的属性名
        /// </summary>
        [Parameter]
        public string Property { get; set; }

        [CascadingParameter]
        public ElTableColumns Columns { get; set; }

        /// <summary>
        /// 所属的分组表头（两层表头场景由 <see cref="ElTableColumnGroup"/> 级联下发）。
        /// 为空表示本列是顶层列，直接登记为 <see cref="ElTable"/> 的叶子列。
        /// </summary>
        [CascadingParameter]
        public ElTableColumn ParentColumn { get; set; }

        /// <summary>本列登记到的分组表头；null 表示顶层列。</summary>
        internal TableHeader GroupHeader { get; set; }

        /// <summary>分组表头收下子列：先确保分组本身已登记，再把子列挂到分组下。</summary>
        internal void AddChildColumn(ElTableColumn child)
        {
            GroupHeader ??= Table.AddGroupColumn(this);
            Table.AddColumnToGroup(child, GroupHeader);
        }
        [CascadingParameter]
        public ElTable Table { get; set; }

        [Parameter]
        public virtual RenderFragment<object> ChildContent { get; set; }

        [Parameter]
        public string Text { get; set; }

        [Parameter]
        public string Label
        {
            get => Text;
            set => Text = value;
        }

        [Parameter]
        public bool Sortable { get; set; }

        [Parameter]
        public TableSortOrder SortOrder { get; set; }

        [Parameter]
        public TableColumnFixed Fixed { get; set; }

        [Parameter]
        public IList<TableFilterOption> Filters { get; set; }

        [Parameter]
        public Func<object, object, bool> FilterMethod { get; set; }

        /// <summary>EP 的 <c>filter-multiple</c>：true＝筛选面板是勾选框 +「筛选 / 重置」，false＝单选列表 +「清除」。默认与 EP 一致为 true。</summary>
        [Parameter]
        public bool FilterMultiple { get; set; } = true;

        /// <summary>本库扩展：这一列的筛选面板改成条件式（运算符 + 值，最多两条，可且可或），用 Element 组件渲染。默认关。</summary>
        [Parameter]
        public bool AdvancedFilter { get; set; }

        /// <summary>
        /// 是否可编辑
        /// </summary>
        [Parameter]
        public bool IsEditable { get; set; } = true;

        /// <summary>
        /// 编辑状态下回车是否允许停在该列。设为 false 时，回车会跳过该列直接进入下一个可编辑单元格。
        /// </summary>
        [Parameter]
        public bool EnterMovesToNextCell { get; set; } = true;

        /// <summary>
        /// 格式化
        /// </summary>
        [Parameter]
        public string Format { get; set; }
// ============ Element Plus 官方属性补齐 ============

        /// <summary>列类型：selection / index / expand（默认 null 为普通列）</summary>
        [Parameter] public string Type { get; set; }

        /// <summary>最小宽度（自适应容器分配）</summary>
        [Parameter] public string MinWidth { get; set; }

        /// <summary>对齐方式：left / center / right</summary>
        [Parameter] public string Align { get; set; } = "left";

        /// <summary>表头对齐方式，不填则用 Align</summary>
        [Parameter] public string HeaderAlign { get; set; }

        /// <summary>单元格内容过长是否隐藏并 hover 显示 tooltip</summary>
        [Parameter] public bool ShowOverflowTooltip { get; set; }

        /// <summary>自定义单元格 className</summary>
        [Parameter] public string ClassName { get; set; }

        /// <summary>自定义表头单元格 className</summary>
        [Parameter] public string LabelClassName { get; set; }

        /// <summary>格式化函数：(row, column, cellValue, index) => displayValue</summary>
        [Parameter] public Func<object, object, object, int, object> Formatter { get; set; }

        /// <summary>列 type=selection 时的选择判断函数</summary>
        [Parameter] public Func<object, bool> Selectable { get; set; }

        /// <summary>是否保留选中状态（type=selection），需 RowKey</summary>
        [Parameter] public bool ReserveSelection { get; set; }

        /// <summary>type=index 时的索引函数：(index) => displayValue</summary>
        [Parameter] public Func<int, object> IndexMethod { get; set; }

        /// <summary>是否必选列（隐藏）— Element Plus 内部用</summary>
        [Parameter] public bool Required { get; set; }

        /// <summary>自定义表头内容 RenderFragment</summary>
        [Parameter] public RenderFragment Header { get; set; }

        /// <summary>
        /// Editor control used by this column in edit mode, e.g.
        /// typeof(ElSelect&lt;string&gt;) / typeof(ElDateTimePicker) / typeof(ElDropDownTableGrow).
        /// When null the control is inferred from the property type.
        /// </summary>
        [Parameter] public Type Editor { get; set; }

        /// <summary>Data source for dropdown editors (ElSelect / ElDropDownTableGrow).</summary>
        [Parameter] public IEnumerable EditorDataSource { get; set; }

        /// <summary>Value field name of EditorDataSource.</summary>
        [Parameter] public string EditorValueField { get; set; }

        /// <summary>Display field name of EditorDataSource; defaults to the value field.</summary>
        [Parameter] public string EditorTextField { get; set; }

        /// <summary>Column definitions forwarded to ElDropDownTableGrow as its ChildContent.</summary>
        [Parameter] public RenderFragment EditorColumns { get; set; }

        /// <summary>
        /// Forward <c>CloseOnDatePick</c> to the ElDateTimePicker editor: 点日期即提交并收起面板（本库扩展，EP 无此属性）。
        /// 默认 false ＝ EP 行为（点日期即写值但面板留着选时间）。只作用于 ElDateTimePicker 编辑器。
        /// </summary>
        [Parameter] public bool EditorCloseOnDatePick { get; set; }

        /// <summary>
        /// 本库扩展（消费方批次110uu）：这一列是否可通过拖拽表头右缘改变宽度。
        /// EP 的 <c>resizable</c> 同名参数；null＝跟随表级 <see cref="ElTable.Resizable"/>，
        /// true/false＝覆盖表级。手柄只在登记时归并出的 <see cref="TableHeader.Resizable"/> 上渲染。
        /// </summary>
        [Parameter] public bool? Resizable { get; set; }

        /// <summary>
        /// 表头点击事件。设置后，点击该列的标题头（&lt;th&gt;）会触发此回调，参数为该列对应的 <see cref="TableHeader"/>。
        /// 用于「某列标题头可点击」的场景（例如点击列头弹出筛选面板 / 列说明 / 批量操作菜单）。
        /// 仅设置了此属性的列才会响应；未设置的列点击表头不触发任何回调。
        /// </summary>
        [Parameter] public EventCallback<TableHeader> OnHeaderClick { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            if (ParentColumn != null)
            {
                ParentColumn.AddChildColumn(this);
                return;
            }
            if (Columns != null)
            {
                Columns.AddColumn(this);
                return;
            }
            if (Table == null)
            {
                throw new ElementException($"列 {GetType().Name} 必须放在 ElTable/ElTable 或 ElTableColumns/ElTableColumns 内");
            }
            RegisterSelf();
        }

        /// <summary>
        /// 把本列登记进表格。分组表头（<see cref="ElTableColumnGroup"/>）重写这里，只登记分组本身、不登记叶子列。
        /// </summary>
        protected internal virtual void RegisterSelf()
        {
            Table.AddColumn(this);
        }
    }
}
