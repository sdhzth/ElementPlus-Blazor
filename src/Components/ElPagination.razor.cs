using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElPagination
    {
        internal bool previousDisabled = false;
        internal bool nextDisabled = false;
        internal bool nextArrow = false;
        internal bool prevArrow = false;

        [Parameter]
        public int Total { get; set; } = 100;

        internal void ShowNext()
        {
            nextArrow = true;
            prevArrow = false;
        }

        internal void HideNext()
        {
            nextArrow = false;
        }

        internal void ShowPrev()
        {
            prevArrow = true;
        }

        internal void HidePrev()
        {
            prevArrow = false;
        }

        [Parameter]
        public int PageSize { get; set; } = 50;

        [Parameter]
        public int CurrentPage { get; set; } = 1;

        [Parameter]
        public bool Background { get; set; } = true;

        [Parameter]
        public ElementSize Size { get; set; } = ElementSize.Default;

        protected ElementSize EffectiveSize => Size == ElementSize.Default ? ConfigSize : Size;

        protected ButtonSize NavigationButtonSize => EffectiveSize switch
        {
            ElementSize.Large => ButtonSize.Large,
            ElementSize.Small => ButtonSize.Small,
            _ => ButtonSize.Default
        };

        [Parameter]
        public int ShowPageCount { get; set; } = 7;

        [Parameter]
        public EventCallback<int> CurrentPageChanged { get; set; }

        [Parameter]
        public EventCallback<int> PageCountChanged { get; set; }

        internal int pageCount;
        private int _lastPageCount;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            // pager-count（ShowPageCount）必须奇数且介于 5~21（对齐 EP pager-count 约束）
            if (ShowPageCount < 5)
            {
                ShowPageCount = 5;
            }
            else if (ShowPageCount > 21)
            {
                ShowPageCount = 21;
            }
            if (ShowPageCount % 2 == 0)
            {
                ShowPageCount = Math.Min(21, ShowPageCount + 1);
            }
            // 总页数：优先使用 page-count（EP 约定「优先使用 page-count」），否则由 total/pageSize 推导
            pageCount = PageCount > 0
                ? PageCount
                : Convert.ToInt32(Math.Ceiling((float)Total / Math.Max(1, PageSize)));
            // 页码总数变化时触发 PageCountChanged（对齐 EP page-count 变更语义）
            if (PageCount != _lastPageCount)
            {
                _lastPageCount = PageCount;
                if (PageCountChanged.HasDelegate)
                {
                    _ = PageCountChanged.InvokeAsync(PageCount);
                }
            }
            if (CurrentPage > pageCount)
            {
                CurrentPage = pageCount;
            }
            if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }
            SwitchButtonStatus();
        }

        internal async Task Jump(int page)
        {
            // 页码越界收口（单一真源）：跳页框手输/步进、快速跳页 +5/-5 都从这里过。
            var maxPage = pageCount > 0 ? pageCount : 1;
            if (page < 1)
            {
                page = 1;
            }
            else if (page > maxPage)
            {
                page = maxPage;
            }
            CurrentPage = page;
            SwitchButtonStatus();
            if (CurrentPageChanged.HasDelegate)
            {
                await CurrentPageChanged.InvokeAsync(page);
            }
            // EP change 事件：current-page 或 page-size 变更时触发，载荷 (currentPage, pageSize)
            if (Change.HasDelegate)
            {
                await Change.InvokeAsync((CurrentPage, PageSize));
            }
        }

        // 分页文案：模板类走 Lang.Template（宿主自定义模板也要插值），纯标签类走 Lang.T / Lang.Text。
        // 词条键与 EP 同名；fallback 一律是"接入词条之前本仓就有的那几个字"，不取词时 DOM 一字不变。
        private string TotalContentText => Lang.Template(
            "el.pagination.total", TotalText, "共 {total} 条", ("total", Total));

        private string PageSizeSuffix => Lang.T("el.pagination.pagesize", "条/页");

        private string JumperPrefixText => Lang.Text("el.pagination.goto", JumperPrefix, "前往");

        private string JumperSuffixText => Lang.Text("el.pagination.page", JumperSuffix, "页");

        /// <summary>跳页输入框的可访问名称：接词条后是 EP 的「第 {pager} 页」并带上真实页码。</summary>
        private string CurrentPageAriaLabel => Lang.T(
            "el.pagination.currentPage", "页码", ("pager", CurrentPage));

        /// <summary>上一页按钮的可访问名称（此前只有图标/自定义文字，读屏说不出用途）。EP 用 el.pagination.prev。</summary>
        private string PrevAriaLabel => Lang.T("el.pagination.prev", "Go to previous page");

        /// <summary>下一页按钮的可访问名称。</summary>
        private string NextAriaLabel => Lang.T("el.pagination.next", "Go to next page");

        /// <summary>点击上一页按钮：回退一页并在页码变更后触发 prev-click（载荷为变更后当前页）。</summary>
        protected async Task OnPrevClickAsync()
        {
            var target = Math.Max(1, CurrentPage - 1);
            if (target == CurrentPage)
            {
                return;
            }
            await Jump(target);
            if (PrevClick.HasDelegate)
            {
                await PrevClick.InvokeAsync(CurrentPage);
            }
        }

        /// <summary>点击下一页按钮：前进一页并在页码变更后触发 next-click（载荷为变更后当前页）。</summary>
        protected async Task OnNextClickAsync()
        {
            var maxPage = pageCount > 0 ? pageCount : 1;
            var target = Math.Min(maxPage, CurrentPage + 1);
            if (target == CurrentPage)
            {
                return;
            }
            await Jump(target);
            if (NextClick.HasDelegate)
            {
                await NextClick.InvokeAsync(CurrentPage);
            }
        }

        /// <summary>是否整体隐藏分页（对齐 EP hide-on-single-page：仅一页时隐藏）。</summary>
        protected bool ShouldHide() => HideOnSinglePage && pageCount <= 1;

        /// <summary>上一页按钮图标：PrevIcon 优先；其次回落 EP 默认箭头（PrevText 非空时改用文字、不显示图标）。</summary>
        protected string GetPrevIcon() => !string.IsNullOrEmpty(PrevIcon)
            ? PrevIcon
            : (string.IsNullOrEmpty(PrevText) ? "el-icon-arrow-left" : null);

        /// <summary>上一页按钮文字：仅当未指定 PrevIcon 且 PrevText 非空时返回。</summary>
        protected string GetPrevText() => string.IsNullOrEmpty(PrevIcon) && !string.IsNullOrEmpty(PrevText) ? PrevText : null;

        /// <summary>下一页按钮图标：NextIcon 优先；其次回落 EP 默认箭头（NextText 非空时改用文字、不显示图标）。</summary>
        protected string GetNextIcon() => !string.IsNullOrEmpty(NextIcon)
            ? NextIcon
            : (string.IsNullOrEmpty(NextText) ? "el-icon-arrow-right" : null);

        /// <summary>下一页按钮文字：仅当未指定 NextIcon 且 NextText 非空时返回。</summary>
        protected string GetNextText() => string.IsNullOrEmpty(NextIcon) && !string.IsNullOrEmpty(NextText) ? NextText : null;

        private void SwitchButtonStatus()
        {
            if (pageCount <= 1)
            {
                previousDisabled = true;
                nextDisabled = true;
                return;
            }
            if (CurrentPage <= 1)
            {
                previousDisabled = true;
                nextDisabled = false;
            }
            else if (CurrentPage >= pageCount)
            {
                previousDisabled = false;
                nextDisabled = true;
            }
            else
            {
                previousDisabled = nextDisabled = false;
            }
        }
    }
}