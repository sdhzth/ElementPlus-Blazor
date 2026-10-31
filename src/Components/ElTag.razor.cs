using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    public partial class ElTag : ElementComponentBase
    {
        private bool isClosed = false;
        private HtmlPropertyBuilder iconCssBuilder;
        private HtmlPropertyBuilder tagCssBuilder;
        private HtmlPropertyBuilder tagStyleBuilder;

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>是否可关闭</summary>
        [Parameter]
        public bool Closable { get; set; }

        /// <summary>标签类型：primary/success/info/warning/danger（默认 primary，对齐官方）</summary>
        [Parameter]
        public TagType Type { get; set; } = TagType.Primary;

        /// <summary>主题：Dark/Light/Plain（对齐官方 effect，默认 Light）</summary>
        [Parameter]
        public TagTheme Theme { get; set; } = TagTheme.Light;

        [Parameter]
        public TagSize Size { get; set; }

        /// <summary>自定义背景色</summary>
        [Parameter]
        public string Color { get; set; }

        /// <summary>前置图标 class</summary>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>自定义关闭图标 class（默认 el-icon-close）</summary>
        [Parameter]
        public string CloseIcon { get; set; } = "el-icon-close";

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        [Parameter]
        public EventCallback OnClick { get; set; }

        /// <summary>关闭前拦截（返回 false 可阻止关闭）</summary>
        [Parameter]
        public Func<Task<bool>> OnBeforeClose { get; set; }

        /// <summary>关闭后触发（旧拼写，建议使用 OnClose）</summary>
        [Obsolete("请使用 OnClose")]
        [Parameter]
        public EventCallback OnAfteClose { get; set; }

        // ========== 方法 ==========

        private async Task OnTagClick()
        {
            if (OnClick.HasDelegate)
            {
                await OnClick.InvokeAsync(null);
            }
        }

        private async Task OnCloseClick()
        {
            if (OnBeforeClose != null && !await OnBeforeClose())
            {
                return;
            }

            isClosed = true;
            StateHasChanged();
#pragma warning disable CS0618
            if (OnAfteClose.HasDelegate) await OnAfteClose.InvokeAsync(null);
#pragma warning restore CS0618
        }

        protected override void OnParametersSet()
        {
            var sz = Size.ToString().ToLower();
            if (sz == "medium") sz = "default";

            iconCssBuilder = HtmlPropertyBuilder.CreateCssClassBuilder()
                .AddIf(Closable, "el-tag__close")
                .AddIf(Closable && !string.IsNullOrEmpty(CloseIcon), CloseIcon);

            tagCssBuilder = HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-tag", $"el-tag--{sz}", $"el-tag--{Theme.ToString().ToLower()}", $"el-tag--{Type.ToString().ToLower()}");

            tagStyleBuilder = HtmlPropertyBuilder.CreateCssStyleBuilder();
            if (!string.IsNullOrEmpty(Color))
            {
                tagStyleBuilder.Add("background-color:" + Color);
                tagStyleBuilder.Add("border-color:" + Color);
                tagStyleBuilder.Add("color:#fff");
            }
            tagStyleBuilder.AddIf(!string.IsNullOrEmpty(Style), Style.Split(';'));
        }
    }
}