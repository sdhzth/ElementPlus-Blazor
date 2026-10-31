using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementPlus
{
    public partial class ElDescriptions : ElementComponentBase
    {
        private readonly List<ElDescriptionsItem> items = new List<ElDescriptionsItem>();

        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public RenderFragment TitleContent { get; set; }

        [Parameter]
        public RenderFragment Extra { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public int Column { get; set; } = 3;

        [Parameter]
        public bool Border { get; set; }

        [Parameter]
        public DescriptionsDirection Direction { get; set; } = DescriptionsDirection.Horizontal;

        [Parameter]
        public ElementSize Size { get; set; } = ElementSize.Default;

        /// <summary>显示冒号（默认 false，对齐 EP 官方无冒号；设为 true 可开启昵称式冒号）。</summary>
        [Parameter]
        public bool Colon { get; set; } = false;

        /// <summary>列标签宽度（EP 2.8.8 label-width）；子项 LabelWidth 优先级更高。</summary>
        [Parameter]
        public string LabelWidth { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        internal IReadOnlyList<ElDescriptionsItem> Items => items.OrderBy(x => x.Order).ToList();

        internal int EffectiveColumn => Column <= 0 ? 1 : Column;

        internal void AddItem(ElDescriptionsItem item)
        {
            if (!items.Contains(item))
            {
                items.Add(item);
                Refresh();
            }
        }

        internal void RemoveItem(ElDescriptionsItem item)
        {
            if (items.Remove(item))
            {
                Refresh();
            }
        }

        internal string DescriptionsClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-descriptions", Cls)
            .AddIf(Size == ElementSize.Large, "el-descriptions--large")
            .AddIf(Size == ElementSize.Small, "el-descriptions--small")
            .ToString();

        internal string TableClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-descriptions__table")
            .AddIf(Border, "is-bordered")
            .ToString();

        /// <summary>
        /// 按 EP <c>getRows()</c> 算法把 Items 排成行：
        /// 每行最多 <see cref="EffectiveColumn"/> 个「项目位」，项目可跨列（span），
        /// 可跨行（rowspan 通过 rowspanTemp 占用后续行的列位）；最后一项自动填满本行剩余列位。
        /// </summary>
        internal List<DescRow> Rows
        {
            get
            {
                var rows = new List<DescRow>();
                var current = new DescRow();
                int count = EffectiveColumn;
                int totalSpan = 0;
                var rowspanTemp = new List<int> { 0 };
                int i = 0;
                int n = Items.Count;
                foreach (var item in Items)
                {
                    int span = item.NormalizedSpan;
                    int rowspan = Math.Max(1, item.RowSpan);
                    int rowNo = rows.Count;
                    EnsureIndex(rowspanTemp, rowNo);
                    if (rowspan > 1)
                    {
                        for (int k = 1; k < rowspan; k++)
                        {
                            EnsureIndex(rowspanTemp, rowNo + k);
                            rowspanTemp[rowNo + k]++;
                            totalSpan++;
                        }
                    }
                    if (rowspanTemp[rowNo] > 0)
                    {
                        count -= rowspanTemp[rowNo];
                        rowspanTemp[rowNo] = 0;
                    }
                    if (i < n - 1)
                    {
                        totalSpan += span > count ? count : span;
                    }
                    if (i == n - 1)
                    {
                        int lastSpan = EffectiveColumn - (totalSpan % EffectiveColumn);
                        int eff = Math.Max(1, Math.Min(span > count ? count : lastSpan, EffectiveColumn));
                        current.Cells.Add((item, eff, rowspan));
                        rows.Add(current);
                        return rows;
                    }
                    if (span < count)
                    {
                        count -= span;
                        current.Cells.Add((item, span, rowspan));
                    }
                    else
                    {
                        int eff = span > count ? count : span;
                        current.Cells.Add((item, eff, rowspan));
                        rows.Add(current);
                        count = EffectiveColumn;
                        current = new DescRow();
                    }
                    i++;
                }
                if (current.Cells.Count > 0) rows.Add(current);
                return rows;
            }
        }

        private static void EnsureIndex(List<int> list, int idx)
        {
            while (list.Count <= idx) list.Add(0);
        }

        internal sealed class DescRow
        {
            public List<(ElDescriptionsItem Item, int ColSpan, int RowSpan)> Cells { get; } = new();
        }
    }
}
