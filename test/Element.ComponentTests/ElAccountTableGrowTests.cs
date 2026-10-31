using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElAccountTableGrow 记账凭证表格。
    /// 覆盖：列结构（无日期/凭证号/操作列）、列宽属性、摘要列悬浮增删行、回车导航、
    /// 合计行（不可编辑 / 自动合计 / 平衡时显示大写金额）、只读模式、数据装载与导出。
    /// </summary>
    public class ElAccountTableGrowTests : BunitContext
    {
        public ElAccountTableGrowTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static ObservableCollection<ElAccountTableGrowRow> Rows(params (decimal? debit, decimal? credit)[] amounts)
        {
            var list = new ObservableCollection<ElAccountTableGrowRow>();

            foreach (var (debit, credit) in amounts)
            {
                list.Add(new ElAccountTableGrowRow
                {
                    Abstracts = "测试摘要",
                    AccountSubject = "银行存款",
                    AmountIn = debit,
                    AmountOut = credit
                });
            }

            return list;
        }

        /// <summary>
        /// 「聚焦元素」相关 JS 调用的目标 id（框架 ElementReference.FocusAsync 产生）。
        /// 返回空列表 = 没有发生聚焦。
        /// </summary>
        private List<string> FocusTargetIds()
            => JSInterop.Invocations
                .Where(i => i.Identifier.IndexOf("focus", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(i => i.Arguments.Count > 0 && i.Arguments[0] is ElementReference reference ? reference.Id : "<none>")
                .ToList();

        /// <summary>重新查询金额输入框（每次交互后旧的元素句柄会失效）</summary>
        private static IReadOnlyList<AngleSharp.Dom.IElement> AmountInputs(IRenderedComponent<ElAccountTableGrow> cut)
            => cut.FindAll("tbody tr.el-account-row input.amount-input");

        #region 列结构

        [Fact]
        public void Renders_OnlyAbstractAccountAndAmountColumns()
        {
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, Rows((100m, null))));

            var headRow1 = cut.FindAll("thead tr:nth-child(1) th").Select(th => th.TextContent.Trim()).ToArray();
            Assert.Equal(new[] { "摘要", "会计科目", "借方金额", "贷方金额" }, headRow1);

            // 双行表头的第二行 = 亿…分 ×2
            Assert.Equal(22, cut.FindAll("thead tr:nth-child(2) th").Count);

            // 已去掉的三列
            Assert.DoesNotContain("操作", cut.Markup);
            Assert.DoesNotContain("凭证号", cut.Markup);
            Assert.DoesNotContain("日期", cut.Markup);

            // 数据行 = 摘要 + 会计科目 + 22 个金额格
            Assert.Equal(24, cut.FindAll("tbody tr.el-account-row td").Count);
            Assert.Equal(22, cut.FindAll("tbody tr.el-account-row input.amount-input").Count);
            Assert.Single(cut.FindAll("tbody tr.el-account-row"));
        }

        [Fact]
        public void Renders_AmountDigitsFromModel()
        {
            // 1280.00 → 千=1 百=2 十=8 元=0 角=0 分=0（前四格为空位）
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, Rows((1280m, null))));

            var values = cut.FindAll("tbody tr.el-account-row input.amount-input").Select(i => i.GetAttribute("value") ?? string.Empty).ToArray();
            Assert.Equal(22, values.Length);
            Assert.Equal(new[] { "", "", "", "", "", "1", "2", "8", "0", "0", "0" }, values.Take(11));
            Assert.All(values.Skip(11), v => Assert.Equal(string.Empty, v));
        }

        [Fact]
        public void Renders_ColumnsAreLabeledWithAmountUnits()
        {
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, Rows((1m, null))));
            var labels = cut.FindAll("thead tr:nth-child(2) th").Select(th => th.TextContent.Trim()).ToArray();

            Assert.Equal(new[] { "亿", "千", "百", "十", "万", "千", "百", "十", "元", "角", "分" }, labels.Take(11));
            Assert.Equal(labels.Take(11), labels.Skip(11));
        }

        #endregion

        #region 列宽

        [Fact]
        public void DefaultColumnWidths_PutAbstractAsRemainderAndAmountAsFixedCell()
        {
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, Rows((1m, null))));
            var style = cut.Find("table").GetAttribute("style");

            // 摘要列自动占满剩余宽度；会计科目与金额格为固定宽度，金额格 22 列
            Assert.Contains("--eat-abstract-width:calc(100% - 220px - 22px * 22)", style);
            Assert.Contains("--eat-account-width:220px", style);
            Assert.Contains("--eat-amount-width:22px", style);
        }

        [Fact]
        public void CustomColumnWidths_AreEmitted()
        {
            var cut = Render<ElAccountTableGrow>(p => p
                .Add(x => x.Rows, Rows((1m, null)))
                .Add(x => x.AbstractWidth, "360px")
                .Add(x => x.AccountWidth, "180px")
                .Add(x => x.AmountCellWidth, "20px"));

            var style = cut.Find("table").GetAttribute("style");

            Assert.Contains("--eat-abstract-width:360px", style);
            Assert.Contains("--eat-account-width:180px", style);
            Assert.Contains("--eat-amount-width:20px", style);
        }

        [Fact]
        public void ColumnCount_FollowsMaxIntegerDigits()
        {
            // 整数位 6（十万）→ 单侧 8 格，共 16 格
            var cut = Render<ElAccountTableGrow>(p => p
                .Add(x => x.Rows, Rows((1m, null)))
                .Add(x => x.MaxIntegerDigits, 6));

            Assert.Equal(16, cut.FindAll("tbody tr.el-account-row input.amount-input").Count);
            Assert.Equal(16, cut.FindAll("thead tr:nth-child(2) th").Count);
            Assert.Contains("--eat-abstract-width:calc(100% - 220px - 22px * 16)", cut.Find("table").GetAttribute("style"));
        }

        #endregion

        #region 合计行

        [Fact]
        public void SummaryRow_IsSingleAndNotEditable()
        {
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, Rows((24m, null), (null, 24m))));
            var summaryRows = cut.FindAll("tbody tr.summary-row");

            Assert.Single(summaryRows);
            Assert.Empty(cut.FindAll("tbody tr.summary-row input"));
            Assert.DoesNotContain("大写金额", cut.Markup);

            var cells = summaryRows[0].Children;
            Assert.Equal(23, cells.Length);                                  // colspan=2 + 22 个金额格
            Assert.Equal("2", cells[0].GetAttribute("colspan"));
        }

        [Fact]
        public void Summary_ShowsUppercaseAmount_WhenDebitEqualsCredit()
        {
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, Rows((24m, null), (null, 24m))));

            Assert.Equal("合计：贰拾肆元整", cut.Find("tbody tr.summary-row td.summary-label").TextContent.Trim());

            var totals = cut.FindAll("tbody tr.summary-row td.is-total").Select(td => td.TextContent.Trim()).ToArray();
            Assert.Equal(22, totals.Length);
            Assert.Equal("2400", string.Concat(totals.Take(11)));
            Assert.Equal("2400", string.Concat(totals.Skip(11)));
        }

        [Fact]
        public void Summary_ComputesTotalsAutomatically()
        {
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, Rows((50000m, null), (1280m, null), (null, 74680m))));

            Assert.Equal(51280m, cut.Instance.TotalIn);
            Assert.Equal(74680m, cut.Instance.TotalOut);
            Assert.False(cut.Instance.IsBalanced);

            var totals = cut.FindAll("tbody tr.summary-row td.is-total").Select(td => td.TextContent.Trim()).ToArray();
            Assert.Equal("5128000", string.Concat(totals.Take(11)));      // 50000 + 1280 → 万5 千1 百2 十8 元0 角0 分0
            Assert.Equal("7468000", string.Concat(totals.Skip(11)));      // 贷方单笔 74680
        }

        [Fact]
        public void Summary_ShowsPlainLabel_OnlyWhenBothTotalsEmpty()
        {
            // 双侧均为空（0/0）→ 仍显示「合　计」
            var zero = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, Rows((0m, 0m))));
            Assert.Equal("合　计", zero.Find("tbody tr.summary-row td.summary-label").TextContent.Trim());
            Assert.False(zero.Instance.IsBalanced);
        }

        [Fact]
        public void Summary_ShowsUppercaseOfRunningTotal_WhenUnbalanced()
        {
            // 借贷不平衡（借100 / 贷99）：实时显示「借方合计」的大写（取非空一侧），而非退回「合　计」
            var unbalanced = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, Rows((100m, null), (null, 99m))));
            Assert.Equal("合计：壹佰元整", unbalanced.Find("tbody tr.summary-row td.summary-label").TextContent.Trim());

            // 仅贷方有值 → 取贷方合计大写
            var creditOnly = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, Rows((null, 1234.56m))));
            Assert.Equal("合计：壹仟贰佰叁拾肆元伍角陆分", creditOnly.Find("tbody tr.summary-row td.summary-label").TextContent.Trim());
        }

        [Fact]
        public void Summary_ToggleUppercaseWithEdit()
        {
            var rows = Rows((24m, null), (null, 24m));
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, rows));

            Assert.StartsWith("合计：", cut.Find("tbody tr.summary-row td.summary-label").TextContent.Trim());

            // 改一位数字（借方分位）→ 借贷不平衡 → 实时显示借方合计大写（而非退回「合　计」）
            cut.FindAll("tbody tr.el-account-row input.amount-input")[10].Input(new ChangeEventArgs { Value = "9" });
            Assert.Equal(24.09m, cut.Instance.TotalIn);
            Assert.Equal("合计：贰拾肆元零玖分", cut.Find("tbody tr.summary-row td.summary-label").TextContent.Trim());
        }

        #endregion

        #region 大写金额

        [Theory]
        [InlineData(0, "零元整")]
        [InlineData(24, "贰拾肆元整")]
        [InlineData(105, "壹佰零伍元整")]
        [InlineData(1001, "壹仟零壹元整")]
        [InlineData(1280, "壹仟贰佰捌拾元整")]
        [InlineData(10100, "壹万零壹佰元整")]
        [InlineData(11000, "壹万壹仟元整")]
        [InlineData(2000, "贰仟元整")]
        [InlineData(1234.56, "壹仟贰佰叁拾肆元伍角陆分")]
        [InlineData(10000, "壹万元整")]
        [InlineData(74680, "柒万肆仟陆佰捌拾元整")]
        [InlineData(100000000, "壹亿元整")]
        [InlineData(100000001, "壹亿零壹元整")]
        [InlineData(10.05, "壹拾元零伍分")]
        [InlineData(100.05, "壹佰元零伍分")]
        [InlineData(0.5, "零元伍角")]
        [InlineData(0.05, "零元零伍分")]
        public void DigitUppercase_Converts(decimal amount, string expected)
        {
            Assert.Equal(expected, ElAccountTableGrow.DigitUppercase(amount));
        }

        #endregion

        #region 回车导航

        [Fact]
        public void EnterInAmountCell_FocusesNextCell()
        {
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, Rows((100m, null))));

            // 注意：每次交互后元素句柄会失效，需要重新查询
            AmountInputs(cut)[0].KeyDown(new KeyboardEventArgs { Key = "Enter" });
            var first = FocusTargetIds().Last();
            Assert.NotEqual("<none>", first);

            AmountInputs(cut)[1].KeyDown(new KeyboardEventArgs { Key = "Enter" });
            var second = FocusTargetIds().Last();

            // 不同列 → 焦点目标不同（列号推进）
            Assert.Equal(2, FocusTargetIds().Count);
            Assert.NotEqual(first, second);
        }

        [Fact]
        public void EnterAtRowEnd_MovesToNextRowAbstractColumn()
        {
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, Rows((100m, null), (200m, null))));

            // 第 1 行最后一格（贷方分位）→ 下一行摘要列
            AmountInputs(cut)[21].KeyDown(new KeyboardEventArgs { Key = "Enter" });
            var toAbstract = FocusTargetIds().Last();
            Assert.NotEqual("<none>", toAbstract);

            // 同一行第 1 格回车 → 目标是本行第 2 格，与上面不同
            AmountInputs(cut)[0].KeyDown(new KeyboardEventArgs { Key = "Enter" });
            var toNextCell = FocusTargetIds().Last();

            Assert.Equal(2, FocusTargetIds().Count);
            Assert.NotEqual(toAbstract, toNextCell);
        }

        [Fact]
        public void EnterAtLastRowLastColumn_DoesNothing()
        {
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, Rows((100m, null))));

            AmountInputs(cut)[21].KeyDown(new KeyboardEventArgs { Key = "Enter" });

            Assert.Empty(FocusTargetIds());
        }

        [Fact]
        public void NonEnterKey_DoesNotMoveFocus()
        {
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, Rows((100m, null))));

            AmountInputs(cut)[0].KeyDown(new KeyboardEventArgs { Key = "Tab" });
            AmountInputs(cut)[0].KeyDown(new KeyboardEventArgs { Key = "a" });
            AmountInputs(cut)[0].KeyDown(new KeyboardEventArgs { Key = "NumpadEnter" });

            Assert.Empty(FocusTargetIds());
        }

        [Fact]
        public void Enter_DoesNothingInReadOnly()
        {
            var cut = Render<ElAccountTableGrow>(p => p
                .Add(x => x.Rows, Rows((100m, null), (200m, null)))
                .Add(x => x.ReadOnly, true));

            AmountInputs(cut)[0].KeyDown(new KeyboardEventArgs { Key = "Enter" });
            AmountInputs(cut)[21].KeyDown(new KeyboardEventArgs { Key = "Enter" });

            Assert.Empty(FocusTargetIds());
        }

        [Fact]
        public void CreditDigits_CommitToOwnColumns()
        {
            // 贷方每一位都必须写进自己那一列（曾因 lambda 捕获循环变量导致贷方整侧失效）
            var rows = Rows((null, 0m));
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, rows));

            AmountInputs(cut)[11].Input(new ChangeEventArgs { Value = "1" });   // 贷方亿位
            Assert.Equal(100000000m, rows[0].AmountOut);
            Assert.Null(rows[0].AmountIn);

            AmountInputs(cut)[19].Input(new ChangeEventArgs { Value = "7" });   // 贷方元位
            Assert.Equal(100000007m, rows[0].AmountOut);

            AmountInputs(cut)[21].Input(new ChangeEventArgs { Value = "5" });   // 贷方分位
            Assert.Equal(100000007.05m, rows[0].AmountOut);
        }

        [Fact]
        public void DigitInput_CommitsAndKeepsDebitCreditExclusive()
        {
            var rows = Rows((0m, 0m));
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, rows));

            // 元位输入 7 → 借方 7
            AmountInputs(cut)[8].Input(new ChangeEventArgs { Value = "7" });
            Assert.Equal(7m, rows[0].AmountIn);

            // 贷方分位输入 5 → 借方被清空（互斥）
            AmountInputs(cut)[21].Input(new ChangeEventArgs { Value = "5" });
            Assert.Equal(0.05m, rows[0].AmountOut);
            Assert.Null(rows[0].AmountIn);

            // 借方角位输入 3 → 仍写借方（角/分位不能被误判为贷方）
            AmountInputs(cut)[9].Input(new ChangeEventArgs { Value = "3" });
            Assert.Equal(0.30m, rows[0].AmountIn);
            Assert.Null(rows[0].AmountOut);
        }

        [Fact]
        public void DigitKeyDown_OverwritesExistingDigitInPlace()
        {
            var rows = Rows((50000m, null));
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, rows));

            // 元位已有 0，keydown 8 → 原位覆盖为 8（50000 → 50008）
            AmountInputs(cut)[8].KeyDown(new KeyboardEventArgs { Key = "8" });
            Assert.Equal(50008m, rows[0].AmountIn);
        }

        [Fact]
        public void DigitKeyDown_NonDigitOrControlKey_DoesNotTouchModel()
        {
            var rows = Rows((50000m, null));
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, rows));

            AmountInputs(cut)[8].KeyDown(new KeyboardEventArgs { Key = "a" });
            AmountInputs(cut)[8].KeyDown(new KeyboardEventArgs { Key = "-" });
            AmountInputs(cut)[8].KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
            AmountInputs(cut)[8].KeyDown(new KeyboardEventArgs { Key = "F2" });

            Assert.Equal(50000m, rows[0].AmountIn);
            Assert.Null(rows[0].AmountOut);
        }

        [Fact]
        public void BackspaceKey_ClearsTheDigit()
        {
            var rows = Rows((50007m, null));
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, rows));

            AmountInputs(cut)[8].KeyDown(new KeyboardEventArgs { Key = "Backspace" });
            Assert.Equal(50000m, rows[0].AmountIn);
        }

        [Fact]
        public void DigitKeyDown_DoesNothingInReadOnly()
        {
            var rows = Rows((50000m, null));
            var cut = Render<ElAccountTableGrow>(p => p
                .Add(x => x.Rows, rows)
                .Add(x => x.ReadOnly, true));

            AmountInputs(cut)[8].KeyDown(new KeyboardEventArgs { Key = "8" });

            Assert.Equal(50000m, rows[0].AmountIn);
        }

        [Fact]
        public void DigitKeyDown_CreditSide_WritesCreditAndClearsDebit()
        {
            var rows = Rows((50000m, null));
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, rows));

            // 贷方分位 keydown 5 → 借贷互斥：贷方 0.05、借方清空
            AmountInputs(cut)[21].KeyDown(new KeyboardEventArgs { Key = "5" });
            Assert.Equal(0.05m, rows[0].AmountOut);
            Assert.Null(rows[0].AmountIn);
        }

        [Fact]
        public void RowsChanged_FiresOnEditsAndRowOperations()
        {
            var rows = Rows((1m, null));
            var count = 0;
            var cut = Render<ElAccountTableGrow>(p => p
                .Add(x => x.Rows, rows)
                .Add(x => x.RowsChanged, EventCallback.Factory.Create(this, () => count++)));

            cut.FindAll("tbody tr.el-account-row input.amount-input")[8].Input(new ChangeEventArgs { Value = "3" });
            Assert.Equal(1, count);

            cut.Find(".abstract-btn.is-add").Click();
            Assert.Equal(2, count);
        }

        #endregion

        #region 摘要列悬浮按钮

        [Fact]
        public void AbstractCell_HostsHoverActionsPerRow()
        {
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, Rows((1m, null), (2m, null))));

            // 默认（非只读）每行渲染一组按钮，靠 CSS hover 显示
            Assert.Equal(2, cut.FindAll("tbody tr.el-account-row .abstract-actions").Count);
            Assert.Equal(2, cut.FindAll("tbody tr.el-account-row .abstract-btn.is-add").Count);
            Assert.Equal(2, cut.FindAll("tbody tr.el-account-row .abstract-btn.is-del").Count);

            foreach (var cell in cut.FindAll("tbody tr.el-account-row td.abstract-cell"))
            {
                Assert.Contains("abstract-actions", cell.InnerHtml);
            }
        }

        [Fact]
        public void AddButton_InsertsEmptyRowBelow()
        {
            var rows = Rows((1m, null), (2m, null), (3m, null));
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, rows));

            cut.FindAll("tbody tr.el-account-row .abstract-btn.is-add")[0].Click();

            Assert.Equal(4, rows.Count);
            Assert.True(rows[1].IsEmpty);
            Assert.Equal(1m, rows[0].AmountIn);
            Assert.Equal(2m, rows[2].AmountIn);
        }

        [Fact]
        public void DelButton_RemovesOwnRowAndKeepsAtLeastOne()
        {
            var rows = Rows((1m, null), (2m, null));
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, rows));

            cut.FindAll("tbody tr.el-account-row .abstract-btn.is-del")[0].Click();

            Assert.Single(rows);
            Assert.Equal(2m, rows[0].AmountIn);

            // 只剩一行时不再删除
            cut.Find("tbody tr.el-account-row .abstract-btn.is-del").Click();
            Assert.Single(rows);
        }

        [Fact]
        public void AddButton_RespectsDefaultRowCountAsMax()
        {
            var rows = Rows((1m, null));
            var cut = Render<ElAccountTableGrow>(p => p
                .Add(x => x.Rows, rows)
                .Add(x => x.DefaultRowCount, 1));

            cut.Find("tbody tr.el-account-row .abstract-btn.is-add").Click();

            Assert.Single(rows);
        }

        #endregion

        #region 只读模式

        [Fact]
        public void ReadOnly_HidesHoverActionsAndLocksInputs()
        {
            var cut = Render<ElAccountTableGrow>(p => p
                .Add(x => x.Rows, Rows((1m, null)))
                .Add(x => x.ReadOnly, true));

            Assert.Contains("is-readonly", cut.Find("table").ClassList);
            Assert.Empty(cut.FindAll(".abstract-actions"));
            Assert.All(
                cut.FindAll("tbody tr.el-account-row input"),
                input => Assert.True(input.HasAttribute("readonly")));
        }

        [Fact]
        public void ReadOnly_IgnoresProgrammaticMutations()
        {
            var rows = Rows((1m, null), (2m, null));
            var cut = Render<ElAccountTableGrow>(p => p
                .Add(x => x.Rows, rows)
                .Add(x => x.ReadOnly, true));

            cut.InvokeAsync(() => cut.Instance.AddRow(0));
            cut.InvokeAsync(() => cut.Instance.DelRow(0));
            cut.Find("tbody tr.el-account-row .abstract-input").Change(new ChangeEventArgs { Value = "改了" });

            Assert.Equal(2, rows.Count);
            Assert.Equal("测试摘要", rows[0].Abstracts);   // 只读：编辑被忽略，原值不变
        }

        #endregion

        #region 数据装载 / 导出

        private sealed class VoucherDto
        {
            public string 摘要 { get; set; }
            public string 会计科目 { get; set; }
            public decimal 借方金额 { get; set; }
            public decimal 贷方金额 { get; set; }
        }

        private sealed class EnglishDto
        {
            public string Abstracts { get; set; }
            public string AccountSubject { get; set; }
            public decimal? AmountIn { get; set; }
            public decimal? AmountOut { get; set; }
        }

        [Fact]
        public void LoadRows_MapsChinesePropertyNames_AndFillsToDefaultRowCount()
        {
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, new ObservableCollection<ElAccountTableGrowRow>()));

            cut.InvokeAsync(() => cut.Instance.LoadRows(new[]
            {
                new VoucherDto { 摘要 = "提取现金备用", 会计科目 = "库存现金", 借方金额 = 2000m },
                new VoucherDto { 摘要 = "提取现金备用", 会计科目 = "银行存款", 贷方金额 = 2000m }
            }));

            var rows = cut.Instance.Rows;

            Assert.Equal(10, rows.Count);                        // 补齐到 DefaultRowCount
            Assert.Equal("提取现金备用", rows[0].Abstracts);
            Assert.Equal("库存现金", rows[0].AccountSubject);
            Assert.Equal(2000m, rows[0].AmountIn);
            Assert.Null(rows[0].AmountOut);                      // 0 → null（借贷互斥）
            Assert.Equal(2000m, rows[1].AmountOut);
            Assert.Null(rows[1].AmountIn);
            Assert.True(rows[2].IsEmpty);
        }

        [Fact]
        public void LoadRows_MapsEnglishPropertyNames()
        {
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, new ObservableCollection<ElAccountTableGrowRow>()));

            cut.InvokeAsync(() => cut.Instance.LoadRows(new[]
            {
                new EnglishDto { Abstracts = "支付办公用品费", AccountSubject = "管理费用", AmountIn = 1280m },
                new EnglishDto { Abstracts = "支付办公用品费", AccountSubject = "银行存款", AmountOut = 1280m }
            }));

            Assert.Equal("支付办公用品费", cut.Instance.Rows[0].Abstracts);
            Assert.Equal("管理费用", cut.Instance.Rows[0].AccountSubject);
            Assert.Equal(1280m, cut.Instance.Rows[0].AmountIn);
            Assert.Equal(1280m, cut.Instance.Rows[1].AmountOut);
        }

        [Fact]
        public void LoadRows_WithSelector_UsesCustomMapping()
        {
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, new ObservableCollection<ElAccountTableGrowRow>()));

            cut.InvokeAsync(() => cut.Instance.LoadRows(
                new[] { new VoucherDto { 摘要 = "自定义映射", 会计科目 = "库存现金", 借方金额 = 88m } },
                dto => new ElAccountTableGrowRow { Abstracts = dto.摘要 + "!", AccountSubject = dto.会计科目, AmountIn = dto.借方金额 }));

            Assert.Equal("自定义映射!", cut.Instance.Rows[0].Abstracts);
        }

        [Fact]
        public void LoadRows_TruncatesBeyondMaxRows()
        {
            var cut = Render<ElAccountTableGrow>(p => p
                .Add(x => x.Rows, new ObservableCollection<ElAccountTableGrowRow>())
                .Add(x => x.DefaultRowCount, 3));

            cut.InvokeAsync(() => cut.Instance.LoadRows(Enumerable.Range(0, 10).Select(i => new ElAccountTableGrowRow { Abstracts = "行" + i })));

            Assert.Equal(3, cut.Instance.Rows.Count);
            Assert.Equal("行0", cut.Instance.Rows[0].Abstracts);
        }

        [Fact]
        public void GetData_FiltersEmptyRowsByDefault()
        {
            var rows = new ObservableCollection<ElAccountTableGrowRow>
            {
                new(),
                new() { Abstracts = "有内容", AccountSubject = "银行存款", AmountIn = 5m },
                new(),
                new() { Abstracts = "", AmountOut = 0m }
            };
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, rows));

            Assert.Single(cut.Instance.GetData());
            Assert.Equal(4, cut.Instance.GetData(includeEmptyRows: true).Count);
            Assert.Equal(new[] { "有内容" }, cut.Instance.GetData().Select(r => r.Abstracts));
        }

        [Fact]
        public void GetData_ProjectsToBusinessType()
        {
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, Rows((24m, null), (null, 24m))));
            var exported = cut.Instance.GetData(r => new VoucherDto
            {
                摘要 = r.Abstracts,
                会计科目 = r.AccountSubject,
                借方金额 = r.AmountIn ?? 0m,
                贷方金额 = r.AmountOut ?? 0m
            });

            Assert.Equal(2, exported.Count);
            Assert.Equal(24m, exported[0].借方金额);
            Assert.Equal(24m, exported[1].贷方金额);
        }

        [Fact]
        public void ClearAndReset_KeepComponentUsable()
        {
            var rows = Rows((1m, null), (2m, null));
            var cut = Render<ElAccountTableGrow>(p => p.Add(x => x.Rows, rows));

            cut.InvokeAsync(() => cut.Instance.Clear());
            Assert.Single(cut.Instance.Rows);
            Assert.True(cut.Instance.Rows[0].IsEmpty);

            cut.InvokeAsync(() => cut.Instance.ResetRows());
            Assert.Equal(10, cut.Instance.Rows.Count);

            Assert.Equal(10, cut.FindAll("tbody tr.el-account-row").Count);
            Assert.Single(cut.FindAll("tbody tr.summary-row"));
        }

        #endregion

        #region 守卫

        [Fact]
        public void Guard_NoCustomJavaScript_DependsOnFrameworkFocusOnly()
        {
            var razor = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "ElAccountTableGrow.razor"));
            var code = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "ElAccountTableGrow.razor.cs"));

            foreach (var text in new[] { razor, code })
            {
                Assert.DoesNotContain("IJSRuntime", text);
                Assert.DoesNotContain("JSRuntime", text);
                Assert.DoesNotContain("<script", text);
                Assert.DoesNotContain("dom.js", text);
                Assert.DoesNotContain("eval(", text);
            }

            // 焦点导航只用框架自带的 ElementReference.FocusAsync（无需自写 JS）
            Assert.Contains("FocusAsync", code);
        }

        [Fact]
        public void Guard_CssHasNoImportantRule()
        {
            var razor = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "ElAccountTableGrow.razor"));
            var styleStart = razor.IndexOf("<style>", StringComparison.Ordinal);
            var styleEnd = razor.IndexOf("</style>", StringComparison.Ordinal);

            Assert.True(styleStart >= 0 && styleEnd > styleStart, "未找到组件的 <style> 段");
            Assert.DoesNotContain("!important", razor.Substring(styleStart, styleEnd - styleStart));
        }

        [Fact]
        public void Guard_ScopedClassesExistInStyleBlock()
        {
            var razor = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "ElAccountTableGrow.razor"));
            var styleStart = razor.IndexOf("<style>", StringComparison.Ordinal);
            var styleEnd = razor.IndexOf("</style>", StringComparison.Ordinal);
            var style = razor.Substring(styleStart, styleEnd - styleStart);

            foreach (var className in new[]
                     {
                         "el-account-table-grow", "abstract-cell", "abstract-input", "abstract-actions",
                         "abstract-btn", "amount-input", "amount-sub-header", "summary-label", "is-total", "is-readonly"
                     })
            {
                Assert.Contains(className, style);
            }
        }

        private static string RepoRoot([CallerFilePath] string file = null)
        {
            var directory = new FileInfo(file).Directory;

            return directory.Parent.Parent.FullName;
        }

        #endregion
    }
}
