# ElTable 的 auto 列：不写 `width` 的那一列吸收富余（批次㊸，2026-10-15）

宿主 `zyweb` 的一句「http://localhost:5171/baseinfo/deptinfo，这个操作列太宽」牵出的是库侧缺陷：
本仓此前**不存在 auto 列**，所以「声明列宽之和 < 表格宽度」的稀疏表里，任何钉死的宽度都保不住。
本篇把推导、四档试用、修法与实测数字记全，避免下次又把「列宽调了没反应」当成 CSS 问题查。

配套口径在 `docs/table-column-width-and-paging.md` §1–2（列宽三档与表宽求和）与
`docs/table-header-cell-and-column-width.md` §2（表头图标盒加量，批次⑦）。

## 问题

部门维护表只有四列，宿主写的是 `操作=160px / 编码=160px / 名称=200px / 停用=60px`，
`.el-table` 的容器实测 1106px。浏览器给出的是 **305 / 305 / 381 / 114** —— 操作列被摊成 305px，
两颗按钮右边一大片死区，就是用户截图里那块。

机制在 `table-layout:fixed` 的分配规则：

- **每列都带宽度、且宽度之和 < 表格宽度** ⇒ 富余**按声明宽度比例**摊回每一列。
  手算 `160/580 × 1106 = 305.2`、`200/580 × 1106 = 381.4`、`60/580 × 1106 = 114.4`，与实测逐列一致。
- **有列不带宽度** ⇒ 带宽度的列按声明钉死，剩余宽度**均分给不带宽度的列**。

`ElTable.razor.cs` 的第三档原本把兜底值 `80` 真写进了 `<col width="80">`，
于是本仓永远落在第一条规则里。后果是消费侧无论把数字改成什么都没用：
`160 → 140` 只会得到 `140/560…→277`，方向对、量级完全不对。

### 「改前」复现（DOM 级对照，`_tmp_probe/b39_before.log`）

批次㊸ 上线后页面已读不到旧症状，所以把四列的 `width` 属性按改前写法在真实 DOM 上钉回去
（`160/160/200/60`，表头表体两个 colgroup 都改），同一页同一视口三段读数：

| 视口 | 现状（名称 auto） | **复现改前（四列全钉死）** | 反向对照（只把名称改回 auto，其余仍 160） |
| --- | --- | --- | --- |
| 1440（容器 1106） | `140/160/744/60` | **`305/305/381/114`** | `160/160/724/60` |
| 1100（容器 766） | `140/160/404/60` | **`211/211/263/79`** | `160/160/384/60` |
| 1920（容器 1586） | `140/160/1224/60` | **`437/437/546/164`** | `160/160/1204/60` |

第三列（`名称`）改成 auto 之后，前两列立刻回到各自的声明值（`160` 而非 `305/211/437`）——
**决定「声明宽度算不算数」的是有没有 auto 列，不是声明值本身**。
中间那一段就是用户看到的死区，且它随视口线性放大（1100→1920：211→437），
所以任何「按某个视口算出来的像素数」都救不了这张表。

## 修法

三处，都在 `src/Components/ElTable.razor.cs`：

1. **列宽三档改成 `Width` > `MinWidth` > auto**（`ResolveBaseColumnWidth:1154`、`ResolveColumnWidth:1119`）。
   第三档返回 `null` 而不是 `80`；Blazor 对 `null` 属性是整体省略，所以 `<col>` 不带 `width`，
   `ElTable.razor` 两个 colgroup（:102 表头 / :291 表体）一字未改。
   auto 列**不吃表头图标盒加量**（批次⑦ 的 24/18/42 是给钉死宽度补房间用的，auto 列没有可补的数）。
2. **auto 列的可读性下限换成记账**（`ResolveColumnWidthAsInt:1223`）：属性上不写宽度，
   但 `ColumnsTotalWidth` 仍按 `80 + 该列加量` 进 `width:max(100%,Σ)`。
   于是窄容器（Σ > 容器）走横向滚动、每列下限 ≈80px，批次⑦ 那条
   「十个无宽列挤成 25px，`.cell{word-break:break-all}` 把 `Category` 逐字母竖排」的回归不会回来；
   宽容器（Σ < 容器）富余全部由 auto 列吸收。二者不可兼得的那一档正是这次要修的稀疏表。
3. **表宽求和认 `px` 后缀**（新增 `ParseColumnWidthPixels:1178`）：
   原来用 `int.TryParse(ResolveColumnWidth(header))`，而宿主普遍写 `Width="160px"` ⇒ 解析失败、
   **带后缀的声明一律按 80 记账**。部门维护表真实 Σ=580 被记成 320（页面读数 `max(100%,320px)`），
   缺口 A 的失真版本；本仓 32 个含列声明的 .razor 里后缀写法是主流，所以这条几乎每页都在错。
   现在剥 `px` 再解析，非法值才回落 80；`<col width>` 的字面值保持宿主原样，不动它。

连带的第三处防守：`CellOverflowStyle`（EP `table-column/render-helper.ts:195` 那条
`.cell` 内联 `width = realWidth − 1`，本仓用记账宽代替 realWidth）必须对 auto 列返回 `null`
（`:3167-3184`），否则 744px 的列里 `.cell` 被钉成 `width:79px`，长文本提前打点。
这条只有改动 1 之后才会露出来，是改动 1 的连带回归，不是既有缺陷。

### 四档消费侧写法的实测（`_tmp_probe/b39_widthtrial.log`）

在同一张真实表上直接改 `<col>` 属性，三档视口各跑一遍：

| 写法 | 1440 | 1100 | 1920 | 判定 |
| --- | --- | --- | --- | --- |
| A：全写死，名称按剩余空间算成 `746` | `140/160/746/60` ✓ | 表宽仍是 1106 → 溢出 766 的容器 | `201/229/1068/86`（比例摊回来了） | 只对单一视口成立 |
| **B：名称不写 width（auto）** | `140/160/744/60` ✓ | `140/160/404/60` ✓ | `140/160/1224/60` ✓ | **三档全钉住，采用** |
| C：名称写 `width="100%"` | `140/160/744/60` | `140/160/404/60` | `140/160/1224/60` | 读数与 B 逐视口相同 —— 浏览器把这个属性当成「未参与分配」 |
| D：四列全百分比 `12/15/63/10%` | `276×4` | `191×4` | `396×4` | 等宽塌法，声明比例不生效 |

C 与 B 的**几何**相同，但**库侧不同**：百分比在 `ResolveColumnWidthAsInt` 里返回 0，
那条 80px 的下限记账就没了（§1 第 2 点）。所以「让某一列吃满剩余空间」的正解是**不写宽度**，
不是写 `Width="100%"`。

## 验证

单元测试（`test/Element.ComponentTests`）：

| 用例 | 钉住什么 |
| --- | --- |
| `ElTableHeaderCellTests.ColumnWithoutWidthEmitsAutoCol` | 表头与表体的 `<col>` 都**没有** width 属性（`Assert.Null`） |
| `ElTableHeaderCellTests.AutoColumnStillCountsFloorInTableWidth` | auto 列仍进求和：无图标 `max(100%,80px)`、带排序 `104px`、带漏斗 `98px` |
| `ElTableHeaderCellTests.PxSuffixWidthCountsItsRealPixels` | `Width="160px"` ⇒ col 原样 `160px`、表宽按 `160` 记（不是 80） |
| `ElTableHeaderCellTests.MinWidthColumnGetsAllowance` | MinWidth 一档仍按 `90px + 24` 记 `114`（由原来同时断言 auto 档的 `MinWidthAndDefaultWidthAlsoGetAllowance` 改写而来 —— 全仓只有这一处依赖旧兜底） |
| `ElTableOverflowTooltipTests.AutoColumnSkipsInlineWidth` | auto 列的 `.cell` 不写内联宽度；同类的 `ColumnFlagSetsEpCellWidthOnePixelNarrower`（170→`width:169px`）留作正向对照 |

- 命名 8 例（含 3 条既有对照）**8/8**（`_tmp_b39_named.log`）；`ElTableHeaderCellTests` + `ElTableOverflowTooltipTests`
  两个整类 **26/26**（`_tmp_probe/b39_test1.log`）；全量 **2657 通过 / 0 失败**（`_tmp_b39_full2.log`）。
  首次全量曾 1 红 `ElNotificationTests.PauseOnHover_KeepsNotificationAlive_AndResumesAfterLeaveAsync`
  （bUnit 自述「timeout is too short… highly utilized hardware」，单跑即绿）—— 负载敏感的偶发，与本批无关，
  按「偶发红要挖真身」的口径记在这里而不是悄悄跳过。
- **翻面验牙 A**（把 `ResolveBaseColumnWidth` 第三档还原成 `80`）⇒ **3 红 / 4 绿**（`b39_mutA.log`），
  三条读数正是三种旧症状：`Actual: "width:79px"`（缺口 C）、`Actual: "80"`（auto 档）、
  `String: "width:100%;width:max(100%,80px)" Not found: "max(100%,160px)"`（px 后缀）。
- **翻面验牙 B**（让 auto 列记账为 0）⇒ **1 红 / 3 绿**（`b39_mutB.log`），
  `String: "width:100%" Not found: "max(100%,80px)"` —— 下限确实被这条断言绑住。
- 还原：`cp Temp/b39/…`（取当前已验证的好版本）+ `md5sum -c` 命中 `e54480bf687fff2c750bd9d0c63a76a5`，
  **未用 `git checkout --`**。
- 出库：Release 构建 **0 警告 / 0 错误** → `nuget/Element.0.10.0.nupkg` **1 758 829 字节 / 21:48** →
  删缓存重装 → zyweb 重建 **0 错误**（1138 警告均为既有）→ dev server 重启 + `setCacheDisabled` 刷新。

## 消费侧（zyweb 部门维护页，浏览器实跑）

`zyweb/Pages/BaseInfo/DeptInfo.razor`：操作列 `Width="160px"` → **`140px`**
（两颗 small 朴素圆角钮的口径 `n*48+(n-1)*12+24 = 132`，取 140），
「名称」列**去掉** `Width="200px"` 变成 auto，并由它吸收全部富余（改动的理由写进了 Razor 注释）。

- 结构读数（`_tmp_probe/b39_accept.mjs`，11/11）：`colAttrs=["140px","160px",null,"60px"]`、
  `nameColHasWidthAttr=false`、`style=width:100%;width:max(100%,440px)`
  （`140+160+60` + auto 记账 `80`；改前是失真的 `320` —— 缺口 A、B 同时落地的证据）。
- 逐视口表头：`[140,160,744,60]` @1440 / `[140,160,1224,60]` @1920 / `[140,160,404,60]` @1100，
  且 `scrollWidth == clientWidth`（窄容器**不出**横向滚动条，因为 Σ=440 < 766）。
- 操作列内容几何：`need=108 td=140 scrollW=140 clientW=140 左留白=10 右留白=22` —— 按钮完整、不裁切，
  死区从 165px（305−140）收到 22px。
- 回归 11 页（`b39_regress.mjs` / `b39_regress2.mjs`）：员工维护 13 列、生产订单 21 列（Σ>容器 → 横向滚动，符合预期）、
  物料类别树表、基本字典、供应商、仓库、物料列表…全部**无表头 CLIP / 无 SHRUNK（列被压到低于声明宽）/ 无 0 宽列**，
  `errs=[]`。三张物料页读数 `auto0`（全列都写了宽度 ⇒ 本批不动它们）。
  截图：`b39_dept_head.png`（真实 0 行）与合成行版 `b39_dept_synth.png` / `b39_dept_opcol.png`。
- 量之前先自证样式挂上：`table-layout:fixed`、8 张表样式、小按钮 `11px/11px`+圆角 `20px`+字号 `12px`、
  表头格内缩 `10px`、`--el-color-primary:#409eff` 非空（`b39_regress_report.txt` 首两行）。

## 边界与未做

1. **本批只改「不写宽度」这一档**。全列都写了宽度的表仍然按 `fixed` 的比例摊法走
   （物料类别 `180→184` 这类几像素偏差就是它）—— 要不要给某一列去掉宽度是消费侧的排版决定，库不替它决定。
2. **EP 对齐的是行为，不是实现**。Element Plus 有 realWidth 布局引擎（`table-layout:auto` + JS 计算），
   本仓是 `<col width>` 直接钉死 + 一条 `max(100%,Σ)`；两者在「不写 width 的列吸收富余」这一点上口径一致，
   但本仓不会按内容收缩，也不会在多个 auto 列之间按内容分配。
3. **百分比列宽仍是原样透传**（`width` 属性写不了 `calc()`，也不认 `100%` 的分配语义，见上表 C/D），
   且记账为 0 —— 需要下限就别用百分比。
4. **本环境无业务接口**（`unitsurl` 为空）：报表中心的分组表头与款号管理左表都渲染成 **0 列**，
   `ElTableColumnGroup` 那条 colgroup 路径只有源码级证据，没有浏览器证据。
   DeptInfo 的合成行截图同理是合成 DOM，不是真数据。
   顺带一句免得有人误读截图：冷启动任一数据页时顶部会有一条红条
   `ArgumentNull_Generic Arg_ParamName_Name, uriString`，来自宿主 `ComFunction.razor:233` 的
   `new Uri(unitsurl)`（unitsurl 不存在 ⇒ 抛异常 ⇒ `hishttppost` 的 catch 折成 `result:"1"` 回给页面弹提示），
   与本批无关，属既有健壮性问题 + 环境缺接口。
5. 全仓 32 个含列声明的 .razor 里，auto 列目前只有 DeptInfo 这一处（本批新增）；
   「有宽列 + 无宽列混在同一张稀疏表」这个用法此前在本仓不存在，所以改动面就是这一页 + 那一处旧兜底断言。
