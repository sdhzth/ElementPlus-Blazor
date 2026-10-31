# ElTable 编辑状态：回车进入下一个单元格 / 下一行同一列

对齐 EhLib / Excel 的录入习惯：表格处于编辑状态时，在单元格里直接按 **回车** 即跳到下一个可编辑单元格（或下一行的同一列）并且可以继续录入，不需要鼠标点来点去。

## 结论（改动前 / 改动后）

| 场景 | 改动前 | 改动后 |
| --- | --- | --- |
| 编辑行内按回车 | 不跳转；而且会带着当前的 `saveAction`（双击进入编辑时是 `Create`）**误保存一次**，未注册 `OnSave` 时只弹一个提示 | 跳到下一个可编辑单元格并自动聚焦，**不再误保存** |
| 需要按列逐行录入 | 只能水平走，或每行都重新双击 | `EnterNavigationMode="SameColumnNextRow"` 后回车直接进入下一行同一列 |
| 双击进入编辑 | 只是把整行变成编辑器，无焦点、无当前单元格概念 | 自动激活并聚焦起始单元格（取双击位置所在列） |
| 当前编辑单元格 | 无任何视觉标识 | 单元格带 `el-table__cell--editing` 高亮 |
| 点其它单元格 | 只是选中行 | 该单元格变成当前活动单元格（焦点跟随） |
| 只读表格（`IsEditable=false`）按回车 | 会走到保存分支，数据源列未初始化编辑器时可能抛异常 | 直接忽略，不触发保存 |
| `SameColumnNextRow` 模式在**最后一行**按回车 | 停住不动，一列录完必须先加行才能接下一列 | 批次㊻：跳到右边一列、留在当前行 → **批次㊾：跳到右边一列、落在该列第一行** |
| 「每格常驻编辑器」的表格（`IsEditable=false`，模板列直接把输入框画在每个单元格里） | 回车什么都不做（库里那条“活动单元格”的路要求 `IsEditable=true` 且有 `EditorRender`，这类表格一条都不满足） | `EnterNavigatesEditors="true"` 后由 `dom.js` 按 DOM 行列跳格（批次㊻，末行落点同批次㊾） |

## 新增参数

| 参数 | 位置 | 类型 | 默认 | 说明 |
| --- | --- | --- | --- | --- |
| `EnterMovesToNextCell` | `ElTable` | `bool` | `true` | 编辑状态下回车是否跳格（总开关）。整体关闭时恢复旧行为（但旧行为里的误保存已一并修掉） |
| `EnterNavigationMode` | `ElTable` | `TableEnterNavigationMode` | `NextCell` | 跳转方向：`NextCell` = 本行下一个可编辑单元格；`SameColumnNextRow` = 下一行的同一列 |
| `EnterWrapsToNextRow` | `ElTable` | `bool` | `false` | **仅 `NextCell` 模式生效**：在最后一个（第一个）可编辑单元格回车时跨行跳到下一行（上一行）。**跨行跳转不保存、也不保留上一行的编辑值** |
| `EnterMovesToNextCell` | `ElTableColumn` / `TableHeader` | `bool` | `true` | 单列退出跳转序列（该列不参与回车跳转） |
| `EnterNavigatesEditors` | `ElTable` | `bool` | `false` | **批次㊻**：给「每格常驻编辑器」的表格（`IsEditable=false`）开回车跳格，走 DOM 而不是编辑单元格那条路，见下文「常驻编辑器表格」 |

键盘约定：

| 按键 | `NextCell`（默认） | `SameColumnNextRow` |
| --- | --- | --- |
| `Enter` | 下一个可编辑单元格 | 下一行的同一列 |
| `Shift + Enter` | 上一个可编辑单元格 | 上一行的同一列 |
| 其它按键 | 不受影响（不拦截 `Tab`，浏览器默认行为保留） | 同左 |

## 两种跳转方向

### `NextCell`（默认，行内水平）

录入时一行内从左到右走。到最后一个可编辑单元格后停留在原地（除非 `EnterWrapsToNextRow="true"` 跨到下一行第一个可编辑列）。

```
姓名 ─Enter→ 年龄 ─Enter→ 科室 ─Enter→ 停住（或跨行回到下一行“姓名”）
```

### `SameColumnNextRow`（列内垂直）

保持列不变，一行一行往下走，适合“把某一列逐行填满”的录入场景（如给一批病人逐个填诊断）。

```razor
<ElTable @ref="table" DataSource="patients" IsEditable="true"
         AutoGenerateColumns="false" HasSelectionColumn="false"
         EnterMovesToNextCell="true"
         EnterNavigationMode="TableEnterNavigationMode.SameColumnNextRow">
    <ElTableColumn Property="Name" Text="姓名"></ElTableColumn>
    <ElTableColumn Property="Age"  Text="年龄"></ElTableColumn>
    <ElTableColumn Property="Dept" Text="科室"></ElTableColumn>
</ElTable>
```

```
双击第 1 行“科室” ─Enter→ 第 2 行“科室” ─Enter→ 第 3 行“科室” …
                        Shift + Enter 反向回到上一行“科室”
             最后一行“科室” ─Enter→ 右边一列的“科室”第一行（批次㊾）
```

- 末行按回车改为**往右换列、落在该列第一行**（批次㊾，此前批次㊻ 是留在当前行）：已经是最后一列时才停住（返回 `false`）。这样一列填满能自然接上下一列，并从表头下面第一格重新开始填。
- **换列会换编辑行**：因此上一行尚未提交的编辑值按 `SwitchEditingRow(int)` 的口径丢弃（每列 `Clear`）；这与批次㊻ 「留在当前行、值还在」的语义不同，是 ㊾ 的有意取舍。
- 首行 `Shift + Enter` **仍是停住**，不往左换列（换列只给正向，与旧语义的既有测试锁法一致）。
- 整块表**只有一个编辑行**：跳到新行会把上一行尚未提交的编辑值一并清掉（与 `NextCell` 模式跨行时的行为一致）。若需要“回车即提交本行”，请在 `OnRowDblClick` / 外部按钮里走保存流程。
- 自动跳过 `hiddenRows`（树形折叠行）。

### 常驻编辑器表格（`EnterNavigatesEditors`，批次㊻）

宿主里更常见的一类表格**不走** `IsEditable` 模型：模板列把 `ElInput` / 数字输入框直接画在**每个单元格**里，整表没有“编辑行 / 活动单元格”的概念（`GetEditingColumns()` 要求 `CanEdit(header) && header.EditorRender != null`，这类列一条都不满足）。上面那条 `MoveEditingCellAsync` 的路在这里**一步都跑不到**，所以给它单独开了一个属性。

```razor
<ElTable DataSource="sizegrid" IsEditable="false" AutoGenerateColumns="false"
         EnterNavigatesEditors="true">
    @* 每格里常驻一个输入框 *@
</ElTable>
```

- 焦点在当前格内的编辑器上按 `Enter` → 下一行同一列；到最后一行 → 右边一列、**落在该列第一行**（批次㊾，之前是留在当前行）；`Shift + Enter` 反向走一行，到首行不再往左换列（与 `SameColumnNextRow` 的边界口径刻意保持一致）。
- 目标格内**没有可聚焦编辑器**时保持不动（返回 `false`），焦点不会掉出表格。末行换列时从右边第一列的**首行**往下扫，该列整列都没有编辑器就继续往右找下一列，仍然从它的首行开始。
- 「算编辑器」的判据：`input` / `textarea` / `select`（非 `hidden`、非 `disabled`）。`readonly` 的 `input` 默认**不算**，但 EP 把下拉、日期、数字步进做成 readonly input，所以落在 `.el-select` / `.el-date-editor` / `.el-time-picker` / `.el-input-number` 里的 readonly input **算**。
- 实现：`dom.js` 的 `elementTableEnterNextEditor(shift)` 从 `document.activeElement.closest('td')` 出发按 DOM 顺序取行列；C# 侧 `OnEditingRowKeyDownAsync` 在 `!IsEditable` 分支里调它（行上的 `onkeydown` 一直是挂着的，输入框的事件冒泡到 `<tr>`）。默认 `false` ⇒ 不加属性的既有表格行为逐字节不变。
- 端到端验证（批次㊻ 当时口径，宿主无业务数据、真页面表格 0 行时）：在宿主里临时加一张「本地数据 + 常驻编辑器」的 `ElTable` 探针页，对格内 `<input>` 派发 `keydown(key=Enter)` 并等 1.2s 读 `document.activeElement`，同页再放一张**不开属性**的同构表作负向对照。实测正向为同列下行 → 末行换列（留在当前行）→ 跳进 `.el-select` 的 readonly input → 末行末编辑器列原地不动；Shift 反向到首行停住；对照组焦点一格不动。探针页验完即删。
- 批次㊾ 复验（改的是同一处末行落点）：`dom.js` 侧在合成多行多列真实 DOM 上调**真函数** `elementTableEnterNextEditor`，
  验「末行 → 右边一列首行」，并配「右邻整列无编辑器 → 继续往右仍从首行起」「非末行同列下行」「末行末列返回 `false` 原地不动」
  「首行 `Shift` 返回 `false` 不换列」「`Shift` 同列上行」「单行表换列但行号仍 0」共 7 档（正向/边界/对照齐备）；
  同批用克隆真 `<col>`/`<th>` 节点的档位量首列 80px。宿主侧探针 `zywebGroup/_tmp_probe/b60_verify.mjs`，
  日志 `b60_verify_run.log` 与最终包复跑 `b60_verify_run_final.log`（**29 PASS / 0 FAIL**，控制台 0 错误，无未处理错误黄条）。

## 新增成员

| 成员 | 说明 |
| --- | --- |
| `ElTable.EditingRow` | 当前编辑中的行；未编辑为 `null` |
| `ElTable.ActiveEditingColumn` | 当前活动编辑单元格对应的列 |
| `ElTable.ActiveEditingCellIndex` | 活动单元格在 `EditableColumns` 中的下标，`-1` 表示无 |
| `ElTable.EditableColumns` | 参与回车跳转的列（显示中、非复选框列、可编辑、已初始化编辑器） |
| `ElTable.MoveEditingCellAsync(int direction)` | 以代码方式按当前模式移动（`-1` 上一个 / `1` 下一个），返回是否移动成功 |

参与跳转的单元格只限于“可编辑列”：忽略列、复选框列、`CanEdit` 为假（无 `Eval`）以及没有编辑器的列都会被跳过。

## 用法

```razor
<ElTable @ref="table" DataSource="patients" IsEditable="true"
         AutoGenerateColumns="false" HasSelectionColumn="false"
         EnterMovesToNextCell="true" EnterWrapsToNextRow="false">
    <ElTableColumn Property="Name" Text="姓名"></ElTableColumn>
    <ElTableColumn Property="Age"  Text="年龄"></ElTableColumn>
    <ElTableColumn Property="Dept" Text="科室" EnterMovesToNextCell="false"></ElTableColumn>
</ElTable>
```

双击某行的“姓名”单元格即可开始录入，之后一路回车即可（姓名 → 年龄，科室列已被排除）。

## 实现要点

1. **活动单元格状态**：`editingCellIndex` + `activeEditingHeader` 记录当前单元格，`editingRow` 改为属性，退出编辑（置 `null`）时自动清空活动单元格。
2. **只在编辑行挂事件**：单元格的 `id` 与点击切换事件通过 `GetEditingCellAttributes(row, header)` 返回，非编辑行返回空字典——避免给整张表所有单元格都注册事件、写 DOM 属性。
3. **回车在 `keydown` 处理**：行上的 `keydown` 负责跳转，`keyup` 只做兼容分发（`OnRowKeyUpAsync`），保证编辑行内的回车不会走进保存分支。
4. **聚焦跨渲染周期**：跳转只是登记 `pendingFocusCell`，真正的 `elementTableFocusCell(cellId, selectText)` 在 `OnAfterRenderAsync` 里调用，避免在 DOM 还没更新时聚焦。宿主页面未引用 `dom.js` 时静默忽略（只影响自动聚焦）。
5. **双击定位起始单元格**：`elementTableColumnIndexAt(x, y)` 用鼠标坐标 + `document.elementFromPoint` 找到点到的列，取不到时从第一个可编辑列开始。
6. **切换编辑行会重置编辑器**：切换行前对每列调用 `Clear`，避免新行显示上一行的编辑值（这个问题在不回车、直接双击另一行时也存在，本次一并修掉）。
7. **两种模式共用一套状态**：`MoveEditingCellAsync` 按 `EnterNavigationMode` 分发到水平/垂直两个私有方法；`FindVisibleRowIndex`（找最近可见行）与 `SwitchEditingRow`（切行并重置编辑器）被两个方向复用。

## 前端资源

- `wwwroot/js/dom.js`：新增 `elementTableFocusCell`、`elementTableColumnIndexAt`；批次㊻ 新增 `elementTableEnterNextEditor`（常驻编辑器表格的行列跳格）；批次㊾ 把该函数末行换列的落点从「当前行」改成「从新列首行往下扫」
- `wwwroot/css/index.css`：`.el-table__cell--editing` 高亮样式

## 测试

`test/Element.ComponentTests/ElTableEnterCellNavigationTests.cs`，共 24 个用例：

| 用例 | 覆盖点 |
| --- | --- |
| `DoubleClickRow_StartsEditing_AtFirstEditableCell` | 进入编辑、活动单元格、高亮类、DOM id、聚焦 JS 调用 |
| `Enter_MovesToNextEditableCell_AndFocusesIt` | 回车跳转 + 焦点 id 变化 + 全选文本 |
| `Enter_OnLastCell_StaysWhenWrapDisabled` | 默认不跨行 |
| `Enter_OnLastCell_WrapsToNextRow_WhenEnabled` | 跨行跳转 |
| `ShiftEnter_MovesToPreviousEditableCell` | 反向跳转 |
| `Enter_DoesNotTriggerSave` | 回归：回车不再误保存 |
| `EnterMovesToNextCellDisabled_KeepsCell_AndDoesNotSave` | 关闭参数后保持单元格且不保存 |
| `NonEditableTable_EnterIsIgnored` | 只读表格回车不抛异常 |
| `ClickAnotherCell_MakesItActive` | 点击切换活动单元格 |
| `MoveEditingCellAsync_NavigatesProgrammatically` | 公开 API 与边界 |
| `DefaultMode_IsNextCell` | 默认仍是水平模式，回车不换行 |
| `SameColumnMode_Enter_MovesToNextRowSameColumn` | 垂直模式回车换行同列（DOM 单元格 id 到 `_r1_c0`） |
| `SameColumnMode_Enter_KeepsActiveColumn` | 在第 3 列回车仍停在第 3 列（`_r1_c2`） |
| `SameColumnMode_ShiftEnter_MovesToPreviousRowSameColumn` | 反向换行 |
| `SameColumnMode_Enter_OnLastRow_MovesToNextColumnFirstRow` | 批次㊾：末行回车跳到右边一列、**编辑行回到第一行**（活动单元格 `_r0_c1`） |
| `SameColumnMode_Enter_OnSingleRow_MovesToNextColumnSameRow` | 正向对照：表只有一行时末行即首行 ⇒ 换列但编辑行不变（`_r0_c1`） |
| `SameColumnMode_Enter_OnLastRowLastColumn_Stays` | 末行且最后一列才真正停住 |
| `SameColumnMode_ShiftEnter_OnFirstRow_Stays` | 首行停住（反向不换列） |
| `SameColumnMode_EnterMovesToNextCellDisabled_DoesNothing` | 总开关在垂直模式下同样生效 |
| `SameColumnMode_MoveEditingCellAsync_MovesRows` | 公开 API 在垂直模式下移动行 |
| `AlwaysOnEditors_Enter_CallsDomNavigation` | 批次㊻：`IsEditable=false` + `EnterNavigatesEditors=true` ⇒ 回车发一次 `elementTableEnterNextEditor(false)` |
| `AlwaysOnEditors_ShiftEnter_CallsDomNavigationReversed` | `Shift + Enter` 传 `true`（反向） |
| `AlwaysOnEditors_WithoutFlag_DoNotCallDomNavigation` | 正向对照的反面：不开属性的非编辑表格**一个 JS 调用都不发** |
| `EditableTable_IgnoresAlwaysOnEditorNavigation` | 编辑表格走编辑单元格那条路，属性开着也不去问 DOM |

## 已知限制

- 新增行（“+”行）里的回车仍然是**保存这一行**（保持原有交互），不做跳转。
- 跨行跳转（`EnterWrapsToNextRow="true"`、`SameColumnNextRow` 模式跨行，以及批次㊾ 的**末行换列**）视为放弃上一行未保存的修改，需要保留请自行在按键前保存或改用“更新”按钮。
- 树形列（`ElTableTreeColumn`）不参与回车跳转。

