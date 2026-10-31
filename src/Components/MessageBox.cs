using ElementPlus.Lang;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElementPlus
{
    public class MessageBox
    {
        public MessageBox(DialogService dialogService, ILang lang)
        {
            DialogService = dialogService;
            Lang = lang;
        }

        private DialogService DialogService { get; }

        /// <summary>
        /// 库级词条服务。按钮文字要<b>在渲染那一刻</b>才取值（见 CreateButtonRenderer 里的闭包），
        /// 这样弹层开着的时候切语言，文字会跟着换；在服务调用时就把字符串定死则不会。
        /// </summary>
        private ILang Lang { get; }

        public Task<MessageBoxResult> AlertAsync(string text)
        {
            return AlertAsync(new MessageBoxOption { Message = text });
        }

        public async Task<MessageBoxResult> AlertAsync(MessageBoxOption messageBoxOption)
        {
            messageBoxOption.ShowCancelButton = false;
            messageBoxOption.ShowConfirmButton = true;
            var dialogResult = await ShowAsync(messageBoxOption);
            return dialogResult.Result;
        }

        public Task<MessageBoxResult> ConfirmAsync(string text)
        {
            return ConfirmAsync(new MessageBoxOption { Message = text });
        }

        public async Task<MessageBoxResult> ConfirmAsync(MessageBoxOption messageBoxOption)
        {
            messageBoxOption.ShowCancelButton = true;
            messageBoxOption.ShowConfirmButton = true;
            var dialogResult = await ShowAsync(messageBoxOption);
            return dialogResult.Result;
        }

        public Task<MessageBoxPromptResult> PromptAsync(string message, string title = null)
        {
            return PromptAsync(new MessageBoxOption
            {
                Message = message,
                // 不再在这里定死「提示」：留 null 交给弹层渲染处走词条 el.messagebox.title（EP 的
                // messagebox 标题也是渲染期 {{ confirmButtonText || t(...) }} 那种形状）
                Title = title,
                ShowInput = true,
                ShowCancelButton = true
            });
        }

        public async Task<MessageBoxPromptResult> PromptAsync(MessageBoxOption messageBoxOption)
        {
            messageBoxOption.ShowInput = true;
            messageBoxOption.ShowCancelButton = true;
            messageBoxOption.ShowConfirmButton = true;
            return await ShowAsync(messageBoxOption);
        }

        private async Task<MessageBoxPromptResult> ShowAsync(MessageBoxOption messageBoxOption)
        {
            var option = CreateOption(messageBoxOption);
            if (messageBoxOption.ShowCancelButton)
            {
                option.Buttons.Add(CreateButtonRenderer(option, messageBoxOption, messageBoxOption.CancelButtonText, "el.messagebox.cancel", "取消", MessageBoxResult.Cancel, messageBoxOption.CancelButtonType, MessageBoxAction.Cancel));
            }
            if (messageBoxOption.ShowConfirmButton)
            {
                option.Buttons.Add(CreateButtonRenderer(option, messageBoxOption, messageBoxOption.ConfirmButtonText, "el.messagebox.confirm", "确定", MessageBoxResult.Ok, messageBoxOption.ConfirmButtonType, MessageBoxAction.Confirm));
            }

            DialogService.Dialogs.Add(option);
            // 这两个 await 后面只是拼返回值、不碰组件状态，所以显式脱离 UI 同步上下文：
            // 否则"结果 Task 的完成"要等渲染器被泵一次才发生 —— 高压/并行下这就是偶发红与
            // 宿主 await 迟迟不返回的共同来源。
            var dialogResult = await option.TaskCompletionSource.Task.ConfigureAwait(false);
            await Task.Delay(10).ConfigureAwait(false);
            return dialogResult.Result as MessageBoxPromptResult ?? new MessageBoxPromptResult
            {
                Result = dialogResult.Result is MessageBoxResult result ? result : MessageBoxResult.Close,
                Value = messageBoxOption.InputValue
            };
        }

        private DialogOption CreateOption(MessageBoxOption messageBoxOption)
        {
            var taskCompletionSource = new TaskCompletionSource<DialogResult>();
            return new DialogOption
            {
                Title = messageBoxOption.Title,
                Content = typeof(MessageBoxContent),
                Parameters = new Dictionary<string, object>
                {
                    [nameof(MessageBoxContent.Option)] = messageBoxOption
                },
                IsDialog = false,
                IsModal = true,
                ShowClose = messageBoxOption.ShowClose,
                CloseOnClickModal = messageBoxOption.CloseOnClickModal,
                CloseOnPressEscape = messageBoxOption.CloseOnPressEscape,
                MessageBoxOption = messageBoxOption,
                TaskCompletionSource = taskCompletionSource
            };
        }

        private RenderFragment CreateButtonRenderer(DialogOption option, MessageBoxOption messageBoxOption, string text, string labelKey, string labelFallback, MessageBoxResult result, ButtonType type, MessageBoxAction action)
        {
            return builder =>
            {
                builder.OpenComponent<ElButton>(0);
                builder.AddAttribute(1, nameof(ElButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(option.Instance, async e =>
                {
                    await CloseAsync(option, messageBoxOption, result, action);
                }));
                builder.AddAttribute(2, nameof(ElButton.Type), type);
                // 取词必须在<b>外层片段这次渲染</b>里求值，并把结果当字符串走 Content 参数：
                // 若像以前那样包一层 new RenderFragment(...) 当 ChildContent，这个委托实例会被
                // option.Buttons 缓存、每次渲染都是同一个引用 → ElButton 认为参数没变而不重渲染，
                // 于是"弹层开着时切语言，按钮文字永远停在旧语言"（并行测试里表现为偶发不更新）。
                builder.AddAttribute(3, nameof(ElButton.Content), Lang.Text(labelKey, text, labelFallback));
                builder.AddAttribute(4, nameof(ElButton.Size), ButtonSize.Small);
                builder.CloseComponent();
            };
        }

        internal static async Task CloseAsync(DialogOption option, MessageBoxOption messageBoxOption, MessageBoxResult result, MessageBoxAction action)
        {
            // EP 口径（message-box/src/index.vue#handleAction）：
            //   if (state.$type === 'prompt' && action === 'confirm' && !validate()) return
            // 输入校验只挂在「确定」上；取消 / × / ESC / 点遮罩都属于「不提交这次输入」，
            // 不该被校验拦住。原来这里不分 action，Prompt 点「取消」也走 InputValidator，
            // 空值时 return 掉就是不关 —— 表现为「点取消也必须先输入病历号」才能关掉。
            if (action == MessageBoxAction.Confirm && messageBoxOption.ShowInput && messageBoxOption.InputValidator != null)
            {
                var err = messageBoxOption.InputValidator(messageBoxOption.InputValue ?? "");
                messageBoxOption.InputErrorMessage = err;
                if (!string.IsNullOrEmpty(err)) return;  // 校验失败，不关闭
            }

            if (messageBoxOption.BeforeClose != null && !await messageBoxOption.BeforeClose(action))
            {
                return;
            }

            await option.Instance.CloseDialogAsync(option, new DialogResult
            {
                Result = new MessageBoxPromptResult
                {
                    Result = result,
                    Value = messageBoxOption.InputValue
                }
            });
        }
    }
}