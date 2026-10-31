# ElPopover.FixedPopper：气泡卡片钉到视口（批次㊹）

## 问题

宿主 `zyweb` 的人员编辑对话框里，「用户权限等级」旁有一颗 `?` 按钮，点开一块 320px 宽的
「操作说明」气泡卡片。用户反馈：**点击这个弹出的内容被遮挡了**（截图里卡片右半部文字被拦腰裁掉）。

先量再改。`_tmp_probe/b41_diag.mjs` 用真鼠标点开对话框、点开 `?`，然后从面板往上走整条祖先链
（`b41_diag.log`）：

```
span.el-popover__reference-wrapper  overflow:visible
div.el-col-2                        overflow:visible
div.el-row                          overflow:visible
div.el-tabs__content                overflow:hidden   ✂  rect 164,253 .. 1276,307
div.el-tabs                         overflow:visible
form                                overflow:visible
div.el-dialog__body                 overflow:auto     ✂  rect 144,189 .. 1296,765
div.el-dialog                       overflow:visible   transform:none
div.el-overlay-dialog               overflow:auto      ✂  rect 0,0 .. 1440,900
body                                overflow:hidden   ✂
```

面板自己的读数：`rect [936,108,1282,260]`、`position:absolute`、`x-placement="top"`。

- 裁剪层 `.el-tabs__content` 的**上缘是 253**，面板占 108..260 ⇒ 152px 高只剩 **7px**；
- `document.elementFromPoint(面板中心)` 与 `(面板右缘)` 命中的都是 `div.el-dialog__header`，
  不是面板本身 —— 这才是"被遮挡"的硬判据；
- 整条链上没有任何 `transform`（每一层的 `tf` 都是 `null`），所以 `position:fixed` 的包含块
  仍然是视口，这条路可行。

根因与批次㊶ 的 `ElTooltip` 完全同一条：本库的气泡面板**内联**渲染在
`.el-popover__reference-wrapper` 里（EP 走 teleport 挂到 body，本仓没有那套传送门），
而 `PopoverStyleValue` 写死 `position:absolute`，于是 absolute 的裁剪规则接管——
「最近的非 visible overflow 祖先」是谁，面板就被谁切。页签容器（`.el-tabs__content`
是 `overflow:hidden`）、对话框滚动正文（`.el-dialog__body` 是 `auto`）、表格 `.cell` 都是这类盒子。

**消费侧没有参数可用**：`Placement="bottom"` 一样被同一层裁（面板落到 307 以下，仍在框外）；
`ZIndex` / `PopperStyle` 救不了裁剪；`Offset` 只改间距。所以这是库缺口。

## 修法

给 `ElPopover` 补上批次㊶ 已经给过 `ElTooltip` 的那个开关，复用同一套原语：

```csharp
[Parameter] public bool FixedPopper { get; set; }   // 默认 false，absolute，行为与加参数前逐字节一致
```

- `ElPopover.razor`：`.el-popover__reference` 加 `@ref="referenceRef"`（量的是**参考元素**的矩形，
  与 `ElTooltip` 量 `triggerRef` 同口径）；面板的 `x-placement` 从 `@Placement` 改读
  `EffectivePlacement`。
- `ElPopover.razor.cs`：`SetVisibleAsync` 在真正改 `internalVisible` / 回调 `ModelValueChanged`
  **之前** `await UpdateFixedPopperAsync(visible)`；该方法用 `TextClip.MeasureRectAsync` 量矩形，
  再交给 `TextClip.BuildFixedPopperStyle(state, Placement, out fixedPlacement, Offset)` 算坐标。
- `PopoverStyleValue` 拆成 `BuildPopoverStyle()`：`z-index` 与 `width:{Width}px` 挪进公共分支，
  fixed 分支只拼 `fixedPopperStyle`（自带 `position:fixed`），**不再拼** `position:absolute`
  与 `top:100%/bottom:100%/left:50%` 那套相对锚点 —— 那套锚点在 fixed 下会把面板算到屏幕外。
- 量不到（bUnit 未打桩、元素在点击与渲染之间被移除、JS 互操作抛）时静默退回 absolute，
  不能让一次点击崩掉整页渲染。

三处与批次㊶ 保持同一口径，不另起一套：翻面要落到 `x-placement`（箭头跟着翻）、
间距用 `Offset` 而不是写死的 8、`TextClip` 的常量（`PopperGap=8`、`PopperMinRoomAbove=40`）不复制粘贴。

## 验证

### bUnit（`test/Element.ComponentTests/ElPopoverFixedPopperTests.cs`，新增 7 例）

全量 **2664 通过 / 0 失败**（`_tmp_b41_full.log`）。喂的矩形是探针实抓的那颗 `?` 钮：`273,1096` 起 40×32。

| 用例 | 钉的是什么 |
|------|------------|
| `FixedPopper_Top_AnchorsToMeasuredViewportRect` | `position:fixed` + `left:1116px`（中心）+ `top:265px`（273−8）+ `translate(-50%, -100%)`，且**没有** `position:absolute` / `bottom:100%` |
| `FixedPopper_FlipsBelowWhenNoRoomAbove` | 上方只 10px ⇒ `x-placement="bottom"`、`top:50px`、`translate(-50%, 0)` |
| `FixedPopper_UsesOffsetAsGap` | `Offset=20` ⇒ `top:253px`（改前恒 8，调了不动） |
| `FixedPopper_KeepsWidthAndZIndex` | fixed 分支没把 `width:320px` 与 `z-index:var(--el-index-popper, 2000)` 弄丢 |
| `FixedPopper_ClearsStyleOnHide` | 关一次再开要**重量**：换矩形后是 `top:592px`，不残留上次的 `top:265px` |
| `FixedPopperOff_KeepsAbsoluteAnchors` | **正向对照**：不开开关仍是 `position:absolute` + `bottom:100%` + `left:50%` + `translate(-50%, -8px)` |
| `FixedPopper_WithoutJsReading_FallsBackToAbsolute` | 量不到时退回 absolute，不抛 |

**翻面验牙**：把 `UpdateFixedPopperAsync` 首行改成无条件 `return`（等价于"这个参数没生效"）
⇒ **失败 4 / 通过 3**，红的正是四条 fixed 坐标用例，两条 absolute 对照与 z-index 守卫仍绿
—— 说明断言绑的是新行为而不是恒真式。还原用 `cp Temp/b41-ElPopover.razor.cs.good2`，
`md5sum -c` = OK（`42ad930a0bd603f2e50e7bc249dbb6e9`），**未用 `git checkout --`**。

### 浏览器实跑（消费侧，`_tmp_probe/b41_after.mjs` **26/26**）

真页的对话框要后端 `validReg()` 通过才打得开，本环境没有后端（禁手动塞 `unitsurl` 造数据），
所以用临时探针页 `Pages/ZzProbePopHost.razor` + `ZzProbePopContent.razor`：
同一组生产 options（`Width="80%" Height="70%"`）打开，正文是 `EmployeeInfoEdit.razor:21-202`
「基本信息」页签四行的逐字照抄，**同一容器里放两颗 `?`**：

- **A 档**＝与真页改后逐字一致（`FixedPopper="true"`）：`position:fixed`、`346×152` 完整、
  中心/上缘/右缘/右上角四个 `elementFromPoint` **全部命中面板自身**、
  文字 111 字读到结尾「等级来约束。」、面板上缘 220 已在裁剪层上缘 253 之上（逸出）。
- **B 档**（正向对照）＝同一个面板、同一个 `.el-tabs__content`、同一组栅格，唯一差别是不开开关：
  仍是 `position:absolute`，`clipRight=1276` 对上 `面板右缘=1282` ⇒ 可见宽 **340/346**，
  右缘与右上角两个探针点命中的是 `div.el-dialog__body`。
  窄一档到 1100 视口，缺口放大到 **52px**（`b41_B_narrow.png` 复现的就是用户那张症状图），
  而 A 档在 1100 视口仍是完整 `346×152`、四点全命中。
- 开关不泄漏：A 再点一次能关（残留 0 个面板）；控制台 0 异常。

### 默认档回归（`_tmp_probe/b41_regress.mjs` **11/11**）

顶栏消息铃铛（`Trigger=Click`、`Placement="bottom-end"`、未开开关）：`position:absolute`、
style 里没有 `position:fixed`、`x-placement` 原样透传 `bottom-end`、中心命中面板、再点能关；
员工维护页 / 物料供应商页照常出表格、无黄条；当前页 0 个 fixed 面板。

> 中途两条红是**量法错**不是产品坏，记录以免重犯：① 第一版对 fixed 面板也拿祖先盒求交
> （fixed 的包含块是视口，祖先 overflow 不参与裁剪）⇒ 可见区域应按 `position` 分档；
> ② 求交时漏并 `clip`，于是 B 档永远"没被裁"、对照失去牙。另外 B 档最初放在靠左的列里
> （`?` 中心 x≈740），那个几何根本越不过裁剪层 —— 正向对照必须和被测档**同一组栅格**。

## 边界与未做

- **水平方向不做视口夹取**：面板中心钉在参考元素中心，`BuildFixedPopperStyle` 只在"上方放不下"时
  纵向翻面，不会横向平移。实测 1440/1100 两档下 A 档右缘分别是 1282 / 1022，都 ≤ 视口宽 ⇒ 本例够用。
  窗口再窄、或参考元素更贴右缘时，面板可能压到视口外（EP 的 popper.js 有 `preventOverflow` 会平移，
  本仓这条没有）。要做需要面板自身宽度 + `innerWidth`，届时一并补 `ElTooltip`。
- **祖先带 `transform/filter/contain` 时 fixed 仍会被裁**（包含块不再是视口）。对话框这条链实抓
  `transform:none`，但那是**动画结束后**的量法；如果宿主给 `.el-dialog` 加了淡入缩放动画，
  打开瞬间落在动画里的面板坐标要另验。
- 坐标是**打开那一刻**测的，之后不跟随：对话框正文滚动、窗口 resize、行重排都不会重算
  （关掉再开才会）。EP 用 popper.js 的 update 循环，本仓按"开一次量一次"够用。
- `Placement` 的 `-start` / `-end` 后缀在本库仍被忽略（`GetPlacementStyle` 与
  `BuildFixedPopperStyle` 都只认前缀），`bottom-end` 实际是 `bottom` 居中 —— 本批次没动这个既有口径。
