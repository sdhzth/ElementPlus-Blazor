using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// Element Plus Icon 组件 — 字体图标方案，对齐官方 size / color / default slot API
    /// </summary>
    public partial class ElIcon : ElementComponentBase
    {
        private string _iconName;


        /// <summary>通过 Icon 枚举选择图标（推荐，类型安全）</summary>
        [Parameter]
        public Icon Icon { get; set; }

        /// <summary>通过字符串名称选择图标（如 "close"、"user"、"el-icon-delete"）</summary>
        [Parameter]
        public string Name { get; set; }

        /// <summary>图标大小，如 "16px" / "1.5rem" / 数字（自动补 px）。继承字体大小</summary>
        [Parameter]
        public string Size { get; set; }

        /// <summary>图标颜色。继承颜色</summary>
        [Parameter]
        public string Color { get; set; }

        /// <summary>是否旋转加载动画（对应官方 CSS .is-loading）</summary>
        [Parameter]
        public bool Spin { get; set; }

        /// <summary>自定义默认内容（可放 SVG 图标或其他子元素）</summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>点击事件</summary>
        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        /// <summary>无障碍标签（aria-label）</summary>
        [Parameter]
        public string AriaLabel { get; set; }

        /// <summary>鼠标悬停提示（title）</summary>
        [Parameter]
        public string Title { get; set; }

        protected string IconClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-icon", Cls)
            .AddIf(!string.IsNullOrWhiteSpace(_iconName), $"el-icon-{_iconName}")
            .AddIf(Spin, "is-loading")
            .ToString();

        protected string IconStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(!string.IsNullOrWhiteSpace(Size), $"font-size:{ElementCssUtility.NormalizeCssSize(Size)}")
            .AddIf(!string.IsNullOrWhiteSpace(Color), $"color:{Color}")
            .Add(Style)
            .ToString();

        protected override void OnParametersSet()
        {
            // 优先级：Name（string） > Icon（枚举） > null
            if (!string.IsNullOrWhiteSpace(Name))
            {
                _iconName = ElementIconName.Normalize(Name);
            }
            else
            {
                _iconName = ElementIconName.FromEnum(Icon);
            }
        }

        private async Task OnElClickAsync(MouseEventArgs e)
        {
            if (OnClick.HasDelegate)
            {
                await OnClick.InvokeAsync(e);
            }
        }

    }
}