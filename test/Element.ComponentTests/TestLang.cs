using ElementPlus.Lang;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 测试用的词条替身：造一个「已成功载入指定 JSON」的 <see cref="ElementLang"/>，
    /// 让组件走真实的取词路径（而不是 mock 接口），这样键名写错、值取错都照样能在断言里暴露。
    /// </summary>
    internal static class TestLang
    {
        /// <summary>按 <c>{locale}.json</c> 返回对应 JSON；没登记的 locale 一律 404。</summary>
        internal static ElementLang Loaded(IDictionary<string, string> jsonByLocale, string initialLocale = "zh-CN")
        {
            var handler = new TableHandler(jsonByLocale);
            var http = new HttpClient(handler) { BaseAddress = new System.Uri("http://localhost/") };
            var lang = new ElementLang(http, initialLocale);
            lang.SetLangAsync(initialLocale).GetAwaiter().GetResult();
            return lang;
        }

        internal static ElementLang Loaded(string json, string locale = "zh-CN")
        {
            return Loaded(new Dictionary<string, string> { { locale, json } }, locale);
        }

        private sealed class TableHandler : HttpMessageHandler
        {
            private readonly IDictionary<string, string> jsonByLocale;

            public TableHandler(IDictionary<string, string> jsonByLocale)
            {
                this.jsonByLocale = jsonByLocale;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var url = request.RequestUri.ToString();
                foreach (var pair in jsonByLocale)
                {
                    if (url.EndsWith("/" + pair.Key + ".json", System.StringComparison.Ordinal))
                    {
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(pair.Value),
                        });
                    }
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("{}"),
                });
            }
        }
    }
}
