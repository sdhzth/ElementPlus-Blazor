# P1 Style Inventory And Layering

This inventory records how the legacy style files are treated after the Element Plus 2.14 token layer landed and was unified into a single stylesheet.

## Unification (2026-09-26)

`index.css`, `theme.css` and `fix.css` were merged into **one file called `index.css`**. The single file preserves the original cascade order exactly:

```
index.css  =  [legacy theme-chalk baseline]      (top)
           +  /* ==== THEME_CSS_START ==== */     (Element Plus 2.14 token layer, former theme.css)
              ... --el-* variables & migrated selectors ...
              /* ==== THEME_CSS_END ==== */
           +  /* fix.css folded */                (Element-Blazor patch layer, former fix.css)
              ... self-rendered El*Grow styles, layout/scrollbar compatibility ...
```

Because base → theme → fix sit in that order inside one file, the cascade is **identical** to the old three-`<link>` load order, so the patch layer still overrides both the baseline and the token layer. `theme.css` and `fix.css` were deleted.

Load only:

```html
<link rel="stylesheet" href="/_content/Element-Plus/css/index.css" />
```

> Historical note: the old README once used `fix → index → theme`, which let `index.css` override the patches — that order is obsolete. The recommended order then became `index → theme → fix` (patches last); unification keeps that same order inside one file.

## File Decisions

| File | Decision | Notes |
| --- | --- | --- |
| `src/Components/wwwroot/css/index.css` | Unified baseline + token layer + patch layer (merged 2026-09-26). | The theme-chalk bundle is at the top; the former `theme.css` token rules sit inside the `THEME_CSS_START`/`THEME_CSS_END` markers; the former `fix.css` patch rules sit at the very end under the「自渲染补丁层」banner comment. Do not insert new styles between these sections. |
| ~~`src/Components/wwwroot/css/theme.css`~~ | Removed (folded into `index.css`). | `--el-*` variable layer now lives inside the `THEME_CSS_START/END` block. |
| ~~`src/Components/wwwroot/css/fix.css`~~ | Removed (folded into `index.css`). | Patch/layout fixes, self-rendered `El*Grow` styles and scrollbar compatibility now live at the end of `index.css`. Do not place new design tokens there. |
| Template host pages | Load `index.css` only. | Server, WebAssembly, PWA, and SEO samples now load the single unified file. |

## Migration Rules

- New visual work prefers `--el-*` variables (the token rules live in the `THEME_CSS_START/END` block of `index.css`) and keeps selectors compatible with `el-*` and `is-*`.
- Migrate hard-coded colors, borders, radius, shadows, spacing, and component sizes to `--el-*` variables.
- Keep public DOM classes stable: `el-*` and `is-*` selectors remain the contract.
- Narrow layout compatibility / one-off fixes go to the patch (`fix.css` folded) section at the end of `index.css`.
- Leave the `index.css` baseline section as an upstream snapshot until the component surface has enough variable-backed replacements to safely split or regenerate it.

## CSS Guard Tests

The bUnit compatibility guards read `index.css` directly:

- Patch-layer guards locate a component marker (e.g. `ElFormFieldGrow`, `ElDateTimePicker：时间编辑器`) and assert the section contains no `gap:`, `:has(`, `:is(`, `:where(`, `clamp(`, `:focus-visible`, `scrollbar-gutter`, or `margin/padding/border-inline` / `inset-block/inline` (Win7 legacy-kernel target). New patch blocks must start with the `/* ==== ComponentName ==== */` banner so the guard can delimit them.
- The token-layer guard extracts the block between `/* ==== THEME_CSS_START ==== */` and `/* ==== THEME_CSS_END ==== */` and runs the same modern-syntax assertions on it.

## Current Tokenized Surfaces

- Global color, text, background, fill, border, radius, shadow, transition, size, overlay, and z-index tokens.
- Button, Input, Table, Select, Dialog, Tabs, Menu, Popper, Select dropdown, table loading and empty states.
- Compatibility size aliases for existing Element-Blazor values: medium and mini.

## Layering Rules

Element Plus 2.14 uses `--el-index-*` as the shared z-index vocabulary. Element-Blazor follows that vocabulary in the token block of `index.css`:

| Token | Default | Use |
| --- | --- | --- |
| `--el-index-normal` | `1` | Component-local stacking such as table internals. |
| `--el-index-top` | `1000` | Sticky/high local surfaces. |
| `--el-index-popper` | `2000` | Popper, select dropdown, menu popups, dialog overlays. |

The popup renderer still allocates per-instance z-index numbers in C# for ordering multiple active overlays. CSS surfaces should use `--el-index-popper`, `--el-box-shadow`, `--el-box-shadow-light`, or component-specific aliases so Dialog, Dropdown, Select, Popover and Tooltip can share the same visual stack when their component implementations are added or completed.
