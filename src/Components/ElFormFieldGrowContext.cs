using System;

namespace ElementPlus
{
    /// <summary>
    /// 表单字段上下文。内部控件（输入框、下拉、日期选择器等）可通过级联参数获取本接口，
    /// 从而向父级 <c>ElFormFieldGrow</c> 回报自身的禁用状态。
    /// <para>
    /// 该接口与 Radzen 的 <c>IFormFieldContext</c> 语义对齐（<c>DisabledChanged</c> +
    /// <c>AllowFloatingLabel</c>），但为独立实现，不引用 Radzen 的任何类型。
    /// </para>
    /// </summary>
    public interface IElFormFieldContext
    {
        /// <summary>
        /// 通知表单字段：内部控件的禁用状态已变化。传 <c>true</c> 表示内部控件被禁用，
        /// 外层随即进入禁用外观（灰底、灰字、禁用边框）。
        /// </summary>
        Action<bool> DisabledChanged { get; set; }

        /// <summary>
        /// 标签是否允许浮动。控件可据此决定是否给自身添加占位符等配合项。
        /// </summary>
        bool AllowFloatingLabel { get; set; }

        /// <summary>
        /// 外层是否已处于禁用状态（只读，供内部控件判断是否需要同步禁用自身）。
        /// </summary>
        bool Disabled { get; }
    }

    /// <summary>
    /// <see cref="IElFormFieldContext"/> 的默认实现。
    /// </summary>
    public class ElFormFieldGrowContext : IElFormFieldContext
    {
        /// <inheritdoc />
        public Action<bool> DisabledChanged { get; set; } = _ => { };

        /// <inheritdoc />
        public bool AllowFloatingLabel { get; set; } = true;

        /// <inheritdoc />
        public bool Disabled { get; internal set; }
    }
}
