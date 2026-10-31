using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 弹层宿主的隔离契约：存在<b>第二个</b> <c>ElPopup</c> 宿主时（真实场景＝Blazor Server 上的第二个
    /// circuit，测试场景＝另一个并行测试类），不许把第一个宿主正在用的弹层队列换走。
    /// <para>
    /// 这条就是那条"老 flaky"第 5 层（两个弹层类并行互相干扰）的真身：
    /// <c>ElPopup.OnInitialized</c> 里对弹层集合做的是
    /// "-= 自己的处理器 → 把集合整个换成新实例 → += 自己的处理器"。
    /// 只要那个集合是<b>跨宿主共享</b>的（<c>DialogService.Dialogs</c> 曾经是 static），
    /// 后初始化的宿主就会让先初始化的宿主订阅到一个已经没人写入的旧集合 →
    /// 弹层永远不出现（症状与第 1 层一模一样：结果 Task 不完成、FindAll 数量为 0）。
    /// </para>
    /// </summary>
    public class PopupHostIsolationTests
    {
        private static BunitContext NewContext()
        {
            // ElPopup / ElDialog 会 execFocus 之类的 JS 调用；没配 Loose 会抛未处理 interop 异常
            var context = new BunitContext();
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            context.Services.AddElementPlusServices();
            return context;
        }

        /// <summary>与 ElFeedbackTests 同一个门：每轮主动排干调度队列，不等"下次渲染"来叫醒。</summary>
        private static async Task WaitUntilAsync(IRenderedComponent<ElPopup> popup,
            Func<bool> condition, string because)
        {
            var deadline = DateTime.UtcNow + TestWait.Timeout;
            while (DateTime.UtcNow < deadline)
            {
                await popup.InvokeAsync(() => { });
                if (condition())
                {
                    return;
                }
                await Task.Delay(25);
            }

            Assert.True(false, "等待超时（" + TestWait.Timeout.TotalSeconds + "s）：" + because);
        }

        [Fact]
        public async Task SecondPopupHostDoesNotStealTheFirstHostsDialogQueue()
        {
            using var contextA = NewContext();
            var hostA = contextA.Render<ElPopup>();

            // 第二个宿主：另一个作用域（另一个 circuit / 另一个并行测试类）里正常初始化一个弹层宿主
            using var contextB = NewContext();
            var hostB = contextB.Render<ElPopup>();

            var messageBoxA = contextA.Services.GetRequiredService<MessageBox>();
            await hostA.InvokeAsync(() => { _ = messageBoxA.AlertAsync("只有 A 该看见的提示"); });

            await WaitUntilAsync(hostA, () => hostA.FindAll(".el-message-box").Count > 0,
                "B 宿主初始化之后，A 宿主的 MessageBox 再也不出现 —— 说明 A、B 共用了一份弹层队列，"
                + "而 B 的 OnInitialized 把它换成了自己的新集合");

            Assert.Contains("只有 A 该看见的提示", hostA.Markup);

            var inB = await hostB.InvokeAsync(() => hostB.FindAll(".el-message-box").Count);
            Assert.Equal(0, inB);   // 反向：A 的弹层也不许漏进 B 的宿主
        }
    }
}
