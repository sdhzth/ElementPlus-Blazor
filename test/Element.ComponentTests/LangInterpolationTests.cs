using Bunit;
using ElementPlus;
using ElementPlus.Lang;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 词条插值能力与其首批消费者（分页 / 输入框字数 / 走马灯指示器）。
    /// <para>
    /// 插值语义必须与 EP 的 <c>useLocale.translate</c> 一致：只认 <c>{单词}</c>；
    /// <b>没有对应实参时原样保留占位符</b>（既不吞也不抛），数字按不变文化转字符串。
    /// </para>
    /// </summary>
    public class LangInterpolationTests : BunitContext
    {
        public LangInterpolationTests()
        {
            Services.AddElementServices();
        }

        private const string ZhJson = @"{
  ""el"": {
    ""pagination"": {
      ""total"": ""共 {total} 条"",
      ""pagesize"": ""条/页"",
      ""goto"": ""前往"",
      ""page"": ""页"",
      ""currentPage"": ""第 {pager} 页"",
      ""prev"": ""上一页"",
      ""next"": ""下一页""
    },
    ""input"": { ""characters"": ""{count} / {max} 个汉字"" },
    ""carousel"": { ""indicator"": ""切到第 {index} 张"" },
    ""table"": { ""sortLabel"": ""按 {column} 排序"", ""filterLabel"": ""按 {column} 过滤"" }
  }
}";

        // ===== LocaleFormat：语义边界（与 EP 的 translate 逐条对齐）=====

        [Fact]
        public void InterpolateWithoutArgsKeepsPlaceholderLiteral()
        {
            // EP：option 缺这个 key 时返回 `{key}` 本身，不吞不抛
            Assert.Equal("共 {total} 条", LocaleFormat.Interpolate("共 {total} 条"));
            Assert.Equal("共 {total} 条", LocaleFormat.Interpolate("共 {total} 条", ("pager", 3)));
        }

        [Fact]
        public void InterpolateHandlesMultiplePlaceholdersInAnyOrder()
        {
            Assert.Equal("3 / 10", LocaleFormat.Interpolate("{count} / {max}", ("max", 10), ("count", 3)));
        }

        [Fact]
        public void InterpolateIgnoresNonWordAndEmptyBraces()
        {
            // 只认 {单词}：带点、带空格、空的都不算占位符，原样留着
            Assert.Equal(" { } 和 {a.b} ", LocaleFormat.Interpolate(" { } 和 {a.b} ", ("a", 1)));
            Assert.Equal("{}", LocaleFormat.Interpolate("{}", ("", 1)));
        }

        [Fact]
        public void InterpolatePassesThroughTextWithoutBraces()
        {
            Assert.Equal("纯文字", LocaleFormat.Interpolate("纯文字", ("x", 1)));
            Assert.Null(LocaleFormat.Interpolate(null, ("x", 1)));
        }

        [Fact]
        public void InterpolateUsesInvariantCultureAndTolerantValues()
        {
            // 数字不能带区域分隔符，也不能因区域设置变成 1.234,00
            Assert.Equal("1234 条", LocaleFormat.Interpolate("{total} 条", ("total", 1234)));
            Assert.Equal("1.5", LocaleFormat.Interpolate("{v}", ("v", 1.5)));
            // null 实参 → 空串（EP 的 ?? 也会给出空），不能打印成 "null"
            Assert.Equal("[]", LocaleFormat.Interpolate("[{v}]", ("v", null)));
            // 重名取第一个
            Assert.Equal("a", LocaleFormat.Interpolate("{v}", ("v", "a"), ("v", "b")));
        }

        [Fact]
        public void TemplateAndTInterpolateTheChosenText()
        {
            var lang = TestLang.Loaded(ZhJson, "zh-CN");

            // 词条命中 → 插词条
            Assert.Equal("共 42 条", lang.T("el.pagination.total", "fb {total}", ("total", 42)));
            // 词条没命中 → 插 fallback（这条很容易漏：只插词条会让默认文案露出 {total}）
            Assert.Equal("剩 7 项", lang.T("el.nope.exists", "剩 {n} 项", ("n", 7)));
            // Template：宿主模板优先，而且照样插值
            Assert.Equal("合计 9 笔", lang.Template("el.pagination.total", "合计 {total} 笔", "fb", ("total", 9)));
            // Text 不插值：宿主给的是字面文字
            Assert.Equal("确定", lang.Text("el.messagebox.confirm", "确定", "OK"));
        }

        // ===== 消费方：分页 =====

        [Fact]
        public void PaginationTextsComeFromEntriesWithNumbersSubstituted()
        {
            Services.AddSingleton<ILang>(TestLang.Loaded(ZhJson, "zh-CN"));

            var cut = Render<ElPagination>(p => p
                .Add(x => x.Total, 128)
                .Add(x => x.PageSize, 20)
                .Add(x => x.CurrentPage, 3)
                .Add(x => x.Layout, "total, sizes, prev, pager, next, jumper"));

            Assert.Equal("共 128 条", cut.Find(".el-pagination__total").TextContent.Trim());
            Assert.Contains("20 条/页", cut.Markup);
            Assert.Contains("前往", cut.Markup);
            Assert.Equal("第 3 页", cut.Find(".el-pagination__editor input").GetAttribute("aria-label"));
            Assert.Contains("上一页", cut.Markup);
            Assert.Contains("下一页", cut.Markup);
        }

        [Fact]
        public void PaginationHostTemplateIsStillInterpolated()
        {
            Services.AddSingleton<ILang>(TestLang.Loaded(ZhJson, "zh-CN"));

            var cut = Render<ElPagination>(p => p
                .Add(x => x.Total, 7)
                .Add(x => x.TotalText, "合计 {total} 项")
                .Add(x => x.Layout, "total"));

            // 宿主给的是模板而不是成品文字 —— 用 Text() 就会漏出 {total}
            Assert.Equal("合计 7 项", cut.Find(".el-pagination__total").TextContent.Trim());
        }

        [Fact]
        public void PaginationFallsBackToPreEntryTextsWithoutEntries()
        {
            var cut = Render<ElPagination>(p => p
                .Add(x => x.Total, 95)
                .Add(x => x.CurrentPage, 1)
                .Add(x => x.Layout, "total, prev, next, jumper"));

            Assert.Equal("共 95 条", cut.Find(".el-pagination__total").TextContent.Trim());
            // prev / next 的可访问名称是新补的缺口，不接词条时给的是 EP 的英文值
            Assert.Contains("Go to previous page", cut.Markup);
            Assert.Contains("Go to next page", cut.Markup);
            Assert.Equal("页码", cut.Find(".el-pagination__editor input").GetAttribute("aria-label"));
        }

        // ===== 消费方：输入框字数与走马灯指示器 =====

        [Fact]
        public void InputCountUsesEntriesTemplateWhenLoaded()
        {
            Services.AddSingleton<ILang>(TestLang.Loaded(ZhJson, "zh-CN"));

            var cut = Render<ElInput<string>>(p => p
                .Add(x => x.Value, "abcdefg")
                .Add(x => x.Maxlength, 7)
                .Add(x => x.ShowWordLimit, true));

            Assert.Equal("7 / 7 个汉字", cut.Find(".el-input__count-inner").TextContent.Trim());
        }

        [Fact]
        public void InputCountKeepsPlainNumbersWithoutEntries()
        {
            var cut = Render<ElInput<string>>(p => p
                .Add(x => x.Value, "abcdefg")
                .Add(x => x.Maxlength, 7)
                .Add(x => x.ShowWordLimit, true));

            Assert.Equal("7 / 7", cut.Find(".el-input__count-inner").TextContent.Trim());
        }

        [Fact]
        public void CarouselIndicatorGetsAccessibleNameWithIndex()
        {
            Services.AddSingleton<ILang>(TestLang.Loaded(ZhJson, "zh-CN"));

            var cut = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.ChildContent, Items(3)));

            var labels = new List<string>();
            foreach (var button in cut.FindAll(".el-carousel__button"))
            {
                labels.Add(button.GetAttribute("aria-label"));
            }

            // 与 EP 一样是 0 基索引
            Assert.Equal(new[] { "切到第 0 张", "切到第 1 张", "切到第 2 张" }, labels);
        }

        /// <summary>
        /// 表格排序/筛选控件的可访问名称：此前只有 <c>aria-sort</c> 与图标，读屏说不出"按哪一列"。
        /// 键与 EP 同名（<c>el.table.sortLabel</c> / <c>el.table.filterLabel</c>），列名要插进 <c>{column}</c>。
        /// </summary>
        [Fact]
        public void TableSortAndFilterControlsGetAccessibleNames()
        {
            var cut = RenderTableWithSortAndFilter();

            Assert.Equal("Sort by 姓名", cut.Find(".caret-wrapper").GetAttribute("aria-label"));
            Assert.Equal("Filter by 姓名", cut.Find(".el-table__column-filter-trigger").GetAttribute("aria-label"));
        }

        [Fact]
        public void TableAccessibleNamesFollowEntries()
        {
            Services.AddSingleton<ILang>(TestLang.Loaded(ZhJson, "zh-CN"));

            var cut = RenderTableWithSortAndFilter();

            Assert.Equal("按 姓名 排序", cut.Find(".caret-wrapper").GetAttribute("aria-label"));
            Assert.Equal("按 姓名 过滤", cut.Find(".el-table__column-filter-trigger").GetAttribute("aria-label"));
        }

        private IRenderedComponent<ElTable> RenderTableWithSortAndFilter()
        {
            return Render<ElTable>(p => p
                .Add(x => x.DataSource, new List<AriaRow>())
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.ChildContent, builder =>
                {
                    builder.OpenComponent<ElTableColumn>(0);
                    builder.AddAttribute(1, nameof(ElTableColumn.Property), "Name");
                    builder.AddAttribute(2, nameof(ElTableColumn.Text), "姓名");
                    builder.AddAttribute(3, nameof(ElTableColumn.Sortable), true);
                    builder.AddAttribute(4, nameof(ElTableColumn.Filters),
                        new List<TableFilterOption> { new TableFilterOption { Text = "甲", Value = "a" } });
                    builder.CloseComponent();
                }));
        }

        private static RenderFragment Items(int count)
        {
            return builder =>
            {
                for (var i = 0; i < count; i++)
                {
                    var seq = i * 3;
                    var text = "Slide " + (i + 1);
                    builder.OpenComponent<ElCarouselItem>(seq);
                    builder.AddAttribute(seq + 1, "ChildContent", (RenderFragment)(inner => inner.AddContent(0, text)));
                    builder.CloseComponent();
                }
            };
        }

        private sealed class AriaRow
        {
            public string Name { get; set; }
        }
    }
}