# `Visible` 铺开：ElRow / ElSpace / ElProgress / ElTabPane（批次103）

宿主电子档案应用的一次改版把三个模式块（卡片 / 表格 / 编辑）写成 `<ElRow Visible="@(showstyle==N)">`，
结果整页三块一起画；同一批里 `ElTabPane` 也按同样写法收起「归还」页签，同样没生效。宿主原话：

> 组件不完善的话，就修改完善组件 E:\电子病历\Element-Blazor-master

## 一、真身：`Visible` 之前只在个别组件上是真参数

`ElementComponentBase` 带

```csharp
[Parameter(CaptureUnmatchedValues = true)]
public IDictionary<string, object> Attributes { get; set; }
```

未声明的属性不会报错，而是被当普通 HTML 属性吐到 DOM 上。`Visible` 在 0.10.23 及以前**只**由
`ElButton` / `ElDialog` / `ElDialogGrow` / `ElDrawer` / `ElDropDownTableGrow` / `ElWaitingPopupGrow`
声明（`ElBacktop` 那个是只读派生值）。所以：

| 需求 | 0.10.23 | 0.10.24 |
| --- | --- | --- |
| 单颗按钮拿掉 | **有** `ElButton.Visible` | 不变 |
| 整行（含 `ElCol` 子树）拿掉 | 无 —— 属性被吞，块常驻 | `ElRow.Visible` |
| 间距容器拿掉 | 无 —— 同上 | `ElSpace.Visible` |
| 进度条拿掉 | 无 —— 同上 | `ElProgress.Visible` |
| 单个页签拿掉 | 无 —— 同上 | `ElTabPane.Visible` |

`Visible="@(…)"` 写下去不报错、也不生效，这类 bug 编译期零信号，只有实抓 DOM 才看得见。

## 二、口径：为什么是 `@if (Visible)`，不是 `display:none`

沿用 `ElButton` 的既有语义 —— **此刻不存在**：标记整块包进 `@if (Visible)`，收起时连壳带子树都不进
渲染树，占位、外边距、`Gutter` 造成的缝一起消失。不用 `display:none` 有三个理由：

1. `display:none` 的容器仍会参与 JS 测量。`ElTabPane` 的表头测量（`AcitveTabOnRenderCompletedAsync`
   量 `clientWidth`/`offsetLeft`）在隐藏容器里恒得 0，重试上限 `MaxMeasureRetries = 3` 就是宿主
   DocManage 整页卡死那一次留下的刹车；隐藏如果靠 CSS，等于把这条死循环的燃料留着。
2. 收起一块版式的意图通常就是"这一帧别占地方"，CSS 隐藏要宿主再补一条规则才不留缝。
3. 与 `Disabled` 分家：禁用是"还在条上、点不动"，收起是"没有这个页签"。二者在
   `HiddenPaneIsNotTheSameAsDisabledPane` 里被当成两条独立判据钉住。

默认值一律 `= true`，所以加参数之前的用法一字不变。

## 三、`ElTabPane` 比另外三个多做的那半件事

`ElRow` / `ElSpace` / `ElProgress` 只需要包住自己的 markup。页签不是这样：内容区画的是
`ElTabs` 里的 `ActiveTab?.ChildContent`（`src/Components/ElTabs.razor:22`），页签自己藏掉不会带着
内容区一起变。所以 `Visible=false` 必须一路管到容器：

| 时机 | 处理 | 代码位置 |
| --- | --- | --- |
| 首次初始化就收起 | 不进 `tabPanels` 集合 ⇒ 容器"挑第一个 / 按 ModelValue 定位"都不会选中它 | `ElTabPane.OnInitializedAsync` |
| 运行中翻面 | `OnParametersSet` 调 `SyncRegistration()`：可见走 `TabContainer.ShowTab(this)`，收起走 `HideTab(this)` | `ElTabPane.razor.cs` |
| 收起的正是当前激活页签 | `ElTabs.ReactivateAfterRemoval` 顺延到下一个可用页签、同步 `ModelValue`、`Refresh()` 它，并让容器重渲染 | `ElTabs.razor.cs` |
| 页签翻回可见 | `ShowTab` 重新入集合，但**不**抢激活位 | 同上 |
| 收起期间 | 跳过表头测量，也不对外报"渲染完成"（这一帧它根本没渲染） | `AcitveTabOnRenderCompletedAsync` |

两个坑都是实跑出来的：

- **脏 `IsActive`**：从集合里摘掉的页签不会被"给集合里每项置激活态"那段碰到，`IsActive` 留着 true
  的话，页签翻回可见、重新注册时会被 `OnAfterRenderAsync` 误选成当前页签 ⇒ 摘除时统一 `DeActivate()`。
- **收起与 `closed` 不是一件事**：`closed` 是用户点 X 关掉的硬编码页签，名字被 `closedTabNames` 记住、
  不再回来；`HideTab` 只是本轮不在场，`ShowTab` 对 `IsTabClosed` 的名字直接拒绝复活。

## 四、验证

- 单测：新增 `test/Element.ComponentTests/ElVisibleParameterTests.cs`，12 条全绿
  （失败 0 / 通过 12 / 总计 12）。头一条是反射守卫 —— 四个组件 + `ElButton` 的 `Visible` 必须是
  `bool`、带 `[Parameter]`、默认 `true`，防止以后有人把参数删掉却留下吞属性的用法。其余成对出现：
  默认可见（正向对照）+ 收起不留壳不留子树，页签那组另加"收起激活页签会顺延""翻回可见不抢激活"
  "只剩一个可见时它就是激活项""收起 ≠ 禁用"。
- 全量：`Element.ComponentTests` 失败 2 / 通过 2775 / 总计 2777。那 2 条（
  `ElDropDownTableGrowTests.Guard_PanelScrollbarGutterIsOneSharedEqualWidthRule`、
  `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows`）在本批 9 个文件回退、新测试移除
  的基线档里同样红 ⇒ 与本批无关，归因为先前已存在。
- 出库校验：包 `Element-Plus 0.10.24`，内层 `Element.dll` 3,017,216 B、md5
  `52502d1b7ccaa752fe11d7608f2fa6d7`，与构建产物逐字节相同。

## 五、消费侧口径（电子档案应用）

宿主换包后把批次101/102 写的 `@if` 兜法全部退回原始 `Visible="@(…)"` 写法，业务分支一个字没动；
顺带把两条**从没改过业务标记**的页面救活：

- `Pages/Business/DocPrint.razor`（4 处 `ElRow.Visible`）、`Pages/Business/DocView.razor`（3 处）：
  探针 `_tools/b103_printview_probe.mjs` 改前 红 7 / 共 13（三块同时在场），改后 全绿 13 / 13。
- `DocRemake`（含 `ElProgress Visible="@pg_show"`）退回原口径后 `_tools/b101_docremake_probe.mjs` 全绿 9 / 9；
  `DocBorrow` / `DocLost` / `DocSeale` 的「归还 / 找回 / 解封」页签退回 `ElTabPane.Visible="@(!u_new)"` 后
  `_tools/b102_tabprobe.mjs` 全绿 12 / 12。
- `_tools/b103_fixture_probe.mjs` 全绿 9 / 9：每条"看不见"的判据都配了同一把尺子的"看得见"对照
  （默认 `Visible` 的 `ElProgress` 必须画出轨道、可见 `ElRow` 的子节点必须跟着在），真实页那档先把
  前提写成判据（编辑面板确有页签头 + 29 个输入框）再断言 `.el-progress` 为 0。
- 真实入口冒烟 `_tools/b103_dialogsmoke.mjs`（红 0 / 共 4）：不走夹具，直接 DocManage 行菜单「借阅登记」
  → `dialogService.OpenAsync<DocBorrow>`（u_new=true）→ 弹窗标题「病案借阅」+ 3 个输入框（前提判据先亮），
  页签实读 `["借阅"]`。「借阅归还」那一档（u_new=false 的正向对照）前 10 行菜单里都没出现（借出中的记录
  本轮没有，状态驱动）⇒ 显式记 SKIP 不算红，该正向对照由夹具档 `p-borrow-old` / `p-lost-old` /
  `p-seale-old` 三块承担。只开弹窗、只读 DOM，绝不点「确定」⇒ 不写库。

## 六、已知边界（本批未做）

- `Visible` 仍是本仓 Grow 口径，Element Plus 官方没有这个属性（官方用父级 `v-if`）。XML 文档注释里
  写明了这一点，别把它当成 EP 对齐项。
- 收起只保证"不进渲染树"，不负责宿主自己的 CSS 选择器：靠 `.el-row:nth-child(…)` 之类位置选择器
  定型的版式，块消失后位置会前移。
- `ElCol` 没单独加 `Visible`：要收起一格，把 `@if` 写在 `ElCol` 外层或直接收整行。库里 `ElRow`
  收起时子树本来也不渲染。
