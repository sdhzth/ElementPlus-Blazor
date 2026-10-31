using Bunit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 浮层（Loading / 消息 / 通知 / 对话框 / 下拉面板）的<b>归属与生命周期契约</b>。
    /// <para>
    /// 这些浮层都不是原地渲染的：组件把浮层模型注册进服务上的集合，由<em>唯一一个</em>
    /// <see cref="ElPopup"/> 宿主渲染。<c>ElPopup.OnInitialized</c> 会对每个集合做
    /// 「解订阅 → 换成新实例 → 再订阅」，也就是<b>接管队列</b>。所以有两件事必须成立：
    /// </para>
    /// <list type="number">
    /// <item>存放队列的服务必须<em>按作用域</em>注册。注册成 Singleton（或集合写成 static）时，
    /// 后来初始化的宿主会接管前一个宿主正在用的队列 —— Blazor Server 上就是后开的连接
    /// 接管前一个连接的遮罩/对话框；表现为"遮罩关不掉"或"调用后什么都不出现"，且不报错。
    /// <c>DialogService.Dialogs</c> 以前是 static（见 <c>PopupHostIsolationTests</c>），
    /// <c>LoadingService</c> 以前注册成 Singleton（本类第 2 条钉住它）。</item>
    /// <item>一个作用域内只能挂一个宿主。挂第二个是宿主方的用法错误，本类第 3、4 条分别钉住
    /// "运行时能数出来并告警"与"演示站自己不许违反"。</item>
    /// </list>
    /// <para>取证与来龙去脉见 <c>docs/popup-host-isolation.md</c>。</para>
    /// </summary>
    public class PopupHostScopeTests : BunitContext
    {
        public PopupHostScopeTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddElementPlusServices();
        }

        private static readonly Type[] OverlayServiceTypes =
        {
            typeof(LoadingService),
            typeof(MessageService),
            typeof(NotificationService),
            typeof(DialogService),
            typeof(PopupService),
            typeof(MessageBox),
            // 这两个同样持有"由唯一宿主渲染"的队列（ElDialogGrowHost 在 App.razor 里与 ElPopup 并列挂载）
            typeof(ElDialogGrowService),
        };

        /// <summary>
        /// 契约：所有持有浮层队列的服务一律 <see cref="ServiceLifetime.Scoped"/>。
        /// 任何一条被改成 Singleton，下一个宿主就会接管上一个宿主的队列（第 2 条用例是它的实参版）。
        /// </summary>
        [Fact]
        public void AllPopupQueueServicesAreScoped()
        {
            var services = new ServiceCollection();
            services.AddElementPlusServices();

            var offenders = new List<string>();
            foreach (var type in OverlayServiceTypes)
            {
                var descriptor = services.FirstOrDefault(x => x.ServiceType == type);
                Assert.True(descriptor != null, type.Name + " 没有被 AddElementPlusServices 注册");
                if (descriptor.Lifetime != ServiceLifetime.Scoped)
                {
                    offenders.Add(type.Name + " = " + descriptor.Lifetime);
                }
            }

            Assert.True(offenders.Count == 0, "浮层队列服务必须按作用域注册，实际：" + string.Join("、", offenders));
        }

        /// <summary>
        /// 两个作用域（＝Blazor Server 上的两条连接）不许共享同一份 Loading 队列，
        /// 也不许共享同一个集合实例：<c>ElPopup</c> 会把集合整个换掉，共享实例就等于跨连接接管。
        /// 这条是"注册成 Singleton"时的直接复现（改回 Singleton 必红）。
        /// </summary>
        [Fact]
        public void TwoScopesDoNotShareTheLoadingQueue()
        {
            var provider = new ServiceCollection().AddElementPlusServices().BuildServiceProvider();
            using var scopeA = provider.CreateScope();
            using var scopeB = provider.CreateScope();

            var loadingA = scopeA.ServiceProvider.GetRequiredService<LoadingService>();
            var loadingB = scopeB.ServiceProvider.GetRequiredService<LoadingService>();

            Assert.NotSame(loadingA, loadingB);
            Assert.NotSame(loadingA.LoadingOptions, loadingB.LoadingOptions);

            // 正向对照：作用域内的两次取值必须是同一个实例，否则上面那条 NotSame 只是因为"每次都是新的"，
            // 并没有证明"每个连接一份、连接内部共享"这个真正想要的形状。
            Assert.Same(loadingA, scopeA.ServiceProvider.GetRequiredService<LoadingService>());
        }

        /// <summary>
        /// 同一作用域挂第二个宿主时必须能<em>观测到</em>（计数 + 控制台告警）。
        /// 钉住这条是因为这件事原本完全静默：演示站的 LoginLayout 多挂了一个宿主，
        /// 导致访问过 /login 之后整站的 Loading / MessageBox 再也不出现，而且 0 个 JS 异常。
        /// </summary>
        [Fact]
        public void SecondPopupHostInSameScopeIsCounted()
        {
            var popupService = Services.GetRequiredService<PopupService>();
            Assert.Equal(0, popupService.PopupHostCount);

            Render<ElPopup>();
            Assert.Equal(1, popupService.PopupHostCount);

            Render<ElPopup>();
            Assert.Equal(2, popupService.PopupHostCount);
        }

        private static string RepoRoot()
        {
            var dir = new Uri(typeof(PopupHostScopeTests).Assembly.Location).LocalPath;
            for (var i = 0; i < 10 && !string.IsNullOrEmpty(dir); i++)
            {
                if (File.Exists(Path.Combine(dir, "src", "Components", "Element.csproj")))
                {
                    return dir;
                }

                dir = Path.GetDirectoryName(dir);
            }

            throw new DirectoryNotFoundException("找不到仓库根（src/Components/Element.csproj）");
        }

        private static readonly Regex RazorComment = new Regex(@"@\*[\s\S]*?\*@", RegexOptions.CultureInvariant);
        private static readonly Regex HtmlComment = new Regex(@"<!--[\s\S]*?-->", RegexOptions.CultureInvariant);
        private static readonly Regex CodeBlock = new Regex(@"<code>[\s\S]*?</code>", RegexOptions.CultureInvariant);
        private static readonly Regex HostMount = new Regex(@"<ElPopup(\s*/|\s+[A-Za-z])|<ElPopup>", RegexOptions.CultureInvariant);

        /// <summary>掩掉注释与示例代码块，只留真正会挂载的元素。</summary>
        private static string MountableMarkup(string razor)
        {
            var stripped = RazorComment.Replace(razor, " ");
            stripped = HtmlComment.Replace(stripped, " ");
            stripped = CodeBlock.Replace(stripped, " ");
            return stripped;
        }

        /// <summary>
        /// 掩码自身的双侧夹具：文档/注释里写到 <c>&lt;ElPopup /&gt;</c> 不算挂载，真挂载必须算。
        /// 没有这条，上面的扫描器一旦写坏就会"什么都不匹配"而假绿。
        /// </summary>
        [Fact]
        public void HostMountScannerIgnoresCommentsAndCodeSamples()
        {
            Assert.True(HostMount.IsMatch(MountableMarkup("<div>\n  <ElPopup />\n</div>")), "真实挂载必须被识别");
            Assert.False(HostMount.IsMatch(MountableMarkup("@* 说明：<ElPopup /> 只挂一次 *@\n<div />")), "Razor 注释里的不算");
            Assert.False(HostMount.IsMatch(MountableMarkup("<!-- <ElPopup /> -->\n<div />")), "HTML 注释里的不算");
            Assert.False(HostMount.IsMatch(MountableMarkup("<code>这里写的是 <ElPopup /> 标记</code>")), "示例代码块里的不算");
        }

        /// <summary>
        /// 演示站自己必须遵守"一个应用一个宿主"：<c>&lt;ElPopup /&gt;</c> 只允许出现在 App.razor。
        /// 这条就是把 /login 那次事故钉成机器判据（布局里再挂一个就红）。
        /// </summary>
        [Fact]
        public void DemoMountsThePopupHostOnlyOnce()
        {
            var root = Path.Combine(RepoRoot(), "demo");
            var mountFiles = new List<string>();
            foreach (var file in Directory.EnumerateFiles(root, "*.razor", SearchOption.AllDirectories))
            {
                if (file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                    || file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
                {
                    continue;
                }

                if (HostMount.IsMatch(MountableMarkup(File.ReadAllText(file))))
                {
                    mountFiles.Add(Path.GetFileName(file));
                }
            }

            Assert.Equal(new[] { "App.razor" }, mountFiles.Distinct().OrderBy(x => x).ToArray());
        }
    }
}
