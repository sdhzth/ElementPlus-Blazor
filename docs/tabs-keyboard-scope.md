# ElTabs 键盘导航的作用范围（批次81）

参考：<https://element-plus.org/zh-CN/component/tabs>（无 `keyboard` 开关属性；键盘导航是 tablist 的内建行为）

## 结论

- **没有属性可以关掉 ElTabs 的方向键导航**——EP 的 tablist 键盘导航是语义内建行为，本库此前也没有开关。
- 批次81 修的是**作用范围**缺陷：`@onkeydown` 原先挂在 ElTabs **根 div** 上，Blazor 事件委托会把页签
  **内容区**里一切可聚焦元素冒泡上来的 `ArrowLeft` / `ArrowRight` / `Home` / `End` 全部吃掉——
  输入框里想移动光标，页签却被切走了。horizontal 模式下 `ArrowUp` / `ArrowDown` 在 `OnKeyDownAsync`
  里早退，不受影响。
- **改法**：`@onkeydown="OnKeyDownAsync"` 从根 div 挪到 `role="tablist"` 的 `.el-tabs__nav` div 上
  （`ElTabs.razor`）。键事件只有焦点位于**页签头**（页签项是 tablist 的子节点）才会冒泡到这里；
  pane 内容里的按键不再触发页签切换。`OnKeyDownAsync` 的逻辑本身一字未动。
- **EP parity 保留**：页签项是 roving tabindex（激活 0 / 其余 −1），`Tab` 键落到激活页签头上之后
  `←/→` 逐个移动、`Home` / `End` 到首尾、`Disabled` 跳过——全部照旧。
- **ElProTabsGrow 不受影响**：它的 keydown 处理一直挂在页签项自己身上，本来就是正确作用范围。

## 消费侧受益面

凡「对话框/页面里有 ElTabs 且页签内容含输入框」的宿主都被此缺陷波及（zyweb 的款号编辑、物料编辑等
多页签编辑对话框都在内），修复是库侧一处，无需消费端逐页改。

以款号管理（`/dictionary/styleinfo`）点「新增」对话框为例：15 个输入框在页签条**上方**的表单区
（ElTabs 的兄弟节点，冒泡不经过 ElTabs，本来就不会触发）；「预算成本/销售价格」pane 里的 16 个
DecNumberInput 在修复前按 `←/→/Home/End` 会真实切走页签，修复后只动光标。

## 验证

- **改前缺陷复现**：`zywebGroup/_tmp_probe/b82_probe2b_run.log`（8 PASS / 0 FAIL）——pane 内
  ArrowRight 切到下一页签、ArrowLeft 切回、Home 到第一个、End 到最后一个；上方表单区 ArrowRight
  不切（证明委托范围就是 ElTabs 根，不是整个页面）。
- **改后**：`zywebGroup/_tmp_probe/b82_probe3_run.log`（13 PASS / 0 FAIL）——
  pane 内四键全不切（P2–P5）；**正向对照**焦点放页签头上 ArrowRight→下一页签、Home→第一个、
  End→最后一个（PC1–PC3）；console error / 未捕获异常 = 0。
- **单测**：`test/Element.ComponentTests/ElNavigationTests.cs` 的
  `TabsSupportClosableEditableStretchBeforeLeaveAndKeyboard` 派发目标由 `.el-tabs`（旧挂点）改到
  `.el-tabs__nav`（新挂点），断言语义不变（ArrowRight 被 `BeforeLeaveSync` 挡下、End 到末签）；
  全套 **2707 / 0**。
- 重打包 `nuget/Element.0.10.0.nupkg`（md5 `b6517209e8be7806847a657a5f00948e`），消费端重建 0 错误。
