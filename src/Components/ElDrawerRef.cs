using System;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 抽屉内容的**级联引用**。由 <see cref="ElDrawer"/> 通过 <c>CascadingValue</c> 下发给抽屉内部任意组件
    /// （典型场景：抽屉里渲染一个表单组件），让内容组件可以动态改抽屉标题、关闭抽屉、请求重渲染。
    ///
    /// 用法（内容组件内）：
    /// <code>
    /// [CascadingParameter] private ElDrawerRef Drawer { get; set; }
    /// ...
    /// Drawer.Title = "编辑员工";   // 动态改抽屉标题
    /// await Drawer.CloseAsync();   // 走 before-close 校验后关闭
    /// </code>
    /// </summary>
    public class ElDrawerRef
    {
        internal Func<Task> CloseCallback { get; set; }
        internal Action<string> TitleSetter { get; set; }

        /// <summary>所属抽屉组件（可为空）。</summary>
        internal ElDrawer Target { get; set; }

        /// <summary>当前抽屉标题；赋值即刷新（等价于改 <see cref="ElDrawer.Title"/> 参数）。</summary>
        public string Title
        {
            get => Target?.EffectiveTitle;
            set => TitleSetter?.Invoke(value);
        }

        /// <summary>是否处于打开状态。</summary>
        public bool IsOpen => Target != null && Target.ModelValue;

        /// <summary>关闭抽屉（会先经过 before-close 校验）。</summary>
        public Task CloseAsync()
        {
            return CloseCallback != null ? CloseCallback() : Task.CompletedTask;
        }

        /// <summary>
        /// 由 <see cref="ElDrawer"/> 在渲染时接线：把「改标题 / 关闭」能力接到本引用上。
        /// 宿主也可以自带一个空实例传进来，这里负责补齐能力 —— 否则内容组件拿到的会是一个
        /// 「什么都不做」的引用，<c>Title</c> 赋值与 <c>CloseAsync</c> 都会静默失效。
        /// </summary>
        internal void Bind(ElDrawer target)
        {
            Target = target;
            if (CloseCallback == null)
            {
                CloseCallback = () => target.HandleCloseAsync();
            }

            if (TitleSetter == null)
            {
                TitleSetter = value => target.MutableTitle = value;
            }
        }
    }
}
