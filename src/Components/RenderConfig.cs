using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ElementPlus
{
    public class RenderConfig
    {
        public bool IsRequired { get; set; }
        public string RequiredMessage { get; set; }
        public Type InputControlType { get; set; }
        public object ControlAttribute { get; set; }
        public string Placeholder { get; set; }
        public PropertyInfo Property { get; set; }
        public object InputControl { get; set; }
        public object DataSource { get; set; }
        public Type DataSourceLoader { get; set; }

        private object editValue = null;
        /// <summary>
        /// 单元格编辑器回写的当前值。ElTable 在保存时读它。
        /// 编辑器（ElSelect / ElDateTimePicker 等）选中值后只写这里，不会自己重绘，
        /// 所以宿主组件要挂 ValueChanged 通知来触发一次 StateHasChanged，界面才会跟着变。
        /// </summary>
        public object EditingValue
        {
            get => editValue;
            set
            {
                var changed = !Equals(editValue, value);
                editValue = value;
                if (changed)
                {
                    ValueChanged?.Invoke();
                }
            }
        }
        /// <summary>EditingValue 真正变化时触发（由宿主组件挂 StateHasChanged）。</summary>
        public Action ValueChanged { get; set; }
        public object Page { get; internal set; }

        /// <summary>
        /// 编辑器的<b>所有者组件</b>（表格里就是 ElTable 自身）：双向绑定的 EventCallback 优先用它当接收者。
        /// <para>
        /// 为什么必要：Blazor 在事件回调执行完后会标记<b>接收者组件</b>重渲染，而 <see cref="Page"/> 是整个页面，
        /// 于是每敲一个字都要重渲染整页 —— 实测含约 10 个表格的 /table 演示页 142ms/字，表格外同样输入 7ms/字。
        /// 留空时回落到 <see cref="Page"/>，行为与改动前一致。
        /// </para>
        /// </summary>
        public object Owner { get; set; }
        public object RawValue { get; set; }
        public object RawLabel { get; set; }
        public bool RawInfoHasSet { get; set; }
        public Type ValueType { get; set; }
        /// <summary>下拉类编辑器的值字段名（ElSelect / ElDropDownTableGrow）。</summary>
        public string ValueField { get; set; }
        /// <summary>下拉类编辑器的显示字段名（ElSelect / ElDropDownTableGrow）。</summary>
        public string TextField { get; set; }
        /// <summary>ElDropDownTableGrow 的列定义，直接作为其 ChildContent 下发。</summary>
        public RenderFragment EditorColumns { get; set; }
        /// <summary>
        /// ElDateTimePicker 编辑器：点日期即提交并收起面板（下发给 <c>ElDateTimePicker.CloseOnDatePick</c>，本库扩展）。
        /// 默认 false＝EP 行为：点日期写值但面板留着选时间。
        /// </summary>
        public bool CloseOnDatePick { get; set; }
    }
}
