using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// ElScrollbar — 自定义滚动条组件，对齐 Element Plus Scrollbar 官方 API
    /// </summary>
    public partial class ElScrollbar : ElementComponentBase
    {
        private const decimal DefaultThumbSize = 20m;

        /// <summary>竖向 thumb 占轨道高度的百分比（按 视口/内容 比动态测算；0 = 尚未测算）</summary>
        private decimal thumbSizeY;

        /// <summary>横向 thumb 占轨道宽度的百分比（0 = 尚未测算）</summary>
        private decimal thumbSizeX;

        private decimal scrollTopPercentage;
        private decimal scrollLeftPercentage;

        [Parameter] public RenderFragment ChildContent { get; set; }

        /// <summary>滚动条高度（如 "400px"、"50vh"）</summary>
        [Parameter] public string Height { get; set; }

        /// <summary>滚动条最大高度</summary>
        [Parameter] public string MaxHeight { get; set; }

        /// <summary>是否使用浏览器原生滚动条样式</summary>
        [Parameter] public bool Native { get; set; }

        /// <summary>不响应容器尺寸变化（容器尺寸不变时可优化性能）</summary>
        [Parameter] public bool Noresize { get; set; }

        /// <summary>包裹容器的自定义样式</summary>
        [Parameter] public string WrapStyle { get; set; }

        /// <summary>包裹容器的自定义类名（wrap-class）</summary>
        [Parameter] public string WrapClassName { get; set; }

        /// <summary>视图的自定义样式</summary>
        [Parameter] public string ViewStyle { get; set; }

        /// <summary>视图的自定义类名（view-class）</summary>
        [Parameter] public string ViewClassName { get; set; }

        /// <summary>滚动条总是显示</summary>
        [Parameter] public bool Always { get; set; }

        /// <summary>滚动条最小尺寸，默认 20（单位：百分比）</summary>
        [Parameter] public int MinSize { get; set; } = 20;

        /// <summary>视图的元素标签，默认 div</summary>
        [Parameter] public string Tag { get; set; } = "div";

        /// <summary>触发到达边缘事件的距离（像素），默认 0（刚好到达）</summary>
        [Parameter] public int Distance { get; set; } = 0;

        /// <summary>视图的 ID（a11y）</summary>
        [Parameter] public string Id { get; set; }

        /// <summary>容器的 role（a11y）</summary>
        [Parameter] public string Role { get; set; }

        /// <summary>视图的 aria-label（a11y）</summary>
        [Parameter] public string AriaLabel { get; set; }

        /// <summary>视图的 aria-orientation（a11y）</summary>
        [Parameter] public string AriaOrientation { get; set; }

        /// <summary>容器的 tabindex</summary>
        [Parameter] public int? Tabindex { get; set; }

        /// <summary>滚动事件 — 返回 ScrollEventArgs（含 scrollTop/scrollLeft 及百分比）</summary>
        [Parameter] public EventCallback<ScrollEventArgs> OnScroll { get; set; }

        /// <summary>
        /// 滚动到达边缘事件（2.10.0+ 无限滚动）
        /// 参数 ScrollbarDirection：Top/Bottom/Left/Right
        /// </summary>
        [Parameter] public EventCallback<ScrollEndEventArgs> EndReached { get; set; }

        /// <summary>滚动条包裹元素的引用（官方 wrapRef）</summary>
        public ElementReference WrapElement { get; set; }

        /// <summary>根容器引用（用于接管落在自定义滚动条上的滚轮）</summary>
        private ElementReference RootElement { get; set; }

        /// <summary>JSRuntime 注入</summary>
        [Inject] private IJSRuntime JSRuntime { get; set; }

        protected string EffectiveTag => string.IsNullOrWhiteSpace(Tag) ? "div" : Tag;

private IDictionary<string, object> ViewParameters
{
    get
    {
        var extraAttrs = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(Id)) extraAttrs["id"] = Id;
        if (!string.IsNullOrWhiteSpace(AriaLabel)) extraAttrs["aria-label"] = AriaLabel;
        if (!string.IsNullOrWhiteSpace(AriaOrientation)) extraAttrs["aria-orientation"] = AriaOrientation;

        return new Dictionary<string, object>
        {
            [nameof(ElementTagRenderer.Tag)] = EffectiveTag,
            [nameof(ElementTagRenderer.ChildContent)] = ChildContent,
            [nameof(ElementTagRenderer.Attributes)] = extraAttrs,
            [nameof(ElementTagRenderer.Class)] = ViewClass,
            [nameof(ElementTagRenderer.Style)] = ViewStyle
        };
    }
}

        protected string ScrollbarClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-scrollbar", Cls)
            .AddIf(Always, "is-always")
            .AddIf(Native, "is-native")
            .ToString();

        protected string ScrollbarStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(!string.IsNullOrWhiteSpace(Height), $"height:{ElementCssUtility.NormalizeCssSize(Height)}")
            .AddIf(!string.IsNullOrWhiteSpace(MaxHeight), $"max-height:{ElementCssUtility.NormalizeCssSize(MaxHeight)}")
            .Add(Style)
            .ToString();

        protected string WrapClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-scrollbar__wrap", WrapClassName)
            .AddIf(!Native, "el-scrollbar__wrap--hidden-default")
            .ToString();

        protected string ViewClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-scrollbar__view", ViewClassName)
            .ToString();

        protected string BarVisibilityStyle => Always ? "opacity:1" : null;

        // 注意：thumb 用 transform 位移，而 translate 的百分比是相对**thumb 自身**尺寸，
        // 不是相对轨道。所以位移必须换算成 ratio × (100 - size) / size，否则滚到底时
        // thumb 只能挪动自身高度、永远到不了轨道底部。
        protected string HorizontalThumbStyle =>
            $"width:{ThumbSizeX.ToString("0.###", CultureInfo.InvariantCulture)}%;transform:translateX({MovePercent(scrollLeftPercentage, ThumbSizeX).ToString("0.###", CultureInfo.InvariantCulture)}%)";

        protected string VerticalThumbStyle =>
            $"height:{ThumbSizeY.ToString("0.###", CultureInfo.InvariantCulture)}%;transform:translateY({MovePercent(scrollTopPercentage, ThumbSizeY).ToString("0.###", CultureInfo.InvariantCulture)}%)";

        /// <summary>最小 thumb 尺寸（%），对齐官方 min-size，默认 20</summary>
        private decimal MinThumb => MinSize <= 0 ? DefaultThumbSize : MinSize;

        /// <summary>尚未从 JS 测算时退回最小尺寸（保证未滚动时也有可用外观）</summary>
        private decimal ThumbSizeY => thumbSizeY > 0 ? thumbSizeY : MinThumb;

        private decimal ThumbSizeX => thumbSizeX > 0 ? thumbSizeX : MinThumb;

        private async Task OnScrollAsync(EventArgs args)
        {
            var state = await JSRuntime.InvokeAsync<ScrollbarState>("elementScrollbarGetState", WrapElement);
            UpdateThumbFromState(state);

            if (OnScroll.HasDelegate)
            {
                var ea = new ScrollEventArgs
                {
                    ScrollTop = state.ScrollTop,
                    ScrollLeft = state.ScrollLeft,
                    ScrollHeight = state.ScrollHeight,
                    ScrollWidth = state.ScrollWidth,
                    ClientHeight = state.ClientHeight,
                    ClientWidth = state.ClientWidth,
                    TopPercentage = scrollTopPercentage,
                    LeftPercentage = scrollLeftPercentage
                };
                await OnScroll.InvokeAsync(ea);
            }

            if (EndReached.HasDelegate)
            {
                await CheckEndReachedAsync(state);
            }
        }

        /// <summary>
        /// 检查是否到达边缘，触发 EndReached 事件（可多次触发不同方向）
        /// </summary>
        private async Task CheckEndReachedAsync(ScrollbarState state)
        {
            if (state.ScrollHeight - state.ClientHeight > Distance)
            {
                // 垂直方向
                if (state.ScrollTop <= Distance)
                    await EndReached.InvokeAsync(new ScrollEndEventArgs { Direction = ScrollbarDirection.Top });
                if (state.ScrollTop >= state.ScrollHeight - state.ClientHeight - Distance)
                    await EndReached.InvokeAsync(new ScrollEndEventArgs { Direction = ScrollbarDirection.Bottom });
            }

            if (state.ScrollWidth - state.ClientWidth > Distance)
            {
                // 水平方向
                if (state.ScrollLeft <= Distance)
                    await EndReached.InvokeAsync(new ScrollEndEventArgs { Direction = ScrollbarDirection.Left });
                if (state.ScrollLeft >= state.ScrollWidth - state.ClientWidth - Distance)
                    await EndReached.InvokeAsync(new ScrollEndEventArgs { Direction = ScrollbarDirection.Right });
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            // 接管落在自定义滚动条（.el-scrollbar__bar）上的滚轮，避免事件冒泡到页面导致整页滚动。
            // bar 是 wrap 的兄弟节点，wrap 上的原生 wheel 不受影响。
            try
            {
                await JSRuntime.InvokeVoidAsync("elementScrollbarBindWheel", RootElement, WrapElement);

                // 首帧按真实「视口/内容」比测算 thumb 尺寸与位置（未滚动时也要正确）
                if (firstRender)
                {
                    var state = await JSRuntime.InvokeAsync<ScrollbarState>("elementScrollbarGetState", WrapElement);
                    UpdateThumbFromState(state);
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch
            {
                // JS 未加载 / 预渲染阶段：静默忽略，不影响渲染
            }
        }

        // ===== 公开方法（对齐官方 Exposes）=====

        /// <summary>滚动到指定坐标</summary>
        public async Task ScrollToAsync(int top, int left = 0)
            => await JSRuntime.InvokeVoidAsync("elementScrollbarScrollTo", WrapElement, top, left);

        /// <summary>设置滚动条到顶部的距离</summary>
        public async Task SetScrollTopAsync(int top)
            => await JSRuntime.InvokeVoidAsync("elementScrollbarSetScrollTop", WrapElement, top);

        /// <summary>设置滚动条到左边的距离</summary>
        public async Task SetScrollLeftAsync(int left)
            => await JSRuntime.InvokeVoidAsync("elementScrollbarSetScrollLeft", WrapElement, left);

        /// <summary>获取当前滚动状态（对齐官方 handleScroll 返回值）</summary>
        public async Task<ScrollEventArgs> GetScrollStateAsync()
        {
            var state = await JSRuntime.InvokeAsync<ScrollbarState>("elementScrollbarGetState", WrapElement);
            return new ScrollEventArgs
            {
                ScrollTop = state.ScrollTop,
                ScrollLeft = state.ScrollLeft,
                ScrollHeight = state.ScrollHeight,
                ScrollWidth = state.ScrollWidth,
                ClientHeight = state.ClientHeight,
                ClientWidth = state.ClientWidth,
                TopPercentage = CalculateOffset(state.ScrollTop, state.ScrollHeight, state.ClientHeight),
                LeftPercentage = CalculateOffset(state.ScrollLeft, state.ScrollWidth, state.ClientWidth)
            };
        }

        /// <summary>手动更新滚动条状态（官方 update）</summary>
        public async Task UpdateAsync()
        {
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>手动触发滚动事件（官方 handleScroll）</summary>
        public async Task HandleScrollAsync()
        {
            await OnScrollAsync(EventArgs.Empty);
        }

        private static decimal CalculateOffset(decimal current, decimal scrollSize, decimal clientSize)
        {
            var max = scrollSize - clientSize;
            return max <= 0 ? 0 : current / max * 100;
        }

        /// <summary>
        /// 按「视口 / 内容」比测算 thumb 长度百分比，对齐官方 Math.max(ratio * 100, minSize)。
        /// 内容未超出视口时占满轨道（100%）。
        /// </summary>
        private decimal CalcThumbSize(decimal clientSize, decimal scrollSize)
        {
            if (scrollSize <= clientSize || scrollSize <= 0)
            {
                return 100m;
            }

            var minSize = MinSize <= 0 ? DefaultThumbSize : MinSize;
            return Math.Max(minSize, clientSize / scrollSize * 100m);
        }

        /// <summary>
        /// 把「滚动比例(%)」换算成 thumb 的 translate 百分比。
        /// translate 的百分比相对 thumb 自身尺寸 ⇒ 位移 = ratio × (轨道 - thumb) / thumb
        ///                     = ratio × (100 - size) / size。
        /// 这样 ratio=100（滚到底）时 thumb 底边恰好落到轨道底部。
        /// </summary>
        private static decimal MovePercent(decimal ratioPercent, decimal sizePercent)
        {
            if (sizePercent <= 0 || sizePercent >= 100m)
            {
                return 0m;
            }

            return ratioPercent * (100m - sizePercent) / sizePercent;
        }

        /// <summary>按滚动状态刷新 thumb 尺寸与位移（滚动时与首帧共用）</summary>
        private void UpdateThumbFromState(ScrollbarState state)
        {
            if (state == null)
            {
                return;
            }

            thumbSizeY = CalcThumbSize(state.ClientHeight, state.ScrollHeight);
            thumbSizeX = CalcThumbSize(state.ClientWidth, state.ScrollWidth);
            scrollTopPercentage = CalculateOffset(state.ScrollTop, state.ScrollHeight, state.ClientHeight);
            scrollLeftPercentage = CalculateOffset(state.ScrollLeft, state.ScrollWidth, state.ClientWidth);
        }

        private sealed class ScrollbarState
        {
            public decimal ScrollTop { get; set; }
            public decimal ScrollLeft { get; set; }
            public decimal ScrollHeight { get; set; }
            public decimal ScrollWidth { get; set; }
            public decimal ClientHeight { get; set; }
            public decimal ClientWidth { get; set; }
        }
    }
}