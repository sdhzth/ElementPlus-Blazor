using System;

namespace ElementPlus
{
    /// <summary>会计凭证行（ElAccountTableGrow 专用行模型）</summary>
    public class ElAccountTableGrowRow
    {
        /// <summary>内部唯一键（用于表格行缓存与焦点定位）</summary>
        public string Key { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 日期（yyyy-MM-dd）。组件当前不再渲染该列，保留字段以兼容既有绑定与数据装载。
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// 凭证号。组件当前不再渲染该列，保留字段以兼容既有绑定与数据装载。
        /// </summary>
        public string VoucherNo { get; set; }

        /// <summary>摘要</summary>
        public string Abstracts { get; set; }

        /// <summary>会计科目</summary>
        public string AccountSubject { get; set; }

        /// <summary>借方金额（decimal，null 表示空）</summary>
        public decimal? AmountIn { get; set; }

        /// <summary>贷方金额（decimal，null 表示空）</summary>
        public decimal? AmountOut { get; set; }

        /// <summary>整行为空（摘要、会计科目、借贷金额均无内容）</summary>
        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(Abstracts)
            && string.IsNullOrWhiteSpace(AccountSubject)
            && (AmountIn ?? 0) == 0
            && (AmountOut ?? 0) == 0;

        /// <summary>本行金额（借方优先，其次贷方，均为空时返回 0）</summary>
        public decimal Amount => AmountIn ?? AmountOut ?? 0;

        /// <summary>复制一行（含金额，Key 重新生成）</summary>
        public ElAccountTableGrowRow Clone() => new()
        {
            Date = Date,
            VoucherNo = VoucherNo,
            Abstracts = Abstracts,
            AccountSubject = AccountSubject,
            AmountIn = AmountIn,
            AmountOut = AmountOut
        };
    }
}
