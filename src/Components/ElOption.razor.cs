


using ElementPlus.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElOption<TValue> : IDisposable
    {
        private SelectResultModel<TValue> currentResultModel;

        [CascadingParameter]
        public DropDownOption Option { get; set; }

        [CascadingParameter]
        public ElOptionGroup<TValue> OptionGroup { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }
        [Parameter]
        public TValue Value { get; set; }

        [Parameter]
        public string Text { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool IsDisabled
        {
            get => Disabled;
            set => Disabled = value;
        }

        protected override void OnInitialized()
        {
            currentResultModel = new SelectResultModel<TValue>()
            {
                Key = Value,
                Text = Text ?? Convert.ToString(Value),
                Disabled = EffectiveDisabled
            };
            var select = (ElSelect<TValue>)Option.Select;
            select.RegisterOption(currentResultModel);
            // 「是否高亮」不在本组件的参数里（读的是 ElSelect 持有的 hoveredIndex），
            // 参数不变时 Blazor 会跳过本组件重渲染（ShouldRender 都不会被调用），
            // 键盘上下键改出的高亮就刷不到 DOM。订阅高亮变化自行重渲染。
            select.HoverChanged += OnSelectHoverChanged;
            // 搜索关键字同理：关键字变化时本组件入参不变，不订阅就不会重算 IsVisible（display:none），
            // 表现为「输入关键字选项不收缩」。订阅过滤变化自行重渲染。
            select.FilterChanged += OnSelectFilterChanged;
        }

        private void OnSelectHoverChanged()
        {
            _ = InvokeAsync(StateHasChanged);
        }

        private void OnSelectFilterChanged()
        {
            _ = InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            if (Option?.Select is ElSelect<TValue> select)
            {
                select.HoverChanged -= OnSelectHoverChanged;
                select.FilterChanged -= OnSelectFilterChanged;
            }
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            if (currentResultModel != null)
            {
                currentResultModel.Key = Value;
                currentResultModel.Text = Text ?? Convert.ToString(Value);
                currentResultModel.Disabled = EffectiveDisabled;
            }
        }

        public async Task SelectItemAsync(MouseEventArgs e)
        {
            if (EffectiveDisabled)
            {
                return;
            }
            await ((ElSelect<TValue>)Option.Select).OnInternalSelectAsync(currentResultModel);
        }
        protected override bool ShouldRender()
        {
            return true;
        }

        private bool IsSelected => Option?.Select is ElSelect<TValue> select && select.IsOptionSelected(currentResultModel);

        private bool IsHover => Option?.Select is ElSelect<TValue> select && select.IsOptionHover(currentResultModel);

        private bool IsVisible => Option?.Select is not ElSelect<TValue> select || select.IsOptionVisible(currentResultModel);

        private bool EffectiveDisabled => Disabled || (OptionGroup?.EffectiveDisabled ?? false);

        private string AriaSelected => IsSelected ? "true" : "false";

        private string AriaDisabled => EffectiveDisabled ? "true" : "false";
    }
}
