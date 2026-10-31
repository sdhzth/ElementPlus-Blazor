# ElSelect 的 `Style` 落在根节点（批次㊻）

宿主 `zyweb` 款号编辑里两颗下拉写了非 100% 宽度 —— `<ElSelect Style="width:250px;">`（条码生成方式）与
`<ElSelect Style="width:130px;">`（工序分类）。浏览器实测**一点作用都没有**：`.el-select` 计算宽 794px，
把「工序分类」那一行 flex 的剩余空间全吃掉，就是用户标的「这个字段太宽」。

## 1. 定位（不是消费侧能解决的那类）

- `ElementComponentBase.Style`（`src/Components/ElementComponentBase.cs:74`）是全站通用内联样式属性，
  渲染方式是各组件自己写的 `style="@Style"`。全仓扫：`src/Components/*.razor` 里 **42 个组件有这一行**，
  `ElSelect.razor` **一处都没有**（`grep -n "Style" src/Components/ElSelect.razor` 空）。
  ⇒ 赋给 `ElSelect` 的 `Style` 被静默吞掉，不存在「优先级不够」的问题。
- 排除了两个误判方向（同一颗下拉，改前/改后各量一次盒宽）：
  - `flex-grow:0` ⇒ 794px（无效，宽度不是被 flex 拉大的主因）
  - `max-width:130px` ⇒ 130px（有效，但只是绕过，不是修法）
  - 根因是 EP 的 `.el-select` 宽度取自 `--el-select-width`（默认 100%），
    缺内联 `style` 就没有任何东西能盖住这条声明。

## 2. 改法

`src/Components/ElSelect.razor` 根 `<div class="@warpperClsBuilder…">` 补一行 `style="@Style"`，
与 `ElCascader.razor:4` 等同类组件同形。只加在根节点：内层 `.el-select__wrapper` 的高/圆角由 EP 自己管，
消费端写 `width` 的语义就是「这颗框多宽」，落在 `.el-select` 上最贴近 EP 的口径。

## 3. 验证

- 单测：`test/Element.ComponentTests/ElSelectTests.cs` 新增 `InlineStyleLandsOnRoot`
  —— 给 `Style="width:130px;"` 后断言 `.el-select` 的 `style` **属性值**逐字相等（不是计算宽度，bUnit 量不到）。
  `SelectHost` 测试宿主同步加 `Style` 参数透传。
- 浏览器（宿主 `zyweb` 款号编辑，1440×900，探针 `_tmp_probe/b51n_probe.mjs`，日志 `_tmp_probe/b51n.log`）：
  - 条码表页签「条码生成方式」：`root=250x32 inline="width:250px;"`
  - 生产指导书页签「工序分类」：`root=130x32 inline="width:130px;"`
  - 改前同一颗是 794px。

## 4. 边界与刻意留下的偏差

- `Style` 只影响内联样式，不与 `Width` 类参数争：`ElSelect` 没有宽度参数，宿主历来靠 `Style` 或外层 `ElCol` 定宽。
- 因为这条属性此前**从未生效**，修复后所有写过 `Style` 的宿主都会第一次真的按它排版。
  本仓（zyweb）全量 grep 只有上述两处写了非 100% 宽度，其余都是 `width:100%`（与 EP 默认等价，渲染不变）。
  对外部宿主这是一次「以前被忽略的声明现在开始生效」的行为变化，故记在批次㊻ 而不是当缺陷偷偷修。
