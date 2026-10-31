# /table ① 可编辑表格：两个问题（均已修复，2026-10-08）

> 记录时间 2026-10-08。以下是**实测结论**，不是推测；复现脚本已留在 `demo/tools/probes/`，
> 直接 `node diag-dt-isolate.cjs` / `node diag-table-typing.cjs` 就能重跑（需先起演示站 5199）。

## 问题一：单元格里的 ElDateTimePicker「确定」点不动 —— 已修

**根因**：footer（含「确定」按钮）是由应用根的 `ElPopup` 宿主渲染的，不在 `ElDateTimePicker`
自己的渲染树里。`OnPanelPickAsync` 选完日期只调了 `StateHasChanged()`，宿主不会重渲染，
于是 `disabled = !CanConfirmPending` 停在初次渲染的值上——空值起步时永远是禁用。
本仓早有这个机制与注释（`ElCascader.razor.cs:678`「面板内容由 ElPopup 宿主渲染，
不在本组件的渲染树里 —— 必须走 RequestRender 才能刷新」），`ElDateTimePicker` 也已有
`RefreshPendingAsync()`（= `RequestRender` + `StateHasChanged`），只是**选日期那一路漏用了**
（时间面板那一路是对的）。

**修法**：`OnPanelPickAsync` 三处 `StateHasChanged()` 改为 `return RefreshPendingAsync()`。

**为什么 bUnit 一开始没抓到**：测试里读弹层用的是 `Render(option.OptionContent)`，等于测试自己
强制重渲染了一次，把"宿主没被通知"这个缺陷遮住了 —— 这是一次典型的**假绿**。第一版用例因此全绿
而浏览器仍复现。ElPopup 又无法在 bUnit 里独立渲染（`RenderDropDownAsync` 抛 NRE），搭不起真宿主，
所以这一面固定由浏览器探针覆盖：`diag-dt-table-probe.cjs`。

**修复后实测**：选 15 号 → `confirmDisabled` 由 `true` 变 **`false`**；改点 20 号 → 选中态跟手；
点确定 → 面板关闭且**单元格值 = `2026-10-20 00:00:00`**，0 JS 异常。

---

## 旧版记录（问题一定位过程，保留备查）

实测（`diag-dt-isolate.cjs`）：

| 场景 | 打开面板 | 点 15 号后 | 点确定后 |
| --- | --- | --- | --- |
| `/datetimepicker` 独立页 | 按钮可用（原本就有值） | — | 值 `2026-10-08 01:37:32` → **`2026-10-15 01:37:32`**，面板关闭 ✔ |
| `/table` ① 单元格（值为空） | 按钮 `disabled` | 面板内 15 号已高亮，但按钮**仍然 `disabled`** | 无反应，单元格值不变，`保存结果：（尚未保存过）` ✘ |

结论：**控件本身没问题，问题在表格单元格这个上下文**。

机制链（都在 `ElDateTimePicker.razor.cs`）：

- 按钮 `disabled = !CanConfirmPending`（`BuildFooter`，约 562 行）
- `CanConfirmPending => IsRange ? pendingRangeStart.HasValue && pendingRangeEnd.HasValue : pendingSingleValue.HasValue`（约 1268 行）
- 单元格值为空时 `pendingSingleValue = Value = null`（`InitializePendingValues`，约 1011 行）→ 打开即禁用，**必须靠点日期解锁**
- 点日期的链路：`ElDatePickerPanel.SelectDateAsync` → `CommitValueAsync` → `OnPick.InvokeAsync` → `OnPanelPickAsync`（约 647 行）→ 设 `pendingSingleValue` + `StateHasChanged()`

两个待验证的怀疑，按可能性排序：

1. **`IsRange` 在表格里被判定为真**。若为真，点一个日期只填 `pendingRangeStart`，要点两次才解锁确定 —— 与症状完全吻合。
   表格里编辑器由 `src/Components/ControlRenders/DateTimePickerRender.cs` 用 `RenderTreeBuilder` 手工搭，
   只传了 `Value` / `ValueChanged` / `Placeholder`，**没传 `Type`**；要确认 `ElDateTimePicker.Type` 的默认值
   以及 `IsRange => Type == DateTimePickerType.DateTimeRange`（约 1168 行）在这条路径上的实际取值。
   验证成本最低的做法：加一条 bUnit 用例，在 `ElTable` 编辑行里渲染该编辑器，点一次日期后断言
   `.el-date-time-picker__confirm` 不再带 `disabled`。
2. **`OnPick` 回调没送达**：`EventCallback.Factory.Create<DatePickerPanelSelection>(this, OnPanelPickAsync)` 的接收者
   与手工渲染树的组件实例是否同一个；或 `StateHasChanged()` 之后 footer 没重绘（`ElementComponentBase.ShouldRender()`
   实测恒为 true，所以这条可能性低）。

## 问题二：单元格输入卡顿 —— 已修（275ms/字 → 37ms/字）

两处独立成因，都改了：

1. **库里残留 13 处 `Console.WriteLine`，且全在热路径**：`RenderConfig` 构造函数、
   `EditingValue` 的 **getter 和 setter**（每次渲染每个编辑器都要读写）、`RenderBase.CreateBind`、
   `ElTree` 遍历节点、`ElTransition` 动画过程。WASM 下每条 `Console.WriteLine` 都是一次托管→JS 的
   interop 调用。全部清掉（有诊断价值的失败分支改 `Debug.WriteLine`，Release 下编译掉）：
   **275 → 142 ms/字**。
2. **双向绑定的 EventCallback 接收者是"整个页面"**：`RenderBase.CreateBind` 用
   `EventCallback.Factory.Create(config.Page, setter)`，而 Blazor 在回调执行完后会标记**接收者组件**
   重渲染 —— 于是每敲一个字重渲染整页（TableDemo 那一页有约 10 个表格）。
   修法：`RenderConfig` 加 `Owner`（表格里指向 `ElTable` 自身），`CreateBind` 用
   `config.Owner ?? config.Page` 当接收者，一次按键只重渲染当前表格：**142 → 37 ms/字**。
   `Owner` 留空时回落 `Page`，其它使用方（表单生成控件等）行为不变。

对照基线：表格外 `/input` 同样连打 12 字是 7~11ms/字，所以还剩约 3 倍差距 = 单个表格子树的 diff 成本，
属可接受范围（要再降需要虚拟化或按格局部刷新 —— 按格局部刷新见下面那一节，虚拟化仍未做）。

另外 `ElTable.razor.cs` 里给 `EditorRenderConfig.ValueChanged` 挂的整表通知，对文本/数字框是**重复**的
（`EditingValue` 的 setter 已经会触发一次），已按控件类型跳过——省一次整表 diff；
实测它不是主因（加诊断日志确认"名称"列已被判为自显示、跳过后耗时不变），
但保留这个跳过，并加了两条契约用例：文本/数字框不挂通知、`ElSelect` 等仍需挂（防改过头）。

### 端到端验证

`verify-table-edit-typing.cjs` **7/7 PASS**：连打 12 字 ≈37ms/字、输入内容完整、
点"更新"后 `保存结果：更新 #1：…` 里就是刚输入的值（跳过重复渲染没有丢编辑值）、0 JS 异常。

## 按格局部刷新：已达成，那条"还剩一个来源"是计数口径造成的错觉（2026-10-08）

把双向绑定的回调接收者从"整张表"进一步缩到"单元格宿主"：`ElDynamicComponent.BuildRenderTree`
在渲染编辑器前设 `Config.Owner = this`，于是 Blazor 事件回调后标记重渲染的是这个单元格子树。

实测（一次连打 12 字的口径，`diag-typing-cost3.cjs`）：

| 阶段 | 表格单元格每键 |
| --- | --- |
| 起点 | 275 ms |
| 清掉 13 处热路径 `Console.WriteLine` | 142 ms |
| `Owner` = ElTable（不再重渲染整页） | 37 ms |
| `Owner` = 单元格宿主（本步） | **23 ms** |
| 对照：表格外 `/input` 同一控件 | 11 ms |

**上一轮记下的"第二来源"不存在。** 当时写的是"bUnit 量到输入一次仍让 `ElTable` 重渲染 2 次"，
逐帧取证（测试内的 `ElTable` 子类只覆写 `BuildRenderTree` 自己数，不动产品代码）结果是：

| 被测 | `ElTable` 自身 `BuildRenderTree` 增量 | bUnit `cut.RenderCount` 增量 |
| --- | --- | --- |
| 表格单元格输入一次 | **0** | 2 |
| 对照：只渲染一个 `ElInput` 的宿主，输入一次 | 1 | 2 |

两行合起来就是结论：**bUnit 的 `IRenderedComponent<T>.RenderCount` 统计的是这次调度里
"该组件子树内的帧变化"，子组件（`ElDynamicComponent` + `ElInput`）的渲染会记在被渲染组件 `T` 头上。**
对照组的 1 对 2 就是这个偏移量：宿主自己 1 次 + 它下面的 `ElInput` 1 次。
表格里那两个"看不见的表格渲染"其实就是单元格宿主和输入框各渲染一次 —— 正是这一步想要的行为。
上一轮那三条线索（`ElInput` 自身 `@oninput` 的接收者、`InputRender` 的 `EnableAlwaysRender=true`、
`ElementComponentBase` 的 `Page` 级联）因此都不用再查：它们没有让表格重渲染。

取证时的调用栈也顺手排除了两种担心：初始两次渲染来自 bUnit 的 `Render`，进入编辑态那次来自
`BeginEditRowAsync → BeginEditAsync → ElTable.razor.cs:2298`；输入之后栈里再没有出现表格。

因此用例从"有界 ≤2"收紧成**零**，并把口径本身钉成一条独立用例：

- `TableEditCellTests.TypingInTextEditorDoesNotRerenderTheTableItself` —— 输入一次后表格自身
  `BuildRenderTree` 增量为 0，同时断言编辑值落到 `EditingValue` 且回写到 DOM 的 `value` 上
  （证明"零"不是因为根本没渲染），并带两个对照：进入编辑态时 `Builds > 0`（计数器确实接得上）、
  改 `ElSelect` 那列的 `EditingValue` 后 `Builds` 必须上涨（选择类仍走整表通知，别改过头）。
- `TableEditCellTests.BunitRenderCountAlsoCountsDescendantRenders` —— 只渲染一个 `ElInput` 的宿主，
  输入一次：自身 `BuildRenderTree` 增 1、`cut.RenderCount` 增 2。这条存在的唯一理由就是警告后来的人
  **不要用 `cut.RenderCount` 证明某个组件没重渲染**；哪天它变成 1，上面那条用例的注释要重新核对。

余下的 23ms/字（对照 11ms）就是单元格子树本身的 diff 成本，与"整表重渲染"无关；
要再往下降只能动虚拟化或把编辑器实例复用起来，是另一件事。

上一轮收尾时的回归：全量 2247 通过 / 0 失败；`verify-lang-i18n` 21/21、`verify-table-edit-typing` 7/7
（后者会重新查询"保存后行内显示的是刚输入的值"，所以绑定接收者改坏会当场暴露）。

本轮（取证 + 收紧断言）**只改测试与文档，产品代码一行未动**，所以浏览器侧耗时不变（仍是实测的 23ms/字）。
全量 **2271 通过 / 0 失败**（`TypingInTextEditorRerendersOnlyItsOwnCellHost` 一条换成
`TypingInTextEditorDoesNotRerenderTheTableItself` + `BunitRenderCountAlsoCountsDescendantRenders` 两条）。

## 那条"老 flaky"已根治：五层原因叠在一起（2026-10-08）

`ElFeedbackTests.MessageBoxPromptCreatesInputAndBeforeCloseCanBlock` 长期被记成"并行争抢抖动"
（`TestWait.cs` 点过名）。这次逐层剥开，其实是**五个独立问题**——现在五层全部定位并修掉了
（第 5 层是 2026-10-08 后一轮补的，见下面那条）。

1. **丢弃调度任务**：`_ = popup.InvokeAsync(() => messageBox.PromptAsync(option))` 把工作项排进渲染器
   队列却没人排干 → 高压下 lambda 根本不执行，弹层压根没出现（日志 `String: ""`、`Check count: 1`）。
   改成 `await popup.InvokeAsync(() => { _ = ...; })`：await 调度本身，但 lambda 内丢弃那个
   "要点按钮才完成"的 Task，否则 await 会挂住。
2. **等待机制用错**：bUnit 的 `WaitForAssertion` **只在"下一次渲染"时重新求值**。
   而"结果 Task 完成""点击是否送达"这类条件**不引发渲染** → 条件早就满足了它也永远不再看第二眼。
   改成：非渲染条件用 `Task.WhenAny(task, Task.Delay(timeout))`；需要等的渲染条件用一个
   自己会泵的门（`WaitUntilAsync`：每轮 `InvokeAsync` 排干 + 让出 25ms，预算同 `TestWait.Timeout`）。
3. **`Find` 与 `Click` 不在同一批次**：中间一旦发生重渲染，拿到的旧元素 handler id 已失效，
   点击静默丢失。改成 `await popup.InvokeAsync(() => popup.FindAll(...).First().Click())`
   —— 本仓早就记过这条，这次绕了一圈才想起来，是我的流程问题。
4. **产品缺陷（顺手修了）**：`MessageBox.CreateButtonRenderer` 把取词包在
   `new RenderFragment(child => ...)` 里当 `ChildContent`，而这个委托实例被缓存在 `option.Buttons`，
   每次渲染都是同一个引用 → `ElButton` 认为参数没变不重渲染 → **弹层开着时切语言，按钮文字永远停在旧语言**。
   改为在外层片段求值、用 `Content` 字符串参数下发。同时 `ShowAsync` 尾部两个 await 加
   `ConfigureAwait(false)`（后面只拼返回值、不碰组件状态），让"结果 Task 的完成"不再依赖 UI 上下文被泵。
5. **两个弹层类并行时互相干扰 —— 真身是 `DialogService.Dialogs` 是 `static`（2026-10-08 定位并修掉）**：
   上面四条修完，两类各自单跑 10 次 0 红，但两类同时跑 20 次仍红 5 次，当时先锁进同一个
   `[Collection("ElPopupHost")]` 保套件可重复，并把根因记成"未定位"。这一轮把它挖出来了：
   `ElPopup.OnInitialized` 对每一类弹层集合做的都是同一个三步 ——
   **解订阅自己的处理器 → 把集合整个换成一个新实例 → 再订阅**。别的集合
   （`MessageService.Messages` / `NotificationService.Notifications` / `PopupService.*OptionsCore`）
   都是 `AddScoped` 服务上的实例成员，各测试作用域互不相干；唯独
   `DialogService.Dialogs` 声明成了 `internal static`。于是：

   1. 测试 A 的宿主初始化：静态集合换成 A 的实例，A 订阅它；
   2. 并行跑的测试 B 的宿主初始化：**同一个静态字段**换成 B 的新实例 —— A 的处理器还挂在
      那个已经被弃用的旧集合上；
   3. A 里调 `MessageBox.PromptAsync` → 写入的是 B 的集合 → A 的宿主永远收不到
      `CollectionChanged` → **弹层压根不出现**，症状与第 1 层完全一样（结果 Task 不完成、
      `FindAll` 数量为 0），所以先前才被误判成"并行争抢抖动"。

   这也解释了为什么"单跑必绿、并行才红"：同一个测试类内部 xUnit 不并行，前一类的宿主已 Dispose，
   后一个宿主接手静态集合时没人还在等它。

   修法：`DialogService.Dialogs` 去掉 `static`（注册本来就是 `AddScoped<DialogService>`），
   队列跟着作用域走；三处读写（`ShowDialog` / `CloseDialogAsync` / `MessageBox.ShowAsync`）
   原本就通过注入的 `DialogService` 实例访问，改完不用动调用点。
   **并行锁已解除**，两个类恢复并行；回归用例是 `PopupHostIsolationTests`
   （两个 `BunitContext` 各起一个宿主，再让 A 弹 MessageBox —— 修复前 20s 超时红，修复后 314ms 绿）。
   顺手排除的两条：`ElPopup` 的 `static ZIndex` 已经用 `Interlocked.Add`（并发安全），
   对话框的 `@key` 用的是 `option` 对象本身。

   ✅ **相邻的那一处已经在同一天单独一批修掉了**（原本是"留作单独一批处理"）：`LoadingService`
   注册的是 `AddSingleton`，而 `ElPopup.OnInitialized` 同样会把它那份 `LoadingOptions` 换成新集合，
   所以多 circuit / 多宿主时后初始化的一方会接管前一方正在显示的遮罩。修法是把注册改成 `AddScoped`
   （`src/Components/Extension.cs:33`），并补了契约文档、运行时宿主计数告警、5 条守卫用例与一支浏览器探针；
   真实复现来自演示站自己的 `Layout/LoginLayout.razor`（那里挂了第二个 `<ElPopup />`，站内访问过 `/login`
   之后整站浮层静默消失、0 个 JS 异常）。机制、取证数字与"明确没做的事"见 **`docs/popup-host-isolation.md`**。

**验证口径**（稳定性数据，别再拿单次通过当结论）：

- 相邻批次（`LoadingService` 去 Singleton + 演示站第二个宿主删掉，同日）：四个弹层类
  `ElFeedbackTests|LangOverlayTextTests|PopupHostIsolationTests|PopupHostScopeTests` 定向连跑
  **20 次 0 红**（每次 23 例 = 上轮 18 + 新增 5），全量 **2277 通过 / 0 失败 / 0 跳过**（基线 2272 + 5）。
  守卫的 red-before 是"临时把注册改回 `AddSingleton` → 失败 2 / 通过 3"。
  浏览器侧新探针 `verify-popup-host-single-mount.cjs` 在修复前 **6/9 PASS（3 条 FAIL，读数 `inDocument:0 / visible:0`）**、
  修复后 **9/9 PASS**；回归 `verify-feedback-messagebox` 39/39、`verify-dialogdata` 19/19、
  `verify-lang-i18n` 21/21、`verify-lang-picker-family` 34/34。全部细节见 `docs/popup-host-isolation.md`。
- 本轮（解除并行锁 + 去 static 之后）：`ElFeedbackTests` + `LangOverlayTextTests` + `PopupHostIsolationTests`
  三类定向连跑 **20 次 0 红**（每次 18 例通过），全量并发跑 **3 次 2272 通过 / 0 失败**。
  这里的"定向"是三类一起跑（不是各类单独跑），因为单独跑永远绿 —— 只有互相并行才能测到隔离性。
- 上一轮（还是 `[Collection("ElPopupHost")]` 锁着的口径，保留备查）：两类定向连跑 20 次 0 红；
  全量 5 次 0 红（1 顺序 + 3 路并发 + 1 顺序），每次 2246 通过 / 0 失败。
  那次是"靠锁拿到的绿"，不是"靠修好拿到的绿" —— 这正是这轮要区分开的地方。
- 浏览器侧（重新 build 演示站 + 重启 5199 后实跑，明细见 `docs/testing-guards.md` §3.10）：
  `verify-feedback-messagebox` **39/39**、`verify-dialogdata` **19/19**、`verify-lang-i18n` **21/21**、
  `verify-lang-picker-family` **34/34**、`verify-table-edit-typing` **7/7**、
  `diag-typing-cost3` 表格单元格 **≈19ms/字**（表格外 ≈13ms/字）、`diag-dt-table-probe` 五步全对，全程 0 JS 异常。
  **注意这一列只证"无退红"**：演示站是 WASM、整站一条 circuit（= 一个 DI 作用域、只有一个 `ElPopup` 宿主），
  缺陷需要第二个宿主才现形，所以正向证据是 `PopupHostIsolationTests` 那条双 `BunitContext` 用例。

## 旧版记录（问题二定位过程，保留备查）

### 可信测量

`diag-typing-cost3.cjs`（一次动作连打 12 字，避开 Playwright 每次动作的固定开销）：

| 场景 | 连打 12 字 | 每字 |
| --- | --- | --- |
| 表格外 `/input` | 126 ms | **≈11 ms** |
| 表格单元格 `/table` | 3297 ms | **≈275 ms（慢 25 倍）** |

`diag-keypress-dom.cjs`：在表格里派发一次 input 事件后 1.2 秒内 **DOM 变更记录 = 0**。
说明开销在 Blazor 的渲染树 diff（重渲染后输出相同，所以没有 DOM 写入），不是写 DOM。

> 更正一处早先的错误结论：本文档旧版写的"每字符 342~382ms"是用 `locator.type()` 逐字测的，
> 那个数字里混进了 Playwright 每次动作的可点击性/稳定性检查开销，**不能当应用耗时用**。

### 真正的根因

`RenderBase.CreateBind`（`src/Components/ControlRenders/RenderBase.cs` 约 78~86 行）用

```csharp
createMethod.Invoke(EventCallback.Factory, new object[] { config.Page, settter });
```

**把 `config.Page`（整个页面组件）当作 EventCallback 的接收者**。Blazor 在事件回调执行完后会标记
接收者组件重渲染 —— 于是每敲一个字，重渲染的是**整个 TableDemo 页面**（那一页上有约 10 个表格），
而不是当前单元格或当前表格。这就是 275ms/字 的来源。

验证过并被排除的假设：`ElTable.razor.cs` 里给 `EditorRenderConfig.ValueChanged` 挂的整表重渲染通知
（`RenderConfig.EditingValue` 的 setter 会 `ValueChanged?.Invoke()`）。加诊断日志确认"名称"列已被
判为自显示控件、跳过确实生效后，**输入耗时没有变化** —— 所以它只是一次重复渲染，不是主因。
该跳过仍保留（省一次整表 diff，且实测不影响任何编辑值的落地：输入后点"更新"，
`保存结果：更新 #1：…` 里能看到刚输入的值）。

### 修法方向（需要单独一轮，改动面比前面几批大）

把 EventCallback 的接收者从"页面"换成"拥有这个编辑器的表格"，让一次按键只重渲染当前表格：

- 需要给 `RenderConfig` 加一个所有者引用（例如 `Owner`，指向 `ElTable` 实例），
  `CreateBind` 里用 `config.Owner ?? config.Page` 当接收者；
- 影响面：所有走 `RenderBase` 的单元格编辑器（表格、表单生成控件、下拉表格等），
  以及依赖 `config.Page` 做 `EventCallback.Factory.Create` 的其它渲染器；
- 必须有的回归：编辑值落地（保存后行值正确）、`ElSelect` 选完显示更新、
  键盘跨格导航（`ElTableEnterCellNavigationTests`）、以及 `diag-typing-cost3.cjs` 的每字耗时对比。

## 问题一的下一步线索

`DateTimeEditorInTableCellUnlocksConfirmAfterPickingDay`（bUnit，表格上下文）**通过**，
即 bUnit 复现不出来；浏览器里则是"面板内部选中态跟着点走、父组件 pending 不变"。
结合问题二的发现，同一个嫌疑适用于它：`Page` 级重渲染 + 弹层内容归属 ——
真实应用里每次交互都会重渲染整页，打开的弹层内容 `RenderFragment` 可能已属于被替换掉的
`ElDateTimePicker` 实例，于是 `OnPick` 送达的是一个不再渲染的实例，footer 的 `disabled` 停在初次渲染的值。
下一步应当在修问题二之后**重跑** `diag-dt-table-probe.cjs`：如果确定按钮随之解禁，两个缺陷同源；
如果仍旧禁用，再往"弹层实例归属"方向查。

## 修完要跑的验证

```bash
# 1) 编译 + 定向用例（表格 / 日期时间）
dotnet build test/Element.ComponentTests/Element.ComponentTests.csproj -c Debug
dotnet test  test/Element.ComponentTests/Element.ComponentTests.csproj --filter "FullyQualifiedName~ElTable|FullyQualifiedName~DateTime|FullyQualifiedName~LangResourceGuard"
# 2) 弹层宿主隔离（这两轮那条缺陷的回归口径；四类一起跑才有意义，单独跑永远绿）
dotnet test  test/Element.ComponentTests/Element.ComponentTests.csproj --filter "FullyQualifiedName~ElFeedbackTests|FullyQualifiedName~LangOverlayTextTests|FullyQualifiedName~PopupHostIsolationTests|FullyQualifiedName~PopupHostScopeTests"
# 3) 全量（当前基线 2310 / 0）
dotnet test  test/Element.ComponentTests/Element.ComponentTests.csproj
# 4) 浏览器实测：确定能落地 + 每字符往返耗时
cd demo/tools/probes
NODE_PATH="$HOME/.workbuddy/ce_verify/node_modules" node diag-dt-isolate.cjs
NODE_PATH="$HOME/.workbuddy/ce_verify/node_modules" node diag-table-typing.cjs
# 5) 浮层宿主唯一性（必须**站内跳转**才测得到，探针已用注入 <a> 点击；page.goto 会得到假 PASS）
NODE_PATH="$HOME/.workbuddy/ce_verify/node_modules" node verify-popup-host-single-mount.cjs
```

注意：改的是库源码，跑探针前必须**杀掉 5199 旧实例、重新 build 并重启演示站**，否则测的是旧构建。

## 顺带发现

`src/Components/ElDatePicker.cs.bak2` 是一个历史遗留的备份文件，躺在源码目录里（`.bak2` 不参与编译，
但会干扰 grep 与阅读）。建议连同其它 `_backup_before_*` 目录一起归档到仓库外或加进 `.gitignore`。
（更正：这句话原先写的是"本仓没有版本控制，所以每轮手工建备份目录"—— 现在仓库**已经在 git 管理下**
（`main` 分支，近期提交如 `134c95b`），那些手工备份目录属于历史遗留，新改动不需要再手工复制一份。）

## 供应商列显示态：出编码还是出名称（批次㉔，2026-10-12，已按用户口径改为出名称）

用户口径（原话）：「供应商列显示态出编码(S03)还是出名称，**出名称**」。

**改的只有演示页，组件侧零改动**：`demo/Element.Blazor.Demo/Pages/TableDemo.razor` 给该列补了一段显示模板
—— 值域仍是编码（`Product.Supplier = "S01"…`、编辑器 `EditorValueField="Code"`、`OnSave` 写回的都是编码），
只是显示态做一次 code→name 映射，与编辑态触发器（按 `EditorTextField="Name"` 取名称）同源，
所以退出编辑态不会再跳回 `S03`：

```razor
<ChildContent Context="cellRow">@SupplierDisplayText(cellRow)</ChildContent>
```
```csharp
private static string SupplierDisplayText(object row)
    => row is Product p ? (MockData.Suppliers.FirstOrDefault(s => s.Code == p.Supplier)?.Name ?? p.Supplier) : Convert.ToString(row);
```

**为什么写成显式 `<ChildContent>`**：这一列同时带具名片段 `<EditorColumns>`（给 `ElDropDownTableGrow` 供列定义），
Razor 不允许在具名片段之后再放隐式子内容 —— 实测编译报 `error RZ9996: Unrecognized child content inside component 'ElTableColumn'`，
提示可接受的顶层项只有 `ChildContent` / `Header` / `EditorColumns`。

**为什么不影响编辑与排序**（读源码确认，不是猜）：`ElTable` 单元格渲染优先走 `header.Template(row)`、否则才走 `header.Eval(row)`
（`src/Components/ElTable.razor` 显示态分支），而 `CanEdit` 的判据是 `header.IsEditable && header.Eval != null`
（`ElTable.razor.cs:1952`）—— `Eval` 仍由列的 `Property` 生成，加模板不会把它抹掉；排序与筛选读的是 `EvalRaw`
（`ElTable.razor.cs:2793-2806`），拿到的还是编码域，口径没动。

**证据（两条臂，改前/改后各一份）**：
- 翻面基线＝改前那次：`Temp/recheck-b23-probe-table-editors.log` 第 19 行 **FAIL**「⑤ 行内供应商已变为所选供应商」，
  同一行读数里该格显示的是 `S03`（编辑态触发器已正确写成「星澜医药站」，红只红在显示态）。
- 改后：`Temp/b24-probe-table-editors-run1.log` **19/19 全 PASS**，同一条判据读数变成「… 保健品 星澜医药站 …」。
  两份日志逐行 diff 的结论：**前 17 行逐字一致**，只有第 18、19 两行变 —— 19 行 FAIL→PASS，
  18 行「⑤ 行内类别」判据没变，只是它把整行文本当读数带出来，那一格里的供应商从 `S03` 变成了「星澜医药站」。
- 显示态逐行取证（不进编辑，`Temp/b24-supplier-display.log`）：6 行数据分别是
  云溪医药控股 / 北屿医药供应链 / 星澜医药站 / 青梧医药物流 / 澜溪器械耗材 / 云溪医药控股，
  `supplierCellsShowingCode = 0`（正则找 `S0\d` 一格都没有）；同页其余列（ID、名称、类别、单价、库存、上架、上架时间、操作）文本与改前一致。
- 全页只有这一处声明 `Property="Supplier"`，且本页所有表格都是 `AutoGenerateColumns="false"` ⇒ 不存在别处还按编码显示供应商。

**顺带发现的库侧空白（本批没动，等用户定）**：`ElTableColumn.Formatter`（EP 2.14.0 有此契约）在本库
声明了（`src/Components/ElTable.razor.cs:111` 附近）、也从列参数复制进了 `TableHeader`（`ElTable.razor.cs:154`），
但 `src` 内**没有任何消费者**——传了也不生效，属死参数。今天没有演示页或测试用到它，所以接它本来是 DOM 中性的；
但「EP 有、本库形似而实无」这件事要不要补，属库侧语义决定，不在本批顺手扩范围。

> **后续（2026-10-27 批次105 · #178/#179）**：上面这段「等用户定」已经定了 —— 用户点名**接线**（连同 bUnit + 探针 + 翻面 + 重打包），
> 现在 `Formatter` 是真行为，EP 口径的「插槽优先 → formatter → 默认渲染」三档落在 `src/Components/ElTable.razor:446-448`（树形列）
> 与 `:487-489`（主分支），实现、12 例 bUnit、八档变异与 14 项浏览器判据见 **`docs/table-formatter-alignment.md`**。
> 本节其余内容（供应商显示态映射那件事）与本批无关，未受影响：`ElTableFormatterColumnTests.WithoutFormatter_RenderingIsUnchanged`
> 与探针第 3 项都专门钉了「不挂 formatter 时渲染逐档不变」。
