

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public class DropDownOption : PopupOption
    {
        public Action Refresh { get; set; }
        internal Action RequestRender { get; set; }
        public Func<Task> OnClosed { get; set; }
        public object Select { get; set; }
        public RenderFragment OptionContent { get; set; }

        /// <summary>
        /// 下拉面板顶部固定区域内容（对齐 EP 2.5.2+ 的 <c>header</c> 插槽）。
        /// 官方常用它放下拉内搜索框 / 全选工具条；渲染为 <c>.el-select-dropdown__header</c>，
        /// 位于滚动区之上，不随选项列表滚动。
        /// </summary>
        public RenderFragment HeaderContent { get; set; }

        /// <summary>
        /// 下拉面板底部固定区域内容（对齐 EP 2.5.2+ 的 <c>footer</c> 插槽），
        /// 渲染为 <c>.el-select-dropdown__footer</c>。
        /// </summary>
        public RenderFragment FooterContent { get; set; }

        public bool IsTree { get; set; }
        public string PopperClass { get; set; }
        public string PopperStyle { get; set; }
        public int MaxHeight { get; set; }
        public bool FitInputWidth { get; set; }

        /// <summary>
        /// 菜单容器（<c>ul.el-dropdown-menu</c>）的 <c>role</c>。为 null 时不输出该属性
        /// ⇒ 既有下拉的 DOM 一个字节不变；目前只有 ElSplitButton 传入 "menu"。
        /// </summary>
        public string AriaRole { get; set; }

        /// <summary>菜单容器的 <c>aria-label</c>；为 null 时不输出。</summary>
        public string AriaLabel { get; set; }

        /// <summary>
        /// 菜单容器的 keydown 处理器（事件从获得焦点的菜单项冒泡上来）。
        /// 只有弹出菜单型组件（ElSplitButton）会赋值；其余为默认空回调。
        /// </summary>
        public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

        /// <summary>
        /// 菜单 keydown 是否 <c>preventDefault</c>。ElSplitButton 在打开期间恒为 true：
        /// 方向键/Home/End 不滚页，Tab 由组件自己关菜单并把焦点还给触发按钮。
        /// </summary>
        public bool PreventKeyDown { get; set; }

        /// <summary>菜单 keydown 是否 <c>stopPropagation</c>（打开期间不把按键漏给页面级快捷键）。</summary>
        public bool StopKeyDownPropagation { get; set; }

        internal bool AutoWidth { get; set; } = true;
        internal string DropDownId { get; set; }
        internal string Placement { get; set; } = "bottom-start";
        internal string TransformOrigin { get; set; } = "center top 0px";

        /// <summary>
        /// 是否渲染「点击外部关闭」用的全屏透明遮罩（.v-modal）。
        /// hover 触发的菜单必须为 false：遮罩铺满整屏且层级高于触发器，一旦它出现在指针下方，
        /// 浏览器重算命中后会给触发器补发 mouseout，菜单随即被 HideTimeout 关闭
        /// （表现为「弹出后马上消失 / 反复闪烁」）。
        /// </summary>
        internal bool ShowShadow { get; set; } = true;
        internal float MinWidth { get; set; }
        internal float WidthPx { get; set; }
        internal ElementReference ScrollElement { get; set; }

        /// <summary>
        /// 浮层宽度（像素）。大于 0 时由 <see cref="ElPopup"/> 写成 <c>width:Npx;</c> 内联样式，
        /// 否则退化为自动宽度 + <c>min-width</c>（对齐宿主输入框宽度）。
        /// </summary>
        public int Width
        {
            get => (int)Math.Round(WidthPx);
            set => WidthPx = value;
        }
    }
}
