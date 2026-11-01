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

### Admin Starter Template (中台模板)

The `demo/` project is **not only a component gallery** — it is also a **ready-to-fork admin
shell**. Fork it and replace the in-memory Mock data with your own API to get a working backend
in minutes. The admin pages live under the **「中台模板」** sidebar group:

| Route | Page | What it demonstrates |
| --- | --- | --- |
| `/login` | 登录页 Login | A **standalone login layout** (`@layout LoginLayout`) decoupled from the main shell. |
| `/dashboard` | 控制台 Dashboard | Stat cards (`ElStatistic`) + recent-data table + todo timeline. |
| `/admin/users` | 用户管理 | List + CRUD via a **service dialog** (`ElDialogGrowService.OpenAsync`). |
| `/admin/roles` | 角色管理 | List + CRUD via an **inline `ElDialog` form** (no service). |
| `/admin/permissions` | 权限管理 | Same pattern, with an enum **dropdown** (菜单 / 按钮 / 接口). |
| `/admin/menus` | 菜单管理 | Same pattern, with a dynamic **parent-menu dropdown** + numeric sort. |

**How the navigation stays consistent:** every sidebar item, tab title and breadcrumb is derived
from the single source of truth `demo/.../Shared/DemoNavigation.cs` (fixed entries + grouped
sections). Add a page → add one `DemoNavEntry` → the menu / tab / breadcrumb all follow.

**How to turn a template page into your real backend:**

1. Inject your `HttpClient` / service into the page (e.g. `UserManage.razor`).
2. Replace the in-memory `List<T>` (e.g. `users`, `roles`, `perms`, `menus`) with calls to your
   API: load in `OnInitializedAsync`, and call `POST/PUT/DELETE` in `AddAsync` / `EditAsync` /
   `DeleteAsync`.
3. Swap the model classes in `demo/.../Models/` (`EmployeeModel`, `RoleModel`,
   `PermissionModel`, `MenuModel`) for your DTOs.

Two CRUD dialog styles are intentionally shown so you can pick one:
- **Service dialog** (`UserManage`): open any component with one line and `await` its return value.
- **Inline dialog** (`RoleManage` / `PermissionManage` / `MenuManage`): a local `ElDialog` whose
  form binds directly to a page field — fully self-contained, easiest to copy into a new page.

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
