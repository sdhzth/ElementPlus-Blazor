using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElTransition : ElementComponentBase, IAsyncDisposable
    {
        private ElementReference animationElement;
        private bool animationBegined = false;
        private HtmlPropertyBuilder styleBuilder;
        private List<string> currentStyleNames;
        private Queue<TransitionOption> paths = new Queue<TransitionOption>();
        private DotNetObjectReference<ElTransition> thisRef;
        private TransitionOption currentPath;
        private HtmlPropertyBuilder clsBuilder;
        private TaskCompletionSource<int> taskTrigger;
        private bool animating = false;

        /// <summary>
        /// 当动画暂停时触发
        /// </summary>
        [Parameter]
        public EventCallback OnPause { get; set; }

        /// <summary>
        /// 根据动画元素尺寸自动调整位置，一般用于弹窗位置调整
        /// </summary>
        [Parameter]
        public bool AutoAdjustPosition { get; set; }

        /// <summary>
        /// 当所有动画执行完成后触发
        /// </summary>
        [Parameter]
        public EventCallback OnAllEnd { get; set; }

        /// <summary>
        /// 绝对定位
        /// </summary>
        [Parameter]
        public bool IsAbsolute { get; set; }

        [Inject]
        internal Document Document { get; set; }

        /// <summary>
        /// 动画执行路径
        /// </summary>
        [Parameter]
        public RenderFragment Path { get; set; }

        /// <summary>
        /// 动画开始之前的样式
        /// </summary>
        [Parameter]
        public string InitialStyle { get; set; }

        internal void AddPathConfig(TransitionOption option)
        {
            if (!IsAbsolute && (option.Style.StartsWith("top:") || option.Style.StartsWith("left:") || option.Style.Contains(";top:") || option.Style.Contains(";left:")))
            {
                IsAbsolute = true;
            }
            paths.Enqueue(option);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            styleBuilder = HtmlPropertyBuilder.CreateCssStyleBuilder()
                .Add("display:none");
        }

        /// <summary>
        /// 要进行动画播放的内容
        /// </summary>
        [Parameter]
        public RenderFragment Content { get; set; }

        private async Task AnimationElementLoadAsync()
        {
            if (paths == null || !paths.Any())
            {
                return;
            }
            if (AutoAdjustPosition)
            {
                var rect = await animationElement.Dom(JSRuntime).GetBoundingClientRectAsync();
                var screenWidth = await Document.GetClientWidthAsync();
                var left = screenWidth / 2 - rect.Width / 2;
                styleBuilder.Add($"left:{left}px");
            }
            currentPath = paths.Dequeue();
            await DelayPausableAsync(currentPath.Delay);
            ExecuteAnimation(currentPath.Style, currentPath.Increment);
        }

        private void ExecuteAnimation(string style, bool increment)
        {
            animating = true;
            var styles = style.Split(';');
            currentStyleNames = styles.Select(x => x.Split(':')[0]).ToList();
            for (int i = 0; i < currentStyleNames.Count; i++)
            {
                var styleName = currentStyleNames[i];
                if (styleName == "margin")
                {
                    currentStyleNames[i] = "margin-left";
                    currentStyleNames.AddRange(new string[] { "margin-right", "margin-bottom", "margin-top" });
                    continue;
                }
                if (styleName == "padding")
                {
                    currentStyleNames[i] = "padding-left";
                    currentStyleNames.AddRange(new string[] { "padding-right", "padding-bottom", "padding-top" });
                    continue;
                }
            }
            foreach (var styleItem in styles)
            {
                var styleName = styleItem.Split(':')[0];
                var finalStyle = styleItem;
                if (increment && styleItem.EndsWith("px", StringComparison.CurrentCultureIgnoreCase))
                {
                    var incrementValue = Convert.ToDouble(Regex.Match(styleItem, @"\d+\.*").Value);
                    var oldStyle = styleBuilder.FirstOrDefault(x => x.StartsWith($"{styleName}:"));
                    if (oldStyle == null)
                    {
                        styleBuilder.Remove(styleName).Add(finalStyle);
                        continue;
                    }
                    var oldValue = Convert.ToDouble(Regex.Match(oldStyle, @"\d+\.*").Value);
                    finalStyle = $"{styleName}:{(oldValue + incrementValue).ToString()}px";
                    styleBuilder.Remove(styleName).Add(finalStyle);
                    continue;
                }
                styleBuilder.Remove(styleName).Add(finalStyle);
            }
            MarkAsRequireRender();
            StateHasChanged();
        }

        /// <summary>
        /// 使暂停的动画继续执行
        /// </summary>
        public void Resume()
        {
            if (taskTrigger == null)
            {
                return;
            }
            taskTrigger.TrySetResult(0);
        }

        // —— 可暂停延时（对齐 EP：ElMessage 鼠标悬停时暂停自动关闭，移开后继续倒计时）——
        private CancellationTokenSource delayCts;
        private TaskCompletionSource<int> resumeTrigger;

        /// <summary>
        /// 可暂停的延时等待：<see cref="PauseTiming"/> 打断当前等待并记住剩余时间，
        /// <see cref="ResumeTiming"/> 恢复后按剩余时间继续（暂停期间不计入，总时长不变）。
        /// </summary>
        private async Task DelayPausableAsync(int milliseconds)
        {
            if (milliseconds <= 0)
            {
                return;
            }
            var remaining = milliseconds;
            while (remaining > 0)
            {
                var cts = new CancellationTokenSource();
                delayCts = cts;
                var start = DateTime.UtcNow;
                try
                {
                    await Task.Delay(remaining, cts.Token);
                    remaining = 0;
                }
                catch (TaskCanceledException)
                {
                    // 被 PauseTiming 打断：记下剩余时间，挂起等 ResumeTiming
                    remaining -= (int)(DateTime.UtcNow - start).TotalMilliseconds;
                    if (remaining < 0)
                    {
                        remaining = 0;
                    }
                    resumeTrigger = new TaskCompletionSource<int>();
                    await resumeTrigger.Task;
                    resumeTrigger = null;
                }
                finally
                {
                    delayCts = null;
                    cts.Dispose();
                }
            }
        }

        /// <summary>
        /// 暂停当前动画路径的延时计时（不处于延时等待期时为空操作）。
        /// 典型用途：ElMessage 鼠标悬停时暂停「Duration 到期自动关闭」。
        /// </summary>
        public void PauseTiming()
        {
            try { delayCts?.Cancel(); } catch { }
        }

        /// <summary>
        /// 恢复被 <see cref="PauseTiming"/> 暂停的延时计时（未暂停时为空操作）。
        /// </summary>
        public void ResumeTiming()
        {
            resumeTrigger?.TrySetResult(0);
        }

        [JSInvokable("AnimationEnd")]
        public async Task AnimationEndAsync(string propertyName)
        {
            if (currentStyleNames != null)
            {
                if (!currentStyleNames.Remove(propertyName))
                {
                    System.Diagnostics.Debug.WriteLine("样式移除失败:" + propertyName + "，还有" + currentStyleNames.Count + "个样式等待移除");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"{propertyName}移除成功，还有{currentStyleNames.Count  }个样式等待移除");
                    while (currentStyleNames.Contains(propertyName))
                    {
                        currentStyleNames.Remove(propertyName);
                    }
                    if (currentStyleNames.Any())
                    {
                        return;
                    }
                }
            }

            if (!animating)
            {
                return;
            }
            animating = false;
            if (paths == null || !paths.Any())
            {
                if (OnAllEnd.HasDelegate)
                {
                    await OnAllEnd.InvokeAsync(null);
                }
                return;
            }
            currentPath = paths.Dequeue();
            if (currentPath.Pause.HasValue && currentPath.Pause.Value)
            {
                if (OnPause.HasDelegate)
                {
                    await OnPause.InvokeAsync(null);
                }
                taskTrigger = new TaskCompletionSource<int>();
                await taskTrigger.Task;
                taskTrigger = null;
            }
            await DelayPausableAsync(currentPath.Delay);
            styleBuilder.Remove("transition").Add($"transition:all {(currentPath.Duration <= 0 ? 1000 : currentPath.Duration)}ms ease");
            ExecuteAnimation(currentPath.Style, currentPath.Increment);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                thisRef = DotNetObjectReference.Create(this);
                clsBuilder = HtmlPropertyBuilder.CreateCssClassBuilder()
                    .Add("blazor-animation")
                    .Add(Cls?.Split(' ') ?? new string[0]);

                await JSRuntime.InvokeVoidAsync("RegisterAnimationBegin", thisRef, animationElement);
                styleBuilder = HtmlPropertyBuilder.CreateCssStyleBuilder()
                    .Add(InitialStyle?.Split(';') ?? new string[0])
                    .AddIf(IsAbsolute, "position:absolute")
                    .Add("transition:all 1s ease");
                if (paths.Any())
                {
                    var firstStyle = paths.Peek();
                    styleBuilder.Remove("transition").Add($"transition:all {(firstStyle.Duration <= 0 ? 1000 : firstStyle.Duration)}ms ease");
                }
                MarkAsRequireRender();
                StateHasChanged();
                return;
            }
            if (!animationBegined)
            {
                animationBegined = true;
                await AnimationElementLoadAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("UnregisterAnimationBegin", animationElement);
            }
            catch { }
            thisRef?.Dispose();
            thisRef = null;
        }
    }
}
