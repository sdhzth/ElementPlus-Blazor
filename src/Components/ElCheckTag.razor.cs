using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// ElCheckTag — 可选中的标签（Element Plus CheckTag）
    /// </summary>
    public partial class ElCheckTag : ElementComponentBase
    {
        private bool _checked;

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// 是否选中（v-model:checked）
        /// </summary>
        [Parameter]
        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                }
            }
        }

        /// <summary>
        /// 选中状态变化回调
        /// </summary>
        [Parameter]
        public EventCallback<bool> CheckedChanged { get; set; }

        /// <summary>
        /// 类型（primary / success / info / warning / danger）
        /// </summary>
        [Parameter]
        public TagType Type { get; set; } = TagType.Primary;

        /// <summary>
        /// 是否禁用
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// 点击事件
        /// </summary>
        [Parameter]
        public EventCallback OnClick { get; set; }

        protected string CheckTagClass
        {
            get
            {
                var cb = HtmlPropertyBuilder.CreateCssClassBuilder();
                cb.Add("el-check-tag");
                cb.AddIf(_checked, "is-checked");
                cb.AddIf(Disabled, "is-disabled");
                cb.Add($"el-check-tag--{Type.ToString().ToLower()}");
                cb.Add(Cls);
                return cb.ToString();
            }
        }

        protected string CheckTagStyle
        {
            get
            {
                var sb = HtmlPropertyBuilder.CreateCssStyleBuilder();
                sb.AddIf(!string.IsNullOrEmpty(Style), Style.Split(';'));
                return sb.ToString();
            }
        }

        private async Task OnClickAsync()
        {
            if (OnClick.HasDelegate)
                await OnClick.InvokeAsync(null);

            if (Disabled) return;

            _checked = !_checked;
            StateHasChanged();

            if (CheckedChanged.HasDelegate)
                await CheckedChanged.InvokeAsync(_checked);
        }
    }
}