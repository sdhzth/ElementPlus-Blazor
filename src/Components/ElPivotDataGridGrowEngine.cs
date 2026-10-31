using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ElementPlus
{
    /// <summary>
    /// 引擎选项：把「组件参数」与「引擎」解耦，使引擎可以完全脱离 Blazor 单测。
    /// </summary>
    public sealed class ElPivotEngineOptions
    {
        /// <summary>是否允许下钻（分组折叠/展开）。与参考实现的 <c>AllowDrillDown</c> 一致。</summary>
        public bool AllowDrillDown { get; set; } = true;

        /// <summary>
        /// 分组默认是否展开。参考实现默认**折叠**（只显示最外层），这里保持一致；
        /// 置为 <c>true</c> 可让初始视图直接展示全部层级。
        /// </summary>
        public bool ExpandGroupsByDefault { get; set; }

        /// <summary>
        /// 分组折叠状态覆盖表：键为分组路径键，值为「是否折叠」。
        /// 缺省项使用 <see cref="ExpandGroupsByDefault"/>；用户点击展开/收起后写入此表。
        /// </summary>
        public IDictionary<string, bool> GroupState { get; set; }

        /// <summary>「其他」分组的默认标题（被字段级 <c>OthersLabel</c> 覆盖）。</summary>
        public string OthersText { get; set; } = "其他";

        /// <summary>分组键为 <c>null</c> 时的显示标题。</summary>
        public string NullGroupText { get; set; } = "（空）";

        /// <summary>是否计算行合计。</summary>
        public bool ShowRowsTotals { get; set; }

        /// <summary>是否计算列合计与右下角总计。</summary>
        public bool ShowColumnsTotals { get; set; }

        /// <summary>多个字段之间筛选条件的组合方式。</summary>
        public ElPivotLogicalOperator LogicalOperator { get; set; } = ElPivotLogicalOperator.And;

        /// <summary>筛选的默认大小写敏感性（用于自身未显式指定的筛选条件）。</summary>
        public ElPivotFilterCaseSensitivity FilterCaseSensitivity { get; set; } = ElPivotFilterCaseSensitivity.CaseInsensitive;

        /// <summary>网格级筛选条件（对所有字段生效）。</summary>
        public ElPivotFilterDescriptor Filter { get; set; }
    }

    /// <summary>
    /// 交叉表（透视表）计算引擎 —— 本组件的**核心与单一真源**。
    /// <para>
    /// 输入：扁平记录集合 + 行字段 + 列字段 + 值字段（聚合）；
    /// 输出：<see cref="ElPivotLayout{TItem}"/>，包含表头（含跨行/跨列）、列叶子、数据行、
    /// 行合计、列合计与总计。组件只负责把该布局翻译成 HTML。
    /// </para>
    /// <para>
    /// 与参考实现的差异（均为有意为之，见 <c>docs/pivot-data-grid-alignment.md</c>）：
    /// </para>
    /// <list type="number">
    /// <item><description>不生成任何动态 LINQ 表达式串，筛选/分组/排序/聚合全部用纯 C# 实现，零第三方依赖。</description></item>
    /// <item><description>聚合单趟遍历完成（参考实现为每个单元格重建一次 <c>IQueryable</c> 并多次枚举）。</description></item>
    /// <item><description>修复参考实现 <c>GetColumnHeaderLeavesRecursive</c> 中「值非 null 才入栈、却无条件出栈」导致的路径错乱。</description></item>
    /// <item><description>行/列字段为空时不再产生结构性空表：无行字段 → 单行总计；无列字段 → 单列总计。</description></item>
    /// <item><description>分组值排序使用确定性比较（字符串按序数比较），不随区域设置变化。</description></item>
    /// </list>
    /// </summary>
    public static class ElPivotEngine
    {
        private static readonly object NullKeySentinel = new object();

        private const string OthersPathKey = "__el_pivot_others__";

        /// <summary>「其他」分组的固定路径键。</summary>
        public static string OthersKey
        {
            get { return OthersPathKey; }
        }

        /// <summary>
        /// 构建交叉表布局。
        /// </summary>
        /// <typeparam name="TItem">记录类型。</typeparam>
        /// <param name="data">原始记录集合（可为 <c>null</c>）。</param>
        /// <param name="rows">行字段（按层级顺序）。</param>
        /// <param name="columns">列字段（按层级顺序）。</param>
        /// <param name="aggregates">值字段（按显示顺序）。</param>
        /// <param name="options">引擎选项。</param>
        public static ElPivotLayout<TItem> Build<TItem>(
            IEnumerable<TItem> data,
            IReadOnlyList<ElPivotFieldSpec> rows,
            IReadOnlyList<ElPivotFieldSpec> columns,
            IReadOnlyList<ElPivotAggregateSpec> aggregates,
            ElPivotEngineOptions options = null)
        {
            options = options ?? new ElPivotEngineOptions();
            rows = rows ?? Array.Empty<ElPivotFieldSpec>();
            columns = columns ?? Array.Empty<ElPivotFieldSpec>();
            aggregates = aggregates ?? Array.Empty<ElPivotAggregateSpec>();

            var layout = new ElPivotLayout<TItem>();
            var source = data == null ? new List<TItem>() : new List<TItem>(data);

            var context = new BuildContext<TItem>(rows, columns, aggregates, options);

            // 筛选必须先于分组：先把字段级筛选登记进来，再一次性过滤数据。
            context.RegisterFieldFilters(rows, columns, aggregates);
            layout.Items = context.ApplyFilters(source);

            var rowRoot = new Node<TItem> { Level = 0 };
            var columnRoot = new Node<TItem> { Level = 0 };

            context.BuildChildren(rowRoot, layout.Items, rows, 0, new List<object>(), true);
            context.BuildChildren(columnRoot, layout.Items, columns, 0, new List<object>(), false);
            context.SetColumnContext(columns, columnRoot);

            layout.HasOthersColumns = HasOthers(columns);

            // ---- 表头 ----
            var headerRows = new List<ElPivotHeaderRow>(columns.Count);
            for (var i = 0; i < columns.Count; i++)
            {
                headerRows.Add(new ElPivotHeaderRow { Index = i });
            }

            context.FlattenColumns(columnRoot, headerRows, columns.Count, aggregates.Count, columns);

            while (headerRows.Count > 0 && headerRows[headerRows.Count - 1].Cells.Count == 0)
            {
                headerRows.RemoveAt(headerRows.Count - 1);
            }

            // 列分组被折叠（或某个深层字段整体没有单元格）时，深层表头行会整行为空并被上面裁掉。
            // 此时叶子单元格按「列字段层数」算出的 rowspan 会**超出实际渲染出来的表头行数**：
            // 浏览器会让它继续占住下面那行（值字段名表头行，如「金额(求和)」）的列位，把值表头
            // 顶到表格最右侧，凭空多出几列「只有表头、没有数据」的空列。
            //
            // 所以这里统一按「本行之下还剩几行表头」收敛一次跨行数：折叠时它应当只跨自己那一行，
            // 让值字段名落回同一列的正下方（与「只有一列字段」时的行为保持一致）；
            // 未折叠（还有深层表头）时该收敛是幂等的，不改变原有结果。
            for (var i = 0; i < headerRows.Count; i++)
            {
                var remaining = headerRows.Count - i;

                foreach (var cell in headerRows[i].Cells)
                {
                    if (cell.RowSpan > remaining)
                    {
                        cell.RowSpan = remaining;
                    }
                }
            }

            layout.HeaderRows.AddRange(headerRows);
            layout.HeaderRowCount = headerRows.Count;

            // ---- 列叶子 ----
            var leaves = new List<List<object>>();

            if (columns.Count == 0)
            {
                // 没有列字段：退化为一列（总计列），避免出现「零个值列」的空表。
                leaves.Add(new List<object>());
            }
            else
            {
                context.CollectLeaves(columnRoot, new List<object>(), leaves);
            }

            layout.ColumnLeaves.AddRange(leaves);

            // ---- 数据行 ----
            var hasColumnFields = columns.Count > 0;

            if (rows.Count == 0)
            {
                var single = new ElPivotBodyRow<TItem> { Items = layout.Items };
                context.PopulateValueCells(single, layout.Items, leaves, hasColumnFields);
                layout.BodyRows.Add(single);
                layout.RowHeaderDepth = 0;
            }
            else
            {
                var bodyRows = new List<ElPivotBodyRow<TItem>>();
                context.FlattenRows(rowRoot, new List<ElPivotRowHeaderCell>(), new List<object>(), bodyRows, rows.Count, leaves, hasColumnFields, rows);
                context.PadHeaderCells(bodyRows);
                layout.BodyRows.AddRange(bodyRows);
                layout.RowHeaderDepth = MaxHeaderCells(bodyRows);
            }

            // ---- 合计 ----
            if (options.ShowColumnsTotals)
            {
                var buckets = context.BucketByLeaf(layout.Items, leaves, hasColumnFields);

                foreach (var leaf in leaves)
                {
                    var key = PathKey(leaf);
                    buckets.TryGetValue(key, out var bucket);
                    layout.ColumnTotals[key] = context.ComputeAll(bucket, aggregates);
                }

                layout.GrandTotals.AddRange(context.ComputeAll(layout.Items, aggregates));
            }

            return layout;
        }

        private static bool HasOthers(IReadOnlyList<ElPivotFieldSpec> fields)
        {
            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                if (field != null && field.MaxGroups.HasValue && field.MaxGroups.Value > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static int MaxHeaderCells<TItem>(List<ElPivotBodyRow<TItem>> rows)
        {
            var max = 0;

            foreach (var row in rows)
            {
                if (row.HeaderCells.Count > max)
                {
                    max = row.HeaderCells.Count;
                }
            }

            return max;
        }

        /// <summary>
        /// 分组路径键：把路径值按 <c>|</c> 连接，<c>null</c> 归一化为空串。
        /// </summary>
        public static string PathKey(IEnumerable<object> path)
        {
            if (path == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            var first = true;

            foreach (var value in path)
            {
                if (!first)
                {
                    builder.Append('|');
                }

                builder.Append(Normalize(value));
                first = false;
            }

            return builder.ToString();
        }

        /// <summary>
        /// 把分组键归一化为字符串（用于叶子键匹配，与 <see cref="PathKey"/> 严格一致）。
        /// </summary>
        public static string Normalize(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is string text)
            {
                return text;
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        /// <summary>
        /// 分组节点。行列树共用同一结构（两者只在排序规则上不同）。
        /// </summary>
        private sealed class Node<TItem>
        {
            public object Value { get; set; }

            public string Title { get; set; }

            public int Level { get; set; }

            public string PathKey { get; set; }

            public bool IsOthers { get; set; }

            public bool IsCollapsed { get; set; }

            public string Width { get; set; }

            public List<Node<TItem>> Children { get; } = new List<Node<TItem>>();

            public List<TItem> Items { get; set; }

            public int GroupCount { get; set; }
        }

        private sealed class Group<TItem>
        {
            public object Key { get; set; }

            public List<TItem> Items { get; } = new List<TItem>();
        }

        /// <summary>
        /// 单次构建的可变上下文：缓存取值委托（避免重复反射）、持有筛选登记与列树引用。
        /// </summary>
        private sealed class BuildContext<TItem>
        {
            private readonly IReadOnlyList<ElPivotAggregateSpec> aggregates;
            private readonly ElPivotEngineOptions options;
            private readonly Dictionary<string, Func<object, object>> getterCache = new Dictionary<string, Func<object, object>>(StringComparer.Ordinal);
            private readonly List<ElPivotFilterDescriptor> pendingFilters = new List<ElPivotFilterDescriptor>();

            private IReadOnlyList<ElPivotFieldSpec> columnFields = Array.Empty<ElPivotFieldSpec>();
            private Node<TItem> columnTree;

            public BuildContext(
                IReadOnlyList<ElPivotFieldSpec> rows,
                IReadOnlyList<ElPivotFieldSpec> columns,
                IReadOnlyList<ElPivotAggregateSpec> aggregates,
                ElPivotEngineOptions options)
            {
                this.aggregates = aggregates;
                this.options = options;
            }

            public void SetColumnContext(IReadOnlyList<ElPivotFieldSpec> fields, Node<TItem> tree)
            {
                columnFields = fields ?? Array.Empty<ElPivotFieldSpec>();
                columnTree = tree;
            }

            public Func<object, object> Getter(string property)
            {
                if (string.IsNullOrWhiteSpace(property))
                {
                    return null;
                }

                if (getterCache.TryGetValue(property, out var cached))
                {
                    return cached;
                }

                var getter = ElPivotPropertyAccess.ValueGetter(typeof(TItem), property, null);
                getterCache[property] = getter;
                return getter;
            }

            private Func<object, object> RequireGetter(string property)
            {
                var getter = Getter(property);
                return getter ?? (instance => ElPivotPropertyAccess.GetValue(instance, property));
            }

            // ---------------------------------------------------------------- 筛选

            public void RegisterFieldFilters(
                IReadOnlyList<ElPivotFieldSpec> rows,
                IReadOnlyList<ElPivotFieldSpec> columns,
                IReadOnlyList<ElPivotAggregateSpec> aggregates)
            {
                RegisterList(rows);
                RegisterList(columns);

                foreach (var aggregate in aggregates)
                {
                    if (aggregate == null || aggregate.Filter == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(aggregate.Filter.Property))
                    {
                        aggregate.Filter.Property = aggregate.Property;
                    }

                    RegisterFilter(aggregate.Filter);
                }
            }

            private void RegisterList(IReadOnlyList<ElPivotFieldSpec> fields)
            {
                foreach (var field in fields)
                {
                    if (field == null || field.Filter == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(field.Filter.Property))
                    {
                        field.Filter.Property = field.Property;
                    }

                    RegisterFilter(field.Filter);
                }
            }

            private void RegisterFilter(ElPivotFilterDescriptor descriptor)
            {
                if (descriptor == null || descriptor.IsEmpty || descriptor.Operator == ElPivotFilterOperator.Custom)
                {
                    return;
                }

                // 补齐属性类型，供比较值强制转换使用。
                if (descriptor.Type == null && !string.IsNullOrEmpty(descriptor.Property))
                {
                    descriptor.Type = ElPivotPropertyAccess.GetPropertyType(typeof(TItem), descriptor.Property);
                }

                pendingFilters.Add(descriptor);
            }

            public List<TItem> ApplyFilters(IReadOnlyList<TItem> source)
            {
                var predicates = new List<KeyValuePair<Func<object, object>, ElPivotFilterDescriptor>>();

                var gridFilter = options.Filter;
                if (gridFilter != null && !gridFilter.IsEmpty && gridFilter.Operator != ElPivotFilterOperator.Custom)
                {
                    predicates.Add(new KeyValuePair<Func<object, object>, ElPivotFilterDescriptor>(RequireGetter(gridFilter.Property), Prepare(gridFilter)));
                }

                foreach (var descriptor in pendingFilters)
                {
                    predicates.Add(new KeyValuePair<Func<object, object>, ElPivotFilterDescriptor>(RequireGetter(descriptor.Property), Prepare(descriptor)));
                }

                if (predicates.Count == 0)
                {
                    return new List<TItem>(source);
                }

                var result = new List<TItem>(source.Count);

                foreach (var item in source)
                {
                    if (Matches(item, predicates))
                    {
                        result.Add(item);
                    }
                }

                return result;
            }

            private ElPivotFilterDescriptor Prepare(ElPivotFilterDescriptor descriptor)
            {
                if (descriptor.CaseSensitivity != ElPivotFilterCaseSensitivity.Default
                    || options.FilterCaseSensitivity == ElPivotFilterCaseSensitivity.Default)
                {
                    return descriptor;
                }

                var clone = descriptor.Clone();
                clone.CaseSensitivity = options.FilterCaseSensitivity;
                return clone;
            }

            private bool Matches(TItem item, List<KeyValuePair<Func<object, object>, ElPivotFilterDescriptor>> predicates)
            {
                var any = options.LogicalOperator == ElPivotLogicalOperator.Or;

                foreach (var predicate in predicates)
                {
                    var value = predicate.Key(item);

                    if (ElPivotFilterMatcher.Matches(value, predicate.Value))
                    {
                        if (any)
                        {
                            return true;
                        }
                    }
                    else if (!any)
                    {
                        return false;
                    }
                }

                return !any;
            }

            // ---------------------------------------------------------------- 分组树

            public void BuildChildren(
                Node<TItem> parent,
                IReadOnlyList<TItem> items,
                IReadOnlyList<ElPivotFieldSpec> fields,
                int level,
                List<object> path,
                bool isRowTree)
            {
                if (level >= fields.Count)
                {
                    parent.Items = items as List<TItem> ?? new List<TItem>(items);
                    return;
                }

                var field = fields[level];
                var getter = field == null ? null : Getter(field.Property);

                var groups = BuildGroups(items, getter);
                var ordered = OrderGroups(groups, field, isRowTree);

                List<TItem> othersItems;
                var kept = ApplyMaxGroups(ordered, field == null ? null : field.MaxGroups, out othersItems);

                foreach (var group in kept)
                {
                    AddChild(parent, fields, field, group.Key, group.Items, level, path, isRowTree, false);
                }

                if (othersItems != null)
                {
                    var label = field != null && !string.IsNullOrEmpty(field.OthersLabel) ? field.OthersLabel : options.OthersText;
                    AddChild(parent, fields, field, label, othersItems, level, path, isRowTree, true);
                }
            }

            private void AddChild(
                Node<TItem> parent,
                IReadOnlyList<ElPivotFieldSpec> fields,
                ElPivotFieldSpec field,
                object value,
                List<TItem> items,
                int level,
                List<object> path,
                bool isRowTree,
                bool isOthers)
            {
                var childPath = new List<object>(path) { value };
                var pathKey = PathKey(childPath);

                var child = new Node<TItem>
                {
                    Value = value,
                    Title = FormatGroupTitle(value),
                    Level = level + 1,
                    PathKey = pathKey,
                    Width = field == null ? null : field.Width,
                    IsOthers = isOthers,
                    GroupCount = items.Count
                };

                var hasChildren = level + 1 < fields.Count;
                child.IsCollapsed = IsCollapsed(pathKey, hasChildren);

                parent.Children.Add(child);

                if (hasChildren && child.IsCollapsed)
                {
                    child.Items = items;
                }
                else
                {
                    BuildChildren(child, items, fields, level + 1, childPath, isRowTree);
                }
            }

            private bool IsCollapsed(string pathKey, bool hasChildren)
            {
                if (!hasChildren || !options.AllowDrillDown || string.IsNullOrEmpty(pathKey))
                {
                    return false;
                }

                if (options.GroupState != null && options.GroupState.TryGetValue(pathKey, out var collapsed))
                {
                    return collapsed;
                }

                return !options.ExpandGroupsByDefault;
            }

            private List<Group<TItem>> BuildGroups(IReadOnlyList<TItem> items, Func<object, object> getter)
            {
                var ordered = new List<Group<TItem>>();

                if (getter == null)
                {
                    // 属性无法解析（字段未设置 / 路径写错）：退化为单个分组，保证界面不崩。
                    var only = new Group<TItem> { Key = null };
                    only.Items.AddRange(items);
                    ordered.Add(only);
                    return ordered;
                }

                var map = new Dictionary<object, Group<TItem>>(ElPivotPropertyAccess.GroupKeyComparer.Instance);

                foreach (var item in items)
                {
                    var key = getter(item);
                    var lookup = key ?? NullKeySentinel;

                    if (!map.TryGetValue(lookup, out var group))
                    {
                        group = new Group<TItem> { Key = key };
                        map[lookup] = group;
                        ordered.Add(group);
                    }

                    group.Items.Add(item);
                }

                return ordered;
            }

            private List<Group<TItem>> OrderGroups(List<Group<TItem>> groups, ElPivotFieldSpec field, bool isRowTree)
            {
                if (isRowTree)
                {
                    var sortedAggregate = FirstSortedAggregate();

                    if (sortedAggregate != null)
                    {
                        var getter = Getter(sortedAggregate.Property);
                        var keyed = new List<KeyValuePair<double, Group<TItem>>>(groups.Count);

                        foreach (var group in groups)
                        {
                            var value = ElPivotAggregator.Compute(group.Items, getter, sortedAggregate.Function, sortedAggregate.PropertyType);
                            keyed.Add(new KeyValuePair<double, Group<TItem>>(ElPivotPropertyAccess.ToSortKey(value) ?? double.MinValue, group));
                        }

                        var descending = sortedAggregate.SortOrder == ElPivotSortOrder.Descending;
                        keyed.Sort((left, right) => descending ? right.Key.CompareTo(left.Key) : left.Key.CompareTo(right.Key));

                        var byAggregate = new List<Group<TItem>>(keyed.Count);
                        foreach (var pair in keyed)
                        {
                            byAggregate.Add(pair.Value);
                        }

                        return byAggregate;
                    }
                }

                var order = field == null ? null : field.SortOrder;

                if (order == null)
                {
                    return groups;
                }

                var comparer = Comparer<object>.Create(ElPivotPropertyAccess.CompareValues);
                var descendingOrder = order == ElPivotSortOrder.Descending;
                var ordered = new List<Group<TItem>>(groups);

                ordered.Sort((left, right) =>
                {
                    var comparison = comparer.Compare(left.Key, right.Key);
                    return descendingOrder ? -comparison : comparison;
                });

                return ordered;
            }

            private ElPivotAggregateSpec FirstSortedAggregate()
            {
                foreach (var aggregate in aggregates)
                {
                    if (aggregate != null && aggregate.SortOrder.HasValue)
                    {
                        return aggregate;
                    }
                }

                return null;
            }

            private ElPivotAggregateSpec FirstAggregate()
            {
                foreach (var aggregate in aggregates)
                {
                    if (aggregate != null)
                    {
                        return aggregate;
                    }
                }

                return null;
            }

            private List<Group<TItem>> ApplyMaxGroups(List<Group<TItem>> groups, int? maxGroups, out List<TItem> othersItems)
            {
                othersItems = null;

                if (!maxGroups.HasValue || maxGroups.Value <= 0 || groups.Count <= maxGroups.Value)
                {
                    return groups;
                }

                var limit = maxGroups.Value;
                var ranking = FirstSortedAggregate() ?? FirstAggregate();
                var kept = new HashSet<Group<TItem>>();

                if (ranking != null)
                {
                    var getter = Getter(ranking.Property);
                    var keyed = new List<KeyValuePair<double, Group<TItem>>>(groups.Count);

                    foreach (var group in groups)
                    {
                        var value = ElPivotAggregator.Compute(group.Items, getter, ranking.Function, ranking.PropertyType);
                        keyed.Add(new KeyValuePair<double, Group<TItem>>(ElPivotPropertyAccess.ToSortKey(value) ?? double.MinValue, group));
                    }

                    keyed.Sort((left, right) => right.Key.CompareTo(left.Key));

                    for (var i = 0; i < keyed.Count && kept.Count < limit; i++)
                    {
                        kept.Add(keyed[i].Value);
                    }
                }
                else
                {
                    for (var i = 0; i < groups.Count && kept.Count < limit; i++)
                    {
                        kept.Add(groups[i]);
                    }
                }

                var result = new List<Group<TItem>>(limit);
                var others = new List<TItem>();

                foreach (var group in groups)
                {
                    if (kept.Contains(group))
                    {
                        result.Add(group);
                    }
                    else
                    {
                        others.AddRange(group.Items);
                    }
                }

                othersItems = others;
                return result;
            }

            private string FormatGroupTitle(object key)
            {
                return key == null ? options.NullGroupText : Convert.ToString(key) ?? string.Empty;
            }

            // ---------------------------------------------------------------- 表头展平

            public void FlattenColumns(
                Node<TItem> parent,
                List<ElPivotHeaderRow> rows,
                int maxLevel,
                int aggregateCount,
                IReadOnlyList<ElPivotFieldSpec> fields)
            {
                foreach (var child in parent.Children)
                {
                    var depth = child.Level;

                    if (depth >= 1 && depth <= rows.Count)
                    {
                        var isLeaf = child.Children.Count == 0;
                        var hasChildren = depth < maxLevel;

                        var cell = new ElPivotHeaderCell
                        {
                            Value = child.Value,
                            Title = child.Title,
                            Level = depth - 1,
                            Width = child.Width,
                            IsCollapsed = child.IsCollapsed,
                            HasChildren = hasChildren,
                            PathKey = hasChildren ? child.PathKey : null,
                            IsOthers = child.IsOthers,
                            GroupCount = child.GroupCount
                        };

                        if (isLeaf)
                        {
                            cell.RowSpan = Math.Max(1, maxLevel - (depth - 1));
                            cell.ColSpan = Math.Max(1, aggregateCount);
                        }
                        else
                        {
                            cell.RowSpan = 1;
                            cell.ColSpan = LeafCount(child) * Math.Max(1, aggregateCount);
                        }

                        // 注意：叶子单元格同样要有 Context——列字段的 HeaderTemplate 需要它
                        // （参考实现里 HeaderTemplate 对叶子表头也生效）。RowHeader 那边只在有子级时给，
                        // 因为行字段没有 HeaderTemplate 这一需求。
                        if (fields != null && cell.Level >= 0 && cell.Level < fields.Count)
                        {
                            cell.Context = new ElPivotGroupContext
                            {
                                Key = child.Value,
                                Title = child.Title,
                                Count = child.GroupCount,
                                Level = cell.Level,
                                Property = fields[cell.Level].Property,
                                IsCollapsed = child.IsCollapsed,
                                PathKey = cell.PathKey
                            };
                        }

                        rows[depth - 1].Cells.Add(cell);
                    }

                    FlattenColumns(child, rows, maxLevel, aggregateCount, fields);
                }
            }

            public int LeafCount(Node<TItem> node)
            {
                if (node.Children.Count == 0)
                {
                    return 1;
                }

                var total = 0;

                foreach (var child in node.Children)
                {
                    total += LeafCount(child);
                }

                return total;
            }

            public void CollectLeaves(Node<TItem> node, List<object> path, List<List<object>> leaves)
            {
                if (node.Children.Count == 0)
                {
                    if (path.Count > 0)
                    {
                        leaves.Add(new List<object>(path));
                    }

                    return;
                }

                foreach (var child in node.Children)
                {
                    path.Add(child.Value);
                    CollectLeaves(child, path, leaves);

                    // 与入栈严格配对出栈（修正参考实现「条件入栈、无条件出栈」导致的路径错乱）。
                    path.RemoveAt(path.Count - 1);
                }
            }

            // ---------------------------------------------------------------- 数据行

            public void FlattenRows(
                Node<TItem> node,
                List<ElPivotRowHeaderCell> prefix,
                List<object> path,
                List<ElPivotBodyRow<TItem>> result,
                int maxDepth,
                List<List<object>> leaves,
                bool hasColumnFields,
                IReadOnlyList<ElPivotFieldSpec> fields)
            {
                if (node.Level == maxDepth)
                {
                    var row = new ElPivotBodyRow<TItem>
                    {
                        Items = node.Items ?? new List<TItem>(),
                        PathValues = new List<object>(path)
                    };

                    row.HeaderCells.AddRange(prefix);
                    PopulateValueCells(row, row.Items, leaves, hasColumnFields);
                    result.Add(row);
                    return;
                }

                foreach (var child in node.Children)
                {
                    var hasChildren = child.Level < maxDepth;

                    var cell = new ElPivotRowHeaderCell
                    {
                        Value = child.Value,
                        Title = child.Title,
                        Level = child.Level - 1,
                        RowSpan = LeafCount(child),
                        IsCollapsed = child.IsCollapsed,
                        HasChildren = hasChildren,
                        PathKey = hasChildren ? child.PathKey : null,
                        IsOthers = child.IsOthers,
                        GroupCount = child.GroupCount
                    };

                    if (hasChildren)
                    {
                        cell.Context = new ElPivotGroupContext
                        {
                            Key = child.Value,
                            Title = child.Title,
                            Count = child.GroupCount,
                            Level = cell.Level,
                            Property = fields != null && cell.Level < fields.Count ? fields[cell.Level].Property : null,
                            IsCollapsed = child.IsCollapsed,
                            PathKey = child.PathKey
                        };
                    }

                    var newPrefix = new List<ElPivotRowHeaderCell>(prefix) { cell };
                    var newPath = new List<object>(path) { child.Value };

                    if (child.IsCollapsed)
                    {
                        var row = new ElPivotBodyRow<TItem>
                        {
                            Items = child.Items ?? new List<TItem>(),
                            PathValues = newPath,
                            IsCollapsedGroup = true
                        };

                        row.HeaderCells.AddRange(newPrefix);
                        PopulateValueCells(row, row.Items, leaves, hasColumnFields);
                        result.Add(row);
                    }
                    else
                    {
                        FlattenRows(child, newPrefix, newPath, result, maxDepth, leaves, hasColumnFields, fields);
                    }
                }
            }

            public void PadHeaderCells(List<ElPivotBodyRow<TItem>> rows)
            {
                var maxDepth = 0;

                foreach (var row in rows)
                {
                    if (row.HeaderCells.Count > maxDepth)
                    {
                        maxDepth = row.HeaderCells.Count;
                    }
                }

                foreach (var row in rows)
                {
                    while (row.HeaderCells.Count < maxDepth)
                    {
                        row.HeaderCells.Add(new ElPivotRowHeaderCell
                        {
                            Title = string.Empty,
                            RowSpan = 1,
                            IsPlaceholder = true
                        });
                    }
                }
            }

            // ---------------------------------------------------------------- 取值

            public void PopulateValueCells(ElPivotBodyRow<TItem> row, IReadOnlyList<TItem> items, List<List<object>> leaves, bool hasColumnFields)
            {
                row.ValueCells.Clear();
                row.RowTotals.Clear();

                var buckets = BucketByLeaf(items, leaves, hasColumnFields);

                foreach (var leaf in leaves)
                {
                    buckets.TryGetValue(PathKey(leaf), out var bucket);

                    foreach (var aggregate in aggregates)
                    {
                        row.ValueCells.Add(Compute(bucket, aggregate));
                    }
                }

                if (options.ShowRowsTotals)
                {
                    foreach (var aggregate in aggregates)
                    {
                        row.RowTotals.Add(Compute(items as List<TItem>, aggregate));
                    }
                }
            }

            public List<object> ComputeAll(IReadOnlyList<TItem> items, IReadOnlyList<ElPivotAggregateSpec> specs)
            {
                var result = new List<object>(specs.Count);

                foreach (var aggregate in specs)
                {
                    result.Add(Compute(items as List<TItem>, aggregate));
                }

                return result;
            }

            private object Compute(List<TItem> items, ElPivotAggregateSpec aggregate)
            {
                if (aggregate == null || items == null || items.Count == 0)
                {
                    return null;
                }

                if (aggregate.Filter != null && !aggregate.Filter.IsEmpty && aggregate.Filter.Operator != ElPivotFilterOperator.Custom)
                {
                    var descriptor = Prepare(aggregate.Filter);
                    var filterGetter = Getter(descriptor.Property);
                    var filtered = new List<TItem>(items.Count);

                    foreach (var item in items)
                    {
                        var value = filterGetter == null ? null : filterGetter(item);

                        if (ElPivotFilterMatcher.Matches(value, descriptor))
                        {
                            filtered.Add(item);
                        }
                    }

                    items = filtered;

                    if (items.Count == 0)
                    {
                        return null;
                    }
                }

                return ElPivotAggregator.Compute(items, Getter(aggregate.Property), aggregate.Function, aggregate.PropertyType);
            }

            // ---------------------------------------------------------------- 列叶子归桶

            public Dictionary<string, List<TItem>> BucketByLeaf(IReadOnlyList<TItem> items, List<List<object>> leaves, bool hasColumnFields)
            {
                var buckets = new Dictionary<string, List<TItem>>(StringComparer.Ordinal);

                if (items == null || items.Count == 0 || leaves.Count == 0)
                {
                    return buckets;
                }

                if (!hasColumnFields)
                {
                    buckets[string.Empty] = new List<TItem>(items);
                    return buckets;
                }

                var getters = new List<Func<object, object>>(columnFields.Count);

                foreach (var column in columnFields)
                {
                    getters.Add(column == null ? (instance => null) : RequireGetter(column.Property));
                }

                var leafKeys = new HashSet<string>(StringComparer.Ordinal);

                foreach (var leaf in leaves)
                {
                    leafKeys.Add(PathKey(leaf));
                }

                // 存在「其他」分组或存在折叠分组时，必须沿列树定位（叶子可能比列字段更浅）。
                var useTree = HasOthers(columnFields) || HasCollapsedColumnGroups();

                var path = new List<object>(columnFields.Count);

                foreach (var item in items)
                {
                    string matchKey;

                    if (useTree)
                    {
                        matchKey = FindLeafKeyByTree(item, getters);
                    }
                    else
                    {
                        matchKey = null;
                        path.Clear();

                        for (var level = 0; level < columnFields.Count; level++)
                        {
                            path.Add(getters[level](item));

                            var key = PathKey(path);

                            if (leafKeys.Contains(key))
                            {
                                matchKey = key;
                                break;
                            }
                        }
                    }

                    if (matchKey == null)
                    {
                        continue;
                    }

                    if (!buckets.TryGetValue(matchKey, out var bucket))
                    {
                        bucket = new List<TItem>();
                        buckets[matchKey] = bucket;
                    }

                    bucket.Add(item);
                }

                return buckets;
            }

            private bool HasCollapsedColumnGroups()
            {
                if (options.GroupState != null)
                {
                    foreach (var pair in options.GroupState)
                    {
                        if (pair.Value)
                        {
                            return true;
                        }
                    }
                }

                return options.AllowDrillDown && !options.ExpandGroupsByDefault;
            }

            private string FindLeafKeyByTree(TItem item, List<Func<object, object>> getters)
            {
                var node = columnTree;

                if (node == null)
                {
                    return null;
                }

                var path = new List<object>(columnFields.Count);
                var level = 0;

                while (node.Children.Count > 0 && level < columnFields.Count)
                {
                    var valueKey = Normalize(getters[level](item));

                    Node<TItem> match = null;
                    Node<TItem> others = null;

                    foreach (var child in node.Children)
                    {
                        if (child.IsOthers)
                        {
                            others = child;
                        }
                        else if (Normalize(child.Value) == valueKey)
                        {
                            match = child;
                            break;
                        }
                    }

                    var next = match ?? others;

                    if (next == null)
                    {
                        return null;
                    }

                    path.Add(next.Value);
                    node = next;
                    level++;
                }

                return node.Children.Count == 0 && path.Count > 0 ? PathKey(path) : null;
            }
        }
    }
}
