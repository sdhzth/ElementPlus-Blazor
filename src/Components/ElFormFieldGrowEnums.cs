namespace ElementPlus
{
    /// <summary>
    /// 表单字段外观变体（Element Plus 自渲染版的 <c>ElFormFieldGrow</c> 专用）。
    /// 与 Radzen 的 <c>Variant</c> 语义对齐，但不引入其任何代码。
    /// </summary>
    public enum ElFormFieldVariant
    {
        /// <summary>外描边（默认）：四边完整 1px 边框，浮标上浮时用背景色断开上边框。</summary>
        Outlined = 0,

        /// <summary>填充：浅色填充 + 四边边框，浮标上浮时贴合填充色。</summary>
        Filled = 1,

        /// <summary>扁平：仅底部下划线，聚焦时下划线展开。</summary>
        Flat = 2,

        /// <summary>纯文本：无边框无背景，仅标签与内容。</summary>
        Text = 3
    }

    /// <summary>
    /// 表单字段尺寸。
    /// </summary>
    public enum ElFormFieldSize
    {
        /// <summary>小号。</summary>
        Small = 0,

        /// <summary>默认。</summary>
        Default = 1,

        /// <summary>大号。</summary>
        Large = 2
    }

    /// <summary>
    /// 标签浮动行为。三种取值互斥，映射到三个互斥的修饰类
    /// （<c>el-form-field--float-auto</c> / <c>--float-always</c> / <c>--float-never</c>），
    /// 从而在 CSS 中彻底避免特异度冲突，也不依赖 <c>:has()</c> 等新特性。
    /// </summary>
    public enum ElFormFieldFloat
    {
        /// <summary>
        /// 自动（默认）：聚焦时上浮；内容有值时上浮。
        /// “有值”检测走两条路：原生 <c>input</c>/<c>textarea</c> 的 <c>:not(:placeholder-shown)</c>
        /// （需控件带 <c>placeholder</c>），或由使用方通过 <see cref="ElFormFieldGrow.HasValue"/> 显式声明。
        /// </summary>
        Auto = 0,

        /// <summary>始终上浮（常用于纯展示型字段或已知恒有值的场景）。</summary>
        Always = 1,

        /// <summary>永不上浮（标签固定居中，等价于 Radzen 的 <c>AllowFloatingLabel="false"</c>）。</summary>
        Never = 2
    }
}
