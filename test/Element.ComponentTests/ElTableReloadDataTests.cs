using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// <see cref="ElTable.ReloadData"/> 的契约：DataSource 的引用没变、只是集合被原地增删行时，
    /// 表格要能重新取数。
    ///
    /// 背景：<c>Refresh()</c>（ElementComponentBase）只是 MarkAsRequireRender + StateHasChanged，
    /// 而行是在 OnAfterRenderAsync 的两段式渲染里、只有 dataSourceUpdated 为真时才重算的，
    /// 所以「list.Add(row) + Refresh()」看不到新行。Radzen 的 Reload()/Reset() 是会重新查询的，
    /// 迁移过来的页面需要这个对等方法。
    ///
    /// 顺序注意：ReloadData 要在改完集合之后调用。先调用再加行，那次渲染当场就把旧行数画上去了，
    /// 新行要等下一次 ReloadData 才出现（页面里迁移前"未 await 的 Radzen Reset() 写在方法开头"的
    /// 位置不能照搬）。这条结论原本有测试钉着，但"不该出现新行"的断言在套件并发下和渲染批次赛跑、
    /// 偶发红，且没法不靠超时就写稳，所以改成记录在这里、由先改后调的用例正面约束。
    /// </summary>
    public class ElTableReloadDataTests : BunitContext
    {
        public ElTableReloadDataTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        public class Person
        {
            public string Name { get; set; }
        }

        /// <summary>名称列的单元格文本，按行序读，用来观察表体到底重算没有。</summary>
        private static string[] NameCells(IRenderedComponent<ElTable> cut)
            => cut.FindAll(".el-table__body tbody tr td:first-child")
                .Select(td => td.TextContent.Trim()).ToArray();

        private IRenderedComponent<ElTable> RenderTable(List<Person> people)
        {
            return Render<ElTable>(p => p
                .Add(x => x.DataSource, people)
                .Add(x => x.AutoGenerateColumns, false)
                .Add(x => x.HasSelectionColumn, false)
                .Add(x => x.EnablePagination, false)
                .AddChildContent(b =>
                {
                    b.OpenComponent<ElTableColumn>(0);
                    b.AddAttribute(1, "Property", "Name");
                    b.AddAttribute(2, "Text", "名称");
                    b.CloseComponent();
                }));
        }

        /// <summary>原地加一行：Refresh() 不会让新行出现，ReloadData() 才会。</summary>
        [Fact]
        public void InPlaceRowAddNeedsReloadData()
        {
            var people = new List<Person>
            {
                new Person { Name = "张三" },
                new Person { Name = "李四" },
            };
            var cut = RenderTable(people);

            Assert.Equal(new[] { "张三", "李四" }, NameCells(cut));

            people.Add(new Person { Name = "王五" });

            cut.Instance.Refresh();
            // 正向对照：这一句证明"新行没出来"确实是 Refresh() 的性质，而不是选择器数错了行。
            // Refresh 不置 dataSourceUpdated，行不会重算，与渲染时机无关。
            Assert.Equal(2, NameCells(cut).Length);

            cut.Instance.ReloadData();
            // 行的重算发生在异步的 OnAfterRenderAsync 里，断言要等它落地，不能抢跑。
            cut.WaitForAssertion(() => Assert.Equal(new[] { "张三", "李四", "王五" }, NameCells(cut)), timeout: TimeSpan.FromSeconds(2));
        }

        /// <summary>ReloadData 只重算行：列定义（Headers 与表头格数）不能被动过或重复注册。</summary>
        [Fact]
        public void ReloadDataKeepsColumnDefinitions()
        {
            var people = new List<Person> { new Person { Name = "张三" } };
            var cut = RenderTable(people);

            var headersBefore = cut.Instance.Headers.Count;
            var headerCellsBefore = cut.FindAll(".el-table__header th.el-table__cell").Count;

            people.Add(new Person { Name = "李四" });
            people.Add(new Person { Name = "王五" });
            cut.Instance.ReloadData();

            cut.WaitForAssertion(() => Assert.Equal(3, NameCells(cut).Length), timeout: TimeSpan.FromSeconds(2));
            Assert.Equal(headersBefore, cut.Instance.Headers.Count);
            Assert.Equal(headerCellsBefore, cut.FindAll(".el-table__header th.el-table__cell").Count);
        }

        /// <summary>原地删一行同样要能被 ReloadData 反映出来。</summary>
        [Fact]
        public void InPlaceRowRemoveNeedsReloadData()
        {
            var people = new List<Person>
            {
                new Person { Name = "张三" },
                new Person { Name = "李四" },
            };
            var cut = RenderTable(people);

            people.RemoveAt(1);
            cut.Instance.ReloadData();

            cut.WaitForAssertion(() => Assert.Equal(new[] { "张三" }, NameCells(cut)), timeout: TimeSpan.FromSeconds(2));
        }
    }
}
