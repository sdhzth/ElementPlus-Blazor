using System;
using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// <see cref="ElPivotDataGridGrow{TItem}"/> 的可本地化文案集合。
    /// <para>
    /// 参考实现把 30 余条文案拆成 30 余个独立组件参数（<c>ApplyText</c>/<c>ClearText</c>/<c>EqualsText</c>…），
    /// 这里收敛为单个 <c>Strings</c> 参数的一个对象，避免组件参数表膨胀；
    /// 需要改文案时整体替换或逐项覆盖即可。
    /// </para>
    /// </summary>
    public sealed class ElPivotDataGridGrowStrings
    {
        /// <summary>无数据提示。</summary>
        public string EmptyText { get; set; } = "暂无数据";

        /// <summary>未配置值字段时的提示。</summary>
        public string NoAggregatesText { get; set; } = "请至少添加一个值字段";

        /// <summary>未配置行字段与列字段时的提示。</summary>
        public string NoFieldsText { get; set; } = "请至少添加一个行字段或列字段";

        /// <summary>加载中提示（无障碍文本）。</summary>
        public string LoadingText { get; set; } = "加载中";

        /// <summary>「其他」分组的默认标题。</summary>
        public string OthersText { get; set; } = "其他";

        /// <summary>分组键为 <c>null</c> 时的显示标题。</summary>
        public string NullGroupText { get; set; } = "（空）";

        /// <summary>字段选择器标题。</summary>
        public string FieldsPickerText { get; set; } = "字段选择";

        /// <summary>行字段分区标题。</summary>
        public string RowsText { get; set; } = "行";

        /// <summary>列字段分区标题。</summary>
        public string ColumnsText { get; set; } = "列";

        /// <summary>值字段分区标题。</summary>
        public string AggregatesText { get; set; } = "值";

        /// <summary>筛选面板标题。</summary>
        public string FilterText { get; set; } = "筛选";

        /// <summary>应用按钮文案。</summary>
        public string ApplyText { get; set; } = "应用";

        /// <summary>清除按钮文案。</summary>
        public string ClearText { get; set; } = "清除";

        /// <summary>关闭按钮无障碍文本。</summary>
        public string CloseText { get; set; } = "关闭";

        /// <summary>筛选值输入框占位文本。</summary>
        public string FilterValuePlaceholder { get; set; } = "筛选值";

        /// <summary>第二个筛选值输入框占位文本。</summary>
        public string SecondFilterValuePlaceholder { get; set; } = "第二个筛选值";

        /// <summary>升序按钮标题（同时也是排序状态的无障碍文本）。</summary>
        public string SortAscendingText { get; set; } = "升序";

        /// <summary>降序按钮标题。</summary>
        public string SortDescendingText { get; set; } = "降序";

        /// <summary>取消排序按钮标题。</summary>
        public string SortNoneText { get; set; } = "取消排序";

        /// <summary>展开分组按钮标题。</summary>
        public string ExpandText { get; set; } = "展开";

        /// <summary>折叠分组按钮标题。</summary>
        public string CollapseText { get; set; } = "折叠";

        /// <summary>总计文案（右下角单元格的表头提示）。</summary>
        public string TotalText { get; set; } = "总计";

        /// <summary>上一页按钮标题。</summary>
        public string PreviousPageText { get; set; } = "上一页";

        /// <summary>下一页按钮标题。</summary>
        public string NextPageText { get; set; } = "下一页";

        /// <summary>分页摘要格式，<c>{0}</c> 为总行数，<c>{1}</c> 为当前页行数。</summary>
        public string PagerSummaryFormat { get; set; } = "共 {0} 行";

        /// <summary>每页条数选择的无障碍文本。</summary>
        public string PageSizeText { get; set; } = "每页行数";

        /// <summary>值为 <c>null</c> 时单元格显示的占位符。</summary>
        public string NullValueText { get; set; } = string.Empty;

        /// <summary>
        /// 创建一份拷贝（避免组件把外部传入的实例就地改动）。
        /// </summary>
        public ElPivotDataGridGrowStrings Clone()
        {
            return new ElPivotDataGridGrowStrings
            {
                EmptyText = EmptyText,
                NoAggregatesText = NoAggregatesText,
                NoFieldsText = NoFieldsText,
                LoadingText = LoadingText,
                OthersText = OthersText,
                NullGroupText = NullGroupText,
                FieldsPickerText = FieldsPickerText,
                RowsText = RowsText,
                ColumnsText = ColumnsText,
                AggregatesText = AggregatesText,
                FilterText = FilterText,
                ApplyText = ApplyText,
                ClearText = ClearText,
                CloseText = CloseText,
                FilterValuePlaceholder = FilterValuePlaceholder,
                SecondFilterValuePlaceholder = SecondFilterValuePlaceholder,
                SortAscendingText = SortAscendingText,
                SortDescendingText = SortDescendingText,
                SortNoneText = SortNoneText,
                ExpandText = ExpandText,
                CollapseText = CollapseText,
                TotalText = TotalText,
                PreviousPageText = PreviousPageText,
                NextPageText = NextPageText,
                PagerSummaryFormat = PagerSummaryFormat,
                PageSizeText = PageSizeText,
                NullValueText = NullValueText
            };
        }

        /// <summary>
        /// 默认文案（中文）。
        /// </summary>
        public static ElPivotDataGridGrowStrings Default
        {
            get { return new ElPivotDataGridGrowStrings(); }
        }

        /// <summary>
        /// 按状态码取文案。用于把「空数据集 / 缺字段 / 缺值字段」这三种结构性状态统一成一个可翻译入口。
        /// </summary>
        public static string Resolve(string code)
        {
            var strings = Default;

            switch (code)
            {
                case "pivot.empty": return strings.EmptyText;
                case "pivot.noAggregates": return strings.NoAggregatesText;
                case "pivot.noFields": return strings.NoFieldsText;
                case "pivot.loading": return strings.LoadingText;
                default: return code;
            }
        }

        /// <summary>
        /// 全部状态码（供测试与文档使用）。
        /// </summary>
        public static IReadOnlyList<string> Codes
        {
            get
            {
                return new[]
                {
                    "pivot.empty",
                    "pivot.noAggregates",
                    "pivot.noFields",
                    "pivot.loading"
                };
            }
        }
    }

    /// <summary>
    /// 筛选面板的工作上下文，作为 <c>FilterTemplate</c> / <c>FilterValueTemplate</c> /
    /// <c>SecondFilterValueTemplate</c> 的模板参数。
    /// <para>
    /// 与参考实现直接传 <c>RadzenPivotField&lt;TItem&gt;</c> 不同：这里只在模板里暴露
    /// 「需要的值 + 变更回调」，模板不需要（也无法）触达内部状态，因此可以脱离组件单测渲染结果。
    /// </para>
    /// </summary>
    public sealed class ElPivotFilterContext
    {
        /// <summary>被筛选的属性路径。</summary>
        public string Property { get; internal set; }

        /// <summary>字段标题。</summary>
        public string Title { get; internal set; }

        /// <summary>属性类型（决定可用运算符与取值控件）。</summary>
        public Type Type { get; internal set; }

        /// <summary>当前第一个条件的运算符。</summary>
        public ElPivotFilterOperator Operator { get; internal set; }

        /// <summary>当前第一个条件的比较值。</summary>
        public object Value { get; internal set; }

        /// <summary>当前第二个条件的运算符。</summary>
        public ElPivotFilterOperator SecondOperator { get; internal set; }

        /// <summary>当前第二个条件的比较值。</summary>
        public object SecondValue { get; internal set; }

        /// <summary>当前逻辑运算符。</summary>
        public ElPivotLogicalOperator LogicalOperator { get; internal set; }

        /// <summary>该属性类型可用的运算符集合。</summary>
        public IReadOnlyList<ElPivotFilterOperator> Operators { get; internal set; } = Array.Empty<ElPivotFilterOperator>();

        /// <summary>该属性类型可用的逻辑运算符集合（始终为「与 / 或」）。</summary>
        public IReadOnlyList<ElPivotLogicalOperator> LogicalOperators { get; internal set; } = Array.Empty<ElPivotLogicalOperator>();

        /// <summary>设置第一个条件的运算符。</summary>
        public Action<ElPivotFilterOperator> SetOperator { get; internal set; }

        /// <summary>设置第一个条件的比较值。</summary>
        public Action<object> SetValue { get; internal set; }

        /// <summary>设置第二个条件的运算符。</summary>
        public Action<ElPivotFilterOperator> SetSecondOperator { get; internal set; }

        /// <summary>设置第二个条件的比较值。</summary>
        public Action<object> SetSecondValue { get; internal set; }

        /// <summary>设置逻辑运算符。</summary>
        public Action<ElPivotLogicalOperator> SetLogicalOperator { get; internal set; }

        /// <summary>立即应用筛选（通常由内置的「应用」按钮调用）。</summary>
        public Func<System.Threading.Tasks.Task> Apply { get; internal set; }

        /// <summary>清空筛选（通常由内置的「清除」按钮调用）。</summary>
        public Func<System.Threading.Tasks.Task> Clear { get; internal set; }

        /// <summary>
        /// 便捷方法：把第一个条件的运算符与值一次性设置好。
        /// </summary>
        public void SetCondition(ElPivotFilterOperator op, object value)
        {
            if (SetOperator != null)
            {
                SetOperator(op);
            }

            if (SetValue != null)
            {
                SetValue(value);
            }
        }
    }
}
