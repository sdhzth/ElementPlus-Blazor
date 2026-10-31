using System;
using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// 单个字段的筛选条件。对应参考实现中挂在 <c>RadzenPivotField</c> 上的
    /// <c>FilterValue</c>/<c>FilterOperator</c>/<c>SecondFilterValue</c>/<c>SecondFilterOperator</c>/<c>LogicalFilterOperator</c>
    /// 五个参数，这里收敛为一个可序列化、可直接单测的纯数据对象。
    /// </summary>
    public sealed class ElPivotFilterDescriptor
    {
        /// <summary>被筛选的属性路径。</summary>
        public string Property { get; set; }

        /// <summary>第一个条件的运算符。</summary>
        public ElPivotFilterOperator Operator { get; set; } = ElPivotFilterOperator.Equals;

        /// <summary>第一个条件的比较值。</summary>
        public object Value { get; set; }

        /// <summary>第二个条件的运算符（仅在 <see cref="HasSecond"/> 为 <c>true</c> 时参与）。</summary>
        public ElPivotFilterOperator SecondOperator { get; set; } = ElPivotFilterOperator.Equals;

        /// <summary>第二个条件的比较值。</summary>
        public object SecondValue { get; set; }

        /// <summary>连接两个条件的逻辑运算符。</summary>
        public ElPivotLogicalOperator LogicalOperator { get; set; } = ElPivotLogicalOperator.And;

        /// <summary>字符串比较的大小写敏感性。</summary>
        public ElPivotFilterCaseSensitivity CaseSensitivity { get; set; } = ElPivotFilterCaseSensitivity.Default;

        /// <summary>属性类型（用于筛选运算符的可用集合判断与比较值强制转换）。</summary>
        public Type Type { get; set; }

        /// <summary>
        /// 是否启用第二个条件。要求已设置 <see cref="SecondValue"/>（一元运算符除外）。
        /// </summary>
        public bool HasSecond
        {
            get
            {
                if (SecondValue != null)
                {
                    return true;
                }

                return !ElPivotFilterOperators.RequiresValue(SecondOperator);
            }
        }

        /// <summary>
        /// 该筛选条件是否为空（两个条件都不生效）。
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                var first = Value != null || !ElPivotFilterOperators.RequiresValue(Operator);
                var second = HasSecond;
                return !first && !second;
            }
        }

        /// <summary>
        /// 是否需要字符串比较时区分大小写。
        /// </summary>
        public bool IsCaseSensitive
        {
            get { return CaseSensitivity == ElPivotFilterCaseSensitivity.CaseSensitive; }
        }

        /// <summary>
        /// 创建一份浅拷贝（<see cref="Value"/> / <see cref="SecondValue"/> 为不可变引用，无需深拷贝）。
        /// </summary>
        public ElPivotFilterDescriptor Clone()
        {
            return new ElPivotFilterDescriptor
            {
                Property = Property,
                Operator = Operator,
                Value = Value,
                SecondOperator = SecondOperator,
                SecondValue = SecondValue,
                LogicalOperator = LogicalOperator,
                CaseSensitivity = CaseSensitivity,
                Type = Type
            };
        }

        /// <summary>
        /// 清空两个条件的比较值（保留运算符）。
        /// </summary>
        public void Clear()
        {
            Value = null;
            SecondValue = null;
        }
    }

    /// <summary>
    /// 排序描述信息（供事件与状态回传使用）。
    /// </summary>
    public sealed class ElPivotSortDescriptor
    {
        /// <summary>属性路径。</summary>
        public string Property { get; set; }

        /// <summary>排序方向。</summary>
        public ElPivotSortOrder SortOrder { get; set; }

        /// <summary>该排序来自哪个区域：<c>row</c> / <c>column</c> / <c>aggregate</c>。</summary>
        public string Scope { get; set; }
    }

    /// <summary>
    /// 引擎输入：一个行字段或列字段的完整规格。
    /// </summary>
    public sealed class ElPivotFieldSpec
    {
        /// <summary>属性路径。</summary>
        public string Property { get; set; }

        /// <summary>显示标题；为 <c>null</c> 时回退到 <see cref="Property"/>。</summary>
        public string Title { get; set; }

        /// <summary>排序方向；<c>null</c> 表示按首次出现顺序。</summary>
        public ElPivotSortOrder? SortOrder { get; set; }

        /// <summary>该层级最多显示的分组数；超出部分合并为「其他」。<c>null</c> 或 <c>&lt;= 0</c> 表示不限制。</summary>
        public int? MaxGroups { get; set; }

        /// <summary>「其他」分组的标题；为 <c>null</c> 时使用网格级的 <c>OthersText</c>。</summary>
        public string OthersLabel { get; set; }

        /// <summary>该字段的筛选条件；<c>null</c> 表示不筛选。</summary>
        public ElPivotFilterDescriptor Filter { get; set; }

        /// <summary>列宽（仅列字段使用，原样写入表头 <c>style="width:…"</c>）。</summary>
        public string Width { get; set; }

        /// <summary>属性的静态类型（用于筛选运算符选择与比较值转换）；可为 <c>null</c>。</summary>
        public Type PropertyType { get; set; }

        /// <summary>标题解析结果（<see cref="Title"/> 为空时回退 <see cref="Property"/>）。</summary>
        public string ResolveTitle()
        {
            return string.IsNullOrEmpty(Title) ? Property : Title;
        }

        /// <summary>创建一份拷贝。</summary>
        public ElPivotFieldSpec Clone()
        {
            return new ElPivotFieldSpec
            {
                Property = Property,
                Title = Title,
                SortOrder = SortOrder,
                MaxGroups = MaxGroups,
                OthersLabel = OthersLabel,
                Filter = Filter,
                Width = Width,
                PropertyType = PropertyType
            };
        }
    }

    /// <summary>
    /// 引擎输入：一个值字段（度量）的完整规格。
    /// </summary>
    public sealed class ElPivotAggregateSpec
    {
        /// <summary>属性路径。<c>null</c> 表示对记录本身计数（<see cref="ElPivotAggregateFunction.Count"/>）。</summary>
        public string Property { get; set; }

        /// <summary>显示标题；为 <c>null</c> 时使用 <c>标题 (函数名)</c> 形式。</summary>
        public string Title { get; set; }

        /// <summary>聚合函数。</summary>
        public ElPivotAggregateFunction Function { get; set; } = ElPivotAggregateFunction.Sum;

        /// <summary>属性静态类型；为 <c>null</c> 时交由取值结果推断。</summary>
        public Type PropertyType { get; set; }

        /// <summary>
        /// 聚合结果的排序方向。任一值字段设置了该方向时，行分组按该聚合值排序（与参考实现一致）。
        /// </summary>
        public ElPivotSortOrder? SortOrder { get; set; }

        /// <summary>该值字段的筛选条件（筛选应用于参与聚合的记录）。</summary>
        public ElPivotFilterDescriptor Filter { get; set; }

        /// <summary>创建一份拷贝。</summary>
        public ElPivotAggregateSpec Clone()
        {
            return new ElPivotAggregateSpec
            {
                Property = Property,
                Title = Title,
                Function = Function,
                PropertyType = PropertyType,
                SortOrder = SortOrder,
                Filter = Filter
            };
        }
    }

    /// <summary>
    /// 字段选择器中「分组字段」的运行时选中项（行/列共用）。
    /// <see cref="Property"/> 与 <see cref="Function"/> 一一对应一份 <see cref="ElPivotAggregateSpec"/>。
    /// </summary>
    public sealed class ElPivotSelectedField
    {
        /// <summary>属性路径。</summary>
        public string Property { get; set; }

        /// <summary>聚合函数（仅值字段使用，对应参考实现的 <c>selectedAggregates</c>）。</summary>
        public ElPivotAggregateFunction Function { get; set; } = ElPivotAggregateFunction.Count;

        /// <summary>属性类型。</summary>
        public Type PropertyType { get; set; }

        /// <summary>创建一份拷贝。</summary>
        public ElPivotSelectedField Clone()
        {
            return new ElPivotSelectedField
            {
                Property = Property,
                Function = Function,
                PropertyType = PropertyType
            };
        }
    }

    /// <summary>
    /// 提供给表头模板的数据上下文（替代参考实现里的 <c>GroupResult</c>，不引入 Radzen 类型）。
    /// </summary>
    public sealed class ElPivotGroupContext
    {
        /// <summary>该分组的键值。</summary>
        public object Key { get; set; }

        /// <summary>该分组的显示标题。</summary>
        public string Title { get; set; }

        /// <summary>该分组包含的原始记录条数。</summary>
        public int Count { get; set; }

        /// <summary>该分组所在的层级（从 0 开始）。</summary>
        public int Level { get; set; }

        /// <summary>对应的属性路径。</summary>
        public string Property { get; set; }

        /// <summary>该分组是否被折叠。</summary>
        public bool IsCollapsed { get; set; }

        /// <summary>该分组路径的稳定键。</summary>
        public string PathKey { get; set; }
    }

    /// <summary>
    /// 一个列头单元格（含跨行/跨列信息）。行头单元格也用同一结构。
    /// </summary>
    public sealed class ElPivotHeaderCell
    {
        /// <summary>单元格值（分组键）。</summary>
        public object Value { get; set; }

        /// <summary>单元格显示标题。</summary>
        public string Title { get; set; }

        /// <summary>横跨的列数（已乘以值字段数量，可直接用于 <c>colspan</c>）。</summary>
        public int ColSpan { get; set; } = 1;

        /// <summary>
        /// 纵向跨越的表头行数。
        /// <para>
        /// 只统计**实际渲染出来的**列字段表头行（<see cref="ElPivotLayout{TItem}.HeaderRows"/>），
        /// 不含额外的「值字段名表头行」（<c>金额(求和)</c> 那一行）。列分组被折叠、深层表头行整行为空
        /// 而被裁掉时，这里会收敛为剩余行数，避免跨行数溢出到值表头行、把值表头挤到表格右侧。
        /// </para>
        /// </summary>
        public int RowSpan { get; set; } = 1;

        /// <summary>该单元格所属的字段层级（从 0 开始，映射到 <c>Columns</c>/<c>Rows</c> 集合下标）。</summary>
        public int Level { get; set; }

        /// <summary>列宽（来自字段的 <c>Width</c>）。</summary>
        public string Width { get; set; }

        /// <summary>该分组是否被折叠（叶子节点为 <c>false</c>）。</summary>
        public bool IsCollapsed { get; set; }

        /// <summary>该分组下是否还有更深层级（决定是否渲染下钻箭头）。</summary>
        public bool HasChildren { get; set; }

        /// <summary>分组路径键；叶子节点为 <c>null</c>。</summary>
        public string PathKey { get; set; }

        /// <summary>是否为 <c>MaxGroups</c> 产生的「其他」分组。</summary>
        public bool IsOthers { get; set; }

        /// <summary>该分组包含的记录条数。</summary>
        public int GroupCount { get; set; }

        /// <summary>参与渲染模板的上下文（仅分组单元格有值）。</summary>
        public ElPivotGroupContext Context { get; set; }
    }

    /// <summary>
    /// 表头的一行单元格。
    /// </summary>
    public sealed class ElPivotHeaderRow
    {
        /// <summary>该行在表头中的下标（从 0 开始）。</summary>
        public int Index { get; set; }

        /// <summary>该行的单元格集合（已按渲染顺序排列）。</summary>
        public List<ElPivotHeaderCell> Cells { get; } = new List<ElPivotHeaderCell>();
    }

    /// <summary>
    /// 数据区某一行的行头单元格。
    /// </summary>
    public sealed class ElPivotRowHeaderCell
    {
        /// <summary>单元格值（分组键）。</summary>
        public object Value { get; set; }

        /// <summary>单元格显示标题。</summary>
        public string Title { get; set; }

        /// <summary>占位符（补齐深度时使用的空单元格）。</summary>
        public bool IsPlaceholder { get; set; }

        /// <summary>纵向跨越的数据行数。</summary>
        public int RowSpan { get; set; } = 1;

        /// <summary>所属字段层级（从 0 开始）。</summary>
        public int Level { get; set; }

        /// <summary>该分组是否被折叠。</summary>
        public bool IsCollapsed { get; set; }

        /// <summary>该分组下是否还有更深层级。</summary>
        public bool HasChildren { get; set; }

        /// <summary>分组路径键；叶子节点为 <c>null</c>。</summary>
        public string PathKey { get; set; }

        /// <summary>是否为「其他」分组。</summary>
        public bool IsOthers { get; set; }

        /// <summary>该分组包含的记录条数。</summary>
        public int GroupCount { get; set; }

        /// <summary>参与渲染模板的上下文（仅分组单元格有值）。</summary>
        public ElPivotGroupContext Context { get; set; }
    }

    /// <summary>
    /// 数据区的一行。
    /// </summary>
    public sealed class ElPivotBodyRow<TItem>
    {
        /// <summary>行头单元格（已补齐到行头最大深度）。</summary>
        public List<ElPivotRowHeaderCell> HeaderCells { get; } = new List<ElPivotRowHeaderCell>();

        /// <summary>
        /// 值单元格。长度固定为 <c>ColumnLeaves.Count × Aggregates.Count</c>，
        /// 下标为 <c>leafIndex * Aggregates.Count + aggregateIndex</c>。
        /// </summary>
        public List<object> ValueCells { get; } = new List<object>();

        /// <summary>行合计单元格，长度等于值字段数量。</summary>
        public List<object> RowTotals { get; } = new List<object>();

        /// <summary>该行（分组）包含的记录。</summary>
        public List<TItem> Items { get; set; } = new List<TItem>();

        /// <summary>该行的分组路径值（按行字段顺序）。</summary>
        public List<object> PathValues { get; set; } = new List<object>();

        /// <summary>该行是否为折叠分组产生的汇总行（缺少更深层级的行头）。</summary>
        public bool IsCollapsedGroup { get; set; }
    }

    /// <summary>
    /// 引擎输出：完整的交叉表布局。这是渲染的**单一真源**——
    /// 组件只负责把本对象翻译成 HTML，全部计算都在纯 C# 引擎里完成，可脱离浏览器单测。
    /// </summary>
    public sealed class ElPivotLayout<TItem>
    {
        /// <summary>参与计算的记录（已应用筛选）。</summary>
        public List<TItem> Items { get; set; } = new List<TItem>();

        /// <summary>表头各行。</summary>
        public List<ElPivotHeaderRow> HeaderRows { get; } = new List<ElPivotHeaderRow>();

        /// <summary>列叶子路径集合，顺序与值单元格列顺序一致。</summary>
        public List<List<object>> ColumnLeaves { get; } = new List<List<object>>();

        /// <summary>数据区各行。</summary>
        public List<ElPivotBodyRow<TItem>> BodyRows { get; } = new List<ElPivotBodyRow<TItem>>();

        /// <summary>列合计：键为列叶子路径键，值为该列各值字段的合计（顺序同值字段）。</summary>
        public Dictionary<string, List<object>> ColumnTotals { get; } = new Dictionary<string, List<object>>(StringComparer.Ordinal);

        /// <summary>右下角总计：各值字段在全部记录上的合计。</summary>
        public List<object> GrandTotals { get; } = new List<object>();

        /// <summary>行头的最大深度（即补齐后的行头单元格数量）。</summary>
        public int RowHeaderDepth { get; set; }

        /// <summary>实际渲染的表头行数（已去掉尾部空行）。</summary>
        public int HeaderRowCount { get; set; }

        /// <summary>列字段是否产生了「其他」分组（影响列叶子定位算法）。</summary>
        public bool HasOthersColumns { get; set; }

        /// <summary>数据区是否为空（没有任何行）。</summary>
        public bool IsEmpty
        {
            get { return BodyRows.Count == 0; }
        }

        /// <summary>
        /// 根据列叶子路径计算值单元格的起始下标。
        /// </summary>
        public int ValueCellIndex(int leafIndex, int aggregateIndex, int aggregateCount)
        {
            return (leafIndex * aggregateCount) + aggregateIndex;
        }
    }
}
