# 验证层手册：全库守卫与浏览器探针

> 目的：让下一次会话（或下一个人）不用再考古日志，就能知道**这个仓库有哪些现成的守门工具、怎么跑、上一次结果是什么**。
> 最后更新：2026-10-08（同日七批：① 弹层宿主的跨作用域共享集合缺陷修掉、并行锁解除；② `LoadingService` 去 Singleton、演示站第二个 `<ElPopup />` 宿主删掉、宿主数量变成可观测；③ 全仓 GBK 存盘的文本文件清成 UTF-8，并加第七常驻守卫 `SourceEncodingGuardTests`；④ `ElMenu` 深色主题悬浮"白底白字"——一个写死的 `HoverColor` 默认值压过主题类，见 `docs/menu-alignment.md` §7；⑤ `ElTabs` 三件事：硬编码页签点 X 不删（`ResetActiveTab` 在 `DataSource==null` 时直接 return）、`IsRemovable` 漏标 `[Parameter]`、`before-leave` 初始激活也参与，外加新增 `TabTextAlign`，见 `docs/tabs-alignment.md` §8；⑥ `ElMenu` 的 `ellipsis` 从未生效：默认值与 EP `menu.ts:124` 相反、`SyncOptions` 漏掉 `Ellipsis` 字段、子菜单标题是半条功能，见 `docs/menu-alignment.md` §8；⑦ `ElMenu` 被截断的行悬停时用 `elTooltip` 浮出完整文字（**本库扩展**，EP 只在折叠态挂 tooltip，`menu-item.vue:12-31`），这一批留下一条"bUnit 看不见布局"的实测证据，见 `docs/menu-alignment.md` §9、本篇 §3.12 与 §4 第 11 条）。同日第八批（`ElText` / `ElTable` / `ElTableV2` / `*TableGrow` 的截断悬停浮层 + 浮层被滚动容器裁掉的修法 + 滚条淡入写回盖掉滚动位置的竞态）见 **`docs/text-ellipsis-tooltip.md`** 与 **`docs/table-overflow-tooltip.md`**，本篇 §3.13 与 §4 第 13、14 条。当前基线：**全量 bUnit 2341 通过 / 0 失败 / 0 跳过**（上一条记录 2310 ＋31：新增两份测试文件 14 例 + 改动的 `ElTextTests`/`ElTableV2Tests`/`ElMenuTests` 17 例）（上一批 2301，本批 +9 例 ellipsis 悬停 tooltip；改完后连跑 10 次读数一致，其中 6 次在 12~24 个 CPU 饱和负载下 —— 例外那一次红见 §4 第 12 条，没抹平）。第二批的机制与取证见 **`docs/popup-host-isolation.md`**，第三批见 **`docs/source-encoding.md`**。
>
> **基线更新（2026-10-09 批次⑧）**：上面那句里的 2341 是 2026-10-08 的时点值，按"历史证据不改写"留着；
> 当前基线是 **全量 bUnit 2381 通过 / 0 失败 / 0 跳过**（批次⑦的 2376 ＋ 批次⑧新增 5 条：开关两个 Tab 停靠点契约 2 条 +
> 三条 CSS 文本守卫），日志 `Temp/bunit-batch8.log`。批次⑧本体见 **`docs/caret-scrollbar-focus-ring-alignment.md`**
> （排序箭头盒 24×34→EP 24×14、grow 面板滚条槽两侧同宽 4px、开关原生黑环换 EP 主色环、城市下拉虚构化）。
>
> **基线更新（2026-10-10 批次⑩）**：当前基线 **全量 bUnit 2442 通过 / 0 失败 / 0 跳过**
> （上一批的 2431 ＋11：`ElTableTreeExpansionTests` 7 例 + `ScrollbarCaliberGuardTests` 4 例，日志 `Temp/b10-bunit-full.log`）。
> 本批两件事：① `ElTable` 本地树形的展开/折叠改成 EP 的「只翻转被点节点 + 可见性由祖先链推导」，
> 修掉「有时点击 2 次才行」（改前 12 次单击有 7 次是空击）；② 全站细滚条口径从「一律 4px」改为
> 「**纵向 4px / 横向 8px**」（横条太细抓不住、拖不动），见 §3.14 与 §4 第 19 条。
> （**横轴那个 8 已被同日批次⑫ 按用户指定改成 6**，见 §3.16；本节读数按批次⑩ 时点保留。）
>
> **基线更新（2026-10-10 批次⑪）**：当前基线 **全量 bUnit 2466 通过 / 0 失败 / 0 跳过**、RC=0
> （批次⑩的 2442 ＋24 条 `ElTourTests`，日志 `Temp/b11-fullsuite-4.log`）。
> 本批把 `ElTour` 按官方 `2.14.0` 重接：遮罩画成真正的镂空洞（目标可见且点得穿）、面板贴目标 16px、
> 箭头随 `data-side` 换边、指示器换成官方圆点、键盘只认 ESC/←/→、关闭时 `current` 归零，见 §3.15 与 §4 第 20-22 条。
> ⚠️ 本批首次全量红的那一条**是守卫对了**：`ProbeHygieneGuardTests` 点名我新写的
> `diag-batch11-i18n-hole.cjs` 把演示站地址写死，按 H1 补 `BASE = process.argv[2]` 后 2466 全绿（§4 第 22 条）。
>
> **基线更新（2026-10-10 批次⑫）**：当前基线 **全量 bUnit 2467 通过 / 0 失败 / 0 跳过**、RC=0（日志 `Temp/b12-fullsuite-2.log`；
> 批次⑪的 2466 ＋1 例 `ScrollbarCaliberGuardTests.Guard_MarkdownEditorScrollbarFollowsLibraryCaliber`）。
> 本批两件事：① 全站**横向**滚条按用户指定 8px → **6px**（纵轴 4px 没碰，grow 的 `th`/`td` 逐列边界仍逐个相同）；
> ② /grow 的 Markdown 编辑器滚条收回库口径（vendor `editormd.min.css` 的 `.editormd ::-webkit-scrollbar` 特异性
> (0,1,x) 把全局 `*` 规则整条顶掉，且编辑栏真正可见的那条是 CodeMirror 的**替身** `.CodeMirror-vscrollbar`），见 §3.16 与 §4 第 23、24 条。
>
> **基线更新（2026-10-10 /grow 章节分批建）**：当前基线 **全量 bUnit 2489 通过 / 0 失败 / 0 跳过**、RC=0
> （上一份实测基线 2484 取自 `Temp/b16-fullsuite2.log`，＋5 例新常驻守卫 `GrowSectionGateGuardTests`，日志 `Temp/bunit-full-k5.log`）。
> 本批把「操作整个界面不流畅」定位到 /grow 冷开并改掉，见 **`docs/grow-progressive-section-build.md`**、§1 的
> 「/grow 章节闸门结构」一行与 §4 第 38-41 条。
> ⚠️ 24 份打到 /grow 的常驻探针串行批跑 20 绿 4 红，4 条的定性（3 条既有红 + 1 条**未定位**）写在
> `CHANGELOG.md` 本批条目与 §4 第 41 条 —— 别把这 4 条红算成本批引入。
>
> **基线更新（2026-10-11 批次⑰ · `/anchor`）**：当前基线 **全量 bUnit 2518 通过 / 0 失败 / 0 跳过**（日志 `Temp/b13-fullsuite-final.log`）。
> 相对上一份记录值 2489 的 **+29 逐条对齐**：`AnchorMarkerCaliberGuardTests` **16**（本批新增，未跟踪文件）
> ＋ `ElColorPickerTests` **+4**（`git diff HEAD -- test/` 实测新增 4 个 `[Fact]`，属同批 /colorpicker 项）
> ＋ `ElSelectInitialLabelTests` **9**（未跟踪文件，属同批 /layout·#112 线，其文档还没齐）。
> 本批 `/anchor` 的三条真身、订正与两轮翻面见 **`docs/anchor-alignment.md`** §5–§7、§1 的
> 「ElAnchor 指示条口径」一行与 §4 第 42-44 条。
> ⚠️ 加守卫后、修导航用例前是 **2517 / 1**（`Temp/b13-fullsuite-after-anchor.log`）：那 1 红是**真回归**而不是噪音 ——
> 组件多了一次 JS 调用，严格 JSInterop 的 `ElNavigationTests.AnchorRendersLinksAndRaisesClickChange` 就必须配它，
> 配完还补了 `JSInterop.VerifyInvoke("elementAnchorUpdateMarker")` 正向对照（只 Setup 不验证的话调用消失也不会红）。

> **基线更新（2026-10-11 批次⑰ · `/colorpicker`）**：当前基线 **全量 bUnit 2519 通过 / 0 失败 / 0 跳过**
> （代码态 `Temp/b14-fullsuite-after-cp.log`；本文件与 `docs/color-picker-alignment.md` 改完复跑同为 2519/0，`Temp/b14-fullsuite-docs-sync.log`）。
> 相对上一份记录值 2518 的 **+1 已逐条对齐**：`ElColorPickerTests` 新增 `ClearButtonStaysEnabledWithoutAColor`
> （无值时页脚「清空」仍不禁用，EP 的页脚按钮连 `:disabled` 都没绑）—— 同批另 4 条新增用例已在上一条基线里计入。
> 本批 `/colorpicker` 的三条真身、6 条翻面变异与"删掉的本库扩展待用户确认"见 **`docs/color-picker-alignment.md`** §1/§4/§5、
> §1 的「ElColorPicker 页脚与触发器图标口径」一行与 §4 第 44-47 条。
>
> **基线更新（2026-10-12 批次㉔）**：当前基线仍是 **全量 bUnit 2554 通过 / 0 失败 / 0 跳过**、RC=0
> （修复后字节实测 `Temp/b24-full-suite-fixed.log`，与 `Temp/b22-full-suite-3.log` 同数 —— 本批牙口全在探针侧，没新增用例）。
> 常驻探针 `verify-treeselect-b19.cjs` 判据数 **44 → 47**（新增段9 三条，见 §3.19、§3.24；新字节 47/47 `Temp/b24-verify-treeselect-after.log`、翻面 46/47 `Temp/b24-verify-treeselect-flip.log`）。
> 本批另记两条与组件无关的账：§4 第 67 条（复测「只有批跑才红」的重放做法 + 证人当场抓到的 rAF 1Hz 现场）、
> 第 68 条（`verify-gap-margin-equivalence` 的 `dtg-tags` 那条**一直没记过档**的确定性红，本轮点名）。

> **基线更新（2026-10-12 批次㉕ · #124 + #125/#128/#129）**：当前基线 **全量 bUnit 2566 通过 / 0 失败 / 0 跳过**、RC=0
> （`Temp/b25-full-suite-restored.log`；文档三件齐改完复跑同为 **2566/0/0**，`Temp/b25-full-suite-docs-sync.log`；= 批次㉔ 的 2554 ＋ `ElStatisticTests` 新增 12 例，逐条点名见 `Temp/b25-statistic-new-tests.log`，
> 两族过滤跑 **31/31** 见 `Temp/b25-statistic-tests.log`）。探针侧两条账：
> ① `verify-datepicker-panel-sidebar.cjs` 判据数 **10 → 16**（新增 C 组 6 条弹层宽度），**16/16**（`Temp/b25-dp-sidebar-probe-final.log`），
>    撤掉侧栏宽度即翻面 **12 通过 / 4 失败**（`Temp/b25-dp-sidebar-probe-C-FLIP.log`，红字里就有用户截图那条底部横滚条的几何形状：`panelRight=632` 对末列 `right=729`）；
> ② 新增常驻探针 `verify-statistic-demo.cjs` **30 项**，**30/30**（`Temp/b25-statistic-probe-RESTORED.log`），CSS 写死字号的翻面 **29 绿 / 1 红**
>    （`Temp/b25-statistic-probe-FLIP.log`，只有 K1 红 —— 原因见 §4 第 71 条）。
> bUnit 侧三次代码翻面：Flip A **1 红 / 7 绿**、Flip B **6 红 / 25 绿**、Flip C **4 红 / 27 绿**（`Temp/b25-flipA-countdown.log`、
> `Temp/b25-flipB-statistic.log`、`Temp/b25-flipC-statistic.log`），全部 `cp` 备份 + `md5sum -c` 还原，未用 `git checkout`。
> 修法与 EP 证据在 **`docs/date-picker-alignment.md`**（#124 新增段）与整篇重写的 **`docs/statistic-alignment.md`**；本文件新增 §3.25 与 §4 第 69–71 条。

> **基线更新（2026-10-12 批次㉖ · #130）**：基线**未动**，仍是 **全量 bUnit 2566 通过 / 0 失败 / 0 跳过**、RC=0
> （`Temp/b26-full-suite.log`）。这条缺陷的牙**只能**长在浏览器侧（真事件滚轮 + 布局引擎，§4 第 11 条的老账），
> 所以本批 0 新增 bUnit 用例，新增一份常驻探针 `verify-popup-wheel-passthrough.cjs` **26 项**：
> **26/26**（`Temp/b26-wheel-probe-RESTORED.log`），换回修复前的 `dom.js` 翻面 **13/26**
> （`Temp/b26-wheel-probe-FLIP2.log`：6 条行为判据 B1–B6 ＋ 7 条跨族判据 F/* 全红，4 条前提证人 A 组与 C/D/E 边界对照全绿），
> 还原靠 `cp Temp/b26-dom.js.fixed` + `md5sum`（修复后 `084493229fdac7b700377afe16137f2b`，修复前 `de198096835f5f63eaf3e062506183c8`）。
> 同页回归 `verify-datepicker-panel-sidebar.cjs` **16/16**（`Temp/b26-dp-sidebar-regression.log`）。
> 修法与取证在 **`docs/popup-shadow-wheel-alignment.md`**（新建，共享层口径）；本文件新增 §3.26 与 §4 第 72–74 条。
> 文档改完后复跑：同一支探针 **26/26**、RC=0（`Temp/b26-wheel-probe-docs-sync.log`），全量 **2566/0/0**（`Temp/b26-full-suite-docs-sync.log`）——本批各处写下的数字与这两份同源。

> **基线更新（2026-10-12 批次㉗ · #132）**：当前基线 **全量 bUnit 2570 通过 / 0 失败 / 0 跳过**、RC=0（`Temp/b27-full-suite.log`
> ＝ 批次㉖ 的 2566 ＋ 新常驻源码守卫 `VendoredLibraryGuardTests` 的 4 条，单独跑 **4/4** 见 `Temp/b27-vendored-guard.log`）。
> 三次翻面：A 示例地址换回 CDN **2 红 / 2 绿**、B 站内 `hls.min.js` 追加一个空格 **1 红 / 3 绿**、C 移走 `hls.min.js.map` **1 红 / 3 绿**
> （`Temp/b27-flipA-cdnurl.log`、`Temp/b27-flipB-bytemod.log`、`Temp/b27-flipC-filemissing.log`），全部 `cp` 备份 + `md5sum -c Temp/b27-flip-backups.md5` 还原，未用 `git checkout --`。
> 新增常驻探针 `verify-vendored-hls-lib.cjs` **22 项**（**批次83 起为 23 项**：把 `goto` 的等待条件从 `networkidle` 换成显式的"站内库已注入"，
> 并把这一点写成 `A0` 前提证人，见 §3.31 与 §4 第 91 条；下面 22/22 与 13/22 是批次㉗ 当时的读数，不回写）：开发站 5199 **22/22**（`Temp/b27-hls-probe-dev-final.log`）、
> 干净发布包 5200 **22/22**（`Temp/b27-hls-probe-CLEANRELEASE.log`）；翻面**直接用用户部署的那份旧发布包** **13/22**
> （`Temp/b27-hls-probe-FLIP-oldrelease.log`，9 条红点名 CDN 请求与站内文件两条轴，而「库真被 new 出来」的行为证人两版都绿）。
> 主目录探针份数 **252 → 253**。新包回归三份：启动自检 **10/10**、滚轮穿透 **26/26**、日期面板 **16/16**。
> 修法与口径在 **`docs/vendored-library-deployment-alignment.md`**（新建，可部署契约）；本文件新增 §3.27 与 §4 第 75–78 条。

> **基线更新（2026-10-12 批次㉘ · #133/#134）**：当前基线 **全量 bUnit 2580 通过 / 0 失败 / 0 跳过**、RC=0（`Temp/b28-tests-clean.log`
> ＝ 批次㉗ 的 2570 ＋ 新常驻 `DateTimePickerCloseOnDatePickTests` 的 10 条，单独跑 **10/10** 见 `Temp/b28-tests-newguards-1.log`）。
> 七次变异 M1 **7 红/44**、M2 **4/47**、M3 **7/44**、M4 **1/50**、M5 **3/48**、M6 **1/50**、M7 **1/50**（`Temp/b28-mut-M1..M7.log`），
> `cp Temp/b28-backup/*.cs` ＋ `md5sum -c Temp/b28-backup/md5.txt` 三份 OK 还原，未用 `git checkout --`。
> 新增常驻探针 `verify-datetime-close-on-date-pick.cjs` **30 项**：**30/30**、RC=0（`Temp/b28-probe-run6.log`，三次翻面还原后复跑 `Temp/b28-probe-restored.log` 同为 30/30）；
> 探针层三次翻面 **21/30**（改前档）、`close` 撤掉后收起判据红且下游超时（`Temp/b28-probe-flip-P2.log`）、透传撤掉后只有表格段红（`Temp/b28-probe-flip-M7.log`）。
> 主目录探针份数 **253 → 254**。⚠️ `Temp/b28-tests-full-2.log`（2 失败）那份**不作数**：`dotnet test` 与变异重建撞在同一时间窗，编的是被改坏的源码（§4 第 81 条）。
> 修法与 EP 口径在 **`docs/datetime-picker-alignment.md`**「点日期这一步：即写值（对齐 EP）与 CloseOnDatePick（本库扩展）」；本文件新增 §3.28 与 §4 第 79–83 条。
> 文档改完后复跑：同一支探针 **30/30**、RC=0（`Temp/b28-probe-docs-sync.log`，改完引用后再复跑一份 `Temp/b28-probe-docs-sync-2.log` 同绿），全量 **2580/0/0**、RC=0（`Temp/b28-tests-docs-sync.log`）——本批各处写下的数字与这两份同源。

> **基线更新（2026-10-12 批次㉙ · #138）**：当前基线 **全量 bUnit 2585 通过 / 0 失败 / 0 跳过**、RC=0（整份输出 `Temp/b29-tests-full-clean.log`；
> 同一轮截断件 `Temp/b29-tests-full.log` 读数一致）＝ 批次㉘ 的 2580 ＋ `VendoredLibraryGuardTests` 新增的 5 例（判据④ Theory×4 ＋ 前提证人 1），
> 判据类单独跑 **9/9**（`Temp/b29-tests-newguard-2.log`，全部还原后再复验一次同为 9/9 —— `Temp/b29-tests-restored.log`）。
> 四次翻面 **M1 2 红/7 绿**（注释掉那行规则）、**M2 2 红/7 绿**（规则写成过宽 `** -text`，红的是反向对照）、**M3 2 红/7 绿**（解析器改成先命中优先）、
> **M4 1 红/8 绿**（前提证人改指 LF=0 的 `.map`），日志 `Temp/b29-mut-M1.log`…`-M4.log`；`cp Temp/b29-backup/*` ＋ `md5sum -c Temp/b29-backup/md5.txt` 两份 OK，未用 `git checkout --`。
> ⚠️ 本批第一次 `cp` 备份取在**修好之前的那一版**上，还原时把两处未提交修复一起退掉了 —— `md5sum -c` 当时报的 `FAILED` 就是信号，别当噪声（§4 第 86 条）。
> 修法与量数在 **`docs/vendored-library-deployment-alignment.md`** §9；本文件新增 §3.29 与 §4 第 84–86 条。探针份数未动（本批没有新探针，仍 254 份）。
> 文档改完后复跑：判据类 **9/9**（`Temp/b29-tests-docs-sync.log`），全量 **2585/0/0**、RC=0（`Temp/b29-tests-docs-sync-full.log`）——本批各处数字与这两份同源。

> **基线更新（2026-10-18 批次83 · #145–#148）**：当前基线 **全量 bUnit 2709 通过 / 1 失败 / 0 跳过 / 总计 2710**、51 s（整份输出 `Temp/b53-tests-full.log`，GBK 控制台落盘、UTF-8 转写件 `Temp/b53-tests-full.utf8.log`）。
> **这一条是有 1 个红的基线，别把它当"全绿"**：唯一红 `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows`
> （`Assert.IsType()` 期望 `Element.ElementException`、实得 `null`，测试文件 110 行）**不是本批引入的**——改名前最近一次全量 `Temp/b52-tests-full-1.log`
> 就是 **2707 通过 / 3 失败 / 总计 2710**，同名同断言，落在用户未提交的 `ElTable` 无 property 列改动上；那 3 红里另外两条
> （`ElFormGeneratedControlTests.GeneratedMentionSelectsOptionAndUpdatesModel`、`ElInputNumberTests.ClickAfterHoldIsSwallowed`）本轮皆绿，仍是负载偶发口径。
> 主目录探针份数 **254 → 255**（新增常驻守卫 `verify-package-asset-paths.cjs`，14 项判据）。
> 一次尺子红：改前 **12/13、RC=1** 的红在 A5a——探针自己去取基路径清单，把"运行期资源请求零旧前缀"的快照污染了；修尺子（先取快照再自取，脚本 39–42 行注明原因），断言没放宽 ⇒ 改后 **14/14、RC=0**（`Temp/b53-asset-path-probe.log` / `-probe2.log`）。
> 另一处尺子改动：`verify-vendored-hls-lib.cjs` 的 `goto` 等待条件由 `networkidle` 换成显式"站内库已注入 `window.Hls`"并新增前提证人 **A0**，项数 **22 → 23**
> （改前 RC=2 超时 / 改后 **23/23、RC=0**，归因档 `Temp/b53-diag-videopage.log`，见 §4 第 91 条）。
> 六个常驻探针复跑全绿：lang-i18n 21/21、lang-picker-family 34/34、tablev2-expand 9/9、treev2-scrollbar 7/7、tour-hollow-mask 57/57、datetime-close-on-date-pick 30/30。
> 本批口径与实测数字在 **`docs/package-identity-alignment.md`**（新建）；本文件新增 §3.31 与 §4 第 89–93 条。
> 追加（同日用户指令「`nuget/` 全部清空，重新打包」）：三包重打 RC=0、**尺寸逐一只一致而六只 md5 全变**（zip 时间戳，包内条目内容 0 处不一致），
> 清缓存重 restore 后 cache↔feed 重新对齐、消费侧端到端 **12/12**（`Temp/b53-repack-*.log`，§4 第 92 条）；
> 演示站发布包同步重打并裸静态托管复验（5202）：启动自检 **10/10**、基路径守卫 **14/14**、滚轮 **26/26**、日期写值 **30/30**、日期面板 **16/16**、MSE 库 **23/23**，
> 首轮 7/10 的三条红是 `Temp/` 里那份一次性脚本自己写死旧路径（§4 第 93 条）；交付 zip 388 条目、22,953,677 B、md5 `aff83b3e6883905526807ee10de0dd55`。

> **基线更新（2026-10-24 批次100 · #166–#169 → 2026-10-27 批次101 · #170–#177）**：当前基线 **全量 bUnit 2824 通过 / 0 失败 / 0 跳过 / 总计 2824**、RC=0（`Temp/b101-bunit-full-4.log`，2026-10-27 收尾复跑：`2819` ＋ 并发会话批次102 的 +2、批次104 的 +3）。批次101 自己那档是 **2819/2819**（`Temp/b101-bunit-full-3.log`）。`dotnet test` 重建 Debug 产物后，常驻 `verify-ofd.cjs` 在 5199 复跑仍 **78/78、控制台 0 错误**、RC=0（退出码与 `PASS`/`FAIL` 计数同档可查：`Temp/b101-baseline-verify-ofd-2.log`，`PASS` 行 78 / `FAIL` 行 0 / 尾行 `PROBE_RC=0`；同轮首档 `Temp/b101-baseline-verify-ofd-1.log` 只记了读数没记退出码）；主目录常驻探针 **257 份**（§3 标题的重数口径），`verify-ofd.cjs` 从 46 → 61（批次100）→ **78 项**（批次101）。
> **再更新（2026-10-27 批次105 · #178/#179）**：当前基线 **全量 bUnit 2836 / 0 失败 / 0 跳过 / 总计 2836**、`TEST_RC=0`（`Temp/b105-bunit-full-1.log`）＝ 上面那份 2824 ＋ `ElTableFormatterColumnTests` 新增 12 例（2824 + 12 = 2836，账能对平）。同批新增常驻探针 `verify-table-formatter.cjs`（14 项，绿 `Temp/b105-probe-green-1.log` ＋ 还原复跑 `-green-2.log` 各 14/14、RC=0；库级翻面档 **通过=10 失败=4、RC=1**，`Temp/b105-probe-red-1.log`），主目录 `.cjs` 重数 **257 → 258 份**（`find demo/tools/probes -maxdepth 1 -name '*.cjs' | wc -l` 于 2026-10-27 重跑；`attic/` 仍 52 份）。同批表族回归 7 支全绿（`Temp/b105-regress-*-1.log`，每档尾行都带 `PROBE_RC=0`：54 / 17 / 30 / 18 / 8 / 7 / 5 条 PASS）。
> **批次83 上面记的那条「有 1 个红的基线」已经在批次101 结掉（#177），别再照着它当红灯噪声**。真身是**夹具过期**不是组件回归：守卫在批次91-95（提交 `61a990d`）按 EP 口径放宽成「`ElTableTemplateColumn` **或**带 `ChildContent` 的列」都不强制 `Property`，而 `PlainColumnWithoutPropertyThrows` 的坏档一直用**带模板**的 `ElTableColumn` ⇒ 放宽后它根本不抛，`Record.Exception` 拿到 `null`，`Assert.IsType` 从 2026-10-21 起每条全量都红在这里。
> 修法是把坏档改窄成真会踩守卫的那一档（无 `Property`、无 `ChildContent`、`Type` 非 index/selection），**并另立 `PlainColumnWithChildContentDoesNotThrow` 把 EP 的豁免档钉住** —— 这两支是一对反向判据：豁免档红＝有人把守卫又收紧回去伤到合法写法，坏档红＝守卫真失守。账算平：`2815（批次100）+ 3（本批卡片三例）+ 1（豁免档新例）= 2819`。
> 详见 `docs/viewer-alignment.md` §13.3；本文件新增 §4 第 114–116 条（116＝交付物 vs 磁盘逐字节对账那把尺子）。

## 1. 十条常驻守卫（改坏了会立刻红）

| 守卫 | 位置 | 判据 | 挡住的真实事故 |
| --- | --- | --- | --- |
| **R1 渲染树序列漂移** | `test/Element.ComponentTests/RenderTreeSequenceScanner.cs` + `RenderTreeSequenceGuardTests.cs`（8 例） | 手写 `RenderTreeBuilder` 用 `seq++` 连续编号，且**引用捕获帧之前存在条件帧**（`if`/`else`/`switch` 分支里的帧发射，含 `if (x) { builder.Add…(); }` 单行写法） | `ElButton` 一进 loading 整页崩：条件帧增减让捕获帧序列漂移 → `NotImplementedException: Unexpected frame type during RemoveOldFrame: ElementReferenceCapture` |

| **R2 捕获后加属性** | 同上文件 | 同一元素内属性帧出现在引用捕获帧**之后** | `ElTreeSelect` 一用声明式 `TreeContent` 就抛 `Attributes may only be added immediately after frames of type Element or Component`（`Data` 路径不崩） |
| **等待必须显式超时** | `TimingGuardTests.cs`（5 例）+ `TestWait.cs` | `.WaitForAssertion/State/Element(` 的实参里没有 `timeout` | bUnit 默认只等 1 秒，全量并行时 CPU 争抢 → "单类必过、全量偶发红"的假失败淹没真缺陷 |
| **探针卫生（attic 判据机器化）** | `ProbeHygieneGuardTests.cs`（7 例） | ① 连本地演示站的探针必须能覆盖地址（`process.argv[2]` 或 `process.env.*BASE`，写在注释里不算）；② `attic/` 里的脚本不得被 CHANGELOG/docs 点名；③ 探针不得拿 `#app` 当就绪判据 | 防 attic 治理缓慢回退：新会话又写死端口、或存档脚本复活成回归资产却还躺在 attic |
| **词条资源体检** | `LangResourceGuardTests.cs`（7 例） | ① `wwwroot/lang/{zh-CN,en-US}.json` 拍平后键集必须互为相等；② 源码里引用的每个 `el.*` 键必须在**两份**文件里都存在且值非空白；③ **同一个键在两份语言包里的占位符集合必须相同**（不同＝切语言时某一边会露出 `{pager}` 这种字面量）；④ **引用了带占位符的词条却没传对应参数**（按"键 → 该键各处调用片段"扫，实参写在下一行也算）；⑤ 覆盖断言（引用点 ≥12、词条数 ≥30、被引用键里至少 3 条带占位符），否则这些判据是零输入空转 | 假接入：组件写了 `T("el.xxx")` 但两份语言包里没这条 → 无障碍标签静默掉回 fallback，切语言时看起来"没生效"。判据是纯函数 `Audit` + 注释掩码 `MaskComments`（文档里举例写的 `el.xxx.yyy` 不算引用，实测误报过一次），两侧夹具（坏数据必被抓、好数据不误报）都在同一文件里；插值两条还专门验了"实参写在下一行的跨行调用"不被误报 |
| **浮层队列归属** | `PopupHostScopeTests.cs`（5 例） | ① `AddElementServices()` 里 **7 个**持浮层队列的服务（`LoadingService` / `MessageService` / `NotificationService` / `DialogService` / `PopupService` / `MessageBox` / `ElDialogGrowService`）必须全是 `Scoped`；② 同一个 `ServiceProvider` 开两个 scope，服务实例与 `LoadingOptions` 集合都要 `NotSame`，同时**正向对照** `Assert.Same`（作用域内共享，否则只是"每次都新建"）；③ 同作用域第二个 `<ElPopup />` 必须被 `PopupHostCount` 数出来；④ 挂载点扫描器的**双侧夹具**（注释/`<code>` 里的 `<ElPopup />` 不算、真挂载必须算）；⑤ `demo/**/*.razor` 里宿主只能出现在 `App.razor` | 两个静默事故（都不抛异常、0 个 JS 错误）：`LoadingService` 注册成 Singleton → 跨连接接管遮罩（关不掉 / 后续不出现）；演示站 `LoginLayout.razor` 多挂一个宿主 → 站内访问过 `/login` 之后整站浮层不再出现。机制、取证与"明确没做的事"见 `docs/popup-host-isolation.md` |

| **源码编码（只许 UTF-8）** | `SourceEncodingGuardTests.cs`（4 例） | ① `src/ demo/ test/ docs/` 下所有文本文件必须能被 `new UTF8Encoding(false, true)` 解出来（GBK 存盘必红）；② `src/**/*.cs`、`*.razor` 不得含 U+FFFD（`EF BF BD`）；③ 双侧夹具：`{BF C6 C8 CF}`（"确认"的 GBK 字节）必须抛、同词 UTF-8 必须解出，并断言**宽松解码不抛**（这正是编译器 ANSI 兜底让缺陷静默的机器版本）；④ 覆盖断言 ≥700（2026-10-09 实测 **1595**：src 762／demo 574／test 152／docs 94／仓库根顶层 13；取数是临时抬高下限逼 `files.Count` 进失败消息）且必须包含 `TableRender.cs`、守卫自己、以及根层的 `CHANGELOG.md` | 2026-10-08 实测清理：30 个纯 GBK 文件（`src/Components` 15、`src/Markdown` 14、`test/TabTest.cs`）、`Element.xml` 里 12 个第三字节被写成 `?` 的汉字、3 个文件 9 行已经塌成 U+FFFD 的注释。这类文件在中文 Windows 上**编译 0 错误、运行时字符串也对**（Roslyn 无 BOM 时先按 UTF-8 解、解不动退回 ACP=936 再解），只有换代码页的机器/编辑器/注释 XML 会看到乱码，所以从来没有被任何检查发现。分类判据与三类损坏的修法见 `docs/source-encoding.md` |

| **/grow 章节闸门结构** | `GrowSectionGateGuardTests.cs`（5 例） | ① `.demo-section` 数 == `@if (builtSections > k)` 闸门数 == `GrowSectionCount`；② 闸门索引恰好 `0..N-1` 且按源码顺序升序（无重复无缺口）；③ 每道闸门往上最近一条非空行必须是 `<h3>`（标题永不参与闸门）；④ 泵用独立 `CancellationTokenSource`、`Dispose` 里 `Cancel()`、只在首渲染起一次；⑤ `GrowEagerSections < GrowSectionCount`、`GrowSectionChunk` 钉在实测最优值 **5**。覆盖断言：扫到的章节数 ≥20，否则判据失效先炸 | 两个静默事故（都不报错、页面看不出异常）：加/删一节却没同步闸门与常量 ⇒ 序号 ≥ `GrowSectionCount` 的那一节**永远不渲染**，内容静默丢失；把 `GrowSectionChunk` "顺手优化"成 1（看着更平滑）⇒ 同窗口实测长任务合计 **+70~90%**、落定多花约 3 秒（每多渲染一次就要整页重跑一次 Blazor 差分）。机制、三臂实测与作废留档见 `docs/grow-progressive-section-build.md` |

| **ElAnchor 指示条口径** | `AnchorMarkerCaliberGuardTests.cs`（16 例） | ① CSS 五条：marker 基态 `opacity:0`（官方靠 JS 写 `opacity:1` 才显形）、竖向 `4×14 / top:8 / left:0 / transition top`、横向 `20×2 / bottom:0 / transition left,width`、根列表缩进 `var(--el-anchor-padding-indent, 14px)`、`.el-anchor__cursor` 也过渡 `top`（CSS 等 JS 写 top，两条必须一起动）；② JS 七条：`elementAnchorScrollTo` 段（去注释后）**不含** `replaceState`/`pushState`、调用 `elementSetScrollTop` 必须带 behavior 实参、点击滚动要开 `elementAnchorBeginSuppressScroll`、`scrollend` + `setTimeout(release` 两条解除路径都在、`update()` 里抑制闸必须排在 `links.forEach` **之前**、`elementAnchorUpdateMarker` 要含 `:scope > .el-anchor__marker`/`:scope > .el-anchor__cursor`/`requestAnimationFrame`/四个 `marker.style.*`/清空分支 `opacity = ""`/按 `getAttribute("data-anchor-href") === href` 比对、`window.elementSetScrollTop =` 全库只许 1 处且签名是 `(container, top, behavior)`、ElTreeV2 用改名后的 `elementSetElementScrollTop`；③ .NET 一条：`ScrollToAsync` 与 `HandleLinkClickAsync` **两条路**都要既 `SetActiveHrefAsync` 又刷 marker（`elementAnchorUpdateMarker` 恰好出现 2 次）；④ DOM 结构三条：marker/cursor 必须是根的直接子且不落在 `.el-anchor__list` 里（`:scope >` 的取节点契约）、链接必须输出 `data-anchor-href` | 「激活的是章节 C，蓝线却在章节 A 那一行」（JS 从不写 style、CSS 写死 `top:8px`）、「点了 C 亮的还是 A」（缺官方 `isScrolling` 闸；容器滚到底时目标被 max 夹住，实测 82px vs 阈值 75px 永远算不回来）、「红色的线离字太近」（`left:-12px` + 列表不缩进）、点完锚点 URL 掉到站根刷新回首页（`replaceState` 遇 `<base href="/">`）、以及**平滑滚动静默失效**（`window.elementSetScrollTop` 被定义两次，后定义的 ElTreeV2 两参版覆盖了带 behavior 的三参版）。取证与实测数字见 `docs/anchor-alignment.md` §5–§7 |

| **ElColorPicker 页脚与触发器图标口径** | `ElColorPickerTests.cs`（10 例，批次⑰新增 5 例） | ① `Clearable` 默认必须是 `true`（EP `color-picker.ts` 的 `clearable: true`）⇒ 没写参数的色板页脚也有「清空」；② 显式 `Clearable="false"` 时页脚不许有「清空」但仍有「确定」（负向断言旁边配同前提的正向对照）；③ 页脚两个按钮必须是 `button.el-button` 且带 `el-button--small` / `is-text` / `is-plain`，同时 `index.css` 里**不得**再出现 `.el-color-dropdown__btn{` 或 `.el-color-dropdown__link-btn{`（尺寸只由 `.el-button` 出，遗留规则是 51px vs 24px 的成因）；④ 触发器图标只能是 `el-icon-arrow-down`（不能是 `el-icon-close`），**点它必须开面板**而不是清空（`OnClear` 不触发、绑定值不变、`PopupService` 里确实登记了浮层）；⑤ 无色值时「清空」仍不禁用 | 「清空不起作用」的两条真身（默认值与 EP 不一致 ⇒ 页脚压根没按钮；图标吃掉首点 ⇒ 实测 6/6 点成清空）与「按钮大小和旁边的不一样」（遗留 element-ui 尺寸规则，实测「清空」h=51 对「确定」h=24）。6 条翻面变异各自咬到哪条用例见 `docs/color-picker-alignment.md` §5 |

另有两类**按组件的兼容性黑名单守卫**
（CSS / `dom.js` 文本扫描）：`ElFormFieldGrowTests.Css_FormFieldSection_AvoidsUnsupportedCssFeatures`、`ElMarkdownGrowTests.CssUsesOnlyWidelySupportedFeatures`、`ElHtmlEditorGrowTests.JsChannel_IsWrittenInEs5Syntax` / `JsChannel_UsesOnlyBrowserBuiltIns`、`ElViewerGrowTests.Guard_OfdViewerChannel_IsEs5Safe`。它们的公共底座是 `TestJsScanner.cs`（引号感知的注释剥离器，6 例自测）。

> ⚠️ 这类守卫**按组件取文本段**，批次⑧在这里连撞两次（取证与修法见 `docs/caret-scrollbar-focus-ring-alignment.md` §3.4）：
> ① `ElMarkdownGrowTests.CssUsesOnlyWidelySupportedFeatures` 的取段范围是「段标记 → **文件末尾**」，
> 所以写在 `index.css` 尾段的任何新 CSS 都自动落进那条黑名单 —— `:focus-visible` 直接判红，带这类特性的段落要挪到文件前半部；
> ② 段标记用的是「**第一次**命中的字面量」，所以别的段落只要在注释里提一次那个组件的类名前缀，标记起点就被拉到那里、
> 把无关段落划进守卫范围（第一次就是这么红的，报错位置 `pos 77`）。写跨段落注释用描述性说法，别抄类名。

### 写守卫的五步配方（本仓血泪，任何"扫源码文本"的守卫都照这个来）

1. **先掩码再判定**：注释与字符串/字符/逐字串内容替换成**等长空格**（偏移、行号不变）后再跑正则。三条兼容性守卫曾因为中文注释里写反引号、写"对齐 Radzen …"而长期误报；新写的守卫也被自己的夹具抓出过同样问题。**反方向同样致命**：过度剥离＝假绿（旧 `StripComments` 按行切 `//`，字符串里一个 `"https://…"` 就把该行之后的真代码整段吞掉）。
2. **两侧夹具**：每条规则同时要有「必须抓到」和「不许误报」样本。
3. **覆盖断言**：要求扫描器报告它真的看到了足量目标（`TimingGuard` 要求全工程 ≥60 处 `WaitFor`、渲染树守卫要求 >400 个源文件），否则"0 违规"只是正则失效的同义词。
4. **反向验证**：临时注入一条真违规 → 守卫必须变红；撤掉 → 复原并核对文件字节数。已用过两次：往 `dom.js` 插一行真 ES6 `var x = function (a) { return (b) => b + a; };` → ES5 守卫立刻红。

5. **扫"整个工作树的文件"还要加一条：只读打开必须容许别人正在写。**
`File.OpenRead` 的共享模式是 `FileShare.Read`（＝"别人也只许读"），只要有一个进程正开着写工作树里某个会被扫到的文件
（本仓很常见：探针往 `demo/tools/probes/*-result.txt` 写结果、脚本开着输出文件），它就直接抛
`IOException: being used by another process` —— **而且每次必红**，报的还不是判据要报的东西。
所以用 `new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)`（本仓的 `OpenShared` / `ReadAllBytesShared`）。
顺带一条读结果的方法：**先分异常类型再动判据** —— `IOException`＝工具坏了，`DecoderFallbackException`/断言失败＝仓库真坏了，
两者长得一样（都红）但结论完全相反。实测：同一场景连跑 6 次 6 次 `失败数: 2` → 换成 ReadWrite 共享后连跑 6 次全绿（`docs/source-encoding.md` §4.1）。

## 2. 怎么跑

```bash
# 组件库 + 全量测试（dotnet 必须带全套 env，否则 NuGet 在 Path.Combine(null) 崩）
env APPDATA="C:/Users/Administrator/AppData/Roaming" LOCALAPPDATA="C:/Users/Administrator/AppData/Local" \
  ProgramData="C:/ProgramData" SystemRoot="C:/Windows" SystemDrive="C:" USERPROFILE="C:/Users/Administrator" \
  ALLUSERSPROFILE="C:/ProgramData" PUBLIC="C:/Users/Public" ProgramFiles="C:/Program Files" \
  "ProgramFiles(x86)=C:/Program Files (x86)" CommonProgramFiles="C:/Program Files/Common Files" \
  "CommonProgramFiles(x86)=C:/Program Files (x86)/Common Files" \
  TEMP="C:/Users/Administrator/AppData/Local/Temp" TMP="C:/Users/Administrator/AppData/Local/Temp" \
  NUGET_PACKAGES="C:/Users/Administrator/.nuget/packages" \
  "C:/Program Files/dotnet/dotnet.exe" build src/Components/Element.csproj -c Debug --nologo \
    -nodeReuse:false -p:EnableSourceLink=false -p:DeterministicSourcePaths=false
```

- **`dotnet` 的 stdout 在 Bash 工具里会被吞成 0 字节**（不只是 `dotnet test`，批次⑭ 实测 `dotnet build` 也是 RC=0 / 日志 0 字节，
  于是「编译其实没跑」看起来像「编译成功」）。必须用 Python 子进程捕获再落盘：`Temp/run-dotnet-test.py <filter> <log>`（测试）、
  `Temp/run-dotnet-build.py <log>`（构建 demo）。**判「真的重编过」还要看产物 mtime**，别只看 RC。
- **`--no-build` 配 `--filter` 会造出假绿**：过滤条件匹配不到任何用例时 `dotnet test` **返回 0**，stdout 只有"总共 1 个测试文件与指定模式相匹配"；
  而 `--no-build` 用的是**上一次成功构建的 DLL**，本批就出现过"编译其实失败（error CS1061）→ 测试 RC 仍 0"。
  所以判绿要读**通过/失败/总计那一行**（并核对总数等于当前基线），不能只看退出码。
- **改完源码或测试文件先 `touch` 再 build/test**：MSBuild 增量有时不重编，症状是"改了却没生效、编译仍 0 错误"。
  **还原文件也算改动**：用 `.bak` 覆盖回去（`shutil.copy` + `shutil.move` 会保留原 mtime）同样骗得过增量编译，
  于是"变异体还原之后"的那次测试跑的仍是变异体 DLL —— 本批实测还原后仍报 `失败 1`，`touch` 之后才 53/53。
  变异法必须"改 → 跑 → 还原 → **touch** → 再跑一遍确认绿"。
- **demo 站点**：`demo/Element.Blazor.Demo`（WASM），端口固定 `http://127.0.0.1:5199`。重启前先 `netstat -ano | grep :5199` 杀掉旧实例（旧实例会继续服务旧构建，极易误判），再 `dotnet run`（同上 env 前缀），curl 到 200 才开始测；本地 curl 要 `--noproxy 127.0.0.1`。
  批次⑭ 实测：**`nohup dotnet run &` 在 Bash 工具里起不来**（进程消失、日志 0 字节、5199 无 LISTEN），要改用 Python
  `subprocess.Popen(..., creationflags=DETACHED_PROCESS)` 起服务并落日志（`Temp/` 里那次起服务的写法），起来后仍要 `netstat` 见到 LISTEN 再 curl。
  **验收「改动是否真进了被服务的包」**：改 `.cs` 后除了看 `Element.Blazor.Demo.dll` mtime 变新，还要直接 grep
  WASM —— `bin/Debug/net10.0/wwwroot/_framework/Element.Blazor.Demo.<hash>.wasm`，新标识符（批次⑭ 是 `seededMounted`）
  能在里面命中，才算「浏览器拿到的是这版代码」；`curl -o /dev/null -w "%{size_download}"` 对同一个 hash 能取到整份也作旁证。
- **改完 CSS 要重启 demo 服务**再验收。

## 3. 浏览器探针（`demo/tools/probes/`：主目录 `.cjs` 实测 **258 份**（2026-10-27 批次105 重数，`find demo/tools/probes -maxdepth 1 -name '*.cjs' | wc -l`；批次101 那轮是 257，本批新增 `verify-table-formatter.cjs` 一支）+ `attic/` 52 份 `.cjs`（另有 README.md））

> 主目录份数口径：`cd demo/tools/probes && find . -maxdepth 1 -name '*.cjs' | wc -l`（2026-10-12 批次㉑ 复测 **250**、`attic/` **52**；
> 批次㉑ 新增 2 份：`verify-cascader-panel.cjs`（**5 项**，/cascader 多选面板判据：勾两片叶子后全部 `li` 仍带
> `el-cascader-node` + `role="menuitem"`（换代 key 对增量 diff 丢属性帧的免疫判据，改前 diag3 抓到裸 `<li aria-checked="">`）、
> 两片勾中叶子均 `is-active is-checked` 且 700/`rgb(64,158,255)`、标签容器 `el-cascader__tags` 文本无 120px 顶且未省略、
> 全程 0 页面错误；**5/5** 见 `Temp/b21-cascader-probe-resident.log`）
> ＋ `verify-popover-actions.cjs`（**4 项**，/popover 操作面板判据：面板打开带标题、点「确认」回显变化 + 面板收起、
> 重开后点「取消」同样、0 页面错误——#110 在批次⑰ `ced0a51` 已接线修复，本探针是该修法的绿基线尺子；
> **4/4** 见 `Temp/b21-popover-probe-resident.log`，截图 `demo/tools/probes/shots/b21-cascader-verify.png` / `b21-popover-verify.png`，
> 见 `docs/cascader-alignment.md` 批次㉑记录与 #106/#110）。
> 批次⑳ 新增 1 份：`verify-colorpicker-drag.cjs`（**43 项 / 7 段**，/colorpickerpanel 饱和度面板拖动判据：280×180 面板完整 + 四张透明 range 全部
> `pointer-events:none`、按下即跳位 + 6 步拖动两轴 ±1.5px 逐点跟随 + 单调无回弹 + 松手不回跳 + sat/val 写回宿主 + 焦点落在 saturation、
> 越界 4 步钳到角落（pointer capture 的判据）且松手后保持、hue/alpha 与 EP thumb 宽公式**零容差**相等、neutral click 后方向键仍能 +1（透明 range 仍是键盘把手）、
> 单击落点 34%/66% + 宿主读数变化、全程 0 控制台错误；**43/43** 见 `Temp/b20-cp-probe-2.log`，
> 浏览器翻面 M5 去掉 click 聚焦 → B7 红 42/43（`Temp/b20-mut-m5-probe.log`），
> 见 `docs/color-picker-panel-alignment.md` 与 #108/#118）。
> 批次⑯ 新增 2 份：`verify-batch16.cjs`（**41 项**，「反馈组件」11 报的逐项判据：L0–L5 区域 loading 遮罩矩形＝目标矩形且整屏仍 fixed、
> N1–N4 通知右下/右上/左下定位与 primary 图标、P1–P6b popover 的 dark 主题 + hover 默认 + 点外收 + 点面板内不收、
> S0–S5 选择 v2 的标签回显 + 点外收 + 控件内对照、SP1–SP2 纵向子项左边缘与相邻按钮对照、T1–T2 内置「+」移除与外部代码新增、
> TT1–TT4 tooltip 悬停即现/transform 真插值/移开即消/click 路径对照、TR0–TR2 穿梭框逐标签的左右移动、U1–U3 粘贴上传 + 区块隔离，
> 外加 Z0/Z0b 全程 0 pageerror；终局 **41/41**，见 `Temp/b16-verify-run9.log`）
> ＋ `verify-datepicker-panel-sidebar.cjs`（/datepickerpanel 那条 110px 宽、整页高的白侧栏带逃逸进页面的判据，见 #86）。
> 上一记录 **239** 是同日批次⑮ 时点值，该批新增 1 份：`verify-checkbox-indeterminate.cjs`（**16 项**，/checkbox「半选状态」的显示判据：载入即横线（`is-indeterminate` +
> `::before` 白条几何 + `::after` 隐藏 + `aria-checked="mixed"`）、半选→打勾→空的点击链、点子项时宿主重算半选，
> 外加 B1/B2 一正一负的 CSS 通路对照与 T0 勾判据自证；改前 **7/9**、改后 **16/16**（连跑两次）、把初始态改成 4/4 的浏览器翻面 **9 项红**，
> 见 `Temp/b15-probe-{before,after,after-r1,after-r2,mutation}.log` 与 `docs/checkbox-alignment.md` §3）。
> 上一记录 **238** 是同日批次⑭ 时点值，该批新增 1 份：`verify-demo-tab-mount-stall.cjs`（**9 项：V1–V8 + ENV 环境体检**，演示外壳开/切/悬停页签的主线程不变量：开轻页的阻塞不随缓存页数放大、
> 纯 hover 扫页签条 3 轮 0 长任务且必须真的把每个页签都进出过、关页签真摘除实例、静置 0 长任务、全程 0 JS 异常，
> 外加 ENV「测量窗口内逐帧 rAF 间隔」判这一轮计时可不可信（帧数 <30 或中位 >60ms ⇒ 长任务判据不作结论并整体非 0 退出）；
> 闸门版 **9/9**，见 `Temp/b14-probe-gate-env.log` 与 `docs/pro-tabs-alignment.md` §12 —— 浏览器侧判据是**护栏**，
> 货架去重本身的依据是 .NET 侧那 5 例计数用例，翻面没判别成功、且污染因素至今未定位，都如实记在 §12.4 与本文 §4 第 27 条）。
> 批次⑬ 复测是 **237**，该批新增 2 份：`verify-table-tree-caret.cjs`（17 项，树形列箭头判据：本地数据里叶子误标 `HasChildren` 也不该长箭头、
> 有孩子的行**第一击**就 change，含 /table ④⑥ 两棵树与第 2 页分页；浏览器翻面 13/17，见 `docs/table-tree-expansion-alignment.md` §7.3）
> ＋ `verify-demo-menu-order.cjs`（10 项，侧栏「反馈组件」之后 80 条按英文名首字母升序；翻面 7/10，见 `docs/pro-tabs-alignment.md` §11）。
> 批次⑫ 复测时是 **235**，该批新增 2 份：`verify-markdown-scrollbar.cjs`（本批 /grow 编辑器滚条判据，20 项，含两栏各一对静止/悬停像素对照与一次
> 重新注入 vendor 规则的浏览器翻面）＋ `diag-batch12-grow-scrollbar.cjs`（改前几何 dump：`CodeMirror-scroll vSlot:6 hSlot:10`）。
> 上一记录 **233** 是同日批次⑪ 时点值，**209 → 233 这段差额是跨三批的**：
> 批次⑨ 的 17 份 `diag-batch9-*.cjs` ＋ `video-media-fixture.cjs`、批次⑩ 的 `verify-scrollbar-wide-h.cjs` /
> `verify-table-tree-onclick.cjs` / `diag-table-tree-clicks.cjs`、批次⑪ 的 `verify-tour-hollow-mask.cjs`（本批判据，57 项，
> 含洞边界与「flip 后写回 `bottom`」两处浏览器翻面）＋ `diag-batch11-i18n-hole.cjs`（`/i18n` 镂空洞的定位 dump：
> 把祖先 `transform/filter/zoom/will-change/contain/perspective` 逐层打出，结论是没有 stacking context 肇事、
> `hitAtTargetCenter` 命中的就是目标本身）；同期另有 4 份主目录脚本被归档进 `attic/`
> （`diag-scrollbar-hbar` / `diag-selector-presence` / `diag-table-double-click` / `measure-scrollbar`），
> 所以净增不是一次直加 —— **这 ±1 的残差我没有逐条对账**，只按命令读数记录，不硬凑成一个自洽的账。
> ⚠️ 新增 `.cjs` 之后**先跑 `ProbeHygieneGuardTests` 再跑全量**：批次⑪ 就是被它抓到新探针写死了演示站地址（§4 第 22 条）。
> 上一记录 209：2026-10-09 第十二批（排序箭头盒 / 面板滚条槽 / 开关焦点环）复测；
> 上一记录 205 之后新增的 4 份是 `verify-batch8.cjs`（本批判据，C/G/F 三段 17 项，含 4 处翻面 C5/G3/F5+F6/F7 与 F1 正向对照）
> 与 3 份取证脚本 `diag-batch8-detail`（改前现场：行高 51、原生黑环、槽宽 8 vs 4、列差 0/0.9/2.9/4）、
> `diag-batch8-visuals`（三张用户截图对应的 DOM 定位）、`diag-batch8-scrollbar-zero`（「去掉」与「调细」两条写法的同面板实测，
> 见 `docs/caret-scrollbar-focus-ring-alignment.md` §2.3）。口径同上批 §7.5。
> ⚠️ 批次⑧另外**改**了 `verify-batch7.cjs` 一处口径（H2 的箭头命中盒 24×34 → EP 的 24×14），份数不变。
> 上一记录 205：2026-10-09 第十一批（表头单元格结构与列宽图标盒）复测；
> 上一记录 200 之后新增的 5 份是 `verify-batch7.cjs`（本批判据，22 项，含 4 处翻面变异 H4/S4/N4/W4 与 N5 还原对照）与 4 份取证脚本
> `diag-batch7-layout / diag-batch7-wheel / diag-batch7-colwidth / diag-batch6-C`，口径见 `docs/table-header-cell-and-column-width.md` §7.5。
> ⚠️ 200→205 这 5 份里 `diag-batch6-C.cjs` 不是本批新写的功能取证，而是**拆批次⑥假红**用的点位 dump（见 §3.6 第 7 条同一个坑的第二种形态），
> 它和被改的 `verify-batch6.cjs` 一起构成"C 段凭什么改点位"的可重放证据，所以留在主目录不进 attic。
> 上一记录 200：2026-10-09 第十批（弹层定位与滚轮）复测；
> 新增的 8 份是 `verify-batch6.cjs`（本批判据，15 项）、`verify-batch6-docs.cjs`（确认 5199 服务的是重建后的演示程序集）
> 与 6 份取证脚本
> `diag-dropdown-up-wheel / diag-dropdown-up-wheel2 / diag-header-menu-latency / diag-header-menu-profile /
> diag-page-render-cost / diag-grow-wheel-points`，口径见 `docs/dropdown-table-grow-placement.md` §4。
> 上一记录 192：2026-10-09 第九批（表头筛选面板）复测；
> 183 之后新增的 9 份是 `verify-batch5.cjs`（本批判据，14 项）与 8 份取证脚本
> `diag-table-filter / diag-table-current-row / diag-table-current-row2 / diag-table-wheel / diag-table-dropdown-wheel /
> diag-aside-tooltip-clip / diag-batch5-fail / diag-menu-popper-stack`。口径见 §4 第 15、16 条与 `docs/table-filter-alignment.md` §8。
> 上一记录 183 之前那次口径：2026-10-08 第七批复测 **170**；
> 170 → 183 那 13 份按 mtime 归类：第八批（四个宿主的截断悬停 tooltip）8 份
> （`verify-ellipsis-tooltip.cjs` + `diag-eltext-trigger-style / diag-eltext-clip / diag-ellipsis-popper /
> diag-table-popper-clip / diag-tv2-title / diag-page-scroller / diag-flip-scroll`），
> 滚条淡入竞态那批 5 份（`verify-scrollbar-fade-scroll-race.cjs`、`diag-fade-writeback.cjs`、
> `diag-flip-mirror / diag-flip-seq / diag-flip-reset`）。

> 主目录数字相比 §3.1 那次盘点（148）又多了一批：ElInputNumber 对齐的 5 份回归探针
> （`verify-inputnumber-align/-repeat/-icons/-formatter`、`measure-inputnumber-type`）、库级词条的
> `verify-lang-i18n.cjs`，以及第五批日历 / 颜色 / 滑块 / 穿梭框族的 `verify-lang-picker-family.cjs`（见 §3.8b）。
> §3.1 表里那些"195/196"是**那次盘点的时点值**，保留不动，避免把历史证据改成现在的成绩。

### 3.1 盘点结果（2026-10-07 全量体检，非抽样）

| 维度 | 数 | 说明 |
| --- | --- | --- |
| 总数 | **196**（盘点时点 195 + 本次新采集器 1；随后 48 份一次性脚本移入 `attic/`，主目录 148） | 前缀分布：`probe-` 117、`verify-` 44、`diag-` 23、`shot(s)-` 6、其余 `dump-/exp-/gap-/smoke-/survey-/collect-` 各 1–2 |
| 语法健康 | **196/196 通过 `node --check`**（本次 76 份改动后逐份复检） | 无一损坏 |
| 被 CHANGELOG / docs 点名为回归依据 | **101** | 视为「仍在用」清单：`probe-` 60 + `verify-` 30 + `diag-` 6 + 其余 5 |
| 访问 element-plus.org 的**外部基准**探针 | 5 | `probe-ep-button.cjs`、`probe-ep-button-hover.cjs`、`probe-ep-css.cjs`、`probe-button-checks.cjs`、`verify-button-ep.cjs` —— 它们是"实测官网取基准"的工具，不是本地回归，引用 `/component` 路由属正常 |
| 选择器仍全部存在于库/演示站 | **194/195** | 比对基准：源码 class 词表 ∪ 演示站 26 条路由**真实渲染出的 class**（采集器见 §3.4） |
| 唯一过时项 | 1 | `probe-table-editors.cjs` 里的 `.el-picker__popper` 库内已不存在（现契约是 `.el-date-picker__dropdown` / `.el-picker-panel`）—— 该探针这一处查询已失效，其余仍可用 |
| 可覆盖 BASE（`argv[2]` / `process.env.*BASE`） | **主目录 148/148 全部可覆盖**（2026-10-07 整理后） | 整理前主目录 133 份硬编码：被文档点名的 63 份 + 5 份 `verify-*`（未被点名但按命名约定属回归资产）已加覆盖能力；其余 **48 份一次性诊断脚本移入 `attic/`**（内容一字未改）。口径见 §3.5 |

结论：**没有"已失效需要删"的探针**（0 语法坏、只有 1 处选择器过时）；被文档点名的 101 份回归资产**现在全部可以用参数覆盖演示站地址**。口径与现状：`grep -L "process.argv\[2\]|process.env.BASE" *.cjs` ∩ `grep -l 5199 *.cjs` → 还剩 **53 份**硬编码，且这 53 份**全部未被任何文档点名**（一次性诊断存档，保留以便复现历史取证）。整理前是 133 份硬编码，其中被点名的 63 份已全部加上 BASE 覆盖能力。

### 3.2 跑法（统一入口）

```bash
cd demo/tools/probes
NODE_PATH="C:/Users/Administrator/.workbuddy/ce_verify/node_modules" node <probe>.cjs        # 可选参数：BASE，默认 http://127.0.0.1:5199
```

探针内部统一用 `chromium.launch({ channel:'msedge', executablePath:'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe', headless:true, args:['--no-sandbox'] })`。

### 3.3 本会话新增的 9 份（都在 2026-10-07 实测通过，可当写探针的模板）

| 探针 | 验什么 | 上次结果 |
| --- | --- | --- |
| `gap-snapshot.cjs` | 12 个曾用 flex `gap` 的容器**几何快照**（走真实交互：多选勾满逼出换行、开引导、开确认框、下拉表格多选标签），改造前后逐像素比对 | 12/12 子项相对坐标一致 |
| `verify-gap-margin-equivalence.cjs` | **改文件之前**就在同一会话里「注入等价规则 + `gap:normal !important`」预演，把试错代价从改坏 CSS 降到改探针常量 | 首轮 5/13 EQUAL → 据此修正三处 → 全 EQUAL |
| `dump-slider-gap.cjs` | 变体搜索器：同一容器一次试 6~8 种 margin 写法，量化 `dx` 后比较相对间距 | 定出 `.el-slider--with-input` 的等价写法（padding 16 两侧 + `> * + *` margin-left 16） |
| `verify-form-rerender.cjs` | 表单字段键入后父级重渲染不回滚（真实逐字键盘，非 `fill`） | 4/4 PASS、0 JS 异常 |
| `verify-select-slots-video.cjs` | `ElSelect` Filterable 打字收缩、下拉 header/footer 插槽、视频全屏尺寸 | 4/4 PASS |
| `verify-button-loading-diff.cjs` | 按钮进 loading 不再让整页渲染崩（`ElButton` 序列号修复的浏览器侧证据），含连续多轮进出 loading | 8/8 PASS、0 JS 异常 |
| `verify-descriptions-border.cjs` | `ElDescriptions` 按「水平无边框 / 有边框 / 垂直」三类分派断言边框与结构 | 7/7 PASS |
| `verify-tablev2-expand.cjs` | `ElTableV2` 展开箭头契约类名、展开/收回可逆、箭头 90° 旋转矩阵 | 9/9 PASS |
| `survey-gap-pages.cjs` | 侦察型：先摸清哪些 demo 页面真的渲染了目标容器，再决定探针打哪 | — |

既有探针里值得先看的：`verify-fixes.cjs`（表格列宽 / 分页 / 动态列，断言最多的一份）、`verify-pivot-header.cjs` / `verify-pivot-row-align.cjs`（在测试里**模拟浏览器表格布局**，能抓 rowspan/colspan 造成的幽灵列与整行左移）、`verify-scrollbar-thumb.cjs` / `verify-scrollbar-wheel.cjs`（滚动条；量宽度必须 headful，headless 恒为 0）、`probe-csscheck.cjs`（"样式整个丢了"时先查 `styleSheets.cssRules` 数，别急着回退 CSS）。

### 3.4 类词表采集器 `collect-rendered-classes.cjs`（盘点探针用的工具本身）

一个会话内跑完 26 条演示路由、dump 每页真实出现的 class 名 —— 关键是**不重载 WASM**：Blazor 路由监听 `popstate`，所以

```js
await page.evaluate((path) => {
  window.history.pushState({}, '', path);
  window.dispatchEvent(new PopStateEvent('popstate', { state: {} }));   // pushState 本身不派发，必须手动补一发
}, route);
```

即可几十秒扫完全部路由（对比每页重新 `goto` 要等 45~90s 冷启动）。跑法：

```bash
NODE_PATH="C:/Users/Administrator/.workbuddy/ce_verify/node_modules" \
  node collect-rendered-classes.cjs routes.json rendered-classes.json
```

用途：① 判断某探针依赖的 class 现在还在不在（本次 194/195 命中）；② 新组件写完后确认它在演示站真的渲染出来了；③ 因为演示站是 KeepAlive，集合是累积的 —— 正好适合"这个 class 全站还在用吗"这类问题，但不适合判断"某个元素是否只在某页出现"。

### 3.5 老探针的定位与归档规则

- **判据已机器化**：本节三条规则由 `test/Element.ComponentTests/ProbeHygieneGuardTests.cs` 在每次全量测试时检查（连本地站却写死端口、attic 被文档点名、拿 `#app` 当就绪判据，都会让测试红）。已做反向验证：临时注入一份写死端口的探针 → 守卫立刻点名报错；删掉后复原全绿。
  判据的作用域也做了收敛：**不连本地站的探针不适用**（5 份访问 element-plus.org 的外部基准探针与用 `setContent` 喂内联 HTML 的脚本没有端口可覆盖，不该误报）。
- **不删、只挪窝**：48 份未被点名且端口硬编码的一次性诊断脚本已移入 `attic/`（内容一字未改，`attic/README.md` 写了判据与还原方法）。它们是历史取证的原始材料，删了就断了可复现性，所以只隔离不销毁。
- **attic 的边界**：名字是 `verify-*` 但未被点名的 5 份**没有**进 attic —— 按仓库命名约定它们属回归资产，已就地补上 `process.argv[2]` 覆盖能力（其中 `verify-selectv2-navscroll.cjs` 经 A/B 复核结果与改前一致，其"页面本身可滚动"前置断言已随演示站布局变化而过时，重用前先修前置条件）。
- **口径命令**（表格放不下，放这里）：`comm -12 <(grep -L "process.argv[2]|process.env.BASE" *.cjs | sort) <(grep -l 5199 *.cjs | sort)`
- **换端口**：主目录 154 份全部支持 `node <probe>.cjs http://127.0.0.1:PORT`（个别读 `process.env.EB_BASE`），其中新增的 6 份由 `ProbeHygieneGuard` 的 H1 判据机器校验；`attic/` 里那 48 份仍要改文件里那一行，或把演示站起在 5199。
- **唯一确认过时的一处**：`probe-table-editors.cjs` 查询 `.el-picker__popper`，库内已无此类名（现行契约 `.el-date-picker__dropdown` / `.el-picker-panel`）→ 只这一处失效，整份不必废弃；下次跑到它时按新类名改。
- **新写的探针必须**：① 支持 `process.argv[2]` 传 BASE；② 就绪判定等 Blazor 真渲染出的 class（不要等 `#app`）；③ 断言前打印实测值（PASS/FAIL 都带数字），否则失败时无法定位是探针还是组件的问题。

### 3.6 探针脚本自身的坑（逐条都是被抓过才写下来的）

1. `#app` 在静态 `index.html` 里就存在 → 等它等于没等；要等 Blazor 真渲染出的 `.demo-section` / 目标 class，超时 90~150s。
2. `fill()` 只发 `input`，Blazor `@bind` 走 `change` → 造假象；用 `type(...,{delay})`。
3. 异步重渲染后缓存的元素 handler id 会失效：要么抛 `UnknownEventHandlerIdException`，要么**静默点到旧渲染树上**。bUnit 里写 `cut.InvokeAsync(() => cut.Find(sel).Click()).Wait()`；JS 里同理重新 `querySelector`。
4. `getComputedStyle(el).backgroundSize` 返回**不带属性名**的纯值，正则写成 `background-size:` 会恒 `NaN`；`boundingBox()` 只有 `{x,y,width,height}`，**没有 `top`**。
5. `page.evaluate(fn, arg)` 只传**一个**参数，写成 `(a,b)=>` 时第二个恒 `undefined`（会拿"假的空结果"下结论）→ 用对象解构。
6. 多形态组件要**先分类再断言**（`/descriptions` 探针第一版把"垂直"当"水平无边框"，假 FAIL 一次）。遮罩下的控件 `click({force:true})` 也点不到（force 只跳可交互性检查，命中仍被遮罩吃掉）→ `locator.evaluate(el => el.click())`。
7. **量滚轮的点位必须落在真正的滚动容器上**。`verify-batch6.cjs` 的 C3 按面板坐标算出的 `(731,28)` 落在吸顶的
   `.demo-header` 上，而它**不在 `.demo-main` 里** —— 在它上面滚滚轮不带页面滚是**正确行为**，量出来是假红
   （`mainTop 8355→8355`；同屏内容上的 `(1300,200)` 是 `8355→8595`）。判据要用 `elementFromPoint` 现算：
   「在滚动容器内 ∧ 不在吸顶层内 ∧ 不在被测弹层内」（`pickPagePoint()`）。
   同一段还有方向性：页面已贴底还往下滚，滚轮本来就没余量 ⇒ 先看 `scrollTop + clientHeight >= scrollHeight - 6`
   再决定滚轮符号，并把理由写进 detail（`already-at-bottom-so-wheel-up`）。
8. **弹层状态挂在页面字段上，量到的不是组件**。`/table` ⑪ 段把菜单的开合/坐标放在 `TableDemo.razor` 里，
   点一次列头 = 整页 12 段重渲染，实测「点击 → 首个 DOM mutation」**1155 / 1271ms**；搬进独立组件后 **107 / 82ms**，
   同页只重渲染一张表的 ⑫ 是 176 / 214ms，另两处页面级 `StateHasChanged`（隐藏表头 / 展开行）473~557ms ——
   后两组**改前后量级没变**（页面级 473/557 → 519/472，展开行 501 → 532；组件级 176/65 → 214/68），
   正好充当"环境没有整体变快"的对照：只有 ⑪ 那一行掉了一个数量级。量渲染扇出必须
   **同一把尺子在同一个页面上跑多种点击**（`diag-page-render-cost.cjs`），只报一个数字分不出是代码快还是机器快。
9. **观测窗不能按挂钟写死**（批次⑭）。`verify-demo-tab-mount-stall.cjs` 的 V5 第一版是「装 6s `PerformanceObserver` → 扫页签条」，
   而扫描本身要 15~90s（91 步 × CDP 往返），窗口在扫描结束前就关了 → **恒 0 个长任务的假绿**。
   正确写法：装观察器与埋点 → 跑完全部动作 → 再 `disconnect` 读数；并且负向断言必须配一个「动作真的发生了」的正向对照
   （这里用 `over ≥ 3×页签数` **且** `进过的页签 === 页签数`），否则"一次都没扫到"也能交白卷蒙过。
10. **「一趟 91s + 一个 ~1000ms 长任务」是环境噪音，不是代码回归**（批次⑭，代价最大的一条，且结论改过两次）。
    变异构建的 V5 报「单趟 91s、长任务 1009ms」，看着像探针有牙；但同一份**改后代码**的前两轮也卡在 90.8~91.6s/趟。
    当时的第一版解释是「上几轮被手动终止的后台 Playwright Edge 没退干净」（`Temp/list-edge.ps1` 查到一个遗留渲染进程累计 CPU **2015s**；
    只杀 `--user-data-dir` 指向 `playwright_chro…` 临时配置的那棵树，用户自己的 Edge 一棵不动，清掉后单趟降到 **2.8s**）——
    **这个解释不完整**：后来在**零残留**（`playwright` 配置进程数实测 0）下又复现了同一个 91s/趟，
    而「整机 CPU 被抢」与「诊断脚本自己的 CDP Tracing」两个候选也都被受控实验否证（数字与做法见本文 §4 第 27 条）。
    ⇒ 可交付的结论只有一条：**时长类断言读数前必须先判这一轮计时可不可信**，本机现在做法是
    ① 查遗留 Playwright 实例；② 在测量窗口内逐帧记 rAF 间隔，中位帧 >60ms 或帧数 <30 就判「环境不可信」，
    让时长判据不参与结论并整体非 0 退出（`verify-demo-tab-mount-stall.cjs` 的 ENV 项）。
    另：`--disable-backgrounding-occluded-windows --disable-renderer-backgrounding --disable-background-timer-throttling`
    ＋ `page.bringToFront()`，否则窗口被遮挡时 rAF/定时器自己就被节流，"停滞"是浏览器给的。
11. **「挂载已落屏」的就绪门槛要按现场规模定**（批次⑭）。定位脚本初版等「该页节点数 >300」，60s 超时 FATAL ——
    实测 `TourDemo` 只有 79 个节点、`TimePickerDemo` 198 个，永远够不到。现按 `>30` 判"真挂载出内容"，
    再叠加「连续 3 次采样节点数不变」当稳定判据。
12. **给「计时可信度」做判据时，阈值两侧都要有实抓，自己写过的原因也要敢否证**（批次⑭）。
    ENV 的 60ms 不是拍的：污染跑实测中位帧 **1008ms**、健康跑 **8~10ms**（其中一轮 1979 帧），差两个数量级。
    本批为这个污染先后写过三个解释（残留浏览器 / 整机 CPU 打满 / tracing 自身开销），后两个各跑一次就被否证：
    16 个忙等进程吃掉 38.6/60 CPU 秒时探针照样 8~10ms 中位帧；同一份诊断脚本 `trace`/`notrace` A/B 两档都干净。
    污染真身至今**未定位**（要确定性复现得真去锁屏或断会话，属危险操作，没做），文档就按「未定位」写着，
    不拿最像的那个解释充当结论 —— 与 `docs/pro-tabs-alignment.md` §12.4 同一口径。

### 3.7 现役探针实跑结果（2026-10-07，94 份被点名的 `verify-*`/`probe-*` 全跑）

跑法：3 通道并跑 + 失败串行复跑；每份解析自身输出的 PASS/FAIL 行，不猜结论。汇总：
**94 份全部跑完，71 份全绿**（其中 9 份是只 dump 数据、无判定语句的取证脚本），**23 份有 FAIL 或未跑通**。
下面按性质分组——**分组结论比数字更重要**：

**A. 断言腐化（探针自身过时，组件无错）**

| 探针 | 现象 | 判定依据 |
| --- | --- | --- |
| `probe-time-scrollbar.cjs`（F7） | 断言「`fix.css` 存在 4px 轨道规则」「鼠标色 = rgba(0,0,0,.1)」 | `fix.css` 已于 2026-09-26 合并进 `index.css` 并**永久删除**，断言对象根本不存在 |
| `verify-selectv2-navscroll.cjs`（F1） | 前置断言「页面本身可滚动」拿到 `scrollY=0/maxScroll=0` | 演示站布局变化后前置条件不再成立；与改端口无关（A/B 已证） |
| `verify-gap-margin-equivalence.cjs` | 现状 EQUAL=12 / DIFF=1 / ABSENT=1 | 它是「**改 gap 之前**预演等价性」的工具；gap 已全清，其使命完成，DIFF 是已知的容器盒差 ±4px |

**B. 疑似组件缺陷（值得单开一轮，按可信度排序）**

| 探针 | 现象 | 可信度依据 |
| --- | --- | --- |
| `probe-c4.cjs`(F2) + `probe-inputnumber.cjs`(F2) | `ElInputNumber` 的清除图标 `.el-input__clear` **未渲染**，点不出清除 | **两份独立探针报同一现象**，最可信 |
| `verify-verifycode-demo.cjs`(F1) | `InputWidth=200` 未生效，实测宽 267 | 单一探针但量的是几何，可复核 |
| `probe-button-group-checks.cjs`(F2) | 按钮组 Large/Small 实测高度都 32px | 尺寸级联疑未透传 |
| `probe-table-editors.cjs`(F1) | 行内「供应商」选了之后单元格未更新 | 与可编辑表格编辑器映射有关 |
| `probe-nav-collapse.cjs`(F3) | 侧栏展开 220px / 折叠 64px 宽度不符 | 需先确认演示站当前设定 |
| `probe-date-range-panel.cjs`(F2) | 英文面板标签偏一格（期望 2026 September，得 October） | **日期敏感断言**：探针把"当月"写死，跨月必错；先按腐化处理 |

**C. 时序/视口敏感，须单份串行复跑定性**（并行 CPU 争抢或采样到动画中间态，别急着判缺陷）：
`probe-carousel-image`(F1，箭头 opacity 采到 0.525 过渡值)、`probe-selectv2-clear`(F1，叉与箭头中心 x 差 4px)、
`probe-scrollbar-final`(F1)、`probe-tablev2-thumb`(F1)、`probe-carousel-preview`(F2)、`probe-keepalive-perf`(F1)。

**D. 修复后未复跑**：`probe-menu-vertical`、`probe-table-demo`、`probe-keepalive`、`probe-menu-horizontal-nested`、`probe-tablev2`（无判定输出，需单独跑一次看是否只是拿不到环境）。

> **批次㉓ 复核定性（2026-10-12，把上面 B 组逐份重跑，表是 10-07 的快照、不作当前读数）**，日志 `Temp/recheck-b23-*.log`：
>
> | 探针 | 本轮实测 | 定性 |
> | --- | --- | --- |
> | `probe-button-group-checks.cjs` | **15/15 全绿**（`recheck-b23-probe-button-group-checks.log`） | 当初报的「Large/Small 都 32px」已不复现（尺寸级联后来已修），B 组这一条销账 |
> | `verify-verifycode-demo.cjs` | 先 7/8（`recheck-b23-verifycode-demo.log`：`InputWidth=200` 读到 w=267）→ 修尺子后 **8/8**（`recheck-b23-verifycode-fixed.log`） | **组件无错，红在探针按 `nth(4)` 取旋钮**：演示区在「字号」和「输入框宽」之间插过「占位符字号」，序号漂移后 `nth(4)` 写到了占位符字号上（诊断件 `Temp/diag-evcg.cjs`：写 nth(4) ⇒ `--evcg-placeholder-font-size:200px` 而输入框仍 194px；按 `<label>` 文本定位到 nth(5) ⇒ 内联 `width:200px`、实测精确 200）。修法＝探针改按 label 文本取序号，**8 条判据数不变** |
> | `probe-table-editors.cjs` | 18/19 绿，唯一红「⑤ 行内供应商已变为所选供应商」（`recheck-b23-probe-table-editors.log`）→ **批次㉔ 已修到 19/19**（`Temp/b24-probe-table-editors-run1.log`） | **不是没更新**：同一份日志里编辑态触发器已正确变为「星澜医药站」，而该列 `Property="Supplier"` 存的是**编码**（`MockData` 里 `Supplier = "S01"…`），退出编辑态后单元格按原值显示 `S03` ⇒ 显示域＝编码、编辑域＝名称，探针按名称断行文本。要不要让显示态也出名称属**演示口径**（列模板做一次 code→name 映射），组件侧无错。**批次㉔ 用户拍板「出名称」**：只在演示页给该列加 `<ChildContent>` 显示模板，值域仍是编码（见 `docs/table-edit-issues.md` 的「供应商列显示态：出编码还是出名称」一节） |
> | `probe-nav-collapse.cjs` | 4/7（展开 235/239 判 220、折叠 48 判 60~70，`recheck-b23-probe-nav-collapse.log`） | 展开侧是**外壳常量换了**：现 `.demo-aside` 由 `--demo-aside-w` 定（展开 256px、折叠 64px，实测 aside 256/64、菜单 239/48）；库侧 `.el-menu--collapse{width:64px}` 在样式表里实测存在（`Temp/diag-navw-1.log`）。菜单 48 与 64 的差额来源（侧栏竖向滚条占位 vs 弹性拉伸/`max-width` 收敛）**未定位**——我那条 JS 直接切 `is-collapsed` 的假路径没走组件，不算证据；探针的 220/64 常量按 10-07 外壳写死，先按口径过时处理，别判组件 |

> ⚠️ **本轮我自己弄坏过 12 份探针**：上一轮给探针批量加 `process.argv[2]` 时，转换脚本产出了
> `const BASE = process.argv[2] || BASE;`（自引用 → `ReferenceError: Cannot access 'BASE' before initialization`）。
> 已全部从 pristine 备份复原并改用「声明必早于使用」的顺序自检重写；复跑证据：`probe-demo-shell` 33/33、
> `probe-tablev2` 20/20、`probe-keepalive` 7/7、`probe-preview-arrow-center` 8/8 恢复全绿，其余不再出现该类异常。
> 教训已写进技能：**批量改代码后必须实跑受影响文件**，`node --check` 只查语法、抓不到 TDZ 这类运行时错误。


### 3.8 库级词条探针 `verify-lang-i18n.cjs`（21/21，"运行时会变的状态"探针模板）

与 §3.3 那批静态 DOM 探针的差别是：它验的是一个**可变的注入服务**，所以三件事必须做对——

1. **先确认静态资源本身到位**：`fetch('/_content/Element-Plus/lang/zh-CN.json')` 必须 200 且含 `inputNumber`。
   跳过这步，后面所有 FAIL 都可能是"演示站还在服务旧构建的词条文件"，跟组件无关（本仓栽过同类的一次）。
2. **同一个页面里连测三种状态**：启动即中文 → 切 en-US → 切回中文，中间**不 reload**
   （沿用 §3.4 的 `pushState + popstate` 思路）。这样测到的才是"运行时切换"，
   而不是"刷新后恰好又加载对了"。
3. **不可见的东西要读属性，并且留一个可见锚点做交叉验证**：断言取的是 `getAttribute('aria-label')`，
   同时读页面上把同一次取词结果显示出来的 `#c4-num-lang-state`。第一版就是靠"状态行已换、按钮标签没换"
   这个不一致抓到 2 条真缺陷 —— Blazor 会跳过"参数没变"的子组件，宿主的 `StateHasChanged()` 刷不到深层组件。
   根因与修法见 `docs/lang-i18n.md` §4。

补两条第三批实测到的经验：**开着模态框时 Playwright 的点击会被遮罩拦掉**（改用 `page.evaluate` 走 DOM 层点击）；
**多状态探针每节结尾要把语言/开关状态归位**，否则下一节会因为上一节的遗留状态假红（本会话真发生过一次）。

### 3.8b 日历 / 颜色 / 滑块 / 穿梭框族探针 `verify-lang-picker-family.cjs`（34/34）

与 §3.8 同一套"运行时会变的状态"方法，但这一族多踩了四个只有浏览器能暴露的坑，写探针时必须避开：

1. **`KeepAliveRouteView` 让已访问页面留在 DOM 里**。所以每条查询都要限定在自己的组件类名上
   （裸 `div[role="combobox"]` 会命中上一页遗留的下拉，读出 `null` 变**假 FAIL**；
   `table.el-date-table th[aria-label]` 会把三个内嵌面板的 7 条周标题读成 21 条），
   并且**攒页越多首屏越慢** —— 本仓实测走到第八页时 `.el-transfer` 60 秒不出，所以这份探针**独立成文、自起一条 circuit**，
   不与 §3.8 那份串跑。
2. **`ElButton` 在两个 CJK 字符之间插空格**（EP 的 `autoInsertSpace`）：断言按钮文案要 `replace(/\s+/g, '')`，
   否则「确 定」永远不等于「确定」。bUnit 断言的是渲染树文本，测不到这件事。
3. **DateRange 型 picker 的宿主 `Placeholder` 不落进 `input`**（那两个输入框是「开始日期 / 结束日期」），
   所以"按占位符找示例"会找不到 —— 按所属小节的 `h3` 标题定位。
4. **优先级与门控要各自留反向断言**：`RangeStartLabel="最小值"` 的实例在 zh 和 en 下都必须仍是「最小值」；
   而 `ElConfigProvider.Locale` 与 `ILang.CurrentLang` 不一致时，日历族的可见文字保持 `Locale` 那套语言、
   面板新增的 `aria-label` / `aria-valuetext` **整组不渲染**（最后两条断言就是钉这件事的，
   免得下次有人把"切英文后面板没变"当缺陷去改组件）。

跑法与 §3.8 相同：`NODE_PATH="…/ce_verify/node_modules" node verify-lang-picker-family.cjs [base]`（默认 `http://127.0.0.1:5199`）。


### 3.9 弹层类用例的老 flaky 已定位并修掉（2026-10-07，不是"再等等就好"）

`ElFeedbackTests.MessageBoxPromptCreatesInputAndBeforeCloseCanBlock` 长期被当成"并行争抢抖动"
（`TestWait.cs` 里就点名过它）。这次定位到的是**测试写法的确定性问题**，不是运气：

1. `popup.InvokeAsync(() => messageBox.PromptAsync(option))` 里那个 Task 要**等用户点按钮才完成**，
   所以调用方不能 await 它；但"不 await"就等于没等这次调度+渲染批落地，下一行直接 `Find(...)`
   就会偶发拿到空 markup。正确形状：**在 `InvokeAsync` 里只发起弹层、等 `InvokeAsync` 自己的调度完成、
   再把首帧断言包进 `WaitForAssertion`**。
2. **切语言这类"会引起重渲染的动作"要在渲染器的调度队列里做，并且用 `await` 而不是 `Wait()`**：
   `await popup.InvokeAsync(() => lang.SetLangAsync("en-US"));` 之后**再主动 `popup.Render()` 一次** ——
   事件回调是把重渲染排进队列的，而 `WaitForAssertion` 只会被"已经发生的渲染"唤醒；
   在测试线程上 `.Wait(timeout)` 则可能和调度互等，表现为整 20 秒的超时。
3. 收尾要点掉一个按钮，让挂起的 MessageBox Task 完成，别把未等待的任务留在上下文里。

第一次改完（只把首帧断言包进 `WaitForAssertion`）连跑三次确实全绿，但第四批加负载后又红过一次 —— 说明只等"首帧"不够。
剩下的两半是：**① 别用 `.Wait(timeout)` 阻塞测试线程等渲染器**（改 `await`，否则与调度互等，表现为 20s 超时）；
**② 由事件回调排进队列的重渲染，`WaitForAssertion` 只会被"已发生的渲染"唤醒**，所以测试里切语言后要主动 `Render()` 一次。
两条都补上后连跑三次 **2238 / 0 失败**。真实浏览器没这个问题（渲染器自己批处理，探针 21/21 实证）。
判据留在这里：**用例只在并行全量里红、单独跑必绿 → 先查"有没有等调度 / 是不是阻塞了测试线程"，而不是先怀疑超时不够长**；
而且**同一件事可能有两半，修一半不算修完，要连跑多次回归**。

**这一节当时的收尾是半成品**：那四条（其实是五层里的四层）修完后，两个弹层类同时跑仍会红，
我用 `[Collection("ElPopupHost")]` 把它们锁在一起，并把真身记成"未定位"。2026-10-08 挖到了：
**第五层不是测试写法问题，而是产品缺陷 —— `DialogService.Dialogs` 声明成了 `internal static`**，
而 `ElPopup.OnInitialized` 会把弹层集合整个换新实例，后初始化的宿主让先初始化的宿主守着一个没人写入的旧集合，
症状与第 1 层一模一样（结果 Task 不完成、`FindAll` 为 0）。机制、修法与确定性复现见
`docs/table-edit-issues.md` 里「那条老 flaky 已根治：五层原因叠在一起」一节的第 5 条，判据已提炼成上面 §4 第 8 条。
教训：**"锁起来能绿"不等于"修好了"** —— 锁只是把真实缺陷从套件里藏起来。

### 3.10 弹层宿主去 `static` 之后的浏览器侧口径（2026-10-08）

跑之前必须**杀掉 5199 旧实例 → `dotnet build demo/Element.Blazor.Demo` → 重启 blazor-devserver**，
否则测的是旧构建（本轮实测：`_framework/*.wasm` 的时间戳就是判据，浏览器缓存会被新的 hash 绕开）。

| 探针 | 结果 | 覆盖的是本轮哪一面 |
| --- | --- | --- |
| `verify-feedback-messagebox.cjs` | **39 / 39 PASS**，0 控制台报错 | Alert/Confirm/Prompt 出框、校验拦下不关、× 与遮罩关闭的返回值区分、`DangerouslyUseHTMLString` 真 HTML —— 全走 `DialogService.Dialogs` 这条被改动的队列 |
| `verify-dialogdata.cjs` | **19 / 19 PASS** | `ShowDialogAsync` 打开的自定义尺寸对话框（宽度/全屏/Esc/遮罩/×）；证明队列从 static 变实例后，真应用的开框关框没有退化 |
| `verify-lang-i18n.cjs` | **21 / 21 PASS** | 含"对话框开着时切语言，关闭按钮名称当场换"（渲染期取词那条）与"同页其它实例一起换（Scoped 服务共享同一实例）"——后者正是"作用域内共享、作用域间隔离"的正面证据 |
| `verify-lang-picker-family.cjs` | **34 / 34 PASS** | 四族取词无退红 |
| `verify-table-edit-typing.cjs` | **7 / 7 PASS** | 编辑值落到保存结果（绑定接收者改动若被弄坏会当场暴露） |
| `diag-typing-cost3.cjs` | 表格单元格 **≈19ms/字**，表格外 ≈13ms/字，0 报错 | 耗时口径与上一轮记的 23 vs 11 同级（机器噪声），无退化 |
| `diag-dt-table-probe.cjs` | 5 步全对：点 15 号 `confirmDisabled` 由 true 变 false，改点 20 号跟手，点确定单元格值 = `2026-10-20 00:00:00` | 问题一那条老契约仍在 |

**浏览器侧只能证"无退红"，证不了这条缺陷本身**：演示站是 WebAssembly，整站一条 circuit、
一个 DI 作用域，也就只有一个 `ElPopup` 宿主 —— 缺陷需要**第二个宿主**才现形。
所以正向证据是 bUnit 那条 `PopupHostIsolationTests`（两个 `BunitContext` = 两个作用域，修复前 20s 红、修复后绿），
浏览器这一侧的职责是确认没有把单作用域下原本正常的开框/关框改坏。
另注：`diag-table-typing.cjs` 打印的 71~89ms/字**不能**与 19ms/字 直接比 —— 那份是逐字 Playwright 动作、
每次动作含固定开销，可比口径只有 `diag-typing-cost3.cjs`（一次动作连打 12 字）。

### 3.11 浮层宿主唯一性探针 `verify-popup-host-single-mount.cjs`（2026-10-08，同日第二批）

这条缺陷在浏览器里的形状是**静默**的，所以探针的职责就是把"什么都没出现"变成可读数：

| 段 | 断言 | 读数方式 |
| --- | --- | --- |
| ① 基线 `/loading` | 整屏 Loading 出现且覆盖视口 ≥90%；2 秒后自己关掉 | `serviceMask` = `.el-loading-mask` 且**不在** `.el-loading-parent` 里（把服务式和组件式分开数） |
| ② 基线 `/feedback` | Alert 出框，并记下它的宿主链 | `.el-message-box < .el-message-box__wrapper < #app` |
| ③ 站内进 `/login` | 第二个宿主在这里初始化 | 就绪判据用 `/login` 独有的 `.el-login-heading` |
| ④ 站内回 `/loading` | 整屏 Loading 仍然出现、仍然关得掉 | 修复前读数 `{"inDocument":0,"visible":0,"host":null}` |
| ⑤ 站内回 `/feedback` | Alert 仍然出框、且**仍由同一个宿主渲染** | 比对 ② 记下的宿主链 |

**实测：修复前 6/9 PASS（3 条 FAIL，全在 ④⑤），删掉 `LoginLayout.razor` 里第二个宿主并重新 build 后 9/9 PASS，全程 0 个 JS 异常。**

写这条探针踩到的四个坑（都在脚本注释里，任何"量浮层"的探针通用）：

1. **`page.goto` 测不到这条缺陷**：整页重载＝新应用实例＝新 DI 作用域，两个宿主永不共存 → 假 PASS。
   开头只 `goto` 一次，之后一律注入临时 `<a href>` 并点击（Blazor 在 document 上拦截同域链接），
   并且每步 `waitForURL` 校验路由真的换了 —— 否则"元素没出现"分不清是没跳成功还是跳过去但坏了。
2. **不能用 `offsetParent` 判可见**：`position:fixed` 的元素 `offsetParent` 恒为 `null`，
   会把铺满全屏的遮罩读成"不可见"→ 假 FAIL（第一版就是这么误报的）。改用 computed style + rect 覆盖率。
3. **两类遮罩要分开数**：组件式 `ElLoading` 的遮罩常驻在 `.el-loading-parent` 内、不加载时 `display:none`。
   混在一起数，"全屏层根本没生成"会被读成"生成了但不可见"，缺陷就看不见。分类判据用**祖先**而不是
   `is-fullscreen`（那是 `ElLoading` 自己的类，`ElPopup.razor:141` 渲染的那层不带 —— 属 EP 对齐的另一件事）。
4. **"能关掉"这种负向断言在缺陷态下会空转 PASS**：`inDocument === 0` 在"压根没出现"时也成立。
   所以这条必须与前面"出现了"的正向断言成对出现，单独看它没有意义。

边界说明：演示站是 WASM，整站一条 circuit，所以**浏览器里本来只有一个宿主**；这条探针能抓到缺陷
是因为演示站的 `LoginLayout` 违反了契约。契约本身（服务必须 Scoped、集合不许 static、宿主只挂一个）
由 §1 的第六常驻守卫 `PopupHostScopeTests`（5 例，含 red-before：把 `LoadingService` 改回
`AddSingleton` 立刻红 2 条）钉住。完整机制、修法与"明确没做的事"见 **`docs/popup-host-isolation.md`**。

### 3.12 截断行悬停 tooltip 探针段（`verify-menu-ellipsis.cjs` 第 ⑦ 段，2026-10-08，同日第七批）

本库扩展（EP 只在折叠态挂 tooltip，`menu-item.vue:12-31`）没有官方参照可比，所以这一段的职责是把
"浮出来了"变成四个可读数，并且**每一条都配反空转对照**：

| 断言 | 读数方式 | 为什么这样量 |
| --- | --- | --- |
| 浮层内容＝**完整原文**（不是省略号那截） | `.el-tooltip__popper` 的 `textContent` 与常量 `LONG` 全等 | 只断"有浮层"会在"浮层显示的是被裁的那截"时假绿 |
| 浮层**真的可见** | `w/h > 0` + `elementFromPoint(中心)` 命中的是浮层自己 + `scrollWidth <= clientWidth`（自身没被裁） | 不能用 `offsetParent`（`position:fixed` 恒 `null`，§3.11 第 2 条同一个坑）；"浮层被父级 `overflow:hidden` 裁掉"恰恰是这条功能最容易做的形态 |
| 位置与视口 | `x-placement="right"`、`left >= 项右缘 - 30`、`right <= 1400` | 竖向贴右（EP 折叠态同款），横向朝下，避免压住相邻项 |
| 移出后收起 + **未截断的行悬停不弹** | `waitForFunction` 等 `.el-tooltip__popper` 消失，再对短行悬停数浮层（必须 `0`） | 同一次运行里截断行刚弹过 → 这条负向断言不会空转 |
| 四处挂载计数 | `offTriggers / collapseTriggers / hSubTriggers / hItemTriggers` 与两个 `1` | 把"扩展可以完全退出"钉住：`Ellipsis="false"`、折叠态、横向标题**一个触发器都不挂**，DOM 与上一批一致 |

**实测 26/26 PASS、0 个 JS 异常**（该探针从 15 项扩到 26 项）。两处工程细节：
① 文字 span 从 `li` 的直接子节点挪进 `.el-tooltip__trigger`，所以测量用的选择器写成**三形态回退**
（`> span.类` → `> .el-tooltip__trigger > span.类` → `> span`）并额外报 `hasTrigger`，否则"读不到"会被当成"没裁"；
② 布局类断言一律先注入 `transition/animation:none`（悬停过渡会让"刚操作完立刻量"拿到中间值）。

### 3.13 截断悬停浮层探针组（2026-10-08，同日第八批）

| 探针 | 判据要点 | 上一次实测 |
| --- | --- | --- |
| `verify-ellipsis-tooltip.cjs` | 四个宿主（`/basic` 的 `ElText`、`/table`、`/tablev2`、`/grow`）的截断单元格/文字悬停浮全文。三条一起才算"完整显示"：`fullyInside`（矩形整个在视口里）、`onTop`（中心命中测试落在浮层自己）、`selfClipped`（只在浮层自身 computed overflow 非 visible 时才算被裁 —— `overflow:visible` 的盒子 `scrollWidth>clientWidth` 是常态，拿它判"被裁"会把好的判成坏的）。另加锚点（下缘＝文字上缘 −8px、水平中心差 ≤1px）、`position:"fixed"`、以及**每个正向断言旁边的反对照**（文字放得下的格子悬停什么都不弹、列级/表级关着时 DOM 逐字节回到改动前） | **54/54 PASS，连跑 8 次读数一致**；变异 M1（`fixed`→`absolute`）当场 **9 条红** |
| `verify-scrollbar-fade-scroll-race.cjs` | 滚条淡入的两帧窗口里，别人写的新 `scrollTop` 必须留住。**正向对照不可省**：窗口内 `getComputedStyle(host).overflowY === 'scroll'` 且 `scrollbarGutter === 'stable'`，否则"没被改回去"会因为机制压根没跑而空转通过 | **8/8 PASS**；把旧写回塞回去 → **3 条红**（写回留住的那条、假绿守卫那条、重叠触发那条） |
| `diag-table-popper-clip.cjs` | 留档"absolute 浮层被 `.el-table__body-wrapper` 裁掉"的原始形状（`hitNormal` 拿到 `DIV.cell` 而不是浮层） | 变异回旧实现复现：row 0 `1127..1159` vs 容器上缘 `1156` |
| `diag-eltext-trigger-style.cjs` | computed 等价变异证探针有牙：同一 DOM 只改触发层样式 | 现状 `trigger 180 / 378>180 被裁`；退回 EP 默认 `trigger 378、378/378 不裁` |
| `diag-flip-reset.cjs` / `diag-flip-seq.cjs` / `diag-flip-mirror.cjs` | 定位"翻面段偶发红"的取证阶梯：先证明孤立复现不了（reset 不在），再逐句镜像探针，最后在元素实例上**劫持 `scrollTop` 的 setter 打调用栈** | mirror 第 5 次复现，栈指向 `dom.js:4182`；修完后连跑 6 次 `resets=0` |

菜单那条路径**没有**迁到 `ElOverflowTooltip`（`ElMenuItem.razor.cs:143`、`ElSubMenu.razor.cs:322` 只共用 `TextClip` 量测算，
浮层仍是各自的 `ElTooltip`、`placement="right"`），所以本批对它只是改名与 rect 扩展；
`verify-menu-ellipsis.cjs` 同期复跑 **26/26 PASS**，没有退化。

### 3.14 树形单击与滚条厚度探针（2026-10-10，批次⑩）

| 资产 | 判据要点 | 上一次实测 |
| --- | --- | --- |
| `verify-table-tree-onclick.cjs` | **差分模型**：在 JS 里按 EP 语义另建一份可见性模型（`ORDER`/`PARENT`/`expanded` + `expectedVisible()`），驱动 `/table` ④ 节做 12 次真实单击，每次把 DOM 里可见行名与模型逐步对照；再加"每一次单击都必须改变可见行"（旧实现有 **7/12 次是空击**）、展开顶层只该 5 行（旧实现一次摊成 17 行）、收起后子孙整体不显示、重展开回到原下钻、箭头 `right`/`up` 与状态一致 | **8/8 PASS**，0 个 JS 异常（`Temp/b10-probe-tree-onclick.log`）；变异 MUT-1（还原旧的"展开＝整棵子树摘出 `hiddenRows`"）→ bUnit 侧 **5/7 红**，含"第一击即展开"那条 |
| `verify-scrollbar-wide-h.cjs` | **必须 `headless:false`**（见 §4 第 19 条）。第 ① 条是环境能力正向对照：裸 `div` 两轴槽宽非 0，为 0 说明浏览器走覆盖式滚条、整份探针作废；再量横条厚度 = 8px、`scrollLeft` 真被拖动（鼠标按在条带中点拖 200px，Δ=298）、两轴同溢出时纵轴仍 4px、表头/表体逐列边界对齐、被批准归零的滚道仍 `0×0` | **8/8 PASS**（`Temp/b10-probe-scrollbar-final.log`）；把 `height` 翻回 `4px` → **5/8 FAIL**（厚度、拖动 Δ=0、两轴三条红），还原后回到 8/8（`Temp/b10-probe-scrollbar-MUT-4px.log`） |
| `ScrollbarCaliberGuardTests.cs`（4 例） | 全站细滚条口径「**纵向 4px / 横向 8px**」写死在 CSS 文本上：全局段与 `.el-tree-v2__body` 段两轴分别 4/8；用户批准归零的两处滚道（`.el-pro-tabs-grow__scroll`、时间面板）必须仍是 `0/0`；全局段必须保留 `scrollbar-width/color: auto` 复位（Chromium ≥121 继承到非 initial 值就整体忽略 `::-webkit-scrollbar`） | 4/4 绿；`height` 改回 `4px` → `Guard_GlobalScrollbarIsThinVertical_WideHorizontal` 红（`Expected: 8 / Actual: 4`） |

两份新资产合进本批的数：**全量 bUnit 2442 通过 / 0 失败 / 0 跳过**（改前基线 2431，+11 例）。
树形那半的口径、取证与"没做的事"见 **`docs/table-tree-expansion-alignment.md`**，滚条那半见 **`docs/caret-scrollbar-focus-ring-alignment.md` §8**。
> 本节表里的「横条 8px」是批次⑩ 的时点读数，同日批次⑫ 已按用户指定改为 **6px**、守卫由 4 例增至 5 例 —— 最新读数在 §3.16。

### 3.15 引导镂空与面板定位探针（2026-10-10，批次⑪）

| 资产 | 判据要点 | 上一次实测 |
| --- | --- | --- |
| `verify-tour-hollow-mask.cjs` | 全几何只能浏览器量（§4 第 11 条）。① 从 `path.d` **反解**出洞矩形（`left = M.x − r`、`top = M.y`），断言它 = 目标矩形四周外扩 `gap.offset`，再用像素扫描量洞的右边界落在 `target.right + 6`（±2px），两侧各做 `elementFromPoint` 归属（洞中心命中目标自己、视口角落命中 `path.el-tour__hollow`）；② 面板贴边 = `gap.offset + offset` = 16px，`offsetHeight === scrollHeight` 且连续 6 帧坐标/高度不变（证"定位↔高度反馈环"确实没了）；③ 箭头四要素（外接框 14.14、`matrix(0.7071…)`、底色 === 面板底色、`pointer-events:none`）+ 按 `data-side` 命中 `top/left/bottom:-5px` + 中心压在面板边上；④ 镂空里 `page.mouse.click` 真点下去 ⇒ 演示计数 `已点击 0 次 → 1 次` 且引导仍开着，点暗处 (8,8) 不关闭；⑤ 键盘段 `a` / `Tab` 不关（负向）与 → / ← 切步、最后一步 → 完成（正向）成对；⑥ 三条关闭路径后重开都回第一步；⑦ 无 `Target` 的那一步：无洞、`maskPE=auto`、无箭头、面板 `50% + translate3d(-50%,-50%,0)`、不写 `data-side` | **57/57 PASS**，0 个 JS 异常（`Temp/b11-tour-hollow-4.log`）。浏览器侧翻面：`data-gap-offset=20` → 洞边 +6 移到 +20、洞宽同步 +40，去掉后回 +6；视口压到 400 触发 flip 后把旧 `bottom:24px` 写回去 → `offsetHeight` **126 → 376**（内容高仍 126），清掉回 126 |
| `ElTourTests.cs`（7 → **31** 例） | 批次⑪ 新增 24 例管的是 C# 侧输出：镂空 path 存在性、箭头渲染门（无目标 / `ShowArrow=false` 都不出）、`MaskStyle` 的 `pointer-events` 两种取值、遮罩点击默认不关闭 + 宿主打开才关闭（两向）、只认三个键、`Current` 在两条关闭路径都复位、`Gap`/`Offset`/`ZIndex` 落到 `data-*` 与 style、style 里**不许**出现臆造几何（`padding` / `inset`）、`elementScrollIntoView` 的调用参数与两种跳过条件 | 31/31 绿；**变异两组**：A 组改坏键盘与复位语义 ⇒ **7 例红**（`UnrelatedKeyDownKeepsTourOpen`、`CurrentResetsWhenClosedByCloseButton`、`CurrentResetsWhenFinished`、`ArrowLeftGoesBack`、`EscapeIgnoredWhenCloseOnPressEscapeFalse`、`ArrowKeysSwitchStepsAndRightOnLastStepFinishes`、`MaskClickDoesNotCloseByDefault`）；B 组改坏镂空/箭头/`gap` 接线 ⇒ **4 例红**（`ArrowOnlyRenderedForTargetedStep`、`MaskPassesClicksThroughOnlyWhenTargetExists`、`GapDataAttributesUseOfficialDefaults`、`MaskBlocksTargetClicksWhenTargetAreaClickableFalse`）；还原后全绿（`Temp/b11-mutate-A.log` / `Temp/b11-mutate-B.log`） |

本批的数：**全量 bUnit 2466 通过 / 0 失败 / 0 跳过**、RC=0（`Temp/b11-fullsuite-4.log`，改前基线 2442，+24 例）；
回归 `verify-tour-upload-i18n.cjs` **27/27 PASS**（`Temp/b11-i18n-tour-3.log`，贴边口径随官方 `gap.offset + offset` 改成 16px）。
组件侧口径与全部真身见 **`docs/tour-alignment.md` §6-§10**。

### 3.16 横轴 6px 与 Markdown 编辑器滚条探针（2026-10-10，批次⑫）

| 资产 | 判据要点 | 上一次实测 |
| --- | --- | --- |
| `verify-scrollbar-wide-h.cjs`（复跑，口径改 6） | 同 §3.14 那份，只把 `H_EXPECT` 从 8 换成 6 并在注释里写明"批次⑩ 取 8、批次⑫ 按用户指定回到 6" | **8/8 PASS**（`Temp/b12-scrollbar-6px.log`）：`{vSlot:4, hBar:6}`、按住横条拖 200px **Δ=297**、`th`/`td` 逐列边界逐个相同（`headSlot=0 / bodySlot=4`，即"不影响 grow 对齐"那条）、`.el-pro-tabs-grow__scroll` 仍 `0×0`、0 个 JS 异常 |
| `verify-markdown-scrollbar.cjs`（新） | **必须 `headless:false`**（§4 第 19 条）＋第 ① 项裸 `div` 能力正对照。① 厚度：`.CodeMirror-scroll` 纵 4 / 横 6、替身 `.CodeMirror-vscrollbar` 纵 4、`.editormd-preview` 纵 4；② **取点先自证**：断言替身竖条落在编辑栏 `x` 界内（`vbarRight=800 ≤ cmRight=801 < pvLeft=801`），否则 clip 会量到隔壁栏；③ 显形用**截图取像素**（UA 伪元素 `getComputedStyle` 读不到）：两栏各一对「静止 `ratio≤0.02` / 指针落在**栏中间**后 `ratio>0.3`」，同一 clip 同一算法；④ 拖动按各栏真实量程卡（§4 第 24 条） | **20/20 PASS**（`Temp/b12-markdown-sb-5.log`）：静止 `ratio=0 / minL=255` → 悬停 `0.86 / 0.98`、`minL=214`（＝ `rgba(0,0,0,.16)` 压白底）；拖 4px 替身竖条 150px → 替身 `0→62`（量程 63）**且真容器 `.CodeMirror-scroll.scrollTop` 同步 0→62**；预览栏量程仅 11px，判据写成"拖到底"（`Δ=10`）。视觉留证 `Temp/b12-md-idle.png` / `-hover-editor.png` / `-hover-preview.png` |
| 浏览器侧翻面（同一探针第 ⑤⑥ 段） | 往页面**重新注入** vendor 那四条 `.editormd ::-webkit-scrollbar…`（同特异性、后来者赢），厚度与"静止常显"两条必须同时翻红；还原只删自己 `addStyleTag` 返回的那个 `<style>`（不按"最后一个"猜） | 注入后替身 `4→5`、预览栏 `4→6`、静止条带 `ratio=1 / minL=160`；删掉注入回到 `4px` + `ratio=0` |
| `diag-batch12-grow-scrollbar.cjs`（新，一次性归因） | /grow 全页可滚元素两轴槽宽 + computed `scrollbar-width/color` + 能力对照 | 改前读数 `CodeMirror-scroll vSlot:6 hSlot:10`、`editormd-preview vSlot:6`，其余宿主 `vSlot:4`（`Temp/b12-grow-sb-1.log`）；几何表另见 `Temp/b12-editor-geom-1.log` |
| `ScrollbarCaliberGuardTests.cs`（4 → **5** 例） | 两轴口径改 `4/6`；新增 `Guard_MarkdownEditorScrollbarFollowsLibraryCaliber`：editor.css 必须 `width:4px / height:6px / background:transparent` + 拇指静止透明 + **同时**存在两条悬停显形（`:hover::-webkit-scrollbar-thumb` 与替身的按栏点亮），只有透明没有显形＝彻底没有滚动条 | 5/5 绿；**变异实测**：从 `editor.css` 删掉按栏点亮那条 → 该例红（`Assert.Contains() Failure: Sub-string not found`）、其余 4 例绿，还原后 5/5 且文件逐字节回到变异前 |

本批的数：**全量 bUnit 2467 通过 / 0 失败 / 0 跳过**、RC=0（`Temp/b12-fullsuite-2.log`，改前基线 2466，+1 例）。
口径、两层真身（vendor 特异性 ＋ CodeMirror 替身滚条）与全部读数见 **`docs/caret-scrollbar-focus-ring-alignment.md` §9**。

### 3.17 半选显示判据 `verify-checkbox-indeterminate.cjs`（2026-10-10，批次⑮）

| 资产 | 判据要点 | 上一次实测 |
| --- | --- | --- |
| `verify-checkbox-indeterminate.cjs`（新，16 项） | 读的是**画出来的东西**而不是类名：`.el-checkbox__inner` 的 `::before`（横线：`content:""` + `height:2px` + `top:5px` + `left/right:0` + `transform` 含 `0.5` + 白色）与 `::after`（勾：`display` 且矩阵 `d` 分量 >0.5）。四段：A 载入即半选（类名 + 横线 + `aria-checked="mixed"` + 有 4 个子项）、C/D 半选→打勾→空的点击链、E 点子项时宿主重算半选、B/T0 对照 | 改前 **7/9 通过**（A1/A2/A3/A4 全红：那一格根本没有 `is-indeterminate`，本节只有 1 个 `el-checkbox`）→ 改后 **16/16**（`Temp/b15-probe-after.log`，另连跑两次 `…-after-r{1,2}.log` 各 16/16） |
| B1/B2 CSS 通路对照 | 把 `is-indeterminate` 手工贴到「基础用法」的框上必须立刻长出横线（B1 正），未贴的框不得有横线（B2 负）—— 两条一起把「A 段红」限定成**状态没进去**，而不是样式坏了 | B1/B2 改前改后都 PASS（`before` 实测 `display:block / height:2px / top:5px / matrix(0.5,…)`） |
| T0 勾判据自证 | 「勾」的 `::after` 在 EP 里是常驻元素、靠 `scaleY(0/1)` 显隐，`display` 恒为 `block` —— 只看 `display` 的判据是恒真的。T0 用同一个基础用法段里「未勾的 A」和「已勾的 B」把 `isTick` 钉在矩阵 `d` 分量上 | 实测 `A.after=matrix(.707,.707,0,0,0,0)`（`d=0`）/ `B.after=matrix(.707,.707,-.707,.707,0,0)`（`d=.707`）→ 判据分得开 |
| 浏览器翻面（改演示页初始态） | 把 `checkedItems` 初始从「4 项选 2 项」改成「4 项全中」→ A1/A2/A3 必须红（还有 C/D 因为起点错位一起红） | 翻面 **9 项红 / 7 项通过**（`Temp/b15-probe-mutation.log`），还原后 16/16 |
| `ElCheckboxIndeterminateTests.cs`（新，**9 例**） | .NET 侧把「宿主拥有半选状态」这条闭环钉死：`SelectAllHost`（演示页那一段的等价宿主，4 个子项 + 全选）的 2/4→横线、点横线→4/4 打勾、再点→空、取消 1 个子项→回横线、补上最后一个→翻成打勾；另有 `Indeterminate` 参数盖过 `@bind-Value` 的守卫例、setter 翻 false 退回未选、半选时点击回传 `true`+`Status.Checked`，以及 `BindIndeterminate_IsNotAParameter_…`（钉住「库里没有 `IndeterminateChanged`」这个陷阱本身） | 9/9 绿；**变异 M1**（拆掉 `OnParametersSet` 里 `Status != Status.Indeterminate` 守卫）⇒ **4 例红**；**变异 M2**（`ChangeStatus` 里半选分支改成 `UnChecked`）⇒ **4 例红**（与 M1 不是同一批）；两次还原后 9/9，`ElCheckbox.razor.cs` 逐字节回到改前（`git diff` 空） |

本批的数：**全量 bUnit 2484 通过 / 0 失败 / 0 跳过**（`Temp/b15-fullsuite-r1.log`，改前基线 2475，+9 例）、
七条常驻守卫 **82 / 0**（`Temp/b15-guards.log`）。口径与真身见 **`docs/checkbox-alignment.md` §3**。

### 3.18 反馈组件 11 报逐项判据 `verify-batch16.cjs`（2026-10-10，批次⑯）

| 资产 | 判据要点 | 实测 |
| --- | --- | --- |
| `verify-batch16.cjs`（新，41 项） | 11 个用户报 each 一段，每段都带**同前提的正对照**：L0 先确认目标本身 `position:static`（否则 L1 的矩形相等是假绿）、L5 对照整屏 loading 仍 `fixed` 铺满 1500×1000；P3 对照 light 弹层仍白底（证明 P1 的 dark 读数不恒真）+ P6/P6b 悬停开与移开收 + **P5 对照：点面板内部不收**（证明 P4 的点外收不是「点哪都关」）；S4b 控件内 mousedown 不收、S5 单选对照收起+回显；SP2 对照 space 外按钮 margin-left 真 12px；TT4 对照 click 触发路径没被 hover 修复带坏；TR0 先确认勾选真生效；U3 断言文件不串到别的上传区块；Z0/Z0b 全程 0 pageerror | 终局 **41/41**（`Temp/b16-verify-run9.log`）。中途轨迹：run5 37/37 → run6 **P5 红** `{"opened":1,"inside":0}`（确定性、runs 6-8 皆红）→ run7/8 **S5 FATAL** → run9 41/41 |
| P5 红→修好这一支 | `@onclick` 绑在含内联面板的外层 wrapper 上，面板内的点击冒泡回来等于再点一次触发器 | 把 click/contextmenu 下移到 `.el-popover__reference` 后 `{"opened":1,"inside":1}` |
| S5 的两次 FATAL（未解释） | `Cannot read properties of null (reading 'textContent')`；两份隔离脚本（直连 / 先跑 /popover 的 P4-P5 再来）都显示单选正常，run9 亦全绿 ⇒ **没复现、没锁 workaround** | 处置是把该处改成**打印状态**（`itemCount`/`hasDropdownIn`/`dropdownsAll`/`rootCls`/`roots`）而不是抛异常，真回归会出数字 |
| `ElFeedbackTests.cs`、`ElSelectV2Tests.cs`（同步） | tooltip 用例改 `MouseEnter()/MouseLeave()`（`mouseover` 系冒泡事件，改了绑定就得改测试）；点击型 popover 显式 `Trigger="PopperTrigger.Click"` 并点 `.el-popover__reference`；`ElSelectV2Tests` 构造器补 `JSRuntimeMode.Loose` | `b16-test-feedback.log` 1 红 → `…-feedback2.log` **6/6**；全量改前 **14 例**红在 `JSRuntimeUnhandledInvocationException … elementPickerOutsideAttach`，加 `Loose` 后 2484/0 |

本批的数：**全量 bUnit 2484 / 0**（`Temp/b16-fullsuite2.log`）、七条常驻守卫 **82 / 0**（`Temp/b16-guards.log`）、
构建 0 错误（`Temp/b16-build5.log`）。#95 的对照组另见 `Temp/b16-select-control2.cjs` → `b16-control2.log`
（/select 973ms/max 759ms vs /treeselect 331ms/max 279ms，两页选择环节阻塞均 0）。
口径分别见 `docs/popover-alignment.md`、`docs/tooltip-alignment.md`、`docs/select-v2-alignment.md`、
`docs/transfer-alignment.md`、`docs/upload-paste-alignment.md`、`docs/tree-select-alignment.md`、`docs/loading-alignment.md`。

### 3.19 树选择器勾选/回显同源 `verify-treeselect-b19.cjs`（2026-10-11，批次⑲）

| 资产 | 判据要点 | 实测 |
| --- | --- | --- |
| `verify-treeselect-b19.cjs`（新，批次⑲ 44 项 8 段 → **批次㉔ 加段9 后 47 项 9 段**，判据要点见 §3.24） | 面板勾选集合按 `Select.Value` 反推，所以每段都同时读**面板三态**（`is-checked` / `is-indeterminate` / `aria-checked`）和**输入框回显**，两者不同源立刻红。牙口分三类：① 负向 —— 点框的那一行不该出现 `is-current`（`@click.stop` 没拦就会亮，见下）；② 正向对照 —— 点节点文字**必须**点亮 `is-current`（同实例同一次渲染，证明这类名在这页量得到，不是恒真的负向）；③ 交叉对照 —— 没开 `ShowCheckbox` 的那个实例面板里 0 个复选框（反证勾选没写进共享 `DataSource`）、共用 `value1` 的「基础用法」回显与勾选实例一字不差 | **44/44**（RC=0，`Temp/b19-probe-final2.log`；注释订正后在新字节上复跑 `Temp/b19-probe-final3.log` 同为 **44/44**）。翻面（删掉复选框外那层 `stopPropagation` 后重建）**41/44**，红的正好三条 `is-current` 判据（`Temp/b19-probe-flip2.log`），同一次跑里两条正向对照仍绿。**当前字节复跑（批次㉔）：47/47**（`Temp/b24-verify-treeselect-after.log`），顺序翻面 **46/47**（`Temp/b24-verify-treeselect-flip.log`） |
| 为什么负向判据只能挑 `is-current` | 冒泡多跑的那一遍 content 会顺带折叠子树、再算一次勾选，但 label 联动 `input` 会产生**第二次**合成 click，把这两样又翻回去 ⇒ `rowCount` / `checked` 净读数与修前一致（实测：翻面构建点 A-1 的框仍得 `checked=[二级节点 A-1]`、点 B 的框仍折成 B-1）。只有 content 那条路才会写的 `_currentNode`（`is-current`）留得住差异 | 对比两份脚本输出：`Temp/b19-flip-check-restored.log`（有拦截，`current:false`）vs 翻面构建下同一脚本（`current:true`） |
| 演示页共享数据源的坑 | `/treeselect` 五个实例用同一批 `TreeItemBase`，`node.Expanded` 跨实例共享 ⇒ 上一段点文字折叠了 A，下一段行数判据凭空少两行。探针每段开头用展开图标（那条 onclick 本来就 stopPropagation）把树归一到全展开 | 归一后各段行数判据稳定为 6 / 4 / 6 |
| bUnit 侧的分工 | 勾选三态、值折算、严格模式、初始预置值这八条走 `ElTreeSelectTests`（`input.el-checkbox__original` 用 `Change(true)` 驱动，AngleSharp 不做 label 联动，真实冒泡量不到）；冒泡与 `is-current` 只在探针里 | `filter FullyQualifiedName~ElTree` **23/23**（`Temp/b19-treeselect-tests-4.log`；新字节复跑 `Temp/b19-treeselect-tests-5.log` 同为 23/23）；全量 **2536/0**（`Temp/b19-full-suite-2.log`，基线 2528 + 本批 8；复跑 `Temp/b19-full-suite-3.log`） |
| 写 EP 符号名/「已知差异」之前的那道自查 | 注释和文档里每出现一个 EP 函数名，就得能在 `Temp/ep2140-*.mjs` 里 grep 到；每条「我们和 EP 不一样」都得把两侧都读到那一行。本批自查抓出两处不合格：① 三处注释写 `handleParentNodes`，2.14.0 源码里**没有**这个符号（真的那段是 `reInitChecked`，`node.mjs:37-40`、`:45-46`）；② 记了一条「预置禁用叶子时 EP 的 `getChildState` 跳过 disabled ⇒ 父节点不显选中」的假差异 —— `allWithoutDisable` 全源码只出现 3 次且都在 `getChildState` 内部，**无消费者**，`_setCheckedKeys` 对命中 key 的叶子也不看 disabled ⇒ EP 同样把父节点显成选中 | 证伪与逐条行号见 `docs/tree-select-alignment.md`「撤回我自己写的已知差异」。改的都是注释，行为不变，但仍按新字节重跑三件：23/23、2536/0、44/44 |
| 七条变异（bUnit 有牙证明） | 见 `docs/tree-select-alignment.md` 的「回归与牙口」表：M-a 6 红、M-b 2、M-c 3、M-d 2、M-e/M-f/M-g 各 1，每条跑完从快照还原并校验 md5 一致（`Temp/b19-mutation-summary-2.log` + `Temp/b19-mut-M-*.log`） | M-g 初版 0 红（那条严格模式用例只点框不碰折叠）⇒ 补「先点父节点文字再断言子树没收」后复跑 1 红 |

### 3.20 防抖偶发红的证人三段法与「回合内 spin」（2026-10-12，批次㉒ #116）

| 资产 | 判据要点 | 实测 |
| --- | --- | --- |
| 临时证人 `Temp116Witness.cs`（验完已删） | 三段各证一件事，不许合并：**W1** 记录防抖续跑落在哪个线程（`fetchThread` vs `testThread` 写进日志）；**W2** 免负载确定性复现——键间强制 150ms > Debounce 75ms ⇒「a」必上远端（这是 EP 同款「真超窗各发一次」语义，证人断 `Contains("a")`，不是 bug 展示位）；**W3** 新结构免疫——两键合入同一 `cut.InvokeAsync` 回合 + 回合内 `SpinWait` 60ms（3 倍防抖），回合结束立刻 `Assert.Empty` 必须成立 | 修前 `Temp/b22-w116-witness.log`：W1/W2 绿、W3 红（终态 `["a","ab"]`，pos 0 差异）——正是这两条合起来指向「到点→发远端」窗口没有预检；修后 `Temp/b22-w116-after.log` 9/9 绿 |
| 结论：派发回合串行，但**续跑不消失** | W3 修前红的方式很讲纪律：回合内没漏（`Assert.Empty` 过了），漏在回合结束后被取代的定时器照常发远端。所以「合入同一回合」只解决**键间墙钟**，组件侧 `IsCurrentQuery` 预检才解决**请求出网**——两件都到位才确定性 | 变异 M1（删预检）红 2 条、M2（整窗跳过防抖）红 2 条、M3（删查询起点 `CancelPendingQuery()`）**9/9 绿（幸存）**：版本计数独立判旧，两防线互为双保险；这条幸存要写进文档，别把「没红」讲成「测不到」 |
| 断言口径 | 终态断言蕴含中间态：多余的「a」一旦发出就留在 `calls` 里，等不来也藏不掉，所以 `calls == ["ab"]` 比「键入后立刻 `Assert.Empty`」既强又不赌调度。旧记录（CHANGELOG「零时间赌注」那条）里「同步路径里绝不会 fetch」只对**回合内**成立，本批把它限定清楚 | 全量 **2554 / 0 失败**（`Temp/b22-full-suite-1.log`）；24 spinner 负载复核（实测 26 python、墙钟 2m55s vs 空载 ~1min，负载自证生效）**6/6 绿**（`Temp/b22-load116-verify2.log`；首跑 `Temp/b22-load116-verify.log` 因 spawner 随前台命令被连带杀掉、负载未自证 ⇒ 作废不引用） |

### 3.21 加载门控偶发红：环境门控 trace + 「每轮跑批前当场数进程」（2026-10-12，批次㉒ #115）

| 资产 | 判据要点 | 实测 |
| --- | --- | --- |
| 环境门控 trace（`B22_115_TRACE` 文件 sink，验完已还原） | 负载红抓不到现场时，把怀疑点做成**每次进出都落一行**的探针（tid / 门 / 状态 / 前值 / elapsed），idle 跑一遍就能看到时序畸变——本案 idle 就复现了全部三段机制：Arm 窗口未收时命令式举门（污染）、渲染路 `EndDrop` 在命令式半途落地（elapsed 141）、`Loading` 一拍排在收门后的续体里 | `Temp/b22-115-trace-idle.log`（机制口径见 `docs/viewer-alignment.md` §10）；修法：入口同步发一拍（用户拍板）+ `loadGateImperative` 归属守卫 + 污染守卫 |
| 翻面牙口 | M-a 删入口一拍恰 1 红（`Temp/b22-115-mut-ma.log`）；M-c 删污染守卫红在**新增断言** `NotEqual(Loading, PreviousStatus)`（`Temp/b22-115-mut-mc.log`，失败行 ：2665）——加强断言先拿到阳性再删守卫，顺序不能反；M-b 删归属守卫 **幸存**（idle 180/180 + 24 spinner 真载 2 轮开局 26 进程仍绿，`Temp/b22-115-mut-mb-load1/2.log`）：坏需要「放门时 `current` 未赋」的竞态窗，bUnit 过滤器进不去 ⇒ 有界结论写进文档，机制判定由 trace 承担（批次㉑ M4 同款） | 修复后 `ElViewerGrowTests` 180/180（`Temp/b22-115-after-fix.log`）、全量 **2554/2554**（`Temp/b22-full-suite-2.log`）、24 spinner 真载 26 进程开局收尾 180/180（`Temp/b22-115-load-verify1.log`） |
| 负载自证升级：**每轮跑批前当场数进程** | 负载下 `sleep`/`dotnet test` 墙钟大幅漂移（实测一轮 `dotnet test` 载下要数分钟，420s/600s spawner 窗口常常只装得下 1–2 轮）：只在 spawner 启动时数一次不够，**每轮**跑批前后各数一次，`before` 与 `after` 不全为高值（26）的轮按无载/弱载论，只能列作参考不能当负载证据。powershell `Get-Process` 计数可信；跨窗轮的「procs=0」是窗口真过期，不是测量坏了 | 有效载轮：`b22-115-load-verify1.log`（26/26）、`b22-115-load-gate1.log`（26 开局）；弱载/跨窗轮 `b22-115-load-verify2/3.log`、`b22-115-load-gate2.log` 全绿但仅参考；M-b 负载轮 `b22-115-mut-mb-load1/2.log`（26 开局 ×2） |

### 3.22 bUnit `Nodes`/`FindAll` 会被缓存投毒：判「DOM 撤没撤」要读 `Markup`（2026-10-12，批次㉒ #117）

| 资产 | 判据要点 | 实测 |
| --- | --- | --- |
| 竞态本体 | bUnit 2.7.2 `RenderedComponent.Nodes` = `latestRenderNodes ??= Parse(Markup)`（无锁 check-then-act），`UpdateMarkup` = 清缓存 → 慢 `GetHtml` → 才 `Volatile.Write(markup)`。窗口内读 `Nodes` 的线程拿旧 markup 解析、把**旧解析树写回缓存**，非空永不重解析 ⇒ `FindAll` 从此数的是历史帧；`Markup` 本身 `Volatile.Read`，无锁 | 十五档猎取 122 轮 / 13 次红的最后一环（`docs/message-box-alignment.md` 第 11 条） |
| 放大见证的纪律：**判据必须绕开被测缓存** | 锤击 `Nodes` 的见证若还用 `FindAll` 当等待条件，投毒者就是它自己，红绿都无法归因。本案全程用 `popup.Markup` 字符串判开/关层，只在取读数那一刻走缓存；每轮锤 5.8M–10.9M 次 ⇒ **6/8 轮必现** `markup.wrapper=0 dom.wrapper=1`，与无放大时 ~10% 命中率同源 | `Temp/b22-117-witness.log`（临时件验完已删；运行 `Temp/b22-117-witness-run.log` 按设计 1 红） |
| 「只有真实 DOM 变化的渲染能清投毒缓存」 | 空转 `InvokeAsync(() => {})` 泵（历史红泵 602 轮）、反射 `StateHasChanged` 强制渲染（同一棵树 diff 0 编辑 ⇒ bUnit 不重新生成 markup，`isMarkupGenerationRequired=diff.Edits.Count>0`）都**不清**；真实增删层的渲染才清。这三条形状把历史红的「泵不出来／强制渲染也不出来／再开一层反而归零」一次全解释掉 | 见证的恢复对照（再开+关第二个 Prompt 后归零）＋逐条对上（`docs/message-box-alignment.md` 第 11 条） |
| 换尺子 ≠ 放宽断言 | markup 就是渲染器刚序列化的 DOM 文本，「wrapper 撤没撤」在 markup 上判与在解析树上判**同一事实**，只是前者无锁；判据有没有变钝要用翻面验：注释 `Dialogs.Remove` ⇒ 三条全红在「等待超时」 | `Temp/b22-117-mut-remove.log`（3/3 红）→ `cp` 还原 md5 逐位一致 → `Temp/b22-117-after-restore.log`（3/3 绿）；全量 **2554/2554**（`Temp/b22-full-suite-3.log`） |
| 负载自证先把**进程种类**数对 | b14 纺织器造的是 **python** 忙循环，自证要数 `Get-Process python`；本案第一批 soak 数了 `dotnet`（恒 =2，与负载无关）⇒ 那三轮只能按参考记（承 3.21 的「每轮前后各数一次」，先确认数的是负载本尊的进程型） | r1–r3 窗内当场 26 python 但未按轮留证 ⇒ 仅参考；r4–r6 `python_before=0` ⇒ 窗口过期、空转轮，如实记 |

### 3.23 浏览器侧计时三条规矩（2026-10-12，批次㉓ #98 收尾复测，无代码改动）

| 资产 | 判据要点 | 实测 |
| --- | --- | --- |
| **Node 侧 `page.evaluate` 轮询不能量「DOM 变更时刻」** | `evaluate` 的回调和渲染器抢同一条主线程：主线程被长任务堵住时轮询拿不到运行机会，恢复运行那一刻读到的时间戳是「主线程下一次可运行」，不是「变更发生」。暴露方式是**同一轮里两个本应不同的量变成同一个数**：`T-menu`（侧栏高亮）与 `T-page`（页内容器挂子节点）三组读数一模一样 ⇒ 尺子坏了，不是两件事同时发生 | `Temp/diag-growfirst.cjs` / `Temp/diag-growfirst-1.log`：/grow 两臂 1714 / 1440 / 1456ms 在两臂同时相等，该口径整条作废（只在文档里作反例引用）。改法：页内 `MutationObserver` 在记录回调里 `performance.now() - t0` 自己打时间戳，Node 侧只在结尾读一次 `Temp/diag-growfeel.cjs` |
| **页内观测器的判据要按目标分开** | 一次性建完的轻页和分批建的重页不能用同一个「落定」条件：静置窗太短会在泵中途就判落定（读到一半的 DOM），固定「子树 ≥ 4000」会把轻页判不出来。做法：每臂带 `minSub` + `quietMs` 两个参数（重页 4000/1600ms、轻页 1/600ms），报「首次变更 / 高亮 / 货架挂载 / 安静 / 末次变更」五个时刻而不是一个 | `Temp/diag-growfeel-1.log`：/grow 首变 49/56/62ms、高亮与本页挂载 227/298/371ms（6/6 轮同毫秒＝两阶段挂载按设计生效）、末次变更 19.2~20.3s；Badge 对照 41/50/58ms、187/188/223ms |
| **负载在场时跨 boot 档位对比要换成同 boot 配对阶梯** | 「页越多开新页越慢」这类档位（累积量）效应，跨 boot/跨窗口跑会被整机负载漂移吃掉甚至把方向跑反（实测 /grow 在货架第 13 位 7782ms 比第 1 位 10163ms **快**）。正确设计：一次 boot 内让同一页在第 k 位与第 N+1−k 位各测一次（正序轮 + 反序轮），**页面自身内容成本自动抵消**，配对差才是档位差 | `Temp/diag-shelf4.cjs` / `diag-shelf4-1.log`：12 组轻页配对 Δ安静 均值 −4.8ms、符号 6 负/5 正/1 零 ⇒ 档位效应低于噪声；同轮阶梯也平（12 组轻页首变三轮分别 207~463 / 227~308 / 192~351ms，不随货架 1→13、DOM 4302→7246 涨）。据此否掉了 #98 那条「已开页数放大开页成本」（原判据来自 `Temp/diag-lag3-1.log`，那轮帧节奏塌过且属跨窗口数字） |
| **直连 URL 静置 0 变更 ≠ 真实路径无噪音** | 要按用户路径复现（启动在 A 页 → 点侧栏开 B 页 → 静置），并把变更按时间分桶、逐条点名「类型 @ class 链」；否则会把「切走的页还在跑」这类常驻噪音漏在观测窗外（3s 窗根本到不了） | `Temp/diag-churn.cjs`（/badge、/grow 直开静置 30s：0 变更/0 长任务）→ `Temp/diag-churn2.cjs`（45s 窗）：9s 之后全是 `attributes @ div.el-carousel__item(.is-active)` / `li.el-carousel__indicator(.is-active)`，节奏 1~2s；**Badge 臂没开过 /grow 也照样有** ⇒ 归属**已 `display:none` 的 /table 页**的自动播放轮播。两桶 **0 个长任务** ⇒ 只烧 CPU/功耗，不掉帧，不作「不流畅」成因 |
| **Git Bash 传以 `/` 开头的环境变量要 `MSYS_NO_PATHCONV=1`** | `PATHS='/badge,/grow'` 被 MSYS 当路径转换成 `E:/.../git/badge`，拼出 `http://127.0.0.1:5199C:/...` ⇒ `page.goto: Cannot navigate to invalid URL`。同类坑：`TAG`、`OUT` 之外凡是值以 `/` 开头的环境变量都要加前缀 | 加 `MSYS_NO_PATHCONV=1` 后两条静置探针跑通（配方见 `docs/grow-progressive-section-build.md` §8.7） |

### 3.24 时序类判据的三条新规矩（2026-10-12，批次㉔ #121/#123）

| 资产 | 判据要点 | 实测 |
| --- | --- | --- |
| **页内 rAF 轮询尺的读数 ≈ 本臂长任务的尾巴，不能当「变更时刻」** | §3.23 否定了 Node 侧 `evaluate` 轮询，但页内 rAF 轮询也不是真值：DOM 在长任务**中途**写下，轮询回调要等主线程让开才跑，于是命中时刻被系统性抬高到接近该臂长任务合计。做法：**两把尺挂同一次点击**（rAF 轮询 + `MutationObserver` 回调里 `performance.now()-t0` 打戳），并同臂报出长任务合计，看到「轮询≈长任务」才对得上号 | `Temp/b24-open-matrix2.log` 六臂中位帧全部 8.3ms（无降频），轮询↔长任务逐条：91↔90、214↔213、197↔196（/select）与 199↔197、341↔340、210↔209（/treeselect）；同臂观测器打戳只有 64/125/112 与 125/251/129ms ⇒ 用观测尺作真值，轮询尺只作历史口径复现 |
| **「A 早于 B」这类顺序判据不能配绝对毫秒** | 顺序本身是两回事，绝对值随尺子/机器漂。段9 三条的写法：① 回显时刻 < 面板节点移除时刻（顺序）；② **正向对照**：`removed ≥ 150ms`，证明采样点确实落在收起动画之内 —— 没有这条，「动画被整块删掉 ⇒ removed 也提前 ⇒ 顺序反而假绿」这条路堵不住；③ 回显文本等于所选路径（证明不是别的写入撞对了时刻） | `Temp/b24-verify-treeselect-after.log:716-721`：**47/47**，段9 `echo=35ms removed=243ms`。翻面（把关闭动作挪回写值之前、重建）`Temp/b24-verify-treeselect-flip.log` **46/47**，唯一红的正是顺序那条，现场读数 `echo=238ms removed=238ms`（同帧）⇒ 判据有牙；同一次翻面跑里正向对照②仍绿（`removed=238ms`） |
| **历史读数不复现时，先查两把尺是不是同一前提，再谈「消失」** | 同一支探针复跑给出 414~609ms、新口径只给 51~129ms，不是矛盾：复跑那一支每轮之间**点了树节点**，把「选中→回显→五个共享实例重渲染」的成本算进了下一轮「点开」的窗口（本轮抓到那条 210ms 写值顺序问题正好是它的成分）。并列引用时必须写明各自主线程内发生过什么 | `Temp/b16-control2.log`（历史 536~582ms）vs `Temp/b24-b16-control2-rerun.log`（414~609ms）vs `Temp/b24-open-matrix2.log` / `b24-treeselect-latency3.log` / `b24-treeselect-open-why.log`（点开 51~251ms，冷开首变 80ms、复开 17ms）；逐条归因见 `docs/tree-select-alignment.md` 批次㉔ 第一节 |
| **降频/节流类现象：操控检查不过就只能写「未定位」** | 「另开一个 Edge 窗口置顶来制造遮挡」这个实验要先证明确实遮挡了（`document.visibilityState`、`hasFocus`、中位帧）。实测遮挡相 `focus=true`、中位帧 8.4ms ⇒ 遮挡没发生，实验既不能证明也不能否证「遮挡导致 rAF 掉到 1Hz」，只能记为没做成对照。**同日更正**：批次㉓ 那个「中位帧 1008.2ms」的形状当天被页内证人**抓到过一次现场**（`verify-markdown-scrollbar` 证人副本的预览栏臂，见第 67 条）——`rafMed=1000ms`、最长帧 1008ms、长任务 `0/0ms`、`vis=visible`、DOM 变更计数还在涨 ⇒ 「页面活着但帧被掐到 1Hz」是真实状态而不是读数假象；带全套反遮挡旗的 `b24-treeselect-latency.cjs` 与只带 `--no-sandbox` 的证人脚本都出现过同样读数 ⇒ **旗子不是分水岭，触发条件仍未定位**，但不再当作「条件不存在」。**本轮把三种候选全部记名**（三种操控的自证人都不成立，所以都是「没做成对照」而非否证）：① 另开 Edge 窗口置顶（`focus=true`、8.4ms）；② Win32 `SW_MINIMIZE`（`iconicNow=True` 而页面仍 `vis=visible focus=true`、8.3ms）；③ 置顶无边框窗整幅压住被测窗口（`Temp/b24-raf-cover-2.log` 覆盖窗自身 `SHOWN=false ticks=0` ⇒ 连"覆盖窗显示过"都没证到），同轮把 `--disable-backgrounding-occluded-windows` 等三面旗用 `ignoreDefaultArgs`（传数组，整组传 `true` 会让 Edge 直接退出）拿掉做 A/B，盖住相仍是 8.3ms。**另一条要把因果方向钉死**：那次 1Hz 现场所在的整份运行是 **20/20 PASS**，且 1Hz 那一臂四枪 `hover=true`、ratio 全是 `0.981081081081081` ⇒ **1Hz 与 #100 的两条 hover 红没有因果关系**，它只污染时长读数；反过来若哪次 `removed` 与 `echo` 同时变成 ~1000ms 量级，那是这个场景，不是回归。 | `Temp/b24-treeselect-latency3.log`（三臂 104/95/51ms 全在健康门内；遮挡相 73/87、撤遮挡 71ms，中位帧 8.4ms＝操控检查没过）；`Temp/b24-batch/verify-markdown-scrollbar.log:15-16`（1Hz 现场在一份 20/20 的日志里，同臂四枪 ratio 一字不差 `0.981081081081081`）；`Temp/b24-raf-min-1.log`（minimize 臂）、`Temp/b24-raf-cover-1.log` / `Temp/b24-raf-cover-2.log`（覆盖窗臂与三面旗 A/B：盖住时中位帧 默认=8.3/8.3ms 对比 无旗=8.3/8.3ms）；/treeselect 侧三次运行（`latency3`/`open-why`/`open-matrix2`）未再现该形状 |

### 3.25 日期弹层宽度组与 /statistic 演示守卫（2026-10-12，批次㉕ #124 + #125/#128/#129）

| 资产 | 判据要点 | 实测 |
| --- | --- | --- |
| `verify-datepicker-panel-sidebar.cjs` C 组（新增 6 条：C1 单面板带快捷弹层 438px、C2 面板内 0 个横向溢出盒子、C3 日历末列「六」整列在面板右界内、C4 双面板带快捷 756px、C5 **正向对照**无快捷列仍 322px 且 `hasSidebar=false`、C6 点侧栏「本月」真的写进两个输入框） | 弹层宽度是**钳制链的一环**：`.el-date-picker__dropdown .el-picker-panel{max-width:100%}` 把面板宽度绑在 popper 上，所以宽度判据只能量 popper 与面板的**内容宽/滚动宽**（`scrollWidth` vs `clientWidth`），不能只看面板自身声明的 CSS。C2 的溢出清单直接把红字写成 `[{cls,sw,cw},…]`，红了当场能归因到是哪个盒子装不下 | **16/16**（RC=0，`Temp/b25-dp-sidebar-probe-final.log`）。翻面（`DropDownWidth` 撤掉侧栏那 116/110 后重建）**12 通过 / 4 失败**（`Temp/b25-dp-sidebar-probe-C-FLIP.log`）：红的正好 C1–C4，C5 仍绿；改前现场 `popper w=322`、`__body cw=210` 装 `sw=307`、4 个溢出盒子、末列 `right=729` 越出 `panelRight=632` —— 与用户截图同形 |
| `verify-statistic-demo.cjs`（新常驻，30 项 7 组） | S 组：6 个 `--el-statistic-*` 变量必须能在**根节点** `getComputedStyle().getPropertyValue()` 读到，`__head` 是 12px/line-height 20px/margin-bottom 4px/#606266、`__content` 是 20px/#303133；P 组：按标题文本定位格子，直接读 `.el-statistic__number` 的文本比对官方口径（`12,345.67` 截断、`12345.67` 关分组、`12 345.67` 空格分组、`1.234.567,89` 两符不污染、`-4,580.5` 负号在分组前）；M 组：数值过渡终值 `172,000` ＋ 点「重放」回到中途值（证明定时回派真的在重渲染）；C 组：`DD [天] HH:mm:ss` 出「19 天 11:32:41」且**没有方括号**、只有 `HH` 时是总小时（40~48）、2.2s 内至少 2 个倒计时文本在走；K 组：卡片 28px 与基本段 20px 正反对照；E 组：本库趋势/Extra 扩展 | **30/30**（RC=0，`Temp/b25-statistic-probe-RESTORED.log`；首跑 28/30 的两条红都是探针自己写错，见 §4 第 69、70 条）。CSS-1 翻面（`.el-statistic__content` 的 `font-size` 从变量改回写死 20px 后重建）**29 绿 / 1 红**（`Temp/b25-statistic-probe-FLIP.log`），红的只有 K1，现场 `largeVar="28px"` 而 `largeSize="20px"`＝覆盖读不到 ⇒ 这项修复的牙只在「外部覆盖值 ≠ 默认值」那一侧，见 §4 第 71 条 |
| bUnit 三族翻面（同一批，判据一字未改） | Flip A：`ElCountdown` 末尾不剥方括号 ⇒ 期望只有 `CountdownStripsBracketLiterals` 红。Flip B：`ElStatistic` 跳过千分位插入 ⇒ 分组相关 4 条新用例 + 2 条旧用例红，而 `PrecisionPadsMissingDecimalsWithZero` 必须仍绿（补零不依赖分组，作正向对照）。Flip C：去掉小数截断 ⇒ 4 条红且红字全是「尾巴多出来」形状 | A **1 红 / 7 绿**（`Temp/b25-flipA-countdown.log`，红字 `String: "02 [天] 05:04:59"`）；B **6 红 / 25 绿**（`Temp/b25-flipB-statistic.log`）；C **4 红 / 27 绿**（`Temp/b25-flipC-statistic.log`，`1,234.567` 对期望 `1,234.5`、`1.234.567,891` 对 `1.234.567,89`）。三次都 `cp Temp/b25-*.bak` 还原 + `md5sum -c` 校验（`84368312439b646d7134035aa0f37e7b` / `aebb878f1a5a98ed098808fec01cf3fc`），CSS 还原校验 `c2a54223dd1b84ef15951896efac1968`；还原后全量 **2566/0/0** |

### 3.26 弹层透明遮罩的滚轮穿透守卫（2026-10-12，批次㉖ #130）

| 资产 | 判据要点 | 实测 |
| --- | --- | --- |
| `verify-popup-wheel-passthrough.cjs`（新常驻，26 项 6 组） | 这一类几何/滚动链问题 bUnit 看不见（无布局引擎，§4 第 11 条的老账），守卫只能落在浏览器探针层。分组设计：`A` 组四条全是**前提证人**（A4 `.demo-main` 可滚且视口不可滚、A1 面板外那点最上层是 `position:fixed` 的 `opacity:0` 遮罩、A2 遮罩祖先链 0 个可滚容器、A3 视口不可滚）——前提一破后面全部红得没有意义，所以它们必须**修前修后都绿**；`B` 组（B1 右、B2 上、B3 左+下两额外点位、B4 指针在侧栏时滚的是侧栏）是行为本体；`B5`/`B6` 是**机制证人**，`C1`/`D1`/`D1-pre`/`E1`/`D4` 是**反向对照**（不许抢走原生可滚容器、不许改点击语义、滚到底要交回视口），`D2`/`D2-pre`/`D3`/`D5` 是**边界对照**（真遮罩 `opacity:0.5` 的模态必须仍然锁住后面，同一段代码两种结果），`F` 组把 7 个弹层家族（select / cascader / timepicker / treeselect / autocomplete / colorpicker / dropdown）各跑一遍，防止只对某个组件生效 | **26/26**（RC=0，`Temp/b26-wheel-probe-RESTORED.log`）。翻面（`cp Temp/b26-dom.js.bak` 换回修复前 `dom.js`，**无需重新构建**）：**13 通过 / 13 失败**（`Temp/b26-wheel-probe-FLIP2.log`），红的正好是 6 条 B（B1–B6）+ 7 条 F，而 A / C1 / D 全部仍绿，现场 `main 0->0` ⇒ 只有「谁滚的」这一环被绑住，前提与对照都没被顺手改坏。还原校验 `md5sum`：修复后 `084493229fdac7b700377afe16137f2b`（`Temp/b26-dom.js.fixed`）对修复前 `de198096835f5f63eaf3e062506183c8`（`Temp/b26-dom.js.bak`），**未用 `git checkout --`**。同页回归 `verify-datepicker-panel-sidebar.cjs` **16/16**（`Temp/b26-dp-sidebar-regression.log`），全量 bUnit **2566/0/0** 未动（`Temp/b26-full-suite.log`，本批 0 条新 bUnit 用例） |
| 三条读数口径（写滚动类判据前必读，详见 `docs/popup-shadow-wheel-alignment.md` §6） | ① **Chromium 把 `page.mouse.wheel(0,d)` 的 deltaY 放大 1.25 倍且只发一个事件**：`300→374.9999944120646`、`100→124.99999813735488`、`−200→−249.99999627470976`。期望值要按事件里读到的 `e.deltaY` 比对，不能按传入的 300 硬算，容差 `≤1.5px`。② **`prevented` 是「谁滚的」的唯一证人**：`prevented===true` 且 `Δmain == e.deltaY` ⇒ 我们的转发器；`prevented===false` ⇒ 浏览器原生滚动链。B5（转发，`prevented=true`）与 D5（放行，`notPrevented=true`）用的是**同一个派发手法、只差 opacity**，这一对才是这条修复真正的牙。③ **`scrollHeight-clientHeight` 与 `scrollTop` 可以差 1px**：E1 现场 `main=986 max=987`，「滚到底」类判据必须带 ±1 容差，否则是假红 | 三条都在同一次翻面里被验证过方向：翻面后 B5 的红字是 `prevented=false`（转发器不在场，浏览器自己也没滚 ⇒ `Δ=0`），而 E1 两版都绿（`prevented=false`、停在 max）⇒ 容差与证人口径不是为了让断言变绿而加的 |

### 3.27 站内第三方库（可部署契约）守卫（2026-10-12，批次㉗ #132）

| 资产 | 判据要点 | 实测 |
| --- | --- | --- |
| `test/Element.ComponentTests/VendoredLibraryGuardTests.cs`（新常驻，批次㉗ 落 4 条用例：判据① Fact ＋ 判据② Theory×2 ＋ 判据③ Fact；批次㉙ 起该类共 9 例，新增判据④ 与其前提证人，见 §3.29） | 可部署契约：`src/` 与 `demo/` 的 `*.razor` 里**不许**出现 `参数="http(s)://<公共 CDN>/…"`（只匹配引号里的地址，所以演示页正文里 `<code>cdn.jsdelivr.net</code>` 解释病因不会被误伤）。判据② 用 MD5＋长度核站内那份 hls.js 与 npm 官方 tarball 逐字节一致（MD5 在此只作**改动指纹**，不当安全校验）。判据③ 是**正向接线证人**：`HlsLibUrl="js/vendor/hls.min.js"` 必须在 `VideoPlayerGrowDemo.razor` 里 —— 没有它，判据① 会因为"示例整段被删掉"而恒绿空转（§4 第 16 条 ①的老规矩）。目录里 `SkippedDirectories` 含 `vendor`，所以判据① 不会去审自己引进来的第三方文件 | 4/4（RC=0，`Temp/b27-vendored-guard.log`），全量 **2570/0/0**（RC=0，`Temp/b27-full-suite.log`＝基线 2566 ＋ 本批 4）。三次翻面：A 把示例地址换回 CDN ⇒ **2 红 / 2 绿**（`Temp/b27-flipA-cdnurl.log`，红字点名 `demo\Element.Blazor.Demo\Pages\VideoPlayerGrowDemo.razor:114 ="https://cdn.jsdelivr.net` ＋ 接线证人）；B 往 `hls.min.js` 追加一个空格 ⇒ **1 红 / 3 绿**（`Temp/b27-flipB-bytemod.log`，先被长度判据抓住）；C 把 `hls.min.js.map` 移走 ⇒ **1 红 / 3 绿**（`Temp/b27-flipC-filemissing.log`，红字「站内第三方库文件不见了」）。还原全部 `cp Temp/b27-razor.bak` / `cp Temp/b27-hls.min.js.bak` / `mv`，`md5sum -c Temp/b27-flip-backups.md5` 四条全 OK，**未用 `git checkout --`** |
| `demo/tools/probes/verify-vendored-hls-lib.cjs`（新常驻，22 项 5 组） | 浏览器侧才是这条缺陷发生的层。`A` 组＝前提证人（页面起来 12 个播放器、`_framework` 请求 218 条证明监听器不是空转、站内 vendor 文件过 HTTP 拿得到 200、页面静态 `<script src>` 清单里没有公网 CDN ⇒ 外链只可能来自组件动态注入）；`B` 组＝修复生效（加载期间 0 个 CDN 请求、恰好 1 个请求打站内 `hls.min.js`、注入的 `script.src` 是站内相对路径、`window.Hls.version` 是 1.6.15、`new Hls()` 真发生且 `loadSource` 收到第 7 节的 `.m3u8`、`attachMedia` 挂到该节播放器内的 `<video>`、没有「MSE 库加载失败/接入失败」）；`C` 组＝用户那条报错消失（无 CORS/PNA 红字、无未捕获异常、无失败脚本请求）；`D` 组＝牙（故意注入 CDN `<script>` 必须被监听器抓到、Content-Type 非空且是 javascript、字节数与 bUnit 指纹同为 541870）；`E` 组＝范围外不回归（`/` 与 `/table` 也 0 个 CDN 请求）。`B6/B7/B8` 用 `addInitScript` 包一层 `window.Hls` 取证人，**不依赖公网流媒体能否播通**（`attachMedia` 后 `srcObject` 仍是 `null` 属正常，hls.js 异步建 MediaSource，所以没把「画面真播通」写进判据） | 干净发布包 **22/22**（RC=0，`Temp/b27-hls-probe-CLEANRELEASE.log`）、开发站 5199 **22/22**（RC=0，`Temp/b27-hls-probe-dev-final.log`）。翻面＝**用户部署的那份旧包**（改 razor 前 publish 的 `artifacts/demo-release`，站内 vendor 尚不存在）：**13/22**（`Temp/b27-hls-probe-FLIP-oldrelease.log`），9 条红正好是 A3/A4/B1/B2/B3/B5/B5b/D2/D3，绿的 13 条是前提证人（A1/A2）＋「库真被 new 出来并接入视频元素」那组行为证人（B4/B6/B7/B8/B9）＋ C1/C2/C3 ＋ 牙 D1 ＋ E1/E2 ⇒ 归因到位：**外链请求本身可测、拦截后果只在用户环境出现**。旧包上 `window.Hls.version` 实测 **1.7.3**（`@1` 浮动），站内钉的是 **1.6.15** —— 版本漂移是这条外链的第二宗罪 |
| 发布包重打与残留清理 | `dotnet publish -c Release -o artifacts/demo-release` 不清空输出目录：首查重打后 `_framework/` 里同时躺着改前的 `Element.Blazor.Demo.su0uqc08hw.wasm`（搜站内路径 0 命中）与改后的 `16dwaxmlky`（2 命中），zip 里也就带了一份**过期程序集**。第二查把目录整份删掉重发，`_framework` 里只剩 `16dwaxmlky` 一份、源目录文件数 388（zip 条目从含过期那份的 395 降到 392）。⚠️ `rm -rf` 时若后台静态服务器还开着该目录会报 `Device or resource busy` 并**只删掉一半**（`_framework` 被掏空但 `wwwroot` 还在）—— 先停服务再删 | 核验清单与数字同源落 `Temp/b27-zip-verify.log`：目录 `E:\电子病历\Element-Blazor-master\artifacts\demo-release`（388 文件）、zip `…\artifacts\Element.Blazor.Demo-release.zip`（22,904,044 字节，`md5 0e344a2dc9272caacd3344a83bc0720c`）、包内 `hls.min.js` 指纹 `69991b6ecc5d57cf73bad5b3a1296058` 与源目录一致。新包回归：启动自检 **10/10**（`Temp/b27-release-boot-check-CLEAN.log`）、批次㉖ 滚轮守卫 **26/26**（`Temp/b27-wheel-guard-on-cleanrelease.log`）、日期面板 **16/16**（`Temp/b27-dp-guard-on-cleanrelease.log`） |

### 3.28 日期时间「点日期」这一步的两档口径守卫（2026-10-12，批次㉘ #133/#134）

| 资产 | 判据要点 | 实测 |
| --- | --- | --- |
| `test/Element.ComponentTests/DateTimePickerCloseOnDatePickTests.cs`（新常驻，10 条） | bUnit 这层守**逻辑与事件次数**：默认档「点日期即写值、面板仍开、`change` 一条都不发」（EP 口径，源码抄在 `Temp/b28-ep-use-common-picker.ts:121` 的 `onPick(date, visible=false){…emitInput(result)}` 与 `Temp/b28-ep-panel-date-pick.vue:361` 的 `emit(newDate, showTime.value \|\| keepOpen)` —— datetime 面板 `showTime` 为真 ⇒ 写值但**不关**）；参数档 `CloseOnDatePick`「写值＋收起＋`change` 恰好一次」；空初值时分秒兜底两条（有 `DefaultTime` 用它、没有落 `00:00:00`）；关闭补发 `change` 的**两个方向**（`ChangeOnCloseAfterDatePick` 变了要发 ＋ `ClosingPanelWithoutPickingFiresNoChange` 没变不许发 —— 缺后一条，"无条件补发"的写法也能全绿）；「确定」在参数档点过日期后不许二次发；范围档忽略该参数；表格列透传（正向证人）＋表格默认档仍"写值不关"。弹层证人走 `PopupService.SelectDropDownOptions`（`IReadOnlyList<DropDownOption>`，不是 `IList`）＋`Render(option.OptionContent)` 单独渲染 detached 内容；`Escape` 必须打在 `input.el-input__inner` 上（keydown 绑在它，不在 `.el-date-editor`，否则 `Bunit.MissingEventHandlerException`）。 | 10/10（RC=0，`Temp/b28-tests-newguards-1.log`、`-2.log`），全量 **2580/0/0**（RC=0，`Temp/b28-tests-clean.log`＝基线 2570 ＋ 本批 10）。七次变异**每次只红自己该红的那几条**：M1 点日期回改前行为 **7 红/44**、M2 `notifyChange` 恒 true **4/47**、M3 点日期一律关面板 **7/44**、M4 撤 `DefaultTime` 兜底 **1/50**、M5 关闭无条件补发 change（丢 `valueOnOpen` 判据）**3/48**、M6 表格不透传 **1/50**、M7 单元格编辑器不下发参数 **1/50**（`Temp/b28-mut-M1..M7.log`）。还原 `cp Temp/b28-backup/*.cs` ＋ `md5sum -c Temp/b28-backup/md5.txt` 三份 OK，**未用 `git checkout --`** |
| `demo/tools/probes/verify-datetime-close-on-date-pick.cjs`（新常驻，30 项） | 浏览器这层守**用户在屏幕上看到的那一下**（浏览器层量的是真 DOM：弹层根在不在、真实点击后的焦点与收起、保存后显示态跟不跟得上；本批四条探针点位坑记在 §4 第 79、80、82、83 条）。A 组 `/datetimepicker` 两档并排：默认档写值＋面板仍开＋「确定」仍在＋Escape 关闭才收起；参数档收起＋页内 `change` 计数器 **0→1→仍 1**（开一下不选就关）→**2**（再选一天）—— 这个计数器就是"负向断言配同前提正向对照"的现场版，恒 1 与恒 0 两种写法都会被它抓到。B 组 `/table` ① 拆成「新增行空初值」与「数据行有原值」两段：前者量时间落 `00:00:00`，后者才量得到**只换日期不换 09:30:00**，再点保存看该列显示态跟得上 | **30/30**、RC=0（`Temp/b28-probe-run6.log`；三次翻面还原后复跑 `Temp/b28-probe-restored.log` 同为 30/30）。探针层三次翻面：**改前档 M1** ⇒ **21/30**（9 条红全部落在"点日期写值/提交/change 次数"这几条，`Temp/b28-probe-flip-M1.log`）；**只把 `close` 撤成 `false`**（值仍写）⇒「参数档：点日期后面板当场收起」`panels=1` 红，随后下游点击 30s 超时 —— 面板没关，尺子被产品行为挡住，正是这条判据要防的形状（`Temp/b28-probe-flip-P2.log`，脚本 `Temp/b28-probe-flip-P2.py`）；**撤表格透传 M7** ⇒ 表格段收起判据红、`/datetimepicker` 段全绿（归因在透传链，不在组件，`Temp/b28-probe-flip-M7.log`） |

### 3.29 vendor 文件的「逐字节一致」在检出层会不会成立（2026-10-12，批次㉙ #138）

| 资产 | 判据要点 | 实测 |
| --- | --- | --- |
| `.gitattributes` 末尾一行 ＋ `VendoredLibraryGuardTests.VendoredBytesStayExactOnCheckout`（Theory×4）＋ `GuardedVendoredFileActuallyHasLfEndings` | 换行转换是**按字节判据的隐性敌人**：本机 `core.autocrlf=true` ＋ `* text=auto` 时，库里存的字节对（`git show HEAD:…hls.min.js \| md5sum` ＝ 上游 `69991b6e…`）**不代表检出后对**。`hls.min.js` 实测 LF=2/CR=0 ⇒ 会被检成 541,872 字节，批次㉗ 判据②（长度＋MD5）在全新克隆上必假红。修法是 `demo/…/wwwroot/js/vendor/** -text`，**只圈 vendor**（其余源码仍 `text=auto`：改全站检出形态＝改 git 配置口径，不在授权范围）。判据按 git 的口径解析 `.gitattributes`（逐行、后命中覆盖前命中、`**` 跨目录、`*`/`?` 不跨 `/`）读 `text` 档，两行 vendor 要 `binary`、两行**反向对照**（`VideoPlayerGrowDemo.razor`、`ElDateTimePicker.razor.cs`）要仍是 `auto`；前提证人钉住"被守护的那份真的有 LF"（`.map` 实测 **LF=0**，单行文件转换根本动不了它，拿它当转换风险的证人就是错前提） | 判据类 **9/9**（`Temp/b29-tests-newguard-2.log`、还原后 `Temp/b29-tests-restored.log`），全量 **2585/0/0**、RC=0（`Temp/b29-tests-full-clean.log`）。四次翻面各落自己判据：M1 撤规则 **2 红/7 绿**（两行 vendor；`git check-attr` 同时变回 `text: auto`）、M2 过宽 `** -text` **2 红/7 绿**（红的是两行反向对照；`check-attr` 读数 `src/….razor.cs: text: unset`）、M3 解析器改先命中优先 **2 红/7 绿**（证明判据真按 last-match-wins 语义而非撞字符串）、M4 证人指到 `.map` **1 红/8 绿**（`Temp/b29-mut-M1..M4.log`）。另有一条**本批自己的尺子错**留档：判据④ 第一版把"有 LF"写成 Theory 每行的断言 ⇒ `.map` 假红（**1 红/7 绿/总计 8**，`Temp/b29-tests-newguard.log`）。`git add --renormalize …/vendor/` 零改动 ⇒ 新规则不需要重写已有 blob |
| git 侧三条自证读数（凡是"按字节相等"的判据都该配一组） | ① **提交后的 blob 才是"存的是什么"的答案**：`git show HEAD:<path> \| md5sum`，不要只看工作树那份。② **检出形态由属性决定**：`git check-attr text -- <path>` 出 `unset` 才是"不动它"，`auto` 意味着跟随 autocrlf 在检出时改写。③ **规则是否只是文本**：`git add --renormalize <dir>` 无输出＝新属性与已有 blob 一致，不引发一次全库重写 | 三条读数全部落在 `docs/vendored-library-deployment-alignment.md` §9 与 §9.1，本批的 M1/M2 两次翻面里 `check-attr` 与 bUnit 红名单**同向变化**（规则撤掉 ⇒ 两处都变 `auto`／两行 vendor 红；规则过宽 ⇒ 两处都变 `unset`／两行对照红） |

### 3.30 打包与版本线：nuspec 对不代表包对（2026-10-13，批次㉚）

| 判据点位 | 要点 | 实测 |
| --- | --- | --- |
| 包内**两个**版本号都要读 | 一个 NuGet 包身上有两条独立的版本：nuspec `<version>`（= 文件名、= 依赖边）与 `lib/net10.0/*.dll` 的 **`AssemblyVersion`**。`dotnet pack` 复用"已是最新"的构建产物 ⇒ 只改 nuspec、dll 还是旧的。`--no-incremental` 给 `dotnet pack` 会 `MSB1001`，只有给 `dotnet build` 才认 ⇒ 正确顺序是 `dotnet build -c Release --no-incremental` 再 `dotnet pack -c Release -o nuget` | 三包 dll 用 `[Reflection.AssemblyName]::GetAssemblyName` 复验全为 **`0.10.0.0`**（`Temp/b31-assemblyversion-all.log`）；nuspec `<version>`=0.10.0、依赖边 `Element.X → Element 0.10.0`、`Element.Markdown → Element 0.10.0 + Markdig 0.30.4`（`Temp/b31-nupkg-inspect-0103.log`） |
| 改**内容**不改版本号时，缓存会替你保守旧包 | 消费侧 `dotnet restore` 只看"这个 id + 这个版本在不在全局缓存"，不校验内容。实测旧包 `~/.nuget/packages/element/0.10.0/element.0.10.0.nupkg` MD5 `c71b4bae…` ≠ feed 里新包 `a3094ed8…`，restore 照用旧那份 ⇒ 验收结论会指向一个已经不存在的包。处理：删缓存中**自己的包 id** 目录（`element`/`element.x`/`element.markdown`，可由本地 feed 重解，不碰第三方），再 restore | 清缓存后复 restore，三个缓存 `.nupkg` 的 MD5 与 `nuget/` feed **逐一对齐**（`Temp/b31-app-restore-3pkgs.log`） |
| 消费侧"包真的可用"要看到 DOM，不是看到还原成功 | 只用本地 feed 建一个 Blazor WASM 壳，三个包各挂一个代表组件（`ElButton` / `ElXBubble` / `ElMarkdown`），`index.html` 引 `_content/Element[.X\|.Markdown]/` 的 css+js（**批次㉚ 的读数，包改名前的路径**；批次83 之后基路径随 PackageId 变成 `_content/Element-Plus[...]`，见 §3.31）；判据含**每条包内静态资源 200 且非空** ＋ **同路径下一个不存在的路径必须非 200**（否则"200 且非空"可能被一个 catch-all 路由糊过去） | 一次性探针 `Temp/b31-consumer-verify.cjs` **12/12、RC=0**（`Temp/b31-consumer-verify-2.log`）；首轮 **9/12**，两条红都是尺子自己的（见 §4 第 88 条）。bUnit 全量 **2585/0/0**、RC=0（`Temp/b31-tests-full-1.log`，本批无新增用例） |

### 3.31 包身份改名：基路径是派生值，改名面要在浏览器层复验（2026-10-18，批次83）

| 资产 | 判据要点 | 实测 |
| --- | --- | --- |
| `demo/tools/probes/verify-package-asset-paths.cjs`（新常驻，14 项） | 这条改名的**功能面全在浏览器层**：`_content` 基路径由 `PackageId` 派生，库里有四处硬编码消费点（`ElementLang.LangPathPrefix`、`ElTableV2`/`ElTreeV2` 的 `import("./…/js/dom.js")`、`ElVideoPlayerGrow` 的 `elvideoplayergrow.js` import、`wwwroot/js/dom.js` 的 `CORE_FALLBACK`），其中**三条是 JS 动态 import ⇒ bUnit 完全看不见**。判据分四组：`A1` 前提证人（页面确实声明了 ≥6 条 `_content` 资源、每条 200 且 ≥200 B、12/12 全在新前缀下）；`A2a`/`A2a2` 负向（旧共享前缀四条 **与中间档 `Element-Plus-Blazor` 两条** 全部取不到资源 ⇒ 与对面那个包不再共用路径），`A2b` 是它的牙（新前缀下一个不存在的路径同样 404，否则 A2a 恒真）；`A3a`/`A3b` 取词链路真生效（`lang/zh-CN.json` 200 且含 `inputNumber`，**并且真的写进 DOM**：步进按钮 `aria-label=减少数值`）；`A4` CSS 真应用到像素（primary 底色 `rgb(64, 158, 255)`）；`A5a` 运行期资源请求**零旧前缀**（扫漏的硬编码在这里露）、`A5b` `dom.js` 经新前缀被实际请求过；`A6`/`A6b` 控制台无 error、无落在旧前缀上的 `requestfailed` | **14/14、RC=0**（`Temp/b53-asset-path-probe2.log`，跑在 `--no-incremental` 重建 + 重打包之后的开发站 5199）。首轮 **12/13、RC=1**（`Temp/b53-asset-path-probe.log`），红的是 `A5a`：`performance.getEntriesByType('resource')` 把探针自己在 `A2a` 里 fetch 的四条旧前缀 URL 也算进"运行期请求"——**尺子自己污染判据**，不是产品缺陷（修法见 §4 第 90 条：快照在任何自造 fetch 之前取） |
| `test/Element.ComponentTests/ElementLangTests.cs`（既有类，断言换档） | bUnit 这层只能守住**取词请求的 URL**：`Assert.EndsWith("_content/Element-Plus/lang/zh-CN.json", handler.Requests[0])`。注释里写明「`_content` 基路径 = `_content/{PackageId}`，包改名后随之变」，避免下一次改名时把它当成随手写死的字符串 | 该类随全量套件跑（本批无新增用例）；改名前那份断言是 `_content/Element/lang/zh-CN.json` |
| 消费侧一次性夹具 `Temp/b31-app` ＋ `Temp/b31-consumer-verify.cjs`（批次㉚ 那份，id/路径已同步） | 只用本地 `nuget/` feed 还原三个**新 id**，三个包各挂一个代表组件（`ElButton`/`ElXBubble`/`ElMarkdown`），看的是 DOM 而不是还原成功；六条 `_content/Element-Plus*` 资源 200 且非空 ＋ 同路径下不存在的路径必须 404（牙）。还原前先 `rm -rf bin obj`，再核**全局缓存里三份 `.nupkg` 的 md5 与 feed 逐一对齐**（§3.30 第二条的老坑） | **12/12、RC=0**（`Temp/b53-consumer-verify.log`，base `127.0.0.1:5211`）；`project.assets.json` 里解析到的 id 恰为 `Element-Plus`/`.X`/`.Markdown` @0.10.6（`Temp/b53-app-restore.log`），缓存 md5 三条 `SAME`（`Temp/b53-basepath-readouts.log` 段 C/D） |
| `demo/tools/probes/verify-vendored-hls-lib.cjs` 的**等待条件**（22 项 → 23 项） | 本批改了 `ElVideoPlayerGrow` 的动态 import 路径，这条守卫是它的复验载体，但它一开始在 `page.goto(… { waitUntil: 'networkidle' })` 上 30s 超时。归因：这一页的示例视频是**外链流媒体**（`media.w3.org` 的 mp4、`test-streams.mux.dev` 的 `.m3u8`/`.ts`，见 `docs/vendored-library-deployment-alignment.md` 的剩余外链清单），按 range 持续取分片 ⇒ 全局网络空闲永不成立；诊断档里 9-12s 静默、12s 起又是一串 `trailer.mp4`，11 条 `ERR_ABORTED`，**而 0 条非 200 响应**，且 `_content/Element-Plus/js/elvideoplayergrow.js`、`/js/vendor/hls.min.js` 都在新前缀下取到了。改法不是放宽断言：把"等网络空"换成"等本探针真正依赖的那件事"（`window.__vlg.libAssignedAt` 有值＝站内库被注入并赋给 `window.Hls`，30s 有界轮询），并加 `A0` 前提证人把这一点显式打出来（红＝接入没发生，不再是导航超时） | 改前 **RC=2 FATAL**（`Temp/b53-reg-verify-vendored-hls-lib.log`）＋ 归因诊断 **`Temp/b53-diag-videopage.log`**；改后 **23/23 绿、RC=0**（`Temp/b53-reg-verify-vendored-hls-lib2.log`，`A0 waitedMs=91`、`B2` 站内 `hls.min.js` 恰好 1 次、`D3` 字节指纹 541870 三条与 bUnit 同源） |

### 3.32 滚条悬停加高（6→12px）的守卫与像素判据（2026-10-21，批次94 · #154）

| 资产 | 判据要点 | 实测 |
| --- | --- | --- |
| `test/Element.ComponentTests/ScrollbarCaliberGuardTests.cs`（5 → **6** 例，新增 `Guard_HorizontalBarDoublesOnHover_WithoutTouchingTheVerticalAxis`，`:113`） | 静态钉 CSS 口径，四条钉子：① 静止 `6px` / 悬停 `12px` 且**用 `Assert.Equal(2 * rest, hover)`**（写死 12 就看不出「一倍」这个口径，下次有人定成 10px 也绿）；② 悬停档里**不许出现 `width` 声明**——Chromium 里 `height` 管横条、`width` 管纵条，写上 `width` 就把列对齐那一轴一起改了（牙在 `:132`）；③ 伴生 hover 规则（`.el-thin-scrollbar` / `.el-tree-v2__body`，那段自声明两轴、特异性 (0,1,1) 打平全局且位置更靠后）必须与全局同值；④ 退出钩子那条必须**同时**列出两处归零宿主，少一条就有一类刻意归零的滚道被加高复活 | 目标守卫 **6/6**（`Temp/b55-guard-after-3.log`、`Temp/b55-guard-after-4.log`）。**四档单点翻面各红自己那条，每档 `1 失败 / 5 通过`**：M1 `12px→6px` ⇒ `Assert.Equal() Values differ`（`Temp/b55-guard-mutation.log`）；M2 悬停档补 `width:8px` ⇒ `*:hover::-webkit-scrollbar 里出现了 width 声明：悬停会把纵向 4px 那一轴一起改掉（列对齐依赖它）`（`Temp/b55-guard-m2-raw.log`，消息复跑留证 `Temp/b55-guard-m2-assert.log`）；M3 删伴生规则 ⇒ `index.css 中未找到规则：.el-tree-v2__body:hover::-webkit-scrollbar {`（`Temp/b55-guard-m3-raw.log`）；M4 删退出钩子 ⇒ `退出钩子 --el-sb-h-hover: 0px 整条不见了：悬停加高会复活刻意归零的滚道`（`Temp/b55-guard-m4-raw.log`）。还原用 `cp Temp/b55-index-css-good.css` ＋ `md5sum -c` → `OK`，**未用 `git checkout --`**。全量 **失败 1 / 通过 2764 / 总计 2765**（`Temp/b55-full-suite.log`），唯一红是 §1 记档的 1 红基线 |
| `demo/tools/probes/verify-scrollbar-hover-grow.cjs`（新常驻，**17 项**，`/table` + `/grow`） | 几何与像素**两头都要量**，且每档配对照与翻面：① 裸 div **前提证人**（静止 `hBar:6/vSlot:4`；读 0 表示环境是覆盖式滚条，整支探针作废，不许拿它判产品）；④ 悬停 `hBar:12` ＋ `cssH:"12px"` ＋ **`cssW` 仍 `"4px"`** ＋ 9 列 `th-td` 边界差全 0 ＋ `th`/`td` 相对静止位移全 0（钉的是**列边界**，不拿 `vSlot` 相等当判据，见下）；⑤/⑥ 墨带（悬停画得出来）；⑦ 移出 `6px` ＋ `bandAbove:0`；⑧ 拖曳 `press/mid/end` 全 12；⑨ `scrollLeft` Δ≥40（加高不是为了好看而拖不动）；⑩ **翻面**：页内把钩子压回 6px ⇒ 同一落点量不到 12；⑪⑫⑬ 归零宿主（含 grow 面板）静止/悬停都 `0×0` 且 `hover:true` 为真；⑤w/⑥w/⑦w/⑩w **整帧非空白证人**（`frameNonWhite`） | **17/17 PASS、JS 异常 0**（`Temp/b55-hover-grow-8.log`；`frameNonWhite = 0.2376 / 0.2051 / 0.2061 / 0.2394`）。邻居回归：`verify-scrollbar-wide-h.cjs` **8/8**、`verify-scrollbar-thumb.cjs` **7/7**、`verify-scrollbar-fade-scroll-race.cjs` **8/8**、`verify-markdown-scrollbar.cjs` **26/26**（`Temp/b55-wide-h-regress-1.log` 等） |
| 像素尺子本身的三条标定（本轮各错一次，都是尺子的错） | ① **墨阈值要落在边框与拇指之间**：亮度 ≤225 判墨，悬停拇指 `≈214`、EP 边框 `#EBEEF5 ≈235-238`，用 250 会把边框行算成墨（上轮⑤ 误量 13 行）。② **真表体宿主的墨带天生比厚度少 1 行**：EP 给 `.el-table` 画了 `::before`（1px、`bottom:0`、`#EBEEF5`、`z-index:1`）压在滚条最后一行上，被浅灰判为无墨（`Temp/b55-band-diag-2.log` A0/A3）⇒ ⑤/⑩ 要「厚度−1 行」并以「最底行 RGB 对上 `#EBEEF5`」为证人，**完整厚度的画证交给没有覆盖线的裸 div**（⑥，墨带必须正好 12 行）。③ **`vSlot 0→4` 是 `scrollbar-gutter:stable` 不是加高**：`window.elementBindFadeScrollbar`（`src/Components/wwwroot/js/dom.js:4597`）里 `repaint()`（`:4649-4653`）会把它钉成 `stable` ⇒ 否证档 E（压回 6px，条子不加高）`vSlot` 照样 `0→4`，而三档 `th`/`td` 边界逐列相同（`311.6/371.6/521.6/…`，`Temp/b55-colshift-2.log`） | 三条都由**否证/对照档**坐实，不是由"看着合理"坐实：阈值 250→225 后行数从 13→12；裸 div 与真表体同时在场，前者给完整厚度、后者给真实宿主；E 档与 D 档同尺子同读数，只有 `cssH` 差一档 |

文档改完后复跑（本批各处数字与这两份同源）：`SourceEncodingGuardTests`（它连 `docs/` 与根 `CHANGELOG.md` 一起扫，所以改文档必须复跑它）＋ `ScrollbarCaliberGuardTests` 合并 **10/10 绿、RC=0**（`Temp/b55-docsync-guards-1.log`）；全量 **失败 1 / 通过 2764 / 总计 2765**（`Temp/b55-full-suite-docsync.log`，唯一红仍是 §1 记档的那条基线）；服务侧 CSS 重取一份仍 `ffd70203…` / 568,706 B（`Temp/b55-served-docsync.css`）；`verify-scrollbar-hover-grow.cjs` 在 `dotnet test` 重建 Debug 产物之后再跑一次仍 **17/17、JS 异常 0**（`Temp/b55-hover-grow-9.log`）—— 这条同时排掉「`dotnet test` 把 dev server 的哈希打断 ⇒ 旧资产 404」那种假绿/假红。主目录常驻探针份数 **255 → 256**。

### 3.33 换一轴、留一轴：grow 下拉面板横条装回库口径（2026-10-21，批次96 · #155）

用户裁定链：批次94 收尾时把「要不要给 `ElDropDownTableGrow` 面板装回一根 6→12 的可拖横条」单独挂出来问（没擅自反转批次⑧ 那条批准归零），回答是「**需要**」。
本批因此是**局部反转**：横轴（`::-webkit-scrollbar` 的 `height`）装回库口径 6→12，纵轴（`width`，列对齐的前提）**仍是 0**。
口径与实测在 `docs/caret-scrollbar-focus-ring-alignment.md` §11。

| 资产 | 判据要点 | 实测 |
| --- | --- | --- |
| `ScrollbarCaliberGuardTests`（6 → **7** 例，新增 `Guard_GrowDropDownPanel_HorizontalBarUsesLibraryCaliber_VerticalSlotStaysZero`，`:193`） | 读**组件 `<style>` 源码**（`GrowRazor()` ＋ `StripComments()`），四颗钉子：① 表体/表头 `width` 都得是 0（批次⑧ 的列对齐前提）；② 面板横条的静止/悬停值必须与 `index.css` 全局那条**同数**（`Assert.Equal(globalRest, restH)`、`Assert.Equal(globalHover, hoverH)`）而不是各写一份字面数字，再加 `Assert.Equal(2 * restH, hoverH)` 钉住「一倍」；③ 挂的必须是同两个钩子（`HookOf()` 断 `--el-sb-h` / `--el-sb-h-hover`）；④ 组件悬停档**不许出现 `width`**、表头档 `height` 必须是 0 | 目标守卫 **7/7 绿**（`Temp/b56-guard-before.log`、变异跑完复验 `Temp/b56-guard-mut-after.log`）。**六档单点变异每档都是 `失败 1 / 通过 6`、红的都是同一例**：S1 静止回退值 `6px→0` ⇒ `:214` `Assert.Equal(globalRest, restH)`；S2 删掉组件 hover 档 ⇒ `BlockOf` 报「未找到规则 .el-table__body-wrapper:hover::-webkit-scrollbar」（抛点 `:198`，助手在 `:71`）；S3 表体 `width:0→8px` ⇒ `:203`；S4 悬停档加 `width:8px` ⇒ `:223`，消息「面板的悬停加高档写了 width：会把纵轴槽宽一起改掉…」；S5 表头 `height:0→6px` ⇒ `:228`（`Expected: 0 / Actual: 6`）；S6 悬停回退值 `12px→10px` ⇒ `:215`（`Expected: 12 / Actual: 10`）。逐档日志 `Temp/b56-guard-mut-S1…S6-*.log`；还原一律 `cp Temp/b56-grow-razor-good.razor` ＋ md5 对账 `417c586d…`，**未用 `git checkout --`**（驱动脚本 `Temp/b56-mut-sweep.py` 每档跑完自己 `restore()` 并在末尾复跑基线） |
| `verify-scrollbar-hover-grow.cjs`（**17 → 30 项**，新增 ⑫–⑳ 那一段 /grow 面板） | 换档判据必须**成对**且**两头量**：几何（`offsetHeight-clientHeight` 的 `hBar` ＋ `getComputedStyle(…, '::-webkit-scrollbar')` 的 `cssH/cssW`）＋像素（底边墨带行数）；再加前提证人 ⑫（那台「六列 660px 塞进 360px 面板」的下拉真横向溢出：`sw:660 > cw:342`）、列对齐证人 ⑬c/⑭c（`th`/`td` 逐列左边界差与相对静止档位移都得全 0）、翻面 ⑲（`--el-sb-h-hover: 6px` 压回后量不到 12、`--el-sb-h: 0px` 压回后量不到 6 ⇒ 两档各由自己的钩子驱动）、对照组 ⑳（同页那台**没有溢出**的面板：声明 6px 但 `hBar:0/vSlot:0`，换档**零 DOM 变化**）＋「找不到这种面板＝这一档没跑」的防空转证人 | **30/30 PASS、JS 异常 0、RC=0**（`Temp/b56-hover-grow-2.log`）。关键读数：⑬ `{hBar:6,cssH:"6px",vSlot:0,cssW:"0px"}` → ⑭ `{hBar:12,cssH:"12px",cssW:"0px",vSlot:0,hover:true}` → ⑯ `{hBar:6}`；⑰ `{press:12,mid:12,end:12}`；⑱ `scrollLeft 0->318 Δ=318`；⑮r/⑮h 墨带 `4 → 11` 行（**严格增**，两个死数字改成了一条趋势判据）；⑬c/⑭c 差值全 0 |
| 另一轴的**既有尺子**必须一起复跑（本批的"没碰纵轴"不能只靠嘴说） | `verify-batch8.cjs` 的 G1（表头/表体槽宽都 0 且相等）、G2（面板内逐列 `th/td` 边界差全 0，改前是 `0/0.9/2.9/4`）、G3（注入 8px 槽宽逼重排 ⇒ 边界重新错开、撤掉回 0/0）＝ 批次⑧ 那笔依赖的常驻牙 | **17/17 PASS、FAIL=0**（`Temp/b56-verify-batch8-1.log`，G1 `{body:{offW:316,clientW:316,slot:0}, header:{…slot:0}}`、G3 `{injSlot:[8,0], injDeltas:[1.9,5.9,8], restoreDeltas:[0,0,0]}`）。同轮邻档：`verify-scrollbar-wide-h` **8/8**、`verify-scrollbar-thumb` **7/7**、`verify-scrollbar-fade-scroll-race` **8/8**（`Temp/b56-verify-<name>-1.log`，全 RC=0） |
| 演示页给横条一个对象（`GrowDemo.razor`，`@if (builtSections > 9)` 段内） | **不许为演示新增 `.demo-section`**：`GrowSectionGateGuardTests` 钉着「章节数 == 闸门数 == `GrowSectionCount`」，新增段落会把闸门守卫打红 ⇒ 新内容只能塞进已有 section，且闸门仍紧跟它的 `<h3>` | 探针 ⑫ 现场读到 `sections:21`、`n:12` 台下拉、目标那台 `idx:5` 已点开且 `cols:7`（第 7 个 `th` 是 EP `hidden-columns` 的零矩形，见 §4 第 97 条） |

浏览器层两档（证明**探针**的牙，不只证明守卫的牙；都从当前好档 `262b5e24…` 出发，每档只改一处、重建重启后整支复跑）：

- **MB1 删掉组件自己的悬停档**（`…body-wrapper:hover::-webkit-scrollbar`，md5 → `ad132555…`）：探针 **26/30**（`Temp/b56-verify-scrollbar-hover-grow-MB1-2.log`）——⑭ `{hBar:6, hover:true}`、⑮h `{rest:4, hover:{bandAbove:4}}`、⑰ `{press:6, mid:6, end:6}`、⑱ `Δ0`；`verify-batch8` **17/17**（`Temp/b56-verify-batch8-MB1-2.log`，横轴那一刀不碰纵轴）。
- **MB2 把表体 `width: 0` 改成 `4px`**（md5 → `d6ee56ce…`）：探针 **27/30**（`Temp/b56-verify-scrollbar-hover-grow-MB2-1.log`）——⑬ `{vSlot:4, cw:338}`、⑭ `{hBar:12, cssW:"4px", vSlot:4}`、⑳ 对照组 `{vSlot:4}`；⑫ 的规则证人由 `"0px/6px"` 变成 `"4px/6px"`，面板可视宽 **342 → 338**（正好 4px）。`verify-batch8` **14/17**，G1 `{body:{clientW:312,slot:4}, header:{clientW:316,slot:0}}`、G2 边界差 `0.9/2.9/4`、G3 `restoreSlot:[4,0]`（`Temp/b56-verify-batch8-MB2-1.log`）。
- **还原**：`cp Temp/b56-grow-razor-good.razor` ＋ `md5sum -c Temp/b56-good.md5` → **4/4 OK**（未用 `git checkout --`），重建重启后 **30/30**（`Temp/b56-verify-scrollbar-hover-grow-RESTORE-1.log`）＋ **17/17**（`Temp/b56-verify-batch8-RESTORE-1.log`）。
- **守卫换代**：`ElDropDownTableGrowTests` 的 gutter 守卫从「一条共享规则」改为「逐选择器恰好一次、值 0、两边相等」，改后 **失败 0 / 通过 38 / 总计 38**（`Temp/b56-growtests-fixed-1.log`）；全量 **失败 1 / 通过 2777 / 总计 2778**（`Temp/b96-bunit-full-1.log`，唯一红是 §1 既有基线）。`dotnet test` 重建产物之后再跑一次探针仍 **30/30**（`Temp/b56-post-test-1.log`）。

### 3.34 ElSplitButton：关闭窗口期的 toggle 判据与 option 身份（2026-10-22，批次97 · #159–#163）

功能面取 RadzenSplitButton、渲染取 EP（`el-button-group` + `el-dropdown__caret-button`）；口径与映射在 **`docs/split-button-alignment.md`**。

| 资产 | 判据要点 | 实测 |
| --- | --- | --- |
| `demo/tools/probes/verify-split-button.cjs`（新常驻，**28 项**） | 结构（`el-button-group` 恰两颗按钮、-2~0px 紧贴、等高、箭头窄条、`border-left:0`）；ARIA（`expanded`/`haspopup=menu`/`controls`/label）；portal + `role=menu`；`bottom-start` 几何与 EP 行高（36px / 0 20px）；全套键盘（↓↑ 跳禁用+clamp、Home/End、Enter/Space、Esc/Tab 回焦箭头、首字母定位）；菜单开着点主按钮＝收起**并**执行 Click(null)；点外收起（document 捕获）；AlwaysOpenPopup 不触发 Click；Disabled/Busy 两半同禁；全禁用档 Enter＝原生按钮语义；程序 Close()；图标/IconColor/自定义箭头；收起后无残留。**关闭判据一律 `waitMenuGone`（菜单 ul 从 DOM 移除）**——`h==0` 只是不可见，证明不了 210ms 关闭尾巴已落地 | 修前 **26/28**（`Temp/b97-split-verify-2/3.log`：检查 21「Esc 后 Space 再也开不出」确定性红、检查 28 split-search 菜单残留）；仪器化取证 `Temp/b97-split-verify-4.log` 抓到 `preSpace {menus:[{id,h:0}], expanded:"true"}`＝关闭尾巴未落地、`dropDownOption` 未清；修复后 **28/28 两连绿、JS 异常 0**（`Temp/b97-split-verify-5/6.log`）。主目录探针份数 **256 → 257** |
| 真身缺陷与修法（`ElSplitButton.razor.cs`） | ① toggle 判据原来是 `dropDownOption != null`——`IsShow` 在关闭动画一开始就 false 而 option 要等 210ms 尾巴才清，窗口期内的点击全被吞；② `OnClosed` 不按 option 识别——旧 option 的尾巴若在重开之后落地，会把新 option 引用一并清掉。修法：`IsOpen => dropDownOption != null && dropDownOption.IsShow`、开启前 `DiscardClosingOption()`（尾巴未落地先 ForceClose 摘旧 option，防重复 id 弹层）、`OnClosed` 比 `dropDownOption == option` 才 FinishClose | bUnit 回归锁 `CaretClickDuringClosingWindowReopens`（不等尾巴 → IsOpen 必须翻 false → 点箭头必须能重开 → 尾巴落地后 IsOpen 仍 true）：吞点击与清新 option 两处修反任意一处这条都红 |
| `ElSplitButtonTests`（新，**27 例**） | 宿主同时渲染 `ElPopup` ⇒ 打开菜单走 `RenderDropDownAsync` 量触发器矩形，Loose 模式必须 stub `getBoundingClientRect`（否则 `ElPopup.razor.cs:853` `option.Top = top + rect.Height` NRE，连坐 4 例）；default 类型 caret 补 `el-button--default` 由独立用例钉；Close() 必须在 dispatcher 内触发（见 §4 第 100 条）；开菜单后要与后续派发交互的 8 例一律走 `OpenAndSettleMenu`（等 10ms 开启尾巴渲染的输出 `scaleY(1)` 落地再 `Find`+派发，否则缓存节点上的派发会被 bUnit 静默丢弃——见 §4 第 102 条） | **27/27**（`Temp/b97-bunit-split-3.log`）；flake 取证与验证 `Temp/b97-diag-navflake-1/4.log`；全量两连 **失败 1 / 通过 2804 / 总计 2805**（`Temp/b97-bunit-full-3/4.log`），唯一红仍是 §1 记档的既有基线 `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows`（本批改动清单里没有 ElTable，非连带） |
| 打包 0.10.25 核验（`Temp/b97-nupkg-verify.py` → `.log`） | §6 四条 + 批次特有：**类型名查 ASCII #Strings 元数据堆、字面量查 UTF-16LE 字节模式**（见 §4 第 101 条）；`css`/`dom.js` 包内 md5 == 源码 | nuspec id/version/authors/依赖边全对；dll `_content/Element-Plus` UTF-16LE **4 处** == 源码 4 处、旧前缀 **0 处**；`ElSplitButton` ×17 / `ElSplitButtonItem` ×4（对照 `ElPopup` ×35）；`el-dropdown__caret-button` ×6、`el-button-group` ×49；nupkg 1,793,986 B md5 `488699f7…` |

### 3.35 打了本地补丁的 vendor 文件：怎么钉住「补丁还在」与「检出不改字节」（2026-10-23，批次99 · #164）

缺陷本体与补丁表在 **`docs/viewer-alignment.md` §11**；本节只记尺子。被测对象是
`src/Components/wwwroot/js/ofd/ofd.umd.min.js` —— 上游 ofd.js 0.2.6 **加**本地补丁 P1–P4，
所以它既不是「上游逐字节」（demo 的 hls 那一档），也不是自己写的代码，判据要同时挡住
「上游覆盖回来」和「git 检出换行改写」。

| 资产 | 判据要点 | 实测 |
| --- | --- | --- |
| `ElViewerGrowTests.Guard_OfdCore_CarriesTheLocalPatchAndIsBytePinned`（新） | 长度 927,195 + MD5 `8b9ea235…` + 六条锚点各在（P0 头、P1a `@_Loc`、P1b warn 文案、P2 键归一、P3 零号盒、P4 前导斜杠）+ 三条锚点**恰好 1 次**（防重复打补丁）。锚点串与 `Temp/b99-vendor-patch.py` 同源，改补丁必须同步改这里 | 改后 **9/9 绿**（`Temp/b99-guard-ofd-run-2.log`）；**翻面**：把上游原版放回 vendor 路径 ⇒ **失败 1 / 通过 8**，红就红在长度钉 `ElViewerGrowTests.cs:2449`（`Temp/b99-guard-ofd-flip-pristine-1.log`）—— 有牙 |
| `VendoredLibraryGuardTests` 判据④ 扩面 | `VendoredBytesStayExactOnCheckout` 加 `src/Components/wwwroot/js/ofd/ofd.umd.min.js` 的 `-text` 档；`GuardedVendoredFileActuallyHasLfEndings` 由 `Fact` 换 `Theory` 给两份文件各当**前提证人**（无 LF ⇒ 换行转换动不了它 ⇒ 判据空转）。`修法` 提示文案原来硬编码 demo vendor 目录，改成引用 `relative` 自身 | 5 条规则 + 2 条证人全绿；反向对照（`.razor` / `.cs` 仍落在「会转换」档）同跑 |
| `demo/tools/probes/verify-ofd.cjs`（常驻，25 → **46 项**） | 新增 §9 OFD-H 病案首页、§10 默认命名空间信息卡片、§11 HCView 横向单页；每档都判**几何与对象数**（页数 / 非空白字数 / path / svg / img / 页幅比例 / 内容包围盒覆盖大半页且不出页），不只判「没抛异常」；样例文件在场写成独立判据（`ofdFixture()` 按仓库根算，见 §4 第 104 条）；负向判据（`signature layer skipped` 不出现）前先注入 `ofd: probe-ruler-witness` 证明台账看得见同类消息 | 改后 **46/46、控制台 0 错误**（`Temp/b99-verify-ofd-after-3.log`）；改前（上游原版核心）**35/46**，11 条红全在 OFD-H 两段，页面直接读到 `t.indexOf is not a function`（`Temp/b99-verify-ofd-before-2.log`）；基线段 1–8 与护理记录单档**两档同数全绿**＝红是被测文档特有，不是会话状态 |
| 离线 A/B（`Temp/b99-ofd-parity.cjs`，两核心 × 7 样例） | 同一台无头浏览器、同一份页、换 `<script src>` 指向不同核心；每档 reload 防缓存；读 `parseOfdDocument` 的 success/fail 与 `renderOfd` 后的 DOM 计数 | `Temp/b99-ofd-parity-3-ship.log`：5 份样例两核心**逐字相同**（补丁不吃劲），信息卡片与病案首页 失败→打开，`hcview` 两核心同为 1 页 / 155 字 / 2448 path |
| 发布包档（真静态托管，用户实际部署的那一层） | 尺子完全复用同一支探针，只把 BASE 换成 `node Temp/b27-static-server.cjs artifacts/demo-release/wwwroot 5203`；**翻面档就是重发前那份包**（里面躺着未打补丁的核心），不必手工换文件；核验要落到「包内 + HTTP 吐出的」两层字节，且 `.gz`/`.br` 要**解回原始字节**再比 md5 | 旧包 **29/41**（12 红）→ 新包 **46/46**、控制台 0 错误；新包与开发站逐条同读数（3 页 / 1367 字 / 87 path / 169 svg / 包围盒 `{x:96,y:148,w:816,h:725}`）。⚠️ 旧包多的那条红是「样例按钮 count=0」——**旧包压根没有护理记录单这份样例**，它下游 4 条判据整段没跑（46→41），记账时要与产品缺陷分开，否则会把「包里没有样例」读成「补丁吃掉了样例」。同包回归：启动自检 10/10、vendor 23/23、Viewer 交互 20/20（`Temp/b99-release-*.log`） |
| 全量归因 | —— | **失败 1 / 通过 2811 / 总计 2812**（`Temp/b99-full-bunit-1.log`），唯一红仍是 §1 记档的既有基线 `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows`；对比批次98 的 2809 总计 **+3** ＝本批新判据 1 + 判据④ 新 `InlineData` 1 + LF 证人换 `Theory` 多 1，零新增红 |

**变异/还原纪律**：改前档靠**换核心**做（`cp Temp/b99-shipped-core-pristine.js` → vendor 路径），
每档都要重新 `dotnet build` + 重启 dev server（静态资源带指纹，`.gz` 也要重生成，否则浏览器拿到的还是上一档的字节），
并用 `curl --compressed … \| md5sum` 自证服务端此刻吐的确实是那一档（plain 与 gzip 两式都验）；
还原用 `cp Temp/b99-core-patched-ship-backup.js` + `md5sum -c`（报 `OK`）+ `cmp` 三重，未用 `git checkout --`。

### 3.36 「上一帧就被移除的元素」这种缺陷只在 JS 侧可见：三层判据与它的翻面（2026-10-24，批次100 · #166–#168）

缺陷本体与修法在 **`docs/viewer-alignment.md` §12**；本节只记尺子。难点是这类缺陷的**症状落在浏览器**
（`ElementReference` 随上一帧被移除 ⇒ interop 到 JS 收到 `null`），bUnit 没有布局引擎、也从不真的把引用交给 JS，
所以「一层判据」必然漏：本批用了三层，每层都带翻面档。

| 层 | 判据怎么搭 | 实测 |
| --- | --- | --- |
| ① bUnit（决策层） | **不在断言里判几何**，只判「挂载调用发生的那一刻，挂载点在不在渲染树里」：给 `JSInterop.Setup<int>("elementOfdViewerMount", …)` 的谓词做副作用记账（`cut?.Markup.Contains("el-viewer-grow__ofd")`），`Markup` 而非 `Nodes`/`FindAll`（§3.22）；**有界性**另断计数（`Assert.Equal(2, …)`），负向断言（「不许挂」）前面必须有一条正向对照（「确实放行了一次重挂」） | 本组件 **184/184 绿**（`Temp/b100-bunit-full-2.log`）；**M1** 红 `[1]: Item: False`（`Temp/b100-flip-M1-witness-1.log`）、**M2** 红在命令式那条（`Temp/b100-flip-M2-1.log`）——两处修法各有自己的翻面证人 |
| ② 一次性插桩台账（真相层） | `context.route('**/js/dom.js*')` + `route.fetch({headers:{'accept-encoding':'identity'}})` + `route.fulfill(body: text + INJECT)`：interop **不会在调用点重读 `window.*`**，事后改 `window.elementOfdViewerMount` 包不到，必须把包装搭在响应体上；记录 `containerNull / containerConnected / hostsInDocument / file / result\|rejected` 并落 `sessionStorage`；开头一条**仪器自证**（看不见 boot 那次真实挂载就整轮作废） | 修后 **12/12、`nullOnMount=0`**（`Temp/b100-nullhost2-after-2.log`）；**M1** 档 3 FAIL、`nullOnMount=1`，用户那行 `container is null` 当场复现（`Temp/b100-nullhost2-flipM1-1.log`）；未修版 `nullOnMount=1`（`Temp/b100-nullhost-before-1.log`） |
| ③ 常驻探针（回归层，`verify-ofd.cjs` 46 → **61 项**） | **不插桩**，只看用户可见的事实：34 字节垃圾文件 → 必须落进 `parse failed`（不是「容器为空」）且那一帧挂载点数=0 → 紧接着传正常文档 → 必须画出 3 页且文案不许是「地址不被允许」→ 再传第二份垃圾 → 点「最大化」→ `.is-maximized` 真在场（前提证人）→ 文案仍不许出现 `container is null`；注释层判 `[data-ofd-annot]` 的 `type/id/尺寸`、`title` 里的 Creator + LastModDate + Remark、印章红 `rgb(183,28,28)` 命中数，且红色采样器自带**注入同色节点命中数 +1** 的有牙证人 | 修后 **61/61、控制台 0 错误**（`Temp/b100-verify-ofd-after-2.log`）；**M1** 档 **60/61**，红**只有**目标那条，detail 直接回吐 `hasNull=true hasParseFailed=false … tail="…dotnet.native.rw4kynp763.wasm:wasm-function[256]:0x1d974"`（`Temp/b100-verify-ofd-flipM1-2.log`）⇒ 现场即可归因，不必重跑 |

**字节钉的另一档用法**：补丁加到 P5–P7 后，守卫的**天然翻面档就是上一批那一档核心**
（`Temp/b99-core-patched-ship-backup.js`，`8b9ea235…` / 927,195 B）—— 放回 vendor 路径 ⇒ 守卫红在长度钉
（`Temp/b100-guard-ofd-flip-b99core-1.log`），不必去造上游原版。长度钉先炸会挡住后面的锚点断言，
所以另数一遍字符串：三条新锚点在批次99 版里**命中 0 次**、现档各 1 次，锚点钉单独也有牙。

**「这一档到底是不是被测的那份字节」要自证**：本批改一次 C# 就要重 build + 重启 dev server，
最省事的证人是**内容寻址的文件名自己变了**（`_framework/Element.ejlcik5v1k.wasm`（M1 档）→ `Element.mbkzk662xs.wasm`（还原档））；
只看时间戳会被增量缓存骗（`obj/webcil`、`.gz` 都只比时间不比内容）。

**样例数字随补丁变化要当场判性质，不能凭手感**：同一份样例在 P5–P7 之后从 1367 字 / 87 path / 169 svg 变成 1392 / 88 / 173，
常驻探针立刻红一条。先离线数一遍包内到底有没有注释层（`Temp/b100-fixture-annot.cjs` → `Temp/b100-fixture-annot-1.log`：
`annotCount=1 types=["Path"] ids=["11"]`）再决定「钉住新数」还是「回退补丁」——
反过来（先改判据去迎合读数）就等于把回归合法化。

### 3.37 「画面上的小交互件」怎么钉：通道后画进来的 DOM、打印档与根盒子高度账（2026-10-24，批次101 · #174/#175）

缺陷本体与实现见 `docs/viewer-alignment.md` §13；本节只记尺子。本批的特殊处在于：
角标是 `dom.js` 在 paint **之后**画进来的真 `<button>`，bUnit 里它压根不存在；
「打印不显示」只能靠媒体查询，而 bUnit 没有媒体；「状态栏被裁」是纯几何，bUnit 也没有。

| 资产 | 判据要点 | 实测 |
| --- | --- | --- |
| `Guard_OfdCore_…`（扩面） | 长度 928,111 + MD5 `9685428c…` + **十条**锚点各在 + **七条**恰好 1 次（新增 P8 `data-ofd-annot-remark`）。**天然翻面档＝上一批核心**（`Temp/b100-core-p5p7-backup.js`，927,798 B），不必造上游原版 | 翻面红在长度钉 `Expected: 928111 / Actual: 927798`（`Temp/b101-guard-flip-b100core-1.log`）；P8 锚点在批次100 核心命中 **0** 次、现档 **1** 次。还原靠**重放补丁** `Temp/b101-vendor-patch.py`（干跑先印同一对字节 / MD5 再 `--write`），守卫 1/1 绿（`Temp/b101-guard-restore-1.log`） |
| `Print_CssSection_…`（扩面） | `@media print` 段里必须同时出现 `.el-viewer-grow__ofd-annot-marker` 与 `.el-viewer-grow__annot` | 随全量 2819/2819 绿 |
| bUnit 三条注解判据（`ElViewerGrowTests.cs:2334/2375/2393`） | 点宿主 → 卡片出现且 Remark / 作者 / 时间 / 标签齐全；通道回 `null` 不渲染遮罩；换文档先关卡片、空作者 / 时间不画空行。**卡片本体点击不是 bUnit 能测的**（只挂 `stopPropagation`、无 handler ⇒ `MissingEventHandlerException`）：钉接线即可，行为交给真浏览器 | 3/3 绿；翻面：去掉 `OnOfdHostClickAsync` 开卡片那句，第一条红 |
| `verify-ofd.cjs`（61 → **78 项**） | 角标：数量＝批注数、真 `button` + `(i)` 圆点 + `aria-label`＝Remark、钉在批注盒右上角（≤4px，两边都不是零号盒）、屏幕媒体可见；卡片：真鼠标点角标 → 内容 / 作者 / 时间齐全、落在可视区内、遮罩钉根盒子、点卡片不关 / 关闭关 / 遮罩空白关；打印：`emulateMedia({media:'print'})` 下两件 `display:none`，且**同一次运行里先读到屏幕媒体的 `flex/block`**（正向对照），最后切回屏幕媒体确认重新可见；根盒子：`overflow===0` + 状态栏中心点 `elementFromPoint` 命中自身 | **78/78、控制台 0 错误**（`Temp/b101-verify-ofd-5.log`；四档翻面还原后复跑同为 78/78，`Temp/b101-verify-ofd-final-1027.log`）；**翻面 D**（删 dom.js 角标段）⇒ 4 红（`markers=0` / 没有 button / 没有位置 / 不可见）+ 点角标 FATAL（`Temp/b101-flip-D.log`）；**翻面 E**（删 print 规则）⇒ **76/78**，恰好两条打印红，detail 回吐 `overlayDisplay=flex` / `markerDisplay=block`（`Temp/b101-flip-E.log`）；**翻面 G**（根盒子压回 `block`＋预览区 `height:100%`）⇒ **76/78**，恰好两条根盒子红（`overflow=70` / `statusHit=false`，`Temp/b101-flip-G.log`）；**翻面 F**（遮罩挂回 `__viewport`）⇒ **76/78**，两条红（见下面两行） |
| 打印判据的两侧证人 | 只判 `none` 会空转：整块样式都没应用时读到的也是 `none`（display 默认不是 none，但一旦规则没生效就是可见）。所以必须**先读屏幕媒体**（`flex` / `block`）→ 再读打印（`none`）→ 再切回屏幕媒体（重新可见），三段读数都进 detail | 三段都在 `Temp/b101-verify-ofd-5.log` 的 57–60 行；翻面 E 档红的就是前两格 |
| 根盒子高度账 | 定高 viewer 里「预览区底部被裁 / 状态栏只剩一条边」用两条几何判据：根盒子 `scrollHeight - clientHeight === 0`，以及**状态栏中心点的 `elementFromPoint` 命中的是状态栏自己**（被裁掉时命中的是背后的 `.demo-section`） | 改前根盒子 `scrollableOverflow=70`、状态栏中心命中 `DIV.demo-section`（`Temp/b101-status-visible-2.log`）；改后 `overflow=0`、`statusHit=true`（`Temp/b101-root-box-2.log`，探针 48–49 行）；**翻面 G** 把这两条压回旧口径 ⇒ 恰好这两条红，读数 `overflow=70 client=558 scroll=628` / `statusHit=false status=826..855 rootBottom=827`（`Temp/b101-flip-G.log`） |
| 浮层的包含块要当判据读，不能只读「看得见」 | 遮罩 `position:absolute; inset:0` 挂在**滚动容器**里时，铺的是 `scrollHeight` 而不是可视框：卡片被居中到内容正中（三页 OFD 实测 `scrollHeight 4409 / clientHeight 558`），屏幕上是「背景变暗但没有卡片」。判据必须直接读**包含块本身**：`offsetParent` 的 class ＋ 卡片 `top/bottom` 与 `innerHeight` 求交；「点遮罩空白关」还要把**空白点自己是否命中遮罩本体**回吐（`hitsOverlay`），否则红的时候分不清是没关还是尺子没够着 | **翻面 F**（遮罩挂回 `__viewport`）⇒ 两条红：`offsetParent=el-viewer-grow__viewport` + `cardTop=-557 cardBottom=-382`、`blank=0,0 hitsOverlay=false cards=1`（`Temp/b101-flip-F.log`）；现档 `offsetParent=el-viewer-grow` + `cardTop=453` + `hitsOverlay=true cards=0` |
| 用户原文件的验收（#175）：期望值从包里读，不手写 | 常驻探针钉的是站内副本（90,720 B 的桩字体），拿它的页数 / 批注数 / 文案去套用户那两份（6,026,308 / 11,841,181 B，内嵌 10,570,564 B 仿宋 TTF）就是错前提。一次性驱动 `Temp/b101-accept-two-files.cjs` 先用 `Temp/b101-ofd-manifest.py` **离线读包**得到页数、`PageID`、每条批注的 `Type/ID/Creator/LastModDate/Remark/Boundary`，浏览器侧只数 `.el-viewer-grow__ofd-page` 与 `[data-ofd-annot]` 再逐字比对角标上的 data 属性 | **42/42、控制台 error 0**（`Temp/b101-accept-two-files-4.log`；两份各 3 页 / 1 条批注 / `ID=11` / `Boundary="148 246 38 18"`，渲染耗时 16s / 18s）。两条假绿前提的细则在 §4 第 115 条；被 `K()` 吞掉的 `TypeError` 写成 `OBS` 点名而不是塞进「0 错误」 |


### 3.38 「参数存在、行为为零」这种缺陷怎么钉：优先级不在布尔里、调用次数不硬编码（2026-10-27，批次105 · #178/#179）

缺陷本体与八档变异见 `docs/table-formatter-alignment.md`；本节只留**尺子层面可复用的四条**。

| 规矩 | 抓到它的判据 | 读数 |
| --- | --- | --- |
| **①「照抄上游语义写出的守卫条件」要用全量套件否证，不是靠它读起来对** | `UseCellFormatter` 最初按 EP 写了 `Template == null` / `Type != "index"` / `!IsCheckBox` 三个豁免，看起来正是「插槽优先」的钉子。**M6 把三句全删 ⇒ 全量 2836/2836 仍绿**（`Temp/b105-mut-M6-1.log`，`FAILED_TESTS=0`）—— 三句是死代码，真正钉住优先级的是 razor 里的分支顺序 + 两支改呈现树顺序才会红的用例（`TemplateColumn_WinsOverFormatter` / `IndexTypeColumn_DoesNotRunFormatter`） | 派生布尔的每一支都要单独压一次；压完不红的就是装饰，要么删要么换成真判据（本批**保留** `header != null`，M2 同档也是 0 红，但它是入参护栏而不是语义护栏） |
| **② 调用次数不能硬编码，先量组件渲染几拍** | `ElTable` 在 bUnit 里**渲染两拍**（实测 `cut.RenderCount == 2`，转储 `Temp/b105-markup-dump.txt`），首轮 12 例红 9 例，一半栽在「formatter 被调了几次」上 | 一律写成 `格子数 × cut.RenderCount`；异常那条更窄：每格的 formatter 跑在自己的 `ElOverflowTooltip` 子渲染里，数据源降到 **1 行**才让 `Calls == 1` 成立 |
| **③ 负向判据的 scope 要避开自己写的说明文字** | 探针第一版判「插槽优先列里绝不出现 `不该出现`」读的是整节 `sec.textContent`，而演示说明段落**原样写着** `不该出现` / `非原值` 这两个 needle ⇒ 无论行为好坏都永远绿 | 改成只读 `table.textContent`；顺带规矩：**演示页里当假绿探测器用的串，判据只能扫表格本体，不能扫包含说明文字的容器** |
| **④ 库级翻面要让行为自己作证，不必数包字节** | `Temp/b105-flip.py apply` 把 `UseCellFormatter` 压回 `false` 再重打 demo ⇒ 探针 **通过=10 失败=4、RC=1**（`Temp/b105-probe-red-1.log`），红的四条格子里回吐裸原值 `12.5` / `320` | 「格子从 `¥12.50` 变回 `12.5`」这条**读数差异本身**就是变异进了被服务 WASM 的证据，比 md5 更贴用户口径；还原后 `-green-2.log` 复跑 14/14 当第二证人 |

新常驻探针 `demo/tools/probes/verify-table-formatter.cjs`（**14 项**，统一跑法 `node <probe>.cjs http://127.0.0.1:5199 > 日志 2>&1; echo "PROBE_RC=$?" >> 日志`）另有两条老规矩的落地样本：表头文字→列序号**当场映射**再取格（列序一改就红，不产出错位读数）；控制台 canary 先注入 `probe-canary-table-formatter` 并**断言数得到它**，再判「本轮 0 真错误」。
另记一条 Razor 层的硬教训（属实现但只有测试能揭穿）：`@((object)v)` 走 `RenderTreeBuilder.AddContent(int, object)`，那个重载只认 `string`，于是 `MarkupString` 被转义、`RenderFragment` 打成类名 —— 接线前那档的呈现树转储 `Temp/b105-markup-dump.txt` 里两种垃圾都出现过，**bUnit 的子串断言当时也没能看出**（`<!--!-->` 注释帧干扰），是靠数真 `<b>` 元素才判对的。


## 4. "是组件错还是测试错"的取证顺序
本会话 4 例长期红的用例最后都是**测试写错**，判法固定：

1. 查 EP 权威源码（`.vue`/`.tsx` + `theme-chalk/*.scss`），不凭记忆。例：`is-bordered` 挂在 `.el-descriptions__table` 而非根节点；TableV2 展开图标是 `el-table-v2__expand-icon` + `is-expanded`（`el-table__expand-icon` 是**旧一代** el-table 的契约，两代不可混用）。
2. 查本仓 `index.css` 实际用的选择器（CSS 与组件一致就说明契约在哪）。
3. 查同名**专用测试类**是否已按正确契约断言且常绿 —— 若是，红的那条多半是重复且写歪的冒烟用例。
4. 竞态类问题用**反射探针**拿硬证据：打印私有状态（`dropdownVisible`、`activeMentionStart`）、实例数、每一步命中元素数 —— 据此查过 `GeneratedMention`（结论：防抖 300ms，测试没等，组件无错）。
5. 断言空洞自查：只测"开"不测"关"（TableV2 补"再点一次必须收回"）；只测"类存在"不测"几何生效"（Descriptions 补边框 1px/0 计算值）。
6. 收尾区分 flaky：可疑用例**单类复跑**，能过就是并行抖动；再按 §1 把超时显式化根治。
7. **"渲染了几次"不能用 bUnit 的 `cut.RenderCount` 判定** —— 它统计的是这一批调度里被渲染组件
   **子树内**的帧变化，子组件的渲染会记在父组件头上。实测：只渲染一个 `ElInput` 的宿主输入一次，
   自身 `BuildRenderTree` 增 1、`RenderCount` 增 2；表格单元格输入一次，`ElTable` 自身增 **0**、
   `RenderCount` 增 2（那 2 次是单元格宿主 + 输入框）。要证"某个组件没重渲染"，就在测试里
   子类覆写 `BuildRenderTree` 自己数（`TableEditCellTests.CountedTable`），并且必须带一个正向对照
   （确有重渲染的场景数字要上涨），否则计数器接不上时断言会空转成"绿"。
   这一条曾经把性能收尾带偏：据此记下的"还剩第二个来源"其实不存在，见 `docs/table-edit-issues.md`。
8. **"单跑必绿、并行才红"先怀疑跨作用域的 `static` 可变状态，不要先怀疑时间预算，更不要先加
   `[Collection]` 把并行锁起来**。本仓抓到过一次：`DialogService.Dialogs` 是 `internal static`，
   而 `ElPopup.OnInitialized` 对弹层集合做的是"解订阅 → 换成新实例 → 再订阅"，
   于是后初始化的宿主让先初始化的宿主守着一个没人写入的旧集合 → 弹层永远不出现（服务本身是
   `AddScoped`，注册早就是按作用域的，只有字段忘了去 static）。
   找真身的做法：写一条**确定性**复现（两个 `BunitContext` 各起一个宿主，见 `PopupHostIsolationTests`），
   而不是靠反复并行赌一次红；修好后把并行锁解掉再连跑 20 次，才算证完。
9. **"调用了却什么都不出现、而且 0 个异常"先怀疑归属被接管，不要先怀疑 CSS / z-index**。
   本仓两个静默缺陷（`DialogService.Dialogs` 是 static、`LoadingService` 是 Singleton）症状一样：
   前一个宿主/连接守着一个没人写入的旧集合。查法是把"共存"造出来再读 DOM：
   **bUnit 双 `BunitContext` 造不出 Singleton 那种共存**（每个 context 自建 provider），
   必须换成同一个 `ServiceProvider` + 两个 `CreateScope()`；浏览器同理，`page.goto` 会新建 DI 作用域，
   必须站内跳转（见 §3.11）。还有一条通用自查：**"能关掉 / 已消失"这类负向断言在缺陷态下会空转 PASS**，
   必须与"确实出现过"的正向断言成对写。
10. **"中文显示成乱码 / 但编译和运行都正常"先查文件的存盘编码，不要先怀疑渲染层**。
    本机 ACP=936 时 Roslyn 对无 BOM 源文件是"UTF-8 失败 → 按 GBK 再解一遍"，所以 GBK 存盘的中文
    在本机**完全正常**（DLL 里是正确 UTF-16 字面量，实测命中），只在别的机器、别的工具上坏。
    判据是 `git ls-files` 逐个严格 UTF-8 解码 ＋ 扫 `EF BF BD`（后者是"字符已经丢了"的痕迹，转换救不回来只能重写，
    常见成因是拿编辑工具直接改一份 GBK 文件）。三类损坏与实测数字见 `docs/source-encoding.md`。

11. **bUnit 全绿不等于布局没坏 —— bUnit 没有布局引擎。** 凡是"能不能收缩 / 会不会溢出 / 浮层被不被裁"这类
    **几何**结论，bUnit 只能证"类与属性输出对了"，证不了"结果真的生效"。本仓抓到过一次：`ElMenu` 的截断
    tooltip 触发 span 是 flex item，自动最小尺寸＝min-content，必须 `min-width:0` 才允许收缩 ——
    把它改成 `min-width:auto` 之后 **`ElMenuTests` + `ElNavigationTests` 62 例全绿（失败 0）**，
    而浏览器里文字重新冲出菜单边框（`li scrollWidth 322 > clientWidth 179`，文字右缘 `633` vs 菜单右缘 `491`）。
    做法：**用 computed 值的等价变异来证明探针有牙**（同一 DOM 只改 `min-width`，量 `scrollWidth` / `clientWidth` / 右缘），
    别只靠"探针这次是绿的"下结论。变异脚本本身也要防一种假变异：删掉 ` Style="min-width:0">` 得到的是
    `error RZ1035 / RZ1024`（标记不闭合）—— 那是**语法破坏**，编译期就红，测不到任何语义回归。
    见 `docs/menu-alignment.md` §9。
12. **赌不出的红也要记在案上，别当成"没红过"。** 本批**第一次**全量红过一条与本体无关的墙钟用例
    `ElNotificationTests.PauseOnHover_KeepsNotificationAlive_AndResumesAfterLeaveAsync`
    （读数 `失败 1 / 通过 2309`；用例耗时 `[4 s]` ≈ `Task.Delay(900)` ＋ 那 3 秒 `WaitForAssertion`，
    所以红的是"mouseleave 之后 3 秒内没消失"那一半，不是"悬停没暂停"那一半）。
    单类复跑 **18/18 绿**；其后 **10 次全量（其中 6 次另开 12~24 个 CPU 饱和进程）都没再红**。
    那次红的错误详情**没拿到** —— 取数脚本只 print 含 `Failed` / `error` 的行，把 stack 截掉了，
    所以案上只有读数、没有真身。按 §3.9 与第 8 条的判据它属于"墙钟预算与调度耦合"的形态
    （用例里 `Duration=600` / `Delay(900)` / 超时 `3s` 三个数都是硬编码的），
    但**没做成确定性复现就不算定位**，也因此**不许**用"加大超时 / `[Collection]` 锁起来"把它按住。
    下次再红先做两件事：① `dotnet test` 的**全输出整份落盘**（别只打印过滤后的行，否则真身每次都丢）；
    ② 在 mouseleave 之后每 100ms 打印 `NotificationOption` 的 `ElapsedMs / Paused / StartedAt`
    （`ElPopup.razor.cs:610` 的 `AutoCloseNotificationAsync` 就是按这三件套算剩余的），
    分清是"计时没恢复"还是"恢复后那次重渲染没被调度"。

13. **"偶发红"的下一个嫌疑人是别人的 `scrollTop` 写回 —— 用劫持 setter 打栈，比读日志快。** 本批
    `verify-ellipsis-tooltip.cjs` 的翻面段 **10 次红 3 次**，读数永远是"我把容器滚到 top=20 之后又回到 798"。
    三步定位：① 孤立复现（只藏 chrome + 滚）→ 不红，说明**需要前序动作**；② 逐句镜像探针（同样的 hover /
    `waitForFunction` / `addStyleTag` 顺序）→ 5 次红 1 次，抓到 `scrollTop` 被**第二次写成 0**，而 `scrollHeight`
    没变（＝不是内容塌了导致的钳位，是有人主动赋值）；③ 在被滚的那个元素实例上
    `Object.defineProperty(el, 'scrollTop', {set(v){ log.push(new Error().stack) … }})` → 栈直接指向
    `dom.js:4182`，即滚条淡入的 `repaint()` 在两帧之后把位置**钉回"进入时"读的旧值**，盖掉了窗口里的真实滚动。
    判据侧的收获：`repaint` 的 auto↔scroll 互换**自己不会移动位置**（`diag-fade-writeback.cjs` 实测三个宿主两轴偏移全 0），
    所以那条写回是 `overflow:hidden` 年代的遗留 —— 删掉它，同时补 §3.13 那条带正向对照的竞态探针。
    顺带一条同类旧账：`repaint` 上面那段 `isVirtualScroller` 注释记的正是"跨帧 overflow 切换 + 事后钉回旧值 ⇒ 回跳"，
    当时只给虚拟滚动容器开了豁免，普通滚动容器身上同一把刀一直没处理。
14. **旧探针红了先做归因，别顺着它改本批的代码。** `probe-scrollbar-final.cjs` 这次报
    `.demo-aside-scroll 交互后拇指若隐若现显形 dark=0 min=255`（12/13）。归因三步：把**本批改动临时放回旧实现**跑 →
    同样红；再跑还原版 → 同样红；**3/3 稳定同一条、与写回在不在无关**，且实测 `.demo-aside-scroll`
    `scrollHeight 4600 > clientHeight 1344`、`overflowY:auto`（不是"不再溢出"这种前提失效）。
    结论：这是一条**先于本批存在**的红，本批不顺手修（修它要另开一批查 aside 的 hover 通路），只在案上留读数与判据。
    反面教训：如果因为这条红就把本批的修法改窄（或给探针加超时/重试），就把两个不相干的缺陷焊死在一起了。

15. **「浮层看不见」有两种成因，先看矩形再看命中，别一上来就改 `overflow`。** 第九批用户报「eltooltip 没显示全」，
    直觉是"又被裁了"，但实测浮层矩形**完整**（`243..406 × 462..494`，而侧栏右缘只有 256 ⇒ 冲出 150px 一个像素没少），
    逐层 dump 的 `transform / filter / contain / will-change / backdrop-filter / clip-path` **全是 `none`**
    （`diag-batch5-fail.cjs`）—— `position:fixed` 的包含块是视口，祖先的 `overflow` 根本不参与裁剪。
    真身在绘制层级：`elementFromPoint(浮层中心)` 命中的是正文的 `.el-menu-item-group__title`。
    定罪手法是**只动一个 CSS 属性的翻面变异**（`diag-menu-popper-stack.cjs`：hover → 量 → 仅
    `.demo-aside.style.zIndex='60'` → 重新 hover → 量）：两次矩形逐像素相同，命中从正文变回浮层 ⇒ 动的只有层级
    （`flipped:true`；同一脚本在 `/tabs` 上 `flipped:false`，说明那页本来就没坏 —— **变异要两侧都跑**，
    否则会把"环境恰好没压住"当成机制普适）。成因与修法见 `docs/menu-alignment.md` §10。
16. **Razor 把 C# 印进 DOM：bUnit 抓得住句子，抓不住它挤掉的 171px。** `@for` 体里一旦输出过标记，解析器已经在
    标记上下文，裸 `if (cond) { <div/> }` 会被当成正文整句打印。浏览器实测那句 `if (NeedsValue(condition)) {`
    躺在 `outerHTML` 里，并作为第三个 flex item 把右值框从 `182px` 挤到 **`11.375px`**（面板宽 320px）。
    两条判据设计上的讲究：① 负向断言 `DoesNotContain("NeedsValue")` **必须配正向对照**（同一例里断言运算符读出
    `["开头是","等于"]`、关系读出「且」），否则面板整块没渲染时负向断言也会空转通过；② 尺寸那条只能由浏览器量
    （bUnit 没有布局引擎，第 11 条的老账），所以 `valBoxes[0].w>=60` 写进探针 B1 而不是塞进 bUnit。
    变异 M1（`@if`→`if`）实测 **1 红 / 13 通过**，红在该例 L292。
17. **bUnit 断言"参数没生效"，先核参数类型，别急着改产品**。批次⑦新写的 6 条列宽用例第一版全报 80/104 而不是
    110/134：`ElTableColumn.Width` 声明的是 **`string`**（`ElTableColumn.razor.cs:15`），而
    `TableHeader.Width` 是 `IntString?`（`TableHeader.cs:17`），用 `AddAttribute("Width", (IntString)110)` 塞进去
    会**静默变 null**（不抛、不红，只是参数没落地），列于是走兜底 80。改成 `AddAttribute("Width", "110")` 立刻对。
    ⇒ 写"参数驱动几何/标记"的用例前，先把属性的声明类型读一遍；类型不匹配在 Blazor 里通常是**静默失败**。
18. **几何断言必须先建立"绑定条件"，否则翻面变异咬不到东西**。同一张表在 1440×900 视口下
    `table-layout` 已是 `fixed`，但实测 `tableW=1074` 而 `Σcol=842`，多出的 232px 被 fixed 布局**按比例摊给每一列**
    （`col 134 → th 170`、`col 222 → th 283`），摊完以后把列宽的图标盒加量整段去掉照样撑得开 —— 断言恒绿，
    等于没写（`diag-batch7-colwidth.cjs` 的 AUTO/FIXED/FLIP 三段就是这个空转的取证）。
    判据改成在 800×700 跑，前置一条 `layout=fixed ∧ |tableW − Σcol| <= 4`（实测 `842 == 842`），
    翻面时还得把表宽钉成 Σ 声明值，防止富余再次被摊。
    ⇒ 凡"量宽度/量位置"的断言，先问一句：**这个数值现在真的被什么绑住吗？** 没有就先构造绑定条件，再谈红绿。
19. **量滚条厚度不能用 headless**：headless Chromium 渲染**覆盖式（overlay）滚条**，
    `offsetHeight - clientHeight` 与 `offsetWidth - clientWidth` 恒为 **0**，于是"横条 4px → 8px"这种改动
    在任何断言上都读成 0，看起来像"改动没生效"，实际是量不出来。改 `headless:false` 才读到 `hBar=8`。
    光换模式还不够 —— 必须把这条变成**判据**：探针第 ① 项用一个内容溢出的裸 `div` 断言两轴槽宽非 0，
    读数为 0 就直接宣告"本探针作废"，否则以后哪一天环境又变回覆盖式，整份探针会**静默失明**
    （全绿但什么都没测，比红更坏）。另外 `::-webkit-scrollbar` 只有 `width` 管纵轴、`height` 管横轴，
    拿 `height` 去调竖条厚度是另一条走不通的路。
20. **量 SVG 路径几何之前先停掉它的过渡，否则量到的是动画中间帧。** 批次⑪ 洞边界的断言第一次读出
    `edgeX=1461`（期望 1465）、第二次 `1453`，都不是组件算错：官方 `tour.scss` 里唯一那条过渡就是
    `.el-tour__hollow{transition:all .3s ease}`，而 **`d` 在 Chromium 里是可动画属性**，
    切步 / 换 gap 之后 `path.d` 已经是终值、渲染出来的洞还在路上。
    做法：几何段落里先 `style.transition='none'` 再量，把"官方有这条过渡"单独写成一条独立断言
    （从改动前的快照读 `all/0.3s`），两件事各自成立，不互相污染。
21. **`MutationObserver` 的属性白名单里不能放自己会写的属性。** `position()` 会写 `style` 和 `data-side`，
    把它们放进 `attributeFilter` 就是观察者因自己的写回再触发 —— 加值时看着没事，删掉守卫那刻变死循环。
    同一条理由要求 `writeStyle()` 只在值真的变了才写。批次⑪ 因此白名单只有
    `data-target` / `data-placement` / `data-offset` / `data-gap-offset` / `data-gap-radius` 五项，
    而"Blazor 重渲染把内联 `left/top` 抹掉"这件事改由 `childList` + 按帧跟进兜住。
22. **常驻守卫红了，先确认它抓的是不是自己新加的东西 —— 这次是守卫对、我的探针错。**
    批次⑪ 全量首次跑出 `失败 1 / 通过 2465`，红的是
    `ProbeHygieneGuardTests.Guard_EveryActiveProbeCanOverrideBaseUrl`，点名
    `diag-batch11-i18n-hole.cjs`（我本轮写的一次性诊断探针把 `http://127.0.0.1:5199` 写死在 `page.goto` 里）。
    这条守卫是 H1 规则（连本地站的探针必须能被 `process.argv[2]` / `process.env.*BASE` 覆盖地址），
    判据跑在 `StripComments` 之后，所以"只在注释里提了端口"不算违规 —— 我这轮是真写死了。
    修法按同目录既有惯例（`diag-batch7-colwidth.cjs:6`）补 `const BASE = process.argv[2] || …`
    **并让 `goto` 真的用它**：只声明 `BASE` 不改调用点会让守卫绿着、探针照样换端口跑不动。
    ⇒ 新增任何 `.cjs` 时先跑这一条守卫，别等全量。
23. **在"元素右缘"取滚条条带之前，先证明那个元素真的把滚条画在可见区里。** 批次⑫ 第一版探针照 §3.16 的思路
    在 `.CodeMirror-scroll` 右缘（x 847..848）取 2px 条带，静止"无墨"当场红了 4 条、拖动 `Δ=0`，
    看着像复位没生效。实际是：headful 几何表（`Temp/b12-editor-geom-1.log`）显示 `.CodeMirror-scroll` 右缘在
    **x=850**，而外层 `.CodeMirror{overflow:hidden}` 只到 **x=801** —— 它的原生 4px 条整个被裁掉，
    x 847 上量到的是**隔壁预览栏的列表文字**（`elementsFromPoint` 命中 `UL`，`minL=51` 而不是滚条的 ≈211）。
    用户看得见、拖得动的是 CodeMirror 的替身 `.CodeMirror-vscrollbar`（x 795..800、`z-index:6`）。
    ⇒ 两条常驻做法：① 取条带之前先断言"这条带落在自己栏的 `x` 界内、没被邻栏盖住"；
    ② 红了的读数要和 `minL` 一起看 —— 211 是 `rgba(0,0,0,.16)` 压白底，51 是文字，两者不是同一种东西。
24. **"拖得动"的阈值要按被测对象的真实量程卡，不能一律写死像素数。** 同一份探针让按住预览栏竖条拖 150px、
    要求 `scrollTop` 至少动 40px，结果 `FAIL delta=10` —— 不是拖不动，是那一栏 `scrollHeight-clientHeight`
    总共只有 **11px**，10 已经拖到底了。判据改成两半：量程足够的编辑栏（63px）仍要求"大幅跟上 ≥40"，
    量程不足的预览栏要求"拖到底 `Δ ≥ maxScroll-1`"，并且把 `maxScroll` 与 `before/after` 一起打进 detail
    （数字与日志同源，见 `~/.qoder/memory/bounded-assertions-and-metric-validation.md`）。
25. **全局 `*` 规则挡不住带 class 的第三方滚条规则 —— 复位要用"同选择器 + 排在它后面"。**
    `editormd.min.css` 的 `.editormd ::-webkit-scrollbar{width:7px;height:10px;background:rgba(0,0,0,.1)}`
    特异性 (0,1,x) 高于 `index.css` 的 `*::-webkit-scrollbar` (0,0,x)，全站口径在编辑器上整条被顶掉，
    而且**静止就是灰条常显**（用户截图红箭头那条）。做法是在 `Element.Markdown` 自己的 `editor.css` 里
    用**与 vendor 完全相同的选择器**重写一遍（`index.html` 里它排在 `editormd.min.css` 之后 → 同特异性后来者赢），
    不去改第三方压缩包。⇒ 遇到"全局口径在某处不生效"，先查场上有没有更高特异性的同名规则，
    而不是给全局规则加 `!important`（那会连本库自己的例外一起压掉，比如 §3.14 那两处批准归零的滚道）。
26. **"参数没变所以子组件不该重渲染"这句整条是错的，批次⑭ 在它上面栽了两次。** 先记错的形态：
    把每个缓存页包成 `RenderFragment` 再 `AddContent`，成本是「页数 × 轮数」—— 委托型片段每次父级重渲染都必然
    重新执行一遍。**第一版修法以为换成「引用可比的 `RouteData` + `bool` 参数 + 每页一个子组件」就能让
    Blazor 的变更检测整页跳过 —— 实测否证**：`ComponentBase.SetParametersAsync` 从不比对参数值，
    宿主与页内组件仍旧每轮各跑一次（6 页 × 4 轮 = 各 24 次）。
    ⇒ 唯一真管用的是**子组件自己覆写 `ShouldRender()`**；且闸门上一次的取值必须在 `BuildRenderTree` 里播种，
    因为首帧不走 `ShouldRender`，漏播种就会让第 2 轮白渲染一次（实测 2 而不是 1）。
    三种形态（旧委托 / 只换引用参数 / 带闸门）的计数由常驻用例钉死：
    `test/Element.ComponentTests/KeepAliveShelfRenderFanOutTests.cs` **5 例**，
    变异「把闸门改成恒 `true`」实测 **3 例红**（1×N→4×N、翻面控制项 1→2、规模例 3→12），还原 `touch` 后复绿。
    这条在 bUnit 的常规断言里也看不见（没有布局/渲染扇出），只能靠这种自建两级埋点计数；
    浏览器侧的时长断言只当护栏，不当证据（原因见本节第 27 条）。
27. **浏览器侧的「长任务」计数会被外部因素污染，必须先判计时可不可信再读它。** 批次⑭ 连着三次把环境噪音读成
    页面卡顿：同一段「来回 hover 扫页签条」在健康环境是「一趟 2.8s、rAF 中位帧 8ms、长任务 0 个」，
    在被污染的环境是「一趟 91s、rAF 中位帧 **1008ms**、长任务 8 个全在 986~1012ms」，
    而污染那次 CPU Profiler 600s 自时间里 `(idle)` 占 529s、wasm 只有 ~4s —— **页面没在算，是帧被停摆了**。
    三个候选解释都被受控实验否证：① 残留 Playwright 实例（零残留下照样复现）；
    ② 整机 CPU 被抢（`Temp/b14-load-spawner.py` 起 16 个忙等进程吃掉 38.6/60 CPU 秒，探针照样 8~10ms 中位帧）；
    ③ 诊断脚本自己的 CDP Tracing/Profiler（同一脚本加 `trace`/`notrace` 开关做 A/B，两档都干净）。
    真身**未定位**（要确定性复现得真去锁屏/断会话，属危险操作，没做）。
    ⇒ 可交付的做法是把「可信度」做成判据而不是靠人记得：`verify-demo-tab-mount-stall.cjs` 的 ENV 项在测量窗口内
    逐帧记 rAF 间隔，帧数 <30 或中位 >60ms 就判「环境不可信」，长任务判据不参与结论并整体非 0 退出。
    阈值两侧都是实抓（1008ms vs 8ms，差 100 倍），不是拍的。
28. **`@bind-X` 少了 `XChanged` 参数不会报错，会被基类静默吞掉 —— 批次⑮ 的「半选永远不出现」就是这么来的。**
    演示页写的是 `<ElCheckbox @bind-Indeterminate="indeterminate" …>`，Razor 于是同时发 `Indeterminate` 和
    `IndeterminateChanged` 两个参数；而 `ElCheckbox` 只有前者（EP 的 `indeterminate` 本来就是单向属性），
    `ElementComponentBase` 上挂着 `[Parameter(CaptureUnmatchedValues = true)] Attributes`，
    于是 `IndeterminateChanged` 被当成"额外属性"收进字典，**既不抛异常也不进 DOM**，
    `indeterminate` 字段永远是初始值 ⇒ 那一格从头到尾只是个普通复选框（用户截图里是打勾，不是横线）。
    ⇒ 判法：这类"绑了没反应"先问一句**库里到底有没有那个 `XChanged` 参数**（`typeof(T).GetProperty("IndeterminateChanged")` 一行就否掉），
    别去猜渲染时序。修法是回到 EP 口径（宿主按"已选数 vs 总数"重算，见 `docs/checkbox-alignment.md` §3），
    不是给库加一个 EP 没有的双向参数。陷阱本身由常驻用例
    `ElCheckboxIndeterminateTests.BindIndeterminate_IsNotAParameter_ThisIsWhyTheOldDemoSilentlyBroke` 钉住。
29. **读 CSS transition 的终值，必须等 `getAnimations()` 清空；固定 `sleep` 会量到动画起点，把环境读成缺陷。**
    批次⑮ 探针第一版点完 `全选` 死等 250ms，读到「类名已是 `is-checked`、但勾的 `transform` 还是 `scaleY(0)`」，
    下一跳又读到「类名已空、勾还挂着 `scaleY(1)`」—— 两条一红一红得莫名其妙。真身：EP 的勾是
    `.el-checkbox__inner::after` 上 `transition: transform .15s ease-in .05s`，**类名一换还有 50ms 延迟 + 150ms 动画**，
    而 Blazor 的点击回传正好落在那 250ms 里。`Temp/b15-diag-anim.log` 实测：点击瞬间 6 条 transition 在跑
    （`background-color` / 四条 `border-*-color` / `transform`，`currentTime=9ms`），250ms 后全部清空、`scaleY(1)` 稳定。
    ⇒ 可交付的做法是把"落定"做成判据：轮询到本节每个框 `getAnimations({subtree:true})` 为 0 且类名/伪元素读数连续两次一致
    （`SETTLE`，上限 4s 并在读数里写明"落定(Xms)"还是"超时"）。这是测量卫生，不是拿等待掩盖缺陷 ——
    等待去掉后判据仍会红（M1/M2 两个库侧变异各 4 例红、演示页翻面 9 项红都是真红），
    而"等动画清空"这一步只保证我读的是终值。

30. **弹层（popover / tooltip）的悬停绑定必须用 `mouseenter`/`mouseleave`，不能是 `onmouseover`/`onmouseout`。**
    后者会冒泡：参考元素与面板都在同一个 wrapper span 里，两者之间的**每次过渡**都会各触发一次 show/hide，
    两条带延迟的 `SetVisibleAsync` 互相抢先后顺序 —— 表现是鼠标早就离开了、面板还挂在屏幕上，
    而「移进去悬停根本没反应」是同一根因的另一面（批次⑯ #89②/#93，P6b、TT3 是判据）。
31. **`@onclick` 不能挂在「内含被内联面板的 wrapper」上。** 本库的 popover/tooltip 面板就渲染在 wrapper 内部，
    绑在 wrapper 等于「点面板内部也算重新点击触发器」⇒ 点一下面板里的文字，面板自己关掉。
    EP 的 trigger 事件只绑在 trigger 元素上（`tooltip/src/trigger.mjs`），照这个把 click/contextmenu 下移到
    `.el-popover__reference`。判据必须成对：P4 点外会收 + **P5 点面板内不收**（只写 P4 的话，「点哪都关」也绿）。
32. **EP 的 popover 默认 `trigger` 是 hover，且点外关闭只在 `trigger !== hover` 时生效**
    （`tooltip/src/trigger2.mjs` 的 default、`tooltip/src/content2.mjs:112-121` 的 `onClickOutside`）。
    本库默认写成了 Click，于是"改 hover 绑定"怎么测都不通 —— 归因前先把**默认值与 EP 对齐**再判事件层。
33. **document 级 / DOM 级 JS 助手在 bUnit 里必须 `JSInterop.Mode = JSRuntimeMode.Loose`。**
    点外关闭（`elementPickerOutsideAttach`）绑的是 document 捕获阶段 `mousedown`，bUnit 没有 DOM；
    不加 Loose 就是 14 例 `JSRuntimeUnhandledInvocationException`。这类行为的判据归**浏览器探针**（S4/S4b），
    别在 bUnit 里假造。
34. **探针里的 `requestAnimationFrame` 轮询必须有硬截止。** 无界 rAF 循环会挂死，并且把「脚本自己的等待」
    算进长任务归因里 —— 批次⑯ #95 先前记在树选择头上的 735–756ms 就是这么来的（换成带截止的对照脚本后
    /select 反而更差：973ms/max 759ms vs 331ms/max 279ms）。跨页归因前先跑**同脚本、另一页**的对照。
35. **探针遇到 `null` 要打印状态再判失败，不要直接抛。** run7/run8 的 S5 是 `Cannot read properties of null`
    的 FATAL，整段读数一起丢；改成把 `itemCount`/`hasDropdownIn`/`roots` 塞进 `diag` 输出后，同样的位置要么绿、
    要么给出可比较的数字。「不复现的崩溃」按未解释记录，不能借机把判据调宽。
36. **虚拟表（TableV2）里别用坐标点击 + `hasText` 定位混着测**：`scrollIntoViewIfNeeded` 会滚**表格自己的**虚拟窗口，
    重渲染后同一个坐标已经不是同一行。可交付的做法是在**同一次 `page.evaluate` 里**「读这一行的文字 → `el.click()`」，
    这样「我点了谁」没有歧义；归因再按 `.el-table-v2__expanded-row` **容器自己的文字**读
    （展开内容是行 div 的**兄弟节点**，见 `ElTableV2.razor:131-137`，用 `row.querySelector` 永远读不到 ⇒ 假红）。
    批次⑯ 后复跑时我先踩了「行数变化」当判据（展开不增加 `.el-table-v2__row`）、又踩了「数组 `includes(短名)`」，
    两次都把正确的组件读成了 7~10 个死箭头。
37. **逐个元素的循环必须自带「回到干净状态」这一步，并断言真干净。** 展开类判据里若上一轮的开态没收回，
    下一轮「容器数 1→0」会被读成「点了没收」或「点了别人」。做法：每轮先收起所有 `is-expanded`，
    读到还剩就单独记 `DIRTY` 并跳过该轮（而不是继续判展开），展开/收回各判一次形成闭环。
38. **时长类 A/B 必须同一个窗口内连着跑完，跨窗口的数字一律作废。** /grow 分批建这一批里，
    同一份**改前**代码相隔十几分钟测两次，最长冻结一次是 `1317/2850/2700/2875ms`、一次是 `958/1033/967/1008ms`
    —— 差 **2.8 倍**，比改动本身的效果还大。做法：三臂（改前 / 采用值 / 被否的值）在同一个 9 分钟窗口内依次各跑 4 轮，
    每轮把整机负载（`os.cpus()` 前后各采一次）写进同一份日志；
    并且**把被否的那一臂放到序列的头和尾各测一次**，两次数字一致才能排除"先跑的吃亏"这种位置漂移
    （实测一节一帧臂 runB 4137~4902ms / runC 4525~4824ms，确认差异来自批大小而不是位置）。
39. **仪器要先自证这一轮计时可不可信，而且判据得能挡住"看起来正常"的那一类。** 只写「rAF 中位帧 ≤100ms + `vis=visible`」
    是不够的：有一轮最大帧间隔 **2307594ms（38 分钟）**、中位帧却是 9ms、`vis` 也是 `visible`，
    于是被判成"可信"——那是页面被挂起/节流，不是主线程在算。补上的判据是**可解释性**：
    最大帧间隔若超出同窗口最大长任务 500ms 以上 ⇒ 这段帧间隔没有对应的主线程工作 ⇒ 判「计时不可信」、本轮时长读数不作结论。
    （正常的长任务冻结能通过这条：改前臂最大帧间隔 2850ms 对应最大长任务 2835ms，差 15ms。）
    已经写进日志的错判要**追加更正**而不是悄悄改掉，见 `Temp/grow-after-k5-samewindow.log` 尾部三行。
40. **判据本身也可能量错对象，量出来的数要先过一遍"物理上可能吗"。** 同一批里「20 节正文全建齐」这个读数，
    在"一节一帧"臂量到 `351~575ms`，而那个泵每节 `Task.Delay(60)`、要走 18 次 ⇒ 物理下限 1080ms。
    静态核对过 20 道闸门的位置都对（`<h3>` 是每节唯一不被闸门的元素子节点），所以真身**未定位**；
    处理办法是把这个读数从所有结论里剔除（文档与 CHANGELOG 都不引用它），
    只依赖不经过它的四个量：rAF 帧间隔、`PerformanceObserver` 长任务、落定墙钟、落定 DOM 快照。

41. **「依赖 `:hover` 的像素断言」是探针里最脆的一类，红要先按"是否依赖悬停"分组，别急着判组件。**
    /grow 分批建这批 24 份常驻探针串行批跑，`verify-markdown-scrollbar` 由当天早些时候的 20/20 变 18/20，
    红的恰好只有两条**要悬停才显形**的断言（`ratio=0 / minL=255` 整条纯白），而同一份日志里紧随其后、
    靠**注入静态 vendor 规则**判常显灰条的那条变异对照仍是 `ratio=1` —— 这个"只红在 hover 上"的形状本身就是线索：
    截图通道、条带位置、槽宽都对，唯独"悬停"当时没发生。**但线索不是结论**：三条候选机制做了否证实验全部推翻 ——
    ① 「分批建把编辑器推离旧落点」：实测 `Δy=0`、`ΔscrollY=0`、旧落点仍在栏内、`matches(':hover')=true`；
    ② 「多浏览器并跑争抢」：与另外 3 份 headful 探针 4 路并跑 2 轮，各 20/20；
    ③ 「窗口被遮挡成隐藏页所以不更新 hover」：故意开一个全屏白窗置顶，目标页 `visibilityState` 依然 `visible`、
    `:hover` 依然成立、条带 `ratio` 依然 0.865（诊断件 `Temp/diag-mdhover.cjs`、`Temp/diag-mdhover-occluded.cjs`）。
    同一份 build **串行单跑 20/20**，批跑那一次到此**未复现 ⇒ 真身未定位**，按第 30 条的口径写"未定位"而不是"偶发/环境问题"。
    可复用的两条做法：(i) 像素断言旁边放一条**不依赖 hover** 的同取点对照（该探针本来就有），
    红的时候才能把"绘制通道坏了"和"悬停状态没进来"分开；(ii) 判"组件坏了"之前，先在同一份 build 上单跑一次 ——
    单跑就绿的，账不能记在被测代码上，但也**不许就此结案**。
    **批次㉔ 复测（同一批 24 份、同一顺序、串行重放＋页内证人）：20/20，仍未复现 ⇒ 真身继续挂「未定位」**，做法与证人当场抓到的 rAF 1Hz 现场见第 67 条。

42. **同名 `window.element*` 全局函数 = 后定义静默覆盖前定义，参数多的那一份先死。**
    `dom.js` 里 `window.elementSetScrollTop` 被定义了两次（三参带 `behavior` 的和 ElTreeV2 的两参版），后者赢，
    于是 ElAnchor 的 `Duration` 与 ElBacktop 的 smooth **降级成 `el.scrollTop = top` 的瞬跳**，编译 0 错误、调用点看起来完全正常、
    只有传 `"auto"` 的 ElTable 恰好不受影响所以没人报。守卫写法：`window\.elementSetScrollTop\s*=` 在**去注释后**的
    整份 dom.js 里计数必须为 1，同时断言签名 `(container, top, behavior)` 原文还在，并单独钉 ElTreeV2 用的是新名。
    可复用判据：**加 JS 助手前先 grep `window.<候选名>`**；重名就改名，别指望调用点传的参数会被读进去。
    反向验证（第二轮变异 B1/B3）：把调用点第三参删掉 ⇒ `AnchorScrollToHonorsDurationWithBehaviorArgument` 红；
    把 ElTreeV2 的调用改回旧名 ⇒ `TreeV2CallsTheRenamedHelper` 红（`Temp/b13-anchor-guard-mutation2.log` 恰 4 红 20 绿）。

43. **探针里 `new MouseEvent('click')` 派发的是不可取消事件，量到的是死子树。**
    `dispatchEvent(new MouseEvent('click'))` 的 `cancelable` 默认 `false` ⇒ Blazor 的 `@onclick:preventDefault` 拦不住
    `<a href="#c-1">` 的默认动作，页面整页重载；此后所有矩形都是 `{0,0,0,0}`、`offsetParent === null`，
    看着像"指示条算错了"，其实是量错了对象。三条处置：① 交互一律用真实点击（Playwright `locator.click()`，可信且可取消）；
    ② 点击后的第一批断言先钉**前提**（`offsetParent` 非 null、矩形非零、`location.pathname` 没变），再钉结论；
    ③ 把这个坑写进探针文件头（`Temp/diag-b13-anchor-cursor-dump.cjs` 就是专门留来证明这件事的存档件）。
    同源坑：按属性值找节点不要用选择器插值（`href` 里有 `#` 和任意字符），要 `getAttribute(...) === value`；
    而对"程序滚动之后目标到没到阈值"这类结论，必须先算容器 `scrollHeight - clientHeight` 的上限再判
    （`#c-2` 实测只到 82px 而阈值 75px，是**夹住**而不是没滚到，日志 `Temp/b13-anchor-cursor-active.log`）。

44. **守卫/用例的正向对照也要单点翻面，否则"配了 Setup"等于没测。**
    给 `ElNavigationTests` 补 `JSInterop.SetupVoid("elementAnchorUpdateMarker", _ => true)` 只是让它不炸；
    真正钉住行为的是 `JSInterop.VerifyInvoke("elementAnchorUpdateMarker")`。这条断言有没有牙是**单独验的**：
    只把 `HandleLinkClickAsync` 里那一次 JS 调用注掉（变异 S1）⇒ 该用例红在
    `JSInvokeCountExpectedException : "elementAnchorUpdateMarker" was not called the expected number of times`，
    还原 + `touch` 后 33/33（翻面那份整份落盘在 `Temp/b13-nav-verifyinvoke-flip.log`：失败 1 / 通过 8；
    还原后 `Temp/b13-anchor-guard-clean4.log`：33/33、`MUTATION-TEMP` 残留 0）。同理第一轮 12 处变异里有 4 个绑定点没被任何用例接住
    （调用点 behavior、抑制闸调用、TreeV2 改名、`data-anchor-href`），第二轮专打这 4 处才补齐 ——
    **翻面轮要留"哪些用例没红"的清单**，那份清单就是下一轮变异的靶子。本批 /colorpicker 就是这么发现
    「显式 `Clearable=false` 不该出清空」那条负向断言谁都没咬，补齐成变异 CP6 单点注入 ⇒ 恰它一条红
    （`Temp/b14-cp-mutation-perid.log`）。

45. **「首点没反应 / 点成了别的行为」先量命中元素，别猜事件顺序。**
    /colorpicker 的「清空不起作用」有一条根本不在清空逻辑里：触发器图标被遗留 CSS 拉成 `width/height:100%`（整块色板都是命中区），
    又挂着 `@onclick:stopPropagation`，实测**连续 6 次首点 6/6 都点中图标变成清空、面板压根没开**
    （`Temp/b13-cp-openrate.log`：每行都记了点前的 `document.elementFromPoint(中心)` tag+class 与点后的面板数）。
    判据写法：点**之前**取一次命中元素、点**之后**再取一次，两条都落进日志；只有把"命中元素＝预期的那个"钉住，
    才能把"绑定顺序/事件冒泡错了"和"另一层东西挡在前面"分开。第 43 条的"红先判是不是前提坏了"在这里同样适用 ——
    只看面板开没开，会把它误判成弹层缺陷。

46. **改默认值、删扩展、删 CSS 规则这三类"看起来在收尾"的改动，要在同一批日志里留下"改前长什么样"。**
    本批三处都有改前实测，而不是读代码推断：① `Clearable` 默认 `false`（EP `Temp/ep2140-color-picker.ts:196` 是 `true`）
    的改前现场是**页脚 `清空 buttons found: 0`、`footer: missing`**（`Temp/b13-colorpicker-clear.log`、`Temp/b13-cp-before.log` 整轮 5/12）；
    ② 「按钮大小不一样」用翻面工具把遗留裸 button（CP3）与遗留尺寸规则（CP5）**注回同一份 build** 复现，
    实测 **「清空」h=51 对「确定」h=24**（`Temp/b14-cp-legacy-geometry.log`，整轮 9/12）；
    ③ 回滚到干净构建后复跑 12/12，并核对**服务产物 = 磁盘**（`index.css` 524670 字节逐字节相同，`Temp/served-css-b14.css`）。
    ⚠️ 本条自己差点违约一次：代码注释里原先写的「实测 50px 对 26px」**在任何一份落盘日志里都不存在**（是会话摘要里的数），
    重新量到的是 51/24，注释与文档都已按实测改过并标上日志路径。**写"实测 X"之前先 grep 一遍日志**，找不到就把数重测出来。

47. **删掉一处"不属 EP 的本库扩展"要先问过用户，即便它正是 bug 的载体。**
    悬停把触发器图标换成 ✕ 即可一键清空 —— 它不是 EP 的行为（`Temp/ep2140-color-picker.vue:82-93` 两个图标只有 `v-show`，
    模板里没有任何 `@click`），又恰好是第 45 条那个吃首点的载体，所以本轮直接移除了，**但用户没批准过这个删除**。
    正确做法是当场披露并把选择权交回去（要保留就加一个默认关闭的参数如 `HoverClear` 重新提供），
    已记进 `docs/color-picker-alignment.md` §4 偏差表。同类的"改用户可见契约"还包括默认值反转（`Clearable` `false`→`true`），
    都要落进对齐文档 + CHANGELOG，不许只留在代码注释里。

48. **「首屏是空白」这类负向现象要用 OFF 档浏览器读数坐实，并把「弹层还没挂载」当前提钉住。**
    #112 的表现为「预置了值的下拉什么都不显示」，这种"没有"最容易用一份绿日志糊过去，所以三件事分开做：
    ① **A/B 同一条探针跑两档** —— 把补救函数首行注掉当"改动前"，`/select` 11/12、`/layout` 23/26，
    红的**恰好只有首屏回显那几条**（几何、点选生效、往返可逆两档都绿），这才同时证伪了两个假设
    （"是换控件换坏了" / "修了个不存在的东西"）；日志 `Temp/b15-*-mutant.log` ↔ `Temp/b15-*-restored.log`。
    ② **正向对照要带前提**：每条回显断言都同时钉 `.el-select-dropdown__item` 计数 **0**，
    否则"文本对了"完全可能是"顺手把弹层挂载出来了"——那是另一个代价极大的改动（见 `docs/select-alignment.md` §4）。
    ③ **负向对照要防"看起来有显示"**：另外钉一条"显示的是选项文本、不是裸值 `opt1`"，
    正好接住变异 M2（命中后不取 `Text`、一律回落 `Value.ToString()`）—— 只钉"非空"是接不住它的。
    同源坑：把临时脚本提升为常驻探针时**必须重跑**（原 24 项那份日志作废，本次加了前提与裸值两条 ⇒ 26 项新读数），
    不能拿旧日志的数字当新探针的结果。

49. **等服务器往返的 Task 用截止线，不猜固定预算；负向侧改成「即时状态」。**
    #102 的 bUnit 用例最初把「点取消会不会回结果」写成 `WaitForAssertion(..., 300ms)` / `2s` 这类**猜出来的预算**，
    14 进程满载 + 全量并行时它们自己先打不住 —— 负载全量第二轮实测两条同档红，
    报的文案是「没关」而不是「被拦住」（`Temp/b17-loaded-fullraw.log` RUN2），差点被当成修复本身有问题。
    改法：正向等待一律走 `TestWait.Timeout` 截止线并把**已等毫秒**写进失败文案（`SettleAsync`）；
    负向侧（「空值点确定必须**不**放行」）不写「n ms 内没完成」，改成断言即时状态
    `Assert.False(pending.IsCompleted, …)` —— 被校验拦住是**状态**，不是**时长**。
    配套：负载实验必须先自证负载真的生效（同档用例 0.5s → 单条等待 23~47s 的对比），
    判红因**只看错误文案**，不看「第几轮 / 红了几条」。

50. **「关不掉」类现象在浏览器里会把后续点击全挡回遮罩，探针必须能在 OFF 档跑满。**
    #102 的常驻探针 `demo/tools/probes/verify-feedback-prompt-validator.cjs` 首跑「改动前」档直接 `rc=2 PROBE_ERROR`：
    空值点「取消」关不掉 ⇒ 遮罩还在 ⇒ 之后每个点击都被挡回去（`Temp/b17-feedback-probe-off.log`）。
    这种崩会**吞掉后面所有分叉证据**，等于没做 A/B。改法：`openPrompt()` 见到残留
    `.el-message-box__wrapper` 就**重载页面**再开，OFF 档才能跑满 22 项拿到
    17/22（红的恰是 F10/F11/F12/F13/F17 五条关闭路径，`Temp/b17-feedback-probe-off2.log`）。
    同一轮还踩到一个纯探针侧口径错：`style ? … : null` 把 `style=""` 当 falsy 判成「没有 style」，
    于是「隐藏的错误文案」被误报成红（`Temp/b17-feedback-probe-on.log` 21/22）。
    判"没显示"要按实际内容走正则（`/visibility:\s*hidden/`），别拿属性串真假当结论。

51. **`JSInterop.SetupVoid(...)` 后面必须 `.SetVoidResult()`，否则 bUnit 的 ~1s 看门狗会把真实断言整个盖掉。**
    负载全量里 `ElRadioTests` 两条（以及此前被记成"偶发"的 `ElViewerGrowTests.LoadingGate_MinDuration_*`）
    报的都是 `JSRuntimeInvocationNotSetException : … invocation handler was not configured to return a result`。
    真身不在库：`ElRadioGroup.TrySetValueAsync` 先赋好值、**之后**才 `await target.FocusAsync()`
    （`src/Components/ElRadioGroup.razor.cs:198-201`、`:214-217`），值早就对了，
    但那行注册写成了 `JSInterop.SetupVoid("execFocus", _ => true);` —— 没有 `SetVoidResult()`，
    这次 interop 永远悬着；bUnit ~1s 后把它置成异常并记为未处理渲染异常，
    在**下一次派发/等待**里重新抛出，把「断言其实已经成立」盖成红。
    无负载时整条用例 0.5s 内跑完，看门狗赶不上 ⇒ 一直绿；满载跨过 1s ⇒ 偶发红。
    确定性复现（**不需要负载**，临时诊断类 `TempDiagExecFocus`，日志 `Temp/b17-diag-execfocus.log` ↔
    `Temp/b17-diag-execfocus-fixed.log`）：把断言窗口拉长到 6s，
    悬空档 3/3 在 **1007 / 1018 / 1020ms** 抛 `JSRuntimeInvocationNotSetException`；
    补 `.SetVoidResult()` 档 3/3 在 **6006 / 6014 / 6010ms** 抛真实的 `WaitForFailedException`。
    修的是测试脚手架：全站 14 处缺 `SetVoidResult()` 的注册一次补齐（8 个文件，改完全站 `SetupVoid` 30 处、悬空 0 处），
    仓内另有 16 处本来就是正确写法（`ElNavigationTests.cs:58-59`、`ElUploadTests.cs:124`、`ElInputNumberTests.cs:17` 等），
    这次是**向已存在的多数口径收敛**。
    判别口诀：**同一个类里两条用例轮换红 + 错误文案是 JSInterop 而不是断言本身 + 无负载永远绿**，
    先查 `SetupVoid` 有没有给结果，别急着怀疑被测组件。

52. **自己写的"等待门"（裸 `InvokeAsync` 泵）不会触发 bUnit 的未处理渲染异常检查，所以超时消息必须自己去翻它。**
    第 51 条那个看门狗之所以能被 `WaitForAssertion` 顺带翻出来，是因为 bUnit 的等待/事件分派路径会调
    `BunitRenderer.AssertNoUnhandledExceptions()`。换成自己写的 `while (未截止) { await cut.InvokeAsync(() => { }); … }`
    就**没人调它**了：渲染批里真抛了异常，表现只会是"等满 20s、DOM 没变"，红得完全不可归因。
    `ElFeedbackTests.PeekRendererFault()` 就是补这一项：反射调（bUnit 2.7.2 里 `Renderer` 属性是公开的，
    `AssertNoUnhandledExceptions` 不是 ⇒ 直接写会 CS1061），只在超时路径跑，取不到只降级成一句说明。
    它已经把一次红从"弹层没关"变成了可读的三分叉 —— `泵了 447 轮` 否掉"我的门被饿死"、
    `host.DialogOptions=0 / service.Dialogs=0` 否掉"库的关闭链没走完"、
    `渲染器：没有未处理异常` 否掉"渲染报错被吞"（`Temp/b17-final-loadhunt2.log` 两条同型红：447 轮 / 532 轮）。
    配套要求：**超时消息要带上"模型侧计数 + DOM 侧计数"两份现场**，只报"没关掉"下次还得重查一遍。

53. **负载实验的并发数要在开跑前实测并写进日志抬头，不能引用自己启动时写的那个数字。**
    本批踩过：`Temp/b17-hunt4.log` 标着"24 spinner"，实测是 **38 个忙循环 / 12 逻辑核 = 3.2 倍超额订阅** ——
    上一批 14 个的 spawner 存活时长（`DUR=1500s`）还没到期就叠加了新一批。
    后果不是"数字不好看"，而是那一轮抓到的 3 条红**只能在 3.2 倍超额订阅下引用**，
    不能当常规机器上的偶发率（同一批用例在实测 14 个的那轮里红的是另外几条）。
    做法：每轮前测一次"3 秒内 CPU 秒涨了 >1s 的 python 进程数"，把它写进该轮的日志抬头；
    实验结束按**记档 pid 列表**逐个 `taskkill`（不要 `//IM` 一把梭，会连无关进程一起杀），并复测为 0 再收尾。
    驱动脚本若用 `>>` 追加日志，中途手写 caveat 会落在文件中部 —— 带时间戳即可，那不是覆盖证据。

54. **指认缺陷的"签名"要先量它的出现率：全绿用例里也出现的形状没有区分力。**
    第 52 条那套现场把可能压到"那一批渲染没派发"之后，我据插桩 trace 写过一条签名：
    「正常关闭会在 `CH Remove` 与 `CH Remove SHC-done` 之间夹一次 `AfterRender`，缺失即本次缺陷」。
    把整份 trace 按宿主分组重算后发现：**32 次移除里 16 次移除之后整场再没有该宿主的任何事件**（`Temp/b17-instr2-rel.log`，
    40 个宿主 / 24 条线程 / 跨 243s；分组结果 `Temp/b17-instr2-seq.log`、逐条落点分类 `Temp/b17-instr2-gap2.log`），
    而这 16 个宿主所属用例**全绿** —— 它们的共同形状是"用例到移除就结束、同线程下一条事件 0.18~145s 后才来且属于别的宿主"，
    即没人等这次渲染的正常收尾。**50% 出现率的形状不能用来点名一次偶发红**，那句签名已作废。
    两条配套要求：① 提出签名前先量它在通过用例里的出现率（同一份 trace 就够，别只看一两个样本）；
    ② 插桩打的 id 要**同时**出现在超时消息里（本仓做法：`CloseScene` 现在带 `hostId=`，与库侧
    `RuntimeHelpers.GetHashCode` 同源），否则 trace 与红永远接不上、白跑一轮负载。
    同理，负载猎取里如果**不相关的计时用例也在红**（本批同档负载下 `ElInputNumber` 按住连发、`ElButton` 防抖合并各红过一次），
    那就是环境结论的一部分，必须一起写进表里，不能只挑自己在追的那条。

55. **临时加进超时现场的每条读数，都要先在成功路径上验它「真的会变」**，否则它会给出一个自洽但错误的结论。
    本批连踩两条（都由同一场景的正向对照当场抓住，对照件是临时探针 `test/Element.ComponentTests/PopupCloseThreadProbeTests.cs`，
    **收尾时已删除**，读数原文留档 `Temp/b17-threadprobe.log`、对照全绿 `Temp/b17-controls-refix-150820.log`（14/14））：
    ① 我按记忆写了「从残留元素沿 `INode.Parent` 走到树根要求它是 `IDocument`，再用全文档口径重数一遍就能分
    『bUnit 节点表陈旧』与『DOM 里真有』」。实测这条链是 `HtmlDivElement < HtmlBodyElement` 就断了（body 的 `Parent` 是 null），
    运行型式上也没有 `Owner`/`Document`/`Context`/`ParentNode` 任何一个 ⇒ 这条尺子根本不存在，它给出的 `doc.wrapper=-1` 会被读成
    「文档里数不到 ⇒ 红在尺子上」，方向整个反了。
    ② 换成「被摘掉的那个元素对象 `Parent` 变 null」，绿路实测关闭后它的 `Parent` **仍是 `HtmlBodyElement`**
    ⇒ AngleSharp 不随移除清 `Parent`，这条读数没有判别力。若直接用在红上，"父节点还在"会被当成"DOM 里真挂着这一层"，
    而其实它一直挂着。
    做法：读数分三类各自要求不同 —— **计数类**要绿路两端各有值（开=1、关=0）；**形状类**要绿路有那个形状且消失过
    （`popup.Nodes` 关干净后整个变空，所以红时仍是开层那 5 个节点才等于"DOM 真挂着"，而不是"尺子没刷新"）；
    **实例类**要先证同一个节点两次查询给同一对象（`同一实例=True`），否则"残留是不是开层那一份"无从判。
    这些对照写成 `[Fact]` 让它常驻红绿，别只在脑子里过一遍。

56. **判别"这台渲染机还活着吗"要给一个必然要求新增编辑的输入，而不是只观察它不动。**
    弹层 removal 红里「渲染跑完但 DOM 不动」有两种完全相反的修法（只有 Remove 那一批没落 vs 整个渲染管道停了），
    光看残留分不开。做法是在断言已经失败之后（只跑 2 秒预算、不改判定）**再开一层**：`dom.wrapper` 从 1 变 2 ⇒ 管道还活着；
    仍是 1 ⇒ 这个宿主的渲染不再落到 DOM。前提同样是正向对照：绿路上第一层开着时再开一层必须能到 2
    （实测 `第一层=1 成立 第二层出现=True dom.wrapper=2`），否则"加之后=1"就说不清是管道停了还是这条实验本身看不见第二层。

57. **猎取脚本不要复用固定日志名再 `: > "$LOG"`**：重跑一次就把上一轮唯一那份红现场截掉了。
    本批真的丢过一次证据（`Temp/b17-idle-hunt.log` 被第二次猎取清空，只能靠事先 `cp` 的 `Temp/b17-idle-hunt2.log` 才保住那条红）。
    做法：日志名带时间戳（`LOG="Temp/b17-idle-hunt-$(date +%m%d-%H%M%S).log"`），旧文件永不原地覆盖；
    确实要复用固定名时，先 `cp` 一份带时间戳的再动。

58. **浏览器探针复跑前，必须核对 WASM 载荷真是从当前源码构建出来的**，否则 22/22 证明的是旧二进制。
    本批实测：`src/Components/ElPopup.razor.cs` 撤桩在 13:09，而 `demo/…/wwwroot/_framework/Element.<hash>.wasm` 的 mtime
    还停在 09:47；中间 15:12 那次 `dotnet build demo/…`（增量）**报告"已成功生成"却没动载荷** ⇒ "build 成功"不等于"探针跑的是新代码"。
    做法：`--no-incremental` 重建后比对两件事 —— 载荷 mtime 必须晚于最后一次 `src` 改动；
    载荷文件名的内容哈希没变，就说明源码语义确实没变（本批正是这种情形：撤桩后内容与 09:47 那份同代），
    这两条要一起写进探针日志抬头，别让读者去猜。

59. **只采一侧（DOM）的读数不能用来在「渲染没落地」与「宿主根本没收到这次变化」之间做结论。**
    第 56 条那个"再开一层"实验只数 `dom.wrapper`，于是「加之后=1」被我写成"这个宿主的渲染整体不再落到 DOM"（`Temp/b17-idle-hunt-1011-151805.log`
    里那次红的原文就是这个两分岔口径，**它已被本条推翻，引用时别当结论读**）。但 DOM 不动至少有两种完全相反的因：
    ② 宿主 `DialogOptions` 涨了、渲染跑了、编辑没落地；③ 宿主压根没收到这层（它订阅的队列不是服务上那一份，属
    `ElPopup.razor.cs:190-222` 那条换队列路径）—— ③根本不是渲染问题，修法在订阅侧。做法：同一句读数同时打**模型侧**
    （`host.DialogOptions=前→后`）与**队列侧**（`service.Dialogs=` + 与库侧同源的 `queueId=`），三分岔各自给结论；
    并且新分岔的每一条都要按第 55 条先验绿路（健康宿主再开一层必须落到"第一分支（渲得出来）"，
    实测 `host.DialogOptions=1→2 dom.wrapper=1→2`，`Temp/b17-treeprobe.log`、运行 `Temp/b17-treeprobe-run-152605.log` 第二段 3/3）。
    同类形状：**"某个元素还在不在宿主子树里"这种成员判定也要防尺子本身**（`FindAll` 与 `Nodes` 可能不是同一份），
    所以 `CloseScene` 现在同时打 `残留在本宿主Nodes里=` / `开层样本在本宿主Nodes里=`，且对照里专门验过
    **第二个宿主渲出的同类元素对第一个宿主判「否」**（不恒真才算有判别力）。

60. **「换没换 / 在不在」这类身份读数，必须先在"健康宿主做一件完全正常的事"上取一次绿值**；否则普通重渲会被写成"被摘掉又重建"。
    本批第 7 条我曾据 `残留是否开层那一份=同一实例=否` + `开层样本在本宿主Nodes里=否` 断言"开层那份被摘过、又被谁按非空模型重建回来"。
    **实测推翻**：在**没有任何毛病**的宿主上再做一次最普通的事（模型里加第二层，绿路 2/2，运行 `Temp/b17-frameprobe-run7.log`、
    读数原文 `Temp/b17-frameprobe.log` 第 15 行）—— `实验后第一份与实验前同一实例=否（换了实例）`、
    `实验前那份现在在Nodes里=不在（这份已被摘掉）`，而这时 DOM 与模型两侧都完全正确（`1→2` 层、两层 label 都在）。
    原因就是第 59 条那条分支渲染是**按下标复用、message-box 分支没有 `@key`**（`ElPopup.razor:240`），
    所以"对象换了 / 旧对象不在树里"是**任何一次动到该位置的渲染**的普通签名，不是 removal 的证据。做法：
    凡是形如「是不是同一份」「还在不在子树里」的读数，要配一条**预期它就会翻面**的对照（健康宿主 + 一次正常改动）；
    对照里也翻面的，只能当旁证留档、**不进判定**（`PipelineAliveSceneAsync` 里这两条现在明确标注只作旁证，
    判定改用"个数 + `aria-label`"）。写日志时把绿路的这两条原文一起留下，否则下一轮又会被同一个形状骗一次。

61. **反射读一个池化/可增长缓冲区时，数组长度是"容量"不是"在用的格数"** —— 拿容量当计数会把上一轮留下的残帧一起数进来。
    本批的帧尺子第一版打的是 `已用格=128/128`，加了同持有时上的 `_itemsInUse` 之后是 `已用格=72/128`（同一层、同一次构建，
    `Temp/b17-frameprobe.log` 第 10 行 vs 第 13 行）—— 少读的这 56 格全是**上一轮渲染的残帧**，靠"class 里有没有 wrapper"这种
    存在性判据会被它们骗出假阳性。做法：找与那份数组**同前缀的 int 计数字段**（实测 `_items` 配 `_itemsInUse`；
    按 `count`/`length`/`used` 兜底时注意 `InUse` 不含 `used` 这个子串，配对要按数组字段名走），
    并把 `已用/容量` **两个数一起打出来**；计数取到 0 是合法读数（关掉之后在用的就是 0），
    所以判"没读到计数"要用 `< 0` 而不是 `<= 0`（写成 `> 0 ? 截 : 用容量` 就把 0 格当成整池残帧了）。
    **但 `已用/容量` 还消不了"读到的是归还后的空壳"这一支**（池化数组归还后容量也是 0，`0/0` 与"活表真的空"同形）。
    消法是**再配一条不经反射的公开计数器**并**在绿路的两个状态各取一次**：本批加的是 `popup.RenderCount`
    与数组持有者上的归还标志/最小容量，绿路实测（`Temp/b17-frameprobe2.log`，运行 `Temp/b17-frameprobe2-run2.log` `Passed: 1, Total: 1`）
    开一层 `池已归还=否 池最小容量=32 已用格=72/128 RenderCount=2`、关干净
    `池已归还=否 池最小容量=32 已用格=0/0 RenderCount=6` ⇒ 归还标志两态恒"否"、最小容量恒 32，
    所以那条 `已用格=0` 是**活表报的 0**而不是空壳。口径：反射读来的"0"在被两条独立读数（公开计数器 + 池状态标志）
    于**同一状态**上印证之前，只能写成"与关干净同形"，不能写成"树真的空"。

62. **反射扫描「顺着对象图往下走」的读数要做窄链路 + 各自包 try**，一次深走能把整行主读数吃掉。
    本批给帧尺子加「有没有第二份帧表还挂着这一层」那一节时，第一版直接把 `CollectRenderFrames` 的深度从 3 提到 6 做整图深走，
    结果绿路**开层那一面正常出数、关干净那一面整行退化成** `｜帧：没读成（ElementRemovedFromDomException，该项不作结论）`
    （失败原文 `Temp/b17-frameprobe3-failmsg.txt`，抽取 `Temp/b17-frameprobe3-deep.txt` 第 3-4 行）——
    被丢的不只是新读数，**连主读数（`池已归还` / `已用格` / `帧里的wrapper`）一起没了**，
    而这条线常驻在超时现场里，红来的时候就会以为"尺子没跑成"。做法两条：
    ① **按已知链路走窄的路**（这里改成沿 `ComponentState.ParentComponentState` 一跳一跳走、每人只给 depth 3），
    不要为了"多找几份表"把整张对象图走深 —— 深处一定有 DOM/角标器之类的对象，碰它就抛；
    ② **每一节单独 try**，副读数失败只让它自己写成「该项不作结论」。
    配套：新扫描节要在绿路的**两个状态**都断言它真打出了预期的**份数**（这里断言 `状态^0.CurrentRenderTree` 与
    `状态^0._nextRenderTree` 两份都在，且 `DoesNotContain("整轮扫描没跑成")`）——
    只断言"这一节存在"不够，空列表也会通过"存在"。

63. **拿一条读数当"红与绿的分界"之前，基准必须是同一条操作路径** —— 不同关闭入口的 `RenderCount` 只是数值巧合。
    本批第 13 次猎取的红（点 ×）打出 `RenderCount=6 已用格=0/0`，而我手上那件绿路对照（`FrameProbe2…`）走的是**点取消**，
    两边都是 6 —— 这**不构成**"红与绿同形"的证据，因为两次操作的渲染次数本来就可能各自不同（同第 60 条的判别力要求）。
    补了同操作对照（`FrameProbe3HeaderCloseGreenBaselineRenderCountAndFrames`，验完已删，
    `Temp/b17-frameprobe3-run5/6/7.log` 各 `Passed: 1, Total: 1`）才敢说：点 × 的绿路也是 `RenderCount=2 → 6`、
    `已用格=72/128 → 0/0`。反向也记档：本批红有两族（`Prompt…Cancel…`＝点取消、`PromptHeaderClose…`＝点 ×），
    引用某条绿的数字时先看清那条红走的是哪个入口；跨入口借用数字（本批的父链那两格 `1/32`、`2/32`）只能标成**旁证**。

64. **几何判据的阈值必须比"要抓的缺陷量级"更细，而且要用变异实验证明这条判据真能翻面。**
    批次⑱ 第一版 `verify-timepicker-b18b.cjs` 用 `|Δ| ≤ 1px`，而它要抓的正是"整列比 EP 高 1px"这种系统性偏差 ——
    把 M3 变异（`dom.js` 的 `center()` 从选中带中心换回列中心）注进去，实测读数确实变了（`Δ 0 → -1`、
    `scrollTop 256 → 257`），但 `≤1` 把它当通过，**42/42 全绿**（`Temp/b18-mutation-M3-container-centre.log`）。
    收到 `<1px` 后同一变异立刻 30 通过 / 12 失败（`Temp/b18-mutation-M3-tighter-threshold.log`）。
    亚像素取整的抖动远小于 1px（读数本身只到 0.1px），所以 `<1` 不误伤绿路。
    推论：**"变异跑了没红"有两种可能 —— 代码没问题，或尺子没牙**；必须再看那条变异有没有改变原始读数
    （本批 `Δ` 与 `scrollTop` 两栏都变了 ⇒ 是尺子没牙，不是代码没问题）。这条与第 60 条配套：负向断言要配正向对照，
    阈值本身也要配"翻面对照"。

65. **量"落点是否到位"必须分清视觉尺寸与布局尺寸**：`getBoundingClientRect()` 受 `transform` 影响，
    `offsetHeight / clientHeight / scrollTop` 不受。弹层带 `scaleY` 展开动画时，用 rect 算出来的 `|delta| < 1`
    当提前收手的条件，会把动画中途的**残差焊死** —— 因为逐帧收敛的循环一旦数够"连续两帧不用再动"就停表，
    动画幅度一停真实 Δ 才浮现，已没人再纠正。批次⑱ 实测：目标已经是选中带中心、但还没有"动画已停"这道门时，
    连开 5 次时间面板，**4 次**停在 1~2px 残差上（`scrollTop` 382/383/383/383 对 EP 384，`Temp/b18-settle-series.log`），
    加一道 `|rect.height − offsetHeight| > 0.5 → 这一帧算 pending` 的门之后 **5/5 精确 384**
    （`Temp/b18-settle-series-after-animfix.log`），且纠正发生在动画停的那一刻（112→219ms 一次到位，没有"先错位再跳回"）。
    通用形式：**任何"逐帧收敛到几何目标"的循环，都要有一个"几何还在动"的判据**，别只靠"这一帧不用再动"。

66. **做 CSS 几何变异前先问"这个尺寸是谁给的"**：inline style 压过任何非 `!important` 的外链规则，
    而本库时间滚轮三列的 `max-height:190px / flex:1 1 0 / overflow:auto` 是 `TimeSpinnerRenderer.cs:152`
    以 **inline style** 写的。第一次 M2 变异（`index.css` 末尾追加 `.el-time-spinner__wrapper:first-child{max-height:150px}`）
    跑出来 **42/42 全绿**，看着像"等高判据没牙"，其实是**变异根本没生效**：
    浏览器读回 `inlineStyle="position:relative;flex:1 1 0;max-height:190px;overflow:auto;"`、
    CSSOM 里"命中该元素且写 max-height 的规则"列表为空，只有加 `!important` 才复现出现场
    （`Temp/b18-mutation-M2-hour-150px-v3.log`：`heights=150/190/190`）。
    所以变异实验的第一步是**证明变异到了浏览器**（读 `inlineStyle` + 遍历 `document.styleSheets` 找命中规则），
    而不是直接拿"没红"当结论。附带一条：改 `index.css` 做临时变异要用 shell 追加/`cp` 还原，
    别用 `io.open(p,"w")` 整份重写 —— 它会把全文件转成 CRLF，md5 立刻对不上 HEAD（本批踩过并已还原验证）。

67. **复测一条「只有批跑才红」的偶发：同一批、同一顺序、同一 build 串行重放，证人挂在判据同一次取样上。**
    批次㉔ 对 #100 的做法（`Temp/b24-batch-replay.sh`）：按归档 `Temp/grow-probes-k5/_summary.log` 的同一批 24 份、同一顺序串行重放，
    只把 `verify-markdown-scrollbar` 换成页内证人副本 `Temp/b24-md-scroll-witness.cjs` —— **判据一字未改**（仍是「悬停 700ms 后取条带像素，`ratio>0.3` 才算显形」），
    只在同一次悬停后多开三枪补取样，并同场报 `matches(':hover')`、命中链、条带/栏几何相对取点时刻的漂移、帧中位、长任务、`visibilityState`。
    本轮 **20/20**（`Temp/b24-batch/verify-markdown-scrollbar.log`）：两条 hover 判据现场 `hover=true`、ratio 0.8648/0.9811，
    同份日志里不依赖 hover 的静态变异对照（注入 vendor 规则 → `ratio=1 / minL=160`）也成立 ⇒ **未复现，真身仍未定位**，按第 41 条口径不写「偶发/环境问题」，也不许就此结案。
    另：`_summary.log` 里「自报」为空**不等于那份没跑**——各家汇总行格式不同（`TOTAL=14 FAIL=0`、`TOTAL 12/12`、`===== 34/34 通过 =====`、`RESULT: 9 PASS, 0 FAIL`），
    本轮抽查 5 份 rc=0 的日志都数到真 PASS 行（`verify-batch5` 14 项、`verify-markdown-editor` 12、`verify-pickers` 34、`verify-viewer-ux` 20、`verify-demo-tab-mount-stall` 9），
    按第 40 条口径，rc 之外仍要自己数一遍 PASS/FAIL。
    证人这一轮第一次抓到那个降频**现场**：预览栏那一臂 `rafMed=1000ms`、最长帧 1008ms、长任务 `0/0ms`、`vis=visible`、`hoverHover=true`，
    而同一次运行的编辑栏臂还是 `rafMed=8.4ms`（`mutsAfterCap` 两臂都还在涨 ⇒ 页面活着，只是帧被掐到 1Hz）。
    两点可直接复用的结论：① **像素/悬停类判据不受 rAF 降频影响**（同臂四枪 ratio 一字不差 `0.981081081081081`），
    降频只会污染**时长**读数 —— 正是 §3.23「最大帧间隔远超长任务 ⇒ 本轮时长不作结论」那道门的又一个实例；
    ② 这一臂的浏览器只带 `--no-sandbox`、**没带**反遮挡/反后台节流旗，
    而带全套旗的 `Temp/b24-treeselect-latency.cjs` 也红过同样的 1008ms ⇒ **旗子不是分水岭，触发条件仍未定位**（§3.24 第 4 行已按这条改口）。
    ③ 为造出这个状态做的三种操控（另开 Edge 窗口置顶、Win32 `SW_MINIMIZE`、置顶无边框窗整幅压住）**全部没过操控检查**，
    细节与否证条件记在 §3.24 第 4 行；此处只留一句：那两份日志的整份结果都是 **20/20 PASS**，
    所以 1Hz 与 #100 那两条 hover 红**没有因果关系**，别再把它当偶发红的原因去追。

68. **跑批时那条「一直红但没人记过档」的红，要在本轮点名并给出同探针内的对照，别让它继续隐身。**
    `verify-gap-margin-equivalence` 在归档批跑与本轮批跑都是 rc=1，唯一的 `DIFF` 项 `dtg-tags`
    两处读数**逐字一致**（`diff` 两份日志第 100~110 行为空）⇒ 这是**确定性**红，不是 #100 那种偶发：
    `host {"x":694,"y":534.5,"w":358,"h":24} -> {"h":28}`、`span.el-tag [694/… ml=0px] -> [696/… ml=2px]`。
    同探针里 `dtg-tag-inner` 用的替换式是 `.el-select__tags>.el-tag>*+*{margin-left:4px}` 判 **EQUAL**，
    而 `dtg-tags` 用的是 `.el-select__tags>*{margin:2px}` —— 差别正好解释现场：`margin` 会给了**首个子项**也补 2px 前导、
    纵向 2px×2 把容器从 24 撑到 28，而 `gap` 两条都不产生 ⇒ 指向**替换 CSS 的选择器写法**，不是库缺陷。
    此前 `grep -rn "dtg-tags" docs/*.md CHANGELOG.md` **0 命中**，即这条红从来没被记进任何文档，本轮第一次点名。
    **本批没动它**（改探针替换式还是改库口径属另行决定），只记档；可复用做法：跑批收尾时要把**全部**非 0 rc 逐条列出来并给同批内对照，
    不能只盯自己那一颗钉子。

69. **侧栏导航条目不是 `a` 元素：按 `href` 找入口会假红，而且是两种假红。**
    批次㉕ 首跑 `/statistic` 探针 28/30，S1「侧栏有 /statistic 入口」用了 `a[href]` 结尾匹配 —— 命中的是页面正文里那条
    **官方文档外链**（`https://element-plus.org/zh-CN/component/statistic`，同样以 `/statistic` 结尾），拿到 `icon=""` 而红；
    改成精确 `a[href="/statistic"]` 之后直接 `null` 又红一次。真身是侧栏条目渲染为 `li.el-menu-item`（`SidebarMenu.razor:20` 的
    `ElMenuItem Route=…`，点击走路由、**不渲染 `a`**），整页 DOM 里根本没有 `/statistic` 这个 href（`Temp/b25-nav-dump.log` 现场：
    `li.el-menu-item.is-active` 内含 `i.el-icon-data-line`）。可复用做法：写「入口可达 / 菜单项存在」类判据前，
    先跑一次 DOM dump 看清条目到底是什么元素，别按 HTML 习惯猜；能验 `is-active` 的地方就顺手验，它同时证明「这条目对应当前页」。
70. **全角与半角字符会让「值对、断言错」**：同一次首跑另一条红是 P7，期望 `'¥'`（U+00A5 半角），现场是 `'￥'`（U+FFE5 全角），
    红字打印成 `pre:"￥"` 对 `'¥'`，肉眼几乎分不出。判据本身没错，错在期望值是从记忆里敲的而不是从源码/页面复制的。
    可复用做法：所有相等类判据都把实际值原样打进 detail（本探针每条 `check` 都带 `JSON.stringify(...)`，红了直接读码位），
    写死字符的期望一律从 `*.razor` 或页面文本复制；这类红**先怀疑尺子，再怀疑现场**。
71. **CSS 变量口径的修复只有「外部覆盖」那一侧的判据能证明；默认读数那几条对它完全没牙。**
    CSS-1 变异（把 `.el-statistic__content` 的 `font-size` 从 `var(--el-statistic-content-font-size)` 改回写死 `20px`）之后，
    30 项里**只有 K1 红**：S4（基本段 20px）与 K2（正向对照基本段仍 20px）都绿，因为写死值恰好等于默认值，两条判据在同一前提下分不开。
    现场能分辨差异的只有 K1 那一对同时报出的 `largeVar="28px"` 与 `largeSize="20px"`（覆盖声明在、读不到）。
    可复用做法：凡是把写死值换成 CSS 变量的对齐，守卫里必须配一条「在外部把该变量覆盖成**与默认值不同**的值，再看消费点」的判据，
    并把「变量声明值」与「计算后字号」两样一起报出来；只报默认读数的守卫等于没守。
72. **一张 `position:fixed` 的全屏透明遮罩会重写滚轮的滚动链 —— 「弹层外面也滚不动」是它的形状，不是组件的逻辑。**
    批次㉖ #130：/datepicker 打开后指针移出面板，滚轮整页一动不动。取证的钥匙是**同一个坐标、同一个滚动容器，只把弹层开关切换一次**：
    关着时 `main 0->300`（原生链：target → 最近可滚祖先 `.demo-main` → …），开着时 `main 0->0`。
    差别只有一个 `elementFromPoint` 命中的 `.v-modal.el-select-dropdown__outside`（`position:fixed`、四边 `inset:0`、`opacity:0`、
    自身不可滚、祖先链 0 个可滚容器）—— 原生滚动链从命中元素往上找可滚祖先，被这张整屏遮罩拐到视口，而本演示站视口本身不可滚
    （滚的是内层 `.demo-main`），链子当场断掉。官方 EP 没有这一层（它在 document 上挂 `ClickOutside` 指令），所以官方没这个症状。
    可复用做法：遇到「鼠标在某处滚不动」先跑 `elementsFromPoint` 报出命中栈 + 逐级 `overflowY/X ∈ {auto,scroll}` 且边界可滚的证人，
    再决定是组件问题还是**多出来的那一层**。判据的牙要落在「谁滚的」（见第 73 条的 `prevented`），否则翻面与修复两版都可能绿。
73. **滚动类判据的三个读数口径必须先自证：Chromium 把 `mouse.wheel` 的 deltaY 放大 1.25 倍；`prevented` 才是「谁滚的」证人；「滚到底」要带 ±1px。**
    ① `page.mouse.wheel(0,300)` 在 Chromium 只发**一个**事件而 `deltaY=374.9999944120646`（`100→124.99…`、`−200→−249.99…`）——
    首跑把期望写成 300 就红了，实际是尺子的换算。期望值要么读事件里的 `e.deltaY` 再比 `ΔscrollTop`（容差 ≤1.5px），要么按 ×1.25 换算，
    不能按传入值硬算。② 光看「页面滚了没有」分不清是浏览器原生滚的还是我们的转发器滚的：同一次派发里既报 `Δ` 也报
    `e.preventDefault()` 是否被调用（捕获阶段读 `defaultPrevented`）。修复后 `prevented=true` 且 `Δ == e.deltaY`（转发器在场），
    翻面后 `prevented=false` 且 `Δ=0`（两边都没滚）——这一对才是牙；边界对照 D5 与反向对照 E1/D1 用的都是 `prevented=false`，
    三条一起才排除「顺手把所有滚轮都抢过来」。③ `scrollHeight - clientHeight` 与 `scrollTop` 上限可差 1px（现场 `main=986 max=987`），
    「到底不再转发」类断言必须 ±1。
74. **红字自相矛盾时先查尺子本身有没有被编辑工具拼坏：一次 Edit 把代码并进了上一行 `//` 注释，判据就悄悄复用了上一条的结果对象。**
    批次㉖ 守卫跑出过一组物理上不可能的数（`before=0 after=375` 而该条 `dy=+374.99` 是**反向**滚轮）。
    处理方式不是放宽断言：先写两支最小诊断（`Temp/b26-b2-diag.cjs`、`b26-b2-diag2.cjs`）在同一序列里量产品行为，证明产品在该点行为正确，
    再回头读探针源码 —— 找到 B2 的 `r = await wheelAt(...)` 被吞进了上一行注释，于是 B2 打印的是 B1 的结果。
    可复用做法：① 每条判据的现场值必须来自**本条自己**的一次派发，断言里把坐标、`dy`、`before/after` 三样一起打出来，
    值与不上就当场暴露；② 改完探针**通读一遍改动邻行**（注释行尾有没有跟着代码是 Edit 类替换的高危形状）；
    ③ 负向/反向滚轮的期望符号要在纸上先算一遍（`d2 - dy` 对 `d2 + dy`，两边同负时差别一眼看不出来）。
75. **一次性的「0 个外链请求」判据要先取快照，再让探针自己动手：探针为了自证而发的那次 fetch 会污染同一份清单。**
    批次㉗ 首跑 B2「恰好 1 个请求打向站内 `hls.min.js`」数到 **2**，多出来的一条是前提证人 A3 自己的 `fetch('/js/vendor/hls.min.js')`；
    同形的问题会落进 B1（0 个 CDN 请求）。修法不是把 B2 放宽成「≥1」，而是在页面加载完成那一刻把 `reqs / failedReqs / consoleMsgs / pageErrors`
    四份清单一起 `slice()` 定格，之后 A/C/D 组怎么折腾都不回写这份快照。**同前提正对照**（A2：`_framework` 请求 218 条）与**牙**
    （D1：故意注入 CDN `<script>` 必须被同一份监听器抓到）读的必须是同一份监听器，否则「0」分不清是"没有外链"还是"监听器死了"（§4 第 16 条 ①的老规矩）。
76. **包一层第三方全局构造函数当页内证人时，静态成员要一起搬 —— 否则红的是尺子不是产品。**
    为了在不依赖公网流媒体能否播通的前提下证明「库真被接上」，`addInitScript` 给 `window.Hls` 装了 accessor 包住 `new`（记 `loadSource` 的 URL、
    `attachMedia` 的目标元素与它所在章节标题）。首跑 B5 `window.Hls.version` 读成 `undefined`：静态 `version` 挂在**类本身**，
    `Wrapped.prototype = Real.prototype` 只搬了原型。补 `Object.getOwnPropertyNames(Real)` 逐项 `defineProperty`，
    再加一条 **B5b**：在包装**之前**先把真身 `version` 记进证人对象，两条对上才算「1.6.15 是真读到的」而不是包装层编出来的。
    这条尺子在旧包翻面上反向立了一次功：同一判据读出 **1.7.3**，直接暴露 `hls.js@1` 这个 CDN 地址**跟着 npm latest 漂**。
77. **「主动注入一次命中」当正向对照时会被内存缓存吃掉，必须带击穿参数。**
    D1 首版在旧发布包上是红的，但红得没有意义：那一版加载时已经请过 `…/dist/hls.min.js`，探针再插一个**同 URL** 的 `<script>` 命中缓存、
    不发新请求 ⇒ 监听器"没抓到"。改成 `…/dist/hls.min.js?probe=<Date.now()>` 后新旧两版都绿
    （`Temp/b27-hls-probe-OLDRELEASE.log` **12/22** → `Temp/b27-hls-probe-FLIP-oldrelease.log` **13/22**，两版红名单逐条对过，**唯一变化的就是 D1 这一条**）。
    可复用做法：凡靠「制造一次命中」证明监听器活着的判据，都要先问这次命中走不走缓存（脚本/图片/CSS 都可能；`fetch` 侧要显式 `cache:'no-store'`，本探针 A3 就是这么写的）。
78. **一条只在用户部署环境出现的报错，不能当验收判据 —— 要钉的是行为，不是症状。**
    用户那边是 `blocked by CORS policy: … more-private address space loopback`，但同一份旧包在我本机跑，CDN 脚本**加载成功**了
    （B4/B6/B7/B8/B9 全绿、`C1 无 CORS 红字`也绿），因为本机有代理把 `cdn.jsdelivr.net` 接回 `127.0.0.1:443` 并真给了内容
    （`Temp/b27-cdn-dns.log`：DNS 返 `151.101.x.x` 假 IP、`curl` 的 `remote_ip=127.0.0.1`、200 之后 20s 超时截在 327680 字节）。
    ⇒ **拦截本身在本机未能复现（未定位到本机触发条件），这条如实记在这里**；能复现且能钉死的是「0 个外部请求」这一形状：
    `B1/B2/B3`（请求层）＋ `D3`（站内字节数与 bUnit 指纹同源）＋ `B5/B5b`（版本号字面量）。改前 9 条红、改后 22/22，方向在两版包上都当场可辨，
    这比"控制台没红字"可靠得多。

79. **一个开着的日期弹层里 `.el-picker-panel` 命中 2 个（面板根＋内层 body-wrapper 同带这个类）—— 用它当"开合证人"必然量错。**
    本批探针首跑把「面板已收起」写成 `.el-picker-panel` 计数，实测 `panels=2` 而不是预期的 0，两条判据一起假红
    （`Temp/b28-probe-diag-panels.log`：同一个开着的弹层 `.el-picker-panel=2`、`.el-date-picker__dropdown=1`）。
    ⇒ 开合证人换成弹层根 `.el-date-picker__dropdown`（实测 开=1 / 关=0），`pickDay` 的选择器也一并限进这个根里，
    免得范围档/双面板场景下 `.el-date-table td.available` 命中到另一块日历。判"翻面红没红"时红字带的 `panels=` 读数也是这个口径。

80. **表格进入编辑态后，行内文本不再有"那一行的文字"—— 行锚点必须在点「编辑」之前算好。**
    `/table` ① 的数据行进入编辑态后「名称」变成 `<input>` 的 value，`tr.innerText` 里搜不到药名，
    用 `hasText: '阿莫西林胶囊'` 重新解析行 ⇒ 「数据行进入编辑态后出现日期编辑器」假红（`Temp/b28-probe-run4.log` 唯一的红，`Temp/b28-probe-diag-table-cell.log` 现场对比确认）。
    ⇒ 先用 `page.evaluate` 在**未编辑态**把 `rowIdx` 算出来，之后一律 `locator('.el-table tbody tr').nth(rowIdx)`。
    同一段还纠正过一次归因：/table 首屏唯一那个 `.el-date-editor` 是表格自带的**新增行**（初值为空），
    在它身上量到的 `00:00:00` 是正确口径不是缺陷 ⇒ B 组拆成「新增行空初值」与「数据行有原值」两段，后者才量得到"只换日期不换时分秒"。

81. **变异期间跑全量套件，得到的既不是基线也不是变异证据。**
    `Temp/b28-tests-full-2.log`（2 失败）是 `dotnet test` 与变异重建撞在同一时间窗的产物：编的是被改坏的源码，
    且两次 build 抢同一 `obj/`。红的那条 `TableColumnForwardsCloseOnDatePick…` 看着像 M6/M7 的证据，但它没有"单点变异"的前提，
    另一条 6s 超时的 `GeneratedMentionSelectsOptionAndUpdatesModel` 更无法归因。
    ⇒ 变异只跑目标类（本批七次各 51 例，`Temp/b28-mut-M*.log`），全量等 `md5sum -c` 还原通过后单独跑一次（`Temp/b28-tests-clean.log` 2580/0/0），文档里的数字只取这一份。

82. **「面板没关」会让下游步骤超时而不是只红一条 —— 超时不是尺子坏，要回头核对它前面那条红。**
    只把 `close` 撤成 `false`（值仍写）这一版翻面里，「参数档：点日期后面板当场收起」红成 `panels=1`，
    紧接着"再点开面板"的 `locator.click` 30s 超时、整个探针以 RC=2 结束（`Temp/b28-probe-flip-P2.log`）：面板还盖在那儿，点击被遮罩吃掉。
    ⇒ 别把这种超时当"偶发"去加超时或重试；红已经出现并点到了正确判据，后面的超时是它的直接后果。
    真正要防的是反方向——如果翻面后**全绿**，才说明判据没牙。

83. **改了 `src/` 或演示页之后必须重启 blazor-devserver：它在启动那一刻加载程序集，`dotnet run --no-build` 更不会重编。**
    本批探针 run1 整段 `waitForSelector('#dt-pick-keep-open …')` 超时的真身就是服务器还挂着改 demo 之前那份装配
    （新段落压根不在页上），杀掉重启后同一份探针立刻能定位到两个 `#dt-*` 行。
    ⇒ 变异/还原/新增演示段三步里每改一次 C# 或 razor 都要 `taskkill` ＋ 重新 `dotnet run`，再跑探针；
    顺带一条同类陷阱：页内 `Escape` 只在焦点落在 `input.el-input__inner` 上时才走到组件的 keydown，
    点过日历一天后焦点回落到 `<body>`，所以关面板那段要先 `.focus()` 再按键（`Temp/b28-probe-run3.log` 的假红）。

84. **按字节/MD5 判"逐字节一致"的守卫，要问一句「检出时会不会被换行转换改写」—— 库里对不等于克隆出来对。**
    本机 `core.autocrlf=true` ＋ `.gitattributes` 的 `* text=auto`，`git add` 时报的
    `LF will be replaced by CRLF the next time Git touches it` **不是排版噪声而是真信号**：
    `hls.min.js` 实测 LF=2/CR=0 ⇒ 全新克隆重检出会变成 541,872 字节，批次㉗ 判据② 当场假红（红的是尺子）。
    ⇒ 三条 git 侧自证读数配齐再看：`git show HEAD:<path> | md5sum`（库里到底存了什么）、
    `git check-attr text -- <path>`（`unset` 才是不动它；`auto` 是跟随 autocrlf 改写）、
    `git add --renormalize <dir>`（无改动 ⇒ 加规则不需要重写已有 blob）。细则见 §3.29 与 `docs/vendored-library-deployment-alignment.md` §9。

85. **前提证人要按"哪一份真有风险"分层，不要顺手写成 Theory 每行的通用断言。**
    判据④ 第一版把"该文件有 LF"写成 Theory 每行的现场证人 ⇒ 同目录 `hls.min.js.map`（实测 **LF=0**、单行 2,383,712 字节）被判红，
    红字「`hls.min.js.map` 里没有 LF，判据④的前提不成立」（**1 红 / 7 绿 / 总计 8**，`Temp/b29-tests-newguard.log`）。
    前提本身错了：单行文件不可能被换行转换改写，它属于"规则覆盖范围"而不属于"转换风险的证人"。
    ⇒ 改成规则档由 Theory 判、"非空转"只由那份真有 LF 的文件判，并给这条新判据单独做翻面（M4 把它指回 `.map` ⇒ 1 红 / 8 绿，`Temp/b29-mut-M4.log`）。

86. **`cp` 备份必须取当前"已验证的好版本"；还原后 `md5sum -c` 报 `FAILED` 不是噪声。**
    本批第一次备份取在**修好之前的那一版**上（备份之后又改了两处判据），于是 `cp` 还原把两处未提交的修复一起退掉了。
    信号当时就有：`md5sum -c Temp/b29-backup/md5.txt` 对测试文件报 `FAILED`，我先归因成"版本不同"而没有立刻处理，
    是靠后续 `grep` 到已删掉的旧断言才发现。⇒ 每次改完判据就刷新备份与 md5；还原后除读 `OK` 之外，
    还要跑一次已知绿量的复跑（本批 `Temp/b29-tests-restored.log` 9/9）当第二证人。恢复一律用 `cp` 备份 ＋ `md5sum -c`，**不许 `git checkout --`**（autocrlf 会吞未提交改动并改 EOL，本仓长期规矩）。

87. **打 NuGet 包要读"两个版本"，并确认消费侧拿到的字节就是刚打的那份。**
    `dotnet pack` 会复用已是最新的构建产物 ⇒ nuspec 写 `0.10.0`、包内 dll 仍是 `2.14.0.0`（本批实测第一版包就是这样，`AssemblyName::GetAssemblyName` 才看得见）；
    `--no-incremental` 只能加在 `dotnet build` 上（加在 `dotnet pack` 上 `MSB1001`）。另一侧：同版本号换内容后 NuGet **不会**重新解包，
    全局缓存里那份旧 `.nupkg` 会被继续命中（实测 `c71b4bae…` vs feed `a3094ed8…`），验收前要先删自己包 id 的缓存目录再 restore，
    然后拿 `MD5(缓存) == MD5(feed)` 当"验收对象没错位"的证人。细则见 §3.30。

88. **一次性验收夹具的两条错，都要在同一条日志里留下"是尺子不是产品"的证据。**
    ① Markdown 字面量写在 Razor 属性里（`Text="# …&#10;&#10;- 列表项"`）被渲成单个 `<h1>`（列表被吞进标题）⇒ 换 `@code` 里的 C# 字符串（真空行）后 `h1`+`ul` 正常，
    **包与组件一个字节都没动**，所以归因是夹具；Razor 对属性里 `&#10;` 的解码细节本批没继续挖，不留"已定位"的假账。
    ② "无控制台错误"把**反向对照自己去 fetch 那个不存在路径**产生的 404 也算成应用的错 ⇒ 正确做法是按 URL 过滤（只排 `__nope__`）
    并**把排除条数如实报成另一条判据**（A6b：`excluded=1 total=1`），不是把整条断言放宽或删掉。

89. **"`_content/{包名}` 只是打包侧的事"是错的：它是派生值，引源码的项目也一样跟着换。**
    Razor SDK 的静态资源 `<BasePath>` 由 `PackageId` 推出，`ProjectReference` 直接引 `src/Components` 的业务项目同样跟着换基路径
    （批次83 实测：`src/Components/obj/Release/net10.0/staticwebassets.build.json` 16 段、`demo/…/obj/Debug/net10.0/staticwebassets.build.json` 195 段，
    改名后**一条旧 id 都不剩**，`Temp/b53-basepath-readouts.log`）。所以库内任何硬编码的 `_content/...` 串都是**改名债**，
    而其中最贵的一类是 **JS 动态 `import()`**：bUnit 不执行浏览器模块加载，改了路径也全绿，只有浏览器层"运行期资源请求零旧前缀"这条能抓（§3.31）。
    另一条相关的错读数留档：`dotnet pack -p:PackageId=…` **不会**把新 id 传到静态资源求值那一层，
    只有落进 csproj 的真实属性才会 —— 探针要用「改前/改后各一份 obj 读数」钉死，别拿一次覆盖参数的尝试下结论。

90. **探针自己的反向对照会污染自己的"零旧前缀"判据 —— 运行期快照必须在任何自造 fetch 之前取。**
    `verify-package-asset-paths.cjs` 首轮 12/13，红的 `A5a` 用的 `performance.getEntriesByType('resource')`
    把 `A2a` 那四条"故意去 fetch 旧前缀"的请求算了进来（`Temp/b53-asset-path-probe.log` 里 `old=[…/Element/css/index.css, …/Element/js/dom.js]`）。
    这不是产品缺陷，也不许用"过滤掉这几条 URL"糊过去（那会把真硬编码也滤掉）：改法是**在导航完成、探针动手之前**取一次快照，
    `A5a` 只判那一段（`requested=14 old=[]`，`Temp/b53-asset-path-probe2.log` 14/14）。同一条规矩也适用于"0 个 CDN 请求"那类计数判据。

91. **有流媒体外链的页面永远等不到 `networkidle` —— 要等"本探针依赖的那件事"，不是等全局空闲。**
    `verify-vendored-hls-lib.cjs` 在 `/video-player-grow` 上 `goto(… 'networkidle')` 30s 超时（RC=2）。归因读数：该页示例视频是
    `media.w3.org` 的 mp4 与 `test-streams.mux.dev` 的 `.m3u8`/`.ts`，按 range 持续取分片（9-12s 静默后 12s 起又是一串），
    11 条 `ERR_ABORTED`、**0 条非 200**、`_content/Element-Plus/js/elvideoplayergrow.js` 与站内 `hls.min.js` 都取到了
    ⇒ 应用与新基路径都没坏，坏的是尺子的等待条件（`Temp/b53-diag-videopage.log`）。改法保持全部判据不动，
    换成有界的显式等待（`window.__vlg.libAssignedAt` 有值＝站内库被注入），并**把这一点写成一条前提证人**（`A0`），
    这样将来真的"库没接上"会红在 `A0` 而不是超时（改后 23/23，`Temp/b53-reg-verify-vendored-hls-lib2.log`）。

92. **`.nupkg` 的 md5 是"这只工件"的指纹，不是"这份内容"的指纹；比缓存指纹之前必须先把缓存清掉重 restore。**
    用户指令"清空 `nuget/` 重打"之后：三包尺寸逐一只一致（1,770,031 / 93,814 / 268,766），**六只 md5 全部变了**。
    逐条目比对给出真身：三包 22/16/24 个条目**名字集合相同、内容 0 处不一致**，只有每包恰好 11 个条目的 zip 时间戳变了
    （`Temp/b53-repack-content-compare.log`）⇒ 抄 md5 必须连"哪一轮打的"一起抄，拿 md5 当"内容变了没"的判据是错的口径。
    第二宗更贵：**全局缓存按 id+version 命中、不校验内容**，重打后 `~/.nuget` 里仍是旧字节而 feed 已是新字节，
    此时"cache 与 feed 逐字节一致"这条证人会**假红**（`Temp/b53-repack-cache-align.log` 首段）；
    删掉 `element-plus[.x|.markdown]/0.10.6/` 三个版本目录 + `restore --force` 之后重新 SAME，消费侧端到端复跑 12/12。
    旧 id 的缓存目录（`element`/`element.x`/`element.markdown`）**不动**——nuget.org 那只对手包 2.14.1 就住在同一个 `element/` 里。

93. **"改名扫过了"只对常驻探针成立，`Temp/` 里的一次性脚本不在覆盖面内；首轮红要先按产物 grep 归因。**
    两轮品牌 sweep 扫的是源码、文档与 `demo/tools/probes/`，`Temp/b27-release-boot-check.cjs` 里写死的三条 `_content/Element/...` 没被扫到，
    于是发布包复验首轮 **7/10**：B5 自己 `fetch` 旧路径拿到 31 字节的 404 文本，那条 404 又连带把 Z1（0 JS 异常）/Z2（0 个 ≥400）判红。
    判"是产品坏还是尺子坏"的依据是**整份发布包逐文件 grep 旧前缀 0 命中**（含 `.wasm`，`Temp/b53-release-after.log` 与那次 grep），
    所以不可能是产物里漏的硬编码；改脚本里那三处路径后 10/10（`Temp/b53-release-boot-check2.log`）。
    配套：静态服务器端口别抢 5200——那是用户自己的 `smarthis.api`（`--urls http://localhost:5200`），本轮改用 5202。

94. **扫 CSS 源码的静态判据有两把常见的"看不见"：注释里的字面量，和只会读字面 px 的取值器。**
    批次94 一支新守卫首跑就红在自己身上（`Temp/b55-guard-new-1.log`，`ScrollbarCaliberGuardTests.cs:147`）：
    `IndexOf("--el-sb-h-hover: 0px")` 命中的是**口径说明注释里的那串字面量**，再往回找选择器就落到别的规则上，
    于是 `Assert.Contains(".el-scrollbar__wrap--hidden-default")` 假红 —— 规则明明在文件里（`:4983`）。
    两条修尺子的规矩，不是放宽断言：① 读之前先 `StripComments()`（`:50`），且**选择器串一律带尾随 `" {"`** 才算钉住规则本体
    —— 这正是 §1「写守卫的五步配方」第 1 条（先掩码再判定）的口径，本批是在**自己新写的 helper 上没照办**才红的；
    ② 取值器要**同时认 `var()` 的回退值** —— `Decl()` 原来只认 `width:4px`，工作区里那条 `width: var(--el-sb-w, 4px)`
    让它把批次93 的 2 例判成假红（`Guard_GlobalScrollbarIsThinVertical_WideHorizontal`、
    `Guard_ThinScrollbarAndTreeV2Body_UseTheSameCaliber`，`Temp/b55-guard-before.log` 复现 → 修后转绿）。
    推论：**凡是"把写死的字面值换成变量钩子"的改动，都会连带要求所有静态守卫升级**；
    改口径时先 grep 谁在按字面量读这个属性（bUnit 守卫、探针里的正则、文档里的读数），别只改一处。

95. **"悬停才显形"这一类判据会被真实（OS）光标污染 —— 要把"前提成立"本身写成证人，红才能归因。**
    `verify-markdown-scrollbar.cjs` 首轮 **19/22**（`Temp/b55-markdown-regress-1.log`），三条红全在显形档。
    真身不在被测改动上：`wwwroot/js/dom.js:39-71` 的挂 class 逻辑有 `if (same) return;` 早退，
    若真实光标早已把 `md-bar-hover` 挂上，随后的**合成**移动不再造成 class 变更 ⇒ Chromium 不重绘伪元素
    （静止档里 `barClass:true` 就是这个形状）。归因靠**否证档**而非猜测：`Temp/b55-md-nudge-1.log` 的 G1（加高在位）
    与 G2（把 `--el-sb-h-hover` 压回 6px）读数**完全相同**（编辑栏 0.865/214、预览栏 0.982/214）⇒ 杠杆不是 #154。
    改的是尺子：给每栏补两条**前提证人**（静止时该栏 `md-bar-hover` 没挂着 ＋ 悬停时该栏 `:hover` 真成立），22 → **26 项**；
    被污染时红在前提证人上并写明「本轮被污染」，不再伪装成产品缺陷（改后 26/26，`Temp/b55-markdown-regress-3.log`）。
    同一条也适用于几何档：**只要判据依赖 `:hover`，就先证明确实在 `:hover`**（本批 ⑫⑬ 打 `hover:true` 就是这个用途 ——
    归零宿主"悬停后仍 0×0"若是没命中 `:hover`，跟"命中了但规则压不住"读数一样）。

96. **CDP 截到空帧会把「画没画」两头一起弄反：像素档假红，而"无墨"这种负向断言假绿。**
    批次94 收尾复跑 `verify-scrollbar-hover-grow.cjs` 出 **10/13**（`Temp/b55-hover-grow-6.log`），红的正好是三条像素档
    （⑤⑥⑩：逐行墨量 `rows` 全 0、⑤ 最底行 RGB `255,255,255` —— 连 EP 那条浅灰底线都不在帧里），
    而 ③④⑦⑧⑨⑪⑫⑬ 这些 DOM 档全绿、`getComputedStyle` 当场读到 `cssH:"12px"`。
    判"是产品坏还是帧坏"用的是同一步序的取证档（`Temp/b55-band-frame-2.log` A1/C1 画出 12 行）＋
    **未改动脚本的复跑档**（`Temp/b55-hover-grow-7.log` 13/13）⇒ 坏的是那一帧。
    **触发条件未复现，所以案上只写"未定位"，不写原因。** 牙补在尺子上：每档取一次**整页**帧、数 `lum<250` 的采样像素占比，
    13 → **17 项**（⑤w/⑥w/⑦w/⑩w）。翻面证明证人有牙（`Temp/b55-frame-witness-flip-1.log`，RC=0）：
    盖一层 `pointer-events:none` 全屏白幕后 DOM 仍 `{hover:true, hBar:12, cssH:"12px"}`，
    而条带 `rows` 全 0、`minL 214→255`、`frameNonWhite 0.2376→0`；撤幕后条带帧指纹回到与正常档**同一个 md5**
    （`8eba0ae6ff03`，白幕档 `0802dbdb91e8`）。**为什么必须专门钉在⑦上**：⑦ 的判据就是「移出后无墨」，
    空帧会让它**恒真** —— 这正是 §4 第 9 条说的"负向断言在缺陷态下会空转 PASS"的像素版。
    通用口径：凡是「按像素判某东西画没画」的档，都要在同轮配一条**整帧有墨**的证人；
    而凡是「按像素判某东西没画」的档，没有这条证人就等于没有判据。

97. **比"逐列边界对齐"之前要先过滤零矩形的表头格**（批次96 尺子错一次）。
    EP 把 `hidden-columns` 模板挂在 `.el-table__header-wrapper` 里，它产出的 `<th>` 宽高都是 0、`left` 甚至能是负数
    （取证档 `Temp/b56-grow-diag-1.log` 里第 7 个 `th` 的 `left=-311`）。
    于是首轮 ⑬c 读出 `7 个 th vs 6 个 td` ⇒ 对不齐的是尺子，不是产品（`Temp/b56-hover-grow-1.log`）。
    通用口径：拿 DOM 集合做"一一对应"的比较时，先把**结构性占位节点**（零尺寸、`display:contents`、隐藏容器）滤掉，
    并把"滤掉了几个"打进日志；不然下一轮真多出一列时，这条判据会因为早就一直红着而失去信号。

98. **"静止档 vs 悬停档"的像素读数必须各自在档口截帧**（批次96 尺子错第二次）。
    `⑮` 原来把静止档的墨带放在悬停之后取，6 行的裁剪框实际盖在一条已经 12px 的条上、量到它的下半截 ⇒ 读出 5 行、当场假红。
    拆成 ⑮r（进容器**之前**）/⑮h（之后）之后，判据顺势从「4 与 11 这两个死数字」改成 `hover > rest` 的**严格增**，
    既不会被口径改动（6→8）打穿，也不会因为一次截帧顺序错而恒真。
    通用口径：凡两档比较，截图/取样时机要写在各自的档里；能表达成"趋势"的判据就别留成两个魔数。

99. **换一轴的"另一轴没动"要在另一轴的常驻尺子上复跑，还要往那一轴真的注一次变异**（批次96）。
    本批把 `::-webkit-scrollbar` 的 `height`（横条）从 0 换回 6→12，声称 `width`（纵条槽宽）仍 0。
    证据不是"我没改那行"，而是：① `verify-batch8.cjs` 的 G1/G2/G3 整支复跑 17/17（`Temp/b56-verify-batch8-1.log`）；
    ② 反向变异 MB2 把表体 `width` 改回 `4px`，看 ⑫ 的 `rule` 证人当场变成 `4px/6px`、⑬ 的 `vSlot` 由 0 变 4、
    ⑬c/⑭c 的逐列边界差重新非 0、G1/G2 同时红（`Temp/b56-*MB2*.log`）⇒ 这两支尺子确实咬在那一轴上。
    通用口径：**任何"我只动了 A" 的声明，都要用"B 轴的牙"来否证一次**；只跑 A 轴的绿档证明不了 B 轴没坏。

100. **bUnit 里凡会触发组件内部 `StateHasChanged` 的调用，必须在渲染 dispatcher 上发起**（批次97）。
    测试在测试线程直接调 `Button(cut).Close()`（fire-and-forget `CloseAsync`），链路走进 `ElPopup.CloseDropDownAsync`
    的 `StateHasChanged()` 时撞 `Dispatcher.AssertAccess` 抛 `InvalidOperationException`——异常被 `_ =` 吞掉，
    表象只剩「WaitForAssertion(Assert.Empty(Menus)) 超时、菜单没关」，看起来像组件缺陷。
    定位法：临时把 fire-and-forget 改成显式 `CloseAsync().Wait(5s)`，AggregateException 当场交出
    `at Microsoft.AspNetCore.Components.Dispatcher.AssertAccess() … at Element.ElPopup.CloseDropDownAsync`
    （`Temp/b97-diag-close.log`）。修法是测试侧 `cut.InvokeAsync(() => Button(cut).Close())`，**不是**给组件加
    dispatcher 兜底（WASM 单线程下用户直调本来就没问题，全仓组件都不做这层包装）。
    通用口径：bUnit 红「等不到状态变化」且组件路径里有没有 await 的方法调用，先怀疑测试线程违规；
    fire-and-forget 的异常不可见，归因前先改成显式 await 抓真异常。

101. **包内核验里「类型名」和「字符串字面量」是两种字节形态，查法不能混**（批次97）。
    .NET 程序集里类型名存在 #Strings 元数据堆（**ASCII**），字符串字面量是 **UTF-16LE**；把整份 dll
    `decode('utf-16-le')` 再数子串，只能命中偶偏移上的字面量，奇偏移的全漏——首轮脚本把 dll 里的
    `_content/Element-Plus` 数成 1 处，而源码里有 4 处（ElTableV2 / ElTreeV2 / ElVideoPlayerGrow /
    `ElementLang.LangPathPrefix`），类型名 `ElSplitButton` 数成 0。改法：字面量按字节模式
    `probe.encode('utf-16-le')` 搜原始字节、类型名按 ASCII 搜（`Temp/b97-nupkg-verify.py`）。
    通用口径：**数"包里有没有 X"先拿源码里 X 的出现次数当正向对照**——计数对不上时先怀疑尺子，
    别急着信「包里没有」。

102. **bUnit 里跨渲染复用 `Find` 缓存的节点派发事件，组件若有异步定时器渲染（动画尾巴），
     派发会被静默丢弃**（批次97）。`ElSplitButtonTests.NavigationKeysSkipDisabledItemsAndClamp`
     单跑必绿、全量负载偶发红（`Expected 2 / Actual 1`）。真身：`ElPopup.RenderDropDownAsync` 开菜单后
     有 `ShowStatus Begin → Task.Delay(10) → End` 一笔**真实定时器渲染**（`ElPopup.razor.cs:894`），
     它落在两次 `menu.KeyDown(...)` 之间时，第二次派发在缓存节点上**handler 不执行、不抛异常、
     产品状态无任何可见变化**（`focusedIndex` 停在 1）。
     取证：临时诊断用例 200 轮循环、每步快照 `focusedIndex`/`pendingFocus`(反射)/`items` 身份与顺序/
     弹层 `ShowStatus`——捕获 1/200（`Temp/b97-diag-navflake-1.log`）：items 四项身份与顺序稳定
     （排除 dispose 重建）、`pendingFocus` 全程 None（排除晚落 consume 与双跑）、
     keydown #2 后读数纹丝不动 ⇒ **派发根本没到 handler**；先排除掉组件侧，剩下的才是测试侧。
     修法（测试侧，判据不放宽）：`OpenAndSettleMenu` 助手——开菜单后
     `WaitForAssertion(() => Assert.Contains("scaleY(1)", cut.Markup))` 等**尾巴渲染的输出**落地
     （`ShowStatus` 初值是 Begin，首帧就是 `scaleY(0)`，`scaleY(1)` 只出现在尾巴渲染的输出里；
     读 Markup 不读字段，字段在渲染落地前就翻了——「撤没撤读 Markup」同款口径），
     尾巴落地后再 `Find` + 派发，8 处开菜单+交互的用例全部换上。
     验证：换装后同款 200 轮循环 **0 红**（`Temp/b97-diag-navflake-4.log`）、单类 27/27
     （`Temp/b97-bunit-split-3.log`）、全量两连 **失败 1 / 通过 2804 / 总计 2805** 且唯一红是既有基线
     （`Temp/b97-bunit-full-3/4.log`；文档同步后再复跑一次仍是同一读数，`-5.log`）。
     通用口径：**组件有异步尾巴渲染时，测试不能拿「开着」当「渲染完了」**；要等以渲染输出为证的
     完成证人，否则跨帧复用的节点派发随时可能被吞。程序化 `Close()` 之类走 `InvokeAsync` 的调用
     不受影响（不经缓存节点派发）。

103. **拿独立 worktree 跑 HEAD 基线时，autocrlf 会把 LF 存盘的样式转成 CRLF，按行匹配的静态守卫会假红**（批次97）。
    为了给「本批之前有多少红」拿权威读数，`git worktree add ../elm-b97-baseline HEAD` 跑全量：
    **失败 3 / 通过 2775 / 总计 2778**（`Temp/b97-head-baseline.log`），比主树多出的一条
    `ElTableTrailingFillerTests.CssZeroesTheFillerPaddingAndStillDrawsTheLeafHairline`
    报「index.css 中未找到尾列归零规则（选择器必须整两行、行首对齐）」——可**同一个 blob** 在主树是
    `CR=0`（md5 `d7fd75a8…`，与仓库内一致），在 worktree 是 `CR=10660`（md5 `7251cb16…`）。
    产品一个字节没坏，是尺子在 CRLF 下失配（`.gitattributes` 没给 css 标 `-text`，autocrlf 生效）。
    通用口径：**基线多出来的红，先 `tr -cd '\r' < f | wc -c` 对比两份 EOL 再归因**；
    worktree 基线只直接信「非文本匹配类」的红，文本匹配类要先按上式自证再记账。
    顺带一条对账口径：**全量总数要能拆成新类例数**——2778 + 27（`ElSplitButtonTests`）= 2805 对得上，
    对不上就说明有一份文件根本没进 dll（本次是另一会话 17:43 落的在途测试，`--no-build` 复跑才发现它 3 红）。

104. **判据的前提要自己成一条判据：落在不存在路径上的样例＝整段静默跳过**（批次99）。
     `verify-ofd.cjs` 里那份「本地选 `.ofd`」的样例路径写成 `probes/Element.Blazor.Demo/wwwroot/...`
     ——仓库里从来没有这个位置，于是 `if (hasInput && fs.existsSync(localSample))` 直接走 else，
     那六条判据**一次也没跑过**，日志上只少不少几行、总数看着还挺正常。修法：样例一律从仓库根算
     （`const REPO = path.resolve(__dirname, '..', '..', '..')`），并且把「样本文件在场」单独 `check()` 一条，
     让它红的时候看得见。通用口径：**分支条件不是判据**，判据要能自己报「这一档没跑」。
     同一条教训也适用于探针里 `if ((await btn.count()) > 0)` 这种「找不到就跳过」的段落 —— 找不到要 FAIL 不要 SKIP。

105. **在分批建的页面上，一次性 `count()` 不能当判据；detail 与判据必须同源**（批次99）。
     改前档里出现过一条自相矛盾的红：`FAIL OFD 预览区存在文件选择入口 | inputs=1` ——
     判据用的是上一行的 `hasInput`（那一刻取到 0），detail 用的是下一行重新数的 `count()`（1）。
     真身：`/grow` 的章节是分批建的（批次㉑），`h1` 出现不等于目标 `.demo-section` 已渲染，
     改后那轮恰好赶上已建好所以绿，改前那轮赌输了＝**假红**。修法：条件要**等**出来
     （循环到命中或超时），并且判据与 detail 读同一个变量。通用口径：**同一个数只取一次，两头共用**。

106. **改前档要假设被测 DOM 整块不在：`page.evaluate` 里的类型误用与截屏超时都会把探针崩成 FATAL**（批次99）。
     两处各崩一次：① `const first = pages[0].getBoundingClientRect()` 之后又写 `first.querySelectorAll(...)`
     ——DOMRect 上没有 `querySelectorAll`，TypeError 直接把整支探针带走；② 失败态页面里没有
     `.el-viewer-grow__ofd`，`scrollIntoViewIfNeeded()` 等 30 秒超时 ⇒ 后面的对照段（信息卡片、护理记录单）
     一起没跑。修法：几何量算前先取元素变量、rect 只当 rect 用；凡依赖被测 DOM 的动作先判存在，
     缺失时打一行 `SKIP  截屏 …（页面上没有 …，这就是改前的真实状态）` —— SKIP 要能被看见，不能静默。

107. **发布包复验的「改前档」不用手工造：上一份没重发的包就是天然翻面档；但字节核验要解到压缩档里面去**（批次99）。
     对重发前的 `artifacts/demo-release` 直接跑同一支探针（BASE 换成 `node Temp/b27-static-server.cjs … 5203`），
     免费拿到 **29/41** 的改前读数，而且这一档是**用户真实部署的那一层**（比开发站 5199 更接近现场，
     开发站还要额外吃一次「静态资源带指纹 + 服务侧缓存」的偏差）。三条纪律：
     ① 静态资源发布同时生成 `.gz` / `.br`，**只看未压缩文件会漏掉过期压缩档** —— 用
     `zlib.gunzipSync` / `zlib.brotliDecompressSync` 解回原始字节再比 md5，三份同源才算过；
     ② `dotnet publish -o` 不清空输出目录（批次㉗ 已记），所以先整份删，删之前确认没有静态服务还开着该目录
     （否则 `Device or resource busy` 会**只删掉一半**），旧 zip 改名留证而不是原地覆盖；
     ③ 跨档对比时**总数变化要能拆开** —— 本批 46 → 41 的 5 条差是「旧包压根没有这份样例 ⇒ 样例按钮判据 1 条红
     + 它下游 4 条整段没跑」，跟产品缺陷混在一起记，就会把「包里没样例」读成「补丁吃掉了样例」。
108. **bUnit 里用谓词的副作用记账，被记账的对象可能还没被赋值**（批次100 · #166）。
    `JSInterop.Setup<int>("elementOfdViewerMount", _ => { seen.Add(cut.Markup.Contains(…)); return 3; })` 配
    `cut = Viewer(p => p.Add(x => x.Document, ofd))`：参数驱动的那次挂载发生在 `Render()` 里头，
    而 `cut` 要到 `Viewer(...)` **返回之后**才有值 ⇒ 记账记到的是 `cut == null` 兜出来的**假 `false`**，
    用例红在「第一次挂载就该发生在有挂载点的那一帧」，而组件其实是好的。
    判据：红在**第一条**而不是第二条时，先怀疑记账时机。改法是把文档用 `await cut.InvokeAsync(() => cut.Instance.LoadBytesAsync(…))`
    命令式灌进来（`Viewer(...)` 已返回，`cut` 必然就位），并在注释里写清为什么不走参数。
109. **「同一个函数被包了两次」的证人别写成派生布尔，要写成「一次动作记几条」**（批次100 · #166）。
    注入脚本用 `window.__b100gen` 挡重复执行，第二次执行时走 `__b100dup++` 早退 ——
    于是「`dupInject > 0`」的含义是**守卫生效了**（好事），我却把它当「dom.js 被执行两次 ⇒ 记录翻倍」（坏事）来断言，
    判据方向整个反了，绿档稳定红一条（`Temp/b100-nullhost2-after-1.log`）。
    换成可数的口径：同一次真实动作在台账里**只能有一条** settled 记录（`bootRecords === 1`），
    原始 `dupInject` 只作为 OBSERVE 打出来看，不进判据。**凡派生布尔先手算一遍它在「好档」和「坏档」各是什么值。**
110. **Git Bash 里 `taskkill /PID 1234 /T /F` 的斜杠会被 MSYS 折叠成路径**（批次100 · #166）。
    报的错长得像参数写错（`无效的参数/选项 - 'PID'`），实际是 `/PID` 被换成了 `C:/.../PID`。
    改法：`powershell -NoProfile -Command "taskkill /PID 1234 /T /F"`。
    另外**杀之前先按端口找归属**（`Get-NetTCPConnection -LocalPort 5199 -State Listen` → `Win32_Process.CommandLine`），
    确认是本项目 `dotnet run` 起的那棵树再动，`/T` 连子进程一起（blazor-devserver 是 `dotnet run` 的子进程，只杀根会留孤儿占端口）；
    顺序固定为**先 kill → sleep → 再 build/启动**，反过来新实例会在 Kestrel「address in use」上崩掉。
111. **常驻探针的 detail 不许只印章节头**（批次100 · #166）：判据读的是整块 `.demo-section` 的 `innerText`，
    而这块文案**开头固定是几行介绍语**，`slice(0, 160)` 印出来的全是介绍语，
    红的时候看不出「到底有没有命中那个字符串」（第一次翻面档就是这样，只能看出「红了」看不出为什么红）。
    改法：把证人放最前面 —— `hasNull=… hasParseFailed=… pagesShown=…` 三个现算的布尔 + 只补**末尾** 90 字（真正的错误文案在末尾），
    于是 M1 档那条红自己写着 `hasNull=true … tail="…wasm-function[256]:0x1d974"`，现场即可归因。

112. **出库前先拆「上一批的包」，别按印象里的形状打**（批次100 · #169）。两件差点出厂的漂移都在这一步：
    ① **版本号**——并发会话已经把 `Element.csproj` 推到 `0.10.29` 并把 28 / 29 两颗包发进了 `nuget/`，
    如果我按「上一批是 27」去占 28 就是覆盖别人的交付物。判「这一档里到底有没有我的改动」不靠时间戳，
    靠**包内 `lib/net10.0/Element.dll` 的 md5 与磁盘 Release 产物对账**：实测 28/29 的 dll 是 `18e0bd37…` / `e2998e5b…`，
    本批磁盘产物 `1c739256…` ⇒ 那两颗包收了 vendor 静态资源（补丁已在），**没收 C# 修复**，所以要占新号。
    ② **包布局**——上一批的 zip 打的是**整个 publish 目录**（含根级 `web.config` / `dotnet.js` / `*.endpoints.json`），
    我第一版只打了 `wwwroot/` 子树：静态托管看着没错，但按老形状换包的人会少掉 IIS 要的那几个文件。
    纠正的判据写成可核对的两条：**文件数与上一批同**（390）、**文件名集合差异全部落在 `_framework` 内容哈希改名**
    （`only in old` / `only in new` 两个集合要成对、且每条都带 `_framework/`），样例与根级文件一条不多一条不少。
    顺带一个自检坑：`zipfile` 手加根目录条目会写出**空名条目**（`namelist()` 里出现 `''`），
    解压工具未必认——打包的脚本要 `assert "" not in names`，并把「与上一批的集合差」直接印进日志。
113. **字节核验的运行时坑：本机 Python 的 `zlib` 没有 `brotli_decompress`**（批次100 · #169）。
    「三份同源」（未压缩 / `.gz` / `.br`）这条判据在批次99 记过，但那次是 Node 跑的；这次先用 Python 写，
    `zlib.brotli_decompress` 当场 `AttributeError` 崩在 leg 中途（`Temp/b100-release-verify-1a-python-traceback.log` 留了原样）。
    规矩：**压缩档这一腿交给 Node**（`zlib.brotliDecompressSync`），Python 只做未压缩与 `.gz`，两个脚本都要跑、都要 `RESULT OK`；
    崩掉的脚本留下的**半份日志**要改名留证再重跑（`-1a-*` 那条），别让它顶掉正式档。
    同一支脚本里的另一条：`console.log('%-20s', x)` 在 Node 里**不是** printf 占位，会原样打出 `%-20s` ——
    看着像「格式化没生效」，其实是根本没格式化；要对齐就 `k.padEnd(20)`。

114. **翻面档的「还原」不是删掉一处改动，而是让源码回到原来那一档 —— 把规则整段包进注释最像还原，其实最像没改**（批次101 · #174）。
    翻面 E 去掉 print 规则后探针 **76/78**（恰好两条打印红），还原时我把
    `.el-viewer-grow__ofd-annot-marker, .el-viewer-grow__annot { display:none }` 整段包进
    `/* FLIP-E … */` 而不是**删掉注释标记**，复跑仍是 76/78（读数同 `Temp/b101-flip-E.log`），
    修掉注释后才回到 78/78 ——
    规则在源码里看着还在，CSS 里其实已经没生效。规矩：**还原后必须复跑并回到满分**，只看 diff / 只看备份 MD5 都不算；
    兜底是「全库搜 `FLIP-[A-Z]|翻面档` 必须 0 命中」，以及拿翻面前的 CSS 备份逐段对账（`Temp/b101-css-backup-flex.css`）。
    两条同批踩到的补充：**① 还原 `dom.js`/`index.css` 只要 `cp` ＋ 复跑；还原 `.razor` 必须重新 `build` 并重启 dev server**
    （翻面 F 就是靠复跑读回 `offsetParent=el-viewer-grow` 才确认新构建真吃到了还原）。
    **② 固定路径的日志复跑前要先 `cp` 走一份** —— 本轮为验还原复跑时把上一次记作
    `Temp/b101-verify-ofd-4.log` 的假还原档覆盖成了 78/78，那份读数只在 `Temp/b101-flip-E.log` 里逐字还在；
    今天的还原对照已改名留证为 `Temp/b101-restore-control-1027.log`。
    同源坑还有一条：本批核心是**原地打补丁**、没留 928,111 的副本，翻面后「拷回批次100 备份」只回到 927,798 ——
    必须再用 `Temp/b101-vendor-patch.py` **重放**，并让脚本干跑先印出与守卫相同的字节 / MD5，落盘后再 `Get-FileHash` 对账。

115. **「换文档」类验收的尺子不能是章节文案，也不能是状态栏文件名**（批次101 · #175，两份原文件验收连踩两次）。
    ① 演示页那句「… 渲染完成：共 N 页」是**宿主**用 `ofdActive` 拼的（`GrowDemo.razor:1419`），而组件内建的
    「选择文件」通道根本不更新 `ofdActive` ⇒ 上传后读到的是**上一份**文档的完成文案（实测 `tail="…出院小结 渲染完成：共 1 页。"`
    同轮 `annots=0 markers=0`，`Temp/b101-accept-two-files-1.log`）。
    ② 文件名进状态栏只代表「选中」，不代表「画完」：把它当正向前提同样假绿
    （`Temp/b101-accept-two-files-2.log` 里「文件名出现在文案里，等待 6s」PASS、角标判据 FAIL）。
    正确尺子是**数 DOM**：`.el-viewer-grow__ofd-page` 的个数 == **包内声明的页数**；而页数、批注数、
    `Remark / Creator / LastModDate / Boundary` 这些期望值**先离线读 zip 里的 XML**（`Temp/b101-ofd-manifest.py`：
    `OFD.xml → DocBody/DocRoot → Document.xml` 的 `Pages/Page@ID`，再 `Annotations → FileLoc → PageAnnot/Annot`），
    浏览器只负责比对「画出来的是不是那一份」。
    ③ **站内副本与用户原文件不是一个体积尺度**：原文件内嵌 10,570,564 B 仿宋 TTF，副本把它换成 25 KB 桩
    （整包 90,720 B），实测渲染耗时 **16s / 18s**（`Temp/b101-accept-two-files-4.log` 的 `waited=`），
    而常驻探针等的是副本，`waitForOfdPages(3, 40)` 的 40s 预算够它自己用（`verify-ofd.cjs:384`）。
    验收脚本的预算按**原文件**放大到 300s；6s 读到的 `annots=0` 是「还没画完」，不是「注释层丢了」。
    大文件判据要带进度证人（每 15s 一行 `WAIT … pages=… annots=…`），预算到点按**现场读数**记红，
    不许改成「超时就算了」或把 `== 包内条数` 放宽成 `>= 1`。
    ④ 同批一处**别藏进「控制台 0 错误」**的读数：`_标准合规` 的 `SignedValue.dat` 是 CMS/SM2
    （`SignatureMethod 1.2.156.10197.1.501`）而不是核心认识的 SES 印章，核心 `K()` 自己 `try/catch` 后
    `console.log(e)` 回 `{}` ⇒ 现场有 2 条 `TypeError` 但以 **log** 出现（`error` 计数仍是 0）。
    判据把它点名成 `OBS 签章层读数 count=2` ＋ 一条「签章层失败没有升级成控制台错误」，
    这样哪天 catch 被摘掉会当场红，而不是继续以"0 错误"混过去（`Temp/b101-accept-two-files-4.log` 第 57 行）。

116. **「要不要重发包」按交付物 vs 磁盘逐字节判，不按「我这一批改没改」判；差异行的方向（包独有条 / 磁盘独有条）用来回吐是谁改的；「RC=0」这种退出码也得落进同一份日志档**（批次101 · #176）。
    批次101 改完 vendor 核心、`dom.js`、`index.css` 后，出厂件里到底有没有这三份，只有两种说法：
    「我改完才打的包，所以有」＝记忆，`Temp/b101-ship-vs-disk.py` 把 `nuget/Element-Plus.0.10.31.nupkg`
    的 `staticwebassets/…` 和 `artifacts/demo-release/wwwroot/_content/Element-Plus/…` 两份**都**与磁盘比字节＝实测。
    结果 vendor **928,111 / `9685428c…`** 与 `dom.js` **224,433 / `bc3a87c1…`** 在三处 SAME ⇒ 本批三件资源无需再动；
    `index.css` 唯一 DIFF。**量的那份 nupkg 要先验身份再说结论** —— `nuget/` 现存的 0.10.31 是 `GeneratePackageOnBuild`
    由 Debug 重打那份（1,895,826 B / `b4413b5c…`，`Element.dll` 3,334,656 B / `bcec1ca2…`），不是出库记录的
    Release 档（1,800,067 B / dll `ac21081f…`）；静态资源与构建配置无关所以「SAME」仍成立，但**程序集半边两份不同字节**，
    这句话必须跟在对账结论旁边（`CHANGELOG.md:16` 已报用户）。**归因靠方向**：磁盘 572,880 / nupkg 574,097（独有条 **13**）/ 发布包 574,833（独有条 **21**），
    而**磁盘独有条两边都是 0** ⇒ 包里的行磁盘上全在，说明是「出厂之后有人把这段删了」，
    多出来的整段是并发会话批次102 的 `.el-form-field--float-auto:not(:focus-within) … ::placeholder { color:transparent }`
    （发布包那档还带 `-webkit-` / `-moz-` 前缀版，所以 21 > 13），批次104 又撤掉 —— 属**别人的出库题**，本批不替决定。
    配套第二条：**同一记号两种口径能读出两个数，引用时必须连针串和日志档一起写**。
    `.el-viewer-grow__annot` 不带括号数 **10 / 10 / 10**，带 ` {` 数 **2 / 2 / 2**（前者含 `__annot__row` 这类后代选择器），
    两份都对；文档原来把 10 那档挂到了 `-1.txt`（那是带括号口径的档）就属于「数字与日志不同源」，
    现在计数脚本 `Temp/b101-ship-vs-disk-viewer-counts.py` → `-3.txt`（RESULT OK）把四档针串一次跑齐并落盘。
    第三条同批踩到：**`verify-ofd.cjs` 的退出码从来没进过日志档**，所以文档里「78/78、RC=0（`-1.log`）」这半句
    在档里找不到对应行（翻遍 `Temp/b101-verify-ofd-*.log`、`-final-1027.log` 都没有 `PROBE_RC`）。
    补跑一份把输出与退出码写进**同一档**的复跑（`node demo/tools/probes/verify-ofd.cjs http://127.0.0.1:5199 > 日志 2>&1; echo "PROBE_RC=$?" >> 日志`）
    ⇒ `Temp/b101-baseline-verify-ofd-2.log`（`PASS` 行 **78** / `FAIL` 行 **0** / 尾行 `PROBE_RC=0`），规矩改成：**引用退出码就当场把退出码写进同一份档**，
    只在终端里看到 `RC=0` 不算证据。
    同批一处小坑顺手记下：`Temp/b101-ship-vs-disk-1.log` 里「`index.css` 差在哪」那行中文标题被 Python 的 stdout
    按 CP936 重编码成了乱码（`-css-lines-1.log` 同样），**但 `bytes=` / `md5=` / 独有条数全是 ASCII，读数没坏** ——
    正则取证只用 ASCII 锚点这条老规矩在本批又一次省了事（脚本只往 stdout 印，落档编码不归它管）。

## 5. 加东西时的检查单

- 新写守卫：§1 五步配方一条都不能省；把规则名与本文件表格同步。
- 新建/保存任何文本文件：UTF-8（无 BOM）。要改一份 GBK 存盘的文件，**先整份转 UTF-8 再改**，否则 Edit 类工具会把坏字节写成 U+FFFD，中文永久丢失（`SourceEncodingGuardTests` 第 2 条会拦）。
- 新增 `.WaitFor*`：写 `timeout: TestWait.Timeout`（否则 `TimingGuard` 拦）。
- 新样式：不写 `gap`（要 margin，配方见技能 ③ 片）；`index.css` 是唯一真源，设计变量进 `THEME_CSS_START/END`，布局补丁进末尾 `/* ==== 组件名 ==== */`。
- 组件按钮有 loading 态：给 `ElButtonRenderTreeDiffTests` 补一条同型用例（切换参数后断 renderer 无未处理异常）。
- 新写探针：连本地演示站就必须支持 `node <probe>.cjs http://host:PORT`（`ProbeHygieneGuard` 会查），不要用 `#app` 当就绪判据。
- 每次特性变更：`CHANGELOG.md` 的 `[Unreleased]` + 相关 `docs/<name>-alignment.md` + 探针回归记录（写了实测数字才算交付）。

## 6. 相关记忆与技能

- 三片技能：`element-blazor-ep-alignment`（流程·环境·验证）、`element-blazor-grow-components`（组件实战）、`element-blazor-testing-probes`（守卫与探针配方）。
- 本文件与 `CHANGELOG.md` 的 `[Unreleased]` 前五条对应（ElButton 序列号、ElTreeSelect、gap→margin、三条守卫误报、等待抖动）。
- 仓库**已** `git init`（早期文档写的"不是 git"已过期）。改前先看 `git status`，提交按文件名 `git add`；
  整仓备份到 `E:\电子病历\_backup_before_<变更名>` 的做法在有大文件未纳入版本控制时仍然沿用。
