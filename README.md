# ElementPlus-Blazor

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Target Framework](https://img.shields.io/badge/Target-net10.0-9cf.svg)](#)

---

## English

**ElementPlus-Blazor** is an independent Blazor component library aligned with
[Element Plus 2.14](https://element-plus.org/), maintained by **Smart**. It ships a rich set of
Razor components (the `El*` family) and brings the Element Plus look-and-feel to
**Blazor Server**, **Blazor WebAssembly**, and the **Blazor Web App** hosting model.

> The root namespace is `ElementPlus` (formerly `Element`). The NuGet `PackageId`
> stays `Element-Plus`, so static asset paths remain `/_content/Element-Plus/...`.

### NuGet Packages

| Package | Description | Target |
| --- | --- | --- |
| `Element-Plus` | Core component library | `net10.0` |
| `Element-Plus.X` | Extended / extra components | `net10.0` |
| `Element-Plus.Markdown` | Markdown rendering support | `net10.0` |

### Installation

```powershell
dotnet add package Element-Plus
# optional extensions / markdown
dotnet add package Element-Plus.X
dotnet add package Element-Plus.Markdown
```

### Getting Started

1. Register the services in `Program.cs` / `Startup.cs`:

   ```csharp
   builder.Services.AddElementServices();
   ```

2. Reference the static assets in your host page (e.g. `App.razor` or `_Host.cshtml`):

   ```html
   <link rel="stylesheet" href="/_content/Element-Plus/css/index.css" />
   <script src="/_content/Element-Plus/js/dom.js"></script>
   ```

3. Use the `El*` components in a Razor file:

   ```razor
   @using ElementPlus

   <ElButton Type="@ButtonType.Primary">Primary</ElButton>
   <ElInput TValue="string" Placeholder="Search" Clearable="true" />
   ```

### Project Structure

```
src/
  Components/        # Core library (Element-Plus)
  X/                # Extended components (Element-Plus.X)
  Markdown/         # Markdown support (Element-Plus.Markdown)
  VSExtension/      # Visual Studio item/project templates
demo/
  Element.Blazor.Demo/  # Example Blazor application
test/               # Component tests
docs/               # Design & usage documentation
```

### Build from Source

```powershell
dotnet build ElementPlus-Blazor.slnx -c Release
```

### Documentation

- Component & design notes: [`docs/`](docs)
- Release history: [`CHANGELOG.md`](CHANGELOG.md)
- Contribution guide: [`CONTRIBUTING.md`](CONTRIBUTING.md)

### License

Released under the [MIT License](LICENSE).

---

## 中文

**ElementPlus-Blazor** 是一套独立的 Blazor 组件库，对齐 [Element Plus 2.14](https://element-plus.org/)，
由 **Smart** 维护。它提供丰富的 Razor 组件（`El*` 系列），在 **Blazor Server**、
**Blazor WebAssembly** 以及 **Blazor Web App** 托管模型中还原 Element Plus 的视觉风格与交互体验。

> 根命名空间为 `ElementPlus`（原 `Element`）。NuGet 包名 `PackageId` 仍为 `Element-Plus`，
> 因此静态资源路径保持为 `/_content/Element-Plus/...`。

### NuGet 包

| 包名 | 说明 | 目标框架 |
| --- | --- | --- |
| `Element-Plus` | 核心组件库 | `net10.0` |
| `Element-Plus.X` | 扩展 / 额外组件 | `net10.0` |
| `Element-Plus.Markdown` | Markdown 渲染支持 | `net10.0` |

### 安装

```powershell
dotnet add package Element-Plus
# 可选扩展 / Markdown
dotnet add package Element-Plus.X
dotnet add package Element-Plus.Markdown
```

### 快速开始

1. 在 `Program.cs` / `Startup.cs` 中注册服务：

   ```csharp
   builder.Services.AddElementServices();
   ```

2. 在宿主页面（如 `App.razor` 或 `_Host.cshtml`）引用静态资源：

   ```html
   <link rel="stylesheet" href="/_content/Element-Plus/css/index.css" />
   <script src="/_content/Element-Plus/js/dom.js"></script>
   ```

3. 在 Razor 文件中使用 `El*` 组件：

   ```razor
   @using ElementPlus

   <ElButton Type="@ButtonType.Primary">主要按钮</ElButton>
   <ElInput TValue="string" Placeholder="搜索" Clearable="true" />
   ```

### 项目结构

```
src/
  Components/           # 核心库（Element-Plus）
  X/                   # 扩展组件（Element-Plus.X）
  Markdown/            # Markdown 支持（Element-Plus.Markdown）
  VSExtension/         # Visual Studio 项/项目模板
demo/
  Element.Blazor.Demo/ # 示例 Blazor 应用
test/                  # 组件测试
docs/                  # 设计与使用文档
```

### 从源码构建

```powershell
dotnet build ElementPlus-Blazor.slnx -c Release
```

### 文档

- 组件与设计说明：[`docs/`](docs)
- 版本历史：[`CHANGELOG.md`](CHANGELOG.md)
- 贡献指南：[`CONTRIBUTING.md`](CONTRIBUTING.md)

### 许可证

基于 [MIT 许可证](LICENSE) 发布。
