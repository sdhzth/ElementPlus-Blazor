using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 页签导航（ProTabs 风格，Element Plus 自渲染版）。
    /// <para>
    /// 用于后台布局顶部/底部的「多标签页」条：圆点（或图标）+ 标题 + 关闭按钮的胶囊页签，
    /// 溢出时出现左右滚动箭头，右侧有「更多」下拉（列出全部已打开标签 + 关闭其他/关闭全部）。
    /// 视觉对齐 teek-design-vue3 的 <c>classic-tabs-nav</c>（simple 变体）。
    /// </para>
    /// <para>
    /// 设计取向：**组件只管展示与抛事件**。<see cref="Items"/> 由宿主维护（打开/关闭/持久化），
    /// 组件通过 <see cref="OnSelect"/> / <see cref="OnClose"/> / <see cref="OnCloseOthers"/> /
    /// <see cref="OnCloseAll"/> 通知宿主；路由跳转、localStorage 持久化都在宿主侧。
    /// </para>
    /// <para>
    /// 需要 &lt;ElPopup /&gt; 之外的额外宿主？不需要 —— 滚动量测量与「更多」面板的点击外部关闭
    /// 都由 <c>elementProTabs*</c> JS 通道完成（<c>_content/Element-Plus/js/dom.js</c> 随库引入）。
    /// </para>
    /// </summary>
    public partial class ElProTabsGrow : ElementComponentBase
    {
        #region 参数

        /// <summary>页签集合。宿主负责去重与顺序；<c>Path</c> 为空的项会被忽略。</summary>
        [Parameter]
        public IEnumerable<ElProTabsGrowItem> Items { get; set; }

        /// <summary>当前激活页签的 <see cref="ElProTabsGrowItem.Path"/>，支持 <c>@bind-ActivePath</c>。</summary>
        [Parameter]
        public string ActivePath { get; set; }

        /// <summary><see cref="ActivePath"/> 的双向绑定回调。</summary>
        [Parameter]
        public EventCallback<string> ActivePathChanged { get; set; }

        /// <summary>
        /// 激活页签的文字颜色。任意 CSS 颜色；不设则回落
        /// <c>--el-color-primary</c>（内置兜底 <c>#409eff</c>）。
        /// 也可用 <see cref="SetActiveTextColor"/> 在运行时改。
        /// </summary>
        [Parameter]
        public string ActiveTextColor { get; set; }

        /// <summary>激活页签圆点颜色；不设则回落 <c>--el-color-primary-light-3</c>。</summary>
        [Parameter]
        public string ActiveDotColor { get; set; }

        /// <summary>全局关闭按钮开关，默认 <c>true</c>。单项可用 <see cref="ElProTabsGrowItem.Closable"/> 再收紧。</summary>
        [Parameter]
        public bool Closable { get; set; } = true;

        /// <summary>关闭按钮显示时机，默认 <see cref="ElProTabsGrowCloseButtonDisplay.Hover"/>（悬停该页签才显示）。</summary>
        [Parameter]
        public ElProTabsGrowCloseButtonDisplay CloseButtonDisplay { get; set; } = ElProTabsGrowCloseButtonDisplay.Hover;

        /// <summary>没有图标时是否显示前置圆点，默认 <c>true</c>。</summary>
        [Parameter]
        public bool ShowDot { get; set; } = true;

        /// <summary>有图标时是否显示图标（优先于圆点），默认 <c>true</c>。</summary>
        [Parameter]
        public bool ShowIcon { get; set; } = true;

        /// <summary>是否显示右侧「更多」下拉，默认 <c>true</c>。</summary>
        [Parameter]
        public bool ShowMore { get; set; } = true;

        /// <summary>「更多」按钮文案，默认「更多」。</summary>
        [Parameter]
        public string MoreText { get; set; } = "更多";

        /// <summary>溢出时是否显示左右滚动箭头，默认 <c>true</c>。设为 <c>false</c> 时仍可用触摸板/滚轮横向滚动。</summary>
        [Parameter]
        public bool ShowScrollButtons { get; set; } = true;

        /// <summary>追加到每个页签项上的 CSS 类。</summary>
        [Parameter]
        public string ItemCls { get; set; }

        /// <summary>追加到每个页签项上的内联样式。</summary>
        [Parameter]
        public string ItemStyle { get; set; }

        /// <summary>页签被点击（含已被激活的页签被再次点击，便于宿主实现「点击当前页刷新」）。</summary>
        [Parameter]
        public EventCallback<ElProTabsGrowItem> OnSelect { get; set; }

        /// <summary>某个页签的关闭按钮被点击。</summary>
        [Parameter]
        public EventCallback<ElProTabsGrowItem> OnClose { get; set; }

        /// <summary>「更多 → 关闭其他」被点击（参数为触发时的当前页签）。</summary>
        [Parameter]
        public EventCallback<ElProTabsGrowItem> OnCloseOthers { get; set; }

        /// <summary>「更多 → 关闭全部」被点击。</summary>
        [Parameter]
        public EventCallback OnCloseAll { get; set; }

        /// <summary>
        /// 是否启用页签右键菜单（刷新 / 关闭 / 关闭左侧 / 关闭右侧 / 关闭其他 / 关闭全部），
        /// 默认 <c>true</c>。「关闭左侧/右侧」按被右键页签的位置自动禁用。
        /// </summary>
        [Parameter]
        public bool ShowContextMenu { get; set; } = true;

        /// <summary>
        /// 是否允许拖拽页签调整顺序，默认 <c>false</c>。
        /// 置钉项（<see cref="ElProTabsGrowItem.Closable"/> 为 <c>false</c>，如首页）作为顺序锚点不可拖。
        /// </summary>
        [Parameter]
        public bool Sortable { get; set; }

        /// <summary>
        /// 「刷新」被点击，参数是被刷新的页签 —— 标签上的刷新图标（<see cref="ShowRefresh"/>）与右键菜单「刷新」
        /// 抛的是同一个事件。组件只抛事件，怎么刷由宿主决定；推荐**只重建该页签的内容**
        /// （换掉包住页面组件的 <c>@key</c>），而不是整页重载（那会把整个应用重启、所有页签一起丢状态）。
        /// </summary>
        [Parameter]
        public EventCallback<ElProTabsGrowItem> OnRefreshTab { get; set; }

        /// <summary>
        /// 是否在每条页签标签上渲染常驻的刷新图标，默认 <c>false</c>（不改变既有宿主的观感）。
        /// 单项可用 <see cref="ElProTabsGrowItem.Refreshable"/> 退出；图标点击只抛 <see cref="OnRefreshTab"/>，
        /// 并已阻止冒泡，不会顺带选中该页签。
        /// </summary>
        [Parameter]
        public bool ShowRefresh { get; set; }

        /// <summary>右键菜单「关闭左侧」被点击（参数为被右键的页签）。</summary>
        [Parameter]
        public EventCallback<ElProTabsGrowItem> OnCloseLeft { get; set; }

        /// <summary>右键菜单「关闭右侧」被点击（参数为被右键的页签）。</summary>
        [Parameter]
        public EventCallback<ElProTabsGrowItem> OnCloseRight { get; set; }

        /// <summary>拖拽排序落定后抛出（参数为排序后的 <see cref="ElProTabsGrowItem.Path"/> 数组，宿主按它重排 <see cref="Items"/>）。</summary>
        [Parameter]
        public EventCallback<string[]> OnReorder { get; set; }

        /// <summary>无障碍标签。</summary>
        [Parameter]
        public string AriaLabel { get; set; } = "页签导航";

        #endregion

        #region 状态

        private ElementReference Root;
        private ElementReference ScrollView;
        private DotNetObjectReference<ElProTabsGrow> objectReference;

        /// <summary>是否溢出（由 JS 测量后回填）。</summary>
        private bool hasScroll;

        private bool atStart = true;
        private bool atEnd = true;
        private bool moreOpen;

        /// <summary>右键菜单状态：是否打开 / 被右键的页签 / 弹出坐标（视口）。</summary>
        private bool contextOpen;
        private string contextPath;
        private double contextX;
        private double contextY;

        /// <summary>拖拽排序状态：正在拖的页签 / 当前悬停的落点页签。</summary>
        private string dragPath;
        private string dropTargetPath;

        private string activeTextColorOverride;
        private string attachKey;
        private bool moreAttached;

        /// <summary>运行时覆盖的激活色（<see cref="SetActiveTextColor"/> 写入）。</summary>
        private string EffectiveActiveTextColor =>
            string.IsNullOrWhiteSpace(activeTextColorOverride) ? ActiveTextColor : activeTextColorOverride;

        /// <summary>过滤掉空项后的页签列表。</summary>
        private IReadOnlyList<ElProTabsGrowItem> EffectiveItems =>
            Items == null
                ? Array.Empty<ElProTabsGrowItem>()
                : Items.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Path)).ToList();

        /// <summary>是否处于溢出状态（已出现滚动箭头）。</summary>
        public bool HasScroll => hasScroll;

        /// <summary>「更多」下拉当前是否展开。</summary>
        public bool IsMoreOpened => moreOpen;

        /// <summary>右键菜单当前是否展开。</summary>
        public bool IsContextMenuOpened => contextOpen;

        #endregion

        #region 便捷方法（宿主可用；对齐「属性或方便的方法」两种改色入口）

        /// <summary>
        /// 运行时改激活页签文字颜色（等价于改 <see cref="ActiveTextColor"/>，但不必等父组件重渲染）。
        /// </summary>
        public void SetActiveTextColor(string color)
        {
            activeTextColorOverride = color;
            Refresh();
        }

        /// <summary>撤销 <see cref="SetActiveTextColor"/> 的运行时覆盖，回到 <see cref="ActiveTextColor"/>。</summary>
        public void ResetActiveTextColor()
        {
            activeTextColorOverride = null;
            Refresh();
        }

        /// <summary>把指定页签滚入可视区（路由变化后由宿主调用）。</summary>
        public async Task ScrollToPathAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try { await JSRuntime.InvokeVoidAsync("elementProTabsScrollToTab", Root, path); }
            catch { }
        }

        /// <summary>把当前激活页签滚入可视区。</summary>
        public Task ScrollToActiveAsync() => ScrollToPathAsync(ActivePath);

        /// <summary>关闭「更多」下拉（右键菜单若仍开着则保留外部点击监听）。</summary>
        public async Task CloseMoreAsync()
        {
            if (!moreOpen) return;
            moreOpen = false;
            if (!contextOpen)
            {
                await DetachMoreAsync();
            }
            await InvokeAsync(StateHasChanged);
        }

        #endregion

        #region 渲染辅助

        private bool IsActivePath(string path) =>
            !string.IsNullOrWhiteSpace(path) && string.Equals(path, ActivePath, StringComparison.OrdinalIgnoreCase);

        private bool IsClosable(ElProTabsGrowItem item) =>
            Closable && item != null && item.Closable && !item.Disabled;

        private bool IsRefreshable(ElProTabsGrowItem item) =>
            ShowRefresh && item != null && item.Refreshable && !item.Disabled;

        private static string EffectiveTooltip(ElProTabsGrowItem item) =>
            string.IsNullOrWhiteSpace(item.Tooltip) ? item.Title : item.Tooltip;

        private static string BoolText(bool value) => value ? "true" : "false";

        private static string NormalizeIcon(string icon)
        {
            if (string.IsNullOrWhiteSpace(icon)) return null;
            return icon.StartsWith("el-icon-", StringComparison.OrdinalIgnoreCase) ? icon : $"el-icon-{icon}";
        }

        #endregion

        #region 交互

        private async Task SelectAsync(ElProTabsGrowItem item)
        {
            if (item == null || item.Disabled) return;

            await CloseMoreAsync();
            await CloseContextAsync();

            if (OnSelect.HasDelegate)
            {
                await OnSelect.InvokeAsync(item);
            }

            if (!IsActivePath(item.Path))
            {
                ActivePath = item.Path;
                if (ActivePathChanged.HasDelegate)
                {
                    await ActivePathChanged.InvokeAsync(item.Path);
                }
            }
        }

        private async Task CloseAsync(ElProTabsGrowItem item)
        {
            if (item == null) return;

            await CloseMoreAsync();
            await CloseContextAsync();

            if (OnClose.HasDelegate)
            {
                await OnClose.InvokeAsync(item);
            }
        }

        /// <summary>
        /// 标签上的刷新图标被点击。与右键菜单「刷新」抛同一个 <see cref="OnRefreshTab"/>，
        /// 区别只在于这里不经过菜单（菜单本来就没开，CloseContextAsync 是幂等的）。
        /// </summary>
        private async Task RefreshAsync(ElProTabsGrowItem item)
        {
            if (item == null) return;

            await CloseMoreAsync();
            await CloseContextAsync();

            if (OnRefreshTab.HasDelegate)
            {
                await OnRefreshTab.InvokeAsync(item);
            }
        }

        private async Task CloseOthersAsync()
        {
            var current = EffectiveItems.FirstOrDefault(x => IsActivePath(x.Path)) ?? EffectiveItems.FirstOrDefault();

            await CloseMoreAsync();

            if (OnCloseOthers.HasDelegate)
            {
                await OnCloseOthers.InvokeAsync(current);
            }
        }

        private async Task CloseAllAsync()
        {
            await CloseMoreAsync();

            if (OnCloseAll.HasDelegate)
            {
                await OnCloseAll.InvokeAsync();
            }
        }

        private async Task ToggleMoreAsync()
        {
            if (moreOpen)
            {
                await CloseMoreAsync();
                return;
            }

            moreOpen = true;
            await InvokeAsync(StateHasChanged);
            await AttachMoreAsync();
        }

        private async Task ScrollLeftAsync() => await ScrollByAsync(-240);

        private async Task ScrollRightAsync() => await ScrollByAsync(240);

        private async Task ScrollByAsync(int delta)
        {
            try { await JSRuntime.InvokeVoidAsync("elementProTabsScrollBy", Root, delta); }
            catch { }
        }

        /// <summary>键盘：Enter/Space 激活，Delete/Backspace 关闭，←/→/Home/End 在页签间移动激活。</summary>
        private async Task OnItemKeyDownAsync(KeyboardEventArgs e, ElProTabsGrowItem item)
        {
            if (item == null || item.Disabled) return;

            switch (e.Key)
            {
                case "Enter":
                case " ":
                case "Spacebar":
                    await SelectAsync(item);
                    return;
                case "Delete":
                case "Backspace":
                    if (IsClosable(item)) await CloseAsync(item);
                    return;
            }

            if (e.Key != "ArrowLeft" && e.Key != "ArrowRight" && e.Key != "Home" && e.Key != "End") return;

            var list = EffectiveItems.Where(x => !x.Disabled).ToList();
            if (list.Count == 0) return;

            var index = list.FindIndex(x => string.Equals(x.Path, item.Path, StringComparison.OrdinalIgnoreCase));
            if (index < 0) index = 0;

            index = e.Key switch
            {
                "Home" => 0,
                "End" => list.Count - 1,
                "ArrowLeft" => (index - 1 + list.Count) % list.Count,
                _ => (index + 1) % list.Count
            };

            await SelectAsync(list[index]);
        }

        #endregion

        #region 右键菜单

        /// <summary>被右键的页签（菜单已关或找不到时为 null）。</summary>
        private ElProTabsGrowItem ContextItem =>
            string.IsNullOrWhiteSpace(contextPath)
                ? null
                : EffectiveItems.FirstOrDefault(x => string.Equals(x.Path, contextPath, StringComparison.OrdinalIgnoreCase));

        private int ContextIndex
        {
            get
            {
                var items = EffectiveItems;
                for (var i = 0; i < items.Count; i++)
                {
                    if (string.Equals(items[i].Path, contextPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
                return -1;
            }
        }

        /// <summary>锚点左侧还有可关闭的页签 → 「关闭左侧」可用。</summary>
        public bool CanCloseLeft()
        {
            var index = ContextIndex;
            if (index <= 0) return false;

            var items = EffectiveItems;
            for (var i = 0; i < index; i++)
            {
                if (IsClosable(items[i])) return true;
            }
            return false;
        }

        /// <summary>锚点右侧还有可关闭的页签 → 「关闭右侧」可用。</summary>
        public bool CanCloseRight()
        {
            var index = ContextIndex;
            var items = EffectiveItems;

            for (var i = index + 1; i < items.Count; i++)
            {
                if (IsClosable(items[i])) return true;
            }
            return false;
        }

        /// <summary>除锚点外还有可关闭页签 → 「关闭其他」可用。</summary>
        public bool CanCloseOthers() => EffectiveItems.Count(x => IsClosable(x) && !string.Equals(x.Path, contextPath, StringComparison.OrdinalIgnoreCase)) > 0;

        /// <summary>存在任何可关闭页签 → 「关闭全部」可用。</summary>
        public bool CanCloseAll() => EffectiveItems.Any(IsClosable);

        private async Task OpenContextAsync(ElProTabsGrowItem item, MouseEventArgs e)
        {
            if (!ShowContextMenu || item == null || item.Disabled) return;

            await CloseMoreAsync();

            contextPath = item.Path;
            contextX = e.ClientX;
            contextY = e.ClientY;
            contextOpen = true;

            await InvokeAsync(StateHasChanged);
            await AttachMoreAsync();
        }

        private async Task CloseContextAsync()
        {
            if (!contextOpen) return;

            contextOpen = false;
            contextPath = null;

            if (!moreOpen)
            {
                await DetachMoreAsync();
            }
            await InvokeAsync(StateHasChanged);
        }

        private async Task ContextRefreshAsync()
        {
            var item = ContextItem;
            await CloseContextAsync();

            if (item != null && OnRefreshTab.HasDelegate)
            {
                await OnRefreshTab.InvokeAsync(item);
            }
        }

        private async Task ContextCloseSelfAsync()
        {
            var item = ContextItem;
            if (item == null || !IsClosable(item)) return;

            // CloseAsync 内部会再关一次菜单（幂等），并抛 OnClose
            await CloseAsync(item);
        }

        private async Task ContextCloseLeftAsync()
        {
            var item = ContextItem;
            if (item == null || !CanCloseLeft()) return;

            await CloseContextAsync();

            if (OnCloseLeft.HasDelegate)
            {
                await OnCloseLeft.InvokeAsync(item);
            }
        }

        private async Task ContextCloseRightAsync()
        {
            var item = ContextItem;
            if (item == null || !CanCloseRight()) return;

            await CloseContextAsync();

            if (OnCloseRight.HasDelegate)
            {
                await OnCloseRight.InvokeAsync(item);
            }
        }

        /// <summary>「关闭其他」以被右键的页签为锚（而非当前激活页签）。</summary>
        private async Task ContextCloseOthersAsync()
        {
            var item = ContextItem;
            if (item == null || !CanCloseOthers()) return;

            await CloseContextAsync();

            if (OnCloseOthers.HasDelegate)
            {
                await OnCloseOthers.InvokeAsync(item);
            }
        }

        private async Task ContextCloseAllAsync()
        {
            if (!CanCloseAll()) return;

            await CloseContextAsync();
            await CloseAllAsync();
        }

        #endregion

        #region 拖拽排序

        /// <summary>可拖拽的页签：开启排序、未禁用且非置钉（Closable=false 是顺序锚点）。</summary>
        private bool IsDraggable(ElProTabsGrowItem item) =>
            Sortable && item != null && !item.Disabled && item.Closable;

        private string ItemClass(ElProTabsGrowItem item, bool isActive)
        {
            var path = item.Path;

            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-pro-tabs-grow__item", ItemCls, item.Cls)
                .AddIf(isActive, "is-active")
                .AddIf(item.Disabled, "is-disabled")
                .AddIf(Sortable && string.Equals(path, dragPath, StringComparison.OrdinalIgnoreCase), "is-dragging")
                .AddIf(Sortable && string.Equals(path, dropTargetPath, StringComparison.OrdinalIgnoreCase)
                                   && !string.Equals(path, dragPath, StringComparison.OrdinalIgnoreCase), "is-drop-target")
                .ToString();
        }

        private Task DragStartAsync(ElProTabsGrowItem item, DragEventArgs e)
        {
            if (!IsDraggable(item)) return Task.CompletedTask;

            dragPath = item.Path;
            dropTargetPath = null;
            return InvokeAsync(StateHasChanged);
        }

        private Task DragOverItemAsync(ElProTabsGrowItem item)
        {
            if (dragPath == null || string.Equals(item.Path, dragPath, StringComparison.OrdinalIgnoreCase)) return Task.CompletedTask;

            if (string.Equals(item.Path, dropTargetPath, StringComparison.OrdinalIgnoreCase)) return Task.CompletedTask;

            dropTargetPath = item.Path;
            return InvokeAsync(StateHasChanged);
        }

        private async Task DropOnItemAsync(ElProTabsGrowItem item)
        {
            if (dragPath == null) return;

            if (item != null)
            {
                dropTargetPath = item.Path;
            }

            await CompleteReorderAsync();
        }

        private Task DragEndAsync()
        {
            dragPath = null;
            dropTargetPath = null;
            return InvokeAsync(StateHasChanged);
        }

        /// <summary>把拖拽页签移到落点页签的索引上，抛出排序后的 Path 数组。</summary>
        private async Task CompleteReorderAsync()
        {
            var from = dragPath;
            var to = dropTargetPath;
            dragPath = null;
            dropTargetPath = null;

            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to)
                || string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            {
                await InvokeAsync(StateHasChanged);
                return;
            }

            var list = EffectiveItems.ToList();
            var fromIndex = list.FindIndex(x => string.Equals(x.Path, from, StringComparison.OrdinalIgnoreCase));
            var toIndex = list.FindIndex(x => string.Equals(x.Path, to, StringComparison.OrdinalIgnoreCase));

            if (fromIndex < 0 || toIndex < 0)
            {
                await InvokeAsync(StateHasChanged);
                return;
            }

            var moved = list[fromIndex];
            list.RemoveAt(fromIndex);
            list.Insert(toIndex, moved);

            if (OnReorder.HasDelegate)
            {
                await OnReorder.InvokeAsync(list.Select(x => x.Path).ToArray());
            }

            await InvokeAsync(StateHasChanged);
        }

        #endregion

        #region JS 通道

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // 页签数量变化会影响是否溢出，需要重新测量
            var key = EffectiveItems.Count.ToString();
            if (key != attachKey)
            {
                attachKey = key;
                objectReference ??= DotNetObjectReference.Create(this);
                try { await JSRuntime.InvokeVoidAsync("elementProTabsAttach", Root, ScrollView, objectReference); }
                catch { }
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task AttachMoreAsync()
        {
            if (moreAttached) return;
            moreAttached = true;
            objectReference ??= DotNetObjectReference.Create(this);
            try { await JSRuntime.InvokeVoidAsync("elementProTabsMoreOpen", Root, objectReference); }
            catch { }
        }

        private async Task DetachMoreAsync()
        {
            if (!moreAttached) return;
            moreAttached = false;
            try { await JSRuntime.InvokeVoidAsync("elementProTabsMoreClose", Root); }
            catch { }
        }

        /// <summary>JS 回填滚动状态（溢出 / 是否到两端）。</summary>
        [JSInvokable]
        public async Task OnProTabsScrollState(bool scrollable, bool start, bool end)
        {
            await InvokeAsync(() =>
            {
                if (hasScroll == scrollable && atStart == start && atEnd == end) return;

                hasScroll = scrollable;
                atStart = start;
                atEnd = end;
                MarkAsRequireRender();
                StateHasChanged();
            });
        }

        /// <summary>JS 上报「点击落在组件外部」→ 收起右键菜单与「更多」面板。</summary>
        [JSInvokable]
        public async Task OnProTabsOutsideClick()
        {
            await CloseContextAsync();
            await CloseMoreAsync();
        }

        #endregion

        public override void Dispose()
        {
            base.Dispose();

            if (moreAttached)
            {
                moreAttached = false;
                try { JSRuntime.InvokeVoidAsync("elementProTabsMoreClose", Root); } catch { }
            }

            try { JSRuntime.InvokeVoidAsync("elementProTabsDetach", Root); } catch { }

            objectReference?.Dispose();
            objectReference = null;
        }
    }
}
