# ElTable 选择列宽度：`SelectionColumnWidth`（批次82）

2026-10-17 · 触发：zyweb 生产订单新增对话框「生产订单数量」页签，用户原话
「这个列太宽了，最多6个汉字宽就够了，前面选择的列的宽度也调整小一点」。

## 结论

- `HasSelectionColumn` 渲染的第一列勾选框列，宽度原先**硬编码 55px**，宿主无法收窄。
- 新增参数 `SelectionColumnWidth`（像素，**默认 55**），三处硬编码全部换用：
  1. 表头 colgroup：`ElTable.razor` `<col name="el-table_1_column_0" width="@SelectionColumnWidth" />`；
  2. 表体 colgroup：同上（表体那张 table，两根必须同一套值才对得齐）；
  3. `ColumnsTotalWidth`（`ElTable.razor.cs`）：横向滚动记账 `HasSelectionColumn ? max(0, SelectionColumnWidth) : 0`。
- 默认值不变 ⇒ 所有既有表格零变化；`ElDropDownTableGrow` 等内部使用者走默认值，同样不受影响。

## 用法

```razor
<ElTable HasSelectionColumn="true" SelectionColumnWidth="40" ...>
```

## 配套口径（消费侧，zyweb #151）

钉死列（选择列 40 + 名称列 100px）要真正钉得住，前提是**表挂 `TableLayout="fixed"`**：

- fixed 布局下不带 width 属性的 `<col>`（批次㛸 的 auto 列）**均分剩余宽度**——伸展由 auto 列承担；
- 不挂 fixed（库默认 auto 布局）时，全部列钉死会把富余**按比例摊给每一列**
  （部门维护表 160/160/200/60 在 1106px 容器实测摊成 305/305/381/114），
  选择列/名称列的声明宽度会被打回原形。

## 验证

- `_tmp_probe/b85_probe1_run.log`：选择列 40±2、名称列 100±2、auto 数量列均分余量、
  无横向溢出、表头不裁字、窄视口上限档，PASS/FAIL 见该档。
