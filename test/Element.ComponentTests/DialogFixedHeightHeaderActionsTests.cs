using System;
using System.IO;
using System.Text.RegularExpressions;
using Bunit;
using ElementPlus;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 批次㊲：定高对话框的正文必须真的能滚，标题栏必须钉住；宿主把「放弃 / 确定」挂到标题栏右侧时不能压到关闭图标。
    ///
    /// 触发点是宿主 <c>zyweb</c> 的反馈「这个排版怎么全乱了，新增的时候，没显示全」+「能不能把放弃和确定的按钮，
    /// 和标题栏放在一起，放到右边不影响关闭图标」。浏览器实测（1280×720，部门编辑 650×500）给了两条真身：
    /// ① 旧规则 <c>.el-dialog-grow[style*="height"] &gt; .el-dialog__body{overflow:auto}</c> 只声明了 overflow，
    ///   块级布局下正文高度仍由内容决定（实测 body 盒 162..678，对话框盒底 608）——溢出 70px 画到卡片外面，
    ///   scrollHeight==clientHeight==516 说明滚动条从来不出现，「没显示全」就是这么来的；
    /// ② 宿主用 <c>margin-top:-15px</c> 把按钮条上提进标题栏，实测其右边缘 945 正好压在关闭图标 921..945 上。
    /// 所以这里钉两件事：定高时把对话框改成 flex 列（正文 min-height:0 + overflow:auto 才有界可滚），
    /// 以及标题栏动作条的定位口径（top:16px 与标题/关闭图标同中线 140，右移量按有没有关闭图标给）。
    /// </summary>
    public class DialogFixedHeightHeaderActionsTests : BunitContext
    {
        public DialogFixedHeightHeaderActionsTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }
        private static string RepoRoot()
        {
            // 编译目录里往上找仓库根（bin/Debug/net10.0 → 测试工程 → test → 仓库根）
            var directory = new FileInfo(typeof(DialogFixedHeightHeaderActionsTests).Assembly.Location).Directory;
            while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src", "Components")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("未找到仓库根。");
        }

        private static string Css() =>
            File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "css", "index.css"));

        /// <summary>
        /// 取本仓手写规则的声明体。选择器必须在行首：index.css 第 1 行是整份 EP 压缩产物，
        /// 任何「子串命中」都会先撞上那份压缩规则，行首判定把这类假命中筛掉。
        /// </summary>
        private static string CssBlock(string selector)
        {
            var css = Css();
            var from = 0;
            while (true)
            {
                var index = css.IndexOf(selector, from, StringComparison.Ordinal);
                Assert.True(index >= 0, $"index.css 中未找到行首规则：{selector}");

                var lineStart = css.LastIndexOf('\n', index) + 1;
                if (css.Substring(lineStart, index - lineStart).Trim().Length == 0)
                {
                    var open = css.IndexOf('{', index);
                    var close = css.IndexOf('}', open);
                    return Regex.Replace(css.Substring(open + 1, close - open - 1), @"/\*.*?\*/", string.Empty, RegexOptions.Singleline)
                        .Replace("\n", " ").Replace("\r", " ");
                }

                from = index + 1;
            }
        }

        private static void AssertDecl(string selector, string property, string value)
        {
            var block = CssBlock(selector);
            var match = Regex.Match(block, Regex.Escape(property) + @"\s*:\s*([^;]+)");
            Assert.True(match.Success, $"{selector} 里没有 {property} 声明（实测块体：{block.Trim()}）。");
            Assert.Equal(value, match.Groups[1].Value.Trim());
        }

        // —— ① 类名口径：只有「定了高度且非全屏」才挂 flex 链 ——
        [Fact]
        public void HeightSet_DialogGetsFixedHeightChain()
        {
            var cut = Render<ElDialogGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.Height, "500px"));

            Assert.Contains("el-dialog-grow--fixed-height", cut.Find(".el-dialog-grow").ClassName);
        }

        /// <summary>正向对照：没给 Height 时不挂链——服务化 Confirm/Alert 的 Height 是 null，批次㉞ 的正文内边距口径不能被 flex 改掉。</summary>
        [Fact]
        public void NoHeight_DialogKeepsPlainBlockChain()
        {
            var cut = Render<ElDialogGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.Title, "标题"));

            Assert.DoesNotContain("el-dialog-grow--fixed-height", cut.Find(".el-dialog-grow").ClassName);
        }

        [Fact]
        public void FullscreenWithHeight_DoesNotGetFixedHeightChain()
        {
            var cut = Render<ElDialogGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.Fullscreen, true)
                .Add(x => x.Height, "500px"));

            Assert.DoesNotContain("el-dialog-grow--fixed-height", cut.Find(".el-dialog-grow").ClassName);
        }

        // —— ② CSS 口径：正文要有界可滚，标题/页脚不许被压缩 ——
        [Fact]
        public void FixedHeightChain_MakesTheDialogAColumnFlex()
        {
            AssertDecl(".el-dialog-grow--fixed-height", "display", "flex");
            AssertDecl(".el-dialog-grow--fixed-height", "flex-direction", "column");
        }

        [Fact]
        public void FixedHeightChain_BodyIsTheOnlyThingThatScrolls()
        {
            // min-height:0 是这条链的要点：flex 子项默认 min-height:auto，不给 0 就撑回内容高度，滚动条照样不出现。
            AssertDecl(".el-dialog-grow--fixed-height>.el-dialog__body", "min-height", "0");
            AssertDecl(".el-dialog-grow--fixed-height>.el-dialog__body", "overflow", "auto");
            AssertDecl(".el-dialog-grow--fixed-height>.el-dialog__header", "flex", "0 0 auto");
            AssertDecl(".el-dialog-grow--fixed-height>.el-dialog__footer", "flex", "0 0 auto");
        }

        // —— ③ 标题栏动作条：与标题/关闭图标同一条中线，右移量按有没有关闭图标给 ——
        [Fact]
        public void HeaderActionsSitOnTheTitleBandCenterLine()
        {
            // 实测：标题行盒 128..152（对话框内 20..44），按钮高 32 ⇒ top:20+(24-32)/2=16，中线 140 与关闭图标一致。
            AssertDecl(".el-dialog-grow__header-actions", "position", "absolute");
            AssertDecl(".el-dialog-grow__header-actions", "top", "16px");
            AssertDecl(".el-dialog-grow__header-actions", "display", "flex");
        }

        [Fact]
        public void HeaderActionsClearTheCloseIconOnlyWhenItExists()
        {
            // 有关闭图标（默认）时让到它左边：图标占右侧 inward 20..44px，再留 8px 间距 ⇒ 52px。
            AssertDecl(".el-dialog-grow__header-actions", "right", "52px");
            // 无关闭图标（ShowClose=false，如「锁定屏幕」「密码修改」）时贴回 20px 内缩，别留一段无主空隙。
            AssertDecl(".el-dialog-grow--no-close .el-dialog-grow__header-actions", "right", "20px");
        }

        // —— ④ --no-close 修饰类：让位的唯一依据，组件里 ShowClose 才说了算 ——
        [Fact]
        public void ShowCloseFalse_MarksDialogNoClose()
        {
            var cut = Render<ElDialogGrow>(p => p
                .Add(x => x.Visible, true)
                .Add(x => x.ShowClose, false));

            Assert.Contains("el-dialog-grow--no-close", cut.Find(".el-dialog-grow").ClassName);
            Assert.Empty(cut.FindAll(".el-dialog__headerbtn"));
        }

        /// <summary>正向对照：默认 ShowClose=true 时既不挂 --no-close，也真的渲染出关闭图标。</summary>
        [Fact]
        public void ShowCloseDefault_MarksNothingAndRendersCloseIcon()
        {
            var cut = Render<ElDialogGrow>(p => p.Add(x => x.Visible, true));

            Assert.DoesNotContain("el-dialog-grow--no-close", cut.Find(".el-dialog-grow").ClassName);
            Assert.Single(cut.FindAll(".el-dialog__headerbtn"));
        }
    }
}
