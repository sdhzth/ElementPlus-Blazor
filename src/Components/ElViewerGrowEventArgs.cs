using System;

namespace ElementPlus
{
    /// <summary>
    /// 当前文档发生变化时的事件参数。
    /// </summary>
    public sealed class ElViewerDocumentEventArgs : EventArgs
    {
        /// <summary>新的文档；关闭时为 <c>null</c>。</summary>
        public ElViewerDocument Document { get; set; }

        /// <summary>文档类型；没有文档时为 <see cref="ElViewerDocumentKind.Unknown"/>。</summary>
        public ElViewerDocumentKind Kind
        {
            get { return Document == null ? ElViewerDocumentKind.Unknown : Document.Kind; }
        }
    }

    /// <summary>
    /// 预览状态发生变化（空 / 加载 / 就绪 / 错误 / 不支持）时的事件参数。
    /// <para>参考实现只有一串拼接出来的状态字符串，宿主无法程序化判断；这里把状态与视图参数一起给出。</para>
    /// </summary>
    public sealed class ElViewerStateChangedEventArgs : EventArgs
    {
        /// <summary>新状态。</summary>
        public ElViewerStatus Status { get; set; }

        /// <summary>上一次状态。</summary>
        public ElViewerStatus PreviousStatus { get; set; }

        /// <summary>当前文档。</summary>
        public ElViewerDocument Document { get; set; }

        /// <summary>当前缩放。</summary>
        public double Zoom { get; set; }

        /// <summary>当前旋转角度（0 / 90 / 180 / 270）。</summary>
        public int Rotation { get; set; }

        /// <summary>当前页码。</summary>
        public int PageNumber { get; set; }

        /// <summary>总页数。</summary>
        public int PageCount { get; set; }

        /// <summary>状态附带的说明文案。</summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// 出错时的事件参数。
    /// </summary>
    public sealed class ElViewerErrorEventArgs : EventArgs
    {
        /// <summary>错误信息。</summary>
        public string Message { get; set; }

        /// <summary>原始异常（可能为 <c>null</c>）。</summary>
        public Exception Exception { get; set; }

        /// <summary>出错时正在处理的文档（可能为 <c>null</c>）。</summary>
        public ElViewerDocument Document { get; set; }
    }
}
