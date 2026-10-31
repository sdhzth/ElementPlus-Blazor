namespace ElementPlus
{
    /// <summary>
    /// <see cref="ElCarouselItem"/> 内直接放置的 <c>&lt;img&gt;</c> 如何填充分配到的区域
    /// （对应 CSS <c>object-fit</c>）。
    /// </summary>
    public enum CarouselImageFit
    {
        /// <summary>等比缩放填满，多余部分裁切（默认，与 Element Plus 示例观感一致）。</summary>
        Cover = 0,

        /// <summary>等比缩放完整显示，留白处理（不裁切）。</summary>
        Contain = 1,

        /// <summary>拉伸铺满，不保持比例。</summary>
        Fill = 2,

        /// <summary>不缩放，按图片原始尺寸显示（超出部分被容器裁切）。</summary>
        None = 3
    }
}
