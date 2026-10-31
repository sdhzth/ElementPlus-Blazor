using System;
using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace ElementPlus
{
    /// <summary>
    /// ElTableV2 列定义
    /// </summary>
    public class TableV2Column
    {
        /// <summary>唯一 key（用于 RowKey 回显）</summary>
        public string Key { get; set; }

        /// <summary>列标题</summary>
        public string Title { get; set; }

        /// <summary>属性名</summary>
        public string Property { get; set; }

        /// <summary>自定义数据键（与 Property 类似，兼容官方命名）</summary>
        public string DataKey { get; set; }

        /// <summary>列宽</summary>
        public string Width { get; set; }

        /// <summary>最小列宽（对齐 EP minWidth）</summary>
        public string MinWidth { get; set; }

        /// <summary>最大列宽（对齐 EP maxWidth）</summary>
        public string MaxWidth { get; set; }

        /// <summary>flex 增长系数（对齐 EP flexGrow；设置后列宽参与弹性分配）</summary>
        public double? FlexGrow { get; set; }

        /// <summary>flex 收缩系数（对齐 EP flexShrink）</summary>
        public double? FlexShrink { get; set; }

        /// <summary>是否可排序（对齐 EP sortable；点击表头触发 ColumnSort 事件，由调用方排序数据）</summary>
        public bool Sortable { get; set; }

        /// <summary>文本对齐方式（left/center/right）</summary>
        public string Align { get; set; } = "left";

        /// <summary>固定列（left / right / null）</summary>
        public string Fixed { get; set; }

        /// <summary>整列 CSS class</summary>
        public string ClassName { get; set; }

        /// <summary>单元格自定义 class（Func 形式，按行数据返回）</summary>
        public Func<object, string> CellClassName { get; set; }

        /// <summary>表头单元格自定义 class</summary>
        public string HeaderCellClassName { get; set; }

        /// <summary>
        /// 这一列的被裁单元格悬停浮出全文（本库扩展；EP 的 table-v2 只有原生 <c>title</c>，
        /// 见 <c>components/cell.tsx:14</c>）。<c>null</c> 时跟随 <see cref="ElTableV2.EllipsisTooltip"/>。
        /// </summary>
        public bool? EllipsisTooltip { get; set; }

        /// <summary>单元格模板</summary>
        public RenderFragment<object> Template { get; set; }

        /// <summary>表头单元格模板</summary>
        public RenderFragment HeaderTemplate { get; set; }

        public string ResolveProperty => Property ?? DataKey;

        public object GetValue(object row)
        {
            if (row == null || string.IsNullOrWhiteSpace(ResolveProperty))
            {
                return null;
            }

            return row.GetType().GetProperty(ResolveProperty, BindingFlags.Instance | BindingFlags.Public)?.GetValue(row);
        }
    }
}