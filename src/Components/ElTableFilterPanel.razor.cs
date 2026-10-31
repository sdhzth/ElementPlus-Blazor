using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 列筛选面板：EP <c>filter-panel.vue</c> 的两态（勾选组 / 单选列表）+ 本库扩展的条件式面板。
    /// 不由使用者直接摆放，<see cref="ElTable"/> 点漏斗时才会渲染一个，坐标由表格钉成 position:fixed。
    /// </summary>
    public partial class ElTableFilterPanel : ElementComponentBase
    {
        [Parameter]
        public TableHeader Header { get; set; }

        /// <summary>面板自身的内联样式（含 position:fixed 与视口坐标），由 <see cref="ElTable"/> 测算后下发。</summary>
        [Parameter]
        public string PanelStyle { get; set; }

        /// <summary>草稿已落到 <see cref="Header"/> 上：表格据此重算过滤并抛 <c>FilterChange</c>。</summary>
        [Parameter]
        public EventCallback<TableHeader> Committed { get; set; }

        /// <summary>单选列表点完一项：除了落值还要收起面板（EP 的 <c>handleSelect</c> 里 <c>hidden()</c>）。</summary>
        [Parameter]
        public EventCallback Closed { get; set; }

        private HashSet<object> draft = new HashSet<object>();
        private TableFilterCondition[] slots = new[] { new TableFilterCondition(), new TableFilterCondition() };

        protected HashSet<object> Draft => draft;

        protected TableFilterCondition[] Slots => slots;

        protected IReadOnlyList<TableFilterOperator> Operators => TableFilterCondition.OperatorsFor(Header?.Property?.PropertyType);

        // 前三条与 EP 2.14.0 语言包逐字对齐（zh 见 locale/zh-cn.ts:152-154，en 见 locale/en.ts:155-157）：
        // confirmFilter=筛选/Confirm、resetFilter=重置/Reset、clearFilter=全部/All。
        // 后三条是 EP 没有形态的扩展键（条件面板是本库独有，EP 只有选项列表）。
        protected string ConfirmLabel => Lang.T("el.table.confirmFilter", "筛选");

        protected string ResetLabel => Lang.T("el.table.resetFilter", "重置");

        /// <summary>单选列表首项：EP 用的是 clearFilter，值是「全部」而不是「清除」。</summary>
        protected string ClearLabel => Lang.T("el.table.clearFilter", "全部");

        protected string ApplyLabel => Lang.T("el.table.applyFilter", "应用");

        protected string AdvancedClearLabel => Lang.T("el.table.clearAdvancedFilter", "清除");

        protected string RelationAndLabel => Lang.T("el.table.filterRelationAnd", "且");

        protected string RelationOrLabel => Lang.T("el.table.filterRelationOr", "或");

        /// <summary>面板每次打开都是一棵新组件（ElTable 用 <c>@key</c> 保证），草稿只在第一帧从列的现值取一次。</summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();
            LoadDraft();
        }

        private void LoadDraft()
        {
            draft = Header?.FilterValues == null
                ? new HashSet<object>()
                : new HashSet<object>(Header.FilterValues);
            slots = new[] { new TableFilterCondition(), new TableFilterCondition() };
            if (Header?.AdvancedConditions != null)
            {
                var applied = Header.AdvancedConditions.Where(x => x != null).ToList();
                for (var i = 0; i < Math.Min(2, applied.Count); i++)
                {
                    slots[i] = new TableFilterCondition
                    {
                        Operator = applied[i].Operator,
                        Value = applied[i].Value,
                        Relation = applied[i].Relation
                    };
                }
            }
        }

        protected static string OperatorKey(TableFilterOperator op) => op.ToString().ToLowerInvariant();

        /// <summary>
        /// 运算符文案。EP 没有运算符词条，这 12 条是本库扩展键。
        /// 每条都写完整的字面量键，不用 <c>"el.table.operator." + 名称</c> 拼接：
        /// LangResourceGuard 靠静态扫描引号里的键名核对两份语言包，拼接写法它只看到前缀，会把整族报成缺键。
        /// </summary>
        protected string LabelOf(TableFilterOperator op)
        {
            switch (op)
            {
                case TableFilterOperator.NotContains: return Lang.T("el.table.operator.notContains", "不包含");
                case TableFilterOperator.StartsWith: return Lang.T("el.table.operator.startsWith", "开头是");
                case TableFilterOperator.EndsWith: return Lang.T("el.table.operator.endsWith", "结尾是");
                case TableFilterOperator.Equal: return Lang.T("el.table.operator.equal", "等于");
                case TableFilterOperator.NotEqual: return Lang.T("el.table.operator.notEqual", "不等于");
                case TableFilterOperator.Greater: return Lang.T("el.table.operator.greater", "大于");
                case TableFilterOperator.GreaterOrEqual: return Lang.T("el.table.operator.greaterOrEqual", "大于等于");
                case TableFilterOperator.Less: return Lang.T("el.table.operator.less", "小于");
                case TableFilterOperator.LessOrEqual: return Lang.T("el.table.operator.lessOrEqual", "小于等于");
                case TableFilterOperator.Empty: return Lang.T("el.table.operator.empty", "为空");
                case TableFilterOperator.NotEmpty: return Lang.T("el.table.operator.notEmpty", "不为空");
                default: return Lang.T("el.table.operator.contains", "包含");
            }
        }

        protected bool IsSingleActive(TableFilterOption option)
        {
            return draft.Count > 0 && draft.Contains(option.Value);
        }

        protected bool NeedsValue(TableFilterCondition condition)
        {
            return condition != null
                   && condition.Operator != TableFilterOperator.Empty
                   && condition.Operator != TableFilterOperator.NotEmpty;
        }

        protected string OperatorValue(TableFilterCondition condition) => OperatorKey(condition.Operator);

        protected string RelationValue(TableFilterCondition condition)
            => condition.Relation == TableFilterRelation.Or ? "or" : "and";

        /// <summary>关系下拉的显示文本。<b>必须显式传给 ElSelect 的 Label</b>：ElOption 只在下拉首次展开时才挂载注册，
        /// 光靠 Value 匹配不出显示文本，输入框会一直空白（运算符下拉同一个坑，实测空白到点开下拉才回填）。</summary>
        protected string RelationText(TableFilterCondition condition)
            => condition.Relation == TableFilterRelation.Or ? RelationOrLabel : RelationAndLabel;

        private async Task CommitAsync()
        {
            if (Committed.HasDelegate)
            {
                await Committed.InvokeAsync(Header);
            }
        }

        private async Task CloseAsync()
        {
            if (Closed.HasDelegate)
            {
                await Closed.InvokeAsync();
            }
        }

        /// <summary>
        /// 勾选只改草稿：EP 的行集要等「筛选」（<c>confirmFilter</c> → <c>store.commit('filterChange')</c>）才重算，
        /// 勾一半就缩表会让用户找不到刚勾的项。
        /// </summary>
        private void ToggleOption(TableFilterOption option, Status status)
        {
            if (status == Status.Checked)
            {
                draft.Add(option.Value);
            }
            else
            {
                draft.Remove(option.Value);
            }
        }

        private async Task ConfirmAsync()
        {
            await ApplyDraftAsync();
            await CloseAsync();
        }

        private async Task ResetDraftAsync()
        {
            draft.Clear();
            await ApplyDraftAsync();
            await CloseAsync();
        }

        private async Task ApplyDraftAsync()
        {
            if (Header != null)
            {
                Header.FilterValues = new HashSet<object>(draft);
            }
            await CommitAsync();
        }

        private async Task ClearSingleAsync()
        {
            draft.Clear();
            await ApplyDraftAsync();
            await CloseAsync();
        }

        private async Task SelectSingleAsync(TableFilterOption option)
        {
            draft.Clear();
            draft.Add(option.Value);
            await ApplyDraftAsync();
            await CloseAsync();
        }

        private void OperatorChanged(int index, string key)
        {
            if (TryParseOperator(key, out var op))
            {
                slots[index].Operator = op;
                if (!NeedsValue(slots[index]))
                {
                    slots[index].Value = null;
                }
            }
        }

        private void RelationChanged(int index, string key)
        {
            slots[index].Relation = key == "or" ? TableFilterRelation.Or : TableFilterRelation.And;
        }

        private void ConditionValueChanged(int index, string value)
        {
            slots[index].Value = value;
        }

        private async Task ApplyAdvancedAsync()
        {
            await ApplySlotsAsync();
            await CloseAsync();
        }

        private async Task ClearAdvancedAsync()
        {
            slots = new[] { new TableFilterCondition(), new TableFilterCondition() };
            if (Header != null)
            {
                Header.AdvancedConditions = new List<TableFilterCondition>();
            }
            await CommitAsync();
        }

        private async Task ApplySlotsAsync()
        {
            if (Header != null)
            {
                Header.AdvancedConditions = slots.Where(x => x.IsActive).Select(x => new TableFilterCondition
                {
                    Operator = x.Operator,
                    Value = x.Value,
                    Relation = x.Relation
                }).ToList();
            }
            await CommitAsync();
        }

        private static bool TryParseOperator(string key, out TableFilterOperator op)
        {
            return Enum.TryParse(key, true, out op);
        }
    }
}
