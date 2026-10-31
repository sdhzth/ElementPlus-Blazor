using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// <see cref="ElSplitButton"/> 的菜单项。由 ElPopup 宿主在菜单展开时挂载，
    /// 挂载即向父组件登记；点击后由父组件统一收起菜单并回传本项。
    /// </summary>
    public partial class ElSplitButtonItem : ElementComponentBase
    {
        private ElSplitButton registeredOwner;

        internal ElementReference Element;

        [Parameter]
        public string Text { get; set; } = string.Empty;

        /// <summary>Element 图标类名；可省略 el-icon- 前缀</summary>
        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public string IconColor { get; set; }

        /// <summary>业务值（对齐 RadzenSplitButtonItem.Value），Click 回传项后可读取。</summary>
        [Parameter]
        public string Value { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public bool Visible { get; set; } = true;

        [CascadingParameter]
        public DropDownOption Option { get; set; }

        internal ElSplitButton Owner => Option?.Select as ElSplitButton;

        protected string ItemClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-dropdown-menu__item", Cls)
            .AddIf(Disabled, "is-disabled")
            .ToString();

        private string IconStyle => string.IsNullOrWhiteSpace(IconColor) ? null : $"color:{IconColor}";

        protected override void OnInitialized()
        {
            base.OnInitialized();
            SyncRegistration();
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            SyncRegistration();
        }

        private void SyncRegistration()
        {
            var owner = Owner;
            if (ReferenceEquals(registeredOwner, owner))
            {
                return;
            }

            registeredOwner?.RemoveItem(this);
            registeredOwner = owner;
            if (owner != null && Visible)
            {
                owner.AddItem(this);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (Visible && Owner?.TryConsumeFocus(this) == true)
            {
                await FocusAsync();
            }
        }

        internal async Task FocusAsync()
        {
            try
            {
                if (!Element.Equals(default(ElementReference)))
                {
                    await Element.FocusAsync();
                }
            }
            catch (JSException)
            {
                // 预渲染 / 无 JS 环境：焦点落不下去，键盘交互退化为点击
            }
            catch (InvalidOperationException)
            {
                // 元素已随菜单卸载
            }
        }

        protected async Task OnClickAsync(MouseEventArgs e)
        {
            if (Disabled)
            {
                return;
            }

            var owner = Owner;
            if (owner != null)
            {
                await owner.ActivateItemAsync(this, focusCaret: false);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                registeredOwner?.RemoveItem(this);
                registeredOwner = null;
            }

            base.Dispose(disposing);
        }

        private static string NormalizeIcon(string icon)
        {
            if (string.IsNullOrWhiteSpace(icon))
            {
                return null;
            }

            return icon.StartsWith("el-icon-", StringComparison.Ordinal) ? icon : $"el-icon-{icon}";
        }
    }
}
