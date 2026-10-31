using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 分裂按钮：左侧执行主操作、右侧展开一组备选操作。
    /// 行为对齐 RadzenSplitButton（主按钮 Click(null)、菜单项 Click(item)、AlwaysOpenPopup、
    /// 方向键/Home/End/Enter/Space/Escape/Type-ahead），渲染对齐 Element Plus 的
    /// el-button-group + el-dropdown 组合。菜单由 ElPopup 宿主统一渲染。
    /// </summary>
    public partial class ElSplitButton : ElementComponentBase
    {
        internal ElementReference Target;
        private ElButton caretButton;
        private DropDownOption dropDownOption;
        private readonly List<ElSplitButtonItem> items = new();
        private DotNetObjectReference<ElSplitButton> outsideRef;
        private bool outsideBound;
        private bool navLockBound;
        private string autoId;
        private int focusedIndex = -1;
        private PendingFocus pendingFocus = PendingFocus.None;
        private int pendingIndex = -1;

        private enum PendingFocus
        {
            None,
            First,
            Last,
            Index
        }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>自定义主按钮内容；给了它就不渲染 Text/BusyText（对齐 Radzen ButtonContent 优先）。</summary>
        [Parameter]
        public RenderFragment ButtonContent { get; set; }

        [Parameter]
        public string Text { get; set; } = string.Empty;

        /// <summary>Element 图标类名；可省略 el-icon- 前缀</summary>
        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public string IconColor { get; set; }

        [Parameter]
        public string Image { get; set; }

        [Parameter]
        public string ImageAlternateText { get; set; }

        [Parameter]
        public ButtonType Type { get; set; } = ButtonType.Default;

        [Parameter]
        public ButtonSize Size { get; set; } = ButtonSize.Default;

        [Parameter]
        public bool Plain { get; set; }

        [Parameter]
        public bool Round { get; set; }

        [Parameter]
        public bool Link { get; set; }

        [Parameter]
        public bool Bg { get; set; }

        [Parameter]
        public bool Dashed { get; set; }

        [Parameter]
        public bool Dark { get; set; }

        [Parameter]
        public string Color { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>忙碌态：主按钮进入 Loading 且两颗按钮都不可点（对齐 Radzen IsDisabled = Disabled || IsBusy）</summary>
        [Parameter]
        public bool IsBusy { get; set; }

        [Parameter]
        public string BusyText { get; set; } = string.Empty;

        /// <summary>true 时点主按钮只切换菜单，不触发 Click（对齐 Radzen AlwaysOpenPopup）</summary>
        [Parameter]
        public bool AlwaysOpenPopup { get; set; }

        [Parameter]
        public string OpenAriaLabel { get; set; } = "dropdown trigger";

        [Parameter]
        public string DropDownIcon { get; set; } = "el-icon-arrow-down";

        /// <summary>两颗按钮共用的 tabindex（Element 命名；对应 Radzen 的 TabIndex）</summary>
        [Parameter]
        public int Tabindex { get; set; }

        [Parameter]
        public string NativeType { get; set; } = "button";

        [Parameter]
        public EventCallback<ElSplitButtonItem> Click { get; set; }

        [Parameter]
        public string ButtonAriaLabel { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string PopperClass { get; set; }

        [Parameter]
        public string PopperStyle { get; set; }

        [Parameter]
        public bool Visible { get; set; } = true;

        [Inject]
        private PopupService PopupService { get; set; }

        internal bool IsDisabled => Disabled || IsBusy;

        /// <summary>
        /// 菜单是否处于展开态。IsShow 在关闭动画一开始就被置 false，
        /// 而 <c>dropDownOption</c> 要等 210ms 关闭尾巴跑完才清空——
        /// toggle 判据若用后者，关闭窗口期内的点击会被当成「关闭中」吞掉。
        /// </summary>
        internal bool IsOpen => dropDownOption != null && dropDownOption.IsShow;

        internal int FocusedIndex => focusedIndex;

        internal IReadOnlyList<ElSplitButtonItem> Items => items;

        internal string EffectiveId => string.IsNullOrWhiteSpace(Id)
            ? (autoId ??= $"el-split-button-{Guid.NewGuid():N}".Substring(0, 24))
            : Id;

        internal string MenuId => $"{EffectiveId}-menu";

        private string MainId => $"{EffectiveId}-main";

        private string CaretId => $"{EffectiveId}-toggle";

        internal string RootClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-dropdown", "el-split-button", Cls)
            .AddIf(IsDisabled, "is-disabled")
            .ToString();

        private string MainText => ButtonContent != null
            ? null
            : (IsBusy && !string.IsNullOrWhiteSpace(BusyText) ? BusyText : Text);

        private string MainAriaLabel => ButtonAriaLabel ?? (string.IsNullOrWhiteSpace(Text) ? null : Text);

        private bool HasCustomIcon => !string.IsNullOrWhiteSpace(Image)
            || (!string.IsNullOrWhiteSpace(Icon) && !string.IsNullOrWhiteSpace(IconColor));

        private string IconValue => HasCustomIcon ? null : NormalizeIcon(Icon);

        private RenderFragment IconContentValue => HasCustomIcon ? BuildIconContent : null;

        private string DropDownIconValue => NormalizeIcon(DropDownIcon);

        private RenderFragment BuildIconContent => builder =>
        {
            var icon = NormalizeIcon(Icon);
            if (!string.IsNullOrWhiteSpace(icon))
            {
                builder.OpenElement(0, "i");
                builder.AddAttribute(1, "class", $"{icon} el-icon--left");
                builder.AddAttribute(2, "aria-hidden", "true");
                if (!string.IsNullOrWhiteSpace(IconColor))
                {
                    builder.AddAttribute(3, "style", $"color:{IconColor}");
                }
                if (!string.IsNullOrWhiteSpace(Image))
                {
                    builder.AddAttribute(4, "style", $"color:{IconColor};margin-right:5px");
                }
                builder.CloseElement();
            }

            if (!string.IsNullOrWhiteSpace(Image))
            {
                builder.OpenElement(5, "img");
                builder.AddAttribute(6, "class", "el-icon--left el-split-button__image");
                builder.AddAttribute(7, "src", Image);
                builder.AddAttribute(8, "alt", ImageAlternateText ?? Text ?? string.Empty);
                builder.CloseElement();
            }
        };

        // 默认类型必须显式带 el-button--default：EP 用这条类给分裂按钮的分隔线
        // 上灰（rgba(220,223,230,.5)），白底按钮上的白线等于没有分隔。
        private string CaretCls => Type == ButtonType.Default
            ? "el-dropdown__caret-button el-button--default"
            : "el-dropdown__caret-button";

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _ = EffectiveId;
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            if ((!Visible || IsDisabled) && dropDownOption != null)
            {
                ForceClose();
            }
        }

        internal void AddItem(ElSplitButtonItem item)
        {
            if (item != null && items.IndexOf(item) < 0)
            {
                items.Add(item);
            }
        }

        internal void RemoveItem(ElSplitButtonItem item)
        {
            if (item == null)
            {
                return;
            }

            var index = items.IndexOf(item);
            if (index < 0)
            {
                return;
            }

            items.RemoveAt(index);
            if (focusedIndex == index)
            {
                focusedIndex = -1;
            }
            else if (focusedIndex > index)
            {
                focusedIndex--;
            }
        }

        internal string ItemId(ElSplitButtonItem item)
        {
            var index = items.IndexOf(item);
            return index < 0 ? null : $"{EffectiveId}-item-{index}";
        }

        /// <summary>菜单项挂载后调用：若它正是本次打开要落焦的那一项，则返回 true。</summary>
        internal bool TryConsumeFocus(ElSplitButtonItem item)
        {
            if (dropDownOption == null || pendingFocus == PendingFocus.None)
            {
                return false;
            }

            var index = items.IndexOf(item);
            if (index < 0)
            {
                return false;
            }

            var target = pendingFocus switch
            {
                PendingFocus.First => FindEnabledIndex(0, 1),
                PendingFocus.Last => FindEnabledIndex(items.Count - 1, -1),
                PendingFocus.Index => pendingIndex,
                _ => -1
            };

            if (target < 0 || target != index)
            {
                return false;
            }

            pendingFocus = PendingFocus.None;
            focusedIndex = index;
            return true;
        }

        private int FindEnabledIndex(int start, int step)
        {
            for (var i = start; i >= 0 && i < items.Count; i += step)
            {
                if (!items[i].Disabled)
                {
                    return i;
                }
            }

            return -1;
        }

        private async Task MoveFocusAsync(int index)
        {
            if (index < 0 || index >= items.Count)
            {
                return;
            }

            focusedIndex = index;
            await items[index].FocusAsync();
        }

        private async Task TypeAheadAsync(string character)
        {
            if (items.Count == 0)
            {
                return;
            }

            var start = focusedIndex < 0 ? 0 : (focusedIndex + 1) % items.Count;
            for (var i = 0; i < items.Count; i++)
            {
                var index = (start + i) % items.Count;
                var item = items[index];
                if (!item.Disabled && !string.IsNullOrEmpty(item.Text)
                    && item.Text.StartsWith(character, StringComparison.OrdinalIgnoreCase))
                {
                    await MoveFocusAsync(index);
                    return;
                }
            }
        }

        internal async Task ShowDropDownAsync(bool focusLast = false)
        {
            if (IsDisabled || dropDownOption != null)
            {
                return;
            }

            pendingFocus = focusLast ? PendingFocus.Last : PendingFocus.First;
            pendingIndex = -1;
            focusedIndex = -1;

            var option = new DropDownOption
            {
                Target = Target,
                OptionContent = ChildContent,
                Select = this,
                Refresh = () => InvokeAsync(StateHasChanged),
                Placement = "bottom-start",
                IsShow = true,
                // 点外面收起走 document 捕获阶段监听（elementPickerOutsideAttach）：
                // 遮罩会盖住主按钮，菜单开着时点主操作变成「只收起、不执行」。
                ShowShadow = false,
                AriaRole = "menu",
                AriaLabel = OpenAriaLabel,
                DropDownId = MenuId,
                OnKeyDown = EventCallback.Factory.Create<KeyboardEventArgs>(this, OnMenuKeyDownAsync),
                PreventKeyDown = true,
                StopKeyDownPropagation = true,
                PopperClass = PopperClass,
                PopperStyle = PopperStyle
            };

            option.OnClosed = () => OnDropDownClosedAsync(option);
            dropDownOption = option;
            PopupService.DropDownMenuOptionsCore.Add(option);
            await InvokeAsync(StateHasChanged);
        }

        internal async Task CloseAsync()
        {
            var option = dropDownOption;
            if (option == null)
            {
                return;
            }

            if (option.Instance != null)
            {
                await option.Instance.CloseDropDownAsync(option);
            }
            else
            {
                FinishClose();
            }
        }

        /// <summary>以编程方式收起菜单（对齐 Radzen 的 Close()）。</summary>
        public void Close()
        {
            _ = CloseAsync();
        }

        private Task OnDropDownClosedAsync(DropDownOption option)
        {
            // 按 option 识别：旧 option 的关闭尾巴若在重新打开之后才落地，
            // 不能把新 option 的引用一并清掉
            if (dropDownOption == option)
            {
                FinishClose();
            }

            return InvokeAsync(StateHasChanged);
        }

        private void FinishClose()
        {
            dropDownOption = null;
            focusedIndex = -1;
            pendingFocus = PendingFocus.None;
            pendingIndex = -1;
        }

        private void ForceClose()
        {
            var option = dropDownOption;
            if (option != null)
            {
                option.IsShow = false;
                PopupService.DropDownMenuOptionsCore.Remove(option);
            }

            FinishClose();
        }

        /// <summary>
        /// 关闭动画尾巴（210ms Task.Delay 链）尚未落地时，<c>dropDownOption</c> 仍挂着
        /// 一个 IsShow=false 的旧 option；直接开新菜单会和它撞出重复 id 的弹层，先摘除。
        /// </summary>
        private void DiscardClosingOption()
        {
            if (dropDownOption != null && !dropDownOption.IsShow)
            {
                ForceClose();
            }
        }

        private async Task OnMainClickAsync(MouseEventArgs e)
        {
            if (IsDisabled)
            {
                return;
            }

            if (AlwaysOpenPopup)
            {
                if (IsOpen)
                {
                    await CloseAsync();
                }
                else
                {
                    DiscardClosingOption();
                    await ShowDropDownAsync();
                }

                return;
            }

            if (IsOpen)
            {
                await CloseAsync();
            }
            else
            {
                DiscardClosingOption();
            }

            if (Click.HasDelegate)
            {
                await Click.InvokeAsync(null);
            }
        }

        private async Task OnCaretClickAsync(MouseEventArgs e)
        {
            if (IsDisabled)
            {
                return;
            }

            if (IsOpen)
            {
                await CloseAsync();
            }
            else
            {
                DiscardClosingOption();
                await ShowDropDownAsync();
            }
        }

        private async Task OnCaretKeyDownAsync(KeyboardEventArgs e)
        {
            if (IsDisabled)
            {
                return;
            }

            var key = string.IsNullOrEmpty(e.Code) ? e.Key : e.Code;

            if (!IsOpen && (key == "ArrowDown" || key == "ArrowUp"))
            {
                DiscardClosingOption();
                await ShowDropDownAsync(focusLast: key == "ArrowUp");
                return;
            }

            if (IsOpen && key == "Escape")
            {
                await CloseAndFocusCaretAsync();
            }
        }

        private async Task OnMenuKeyDownAsync(KeyboardEventArgs e)
        {
            if (dropDownOption == null)
            {
                return;
            }

            var key = string.IsNullOrEmpty(e.Code) ? e.Key : e.Code;

            switch (key)
            {
                case "Home":
                    await MoveFocusAsync(FindEnabledIndex(0, 1));
                    return;
                case "End":
                    await MoveFocusAsync(FindEnabledIndex(items.Count - 1, -1));
                    return;
                case "ArrowUp":
                    await MoveFocusAsync(FindEnabledIndex(focusedIndex - 1, -1));
                    return;
                case "ArrowDown":
                    await MoveFocusAsync(FindEnabledIndex(focusedIndex + 1, 1));
                    return;
                case "Enter":
                case "Space":
                case "NumpadEnter":
                    if (focusedIndex >= 0 && focusedIndex < items.Count)
                    {
                        await ActivateItemAsync(items[focusedIndex], focusCaret: true);
                    }

                    return;
                case "Escape":
                case "Tab":
                    await CloseAndFocusCaretAsync();
                    return;
            }

            if (!string.IsNullOrEmpty(e.Key) && e.Key.Length == 1
                && !e.AltKey && !e.CtrlKey && !e.MetaKey && !char.IsControl(e.Key[0]))
            {
                await TypeAheadAsync(e.Key);
            }
        }

        internal async Task ActivateItemAsync(ElSplitButtonItem item, bool focusCaret)
        {
            if (dropDownOption == null || item == null || item.Disabled || IsDisabled)
            {
                return;
            }

            var closeTask = CloseAsync();
            if (Click.HasDelegate)
            {
                await Click.InvokeAsync(item);
            }

            if (focusCaret)
            {
                await FocusCaretAsync();
            }

            await closeTask;
        }

        private async Task CloseAndFocusCaretAsync()
        {
            var closeTask = CloseAsync();
            await FocusCaretAsync();
            await closeTask;
        }

        private async Task FocusCaretAsync()
        {
            if (caretButton != null)
            {
                try
                {
                    await caretButton.FocusAsync();
                }
                catch (JSException)
                {
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (!navLockBound && caretButton != null
                && !caretButton.ButtonReference.Equals(default(ElementReference)))
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync(
                        "elementSplitButtonLockNavScroll", caretButton.ButtonReference);
                    navLockBound = true;
                }
                catch (JSException)
                {
                }
            }

            if (dropDownOption != null && !outsideBound)
            {
                try
                {
                    outsideRef ??= DotNetObjectReference.Create(this);
                    await JSRuntime.InvokeVoidAsync(
                        "elementPickerOutsideAttach",
                        Target,
                        $"#{MenuId}",
                        outsideRef,
                        nameof(CloseFromOutsideAsync));
                    outsideBound = true;
                }
                catch (JSException)
                {
                    // 预渲染 / 无 JS 环境：菜单仍可用，只是点外面不会自动收起
                }
            }
            else if (dropDownOption == null && outsideBound)
            {
                outsideBound = false;
                try
                {
                    await JSRuntime.InvokeVoidAsync("elementPickerOutsideDetach", Target);
                }
                catch (JSException)
                {
                }
            }

            if (dropDownOption != null && pendingFocus != PendingFocus.None && items.Count > 0)
            {
                var hasCandidate = pendingFocus == PendingFocus.Last
                    ? FindEnabledIndex(items.Count - 1, -1) >= 0
                    : FindEnabledIndex(0, 1) >= 0;
                if (!hasCandidate)
                {
                    pendingFocus = PendingFocus.None;
                }
            }
        }

        /// <summary>document 捕获阶段收到菜单/按钮之外的 mousedown：收起菜单（由 JS 回调）。</summary>
        [JSInvokable]
        public Task CloseFromOutsideAsync()
        {
            return CloseAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (dropDownOption != null)
                {
                    PopupService?.DropDownMenuOptionsCore.Remove(dropDownOption);
                    dropDownOption = null;
                }

                if (outsideBound)
                {
                    outsideBound = false;
                    try
                    {
                        _ = JSRuntime?.InvokeVoidAsync("elementPickerOutsideDetach", Target);
                    }
                    catch (JSException)
                    {
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }

                outsideRef?.Dispose();
                outsideRef = null;
            }

            base.Dispose(disposing);
        }

        private static string NormalizeIcon(string icon)
        {
            if (string.IsNullOrWhiteSpace(icon))
            {
                return null;
            }

            return icon.StartsWith("el-icon-", StringComparison.Ordinal) ? icon : $"el-icon-{icon}";
        }
    }
}
