using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElCard : ElementComponentBase, IContainerComponent
    {
        internal HtmlPropertyBuilder wrapperClassBuilder;
        internal HtmlPropertyBuilder bodyClassBuilder;

        /// <summary>
        /// Body 上的 Class 、
        /// </summary>
        [Parameter]
        public string BodyCls { get; set; }

        /// <summary>
        /// 阴影类型
        /// </summary>
        [Parameter]
        public ShadowShowType Shadow { get; set; } = ShadowShowType.Always;

        [Parameter]
        public RenderFragment Header { get; set; }

        /// <summary>
        /// EP <c>header</c> 属性：当未提供 <see cref="Header"/> 插槽时，作为标题纯文本渲染（等价于 EP 的 slot fallback）。
        /// 若同时提供两者，插槽优先级更高（与 EP 行为一致）。
        /// </summary>
        [Parameter]
        public string HeaderText { get; set; }

        [Parameter]
        public RenderFragment Body { get; set; }

        [Parameter]
        public string HeaderStyle { get; set; }

        [Parameter]
        public string HeaderCls { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        public ElementReference Container { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            wrapperClassBuilder = CssClassBuilder.CreateCssClassBuilder().Add("el-card", "box-card", $"is-{Shadow.ToString().ToLower()}-shadow", Cls);
            bodyClassBuilder = CssClassBuilder.CreateCssClassBuilder().Add("el-card__body", BodyCls);
        }
    }
}
