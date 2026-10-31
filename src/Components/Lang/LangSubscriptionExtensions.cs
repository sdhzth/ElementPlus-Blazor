using System;

namespace ElementPlus.Lang
{
    /// <summary>
    /// 组件侧接 <see cref="ILang.LocaleChanged"/> 的小工具：把「订阅 + 退订」做成一个
    /// <see cref="IDisposable"/>，组件只需在 <c>OnInitialized</c> 里存下返回值、在 <c>Dispose</c> 里 Dispose 掉，
    /// 不必各自复制加减号的配对代码（漏一半就是内存泄漏，而且只在切语言时才暴露）。
    /// <para>用法：</para>
    /// <code>
    /// protected override void OnInitialized() => langSubscription = Lang.SubscribeLocaleChanged(() => InvokeAsync(StateHasChanged));
    /// public override void Dispose() { langSubscription?.Dispose(); base.Dispose(); }
    /// </code>
    /// </summary>
    public static class LangSubscriptionExtensions
    {
        /// <summary>
        /// 订阅词条变化；<paramref name="lang"/> 为 null（DI 没接好）时返回空句柄而不是抛异常 ——
        /// 组件渲染本身不依赖词条，取不到词就该走 fallback，不该因此把页面弄崩。
        /// </summary>
        public static IDisposable SubscribeLocaleChanged(this ILang lang, Action onLocaleChanged)
        {
            if (lang == null || onLocaleChanged == null)
            {
                return NullDisposable.Instance;
            }

            lang.LocaleChanged += onLocaleChanged;
            return new Subscription(lang, onLocaleChanged);
        }

        private sealed class Subscription : IDisposable
        {
            private ILang lang;
            private Action handler;

            public Subscription(ILang lang, Action handler)
            {
                this.lang = lang;
                this.handler = handler;
            }

            public void Dispose()
            {
                var currentLang = lang;
                var currentHandler = handler;
                if (currentLang == null || currentHandler == null)
                {
                    return;
                }

                // 先清字段再退订：重复 Dispose 就是干净的 no-op
                lang = null;
                handler = null;
                currentLang.LocaleChanged -= currentHandler;
            }
        }

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new NullDisposable();

            public void Dispose()
            {
            }
        }
    }
}
