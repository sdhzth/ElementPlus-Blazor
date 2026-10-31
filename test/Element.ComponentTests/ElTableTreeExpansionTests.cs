using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElTable 本地树形数据的展开 / 折叠语义（EP 2.14.0 口径）。
    /// <para>
    /// EP 实抓：<c>store/tree.ts:175-191 toggleTreeExpansion</c> 只翻转被点节点自己的 <c>expanded</c>，
    /// 子孙的标志一律不动；<c>table-body/render-helper.ts:249</c> 的 <c>display: parent.display &amp;&amp; parent.expanded</c>
    /// 与 <c>:267</c> 的 <c>cur.display = !!(cur.expanded &amp;&amp; innerTreeRowData.display)</c> 合起来即
    /// 「一行可见 ⇔ 它的每一层祖先都 expanded」。
    /// </para>
    /// <para>
    /// 本库旧写法是「展开＝把整棵子树从 hiddenRows 里摘掉」：点父级一次就把孙子曾孙全摊开，
    /// 而里层节点自己的 <c>Expanded</c> 仍是 false —— 再点它的箭头第一下走「展开」分支、行本来就在，
    /// 等于什么也没变，第二下才轮到折叠，即用户报的「有时点击 2 次才行」。
    /// </para>
    /// </summary>
    public class ElTableTreeExpansionTests : BunitContext
    {
        public ElTableTreeExpansionTests()
        {
            Services.AddElementPlusServices();
            Services.AddSingleton(new HttpClient());
        }

        public class Org : TreeItemBase
        {
            public string Name { get; set; }
            public string Kind { get; set; }
        }

        /// <summary>
        /// 四级 10 节点、两个根（总院 / 东院区），够覆盖「根 → 系统 → 科室 → 病区」的每一层。
        /// </summary>
        private static List<Org> Tree() => new()
        {
            new Org { Id = 1, ParentId = 0, Level = 0, HasChildren = true, Name = "总院", Kind = "院区" },
            new Org { Id = 2, ParentId = 1, Level = 1, HasChildren = true, Name = "内科系统", Kind = "学科系统" },
            new Org { Id = 3, ParentId = 2, Level = 2, HasChildren = true, Name = "心血管内科", Kind = "科室" },
            new Org { Id = 4, ParentId = 3, Level = 3, Name = "心血管一病区", Kind = "病区" },
            new Org { Id = 5, ParentId = 3, Level = 3, Name = "心血管二病区", Kind = "病区" },
            new Org { Id = 6, ParentId = 2, Level = 2, HasChildren = true, Name = "呼吸内科", Kind = "科室" },
            new Org { Id = 7, ParentId = 6, Level = 3, Name = "呼吸重症监护室", Kind = "病区" },
            new Org { Id = 8, ParentId = 1, Level = 1, HasChildren = true, Name = "外科系统", Kind = "学科系统" },
            new Org { Id = 9, ParentId = 8, Level = 2, Name = "普外科", Kind = "科室" },
            new Org { Id = 10, ParentId = 0, Level = 0, HasChildren = true, Name = "东院区", Kind = "院区" },
            new Org { Id = 11, ParentId = 10, Level = 1, Name = "急诊科", Kind = "科室" },
        };

        private static RenderFragment TreeColumns() => new(b =>
        {
            var seq = 0;
            b.OpenComponent<ElTableTreeColumn>(seq++);
            b.AddAttribute(seq++, "Property", "Name");
            b.AddAttribute(seq++, "Text", "机构 / 科室");
            b.CloseComponent();

            b.OpenComponent<ElTableColumn>(seq++);
            b.AddAttribute(seq++, "Property", "Kind");
            b.AddAttribute(seq++, "Text", "类别");
            b.CloseComponent();
        });

        private IRenderedComponent<ElTable> RenderTree(List<Org> data)
        {
            return Render<ElTable>(p => p
                .Add(x => x.DataSource, data)
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.EnablePagination, false)
                .AddChildContent(TreeColumns()));
        }

        /// <summary>当前 DOM 里真实出现的行名，按文档序。</summary>
        private static List<string> VisibleNames(IRenderedComponent<ElTable> cut) =>
            cut.FindAll(".el-table__body tbody tr")
                .Select(tr => tr.QuerySelector(".el-table__cell .cell")?.TextContent?.Trim() ?? string.Empty)
                .ToList();

        /// <summary>点某一行树形列的展开箭头（一次真实单击）。</summary>
        private static void ClickCaret(IRenderedComponent<ElTable> cut, string name)
        {
            var row = cut.FindAll(".el-table__body tbody tr")
                .First(tr => (tr.QuerySelector(".el-table__cell .cell")?.TextContent ?? string.Empty).Trim() == name);
            var caret = row.QuerySelector(".el-table__expand-icon");
            Assert.NotNull(caret);
            caret.Click();
        }

        private static Org Node(List<Org> data, string name) => data.First(x => x.Name == name);

        [Fact]
        public async Task 折叠全部后只剩两个根节点()
        {
            var data = Tree();
            var cut = RenderTree(data);
            Assert.Equal(11, VisibleNames(cut).Count);   // 未折叠前整棵树平铺可见

            await cut.InvokeAsync(() => cut.Instance.CollapseAllTree());

            Assert.Equal(new[] { "总院", "东院区" }, VisibleNames(cut));
            Assert.All(data, x => Assert.False(x.Expanded));
        }

        [Fact]
        public async Task 单击根节点只展开一层_不摊开整棵子树()
        {
            var data = Tree();
            var cut = RenderTree(data);
            await cut.InvokeAsync(() => cut.Instance.CollapseAllTree());

            ClickCaret(cut, "总院");

            // 旧逻辑这里会一次冒出 8 行（内科系统 + 其子孙全摊开）
            Assert.Equal(new[] { "总院", "内科系统", "外科系统", "东院区" }, VisibleNames(cut));
            Assert.True(Node(data, "总院").Expanded);
            // 子孙的标志一律不动：EP toggleTreeExpansion 只碰被点的那一个
            Assert.False(Node(data, "内科系统").Expanded);
            Assert.False(Node(data, "心血管内科").Expanded);
            Assert.False(Node(data, "外科系统").Expanded);
        }

        [Fact]
        public async Task 里层节点第一击即展开_不需要点两次()
        {
            var data = Tree();
            var cut = RenderTree(data);
            await cut.InvokeAsync(() => cut.Instance.CollapseAllTree());
            ClickCaret(cut, "总院");
            var before = VisibleNames(cut);

            ClickCaret(cut, "内科系统");

            // 旧逻辑：这一下只翻标志、行本来就在 ⇒ DOM 零变化，用户要再点一下才看到东西
            Assert.NotEqual(before, VisibleNames(cut));
            Assert.Equal(new[] { "总院", "内科系统", "心血管内科", "呼吸内科", "外科系统", "东院区" }, VisibleNames(cut));
            Assert.True(Node(data, "内科系统").Expanded);
        }

        [Fact]
        public async Task 折叠父级隐藏全部子孙_重新展开回到原下钻状态()
        {
            var data = Tree();
            var cut = RenderTree(data);
            await cut.InvokeAsync(() => cut.Instance.CollapseAllTree());
            ClickCaret(cut, "总院");
            ClickCaret(cut, "内科系统");
            ClickCaret(cut, "心血管内科");
            Assert.Contains("心血管一病区", VisibleNames(cut));

            ClickCaret(cut, "总院");   // 收起顶层：子孙整体消失
            Assert.Equal(new[] { "总院", "东院区" }, VisibleNames(cut));

            ClickCaret(cut, "总院");   // 再展开：回到收起前的下钻状态（EP 靠标志保留做到的）
            Assert.Equal(new[] { "总院", "内科系统", "心血管内科", "心血管一病区", "心血管二病区", "呼吸内科", "外科系统", "东院区" },
                VisibleNames(cut));
            Assert.True(Node(data, "内科系统").Expanded);
            Assert.True(Node(data, "心血管内科").Expanded);
            Assert.False(Node(data, "呼吸内科").Expanded);
            Assert.DoesNotContain("呼吸重症监护室", VisibleNames(cut));
        }

        [Fact]
        public async Task 折叠中间层只收自己这一支_别的分支与祖先不受影响()
        {
            var data = Tree();
            var cut = RenderTree(data);
            await cut.InvokeAsync(() => cut.Instance.CollapseAllTree());
            ClickCaret(cut, "总院");
            ClickCaret(cut, "内科系统");
            ClickCaret(cut, "心血管内科");
            Assert.Contains("心血管一病区", VisibleNames(cut));

            ClickCaret(cut, "内科系统");   // 收起中间层：整支（含曾孙）消失，但自己那行还在

            Assert.Equal(new[] { "总院", "内科系统", "外科系统", "东院区" }, VisibleNames(cut));
            // 子孙自己的标志保留不动：再展开时原下钻状态自动回来
            Assert.False(Node(data, "内科系统").Expanded);
            Assert.True(Node(data, "心血管内科").Expanded);
            Assert.True(Node(data, "总院").Expanded);
        }

        [Fact]
        public async Task 箭头字形跟随本节点自身状态()
        {
            var data = Tree();
            var cut = RenderTree(data);
            await cut.InvokeAsync(() => cut.Instance.CollapseAllTree());

            Assert.Equal("up", Node(data, "总院").Direction);
            ClickCaret(cut, "总院");
            Assert.Equal("right", Node(data, "总院").Direction);

            var caret = cut.FindAll(".el-table__body tbody tr")
                .First(tr => (tr.QuerySelector(".el-table__cell .cell")?.TextContent ?? string.Empty).Trim() == "总院")
                .QuerySelector(".el-table__expand-icon i");
            Assert.Contains("el-icon-arrow-right", caret.GetAttribute("class"));
        }

        [Fact]
        public async Task 一行可见当且仅当它的每层祖先都展开()
        {
            var data = Tree();
            var cut = RenderTree(data);
            await cut.InvokeAsync(() => cut.Instance.CollapseAllTree());

            // 只展开曾祖 → 祖父这条链上的中间层， deepest 仍不该出现
            ClickCaret(cut, "总院");
            ClickCaret(cut, "内科系统");
            ClickCaret(cut, "呼吸内科");

            var visible = VisibleNames(cut);
            foreach (var node in data)
            {
                var shouldShow = AncestorsAllExpanded(data, node);
                Assert.Equal(shouldShow, visible.Contains(node.Name));
            }
            Assert.Contains("呼吸重症监护室", visible);
            Assert.DoesNotContain("心血管一病区", visible);
        }

        /// <summary>EP 口径的独立复算：自身不入账，只看祖先链是否全展开。</summary>
        private static bool AncestorsAllExpanded(List<Org> data, Org node)
        {
            var parentId = node.ParentId;
            while (parentId != 0)
            {
                var parent = data.FirstOrDefault(x => x.Id == parentId);
                if (parent == null)
                {
                    return true;   // 祖先不在行集：按顶层处理
                }
                if (!parent.Expanded)
                {
                    return false;
                }
                parentId = parent.ParentId;
            }
            return true;
        }

        // ===== 箭头的「有没有」：EP 2.14.0 config.ts:249 + store/tree.ts:52-67 =====

        /// <summary>
        /// 一个「假装有孩子」的叶子：Id=2 标了 HasChildren，但没有任何节点的 ParentId 指向它。
        /// 这正是用户 2026-10-10 截图里点了没反应的那类行。
        /// </summary>
        private static List<Org> BogusFlagTree() => new()
        {
            new Org { Id = 1, ParentId = 0, Level = 0, HasChildren = true, Name = "内科", Kind = "一级科室" },
            new Org { Id = 2, ParentId = 1, Level = 1, HasChildren = true, Name = "心血管内科", Kind = "亚专业" },
            new Org { Id = 3, ParentId = 1, Level = 1, Name = "呼吸内科", Kind = "亚专业" },
        };

        private static OrgNodeCaret CaretOf(IRenderedComponent<ElTable> cut, string name)
        {
            var row = cut.FindAll(".el-table__body tbody tr")
                .First(tr => (tr.QuerySelector(".el-table__cell .cell")?.TextContent ?? string.Empty).Trim() == name);
            return new OrgNodeCaret
            {
                HasCaret = row.QuerySelector(".el-table__expand-icon") != null,
                IndentStyle = row.QuerySelector(".el-table__indent")?.GetAttribute("style"),
            };
        }

        internal sealed class OrgNodeCaret
        {
            public bool HasCaret { get; set; }
            public string IndentStyle { get; set; }
        }

        [Fact]
        public void 本地数据里叶子即使误标HasChildren也不画箭头()
        {
            var cut = RenderTree(BogusFlagTree());

            // 反向侧：这一行没有孩子 ⇒ 不该出现点了没反应的死箭头（改前是 HasChildren 优先，箭头常驻）
            Assert.False(CaretOf(cut, "心血管内科").HasCaret);
            Assert.False(CaretOf(cut, "呼吸内科").HasCaret);
            // 但缩进必须保留，否则「一级科室 / 亚专业」两列的文字会错位
            Assert.Contains("padding-left: 32px", CaretOf(cut, "心血管内科").IndentStyle);
        }

        [Fact]
        public void 本地数据里真有孩子的节点照常画箭头()
        {
            var cut = RenderTree(BogusFlagTree());

            // 同前提的正向对照：只有 ParentId 链上真有孩子才画 —— 别把判据写反成"一律不画"
            Assert.True(CaretOf(cut, "内科").HasCaret);
            Assert.Contains("padding-left: 0px", CaretOf(cut, "内科").IndentStyle);
        }

        /// <summary>
        /// 懒加载（设了 <c>Url</c>）的那一半：那一刻子节点还没取回来，EP 正是靠
        /// <c>lazyColumnIdentifier</c>（默认 <c>hasChildren</c>，<c>store/tree.ts:20</c>）放行箭头，
        /// 加载完拿到空数组才用 <c>noLazyChildren</c> 撤掉（<c>render-helper.ts:234</c>）。
        /// 本地那条规则不能把这一半一起砍掉，否则异步树全部塌成没有箭头。
        /// </summary>
        [Fact]
        public void 懒加载模式仍按HasChildren画箭头()
        {
            var roots = new List<Org>
            {
                new Org { Id = 1, ParentId = 0, Level = 0, HasChildren = true, Name = "总院", Kind = "院区" },
                new Org { Id = 2, ParentId = 0, Level = 0, Name = "门诊楼", Kind = "楼" },
            };
            Services.AddSingleton(new HttpClient(new LazyTreeHandler(JsonSerializer.Serialize(roots)))
            {
                BaseAddress = new Uri("http://localhost/"),
            });

            var cut = Render<ElTable>(p => p
                .Add(x => x.Url, "/api/tree")
                .Add(x => x.DataType, typeof(Org))
                .Add(x => x.AutoExpandAll, false)
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .AddChildContent(TreeColumns()));
            cut.WaitForState(() => cut.FindAll(".el-table__body tbody tr").Count == 2, timeout: TestWait.Timeout);

            Assert.True(CaretOf(cut, "总院").HasCaret);
            Assert.False(CaretOf(cut, "门诊楼").HasCaret);
        }

        /// <summary>只喂根列表；子节点 URL 返回空数组，本用例不触发加载。</summary>
        private sealed class LazyTreeHandler : HttpMessageHandler
        {
            private readonly string rootsJson;

            public LazyTreeHandler(string rootsJson)
            {
                this.rootsJson = rootsJson;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var body = request.RequestUri.AbsolutePath == "/api/tree" ? rootsJson : "[]";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                });
            }
        }
    }
}
