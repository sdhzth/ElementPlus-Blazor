using Microsoft.AspNetCore.Components;
using System.Globalization;
using System.Linq;

namespace ElementPlus
{
    public partial class ElCarouselItem : ElementComponentBase
    {
        [CascadingParameter]
        public ElCarousel Carousel { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public int Order { get; set; }

        /// <summary>幻灯片的名字，可用作父级 <see cref="ElCarousel.SetActiveItem(object)"/> 的参数。对齐 Element Plus <c>el-carousel-item</c> 的 <c>name</c>。</summary>
        [Parameter]
        public string Name { get; set; }

        /// <summary>该幻灯片所对应指示器的文本；设置后指示器内显示该文字而非圆点。对齐 Element Plus <c>el-carousel-item</c> 的 <c>label</c>。</summary>
        [Parameter]
        public string Label { get; set; }

        internal bool IsActive => Carousel?.Items.ToList().IndexOf(this) == Carousel?.ActiveIndex;

        /// <summary>是否处于卡片模式（由父级 <see cref="ElCarousel.Type"/> 决定）。</summary>
        private bool IsCardMode => Carousel?.IsCardType == true;

        /// <summary>卡片模式下本项相对当前激活项的偏移（index - activeIndex），驱动两侧卡片平移与缩放。</summary>
        private int CardOffset => Carousel == null ? 0 : Carousel.Items.ToList().IndexOf(this) - Carousel.ActiveIndex;

        /// <summary>仅当父级 <see cref="ElCarousel.PreviewOnClick"/> 开启时挂接单击放大；卡片模式下非激活项单击切换；否则为空（Blazor 不挂监听，单击无反应）。</summary>
        protected EventCallback ItemClickHandler
        {
            get
            {
                if (Carousel == null)
                {
                    return default;
                }

                if (IsCardMode && !IsActive)
                {
                    var target = Carousel.Items.ToList().IndexOf(this);
                    return EventCallback.Factory.Create(this, () => Carousel.SetActiveIndexAsync(target));
                }

                if (Carousel.PreviewOnClick)
                {
                    return EventCallback.Factory.Create(this, () => Carousel.OnItemClickAsync(this));
                }

                return default;
            }
        }

        protected string ItemClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-carousel__item", Cls)
            .AddIf(IsActive, "is-active")
            .ToString();

        protected string ItemStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(!IsCardMode && !IsActive, "display:none")
            .AddIf(IsCardMode, CardTransform)
            .Add(Style)
            .ToString();

        private string CardTransform
        {
            get
            {
                if (Carousel == null)
                {
                    return string.Empty;
                }

                var scale = IsActive ? 1d : Carousel.CardScale;
                var z = IsActive ? 2 : 1;
                return $"transform:translateX(calc(-50% + {CardOffset * 100}%)) scale({scale.ToString("0.##", CultureInfo.InvariantCulture)});z-index:{z};";
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Carousel?.AddItem(this);
        }

        public override void Dispose()
        {
            Carousel?.RemoveItem(this);
            base.Dispose();
        }
    }
}
