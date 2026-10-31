using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace ElementPlus
{
    /// <summary>
    /// 属性访问器：把「属性路径字符串」编译为 <see cref="Func{T, TResult}"/> 形式的取值委托，并缓存。
    /// <para>
    /// 这是对参考实现 <c>Radzen.PropertyAccess</c> 与 <c>System.Linq.Dynamic.Core</c>（动态 LINQ 表达式）
    /// 的**自写替代**：本组件不引用任何第三方表达式库，只用 <see cref="System.Reflection"/> 完成路径解析。
    /// </para>
    /// <para>支持的路径语法（以 <c>.</c> 分隔，可嵌套任意深度）：</para>
    /// <list type="bullet">
    /// <item><description>公共实例属性：<c>Region</c>、<c>Customer.Address.City</c></description></item>
    /// <item><description>公共实例字段：<c>RegionField</c></description></item>
    /// <item><description>实现 <see cref="IDictionary{TKey, TValue}"/>（键为 <see cref="string"/>）的类型的键访问：<c>Amount</c></description></item>
    /// <item><description>实现非泛型 <see cref="IDictionary"/> 的类型的键访问</description></item>
    /// <item><description>声明了单个 <see cref="string"/> 参数的公共索引器的类型（例如 <c>DataRow</c> 的 <c>this[string]</c>）</description></item>
    /// </list>
    /// <para>属性名匹配优先精确（<see cref="StringComparison.Ordinal"/>），其次忽略大小写，保证大小写写错也能工作。</para>
    /// </summary>
    public static class ElPivotPropertyAccess
    {
        private const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.Instance;

        private static readonly ConcurrentDictionary<string, Type> typeCache = new ConcurrentDictionary<string, Type>(StringComparer.Ordinal);

        private static readonly ConcurrentDictionary<string, Func<object, object>> getterCache =
            new ConcurrentDictionary<string, Func<object, object>>(StringComparer.Ordinal);

        /// <summary>
        /// 去掉 <see cref="Nullable{T}"/> 包装，返回底层类型；非可空类型原样返回。
        /// </summary>
        public static Type UnwrapNullable(Type type)
        {
            if (type == null)
            {
                return null;
            }

            var underlying = Nullable.GetUnderlyingType(type);
            return underlying ?? type;
        }

        /// <summary>
        /// 是否为可空值类型（<see cref="Nullable{T}"/>）。
        /// </summary>
        public static bool IsNullable(Type type)
        {
            return type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// 是否为可空枚举。
        /// </summary>
        public static bool IsNullableEnum(Type type)
        {
            return type != null && UnwrapNullable(type).IsEnum && IsNullable(type);
        }

        /// <summary>
        /// 是否为枚举（含可空枚举）。
        /// </summary>
        public static bool IsEnum(Type type)
        {
            return type != null && UnwrapNullable(type).IsEnum;
        }

        /// <summary>
        /// 是否为数值类型（不含 <see cref="bool"/>、<see cref="char"/>）。
        /// </summary>
        public static bool IsNumeric(Type type)
        {
            var t = UnwrapNullable(type);

            if (t == null || t.IsEnum)
            {
                return false;
            }

            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 是否为日期/时间类型（含 <see cref="DateTimeOffset"/>）。
        /// </summary>
        public static bool IsDate(Type type)
        {
            var t = UnwrapNullable(type);
            return t == typeof(DateTime) || t == typeof(DateTimeOffset);
        }

        /// <summary>
        /// 是否为字符串。
        /// </summary>
        public static bool IsString(Type type)
        {
            return UnwrapNullable(type) == typeof(string);
        }

        /// <summary>
        /// 返回类型上指定路径的最终值类型；无法解析时返回 <c>null</c>。
        /// </summary>
        public static Type GetPropertyType(Type type, string path)
        {
            if (type == null || string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var cacheKey = type.FullName + "|" + path;

            if (typeCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var current = type;
            Type result = null;

            foreach (var segment in Split(path))
            {
                if (current == null)
                {
                    break;
                }

                Type next;
                if (!TryResolveMember(current, segment, out next))
                {
                    current = null;
                    break;
                }

                current = next;
            }

            result = current;
            typeCache[cacheKey] = result;
            return result;
        }

        /// <summary>
        /// 返回指定类型 + 路径的取值委托；无法解析时返回 <c>null</c>。
        /// </summary>
        public static Func<object, object> CreateGetter(Type type, string path)
        {
            if (type == null || string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var cacheKey = type.FullName + "|" + path;

            if (getterCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var segments = new List<string>(Split(path));
            Func<object, object> getter = null;

            try
            {
                getter = BuildGetter(type, segments, 0);
            }
            catch
            {
                getter = null;
            }

            getterCache[cacheKey] = getter;
            return getter;
        }

        /// <summary>
        /// 直接在实例上按路径取值。解析失败或中途为 <c>null</c> 时返回 <c>null</c>。
        /// </summary>
        public static object GetValue(object instance, string path)
        {
            if (instance == null || string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var getter = CreateGetter(instance.GetType(), path);
            if (getter == null)
            {
                return null;
            }

            try
            {
                return getter(instance);
            }
            catch
            {
                return null;
            }
        }

        private static IEnumerable<string> Split(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                yield break;
            }

            foreach (var segment in path.Split('.'))
            {
                var trimmed = segment.Trim();
                if (trimmed.Length > 0)
                {
                    yield return trimmed;
                }
            }
        }

        private static Func<object, object> BuildGetter(Type type, IReadOnlyList<string> segments, int index)
        {
            if (index >= segments.Count)
            {
                return value => value;
            }

            var segment = segments[index];

            Func<object, object> step;
            Type nextType;

            if (!TryBuildStep(type, segment, out step, out nextType))
            {
                return null;
            }

            if (index == segments.Count - 1)
            {
                return step;
            }

            var rest = BuildGetter(nextType, segments, index + 1);
            if (rest == null)
            {
                return null;
            }

            return value =>
            {
                var intermediate = step(value);
                return intermediate == null ? null : rest(intermediate);
            };
        }

        private static bool TryResolveMember(Type type, string segment, out Type memberType)
        {
            Func<object, object> ignored;
            return TryBuildStep(type, segment, out ignored, out memberType);
        }

        private static bool TryBuildStep(Type type, string segment, out Func<object, object> step, out Type stepType)
        {
            step = null;
            stepType = null;

            if (type == null)
            {
                return false;
            }

            var property = FindProperty(type, segment);
            if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
            {
                var getter = property.GetGetMethod();
                if (getter != null)
                {
                    step = instance => getter.Invoke(instance, null);
                    stepType = property.PropertyType;
                    return true;
                }
            }

            var field = FindField(type, segment);
            if (field != null)
            {
                step = field.GetValue;
                stepType = field.FieldType;
                return true;
            }

            if (TryBuildDictionaryStep(type, segment, out step, out stepType))
            {
                return true;
            }

            if (TryBuildStringIndexerStep(type, segment, out step, out stepType))
            {
                return true;
            }

            return false;
        }

        private static PropertyInfo FindProperty(Type type, string segment)
        {
            var properties = type.GetProperties(InstanceFlags);

            foreach (var property in properties)
            {
                if (string.Equals(property.Name, segment, StringComparison.Ordinal))
                {
                    return property;
                }
            }

            foreach (var property in properties)
            {
                if (string.Equals(property.Name, segment, StringComparison.OrdinalIgnoreCase))
                {
                    return property;
                }
            }

            return null;
        }

        private static FieldInfo FindField(Type type, string segment)
        {
            var fields = type.GetFields(InstanceFlags);

            foreach (var field in fields)
            {
                if (string.Equals(field.Name, segment, StringComparison.Ordinal))
                {
                    return field;
                }
            }

            foreach (var field in fields)
            {
                if (string.Equals(field.Name, segment, StringComparison.OrdinalIgnoreCase))
                {
                    return field;
                }
            }

            return null;
        }

        private static bool TryBuildDictionaryStep(Type type, string segment, out Func<object, object> step, out Type stepType)
        {
            step = null;
            stepType = null;

            var genericDictionary = FindGenericStringDictionary(type);
            if (genericDictionary != null)
            {
                var valueType = genericDictionary.GetGenericArguments()[1];
                step = instance => ReadGenericDictionary(instance, segment);
                stepType = valueType;
                return true;
            }

            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                step = instance => instance is IDictionary dictionary ? dictionary[segment] : null;
                stepType = typeof(object);
                return true;
            }

            return false;
        }

        private static Type FindGenericStringDictionary(Type type)
        {
            foreach (var candidate in type.GetInterfaces())
            {
                if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    var arguments = candidate.GetGenericArguments();
                    if (arguments[0] == typeof(string))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private static object ReadGenericDictionary(object instance, string key)
        {
            var typed = instance as IDictionary<string, object>;

            if (typed != null)
            {
                object value;
                return typed.TryGetValue(key, out value) ? value : null;
            }

            // Dictionary<string, int> 之类的强类型值字典无法直接转成 IDictionary<string, object>，
            // 但所有泛型字典都实现了非泛型 IDictionary（下标取不到键时返回 null，不抛异常）。
            var dictionary = instance as System.Collections.IDictionary;

            return dictionary != null ? dictionary[key] : null;
        }

        private static bool TryBuildStringIndexerStep(Type type, string segment, out Func<object, object> step, out Type stepType)
        {
            step = null;
            stepType = null;

            foreach (var property in type.GetProperties(InstanceFlags))
            {
                if (!property.CanRead)
                {
                    continue;
                }

                var parameters = property.GetIndexParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
                {
                    continue;
                }

                var getter = property.GetGetMethod();
                if (getter == null)
                {
                    continue;
                }

                var args = new object[] { segment };
                step = instance => getter.Invoke(instance, args);
                stepType = property.PropertyType;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 把「属性路径」转换为直接委托（跳过重复反射），失败时退化为 <c>null</c>。
        /// 供聚合与分组使用，避免每个单元格重复走反射。
        /// </summary>
        public static Func<object, object> ValueGetter(Type type, string path, Type declaredType)
        {
            var getter = CreateGetter(type, path);

            if (getter != null)
            {
                return getter;
            }

            // 动态数据（字典 / DataRow / 匿名展开）无法在类型上解析时，退化为运行时按路径查找。
            return instance => GetValue(instance, path);
        }

        /// <summary>
        /// 尝试把任意值转换为 <see cref="double"/>，用于排序键比较；失败返回 <c>null</c>。
        /// </summary>
        public static double? ToSortKey(object value)
        {
            if (value == null)
            {
                return null;
            }

            try
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 排序比较器：字符串按序数比较，同类型 <see cref="IComparable"/> 直接比较，
        /// 数值之间按数值比较，其余按不变文化的字符串比较。全部规则都是确定性的（不依赖当前区域设置）。
        /// </summary>
        public static int CompareValues(object left, object right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            if (left is string leftText && right is string rightText)
            {
                return string.CompareOrdinal(leftText, rightText);
            }

            if (left.GetType() == right.GetType() && left is IComparable comparable)
            {
                try
                {
                    return comparable.CompareTo(right);
                }
                catch
                {
                    // 落到字符串比较
                }
            }

            var leftNumber = ToSortKey(left);
            var rightNumber = ToSortKey(right);

            if (leftNumber.HasValue && rightNumber.HasValue)
            {
                return leftNumber.Value.CompareTo(rightNumber.Value);
            }

            return string.CompareOrdinal(
                Convert.ToString(left, CultureInfo.InvariantCulture) ?? string.Empty,
                Convert.ToString(right, CultureInfo.InvariantCulture) ?? string.Empty);
        }

        /// <summary>
        /// 分组键等价比较器：用于把属性值聚成组。数值之间按数值比较（<c>1</c> 与 <c>1.0m</c> 同组），
        /// 字符串按序数比较，其余按类型 + <see cref="object.Equals(object)"/>。
        /// </summary>
        public sealed class GroupKeyComparer : IEqualityComparer<object>
        {
            /// <summary>共享实例。</summary>
            public static readonly GroupKeyComparer Instance = new GroupKeyComparer();

            /// <inheritdoc />
            public new bool Equals(object x, object y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                if (x is string sx && y is string sy)
                {
                    return string.Equals(sx, sy, StringComparison.Ordinal);
                }

                // bool 不是数值：true 不应与 1 归为同一组（ToSortKey 会把 bool 转成 1/0，必须在这里挡掉）。
                if (x is bool || y is bool)
                {
                    return x is bool bx && y is bool by && bx == by;
                }

                if (x.GetType() == y.GetType())
                {
                    return x.Equals(y);
                }

                var nx = ToSortKey(x);
                var ny = ToSortKey(y);

                if (nx.HasValue && ny.HasValue)
                {
                    return nx.Value.Equals(ny.Value);
                }

                return x.Equals(y);
            }

            /// <inheritdoc />
            public int GetHashCode(object obj)
            {
                if (obj == null)
                {
                    return 0;
                }

                if (obj is bool boolean)
                {
                    return boolean.GetHashCode();
                }

                // 必须与 Equals 保持一致：Equals 把「数值相等的不同类型」视为同一分组键
                // （int 1 / long 1 / decimal 1 / "1"），所以哈希也必须按数值计算，
                // 否则会出现「Equals 为 true 但哈希不同」，字典分组会错乱出重复分组。
                var numeric = ToSortKey(obj);

                if (numeric.HasValue)
                {
                    return numeric.Value.GetHashCode();
                }

                return obj is string text ? StringComparer.Ordinal.GetHashCode(text) : obj.GetHashCode();
            }
        }
    }
}
