using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementPlus.Blazor.Demo.Models
{
    /// <summary>
    /// 演示用的实体模型，供 Table / Tree / Select / 表单等需要数据的组件复用。
    /// </summary>
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool OnSale { get; set; }
        public DateTime CreatedAt { get; set; }
        /// <summary>供应商编码，用于可编辑表格里的 ElDropDownTableGrow 单元格编辑器。</summary>
        public string Supplier { get; set; } = string.Empty;
    }

    /// <summary>下拉/下拉表格编辑器用的选项模型。</summary>
    public class OptionItem
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>供应商（ElDropDownTableGrow 的数据源）。</summary>
    public class Supplier
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    public class Patient
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
    }

    /// <summary>
    /// 通用树节点（供 Tree 等层级组件演示）。
    /// </summary>
    public class TreeNode
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public List<TreeNode> Children { get; set; } = new List<TreeNode>();
    }

    public static class MockData
    {
        public static List<Product> Products { get; } = new List<Product>
        {
            new Product { Id = 1, Name = "阿莫西林胶囊", Price = 12.5m, Stock = 320, Category = "西药", Supplier = "S01", OnSale = true, CreatedAt = new DateTime(2026,1,12,9,30,0) },
            new Product { Id = 2, Name = "感冒灵颗粒", Price = 18.0m, Stock = 156, Category = "中成药", Supplier = "S02", OnSale = true, CreatedAt = new DateTime(2026,2,3,14,15,0) },
            new Product { Id = 3, Name = "布洛芬缓释片", Price = 22.8m, Stock = 88, Category = "西药", Supplier = "S03", OnSale = false, CreatedAt = new DateTime(2026,3,21,10,0,0) },
            new Product { Id = 4, Name = "维生素C片", Price = 9.9m, Stock = 540, Category = "保健品", Supplier = "S04", OnSale = true, CreatedAt = new DateTime(2026,4,9,16,45,0) },
            new Product { Id = 5, Name = "生理盐水", Price = 4.5m, Stock = 1200, Category = "耗材", Supplier = "S05", OnSale = false, CreatedAt = new DateTime(2026,5,1,8,20,0) },
            new Product { Id = 6, Name = "头孢克肟分散片", Price = 35.6m, Stock = 64, Category = "西药", Supplier = "S01", OnSale = true, CreatedAt = new DateTime(2026,6,15,11,5,0) },
        };

        /// <summary>扩展产品清单（23 条），专门给 ElTable 分页演示用，确保跨多页。</summary>
        /// <summary>类别选项，可编辑表格里 ElSelect 单元格编辑器的数据源。</summary>
        public static List<OptionItem> Categories { get; } = new List<OptionItem>
        {
            new OptionItem { Value = "西药", Text = "西药" },
            new OptionItem { Value = "中成药", Text = "中成药" },
            new OptionItem { Value = "保健品", Text = "保健品" },
            new OptionItem { Value = "耗材", Text = "耗材" },
        };

        /// <summary>供应商，可编辑表格里 ElDropDownTableGrow 单元格编辑器的数据源。</summary>
        public static List<Supplier> Suppliers { get; } = new List<Supplier>
        {
            new Supplier { Code = "S01", Name = "云溪医药控股", Contact = "Kevin Carter", Phone = "138-0000-0001" },
            new Supplier { Code = "S02", Name = "北屿医药供应链", Contact = "Laura Bennett", Phone = "138-0000-0002" },
            new Supplier { Code = "S03", Name = "星澜医药站", Contact = "Fiona Hayes", Phone = "138-0000-0003" },
            new Supplier { Code = "S04", Name = "青梧医药物流", Contact = "Victor Adams", Phone = "138-0000-0004" },
            new Supplier { Code = "S05", Name = "澜溪器械耗材", Contact = "Nina Foster", Phone = "138-0000-0005" },
        };

        public static List<Product> ProductsLarge { get; } = BuildProductsLarge();

        private static List<Product> BuildProductsLarge()
        {
            var cats = new[] { "西药", "中成药", "保健品", "耗材" };
            var names = new[] { "阿莫西林胶囊", "感冒灵颗粒", "布洛芬缓释片", "维生素C片", "生理盐水", "头孢克肟分散片", "复方甘草片", "阿奇霉素", "板蓝根颗粒", "健胃消食片" };
            var rnd = new Random(20260923);
            var list = new List<Product>();
            for (var i = 1; i <= 23; i++)
            {
                list.Add(new Product
                {
                    Id = i,
                    Name = i <= names.Length ? names[i - 1] : $"{names[(i - 1) % names.Length]}(批次{i})",
                    Category = cats[rnd.Next(cats.Length)],
                    Price = Math.Round((decimal)(rnd.NextDouble() * 80 + 5), 2),
                    Stock = rnd.Next(20, 1500),
                    OnSale = rnd.Next(2) == 0,
                    CreatedAt = new DateTime(2026, 1, 1).AddDays(i * 3),
                });
            }
            return list;
        }

        public static List<Patient> Patients { get; } = new List<Patient>
        {
            new Patient { Id = 1, Name = "Kevin Carter", Age = 45, Gender = "男", Department = "内科", Diagnosis = "高血压" },
            new Patient { Id = 2, Name = "Laura Bennett", Age = 32, Gender = "女", Department = "妇科", Diagnosis = "盆腔炎" },
            new Patient { Id = 3, Name = "Fiona Hayes", Age = 58, Gender = "女", Department = "心内科", Diagnosis = "冠心病" },
            new Patient { Id = 4, Name = "Victor Adams", Age = 27, Gender = "男", Department = "骨科", Diagnosis = "骨折" },
        };

        public static List<TreeNode> OrgTree { get; } = new List<TreeNode>
        {
            new TreeNode { Label = "某某市人民医院", Value = "hospital", Children = new List<TreeNode>
            {
                new TreeNode { Label = "内科", Value = "internal", Children = new List<TreeNode>
                {
                    new TreeNode { Label = "心血管", Value = "cardio" },
                    new TreeNode { Label = "呼吸", Value = "resp" },
                } },
                new TreeNode { Label = "外科", Value = "surgery", Children = new List<TreeNode>
                {
                    new TreeNode { Label = "骨科", Value = "ortho" },
                    new TreeNode { Label = "普外", Value = "general" },
                } },
                new TreeNode { Label = "妇产科", Value = "obgyn" },
            } },
            new TreeNode { Label = "医技科室", Value = "tech", Children = new List<TreeNode>
            {
                new TreeNode { Label = "检验科", Value = "lab" },
                new TreeNode { Label = "影像科", Value = "radiology" },
            } },
        };

        public static List<string> PlainOptions { get; } = new List<string>
        {
            "选项一", "选项二", "选项三", "选项四"
        };

        public static List<(string Label, string Value)> KeyValueOptions { get; } = new List<(string, string)>
        {
            ("阿莫西林", "amoxicillin"),
            ("布洛芬", "ibuprofen"),
            ("头孢克肟", "cefixime"),
            ("维生素C", "vitc"),
        };

        /// <summary>透视表演示用的订单记录。</summary>
        public class Order
        {
            public string Region { get; set; } = string.Empty;
            public int Year { get; set; }
            public string Category { get; set; } = string.Empty;
            public string Product { get; set; } = string.Empty;
            public decimal Amount { get; set; }
        }

        public static List<Order> Orders { get; } = new List<Order>
        {
            new Order { Region = "华东", Year = 2025, Category = "西药", Product = "阿莫西林", Amount = 12000m },
            new Order { Region = "华东", Year = 2025, Category = "中成药", Product = "感冒灵", Amount = 8000m },
            new Order { Region = "华北", Year = 2025, Category = "西药", Product = "布洛芬", Amount = 15000m },
            new Order { Region = "华北", Year = 2026, Category = "西药", Product = "头孢克肟", Amount = 9500m },
            new Order { Region = "华南", Year = 2026, Category = "保健品", Product = "维生素C", Amount = 6000m },
            new Order { Region = "华南", Year = 2025, Category = "耗材", Product = "生理盐水", Amount = 4200m },
            new Order { Region = "华东", Year = 2026, Category = "西药", Product = "阿莫西林", Amount = 13500m },
            new Order { Region = "华北", Year = 2026, Category = "中成药", Product = "感冒灵", Amount = 7200m },
        };

        /// <summary>图表演示用的销量点。</summary>
        public class ChartPoint
        {
            public string Category { get; set; } = string.Empty;
            public double Value { get; set; }
        }

        /// <summary>热力图演示用的一格：行 / 列 / 值。</summary>
        public class HeatPoint
        {
            public string Cell { get; set; } = string.Empty;
            public string Slot { get; set; } = string.Empty;
            public double Count { get; set; }
        }

        /// <summary>K 线 / OHLC / 高低图演示用的一根蜡烛（四价）。</summary>
        public class PricePoint
        {
            public string Category { get; set; } = string.Empty;
            public double Open { get; set; }
            public double High { get; set; }
            public double Low { get; set; }
            public double Close { get; set; }
        }

        /// <summary>箱线图演示用的一组五数概括（最小 / Q1 / 中位 / Q3 / 最大）。</summary>
        public class BoxPoint
        {
            public string Category { get; set; } = string.Empty;
            public double Min { get; set; }
            public double Q1 { get; set; }
            public double Median { get; set; }
            public double Q3 { get; set; }
            public double Max { get; set; }
        }

        /// <summary>范围图（范围柱 / 范围条 / 范围面积）演示用的上下界。</summary>
        public class RangePoint
        {
            public string Category { get; set; } = string.Empty;
            public double Low { get; set; }
            public double High { get; set; }
        }

        /// <summary>瀑布图演示用的一项收支；<see cref="IsTotal"/> 非 0 视为小计/合计行。</summary>
        public class WaterfallPoint
        {
            public string Category { get; set; } = string.Empty;
            public double Amount { get; set; }
            public int IsTotal { get; set; }
        }

        /// <summary>子弹图演示用的一行 KPI（实际值 + 目标值）。</summary>
        public class BulletPoint
        {
            public string Category { get; set; } = string.Empty;
            public double Actual { get; set; }
            public double Target { get; set; }
        }

        /// <summary>散点 / 气泡图演示数据（X、Y 走数值轴，Size 为气泡第三维）。</summary>
        public static List<ScatterPoint> ScatterPoints { get; } = new List<ScatterPoint>
        {
            new ScatterPoint { X = 1.2, Y = 3.4, Size = 12 },
            new ScatterPoint { X = 2.1, Y = 4.8, Size = 20 },
            new ScatterPoint { X = 0.8, Y = 2.2, Size = 8 },
            new ScatterPoint { X = 3.0, Y = 5.5, Size = 28 },
            new ScatterPoint { X = 2.6, Y = 3.9, Size = 16 },
            new ScatterPoint { X = 3.8, Y = 6.1, Size = 32 },
            new ScatterPoint { X = 1.5, Y = 2.8, Size = 10 },
            new ScatterPoint { X = 4.2, Y = 5.0, Size = 24 },
        };

        /// <summary>散点 / 气泡图用的二维点（数值 X、Y + 气泡半径 Size）。</summary>
        public class ScatterPoint
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Size { get; set; }
        }

        /// <summary>
        /// 热力图数据：一周七天的三个时段门诊量。
        /// 之所以用「星期 × 时段」这种二维分类而不是数值轴，是为了覆盖
        /// <c>NumericAxes = false</c> 这条分类轴路径（默认值）。
        /// </summary>
        public static List<HeatPoint> OutpatientHeat { get; } = BuildOutpatientHeat();

        private static List<HeatPoint> BuildOutpatientHeat()
        {
            var days = new[] { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
            var slots = new[] { "上午", "下午", "夜间" };
            var peak = new double[,]
            {
                { 186, 142, 38 },
                { 172, 155, 41 },
                { 191, 148, 36 },
                { 178, 161, 44 },
                { 203, 172, 52 },
                { 96, 88, 61 },
                { 62, 54, 47 },
            };

            var list = new List<HeatPoint>();
            for (var d = 0; d < days.Length; d++)
            {
                for (var s = 0; s < slots.Length; s++)
                {
                    list.Add(new HeatPoint { Cell = days[d], Slot = slots[s], Count = peak[d, s] });
                }
            }

            return list;
        }

        public static List<ChartPoint> SalesPoints { get; } = new List<ChartPoint>
        {
            new ChartPoint { Category = "一月", Value = 320 },
            new ChartPoint { Category = "二月", Value = 410 },
            new ChartPoint { Category = "三月", Value = 280 },
            new ChartPoint { Category = "四月", Value = 530 },
            new ChartPoint { Category = "五月", Value = 470 },
        };

        /// <summary>金额序列（量级远大于 <see cref="SalesPoints"/>），专门用来演示命名次坐标轴。</summary>
        public static List<ChartPoint> RevenuePoints { get; } = new List<ChartPoint>
        {
            new ChartPoint { Category = "一月", Value = 3200 },
            new ChartPoint { Category = "二月", Value = 4100 },
            new ChartPoint { Category = "三月", Value = 2800 },
            new ChartPoint { Category = "四月", Value = 5300 },
            new ChartPoint { Category = "五月", Value = 4700 },
        };

        /// <summary>第二条销量序列（与 <see cref="SalesPoints"/> 同类别），用于堆叠 / 100% 堆叠演示。</summary>
        public static List<ChartPoint> SalesPointsB { get; } = new List<ChartPoint>
        {
            new ChartPoint { Category = "一月", Value = 180 },
            new ChartPoint { Category = "二月", Value = 260 },
            new ChartPoint { Category = "三月", Value = 340 },
            new ChartPoint { Category = "四月", Value = 240 },
            new ChartPoint { Category = "五月", Value = 390 },
        };

        /// <summary>K 线演示数据（收盘 &gt; 开盘 = 涨，按中国习惯画红）。</summary>
        public static List<PricePoint> PriceCandles { get; } = new List<PricePoint>
        {
            new PricePoint { Category = "1/6", Open = 42.10, High = 44.80, Low = 41.20, Close = 44.10 },
            new PricePoint { Category = "1/7", Open = 44.10, High = 45.60, Low = 42.30, Close = 42.80 },
            new PricePoint { Category = "1/8", Open = 42.80, High = 46.20, Low = 42.50, Close = 45.90 },
            new PricePoint { Category = "1/9", Open = 45.90, High = 47.10, Low = 44.60, Close = 45.20 },
            new PricePoint { Category = "1/10", Open = 45.20, High = 45.80, Low = 41.90, Close = 42.40 },
            new PricePoint { Category = "1/13", Open = 42.40, High = 44.30, Low = 42.00, Close = 43.90 },
        };

        /// <summary>箱线图演示数据（各科室平均住院日的分布）。</summary>
        public static List<BoxPoint> StayBoxes { get; } = new List<BoxPoint>
        {
            new BoxPoint { Category = "内科", Min = 4.2, Q1 = 6.1, Median = 7.4, Q3 = 9.2, Max = 12.5 },
            new BoxPoint { Category = "外科", Min = 5.8, Q1 = 8.0, Median = 9.6, Q3 = 12.1, Max = 16.8 },
            new BoxPoint { Category = "儿科", Min = 3.1, Q1 = 4.2, Median = 5.0, Q3 = 6.3, Max = 8.4 },
            new BoxPoint { Category = "妇产科", Min = 3.6, Q1 = 5.0, Median = 5.8, Q3 = 7.1, Max = 9.9 },
            new BoxPoint { Category = "骨科", Min = 6.4, Q1 = 9.8, Median = 12.0, Q3 = 15.4, Max = 21.2 },
        };

        /// <summary>范围图演示数据（一天内的体温区间）。</summary>
        public static List<RangePoint> TempRanges { get; } = new List<RangePoint>
        {
            new RangePoint { Category = "8:00", Low = 36.2, High = 36.8 },
            new RangePoint { Category = "12:00", Low = 36.5, High = 37.2 },
            new RangePoint { Category = "16:00", Low = 36.7, High = 37.6 },
            new RangePoint { Category = "20:00", Low = 36.6, High = 37.4 },
            new RangePoint { Category = "24:00", Low = 36.3, High = 37.0 },
        };

        /// <summary>瀑布图演示数据（门诊收入逐月变化 + 末行合计）。</summary>
        public static List<WaterfallPoint> RevenueWaterfall { get; } = new List<WaterfallPoint>
        {
            new WaterfallPoint { Category = "一月", Amount = 320 },
            new WaterfallPoint { Category = "二月", Amount = 90 },
            new WaterfallPoint { Category = "三月", Amount = -130 },
            new WaterfallPoint { Category = "四月", Amount = 250 },
            new WaterfallPoint { Category = "五月", Amount = -60 },
            new WaterfallPoint { Category = "合计", Amount = 0, IsTotal = 1 },
        };

        /// <summary>子弹图演示数据（各科室 KPI 完成情况）。</summary>
        public static List<BulletPoint> DeptKpi { get; } = new List<BulletPoint>
        {
            new BulletPoint { Category = "门诊量", Actual = 82, Target = 90 },
            new BulletPoint { Category = "床位使用率", Actual = 94, Target = 88 },
            new BulletPoint { Category = "平均住院日", Actual = 62, Target = 75 },
            new BulletPoint { Category = "药占比", Actual = 41, Target = 38 },
        };

        /// <summary>示例参考线（销量目标线）。</summary>
        public static List<ElementPlus.ElChartReferenceLine> SalesReferenceLines { get; } = new List<ElementPlus.ElChartReferenceLine>
        {
            new ElementPlus.ElChartReferenceLine { Value = 450, Label = "目标 450", Color = "#e6a23c" },
        };

        /// <summary>
        /// 把层级化的 <see cref="OrgTree"/> 摊平成 ElTree 默认 DataFormat=List 所需的结构：
        /// 唯一 Id + ParentId（根节点 ParentId=0）。
        /// 注意：Id 必须唯一，若 Id 与 ParentId 取值相同（例如都为默认 0），
        /// ElTree 会把所有节点互相当成子节点从而形成环，必须避免。
        /// </summary>
        public static List<ElementPlus.TreeItemBase> FlatOrgTree()
        {
            var flat = new List<ElementPlus.TreeItemBase>();
            var nextId = 0;

            void Add(TreeNode n, int parentId)
            {
                var node = new ElementPlus.Model.TreeItemModel
                {
                    Id = ++nextId,
                    ParentId = parentId,
                    Text = n.Label,
                    HasChildren = n.Children.Count > 0,
                };
                flat.Add(node);
                foreach (var child in n.Children)
                {
                    Add(child, node.Id);
                }
            }

            foreach (var root in OrgTree)
            {
                Add(root, 0);
            }

            return flat;
        }

        /// <summary>虚拟滚动（ElTableV2 / ElTreeV2）演示用的较大数据集。</summary>
        public static List<Product> BigProducts { get; } = Enumerable.Range(1, 200)
            .Select(i => new Product
            {
                Id = i,
                Name = "药品-" + i.ToString("D3"),
                Category = (i % 3) switch { 0 => "西药", 1 => "中成药", _ => "耗材" },
                Price = 5m + i % 60,
                Stock = (i * 7) % 500,
                OnSale = i % 2 == 0,
                CreatedAt = new DateTime(2026, 1, 1).AddDays(i),
            })
            .ToList();
    }
}
