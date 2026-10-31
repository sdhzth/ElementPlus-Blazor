using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElPopconfirm : ElementComponentBase
    {
        private bool internalVisible;

        #region 内容

        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public RenderFragment TitleContent { get; set; }

        /// <summary>正文内容（官方 content）</summary>
        [Parameter]
        public string Content { get; set; }

        /// <summary>正文内容 RenderFragment</summary>
        [Parameter]
        public RenderFragment ContentTemplate { get; set; }

        [Parameter]
        public RenderFragment Reference { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>自定义操作按钮区插槽（EP actions，2.8+）。提供后替换默认确认/取消按钮。</summary>
        [Parameter]
        public RenderFragment ActionsContent { get; set; }

        #endregion

        #region 定位

        [Parameter]
        public string Placement { get; set; } = "top";

        [Parameter]
        public PopperTrigger Trigger { get; set; } = PopperTrigger.Click;

        [Parameter]
        public int Offset { get; set; } = 8;

        #endregion

        #region 状态

        [Parameter]
        public bool ModelValue { get; set; }

        [Parameter]
        public EventCallback<bool> ModelValueChanged { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        #endregion

        #region 外观

        [Parameter]
        public int Width { get; set; } = 150;

        /// <summary>类型（控制默认图标与图标颜色，EP 无此参数、为本库扩展）</summary>
        [Parameter]
        public PopconfirmType Type { get; set; } = PopconfirmType.Warning;

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public string IconColor { get; set; }

        /// <summary>隐藏图标</summary>
        [Parameter]
        public bool HideIcon { get; set; }

        /// <summary>显示关闭按钮</summary>
        [Parameter]
        public bool ShowClose { get; set; }

        [Parameter]
        public bool ShowArrow { get; set; } = true;

        [Parameter]
        public string PopperClass { get; set; }

        [Parameter]
        public string PopperStyle { get; set; }

        #endregion

        #region 按钮

        /// <summary>
        /// 确认按钮文字。留空时取词条 <c>el.popconfirm.confirmButtonText</c>，再回落「确定」。
        /// （EP 的 en-US 值是 Yes / No，不是 OK / Cancel —— 走词条才会拿到 EP 措辞。）
        /// </summary>
        [Parameter]
        public string ConfirmButtonText { get; set; }

        /// <summary>取消按钮文字。留空时取词条 <c>el.popconfirm.cancelButtonText</c>，再回落「取消」。</summary>
        [Parameter]
        public string CancelButtonText { get; set; }

        private string EffectiveConfirmButtonText => Lang.Text("el.popconfirm.confirmButtonText", ConfirmButtonText, "确定");

        private string EffectiveCancelButtonText => Lang.Text("el.popconfirm.cancelButtonText", CancelButtonText, "取消");

        [Parameter]
        public ButtonType ConfirmButtonType { get; set; } = ButtonType.Primary;

        [Parameter]
        public ButtonType CancelButtonType { get; set; } = ButtonType.Default;

        /// <summary>点击 confirm/cancel 后是否保持打开（默认 false）</summary>
        [Parameter]
        public bool Persistent { get; set; }

        #endregion

        #region 事件

        [Parameter]
        public EventCallback OnConfirm { get; set; }

        [Parameter]
        public EventCallback OnCancel { get; set; }

        [Parameter]
        public EventCallback<bool> OnVisibleChange { get; set; }

        #endregion

        protected bool IsVisible => !Disabled && (Trigger == PopperTrigger.Manual ? ModelValue : internalVisible);

        /// <summary>实际渲染的 icon class（考虑 HideIcon + 默认值 + Type）</summary>
        protected string EffectiveIcon
        {
            get
            {
                if (HideIcon) return null;
                if (!string.IsNullOrWhiteSpace(Icon)) return Icon;
                // 根据 Type 给默认图标
                return Type switch
                {
                    PopconfirmType.Success => "el-icon-success-filled",
                    PopconfirmType.Warning => "el-icon-warning-filled",
                    PopconfirmType.Error => "el-icon-circle-close-filled",
                    PopconfirmType.Info => "el-icon-info-filled",
                    _ => "el-icon-question"
                };
            }
        }

        protected string EffectiveIconColor
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(IconColor)) return IconColor;
                return Type switch
                {
                    PopconfirmType.Success => "var(--el-color-success)",
                    PopconfirmType.Warning => "var(--el-color-warning)",
                    PopconfirmType.Error => "var(--el-color-danger)",
                    PopconfirmType.Info => "var(--el-color-info)",
                    _ => "#f90"
                };
            }
        }

        protected string ReferenceStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add("position:relative", "display:inline-block")
            .Add(Style)
            .ToString();

        protected string PopoverClassValue => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-popover", "el-popper", "el-popconfirm")
            .Add(PopperClass)
            .Add(Cls)
            .ToString();

        protected string PopoverStyleValue => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add("position:absolute")
            .Add("z-index:var(--el-index-popper, 2000)")
            .AddIf(Width > 0, $"width:{Width}px")
            .Add(GetPlacementStyle())
            .Add(PopperStyle)
            .ToString();

        protected string IconClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-popconfirm__icon")
            .Add(EffectiveIcon)
            .ToString();

        protected string IconStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(!string.IsNullOrWhiteSpace(EffectiveIconColor), $"color:{EffectiveIconColor}")
            .ToString();

        protected Task ToggleAsync(MouseEventArgs e)
        {
            return Trigger == PopperTrigger.Click ? SetVisibleAsync(!IsVisible) : Task.CompletedTask;
        }

        protected Task OnMouseOverAsync(MouseEventArgs e)
        {
            return Trigger == PopperTrigger.Hover ? SetVisibleAsync(true) : Task.CompletedTask;
        }

        protected Task OnMouseOutAsync(MouseEventArgs e)
        {
            return Trigger == PopperTrigger.Hover ? SetVisibleAsync(false) : Task.CompletedTask;
        }

        protected async Task ConfirmAsync(MouseEventArgs e)
        {
            if (OnConfirm.HasDelegate) await OnConfirm.InvokeAsync(null);
            if (!Persistent) await SetVisibleAsync(false);
        }

        protected async Task CancelAsync(MouseEventArgs e)
        {
            if (OnCancel.HasDelegate) await OnCancel.InvokeAsync(null);
            if (!Persistent) await SetVisibleAsync(false);
        }

        protected Task CloseBtnAsync() => SetVisibleAsync(false);

        public async Task SetVisibleAsync(bool visible)
        {
            if (Disabled) visible = false;

            if (Trigger == PopperTrigger.Manual)
            {
                if (ModelValueChanged.HasDelegate) await ModelValueChanged.InvokeAsync(visible);
            }
            else
            {
                internalVisible = visible;
            }

            if (OnVisibleChange.HasDelegate) await OnVisibleChange.InvokeAsync(visible);
        }

        private string GetPlacementStyle()
        {
            var placement = string.IsNullOrWhiteSpace(Placement) ? "top" : Placement.ToLowerInvariant();
            var offset = Offset;
            if (placement.StartsWith("bottom", System.StringComparison.Ordinal))
                return $"top:100%;left:50%;transform:translate(-50%, {offset}px)";
            if (placement.StartsWith("left", System.StringComparison.Ordinal))
                return $"right:100%;top:50%;transform:translate(-{offset}px, -50%)";
            if (placement.StartsWith("right", System.StringComparison.Ordinal))
                return $"left:100%;top:50%;transform:translate({offset}px, -50%)";
            return $"bottom:100%;left:50%;transform:translate(-50%, -{offset}px)";
        }
    }

    public enum PopconfirmType
    {
        Success,
        Warning,
        Info,
        Error
    }
}