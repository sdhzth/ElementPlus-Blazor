using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// <see cref="FullScreenService"/> 守卫：zyweb 迁移（Radzen/BootstrapBlazor → Element）时，
    /// 顶栏全屏按钮依赖 BootstrapBlazor 的 <c>FullScreenService</c>，本库没有对应实现，
    /// 故补齐同名服务 + <c>dom.js</c> 里的 <c>elementFullscreen*</c> 通道。
    /// <para>
    /// bUnit 没有真实 Fullscreen API（也没有布局引擎，见 <c>docs/testing-guards.md</c>），
    /// 所以这里守三件事：服务可从 <c>AddElementServices()</c> 解析、C# 调用的标识符与 dom.js
    /// 里定义的全局函数一一对应（改名字就会红）、JS 返回值原样透传给调用方。
    /// 真实进入/退出全屏由浏览器探针验证。
    /// </para>
    /// </summary>
    public class FullScreenServiceTests : BunitContext
    {
        public FullScreenServiceTests()
        {
            Services.AddElementServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static string DomJs([CallerFilePath] string file = null)
        {
            // .../test/Element.ComponentTests/FullScreenServiceTests.cs → 上溯三层到仓库根
            var directory = new FileInfo(file).Directory;
            var root = directory.Parent.Parent.FullName;
            var path = Path.Combine(root, "src", "Components", "wwwroot", "js", "dom.js");
            Assert.True(File.Exists(path), $"找不到 dom.js：{path}");
            return File.ReadAllText(path);
        }

        [Fact]
        public void AddElementServicesRegistersFullScreenService()
        {
            // 与其余浮层/对话框服务同一口径：AddElementServices 里注册成 Scoped，宿主 @inject 即可用
            Assert.NotNull(Services.GetRequiredService<FullScreenService>());
        }

        [Fact]
        public async Task IsFullScreenAsyncReturnsJsState()
        {
            var service = Services.GetRequiredService<FullScreenService>();
            JSInterop.Setup<bool>("elementIsFullscreen", _ => true).SetResult(true);

            Assert.True(await service.IsFullScreenAsync());
        }

        [Fact]
        public async Task EnterAsyncWithoutTargetUsesDocumentAndReturnsState()
        {
            var service = Services.GetRequiredService<FullScreenService>();
            JSInterop.Setup<bool>("elementFullscreenEnter", _ => true).SetResult(true);

            Assert.True(await service.EnterAsync());

            var invocation = JSInterop.Invocations.Single(x => x.Identifier == "elementFullscreenEnter");
            Assert.Empty(invocation.Arguments);
        }

        [Fact]
        public async Task EnterAsyncWithTargetPassesElementReference()
        {
            var service = Services.GetRequiredService<FullScreenService>();
            JSInterop.Setup<bool>("elementFullscreenEnter", _ => true).SetResult(true);
            var cut = Render<ElButton>();

            Assert.True(await service.EnterAsync(cut.Instance.ButtonReference));

            var invocation = JSInterop.Invocations.Last(x => x.Identifier == "elementFullscreenEnter");
            Assert.Single(invocation.Arguments);
            Assert.IsType<ElementReference>(invocation.Arguments[0]);
        }

        [Fact]
        public async Task ExitAsyncReturnsStateAfterExit()
        {
            var service = Services.GetRequiredService<FullScreenService>();
            JSInterop.Setup<bool>("elementFullscreenExit", _ => true).SetResult(false);

            Assert.False(await service.ExitAsync());
            Assert.Contains(JSInterop.Invocations, x => x.Identifier == "elementFullscreenExit");
        }

        [Fact]
        public async Task ToggleAsyncUsesToggleChannelOnce()
        {
            var service = Services.GetRequiredService<FullScreenService>();
            JSInterop.Setup<bool>("elementFullscreenToggle", _ => true).SetResult(true);

            Assert.True(await service.ToggleAsync());

            var invocation = JSInterop.Invocations.Single(x => x.Identifier == "elementFullscreenToggle");
            Assert.Empty(invocation.Arguments);
        }

        [Fact]
        public void ToggleWithTargetPassesElementReference()
        {
            var service = Services.GetRequiredService<FullScreenService>();
            JSInterop.Setup<bool>("elementFullscreenToggle", _ => true).SetResult(false);
            var cut = Render<ElButton>();

            var task = service.ToggleAsync(cut.Instance.ButtonReference);

            var invocation = JSInterop.Invocations.Single(x => x.Identifier == "elementFullscreenToggle");
            Assert.Single(invocation.Arguments);
            Assert.True(task.IsCompletedSuccessfully);
        }

        [Fact]
        public void DomJsDefinesEveryFullScreenChannelFunction()
        {
            var js = DomJs();

            // C# 侧调用的标识符必须和 dom.js 里定义的全局函数同名，否则运行时才报 JS 函数不存在
            foreach (var name in new[]
                     {
                         "elementIsFullscreen", "elementFullscreenEnter",
                         "elementFullscreenExit", "elementFullscreenToggle"
                     })
            {
                Assert.Contains($"window.{name} = function", js);
            }
        }

        [Fact]
        public void FullScreenJsStaysEs5AndPrefixedApiOnly()
        {
            var js = DomJs();
            var start = js.IndexOf("window.elementIsFullscreen = function", StringComparison.Ordinal);
            Assert.True(start >= 0);
            // 取到最后一个函数（toggle）结尾为止
            var end = js.IndexOf("window.elementFullscreenToggle", StringComparison.Ordinal);
            Assert.True(end > start);
            var block = js.Substring(start, js.Length - start);

            // 本库 JS 只允许 ES5：Chrome 90 及以下对 const/箭头函数的兼容虽存在，但口径统一比省字重要
            Assert.False(block.Contains("=>"), "dom.js 全屏段出现箭头函数");
            Assert.False(block.Contains("let "), "dom.js 全屏段出现 let");
            Assert.False(block.Contains("const "), "dom.js 全屏段出现 const");
            // 全屏失败的 Promise rejection 必须回落成布尔值，不能让 JS 异常冒到 C#
            Assert.Contains("['catch']", block);
            Assert.Contains("webkitRequestFullscreen", block);
        }
    }
}
