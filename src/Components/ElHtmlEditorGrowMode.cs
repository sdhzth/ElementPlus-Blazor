using System;

namespace ElementPlus
{
    /// <summary>
    /// ElHtmlEditorGrow 的编辑模式。工具栏可按模式启用/禁用某个工具。
    /// 对应参考实现 Radzen 的 <c>HtmlEditorMode</c>。
    /// </summary>
    [Flags]
    public enum ElHtmlEditorGrowMode
    {
        /// <summary>可视化（所见即所得）编辑模式。</summary>
        Design = 1,

        /// <summary>HTML 源码编辑模式。</summary>
        Source = 2,

        /// <summary>两种模式都适用。</summary>
        All = Design | Source
    }
}
