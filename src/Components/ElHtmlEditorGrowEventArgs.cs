namespace ElementPlus
{
    /// <summary>
    /// 粘贴事件参数：可通过改写 <see cref="Html"/> 过滤粘贴进来的内容。
    /// <para>对应参考实现 Radzen 的 <c>HtmlEditorPasteEventArgs</c>。</para>
    /// </summary>
    public class ElHtmlEditorGrowPasteArgs
    {
        /// <summary>被粘贴的 HTML 内容（可改写成过滤后的内容）。</summary>
        public string Html { get; set; }
    }

    /// <summary>
    /// 命令执行事件参数。
    /// <para>对应参考实现 Radzen 的 <c>HtmlEditorExecuteEventArgs</c>。</para>
    /// </summary>
    public class ElHtmlEditorGrowExecuteArgs
    {
        /// <summary>发起命令的编辑器实例。</summary>
        public ElHtmlEditorGrow Editor { get; set; }

        /// <summary>命令名（自定义工具 <c>CommandName</c> 与内置命令共用同一通道）。</summary>
        public string CommandName { get; set; }

        /// <summary>命令参数（可为 <c>null</c>）。</summary>
        public string Value { get; set; }
    }

    /// <summary>图片上传完成事件参数。</summary>
    public class ElHtmlEditorGrowUploadArgs
    {
        /// <summary>服务端返回的原始响应文本。</summary>
        public string RawResponse { get; set; }

        /// <summary>从响应中解析出的图片地址（可能为 <c>null</c>）。</summary>
        public string Url { get; set; }

        /// <summary>上传是否成功。</summary>
        public bool Success { get; set; } = true;

        /// <summary>失败原因。</summary>
        public string Error { get; set; }
    }
}
