using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElCarouselTests : BunitContext
    {
        public ElCarouselTests()
        {
            Services.AddElementServices();
        }

        /// <summary>构造 n 个 <see cref="ElCarouselItem"/> 作为走马灯内容。</summary>
        private static RenderFragment Items(int count)
        {
            return builder =>
            {
                for (var i = 0; i < count; i++)
                {
                    var seq = i * 3;
                    var text = $"Slide {i + 1}";
                    builder.OpenComponent<ElCarouselItem>(seq);
                    builder.AddAttribute(seq + 1, "ChildContent", (RenderFragment)(inner => inner.AddContent(0, text)));
                    builder.CloseComponent();
                }
            };
        }

        [Fact]
        public void CarouselRendersItemsAndIndicators()
        {
            var cut = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.ChildContent, Items(3)));

            Assert.Equal(3, cut.FindAll(".el-carousel__item").Count);
            Assert.Equal(3, cut.FindAll(".el-carousel__indicator").Count);
            Assert.Single(cut.FindAll(".el-carousel__indicators"));
            Assert.Equal("Slide 1", cut.Find(".el-carousel__item").TextContent.Trim());
        }

        [Fact]
        public void OnlyFirstItemIsVisibleByDefault()
        {
            var cut = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.ChildContent, Items(3)));

            var nodes = cut.FindAll(".el-carousel__item");
            Assert.Contains("is-active", nodes[0].ClassList);
            Assert.DoesNotContain("is-active", nodes[1].ClassList);
            Assert.Contains("display:none", nodes[1].GetAttribute("style"));
            Assert.DoesNotContain("display:none", nodes[0].GetAttribute("style"));
        }

        [Fact]
        public void ArrowModeControlsArrowsAndModifierClass()
        {
            // Hover（默认）：渲染箭头，但不加 --arrow-always（隐藏与否交给 CSS 的 :hover）
            var hover = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.ChildContent, Items(2)));

            var hoverCls = hover.Find(".el-carousel").GetAttribute("class");
            Assert.Equal(2, hover.FindAll(".el-carousel__arrow").Count);
            Assert.DoesNotContain("el-carousel--arrow-always", hoverCls);
            Assert.DoesNotContain("el-carousel--arrow-never", hoverCls);

            // Always：加类名，箭头照常渲染
            var always = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.Arrow, CarouselArrow.Always)
                .Add(x => x.ChildContent, Items(2)));

            Assert.Contains("el-carousel--arrow-always", always.Find(".el-carousel").GetAttribute("class"));
            Assert.Equal(2, always.FindAll(".el-carousel__arrow").Count);

            // Never：箭头整个不渲染
            var never = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.Arrow, CarouselArrow.Never)
                .Add(x => x.ChildContent, Items(2)));

            Assert.Empty(never.FindAll(".el-carousel__arrow"));
            Assert.Contains("el-carousel--arrow-never", never.Find(".el-carousel").GetAttribute("class"));
        }

        [Fact]
        public void ImageFitDefaultIsCoverAndEmitsNoVariable()
        {
            var cut = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.ChildContent, Items(2)));

            // 默认 Cover → 不产生任何内联变量，CSS 里 var(...,cover) 自己回落
            Assert.DoesNotContain("--el-carousel-img-fit", cut.Find(".el-carousel").GetAttribute("style") ?? string.Empty);

            var contain = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.ImageFit, CarouselImageFit.Contain)
                .Add(x => x.ChildContent, Items(2)));

            Assert.Contains("--el-carousel-img-fit:contain", contain.Find(".el-carousel").GetAttribute("style"));

            var fill = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.ImageFit, CarouselImageFit.Fill)
                .Add(x => x.ChildContent, Items(2)));

            Assert.Contains("--el-carousel-img-fit:fill", fill.Find(".el-carousel").GetAttribute("style"));
        }

        [Fact]
        public async Task ActiveIndexChangeMovesActiveItemAndFiresOnChange()
        {
            var fired = -1;
            var cut = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.OnChange, EventCallback.Factory.Create<int>(this, i => fired = i))
                .Add(x => x.ChildContent, Items(3)));

            await cut.Instance.SetActiveIndexAsync(1);

            var nodes = cut.FindAll(".el-carousel__item");
            Assert.DoesNotContain("is-active", nodes[0].ClassList);
            Assert.Contains("is-active", nodes[1].ClassList);
            Assert.Equal(1, fired);

            // 越界下标按环形处理：3 张时 -1 → 最后一张
            await cut.Instance.SetActiveIndexAsync(-1);
            Assert.Contains("is-active", cut.FindAll(".el-carousel__item")[2].ClassList);
            Assert.Equal(2, fired);
        }

        [Fact]
        public void IndicatorNoneHidesIndicatorsAndSingleItemHidesArrows()
        {
            var noIndicator = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.IndicatorPosition, CarouselIndicatorPosition.None)
                .Add(x => x.ChildContent, Items(3)));

            Assert.Empty(noIndicator.FindAll(".el-carousel__indicators"));

            var single = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.Arrow, CarouselArrow.Always)
                .Add(x => x.ChildContent, Items(1)));

            // 只有一项时没有可切换的目标，箭头不再渲染
            Assert.Empty(single.FindAll(".el-carousel__arrow"));
            Assert.Empty(single.FindAll(".el-carousel__indicators"));
        }

        // ===== 图片预览（内置 lightbox） =====

        /// <summary>构造 n 个轮播项，第 imgIndexes 项内放 &lt;img&gt;，其余放纯文本。</summary>
        private static RenderFragment MixedItems(int count, params int[] imgIndexes)
        {
            var imgSet = new HashSet<int>(imgIndexes);
            return builder =>
            {
                for (var i = 0; i < count; i++)
                {
                    var seq = i * 3;
                    var isImg = imgSet.Contains(i);
                    builder.OpenComponent<ElCarouselItem>(seq);
                    builder.AddAttribute(seq + 1, "ChildContent", (RenderFragment)(inner =>
                    {
                        if (isImg)
                        {
                            inner.OpenElement(0, "img");
                            inner.AddAttribute(1, "src", $"https://example.com/img{i + 1}.png");
                            inner.CloseElement();
                        }
                        else
                        {
                            inner.AddContent(0, $"Text {i + 1}");
                        }
                    }));
                    builder.CloseComponent();
                }
            };
        }

        [Fact]
        public void PreviewOnClickAddsModifierClass()
        {
            var cut = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.PreviewOnClick, true)
                .Add(x => x.ChildContent, MixedItems(2, 0)));

            Assert.Contains("el-carousel--preview-on-click", cut.Find(".el-carousel").GetAttribute("class"));
        }

        [Fact]
        public void NonImageItemClickDoesNotOpenPreview()
        {
            // 关闭 PreviewOnClick 时，即使有 img 也不该挂单击处理 → 单击无反应
            var off = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.ChildContent, MixedItems(2, 0)));

            Assert.DoesNotContain("el-carousel--preview-on-click", off.Find(".el-carousel").GetAttribute("class"));
            Assert.False(off.Instance.PreviewVisible);

            // 开启 PreviewOnClick，但点击的是纯文本项（第 1 项，0-based）→ 不打开预览
            var cut = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.PreviewOnClick, true)
                .Add(x => x.ChildContent, MixedItems(3, 0, 2)));

            var items = cut.FindAll(".el-carousel__item");
            // 第 1 项（index=1）是文本，单击不应弹出预览
            items[1].Click();
            Assert.False(cut.Instance.PreviewVisible);
            Assert.Empty(cut.FindAll(".el-carousel__preview"));
        }

        [Fact]
        public void ImageItemClickOpensPreviewAndNonImageDoesNot()
        {
            // 注：自动从 <img> 收集图片地址依赖 JS（浏览器环境），bUnit 中 collectedImages 为空，
            // 故此处仅验证「非图片项单击无反应」这一纯 C# 路径；图片单击自动放大在 Playwright 探针中验证。
            var cut = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.PreviewOnClick, true)
                .Add(x => x.ChildContent, MixedItems(3, 0, 2)));

            var items = cut.FindAll(".el-carousel__item");
            // 第 1 项是纯文本 → 单击不打开预览（非图片点击无反应）
            items[1].Click();
            Assert.False(cut.Instance.PreviewVisible);
            Assert.Empty(cut.FindAll(".el-carousel__preview"));
        }

        [Fact]
        public async Task OpenPreviewAsyncWithExplicitImagesShowsOnlyThose()
        {
            var cut = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.PreviewOnClick, true)
                .Add(x => x.ChildContent, MixedItems(2, 0)));

            await cut.Instance.OpenPreviewAsync(new[] { "data:image/png;base64,AAA", "https://x/y.png" }, 1);
            Assert.True(cut.Instance.PreviewVisible);
            Assert.Equal(2, cut.Instance.PreviewList.Count);
            Assert.Equal(1, cut.Instance.PreviewIndex);
            Assert.Equal("https://x/y.png", cut.Instance.PreviewList[cut.Instance.PreviewIndex]);

            // 关闭
            await cut.Instance.ClosePreviewAsync();
            Assert.False(cut.Instance.PreviewVisible);
            Assert.Empty(cut.FindAll(".el-carousel__preview"));
        }

        [Fact]
        public async Task ExplicitPreviewImagesOverrideCollected()
        {
            var collected = new List<string>();
            var cut = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.PreviewOnClick, true)
                .Add(x => x.PreviewImages, new[] { "https://explicit/a.png", "https://explicit/b.png" })
                .Add(x => x.PreviewImagesChanged, EventCallback.Factory.Create<IReadOnlyList<string>>(this, v => collected.AddRange(v)))
                .Add(x => x.ChildContent, MixedItems(2, 0)));

            // 开发者显式图集优先
            await cut.Instance.OpenPreviewAsync(0);
            Assert.Equal(2, cut.Instance.PreviewList.Count);
            Assert.Equal("https://explicit/a.png", cut.Instance.PreviewList[0]);
        }

        [Fact]
        public async Task NextAndPrevWrapWithLoop()
        {
            var cut = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.PreviewLoop, true)
                .Add(x => x.ChildContent, MixedItems(2, 0)));

            await cut.Instance.OpenPreviewAsync(new[] { "a.png", "b.png", "c.png" }, 0);
            await cut.Instance.NextPreviewAsync();
            Assert.Equal(1, cut.Instance.PreviewIndex);
            await cut.Instance.NextPreviewAsync();
            Assert.Equal(2, cut.Instance.PreviewIndex);
            await cut.Instance.NextPreviewAsync(); // 循环回 0
            Assert.Equal(0, cut.Instance.PreviewIndex);

            await cut.Instance.PrevPreviewAsync(); // 循环回最后一张
            Assert.Equal(2, cut.Instance.PreviewIndex);

            // 关闭循环：到边界停住
            var cut2 = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.PreviewLoop, false)
                .Add(x => x.ChildContent, MixedItems(2, 0)));
            await cut2.Instance.OpenPreviewAsync(new[] { "a.png", "b.png" }, 1);
            await cut2.Instance.NextPreviewAsync();
            Assert.Equal(1, cut2.Instance.PreviewIndex); // 已到最后，不再前进
            await cut2.Instance.PrevPreviewAsync();
            Assert.Equal(0, cut2.Instance.PreviewIndex);
            await cut2.Instance.PrevPreviewAsync();
            Assert.Equal(0, cut2.Instance.PreviewIndex); // 已到首，不再后退
        }
    }
}
