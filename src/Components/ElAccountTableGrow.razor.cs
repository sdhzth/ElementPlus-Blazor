using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ElementPlus
{
    /// <summary>
    /// 记账凭证表格（Element Plus 风格、纯 C# 渲染）。
    /// <para>列：摘要 | 会计科目 | 借方金额（亿…分 逐位） | 贷方金额（亿…分 逐位）。</para>
    /// <para>末行为不可编辑的合计行：左侧「合计」实时显示借方合计 / 贷方合计（取非空一侧）的标准财务大写金额；双侧均为空时显示「合　计」。</para>
    /// </summary>
    public partial class ElAccountTableGrow : ElementComponentBase
    {
        #region 参数

        /// <summary>行集合（双向绑定数据源）</summary>
        [Parameter]
        public ObservableCollection<ElAccountTableGrowRow> Rows { get; set; } = new();

        /// <summary>类型（1=记/2=收/3=付/4=转）</summary>
        [Parameter]
        public string Category { get; set; } = "1";

        /// <summary>默认行数（同时作为最大行数，默认 10）</summary>
        [Parameter]
        public int DefaultRowCount { get; set; } = 10;

        /// <summary>会计科目下拉数据源</summary>
        [Parameter]
        public IEnumerable<object> AccountItems { get; set; }

        /// <summary>会计科目 ValueField</summary>
        [Parameter]
        public string AccountValueField { get; set; } = "Code";

        /// <summary>会计科目 TextField</summary>
        [Parameter]
        public string AccountTextField { get; set; } = "Name";

        /// <summary>金额最大整数位数（9 = 亿级；列数 = 该值 + 角 + 分）</summary>
        [Parameter]
        public int MaxIntegerDigits { get; set; } = 9;

        /// <summary>只读模式：所有输入框不可编辑、不显示摘要列的悬浮增删按钮、合计行本就不可编辑</summary>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>摘要列宽（"auto" = 自动占满剩余宽度，也可给 "320px" / "30%" 等具体长度）</summary>
        [Parameter]
        public string AbstractWidth { get; set; } = "auto";

        /// <summary>会计科目列宽</summary>
        [Parameter]
        public string AccountWidth { get; set; } = "220px";

        /// <summary>金额单格列宽（亿/千/百/十/万/千/百/十/元/角/分 每一格的宽度）</summary>
        [Parameter]
        public string AmountCellWidth { get; set; } = "22px";

        /// <summary>行数据发生变化（改金额 / 改摘要 / 增删行）时触发，便于宿主标记脏数据或即时保存</summary>
        [Parameter]
        public EventCallback RowsChanged { get; set; }

        #endregion

        /// <summary>单侧金额列数（整数位 + 角 + 分）</summary>
        protected int AmountColumnsPerSide => Math.Max(3, MaxIntegerDigits) + 2;

        /// <summary>金额位标签（亿千百十万千百十圆角分）</summary>
        private static readonly string[] AmountLabels = { "亿", "千", "百", "十", "万", "千", "百", "十", "元", "角", "分" };

        /// <summary>金额位标签（长度不足时按位补空）</summary>
        protected static string[] GetAmountLabels() => AmountLabels;

        /// <summary>摘要列实际 CSS 宽度（auto → 用 calc 占满剩余宽度，金额列保持精确宽度）</summary>
        private string AbstractColumnCssWidth =>
            string.IsNullOrWhiteSpace(AbstractWidth) || string.Equals(AbstractWidth.Trim(), "auto", StringComparison.OrdinalIgnoreCase)
                ? $"calc(100% - {AccountWidth} - {AmountCellWidth} * {AmountColumnsPerSide * 2})"
                : AbstractWidth;

        /// <summary>表格内联样式（列宽以 CSS 变量下传，宿主 Style 追加在后可覆盖）</summary>
        private string ColumnStyle =>
            $"--eat-abstract-width:{AbstractColumnCssWidth};--eat-account-width:{AccountWidth};--eat-amount-width:{AmountCellWidth};{Style}";

        // 焦点引用：按行的 Key 定位，避免增删行后下标错位（@ref 需要可赋值的成员，故用持有者对象）
        private sealed class CellRefHolder
        {
            public ElementReference Element;
        }

        private readonly Dictionary<string, List<CellRefHolder>> _amountRefs = new();
        private readonly Dictionary<string, CellRefHolder> _abstractRefs = new();

        protected override void OnInitialized()
        {
            base.OnInitialized();
            if (Rows == null) Rows = new ObservableCollection<ElAccountTableGrowRow>();
            NormalizeRowCount();
        }

        /// <summary>行数收敛到 [1, DefaultRowCount]</summary>
        private void NormalizeRowCount()
        {
            if (Rows.Count == 0)
            {
                for (int i = 0; i < DefaultRowCount; i++) Rows.Add(new ElAccountTableGrowRow());
            }
            else if (Rows.Count > DefaultRowCount)
            {
                while (Rows.Count > DefaultRowCount) Rows.RemoveAt(Rows.Count - 1);
            }
        }

        #region 行增删

        /// <summary>重置到 DefaultRowCount 行（全部为空行）</summary>
        public void ResetRows()
        {
            Rows.Clear();
            _amountRefs.Clear();
            _abstractRefs.Clear();
            for (int i = 0; i < DefaultRowCount; i++) Rows.Add(new ElAccountTableGrowRow());
            StateHasChanged();
        }

        /// <summary>清空数据（保留 1 行空行）</summary>
        public void Clear()
        {
            Rows.Clear();
            _amountRefs.Clear();
            _abstractRefs.Clear();
            Rows.Add(new ElAccountTableGrowRow());
            StateHasChanged();
            RowsChanged.InvokeAsync();
        }

        /// <summary>在指定行之后插入一行（摘要列 + 按钮调用；超过最大行数时不动作）</summary>
        public void AddRow(int index)
        {
            if (ReadOnly) return;
            if (Rows.Count >= DefaultRowCount) return;
            if (index < 0 || index >= Rows.Count) index = Rows.Count - 1;
            Rows.Insert(index + 1, new ElAccountTableGrowRow());
            StateHasChanged();
            RowsChanged.InvokeAsync();
        }

        /// <summary>删除指定行（至少保留 1 行）</summary>
        public void DelRow(int index)
        {
            if (ReadOnly) return;
            if (Rows.Count <= 1) return;
            if (index < 0 || index >= Rows.Count) return;
            var key = Rows[index].Key;
            Rows.RemoveAt(index);
            _amountRefs.Remove(key);
            _abstractRefs.Remove(key);
            StateHasChanged();
            RowsChanged.InvokeAsync();
        }

        #endregion

        #region 焦点与键盘导航（纯 C#，ElementReference.FocusAsync）

        /// <summary>取某行的金额格引用集合（按列，懒创建）</summary>
        private List<CellRefHolder> GetRowCellRefs(string rowKey)
        {
            var need = AmountColumnsPerSide * 2;
            if (!_amountRefs.TryGetValue(rowKey, out var list) || list.Count != need)
            {
                list = new List<CellRefHolder>(need);
                for (int i = 0; i < need; i++) list.Add(new CellRefHolder());
                _amountRefs[rowKey] = list;
            }
            return list;
        }

        /// <summary>取某行摘要输入框的引用（懒创建）</summary>
        private CellRefHolder GetAbstractCellRef(string rowKey)
        {
            if (!_abstractRefs.TryGetValue(rowKey, out var holder))
            {
                holder = new CellRefHolder();
                _abstractRefs[rowKey] = holder;
            }
            return holder;
        }

        private static bool IsUsable(CellRefHolder holder) => holder != null && holder.Element.Context != null;

        /// <summary>
        /// 金额格回车导航：同一行内跳到下一格（借方 → 贷方 → 行尾）；
        /// 行尾回车跳到下一行的摘要列并进入编辑。仅非只读模式生效。
        /// </summary>
        protected async Task OnAmountKeyDown(KeyboardEventArgs e, ElAccountTableGrowRow row, int column)
        {
            if (ReadOnly) return;

            var key = e.Key ?? string.Empty;

            // 每格只允许一位数字：keydown 直接写入模型，
            // 配合 maxlength="1"（浏览器拦截真实插入）实现「重复输入即覆盖」。
            if (key.Length == 1 && char.IsDigit(key[0]))
            {
                WriteDigit(row, column, key[0]);
                return;
            }

            if (string.Equals(key, "Backspace", StringComparison.Ordinal)
                || string.Equals(key, "Delete", StringComparison.Ordinal))
            {
                WriteDigit(row, column, '\0');
                return;
            }

            if (!string.Equals(key, "Enter", StringComparison.Ordinal)) return;

            var lastColumn = AmountColumnsPerSide * 2 - 1;
            if (column < lastColumn)
            {
                await FocusAmountAsync(row.Key, column + 1);
                return;
            }

            var index = Rows.IndexOf(row);
            if (index < 0 || index + 1 >= Rows.Count) return;

            if (_abstractRefs.TryGetValue(Rows[index + 1].Key, out var target) && IsUsable(target))
            {
                await target.Element.FocusAsync();
            }
        }

        /// <summary>把一位数字（'\0' 表示清除）写入指定列，并合并回行模型</summary>
        private void WriteDigit(ElAccountTableGrowRow row, int column, char ch)
        {
            var isDebit = column < AmountColumnsPerSide;
            var index = isDebit ? column : column - AmountColumnsPerSide;
            var digits = SplitAmount(isDebit ? row.AmountIn : row.AmountOut);

            if (index < 0 || index >= digits.Length) return;

            digits[index] = ch;
            MergeDigitsToRow(row, digits, isDebit);
            StateHasChanged();
            RowsChanged.InvokeAsync();
        }

        private async Task FocusAmountAsync(string rowKey, int column)
        {
            if (_amountRefs.TryGetValue(rowKey, out var refs)
                && column >= 0 && column < refs.Count
                && IsUsable(refs[column]))
            {
                await refs[column].Element.FocusAsync();
            }
        }

        #endregion

        #region 摘要 / 会计科目

        protected void OnAbstractChanged(ElAccountTableGrowRow row, ChangeEventArgs e)
        {
            if (ReadOnly) return;
            row.Abstracts = e.Value?.ToString();
            RowsChanged.InvokeAsync();
        }

        protected void OnAccountChanged(ElAccountTableGrowRow row, object value)
        {
            if (ReadOnly) return;
            row.AccountSubject = value?.ToString();
            RowsChanged.InvokeAsync();
        }

        #endregion

        #region 金额计算（C# 版）

        /// <summary>把 decimal 拆成 {整数各位…, 角, 分} 共 AmountColumnsPerSide 个字符（'\0' 表示空位）</summary>
        protected char[] SplitAmount(decimal? value)
        {
            var digits = new char[AmountColumnsPerSide];
            for (int i = 0; i < digits.Length; i++) digits[i] = '\0';

            if (!value.HasValue || value.Value == 0) return digits;

            var abs = Math.Abs(value.Value);
            var s = abs.ToString("F2", CultureInfo.InvariantCulture);
            var parts = s.Split('.');
            var intStr = parts[0];
            var decStr = parts.Length > 1 ? parts[1] : "00";

            if (intStr.Length > MaxIntegerDigits) intStr = intStr.Substring(intStr.Length - MaxIntegerDigits);

            var padLeft = MaxIntegerDigits - intStr.Length;
            for (int i = 0; i < MaxIntegerDigits && i < digits.Length; i++)
            {
                digits[i] = i < padLeft ? '\0' : intStr[i - padLeft];
            }
            if (digits.Length > MaxIntegerDigits) digits[MaxIntegerDigits] = decStr.Length > 0 ? decStr[0] : '\0';
            if (digits.Length > MaxIntegerDigits + 1) digits[MaxIntegerDigits + 1] = decStr.Length > 1 ? decStr[1] : '\0';

            return digits;
        }

        /// <summary>单元格显示文本（空位不显示字符）</summary>
        protected static string DigitText(char[] digits, int index)
            => index >= 0 && index < digits.Length && digits[index] != '\0' ? digits[index].ToString() : string.Empty;

        /// <summary>逐位输入 → 合并回 AmountIn / AmountOut（借贷互斥）</summary>
        protected void OnDigitInput(ElAccountTableGrowRow row, char[] digits, int index, bool isDebit, ChangeEventArgs e)
        {
            if (ReadOnly) return;
            if (index < 0 || index >= digits.Length) return;

            var text = e.Value?.ToString();
            var ch = string.IsNullOrEmpty(text) ? '\0' : text[text.Length - 1];
            digits[index] = char.IsDigit(ch) ? ch : '\0';

            MergeDigitsToRow(row, digits, isDebit);
            StateHasChanged();
            RowsChanged.InvokeAsync();
        }

        /// <summary>逐位输入 → 合并回 AmountIn / AmountOut</summary>
        protected void MergeDigitsToRow(ElAccountTableGrowRow row, char[] digits, bool isDebit)
        {
            var val = DigitsToDecimal(digits);

            if (isDebit)
            {
                row.AmountIn = val;
                if (val != 0) row.AmountOut = null; // 借贷互斥
            }
            else
            {
                row.AmountOut = val;
                if (val != 0) row.AmountIn = null;
            }
        }

        /// <summary>位数组 → decimal（空位按 0 补齐）</summary>
        private decimal DigitsToDecimal(char[] digits)
        {
            var chars = new char[MaxIntegerDigits + 2];
            for (int i = 0; i < chars.Length; i++)
            {
                var c = i < digits.Length ? digits[i] : '\0';
                chars[i] = c == '\0' ? '0' : c;
            }
            var intStr = new string(chars, 0, MaxIntegerDigits);
            var decStr = new string(chars, MaxIntegerDigits, 2);
            return decimal.TryParse($"{intStr}.{decStr}", NumberStyles.Number, CultureInfo.InvariantCulture, out var val) ? val : 0m;
        }

        /// <summary>合计借方</summary>
        public decimal TotalIn => Rows.Sum(r => r.AmountIn ?? 0);

        /// <summary>合计贷方</summary>
        public decimal TotalOut => Rows.Sum(r => r.AmountOut ?? 0);

        /// <summary>借贷平衡（双方都不为 0 且相等）</summary>
        public bool IsBalanced => TotalIn != 0 && TotalIn == TotalOut;

        /// <summary>合计行左侧文案：实时以「借方合计 / 贷方合计」（取非空一侧）转换为标准财务大写</summary>
        protected string BuildSummaryLabel(decimal totalIn, decimal totalOut)
        {
            var amount = totalIn != 0 ? totalIn : totalOut;
            return amount != 0 ? "合计：" + DigitUppercase(amount) : "合　计";
        }

        /// <summary>大写金额转换（标准财务大写：人民币大写规则）</summary>
        public static string DigitUppercase(decimal amount)
        {
            if (amount == 0) return "零元整";

            var negative = amount < 0;
            amount = Math.Abs(amount);
            // 金额只精确到分（四舍五入）
            amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);

            var digit = new[] { "零", "壹", "贰", "叁", "肆", "伍", "陆", "柒", "捌", "玖" };
            var intPart = (long)Math.Floor(amount);
            var centPart = (int)Math.Round((amount - intPart) * 100);
            var jiao = centPart / 10;
            var fen = centPart % 10;

            // 整数部分（不到 1 元时为「零」，用于「零元X角X分」开头）
            var intStr = intPart == 0 ? "零" : ConvertInteger(intPart, digit);

            // 小数部分
            string decStr;
            if (jiao == 0 && fen == 0)
            {
                decStr = "整";
            }
            else
            {
                decStr = string.Empty;
                // 元后无角但分不为零（如 10.05、0.05）→ 角位补「零」（标准财务大写规则）
                if (jiao == 0 && fen > 0) decStr += "零";
                if (jiao > 0) decStr += digit[jiao] + "角";
                if (fen > 0) decStr += digit[fen] + "分";
            }

            return (negative ? "负" : string.Empty) + intStr + "元" + decStr;
        }

        /// <summary>把整数（不含小数）转为大写，逐 4 位一段处理（万 / 亿 / 兆），并正确处理段间零</summary>
        private static string ConvertInteger(long n, string[] digit)
        {
            if (n == 0) return "零";

            var units = new[] { "", "拾", "佰", "仟" };
            var sections = new[] { "", "万", "亿", "兆" };

            // 计算最高段索引（避免无意义的高位循环）
            var maxSection = 0;
            for (var t = n; t > 0; t /= 10000)
            {
                if (maxSection >= sections.Length - 1) break;
                maxSection++;
            }

            var sb = new StringBuilder();
            var pendingZero = false; // 上一段是否为全空段（段间需补零）

            for (var i = maxSection; i >= 0; i--)
            {
                var divisor = (long)Math.Pow(10000, i);
                var sectionValue = (int)((n / divisor) % 10000);

                if (sectionValue == 0)
                {
                    if (sb.Length > 0) pendingZero = true;
                    continue;
                }

                var sectionStr = ConvertSection(sectionValue, units, digit);
                // 段间补零：① 上一段全空；② 当前段的高位（千位）为空但已有更高段内容（如 10100 → 壹万零壹佰）
                if (pendingZero || (sb.Length > 0 && sectionValue < 1000))
                {
                    sectionStr = "零" + sectionStr;
                }

                pendingZero = false;
                sb.Append(sectionStr).Append(sections[i]);
            }

            return sb.ToString();
        }

        /// <summary>把一段 4 位整数（1~9999）转为大写，处理段内连续零（连续零只写一次）</summary>
        private static string ConvertSection(int sectionValue, string[] units, string[] digit)
        {
            var sb = new StringBuilder();
            var zeroPending = false;

            for (var i = 3; i >= 0; i--)
            {
                var d = (sectionValue / (int)Math.Pow(10, i)) % 10;
                if (d == 0)
                {
                    if (sb.Length > 0) zeroPending = true;
                }
                else
                {
                    if (zeroPending)
                    {
                        sb.Append("零");
                        zeroPending = false;
                    }
                    sb.Append(digit[d]).Append(units[i]);
                }
            }

            return sb.ToString();
        }

        #endregion

        #region 数据装载 / 保存（快捷 API）

        /// <summary>装载数据：先清空，再写入（不足 DefaultRowCount 时用空行补齐，超过则截断）</summary>
        public void LoadRows(IEnumerable<ElAccountTableGrowRow> data)
        {
            Rows.Clear();
            _amountRefs.Clear();
            _abstractRefs.Clear();
            if (data != null)
            {
                foreach (var row in data)
                {
                    if (row == null) continue;
                    if (Rows.Count >= DefaultRowCount) break;
                    Rows.Add(row);
                }
            }
            while (Rows.Count < DefaultRowCount) Rows.Add(new ElAccountTableGrowRow());
            if (Rows.Count == 0) Rows.Add(new ElAccountTableGrowRow());
            StateHasChanged();
        }

        /// <summary>装载任意实体：不传 selector 时按属性名约定自动映射（摘要/Abstract、会计科目/Subject、借方/AmountIn…）</summary>
        public void LoadRows<TSource>(IEnumerable<TSource> data, Func<TSource, ElAccountTableGrowRow> selector = null)
        {
            var mapper = selector ?? BuildConventionMapper<TSource>();
            LoadRows(data?.Select(mapper));
        }

        /// <summary>导出当前数据（默认过滤掉整行空白行）</summary>
        public List<ElAccountTableGrowRow> GetData(bool includeEmptyRows = false)
            => Rows.Where(r => includeEmptyRows || !r.IsEmpty).ToList();

        /// <summary>导出任意实体（与 LoadRows&lt;T&gt; 对称，便于直接回写业务对象）</summary>
        public List<TResult> GetData<TResult>(Func<ElAccountTableGrowRow, TResult> selector, bool includeEmptyRows = false)
            => GetData(includeEmptyRows).Select(selector).ToList();

        private static Func<TSource, ElAccountTableGrowRow> BuildConventionMapper<TSource>()
        {
            var props = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var date = FindProperty(props, "Date", "BizDate", "VoucherDate", "OccurDate", "日期", "发生日期");
            var voucher = FindProperty(props, "VoucherNo", "VoucherNumber", "Voucher", "凭证号", "凭证编号");
            var abstracts = FindProperty(props, "Abstracts", "Abstract", "Summary", "Digest", "Remark", "摘要", "内容", "说明");
            var subject = FindProperty(props, "AccountSubject", "Subject", "AccountName", "Account", "科目", "会计科目");
            var debit = FindProperty(props, "AmountIn", "Debit", "DebitAmount", "借方", "借方金额");
            var credit = FindProperty(props, "AmountOut", "Credit", "CreditAmount", "贷方", "贷方金额");

            return source => new ElAccountTableGrowRow
            {
                Date = ToText(date, source),
                VoucherNo = ToText(voucher, source),
                Abstracts = ToText(abstracts, source),
                AccountSubject = ToText(subject, source),
                AmountIn = ToDecimal(debit, source),
                AmountOut = ToDecimal(credit, source)
            };
        }

        private static PropertyInfo FindProperty(PropertyInfo[] props, params string[] names)
        {
            foreach (var name in names)
            {
                var hit = props.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
                if (hit != null) return hit;
            }
            return props.FirstOrDefault(p => names.Any(n => p.Name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static string ToText(PropertyInfo prop, object source)
        {
            if (prop == null || source == null) return null;
            var value = prop.GetValue(source);
            return value?.ToString();
        }

        private static decimal? ToDecimal(PropertyInfo prop, object source)
        {
            if (prop == null || source == null) return null;
            var value = prop.GetValue(source);
            if (value == null) return null;
            decimal parsed;
            if (value is decimal d) parsed = d;
            else if (!decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed)) return null;
            return parsed == 0 ? null : parsed; // 0 视为空（借贷互斥更自然）
        }

        #endregion
    }
}
