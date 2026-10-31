using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElVideoPlayerGrow 的释放契约（批次⑨：关掉页签后视频还在放）。
    /// <para>
    /// 这里只管「.NET 销毁时必须把 JS 侧实例摘掉」这一条：真正的停播、摘监听、删注册表发生在
    /// <c>elvideoplayergrow.js</c> 的 <c>detach()</c>，bUnit 看不见，由
    /// <c>demo/tools/probes/diag-batch9-video.cjs</c> 在浏览器里量（含 detach 能停播的正对照）。
    /// </para>
    /// <para>
    /// 键名也是契约的一部分：<c>players</c> 注册表用容器 div 的 id（组件 <c>_uid</c>）作键，
    /// 不是 <c>video</c> 元素的 id（那是 <c>_uid + "-video"</c>）。探针第一版拿错了键，
    /// 于是「注册表已摘」的断言恒真 —— 所以这里显式断言传给 detach 的参数等于容器 id。
    /// </para>
    /// </summary>
    public class ElVideoPlayerGrowDisposeTests : BunitContext
    {
        public ElVideoPlayerGrowDisposeTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private IRenderedComponent<ElVideoPlayerGrow> RenderPlayer()
        {
            var cut = Render<ElVideoPlayerGrow>(parameters => parameters
                .Add(p => p.Src, "https://example.com/clip.mp4")
                .Add(p => p.Controls, true));

            // 挂载脚本是 OnAfterRenderAsync 里做的，先等它落地（后面的断言都以「已 attach」为前提）
            cut.WaitForState(() => JSInterop.Invocations.Any(i => i.Identifier == "elVideoPlayerGrowAttach"), timeout: TestWait.Timeout);
            return cut;
        }

        private int DetachCallCount() =>
            JSInterop.Invocations.Count(i => i.Identifier == "elVideoPlayerGrowDetach");

        /// <summary>正对照：attach 用的键就是容器 id，detach 必须用同一个键，否则注册表永远摘不掉。</summary>
        [Fact]
        public async Task DisposeAsyncDetachesWithContainerIdKey()
        {
            var cut = RenderPlayer();
            var containerId = cut.Find(".el-video-player-grow").GetAttribute("id");
            var attachKey = JSInterop.Invocations
                .First(i => i.Identifier == "elVideoPlayerGrowAttach").Arguments[0] as string;

            Assert.False(string.IsNullOrEmpty(containerId));
            Assert.Equal(attachKey, containerId);
            Assert.Equal(0, DetachCallCount());

            await cut.Instance.DisposeAsync();

            Assert.Equal(1, DetachCallCount());
            Assert.Equal(containerId,
                JSInterop.Invocations.First(i => i.Identifier == "elVideoPlayerGrowDetach").Arguments[0] as string);
        }

        [Fact]
        public async Task DisposeAsyncIsIdempotent()
        {
            var cut = RenderPlayer();

            await cut.Instance.DisposeAsync();
            await cut.Instance.DisposeAsync();

            Assert.Equal(1, DetachCallCount());
        }

        /// <summary>异步通道之外还要认同步通道：宿主只调 Dispose 时同样要把 JS 实例摘掉。</summary>
        [Fact]
        public void SyncDisposeAlsoRequestsDetach()
        {
            var cut = RenderPlayer();
            Assert.Equal(0, DetachCallCount());

            cut.Instance.Dispose();

            Assert.Equal(1, DetachCallCount());
        }

        /// <summary>同步与异步两条通道共用同一个闸门：先走哪条都只摘一次，另一条再来就是 no-op。</summary>
        [Fact]
        public async Task SyncThenAsyncDisposeDetachesOnlyOnce()
        {
            var cut = RenderPlayer();

            cut.Instance.Dispose();
            await cut.Instance.DisposeAsync();

            Assert.Equal(1, DetachCallCount());
        }

        /// <summary>
        /// attach 没成功过（脚本加载失败）时不该发 detach —— 组件此时在 JS 侧根本没有注册表项。
        /// 用 Strict 互操作造这个前提：连 <c>import</c> 都没有处理器，attach 那步必然抛异常，
        /// 组件走「静默降级」分支（同文件里其余用例的 attach 记录就是同一前提的正对照）。
        /// </summary>
        [Fact]
        public void DisposeWithoutSuccessfulAttachDoesNotDetach()
        {
            JSInterop.Mode = JSRuntimeMode.Strict;

            var cut = Render<ElVideoPlayerGrow>(parameters => parameters.Add(p => p.Src, "https://example.com/clip.mp4"));

            Assert.Contains("import", JSInterop.Invocations.Select(i => i.Identifier));
            Assert.DoesNotContain("elVideoPlayerGrowAttach", JSInterop.Invocations.Select(i => i.Identifier));
            Assert.Equal(0, DetachCallCount());

            cut.Instance.Dispose();

            Assert.Equal(0, DetachCallCount());
        }
    }
}
