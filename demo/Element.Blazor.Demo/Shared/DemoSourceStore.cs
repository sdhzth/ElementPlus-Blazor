using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace ElementPlus.Blazor.Demo.Shared;

/// <summary>
/// demo 页源码仓库：把 <c>Pages/**/*.razor</c>、<c>Components/**/*.razor</c>
/// （编译期已按 <c>EmbeddedResource</c> 打进本程序集，见 csproj）按文件名读出来，
/// 供每个示例块右下角的「复制代码 / 查看源代码」使用。
/// </summary>
/// <remarks>
/// Blazor WASM 在浏览器里跑，读不到磁盘上的 .razor 源文件，所以源码必须在编译期
/// 随程序集下发。页面类型全名（keep-alive 的 <c>data-keep-key</c> 前缀）末段
/// 就是文件名：<c>Element.Blazor.Demo.Pages.BorderDemo</c> → <c>BorderDemo.razor</c>。
/// </remarks>
public static class DemoSourceStore
{
    private const string Prefix = "DemoSource.";

    private static readonly Assembly Self = typeof(DemoSourceStore).Assembly;
    private static readonly Dictionary<string, string?> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 按页面类型全名取整份 .razor 源码；取不到（例如页面不在 Pages/Components 下）返回 <c>null</c>。
    /// </summary>
    public static string? GetPageSource(string? pageTypeFullName)
    {
        var fileName = ToFileName(pageTypeFullName);

        if (fileName is null)
        {
            return null;
        }

        lock (Cache)
        {
            if (Cache.TryGetValue(fileName, out var cached))
            {
                return cached;
            }
        }

        var text = Read(fileName);

        lock (Cache)
        {
            Cache[fileName] = text;
        }

        return text;
    }

    /// <summary>页面类型全名 → 源文件名；非法输入返回 <c>null</c>。</summary>
    public static string? ToFileName(string? pageTypeFullName)
    {
        if (string.IsNullOrWhiteSpace(pageTypeFullName))
        {
            return null;
        }

        var name = pageTypeFullName!;
        var nested = name.IndexOf('+');

        if (nested >= 0)
        {
            name = name.Substring(0, nested);
        }

        var dot = name.LastIndexOf('.');

        if (dot >= 0)
        {
            name = name.Substring(dot + 1);
        }

        return name.Length == 0 ? null : name + ".razor";
    }

    private static string? Read(string fileName)
    {
        try
        {
            using var stream = Self.GetManifestResourceStream(Prefix + fileName);

            if (stream is null)
            {
                return null;
            }

            // 带 BOM 自动识别；demo 页源码统一 UTF-8（无 BOM），detect 只作兜底。
            using var reader = new StreamReader(stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);

            return reader.ReadToEnd();
        }
        catch (Exception)
        {
            // 取不到源码只是少了「复制代码」，不能影响页面本身
            return null;
        }
    }
}
