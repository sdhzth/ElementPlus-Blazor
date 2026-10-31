

using ElementPlus.Lang;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ElementPlus
{
    public class ElementComponentBase : ComponentBase, IDisposable
    {
        internal bool allowRefresh;
        [CascadingParameter(Name = "Page")]
        public ElementComponentBase Page { get; set; }

        [CascadingParameter]
        public ElementConfig ElementConfiguration { get; set; }
        protected bool RequireRender { get; set; } = true;

        [Parameter(CaptureUnmatchedValues = true)]
        public IDictionary<string, object> Attributes { get; set; }

        /// <summary>
        /// 该值设为 true，则该组件会始终允许刷新，不受 <seealso cref="ElementComponentBase.MarkAsRequireRender"/> 包与链接
        /// </summary>
        [Parameter]
        public bool EnableAlwaysRender { get; set; }
        [Inject]
        ElementPlus.MessageBox MessageBox { get; set; }

        [Inject]
        public DialogService DialogService { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }
        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        [Inject]
        MessageService MessageService { get; set; }

        [Inject]
        public LoadingService LoadingService { get; set; }

        /// <summary>
        /// 库级词条（i18n）服务。基类已经注入了 MessageBox / DialogService 等一堆必需服务，
        /// 所以这一项不给宿主增加任何新要求（都在 <c>AddElementServices()</c> 里）。
        /// 组件取词统一写 <c>Lang.Text("el.xxx.yyy", 显式参数, 改动前那个字面量)</c>；
        /// 切换语言后的重渲染由基类统一订阅处理，组件不必各自接事件。
        /// </summary>
        [Inject]
        public ILang Lang { get; set; }

        [Parameter]
        public Func<object, Task> OnRenderCompleted { get; set; }

        /// <summary>
        /// 自定义 CSS 类
        /// </summary>
        [Parameter]
        public virtual string Cls { get; set; }

        [CascadingParameter]
        public ElBadge Badge { get; set; }
        /// <summary>
        /// 设置自定义样式
        /// </summary>
        [Parameter]
        public string Style { get; set; } = string.Empty;

        /// <summary>
        /// 弹出 Alert 消息
        /// </summary>
        /// <param name="text"></param>
        public void Alert(string text)
        {
            _ = MessageBox.AlertAsync(text);
        }

        protected override void OnInitialized()
        {
            if (DialogContainer != null)
            {
                DialogContainer.OnShow += OnDialogShowAsync;
            }

            // 词条是应用级可变状态，而 Blazor 在父组件重渲染时会跳过"参数没变"的子组件：
            // 光靠宿主 StateHasChanged 刷不到深层组件的文字，所以在这里统一订阅一次。
            langSubscription = Lang.SubscribeLocaleChanged(() => InvokeAsync(StateHasChanged));
        }

        private IDisposable langSubscription;

        /// <summary>
        /// 弹出 Confirm 消息
        /// </summary>
        /// <param name="text"></param>
        public async Task<MessageBoxResult> ConfirmAsync(string text)
        {
            return await MessageBox.ConfirmAsync(text);
        }

        /// <summary>
        /// 默认情况下所有复杂组件都只进行一次渲染，该方法将组件置为需要再次渲染
        /// </summary>
        public virtual void MarkAsRequireRender()
        {
            RequireRender = true;
        }

        [CascadingParameter]
        public ElementDialogBase DialogContainer { get; set; }

        protected ElementSize ConfigSize => ElementConfiguration?.Size ?? ElementSize.Default;

        protected string ConfigNamespace => string.IsNullOrWhiteSpace(ElementConfiguration?.Namespace)
            ? ElementConfig.DefaultNamespace
            : ElementConfiguration.Namespace;

        protected int ConfigZIndex => ElementConfiguration?.ZIndex ?? ElementConfig.DefaultZIndex;

        protected string ConfigLocale => ElementConfiguration?.Locale;

        protected ButtonSize ResolveButtonSize(ButtonSize size)
        {
            if (size != ButtonSize.Default)
            {
                return size;
            }

            return ConfigSize switch
            {
                ElementSize.Large => ButtonSize.Large,
                ElementSize.Small => ButtonSize.Small,
                _ => ButtonSize.Default
            };
        }

        protected InputSize ResolveInputSize(InputSize size)
        {
            if (size != InputSize.Normal)
            {
                return size;
            }

            return ConfigSize switch
            {
                ElementSize.Large => InputSize.Large,
                ElementSize.Small => InputSize.Small,
                _ => InputSize.Normal
            };
        }

        protected RadioSize ResolveRadioSize(RadioSize size)
        {
            if (size != RadioSize.Default)
            {
                return size;
            }

            return ConfigSize switch
            {
                ElementSize.Large => RadioSize.Medium,
                ElementSize.Small => RadioSize.Small,
                _ => RadioSize.Default
            };
        }

        protected virtual Task OnDialogShowAsync()
        {
            return Task.CompletedTask;
        }
        public void Toast(string text)
        {
            MessageService.Show(text);
        }
        public void Toast(string text, MessageType type)
        {
            MessageService.Show(text, type);
        }
        public async Task<MessageBoxResult> AlertAsync(string text)
        {
            return await MessageBox.AlertAsync(text);
        }

        protected override void OnAfterRender(bool firstRender)
        {
            RequireRender = false;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                return;
            }

            if (OnRenderCompleted != null)
            {
                await OnRenderCompleted(this);
            }
            RequireRender = false;
        }

        public virtual void Refresh()
        {
            _ = InvokeAsync(() =>
              {
                  MarkAsRequireRender();
                  StateHasChanged();
              });
        }

        protected override bool ShouldRender()
        {
            return true;
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && DialogContainer != null)
            {
                DialogContainer.OnShow -= OnDialogShowAsync;
            }

            if (disposing)
            {
                // 漏退订＝切一次语言泄漏一批组件；句柄自身对重复 Dispose 是 no-op
                langSubscription?.Dispose();
                langSubscription = null;
            }
        }
    }
}
