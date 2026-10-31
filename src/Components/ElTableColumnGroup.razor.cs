using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    /// <summary>
    /// 两层表头的分组列：自身只渲染表头第一行的一个 <c>th[colspan]</c>，不进 colgroup、不出表体单元格；
    /// 叶子列写在 <see cref="ChildColumns"/> 里，通过级联登记到本分组下，宽度与取数仍按叶子列算。
    /// 用法对齐 EP 的「列嵌套 columns」，写法对齐 Radzen 的嵌套 <c>Columns</c> 片段。
    /// </summary>
    public partial class ElTableColumnGroup : ElTableColumn
    {
        /// <summary>
        /// 分组下的叶子列。命名用 <c>ChildColumns</c> 而非 <c>Columns</c>，
        /// 因为基类的 <see cref="ElTableColumn.Columns"/> 已被 <c>ElTableColumns</c> 容器级联参数占用。
        /// </summary>
        [Parameter]
        public RenderFragment ChildColumns { get; set; }

        /// <summary>登记为分组表头（而不是叶子列）。</summary>
        protected internal override void RegisterSelf()
        {
            GroupHeader = Table.AddGroupColumn(this);
        }
    }
}
