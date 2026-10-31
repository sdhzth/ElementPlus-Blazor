
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public class PopupOption
    {
        /// <summary>
        /// 关联到的 ElPopup 组件实例
        /// </summary>
        public ElPopup Instance { get; internal set; }

        /// <summary>
        /// 弹出层元素本身
        /// </summary>
        internal ElementReference Element { get; set; }

        /// <summary>
        /// 遮罩层元素
        /// </summary>
        internal ElementReference ShadowElement { get; set; }

        /// <summary>
        /// 要将弹出层显示到哪个元素下方
        /// </summary>
        internal ElementReference Target { get; set; }

        /// <summary>
        /// 隐藏动画状态
        /// </summary>
        internal AnimationStatus HideStatus { get; set; }

        /// <summary>
        /// 显示动画状态
        /// </summary>
        internal AnimationStatus ShowStatus { get; set; }

        internal bool IsShow { get; set; }

        internal float Top { get; set; }
        internal float Left { get; set; }
        public bool IsNew { get; internal set; }
        internal int ZIndex { get; set; }
        internal int ShadowZIndex { get; set; }

        /// <summary>Whether the popper stays open when clicking outside (EP "persistent" semantics). When true, the outside-click shadow overlay does not close it.</summary>
        public bool Persistent { get; set; }

        /// <summary>Mount target selector (EP "append-to", defaults to body). This Blazor port always renders poppers via the ElPopup host at root level, so the effective mount is always body; the value is recorded here and exposed via data-append-to for API parity / debugging.</summary>
        public string AppendTo { get; set; }
    }
}
