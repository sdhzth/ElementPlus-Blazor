using Microsoft.AspNetCore.Components;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ElementPlus
{
    public partial class ElStatistic : ElementComponentBase
    {
        #region 基础

        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public RenderFragment TitleContent { get; set; }

        [Parameter]
        public double Value { get; set; }

        [Parameter]
        public string ValueStyle { get; set; }

        [Parameter]
        public CultureInfo Culture { get; set; }

        #endregion

        #region 格式化

        /// <summary>精度（小数位数）</summary>
        [Parameter]
        public int Precision { get; set; }

        /// <summary>格式化字符串（如 "{0:C2}"）</summary>
        [Parameter]
        public string Format { get; set; }

        /// <summary>自定义格式化函数（优先于 Format/Precision）</summary>
        [Parameter]
        public Func<double, string> Formatter { get; set; }

        /// <summary>是否启用千分位（默认 true）</summary>
        [Parameter]
        public bool GroupSeparator { get; set; } = true;

        /// <summary>自定义千分位分隔符（默认逗号）</summary>
        [Parameter]
        public string ThousandsSeparator { get; set; } = ",";

        /// <summary>自定义小数点符号（默认跟随区域；官方 decimal-separator）</summary>
        [Parameter]
        public string DecimalSeparator { get; set; }

        #endregion

        #region 前后缀

        [Parameter]
        public string PrefixText { get; set; }

        [Parameter]
        public RenderFragment Prefix { get; set; }

        [Parameter]
        public string SuffixText { get; set; }

        [Parameter]
        public RenderFragment Suffix { get; set; }

        #endregion

        #region 趋势指示

        /// <summary>趋势值（正 = 上升，负 = 下降）</summary>
        [Parameter]
        public double Trend { get; set; }

        /// <summary>趋势展示：prefix / suffix（默认 suffix）</summary>
        [Parameter]
        public TrendPlacement TrendPlacement { get; set; } = TrendPlacement.Suffix;

        /// <summary>趋势类型：primary / success / warning / info / danger</summary>
        [Parameter]
        public string TrendType { get; set; } = "primary";

        /// <summary>趋势数字格式化（如 "{0:P2}"）</summary>
        [Parameter]
        public string TrendFormat { get; set; } = "{0:+0.##;-0.##;0}";

        /// <summary>是否显示趋势正负符号（默认 true）</summary>
        [Parameter]
        public bool ShowTrendSign { get; set; } = true;

        #endregion

        #region 扩展

        /// <summary>底部附加内容</summary>
        [Parameter]
        public RenderFragment Extra { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        #endregion

        #region 计算属性

        /// <summary>暴露给调用方的格式化后显示值（官方 exposes: displayValue）</summary>
        public string DisplayValueText => DisplayValue;

        protected string StatisticClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-statistic", Cls)
            .ToString();

        protected string TrendClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-statistic__trend")
            .Add($"trend-{TrendType}")
            .AddIf(Trend > 0, "is-up")
            .AddIf(Trend < 0, "is-down")
            .ToString();

        protected bool HasTrend => Trend != 0;

        protected string TrendArrowIcon => Trend > 0 ? "el-icon-top" : "el-icon-bottom";

        protected string DisplayValue
        {
            get
            {
                if (Formatter != null)
                {
                    return Formatter(Value);
                }

                var culture = Culture ?? CultureInfo.CurrentCulture;

                if (!string.IsNullOrWhiteSpace(Format))
                {
                    return string.Format(culture, Format, Value);
                }

                return EpDisplayValue(culture);
            }
        }

        /// <summary>
        /// EP 2.14.0 的 <c>displayValue</c> 算法原样移植（实抓 packages/components/statistic/src/statistic.vue）：
        /// <code>
        /// let [integer, decimal = ''] = String(value).split('.')
        /// decimal = decimal.padEnd(precision, '0').slice(0, precision &gt; 0 ? precision : 0)
        /// integer = integer.replace(/\B(?=(\d{3})+(?!\d))/g, groupSeparator)
        /// return [integer, decimal].join(decimal ? decimalSeparator : '')
        /// </code>
        /// 两处与本库旧写法（N/F 标准数值格式）不等价，都是演示页对齐官方时才暴露的：
        /// ① <c>precision</c> 是<b>截断</b>不是四舍五入（slice 直接砍），1234.567 取 1 位是 1,234.5 而非 1,234.6；
        /// ② 千分位只作用在整数段、小数点只作用在两段接缝，所以 <c>ThousandsSeparator="."</c> 配
        ///    <c>DecimalSeparator=","</c>（欧式写法）不会互相污染；旧写法对整串做 Replace，会把千分位一起换掉。
        /// </summary>
        private string EpDisplayValue(CultureInfo culture)
        {
            if (double.IsNaN(Value) || double.IsInfinity(Value))
            {
                // 对齐 EP 的 isNumber/isNaN 前置判断：非正常数值原样输出，不做分组与精度处理。
                return Value.ToString(culture);
            }

            // 定长小数自定义格式：不会出现科学计数法（JS 的 String(value) 对极大值也是十进制展开）。
            var text = Value.ToString("0.################", CultureInfo.InvariantCulture);
            var dot = text.IndexOf('.');
            var integer = dot < 0 ? text : text.Substring(0, dot);
            var decimals = dot < 0 ? string.Empty : text.Substring(dot + 1);

            var precision = Math.Max(0, Precision);
            if (decimals.Length < precision)
            {
                decimals = decimals.PadRight(precision, '0');
            }
            else if (decimals.Length > precision)
            {
                decimals = decimals.Substring(0, precision);
            }

            if (GroupSeparator)
            {
                integer = GroupEveryThree.Replace(integer, ThousandsSeparator ?? ",");
            }

            if (decimals.Length == 0)
            {
                return integer;
            }

            var separator = string.IsNullOrEmpty(DecimalSeparator)
                ? culture.NumberFormat.NumberDecimalSeparator
                : DecimalSeparator;
            return integer + separator + decimals;
        }

        // 与 EP 同一条正则：从右往左每 3 位插一个千分位符，且不落在首字符边界上（负号前不插）。
        private static readonly Regex GroupEveryThree = new(@"\B(?=(\d{3})+(?!\d))", RegexOptions.Compiled);

        protected string DisplayTrend
        {
            get
            {
                if (!HasTrend) return string.Empty;
                return string.Format(TrendFormat, Trend);
            }
        }

        #endregion
    }

    public enum TrendPlacement
    {
        Prefix,
        Suffix
    }
}