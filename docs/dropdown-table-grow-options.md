# ElDropDownTableGrow 下拉表格选择器：面板宽度、紧凑尺寸与字段级搜索

`ElDropDownTableGrow` 把「表格 + 搜索 + 分页 + 行选中」塞进下拉面板，是 Element Plus 自渲染实现
（**零 Radzen 依赖、零第三方 JS**），内嵌表格直接复用本仓库的 `ElTable` / `ElTableColumn`。

本文记录三个可调维度：**搜索范围**、**表格密度**、**面板宽度**。

---

## 1. 搜索范围：`SearchProperties`（可给多个）

| 场景 | 写法 |
|---|---|
| 检索所有公开属性（默认） | 不传 `SearchProperties` |
| 只搜某几个字段 | `SearchProperties="@(new[] { "Name", "Category" })"` |

行为约定：

- 传入字段名后，**只**在这些字段里做 `IndexOf(..., OrdinalIgnoreCase)` 匹配；
- 可给多个字段，**命中任一即算匹配**（OR 语义）；
- 字段名对不上任何属性时不抛异常，只是永远不命中（与 `GetFieldValue` 的宽容语义一致）；
- 留空 / `null` → 回落到反射扫描该行对象的**所有**公开属性。

字段名来自行对象的公开属性，与 `Property`（`ElTableColumn`）用的是同一套命名。

```razor
<ElDropDownTableGrow Items="MockData.Products"
                     ValueField="Name" TextField="Name"
                     SearchPlaceholder="只搜 名称、类别"
                     SearchProperties="@(new[] { "Name", "Category" })"
                     Width="320px" Height="0" PageSize="3">
    <ElTableColumn Property="Name" Text="名称" Width="140" />
    <ElTableColumn Property="Category" Text="类别" Width="90" />
    <ElTableColumn Property="Price" Text="单价" Width="80" />
</ElDropDownTableGrow>
```

> 上例中搜 `西药` 能命中（类别字段），搜 `12.5` 命中不了（单价已被排除）。

## 2. 表格密度：`Size`

`Size` 是 `InputSize` 枚举，**同时作用于触发器和面板内表格**，并透传给内嵌 `ElTable`。

| 值 | 效果 | 内嵌表格类名 |
|---|---|---|
| `InputSize.Normal`（默认） | 正常尺寸 | — |
| `InputSize.Small` | 紧凑：字号 12px、单元格上下 padding 8px、面板留白 6px、搜索框 28px、单元格左右留白 8px | `el-table--small` |
| `InputSize.Large` | 宽松：触发器 36px、面板留白 12px | `el-table--large` |

宿主组件同时会带上 `el-dropdown-table-grow--small` / `--large`，面板级的收紧规则都挂在它下面
（见 `wwwroot/css/index.css`），所以紧凑是「整个面板」的观感而不是只改了表格字号。

```razor
<ElDropDownTableGrow Items="MockData.Products" Size="InputSize.Small" ... />
```

浏览器实测（`demo/verify-fixes.cjs`）：紧凑行高 **30px**、正常行高 **40px**；紧凑面板内搜索框 **28px**。

## 3. 面板宽度：`PanelWidth` / `PanelMinWidth` / `PanelMaxWidth`

历史行为是「`Width` 同时决定触发器和面板」——面板永远和触发器一样宽。现在两者解耦：

| 参数 | 作用 |
|---|---|
| `Width` | **触发器**宽度（如 `240px`） |
| `PanelWidth` | **下拉面板**宽度（如 `640px` / `70vw`）。留空回落到 `Width` |
| `PanelMinWidth` | 面板最小宽度（如 `520px`），比 `PanelWidth` 更弱的约束 |
| `PanelMaxWidth` | 面板最大宽度（如 `90vw`），小屏兜底 |

优先级与默认值：

1. `PanelWidth` 非空 → 面板 `width: {PanelWidth}`；
2. `PanelWidth` 为空 → 面板 `width: {Width}`（与改造前完全一致）；
3. `PanelWidth` 与 `Width` 都为空 → 面板 `width:auto`，且未给 `PanelMinWidth` 时附 `min-width:100%`（不窄于触发器）；
4. `PanelMinWidth` / `PanelMaxWidth` 独立叠加，可任意组合。

```razor
@* 触发器 240px，面板 640px，小屏最多 90vw *@
<ElDropDownTableGrow Items="MockData.Products" Multiple="true"
                     Width="240px" PanelWidth="640px" PanelMaxWidth="90vw"
                     Size="InputSize.Small" ShowPagination="false" PageSize="6" ... >
    <ElTableColumn Property="Id" Text="ID" Width="60" />
    <ElTableColumn Property="Name" Text="名称" Width="160" />
    ...
</ElDropDownTableGrow>
```

> 面板比触发器宽时，列宽之和可以超过触发器宽度；`ElTable` 表宽走
> `width:100%; width:max(100%, Σ列宽px)`，所以列多时是面板内横向滚动，而不是把列压窄。
> 想让「面板刚好装下所有列」，把 `PanelWidth` 设成略大于 `Σ列宽 + 勾选列 55 + 面板 padding 16` 即可。

---

## 两个视觉缺陷的根因（均已修复）

### 多选已选标签「文字/✕ 错位、没有胶囊背景」

`index.css` 里有一条来自 Element Plus 2.x 的规则：

```css
.el-select .el-select__tags > span { display: contents }
```

本组件的触发器**故意复用** `el-select` 类名，于是这条规则命中了我们的
`<span class="el-tag el-tag--info el-tag--small el-tag--light">`，把 `.el-tag` 的盒子整个取消
（实测 `getComputedStyle(tag).display === "contents"`）。后果：

- 标签自身的背景 / 边框 / 内边距 / 高度全部失效；
- 文字与关闭图标被提升为 `.el-select__tags` 的 flex 子项；
- 再叠加 `.el-tag .el-icon-close{position:relative; right:-5px; top:-1px}`、
  `.el-tag--small .el-icon-close{transform:scale(.8)}`、`.el-tag__close{margin-top:-2px}`
  这三个相对偏移，图标就与文字错开了。

修法（全部限定在 `.el-dropdown-table-grow` 作用域内，不污染其它组件）：

- `.el-select__tags` 回到 `position:static`（原为 `absolute; top:50%; translateY(-50%)`），
  换成多行标签时触发器会自己长高；
- `.el-select__tags > .el-tag` 恢复 `inline-flex` 胶囊（`height:24px`、`gap:4px`、`border-radius:4px`）；
- `.el-tag__close.el-icon-close` 的相对偏移与 `scale(.8)` 归零，改 `inline-flex` 居中，14px 圆形按钮。

断言：标签 `display !== contents`、有真实盒子、`position:static`、文字与图标中心差 ≤ 2px、两者都在标签盒内。

### 搜索框 placeholder 和正文一样黑

全仓库 CSS 里没有任何 `::placeholder` 规则，Chromium 于是用了 input 自身的 `color`
（实测 `rgb(48,49,51)`）。现在显式指定：

```css
.el-dropdown-table-grow__search .el-input__inner::placeholder {
  color: var(--el-text-color-placeholder, #a8abb2);
  opacity: 1; /* Firefox 默认再叠一层透明度 */
}
```

实测占位色 `rgb(168,171,178)`，正文色 `rgb(96,98,102)`——亮度 171 vs 98。

---

## 验证

| 层 | 内容 | 结果 |
|---|---|---|
| 单元测试 | `test/Element.ComponentTests/ElDropDownTableGrowTests.cs` 新增 11 例（字段搜索 3、面板宽度 5、尺寸 3） | 该类 22/22；全量 **1429/1429** |
| 浏览器 | `demo/verify-fixes.cjs` 扩展 22 条断言（标签几何、占位色、字段搜索对照组、紧凑行高、面板宽度、新演示区块） | **46/46** |
| 回归 | `verify-pickers` / `browser-test` / `verify-detail` / `verify-ofd` | 29/29 · 12/12 · 55/55 · 25/25 |

字段搜索的**对照组**设计：同一个数据集，第 0 个下拉不限字段搜 `12.5` 命中 1 行（单价参与检索），
第 2 个下拉限定 `Name + Category` 搜 `12.5` 命中 0 行——用同数据同关键字证明参数真的生效，
而不是「碰巧搜不到」。

---

## 相关文档

弹层**定位**（向上展开为何要锚 `bottom`）、「面板开着时滚轮失效」的滚动链真身、
以及「点外面收起」改走 `elementPickerOutsideAttach` 后的行为变化，见 `docs/dropdown-table-grow-placement.md`。
