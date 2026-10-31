using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElDropdownItem : ElementComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [CascadingParameter]
        public DropDownOption Option { get; set; }

        [Parameter]
        public EventCallback<DropDownOption> OnClick { get; set; }

        [Parameter]
        public object Command { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool Divided { get; set; }

        [Parameter]
        public string Icon { get; set; }

        protected string ItemClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-dropdown-menu__item", Cls)
            .AddIf(Disabled, "is-disabled")
            .ToString();

        internal async Task InternalOnClickAsync()
        {
            if (Disabled)
            {
                return;
            }

            var owner = Option?.Select as ElDropdown;

            if (Option?.Instance != null && (owner == null || owner.HideOnClick))
            {
                await Option.Instance.CloseDropDownAsync(Option);
            }

            if (owner != null)
            {
                await owner.NotifyCommandAsync(this);
            }

            if (OnClick.HasDelegate)
            {
                await OnClick.InvokeAsync(Option);
            }
        }

        protected async Task OnKeyDownAsync(KeyboardEventArgs e)
        {
            if (e.Key != "Enter" && e.Key != " ")
            {
                return;
            }

            await InternalOnClickAsync();
        }

        private static string NormalizeIcon(string icon)
        {
            if (string.IsNullOrWhiteSpace(icon))
            {
                return null;
            }

            return icon.StartsWith("el-icon-") ? icon : $"el-icon-{icon}";
        }
    }
}
