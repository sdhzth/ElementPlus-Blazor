using System;
using System.Collections.Generic;
using System.Globalization;

namespace ElementPlus
{
    /// <summary>
    /// 聚合计算器：把「一组记录 + 一个取值委托」折算成单个聚合值。
    /// <para>
    /// 单趟遍历完成所有聚合函数（参考实现按函数调用 <c>IQueryable.Sum/Average/Min/Max</c>，
    /// 且为每个单元格重建一次查询；这里改为一次遍历直接算出结果，避免 N 次枚举）。
    /// </para>
    /// <para>
    /// 返回类型约定（便于测试与显示都可预期）：
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="ElPivotAggregateFunction.Count"/> / <see cref="ElPivotAggregateFunction.CountDistinct"/> → <see cref="int"/></description></item>
    /// <item><description><see cref="ElPivotAggregateFunction.Sum"/> → 属性为 <see cref="decimal"/> 时返回 <see cref="decimal"/>；含浮点时返回 <see cref="double"/>；否则返回 <see cref="long"/></description></item>
    /// <item><description><see cref="ElPivotAggregateFunction.Average"/> → <see cref="double"/></description></item>
    /// <item><description><see cref="ElPivotAggregateFunction.Min"/> / <see cref="ElPivotAggregateFunction.Max"/> / <see cref="ElPivotAggregateFunction.First"/> / <see cref="ElPivotAggregateFunction.Last"/> → 原始值（保留原类型）</description></item>
    /// <item><description>空分组 → <c>null</c></description></item>
    /// <item><description>非数值属性上的 <see cref="ElPivotAggregateFunction.Sum"/> / <see cref="ElPivotAggregateFunction.Average"/> → 记录条数（与参考实现一致）</description></item>
    /// </list>
    /// </summary>
    public static class ElPivotAggregator
    {
        /// <summary>
        /// 计算聚合值。
        /// </summary>
        /// <typeparam name="TItem">记录类型。</typeparam>
        /// <param name="items">参与计算的记录（可为空集合）。</param>
        /// <param name="getter">取值委托；<c>null</c> 表示只做计数类聚合。</param>
        /// <param name="function">聚合函数。</param>
        /// <param name="propertyType">属性静态类型，用于判断是否数值型。</param>
        public static object Compute<TItem>(IReadOnlyList<TItem> items, Func<object, object> getter, ElPivotAggregateFunction function, Type propertyType)
        {
            if (items == null || items.Count == 0)
            {
                return null;
            }

            if (function == ElPivotAggregateFunction.Count)
            {
                return items.Count;
            }

            if (getter == null)
            {
                // 无取值委托时，只有计数类聚合有意义，其余按参考实现退化为计数。
                return items.Count;
            }

            if (function == ElPivotAggregateFunction.First)
            {
                return getter(items[0]);
            }

            if (function == ElPivotAggregateFunction.Last)
            {
                return getter(items[items.Count - 1]);
            }

            var isNumeric = propertyType != null && ElPivotPropertyAccess.IsNumeric(propertyType);
            var useDecimal = ElPivotPropertyAccess.UnwrapNullable(propertyType) == typeof(decimal);

            if (function == ElPivotAggregateFunction.CountDistinct)
            {
                var seen = new HashSet<object>(ElPivotPropertyAccess.GroupKeyComparer.Instance);
                for (var i = 0; i < items.Count; i++)
                {
                    var value = getter(items[i]);
                    if (value != null)
                    {
                        seen.Add(value);
                    }
                }

                return seen.Count;
            }

            if (function == ElPivotAggregateFunction.Min || function == ElPivotAggregateFunction.Max)
            {
                object best = null;
                var hasBest = false;

                for (var i = 0; i < items.Count; i++)
                {
                    var value = getter(items[i]);
                    if (value == null)
                    {
                        continue;
                    }

                    if (!hasBest)
                    {
                        best = value;
                        hasBest = true;
                        continue;
                    }

                    if (!isNumeric)
                    {
                        // 非数值属性：与参考实现一致，Min 取第一条、Max 取最后一条。
                        if (function == ElPivotAggregateFunction.Max)
                        {
                            best = value;
                        }

                        continue;
                    }

                    var comparison = ElPivotPropertyAccess.CompareValues(value, best);
                    if (function == ElPivotAggregateFunction.Min ? comparison < 0 : comparison > 0)
                    {
                        best = value;
                    }
                }

                return best;
            }

            if (!isNumeric)
            {
                // 非数值属性上的 Sum/Average：与参考实现一致，退化为计数。
                return items.Count;
            }

            if (function == ElPivotAggregateFunction.Average)
            {
                var sum = 0d;
                var counted = 0;

                for (var i = 0; i < items.Count; i++)
                {
                    if (TryToDouble(getter(items[i]), out var value))
                    {
                        sum += value;
                        counted++;
                    }
                }

                return counted == 0 ? null : sum / counted;
            }

            // Sum
            if (useDecimal)
            {
                var sum = 0m;
                for (var i = 0; i < items.Count; i++)
                {
                    if (TryToDecimal(getter(items[i]), out var value))
                    {
                        sum += value;
                    }
                }

                return sum;
            }

            var floating = IsFloatingType(propertyType);
            if (floating)
            {
                var sum = 0d;
                for (var i = 0; i < items.Count; i++)
                {
                    if (TryToDouble(getter(items[i]), out var value))
                    {
                        sum += value;
                    }
                }

                return sum;
            }

            var integral = 0L;
            for (var i = 0; i < items.Count; i++)
            {
                if (TryToDouble(getter(items[i]), out var value))
                {
                    integral += (long)Math.Round(value, MidpointRounding.AwayFromZero);
                }
            }

            return integral;
        }

        /// <summary>
        /// 把聚合值格式化为显示文本。
        /// </summary>
        /// <param name="value">聚合值。</param>
        /// <param name="formatString">格式字符串（可为 <c>null</c>）。</param>
        /// <param name="formatProvider">格式提供程序；为 <c>null</c> 时使用 <see cref="CultureInfo.CurrentCulture"/>。</param>
        public static string FormatValue(object value, string formatString, IFormatProvider formatProvider)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(formatString) && value is IFormattable formattable)
            {
                try
                {
                    return formattable.ToString(formatString, formatProvider ?? CultureInfo.CurrentCulture);
                }
                catch (FormatException)
                {
                    // 格式串与值类型不兼容时退化为默认 ToString。
                }
            }

            return value.ToString();
        }

        private static bool IsFloatingType(Type type)
        {
            var t = ElPivotPropertyAccess.UnwrapNullable(type);

            if (t == null)
            {
                return false;
            }

            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Single:
                case TypeCode.Double:
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryToDouble(object value, out double result)
        {
            result = 0d;

            if (value == null)
            {
                return false;
            }

            try
            {
                result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryToDecimal(object value, out decimal result)
        {
            result = 0m;

            if (value == null)
            {
                return false;
            }

            try
            {
                result = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
