using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Collections;

namespace ElementPlus.ControlRenders
{
    /// <summary>
    /// In-cell editor that renders a real <see cref="ElDropDownTableGrow"/> (a dropdown grid).
    /// The column declares the grid columns through RenderConfig.EditorColumns, which is forwarded
    /// straight to the dropdown's ChildContent.
    /// </summary>
    internal class DropDownTableRender : RenderBase, IDropDownTableRender
    {
        public void Render(RenderTreeBuilder renderTreeBuilder, RenderConfig config)
        {
            renderTreeBuilder.OpenComponent(0, typeof(ElDropDownTableGrow));

            if (config.DataSource is IEnumerable items)
            {
                renderTreeBuilder.AddAttribute(1, nameof(ElDropDownTableGrow.Items), items);
            }
            if (!string.IsNullOrWhiteSpace(config.ValueField))
            {
                renderTreeBuilder.AddAttribute(2, nameof(ElDropDownTableGrow.ValueField), config.ValueField);
            }
            if (!string.IsNullOrWhiteSpace(config.TextField))
            {
                renderTreeBuilder.AddAttribute(3, nameof(ElDropDownTableGrow.TextField), config.TextField);
            }
            if (config.EditorColumns != null)
            {
                renderTreeBuilder.AddAttribute(4, nameof(ElDropDownTableGrow.ChildContent), config.EditorColumns);
            }

            renderTreeBuilder.AddAttribute(5, nameof(ElDropDownTableGrow.Value), config.EditingValue ?? config.RawValue);

            if (config.Page != null)
            {
                renderTreeBuilder.AddAttribute(6, nameof(ElDropDownTableGrow.ValueChanged),
                    EventCallback.Factory.Create<object>(config.Page, v => config.EditingValue = v));
            }

            renderTreeBuilder.AddComponentReferenceCapture(7, e => config.InputControl = e);
            renderTreeBuilder.CloseComponent();
        }
    }
}
