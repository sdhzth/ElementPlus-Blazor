using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 浮层（下拉面板 / 弹层 / 菜单）的集中注册中心。
    /// 组件把浮层模型注册进来，由 <see cref="ElPopup"/> 统一渲染到 body 层。
    /// <para>所有 xxxOptions 集合对外公开为**只读快照**（<see cref="IReadOnlyList{T}"/>），
    /// 宿主与测试可以观测当前打开的浮层，但不能直接增删——增删仍由组件通过内部方法完成。</para>
    /// </summary>
    public class PopupService
    {
        internal ObservableCollection<DateTimePickerOption> DateTimePickerOptionsCore = new ObservableCollection<DateTimePickerOption>();
        internal ObservableCollection<DropDownOption> SelectDropDownOptionsCore = new ObservableCollection<DropDownOption>();
        internal ObservableCollection<DropDownOption> DropDownMenuOptionsCore = new ObservableCollection<DropDownOption>();
        internal ObservableCollection<SubMenuOption> SubMenuOptionsCore = new ObservableCollection<SubMenuOption>();
        internal ObservableCollection<PopupLayerOption> PopupLayerOptionsCore = new ObservableCollection<PopupLayerOption>();
        internal ObservableCollection<PopupLayerOption> DropDownTreeOptionsCore = new ObservableCollection<PopupLayerOption>();

        /// <summary>
        /// 本作用域内已经初始化过几个 <see cref="ElPopup"/> 宿主。<b>契约是 1</b>：
        /// 宿主在 <c>OnInitialized</c> 里会重建各类浮层集合（等于接管浮层队列的渲染权），
        /// 第二个宿主一出现，前一个宿主就再也收不到集合变化 —— 它已经渲染出来的浮层留在页面上关不掉，
        /// 而之后新开的浮层只会在后一个宿主那里出现（若后一个宿主是布局级宿主，离开那条路由时它被拆掉，
        /// 而所有权不会交还：Router 外的宿主不会再初始化、<c>ElPopup.Dispose</c> 也不退订，
        /// 于是变成"调用了却什么都不出现"）。演示站的 <c>LoginLayout</c> 踩过，取证见
        /// <c>docs/popup-host-isolation.md</c>。第二个宿主初始化时会被 <see cref="ElPopup"/> 打到控制台。
        /// </summary>
        internal int PopupHostCount { get; set; }

        /// <summary>当前已打开的日期时间选择面板（只读快照）。</summary>
        public IReadOnlyList<DateTimePickerOption> DateTimePickerOptions => DateTimePickerOptionsCore;

        /// <summary>当前已打开的下拉选择类面板（Select / Picker / Autocomplete 等，只读快照）。</summary>
        public IReadOnlyList<DropDownOption> SelectDropDownOptions => SelectDropDownOptionsCore;

        /// <summary>当前已打开的下拉菜单（Dropdown 菜单项面板，只读快照）。</summary>
        public IReadOnlyList<DropDownOption> DropDownMenuOptions => DropDownMenuOptionsCore;

        /// <summary>当前已打开的子菜单（只读快照）。</summary>
        public IReadOnlyList<SubMenuOption> SubMenuOptions => SubMenuOptionsCore;

        /// <summary>当前已打开的通用弹层（只读快照）。</summary>
        public IReadOnlyList<PopupLayerOption> PopupLayerOptions => PopupLayerOptionsCore;

        /// <summary>当前已打开的下拉树面板（只读快照）。</summary>
        public IReadOnlyList<PopupLayerOption> DropDownTreeOptions => DropDownTreeOptionsCore;
    }
}
