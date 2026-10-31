using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// <b>编码契约</b>：仓库里所有文本文件必须以 UTF-8 存盘，不允许 GBK。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 为什么这条以前从来没被任何检查发现：<b>C# 编译器不会报错</b>。Roslyn 读没有 BOM 的源文件时
    /// 先按 UTF-8 解，解不动就退回系统 ANSI 代码页（本机 ACP=936/GBK）再解一遍。所以 GBK 存盘的
    /// 中文注释在中文 Windows 上<b>编译通过、运行时字符串也正确</b>（实测 <c>Element.dll</c> 里是
    /// UTF-16 的「确认删除吗？」），只在换了代码页的机器、Git 托管站网页、以及任何按 UTF-8 读文件的
    /// 工具（grep/编辑器/本仓库的文档注释 XML）上显示成乱码。也就是说这个缺陷<b>完全静默</b>，
    /// 只能靠机器判据钉住，不能靠"编译过了"。
    /// </para>
    /// <para>
    /// 2026-10 那次清理把 30 个纯 GBK 文件无损转成 UTF-8（无 BOM），另有 9 行注释是
    /// "UTF-8 里嵌 U+FFFD"——那种损坏不可逆，转换救不回来，只能照上下文重写。
    /// 所以这里分两条判据：<see cref="EveryTextFileDecodesAsStrictUtf8"/> 挡住 GBK 存盘，
    /// <see cref="CodeFilesContainNoReplacementCharacter"/> 挡住已经写坏的 U+FFFD 再次进来。
    /// </para>
    /// </remarks>
    public class SourceEncodingGuardTests
    {
        /// <summary>扫描范围：四个源码目录（递归）＋ 仓库根目录本身（只取顶层文件，如 CHANGELOG.md / README*.md）。</summary>
        private static readonly string[] ScannedRoots = { "src", "demo", "test", "docs" };

        private static readonly HashSet<string> TextExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".razor", ".csproj", ".props", ".targets", ".json", ".md", ".html", ".htm",
            ".css", ".js", ".ts", ".xml", ".yml", ".yaml", ".txt", ".sln", ".editorconfig",
            ".config", ".ps1", ".cjs", ".mjs", ".cmd", ".bat",
        };

        private static readonly HashSet<string> SkippedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", "publish", "node_modules", ".git",
        };

        /// <summary>严格解码器：遇到非法 UTF-8 序列就抛，不做替换式兜底。</summary>
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        private static string RepoRoot()
        {
            var dir = new Uri(typeof(SourceEncodingGuardTests).Assembly.Location).LocalPath;
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

        /// <summary>
        /// 只读地把文件打开：<b>必须用 <see cref="FileShare.ReadWrite"/></b>。
        /// <see cref="File.OpenRead(string)"/> 的默认共享是 <c>FileShare.Read</c>，意思是"别人也不许写"，
        /// 于是任何一份正被别的进程写入的文件都会让它抛 <c>IOException: being used by another process</c>。
        /// 本守卫只是校验字节，不该对别人的写入权限提要求（实测：另一个进程正把一份根目录 .txt 开着写，
        /// 用 OpenRead 时两条用例各红一次，换成 ReadWrite 共享后同一场景稳定通过）。
        /// </summary>
        private static FileStream OpenShared(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        /// <summary>按 <see cref="OpenShared"/> 的口径读全部字节。</summary>
        private static byte[] ReadAllBytesShared(string path)
        {
            using (var stream = OpenShared(path))
            {
                var bytes = new byte[stream.Length];
                var read = 0;
                while (read < bytes.Length)
                {
                    var n = stream.Read(bytes, read, bytes.Length - read);
                    if (n <= 0)
                    {
                        break;
                    }

                    read += n;
                }

                return read == bytes.Length ? bytes : bytes.Take(read).ToArray();
            }
        }

        /// <summary>
        /// 收集扫描范围内的文本文件：先按扩展名筛，再跳过含 NUL 的字节的文件（当作二进制）。
        /// </summary>
        private static List<string> TextFiles()
        {
            var root = RepoRoot();
            var files = new List<string>();
            foreach (var start in ScannedRoots)
            {
                var dir = Path.Combine(root, start);
                if (!Directory.Exists(dir))
                {
                    continue;
                }

                foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                {
                    var relative = file.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar);
                    if (relative.Split(Path.DirectorySeparatorChar).Any(SkippedDirectories.Contains))
                    {
                        continue;
                    }

                    if (!TextExtensions.Contains(Path.GetExtension(file)))
                    {
                        continue;
                    }

                    var head = new byte[8192];
                    using (var stream = OpenShared(file))
                    {
                        var read = stream.Read(head, 0, head.Length);
                        if (head.Take(read).Contains((byte)0))
                        {
                            continue;
                        }
                    }

                    files.Add(relative.Replace(Path.DirectorySeparatorChar, '/'));
                }
            }

            // 仓库根的顶层文件（CHANGELOG.md / README*.md 这些每批都要写中文的地方）
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly))
            {
                if (!TextExtensions.Contains(Path.GetExtension(file)))
                {
                    continue;
                }

                var head = new byte[8192];
                using (var stream = OpenShared(file))
                {
                    var read = stream.Read(head, 0, head.Length);
                    if (head.Take(read).Contains((byte)0))
                    {
                        continue;
                    }
                }

                files.Add(Path.GetFileName(file));
            }

            return files;
        }

        /// <summary>
        /// 手工扫一遍字节，返回第一个不合法 UTF-8 序列的偏移，只用于把失败信息说清楚
        /// （DecoderFallbackException 本身不暴露偏移；替换符在消息里也看不出是哪个字节坏了）。
        /// </summary>
        private static int FirstInvalidUtf8Byte(byte[] bytes)
        {
            var i = 0;
            while (i < bytes.Length)
            {
                var b = bytes[i];
                var length = b < 0x80 ? 1 : b >= 0xF0 ? 4 : b >= 0xE0 ? 3 : b >= 0xC0 ? 2 : 0;
                if (length == 0 || i + length > bytes.Length)
                {
                    return i;
                }

                for (var k = 1; k < length; k++)
                {
                    if (bytes[i + k] < 0x80 || bytes[i + k] >= 0xC0)
                    {
                        return i;
                    }
                }

                i += length;
            }

            return -1;
        }

        /// <summary>
        /// 主判据：任何一个文本文件都必须能用严格 UTF-8 解出来。GBK 存盘的中文必然在这里红。
        /// </summary>
        [Fact]
        public void EveryTextFileDecodesAsStrictUtf8()
        {
            var offenders = new List<string>();
            foreach (var relative in TextFiles())
            {
                var bytes = ReadAllBytesShared(Path.Combine(RepoRoot(), relative));
                try
                {
                    StrictUtf8.GetString(bytes);
                }
                catch (DecoderFallbackException e)
                {
                    offenders.Add(relative + " @byte " + FirstInvalidUtf8Byte(bytes) + "：" + e.Message.Split('\n')[0]);
                }
            }

            Assert.True(offenders.Count == 0,
                "以下文件不是合法 UTF-8（多半是 GBK 存盘）。请用 UTF-8（无 BOM）重新存盘：\n"
                + string.Join("\n", offenders.Take(50))
                + (offenders.Count > 50 ? "\n… 共 " + offenders.Count + " 个" : ""));
        }

        /// <summary>
        /// src 下的代码文件不许带 U+FFFD（<c>EF BF BD</c>）。它本身是合法 UTF-8，所以上一条查不出来，
        /// 但它是"中文已经被丢过一次"的痕迹——丢掉的字符转换救不回来，只能重写。
        /// </summary>
        [Fact]
        public void CodeFilesContainNoReplacementCharacter()
        {
            var root = RepoRoot();
            var offenders = new List<string>();
            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                         .Concat(Directory.EnumerateFiles(root, "*.razor", SearchOption.AllDirectories)))
            {
                var relative = file.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar);
                if (!relative.StartsWith("src" + Path.DirectorySeparatorChar))
                {
                    continue;
                }

                if (relative.Split(Path.DirectorySeparatorChar).Any(SkippedDirectories.Contains))
                {
                    continue;
                }

                var bytes = ReadAllBytesShared(file);
                for (var i = 0; i + 2 < bytes.Length; i++)
                {
                    if (bytes[i] == 0xEF && bytes[i + 1] == 0xBF && bytes[i + 2] == 0xBD)
                    {
                        offenders.Add(relative + " @byte " + i);
                        break;
                    }
                }
            }

            Assert.True(offenders.Count == 0, "src 下的代码里有 U+FFFD（中文已损坏，需按上下文重写）：\n" + string.Join("\n", offenders));
        }

        /// <summary>
        /// 判据夹具（双侧）：同一段中文的 GBK 字节必须被拒、UTF-8 字节必须被收；
        /// 同时验证"宽松解码会把 GBK 悄悄放过去"，这正是编译器 ANSI 兜底让缺陷静默的原因。
        /// 没有这条，上面的扫描器一旦写坏（比如筛选条件变了）就会假绿。
        /// </summary>
        [Fact]
        public void StrictDecoderRejectsGbkButAcceptsUtf8()
        {
            // 「确认」的 GBK 编码（GBK 936 代码页下的 4 个字节）
            var gbk = new byte[] { 0xBF, 0xC6, 0xC8, 0xCF };
            // 同一词的 UTF-8 编码
            var utf8 = Encoding.UTF8.GetBytes("确认");

            Assert.Throws<DecoderFallbackException>(() => StrictUtf8.GetString(gbk));
            Assert.Equal("确认", StrictUtf8.GetString(utf8));

            // 反向证据：宽松 UTF-8 不抛异常，只是把非法字节替换成 U+FFFD —— 所以"能解码"不等于"编码正确"。
            var lenient = new UTF8Encoding(false, false).GetString(gbk);
            Assert.Contains('\uFFFD'.ToString(), lenient);
        }

        /// <summary>
        /// 覆盖率哨兵：扫描必须真的扫到足够多的文件，并且必须包含当初出问题的那类文件，
        /// 否则上面两条"零违规"的断言可能只是因为什么都没扫。
        /// </summary>
        [Fact]
        public void ScanCoversTheWholeSourceTree()
        {
            var files = TextFiles();

            // 2026-10-09 实测 1595 个（src 762 / demo 574 / test 152 / docs 94 / 仓库根顶层 13），留一半余量作下限。
            // 读数口径：把下面的下限临时抬到 999999 跑一次，失败消息里的数字才是 files.Count 的真身
            // （`Temp/scope_probe2.log:14`）—— 分桶数字同口径重数，别照上一批的记录抄。
            Assert.True(files.Count >= 700, "扫描到的文本文件太少（" + files.Count + "），判据可能已经失效");

            foreach (var required in new[]
                     {
                         "src/Components/ControlRenders/TableRender.cs",   // 当初被指出的 GBK 文件
                         "src/Components/Extension.cs",                    // 曾有替换符（U+FFFD）的三个文件之一
                         "src/Components/Element.csproj",
                         "demo/Element.Blazor.Demo/App.razor",
                         "test/Element.ComponentTests/SourceEncodingGuardTests.cs",  // 本守卫自己也要被扫到
                         "CHANGELOG.md",                                   // 仓库根顶层那条通道
                     })
            {
                Assert.Contains(required, files);
            }
        }
    }
}
