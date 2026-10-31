using System;
using System.Globalization;
using System.Text;

namespace ElementPlus.Lang
{
    /// <summary>
    /// 词条插值：把 <c>{name}</c> 占位符换成实参，语义与 EP 的 <c>useLocale</c> 里的
    /// <c>translate</c> 一致 ——
    /// <c>(get(locale, path, path)).replace(/\{(\w+)\}/g, (_, key) =&gt; option?.[key] ?? `{${key}}`)</c>。
    /// <para>
    /// 三条对齐要点：① 只认 <c>{</c> + 单词字符 + <c>}</c>（<c>{a.b}</c>、空 <c>{}</c> 一律原样留着，不当占位符）；
    /// ② <b>没给对应实参时原样保留 <c>{name}</c></b>，不吞掉、不抛异常 —— 词条里多写一个占位符
    /// 只是让读屏/界面看到 <c>{pager}</c> 这种明显线索，比悄悄少一段文字好排查；
    /// ③ 数字用 <see cref="CultureInfo.InvariantCulture"/> 转（EP 也只是 <c>String(value)</c>），
    ///    避免 <c>1234</c> 变成 <c>1,234</c> 或小数点随区域设置漂移。
    /// </para>
    /// </summary>
    public static class LocaleFormat
    {
        /// <summary>
        /// 按 <paramref name="args"/> 插值 <paramref name="text"/>。
        /// <paramref name="text"/> 里没有 <c>{</c> 时直接返回原串（不做无谓的分配）。
        /// </summary>
        public static string Interpolate(string text, params (string Name, object Value)[] args)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0)
            {
                return text;
            }

            var builder = new StringBuilder(text.Length + 16);
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c != '{')
                {
                    builder.Append(c);
                    continue;
                }

                var end = text.IndexOf('}', i + 1);
                var nameLength = end < 0 ? 0 : end - i - 1;
                if (nameLength <= 0 || !IsPlaceholderName(text, i + 1, nameLength))
                {
                    builder.Append(c);
                    continue;
                }

                var name = text.Substring(i + 1, nameLength);
                var found = false;
                object value = null;
                if (args != null)
                {
                    for (var j = 0; j < args.Length; j++)
                    {
                        if (string.Equals(args[j].Name, name, StringComparison.Ordinal))
                        {
                            value = args[j].Value;
                            found = true;
                            break;
                        }
                    }
                }

                if (found)
                {
                    builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
                }
                else
                {
                    // 与 EP 一致：找不到实参就原样留着 {name}
                    builder.Append('{').Append(name).Append('}');
                }

                i = end;
            }

            return builder.ToString();
        }

        private static bool IsPlaceholderName(string text, int start, int length)
        {
            for (var i = 0; i < length; i++)
            {
                var c = text[start + i];
                var isWordChar = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
                if (!isWordChar)
                {
                    return false;
                }
            }

            return length > 0;
        }
    }
}
