using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElementPlus
{
    /// <summary>
    /// ElHtmlEditorGrow 的工具栏命令状态（当前选区/光标处的格式）。
    /// <para>
    /// 对应参考实现 Radzen 的 <c>RadzenHtmlEditorCommandState</c>。本类为**纯 C# 模型**：
    /// 浏览器端只负责回传一段 JSON，反序列化由 <see cref="Parse"/> 完成，因此可在无浏览器环境下（单元测试、SSR）验证。
    /// </para>
    /// </summary>
    public class ElHtmlEditorGrowState
    {
        /// <summary>当前编辑区的完整 HTML。</summary>
        public string Html { get; set; }

        /// <summary>撤销是否可用。</summary>
        public bool Undo { get; set; }

        /// <summary>重做是否可用。</summary>
        public bool Redo { get; set; }

        /// <summary>加粗是否激活。</summary>
        public bool Bold { get; set; }

        /// <summary>斜体是否激活。</summary>
        public bool Italic { get; set; }

        /// <summary>下划线是否激活。</summary>
        public bool Underline { get; set; }

        /// <summary>删除线是否激活。</summary>
        public bool StrikeThrough { get; set; }

        /// <summary>下标是否激活。</summary>
        public bool Subscript { get; set; }

        /// <summary>上标是否激活。</summary>
        public bool Superscript { get; set; }

        /// <summary>左对齐是否激活。</summary>
        public bool JustifyLeft { get; set; }

        /// <summary>居中是否激活。</summary>
        public bool JustifyCenter { get; set; }

        /// <summary>右对齐是否激活。</summary>
        public bool JustifyRight { get; set; }

        /// <summary>两端对齐是否激活。</summary>
        public bool JustifyFull { get; set; }

        /// <summary>光标处是否存在可取消的链接。</summary>
        public bool Unlink { get; set; }

        /// <summary>光标处是否存在表格（用于启用表格工具）。</summary>
        public bool InTable { get; set; }

        /// <summary>当前字体名。</summary>
        public string FontName { get; set; }

        /// <summary>当前字号。</summary>
        public string FontSize { get; set; }

        /// <summary>当前块级格式（<c>p</c> / <c>h1</c>…）。</summary>
        public string FormatBlock { get; set; }

        /// <summary>命令是否执行成功（表格命令被拒绝时置 false）。</summary>
        public bool Success { get; set; } = true;

        /// <summary>命令失败时的原因代号，便于本地化。</summary>
        public string Message { get; set; }

        /// <summary>最近一次工具执行后由 JS 回传的状态（不在 JSON 中，由组件维护）。</summary>
        [JsonIgnore]
        public bool IsDefault => string.IsNullOrEmpty(Html)
            && !Undo && !Redo && !Bold && !Italic && !Underline && !StrikeThrough
            && !Subscript && !Superscript
            && !JustifyLeft && !JustifyCenter && !JustifyRight && !JustifyFull
            && !Unlink && !InTable
            && string.IsNullOrEmpty(FontName) && string.IsNullOrEmpty(FontSize) && string.IsNullOrEmpty(FormatBlock);

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        /// <summary>
        /// 从浏览器回传的 JSON 解析状态。<paramref name="json"/> 为 <c>null</c>／空／非法时返回默认状态
        /// （<see cref="Success"/> 为 <c>true</c>），以便在无 JS 环境（bUnit、SSR）下安全降级。
        /// </summary>
        public static ElHtmlEditorGrowState Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new ElHtmlEditorGrowState();
            }

            try
            {
                return JsonSerializer.Deserialize<ElHtmlEditorGrowState>(json, Options) ?? new ElHtmlEditorGrowState();
            }
            catch (JsonException)
            {
                return new ElHtmlEditorGrowState();
            }
        }

        /// <summary>把当前状态序列化为 JSON（与浏览器端格式一致）。</summary>
        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.Never
            });
        }
    }
}
