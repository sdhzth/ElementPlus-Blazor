# 截断文字悬停看全文：`ElText` 与共用的 `ElOverflowTooltip`（2026-10-08）

> 触发点：用户报「`http://localhost:5199/basic`，鼠标移动到有 Ellipsis 的时，能不能用 elTooltip 完整显示一下内容，
> 包括 eltable、eltablev2 和 eltablegrow」。表格侧（`ElTable` / `ElTableV2` / `*TableGrow`）见
> **`docs/table-overflow-tooltip.md`**，本篇只讲共用件与 `ElText`。
> 本篇每个数字都是这一批在 `http://127.0.0.1:5199` 上实测量出来的（Edge headless + Playwright，1600×1400）。

## 1. 口径：EP 2.14.0 只有原生 `title`，浮层是本库扩展

源码直取（HTTP 200，tag 无 `v` 前缀）：`packages/components/text/src/text.vue:42-71` 用
`useResizeObserver` 量 `scrollWidth > clientWidth`（开了 `line-clamp` 就改量 `scrollHeight > clientHeight`），
真的被裁才 `target.setAttribute('title', ...)` —— **EP 的 `ElText` 没有 tooltip**。

所以本库分两条路，互斥、不会叠成两层提示：

| 属性 | 默认 | 行为 |
| --- | --- | --- |
| `EllipsisTooltip` | **`true`** | 悬停实测被裁 → 浮 `elTooltip`（本库扩展） |
| `EllipsisTooltip="false"` | — | 退回 EP 口径：首帧后量一次，被裁才写原生 `title`（`window.elementSetEllipsisTitle`） |

两者都只在「真的开了截断」时才可能生效：`ShowEllipsisTooltip = EllipsisTooltip && (Truncated || LineClamp>0)`。
没开截断的 `<ElText>` 输出与本属性引入前**逐字节一致**（不加任何包装层）。

## 2. 共用件 `ElOverflowTooltip`

`src/Components/ElOverflowTooltip.razor(.cs)` —— 不是 EP 组件，是 `ElText` / `ElTable` / `ElTableV2` / `ElMenuItem` /
`ElSubMenu` 共用的宿主。三个参数形状值得记住，都是被实测逼出来的：

- **`ClipStyle` 而不是 `Style`**：基类 `ElementComponentBase` 已有 `Style` 参数，同名会在
  「参数名大小写不敏感且必须唯一」上撞车（运行时 `InvalidOperationException`，bUnit 里表现为整棵渲染树失败）。
- **`Trigger="Manual"` + `ModelValue`**：可见性只有父组件一个来源，`ElTooltip` 自己的 hover 处理器变 no-op。
  鼠标停在行内空白处也看得见浮层。
- **`ContentOnlyWhenDisabled`**：`ElTableV2` 用 —— 关掉时连被裁的那个盒子都不输出（那层由宿主自己写），
  这样「关着＝零 DOM 变化」这条契约才守得住。

判据在 `src/Components/TextClip.cs`（上一批叫 `MenuEllipsisTooltip`，本批改名，因为四个宿主都用它）：
`TextClip.MeasureClippedStateAsync` 一次 JS 往返同时拿「有没有被裁」与「被裁元素的视口矩形」，
`MeasureClippedTextAsync` 是只要文字的薄封装。JS 侧 `window.elementTextOverflowState`（`wwwroot/js/dom.js:774`）
本批补了 `scrollHeight/clientHeight` 与 `rectTop/rectLeft/rectWidth/rectHeight`。

## 3. 浮层必须钉成 `position:fixed`（本批的核心缺陷）

`ElTooltip` 默认把浮层渲染在**触发元素的内部**、样式是 `position:absolute`。表格里的祖先链是
`span.el-tooltip__trigger(relative) → td(relative) → … → .el-table__body-wrapper(relative, overflow:auto) → .el-table(overflow:hidden)`：
absolute 的包含块在这条链里、裁剪也来自这条链，于是**首行单元格朝上的浮层被滚动容器上缘整条裁掉**。

变异回旧实现（只把 `position:fixed` 换成 `absolute`）实测复现，一行好一行坏：

```
row 0  popper {x:536 y:1127 w:271 h:32}  bodyWrapper.top 1156  → hitNormal "DIV.cell"           ← 中心命中的是单元格，浮层没了
row 1  popper {y:1167 ...}                （在同一盒里）        → hitNormal "DIV.el-tooltip__popper el-popper is-dark"  ← 放得下就没事
```

`position:fixed` 的包含块是视口，只要祖先没有 `transform/filter/contain` 就不受 overflow 裁剪
（实测 `.el-table-v2__row` 与 `td` 都没有这些属性，它们的样式只来自 `RowStyle`）。
EP 不遇到这个问题，因为它给整张表只渲染**一个挂在滚动容器外的虚拟 tooltip**（`table/src/composables/use-tooltip/index.ts`），
本仓按单元格各挂各的，就只能把坐标钉到视口。坐标由 `BuildFixedPopperStyle` 算，`PopperStyle` 是 `ElTooltip` 最后拼上去的，
所以必须显式写 `right:auto;bottom:auto` 让 placement 规则里的锚点失效（否则 `top` 与 `bottom:100%` 同时生效会把浮层顶出去）。

- **间距 `PopperGap = 8`**：与 `ElTooltip.Offset` 默认值同口径，坐标由本件算，两边必须是同一个数。
- **上方放不下就翻面**：`PopperMinRoomAbove = 40`（`.el-tooltip__popper` 是 12px 字号 × 1.2 行高 + 上下 8px padding ≈ 31px，加边框与余量）。
  翻面时 `placement` 一起交给 `ElTooltip`，`x-placement` 上的箭头与外边距跟着翻，不会浮在下边却带个朝上的箭头。
- **验证**：四个宿主都量到 `position:"fixed"`、`fullyInside:true`、`onTop:true`，且浮层中心与被裁文字中心差 ≤1px、
  下缘正好在文字上缘 −8px（探针 `verify-ellipsis-tooltip.cjs` 的锚点段）。

### 3.5 `fixed` 只挡裁剪，不挡层叠（同日第九批补，别把两件事混成一件）

修完裁剪后用户又报「eltooltip 没显示全」，这次矩形与坐标全对、命中测试却落在正文元素上：
**宿主把浮层放进了 `position:sticky` 的侧栏**，sticky 无条件创建层叠上下文，浮层那个 `z-index:2000`
出不了侧栏那一层，而正文里 `position:relative` 的 `.el-menu` 与侧栏同处「定位但 auto」这一步、DOM 更靠后 ⇒ 盖在上面。
库侧没有 teleport（`ElTooltip` 无 `Teleport` / `appendTo` / `PopupService`），EP 靠 `teleported` 挂 body 才天然避开，
所以这条只能在宿主修：`demo` 的 `.demo-aside` 加 `z-index: 25`。取证（逐层 dump `transform/filter/contain` 全 `none`、
只改 aside 的 `z-index` 就翻面 `flipped:true`）与完整机制见 **`docs/menu-alignment.md` §10**；
判据是 `verify-batch5.cjs` 的 F 段（`/menu`、`/tabs` 各一条，中心与右缘两点都命中浮层）。

## 4. `ElText` 的触发层必须带 `max-width:100%`（浏览器 computed 等价变异）

EP 的 `.el-text.is-truncated` 靠自己 `max-width:100%` 被宿主宽度钳住。插入一层 auto 宽的
`inline-block` 触发 span 之后，那层的 shrink-to-fit 下限＝不换行文字的 min-content，整条钳制链失效。
同一棵 DOM、只改那层的 `style.cssText`（`diag-eltext-trigger-style.cjs`）：

| 触发层样式 | trigger 宽 | `.el-text` 宽 | `scrollWidth/clientWidth` | 被裁？ |
| --- | --- | --- | --- | --- |
| `min-width:0;max-width:100%`（现状） | **180** | 180 | 378/180 | ✅ 是 |
| 退回 EP 默认（`position:relative;display:inline-block`，无这两条） | **378** | 378 | 378/378 | ❌ 否，冲出 180 的宿主 |

`min-width:0` 与 `max-width:100%` 各挡一种塌法（flex 容器里的自动最小尺寸、块级宿主里的 shrink-to-fit），
`ElText.razor` 因此写死 `TriggerStyle="min-width:0;max-width:100%"`，注释里留住这两个数。
这条正是 `docs/testing-guards.md` §4 第 11 条说的「bUnit 看不见布局」——17 条 `ElText` 用例全绿时这个缺陷可以完整存在。

## 5. DOM 形状（写断言前先认清多出来的是哪几层）

| 宿主 | 开着 | 关着（默认） |
| --- | --- | --- |
| `ElText` | `span.el-tooltip__trigger` **包住** `.el-text`（浮层不能放进 `.el-text`，那是裁剪盒） | 单个 `.el-text`，与改动前一致 |
| `ElTable` | `<td>` 里 `<div class="cell">` → `ElOverflowTooltip`（`TriggerStyle="display:block"`，类名仍 `cell` / `cell el-tooltip`） | `<div class="cell">` 原样 |
| `ElTableV2` | `.el-table-v2__cell.is-ellipsis-tooltip`（`overflow:visible`）里两层 span：触发 span + `.el-table-v2__cell-text` | 逐字节回到改动前：没有内层 span、没有触发层、写原生 `title` |

## 6. 测试、探针与变异

- **bUnit**：`ElOverflowTooltipTests` 4 例钉坐标算术（`position:fixed` 必须晚于 `position:absolute`、
  `left:190px`/`top:292px`/`translate(-50%, -100%)`、翻面后 `top:52px`+`x-placement="bottom"`、
  `right` 分支 `left:288px`/`translate(0, -50%)`、未裁什么都不挂）。全量 **2341 通过 / 0 失败 / 0 跳过**
  （同日第九批筛选面板那批 +20 → **2361 / 0 / 0**，见 `docs/table-filter-alignment.md` §8）。
- **探针**：`verify-ellipsis-tooltip.cjs` **54/54 PASS**，改完后**连跑 8 次读数一致**（含 `/basic`、`/table`、`/tablev2`、`/grow` 四页）。
  菜单那条路径同期 `verify-menu-ellipsis.cjs` **26/26 PASS**，没被这批牵连。
- **变异（改 → 跑 → 还原 → touch → 再跑）**：

  | 变异 | 谁抓到 | 红了几条 |
  | --- | --- | --- |
  | M1 `position:fixed` → `absolute` | 探针 | 9 条（锚点、fixed、`fullyInside`、四个宿主的裁掉检查） |
  | M2 永不翻面 | bUnit | 1（翻面例） |
  | M3 间距丢（不写 `PopperGap`） | bUnit | 1（`top:292px`） |
  | M4 不居中（去掉 `translate(-50%,…)`） | bUnit | 1 |
  | M5 不写 `right/bottom:auto` | bUnit | 1 |
  | M6 `right` 分支坐标写错 | bUnit | 1 |

  还原后 `same bytes as backup: True`，四个宿主重新全绿。

## 7. 有界没覆盖的（别当成已解决）

1. **不做视口左右钳位**：坐标按被裁元素居中，浮层宽可达 `min-width:max-content`。四个宿主的演示实测
   `fullyInside:true`，但极靠右的窄列理论上会溢出右缘 —— 没实现横向翻转。
2. **`top-start` / `top-end` 当成居中处理**：只按 family 前缀判方位。
3. **坐标在悬停那一刻冻结**：悬停期间滚动会留下错位浮层（`mouseleave` 才收）。滚轮滚动时同时悬停是罕见形状。
4. **`PopperMinRoomAbove = 40` 是单行浮层的估算**：多行内容仍可能超出，这时浮层压到上方行上而不是消失
   —— 阈值只管"整条跑到视口外"那种彻底看不见的情况。
5. **`BindTitle` 只量首帧之后一次**：EP 是每次更新重算（`text.vue:42-67`）。异步改文字后不重测 title。

## 8. 顺带定位、**没有**在本批修的缺陷

`ElTooltip.razor.cs:245-256` 与 `ElPopover.razor.cs:234-245` 拼的是
`"…transform:translate(-50%, -{Offset}px)"` —— **少了 `$`**，插值没发生，写出的是字面量 `-{Offset}px`，
浏览器把整条 `transform` 声明丢掉。后果：走默认 absolute 路径的浮层不居中、`Offset` 被忽略。
本批的浮层自己算坐标并写 `transform`，所以四个宿主不受它影响；`ElPopconfirm.razor.cs:256-261` 是对的（同一族第三种写法）。
修它要同时回归 `ElTooltip` / `ElPopover` 的所有宿主，属另一批，这里只记账。
