using System;

namespace ElementPlus
{
    /// <summary>
    /// <see cref="ElViewerGrow"/> 的手写内联 SVG 图标（24×24，线性风格）。
    /// <para>
    /// 与 <c>ElHtmlEditorGrow</c> 一样：不引图标字体、不引图标包，全部为仓库自带字符串，
    /// 因此离线可用、可被单元测试断言，也不会出现字体加载失败的方块。
    /// </para>
    /// </summary>
    public static class ElViewerGrowIcons
    {
        /// <summary>空态的文档图标。</summary>
        public const string Document = "<path d=\"M7 3h7l5 5v13a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1z\"/>"
            + "<path d=\"M14 3v5h5\"/>"
            + "<path d=\"M9.5 13h6\"/>"
            + "<path d=\"M9.5 17h6\"/>";

        /// <summary>错误 / 不支持状态的警示图标。</summary>
        public const string Warning = "<path d=\"M12 4l9 16H3z\"/>"
            + "<path d=\"M12 10v4\"/>"
            + "<path d=\"M12 17h.01\"/>";

        private const string Folder = "<path d=\"M3 7.2A1.2 1.2 0 0 1 4.2 6h3.4l1.8 2.2h10.4A1.2 1.2 0 0 1 21 9.4v8.4A1.2 1.2 0 0 1 19.8 19H4.2A1.2 1.2 0 0 1 3 17.8z\"/>";

        private const string Globe = "<circle cx=\"12\" cy=\"12\" r=\"8\"/>"
            + "<path d=\"M4 12h16\"/>"
            + "<path d=\"M12 4a13 13 0 0 1 0 16a13 13 0 0 1 0-16\"/>";

        private const string ZoomIn = "<circle cx=\"10.5\" cy=\"10.5\" r=\"6.5\"/>"
            + "<path d=\"M15.4 15.4L21 21\"/>"
            + "<path d=\"M10.5 7.6v5.8\"/>"
            + "<path d=\"M7.6 10.5h5.8\"/>";

        private const string ZoomOut = "<circle cx=\"10.5\" cy=\"10.5\" r=\"6.5\"/>"
            + "<path d=\"M15.4 15.4L21 21\"/>"
            + "<path d=\"M7.6 10.5h5.8\"/>";

        private const string Crosshair = "<circle cx=\"12\" cy=\"12\" r=\"7\"/>"
            + "<path d=\"M12 2.5v3.5\"/>"
            + "<path d=\"M12 18v3.5\"/>"
            + "<path d=\"M2.5 12h3.5\"/>"
            + "<path d=\"M18 12h3.5\"/>";

        private const string RotateLeft = "<path d=\"M4 12a8 8 0 1 1 2.6 5.9\"/>"
            + "<path d=\"M3.4 6.6V12H9\"/>";

        private const string RotateRight = "<path d=\"M20 12a8 8 0 1 0-2.6 5.9\"/>"
            + "<path d=\"M20.6 6.6V12H15\"/>";

        private const string Maximize = "<path d=\"M4 9V5.5A1.5 1.5 0 0 1 5.5 4H9\"/>"
            + "<path d=\"M15 4h3.5A1.5 1.5 0 0 1 20 5.5V9\"/>"
            + "<path d=\"M20 15v3.5a1.5 1.5 0 0 1-1.5 1.5H15\"/>"
            + "<path d=\"M9 20H5.5A1.5 1.5 0 0 1 4 18.5V15\"/>";

        private const string Minimize = "<path d=\"M4 8.5h4.5V4\"/>"
            + "<path d=\"M20 8.5h-4.5V4\"/>"
            + "<path d=\"M4 15.5h4.5V20\"/>"
            + "<path d=\"M20 15.5h-4.5V20\"/>";

        private const string NewTab = "<path d=\"M14 4h6v6\"/>"
            + "<path d=\"M20 4l-8 8\"/>"
            + "<path d=\"M18 14v4.5A1.5 1.5 0 0 1 16.5 20h-11A1.5 1.5 0 0 1 4 18.5v-11A1.5 1.5 0 0 1 5.5 6H10\"/>";

        private const string Download = "<path d=\"M12 4v11\"/>"
            + "<path d=\"M7.5 10.5L12 15l4.5-4.5\"/>"
            + "<path d=\"M5 19h14\"/>";

        private const string Printer = "<path d=\"M7 8V4h10v4\"/>"
            + "<path d=\"M7 17H5.2A1.2 1.2 0 0 1 4 15.8v-5.6A1.2 1.2 0 0 1 5.2 9h13.6A1.2 1.2 0 0 1 20 10.2v5.6a1.2 1.2 0 0 1-1.2 1.2H17\"/>"
            + "<path d=\"M7 14h10v6H7z\"/>";

        private const string Reload = "<path d=\"M20.5 12a8.5 8.5 0 1 1-2.5-6\"/>"
            + "<path d=\"M20.5 3.5V9H15\"/>";

        private const string Close = "<path d=\"M6.5 6.5l11 11\"/>"
            + "<path d=\"M17.5 6.5l-11 11\"/>";

        private const string ChevronLeft = "<path d=\"M15 5l-7 7 7 7\"/>";

        private const string ChevronRight = "<path d=\"M9 5l7 7-7 7\"/>";

        /// <summary>
        /// 把图标内部标记包装成完整的 <c>svg</c> 字符串。
        /// 统一在这里定义尺寸与描边属性，组件标记里只需输出一个 <c>MarkupString</c>。
        /// </summary>
        public static string Svg(string inner)
        {
            return "<svg class=\"el-viewer-grow__icon\" viewBox=\"0 0 24 24\" width=\"16\" height=\"16\""
                + " fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\""
                + " stroke-linejoin=\"round\" aria-hidden=\"true\" focusable=\"false\">"
                + (inner ?? string.Empty)
                + "</svg>";
        }

        /// <summary>按工具栏动作取图标的内部 SVG 标记（只含图形元素，不含 <c>svg</c> 外壳）。</summary>
        public static string Get(ElViewerToolbarAction action)
        {
            switch (action)
            {
                case ElViewerToolbarAction.OpenFile:
                    return Folder;
                case ElViewerToolbarAction.OpenUrl:
                    return Globe;
                case ElViewerToolbarAction.ZoomIn:
                    return ZoomIn;
                case ElViewerToolbarAction.ZoomOut:
                    return ZoomOut;
                case ElViewerToolbarAction.ZoomReset:
                    return Crosshair;
                case ElViewerToolbarAction.RotateLeft:
                    return RotateLeft;
                case ElViewerToolbarAction.RotateRight:
                    return RotateRight;
                case ElViewerToolbarAction.Maximize:
                    return Maximize;
                case ElViewerToolbarAction.OpenInNewTab:
                    return NewTab;
                case ElViewerToolbarAction.Download:
                    return Download;
                case ElViewerToolbarAction.Print:
                    return Printer;
                case ElViewerToolbarAction.Reload:
                    return Reload;
                case ElViewerToolbarAction.Close:
                    return Close;
                case ElViewerToolbarAction.PrevPage:
                    return ChevronLeft;
                case ElViewerToolbarAction.NextPage:
                    return ChevronRight;
                default:
                    return string.Empty;
            }
        }

        /// <summary>按状态取占位图标（空态 / 错误 / 不支持）。</summary>
        public static string Get(ElViewerStatus status)
        {
            return status == ElViewerStatus.Empty ? Document : Warning;
        }
    }
}
