using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// <b>可部署契约</b>：演示站与组件库里「由组件动态加载的第三方库地址」必须指向站内文件，不许指向公网 CDN。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 触发这条契约的真实事故（2026-10-12）：用户把演示站发到内网 <c>http://175.178.5.253:8049</c> 后，
    /// <c>/video-player-grow</c> 第 7 节在控制台报
    /// <c>Access to script at 'https://cdn.jsdelivr.net/npm/hls.js@1/dist/hls.min.js' from origin 'http://...'
    /// has been blocked by CORS policy: The request client is not a secure context and the resource is in
    /// more-private address space `loopback`.</c>
    /// 真身不是组件库（<c>ElVideoPlayerGrow</c> 零第三方播放器依赖，只有显式给出 <c>HlsLibUrl</c> 才加载脚本），
    /// 而是<b>演示页示例</b>把 CDN 地址当参数传了进去。内网里 <c>cdn.jsdelivr.net</c> 常被解析到 <c>127.0.0.1</c>
    /// （本机 <c>Temp/b27-cdn-dns.log</c> 实测 <c>curl</c> 的 remote_ip 就是 127.0.0.1），页面又是非 HTTPS，
    /// Chromium 的 Private Network Access 于是直接拦掉整个请求。
    /// </para>
    /// <para>
    /// 修法是把库放进演示站自己的静态目录（<c>demo/Element.Blazor.Demo/wwwroot/js/vendor/</c>，
    /// 取处与逐字节校验写在同目录 <c>README.md</c>）。下面四条判据分别挡住：① 外链地址重新回来、
    /// ② 站内那份被改动或被别的构建产物顶掉、③ 示例被删掉后判据①变成"恒真"的空守卫、
    /// ④ 换行转换让<b>全新克隆</b>里检出的 vendor 文件不再是上游那一份字节（判据② 于是在干净克隆上假红）。
    /// </para>
    /// <para>
    /// <b>范围是有界的</b>：本文件只管"由组件加载的脚本/库地址"。演示数据里的<b>图片与视频</b>
    /// （<c>media.w3.org</c>、<c>*.elemecdn.com</c>、<c>test-streams.mux.dev</c>）不在这里拦 ——
    /// 它们只影响那一条示例能不能放出来，不会在控制台造出 CORS 拦截，且各演示页正文已写明
    /// "实际项目请把地址换成内网可访问的"。
    /// </para>
    /// </remarks>
    public class VendoredLibraryGuardTests
    {
        /// <summary>第三方公共 CDN 域名（只列"脚本/库"常用的这几家，按字面匹配）。</summary>
        private static readonly string[] CdnHosts =
        {
            "cdn.jsdelivr.net", "unpkg.com", "cdnjs.cloudflare.com", "cdn.bootcdn.net", "bootcdn.net",
            "ajax.googleapis.com", "fonts.googleapis.com", "fonts.gstatic.com", "cdn.staticfile.org",
        };

        private static readonly string[] ScannedRoots = { "src", "demo" };

        private static readonly HashSet<string> SkippedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", "publish", "node_modules", ".git", "vendor",
        };

        private static string RepoRoot()
        {
            var dir = new Uri(typeof(VendoredLibraryGuardTests).Assembly.Location).LocalPath;
            for (var i = 0; i < 10 && !string.IsNullOrEmpty(dir); i++)
            {
                if (File.Exists(Path.Combine(dir, "src", "Components", "Element.csproj")))
                {
                    return dir;
                }

                dir = Path.GetDirectoryName(dir);
            }

            throw new InvalidOperationException("找不到仓库根（要含 src/Components/Element.csproj）。");
        }

        private static IEnumerable<string> ScanFiles()
        {
            var root = RepoRoot();
            foreach (var top in ScannedRoots)
            {
                var dir = Path.Combine(root, top);
                if (!Directory.Exists(dir))
                {
                    continue;
                }

                foreach (var file in Directory.EnumerateFiles(dir, "*.razor", SearchOption.AllDirectories))
                {
                    var parts = file.Split(Path.DirectorySeparatorChar);
                    if (SkippedDirectories.Overlaps(parts))
                    {
                        continue;
                    }

                    yield return file;
                }
            }
        }

        /// <summary>
        /// 判据①：任何 <c>参数="http(s)://&lt;公共 CDN&gt;/..."</c> 形式的赋值都不许出现。
        /// 只匹配<b>引号里的地址</b>，所以正文里提到 <c>cdn.jsdelivr.net</c> 解释病因不会被误伤。
        /// </summary>
        [Fact]
        public void LibraryUrlAttributesNeverPointAtPublicCdn()
        {
            var hits = new List<string>();
            foreach (var file in ScanFiles())
            {
                var text = File.ReadAllText(file, Encoding.UTF8);
                foreach (var host in CdnHosts)
                {
                    foreach (var scheme in new[] { "https://", "http://" })
                    {
                        var needle = "=\"" + scheme + host;
                        var at = text.IndexOf(needle, StringComparison.Ordinal);
                        while (at >= 0)
                        {
                            var line = text.Substring(0, at).Count(c => c == '\n') + 1;
                            hits.Add(Path.GetRelativePath(RepoRoot(), file) + ":" + line + " " + needle);
                            at = text.IndexOf(needle, at + 1, StringComparison.Ordinal);
                        }
                    }
                }
            }

            Assert.True(hits.Count == 0,
                "演示站/组件库把第三方库地址指向了公共 CDN，内网与非 HTTPS 部署会被 CORS/Private Network Access 拦掉。" +
                "请改成站内相对路径（库放 demo/Element.Blazor.Demo/wwwroot/js/vendor/），命中清单：\n" +
                string.Join("\n", hits));
        }

        /// <summary>
        /// 判据②：站内那份 hls.js 必须与 npm 官方 tarball <c>hls.js-1.6.15.tgz</c> 里的逐字节一致。
        /// 这里用 MD5 只作<b>改动指纹</b>（防误覆盖、防换版本没记档），不当安全校验用。
        /// </summary>
        [Theory]
        [InlineData("demo/Element.Blazor.Demo/wwwroot/js/vendor/hls.min.js", 541870, "69991b6ecc5d57cf73bad5b3a1296058")]
        [InlineData("demo/Element.Blazor.Demo/wwwroot/js/vendor/hls.min.js.map", 2383712, "2579fb128c905163dd84662c7b48c8a5")]
        public void VendoredFileStillMatchesOfficialBytes(string relative, long expectedLength, string expectedMd5)
        {
            var path = Path.Combine(RepoRoot(), relative.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), "站内第三方库文件不见了：" + relative);

            var bytes = File.ReadAllBytes(path);
            Assert.Equal(expectedLength, bytes.LongLength);
            using var md5 = MD5.Create();
            var actual = string.Concat(md5.ComputeHash(bytes).Select(b => b.ToString("x2")));
            Assert.Equal(expectedMd5, actual);
        }

        /// <summary>
        /// 判据③（正向接线证人）：第 7 节示例真的把 <c>HlsLibUrl</c> 接到了站内那份。
        /// 没有这条，判据①会因为"示例整段被删掉"而恒绿。
        /// </summary>
        [Fact]
        public void VideoDemoWiresHlsLibUrlToVendoredFile()
        {
            var path = Path.Combine(RepoRoot(), "demo", "Element.Blazor.Demo", "Pages", "VideoPlayerGrowDemo.razor");
            Assert.True(File.Exists(path), "演示页不见了：demo/Element.Blazor.Demo/Pages/VideoPlayerGrowDemo.razor");

            var text = File.ReadAllText(path, Encoding.UTF8);
            Assert.True(text.Contains("HlsLibUrl=\"js/vendor/hls.min.js\""),
                "第 7 节示例没有把 HlsLibUrl 接到站内文件（判据①的正向接线证人：没有它，判据①会因为示例被删而恒绿）。");
        }

        /// <summary>
        /// 判据④：仓库必须把 vendor 目录标成「不做换行转换」，否则<b>全新克隆</b>里检出的那份文件就不是上游那一份字节，
        /// 判据② 会在干净克隆上假红（本机 <c>core.autocrlf=true</c> ＋ <c>* text=auto</c> 时
        /// <c>git add</c> 就报过 <c>LF will be replaced by CRLF the next time Git touches it</c>）。
        /// <para>
        /// 同一个判据带<b>反向对照</b>：vendor 之外的源码仍要落在「会转换」那一档 —— 有人把规则写成
        /// <c>** -text</c> 这种过宽形式时，这条对照先红，而不是留下一个「全站都不换行」的隐性改动。
        /// </para>
        /// </summary>
        [Theory]
        [InlineData("demo/Element.Blazor.Demo/wwwroot/js/vendor/hls.min.js", true)]
        [InlineData("demo/Element.Blazor.Demo/wwwroot/js/vendor/hls.min.js.map", true)]
        [InlineData("src/Components/wwwroot/js/ofd/ofd.umd.min.js", true)]
        [InlineData("demo/Element.Blazor.Demo/Pages/VideoPlayerGrowDemo.razor", false)]
        [InlineData("src/Components/ElDateTimePicker.razor.cs", false)]
        public void VendoredBytesStayExactOnCheckout(string relative, bool expectNoConversion)
        {
            var rule = ResolveTextRule(GitattributesRules(), relative);

            if (expectNoConversion)
            {
                Assert.True(rule == "binary",
                    relative + " 没有落在「不做换行转换」那一档（当前命中规则=" + rule + "）。全新克隆检出时会换行改写，"
                    + "判据② 的字节数与 MD5 会在干净克隆上假红。修法：仓库根 .gitattributes 给 "
                    + relative + "（或其所在 vendor 目录）标 -text。");
            }
            else
            {
                Assert.True(rule != "binary",
                    "换行规则被写宽了：" + relative + " 现在也不做转换（vendor 之外一切照旧）。当前命中规则=" + rule);
            }
        }

        /// <summary>
        /// 判据④ 的前提证人：被守护的那份 <c>hls.min.js</c> 本体确实是 LF 结尾、且落盘没有 CR ——
        /// 既没有 LF 也没有 CR 的文件（例如同目录的 <c>hls.min.js.map</c>，实测 LF=0）换行转换动不了它，
        /// 所以「规则存在」要由这个有 LF 的文件来证明判据不是空转，而不是由整目录来证明。
        /// </summary>
        [Theory]
        [InlineData("demo", "Element.Blazor.Demo", "wwwroot", "js", "vendor", "hls.min.js")]
        [InlineData("src", "Components", "wwwroot", "js", "ofd", "ofd.umd.min.js")]
        public void GuardedVendoredFileActuallyHasLfEndings(params string[] parts)
        {
            var path = Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray());
            var name = parts[parts.Length - 1];
            var bytes = File.ReadAllBytes(path);
            var lf = bytes.Count(b => b == (byte)'\n');
            var cr = bytes.Count(b => b == (byte)'\r');

            Assert.True(lf > 0, name + " 里没有 LF（实测 " + lf + "），那换行转换本来就动不了它，判据④ 是空转。");
            Assert.True(cr == 0, name + " 落盘时已带 " + cr + " 个 CR，判据② 当场就该红，不该由判据④ 解释。");
        }

        /// <summary>读仓库根 <c>.gitattributes</c> 的有效行（去注释、去空行）。</summary>
        private static List<string> GitattributesRules()
        {
            var path = Path.Combine(RepoRoot(), ".gitattributes");
            Assert.True(File.Exists(path), "仓库根没有 .gitattributes，判据④无法解析。");

            return File.ReadAllLines(path, Encoding.UTF8)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && !l.StartsWith("#", StringComparison.Ordinal))
                .ToList();
        }

        /// <summary>
        /// 按 git 的口径取<b>最后一条</b>命中规则，返回该路径的 text 属性档：
        /// <c>binary</c>（-text / text=unset，不做换行转换）、<c>auto</c>（text=auto，跟随 core.autocrlf）、
        /// <c>on</c>（text / text=set）、<c>none</c>（没有规则管它）。
        /// </summary>
        private static string ResolveTextRule(List<string> rules, string relativePath)
        {
            var result = "none";
            foreach (var rule in rules)
            {
                var tokens = rule.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (!PatternMatches(tokens[0], relativePath))
                {
                    continue;
                }

                foreach (var token in tokens.Skip(1))
                {
                    if (string.Equals(token, "-text", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(token, "text=unset", StringComparison.OrdinalIgnoreCase))
                    {
                        result = "binary";
                    }
                    else if (string.Equals(token, "!text", StringComparison.OrdinalIgnoreCase))
                    {
                        result = "none";
                    }
                    else if (string.Equals(token, "text", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(token, "text=set", StringComparison.OrdinalIgnoreCase))
                    {
                        result = "on";
                    }
                    else if (string.Equals(token, "text=auto", StringComparison.OrdinalIgnoreCase))
                    {
                        result = "auto";
                    }
                }
            }

            return result;
        }

        /// <summary>把 gitattributes 的通配模式翻成正则：<c>**</c> 跨目录、<c>*</c>/<c>?</c> 不跨 <c>/</c>。</summary>
        private static bool PatternMatches(string pattern, string relativePath)
        {
            var trimmed = pattern.TrimEnd('/');
            var anchored = trimmed.Contains('/');
            var candidate = (anchored ? relativePath : Path.GetFileName(relativePath)).Replace('\\', '/');

            var sb = new StringBuilder("^");
            for (var i = 0; i < trimmed.Length; i++)
            {
                var c = trimmed[i];
                if (c == '*')
                {
                    if (i + 1 < trimmed.Length && trimmed[i + 1] == '*')
                    {
                        i++;
                        if (i + 1 < trimmed.Length && trimmed[i + 1] == '/')
                        {
                            i++;
                            sb.Append("(?:.*/)?");
                        }
                        else
                        {
                            sb.Append(".*");
                        }
                    }
                    else
                    {
                        sb.Append("[^/]*");
                    }
                }
                else if (c == '?')
                {
                    sb.Append("[^/]");
                }
                else if ("\\.[]{}()+-|^$".IndexOf(c) >= 0)
                {
                    sb.Append('\\').Append(c);
                }
                else
                {
                    sb.Append(c);
                }
            }

            sb.Append("$");
            return System.Text.RegularExpressions.Regex.IsMatch(
                candidate, sb.ToString(), System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }
}
