# ElTable 尾部填充列 `TrailingFiller`（批次91，2026-10-21）

宿主 `emrelecrecordplusgroup` 的一句「/dictionary/docelement 太宽了」（截图圈住「编辑时间」列）牵出的是库侧缺陷：
`table-layout:fixed` 下「让声明宽度算数」和「表格铺满容器」这两件事，本仓此前**只能靠留一列 auto 来同时做到**，
而留哪一列当 auto 是宿主每次都要重新猜一次的排版决定。本篇记下推导、三种版式的实测、参数语义与那根断掉的表头横线。

相关口径：`docs/table-auto-column-width.md`（批次㊸，auto 列的来历）、
`docs/table-column-width-and-paging.md` §1–2（列宽三档与表宽求和）、
`docs/table-header-cell-and-column-width.md` §2（表头图标盒加量 24/18/42）。

## 问题

DocElement 表 9 列，容器实测 1307px。razor 原先的写法是「除名称外每列都钉宽，名称不写宽度」，
但这一版 razor 里没写宽度的那一列是**编辑时间**（日期列），于是：

| 列 | 声明 | 实测格宽 | 内容自然宽 | 需要的列宽 | 倍率 |
| --- | --- | --- | --- | --- | --- |
| 名称 | 120 | 120 | 126 | 146 | 0.82（**折两行**） |
| 编辑时间 | 不写（auto） | **634** | 71.6 | 91.6 | **6.92** |

读数：`_tools/docelement_probe_de_before.out`（1600×900，20 行真数据，金丝雀把该列 `<col>` 钉到 40 ⇒
表头从 1 行变 4 行、倍率 6.92→0.44、复原回 634，证明这一列的宽度确实被尺子绑住）。

用户的原话「太宽了」就是那 634 − 91.6 = 542px 的死白。

### 那「一列都不留 auto」行不行？（宿主追问过，答案是实测的）

不行，而且更糟。`table-layout:fixed` 的分配规则只有两条：

1. **每列都有宽度且 Σ < 表宽** ⇒ 富余**按声明宽度比例摊回每一列**；
2. **有列不带宽度** ⇒ 带宽度的列按声明钉死，剩余**均分给不带宽度的列**。

把 9 列全钉上（声明之和 773，容器 1307）实测（`_tools/docelement_allpinned_ap1.out`）：
整体 ×1.69 —— 操作 128→216.4、日期 100→169.1。也就是**写多少都不算数**，而且富余仍然按比例落进日期列。
所以「留 auto」不是为了好看，是为了让声明值有意义；这条口径本身没错，错的是**必须由宿主来挑那一列**。

挑错的代价就是本页：末列是日期 ⇒ 日期吃下全部富余。批次90 之后消费侧的临时解法是把 auto 位交给名称列
（`_tools/docelement_probe_de_after2.out`：名称 654、编辑时间 100，逐列精确、不折行）——能用，
但它把「哪一列该吸收富余」变成了一张页一个答案的排版题，且名称列凭空白得 654px。

## 修法：把富余交给一根没有内容的尾列

`TrailingFiller`（默认 `false` ⇒ 既有渲染一个字节都不变）：

1. **表头/表体各多渲染一根不带 `width` 的 `<col>` + 一个空格子**（`ElTable.razor`，共 6 处插入：
   两个 colgroup、单行表头、分组表头第一行、每个表体行、合计行）。规则 2 生效 ⇒
   数据列一律精确等于声明值，富余全归尾列。
2. **开了尾列后，不写 `Width` 的数据列按记账钉死**（`ResolveColumnWidth`：`DefaultColumnWidth(80) + 图标盒`）。
   必须钉：否则场上有两根不带宽度的 col，规则 2 变成「尾列和它**均分**」，声明值照样保不住。
   这个数与 `ColumnsTotalWidth` 的记账同源，所以窄容器（Σ > 容器）时表宽走 `max(100%,Σ)`、尾列自然塌到 0、
   照旧横向滚动 —— 尾列不参与求和（它不是 `Headers` 里的一项）。
3. **整表一列都没声明宽度时不开尾列**（`UsesTrailingFiller`）。那种表本来每列均分，插尾列只会把
   「均分 N 份」变成「均分 N+1 份」，把每一列都挤窄。
4. **CSS 把尾格的内衬与右边框归零**（`wwwroot/css/index.css`，选择器写到 `(0,4,1)` 才压得住 vendor 老主题的
   `.el-table th>.cell{padding-left:10px}`）。归零不是美化：窄容器时它塌成 0 宽，
   留着 10/12px 内衬会把**每一行**撑出十几像素，凭空多一条滚不动也看不见的滚动条。

尾列**不是** `Headers` 里的一项：排序、筛选、span-method、编辑取列、合计行下标都不受影响。

## 那根断掉的表头横线（本批第二个症状，出库前才被像素级探针抓到）

首版尾格的 `th` 只写了 `el-table__cell el-table-column--filler`，表头下沿那条 1px 横线**在 1125 处断掉**，
右边 454px 一片没有底线。根因是本仓的口径：表头下划线只挂在 `th.is-leaf` 上
（`index.css` 的 `.el-table th.is-leaf, .el-table th.el-table__cell.is-leaf, .el-table td.el-table__cell
{border-bottom:var(--el-table-border)}`），而 `td.el-table__cell` 自带一条 —— 所以**表体尾格是对的、表头尾格漏了**。
分组表头那一支同理：尾格 `rowspan=2`，它的下缘正好是整个表头的下缘，所以也必须 `is-leaf`。
两处补 `is-leaf`（而不是新写一条 CSS 规则）才是本仓既有口径的正解：`is-leaf` 在 EP 里表示「叶子表头格」，
顶层非分组列本来就带它（`GetHeaderCellClass`）。

## 验证

**单元测试**：`test/Element.ComponentTests/ElTableTrailingFillerTests.cs`（本批新增 10 例）钉住
默认关无尾列 / 尾列两根 col 都不带宽度且顺序在数据列之后（表头倒数第二根，最后一根是 gutter）/
auto 列钉成 80、带排序钉成 104（关着时必须读到 `null`，否则只是碰对）/ 表头一格表体每行一格且无内容 /
表头尾格带 `is-leaf`（含分组档 `rowspan=2`）/ 开关两版 `max(100%,Σ)` 同一个数 / 全 auto 表不开尾列 /
MinWidth 一档也算声明 / CSS 侧同源约定（bUnit 不加载样式表，只能读源码钉：归零规则 + 下划线仍挂在 `th.is-leaf`）。

- 本类 **10/10**（`Temp/b91tf_newtests3.log`）。
- **翻面验牙 1**（把两处 `is-leaf` 摘掉）⇒ **2 红 / 8 绿**（`Temp/b91tf_mut_isleaf.log`），
  红的正是 `FillerHeaderCellCarriesTheLeafClassThatDrawsTheHairline` 与分组那一例。
- **翻面验牙 2**（`UsesTrailingFiller => false`）⇒ **6 红 / 4 绿**（`Temp/b91tf_mut_usesfiller.log`），
  4 个绿的正是「关着时不该有尾列」那几条 —— 断言方向对得上。
- 还原用 `cp Temp/b91tf_ElTable.razor.good`（**未用 `git checkout --`**），`md5sum -c` 命中
  `b121860d34f1c58348e03f87689eab74` / `118f9b20797351b19726af205cb5269f`，`touch` 后复跑 10/10。
- 全量：**失败 1 / 通过 2727 / 总计 2728**（`Temp/b91tf_full_01021.log`）＝ 批次90 基线 2718 ＋ 本批 10，
  唯一红仍是既有 `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows`（`docs/testing-guards.md:113` 写明的 1 红基线）。

**浏览器实测**（同一把尺子，改前/改后各一份，1600×900、20 行真数据）：

| 档 | 逐列格宽 | 尾部空白 | 折行 | 探针 |
| --- | --- | --- | --- | --- |
| 改前（0.10.19，日期列 auto） | 55/128/120/120/50/50/50/100/**634** | 无 | 名称 2 行 | `_tools/docelement_probe_de_before.out` |
| 消费侧临时解（名称 auto） | 55/128/120/**654**/50/50/50/100/100 | 无 | 无 | `_tools/docelement_probe_de_after2.out` |
| **0.10.21 `TrailingFiller`** | **55/128/120/200/50/50/50/100/100**（逐列 = 声明） | 454 | 无 | `_tools/docelement_probe_de_tfafter.out` |

- 尾格横线：`_tools/filler_line_f2.out` ⇒ 尾 `th` `1px rgb(235,238,245)` == 左邻 `th`、尾 `td` == 左邻 `td`、
  尾 `th` 右缘 1579 == 表右缘；改前那份（`_tools/filler_line_f1.out`）读的是 `0px rgb(144,147,153)`。
  判据用「与左邻同档」而不是「> 0」，否则粗细/颜色降一档的半修好会漏网。
- **列宽档对照**（`_tools/filler_width_ab_ab1.out`，只改 DOM 上的 `<col width>`，跑完复原并核对 200）：
  名称 200 → 尾 454 / 320 → 334 / 460 → 194，三档都无折行、尾格都有横线。**采用 200**：
  名称钉到 460 时行内出现一段比列宽还长的空白，眼睛要从「名称」的值跨过 300 多像素才够到「病历」勾选框，
  比把空白留在表格右端更难看（截图 `filler_width_ab_ab1_{200,320,460}.png`）。
  空白的位置是排版决定，库不替宿主决定；库只保证「声明多少就是多少」。
- **真实像素**（`_tools/png_scan_control.txt`，自写无依赖 PNG 解码器逐行扫 `_tools/png_row_scan.mjs`，
  对同一张截图取「正文列 x=1060」和「尾部填充列 x=1400」两列，看 y=249（表头下沿）有没有横线）：

  | 截图 | x=1060（数据列，对照） | x=1400（填充列，判据） |
  | --- | --- | --- |
  | 改前 0.10.20（尾格无 `is-leaf`） | 有：248[d6]、249[d11] | **没有**（最近的 259[d8]/260[d7] 是水印笔画） |
  | 改后 0.10.21 | 有：248[d6]、249[d11] | **有**：249[d0] |

  两档的横线数差恰好只有一条 `249[d0]` —— 补上的那根线，别的什么都没动。
  **为什么必须带容差**：这一页有斜排水印，水印压在横线上时像素被混色（x=1060 的表头下沿实测读成
  `rgb(241,241,241)`/`rgb(225,228,234)`，不是精确的 `rgb(235,238,245)`），精确色匹配会在**有内容的那侧假红**，
  于是对照档一度被误判成「尺子坏了」。改成「逐通道最大差 ≤ T」后，T=6/12/20 三档跑完，
  上表四个格子**判据一字未变**（`_tools/png_scan_tolerance.txt`）—— 结论不依赖我挑的容差。
  注意容差 ≠ 失去牙齿：填充列改前那格在 T=20 下仍然是 false，因为空白处是纯白（差 20+），
  水印那几条也落在 249±1 之外。

**出库校验**：`nuget/Element-Plus.0.10.21.nupkg` 内层 `lib/net10.0/Element.dll` md5
`b74e4aed310a1af8cb4fc4f80e6594d3` == `bin/Release/net10.0/Element.dll`；
`staticwebassets/css/index.css` md5 `e882792302c9f6e1386c3fa88674726c`（567151 字节）== 源码 ==
服务侧 `http://localhost:5084/_content/Element-Plus/css/index.css` 逐字节；
服务侧 `/_framework/Element.vitch2jwf9.wasm` md5 == 磁盘 == 且含 `el-table__cell el-table-column--filler is-leaf`
（UTF-16 #US 命中 1 处，两处 razor 字面量在 #US 堆里合并，所以 1 就够）。

> **本批踩到的发版坑（记下来免得再踩）**：第一次 `dotnet pack` 打出 0.10.21 时 RC=0、包也生成了，
> 但内层 dll 是**上一版的字节**（`el-table-column--filler is-leaf` 命中 0、mtime 仍是改前的 11:12），
> 日志里连 `Element -> ...dll` 那一行都没有 —— 就是 `docs/testing-guards.md:193` 那条「改了却没生效、编译仍 0 错误」。
> 靠「解包 grep 新字面量 + 比 dll mtime」才抓住，光看 pack 的 RC 会直接发错版。

## 边界与未做

1. **尾列不吃内容**。EP 的 realWidth 引擎会按内容分配剩余宽度，本仓的尾列只是「一根不带宽度的 col」，
   所以它永远等于 `表宽 − Σ声明`，与内容无关。
2. **`IsEditable` 的「新增行」那一行没补尾格**（`ElTable.razor` 里那条独立 `<tr>`）。它本来就和 colgroup 不同源，
   浏览器会自己补空格子；补了反而改变既有编辑行的格数。
3. **合计行补了尾格**（`tfoot`），否则合计行的格子数与 colgroup 对不上，边框/底色不跟上面一行了。
4. **默认关**。全站其余表格页要换这套版式，是逐页的排版决定（要先给每列一个能接受的声明宽度），
   库不替它们默认打开。已扫到三张「日期列没写宽度」的页（`Home.razor` 公告时间、`BaseInfo/NoticeInfo.razor`、
   `Dictionary/SettleMentInfo.razor` 创建时间，见 `_tools/date_auto_scan.txt`）仍是批次㊸ 的 auto 口径，未动。
