using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    public partial class ElEmpty : ElementComponentBase
    {
        private readonly string gradientId = $"el-empty-{System.Guid.NewGuid():N}";

        /// <summary>
        /// 图片地址。传入图片 URL 时优先于 <see cref="ImageContent"/> 插槽渲染；
        /// 留空且未提供插槽时，渲染 EP 同款默认插画（受 --el-empty-fill-color-* 变量控制配色）。
        /// </summary>
        [Parameter]
        public string Image { get; set; }

        /// <summary>图片尺寸（宽度，单位 px）。对齐 EP <c>image-size: number</c>。</summary>
        [Parameter]
        public int? ImageSize { get; set; }

        /// <summary>
        /// 描述文字。留空时回落到 <c>暂无数据</c>（对齐 EP 的 <c>t('el.table.emptyText')</c> 本地化回落语义）。
        /// </summary>
        [Parameter]
        public string Description { get; set; }

        /// <summary>图片插槽（image slot）。</summary>
        [Parameter]
        public RenderFragment ImageContent { get; set; }

        /// <summary>描述插槽（description slot）。</summary>
        [Parameter]
        public RenderFragment DescriptionContent { get; set; }

        /// <summary>底部内容插槽（默认插槽），通常用于放操作按钮。</summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        protected string EmptyClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-empty", Cls)
            .ToString();

        protected string ImageStyle => ImageSize.HasValue
            ? $"width:{ImageSize.Value}px"
            : string.Empty;

        /// <summary>描述最终取值：显式 Description 优先，否则回落到本地化默认「暂无数据」。</summary>
        protected string EmptyDescription =>
            // EP 的 ElEmpty 就是用 t('el.table.emptyText') 兜底（本仓此前直接写中文），键与值都对齐它
            string.IsNullOrWhiteSpace(Description) ? Lang.T("el.table.emptyText", "暂无数据") : Description;

        protected string GradientId => gradientId;
    }
}
