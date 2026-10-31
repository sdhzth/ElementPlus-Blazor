using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 批次⑭：保活货架「新开一页时，另外 N-1 页（含它们的整棵页面组件树）要不要跟着重跑」的三形态对照。
    /// <para>
    /// 被测对象是演示外壳 <c>demo/Element.Blazor.Demo/KeepAliveRouteView.cs</c> 的货架形态。那个类是 demo
    /// 程序集里的 <c>private</c> 嵌套类型，测试拿不到，所以这里用同构模型，并把真实结构照抄成两层：
    /// 货架 → 每页宿主（真代码的 <c>CachedPage</c>）→ 页面组件（真代码的 <c>TableDemo</c> 等）。
    /// 两层的渲染次数都单独计数，因为**只有内层不重跑才算省下成本**。
    /// </para>
    /// <para>实测结论（2026-10-10，本文件把这三条钉住）：</para>
    /// <list type="bullet">
    /// <item><description>旧形态 <c>AddContent(RenderFragment)</c>：父级每重渲染一次，每个缓存页的整棵页面树重跑一遍（内层 4 次）。</description></item>
    /// <item><description>「把参数换成引用可比的 RouteData + bool」并**不**跳过子组件：宿主与页面组件仍各 4 次 ——
    /// Blazor 的 <c>SetParametersAsync</c> 不比较值，参数没变也照样重渲染。</description></item>
    /// <item><description>只有宿主自己带 <c>ShouldRender</c> 闸门才真跳过：宿主 1 次、页面组件 1 次，
    /// 且闸门吞掉时不产出任何帧（内层计数不涨）。</description></item>
    /// </list>
    /// <para>
    /// 为什么要在 .NET 侧数：浏览器侧的时长断言分不开这几态（单页成本 ~55ms，远在 90ms 门槛之下，
    /// 见 <c>docs/pro-tabs-alignment.md</c> §12.4 那次没判别成功的翻面），而"有没有重跑渲染树"是纯 .NET 事实。
    /// 计数一律自己数 <see cref="BuildRenderTree"/>，不用 bUnit 的 <c>RenderCount</c>（理由见 <c>TableEditCellTests</c>）。
    /// </para>
    /// </summary>
    public class KeepAliveShelfRenderFanOutTests : BunitContext
    {
        /// <summary>按 "页名#out" / "页名#in" 统计渲染次数。页名带用例前缀，避免 xUnit 并行类串味。</summary>
        private static readonly ConcurrentDictionary<string, int> Builds = new();

        private static void Hit(string name) => Builds.AddOrUpdate(name, 1, (_, v) => v + 1);

        private static int CountOf(string name) => Builds.TryGetValue(name, out var v) ? v : 0;

        private static int Outer(List<PageData> pages) => pages.Sum(x => CountOf(x.Name + "#out"));

        private static int Inner(List<PageData> pages) => pages.Sum(x => CountOf(x.Name + "#in"));

        /// <summary>等价于真货架里的 <c>RouteData</c>：整场用同一实例，只做引用比对。</summary>
        private sealed class PageData
        {
            public PageData(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }

        /// <summary>页面组件（真代码里的 TableDemo / GrowDemo…）：内层计数器。</summary>
        private sealed class PageBody : ComponentBase
        {
            [Parameter] public PageData Data { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                Hit(Data.Name + "#in");
                builder.OpenElement(0, "span");
                builder.AddContent(1, Data.Name);
                builder.CloseElement();
            }
        }

        /// <summary>
        /// 每页宿主（真代码里的 <c>CachedPage</c>）。<paramref name="UseGate"/> 决定要不要自带
        /// <see cref="ShouldRender"/> 闸门 —— 闸门必须**在 BuildRenderTree 里播种**上一次的值，
        /// 因为首帧不走 ShouldRender，漏播种就会让第 2 轮白渲染一次（本批第一次实测就踩在这）。
        /// </summary>
        private sealed class PageHost : ComponentBase
        {
            [Parameter] public PageData Data { get; set; }

            [Parameter] public bool Mounted { get; set; }

            [Parameter] public bool UseGate { get; set; }

            private PageData lastData;

            private bool lastMounted;

            protected override bool ShouldRender()
            {
                if (!UseGate)
                {
                    return true;
                }

                return !ReferenceEquals(lastData, Data) || lastMounted != Mounted;
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                lastData = Data;
                lastMounted = Mounted;
                Hit(Data.Name + "#out");

                if (!Mounted)
                {
                    builder.AddContent(0, "占位");
                    return;
                }

                builder.OpenComponent<PageBody>(0);
                builder.AddAttribute(1, nameof(PageBody.Data), Data);
                builder.CloseComponent();
            }
        }

        /// <summary>改前形态：每页一个 <c>RenderFragment</c> 字段，货架用 <c>AddContent</c> 直接展开整棵页面树。</summary>
        private sealed class OldShelf : ComponentBase
        {
            [Parameter] public int Version { get; set; }

            [Parameter] public List<PageData> Pages { get; set; } = new();

            private List<RenderFragment> fragments;

            private List<PageData> builtFor;

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                // 片段只建一次（对应 Entry.Fragment 在入缓存时生成）；但 AddContent 每轮都会重新执行它。
                if (fragments == null || !ReferenceEquals(builtFor, Pages))
                {
                    builtFor = Pages;
                    fragments = Pages.Select(p => (RenderFragment)(b =>
                    {
                        Hit(p.Name + "#out");
                        b.OpenComponent<PageBody>(0);
                        b.AddAttribute(1, nameof(PageBody.Data), p);
                        b.CloseComponent();
                    })).ToList();
                }

                var seq = 0;
                for (var i = 0; i < fragments.Count; i++)
                {
                    builder.OpenElement(seq++, "div");
                    builder.AddContent(seq, fragments[i]);
                    builder.CloseElement();
                }
            }
        }

        /// <summary>
        /// 货架：每页一个独立子组件。<see cref="UseGate"/> 区分「只靠参数比对」(false)
        /// 与「宿主自带 ShouldRender 闸门」(true) 两态，其余完全相同。
        /// </summary>
        private sealed class NewShelf : ComponentBase
        {
            [Parameter] public int Version { get; set; }

            [Parameter] public List<PageData> Pages { get; set; } = new();

            [Parameter] public bool UseGate { get; set; }

            /// <summary>正对照开关：把这一页的 <c>Mounted</c> 翻掉，它必须真的重渲染。</summary>
            [Parameter] public string UnmountedName { get; set; } = string.Empty;

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                var seq = 0;
                for (var i = 0; i < Pages.Count; i++)
                {
                    var entry = Pages[i];
                    builder.OpenElement(seq++, "div");
                    builder.OpenComponent<PageHost>(seq++);
                    builder.AddAttribute(seq++, nameof(PageHost.Data), entry);
                    builder.AddAttribute(seq++, nameof(PageHost.Mounted), entry.Name != UnmountedName);
                    builder.AddAttribute(seq++, nameof(PageHost.UseGate), UseGate);
                    builder.CloseComponent();
                    builder.CloseElement();
                }
            }
        }

        private static List<PageData> Pages(string prefix, int n) =>
            Enumerable.Range(0, n).Select(i => new PageData(prefix + "-" + i)).ToList();

        private const int RenderRounds = 4;

        private IRenderedComponent<OldShelf> RenderOld(List<PageData> pages) =>
            Render<OldShelf>(p => p.Add(x => x.Version, 1).Add(x => x.Pages, pages));

        private void RerenderOld(IRenderedComponent<OldShelf> cut, List<PageData> pages, int version) =>
            cut.Render(p => p.Add(x => x.Version, version).Add(x => x.Pages, pages));

        private IRenderedComponent<NewShelf> RenderNew(List<PageData> pages, bool useGate) =>
            RenderNew(pages, useGate, string.Empty);

        private IRenderedComponent<NewShelf> RenderNew(List<PageData> pages, bool useGate, string unmountedName) =>
            Render<NewShelf>(p => p
                .Add(x => x.Version, 1)
                .Add(x => x.Pages, pages)
                .Add(x => x.UseGate, useGate)
                .Add(x => x.UnmountedName, unmountedName));

        private void RerenderNew(IRenderedComponent<NewShelf> cut, List<PageData> pages, bool useGate, int version, string unmountedName) =>
            cut.Render(p => p
                .Add(x => x.Version, version)
                .Add(x => x.Pages, pages)
                .Add(x => x.UseGate, useGate)
                .Add(x => x.UnmountedName, unmountedName));

        /// <summary>
        /// 旧形态基线：货架重渲染 4 次（对应「新开一页」的两阶段挂载连着几次 <c>version++</c>），
        /// 6 个已缓存页的**页面组件**每页被重跑 4 遍 —— 成本 = 页数 × 轮数。
        /// </summary>
        [Fact]
        public void OldShelf_AddContentFragment_RerunsEveryCachedPageTreeEachRound()
        {
            var pages = Pages("old", 6);
            var cut = RenderOld(pages);
            for (var v = 2; v <= RenderRounds; v++)
            {
                RerenderOld(cut, pages, v);
            }

            Assert.Equal(pages.Count * RenderRounds, Inner(pages));
            Assert.Equal(RenderRounds, pages.Select(x => CountOf(x.Name + "#in")).Distinct().Single());
            // 片段被执行 4 次 = 旧形态没有「宿主组件」这一层，整棵页面树每轮直接重跑
            Assert.Equal(pages.Count * RenderRounds, Outer(pages));
        }

        /// <summary>
        /// **实测否证**：只把参数换成引用可比的 <c>PageData</c>(=真代码的 RouteData) + <c>bool</c>，
        /// 宿主和页面组件仍旧每轮各跑一次（6 页 × 4 轮）。Blazor 的 <c>SetParametersAsync</c> 不比值，
        /// 参数没变也会重渲染 —— 所以「拆出 CachedPage 子组件」本身**不是**省成本的原因。
        /// </summary>
        [Fact]
        public void NewShelf_WithoutGate_ParametersAloneDoNotSkipCachedPages()
        {
            var pages = Pages("new", 6);
            var cut = RenderNew(pages, useGate: false);
            for (var v = 2; v <= RenderRounds; v++)
            {
                RerenderNew(cut, pages, false, v, string.Empty);
            }

            Assert.Equal(RenderRounds, pages.Select(x => CountOf(x.Name + "#out")).Distinct().Single());
            Assert.Equal(RenderRounds, pages.Select(x => CountOf(x.Name + "#in")).Distinct().Single());
        }

        /// <summary>
        /// 真省成本的形态：宿主自带 <c>ShouldRender</c> 闸门。同样 6 页、同样 4 轮，
        /// 宿主与页面组件都只渲染 1 次 —— 新开第 N 页只付第 N 页的钱。
        /// </summary>
        [Fact]
        public void NewShelf_WithShouldRenderGate_CachedPagesRenderExactlyOnce()
        {
            var pages = Pages("memo", 6);
            var cut = RenderNew(pages, useGate: true);
            for (var v = 2; v <= RenderRounds; v++)
            {
                RerenderNew(cut, pages, true, v, string.Empty);
            }

            Assert.Equal(1, pages.Select(x => CountOf(x.Name + "#out")).Distinct().Single());
            Assert.Equal(1, pages.Select(x => CountOf(x.Name + "#in")).Distinct().Single());
            Assert.Equal(pages.Count, Outer(pages));
            Assert.Equal(pages.Count, Inner(pages));
        }

        /// <summary>
        /// 上一条的**同前提正向对照**（也证明闸门没有把该渲染的一起吞掉）：
        /// 目标页先以 <c>Mounted=false</c> 入架（只画占位，内层页面组件一次都没跑），
        /// 第三轮翻成 <c>true</c> —— 该页宿主 +1、**内层页面组件从 0 变 1**，其余 5 页两个计数都不动。
        /// </summary>
        [Fact]
        public void NewShelf_WithGate_ParameterFlip_RerendersOnlyThatPage()
        {
            var pages = Pages("mflip", 6);
            var target = pages[2];
            var cut = RenderNew(pages, true, target.Name);

            // 普通一轮（每页参数都没变）→ 全被吞
            RerenderNew(cut, pages, true, 2, target.Name);
            Assert.Equal(1, CountOf(target.Name + "#out"));
            Assert.Equal(0, CountOf(target.Name + "#in"));
            Assert.Contains("占位", cut.Markup);

            // 只翻目标页的 Mounted（false → true）→ 只有它 +1，内层页面组件跟着被建出来
            RerenderNew(cut, pages, true, 3, string.Empty);
            Assert.Equal(2, CountOf(target.Name + "#out"));
            Assert.Equal(1, CountOf(target.Name + "#in"));
            Assert.DoesNotContain("占位", cut.Markup);
            Assert.Equal(new List<int> { 1 }, pages.Where(x => x != target).Select(x => CountOf(x.Name + "#out")).Distinct().ToList());
            Assert.Equal(new List<int> { 1 }, pages.Where(x => x != target).Select(x => CountOf(x.Name + "#in")).Distinct().ToList());
        }

        /// <summary>
        /// 规模口径：页 3→6 时旧形态/无闸门形态按「页数 × 轮数」涨，闸门形态只按「页数」涨。
        /// </summary>
        [Fact]
        public void Scale_UngatedGrowsWithPagesTimesRounds_GatedGrowsOnlyWithPages()
        {
            Assert.Equal(3 * RenderRounds, RunInner(ShelfKind.Old, "so3", 3));
            Assert.Equal(6 * RenderRounds, RunInner(ShelfKind.Old, "so6", 6));
            Assert.Equal(3 * RenderRounds, RunInner(ShelfKind.NoGate, "sn3", 3));
            Assert.Equal(6 * RenderRounds, RunInner(ShelfKind.NoGate, "sn6", 6));
            Assert.Equal(3, RunInner(ShelfKind.Gated, "sg3", 3));
            Assert.Equal(6, RunInner(ShelfKind.Gated, "sg6", 6));
        }

        private enum ShelfKind
        {
            Old,
            NoGate,
            Gated
        }

        private int RunInner(ShelfKind kind, string prefix, int n)
        {
            var pages = Pages(prefix, n);
            if (kind == ShelfKind.Old)
            {
                var cut = RenderOld(pages);
                for (var v = 2; v <= RenderRounds; v++)
                {
                    RerenderOld(cut, pages, v);
                }

                return Inner(pages);
            }

            var useGate = kind == ShelfKind.Gated;
            var newCut = RenderNew(pages, useGate);
            for (var v = 2; v <= RenderRounds; v++)
            {
                RerenderNew(newCut, pages, useGate, v, string.Empty);
            }

            return Inner(pages);
        }
    }
}
