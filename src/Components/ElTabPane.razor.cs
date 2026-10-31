

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElTabPane : ElementComponentBase, IDisposable
    {
        [Parameter]
        public EventCallback<ElementChangeEventArgs<ElTabPane>> OnTabPanelChanging { get; set; }
        private static int tabIndex = 0;
        /// <summary>表头宽度量不到时的重试上限；容器隐藏时 clientWidth 恒为 0，不设上限就是渲染自循环。</summary>
        private const int MaxMeasureRetries = 3;
        private int measureRetries = 0;

        public override string ToString()
        {
            return Name;
        }

        protected async Task OnInternalTabCloseAsync(MouseEventArgs e)
        {
            if (Disabled)
            {
                return;
            }
            RequireRender = true;
            await TabContainer.CloseTabAsync(this);
        }

        [Parameter]
        public bool IsActive { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool IsDisabled
        {
            get => Disabled;
            set => Disabled = value;
        }
        public ElementReference Element { get; set; }

        [CascadingParameter]
        public ElTabs TabContainer { get; set; }

        protected async Task Activate(MouseEventArgs e)
        {
            if (Disabled)
            {
                return;
            }
            RequireRender = true;
            // 官方 tab-click：只要点中页签就触发（即使切换被 before-leave 拦截、激活项没变）
            await TabContainer.NotifyTabClickAsync(this, e);
            IsActive = await TabContainer.SetActivateTabAsync(this);
        }

        /// <summary>是否已经渲染过内容（配合 lazy 使用）</summary>
        internal bool ContentRendered { get; set; }

        public void Activate()
        {
            TabContainer.headerSizeUpdated = false;
            IsActive = true;
        }

        /// <summary>
        /// 本仓 Grow 口径（Element Plus 没有这个属性，官方用父级 v-if）：<c>false</c> 时这个页签<b>此刻不存在</b>——
        /// 页签头不渲染、不进 <see cref="ElTabs"/> 的页签集合、不参与表头测量。收起的正是当前激活页签时，
        /// 容器会顺延到下一个可用页签（内容区渲染的是 <c>ActiveTab.ChildContent</c>，只藏头不换位会让内容区
        /// 停在一个已摘掉的页签上）。
        /// 与 <see cref="Disabled"/> 是两件事（禁用还在条上、只是点不动），与「用户点 X 关掉」也是两件事
        /// （关掉的记进名字表不再回来，隐藏的随条件翻回来）。默认 <c>true</c>，加参数前的行为逐字节不变。
        /// </summary>
        [Parameter]
        public bool Visible { get; set; } = true;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            RequireRender = TabContainer?.ActiveTab != null;
            SyncRegistration();
        }

        /// <summary>
        /// 把「页签集合里有我」和「Visible」对齐：<see cref="OnInitializedAsync"/> 只按初始值决定注册，
        /// 运行中翻转（宿主的状态量变了、弹窗复用同一棵渲染树）就靠这里补。两种情况都是空操作：
        /// 可见且已注册 ⇒ <see cref="ElTabs.ShowTab"/> 直接返回；隐藏且未注册 ⇒ <see cref="ElTabs.HideTab"/> 找不到索引。
        /// </summary>
        private void SyncRegistration()
        {
            if (TabContainer == null || string.IsNullOrWhiteSpace(Name))
            {
                return;
            }
            if (Visible)
            {
                TabContainer.ShowTab(this);
            }
            else
            {
                TabContainer.HideTab(this);
            }
        }

        [Parameter]
        public string Name { get; set; }

        [Parameter]
        public string Title { get; set; }

        /// <summary>官方 label 属性别名（与 Title 等价，方便照抄官方示例）</summary>
        [Parameter]
        public string Label
        {
            get => Title;
            set => Title = value;
        }

        /// <summary>官方 label 插槽：自定义页签标题内容，优先级高于 Title</summary>
        [Parameter]
        public RenderFragment TitleTemplate { get; set; }

        /// <summary>官方 lazy：该页签首次激活前不渲染内容（未设置时回落到 ElTabs.Lazy）</summary>
        [Parameter]
        public bool? Lazy { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public bool IsClosable { get; set; }

        [Parameter]
        public bool Closable
        {
            get => IsClosable;
            set => IsClosable = value;
        }

        protected bool EffectiveClosable => IsClosable || TabContainer?.IsRemovable == true || TabContainer?.IsEditable == true;

        protected override async Task OnParametersSetAsync()
        {
            if (!string.IsNullOrWhiteSpace(Name))
            {
                return;
            }
            Name = "tab_" + (++tabIndex);
            await Task.CompletedTask;
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = "tab_" + (++tabIndex);
            }
            if (!Visible)
            {
                // 初始就收起的页签不进集合：集合里没有它，OnAfterRenderAsync 的「挑第一个/按 ModelValue 定位」
                // 都不会选中它，内容区也不会渲染它的 ChildContent。
                return;
            }
            if (!TabContainer.Exists(Name))
            {
                TabContainer.AddTab(this);
            }
        }

        private async Task AcitveTabOnRenderCompletedAsync()
        {
            if (!Visible)
            {
                // 收起的页签既没有 DOM 可量，也不该对外报「渲染完成」（它这一帧根本没渲染）。
                return;
            }
            if (IsActive && TabContainer.Type == TabType.Normal)
            {
                if (string.IsNullOrWhiteSpace(Element.Id))
                {
                    return;
                }
                var dom = Element.Dom(JSRuntime);
                var width = await dom.GetClientWidthAsync();
                var paddingLeft = await dom.Style.GetPaddingLeftAsync();
                var offsetLeft = await dom.GetOffsetLeftAsync();
                var padding = paddingLeft + (await dom.Style.GetPaddingRightAsync());
                var barWidth = width - padding;
                var barOffsetLeft = offsetLeft + paddingLeft;
                if (barWidth < 0)
                {
                    // 重试必须有上限：页签被放在 display:none 的容器里时 clientWidth 恒为 0，
                    // barWidth 永远是负数，无条件 Refresh 就成了「渲染→测量→渲染」自循环
                    // （DocManage 插桩档实测 12492 次/25 秒；复现档 10754 次/28 秒，25 秒窗口 evaluate
                    //  全超时、后端 127.0.0.1:5282 请求 x0，而同探针在净包健康档看得到 x10
                    //  ⇒ 主线程在页面发出首次后端查询之前就被占死。宿主 _tools/repro1_tally_dm*.out）。
                    // EP 侧只在 nextTick 量一次、量不到不重渲染，这里用上限对齐同一行为。
                    if (measureRetries < MaxMeasureRetries)
                    {
                        measureRetries++;
                        Refresh();
                    }
                    return;
                }
                measureRetries = 0;
                await TabContainer.UpdateHeaderSizeAsync(this, barWidth, barOffsetLeft);
            }
            else
            {
                await TabContainer.TabRenderCompletedAsync(this);
            }
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await AcitveTabOnRenderCompletedAsync();
            if (!firstRender)
            {
                return;
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        public override void Dispose()
        {
            base.Dispose();
            TabContainer?.RemoveTab(this.Name);
        }

        internal void DeActivate()
        {
            IsActive = false;
        }

        protected override bool ShouldRender()
        {
            return true;
        }
    }
}
