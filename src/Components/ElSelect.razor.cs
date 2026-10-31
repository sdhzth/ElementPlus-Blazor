




using ElementPlus.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElSelect<TValue> : ElementFieldComponentBase<TValue>, ISelectDropDownContext, IDisposable
    {
        protected internal bool isTree;
        private HtmlPropertyBuilder warpperClsBuilder;
        internal ElementReference elementSelect;
        /// <summary>内层输入框引用：焦点真源（对齐 EP），用于点击后把焦点从根节点转到 input</summary>
        private ElInput<string> innerInput;
        private Type valueType;
        private Type nullable;
        private InputSize effectiveSize = InputSize.Normal;
        private bool effectiveDisabled;
        internal bool EnableClearButton { get; set; }
        private int hoveredIndex = -1;

        /// <summary>
        /// 高亮项变化通知。弹层里的 <c>ElOption</c> 由 ElPopup 宿主渲染，而「是否高亮」不在
        /// ElOption 的参数里（它读的是本组件的 hoveredIndex）：参数没变时 Blazor 会直接跳过该
        /// 子组件的重渲染（ShouldRender 根本不会被询问），因此键盘上下键改出的高亮刷不到 DOM。
        /// 改由事件通知，让每个候选项自行重渲染。
        /// </summary>
        internal event Action HoverChanged;
        /// <summary>搜索关键字变化通知。弹层里 <c>ElOption</c> 由 ElPopup 宿主渲染，其入参（Value/Text/Disabled）在
        /// 关键字变化时并不会变，Blazor 会直接跳过它的重渲染，导致 <c>IsVisible</c>（display:none）刷不到 DOM、
        /// 输入关键字后选项不收缩。改由事件通知，让每个候选项自行重渲染（与 <see cref="HoverChanged"/> 同理）。</summary>
        internal event Action FilterChanged;
        private string _filterText;

        /// <summary>
        /// 当前搜索关键字（Filterable 时由触发器输入框写入）。
        /// 用属性包一层是为了给子类（ElTreeSelect）一个统一的挂钩点 —— 关键字不仅会在输入时变化，
        /// 还会在选中、清空、关闭下拉时被置空，这些位置分散在基类里，逐个改容易漏。
        /// </summary>
        protected string filterText
        {
            get => _filterText;
            set
            {
                if (string.Equals(_filterText, value, StringComparison.Ordinal))
                {
                    return;
                }

                _filterText = value;
                OnFilterTextChanged(value);
                FilterChanged?.Invoke();
            }
        }

        /// <summary>搜索关键字变化钩子（含被清空）。ElTreeSelect 借此把关键字下发给内嵌的 ElTree。</summary>
        protected virtual void OnFilterTextChanged(string value)
        {
        }
        private static long dropDownIdSeed;
        internal bool OptionsRendered { get; private set; }
        internal string DropDownId { get; } = $"el-select-dropdown-{Interlocked.Increment(ref dropDownIdSeed)}";

        [Parameter]
        public string Label { get; set; }

        [Parameter]
        public Action<string> LabelChanged { get; set; }
        /// <summary>
        /// 当前已注册的候选项（含分组标题项），由 <c>ElOption</c> 子组件在渲染时自动注册进来。
        /// 只读快照顺序与声明顺序一致。
        /// </summary>
        public ObservableCollection<SelectResultModel<TValue>> Options { get; } = new ObservableCollection<SelectResultModel<TValue>>();

        [Parameter]
        public TValue InitialValue { get; set; }

        /// <summary>
        /// 占位文案。<b>留空（默认）时取词条 <c>el.select.placeholder</c>，再回落「请选择」</b>；
        /// 显式传值优先级最高。默认值从「请选择」改成 null 就是为了区分"宿主指定"与"未指定"，
        /// 渲染出的文字在不接词条时与改动前一字不变。
        /// </summary>
        [Parameter]
        public string Placeholder { get; set; }
        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }

        [Parameter]
        public EventCallback<TValue> ModelValueChanged { get; set; }

        [Parameter]
        public TValue ModelValue
        {
            get => Value;
            set => Value = value;
        }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool IsDisabled
        {
            get => Disabled;
            set => Disabled = value;
        }

        [Parameter]
        public bool Clearable { get; set; } = true;

        [Parameter]
        public bool IsClearable
        {
            get => Clearable;
            set => Clearable = value;
        }

        [Parameter]
        public bool Loading { get; set; }

        /// <summary>加载中提示。留空时取词条 <c>el.select.loading</c>，再回落「加载中」。</summary>
        [Parameter]
        public string LoadingText { get; set; }

        /// <summary>无数据提示。留空时取词条 <c>el.select.noData</c>，再回落「无数据」。</summary>
        [Parameter]
        public string NoDataText { get; set; }

        /// <summary>过滤后无匹配提示。留空时取词条 <c>el.select.noMatch</c>，再回落「无匹配数据」。</summary>
        [Parameter]
        public string NoMatchText { get; set; }

        [Parameter]
        public bool Filterable { get; set; }

        [Parameter]
        public bool Multiple { get; set; }

        [Parameter]
        public ICollection<TValue> Values { get; set; }

        [Parameter]
        public EventCallback<ICollection<TValue>> ValuesChanged { get; set; }

        [Parameter]
        public string Separator { get; set; } = ", ";

        [Parameter]
        public bool CollapseTags { get; set; }

        [Parameter]
        public int MaxCollapseTags { get; set; } = 1;

        [Parameter]
        public bool ReserveKeyword { get; set; }

        [Parameter]
        public bool Remote { get; set; }

        [Parameter]
        public EventCallback<string> RemoteMethod { get; set; }

        [Parameter]
        public EventCallback<string> OnFilter { get; set; }

        [Parameter]
        public bool FitInputWidth { get; set; } = true;

        /// <summary>
        /// 下拉面板宽度（px）。大于 0 时面板按这个宽度画，<see cref="FitInputWidth"/> 与自动宽度都不再参与
        /// （否则 <c>ElPopup.RenderDropDownAsync</c> 会用触发器的 rect 覆盖面板宽）；0（默认）保持既有口径。
        /// 用于「选项文字比触发器长」的场景：面板跟控件等宽时长选项会被 <c>.el-select-dropdown__item</c>
        /// 的 <c>overflow:hidden</c> 静默截断，而消费侧没有任何属性能改到面板宽。
        /// </summary>
        [Parameter]
        public int DropDownWidth { get; set; }

        [Parameter]
        public int PopperMaxHeight { get; set; } = 274;

        [Parameter]
        public string PopperClass { get; set; }

        [Parameter]
        public string PopperStyle { get; set; }

        /// <summary>
        /// 下拉面板顶部固定区域（对齐 EP 2.5.2+ 的 <c>header</c> 插槽）。
        /// 官方的 el-select 本身没有「面板内搜索框」属性 —— 搜索就是 <see cref="Filterable"/>
        /// 让触发器里的输入框可编辑；若要在面板里再放一个独立搜索框，官方做法正是本插槽。
        /// </summary>
        [Parameter]
        public RenderFragment HeaderContent { get; set; }

        /// <summary>下拉面板底部固定区域（对齐 EP 2.5.2+ 的 <c>footer</c> 插槽）。</summary>
        [Parameter]
        public RenderFragment FooterContent { get; set; }

        [Parameter]
        public EventCallback<bool> OnVisibleChange { get; set; }

        [Parameter]
        public EventCallback OnEndReached { get; set; }

        [Parameter]
        public EventCallback<FocusEventArgs> OnFocus { get; set; }

        [Parameter]
        public EventCallback<FocusEventArgs> OnBlur { get; set; }

        [Parameter]
        public InputSize Size { get; set; } = InputSize.Normal;

        /// <summary>
        /// 当绑定为枚举时，指定哪些枚举名需要忽略
        /// </summary>
        [Parameter]
        public string[] IgnoreEnumNames { get; set; } = new string[0];

        /// <summary>多选时 tag 的类型 (success/info/warning/danger)，默认 info</summary>
        [Parameter]
        public string TagType { get; set; } = "info";

        /// <summary>多选时 tag 的 effect (light/dark/plain)，默认 light</summary>
        [Parameter]
        public string TagEffect { get; set; } = "light";

        /// <summary>CollapseTags 时是否显示 tooltip</summary>
        [Parameter]
        public bool CollapseTagsTooltip { get; set; }

        /// <summary>aria-label</summary>
        [Parameter]
        public string AriaLabel { get; set; }

        /// <summary>选中后是否清空关键字（Filterable 时）</summary>
        [Parameter]
        public bool ClearAfterSelect { get; set; }

        /// <summary>回车是否默认选中第一个选项</summary>
        [Parameter]
        public bool DefaultFirstOption { get; set; }

        /// <summary>是否允许创建新选项（Filterable + 非 Multiple 时可用）</summary>
        [Parameter]
        public bool AllowCreate { get; set; }

        /// <summary>AllowCreate 创建新选项后的回调，参数为输入文字</summary>
        [Parameter]
        public EventCallback<string> OnCreateOption { get; set; }

        /// <summary>多选时最多可选数量（0 = 不限制，对齐 EP multiple-limit）</summary>
        [Parameter]
        public int MultipleLimit { get; set; }

        /// <summary>多选移除某个 tag 时触发，参数为被移除项的值（对齐 EP remove-tag）</summary>
        [Parameter]
        public EventCallback<TValue> OnRemoveTag { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            effectiveDisabled = Disabled || (FormItem?.Form?.Disabled ?? false);
            effectiveSize = Size == InputSize.Normal
                ? FormItem?.Size ?? FormItem?.Form?.EffectiveSize ?? ResolveInputSize(InputSize.Normal)
                : Size;
            var sizeCssValue = GetSizeCssValue(effectiveSize);
            warpperClsBuilder = HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-select", Cls)
                .AddIf(effectiveDisabled, "is-disabled")
                .AddIf(Loading, "is-loading")
                .AddIf(Filterable, "is-filterable")
                .AddIf(Multiple, "is-multiple")
                .AddIf(Clearable, "is-clearable")
                .AddIf(IsDropDownOpen, "is-focus")
                .AddIf(sizeCssValue != null, $"el-select--{sizeCssValue}");
            if (Multiple)
            {
                SyncSelectedOptionsFromValues();
            }
            if (valueType == null)
            {
                InitilizeEnumValues(FormItem != null);
            }
            if (FormItem == null)
            {
                Label = Label ?? Options.FirstOrDefault(x => TypeHelper.Equal(x.Key, Value))?.Text
                    ?? ResolveLabelFromUnmountedOptions();
                return;
            }

            if (FormItem.OriginValueHasRendered)
            {
                return;
            }
            FormItem.OriginValueHasRendered = true;
            if (FormItem.Form?.Values.Any() ?? false)
            {
                Value = FormItem.OriginValue;
            }

            if (dict != null && Value != null)
            {
                Label = Label ?? dict[Value];
            }
            // 表单里的下拉同样可能是「预置值 + 声明式子项」，dict 为空时算不出文本
            Label = Label ?? ResolveLabelFromUnmountedOptions();
            SetFieldValue(Value, false);
        }

        private static readonly Type optionComponentType = typeof(ElOption<TValue>);

        /// <summary>扫描 ChildContent 时允许的最大嵌套层数：只用来兜住「分组 / 循环 / div 包裹」这类
        /// 真实写法，再深就不该继续钻（也防住自相似片段把递归拖长）。</summary>
        private const int OptionScanMaxDepth = 4;

        /// <summary>
        /// 「预置 <c>Value</c> ＋ 声明式 <c>ElOption</c> 子项」的下拉首屏不回显的补救（单选）。
        /// <para>
        /// 真身：<see cref="ElOption{TValue}"/> 挂在弹层里，而弹层内容只在第一次点开时才交给
        /// 宿主（<see cref="ToggleDropDownAsync"/> 里才把 <see cref="ChildContent"/> 塞进 DropDownOption），
        /// 所以首屏 <see cref="Options"/> 是空的 —— <c>OnParametersSet</c> 与 <c>OnAfterRenderAsync</c>
        /// 那两条「拿 Options 算文本」的回填都落空，表现为输入框空白、占位符顶着，点开一次才补上。
        /// Element Plus 的下拉是 v-show，选项一挂载就注册，所以原生没有这个问题。
        /// </para>
        /// <para>
        /// 这里<b>不</b>改成首屏就挂载弹层：那会让全站每个下拉都提前建出一整份候选项 DOM
        /// （弹层隔离与首屏渲染开销的既有结论会整个作废）。改法是把 <see cref="ChildContent"/>
        /// 的帧序列<b>只读</b>地扫一遍，取 <c>Value</c> 匹配那条的 <c>Text</c>，手法与
        /// <c>ElMarkdownGrow.ExtractMarkdownFromChildContent</c> 同源。
        /// </para>
        /// <para>
        /// 多选是同一个洞的另一半：候选项没注册时 <see cref="SyncSelectedOptionsFromValues"/> 只能回落到
        /// <c>Convert.ToString(value)</c>，于是数字字典（生产状态一类）的标签会显示成 <c>0 / 1 / 2</c>
        /// 而不是汉字。那条路径走 <see cref="CollectUnmountedOptionTexts"/>，共用同一套遍历。
        /// </para>
        /// </summary>
        private string ResolveLabelFromUnmountedOptions()
        {
            // 只在「首屏、单选、有值、没算出文本、子项还没注册」这一种前提下扫，
            // 其余情形走原有路径（点开下拉后 Options 有真数据，以真数据为准）。
            if (Multiple || Value == null || !string.IsNullOrEmpty(Label) || Options.Count > 0 || ChildContent == null)
            {
                return null;
            }

            string matched = null;
            WalkOptionFragment(ChildContent, 0, (key, text) =>
            {
                if (!TypeHelper.Equal(key, Value))
                {
                    return false;
                }
                matched = text;
                return true;
            });
            return matched;
        }

        /// <summary>多选首屏的标签回填：候选项还没挂进弹层时，把 <see cref="ChildContent"/> 只读扫成
        /// 「值 → 文本」的表。<para>
        /// 只在 <see cref="Options"/> 还是空的时候才扫（点开下拉后一律以真注册的选项为准），
        /// 所以不做缓存：扫描结果的生命周期与「候选项未注册」这个前提一样短，缓存反而会在
        /// 父级重新生成候选项（<c>@foreach</c> 的数据源换了）之后继续吐旧文本。
        /// </para></summary>
        private Dictionary<TValue, string> CollectUnmountedOptionTexts()
        {
            if (Options.Count > 0 || ChildContent == null)
            {
                return null;
            }
            var map = new Dictionary<TValue, string>();
            WalkOptionFragment(ChildContent, 0, (key, text) =>
            {
                // 同一个值重复出现时以先出现的为准，和弹层里第一个命中的选项口径一致
                if (!map.ContainsKey(key))
                {
                    map[key] = text;
                }
                return false;
            });
            return map;
        }

#pragma warning disable BL0006 // 只读渲染帧，不往本组件的渲染树里写
        /// <summary>把一段片段渲染进临时 builder 后，遍历其中所有 <see cref="ElOption{TValue}"/>
        /// 并按声明顺序回调 <paramref name="onOption"/>。<para>
        /// <paramref name="onOption"/> 返回 <c>true</c> 表示「已经拿到要的」，遍历就地中止（单选命中即用）；
        /// 返回值本身是「是否被中止」，回调全程返回 <c>false</c> 时（多选收集）返回 <c>false</c>。
        /// </para></summary>
        /// <param name="fragment">待扫描的片段</param>
        /// <param name="depth">当前嵌套深度，用于限制递归层数</param>
        /// <param name="onOption">收到一对（选项值, 选项文本）</param>
        private bool WalkOptionFragment(RenderFragment fragment, int depth, Func<TValue, string, bool> onOption)
        {
            if (fragment == null || depth > OptionScanMaxDepth)
            {
                return false;
            }

            ArrayRange<RenderTreeFrame> frames;
            try
            {
                var builder = new RenderTreeBuilder();
                fragment(builder);
                frames = builder.GetFrames();
            }
            catch (Exception)
            {
                // 片段里写了只有在真实渲染上下文里才成立的东西（拿级联参数、@context 等），
                // 在脱离弹层的临时 builder 上可能构造不出来。此时退回改动前的行为（空白 + 占位符、
                // 多选回落裸值），而不是让整页崩。
                return false;
            }

            return WalkFrames(frames.Array, 0, frames.Count, depth, onOption);
        }

        private bool WalkFrames(RenderTreeFrame[] array, int start, int end, int depth, Func<TValue, string, bool> onOption)
        {
            for (var index = start; index < end; index++)
            {
                var frame = array[index];
                int subtreeEnd;
                switch (frame.FrameType)
                {
                    // Region（@if / @foreach）和 Element 的子帧就在同一段数组里，直接钻进子树继续找
                    case RenderTreeFrameType.Region:
                        subtreeEnd = index + frame.RegionSubtreeLength;
                        if (WalkFrames(array, index + 1, Math.Min(subtreeEnd, end), depth, onOption))
                        {
                            return true;
                        }
                        index = subtreeEnd - 1;
                        break;

                    case RenderTreeFrameType.Element:
                        subtreeEnd = index + frame.ElementSubtreeLength;
                        if (WalkFrames(array, index + 1, Math.Min(subtreeEnd, end), depth, onOption))
                        {
                            return true;
                        }
                        index = subtreeEnd - 1;
                        break;

                    case RenderTreeFrameType.Component:
                        subtreeEnd = index + frame.ComponentSubtreeLength;
                        var isOption = frame.ComponentType == optionComponentType;
                        RenderFragment nestedContent = null;
                        var optionValue = default(TValue);
                        var hasOptionValue = false;
                        string optionText = null;
                        for (var p = index + 1; p < subtreeEnd; p++)
                        {
                            var attribute = array[p];
                            // 组件入参紧跟在组件帧之后连续发射；碰到别的帧型就说明参数已经读完
                            if (attribute.FrameType != RenderTreeFrameType.Attribute)
                            {
                                break;
                            }
                            switch (attribute.AttributeName)
                            {
                                case "ChildContent":
                                    nestedContent = attribute.AttributeValue as RenderFragment;
                                    break;
                                case "Value" when isOption:
                                    if (attribute.AttributeValue is TValue parsed)
                                    {
                                        optionValue = parsed;
                                        hasOptionValue = true;
                                    }
                                    break;
                                case "Text" when isOption:
                                    optionText = attribute.AttributeValue as string;
                                    break;
                            }
                        }

                        if (isOption)
                        {
                            // 与 ElOption.OnInitialized 同一条口径：没写 Text 就回落到 Value.ToString()
                            var text = string.IsNullOrEmpty(optionText) ? Convert.ToString(optionValue) : optionText;
                            if (hasOptionValue && onOption(optionValue, text))
                            {
                                return true;
                            }
                        }
                        else if (WalkOptionFragment(nestedContent, depth + 1, onOption))
                        {
                            // 分组（ElOptionGroup）之类包了一层：子项不在当前数组里，得钻它的 ChildContent
                            return true;
                        }
                        index = subtreeEnd - 1;
                        break;
                }
            }

            return false;
        }
#pragma warning restore BL0006

        private void InitilizeEnumValues(bool firstItemAsValue)
        {
            valueType = typeof(TValue);
            nullable = Nullable.GetUnderlyingType(valueType);
            Clearable = Clearable && nullable != null;
            valueType = nullable ?? valueType;
            var valueSet = false;
            if (valueType.IsEnum)
            {
                var names = Enum.GetNames(valueType);
                var values = Enum.GetValues(valueType);
                dict = new Dictionary<TValue, string>();
                for (int i = 0; i < names.Length; i++)
                {
                    var name = names[i];
                    if (IgnoreEnumNames.Contains(name, StringComparer.CurrentCultureIgnoreCase))
                    {
                        continue;
                    }
                    var value = values.GetValue(i);
                    var field = valueType.GetField(name);
                    var text = field.GetCustomAttributes(typeof(DescriptionAttribute), true)
                        .Cast<DescriptionAttribute>()
                        .FirstOrDefault()?.Description ?? name;
                    if (!valueSet && firstItemAsValue)
                    {
                        valueSet = true;
                        if (nullable == null)
                        {
                            if (!TypeHelper.Equal(Value, (TValue)value))
                            {
                                Value = (TValue)value;
                                InitialValue = Value;
                                SetFieldValue(Value, false);
                            }
                            Label = text;
                        }
                    }
                    dict.Add((TValue)value, text);
                }
                ChildContent = builder =>
                {
                    int seq = 0;
                    foreach (var itemValue in dict.Keys)
                    {
                        builder.OpenComponent<ElOption<TValue>>(seq++);
                        builder.AddAttribute(seq++, "Text", dict[itemValue]);
                        builder.AddAttribute(seq++, "Value", itemValue);
                        builder.CloseComponent();
                    }
                };
            }
        }

        public void UpdateValue(string text)
        {
            _ = UpdateValueAsync(text);
        }

        protected virtual async Task UpdateValueAsync(string text)
        {
            if (!Filterable)
            {
                if (string.IsNullOrEmpty(text))
                {
                    ClearSelection();
                }
                return;
            }
            filterText = text;
            // 关键字变了：让弹层（含「无匹配数据」空态）重渲染，否则下拉里的选项列表不会收缩。
            dropDownOption?.RequestRender?.Invoke();
            if (Remote)
            {
                if (RemoteMethod.HasDelegate)
                {
                    await RemoteMethod.InvokeAsync(text);
                }
                if (OnFilter.HasDelegate)
                {
                    await OnFilter.InvokeAsync(text);
                }
                StateHasChanged();
                return;
            }
            if (OnFilter.HasDelegate)
            {
                await OnFilter.InvokeAsync(text);
            }
            // 过滤模式：只更新关键字并刷新下拉，不在此处自动选中 / 关闭。
            // 精确命中也不自动选中（对齐 EP / ElSelectV2）——选中交给回车（SelectHoveredOptionAsync）或点击，
            // 否则输入「完整词」会把下拉直接关掉，搜不到其它候选。
            if (string.IsNullOrEmpty(text))
            {
                ClearSelection();
            }
            StateHasChanged();
        }

        /// <summary>
        /// 外部（例如下拉 <c>HeaderContent</c> 里的搜索框）驱动本组件的过滤关键字。
        /// 直接改写 <see cref="filterText"/> 并触发自身重渲染，因此即使搜索框位于弹层插槽内也能即时过滤候选项。
        /// </summary>
        public void ApplyFilter(string text)
        {
            filterText = text;
            StateHasChanged();
        }
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Inject]
        internal PopupService PopupService { get; set; }

        /// <summary>
        /// 当前选中值
        /// </summary>
        [Parameter]
        public TValue Value { get; set; }

        [Parameter]
        public EventCallback<ElementChangeEventArgs<SelectResultModel<TValue>>> OnChange { get; set; }

        [Parameter]
        public EventCallback<ElementChangeEventArgs<SelectResultModel<TValue>>> OnChanging { get; set; }

        internal SelectResultModel<TValue> SelectedOption
        {
            get
            {
                return selectedOption;
            }
            set
            {
                if (value == null)
                {
                    Value = default;
                    Label = string.Empty;
                }
                else
                {
                    Value = value.Key;
                    Label = value.Text;
                }
                selectedOption = value;
                LabelChanged?.Invoke(Label);
                if (ValueChanged.HasDelegate)
                {
                    _ = ValueChanged.InvokeAsync(Value);
                }
                if (ModelValueChanged.HasDelegate)
                {
                    _ = ModelValueChanged.InvokeAsync(Value);
                }

            }
        }

        private SelectResultModel<TValue> selectedOption;
        internal DropDownOption dropDownOption;
        private Dictionary<TValue, string> dict;

        /// <summary>
        /// 选中（或取消选中）指定候选项，等价于用户点击该选项：会同步触发
        /// <c>ValueChanged</c> / 表单校验 / 关闭面板（单选）等全部副作用。
        /// </summary>
        public Task OnInternalSelectAsync(SelectResultModel<TValue> item) => SelectValueAsync(item);

        /// <summary>
        /// 单选选中一项后是否收起面板。官方 tree-select 在勾选（showCheckbox）模式下由
        /// <c>onCheck</c> 只更新 modelValue、不调 <c>handleOptionSelect</c>，面板保持打开
        /// （<c>Temp/ep2140-treeselect-tree.mjs:103-131</c>），所以子类把它置为 false 即可。
        /// </summary>
        protected internal virtual bool CloseDropDownOnSelect => !Multiple;

        private async Task SelectValueAsync(SelectResultModel<TValue> item)
        {
            var args = new ElementChangeEventArgs<SelectResultModel<TValue>>();
            args.NewValue = item;
            args.OldValue = SelectedOption;
            if (OnChanging.HasDelegate)
            {
                await OnChanging.InvokeAsync(args);
                if (args.DisallowChange)
                {
                    return;
                }
            }

            if (Multiple)
            {
                await ToggleMultipleOptionAsync(item);
            }
            else
            {
                SelectedOption = item;
                SetFieldValue(item.Key, true);
                Value = item.Key;
                if (dict != null)
                {
                    dict.TryGetValue(Value, out var label);
                    Label = label;
                }
                if (OnChange.HasDelegate)
                {
                    await OnChange.InvokeAsync(args);
                }
            }
            // 收起面板排在写值之后：EP 2.14.0 单选分支是 emit(update:modelValue) → emitChange → expanded=false
            // （Temp/ep2140-useSelect.ts:607-612）。此前这里先 await 了关闭动画（内部有 Task.Delay(10)+Task.Delay(200)），
            // 回显值被压在动画后面，点完选项要空等 ~210ms 才看得到输入框变化。
            if (CloseDropDownOnSelect && dropDownOption?.Instance != null)
            {
                await dropDownOption.Instance.CloseDropDownAsync(dropDownOption);
            }
            EnableClearButton = false;
            if (ClearAfterSelect || !ReserveKeyword)
            {
                filterText = null;
            }
            StateHasChanged();
        }

        internal async Task OnSelectClick(MouseEventArgs e)
        {
            await ToggleDropDownAsync();
            await FocusInnerInputAsync();
        }

        /// <summary>
        /// 对齐 Element Plus：点击后焦点落在内层 input 上（EP 的选择器焦点就在 input，根节点只做事件代理）。
        /// 根节点只有 tabindex="-1"，鼠标点击它时 Chrome 仍会把它设成 activeElement，
        /// 此时若不转焦，焦点会停在根节点：既不显示 Element 的输入框内描边（无 is-focus），
        /// 又因为根节点不在 Tab 序列里，下一次 Tab 会先落到内层 input、要按两次才离开本控件。
        /// </summary>
        private async Task FocusInnerInputAsync()
        {
            if (effectiveDisabled || innerInput == null)
            {
                return;
            }
            try
            {
                await innerInput.FocusAsync();
            }
            catch (Exception)
            {
                // 宿主页面未引用 dom.js 时忽略：只影响点击后的自动聚焦，不影响选择功能
            }
        }

        private async Task ToggleDropDownAsync()
        {
            if (effectiveDisabled)
            {
                return;
            }
            if (EnableClearButton)
            {
                EnableClearButton = false;
                ClearSelection();
                return;
            }
            if (IsDropDownOpen)
            {
                if (dropDownOption?.Instance != null)
                {
                    await dropDownOption.Instance.CloseDropDownAsync(dropDownOption);
                }
                return;
            }

            hoveredIndex = FindSelectedIndex();
            dropDownOption = new DropDownOption()
            {
                IsTree = isTree,
                Select = this,
                Target = elementSelect,
                OptionContent = ChildContent,
                HeaderContent = HeaderContent,
                FooterContent = FooterContent,
                PopperClass = PopperClass,
                PopperStyle = PopperStyle,
                MaxHeight = PopperMaxHeight,
                FitInputWidth = DropDownWidth > 0 ? false : FitInputWidth,
                AutoWidth = DropDownWidth <= 0,
                Width = DropDownWidth,
                DropDownId = DropDownId,
                Refresh = () =>
                {
                    StateHasChanged();
                },
                OnClosed = () => NotifyVisibleChangeAsync(false),
                IsShow = true
            };
            PopupService.SelectDropDownOptionsCore.Add(dropDownOption);
            await NotifyVisibleChangeAsync(true);
        }

        private void ClearSelection()
        {
            SelectedOption = null;
            filterText = null;
            if (Multiple)
            {
                selectedOptions.Clear();
                Values = CreateValuesCollection(selectedOptions.Select(x => x.Key));
                if (ValuesChanged.HasDelegate)
                {
                    _ = ValuesChanged.InvokeAsync(Values);
                }
                Label = string.Empty;
            }
            SetFieldValue(Value, true);
        }

        private async Task OnKeyDownAsync(KeyboardEventArgs e)
        {
            if (effectiveDisabled)
            {
                return;
            }
            switch (e.Key)
            {
                case "ArrowDown":
                    if (!IsDropDownOpen)
                    {
                        // 对齐 Element Plus：方向键既打开下拉又立即移动高亮（一次按键可见反馈，
                        // 否则首按只打开、高亮不动，用户会以为「上下键不能选」）。
                        await OpenDropDownAsync();
                        hoveredIndex = -1;
                        MoveHover(1);
                        break;
                    }
                    MoveHover(1);
                    break;
                case "ArrowUp":
                    if (!IsDropDownOpen)
                    {
                        await OpenDropDownAsync();
                        hoveredIndex = -1;
                        MoveHover(-1);
                        break;
                    }
                    MoveHover(-1);
                    break;
                case "Enter":
                    if (!IsDropDownOpen)
                    {
                        await OpenDropDownAsync();
                        return;
                    }
                    await SelectHoveredOptionAsync();
                    break;
                case "Escape":
                    if (dropDownOption?.Instance != null)
                    {
                        await dropDownOption.Instance.CloseDropDownAsync(dropDownOption);
                    }
                    break;
                case "Tab":
                    // 对齐 EP：Tab 离开控件时收起下拉。不 preventDefault —— 保留浏览器原生 Tab 焦点移动
                    if (dropDownOption?.Instance != null && IsDropDownOpen)
                    {
                        await dropDownOption.Instance.CloseDropDownAsync(dropDownOption);
                    }
                    break;
                case "Backspace":
                    if (Multiple && SelectedOptions.Any() && string.IsNullOrWhiteSpace(filterText))
                    {
                        await RemoveTagAsync(SelectedOptions.Last());
                    }
                    else if (Clearable && !Filterable && !TypeHelper.Equal(Value, default))
                    {
                        ClearSelection();
                    }
                    break;
            }
        }

        private async Task OpenDropDownAsync()
        {
            if (IsDropDownOpen)
            {
                return;
            }
            await ToggleDropDownAsync();
        }

        private void MoveHover(int step)
        {
            var enabledOptions = FilteredOptions.Where(x => !x.Disabled).ToList();
            if (!enabledOptions.Any())
            {
                hoveredIndex = -1;
                return;
            }
            var currentOption = hoveredIndex >= 0 && hoveredIndex < Options.Count ? Options[hoveredIndex] : null;
            var currentIndex = currentOption == null ? -1 : enabledOptions.IndexOf(currentOption);
            currentIndex = (currentIndex + step + enabledOptions.Count) % enabledOptions.Count;
            hoveredIndex = Options.IndexOf(enabledOptions[currentIndex]);
            StateHasChanged();
            // 下拉里的候选项由 ElPopup 宿主渲染，不在本组件渲染树内：只 StateHasChanged 只能刷新
            // 触发器本身，下拉里的高亮项不会动 —— 必须显式请求宿主重渲染，否则键盘上下键改了
            // hoveredIndex 却看不到高亮变化（看起来像「上下键不能选择」）。
            dropDownOption?.RequestRender?.Invoke();
            // 宿主重渲染还不够：ElOption 的入参没变，Blazor 会跳过它，得让它自己重渲染。
            HoverChanged?.Invoke();
        }

        private async Task SelectHoveredOptionAsync()
        {
            if (hoveredIndex < 0 || hoveredIndex >= Options.Count)
            {
                return;
            }
            if (Loading)
            {
                return;
            }
            var option = Options[hoveredIndex];
            if (option.Disabled)
            {
                return;
            }
            await SelectValueAsync(option);
        }

        internal bool IsOptionSelected(SelectResultModel<TValue> option)
        {
            if (option == null)
            {
                return false;
            }
            return Multiple
                ? selectedOptions.Any(x => TypeHelper.Equal(x.Key, option.Key))
                : TypeHelper.Equal(option.Key, Value);
        }

        internal bool IsOptionHover(SelectResultModel<TValue> option)
        {
            return option != null && hoveredIndex >= 0 && hoveredIndex < Options.Count && Options[hoveredIndex] == option;
        }

        /// <summary>
        /// 判断候选项在当前筛选词下是否可见。未开启筛选 / 远程模式 / 无筛选词时恒为 true。
        /// </summary>
        public bool IsOptionVisible(SelectResultModel<TValue> option)
        {
            if (!Filterable || Remote || string.IsNullOrWhiteSpace(filterText))
            {
                return true;
            }
            return (option.Text ?? string.Empty).IndexOf(filterText, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private int FindSelectedIndex()
        {
            for (var i = 0; i < Options.Count; i++)
            {
                if (IsOptionSelected(Options[i]))
                {
                    return i;
                }
            }
            return Options.Count > 0 ? 0 : -1;
        }

        private IReadOnlyList<SelectResultModel<TValue>> FilteredOptions => Options.Where(IsOptionVisible).ToList();

        private Task NotifyVisibleChangeAsync(bool visible)
        {
            if (!visible && dropDownOption != null)
            {
                if (!ReserveKeyword)
                {
                    filterText = null;
                }
                dropDownOption = null;
            }
            if (!OnVisibleChange.HasDelegate)
            {
                StateHasChanged();
                return Task.CompletedTask;
            }
            return OnVisibleChange.InvokeAsync(visible);
        }

        protected async Task OnInputFocusAsync(FocusEventArgs e)
        {
            if (OnFocus.HasDelegate)
            {
                await OnFocus.InvokeAsync(e);
            }
        }

        protected async Task OnInputBlurAsync(FocusEventArgs e)
        {
            if (OnBlur.HasDelegate)
            {
                await OnBlur.InvokeAsync(e);
            }
        }

        internal void RegisterOption(SelectResultModel<TValue> option)
        {
            OptionsRendered = true;
            if (Options.Contains(option))
            {
                return;
            }
            Options.Add(option);
            if (Multiple)
            {
                SyncSelectedOptionsFromValues();
            }
            else if (string.IsNullOrEmpty(Label))
            {
                // 受控初始值：父级直接传 Value 且无 FormItem 时，首次 OnParametersSet 里子项还没注册，
                // Options 为空导致显示文本算不出来（输入框一直空白）。等子项注册完补一次。
                // 注意：此处正处在渲染过程中，直接 StateHasChanged 会被丢掉，只能打标记等渲染结束再刷。
                var matched = Options.FirstOrDefault(x => TypeHelper.Equal(x.Key, Value));
                if (matched != null && !string.IsNullOrEmpty(matched.Text))
                {
                    Label = matched.Text;
                    pendingLabelBackfill = matched.Text;
                }
            }

            // 下拉第一次打开时 Options 还是空的（ElOption 只在弹层被渲染时才挂载注册），
            // 打开那一刻算不出选中项，hoveredIndex 停在 -1：下拉里没有任何高亮，键盘上下键
            // 也不是从当前选中项起跳。候选项注册进来后补算一次（对齐 EP 打开即高亮选中项）。
            if (hoveredIndex < 0 && dropDownOption != null && dropDownOption.IsShow)
            {
                var selectedIndex = FindSelectedIndex();
                if (selectedIndex >= 0)
                {
                    hoveredIndex = selectedIndex;
                    HoverChanged?.Invoke();
                }
            }
        }

        internal bool IsDropDownOpen => dropDownOption != null && dropDownOption.IsShow;

        /// <summary>显示文本回填的落地时机：RegisterOption 是在渲染过程中改 Label 的，
        /// 那次 StateHasChanged 会被丢弃，必须等本帧渲染结束再刷一次，DOM 里的输入框才会显示选中项。</summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                // 键盘导航（上下键）只在控件内移动高亮项，不应滚动页面 —— 原生方向键的默认行为
                // 会滚动最近的可滚动祖先（在表格/页面上表现为「页面跟着动了」）。Blazor 无法按键
                // 动态 preventDefault（@onkeydown:preventDefault 是静态的，会连 Tab 一起吞掉），
                // 所以交给 dom.js 按 key 精确拦截。
                try
                {
                    await JSRuntime.InvokeVoidAsync("elementSelectLockNavScroll", elementSelect);
                }
                catch (Exception)
                {
                    // 宿主页面未引用 dom.js 时忽略：只影响键盘导航时是否滚动页面
                }
            }
            if (pendingLabelBackfill != null)
            {
                pendingLabelBackfill = null;
                await InvokeAsync(StateHasChanged);
                return;
            }
            if (firstRender && !Multiple && string.IsNullOrEmpty(Label))
            {
                var matched = Options.FirstOrDefault(x => TypeHelper.Equal(x.Key, Value));
                if (matched != null && !string.IsNullOrEmpty(matched.Text))
                {
                    Label = matched.Text;
                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        private string pendingLabelBackfill;

        protected bool IsSelectDisabled => effectiveDisabled;

        protected string AriaMultiple => Multiple ? "true" : "false";

        protected string AriaExpanded => IsDropDownOpen ? "true" : "false";

        protected string AriaDisabled => IsSelectDisabled ? "true" : "false";

        protected string AriaBusy => Loading ? "true" : "false";

        protected InputSize EffectiveSize => effectiveSize;

        internal string DisplayLabel => Multiple
            ? (Filterable && IsDropDownOpen ? filterText : string.Empty)
            : (Filterable && IsDropDownOpen && filterText != null ? filterText : Label);

        // 占位、加载、空态、无匹配四处文案统一走「显式参数 > 词条 > 改动前字面量」，
        // 键名与 EP 的 useSelect.ts 完全对齐（el.select.placeholder / loading / noData / noMatch）。
        private const string DefaultPlaceholder = "请选择";
        private const string DefaultLoadingText = "加载中";
        private const string DefaultNoDataText = "无数据";
        private const string DefaultNoMatchText = "无匹配数据";

        protected string EffectivePlaceholder => Multiple && SelectedOptions.Any()
            ? string.Empty
            : Lang.Text("el.select.placeholder", Placeholder, DefaultPlaceholder);

        private readonly List<SelectResultModel<TValue>> selectedOptions = new List<SelectResultModel<TValue>>();

        internal IReadOnlyList<SelectResultModel<TValue>> SelectedOptions => selectedOptions;

        protected IEnumerable<SelectResultModel<TValue>> VisibleSelectedOptions => CollapseTags
            ? selectedOptions.Take(Math.Max(1, MaxCollapseTags))
            : selectedOptions;

        protected int CollapsedTagCount => CollapseTags
            ? Math.Max(0, selectedOptions.Count - Math.Max(1, MaxCollapseTags))
            : 0;

        private async Task ToggleMultipleOptionAsync(SelectResultModel<TValue> item)
        {
            var existing = selectedOptions.FirstOrDefault(x => TypeHelper.Equal(x.Key, item.Key));
            if (existing == null)
            {
                if (Multiple && MultipleLimit > 0 && selectedOptions.Count >= MultipleLimit)
                {
                    return;
                }
                selectedOptions.Add(item);
            }
            else
            {
                selectedOptions.Remove(existing);
            }
            await CommitMultipleSelectionAsync(true);
        }

        protected async Task RemoveTagAsync(SelectResultModel<TValue> item)
        {
            if (IsSelectDisabled || item == null)
            {
                return;
            }
            selectedOptions.RemoveAll(x => TypeHelper.Equal(x.Key, item.Key));
            await CommitMultipleSelectionAsync(true);
            if (OnRemoveTag.HasDelegate)
            {
                await OnRemoveTag.InvokeAsync(item.Key);
            }
        }

        protected Task OnInputClearAsync(MouseEventArgs e)
        {
            if (Multiple)
            {
                ClearSelection();
            }
            return Task.CompletedTask;
        }

        private async Task CommitMultipleSelectionAsync(bool validate)
        {
            Values = CreateValuesCollection(selectedOptions.Select(x => x.Key));
            Label = string.Join(Separator, selectedOptions.Select(x => x.Text).Where(x => !string.IsNullOrWhiteSpace(x)));
            if (Values.Any())
            {
                Value = Values.Last();
                selectedOption = selectedOptions.LastOrDefault();
            }
            else
            {
                Value = default;
                selectedOption = null;
            }
            SetFieldValue(Value, validate);
            if (ValuesChanged.HasDelegate)
            {
                await ValuesChanged.InvokeAsync(Values);
            }
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                await ModelValueChanged.InvokeAsync(Value);
            }
            if (OnChange.HasDelegate)
            {
                await OnChange.InvokeAsync(new ElementChangeEventArgs<SelectResultModel<TValue>>
                {
                    NewValue = selectedOptions.LastOrDefault()
                });
            }
        }

        private void SyncSelectedOptionsFromValues()
        {
            if (!Multiple)
            {
                return;
            }
            var sourceValues = Values ?? ExtractValues(Value);
            // 扫描只在「Options/dict 都给不出这一项文本」时按需做一次，且整个循环里最多做一次：
            // 候选项已注册（点开过下拉）时 CollectUnmountedOptionTexts 直接返回 null，热路径不付扫描钱。
            Dictionary<TValue, string> unmountedTexts = null;
            var unmountedTextsResolved = false;
            selectedOptions.Clear();
            foreach (var value in sourceValues)
            {
                var option = Options.FirstOrDefault(x => TypeHelper.Equal(x.Key, value));
                if (option != null)
                {
                    selectedOptions.Add(option);
                    continue;
                }
                var text = dict != null && dict.TryGetValue(value, out var dictText) ? dictText : null;
                if (text == null)
                {
                    if (!unmountedTextsResolved)
                    {
                        unmountedTexts = CollectUnmountedOptionTexts();
                        unmountedTextsResolved = true;
                    }
                    text = unmountedTexts != null && unmountedTexts.TryGetValue(value, out var scannedText) ? scannedText : null;
                }
                selectedOptions.Add(new SelectResultModel<TValue>
                {
                    Key = value,
                    Text = text ?? Convert.ToString(value)
                });
            }
            Label = string.Join(Separator, selectedOptions.Select(x => x.Text).Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private ICollection<TValue> CreateValuesCollection(IEnumerable<TValue> values)
        {
            if (Values != null)
            {
                return values.ToList();
            }
            return values.ToList();
        }

        private IEnumerable<TValue> ExtractValues(object value)
        {
            if (value is IEnumerable<TValue> typedValues && value is not string)
            {
                return typedValues;
            }
            if (value is IEnumerable enumerable && value is not string)
            {
                return enumerable.Cast<object>()
                    .Where(x => x != null)
                    .Select(x => (TValue)TypeHelper.ChangeType(x, typeof(TValue)));
            }
            return Enumerable.Empty<TValue>();
        }

        string ISelectDropDownContext.LoadingText => Lang.Text("el.select.loading", LoadingText, DefaultLoadingText);

        string ISelectDropDownContext.EmptyText => ShouldShowNoMatch
            ? Lang.Text("el.select.noMatch", NoMatchText, DefaultNoMatchText)
            : Lang.Text("el.select.noData", NoDataText, DefaultNoDataText);

        bool ISelectDropDownContext.ShouldShowEmpty => ShouldShowEmpty;

        bool ISelectDropDownContext.ShouldShowNoMatch => ShouldShowNoMatch;

        Task ISelectDropDownContext.OnEndReachedAsync()
        {
            if (!OnEndReached.HasDelegate || Loading)
            {
                return Task.CompletedTask;
            }
            return OnEndReached.InvokeAsync();
        }

        private bool ShouldShowEmpty => !Loading && (OptionsRendered || ChildContent == null) && !FilteredOptions.Any();

        private bool ShouldShowNoMatch => !Loading && Options.Any() && Filterable && !Remote && !string.IsNullOrWhiteSpace(filterText) && !FilteredOptions.Any();

        protected override void FormItem_OnReset(object value, bool requireRerender)
        {
            var enumValue = (TValue)value;
            if (nullable != null && value == null)
            {
                SelectedOption = null;
            }
            else
            {
                Label = dict?[enumValue];
                Value = enumValue;
            }
            if (ValueChanged.HasDelegate)
            {
                _ = ValueChanged.InvokeAsync(Value);
            }
            else
            {
                StateHasChanged();
            }
        }
        protected override bool ShouldRender()
        {
            return true;
        }

        private static string GetSizeCssValue(InputSize size) => size switch
        {
            InputSize.Large => "large",
            InputSize.Small => "small",
            _ => null
        };
    }
}
