using ElementPlus.Blazor.Demo;
using ElementPlus;
using ElementPlus.Lang;
using ElementPlus.Markdown;
using ElementPlus.X;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddElementPlusServices();
builder.Services.AddElementPlusX();
// Element.Markdown 可选包：注册 ElMarkdownEditor 工具栏各个按钮的处理程序
builder.Services.AddMarkdown();

// ElTable / ElTree 通过 [Inject] HttpClient 支持 Url 远程取数，必须注册否则组件无法实例化
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var app = builder.Build();

// 库级词条（i18n）：首帧之前把词条取回来，避免无障碍标签先渲染成 fallback 再跳一次。
// ElementLang 内部吞掉 404/断网并记 LastLoadError，所以这里不会因为取不到词条而启动失败。
var lang = app.Services.GetRequiredService<ILang>();
await lang.InitElementLangBaseAsync();

await app.RunAsync();
