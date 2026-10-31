using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// ElSplitterPanel — 分隔面板子组件，panel div 和 bar div 是兄弟元素
    /// </summary>
    public partial class ElSplitterPanel : ElementComponentBase
    {
        private bool resizing;
        private bool collapsed;
        private decimal preCollapseSize = 100;
        private decimal? liveSize;

        [CascadingParameter]
        internal ElSplitter Splitter { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public string Size { get; set; }

        [Parameter]
        public string Min { get; set; }

        [Parameter]
        public string Max { get; set; }

        [Parameter]
        public bool Resizable { get; set; } = true;

        [Parameter]
        public bool Collapsible { get; set; }

        [Parameter]
        public EventCallback<string> SizeChanged { get; set; }

        public ElementReference PanelRef { get; set; }

        [Parameter]
        public RenderFragment StartCollapsible { get; set; }

        [Parameter]
        public RenderFragment EndCollapsible { get; set; }

        internal int PanelIndex { get; private set; }
        internal bool IsLastPanel => Splitter != null && PanelIndex >= Splitter.GetPanelCount() - 1;

        internal decimal CurrentPixelSize => collapsed ? 0 : (liveSize ?? (decimal)ParsePixelSize(Size, 200));

        protected string AriaOrientation => Splitter?.Direction == SplitterDirection.Vertical ? "horizontal" : "vertical";

        protected string PanelClass
        {
            get
            {
                var cb = HtmlPropertyBuilder.CreateCssClassBuilder();
                cb.Add("el-splitter-panel");
                cb.AddIf(!string.IsNullOrWhiteSpace(Cls), Cls);
                cb.AddIf(Collapsible, "is-collapsible");
                cb.AddIf(collapsed, "is-collapsed");
                return cb.ToString();
            }
        }

        protected string BarClass
        {
            get
            {
                var cb = HtmlPropertyBuilder.CreateCssClassBuilder();
                cb.Add("el-splitter__bar");
                cb.AddIf(resizing, "is-active");
                cb.AddIf(Collapsible, "is-collapsible");
                return cb.ToString();
            }
        }

        /// <summary>
        /// 拖拽热区（16px，绝对定位覆盖相邻面板各 8px）。带 <c>is-lazy</c> 时额外露出第二条
        /// 半透明分隔线，配合根元素上的 <c>--el-splitter-bar-offset</c> 预览落点（对齐官方 split-bar）。
        /// </summary>
        protected string DraggerClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-splitter__bar-dragger")
            .AddIf(Splitter?.Lazy == true, "is-lazy")
            .ToString();

        protected string PanelStyle
        {
            get
            {
                var size = collapsed ? "0" :
                    (liveSize.HasValue
                        ? $"{liveSize.Value.ToString("0.###", CultureInfo.InvariantCulture)}px"
                        : ElementCssUtility.NormalizeCssSize(Size));

                var horizontal = Splitter?.Direction != SplitterDirection.Vertical;
                var cb = HtmlPropertyBuilder.CreateCssStyleBuilder();
                if (!string.IsNullOrWhiteSpace(size))
                {
                    cb.Add($"flex-basis:{size}");
                    cb.Add(horizontal ? $"width:{size}" : $"height:{size}");
                }
                if (!string.IsNullOrWhiteSpace(Min)) cb.Add($"min-{(horizontal ? "width" : "height")}:{ElementCssUtility.NormalizeCssSize(Min)}");
                if (!string.IsNullOrWhiteSpace(Max)) cb.Add($"max-{(horizontal ? "width" : "height")}:{ElementCssUtility.NormalizeCssSize(Max)}");
                cb.Add(Style);
                return cb.ToString();
            }
        }

        private string SizeAxis => Splitter?.Direction == SplitterDirection.Vertical ? "height" : "width";

        protected override void OnInitialized()
        {
            base.OnInitialized();
            PanelIndex = Splitter?.RegisterPanel(this) ?? 0;
        }

        public new void Dispose() { Splitter?.UnregisterPanel(this); base.Dispose(); }

        // ===== Resize（事件源在 JS 侧：document 级 mousemove，见 dom.js 的 elementSplitterInit）=====
        //
        // 曾经的实现把 onmousedown/onmousemove/onmouseup/onmouseleave 全绑在分隔条这个 6px 的 div 上：
        // 指针一移出这条带子就收不到 mousemove（mouseleave 还会立刻结束拖拽），实测拖 60px 只生效 34px。
        // 现在统一由 JS 在 document 上接管（同时直接写 DOM 保证跟手），这里只做「记状态 + 派事件」。

        /// <summary>拖拽开始（JS 调用）：标记激活态并派发 resize-start。</summary>
        internal async Task BeginDragAsync()
        {
            if (!Resizable)
            {
                return;
            }

            resizing = true;
            if (Splitter != null)
            {
                await Splitter.FireResizeStartAsync(PanelIndex);
            }
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// 拖拽中（JS 节流调用）：记录像素尺寸。JS 已直接把尺寸写到 DOM 上保证跟手，
        /// 这里必须同步 <see cref="liveSize"/>，否则下一次重渲染会把面板弹回原宽。
        /// </summary>
        internal async Task ApplyDragSizeAsync(int size)
        {
            if (!Resizable)
            {
                return;
            }

            liveSize = size < 0 ? 0 : size;
            resizing = true;
            if (SizeChanged.HasDelegate)
            {
                await SizeChanged.InvokeAsync($"{liveSize.Value.ToString("0.###", CultureInfo.InvariantCulture)}px");
            }
            if (Splitter != null)
            {
                await Splitter.FireResizeAsync(PanelIndex);
            }
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// 被相邻分隔条带动时同步尺寸（只改 liveSize，不派事件、不进激活态）。
        /// </summary>
        internal async Task ApplyNeighborSizeAsync(int size)
        {
            liveSize = size < 0 ? 0 : size;
            if (SizeChanged.HasDelegate)
            {
                await SizeChanged.InvokeAsync($"{liveSize.Value.ToString("0.###", CultureInfo.InvariantCulture)}px");
            }

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>拖拽结束（JS 调用）：提交最终尺寸并派发 resize-end。</summary>
        internal async Task EndDragAsync(int size)
        {
            if (!Resizable)
            {
                return;
            }

            if (size >= 0)
            {
                liveSize = size;
            }
            resizing = false;
            if (SizeChanged.HasDelegate && liveSize.HasValue)
            {
                await SizeChanged.InvokeAsync($"{liveSize.Value.ToString("0.###", CultureInfo.InvariantCulture)}px");
            }
            if (Splitter != null)
            {
                await Splitter.FireResizeEndAsync(PanelIndex);
            }
            await InvokeAsync(StateHasChanged);
        }

        // ===== Collapse =====

        private async Task OnDoubleClickAsync()
        {
            if (!Collapsible) return;
            await ToggleCollapseAsync();
        }

        public async Task ToggleCollapseAsync()
        {
            if (!Collapsible) return;

            if (!collapsed)
            {
                preCollapseSize = liveSize ?? (decimal)ParsePixelSize(Size, 100);
                liveSize = 0;
                collapsed = true;
            }
            else
            {
                liveSize = preCollapseSize;
                collapsed = false;
            }

            StateHasChanged();
            await Splitter?.FireCollapseAsync(PanelIndex, collapsed);
            if (SizeChanged.HasDelegate)
                await SizeChanged.InvokeAsync(collapsed ? "0px" : $"{liveSize}px");
        }

        public async Task SetCollapsedAsync(bool isCollapsed)
        {
            if (!Collapsible || isCollapsed == collapsed) return;
            await ToggleCollapseAsync();
        }

        private static double ParsePixelSize(string value, double fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            var normalized = ElementCssUtility.NormalizeCssSize(value);
            normalized = Regex.Replace(normalized, "[^0-9.\\-]", string.Empty);
            return double.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }
    }
}