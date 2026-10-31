# 浮层图标/按钮重叠 专项（ElInputNumber / ElSelectV2 / 分页跳页框）

> 用户报告：`/basic` 的 `ElInputNumber` 右侧「看起来是两个按钮」。排查确认是**清除图标压在步进按钮上**，
> 并顺带查出 `ElSelectV2` 后缀图标并排、分页跳页框按钮双边框两个同类问题。

## 一、根因

三类问题的共同结构：**`position:absolute` 的浮层图标各自独立定位，没有任何容器负责给它们让位**。

| # | 位置 | 根因 |
|---|---|---|
| 1 | `ElInputNumber` 清除图标 | clear 定位 `right:10px`，步进按钮 `right:0; width:32px` —— 两者矩形相交。命中测试显示 `x ∈ [右-24, 右-12]` 命中的是 `i.el-input__clear.el-icon-circle-close`，即**圆圈叉压在 + 按钮上**，叠成「⊕」，被看成「两个按钮」 |
| 2 | `ElInputNumber` 步进按钮 | 按钮 `border: var(--el-border)` 四边全画 + `top:0; height:100%`，与 `.el-input__inner` 自身边框完全重合 ⇒ 像「外挂在输入框边上的独立按钮」 |
| 3 | `ElSelectV2` 后缀 | clear `right:30px` 且**常显**，与箭头 `right:5px`（宽 25px）**零间距并排**（实测 clear `1330..1355` / caret `1355..1380`）。EP 语义是同一位置的互斥切换 |
| 4 | 分页跳页框 | `.el-pagination__jump ... .el-input-number__increase/decrease` 写死 `border: 1px solid` 四边全画 → 与容器边框叠成「框中有框」 |

## 二、修法（均在 `src/Components/wwwroot/css/index.css`）

**1) 步进按钮内缩 1px，只画内侧分隔线**（对齐 EP `input-number.scss`）

```css
.el-input-number__decrease,
.el-input-number__increase { bottom: 1px; top: 1px; }   /* 去掉 height:100%、top:0 */
.el-input-number__decrease { border-right: 1px solid var(--el-border-color); left: 1px; }
.el-input-number__increase { border-left: 1px solid var(--el-border-color); right: 1px; }
/* is-controls-right：increase 追加 border-bottom，right:1px */
```

并补 EP 的悬停联动：`.el-input-number__increase:hover:not(.is-disabled) ~ .el-input .el-input__inner:not(.is-disabled) { border-color: var(--el-color-primary); }`

**2) 清除图标让位**

```css
.el-input-number .el-input__clear { right: 42px; z-index: 2; }        /* 32px 按钮 + 10px 间隙 */
.el-input-number.is-without-controls .el-input__clear { right: 10px; } /* 无按钮时回 EP 默认 */
```

**3) `ElSelectV2` 后缀图标「同位互斥」**（EP `showClose = clearable && !disabled && inputHovering && hasModelValue`）
用**纯 CSS** 实现，DOM 不动 ⇒ bUnit 断言（`cut.Find(".el-select-v2__clear")`）不受影响：

```css
.el-select-v2__clear { right: 5px; display: none; }              /* 与箭头同位 */
.el-select-v2:hover .el-select-v2__clear,
.el-select-v2:focus-within .el-select-v2__clear { display: block; }
.el-select-v2:hover .el-select__caret:not(.el-icon-loading),
.el-select-v2:focus-within .el-select__caret:not(.el-icon-loading) { display: none; }
```

`:focus-within` 比 EP 多覆盖键盘用户；`loading` 图标不参与切换。

**4) 分页跳页框**：删掉写死的 `border: 1px solid`，分隔线交回通用 `is-controls-right` 段（左 + 下各 1px）。

## 三、取证（Playwright，全部实测）

| 探针 | 断言 | 结果 |
|---|---|---|
| `demo/probe-inputnumber.cjs` | 按钮内缩/高度/单边边框/clear 不相交/中心点命中 | **8/8** （`inc.right=490.00=root.right-1`，`inc.h=30.00`，边框 `0/0/0/1`，clear∩increase = `-9px`） |
| `demo/probe-selectv2-clear.cjs` | 静止只见箭头、hover 只见叉、叉与箭头同位、点叉可清空 | **6/6**（hover `clear.cx=1367.5` = 静止 `caret.cx=1367.5`） |
| `demo/probe-pagination-jump.cjs` | 跳页框按钮 `border-left+bottom`、右侧无边框、20×50% | 见探针输出 |
| `demo/probe-icon-overlap.cjs` | 全站 28 页两两矩形相交体检 | 修前 2 页命中 → **修后 0/28** |

复用场景按钮几何（`demo/probe-inputnumber-contexts.cjs` 实拍 + 测量）：
Slider 带输入框 `32×30` 仅 `border-right`；右侧竖直步进 `32×16` 为 `border-bottom+left`；分页跳页框 `20×14` 为 `border-left+bottom`。

bUnit：`~SelectV2|~InputNumber` **28/28 通过**（本轮仅改 CSS，未触碰 C#/razor）。

## 四、回归手法

`demo/probe-icon-overlap.cjs` 是通用体检器：把新组件的浮层图标 class 加进脚本顶部 `CLS` 数组即可复用。
判定规则：两元素矩形相交面积 > 2px² 且无祖先-后代关系 ⇒ 报告。`display:none` 的元素没有布局盒（rect 全 0），
比较位置时要用其**可见状态**下的坐标（本专项踩过：拿 `display:none` 的 caret 比中心 x 恒为 0）。

## 五、后续（2026-10-03）：清除图标改为「悬停才显示」+ 只读属性

用户反馈：`/basic` 的清除图标「平时不该显示，鼠标放上去才显示」。

- **组件本身不需要改**：`ShouldShowClearIcon => Clearable && !IsDisabled && !Readonly && Value.HasValue && (ShowClearIcon || isHovering)` 本就是「默认悬停（`isHovering`）才出」，`ShowClearIcon` 才是「常显」开关（bUnit `ClearableWithoutShowClearIconRendersOnlyOnHover` 早已锁定该语义）。此前 `/basic` 的用例写了 `ShowClearIcon="true"`，才导致圆圈叉常驻。
- **实际改动**：`demo/Element.Blazor.Demo/Pages/BasicDemo.razor` 的 `#c4-num` 去掉 `ShowClearIcon="true"`（保留 `Clearable="true"`），并补一个 `Readonly="true"` 实例（`#c4-num-ro`）作为只读活样例。
- **`ElInputNumber` 有只读属性**：`Readonly`（`bool`，默认 `false`），对齐 EP 官方 `readonly`（2.2.16，「原生 readonly 属性，是否只读」）。为 `true` 时：input 打 `readonly` + `aria-readonly="true"`；± 步进按钮加 `is-disabled`；键盘步进（`OnKeyDownAsync`）/ 点击步进（`StepValueAsync`）/ 清除（`ClearAsync`）全部拦截；`Clearable` 也不再渲染清除图标。
- **取证**：`demo/probe-inputnumber-clear-hover.cjs` **10/10 通过** —— 静止 `#c4-num` 内 `.el-input__clear` 数量 `0` → 悬停 `1` 且可见 → 移开回 `0` → 悬停点击叉后值 `"5" → ""` 且图标消失；只读实例 `readonly` 属性存在、`aria-readonly="true"`、± 按钮均 `is-disabled`、`ArrowUp` + 键入 `99` 后值仍为 `33`、悬停亦不出清除图标；JS 异常 0。

## 六、遗留

- `ElScrollbar` 细滚动条命中区实测仅 **1px**（历史「4px 细条 + 悬停显形」设计，thumb 的 20px 宽被 `overflow:hidden` 裁掉）。
  行为已修（滚轮接管），但存在「难点中」体验问题；调整为 EP 的 6px 会牵动 select/tree 等复用弹层视觉，**待用户确认**。
