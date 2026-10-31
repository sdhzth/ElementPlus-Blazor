# P2 El Component Entrypoints

Element-Blazor exposes Element Plus-style `El*` Razor component entrypoints. Public application, demo, template, and community examples should use only `El*` tags.

## Core Entrypoints

`ElButton`, `ElButtonGroup`, `ElContainer`, `ElHeader`, `ElAside`, `ElMain`, `ElFooter`, `ElIcon`, `ElRow`, `ElCol`, `ElLink`, `ElText`, `ElScrollbar`, `ElSpace`, `ElSplitter`, `ElSplitterPanel`, `ElInput`, `ElInputNumber`, `ElInputTag`, `ElInputOtp`, `ElAutocomplete`, `ElCascader`, `ElRate`, `ElSlider`, `ElTimeSelect`, `ElForm`, `ElFormItem`, `ElFormActionItem`, `ElSelect`, `ElOption`, `ElTable`, `ElTableColumn`, `ElTableColumns`, `ElTableTemplateColumn`, `ElTableDateTimeColumn`, `ElTableCheckBoxColumn`, and `ElTableTreeColumn` are the public core entrypoints.

Additional Element Plus-style entrypoints exist for the rest of the current component surface, including `ElCard`, `ElCheckbox`, `ElRadio`, `ElSwitch`, `ElMenu`, `ElPagination`, `ElTabs`, `ElTabPane`, `ElTag`, `ElTree`, `ElUpload`, `ElDialogGrow` (modal dialog, Element Plus self-rendered, zero Radzen dependency), `ElDropDownTableGrow` (dropdown table picker, popup embeds `ElTable` for row selection, field-scoped search via `SearchProperties`, compact/normal density via `Size`, panel width decoupled from the trigger width via `PanelWidth` / `PanelMinWidth` / `PanelMaxWidth`, zero Radzen dependency — see `docs/dropdown-table-grow-options.md`), `ElChartGrow` (chart component: line/area/column/bar/scatter/bubble/pie/donut/radar + stacked variants, pure C# SVG self-rendered, zero Radzen dependency), `ElHeatmapGrow` (heatmap component: category grid or numeric-coordinate grid, linear or bucketed colour mapping with a colour-scale legend, pure C# SVG self-rendered, zero Radzen dependency), `ElMarqueeGrow` (scrolling marquee component: four directions, seamless pure-CSS animation with zero JS, controllable corner radius via `Round` / `RoundRadius`, accepts arbitrary `ChildContent`, pure self-rendered, zero BootstrapBlazor dependency), `ElVerifyCodeGrow` (captcha input: generated math-captcha SVG beside a bindable input (`Value` / `ValueChanged`, `@bind-Value`), click / keyboard / timer (`AutoRefreshInterval`) refresh with `OnRefresh` event, noise complexity via `NoiseComplexity`, one-shot validation via `ValidateAsync`, border and corner radius controllable via `ShowBorder` / `Round` / `RoundRadius`, pure self-rendered, zero JS), `ElWaitingPopupGrow` (waiting / processing popup: full-screen translucent overlay + centered card with Element Plus-style inline-SVG spinning loader, title, message, optional cancel button; `Show()` / `Hide()` imperative API or `Visible` + `@bind-Visible`, `MaskClosable` to allow dismiss on overlay click, `OnCancel` callback, `ChildContent` extra slot, pure self-rendered, zero JS), `ElFormFieldGrow` (form-field container: floating/fixed label, leading & trailing slots, helper + error text, Outlined/Filled/Flat/Text variants, zero JS via CSS `:focus-within`, browser-compatibility guarded — see `docs/form-field-alignment.md`), `ElMarkdownGrow` (Markdown renderer: hand-written CommonMark subset + GFM tables/task lists/strikethrough, HTML sanitiser with tag & attribute whitelists, heading auto-anchors, `Text` or `ChildContent` input, zero JS and zero third-party parser, browser-compatibility guarded — see `docs/markdown-alignment.md`), `ElHtmlEditorGrow` (rich-text editor: data-driven toolbar, design/source modes, insert link/image/table, table row-column-merge editing, paste sanitiser, image upload, pure-C# HTML building, self-written ES5 JS channel — see `docs/html-editor-alignment.md`), `ElBarcodeGrow` (1D barcode renderer, see `docs/barcode-alignment.md`), `ElQRCodeGrow` (QR Code renderer, see `docs/qrcode-alignment.md`) and `ElPivotDataGridGrow` (pivot / cross-tab grid: declarative `ElPivotColumnGrow` / `ElPivotRowGrow` / `ElPivotAggregateGrow` fields, pure-C# engine with grouping, 8 aggregate functions, 17 filter operators, drill-down, field picker and paging, zero JS and zero third-party dependency — see `docs/pivot-data-grid-alignment.md`) and `ElViewerGrow` (document viewer: Element Plus self-rendered shell + browser-native / pure-C# document rendering, data-driven toolbar, zoom ladder & rotation via CSS transform, in-page maximize, native `<a download>` download, `@media print` printing, URL safety whitelist, zh-CN/en strings, plus a built-in **lazily loaded OFD channel** (`EnableOfdPreview`, off means back to `Unsupported`) whose only JS surface is `dom.js`'s ES5 `elementOfdViewer*` bridge fetching `wwwroot/js/ofd/ofd.umd.min.js` on demand — see `docs/viewer-alignment.md`) and `ElAccountTableGrow` (accounting voucher grid: 摘要 / 会计科目 / 借方金额(亿…分) / 贷方金额(亿…分) with per-digit amount cells, hover ⊕/⊗ row add-remove, a non-editable auto-totalled 合计 row that prints `合计:大写金额` when the two sides balance, Enter-to-next-cell navigation ending on the next row's 摘要 column, `ReadOnly` mode, and `LoadRows` / `GetData` for one-line load & save, zero JS and zero third-party dependency — see `docs/account-table-alignment.md`) and `ElNotification` (notification toasts: `NotificationService` queue rendered by the `ElPopup` host — `ElNotification` is a thin shell over it. Element Plus-aligned options: nullable `Type` (empty by default ⇒ no icon, matching EP), `Title` / `Message` / `MessageContent`, `DangerouslyUseHTMLString`, `Icon`, `CustomClass`, `Duration` (0 = sticky), four-corner `Position`, `Offset`, `ShowClose` + `CloseIcon`, `OnClick`/`OnClose`, `ZIndex`, `pauseOnHover` timer suspension (2.14.4), `Progress` + `ProgressColor` countdown bar (2.14.4), `Grouping` merge for identical notices, plus `Primary()` / `UpdateOffsets(position)` service methods and `Max` queue cap — see `docs/notification-alignment.md`).

`ElProgress` (progress bar: line/circle/dashboard, `ColorStops` gradient via `ProgressColorStop`, `ColorFunction`, `FormatFunc`, striped/indeterminate modes), `ElResult` (result page: success/warning/info/error/primary icon types with slot overrides), `ElTableV2` (virtualized table aligned with Element Plus TableV2: tree data via `ExpandColumnKey`/`TreeChildrenField`/`IndentSize`, `SortBy`/`SortState`/`ColumnSort` emit-only sorting, `FixedData` sticky rows, `FooterContent`/`OverlayContent` slots, `ScrollToAsync`/`ScrollToTopAsync`/`ScrollToLeftAsync`/`ScrollToRowAsync` exposes — see `docs/table-v2-alignment.md`) are additional data-display entrypoints.

## Table Column Shape

`ElTableColumn` can be used directly under `ElTable`:

```razor
<ElTable DataSource="Datas" AutoGenerateColumns="false">
    <ElTableColumn Property="@nameof(Row.Name)" Text="Name" />
    <ElTableTemplateColumn Text="Action">
        <ElButton Type="ButtonType.Primary" Size="ButtonSize.Small">Edit</ElButton>
    </ElTableTemplateColumn>
</ElTable>
```

The grouped column shape remains available:

```razor
<ElTable DataSource="Datas" AutoGenerateColumns="false">
    <ElTableColumns>
        <ElTableColumn Property="@nameof(Row.Name)" Text="Name" />
    </ElTableColumns>
</ElTable>
```

## Recently Added Entrypoints (2026-09-28)

`ElProTabsGrow` (ProTabs-style multi-tab navigation: capsule tabs with dot/icon + title + close button, left/right scroll arrows on overflow, "more" dropdown with close-others/close-all; `ActiveTextColor` parameter or `SetActiveTextColor()`/`ResetActiveTextColor()` runtime methods for the active tab colour; `CloseButtonDisplay` Hover/Always — X shows only on hover by default; `ShowRefresh` + per-item `Refreshable` raise `OnRefreshTab` from a always-visible label icon and from the context menu's 刷新 item — the same event, and the click does not select the tab (`stopPropagation`), so the host rebuilds only that tab's content subtree (see `docs/pro-tabs-alignment.md` §13); pure self-rendered, zero external dependency; the demo shell uses it for persistent multi-tab navigation restored from localStorage on hard refresh).

## Navigation / Layout Entrypoints (aligned 2026-09-30)

`ElAffix`, `ElAnchor` + `ElAnchorLink`, `ElBacktop`, `ElBreadcrumb` + `ElBreadcrumbItem`, `ElDropdown` + `ElDropdownItem`,
`ElMenu` + `ElSubMenu` / `ElMenuItem` / `ElMenuItemGroup`, `ElPageHeader`, `ElSteps` + `ElStep`, `ElTabs` + `ElTabPane`
were re-aligned against the official Element Plus docs. Alignment notes live in
`docs/affix-alignment.md`, `docs/anchor-alignment.md`, `docs/backtop-alignment.md`, `docs/breadcrumb-alignment.md`,
`docs/dropdown-alignment.md`, `docs/menu-alignment.md`, `docs/page-header-alignment.md`, `docs/steps-alignment.md`,
`docs/tabs-alignment.md`.

Highlights (all pre-existing extensions preserved):

- `ElAnchor`: `SelectScrollTop` (EP 2.10.5), per-link `Target` / `Offset`, `ScrollToAsync(href)`, read-only `ActiveHref`.
  `CursorTemplate` is a 本库扩展，不是官方 `cursor` 插槽（EP 2.14.0 的 `anchor.vue.d.ts` 里 `__VLS_Slots` 只有 `default`，
  `theme-chalk/el-anchor.css` 中 `cursor` 出现 0 次）；口径见 `docs/anchor-alignment.md` 的订正说明。
- `ElDropdown`: `Placement`, `MaxHeight`, `Tabindex`, `Role`, `Id` / `AriaLabel`, `OnVisibleChange` / `OnOpen` / `OnClose`; `ShowTimeout` default now 250 (official).
- `ElMenu`: `Ellipsis` (text truncation half of the official feature), `Id` / `AriaLabel`; exposes already `OpenAsync` / `CloseAsync` / `HandleResizeAsync`.
- `ElTabs`: `OnTabClick` (official tab-click), `OnEdit` (add / remove), exposes `CurrentName`; `ElTabPane` gains `Label` alias, `TitleTemplate` (label slot) and `Lazy`.
- `ElStep`: `Space` (official Step.space, overrides `ElSteps.Space`).

Dedicated demo pages: `/affix`, `/anchor`, `/backtop`, `/breadcrumb`, `/dropdown`, `/menu`, `/page-header`, `/steps`, `/tabs`.

## Public Example Policy

Public examples in `demo`, `template`, and `community` use `El*` tags. Element Plus-style parameter names are used in examples:

| Public API | Use |
| --- | --- |
| `Disabled` | Disable controls. |
| `Loading` | Button loading state. |
| `Plain` | Plain button style. |
| `Round` | Round button style. |
| `Circle` | Circle button style. |
| `Clearable` | Input clear button. |
| `Required` | Form item required state. |
| `Error` | Form item immediate error message. |
| `LabelPosition` | Form label alignment. |
| `Prop` | Form item model property. |
| `Model` | Form model object. |
| `Rules` | Form-level validation rules keyed by `Prop`/`Name`. |
| `Validate` / `ValidateField` | Validate all fields or selected fields. |
| `ResetFields` / `ClearValidate` | Reset field values or clear validation state. |
| `ScrollToFieldAsync` | Scroll a form field into view. |
| `OnSubmitForm` / `OnValidSubmitForm` / `OnInvalidSubmitForm` | Typed form submit callbacks with the `ElForm` instance and validation result. |
| `LabelContent` / `ErrorContent` | Form item label and error render fragments. |
