using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElTabsTests : BunitContext
    {
        public ElTabsTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static RenderFragment TwoPanes() => builder =>
        {
            builder.OpenComponent<ElTabPane>(0);
            builder.AddAttribute(1, "Name", "p1");
            builder.AddAttribute(2, "Title", "页签一");
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "内容一")));
            builder.CloseComponent();

            builder.OpenComponent<ElTabPane>(4);
            builder.AddAttribute(5, "Name", "p2");
            builder.AddAttribute(6, "Title", "页签二");
            builder.AddAttribute(7, "ChildContent", (RenderFragment)(b => b.AddContent(0, "内容二")));
            builder.CloseComponent();
        };

        [Fact]
        public void RendersPanesAndActivatesFirst()
        {
            var cut = Render<ElTabs>(p => p.AddChildContent(TwoPanes()));

            Assert.Equal(2, cut.FindAll(".el-tabs__item").Count);
            Assert.Contains("页签一", cut.FindAll(".el-tabs__item")[0].TextContent);
            Assert.Contains("is-active", cut.FindAll(".el-tabs__item")[0].GetAttribute("class"));
        }

        [Fact]
        public void ModelValueDrivesActivePane()
        {
            var cut = Render<ElTabs>(p => p
                .Add(x => x.ModelValue, "p2")
                .AddChildContent(TwoPanes()));

            Assert.Equal("p2", cut.Instance.CurrentName);
            Assert.Contains("is-active", cut.FindAll(".el-tabs__item")[1].GetAttribute("class"));
        }

        [Fact]
        public void TabClickFiresEvenWhenAlreadyActive()
        {
            var clicks = new List<string>();
            var cut = Render<ElTabs>(p => p
                .Add(x => x.ModelValue, "p1")
                .Add(x => x.OnTabClick, (TabClickEventArgs e) => clicks.Add(e.Pane?.Name))
                .AddChildContent(TwoPanes()));

            // 点当前已激活的页签：tab-click 仍应触发
            cut.FindAll(".el-tabs__item")[0].Click();

            Assert.Single(clicks);
            Assert.Equal("p1", clicks[0]);
        }

        [Fact]
        public void TabClickFiresBeforeSwitch()
        {
            var clicks = new List<string>();
            var cut = Render<ElTabs>(p => p
                .Add(x => x.OnTabClick, (TabClickEventArgs e) => clicks.Add(e.Pane?.Name))
                .AddChildContent(TwoPanes()));

            cut.FindAll(".el-tabs__item")[1].Click();

            Assert.Single(clicks);
            Assert.Equal("p2", clicks[0]);
            Assert.Equal("p2", cut.Instance.CurrentName);
        }

        [Fact]
        public void EditEventFiresWithAddAction()
        {
            var edits = new List<string>();
            var dataSource = new System.Collections.ObjectModel.ObservableCollection<TabOption>
            {
                new TabOption { Name = "a1", Title = "一" },
                new TabOption { Name = "a2", Title = "二" }
            };

            var cut = Render<ElTabs>(p => p
                .Add(x => x.IsAddable, true)
                .Add(x => x.DataSource, dataSource)
                .Add(x => x.OnEdit, (TabEditEventArgs e) => edits.Add(e.Action)));

            cut.Find(".el-tabs__new-tab").Click();

            Assert.Single(edits);
            Assert.Equal("add", edits[0]);
        }

        [Fact]
        public void EditEventFiresWithRemoveActionOnClose()
        {
            var edits = new List<string>();
            var cut = Render<ElTabs>(p => p
                .Add(x => x.OnEdit, (TabEditEventArgs e) => edits.Add(e.Action))
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<ElTabPane>(0);
                    builder.AddAttribute(1, "Name", "c1");
                    builder.AddAttribute(2, "Title", "一");
                    builder.AddAttribute(3, "IsClosable", true);
                    builder.CloseComponent();
                    builder.OpenComponent<ElTabPane>(4);
                    builder.AddAttribute(5, "Name", "c2");
                    builder.AddAttribute(6, "Title", "二");
                    builder.CloseComponent();
                }));

            cut.Find(".el-icon-close").Click();

            Assert.Contains("remove", edits);
        }

        [Fact]
        public void TitleTemplateOverridesTitle()
        {
            var cut = Render<ElTabs>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElTabPane>(0);
                builder.AddAttribute(1, "Name", "x1");
                builder.AddAttribute(2, "Title", "原标题");
                builder.AddAttribute(3, "TitleTemplate", (RenderFragment)(b =>
                {
                    b.OpenElement(0, "span");
                    b.AddAttribute(1, "class", "my-label");
                    b.AddContent(2, "自定义");
                    b.CloseElement();
                }));
                builder.CloseComponent();
            }));

            Assert.NotNull(cut.Find(".my-label"));
        }

        [Fact]
        public void LabelAliasSetsTitle()
        {
            var cut = Render<ElTabs>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElTabPane>(0);
                builder.AddAttribute(1, "Name", "l1");
                builder.AddAttribute(2, "Label", "标签别名");
                builder.CloseComponent();
            }));

            Assert.Contains("标签别名", cut.Find(".el-tabs__item").TextContent);
        }

        [Fact]
        public void DisabledPaneIsNotActivated()
        {
            var cut = Render<ElTabs>(p => p.AddChildContent(builder =>
            {
                builder.OpenComponent<ElTabPane>(0);
                builder.AddAttribute(1, "Name", "d1");
                builder.AddAttribute(2, "Title", "一");
                builder.CloseComponent();
                builder.OpenComponent<ElTabPane>(3);
                builder.AddAttribute(4, "Name", "d2");
                builder.AddAttribute(5, "Title", "二");
                builder.AddAttribute(6, "Disabled", true);
                builder.CloseComponent();
            }));

            cut.FindAll(".el-tabs__item")[1].Click();

            Assert.Contains("is-disabled", cut.FindAll(".el-tabs__item")[1].GetAttribute("class"));
            Assert.Equal("d1", cut.Instance.CurrentName);
        }

        #region 硬编码页签的真删除 + 文字对齐 + before-leave 参数序

        private static RenderFragment ClosableTwoPanes() => builder =>
        {
            builder.OpenComponent<ElTabPane>(0);
            builder.AddAttribute(1, "Name", "p1");
            builder.AddAttribute(2, "Title", "页签一");
            builder.AddAttribute(3, "IsClosable", true);
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "内容一")));
            builder.CloseComponent();

            builder.OpenComponent<ElTabPane>(5);
            builder.AddAttribute(6, "Name", "p2");
            builder.AddAttribute(7, "Title", "页签二");
            builder.AddAttribute(8, "IsClosable", true);
            builder.AddAttribute(9, "ChildContent", (RenderFragment)(b => b.AddContent(0, "内容二")));
            builder.CloseComponent();
        };

        [Fact]
        public void CloseOnActiveHardcodedPaneRemovesItAndActivatesNeighbour()
        {
            var cut = Render<ElTabs>(p => p.AddChildContent(ClosableTwoPanes()));
            Assert.Equal(2, cut.FindAll(".el-tabs__item").Count);

            cut.FindAll(".el-icon-close")[0].Click();

            var items = cut.FindAll(".el-tabs__item");
            Assert.Single(items);
            Assert.Contains("页签二", items[0].TextContent);
            Assert.Contains("is-active", items[0].GetAttribute("class"));
            Assert.Equal("p2", cut.Instance.CurrentName);
            Assert.Contains("内容二", cut.Find(".el-tabs__content").TextContent);
        }

        [Fact]
        public void ClosedHardcodedPaneStaysClosedAfterParentRerenders()
        {
            // 硬编码子标记由父组件重渲染时会重新走一遍渲染；被关掉的页签必须记住，否则 X 点了等于没点。
            var cut = Render<ElTabs>(p => p.AddChildContent(ClosableTwoPanes()));
            cut.FindAll(".el-icon-close")[0].Click();

            cut.Render();
            cut.Render();

            Assert.Single(cut.FindAll(".el-tabs__item"));
            Assert.Equal("p2", cut.Instance.CurrentName);
        }

        [Fact]
        public void CloseOnInactiveHardcodedPaneKeepsCurrentActive()
        {
            var cut = Render<ElTabs>(p => p.AddChildContent(ClosableTwoPanes()));

            cut.FindAll(".el-icon-close")[1].Click();

            var items = cut.FindAll(".el-tabs__item");
            Assert.Single(items);
            Assert.Contains("页签一", items[0].TextContent);
            Assert.Contains("is-active", items[0].GetAttribute("class"));
            Assert.Equal("p1", cut.Instance.CurrentName);
        }

        [Fact]
        public void CloseStillEmitsEditRemoveAndTabClose()
        {
            var edits = new List<string>();
            var closed = new List<string>();
            var cut = Render<ElTabs>(p => p
                .Add(x => x.OnEdit, (TabEditEventArgs e) => edits.Add(e.Action + ":" + e.PaneName))
                .Add(x => x.OnTabClose, (ElTabPane t) => closed.Add(t.Name))
                .AddChildContent(ClosableTwoPanes()));

            cut.FindAll(".el-icon-close")[0].Click();

            Assert.Contains("remove:p1", edits);
            Assert.Single(closed);
            Assert.Equal("p1", closed[0]);
        }

        [Fact]
        public void TabTextAlignEmitsJustifyOnNavAndItems()
        {
            var cut = Render<ElTabs>(p => p
                .Add(x => x.TabTextAlign, TabTextAlign.Center)
                .AddChildContent(TwoPanes()));

            var navStyle = cut.Find(".el-tabs__nav").GetAttribute("style");
            Assert.Contains("justify-content:center", navStyle);
            // 不撑满容器宽度就无从居中：实测 .el-tabs__nav 带 float:left，栏宽会收缩到内容宽。
            Assert.Contains("min-width:100%", navStyle);
            foreach (var item in cut.FindAll(".el-tabs__item"))
            {
                Assert.Contains("justify-content:center", item.GetAttribute("style"));
            }
        }

        [Fact]
        public void TabTextAlignLeftMapsToFlexStart()
        {
            var cut = Render<ElTabs>(p => p
                .Add(x => x.TabTextAlign, TabTextAlign.Left)
                .AddChildContent(TwoPanes()));

            Assert.Contains("justify-content:flex-start", cut.Find(".el-tabs__nav").GetAttribute("style"));
        }

        [Fact]
        public void TabTextAlignNullEmitsNoJustifyButKeepsNavTransform()
        {
            var cut = Render<ElTabs>(p => p.AddChildContent(TwoPanes()));

            var navStyle = cut.Find(".el-tabs__nav").GetAttribute("style");
            Assert.DoesNotContain("justify-content", navStyle);
            Assert.DoesNotContain("min-width", navStyle);
            // 正向对照：读的是同一个属性，不是读空了才「不含」。
            Assert.Contains("transform: translateX(0px)", navStyle);
            Assert.DoesNotContain("justify-content", cut.Find(".el-tabs__item").GetAttribute("style"));
        }

        [Fact]
        public void VerticalTabPositionAlignsItemsOnly()
        {
            // 纵向时 .el-tabs__nav 是 flex-direction:column：justify-content 会变成上下分布、
            // min-width 会撑破栏宽，所以对齐只落在页签项上。
            var cut = Render<ElTabs>(p => p
                .Add(x => x.TabPosition, TabPosition.Left)
                .Add(x => x.TabTextAlign, TabTextAlign.Right)
                .AddChildContent(TwoPanes()));

            var navStyle = cut.Find(".el-tabs__nav").GetAttribute("style");
            Assert.DoesNotContain("justify-content", navStyle);
            Assert.DoesNotContain("min-width", navStyle);
            Assert.Contains("justify-content:flex-end", cut.Find(".el-tabs__item").GetAttribute("style"));
        }

        [Fact]
        public void BeforeLeaveReceivesNewNameFirstAndBlocksSwitch()
        {
            var seen = new List<string>();
            var cut = Render<ElTabs>(p => p
                .Add(x => x.BeforeLeave, new Func<string, string, Task<bool>>((newName, oldName) =>
                {
                    seen.Add(newName + "<-" + oldName);
                    return Task.FromResult(newName != "p2");
                }))
                .AddChildContent(ClosableTwoPanes()));

            cut.FindAll(".el-tabs__item")[1].Click();

            Assert.Single(seen);
            // EP tabs.tsx:147 是 beforeLeave(value, currentName.value)：第一个参数是要切去的页签。
            Assert.Equal("p2<-p1", seen[0]);
            Assert.Equal("p1", cut.Instance.CurrentName);
            Assert.Contains("is-active", cut.FindAll(".el-tabs__item")[0].GetAttribute("class"));
            Assert.DoesNotContain("is-active", cut.FindAll(".el-tabs__item")[1].GetAttribute("class"));
        }

        [Fact]
        public void IsRemovableParameterCascadesCloseIconToEveryPane()
        {
            // IsRemovable 曾经漏标 [Parameter]：演示页写 IsRemovable="true" 时它压根不会被赋值，
            // 结果只有显式 IsClosable 的那一个页签有 X（实测 3 个页签只有 1 个图标）。
            var cut = Render<ElTabs>(p => p
                .Add(x => x.Type, TabType.Card)
                .Add(x => x.IsRemovable, true)
                .AddChildContent(TwoPanes()));

            Assert.Equal(2, cut.FindAll(".el-tabs__item .el-icon-close").Count);
        }

        [Fact]
        public void BeforeLeaveIsNotConsultedForInitialActivation()
        {
            // 一个"没有当前页签就不放行"的回调如果参与初始化，整条页签栏会一个激活项都没有、内容区空白。
            var seen = new List<string>();
            var cut = Render<ElTabs>(p => p
                .Add(x => x.BeforeLeave, new Func<string, string, Task<bool>>((newName, oldName) =>
                {
                    seen.Add(newName + "<-" + (oldName ?? "null"));
                    return Task.FromResult(false);
                }))
                .AddChildContent(ClosableTwoPanes()));

            Assert.Empty(seen);
            Assert.Equal("p1", cut.Instance.CurrentName);
            Assert.Contains("is-active", cut.FindAll(".el-tabs__item")[0].GetAttribute("class"));

            // 正向对照：用户点击这条链路仍然会问，而且问完确实拦住了。
            cut.FindAll(".el-tabs__item")[1].Click();
            Assert.Equal(new[] { "p2<-p1" }, seen);
            Assert.Equal("p1", cut.Instance.CurrentName);
        }

        [Fact]
        public void BeforeLeaveAllowsSwitchWhenItReturnsTrue()
        {
            // 正向对照：同一条链路放行时必须真的切过去，否则上一条用例的「拦住了」是假绿。
            var cut = Render<ElTabs>(p => p
                .Add(x => x.BeforeLeave, new Func<string, string, Task<bool>>((newName, oldName) => Task.FromResult(true)))
                .AddChildContent(ClosableTwoPanes()));

            cut.FindAll(".el-tabs__item")[1].Click();

            Assert.Equal("p2", cut.Instance.CurrentName);
        }

        [Fact]
        public void ActivatingTheAlreadyActiveTabDoesNotRefireValueChanged()
        {
            // EP 的 setCurrentName（tabs.tsx:150）在目标名与当前激活项相同时直接 return，不重发
            // tab-change / update:modelValue。少了这一条，OnAfterRenderAsync 每轮渲染都会把同一个页签名
            // 再发一遍，使用方回调里一句 StateHasChanged 就自喂成无限渲染，整页主线程被吃满
            // （宿主首轮实测 60 秒 evaluate OK=0/blocked=8；分源复测钉死同一口径：卡死窗口内后端
            //  127.0.0.1:5282 请求 x0，而净包健康档用同一把尺子量得到 x10）。
            var fired = new List<string>();
            var cut = Render<ElTabs>(p => p
                .Add(x => x.ValueChanged, name => fired.Add(name))
                .AddChildContent(TwoPanes()));

            Assert.Equal(new[] { "p1" }, fired);

            // 再点一次当前页签：官方 tab-click 照发，但激活值没变 => 不能再发 ValueChanged。
            cut.FindAll(".el-tabs__item")[0].Click();
            Assert.Equal(new[] { "p1" }, fired);

            // 程序化重复激活走的是同一个内部口（OnAfterRenderAsync / OnParametersSet 都调它）。
            cut.InvokeAsync(() => cut.Instance.SetActivateTabAsync("p1"));
            Assert.Equal(new[] { "p1" }, fired);

            // 正向对照：真切换必须照发，否则上面的「没再发」是颗哑弹。
            cut.FindAll(".el-tabs__item")[1].Click();

            Assert.Equal(new[] { "p1", "p2" }, fired);
        }

        #region 隐藏容器里的表头测量必须有上限

        [Fact]
        public void VisibleActivePaneMeasuresAndPositionsTheActiveBar()
        {
            // 正向对照：先证明「测量→定位下划线」这条路是活的，否则下面的「有界」是颗哑弹。
            JSInterop.Setup<int>("getClientWidth", _ => true).SetResult(100);
            JSInterop.Setup<int>("getPaddingLeft", _ => true).SetResult(10);
            JSInterop.Setup<int>("getPaddingRight", _ => true).SetResult(10);
            JSInterop.Setup<int>("getOffsetLeft", _ => true).SetResult(5);

            var cut = Render<ElTabs>(p => p.AddChildContent(TwoPanes()));

            var style = cut.Find(".el-tabs__active-bar").GetAttribute("style");
            Assert.Contains("width: 80px", style);
            Assert.Contains("translateX(15px)", style);
        }

        [Fact]
        public void HiddenActivePaneGivesUpMeasuringAfterBoundedRetries()
        {
            // 页签在 display:none 的容器里时 clientWidth 恒为 0，减去 padding 永远是负数。
            // 旧实现把「量不到」当成「布局未就绪，下一轮再量」并无条件 Refresh，自己喂出渲染死循环
            // （宿主插桩档实测 12492 次测量/25 秒；本轮把上限去掉打复现档，28 秒内 10754 次、
            //  evaluate OK=0/blocked=4，且分源计数后端 127.0.0.1:5282 请求 x0＝页面卡在首次查询之前）。
            JSInterop.Setup<int>("getClientWidth", _ => true).SetResult(0);
            JSInterop.Setup<int>("getPaddingLeft", _ => true).SetResult(10);
            JSInterop.Setup<int>("getPaddingRight", _ => true).SetResult(10);
            JSInterop.Setup<int>("getOffsetLeft", _ => true).SetResult(0);

            var cut = Render<ElTabs>(p => p.AddChildContent(TwoPanes()));

            var measures = JSInterop.Invocations["getClientWidth"].Count;
            // 下界=1：测量确实跑过（不是整条路没被走到）；上界=8：重试收口在常数级，不再自循环。
            Assert.InRange(measures, 1, 8);
            // 没量出尺寸就不该给下划线写死宽度。
            Assert.DoesNotContain("width: 80px", cut.Find(".el-tabs__active-bar").GetAttribute("style"));
        }

        #endregion
        #endregion
    }
}
