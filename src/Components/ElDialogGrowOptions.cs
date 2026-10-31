using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// <see cref="ElDialogGrow"/> 的**服务化**选项集合。能力范围参考 Radzen Blazor 的
    /// <c>DialogOptions</c> / <c>ConfirmOptions</c>（参数命名保持一致，便于迁移），
    /// 但实现完全自研：不引用 Radzen 的任何类型、包或运行时。
    ///
    /// 用法一（组件内联）：直接给 <see cref="ElDialogGrow"/> 传同名参数即可，不需要本类。
    /// 用法二（服务化）：<c>ElDialogGrowService.OpenAsync(...)</c> 传入本类实例，由
    /// <c>ElDialogGrowHost</c> 渲染成对话框。
    ///
    /// 选项值在对话框打开期间可随时修改并调用 <see cref="NotifyChanged"/>，宿主会重渲染
    /// （对应 Radzen 的「通过 Cascading Dialog 引用更新标题 / 宽高」示例）。
    /// </summary>
    public class ElDialogGrowOptions
    {
        private string title;
        private string width;
        private string height;
        private string top;
        private bool showTitle = true;
        private bool showClose = true;
        // 批次110zz（2026-10-27 用户口径）：全站弹窗点遮罩不关闭 → 默认翻 false。
        // OpenAsync / ConfirmAsync / AlertAsync 共用本基类这一份默认；
        // 个别确需遮罩关闭的调用点显式传 CloseOnClickModal = true 覆盖。
        private bool closeOnClickModal = false;
        private bool closeOnPressEscape = true;
        private bool autoFocusFirstElement = true;
        private bool focusTrap = true;
        private string cssClass;
        private string wrapperCssClass;
        private string contentCssClass;
        private string modalClass;
        private string ariaLabel;
        private string closeAriaLabel = "Close";
        private string role = "dialog";
        private int closeTabIndex;
        private Func<ElDialogGrowSize, Task> resize;

        /// <summary>变更回调：宿主订阅后，调用 <see cref="NotifyChanged"/> 即可触发对话框重渲染。</summary>
        internal Action<ElDialogGrowOptions> Changed { get; set; }

        /// <summary>对话框标题。</summary>
        public string Title
        {
            get => title;
            set => Set(ref title, value);
        }

        /// <summary>对话框宽度，默认 <c>50%</c>（全屏时忽略）。</summary>
        public string Width
        {
            get => width;
            set => Set(ref width, value);
        }

        /// <summary>对话框高度，默认空（自适应内容）。</summary>
        public string Height
        {
            get => height;
            set => Set(ref height, value);
        }

        /// <summary>距顶部距离，默认 <c>15vh</c>（全屏 / 竖直居中时忽略）。</summary>
        public string Top
        {
            get => top;
            set => Set(ref top, value);
        }

        /// <summary>是否全屏。</summary>
        public bool Fullscreen { get; set; }

        /// <summary>是否在视口中竖直居中。</summary>
        public bool AlignCenter { get; set; }

        /// <summary>标题与页脚是否水平居中（<c>el-dialog--center</c>）。</summary>
        public bool Center { get; set; }

        /// <summary>是否显示遮罩层，默认 <c>true</c>。</summary>
        public bool Modal { get; set; } = true;

        /// <summary>是否显示标题栏，默认 <c>true</c>。</summary>
        public bool ShowTitle
        {
            get => showTitle;
            set => Set(ref showTitle, value);
        }

        /// <summary>是否显示右上角关闭按钮，默认 <c>true</c>。</summary>
        public bool ShowClose
        {
            get => showClose;
            set => Set(ref showClose, value);
        }

        /// <summary>点击遮罩是否关闭，默认 <c>false</c>（110zz：全站弹窗点遮罩不关闭）。</summary>
        public bool CloseOnClickModal
        {
            get => closeOnClickModal;
            set => Set(ref closeOnClickModal, value);
        }

        /// <summary>按 Esc 是否关闭，默认 <c>true</c>。</summary>
        public bool CloseOnPressEscape
        {
            get => closeOnPressEscape;
            set => Set(ref closeOnPressEscape, value);
        }

        /// <summary>是否允许拖拽标题栏移动对话框。</summary>
        public bool Draggable { get; set; }

        /// <summary>是否显示右下角缩放手柄。</summary>
        public bool Resizable { get; set; }

        /// <summary>缩放时的最小宽高（像素）。</summary>
        public int ResizeMinWidth { get; set; } = 120;

        /// <summary>缩放时的最小宽高（像素）。</summary>
        public int ResizeMinHeight { get; set; } = 80;

        /// <summary>缩放结束回调（参数为最终像素宽高）。</summary>
        public Func<ElDialogGrowSize, Task> Resize
        {
            get => resize;
            set => Set(ref resize, value);
        }

        /// <summary>关闭后销毁内部内容（再次打开重新渲染），默认 <c>true</c>（服务化默认按「一次性」处理）。</summary>
        public bool DestroyOnClose { get; set; } = true;

        /// <summary>打开时是否锁定 body 滚动，默认 <c>true</c>。</summary>
        public bool LockScroll { get; set; } = true;

        /// <summary>打开后是否自动聚焦第一个可聚焦元素，默认 <c>true</c>（对齐 Radzen 的 AutoFocusFirstElement）。</summary>
        public bool AutoFocusFirstElement
        {
            get => autoFocusFirstElement;
            set => Set(ref autoFocusFirstElement, value);
        }

        /// <summary>是否把 Tab / Shift+Tab 焦点锁在对话框内，默认 <c>true</c>。</summary>
        public bool FocusTrap
        {
            get => focusTrap;
            set => Set(ref focusTrap, value);
        }

        /// <summary>自定义内联样式（作用在 <c>.el-dialog</c> 上）。</summary>
        public string Style { get; set; }

        /// <summary>附加到对话框盒子的 class。</summary>
        public string CssClass
        {
            get => cssClass;
            set => Set(ref cssClass, value);
        }

        /// <summary>附加到全屏包装层的 class。</summary>
        public string WrapperCssClass
        {
            get => wrapperCssClass;
            set => Set(ref wrapperCssClass, value);
        }

        /// <summary>附加到正文容器的 class。</summary>
        public string ContentCssClass
        {
            get => contentCssClass;
            set => Set(ref contentCssClass, value);
        }

        /// <summary>附加到遮罩层的 class。</summary>
        public string ModalClass
        {
            get => modalClass;
            set => Set(ref modalClass, value);
        }

        /// <summary>无标题时的 aria-label 文案。</summary>
        public string AriaLabel
        {
            get => ariaLabel;
            set => Set(ref ariaLabel, value);
        }

        /// <summary>关闭按钮的 aria-label 文案，默认 <c>Close</c>。</summary>
        public string CloseAriaLabel
        {
            get => closeAriaLabel;
            set => Set(ref closeAriaLabel, value);
        }

        /// <summary>关闭按钮 tabindex，默认 <c>0</c>（可 Tab 到）。</summary>
        public int CloseTabIndex
        {
            get => closeTabIndex;
            set => Set(ref closeTabIndex, value);
        }

        /// <summary>ARIA role，默认 <c>dialog</c>；确认类对话框可传 <c>alertdialog</c>。</summary>
        public string Role
        {
            get => role;
            set => Set(ref role, value);
        }

        /// <summary>自定义层级；未指定时由组件统一递增分配。</summary>
        public int? ZIndex { get; set; }

        /// <summary>关闭前拦截回调；返回 <c>false</c> 阻止关闭（对应 Radzen 的 <c>CanClose</c>）。</summary>
        public Func<Task<bool>> CanClose { get; set; }

        /// <summary>对话框内容组件类型（服务化打开任意组件）。</summary>
        public Type ContentComponent { get; set; }

        /// <summary>传给 <see cref="ContentComponent"/> 的参数。</summary>
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>内联正文（与 <see cref="ContentComponent"/> 二选一，优先组件）。</summary>
        public RenderFragment<ElDialogGrowRef> ChildContent { get; set; }

        /// <summary>自定义标题栏内容。</summary>
        public RenderFragment<ElDialogGrowRef> TitleContent { get; set; }

        /// <summary>页脚内容。</summary>
        public RenderFragment<ElDialogGrowRef> FooterContent { get; set; }

        /// <summary>通知宿主「选项已变更，请重渲染」。</summary>
        public void NotifyChanged()
        {
            Changed?.Invoke(this);
        }

        private void Set<T>(ref T field, T value)
        {
            if (Equals(field, value))
            {
                return;
            }

            field = value;
            NotifyChanged();
        }
    }

    /// <summary>对话框缩放后的像素尺寸。</summary>
    public class ElDialogGrowSize
    {
        public ElDialogGrowSize(double width, double height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>像素宽度。</summary>
        public double Width { get; }

        /// <summary>像素高度。</summary>
        public double Height { get; }
    }
}