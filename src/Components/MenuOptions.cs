using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public class MenuOptions
    {
        public string TextColor { get; set; }

        public string ActiveTextColor { get; set; }

        public string BorderColor { get; set; }

        public string BackgroundColor { get; set; }

        public string HoverColor { get; set; }

        public string DefaultActiveIndex { get; set; }

        public MenuMode Mode { get; set; }

        public bool Disabled { get; set; }

        public bool Collapse { get; set; }

        public bool Router { get; set; } = true;

        public MenuTrigger MenuTrigger { get; set; } = MenuTrigger.Hover;

        public string PopperClass { get; set; }

        public string PopperStyle { get; set; }

        public string PopperEffect { get; set; } = "light";

        public MenuTheme Theme { get; set; } = MenuTheme.Light;

        /// <summary>EP <c>show-timeout</c>：子菜单出现前的延迟（毫秒）。</summary>
        public int ShowTimeout { get; set; } = 300;

        /// <summary>EP <c>hide-timeout</c>：子菜单消失前的延迟（毫秒）。</summary>
        public int HideTimeout { get; set; } = 300;

        /// <summary>EP <c>popper-offset</c>：弹出层偏移量。</summary>
        public int PopperOffset { get; set; } = 6;

        /// <summary>
        /// 官方 <c>ellipsis</c>（EP 2.14.0 <c>menu.ts:124</c> 默认 true）：菜单项与子菜单标题文字超长时省略号截断。
        /// 本库实现为文字级 CSS 截断（EP 的「折叠进更多弹层」未实现），见 <see cref="ElMenu.Ellipsis"/>。
        /// </summary>
        public bool Ellipsis { get; set; } = true;

        /// <summary>当前层级，用于输出 <c>--el-menu-level</c>。</summary>
        public int Level { get; set; }
    }
}
