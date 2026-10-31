using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElIpAddressGrow（IPv4 四段输入）契约测试。
    /// <para>
    /// 分工：值模型（拆分、只留数字、截到三位、钳到 255、合成点分串）在 C# 侧，所以本文件全部可测；
    /// 跨格移动/全选/粘贴摊格在 <c>elementIpAddressGrowAttach</c>（浏览器侧），本文件只验「attach 恰好发生一次」，
    /// 真实键入与粘贴行为由 demo/tools/probes/verify-batch9.cjs 的量测覆盖。
    /// </para>
    /// </summary>
    public class ElIpAddressGrowTests : BunitContext
    {
        public ElIpAddressGrowTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private IRenderedComponent<ElIpAddressGrow> RenderIp(string value, List<string> captured = null)
        {
            return Render<ElIpAddressGrow>(parameters =>
            {
                if (value != null)
                {
                    parameters.Add(x => x.Value, value);
                }

                if (captured != null)
                {
                    parameters.Add(x => x.ValueChanged,
                        EventCallback.Factory.Create<string>(this, v => captured.Add(v)));
                }
            });
        }

        private static List<string> CellValues(IRenderedComponent<ElIpAddressGrow> cut)
            => cut.FindAll("input.el-ip-address-grow__cell")
                .Select(i => i.GetAttribute("value") ?? string.Empty)
                .ToList();

        [Fact]
        public void RendersFourCellsAndThreeDots()
        {
            var cut = RenderIp("192.168.1.10");

            var cells = cut.FindAll("input.el-ip-address-grow__cell");
            Assert.Equal(4, cells.Count);
            Assert.Equal(3, cut.FindAll(".el-ip-address-grow__dot").Count);
            Assert.All(cells, c => Assert.Equal("3", c.GetAttribute("maxlength")));
            // 四段各自有可读的无障碍名，缺一个就等于少一段可定位的输入
            for (var i = 0; i < 4; i++)
            {
                Assert.Contains($"第 {i + 1} 段", cells[i].GetAttribute("aria-label"));
            }
        }

        [Fact]
        public void ValueSplitsIntoSegments()
        {
            var cut = RenderIp("192.168.1.10");

            Assert.Equal(new[] { "192", "168", "1", "10" }, CellValues(cut));
        }

        [Fact]
        public void MissingSegmentsFallBackToZero()
        {
            var cut = RenderIp("10.1");

            Assert.Equal(new[] { "10", "1", "0", "0" }, CellValues(cut));
        }

        /// <summary>
        /// 规范化契约：超 255 钳到 255、超三位截断、非数字剔除。
        /// 有牙的地方：把 <see cref="ElIpAddressGrow"/> 的钳位或截断任一条去掉，本用例即红。
        /// </summary>
        [Fact]
        public void InvalidOctetsAreClampedTruncatedAndStripped()
        {
            var cut = RenderIp("999.1234.ab12.7");

            Assert.Equal(new[] { "255", "123", "12", "7" }, CellValues(cut));
        }

        [Fact]
        public void NullValueRendersAllZero()
        {
            var cut = RenderIp(null);

            Assert.Equal(new[] { "0", "0", "0", "0" }, CellValues(cut));
        }

        [Fact]
        public void TypingIntoACellEmitsComposedValue()
        {
            var emitted = new List<string>();
            var cut = RenderIp("1.2.3.4", emitted);

            cut.FindAll("input.el-ip-address-grow__cell")[1].Input("56");

            Assert.Equal("1.56.3.4", emitted.Last());
            Assert.Equal("1.56.3.4", cut.Instance.Value);
        }

        [Fact]
        public void TypedValueIsNormalizedBeforeEmitting()
        {
            var emitted = new List<string>();
            var cut = RenderIp("1.2.3.4", emitted);

            cut.FindAll("input.el-ip-address-grow__cell")[0].Input("9999");

            // 截到三位再钳位：9999 → 999 → 255
            Assert.Equal("255.2.3.4", emitted.Last());
        }

        /// <summary>
        /// 空段是「正在编辑」的中间态：<c>Value</c> 里按 0 合成，格子里保持空，
        /// 并且父级按 <c>@bind-Value</c> 回灌同一个字符串时不能把空段填成 "0"（那会打断用户继续输入）。
        /// </summary>
        [Fact]
        public void EmptyCellComposesAsZeroAndSurvivesBindRoundTrip()
        {
            var emitted = new List<string>();
            var cut = RenderIp("1.2.3.4", emitted);

            cut.FindAll("input.el-ip-address-grow__cell")[0].Input("");

            Assert.Equal("0.2.3.4", emitted.Last());
            Assert.Equal("", CellValues(cut)[0]);

            cut.Render(parameters => parameters.Add(x => x.Value, "0.2.3.4"));

            Assert.Equal("", CellValues(cut)[0]);
        }

        /// <summary>父级真的换了值时才回填四段（与上一个用例同前提、反方向）。</summary>
        [Fact]
        public void ParentValueChangeRefillsSegments()
        {
            var cut = RenderIp("1.2.3.4");

            cut.Render(parameters => parameters.Add(x => x.Value, "10.0.0.255"));

            Assert.Equal(new[] { "10", "0", "0", "255" }, CellValues(cut));
        }

        [Fact]
        public async Task PasteCallbackWritesAllFourSegments()
        {
            var emitted = new List<string>();
            var cut = RenderIp("1.2.3.4", emitted);

            // 与浏览器一致：JS 回调在渲染调度器上执行
            await cut.InvokeAsync(() => cut.Instance.TriggerUpdate(10, 0, 0, 255));

            Assert.Equal("10.0.0.255", emitted.Last());
            Assert.Equal(new[] { "10", "0", "0", "255" }, CellValues(cut));
        }

        [Fact]
        public async Task OutOfRangePasteCallbackIsClamped()
        {
            var emitted = new List<string>();
            var cut = RenderIp("1.2.3.4", emitted);

            await cut.InvokeAsync(() => cut.Instance.TriggerUpdate(999, -5, 300, 0));

            Assert.Equal("255.0.255.0", emitted.Last());
        }

        [Fact]
        public void DisabledAndReadonlyRenderOnEveryCell()
        {
            var cut = Render<ElIpAddressGrow>(parameters => parameters
                .Add(x => x.Value, "1.1.1.1")
                .Add(x => x.Disabled, true));

            Assert.Contains("is-disabled", cut.Find(".el-ip-address-grow").ClassList);
            Assert.All(cut.FindAll("input.el-ip-address-grow__cell"), c => Assert.True(c.HasAttribute("disabled")));
        }

        [Fact]
        public void ReadonlyRendersOnEveryCellAndKeepsTabOrder()
        {
            var cut = Render<ElIpAddressGrow>(parameters => parameters
                .Add(x => x.Value, "1.1.1.1")
                .Add(x => x.Readonly, true));

            Assert.All(cut.FindAll("input.el-ip-address-grow__cell"), c => Assert.True(c.HasAttribute("readonly")));
            // 只读≠禁用：不能顺手带上 is-disabled（外观与 Tab 序都不同）
            Assert.DoesNotContain("is-disabled", cut.Find(".el-ip-address-grow").ClassList);
        }

        [Theory]
        [InlineData(InputSize.Large)]
        [InlineData(InputSize.Small)]
        [InlineData(InputSize.Normal)]
        public void SizeSelectsAtMostOneModifierClass(InputSize size)
        {
            var cut = Render<ElIpAddressGrow>(parameters => parameters
                .Add(x => x.Value, "1.1.1.1")
                .Add(x => x.Size, size));

            var root = cut.Find(".el-ip-address-grow");
            var large = root.ClassList.Contains("el-ip-address-grow--large");
            var small = root.ClassList.Contains("el-ip-address-grow--small");

            Assert.Equal(size == InputSize.Large, large);
            Assert.Equal(size == InputSize.Small, small);
            // Normal 两个修饰类都不该有：这是「漏了分支就两个都挂上/都不挂」的反向对照
            Assert.False(large && small);
        }

        /// <summary>
        /// JS 只挂一次：重复渲染（父级状态变化、@bind 回写）不能再叠一层监听器，
        /// 否则一次按键会被处理多次（跨格跳两格、退格吃掉两位）。
        /// </summary>
        [Fact]
        public void AttachesJsExactlyOnceAcrossRerenders()
        {
            var cut = RenderIp("1.2.3.4");

            cut.Render(parameters => parameters.Add(x => x.Value, "1.2.3.5"));
            cut.Render(parameters => parameters.Add(x => x.Value, "1.2.3.6"));

            Assert.Single(JSInterop.Invocations, i => i.Identifier == "elementIpAddressGrowAttach");
        }

        [Fact]
        public async Task DisposeDetachesJsListeners()
        {
            var cut = RenderIp("1.2.3.4");
            await cut.Instance.DisposeAsync();

            Assert.Contains(JSInterop.Invocations, i => i.Identifier == "elementIpAddressGrowDetach");
        }
    }
}
