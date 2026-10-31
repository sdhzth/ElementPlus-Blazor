using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// <see cref="ElButton.Visible"/>：工具条里「按权限/状态让某颗按钮此刻不存在」的开关。
    /// <para>
    /// 背景：业务侧（zyweb 生产订单页工具条）要在按钮组里按状态收起个别按钮。
    /// Element Plus 官方没有这个属性，靠父级 <c>v-if</c> 表达；Blazor 里等价写法是把整颗按钮包在
    /// <c>@if</c> 里，但每个宿主都要自己包一层，包漏了就变成一个看得见点不动的灰按钮。
    /// 按钮本身此前只有 <see cref="ElButton.Disabled"/>（看得见但点不动，还会从
    /// <see cref="ElButtonGroup"/> 级联下来），没有任何「隐藏」的口径。
    /// </para>
    /// <para>
    /// 六条判据：默认值为 true（加参数前的用法一字不变）；<c>false</c> 时 DOM 里连壳都不留
    /// （不是 <c>display:none</c>，否则占位缝还在）；<c>false</c> 的按钮点不到（没有节点可点）；
    /// true→false→true 反复切换渲染树不能崩（<c>ElButton</c> 是手写渲染树，末帧是 ElementReferenceCapture，
    /// 整棵子树进出正是当年 loading 态崩页的同一条路径，必须钉住）；
    /// 按钮组里收起中间一颗，前后两颗不受影响、组壳不塌；隐藏与禁用是两件事
    /// （<c>Disabled</c> 仍出 DOM 且带 disabled，<c>Visible=false</c> 直接没节点）。
    /// </para>
    /// </summary>
    public class ElButtonVisibleTests : BunitContext
    {
        public ElButtonVisibleTests()
        {
            Services.AddElementPlusServices();
            JSInterop.SetupVoid("execFocus", _ => true).SetVoidResult();
        }

        private static RenderFragment Text(string value) => builder => builder.AddContent(0, value);

        [Fact]
        public void VisibleDefaultsToTrueSoExistingUsageIsUnchanged()
        {
            var cut = Render<ElButton>(p => p.Add(x => x.ChildContent, Text("查询")));

            Assert.True(new ElButton().Visible);
            Assert.Single(cut.FindAll("button"));
            Assert.Contains("查询", cut.Find("button").TextContent);
        }

        [Fact]
        public void VisibleFalseRemovesTheButtonFromTheDomEntirely()
        {
            var cut = Render<ElButton>(p => p
                .Add(x => x.Visible, false)
                .Add(x => x.ChildContent, Text("打印订单表")));

            // 连包装都不留：如果实现是 display:none，按钮节点仍然存在、按钮组的缝也还占着
            Assert.Empty(cut.Nodes);
        }

        [Fact]
        public void HiddenButtonCannotBeClicked()
        {
            var clicked = false;
            Action<MouseEventArgs> onClick = _ => clicked = true;
            var cut = Render<ElButton>(p => p
                .Add(x => x.Visible, false)
                .Add(x => x.OnClick, EventCallback.Factory.Create(this, onClick))
                .Add(x => x.ChildContent, Text("生产制单")));

            // 没有节点可点：隐藏不是禁用（禁用还在 DOM 里，只是点不动）
            Assert.Empty(cut.FindAll("button"));
            Assert.False(clicked);
        }

        [Fact]
        public void TogglingVisibleKeepsTheRenderTreeDiffableAndRestoresTheButton()
        {
            var cut = Render<ElButton>(p => p.Add(x => x.ChildContent, Text("增加订单")));
            Assert.Single(cut.FindAll("button"));

            cut.Render(p => p
                .Add(x => x.Visible, false)
                .Add(x => x.ChildContent, Text("增加订单")));
            Assert.Empty(cut.FindAll("button"));

            cut.Render(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.ChildContent, Text("增加订单")));
            var button = cut.Find("button");
            Assert.Contains("增加订单", button.TextContent);
            // 重建后事件接线要还在（不是只剩一个静态壳）
            button.Click();
        }

        [Fact]
        public void HidingOneButtonInsideAGroupLeavesTheNeighboursAndTheGroupShellAlone()
        {
            var cut = Render<ElButtonGroup>(p => p.Add(x => x.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<ElButton>(0);
                builder.AddAttribute(1, nameof(ElButton.ChildContent), Text("查询"));
                builder.CloseComponent();

                builder.OpenComponent<ElButton>(10);
                builder.AddAttribute(11, nameof(ElButton.ChildContent), Text("实裁情况"));
                builder.AddAttribute(12, nameof(ElButton.Visible), false);
                builder.CloseComponent();

                builder.OpenComponent<ElButton>(20);
                builder.AddAttribute(21, nameof(ElButton.ChildContent), Text("打印订单表"));
                builder.CloseComponent();
            })));

            var group = cut.Find(".el-button-group");
            var buttons = group.QuerySelectorAll("button").ToArray();
            Assert.Equal(2, buttons.Length);
            Assert.Equal(new[] { "查询", "打印订单表" }, buttons.Select(x => x.TextContent));
        }

        [Fact]
        public void VisibleFalseIsNotTheSameAsDisabled()
        {
            var disabled = Render<ElButton>(p => p
                .Add(x => x.Disabled, true)
                .Add(x => x.ChildContent, Text("设计订单表")));

            // 禁用：看得见、点不动，节点带着 disabled 属性留在原地（还能给它挂 tooltip 说明为什么不能点）
            var button = disabled.Find("button");
            Assert.True(button.HasAttribute("disabled"));
            Assert.Contains("is-disabled", button.ClassList);

            var hidden = Render<ElButton>(p => p
                .Add(x => x.Visible, false)
                .Add(x => x.Disabled, true)
                .Add(x => x.ChildContent, Text("设计订单表")));
            // 隐藏：即便同时禁用了，也不该有任何节点剩下
            Assert.Empty(hidden.FindAll("button"));
        }
    }
}
