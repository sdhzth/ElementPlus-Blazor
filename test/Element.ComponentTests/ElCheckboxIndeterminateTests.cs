using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 批次⑮：/checkbox「半选状态（Indeterminate）」显示不对（用户截图里全选框是打勾，不是横线）。
    ///
    /// 真身不在库的渲染上 —— CSS 通路与 `is-indeterminate` 类名都是好的（浏览器探针 B1/B2 正负对照实测），
    /// 而是演示页从未进入过半选态：旧写法 `@bind-Indeterminate="indeterminate"` 里
    /// **库里根本没有 `IndeterminateChanged` 这个参数**，它被基类 `ElementComponentBase.Attributes`
    /// 的 `[Parameter(CaptureUnmatchedValues = true)]` 静默吞掉（不报错、不生效），
    /// 于是 indeterminate 永远是 false，一格「半选」演示从头到尾只是个普通复选框。
    ///
    /// EP 2.14.7 口径（Temp/ep214/intermediate.vue）：`indeterminate` 是**单向属性**，
    /// 半选/全选由宿主按「已选数 vs 总数」重算；初始 4 项选 2 项 → 一进页面就是横线。
    /// 下面这个 SelectAllHost 就是该口径的 .NET 版，用来把「宿主拥有状态」这条闭环钉住。
    /// </summary>
    public class ElCheckboxIndeterminateTests : BunitContext
    {
        public ElCheckboxIndeterminateTests()
        {
            Services.AddElementPlusServices();
        }

        [Fact]
        public void BindIndeterminate_IsNotAParameter_ThisIsWhyTheOldDemoSilentlyBroke()
        {
            // 记录陷阱本身：@bind-X 需要 XChanged，而 ElCheckbox 只有单向的 Indeterminate。
            // 谁哪天加了 IndeterminateChanged，这条会红 —— 那时要连带动演示页和 docs/checkbox-alignment.md。
            Assert.Null(typeof(ElCheckbox<bool>).GetProperty("IndeterminateChanged"));
            Assert.NotNull(typeof(ElCheckbox<bool>).GetProperty("Indeterminate"));
        }

        [Fact]
        public void IndeterminateParamTrue_WinsOverBoundTrueValue()
        {
            // ElCheckbox.razor.cs 里 `Status != Status.Indeterminate` 那道守卫：
            // @bind-Value 接线后 Status 通常跟随 Value，但半选是宿主说了算，不能被 Value=true 顶成打勾。
            var cut = Render<ElCheckbox<bool>>(parameters => parameters
                .Add(x => x.Value, true)
                .Add(x => x.ValueChanged, _ => { })
                .Add(x => x.Indeterminate, true)
                .AddChildContent("全选"));

            var input = cut.Find(".el-checkbox__input");
            Assert.Contains("is-indeterminate", input.ClassList);
            Assert.DoesNotContain("is-checked", input.ClassList);
            Assert.Equal("mixed", cut.Find("label").GetAttribute("aria-checked"));
        }

        [Fact]
        public void IndeterminateParamFalse_FallsBackToUnChecked_NotChecked()
        {
            // Indeterminate 的 setter 是「false 且当前是半选 → 退回未选」，不会自己变成打勾。
            var cut = Render<ElCheckbox<bool>>(parameters => parameters
                .Add(x => x.Value, true)
                .Add(x => x.Indeterminate, true)
                .AddChildContent("全选"));
            Assert.Contains("is-indeterminate", cut.Find(".el-checkbox__input").ClassList);

            // 直接翻 Indeterminate 属性再重渲染：走的就是 setter 的 false 分支
            cut.Instance.Indeterminate = false;
            cut.Render();

            var input = cut.Find(".el-checkbox__input");
            Assert.DoesNotContain("is-indeterminate", input.ClassList);
            Assert.DoesNotContain("is-checked", input.ClassList);
        }

        [Fact]
        public void ClickWhileIndeterminate_EmitsCheckedValueSoHostCanRecompute()
        {
            var values = new List<bool>();
            var statuses = new List<Status>();
            var cut = Render<ElCheckbox<bool>>(parameters => parameters
                .Add(x => x.Value, false)
                .Add(x => x.ValueChanged, v => values.Add(v))
                .Add(x => x.StatusChanged, s => statuses.Add(s))
                .Add(x => x.Indeterminate, true)
                .AddChildContent("全选"));

            cut.Find("input").Change(true);

            Assert.Equal(new[] { true }, values);
            Assert.Equal(new[] { Status.Checked }, statuses);
        }

        [Fact]
        public void SelectAllHost_TwoOfFour_StartsAsIndeterminateDash()
        {
            var cut = Render<SelectAllHost>();
            var all = cut.Find(".el-checkbox");
            Assert.Contains("is-indeterminate", all.ClassList);
            Assert.DoesNotContain("is-checked", all.ClassList);
            Assert.Equal("mixed", all.GetAttribute("aria-checked"));
            Assert.Equal(2, CountCheckedChildren(cut));
        }

        [Fact]
        public void SelectAllHost_ClickDash_SelectsAllFourAndBecomesChecked()
        {
            var cut = Render<SelectAllHost>();
            cut.FindAll("input")[0].Change(true);

            Assert.Contains("is-checked", cut.FindAll(".el-checkbox")[0].ClassList);
            Assert.DoesNotContain("is-indeterminate", cut.FindAll(".el-checkbox")[0].ClassList);
            Assert.Equal("true", cut.FindAll("label")[0].GetAttribute("aria-checked"));
            Assert.Equal(4, CountCheckedChildren(cut));
        }

        [Fact]
        public void SelectAllHost_ClickTwice_ClearsAllAndShowsPlainBox()
        {
            var cut = Render<SelectAllHost>();
            var inputs = () => cut.FindAll("input");
            inputs()[0].Change(true);
            inputs()[0].Change(false);

            var all = cut.FindAll(".el-checkbox")[0];
            Assert.DoesNotContain("is-checked", all.ClassList);
            Assert.DoesNotContain("is-indeterminate", all.ClassList);
            Assert.Equal("false", all.GetAttribute("aria-checked"));
            Assert.Equal(0, CountCheckedChildren(cut));
        }

        [Fact]
        public void SelectAllHost_UncheckOneOfFourChildren_GoesBackToDash()
        {
            var cut = Render<SelectAllHost>();
            cut.FindAll("input")[0].Change(true);      // 全选 → 4 个全中
            Assert.Equal(4, CountCheckedChildren(cut));

            cut.FindAll("input")[2].Change(false);      // 取消 1 个子项 → 3/4
            var all = cut.FindAll(".el-checkbox")[0];
            Assert.Contains("is-indeterminate", all.ClassList);
            Assert.DoesNotContain("is-checked", all.ClassList);
            Assert.Equal(3, CountCheckedChildren(cut));
        }

        [Fact]
        public void SelectAllHost_CheckLastMissingChild_DashBecomesTick()
        {
            var cut = Render<SelectAllHost>();
            // 初始 B、D 两个选中；补上 A、C → 4/4，半选必须翻成打勾
            cut.FindAll("input")[1].Change(true);
            Assert.Contains("is-indeterminate", cut.FindAll(".el-checkbox")[0].ClassList);

            cut.FindAll("input")[3].Change(true);
            var all = cut.FindAll(".el-checkbox")[0];
            Assert.Contains("is-checked", all.ClassList);
            Assert.DoesNotContain("is-indeterminate", all.ClassList);
            Assert.Equal(4, CountCheckedChildren(cut));
        }

        private static int CountCheckedChildren(IRenderedComponent<SelectAllHost> cut)
        {
            // 子项是 index 1..4；原生 input 的 checked 特性由 Status==Checked 时渲染出来
            return cut.FindAll(".el-checkbox").Skip(1).Count(x => x.ClassList.Contains("is-checked"));
        }

        /// <summary>演示页 CheckboxDemo.razor「半选状态」那一段的等价宿主：状态全部由宿主拥有。</summary>
        private sealed class SelectAllHost : ComponentBase
        {
            private static readonly string[] AllItems = { "备选项 A", "备选项 B", "备选项 C", "备选项 D" };
            private List<string> checkedItems = new List<string> { "备选项 B", "备选项 D" };
            private bool checkAll;
            private bool isIndeterminate = true;

            protected override void OnInitialized()
            {
                SyncAllState();
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<ElCheckbox<bool>>(0);
                builder.AddAttribute(1, "Value", checkAll);
                builder.AddAttribute(2, "ValueChanged", EventCallback.Factory.Create<bool>(this, OnCheckAllChanged));
                builder.AddAttribute(3, "Indeterminate", isIndeterminate);
                builder.AddAttribute(4, "ChildContent", (RenderFragment)(x => x.AddContent(0, "全选")));
                builder.CloseComponent();

                builder.OpenComponent<ElCheckboxGroup<string>>(10);
                builder.AddAttribute(11, "Value", checkedItems);
                builder.AddAttribute(12, "ValueChanged", EventCallback.Factory.Create<IEnumerable<string>>(this, OnCheckedItemsChanged));
                builder.AddAttribute(13, "ChildContent", (RenderFragment)(group =>
                {
                    var seq = 0;
                    foreach (var item in AllItems)
                    {
                        group.OpenComponent<ElCheckbox<string>>(seq++);
                        group.AddAttribute(seq++, "Value", item);
                        group.AddAttribute(seq++, "ChildContent", (RenderFragment)(label => label.AddContent(0, item)));
                        group.CloseComponent();
                    }
                }));
                builder.CloseComponent();
            }

            private void OnCheckAllChanged(bool value)
            {
                checkedItems = value ? AllItems.ToList() : new List<string>();
                checkAll = value;
                isIndeterminate = false;
            }

            private void OnCheckedItemsChanged(IEnumerable<string> values)
            {
                checkedItems = values?.ToList() ?? new List<string>();
                SyncAllState();
            }

            private void SyncAllState()
            {
                var count = checkedItems?.Count ?? 0;
                checkAll = count == AllItems.Length;
                isIndeterminate = count > 0 && count < AllItems.Length;
            }
        }
    }
}
