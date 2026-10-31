using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public interface IMenuItem
    {
        string Icon { get; set; }
        string Index { get; set; }

        /// <summary>实际使用的唯一标志：未显式指定 Index 时回退到 Label / Route。</summary>
        string EffectiveIndex { get; }

        ElMenuContainer Menu { get; set; }

        object Model { get; set; }
        MenuOptions Options { get; set; }
        bool Disabled { get; set; }

        /// <summary>当前是否处于激活态（菜单项 = 自身激活；子菜单 = 后代中存在激活项）。</summary>
        bool IsActive { get; }

        /// <summary>从根菜单到自身的 index 路径（EP <c>indexPath</c>）。</summary>
        IReadOnlyList<string> GetIndexPath();

        void Activate();

        void DeActivate();
    }
}
