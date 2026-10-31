using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;

namespace ElementPlus
{
    /// <summary>
    /// ElSplitter — 分隔面板，对齐 Element Plus Splitter 官方 API
    /// </summary>
    public partial class ElSplitter : ElementComponentBase, IAsyncDisposable
    {
        private int nextPanelIndex;
        private readonly List<ElSplitterPanel> panels = new();
        private DotNetObjectReference<ElSplitter> dotNetRef;

        /// <summary>已绑定过拖拽的分隔条数量（= 面板数 - 1）；变化时重新下发绑定。</summary>
        private int boundBarCount = -1;

        /// <summary>lazy 模式下拖拽过程中的位移（px）：只用于在分隔条上预览落点（--el-splitter-bar-offset）。</summary>
        private double lazyOffset;

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>分隔面板的布局方向</summary>
        [Parameter]
        public SplitterDirection Direction { get; set; } = SplitterDirection.Horizontal;

        /// <summary>延迟 2.11.0 — 启用时面板大小将不会在拖动时实时更新，只能在拖动结束后更新</summary>
        [Parameter]
        public bool Lazy { get; set; }

        /// <summary>开始调整面板大小时触发，index 是拖拽条的索引（对应官方 resize-start）</summary>
        [Parameter]
        public EventCallback<SplitterResizeEventArgs> OnResizeStart { get; set; }

        /// <summary>调整面板大小时触发（对应官方 resize）</summary>
        [Parameter]
        public EventCallback<SplitterResizeEventArgs> OnResize { get; set; }

        /// <summary>面板调整大小结束时触发（对应官方 resize-end）</summary>
        [Parameter]
        public EventCallback<SplitterResizeEventArgs> OnResizeEnd { get; set; }

        /// <summary>当面板折叠时触发（延迟 2.10.3，对应官方 collapse）</summary>
        [Parameter]
        public EventCallback<SplitterCollapseEventArgs> OnCollapse { get; set; }

        /// <summary>根元素引用</summary>
        public ElementReference RootElement { get; set; }

        [Inject] private IJSRuntime JSRuntime { get; set; }

        protected string SplitterClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-splitter", Cls)
            .Add($"el-splitter--{Direction.ToString().ToLowerInvariant()}")
            .AddIf(Lazy, "is-lazy")
            .ToString();

        protected string SplitterStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(Lazy, $"--el-splitter-bar-offset:{lazyOffset.ToString("0.###", CultureInfo.InvariantCulture)}px")
            .Add(Style)
            .ToString();

        // ===== Panel 注册（供 ElSplitterPanel 通过 Cascading 调用）=====

                // ===== Panel 操作方法（供 ElSplitterBar 调用）=====

        internal int GetPanelCount() => panels.Count;

        internal int RegisterPanel(ElSplitterPanel panel)
        {
            panels.Add(panel);
            nextPanelIndex++;
            return nextPanelIndex - 1; // 索引从 0 开始
        }

        internal void UnregisterPanel(ElSplitterPanel panel)
        {
            panels.Remove(panel);
        }

        internal List<decimal> GetAllPanelSizes()
        {
            var sizes = new List<decimal>();
            foreach (var p in panels)
                sizes.Add(p.CurrentPixelSize);
            return sizes;
        }

        // ===== 事件触发（供 Panel 调用）=====

        internal async Task FireResizeStartAsync(int index)
        {
            if (OnResizeStart.HasDelegate)
                await OnResizeStart.InvokeAsync(new SplitterResizeEventArgs
                { Index = index, Sizes = GetAllPanelSizes() });
        }

        internal async Task FireResizeAsync(int index)
        {
            if (OnResize.HasDelegate)
                await OnResize.InvokeAsync(new SplitterResizeEventArgs
                { Index = index, Sizes = GetAllPanelSizes() });
        }

        internal async Task FireResizeEndAsync(int index)
        {
            if (OnResizeEnd.HasDelegate)
                await OnResizeEnd.InvokeAsync(new SplitterResizeEventArgs
                { Index = index, Sizes = GetAllPanelSizes() });
        }

        internal async Task FireCollapseAsync(int index, bool collapsed)
        {
            if (OnCollapse.HasDelegate)
                await OnCollapse.InvokeAsync(new SplitterCollapseEventArgs
                { Index = index, Collapsed = collapsed });
        }

        // ===== 拖拽（事件源在 JS 侧：dom.js 的 elementSplitterInit，document 级 mousemove）=====
        //
        // 分隔条自身宽度为 0（对齐官方），所以拖拽事件不能绑在它身上；JS 在 mousedown 时改为
        // 监听 document，直接写面板 DOM 保证跟手，并把这些回调喂回 .NET 维护状态 / 派事件。

        private ElSplitterPanel FindPanel(int index)
        {
            foreach (var panel in panels)
            {
                if (panel.PanelIndex == index)
                {
                    return panel;
                }
            }

            return null;
        }

        /// <summary>拖拽开始（JS 调用）：触发 resize-start。</summary>
        [JSInvokable]
        public async Task JsSplitterStart(int index)
        {
            var panel = FindPanel(index);
            if (panel != null)
            {
                await panel.BeginDragAsync();
            }
        }

        /// <summary>拖拽中（JS 节流调用）：记录尺寸并触发 resize。nextSize 是相邻面板的新尺寸（-1 表示无）。</summary>
        [JSInvokable]
        public async Task JsSplitterMove(int index, int size, int nextSize)
        {
            var panel = FindPanel(index);
            if (panel != null)
            {
                await panel.ApplyDragSizeAsync(size);
            }

            await ApplyNeighborSizeAsync(index, nextSize);
        }

        /// <summary>拖拽结束（JS 调用）：提交尺寸、触发 resize-end。</summary>
        [JSInvokable]
        public async Task JsSplitterEnd(int index, int size, int nextSize)
        {
            var panel = FindPanel(index);
            if (panel != null)
            {
                await panel.EndDragAsync(size);
            }

            await ApplyNeighborSizeAsync(index, nextSize);
            SetLazyOffset(0);
        }

        /// <summary>
        /// 相邻面板只同步尺寸：不置激活态、不派 resize 事件（拖拽事件只属于被拖的那根分隔条），
        /// 但必须写进 liveSize，否则下一次 Blazor 重渲染会把它弹回原来的百分比。
        /// </summary>
        private Task ApplyNeighborSizeAsync(int index, int nextSize)
        {
            if (nextSize < 0)
            {
                return Task.CompletedTask;
            }

            var neighbor = FindPanel(index + 1);
            return neighbor != null ? neighbor.ApplyNeighborSizeAsync(nextSize) : Task.CompletedTask;
        }

        /// <summary>lazy 模式拖拽位移（JS 调用）：只更新预览线，面板尺寸等松手才变。</summary>
        [JSInvokable]
        public Task JsSplitterLazy(int index, int offset)
        {
            SetLazyOffset(offset);
            return Task.CompletedTask;
        }

        private void SetLazyOffset(double offset)
        {
            if (Math.Abs(lazyOffset - offset) < 0.5)
            {
                return;
            }

            lazyOffset = offset;
            _ = InvokeAsync(StateHasChanged);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            /* 分隔条数量 = 面板数 - 1（最后一个面板不渲染分隔条）。数量变化才重新下发绑定：
               elementSplitterInit 按条幂等，已经绑过的那几根不会重复绑。 */
            var wantBars = Math.Max(0, panels.Count - 1);
            if (wantBars == boundBarCount)
            {
                return;
            }

            dotNetRef ??= DotNetObjectReference.Create(this);
            try
            {
                await JSRuntime.InvokeVoidAsync("elementSplitterInit", RootElement, dotNetRef);
                boundBarCount = wantBars;
            }
            catch (Exception)
            {
                // 无 JS 环境时降级：不能拖拽，折叠按钮仍可用
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (boundBarCount >= 0)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("elementSplitterDispose", RootElement);
                }
                catch (Exception)
                {
                    // 忽略
                }
                boundBarCount = -1;
            }

            dotNetRef?.Dispose();
            dotNetRef = null;
        }
    }
}