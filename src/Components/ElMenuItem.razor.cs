
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 菜单项（Element Plus 自渲染版）。对齐 EP <c>el-menu-item</c>：
    /// 属性 index / route / disabled；事件 click；插槽 default / title。
    /// 颜色一律交由 <c>--el-menu-*</c> CSS 变量与 <c>is-active</c> / <c>is-disabled</c> 修饰类处理，
    /// 不再写内联颜色（内联样式会压过 <c>:hover</c> 规则）。
    /// </summary>
    public partial class ElMenuItem : ElementComponentBase, IMenuItem
    {
        #region 参数

        /// <summary>EP <c>index</c>：唯一标志。</summary>
        [Parameter]
        public string Index { get; set; }

        /// <summary>实际使用的唯一标志：未显式指定 Index 时回退到 Route。</summary>
        public string EffectiveIndex => !string.IsNullOrWhiteSpace(Index) ? Index : Route;

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>EP <c>disabled</c>：是否禁用。</summary>
        [Parameter]
        public bool Disabled { get; set; } = false;

        /// <summary>EP <c>route</c>：Vue Route 路由位置参数（本库为 Blazor 路径）。</summary>
        [Parameter]
        public string Route { get; set; }

        [Parameter]
        public object Model { get; set; }

        /// <summary>图标类名。EP 无该属性（EP 直接把图标放进默认插槽），保留以兼容既有调用方；默认不渲染。</summary>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>EP <c>title</c> 插槽的文本形式：折叠时作为原生 tooltip。</summary>
        [Parameter]
        public string Title { get; set; }

        /// <summary>EP <c>title</c> 插槽（RenderFragment 形式）。</summary>
        [Parameter]
        public RenderFragment TitleTemplate { get; set; }

        /// <summary>EP MenuItem 事件 <c>click</c>：点击菜单项时回调。</summary>
        [Parameter]
        public EventCallback<MenuSelectEventArgs> OnClick { get; set; }

        /// <summary>路由变更前的确认回调（仓库自有）。</summary>
        [Parameter]
        public EventCallback<ElementChangeEventArgs<string>> OnRouteChanging { get; set; }

        #endregion

        [CascadingParameter]
        public ElMenu TopMenu { get; set; }
        [CascadingParameter]
        public ElMenuContainer Menu { get; set; }

        [CascadingParameter]
        public ElSubMenu ParentMenu { get; set; }
        [CascadingParameter]
        public MenuOptions Options { get; set; }

        #region 兼容属性（颜色改由 CSS 变量承载，保留属性不影响既有调用方）

        public virtual string TextColor { get; set; }
        public virtual string BorderColor { get; set; }
        public virtual string BackgroundColor { get; set; }

        /// <summary>
        /// 本地激活标记：仅在没有根菜单（脱离 ElMenu 单独使用）时生效，
        /// 正常情况下以 <see cref="ElMenu.ActiveIndex"/> 为唯一真源。
        /// </summary>
        public virtual bool IsActive { get; set; }

        #endregion

        private string currentRoute = string.Empty;

        /// <summary>是否激活：以根菜单的 ActiveIndex 为唯一真源。</summary>
        protected bool IsActiveNow => TopMenu != null ? TopMenu.IsActiveIndex(EffectiveIndex) : IsActive;

        protected string ItemClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-menu-item", Cls)
            .AddIf(IsActiveNow, "is-active")
            .AddIf(Disabled || (Options != null && Options.Disabled), "is-disabled")
            .ToString();

        protected bool EffectiveCollapse => Options?.Collapse == true;

        protected string ItemStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add($"--el-menu-level:{Level}")
            .ToString();

        /// <summary>本项所在的层级（根菜单下为 0，一级子菜单内为 1……）。</summary>
        protected int Level => ParentMenu == null ? (Options?.Level ?? 0) : ParentMenu.ChildLevel;

        /// <summary>折叠态下作为 tooltip 的标题文本。</summary>
        protected string EffectiveTitle => EffectiveCollapse ? Title : null;

        #region ellipsis 截断悬停 tooltip（本库扩展）

        private ElementReference textElement;
        private string ellipsisTooltipText;
        private bool ellipsisTooltipVisible;
        private string ellipsisPopperStyle;
        private string ellipsisPopperPlacement;

        /// <summary>
        /// 是否为「被 ellipsis 截断的文字」挂 elTooltip 悬停显示全文。
        /// 折叠态排除：EP 在折叠态用自己的 tooltip（<c>menu-item.vue:12-31</c>，条件 <c>collapse &amp;&amp; $slots.title</c>），
        /// 本库折叠态沿用原生 <c>title</c>（见 <see cref="EffectiveTitle"/>），且折叠时文字 span 被 CSS 收成 0×0，
        /// 再挂一层浮层只会互相遮挡。
        /// </summary>
        protected bool ShowEllipsisTooltip => Options?.Ellipsis == true && !EffectiveCollapse;

        protected bool EllipsisTooltipVisible => ellipsisTooltipVisible;

        protected string EllipsisTooltipText => ellipsisTooltipText;

        /// <summary>竖向菜单浮在文字右侧（EP 折叠态 tooltip 即 <c>placement="right"</c>，<c>menu-item.vue:20</c>）；横向菜单朝下，避免压住相邻项。</summary>
        protected string EllipsisTooltipPlacement => Options?.Mode == MenuMode.Horizontal ? "bottom" : "right";

        /// <summary>与子菜单弹层同一主题口径（<c>popper-effect</c>），未指定时用菜单默认的 light。</summary>
        protected string EllipsisTooltipEffect => string.IsNullOrWhiteSpace(Options?.PopperEffect) ? "light" : Options.PopperEffect;

        /// <summary>钉成 fixed 的浮层坐标（口径见 <see cref="TextClip.BuildFixedPopperStyle"/>），加上用户自己的
        /// <c>popper-style</c> 让它后声明取胜。未截断时为 null。</summary>
        protected string EllipsisTooltipPopperStyle => ellipsisPopperStyle == null
            ? Options?.PopperStyle
            : (string.IsNullOrWhiteSpace(Options?.PopperStyle) ? ellipsisPopperStyle : ellipsisPopperStyle + ";" + Options.PopperStyle);

        /// <summary>实际 placement：用户给了 <c>popper-placement</c> 就用用户的，否则竖向 right / 横向 bottom。</summary>
        protected string EllipsisTooltipPopperPlacement => ellipsisPopperPlacement ?? EllipsisTooltipPlacement;

        /// <summary>进入整行时测算：只有真的被截断才浮出 tooltip（绑在 li 上，鼠标停在行内空白处也能看到）。</summary>
        protected async Task OnMouseEnterAsync()
        {
            if (!ShowEllipsisTooltip)
            {
                return;
            }

            var state = await TextClip.MeasureClippedStateAsync(JSRuntime, textElement);
            ellipsisTooltipText = state == null ? null : TextClip.ExtractText(state);
            ellipsisTooltipVisible = ellipsisTooltipText != null;
            if (!ellipsisTooltipVisible)
            {
                ellipsisPopperStyle = null;
                ellipsisPopperPlacement = null;
                return;
            }

            ellipsisPopperStyle = TextClip.BuildFixedPopperStyle(state, EllipsisTooltipPlacement, out var placement);
            ellipsisPopperPlacement = placement;
        }

        protected void OnMouseLeave()
        {
            ellipsisTooltipVisible = false;
            ellipsisTooltipText = null;
            ellipsisPopperStyle = null;
            ellipsisPopperPlacement = null;
        }

        #endregion

        public void Activate()
        {
            if (Options != null && Options.Disabled)
            {
                return;
            }

            IsActive = true;
            TopMenu?.SetActiveIndexCore(EffectiveIndex);
        }

        public void DeActivate()
        {
            IsActive = false;
        }

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

        protected override void OnInitialized()
        {
            base.OnInitialized();
            TopMenu?.RegisterItem(this);
            Menu?.AddMenuItem(this);
            SyncActiveFromState();
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            SyncActiveFromState();
        }

        /// <summary>
        /// 命中 <c>default-active</c> 或当前路由时置为激活（EP 由 watch(defaultActive) + router 负责）。
        /// </summary>
        private void SyncActiveFromState()
        {
            if (TopMenu == null || TopMenu.IsActiveIndex(EffectiveIndex)) return;

            var matchFunc = TopMenu.Match ?? DefaultMenuMatcher;
            var matched = (!string.IsNullOrWhiteSpace(TopMenu.DefaultActive) && TopMenu.DefaultActive == EffectiveIndex)
                          || matchFunc(Route);

            if (matched)
            {
                TopMenu.SetActiveIndexCore(EffectiveIndex);
            }
        }

        private bool DefaultMenuMatcher(string route)
        {
            if (string.IsNullOrWhiteSpace(Route))
            {
                return false;
            }
            var uri = new Uri(NavigationManager.Uri);
            var paths = uri.LocalPath.Split('/').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            var menuPaths = route.Split('/').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (paths.Length != menuPaths.Length)
            {
                return false;
            }
            for (int i = 0; i < paths.Length; i++)
            {
                if (paths[i].ToUpper() != menuPaths[i].ToUpper())
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>悬浮：仅用于保持弹出式子菜单不被关闭（颜色交给 CSS :hover）。</summary>
        public void OnOver()
        {
            if (Disabled || (Options != null && Options.Disabled))
            {
                return;
            }
            if ((Options?.Mode == MenuMode.Horizontal || Options?.Collapse == true) && ParentMenu != null)
            {
                ParentMenu.KeepSubMenuOpen();
            }
        }

        protected override bool ShouldRender()
        {
            return true;
        }

        public async Task OnClickAsync()
        {
            if (Disabled || (Options != null && Options.Disabled))
            {
                return;
            }
            if (ParentMenu != null && (TopMenu?.Mode == MenuMode.Horizontal || Options?.Collapse == true))
            {
                await ParentMenu.CloseAsync();
            }

            if (TopMenu != null)
            {
                await TopMenu.SelectItemAsync(this);
            }
            else
            {
                IsActive = true;
            }

            if (OnClick.HasDelegate)
            {
                await OnClick.InvokeAsync(new MenuSelectEventArgs
                {
                    Index = EffectiveIndex,
                    IndexPath = GetIndexPath(),
                    Route = Route,
                    Item = this
                });
            }

            if (!string.IsNullOrEmpty(Route))
            {
                if (OnRouteChanging.HasDelegate)
                {
                    var arg = new ElementChangeEventArgs<string>();
                    arg.NewValue = Route;
                    arg.OldValue = currentRoute;
                    await OnRouteChanging.InvokeAsync(arg);
                    if (arg.DisallowChange)
                    {
                        return;
                    }
                }
                if (Options == null || Options.Router)
                {
                    // Route 以 "/" 开头时按「应用内路径」语义处理：Blazor 的 NavigateTo 对根相对
                    // 路径按站点根解析（Uri 语义），部署在子路径下（如 GitHub Pages /repo/）会丢
                    // base href 导致跳到站外 404。去掉首斜杠按 base 相对路径导航，
                    // 根路径与子路径两种部署形态都正确。
                    NavigationManager.NavigateTo(Route.TrimStart('/'));
                    NavigationManager.LocationChanged += NavigationManager_LocationChanged;
                }
            }
        }

        private void NavigationManager_LocationChanged(object sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            NavigationManager.LocationChanged -= NavigationManager_LocationChanged;
            ParentMenu?.MarkAsRequireRender();
            TopMenu?.MarkAsRequireRender();
            TopMenu?.UpdateActiveIndex(EffectiveIndex);
        }

        protected async Task OnKeyDownAsync(KeyboardEventArgs e)
        {
            if (e.Key != "Enter" && e.Key != " ")
            {
                return;
            }
            await OnClickAsync();
        }

        // 归一口径与 <ElIcon Name="..."> 完全一致（补前缀 + kebab + 字体别名），
        // 否则同一个 u_icon 在两个组件里一个有图、一个空白格。
        private static string NormalizeIcon(string icon) => ElementIconName.ToClass(icon);

        /// <summary>图标类名（已补全 el-icon- 前缀）。</summary>
        protected string EffectiveIcon => NormalizeIcon(Icon);

        public override void Dispose()
        {
            base.Dispose();
            TopMenu?.UnregisterItem(this);
            Menu?.RemoveMenuItem(this);
        }
    }
}
