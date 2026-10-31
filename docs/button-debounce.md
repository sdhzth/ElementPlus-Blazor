# ElButton 点击防抖

Element Plus 的 `el-button` 没有防抖参数，这是 Element-Blazor 的扩展能力，用于抑制重复提交、误触双击等场景。

## 新增 API

| 参数 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Debounce` | `int` | `0` | 防抖窗口（毫秒）。`0` 表示不防抖，点击立即触发 `OnClick`。 |
| `DebounceLeading` | `bool` | `false` | `false`：尾部防抖，窗口内只执行**最后一次**点击；`true`：领先沿节流，窗口内只执行**第一次**点击。仅在 `Debounce > 0` 时生效。 |

## 行为对照

以 `Debounce="120"`、在 120ms 内连续点击 3 次为例：

| 配置 | 点击瞬间 | 窗口结束后 | 语义 |
| --- | --- | --- | --- |
| `Debounce="0"`（默认） | 触发 3 次 | — | 与改造前完全一致，向后兼容 |
| `Debounce="120"` | 触发 0 次 | 触发 1 次 | 尾部防抖（防重复提交） |
| `Debounce="120" DebounceLeading="true"` | 触发 1 次 | 仍为 1 次 | 节流（首次立即响应） |

## 用法

```razor
@* 尾部防抖：300ms 内多次点击只提交一次（推荐用于保存/提交按钮） *@
<ElButton Type="ButtonType.Primary" Debounce="300" OnClick="SaveAsync">保存</ElButton>

@* 领先沿节流：首次点击立即执行，之后 500ms 内的点击丢弃（推荐用于刷新/查询） *@
<ElButton Debounce="500" DebounceLeading="true" OnClick="Reload">刷新</ElButton>
```

## 实现要点

| 位置 | 修改 |
| --- | --- |
| `src/Components/ElButton.razor.cs` | 新增 `Debounce`、`DebounceLeading` 参数；`OnButtonClickedAsync` 拆分为「防抖判定 + `RaiseClickAsync` 实际触发」两步 |
| 取消机制 | 每次新点击先 `CancelPendingClick()` 取消并释放旧的 `CancellationTokenSource`，保证只有最后一次点击会真正执行 |
| 生命周期 | `Dispose()` 中取消未执行的点击，避免在已销毁组件上回调 |
| 线程安全 | 延迟结束后通过 `InvokeAsync` 回到渲染器同步上下文再触发 `OnClick` |
| 键盘可达 | Enter / Space 触发的 `OnButtonKeyDownAsync` 走同一入口，同样受防抖保护 |
| 禁用态 | `Disabled` / `Loading` 时直接返回，不进入防抖计时 |

原有 `ClickImage` 点击动画、图片还原逻辑保持不变，只是移入 `RaiseClickAsync`。

## 注意事项

- `NativeType="submit"` 时，浏览器会在点击瞬间提交表单，防抖无法拦截原生提交。需要防抖的表单按钮请使用 `NativeType="button"` 并在 `OnClick` 中手动提交。
- 防抖不会自动切换 `Loading`，如需等待反馈请在 `OnClick` 内自行维护。
- `Debounce` 默认 `0`，未显式设置时行为与改造前完全一致。

## 测试

`test/Element.ComponentTests/ElButtonDebounceTests.cs` 共 6 个用例：

| 用例 | 覆盖点 |
| --- | --- |
| `WithoutDebounce_ClickFiresImmediately` | 默认行为不变，点击立即触发 |
| `TrailingDebounce_CoalescesRapidClicksIntoOne` | 尾部防抖合并窗口内点击 |
| `LeadingDebounce_FiresFirstClickOnly` | 领先沿节流只执行首次 |
| `TrailingDebounce_NextWindowFiresAgain` | 下一窗口可再次触发 |
| `DisabledButton_NeverFiresOnClick` | 禁用态不触发 |
| `DebounceParameters_DoNotAffectRendering` | 参数不影响 `el-button` 渲染 |

回归结果：`dotnet test` 全量 **259/259 通过**（该数字是这批用例刚落地时的全仓口径，现已随各批次增长，
最新口径见 `docs/testing-guards.md`；本文件只关心这 6 例）。

## 偶发记录（2026-10-10 批次⑭，**未解决，按原样留着**）

全量跑第一次红 1 例：`ElButtonDebounceTests.TrailingDebounce_NextWindowFiresAgain`，
`Assert.Equal(1, count)` **Expected 1 / Actual 0**（`ElButtonDebounceTests.cs:100`，该用例耗时 5s ⇒
是上面的 `WaitForAsync` 轮询到 5s 上限后仍没等到 `count` 变 1，日志 `Temp/b14-fullsuite-gate.log`）。

同机复测：单跑这个类 **6/6 绿**（`Temp/b14-debounce-isolated1.log`），随后两次全量都是
**2475 / 0 失败**（`Temp/b14-fullsuite-r3.log`、`…-r4.log`）⇒ 三次全量里红 1 次，与本批改动无关
（本批只动演示外壳 `KeepAliveRouteView.cs` 与新增 `KeepAliveShelfRenderFanOutTests.cs`）。

嫌疑（**只到嫌疑，没做确定性复现**）：尾部防抖那支是 `await Task.Delay(120, token)` 之后
`await InvokeAsync(RaiseClickAsync)`，续延要排到 bUnit 的渲染器 dispatcher 上才落到 `count++`；
xUnit 默认并行跑测试集合、同机当时还有整满的 CPU（本批另有一条「整满也不该拖慢页面帧」的否证实验在跑，
见 `docs/testing-guards.md` §4 第 27 条），这条排队就有机会超过 5s 的轮询上限。

刻意**没有**做的两件事：把 `timeoutMilliseconds` 调大（那只会把「排队慢」伪装修复），
以及给这个类加串行锁/单独收集（掩盖并行本身）。要真定位，得先能确定性复现。
