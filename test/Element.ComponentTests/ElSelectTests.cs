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
    public class ElSelectTests : BunitContext
    {
        public ElSelectTests()
        {
            Services.AddElementPlusServices();
        }

        [Fact]
        public async System.Threading.Tasks.Task MultipleSelectRendersTagsAndUpdatesValues()
        {
            IList<string> values = new List<string>();
            var cut = Render<SelectHost>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.Values, values)
                .Add(x => x.ValuesChanged, next => values = (IList<string>)next));

            cut.Find(".el-select").Click();
            var dropdown = GetDropdown();
            var fragment = Render(builder =>
            {
                builder.OpenComponent<CascadingValue<DropDownOption>>(0);
                builder.AddAttribute(1, "Value", dropdown);
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(content => content.AddContent(0, dropdown.OptionContent)));
                builder.CloseComponent();
            });
            Assert.Equal(2, fragment.FindAll(".el-select-dropdown__item").Count);

            await cut.InvokeAsync(() => ((ElSelect<string>)dropdown.Select).OnInternalSelectAsync(((ElSelect<string>)dropdown.Select).Options[0]));
            await cut.InvokeAsync(() => ((ElSelect<string>)dropdown.Select).OnInternalSelectAsync(((ElSelect<string>)dropdown.Select).Options[1]));

            Assert.Equal(new[] { "alpha", "beta" }, values);
            Assert.Equal(2, cut.FindAll(".el-select__tags .el-tag").Count);
        }

        /// <summary>
        /// 标签胶囊必须是 <c>.el-select__tags</c> 的「直接子元素」，且文字在 <c>.el-select__tags-text</c> 里。
        /// <para>
        /// EP 编译段带着 <c>.el-select .el-select__tags&gt;span{display:contents}</c>，它本意是给 EP 那种
        /// 「标签外面再套一层 span」的结构；本库把 <c>.el-tag</c> 直接渲染成那个 span，规则就会命中胶囊本身、
        /// 把盒子取消（浏览器实测 computed display:contents、宽高全 0，文字与关闭图标散成松散 flex 项，
        /// 就是「选项 B 被遮挡一部分」的样子）。index.css 用更高特异性的
        /// <c>.el-select .el-select__tags &gt; .el-tag{display:inline-flex}</c> 兜住。
        /// 这条用例锁住 DOM 形状：将来若把结构改成 EP 的外包 span，就该同时删掉那条兜底规则，
        /// 否则两边都改、形状漂移一次，胶囊就又没了。
        /// 几何本身 bUnit 量不到，由 demo/tools/probes/diag-batch9-select.cjs 在浏览器里量 computed 值与矩形。
        /// </para>
        /// </summary>
        [Fact]
        public async System.Threading.Tasks.Task TagsAreDirectChildrenWithTextSpan()
        {
            IList<string> values = new List<string>();
            var cut = Render<SelectHost>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.Values, values)
                .Add(x => x.ValuesChanged, next => values = (IList<string>)next));

            cut.Find(".el-select").Click();
            var dropdown = GetDropdown();
            var fragment = Render(builder =>
            {
                builder.OpenComponent<CascadingValue<DropDownOption>>(0);
                builder.AddAttribute(1, "Value", dropdown);
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(content => content.AddContent(0, dropdown.OptionContent)));
                builder.CloseComponent();
            });
            Assert.Equal(2, fragment.FindAll(".el-select-dropdown__item").Count);

            await cut.InvokeAsync(() => ((ElSelect<string>)dropdown.Select).OnInternalSelectAsync(((ElSelect<string>)dropdown.Select).Options[0]));

            var pills = cut.FindAll(".el-select__tags > .el-tag");
            Assert.Single(pills);
            Assert.Equal("Alpha", pills[0].QuerySelector(".el-select__tags-text")?.TextContent);
            // 直接子元素而不是外包一层：命中 EP 那条 display:contents 的正是这个形状
            Assert.Equal("SPAN", pills[0].TagName);
            Assert.Empty(cut.FindAll(".el-select__tags > span:not(.el-tag)"));
        }

        [Fact]
        public async System.Threading.Tasks.Task MultipleLimitEnforced()
        {
            IList<string> values = new List<string>();
            var cut = Render<SelectHost>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.MultipleLimit, 1)
                .Add(x => x.Values, values)
                .Add(x => x.ValuesChanged, next => values = (IList<string>)next));

            cut.Find(".el-select").Click();
            var dropdown = GetDropdown();
            var fragment = Render(builder =>
            {
                builder.OpenComponent<CascadingValue<DropDownOption>>(0);
                builder.AddAttribute(1, "Value", dropdown);
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(content => content.AddContent(0, dropdown.OptionContent)));
                builder.CloseComponent();
            });
            Assert.Equal(2, fragment.FindAll(".el-select-dropdown__item").Count);

            await cut.InvokeAsync(() => ((ElSelect<string>)dropdown.Select).OnInternalSelectAsync(((ElSelect<string>)dropdown.Select).Options[0]));
            await cut.InvokeAsync(() => ((ElSelect<string>)dropdown.Select).OnInternalSelectAsync(((ElSelect<string>)dropdown.Select).Options[1]));

            Assert.Equal(new[] { "alpha" }, values);
            Assert.Single(cut.FindAll(".el-select__tags .el-tag"));
        }

        [Fact]
        public void NameAttributeRenderedOnRoot()
        {
            var cut = Render<SelectHost>(parameters => parameters
                .Add(x => x.Name, "city"));

            Assert.Equal("city", cut.Find(".el-select").GetAttribute("name"));
        }

        [Fact]
        public async System.Threading.Tasks.Task RemoveTagFiresOnRemoveTag()
        {
            IList<string> values = new List<string> { "alpha" };
            string removed = null;
            var cut = Render<SelectHost>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.Values, values)
                .Add(x => x.ValuesChanged, next => values = (IList<string>)next)
                .Add(x => x.OnRemoveTag, (string v) => removed = v));

            var closeIcon = cut.Find(".el-select__tags .el-tag__close");
            await cut.InvokeAsync(() => closeIcon.Click());

            Assert.Equal("alpha", removed);
            Assert.Empty(values);
        }

        [Fact]
        public void FilterableSelectFiltersVisibleOptions()
        {
            var cut = Render<SelectHost>(parameters => parameters.Add(x => x.Filterable, true));

            cut.Find(".el-select").Click();
            var dropdown = GetDropdown();
            Render(builder =>
            {
                builder.OpenComponent<CascadingValue<DropDownOption>>(0);
                builder.AddAttribute(1, "Value", dropdown);
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(content => content.AddContent(0, dropdown.OptionContent)));
                builder.CloseComponent();
            });
            cut.Find("input").Input("Be");

            var select = (ElSelect<string>)dropdown.Select;
            cut.WaitForAssertion(() =>
            {
                Assert.False(select.IsOptionVisible(select.Options[0]));
                Assert.True(select.IsOptionVisible(select.Options[1]));
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public void FilterableSelectHidesNonMatchingItemsInDom()
        {
            // 回归：输入关键字后下拉里的选项必须真正被 display:none（而非只改 IsOptionVisible 逻辑），
            // 否则「输入关键字选项不收缩」。验证 FilterChanged 事件驱动 ElOption 重渲染。
            var cut = Render<SelectHost>(parameters => parameters.Add(x => x.Filterable, true));

            cut.Find(".el-select").Click();
            var dropdown = GetDropdown();
            var fragment = RenderDropdown(dropdown);

            Assert.Equal(2, fragment.FindAll(".el-select-dropdown__item").Count);

            cut.Find("input").Input("Be");

            cut.WaitForAssertion(() =>
            {
                var items = fragment.FindAll(".el-select-dropdown__item");
                var alpha = items.First(i => i.TextContent.Trim() == "Alpha");
                var beta = items.First(i => i.TextContent.Trim() == "Beta");
                // Alpha 不含 "Be" → 隐藏；Beta 含 "Be" → 可见
                Assert.Contains("display:none", (alpha.GetAttribute("style") ?? "").ToLowerInvariant());
                Assert.DoesNotContain("display:none", (beta.GetAttribute("style") ?? "").ToLowerInvariant());
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public void DropdownCarriesHeaderAndFooterContent()
        {
            // 验证 HeaderContent / FooterContent 参数从 ElSelect 流向 DropDownOption（实际渲染由 ElPopup 完成，E2E 已覆盖）。
            RenderFragment header = builder => { builder.OpenElement(0, "div"); builder.AddContent(1, "城市列表"); builder.CloseElement(); };
            RenderFragment footer = builder => { builder.OpenElement(0, "span"); builder.AddContent(1, "共 1 项"); builder.CloseElement(); };
            var cut = Render<ElSelect<string>>(parameters => parameters
                .Add(x => x.ChildContent, (RenderFragment)(content =>
                {
                    content.OpenComponent<ElOption<string>>(0);
                    content.AddAttribute(1, nameof(ElOption<string>.Value), "a");
                    content.AddAttribute(2, nameof(ElOption<string>.Text), "Apple");
                    content.CloseComponent();
                }))
                .Add(x => x.HeaderContent, header)
                .Add(x => x.FooterContent, footer));

            cut.Find(".el-select").Click();
            var dropdown = GetDropdown();

            Assert.NotNull(dropdown.HeaderContent);
            Assert.NotNull(dropdown.FooterContent);
        }

        [Fact]
        public void RemoteSelectInvokesRemoteMethod()
        {
            var filters = new List<string>();
            var cut = Render<SelectHost>(parameters => parameters
                .Add(x => x.Filterable, true)
                .Add(x => x.Remote, true)
                .Add(x => x.RemoteMethod, query => filters.Add(query))
                .Add(x => x.ChildContent, (RenderFragment)(builder => { })));

            cut.Find(".el-select").Click();
            cut.Find("input").Input("remote");

            Assert.Equal(new[] { "remote" }, filters);
        }

        [Fact]
        public void RemoteSelectDoesNotHideExistingOptionsWhileSearching()
        {
            var cut = Render<SelectHost>(parameters => parameters
                .Add(x => x.Filterable, true)
                .Add(x => x.Remote, true));

            cut.Find(".el-select").Click();
            var dropdown = GetDropdown();
            RenderDropdown(dropdown);

            cut.Find("input").Input("missing");
            var select = (ElSelect<string>)dropdown.Select;

            Assert.All(select.Options, option => Assert.True(select.IsOptionVisible(option)));
            Assert.Equal("true", cut.Find(".el-select").GetAttribute("aria-expanded"));
            Assert.Equal("false", cut.Find(".el-select").GetAttribute("aria-disabled"));
            Assert.Equal("false", cut.Find(".el-select").GetAttribute("aria-busy"));
            Assert.Equal("false", cut.Find(".el-select").GetAttribute("aria-multiselectable"));
        }

        [Fact]
        public async System.Threading.Tasks.Task OptionGroupsRenderHeadersAndPreserveSelection()
        {
            string value = null;
            var cut = Render<ElSelect<string>>(parameters => parameters
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.ChildContent, (RenderFragment)(content =>
                {
                    content.OpenComponent<ElOptionGroup<string>>(0);
                    content.AddAttribute(1, nameof(ElOptionGroup<string>.Label), "Backend");
                    content.AddAttribute(2, nameof(ElOptionGroup<string>.ChildContent), (RenderFragment)(group =>
                    {
                        group.OpenComponent<ElOption<string>>(0);
                        group.AddAttribute(1, nameof(ElOption<string>.Value), "api");
                        group.AddAttribute(2, nameof(ElOption<string>.Text), "API");
                        group.CloseComponent();
                    }));
                    content.CloseComponent();

                    content.OpenComponent<ElOptionGroup<string>>(3);
                    content.AddAttribute(4, nameof(ElOptionGroup<string>.Label), "Frontend");
                    content.AddAttribute(5, nameof(ElOptionGroup<string>.ChildContent), (RenderFragment)(group =>
                    {
                        group.OpenComponent<ElOption<string>>(0);
                        group.AddAttribute(1, nameof(ElOption<string>.Value), "ui");
                        group.AddAttribute(2, nameof(ElOption<string>.Text), "UI");
                        group.CloseComponent();
                    }));
                    content.CloseComponent();
                })));

            cut.Find(".el-select").Click();
            var dropdown = GetDropdown();
            var fragment = Render(builder =>
            {
                builder.OpenComponent<CascadingValue<DropDownOption>>(0);
                builder.AddAttribute(1, "Value", dropdown);
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(content => content.AddContent(0, dropdown.OptionContent)));
                builder.CloseComponent();
            });

            var titles = fragment.FindAll(".el-select-group__title");
            Assert.Equal(new[] { "Backend", "Frontend" }, new[] { titles[0].TextContent.Trim(), titles[1].TextContent.Trim() });

            await cut.InvokeAsync(() => ((ElSelect<string>)dropdown.Select).OnInternalSelectAsync(((ElSelect<string>)dropdown.Select).Options.Last()));
            Assert.Equal("ui", value);
        }

        [Fact]
        public void OptionGroupKeepsStructureAndHidesFilteredChildren()
        {
            var cut = Render<GroupedSelectHost>();

            cut.Find(".el-select").Click();
            var dropdown = GetDropdown();
            var fragment = RenderDropdown(dropdown);

            Assert.Equal(2, fragment.FindAll(".el-select-group__title").Count);

            cut.Find("input").Input("AP");
            var select = (ElSelect<string>)dropdown.Select;

            var titles = fragment.FindAll(".el-select-group__title");

            Assert.Equal(new[] { "Backend", "Frontend" }, new[] { titles[0].TextContent.Trim(), titles[1].TextContent.Trim() });
            Assert.True(select.IsOptionVisible(select.Options[0]));
            Assert.False(select.IsOptionVisible(select.Options[1]));
        }

        [Fact]
        public void DisabledOptionGroupDisablesChildOptions()
        {
            string value = null;
            var cut = Render<ElSelect<string>>(parameters => parameters
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.ChildContent, (RenderFragment)(content =>
                {
                    content.OpenComponent<ElOptionGroup<string>>(0);
                    content.AddAttribute(1, nameof(ElOptionGroup<string>.Label), "Locked");
                    content.AddAttribute(2, nameof(ElOptionGroup<string>.Disabled), true);
                    content.AddAttribute(3, nameof(ElOptionGroup<string>.ChildContent), (RenderFragment)(group =>
                    {
                        group.OpenComponent<ElOption<string>>(0);
                        group.AddAttribute(1, nameof(ElOption<string>.Value), "locked");
                        group.AddAttribute(2, nameof(ElOption<string>.Text), "Locked option");
                        group.CloseComponent();
                    }));
                    content.CloseComponent();
                })));

            cut.Find(".el-select").Click();
            var dropdown = GetDropdown();
            var fragment = RenderDropdown(dropdown);
            var option = fragment.Find(".el-select-dropdown__item");

            Assert.Contains("is-disabled", option.ClassList);
            Assert.Equal("true", option.GetAttribute("aria-disabled"));
            Assert.Equal("-1", option.GetAttribute("tabindex"));

            option.Click();

            Assert.Null(value);
        }

        /// <summary>
        /// 批次 80：<c>DropDownWidth</c> 大于 0 时，面板宽度交给宿主说了算，并且必须把
        /// <c>FitInputWidth</c> 与 <c>AutoWidth</c> 一起关掉 —— <c>ElPopup.RenderDropDownAsync</c> 里
        /// <c>if ((autoWidth &amp;&amp; option.AutoWidth) || option.FitInputWidth) option.WidthPx = rect.Width;</c>
        /// 会用触发器的 rect 覆盖面板宽，只设 Width 不设这两个开关等于没设。
        /// 面板真实几何（宽度、选项是否被裁）bUnit 量不到，由 zyweb 侧浏览器探针量。
        /// </summary>
        [Fact]
        public void DropDownWidthOverridesInputFitting()
        {
            var cut = Render<SelectHost>(parameters => parameters.Add(x => x.DropDownWidth, 200));

            cut.Find(".el-select").Click();

            var dropdown = GetDropdown();
            Assert.Equal(200, dropdown.Width);
            Assert.False(dropdown.FitInputWidth);
            Assert.False(dropdown.AutoWidth);
        }

        /// <summary>
        /// 正向对照：不填 DropDownWidth 时保持既有口径（面板跟随控件宽）。
        /// </summary>
        [Fact]
        public void DropDownWidthNotSet_KeepsFitInputWidth()
        {
            var cut = Render<SelectHost>();

            cut.Find(".el-select").Click();

            var dropdown = GetDropdown();
            Assert.Equal(0, dropdown.Width);
            Assert.True(dropdown.FitInputWidth);
            Assert.True(dropdown.AutoWidth);
        }

        private DropDownOption GetDropdown()
        {
            return Services.GetRequiredService<PopupService>().SelectDropDownOptions.Single();
        }

        private IRenderedComponent<CascadingValue<DropDownOption>> RenderDropdown(DropDownOption dropdown)
        {
            return Render<CascadingValue<DropDownOption>>(parameters => parameters
                .Add(x => x.Value, dropdown)
                .Add(x => x.ChildContent, (RenderFragment)(content => content.AddContent(0, dropdown.OptionContent))));
        }

        /// <summary>
        /// <c>Style</c> 必须落在 <c>.el-select</c> 根节点。EP 的 <c>.el-select</c> 宽度取自
        /// <c>--el-select-width</c>（默认 100%），缺这一句时消费端写的 <c>Style="width:130px"</c>
        /// 会静默失效，在 flex 行里被拉成一整行宽。
        /// </summary>
        [Fact]
        public void InlineStyleLandsOnRoot()
        {
            var cut = Render<SelectHost>(parameters => parameters
                .Add(x => x.Style, "width:130px;"));

            var root = cut.Find(".el-select");

            Assert.Equal("width:130px;", root.GetAttribute("style"));
        }

        private class SelectHost : ComponentBase
        {
            [Parameter]
            public bool Multiple { get; set; }

            [Parameter]
            public bool Filterable { get; set; }

            [Parameter]
            public bool Remote { get; set; }

            [Parameter]
            public int MultipleLimit { get; set; }

            [Parameter]
            public IList<string> Values { get; set; } = new List<string>();

            [Parameter]
            public EventCallback<ICollection<string>> ValuesChanged { get; set; }

            [Parameter]
            public EventCallback<string> OnRemoveTag { get; set; }

            [Parameter]
            public EventCallback<string> RemoteMethod { get; set; }

            [Parameter]
            public string Name { get; set; }

            [Parameter]
            public string Style { get; set; }

            [Parameter]
            public int DropDownWidth { get; set; }

            [Parameter]
            public RenderFragment ChildContent { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<ElSelect<string>>(0);
                builder.AddAttribute(1, nameof(ElSelect<string>.Multiple), Multiple);
                builder.AddAttribute(2, nameof(ElSelect<string>.Filterable), Filterable);
                builder.AddAttribute(3, nameof(ElSelect<string>.Remote), Remote);
                builder.AddAttribute(4, nameof(ElSelect<string>.MultipleLimit), MultipleLimit);
                builder.AddAttribute(5, nameof(ElSelect<string>.Values), Values);
                builder.AddAttribute(6, nameof(ElSelect<string>.ValuesChanged), ValuesChanged);
                builder.AddAttribute(7, nameof(ElSelect<string>.OnRemoveTag), OnRemoveTag);
                builder.AddAttribute(8, nameof(ElSelect<string>.RemoteMethod), RemoteMethod);
                builder.AddAttribute(9, nameof(ElSelect<string>.Name), Name);
                builder.AddAttribute(10, nameof(ElSelect<string>.Style), Style);
                builder.AddAttribute(11, nameof(ElSelect<string>.DropDownWidth), DropDownWidth);
                builder.AddAttribute(7, nameof(ElSelect<string>.ChildContent), ChildContent ?? (RenderFragment)(content =>
                {
                    content.OpenComponent<ElOption<string>>(0);
                    content.AddAttribute(1, nameof(ElOption<string>.Value), "alpha");
                    content.AddAttribute(2, nameof(ElOption<string>.Text), "Alpha");
                    content.CloseComponent();

                    content.OpenComponent<ElOption<string>>(3);
                    content.AddAttribute(4, nameof(ElOption<string>.Value), "beta");
                    content.AddAttribute(5, nameof(ElOption<string>.Text), "Beta");
                    content.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }

        private class GroupedSelectHost : ComponentBase
        {
            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<ElSelect<string>>(0);
                builder.AddAttribute(1, nameof(ElSelect<string>.Filterable), true);
                builder.AddAttribute(2, nameof(ElSelect<string>.ChildContent), (RenderFragment)(content =>
                {
                    content.OpenComponent<ElOptionGroup<string>>(0);
                    content.AddAttribute(1, nameof(ElOptionGroup<string>.Label), "Backend");
                    content.AddAttribute(2, nameof(ElOptionGroup<string>.ChildContent), (RenderFragment)(group =>
                    {
                        group.OpenComponent<ElOption<string>>(0);
                        group.AddAttribute(1, nameof(ElOption<string>.Value), "api");
                        group.AddAttribute(2, nameof(ElOption<string>.Text), "API");
                        group.CloseComponent();
                    }));
                    content.CloseComponent();

                    content.OpenComponent<ElOptionGroup<string>>(3);
                    content.AddAttribute(4, nameof(ElOptionGroup<string>.Label), "Frontend");
                    content.AddAttribute(5, nameof(ElOptionGroup<string>.ChildContent), (RenderFragment)(group =>
                    {
                        group.OpenComponent<ElOption<string>>(0);
                        group.AddAttribute(1, nameof(ElOption<string>.Value), "ui");
                        group.AddAttribute(2, nameof(ElOption<string>.Text), "UI");
                        group.CloseComponent();
                    }));
                    content.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }
    }
}
