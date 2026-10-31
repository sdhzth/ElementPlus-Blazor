# Element-Blazor 安装与开发指南

适用版本：主包 `Element-Plus` **0.10.6**，目标框架 **net10.0**，命名空间 **`Element`**，组件前缀 **`El`**。
配套可选包：`Element-Plus.Markdown`（Markdown 编辑器）、`Element-Plus.X`（AI / 会话相关可选组件）。
包 id 与静态资源基路径于 2026-10-18 由 `Element` / `Element.X` / `Element.Markdown` 改为本名（C# 命名空间未变），
口径与破坏性见 `docs/package-identity-alignment.md`。

本文回答三件事：**怎么装**、**`index.html` 里要放什么**、**`Program.cs` 里要改什么**，
末尾给出本地源码开发的工作流与排坑清单。

---

## 1. 三种安装方式

### 1.1 用 NuGet 包（推荐，业务项目）

```powershell
dotnet add package Element-Plus
# 可选
dotnet add package Element-Plus.Markdown
dotnet add package Element-Plus.X
```

### 1.2 引用本地源码（改组件库同时调试业务项目）

```xml
<!-- 业务项目.csproj -->
<ItemGroup>
  <ProjectReference Include="E:\电子病历\Element-Blazor-master\src\Components\Element.csproj" />
</ItemGroup>
```

项目引用同样支持 `_content/Element-Plus/...` 静态资源（.NET 6+ 起 RCL 静态资源对引用方可见），
所以下面的资源引用写法在两种方式下完全一致。

### 1.3 内网离线：本地包源

```powershell
# 打包：版本号已在三个 csproj 的 <Version>0.10.6</Version> 里落死，不要再加 -p:Version 覆盖。
# 必须先 --no-incremental 构建一次：dotnet pack 会复用"已是最新"的构建产物，
# 那样 nuspec 写的是新版本、包内 dll 的程序集版本还是旧的（实测过这个坑）。
dotnet build src/Components/Element.csproj -c Release --no-incremental
dotnet pack src/Components/Element.csproj -c Release -o D:\local-nuget

# 业务项目里加本地源（或在 %APPDATA%\NuGet\NuGet.Config 里加一次）
dotnet nuget add source D:\local-nuget -n local
dotnet add package Element-Plus --version 0.10.6
```

> ⚠️ **同一个版本号换过一次内容时，NuGet 全局缓存不会重新解包**。
> 实测（2026-10-13，包改名前的读数）：本机 `~/.nuget/packages/element/0.10.0/element.0.10.0.nupkg` 的 MD5 与 `nuget/Element.0.10.0.nupkg` 不一致，
> 但 `dotnet restore` 直接沿用缓存里那份旧包。改包内容又不升版本号时，先删缓存里对应包目录再 restore
> （只删自己的包 id：改名后是 `element-plus`、`element-plus.x`、`element-plus.markdown`；
> 本机还留着改名前的 `element`、`element.x`、`element.markdown` 三个旧目录。2026-10-18 缓存普查
> （`Temp/b53-cache-census.log`）：`~/.nuget/packages/element/` 下**同时**躺着我们自己出过的 `0.10.0`~`0.10.6`
> 与对面 nuget.org 的 `2.14.1`，两份 nuspec 的 `title`/`authors`/`description` 逐字相同、dll 都叫 `Element.dll`
> （md5 不同：`15b46b2a…` vs `94e19a3f…`）——这正是包 id 必须改的理由。删缓存前先认清版本目录归谁，别顺手删别人的包目录）。

---

## 2. 静态资源：宿主页面里要放什么

组件库自带一份统一 CSS 与一份 JS，都通过 `_content/Element-Plus/...` 暴露：

| 文件 | 作用 | 说明 |
| --- | --- | --- |
| `css/index.css` | **统一样式表**（2026-09-26 由 `index.css` + `theme.css` + `fix.css` 三段按原顺序合并为单一文件） | 文件内顺序：① Element Plus 原版样式基底 + `element-icons` 图标字体；② 原 `theme.css` 的 token 层（`--el-*` 变量与迁移后的 2.14 选择器，由 `/* ==== THEME_CSS_START ==== */` / `END` 标记包裹）；③ 本仓库补丁层（自渲染组件 `El*Grow` 等样式，位于文件末尾的「自渲染补丁层」段）。体积最大；**只需加载这一个文件** |
| `js/dom.js` | 少数组件需要的自写 JS 通道（ES5，无依赖） | 放在框架脚本之后 |

> ⚠️ **只需一份 CSS**：合并后级联与原「`index → theme → fix`」三文件加载 100% 等价，补丁层仍压过基底与主题，无行为变化。旧项目里若还写着 `<link ... theme.css />` 或 `<link ... fix.css />`，直接删掉这两行即可，否则会 404（不影响其余样式）。

### 2.1 Blazor WebAssembly（`wwwroot/index.html`）

```html
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <title>My App</title>

    <link rel="stylesheet" href="css/app.css" />
    <link rel="stylesheet" href="/_content/Element-Plus/css/index.css" />

</head>
<body>
    <div id="app">Loading...</div>

    <script src="_framework/blazor.webassembly.js"></script>
    <script src="/_content/Element-Plus/js/dom.js"></script>
</body>
</html>
```

### 2.2 Blazor Server（`Pages/_Host.cshtml` 或 `App.razor`）

```html
<head>
    <link rel="stylesheet" href="/_content/Element-Plus/css/index.css" />

</head>
<body>
    @* ... *@
    <script src="_framework/blazor.server.js"></script>
    <script src="/_content/Element-Plus/js/dom.js"></script>
</body>
```

### 2.3 Blazor Web App（.NET 8/9/10 统一模板，`Components/App.razor`）

```razor
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <link rel="stylesheet" href="/_content/Element-Plus/css/index.css" />

    <HeadOutlet @rendermode="InteractiveServer" />
</head>
<body>
    <Routes @rendermode="InteractiveServer" />
    <script src="_framework/blazor.web.js"></script>
    <script src="/_content/Element-Plus/js/dom.js"></script>
</body>
</html>
```

> 站点部署在子路径（如 `https://host/app/`）时，请保证 `<base href="/app/" />` 正确，
> 或把 `_content` 前缀换成应用相对路径，否则静态资源 404。

---

## 3. 服务注册：`Program.cs` 改什么

只需一行 `AddElementServices()`，它注册了消息、通知、对话框、弹出层、加载、表单映射等服务：

| 注册项 | 生命周期 | 用途 |
| --- | --- | --- |
| `MessageService` / `MessageBox` | Scoped | `ElMessage` 提示、确认框 |
| `NotificationService` / `ElNotification` | Scoped | 右上角通知 |
| `DialogService` | Scoped | 对话框 |
| `PopupService` | Scoped | 下拉/弹出层定位 |
| `LoadingService` | Singleton | 全局加载遮罩 |
| `Document` | Scoped | DOM 相关通道 |
| `FormFieldControlMap` / `TableEditorMap` | Singleton | 表单与表格控件映射 |
| `ILang`（`ElementLang`） | Scoped | 库级词条（i18n）。不显式 `Init` 也不影响渲染：取词一律走组件自带的 fallback |
| `IInputRender` → `InputRender` | Scoped | 输入控件渲染 |

> 表里的 `ILang` 是库级词条（i18n）入口：**不调 `Init` 也完全不影响渲染**（组件取不到词就走自带 fallback）。
> 想在首帧之前把词条取回来、以及运行时切语言的正确姿势（含"子组件不会自己跟着换"这个坑），见 `docs/lang-i18n.md`。

### 3.1 Blazor WebAssembly（`Program.cs`）

```csharp
using ElementPlus;                 // AddElementServices 在这里
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddElementServices();     // ← 唯一必需的一行

await builder.Build().RunAsync();
```

### 3.2 Blazor Server / Blazor Web App（`Program.cs`）

```csharp
using ElementPlus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();   // 若用 Server 交互

builder.Services.AddElementServices();                // ← 必需

var app = builder.Build();
// ... 中间件 ...
app.Run();
```

> 若同时用 WebAssembly 客户端项目（`.Client`），**两端都要各注册一次**：
> 服务端 `Program.cs` 与客户端 `Program.cs` 各写一行 `builder.Services.AddElementServices();`。

### 3.3 `_Imports.razor`

```razor
@using ElementPlus
```

（不想全局引入时，也可以在单个组件里 `@using ElementPlus`。）

---

## 4. 最小可运行示例

```razor
@page "/demo"
@using ElementPlus

<ElButton Type="@ButtonType.Primary" OnClick="OnClick">点我</ElButton>
<ElInput TValue="string" @bind-Value="keyword" Placeholder="搜索" Clearable="true" />
<ElTable TItem="Row" Data="@rows">
    <ElTableColumn TItem="Row" Property="Name" Label="姓名" />
</ElTable>

@code {
    private string keyword;
    private List<Row> rows = new();

    private async Task OnClick()
    {
        await MessageService.ShowAsync("已点击");
    }

    public class Row { public string Name { get; set; } }
}
```

文档预览器示例（自渲染、零第三方依赖；除下面这条懒加载的 OFD 通道外零自定义 JS）：

```razor
@* PDF / 图片 / 音视频 / 文本 / Markdown / CSV 直接给地址或字节 *@
<ElViewerGrow Src="@pdfUrl" Height="720px" />

@* OFD 默认就能开：核心脚本只在真的打开 .ofd 时才按需下载（约 0.9 MB） *@
<ElViewerGrow Src="/ofd/out.ofd" FileName="out.ofd" Height="560px" />

@* 不想用内置通道 / 想换成自己的库，见 docs/viewer-alignment.md 第 5.2 节 *@
<ElViewerGrow Document="@report" EnableOfdPreview="false" />
```

---

## 5. 本地源码开发工作流

```bash
# 1) 编译组件库
cd E:/电子病历/Element-Blazor-master
dotnet build src/Components/Element.csproj -c Release

# 2) 只跑某个组件的测试（推荐，见下）
cd C:/Users/Administrator/.workbuddy/ce_build/<组件>_verify
dotnet test <组件>Verify.csproj -c Release

# 3) 全量体检（排除其它会话正在改的半成品文件）
cd C:/Users/Administrator/.workbuddy/ce_build/full_verify
dotnet test FullVerify.csproj -c Release
```

**为什么要用隔离工程**：`test/Element.ComponentTests` 是多个会话共用的目录，
一旦有文件处于半成品状态（引用了当前 bUnit 版本不存在的 API），整个测试程序集就编不过，
其余上百个组件的测试会一起跑不起来。隔离工程用 `EnableDefaultCompileItems=false` +
`Compile Include/Link` 只编译指定文件（或排除坏文件），与仓库整体编译状态解耦。

**新增组件的约定**：

- 文件放 `src/Components/ElXxx.razor` + `.razor.cs`，命名空间 `Element`，基类 `ElementComponentBase`；
- 样式追加到 `src/Components/wwwroot/css/index.css` **末尾的补丁层**（原 `fix.css`，2026-09-26 并入），
  段首用统一的分段注释
  （`/* ====…\n * ElXxx：… */`），选择器必须全部以 `.el-xxx` 作用域开头；
- CSS 不用 `:has()/:is()/:where()/:focus-visible/accent-color/clamp()`、
  逻辑属性、`gap`、`!important`，每个 `var(--el-*)` 都要带静态回退（有守卫测试会查）；
- 需要 JS 时只能复用 `wwwroot/js/dom.js` 里的 `element*` 通道，且必须 ES5；
- 测试文件放 `test/Element.ComponentTests/ElXxxTests.cs`，并配一个隔离 csproj。

---

## 6. 排坑清单

| 现象 | 原因 | 处理 |
| --- | --- | --- |
| 组件有结构没样式 | 两份 CSS 没引全 | 按 `index.css（含 theme 段）` 顺序引入 |
| 图标全是方框 | `index.css` 会去 `css/fonts/element-icons.woff` 取字体；子路径部署时相对路径会 404 | 检查 `_content/Element-Plus/css/fonts/` 是否可达 |
| 弹层/消息不出现 | 忘了 `AddElementServices()` | 服务端与 WASM 客户端**各注册一次** |
| 只有部分交互无效 | `dom.js` 没引，或引在框架脚本之前 | 放在 `blazor.*.js` 之后 |
| 改了 `index.css` 补丁层没生效 | MSBuild 增量编译没触发（编辑工具写回后时间戳未更新） | `touch` 该文件后重新 build；仍不行就 `dotnet build-server shutdown` |
| 打印只出第一屏 | 旧版外壳把宽高写成内联样式，打印媒体查询盖不住 | 2.14 起 `ElViewerGrow` 的尺寸与 transform 走 CSS 变量（`--el-viewer-grow-w/-h/-transform`），`@media print` 已放开 |
| 深色主题打印全黑 | 画布背景是深色变量 | 打印规则里已反白；自定义主题需自行补 `@media print` |
| NuGet restore 报 `Value cannot be null. (path1)` | 宿主会话缺少系统环境变量（APPDATA / ProgramData / **`ProgramFiles(x86)`** 等） | 用 `env` 前缀补齐这些变量再执行；不要裸跑 |
| 构建报 `Access to the path is denied` | MSBuild 常驻节点持锁 | `dotnet build-server shutdown` 后加 `-nodeReuse:false` 重跑 |
| 跑全量测试编不过 | 其它会话的半成品文件 | 用隔离工程排除，不要去改别人的文件 |

---

## 7. 相关文档

- 组件用法：仓库 README、演示站 <https://element-blazor.github.io/>
- 文档预览器：`docs/viewer-alignment.md`（含 OFD/PDF 支持与打印说明）
- 交叉表：`docs/pivot-data-grid-alignment.md`
- 富文本编辑器：`docs/html-editor-alignment.md`
- 二维码 / 条形码：`docs/qrcode-alignment.md`、`docs/barcode-alignment.md`
- 组件入口清单：`docs/p2-el-component-entrypoints.md`
- **验证层手册：`docs/testing-guards.md`** —— 七条常驻守卫（渲染树序列号漂移 / 捕获后加属性 / 等待必须显式超时 / 探针卫生 / 词条资源体检 / 浮层队列归属 / 源码只许 UTF-8）、写守卫的五步配方、`demo/tools/probes/` 探针清单与跑法、"是组件错还是测试错"的取证顺序。改组件、加测试或验收前先读它。浮层（Loading / 消息 / 通知 / 对话框 / 下拉）的宿主与生命周期契约单开一篇：**`docs/popup-host-isolation.md`**；源码存盘编码（GBK 为什么能潜伏）单开一篇：**`docs/source-encoding.md`**。
- **库级词条（i18n）：`docs/lang-i18n.md`** —— 词条运行时怎么接、EP 权威取值、组件为什么要自己订阅 `LocaleChanged`、以及"不 Init 就零 DOM 变更"的保证在哪。
