# ElButton.Visible 与 ElSelect 多选首屏回显（批次㊷）

这一批解决两件同源的事：工具条「按钮组里单颗按钮要能拿掉」，以及数字字典的多选下拉
首屏把汉字显示成 `0 / 1 / 2`。两件都来自宿主 `zyweb`「生产订单」查询条那一次改版。

## 一、`ElButton.Visible`：隐藏 ≠ 禁用

### 问题

`zyweb` 的六颗工具条按钮要收进 `ElButtonGroup`，同时提出：

> 按钮组是不是有属性设置某个按钮 可以点击 或隐藏的属性，没有的话，修改组件。

核查现有 API：

| 需求 | 有没有 | 走哪条 |
| --- | --- | --- |
| 某颗按钮不可点击 | **有** | `ElButton.Disabled`；`ElButtonGroup.Disabled` 会级联到组内每一颗 |
| 某颗按钮不出现 | **没有** | 此前只能消费侧写 `@if`，或者外面套一层带 `display:none` 的容器 |

`ElButtonGroup` 本身不管可见性——它只负责把子按钮粘成一段圆角连体，并向下级联 `Disabled`。
所以「组内单颗隐藏」这个能力只能加在 `ElButton` 上。

### 修法

```csharp
/// 本仓 Grow 口径（Element Plus 没有这个属性，官方用父级 v-if）：
/// false 时整颗按钮不渲染（连 DOM 都不留），用于权限/状态驱动的工具条。
/// 与 Disabled 是两件事——后者是「看得见但点不动」（会级联按钮组的禁用态），
/// 前者是「此刻不存在」。默认 true，加参数前的行为逐字节不变。
[Parameter] public bool Visible { get; set; } = true;
```

`ElButton.razor` 的整段正文包进 `@if (Visible)`。这里刻意**不是** `display:none`：

- `display:none` 留着 DOM ⇒ 权限驱动的按钮仍然在可访问性树里能被读到，也仍然能被脚本点；
- 不渲染 ⇒ 与「用户没这个权限」的语义一致，且和消费侧 `@if` 等价，只是不用每页各写一份。

`ElButton` 的渲染树是手写的、每个帧位有固定槽位号（原因见 `ElButtonRenderTreeDiffTests` 的注释头：
可选帧若用 `seq++` 连续分配，一旦某帧增减，其后所有帧整体平移，末帧的
`AddElementReferenceCapture` 会与旧树同序列的异类型帧相撞，抛
`Unexpected frame type during RemoveOldFrame: ElementReferenceCapture`）。
`Visible` 是「整棵子树进出」，正是这套槽位编号要保住的那个场景，所以有一条往返用例专门盯着它。

## 二、`ElSelect`：首屏不回显的另一半

### 真身

`ElOption` 挂在弹层里，而弹层内容**只在第一次点开时**才交给宿主
（`ToggleDropDownAsync` 里才把 `ChildContent` 塞进 `DropDownOption`）。所以首屏：

```
Options.Count == 0
```

单选此前已经补过：`ResolveLabelFromUnmountedOptions()` 把 `ChildContent` 的帧序列**只读**扫一遍，
取 `Value` 匹配那条的 `Text`。多选走的是另一条路 `SyncSelectedOptionsFromValues()`，
那里拿不到选项文本时回落到 `Convert.ToString(value)` —— 于是：

```razor
<ElSelect TValue="int" Multiple="true" @bind-Values="find_scstatus">
    @foreach (var x in dsscstatus)
    {
        <ElOption TValue="int" Value="@x.fcode" Text="@x.u_name" />
    }
</ElSelect>
```

首屏胶囊显示成 `0 × 1 × 2 ×`，点开下拉一次才变成 `领料计划 × 开裁 × 生产中 ×`。
Element Plus 的下拉是 `v-show`，选项一挂载就注册，所以原生没有这个问题；
本库为了弹层隔离与首屏开销不能改成「首屏就挂载弹层」（那会让全站每个下拉提前建出一整份候选项 DOM）。

### 修法：把扫描器抽成共用的遍历器

```
WalkOptionFragment(fragment, depth, onOption)   // 只读 build 进临时 RenderTreeBuilder
  └─ WalkFrames(array, start, end, depth, onOption)
       ├─ Region / Element        → 在数组内递归
       ├─ ElOption 组件            → onOption(Value, Text ?? Convert.ToString(Value))
       └─ 其他组件                 → 从它的 ChildContent 属性再钻进去
```

- 深度上限 `OptionScanMaxDepth = 4`；
- 整段 build 包在 `try/catch` 里：`ChildContent` 本身不可 build（畸形片段）时不抛，退化成改动前的原始值回落；
- 两条消费路径：`ResolveLabelFromUnmountedOptions`（单选，前提判断一字未改）与
  `CollectUnmountedOptionTexts`（多选，扫成「值 → 文本」的表，同值重复以先出现者为准，
  与弹层里第一个命中的选项口径一致）。

`SyncSelectedOptionsFromValues` 的取值优先级：

```
已注册 Options  →  dict（Options 数据源）  →  扫描结果  →  Convert.ToString(value)
```

三点约束：

1. **整个循环最多扫一次**（`unmountedTextsResolved` 门闩），不在每个值上重复 build 片段；
2. **`Options` 非空时扫描函数直接返回 `null`** —— 点开过下拉之后一律以真注册的选项为准，
   扫描只是首屏那一帧的补位，热路径不付这笔钱；
3. **不做缓存**。扫描结果的生命周期与「候选项未注册」这个前提一样短；缓存会活过
   父级重新生成 `@foreach` 数据源的那一刻，继续吐旧文本。

## 三、验证

- 新增 `test/Element.ComponentTests/ElButtonVisibleTests.cs`（6 例）：默认 `true` 的兼容、
  `Visible=false` ⇒ `Assert.Empty(cut.Nodes)`、隐藏那颗点不动、true→false→true 往返仍可 diff 且仍可点、
  组内中间那颗隐藏后剩两颗且组壳完好、`Visible=false` 与 `Disabled` 的区别。
- `ElSelectInitialLabelTests.cs` 多选 6 例：首屏胶囊是选项文本（且触发框不串显）、未知值**逐值**回落
  而非整组、选项嵌在分组/`@foreach` 区域里也找得到、不可 build 的 `ChildContent` 不崩、
  已注册选项优先于扫描（只判组件态 `Label`/`SelectedOptions`）、无候选项时保持原始值回落。
- 过滤跑 20/20；全量 **2653 / 0 / 0**（基线 2642 + 11）。
- 翻面验牙：拆掉 `@if (Visible)` ＋ 让 `CollectUnmountedOptionTexts` 恒返回 `null` ⇒ **失败 8 / 通过 12**，
  红的正是 5 颗按钮 + 3 条多选文本用例；三条对照仍绿。多选那两条的失败读数就是改动前的症状：
  `String: "beta" / Not found: "Beta"`。

## 四、消费侧口径（zyweb「生产订单」）

字段宽度是**定值**，所以「胶囊一行放下、文字完整」与浏览器宽窄无关；窄屏下换行的是整行字段，
不是字段里的胶囊。生产状态那一格实测：胶囊层宽 = 选择框宽 − 28，选择框宽 = 字段宽 − 24，
三颗中文胶囊（82 + 58 + 70 + 缝）需要约 258px ⇒ 270px 的字段仍会把第三颗挤到第二行，
定到 **320px** 才一行放下。

## 五、已知边界（本批未修）

1. `Visible` 是本仓 Grow 口径，EP 没有同名属性，`ElButtonGroup` 也没有「组内单颗隐藏」的概念；
2. 扫描器只认声明式 `ElOption` 子项。用 `ElSelectTreeGrow` / `ElSelectV2` 那种非 `ElOption` 结构的
   仍然回落到原始值；嵌得比 4 层更深的候选项同样扫不到（是回落，不是崩）；
3. **多选胶囊换行时的溢出**：`.el-select__tags` 是绝对定位的 `flex-wrap` 层，两行胶囊（60px）
   会在 32px 的选择框里上下各溢出约 14px。本批只在消费侧靠定宽规避，库侧的溢出容器口径留给下一批。
