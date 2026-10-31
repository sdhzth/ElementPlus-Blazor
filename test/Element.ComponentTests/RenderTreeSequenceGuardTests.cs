using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 全库「手写渲染树序列号」守卫。规则与背景见 <see cref="RenderTreeSequenceScanner"/>。
    ///
    /// 它要防的是 2026-10-07 那类事故复发：ElButton 用 <c>seq++</c> 连续编号 + 条件帧，
    /// 只要运行期把 Loading 从 false 改成 true，捕获帧的序列号就漂走，
    /// Blazor diff 抛 <c>NotImplementedException: Unexpected frame type during RemoveOldFrame</c>，
    /// 整页渲染失败；而单测之外几乎没人会去点一个进入 loading 的按钮，所以它潜伏了很久。
    ///
    /// 每个规则都配了「必须抓到」与「不许误报」两侧夹具（本仓已经因为「守卫拿原文断言」
    /// 误报过三条兼容性守卫，也差点因为剥离器过剥而假绿 —— 这里两侧都锁住）。
    /// </summary>
    public class RenderTreeSequenceGuardTests
    {
        private static readonly string[] BuilderFiles = DiscoverBuilderFiles().ToArray();

        private static IEnumerable<string> DiscoverBuilderFiles()
        {
            var src = Path.Combine(RepoRoot(), "src");
            return Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains(@"\bin\") && !p.Contains(@"\obj\") && !p.Contains("/bin/") && !p.Contains("/obj/"));
        }

        [Fact]
        public void Guard_RepositoryHasNoUnstableRenderTreeSequences()
        {
            var report = new StringBuilder();
            int hits = 0;

            foreach (var file in BuilderFiles)
            {
                var violations = RenderTreeSequenceScanner.Scan(ReadSource(file));
                foreach (var violation in violations)
                {
                    hits++;
                    report.AppendLine(string.Format("{0} :: {1}", Path.GetRelativePath(RepoRoot(), file), violation));
                }
            }

            Assert.True(hits == 0, "手写渲染树序列号守卫发现 " + hits + " 处：\n" + report);
        }

        /// <summary>
        /// 防「假绿」：扫描器若因正则/词法退化而什么都识别不到，上面的断言会毫无意义地通过。
        /// 这里要求它在全仓真的识别出足够多的 builder 发射点（含已知的正常 seq++ 用法）。
        /// </summary>
        [Fact]
        public void Guard_ScannerActuallySeesBuilderCode()
        {
            var scanned = 0;
            var withFrames = 0;

            foreach (var file in BuilderFiles)
            {
                scanned++;
                var text = RenderTreeSequenceScanner.StripCommentsAndLiterals(ReadSource(file));
                if (text.Any(p => p.Item2.IndexOf("builder.", StringComparison.Ordinal) >= 0))
                {
                    withFrames++;
                }
            }

            Assert.True(scanned > 400, "扫描到的源码文件过少，可能路径解析失效：" + scanned);
            Assert.True(withFrames >= 10, "全仓竟没找到含 builder. 发射的源文件，扫描器已失效：" + withFrames);
        }

        // ---------- R1：序列漂移 ----------

        [Fact]
        public void Guard_R1_CatchesConditionalFramesBeforeReferenceCapture()
        {
            var source = R1_Bad;

            var violations = RenderTreeSequenceScanner.Scan(source);

            Assert.Contains(violations, v => v.Rule == "R1");
        }

        [Fact]
        public void Guard_R1_AllowsFixedSlotNumbering()
        {
            // 与 ElButton 修复后同构：每个帧位是固定常量，条件帧不占后续编号
            Assert.Empty(RenderTreeSequenceScanner.Scan(R1_Good_FixedSlots));
        }

        [Fact]
        public void Guard_R1_AllowsCounterWhenConditionalFramesComeAfterCapture()
        {
            // 捕获帧先出现、条件帧在它之后（且都是新元素的合法结构）：序列漂移不影响该捕获帧
            Assert.Empty(RenderTreeSequenceScanner.Scan(R1_Good_ConditionalAfterCapture));
        }

        [Fact]
        public void Guard_IgnoresCodeThatOnlyAppearsInCommentsOrStrings()
        {
            // 整段坏代码被注释掉存档（文档里常见）时不许触发守卫 —— 本仓已经因为
            // 「守卫拿原文断言」误报过三条兼容性守卫，见 TestJsScanner。
            var commented = string.Join("\n", R1_Bad.Split('\n')
                .Select(l => string.IsNullOrWhiteSpace(l) ? l : "// " + l));

            Assert.DoesNotContain(RenderTreeSequenceScanner.Scan(commented), v => v.Rule == "R1");

            // 块注释同样不算代码
            var blocked = "/*\n" + R1_Bad + "\n*/\n" + R1_Good_FixedSlots;
            Assert.DoesNotContain(RenderTreeSequenceScanner.Scan(blocked), v => v.Rule == "R1");

            // 字符串里的代码不算代码
            var inString = "class X { void M() { var s = \"builder.AddAttribute(seq++, \\\"a\\\", b); builder.AddElementReferenceCapture(seq++, r => {});\"; } }";
            Assert.DoesNotContain(RenderTreeSequenceScanner.Scan(inString), v => v.Rule == "R1");
        }

        // ---------- R2：捕获之后加属性 ----------

        [Fact]
        public void Guard_R2_CatchesAttributeAfterReferenceCapture()
        {
            var violations = RenderTreeSequenceScanner.Scan(R2_Bad);

            Assert.Contains(violations, v => v.Rule == "R2");
        }

        [Fact]
        public void Guard_R2_AllowsAttributesOnChildElementAfterCapture()
        {
            // 与 ElDateTimePicker 同构：捕获之后开的是<b>子元素</b>，其属性合法
            Assert.Empty(RenderTreeSequenceScanner.Scan(R2_Good_ChildElement));
        }

        // ---------- 夹具（真实源码的抽象化） ----------

        /// <summary>修复前的 ElButton：条件帧在捕获之前 + seq++。</summary>
        private const string R1_Bad = @"
namespace ElementPlus
{
    public class Foo
    {
        protected RenderFragment ButtonElement => builder =>
        {
            var seq = 0;
            builder.OpenElement(seq++, ""button"");
            builder.AddAttribute(seq++, ""class"", css);
            if (Loading) { builder.AddAttribute(seq++, ""aria-busy"", ""true""); }
            BuildContent(builder, ref seq);
            builder.AddElementReferenceCapture(seq++, r => Reference = r);
            builder.CloseElement();
        };
    }
}
";

        /// <summary>修复后的 ElButton：固定槽位，条件帧不占后续编号。</summary>
        private const string R1_Good_FixedSlots = @"
namespace ElementPlus
{
    public class Foo
    {
        private const int SeqClass = 3;
        private const int SeqBusy = 5;
        private const int SeqCapture = 90;

        protected RenderFragment ButtonElement => builder =>
        {
            builder.OpenElement(0, ""button"");
            builder.AddAttribute(SeqClass, ""class"", css);
            if (Loading) { builder.AddAttribute(SeqBusy, ""aria-busy"", ""true""); }
            BuildContent(builder);
            builder.AddElementReferenceCapture(SeqCapture, r => Reference = r);
            builder.CloseElement();
        };
    }
}
";

        /// <summary>有 seq++ 也有条件帧，但捕获帧在所有条件帧<b>之前</b>，条件帧挂在新元素上。</summary>
        private const string R1_Good_ConditionalAfterCapture = @"
namespace ElementPlus
{
    public class Foo
    {
        protected void Build(RenderTreeBuilder builder)
        {
            var seq = 0;
            builder.OpenComponent<Foo>(seq++);
            builder.AddAttribute(seq++, ""Data"", data);
            builder.AddComponentReferenceCapture(seq++, captured => Tree = captured);
            builder.CloseComponent();
            if (Visible)
            {
                builder.OpenElement(seq++, ""div"");
                builder.AddAttribute(seq++, ""class"", ""tip"");
                builder.CloseElement();
            }
        }
    }
}
";

        /// <summary>修复前的 ElTreeSelect：捕获帧之后再补属性帧（泛型 OpenComponent 也必须识别）。</summary>
        private const string R2_Bad = @"
namespace ElementPlus
{
    public class Foo
    {
        private static RenderFragment BuildTreeContent(RenderFragment childContent)
        {
            return builder =>
            {
                var seq = 0;
                builder.OpenComponent<Bar>(seq++);
                builder.AddAttribute(seq++, ""Data"", data);
                builder.AddComponentReferenceCapture(seq++, captured => Tree = captured);
                if (childContent != null)
                {
                    builder.AddAttribute(seq++, ""ChildContent"", childContent);
                }
                builder.CloseComponent();
            };
        }
    }
}
";

        /// <summary>修复前的 ElDateTimePicker 形状：捕获之后开子元素再写属性，合法，不许误报。</summary>
        private const string R2_Good_ChildElement = @"
namespace ElementPlus
{
    public class Foo
    {
        private void BuildTimePopover(RenderTreeBuilder builder)
        {
            var seq = 0;
            builder.OpenElement(seq++, ""div"");
            builder.AddAttribute(seq++, ""class"", ""el-time-panel"");
            builder.AddElementReferenceCapture(seq++, el => Panel = el);
            builder.OpenElement(seq++, ""div"");
            builder.AddAttribute(seq++, ""class"", ""el-time-panel__content"");
            builder.CloseElement();
            builder.CloseElement();
        }
    }
}
";

        /// <summary>本仓库源码编码混用（GBK 无 BOM 与 UTF-8 并存），读文件必须先做编码回退。</summary>
        private static string ReadSource(string path)
        {
            var bytes = File.ReadAllBytes(path);
            try
            {
                // 严格 UTF-8（遇到非法字节抛异常），避免把 GBK 文件当成 UTF-8 而静默乱码
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                try
                {
                    return Encoding.GetEncoding("GBK").GetString(bytes);
                }
                catch (Exception)
                {
                    return Encoding.UTF8.GetString(bytes);
                }
            }
        }

        private static string RepoRoot([CallerFilePath] string callerFilePath = null)
        {
            var dir = new FileInfo(callerFilePath ?? string.Empty).Directory;
            return dir.Parent.Parent.FullName;
        }
    }
}
