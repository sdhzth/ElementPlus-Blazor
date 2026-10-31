using System;

namespace ElementPlus
{
    /// <summary>
    /// ElTable 列宽拖拽落定的回调参数（本库扩展，消费方批次110uu）。
    /// 对应 EP 的 <c>header-dragend(newWidth, oldWidth, column, event)</c>：拖拽全程不进 Blazor
    /// 渲染管线（dom.js 直接改两张表的 &lt;col&gt;），松手才回调 <see cref="ElTable.JsTableColumnResizeEnd"/>,
    /// 覆盖宽已记进 <see cref="TableHeader.ResizeWidthOverride"/> 并重渲染，此事件只做通知
    /// （宿主要持久化列宽时在这里拿数据）。
    /// </summary>
    public class TableColumnWidthChangeEventArgs : EventArgs
    {
        /// <summary>被拖拽的列。</summary>
        public TableHeader Column { get; set; }

        /// <summary>列在渲染序里的下标（与 &lt;col name="el-table_1_column_N"&gt; 的 N 一致；选择列是 0、不参与拖拽）。</summary>
        public int ColIndex { get; set; }

        /// <summary>落定后的列宽（px）。</summary>
        public int Width { get; set; }

        /// <summary>拖拽前的列宽（px）。</summary>
        public int OldWidth { get; set; }
    }
}
