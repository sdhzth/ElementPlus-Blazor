using ElementPlus.Lang;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 服务化对话框服务。能力范围参考 Radzen Blazor 的 <c>DialogService</c>
    /// （<c>OpenAsync</c> 打开任意组件并回传结果、<c>Close</c>、<c>Confirm</c> / <c>Alert</c> 快捷方法），
    /// 但实现完全自研：<see cref="ElDialogGrowHost"/> 用 <see cref="ElDialogGrow"/> 自渲染对话框，
    /// 不依赖 Radzen 的任何类型、包或运行时。
    ///
    /// 需在应用根布局挂载一次 <c>&lt;ElDialogGrowHost /&gt;</c>（与 <c>ElPopup</c> 同级）。
    /// </summary>
    public class ElDialogGrowService
    {
        public ElDialogGrowService(ILang lang)
        {
            Lang = lang;
        }

        /// <summary>
        /// 库级词条。确认/取消按钮的文字放在渲染片段里取值（每次渲染求值），
        /// 所以弹层开着的时候切语言也会跟着换；标题因为要进 <see cref="ElDialogGrowOptions"/>，只在打开时定型。
        /// </summary>
        private ILang Lang { get; }

        private readonly List<ElDialogGrowRequest> dialogs = new List<ElDialogGrowRequest>();

        /// <summary>打开新对话框时触发。</summary>
        public event Action<ElDialogGrowRequest> OnOpen;

        /// <summary>对话框关闭时触发，参数为回传结果（可能为 <c>null</c>）。</summary>
        public event Action<object> OnClose;

        /// <summary>请求已打开的对话框重渲染（对应 Radzen 的 <c>Refresh()</c>）。</summary>
        public event Action OnRefresh;

        /// <summary>当前打开的对话框数量。</summary>
        public int Count => dialogs.Count;

        /// <summary>
        /// 打开一个以 <typeparamref name="TComponent"/> 为内容的对话框，等待其关闭并返回结果。
        /// </summary>
        /// <typeparam name="TComponent">对话框内容组件类型（任意 <see cref="IComponent"/>）。</typeparam>
        /// <typeparam name="TResult">回传结果类型。</typeparam>
        /// <param name="title">标题。</param>
        /// <param name="parameters">传给内容组件的参数。</param>
        /// <param name="options">对话框选项。</param>
        public Task<TResult> OpenAsync<TComponent, TResult>(string title,
            IDictionary<string, object> parameters = null, ElDialogGrowOptions options = null)
            where TComponent : IComponent
        {
            options ??= new ElDialogGrowOptions();
            // 只有显式传了 title 才覆盖选项里已有的值（允许调用方先构造好 options 再传进来）。
            if (!string.IsNullOrWhiteSpace(title))
            {
                options.Title = title;
            }

            if (parameters != null)
            {
                options.Parameters = parameters;
            }

            options.ContentComponent = typeof(TComponent);
            return OpenCore<TResult>(options);
        }

        /// <summary>
        /// 打开一个以 <typeparamref name="TComponent"/> 为内容的对话框，等待其关闭并取回布尔结果。
        /// 内容组件里用 <c>DialogService.Close(true/false)</c> 表示「已确认 / 已放弃」，
        /// 因此这里不额外声明结果类型。
        /// </summary>
        /// <typeparam name="TComponent">对话框内容组件类型（任意 <see cref="IComponent"/>）。</typeparam>
        /// <param name="title">标题。</param>
        /// <param name="parameters">传给内容组件的参数。<b>必须显式传</b>（可为 <c>null</c>），
        /// 否则单类型参数的调用会与 <see cref="OpenAsync{TResult}(string, ElDialogGrowOptions)"/> 产生歧义。</param>
        /// <param name="options">对话框选项。</param>
        public Task<bool> OpenAsync<TComponent>(string title,
            IDictionary<string, object> parameters, ElDialogGrowOptions options = null)
            where TComponent : IComponent
        {
            return OpenAsync<TComponent, bool>(title, parameters, options);
        }

        /// <summary>
        /// 打开一个以内联 <c>RenderFragment</c> 为内容的对话框，等待其关闭并返回结果。
        /// </summary>
        /// <typeparam name="TResult">回传结果类型。</typeparam>
        /// <param name="title">标题。</param>
        /// <param name="childContent">正文内容，可通过级联 <see cref="ElDialogGrowRef"/> 调用 <c>CloseAsync(result)</c> 回传。</param>
        /// <param name="options">对话框选项。</param>
        public Task<TResult> OpenAsync<TResult>(string title, RenderFragment<ElDialogGrowRef> childContent,
            ElDialogGrowOptions options = null)
        {
            options ??= new ElDialogGrowOptions();
            options.Title = title;
            options.ChildContent = childContent;
            options.ContentComponent = null;
            return OpenCore<TResult>(options);
        }

        /// <summary>
        /// 打开一个不带内联内容的对话框（由 <see cref="ElDialogGrowOptions.ContentComponent"/> 或后续设置的内容决定）。
        /// 便捷重载，等价于 <c>OpenAsync&lt;TResult&gt;(title, dlg =&gt; null, options)</c>。
        /// </summary>
        public Task<TResult> OpenAsync<TResult>(string title, ElDialogGrowOptions options = null)
        {
            return OpenAsync<TResult>(title, null, options);
        }

        /// <summary>
        /// 打开一个不带返回值的对话框（非泛型便捷重载）。
        /// </summary>
        public Task<object> OpenAsync(string title, RenderFragment<ElDialogGrowRef> childContent,
            ElDialogGrowOptions options = null)
        {
            return OpenAsync<object>(title, childContent, options);
        }

        /// <summary>打开对话框但不等待结果（对应 Radzen 的 <c>Open</c>）。</summary>
        public void Open(string title, RenderFragment<ElDialogGrowRef> childContent, ElDialogGrowOptions options = null)
        {
            options ??= new ElDialogGrowOptions();
            options.Title = title;
            options.ChildContent = childContent;
            options.ContentComponent = null;
            OpenDialog(options);
        }

        /// <summary>
        /// 关闭栈顶对话框。这是「编程式强制关闭」路径，<b>不受</b>内容组件 CanClose 限制
        /// （对齐 Radzen 的 <c>DialogService.Close()</c>）。
        /// </summary>
        public void Close(object result = null)
        {
            CloseRequest(dialogs.LastOrDefault(), result);
        }

        /// <summary>
        /// 关闭指定请求。有宿主项监听时交由它走对话框自身的关闭管线（过渡 + 解锁滚动 + 回传），
        /// 由宿主在收到回报后把该项从渲染列表摘除；无宿主时（如单元测试）直接完成任务。
        /// </summary>
        private void CloseRequest(ElDialogGrowRequest request, object result)
        {
            if (request == null || !dialogs.Remove(request))
            {
                return;
            }

            if (request.HasForceCloseHandler)
            {
                request.RequestForceClose(result);
            }
            else
            {
                OnClose?.Invoke(result);
                request.Complete(result);
            }
        }

        /// <summary>尝试关闭栈顶对话框；若 <see cref="ElDialogGrowOptions.CanClose"/> 返回 <c>false</c> 则不关闭。</summary>
        public async Task<bool> TryCloseAsync(object result = null)
        {
            var request = dialogs.LastOrDefault();
            if (request == null)
            {
                return false;
            }

            if (request.Options?.CanClose != null && !await request.Options.CanClose())
            {
                return false;
            }

            Close(result);
            return true;
        }

        /// <summary>请求当前栈顶对话框重渲染。</summary>
        public void Refresh()
        {
            OnRefresh?.Invoke();
        }

        /// <summary>
        /// 确认对话框（对应 Radzen 的 <c>Confirm</c>）。按钮渲染在官方 <c>.el-dialog__footer</c>：
        /// 次按钮在前、主按钮在最后，主按钮标 <c>autofocus</c> 以保住 Enter＝「确定」。
        /// </summary>
        /// <param name="message">提示内容。</param>
        /// <param name="title">标题。</param>
        /// <param name="options">选项，可额外设置 <c>OkButtonText</c> / <c>CancelButtonText</c>。</param>
        /// <returns>点「确定」返回 <c>true</c>，否则 <c>false</c>。</returns>
        public async Task<bool> ConfirmAsync(string message, string title = null, ElDialogGrowConfirmOptions options = null)
        {
            // 标题要进 options/DialogOption，只能在打开时定型；按钮文字才是渲染期取词
            title ??= Lang.T("el.messagebox.title", "提示");
            options ??= new ElDialogGrowConfirmOptions();
            options.Title = title;
            options.Role = "alertdialog";
            options.AlignCenter = true;
            ApplyConfirmLayout(options);

            // 按钮区挂到官方 .el-dialog__footer 节点（EP Dialog 示例与 MessageBox 都是次按钮在前、主按钮在最后），
            // 不再塞进正文里用自造类居中；正文只剩一行提示，内边距就按对话框本来的节奏走。
            options.FooterContent = dlg => BuildConfirmFooter(dlg,
                Lang.Text("el.messagebox.confirm", options.OkButtonText, "确定"),
                Lang.Text("el.messagebox.cancel", options.CancelButtonText, "取消"));

            var result = await OpenAsync<object>(title, _ => builder =>
            {
                builder.OpenElement(0, "p");
                builder.AddAttribute(1, "class", "el-dialog-grow__confirm-message");
                builder.AddContent(2, message);
                builder.CloseElement();
            }, options);

            return result is true;
        }

        /// <summary>
        /// 确认/警告框的页脚按钮条。<paramref name="okText"/> 为主按钮（<c>true</c>），
        /// <paramref name="cancelText"/> 非空时先渲染次按钮（<c>false</c>）。
        /// </summary>
        private RenderFragment BuildConfirmFooter(ElDialogGrowRef dlg, string okText, string cancelText) => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "el-dialog-grow__confirm-buttons");
            var seq = 2;
            if (!string.IsNullOrEmpty(cancelText))
            {
                builder.OpenComponent<ElButton>(seq++);
                builder.AddAttribute(seq++, nameof(ElButton.OnClick),
                    EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this,
                        () => dlg.CloseAsync(false)));
                builder.AddAttribute(seq++, nameof(ElButton.ChildContent), (RenderFragment)(b => b.AddContent(0, cancelText)));
                builder.CloseComponent();
            }
            // 主按钮排在最后（视觉口径），但要它接住 Enter：autofocus 由 elementDialogFocusFirst 优先认领。
            builder.OpenComponent<ElButton>(seq++);
            builder.AddAttribute(seq++, nameof(ElButton.Type), ButtonType.Primary);
            builder.AddAttribute(seq++, nameof(ElButton.Autofocus), true);
            builder.AddAttribute(seq++, nameof(ElButton.OnClick),
                EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this,
                    () => dlg.CloseAsync(true)));
            builder.AddAttribute(seq++, nameof(ElButton.ChildContent), (RenderFragment)(b => b.AddContent(0, okText)));
            builder.CloseComponent();
            builder.CloseElement();
        };

        /// <summary>警告对话框（对应 Radzen 的 <c>Alert</c>）。</summary>
        /// <param name="message">提示内容。</param>
        /// <param name="title">标题。</param>
        /// <param name="options">选项，可设置 <see cref="ElDialogGrowOkOptions.OkButtonText"/>。</param>
        public Task AlertAsync(string message, string title = null, ElDialogGrowOkOptions options = null)
        {
            title ??= Lang.T("el.messagebox.title", "提示");
            options ??= new ElDialogGrowOkOptions();
            options.Title = title;
            options.Role = "alertdialog";
            options.AlignCenter = true;
            ApplyConfirmLayout(options);

            options.FooterContent = dlg => BuildConfirmFooter(dlg, options.OkButtonText ?? "知道了", null);

            return OpenAsync<object>(title, _ => builder =>
            {
                builder.OpenElement(0, "p");
                builder.AddAttribute(1, "class", "el-dialog-grow__confirm-message");
                builder.AddContent(2, message);
                builder.CloseElement();
            }, options);
        }

        /// <summary>
        /// 确认/警告框的排版口径：宽度按 EP MessageBox 的 420px（对话框类默认 50%，
        /// 一行提示会被拉成横幅），并给正文区加 <c>el-dialog-grow__confirm-body</c>
        /// 让内容贴标题（默认 body 上下等距内边距会把一行提示夹成垂直居中）。
        /// 调用方显式给过 Width 时不覆盖。
        /// </summary>
        private static void ApplyConfirmLayout(ElDialogGrowOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Width))
            {
                options.Width = "420px";
            }
            options.ContentCssClass = string.IsNullOrEmpty(options.ContentCssClass)
                ? "el-dialog-grow__confirm-body"
                : options.ContentCssClass + " el-dialog-grow__confirm-body";
        }

        private Task<TResult> OpenCore<TResult>(ElDialogGrowOptions options)
        {
            var request = OpenDialog(options);
            return AwaitResult<TResult>(request);
        }

        private ElDialogGrowRequest OpenDialog(ElDialogGrowOptions options)
        {
            var completion = new TaskCompletionSource<object>();
            var request = new ElDialogGrowRequest
            {
                Options = options,
                Completion = completion
            };
            dialogs.Add(request);
            OnOpen?.Invoke(request);
            return request;
        }

        private async Task<TResult> AwaitResult<TResult>(ElDialogGrowRequest request)
        {
            var result = await request.Completion.Task;
            if (result == null)
            {
                return default;
            }

            if (result is TResult typed)
            {
                return typed;
            }

            // 内容组件回传的是基类型 / 无类型时做一次类型转换，失败按默认值处理（不抛给调用方）。
            try
            {
                return (TResult)Convert.ChangeType(result, typeof(TResult));
            }
            catch (Exception) when (result is IConvertible)
            {
                return default;
            }
            catch (InvalidCastException)
            {
                return default;
            }
        }

        internal void CompleteDialog(ElDialogGrowRequest request, object result)
        {
            dialogs.Remove(request);
            OnClose?.Invoke(result);
            request.Complete(result);
        }

        internal void RaiseRefresh()
        {
            OnRefresh?.Invoke();
        }
    }

    /// <summary>一次服务化对话框打开的内部状态。</summary>
    public class ElDialogGrowRequest
    {
        internal TaskCompletionSource<object> Completion { get; set; }
        internal ElDialogGrowOptions Options { get; set; }

        /// <summary>本次打开使用的选项。</summary>
        public ElDialogGrowOptions Option => Options;

        /// <summary>
        /// 宿主项订阅此事件以响应「服务发起的编程式关闭」：走对话框自身的关闭管线
        /// （绕过 CanClose 拦截、保留过渡动画并解锁 body 滚动），最终回报宿主把本项摘除。
        /// </summary>
        internal event Action<object> ForceCloseRequested;

        /// <summary>是否有宿主项在监听强制关闭（没有宿主时回退为直接完成任务）。</summary>
        internal bool HasForceCloseHandler => ForceCloseRequested != null;

        /// <summary>通知宿主项执行强制关闭。</summary>
        internal void RequestForceClose(object result)
        {
            ForceCloseRequested?.Invoke(result);
        }

        /// <summary>完成任务并回传结果（幂等）。</summary>
        internal void Complete(object result)
        {
            Completion?.TrySetResult(result);
        }
    }

    /// <summary>服务化对话框关闭事件参数（携带请求与回传结果）。</summary>
    public class ElDialogGrowClosedEventArgs
    {
        public ElDialogGrowClosedEventArgs(ElDialogGrowRequest request, object result)
        {
            Request = request;
            Result = result;
        }

        /// <summary>被关闭的请求。</summary>
        public ElDialogGrowRequest Request { get; }

        /// <summary>内容组件回传的结果（可能为 <c>null</c>）。</summary>
        public object Result { get; }
    }

    /// <summary>「确定」类对话框选项。</summary>
    public class ElDialogGrowOkOptions : ElDialogGrowOptions
    {
        /// <summary>确定按钮文案。</summary>
        public string OkButtonText { get; set; }
    }

    /// <summary>「确认」类对话框选项。</summary>
    public class ElDialogGrowConfirmOptions : ElDialogGrowOkOptions
    {
        /// <summary>取消按钮文案。</summary>
        public string CancelButtonText { get; set; }
    }
}