# ElTable 动态列

## 结论

- **改造前：不支持**。列只在 `ElTableColumn.OnInitialized` 时注册一次，而 `ElTable` 在 `headerRendered = true` 之后就不再渲染 `ChildContent`，运行期新写的 `<ElTableColumn>` 根本不会被实例化，自然也不会出现。
- **改造后：支持**，提供命令式（立即生效）与声明式（ReloadColumns）两条路径。已有数据的表格加列后，单元格按 `Eval` 立即求值显示，不需要重新加载数据。

## 新增 API（`src/Components/ElTable.razor.cs`）

| 成员 | 说明 |
| --- | --- |
| `IReadOnlyList<TableHeader> Columns` | 当前已注册列的只读快照 |
| `AddColumn(TableHeader header)` | 添加自定义列（自行设置 `Text`/`Eval`/`Template`/`SortNo`） |
| `AddColumn(string property, string text = null, int sortNo = 0)` | 按属性名添加列，反射取 `PropertyInfo` 并走 `DisplayRenderFactory`；**同属性已存在则跳过**，避免重复点击造成重复列 |
| `AddColumn(string text, Func<object, object> eval, int sortNo = 0)` | 计算列／派生列，立即对已有行求值 |
| `AddColumn(string text, RenderFragment<object> template, int sortNo = 0)` | 模板列 |
| `AddColumns(params TableHeader[] headers)` | 批量添加 |
| `RemoveColumn(Func<TableHeader, bool> predicate)` / `RemoveColumn(string text)` | 移除列，返回是否命中 |
| `ClearColumns()` | 清空所有列 |
| `ReloadColumns()` | 重新从 `ChildContent` 收集列定义（声明式场景） |

## 用法

### 1. 命令式（推荐，立即生效）

```razor
<ElTable @ref="table" TItem="Person" DataSource="people" AutoGenerateColumns="false">
    <ElTableColumn Property="Name" Text="姓名"></ElTableColumn>
</ElTable>

<ElButton OnClick="AddDeptColumn">添加部门列</ElButton>

@code {
    ElTable table;

    void AddDeptColumn()
    {
        // 已有数据的行会立即显示部门值
        table.AddColumn("Dept", "部门");

        // 计算列：不改数据也能显示派生值
        table.AddColumn("明年年龄", row => ((Person)row).Age + 1);

        // 插到最前：SortNo 更小
        table.AddColumn("工号", row => ((Person)row).Code, -1);
    }

    void RemoveDeptColumn() => table.RemoveColumn("部门");
}
```

### 2. 声明式（`@foreach` + `ReloadColumns`）

列写在 `ChildContent` 的 `@foreach` 里时，修改列集合后**必须**调用 `ReloadColumns()`，因为 `headerRendered` 为 `true` 时 `ChildContent` 不再渲染、新列组件不会被实例化：

```razor
<ElTable @ref="table" DataSource="people" AutoGenerateColumns="false">
    @foreach (var col in visibleColumns)
    {
        <ElTableColumn Property="@col.Property" Text="@col.Text"></ElTableColumn>
    }
</ElTable>

@code {
    void ShowDept()
    {
        visibleColumns.Add(new ColumnDef { Property = "Dept", Text = "部门" });
        table.ReloadColumns();   // 缺这行新列不会出现
    }
}
```

## 实现要点

| 点 | 处理 |
| --- | --- |
| 立即生效 | `ApplyColumnChanges()` 内 `Headers = Headers.OrderBy(x => x.SortNo)` 后 `InvokeAsync(StateHasChanged)`；**不重置 `headerRendered`**，避免重渲染 `ChildContent` 造成列重复注册 |
| 已有数据 | 单元格渲染走 `header.Eval(row)`，新列对现有 `rows` 直接求值，无需重新加载 `DataSource` |
| 属性列 | 复用内置 `displayRender.CreateRenderFactory(...)`，与声明式列显示效果一致（Format、控件渲染器） |
| 位置控制 | 依赖 `TableHeader.SortNo`，渲染前统一排序 |
| `ReloadColumns()` | 清空 `Headers` + `headerRendered/headerInitilized/renderCompleted = false` → 重新渲染 `ChildContent` → 列重新注册 → `InitlizeDataSourceAsync` 重建 `rows` |

## 注意事项

- `AutoGenerateColumns` 默认 **true**：会自动为每个属性生成列（同名属性会跳过）。动态列场景建议设为 `false`，否则初次渲染就会多出属性列。
- `HasSelectionColumn` 默认 **true**：表头第一列是选择列（无文本），统计列数时记得排除。
- `ReloadColumns()` 会重建 `rows`（走 `InitlizeDataSourceAsync`），树形表格的展开状态、选中行可能重置；仅命令式增删列则不会。
- `AddColumn(string property, ...)` 对同名属性静默跳过；若确实需要同属性多列，用 `AddColumn(TableHeader)` 或 `AddColumn(text, eval)`。

## 测试

`test/Element.ComponentTests/ElTableDynamicColumnTests.cs` 共 7 个用例：

| 用例 | 覆盖点 |
| --- | --- |
| `AddColumnByProperty_OnPopulatedTable_ShowsImmediately` | 已有数据表按属性加列，表头与单元格立即出现 |
| `AddComputedColumn_EvaluatesExistingRowsImmediately` | 计算列对现有行立即求值 |
| `AddColumn_WithSortNo_IsInsertedAtExpectedPosition` | `SortNo` 控制插入位置 |
| `RemoveColumn_DropsHeaderAndCells` / `RemoveColumn_MissingColumn_ReturnsFalse` | 移除列与未命中返回 false |
| `ReloadColumns_PicksUpDeclarativeColumnChanges` | 声明式 `@foreach` 加列后 `ReloadColumns()` 生效 |
| `Columns_ReflectsRegisteredHeaders` | `Columns` 快照反映注册结果 |

回归结果：`dotnet test` 全量 **266/266 通过**。
