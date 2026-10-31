using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// EP Menu 的 <c>open</c> / <c>close</c> 事件参数：
    /// <c>(index: string, indexPath: string[]) </c>。
    /// </summary>
    public class MenuOpenEventArgs
    {
        /// <summary>展开 / 收起的 sub-menu 的 index。</summary>
        public string Index { get; set; }

        /// <summary>从根菜单到该 sub-menu 的 index 路径。</summary>
        public IReadOnlyList<string> IndexPath { get; set; }
    }
}
