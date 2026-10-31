using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;

namespace ElementPlus.ControlRenders
{
    /// <summary>
    /// In-cell editor that renders a real <see cref="ElDateTimePicker"/> (date + time spinner).
    /// The picked value round-trips through <see cref="RenderConfig.EditingValue"/>, which is what
    /// ElTable reads back when saving the row.
    /// </summary>
    internal class DateTimePickerRender : RenderBase, IDateTimePickerRender
    {
        public void Render(RenderTreeBuilder renderTreeBuilder, RenderConfig config)
        {
            renderTreeBuilder.OpenComponent(0, typeof(ElDateTimePicker));

            var value = config.EditingValue ?? config.RawValue;
            DateTime? dateTime = null;
            if (value != null)
            {
                if (value is DateTime dt)
                {
                    dateTime = dt;
                }
                else
                {
                    DateTime parsed;
                    if (DateTime.TryParse(Convert.ToString(value), out parsed))
                    {
                        dateTime = parsed;
                    }
                }
            }
            renderTreeBuilder.AddAttribute(1, nameof(ElDateTimePicker.Value), dateTime);

            // 接收者决定"这次选值之后重渲染谁"：优先表格本身（RenderBase.CreateTwoWayBinding 的同一条口径），
            // 只有脱离表格的宿主才退回页面。点日期现在即写值，绑定在页面上会连带整页重渲染。
            var receiver = config.Owner ?? config.Page;
            if (receiver != null)
            {
                renderTreeBuilder.AddAttribute(2, nameof(ElDateTimePicker.ValueChanged),
                    EventCallback.Factory.Create<DateTime?>(receiver, v => config.EditingValue = v));
            }

            if (!string.IsNullOrWhiteSpace(config.Placeholder))
            {
                renderTreeBuilder.AddAttribute(3, nameof(ElDateTimePicker.Placeholder), config.Placeholder);
            }

            // 单元格编辑器里没有"再点一下确定"以外的确认动作，列上开了 EditorCloseOnDatePick 就把参数透下去。
            renderTreeBuilder.AddAttribute(4, nameof(ElDateTimePicker.CloseOnDatePick), config.CloseOnDatePick);

            renderTreeBuilder.AddComponentReferenceCapture(5, e => config.InputControl = e);
            renderTreeBuilder.CloseComponent();
        }
    }
}
