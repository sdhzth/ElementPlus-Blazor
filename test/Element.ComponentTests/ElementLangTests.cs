using Bunit;
using ElementPlus;
using ElementPlus.Lang;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 库级词条运行时 <see cref="ElementLang"/> 的行为契约。
    /// 核心不变式：<b>任何取词路径都不返回空</b> —— 没载入、404、坏 JSON、宿主没给 HttpClient，
    /// 都必须回落到调用方传的 fallback，因为消费点是 aria-label 与按钮文字。
    /// </summary>
    public class ElementLangTests : BunitContext
    {
        public ElementLangTests()
        {
            // 组件渲染本来就要求宿主调过 AddElementServices（基类里 MessageBox / DialogService 等是 [Inject]）；
            // 这一行同时把 ILang 的正式注册也带上，下面各例再用 AddSingleton 覆盖成替身
            Services.AddElementServices();
        }

        /// <summary>构造 n 个 <see cref="ElCarouselItem"/>：走马灯只在多于 1 项时渲染左右箭头。</summary>
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

        private const string ZhJson = @"{
  ""el"": {
    ""inputNumber"": { ""decrease"": ""减少数值"", ""increase"": ""增加数值"" },
    ""carousel"": { ""leftArrow"": ""上一张"", ""rightArrow"": ""下一张"" },
    ""blank"": ""   ""
  },
  ""count"": 3,
  ""flag"": true
}";

        private const string EnJson = @"{
  ""el"": {
    ""inputNumber"": { ""decrease"": ""decrease number"", ""increase"": ""increase number"" }
  }
}";

        /// <summary>按 URL 尾巴返回预设响应，同时记下请求顺序，便于断言"根本没发请求"。</summary>
        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Dictionary<string, (HttpStatusCode Code, string Body)> table;
            private readonly string defaultBody;

            public List<string> Requests { get; } = new List<string>();

            public StubHandler(string defaultBody, Dictionary<string, (HttpStatusCode, string)> table = null)
            {
                this.defaultBody = defaultBody;
                this.table = table ?? new Dictionary<string, (HttpStatusCode, string)>(StringComparer.Ordinal);
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var url = request.RequestUri.ToString();
                Requests.Add(url);

                foreach (var pair in table)
                {
                    if (url.EndsWith(pair.Key, StringComparison.Ordinal))
                    {
                        return Task.FromResult(new HttpResponseMessage(pair.Value.Code)
                        {
                            Content = new StringContent(pair.Value.Body),
                        });
                    }
                }

                if (string.IsNullOrEmpty(defaultBody))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent("not found"),
                    });
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(defaultBody),
                });
            }
        }

        private static ElementLang WithBody(string body, out StubHandler handler, Dictionary<string, (HttpStatusCode, string)> table = null)
        {
            handler = new StubHandler(body, table);
            var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            return new ElementLang(http);
        }

        [Fact]
        public async Task InitElementLangBaseAsyncLoadsDefaultLocale()
        {
            var lang = WithBody(ZhJson, out var handler);

            await lang.InitElementLangBaseAsync();

            // 默认语言 zh-CN，并且打到库自身的静态资源路径（_content 基路径 = _content/{PackageId}，2026-10-18 包改名后随之变为 Element-Plus）
            Assert.Single(handler.Requests);
            Assert.EndsWith("_content/Element-Plus/lang/zh-CN.json", handler.Requests[0]);
            Assert.True(lang.IsLoaded);
            Assert.Null(lang.LastLoadError);
        }

        [Fact]
        public async Task KeysAcceptBothEpDotAndConfigurationColonForms()
        {
            var lang = WithBody(ZhJson, out _);
            await lang.SetLangAsync("zh-CN");

            Assert.Equal("减少数值", lang.T("el.inputNumber.decrease"));
            Assert.Equal("减少数值", lang.T("el:inputNumber:decrease"));
            Assert.Equal("减少数值", lang.T("el.inputNumber.decrease", "fb"));
        }

        [Fact]
        public void WithoutAnyLoadEveryLookupFallsBack()
        {
            var lang = new ElementLang((HttpClient)null);

            Assert.False(lang.IsLoaded);
            Assert.Null(lang.T("el.inputNumber.decrease"));
            Assert.Equal("decrease", lang.T("el.inputNumber.decrease", "decrease"));
        }

        [Fact]
        public async Task MissingKeyAndBlankValueBothFallBack()
        {
            var lang = WithBody(ZhJson, out _);
            await lang.SetLangAsync("zh-CN");

            Assert.Equal("fb", lang.T("el.inputNumber.nope", "fb"));
            // 空白值等同于缺失：aria-label 渲染成 "   " 比渲染成英文更糟
            Assert.Equal("fb", lang.T("el.blank", "fb"));
        }

        [Fact]
        public async Task NumbersAndBooleansAreNotTreatedAsText()
        {
            var lang = WithBody(ZhJson, out _);
            await lang.SetLangAsync("zh-CN");

            Assert.Equal("fb", lang.T("count", "fb"));
            Assert.Equal("fb", lang.T("flag", "fb"));
        }

        [Fact]
        public async Task SwitchingLanguageReplacesWholeTableInsteadOfMerging()
        {
            var lang = WithBody(ZhJson, out _, new Dictionary<string, (HttpStatusCode, string)>
            {
                { "en-US.json", (HttpStatusCode.OK, EnJson) },
            });

            await lang.SetLangAsync("zh-CN");
            Assert.Equal("减少数值", lang.T("el.inputNumber.decrease", "fb"));

            await lang.SetLangAsync("en-US");

            Assert.Equal("decrease number", lang.T("el.inputNumber.decrease", "fb"));
            // zh 独有的词条不能残留，否则切语言会切出一张混合表
            Assert.Equal("fb", lang.T("el.carousel.leftArrow", "fb"));
            Assert.Equal("en-US", lang.CurrentLang);
        }

        [Fact]
        public async Task FailedLoadKeepsPreviousEntriesAndRecordsError()
        {
            var lang = WithBody(ZhJson, out _, new Dictionary<string, (HttpStatusCode, string)>
            {
                { "fr-FR.json", (HttpStatusCode.NotFound, "{}") },
            });

            await lang.SetLangAsync("zh-CN");
            await lang.SetLangAsync("fr-FR");

            Assert.Equal("减少数值", lang.T("el.inputNumber.decrease", "fb"));
            Assert.False(string.IsNullOrEmpty(lang.LastLoadError));
            Assert.Equal("fr-FR", lang.CurrentLang);
        }

        [Fact]
        public async Task BrokenJsonKeepsPreviousEntries()
        {
            var lang = WithBody(ZhJson, out _, new Dictionary<string, (HttpStatusCode, string)>
            {
                { "de-DE.json", (HttpStatusCode.OK, "{ this is not json") },
            });

            await lang.SetLangAsync("zh-CN");
            await lang.SetLangAsync("de-DE");

            Assert.Equal("增加数值", lang.T("el.inputNumber.increase", "fb"));
            Assert.False(string.IsNullOrEmpty(lang.LastLoadError));
        }

        [Fact]
        public async Task UnsafeLocaleIsRejectedWithoutAnyRequest()
        {
            var lang = WithBody(ZhJson, out var handler);

            await lang.SetLangAsync("../../appsettings");

            Assert.Empty(handler.Requests);
            Assert.False(string.IsNullOrEmpty(lang.LastLoadError));
            Assert.False(lang.IsLoaded);
        }

        [Fact]
        public async Task MissingHttpClientDegradesInsteadOfThrowing()
        {
            var lang = new ElementLang((HttpClient)null);

            await lang.SetLangAsync("en-US");
            await lang.InitElementLangBaseAsync();

            Assert.False(lang.IsLoaded);
            Assert.Equal("decrease", lang.T("el.inputNumber.decrease", "decrease"));
        }

        [Fact]
        public void LegacyFourArgConstructorStillReadsHostConfiguration()
        {
            // 原始 ctor（宿主自带 IConfiguration）必须继续可用，且宿主配置优先于库内词条
            var config = new FakeConfiguration(new Dictionary<string, string>
            {
                { "el:inputNumber:decrease", "来自宿主配置" },
                { "Message", "你好,世界" },
            });

            var lang = new ElementLang(config, "zh-CN", null, null);

            Assert.Equal("来自宿主配置", lang.T("el.inputNumber.decrease", "fb"));
            Assert.Equal("你好,世界", lang.T("Message", "fb"));
            Assert.Null(lang.T("nothing.here"));
        }

        [Fact]
        public void AddElementServicesRegistersLangAndSurvivesWithoutHttpClient()
        {
            var services = new ServiceCollection().AddElementServices();

            using (var provider = services.BuildServiceProvider())
            using (var scope = provider.CreateScope())
            {
                var lang = scope.ServiceProvider.GetRequiredService<ILang>();

                // 这里没有注册 HttpClient：解析必须成功（组件的 @inject 不能因为宿主少一行就崩）
                Assert.NotNull(lang);
                Assert.Equal(ElementLang.DefaultLocale, lang.CurrentLang);
                Assert.False(lang.IsLoaded);
            }
        }

        [Fact]
        public async Task ComponentsPickUpLoadedEntries()
        {
            var lang = WithBody(ZhJson, out _);
            await lang.SetLangAsync("zh-CN");
            Services.AddSingleton<ILang>(lang);

            var cut = Render<ElInputNumber>(parameters => parameters.Add(x => x.Controls, true).Add(x => x.Value, 5m));

            Assert.Equal("减少数值", cut.Find(".el-input-number__decrease").GetAttribute("aria-label"));
            Assert.Equal("增加数值", cut.Find(".el-input-number__increase").GetAttribute("aria-label"));
        }

        [Fact]
        public async Task ExplicitParameterWinsOverLoadedEntries()
        {
            var lang = WithBody(ZhJson, out _);
            await lang.SetLangAsync("zh-CN");
            Services.AddSingleton<ILang>(lang);

            var cut = Render<ElInputNumber>(parameters => parameters
                .Add(x => x.Controls, true)
                .Add(x => x.Value, 5m)
                .Add(x => x.DecreaseAriaLabel, "自定义减"));

            Assert.Equal("自定义减", cut.Find(".el-input-number__decrease").GetAttribute("aria-label"));
            // 只覆盖一个，另一个继续走词条
            Assert.Equal("增加数值", cut.Find(".el-input-number__increase").GetAttribute("aria-label"));
        }

        [Fact]
        public void CarouselArrowsKeepExistingChineseWhenLangNotLoaded()
        {
            // 没载入词条时就是改动前硬编码的那两个字，DOM 零变化
            var cut = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.ChildContent, Items(3)));

            Assert.Equal(2, cut.FindAll(".el-carousel__arrow").Count);
            Assert.Equal("上一张", cut.Find(".el-carousel__arrow--left").GetAttribute("aria-label"));
            Assert.Equal("下一张", cut.Find(".el-carousel__arrow--right").GetAttribute("aria-label"));
        }

        [Fact]
        public async Task CarouselArrowsUseLoadedEntries()
        {
            var lang = WithBody(CarouselEnJson, out _);
            await lang.SetLangAsync("en-US");
            Services.AddSingleton<ILang>(lang);

            var cut = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.ChildContent, Items(3)));

            Assert.Equal("Carousel arrow left", cut.Find(".el-carousel__arrow--left").GetAttribute("aria-label"));
            Assert.Equal("Carousel arrow right", cut.Find(".el-carousel__arrow--right").GetAttribute("aria-label"));
        }

        private const string CarouselEnJson = @"{
  ""el"": { ""carousel"": { ""leftArrow"": ""Carousel arrow left"", ""rightArrow"": ""Carousel arrow right"" } }
}";

        /// <summary>
        /// 护栏：宿主只改服务状态、不碰任何组件参数时，组件必须因为订阅 LocaleChanged 自己重渲染。
        /// Blazor 会跳过「参数没变」的子组件，所以少了订阅，深层标签会永远停在旧语言上 ——
        /// 这一条正是浏览器探针先抓到、再回补成测试的。
        /// </summary>
        [Fact]
        public async Task InputNumberLabelsRefreshWhenLocaleChangesWithoutParameterChange()
        {
            var lang = WithBody(ZhJson, out _, new Dictionary<string, (HttpStatusCode, string)>
            {
                { "en-US.json", (HttpStatusCode.OK, EnJson) },
            });
            await lang.SetLangAsync("zh-CN");
            Services.AddSingleton<ILang>(lang);

            var cut = Render<ElInputNumber>(p => p.Add(x => x.Controls, true).Add(x => x.Value, 5m));
            Assert.Equal("减少数值", cut.Find(".el-input-number__decrease").GetAttribute("aria-label"));

            await lang.SetLangAsync("en-US");

            cut.WaitForAssertion(
                () => Assert.Equal("decrease number", cut.Find(".el-input-number__decrease").GetAttribute("aria-label")),
                timeout: TestWait.Timeout);
            Assert.Equal("increase number", cut.Find(".el-input-number__increase").GetAttribute("aria-label"));
        }

        [Fact]
        public async Task CarouselArrowsRefreshWhenLocaleChanges()
        {
            var lang = WithBody(CarouselEnJson, out _, new Dictionary<string, (HttpStatusCode, string)>
            {
                { "zh-CN.json", (HttpStatusCode.OK, CarouselZhJson) },
            });
            await lang.SetLangAsync("zh-CN");
            Services.AddSingleton<ILang>(lang);

            var cut = Render<ElCarousel>(p => p
                .Add(x => x.Autoplay, false)
                .Add(x => x.ChildContent, Items(3)));
            Assert.Equal("上一张", cut.Find(".el-carousel__arrow--left").GetAttribute("aria-label"));

            await lang.SetLangAsync("en-US");

            cut.WaitForAssertion(
                () => Assert.Equal("Carousel arrow left", cut.Find(".el-carousel__arrow--left").GetAttribute("aria-label")),
                timeout: TestWait.Timeout);
        }

        [Fact]
        public async Task SubscriptionUnsubscribesOnceAndSurvivesDoubleDispose()
        {
            var lang = WithBody(ZhJson, out _, new Dictionary<string, (HttpStatusCode, string)>
            {
                { "en-US.json", (HttpStatusCode.OK, EnJson) },
            });

            var hits = 0;
            Action handler = () => hits++;
            var subscription = lang.SubscribeLocaleChanged(handler);

            await lang.SetLangAsync("zh-CN");
            Assert.Equal(1, hits);

            // 重复 Dispose 必须是 no-op：漏退订或退两次都是这类句柄的经典缺陷
            subscription.Dispose();
            subscription.Dispose();

            await lang.SetLangAsync("en-US");
            Assert.Equal(1, hits);
        }

        [Fact]
        public void SubscriptionOnMissingLangIsANoOpInsteadOfAnException()
        {
            var hits = 0;
            var subscription = ((ILang)null).SubscribeLocaleChanged(() => hits++);

            Assert.NotNull(subscription);
            subscription.Dispose();
            Assert.Equal(0, hits);
        }

        private const string CarouselZhJson = @"{
  ""el"": { ""carousel"": { ""leftArrow"": ""上一张"", ""rightArrow"": ""下一张"" } }
}";

        /// <summary>最小 IConfiguration 替身：只实现取索引器，其余成员用不上。</summary>
        private sealed class FakeConfiguration : IConfiguration
        {
            private readonly Dictionary<string, string> map;

            public FakeConfiguration(Dictionary<string, string> map)
            {
                this.map = map;
            }

            public string this[string key]
            {
                get => map.TryGetValue(key, out var value) ? value : null;
                set => throw new NotSupportedException();
            }

            public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();

            public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => new NeverChangeToken();

            public IConfigurationSection GetSection(string key) => throw new NotSupportedException();

            public string GetConnectionString(string name) => null;

            private sealed class NeverChangeToken : Microsoft.Extensions.Primitives.IChangeToken
            {
                public bool ActiveChangeCallbacks => false;

                public bool HasChanged => false;

                public IDisposable RegisterChangeCallback(Action<object> callback, object state) => new Noop();

                private sealed class Noop : IDisposable
                {
                    public void Dispose()
                    {
                    }
                }
            }
        }
    }
}
