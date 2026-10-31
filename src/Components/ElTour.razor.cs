using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElementPlus.Lang;

namespace ElementPlus
{
    public partial class ElTour : ElementComponentBase
    {
        [Inject]
        private ILang Lang { get; set; }
        [Parameter]
        public bool ModelValue { get; set; }

        [Parameter]
        public bool Value
        {
            get => ModelValue;
            set => ModelValue = value;
        }

        [Parameter]
        public EventCallback<bool> ModelValueChanged { get; set; }

        [Parameter]
        public EventCallback<bool> ValueChanged
        {
            get => ModelValueChanged;
            set => ModelValueChanged = value;
        }

        [Parameter]
        public IList<TourStep> Steps { get; set; }

        [Parameter]
        public int Current { get; set; }

        [Parameter]
        public EventCallback<int> CurrentChanged { get; set; }

        [Parameter]
        public bool Mask { get; set; } = true;

        [Parameter]
        public RenderFragment<TourStep> Header { get; set; }

        /// <summary>是否显示箭头（默认 true）</summary>
        [Parameter]
        public bool ShowArrow { get; set; } = true;

        /// <summary>主题：light / dark</summary>
        [Parameter]
        public string Effect { get; set; } = "light";

        /// <summary>是否显示关闭按钮（默认 true）</summary>
        [Parameter]
        public bool ShowClose { get; set; } = true;

        /// <summary>ESC 是否关闭（默认 true）</summary>
        [Parameter]
        public bool CloseOnPressEscape { get; set; } = true;

        /// <summary>点击遮罩是否关闭（默认 <c>false</c>）。
        /// 官方口径：EP 2.14.0 的 tour <b>没有</b> close-on-click-mask 这个 prop，
        /// <c>mask.vue</c> 里遮罩 <c>&lt;path&gt;</c> 既没有 click 监听、样式还写死
        /// <c>pointerEvents:'auto' / cursor:'auto'</c> —— 点暗处什么都不发生。
        /// 本参数是本库扩展，留给确实想要「点空白就退出」的宿主显式打开。</summary>
        [Parameter]
        public bool CloseOnClickMask { get; set; }

        [Parameter]
        public string MaskExtraStyle { get; set; }

        [Parameter]
        public int ZIndex { get; set; } = 2001;

        /// <summary>下一步按钮文本。优先级：<b>本参数 &gt; 词条 <c>el.tour.next</c> &gt; 默认 <c>Next</c></b>。</summary>
        [Parameter]
        public string NextText { get; set; }

        /// <summary>上一步按钮文本，规则同 <see cref="NextText"/>（词条键 <c>el.tour.previous</c>）。</summary>
        [Parameter]
        public string PrevText { get; set; }

        /// <summary>最后一步「完成」按钮文本，规则同 <see cref="NextText"/>（词条键 <c>el.tour.finish</c>）。</summary>
        [Parameter]
        public string FinishText { get; set; }

        /// <summary>面板右上角关闭按钮的可访问名称，规则同 <see cref="NextText"/>（词条键 <c>el.tour.close</c>）。
        /// 兜底值沿用接入 i18n 之前的硬编码 <c>关闭</c>：没词条时 DOM 一字不变。</summary>
        [Parameter]
        public string CloseAriaLabel { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        /// <summary>按钮主题：default（默认）/ primary（选中项主题色，对应官方 type）</summary>
        [Parameter]
        public string Type { get; set; } = "default";

        /// <summary>面板内容区自定义 style（官方 content-style）</summary>
        [Parameter]
        public string ContentStyle { get; set; }

        /// <summary>遮罩镂空与目标之间的空隙（官方 gap：offset / radius，默认 offset=6、radius=2）。
        /// 官方 <c>types.ts</c> 的 <c>TourGap</c> 只有这两个字段：offset 把镂空矩形从目标矩形
        /// <b>四周各外扩</b>这么多像素，radius 是镂空的圆角半径。</summary>
        [Parameter]
        public TourGap Gap { get; set; }

        /// <summary>自定义关闭图标（官方 close-icon，RenderFragment 形式）</summary>
        [Parameter]
        public RenderFragment CloseIcon { get; set; }

        /// <summary>高亮目标区域是否可点击（官方 target-area-clickable，默认 true）</summary>
        [Parameter]
        public bool TargetAreaClickable { get; set; } = true;

        /// <summary>切换步骤时是否将目标元素滚动进可视区（官方 scroll-into-view-options）</summary>
        [Parameter]
        public bool ScrollIntoView { get; set; } = true;

        /// <summary>自定义指示器插槽（官方 indicators）</summary>
        [Parameter]
        public RenderFragment<TourStep> Indicators { get; set; }

        /// <summary>挂载目标容器选择器（官方 append-to，默认 body）</summary>
        [Parameter]
        public string AppendTo { get; set; } = "body";

        /// <summary>弹层默认位置（官方 placement，2.4.2+，默认 bottom）。
        /// 作为所有 step 的兜底位置；单个 <see cref="TourStep.Placement"/> 设置时覆盖本值。</summary>
        [Parameter]
        public string Placement { get; set; } = "bottom";

        // 优先级：宿主显式参数 > 已载入的库级词条 > 接入 i18n 之前就有的英文字面量（词条缺失/DOM 无变化）。
        // 必须在每次渲染现取，不能在 OnInitialized 里 ??= 缓存进参数：缓存会把文字冻在首次取到的语言上，
        // 切语言后整页都换了、这三个按钮却不换。
        private const string NextTextKey = "el.tour.next";
        private const string PrevTextKey = "el.tour.previous";
        private const string FinishTextKey = "el.tour.finish";
        private const string CloseAriaLabelKey = "el.tour.close";

        protected string EffectiveNextText => Lang.Text(NextTextKey, NextText, "Next");

        protected string EffectivePrevText => Lang.Text(PrevTextKey, PrevText, "Previous");

        protected string EffectiveFinishText => Lang.Text(FinishTextKey, FinishText, "Finish");

        protected string EffectiveCloseAriaLabel => Lang.Text(CloseAriaLabelKey, CloseAriaLabel, "关闭");

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        private string PanelId => $"el-tour-panel-{GetHashCode()}";

        // ========== Events ==========

        [Parameter]
        public EventCallback<int> OnChange { get; set; }

        [Parameter]
        public EventCallback OnClose { get; set; }

        [Parameter]
        public EventCallback OnFinish { get; set; }

        [Parameter]
        public EventCallback<int> OnNext { get; set; }

        [Parameter]
        public EventCallback<int> OnPrev { get; set; }

        [Parameter]
        public EventCallback OnEscPress { get; set; }

        [Parameter]
        public EventCallback OnMaskClick { get; set; }

        // ========== 计算属性 ==========

        protected IReadOnlyList<TourStep> StepList => Steps?.Where(x => x != null).ToList() ?? new List<TourStep>();

        /// <summary>当前步的下标。<see cref="ActiveStep"/> 与指示器圆点必须用同一个钳位值，
        /// 否则 <c>Current</c> 越界时面板内容与「第几个圆点亮着」会各说各话。</summary>
        protected int ActiveStepIndex => StepList.Count == 0 ? 0 : Math.Max(0, Math.Min(Current, StepList.Count - 1));

        protected TourStep ActiveStep => StepList.Count == 0 ? null : StepList[ActiveStepIndex];

        protected string TourClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-tour", Cls)
            .AddIf(!string.IsNullOrWhiteSpace(EffectiveEffect), $"is-{EffectiveEffect}")
            .AddIf(EffectiveType == "primary", "el-tour--primary")
            .ToString();

        /// <summary>面板类名。<c>is-{placement}</c> 只带配色/朝向语义，几何（left/top）全部由
        /// dom.js 的逐帧跟进写内联：CSS 一旦同时给 top 和 bottom，会把 fixed 面板的高度夹住，
        /// 而 flip / 定位又要读这个高度，形成高度↔坐标反馈（见 index.css 的 <c>.el-tour__content</c> 注释）。</summary>
        protected string PanelClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-tour__content")
            .AddIf(!string.IsNullOrWhiteSpace(EffectivePlacement), $"is-{EffectivePlacement}")
            .ToString();

        /// <summary>官方 <c>step.vue</c> 的 <c>header</c> 在显示关闭按钮时加 <c>show-close</c>，
        /// 用 <c>padding-right</c> 让标题避开右上角 40×40 的关闭热区。</summary>
        protected string HeaderClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-tour__header")
            .AddIf(EffectiveShowClose, "show-close")
            .ToString();

        /// <summary>
        /// 面板内联样式。这里必须显式给出高于遮罩的 z-index：
        /// 遮罩 <c>.el-tour__mask</c> 使用 <c>inset:0</c> 铺满整个 <c>.el-tour</c> 且 z-index 为 ZIndex-1，
        /// 若面板自身没有 z-index（默认 auto），遮罩会绘制在面板之上，
        /// 造成「上一步 / 下一步 / 关闭」等按钮全部被遮罩挡住、无法点击。
        /// <b>不要再往里写 padding</b>：官方 <c>tour.scss</c> 的面板内边距取自
        /// <c>--el-tour-padding-primary</c>（12px），而 <c>offset</c> 是「面板与镂空矩形之间的距离」（默认 10px），
        /// 把它当 padding 写进来会覆盖 CSS 的 12px，并且让内边距随宿主设的 <c>TourStep.Offset</c> 一起变。
        /// </summary>
        protected string PanelStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add($"z-index:{EffectiveZIndex}")
            .AddIf(!string.IsNullOrWhiteSpace(EffectiveContentStyle), EffectiveContentStyle)
            .ToString();

        protected string TourStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(EffectiveZIndex > 0, $"z-index:{EffectiveZIndex}")
            .Add(Style)
            .ToString();

        /// <summary>镂空能不能画出来：取决于当前步有没有给出目标选择器（官方 <c>useTarget</c> 找不到元素时
        /// <c>posInfo=null</c>，遮罩就是一整块、没有洞）。</summary>
        protected bool HasHollowTarget => !string.IsNullOrWhiteSpace(ActiveStep?.Target);

        /// <summary>
        /// 遮罩层样式，逐条对齐官方 <c>mask.vue</c> 的 <c>maskStyle</c>：铺满视口 +
        /// <c>pointer-events</c> 由「有没有镂空」和 <c>target-area-clickable</c> 共同决定。
        /// 两者都成立时整层放行点击，真正吃点击的是里面的 <c>&lt;path&gt;</c>（它自带
        /// <c>pointer-events:auto</c>，且镂空内没有填充、不吃点击）——镂空里因此能直接点到目标元素；
        /// 反之遮罩自己吃点击，目标点不到。
        /// </summary>
        protected string MaskStyle
        {
            get
            {
                var b = HtmlPropertyBuilder.CreateCssStyleBuilder();
                b.Add("z-index:" + (EffectiveZIndex - 1));
                if (!HasHollowTarget || !TargetAreaClickable)
                {
                    b.Add("pointer-events:auto");
                }
                if (!string.IsNullOrWhiteSpace(MaskExtraStyle))
                {
                    b.Add(MaskExtraStyle);
                }
                var step = ActiveStep;
                if (step != null && !string.IsNullOrWhiteSpace(step.MaskExtraStyle))
                {
                    b.Add(step.MaskExtraStyle);
                }
                return b.ToString() ?? string.Empty;
            }
        }

        protected bool EffectiveShowArrow => ActiveStep?.ShowArrow ?? ShowArrow;
        protected bool EffectiveShowClose => ActiveStep?.ShowClose ?? ShowClose;
        protected bool EffectiveMask => ActiveStep?.Mask ?? Mask;
        protected bool EffectiveCloseOnClickMask => ActiveStep?.CloseOnClickMask ?? CloseOnClickMask;
        protected int EffectiveOffset => ActiveStep?.Offset ?? 10;
        protected string EffectiveType => ActiveStep?.Type ?? Type;
        protected string EffectiveContentStyle => ActiveStep?.ContentStyle ?? ContentStyle;
        protected string EffectiveEffect => ActiveStep?.Effect ?? Effect;

        /// <summary>步骤级 z-index 优先（官方 step 没有这个字段，属本库扩展；改动前它被声明后从未被读）。</summary>
        protected int EffectiveZIndex => ActiveStep?.ZIndex ?? ZIndex;

        /// <summary>镂空相对目标矩形的外扩像素（官方 <c>gap.offset</c>，默认 6）。</summary>
        protected int EffectiveGapOffset => Gap?.Offset ?? 6;

        /// <summary>镂空圆角半径（官方 <c>gap.radius</c>，默认 2）。</summary>
        protected int EffectiveGapRadius => Gap?.Radius ?? 2;

        /// <summary>弹层实际位置：单个 step 的 <see cref="TourStep.Placement"/> 优先，否则用组件级 <see cref="Placement"/> 兜底（默认 bottom）。</summary>
        protected string EffectivePlacement => ActiveStep?.Placement ?? Placement;

        /// <summary><c>data-side</c>：placement 的第一段（top/bottom/left/right），官方 <c>content.vue</c>
        /// 用它决定箭头贴面板哪条边（<c>[data-side^='bottom'] .el-tour__arrow{top:-5px}</c>）。
        /// JS 在 flip 之后会把实际生效的那一面写回来。</summary>
        protected string EffectiveSide => FirstSideSegment(EffectivePlacement);

        private static string FirstSideSegment(string placement)
        {
            if (string.IsNullOrWhiteSpace(placement))
            {
                return null;
            }
            var dash = placement.IndexOf('-');
            return dash > 0 ? placement.Substring(0, dash) : placement;
        }

        // ========== 方法 ==========

        protected async Task PreviousAsync()
        {
            if (Current == 0) return;
            await SetCurrentAsync(Current - 1);
            if (OnPrev.HasDelegate) await OnPrev.InvokeAsync(Current);
        }

        protected async Task NextAsync()
        {
            if (Current >= StepList.Count - 1) return;
            await SetCurrentAsync(Current + 1);
            if (OnNext.HasDelegate) await OnNext.InvokeAsync(Current);
        }

        protected async Task FinishAsync()
        {
            ModelValue = false;
            if (ModelValueChanged.HasDelegate)
                await ModelValueChanged.InvokeAsync(false);
            await ResetCurrentAsync();
            if (OnFinish.HasDelegate)
                await OnFinish.InvokeAsync(null);
            if (OnClose.HasDelegate)
                await OnClose.InvokeAsync(null);
        }

        /// <summary>
        /// 官方 <c>tour.vue</c> 的 <c>watch(() =&gt; props.modelValue)</c>：关掉引导时把 <c>current</c> 归零。
        /// 少了这一步，宿主复用同一个 ElTour 重新点「开始引导」时会停在上次那一步，
        /// 表现为「第二遍引导没有第一步」——用户看到的就是「引导没反应 / 效果不对」。
        /// </summary>
        private async Task ResetCurrentAsync()
        {
            if (Current == 0)
            {
                return;
            }
            Current = 0;
            if (CurrentChanged.HasDelegate)
            {
                await CurrentChanged.InvokeAsync(Current);
            }
        }

        protected async Task SetCurrentAsync(int value)
        {
            if (StepList.Count == 0) return;
            var newVal = Math.Max(0, Math.Min(value, StepList.Count - 1));
            if (newVal == Current) return;
            Current = newVal;
            if (CurrentChanged.HasDelegate)
                await CurrentChanged.InvokeAsync(Current);
            if (OnChange.HasDelegate)
                await OnChange.InvokeAsync(Current);
            await ScrollActiveTargetIntoView();
        }

        /// <summary>
        /// 切换步骤时把目标元素滚动进可视区（官方 scroll-into-view-options）。
        /// 仅在 <see cref="ScrollIntoView"/> 开启且当前步 <see cref="TourStep.Target"/> 为有效 CSS 选择器时生效；
        /// JS 调用失败（如无头测试环境）静默忽略。
        /// </summary>
        protected async Task ScrollActiveTargetIntoView()
        {
            if (!ScrollIntoView || JSRuntime == null) return;
            var target = ActiveStep?.Target;
            if (string.IsNullOrWhiteSpace(target)) return;
            try
            {
                await JSRuntime.InvokeAsync<object>("elementScrollIntoView", target, "{\"block\":\"center\"}");
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is TaskCanceledException)
            {
                // bUnit / 预渲染 / JS 运行时尚未就绪：这里没有几何可滚，镂空与面板由 dom.js 的逐帧跟进兜住。
                // 只吞这两类：其它异常（JS 端真抛了）必须冒出来，否则定位坏掉会被静默成「引导没效果」。
            }
        }

        protected async Task CloseAsync()
        {
            ModelValue = false;
            if (ModelValueChanged.HasDelegate)
                await ModelValueChanged.InvokeAsync(false);
            await ResetCurrentAsync();
            if (OnClose.HasDelegate)
                await OnClose.InvokeAsync(null);
        }

        protected async Task OnMaskClickAsync()
        {
            if (OnMaskClick.HasDelegate)
                await OnMaskClick.InvokeAsync(null);
            if (EffectiveCloseOnClickMask)
                await CloseAsync();
        }

        /// <summary>
        /// 键盘口径对齐官方，而且<b>只认三个键</b>。官方把键盘拆在两处：
        /// ESC 走 <c>content.vue</c> 的 focus-trap <c>@release-requested</c> → <c>tour.vue onEscClose</c>
        /// （再被 <c>close-on-press-escape</c> 拦住），左 / 右方向键走 <c>step.vue</c> 的 window keydown
        /// （目标 contenteditable 时整段跳过）。
        /// 改动前这里<b>不判键名</b>：面板内任何一次 keydown —— 连按 Tab 换焦点、在按钮上按空格 ——
        /// 都会把整条引导关掉，这就是「刚一点下一步引导就没了」。
        /// 面板本身带 <c>tabindex="-1"</c> 且出现时会被聚焦，非面板内的按键由 dom.js 的 document
        /// 监听转发进来（右方向键在最后一步 = 完成，与官方 <c>onNext</c> 一致）。
        /// </summary>
        protected async Task OnKeyDownAsync(KeyboardEventArgs e)
        {
            switch (e?.Key)
            {
                case "Escape":
                    if (!CloseOnPressEscape) return;
                    if (OnEscPress.HasDelegate)
                        await OnEscPress.InvokeAsync(null);
                    await CloseAsync();
                    break;
                case "ArrowLeft":
                    await PreviousAsync();
                    break;
                case "ArrowRight":
                    if (Current >= StepList.Count - 1)
                        await FinishAsync();
                    else
                        await NextAsync();
                    break;
            }
        }
    }
}