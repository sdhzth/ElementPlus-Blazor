using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// <c>Visible</c> 口径铺开：<see cref="ElRow"/>、<see cref="ElSpace"/>、<see cref="ElProgress"/>、
    /// <see cref="ElTabPane"/> 各补一个真参数，语义与既有的 <see cref="ElButton.Visible"/> 一致——
    /// <c>false</c> ⇒ 此刻不存在（不进渲染树），不是 <c>display:none</c>，也不是禁用。
    /// <para>
    /// 背景：业务侧（电子档案 DocPrint / DocView 的卡片·表格双模式、DocRemake 的三个模式块与「处理中」条、
    /// DocBorrow / DocLost / DocSeale 的「归还·找回·解封」页签）一直就在这些组件上写 <c>Visible="@(...)"</c>。
    /// 而库里只有 ElButton 声明过它，其余的会被 <see cref="ElementComponentBase"/> 上的
    /// <c>[Parameter(CaptureUnmatchedValues = true)] Attributes</c> 静默吞掉、当成普通 HTML 属性吐出去
    /// ⇒ 该收起的块一直挂着：卡片模式与表格模式同屏、空态与数据同屏、按状态本不该出现的页签也一直在。
    /// 宿主侧的等价写法是自己包一层 <c>@if</c>，包漏一次就是整页乱（批次101 的 DocRemake 就是这么塌的）。
    /// </para>
    /// <para>
    /// 页签比另三个多一层：内容区渲染的是 <c>ElTabs.ActiveTab.ChildContent</c>，不是页签自己的渲染输出，
    /// 所以 <c>Visible=false</c> 必须同时管住「注册」与「激活」，否则只藏页签头会让内容区停在一个已摘掉的页签上。
    /// </para>
    /// </summary>
    public class ElVisibleParameterTests : BunitContext
    {
        public ElVisibleParameterTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static RenderFragment Text(string value) => builder => builder.AddContent(0, value);

        /// <summary>
        /// 死属性的病根是「组件上根本没声明这个参数」。这条把口径钉在类型上：以后谁把参数删了、改名了，
        /// 或者默认值从 true 滑走，测试先红，而不是在生产页面上多出一块。
        /// </summary>
        [Fact]
        public void VisibleIsADeclaredBoolParameterDefaultingToTrue()
        {
            var types = new[] { typeof(ElRow), typeof(ElSpace), typeof(ElProgress), typeof(ElTabPane), typeof(ElButton) };

            foreach (var type in types)
            {
                var property = type.GetProperty("Visible", BindingFlags.Public | BindingFlags.Instance);
                Assert.NotNull(property);
                Assert.Equal(typeof(bool), property.PropertyType);
                Assert.NotNull(property.GetCustomAttribute<ParameterAttribute>());
                Assert.True((bool)property.GetValue(Activator.CreateInstance(type)), $"{type.Name} 的 Visible 默认值必须是 true");
            }
        }

        [Fact]
        public void ElRowDefaultsToVisibleAndRendersItsShellAndChildren()
        {
            var cut = Render<ElRow>(p => p
                .Add(x => x.Gutter, 16)
                .AddChildContent(Text("卡片方式显示")));

            var row = cut.Find(".el-row");
            Assert.Contains("卡片方式显示", row.TextContent);
        }

        [Fact]
        public void ElRowVisibleFalseLeavesNoShellAndNoChildSubtree()
        {
            var cut = Render<ElRow>(p => p
                .Add(x => x.Visible, false)
                .Add(x => x.Gutter, 16)
                .AddChildContent(Text("卡片方式显示")));

            // 连壳都不留：实现若是 display:none，el-row 的负外边距与占位还在，页面上就是一条推不掉的缝
            Assert.Empty(cut.Nodes);
            Assert.Empty(cut.FindAll(".el-row"));
        }

        [Fact]
        public void TogglingElRowVisibleBackRestoresTheSubtree()
        {
            var cut = Render<ElRow>(p => p.AddChildContent(Text("表格方式显示")));
            Assert.Single(cut.FindAll(".el-row"));

            cut.Render(p => p
                .Add(x => x.Visible, false)
                .AddChildContent(Text("表格方式显示")));
            Assert.Empty(cut.FindAll(".el-row"));

            cut.Render(p => p
                .Add(x => x.Visible, true)
                .AddChildContent(Text("表格方式显示")));
            Assert.Contains("表格方式显示", cut.Find(".el-row").TextContent);
        }

        [Fact]
        public void ElSpaceHonoursVisibleWithoutLeavingTheGapContainerBehind()
        {
            var shown = Render<ElSpace>(p => p
                .Add(x => x.Wrap, true)
                .AddChildContent(Text("模式切换")));
            Assert.Single(shown.FindAll(".el-space"));

            var hidden = Render<ElSpace>(p => p
                .Add(x => x.Visible, false)
                .Add(x => x.Wrap, true)
                .AddChildContent(Text("模式切换")));
            Assert.Empty(hidden.Nodes);
        }

        [Fact]
        public void ElProgressHonoursVisibleWithoutLeavingAnEmptyTrack()
        {
            var shown = Render<ElProgress>(p => p.Add(x => x.Percentage, 40));
            Assert.Single(shown.FindAll("[role=progressbar]"));

            var hidden = Render<ElProgress>(p => p
                .Add(x => x.Visible, false)
                .Add(x => x.Percentage, 40));
            // 「处理中」收起时应该连整条轨道的高度都消失，而不是留一条灰槽
            Assert.Empty(hidden.FindAll(".el-progress"));
            Assert.Empty(hidden.Nodes);
        }

        private static RenderFragment TwoPanes(bool firstVisible = true, bool secondVisible = true) => builder =>
        {
            builder.OpenComponent<ElTabPane>(0);
            builder.AddAttribute(1, nameof(ElTabPane.Name), "p1");
            builder.AddAttribute(2, nameof(ElTabPane.Title), "借阅");
            builder.AddAttribute(3, nameof(ElTabPane.Visible), firstVisible);
            builder.AddAttribute(4, nameof(ElTabPane.ChildContent), (RenderFragment)(b => b.AddContent(0, "内容一")));
            builder.CloseComponent();

            builder.OpenComponent<ElTabPane>(10);
            builder.AddAttribute(11, nameof(ElTabPane.Name), "p2");
            builder.AddAttribute(12, nameof(ElTabPane.Title), "归还");
            builder.AddAttribute(13, nameof(ElTabPane.Visible), secondVisible);
            builder.AddAttribute(14, nameof(ElTabPane.ChildContent), (RenderFragment)(b => b.AddContent(0, "内容二")));
            builder.CloseComponent();
        };

        [Fact]
        public void BothPanesVisibleIsTheUnchangedBaseline()
        {
            var cut = Render<ElTabs>(p => p.AddChildContent(TwoPanes()));

            // 尺子要有牙：两条都可见时页签头与内容都找得到，后面的负向断言才有对照
            Assert.Equal(2, cut.FindAll(".el-tabs__item").Count);
            Assert.Contains("内容一", cut.Find(".el-tabs__content").TextContent);
        }

        [Fact]
        public void HiddenPaneHasNeitherHeaderNorContent()
        {
            var cut = Render<ElTabs>(p => p.AddChildContent(TwoPanes(secondVisible: false)));

            var headers = cut.FindAll(".el-tabs__item");
            Assert.Single(headers);
            Assert.Contains("借阅", headers[0].TextContent);
            var content = cut.Find(".el-tabs__content").TextContent;
            Assert.Contains("内容一", content);
            Assert.DoesNotContain("内容二", content);
        }

        [Fact]
        public void HidingTheActivePaneReactivatesTheNextOneAndSwapsTheContent()
        {
            var cut = Render<ElTabs>(p => p
                .Add(x => x.ModelValue, "p2")
                .AddChildContent(TwoPanes()));
            Assert.Contains("内容二", cut.Find(".el-tabs__content").TextContent);

            cut.Render(p => p
                .Add(x => x.ModelValue, "p2")
                .AddChildContent(TwoPanes(secondVisible: false)));

            // 内容区渲染的是 ActiveTab.ChildContent：只藏页签头、不换位，这里就会停在已摘掉的 p2 上（空白）
            Assert.Equal("p1", cut.Instance.CurrentName);
            var headers = cut.FindAll(".el-tabs__item");
            Assert.Single(headers);
            Assert.Contains("is-active", headers[0].ClassList);
            Assert.Contains("内容一", cut.Find(".el-tabs__content").TextContent);
        }

        [Fact]
        public void VisibleFalseDoesNotStealActivationWhenThePaneComesBack()
        {
            var cut = Render<ElTabs>(p => p
                .Add(x => x.ModelValue, "p2")
                .AddChildContent(TwoPanes()));

            cut.Render(p => p
                .Add(x => x.ModelValue, "p2")
                .AddChildContent(TwoPanes(secondVisible: false)));
            Assert.Equal("p1", cut.Instance.CurrentName);

            // 条件翻回来（宿主的状态量变了，比如同一棵渲染树从「已借出」行换成「正常」行）：页签要回来，
            // 但不许把 p1 的激活位抢走——摘掉时没 DeActivate 的话，「挑激活项」那段就会误选它
            cut.Render(p => p
                .Add(x => x.ModelValue, "p1")
                .AddChildContent(TwoPanes()));

            Assert.Equal(2, cut.FindAll(".el-tabs__item").Count);
            Assert.Equal("p1", cut.Instance.CurrentName);
            Assert.Contains("内容一", cut.Find(".el-tabs__content").TextContent);
            Assert.DoesNotContain("is-active", cut.FindAll(".el-tabs__item")[1].ClassList);
        }

        [Fact]
        public void WhenOnlyOnePaneIsVisibleItBecomesTheActiveOne()
        {
            var cut = Render<ElTabs>(p => p.AddChildContent(TwoPanes(firstVisible: false)));

            var headers = cut.FindAll(".el-tabs__item");
            Assert.Single(headers);
            Assert.Contains("归还", headers[0].TextContent);
            Assert.Contains("is-active", headers[0].ClassList);
            Assert.Contains("内容二", cut.Find(".el-tabs__content").TextContent);
        }

        [Fact]
        public void HiddenPaneIsNotTheSameAsDisabledPane()
        {
            var disabled = Render<ElTabs>(p => p.AddChildContent((RenderFragment)(builder =>
            {
                builder.OpenComponent<ElTabPane>(0);
                builder.AddAttribute(1, nameof(ElTabPane.Name), "p1");
                builder.AddAttribute(2, nameof(ElTabPane.Title), "借阅");
                builder.AddAttribute(3, nameof(ElTabPane.ChildContent), Text("内容一"));
                builder.CloseComponent();

                builder.OpenComponent<ElTabPane>(10);
                builder.AddAttribute(11, nameof(ElTabPane.Name), "p2");
                builder.AddAttribute(12, nameof(ElTabPane.Title), "归还");
                builder.AddAttribute(13, nameof(ElTabPane.Disabled), true);
                builder.AddAttribute(14, nameof(ElTabPane.ChildContent), Text("内容二"));
                builder.CloseComponent();
            })));

            // 禁用：条上还在、只是点不动（DocManage 的入口按 u_status 收起时用 Visible，用错成 Disabled 就是一个灰页签）
            var headers = disabled.FindAll(".el-tabs__item");
            Assert.Equal(2, headers.Count);
            Assert.Contains("is-disabled", headers[1].ClassList);
            // aria-disabled 是布尔值属性，Blazor 按「条件属性」渲染：启用侧整条属性不存在，禁用侧存在
            Assert.False(headers[0].HasAttribute("aria-disabled"));
            Assert.True(headers[1].HasAttribute("aria-disabled"));

            var hidden = Render<ElTabs>(p => p.AddChildContent(TwoPanes(secondVisible: false)));
            Assert.Single(hidden.FindAll(".el-tabs__item"));
        }
    }
}
