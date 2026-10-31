using System;
using System.Collections.Generic;
using System.Text;

namespace ElementPlus
{
    /// <summary>
    /// 标签尺寸（对齐 Element Plus Tag size: large / default / small）
    /// </summary>
    public enum TagSize
    {
        Large = 0,
        Default = 1,
        Medium = 1,   // 别名，等于 Default
        Small = 2,
        Mini = 3      // 超小（非正式 API，保留兼容）
    }
}