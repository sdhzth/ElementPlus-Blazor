using System;
using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// 排序方向。对应参考实现中的 <c>Radzen.SortOrder</c>（此处自写，不引用 Radzen）。
    /// </summary>
    public enum ElPivotSortOrder
    {
        /// <summary>升序。</summary>
        Ascending,
        /// <summary>降序。</summary>
        Descending
    }

    /// <summary>
    /// 值字段（度量）的聚合函数。
    /// </summary>
    public enum ElPivotAggregateFunction
    {
        /// <summary>求和（仅数值类型；非数值类型退化为计数，与参考实现一致）。</summary>
        Sum,
        /// <summary>平均值（仅数值类型；非数值类型退化为计数）。</summary>
        Average,
        /// <summary>计数（统计分组内的记录条数）。</summary>
        Count,
        /// <summary>去重计数（统计分组内该属性不同取值的个数，忽略 <c>null</c>）。</summary>
        CountDistinct,
        /// <summary>最小值（仅数值类型；非数值类型取第一条记录的值）。</summary>
        Min,
        /// <summary>最大值（仅数值类型；非数值类型取最后一条记录的值）。</summary>
        Max,
        /// <summary>第一条记录的值。</summary>
        First,
        /// <summary>最后一条记录的值。</summary>
        Last
    }

    /// <summary>
    /// 字段筛选运算符。
    /// </summary>
    public enum ElPivotFilterOperator
    {
        /// <summary>等于。</summary>
        Equals,
        /// <summary>不等于。</summary>
        NotEquals,
        /// <summary>小于。</summary>
        LessThan,
        /// <summary>小于等于。</summary>
        LessThanOrEquals,
        /// <summary>大于。</summary>
        GreaterThan,
        /// <summary>大于等于。</summary>
        GreaterThanOrEquals,
        /// <summary>包含。</summary>
        Contains,
        /// <summary>不包含。</summary>
        DoesNotContain,
        /// <summary>以…开头。</summary>
        StartsWith,
        /// <summary>以…结尾。</summary>
        EndsWith,
        /// <summary>在集合中（值以逗号分隔）。</summary>
        In,
        /// <summary>不在集合中（值以逗号分隔）。</summary>
        NotIn,
        /// <summary>为空（null）。</summary>
        IsNull,
        /// <summary>不为空（null）。</summary>
        IsNotNull,
        /// <summary>为空字符串或空白。</summary>
        IsEmpty,
        /// <summary>不为空字符串或空白。</summary>
        IsNotEmpty,
        /// <summary>自定义（由 <c>FilterTemplate</c> 提供 UI，本组件不做任何匹配）。</summary>
        Custom
    }

    /// <summary>
    /// 逻辑运算符：用于组合同一字段的两个条件，以及组合多个字段之间的条件。
    /// </summary>
    public enum ElPivotLogicalOperator
    {
        /// <summary>与。</summary>
        And,
        /// <summary>或。</summary>
        Or
    }

    /// <summary>
    /// 单元格文本对齐方式。
    /// </summary>
    public enum ElPivotTextAlign
    {
        /// <summary>左对齐。</summary>
        Left,
        /// <summary>居中。</summary>
        Center,
        /// <summary>右对齐。</summary>
        Right
    }

    /// <summary>
    /// 表格网格线样式。对应参考实现的 <c>DataGridGridLines</c>。
    /// </summary>
    public enum ElPivotGridLines
    {
        /// <summary>默认（仅横线）。</summary>
        Default,
        /// <summary>全网格线。</summary>
        Both,
        /// <summary>无网格线。</summary>
        None,
        /// <summary>仅横线。</summary>
        Horizontal,
        /// <summary>仅竖线。</summary>
        Vertical
    }

    /// <summary>
    /// 分页器显示位置。
    /// </summary>
    [Flags]
    public enum ElPivotPagerPosition
    {
        /// <summary>不显示。</summary>
        None = 0,
        /// <summary>表格上方。</summary>
        Top = 1,
        /// <summary>表格下方。</summary>
        Bottom = 2,
        /// <summary>上下都显示。</summary>
        All = Top | Bottom
    }

    /// <summary>
    /// 筛选时字符串比较的大小写敏感性。
    /// </summary>
    public enum ElPivotFilterCaseSensitivity
    {
        /// <summary>默认（按 <see cref="ElPivotFilterDescriptor.CaseSensitivity"/> 的默认设置，不区分大小写）。</summary>
        Default,
        /// <summary>区分大小写。</summary>
        CaseSensitive,
        /// <summary>不区分大小写。</summary>
        CaseInsensitive
    }

    /// <summary>
    /// 聚合函数的元数据与取值规则（单一真源，组件与测试共用）。
    /// </summary>
    public static class ElPivotAggregateFunctions
    {
        private static readonly ElPivotAggregateFunction[] all =
        {
            ElPivotAggregateFunction.Sum,
            ElPivotAggregateFunction.Average,
            ElPivotAggregateFunction.Count,
            ElPivotAggregateFunction.CountDistinct,
            ElPivotAggregateFunction.Min,
            ElPivotAggregateFunction.Max,
            ElPivotAggregateFunction.First,
            ElPivotAggregateFunction.Last
        };

        private static readonly ElPivotAggregateFunction[] numericOnly =
        {
            ElPivotAggregateFunction.Sum,
            ElPivotAggregateFunction.Average,
            ElPivotAggregateFunction.Min,
            ElPivotAggregateFunction.Max
        };

        /// <summary>全部聚合函数，按声明顺序。</summary>
        public static IReadOnlyList<ElPivotAggregateFunction> All => all;

        /// <summary>仅适用于数值属性的聚合函数。</summary>
        public static IReadOnlyList<ElPivotAggregateFunction> NumericOnly => numericOnly;

        /// <summary>
        /// 判断聚合函数是否仅适用于数值属性。
        /// </summary>
        public static bool IsNumericOnly(ElPivotAggregateFunction function)
        {
            foreach (var item in numericOnly)
            {
                if (item == function)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 返回指定属性类型可用的聚合函数：数值类型返回全部，其余类型排除仅数值可用的函数。
        /// </summary>
        public static List<ElPivotAggregateFunction> Available(Type propertyType)
        {
            var result = new List<ElPivotAggregateFunction>(all.Length);

            var numeric = propertyType != null && ElPivotPropertyAccess.IsNumeric(propertyType);

            foreach (var item in all)
            {
                if (!numeric && IsNumericOnly(item))
                {
                    continue;
                }

                result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// 返回指定属性类型可用的聚合函数的默认值（与 <see cref="Available"/> 的首项一致）。
        /// </summary>
        public static ElPivotAggregateFunction DefaultFor(Type propertyType)
        {
            var available = Available(propertyType);
            return available.Count > 0 ? available[0] : ElPivotAggregateFunction.Count;
        }

        /// <summary>
        /// 聚合函数的中文显示名（也是字段选择器的下拉文案）。
        /// </summary>
        public static string DisplayName(ElPivotAggregateFunction function)
        {
            switch (function)
            {
                case ElPivotAggregateFunction.Sum: return "求和";
                case ElPivotAggregateFunction.Average: return "平均值";
                case ElPivotAggregateFunction.Count: return "计数";
                case ElPivotAggregateFunction.CountDistinct: return "去重计数";
                case ElPivotAggregateFunction.Min: return "最小值";
                case ElPivotAggregateFunction.Max: return "最大值";
                case ElPivotAggregateFunction.First: return "首个";
                case ElPivotAggregateFunction.Last: return "末个";
                default: return function.ToString();
            }
        }
    }

    /// <summary>
    /// 筛选运算符的元数据与按属性类型的可用集合（单一真源，组件与测试共用）。
    /// </summary>
    public static class ElPivotFilterOperators
    {
        private static readonly ElPivotFilterOperator[] numeric =
        {
            ElPivotFilterOperator.Equals,
            ElPivotFilterOperator.NotEquals,
            ElPivotFilterOperator.LessThan,
            ElPivotFilterOperator.LessThanOrEquals,
            ElPivotFilterOperator.GreaterThan,
            ElPivotFilterOperator.GreaterThanOrEquals,
            ElPivotFilterOperator.IsNull,
            ElPivotFilterOperator.IsNotNull
        };

        private static readonly ElPivotFilterOperator[] textual =
        {
            ElPivotFilterOperator.Contains,
            ElPivotFilterOperator.DoesNotContain,
            ElPivotFilterOperator.StartsWith,
            ElPivotFilterOperator.EndsWith,
            ElPivotFilterOperator.Equals,
            ElPivotFilterOperator.NotEquals,
            ElPivotFilterOperator.In,
            ElPivotFilterOperator.NotIn,
            ElPivotFilterOperator.IsNull,
            ElPivotFilterOperator.IsNotNull,
            ElPivotFilterOperator.IsEmpty,
            ElPivotFilterOperator.IsNotEmpty
        };

        private static readonly ElPivotFilterOperator[] boolean =
        {
            ElPivotFilterOperator.Equals,
            ElPivotFilterOperator.NotEquals,
            ElPivotFilterOperator.IsNull,
            ElPivotFilterOperator.IsNotNull
        };

        private static readonly ElPivotFilterOperator[] enumerated =
        {
            ElPivotFilterOperator.Equals,
            ElPivotFilterOperator.NotEquals,
            ElPivotFilterOperator.In,
            ElPivotFilterOperator.NotIn,
            ElPivotFilterOperator.IsNull,
            ElPivotFilterOperator.IsNotNull
        };

        /// <summary>
        /// 判断运算符是否需要用户输入一个筛选值。
        /// </summary>
        public static bool RequiresValue(ElPivotFilterOperator op)
        {
            switch (op)
            {
                case ElPivotFilterOperator.IsNull:
                case ElPivotFilterOperator.IsNotNull:
                case ElPivotFilterOperator.IsEmpty:
                case ElPivotFilterOperator.IsNotEmpty:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// 判断运算符是否为「一元」运算符（不需要筛选值，UI 应禁用输入框）。
        /// </summary>
        public static bool IsUnary(ElPivotFilterOperator op)
        {
            return !RequiresValue(op);
        }

        /// <summary>
        /// 返回指定属性类型可用的筛选运算符集合。
        /// </summary>
        public static List<ElPivotFilterOperator> For(Type propertyType)
        {
            var type = ElPivotPropertyAccess.UnwrapNullable(propertyType ?? typeof(string));

            if (type == typeof(bool))
            {
                return new List<ElPivotFilterOperator>(boolean);
            }

            if (ElPivotPropertyAccess.IsNumeric(type))
            {
                return new List<ElPivotFilterOperator>(numeric);
            }

            if (ElPivotPropertyAccess.IsDate(type) || type == typeof(TimeSpan) || type == typeof(TimeOnly) || type == typeof(DateOnly))
            {
                return new List<ElPivotFilterOperator>(numeric);
            }

            if (type.IsEnum)
            {
                return new List<ElPivotFilterOperator>(enumerated);
            }

            return new List<ElPivotFilterOperator>(textual);
        }

        /// <summary>
        /// 筛选运算符的中文显示名。
        /// </summary>
        public static string DisplayName(ElPivotFilterOperator op)
        {
            switch (op)
            {
                case ElPivotFilterOperator.Equals: return "等于";
                case ElPivotFilterOperator.NotEquals: return "不等于";
                case ElPivotFilterOperator.LessThan: return "小于";
                case ElPivotFilterOperator.LessThanOrEquals: return "小于等于";
                case ElPivotFilterOperator.GreaterThan: return "大于";
                case ElPivotFilterOperator.GreaterThanOrEquals: return "大于等于";
                case ElPivotFilterOperator.Contains: return "包含";
                case ElPivotFilterOperator.DoesNotContain: return "不包含";
                case ElPivotFilterOperator.StartsWith: return "开头是";
                case ElPivotFilterOperator.EndsWith: return "结尾是";
                case ElPivotFilterOperator.In: return "属于";
                case ElPivotFilterOperator.NotIn: return "不属于";
                case ElPivotFilterOperator.IsNull: return "为空";
                case ElPivotFilterOperator.IsNotNull: return "不为空";
                case ElPivotFilterOperator.IsEmpty: return "为空字符串";
                case ElPivotFilterOperator.IsNotEmpty: return "不为空字符串";
                case ElPivotFilterOperator.Custom: return "自定义";
                default: return op.ToString();
            }
        }

        /// <summary>
        /// 逻辑运算符的中文显示名。
        /// </summary>
        public static string DisplayName(ElPivotLogicalOperator op)
        {
            return op == ElPivotLogicalOperator.And ? "与" : "或";
        }
    }
}
