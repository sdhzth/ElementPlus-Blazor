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
    /// 导航菜单（Element Plus 自渲染版）。
    /// 对齐 <c>https://element-plus.org/zh-CN/component/menu</c> 的 Menu / SubMenu / MenuItem / MenuItemGroup 四件套：
    /// <list type="bullet">
    /// <item>属性：mode / collapse / default-active / default-openeds / unique-opened / menu-trigger /
    /// collapse-transition / popper-effect / popper-class / popper-style / show-timeout / hide-timeout /
    /// popper-offset / background-color / text-color / active-text-color / router。</item>
    /// <item>事件：select(index, indexPath, item) / open(index, indexPath) / close(index, indexPath)。</item>
    /// <item>Exposes：open / close / updateActiveIndex / handleResize。</item>
    /// </list>
    /// 已知差异见 <c>docs/menu-alignment.md</c>：ellipsis / persistent / close-on-click-outside / teleported
    /// 依赖 Vue 运行时或 DOM 测量，本库不实现；router 默认 true（Blazor 侧靠 NavigationManager 跳转）。
    /// </summary>
    public partial class ElMenu : ElMenuContainer
    {
        internal SemaphoreSlim SemaphoreSlim { get; private set; } = new SemaphoreSlim(1, 1);

        protected string modeClass;
        protected string menuClass;
        protected string menuStyle;

        #region 参数

        /// <summary>菜单展示模式（EP <c>mode</c>）：vertical / horizontal，默认 vertical。</summary>
        [Parameter]
        public MenuMode Mode { get; set; } = MenuMode.Vertical;

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// 仓库自有参数：垂直模式下是否允许折叠子菜单。
        /// false 表示所有子菜单常开（EP 无对应属性，保留以兼容既有调用方）。
        /// </summary>
        [Parameter]
        public bool CanCollapse { get; set; } = true;

        /// <summary>EP <c>collapse</c>：是否水平折叠收起菜单（仅在 mode 为 vertical 时可用）。</summary>
        [Parameter]
        public bool Collapse { get; set; }

        /// <summary><c>Collapse</c> 的别名（既有调用方使用）。</summary>
        [Parameter]
        public bool IsCollapse
        {
            get => Collapse;
            set => Collapse = value;
        }

        /// <summary>
        /// EP <c>router</c>：是否启用路由模式（激活时以 index / route 作为路径跳转）。
        /// EP 默认 false；本库默认 true —— Blazor 侧菜单几乎总是导航入口，保留既有默认行为。
        /// </summary>
        [Parameter]
        public bool Router { get; set; } = true;

        /// <summary>EP <c>unique-opened</c>：是否只保持一个子菜单展开。</summary>
        [Parameter]
        public bool UniqueOpened { get; set; }

        /// <summary>EP <c>menu-trigger</c>：子菜单打开的触发方式（仅 horizontal 生效）。</summary>
        [Parameter]
        public MenuTrigger MenuTrigger { get; set; } = MenuTrigger.Hover;

        /// <summary><c>MenuTrigger</c> 的字符串形式（hover / click）。</summary>
        [Parameter]
        public string Trigger
        {
            get => MenuTrigger.ToString().ToLowerInvariant();
            set => MenuTrigger = string.Equals(value, "click", StringComparison.OrdinalIgnoreCase)
                ? MenuTrigger.Click
                : MenuTrigger.Hover;
        }

        /// <summary>EP <c>popper-class</c>：所有弹出菜单的自定义类名。</summary>
        [Parameter]
        public string PopperClass { get; set; }

        /// <summary>EP <c>popper-style</c>：所有弹出菜单的自定义样式。</summary>
        [Parameter]
        public string PopperStyle { get; set; }

        /// <summary>EP <c>popper-effect</c>：折叠时 Tooltip 主题（dark / light），默认 dark。</summary>
        [Parameter]
        public string PopperEffect { get; set; } = "light";

        /// <summary>仓库自有参数：深色主题（EP 无该属性，用自定义 class 承载）。</summary>
        [Parameter]
        public MenuTheme Theme { get; set; } = MenuTheme.Light;

        /// <summary>EP <c>select</c>：菜单激活回调。</summary>
        [Parameter]
        public EventCallback<MenuSelectEventArgs> OnSelect { get; set; }

        /// <summary>EP <c>open</c>：sub-menu 展开回调。</summary>
        [Parameter]
        public EventCallback<MenuOpenEventArgs> OnOpen { get; set; }

        /// <summary>EP <c>close</c>：sub-menu 收起回调。</summary>
        [Parameter]
        public EventCallback<MenuOpenEventArgs> OnClose { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>EP <c>background-color</c>（已废弃，推荐用 <c>--el-menu-bg-color</c>）。</summary>
        [Parameter]
        public string BackgroundColor { get; set; }

        /// <summary>菜单匹配方法，参数为当前菜单项的路由（仓库自有）。</summary>
        [Parameter]
        public Func<string, bool> Match { get; set; }

        /// <summary>EP <c>collapse-transition</c>：是否开启折叠动画，默认 true。</summary>
        [Parameter]
        public bool CollapseTransition { get; set; } = true;

        /// <summary>EP <c>default-openeds</c>：默认打开的 sub-menu 的 index 数组。</summary>
        [Parameter]
        public IEnumerable<string> DefaultOpeneds { get; set; }

        /// <summary>EP <c>text-color</c>（已废弃，推荐用 <c>--el-menu-text-color</c>）。</summary>
        [Parameter]
        public string TextColor { get; set; }

        /// <summary>EP <c>active-text-color</c>（已废弃，推荐用 <c>--el-menu-active-color</c>）。</summary>
        [Parameter]
        public string ActiveTextColor { get; set; }

        /// <summary>
        /// 仓库自有参数：悬浮背景色，映射到 <c>--el-menu-hover-bg-color</c>。
        /// 默认不输出（null）：EP 的 <c>use-menu-color.ts</c> 只在给了 <c>background-color</c> 时才派生悬浮色
        /// （该色加深 20%），否则让 <c>.el-menu</c>／<c>.el-menu--dark</c> 的类级默认值生效。
        /// </summary>
        [Parameter]
        public string HoverColor { get; set; }

        /// <summary>EP <c>default-active</c>：页面加载时默认激活菜单的 index。</summary>
        [Parameter]
        public string DefaultActive { get; set; }

        /// <summary><c>DefaultActive</c> 的别名（既有调用方使用）。</summary>
        [Parameter]
        public string DefaultActiveIndex
        {
            get => DefaultActive;
            set => DefaultActive = value;
        }

        /// <summary>EP <c>show-timeout</c>：菜单出现前的延迟（毫秒），默认 300。</summary>
        [Parameter]
        public int ShowTimeout { get; set; } = 300;

        /// <summary>EP <c>hide-timeout</c>：菜单消失前的延迟（毫秒），默认 300。</summary>
        [Parameter]
        public int HideTimeout { get; set; } = 300;

        /// <summary>EP <c>popper-offset</c>：弹出层的偏移量（对所有子菜单有效），默认 6。</summary>
        [Parameter]
        public int PopperOffset { get; set; } = 6;

        /// <summary>
        /// 官方 <c>ellipsis</c>（EP 2.14.0 <c>menu.ts:124</c>，默认 <b>true</b>）：菜单项与子菜单标题的文字超长时
        /// 是否省略号截断。
        /// <remarks>
        /// 口径差异（必须知道）：EP 的 <c>ellipsis</c> 注释写着 "available only in horizontal mode"
        /// （<c>menu.ts:122</c>），它管的是 ResizeObserver + 把溢出项折叠进「更多」弹层
        /// （<c>menu.ts:412</c>、<c>menu.ts:508</c>），**不是** CSS 文字截断；EP 的 vertical 菜单里超长文字
        /// 是直接溢出容器的。本库没有实现折叠（需要逐项测量 + 弹层），把该属性实现为**文字级 CSS 截断**，
        /// 且 vertical / horizontal 都生效；默认值取 EP 的 true。设成 false 时不输出截断类，DOM 与本属性
        /// 引入前逐字节一致。
        /// </remarks>
        /// </summary>
        [Parameter]
        public bool Ellipsis { get; set; } = true;

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        #endregion

        #region 状态（EP 内部字段：items / submenus / openedMenus / activeIndex）

        /// <summary>已注册的菜单项（index → 菜单项）。</summary>
        internal readonly Dictionary<string, ElMenuItem> Items = new(StringComparer.Ordinal);

        /// <summary>已注册的子菜单（index → 子菜单）。</summary>
        internal readonly Dictionary<string, ElSubMenu> SubMenus = new(StringComparer.Ordinal);

        /// <summary>当前展开的 sub-menu 的 index 列表（EP <c>openedMenus</c>）。</summary>
        internal readonly List<string> OpenedMenus = new();

        /// <summary>当前激活的菜单 index（EP <c>activeIndex</c>）。</summary>
        public string ActiveIndex { get; private set; }

        /// <summary>
        /// EP <c>isMenuPopup</c>：子菜单是否以弹层方式展开（horizontal，或 vertical + collapse）。
        /// </summary>
        internal bool IsMenuPopup => Mode == MenuMode.Horizontal || (Mode == MenuMode.Vertical && Collapse);

        protected IMenuItem activeItem;

        [Parameter]
        public IMenuItem ActiveItem
        {
            get => activeItem;
            set => activeItem = value;
        }

        [Parameter]
        public EventCallback<IMenuItem> ActiveItemChanged { get; set; }

        #endregion

        #region 注册

        internal void RegisterItem(ElMenuItem item)
        {
            if (item == null) return;
            var index = item.EffectiveIndex;
            if (!string.IsNullOrEmpty(index)) Items[index] = item;
            AddMenuItem(item);
        }

        internal void UnregisterItem(ElMenuItem item)
        {
            if (item == null) return;
            var index = item.EffectiveIndex;
            if (!string.IsNullOrEmpty(index) && Items.TryGetValue(index, out var current) && ReferenceEquals(current, item))
            {
                Items.Remove(index);
            }
            RemoveMenuItem(item);
        }

        internal void RegisterSubMenu(ElSubMenu subMenu)
        {
            if (subMenu == null) return;
            var index = subMenu.EffectiveIndex;
            if (!string.IsNullOrEmpty(index))
            {
                SubMenus[index] = subMenu;

                // 子菜单晚于 default-openeds 落地时补一次展开态
                if (OpenedMenus.Contains(index))
                {
                    subMenu.ApplyOpenedState(true);
                }
            }
            AddMenuItem(subMenu);
        }

        internal void UnregisterSubMenu(ElSubMenu subMenu)
        {
            if (subMenu == null) return;
            var index = subMenu.EffectiveIndex;
            if (!string.IsNullOrEmpty(index) && SubMenus.TryGetValue(index, out var current) && ReferenceEquals(current, subMenu))
            {
                SubMenus.Remove(index);
            }
            if (!string.IsNullOrEmpty(index))
            {
                OpenedMenus.Remove(index);
            }
            RemoveMenuItem(subMenu);
        }

        #endregion

        #region 激活（EP updateActiveIndex / handleMenuItemClick）

        /// <summary>判断某个 index 是否为当前激活项。</summary>
        public bool IsActiveIndex(string index)
            => !string.IsNullOrEmpty(index) && string.Equals(ActiveIndex, index, StringComparison.Ordinal);

        /// <summary>
        /// EP <c>updateActiveIndex</c>：通过索引激活指定菜单。
        /// 找不到该 index 时回退到「当前激活项 → default-active」，最后才原样接受传入值。
        /// </summary>
        public void UpdateActiveIndex(string value)
        {
            var resolved = ResolveActiveIndex(value);
            if (string.Equals(ActiveIndex, resolved, StringComparison.Ordinal)) return;

            var previous = ActiveIndex;
            SetActiveIndexCore(resolved);
            RefreshActivePath(previous, resolved);
        }

        private string ResolveActiveIndex(string value)
        {
            if (!string.IsNullOrEmpty(value) && Items.ContainsKey(value)) return value;
            if (!string.IsNullOrEmpty(ActiveIndex) && Items.ContainsKey(ActiveIndex)) return ActiveIndex;
            if (!string.IsNullOrEmpty(DefaultActive) && Items.ContainsKey(DefaultActive)) return DefaultActive;
            return value ?? string.Empty;
        }

        /// <summary>写入激活 index（不刷新、不触发事件）。</summary>
        internal void SetActiveIndexCore(string index)
        {
            ActiveIndex = index ?? string.Empty;
            if (Items.TryGetValue(ActiveIndex, out var item))
            {
                activeItem = item;
            }

            // EP initMenu：展开激活项所在路径上的全部子菜单
            if (Mode == MenuMode.Vertical && !Collapse && Items.TryGetValue(ActiveIndex, out var active))
            {
                foreach (var parent in active.GetIndexPath().Take(active.GetIndexPath().Count - 1))
                {
                    if (!OpenedMenus.Contains(parent)) OpenedMenus.Add(parent);
                }
            }
        }

        /// <summary>刷新全部已注册项（激活态 / 展开态变更后重算）。</summary>
        internal void RefreshItems()
        {
            foreach (var item in Items.Values.ToList()) item.Refresh();
            foreach (var subMenu in SubMenus.Values.ToList()) subMenu.Refresh();
            Refresh();
        }

        /// <summary>
        /// 只刷新「旧激活项 / 新激活项」这两条祖先链上的菜单项与子菜单。
        /// <para>
        /// 改一处激活高亮本来只需重渲染 2 个菜单项，但 <see cref="RefreshItems"/> 会重渲染全部已注册项
        /// 并连带重渲染整棵菜单（100+ 项时单次导航阻塞主线程约 0.5s，表现为切页签 / 点菜单卡顿）。
        /// 这里做等价的最小刷新：菜单项的激活态取自根菜单的 <see cref="ActiveIndex"/>，
        /// 子菜单的 <c>is-active</c> 取决于「后代中是否存在激活项」，所以只有这两条链会变化。
        /// </para>
        /// </summary>
        private void RefreshActivePath(string previous, string current)
        {
            RefreshPathOf(previous, IndexPathOf(current));
            RefreshPathOf(current, IndexPathOf(previous));
        }

        /// <summary>某个 index 的祖先链（含自身）；index 未注册时返回 <c>null</c>。</summary>
        private IReadOnlyList<string> IndexPathOf(string index)
            => !string.IsNullOrEmpty(index) && Items.TryGetValue(index, out var item)
                ? item.GetIndexPath()
                : null;

        private void RefreshPathOf(string index, IReadOnlyList<string> otherPath)
        {
            if (string.IsNullOrEmpty(index) || !Items.TryGetValue(index, out var item))
            {
                return;
            }

            item.Refresh();

            // 祖先子菜单的 is-active 依赖后代，需一并重渲染（路径末位是该菜单项自身）。
            // 但两条激活链共用的祖先子菜单既不会改变 is-active，又已处于展开态，
            // 重渲染它只会连带重建整棵子树（该分组下全部菜单项）——直接跳过。
            var path = item.GetIndexPath();
            for (var i = 0; i < path.Count - 1; i++)
            {
                if (!SubMenus.TryGetValue(path[i], out var subMenu))
                {
                    continue;
                }

                if (subMenu.Opened && otherPath != null && otherPath.Contains(path[i]))
                {
                    continue;
                }

                subMenu.Refresh();
            }
        }

        /// <summary>菜单项被点击：置激活态、收起弹层、派发 select。</summary>
        internal async Task SelectItemAsync(ElMenuItem item)
        {
            if (item == null) return;

            var index = item.EffectiveIndex;

            // EP：horizontal 或 collapse 下，选中后收起全部弹出子菜单
            // （EP 的弹层可见性由 openedMenus 派生，清空即全关；本库弹层是独立对象，需显式关闭）
            if (Mode == MenuMode.Horizontal || Collapse)
            {
                if (OpenedMenus.Count > 0) OpenedMenus.Clear();

                foreach (var subMenu in SubMenus.Values.ToList())
                {
                    await subMenu.ClosePopupAsync();
                }
            }

            var previous = ActiveIndex;
            SetActiveIndexCore(index);
            RefreshActivePath(previous, index);

            await NotifySelectAsync(item);

            if (ActiveItemChanged.HasDelegate)
            {
                await ActiveItemChanged.InvokeAsync(activeItem);
            }
        }

        #endregion

        #region 展开 / 收起（EP openMenu / closeMenu / open / close）

        /// <summary>指定 index 的子菜单是否处于展开态。</summary>
        internal bool IsSubMenuOpened(string index)
            => !string.IsNullOrEmpty(index) && OpenedMenus.Contains(index);

        /// <summary>EP <c>openMenu</c>：展开子菜单（unique-opened 时收起不在该路径上的其余子菜单）。</summary>
        internal async Task NotifyOpenAsync(ElSubMenu item)
        {
            if (item == null) return;

            var index = item.EffectiveIndex;
            if (string.IsNullOrEmpty(index) || OpenedMenus.Contains(index)) return;

            var indexPath = item.GetIndexPath();
            if (UniqueOpened)
            {
                // EP 语义：保留位于当前路径上的祖先，收起其余（同步落到本地展开态）
                foreach (var stale in OpenedMenus.Where(x => !indexPath.Contains(x)).ToList())
                {
                    OpenedMenus.Remove(stale);
                    if (SubMenus.TryGetValue(stale, out var other) && !ReferenceEquals(other, item))
                    {
                        other.ApplyOpenedState(false);
                        other.Refresh();
                    }
                }
            }

            OpenedMenus.Add(index);

            if (OnOpen.HasDelegate)
            {
                await OnOpen.InvokeAsync(new MenuOpenEventArgs { Index = index, IndexPath = indexPath });
            }
        }

        /// <summary>EP <c>closeMenu</c>：收起子菜单。</summary>
        internal async Task NotifyCloseAsync(ElSubMenu item)
        {
            if (item == null) return;

            var index = item.EffectiveIndex;
            if (string.IsNullOrEmpty(index)) return;

            OpenedMenus.Remove(index);

            if (OnClose.HasDelegate)
            {
                await OnClose.InvokeAsync(new MenuOpenEventArgs { Index = index, IndexPath = item.GetIndexPath() });
            }
        }

        internal async Task NotifySelectAsync(ElMenuItem item)
        {
            if (!OnSelect.HasDelegate || item == null)
            {
                return;
            }

            await OnSelect.InvokeAsync(new MenuSelectEventArgs
            {
                Index = item.EffectiveIndex,
                IndexPath = item.GetIndexPath(),
                Route = item.Route,
                Item = item
            });
        }

        /// <summary>EP Exposes <c>open</c>：打开指定 index 的子菜单（沿路径逐级展开）。</summary>
        public async Task OpenAsync(string index)
        {
            if (string.IsNullOrEmpty(index) || !SubMenus.TryGetValue(index, out var subMenu)) return;

            foreach (var ancestor in subMenu.GetIndexPath())
            {
                if (SubMenus.TryGetValue(ancestor, out var parent) && !OpenedMenus.Contains(ancestor))
                {
                    await parent.SetOpenedAsync(true);
                }
            }

            await subMenu.SetOpenedAsync(true);
        }

        /// <summary>EP Exposes <c>close</c>：关闭指定 index 的子菜单。</summary>
        public async Task CloseAsync(string index)
        {
            if (string.IsNullOrEmpty(index) || !SubMenus.TryGetValue(index, out var subMenu)) return;

            await subMenu.SetOpenedAsync(false);
        }

        /// <summary>
        /// EP Exposes <c>handleResize</c>：手动触发菜单宽度重新计算。
        /// 本库未实现 horizontal 的 ellipsis 折叠（需 ResizeObserver 逐项测量），
        /// 这里只做一次重渲染，保证对外 API 完整。
        /// </summary>
        public Task HandleResizeAsync()
        {
            StateHasChanged();
            return Task.CompletedTask;
        }

        #endregion

        #region 兼容 API

        public virtual void ActivateItem(IMenuItem item)
        {
            if (Disabled || item == null || item.Disabled) return;

            var index = item.EffectiveIndex;
            if (string.Equals(ActiveIndex, index, StringComparison.Ordinal)) return;

            SetActiveIndexCore(index);
            item.Activate();
            RefreshItems();

            if (ActiveItemChanged.HasDelegate)
            {
                ActiveItemChanged.InvokeAsync(activeItem);
            }
        }

        public void DeActiveItem()
        {
            if (ActiveIndex == null) return;

            SetActiveIndexCore(string.Empty);
            RefreshItems();
        }

        #endregion

        protected MenuOptions Options { get; set; }

        private bool collapseOnPreviousRender;

        protected override void OnInitialized()
        {
            modeClass = $"el-menu--{Mode.ToString().ToLowerInvariant()}";
            Options = BuildOptions();
            base.OnInitialized();
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            // EP watch(collapse)：折叠时收起全部子菜单
            if (Collapse && !collapseOnPreviousRender && OpenedMenus.Count > 0)
            {
                OpenedMenus.Clear();
                foreach (var subMenu in SubMenus.Values.ToList())
                {
                    subMenu.ApplyOpenedState(false);
                    subMenu.Refresh();
                }
            }
            collapseOnPreviousRender = Collapse;

            modeClass = $"el-menu--{Mode.ToString().ToLowerInvariant()}";
            menuClass = HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add(modeClass, "el-menu", Cls)
                .AddIf(Disabled, "is-disabled")
                .AddIf(Collapse, "el-menu--collapse")
                .AddIf(!CollapseTransition, "el-menu--no-transition")
                .AddIf(Theme == MenuTheme.Dark, "el-menu--dark")
                .ToString();
            menuStyle = HtmlPropertyBuilder.CreateCssStyleBuilder()
                .Add($"--el-menu-level:{Level}")
                .AddIf(Collapse, "--el-menu-level:0")
                .AddIf(!string.IsNullOrWhiteSpace(BackgroundColor), $"--el-menu-bg-color:{BackgroundColor}")
                .AddIf(!string.IsNullOrWhiteSpace(TextColor), $"--el-menu-text-color:{TextColor}")
                .AddIf(!string.IsNullOrWhiteSpace(ActiveTextColor), $"--el-menu-active-color:{ActiveTextColor}")
                // EP use-menu-css-var.ts：hover-text-color 跟随 text-color（否则悬浮时文字跳成主题的 primary 色）。
                .AddIf(!string.IsNullOrWhiteSpace(TextColor), $"--el-menu-hover-text-color:{TextColor}")
                // EP use-menu-color.ts：给了 background-color 才派生 hover-bg-color = 该色 shade(20)
                // （TinyColor 的 shade 是与纯黑按 20% 线性混合，等价于 color-mix 80% + 黑）。
                // 显式 HoverColor 优先；两者都不给就不输出，让类级默认值生效 —— 内核不支持 color-mix 时
                // 这条声明会整条失效、同样回落到类级默认，不会退回"白底白字"。
                .AddIf(!string.IsNullOrWhiteSpace(HoverColor), $"--el-menu-hover-bg-color:{HoverColor}")
                .AddIf(string.IsNullOrWhiteSpace(HoverColor) && !string.IsNullOrWhiteSpace(BackgroundColor),
                    $"--el-menu-hover-bg-color:color-mix(in srgb, {BackgroundColor} 80%, var(--el-color-black))")
                .Add(Style)
                // 折叠态宽度必须压过用户传进来的 Style（内联样式优先级高于 .el-menu--collapse 类规则），
                // 否则 Style="width:220px" 之类的固定宽度会让折叠后宽度不变。
                .AddIf(Collapse, "width:calc(var(--el-menu-icon-width) + var(--el-menu-base-level-padding) * 2)")
                .ToString();
            SyncOptions();
        }

        private bool initializedDefaultState;

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            // 子组件在本次渲染中完成注册，默认激活 / 默认展开只能在此之后落地
            if (!initializedDefaultState && Items.Count + SubMenus.Count > 0)
            {
                initializedDefaultState = true;

                if (!string.IsNullOrEmpty(DefaultActive))
                {
                    SetActiveIndexCore(ResolveActiveIndex(DefaultActive));
                }

                if (DefaultOpeneds != null && !Collapse)
                {
                    foreach (var index in DefaultOpeneds)
                    {
                        if (string.IsNullOrEmpty(index) || OpenedMenus.Contains(index)) continue;
                        OpenedMenus.Add(index);
                        if (SubMenus.TryGetValue(index, out var subMenu))
                        {
                            subMenu.ApplyOpenedState(true);
                            subMenu.Refresh();
                        }
                    }
                }

                RefreshItems();
            }
        }

        private MenuOptions BuildOptions()
        {
            return new MenuOptions
            {
                BackgroundColor = BackgroundColor,
                ActiveTextColor = ActiveTextColor,
                TextColor = TextColor,
                DefaultActiveIndex = DefaultActive,
                HoverColor = HoverColor,
                Mode = Mode,
                Disabled = Disabled,
                Collapse = Collapse,
                Router = Router,
                MenuTrigger = MenuTrigger,
                PopperClass = PopperClass,
                PopperStyle = PopperStyle,
                PopperEffect = PopperEffect,
                ShowTimeout = ShowTimeout,
                HideTimeout = HideTimeout,
                PopperOffset = PopperOffset,
                Theme = Theme,
                Level = Level,
                Ellipsis = Ellipsis
            };
        }

        private void SyncOptions()
        {
            if (Options == null)
            {
                Options = BuildOptions();
                return;
            }

            Options.BackgroundColor = BackgroundColor;
            Options.ActiveTextColor = ActiveTextColor;
            Options.TextColor = TextColor;
            Options.DefaultActiveIndex = DefaultActive;
            Options.HoverColor = HoverColor;
            Options.Mode = Mode;
            Options.Disabled = Disabled;
            Options.Collapse = Collapse;
            Options.Router = Router;
            Options.MenuTrigger = MenuTrigger;
            Options.PopperClass = PopperClass;
            Options.PopperStyle = PopperStyle;
            Options.PopperEffect = PopperEffect;
            Options.ShowTimeout = ShowTimeout;
            Options.HideTimeout = HideTimeout;
            Options.PopperOffset = PopperOffset;
            Options.Theme = Theme;
            Options.Level = Level;
            Options.Ellipsis = Ellipsis;
        }

        protected override bool ShouldRender()
        {
            return true;
        }

        #region 键盘导航（EP 由 Menubar 提供，本库用 C# 实现等价行为）

        protected async Task OnKeyDownAsync(KeyboardEventArgs e)
        {
            if (Disabled)
            {
                return;
            }
            if (e.Key != "ArrowDown" && e.Key != "ArrowRight" && e.Key != "ArrowUp" && e.Key != "ArrowLeft" && e.Key != "Home" && e.Key != "End" && e.Key != "Enter" && e.Key != " ")
            {
                return;
            }
            var enabledItems = Children.Where(x => !x.Disabled).ToList();
            if (!enabledItems.Any())
            {
                return;
            }
            var currentIndex = -1;
            for (var i = 0; i < enabledItems.Count; i++)
            {
                if (IsActiveIndex(enabledItems[i].EffectiveIndex))
                {
                    currentIndex = i;
                    break;
                }
            }
            if (e.Key == "Home")
            {
                currentIndex = 0;
            }
            else if (e.Key == "End")
            {
                currentIndex = enabledItems.Count - 1;
            }
            else if (e.Key == "Enter" || e.Key == " ")
            {
                if (activeItem is ElMenuItem menuItem)
                {
                    await menuItem.OnClickAsync();
                }
                else if (activeItem is ElSubMenu subMenu)
                {
                    await subMenu.ToggleByKeyboardAsync();
                }
                return;
            }
            else
            {
                var isPrev = Mode == MenuMode.Horizontal
                    ? e.Key == "ArrowLeft"
                    : e.Key == "ArrowUp";
                if (Mode == MenuMode.Horizontal && (e.Key == "ArrowUp" || e.Key == "ArrowDown"))
                {
                    return;
                }
                if (Mode == MenuMode.Vertical && (e.Key == "ArrowLeft" || e.Key == "ArrowRight"))
                {
                    return;
                }
                currentIndex = (currentIndex + (isPrev ? -1 : 1) + enabledItems.Count) % enabledItems.Count;
            }
            ActivateItem(enabledItems[currentIndex]);
            if (ActiveItemChanged.HasDelegate)
            {
                await ActiveItemChanged.InvokeAsync(activeItem);
            }
        }

        #endregion
    }
}
