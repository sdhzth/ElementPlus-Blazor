using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElDropdown : ElementComponentBase
    {
        internal ElementReference Target;
        private DropDownOption dropDownOption;
        private int hoverCloseVersion;

        [Parameter]
        public RenderFragment Trigger { get; set; }

        [Parameter]
        public RenderFragment Items { get; set; }

        [Parameter]
        public DropdownTrigger TriggerMode { get; set; } = DropdownTrigger.Click;

        [Parameter]
        public string TriggerType
        {
            get => TriggerMode.ToString().ToLowerInvariant();
            set => TriggerMode = ParseTrigger(value);
        }

        [Parameter]
        public bool SplitButton { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public ButtonType Type { get; set; } = ButtonType.Default;

        [Parameter]
        public ButtonSize Size { get; set; } = ButtonSize.Default;

        [Parameter]
        public string Icon { get; set; } = "el-icon-arrow-down";

        [Parameter]
        public RenderFragment ButtonContent { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        [Parameter]
        public EventCallback<DropdownCommandEventArgs> OnCommand { get; set; }

        /// <summary>点击菜单项后是否关闭下拉（默认 true）</summary>
        [Parameter]
        public bool HideOnClick { get; set; } = true;

        /// <summary>弹出层自定义 class</summary>
        [Parameter]
        public string PopperClass { get; set; }

        /// <summary>弹出层自定义 style</summary>
        [Parameter]
        public string PopperStyle { get; set; }

        /// <summary>
        /// 弹出层出现位置（官方 placement）。
        /// 取值形如 bottom-start / top / top-end / right-start …，默认 bottom-start。
        /// </summary>
        [Parameter]
        public string Placement { get; set; }

        /// <summary>菜单最大高度（px，官方 max-height）。大于 0 时菜单内部滚动</summary>
        [Parameter]
        public int MaxHeight { get; set; }

        /// <summary>触发器 tabindex（官方 tabindex，默认 0）</summary>
        [Parameter]
        public int Tabindex { get; set; }

        /// <summary>菜单容器的 role（官方 role，默认 menu）</summary>
        [Parameter]
        public string Role { get; set; } = "menu";

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        /// <summary>展开延迟 (ms)；官方默认 250</summary>
        [Parameter]
        public int ShowTimeout { get; set; } = 250;

        /// <summary>关闭延迟 (ms)</summary>
        [Parameter]
        public int HideTimeout { get; set; } = 150;

        /// <summary>
        /// 弹层是否 teleport 到 body。
        /// 已知差异：本库弹层统一由 ElPopup 宿主渲染，该值暂不参与定位计算，保留参数以兼容官方写法。
        /// </summary>
        [Parameter]
        public bool Teleported { get; set; } = true;

        /// <summary>官方 visible-change：下拉显示/隐藏时触发（true=显示，false=隐藏）</summary>
        [Parameter]
        public EventCallback<bool> OnVisibleChange { get; set; }

        /// <summary>官方 open 事件：下拉展开后触发</summary>
        [Parameter]
        public EventCallback OnOpen { get; set; }

        /// <summary>官方 close 事件：下拉收起后触发</summary>
        [Parameter]
        public EventCallback OnClose { get; set; }

        [Inject]
        private PopupService PopupService { get; set; }

        protected string DropdownClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-dropdown", Cls)
            .AddIf(Disabled, "is-disabled")
            .ToString();

        internal bool IsDropDownOpen => dropDownOption != null && PopupService.DropDownMenuOptionsCore.Any(x => x == dropDownOption);

        internal async Task NotifyCommandAsync(ElDropdownItem item)
        {
            if (!OnCommand.HasDelegate)
            {
                return;
            }

            await OnCommand.InvokeAsync(new DropdownCommandEventArgs
            {
                Command = item.Command,
                Item = item
            });
        }

        protected async Task OnTriggerClickAsync(MouseEventArgs e)
        {
            if (SplitButton || TriggerMode != DropdownTrigger.Click)
            {
                return;
            }

            await ToggleDropDownAsync();
        }

        protected async Task OnTriggerMouseOverAsync(MouseEventArgs e)
        {
            if (TriggerMode != DropdownTrigger.Hover)
            {
                return;
            }

            hoverCloseVersion++;
            await ShowDropDownAsync();
        }

        protected async Task OnTriggerMouseOutAsync(MouseEventArgs e)
        {
            if (TriggerMode != DropdownTrigger.Hover || dropDownOption?.Instance == null)
            {
                return;
            }

            await CloseAfterHoverDelayAsync();
        }

        internal void KeepOpen()
        {
            hoverCloseVersion++;
        }

        internal Task CloseAfterHoverDelayAsync()
        {
            if (TriggerMode != DropdownTrigger.Hover)
            {
                return Task.CompletedTask;
            }

            return CloseAfterHoverDelayCoreAsync(++hoverCloseVersion);
        }

        private async Task CloseAfterHoverDelayCoreAsync(int version)
        {
            var option = dropDownOption;
            await Task.Delay(HideTimeout);
            if (version != hoverCloseVersion || option?.Instance == null)
            {
                return;
            }

            await InvokeAsync(() => option.Instance.CloseDropDownAsync(option));
        }

        protected async Task OnTriggerContextMenuAsync(MouseEventArgs e)
        {
            if (TriggerMode != DropdownTrigger.ContextMenu)
            {
                return;
            }

            await ShowDropDownAsync();
        }

        protected async Task OnSplitButtonClickAsync(MouseEventArgs e)
        {
            if (Disabled)
            {
                return;
            }

            if (OnClick.HasDelegate)
            {
                await OnClick.InvokeAsync(e);
            }
        }

        protected async Task OnCaretButtonClickAsync(MouseEventArgs e)
        {
            await ToggleDropDownAsync();
        }

        protected async Task OnKeyDownAsync(KeyboardEventArgs e)
        {
            if (e.Key != "Enter" && e.Key != " " && e.Key != "ArrowDown")
            {
                return;
            }

            await ToggleDropDownAsync();
        }

        internal async Task ToggleDropDownAsync()
        {
            if (IsDropDownOpen && dropDownOption?.Instance != null)
            {
                await dropDownOption.Instance.CloseDropDownAsync(dropDownOption);
                return;
            }

            await ShowDropDownAsync();
        }

        /// <summary>
        /// 以编程方式展开下拉菜单（等价于用户点击触发器）。
        /// 组件 Disabled 或菜单已展开时为空操作。
        /// </summary>
        public async Task ShowDropDownAsync()
        {
            if (Disabled)
            {
                return;
            }

            if (PopupService.DropDownMenuOptionsCore.Any(x => x.Target.Id == Target.Id))
            {
                return;
            }

            dropDownOption = new DropDownOption()
            {
                Target = Target,
                OptionContent = Items,
                Select = this,
                Refresh = () => StateHasChanged(),
                OnClosed = async () =>
                {
                    StateHasChanged();
                    if (OnVisibleChange.HasDelegate)
                    {
                        await OnVisibleChange.InvokeAsync(false);
                    }
                    if (OnClose.HasDelegate)
                    {
                        await OnClose.InvokeAsync();
                    }
                },
                PopperClass = PopperClass,
                PopperStyle = PopperStyle,
                MaxHeight = MaxHeight,
                Placement = string.IsNullOrWhiteSpace(Placement) ? "bottom-start" : Placement,
                IsShow = true,
                // hover 触发时不铺「点击外部关闭」的全屏遮罩：它会盖住触发器并触发 mouseout，
                // 导致菜单刚弹出就被 HideTimeout 关掉。hover 的关闭语义本就由触发器/面板的
                // mouseout + HideTimeout 承担，无需遮罩。
                ShowShadow = TriggerMode != DropdownTrigger.Hover
            };
            PopupService.DropDownMenuOptionsCore.Add(dropDownOption);

            if (OnVisibleChange.HasDelegate)
            {
                await OnVisibleChange.InvokeAsync(true);
            }
            if (OnOpen.HasDelegate)
            {
                await OnOpen.InvokeAsync();
            }
        }

        internal void ShowDropDown()
        {
            _ = ShowDropDownAsync();
        }

        private static DropdownTrigger ParseTrigger(string value)
        {
            if (string.Equals(value, "click", StringComparison.OrdinalIgnoreCase))
            {
                return DropdownTrigger.Click;
            }

            if (string.Equals(value, "contextmenu", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "context-menu", StringComparison.OrdinalIgnoreCase))
            {
                return DropdownTrigger.ContextMenu;
            }

            return DropdownTrigger.Hover;
        }
    }
}
