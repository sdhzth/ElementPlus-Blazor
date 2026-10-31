# 表头单元格的文字位置、图标盒加量与字典样式（2026-10-09 批次⑦）

> 触发点是用户三条实测反馈（附 3 张截图）：
> 「把供应商里面的山东、青岛都改成不存在的东西，联系人都改成英文，整个 demo 里面的都这样改；
> 滚轮只有移动到这里才能上下移动；列的表头没撑开；」
> 其中「表头没撑开」我第一版理解成行高问题，被用户纠正两次，最终口径是：
> - 「**不是行高没撑开，这种的话，应该宽度自适应**」
> - 「**如果设定了列宽（可以设置列宽吗），再设定了排序或过滤，这 2 个是单独算的**」
>   ⇒ 列的 `Width` 是给**文字**的房间；排序箭头盒与筛选漏斗盒**算在这条宽度之外**。
>   （顺带回答用户的问题：能设列宽，`ElTableColumn.Width` / `MinWidth` 就是，类型是 `string`。）
>
> 本篇所有数字都是 `http://127.0.0.1:5199` 上实抓的（Edge headless + Playwright），
> EP 侧结论来自 tag `2.14.0` 源码直取（⚠️ raw URL 的 ref **不带** `v` 前缀，`v2.14.0` 是 404）。
> 日志只落盘在 `Temp/`（已 gitignore），本文按路径引用，不进版本库。

四件事机制互不相干，各自独立小节：§1 表头结构、§2 列宽口径（本批主体）、§3 面板滚轮链、§4 字典样式，
§5 是演示数据去真实化，§6 记已知偏差，§7 给复现方法与全部读数。

---

## 1. 表头 `.cell` 的文字必须是直接子节点，`caret-wrapper` 里只放两个箭头

改前的写法把 label 放进了 `.caret-wrapper` 内部，并且按 `Sortable` 分了两条分支各写一次 `@header.Text`。
EP 2.14.0 不是这样的（`packages/components/table/src/table-header/index.ts:260-295`，本地副本 `Temp/ep214_table_header_index.ts`）：
`.cell` 的子项**先是** `column.renderHeader || column.label`（直接文本），**随后**才是
`button.caret-wrapper`，而这个盒里只有两个 `i.sort-caret`，一个文字都没有。

这不是审美问题，是几何问题：库 CSS 给 `.caret-wrapper` 的是 `width:24px` + `flex-direction:column`
（`Temp/ep214_table.scss:515-526`，压缩块写 `height:14px`，本站当时实测 **24×34**，见 §6 第 2 条 —— 盒高已由批次⑧改成 EP 的 14）。
文字塞进这个 24px 宽的列向 flex 里，就被挤成"一字一行"，再被 `.cell{overflow:hidden}` 裁掉 ——
这正是用户截图上"表头没撑开"的形态。

改后（`src/Components/ElTable.razor:126`）：`@header.Text` 提到 `@if (header.Sortable)` 之前，两条分支合并成一条，
:127-133 的 `caret-wrapper` 只剩两个 `.sort-caret`。

实测（`Temp/verify-batch7.log` H1/H2/H3，视口 1440×900）：

| 列 | `caret-wrapper` 文字 | 命中盒 | `.cell` 裁没裁 |
| --- | --- | --- | --- |
| 单价(元) | `""` | 24×34，2 个 caret | `scrollH 34 / clientH 34` —— 没裁 |
| 库存（可排序＋条件筛选） | `""` | 24×34，2 个 caret | `scrollH 34 / clientH 34` —— 没裁 |

H4 是**翻面变异**（探针自己把文字塞回 `caret-wrapper`）：`scrollH 138 / clientH 34` 立刻红，
还原后回到 `34/34`。改前的形态由变异复现，不靠记忆。

> ⚠️ 上表的 **24×34 是批次⑦当时的读数**，不是现在的值：批次⑧把箭头盒按 EP 2.14.0 改成了 **24×14**
> （34px 的遗留盒会把可排序行顶高到 51px），`verify-batch7.cjs` 的 H2 口径也跟着改成 24×14。
> 见 `docs/caret-scrollbar-focus-ring-alignment.md` §1。§6 第 2 条那条"已知偏差"已由批次⑧消掉。

---

## 2. 列宽口径：`<col width>` = 声明宽 + 表头图标盒（排序 24 / 筛选 18 / 都有 42）

### 2.1 为什么要"另算"

本仓的列宽由 `<col width>` 直接钉死（`table-layout:fixed`），**没有 EP 那套 realWidth 布局引擎**。
EP 把 caret 盒和漏斗排进 `.cell` 的换行流里，自己会重算；本仓若把图标算在声明宽度**里面**，
留给文字的净宽就少 24/18/42px —— 演示页 `Width="110"` 的「单价(元)」带排序时只剩
`110 − 20(pad) − 24(caret) = 66px`（N4 翻面实测 `avail 65`），表头文字必然被 `.cell{overflow:hidden}` 裁。
用户的口径「这 2 个是单独算的」正是这个加量规则。

### 2.2 实现

`src/Components/ElTable.razor.cs:991-1075`：

```csharp
private const int SortIconBoxWidth = 24;    // .caret-wrapper 命中盒
private const int FilterIconBoxWidth = 18;  // 漏斗图标 12px + 本库 margin-left:6px

ResolveColumnWidth(header) =
    ResolveBaseColumnWidth(header)            // 显式 Width > MinWidth > 兜底 80
  + HeaderIconAllowance(header)               // Sortable ? 24 : 0  +  HasFilterTrigger ? 18 : 0
```

三条必须记住的边界：

1. **百分比列原样透传**（`"30%"` 还是 `"30%"`）。`width` 属性写不了 `calc()`，把 `%` 当数字加 18 会变成
   固定像素，语义直接反了。
2. **表头与表体两张 table 走同一个方法**。`ElTable.razor` 的两个 `<colgroup>`（:93 表头、:177 表体）
   都调 `ResolveColumnWidth`，否则列会错位（N1 就是这条的判据）。
3. **渲染漏斗与给漏斗留宽度共用一个判据**：`ElTable.razor` 的 `@if (HasFilterTrigger(header))` 与列宽的
   `HeaderIconAllowance` 都读 `ElTable.razor.cs:1050` 这同一个 `internal static bool HasFilterTrigger`
   （`AdvancedFilter || Filters?.Any()`）。分成两处写就会再次出现"渲染了漏斗却没留宽度"。

加量同时进入求和：`ResolveColumnWidthAsInt`（:1077）也读 `ResolveColumnWidth`，于是
`ColumnsTotalWidth` → `TableWidthStyle`（`width:100%;width:max(100%,{total}px)`）一起变大。
**结果是宁可横向滚动，也不把列压小** —— 与 EP 一致，也是 §1 那条 `.cell` 不被裁的前提。

### 2.3 实测读数（N 段，`Temp/verify-batch7.log`）

| 列 | 声明 | 实测图标盒 | `<col width>` | th 实测 | `.cell` pad | 文字净可用 | 需要 | `.cell` sh/h |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| ID | 70 | 0 | 70 | 70 | 20 | 49 | ≥48 | 23/23 |
| 名称（条件筛选） | 220 | filter18 | 238 | 238 | 20 | 199 | ≥198 | 23/23 |
| 类别（带筛选） | 160 | filter18 | 178 | 178 | 20 | 139 | ≥138 | 23/23 |
| 单价(元) | 110 | caret24 | 134 | 134 | 20 | 89 | ≥88 | 34/34 |
| 库存 | 180 | caret24+filter18=42 | 222 | 222 | 20 | 160 | ≥158 | 57/57 |

（「库存」那一列 `textH` 量到 16，但 `.cell` 高 57 —— 表头文字在 222px 里合法换成了两行；
「单价(元)」的 34 是 34px 高的 caret 盒顶出来的，不是两行文字。两者都**不是裁切**。
所以判据只要求"没被裁"（`sh <= ch + 1`）。第一版 N3 还额外写了"表头文字必须单行"的期望，
那是我自己造的假红 —— 单行在 EP 里从来不是契约（`.cell` 默认就是 `white-space:normal` 换行到完整显示），
已从判据里删掉。）

> 批次⑧把 caret 盒高改成 EP 的 14 之后复跑，这张表的宽度读数**一列没变**（`col 70/238/178/134/222`、
> `avail 49/199/139/89/160`，加量只取宽度 24 与盒高无关），变的是「单价(元)」的 `.cell` 从 `34/34` 变 `23/23`、
> 「库存」从 `57/57` 变 `46/46`（`Temp/verify-batch7-after-b8-final.log` 的 N3 行）。
> 那批 22 项复跑 **22 PASS / 0 FAIL / 0 SKIP**、`CONSOLE_ERRORS=[]`。

N4 翻面：把每列 `<col width>` 收回成声明值后，四个带图标的列**逐列翻红**
（`名称 181<198`、`类别 121<138`、`单价 65<88`、`库存 118<158`），N5 还原后数字与改前逐列相同
（`col 238/178/134/222`，`avail 199/139/89/160`）—— 证明翻面动的是 col，不是页面状态。

### 2.4 ⚠️ 量列宽必须在 `tableW == Σcol` 的条件下

宽视口（1440×900）下这条判据会**空转**：`diag-batch7-colwidth.cjs` 实测
`table-layout` 已是 `fixed`，但 `tableW=1074` 而 `Σcol=842`，多出的 232px 被 fixed 布局按比例摊给每一列
（`col 134 → th 170/171`、`col 222 → th 282/283`）。摊完之后**声明宽和加量宽都撑得开**，
把加量去掉照样绿 —— 探针有牙，但咬不到东西。

所以 N 段单独开一页 **800×700**，前置 N0 断言 `layout=fixed && |tableW − Σcol| <= 4`，实测
`tableW=842 sumCol=842`（`Σ` 是 70+238+178+134+222，正好卡住），`<col width>` 才真的绑住列宽。
N4 翻面时还要把表头 table 的 style 钉成 `width:740px`（= Σ 声明值），否则收回加量后那点富余又会被摊回来。

**通用结论**：几何断言要先找"绑定条件"（这里是被摊薄到 0 的富余），否则翻面断言恒绿、等于没写。

---

## 3. `ElDropDownTableGrow` 面板内滚轮：删掉内层那条 `overscroll-behavior: contain`

用户说的「滚轮只有移动到这里才能上下移动」= 只有面板右侧那条 ~8px 的 padding 带滚得动，落在行上一动不动。

真因在 `src/Components/wwwroot/css/index.css`（改前 :5455-5458）：

```css
.el-dropdown-table-grow__panel .el-table__body-wrapper { overscroll-behavior: contain; }
```

这个组件的 `.el-table__body-wrapper` 为了预留滚条槽（列对齐要它，见 `ElDropDownTableGrow.razor` 的 `<style>`）
被强制 `overflow-y:scroll` —— **即使数据没溢出它也是个滚动容器**。再挂 `contain` 就把滚轮吞在这里、
不再上抛给面板本体，于是滚动链断了。边界防穿透本来就由面板自己的 `contain` 承担，内层这条是多余的。

修复＝删掉这条规则（CSS 里留了注释说明别再写回去）。

实测（W 段，`Temp/verify-batch7.log`，面板出现在 /table）：

| 判据 | 读数 |
| --- | --- |
| W0 前置：真有溢出量程 | `panelRange 16`、`innerRange 43`、`innerOvs=auto`、`panelOvs=contain`；面板 `left 684 / top 8 / w 334 / h 354` |
| W1 行上第一格滚轮 | 内层 `inner 0 → 43`（走满 43 量程） |
| W2 内层到底再滚 | 面板接手 `panel 0 → 16`（走满 16 量程），`inner 43` |
| W3 两处都到底继续滚 | `.demo-main` `115 → 115` 不动 —— 防穿透仍成立 |
| W4 翻面（加回内层 contain） | 第一格 `inner 0→43`；**第二格 `panel 0→0`（应为 0）**；窄条 `panel 0→16` —— 复现用户症状 |

---

## 4. 行/单元格样式字典被 `ToString()` 渲染成垃圾 `style`（自查出的第 4 条）

Blazor 的 `<c style="@dict">` 不会把字典序列化成 CSS，而是把 `Dictionary.ToString()` 原样写进 DOM。
实测 `th` 的 style 值是 `System.Collections.Generic.Dictionary`2[System.String,System.String]` ——
浏览器把整条声明当非法 CSS 丢掉，于是按字典传的 `RowStyle` / `HeaderRowStyle` / `HeaderCellStyle` /
`CellStyle` **全部不生效**，还把垃圾串留在属性里。

修复：`ElTable.razor.cs:2970` 加 `internal static string StyleToString(Dictionary<string,string>)`，
用 `HtmlPropertyBuilder.CreateCssStyleBuilder()` 序列化，空字典/全空项返回 `null`（Blazor 便不渲染该属性）；
`ElTable.razor` 的 5 处 `style="@..."`（表头行 :101、表头单元 :117、表体行 :212、表体单元 :252 与 :287）
统一包一层 `StyleToString(...)`。

实测（S 段）：S1 `attr=box-shadow:inset 0 -3px 0 #409eff` → computed `rgb(64, 158, 255) 0px -3px 0px 0px inset`（真的生效，不是只渲染出字符串）；
S2 `background:#fff7e6;color:#d9480f` 落到「名称」那一列的 `th`；
S3 整页含 `System.Collections` 的元素数 **0**；
S4 翻面换成旧垃圾串后 computed 变 `none`，还原后阴影回来了。

---

## 5. 演示数据去真实化：供应商全换虚构名、联系人全换英文

用户要求「整个 demo 里面的都这样改」。改的是数据源与散落在各页的字面量，共 11 个文件
（`demo/Element.Blazor.Demo/Models/MockData.cs` + 10 个 `Pages/*.razor`）。

| 编码 | 旧供应商（真实地名/企业名） | 新供应商（虚构） | 旧联系人 | 新联系人 |
| --- | --- | --- | --- | --- |
| S01 | 国药控股青岛分公司 | 云溪医药控股 | 张伟 | Kevin Carter |
| S02 | 华润医药（山东） | 北屿医药供应链 | 李娜 | Laura Bennett |
| S03 | 上药控股某某站 | 星澜医药站 | 王芳 | Fiona Hayes |
| S04 | 九州通医药物流 | 青梧医药物流 | 刘强 | Victor Adams |
| S05 | 青岛本地器械耗材 | 澜溪器械耗材 | 赵敏 | Nina Foster |

`Patients` 里同样复用的四个中文名（张伟/李娜/王芳/刘强）一并换成上表的英文名；
`docs/组件使用说明.md:2403` 的示例文字同步。

实测（D 段）：D1 面板可见行就是新名 + 英文联系人；D2 `/table` 全文查旧名 `命中=[]`；D3 `/basic` 同样 `命中=[]`。
（这两条是**负向断言**，都配了正向对照 —— D1 证明页面里确实有数据可查，否则 `命中=[]` 是空转。）

**改的范围与刻意留下的边界**：全仓 `grep` 复查旧供应商名与五个旧联系人姓名，`demo/` `src/` `docs/组件使用说明.md`
命中 **0**（`demo/probe-table-editors-result.txt` 是历史探针输出快照，不参与渲染，按"历史证据不改写"留着）。
医院名本来就是占位写法「某某市人民医院」，不在要改之列。
当时唯一还留着真实城市名的地方是 `Pages/SelectDemo.razor:114` 的 `_cities`
（`北京/上海/广州/深圳/杭州/成都/青岛/武汉/西安/南京`）—— 那是 `ElSelect` 的**通用城市选项演示**，不是供应商或联系人，
按用户原话的字面对象（供应商 + 联系人）当时没有改它，只如实记在这里、不悄悄做半程。
**✅ 批次⑧按用户明确要求做完了**：十个城市换成虚构名（云州/岚川/星澜/青梧/澜溪/沐野/栖迟/海若/望舒/榕溪），
`grep` 旧城市名与 `山东` 在 `demo/Element.Blazor.Demo/{Pages,Models}` 命中 **0**，见
`docs/caret-scrollbar-focus-ring-alignment.md` §4。

---

## 6. 已知偏差（对齐 EP 时有意保留或尚未统一，别当新 bug 查）

1. **`.cell` 内边距**：EP 2.14.0 `table.scss:195` 写 `padding:0 12px`（左右 24px），本库走
   `var(--el-table-cell-padding-x)`，实测 **pad=20**（左右各 10）。N3 的 `avail`/`need` 都按实测 20 算，
   没有硬编 EP 的 24 —— 若要改成 24，本批全部宽度判据要一起重新量。
2. ~~**`.caret-wrapper` 高度**：EP 源码块写 `height:14px`，本仓打包的压缩 CSS 写 `height:34px`，
   实测命中盒 **24×34**（H2）。列宽加量只取宽度 24，与高度无关，所以这条不影响 §2 的数字。~~
   **✅ 批次⑧已消掉**：本仓段落补上 EP 的几何（盒 `24×14`、三角形 `absolute + left:7px`、`top:-5px`/`bottom:-3px`、
   删掉手写 `margin-left`），可排序表头行从 51px 回到与其他行相同的 40px，见
   `docs/caret-scrollbar-focus-ring-alignment.md` §1。列宽加量仍只取宽度 24，§2 的数字没动。
3. **漏斗图标尺寸**：EP `table.scss:620-630` 给 `i{font-size:14px}`，本库用 12px 图标 + `transform:scale(.75)`。
   `scale` 只缩视觉不缩布局盒，故加量取 **18 = 12 + margin-left 6**（实测 `filter18`，见 N2 的 `parts`）。
4. **`ElTableColumn.Width` 是 `string`**（`ElTableColumn.razor.cs:15`），而 `TableHeader.Width` 是 `IntString?`
   （`TableHeader.cs:17`）。bUnit 里用 `AddAttribute("Width", (IntString)110)` 传进去会**静默变 null**，
   列于是回落到 80 —— 本批新加的 6 条宽度用例第一版就是这么"红"的，与被测逻辑无关。写宽度断言一律给字符串。
5. **caret 盒的标签名不同**：EP 用 `<button type="button" class="caret-wrapper">` + 两个 `<i class="sort-caret">`
   （`Temp/ep214_table_header_index.ts:270-295`），本仓沿用既有的 `<span class="caret-wrapper" role="button"
   tabindex="0" aria-label>` + 两个 `<span class="sort-caret">`。语义（可点、可聚焦、有可访问名称）与几何
   （CSS 只认类名，批次⑧后实测 24×14）都对齐，标签名没换 —— 换标签会动到既有事件绑定与外部选择器，超出本批范围。

---

## 7. 复现方法与验证汇总

### 7.1 跑法

```sh
# 先杀掉 5199 上的旧实例（它会继续服务旧构建），再 build，再起站
netstat -ano | grep :5199        # 逐个 kill
dotnet build demo/Element.Blazor.Demo/Element.Blazor.Demo.csproj
# 起站后 curl --noproxy 127.0.0.1 确认 200

NODE_PATH="C:/Users/Administrator/.workbuddy/ce_verify/node_modules" \
  node demo/tools/probes/verify-batch7.cjs > Temp/verify-batch7.log 2>&1
```

`.razor`/`.cs` 改动必须重建；`wwwroot/**/*.css`（§3 那条就在这里）与 `wwwroot/js/dom.js` 是开发期静态资源，
直读源文件，不用重建，但**改完 CSS 要重启服务**再验收。

### 7.2 本批读数

| 项 | 结果 | 日志 |
| --- | --- | --- |
| `verify-batch7.cjs` | **22/22 PASS**（0 FAIL / 0 SKIP），`CONSOLE_ERRORS=[]` | `Temp/verify-batch7.log` |
| bUnit 本批新类（11 例：5 结构/样式 + 6 宽度） | **11/11** | `Temp/bunit-headercell.log` |
| 宽度逻辑变异体（去掉加量） | **失败 4 / 通过 7**，4 条正是四条宽度契约 | `Temp/bunit-headercell-mutated.log` |
| 变异体还原 + `touch` 后复跑 | **11/11** | `Temp/bunit-headercell-restored.log` |
| bUnit 全量 | **2376/2376**（下一批的基线）；文档与注释定稿后复跑同数（含 `ProbeHygieneGuard` 7/7） | `Temp/bunit-full-batch7.log`、`Temp/bunit-full-afterdocs.log`、`Temp/bunit-full-final.log`、`Temp/bunit-hygiene-afterdocs.log` |
| 宽视口取证（证明 §2.4 的富余摊薄） | AUTO `tableW 1074 / Σcol 842`；FLIP `单价 134→110 th 170→144`、`库存 222→180 th 282→240`、`名称 238→220 th 302→285` | `Temp/diag-batch7-colwidth.log` |
| `verify-batch6.cjs` 复跑 | 改点位前 **14/15（C 段红）**；改点位后 **15/15 PASS 连续 6 次**（3 次热跑 + 冷重启演示服务后 3 次） | `Temp/verify-batch6-rerun.log`（红的那次）、`-rerun2/3/4.log`、`-cold1/2/3.log` |
| 其余表格族探针复跑（**全部 0 FAIL**） | `verify-batch5` `TOTAL=14 FAIL=0`、`verify-header-click` 5/5、`verify-op-column` 8 项、`verify-dyntable` 18/18、`verify-table-edit-typing` 7/7、`verify-account-table` 32/32、`verify-pivot-row-align` 3/3、`verify-pivot-header` `ALL_PASS=true`+`CONSOLE_ERRORS=0`、`verify-tree-table` ALL PASS、`verify-detail` 0 FAIL+`consoleErrors=0`、`verify-batch6-docs`（文档数字与探针同源） | 对应 `Temp/verify-*-rerun.log` |

> 上表探针读数采于**最后一次行为改动之后**的那个构建。此后本批只改过一处 XML 文档注释的文字
> （`HeaderIconAllowance` 的示例列名，`ElTable.razor.cs:1055-1061`），不动 DOM 也不动几何，
> 因此没有为它重杀 5199 / 重建 / 重量；改后复跑 bUnit 全量仍是 **2376/2376**（`Temp/bunit-full-final.log`）。
> 下次任何会影响渲染的改动，仍按 §7.1 的顺序"杀端口 → build → 起站 → 跑探针"。
>
> **批次⑧之后确实又有一次影响渲染的改动**（caret 盒几何，`index.css`），本探针按上面顺序复跑过：
> **22 项 PASS / 0 FAIL / 0 SKIP、`CONSOLE_ERRORS=[]`**（`Temp/verify-batch7-after-b8-final.log`）。
> 其中 H2 的期望从 24×34 改成 EP 的 **24×14**（探针注释里写明了是哪批改的），H3/H4 的 `.cell` 读数
> 因盒高变化从 `34/34`、`138/34` 变成 `23/23`、`143/23`，§2.3 表里的**宽度**读数一列未变。

### 7.3 顺手拆掉的一个假红（批次⑥的 C 段）

`verify-batch6.cjs` 的 C 段（面板内滚轮）在本批复跑时红过一次。`diag-batch6-C.cjs` 逐点位 dump 证明
探针取的是 `panel.top + 40`，那里是面板顶部的搜索框，实测命中 `el-input__inner` —— 它不是滚动容器，
滚滚轮一动不动是**正确行为**；把点位下移到第一行中心就 `inner 0→43`（`DY=90` 那条）。
所以这是探针选点错，不是产品坏了：C 段改成"取内层 `.el-table__body-wrapper` 第一行的中心"并在注释里写清这条，
判据同时要求"真有溢出量程"，量不出来就记 `SKIP` 而不是红。

### 7.4 一个未复现的历史读数（如实记录，不抹平）

批次⑥的 F 段（⑪ 列头菜单弹出延迟）在更早一次运行里量到 **1274ms**。本批围绕 C 段一共跑了 7 次这份探针
（改点位前 1 次 + 改点位后 6 次，其中 3 次在冷重启演示服务之后），F 段只量到
**63 / 67 / 73 / 74 / 79 / 91 / 94ms**
（各日志里的 `F-header-menu-open-latency` 行：`Temp/verify-batch6-rerun.log`、`-rerun2/3/4.log`、`-cold1/2/3.log`）。
没有定位到机制，所以**没有放宽阈值**（仍为 400ms），也不把那次说成"环境抖动"—— 它就是一个未复现的离群读数。

批次⑧收口时把这份探针又跑了一次，F 段量到 **196ms**（`Temp/verify-batch6-after-b8-final.log` 的
`F-header-menu-open-latency` 行， verdict 仍为 PASS）。这条比上面 7 个读数都高，但没有接近 400ms，
也不改变"未复现"的结论 —— 补在这里是为了下次复跑时看到 196 不会以为判据莫名变红。

### 7.5 取证脚本去留

`diag-batch6-C.cjs`、`diag-batch7-colwidth.cjs` 留在主目录：它们不是历史一次性 dump，
而是 §2.4 与 §7.3 两条方法论的**可重放证据**（任何人再质疑"N4 凭什么要在 800 宽做"或
"C 段凭什么改点位"，直接跑就行）。判据见 `docs/testing-guards.md` §3.5。

---

## 8. 落点清单（改了什么）

| 文件 | 内容 |
| --- | --- |
| `src/Components/ElTable.razor:126-133` | `@header.Text` 提为 `.cell` 直接子节点，`caret-wrapper` 只剩两个箭头；两条分支合并成一条 |
| `src/Components/ElTable.razor:134` | 漏斗渲染判据改用 `HasFilterTrigger(header)`（与列宽同源） |
| `src/Components/ElTable.razor:93 / :177` | 两个 `<colgroup>`（:81 表头、:165 表体）的注释同步新口径 |
| `src/Components/ElTable.razor:101/117/212/252/287` | 5 处 `style` 包 `StyleToString(...)` |
| `src/Components/ElTable.razor.cs:991-1075` | `SortIconBoxWidth` / `FilterIconBoxWidth` / `ResolveColumnWidth` / `ResolveBaseColumnWidth` / `HasFilterTrigger` / `HeaderIconAllowance` |
| `src/Components/ElTable.razor.cs:2970` | `StyleToString` |
| `src/Components/wwwroot/css/index.css:5455` | 删 `.el-dropdown-table-grow__panel .el-table__body-wrapper{overscroll-behavior:contain}`，留注释 |
| `test/Element.ComponentTests/ElTableHeaderCellTests.cs` | 新文件，11 例（结构 2 + 字典 3 + 宽度 6） |
| `demo/tools/probes/verify-batch7.cjs` | 新探针，H/S/N/W/D 五段 22 项，含 4 处翻面变异（H4/S4/N4/W4）与 N5 还原对照 |
| `demo/tools/probes/{diag-batch7-colwidth,diag-batch7-layout,diag-batch7-wheel,diag-batch6-C}.cjs` | 取证脚本 |
| `demo/tools/probes/verify-batch6.cjs` | C 段点位改到真正的滚动容器（§7.3） |
| `demo/Element.Blazor.Demo/Models/MockData.cs` + 10 个 `Pages/*.razor` | §5 数据去真实化 |
| `docs/组件使用说明.md`、`docs/table-column-width-and-paging.md`、`docs/table-filter-alignment.md`、`docs/testing-guards.md`、`CHANGELOG.md` | 口径与数字同步 |
