using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public class MessageInfo
    {
        public override string ToString() => Message;

        /// <summary>消息内容</summary>
        public string Message { get; set; }

        /// <summary>显示时长（ms），0 = 不自动关闭</summary>
        public int Duration { get; set; } = 3000;

        /// <summary>消息类型</summary>
        public MessageType Type { get; set; } = MessageType.Info;

        /// <summary>是否显示关闭按钮</summary>
        public bool ShowClose { get; set; }

        /// <summary>是否朴素（无底色）</summary>
        public bool Plain { get; set; }

        /// <summary>是否分组（同类型同内容合并）</summary>
        public bool Grouping { get; set; }

        /// <summary>分组时合并的次数（只读/内部维护）</summary>
        public int RepeatCount { get; set; } = 1;

        /// <summary>自定义 class</summary>
        public string CustomClass { get; set; }

        /// <summary>自定义 style</summary>
        public string CustomStyle { get; set; }

        /// <summary>居中显示</summary>
        public bool Center { get; set; }

        /// <summary>自定义图标 class（覆盖 type 默认图标）</summary>
        public string Icon { get; set; }

        /// <summary>
        /// 距窗口顶部的偏移量（对应官方 <c>offset</c>，默认 <c>16</c>）。
        /// 决定整摞提示的基准位置：首条停在该像素处，后续提示依次向下堆叠。
        /// </summary>
        public int Offset { get; set; } = 16;

        /// <summary>把 Message 当作 HTML 渲染</summary>
        public bool DangerouslyUseHTMLString { get; set; }

        /// <summary>关闭时回调</summary>
        public Func<Task> OnClose { get; set; }

        /// <summary>关闭时回调（同步版本）</summary>
        public Action OnCloseSync { get; set; }

        /// <summary>
        /// 渲染本条提示的过渡组件引用（由 ElPopup 通过 @ref 回填），
        /// 鼠标悬停暂停 / 移开恢复自动关闭计时靠它调 <c>PauseTiming</c>/<c>ResumeTiming</c>。
        /// </summary>
        internal ElTransition TransitionRef { get; set; }

        internal ElementReference Element { get; set; }
        internal bool IsNew { get; set; }
        internal int Index { get; set; }
        internal int BeginTop { get; set; }
        internal int EndTop { get; set; }
        internal int ZIndex { get; set; }
        internal Guid InternalId { get; set; } = Guid.NewGuid();
    }
}