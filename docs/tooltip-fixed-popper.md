# ElTooltip.FixedPopper：浮层钉到视口（批次㊶）

## 问题

宿主 `zyweb` 要把「物料类别」表的操作列从四颗文字按钮改成四颗图标钮，名称交给悬停 tooltip。
第一版直接写 `<ElTooltip Content="新增同级"><ElButton …/></ElTooltip>`，浏览器实测**什么都不出来**。

根因不是事件、不是 z-index，是**裁剪链**：本库的 `ElTooltip` 把浮层渲染在触发 span 的**内部**
（EP 走 teleport 挂到 body，本仓没有那套传送门），于是 `position:absolute` 的浮层跟着祖先一起被裁：

```
button → span.el-tooltip__trigger → div.cell(overflow:hidden)
       → td → .el-table__body-wrapper(overflow:auto) → .el-table(overflow:hidden)
```

单元格只有 24px 高，浮层 32px 一露头就整条被 `.cell` 吃掉。同样的坑此前在侧栏菜单
（`.demo-aside-scroll(overflow-y:auto)`）踩过一次，那次的解法见 `docs/menu-alignment.md` §9 与
`docs/table-overflow-tooltip.md`——但那条路只解决了**文字被裁**的场景：`ElOverflowTooltip` 的浮层
只在 `scrollWidth > clientWidth` 时才弹，图标按钮根本没被裁，判据恒假。

## 修法

`ElTooltip` 新增一个开关，复用 `TextClip` 已经算好的视口坐标：

```csharp
[Parameter] public bool FixedPopper { get; set; }   // 默认 false，absolute，行为与加参数前逐字节一致
```

悬停时走 `TextClip.MeasureRectAsync`（同一个 JS 函数 `elementTextOverflowState`，**只取矩形、不做截断判据**），
再用 `TextClip.BuildFixedPopperStyle(state, Placement, out placement, Offset)` 把浮层写成
`position:fixed;left:…;top:…;transform:…`。fixed 的包含块是视口，只要祖先没有
`transform/filter/contain` 就不受 overflow 裁剪。

三处细节：

1. **锚点必须让位**。`BuildPopperStyle` 在 fixed 分支里不再拼 `position:absolute` 与
   `top:100%/left:50%`——那套锚点是相对触发元素的，fixed 下会算到屏幕外。
2. **翻面要落到 `x-placement`**。上方放不下（< 40px）时坐标翻到下方，箭头也得跟着翻，
   所以 razor 上的 `x-placement` 从 `@Placement` 改读 `EffectivePlacement`（未开 fixed 时两者恒等）。
3. **间距用 `Offset`，不是写死的 8**。`BuildFixedPopperStyle` 因此多了一个可选 `gap` 形参，
   默认仍是 `TextClip.PopperGap`——`ElOverflowTooltip` 的既有口径一字没改。

量不到（bUnit 未打桩、元素在悬停与渲染之间被移除）时静默退回 absolute，不能让一次 hover 崩掉整页渲染。

## 验证

`test/Element.ComponentTests/ElTooltipFixedPopperTests.cs` 新增 6 例（全量 **2642 通过 / 0 失败**，
基线 2636 + 6）：

| 用例 | 钉的是什么 |
|------|------------|
| `FixedPopper_Top_AnchorsToMeasuredViewportRect` | `position:fixed` + `left:224px`（中心）+ `top:492px`（500−8），且**没有** `position:absolute` / `bottom:100%` |
| `FixedPopper_FlipsBelowWhenNoRoomAbove` | 上方只 10px ⇒ `x-placement="bottom"`、`top:50px`、`translate(-50%, 0)` |
| `FixedPopper_UsesOffsetAsGap` | `Offset=20` ⇒ `top:480px`（改前是恒 8，调了不动） |
| `FixedPopper_Right_PlacementAnchorsBesideTrigger` | right 档贴到按钮右侧 |
| `FixedPopperOff_KeepsAbsoluteAnchors` | **正向对照**：不开开关仍是 `position:absolute` + `bottom:100%` + `translate(-50%, -8px)` |
| `FixedPopper_WithoutJsReading_FallsBackToAbsolute` | 量不到时退回 absolute，不抛 |

**翻面验牙**：把 `UpdateFixedPopperAsync` 首行改成无条件 `return`（等价于"参数没生效"），
`Temp/b36-tests-mut.log` ⇒ **失败 4 / 通过 2**，红的正是四条 fixed 用例，两条对照仍绿——
说明断言绑的是新行为。还原 `cp Temp/b36-ElTooltip.razor.cs.good` + `md5sum` 一致
（`1720ca3b34abf4563b7a25a9725ba3e5`），**未用 `git checkout --`**。

## 消费侧（zyweb，浏览器实跑）

`Pages/Materiel/MaterielLayerInfo.razor`：操作列四颗 `Size=Small Plain Round` 的**纯图标**按钮
（`el-icon-document-add` 新增同级 / `el-icon-folder-add` 新增下级 / `el-icon-edit` 编辑 / `el-icon-delete` 删除），
每颗外面一层 `<ElTooltip Placement="top" FixedPopper="true">`，按钮自身带 `aria-label`（图标钮没有文字，读屏要说得出名字）。

- `_tmp_probe/h95.mjs` **21/21 PASS**：逐颗真鼠标 `Input.dispatchMouseEvent` 移入 → 浮层文本分别等于
  「新增同级/新增下级/编辑/删除」、computed `position:fixed`、
  **`document.elementFromPoint(浮层中心)` 命中的就是浮层自身**（这条才是"没被裁"的正证），移开后残留 0 个。
  操作列实际宽 **184px**（改前 477），四颗 36×24 同一行，缝 4px。
- 本环境该表 0 行（`sessionStorage` 无 `unitsurl`，禁手动塞造数据），所以悬停这一轮是在源码里临时
  `dstree.Add(一行假数据)` 让真实组件树把按钮和 tooltip 画出来量的（`TEMP-PROBE-88`），
  量完即摘；摘除后重建再跑 `_tmp_probe/h96.mjs` **9/9 PASS**（列宽、无溢出、无黄条、无异常）。
- **截图证据订正（2026-10-15 复核）**：先前记的 `_tmp_probe/h95.png` 与 `h95-hover.png` **md5 完全相同**
  （`shot95.mjs` 的悬停分支要第三个参数 `hover` 才走，当时没传）⇒ 那两张不构成"悬停看得见"的像素证据，只有上面
  `elementFromPoint` 的数值证据。补测走**临时路由** `/probe-h97`（`Pages/Probe/H97Tooltip.razor`，内存造父/子/孙三行，
  列宽与操作列片段逐字抄自本页，不动交付文件）：`_tmp_probe/shot97.mjs` 先拍未悬停、再发真 `mouseMoved` 拍悬停，
  三张 `h97-plain.png` / `h97-hover.png` / `h97-hover-crop.png` md5 互不相同；悬停那颗（第 2 行「新增下级」）读数
  `position:fixed`、浮层 74×32、`elementFromPoint(中心)` 命中 `el-tooltip__popper el-popper is-dark`、浮层 DOM 仍在 `.cell` 内（`inCell:1`）
  却不被裁、移开残影 0。删临时页 + 重建 + 重启后 `_tmp_probe/h98.mjs` 复验：`/probe-h97` 出「404 页面没找到」（0 表 0 钮 0 浮层）、
  真页仍是交付态（0 行 + 空态、表头 `操作:184 / 编码:225 / 名称:634 / 停用:61`、`scrollWidth 1104 == clientWidth 1104`、无黄条、0 异常，
  读数 `_tmp_probe/h98.json`）。

## 边界与未做

- **祖先带 `transform/filter/contain` 时 fixed 仍会被裁**：这时包含块不再是视口。表格这条链实测没有，
  对话框/抽屉那种带 transform 动画的容器里用要另验。
- 浮层坐标是**悬停那一刻**测的，之后不跟随：行滚动、表格重排、窗口 resize 都不会重算（移开再移回来才会）。
  EP 用 popper.js 的 update 循环，本仓这条按"hover 一次量一次"够用。
- ~~没有给 `ElPopover` 加同样的开关（它有独立的定位代码路径），本次只用 `ElTooltip`。~~
  **批次㊹ 已补**：`ElPopover.FixedPopper`，同一套 `TextClip` 原语，见 `docs/popover-fixed-popper.md`。
