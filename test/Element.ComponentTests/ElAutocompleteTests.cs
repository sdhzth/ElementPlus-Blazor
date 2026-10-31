using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElAutocompleteTests : BunitContext
    {
        public ElAutocompleteTests()
        {
            Services.AddElementServices();
        }

        [Fact]
        public async Task DebouncesRemoteSuggestions()
        {
            var calls = new List<string>();
            var cut = Render<ElAutocomplete>(parameters => parameters
                .Add(x => x.Debounce, 20)
                .Add(x => x.TriggerOnFocus, false)
                .Add(x => x.FetchSuggestionsAsync, query =>
                {
                    calls.Add(query);
                    return Task.FromResult<IEnumerable<AutocompleteOption>>(new[]
                    {
                        new AutocompleteOption { Label = query, Value = query }
                    });
                }));

            // 两次键入放进同一个派发回合，回合内 spin 3 倍 Debounce（证人 Temp116Witness W3/W2，
            // 日志 Temp/b22-w116-witness.log）：派发回合串行 ⇒ 键间真实间隙不再赌墙钟；
            // spin 故意让「a」的定时器在回合内到点 ⇒ 若组件在无墙钟保证的「到点→发远端」窗口里
            // 漏发被取代的查询，这里必然多出一条 "a"。旧结构两回合之间一旦被调度拉开 >Debounce，
            // 「a」就已上远端（超额订阅负载实测红：b17-hunt4 run5；临时证人 W2 免负载确定性复现）。
            await cut.InvokeAsync(() =>
            {
                cut.Find("input").Input("a");
                SpinWait.SpinUntil(() => false, TimeSpan.FromMilliseconds(60));
                cut.Find("input").Input("ab");
            });

            // 只断终态：多余的 "a" 一旦发出就留在 calls 里，等不来也藏不掉——
            // 防抖失效（不延迟）或不 cancel（都发）都会以 calls == ["a","ab"] 被抓死。
            cut.WaitForAssertion(() =>
            {
                Assert.Equal(new[] { "ab" }, calls);
                Assert.Equal("ab", cut.Instance.CurrentSuggestions.Single().Value);
            }, timeout: TestWait.Timeout);
        }

        [Fact]
        public async Task ClearCancelsPendingRemoteSuggestions()
        {
            var pending = new TaskCompletionSource<IEnumerable<AutocompleteOption>>();
            var cut = Render<ElAutocomplete>(parameters => parameters
                .Add(x => x.Debounce, 0)
                .Add(x => x.Value, "a")
                .Add(x => x.Clearable, true)
                .Add(x => x.TriggerOnFocus, false)
                .Add(x => x.FetchSuggestionsAsync, _ => pending.Task));

            cut.Find("input").Input("a");
            await Task.Delay(25);
            Assert.True(cut.Instance.IsInternalLoading);

            await cut.InvokeAsync(() => cut.Instance.ClearAsync());
            Assert.False(cut.Instance.IsInternalLoading);

            pending.SetResult(new[]
            {
                new AutocompleteOption { Label = "stale", Value = "stale" }
            });

            cut.WaitForAssertion(() => Assert.Empty(cut.Instance.CurrentSuggestions), timeout: TestWait.Timeout);
        }

        [Fact]
        public async Task IgnoresSlowerStaleRemoteResults()
        {
            var pendingA = new TaskCompletionSource<IEnumerable<AutocompleteOption>>();
            var pendingAb = new TaskCompletionSource<IEnumerable<AutocompleteOption>>();
            var cut = Render<ElAutocomplete>(parameters => parameters
                .Add(x => x.Debounce, 0)
                .Add(x => x.TriggerOnFocus, false)
                .Add(x => x.FetchSuggestionsAsync, query => query == "a" ? pendingA.Task : pendingAb.Task));

            cut.Find("input").Input("a");
            cut.Find("input").Input("ab");

            pendingAb.SetResult(new[]
            {
                new AutocompleteOption { Label = "current", Value = "current" }
            });
            cut.WaitForAssertion(() => Assert.Equal("current", cut.Instance.CurrentSuggestions.Single().Value), timeout: TestWait.Timeout);

            pendingA.SetResult(new[]
            {
                new AutocompleteOption { Label = "stale", Value = "stale" }
            });
            Assert.Equal("current", cut.Instance.CurrentSuggestions.Single().Value);
        }

        [Fact]
        public async Task ShowAsyncOpensDropdown()
        {
            var cut = Render<ElAutocomplete>(parameters => parameters
                .Add(x => x.Debounce, 0)
                .Add(x => x.TriggerOnFocus, false)
                .Add(x => x.Suggestions, new[]
                {
                    new AutocompleteOption { Label = "a", Value = "a" }
                }));

            Assert.Empty(Services.GetRequiredService<PopupService>().SelectDropDownOptions);

            await cut.InvokeAsync(() => cut.Instance.ShowAsync());
            cut.WaitForAssertion(() => Assert.Single(Services.GetRequiredService<PopupService>().SelectDropDownOptions), timeout: TestWait.Timeout);
        }

        [Fact]
        public async Task CloseAsyncCollapsesOpenDropdown()
        {
            var cut = Render<ElAutocomplete>(parameters => parameters
                .Add(x => x.Debounce, 0)
                .Add(x => x.TriggerOnFocus, false)
                .Add(x => x.Suggestions, new[]
                {
                    new AutocompleteOption { Label = "a", Value = "a" }
                }));

            cut.Find("input").Input("a");
            cut.WaitForAssertion(() => Assert.Single(Services.GetRequiredService<PopupService>().SelectDropDownOptions), timeout: TestWait.Timeout);

            await cut.InvokeAsync(() => cut.Instance.CloseAsync());
            Assert.Empty(Services.GetRequiredService<PopupService>().SelectDropDownOptions);
        }

        [Fact]
        public async Task RendersSuggestionItemTemplateAlias()
        {
            RenderFragment<AutocompleteOption> template = item => builder =>
            {
                builder.OpenElement(0, "strong");
                builder.AddAttribute(1, "class", "custom-suggestion");
                builder.AddContent(2, $"{item.Label}:{item.Data}");
                builder.CloseElement();
            };

            var cut = Render<ElAutocomplete>(parameters => parameters
                .Add(x => x.Debounce, 0)
                .Add(x => x.TriggerOnFocus, false)
                .Add(x => x.Suggestions, new[]
                {
                    new AutocompleteOption { Label = "Element Plus", Value = "element-plus", Data = "docs" }
                })
                .Add(x => x.SuggestionItemTemplate, template));

            cut.Find("input").Input("e");
            cut.WaitForAssertion(() => Assert.Single(cut.Instance.CurrentSuggestions), timeout: TestWait.Timeout);

            var dropdown = Services.GetRequiredService<PopupService>().SelectDropDownOptions.Single();
            var fragment = Render(dropdown.OptionContent);

            Assert.Contains("custom-suggestion", fragment.Markup);
            Assert.Contains("Element Plus:docs", fragment.Markup);
        }
    }
}
