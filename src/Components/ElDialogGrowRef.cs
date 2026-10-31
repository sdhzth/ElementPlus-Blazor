using System;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 对话框内容的**级联引用**。通过 <c>CascadingValue</c> 下发给对话框内部任意组件，
    /// 让内容组件可以关闭对话框并回传结果、改标题 / 宽高、触发重渲染 ——
    /// 对应 Radzen Blazor 示例「Update dialog properties」里的 <c>Dialog</c> 级联值。
    ///
    /// 用法（内容组件内）：
    /// <code>
    /// [CascadingParameter] private ElDialogGrowRef Dialog { get; set; }
    /// ...
    /// await Dialog.CloseAsync(model);   // 关闭并回传结果
    /// </code>
    /// </summary>
    public class ElDialogGrowRef
    {
        internal Func<object, Task> CloseCallback { get; set; }
        internal Func<Task> RefreshCallback { get; set; }

        // 声明式用法（没有 ElDialogGrowOptions）时，属性赋值直接落到组件参数上。
        internal Action<string> TitleSetter { get; set; }
        internal Action<string> WidthSetter { get; set; }
        internal Action<string> HeightSetter { get; set; }

        /// <summary>服务化打开时的选项对象；内联用法下为 <c>null</c>。</summary>
        public ElDialogGrowOptions OptionsRef { get; internal set; }

        /// <summary>所属对话框组件（可为空）。</summary>
        internal ElDialogGrow Target { get; set; }

        /// <summary>当前对话框标题；赋值即刷新（等价于 Radzen 的 <c>Dialog.Title</c>）。</summary>
        public string Title
        {
            get => OptionsRef != null ? OptionsRef.Title : Target?.Title;
            set
            {
                if (OptionsRef != null)
                {
                    OptionsRef.Title = value;
                }
                else
                {
                    TitleSetter?.Invoke(value);
                }
            }
        }

        /// <summary>当前宽度；赋值即刷新。</summary>
        public string Width
        {
            get => OptionsRef != null ? OptionsRef.Width : Target?.Width;
            set
            {
                if (OptionsRef != null)
                {
                    OptionsRef.Width = value;
                }
                else
                {
                    WidthSetter?.Invoke(value);
                }
            }
        }

        /// <summary>当前高度；赋值即刷新。</summary>
        public string Height
        {
            get => OptionsRef != null ? OptionsRef.Height : Target?.Height;
            set
            {
                if (OptionsRef != null)
                {
                    OptionsRef.Height = value;
                }
                else
                {
                    HeightSetter?.Invoke(value);
                }
            }
        }

        /// <summary>是否处于打开状态。</summary>
        public bool IsOpen => Target?.Visible ?? false;

        /// <summary>关闭对话框并回传结果。</summary>
        public Task CloseAsync(object result = null)
        {
            return CloseCallback != null ? CloseCallback(result) : Task.CompletedTask;
        }

        /// <summary>关闭对话框并回传指定类型的强类型结果。</summary>
        public Task CloseAsync<T>(T result)
        {
            return CloseAsync((object)result);
        }

        /// <summary>仅关闭，不回传结果。</summary>
        public Task CloseAsync()
        {
            return CloseAsync(null);
        }

        /// <summary>请求重渲染当前对话框（选项被外部代码改过之后调用）。</summary>
        public void Refresh()
        {
            RefreshCallback?.Invoke();
        }

        /// <summary>
        /// 由 <see cref="ElDialogGrow"/> 在渲染时接线：把关闭 / 刷新 / 属性赋值能力接到本引用上。
        /// 宿主（<c>ElDialogGrowHostItem</c>）可以自带一个空实例传进来，这里负责补齐能力 ——
        /// 否则内容组件拿到的是一个「什么都不做」的 ref，<c>CloseAsync</c> 会静默无效。
        /// </summary>
        internal void Bind(ElDialogGrow target, ElDialogGrowOptions options)
        {
            Target = target;
            OptionsRef = options;
            if (CloseCallback == null)
            {
                CloseCallback = result => target.CloseAsync(result);
            }

            if (RefreshCallback == null)
            {
                RefreshCallback = () =>
                {
                    target.Refresh();
                    return Task.CompletedTask;
                };
            }

            if (TitleSetter == null)
            {
                TitleSetter = value => target.MutableTitle = value;
            }

            if (WidthSetter == null)
            {
                WidthSetter = value => target.MutableWidth = value;
            }

            if (HeightSetter == null)
            {
                HeightSetter = value => target.MutableHeight = value;
            }
        }
    }
}