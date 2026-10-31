using System;
using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    /// <summary>
    /// 表单字段容器（Element Plus 自渲染版，零 Radzen 依赖、<b>零 JS</b>）。
    /// <para>
    /// 参考 Radzen.Blazor 的 <c>RadzenFormField</c>：把任意输入控件包一层，提供
    /// 标签（可浮动）、前置/后置内容、辅助文本与校验提示，并支持 Outlined / Filled / Flat / Text 四种变体。
    /// 本组件不引用 Radzen 的任何类型或脚本。
    /// </para>
    /// <para>
    /// <b>浏览器兼容性设计（本组件的重点目标）</b>
    /// <list type="bullet">
    /// <item>Radzen 需要 JS 互操作（<c>Radzen.createFormField</c>）监听 <c>focusin</c>/<c>focusout</c> 给根元素加
    /// <c>rz-state-focused</c> 类；本组件改用 CSS <c>:focus-within</c>（Chrome 60+ / Firefox 52+ / Safari 10.1+），
    /// 因此<strong>完全不需要 JS</strong>，在静态 SSR、预渲染与离线包中均可正常工作。</item>
    /// <item>Radzen 的主题样式使用了 <c>:has()</c>（Chrome 105+）、逻辑属性 <c>inset-block-start</c>/<c>margin-block</c>
    /// （Chrome 87+）、<c>@each</c> 生成的 <c>:has(.rz-input-xs)</c> 尺寸探测等新特性；本组件全部避开，
    /// 只使用物理属性（<c>top</c>/<c>left</c>/<c>right</c>/<c>margin</c>）与<strong>单参数</strong> <c>:not()</c>。
    /// 详见 <c>docs/form-field-alignment.md</c> 的兼容性清单。</item>
    /// <item>浮标“有值上浮”不依赖任何需要探测的特性：既支持原生 <c>input</c>/<c>textarea</c> 的
    /// <c>:not(:placeholder-shown)</c>，也支持使用方通过 <see cref="HasValue"/> 显式声明
    /// （对任意自定义控件、任意浏览器都可靠）。</item>
    /// </list>
    /// </para>
    /// <example>
    /// <code>
    /// &lt;ElFormFieldGrow Text="电子邮箱" Required="true" HasValue="@(!string.IsNullOrEmpty(email))"&gt;
    ///     &lt;ElInput @bind-Value="@email" placeholder=" " /&gt;
    /// &lt;/ElFormFieldGrow&gt;
    /// </code>
    /// </example>
    /// </summary>
    public partial class ElFormFieldGrow : ElementComponentBase
    {
        private readonly ElFormFieldGrowContext fieldContext;
        private bool childDisabled;

        /// <summary>初始化级联上下文，把子组件的禁用回报接到本组件。</summary>
        public ElFormFieldGrow()
        {
            fieldContext = new ElFormFieldGrowContext { DisabledChanged = OnChildDisabledChanged };
        }

        // ------------------------------------------------------------------ 内容

        /// <summary>被包裹的输入控件（<c>ElInput</c>/<c>ElSelect</c>/原生 <c>input</c> 等）。
        /// 直接渲染在内容区内，因此原生控件可与标签构成兄弟关系，从而支持纯 CSS 的有值检测。</summary>
        [Parameter] public RenderFragment ChildContent { get; set; }

        /// <summary>前置内容：通常放图标或货币符号等前缀，位于控件之前、边框以内。</summary>
        [Parameter] public RenderFragment Start { get; set; }

        /// <summary>后置内容：通常放清除按钮、可见性切换或单位后缀，位于控件之后、边框以内。</summary>
        [Parameter] public RenderFragment End { get; set; }

        /// <summary>辅助内容（渲染在字段下方）。非空时优先于 <see cref="HelperText"/> 与 <see cref="ErrorText"/>。</summary>
        [Parameter] public RenderFragment Helper { get; set; }

        /// <summary>标签自定义模板；非空时替代 <see cref="Text"/> 的纯文本渲染。</summary>
        [Parameter] public RenderFragment TextTemplate { get; set; }

        // ------------------------------------------------------------------ 标签

        /// <summary>标签文本（与参考源的 <c>Text</c> 对齐）。</summary>
        [Parameter] public string Text { get; set; }

        /// <summary>标签文本别名（Element Plus 惯用名）。当 <see cref="Text"/> 为空时生效。</summary>
        [Parameter] public string Label { get; set; }

        /// <summary>关联的控件 id，渲染为标签的 <c>for</c> 属性；为空时不输出 <c>for</c>。</summary>
        [Parameter] public string Component { get; set; }

        /// <summary>是否为必填项（标签后追加红色星号）。</summary>
        [Parameter] public bool Required { get; set; }

        /// <summary>标签是否允许浮动。默认 <c>true</c>。
        /// 设为 <c>false</c> 时标签固定在顶部（与参考源 <c>RadzenFormField</c> 的语义一致）。</summary>
        [Parameter] public bool AllowFloatingLabel { get; set; } = true;

        /// <summary>标签浮动行为的精细控制，默认 <see cref="ElFormFieldFloat.Auto"/>。</summary>
        [Parameter] public ElFormFieldFloat Float { get; set; } = ElFormFieldFloat.Auto;

        /// <summary>显式声明内容“已有值”，使标签立即可靠上浮。
        /// 这是**不依赖任何 CSS 新特性、对任意自定义控件都成立**的兼容性后门：
        /// 原生控件可用 <c>placeholder=" "</c> + <c>:not(:placeholder-shown)</c> 自动检测，
        /// 但包装类控件（如 <c>ElInput</c> 渲染出的 <c>div.el-input &gt; input</c>）内部 input 与标签不是兄弟节点，
        /// 纯 CSS 无法探测，此时请使用本参数。</summary>
        [Parameter] public bool HasValue { get; set; }

        // ------------------------------------------------------------------ 外观

        /// <summary>外观变体，默认 <see cref="ElFormFieldVariant.Outlined"/>。</summary>
        [Parameter] public ElFormFieldVariant Variant { get; set; } = ElFormFieldVariant.Outlined;

        /// <summary>尺寸，默认 <see cref="ElFormFieldSize.Default"/>。</summary>
        [Parameter] public ElFormFieldSize Size { get; set; } = ElFormFieldSize.Default;

        /// <summary>是否行内布局。默认 <c>false</c>（块级、占满可用宽度）。</summary>
        [Parameter] public bool Inline { get; set; }

        /// <summary>是否禁用（灰底灰字）。子组件也可通过级联上下文回报禁用。</summary>
        [Parameter] public bool Disabled { get; set; }

        /// <summary>是否只读（外观不变，语义标记）。</summary>
        [Parameter] public bool ReadOnly { get; set; }

        /// <summary>是否处于校验失败态（红色边框、红色标签、错误提示）。</summary>
        [Parameter] public bool Invalid { get; set; }

        /// <summary>字段宽度，支持纯数字（自动补 <c>px</c>）或任意 CSS 长度。</summary>
        [Parameter] public string Width { get; set; }

        /// <summary>是否保留内层控件自身的边框/底色。
        /// 默认 <c>false</c>：字段自带边框，内层 <c>ElInput</c>/<c>ElTextarea</c> 会被“去壳”以免出现双边框；
        /// 设为 <c>true</c> 则原样保留内层控件外观。</summary>
        [Parameter] public bool KeepControlChrome { get; set; }

        // ------------------------------------------------------------------ 辅助文本

        /// <summary>辅助说明文本（仅在 <see cref="Helper"/> 为空时渲染）。</summary>
        [Parameter] public string HelperText { get; set; }

        /// <summary>错误提示文本（仅当 <see cref="Invalid"/> 为 <c>true</c> 且 <see cref="Helper"/> 为空时渲染）。</summary>
        [Parameter] public string ErrorText { get; set; }

        /// <summary>根元素 id（可选）。设置后辅助文本会获得 <c>{id}-helper</c> 便于外部引用。</summary>
        [Parameter] public string Id { get; set; }

        // ------------------------------------------------------------------ 公开状态（便于测试与子组件读取）

        /// <summary>级联给子组件的上下文。</summary>
        public ElFormFieldGrowContext Context => fieldContext;

        /// <summary>子组件回报的禁用状态（与 <see cref="Disabled"/> 取或）。</summary>
        public bool ChildDisabled => childDisabled;

        /// <summary>实际生效的禁用状态。</summary>
        public bool IsDisabled => Disabled || childDisabled;

        /// <summary>实际生效的标签文本（<see cref="Text"/> 优先，其次 <see cref="Label"/>）。</summary>
        public string EffectiveText => !string.IsNullOrWhiteSpace(Text) ? Text : Label;

        /// <summary>实际生效的浮动行为：参数归一化后的结果（决定渲染哪个互斥修饰类）。</summary>
        public ElFormFieldFloat EffectiveFloat
        {
            get
            {
                if (Float == ElFormFieldFloat.Always)
                {
                    return ElFormFieldFloat.Always;
                }

                if (Float == ElFormFieldFloat.Never)
                {
                    return ElFormFieldFloat.Never;
                }

                // 对齐参考源语义：不允许浮动 => 标签固定在顶部。
                if (!AllowFloatingLabel)
                {
                    return ElFormFieldFloat.Always;
                }

                return HasValue ? ElFormFieldFloat.Always : ElFormFieldFloat.Auto;
            }
        }

        /// <summary>标签的 <c>for</c> 值；为空表示不输出该属性。</summary>
        public string LabelFor => string.IsNullOrWhiteSpace(Component) ? null : Component.Trim();

        /// <summary>是否存在辅助区内容（片段、说明文本，或校验失败时的错误文本）。</summary>
        public bool HasHelperContent => Helper != null
            || !string.IsNullOrWhiteSpace(HelperText)
            || ShowError;

        /// <summary>是否应显示错误文本（校验失败且提供了错误文本）。</summary>
        public bool ShowError => Invalid && !string.IsNullOrWhiteSpace(ErrorText);

        /// <summary>辅助区实际文本（错误态取 <see cref="ErrorText"/>，否则取 <see cref="HelperText"/>）。</summary>
        public string EffectiveHelperText => ShowError ? ErrorText : HelperText;

        /// <summary>辅助元素 id（仅当设置了根 <see cref="Id"/> 时输出）。</summary>
        public string HelperId => string.IsNullOrWhiteSpace(Id) ? null : Id.Trim() + "-helper";

        // ------------------------------------------------------------------ 构建

        /// <summary>构建根元素 class（含变体、尺寸、浮动模式、状态修饰类与自定义 <see cref="ElementComponentBase.Cls"/>）。</summary>
        /// <returns>以空格分隔的类名。</returns>
        public string BuildCssClass()
        {
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-form-field")
                .Add("el-form-field--" + VariantToken)
                .Add("el-form-field--size-" + SizeToken)
                .Add("el-form-field--float-" + FloatToken)
                .AddIf(IsDisabled, "el-form-field--disabled")
                .AddIf(ReadOnly, "el-form-field--readonly")
                .AddIf(Invalid, "el-form-field--invalid")
                .AddIf(Required, "el-form-field--required")
                .AddIf(Inline, "el-form-field--inline")
                .AddIf(KeepControlChrome, "el-form-field--keep-control")
                .AddIf(Start != null, "el-form-field--has-start")
                .AddIf(End != null, "el-form-field--has-end")
                .AddIf(!string.IsNullOrWhiteSpace(Cls), Cls)
                .ToString();
        }

        /// <summary>构建根元素行内样式（合并 <see cref="Width"/> 与自定义 <see cref="ElementComponentBase.Style"/>）。</summary>
        /// <returns>以分号分隔的样式声明。</returns>
        public string BuildStyle()
        {
            return HtmlPropertyBuilder.CreateCssStyleBuilder()
                .AddIf(!string.IsNullOrWhiteSpace(Width), "width:" + NormalizeLength(Width))
                .Add(Style)
                .ToString();
        }

        /// <summary>辅助元素 class。</summary>
        /// <returns>类名。</returns>
        public string BuildHelperClass() => ShowError
            ? "el-form-field__helper el-form-field__helper--error"
            : "el-form-field__helper";

        /// <summary>把纯数字长度规范化为 <c>px</c>；其余原样返回（支持 <c>50%</c>、<c>20rem</c> 等）。</summary>
        /// <param name="length">长度字符串。</param>
        /// <returns>规范化后的 CSS 长度。</returns>
        public static string NormalizeLength(string length)
        {
            if (string.IsNullOrWhiteSpace(length))
            {
                return string.Empty;
            }

            var value = length.Trim();
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
                ? value + "px"
                : value;
        }

        // ------------------------------------------------------------------ 内部

        private string VariantToken
        {
            get
            {
                switch (Variant)
                {
                    case ElFormFieldVariant.Filled:
                        return "filled";
                    case ElFormFieldVariant.Flat:
                        return "flat";
                    case ElFormFieldVariant.Text:
                        return "text";
                    default:
                        return "outlined";
                }
            }
        }

        private string SizeToken
        {
            get
            {
                switch (Size)
                {
                    case ElFormFieldSize.Small:
                        return "small";
                    case ElFormFieldSize.Large:
                        return "large";
                    default:
                        return "default";
                }
            }
        }

        private string FloatToken
        {
            get
            {
                switch (EffectiveFloat)
                {
                    case ElFormFieldFloat.Always:
                        return "always";
                    case ElFormFieldFloat.Never:
                        return "never";
                    default:
                        return "auto";
                }
            }
        }

        /// <summary>把告警/只读状态同步进级联上下文（子组件读取用）。</summary>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            fieldContext.AllowFloatingLabel = AllowFloatingLabel;
            fieldContext.Disabled = IsDisabled;
        }

        private void OnChildDisabledChanged(bool value)
        {
            if (childDisabled == value)
            {
                return;
            }

            childDisabled = value;
            fieldContext.Disabled = IsDisabled;
            _ = InvokeAsync(StateHasChanged);
        }
    }
}
