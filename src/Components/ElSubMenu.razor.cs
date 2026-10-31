using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 子菜单（Element Plus 自渲染版）。对齐 EP <c>el-sub-menu</c>：
    /// 属性 index / popper-class / popper-style / show-timeout / hide-timeout / disabled /
    /// popper-offset / expand-close-icon / expand-open-icon / collapse-close-icon / collapse-open-icon；
    /// 插槽 default / title。
    /// DOM：<c>li.el-sub-menu &gt; div.el-sub-menu__title + ul.el-menu.el-menu--inline</c>。
    /// 已知差异：EP 的 <c>teleported</c> 依赖 Vue teleport，本库弹层统一由 <see cref="ElPopup"/> 渲染，不提供该属性。
    /// </summary>
    public partial class ElSubMenu : ElMenuContainer, IMenuItem
    {
        internal SemaphoreSlim SemaphoreSlim { get; private set; } = new SemaphoreSlim(1, 1);
        protected ElementReference Element { get; set; }
        [Inject]
        PopupService PopupService { get; set; }

        #region 参数

        /// <summary>EP <c>index</c>：唯一标志。</summary>
        [Parameter]
        public string Index { get; set; }

        /// <summary>实际使用的唯一标志：未显式指定 Index 时回退到 Label。</summary>
        public string EffectiveIndex => !string.IsNullOrWhiteSpace(Index) ? Index : Label;

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>EP <c>disabled</c>：是否禁用。</summary>
        [Parameter]
        public bool Disabled { get; set; } = false;

        /// <summary>标题文本（EP 的 title 插槽文本形式）。</summary>
        [Parameter]
        public string Label { get; set; }

        /// <summary>EP <c>title</c> 插槽（RenderFragment 形式）。</summary>
        [Parameter]
        public RenderFragment TitleTemplate { get; set; }

        [Parameter]
        public object Model { get; set; }

        /// <summary>标题前置图标类名。</summary>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>EP <c>popper-class</c>：弹出菜单自定义类名（覆盖 Menu 的同名属性）。</summary>
        [Parameter]
        public string PopperClass { get; set; }

        /// <summary>EP <c>popper-style</c>：弹出菜单自定义样式（覆盖 Menu 的同名属性）。</summary>
        [Parameter]
        public string PopperStyle { get; set; }

        /// <summary>EP <c>show-timeout</c>：子菜单出现前的延迟（继承 Menu 的 show-timeout）。</summary>
        [Parameter]
        public int? ShowTimeout { get; set; }

        /// <summary>EP <c>hide-timeout</c>：子菜单消失前的延迟（继承 Menu 的 hide-timeout）。</summary>
        [Parameter]
        public int? HideTimeout { get; set; }

        /// <summary>EP <c>popper-offset</c>：弹出层偏移量（覆盖 Menu 的 popper-offset）。</summary>
        [Parameter]
        public int? PopperOffset { get; set; }

        /// <summary>EP <c>expand-close-icon</c>：展开态 &amp; 子菜单关闭时的图标（需与 expand-open-icon 同时配置）。</summary>
        [Parameter]
        public string ExpandCloseIcon { get; set; }

        /// <summary>EP <c>expand-open-icon</c>：展开态 &amp; 子菜单打开时的图标。</summary>
        [Parameter]
        public string ExpandOpenIcon { get; set; }

        /// <summary>EP <c>collapse-close-icon</c>：折叠态 &amp; 子菜单关闭时的图标。</summary>
        [Parameter]
        public string CollapseCloseIcon { get; set; }

        /// <summary>EP <c>collapse-open-icon</c>：折叠态 &amp; 子菜单打开时的图标。</summary>
        [Parameter]
        public string CollapseOpenIcon { get; set; }

        #endregion

        [CascadingParameter]
        public ElSubMenu ParentMenu { get; set; }

        [CascadingParameter]
        public ElMenuContainer Menu { get; set; }

        [CascadingParameter]
        public MenuOptions Options { get; set; }

        protected string textColor;
        protected string backgroundColor;
        protected string borderColor;

        private bool isActiveFallback;

        private SubMenuOption subMenuOption;

        protected bool IsVertical => Options != null && Options.Mode == MenuMode.Vertical && !Options.Collapse;

        /// <summary>EP <c>isMenuPopup</c>：以弹层方式展开。</summary>
        private bool UsesPopup => TopMenu != null && (TopMenu.Mode == MenuMode.Horizontal || (TopMenu.Mode == MenuMode.Vertical && TopMenu.Collapse));

        /// <summary>
        /// 是否第一级子菜单（EP 用它决定箭头图标与弹层方向）。
        /// ElPopup 需要据此决定弹层方位（仅 horizontal 的一级子菜单向下展开），故为 internal。
        /// </summary>
        internal bool IsFirstLevel => ParentMenu == null;

        internal override int Level => ParentMenu == null ? 0 : ParentMenu.Level + 1;

        /// <summary>内层 ul 的层级（= 本子菜单层级 + 1），用于 --el-menu-level 缩进。</summary>
        internal int ChildLevel => Level + 1;

        #region 展开态

        protected bool IsOpened { get; set; } = false;

        /// <summary>EP expose <c>opened</c>：当前是否展开。</summary>
        public bool Opened => IsOpened;

        /// <summary>由根菜单 API（open / close / default-openeds）驱动的开合。</summary>
        internal async Task SetOpenedAsync(bool value)
        {
            if (value == IsOpened && !UsesPopup)
            {
                return;
            }

            if (!value)
            {
                if (UsesPopup && subMenuOption != null)
                {
                    await CloseAsync();
                    return;
                }

                IsOpened = false;
                await TopMenu.NotifyCloseAsync(this);
                StateHasChanged();
                return;
            }

            if (UsesPopup)
            {
                await ShowPopupCoreAsync();
                return;
            }

            IsOpened = true;
            await TopMenu.NotifyOpenAsync(this);
            StateHasChanged();
        }

        /// <summary>由根菜单同步写入展开态（default-openeds / unique-opened 落地用，不触发事件）。</summary>
        internal void ApplyOpenedState(bool value)
        {
            if (!value && IsOpened && UsesPopup && subMenuOption != null)
            {
                _ = CloseAsync();
            }

            IsOpened = value;
        }

        internal async Task CloseFromMenuAsync(bool notify)
        {
            if (!IsOpened)
            {
                return;
            }

            IsOpened = false;
            if (notify)
            {
                await TopMenu.NotifyCloseAsync(this);
            }
        }

        /// <summary>
        /// 刷新时把「根菜单的展开态」补齐到实例上。
        /// 展开态的唯一真源是根菜单的 <see cref="ElMenu.OpenedMenus"/>，但 <c>default-openeds</c> 与
        /// 「激活项所在的祖先路径」（EP <c>initMenu</c>）只写了根菜单的那份列表，实例的
        /// <see cref="IsOpened"/> 不会自动跟随 —— 表现为该展开的子菜单仍然 <c>display:none</c>。
        /// 这里只做「补齐展开」，绝不反向强制收起（收起只由点击 / unique-opened / open-close API 驱动），
        /// 所以 <c>CanCollapse=false</c> 的常开子菜单与弹层态都不受影响。
        /// </summary>
        public override void Refresh()
        {
            if (!UsesPopup && !IsOpened && TopMenu != null && TopMenu.IsSubMenuOpened(EffectiveIndex))
            {
                IsOpened = true;
            }

            base.Refresh();
        }

        #endregion

        #region 激活态

        /// <summary>
        /// EP 语义：子菜单的 <c>is-active</c> 表示「后代中存在激活项」，
        /// 而不是「鼠标悬浮」。悬浮高亮交给 CSS <c>:hover</c>。
        /// </summary>
        public bool IsActive
        {
            get
            {
                if (TopMenu == null)
                {
                    return isActiveFallback;
                }

                // 键盘导航等场景会显式 Activate() 子菜单，这里把该状态一并纳入
                return HasActiveDescendant() || isActiveFallback;
            }
        }

        private bool HasActiveDescendant()
        {
            foreach (var child in Children)
            {
                if (child is ElSubMenu subMenu)
                {
                    if (subMenu.IsActive) return true;
                }
                else if (child is ElMenuItem menuItem)
                {
                    if (TopMenu.IsActiveIndex(menuItem.EffectiveIndex)) return true;
                }
            }

            return false;
        }

        public void Activate()
        {
            if (Disabled || (Options != null && Options.Disabled))
            {
                return;
            }

            isActiveFallback = true;
        }

        public void DeActivate()
        {
            isActiveFallback = false;
            borderColor = "transparent";

            if (IsOpened)
            {
                IsOpened = false;
                if (TopMenu != null)
                {
                    _ = TopMenu.NotifyCloseAsync(this);
                }
            }

            StateHasChanged();
        }

        #endregion

        #region 类名 / 样式

        protected string SubMenuClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-sub-menu", Cls)
            .AddIf(IsActive, "is-active")
            .AddIf(IsOpened, "is-opened")
            .AddIf(Disabled || (Options != null && Options.Disabled), "is-disabled")
            .ToString();

        protected string EffectiveTitle => EffectiveCollapse ? Label : null;

        protected bool EffectiveCollapse => Options?.Collapse == true;

        #region ellipsis 截断悬停 tooltip（本库扩展）

        private ElementReference titleTextElement;
        private string ellipsisTooltipText;
        private bool ellipsisTooltipVisible;
        private string ellipsisPopperStyle;

        /// <summary>
        /// 标题文字被 ellipsis 截断时是否挂 elTooltip 悬停显示全文（口径同 <see cref="ElMenuItem.ShowEllipsisTooltip"/>）。
        /// 折叠态与横向模式都排除：折叠态用原生 <c>title</c>（见 <see cref="EffectiveTitle"/>），
        /// 横向模式悬停本身要打开子菜单弹层，叠 tooltip 会变成两层浮层互相盖。
        /// </summary>
        protected bool ShowEllipsisTooltip => Options != null && Options.Ellipsis == true
                                             && !EffectiveCollapse
                                             && Options.Mode != MenuMode.Horizontal;

        protected bool EllipsisTooltipVisible => ellipsisTooltipVisible;

        protected string EllipsisTooltipText => ellipsisTooltipText;

        /// <summary>与子菜单弹层同一主题口径（<c>popper-effect</c>），未指定时用菜单默认的 light。</summary>
        protected string EllipsisTooltipEffect => string.IsNullOrWhiteSpace(Options?.PopperEffect) ? "light" : Options.PopperEffect;

        /// <summary>钉成 fixed 的浮层坐标（口径见 <see cref="TextClip.BuildFixedPopperStyle"/>），侧栏这类
        /// <c>overflow-y:auto</c> 的祖先会把 absolute 浮层整条裁掉。未截断时为 null。</summary>
        protected string EllipsisTooltipPopperStyle
        {
            get
            {
                var user = PopperStyle ?? Options?.PopperStyle;
                if (ellipsisPopperStyle == null)
                {
                    return user;
                }
                return string.IsNullOrWhiteSpace(user) ? ellipsisPopperStyle : ellipsisPopperStyle + ";" + user;
            }
        }

        /// <summary>mouseenter 时测算：只有真的被截断才浮出 tooltip。</summary>
        private async Task MeasureEllipsisTooltipAsync()
        {
            if (!ShowEllipsisTooltip)
            {
                return;
            }

            var state = await TextClip.MeasureClippedStateAsync(JSRuntime, titleTextElement);
            ellipsisTooltipText = state == null ? null : TextClip.ExtractText(state);
            ellipsisTooltipVisible = ellipsisTooltipText != null;
            ellipsisPopperStyle = ellipsisTooltipVisible
                ? TextClip.BuildFixedPopperStyle(state, "right", out _)
                : null;
        }

        private void HideEllipsisTooltip()
        {
            ellipsisTooltipVisible = false;
            ellipsisTooltipText = null;
            ellipsisPopperStyle = null;
        }

        #endregion

        /// <summary>
        /// 箭头图标：展开态（horizontal 一级 / vertical 非折叠）用 arrow-down，
        /// 折叠态或弹层态用 arrow-right；成对自定义图标优先。
        /// </summary>
        protected string ArrowIcon
        {
            get
            {
                if (IsExpandedMode)
                {
                    if (!string.IsNullOrWhiteSpace(ExpandCloseIcon) && !string.IsNullOrWhiteSpace(ExpandOpenIcon))
                    {
                        return IsOpened ? ExpandOpenIcon : ExpandCloseIcon;
                    }
                    return "el-icon-arrow-down";
                }

                if (!string.IsNullOrWhiteSpace(CollapseCloseIcon) && !string.IsNullOrWhiteSpace(CollapseOpenIcon))
                {
                    return IsOpened ? CollapseOpenIcon : CollapseCloseIcon;
                }
                return "el-icon-arrow-right";
            }
        }

        /// <summary>是否使用自定义图标（EP：自定义图标时不叠加旋转）。</summary>
        protected bool UsesCustomArrow => IsExpandedMode
            ? !string.IsNullOrWhiteSpace(ExpandCloseIcon) && !string.IsNullOrWhiteSpace(ExpandOpenIcon)
            : !string.IsNullOrWhiteSpace(CollapseCloseIcon) && !string.IsNullOrWhiteSpace(CollapseOpenIcon);

        /// <summary>箭头变换（EP：展开时 rotateZ(180deg)，自定义图标时 none）。</summary>
        protected string ArrowStyle => IsOpened && !UsesCustomArrow && IsExpandedMode
            ? "transform:rotateZ(180deg)"
            : "transform:none";

        /// <summary>EP <c>currentPlacement</c> 的等价判定：展开态 = horizontal 一级 或 vertical 非折叠。</summary>
        protected bool IsExpandedMode => (Options != null && Options.Mode == MenuMode.Horizontal && IsFirstLevel)
                                          || IsVertical;

        protected string EffectiveIcon => NormalizeIcon(Icon);

        /// <summary>
        /// 内层 ul 的行内样式：<c>--el-menu-level</c> 逐级缩进 + 折叠时 <c>display:none</c>
        /// （EP 用 CollapseTransition + v-show，本库用等价的 display 切换）。
        /// </summary>
        protected string ChildStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add($"--el-menu-level:{ChildLevel}")
            .AddIf(!IsOpened, "display:none")
            .ToString();

        #endregion

        #region 超时（EP show-timeout / hide-timeout）

        /// <summary>展开延迟的取消源：鼠标在延迟期内移开时撤销展开。</summary>
        private CancellationTokenSource showDelayCancellationTokenSource;

        private int EffectiveShowTimeout => ShowTimeout ?? (Options?.ShowTimeout ?? 300);

        /// <summary>EP <c>hide-timeout</c>：弹层关闭延迟（ElPopup 移出弹层时使用，故为 internal）。</summary>
        internal int EffectiveHideTimeout => HideTimeout ?? (Options?.HideTimeout ?? 300);
        private int EffectivePopperOffset => PopperOffset ?? (Options?.PopperOffset ?? 6);

        /// <summary>EP <c>popper-effect</c>：子菜单弹层主题，仅继承自 <see cref="MenuOptions"/>（dark/light）。</summary>
        private string EffectivePopperEffect
        {
            get
            {
                var effect = Options?.PopperEffect;
                return string.IsNullOrWhiteSpace(effect) ? "dark" : effect;
            }
        }

        #endregion

        protected override void OnInitialized()
        {
            backgroundColor = Options?.BackgroundColor;
            textColor = Options?.TextColor;
            TopMenu?.RegisterSubMenu(this);
            Menu?.AddMenuItem(this);
            if (TopMenu != null && !TopMenu.CanCollapse && IsVertical)
            {
                IsOpened = true;
            }
            base.OnInitialized();
        }

        #region 弹层展开 / 收起

        /// <summary>创建弹层并置为展开（不含等待关闭的续体）。</summary>
        private async Task ShowPopupCoreAsync()
        {
            await SemaphoreSlim.WaitAsync();
            try
            {
                if (IsOpened)
                {
                    CancelPendingClose();
                    return;
                }

                var delay = EffectiveShowTimeout;
                if (delay > 0)
                {
                    var showCts = new CancellationTokenSource();
                    showDelayCancellationTokenSource = showCts;
                    try
                    {
                        await Task.Delay(delay, showCts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                }
                if (IsOpened)
                {
                    return;
                }

                subMenuOption = new SubMenuOption()
                {
                    SubMenu = this,
                    Content = ChildContent,
                    Options = Options,
                    Target = Element,
                    PopperClass = PopperClass ?? Options?.PopperClass,
                    PopperStyle = PopperStyle ?? Options?.PopperStyle,
                    Offset = EffectivePopperOffset,
                    PopperEffect = EffectivePopperEffect,
                    Level = ChildLevel,
                    HideTimeoutMs = EffectiveHideTimeout
                };
                var taskCompletionSource = new TaskCompletionSource<int>();
                subMenuOption.TaskCompletionSource = taskCompletionSource;
                // 这里**绝不能**等待 SubMenuOptionsCore 清空：嵌套子菜单（horizontal 弹层里的子菜单、
                // vertical+collapse 的第二级）打开时，父级弹层仍留在集合里，等待会永久阻塞 ——
                // 表现就是「点开父菜单后，子菜单怎么都打不开」。同级互斥由 hide-timeout 负责，
                // 后代弹层由 ElPopup.CloseSubMenuAsync 级联关闭。
                PopupService.SubMenuOptionsCore.Add(subMenuOption);
                IsOpened = true;
                if (TopMenu != null)
                {
                    await TopMenu.NotifyOpenAsync(this);
                }
                StateHasChanged();
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        protected async Task OnOverAsync()
        {
            await MeasureEllipsisTooltipAsync();
            if (Disabled || (Options != null && Options.Disabled))
            {
                return;
            }
            if (UsesPopup)
            {
                await ShowPopupCoreAsync();
                var option = subMenuOption;
                if (option?.TaskCompletionSource == null)
                {
                    return;
                }
                // 弹层关闭时由 ElPopup 完成该 Task（见 CloseSubMenuCoreAsync）
                await option.TaskCompletionSource.Task;
                if (!ReferenceEquals(option, subMenuOption) || option.IsShow)
                {
                    // 期间又被重新打开（或已换成新的弹层实例）：不要误清展开态
                    return;
                }
                borderColor = "transparent";
                IsOpened = false;
            }
        }

        internal void KeepSubMenuOpen()
        {
            subMenuOption?.Instance?.KeepShowSubMenu(subMenuOption);
        }

        /// <summary>
        /// 保持「自身 + 全部祖先」的弹层不被关闭。
        /// 弹层统一由 <see cref="ElPopup"/> 渲染成 body 级兄弟节点，嵌套弹层并不在父弹层的 DOM 内部 ——
        /// 鼠标从父弹层移入子弹层时会离开父弹层容器并触发其延迟关闭；EP 用 <c>mouseInChild</c> +
        /// 向父级 <c>dispatchEvent('mouseenter')</c> 解决，这里等价地逐级取消关闭计时。
        /// </summary>
        internal void KeepSelfAndAncestorsOpen()
        {
            KeepSubMenuOpen();
            ParentMenu?.KeepSelfAndAncestorsOpen();
        }

        /// <summary>仅关闭「弹层形态」的展开（vertical 内联展开不受影响）。</summary>
        internal Task ClosePopupAsync()
        {
            if (UsesPopup && subMenuOption != null)
            {
                return CloseAsync();
            }

            return Task.CompletedTask;
        }

        internal async Task CloseAsync()
        {
            if (subMenuOption?.Close != null)
            {
                await subMenuOption.Close(subMenuOption);
            }
            if (IsOpened)
            {
                IsOpened = false;
                if (TopMenu != null)
                {
                    await TopMenu.NotifyCloseAsync(this);
                }
            }
        }

        private void CancelPendingClose()
        {
            var source = subMenuOption?.ClosingTaskCancellationTokenSource;
            if (source == null) return;
            try
            {
                if (!source.IsCancellationRequested)
                {
                    source.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }

        void DisposeTokenSource(CancellationTokenSource cancellationTokenSource)
        {
            if (cancellationTokenSource == null)
            {
                return;
            }
            try
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }
                cancellationTokenSource.Dispose();
            }
            catch (ObjectDisposedException)
            {

            }
        }

        protected async Task OnOutAsync()
        {
            // 悬停 tooltip 与「展开弹层」共用一对 mouseenter / mouseleave：离开标题行即收起，
            // 必须放在下面的各种提前 return 之前（vertical 内联展开的分支会直接 return）。
            HideEllipsisTooltip();

            // 鼠标在展开延迟内移开 → 撤销尚未发生的展开
            if (showDelayCancellationTokenSource != null && !showDelayCancellationTokenSource.IsCancellationRequested)
            {
                showDelayCancellationTokenSource.Cancel();
            }

            if (Disabled || (Options != null && Options.Disabled))
            {
                return;
            }
            // vertical + 非折叠：子菜单是「内联展开」，EP 语义下鼠标移出并不收起 ——
            // 内联 ul 是标题 div 的兄弟节点，从标题移向子项必然触发一次 mouseleave；
            // 若在此收起，子项会被 display:none 挡住而永远点不到（历史缺陷）。
            // 收起只由「点击标题 / unique-opened / 根菜单 open-close API」驱动。
            if (!UsesPopup)
            {
                return;
            }
            if (Options != null && Options.MenuTrigger == MenuTrigger.Click)
            {
                return;
            }
            if (!IsOpened)
            {
                return;
            }
            if (subMenuOption?.IsShow == true)
            {
                subMenuOption.ClosingTaskCancellationTokenSource = new CancellationTokenSource();
                var option = subMenuOption;
                var delay = EffectiveHideTimeout;
                var closingTask = Task.Delay(delay, subMenuOption.ClosingTaskCancellationTokenSource.Token).ContinueWith(async task =>
                     {
                         if (task.IsCanceled)
                         {
                             DisposeTokenSource(option.ClosingTaskCancellationTokenSource);
                             return;
                         }
                         DisposeTokenSource(option.ClosingTaskCancellationTokenSource);
                         IsOpened = false;
                         if (option.Close == null)
                         {
                             return;
                         }
                         await InvokeAsync(async () =>
                         {
                             await option.Close(option);
                         });
                     });
                subMenuOption.ClosingTask = await closingTask;
            }
        }

        #endregion

        #region 点击 / 键盘

        /// <summary>
        /// EP <c>handleClick</c>：hover 触发的 horizontal 与折叠态的 vertical 不响应点击；
        /// 其余情况切换展开态（click 触发的 horizontal 也切换）。
        /// </summary>
        protected async Task OnClickAsync()
        {
            if (Disabled || (Options != null && Options.Disabled))
            {
                return;
            }

            if (Options != null && Options.MenuTrigger == MenuTrigger.Hover && Options.Mode == MenuMode.Horizontal)
            {
                return;
            }
            if (Options != null && Options.Collapse && Options.Mode == MenuMode.Vertical)
            {
                return;
            }

            if (IsVertical && TopMenu != null && !TopMenu.CanCollapse)
            {
                return;
            }

            if (UsesPopup)
            {
                if (IsOpened)
                {
                    await CloseAsync();
                }
                else
                {
                    await ShowPopupCoreAsync();
                }
                return;
            }

            IsOpened = !IsOpened;
            if (TopMenu != null)
            {
                if (IsOpened)
                {
                    await TopMenu.NotifyOpenAsync(this);
                }
                else
                {
                    await TopMenu.NotifyCloseAsync(this);
                }
            }
            StateHasChanged();
        }

        protected async Task OnKeyDownAsync(KeyboardEventArgs e)
        {
            if (e.Key != "Enter" && e.Key != " ")
            {
                return;
            }
            await OnClickAsync();
        }

        internal Task ToggleByKeyboardAsync()
        {
            if (Disabled || (Options != null && Options.Disabled))
            {
                return Task.CompletedTask;
            }
            if (UsesPopup)
            {
                return OnOverAsync();
            }
            return OnClickAsync();
        }

        #endregion

        public IReadOnlyList<string> GetIndexPath()
        {
            var path = new List<string>();
            var parent = ParentMenu;
            while (parent != null)
            {
                path.Insert(0, parent.EffectiveIndex);
                parent = parent.ParentMenu;
            }
            path.Add(EffectiveIndex);
            return path;
        }

        // 归一口径与 <ElIcon Name="..."> 完全一致（补前缀 + kebab + 字体别名），
        // 否则同一个 u_icon 在两个组件里一个有图、一个空白格。
        private static string NormalizeIcon(string icon) => ElementIconName.ToClass(icon);

        public override void Dispose()
        {
            base.Dispose();
            TopMenu?.UnregisterSubMenu(this);
            Menu?.RemoveMenuItem(this);
        }

        protected override bool ShouldRender()
        {
            return true;
        }
    }
}
