using Bunit;
using ElementPlus;
using ElementPlus.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 可编辑表格（IsEditable）单元格编辑器的契约：
    /// ① 文本/数字框<b>不挂</b>整表重渲染通知（否则每敲一个字重绘整表，实测 342~382ms/键）；
    /// ② 选中后不会自己改显示的控件（ElSelect 等）<b>必须仍然挂</b>，不然选完看不见；
    /// ③ 一次按键<b>不重渲染 ElTable 自身</b>（按格局部刷新，接收者是单元格宿主）；
    /// ④ 一条口径证据用例说明为什么③必须自己数 BuildRenderTree，而不能用 bUnit 的 RenderCount。
    /// 另附一条表格上下文里的日期时间编辑器复现用例。
    /// </summary>
    public class TableEditCellTests : BunitContext
    {
        public TableEditCellTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddElementServices();
        }

        public class Cat
        {
            public string Value { get; set; }

            public string Text { get; set; }
        }

        public class Row
        {
            public string Name { get; set; }

            public string Category { get; set; }

            public DateTime? CreatedAt { get; set; }
        }

        private static List<Row> Data() => new List<Row>
        {
            new Row { Name = "阿莫西林", Category = "西药", CreatedAt = null },
        };

        /// <summary>
        /// 只统计<b>自己</b> BuildRenderTree 次数的表格子类。
        /// <para>
        /// 为什么必须自己数：bUnit 的 <c>RenderCount</c> 会把子组件的重渲染算到被渲染组件头上
        /// （见 <see cref="BunitRenderCountAlsoCountsDescendantRenders"/>）。用它判断"整表有没有重渲染"
        /// 会把"单元格宿主 + 输入框各渲染一次"误读成"表格渲染了两次"，从而去追一个不存在的第二来源。
        /// </para>
        /// </summary>
        public class CountedTable : ElTable
        {
            public int Builds { get; private set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                Builds++;
                base.BuildRenderTree(builder);
            }
        }

        private IRenderedComponent<T> RenderEditableTable<T>() where T : ElTable
        {
            var rows = Data();
            var cut = Render<T>(p => p
                .Add(x => x.DataSource, rows)
                .Add(x => x.IsEditable, true)
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .AddCascadingValue("Page", (ElementComponentBase)new ElementDialogBase())
                .Add(x => x.ChildContent, builder =>
                {
                    builder.OpenComponent<ElTableColumn>(0);
                    builder.AddAttribute(1, nameof(ElTableColumn.Property), nameof(Row.Name));
                    builder.AddAttribute(2, nameof(ElTableColumn.Text), "名称");
                    builder.AddAttribute(3, nameof(ElTableColumn.IsEditable), true);
                    builder.CloseComponent();

                    builder.OpenComponent<ElTableColumn>(10);
                    builder.AddAttribute(11, nameof(ElTableColumn.Property), nameof(Row.Category));
                    builder.AddAttribute(12, nameof(ElTableColumn.Text), "类别");
                    builder.AddAttribute(13, nameof(ElTableColumn.IsEditable), true);
                    builder.AddAttribute(14, nameof(ElTableColumn.Editor), typeof(ElSelect<string>));
                    builder.AddAttribute(15, nameof(ElTableColumn.EditorDataSource),
                        new List<Cat> { new Cat { Value = "西药", Text = "西药" } });
                    builder.AddAttribute(16, nameof(ElTableColumn.EditorValueField), "Value");
                    builder.AddAttribute(17, nameof(ElTableColumn.EditorTextField), "Text");
                    builder.CloseComponent();
                }));

            cut.InvokeAsync(() => cut.Instance.BeginEditRowAsync(rows[0])).Wait();
            return cut;
        }

        private static TableHeader HeaderOf(ElTable table, string property)
        {
            return table.Columns.FirstOrDefault(h => h.Property?.Name == property);
        }

        [Fact]
        public void TextEditorDoesNotSubscribeWholeTableRerender()
        {
            var cut = RenderEditableTable<ElTable>();

            var header = HeaderOf(cut.Instance, nameof(Row.Name));
            Assert.NotNull(header);

            // 文本框自己显示用户输入；挂通知会让每次按键付一次整表重渲染的钱
            Assert.Null(header.EditorRenderConfig.ValueChanged);
        }

        [Fact]
        public void SelectEditorStillNotifiesTableAfterPick()
        {
            var cut = RenderEditableTable<ElTable>();

            var header = HeaderOf(cut.Instance, nameof(Row.Category));
            Assert.NotNull(header);

            // 这条是"别改过头"的护栏：ElSelect 选完不会自己改显示，通知必须留着
            Assert.NotNull(header.EditorRenderConfig.ValueChanged);
        }

        // 注：测试里塞的假 Page（从未被渲染的 ElementDialogBase）只能承担"不被调用"的角色 ——
        // 一旦有回调真的以 Page 为接收者去 StateHasChanged，就会抛 "render handle is not yet assigned"。
        // 所以下面那条按键用例能成立，本身也说明接收者已经不在 Page / 表格上。
        // 端到端的"输入 → 保存 → 行值更新"仍由浏览器探针负责：
        // demo/tools/probes/verify-table-edit-typing.cjs。

        /// <summary>
        /// 按格局部刷新的最终契约：文本框一次按键<b>不重渲染 ElTable 本身</b>（自己数 BuildRenderTree），
        /// 值仍然落到 EditingValue 并显示回输入框。
        /// <para>
        /// 此前用 <c>cut.RenderCount</c> 量到"每键 2 次"并记成"还有第二个来源未定位"，实测是口径问题：
        /// 同一次按键表格自身 BuildRenderTree 增 0，而 bUnit 计数增 2（= 单元格宿主 ElDynamicComponent + ElInput，
        /// 都在表格子树内）。对照组见 <see cref="BunitRenderCountAlsoCountsDescendantRenders"/>。
        /// </para>
        /// </summary>
        [Fact]
        public async Task TypingInTextEditorDoesNotRerenderTheTableItself()
        {
            var cut = RenderEditableTable<CountedTable>();
            var before = cut.Instance.Builds;

            // Find 与事件触发放在同一次调度批次里（分开会因中间重渲染拿到失效的 handler id）
            await cut.InvokeAsync(() => cut.Find("td input.el-input__inner").Input("头孢"));
            await cut.InvokeAsync(() => { });   // 排干调度队列

            Assert.Equal("头孢", Convert.ToString(HeaderOf(cut.Instance, nameof(Row.Name)).EditorRenderConfig.EditingValue));

            // 单元格子树确实重绘了（值回到 DOM），只是这一次不再连带整表
            Assert.Equal("头孢", cut.Find("td input.el-input__inner").GetAttribute("value"));

            // 计数器本身要有效：进入编辑态时表格确实重渲染过（否则这条断言会空转）
            Assert.True(before > 0, "进入编辑态后表格自身的渲染次数应大于 0，拿到 0 说明计数器没接上");

            // 绑定回调的接收者是单元格宿主（ElDynamicComponent）：整表零重渲染，浏览器实测每键 37ms → 23ms
            Assert.Equal(before, cut.Instance.Builds);

            // 计数器的正向对照：ElSelect 那一列仍走整表通知（EditingValue 变化 → 表格重渲染）。
            // 这样上面那条"零"不会是因为计数器看不见。
            var selectConfig = HeaderOf(cut.Instance, nameof(Row.Category)).EditorRenderConfig;
            await cut.InvokeAsync(() => selectConfig.EditingValue = "中药");
            await cut.InvokeAsync(() => { });
            Assert.True(cut.Instance.Builds > before,
                "选择类编辑器仍应让整表重渲染一次；没涨说明整表通知被改掉了");
        }

        /// <summary>
        /// 口径证据：一个只渲染单个 ElInput 的宿主，输入一次后 bUnit 的 RenderCount 增 2、
        /// 自身 BuildRenderTree 只增 1 —— 说明前者的计数包含子组件，不能用来证明某个组件没重渲染。
        /// </summary>
        [Fact]
        public void BunitRenderCountAlsoCountsDescendantRenders()
        {
            var cut = Render<ProbeWrapper>();
            var beforeCount = cut.RenderCount;
            var beforeBuilds = ProbeWrapper.OwnBuilds;

            cut.Find("input.el-input__inner").Input("x");

            Assert.Equal(1, ProbeWrapper.OwnBuilds - beforeBuilds);   // 宿主自己只渲染 1 次
            Assert.True(cut.RenderCount - beforeCount > 1,
                "bUnit 计数应把 ElInput 的重渲染也算进来；若哪天它变成 1，说明口径改了，"
                + "上面 TypingInTextEditorDoesNotRerenderTheTable 的注释要重新核对");
        }

        /// <summary>只有一个 ElInput 的宿主，用于量 bUnit 的计数口径。</summary>
        public class ProbeWrapper : ComponentBase
        {
            public static int OwnBuilds { get; private set; }

            public string Value { get; set; } = "abc";

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                OwnBuilds++;
                builder.OpenComponent<ElInput<string>>(0);
                builder.AddAttribute(1, nameof(ElInput<string>.Value), Value);
                builder.AddAttribute(2, nameof(ElInput<string>.ValueChanged),
                    EventCallback.Factory.Create(this, new Action<string>(v => Value = v)));
                builder.CloseComponent();
            }
        }

        /// <summary>
        /// 用户报的缺陷复现：表格里单元格初值为空时，点日历里的日期后「确定」必须解除禁用。
        /// 组件级同样的操作是好的（见 DateTimePickerEmptyValueTests），所以这条专门跑在表格上下文里。
        /// </summary>
        [Fact]
        public void DateTimeEditorInTableCellUnlocksConfirmAfterPickingDay()
        {
            var rows = Data();
            var cut = Render<ElTable>(p => p
                .Add(x => x.DataSource, rows)
                .Add(x => x.IsEditable, true)
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .AddCascadingValue("Page", (ElementComponentBase)new ElementDialogBase())
                .Add(x => x.ChildContent, builder =>
                {
                    builder.OpenComponent<ElTableColumn>(0);
                    builder.AddAttribute(1, nameof(ElTableColumn.Property), nameof(Row.CreatedAt));
                    builder.AddAttribute(2, nameof(ElTableColumn.Text), "上架时间");
                    builder.AddAttribute(3, nameof(ElTableColumn.IsEditable), true);
                    builder.AddAttribute(4, nameof(ElTableColumn.Editor), typeof(ElDateTimePicker));
                    builder.CloseComponent();
                }));

            cut.InvokeAsync(() => cut.Instance.BeginEditRowAsync(rows[0])).Wait();

            var editor = cut.Find(".el-table tbody tr td .el-date-editor");
            editor.Click();

            var dropdowns = Services.GetRequiredService<PopupService>().SelectDropDownOptions;
            var popup = dropdowns.Last();
            AngleSharp.Dom.IElement FindInPopup(string selector) => Render(popup.OptionContent).Find(selector);
            IReadOnlyList<AngleSharp.Dom.IElement> FindAllInPopup(string selector) => Render(popup.OptionContent).FindAll(selector);

            var confirm = FindInPopup(".el-date-time-picker__confirm");
            Assert.True(confirm.HasAttribute("disabled"), "空值起步时确定本应禁用");

            var day = FindAllInPopup(".el-date-table td.available")
                .First(td => td.QuerySelector("span")?.TextContent.Trim() == "15");
            day.Click();

            Assert.False(FindInPopup(".el-date-time-picker__confirm").HasAttribute("disabled"),
                "表格单元格里点日期后「确定」仍禁用 —— 复现用户报的确认不了");
        }
    }
}
