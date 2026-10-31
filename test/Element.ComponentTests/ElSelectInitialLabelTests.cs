using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 「预置 Value ＋ 声明式 ElOption 子项」的下拉首屏回显（<see cref="ElSelect{TValue}"/>）。
    /// <para>
    /// 缺陷原样：候选项挂在弹层里，而弹层内容只在第一次点开时才交给宿主
    /// （<c>ElSelect.ToggleDropDownAsync</c> 才把 <c>ChildContent</c> 塞进 <c>DropDownOption</c>），
    /// 所以首屏 <c>Options</c> 是空的，两条「拿 Options 算文本」的回填（OnParametersSet /
    /// OnAfterRenderAsync）都落空 ⇒ 输入框空白、顶着占位符，点开一次下拉才补上。
    /// 浏览器 A/B 实测（把扫描注掉＝改动前那一档）：/select 四台绑定 <c>value1="opt1"</c> 的单选
    /// 首屏 <c>input.value</c> 全为空串，/layout 两个下拉同样空；还原后 12/12 与 26/26 全绿。
    /// 探针 <c>demo/tools/probes/verify-select-preset-echo.cjs</c> 与
    /// <c>verify-layout-elselect-controls.cjs</c>；读数 <c>Temp/b15-select-echo-mutant.log</c>（OFF 档）、
    /// <c>Temp/b15-select-echo-restored.log</c> 与 <c>Temp/b15-layout-elselect-restored.log</c>（ON 档）。
    /// Element Plus 的下拉是 v-show、选项在 setup 体内就登记（<c>Temp/ep2140_select_select2.mjs:339</c>、
    /// <c>Temp/ep2140_select_option_script.mjs:30</c>），所以原生没有这一空窗；
    /// 口径与已知偏差见 <c>docs/select-alignment.md</c>。
    /// </para>
    /// <para>
    /// 修法不改弹层挂载时机（那会让全站每个下拉首屏就建出一整份候选项 DOM），
    /// 而是把 <c>ChildContent</c> 的帧只读扫一遍。所以下面每条都同时钉两件事：
    /// ① 文本对了；② 候选项 DOM <b>仍然不存在</b>（<c>.el-select-dropdown__item</c> 计数为 0）——
    /// 否则这条用例就退化成「其实是被顺手挂载出来的」。
    /// </para>
    /// <para>判据分两组。单选九条：命中选项文本；未命中的值不编造文本；没写 Text 时按 ElOption 自己的口径回落
    /// <c>Value.ToString()</c>；分组里的选项能钻进去；写在 div 里的选项也能找到；<c>@foreach</c>（Region 帧）
    /// 生成的选项也能找到；片段脱离真实上下文构造不出来时退回空白而不是崩页；显式 <c>Label</c> 参数优先于扫描。
    /// 多选六条（批次㊷ 补齐的另一半）：预置值的首屏胶囊显示选项汉字而不是裸值；按值回落（认不出的那条才显裸值，
    /// 不牵连整组）；分组 + <c>@foreach</c> 混排也认得；片段构造不出来时退回裸值不崩页；已注册的真选项优先于扫描；
    /// 没有子项可扫时行为与改动前逐字节一致（这条是多选组里的「改动前」档，用来证明上面的汉字确实来自扫帧）。</para>
    /// </summary>
    public class ElSelectInitialLabelTests : BunitContext
    {
        public ElSelectInitialLabelTests()
        {
            Services.AddElementServices();
        }

        private static RenderFragment PlainOptions() => builder =>
        {
            builder.OpenComponent<ElOption<string>>(0);
            builder.AddAttribute(1, nameof(ElOption<string>.Value), "alpha");
            builder.AddAttribute(2, nameof(ElOption<string>.Text), "Alpha");
            builder.CloseComponent();

            builder.OpenComponent<ElOption<string>>(3);
            builder.AddAttribute(4, nameof(ElOption<string>.Value), "beta");
            builder.AddAttribute(5, nameof(ElOption<string>.Text), "Beta");
            builder.CloseComponent();
        };

        private string TriggerText(IRenderedComponent<LabelHost> cut) =>
            cut.Find(".el-select input.el-input__inner").GetAttribute("value") ?? string.Empty;

        /// <summary>正向对照的另一半：候选项确实还没挂载（弹层没建），文本是扫出来的。</summary>
        private static void AssertOptionsStillUnmounted(IRenderedComponent<LabelHost> cut) =>
            Assert.Empty(cut.FindAll(".el-select-dropdown__item"));

        [Fact]
        public void PresetValueWithDeclarativeOptionsShowsOptionTextOnFirstRender()
        {
            var cut = Render<LabelHost>(parameters => parameters
                .Add(x => x.Value, "beta")
                .Add(x => x.ChildContent, PlainOptions()));

            Assert.Equal("Beta", TriggerText(cut));
            AssertOptionsStillUnmounted(cut);
        }

        [Fact]
        public void UnknownPresetValueLeavesTriggerEmptyInsteadOfInventingText()
        {
            var cut = Render<LabelHost>(parameters => parameters
                .Add(x => x.Value, "zeta")
                .Add(x => x.ChildContent, PlainOptions()));

            Assert.Equal(string.Empty, TriggerText(cut));
            AssertOptionsStillUnmounted(cut);
        }

        [Fact]
        public void OptionWithoutTextFallsBackToValueStringLikeElOptionItself()
        {
            var cut = Render<LabelHost>(parameters => parameters
                .Add(x => x.Value, "beta")
                .Add(x => x.ChildContent, (RenderFragment)(builder =>
                {
                    builder.OpenComponent<ElOption<string>>(0);
                    builder.AddAttribute(1, nameof(ElOption<string>.Value), "beta");
                    builder.CloseComponent();
                })));

            Assert.Equal("beta", TriggerText(cut));
            AssertOptionsStillUnmounted(cut);
        }

        [Fact]
        public void OptionsNestedInsideOptionGroupAreFound()
        {
            var cut = Render<LabelHost>(parameters => parameters
                .Add(x => x.Value, "ui")
                .Add(x => x.ChildContent, (RenderFragment)(builder =>
                {
                    builder.OpenComponent<ElOptionGroup<string>>(0);
                    builder.AddAttribute(1, nameof(ElOptionGroup<string>.Label), "Frontend");
                    builder.AddAttribute(2, nameof(ElOptionGroup<string>.ChildContent), (RenderFragment)(group =>
                    {
                        group.OpenComponent<ElOption<string>>(0);
                        group.AddAttribute(1, nameof(ElOption<string>.Value), "ui");
                        group.AddAttribute(2, nameof(ElOption<string>.Text), "UI");
                        group.CloseComponent();
                    }));
                    builder.CloseComponent();
                })));

            Assert.Equal("UI", TriggerText(cut));
            AssertOptionsStillUnmounted(cut);
        }

        [Fact]
        public void OptionsWrappedInOrdinaryElementAreFound()
        {
            var cut = Render<LabelHost>(parameters => parameters
                .Add(x => x.Value, "alpha")
                .Add(x => x.ChildContent, (RenderFragment)(builder =>
                {
                    builder.OpenElement(0, "div");
                    builder.AddContent(1, PlainOptions());
                    builder.CloseElement();
                })));

            Assert.Equal("Alpha", TriggerText(cut));
            AssertOptionsStillUnmounted(cut);
        }

        [Fact]
        public void ExplicitLabelParameterWinsOverTheScan()
        {
            var cut = Render<LabelHost>(parameters => parameters
                .Add(x => x.Value, "beta")
                .Add(x => x.Label, "自定义标题")
                .Add(x => x.ChildContent, PlainOptions()));

            Assert.Equal("自定义标题", TriggerText(cut));
            AssertOptionsStillUnmounted(cut);
        }

        [Fact]
        public void MultipleSelectWithPresetValuesShowsOptionTextInTagsOnFirstRender()
        {
            var cut = Render<LabelHost>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.Values, new List<string> { "beta" })
                .Add(x => x.ChildContent, PlainOptions()));

            // 这批的变更点：多选预置值的首屏标签此前显示裸值 "beta" —— 洞和单选那条同源
            // （候选项只在弹层里注册，SyncSelectedOptionsFromValues 无从取值只能 Convert.ToString(value)），
            // 业务上的表现是「生产状态」这类 int 字典的胶囊显示 0 / 1 / 2 而不是汉字。
            var tag = Assert.Single(cut.FindAll(".el-select__tags .el-tag"));
            Assert.Contains("Beta", tag.TextContent);
            AssertOptionsStillUnmounted(cut);

            // 边界不变：扫描只喂标签文本，不把单选的取值文本塞进触发框
            // （塞了会变成「框里一行 Beta、外面一堆胶囊」两套显示）。
            Assert.Equal(string.Empty, TriggerText(cut));
        }

        [Fact]
        public void MultipleSelectWithUnknownPresetValueFallsBackPerValueNotForWholeSet()
        {
            var cut = Render<LabelHost>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.Values, new List<string> { "beta", "zeta" })
                .Add(x => x.ChildContent, PlainOptions()));

            // 回落是「按值」而不是「整组」：认得出的那条要拿到文本，认不出的那条照旧裸值，
            // 不能因为有一条没命中就把已经命中的也拖回裸值。
            var tags = cut.FindAll(".el-select__tags .el-tag").ToArray();
            Assert.Equal(2, tags.Length);
            Assert.Contains("Beta", tags[0].TextContent);
            Assert.Contains("zeta", tags[1].TextContent);
            AssertOptionsStillUnmounted(cut);
        }

        [Fact]
        public void MultipleSelectFindsOptionsNestedInGroupsAndForeachRegions()
        {
            var cities = new[] { "hz", "sz" };
            var texts = new[] { "杭州", "深圳" };
            var cut = Render<LabelHost>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.Values, new List<string> { "ui", "sz" })
                .Add(x => x.ChildContent, (RenderFragment)(builder =>
                {
                    builder.OpenComponent<ElOptionGroup<string>>(0);
                    builder.AddAttribute(1, nameof(ElOptionGroup<string>.Label), "Frontend");
                    builder.AddAttribute(2, nameof(ElOptionGroup<string>.ChildContent), (RenderFragment)(group =>
                    {
                        group.OpenComponent<ElOption<string>>(0);
                        group.AddAttribute(1, nameof(ElOption<string>.Value), "ui");
                        group.AddAttribute(2, nameof(ElOption<string>.Text), "UI");
                        group.CloseComponent();
                    }));
                    builder.CloseComponent();

                    builder.OpenRegion(3);
                    for (var i = 0; i < cities.Length; i++)
                    {
                        builder.OpenComponent<ElOption<string>>(i * 3 + 4);
                        builder.AddAttribute(i * 3 + 5, nameof(ElOption<string>.Value), cities[i]);
                        builder.AddAttribute(i * 3 + 6, nameof(ElOption<string>.Text), texts[i]);
                        builder.CloseComponent();
                    }
                    builder.CloseRegion();
                })));

            var tags = cut.FindAll(".el-select__tags .el-tag").ToArray();
            Assert.Equal(2, tags.Length);
            Assert.Contains("UI", tags[0].TextContent);
            Assert.Contains("深圳", tags[1].TextContent);
            AssertOptionsStillUnmounted(cut);
        }

        [Fact]
        public void MultipleSelectWithUnbuildableChildContentFallsBackToRawValuesWithoutCrashing()
        {
            var cut = Render<LabelHost>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.Values, new List<string> { "beta" })
                .Add(x => x.ChildContent, (RenderFragment)(builder =>
                {
                    throw new System.InvalidOperationException("夹具：模拟脱离上下文构造不出来的片段");
                })));

            // 与单选那条同一份契约：扫不出来就退回改动前的行为（多选＝裸值胶囊），不许把页面带崩。
            var tag = Assert.Single(cut.FindAll(".el-select__tags .el-tag"));
            Assert.Contains("beta", tag.TextContent);
            Assert.Equal(string.Empty, TriggerText(cut));
            AssertOptionsStillUnmounted(cut);
        }

        [Fact]
        public void MultipleSelectPrefersRegisteredOptionsOverTheScan()
        {
            var cut = Render<LabelHost>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.Values, new List<string> { "beta" })
                .Add(x => x.ChildContent, PlainOptions()));
            var select = cut.FindComponent<ElSelect<string>>().Instance;

            // 点开下拉后候选项会真注册（这里直接登记一颗，文本与扫帧结果刻意不同）：
            // 此时以真数据为准，扫描必须让位 —— 否则「Text 是动态算出来的」那类选项会一直显示扫到的旧文本。
            // 判据取状态而不是 DOM：登记发生在渲染之后，胶囊文本要等下一次渲染才刷新，
            // 而 Label / SelectedOptions 正是那次渲染要读的两个源。
            cut.InvokeAsync(() => select.RegisterOption(new ElementPlus.Model.SelectResultModel<string>
            {
                Key = "beta",
                Text = "登记文本"
            }));

            Assert.Equal("登记文本", select.Label);
            var selected = Assert.Single(select.SelectedOptions);
            Assert.Equal("登记文本", selected.Text);
        }

        [Fact]
        public void MultipleSelectWithoutOptionsKeepsTheRawValueFallback()
        {
            var cut = Render<LabelHost>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.Values, new List<string> { "beta" }));

            // 正向对照的另一半：没有子项可扫时行为与改动前逐字节一致（裸值胶囊），
            // 证明上面几条的 "Beta" 确实来自扫帧而不是别处冒出来的。
            var tag = Assert.Single(cut.FindAll(".el-select__tags .el-tag"));
            Assert.Contains("beta", tag.TextContent);
        }

        [Fact]
        public void OptionsEmittedByForeachRegionAreFound()
        {
            // 演示站与业务代码最常见的写法其实是 @foreach 生成选项（/select 的城市下拉、/form 的下拉都是），
            // Razor 会把 foreach 编成 Region 帧，和 Element 包一层是两条不同的递归路径，必须分开钉。
            var cities = new[] { "hz", "sz" };
            var texts = new[] { "杭州", "深圳" };
            var cut = Render<LabelHost>(parameters => parameters
                .Add(x => x.Value, "sz")
                .Add(x => x.ChildContent, (RenderFragment)(builder =>
                {
                    builder.OpenRegion(0);
                    for (var i = 0; i < cities.Length; i++)
                    {
                        builder.OpenComponent<ElOption<string>>(i * 3 + 1);
                        builder.AddAttribute(i * 3 + 2, nameof(ElOption<string>.Value), cities[i]);
                        builder.AddAttribute(i * 3 + 3, nameof(ElOption<string>.Text), texts[i]);
                        builder.CloseComponent();
                    }
                    builder.CloseRegion();
                })));

            Assert.Equal("深圳", TriggerText(cut));
            AssertOptionsStillUnmounted(cut);
        }

        [Fact]
        public void ChildContentThatCannotBeBuiltOutsideItsRealContextFallsBackToEmpty()
        {
            // 扫描是「脱离弹层、拿临时 builder 跑一遍片段」。片段里写了只有真实渲染上下文才成立的东西时
            // 会抛异常 —— 那必须退回改动前的行为（空白 + 占位符），而不是把整页带崩。
            var cut = Render<LabelHost>(parameters => parameters
                .Add(x => x.Value, "beta")
                .Add(x => x.ChildContent, (RenderFragment)(builder =>
                {
                    throw new System.InvalidOperationException("夹具：模拟脱离上下文构造不出来的片段");
                })));

            Assert.Equal(string.Empty, TriggerText(cut));
            AssertOptionsStillUnmounted(cut);
        }

        private class LabelHost : ComponentBase
        {
            [Parameter]
            public string Value { get; set; }

            [Parameter]
            public string Label { get; set; }

            [Parameter]
            public bool Multiple { get; set; }

            [Parameter]
            public IList<string> Values { get; set; } = new List<string>();

            [Parameter]
            public RenderFragment ChildContent { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<ElSelect<string>>(0);
                builder.AddAttribute(1, nameof(ElSelect<string>.Value), Value);
                builder.AddAttribute(2, nameof(ElSelect<string>.Label), Label);
                builder.AddAttribute(3, nameof(ElSelect<string>.Multiple), Multiple);
                builder.AddAttribute(4, nameof(ElSelect<string>.Values), Values);
                builder.AddAttribute(5, nameof(ElSelect<string>.ChildContent), ChildContent);
                builder.CloseComponent();
            }
        }
    }
}
