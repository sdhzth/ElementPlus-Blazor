using ElementPlus.Lang;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElCarousel : ElementComponentBase
    {
        private readonly List<ElCarouselItem> items = new List<ElCarouselItem>();
        private Timer timer;
        private bool _disposed;
        private int activeIndex;

        /// <summary>词条变化的重渲染订阅句柄（Dispose 里退订，漏掉就是切一次语言泄漏一批组件）。</summary>
        private IDisposable langSubscription;

        // 预览（内置 lightbox）相关
        private ElementReference rootRef;
        private DotNetObjectReference<ElCarousel> dotNetRef;
        private List<string> collectedImages = new List<string>();
        private List<string> previewList = new List<string>();
        private bool previewVisible;
        private int previewIndex;

        /* ---- 预览缩放 / 平移状态（对应 ElImage 内置 viewer 的 zoom / drag 能力；本次回归修复补回） ---- */
        private const double PreviewMinScale = 0.2;
        private const double PreviewMaxScale = 7;
        private const double PreviewScaleStep = 0.15;

        private double previewScale = 1;
        private double previewOffsetX;
        private double previewOffsetY;
        private bool previewDragging;
        private double previewDragStartX;
        private double previewDragStartY;
        private double previewDragOriginX;
        private double previewDragOriginY;

        // 动态模糊（motion-blur）：切换过程中为容器加 filter，对齐 EP 的 --transitioning 类
        private bool isTransitioning;

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public int InitialIndex { get; set; }

        [Parameter]
        public string Height { get; set; } = "300px";

        [Parameter]
        public bool Autoplay { get; set; } = true;

        [Parameter]
        public int Interval { get; set; } = 3000;

        [Parameter]
        public CarouselArrow Arrow { get; set; } = CarouselArrow.Hover;

        [Parameter]
        public CarouselIndicatorPosition IndicatorPosition { get; set; } = CarouselIndicatorPosition.Inside;

        [Parameter]
        public CarouselDirection Direction { get; set; } = CarouselDirection.Horizontal;

        /// <summary>是否循环显示（默认 true）。对齐 Element Plus <c>loop</c>。</summary>
        [Parameter]
        public bool Loop { get; set; } = true;

        /// <summary>走马灯类型，<see cref="CarouselType.Card"/> 启用卡片模式。对齐 Element Plus <c>type</c>。</summary>
        [Parameter]
        public CarouselType Type { get; set; } = CarouselType.None;

        /// <summary><see cref="Type"/> 为 <see cref="CarouselType.Card"/> 时，二级卡的缩放大小（默认 0.83）。对齐 Element Plus <c>card-scale</c> (2.7.8)。</summary>
        [Parameter]
        public double CardScale { get; set; } = 0.83;

        /// <summary>指示器的触发方式（hover 悬停 / click 点击），默认 hover。对齐 Element Plus <c>trigger</c>。</summary>
        [Parameter]
        public CarouselTrigger Trigger { get; set; } = CarouselTrigger.Hover;

        /// <summary>添加动态模糊以给走马灯注入活力和流畅性（默认 false）。对齐 Element Plus <c>motion-blur</c> (2.6.0)。</summary>
        [Parameter]
        public bool MotionBlur { get; set; }

        /// <summary>鼠标悬停时暂停自动播放 (默认 true)</summary>
        [Parameter]
        public bool PauseOnHover { get; set; } = true;

        /// <summary>指示器自定义 CSS class</summary>
        [Parameter]
        public string IndicatorClass { get; set; }

        /// <summary>左右箭头自定义 CSS class</summary>
        [Parameter]
        public string ArrowClass { get; set; }

        /// <summary>
        /// 轮播项内直接放置的 <c>&lt;img&gt;</c> 如何填充分配到的区域（对应 CSS <c>object-fit</c>）。
        /// 默认 <see cref="CarouselImageFit.Cover"/>（等比铺满并裁切多余部分）。
        /// </summary>
        [Parameter]
        public CarouselImageFit ImageFit { get; set; } = CarouselImageFit.Cover;

        /// <summary>
        /// 单击轮播项内的图片时放大预览（内置 lightbox）。仅当该项含 <c>&lt;img&gt;</c> 时生效，
        /// 非图片内容（文本/组件等）单击无反应。图片地址可为普通 URL 或 base64 data URI。
        /// </summary>
        [Parameter]
        public bool PreviewOnClick { get; set; }

        /// <summary>
        /// 内置预览在上一张/下一张到达边界时是否循环。默认 true。
        /// </summary>
        [Parameter]
        public bool PreviewLoop { get; set; } = true;

        /// <summary>
        /// 开发者显式指定的预览图集（URL 或 base64 data URI）。提供且非空时优先于组件自动从各轮播项 &lt;img&gt; 收集的结果。
        /// </summary>
        [Parameter]
        public IEnumerable<string> PreviewImages { get; set; }

        /// <summary>
        /// 组件自动从各轮播项收集到的图片地址集合（已解析为绝对地址，含 base64）。每次渲染后若发生变化即回调，
        /// 开发者可据此读取 ElCarousel 内的全部图片并自行选择放大。
        /// </summary>
        [Parameter]
        public EventCallback<IReadOnlyList<string>> PreviewImagesChanged { get; set; }

        /// <summary>
        /// 预览打开/切换图片时回调当前预览图在下拉图集中的下标。
        /// </summary>
        [Parameter]
        public EventCallback<int> OnPreviewChanged { get; set; }

        /// <summary>自动收集到的全部图片地址（已解析绝对地址，含 base64）。无图项为 null。开发者可读取用于编程放大。</summary>
        public IReadOnlyList<string> CollectedImages => collectedImages;

        /// <summary>内置 lightbox 是否可见。</summary>
        public bool PreviewVisible => previewVisible;

        /// <summary>当前预览图片在 <see cref="PreviewList"/> 中的下标。</summary>
        public int PreviewIndex => previewIndex;

        /// <summary>当前预览图集（开发者显式指定或自动收集的有效图片）。</summary>
        public IReadOnlyList<string> PreviewList => previewList;

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        // 左右箭头的可读名称：词条键沿用 EP 权威 locale 的 el.carousel.leftArrow / rightArrow。
        // fallback 是本组件改动前就硬编码的「上一张 / 下一张」——不取词时 DOM 一字不变；取到 zh-CN 词条后
        // 才是 EP 措辞「上一张幻灯片」（第二批曾把 zh 值留成本仓原文，第三批已统一为"词条值一律取 EP"）。
        private string LeftArrowAriaLabel => this.Lang.T("el.carousel.leftArrow", "上一张");

        private string RightArrowAriaLabel => this.Lang.T("el.carousel.rightArrow", "下一张");

        /// <summary>
        /// 指示器按钮的可访问名称：EP 用 <c>t('el.carousel.indicator', { index })</c>，本仓此前没有这个属性。
        /// <b>索引跟 EP 一样传 0 基</b>（EP 模板里就是 v-for 的 index）；要 1 基的宿主覆盖词条即可。
        /// </summary>
        private string GetIndicatorAriaLabel(int index) => this.Lang.T(
            "el.carousel.indicator", "Carousel switch to index {index}", ("index", index));

        [Parameter]
        public EventCallback<int> ActiveIndexChanged { get; set; }

        [Parameter]
        public EventCallback<int> OnChange { get; set; }

        internal IReadOnlyList<ElCarouselItem> Items => items.OrderBy(x => x.Order).ToList();

        /// <summary>当前幻灯片的索引（0 起）。对齐 Element Plus Exposes <c>activeIndex</c> (2.7.8)。</summary>
        public int ActiveIndex => activeIndex;

        /// <summary>是否为卡片模式。</summary>
        internal bool IsCardType => Type == CarouselType.Card;

        /// <summary>是否自动高度（<c>Height</c> 为 <c>auto</c> 或未设置）。</summary>
        internal bool IsAutoHeight => string.IsNullOrWhiteSpace(Height) || Height.Trim().Equals("auto", StringComparison.OrdinalIgnoreCase);

        /// <summary>左箭头是否可见：非 Never 且多于 1 项，且（循环 或 不在第一项）。</summary>
        protected bool LeftArrowVisible => Arrow != CarouselArrow.Never && Items.Count > 1 && (Loop || activeIndex > 0);

        /// <summary>右箭头是否可见：非 Never 且多于 1 项，且（循环 或 不在最后一项）。</summary>
        protected bool RightArrowVisible => Arrow != CarouselArrow.Never && Items.Count > 1 && (Loop || activeIndex < Items.Count - 1);

        internal void AddItem(ElCarouselItem item)
        {
            if (!items.Contains(item))
            {
                items.Add(item);
                NormalizeActiveIndex();
                Refresh();
                // 子项注册发生在父级 OnInitialized/OnParametersSet 之后，那两处的 EnsureTimer 见到的
                // Items.Count 还是 0（定时器不启动），此后再无人启动 —— 表现为「Autoplay 完全不生效」。
                // 注册/注销后重新判定，是保证自动播放真正跑起来的唯一时机。
                EnsureTimer();
            }
        }

        internal void RemoveItem(ElCarouselItem item)
        {
            if (items.Remove(item))
            {
                NormalizeActiveIndex();
                Refresh();
                EnsureTimer();
            }
        }

        protected string CarouselClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-carousel", $"el-carousel--{Direction.ToString().ToLower()}", Cls)
            .AddIf(IsCardType, "el-carousel--card")
            .AddIf(MotionBlur, "el-carousel--motion-blur")
            .AddIf(IsAutoHeight, "el-carousel--auto-height")
            .AddIf(Arrow == CarouselArrow.Always, "el-carousel--arrow-always")
            .AddIf(Arrow == CarouselArrow.Never, "el-carousel--arrow-never")
            .AddIf(Direction == CarouselDirection.Vertical && IndicatorPosition == CarouselIndicatorPosition.Outside, "el-carousel--vertical-outside")
            .AddIf(PreviewOnClick, "el-carousel--preview-on-click")
            .ToString();

        protected string CarouselStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(IsCardType, $"--el-carousel-card-scale:{CardScale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}")
            .AddIf(ImageFit != CarouselImageFit.Cover, $"--el-carousel-img-fit:{ImageFit.ToString().ToLowerInvariant()}")
            .Add(Style)
            .ToString();

        protected string ContainerStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(!IsAutoHeight && !string.IsNullOrWhiteSpace(Height), $"height:{ElementCssUtility.NormalizeCssSize(Height)}")
            .AddIf(isTransitioning, $"filter:url('#elCarousel{(Direction == CarouselDirection.Vertical ? "Vertical" : "Horizontal")}')")
            .ToString();

        protected bool ShowArrows => Arrow != CarouselArrow.Never && Items.Count > 1;

        protected string IndicatorsClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-carousel__indicators", $"el-carousel__indicators--{Direction.ToString().ToLower()}")
            .AddIf(Items.Any(x => !string.IsNullOrWhiteSpace(x.Label)), "el-carousel__indicators--labels")
            .AddIf(IndicatorPosition == CarouselIndicatorPosition.Outside, "el-carousel__indicators--outside")
            .ToString();

        protected override void OnInitialized()
        {
            base.OnInitialized();
            activeIndex = Math.Max(0, InitialIndex);
            EnsureTimer();

            // 箭头按钮的 aria-label 取的是应用级词条：Blazor 会跳过参数没变的子组件，所以必须自己订阅
            langSubscription = this.Lang.SubscribeLocaleChanged(() => InvokeAsync(StateHasChanged));
        }

        /// <summary>动态模糊：切换动画开始时给容器加 filter（仅 <see cref="MotionBlur"/> 时生效）。</summary>
        protected void OnContainerTransitionStart()
        {
            if (!MotionBlur)
            {
                return;
            }

            isTransitioning = true;
            Refresh();
        }

        /// <summary>动态模糊：切换动画结束时移除容器 filter。</summary>
        protected void OnContainerTransitionEnd()
        {
            if (!isTransitioning)
            {
                return;
            }

            isTransitioning = false;
            Refresh();
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            EnsureTimer();
        }

        protected string GetIndicatorClass(int index)
        {
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-carousel__indicator", $"el-carousel__indicator--{Direction.ToString().ToLower()}")
                .AddIf(index == activeIndex, "is-active")
                .ToString();
        }

        public async Task SetActiveIndexAsync(int index)
        {
            if (Items.Count == 0)
            {
                activeIndex = 0;
                return;
            }

            activeIndex = Loop
                ? (index + Items.Count) % Items.Count
                : Math.Clamp(index, 0, Items.Count - 1);

            if (ActiveIndexChanged.HasDelegate)
            {
                await ActiveIndexChanged.InvokeAsync(activeIndex);
            }

            if (OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(activeIndex);
            }

            Refresh();
        }

        /// <summary>
        /// 手动切换幻灯片。index 为下标（0 起），或传入某个 <see cref="ElCarouselItem.Name"/> 同名的字符串。
        /// 对齐 Element Plus Exposes <c>setActiveItem</c>。
        /// </summary>
        public async Task SetActiveItem(object target)
        {
            if (target is string name && !string.IsNullOrWhiteSpace(name))
            {
                var ordered = Items.ToList();
                var idx = ordered.FindIndex(x => x.Name == name);
                if (idx >= 0)
                {
                    await SetActiveIndexAsync(idx);
                }

                return;
            }

            if (target is int i)
            {
                await SetActiveIndexAsync(i);
            }
        }

        /// <summary>切换至上一张幻灯片。对齐 Element Plus Exposes <c>prev</c>。</summary>
        public async Task Prev()
        {
            if (Items.Count == 0)
            {
                return;
            }

            if (Loop)
            {
                await SetActiveIndexAsync(activeIndex - 1);
            }
            else if (activeIndex > 0)
            {
                await SetActiveIndexAsync(activeIndex - 1);
            }
        }

        /// <summary>切换至下一张幻灯片。对齐 Element Plus Exposes <c>next</c>。</summary>
        public async Task Next()
        {
            if (Items.Count == 0)
            {
                return;
            }

            if (Loop)
            {
                await SetActiveIndexAsync(activeIndex + 1);
            }
            else if (activeIndex < Items.Count - 1)
            {
                await SetActiveIndexAsync(activeIndex + 1);
            }
        }

        protected async Task ShowPreviousAsync()
        {
            await Prev();
        }

        protected async Task ShowNextAsync()
        {
            await Next();
        }

        protected void PauseTimer()
        {
            if (PauseOnHover) timer?.Stop();
        }

        protected void ResumeTimer()
        {
            if (Autoplay && PauseOnHover && Items.Count > 1)
            {
                timer?.Start();
            }
        }

        private void EnsureTimer()
        {
            if (_disposed)
            {
                return;
            }

            if (timer == null)
            {
                timer = new Timer(Math.Max(500, Interval));
                timer.Elapsed += OnTimerElapsed;
                timer.AutoReset = true;
            }

            timer.Interval = Math.Max(500, Interval);
            if (Autoplay && Items.Count > 1)
            {
                timer.Start();
            }
            else
            {
                timer.Stop();
            }
        }

        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            await InvokeAsync(async () => await ShowNextAsync());
        }

        private void NormalizeActiveIndex()
        {
            if (items.Count == 0)
            {
                activeIndex = 0;
                return;
            }

            if (activeIndex >= items.Count)
            {
                activeIndex = items.Count - 1;
            }
        }

        // ===== 图片预览（内置 lightbox） =====

        /// <summary>开发者编程打开预览：images 为要预览的图片地址集合（URL 或 base64），startIndex 指定起始显示的第几张。</summary>
        public async Task OpenPreviewAsync(IEnumerable<string> images, int startIndex = 0)
        {
            var list = images?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? new List<string>();
            if (list.Count == 0)
            {
                return;
            }

            previewList = list;
            previewIndex = Math.Clamp(startIndex, 0, list.Count - 1);
            ResetPreviewView();
            previewVisible = true;
            Refresh();
            if (OnPreviewChanged.HasDelegate)
            {
                await OnPreviewChanged.InvokeAsync(previewIndex);
            }
        }

        /// <summary>基于当前有效图集（开发者显式 <see cref="PreviewImages"/> 或自动收集）打开第 index 张预览；index 越界则忽略。</summary>
        public Task OpenPreviewAsync(int index) => OpenPreviewAsync(GetEffectiveImages(), index);

        /// <summary>关闭预览（供内置 lightbox 遮罩单击与 ESC 键回调）。</summary>
        [JSInvokable]
        public async Task ClosePreviewAsync()
        {
            if (!previewVisible)
            {
                return;
            }

            previewVisible = false;
            Refresh();
        }

        /// <summary>预览内切换到下一张（受 <see cref="PreviewLoop"/> 控制边界行为；供键盘右方向键回调）。</summary>
        [JSInvokable]
        public async Task NextPreviewAsync()
        {
            if (previewList.Count == 0)
            {
                return;
            }

            previewIndex = PreviewLoop
                ? (previewIndex + 1) % previewList.Count
                : Math.Min(previewIndex + 1, previewList.Count - 1);
            ResetPreviewView();
            Refresh();
            if (OnPreviewChanged.HasDelegate)
            {
                await OnPreviewChanged.InvokeAsync(previewIndex);
            }
        }

        /// <summary>预览内切换到上一张（受 <see cref="PreviewLoop"/> 控制边界行为；供键盘左方向键回调）。</summary>
        [JSInvokable]
        public async Task PrevPreviewAsync()
        {
            if (previewList.Count == 0)
            {
                return;
            }

            previewIndex = PreviewLoop
                ? (previewIndex - 1 + previewList.Count) % previewList.Count
                : Math.Max(previewIndex - 1, 0);
            ResetPreviewView();
            Refresh();
            if (OnPreviewChanged.HasDelegate)
            {
                await OnPreviewChanged.InvokeAsync(previewIndex);
            }
        }

        /* ---- 预览缩放 / 平移（回归修复：补回被重写丢失的滚轮缩放） ---- */

        /// <summary>预览大图变换：先平移再缩放（滚轮缩放 / 拖动平移共用），对齐 ElImage 的 viewer 行为。</summary>
        protected string PreviewImageStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add($"transform:translate3d({previewOffsetX:0.##}px,{previewOffsetY:0.##}px,0) scale({previewScale:0.###})")
            .ToString();

        private void ResetPreviewView()
        {
            previewScale = 1;
            previewOffsetX = 0;
            previewOffsetY = 0;
            previewDragging = false;
        }

        /// <summary>滚轮缩放（向上放大 / 向下缩小），范围 0.2x ~ 7x；范围外仍允许平移归位。</summary>
        protected Task OnPreviewWheelAsync(WheelEventArgs e)
        {
            if (!previewVisible)
            {
                return Task.CompletedTask;
            }

            var next = Math.Round(previewScale + (e.DeltaY < 0 ? PreviewScaleStep : -PreviewScaleStep), 3);
            previewScale = Math.Clamp(next, PreviewMinScale, PreviewMaxScale);
            if (previewScale <= 1)
            {
                previewOffsetX = 0;
                previewOffsetY = 0;
            }

            return Task.CompletedTask;
        }

        /// <summary>双击在 1x / 2x 之间切换。</summary>
        protected Task OnPreviewDoubleClickAsync(MouseEventArgs e)
        {
            if (previewScale > 1)
            {
                previewScale = 1;
                previewOffsetX = 0;
                previewOffsetY = 0;
            }
            else
            {
                previewScale = 2;
            }

            return Task.CompletedTask;
        }

        protected Task OnPreviewPointerDownAsync(PointerEventArgs e)
        {
            if (!previewVisible || previewScale <= 1)
            {
                return Task.CompletedTask;
            }

            previewDragging = true;
            previewDragStartX = e.ClientX;
            previewDragStartY = e.ClientY;
            previewDragOriginX = previewOffsetX;
            previewDragOriginY = previewOffsetY;
            return Task.CompletedTask;
        }

        protected Task OnPreviewPointerMoveAsync(PointerEventArgs e)
        {
            if (!previewDragging)
            {
                return Task.CompletedTask;
            }

            previewOffsetX = previewDragOriginX + (e.ClientX - previewDragStartX);
            previewOffsetY = previewDragOriginY + (e.ClientY - previewDragStartY);
            return Task.CompletedTask;
        }

        protected Task OnPreviewPointerUpAsync(PointerEventArgs e)
        {
            previewDragging = false;
            return Task.CompletedTask;
        }

        /// <summary>轮播项被单击时的内部入口：仅当该项含图片且开启 <see cref="PreviewOnClick"/> 才放大，否则无反应。</summary>
        internal async Task OnItemClickAsync(ElCarouselItem item)
        {
            if (!PreviewOnClick)
            {
                return;
            }

            var ordered = Items.ToList();
            var pos = ordered.IndexOf(item);
            if (pos < 0 || pos >= collectedImages.Count)
            {
                return;
            }

            var src = collectedImages[pos];
            if (string.IsNullOrEmpty(src))
            {
                return; // 该项不是图片，单击不反应
            }

            var list = GetEffectiveImages();
            var start = list.IndexOf(src);
            if (start < 0)
            {
                start = 0;
            }

            await OpenPreviewAsync(list, start);
        }

        private List<string> GetEffectiveImages()
        {
            if (PreviewImages != null && PreviewImages.Any())
            {
                return PreviewImages.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            }

            return collectedImages.Where(s => !string.IsNullOrEmpty(s)).ToList();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                dotNetRef = DotNetObjectReference.Create(this);
            }

            await CollectImagesAsync();

            try
            {
                if (previewVisible)
                {
                    await JSRuntime.InvokeVoidAsync("elementCarouselBindPreviewKeys", dotNetRef, "ClosePreviewAsync");
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("elementCarouselUnbindPreviewKeys");
                }
            }
            catch
            {
                // 无 JS 环境静默降级。
            }
        }

        /// <summary>重新从各轮播项收集 &lt;img&gt; 地址（JS 回调也可触发）。图片变化或加载完成时刷新 <see cref="CollectedImages"/> 并触发 <see cref="PreviewImagesChanged"/>。</summary>
        [JSInvokable]
        public async Task CollectImagesAsync()
        {
            List<string> imgs;
            try
            {
                imgs = await JSRuntime.InvokeAsync<List<string>>("elementCarouselCollectImages", rootRef) ?? new List<string>();
            }
            catch
            {
                // 无 JS 环境（bUnit 等）静默降级：仅保留已有收集结果。
                imgs = collectedImages;
            }

            var changed = imgs.Count != collectedImages.Count;
            if (!changed)
            {
                for (var i = 0; i < imgs.Count; i++)
                {
                    if (imgs[i] != collectedImages[i])
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                collectedImages = imgs;
                if (PreviewImagesChanged.HasDelegate)
                {
                    await PreviewImagesChanged.InvokeAsync(collectedImages);
                }
            }

            try
            {
                await JSRuntime.InvokeVoidAsync("elementCarouselBindImgLoad", rootRef, dotNetRef, "CollectImagesAsync");
            }
            catch
            {
                // 忽略：图片 load 后重新收集仅是体验增强。
            }
        }

        public override void Dispose()
        {
            _disposed = true;
            langSubscription?.Dispose();
            langSubscription = null;

            if (timer != null)
            {
                timer.Elapsed -= OnTimerElapsed;
                timer.Dispose();
                timer = null;
            }

            try
            {
                JSRuntime.InvokeVoidAsync("elementCarouselUnbindPreviewKeys");
            }
            catch
            {
                // 无 JS 环境静默降级
            }

            dotNetRef?.Dispose();

            base.Dispose();
        }
    }
}
