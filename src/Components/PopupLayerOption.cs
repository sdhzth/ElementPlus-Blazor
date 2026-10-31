using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace ElementPlus
{
    /// <summary>
    /// 通用弹层（PopupLayer / DropDownTree）的配置模型。
    /// 由宿主在调用弹层 API 时创建，注册到 <see cref="PopupService"/> 后由 <see cref="ElPopup"/> 渲染。
    /// </summary>
    public class PopupLayerOption
    {
        public int ShadowZIndex { get; internal set; }
        public ElPopup Instance { get; internal set; }
        public bool IsNew { get; internal set; }
        public int ZIndex { get; internal set; }

        public PointF Position { get; set; }
        public RenderFragment Content { get; internal set; }
        public bool IsShow { get; set; }
        public AnimationStatus ShowStatus { get; set; }
        public ElementReference Element { get; set; }
        public int Width { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public ElementReference ShadowElement { get; set; }
    }
}
