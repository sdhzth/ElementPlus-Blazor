using ElementPlus.Lang;
using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>日期 / 时间选择器内置文案的标识。</summary>
    public enum PickerText
    {
        /// <summary>确认按钮</summary>
        Confirm,

        /// <summary>取消按钮</summary>
        Cancel,

        /// <summary>此刻 / 现在</summary>
        Now,

        /// <summary>选择日期（日期输入框占位符）</summary>
        SelectDate,

        /// <summary>选择时间（时间输入框占位符）</summary>
        SelectTime,

        /// <summary>时间标签</summary>
        Time,

        /// <summary>开始标签</summary>
        Start,

        /// <summary>结束标签</summary>
        End
    }

    /// <summary>
    /// 日期 / 时间选择器的内置文案（多语言，**默认简体中文**）。
    /// <para>
    /// 语言来源：<see cref="ElConfigProvider.Locale"/> → <see cref="ElementConfig.Locale"/>；
    /// 组件上的同名参数（如 <c>ConfirmText</c>）优先于本表。
    /// </para>
    /// <para>
    /// 语言匹配规则：<c>en*</c> → 英文；<c>zh-Hant</c> / <c>zh-TW</c> / <c>zh-HK</c> / <c>zh-MO</c> → 繁体；
    /// 其余（含空值、未识别语言）一律回退到**简体中文**，符合「默认中文」的约定。
    /// </para>
    /// </summary>
    public static class PickerLocale
    {
        private const string Simplified = "zh-cn";
        private const string Traditional = "zh-tw";
        private const string English = "en";

        private static readonly Dictionary<string, Dictionary<PickerText, string>> Table =
            new Dictionary<string, Dictionary<PickerText, string>>
            {
                [Simplified] = new Dictionary<PickerText, string>
                {
                    [PickerText.Confirm] = "确认",
                    [PickerText.Cancel] = "取消",
                    [PickerText.Now] = "此刻",
                    [PickerText.SelectDate] = "选择日期",
                    [PickerText.SelectTime] = "选择时间",
                    [PickerText.Time] = "时间",
                    [PickerText.Start] = "开始",
                    [PickerText.End] = "结束"
                },
                [Traditional] = new Dictionary<PickerText, string>
                {
                    [PickerText.Confirm] = "確認",
                    [PickerText.Cancel] = "取消",
                    [PickerText.Now] = "此刻",
                    [PickerText.SelectDate] = "選擇日期",
                    [PickerText.SelectTime] = "選擇時間",
                    [PickerText.Time] = "時間",
                    [PickerText.Start] = "開始",
                    [PickerText.End] = "結束"
                },
                [English] = new Dictionary<PickerText, string>
                {
                    [PickerText.Confirm] = "OK",
                    [PickerText.Cancel] = "Cancel",
                    [PickerText.Now] = "Now",
                    [PickerText.SelectDate] = "Select date",
                    [PickerText.SelectTime] = "Select time",
                    [PickerText.Time] = "Time",
                    [PickerText.Start] = "Start",
                    [PickerText.End] = "End"
                }
            };

        /// <summary>按语言取文案；未识别的语言回退简体中文。</summary>
        public static string Resolve(string locale, PickerText key)
        {
            var language = Normalize(locale);
            return Table[language][key];
        }

        /// <summary>组件参数优先：参数为空时回退到语言表。</summary>
        public static string Resolve(string locale, PickerText key, string overridden)
        {
            return string.IsNullOrWhiteSpace(overridden) ? Resolve(locale, key) : overridden;
        }

        /// <summary>带词条的版本：<b>组件参数 &gt; <c>el.datepicker.*</c> 词条 &gt; 内置表</b>。
        /// <para>
        /// 没有对应 EP 键的枚举项（<see cref="PickerText.Time"/> / <see cref="PickerText.Start"/> /
        /// <see cref="PickerText.End"/> 等）直接走表。这里刻意不提供「只有 lang、没有 overridden」的三参重载：
        /// 它和 <c>(locale, key, string)</c> 在传 null 时互相歧义。
        /// </para>
        /// </summary>
        public static string Resolve(string locale, PickerText key, string overridden, ILang lang)
        {
            if (!string.IsNullOrWhiteSpace(overridden))
            {
                return overridden;
            }

            var entryKey = EntryKey(key);
            return entryKey == null
                ? Resolve(locale, key)
                : Entry(locale, lang, entryKey, Resolve(locale, key));
        }

        /// <summary>
        /// <see cref="PickerText"/> 在 EP 词条里的对应键。
        /// <para>
        /// 只映射本仓真有落点的两项。<see cref="PickerText.Now"/> / <see cref="PickerText.SelectDate"/> /
        /// <see cref="PickerText.SelectTime"/> 虽然在表里备了值，但面板上没有对应元素（本仓的占位符是
        /// 「请选择日期」这类合成词，EP 的 <c>el.datepicker.now</c> 挂在快捷此刻按钮上），无消费者就不建键。
        /// <see cref="PickerText.Time"/> / <see cref="PickerText.Start"/> / <see cref="PickerText.End"/>
        /// 同理：EP 用的是 <c>startTime</c> / <c>endDate</c> 这类合成词，本仓面板元素形态不同，
        /// 给它们造键就是伪装 EP 契约。
        /// </para>
        /// </summary>
        private static string EntryKey(PickerText key)
        {
            switch (key)
            {
                case PickerText.Confirm: return "el.datepicker.confirm";
                case PickerText.Cancel: return "el.datepicker.cancel";
                default: return null;
            }
        }

        // ====================================================================
        // 词条优先（el.datepicker.*）
        // --------------------------------------------------------------------
        // 本仓有两条语言开关：ElConfigProvider.Locale 决定 picker 说哪套语言，
        // ILang.CurrentLang 决定载入的是哪一份词条文件。两者不一致时必须让 Locale 赢，
        // 否则等于把别的语言的文案漏进宿主明确指定了语言的组件里 —— 所以取词前先比对语言档。
        //
        // 键名一律写成完整字面量数组：LangResourceGuardTests 的正则只认完整字面量键，
        // "el.datepicker.weeks." + name 这种拼接形态会被当成一个不存在的键报违规。
        // ====================================================================

        private static readonly string[] WeekEntryKeys =
        {
            "el.datepicker.weeks.sun", "el.datepicker.weeks.mon", "el.datepicker.weeks.tue",
            "el.datepicker.weeks.wed", "el.datepicker.weeks.thu", "el.datepicker.weeks.fri", "el.datepicker.weeks.sat"
        };

        private static readonly string[] WeekFullEntryKeys =
        {
            "el.datepicker.weeksFull.sun", "el.datepicker.weeksFull.mon", "el.datepicker.weeksFull.tue",
            "el.datepicker.weeksFull.wed", "el.datepicker.weeksFull.thu", "el.datepicker.weeksFull.fri", "el.datepicker.weeksFull.sat"
        };

        private static readonly string[] MonthNameEntryKeys =
        {
            "el.datepicker.months.jan", "el.datepicker.months.feb", "el.datepicker.months.mar",
            "el.datepicker.months.apr", "el.datepicker.months.may", "el.datepicker.months.jun",
            "el.datepicker.months.jul", "el.datepicker.months.aug", "el.datepicker.months.sep",
            "el.datepicker.months.oct", "el.datepicker.months.nov", "el.datepicker.months.dec"
        };

        private static readonly string[] MonthNumberEntryKeys =
        {
            "el.datepicker.month1", "el.datepicker.month2", "el.datepicker.month3", "el.datepicker.month4",
            "el.datepicker.month5", "el.datepicker.month6", "el.datepicker.month7", "el.datepicker.month8",
            "el.datepicker.month9", "el.datepicker.month10", "el.datepicker.month11", "el.datepicker.month12"
        };

        /// <summary>
        /// 取 <c>el.*</c> 词条，回落到内置表的值。<paramref name="lang"/> 为 null 或语言档不一致时直接回表。
        /// 末位参数是 <c>{name}</c> 插值实参（EP 的 <c>el.datepicker.year</c> 在本仓存成 <c>{year} 年</c> 模板，
        /// 因为 EP 的英文值是空串，而本仓守卫不允许空白词条）。
        /// </summary>
        public static string Entry(string locale, ILang lang, string key, string tableValue, params (string Name, object Value)[] args)
        {
            var template = lang != null && Normalize(lang.CurrentLang) == Normalize(locale)
                ? lang.T(key, tableValue)
                : tableValue;

            return args == null || args.Length == 0
                ? template
                : LocaleFormat.Interpolate(template, args);
        }

        /// <summary>把任意 BCP-47 语言标记归一到本表支持的三档之一（默认简体中文）。</summary>
        public static string Normalize(string locale)
        {
            var value = (locale ?? string.Empty).Trim().Replace('_', '-').ToLowerInvariant();
            if (value.Length == 0)
            {
                return Simplified;
            }

            if (value.StartsWith("en", System.StringComparison.Ordinal))
            {
                return English;
            }

            if (value.StartsWith("zh", System.StringComparison.Ordinal))
            {
                var traditional = value.Contains("hant", System.StringComparison.Ordinal)
                    || value.EndsWith("-tw", System.StringComparison.Ordinal)
                    || value.EndsWith("-hk", System.StringComparison.Ordinal)
                    || value.EndsWith("-mo", System.StringComparison.Ordinal);
                return traditional ? Traditional : Simplified;
            }

            // 未识别的语言：默认中文
            return Simplified;
        }

        // ====================================================================
        // 日历（日期面板）文案：周标题 / 月名 / 年月标签
        // --------------------------------------------------------------------
        // 说明：日期面板的表头文案随语言切换，与上面的 PickerText 共用同一套归一规则
        // （en* → 英文；zh-Hant / zh-TW / zh-HK / zh-MO → 繁体；其余 → 简体中文）。
        // 星期固定以**周日**为首列，与 EP 一致。
        // ====================================================================

        private static readonly Dictionary<string, string[]> WeekHeaderTable =
            new Dictionary<string, string[]>
            {
                [Simplified] = new[] { "日", "一", "二", "三", "四", "五", "六" },
                [Traditional] = new[] { "日", "一", "二", "三", "四", "五", "六" },
                [English] = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" }
            };

        private static readonly Dictionary<string, string[]> MonthNameTable =
            new Dictionary<string, string[]>
            {
                [Simplified] = new[] { "一月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "十一月", "十二月" },
                [Traditional] = new[] { "一月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "十一月", "十二月" },
                [English] = new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" }
            };

        /// <summary>星期表头（周日开头，7 项）。给了 <paramref name="lang"/> 时先取 <c>el.datepicker.weeks.*</c>。</summary>
        public static string[] WeekHeaders(string locale, ILang lang = null)
        {
            var table = WeekHeaderTable[Normalize(locale)];
            if (lang == null || Normalize(lang.CurrentLang) != Normalize(locale))
            {
                return table;
            }

            var entries = new string[WeekEntryKeys.Length];
            for (var i = 0; i < entries.Length; i++)
            {
                entries[i] = lang.T(WeekEntryKeys[i], table[i]);
            }

            return entries;
        }

        /// <summary>
        /// 星期表头的完整名称（EP 挂在 <c>&lt;th&gt;</c> 的 <c>aria-label</c> 上，可见文字用 <see cref="WeekHeaders"/> 的短名）。
        /// 本仓内置表没有完整名，所以没有词条时返回 null（该属性不渲染）。
        /// </summary>
        public static string WeekHeaderFullName(string locale, ILang lang, int index)
        {
            if (lang == null || Normalize(lang.CurrentLang) != Normalize(locale)
                || index < 0 || index >= WeekFullEntryKeys.Length)
            {
                return null;
            }

            return lang.T(WeekFullEntryKeys[index], null);
        }

        /// <summary>月份名（1..12），对应 EP 的 <c>el.datepicker.months.*</c>。</summary>
        public static string MonthName(string locale, int month, ILang lang = null)
        {
            var index = System.Math.Min(System.Math.Max(month, 1), 12) - 1;
            return Entry(locale, lang, MonthNameEntryKeys[index], MonthNameTable[Normalize(locale)][index]);
        }

        /// <summary>
        /// 年份标签：中文 <c>2026 年</c>，英文 <c>2026</c>；词条形态是 <c>{year}</c> 模板
        /// （EP 的 <c>el.datepicker.year</c> 中文是后缀「年」、英文是空串，本仓守卫不允许空白词条，所以统一存成模板）。
        /// <paramref name="formatProvider"/> 只影响兜底值的数字写法（调用方的 <c>CultureInfo</c> 由它传入）。
        /// </summary>
        public static string FormatYear(string locale, int year, ILang lang = null, System.IFormatProvider formatProvider = null)
        {
            var yearText = year.ToString(formatProvider ?? System.Globalization.CultureInfo.InvariantCulture);
            var tableValue = Normalize(locale) == English ? yearText : yearText + " 年";

            return Entry(locale, lang, "el.datepicker.year", tableValue, ("year", year));
        }

        /// <summary>
        /// 只取 <c>el.datepicker.monthN</c> 词条，没命中就返回 null（<b>不</b>回落内置表）。
        /// <para>月格的 <c>aria-label</c> 是这次新加的属性，用 null 兜底才能保证「没词条＝零 DOM 变化」。</para>
        /// </summary>
        public static string MonthNumberEntry(string locale, int month, ILang lang)
        {
            var index = System.Math.Min(System.Math.Max(month, 1), 12) - 1;
            return Entry(locale, lang, MonthNumberEntryKeys[index], null);
        }

        /// <summary>月份标签：中文 <c>9 月</c>，英文 <c>September</c>，对应 EP 的 <c>el.datepicker.month{n}</c>。</summary>
        public static string FormatMonth(string locale, int month, ILang lang = null)
        {
            var index = System.Math.Min(System.Math.Max(month, 1), 12) - 1;
            return Entry(
                locale,
                lang,
                MonthNumberEntryKeys[index],
                Normalize(locale) == English ? MonthNameTable[Normalize(locale)][index] : month + " 月");
        }

        /// <summary>
        /// 范围面板的「年月」合并标签：中文 <c>2026 年 9 月</c>，英文 <c>2026 September</c>。
        /// <para>对齐 EP 的 <c>el-date-range-picker__header</c> —— 范围面板每个面板一个合并标签，
        /// 而不是单面板那样把「年」「月」拆成两个可点击块。</para>
        /// </summary>
        public static string FormatYearMonth(string locale, int year, int month, ILang lang = null)
        {
            return FormatYear(locale, year, lang) + " " + FormatMonth(locale, month, lang);
        }
    }
}
