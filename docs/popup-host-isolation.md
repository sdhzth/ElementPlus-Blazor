# 浮层宿主隔离：队列归属、服务生命周期与"只挂一个宿主"

> 目的：把 `<ElPopup />` 初始化时**接管浮层队列**这件事写成可机器检查的三条契约，
> 并完整记录 2026-10-08 那次「访问过 `/login` 之后整站浮层静默消失」的取证、修法与验证数字。
>
> 相关：`docs/table-edit-issues.md`（同源的 `DialogService.Dialogs` 那一层，五层 flaky 的真身）、
> `docs/testing-guards.md` §3.10 / §3.11（浏览器口径）、`docs/loading-alignment.md`（Loading 的 EP 对齐）。

## 0. 结论：三条必须同时成立

本库的浮层（整屏/区域 Loading、消息、通知、对话框、下拉面板、菜单）**都不是原地渲染**的：
组件把浮层模型注册进**服务持有的集合**，再由页面上**唯一一个** `<ElPopup />` 宿主订阅并渲染。

| # | 契约 | 破了会怎样 | 现在由谁守着 |
| --- | --- | --- | --- |
| 1 | 持有浮层队列的服务必须 `AddScoped` | Blazor Server 上后开的 circuit 接管前一个 circuit 的队列 | `PopupHostScopeTests.AllPopupQueueServicesAreScoped` |
| 2 | 队列必须是**实例成员**，不能是 `static` | 跨测试作用域 / 跨连接共享，同上 | `PopupHostScopeTests.TwoScopesDoNotShareTheLoadingQueue`；上一批的 `DialogService.Dialogs` 去 static |
| 3 | 一个作用域内只挂**一个**宿主 | 已渲染的浮层关不掉；之后新开的浮层只出现在最后挂载的宿主里 | `PopupHostScopeTests.SecondPopupHostInSameScopeIsCounted`（运行时计数 + 控制台告警）与 `DemoMountsThePopupHostOnlyOnce`（演示站自查） |

三条的破法症状都是**静默的**：不抛异常、控制台 0 个 JS 错误，只是"调用了却什么都不出现"，
或者"遮罩出现了但关不掉"。这一点是本篇存在的理由 —— 事故当天我先怀疑的是 CSS 和 z-index。

## 1. 机制：`OnInitialized` 的「解订阅 → 换成新实例 → 再订阅」

`ElPopup.OnInitialized`（`src/Components/ElPopup.razor.cs:184-247`）对**每一类**队列做的都是同一个三步，
共 10 个集合 + 2 个事件：

- 集合：`MessageService.Messages`、`NotificationService.Notifications`、`LoadingService.LoadingOptions`、
  `DialogService.Dialogs`、`PopupService` 的 6 个 `*OptionsCore`；
- 事件：`MessageService.GroupingMerged`、`NotificationService.UpdateOffsetsRequested`；
- 另外 `langSubscription` 也在同处先 `Dispose` 再重订阅（词条变更）。

写这么绕不是为了好看：宿主重连/重挂时必须避免同一个处理器被订阅两次（否则一条消息渲染两遍）。
代价是**"宿主初始化"这个动作本身等于"夺取队列所有权"**：换新实例后，前一个宿主的处理器守着一个
再也没人写入的旧集合。所以只要契约 1/2/3 里任何一条让"第二个宿主"出现（第二个实例、跨作用域共享、
或同一作用域真挂两次），前一个宿主就此失联。

## 2. 本次事故：演示站 `LoginLayout.razor` 里的第二个宿主

`demo/Element.Blazor.Demo/Layout/LoginLayout.razor` 里除了 `App.razor` 那个宿主之外还挂了一个 `<ElPopup />`。
链路是：

1. 进应用：`App.razor` 的宿主初始化，拿到 10 个集合的所有权 —— 一切正常；
2. **站内**访问 `/login`：`Login.razor` 带 `@layout LoginLayout`，`KeepAliveRouteView` 用 `LayoutView` 按页面选布局
   （`demo/Element.Blazor.Demo/KeepAliveRouteView.cs:111-116`），于是 `LoginLayout` 的第二个宿主初始化，
   把同一批集合又换成**它自己的**新实例；
3. 离开 `/login`：路由换了页面 → `LayoutView` 换回 `MainLayout`，`LoginLayout` 那一棵（含它的宿主）**被拆掉**；
   而所有权**没有任何机制会交还** —— `App.razor` 的宿主在 Router 之外、应用生命周期内不会再 `OnInitialized`，
   加上 `ElPopup.Dispose()` 本来也不退订（§6 第 1 条），接管就此变成**永久**的；
4. 此后 `LoadingService.ShowFullscreen(...)` / `MessageBox.AlertAsync(...)` 写入的队列，订阅者是那段已经不存在的渲染树
   → 页面上**什么都不出现**（实测 `inDocument:0`），且**没有任何异常**。

> 注：第 3 步里"所有权交还不了"是从代码读出来的（宿主在 Router 之外、只初始化一次 + `Dispose` 不退订），
> 探针只测到"接管之后新宿主也不渲染"这一步（`inDocument:0 / visible:0 / host:null`）。
> 换句话说：**探针证症状与触发条件，链路解释来自源码** —— 两者分开记，别把推断写成实测。

### 取证要点：必须"站内跳转"，`page.goto` 测不到

`page.goto` 是整页重载 = 新建一个 WASM 应用实例 = 新的 DI 作用域，两个宿主**永远不会共存**，
用它探这条缺陷会得到假的 PASS。探针 `demo/tools/probes/verify-popup-host-single-mount.cjs`
从开头一次 `goto` 之后全部改用「注入临时 `<a href>` 并点击」触发 Blazor 的客户端路由，
并且每步先 `waitForURL` 确认真的换了页 —— 否则"元素没出现"分不清是**没跳成功**还是**跳过去但坏了**。

就绪判据也必须挑"这一页独有的元素"（`/login` 用 `.el-login-heading`）：`.demo-page-title` 两页都有，
KeepAlive 又会让上一页残留，拿它等会读到旧页。

### 实测数字（同一个探针，改前 / 改后）

| 运行 | 结果 | 关键读数 |
| --- | --- | --- |
| 修复前（LoginLayout 还挂着第二个宿主） | **6 / 9 PASS（3 条 FAIL）** | 「访问 /login 之后：整屏 Loading 仍然出现并覆盖视口」FAIL，读数 `{"inDocument":0,"visible":0,"host":null}`；「访问 /login 之后：Alert 仍然出框」FAIL，读数同样是 `inDocument:0 / visible:0 / host:null`；「Alert 仍由同一个宿主渲染（浮层归属没被第二个宿主抢走）」FAIL，`before` 是基线链、`after` 是 `null`；全程 `0 个 JS 异常` |
| 修复后（同一探针、同一 build 流程） | **9 / 9 PASS** | 基线整屏遮罩 `{"inDocument":2,"visible":1,"host":"el-loading-mask is-fullscreen el-loading-fade-enter el-loading-fade-enter-active"}`，2 秒后 `inDocument:0`；Alert 基线与回跳后同一条宿主链 `.el-message-box < .el-message-box__wrapper < #app` |

**这条 FAIL 不是"断言写太严"**：修复前读数 `inDocument:0` 说明文档里连一个遮罩节点都没生成，
而基线三段（访问 `/login` 之前）全绿 —— 缺陷只可能来自那次站内跳转。

## 3. 修法（四层，一层一个职责）

| 层 | 改动 | 位置 |
| --- | --- | --- |
| 注册生命周期 | `AddSingleton<LoadingService>` → `AddScoped<LoadingService>`，与其余浮层服务同一口径；理由写成注释钉在改动的**下一行**（`Extension.cs` 是混合编码文件，见 §6） | `src/Components/Extension.cs:33` |
| 队列的契约 | `LoadingOptions` 的 XML 文档写明"必须是实例成员并且服务按作用域注册"，并说明破了长成什么样 | `src/Components/LoadingService.cs:11-20` |
| 运行时可观测 | 宿主初始化时 `++PopupService.PopupHostCount`，`> 1` 打一条带文档指向的 `Console.WriteLine` | `src/Components/ElPopup.razor.cs:190-200`、计数放在 `src/Components/PopupService.cs:24-32`（放 Scoped 服务上才不会跨作用域互相干扰） |
| 演示站自身契约 | `LoginLayout.razor` 删掉第二个宿主，注释说明"这里不要再挂" | `demo/Element.Blazor.Demo/Layout/LoginLayout.razor` |

## 4. 判据：新增的 5 条用例（`test/Element.ComponentTests/PopupHostScopeTests.cs`）

- `AllPopupQueueServicesAreScoped`：扫 `AddElementPlusServices()` 的 `ServiceDescriptor`，
  把 7 类持队列服务（`LoadingService` / `MessageService` / `NotificationService` / `DialogService` /
  `PopupService` / `MessageBox` / `ElDialogGrowService`）逐条要求 `Scoped`，违规项**列名字**报出来。
- `TwoScopesDoNotShareTheLoadingQueue`：**一个 `ServiceProvider` + 两个 `CreateScope()`**，
  断言服务实例 `NotSame` **且** `LoadingOptions` 集合 `NotSame`；再加一条正向对照
  `Assert.Same(loadingA, scopeA 再次 GetRequiredService)`。
  两个断言缺一不可：只 `NotSame` 可能只是"每次都新建"（Transient），而真正想要的形状是
  **每连接一份、连接内部共享**。
- `SecondPopupHostInSameScopeIsCounted`：0 → `Render<ElPopup>()` → 1 → 再 `Render<ElPopup>()` → 2。
- `HostMountScannerIgnoresCommentsAndCodeSamples`：掩码器的**双侧夹具**（真挂载必须识别；
  Razor 注释 / HTML 注释 / `<code>` 示例里的 `<ElPopup />` 不算）。没这条，下面那条扫描器一写坏就"什么都不匹配"→ 假绿。
- `DemoMountsThePopupHostOnlyOnce`：枚举 `demo/**/*.razor`（跳过 `bin/`、`obj/`），挂载点必须**恰好**是 `App.razor`。

### 为什么 bUnit 的"双 context"证不了这条（上一批的坑）

`PopupHostIsolationTests`（上一批）用两个 `BunitContext` 各起一个宿主，那证明的是
**作用域之间隔离**这条路；但 bUnit 每个 context 会**自己建一个 provider**，
所以"`LoadingService` 注册成 Singleton 时跨作用域共享"这件事在那个形状下**根本复现不出来**。
跨作用域必须换成"同一个 provider + 两个 scope"。

### 反向验证（red-before）

临时把注册改回 `services.AddSingleton<LoadingService>();` → `失败: 2，通过: 3`
（红的正是 `AllPopupQueueServicesAreScoped` 与 `TwoScopesDoNotShareTheLoadingQueue`），
改回 `AddScoped` 后 5/5 绿。守卫没有"改坏了也不会红"的空转项。

## 5. 对宿主的影响（`LoadingService` 生命周期变了）

- **Blazor WASM**：整站一个作用域，行为完全不变。
- **Blazor Server**：这正是要修的 —— 以前多个 circuit 共用一份队列，后初始化的宿主会把前一个 circuit
  正在显示的遮罩接管走（遮罩关不掉 / 后续遮罩不出现）。
- ⚠️ **唯一可能的破坏性变化**：如果宿主把 `LoadingService` 注入进**自己的 Singleton 服务**，
  以前能解析（它当时是 Singleton），现在 Server 上会抛
  `Cannot consume scoped service 'Element.LoadingService' from singleton ...`。
  这种写法本身就在跨连接共享可变队列，正是本缺陷的形状。本仓无此类消费点
  （消费方只有 `ElementComponentBase`、`IContainerComponent`、`ElTable`、`ElPopup` 与演示页 `@inject`，全是组件/作用域侧）。
- ⚠️ **库里拦不住宿主覆盖注册**：DI 以**最后一条**注册为准。宿主若在 `AddElementPlusServices()` 之后
  自己再写 `services.AddSingleton<LoadingService>()`，就又回到跨连接共享。
  §4 的守卫只看 `AddElementPlusServices()` 里的注册项，管不到宿主 `Program.cs`。

## 6. 明确没做的事（下次别以为已经修完）

1. **`ElPopup.Dispose()` 不退订集合处理器**（`src/Components/ElPopup.razor.cs:1228-1232` 只 dispose 了
   `langSubscription`）。这不只影响"合法多宿主"的设计：正因为不退订，**宿主被拆掉后所有权也不会交还**，
   接管因此变成永久的（§2 第 3 步）。现在没症状只是因为"一个作用域只有一个宿主、宿主活到应用结束"；
   只要哪天允许布局级宿主，这条必须先补。
2. **没有多宿主设计**。现在的契约是"一个作用域一个宿主"，第二个直接计数告警。
   真要支持嵌套布局各挂一个，得引入宿主注册表 / portal（哪个队列归哪个宿主），是一次设计改动，不是补丁。
3. **`LoadingService` 那层遮罩没有 EP 的 `is-fullscreen` 类**：`ElPopup.razor:141` 只输出
   `el-loading-mask` + `option.CustomClass`，`is-fullscreen` 目前只由组件式 `ElLoading`（`ElLoading.razor.cs:89-92`）
   和对话框/抽屉的全屏分支输出。属 EP 对齐问题，与本篇缺陷无关，另开一轮。
4. **`PopupHostCount` 只计数、不阻止**。故意不做"第二个宿主就抛异常"：库不该让宿主页面在运行时崩，
   而控制台告警 + 探针 + 守卫已经能让它现形。

## 7. 探针口径里两个会骗人的写法

这条缺陷的取证过程踩了两次，都记在探针注释里：

1. **不能用 `offsetParent` 判可见**：`position:fixed` 的元素 `offsetParent` 恒为 `null`，
   用它会把"明明铺满全屏的遮罩"读成不可见 → 假 FAIL。改用 computed style
   （`display` / `visibility` / `opacity`）+ `getBoundingClientRect()` 覆盖视口比例。
2. **两类遮罩必须分开数**：服务式（`LoadingService` 队列、由宿主直接渲染）与组件式
   （`ElLoading` 常驻在 `.el-loading-parent` 内、不加载时 `display:none`）混在一起数，
   "全屏那层根本没生成"会被读成"生成了但不可见"，缺陷就看不见。分类判据用**祖先**
   （`.el-loading-mask` 且不在 `.el-loading-parent` 里）而不是 `is-fullscreen` —— 后者是 `ElLoading`
   自己的类，宿主渲染那层并不带（见 §6 第 3 条）。

## 8. 这一批的数字

- 新增用例 **5 条**全绿；全量 bUnit **2277 通过 / 0 失败 / 0 跳过**（基线 2272 → +5）。
- 定向连跑 **20 次 0 红**，每次 23 例 —— 过滤条件是
  `ElFeedbackTests|LangOverlayTextTests|PopupHostIsolationTests|PopupHostScopeTests`（18 + 本篇新增 5）。
  四个类**必须一起跑**：单独跑永远绿，只有互相并行才测得到队列隔离性。
- 浏览器侧重新 build 演示站 + 重启 5199 后：本篇新探针 **9/9**，回归 `verify-feedback-messagebox` **39/39**、
  `verify-dialogdata` **19/19**、`verify-lang-i18n` **21/21**、`verify-lang-picker-family` **34/34**，全程 0 JS 异常。
- 跑探针前必须**杀掉 5199 旧实例 → `dotnet build demo/Element.Blazor.Demo` → 重启 devserver**，
  否则测的是旧构建；判据看 `_framework/*.wasm` 的时间戳。
