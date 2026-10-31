
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ElementPlus
{
    public class SubMenuOption : PopupOption
    {
        public SubMenuOption()
        {
            IsShow = true;
        }
        public ElSubMenu SubMenu { get; set; }
        public MenuOptions Options { get; set; }
        public RenderFragment Content { get; set; }
        public TaskCompletionSource<int> TaskCompletionSource { get; set; }
        public Func<SubMenuOption, Task> Close { get; set; }

        internal string Placement { get; set; } = "bottom-start";

        internal string PopperClass { get; set; }

        internal string PopperStyle { get; set; }

        /// <summary>EP <c>popper-offset</c>：弹出层与触发元素的间距。</summary>
        internal int Offset { get; set; } = 6;

        /// <summary>弹层内 ul 的层级，用于输出 <c>--el-menu-level</c>。</summary>
        internal int Level { get; set; }

        /// <summary>
        /// EP <c>popper-effect</c>：弹出层的主题（dark/light）。继承自 <see cref="Options"/>。
        /// </summary>
        internal string PopperEffect { get; set; } = "dark";

        /// <summary>
        /// EP <c>hide-timeout</c>：鼠标移出弹层后的关闭延迟（继承触发子菜单的 hide-timeout）。
        /// </summary>
        internal int HideTimeoutMs { get; set; } = 300;

        public CancellationTokenSource ClosingTaskCancellationTokenSource { get; set; }
        public Task ClosingTask { get; internal set; }
    }
}
