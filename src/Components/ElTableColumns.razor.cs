using ElementPlus.DisplayRenders;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElTableColumns
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }
        [CascadingParameter]
        public ElTable Table { get; set; }

        public void AddColumn(ElTableColumn column)
        {
            // 登记入口交给列本身：分组表头（ElTableColumnGroup）要登记成分组而不是叶子列。
            // 容器与列在同一表格下时两者引用同一个 ElTable；容器在表格外时补上容器的表格引用。
            column.Table ??= Table;
            column.RegisterSelf();
        }
    }
}
