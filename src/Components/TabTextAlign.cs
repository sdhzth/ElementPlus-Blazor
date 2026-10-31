using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 页签文字对齐方式，用于 <see cref="ElTabs.TabTextAlign"/>。
    /// 与 <see cref="TabPosition"/>（整条页签栏在容器里的位置：上/下/左/右）是两回事。
    /// </summary>
    public enum TabTextAlign
    {
        /// <summary>靠左（与不设置时的视觉结果一致）</summary>
        Left,
        /// <summary>居中</summary>
        Center,
        /// <summary>靠右</summary>
        Right
    }
}
