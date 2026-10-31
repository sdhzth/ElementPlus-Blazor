using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElRibbon : ElementComponentBase
    {
        private HtmlPropertyBuilder styleBuilder;
        private HtmlPropertyBuilder menuStyleBuilder;
        private ElTabPane menuTabPanel;

        [Parameter]
        public Type Menu { get; set; }
        [Parameter]
        public RenderFragment Tabs { get; set; }

        /// <summary>
        /// 场景能力。
        /// </summary>
        [Parameter]
        public RenderFragment Body { get; set; }

        /// <summary>
        /// 初始激活的功能区页签（对应 <c>Tabs</c> 中某个 <c>ElTabPane</c> 的 <c>Name</c>）。
        /// 透传给内部 <c>ElTabs</c>，避免首屏默认激活「开始」菜单页签而自动弹出菜单层。
        /// 不设置时 <c>ElTabs</c> 会激活首个页签（即菜单页签），从而触发菜单弹出。
        /// </summary>
        [Parameter]
        public string ModelValue { get; set; }
        /// <summary>
        /// 菜单背景颜色
        /// </summary>
        [Parameter]
        public string MenuBackgroundColor { get; set; }

        /// <summary>
        /// 菜单背景颜色
        /// </summary>
        [Parameter]
        public string MenuColor { get; set; }

        /// <summary>
        /// 菜单文字
        /// </summary>
        [Parameter]
        public string MenuText { get; set; }
        /// <summary>
        /// 导航区高度
        /// </summary>
        [Parameter]
        public int NavigationHeight { get; set; }

        protected override void OnParametersSet()
        {
            styleBuilder = HtmlPropertyBuilder.CreateCssStyleBuilder()
                .AddIf(NavigationHeight <= 0, "height:160px")
                .AddIf(NavigationHeight > 0, $"height:{NavigationHeight.ToString()}px");
            menuStyleBuilder = HtmlPropertyBuilder.CreateCssStyleBuilder()
                .AddIf(string.IsNullOrEmpty(MenuBackgroundColor), "background-color:var(--el-color-primary, #409eff);")
                .AddIf(!string.IsNullOrEmpty(MenuBackgroundColor), $"background-color:{MenuBackgroundColor}")
                .AddIf(!string.IsNullOrEmpty(MenuText), $"color:{MenuText}")
                .AddIf(string.IsNullOrEmpty(MenuText), "color:var(--el-color-white, #fff);")
                .Add("border-radus:3px");
        }

    /// <summary>菜单页签的固定 Name，用于稳定识别（避免用中文 Title 比较时因文件编码损坏而失效）。</summary>
    private const string RibbonMenuTabName = "__ribbon_menu__";

    private async System.Threading.Tasks.Task PreventMenuSwitchAsync(ElementChangeEventArgs<ElTabPane> eventArg)
    {
        // 菜单页签（固定 Name）被激活即弹出菜单层。
        // 用 Name 而非 Title 比较，可避免 Title 中文在文件编码异常时被破坏而导致比较失效（菜单打不开）。
        if (eventArg.NewValue != null && eventArg.NewValue.Name == RibbonMenuTabName)
        {
            await ShowMenuAsync(eventArg);
        }
    }

        private async Task ShowMenuAsync(ElementChangeEventArgs<ElTabPane> eventArg)
        {
            eventArg.DisallowChange = true;
            var rect = await menuTabPanel.Element.Dom(JSRuntime).GetBoundingClientRectAsync();
            var point = new System.Drawing.PointF(rect.X, rect.Y + rect.Height);
            await DialogService.ShowLayerAsync<DialogResult>(Menu, 0, new Dictionary<string, object>(), point);
        }
    }
}
