using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElSelectV2Tests : BunitContext
    {
        public ElSelectV2Tests()
        {
            Services.AddElementServices();
            // 面板打开时组件会调 dom.js 的 elementPickerOutsideAttach 绑 document 上的
            // 捕获阶段 mousedown（EP useSelect.mjs#handleClickOutside 的等价物）。bUnit 没有
            // DOM，这一层只能让它空转；点外关闭的行为由常驻浏览器探针 S4/S4b 覆盖。
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void RendersVirtualizedOptionsAndSelectsValue()
        {
            int value = 0;
            var cut = Render<ElSelectV2<int>>(parameters => parameters
                .Add(x => x.Options, Enumerable.Range(1, 1000).Select(i => new SelectV2Option
                {
                    Label = $"Option {i}",
                    Value = i
                }))
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-select-v2").Click();
            cut.FindAll(".el-select-dropdown__item").First().Click();

            Assert.Equal(1, value);
        }

        [Fact]
        public void FiltersVirtualizedOptions()
        {
            var cut = Render<ElSelectV2<int>>(parameters => parameters
                .Add(x => x.Filterable, true)
                .Add(x => x.Options, Enumerable.Range(1, 30).Select(i => new SelectV2Option
                {
                    Label = $"Option {i}",
                    Value = i
                })));

            cut.Find(".el-select-v2").Click();
            cut.Find("input").Input("Option 29");

            var items = cut.FindAll(".el-select-dropdown__item");
            Assert.Single(items);
            Assert.Contains("Option 29", items[0].TextContent);
        }

        [Fact]
        public void MultipleModeRendersTagsAndUpdatesValues()
        {
            ICollection<int> values = new List<int>();
            var cut = Render<ElSelectV2<int>>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.Values, values)
                .Add(x => x.ValuesChanged, next => values = next)
                .Add(x => x.Options, new[]
                {
                    new SelectV2Option { Label = "One", Value = 1 },
                    new SelectV2Option { Label = "Two", Value = 2 }
                }));

            cut.Find(".el-select-v2").Click();
            var items = cut.FindAll(".el-select-dropdown__item");
            items[0].Click();
            items = cut.FindAll(".el-select-dropdown__item");
            items[1].Click();

            Assert.Equal(new[] { 1, 2 }, values);
            Assert.Equal(2, cut.FindAll(".el-select__tags .el-tag").Count);
        }

        [Fact]
        public void RendersGroupedOptions()
        {
            var cut = Render<ElSelectV2<int>>(parameters => parameters
                .Add(x => x.Options, new[]
                {
                    new SelectV2Option { Label = "API", Value = 1, Group = "Backend" },
                    new SelectV2Option { Label = "UI", Value = 2, Group = "Frontend" }
                }));

            cut.Find(".el-select-v2").Click();

            var titles = cut.FindAll(".el-select-group__title");
            Assert.Equal(new[] { "Backend", "Frontend" }, new[] { titles[0].TextContent.Trim(), titles[1].TextContent.Trim() });
        }

        [Fact]
        public void RemoteFilterInvokesCallback()
        {
            var queries = new List<string>();
            var cut = Render<ElSelectV2<int>>(parameters => parameters
                .Add(x => x.Filterable, true)
                .Add(x => x.Remote, true)
                .Add(x => x.RemoteMethod, query => queries.Add(query))
                .Add(x => x.Options, Enumerable.Empty<SelectV2Option>()));

            cut.Find(".el-select-v2").Click();
            cut.Find("input").Input("remote");

            Assert.Equal(new[] { "remote" }, queries);
        }

        [Fact]
        public void RemoteFilterKeepsServerOptionsVisibleUntilResultsArrive()
        {
            var cut = Render<ElSelectV2<int>>(parameters => parameters
                .Add(x => x.Filterable, true)
                .Add(x => x.Remote, true)
                .Add(x => x.Options, new[]
                {
                    new SelectV2Option { Label = "Existing", Value = 1 }
                }));

            cut.Find(".el-select-v2").Click();
            cut.Find("input").Input("missing");

            Assert.Single(cut.FindAll(".el-select-dropdown__item"));
            Assert.Contains("Existing", cut.Find(".el-select-dropdown__item").TextContent);
        }

        [Fact]
        public void KeyboardNavigationSelectsActiveOption()
        {
            int value = 0;
            var cut = Render<ElSelectV2<int>>(parameters => parameters
                .Add(x => x.Options, new[]
                {
                    new SelectV2Option { Label = "One", Value = 1, Disabled = true },
                    new SelectV2Option { Label = "Two", Value = 2 },
                    new SelectV2Option { Label = "Three", Value = 3 }
                })
                .Add(x => x.ValueChanged, next => value = next));

            var root = cut.Find(".el-select-v2");
            root.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
            root.KeyDown(new KeyboardEventArgs { Key = "Enter" });

            Assert.Equal(3, value);
        }

        [Fact]
        public void DisabledOptionDoesNotChangeValue()
        {
            int value = 0;
            var cut = Render<ElSelectV2<int>>(parameters => parameters
                .Add(x => x.Options, new[]
                {
                    new SelectV2Option { Label = "Locked", Value = 1, Disabled = true }
                })
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-select-v2").Click();
            cut.Find(".el-select-dropdown__item").Click();

            Assert.Equal(0, value);
        }

        [Fact]
        public void ChangingCanCancelSelection()
        {
            int value = 0;
            var cut = Render<ElSelectV2<int>>(parameters => parameters
                .Add(x => x.Options, new[]
                {
                    new SelectV2Option { Label = "Option 1", Value = 1 }
                })
                .Add(x => x.OnChanging, args => args.DisallowChange = true)
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-select-v2").Click();
            cut.Find(".el-select-dropdown__item").Click();

            Assert.Equal(0, value);
        }

        [Fact]
        public void ClearableClearsSelectedValue()
        {
            int? value = 1;
            var cut = Render<ElSelectV2<int?>>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.Options, new[]
                {
                    new SelectV2Option { Label = "Option 1", Value = 1 }
                })
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-select-v2__clear").Click();

            Assert.Null(value);
        }

        [Fact]
        public void ClampsVirtualizationBoundaries()
        {
            var cut = Render<ElSelectV2<int>>(parameters => parameters
                .Add(x => x.ItemHeight, 0)
                .Add(x => x.Height, 10)
                .Add(x => x.OverscanCount, -5)
                .Add(x => x.Options, new[]
                {
                    new SelectV2Option { Label = "Option 1", Value = 1 }
                }));

            cut.Find(".el-select-v2").Click();
            var item = cut.Find(".el-select-dropdown__item");

            Assert.Contains("height:24px", item.GetAttribute("style"));
            Assert.Contains("max-height:48px", cut.Find(".el-select-v2__popper").GetAttribute("style"));
        }

        [Fact]
        public void DefaultFirstOptionSelectsFirstOnEnter()
        {
            int value = 0;
            var cut = Render<ElSelectV2<int>>(parameters => parameters
                .Add(x => x.DefaultFirstOption, true)
                .Add(x => x.Options, new[]
                {
                    new SelectV2Option { Label = "One", Value = 1 },
                    new SelectV2Option { Label = "Two", Value = 2 }
                })
                .Add(x => x.ValueChanged, next => value = next));

            cut.Find(".el-select-v2").Click();
            cut.Find(".el-select-v2").KeyDown(new KeyboardEventArgs { Key = "Enter" });

            Assert.Equal(1, value);
        }

        [Fact]
        public void ClearAfterSelectClearsFilterInMultipleMode()
        {
            ICollection<int> values = new List<int>();
            var cut = Render<ElSelectV2<int>>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.Filterable, true)
                .Add(x => x.ClearAfterSelect, true)
                .Add(x => x.Values, values)
                .Add(x => x.ValuesChanged, next => values = next)
                .Add(x => x.Options, new[]
                {
                    new SelectV2Option { Label = "One", Value = 1 },
                    new SelectV2Option { Label = "Two", Value = 2 }
                }));

            cut.Find(".el-select-v2").Click();
            cut.Find("input").Input("One");
            cut.FindAll(".el-select-dropdown__item").First().Click();

            var inputValue = cut.Find("input").GetAttribute("value");
            Assert.True(string.IsNullOrEmpty(inputValue));
        }

        [Fact]
        public void WithoutClearAfterSelectFilterPersistsInMultipleMode()
        {
            ICollection<int> values = new List<int>();
            var cut = Render<ElSelectV2<int>>(parameters => parameters
                .Add(x => x.Multiple, true)
                .Add(x => x.Filterable, true)
                .Add(x => x.ClearAfterSelect, false)
                .Add(x => x.Values, values)
                .Add(x => x.ValuesChanged, next => values = next)
                .Add(x => x.Options, new[]
                {
                    new SelectV2Option { Label = "One", Value = 1 },
                    new SelectV2Option { Label = "Two", Value = 2 }
                }));

            cut.Find(".el-select-v2").Click();
            cut.Find("input").Input("One");
            cut.FindAll(".el-select-dropdown__item").First().Click();

            Assert.Equal("One", cut.Find("input").GetAttribute("value"));
        }

        [Fact]
        public void AllowCreateShowsCreateRowAndInvokesCallback()
        {
            string created = null;
            var onCreated = EventCallback.Factory.Create<string>(this, s => created = s);
            var cut = Render<ElSelectV2<string>>(parameters => parameters
                .Add(x => x.Filterable, true)
                .Add(x => x.AllowCreate, true)
                .Add(x => x.OnCreateOption, onCreated)
                .Add(x => x.Options, new[]
                {
                    new SelectV2Option { Label = "Existing", Value = "existing" }
                }));

            cut.Find(".el-select-v2").Click();
            cut.Find("input").Input("Cool");

            Assert.Single(cut.FindAll(".el-select-dropdown__item--create"));
            Assert.Contains("创建", cut.Find(".el-select-dropdown__item--create").TextContent);

            cut.Find(".el-select-dropdown__item--create").Click();

            Assert.Equal("Cool", created);
        }
    }
}
