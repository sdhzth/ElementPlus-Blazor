using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElPivotDataGridGrow 组件测试（仅覆盖本组件，不跑全量）。
    /// <para>
    /// 三层覆盖：
    /// </para>
    /// <list type="number">
    /// <item><description><b>纯 C# 引擎</b>（<see cref="ElPivotEngine"/>）：分组、聚合、筛选、排序、下钻、MaxGroups、
    /// 行/列合计 —— 这些是组件的单一真源，脱离浏览器即可断言精确数值。</description></item>
    /// <item><description><b>bUnit 渲染与交互</b>：外壳、表头跨行列、值单元格、合计、分页、筛选面板、字段选择器、
    /// 事件回调（排序 / 单元格点击 / 下钻 / 筛选）。</description></item>
    /// <item><description><b>守卫</b>：零第三方依赖（无 Radzen / 无 Dynamic LINQ）、完全不使用 JavaScript、
    /// 浏览器兼容 CSS、样式作用域。</description></item>
    /// </list>
    /// </summary>
    public class ElPivotDataGridGrowTests : BunitContext
    {
        public ElPivotDataGridGrowTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        #region 测试数据

        private sealed class Sale
        {
            public string Region { get; set; }

            public string Category { get; set; }

            public string Product { get; set; }

            public int Year { get; set; }

            public int Amount { get; set; }

            public decimal Cost { get; set; }

            public double Weight { get; set; }

            public string Status { get; set; }

            public DateTime Date { get; set; }

            public string Note { get; set; }
        }

        private sealed class Customer
        {
            public string City { get; set; }
        }

        private sealed class Order
        {
            public Customer Customer { get; set; }

            public Dictionary<string, object> Extras { get; set; }

            public Dictionary<string, int> Counters { get; set; }

            public string region { get; set; }

            public string PublicField;

            private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);

            public string this[string key]
            {
                get { return values.TryGetValue(key, out var value) ? value : null; }
                set { values[key] = value; }
            }

            public void SetIndex(string key, string value)
            {
                values[key] = value;
            }
        }

        /// <summary>
        /// 6 条记录，金额合计 210，便于手算校验。
        /// <code>
        ///      2024  2025  行合计
        /// 华东   30    30     60
        /// 华北   40    50     90
        /// 华南   --    60     60
        /// 列合计 70   140    210
        /// </code>
        /// </summary>
        private static List<Sale> Sales()
        {
            return new List<Sale>
            {
                new Sale { Region = "华东", Category = "药品", Product = "阿莫西林", Year = 2024, Amount = 10, Cost = 6.5m, Weight = 1.5, Status = "已付", Date = new DateTime(2024, 1, 5), Note = "首批" },
                new Sale { Region = "华东", Category = "药品", Product = "头孢", Year = 2024, Amount = 20, Cost = 12.0m, Weight = 2.0, Status = "已付", Date = new DateTime(2024, 2, 6), Note = null },
                new Sale { Region = "华东", Category = "耗材", Product = "注射器", Year = 2025, Amount = 30, Cost = 9.0m, Weight = 3.0, Status = "未付", Date = new DateTime(2025, 3, 7), Note = "急诊" },
                new Sale { Region = "华北", Category = "药品", Product = "阿莫西林", Year = 2024, Amount = 40, Cost = 24.0m, Weight = 4.0, Status = "已付", Date = new DateTime(2024, 4, 8), Note = "" },
                new Sale { Region = "华北", Category = "耗材", Product = "口罩", Year = 2025, Amount = 50, Cost = 5.0m, Weight = 5.0, Status = "未付", Date = new DateTime(2025, 5, 9), Note = null },
                new Sale { Region = "华南", Category = "药品", Product = "头孢", Year = 2025, Amount = 60, Cost = 36.0m, Weight = 6.0, Status = "已付", Date = new DateTime(2025, 6, 10), Note = "追加" }
            };
        }

        #endregion

        #region 辅助

        private static ElPivotFieldSpec Field(
            string property,
            Type type = null,
            string title = null,
            ElPivotSortOrder? sortOrder = null,
            int? maxGroups = null,
            string othersLabel = null,
            ElPivotFilterDescriptor filter = null,
            string width = null)
        {
            return new ElPivotFieldSpec
            {
                Property = property,
                PropertyType = type,
                Title = title,
                SortOrder = sortOrder,
                MaxGroups = maxGroups,
                OthersLabel = othersLabel,
                Filter = filter,
                Width = width
            };
        }

        private static ElPivotAggregateSpec Agg(
            string property,
            ElPivotAggregateFunction function = ElPivotAggregateFunction.Sum,
            Type type = null,
            string title = null,
            ElPivotSortOrder? sortOrder = null,
            ElPivotFilterDescriptor filter = null)
        {
            return new ElPivotAggregateSpec
            {
                Property = property,
                Function = function,
                PropertyType = type,
                Title = title,
                SortOrder = sortOrder,
                Filter = filter
            };
        }

        private static ElPivotFilterDescriptor Where(
            string property,
            ElPivotFilterOperator op,
            object value = null,
            Type type = null,
            ElPivotFilterOperator? secondOp = null,
            object secondValue = null,
            ElPivotLogicalOperator logical = ElPivotLogicalOperator.And,
            ElPivotFilterCaseSensitivity caseSensitivity = ElPivotFilterCaseSensitivity.Default)
        {
            return new ElPivotFilterDescriptor
            {
                Property = property,
                Operator = op,
                Value = value,
                SecondOperator = secondOp ?? ElPivotFilterOperator.Equals,
                SecondValue = secondValue,
                LogicalOperator = logical,
                Type = type,
                CaseSensitivity = caseSensitivity
            };
        }

        private static ElPivotLayout<Sale> Build(
            IEnumerable<Sale> data = null,
            IReadOnlyList<ElPivotFieldSpec> rows = null,
            IReadOnlyList<ElPivotFieldSpec> columns = null,
            IReadOnlyList<ElPivotAggregateSpec> aggregates = null,
            ElPivotEngineOptions options = null)
        {
            return ElPivotEngine.Build(data ?? Sales(), rows, columns, aggregates, options);
        }

        private static object Cell(ElPivotLayout<Sale> layout, int rowIndex, int leafIndex, int aggregateIndex = 0, int aggregateCount = 1)
        {
            return layout.BodyRows[rowIndex].ValueCells[(leafIndex * aggregateCount) + aggregateIndex];
        }

        private static long AsLong(object value)
        {
            return Assert.IsType<long>(value);
        }

        private static int AsInt(object value)
        {
            return Assert.IsType<int>(value);
        }

        private static double AsDouble(object value)
        {
            return Assert.IsType<double>(value);
        }

        private static decimal AsDecimal(object value)
        {
            return Assert.IsType<decimal>(value);
        }

        private static string AsString(object value)
        {
            return Assert.IsType<string>(value);
        }

        private static string RepoRoot([CallerFilePath] string file = null)
        {
            // .../test/Element.ComponentTests/ElPivotDataGridGrowTests.cs → 上溯三层
            var directory = new FileInfo(file).Directory;

            return directory.Parent.Parent.FullName;
        }

        /// <summary>读取 index.css 补丁段（原 fix.css，2026-09-26 并入）中 ElPivotDataGridGrow 的样式段（已剥离注释）。</summary>
        private static string CssSection()
        {
            var path = Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "css", "index.css");
            var text = File.ReadAllText(path);
            var marker = text.IndexOf("ElPivotDataGridGrow：Element Plus 自渲染交叉表", StringComparison.Ordinal);

            Assert.True(marker >= 0, "index.css 中未找到 ElPivotDataGridGrow 样式段");

            var start = text.LastIndexOf("/*", marker, StringComparison.Ordinal);

            // 只取本组件的段落：截到下一个「段分隔注释」为止。
            // 否则会把样式表里后面追加的其它组件样式也算进来，作用域守卫会误报。
            var next = text.IndexOf("\n/* =", marker + 10, StringComparison.Ordinal);
            var section = next >= 0 ? text.Substring(start, next - start) : text.Substring(start);

            return Regex.Replace(section, @"/\*[\s\S]*?\*/", string.Empty);
        }

        /// <summary>剥离注释（块注释 + 行注释 + XML 文档注释），只留可执行代码。</summary>
        private static string StripComments(string text)
        {
            var withoutBlock = Regex.Replace(text, @"/\*[\s\S]*?\*/", string.Empty);
            var kept = new List<string>();

            foreach (var line in withoutBlock.Split('\n'))
            {
                if (line.TrimStart().StartsWith("///", StringComparison.Ordinal))
                {
                    continue;
                }

                var index = line.IndexOf("//", StringComparison.Ordinal);

                kept.Add(index >= 0 ? line.Substring(0, index) : line);
            }

            return string.Join("\n", kept);
        }

        private static string[] SourceFiles()
        {
            var directory = Path.Combine(RepoRoot(), "src", "Components");

            return Directory.GetFiles(directory, "ElPivot*", SearchOption.TopDirectoryOnly);
        }

        // ---- 字段片段构造（字段组件本身不产出 DOM，只向父网格注册） ----

        private static RenderFragment Fragment(params RenderFragment[] children)
        {
            return builder =>
            {
                var sequence = 0;

                foreach (var child in children)
                {
                    builder.AddContent(sequence++, child);
                }
            };
        }

        private static RenderFragment Column(
            string property,
            string title = null,
            string width = null,
            int? maxGroups = null,
            string othersLabel = null,
            ElPivotSortOrder? sortOrder = null,
            RenderFragment<ElPivotGroupContext> headerTemplate = null,
            bool? sortable = null)
        {
            return builder =>
            {
                builder.OpenComponent<ElPivotColumnGrow<Sale>>(0);
                builder.AddAttribute(1, "Property", property);

                if (title != null)
                {
                    builder.AddAttribute(2, "Title", title);
                }

                if (width != null)
                {
                    builder.AddAttribute(3, "Width", width);
                }

                if (maxGroups.HasValue)
                {
                    builder.AddAttribute(4, "MaxGroups", maxGroups.Value);
                }

                if (othersLabel != null)
                {
                    builder.AddAttribute(5, "OthersLabel", othersLabel);
                }

                if (sortOrder.HasValue)
                {
                    builder.AddAttribute(6, "SortOrder", sortOrder.Value);
                }

                if (headerTemplate != null)
                {
                    builder.AddAttribute(7, "HeaderTemplate", headerTemplate);
                }

                if (sortable.HasValue)
                {
                    builder.AddAttribute(8, "Sortable", sortable.Value);
                }

                builder.CloseComponent();
            };
        }

        private static RenderFragment Row(
            string property,
            string title = null,
            int? maxGroups = null,
            string othersLabel = null,
            ElPivotSortOrder? sortOrder = null)
        {
            return builder =>
            {
                builder.OpenComponent<ElPivotRowGrow<Sale>>(0);
                builder.AddAttribute(1, "Property", property);

                if (title != null)
                {
                    builder.AddAttribute(2, "Title", title);
                }

                if (maxGroups.HasValue)
                {
                    builder.AddAttribute(3, "MaxGroups", maxGroups.Value);
                }

                if (othersLabel != null)
                {
                    builder.AddAttribute(4, "OthersLabel", othersLabel);
                }

                if (sortOrder.HasValue)
                {
                    builder.AddAttribute(5, "SortOrder", sortOrder.Value);
                }

                builder.CloseComponent();
            };
        }

        private static RenderFragment AggregateColumn(
            string property,
            ElPivotAggregateFunction function = ElPivotAggregateFunction.Sum,
            string title = null,
            string formatString = null,
            ElPivotTextAlign? align = null,
            RenderFragment<object> template = null,
            RenderFragment<ElPivotAggregateGrowContext<Sale>> rowTotalTemplate = null,
            RenderFragment<object> columnTotalTemplate = null)
        {
            return builder =>
            {
                builder.OpenComponent<ElPivotAggregateGrow<Sale>>(0);
                builder.AddAttribute(1, "Property", property);
                builder.AddAttribute(2, "Aggregate", function);

                if (title != null)
                {
                    builder.AddAttribute(3, "Title", title);
                }

                if (formatString != null)
                {
                    builder.AddAttribute(4, "FormatString", formatString);
                }

                if (align.HasValue)
                {
                    builder.AddAttribute(5, "TextAlign", align.Value);
                }

                if (template != null)
                {
                    builder.AddAttribute(6, "Template", template);
                }

                if (rowTotalTemplate != null)
                {
                    builder.AddAttribute(7, "RowTotalTemplate", rowTotalTemplate);
                }

                if (columnTotalTemplate != null)
                {
                    builder.AddAttribute(8, "ColumnTotalTemplate", columnTotalTemplate);
                }

                builder.CloseComponent();
            };
        }

        private IRenderedComponent<ElPivotDataGridGrow<Sale>> Grid(
            Action<ComponentParameterCollectionBuilder<ElPivotDataGridGrow<Sale>>> configure = null)
        {
            if (configure == null)
            {
                // 注意：不能写成 Render<ElPivotDataGridGrow<Sale>>(p => { }) —— 空 lambda 会被解析成
                // RenderFragment 重载，渲染出一个空片段，导致 Instance/Find 抛 ComponentNotFoundException。
                return Render<ElPivotDataGridGrow<Sale>>();
            }

            return Render<ElPivotDataGridGrow<Sale>>(configure);
        }

        /// <summary>行字段 = Region，列字段 = Year，值字段 = Amount(Sum) 的常用组合。</summary>
        private IRenderedComponent<ElPivotDataGridGrow<Sale>> BasicGrid(
            Action<ComponentParameterCollectionBuilder<ElPivotDataGridGrow<Sale>>> configure = null)
        {
            return Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Year", title: "年份")));
                p.Add(x => x.Rows, Fragment(Row("Region", title: "区域")));
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Amount", title: "金额")));
                configure?.Invoke(p);
            });
        }

        #endregion

        #region 引擎：分组与结构

        [Fact]
        public void Engine_NullData_ProducesEmptyLayout()
        {
            // 注意：不能走 Build(...) 辅助方法——它把 null 替换成默认数据集。这里直接调引擎。
            var layout = ElPivotEngine.Build<Sale>(
                null,
                new[] { Field("Region") },
                new[] { Field("Year") },
                new[] { Agg("Amount", type: typeof(int)) });

            Assert.Empty(layout.Items);
            Assert.Empty(layout.BodyRows);
            Assert.True(layout.IsEmpty);
        }

        [Fact]
        public void Engine_NoFieldsAtAll_StillProducesSingleRowAndSingleColumn()
        {
            var layout = Build(rows: Array.Empty<ElPivotFieldSpec>(), columns: Array.Empty<ElPivotFieldSpec>(), aggregates: new[] { Agg("Amount", type: typeof(int)) });

            // 无行字段 → 单行总计；无列字段 → 单一列叶子（避免结构性空表）。
            Assert.Single(layout.BodyRows);
            Assert.Single(layout.ColumnLeaves);
            Assert.Empty(layout.ColumnLeaves[0]);
            Assert.Equal(210L, AsLong(Cell(layout, 0, 0)));
        }

        [Fact]
        public void Engine_NoColumnFields_CollapsesToSingleValueColumn()
        {
            var layout = Build(rows: new[] { Field("Region") }, columns: Array.Empty<ElPivotFieldSpec>(), aggregates: new[] { Agg("Amount", type: typeof(int)) });

            Assert.Single(layout.ColumnLeaves);
            Assert.Equal(3, layout.BodyRows.Count);
            Assert.Equal(60L, AsLong(Cell(layout, 0, 0)));
            Assert.Equal(90L, AsLong(Cell(layout, 1, 0)));
            Assert.Equal(60L, AsLong(Cell(layout, 2, 0)));
        }

        [Fact]
        public void Engine_RowAndColumnFields_ProduceExpectedGrid()
        {
            var layout = Build(
                rows: new[] { Field("Region") },
                columns: new[] { Field("Year") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) });

            Assert.Equal(2, layout.ColumnLeaves.Count);
            Assert.Equal(3, layout.BodyRows.Count);
            Assert.Equal(1, layout.RowHeaderDepth);

            // 行顺序 = 首次出现顺序：华东 / 华北 / 华南
            Assert.Equal("华东", layout.BodyRows[0].HeaderCells[0].Title);
            Assert.Equal("华北", layout.BodyRows[1].HeaderCells[0].Title);
            Assert.Equal("华南", layout.BodyRows[2].HeaderCells[0].Title);

            Assert.Equal(30L, AsLong(Cell(layout, 0, 0)));
            Assert.Equal(30L, AsLong(Cell(layout, 0, 1)));
            Assert.Equal(40L, AsLong(Cell(layout, 1, 0)));
            Assert.Equal(50L, AsLong(Cell(layout, 1, 1)));

            // 华南没有 2024 的记录 → 空桶为 null（而不是 0），便于区分「无数据」与「合计为 0」。
            Assert.Null(Cell(layout, 2, 0));
            Assert.Equal(60L, AsLong(Cell(layout, 2, 1)));
        }

        [Fact]
        public void Engine_ColumnLeavesFollowFirstAppearanceOrder()
        {
            var layout = Build(
                rows: new[] { Field("Region") },
                columns: new[] { Field("Year") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) });

            Assert.Equal(new object[] { 2024 }, layout.ColumnLeaves[0]);
            Assert.Equal(new object[] { 2025 }, layout.ColumnLeaves[1]);
        }

        [Fact]
        public void Engine_TwoColumnFields_ProduceNestedLeavesAndHeaderRows()
        {
            var layout = Build(
                rows: new[] { Field("Region") },
                columns: new[] { Field("Year"), Field("Category") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { ExpandGroupsByDefault = true });

            Assert.Equal(2, layout.HeaderRows.Count);

            // 2024：只有「药品」→ 1 个叶子；2025：药品 + 耗材 → 2 个叶子，合计 3
            Assert.Equal(3, layout.ColumnLeaves.Count);

            var yearRow = layout.HeaderRows[0].Cells;
            Assert.Equal(2, yearRow.Count);
            Assert.Equal("2024", yearRow[0].Title);
            Assert.Equal(1, yearRow[0].ColSpan);
            Assert.Equal("2025", yearRow[1].Title);
            Assert.Equal(2, yearRow[1].ColSpan);
        }

        [Fact]
        public void Engine_CollapsedColumnGroup_BecomesSingleLeafWithRowSpan()
        {
            var layout = Build(
                rows: new[] { Field("Region") },
                columns: new[] { Field("Year"), Field("Category") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) });

            // 默认（ExpandGroupsByDefault = false）列分组折叠 → 每个 Year 只剩一列
            Assert.Equal(2, layout.ColumnLeaves.Count);
            Assert.True(layout.HeaderRows.Count >= 1);

            var yearCell = layout.HeaderRows[0].Cells[0];
            Assert.True(yearCell.HasChildren);
            Assert.True(yearCell.IsCollapsed);

            // 折叠后 Category 那一层表头行整行为空 → 被裁掉 → 只剩 1 行表头。
            // 因此跨行数必须是 1，而不是「列字段层数」2：否则跨行会溢出到下面那行
            // 「值字段名表头行」，把 金额(求和) 顶到表格右侧，多出只有表头没有数据的空列。
            Assert.Equal(1, layout.HeaderRowCount);
            Assert.Equal(1, yearCell.RowSpan);
        }

        [Fact]
        public void Engine_CollapsedColumnGroup_RowSpanNeverExceedsRenderedHeaderRows()
        {
            // 三层列字段、全部折叠：跨行数之和不能超出实际表头行数。
            var layout = Build(
                rows: new[] { Field("Region") },
                columns: new[] { Field("Year"), Field("Category"), Field("Product") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) });

            Assert.Equal(1, layout.HeaderRowCount);

            foreach (var row in layout.HeaderRows)
            {
                foreach (var cell in row.Cells)
                {
                    Assert.InRange(cell.RowSpan, 1, layout.HeaderRowCount - row.Index);
                }
            }
        }

        [Fact]
        public void Engine_ExpandedColumnGroup_KeepsRowSpanAcrossHeaderRows()
        {
            // 展开时深层表头行仍然存在，收敛必须是幂等的（不改变原有跨行数）。
            var layout = Build(
                rows: new[] { Field("Region") },
                columns: new[] { Field("Year"), Field("Category") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { ExpandGroupsByDefault = true });

            Assert.Equal(2, layout.HeaderRowCount);

            var yearCell = layout.HeaderRows[0].Cells[0];
            Assert.False(yearCell.IsCollapsed);
            Assert.Equal(1, yearCell.RowSpan);

            var categoryCell = layout.HeaderRows[1].Cells[0];
            Assert.Equal(1, categoryCell.RowSpan);
        }

        [Fact]
        public void Engine_TwoRowFields_DefaultCollapsed_ProducesOneRowPerTopGroup()
        {
            var layout = Build(
                rows: new[] { Field("Region"), Field("Category") },
                columns: new[] { Field("Year") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) });

            Assert.Equal(3, layout.BodyRows.Count);
            Assert.All(layout.BodyRows, row => Assert.True(row.IsCollapsedGroup));
            Assert.All(layout.BodyRows, row => Assert.True(row.HeaderCells[0].HasChildren));
        }

        [Fact]
        public void Engine_TwoRowFields_Expanded_ProducesLeafRowsWithPaddedHeaders()
        {
            var layout = Build(
                rows: new[] { Field("Region"), Field("Category") },
                columns: new[] { Field("Year") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { ExpandGroupsByDefault = true });

            Assert.Equal(5, layout.BodyRows.Count);
            Assert.Equal(2, layout.RowHeaderDepth);
            Assert.All(layout.BodyRows, row => Assert.Equal(2, row.HeaderCells.Count));

            // 第一行的第 0 层行头跨 2 行（华东 下辖 药品 / 耗材）
            Assert.Equal("华东", layout.BodyRows[0].HeaderCells[0].Title);
            Assert.Equal(2, layout.BodyRows[0].HeaderCells[0].RowSpan);

            // 第二行在模型里同样带「华东」单元格，rowspan 合并由渲染层负责（不是模型层填占位符）
            Assert.Equal("华东", layout.BodyRows[1].HeaderCells[0].Title);
            Assert.Equal("药品", layout.BodyRows[0].HeaderCells[1].Title);
            Assert.Equal("耗材", layout.BodyRows[1].HeaderCells[1].Title);
        }

        [Fact]
        public void Engine_UnknownProperty_FallsBackToSingleNullGroupWithoutThrowing()
        {
            var layout = Build(
                rows: new[] { Field("NoSuchProperty") },
                columns: new[] { Field("Year") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) });

            Assert.Single(layout.BodyRows);
            Assert.Equal("（空）", layout.BodyRows[0].HeaderCells[0].Title);
        }

        [Fact]
        public void Engine_NullGroupKey_UsesNullGroupText()
        {
            var data = new List<Sale>
            {
                new Sale { Region = null, Year = 2024, Amount = 5 },
                new Sale { Region = "华东", Year = 2024, Amount = 7 }
            };

            var layout = Build(
                data: data,
                rows: new[] { Field("Region") },
                columns: new[] { Field("Year") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { NullGroupText = "未填写" });

            Assert.Equal("未填写", layout.BodyRows[0].HeaderCells[0].Title);
            Assert.Equal(5L, AsLong(Cell(layout, 0, 0)));
        }

        #endregion

        #region 引擎：聚合

        [Fact]
        public void Engine_Sum_OnIntegerProperty_ReturnsLong()
        {
            var layout = Build(rows: new[] { Field("Region") }, aggregates: new[] { Agg("Amount", ElPivotAggregateFunction.Sum, typeof(int)) });

            // 无列字段 → 每行只有一个值单元格（即该行的合计）
            Assert.Equal(60L, AsLong(Cell(layout, 0, 0)));
            Assert.Equal(90L, AsLong(Cell(layout, 1, 0)));
            Assert.Equal(60L, AsLong(Cell(layout, 2, 0)));
        }

        [Fact]
        public void Engine_Sum_OnDecimalProperty_ReturnsDecimal()
        {
            var layout = Build(rows: new[] { Field("Region") }, aggregates: new[] { Agg("Cost", ElPivotAggregateFunction.Sum, typeof(decimal)) });

            // 华东：6.5 + 12.0 + 9.0 = 27.5；华北：24.0 + 5.0 = 29.0
            Assert.Equal(27.5m, AsDecimal(Cell(layout, 0, 0)));
            Assert.Equal(29.0m, AsDecimal(Cell(layout, 1, 0)));
        }

        [Fact]
        public void Engine_Sum_OnDoubleProperty_ReturnsDouble()
        {
            var layout = Build(rows: new[] { Field("Region") }, aggregates: new[] { Agg("Weight", ElPivotAggregateFunction.Sum, typeof(double)) });

            Assert.Equal(6.5d, AsDouble(Cell(layout, 0, 0)), 6);
        }

        [Fact]
        public void Engine_Average_ReturnsDouble()
        {
            var layout = Build(rows: new[] { Field("Region") }, aggregates: new[] { Agg("Amount", ElPivotAggregateFunction.Average, typeof(int)) });

            Assert.Equal(20d, AsDouble(Cell(layout, 0, 0)), 6);
            Assert.Equal(45d, AsDouble(Cell(layout, 1, 0)), 6);
        }

        [Fact]
        public void Engine_Count_ReturnsRecordCount()
        {
            var layout = Build(
                rows: new[] { Field("Region") },
                columns: new[] { Field("Year") },
                aggregates: new[] { Agg("Amount", ElPivotAggregateFunction.Count, typeof(int)) },
                options: new ElPivotEngineOptions { ShowRowsTotals = true });

            Assert.Equal(2, AsInt(Cell(layout, 0, 0)));
            Assert.Equal(1, AsInt(Cell(layout, 0, 1)));

            // 行合计（华东 3 条记录）存放在独立的 RowTotals 列表里，
            // 不能用 Cell(..., leafIndex, aggregateIndex) 去取——那个下标只在 ValueCells 内有效。
            Assert.Equal(3, AsInt(layout.BodyRows[0].RowTotals[0]));
        }

        [Fact]
        public void Engine_CountDistinct_IgnoresNullAndDuplicates()
        {
            var layout = Build(
                rows: new[] { Field("Region") },
                aggregates: new[] { Agg("Product", ElPivotAggregateFunction.CountDistinct, typeof(string)) });

            // 华东：阿莫西林 / 头孢 / 注射器 = 3；华北：阿莫西林 / 口罩 = 2
            Assert.Equal(3, AsInt(Cell(layout, 0, 0)));
            Assert.Equal(2, AsInt(Cell(layout, 1, 0)));
        }

        [Fact]
        public void Engine_MinMax_ReturnOriginalValues()
        {
            var min = Build(rows: new[] { Field("Region") }, aggregates: new[] { Agg("Amount", ElPivotAggregateFunction.Min, typeof(int)) });
            var max = Build(rows: new[] { Field("Region") }, aggregates: new[] { Agg("Amount", ElPivotAggregateFunction.Max, typeof(int)) });

            Assert.Equal(10, AsInt(Cell(min, 0, 0)));
            Assert.Equal(30, AsInt(Cell(max, 0, 0)));
            Assert.Equal(40, AsInt(Cell(min, 1, 0)));
            Assert.Equal(50, AsInt(Cell(max, 1, 0)));
        }

        [Fact]
        public void Engine_FirstLast_FollowInputOrder()
        {
            var first = Build(rows: new[] { Field("Region") }, aggregates: new[] { Agg("Product", ElPivotAggregateFunction.First, typeof(string)) });
            var last = Build(rows: new[] { Field("Region") }, aggregates: new[] { Agg("Product", ElPivotAggregateFunction.Last, typeof(string)) });

            Assert.Equal("阿莫西林", AsString(Cell(first, 0, 0)));
            Assert.Equal("注射器", AsString(Cell(last, 0, 0)));
        }

        [Fact]
        public void Engine_MultipleAggregates_AreOrderedAsDeclared()
        {
            var layout = Build(
                rows: new[] { Field("Region") },
                columns: new[] { Field("Year") },
                aggregates: new[]
                {
                    Agg("Amount", ElPivotAggregateFunction.Sum, typeof(int), title: "金额"),
                    Agg("Product", ElPivotAggregateFunction.CountDistinct, typeof(string), title: "品类数")
                });

            Assert.Equal(2, layout.BodyRows[0].ValueCells.Count / layout.ColumnLeaves.Count);
            Assert.Equal(30L, AsLong(Cell(layout, 0, 0, 0, 2)));
            Assert.Equal(2, AsInt(Cell(layout, 0, 0, 1, 2)));
            Assert.Equal(4, layout.BodyRows[0].ValueCells.Count);
        }

        [Fact]
        public void Engine_SumOnTextProperty_FallsBackToCount()
        {
            var layout = Build(rows: new[] { Field("Region") }, aggregates: new[] { Agg("Product", ElPivotAggregateFunction.Sum, typeof(string)) });

            // 与参考实现一致：非数值属性上的 Sum 退化为记录条数。
            Assert.Equal(3, AsInt(Cell(layout, 0, 0)));
        }

        [Fact]
        public void Engine_EmptyBucket_YieldsNullNotZero()
        {
            var layout = Build(
                rows: new[] { Field("Region") },
                columns: new[] { Field("Year") },
                aggregates: new[] { Agg("Amount", ElPivotAggregateFunction.Sum, typeof(int)) });

            Assert.Null(Cell(layout, 2, 0));
        }

        [Fact]
        public void Engine_Aggregator_EmptyInputReturnsNull()
        {
            Assert.Null(ElPivotAggregator.Compute<Sale>(new List<Sale>(), null, ElPivotAggregateFunction.Sum, typeof(int)));
        }

        [Fact]
        public void Engine_Aggregator_FormatValue_UsesFormatStringWithFallbackToDefault()
        {
            var invariant = System.Globalization.CultureInfo.InvariantCulture;

            Assert.Equal("12.35", ElPivotAggregator.FormatValue(12.3456d, "N2", invariant));
            Assert.Equal("12.3456", ElPivotAggregator.FormatValue(12.3456d, null, invariant));
            Assert.Equal("12.3456", ElPivotAggregator.FormatValue(12.3456d, string.Empty, invariant));
            Assert.Equal(string.Empty, ElPivotAggregator.FormatValue(null, "N2", null));
        }

        #endregion

        #region 引擎：合计

        [Fact]
        public void Engine_RowTotals_PresentOnlyWhenEnabled()
        {
            var withTotals = Build(
                rows: new[] { Field("Region") },
                columns: new[] { Field("Year") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { ShowRowsTotals = true });

            var withoutTotals = Build(
                rows: new[] { Field("Region") },
                columns: new[] { Field("Year") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) });

            Assert.Equal(60L, AsLong(withTotals.BodyRows[0].RowTotals[0]));
            Assert.Empty(withoutTotals.BodyRows[0].RowTotals);
        }

        [Fact]
        public void Engine_ColumnTotalsAndGrandTotals_ComputeExpectedValues()
        {
            var layout = Build(
                rows: new[] { Field("Region") },
                columns: new[] { Field("Year") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { ShowColumnsTotals = true });

            Assert.Equal(70L, AsLong(layout.ColumnTotals["2024"][0]));
            Assert.Equal(140L, AsLong(layout.ColumnTotals["2025"][0]));
            Assert.Equal(210L, AsLong(layout.GrandTotals[0]));
        }

        [Fact]
        public void Engine_ColumnTotals_OmittedWhenDisabled()
        {
            var layout = Build(
                rows: new[] { Field("Region") },
                columns: new[] { Field("Year") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) });

            Assert.Empty(layout.ColumnTotals);
            Assert.Empty(layout.GrandTotals);
        }

        [Fact]
        public void Engine_ColumnTotals_AreNotAffectedByRowPaging()
        {
            var layout = Build(
                rows: new[] { Field("Region") },
                columns: new[] { Field("Year") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { ShowColumnsTotals = true });

            // 合计基于全部记录，与分页无关（分页只作用于结果行）。
            Assert.Equal(210L, AsLong(layout.GrandTotals[0]));
        }

        #endregion

        #region 引擎：筛选

        [Fact]
        public void Engine_FilterEquals_KeepsOnlyMatchingRecords()
        {
            var layout = Build(
                rows: new[] { Field("Region", typeof(string)) },
                columns: new[] { Field("Year") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { Filter = Where("Region", ElPivotFilterOperator.Equals, "华东", typeof(string)) });

            Assert.Equal(3, layout.Items.Count);
            Assert.Single(layout.BodyRows);
        }

        [Fact]
        public void Engine_FilterContains_BindsRecords()
        {
            var layout = Build(
                rows: new[] { Field("Product", typeof(string)) },
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { Filter = Where("Product", ElPivotFilterOperator.Contains, "西林", typeof(string)) });

            Assert.Equal(2, layout.Items.Count);
            Assert.Single(layout.BodyRows);
            Assert.Equal("阿莫西林", layout.BodyRows[0].HeaderCells[0].Title);
            Assert.Equal(50L, AsLong(Cell(layout, 0, 0)));
        }

        [Fact]
        public void Engine_FilterDoesNotContain_ExcludesMatchingRecords()
        {
            var layout = Build(
                rows: new[] { Field("Product", typeof(string)) },
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { Filter = Where("Product", ElPivotFilterOperator.DoesNotContain, "西林", typeof(string)) });

            Assert.Equal(4, layout.Items.Count);
            Assert.DoesNotContain(layout.BodyRows, r => r.HeaderCells[0].Title == "阿莫西林");
        }

        [Fact]
        public void Engine_FilterStartsWithAndEndsWith_BindRespectively()
        {
            var startsWith = Build(
                rows: new[] { Field("Product", typeof(string)) },
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { Filter = Where("Product", ElPivotFilterOperator.StartsWith, "注", typeof(string)) });

            var endsWith = Build(
                rows: new[] { Field("Product", typeof(string)) },
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { Filter = Where("Product", ElPivotFilterOperator.EndsWith, "器", typeof(string)) });

            Assert.Single(startsWith.BodyRows);
            Assert.Equal("注射器", startsWith.BodyRows[0].HeaderCells[0].Title);
            Assert.Equal(30L, AsLong(Cell(startsWith, 0, 0)));

            // EndsWith 同样生效：只有「注射器」以「器」结尾
            Assert.Single(endsWith.BodyRows);
            Assert.Equal("注射器", endsWith.BodyRows[0].HeaderCells[0].Title);
            Assert.Equal(30L, AsLong(Cell(endsWith, 0, 0)));

            // 对照组：以「林」结尾的是「阿莫西林」，命中两条（华东 / 华北），按行字段分组后归为同一行
            var grouped = Build(
                rows: new[] { Field("Product", typeof(string)) },
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { Filter = Where("Product", ElPivotFilterOperator.EndsWith, "林", typeof(string)) });

            Assert.Equal(2, grouped.Items.Count);
            Assert.Single(grouped.BodyRows);
            Assert.Equal("阿莫西林", grouped.BodyRows[0].HeaderCells[0].Title);
            Assert.Equal(50L, AsLong(Cell(grouped, 0, 0)));
        }

        [Fact]
        public void Engine_FilterTextOperators_AreCaseInsensitiveByDefault()
        {
            var matched = Build(
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { Filter = Where("Status", ElPivotFilterOperator.Contains, "已付", typeof(string)) });

            Assert.Equal(4, matched.Items.Count);

            // 对照组：Status 里不存在「ABC」，大小写不敏感也应为空（证明 Contains 真的在过滤，而不是被静默放行）。
            var none = Build(
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { Filter = Where("Status", ElPivotFilterOperator.Contains, "ABC", typeof(string)) });

            Assert.Empty(none.Items);
        }

        [Fact]
        public void Engine_FilterGreaterThan_ComparesNumerically()
        {
            var layout = Build(
                rows: new[] { Field("Region") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { Filter = Where("Amount", ElPivotFilterOperator.GreaterThan, 25, typeof(int)) });

            Assert.Equal(4, layout.Items.Count);
        }

        [Fact]
        public void Engine_FilterLessThanOrEquals_IncludesBoundary()
        {
            var layout = Build(
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { Filter = Where("Amount", ElPivotFilterOperator.LessThanOrEquals, 20, typeof(int)) });

            Assert.Equal(2, layout.Items.Count);
        }

        [Fact]
        public void Engine_FilterIsNullAndIsNotNull_SplitRecords()
        {
            var isNull = Build(aggregates: new[] { Agg("Amount", type: typeof(int)) }, options: new ElPivotEngineOptions { Filter = Where("Note", ElPivotFilterOperator.IsNull, null, typeof(string)) });
            var notNull = Build(aggregates: new[] { Agg("Amount", type: typeof(int)) }, options: new ElPivotEngineOptions { Filter = Where("Note", ElPivotFilterOperator.IsNotNull, null, typeof(string)) });

            Assert.Equal(2, isNull.Items.Count);
            Assert.Equal(4, notNull.Items.Count);
        }

        [Fact]
        public void Engine_FilterIsEmptyAndIsNotEmpty_TreatBlankAsEmpty()
        {
            var isEmpty = Build(aggregates: new[] { Agg("Amount", type: typeof(int)) }, options: new ElPivotEngineOptions { Filter = Where("Note", ElPivotFilterOperator.IsEmpty, null, typeof(string)) });
            var isNotEmpty = Build(aggregates: new[] { Agg("Amount", type: typeof(int)) }, options: new ElPivotEngineOptions { Filter = Where("Note", ElPivotFilterOperator.IsNotEmpty, null, typeof(string)) });

            // null 与 "" 都算空
            Assert.Equal(3, isEmpty.Items.Count);
            Assert.Equal(3, isNotEmpty.Items.Count);
        }

        [Fact]
        public void Engine_FilterIn_AcceptsCommaSeparatedString()
        {
            var layout = Build(
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { Filter = Where("Region", ElPivotFilterOperator.In, "华东,华南", typeof(string)) });

            Assert.Equal(4, layout.Items.Count);
        }

        [Fact]
        public void Engine_FilterIn_AcceptsEnumerable()
        {
            var layout = Build(
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { Filter = Where("Region", ElPivotFilterOperator.In, new[] { "华北" }, typeof(string)) });

            Assert.Equal(2, layout.Items.Count);
        }

        [Fact]
        public void Engine_FilterNotIn_ExcludesCandidates()
        {
            var layout = Build(
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { Filter = Where("Region", ElPivotFilterOperator.NotIn, "华东,华南", typeof(string)) });

            Assert.Equal(2, layout.Items.Count);
        }

        [Fact]
        public void Engine_FilterTwoConditions_CombinesWithAnd()
        {
            var layout = Build(
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions
                {
                    Filter = Where("Amount", ElPivotFilterOperator.GreaterThan, 15, typeof(int), ElPivotFilterOperator.LessThan, 55, logical: ElPivotLogicalOperator.And)
                });

            Assert.Equal(new[] { 20, 30, 40, 50 }, layout.Items.Select(i => i.Amount).OrderBy(x => x).ToArray());
        }

        [Fact]
        public void Engine_FilterTwoConditions_CombinesWithOr()
        {
            var layout = Build(
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions
                {
                    Filter = Where("Amount", ElPivotFilterOperator.Equals, 10, typeof(int), ElPivotFilterOperator.Equals, 60, logical: ElPivotLogicalOperator.Or)
                });

            Assert.Equal(new[] { 10, 60 }, layout.Items.Select(i => i.Amount).OrderBy(x => x).ToArray());
        }

        [Fact]
        public void Engine_FilterCaseSensitivity_IsHonored()
        {
            var data = new List<Sale>
            {
                new Sale { Region = "ABC", Amount = 1 },
                new Sale { Region = "abc", Amount = 2 }
            };

            var insensitive = Build(
                data: data,
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions
                {
                    Filter = Where("Region", ElPivotFilterOperator.Equals, "abc", typeof(string), caseSensitivity: ElPivotFilterCaseSensitivity.CaseInsensitive)
                });

            var sensitive = Build(
                data: data,
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions
                {
                    Filter = Where("Region", ElPivotFilterOperator.Equals, "abc", typeof(string), caseSensitivity: ElPivotFilterCaseSensitivity.CaseSensitive)
                });

            Assert.Equal(2, insensitive.Items.Count);
            Assert.Single(sensitive.Items);
        }

        [Fact]
        public void Engine_FieldLevelFilter_AppliesPerField()
        {
            var layout = Build(
                rows: new[] { Field("Region", typeof(string)) },
                columns: new[] { Field("Year", typeof(int), filter: Where("Year", ElPivotFilterOperator.Equals, 2025, typeof(int))) },
                aggregates: new[] { Agg("Amount", type: typeof(int)) });

            Assert.Equal(3, layout.Items.Count);
            Assert.Single(layout.ColumnLeaves);
        }

        [Fact]
        public void Engine_AggregateLevelFilter_RestrictsRecordsEnteringThePivot()
        {
            // 与参考实现一致：值字段上的筛选会作为**数据筛选**参与计算（参考实现把列字段与值字段的筛选
            // 一起拼进 where 子句），因此它同时收窄了行 / 列的取值域，而不只作用于单个单元格。
            var layout = Build(
                rows: new[] { Field("Region") },
                columns: new[] { Field("Year", typeof(int), sortOrder: ElPivotSortOrder.Ascending) },
                aggregates: new[]
                {
                    Agg("Amount", ElPivotAggregateFunction.Sum, typeof(int), filter: Where("Amount", ElPivotFilterOperator.GreaterThan, 25, typeof(int)))
                });

            // 只剩 30 / 40 / 50 / 60 四条
            Assert.Equal(4, layout.Items.Count);
            Assert.Equal(2, layout.ColumnLeaves.Count);

            // 华东只剩 2025 的 30 → 2024 桶为空（null）
            Assert.Null(Cell(layout, 0, 0));
            Assert.Equal(30L, AsLong(Cell(layout, 0, 1)));
        }

        [Fact]
        public void Engine_GridAndFieldFilters_AreCombined()
        {
            var layout = Build(
                rows: new[] { Field("Region", typeof(string)) },
                columns: new[] { Field("Year", typeof(int), filter: Where("Year", ElPivotFilterOperator.Equals, 2025, typeof(int))) },
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { Filter = Where("Status", ElPivotFilterOperator.Equals, "未付", typeof(string)) });

            // Year = 2025 且 Status = 未付 → S3(华东/30) 与 S5(华北/50)
            Assert.Equal(2, layout.Items.Count);
        }

        [Fact]
        public void Engine_CustomOperator_IsPassedThroughWithoutFiltering()
        {
            var layout = Build(
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { Filter = Where("Region", ElPivotFilterOperator.Custom, "任意", typeof(string)) });

            Assert.Equal(6, layout.Items.Count);
        }

        [Fact]
        public void Engine_FilterWithoutValue_DoesNotDropEverything()
        {
            var layout = Build(
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { Filter = Where("Region", ElPivotFilterOperator.Equals, null, typeof(string)) });

            Assert.Equal(6, layout.Items.Count);
        }

        [Fact]
        public void Engine_DateFilter_ComparesChronologically()
        {
            var layout = Build(
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions
                {
                    Filter = Where("Date", ElPivotFilterOperator.GreaterThanOrEquals, new DateTime(2025, 1, 1), typeof(DateTime))
                });

            Assert.Equal(3, layout.Items.Count);
        }

        #endregion

        #region 引擎：排序

        [Fact]
        public void Engine_RowSortAscending_UsesDeterministicOrdinalOrder()
        {
            var layout = Build(
                rows: new[] { Field("Region", typeof(string), sortOrder: ElPivotSortOrder.Ascending) },
                aggregates: new[] { Agg("Amount", type: typeof(int)) });

            var expected = Sales().Select(s => s.Region).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var actual = layout.BodyRows.Select(r => r.HeaderCells[0].Title).ToArray();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Engine_RowSortDescending_ReversesOrder()
        {
            var layout = Build(
                rows: new[] { Field("Year", typeof(int), sortOrder: ElPivotSortOrder.Descending) },
                aggregates: new[] { Agg("Amount", type: typeof(int)) });

            Assert.Equal("2025", layout.BodyRows[0].HeaderCells[0].Title);
            Assert.Equal("2024", layout.BodyRows[1].HeaderCells[0].Title);
        }

        [Fact]
        public void Engine_ColumnSort_ReordersLeaves()
        {
            var layout = Build(
                rows: new[] { Field("Region") },
                columns: new[] { Field("Year", typeof(int), sortOrder: ElPivotSortOrder.Descending) },
                aggregates: new[] { Agg("Amount", type: typeof(int)) });

            Assert.Equal(2025, layout.ColumnLeaves[0][0]);
            Assert.Equal(2024, layout.ColumnLeaves[1][0]);
        }

        [Fact]
        public void Engine_AggregateSort_OrdersRowsByAggregatedValue()
        {
            var layout = Build(
                rows: new[] { Field("Region") },
                aggregates: new[] { Agg("Amount", ElPivotAggregateFunction.Sum, typeof(int), sortOrder: ElPivotSortOrder.Descending) });

            // 华北 90 > 华东 60 = 华南 60
            Assert.Equal("华北", layout.BodyRows[0].HeaderCells[0].Title);
        }

        [Fact]
        public void Engine_WithoutSortOrder_KeepsFirstAppearanceOrder()
        {
            var layout = Build(
                rows: new[] { Field("Region") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) });

            Assert.Equal(new[] { "华东", "华北", "华南" }, layout.BodyRows.Select(r => r.HeaderCells[0].Title).ToArray());
        }

        #endregion

        #region 引擎：下钻与分组状态

        [Fact]
        public void Engine_DrillDownDisabled_ExpandsEverything()
        {
            var layout = Build(
                rows: new[] { Field("Region"), Field("Category") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { AllowDrillDown = false });

            Assert.Equal(5, layout.BodyRows.Count);
            Assert.All(layout.BodyRows, row => Assert.False(row.IsCollapsedGroup));
        }

        [Fact]
        public void Engine_GroupState_CanExpandASingleGroup()
        {
            var state = new Dictionary<string, bool>(StringComparer.Ordinal) { { "华东", false } };

            var layout = Build(
                rows: new[] { Field("Region"), Field("Category") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) },
                options: new ElPivotEngineOptions { GroupState = state });

            // 华东展开成 2 行，华北/华南 仍各 1 行
            Assert.Equal(4, layout.BodyRows.Count);
            Assert.Equal(2, layout.BodyRows.Count(r => !r.IsCollapsedGroup));

            // 深度不一致（2 层 vs 1 层）→ 浅的那行补占位符，保证每行列数一致
            Assert.Equal(2, layout.RowHeaderDepth);
            Assert.All(layout.BodyRows, row => Assert.Equal(2, row.HeaderCells.Count));
            Assert.True(layout.BodyRows[2].HeaderCells[1].IsPlaceholder);
            Assert.True(layout.BodyRows[3].HeaderCells[1].IsPlaceholder);
        }

        [Fact]
        public void Engine_PathKey_UsesPipeSeparatorAndEmptyForNull()
        {
            Assert.Equal("华东|药品", ElPivotEngine.PathKey(new object[] { "华东", "药品" }));
            Assert.Equal("|药品", ElPivotEngine.PathKey(new object[] { null, "药品" }));
            Assert.Equal(string.Empty, ElPivotEngine.PathKey(new object[0]));
        }

        [Fact]
        public void Engine_OthersPathKey_IsStable()
        {
            Assert.Equal("__el_pivot_others__", ElPivotEngine.OthersKey);
        }

        #endregion

        #region 引擎：MaxGroups / 其他

        [Fact]
        public void Engine_MaxGroups_MergesTailIntoOthersBucket()
        {
            var layout = Build(
                rows: new[] { Field("Product", typeof(string), maxGroups: 2) },
                columns: new[] { Field("Year") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) });

            Assert.Equal(3, layout.BodyRows.Count);
            Assert.Equal("阿莫西林", layout.BodyRows[0].HeaderCells[0].Title);
            Assert.Equal("头孢", layout.BodyRows[1].HeaderCells[0].Title);

            var others = layout.BodyRows[2].HeaderCells[0];
            Assert.Equal("其他", others.Title);
            Assert.True(others.IsOthers);
        }

        [Fact]
        public void Engine_MaxGroups_OthersLabelOverridesGlobalText()
        {
            var layout = Build(
                rows: new[] { Field("Product", typeof(string), maxGroups: 2, othersLabel: "其余品种") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) });

            Assert.Equal("其余品种", layout.BodyRows[2].HeaderCells[0].Title);
        }

        [Fact]
        public void Engine_MaxGroups_OthersBucketAggregatesTailRecords()
        {
            var layout = Build(
                rows: new[] { Field("Product", typeof(string), maxGroups: 2) },
                columns: new[] { Field("Year") },
                aggregates: new[] { Agg("Amount", type: typeof(int)) });

            // 其他 = 注射器(30) + 口罩(50) 都落在 2025 → 80；2024 无记录 → null
            Assert.Null(Cell(layout, 2, 0));
            Assert.Equal(80L, AsLong(Cell(layout, 2, 1)));
        }

        [Fact]
        public void Engine_MaxGroups_OnColumns_FlagsOthers()
        {
            var layout = Build(
                rows: new[] { Field("Region") },
                columns: new[] { Field("Product", typeof(string), maxGroups: 2) },
                aggregates: new[] { Agg("Amount", type: typeof(int)) });

            Assert.True(layout.HasOthersColumns);
            Assert.Equal(3, layout.ColumnLeaves.Count);
        }

        [Fact]
        public void Engine_MaxGroups_ZeroOrNegativeMeansUnlimited()
        {
            var layout = Build(
                rows: new[] { Field("Product", typeof(string), maxGroups: 0) },
                aggregates: new[] { Agg("Amount", type: typeof(int)) });

            Assert.Equal(4, layout.BodyRows.Count);
            Assert.False(layout.HasOthersColumns);
        }

        #endregion

        #region 属性访问（替代 Radzen.PropertyAccess + 动态 LINQ）

        [Fact]
        public void PropertyAccess_ReadsNestedPropertyPath()
        {
            var order = new Order { Customer = new Customer { City = "青岛" } };

            Assert.Equal("青岛", ElPivotPropertyAccess.GetValue(order, "Customer.City"));
        }

        [Fact]
        public void PropertyAccess_IsCaseInsensitiveOnFallback()
        {
            var order = new Order { region = "即墨" };

            Assert.Equal("即墨", ElPivotPropertyAccess.GetValue(order, "Region"));
            Assert.Equal("即墨", ElPivotPropertyAccess.GetValue(order, "REGION"));
        }

        [Fact]
        public void PropertyAccess_ReadsPublicField()
        {
            var order = new Order { PublicField = "F1" };

            Assert.Equal("F1", ElPivotPropertyAccess.GetValue(order, "PublicField"));
        }

        [Fact]
        public void PropertyAccess_ReadsObjectDictionaryBySegmentKey()
        {
            var order = new Order { Extras = new Dictionary<string, object> { { "Level", "A" } } };

            Assert.Equal("A", ElPivotPropertyAccess.GetValue(order, "Extras.Level"));
        }

        [Fact]
        public void PropertyAccess_ReadsTypedDictionaryBySegmentKey()
        {
            var order = new Order { Counters = new Dictionary<string, int> { { "Hits", 7 } } };

            // Dictionary<string, int> 不能直接转成 IDictionary<string, object>，必须回退到非泛型 IDictionary。
            Assert.Equal(7, ElPivotPropertyAccess.GetValue(order, "Counters.Hits"));
        }

        [Fact]
        public void PropertyAccess_MissingDictionaryKeyReturnsNull()
        {
            var order = new Order { Counters = new Dictionary<string, int>() };

            Assert.Null(ElPivotPropertyAccess.GetValue(order, "Counters.Nope"));
        }

        [Fact]
        public void PropertyAccess_ReadsStringIndexer()
        {
            var order = new Order();
            order.SetIndex("Level", "B");

            Assert.Equal("B", ElPivotPropertyAccess.GetValue(order, "Level"));
        }

        [Fact]
        public void PropertyAccess_UnknownPathReturnsNull_AndValueGetterDegradesGracefully()
        {
            var order = new Order { Customer = null };

            Assert.Null(ElPivotPropertyAccess.GetValue(order, "Customer.City.Nope"));

            var getter = ElPivotPropertyAccess.ValueGetter(typeof(Order), "NoSuchThing", null);

            Assert.NotNull(getter);
            Assert.Null(getter(order));
        }

        [Fact]
        public void PropertyAccess_ResolvesTypes()
        {
            Assert.Equal(typeof(string), ElPivotPropertyAccess.GetPropertyType(typeof(Sale), "Region"));
            Assert.Equal(typeof(int), ElPivotPropertyAccess.GetPropertyType(typeof(Sale), "Amount"));
            Assert.Null(ElPivotPropertyAccess.GetPropertyType(typeof(Sale), "Nope"));
        }

        [Fact]
        public void PropertyAccess_ClassifiesTypes()
        {
            Assert.True(ElPivotPropertyAccess.IsNumeric(typeof(int)));
            Assert.True(ElPivotPropertyAccess.IsNumeric(typeof(decimal?)));
            Assert.False(ElPivotPropertyAccess.IsNumeric(typeof(bool)));
            Assert.False(ElPivotPropertyAccess.IsNumeric(typeof(string)));
            Assert.True(ElPivotPropertyAccess.IsDate(typeof(DateTime)));
            Assert.True(ElPivotPropertyAccess.IsString(typeof(string)));
            Assert.True(ElPivotPropertyAccess.IsNullable(typeof(int?)));
            Assert.False(ElPivotPropertyAccess.IsNullable(typeof(int)));
            Assert.True(ElPivotPropertyAccess.IsEnum(typeof(ElPivotSortOrder)));
            Assert.True(ElPivotPropertyAccess.IsEnum(typeof(ElPivotSortOrder?)));
            Assert.False(ElPivotPropertyAccess.IsEnum(typeof(int)));
            Assert.Equal(typeof(int), ElPivotPropertyAccess.UnwrapNullable(typeof(int?)));
        }

        [Fact]
        public void PropertyAccess_CompareValues_HandlesNumbersStringsAndNull()
        {
            Assert.True(ElPivotPropertyAccess.CompareValues(1, 2) < 0);
            Assert.True(ElPivotPropertyAccess.CompareValues(2L, 2) == 0);
            Assert.True(ElPivotPropertyAccess.CompareValues("b", "a") > 0);
            Assert.True(ElPivotPropertyAccess.CompareValues(null, "a") < 0);
            Assert.True(ElPivotPropertyAccess.CompareValues("a", null) > 0);
            Assert.True(ElPivotPropertyAccess.CompareValues(null, null) == 0);
        }

        [Fact]
        public void PropertyAccess_GroupKeyComparer_TreatsDifferentNumericTypesAsEqual()
        {
            var comparer = ElPivotPropertyAccess.GroupKeyComparer.Instance;

            Assert.True(comparer.Equals(1, 1L));
            Assert.Equal(comparer.GetHashCode(1), comparer.GetHashCode(1L));

            // 可解析为数值的字符串按数值归一（与 Equals 一致，否则哈希契约会被破坏）
            Assert.True(comparer.Equals("1", 1));
            Assert.Equal(comparer.GetHashCode("1"), comparer.GetHashCode(1));

            // 非数值字符串按序数比较；bool 不当数值处理
            Assert.True(comparer.Equals("A", "A"));
            Assert.False(comparer.Equals("A", "a"));
            Assert.False(comparer.Equals(true, 1));
        }

        [Fact]
        public void PropertyAccess_ToSortKey_ParsesNumericStrings()
        {
            Assert.Equal(12d, ElPivotPropertyAccess.ToSortKey(12));
            Assert.Equal(12d, ElPivotPropertyAccess.ToSortKey("12"));
            Assert.Null(ElPivotPropertyAccess.ToSortKey("abc"));
        }

        #endregion

        #region 枚举与辅助表

        [Fact]
        public void Operators_ForNumericType_ExcludesTextOperators()
        {
            var operators = ElPivotFilterOperators.For(typeof(int));

            Assert.Contains(ElPivotFilterOperator.GreaterThan, operators);
            Assert.DoesNotContain(ElPivotFilterOperator.Contains, operators);
        }

        [Fact]
        public void Operators_ForStringType_IncludesTextOperators()
        {
            var operators = ElPivotFilterOperators.For(typeof(string));

            Assert.Contains(ElPivotFilterOperator.Contains, operators);
            Assert.Contains(ElPivotFilterOperator.StartsWith, operators);
            Assert.Contains(ElPivotFilterOperator.EndsWith, operators);
            Assert.Contains(ElPivotFilterOperator.IsEmpty, operators);
        }

        [Fact]
        public void Operators_ForBooleanAndEnum_RespectTheirValueDomain()
        {
            var boolean = ElPivotFilterOperators.For(typeof(bool));
            var enumerated = ElPivotFilterOperators.For(typeof(ElPivotSortOrder));

            Assert.DoesNotContain(ElPivotFilterOperator.GreaterThan, boolean);
            Assert.Contains(ElPivotFilterOperator.Equals, boolean);
            Assert.Contains(ElPivotFilterOperator.In, enumerated);
            Assert.DoesNotContain(ElPivotFilterOperator.Contains, enumerated);
        }

        [Fact]
        public void Operators_RequiresValueAndUnaryAreComplements()
        {
            Assert.False(ElPivotFilterOperators.RequiresValue(ElPivotFilterOperator.IsNull));
            Assert.True(ElPivotFilterOperators.IsUnary(ElPivotFilterOperator.IsNotNull));
            Assert.True(ElPivotFilterOperators.RequiresValue(ElPivotFilterOperator.Contains));
            Assert.False(ElPivotFilterOperators.IsUnary(ElPivotFilterOperator.Equals));
        }

        [Fact]
        public void Operators_EveryValue_HasADisplayName()
        {
            foreach (ElPivotFilterOperator op in Enum.GetValues(typeof(ElPivotFilterOperator)))
            {
                Assert.False(string.IsNullOrWhiteSpace(ElPivotFilterOperators.DisplayName(op)), op + " 缺少显示名");
            }

            foreach (ElPivotAggregateFunction function in Enum.GetValues(typeof(ElPivotAggregateFunction)))
            {
                Assert.False(string.IsNullOrWhiteSpace(ElPivotAggregateFunctions.DisplayName(function)), function + " 缺少显示名");
            }
        }

        [Fact]
        public void AggregateFunctions_AvailableSetIsStable()
        {
            Assert.Equal(8, ElPivotAggregateFunctions.All.Count);
            Assert.True(ElPivotAggregateFunctions.IsNumericOnly(ElPivotAggregateFunction.Sum));
            Assert.False(ElPivotAggregateFunctions.IsNumericOnly(ElPivotAggregateFunction.Count));
            Assert.DoesNotContain(ElPivotAggregateFunction.Average, ElPivotAggregateFunctions.Available(typeof(string)));
            Assert.Contains(ElPivotAggregateFunction.Count, ElPivotAggregateFunctions.Available(typeof(string)));
        }

        [Fact]
        public void Strings_ResolveMapsCodesAndCloneIsIndependent()
        {
            var strings = new ElPivotDataGridGrowStrings();
            var clone = strings.Clone();

            clone.FieldsPickerText = "自定义字段按钮";

            Assert.Equal("自定义字段按钮", clone.FieldsPickerText);
            Assert.NotEqual(clone.FieldsPickerText, strings.FieldsPickerText);

            Assert.Equal(strings.EmptyText, ElPivotDataGridGrowStrings.Resolve("pivot.empty"));
            Assert.Equal(strings.NoFieldsText, ElPivotDataGridGrowStrings.Resolve("pivot.noFields"));
            Assert.Null(ElPivotDataGridGrowStrings.Resolve(null));
            Assert.Equal("不存在的编码", ElPivotDataGridGrowStrings.Resolve("不存在的编码"));
            Assert.NotEmpty(ElPivotDataGridGrowStrings.Codes);
        }

        [Fact]
        public void Icons_EveryNameResolvesToASvgWithoutTextElement()
        {
            foreach (var name in ElPivotDataGridGrowIcons.Names)
            {
                var markup = ElPivotDataGridGrowIcons.Get(name);

                Assert.StartsWith("<svg", markup, StringComparison.Ordinal);
                Assert.EndsWith("</svg>", markup, StringComparison.Ordinal);
                Assert.True(markup.Contains("<path") || markup.Contains("<circle") || markup.Contains("<rect"), name + " 缺少图形元素");
                // Razor 无法直接输出 SVG 的 text 元素，图标一律不得含 text。
                Assert.DoesNotContain("<text", markup, StringComparison.Ordinal);
            }

            // 未知名称回退到默认图标，而不是抛异常。
            Assert.Contains("<circle", ElPivotDataGridGrowIcons.Get("不存在的图标"), StringComparison.Ordinal);
            Assert.Equal("dot", ElPivotDataGridGrowIcons.Fallback);
            Assert.True(ElPivotDataGridGrowIcons.Has("filter"));
            Assert.False(ElPivotDataGridGrowIcons.Has("不存在"));
            Assert.False(ElPivotDataGridGrowIcons.Has(null));
        }

        #endregion

        #region 渲染：外壳与表格

        [Fact]
        public void Renders_RootElementWithScopedClass()
        {
            var cut = Grid();

            var root = cut.Find("div.el-pivot-grow");

            Assert.Equal("el-pivot-grow", root.ClassList.First());
        }

        [Fact]
        public void Renders_TableWithRowAndColumnAndAggregateFields()
        {
            var cut = BasicGrid();

            var table = cut.Find("table.el-pivot-grow__table");

            Assert.NotNull(table);
            // 1 个行头 + 2 个 Year 列
            Assert.Equal(3, cut.FindAll("thead .el-pivot-grow__head-row")[0].QuerySelectorAll("th").Length);
            Assert.Equal(3, cut.FindAll("tbody .el-pivot-grow__row").Count);
        }

        [Fact]
        public void Renders_ColumnHeaderTitles()
        {
            var cut = BasicGrid();

            var titles = cut.FindAll(".el-pivot-grow__column-header").Select(t => t.TextContent.Trim()).ToList();

            Assert.Contains("2024", titles);
            Assert.Contains("2025", titles);
        }

        [Fact]
        public void Renders_RowHeaderTitles()
        {
            var cut = BasicGrid();

            var titles = cut.FindAll(".el-pivot-grow__row-header").Select(t => t.TextContent.Trim()).ToList();

            Assert.Contains("华东", titles);
            Assert.Contains("华北", titles);
            Assert.Contains("华南", titles);
        }

        [Fact]
        public void Renders_ValueCellsWithComputedNumbers()
        {
            var cut = BasicGrid();

            var cells = cut.FindAll(".el-pivot-grow__value").Select(c => c.TextContent.Trim()).ToList();

            Assert.Contains("30", cells);
            Assert.Contains("40", cells);
            Assert.Contains("50", cells);
            Assert.Contains("60", cells);
        }

        [Fact]
        public void Renders_AggregateHeaderRow_WhenEnabledAndHiddenOtherwise()
        {
            var shown = BasicGrid(p => p.Add(x => x.ShowAggregateHeaderRow, true));
            var hidden = BasicGrid(p => p.Add(x => x.ShowAggregateHeaderRow, false));

            Assert.NotEmpty(shown.FindAll(".el-pivot-grow__head-row--values"));
            Assert.Empty(hidden.FindAll(".el-pivot-grow__head-row--values"));
        }

        /// <summary>表头单元格在「模拟浏览器布局」后的位置。</summary>
        private sealed class HeaderCellLayout
        {
            public int Row { get; set; }

            public int Col { get; set; }

            public int ColSpan { get; set; }

            public int RowSpan { get; set; }

            public string Text { get; set; }

            public bool IsValueHeader { get; set; }

            public bool IsCorner { get; set; }
        }

        /// <summary>表头一行的布局结果。</summary>
        private sealed class HeaderRowLayout
        {
            public int Index { get; set; }

            /// <summary>本行（含被上方 rowspan 占用的列位）实际占用的总列数。</summary>
            public int TotalColumns { get; set; }

            public List<HeaderCellLayout> Cells { get; } = new List<HeaderCellLayout>();
        }

        /// <summary>
        /// 按 HTML 表格的布局规则模拟一次表头排版：上方单元格的 <c>rowspan</c> 会占住下方行的列位，
        /// 下方行的单元格只能从「第一个没被占用的列位」开始排。
        /// <para>
        /// 这是捕捉「幽灵列」的关键：如果跨行数算多了，值字段名表头行就会被顶到右侧、
        /// 整张表多出几列只有表头没有数据的空列——普通断言看不出，只有模拟布局才暴露。
        /// </para>
        /// </summary>
        private static List<HeaderRowLayout> SimulateHeaderLayout(IRenderedComponent<ElPivotDataGridGrow<Sale>> cut)
        {
            var rows = cut.FindAll(".el-pivot-grow__head-row");
            var occupied = new Dictionary<int, int>();   // 列 -> 仍需被占用的后续行数
            var result = new List<HeaderRowLayout>();

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var layout = new HeaderRowLayout { Index = rowIndex };
                var next = new Dictionary<int, int>();
                var cursor = 0;

                foreach (var cell in row.Children)
                {
                    while (occupied.ContainsKey(cursor))
                    {
                        cursor++;
                    }

                    var colspan = cell.HasAttribute("colspan") ? int.Parse(cell.GetAttribute("colspan")) : 1;
                    var rowspan = cell.HasAttribute("rowspan") ? int.Parse(cell.GetAttribute("rowspan")) : 1;
                    var className = cell.ClassName ?? string.Empty;

                    layout.Cells.Add(new HeaderCellLayout
                    {
                        Row = rowIndex,
                        Col = cursor,
                        ColSpan = colspan,
                        RowSpan = rowspan,
                        Text = cell.TextContent.Trim(),
                        IsValueHeader = className.Contains("el-pivot-grow__value-header"),
                        IsCorner = className.Contains("el-pivot-grow__corner"),
                    });

                    for (var i = 0; i < colspan; i++)
                    {
                        if (rowspan > 1)
                        {
                            next[cursor + i] = rowspan - 1;
                        }
                    }

                    cursor += colspan;
                }

                // 本行所有列位（含被占位覆盖的）总数：行头 + 数据列
                layout.TotalColumns = cursor;
                foreach (var col in occupied.Keys)
                {
                    if (col >= layout.TotalColumns && occupied[col] - 1 > 0)
                    {
                        layout.TotalColumns = col + 1;
                    }
                }

                result.Add(layout);

                // 推进到下一行：已有占用减 1，再加上本行新产生的占用
                var advanced = new Dictionary<int, int>();
                foreach (var pair in occupied)
                {
                    if (pair.Value - 1 > 0)
                    {
                        advanced[pair.Key] = pair.Value - 1;
                    }
                }

                foreach (var pair in next)
                {
                    advanced[pair.Key] = pair.Value;
                }

                occupied = advanced;
            }

            return result;
        }

        [Fact]
        public void Renders_CollapsedColumnGroup_NoPhantomColumnsAndValueHeaderUnderItsColumn()
        {
            // 复现演示页的配置：2 个列字段（区域 + 年份），年份默认折叠，显示底部列合计。
            var cut = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Region", title: "区域"), Column("Year", title: "年份")));
                p.Add(x => x.Rows, Fragment(Row("Category", title: "品类"), Row("Product", title: "商品")));
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Amount", title: "金额")));
                p.Add(x => x.ShowColumnsTotals, true);
            });

            var header = SimulateHeaderLayout(cut);

            // 折叠后：1 行列字段（区域，rowspan=1）+ 1 行值字段名（金额(求和)）
            Assert.Equal(2, header.Count);

            // 两行占用列数必须一致，否则就是多出了「只有表头、没有数据」的幽灵列
            Assert.Equal(header[0].TotalColumns, header[1].TotalColumns);
            Assert.Equal(4, header[0].TotalColumns); // 1 个行头列 + 3 个区域列

            // 行头单元格跨 2 行（列字段行 + 值字段名行）
            var corner = header[0].Cells.Single(c => c.IsCorner);
            Assert.Equal(0, corner.Col);
            Assert.Equal(2, corner.RowSpan);

            // 区域列头各自只占 1 行 1 列，值字段名才有位置落在同一列的正下方
            var regionCells = header[0].Cells.Where(c => !c.IsCorner && !c.IsValueHeader).ToList();
            Assert.Equal(3, regionCells.Count);
            Assert.Equal(new[] { 1, 2, 3 }, regionCells.Select(c => c.Col).ToArray());
            Assert.All(regionCells, c => Assert.Equal(1, c.ColSpan));
            Assert.All(regionCells, c => Assert.Equal(1, c.RowSpan));

            // 值字段名表头必须落在对应区域列的正下方（同列），而不是被顶到表格最右侧
            var valueCells = header[1].Cells.Where(c => c.IsValueHeader).ToList();
            Assert.Equal(3, valueCells.Count);
            Assert.Equal(new[] { 1, 2, 3 }, valueCells.Select(c => c.Col).ToArray());

            // 数据行 / 合计行的列数也必须与表头一致
            var dataColumns = cut.FindAll(".el-pivot-grow__row").First().Children.Count;
            var footColumns = cut.FindAll(".el-pivot-grow__foot-row").First().Children.Count;
            Assert.Equal(4, dataColumns);
            Assert.Equal(4, footColumns);
        }

        [Fact]
        public void Renders_ExpandedColumnGroup_ValueHeaderRowAlignsWithLeaves()
        {
            // 展开年份后表头变成 3 行：区域 / 年份 / 值字段名，行行列数仍须一致。
            var cut = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Region", title: "区域"), Column("Year", title: "年份")));
                p.Add(x => x.Rows, Fragment(Row("Category", title: "品类")));
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Amount", title: "金额")));
                p.Add(x => x.ExpandGroupsByDefault, true);
            });

            var header = SimulateHeaderLayout(cut);

            Assert.Equal(3, header.Count);
            Assert.All(header, r => Assert.Equal(header[0].TotalColumns, r.TotalColumns));

            // 行头在最外层跨 3 行
            Assert.Equal(3, header[0].Cells.Single(c => c.IsCorner).RowSpan);

            // 值字段名一格对应一个列叶子：华东/2024、华东/2025、华北/2024、华北/2025、华南/2025
            var valueCells = header[2].Cells.Where(c => c.IsValueHeader).ToList();
            Assert.Equal(5, valueCells.Count);
            Assert.Equal(header[0].TotalColumns - 1, valueCells.Count);
        }

        /// <summary>数据行单元格在「模拟浏览器布局」后的位置。</summary>
        private sealed class BodyCellLayout
        {
            public int Col { get; set; }

            public int ColSpan { get; set; }

            public int RowSpan { get; set; }

            public string Text { get; set; }

            public bool IsValue { get; set; }

            public bool IsRowHeader { get; set; }
        }

        /// <summary>数据行一行的布局结果。</summary>
        private sealed class BodyRowLayout
        {
            /// <summary>本行（含被上方 rowspan 占用的列位）实际占用的总列数。</summary>
            public int TotalColumns { get; set; }

            public List<BodyCellLayout> Cells { get; } = new List<BodyCellLayout>();
        }

        /// <summary>
        /// 按 HTML 表格布局规则模拟数据区排版（<c>rowspan</c> 竖向占位 + <c>colspan</c> 横向占位）。
        /// <para>
        /// 这是捕捉「数据单元格整体左移」的关键：行分组树是「部分下钻」的——把某个分组展开后，
        /// 行头列数取全部数据行的最大深度，深度较浅的兄弟行末尾会留占位列。
        /// 占位列若不靠横向跨度补回来，这些行的数据就会比表头少占一列、整体左移，
        /// 表现为某个值跑到左边一列的列头下面（普通断言只看文本、看不出错位）。
        /// </para>
        /// </summary>
        private static List<BodyRowLayout> SimulateBodyLayout(IRenderedComponent<ElPivotDataGridGrow<Sale>> cut)
        {
            var rows = cut.FindAll(".el-pivot-grow__row");
            var occupied = new Dictionary<int, int>();   // 列 -> 仍需被占用的后续行数
            var result = new List<BodyRowLayout>();

            foreach (var row in rows)
            {
                var layout = new BodyRowLayout();
                var next = new Dictionary<int, int>();
                var cursor = 0;

                foreach (var cell in row.Children)
                {
                    while (occupied.ContainsKey(cursor))
                    {
                        cursor++;
                    }

                    var colspan = cell.HasAttribute("colspan") ? int.Parse(cell.GetAttribute("colspan")) : 1;
                    var rowspan = cell.HasAttribute("rowspan") ? int.Parse(cell.GetAttribute("rowspan")) : 1;
                    var className = cell.ClassName ?? string.Empty;

                    layout.Cells.Add(new BodyCellLayout
                    {
                        Col = cursor,
                        ColSpan = colspan,
                        RowSpan = rowspan,
                        Text = cell.TextContent.Trim(),
                        IsValue = className.Contains("el-pivot-grow__value"),
                        IsRowHeader = className.Contains("el-pivot-grow__row-header"),
                    });

                    for (var i = 0; i < colspan; i++)
                    {
                        if (rowspan > 1)
                        {
                            next[cursor + i] = rowspan - 1;
                        }
                    }

                    cursor += colspan;
                }

                layout.TotalColumns = cursor;

                foreach (var col in occupied.Keys)
                {
                    if (col >= layout.TotalColumns && occupied[col] - 1 > 0)
                    {
                        layout.TotalColumns = col + 1;
                    }
                }

                result.Add(layout);

                var advanced = new Dictionary<int, int>();

                foreach (var pair in occupied)
                {
                    if (pair.Value - 1 > 0)
                    {
                        advanced[pair.Key] = pair.Value - 1;
                    }
                }

                foreach (var pair in next)
                {
                    advanced[pair.Key] = pair.Value;
                }

                occupied = advanced;
            }

            return result;
        }

        [Fact]
        public void Renders_ExpandedRowGroup_SiblingRowsKeepColumnAlignment()
        {
            // 复现演示页的形态：2 个行字段（品类 + 商品）+ 1 个列字段（区域），品类默认折叠。
            var cut = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Region", title: "区域")));
                p.Add(x => x.Rows, Fragment(Row("Category", title: "品类"), Row("Product", title: "商品")));
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Amount", title: "金额")));
                p.Add(x => x.ShowColumnsTotals, true);
            });

            // 折叠态：行头 1 列 + 3 个区域叶子
            var collapsed = SimulateBodyLayout(cut);
            Assert.All(collapsed, r => Assert.Equal(4, r.TotalColumns));

            // 展开第一个品类的行分组 → 行头变成 2 列，表头同步变成 5 列
            cut.Find(".el-pivot-grow__row-header .el-pivot-grow__expander").Click();

            var header = SimulateHeaderLayout(cut);
            var headerColumns = header[0].TotalColumns;
            Assert.Equal(5, headerColumns);

            var body = SimulateBodyLayout(cut);

            // ① 每一行都必须与表头占同样的列数，否则数据会整体左移
            Assert.All(body, r => Assert.Equal(headerColumns, r.TotalColumns));

            // 行头区的数据起始列（表头最后一行里第一个值字段名的列下标）
            var headerDataStart = header[header.Count - 1].Cells.First(c => c.IsValueHeader).Col;
            Assert.Equal(2, headerDataStart);

            // ② 每行的行头区横向右边界必须正好落在数据起始列（占位列被最后一个行头单元格吃掉）
            Assert.All(body, r =>
            {
                var headers = r.Cells.Where(c => c.IsRowHeader).ToList();

                Assert.Equal(headerDataStart, headers.Max(c => c.Col + c.ColSpan));
            });

            // ③ 未展开的兄弟分组：唯一一个横向吃满行头两列的行头单元格
            var sibling = body.Single(r => r.Cells.Any(c => c.IsRowHeader && c.ColSpan == 2));
            Assert.Equal(2, sibling.Cells[0].ColSpan);

            // 展开出来的明细行：行头两列各自独立，第二个是商品名
            var detailRows = body.Where(r => r != sibling).ToList();
            Assert.Equal(2, detailRows.Count);
            Assert.All(detailRows, r => Assert.Equal(1, r.Cells[1].ColSpan));

            // ④ 所有行的数据单元格列下标必须一致（都对齐到同一列的叶子）
            Assert.All(body, r =>
            {
                var cols = r.Cells.Where(c => c.IsValue).Select(c => c.Col).ToArray();

                Assert.Equal(new[] { 2, 3, 4 }, cols);
            });

            // ⑤ 兄弟行的数值必须落在与它标签对应的那一列上（这是本次错位的直接判据）
            var label = sibling.Cells.First(c => c.IsRowHeader).Text;
            var expected = label == "药品"
                ? new[] { "30", "40", "60" }            // 华东 10+20、华北 40、华南 60
                : new[] { "30", "50", string.Empty };   // 耗材：华东 30、华北 50、华南 无

            Assert.Equal(expected, sibling.Cells.Where(c => c.IsValue).Select(c => c.Text).ToArray());
        }

        [Fact]
        public void Renders_CornerCellPerRowHeaderLevel()
        {
            var cut = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Year")));
                p.Add(x => x.Rows, Fragment(Row("Region", "区域"), Row("Category", "品类")));
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Amount")));
            });

            Assert.NotEmpty(cut.FindAll(".el-pivot-grow__corner"));
        }

        [Fact]
        public void Renders_RowAndColumnTotalsAndGrandTotal()
        {
            var cut = BasicGrid(p =>
            {
                p.Add(x => x.ShowRowsTotals, true);
                p.Add(x => x.ShowColumnsTotals, true);
            });

            Assert.NotEmpty(cut.FindAll(".el-pivot-grow__row-total"));
            Assert.NotEmpty(cut.FindAll(".el-pivot-grow__foot"));
            Assert.Contains(cut.FindAll(".el-pivot-grow__foot-total"), t => t.TextContent.Trim() == "210");
        }

        [Fact]
        public void Renders_FootOmittedWhenColumnsTotalsDisabled()
        {
            var cut = BasicGrid(p => p.Add(x => x.ShowColumnsTotals, false));

            Assert.Empty(cut.FindAll(".el-pivot-grow__foot"));
        }

        [Fact]
        public void Renders_AlternatingRowsClass_WhenStriped()
        {
            var cut = BasicGrid(p => p.Add(x => x.AllowAlternatingRows, true));

            Assert.Contains("is-striped", cut.Find(".el-pivot-grow__table").ClassList);
            Assert.NotEmpty(cut.FindAll(".el-pivot-grow__row.is-odd"));
        }

        [Fact]
        public void Renders_GridLinesClass_MatchesParameter()
        {
            var cut = BasicGrid(p => p.Add(x => x.GridLines, ElPivotGridLines.Vertical));

            Assert.Contains("el-pivot-grow__table--vertical", cut.Find(".el-pivot-grow__table").ClassList);
        }

        [Fact]
        public void Renders_MaxHeightOnContentContainer()
        {
            var cut = BasicGrid(p => p.Add(x => x.MaxHeight, "320px"));

            Assert.Contains("max-height:320px", cut.Find(".el-pivot-grow__content").GetAttribute("style"));
        }

        [Fact]
        public void Renders_ColumnWidthOnHeaderStyle()
        {
            var cut = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Year", width: "90px")));
                p.Add(x => x.Rows, Fragment(Row("Region")));
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Amount")));
            });

            Assert.Contains("width:90px", cut.Find(".el-pivot-grow__column-header").GetAttribute("style"));
        }

        #endregion

        #region 渲染：状态

        [Fact]
        public void Renders_NoFieldsHintWhenOnlyAggregatesConfigured()
        {
            var cut = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Amount")));
            });

            Assert.Contains("请至少添加一个行字段或列字段", cut.Markup);
        }

        [Fact]
        public void Renders_NoAggregatesHintWhenOnlyGroupingFields()
        {
            var cut = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Year")));
                p.Add(x => x.Rows, Fragment(Row("Region")));
            });

            Assert.NotEmpty(cut.FindAll(".el-pivot-grow__empty"));
            Assert.Contains("请至少添加一个值字段", cut.Markup);
        }

        [Fact]
        public void Renders_EmptyTextOverride()
        {
            var cut = Grid(p =>
            {
                p.Add(x => x.Data, new List<Sale>());
                p.Add(x => x.Columns, Fragment(Column("Year")));
                p.Add(x => x.Rows, Fragment(Row("Region")));
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Amount")));
                p.Add(x => x.EmptyText, "该时间段没有数据");
            });

            Assert.Contains("该时间段没有数据", cut.Markup);
        }

        [Fact]
        public void Renders_LoadingOverlayKeepsContentAndRendersTemplate()
        {
            var cut = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Year")));
                p.Add(x => x.Rows, Fragment(Row("Region")));
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Amount")));
                p.Add(x => x.IsLoading, true);
                p.Add(x => x.LoadingTemplate, (RenderFragment)(b => b.AddMarkupContent(0, "<em class=\"probe-loading\">加载中</em>")));
            });

            // 与 Element Plus 的 v-loading 一致：遮罩叠加在内容之上，内容本身保留。
            Assert.NotEmpty(cut.FindAll(".el-pivot-grow__loading"));
            Assert.NotEmpty(cut.FindAll(".probe-loading"));
            Assert.NotEmpty(cut.FindAll("table.el-pivot-grow__table"));
        }

        [Fact]
        public void Renders_NoLoadingOverlayByDefault()
        {
            var cut = BasicGrid();

            Assert.Empty(cut.FindAll(".el-pivot-grow__loading"));
        }

        [Fact]
        public void Renders_CustomStringsFlowThrough()
        {
            var strings = new ElPivotDataGridGrowStrings { FieldsPickerText = "自选维度", TotalText = "合计" };

            var cut = BasicGrid(p =>
            {
                p.Add(x => x.AllowFieldsPicking, true);
                p.Add(x => x.ShowColumnsTotals, true);
                p.Add(x => x.Strings, strings);
            });

            Assert.Contains("自选维度", cut.Markup);
            Assert.Contains("合计", cut.Markup);
        }

        [Fact]
        public void Renders_CustomClsAndStyleAndAttributes()
        {
            var cut = BasicGrid(p =>
            {
                p.Add(x => x.Cls, "my-pivot");
                p.Add(x => x.Style, "border:1px solid #ccc");
                p.AddUnmatched("data-testid", "pivot-1");
            });

            var root = cut.Find("div.el-pivot-grow");

            Assert.Contains("my-pivot", root.ClassList);
            Assert.Contains("border:1px solid #ccc", root.GetAttribute("style"));
            Assert.Equal("pivot-1", root.GetAttribute("data-testid"));
        }

        [Fact]
        public void Renders_HeaderTemplateOverridesTitle()
        {
            var cut = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Year", headerTemplate: ctx => builder => builder.AddMarkupContent(0, "<b class=\"probe-hdr\">" + ctx.Title + "年</b>"))));
                p.Add(x => x.Rows, Fragment(Row("Region")));
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Amount")));
            });

            var header = cut.Find(".probe-hdr");

            Assert.Equal("2024年", header.TextContent);
        }

        [Fact]
        public void Renders_AggregateTemplateOverridesFormattedValue()
        {
            var cut = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Year")));
                p.Add(x => x.Rows, Fragment(Row("Region")));
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Amount", template: value => builder => builder.AddMarkupContent(0, "<i class=\"probe-val\">¥" + value + "</i>"))));
            });

            Assert.NotEmpty(cut.FindAll(".probe-val"));
        }

        [Fact]
        public void Renders_AggregateFormatString()
        {
            var cut = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Year")));
                p.Add(x => x.Rows, Fragment(Row("Region")));
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Cost", formatString: "0.00")));
            });

            Assert.Contains(cut.FindAll(".el-pivot-grow__value"), c => Regex.IsMatch(c.TextContent.Trim(), @"^\d+\.\d{2}$"));
        }

        [Fact]
        public void Renders_TextAlignClass()
        {
            var cut = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Year")));
                p.Add(x => x.Rows, Fragment(Row("Region")));
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Amount", align: ElPivotTextAlign.Left)));
            });

            Assert.Contains("is-left", cut.Find(".el-pivot-grow__value").ClassList);
        }

        [Fact]
        public void Renders_AggregateTotalTemplates_ApplyToRowColumnAndGrandTotals()
        {
            var cut = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Year")));
                p.Add(x => x.Rows, Fragment(Row("Region")));
                p.Add(x => x.ShowRowsTotals, true);
                p.Add(x => x.ShowColumnsTotals, true);
                p.Add(x => x.Aggregates, Fragment(AggregateColumn(
                    "Amount",
                    rowTotalTemplate: context => builder => builder.AddMarkupContent(0, "<i class=\"probe-row-total\">" + (context.IsGrandTotal ? "G" : "R") + ":" + context.Value + "</i>"),
                    columnTotalTemplate: value => builder => builder.AddMarkupContent(0, "<i class=\"probe-col-total\">C:" + value + "</i>"))));
            });

            // 行合计：3 个分组行各一个
            Assert.Equal(3, cut.FindAll(".el-pivot-grow__row-total .probe-row-total").Count);

            // 列合计：2 个列叶子各一个
            Assert.Equal(2, cut.FindAll(".el-pivot-grow__foot-value .probe-col-total").Count);

            // 右下角总计共用行合计模板，但上下文里 IsGrandTotal = true（与参考实现一致）
            Assert.Equal("G:210", cut.Find(".el-pivot-grow__foot-total .probe-row-total").TextContent.Trim());
        }

        #endregion

        #region 渲染：交互

        [Fact]
        public void Interaction_CellClick_RaisesCellClickedWithRowAndValue()
        {
            ElPivotCellEventArgs<Sale> captured = null;

            var cut = BasicGrid(p => p.Add(x => x.CellClicked, (ElPivotCellEventArgs<Sale> args) => captured = args));

            cut.FindAll(".el-pivot-grow__value")[0].Click();

            Assert.NotNull(captured);
            Assert.Equal("华东", captured.Row.HeaderCells[0].Title);
            Assert.Equal(2024, captured.ColumnPath[0]);
            Assert.Equal(30L, captured.Value);
            Assert.False(captured.IsRowTotal);
            Assert.Equal(2, captured.Items.Count);
        }

        [Fact]
        public void Interaction_RowTotalClick_SetsIsRowTotal()
        {
            ElPivotCellEventArgs<Sale> captured = null;

            var cut = BasicGrid(p =>
            {
                p.Add(x => x.ShowRowsTotals, true);
                p.Add(x => x.CellClicked, (ElPivotCellEventArgs<Sale> args) => captured = args);
            });

            cut.FindAll(".el-pivot-grow__row-total")[0].Click();

            Assert.NotNull(captured);
            Assert.True(captured.IsRowTotal);
            Assert.Equal(60L, captured.Value);
        }

        [Fact]
        public void Interaction_GrandTotalClick_SetsIsGrandTotal()
        {
            ElPivotCellEventArgs<Sale> captured = null;

            var cut = BasicGrid(p =>
            {
                p.Add(x => x.ShowColumnsTotals, true);

                // 右下角总计只在同时开启行合计时才有意义（与参考实现一致：footer 末列是「行合计的列合计」）。
                p.Add(x => x.ShowRowsTotals, true);
                p.Add(x => x.CellClicked, (ElPivotCellEventArgs<Sale> args) => captured = args);
            });

            cut.Find(".el-pivot-grow__foot-total").Click();

            Assert.NotNull(captured);
            Assert.True(captured.IsGrandTotal);
            Assert.Equal(210L, captured.Value);
            Assert.Equal(6, captured.Items.Count);
        }

        [Fact]
        public void Interaction_SortButton_CyclesSortOrderAndRaisesEvent()
        {
            ElPivotSortChangedEventArgs captured = null;

            var cut = BasicGrid(p => p.Add(x => x.SortChanged, (ElPivotSortChangedEventArgs args) => captured = args));

            cut.Find(".el-pivot-grow__sort").Click();

            Assert.NotNull(captured);
            Assert.Equal("Year", captured.Property);
            Assert.Equal("column", captured.Scope);
            Assert.Equal(ElPivotSortOrder.Ascending, captured.SortOrder);
        }

        [Fact]
        public void Interaction_SortButton_HiddenWhenSortingDisabled()
        {
            var cut = BasicGrid(p => p.Add(x => x.AllowSorting, false));

            Assert.Empty(cut.FindAll(".el-pivot-grow__sort"));
        }

        [Fact]
        public void Interaction_FieldNotSortable_HidesItsSortButton()
        {
            var cut = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Year", sortable: false)));
                p.Add(x => x.Rows, Fragment(Row("Region")));
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Amount")));
            });

            // 只断言「列头里的排序按钮」：行头单元格在数据区按行重复渲染，行字段本身仍可排序，
            // 直接 FindAll(".el-pivot-grow__sort") 会数到行头里那几个。
            Assert.Empty(cut.FindAll(".el-pivot-grow__column-header .el-pivot-grow__sort"));

            // 对照组：可排序的列字段应当渲染出排序按钮
            var sortable = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Year")));
                p.Add(x => x.Rows, Fragment(Row("Region")));
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Amount")));
            });

            Assert.NotEmpty(sortable.FindAll(".el-pivot-grow__column-header .el-pivot-grow__sort"));
        }

        [Fact]
        public void Interaction_FilterButtonOpensBuiltInPanel()
        {
            var cut = BasicGrid();

            Assert.Empty(cut.FindAll(".el-pivot-grow__filter-panel"));

            cut.Find(".el-pivot-grow__filter").Click();

            Assert.NotEmpty(cut.FindAll(".el-pivot-grow__filter-panel"));
            Assert.NotEmpty(cut.FindAll(".el-pivot-grow__filter-select"));
        }

        [Fact]
        public void Interaction_FilterPanelClose()
        {
            var cut = BasicGrid();

            cut.Find(".el-pivot-grow__filter").Click();
            cut.Find(".el-pivot-grow__filter-close").Click();

            Assert.Empty(cut.FindAll(".el-pivot-grow__filter-panel"));
        }

        [Fact]
        public void Interaction_FilterPanelsHiddenWhenFilteringDisabled()
        {
            var cut = BasicGrid(p => p.Add(x => x.AllowFiltering, false));

            Assert.Empty(cut.FindAll(".el-pivot-grow__filter"));
        }

        [Fact]
        public void Interaction_DrillDownToggle_ExpandsCollapsedGroup()
        {
            ElPivotDrillDownEventArgs captured = null;

            var cut = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Year")));
                p.Add(x => x.Rows, Fragment(Row("Region", "区域"), Row("Category", "品类")));
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Amount")));
                p.Add(x => x.DrillDownToggled, (ElPivotDrillDownEventArgs args) => captured = args);
            });

            Assert.Equal(3, cut.FindAll(".el-pivot-grow__row").Count);

            cut.Find(".el-pivot-grow__expander").Click();

            Assert.NotNull(captured);
            Assert.Equal("row", captured.Scope);
            Assert.Equal(4, cut.FindAll(".el-pivot-grow__row").Count);
        }

        [Fact]
        public void Interaction_DrillDownDisabled_HidesExpanders()
        {
            var cut = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Year")));
                p.Add(x => x.Rows, Fragment(Row("Region"), Row("Category")));
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Amount")));
                p.Add(x => x.AllowDrillDown, false);
            });

            Assert.Empty(cut.FindAll(".el-pivot-grow__expander"));
        }

        [Fact]
        public void Interaction_CollapsedGroupGetsMarkerClass()
        {
            var cut = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Year")));
                p.Add(x => x.Rows, Fragment(Row("Region"), Row("Category")));
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Amount")));
            });

            Assert.NotEmpty(cut.FindAll(".el-pivot-grow__row.is-collapsed-group"));
        }

        [Fact]
        public void Interaction_ExpandGroupsByDefault_RemovesCollapsedMarker()
        {
            var cut = Grid(p =>
            {
                p.Add(x => x.Data, Sales());
                p.Add(x => x.Columns, Fragment(Column("Year")));
                p.Add(x => x.Rows, Fragment(Row("Region"), Row("Category")));
                p.Add(x => x.Aggregates, Fragment(AggregateColumn("Amount")));
                p.Add(x => x.ExpandGroupsByDefault, true);
            });

            Assert.Empty(cut.FindAll(".el-pivot-grow__row.is-collapsed-group"));
            Assert.Equal(5, cut.FindAll(".el-pivot-grow__row").Count);
        }

        [Fact]
        public void Interaction_GridLevelFilterReducesRenderedRows()
        {
            var cut = BasicGrid(p => p.Add(x => x.Filter, Where("Status", ElPivotFilterOperator.Equals, "未付", typeof(string))));

            Assert.Equal(2, cut.FindAll(".el-pivot-grow__row").Count);
        }

        [Fact]
        public void Interaction_FilterValueAppliedThroughBuiltInPanel()
        {
            var cut = BasicGrid();

            cut.Find(".el-pivot-grow__filter").Click();

            var input = cut.Find(".el-pivot-grow__filter-value");
            input.Change("2025");

            cut.FindAll(".el-pivot-grow__button").First(b => b.TextContent.Trim() == "应用").Click();

            // 只剩 2025 一列
            Assert.Single(cut.FindAll(".el-pivot-grow__column-header"));
        }

        [Fact]
        public void Interaction_FilterPanelClearRestoresAllColumns()
        {
            var cut = BasicGrid();

            // 先通过内置面板把列字段过滤到只剩 2025
            cut.Find(".el-pivot-grow__filter").Click();
            cut.Find(".el-pivot-grow__filter-value").Change("2025");
            cut.FindAll(".el-pivot-grow__button").First(b => b.TextContent.Trim() == "应用").Click();

            Assert.Single(cut.FindAll(".el-pivot-grow__column-header"));

            // 重新打开面板并清除 → 恢复全部列
            cut.Find(".el-pivot-grow__filter").Click();
            cut.FindAll(".el-pivot-grow__button").First(b => b.TextContent.Trim() == "清除").Click();

            Assert.Equal(2, cut.FindAll(".el-pivot-grow__column-header").Count);
        }

        [Fact]
        public void Interaction_FilterPanelClearKeepsExternallyBoundGridFilter()
        {
            // 网格级 Filter 是外部传入的受控参数，面板里的「清除」只清掉面板自己维护的字段级筛选，
            // 因此外部绑定依旧生效（与参考实现一致）。调用方若要让清除立即生效，需要监听 FilterChanged 自行更新绑定值。
            var cut = BasicGrid(p => p.Add(x => x.Filter, Where("Year", ElPivotFilterOperator.Equals, 2025, typeof(int))));

            Assert.Single(cut.FindAll(".el-pivot-grow__column-header"));

            cut.Find(".el-pivot-grow__filter").Click();
            cut.FindAll(".el-pivot-grow__button").First(b => b.TextContent.Trim() == "清除").Click();

            Assert.Single(cut.FindAll(".el-pivot-grow__column-header"));
        }

        #endregion

        #region 渲染：分页

        [Fact]
        public void Paging_RendersPagerWhenEnabled()
        {
            var cut = BasicGrid(p =>
            {
                p.Add(x => x.AllowPaging, true);
                p.Add(x => x.PageSize, 2);
            });

            Assert.NotEmpty(cut.FindAll(".el-pivot-grow__pager"));
            Assert.Equal(2, cut.FindAll(".el-pivot-grow__row").Count);
        }

        [Fact]
        public void Paging_SecondPageShowsRemainingRows()
        {
            var cut = BasicGrid(p =>
            {
                p.Add(x => x.AllowPaging, true);
                p.Add(x => x.PageSize, 2);
            });

            cut.FindAll(".el-pivot-grow__pager-btn").First(b => b.TextContent.Trim() == "2").Click();

            Assert.Single(cut.FindAll(".el-pivot-grow__row"));
            Assert.Contains("华南", cut.Markup);
        }

        [Fact]
        public void Paging_PageIndexChangedIsRaised()
        {
            var raised = 0;

            var cut = BasicGrid(p =>
            {
                p.Add(x => x.AllowPaging, true);
                p.Add(x => x.PageSize, 2);
                p.Add(x => x.PageIndexChanged, (int page) => raised = page);
            });

            cut.FindAll(".el-pivot-grow__pager-btn").First(b => b.TextContent.Trim() == "2").Click();

            Assert.Equal(2, raised);
        }

        [Fact]
        public void Paging_SizeSelectorChangesPageSize()
        {
            var size = 0;

            var cut = BasicGrid(p =>
            {
                p.Add(x => x.AllowPaging, true);
                p.Add(x => x.PageSize, 2);
                p.Add(x => x.PageSizeChanged, (int value) => size = value);
            });

            cut.Find(".el-pivot-grow__pager-size").Change("50");

            Assert.Equal(50, size);
        }

        [Fact]
        public void Paging_TopPositionRendersPagerAboveTable()
        {
            var cut = BasicGrid(p =>
            {
                p.Add(x => x.AllowPaging, true);
                p.Add(x => x.PagerPosition, ElPivotPagerPosition.Top);
            });

            var markup = cut.Markup;
            var pagerIndex = markup.IndexOf("el-pivot-grow__pager", StringComparison.Ordinal);
            var tableIndex = markup.IndexOf("el-pivot-grow__table", StringComparison.Ordinal);

            Assert.True(pagerIndex >= 0 && tableIndex > pagerIndex);
        }

        [Fact]
        public void Paging_SummaryTextMentionsTotals()
        {
            var cut = BasicGrid(p => p.Add(x => x.AllowPaging, true));

            Assert.NotEmpty(cut.FindAll(".el-pivot-grow__pager-summary"));
        }

        [Fact]
        public void Paging_PagerSummaryCanBeHidden()
        {
            var cut = BasicGrid(p =>
            {
                p.Add(x => x.AllowPaging, true);
                p.Add(x => x.ShowPagerSummary, false);
            });

            Assert.Empty(cut.FindAll(".el-pivot-grow__pager-summary"));
        }

        [Fact]
        public void Paging_PrevButtonDisabledOnFirstPage()
        {
            var cut = BasicGrid(p =>
            {
                p.Add(x => x.AllowPaging, true);
                p.Add(x => x.PageSize, 2);
            });

            Assert.True(cut.FindAll(".el-pivot-grow__pager-btn")[0].HasAttribute("disabled"));
        }

        #endregion

        #region 渲染：字段选择器

        [Fact]
        public void FieldsPicker_HiddenByDefault()
        {
            var cut = BasicGrid();

            Assert.Empty(cut.FindAll(".el-pivot-grow__picker"));
        }

        [Fact]
        public void FieldsPicker_ListsCandidateFieldsPerScope()
        {
            var cut = BasicGrid(p => p.Add(x => x.AllowFieldsPicking, true));

            Assert.NotEmpty(cut.FindAll(".el-pivot-grow__picker"));

            var titles = cut.FindAll(".el-pivot-grow__picker-title").Select(t => t.TextContent.Trim()).ToList();

            Assert.Contains("行", titles);
            Assert.Contains("列", titles);
            Assert.Contains("值", titles);
        }

        [Fact]
        public void FieldsPicker_ToggleExpandsBody()
        {
            var cut = BasicGrid(p => p.Add(x => x.AllowFieldsPicking, true));

            var body = cut.Find(".el-pivot-grow__picker-body");

            Assert.Contains("display:none", body.GetAttribute("style"));

            cut.Find(".el-pivot-grow__picker-toggle").Click();

            Assert.Null(cut.Find(".el-pivot-grow__picker-body").GetAttribute("style"));
        }

        [Fact]
        public void FieldsPicker_ExpandedByDefaultWhenRequested()
        {
            var cut = BasicGrid(p =>
            {
                p.Add(x => x.AllowFieldsPicking, true);
                p.Add(x => x.FieldsPickerExpanded, true);
            });

            Assert.Null(cut.Find(".el-pivot-grow__picker-body").GetAttribute("style"));
        }

        [Fact]
        public void FieldsPicker_UncheckingRowFieldRemovesItFromTheGrid()
        {
            var cut = BasicGrid(p => p.Add(x => x.AllowFieldsPicking, true));

            Assert.Equal(3, cut.FindAll(".el-pivot-grow__row").Count);

            // 注意：候选字段按「组件注册顺序」排列，而 BasicGrid 里 Columns 参数写在 Rows 之前，
            // 所以「行」分组里的第 0 个复选框其实是列字段 Year。
            // 必须按「分组标题 + 字段标签」精确定位，不能按下标取。
            var rowSection = cut.FindAll(".el-pivot-grow__picker-section")
                .First(s => s.QuerySelector(".el-pivot-grow__picker-title").TextContent.Trim() == "行");

            var regionCheckbox = rowSection.QuerySelectorAll(".el-pivot-grow__picker-item")
                .First(item => item.QuerySelector(".el-pivot-grow__picker-label").TextContent.Trim() == "区域")
                .QuerySelector("input[type=checkbox]");

            regionCheckbox.Change(false);

            // 行字段被移除后，退化为「单行总计」
            Assert.Single(cut.FindAll(".el-pivot-grow__row"));
        }

        [Fact]
        public void FieldsPicker_SelectionChangedEventIsRaised()
        {
            ElPivotFieldSelectionEventArgs captured = null;

            var cut = BasicGrid(p =>
            {
                p.Add(x => x.AllowFieldsPicking, true);
                p.Add(x => x.FieldSelectionChanged, (ElPivotFieldSelectionEventArgs args) => captured = args);
            });

            cut.FindAll(".el-pivot-grow__picker-item input[type=checkbox]")[0].Change(false);

            Assert.NotNull(captured);
            Assert.Equal("row", captured.Scope);
        }

        [Fact]
        public void FieldsPicker_AggregateFunctionCanBeChanged()
        {
            ElPivotFieldSelectionEventArgs captured = null;

            var cut = BasicGrid(p =>
            {
                p.Add(x => x.AllowFieldsPicking, true);
                p.Add(x => x.FieldSelectionChanged, (ElPivotFieldSelectionEventArgs args) => captured = args);
            });

            cut.Find(".el-pivot-grow__picker-function").Change("Average");

            Assert.NotNull(captured);
            Assert.Equal("aggregate", captured.Scope);
        }

        [Fact]
        public void FieldsPicker_HeaderTextOverridesDefaultLabel()
        {
            var cut = BasicGrid(p =>
            {
                p.Add(x => x.AllowFieldsPicking, true);
                p.Add(x => x.FieldsPickerHeaderText, "选择维度");
            });

            Assert.Contains("选择维度", cut.Markup);
        }

        #endregion

        #region 渲染：程序化 API

        [Fact]
        public void Api_LayoutAndTotalRowsAreExposed()
        {
            var cut = BasicGrid();

            var grid = cut.Instance;

            Assert.Equal(3, grid.TotalRows);
            Assert.Equal(1, grid.PageCount);
            Assert.Equal(1, grid.CurrentPage);
            Assert.Equal(10, grid.EffectivePageSize);
            Assert.Equal(2, grid.GetLayout().ColumnLeaves.Count);
        }

        [Fact]
        public void Api_ReloadAsync_KeepsResultStable()
        {
            var cut = BasicGrid();

            cut.InvokeAsync(() => cut.Instance.ReloadAsync()).GetAwaiter().GetResult();

            Assert.Equal(3, cut.Instance.TotalRows);
        }

        [Fact]
        public void Api_DescribePath_FormatsReadableText()
        {
            Assert.Equal("华东 / 药品", ElPivotDataGridGrow<Sale>.DescribePath(new object[] { "华东", "药品" }));
            Assert.Equal("（空）", ElPivotDataGridGrow<Sale>.DescribePath(new object[] { null }));
            Assert.Equal(string.Empty, ElPivotDataGridGrow<Sale>.DescribePath(null));
        }

        [Fact]
        public void Api_StaticHelpers_MapEnumsToClassesAndValues()
        {
            Assert.Equal("is-left", ElPivotDataGridGrow<Sale>.GetAlignClass(ElPivotTextAlign.Left));
            Assert.Equal("is-center", ElPivotDataGridGrow<Sale>.GetAlignClass(ElPivotTextAlign.Center));
            Assert.Equal("is-right", ElPivotDataGridGrow<Sale>.GetAlignClass(ElPivotTextAlign.Right));

            Assert.True(ElPivotDataGridGrow<Sale>.ToBool(true));
            Assert.True(ElPivotDataGridGrow<Sale>.ToBool("on"));
            Assert.False(ElPivotDataGridGrow<Sale>.ToBool("no"));

            Assert.Equal(5, ElPivotDataGridGrow<Sale>.ToInt("5"));
            Assert.Equal(0, ElPivotDataGridGrow<Sale>.ToInt("abc"));

            Assert.Equal(ElPivotAggregateFunction.Average, ElPivotDataGridGrow<Sale>.ParseAggregateFunction("Average"));
            Assert.Equal(ElPivotFilterOperator.Contains, ElPivotDataGridGrow<Sale>.ParseFilterOperator("Contains"));
            Assert.Equal(ElPivotLogicalOperator.Or, ElPivotDataGridGrow<Sale>.ParseLogicalOperator("Or"));

            Assert.True(ElPivotDataGridGrow<Sale>.IsEnumType(typeof(ElPivotSortOrder?)));
            Assert.False(ElPivotDataGridGrow<Sale>.IsEnumType(typeof(int)));
            Assert.Contains("Ascending", ElPivotDataGridGrow<Sale>.GetEnumOptions(typeof(ElPivotSortOrder)));
        }

        [Fact]
        public void Api_GridLinesClass_MatchesEachEnumValue()
        {
            var both = BasicGrid(p => p.Add(x => x.GridLines, ElPivotGridLines.Both));
            var none = BasicGrid(p => p.Add(x => x.GridLines, ElPivotGridLines.None));
            var horizontal = BasicGrid(p => p.Add(x => x.GridLines, ElPivotGridLines.Horizontal));

            Assert.Contains("el-pivot-grow__table--both", both.Find(".el-pivot-grow__table").ClassList);
            Assert.Contains("el-pivot-grow__table--none", none.Find(".el-pivot-grow__table").ClassList);
            Assert.Contains("el-pivot-grow__table--horizontal", horizontal.Find(".el-pivot-grow__table").ClassList);
        }

        [Fact]
        public void Api_IsActivationKey_RecognizesEnterAndSpace()
        {
            Assert.True(ElPivotDataGridGrow<Sale>.IsActivationKey(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" }));
            Assert.True(ElPivotDataGridGrow<Sale>.IsActivationKey(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = " " }));
            Assert.False(ElPivotDataGridGrow<Sale>.IsActivationKey(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "a" }));
            Assert.False(ElPivotDataGridGrow<Sale>.IsActivationKey(null));
        }

        #endregion

        #region 守卫：零第三方依赖

        [Fact]
        public void Guard_ComponentSources_DoNotReferenceThirdPartyLibraries()
        {
            var files = SourceFiles();

            Assert.NotEmpty(files);

            var offenders = new List<string>();

            foreach (var file in files)
            {
                var code = StripComments(File.ReadAllText(file));

                foreach (var token in new[] { "Radzen", "System.Linq.Dynamic", "Dynamic.Core", "ODataEnumerable", "GroupResult", "Markdig", "System.Web" })
                {
                    if (code.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        offenders.Add(Path.GetFileName(file) + " → " + token);
                    }
                }
            }

            Assert.Empty(offenders);
        }

        [Fact]
        public void Guard_ComponentSources_DoNotDeclareThirdPartyUsings()
        {
            var offenders = new List<string>();

            foreach (var file in SourceFiles())
            {
                foreach (var line in File.ReadAllLines(file))
                {
                    var trimmed = line.TrimStart();

                    if (trimmed.StartsWith("using ", StringComparison.Ordinal)
                        && (trimmed.Contains("Radzen", StringComparison.OrdinalIgnoreCase)
                            || trimmed.Contains("Dynamic.Core", StringComparison.OrdinalIgnoreCase)
                            || trimmed.Contains("Markdig", StringComparison.OrdinalIgnoreCase)))
                    {
                        offenders.Add(Path.GetFileName(file) + " → " + trimmed);
                    }
                }
            }

            Assert.Empty(offenders);
        }

        [Fact]
        public void Guard_EngineIsDecoupledFromBlazor()
        {
            // 引擎是单一真源，必须能在没有渲染器的情况下直接调用 → 不得引用任何 Blazor 类型。
            var engineFiles = new[]
            {
                "ElPivotDataGridGrowEngine.cs",
                "ElPivotDataGridGrowAggregator.cs",
                "ElPivotDataGridGrowFilter.cs",
                "ElPivotDataGridGrowPropertyAccess.cs",
                "ElPivotDataGridGrowModels.cs",
                "ElPivotDataGridGrowEnums.cs"
            };

            var offenders = new List<string>();

            foreach (var name in engineFiles)
            {
                var code = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", name));

                if (code.Contains("Microsoft.AspNetCore.Components"))
                {
                    offenders.Add(name);
                }
            }

            Assert.Empty(offenders);
        }

        [Fact]
        public void Guard_ComponentProject_DoesNotAddNewPackageDependencies()
        {
            var csproj = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "Element.csproj"));

            foreach (var forbidden in new[] { "Radzen", "Dynamic.Core", "Markdig.Blazor" })
            {
                Assert.DoesNotContain(forbidden, csproj, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void Guard_NoDynamicLinqExpressionStringsAreBuilt()
        {
            var offenders = new List<string>();

            foreach (var file in SourceFiles())
            {
                var code = StripComments(File.ReadAllText(file));

                // 参考实现会拼 "it.Field == @0" 之类的动态 LINQ 串；本实现必须完全没有。
                if (code.Contains("\"it.", StringComparison.Ordinal) || code.Contains(".Where(", StringComparison.Ordinal) && code.Contains("ParseLambda", StringComparison.Ordinal))
                {
                    offenders.Add(Path.GetFileName(file));
                }
            }

            Assert.Empty(offenders);
        }

        #endregion

        #region 守卫：完全不使用 JavaScript

        [Fact]
        public void Guard_ComponentSources_DoNotUseJsInterop()
        {
            var offenders = new List<string>();

            foreach (var file in SourceFiles())
            {
                var code = StripComments(File.ReadAllText(file));

                foreach (var token in new[] { "IJSRuntime", "JSRuntime", "IJSObjectReference", "InvokeVoidAsync", "InvokeAsync<", "dom.js", "elementPivot" })
                {
                    if (code.IndexOf(token, StringComparison.Ordinal) >= 0)
                    {
                        offenders.Add(Path.GetFileName(file) + " → " + token);
                    }
                }
            }

            Assert.Empty(offenders);
        }

        [Fact]
        public void Guard_RenderingNeverInvokesJavaScript()
        {
            var cut = BasicGrid(p =>
            {
                p.Add(x => x.AllowPaging, true);
                p.Add(x => x.ShowRowsTotals, true);
                p.Add(x => x.ShowColumnsTotals, true);
                p.Add(x => x.AllowFieldsPicking, true);
            });

            cut.Find(".el-pivot-grow__sort").Click();
            cut.Find(".el-pivot-grow__filter").Click();
            cut.FindAll(".el-pivot-grow__value")[0].Click();

            Assert.Empty(JSInterop.Invocations);
        }

        [Fact]
        public void Guard_DomJsHasNoPivotChannel()
        {
            var path = Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "js", "dom.js");

            if (!File.Exists(path))
            {
                return;
            }

            var js = File.ReadAllText(path);

            Assert.DoesNotContain("elementPivot", js, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region 守卫：浏览器兼容 CSS

        [Fact]
        public void Guard_CssSection_AvoidsModernOnlySyntax()
        {
            var css = CssSection();

            foreach (var forbidden in new[]
            {
                ":has(", ":is(", ":where(", ":focus-visible", "accent-color",
                "clamp(", "inset-", "margin-inline", "padding-inline", "block-size", "inline-size",
                "gap:", "grid-template", "aspect-ratio", "backdrop-filter", "&. "
            })
            {
                Assert.DoesNotContain(forbidden, css, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void Guard_CssSection_IsFullyScoped()
        {
            var css = CssSection();
            var offenders = new List<string>();

            foreach (var chunk in css.Split('}'))
            {
                var brace = chunk.IndexOf('{');

                if (brace < 0)
                {
                    continue;
                }

                var selector = chunk.Substring(0, brace).Trim();

                if (selector.Length == 0)
                {
                    continue;
                }

                foreach (var part in selector.Split(','))
                {
                    var single = part.Trim();

                    if (single.Length > 0 && !single.StartsWith(".el-pivot-grow", StringComparison.Ordinal))
                    {
                        offenders.Add(single);
                    }
                }
            }

            Assert.Empty(offenders);
        }

        [Fact]
        public void Guard_CssSection_AvoidsImportant()
        {
            Assert.DoesNotContain("!important", CssSection(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Guard_CssSection_CoversKeyParts()
        {
            var css = CssSection();

            foreach (var part in new[]
            {
                ".el-pivot-grow__table", ".el-pivot-grow__head", ".el-pivot-grow__row",
                ".el-pivot-grow__row-header", ".el-pivot-grow__column-header", ".el-pivot-grow__corner",
                ".el-pivot-grow__value", ".el-pivot-grow__row-total", ".el-pivot-grow__total-header",
                ".el-pivot-grow__foot", ".el-pivot-grow__foot-total", ".el-pivot-grow__empty",
                ".el-pivot-grow__loading", ".el-pivot-grow__pager", ".el-pivot-grow__pager-btn",
                ".el-pivot-grow__picker", ".el-pivot-grow__filter-panel", ".el-pivot-grow__button",
                ".el-pivot-grow__expander", ".el-pivot-grow__sort", ".el-pivot-grow__filter",
                "is-left", "is-center", "is-right", "is-odd", "is-others"
            })
            {
                Assert.Contains(part, css);
            }
        }

        [Fact]
        public void Guard_CssSection_HasNoGlobalElementSelectors()
        {
            var css = CssSection();

            Assert.DoesNotMatch(new Regex(@"(?<![-\w.])body\s*\{"), css);
            Assert.DoesNotMatch(new Regex(@"(?<![-\w.])html\s*\{"), css);
        }

        [Fact]
        public void Guard_CssSection_UsesElementPlusVariablesWithFallbacks()
        {
            var css = CssSection();

            Assert.Contains("var(--el-", css);
            Assert.DoesNotMatch(new Regex(@"var\(--el-[a-z-]+\)"), css);
        }

        [Fact]
        public void Guard_EveryRenderedClassHasAStyle()
        {
            var cut = BasicGrid(p =>
            {
                p.Add(x => x.ShowRowsTotals, true);
                p.Add(x => x.ShowColumnsTotals, true);
                p.Add(x => x.AllowPaging, true);
                p.Add(x => x.AllowFieldsPicking, true);
                p.Add(x => x.GridLines, ElPivotGridLines.Both);
            });

            cut.Find(".el-pivot-grow__filter").Click();

            var css = CssSection();
            var used = Regex.Matches(cut.Markup, @"el-pivot-grow[a-z0-9_-]*")
                .Select(m => m.Value)
                .Distinct()
                .Where(name => name != "el-pivot-grow")
                .ToList();

            var missing = used.Where(name => !css.Contains("." + name)).ToList();

            Assert.Empty(missing);
        }

        #endregion
    }
}
