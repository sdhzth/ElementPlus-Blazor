using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElementPlus.Lang
{
    /// <summary>
    /// <see cref="ILang"/> 的默认实现。
    /// <para>
    /// 线程模型：词条字典一旦发布就不再修改，切换语言是「解析出完整新字典 → 整体替换 volatile 引用」，
    /// 所以渲染线程读到的永远是某一份完整词条，不会看到半截表，也不需要锁。
    /// </para>
    /// <para>
    /// 查找顺序：宿主自己注入的 <see cref="IConfiguration"/> 优先（那是宿主明确声明的来源），
    /// 其次才是本类从 <c>_content/Element-Plus/lang/{locale}.json</c> 载入的词条。
    /// </para>
    /// </summary>
    public class ElementLang : ILang
    {
        /// <summary>库内词条静态资源的路径前缀，相对 <c>HttpClient.BaseAddress</c>。</summary>
        public const string LangPathPrefix = "_content/Element-Plus/lang/";

        /// <summary>宿主没有指定语言时的默认值（本仓中文优先）。</summary>
        public const string DefaultLocale = "zh-CN";

        /// <summary>词条键里不允许出现点：点会被归一成分隔符 <c>:</c>（EP 写法与本仓 <c>IConfiguration</c> 写法的折中）。</summary>
        private const char KeySeparator = ':';

        private readonly Func<HttpClient, string, Task<IConfiguration>> refreshConfiguration;
        private readonly HttpClient defaultHttpClient;
        private IConfiguration configuration;
        private volatile Dictionary<string, string> entries;

        /// <summary>
        /// 兼容保留的原始构造：由宿主提供配置、当前语言、刷新委托与 HttpClient。
        /// 传了 <paramref name="refreshConfiguration"/> 时，切语言走宿主委托而不是库内默认路径。
        /// </summary>
        public ElementLang(IConfiguration configuration, string locale, Func<HttpClient, string, Task<IConfiguration>> refreshConfiguration, HttpClient httpClient)
        {
            this.configuration = configuration;
            this.CurrentLang = locale;
            this.refreshConfiguration = refreshConfiguration;
            this.defaultHttpClient = httpClient;
        }

        /// <summary>
        /// 库内 DI 用的轻量构造。<paramref name="httpClient"/> 允许为 null —— 那时本类退化为
        /// 「一切取词都走 fallback」，组件照常渲染，不会因为没有 HTTP 就崩。
        /// </summary>
        public ElementLang(HttpClient httpClient, string locale = DefaultLocale)
            : this(null, locale, null, httpClient)
        {
        }

        /// <summary>当前语言标识。</summary>
        public string CurrentLang { get; private set; }

        /// <inheritdoc />
        public bool IsLoaded => entries != null && entries.Count > 0;

        /// <inheritdoc />
        public string LastLoadError { get; private set; }

        /// <inheritdoc />
        public event Action LocaleChanged;

        /// <inheritdoc />
        public Task InitElementLangBaseAsync()
        {
            return SetLangAsync(string.IsNullOrEmpty(CurrentLang) ? DefaultLocale : CurrentLang);
        }

        /// <inheritdoc />
        public Task SetLangAsync(string lang)
        {
            return SetLangAsync(lang, defaultHttpClient);
        }

        /// <summary>
        /// 用宿主给的 HttpClient 拉词条：宿主走 <c>AddHttpClient</c>/命名客户端时（容器里取不到裸
        /// <see cref="HttpClient"/>），这是唯一的接入点。
        /// </summary>
        public async Task SetLangAsync(string lang, HttpClient httpClient)
        {
            if (!IsSafeLocale(lang))
            {
                // lang 由宿主传入并直接拼进请求 URL，必须先卡住字符集，否则 "../.." 之类会打到别的资源上
                LastLoadError = "非法语言标识：" + lang;
                return;
            }

            CurrentLang = lang;

            if (ReferenceEquals(lang, this))
            {
                // DI 里注册的 ElementLang 自己广播给自己：换宿主客户端没有意义，只按当前语言重取一次词
                await SetLangAsync(CurrentLang ?? DefaultLocale, defaultHttpClient).ConfigureAwait(false);
                return;
            }

            if (refreshConfiguration != null)
            {
                configuration = await refreshConfiguration(httpClient, lang).ConfigureAwait(false);
                LastLoadError = null;
                RaiseLocaleChanged();
                return;
            }

            if (httpClient == null)
            {
                LastLoadError = "未注册 HttpClient，词条全部走 fallback";
                return;
            }

            string json;
            try
            {
                json = await httpClient.GetStringAsync(LangPathPrefix + lang + ".json").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 404 / 断网 / 超时：保留上一次成功载入的词条。清空会让所有标签瞬间退化成 fallback，
                // 比"还是旧语言"更糟
                LastLoadError = ex.Message;
                return;
            }

            try
            {
                var parsed = Flatten(json);
                entries = parsed;
                LastLoadError = null;
                RaiseLocaleChanged();
            }
            catch (Exception ex)
            {
                LastLoadError = ex.Message;
            }
        }

        private void RaiseLocaleChanged()
        {
            try
            {
                LocaleChanged?.Invoke();
            }
            catch (Exception ex)
            {
                // 订阅者（组件）抛异常不该被误报成"词条没载入"：词条已经换好了，广播失败是另一件事
                LastLoadError = "LocaleChanged 订阅者异常：" + ex.Message;
            }
        }

        /// <inheritdoc />
        public string T(string sections)
        {
            if (string.IsNullOrEmpty(sections))
            {
                return null;
            }

            var key = NormalizeKey(sections);

            var fromConfiguration = configuration?[key];
            if (!string.IsNullOrEmpty(fromConfiguration))
            {
                return fromConfiguration;
            }

            var table = entries;
            if (table != null && table.TryGetValue(key, out var value))
            {
                return string.IsNullOrEmpty(value) ? null : value;
            }

            return null;
        }

        /// <inheritdoc />
        public string T(string key, string fallback)
        {
            var value = T(key);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        /// <inheritdoc />
        public string Text(string key, string explicitValue, string fallback)
        {
            return string.IsNullOrWhiteSpace(explicitValue) ? T(key, fallback) : explicitValue;
        }

        /// <inheritdoc />
        public string T(string key, string fallback, params (string Name, object Value)[] args)
        {
            // 插值作用在最终选中的文字上：词条命中插词条，没命中插 fallback
            return LocaleFormat.Interpolate(T(key, fallback), args);
        }

        /// <inheritdoc />
        public string Template(string key, string explicitTemplate, string fallbackTemplate, params (string Name, object Value)[] args)
        {
            var template = string.IsNullOrWhiteSpace(explicitTemplate)
                ? T(key, fallbackTemplate)
                : explicitTemplate;

            return LocaleFormat.Interpolate(template, args);
        }

        /// <summary>把嵌套 JSON 拍平成 <c>节:子节:键 → 字符串</c>。</summary>
        private static Dictionary<string, string> Flatten(string json)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            using (var doc = JsonDocument.Parse(json))
            {
                Walk(doc.RootElement, string.Empty, map);
            }
            return map;
        }

        private static void Walk(JsonElement node, string prefix, Dictionary<string, string> map)
        {
            foreach (var property in node.EnumerateObject())
            {
                var key = prefix.Length == 0 ? property.Name : prefix + KeySeparator + property.Name;

                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    Walk(property.Value, key, map);
                }
                else if (property.Value.ValueKind == JsonValueKind.String)
                {
                    map[key] = property.Value.GetString();
                }
                // 数字/布尔/数组不当文案：渲染出 "True"、"1.0" 这种无障碍标签比缺失更难查
            }
        }

        private static string NormalizeKey(string key)
        {
            return key.IndexOf('.') >= 0 ? key.Replace('.', ':') : key;
        }

        /// <summary>只放行 BCP-47 风格的字母/数字/连字符，禁掉点、斜杠、反斜杠与空白。</summary>
        private static bool IsSafeLocale(string lang)
        {
            if (string.IsNullOrEmpty(lang) || lang.Length > 35)
            {
                return false;
            }

            for (var i = 0; i < lang.Length; i++)
            {
                var c = lang[i];
                var isLetterOrDigit = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
                if (isLetterOrDigit)
                {
                    continue;
                }

                if (c != '-' || i == 0)
                {
                    return false;
                }
            }

            return lang[lang.Length - 1] != '-';
        }
    }
}
