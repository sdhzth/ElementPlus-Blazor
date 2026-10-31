<p align="center">
  <a href="README.md"><strong>English</strong></a> ·
  <a href="README.zh-CN.md"><strong>中文</strong></a>
</p>

# ElementPlus-Blazor

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Target Framework](https://img.shields.io/badge/Target-net10.0-9cf.svg)](#)

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
   builder.Services.AddElementPlusServices();
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
