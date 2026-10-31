using System;
using System.Collections.Generic;
using System.Globalization;

namespace ElementPlus
{
    /// <summary>条件式筛选的运算符。文本类与有序类各取所需，面板按列的属性类型给候选。</summary>
    public enum TableFilterOperator
    {
        Contains,
        NotContains,
        StartsWith,
        EndsWith,
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        Empty,
        NotEmpty
    }

    /// <summary>一条条件与它前一条的关系（第一条不用）。</summary>
    public enum TableFilterRelation
    {
        And,
        Or
    }

    /// <summary>
    /// 列筛选面板里的一条条件（本库扩展，EP 的列筛选只有选项列表，没有运算符表达式）。
    /// </summary>
    public class TableFilterCondition
    {
        public TableFilterOperator Operator { get; set; } = TableFilterOperator.Contains;

        /// <summary>运算符的右值，按字符串录入，比较时才换算成单元格值的类型。</summary>
        public string Value { get; set; }

        /// <summary>与上一条条件的关系；第一条忽略。</summary>
        public TableFilterRelation Relation { get; set; } = TableFilterRelation.And;

        /// <summary>为空 / 不为空不需要右值，其余运算符右值为空就不算一条生效条件（避免空输入把表筛成空）。</summary>
        public bool IsActive => Operator == TableFilterOperator.Empty
                                || Operator == TableFilterOperator.NotEmpty
                                || !string.IsNullOrWhiteSpace(Value);

        /// <summary>单元格值是否命中本条件。未生效的条件不参与判断，按命中处理。</summary>
        public bool Matches(object cellValue)
        {
            if (!IsActive)
            {
                return true;
            }

            var text = Convert.ToString(cellValue, CultureInfo.InvariantCulture) ?? string.Empty;
            var hasText = cellValue != null && text.Length > 0;
            switch (Operator)
            {
                case TableFilterOperator.Empty:
                    return !hasText;
                case TableFilterOperator.NotEmpty:
                    return hasText;
                case TableFilterOperator.Contains:
                    return Text(text).Contains(Text(Value), StringComparison.CurrentCultureIgnoreCase);
                case TableFilterOperator.NotContains:
                    return !Text(text).Contains(Text(Value), StringComparison.CurrentCultureIgnoreCase);
                case TableFilterOperator.StartsWith:
                    return Text(text).StartsWith(Text(Value), StringComparison.CurrentCultureIgnoreCase);
                case TableFilterOperator.EndsWith:
                    return Text(text).EndsWith(Text(Value), StringComparison.CurrentCultureIgnoreCase);
                case TableFilterOperator.Equal:
                    return Compare(cellValue, text, Value) == 0;
                case TableFilterOperator.NotEqual:
                    return Compare(cellValue, text, Value) != 0;
                case TableFilterOperator.Greater:
                    return Compare(cellValue, text, Value) > 0;
                case TableFilterOperator.GreaterOrEqual:
                    return Compare(cellValue, text, Value) >= 0;
                case TableFilterOperator.Less:
                    return Compare(cellValue, text, Value) < 0;
                case TableFilterOperator.LessOrEqual:
                    return Compare(cellValue, text, Value) <= 0;
                default:
                    return true;
            }
        }

        private static string Text(string value) => value ?? string.Empty;

        /// <summary>
        /// 先按单元格自己的类型把右值换算过去再比（数字、日期、布尔各有自己的排序语义），
        /// 换算不动就退回字符串比较 —— 录入框里永远是字符串，不能要求用户写对格式。
        /// </summary>
        private static int Compare(object cellValue, string cellText, string value)
        {
            if (cellValue is IComparable comparable)
            {
                var targetType = Nullable.GetUnderlyingType(cellValue.GetType()) ?? cellValue.GetType();
                if (targetType == typeof(string))
                {
                    return string.Compare(cellText, value, StringComparison.CurrentCultureIgnoreCase);
                }
                try
                {
                    // 右值留空时拿该类型的默认值比（数字即 0），与用户「清空输入框就是筛 0」的直觉一致
                    var typed = string.IsNullOrWhiteSpace(value)
                        ? Activator.CreateInstance(targetType)
                        : Convert.ChangeType(value.Trim(), targetType, CultureInfo.CurrentCulture);
                    return comparable.CompareTo(typed);
                }
                catch (Exception)
                {
                    // 右值填了「abc」而列是数字这类情况：不报错，退回字符串比较
                }
            }

            return string.Compare(cellText, Text(value), StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>文本列给的运算符候选。</summary>
        internal static IReadOnlyList<TableFilterOperator> TextOperators { get; } = new[]
        {
            TableFilterOperator.Contains,
            TableFilterOperator.NotContains,
            TableFilterOperator.StartsWith,
            TableFilterOperator.EndsWith,
            TableFilterOperator.Equal,
            TableFilterOperator.NotEqual,
            TableFilterOperator.Empty,
            TableFilterOperator.NotEmpty
        };

        /// <summary>数字 / 日期 / 布尔等可排序列给的运算符候选。</summary>
        internal static IReadOnlyList<TableFilterOperator> OrderedOperators { get; } = new[]
        {
            TableFilterOperator.Equal,
            TableFilterOperator.NotEqual,
            TableFilterOperator.Greater,
            TableFilterOperator.GreaterOrEqual,
            TableFilterOperator.Less,
            TableFilterOperator.LessOrEqual,
            TableFilterOperator.Empty,
            TableFilterOperator.NotEmpty
        };

        /// <summary>按列属性类型选候选；string 与未知类型按文本处理。</summary>
        internal static IReadOnlyList<TableFilterOperator> OperatorsFor(Type propertyType)
        {
            if (propertyType == null || propertyType == typeof(string))
            {
                return TextOperators;
            }

            var underlying = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            if (underlying == typeof(bool) || typeof(IComparable).IsAssignableFrom(underlying))
            {
                return OrderedOperators;
            }

            return TextOperators;
        }
    }
}
