# Element-Plus

Element-Plus is an independent Blazor component library aligned with Element Plus 2.14, published by Smart.

## Install

```powershell
dotnet add package Element-Plus --prerelease
```

## Usage

Register services:

```csharp
builder.Services.AddElementServices();
```

Load static assets in your host page or Blazor Web App root:

```html
<link rel="stylesheet" href="/_content/Element-Plus/css/index.css" />

<script src="/_content/Element-Plus/js/dom.js"></script>
```

Use Element Plus-style `El*` Razor components:

```razor
<ElButton Type="@ButtonType.Primary">Primary</ElButton>
<ElInput TValue="string" Placeholder="Search" Clearable="true" />
```

## Package Notes

- Target framework: `net10.0`
- Version line: `0.10.6` (aligned with Element Plus `2.14.0`)
- License: MIT
- Authors: Smart
- Repository: https://github.com/Element-Plus-Blazor/Element-Plus-Blazor
- Changelog: https://github.com/Element-Plus-Blazor/Element-Plus-Blazor/blob/main/CHANGELOG.md
- The assembly name and root namespace stay `Element`, so `@using ElementPlus` and the `/_content/Element-Plus/...` asset paths above are unchanged by the package rename.
