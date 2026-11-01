using ElementPlus;
using ElementPlus.Blazor.Demo.Components;
using ElementPlus.Blazor.Demo.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElementPlus.Blazor.Demo.Layout
{
    /// <summary>
    /// 演示站外壳：ElMenu 侧栏（子菜单 + 分组 + 折叠）+ 页头（折叠/刷新/面包屑/搜索/全屏/通知/头像）
    /// + 多标签页（ElProTabsGrow，支持强刷恢复）。
    /// <para>
    /// 页签的「打开 / 关闭 / 顺序 / 当前项」由本布局维护，并通过 <c>demoStoreSet</c>
    /// 持久化到 localStorage（key = <see cref="StoreKey"/>）——
    /// 所以 <b>强制刷新浏览器后已打开的那几个标签页会原样恢复</b>。
    /// </para>
    /// </summary>
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        /// <summary>页签持久化的 localStorage key。</summary>
        private const string StoreKey = "eb.demo.tabs.v1";

        /// <summary>侧栏折叠状态的 localStorage key。</summary>
        private const string SidebarKey = "eb.demo.sidebar";

        /// <summary>恢复上限，避免历史存储被写坏后一次渲染几千个标签。</summary>
        private const int MaxTabs = 40;

        /// <summary>
        /// 页签激活文字颜色。想换色改这一行即可；
        /// 也可以在运行时调 <c>tabsRef.SetActiveTextColor("#e6a23c")</c>（等价入口）。
        /// </summary>
        private const string TabActiveTextColor = "#395ae3";

        /// <summary>激活页签圆点颜色（主色 70% + 白 30%，对齐参考站）。</summary>
        private const string TabActiveDotColor = "#748ceb";

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        [Inject]
        private NavigationManager NavigationManager { get; set; }

        [Inject]
        private MessageService MessageService { get; set; }

        private SidebarMenu sidebarMenuRef;
        private ElProTabsGrow tabsRef;
        private ElementReference searchInput;
        private DotNetObjectReference<MainLayout> objectReference;

        private readonly List<ElProTabsGrowItem> tabs = new();

        /// <summary>上次写入 localStorage 的页签 JSON，用于跳过「切换标签时的无谓写盘」。</summary>
        private string savedTabsJson;
        private readonly string[] notices =
        {
            "住院部 3 号机扫描任务已完成",
            "检验科报告模板 v2.14 已更新",
            "本周质控指标存在 2 项待处理"
        };

        private string activePath = "/";

        /// <summary>本次渲染前已同步过的路径（见 <see cref="SyncFromUrl"/>），用于判断是否还需要补一次渲染。</summary>
        private string syncedPath;
        private string navSearch = string.Empty;
        private bool sidebarCollapsed;
        private bool isFullscreen;
        private bool shellReady;

        /// <summary>关闭按钮显示时机：默认「悬停标签才显示」（对齐需求）。</summary>
        private ElProTabsGrowCloseButtonDisplay tabCloseDisplay = ElProTabsGrowCloseButtonDisplay.Hover;

        /// <summary>面包屑文本（首页 → 分组 → 当前页）。</summary>
        private IReadOnlyList<string> Crumbs => DemoNavigation.Breadcrumb(activePath);

        /// <summary>搜索结果；为空表示不处于搜索态（此时显示完整分类菜单）。</summary>
        private IReadOnlyList<DemoNavEntry> SearchResults =>
            string.IsNullOrWhiteSpace(navSearch)
                ? null
                : DemoNavigation.Fixed
                    .Where(e => e.Title.Contains(navSearch, StringComparison.OrdinalIgnoreCase)
                                || e.Path.Contains(navSearch, StringComparison.OrdinalIgnoreCase))
                    .Take(80)
                    .ToList();

        #region 生命周期

        protected override async Task OnInitializedAsync()
        {
            NavigationManager.LocationChanged += OnLocationChanged;

            // 侧栏折叠偏好
            try
            {
                var stored = await JSRuntime.InvokeAsync<string>("demoStoreGet", SidebarKey);
                sidebarCollapsed = string.Equals(stored, "1", StringComparison.Ordinal);
            }
            catch
            {
            }

            // 先把上次打开的页签读回来（OnInitializedAsync 里 await 的 JS 调用在 WASM 下是即时的，
            // 因此首帧就已经是恢复后的标签集，不会出现「先闪一下默认标签」）
            await RestoreTabsAsync();

            var path = CurrentPath();
            if (!IsSkippedPath(path))
            {
                EnsureTab(path, activate: true);
            }

            activePath = tabs.Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase))
                ? path
                : tabs.FirstOrDefault()?.Path ?? "/";

            // 首次整页加载不会触发 OnLocationChanged，必须把当前页签也写进 localStorage，
            // 否则直接访问子页或强刷时历史打开的页签丢失、无法累积。
            await SaveTabsAsync();
        }

        /// <summary>
        /// 每次路由导航，父级都会重设本布局的 <c>Body</c> 参数、从而触发本方法——
        /// 这里在「本次渲染之前」把页签集合 / 激活页签 / 侧栏高亮同步到当前 URL。
        /// <para>
        /// 关键：不能再依赖 OnLocationChanged 之后再补一次 StateHasChanged ——
        /// 菜单项跳转（ElMenuItem.Route）不像点页签那样走 <c>@bind-ActivePath</c>，
        /// 没有任何东西会把 activePath 的新值送进渲染，于是页签高亮和面包屑会停在上一页
        /// （实测现象：侧栏高亮已是新页、内容也是新页，但页签高亮与面包屑还是旧页）。
        /// 放在这里即可保证紧随其后的这次渲染就已经是最终正确状态，一次渲染到位。
        /// </para>
        /// </summary>
        protected override void OnParametersSet()
        {
            syncedPath = SyncFromUrl();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender || shellReady)
            {
                return;
            }

            shellReady = true;
            objectReference ??= DotNetObjectReference.Create(this);

            try
            {
                await JSRuntime.InvokeVoidAsync("demoBindFullscreen", objectReference);
                await JSRuntime.InvokeVoidAsync("demoBindShortcut", objectReference);
                // 页头/标签栏固定，只有内容区滚 —— 绑定内容区滚动以驱动标签栏投影
                await JSRuntime.InvokeVoidAsync("demoBindContentScroll");
                // 细滚动条「鼠标移入容器才显形」：Blink 不因 :hover 重绘 ::-webkit-scrollbar，
                // 必须由 JS 在指针进入/离开滚动容器时重建一次滚条（详见 dom.js 内注释）
                await JSRuntime.InvokeVoidAsync("elementBindFadeScrollbar");
                isFullscreen = await JSRuntime.InvokeAsync<bool>("demoIsFullscreen");
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;

            if (shellReady)
            {
                try { JSRuntime.InvokeVoidAsync("demoUnbindFullscreen"); } catch { }
                try { JSRuntime.InvokeVoidAsync("demoUnbindShortcut"); } catch { }
                try { JSRuntime.InvokeVoidAsync("demoUnbindContentScroll"); } catch { }
            }

            objectReference?.Dispose();
            objectReference = null;
        }

        #endregion

        #region 路径 / 页签

        private string CurrentPath() => DemoNavigation.Normalize(NavigationManager.ToBaseRelativePath(NavigationManager.Uri));

        /// <summary>登录页用独立布局（外侧没有外壳），不参与页签。</summary>
        private static bool IsSkippedPath(string path) =>
            string.Equals(path, "/login", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// 按当前 URL 同步页签集合、激活页签与侧栏高亮；返回已同步的路径（无需同步时为 <c>null</c>）。
        /// 只认演示站注册过的路径——野路径（手输错地址等）沿用首次加载时的兜底激活项，避免出现「一个高亮都没有」。
        /// </summary>
        private string SyncFromUrl()
        {
            var path = CurrentPath();

            if (IsSkippedPath(path) || DemoNavigation.Find(path) == null)
            {
                return null;
            }

            EnsureTab(path, activate: true);

            // 侧栏激活态不跟着 URL 走（ElMenu 只在首次 OnAfterRender 解析 default-active），这里手工同步
            sidebarMenuRef?.SetActive(path);

            return path;
        }

        /// <summary>确保某个路径有对应页签；<paramref name="activate"/> 为真时同步激活。</summary>
        private ElProTabsGrowItem EnsureTab(string path, bool activate)
        {
            var key = DemoNavigation.Normalize(path);
            var tab = tabs.FirstOrDefault(x => string.Equals(x.Path, key, StringComparison.OrdinalIgnoreCase));

            if (tab == null)
            {
                var entry = DemoNavigation.Find(key);
                tab = new ElProTabsGrowItem
                {
                    Path = key,
                    Title = entry?.Title ?? DemoNavigation.TitleOf(key),
                    Icon = entry?.Icon ?? DemoNavigation.IconOf(key),
                    Closable = entry?.Closable ?? key != "/"
                };

                // 首页固定钉在最左边
                if (key == "/")
                {
                    tabs.Insert(0, tab);
                }
                else
                {
                    tabs.Add(tab);
                }
            }

            if (activate)
            {
                activePath = key;
            }

            return tab;
        }

        private async Task RestoreTabsAsync()
        {
            string json = null;

            try { json = await JSRuntime.InvokeAsync<string>("demoStoreGet", StoreKey); }
            catch { }

            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var paths = JsonSerializer.Deserialize<string[]>(json);

                    if (paths != null)
                    {
                        foreach (var path in paths)
                        {
                            if (tabs.Count >= MaxTabs) break;

                            var key = DemoNavigation.Normalize(path);
                            if (string.IsNullOrWhiteSpace(key) || IsSkippedPath(key)) continue;

                            // 只恢复「演示站真的注册过」的路径，避免历史存储里的野路径变成死标签
                            if (DemoNavigation.Find(key) == null) continue;

                            EnsureTab(key, activate: false);
                        }
                    }
                }
                catch
                {
                    // 存储内容坏了就当没有，别再抛出去把整个 demo 弄白屏
                }
            }

            if (tabs.Count == 0)
            {
                EnsureTab("/", activate: false);
            }
        }

        private async Task SaveTabsAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(tabs.Select(x => x.Path).ToArray());

                // 切页签并不改变页签集合，只有新开 / 关闭 / 重排才需要真正落盘
                if (string.Equals(json, savedTabsJson, StringComparison.Ordinal))
                {
                    return;
                }

                await JSRuntime.InvokeVoidAsync("demoStoreSet", StoreKey, json);
                savedTabsJson = json;
            }
            catch
            {
            }
        }

        private void OnLocationChanged(object sender, LocationChangedEventArgs e)
        {
            var path = CurrentPath();

            if (IsSkippedPath(path))
            {
                return;
            }

            // 正常情况下本次导航的重渲染已经走过 OnParametersSet → SyncFromUrl（页签高亮 / 面包屑已是对的），
            // 这里只在「没走到」时补同步 + 补一次渲染（例如当前路由与上次相同、父级没重设参数）。
            if (!string.Equals(syncedPath, path, StringComparison.OrdinalIgnoreCase))
            {
                syncedPath = SyncFromUrl();

                if (syncedPath != null)
                {
                    _ = InvokeAsync(StateHasChanged);
                }
            }

            _ = AfterNavigateAsync(path);
        }

        private async Task AfterNavigateAsync(string path)
        {
            await SaveTabsAsync();

            try
            {
                // 切页签后内容区回到顶部（滚动条是内容区的，不会被路由自动重置）
                await JSRuntime.InvokeVoidAsync("demoScrollContentTop");
            }
            catch
            {
            }

            if (tabsRef != null)
            {
                // 等子组件先按新激活态渲染，再把它滚进可视区
                await Task.Yield();
                await tabsRef.ScrollToPathAsync(path);
            }
        }

        private void OnMenuSelect(MenuSelectEventArgs e)
        {
            // 路由跳转由 ElMenuItem.Route 完成，这里只收起搜索结果态
            if (SearchResults != null)
            {
                navSearch = string.Empty;
            }
        }

        #endregion

        #region 页签交互

        private void OnTabSelect(ElProTabsGrowItem item)
        {
            if (item == null) return;

            // 点已激活的页签 → 什么都不做（避免重复导航刷屏）
            if (string.Equals(item.Path, CurrentPath(), StringComparison.OrdinalIgnoreCase))
            {
                activePath = item.Path;
                return;
            }

            NavigationManager.NavigateTo(item.Path);
        }

        /// <summary>
        /// 页签关闭后把该路径对应的 keep-alive 缓存页真正摘掉。
        /// <para>
        /// <see cref="KeepAliveRouteView"/> 平时只把非活动页 <c>display:none</c>，组件实例一直挂在 DOM 里，
        /// 所以「关了页签视频还在放」不是播放器没释放，而是那个页面根本没被卸载。
        /// 移除页签 = 用户明确不要这个页面了，这里补上缺的那条通道（路径按 <c>@page</c> 模板匹配）。
        /// </para>
        /// </summary>
        private static void EvictCachedPage(string path) => KeepAliveRouteView.EvictPath(path);

        private async Task OnTabClose(ElProTabsGrowItem item)
        {
            if (item == null || !item.Closable) return;

            var index = tabs.FindIndex(x => string.Equals(x.Path, item.Path, StringComparison.OrdinalIgnoreCase));
            if (index < 0) return;

            var wasActive = string.Equals(item.Path, activePath, StringComparison.OrdinalIgnoreCase);
            tabs.RemoveAt(index);
            EvictCachedPage(item.Path);

            if (tabs.Count == 0)
            {
                // 全关光了就把首页补回来
                EnsureTab("/", activate: wasActive);
                if (wasActive) NavigationManager.NavigateTo("/");
                await SaveTabsAsync();
                return;
            }

            if (wasActive)
            {
                // 关掉当前页 → 优先激活右邻，没有就取最后一个
                var next = tabs[Math.Min(index, tabs.Count - 1)];
                activePath = next.Path;
                NavigationManager.NavigateTo(next.Path);
                return;
            }

            await SaveTabsAsync();
        }

        private async Task OnTabsCloseOthers(ElProTabsGrowItem current)
        {
            var keep = current ?? tabs.FirstOrDefault(x => string.Equals(x.Path, activePath, StringComparison.OrdinalIgnoreCase));

            foreach (var path in tabs.Where(x => x.Closable && !ReferenceEquals(x, keep)).Select(x => x.Path).ToArray())
            {
                EvictCachedPage(path);
            }

            tabs.RemoveAll(x => x.Closable && !ReferenceEquals(x, keep));

            if (keep != null)
            {
                activePath = keep.Path;

                if (!string.Equals(keep.Path, CurrentPath(), StringComparison.OrdinalIgnoreCase))
                {
                    NavigationManager.NavigateTo(keep.Path);
                }
            }

            await SaveTabsAsync();
        }

        private async Task OnTabsCloseAll()
        {
            foreach (var path in tabs.Where(x => x.Closable).Select(x => x.Path).ToArray())
            {
                EvictCachedPage(path);
            }

            tabs.RemoveAll(x => x.Closable);

            var first = tabs.FirstOrDefault();
            activePath = first?.Path ?? "/";

            if (!string.Equals(activePath, CurrentPath(), StringComparison.OrdinalIgnoreCase))
            {
                NavigationManager.NavigateTo(activePath);
            }

            await SaveTabsAsync();
        }

        /// <summary>右键菜单「刷新」：切到该页（若不在）再整页重载；页签已持久化，重载后原样恢复。</summary>
        private void OnTabRefresh(ElProTabsGrowItem item)
        {
            if (item == null) return;

            var path = DemoNavigation.Normalize(item.Path);

            if (!string.Equals(path, CurrentPath(), StringComparison.OrdinalIgnoreCase))
            {
                NavigationManager.NavigateTo(path);
            }

            NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
        }

        private async Task OnTabsCloseLeft(ElProTabsGrowItem anchor)
        {
            var index = IndexOfTab(anchor);
            if (index < 0) return;

            var activeRemoved = false;

            for (var i = index - 1; i >= 0; i--)
            {
                if (!tabs[i].Closable) continue;

                activeRemoved |= string.Equals(tabs[i].Path, activePath, StringComparison.OrdinalIgnoreCase);
                EvictCachedPage(tabs[i].Path);
                tabs.RemoveAt(i);
            }

            if (activeRemoved)
            {
                activePath = anchor.Path;
                NavigationManager.NavigateTo(anchor.Path);
                return;
            }

            await SaveTabsAsync();
        }

        private async Task OnTabsCloseRight(ElProTabsGrowItem anchor)
        {
            var index = IndexOfTab(anchor);
            if (index < 0) return;

            var activeRemoved = false;

            for (var i = tabs.Count - 1; i > index; i--)
            {
                if (!tabs[i].Closable) continue;

                activeRemoved |= string.Equals(tabs[i].Path, activePath, StringComparison.OrdinalIgnoreCase);
                EvictCachedPage(tabs[i].Path);
                tabs.RemoveAt(i);
            }

            if (activeRemoved)
            {
                activePath = anchor.Path;
                NavigationManager.NavigateTo(anchor.Path);
                return;
            }

            await SaveTabsAsync();
        }

        /// <summary>
        /// 拖拽排序落定：按组件抛出的 Path 顺序重排。
        /// 首页（置钉项）永远钉在最左 —— 组件侧置钉项不可拖，这里再兜底一次。
        /// </summary>
        private async Task OnTabsReorder(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;

            var map = tabs.ToDictionary(x => x.Path, StringComparer.OrdinalIgnoreCase);
            var next = new List<ElProTabsGrowItem>();

            foreach (var path in paths)
            {
                if (path != null && map.TryGetValue(path, out var tab))
                {
                    next.Add(tab);
                    map.Remove(path);
                }
            }

            // 防御：事件里没出现的项（理论不会有）追加回尾部，别弄丢
            next.AddRange(map.Values);

            var home = next.FirstOrDefault(x => string.Equals(x.Path, "/", StringComparison.OrdinalIgnoreCase));

            if (home != null)
            {
                next.Remove(home);
                next.Insert(0, home);
            }

            tabs.Clear();
            tabs.AddRange(next);

            await SaveTabsAsync();
        }

        private int IndexOfTab(ElProTabsGrowItem item) =>
            item == null
                ? -1
                : tabs.FindIndex(x => string.Equals(x.Path, item.Path, StringComparison.OrdinalIgnoreCase));

        #endregion

        #region 页头交互

        private async Task ToggleSidebar()
        {
            sidebarCollapsed = !sidebarCollapsed;

            try
            {
                // 折叠时收起已展开的子菜单（ElMenu 自己会做），侧栏宽度由 .demo-shell.is-collapsed 控制
                await JSRuntime.InvokeVoidAsync("demoStoreSet", SidebarKey, sidebarCollapsed ? "1" : "0");
            }
            catch
            {
            }
        }

        /// <summary>
        /// 刷新当前页：整页重载（forceLoad）。页签已持久化，重载后会自动回到同一组标签。
        /// </summary>
        private void RefreshAsync()
        {
            NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
        }

        private async Task ToggleFullscreenAsync()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("demoToggleFullscreen");
                isFullscreen = await JSRuntime.InvokeAsync<bool>("demoIsFullscreen");
            }
            catch
            {
            }
        }

        private void OnHeaderCommand(DropdownCommandEventArgs e)
        {
            switch (e.Command as string)
            {
                case "profile":
                    MessageService.Info("个人中心为演示占位页");
                    break;
                case "password":
                    MessageService.Info("修改密码为演示占位页");
                    break;
                case "logout":
                    NavigationManager.NavigateTo("/login");
                    break;
                case "notice-read":
                    MessageService.Success("已全部标记为已读");
                    break;
                default:
                    if (e.Command is string notice && notice.StartsWith("notice-", StringComparison.Ordinal))
                    {
                        MessageService.Info(notices[int.Parse(notice.Substring("notice-".Length)) - 1]);
                    }
                    break;
            }
        }

        /// <summary>把第 i 个面包屑映射成可点路径；中间的分组名没有独立路由，返回 null 渲染成纯文本。</summary>
        private string CrumbPath(int index)
        {
            // 只有「首页」是真正可跳的；当前页是最后一项（渲染成纯文本，不走这里）
            return index == 0 ? "/" : null;
        }

        #endregion

        #region JS 回调

        [JSInvokable]
        public async Task OnDemoFullscreenChanged(bool fullscreen)
        {
            await InvokeAsync(() =>
            {
                if (isFullscreen == fullscreen) return;

                isFullscreen = fullscreen;
                StateHasChanged();
            });
        }

        [JSInvokable]
        public async Task OnDemoShortcut(string key)
        {
            if (!string.Equals(key, "search", StringComparison.Ordinal))
            {
                return;
            }

            try { await JSRuntime.InvokeVoidAsync("demoFocusElement", searchInput); }
            catch { }
        }

        #endregion
    }
}
