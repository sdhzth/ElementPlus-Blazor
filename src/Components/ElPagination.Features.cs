using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElPagination : ElementComponentBase
    {
        [Parameter]
        public string Layout { get; set; } = "prev, pager, next";

        [Parameter]
        public int[] PageSizes { get; set; } = { 10, 20, 50, 100 };

        [Parameter]
        public EventCallback<int> PageSizeChanged { get; set; }

        [Parameter]
        public bool HideOnSinglePage { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public string PrevText { get; set; }

        [Parameter]
        public string NextText { get; set; }

        /// <summary>
        /// 总数文案<b>模板</b>（含 <c>{total}</c> 占位符）。留空时取词条 <c>el.pagination.total</c>，再回落 <c>共 {total} 条</c>；宿主自己给的模板照样会被插值。
        /// </summary>
        [Parameter]
        public string TotalText { get; set; }

        /// <summary>跳页前缀。留空时取词条 <c>el.pagination.goto</c>，再回落「前往」。</summary>
        [Parameter]
        public string JumperPrefix { get; set; }

        /// <summary>跳页后缀。留空时取词条 <c>el.pagination.page</c>，再回落「页」。</summary>
        [Parameter]
        public string JumperSuffix { get; set; }

        /// <summary>是否显示跳页输入框（ElInputNumber）。为 false 时整块「前往 N 页」不渲染。</summary>
        [Parameter]
        public bool ShowJumper { get; set; } = true;

        /// <summary>是否显示跳页输入框右侧的 ± 步进按钮。</summary>
        [Parameter]
        public bool ShowJumperControls { get; set; } = true;

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        /// <summary>
        /// 总页数。与 <see cref="Total"/> 二选一即可（EP：优先使用 page-count）。
        /// 设置了 PageCount 后无需再传 Total；若同时需要 page-sizes 切换，则仍须用 Total。
        /// </summary>
        [Parameter]
        public int PageCount { get; set; }

        /// <summary>别名：对齐 EP <c>pager-count</c>（最大页码按钮数，须为 5~21 的奇数）。与 <see cref="ShowPageCount"/> 同源。</summary>
        [Parameter]
        public int PagerCount
        {
            get => ShowPageCount;
            set => ShowPageCount = value;
        }

        /// <summary>每页条数选择器下拉框的 popper 类名（对齐 EP <c>popper-class</c>）。</summary>
        [Parameter]
        public string PopperClass { get; set; }

        /// <summary>上一页图标（字符串图标类），优先级高于 <see cref="PrevText"/>；两者皆空时回落 EP 默认箭头。</summary>
        [Parameter]
        public string PrevIcon { get; set; }

        /// <summary>下一页图标（字符串图标类），优先级高于 <see cref="NextText"/>；两者皆空时回落 EP 默认箭头。</summary>
        [Parameter]
        public string NextIcon { get; set; }

        /// <summary>total 插槽（对齐 EP <c>#total</c>），优先级高于 <see cref="TotalText"/>。</summary>
        [Parameter]
        public RenderFragment TotalContent { get; set; }

        /// <summary>prev 插槽（对齐 EP <c>#prev</c>），优先级高于 <see cref="PrevIcon"/>/<see cref="PrevText"/>。</summary>
        [Parameter]
        public RenderFragment PrevContent { get; set; }

        /// <summary>next 插槽（对齐 EP <c>#next</c>），优先级高于 <see cref="NextIcon"/>/<see cref="NextText"/>。</summary>
        [Parameter]
        public RenderFragment NextContent { get; set; }

        /// <summary>default 插槽内容，对应 layout 中的 <c>slot</c> 标记。</summary>
        [Parameter]
        public RenderFragment SlotContent { get; set; }

        /// <summary>用户点击上一页按钮、页码变更后触发（对齐 EP <c>prev-click</c>）。参数为变更后的当前页。</summary>
        [Parameter]
        public EventCallback<int> PrevClick { get; set; }

        /// <summary>用户点击下一页按钮、页码变更后触发（对齐 EP <c>next-click</c>）。参数为变更后的当前页。</summary>
        [Parameter]
        public EventCallback<int> NextClick { get; set; }

        /// <summary>current-page 或 page-size 变更后触发（对齐 EP <c>change</c>）。参数为 (currentPage, pageSize)。</summary>
        [Parameter]
        public EventCallback<(int CurrentPage, int PageSize)> Change { get; set; }

        protected IReadOnlyList<PaginationLayoutPart> LayoutParts => (Layout ?? "prev, pager, next")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim().ToLowerInvariant())
            .Select(x => x switch
            {
                "prev" => PaginationLayoutPart.Prev,
                "pager" => PaginationLayoutPart.Pager,
                "next" => PaginationLayoutPart.Next,
                "jumper" => PaginationLayoutPart.Jumper,
                "total" => PaginationLayoutPart.Total,
                "sizes" => PaginationLayoutPart.Sizes,
                "->" => PaginationLayoutPart.RightWrapper,
                "slot" => PaginationLayoutPart.Slot,
                _ => (PaginationLayoutPart?)null
            })
            .Where(x => x.HasValue)
            .Select(x => x.Value)
            .ToList();

        /// <summary>"->" 之前（含）的布局元素，正常顺序渲染。</summary>
        protected IReadOnlyList<PaginationLayoutPart> MainParts
        {
            get
            {
                var list = LayoutParts.ToList();
                var idx = list.IndexOf(PaginationLayoutPart.RightWrapper);
                return idx < 0 ? list : list.Take(idx).ToList();
            }
        }

        /// <summary>"->" 之后的布局元素，整体包进 <c>.el-pagination__rightwrapper</c> 右对齐（EP layout 分隔符语义）。</summary>
        protected IReadOnlyList<PaginationLayoutPart> RightParts
        {
            get
            {
                var list = LayoutParts.ToList();
                var idx = list.IndexOf(PaginationLayoutPart.RightWrapper);
                return idx < 0 ? Array.Empty<PaginationLayoutPart>() : list.Skip(idx + 1).ToList();
            }
        }

        /// <summary>跳页框改用 ElInputNumber，回调收 decimal?。</summary>
        protected async Task OnJumperNumberChangedAsync(decimal? value)
        {
            if (value.HasValue)
            {
                await Jump((int)value.Value);
            }
        }

        /// <summary>每页条数改用 ElSelect（EP 原生下拉，替代裸 select）。</summary>
        protected async Task OnPageSizeValueChangedAsync(int size)
        {
            if (size <= 0)
            {
                return;
            }

            PageSize = size;
            CurrentPage = 1;
            pageCount = Convert.ToInt32(Math.Ceiling((float)Total / Math.Max(1, PageSize)));
            SwitchButtonStatus();
            if (PageSizeChanged.HasDelegate)
            {
                await PageSizeChanged.InvokeAsync(size);
            }

            if (CurrentPageChanged.HasDelegate)
            {
                await CurrentPageChanged.InvokeAsync(CurrentPage);
            }
            if (Change.HasDelegate)
            {
                await Change.InvokeAsync((CurrentPage, PageSize));
            }
        }
    }
}
