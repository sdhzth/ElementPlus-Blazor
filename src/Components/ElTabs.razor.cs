using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;


using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ElementPlus
{
    public partial class ElTabs : ElementComponentBase
    {
        internal bool headerSizeUpdated = false;
        /// <summary>
        /// 数据源
        /// </summary>
        [Parameter]
        public ObservableCollection<TabOption> DataSource { get; set; }

        /// <summary>
        /// 是否显示增加图标
        /// </summary>
        [Parameter]
        public bool IsAddable { get; set; }

        [Parameter]
        public bool Addable
        {
            get => IsAddable;
            set => IsAddable = value;
        }

        /// <summary>
        /// 是否可关闭
        /// </summary>
        [Parameter]
        public bool IsRemovable { get; set; }

        [Parameter]
        public bool Closable
        {
            get => IsRemovable;
            set => IsRemovable = value;
        }
        /// <summary>
        /// 渲染后的内容区域
        /// </summary>
        public ElementReference Content { get; set; }
        protected ElementReference navScrollElement;

        /// <summary>
        /// Tab 类型
        /// </summary>
        [Parameter]
        public TabType Type { get; set; }

        [Parameter]
        public bool IsEditable { get; set; }

        [Parameter]
        public bool Editable
        {
            get => IsEditable;
            set => IsEditable = value;
        }

        [Parameter]
        public bool IsStretch
        {
            get => Stretch;
            set => Stretch = value;
        }

        [Parameter]
        public bool Stretch { get; set; }

        [Parameter]
        public Func<string, string, Task<bool>> BeforeLeave { get; set; }

        [Parameter]
        public Func<string, string, bool> BeforeLeaveSync { get; set; }

        /// <summary>延迟渲染标签内容（激活时才渲染）</summary>
        [Parameter]
        public bool Lazy { get; set; }

        /// <summary>标签页自定义 class</summary>
        [Parameter]
        public string TabClass { get; set; }

        /// <summary>
        /// 仓库自有参数（EP 2.14.0 的 tabsProps 里没有对应属性）：页签文字对齐。
        /// 默认 null = 不输出任何样式，DOM 与不设时完全一致。
        /// 横向页签栏（Top/Bottom）决定整排页签在栏内的分布，纵向（Left/Right）只决定单个页签内文字的位置。
        /// </summary>
        [Parameter]
        public TabTextAlign? TabTextAlign { get; set; }

        internal bool IsVerticalPosition => TabPosition == TabPosition.Left || TabPosition == TabPosition.Right;

        /// <summary>页签项／页签栏共用的 justify-content 取值；未设置 <see cref="TabTextAlign"/> 时为 null。</summary>
        /// <remarks>成员与枚举同名且成员是可空类型，C# 的 "color-color" 规则不适用，所以枚举成员必须带命名空间前缀。</remarks>
        internal string TextAlignJustify => TabTextAlign switch
        {
            ElementPlus.TabTextAlign.Left => "flex-start",
            ElementPlus.TabTextAlign.Center => "center",
            ElementPlus.TabTextAlign.Right => "flex-end",
            _ => null
        };

        /// <summary>
        /// 页签栏的对齐内联样式。纵向时 <c>.el-tabs__nav</c> 是 <c>flex-direction:column</c>，
        /// <c>justify-content</c> 会变成上下分布、<c>min-width:100%</c> 会撑破栏宽，所以纵向只交给页签项。
        /// </summary>
        internal string NavAlignStyle
        {
            get
            {
                var justify = TextAlignJustify;
                if (justify == null || IsVerticalPosition)
                {
                    return null;
                }

                // 实测（2026-10，/tabs）：vendored CSS 里 .el-tabs__nav 是 display:flex 但 float:left 没被后面的
                // 规则复位，宽度因此收缩到内容宽（1034px 的容器里只有 220px）——不补 min-width 就无从"居中/靠右"。
                return $"min-width:100%;justify-content:{justify}";
            }
        }

        [Parameter]
        public string ModelValue { get; set; }

        [Parameter]
        public string Value
        {
            get => ModelValue;
            set => ModelValue = value;
        }

        [Parameter]
        public EventCallback<string> ModelValueChanged { get; set; }

        [Parameter]
        public EventCallback<string> ValueChanged
        {
            get => ModelValueChanged;
            set => ModelValueChanged = value;
        }

        [Parameter]
        public TabPosition TabPosition { get; set; }
        private List<ElTabPane> tabPanels { get; set; } = new List<ElTabPane>();

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Tab 页被切换后触发
        /// </summary>
        [Parameter]
        public EventCallback<ElementChangeEventArgs<ElTabPane>> OnActiveTabChanged { get; set; }

        /// <summary>
        /// Tab 页被切换前触发
        /// </summary>
        [Parameter]
        public EventCallback<ElementChangeEventArgs<ElTabPane>> OnActiveTabChanging { get; set; }

        /// <summary>
        /// Tab 页被关闭后触发
        /// </summary>
        [Parameter]
        public EventCallback<ElTabPane> OnTabClose { get; set; }

        /// <summary>
        /// Tab 页被关闭时触发
        /// </summary>
        [Parameter]
        public EventCallback<ElementClosingEventArgs<ElTabPane>> OnTabClosing { get; set; }

        /// <summary>
        /// 官方 tab-click：只要点中页签就触发（切换被 before-leave 拦截、或点的是当前页签也会触发）。
        /// 参数为 (pane, ev)，其中 ev 可能为 null（键盘导航）。
        /// </summary>
        [Parameter]
        public EventCallback<TabClickEventArgs> OnTabClick { get; set; }

        /// <summary>
        /// 官方 edit：新增/删除页签时触发，Action 为 "add" 或 "remove"。
        /// </summary>
        [Parameter]
        public EventCallback<TabEditEventArgs> OnEdit { get; set; }

        /// <summary>官方 exposes：当前激活页签的 name</summary>
        public string CurrentName => ModelValue;

        internal async Task NotifyTabClickAsync(ElTabPane pane, MouseEventArgs e)
        {
            if (!OnTabClick.HasDelegate)
            {
                return;
            }

            await OnTabClick.InvokeAsync(new TabClickEventArgs { Pane = pane, MouseEventArgs = e });
        }

        internal async Task NotifyEditAsync(string paneName, string action)
        {
            if (!OnEdit.HasDelegate)
            {
                return;
            }

            await OnEdit.InvokeAsync(new TabEditEventArgs { PaneName = paneName, Action = action });
        }

        internal async Task CloseTabAsync(ElTabPane tab)
        {
            var previousActive = ActiveTab?.Name;
            if (OnTabClosing.HasDelegate)
            {
                var arg = new ElementClosingEventArgs<ElTabPane>();
                arg.Target = tab;
                await OnTabClosing.InvokeAsync(arg);
                if (arg.Cancel)
                {
                    return;
                }
            }

            if (DataSource == null)
            {
                CloseHardcodedTab(tab);
            }
            else
            {
                ResetActiveTab(tab);
            }
            if (!string.Equals(previousActive, ActiveTab?.Name, StringComparison.Ordinal)
                && ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(ModelValue);
            }
            RequireRender = true;
            await NotifyEditAsync(tab.Name, "remove");
            if (OnTabClose.HasDelegate)
            {
                _ = OnTabClose.InvokeAsync(tab);
            }
            else
            {
                StateHasChanged();
            }
        }

        private readonly HashSet<string> closedTabNames = new HashSet<string>();

        /// <summary>
        /// 硬编码模式下该页签是否已被关掉。<b>DataSource 模式恒为 false</b>：那种模式下删除的是数据，
        /// 同名页签可以再被加回来，记住名字只会把它误藏掉。
        /// </summary>
        internal bool IsTabClosed(string name)
        {
            return DataSource == null && !string.IsNullOrEmpty(name) && closedTabNames.Contains(name);
        }

        /// <summary>
        /// 硬编码 <c>&lt;ElTabPane&gt;</c> 模式下关掉页签。EP 那边是 <c>tabs.tsx:182</c> 的
        /// <c>handleTabRemove</c>：只发 <c>edit(remove)</c> + <c>tab-remove</c>，页签本身由使用方从数组里
        /// splice 掉。Blazor 里写死的子标记没法由使用方删（重渲染又会渲染出来），所以这里由组件自己
        /// 把它从页签集合摘掉并记住名字，之后任何一次重渲染都不会让它再冒出来。
        /// </summary>
        private void CloseHardcodedTab(ElTabPane tab)
        {
            closedTabNames.Add(tab.Name);
            var index = tabPanels.IndexOf(tab);
            tabPanels.Remove(tab);
            ReactivateAfterRemoval(tab, index);
        }

        /// <summary>
        /// 页签 <c>Visible=false</c>：把它从页签集合里摘掉。<b>与 <see cref="CloseHardcodedTab"/> 的区别只在于
        /// 不记名字</b>——关闭是「用户关掉了，重渲染也不许它再冒出来」，隐藏是「按状态收起，条件一变就要回来」，
        /// 所以隐藏走 <see cref="ShowTab"/> 原路注册回来。
        /// </summary>
        internal void HideTab(ElTabPane tab)
        {
            var index = tabPanels.IndexOf(tab);
            if (index < 0)
            {
                return;
            }
            tabPanels.Remove(tab);
            if (ReactivateAfterRemoval(tab, index))
            {
                // 内容区（.el-tabs__content）渲染的是 ActiveTab.ChildContent，属于本组件的渲染树，
                // 页签头自己藏掉不会带着它一起变 ⇒ 激活项被顶掉时必须让本组件重渲染一次。
                RequireRender = true;
                StateHasChanged();
            }
        }

        /// <summary>页签 <c>Visible</c> 由 false 翻回 true：重新入集合（已关闭过的名字不再放回来）。</summary>
        internal void ShowTab(ElTabPane tab)
        {
            if (Exists(tab.Name) || IsTabClosed(tab.Name))
            {
                return;
            }
            tabPanels.Add(tab);
            // 一个激活项都没有（全部页签曾被收起）时，靠 OnAfterRenderAsync 的「挑第一个」兜底把它选成激活项。
            RequireRender = true;
        }

        /// <summary>
        /// 从页签集合摘掉一项之后的收尾：若摘掉的正是激活项，顺延到同一个位置上的下一个可用页签。
        /// 返回是否发生了激活项变更。
        /// </summary>
        private bool ReactivateAfterRemoval(ElTabPane tab, int index)
        {
            // 已摘掉的页签不能再自称激活：它不在集合里，后面那段「给集合里的每一项置激活态」碰不到它，
            // 留着脏标记会让页签翻回可见、重新注册时被 OnAfterRenderAsync 误选成当前页签。
            tab.DeActivate();
            if (!ReferenceEquals(ActiveTab, tab))
            {
                return false;
            }
            var enabled = tabPanels.Where(x => !x.Disabled).ToList();
            var next = enabled.Count == 0 ? null : enabled[Math.Min(Math.Max(index, 0), enabled.Count - 1)];
            if (next == null)
            {
                ActiveTab = null;
                ModelValue = null;
                return true;
            }
            // 摘掉当前页签后的"被动切换"刻意不再过 before-leave／OnActiveTabChanging：被摘的那个页签
            // 此时已经不存在了，拦下来只会让 ActiveTab 停在一个已摘掉的页签上（内容区跟着一起空白）。
            foreach (var pane in tabPanels)
            {
                if (ReferenceEquals(pane, next))
                {
                    pane.Activate();
                }
                else
                {
                    pane.DeActivate();
                }
            }
            ActiveTab = next;
            ModelValue = next.Name;
            next.Refresh();
            return true;
        }

        private TabOption ResetActiveTab(ElTabPane tab)
        {
            if (DataSource == null)
            {
                return null;
            }
            if (DataSource.Count <= 0)
            {
                return null;
            }
            var activeOption = DataSource.FirstOrDefault(x => x.IsActive);
            var activeIndex = DataSource.IndexOf(activeOption);
            if (activeOption.Title != tab.Title)
            {
                var removingIndex = DataSource.IndexOf(DataSource.FirstOrDefault(x => x.Name == tab.Name));
                DataSource.RemoveAt(removingIndex);
                if (activeIndex > removingIndex)
                {
                    activeIndex--;
                }
                return activeOption;
            }
            DataSource.RemoveAt(activeIndex);
            var newActiveIndex = activeIndex;
            if (newActiveIndex >= DataSource.Count - 1)
            {
                newActiveIndex = DataSource.Count - 1;
            }
            if (newActiveIndex == -1)
            {
                return null;
            }
            activeOption = DataSource.ElementAt(newActiveIndex);
            activeOption.IsActive = true;
            return activeOption;
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            if (DataSource == null)
            {
                if (IsAddable || IsEditable)
                {
                    throw new ElementException("标签页组件启用可编辑模式时必须指定 DataSource 属性，硬编码无效");
                }
            }
            else
            {
                var activeTab = DataSource.FirstOrDefault(x => x.IsActive);
                if (activeTab == null)
                {
                    activeTab = DataSource.FirstOrDefault();
                    if (activeTab != null)
                    {
                        activeTab.IsActive = true;
                    }
                }
                DataSource.CollectionChanged -= DataSource_CollectionChanged;
                DataSource.CollectionChanged += DataSource_CollectionChanged;
            }
        }

        private void DataSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add)
            {
                return;
            }
            foreach (var item in DataSource.Take(DataSource.Count - 1))
            {
                item.IsActive = false;
            }
            var repeatKeys = DataSource.GroupBy(x => x.Name).Where(x => x.Count() > 1).Select(x => x.Key).ToArray();
            if (repeatKeys.Any())
            {
                throw new ElementException($"Tab 页以下 Name 重复 {string.Join(",", repeatKeys)}");
            }
        }

        /// <summary>
        /// 点击加号按钮增加 Tab 页时触发
        /// </summary>
        [Parameter]
        public EventCallback<MouseEventArgs> OnAddingTab { get; set; }

        internal ElTabPane ActiveTab { get; private set; }

        internal float BarOffsetLeft { get; set; }
        internal float BarWidth { get; set; }
        internal void AddTab(ElTabPane tab)
        {
            if (Exists(tab.Name) || IsTabClosed(tab.Name))
            {
                return;
            }

            tabPanels.Add(tab);
        }

        internal void RemoveTab(string name)
        {
            tabPanels.Remove(tabPanels.FirstOrDefault(x => x.Name == name));
        }

        internal bool Exists(string name)
        {
            if (tabPanels.Any(x => x.Name == name))
            {
                return true;
            }
            return false;
        }

        internal (string headerPosition, string tabPosition) GetPosition()
        {
            var headerPosition = string.Empty;
            var tabPosition = string.Empty;
            switch (TabPosition)
            {
                case TabPosition.Top:
                    tabPosition = "el-tabs--top";
                    headerPosition = "is-top";
                    break;
                case TabPosition.Bottom:
                    tabPosition = "el-tabs--bottom";
                    headerPosition = "is-bottom";
                    break;
                case TabPosition.Left:
                    tabPosition = "el-tabs--left";
                    headerPosition = "is-left";
                    break;
                case TabPosition.Right:
                    tabPosition = "el-tabs--right";
                    headerPosition = "is-right";
                    break;
            }
            return (headerPosition, tabPosition);
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            var activeTabOption = DataSource?.FirstOrDefault(x => x.IsActive);
            var activeTab = tabPanels.FirstOrDefault(x => x.IsActive);

            // 首次渲染时子 TabPane 尚未注册，OnParametersSet 里无法应用 ModelValue；
            // 这里补一次：优先按 ModelValue 定位初始激活页签（对齐官方 model-value 语义）。
            if (activeTab == null && !string.IsNullOrWhiteSpace(ModelValue))
            {
                activeTab = tabPanels.FirstOrDefault(x => x.Name == ModelValue);
            }

            if (activeTab == null)
            {
                activeTab = tabPanels.FirstOrDefault();
                if (activeTab != null)
                {
                    activeTab.Activate();
                    activeTab.Refresh();
                }
                else if (activeTabOption == null)
                {
                    if (firstRender)
                    {
                        Refresh();
                    }
                }
                else
                {
                    foreach (var item in tabPanels)
                    {
                        if (item.Name != activeTabOption.Name)
                        {
                            continue;
                        }
                        item.Refresh();
                    }
                }
            }
            if (!firstRender && !RequireRender)
            {
                return;
            }
            // ActiveTab 还是 null ⇒ 这一步选的是"初始激活项"，不是"从某个页签离开"，所以不查 before-leave。
            await SetActivateTabAsync(activeTab, ActiveTab == null);
            await base.OnAfterRenderAsync(firstRender);
        }

        internal async Task UpdateHeaderSizeAsync(ElTabPane tabPanel, float barWidth, float barOffsetLeft)
        {
            if (headerSizeUpdated)
            {
                await TabRenderCompletedAsync(tabPanel);
                return;
            }
            headerSizeUpdated = true;
            BarWidth = barWidth;
            BarOffsetLeft = barOffsetLeft;
            RequireRender = true;
            StateHasChanged();
        }
        internal async Task TabRenderCompletedAsync(ElTabPane tabPanel)
        {
            if (OnRenderCompleted != null)
            {
                await OnRenderCompleted(tabPanel);
            }
        }

        public async Task SetActivateTabAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ActiveTab = null;
                return;
            }
            var tab = tabPanels.FirstOrDefault(x => x.Name == name);
            if (tab == null)
            {
                ExceptionHelper.Throw(ExceptionHelper.TabNameNotFound, $"Name 为 {name} 的 Tab 不存在");
            }
            await SetActivateTabAsync(tab);
        }
        internal async Task<bool> SetActivateTabAsync(ElTabPane tab, bool skipBeforeLeave = false)
        {
            if (tab == null || tab.Disabled)
            {
                return false;
            }
            // 对齐 EP tabs.tsx:150 的 setCurrentName：目标名与当前激活项相同就直接返回，既不重测表头，
            // 也不重发 tab-change / update:modelValue。缺这一条时 OnAfterRenderAsync 每轮渲染都会重新
            // 激活同一个页签，回调里一句 StateHasChanged 就能自己喂出无限渲染（宿主实测：25 秒窗口
            // evaluate 全超时、主线程占死，页面连首次后端查询都没轮到发出去）。
            if (ActiveTab == tab || string.Equals(ActiveTab?.Name, tab.Name, StringComparison.Ordinal))
            {
                return ActiveTab != null;
            }
            if (OnActiveTabChanging.HasDelegate)
            {
                var arg = new ElementChangeEventArgs<ElTabPane>();
                arg.NewValue = tab;
                arg.OldValue = ActiveTab;
                await OnActiveTabChanging.InvokeAsync(arg);
                if (arg.DisallowChange)
                {
                    return false;
                }
            }
            // EP 只在"切换"时查 before-leave（tabs.tsx:140 的 setCurrentName），初始化选中第一个页签
            // 不走它。这里如果也查，一个"脏数据时一律不放行"的回调会把首个页签一起拦掉：
            // oldName 是 null、返回 false，结果整条页签栏没有任何激活项、内容区空白。
            if (!skipBeforeLeave)
            {
                if (BeforeLeave != null)
                {
                    var oldName = ActiveTab?.Name;
                    var allowLeave = await BeforeLeave(tab.Name, oldName);
                    if (!allowLeave)
                    {
                        return false;
                    }
                }
                if (BeforeLeaveSync != null)
                {
                    var oldName = ActiveTab?.Name;
                    if (!BeforeLeaveSync(tab.Name, oldName))
                    {
                        return false;
                    }
                }
            }
            if (DataSource == null)
            {
                foreach (var tabPanel in tabPanels)
                {
                    if (tabPanel == tab)
                    {
                        tabPanel.Activate();
                        continue;
                    }
                    tabPanel.DeActivate();
                }
            }
            else
            {
                foreach (var item in DataSource)
                {
                    item.IsActive = item.Name == tab.Name;
                }
            }
            var eventArgs = new ElementChangeEventArgs<ElTabPane>();
            eventArgs.OldValue = ActiveTab;
            eventArgs.NewValue = tab;
            ActiveTab = tab;
            ModelValue = tab.Name;
            RequireRender = true;
            if (OnActiveTabChanged.HasDelegate)
            {
                await OnActiveTabChanged.InvokeAsync(eventArgs);
            }
            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(tab.Name);
            }
            else
            {
                StateHasChanged();
            }
            return true;
        }

        protected async Task OnKeyDownAsync(KeyboardEventArgs e)
        {
            if (e.Key != "ArrowRight" && e.Key != "ArrowDown" && e.Key != "ArrowLeft" && e.Key != "ArrowUp" && e.Key != "Home" && e.Key != "End")
            {
                return;
            }
            var enabledTabs = tabPanels.Where(x => !x.Disabled).ToList();
            if (!enabledTabs.Any())
            {
                return;
            }
            var currentIndex = ActiveTab == null ? -1 : enabledTabs.IndexOf(ActiveTab);
            if (e.Key == "Home")
            {
                currentIndex = 0;
            }
            else if (e.Key == "End")
            {
                currentIndex = enabledTabs.Count - 1;
            }
            else
            {
                var isVertical = TabPosition == TabPosition.Left || TabPosition == TabPosition.Right;
                if (!isVertical && (e.Key == "ArrowUp" || e.Key == "ArrowDown"))
                {
                    return;
                }
                if (isVertical && (e.Key == "ArrowLeft" || e.Key == "ArrowRight"))
                {
                    return;
                }
                var step = e.Key == "ArrowLeft" || e.Key == "ArrowUp" ? -1 : 1;
                currentIndex = (currentIndex + step + enabledTabs.Count) % enabledTabs.Count;
            }
            await SetActivateTabAsync(enabledTabs[currentIndex]);
        }

        protected override void OnParametersSet()
        {
            if (Type == TabType.Normal && IsRemovable && DataSource != null)
            {
                throw new NotSupportedException("TabType为Card的情况下才能进行移除");
            }
            base.OnParametersSet();
            // 仅在“目标值与当前激活页不一致”时才同步激活。
            // 否则配合 @bind-Value 使用时：SetActivateTabAsync 内部会回写 ModelValue 并触发
            // ModelValueChanged → 父组件重渲染 → 再次进入 OnParametersSet，形成无限渲染循环（页面卡死）。
            // 同时要求 tabPanels 已就绪，避免首次 OnParametersSet（子 TabPane 尚未注册）时误报 Tab 不存在。
            if (!string.IsNullOrWhiteSpace(ModelValue)
                && tabPanels.Count > 0
                && !string.Equals(ActiveTab?.Name, ModelValue, StringComparison.Ordinal))
            {
                _ = SetActivateTabAsync(ModelValue);
            }
        }

        /// <summary>新增页签按钮点击（同时派发官方 edit 事件，Action="add"）</summary>
        protected async Task OnInternalAddingTabAsync(MouseEventArgs e)
        {
            await NotifyEditAsync(ModelValue, "add");
            if (OnAddingTab.HasDelegate)
            {
                await OnAddingTab.InvokeAsync(e);
            }
        }
    }

    /// <summary>官方 tab-click 事件参数：被点击的页签 + 原始鼠标事件</summary>
    public class TabClickEventArgs
    {
        public ElTabPane Pane { get; set; }

        public MouseEventArgs MouseEventArgs { get; set; }
    }

    /// <summary>官方 edit 事件参数：页签名 + 动作（add / remove）</summary>
    public class TabEditEventArgs
    {
        public string PaneName { get; set; }

        public string Action { get; set; }
    }
}
