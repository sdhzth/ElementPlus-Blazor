using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// MessageBox 类型（对应官方 type: success/warning/info/error）
    /// </summary>
    public enum MessageBoxType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class MessageBoxOption
    {
        /// <summary>
        /// 标题。留空时由弹层渲染处取词条 <c>el.messagebox.title</c>，再回落「提示」。
        /// 默认值从「提示」改成 null，是为了让"宿主没指定"和"宿主指定了中文"能区分开。
        /// </summary>
        public string Title { get; set; }

        public string Message { get; set; }

        public RenderFragment MessageContent { get; set; }

        /// <summary>类型（显示对应图标）</summary>
        public MessageBoxType Type { get; set; } = MessageBoxType.Info;

        /// <summary>自定义图标 class（覆盖 Type 默认图标）</summary>
        public string Icon { get; set; }

        /// <summary>自定义 class</summary>
        public string CustomClass { get; set; }

        /// <summary>自定义 style</summary>
        public string CustomStyle { get; set; }

        /// <summary>居中布局（标题、内容、按钮全部居中）</summary>
        public bool Center { get; set; }

        /// <summary>圆角按钮</summary>
        public bool RoundButton { get; set; }

        /// <summary>把 Message 当作 HTML 渲染</summary>
        public bool DangerouslyUseHTMLString { get; set; }

        #region 按钮

        /// <summary>确认按钮文字。留空时取词条 <c>el.messagebox.confirm</c>，再回落「确定」。</summary>
        public string ConfirmButtonText { get; set; }

        /// <summary>取消按钮文字。留空时取词条 <c>el.messagebox.cancel</c>，再回落「取消」。</summary>
        public string CancelButtonText { get; set; }

        public ButtonType ConfirmButtonType { get; set; } = ButtonType.Primary;

        public ButtonType CancelButtonType { get; set; } = ButtonType.Default;

        public bool ShowCancelButton { get; set; }

        public bool ShowConfirmButton { get; set; } = true;

        public bool DistinguishCancelAndClose { get; set; }

        #endregion

        #region 遮罩 / 关闭

        public bool ShowClose { get; set; } = true;

        public bool CloseOnClickModal { get; set; } = true;

        public bool CloseOnPressEscape { get; set; } = true;

        /// <summary>锁定 body 滚动（全屏弹窗时推荐 true）</summary>
        public bool LockScroll { get; set; } = true;

        #endregion

        #region 输入框

        public bool ShowInput { get; set; }

        public string InputValue { get; set; }

        public string InputPlaceholder { get; set; }

        /// <summary>input type（text/number/email/password...）</summary>
        public string InputType { get; set; } = "text";

        /// <summary>input pattern（正则验证）</summary>
        public string InputPattern { get; set; }

        /// <summary>input maxlength</summary>
        public int? InputMaxlength { get; set; }

        /// <summary>自定义校验函数（返回 null 或 "" 表示通过，否则返回错误消息）</summary>
        public Func<string, string> InputValidator { get; set; }

        /// <summary>InputValidator 返回的错误消息（由组件内部维护）</summary>
        public string InputErrorMessage { get; set; }

        #endregion

        /// <summary>关闭前回调（返回 false 阻止关闭）</summary>
        public Func<MessageBoxAction, Task<bool>> BeforeClose { get; set; }
    }
}