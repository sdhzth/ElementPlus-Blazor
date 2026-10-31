using Bunit;
using ElementPlus;
using ElementPlus.Lang;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 「下拉与空态族」文案的词条接入契约：
    /// <b>显式参数 &gt; 词条 &gt; 改动前那个字面量</b>，且不接词条时 DOM 必须与接入前一字不变。
    /// 键名逐个对齐 EP（<c>useSelect.ts</c> 里就是 <c>props.loadingText || t('el.select.loading')</c> 同形优先级）。
    /// </summary>
    public class LangDropdownTextTests : BunitContext
    {
        public LangDropdownTextTests()
        {
            Services.AddElementPlusServices();
        }

        private const string EnJson = @"{
  ""el"": {
    ""select"": { ""placeholder"": ""Select"", ""loading"": ""Loading"", ""noData"": ""No data"", ""noMatch"": ""No matching data"" },
    ""cascader"": { ""placeholder"": ""Choose"", ""noData"": ""Cascade no data"", ""noMatch"": ""Cascade no match"" },
    ""table"": { ""emptyText"": ""Table empty"" },
    ""tree"": { ""emptyText"": ""Tree empty"" }
  }
}";

        /// <summary>zh-CN 夹具：值与 EP 的 zh-cn.ts 一致，用来测"切换前"的那一帧。</summary>
        private const string ZhJson = @"{
  ""el"": {
    ""select"": { ""placeholder"": ""请选择"", ""loading"": ""加载中"", ""noData"": ""无数据"", ""noMatch"": ""无匹配数据"" },
    ""cascader"": { ""placeholder"": ""请选择"", ""noData"": ""暂无数据"", ""noMatch"": ""无匹配数据"" },
    ""table"": { ""emptyText"": ""暂无数据"" },
    ""tree"": { ""emptyText"": ""暂无数据"" }
  }
}";

        private void UseEnglishEntries()
        {
            Services.AddSingleton<ILang>(TestLang.Loaded(EnJson, "en-US"));
        }

        private static string PlaceholderOf<T>(IRenderedComponent<T> cut)
            where T : IComponent
        {
            return cut.Find("input").GetAttribute("placeholder");
        }

        [Fact]
        public void SelectPlaceholderKeepsExistingChineseWithoutEntries()
        {
            var cut = Render<ElSelect<string>>();

            Assert.Equal("请选择", PlaceholderOf(cut));
        }

        [Fact]
        public void SelectPlaceholderUsesEntries()
        {
            UseEnglishEntries();

            var cut = Render<ElSelect<string>>();

            Assert.Equal("Select", PlaceholderOf(cut));
        }

        [Fact]
        public void SelectExplicitPlaceholderWinsOverEntries()
        {
            UseEnglishEntries();

            var cut = Render<ElSelect<string>>(p => p.Add(x => x.Placeholder, "挑一个"));

            Assert.Equal("挑一个", PlaceholderOf(cut));
        }

        [Fact]
        public void SelectV2PlaceholderKeepsChineseAndFollowsEntries()
        {
            var bare = Render<ElSelectV2<int>>(p => p.Add(x => x.Options, Enumerable.Empty<SelectV2Option>()));
            Assert.Equal("请选择", PlaceholderOf(bare));
        }

        [Fact]
        public void SelectV2PlaceholderUsesEntries()
        {
            UseEnglishEntries();

            var cut = Render<ElSelectV2<int>>(p => p.Add(x => x.Options, Enumerable.Empty<SelectV2Option>()));

            Assert.Equal("Select", PlaceholderOf(cut));
        }

        [Fact]
        public void CascaderPlaceholderKeepsChineseAndFollowsEntries()
        {
            var bare = Render<ElCascader>();
            Assert.Equal("请选择", PlaceholderOf(bare));
        }

        [Fact]
        public void CascaderPlaceholderUsesItsOwnKeyNotSelects()
        {
            UseEnglishEntries();

            var cut = Render<ElCascader>();

            // el.cascader.placeholder 与 el.select.placeholder 在夹具里刻意给了不同值，
            // 取错命名空间会立刻看得出来（本仓级联与选择器是两套 EP 键）
            Assert.Equal("Choose", PlaceholderOf(cut));
        }

        [Fact]
        public void EmptyDescriptionUsesTableEmptyKeyLikeElementPlus()
        {
            UseEnglishEntries();

            var cut = Render<ElEmpty>();

            // EP 的 ElEmpty 就是回落到 t('el.table.emptyText')，本仓跟随
            Assert.Equal("Table empty", cut.Find(".el-empty__description p").TextContent.Trim());
        }

        [Fact]
        public void TreeEmptyTextKeepsChineseWithoutEntries()
        {
            var cut = Render<ElTree>();

            Assert.Equal("无数据", cut.Find(".el-tree__empty-text").TextContent.Trim());
        }

        [Fact]
        public void TreeEmptyTextUsesEntries()
        {
            UseEnglishEntries();

            var cut = Render<ElTree>();

            Assert.Equal("Tree empty", cut.Find(".el-tree__empty-text").TextContent.Trim());
        }

        /// <summary>
        /// 关键护栏：宿主只切语言、<b>不碰任何组件参数</b>时，深层组件也要跟着换文字。
        /// 这靠 <c>ElementComponentBase</c> 统一订阅 <c>LocaleChanged</c>；
        /// 少了它，Blazor「跳过参数没变的子组件」会让文字停在旧语言上（浏览器探针先抓到的那条缺陷）。
        /// </summary>
        [Fact]
        public async Task PlaceholderRefreshesWhenLocaleChangesWithoutParameterChange()
        {
            // 两个 locale 都要有货：ElementLang 的失败策略是"取词失败就保留旧词"，
            // 只登记 en 的话切 zh-CN 会静默留在旧词上，测不到重渲染这件事。
            var lang = TestLang.Loaded(new System.Collections.Generic.Dictionary<string, string>
            {
                { "zh-CN", ZhJson },
                { "en-US", EnJson },
            }, "zh-CN");
            Services.AddSingleton<ILang>(lang);

            var cut = Render<ElSelect<string>>();
            Assert.Equal("请选择", PlaceholderOf(cut));

            await lang.SetLangAsync("en-US");

            cut.WaitForAssertion(() => Assert.Equal("Select", PlaceholderOf(cut)), timeout: TestWait.Timeout);
        }

        /// <summary>基类订阅必须在组件释放时退订，否则切一次语言就泄漏一批组件。</summary>
        [Fact]
        public async Task SubscriptionIsRemovedWhenComponentIsDisposed()
        {
            var lang = TestLang.Loaded(EnJson, "zh-CN");
            Services.AddSingleton<ILang>(lang);

            var cut = Render<ElSelect<string>>();
            cut.Dispose();

            // Dispose 之后再来一次语言切换：不应抛异常（句柄已退订），也不应再触发渲染
            await lang.SetLangAsync("en-US");
            await lang.SetLangAsync("zh-CN");
        }
    }
}
