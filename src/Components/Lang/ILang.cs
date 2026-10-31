using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ElementPlus.Lang
{
    /// <summary>
    /// 库级词条（i18n）读取入口：由 <c>AddElementServices()</c> 注册，组件侧用
    /// <c>@inject ILang Lang</c> 取用。词条来自库自带的静态资源
    /// <c>_content/Element-Plus/lang/{locale}.json</c>。
    /// <para>
    /// 一条铁律贯穿整个接口：<b>取不到文案时必须回落到调用方给的 fallback，绝不把空交给渲染</b>。
    /// 词条文件可能 404、宿主可能压根没注册 HttpClient、SSR 预渲染阶段可能什么都还没载入 ——
    /// 这些情况下无障碍标签与按钮文字仍必须有内容，否则「接入 i18n」本身就成了新的缺陷来源。
    /// </para>
    /// </summary>
    public interface ILang
    {
        /// <summary>当前语言标识，如 <c>zh-CN</c>。</summary>
        string CurrentLang { get; }

        /// <summary>库内 JSON 词条是否已成功载入；false 时 <c>T(key, fallback)</c> 全部走 fallback。</summary>
        bool IsLoaded { get; }

        /// <summary>最近一次载入失败的原因，成功时为 null。仅供宿主排障，不参与渲染判断。</summary>
        string LastLoadError { get; }

        /// <summary>
        /// 词条发生变化（成功切换语言并重取词条）后触发。
        /// <para>
        /// 为什么需要它：词条是应用级的可变状态，但 Blazor 在父组件重渲染时，
        /// <b>参数没变的子组件会被跳过</b>。所以只靠宿主 <c>StateHasChanged()</c> 是刷不到
        /// 深层组件的无障碍标签与按钮文字的 —— 必须由取词的组件自己订阅这个事件。
        /// </para>
        /// </summary>
        event Action LocaleChanged;

        /// <summary>
        /// 载入 <see cref="CurrentLang"/>（为空时用 <see cref="ElementLang.DefaultLocale"/>）对应的词条。
        /// 没有 HttpClient 时静默跳过并记 <see cref="LastLoadError"/>，不抛异常。
        /// <para>签名修正说明：本方法原先是 <c>void</c>，无法 await，调用方永远看不到加载结果；
        /// 而该接口在本次改动前既无实现方也无消费方，因此把 <c>void</c> 改成 <c>Task</c> 是安全的必要修正。</para>
        /// </summary>
        Task InitElementLangBaseAsync();

        /// <summary>切换语言并拉取该语言词条；载入失败时保留上一次成功载入的词条，不清空。</summary>
        Task SetLangAsync(string lang);

        /// <summary>
        /// 用宿主指定的 HttpClient 切语言：宿主走 <c>AddHttpClient</c>/命名客户端、
        /// 容器里取不到裸 <see cref="HttpClient"/> 时，这是唯一的接入点。
        /// </summary>
        Task SetLangAsync(string lang, HttpClient httpClient);

        /// <summary>
        /// 按节路径取词条（<c>"el:inputNumber:decrease"</c>）；取不到返回 <c>null</c>。
        /// 旧签名与旧语义保持不变（原先就是 <c>configuration?[sections]</c>）。
        /// </summary>
        string T(string sections);

        /// <summary>
        /// 取词条并保证非空。key 允许 EP 写法 <c>el.inputNumber.decrease</c> 与
        /// IConfiguration 写法 <c>el:inputNumber:decrease</c>；缺失或空白时返回 <paramref name="fallback"/>。
        /// </summary>
        string T(string key, string fallback);

        /// <summary>
        /// 组件里最常见的取词形态：<b>显式参数 &gt; 词条 &gt; 改动前那个字面量</b>，
        /// 与 EP 的 <c>props.loadingText || t('el.select.loading')</c> 同形。
        /// <para>
        /// 用法：<c>Lang.Text("el.select.noData", NoDataText, "无数据")</c>。
        /// 要把这套优先级跑通，组件参数的默认值必须是 <c>null</c>（表示"宿主没指定"）而不是中文常量 ——
        /// 渲染出的文字在没接词条时与改动前完全一致。
        /// </para>
        /// </summary>
        /// <param name="key">词条键，如 <c>el.select.noData</c>。</param>
        /// <param name="explicitValue">宿主显式设置的参数值；非空即优先采用。</param>
        /// <param name="fallback">接入词条之前就有的那个字面量，保证不取词时 DOM 一字不变。</param>
        string Text(string key, string explicitValue, string fallback);

        /// <summary>
        /// 取词条并插值 <c>{name}</c> 占位符（语义与 EP 的 <c>translate</c> 一致：
        /// 没给对应实参的占位符<b>原样保留</b>，不吞不抛）。插值作用在<b>最终取到的那段文字</b>上
        /// （词条命中就插词条，没命中就插 fallback）。
        /// </summary>
        string T(string key, string fallback, params (string Name, object Value)[] args);

        /// <summary>
        /// 模板型参数的三级取值：<b>宿主模板 &gt; 词条模板 &gt; 改动前模板</b>，然后对<b>选中的那个模板</b>插值。
        /// <para>
        /// 与 <see cref="Text"/> 的区别就在于此：像 <c>TotalText="共 {total} 条"</c> 这种参数，宿主给的是
        /// <em>模板</em>而不是字面文字，必须照样插值；而 <c>ConfirmButtonText</c> 这类给的是字面文字，
        /// 就不该再动它。用错会让宿主自定义的模板里 <c>{total}</c> 原样显示出来。
        /// </para>
        /// </summary>
        string Template(string key, string explicitTemplate, string fallbackTemplate, params (string Name, object Value)[] args);
    }
}
