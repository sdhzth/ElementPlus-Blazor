

using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElTransfer : ElementFieldComponentBase<List<string>>
    {
        protected HtmlPropertyBuilder CheckBoxGroupCssBuilder;
        protected string list1KeyWords = string.Empty;
        protected string list2KeyWords = string.Empty;
        private bool hasAppliedValueParameter;
        private List<string> lastAppliedValueParameter;
        private string lastAppliedItemsSignature;
        private Status list1Status = Status.UnChecked;
        internal Status List1Status
        {
            get
            {
                return list1Status;
            }
            set
            {
                var visibleEnabledItems = VisibleList1.Where(x => !x.IsDisabled).ToList();
                List1Checked.RemoveAll(visibleEnabledItems.Contains);
                if (value == Status.Checked)
                {
                    List1Checked.AddRange(visibleEnabledItems);
                }
                list1Status = ResolvePanelStatus(List1Checked, VisibleList1);
                RequireRender = true;
            }
        }
        private Status list2Status = Status.UnChecked;
        internal Status List2Status
        {
            get
            {
                return list2Status;
            }
            set
            {
                var visibleEnabledItems = VisibleList2.Where(x => !x.IsDisabled).ToList();
                List2Checked.RemoveAll(visibleEnabledItems.Contains);
                if (value == Status.Checked)
                {
                    List2Checked.AddRange(visibleEnabledItems);
                }
                list2Status = ResolvePanelStatus(List2Checked, VisibleList2);
                RequireRender = true;
            }
        }

        protected override void FormItem_OnReset(object value, bool requireRerender)
        {
            ResetList2(value);
        }

        [Parameter]
        public List<string> Value { get; set; }

        [Parameter]
        public List<string> ModelValue
        {
            get => Value;
            set => Value = value;
        }

        [Parameter]
        public EventCallback<List<string>> ValueChanged { get; set; }

        [Parameter]
        public EventCallback<List<string>> ModelValueChanged { get; set; }

        [Parameter]
        public EventCallback<List<string>> OnChange { get; set; }

        /// <summary>
        /// 是否显示搜索框（别名 Filterable）
        /// </summary>
        [Parameter]
        public bool EnableSearch { get; set; }

        [Parameter]
        public bool Filterable
        {
            get => EnableSearch;
            set => EnableSearch = value;
        }

        [Parameter]
        public Func<string, TransferItem, bool> FilterMethod { get; set; }

        [Parameter]
        public RenderFragment<TransferItem> ItemContent { get; set; }

        [Parameter]
        public RenderFragment<TransferItem> LeftItemContent { get; set; }

        [Parameter]
        public RenderFragment<TransferItem> RightItemContent { get; set; }

        [Parameter]
        public RenderFragment<TransferPanelContext> LeftHeaderContent { get; set; }

        [Parameter]
        public RenderFragment<TransferPanelContext> RightHeaderContent { get; set; }

        [Parameter]
        public RenderFragment<TransferPanelContext> LeftFooterContent { get; set; }

        [Parameter]
        public RenderFragment<TransferPanelContext> RightFooterContent { get; set; }

        [Parameter]
        public string[] ButtonTexts { get; set; }

        [Parameter]
        public string ToLeftText { get; set; }

        [Parameter]
        public string ToRightText { get; set; }

        /// <summary>空列表文案。默认 null（=宿主没指定）→ <c>el.transfer.noData</c> 词条 → 该词条前的字面量 "No data"。</summary>
        [Parameter]
        public string EmptyText { get; set; }

        /// <summary>过滤后无匹配文案。默认 null → <c>el.transfer.noMatch</c> 词条 → 该词条前的字面量 "No matches"。</summary>
        [Parameter]
        public string NoMatchText { get; set; }

        /// <summary>
        /// 左侧列表搜索输入时触发
        /// </summary>
        [Parameter]
        public EventCallback<string> OnList1Search { get; set; }

        /// <summary>
        /// 左侧搜索框占位文字
        /// </summary>
        [Parameter]
        public string List1SearchPlaceHolder { get; set; }

        /// <summary>
        /// 右侧搜索框占位文字
        /// </summary>
        [Parameter]
        public string List2SearchPlaceHolder { get; set; }

        /// <summary>
        /// 右侧列表搜索输入时触发
        /// </summary>
        [Parameter]
        public EventCallback<string> OnList2Search { get; set; }

        protected async Task List1SearchChanged(string keywords)
        {
            list1KeyWords = keywords;
            if (OnList1Search.HasDelegate)
            {
                await OnList1Search.InvokeAsync(keywords);
                return;
            }
        }
        protected async Task List2SearchChanged(string keywords)
        {
            list2KeyWords = keywords;
            if (OnList2Search.HasDelegate)
            {
                await OnList2Search.InvokeAsync(keywords);
                return;
            }
        }

        /// <summary>
        /// 左侧列表的标题
        /// </summary>
        [Parameter]
        public string LeftTitle { get; set; } = "List 1";

        /// <summary>
        /// 右侧列表的标题
        /// </summary>
        [Parameter]
        public string RightTitle { get; set; } = "List 2";

        private void ResetList2(object value)
        {
            var valueList = NormalizeValueList(value);
            List1 ??= new List<TransferItem>();
            List2 ??= new List<TransferItem>();
            if (valueList == null)
            {
                List2.Clear();
            }
            else
            {
                // 分区必须按「Id ∈ value」在两个列表的并集上重算，而不是只扫 List1。
                // 只扫 List1 时，已经在 List2 里的项被要求左移也回不来：它不在 List1 里，
                // 于是既进不了新的 List2，又被下面的兜底塞回 List1 —— 面板内容与 Value
                // 脱钩（用户报的「选中候选点左键不跑向左边」就是这一支）。
                var all = List1.Concat(List2).Where(x => x != null).ToList();
                var nextList2 = all.Where(x => valueList.Contains(x.Id)).ToList();
                List1 = all.Where(x => !nextList2.Contains(x)).ToList();
                List2 = nextList2;
            }
            List1Checked.Clear();
            List2Checked.Clear();
            list1Status = Status.UnChecked;
            list2Status = Status.UnChecked;
            RequireRender = true;
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            CheckBoxGroupCssBuilder = HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-transfer-panel__list")
                .AddIf(IsFilterable, "is-filterable");
            List1 ??= new List<TransferItem>();
            List2 ??= new List<TransferItem>();
            if (FormItem == null)
            {
                ApplyValueParameterIfNeeded();
                RemoveInvalidCheckedItems();
                ApplyDefaultChecked();
                RefreshPanelStatuses();
                return;
            }
            RemoveInvalidCheckedItems();
            RefreshPanelStatuses();
            if (FormItem.OriginValueHasRendered)
            {
                return;
            }
            FormItem.OriginValueHasRendered = true;
            if (FormItem.Form?.Values.Any() ?? false)
            {
                ResetList2(FormItem.OriginValue);
            }
            else if (Value != null)
            {
                ResetList2(Value);
            }
            ApplyDefaultChecked();
            SyncFieldValue(false);
        }

        internal void ToLeft()
        {
            var movingItems = List2Checked.Where(x => x != null && !x.IsDisabled).ToList();
            if (!movingItems.Any())
            {
                return;
            }
            list2Status = Status.UnChecked;
            list1Status = Status.UnChecked;
            List1.AddRange(movingItems);
            List2.RemoveAll(movingItems.Contains);
            List1Checked.Clear();
            List2Checked.Clear();
            RefreshPanelStatuses();
            RequireRender = true;
            SyncFieldValue(true);
        }

        private void SyncFieldValue(bool validate)
        {
            if (List2 == null)
            {
                return;
            }
            var value = List2.Select(x => x.Id).ToList();
            Value = value;
            RememberAppliedValue(value);
            SetFieldValue(value, validate);
            if (ValueChanged.HasDelegate)
            {
                _ = ValueChanged.InvokeAsync(value);
            }
            if (ModelValueChanged.HasDelegate)
            {
                _ = ModelValueChanged.InvokeAsync(value);
            }
            if (validate && OnChange.HasDelegate)
            {
                _ = OnChange.InvokeAsync(value);
            }
        }

        internal void ToRight()
        {
            var movingItems = List1Checked.Where(x => x != null && !x.IsDisabled).ToList();
            if (!movingItems.Any())
            {
                return;
            }
            list1Status = Status.UnChecked;
            list2Status = Status.UnChecked;
            if (List2 == null)
            {
                List2 = new List<TransferItem>();
            }
            List2.AddRange(movingItems);
            List1.RemoveAll(movingItems.Contains);
            List1Checked.Clear();
            List2Checked.Clear();
            RefreshPanelStatuses();
            RequireRender = true;
            SyncFieldValue(true);
        }

        internal void Status1Changed(Status status, TransferItem transferItem)
        {
            if (transferItem == null || transferItem.IsDisabled)
            {
                return;
            }
            if (status == Status.Checked)
            {
                if (!List1Checked.Contains(transferItem))
                {
                    List1Checked.Add(transferItem);
                }
            }
            else
            {
                List1Checked.Remove(transferItem);
            }

            list1Status = ResolvePanelStatus(List1Checked, VisibleList1);
            RequireRender = true;
            if (OnLeftCheckChange.HasDelegate)
            {
                _ = OnLeftCheckChange.InvokeAsync(List1Checked.Where(x => x != null).Select(x => x.Id).ToList());
            }
        }
        internal void Status2Changed(Status status, TransferItem transferItem)
        {
            if (transferItem == null || transferItem.IsDisabled)
            {
                return;
            }
            if (status == Status.Checked)
            {
                if (!List2Checked.Contains(transferItem))
                {
                    List2Checked.Add(transferItem);
                }
            }
            else
            {
                List2Checked.Remove(transferItem);
            }

            list2Status = ResolvePanelStatus(List2Checked, VisibleList2);
            RequireRender = true;
            if (OnRightCheckChange.HasDelegate)
            {
                _ = OnRightCheckChange.InvokeAsync(List2Checked.Where(x => x != null).Select(x => x.Id).ToList());
            }
        }

        private IReadOnlyList<TransferItem> VisibleList1 => GetVisibleItems(List1, list1KeyWords).ToList();

        private IReadOnlyList<TransferItem> VisibleList2 => GetVisibleItems(List2, list2KeyWords).ToList();

        private bool IsFilterable => EnableSearch;

        private string LeftFilterPlaceholder => Lang.Text("el.transfer.filterPlaceholder", List1SearchPlaceHolder, "Enter keyword");

        private string RightFilterPlaceholder => Lang.Text("el.transfer.filterPlaceholder", List2SearchPlaceHolder, "Enter keyword");

        private string LeftCountText => Format != null
            ? Format(List1?.Count ?? 0, List1Checked.Count)
            : $"{List1Checked.Count}/{List1?.Count ?? 0}";

        private string RightCountText => Format != null
            ? Format(List2?.Count ?? 0, List2Checked.Count)
            : $"{List2Checked.Count}/{List2?.Count ?? 0}";

        private string ToLeftButtonText => ResolveButtonText(0, ToLeftText);

        private string ToRightButtonText => ResolveButtonText(1, ToRightText);

        private TransferPanelContext LeftPanelContext => CreatePanelContext("left", LeftTitle, List1Checked, List1, VisibleList1);

        private TransferPanelContext RightPanelContext => CreatePanelContext("right", RightTitle, List2Checked, List2, VisibleList2);

        private RenderFragment<TransferItem> ResolveItemContent(bool left)
        {
            return left ? LeftItemContent ?? ItemContent : RightItemContent ?? ItemContent;
        }

        private IEnumerable<TransferItem> GetVisibleItems(IEnumerable<TransferItem> items, string keyword)
        {
            items ??= Enumerable.Empty<TransferItem>();
            if (!IsFilterable || string.IsNullOrWhiteSpace(keyword))
            {
                return items.Where(x => x != null);
            }
            return items.Where(x => x != null && MatchesFilter(keyword, x));
        }

        private bool MatchesFilter(string keyword, TransferItem item)
        {
            if (FilterMethod != null)
            {
                return FilterMethod(keyword, item);
            }
            return (item.Label ?? string.Empty).IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0
                || (item.Id ?? string.Empty).IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private string GetEmptyText(IEnumerable<TransferItem> source, string keyword)
        {
            return IsFilterable && !string.IsNullOrWhiteSpace(keyword) && source?.Any() == true
                ? Lang.Text("el.transfer.noMatch", NoMatchText, "No matches")
                : Lang.Text("el.transfer.noData", EmptyText, "No data");
        }

        private string ResolveButtonText(int index, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                return fallback;
            }
            return ButtonTexts != null && ButtonTexts.Length > index ? ButtonTexts[index] : null;
        }

        private static TransferPanelContext CreatePanelContext(string direction, string title, ICollection<TransferItem> checkedItems, ICollection<TransferItem> allItems, IReadOnlyCollection<TransferItem> visibleItems)
        {
            return new TransferPanelContext
            {
                Direction = direction,
                Title = title,
                CheckedCount = checkedItems?.Count ?? 0,
                TotalCount = allItems?.Count ?? 0,
                VisibleCount = visibleItems?.Count ?? 0
            };
        }

        private static Status ResolvePanelStatus(ICollection<TransferItem> checkedItems, IEnumerable<TransferItem> visibleItems)
        {
            var enabledItems = visibleItems?.Where(x => x != null && !x.IsDisabled).ToList() ?? new List<TransferItem>();
            if (!enabledItems.Any())
            {
                return Status.UnChecked;
            }
            var checkedCount = enabledItems.Count(x => checkedItems?.Contains(x) == true);
            if (checkedCount == enabledItems.Count)
            {
                return Status.Checked;
            }
            if (checkedCount > 0)
            {
                return Status.Indeterminate;
            }
            return Status.UnChecked;
        }

        private void RefreshPanelStatuses()
        {
            list1Status = ResolvePanelStatus(List1Checked, VisibleList1);
            list2Status = ResolvePanelStatus(List2Checked, VisibleList2);
        }

        private void RemoveInvalidCheckedItems()
        {
            List1Checked.RemoveAll(x => x == null || x.IsDisabled || !List1.Contains(x));
            List2Checked.RemoveAll(x => x == null || x.IsDisabled || !List2.Contains(x));
        }

        private void ApplyValueParameterIfNeeded()
        {
            var itemsSignature = CreateItemsSignature();
            if (!ShouldApplyValueParameter(Value, itemsSignature))
            {
                return;
            }

            ResetList2(Value);
            RememberAppliedValue(Value);
        }

        private void ApplyDefaultChecked()
        {
            if (LeftDefaultChecked != null && LeftDefaultChecked.Any())
            {
                List1Checked.AddRange(List1
                    .Where(x => x != null && !x.IsDisabled && LeftDefaultChecked.Contains(x.Id) && !List1Checked.Contains(x)));
            }
            if (RightDefaultChecked != null && RightDefaultChecked.Any())
            {
                List2Checked.AddRange(List2
                    .Where(x => x != null && !x.IsDisabled && RightDefaultChecked.Contains(x.Id) && !List2Checked.Contains(x)));
            }
        }

        private bool ShouldApplyValueParameter(List<string> value, string itemsSignature)
        {
            if (!hasAppliedValueParameter)
            {
                return value != null;
            }

            return !StringListsEqual(lastAppliedValueParameter, value)
                || !string.Equals(lastAppliedItemsSignature, itemsSignature, StringComparison.Ordinal);
        }

        private void RememberAppliedValue(List<string> value)
        {
            hasAppliedValueParameter = true;
            lastAppliedValueParameter = value?.ToList();
            lastAppliedItemsSignature = CreateItemsSignature();
        }

        private string CreateItemsSignature()
        {
            return string.Join("|", (List1 ?? Enumerable.Empty<TransferItem>())
                .Concat(List2 ?? Enumerable.Empty<TransferItem>())
                .Where(x => x != null)
                .Select(x => x.Id ?? string.Empty));
        }

        private static bool StringListsEqual(IReadOnlyCollection<string> left, IReadOnlyCollection<string> right)
        {
            if (left == null || right == null)
            {
                return left == null && right == null;
            }

            return left.SequenceEqual(right);
        }

        private static List<string> NormalizeValueList(object value)
        {
            if (value == null)
            {
                return null;
            }
            if (value is List<string> list)
            {
                return list;
            }
            if (value is IEnumerable<string> enumerable)
            {
                return enumerable.ToList();
            }
            return new List<string> { Convert.ToString(value) };
        }

        /// <summary>
        /// 列表1
        /// </summary>
        [Parameter]
        public List<TransferItem> List1 { get; set; } = new List<TransferItem>();
        internal List<TransferItem> List1Checked { get; set; } = new List<TransferItem>();

        /// <summary>
        /// 列表2
        /// </summary>
        [Parameter]
        public List<TransferItem> List2 { get; set; } = new List<TransferItem>();
                internal List<TransferItem> List2Checked { get; set; } = new List<TransferItem>();

        /// <summary>
        /// 面板计数文案格式化函数（EP: <c>format</c>，签名 <c>(total, checkLength) => string</c>）。
        /// 为 null 时使用默认 "{checkLength}/{total}"。
        /// </summary>
        [Parameter]
        public Func<int, int, string> Format { get; set; }

        /// <summary>
        /// 左侧列表勾选项变化事件（EP: <c>on-left-check-change</c>，参数为当前左侧已勾选 key 列表）
        /// </summary>
        [Parameter]
        public EventCallback<List<string>> OnLeftCheckChange { get; set; }

        /// <summary>
        /// 右侧列表勾选项变化事件（EP: <c>on-right-check-change</c>，参数为当前右侧已勾选 key 列表）
        /// </summary>
        [Parameter]
        public EventCallback<List<string>> OnRightCheckChange { get; set; }

        /// <summary>是否禁用</summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>目标端排序策略 (original/push/unshift/descending/ascending)</summary>
        [Parameter]
        public string TargetOrder { get; set; } = "original";

        [Parameter]
        public string AriaLabel { get; set; }

        [Parameter]
        public string Id { get; set; }

        /// <summary>两侧列表面板宽度 (px)</summary>
        [Parameter]
        public int PanelWidth { get; set; } = 200;

        /// <summary>两侧列表面板高度 (px)</summary>
        [Parameter]
        public int PanelHeight { get; set; } = 300;

        /// <summary>左侧默认选中 key</summary>
        [Parameter]
        public List<string> LeftDefaultChecked { get; set; } = new List<string>();

        /// <summary>右侧默认选中 key</summary>
        [Parameter]
        public List<string> RightDefaultChecked { get; set; } = new List<string>();

        /// <summary>显示全选 checkbox</summary>
        [Parameter]
        public bool ShowAll { get; set; } = true;
    }
}
