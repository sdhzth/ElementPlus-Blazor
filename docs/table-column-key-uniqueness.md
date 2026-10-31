# ElTable 的 `@key`：为什么必须按列序号，不能按列标题

## 一句话

`ElTable` 的 `<col>` / `<th>` / `<td>` 以前用 `@key="header.Text"` 记账。宿主一张表里只要有两列**标题相同**，
表格第二次渲染（翻页、刷新数据、切页签回来）就会整表抛异常。现在这三处键一律用列序号（`col{i}` / `th{i}` / `td{j}`）。

## 用户侧看到什么

Blazor 抛的是：

```
System.InvalidOperationException: More than one sibling of element 'col' has the same key value, '颜色'. Key values must be unique.
   at Microsoft.AspNetCore.Components.RenderTree.RenderTreeDiffBuilder.ThrowExceptionForDuplicateKey(...)
   at Microsoft.AspNetCore.Components.RenderTree.RenderTreeDiffBuilder.BuildKeyToInfoLookup(...)
```

宿主侧的表现是页面底部那条「发生了未经处理的错误」黄条，而且**这张表从此不再更新**（渲染批被中止）。
在 `zyweb` 的真实复现：物料编辑对话框 → 点「物料条码」页签 → 黄条立刻出现（该表 `u_barcode` 与 `u_color` 两列都叫「颜色」）。

## 为什么首帧不炸、第二次渲染才炸

Blazor 的重复键检查发生在**有旧帧可比的更新 diff** 里（`BuildKeyToInfoLookup` 会把旧树的键塞进一张
`key → index` 的表，塞到第二个同键时才判重）。首帧渲染没有旧树，所以同一个错误代码首帧能画出来。

这条对写测试有直接影响：**只 `Render()` 一次的断言是假绿**。本仓的回归用例
（`test/Element.ComponentTests/ElTableDuplicateColumnTitleTests.cs`）因此统一走「首帧 + 一次 `Render()` 重渲染」，
改动前那两条重渲染用例报红、两条结构对照用例照常绿 —— 红得刚好落在被测的那件事上。

## 三处键各自都有牙吗

有。逐处退回改前写法（其余两处保持修好）后各自报红，说明没有一处是白改：

| 变异 | 结果 | 异常 |
| --- | --- | --- |
| 只退回两处 `<td>` | 2 红 / 2 绿 | `ArgumentException: Attempting to return wrong pooled instance. Get/Return calls must form a stack.` |
| 只退回单行表头的 `<th>` | 2 红 / 2 绿 | `InvalidOperationException: More than one sibling of element 'th' …` |
| 只退回两处 `<col>` | 2 红 / 2 绿 | `InvalidOperationException: More than one sibling of element 'col' …` |

`<td>` 那档报的不是 DuplicateKey 而是 `StackObjectPool` 的 `ArgumentException`：同一行里重复的 td 键先把 diff
引擎的键栈踩坏了，还没走到判重那一步。根因同一个，消息不同 —— 记在这里，免得下次按消息文本找不回来。

## 为什么位置可以当身份、标题不行

- 标题（`Text`）是给人看的显示文本：改一个字、两列重名、宿主本地化，都会改变或撞掉「身份」，但它跟列的语义身份无关。
- 列的位置才是这张表里真正的槽位：`colgroup` 与 `thead`/`tbody` 本来就靠同一个 `Headers` 顺序对齐，
  `<col name="el-table_1_column_@i">` 这个 `name` 也一直在按序号编码。
- 库里本来就有先例：分组表头那两条分支用的是 `@key="@($"c{i}")"`（`ElTable.razor` 的 199/234 行），
  单行表头与表体却按文本记账，两套口径并存。这次统一到序号那套。

## 宿主侧仍然存在的笔误（本批只报告、不代改）

`zyweb` 里三张表有两列同名，改库后它们不再崩，但**显示上仍是两列一样的标题**，属业务文案问题：

| 位置 | 重复标题 | 两列绑的属性 |
| --- | --- | --- |
| `Pages/Materiel/MaterielInfoEdit.razor` 物料条码表 | 颜色 | `u_barcode`、`u_color` |
| `Pages/Dictionary/StyleInfoEdit.razor` BOM 主料表 | 成分 | `mix_component`、`mix_component`（同一属性绑两次） |
| `Pages/Dictionary/StyleInfoEdit.razor` BOM 辅料表 | 成分 | 同上 |

要不要改标题、第二列本来该显示什么，由业务方定；库这边已经把「同名就整表崩」这条兜住了。

## 验收时别误判成串位的一条口径

同一张表的两个 `colgroup` 天生不等长：表头那个末尾固定多一根 `<col name="gutter" width="0" />`
（`ElTable.razor:104`，给滚动条让位的 EP 契约），表体那个没有。所以量到「表头 8 槽 / 表体 7 槽」是**设计如此**。
本次出库复验第一版脚本按「两张表列槽相等」断言，把自己的探针判成了 FAIL，实际产品没问题。
要对齐的是这两条，不是两个 colgroup 的长度：

- 表头一行的 `th` 数 == 表头 `colgroup` 的 `col` 数；
- 表头 `col` 数 == 表体 `col` 数 **+ 1**，且多的那根 `name="gutter"`、`width="0"`。

## 相关

- `docs/table-overflow-tooltip.md`（同一张表体渲染路径上的另一处单元格行为）
- `docs/table-header-cell-and-column-width.md`、`docs/table-column-width-and-paging.md`（列宽与 `colgroup` 的对齐口径）
- 测试：`test/Element.ComponentTests/ElTableDuplicateColumnTitleTests.cs`
