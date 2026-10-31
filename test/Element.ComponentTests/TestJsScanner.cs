using System;
using System.Text;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 兼容「C# 源文件」与「JS 通道源码」两类守卫的注释剥离器。
    ///
    /// 为什么需要它：仓库里的兼容性守卫（ES5 语法、只用浏览器内建 API、不含第三方库名）
    /// 早期是拿**原始文本**做 <c>Assert.DoesNotContain</c> 的，于是中文注释里出现的反引号、
    /// 「对齐 Radzen 的某某」这类说明文字都会把守卫点成红，真正该看的可执行代码反而没被单独看清。
    /// 守卫必须只作用于代码。
    ///
    /// 规则（保守优先：宁可少剥，也不要把代码剥没了让守卫失去牙齿）：
    /// 1. 单/双引号字符串**原样保留**，只是它内部的 <c>//</c>、<c>/*</c> 不再被当成注释起始，
    ///    这样正向断言（例如 <c>Contains("\"inserttable\"")</c>）在剥离后的文本上依然成立。
    /// 2. 反引号本身**保留**（模板字符串正是 ES5 守卫要抓的东西），但丢掉其内部内容。
    /// 3. 行注释 <c>//…</c> 剥到行尾；块注释 <c>/*…*/</c> 整段剥掉但把换行补回，维持行号。
    /// 4. 整行的 <c>///</c> 文档注释按原有行为丢弃。
    /// </summary>
    internal static class TestJsScanner
    {
        public static string StripComments(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text ?? string.Empty;
            }

            var output = new StringBuilder(text.Length);
            int i = 0;
            int n = text.Length;
            bool atLineStart = true;

            while (i < n)
            {
                char c = text[i];

                // 行注释：剥到换行前（换行本身交给主循环保留）
                if (c == '/' && i + 1 < n && text[i + 1] == '/')
                {
                    // 整行 /// 文档注释：整行丢弃，只留换行
                    if (atLineStart && i + 2 < n && text[i + 2] == '/')
                    {
                        int eol = text.IndexOf('\n', i);
                        output.Append('\n');
                        i = eol >= 0 ? eol + 1 : n;
                        atLineStart = true;
                        continue;
                    }

                    int nl = text.IndexOf('\n', i);
                    i = nl >= 0 ? nl : n;
                    continue;
                }

                // 块注释：整段剥掉，但把内部换行补回去以维持行号
                if (c == '/' && i + 1 < n && text[i + 1] == '*')
                {
                    int close = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    int end = close >= 0 ? close + 2 : n;
                    for (int k = i; k < end; k++)
                    {
                        if (text[k] == '\n')
                        {
                            output.Append('\n');
                        }
                    }
                    i = end;
                    atLineStart = false;
                    continue;
                }

                // 单/双引号字符串：原样保留，仅用于屏蔽内部的“注释起始符”
                if (c == '"' || c == '\'')
                {
                    output.Append(c);
                    i++;
                    while (i < n)
                    {
                        char s = text[i];
                        if (s == '\\' && i + 1 < n)
                        {
                            output.Append(s).Append(text[i + 1]);
                            i += 2;
                            continue;
                        }
                        output.Append(s);
                        i++;
                        if (s == c)
                        {
                            break;
                        }
                        if (s == '\n')
                        {
                            break;   // 普通字符串不跨行，交回主循环重新判定
                        }
                    }
                    atLineStart = false;
                    continue;
                }

                // 模板字符串：保留反引号本身（守卫要抓它），丢掉内部内容
                if (c == '`')
                {
                    output.Append(c);
                    i++;
                    while (i < n)
                    {
                        if (text[i] == '\\')
                        {
                            i += 2;
                            continue;
                        }
                        if (text[i] == '`')
                        {
                            output.Append('`');
                            i++;
                            break;
                        }
                        if (text[i] == '\n')
                        {
                            output.Append('\n');
                        }
                        i++;
                    }
                    atLineStart = false;
                    continue;
                }

                output.Append(c);
                atLineStart = c == '\n';
                i++;
            }

            return output.ToString();
        }
    }
}
