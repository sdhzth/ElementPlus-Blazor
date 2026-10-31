using Bunit;
using ElementPlus;
using ElementPlus.X;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElButton 手写渲染树的序列号回归护栏。
    ///
    /// 历史缺陷：ButtonElement / BuildButtonContent / RenderIcon 用 `var seq = 0; seq++` 连续分配序列号，
    /// 而其中若干帧是条件帧（aria-busy、autofocus、tabindex、链接三件套、图标块、Loading 内容、文字 span）。
    /// 任一条件帧增减都会把其后所有帧的序列号整体平移，末帧 AddElementReferenceCapture
    /// 于是与旧树里同序列的「异类型帧」相撞，Blazor diff 抛
    /// NotImplementedException: Unexpected frame type during RemoveOldFrame: ElementReferenceCapture
    /// —— 表现是「按钮一进 loading 态，整页渲染失败」。
    /// 修法是给每个帧位分配固定槽位号（可选帧跳过也不占用后续编号）。本文件锁住这一点。
    /// </summary>
    public class ElButtonRenderTreeDiffTests : BunitContext
    {
        public ElButtonRenderTreeDiffTests()
        {
            Services.AddElementServices();
            JSInterop.SetupVoid("execFocus", _ => true).SetVoidResult();
            JSInterop.Setup<int[]>("elementMentionGetSelection", _ => true).SetResult(new[] { 2, 2 });
            JSInterop.SetupVoid("elementMentionSetSelection", _ => true).SetVoidResult();
        }

        private static RenderFragment Text(string value) => builder => builder.AddContent(0, value);

        [Fact]
        public void LoadingToggle_KeepsRenderTreeDiffableAndUpdatesDom()
        {
            var cut = Render<ElButton>(p => p.Add(x => x.ChildContent, Text("发送")));

            cut.Render(p => p.Add(x => x.Loading, true));

            // 不抛异常之外，还必须真的进入 loading 观感：aria-busy + 图标位存在
            var button = cut.Find("button");
            Assert.Equal("true", button.GetAttribute("aria-busy"));
            Assert.Contains("is-loading", button.ClassList);
            Assert.Contains("发送", button.TextContent);
        }

        [Fact]
        public void IconAppears_KeepsRenderTreeDiffable()
        {
            var cut = Render<ElButton>(p => p.Add(x => x.ChildContent, Text("搜索")));

            cut.Render(p => p.Add(x => x.Icon, "el-icon-search"));

            Assert.Single(cut.FindAll("button .el-icon-search"));
        }

        [Fact]
        public void IconPositionSwap_KeepsRenderTreeDiffable()
        {
            var cut = Render<ElButton>(p => p
                .Add(x => x.Icon, "el-icon-search")
                .Add(x => x.ChildContent, Text("下一步")));

            cut.Render(p => p.Add(x => x.IconPosition, ElButton.IconPositionMode.Right));

            var children = cut.Find("button").QuerySelectorAll("*").ToArray();
            Assert.Equal("el-icon-search", children.Last().ClassList.First(c => c.StartsWith("el-icon")));
        }

        [Fact]
        public void DisabledToggle_WithoutFrameCountChange_StillWorks()
        {
            var cut = Render<ElButton>(p => p.Add(x => x.ChildContent, Text("保存")));

            cut.Render(p => p.Add(x => x.Disabled, true));

            Assert.True(cut.Find("button").HasAttribute("disabled"));
        }

        [Fact]
        public void ButtonReferenceSurvivesLoadingToggle()
        {
            var cut = Render<ElButton>(p => p.Add(x => x.ChildContent, Text("发送")));
            var before = cut.Instance.ButtonReference;

            cut.Render(p => p.Add(x => x.Loading, true));

            Assert.False(before.Equals(default(ElementReference)), "首次渲染就该捕获到元素引用");
            Assert.False(cut.Instance.ButtonReference.Equals(default(ElementReference)), "切换 Loading 后引用仍需有效");
        }

        /// <summary>原始报障现场：ElXSender 的发送按钮随 Loading 变化，曾让整个组件 diff 崩溃。</summary>
        [Fact]
        public void SenderLoadingToggle_KeepsTextareaAndStopButton()
        {
            var cut = Render<ElXSender>(p => p
                .Add(x => x.Value, "Explain Blazor")
                .Add(x => x.OnSubmit, _ => { })
                .Add(x => x.OnStop, () => { })
                .Add(x => x.SubmitOnEnter, false)
                .Add(x => x.HelpText, "Shift Enter sends"));

            cut.Render(p => p.Add(x => x.Loading, true));

            Assert.Single(cut.FindAll("textarea"));
            Assert.Single(cut.FindAll(".el-button--danger"));   // 停止按钮已插入
            Assert.Contains("is-loading", cut.Find("section").ClassList);
        }
    }
}
