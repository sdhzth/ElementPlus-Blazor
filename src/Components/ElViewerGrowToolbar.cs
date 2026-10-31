using System;
using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// <see cref="ElViewerGrow"/> 的工具栏描述表。
    /// <para>
    /// 参考实现把按钮硬编码在 Razor 标记里（Bootstrap 的 <c>btn btn-sm btn-outline-secondary</c>），
    /// 既不能配置也不能测试；这里改为可枚举的数据表，与 <c>ElHtmlEditorGrow</c> 的工具栏做法一致。
    /// </para>
    /// </summary>
    public static class ElViewerGrowToolbar
    {
        /// <summary>
        /// 默认工具条：打开文件 / 输入地址 ‖ 缩小 放大 重置 ‖ 左转 右转 ‖ 最大化 新标签页 下载 打印 ‖ 重新加载 关闭 ‖ 上下页。
        /// </summary>
        public static readonly ElViewerTool[] Default = new[]
        {
            ElViewerTool.Of(ElViewerToolbarAction.OpenFile),
            ElViewerTool.Of(ElViewerToolbarAction.OpenUrl),
            ElViewerTool.Separator(),
            ElViewerTool.Of(ElViewerToolbarAction.ZoomOut),
            ElViewerTool.Of(ElViewerToolbarAction.ZoomIn),
            ElViewerTool.Of(ElViewerToolbarAction.ZoomReset),
            ElViewerTool.Separator(),
            ElViewerTool.Of(ElViewerToolbarAction.RotateLeft),
            ElViewerTool.Of(ElViewerToolbarAction.RotateRight),
            ElViewerTool.Separator(),
            ElViewerTool.Of(ElViewerToolbarAction.Maximize),
            ElViewerTool.Of(ElViewerToolbarAction.OpenInNewTab),
            ElViewerTool.Of(ElViewerToolbarAction.Download),
            ElViewerTool.Of(ElViewerToolbarAction.Print),
            ElViewerTool.Separator(),
            ElViewerTool.Of(ElViewerToolbarAction.Reload),
            ElViewerTool.Of(ElViewerToolbarAction.Close),
            ElViewerTool.Separator(),
            ElViewerTool.Of(ElViewerToolbarAction.PrevPage),
            ElViewerTool.Of(ElViewerToolbarAction.NextPage)
        };

        /// <summary>精简工具条：只保留缩放与下载。</summary>
        public static readonly ElViewerTool[] Minimal = new[]
        {
            ElViewerTool.Of(ElViewerToolbarAction.ZoomOut),
            ElViewerTool.Of(ElViewerToolbarAction.ZoomIn),
            ElViewerTool.Of(ElViewerToolbarAction.ZoomReset),
            ElViewerTool.Separator(),
            ElViewerTool.Of(ElViewerToolbarAction.Download)
        };

        /// <summary>按动作序列构建工具条（<see cref="ElViewerToolbarAction.Separator"/> 即分隔符）。</summary>
        public static ElViewerTool[] Build(params ElViewerToolbarAction[] actions)
        {
            if (actions == null || actions.Length == 0)
            {
                return new ElViewerTool[0];
            }

            var tools = new List<ElViewerTool>(actions.Length);

            foreach (var action in actions)
            {
                tools.Add(action == ElViewerToolbarAction.Separator
                    ? ElViewerTool.Separator()
                    : ElViewerTool.Of(action));
            }

            return tools.ToArray();
        }

        /// <summary>从已有工具条里剔除指定动作。</summary>
        public static ElViewerTool[] Without(ElViewerTool[] source, params ElViewerToolbarAction[] actions)
        {
            if (source == null || source.Length == 0)
            {
                return new ElViewerTool[0];
            }

            if (actions == null || actions.Length == 0)
            {
                return source;
            }

            var removed = new List<ElViewerToolbarAction>(actions);
            var kept = new List<ElViewerTool>(source.Length);

            foreach (var tool in source)
            {
                if (tool != null && !removed.Contains(tool.Action))
                {
                    kept.Add(tool);
                }
            }

            return kept.ToArray();
        }

        /// <summary>
        /// 该动作是否需要「已经加载文档」才有意义。
        /// 打开类动作永远可用，其余（缩放/旋转/下载/关闭/翻页…）在没有文档时应禁用而不是报错。
        /// </summary>
        public static bool RequiresDocument(ElViewerToolbarAction action)
        {
            return action != ElViewerToolbarAction.Separator
                && action != ElViewerToolbarAction.OpenFile
                && action != ElViewerToolbarAction.OpenUrl;
        }

        /// <summary>该动作是否渲染成链接（下载 / 新标签页）而不是按钮。</summary>
        public static bool IsLink(ElViewerToolbarAction action)
        {
            return action == ElViewerToolbarAction.Download || action == ElViewerToolbarAction.OpenInNewTab;
        }
    }
}
