# Element-Plus.X

Element-Plus.X provides optional AI and conversation scene components for Element-Plus.

## Install

```powershell
dotnet add package Element-Plus.X --prerelease
```

## Requirements

Install and configure the main Element-Plus package first:

```powershell
dotnet add package Element-Plus --prerelease
```

Load the Element core assets, then load the Element.X stylesheet:

```html
<link rel="stylesheet" href="/_content/Element-Plus.X/css/x.css" />
<script src="/_content/Element-Plus.X/js/x.js"></script>
```

Register the optional X services:

```csharp
builder.Services.AddElementX();
```

Use `Element.X` components and models:

```razor
<ElXBubbleList Items="@messages" />
<ElXSender @bind-Value="@draft" OnSubmit="SendAsync" />
```

The package also includes offline-first service APIs:

```csharp
await foreach (var chunk in RequestService.StreamAsync(request))
{
    assistantMessage.Content += chunk.Content;
}
```

## Package Notes

- Target framework: `net10.0`
- Version line: `0.10.6` (aligned with Element Plus `2.14.0`)
- License: MIT
- Authors: Smart
- Repository: https://github.com/Element-Plus-Blazor/Element-Plus-Blazor
- Changelog: https://github.com/Element-Plus-Blazor/Element-Plus-Blazor/blob/main/CHANGELOG.md
- The assembly name and namespaces stay `Element.X` / `Element`, so the `/_content/Element-Plus.X/...` asset paths above are unchanged by the package rename.
