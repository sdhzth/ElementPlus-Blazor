using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    /// <summary>
    /// ElLightGrow 是参照 BootstrapBlazor <c>Light</c> 指示灯组件的概念、按 Element Plus 视觉风格自渲染的状态指示灯组件。
    /// 零第三方 UI 依赖，所有渲染与闪烁动画均为本仓库内部实现。
    /// 常用于状态标识（在线/离线、运行/停止、正常/告警），可与文本混排，亦可作为独立圆点使用。
    /// </summary>
    public partial class ElLightGrow : ElementComponentBase
    {
        /// <summary>指示灯颜色，对应库内统一的 <see cref="MessageType"/> 五态（Info/Success/Warning/Error/Primary）。默认 Success（绿）。</summary>
        [Parameter]
        public MessageType Color { get; set; } = MessageType.Success;

        /// <summary>是否闪烁（呼吸动画），默认 <c>false</c> 常亮。</summary>
        [Parameter]
        public bool IsFlash { get; set; }

        /// <summary>是否平面样式（无外发光、纯色实心圆点），默认 <c>false</c> 带发光质感。</summary>
        [Parameter]
        public bool IsFlat { get; set; }

        /// <summary>指示灯直径（像素），默认 12。小于 8 时回退到样式默认值。</summary>
        [Parameter]
        public int Size { get; set; } = 12;

        /// <summary>指示灯右侧显示的文本标签（可选）。</summary>
        [Parameter]
        public string Text { get; set; }

        /// <summary>指示灯右侧的自定义内容（可选），与 <see cref="Text"/> 并存时文本在前。</summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}
