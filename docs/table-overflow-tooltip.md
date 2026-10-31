# 表格截断单元格悬停看全文：`ElTable` / `ElTableV2` / `*TableGrow`（2026-10-08）

> 触发点同 `docs/text-ellipsis-tooltip.md`（用户要求「eltable、eltablev2 和 eltablegrow 有 Ellipsis 的时也这样处理」）。
> 共用件、`position:fixed` 的物理与那 6 条变异见 **`docs/text-ellipsis-tooltip.md`** §3、§6，本篇不重复；
> 本篇只讲三个表族的**接线口径**与两处 EP 差异。数字全部在 `http://127.0.0.1:5199` 实测（1600×1400）。

## 1. 三族各自的对齐面（先把"EP 有什么"说清楚）

| 族 | EP 2.14.0 的事实 | 本库的接线 | 默认 |
| --- | --- | --- | --- |
| `ElTable` | `show-overflow-tooltip` 是**真契约**：`table-column/render-helper.ts:193-200` 给 `.cell` 加 `el-tooltip` 类 + 内联 `width:(realWidth||width)-1px`；`table.scss:197-199` 靠那个类把 `white-space:normal` 换成 `nowrap`。浮层由 `use-tooltip/index.ts` 的**单个虚拟 tooltip** 承担（挂在滚动容器外） | `ShowOverflowTooltip`（表级）+ `ElTableColumn.ShowOverflowTooltip`（列级覆盖）。开着时照抄 EP 的类与定宽，浮层用本库的 `ElOverflowTooltip`（逐格挂） | **`false`**（同 EP） |
| `ElTableV2` | `components/cell.tsx:14` **只无条件给被裁文本绑原生 `title`**，table-v2 没有 `show-overflow-tooltip`、也没有单元格浮层 | `ElTableV2.EllipsisTooltip` + `TableV2Column.EllipsisTooltip`（列级覆盖表级），纯本库扩展 | **`false`**（EP 没这件事，不能默认开） |
| `*TableGrow`（`ElAccountTableGrow` / `ElDropDownTableGrow`） | — | 列定义里的 `ShowOverflowTooltip` 原样透传给内层 `ElTable`，不新增第三条开关 | 跟随 `ElTable` |
| `ElLink` / 标题族 | EP 的 `link.scss` / 标题组件里没有任何截断浮层机制 | **不动**（没有可对齐的面，加上去就变成"本库行为当成 EP 行为"） | — |

## 2. `ElTable`：契约那一半必须先补齐，否则"被裁"根本不成立

旧状态：`<div class="cell">` 从不带 `el-tooltip` 类、也没有那条 −1px 定宽，而 `index.css` 里
`.el-table .cell{white-space:normal}` 让单元格**换行到完整显示** —— 于是「悬停看全文」在这张表上永远不会触发
（判据量不到截断）。补齐两条：

```csharp
// ElTable.razor.cs:2762-2784
CellOverflowClass(header)  → 开着 ? "cell el-tooltip" : "cell"
CellOverflowStyle(header)  → 开着且能折算出像素宽 ? "width:" + (width - 1) + "px" : null
```

CSS 侧新增 `.el-table .cell.el-tooltip{min-width:50px;white-space:nowrap}`（`index.css:1784-1787`）。

**与 EP 的一处真实差异（记下来，别当 bug 报）**：EP 的 `realWidth` 来自它自己的布局引擎，本仓没有，
用与 `<colgroup>` 同源的列宽代替；**百分比列宽折算不出像素**（`ResolveColumnWidthAsInt` 返回 0）时干脆不写内联宽度，
交给 `table-layout:fixed` 或宿主自己的列宽约束。bUnit `PercentageColumnSkipsInlineWidth` 钉的就是这个分支。

实测（`/grow` 的「科目名称」列，列宽 170）：

```
.cell  class="cell el-tooltip"  style="width:169px"  scrollWidth 260 > clientWidth 169  →  真被裁
white-space: nowrap  ·  触发层已挂（hasTrigger:true）
悬停 → .el-tooltip__popper 270×32，position:fixed，下缘 168 = 文字上缘 176 − 8，水平中心对齐
正对照：同列 29 个短科目名（169/169 没被裁）悬停什么都不浮
```

## 3. `ElTableV2`：裁剪必须让给内层 span

浮层渲染在触发 span 内部，而 `.el-table-v2__cell` 自己就是裁剪盒 —— 浮层会跟文字一起被裁（菜单那次踩过同一层，
见 `docs/menu-alignment.md` §9）。所以开着时必须把单元格的 `overflow` 放开、由内层文字 span 承担裁剪：

```css
/* index.css:4519-4528（本库扩展，不是 EP 的规则） */
.el-table-v2__cell.is-ellipsis-tooltip { overflow: visible; }
.el-table-v2__cell-text { display:block; min-width:0; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }
```

`.el-table-v2__cell-text` 的类名与三条规则来自 EP `table-v2.scss:221-226`（`&:not(.is-dynamic)` 内）；
EP 那一层是 flex item（flex item 被块化，`overflow` 才生效），本仓父盒是块级，所以这里要显式 `display:block`。
EP 那条只在**非 dynamic**（固定行高）时生效，本仓同样不掺动态高度。

实测（`/tablev2` 的第一列，列宽 160）：

```
开：cell overflow:visible + is-ellipsis-tooltip · 内层 span 类名 el-table-v2__cell-text
    span scrollWidth 286 > clientWidth 135（真被裁）· 文字右缘 459 ≤ 单元格右缘 472（没冲出格子）
    单元格宽 160＝列宽、行高 50 ＝没开浮层那一段的 50（虚滚动行高没被内层 span 改掉）
    单元格不再写原生 title（免得两层提示叠着）
悬停：popper 271×32，position:fixed，top 1059/下缘 1091 = 文字上缘 1099 − 8，onTop、fullyInside
列级关：退回 EP 原生 title，且 DOM 逐字节回到改动前 —— hasInnerSpan:false / hasTrigger:false / openClass:false
表级关（另一段）：spans 0 / triggers 0 / 原生 title "1" 照旧给 · 粘性左列 fixedLeft 11 没被牵连
开关≠一律弹：文字放得下的格子（66/66）悬停不弹
```

## 4. 顺带修掉的一处真缺陷：`@for` 计数器被延迟片段吃掉

`ElTable.razor` 把单元格内容交给 `ElOverflowTooltip` 的 `RenderFragment` 之后，那段标记要等父片段跑完才求值，
而 `@for` 的计数器 `i` 那时已经是**最后一行的下标** —— 实测 `Type="index"` 列三行全渲染成 `"4"`。
修法是在循环体内复制一份（`var cellRowIndex = i;`，`ElTable.razor:195-198`），子片段只用这份。
bUnit `IndexColumnStillCountsPerPageUnderTooltip` 钉住分页下的序号。

这类"闭包捕获循环变量"的坑在 Blazor  Razor 里不报错、不告警，只是数字不对。

## 5. 浮层被滚动容器裁掉（三族共同的那一刀）

`ElTable` 的 `.el-table__body-wrapper{overflow:auto}` 与 `ElTableV2` 的滚动层都会把 absolute 浮层整条裁掉，
首行最严重。旧实现实测（row 0 的中心命中拿到 `DIV.cell`，row 1 正常）与 `position:fixed` 的解法、
翻面分支、`PopperGap`/`right:auto;bottom:auto` 的必要性，全部记在 **`docs/text-ellipsis-tooltip.md` §3**，
证据探针 `demo/tools/probes/diag-table-popper-clip.cjs`。

## 6. 测试与探针

- **bUnit**：`ElTableOverflowTooltipTests` 10 例（默认关的 DOM、类名、−1px 定宽、百分比列跳过定宽、
  表级开覆盖全列、悬停浮全文、未裁不弹、mouseleave 收起、序号列、Grow 透传），
  加 `ElOverflowTooltipTests` 4 例＝本批新文件 14 例；改动过的 `ElTextTests` / `ElTableV2Tests` / `ElMenuTests` 再 +17 例。
  全量 **2341 通过 / 0 失败 / 0 跳过**。
- **探针**：`verify-ellipsis-tooltip.cjs` **54/54 PASS × 连跑 8 次读数一致**（四个宿主页 + 每页的正反对照都在里面）。
  它跑在真实演示站上，因此同时是"服务里的构建确实是改后那一版"的证据。
- **诊断件（留在 `demo/tools/probes/`，可复跑）**：`diag-table-popper-clip.cjs`（裁掉的原始形状）、
  `diag-tv2-title.cjs`（body 单元格才带 EP 的 title，表头不带 —— 选错 `.el-table-v2__cell` 会把断言读成 null）、
  `diag-page-scroller.cjs` / `diag-flip-scroll.cjs`（演示站滚动层是 `MAIN.demo-main`，`window.scrollBy` 在它身上无效）、
  `diag-eltext-clip.cjs` / `diag-eltext-trigger-style.cjs` / `diag-ellipsis-popper.cjs`。

## 7. 有界没覆盖的

1. 虚拟滚动的 `ElTableV2` 每帧重建这些单元格实例，行数多的表按需开（XML 注释里写了这条代价）。
2. `ShowOverflowTooltip` 打开的列如果宿主自定义了 `CellClassName`/`CellStyle`，本库那条 −1px 内联宽度会与之竞争
   （EP 同样如此，没有新增仲裁规则）。
3. `*TableGrow` 只测了 `ElAccountTableGrow` 的科目列（演示站唯一一条超长列），`ElDropDownTableGrow` 走同一透传代码，
   未单独在浏览器里量。
