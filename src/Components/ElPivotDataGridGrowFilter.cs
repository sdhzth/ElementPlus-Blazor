using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace ElementPlus
{
    /// <summary>
    /// 筛选匹配器：把 <see cref="ElPivotFilterDescriptor"/> 翻译成对单个记录值的判定。
    /// <para>
    /// 与参考实现最大的区别：参考实现把筛选条件下沉成动态 LINQ 表达式串（<c>ToFilterString</c>）再交给
    /// <c>System.Linq.Dynamic.Core</c> 求值；这里改为**纯 C# 直接比较**，不生成也不编译任何表达式，
    /// 因而没有第三方依赖、没有表达式注入面、并且可以脱离数据源直接单测。
    /// </para>
    /// </summary>
    public static class ElPivotFilterMatcher
    {
        /// <summary>
        /// 判断一个筛选条件是否对指定值成立。
        /// </summary>
        /// <param name="value">记录上取到的原始值。</param>
        /// <param name="descriptor">筛选条件（其 <see cref="ElPivotFilterDescriptor.Property"/> 被忽略）。</param>
        public static bool Matches(object value, ElPivotFilterDescriptor descriptor)
        {
            if (descriptor == null || descriptor.IsEmpty)
            {
                return true;
            }

            if (descriptor.Operator == ElPivotFilterOperator.Custom)
            {
                // 自定义运算符完全交给 FilterTemplate / 外部逻辑，本组件不做任何过滤。
                return true;
            }

            var caseSensitive = descriptor.IsCaseSensitive;
            var type = descriptor.Type;

            var first = MatchOne(value, descriptor.Operator, descriptor.Value, type, caseSensitive);

            if (!descriptor.HasSecond)
            {
                return first;
            }

            var second = MatchOne(value, descriptor.SecondOperator, descriptor.SecondValue, type, caseSensitive);

            return descriptor.LogicalOperator == ElPivotLogicalOperator.And ? first && second : first || second;
        }

        /// <summary>
        /// 判断多条筛选条件是否同时成立（用 <paramref name="logicalOperator"/> 组合）。
        /// </summary>
        public static bool MatchesAll(object value, IReadOnlyList<ElPivotFilterDescriptor> descriptors, ElPivotLogicalOperator logicalOperator)
        {
            if (descriptors == null || descriptors.Count == 0)
            {
                return true;
            }

            if (logicalOperator == ElPivotLogicalOperator.Or)
            {
                foreach (var descriptor in descriptors)
                {
                    if (descriptor != null && !descriptor.IsEmpty && Matches(value, descriptor))
                    {
                        return true;
                    }
                }

                return false;
            }

            foreach (var descriptor in descriptors)
            {
                if (descriptor != null && !descriptor.IsEmpty && !Matches(value, descriptor))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchOne(object value, ElPivotFilterOperator op, object expected, Type type, bool caseSensitive)
        {
            switch (op)
            {
                case ElPivotFilterOperator.IsNull:
                    return value == null;

                case ElPivotFilterOperator.IsNotNull:
                    return value != null;

                case ElPivotFilterOperator.IsEmpty:
                    return IsEmpty(value);

                case ElPivotFilterOperator.IsNotEmpty:
                    return !IsEmpty(value);

                case ElPivotFilterOperator.In:
                    return MatchIn(value, expected, type, caseSensitive);

                case ElPivotFilterOperator.NotIn:
                    return !MatchIn(value, expected, type, caseSensitive);
            }

            if (expected == null)
            {
                // 需要比较值却没给值：与「等于 null」语义一致，避免误过滤掉全部数据。
                return true;
            }

            // 文本类运算符必须在数值/有序比较之前处理：
            // 它们不参与大小比较，若落到下面的 switch 会被 default 分支当成「条件不生效」而静默放行。
            if (op == ElPivotFilterOperator.Contains
                || op == ElPivotFilterOperator.DoesNotContain
                || op == ElPivotFilterOperator.StartsWith
                || op == ElPivotFilterOperator.EndsWith)
            {
                return MatchText(value, op, expected, caseSensitive);
            }

            var coerced = Coerce(expected, type);
            var comparison = Compare(value, coerced, type, caseSensitive);

            switch (op)
            {
                case ElPivotFilterOperator.Equals:
                    return comparison == 0;
                case ElPivotFilterOperator.NotEquals:
                    return comparison != 0;
                case ElPivotFilterOperator.LessThan:
                    return comparison < 0;
                case ElPivotFilterOperator.LessThanOrEquals:
                    return comparison <= 0;
                case ElPivotFilterOperator.GreaterThan:
                    return comparison > 0;
                case ElPivotFilterOperator.GreaterThanOrEquals:
                    return comparison >= 0;
                default:
                    return true;
            }
        }

        /// <summary>文本类运算符（包含 / 不包含 / 开头 / 结尾）的匹配。</summary>
        private static bool MatchText(object value, ElPivotFilterOperator op, object expected, bool caseSensitive)
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            var needle = Convert.ToString(expected, CultureInfo.InvariantCulture) ?? string.Empty;
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            switch (op)
            {
                case ElPivotFilterOperator.Contains:
                    return text.IndexOf(needle, comparison) >= 0;
                case ElPivotFilterOperator.DoesNotContain:
                    return text.IndexOf(needle, comparison) < 0;
                case ElPivotFilterOperator.StartsWith:
                    return text.StartsWith(needle, comparison);
                case ElPivotFilterOperator.EndsWith:
                    return text.EndsWith(needle, comparison);
                default:
                    return true;
            }
        }

        private static bool IsEmpty(object value)
        {
            if (value == null)
            {
                return true;
            }

            if (value is string text)
            {
                return string.IsNullOrWhiteSpace(text);
            }

            return false;
        }

        private static bool MatchIn(object value, object expected, Type type, bool caseSensitive)
        {
            foreach (var candidate in EnumerateCandidates(expected))
            {
                if (Compare(value, Coerce(candidate, type), type, caseSensitive) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<object> EnumerateCandidates(object expected)
        {
            if (expected == null)
            {
                yield break;
            }

            if (expected is string text)
            {
                foreach (var part in text.Split(','))
                {
                    var trimmed = part.Trim();
                    if (trimmed.Length > 0)
                    {
                        yield return trimmed;
                    }
                }

                yield break;
            }

            if (expected is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    yield return item;
                }

                yield break;
            }

            yield return expected;
        }

        /// <summary>
        /// 通用比较：返回负数/0/正数。字符串默认不区分大小写；数值按数值比较；日期按其自然顺序。
        /// </summary>
        public static int Compare(object value, object expected, Type type, bool caseSensitive)
        {
            if (value == null && expected == null)
            {
                return 0;
            }

            if (value == null)
            {
                return -1;
            }

            if (expected == null)
            {
                return 1;
            }

            // 字符串必须在这里就地处理：若落到下面的 IComparable 分支，会走 string.CompareTo（区域设置相关），
            // caseSensitive 参数将被完全忽略，导致「不区分大小写」的筛选悄悄变成区分大小写。
            if (value is string leftText && expected is string rightText)
            {
                return string.Compare(leftText, rightText, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            }

            var unwrapped = ElPivotPropertyAccess.UnwrapNullable(type);

            if (unwrapped != null && ElPivotPropertyAccess.IsNumeric(unwrapped))
            {
                if (TryToDouble(value, out var left) && TryToDouble(expected, out var right))
                {
                    return left.CompareTo(right);
                }
            }

            if (unwrapped != null && ElPivotPropertyAccess.IsDate(unwrapped))
            {
                if (TryToDateTime(value, out var left) && TryToDateTime(expected, out var right))
                {
                    return left.CompareTo(right);
                }
            }

            if (unwrapped == typeof(bool) && value is bool leftBool && expected is bool rightBool)
            {
                return leftBool.CompareTo(rightBool);
            }

            if (unwrapped != null && unwrapped.IsEnum)
            {
                var leftEnum = TryToEnum(value, unwrapped);
                var rightEnum = TryToEnum(expected, unwrapped);
                if (leftEnum != null && rightEnum != null)
                {
                    return Convert.ToInt64(leftEnum, CultureInfo.InvariantCulture)
                        .CompareTo(Convert.ToInt64(rightEnum, CultureInfo.InvariantCulture));
                }
            }

            if (value is IComparable comparable && value.GetType() == expected.GetType())
            {
                try
                {
                    return comparable.CompareTo(expected);
                }
                catch
                {
                    // 落到字符串比较
                }
            }

            return string.Compare(
                Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
                Convert.ToString(expected, CultureInfo.InvariantCulture) ?? string.Empty,
                caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 把用户输入（通常是 <see cref="string"/>）转换为属性类型，便于比较。
        /// 支持数值、<see cref="bool"/>、<see cref="DateTime"/>、<see cref="DateTimeOffset"/>、
        /// <see cref="TimeSpan"/>、<see cref="TimeOnly"/>、<see cref="DateOnly"/>、<see cref="Guid"/> 与枚举。
        /// 转换失败时原样返回输入值。
        /// </summary>
        public static object Coerce(object raw, Type targetType)
        {
            if (raw == null)
            {
                return null;
            }

            if (targetType == null)
            {
                return raw;
            }

            var target = ElPivotPropertyAccess.UnwrapNullable(targetType);

            if (target == null || target == typeof(object) || target == typeof(string))
            {
                return raw;
            }

            if (target.IsInstanceOfType(raw))
            {
                return raw;
            }

            var text = raw as string;
            if (text == null)
            {
                // 非字符串且类型不匹配：尝试一次数值/日期方向上的转换。
                if (ElPivotPropertyAccess.IsNumeric(target) && TryToDouble(raw, out var numeric))
                {
                    try
                    {
                        return Convert.ChangeType(numeric, target, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return raw;
                    }
                }

                if (ElPivotPropertyAccess.IsEnum(target))
                {
                    var parsed = TryToEnum(raw, target);
                    if (parsed != null)
                    {
                        return parsed;
                    }
                }

                return raw;
            }

            if (target == typeof(bool))
            {
                if (bool.TryParse(text.Trim(), out var boolean))
                {
                    return boolean;
                }

                if (text.Trim() == "1")
                {
                    return true;
                }

                if (text.Trim() == "0")
                {
                    return false;
                }

                return raw;
            }

            if (ElPivotPropertyAccess.IsNumeric(target))
            {
                try
                {
                    return Convert.ChangeType(text.Trim(), target, CultureInfo.InvariantCulture);
                }
                catch
                {
                    return raw;
                }
            }

            if (target == typeof(DateTime))
            {
                return DateTime.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime) ? dateTime : raw;
            }

            if (target == typeof(DateTimeOffset))
            {
                return DateTimeOffset.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var offset) ? offset : raw;
            }

            if (target == typeof(TimeSpan))
            {
                return TimeSpan.TryParse(text.Trim(), CultureInfo.InvariantCulture, out var span) ? span : raw;
            }

            if (target == typeof(TimeOnly))
            {
                return TimeOnly.TryParse(text.Trim(), CultureInfo.InvariantCulture, out var time) ? time : raw;
            }

            if (target == typeof(DateOnly))
            {
                return DateOnly.TryParse(text.Trim(), CultureInfo.InvariantCulture, out var date) ? date : raw;
            }

            if (target == typeof(Guid))
            {
                return Guid.TryParse(text.Trim(), out var guid) ? guid : raw;
            }

            if (target.IsEnum)
            {
                var parsed = TryToEnum(text, target);
                if (parsed != null)
                {
                    return parsed;
                }
            }

            return raw;
        }

        private static object TryToEnum(object value, Type enumType)
        {
            if (value == null)
            {
                return null;
            }

            if (enumType.IsInstanceOfType(value))
            {
                return value;
            }

            var text = value as string;

            if (text != null)
            {
                try
                {
                    return Enum.Parse(enumType, text.Trim(), true);
                }
                catch
                {
                    // 继续尝试按数值解析
                }
            }

            try
            {
                var underlying = Enum.GetUnderlyingType(enumType);
                var numeric = Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
                return Enum.ToObject(enumType, numeric);
            }
            catch
            {
                return null;
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

        private static bool TryToDateTime(object value, out DateTime result)
        {
            result = default;

            if (value is DateTime dateTime)
            {
                result = dateTime;
                return true;
            }

            if (value is DateTimeOffset offset)
            {
                result = offset.DateTime;
                return true;
            }

            if (value is string text && DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                result = parsed;
                return true;
            }

            return false;
        }
    }
}
