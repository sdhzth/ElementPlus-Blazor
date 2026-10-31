using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    /// <summary>
    /// ElSpace 的子元素包装组件 — 对应官方 el-space__item
    /// </summary>
    public partial class ElSpaceItem : ElementComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>Space 父级（通过 CascadingParameter 注入）</summary>
        [CascadingParameter]
        protected ElSpace ParentSpace { get; set; }

        /// <summary>子元素的自定义类名前缀（来自父级 PrefixCls）</summary>
        protected string ItemClass
        {
            get
            {
                var prefix = ParentSpace?.GetPrefixCls() ?? "";
                var cls = HtmlPropertyBuilder.CreateCssClassBuilder()
                    .Add(string.IsNullOrEmpty(prefix) ? "el-space__item" : $"{prefix}__item")
                    .Add(Cls);
                return cls.ToString();
            }
        }

        /// <summary>是否渲染 separator（最后一个 item 不渲染）</summary>
        protected bool ShouldRenderSeparator => ParentSpace?.Separator != null;

        /// <summary>Separator 模板（来自父级）</summary>
        protected RenderFragment SeparatorContent => ParentSpace?.Separator;
    }
}