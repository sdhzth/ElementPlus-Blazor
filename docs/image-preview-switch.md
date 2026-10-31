# ElImage.Preview：图片点击放大的开关

批次㊳（2026-10-14）。由宿主 `zyweb` 提出：顶栏头像「点击这个图片不要放大，如果组件没有这个属性，加上；」。

---

## 1. 为什么本库需要一个官方没有的开关

Element Plus 的 `el-image` 用「有没有给 `preview-src-list`」来表达能否放大：不给列表就没有预览。
本库在此基础上多做了一步回落——`PreviewSrcList` 为空时，预览图源回落到 `Src` 本身：

```csharp
: string.IsNullOrWhiteSpace(Src) ? new List<string>() : new List<string> { Src };
```

这一步是有意的（单图想放大时不必再重复传一遍 `SrcList`），但代价是**凡是带 `Src` 的图都可点放大**，
且没有任何入口能关掉它。头像、logo、表格单元格缩略图这类「嵌在别的交互里」的图就撞上了：
zyweb 的头像挂在 `ElDropdown TriggerMode="Click"` 的 `Trigger` 内，点开本来要出账号菜单，
结果查看器先把菜单盖住了。

| 口径 | Element Plus | 本库（加开关前） | 本库（加开关后） |
| --- | --- | --- | --- |
| 能否放大 | 由 `preview-src-list` 是否给出决定 | 恒可（回落 `Src`） | 默认仍可，`Preview="false"` 关闭 |
| 单图放大的写法 | 必须显式传 list | 只传 `Src` 就行 | 只传 `Src` 就行（未变） |
| 默认值 | — | — | `true`，既有页面零行为变化 |

---

## 2. 属性

```csharp
/// <summary>
/// 能否点击放大预览，默认 <c>true</c>。……关掉只影响查看器，<see cref="OnClick"/> 照常回调。
/// </summary>
[Parameter]
public bool Preview { get; set; } = true;
```

**真相只放在一处**：开关落在 `PreviewList` 的源头，`!Preview` 直接返回空列表。

```csharp
protected IReadOnlyList<string> PreviewList => !Preview
    ? new List<string>()
    : PreviewSrcList != null && PreviewSrcList.Any()
        ? PreviewSrcList.Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
        : string.IsNullOrWhiteSpace(Src) ? new List<string>() : new List<string> { Src };
```

三个下游全部以它为准，不再有第二处判断：

| 下游 | 位置 | 效果 |
| --- | --- | --- |
| `ShowViewer => showViewer && PreviewList.Any()` | `ElImage.razor.cs` | 查看器即使 `showViewer` 被置真也渲染不出来 |
| `OpenPreviewAsync` 的 `if (!PreviewList.Any()) return;` | 同上 | 点击早退，不做缩放/平移复位与 teleport |
| `@if (ShowViewer)` 整块 markup | `ElImage.razor` | DOM 里根本不存在 `.el-image-viewer__wrapper` |

为什么不写成 `if (!Preview) return;`：那样会有「参数」和「图源」两处口径，将来加 `PreviewSrcList`
动态切换、或给查看器加开关时，两处很容易只改一处。放在源头置空，`PreviewList.Count` 就只有一个含义。

**`OnClick` 不受影响**：`OpenPreviewAsync` 先回调 `OnClick` 再早退，所以「点了图要做别的事」的宿主
（头像那种）关掉放大也照样拿到点击。

---

## 3. 用法

```razor
@* 默认：可点放大 *@
<ElImage Src="/images/photo.jpg" Style="width:200px;height:150px" />

@* 嵌在触发器/按钮里：关掉放大 *@
<ElDropdown TriggerMode="DropdownTrigger.Click">
    <Trigger>
        <ElImage Src="@headimg" Width="32px" Height="32px" Preview="false" />
    </Trigger>
    <ElDropdownMenu>…</ElDropdownMenu>
</ElDropdown>
```

`Preview="false"` 压得住显式传入的 `PreviewSrcList`（开关优先于图源），不需要调用方同时清空列表。

---

## 4. 验证

`test/Element.ComponentTests/ElImageTests.cs`，本批新增 **4** 例（该类 1 例 → 5 例，基线全量 2626 → 2630）：

| 用例 | 断言 | 角色 |
| --- | --- | --- |
| `PreviewFalse_DoesNotOpenViewerOnClick` | 点击后 DOM 里没有 `.el-image-viewer__canvas` | 主断言 |
| `PreviewFalse_WinsOverExplicitPreviewSrcList` | 显式给了 list 仍不打开 | 开关优先级 |
| `PreviewFalse_StillInvokesOnClick` | `OnClick` 回调计数 +1 | 边界：不误伤点击事件 |
| `PreviewDefault_StillOpensViewerFromSrcFallback` | 不写 `Preview` 时点 `Src` 能打开 | **正向对照**：证明默认值没被改动带偏 |
| （既有 `InitialIndexSelectsStartingPreviewImage`） | 多源预览的初始索引 | 回归，未动 |

先红后绿：红轮 失败 2 / 通过 3（`Assert.Empty() Failure: Collection was not empty`，两条 `PreviewFalse_*` 都拿到真实渲染出的查看器 `div`），
补上源头置空后 5/5；全量 2630/0/0。日志 `Temp/verify_batch34_image_red.log`、`Temp/verify_batch34_image_green.log`、`Temp/verify_batch34_fullsuite.log`。

消费侧浏览器实跑（zyweb + headless chromium 1280×720，CDP 真实 `Input.dispatchMouseEvent`，同一页面正反对照）：
点头像 → `viewer:0` 且 `.el-dropdown-menu` 正常展开（密码修改 / 安全退出）；点品牌 logo（默认 `Preview`）→
`viewer:1 / mask:1 / src images/72.png`，关闭后 `viewer:0`。

---

## 5. 已知边界

- `docs/组件使用说明.md` 的 ElImage 示例行写着 `ShowViewer="true"`，而 `ShowViewer` 是受保护的计算属性、
  不是公共 `[Parameter]`——既有文档缺陷，本批只报告未改。参数表已补 `Preview` 行。
- 开关是渲染期判断，不做「已打开查看器后再置 `false`」的状态回收：那种改法要传参回来看已开的查看器，
  调用面不清晰。要动态切就整块条件渲染 `ElImage`。
