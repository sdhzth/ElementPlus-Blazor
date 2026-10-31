# ElDropDownTableGrow 弹层定位与滚轮：三条用户反馈的三种不同真身

起因是用户对 `http://localhost:5199/table`「① 可编辑表格」的三条实测反馈（附 3 张截图）：

1. 供应商下拉（`ElDropDownTableGrow`）**向上弹的位置不对**；
2. 面板开着时**向上/向下滚两轮，滚轮就不能用了**；
3. **列头单击弹出的菜单有些慢**。

三条查下来是三个互不相干的机制，第 4 条（⑪ 菜单自己的遮罩复刻第 2 条）是修 2 的过程中自查出来的。
本文记录机制、修法、口径和实测数字 —— 所有数字都是从 `Temp/` 下的日志重读出来的，不是回忆。

---

## 1. 向上弹的位置不对：`max-height` 是「上限」，不是「高度」

`place()`（`src/Components/wwwroot/js/dom.js:3406`）按触发器的屏幕坐标给面板写 `top/left/max-height`。
旧写法在两个分支里都用同一个公式算 `top`：

```js
var top = goUp ? (r.top - gap - maxH) : (r.bottom + gap);   // 旧
```

问题在于 `goUp` 时 `maxH` 恰好被赋成「上方全部余量」`above = r.top - gap - margin`，代入得

```
top = r.top - gap - (r.top - gap - margin) ≡ margin
```

也就是**面板被钉到视口顶部**，与触发器之间空出一大段；面板真实高度只按内容算（298px），
远小于 `max-height` 的 469px —— 那 171px 的差额就是空出来的缝隙。

修法：向上展开改锚 `bottom`，`top` 显式设 `auto`。

```js
if (goUp) {
    p.style.top = 'auto';
    var b = vp.vh - r.top + gap;                     // 面板底边贴触发器顶边上方 4px
    if (b > vp.vh - margin) { b = vp.vh - margin; }
    p.style.bottom = Math.round(b) + 'px';
} else {
    p.style.bottom = 'auto';
    var tTop = r.bottom + gap;
    if (tTop < margin) { tTop = margin; }
    p.style.top = Math.round(tTop) + 'px';
}
```

`else` 分支的 `bottom:'auto'` 不是多余的：`position:fixed` 的盒子若 `top`/`bottom` 同时给值，
高度会按两条边拉伸 —— 那就不再是「按内容高」，而是被拉满整段余量。两条边必须一进一退。

> 顺带修掉的隐患：`place()` 开头已有 `var t = entry.trigger`，旧 `else` 分支里又写了 `var t = r.bottom + gap`，
> `var` 同作用域重复声明会把触发器引用覆盖成数字。当前代码此后不再读 `t`，所以没暴雷，但已改名 `tTop`。

**实测**（`Temp/verify_batch6_restored.log`，视口 1440×560 逼出向上弹）：

```
A-up-placement  PASS  style = "…; max-height: 469px; top: auto; bottom: 83px;"  gapUp = 4  insideViewport = true
```

`gapUp` 是「触发器顶边 − 面板底边」，要的就是那个 4px 缝隙。

**变异翻面**（`Temp/verify_batch6_mut1_domjs.log`）：把 `goUp` 分支改回 `top = r.top - gap - maxH`，
同一探针同一视口立刻红，且红出来的数字与当初的截图一致 —— 这条探针确实咬得住这个缺陷：

```
A-up-placement  FAIL  style = "…; max-height: 469px; bottom: auto; top: 8px;"  gapUp = 176
```

`top:8px` 即 `margin`，正是上面推导的「恒等于 margin」；缝隙 176px。

---

## 2. 滚两轮后滚轮失效：`position:fixed` 全屏遮罩把滚动链改道到「视口」

面板开着时渲染的 `<div class="el-dropdown-table-grow__backdrop">`（`position:fixed; inset:0; z-index:1999`）
铺满整个视口。滚轮事件命中它之后，Blink 沿「滚动链（scroll chaining）」往上找可滚动祖先 ——
而 fixed 覆盖层没有可滚动的祖先链，链头是**视口**。演示站的页面滚动条并不在视口上，
真正在滚的是 `.demo-main`（实测该元素 `scrollHeight 14351 / clientHeight 336`），
于是「面板开着 ⇒ 整页滚不动」，收起面板又正常，看起来就像"滚两轮之后坏了"。

修法：**删掉 backdrop**（标记与 CSS 规则都删），「点外面收起」改用组件库里现成的
`window.elementPickerOutsideAttach(root, dropdownSelector, dotnetRef, methodName)`
（`src/Components/wwwroot/js/dom.js:3344`，document **捕获阶段**的 `mousedown`，
命中 `root` 内或 `dropdownSelector` 内的目标不回调）—— 与 `ElDateTimePicker` / `ElTimePicker` 同一套。

`ElDropDownTableGrow.razor.cs` 侧配套的三件事：

- `OnAfterRenderAsync` 里 attach 一次（`_outsideBound` 防重复挂）；
- `[JSInvokable] public Task OnOutsideClickAsync() => CloseAsync();`；
- `Dispose()` 里 `elementPickerOutsideDetach`。

**行为变化（有意接受，与 EP 一致）**：点外面现在会**收起面板 + 同时落到底层元素上**。
以前遮罩会把这一下吃掉。实测 `D4-row-click-selects-and-closes` —— 单击一行：一次事件既选中该行又关掉面板。

**实测**（`Temp/verify_batch6_restored.log`）：

```
D-no-backdrop                              PASS  hasPanel=true,  hasBackdrop=false
B-wheel-outside-panel-scrolls-page         PASS  mainTop = [0, 240, 480]，面板仍开着
C2-clamped-panel-self-wheel                PASS  panelTop = [0, 56]（sh 288 > ch 232，压住时滚自己的内容）
C3-grow-panel-outside-wheel-scrolls-page    PASS  mainTop = [8355, 8595, 8835]
```

`B2-panel-reanchored-after-scroll` 同时验了「页面滚了、面板还贴得住触发器」：滚动后 `gapDown = 4`
（滚上去之后空间变了，`place()` 改向下弹，仍然贴 4px）。

**变异翻面**（`Temp/verify_batch6_mut_backdrop.log`，把 backdrop 的标记与 CSS 原样加回去、重建再看）：

```
D-no-backdrop                        FAIL  hasBackdrop=true
B-wheel-outside-panel-scrolls-page   FAIL  mainTop = [0, 0, 0]        ← 用户说的「滚不动」就是这个形状
C3-grow-panel-outside-wheel-scrolls-page FAIL mainTop = [8355, 8355, 8355]
G-demo-menu-backdrop-wheel           FAIL  mainTop = [5642, 5642, 5642]
G2-click-outside-closes-menu         FAIL  backdropNodeExists=true
SUMMARY  9 PASS / 1 SKIP / 5 FAIL
```

bUnit 侧同一刀（`Temp/grow_tests_batch6_mut_backdrop.log`）：`OpenPanel_UsesOutsideHandlerInsteadOfBackdrop`
失败，其余 25 条仍过 —— 说明红是由这条断言咬出来的，不是整类塌方。
还原 + `touch` + 重建后：探针 14 PASS / 1 SKIP / 0 FAIL，bUnit 全量 2365/2365。

### 2.1 探针点位必须落在 `.demo-main` 里、且不在吸顶 `.demo-header` 上

C3 第一次跑红（`mainTop 8355→8355`）差点被当成「还有第二处遮罩」。逐点位取证
（`Temp/diag_grow_wheel_points2.log`）证明是**探针自己的错**：

```
{"point":"panel-right-plus-60-y28","xy":[731,28],"hit":"demo-header",  "mainTop":[8355,8355],"moved":0}
{"point":"far-right-middle",       "xy":[1300,200],"hit":"demo-page-sub","mainTop":[8355,8595],"moved":240}
```

`.demo-header` 是吸顶条（顶部 55px 一带），**不在 `.demo-main` 里**，在它上面滚滚轮本来就不该动页面 ——
那是正确行为，量出来的红是假红。修法是 `verify-batch6.cjs` 里的 `pickPagePoint()`：
用 `elementFromPoint` 校验候选点「在 `.demo-main` 内 ∧ 不在 `.demo-header` 内 ∧ 不在面板内」再选。

同一段还踩过方向性的坑：页面已经贴底时再往下滚，滚轮自然没有可动的余量。
所以 C3 先读 `mainTop + mainCh >= mainSh - 6` 决定往哪个方向滚（`reason` 字段会写明 `already-at-bottom-so-wheel-up`）。

---

## 3. 列头单击的菜单慢：Blazor 的渲染扇出，不是 JS 往返

⑪ 段（列头点击弹菜单）最初把菜单状态（`menuOpen / menuX / menuY / menuHeader`）挂在
`Pages/TableDemo.razor` 的字段上，点击回调也在页面里。于是每次点列头都要**整页 12 段重渲染**。

`demoGetLastHeaderClick` 那次额外的 JS 往返一度是头号嫌疑，实测**不是**：
JS 侧 0ms、派发 13~40ms（`Temp/diag_header_menu_latency*.log`）。真身是扇出，
用同一把尺子在 `/table` 页面上量四种点击（`demo/tools/probes/diag-page-render-cost.cjs`，
`tMut` ＝ 原生 `click` 的 `event.timeStamp` → 首个 DOM mutation）：

| 点击 | 重渲染范围 | 改前 `tMut` | 改后 `tMut` |
|---|---|---|---|
| ⑪ 点列头弹菜单 | 整页 12 段 → **只子组件** | **1155 / 1271** | **107 / 82** |
| ⑫ 开筛选面板 | 单组件（ElTable） | 176 / 65 | 214 / 68 |
| ⑫ 隐藏表头 ×2 | 整页 | 473 / 557 | 519 / 472 |
| 展开/折叠第 1 行 | 整页 | 501 | 532 |

（同一行里两个数是一开一关 / 一点两点的两次独立点击；「⑪ 开菜单」的 `tHit`（菜单真的可见）
改前 1165 / 1280、改后 110 / 84，与 `tMut` 只差十几毫秒，说明瓶颈在渲染而不是绘制。
`⑫ 开筛选面板` 那一行加的是 191 个节点、关的是移除 8 个，工作量本就不对称，两个数不能互相换算。）

三个数字放在一起才说明事：**页面级 `StateHasChanged` 本身就值 470~560ms**（第 3、4 行没被本次改动触及，
改前后都在这个量级里 —— 组件级那行同理，176/65 → 214/68 —— 它们一起充当「环境没有整体变快」的对照，
只有 ⑪ 那一行掉了一个数量级），组件级只要 180~220ms，
而 ⑪ 在页面级之上还多叠了一次往返与菜单定位，改前是 1.2 秒。

修法：把 ⑪ **整段搬进独立组件** `demo/Element.Blazor.Demo/Components/HeaderMenuDemo.razor`，
`TableDemo.razor` 只留 `<HeaderMenuDemo />`。菜单的展开态、坐标、被删列都只存在子组件里，
点列头只让子组件重渲染。顺带把菜单的「点外面收起」也从全屏遮罩换成同一套 `elementPickerOutsideAttach`
（即第 4 条自查出来的缺陷：`.demo-colmenu-backdrop` 复刻了 backdrop 的滚轮改道，
实测 `mainTop 5642 → 5642`，删掉后 5642→5882→5402）。

**ElementReference 必须稳定**：菜单节点改成**常驻渲染、收起时只加 `.demo-colmenu--hidden`（`display:none`）**，
不再用 `@if` 摘掉节点 —— 否则 attach 时抓到的 `ElementReference` 在下次渲染后失效，
`elementPickerOutsideDetach` 解绑不到正确的元素。这也是 EP/Vue 那边 `v-show` 的等价写法。

副作用是「开没开」不能再用 `querySelector` 判断（节点常驻 ⇒ 恒真），
探针里的 `window.__menuVisible()` 改看 `rect.height > 0 && display !== 'none'`。

**验收**（`Temp/verify_batch6_restored.log`）：

```
F-header-menu-open-latency  PASS  latencyMs = 79 / 107 / 143 / 153   （四次独立运行，阈值 400）
G-demo-menu-backdrop-wheel  PASS  mainTop = [5642, 5882, 5402]，菜单还开着
G2-click-outside-closes-menu PASS menuVisibleAfterClick=false, backdropNodeExists=false
G3-menu-item-works           PASS 列数 7 → 6，状态栏「已删除列「上架」」
```

> 关于 F 的"有牙"：F 与表中「改前 1155/1271 → 改后 82/107」是**同一段代码、同一把尺子**的前后对照，
> 这一对照本身就是翻面证据；没有再把页面级状态原样搬回去重跑一次（那要改动 ⑪ 整段与 `TableDemo.razor`，
> 而收益已被前面的对照覆盖）。

---

## 4. 口径与复现

```bash
# 依赖 dev server（改 .razor 要先杀 5199 再重建；只改 wwwroot 下的 js/css 是即时生效的）
cd demo/Element.Blazor.Demo && dotnet build -c Debug
ASPNETCORE_URLS=http://127.0.0.1:5199 dotnet run --no-build --no-launch-profile

NODE_PATH=<node_modules> node demo/tools/probes/verify-batch6.cjs http://127.0.0.1:5199 > Temp/verify_batch6.log 2>&1
dotnet test test/Element.ComponentTests/Element.ComponentTests.csproj --filter "FullyQualifiedName~ElDropDownTableGrowTests"
```

- 视口 **1440×560** 用来逼出向上弹，**1440×900** 验向下弹（`E-down-placement`，`gapDown = 4`、`bottom:auto`）；
- 每组用例前重新 `goto`：上一组留下的滚动位置/筛选态会让下一组的「行序」「能不能滚」失去前提
  （`openGrow()` 先 `scrollIntoView` 就是这个原因 —— 单元格停在吸顶表头底下时，点击打在表头上，面板自然打不开）；
- 面板内部自滚那条（C）在内容不足时会**如实记 SKIP** 而不是硬判过：`sh > ch` 才是「压住且需要自滚」的前提，
  `/grow`（矮视口 + 多数据）才是真验得住的位置（C2）。

日志与产物：`Temp/verify_batch6{e,restored,final}.log`（三份都是 14 PASS / 1 SKIP / 0 FAIL，
`final` 是文档数字定稿后、重建演示程序集再跑的那一次）、
`Temp/verify_batch6_mut1_domjs.log`（第 1 节变异红）、
`Temp/verify_batch6_mut_backdrop.log` + `Temp/grow_tests_batch6_mut_backdrop.log`（第 2 节变异红）、
`Temp/diag_grow_wheel_points2.log`（假红取证）、
`Temp/diag_page_render_cost{,_after}.log`（第 3 节对照表）、
`Temp/build_batch6_{mut,restored,docs}.log`（重建，0 错误；`restored` 那次带出 1 条**既有**警告
`ElTree.Features.cs:73 CS1587`，该文件本批未改动，`git blame` 指向基线提交）、
`Temp/full_suite_batch6{,_final}.log`（**2365/2365**）。

跨批回归：上一批的验收探针 `verify-batch5.cjs` 复跑 **14/14 全绿**（`Temp/verify_batch5_recheck.log`）——
本批动到的 `place()` 与 ⑪ 段都在它的覆盖面里，而 `.el-table-filter__backdrop`（表头筛选的遮罩）**仍在**：
那一段是「一滚就收起」（面板开着滚滚轮会主动关掉它，`P:A5`），与本批删掉的 grow 遮罩不是同一套契约，不属于回归。

验收后另加一份小探针 `demo/tools/probes/verify-batch6-docs.cjs`：文档里的数字要能在**服务中的构建**上站住，
所以直接读 `/table` ⑪ 段渲染出来的正文，确认新数字（`1165 / 1280`、`110 / 84`）在、旧数字（`82 / 107`、`182 / 223`）不在，
顺带确认 `.demo-colmenu-backdrop` 节点确实不再渲染 —— 这是「改完 .razor 忘了重建就验收」那条老坑的正面防呆。

bUnit 侧新加的 4 条：`OpenPanel_UsesOutsideHandlerInsteadOfBackdrop`（负向断言配正向对照 ——
先确认面板真的渲染出来了，再断言没有 backdrop，否则「选择器写错导致永远查不到」也会假绿）、
`OpenPanel_AttachesOutsideHandlerWithSelectorAndMethod`、`ClosePanel_DetachesOutsideHandler`、
`OnOutsideClickAsync_ClosesPanel`。
