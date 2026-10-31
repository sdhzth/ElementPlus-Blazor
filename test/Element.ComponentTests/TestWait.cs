using System;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// bUnit 的 <c>WaitForAssertion</c> / <c>WaitForState</c> / <c>WaitForElement</c> 默认只等 **1 秒**。
    /// 全量套件是并行跑的：CPU 争抢时这 1 秒会不够用，于是出现「单类复跑必过、全量偶发红」的假失败
    /// （本仓的 <c>ElFeedbackTests</c>、<c>ElAutocompleteTests</c> 反复如此，早期记录还写过「flaky 12↔13 属抖动」）。
    /// 统一用这里的显式超时，并由 <c>TimingGuardTests</c> 守住「不许再出现裸 WaitFor 调用」。
    /// </summary>
    internal static class TestWait
    {
        /// <summary>等待上限：慢机器 + 并行争抢也够用；真挂掉的用例不该靠小超时被误判成 flaky。</summary>
        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

        /// <summary>轮询间隔：保持小值，成功即返回，不会因此变慢。</summary>
        public static readonly int IntervalMilliseconds = 25;
    }
}
