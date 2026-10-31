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
    public class ElCollapseTests : BunitContext
    {
        public ElCollapseTests()
        {
            Services.AddElementServices();
        }

        private static RenderFragment TwoItems => builder =>
        {
            builder.OpenComponent<ElCollapseItem>(0);
            builder.AddAttribute(1, "Name", "1");
            builder.AddAttribute(2, "Title", "One");
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(4, "Body1")));
            builder.CloseComponent();
            builder.OpenComponent<ElCollapseItem>(5);
            builder.AddAttribute(6, "Name", "2");
            builder.AddAttribute(7, "Title", "Two");
            builder.CloseComponent();
        };

        [Fact]
        public void BeforeCollapseCancelsToggle()
        {
            var calls = new List<IList<string>>();
            var cut = Render<ElCollapse>(parameters => parameters
                .Add(x => x.ChildContent, TwoItems)
                .Add(x => x.BeforeCollapse, names => { calls.Add(names); return Task.FromResult(false); }));

            cut.FindAll(".el-collapse-item")[0].QuerySelector(".el-collapse-item__header").Click();

            cut.WaitForAssertion(() => Assert.DoesNotContain("is-active", cut.FindAll(".el-collapse-item")[0].ClassName), timeout: TestWait.Timeout);
            Assert.Single(calls);
        }

        [Fact]
        public void BeforeCollapseAllowsToggle()
        {
            var cut = Render<ElCollapse>(parameters => parameters
                .Add(x => x.ChildContent, TwoItems)
                .Add(x => x.BeforeCollapse, _ => Task.FromResult(true)));

            cut.FindAll(".el-collapse-item")[0].QuerySelector(".el-collapse-item__header").Click();

            cut.WaitForAssertion(() => Assert.Contains("is-active", cut.FindAll(".el-collapse-item")[0].ClassName), timeout: TestWait.Timeout);
        }

        [Fact]
        public void ToggleWorksWithoutBeforeCollapse()
        {
            var cut = Render<ElCollapse>(parameters => parameters
                .Add(x => x.ChildContent, TwoItems));

            cut.FindAll(".el-collapse-item")[1].QuerySelector(".el-collapse-item__header").Click();

            cut.WaitForAssertion(() => Assert.Contains("is-active", cut.FindAll(".el-collapse-item")[1].ClassName), timeout: TestWait.Timeout);
        }
    }
}
