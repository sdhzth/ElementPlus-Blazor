using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    /// <summary>
    /// 菜单项分组（Element Plus 自渲染版）。对齐 EP <c>el-menu-item-group</c>：
    /// 属性 title；插槽 default（仅 Menu-Item）/ title。
    /// DOM：<c>li.el-menu-item-group &gt; div.el-menu-item-group__title + ul</c>。
    /// 分组本身不参与注册，内部的 <see cref="ElMenuItem"/> 仍注册到外层菜单容器。
    /// </summary>
    public partial class ElMenuItemGroup : ElementComponentBase
    {
        /// <summary>EP <c>title</c>：组标题。</summary>
        [Parameter]
        public string Title { get; set; }

        /// <summary>EP <c>title</c> 插槽：自定义组标题内容（优先于 <see cref="Title"/>）。</summary>
        [Parameter]
        public RenderFragment TitleTemplate { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        protected override bool ShouldRender()
        {
            return true;
        }
    }
}
