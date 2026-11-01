using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace ElementPlus.Blazor.Demo;

/// <summary>
/// Keep-alive 版路由视图（替代 <see cref="RouteView"/>）：
/// 访问过的页面组件实例按「页面类型 + 路由参数」缓存保活（非活动页 display:none 隐藏），
/// 切回已打开的页签时直接复用旧实例——不重挂载、不重新 OnInitialized，消除「内容刷新一下」。
/// 上下文菜单「刷新」走 forceLoad 整页重载，缓存随之清空，刷新语义不受影响。
/// </summary>
/// <remarks>
/// 性能要点：所有已缓存页面只在「新开页」时由 <see cref="KeepAliveShelf"/> 重建一次；
/// 切换已有页签时 <see cref="KeepAliveRouteView"/> 虽会重渲染，但货架靠自身的 ShouldRender 闸门
/// （只在 Version 变化时才渲染）跳过整棵缓存页面的重渲染——
/// 注意不能指望 Blazor 的参数变更检测：Entries 是集合，它恒判定「可能已变」，
/// 不加闸门就会在每次切页签时重建并 diff 全部缓存页面的渲染树（实测主线程阻塞约 0.5s，即「卡顿/停滞感」）。
/// 光有闸门还不够：货架闸门只挡住「切已有页签」，「新开一页」仍会走一次 version++，
/// 若货架把每页渲染成 <c>AddContent(RenderFragment)</c>，那一次就会重跑<b>所有</b>缓存页的渲染树
/// （AddContent 传委托时父级每次重渲染都必然重新执行它）。所以每页再包一层 <see cref="CachedPage"/>
/// 子组件，并把挂载数据按引用存成 <see cref="Microsoft.AspNetCore.Components.RouteData"/>；
/// <b>跳过与否由 CachedPage 自己的 ShouldRender 决定</b> —— 单靠「参数是引用可比」并不会让 Blazor 跳过子组件
/// （<c>SetParametersAsync</c> 不比值，实测见 <c>test/Element.ComponentTests/KeepAliveShelfRenderFanOutTests.cs</c>）。
/// 这样新开第 N 页只付第 N 页的成本。
/// 显隐切换由 <see cref="OnAfterRenderAsync"/> 调用 JS 命名函数（demo-shell.js 的 demoKeepAliveShow）
/// 直接改 display，不走 Blazor 渲染，且只在激活 key 变化时才调用。
/// <para>
/// 新开页采用「两阶段挂载」：第一次渲染只放占位内容，真正挂载页面实例推迟到浏览器画完一帧之后
/// （见 <see cref="OnAfterRenderAsync"/>）。因为重型页面首次挂载会长时间阻塞主线程，
/// 若与外壳（页签高亮 / 侧栏高亮 / 面包屑）同批渲染，点击后要等到重挂载结束才有任何视觉反馈，
/// 就是「点了没反应、过一会儿才跳过去」的停滞感。
/// </para>
/// </remarks>
public class KeepAliveRouteView : ComponentBase, IDisposable
{
    private sealed class Entry
    {
        public required string Key;

        /// <summary>页签关闭时按 <c>@page</c> 模板反查该缓存项要用（见 <see cref="PathMatches"/>）。</summary>
        public required Type PageType;

        /// <summary>
        /// 挂载页面组件用的路由数据。<b>只存引用</b>、不预建 RenderFragment：货架每轮重渲染都要重新执行
        /// <c>AddContent</c> 里的片段（等于把整棵页面树重跑一遍），而把同一个 <see cref="RouteData"/> 实例
        /// 交给 <see cref="CachedPage"/>，它自己的 <c>ShouldRender</c> 就能按引用判定「没变」并一帧不建。
        /// 注意引用可比本身<b>不会</b>让 Blazor 跳过子组件（<c>SetParametersAsync</c> 不比值），闸门必须在子组件上。
        /// </summary>
        public required RouteData RouteData;

        /// <summary>是否已真正挂载页面组件。新开页先为 <c>false</c>（只渲染占位），下一帧再置真并挂载。</summary>
        public bool Mounted;
    }

    /// <summary>缓存上限，防止页签开了关、关了开导致无限增长（超出时淘汰最旧的未激活页）。</summary>
    private const int MaxCache = 30;

    /// <summary>
    /// 页签关闭 → 真释放该页实例的进程内通道（演示外壳 MainLayout 与本视图之间原本没有任何通道）。
    /// <para>
    /// keep-alive 平时只把非活动页 <c>display:none</c>，实例一直挂着：视频继续播、定时器继续跑 ——
    /// 「关掉 /video-player-grow 页签后声音还在」就是这条缺口的直接后果，且只有超过
    /// <see cref="MaxCache"/> 时被淘汰的页才会 Dispose。WASM 单实例单渲染器，本视图全局唯一，
    /// 所以用静态回调打通即可；Dispose 时清空，避免指向已卸载实例。
    /// </para>
    /// </summary>
    private static Action<string>? evictRequested;

    private readonly List<Entry> entries = new();
    private string? activeKey;
    /// <summary>上次已应用到 DOM 的激活 key，避免重复调用 JS 切换显隐。</summary>
    private string? appliedKey;
    /// <summary>仅在新开页入缓存时自增，用于通知货架重建一次；切已有页保持不变（避免整体重渲染）。</summary>
    private int version;

    [Parameter] public RouteData? RouteData { get; set; }

    /// <summary>页面未标注 <c>@layout</c> 时使用的默认布局（等价 RouteView 的 DefaultLayout）。</summary>
    [Parameter] public Type? DefaultLayout { get; set; }

    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        evictRequested = Evict;
    }

    public void Dispose() => evictRequested = null;

    /// <summary>供演示外壳调用：按页签路径把对应缓存页从货架上摘掉（连带真卸载其组件树）。</summary>
    public static void EvictPath(string path) => evictRequested?.Invoke(path);

    /// <summary>
    /// 「正显示的页」的延迟摘除队列（存归一化路径）。
    /// <para>
    /// 关掉当前页签时不能立刻重渲染：这一次渲染会经 <c>LayoutView</c> 回到 MainLayout 的
    /// <c>OnParametersSet → SyncFromUrl → EnsureTab(CurrentPath())</c>，而 WASM 下 history 更新是异步的，
    /// 此刻 <c>CurrentPath()</c> 仍是刚被关掉的那个路径，于是刚关的页签被按旧 URL 加回页签栏
    /// （实测现象：货架与内容都切走了，页签栏却留下一条关不掉、还会被写进 localStorage 的死标签）。
    /// 挂到下一次 <see cref="OnParametersSet"/>（那时 URL 已经更新）再摘，仍在同一批渲染里完成卸载。
    /// </para>
    /// </summary>
    private readonly List<string> pendingEvictions = new();

    private void Evict(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var normalized = NormalizePath(path);

        // 正在显示的那一页走延迟队列，其余（后台页签）立刻摘 —— 后台页摘除时的重渲染
        // 只会让 SyncFromUrl 按当前 URL 补一个本来就存在的页签，没有副作用。
        if (entries.Any(e => PathMatches(e, normalized) && e.Key == activeKey))
        {
            if (!pendingEvictions.Contains(normalized, StringComparer.Ordinal))
            {
                pendingEvictions.Add(normalized);
            }

            return;
        }

        if (RemoveEntry(normalized) == 0)
        {
            return;
        }

        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>按路径从缓存里摘掉页面并通知货架重建（被摘掉的子树随 diff 卸载 → 组件 Dispose/DisposeAsync 才真正跑到）。</summary>
    private int RemoveEntry(string path)
    {
        var removed = entries.RemoveAll(e => PathMatches(e, path));

        if (removed > 0)
        {
            version++;
        }

        return removed;
    }

    private static bool PathMatches(Entry entry, string path) => PathMatches(entry.PageType, path);

    private static bool PathMatches(Type pageType, string path) =>
        pageType.GetCustomAttributes(typeof(RouteAttribute), false)
            .Cast<RouteAttribute>()
            .Any(r => string.Equals(NormalizePath(r.Template), NormalizePath(path), StringComparison.Ordinal));

    private static string NormalizePath(string path)
    {
        var p = (path ?? string.Empty).Trim();
        if (!p.StartsWith("/", StringComparison.Ordinal))
        {
            p = "/" + p;
        }

        return p.Length > 1 ? p.TrimEnd('/') : p;
    }

    protected override void OnParametersSet()
    {
        if (RouteData is null)
        {
            return;
        }

        var key = ComputeKey(RouteData);

        // 结算延迟摘除队列：只有「已经路由到别的页」才动手，否则这条待摘项就是当前页，
        // 摘了等于把用户正在看的页面抽掉。这样也自然避开了 URL 还没更新的那一批渲染。
        for (var i = pendingEvictions.Count - 1; i >= 0; i--)
        {
            var pending = pendingEvictions[i];
            pendingEvictions.RemoveAt(i);

            if (!PathMatches(RouteData.PageType, pending))
            {
                RemoveEntry(pending);
            }
        }

        if (!entries.Any(e => e.Key == key))
        {
            // 超上限先淘汰最旧的未激活页，再入缓存
            if (entries.Count >= MaxCache)
            {
                var evict = entries.FindIndex(e => e.Key != activeKey);
                entries.RemoveAt(evict >= 0 ? evict : 0);
            }

            var rd = RouteData;
            entries.Add(new Entry
            {
                Key = key,
                PageType = rd.PageType,
                RouteData = rd,
                Mounted = false // 先渲染占位，挂载推迟到下一帧（两阶段挂载）
            });
            version++; // 仅新开页：通知货架重建一次（本次只放占位）
        }

        activeKey = key;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RouteData is null)
        {
            return;
        }

        // 页面自带 @layout 时优先（对齐 RouteView 语义），否则用 DefaultLayout
        var layout = RouteData.PageType.GetCustomAttribute<LayoutAttribute>()?.LayoutType ?? DefaultLayout;

        builder.OpenComponent<LayoutView>(0);
        builder.AddAttribute(1, nameof(LayoutView.Layout), layout);
        builder.AddAttribute(2, nameof(LayoutView.ChildContent), (RenderFragment)(b =>
        {
            // 货架由自身 ShouldRender 闸门控制在「新开页（version 变化）」时才重渲染
            b.OpenComponent<KeepAliveShelf>(0);
            b.AddAttribute(1, nameof(KeepAliveShelf.Entries), entries);
            b.AddAttribute(2, nameof(KeepAliveShelf.Version), version);
            b.CloseComponent();
        }));
        builder.CloseComponent();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // 阶段一：切换显隐。只在激活 key 真的变化时才动 DOM（切页签之外的重渲染不必重复切换），
        // 并且要排在新页挂载之前 —— 页签高亮 / 侧栏高亮 / 面包屑要在这一批就落屏。
        if (activeKey is not null && !string.Equals(activeKey, appliedKey, StringComparison.Ordinal))
        {
            appliedKey = activeKey;

            // JS 命名函数切换各页显隐，不触发 Blazor 重渲染：切标签几乎零开销
            await JSRuntime.InvokeVoidAsync("demoKeepAliveShow", activeKey);
        }

        // 阶段二：真正挂载新开页。等浏览器把上一批 DOM（含新页签高亮与新页占位）画出来之后再挂载，
        // 否则重挂载的阻塞会把这一刻的反馈一起吞掉，用户点完菜单要干等整秒才看到任何变化。
        var pending = false;

        foreach (var entry in entries)
        {
            if (!entry.Mounted)
            {
                entry.Mounted = true;
                pending = true;
            }
        }

        if (!pending)
        {
            return;
        }

        await JSRuntime.InvokeVoidAsync("demoNextFrame");

        version++;        // 通知货架重渲染：这一次才实例化页面组件（重型、会阻塞主线程）
        StateHasChanged();
    }

    private static string ComputeKey(RouteData routeData) =>
        routeData.PageType.FullName
        + "|"
        + string.Join("&", routeData.RouteValues.OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={kv.Value}"));

    /// <summary>
    /// 缓存页面的渲染货架：把所有已缓存页面都挂载在 DOM 中（非活动页 display:none），
    /// 由父级通过 JS 切换显隐。仅在 <see cref="Version"/> 变化（新开页）时整体重渲染，
    /// 切已有页签时不重渲染，避免对所有缓存页面做无谓的 diff。
    /// </summary>
    private sealed class KeepAliveShelf : ComponentBase
    {
        [Parameter] public List<Entry> Entries { get; set; } = new();

        /// <summary>新开页自增，触发本组件一次重渲染（Blazor 据此刷新缓存集合）。</summary>
        [Parameter] public int Version { get; set; }

        private int lastVersion = -1;

        /// <summary>
        /// 关键性能闸门：<see cref="Entries"/> 是集合（引用类型），Blazor 的参数变更检测
        /// （<c>ChangeDetection.MayHaveChanged</c>）对集合一律判定「可能已变」，
        /// 于是父级每次导航都会重设参数并触发本组件 StateHasChanged。
        /// 若不加闸门，就会在每次切页签时重建并 diff 全部缓存页面的渲染树。
        /// 这里只在真正新开页（<see cref="Version"/> 变化）时才重渲染，切已有页签直接跳过。
        /// </summary>
        protected override bool ShouldRender()
        {
            if (Version == lastVersion)
            {
                return false;
            }

            lastVersion = Version;
            return true;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            for (var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];

                // 每页一个独立子组件，且该子组件自带 ShouldRender 闸门：
                // 货架重渲染时这里只产出 N 帧「组件 + 参数」，引用没变的缓存页一帧都不往下建。
                builder.OpenElement(0, "div");
                builder.SetKey(entry.Key);
                builder.AddAttribute(1, "class", "keep-alive-page");
                builder.AddAttribute(2, "data-keep-key", entry.Key);
                builder.AddAttribute(3, "style", "display:none");
                builder.OpenComponent<CachedPage>(4);
                builder.AddAttribute(5, nameof(CachedPage.RouteData), entry.RouteData);
                builder.AddAttribute(6, nameof(CachedPage.Mounted), entry.Mounted);
                builder.CloseComponent();
                builder.CloseElement();
            }
        }
    }

    /// <summary>
    /// 单个缓存页的宿主，<b>自带 <see cref="ShouldRender"/> 闸门</b>（这才是真闸门，参数比对不是）。
    /// <para>
    /// 改造前这里是 <c>builder.AddContent(4, entry.Fragment)</c> —— 而 AddContent(RenderFragment) 在父级
    /// 每次重渲染时<b>必然重新执行</b>该片段，于是「新开一页」触发的 <c>version++</c> 会把所有已缓存页
    /// 的渲染树整个重跑一遍（第 N 次挂载要付 N 个页面的成本）。
    /// </para>
    /// <para>
    /// 但「拆成子组件、参数用引用可比的 <see cref="RouteData"/> + bool」<b>并不足以</b>跳过重渲染：
    /// 实测 6 页 × 4 轮里，无闸门的子组件宿主和它的页面组件仍旧各渲染 4 次 —— Blazor 的
    /// <c>SetParametersAsync</c> 不比较值，参数没变也照样重渲染（用例：
    /// <c>test/Element.ComponentTests/KeepAliveShelfRenderFanOutTests.cs</c>，三形态计数都钉在那儿）。
    /// 真闸门只有 <see cref="ShouldRender"/>：引用与 <see cref="Mounted"/> 都没变就一帧都不往下建。
    /// 闸门状态必须在 <see cref="BuildRenderTree"/> 里播种，因为首帧不走 <see cref="ShouldRender"/>。
    /// </para>
    /// </summary>
    private sealed class CachedPage : ComponentBase
    {
        /// <summary>本页要挂载的路由数据（整个缓存期都是同一实例，只做引用比对）。</summary>
        [Parameter] public RouteData RouteData { get; set; } = default!;

        /// <summary>两阶段挂载的闸门：false 只画占位，true 才实例化页面组件。</summary>
        [Parameter] public bool Mounted { get; set; }

        private RouteData? seededRouteData;
        private bool seededMounted;

        protected override bool ShouldRender()
            => !ReferenceEquals(seededRouteData, RouteData) || seededMounted != Mounted;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            seededRouteData = RouteData;
            seededMounted = Mounted;

            if (!Mounted)
            {
                // 新开页的占位内容：真正的挂载在下一帧（两阶段挂载），这一帧先让用户看到反馈
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "keep-alive-loading");
                builder.AddAttribute(2, "style", "padding:24px;font-size:13px;color:var(--el-text-color-secondary,#909399);");
                builder.AddContent(3, "正在加载…");
                builder.CloseElement();
                return;
            }

            builder.OpenComponent(0, RouteData.PageType);
            var seq = 1;
            foreach (var (name, value) in RouteData.RouteValues)
            {
                builder.AddAttribute(seq++, name, value);
            }
            builder.CloseComponent();
        }
    }
}
