using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public class ElMenuContainer : ElementComponentBase, IContainerComponent
    {
        internal List<IMenuItem> Children { get; set; } = new List<IMenuItem>();

        internal void AddMenuItem(IMenuItem item)
        {
            if (item == null || Children.Contains(item))
            {
                return;
            }
            Children.Add(item);
        }

        internal void RemoveMenuItem(IMenuItem item)
        {
            if (item == null)
            {
                return;
            }
            Children.Remove(item);
        }

        [CascadingParameter]
        public ElMenu TopMenu { get; set; }
        public ElementReference Container { get; set; }

        /// <summary>
        /// 当前容器在菜单树中的层级：根菜单为 0，一级子菜单为 0、其内层 ul 为 1……
        /// 用于输出 <c>--el-menu-level</c>（EP 用它逐级缩进 padding-left）。
        /// </summary>
        internal virtual int Level => 0;

        protected override bool ShouldRender()
        {
            return true;
        }
    }
}
