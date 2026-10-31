using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElSegmented<TValue> : ElementFieldComponentBase<TValue>
    {
        [Parameter]
        public TValue Value { get; set; }

        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }

        [Parameter]
        public EventCallback<TValue> OnChange { get; set; }

        [Parameter]
        public IEnumerable<SegmentedOption> Options { get; set; }

        [Parameter]
        public IEnumerable<TValue> Items { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool Block { get; set; }

        [Parameter]
        public ElementSize Size { get; set; } = ElementSize.Default;

        /// <summary>展示方向：horizontal（默认）/ vertical（2.8.7 起官方对齐）</summary>
        [Parameter]
        public SegmentedDirection Direction { get; set; } = SegmentedDirection.Horizontal;

        /// <summary>按钮主题：default（默认）/ primary（选中项主题色）</summary>
        [Parameter]
        public string ButtonType { get; set; } = "default";

        /// <summary>自定义每个选项的内容模板（等价于官方 default 插槽，作用域为当前 SegmentedOption）</summary>
        [Parameter]
        public RenderFragment<SegmentedOption> ItemTemplate { get; set; }

        /// <summary>是否触发表单验证（默认 true，官方对齐）</summary>
        [Parameter]
        public bool ValidateEvent { get; set; } = true;

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        protected IReadOnlyList<SegmentedOption> ResolvedOptions => Options?.ToList()
            ?? Items?.Select(x => new SegmentedOption
            {
                Label = Convert.ToString(x, CultureInfo.CurrentCulture),
                Value = Convert.ToString(x, CultureInfo.InvariantCulture)
            }).ToList()
            ?? new List<SegmentedOption>();

        protected string SegmentedClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-segmented", Cls)
            .AddIf(Block, "is-block")
            .AddIf(Disabled, "is-disabled")
            .AddIf(Direction == SegmentedDirection.Vertical, "is-vertical")
            .AddIf(Size == ElementSize.Large, "el-segmented--large")
            .AddIf(Size == ElementSize.Small, "el-segmented--small")
            .AddIf(ButtonType == "primary", "el-segmented--primary")
            .ToString();

        protected string GetItemClass(SegmentedOption option, bool selected)
        {
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-segmented__item")
                .AddIf(selected, "is-selected")
                .AddIf(Disabled || option.Disabled, "is-disabled")
                .ToString();
        }

        protected bool IsSelected(SegmentedOption option)
        {
            return EqualityComparer<TValue>.Default.Equals(Value, ConvertValue(option.Value));
        }

        protected async Task SelectAsync(SegmentedOption option)
        {
            if (Disabled || option.Disabled)
            {
                return;
            }

            var next = ConvertValue(option.Value);
            if (EqualityComparer<TValue>.Default.Equals(Value, next))
            {
                return;
            }

            Value = next;
            SetFieldValue(next, ValidateEvent);
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(next);
            }

            if (OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(next);
            }
        }

        private static TValue ConvertValue(string value)
        {
            if (typeof(TValue) == typeof(string))
            {
                return (TValue)(object)value;
            }

            if (typeof(TValue).IsEnum)
            {
                return (TValue)Enum.Parse(typeof(TValue), value);
            }

            var targetType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
            return (TValue)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
    }

    /// <summary>ElSegmented 展示方向（官方 2.8.7 direction）</summary>
    public enum SegmentedDirection
    {
        Horizontal,
        Vertical
    }
}
