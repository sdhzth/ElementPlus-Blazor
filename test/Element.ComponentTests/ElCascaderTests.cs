using Bunit;
using ElementPlus;
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
    public class ElCascaderTests : BunitContext
    {
        public ElCascaderTests()
        {
            Services.AddElementServices();
        }

        [Fact]
        public async Task MultipleSelectionTracksSelectedPaths()
        {
            IList<IList<string>> selected = new List<IList<string>>();
            var cut = Render<ElCascader>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.Options, BuildOptions())
                .Add(x => x.ValuesChanged, paths => selected = paths));

            await OpenAsync(cut);
            var dropdown = GetDropdown();
            Render(dropdown.OptionContent).FindAll("li")[0].Click();
            Render(dropdown.OptionContent).FindAll("li")[1].Click();

            Assert.Collection(selected,
                path => Assert.Equal(new[] { "guide", "install" }, path));
        }

        [Fact]
        public async Task LazyLoadAddsChildrenWhenNodeExpands()
        {
            var root = new CascaderOption { Label = "Remote", Value = "remote" };
            var loadCount = 0;
            var cut = Render<ElCascader>(parameters => parameters
                .Add(x => x.Lazy, true)
                .Add(x => x.Options, new[] { root })
                .Add(x => x.LazyLoad, option =>
                {
                    loadCount++;
                    return Task.FromResult<IEnumerable<CascaderOption>>(new[]
                    {
                        new CascaderOption { Label = "Loaded", Value = "loaded", Leaf = true }
                    });
                }));

            await OpenAsync(cut);
            var dropdown = GetDropdown();
            Render(dropdown.OptionContent).Find("li").Click();

            cut.WaitForAssertion(() =>
            {
                Assert.Equal(1, loadCount);
                Assert.Single(root.Children);
                Assert.Equal("loaded", root.Children[0].Value);
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public async Task FilterableRendersSuggestionTemplate()
        {
            RenderFragment<CascaderSuggestion> template = suggestion => builder =>
            {
                builder.OpenElement(0, "strong");
                builder.AddAttribute(1, "class", "custom-cascader-suggestion");
                builder.AddContent(2, suggestion.Text);
                builder.CloseElement();
            };
            var cut = Render<ElCascader>(parameters => parameters
                .Add(x => x.Filterable, true)
                .Add(x => x.Options, BuildOptions())
                .Add(x => x.SuggestionItemTemplate, template)
                .Add(x => x.Debounce, 0));

            await OpenAsync(cut);
            cut.Find("input").Input("Install");

            var fragment = Render(GetDropdown().OptionContent);
            Assert.Contains("custom-cascader-suggestion", fragment.Markup);
            Assert.Contains("Guide / Install", fragment.Markup);
        }

        [Fact]
        public async Task RendersNodeTemplate()
        {
            RenderFragment<CascaderOption> template = option => builder =>
            {
                builder.OpenElement(0, "strong");
                builder.AddAttribute(1, "class", "custom-cascader-node");
                builder.AddContent(2, option.Label);
                builder.CloseElement();
            };
            var cut = Render<ElCascader>(parameters => parameters
                .Add(x => x.Options, BuildOptions())
                .Add(x => x.NodeTemplate, template));

            await OpenAsync(cut);
            var fragment = Render(GetDropdown().OptionContent);

            Assert.Contains("custom-cascader-node", fragment.Markup);
            Assert.Contains("Guide", fragment.Markup);
        }

        private async Task OpenAsync(IRenderedComponent<ElCascader> cut)
        {
            await cut.InvokeAsync(() => cut.Find(".el-cascader").Click());
        }

        [Fact]
        public async Task ClearAsyncClearsSelectedValues()
        {
            IList<IList<string>> selected = new List<IList<string>>();
            var cut = Render<ElCascader>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.Options, BuildOptions())
                .Add(x => x.ValuesChanged, paths => selected = paths));

            await OpenAsync(cut);
            var dropdown = GetDropdown();
            Render(dropdown.OptionContent).FindAll("li")[0].Click();
            Render(dropdown.OptionContent).FindAll("li")[1].Click();

            Assert.Single(selected);

            // ClearAsync 内部会触发 StateHasChanged，必须在 renderer Dispatcher 上执行
            await cut.InvokeAsync(() => cut.Instance.ClearAsync());

            Assert.Empty(selected);
        }

        [Fact]
        public async Task FocusAndBlurExposesDoNotThrow()
        {
            var cut = Render<ElCascader>(parameters => parameters
                .Add(x => x.Options, BuildOptions()));

            // ElInput 的 FocusAsync/BlurAsync 委托到 execFocus/execBlur JS，需在 bUnit 中先注册
            JSInterop.SetupVoid("execFocus", _ => true).SetVoidResult();
            JSInterop.SetupVoid("execBlur", _ => true).SetVoidResult();

            await cut.Instance.FocusAsync();
            await cut.Instance.BlurAsync();
        }

        private DropDownOption GetDropdown()
        {
            return Services.GetRequiredService<PopupService>().SelectDropDownOptions.Single();
        }

        private static IEnumerable<CascaderOption> BuildOptions()
        {
            return new[]
            {
                new CascaderOption
                {
                    Label = "Guide",
                    Value = "guide",
                    Children = new List<CascaderOption>
                    {
                        new CascaderOption { Label = "Install", Value = "install", Leaf = true },
                        new CascaderOption { Label = "Upgrade", Value = "upgrade", Leaf = true }
                    }
                }
            };
        }

        [Fact]
        public async Task DebounceZeroFiltersImmediately()
        {
            var cut = Render<ElCascader>(parameters => parameters
                .Add(x => x.Filterable, true)
                .Add(x => x.Debounce, 0)
                .Add(x => x.Options, BuildOptions()));

            await OpenAsync(cut);
            cut.Find("input").Input("Install");
            var fragment = Render(GetDropdown().OptionContent);

            Assert.Contains("Guide / Install", fragment.Markup);
        }

        [Fact]
        public async Task ValueKeyMapsDataProperty()
        {
            IList<IList<string>> selected = new List<IList<string>>();
            var options = new[]
            {
                new CascaderOption
                {
                    Label = "Guide", Value = "guide", Data = new { Code = "G" },
                    Children = new List<CascaderOption>
                    {
                        new CascaderOption { Label = "Install", Value = "install", Data = new { Code = "GI" }, Leaf = true }
                    }
                }
            };
            var cut = Render<ElCascader>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.ValueKey, "Code")
                .Add(x => x.Options, options)
                .Add(x => x.ValuesChanged, paths => selected = paths));

            await OpenAsync(cut);
            var dropdown = GetDropdown();
            Render(dropdown.OptionContent).FindAll("li")[0].Click();
            Render(dropdown.OptionContent).FindAll("li")[1].Click();

            Assert.Collection(selected,
                path => Assert.Equal(new[] { "G", "GI" }, path));
        }

        /// <summary>
        /// 与 ElPopup 同构的增量重渲染宿主：面板 fragment 属于 ElPopup 的渲染树，
        /// 勾选叶子后的面板刷新走的是「同宿主增量 diff」，此前用 Render(fragment) 每次全新渲染
        /// 覆盖不到这条路径——叶子被勾选时其 li 尾部新插入 el-icon-check postfix，
        /// 增量 diff 曾把同 li 已有的 class/role 帧错配丢失（浏览器实测 li class 变空）。
        /// </summary>
        private sealed class PanelHost : ComponentBase
        {
            [Parameter]
            public RenderFragment Content { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.AddContent(0, Content);
            }

            public void Refresh() => StateHasChanged();
        }

        private async Task<IRenderedComponent<PanelHost>> OpenPanelHostAsync(IRenderedComponent<ElCascader> cut)
        {
            var host = Render<PanelHost>(parameters => parameters.Add(x => x.Content, GetDropdown().OptionContent));
            await cut.InvokeAsync(() => host.Instance.Refresh());
            return host;
        }

        [Fact]
        public async Task PanelRetainsNodeAttributesWhenPostfixAppears()
        {
            var cut = Render<ElCascader>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.Options, BuildOptions()));

            await OpenAsync(cut);
            var host = await OpenPanelHostAsync(cut);

            await cut.InvokeAsync(() => host.FindAll("li")[0].Click());
            await cut.InvokeAsync(() => host.Instance.Refresh());

            await cut.InvokeAsync(() => host.FindAll(".el-cascader-menu")[1].QuerySelectorAll("li")[0].Click());
            await cut.InvokeAsync(() => host.Instance.Refresh());

            await cut.InvokeAsync(() => host.FindAll(".el-cascader-menu")[1].QuerySelectorAll("li")[1].Click());
            await cut.InvokeAsync(() => host.Instance.Refresh());

            // 第二个叶子被勾选后，它自己的 li 尾部插入 el-icon-check postfix；第一个叶子的 postfix 移除。
            // 增量 diff 曾在这一步把新勾选 li 的 class/role 属性帧整个丢掉。
            var lastMenu = host.FindAll(".el-cascader-menu")[1];
            var install = lastMenu.QuerySelectorAll("li")[0];
            var upgrade = lastMenu.QuerySelectorAll("li")[1];

            Assert.Contains("el-cascader-node", upgrade.ClassName);
            Assert.Contains("el-cascader-node", install.ClassName);
            Assert.Equal("menuitem", upgrade.GetAttribute("role"));
            Assert.Equal("menuitem", install.GetAttribute("role"));
        }

        [Fact]
        public async Task CheckedLeafGetsEpActiveClass()
        {
            var cut = Render<ElCascader>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.Options, BuildOptions()));

            await OpenAsync(cut);
            var host = await OpenPanelHostAsync(cut);

            await cut.InvokeAsync(() => host.FindAll("li")[0].Click());
            await cut.InvokeAsync(() => host.Instance.Refresh());

            // 勾第一个叶子：它同时是活动路径末梢，is-active 由 IsSelected 给出
            await cut.InvokeAsync(() => host.FindAll(".el-cascader-menu")[1].QuerySelectorAll("li")[0].Click());
            await cut.InvokeAsync(() => host.Instance.Refresh());

            // 勾第二个叶子：第一个叶子不再是路径末梢，EP node.vue:12 的 is-active = node.checked
            // 要求它继续保留（用户报的「勾选项字体和其他项不一致」缺口就在这一步）
            await cut.InvokeAsync(() => host.FindAll(".el-cascader-menu")[1].QuerySelectorAll("li")[1].Click());
            await cut.InvokeAsync(() => host.Instance.Refresh());

            var lastMenu = host.FindAll(".el-cascader-menu")[1];
            var install = lastMenu.QuerySelectorAll("li")[0];
            var upgrade = lastMenu.QuerySelectorAll("li")[1];

            Assert.Contains("is-active", install.ClassName);
            Assert.Contains("is-checked", install.ClassName);
            Assert.DoesNotContain("in-checked-path", install.ClassName);
            // EP node.vue:9 —— is-selectable 只在 checkStrictly 出现；多选叶子若带它，
            // 旧 CSS 的 .is-selectable.in-active-path 会把勾中叶子的蓝色压回常规灰
            Assert.DoesNotContain("is-selectable", install.ClassName);
            Assert.Contains("is-active", upgrade.ClassName);
            Assert.Contains("is-checked", upgrade.ClassName);
            Assert.DoesNotContain("is-selectable", upgrade.ClassName);
        }

        [Fact]
        public async Task CheckStrictlyMarksCheckedPathLikeEp()
        {
            var cut = Render<ElCascader>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.CheckStrictly, true)
                .Add(x => x.Options, BuildOptions()));

            await OpenAsync(cut);
            var host = await OpenPanelHostAsync(cut);

            await cut.InvokeAsync(() => host.FindAll("li")[0].Click());
            await cut.InvokeAsync(() => host.Instance.Refresh());

            // EP node.vue:15 — in-checked-path 只在 checkStrictly 模式出现，勾中节点与其祖先都在勾选路径上
            var guide = host.FindAll(".el-cascader-menu")[0].QuerySelectorAll("li")[0];
            Assert.Contains("in-checked-path", guide.ClassName);
            Assert.Contains("is-active", guide.ClassName);
            // strict 模式 is-selectable 必须保留（旧 CSS 的 .is-selectable.in-checked-path 靠它上蓝）
            Assert.Contains("is-selectable", guide.ClassName);

            await cut.InvokeAsync(() => host.FindAll(".el-cascader-menu")[1].QuerySelectorAll("li")[0].Click());
            await cut.InvokeAsync(() => host.Instance.Refresh());

            var guideAfter = host.FindAll(".el-cascader-menu")[0].QuerySelectorAll("li")[0];
            Assert.Contains("in-checked-path", guideAfter.ClassName);
        }

        [Fact]
        public async Task MultipleTagsUseEpCascaderTagDom()
        {
            var cut = Render<ElCascader>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.Options, BuildOptions()));

            await OpenAsync(cut);
            var host = await OpenPanelHostAsync(cut);

            await cut.InvokeAsync(() => host.FindAll("li")[0].Click());
            await cut.InvokeAsync(() => host.Instance.Refresh());
            await cut.InvokeAsync(() => host.FindAll(".el-cascader-menu")[1].QuerySelectorAll("li")[0].Click());
            await cut.InvokeAsync(() => host.Instance.Refresh());

            // EP cascader.vue:72/110 — 标签容器 el-cascader__tags、文本 span el-cascader__tags-text
            var tag = cut.Find(".el-cascader__tags .el-tag");
            var text = tag.QuerySelector(".el-cascader__tags-text");
            Assert.NotNull(text);
            Assert.Equal("Guide / Install", text.TextContent.Trim());
            Assert.NotNull(tag.QuerySelector(".el-tag__close"));
        }
    }
}
