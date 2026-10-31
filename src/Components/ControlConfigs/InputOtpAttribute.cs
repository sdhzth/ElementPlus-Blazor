namespace ElementPlus.ControlConfigs
{
    public class InputOtpAttribute : BaseAttribute
    {
        public int Length { get; set; } = 6;

        public bool IsDisabled { get; set; }

        public bool Readonly { get; set; }

        public string Inputmode { get; set; } = "numeric";

        /// <summary>是否启用掩码。启用时把每段输入渲染为 <see cref="MaskChar"/>。</summary>
        public bool Mask { get; set; }

        /// <summary>
        /// 掩码字符，留空回落为 <see cref="ElInputOtp.DefaultMask"/>（"•"）。
        /// <para>注意：这里刻意与组件 <see cref="ElInputOtp.Mask"/>（string）区分——配置项是开关，
        /// 组件参数是字符，由 <c>FormControlRender</c> 做桥接，避免同映射名下的类型转换异常。</para>
        /// </summary>
        public string MaskChar { get; set; }
    }
}
