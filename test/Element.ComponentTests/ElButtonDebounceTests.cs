using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElButtonDebounceTests : BunitContext
    {
        public ElButtonDebounceTests()
        {
            Services.AddElementPlusServices();
        }

        private IRenderedComponent<ElButton> RenderButton(int debounce, bool leading, EventCallback<MouseEventArgs> onClick)
        {
            return Render<ElButton>(parameters => parameters
                .Add(x => x.Debounce, debounce)
                .Add(x => x.DebounceLeading, leading)
                .Add(x => x.OnClick, onClick)
                .AddChildContent("Submit"));
        }

        /// <summary>
        /// 等待条件成立。防抖依赖真实计时器，固定 sleep 在并行跑测试时会偶发失败，这里改成轮询。
        /// </summary>
        private static async Task WaitForAsync(Func<bool> condition, int timeoutMilliseconds = 5000)
        {
            var start = Environment.TickCount;
            while (!condition() && Environment.TickCount - start < timeoutMilliseconds)
            {
                await Task.Delay(20);
            }
        }

        [Fact]
        public void WithoutDebounce_ClickFiresImmediately()
        {
            var count = 0;
            var cut = RenderButton(0, false, EventCallback.Factory.Create<MouseEventArgs>(this, _ => count++));

            cut.Find("button").Click();
            cut.Find("button").Click();

            Assert.Equal(2, count);
        }

        [Fact]
        public async Task TrailingDebounce_CoalescesRapidClicksIntoOne()
        {
            var count = 0;
            var cut = RenderButton(120, false, EventCallback.Factory.Create<MouseEventArgs>(this, _ => count++));
            var button = cut.Find("button");

            button.Click();
            button.Click();
            button.Click();

            Assert.Equal(0, count);

            await WaitForAsync(() => count == 1);

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task LeadingDebounce_FiresFirstClickOnly()
        {
            var count = 0;
            var cut = RenderButton(120, true, EventCallback.Factory.Create<MouseEventArgs>(this, _ => count++));
            var button = cut.Find("button");

            button.Click();

            Assert.Equal(1, count);

            button.Click();
            button.Click();

            await WaitForAsync(() => count == 1);
            await Task.Delay(200);

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task TrailingDebounce_NextWindowFiresAgain()
        {
            var count = 0;
            var cut = RenderButton(120, false, EventCallback.Factory.Create<MouseEventArgs>(this, _ => count++));
            var button = cut.Find("button");

            button.Click();
            await WaitForAsync(() => count == 1);

            Assert.Equal(1, count);

            button.Click();
            await WaitForAsync(() => count == 2);

            Assert.Equal(2, count);
        }

        [Fact]
        public void DisabledButton_NeverFiresOnClick()
        {
            var count = 0;
            var cut = Render<ElButton>(parameters => parameters
                .Add(x => x.Disabled, true)
                .Add(x => x.Debounce, 120)
                .Add(x => x.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, _ => count++))
                .AddChildContent("Submit"));

            cut.Find("button").Click();

            Assert.Equal(0, count);
        }

        [Fact]
        public void DebounceParameters_DoNotAffectRendering()
        {
            var cut = RenderButton(200, false, EventCallback.Factory.Create<MouseEventArgs>(this, _ => { }));

            var button = cut.Find("button");
            Assert.Contains("el-button", button.ClassList);
            Assert.False(button.HasAttribute("disabled"));
        }
    }
}
