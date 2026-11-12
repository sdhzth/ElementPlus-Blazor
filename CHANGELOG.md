# Changelog

All notable changes to Element-Blazor are recorded here.

This project follows Semantic Versioning and uses a Keep a Changelog style structure.

> **版本线说明**：nuget.org 已发布 `0.12.0` / `0.12.1` / `0.12.3` / `0.12.4` / `0.12.5`；
> **`0.12.2` 只在本地源 `nuget/`，从未推送到 nuget.org**（registration 与 nupkg 均 404），
> 升级请直接用 `0.12.3` 及以上。日期取 nuget.org 的 `published` 时间（UTC）。

## [0.12.5] - 2026-11-12

> 四个包（`Element-Plus` / `.X` / `.Markdown` / `.Admin.Template`）均已 listed，
> `published=2026-11-12T08:15:16Z`；tag `v0.12.5`；GitHub Pages 演示站同批部署。

### Fixed

- **`ElTable` 列对齐 `Align` / `HeaderAlign` 此前是「死参数」**（`923c69b`）：参数值虽已抄进
  `TableHeader`，但 `GetCellAlign()` 定义了却从未被调用，`GetCellClass()` 只输出
  `el-table_1_column_N / el-table__cell / is-leaf / is-fixed-*`；而 CSS 里的
  `.el-table td.is-center, th.is-center { text-align: center }` 一直存在，只是没人挂类 ⇒
  表头 `th` 和表体 `td` 一律 `text-align: left`（连 `Align="right"` 也是 left）。
  现按 Element Plus 类名约定输出 `is-center` / `is-right`：表头优先取 `HeaderAlign`，
  表体取 `Align`，分组表头同样生效；**CSS 一行未改**，不影响其他页面。
  demo `/table` 浏览器实测（探针 `demo/tools/probes/diag-table-align.cjs`）：居中列
  `center`、右对齐列 `right`、默认列 `left`；线上 Pages 版本亦复测通过。
- 回归：新增 5 项 bUnit（`test/Element.ComponentTests/ElTableColumnAlignTests.cs`，
  含默认左 / 居中 / 右 / `HeaderAlign` 只覆盖表头 / 多列互不干扰），5/5 通过；
  全量 **2866 / 2871**，5 个失败均为既有 `VendoredLibraryGuardTests` 环境性失败
  （仓库根无 `.gitattributes`，与本批无关）。
- 0.12.4 及更早的过渡写法仍可用：`ClassName="is-center" LabelClassName="is-center"`
  （`ClassName` 进 td、`LabelClassName` 进 th）。

### Changed

- 版本 `0.12.4 → 0.12.5`：三个库 csproj、模板包 csproj、
  **模板工程 `templates/ElementPlus.Admin/ElementPlus.Blazor.Demo.csproj` 里三个写死的
  `PackageReference Version`**（最容易漏，漏了 `dotnet new` 生成的项目仍引用旧包）、
  `publish.yml` 版本注释全部同步；解包核验模板包内引用确为 `0.12.5`。
- **发布后文档/模板元数据整理**：README 中英版标注当前版本 `0.12.5`，离线安装示例里
  写死的 `0.12.1` 全部同步；VSIX `ProjectTemplates/ElementAdminServerRenderProject.zip`
  内 `$safeprojectname$.csproj` / `MyTemplate.vstemplate` 的 4 处版本引用同步到 `0.12.5`
  （字节级替换，其余 28 个条目逐字节不变）；CHANGELOG 补齐 `0.12.0`–`0.12.5` 段落。

## [0.12.4] - 2026-11-12

> 四个包均已 listed，`published=2026-11-12T07:19:00Z`；tag `v0.12.4`。

### Added

- **`ElTable.AutoFitHeight`**：表格高度自动贴合页面剩余空间，同时**仍可自己设定高度**
  （`1e84680`）。新增参数 `AutoFitHeight`(bool)、`FitBottomPadding`(默认 12)、
  `FitMinHeight`(默认 120)、`FitMaxHeight`(0 = 不限)；唯一判定点 `EffectiveHeight`，
  **手动 `Height` / `MaxHeight` > 0 时优先**，此时不绑定 JS 测高，两者不冲突。
  `wwwroot/js/dom.js` 新增 `elementTableAutoFitHeightInit`（监听 resize / scroll /
  ResizeObserver，去重、DOM 移除自清理），C# 侧对应 `[JSInvokable] JsTableAutoFitHeight`；
  demo `TableDemo.razor` 增加对照示例，新增 `ElTableAutoFitHeightTests.cs`。
  ```razor
  <ElTable Data="@rows" AutoFitHeight="true" FitBottomPadding="16">...</ElTable>
  <ElTable Data="@rows" Height="400">...</ElTable>
  <ElTable Data="@rows" MaxHeight="320">...</ElTable>
  ```
- README（中英）加入演示站徽章与横幅（`ace6bb1`）。

### Fixed

- CI `publish.yml` 的 `NU1301`：`templates/nuget.config` 的 `../../nuget` 在 CI 上解析到
  checkout 上一级，seed 目录由 `mkdir -p nuget` 改为 **`mkdir -p ../nuget`**
  （`e11cc36`、`dfab785`），修复后模板包可正常打包发布。
- 浏览器实测发现 `rect.top` 为负时自动高度会虚高，已 clamp 到 0。
- 回归 2861 / 2866，5 个既有环境性失败。

## [0.12.3] - 2026-11-12

> 四个包均已 listed，`published=2026-11-12T05:52:20Z`。

### Added

- **GitHub Pages 免费演示站**：新增 `.github/workflows/pages-demo.yml`，publish WASM 后
  改 `<base href="/ElementPlus-Blazor/">`、复制 `404.html`、生成 `.nojekyll`，站点
  <https://sdhzth.github.io/ElementPlus-Blazor/>（`7d20a4d`）。
- demo 增加表级 `ShowOverflowTooltip` 对照示例（全局开关 vs 列级开关，`3d73f71`）。
- README 记录 .NET 11 兼容性：SDK `11.0.100` 编译 net10 项目、临时切 `net11.0` 编译成功、
  完整 bUnit 套件通过（`50b6556`）。

### Fixed

- **子路径托管导航 404**（`ee11cd6`）：从 Pages 首页点「全部组件」跳到
  `https://sdhzth.github.io/all`，绝对路径丢掉 base。`ElMenuItem.razor.cs` 改为
  `NavigationManager.NavigateTo(Route.TrimStart('/'))`，demo 的 `href="/..."` /
  `NavigateTo("/...")` 全部改 base-relative。

## [0.12.2] - 2026-11-05

> ⚠️ **只打了本地源 `nuget/`，未发布到 nuget.org**：版本列表从 `0.12.1` 直接跳到
> `0.12.3`。该版本的改动已包含在 `0.12.3` 及之后的版本中。

### Added

- `templates/nuget.config` 增加本地源（`../../nuget`），模板工程 `PackageReference`
  对齐 `0.12.2`（`ea13b63`）。
- README 换上 4 张真实运行截图 `docs/screenshots/{elbutton,charts,
  account-table-grow,dropdown-table-grow}.png`（`4787691`）。

### Fixed

- **plain 按钮 hover 白底白字看不清**（`5b3a6f1`）：`src/Components/wwwroot/css/index.css`
  追加 `.el-button.is-plain:hover { background-color: var(--el-color-primary-light-9);
  border-color: var(--el-color-primary-light-7); }`，置于旧规则之后覆盖。
- 模板登录页放开固定 `admin/123456`，任意非空账号密码即可进入主页（`ea13b63`）。

### Removed

- 移除 4 张演示 SVG `demo-img-*.svg`（`62fe148` revert + `5f77857` 删除）。
  ⚠️ demo 的 `ButtonDemo.razor` / `LayoutDemo.razor` / `TableDemo.razor` 仍残留
  `/images/demo-img-*.svg` 绝对路径引用，Pages 上会产生 4 个 404（历史遗留，未修）。

## [0.12.1] - 2026-11-01

> `published=2026-11-01T01:16:35Z`；tag `v0.12.1`。

### Added

- **VS / `dotnet new` 中台模板三件套**：`src/VSExtension` 的
  `ProjectTemplates/ElementAdminServerRenderProject.zip`（VS 2022 新建项目）、
  `templates/ElementPlus.Admin`（`dotnet new elementplus-admin`）、
  `templates/ElementPlus.Admin.Template`（团队分发的模板 nupkg）。模板含
  `/login`、`/dashboard`、`/admin/users|roles|permissions|menus`，登录路由守卫 +
  mock JWT cookie，实例化时命名空间自动替换成项目名。
- **发布链路**：`publish.yml` 改用 NuGet Trusted Publishing（OIDC，`NuGet/login@v1`），
  并顺便打包发布模板包，全程无需长期 API Key（`395d673`、`a30c5c4`、`fdac551`）。

### Fixed

- **`ElCheckbox<T>` / `ElSwitch<T>` 的 `@bind-Model` 回调不达宿主**（`ee4f306`）：`Model`
  是 `Value` 的别名，但回调侧只配了 `ValueChanged`，`ModelChanged` 无 `[Parameter]` 承接，
  被 `CaptureUnmatchedValues` 静默吞掉 ⇒ 界面会翻、宿主字段永远旧值且不报错。
  改为转发属性 `get => ValueChanged; set => ValueChanged = value;`（不引入第三条状态通路）。
  补 7 项 `ElBindModelTests`（含 `@bind-Value` 未回归的正向对照与变异验证）。

## [0.12.0] - 2026-10-31

> `published=2026-10-31T13:53:00Z`；tag `v0.12.0`。

### Changed

- **破坏性**：根命名空间 / 程序集名 `Element` → **`ElementPlus`**（0.11.0 引入，0.12.0 为
  稳定化抬版）。下游必须把 `@using Element` 改成 `@using ElementPlus`，并把 DI 入口
  `AddElementServices()` 改成 **`AddElementPlusServices()`**（漏改会 `CS1061`）。
- `PackageId` 仍是 `Element-Plus*` ⇒ 静态资源 `/_content/Element-Plus/...`、
  `build/Element-Plus.props` 照旧，宿主 HTML 无需改动。
- `0.11.0 → 0.12.0` 为原样抬版：包体 A/B 仅 `nuspec` / `.psmdcp` / `ElementPlus.dll`
  三项不同，dll 同尺寸、仅版本串与 MVID 差异；CSS / JS / 字体逐字节相同。

### Notes

- 0.11.0 改名的完整明细（847 文件 / 1140 处、字节级 ASCII 替换、
  `InternalsVisibleTo("ElementPlus.ComponentTests")` 等）见下方 `## [Unreleased]` 批次记录。

## [Unreleased]

> 本节保留开发期批次明细（0.10.x / 0.11.x 及 0.12.x 实现细节，按时间倒序）；
> 各版本的发布归纳见上方 `## [0.12.x]` 段落。

### Added

- 🔴 **命名空间与程序集名统一改为 `ElementPlus`（破坏性变更，随包 0.11.0）**（2026-10-30）。
  - **要改什么**：根命名空间 `Element` → **`ElementPlus`**，子命名空间同步（`Element.X` / `Element.Markdown` / `Element.ControlRenders` / `Element.ControlConfigs` / `Element.Core` / `Element.Model` / `Element.Lang` …）；程序集名 `Element.dll` → **`ElementPlus.dll`**（X / Markdown 同理 `ElementPlus.X` / `ElementPlus.Markdown`）。
  - **下游必做一步**：`@using Element` → **`@using ElementPlus`**（`_Imports.razor` 或单页）。只引包不改这行会编译不过（CS0246）。
  - **刻意没动的三样**（动了就会连带破坏，记下来免得下次顺手改）：① `PackageId` 仍是 `Element-Plus*` ⇒ 静态资源路径 `_content/Element-Plus/`、`build/Element-Plus.props`、staticwebassets 全照旧，**宿主 HTML 一行不用改**；② JS 全局对象 `window.Element`（`dom.js:2971`，`ElInfiniteScroll` 调 `Element.getScrollDistance`）与 C# 命名空间无关；③ `CHANGELOG` 与 `Temp/` 里的历史写法按留痕处理，未批量替换。
  - **一处陷阱（改程序集名时必须同步）**：`src/Components/Properties/AssemblyInfo.cs` 的 `InternalsVisibleTo("Element.ComponentTests")` 里那个串是**程序集名不是命名空间**，已同步成 `"ElementPlus.ComponentTests"`；不同步则访问库 internal 成员的测试会成片红。`demo/.../Shared/ComponentCatalog.cs` 目录页展示标签同批改成 `ElementPlus` / `ElementPlus.X`。
  - **改法**：字节级 ASCII 替换（`namespace`/`using`/`@inject`/`@namespace` + `cref="Element.`），**不 decode**，绕开本仓 GBK/UTF-8 混编码陷阱，中文零损伤；共 **847 个文件 / 1140 处**。代码里残留的**完全限定引用不全局替换**（会误伤 `demo/Element.Blazor.Demo/…` 这类路径字符串），而是让编译器报错逐个精修：`Element.MessageBox`、`Element.ControlRender.IControlRender`、`Element.TabTextAlign.`、`Element.Model.SelectResultModel`、`Element.ElChartReferenceLine`、`Element.TreeItemBase`、`Element.Markdown.Icon`、`typeof(Element.Extension)`、`typeof(Element.X.ExtensionBuilder)`、razor 里 `Type="Element.MessageType.*"`。
  - **验证**：`src/Components` / `X` / `Markdown` / `demo`(WASM) / `Element.ComponentTests` Release 全绿（0 错）；`dotnet test` **2847 通过 / 5 失败 / 2852**，失败项与改前完全一致 —— `VendoredLibraryGuardTests.VendoredBytesStayExactOnCheckout`，根因是**仓库根没有 `.gitattributes`**（既有故障，非本批引入）。
  - **未办**：`src/VSExtension`（独立 .NET Fx 4.7.2 VSIX，不引用主库）已跟着改成 `ElementPlus.VSExtension`，但本机无 VS SDK，**未做编译验证**。
  - **出库（0.10.36 → 0.11.0；X / Markdown 0.10.7 → 0.11.0）**：`pack -c Release` 三个包 RC=0，包内 `lib/net10.0/ElementPlus.dll`、`ElementPlus.X.dll`、`ElementPlus.Markdown.dll`，依赖与 staticwebassets 条目数照旧；Markdown / X 对主包的依赖已自动抬到 `0.11.0`。
- 🟢 **批次107 · 饼/环图左/右图例列的预留宽从死值 120 改成「按该列最宽图例文字实测」（消费方批次110zzz-w：首页四张图排成一行后环塌成一条线）**（2026-10-29，随包 0.10.36）。
  - **触发点**：宿主 `emrelecrecordplusgroup` 首页 `/`，用户 2026-10-29 的截图标注「**这4个拍成一行，中间有细微的间隙**」⇒ 四张画布从 `Width="250"` 收到 `Width="180"`（消费方 `Pages/Home.razor`）。收完复验时探针量到性别饼图与病案状态环图的 `rOuter` **都只剩 16**，圆心「一览」（实测 30px 宽）画到洞口（1.2·16 = 19.2px）外面（`_tools/v1/b110zzz/probe_wlib_改前.out`）。
    这不是「收宽度收错了」：摆四条路（画布回到 250 / 图例挪到下方 / 摘掉图例 / 改库按实测）给用户选，用户**点名选 4「改库：图例预留宽按实测」**。
  - **真因**：`ElChartGrowEngine.cs:673` 的 `var legendW = 120;` —— 左/右图例列的预留宽与图例文字长短**完全无关**，「男」「女」两个 12px 的字也照扣 120。180 宽画布上：`plotW = 180 − 8×2 − 120 = 44`、`plotH = 120 − 8×2 = 104`，`ComputeRadial` 取 `rOuter = min(plotW, plotH)/2 − 6 = 44/2 − 6 = 16`，与探针读数逐字对得上。
  - **改法**：`legendW = LegendReserveWidth(m)`（新方法 `:1927`）＝ **`26 + 该列最宽图例文字的实测宽`**。26 这笔账：色块起点距绘图区 12 + 色块 12 + 文字前隙 6 = 30，再加 4px 内衬 = 34，扣掉基础外边 8 ⇒ 26；`ComputeLegend` 里 Left 档的色块钉在 `x=12`，所以左列那 4px 落在文字与绘图区之间——**左右两档都是「留 4px」，口径对称**。取 `Max` 而不是取首条：`LegendItems` 对径向单系列是**逐扇区**列类别，预留必须按最长那条。
  - **字宽表不复制第二份**：`ElWatermark.EstimateTextWidth` 由 `private` 改 `internal`（`ElWatermark.razor.cs:358`，只改可见性＋一行文档），字档与 `RenderLegend` 下发的 `font-size=12` 同口径（1 个中日韩字 @12 = 14.36px）。⇒ 首页实测：性别饼预留 120 → **40.36**，病案状态环预留 120 → **89.8**（`180 − 16 − 89.8 = 74.2 ⇒ rOuter = 31.1`，与改后探针读数 31.1 完全相同）。
  - **边界（两条反向守卫，防「顺手改坏另一轴」）**：Top/Bottom 档走的是 `legendH`，与本方法无关 ⇒ 同一张图短/长图例的绘图盒必须**完全相同**；`ShowLegend=false` 不预留 ⇒ `PlotRight == Width − 8`。空数据在 `ComputeLayout` 的 `hasData` 闸门（`:613`）就早退，`LegendItems` 到不了空序列 ⇒ **不给 `Max` 加兜底**。
  - **验证（库端 bUnit）**：`ElChartGrowTests` 新增 6 例（`LegendRight_ShortTitle_ReserveIsMeasuredNotTheDead120`、`LegendRight_LongTitle_ReserveGrowsSoTextEndsInsideSvg`、`LegendLeft_ShortTitle_PlotStartsClearOfLegendText`、`LegendTopBottom_And_NoLegend_LegendTextLengthMovesNothing`、`DonutOnNarrowChart_ShortLegendKeepsTheRingRound`、`LegendRight_ReserveFollowsWidestItemNotTheFirstOne`）。全量 **2852 / 2852 通过、失败 0、跳过 0**（`Temp/b107-test-full-rerun.log`，`TEST_RC=0`）。
    **要报备的一条**：第一轮全量红过 1 条 `ElSplitButtonTests.NavigationKeysSkipDisabledItemsAndClamp`（`Expected: 1 / Actual: 0`，`ElSplitButtonTests.cs:267`，`Temp/b107-test-full.log`）——单跑该套件 27/27 绿、原样复跑全量 2852 全绿 ⇒ 与本轮改动无关的**负载相关偶发**，真身未定位（记着，不算已解决）。
  - **出库（0.10.35 → 0.10.36）**：`Element.csproj:9` 抬版本 → `build -c Release --no-incremental` **0 警 0 错**（`Temp/b107-release-build-1.log`）→ `pack -c Release -o nuget --no-build` RC=0 → `nuget/Element-Plus.0.10.36.nupkg` **1,807,001 B** + snupkg 685,902 B。
    逐件 A/B vs 0.10.35（`Temp/b107-pack-ab-1.log`）：变的只有 `lib/net10.0/Element.dll`（3,056,640/`6d76239d…` → **3,057,152 / `7e18db3d89878b3485d8cb73b8a74dc0`**，与 Release 磁盘产物**逐字节相同**）、`lib/net10.0/Element.xml`（+1,451 B＝本批两条 XML 文档注释）、nuspec / 两个 StaticWebAssets props / psmdcp（版本串与哈希）；**静态资源（`index.css`、`dom.js`、`ofd.umd.min.js`、两个 lang、两颗字体）全部 SAME**。
    ⇒ **这 512 字节里没有并发会话的东西**：`src/Components` 下比 0.10.35 打包时刻（2026-10-28 21:42）更新的源文件只有 `ElChartGrowEngine.cs` 与 `ElWatermark.razor.cs` 两个，正是本批改的那两个。新 dll 里 `LegendReserveWidth` 命中 **2** 次、`LegendTextWidth` **1** 次（0.10.35 两份都 **0** 次），版本串 `0.10.36` 2 次、`0.10.35`/`0.10.34`/`0.10.33` 各 **0** 次。
  - **顺带说清 0.10.34 / 0.10.35**：这两包是并发会话打的、**没有 CHANGELOG 条目**。消费方从 0.10.34 抬到 0.10.36 的 CSS 差额整棵只有 **1 条规则**变——`.el-drawer__body` 多了 `overflow:auto`（随 0.10.35 出库，非本批）：4,713 个规则块里 `replace` 1 块，块级 A/B 见 `Temp/b107-css-diff-34-vs-36-2.log`（同档另证包内 `index.css` 573,882 / `2f3fde34…` 与磁盘逐字节相同）。
  - **消费方接入（emrelecrecordplusgroup）**：`emrelecrecordplusgroup.csproj:15` pin `0.10.34 → 0.10.36`，**页面一行未改**（`Pages/Home.razor` 仍是 `Width="180" Height="120"`）→ `build -c Debug --no-incremental` 0 错 → 重启 devserver（5084）→ 服务侧对账：`/_framework/Element.wasm` **3,056,921 B / `1524d7c9b5a62db91736fbef89d3d9f3`**，内含 `LegendReserveWidth` 2 次、`0.10.36` 2 次、`0.10.34` **0** 次（改前那份 3,056,409 B / `13a92929…`、`LegendReserveWidth` 0 次）；`/_content/Element-Plus/css/index.css` 573,882 B / `2f3fde34…` 与包内相同。
  - **消费侧浏览器实测**（新探针 `_tools/b110zzz_w_lib.mjs`，headless chromium + CDP 9335，视口 1898 与 1280 两档）：尺子量的是**写进 SVG 的出厂几何**（`path` 里的 `A r r` + `<circle r>` 取 `rOuter`、`text.el-chart-grow__legend` 的 `getBBox`），前提自证「属性宽 == viewBox 宽 == 盒宽」⇒ user unit 就是 CSS px。**改后档两档红=0**（`_tools/v1/b110zzz/probe_wlib_改后_1898_v2.out`、`probe_wlib_改后_1280.out`）：性别饼 `rOuter` 16 → **46**、病案状态环 16 → **31.1**、洞口 37.32px 装得下 30px 的「一览」、图例 4 条无一越出 svg 右缘、无一与环绘制盒重叠、控制台 0 错误、1280 档三张图的盒宽仍是 180（没被卡片挤扁）。
    判据**不拍阈值**：断 `rOuter` 等于「`属性宽 − 16 − (26 + 本轮 DOM 实测最宽图例文字)` 推出来的期望」（容差 3px，给引擎估算与 `getBBox` 的那点差）并且比死值档期望大 ≥5；两档读数 46 / 31.1 对期望 46 / 32.5 偏差 **0 / −1.4**。（第一版判据写的是 `rOuter ≥ 40`，改后档被病案状态环的 31.1 打红——那是**我拍的魔法数**，长图例的卡本来就到不了 40，换成推导式后两档都按 0 红收。）
  - **未办 / 待裁定**：① 180px 画布 + 「未打包入库」5 字图例 ⇒ 环半径只能回到 31（直径 62px，视觉上仍是一只能看的环）；要更大只能放弃「四张排一行」，或把该卡图例挪到下方——**挪下方更差**：4 条图例的 `legendH = 4×22 + 8 = 96`，`plotH = 104 − 96 = 8`，环直接没了。② 其余 6 个消费方仍 pin `0.10.31`，本批不会自己流过去（批次105 那条口径照旧）。

- 🟢 **批次106 · ElTable 列宽拖拽 Resizable（消费方批次110uu：报表中心列宽可拖，刷新即复位）**（2026-10-27，随包 0.10.33）。
  - **背景**：报表中心的动态列宽是按「该列最坏显示值」钉死的（消费方批次110ss），用户要能手动拖宽某一列；拍板口径是**不持久化**——拖宽只活在本页生命周期里，页面刷新即复位。
  - **API 面**：表级 `ElTable.Resizable`（bool，默认 false）+ 列级 `ElTableColumn.Resizable`（bool?，null=跟随表级、true/false=覆盖，`ElTableTemplateColumn` 经继承同样可用）→ 登记时归并进 `TableHeader.Resizable`（`AddColumnInternal`，`ElTable.razor.cs`）。事件 `OnColumnWidthChange`（新 `TableColumnWidthChangeEventArgs`：Column/ColIndex/Width/OldWidth）对齐 EP 的 `header-dragend`，只在松手时触发一次，宿主要持久化时在这里拿数据。
  - **拖拽本体在 dom.js（`elementTableColumnResizeInit`）**：手柄 span 由 razor 渲染在可拖列的 th 里（`.el-table__column-resize-handle`，三处表头分支：单行 / 分组第一行跨两行的顶层列 / 分组叶子行；选择列、尾填充列、gutter 不渲染），JS 按手柄幂等绑定（`__elResizeBound`）。**拖拽全程纯 JS 改「表头+表体」两张 table 的同名 `<col width>` 与表宽 style 的 max() 求和，不进 Blazor 渲染管线**——与 `elementTableSyncScroll` 同一条纪律；松手才 `invokeMethodAsync('JsTableColumnResizeEnd', colIndex, w, oldW)`。
  - **覆盖宽落 C#**：`TableHeader.ResizeWidthOverride`（internal set）+ `ResolveColumnWidth` 最前面优先返回 ⇒ 渲染树从此携带覆盖宽，**排序 / 刷新数据 / 分页的重渲染不会把拖出来的宽度打回去**；`.cell` 的内联宽（ShowOverflowTooltip 档）不在 JS 里追改，落定后 Blazor 重渲染按 ResolveColumnWidth−1 重新下发。新表格实例没有覆盖值 ⇒ 刷新即复位。ReportCenter 动态列换 @key 重建实例同样复位（该表本来就靠换实例换列集）。
  - **防误伤**：colIndex=0（选择列）直接 return（宽度由 `SelectionColumnWidth` 钉死）；点了一下没拖（moved=false）在 th 上捕截一次 click，表头点击（排序 / 筛选漏斗）不会替用户触发；拖拽下限 24px（本仓有 44px 的窄列，不能用 EP 的 80 默认）。
  - **CSS（`index.css` 尾部三规则）**：`.el-table.is-resizable th.el-table__cell{position:relative}` + 手柄 absolute（right:-4px / 9px 宽 / z-index:3，压过 .cell 内容、低于弹层）+ hover 右缘 2px 主题色。手柄必须挂在 th 直接子级——塞进 .cell 会被它的 overflow:hidden 裁掉。
  - **bUnit（新档 `test/Element.ComponentTests/ElTableResizableColumnTests.cs`，10 例）**：默认无手柄无类 / 表级全列挂手柄 / 选择列永不挂 / 列级 true 点亮 is-resizable 且只挂该列 / 列级 false 从表级排除 / 分组头只有叶子行挂 / 落定记覆盖宽+col width 变更+事件参数齐全 / 重渲染不掉宽 / 选择列下标忽略 / 同宽不触发事件。
  - **消费方接入（emrelecrecordplusgroup）**：`Pages/Cw/ReportCenter.razor:112`（固定左侧列）与 `:129`（分组子列）两处 `ElTableTemplateColumn` 加 `Resizable="true"`，包版本 0.10.31 → 0.10.33。
  - **已知边界**：拖拽只改 px 列宽——auto 列（`<col>` 无 width）按渲染实宽起步、拖完落显式 width；百分比列宽在拖拽中被按 px 覆盖（本仓百分比列极少，未单独处理）。`Fixed` 列在本仓不复制表格，拖拽两表同名 col 即可，无额外同步。
- 🟢 **批次105 · `ElTableColumn.Formatter` 从「只有参数、没有行为」接到 EP 2.14.0 口径**（2026-10-27；用户在「补进 TableHeader 就好」与「在 `ElTable.razor.cs` 接上调用，配 bUnit＋探针＋翻面＋重打包」两条路里点名**选 2**）。实现与判据细节见 **`docs/table-formatter-alignment.md`**。
  - **缺陷本体**：`Formatter` 参数（`src/Components/ElTableColumn.razor.cs:128`）被原样抄进 `TableHeader`（`src/Components/ElTable.razor.cs:231`）之后**全仓零调用**（`grep -rn "Formatter" src/` 只剩声明与那行赋值）⇒ 数值列一律走 `DisplayRenderFactory` 的裸 `ToString()`，`12.5` 永远打不出 `¥12.50`；`List<string>` 这类不在映射表里的类型 `Eval == null`，**整格直接空白**。
  - **EP 事实（逐字抓自上游，缓存 `Temp/ep2140_table_config.ts`、`Temp/ep2140_tablecol_render-helper.ts`）**：`config.ts:193-208` 的 `defaultRenderCell` 里 `value = property && getProp(row, property).value`（**属性原值**，不是渲染后文本），`if (column && column.formatter) return column.formatter(row, column, value, $index)`（**命中即整体替换，不再 `toString()`**），否则 `value?.toString?.() || ''`；`render-helper.ts:169-178` 是 **`slots.default` 优先**（有默认插槽就不调 `defaultRenderCell` ⇒ formatter 根本不跑）；`$index` 是**当前渲染序列下标** ⇒ 分页时为页内 0 基。
  - **接线**：新增 `UseCellFormatter` / `FormattedCellFragment` / `FormattedCellValue`（`src/Components/ElTable.razor.cs:3295-3333`），`ElTable.razor:446-448`（树形列）与 `:487-489`（主分支）两处单元格各挂一档，顺序 `编辑态 → index → 勾选列 → Template → formatter → Eval`。三档落地：回 `RenderFragment` 直接渲染（对应 EP 的 VNode）、回 `string` 时跟**表级** `EnableMarkupString` 两档、其余 `ToString()`；第三个参数取 `EvalRaw`（`property.GetValue`）而不是 `Eval`（已 `ToString()` 的渲染器）。
  - **一条 Razor 层的硬教训（写进 XML 文档，别再踩）**：想省事在 `.razor` 里 `@((object)v)` 是**不行**的 —— 静态类型 `object` 走 `RenderTreeBuilder.AddContent(int, object)`，那个重载只认 `string`，其余一律 `ToString()`：`MarkupString` 被转义成 `&lt;b&gt;`，`RenderFragment` 直接打成类名 `Microsoft.AspNetCore.Components.RenderFragment`（接线前那档的呈现树转储 `Temp/b105-markup-dump.txt` 里两个都出现过）。类型分派只能在 C# 建的 `RenderFragment` 里做。
  - **演示页**：`/table` 新增章节 `#sec-table-formatter`（`demo/Element.Blazor.Demo/Pages/TableDemo.razor:465-489`），六列同表对照（无 formatter＝裸 `12.5` / `¥N2` / 库存分档 / `第 N 行`／插槽优先 / formatter 回 HTML），`FormatPrice` 的 `else` 返回 `非原值:…`、`FormatMustNotRun` 返回 `不该出现` —— **这两个串是留给尺子和读者的假绿探测器**。
  - **bUnit（新档 `test/Element.ComponentTests/ElTableFormatterColumnTests.cs`，12 例全绿）**：整体替换 / 原值类型（`Int32×3`+`Decimal×3`）/ 四参数齐全（同一个 `TableHeader` 实例、行同引用）/ `$index` 页内 0 基 / `Eval==null` 的类型照跑 / `null` 值照跑 / 插槽优先（台账 `Calls==0`）/ `type=index` 不跑 / 不挂 formatter 时**渲染逐档不变**（反向对照）/ 回 `RenderFragment` / `EnableMarkupString` 两档 / 抛异常不静默画空格。两处判据口径修正：`ElTable` 在 bUnit 里**渲染两拍**（实测 `cut.RenderCount == 2`）⇒ 调用次数一律写 `格子数 × RenderCount`，不硬编码；Blazor Debug 的 `<!--!-->` 注释帧会让 `cut.Markup` 子串断言失真 ⇒ 改 AngleSharp 元素查询。
  - **翻面变异八档（`Temp/b105-mut-formatter.py` M1–M6 打在 `.razor.cs`、`Temp/b105-mut-razor.py` M7–M8 打在 `.razor`，每档 `RESTORE_CLEAN=True`）**：M1 压回接线前状态 ⇒ **11 红 / 1 绿**（绿的正是那条反向对照）；M3 第三参数换 `Eval` 文本 ⇒ 2 红；M4 `$index` 传 `0` ⇒ 2 红；M5 删 markup 档 ⇒ 1 红；M7 formatter 档挪到 `Template` 前 ⇒ `TemplateColumn_WinsOverFormatter` 红；M8 挪到 `index` 前 ⇒ 4 红（含 `ElTableIndexColumnTests` 三条）。**两档证明判据没牙，已按实测改掉**：M2 删 `header != null` ⇒ 0 红（死护栏，但保留成本为零，未删）；**M6 删 `Template == null` / `Type != "index"` / `!IsCheckBox` 三个豁免条件 ⇒ 全量 2836/2836 仍全绿**（`Temp/b105-mut-M6-1.log`）⇒ 那三句是**死代码**，优先级实际由 razor 的分支顺序钉住，于是把它从布尔里删掉、只留 `Formatter != null`，并在 XML 文档里写下这条否证证据。
  - **浏览器验收（`http://127.0.0.1:5199`，新常驻探针 `demo/tools/probes/verify-table-formatter.cjs`）**：**14/14、RC=0**（`Temp/b105-probe-green-1.log`；还原后复跑同 14/14，`-green-2.log`），控制台 canary 先自证数得到再判「0 真错误」。库级翻面档 `Temp/b105-flip.py apply`（`UseCellFormatter` 压回 `false`）+ 重打 demo ⇒ **通过=10 失败=4、RC=1**（`Temp/b105-probe-red-1.log`），红的四条正是 formatter 那四列、格子回吐裸原值 `12.5` / `320` —— **行为本身**证明变异进了被服务的 WASM。表族回归 7 支全绿（`Temp/b105-regress-*-1.log`，各档尾行 `PROBE_RC=0`）：`verify-ellipsis-tooltip` 54、`verify-table-tree-caret` 17、`verify-dyntable` 18、`verify-op-column` 8、`verify-table-edit-typing` 7、`verify-header-click` 5、`verify-datetime-close-on-date-pick` 30 项全 PASS。探针第一版有个**假绿前提**：负向判据读的是整节 `sec.textContent`，而演示说明段落里就写着 `不该出现` / `非原值` 这两个 needle ⇒ 改成只读 `table.textContent`。
  - **全量 bUnit 基线 2824 → 2836**（新增 12 例，`Temp/b105-bunit-full-1.log`：失败 0 / 通过 2836 / 跳过 0 / 总计 2836，`TEST_RC=0`）。
  - **重打包（版本号抬到 0.10.32）**：先按「同版本号覆盖」打过一次 0.10.31（nupkg 1,801,009 B / md5 `6e38437beeb6c3245a8929921614ad00`），随后**用户裁定「抬到 0.10.32」**⇒ 用 `Temp/b105-backup/Element-Plus.0.10.31.{nupkg,snupkg}.pre-b105` 把 `nuget/Element-Plus.0.10.31.*` **还原回覆盖前的字节**（nupkg 1,895,826 B / md5 `b4413b5c815a1938e10dbc1b5c31e36d`、snupkg 780,220 B / `18cf152957e2c15432ec9742c67af092`，还原读数对账 `Temp/b105-repack-031-restore-1.log`），再把 `src/Components/Element.csproj:9` 的 `<Version>` 从 0.10.31 抬到 **0.10.32**（只改这一行；文件本身是并发会话的未提交改动，抬版后编码仍是 UTF-8 无 BOM、LF、4,066 B），然后 `dotnet build -c Release --no-incremental`（`BUILD_RC=0`、0 警告，`Temp/b105-repack032-build-1.log`）→ `dotnet pack -c Release -o nuget`（`PACK_RC=0`，`Temp/b105-repack032-pack-1.log`）。
    交付：**`nuget/Element-Plus.0.10.32.nupkg` 1,801,015 B / md5 `b5c91aa933a82bf911dc01e1d8efc19b`**、`Element-Plus.0.10.32.snupkg` 685,228 B / `7c31651800dca822195ad503e861185a`；nuspec 身份 `id=Element-Plus` / `version=0.10.32` / `authors=Smart`；包内 `lib/net10.0/Element.dll` 3,051,520 B / md5 `7e7045dc15854a5a54c95561ab9e7e97`，解包按成员名验回 `UseCellFormatter` / `FormattedCellFragment` / `FormattedCellValue` 三个都在；包内 `staticwebassets/css/index.css`（`305f4e15685c09bf48280ace4b794090`）与 `staticwebassets/js/dom.js`（`bc3a87c190e53a600639a2196be950af`）与工作副本**逐字节相同**。
    两次构建的 dll 做了逐字节 A/B：同为 3,051,520 B、**只有 80 个字节不同**，差异落在 PE 时间戳（偏移 136）与版本串（偏移 3,050,548）⇒ 0.10.32 与那次 0.10.31 覆盖包**代码完全一致**，抬的只是身份。
    **两处要用户知道的**：① 七个消费方 csproj 目前全部仍 pin `Element-Plus 0.10.31`（AIDBAgent.Client / AntFlowDesigner / Finance.Client / NetEtl.Client / emrelecrecordplusgroup / weblis，另加库内 demo），**0.10.32 不会自己流到他们手上**，要不要按批次103 那套逐站升级 + 编译 + 运行时复验，等用户点头；② `.gitignore:55-56` 已经忽略 `nuget/*.nupkg` 与 `nuget/*.snupkg`（`git check-ignore -v` 实测命中这两行），所以新包默认**不进版本库**，要按批次㉚ 那样提交得 `git add -f`。
    **第三处（抬版带来的新问题，升消费方前必须先裁定）**：0.10.32 是**整棵工作树**的产物，不只是本批。把两包拆开逐件 A/B（`Temp/b105-repack032-ab-vs-031-1.log`）：打包时 `git diff --ignore-cr-at-eol --stat -- src/Components/` = **15 个文件 / +503 行**，本批只占 3 个（`ElTable.razor` +8、`ElTable.razor.cs` +52、`Element.csproj` 的 `<Version>` 一行），其余 12 个是**并发会话未提交的** `ElInput` / `ElInputNumber` / `ElSelect(V2)` / `ElViewerGrow*` / `ElementFieldComponentBase` / `index.css` / `dom.js` / `ofd.umd.min.js`；包内证据对得上——`Element.dll` 由 `bcec1ca2…`（0.10.31，三个 helper 成员名命中 **0/0/0**）变到 `7e7045dc…`（0.10.32，命中 **1/2/1**），`index.css` 由 `3bbc3681…` 变到 `305f4e15…`（`dom.js` 两边同 `bc3a87c1…`）。⇒ **直接把七站抬到 0.10.32，等于把那份还没验收的 CSS/输入框/查看器改动一起推过去**；要么等并发会话收口后再统一升级，要么按批次100 的口径明知「包版本＝整棵树」仍照推，由用户裁定，本批不擅自动。
  - **还原与自检**：变异还原只用 `Temp/b105-backup/ElTable.razor.{cs,}good` 的 `cp`，**没用** `git checkout --`（仓里有并发会话的未提交改动，且 `autocrlf` 会改 EOL）；收尾 `md5sum -c Temp/b105-good-md5.txt` ⇒ 两份都 OK（`.razor.cs` = `142f19cb1ac4c5838748109f6b1e775f`、`.razor` = `256d12292b207db902c147ae0715e6fa`）。

- 🟢 **批次104 · 消费方批次110r 的两条库侧根治：`.el-form-field__label` 的宽度预算取「中间档」+ `ElInputNumber` 只读/禁用不再在失焦时提交文本（随包 0.10.28 / 0.10.29，2026-10-25 在 0.10.31 上复验）**（2026-10-23 动手，2026-10-25 补记）。
  - **触发点**（消费方 `emrelecrecordplusgroup` 本地审核弹窗，用户两条原话）：①「未显示全的，能不能显示完整，但是不要调整宽度，如果超过了formfield的宽度才显示省略号」——口径是三句连着的：要显示完整、**不许改宽度**、只有真超过 formfield 宽度才出省略号；②「这个单价，是只读的吧，但是点击上，然后鼠标移走会变成1，这个要改一下，要不就换个组件」。
  - **变更 A · 标签预算三档取舍**（`src/Components/wwwroot/css/index.css:6252-6279`，规则里就带着这段取舍注释）：旧口径 `max-width: calc(100% - 24px)` 配 14px 字，在窄字段（Lg3≈84px）下把四颗四字标签裁掉（探针实测「打印份数 / 应收金额 / 优惠金额 / 实收金额」滚 64 > 客 58）；**0.10.28 一刀切**改成 `max-width:none + overflow:visible + clip` —— 四字标签确实不裁了，但超宽标签画到邻颗字段上（实测越出本字段 53px、压到邻颗控件 33px），与用户第三句相反；**0.10.29 取中间档**：预算 = 内容盒宽 − 左偏移 12（`calc(100% - 12px)`，标签右沿正好落在 formfield 内边界），`overflow:hidden` + `text-overflow:ellipsis` 保留 ⇒ 84px 字段可容 62px 文字（四字标签占 48），一字不裁，真超宽才出省略号。**没有动任何字段宽度**。
  - **变更 B · `ElInputNumber` 只读闸门**（`src/Components/ElInputNumber.razor.cs:321`（`CommitTextAsync`）、`:341`（`OnBlurAsync`）、`:358`、`:542`、`:605` 各加 `if (effectiveDisabled || Readonly) return;`）：根因不是「只读没生效」，而是只读的 `<input>` 依然能拿到焦点，失焦时仍走提交通路 ⇒ 文本按空解析再夹 `Min`，于是 `Readonly + Min="1"` 的单价 0.2 在「点一下、鼠标移走」后变成 1。修在库里而不是换组件（用户给了「要不就换个组件」这条退路，但换组件会动 DOM 结构和版面，闸门一处就够）。
  - **2026-10-25 在 0.10.31 上的复验**（消费方尺子 `_tools/b110r.mjs`，**红 0 / 共 22**，日志留档 `E:\电子档案\emrelecrecordplusgroup\_tools\v1\regress110s\b110r_0.10.31复跑_红0.out`）：服务侧 `/_content/Element-Plus/css/index.css` 与库源码**逐字节相同**（574,097 B / md5 `3bbc3681a8306ed0073a7c0f49b03096`，本地源 `nuget/Element-Plus.0.10.31.nupkg` 内 `staticwebassets/css/index.css` 同为该 md5）；包内 `lib/net10.0/Element.dll` = 已安装包 = 库工作副本 Debug 产物（`bcec1ca29b373c0f4eef9d9f3aefd571`，构建 16:29:16 晚于源码 15:53:32）。运行时：13 颗标签 0 裁 / 0 越界 / 0 压邻；超宽变异标签省略后绘制右 216 < 字段右 217；单价 0.2 经「点击→点备注（真 blur）」两趟仍 0.2，手工塞 0 再 blur 仍 0；**正向对照**同一条通路打在非只读的打印份数上必须被夹回 `Min=1`（否则「没变」是空转绿）；**两档变异对照**：按回 0.10.28 的 `visible` ⇒ 越 53px / 压邻 33px 复现，按回旧口径（−24px + 14px 字）⇒ 4 颗被裁复现，撤变异均翻回绿。
  - **版本号为什么挂在 0.10.28/0.10.29 而条目今天才写**：并发会话在 2026-10-23 15:04 / 16:01 已打出这两包（批次100 条目里点名过），随后 10-24 又升 0.10.30 / 0.10.31；本批两条变更因此**跟着后两个版本一起出库**，没有单独重发。**要报用户的一处不一致**：本地源里现存的 `Element-Plus.0.10.31.nupkg`（2026-10-24 16:42，1,895,826 B，dll `bcec1ca2…`）是 `GeneratePackageOnBuild` 由 **Debug** 构建重打的，与批次101 记录的 Release 交付档（1,800,067 B，dll `ac21081f…`）不是同一份字节 ⇒ 消费方实测行为以现在这份为准（上面 22 条判据全绿），但发布包/静态托管侧仍是 Release 那份，重发与否由用户裁定，本批不擅自动。
  - **消费方批次110s 顺带撞出、本批只记档未修的库侧缺陷**：`ElSelect.razor.cs:75-77` 的 `private static long dropDownIdSeed` 在 `@typeparam TValue` 的泛型件上是**按闭合类型各自计数**的 ⇒ `ElSelect<int>` 与 `ElSelect<string>` 会生成**同一个 DOM id**（消费方同屏 4 颗 `.el-select` 实测撞成 `el-select-dropdown-2×2` + `el-select-dropdown-1×2`），`aria-controls` / `getElementById` 因此不能用来收口「哪一层是哪颗的下拉」（消费方尺子改用「开窗瞬间同屏唯一可见层 + 探针自盖记号」）。根治方向：把种子挪到非泛型静态类（或 id 里加实例后缀）。
  - **库端断言已补（2026-10-25，覆盖上面两条变更）**：`test/Element.ComponentTests/ElInputNumberTests.cs` 新增 `ReadonlyBlurAndChangeNeverCommitBackToMin`（同一趟 `Change("0") + Blur()` 跑只读/非只读两档：只读必须停在 0.2 且 `value` 属性仍是 `"0.2"`，非只读必须被夹回 `Min=1` —— 后半句就是那条正向对照，闸门不能顺手把可编辑字段的夹取语义也关掉）与 `DisabledBlurDoesNotCommitBackToMin`（禁用同只读）；`ElFormFieldGrowTests.cs` 新增 `Css_FormFieldLabel_KeepsMiddleBudgetWithEllipsis`（直接读 `wwwroot/css/index.css` 里 `.el-form-field__label` 那条规则，逐字钉 `max-width: calc(100% - 12px)`、`overflow: hidden`、`text-overflow: ellipsis`、`white-space: nowrap`、`left: 12px`，并 `DoesNotMatch` 掉 `max-width: none` / `overflow: visible` 即 0.10.28 那档）。
  - **这三条测试的牙口（变异对照，各档日志留档）**：把 `ElInputNumber.razor.cs` 的闸门改成 `if (false)` ⇒ `b104-mut-test-1.log` 两条只读/禁用测试双双 `[FAIL]`（总计 2，通过 0）；把 `index.css` 的标签规则按回 0.10.28 的 `none + visible` ⇒ `b104-mut-css-test-1.log` 那条 CSS 测试 `[FAIL]`（总计 1，通过 0）；两处变异各自还原到记录的 md5（`ElInputNumber.razor.cs` = `4ce4191730bc095c90a13f531725ebb1`、`index.css` = `3bbc3681a8306ed0073a7c0f49b03096`，还原后复跑 `b104-test-restore-1.log` = 失败 0 / 通过 100）。备份档在 `Temp/b104-good-*`，还原用 cp，没用 `git checkout --`。
  - **全量库测试套件的唯一一条红（与本批无关，根因未定位）**：`b104-full-test-1.log` = 总计 2824 / 通过 2823 / 失败 1，失败的是 `ElButtonDebounceTests.TrailingDebounce_NextWindowFiresAgain`——一支用真计时器（`Debounce=120` + `WaitForAsync`）的按钮防抖测试。**只单独重跑该类 3 次全绿**（`b104-debounce-rerun-1..3.log`，各 6/6），本批只动了 `ElInputNumber.razor.cs`、`index.css` 和上面两个测试文件，与按钮防抖无交集 ⇒ 倾向"全量并发下的真计时窗偶发"，但**没有复现到红、也没做负载实验，因此记为未定位**，不当已排除；下一步要挖的是单独重放 + 加负载复现，不是给它加超时或重试。
  - **仍剩的未办**：批次104 的两条变更现在有「浏览器尺子（22 条判据全绿）+ 库端 bUnit 断言（有牙）」两层兜底；未办的只有上面那条防抖偶发红，以及上一条 `ElSelect` 泛型件 `dropDownIdSeed` 撞 id 的根治（改非泛型种子会动所有下拉的 DOM id，要先摆路线让消费方点头）。

- 🟢 **批次103 · 0.10.31 推广到全部消费方（消费方 csproj 升级 + 编译 + 真浏览器运行时验证）**（2026-10-24）。
  - **消费方盘点**（全盘深扫 `E:\电子病历`、`E:\电子档案`、`E:\天杰`、`F:\his源码`，跳过 `_backup` / `_migration_backup` / `_tools` / `_probe` 副本）：真实消费方共 7 个，本次把其中 5 个从旧版升到 **0.10.31** —— weblis（`E:\天杰\lis\weblisgroup\weblis`）0.10.17→0.10.31、emrelecrecordplusgroup（`E:\电子档案\emrelecrecordplusgroup\`）0.10.29→0.10.31、NetEtl.Client（`E:\电子病历\netetl\src\`）0.10.26→0.10.31、AntFlowDesigner（`E:\电子病历\AntFlow-Designer-master\blazor\`）0.10.19→0.10.31、Finance.Client（`E:\电子病历\Finance\src\`）0.10.15→0.10.31；smarthis.web 与 AIDBAgent.Client 前序已是 0.10.31。`Element-Plus.X` / `Element-Plus.Markdown` 两个子包**无真实消费方**（仅库内 `Temp/b31-app` 引用），本次不动。
  - **Finance.Client 补 NuGet.config**：该目录层级内原本没有指向本地源的 nuget.config（新版本号在 nuget.org 上不可达），新建 `E:\电子病历\Finance\src\Finance.Client\NuGet.Config`，把 `element-local`（`E:\电子病历\Element-Blazor-master\nuget`）排在 nuget.org **之前**——nuget.org 上有个同名的 `Element` 包（2.14.1，与本库无关），顺序反了会抢错包。
  - **跨版本破坏性变更核查**（CHANGELOG 批次→版本映射 + 消费方源码 grep，结论全部「不命中」）：① `ILang.InitElementLangBaseAsync()` 由 `void` 改为 `Task` 的接口破坏 —— 7 个消费方**零实现 ILang**；② `ElSelect.Placeholder` / `ElSelectV2.NoDataText` 等默认值由中文常量改为 `null` —— 消费方**零处直接读这些属性**；③ `MessageBoxOption.Title` / `ConfirmAsync` / `AlertAsync` 的 title 默认值改为 `null` —— 仅 Finance 1 处使用且显式传 title。
  - **步进按钮默认值变更的逐站归因**：`ElInputNumber.Controls` 由 `true` 改为 `false` 是本批唯一有观感差异的变更，逐站核过 —— emrelecrecordplusgroup **已有 25 处显式 `Controls="false"`** ⇒ 零视觉回归；weblis(2) / netetl(6) / Finance(16) 未声明 ⇒ 步进按钮随之消失（符合批次102 的库端本意：窄列不再被挤爆）；AntFlowDesigner **显式 `Controls="true"`** 2 处（`FormFieldPreview.razor` / `PreviewField.razor` 的 number 分支——表单设计器的字段预览器要演示步进语义）⇒ 保留步进按钮属预期行为，不是回归。
  - **编译验证**：6 个消费方逐个 `dotnet build` **全部 0 错误**（weblis / emrelecrecordplusgroup / netetl / AntFlowDesigner / Finance.Client / AIDBAgent.Client；编译需全套 env 变量含 `ProgramFiles(x86)`，否则 NuGet 报 `Value cannot be null (path1)`）。全部项目的 `obj/project.assets.json` 实解析到 `Element-Plus/0.10.31`（不是只改了 csproj 而已还原到旧版）。
  - **运行时验证（新探针 `tools/verify_consumer_031b.cjs`，Playwright + msedge headless）**：消费方是纯前端 WASM 站点且登录后才有表单页，故采两腿设计 —— **腿 A 契约腿**（站点真实 `/_content/Element-Plus/css/index.css` 可取且含批次102 压制规则、`.el-form-field` 的 float 类名契约、全站步进按钮审计）+ **腿 B 注入腿**（在真实站点页面注入与库端结构一致的 DOM，用 `getComputedStyle(el, '::placeholder')` 读伪元素计算色，验证压制规则**真的作用于消费方加载的这份 CSS**，而不只是「CSS 文件里有这行」）。**7 站全绿（各 11/11）**：float-auto 未聚焦 → `rgba(0,0,0,0)`（压制生效）、float-never → `rgba(0,0,0,0)`、float-always → `rgb(96,98,102)`（保留可见，不受影响）、float-auto 聚焦 → `rgb(117,117,117)`（恢复可见）；各站 CSS 字节数完全一致（539972 B 传输 / 574096 B 未压缩）⇒ 同源同包；组件层 JS 错误 0 条。
  - **两处「红项」的诚实归因**（都不是本次升级引入）：netetl / Finance 站点的 401 来自后端 API 未启动（`localhost:5100/system/summary`），属环境问题 ⇒ 探针把「后端 API 类错误」与「组件层错误」分开计；AntFlow 唯一的「步进按钮」红项来自源码显式 `Controls="true"`，属预期行为 ⇒ 探针加 `EXPECT_STEPPER=1` 放行，而不是靠改判据把红项藏掉。
  - **消费方用法建议**：`Controls` 默认关闭后，如某个数字框确需 ± 步进（例如表单设计器预览、数值区间快速微调），显式传 `Controls="true"` 即可，键盘上下键步进不受影响；占位符重叠问题**无需使用方任何改动**即已根治。

- 🟢 **批次102 · 库端根治 FormFieldGrow 占位符重叠与 ElInputNumber 步进按钮（随包 0.10.31）**（2026-10-24）。
  - **FormFieldGrow 占位符协同**：`src/Components/wwwroot/css/index.css` 新增规则——`.el-form-field--float-auto:not(:focus-within)` 与 `.el-form-field--float-never` 状态下，`.el-form-field__content input/textarea::placeholder` 置 `color: transparent`；标签本身在「未浮动态」即停在输入框正中充当占位符，消除 ElSelect「请选择」/ElDatePicker「请选择日期」与标签同位置叠印的「请选择状态」乱码。聚焦（`:focus-within`）或恒浮（`--float-always`）时标签让位，占位符自动恢复可见，使用方无需任何改动。仅用标准 `::placeholder`（现代浏览器已统一支持，去掉 `::-webkit-input-placeholder` / `::-moz-placeholder` 前缀——实测后者与 `:not(:focus-within)` 组合时 Chromium 会**整条丢弃**该规则）。
  - **ElInputNumber.Controls 默认 false**：`ElInputNumber.razor.cs` 的 `Controls` 参数默认值 `true → false`，数字输入框默认不再渲染 −/+ 步进按钮，窄列不再被挤爆。需保留步进按钮的调用方显式 `Controls="true"`（demo 的 InputNumberDemo / BasicDemo / FormDemo / I18nDemo 共 18 处已补）。
  - **发布**：csproj 的 `wwwroot/css/index.css` 原被 `<None>` 标记为不发布，导致 demo/NuGet 包加载的是旧样式；改为默认 Content 使其作为 StaticWebAsset 发布（`GeneratePackageOnBuild` 直接出 `Element-Plus.0.10.31.nupkg`）。
  - **验证**：全量单元测试 2821 通过 / 失败 0（含 `ControlsDefaultsToFalse` 等新断言）；demo 真浏览器探针确认占位符未聚焦透明、聚焦恢复、Float=Always 不受影响；smarthis.web 升级 0.10.31 后 `verify_form_fix` 10/10 + 门诊主线 E2E 24/24 全过，控制台 0 错误。

- 🟢 **批次101 · 取舍反转（用户口径「已审核这里显示这个 (i)，点击会显示，打印的时候要求不能显示」）：`ElViewerGrow` 的 OFD 批注从「只做悬停 title」改成「批注盒右上角 (i) 角标 + 点击读卡 + 打印抑制」，顺带修好定高 viewer 裁掉预览区底部与状态栏的高度账（随包 0.10.31）**（2026-10-24，#174–#176）。
  - **反转了什么**：批次100 的取舍是「不在页边画 (i) 角标（凭空加图标会改用户看到的版面），代价是不悬停就发现不了批注」（`docs/viewer-alignment.md` §12.2 原文），用户不接受这个代价。批次101 的角标**仍然不改版面**：挂在批注盒自己的右上角（批注盒就是定位上下文），`dom.js` 画成真 `<button>`，点击冒泡到组件宿主开卡片，纸上由 viewer 的 `@media print` 一律 `display:none`；也不需要宿主注入渲染器。
  - **vendor P8**（`Temp/b101-vendor-patch.py`，两对锚点各断言「恰好命中 1 次」，干跑先印预期字节）：在 P7 的 `title` 之外，把 `remark / creator / lastMod` 各写一条结构化 data 属性（`data-ofd-annot-remark` / `-creator` / `-time`）—— 标题给鼠标读者，data 属性给点击读卡，**不要把拼接好的 title 再拆开**。字节指纹：批次100 927,798 / `b596eb75…` ⇒ **本批 928,111 / `9685428c3f13476a7b69022ed27bd104`**（LF 69 / CR 0 未变）。
  - **dom.js**：paint 之后 `decorateAnnotationMarkers` 给每个 `[data-ofd-annot]` 补一枚 `button.el-viewer-grow__ofd-annot-marker`（`type=button`、一枚 `(i)` 圆点、`aria-label` 就是 Remark）；新增 `window.elementOfdViewerAnnotationAt(clientX, clientY)`：`elementFromPoint` → `closest(marker)` → `closest(annot)`，命中回 `{remark, creator, time, type, id}`，空白回 `null`。
  - **ElViewerGrow**：宿主 `@onclick` → 问通道 → 命中才开卡片。卡片挂在**根盒子**下而不是 `__viewport`：viewport 是滚动容器，`inset:0` 按内容高度铺开（三页实测 `scrollHeight 4409 / clientHeight 558`，卡片被居中到 `top:-488` 的可视区外，用户只看到变暗）；根盒子是 `position:relative + overflow:hidden + 定高`，`inset:0` 恰好＝可视框。遮罩点击关、卡片 `@onclick:stopPropagation`、关闭按钮关，换文档先关卡片，作者 / 时间为空不画空行。
  - **根盒子高度账**：`.el-viewer-grow` 由 `block` 改 `display:flex; flex-direction:column`，定高 560 时工具栏 / 预览 / 状态栏三段正好装满。改前根盒子 `scrollableOverflow=70`、状态栏中心点 `elementFromPoint` 命中的是 `DIV.demo-section`（`Temp/b101-status-visible-2.log`）；改后 `overflow=0` 且状态栏中心命中自己（`Temp/b101-root-box-2.log`）。
  - **守卫**：`Guard_OfdCore_…` 改钉 928,111 / `9685428c…` + **十条**锚点各在 + **七条**各恰好 1 次（新增 P8 锚点）；`Print_CssSection_…` 另钉打印段里同时有 `.el-viewer-grow__ofd-annot-marker` 与 `.el-viewer-grow__annot`。**天然翻面档＝批次100 核心**（`Temp/b100-core-p5p7-backup.js`）⇒ 红在长度钉 `Expected: 928111 / Actual: 927798`（`Temp/b101-guard-flip-b100core-1.log`），P8 锚点在批次100 核心命中 0 次、现档 1 次。本批核心是原地打补丁、没留 928,111 的副本，还原不是拷备份而是用 `Temp/b101-vendor-patch.py` 从批次100 字节**重放**（干跑先印 `would be bytes=928111 md5=9685428c…`），守卫 1/1 绿（`Temp/b101-guard-restore-1.log`）。
  - **bUnit 3 例**：点宿主 → 卡片内容 / 作者 / 时间齐全（关闭按钮与遮罩都能关，卡片 `stoppropagation` 在）；通道回 `null` 不渲染遮罩；换文档先关卡片、空作者 / 时间不画空行。全量 **2819/2819 通过、失败 0**（`Temp/b101-bunit-full-3.log`）—— 账：批次100 的 `2815（含 1 红）+ 3（本批卡片三例）+ 1（下面 #177 的豁免档）= 2819`，红从 1 变 0 是那笔尺子债还清，不是「这轮碰巧不复现」。卡片本体点击 bUnit 测不了（只挂 `stopPropagation`、无 handler ⇒ `MissingEventHandlerException`），这一条钉接线，行为交给真浏览器探针。
  - **#177 · 还清那条从 2026-10-21 起长期红的尺子债**（`ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows`，批次92–100 每一支全量都记它「唯一红＝既有基线」）：真身是**夹具过期**，不是组件回归。守卫在批次91-95（提交 `61a990d`）按 EP 口径放宽成「`ElTableTemplateColumn` **或**带 `ChildContent` 的列」都不强制 `Property`（`ElTable.razor.cs:185-190`），而这条用例的坏档一直用**带模板**的 `ElTableColumn` ⇒ 放宽后它根本不抛，`Record.Exception` 拿到 `null`，`Assert.IsType<ElementException>` 天天红。修法是**把坏档改窄**成真会踩守卫的那一档（无 `Property`、无 `ChildContent`、`Type` 非 index/selection），**并另立 `PlainColumnWithChildContentDoesNotThrow`** 钉住 EP 的豁免档（只写插槽不写 `prop` 的操作列合法、必须渲染出 2 行 2 个 `op-cell`）—— 两支是一对反向判据：豁免档红＝以后有人把守卫又收紧回去伤到合法写法，坏档红＝守卫真失守。**没动库**（`ElTable.razor.cs` 一行未改），改的是测试文件（`:86` 新夹具 `PropertylessPlainColumn`、`:134` 把坏档换成它、`:147` 新增豁免档用例）。基线记档同步在 `docs/testing-guards.md` §1 的「基线更新（批次101）」块。
  - **常驻探针 `verify-ofd.cjs` 61 → 78 项**：角标数量 / 真 button / 右上角 ≤4px / 屏幕可见；真鼠标点角标 → 卡片内容即 Remark、作者时间来自 data 属性、落在可视区内、遮罩钉根盒子、点卡片不关 / 关闭关 / 遮罩空白关；`emulateMedia({media:'print'})` 下两件 `display:none`，并**在同一次运行里先读屏幕媒体的 `flex/block`、最后切回屏幕媒体确认重新可见**（否则打印判据会空转）；根盒子 `overflow===0` + 状态栏中心点命中自身。实测 **78/78、控制台 0 错误**（`Temp/b101-verify-ofd-5.log`）；**翻面 D**（删 dom.js 角标段）⇒ 4 红 + 点角标 FATAL（`Temp/b101-flip-D.log`）；**翻面 E**（删 print 规则）⇒ 76/78，恰好两条打印红，detail 回吐 `overlayDisplay=flex / markerDisplay=block`（`Temp/b101-flip-E.log`）；**翻面 G**（根盒子压回 `block`＋预览区 `height:100%`）⇒ 76/78，恰好两条根盒子红（`overflow=70 client=558 scroll=628` / `statusHit=false status=826..855 rootBottom=827`，`Temp/b101-flip-G.log`）；**翻面 F**（遮罩挂回 `__viewport`，要重编译）⇒ 76/78，`卡片落在可视区内…` 翻成 `offsetParent=el-viewer-grow__viewport cardTop=-557`，「点遮罩空白关」的自证证人当场回吐 `blank=0,0 hitsOverlay=false cards=1`（`Temp/b101-flip-F.log`）。四档全部还原后复跑 **78/78、控制台 0 错误**（`Temp/b101-verify-ofd-final-1027.log`，并读回 `offsetParent=el-viewer-grow`、`cardTop=453` 证明还原进了新构建）。顺带记一次**假还原**：还原时把规则整段包进注释而非删掉注释，复跑仍 76/78（读数同 `Temp/b101-flip-E.log`），坑记在 `docs/testing-guards.md` §4 第 114 条 —— 该档的日志名是写死的，2026-10-27 为验还原复跑时把它覆盖成了 78/78（那一份已改名留证为 `Temp/b101-restore-control-1027.log`），新规矩：**固定路径的日志复跑前先 `cp` 走一份**。
  - **用户那两份原文件的真浏览器验收（#175，2026-10-27 补跑）**：`住院病案首页_OFD-H.ofd`（6,026,308 B）与 `住院病案首页_OFD-H_标准合规.ofd`（11,841,181 B，不入库＝真实病历数据）走演示页「选择文件」字节通道，**42/42 通过、控制台 error 0**（`Temp/b101-accept-two-files-4.log`；前两档 `Temp/b101-accept-two-files-{1,2}.log` 各留一次假绿前提当反面证据）。期望值不再手写：`Temp/b101-ofd-manifest.py` 离线读包（OFD.xml→DocRoot→Document.xml 的页数与 PageID、`Annotations.xml`→`FileLoc`→`PageAnnot/Annot` 的 `Type/ID/Creator/LastModDate/Remark/Appearance@Boundary`），浏览器只比对画出来的是不是那一份 —— 两份都是 **3 页（PageID 1/4/6）+ 1 条批注（`Path` / `ID=11` / `Boundary="148 246 38 18"` / 作者「王芳（质控医师）」/ 时间「2026-10-24」/ Remark「病历质量审核：病历内容完整、书写规范，予以审核通过。」）**，DOM 侧 `annots=1 markers=1`、角标 `18x18` 钉在 `175x83` 批注盒右上角（`dRight=0 dTop=0`）、`aria-label` 与包内 Remark 逐字相同；真鼠标点击 → 卡片 `cardTop=718 cardBottom=894`（可视框 1000）`offsetParent="el-viewer-grow is-bordered"`、作者/时间两行齐全、点关闭 `overlayAfterClose=0`；`emulateMedia({media:'print'})` 下 `overlayDisplay=none markerDisplay=none`，切回屏幕媒体 `flex/block` 重现。渲染耗时 **16s / 18s**（原文件内嵌 10,570,564 B 仿宋 TTF，站内样本把它换成 25 KB 桩 ⇒ 只有 90,720 B，常驻探针的 40s 尺度套不到原文件上）。**两条假绿前提**（尺子课，细则进 `docs/testing-guards.md` §4 第 115 条）：① 章节那句「… 渲染完成：共 N 页」由宿主的 `ofdActive` 拼（`GrowDemo.razor:1419`），组件内建选文件通道不更新它 ⇒ 读到的是**上一份**文档；② 文件名进状态栏只代表「选中」不代表「画完」⇒ 判据换成数 `.el-viewer-grow__ofd-page`。**一处如实点名的已知边界**：`_标准合规` 那份的 `SignedValue.dat` 是 CMS/SM2（`SignatureMethod 1.2.156.10197.1.501`，Provider `NetOfd.Sign`）而不是核心认识的 SES 印章，核心 `K()` 自己 `try/catch` 后 `console.log` 并回 `{}` ⇒ 现场读到 2 条 `TypeError`（`ofd.umd.min.js:62:34174` 读 `null['1']`、`:62:24233` 读 `undefined['eseal']`，`Temp/b101-accept-two-files-4.log` 第 57 行），**是 log 不是 error，页数与批注都不受影响**；与 §11.5「这份包的签名里没有印章图，电子印章那一格在数据层无物可画」同源，站内样本 `ofd-h-case-page.ofd.bin` 的 `SignedValue.dat`（917 B）是同一格式。另一份（无 `Signs/`）`sealLogs=0`。验收截图 `Temp/b101-accept-shots/accept_{0,1}_{marker,card,print}.png`（同名对 md5 逐对相同 `36ca2dc6… / cc23828b… / d44706f7…` —— 两份包内容本就是同一页病案首页，「像素一样」同时也是「根本没换文件」的形状，所以否证证人挂在 DOM 上：每个文件跑在独立的 `newContext`、上传前读到 `title="out.ofd"`、上传后 `PASS … 标题回填成刚上传那一份` 的 `title=` 逐字是刚选的文件名，离线台账另有 `bytes=6026308 / 11841181`，等待耗时各自 16s / 18s）。
  - 两个 OFD-H 样例的真浏览器验收：病案首页（1 条批注）角标可见、点击出卡片；护理记录单（无批注层）没有角标可点，属正常；截图 `demo/tools/probes/shots/ofd_h_annot_{marker,card,print}.png`（打印无角标无卡片）。实现与证据见 `docs/viewer-alignment.md` §13（副本档）与 §14（原文件补跑）；尺子课见 `docs/testing-guards.md` §3.37 与 §4 第 114–116 条。
  - **出库（0.10.30 → 0.10.31，NuGet + 发布包重发）**：`build -c Release --no-incremental` **0 警 0 错**（`Temp/b101-release-build-1.log`）→ `pack -c Release -o nuget --no-build` RC=0 → `Element-Plus.0.10.31.nupkg` **1,800,067 B** + `.snupkg` 685,070 B。核验 `Temp/b101-nupkg-verify-1.log`（RESULT OK / 0 fail）：nuspec `Element-Plus` / `0.10.31` / `Smart`；包内核心 **928,111 B / `9685428c…` 与磁盘逐字节相同**、十条锚点各在、七条各恰好 1 次；`lib/net10.0/Element.dll` **3,051,008 B / md5 `ac21081f12ee84e82e01378824eb7dd5` == Release 磁盘产物**；UTF-16LE 版本串 `0.10.31` 3 处、`0.10.30` / `0.10.29` **各 0 处**。发布包：旧 zip 改名留证（`Temp/b101-release-old-package.zip`，md5 `903a0297…`＝批次100 交付那份），确认 5203 / 5204 空、5199 只握着 `demo/bin/Debug` 后**整份删** `artifacts/demo-release` 再 `publish -c Release -o`（批次㉗ 的坑）；新档 **390 个文件**，`_framework` 四颗程序集**各只有一个**内容哈希（`Element` = `1a1w350i2m`，wasm 3,050,777 B / md5 `b20638614294a72c6871914b26fd574e`，版本串 `0.10.31` 3 处 / `0.10.30` 0 处）。三份同源（`Temp/b101-release-verify-node-1.log`，RESULT OK）：未压缩 **928,111 / `9685428c…`**、`.gz` 253,285 B 解回同档、`.br` 200,006 B 解回同档；HTTP 侧（5203）`Accept-Encoding: identity` 落盘同为 928,111 / `9685428c…`。**静态托管复验（5203）：OFD 探针 78/78、启动自检 10/10、站内 vendor 无公网 CDN 23/23、Viewer 交互 20/20，控制台 0 错误**（`Temp/b101-release-{probe-ofd,boot-check,hls,viewerux}-1.log`）。交付物 `artifacts/Element.Blazor.Demo-release.zip`：**414 条目 / 390 文件 / 24 目录 / 23,311,066 B / md5 `38138bda5d9ed503abed27c135a960c7`**，与旧包文件名集合差异 **12 / 12 条全部落在 `_framework` 内容哈希改名**（`Temp/b101-release-zip-2.log`），样例与根级文件一条不多一条不少。
  - **收尾复跑（2026-10-27，#176）**：本批最后一轮 CSS／`.razor` 抖动之后重新验收一遍。顺序按规矩来（`dotnet test` 会重建 Debug 产物、打断常驻服务）：先杀自己起的 5199（PID 62428 整棵树）并确认监听已空 → `dotnet test` 全量 **2824 / 2824 通过、失败 0、跳过 0**、RC=0（`Temp/b101-bunit-full-4.log`；本批出库那档仍是 **2819**＝`Temp/b101-bunit-full-3.log`，多出的 5 条是并发会话批次102（+2）/ 批次104（+3）的新断言，本批数字一位未动）→ 重启 dev server（root 60300、监听 2288）→ 常驻 `verify-ofd.cjs` 复跑仍 **78/78、控制台 0 错误**、RC=0（读数档 `Temp/b101-baseline-verify-ofd-1.log`；退出码与计数同档的复跑 `Temp/b101-baseline-verify-ofd-2.log`：`PASS` 行 **78** / `FAIL` 行 **0** / 尾行 `PROBE_RC=0`）。
  - **交付物 ↔ 磁盘逐字节对账（`Temp/b101-ship-vs-disk.py` → `-1.log`，差异行 `Temp/b101-ship-vs-disk-css-lines-1.log`，规则计数 `Temp/b101-ship-vs-disk-viewer-counts.py` → `-3.txt`）**：「要不要重发包」不按「我改没改」判，按两份出厂件里的静态资源 vs 磁盘比 —— vendor 核心 **928,111 / `9685428c…`** 与 `dom.js` **224,433 / `bc3a87c1…`** 在 `nuget/…0.10.31.nupkg`（`staticwebassets/…`）和 `artifacts/demo-release/wwwroot/_content/Element-Plus/…` 两边**都 SAME** ⇒ 本批的角标 / 卡片 / 打印抑制确实在出厂件里，**这三件资源无需再动**。**量的哪一档要说清**：`nuget/` 现存的 0.10.31 是 `GeneratePackageOnBuild` 由 **Debug** 重打那份（1,895,826 B / md5 `b4413b5c815a1938e10dbc1b5c31e36d`，`Element.dll` 3,334,656 B / `bcec1ca2…`），不是上一条记录的 Release 档（1,800,067 B / dll `ac21081f…`）；静态资源与构建配置无关，所以「SAME」成立，但**程序集那半边两份不是同一份字节**（本文件开头已把这笔不一致报用户），重发与否仍由用户裁定。**唯一 DIFF 是 `index.css`，方向能直接归因**：磁盘 572,880 / `305f4e15…`，nupkg 574,097 / `3bbc3681…`（包独有条 **13**）、发布包 574,833 / `8a1aac13…`（**21**），而**磁盘独有条两边都是 0** ⇒ 包里的行磁盘全在＝出厂之后有人删了；多出的整段是批次102 的 `.el-form-field--float-auto:not(:focus-within) … ::placeholder { color:transparent }`（发布包那档另带 `-webkit-` / `-moz-` 前缀版，所以 21 > 13），批次104 已撤 —— 属**并发会话的出库题**，本批不替决定。viewer 的规则三份一条不缺：`.el-viewer-grow__ofd-annot-marker` **3/3/3**、`.el-viewer-grow__annot` **10/10/10**（含后代选择器；带 ` {` 的窄口径 **2/2/2**，两份读数都对、别混引）、`.el-viewer-grow {` **2/2/2**、`@media print` **4/4/4**。尺子课见 `docs/testing-guards.md` §4 第 116 条。

- 🟢 **批次100 · 真身缺陷两处（一处 C# 挂载决策、一处 vendor 注释层）：`ElViewerGrow` 打开 OFD-H 病历时报 `elementOfdViewer: container is null`，且「印章信息 / 交互信息」不显示（用户报障页面 http://localhost:5199/grow，文件 `住院病案首页_OFD-H.ofd` 与 `住院病案首页_OFD-H_标准合规.ofd`；随包 0.10.30）**（2026-10-24，#166–#169）。
  - **报障原话**：「打开…会出现报错信息 `elementOfdViewer: container is null`（`dom.js:3232` → `w.processJSCall` → `dotnet.native…wasm-function[291]`），**但是通过，正常，但是没有显示印章信息和交互信息，这个是其他的阅读器读出的**」＋ 别的阅读器读出的红章「已审核 / 审核医师：王芳 / 2026-10-19 16:30」与页边 (i) 角标截图。定性：**两件互不相干的缺陷**，都要修 —— 报错在 C# 侧（谁在什么时候把 `ElementReference` 交给 interop），缺印章在 vendor 侧（渲染核心查不到 OFD-H 写的包内绝对路径）。
  - **真身一（两条来路，同一句 `container is null`）**：挂载点 `<div class="el-viewer-grow__ofd" @ref="ofdHost">` **只在「画布」分支里**（`ElViewerGrow.razor:121`），`Empty`/`Error`/`Unsupported` 三个占位符分支连它一起撤掉；而 **Blazor WASM 的实测事实**是「`ElementReference` 指向的元素在上一次提交的帧里已离开 DOM ⇒ interop 到 JS 收到 `null`」，`elementOfdViewerMount` 第一件事（量容器）当场抛错，栈看起来纯属 JS 病。来路 a：先传一份解析失败的 `.ofd` 紧接着传正常文档 —— `LoadAsync` 换文档时只清 `errorMessage`，**不清 `ofdFailed`/`ofdErrorMessage`**（只有 `OnParametersSet` 清），粘滞失败态把新文档一进门就算成 Error；来路 b：失败态下点一次「最大化」—— 重挂闸门只认「同一份文档 + 同一个最大化态」（`ElViewerGrow.razor.cs:1081`），尺寸一变就放行，可那一帧根本没有挂载点。**修法各自最小、都不动闸门语义**：(a) `razor.cs:1343-1356` 记下 `previousDocument`，`current` 引用一变就清粘滞失败态（换文档＝给内置通道一次新机会，与 `OnParametersSet` 同口径）；(b) `razor.cs:1095-1110` 闸门之后、挂载之前加前置分支 —— 状态是 `Error`/`Unsupported` 就**这一轮不挂**，清失败态、`ResolveStatus()` 重算、`StateHasChanged()` 让渲染把挂载点画回来，下一帧 `OnAfterRenderAsync` 自然重试；**例外**：`ofdFailed` 且 `ofdErrorMessage` 为空＝通道本身取不到（宿主没引 `dom.js`／核心脚本没拿到）⇒「不支持」是终态，清了会渲染—挂载地空转，所以不清也不重试。**有界性钉成断言**（重试只在同一文档+同一尺寸放行一次，`hostPresentAtMount` 恰好 2 条）。
  - **真身二（注释层，vendor 补丁 P0b/P5/P6/P7）**：OFD-H 的 `Document.xml` 把注释清单写成**包内绝对路径** `/Doc_0/Annots/Annotations.xml`，而 JSZip 条目名没有前导斜杠 ⇒ 上游按字面查 `zip.files[...]` 查不到 ⇒ **整层无声丢弃**，不画批注也不报错（与批次99 缺陷四 `replace("/","")` 削错斜杠同类）。即便读进来，`loadPageAnnot` 的数据袋只留 `{type, appearance, visible}`，标准里的 `Creator`/`LastModDate`/`Remark`（＝用户说的「交互信息」）在解析阶段就丢了。`Temp/b100-vendor-patch.py` 四对锚点各断言「恰好命中 1 次」（上游覆盖回来或脚本重跑都当场炸）：P0b 头注释声明本批加了什么、重放顺序 `Temp/b99-vendor-patch.py` **再** `Temp/b100-vendor-patch.py`；P5 `a&&(a=String(a).replace(/^\/+/,"")` 只削**前导**斜杠，绝对/相对路径都查得到；P6 `remark:i["ofd:Remark"],creator:i["@_Creator"]` 把 `remark/creator/lastMod/annotId` 带进数据袋；P7 `a.setAttribute("data-ofd-annot","1")` 给 wrapper 挂 `data-ofd-annot`/`-type`/`-id`，并把 `Creator / LastModDate` + 换行 + `Remark` 写进 `title`。**刻意的取舍（已记档）**：不做页边 (i) 角标 —— 上游没有页边注栏的概念，凭空加不属于版面的图标会改用户看到的版面；代价是不悬停就发现不了批注，但 `data-ofd-annot` 常驻 DOM，探针与宿主脚本都数得到。
  - **字节指纹**：上游原版 926,580 B / `bced7f3f…` ⇒ 批次99（P0–P4）927,195 B / `8b9ea235…` ⇒ **本批（P0–P7）927,798 B / `b596eb7541688ac58eed31b130090fb3`**（LF 69 / CR 0 未变，`.gitattributes` 的 `-text` 标照旧有效，`node --check` 通过）。守卫 `Guard_OfdCore_CarriesTheLocalPatchAndIsBytePinned`（`ElViewerGrowTests.cs:2602`）现在钉长度 + MD5 + **九条**锚点各在 + **六条**各恰好 1 次；**天然翻面档＝批次99 版**（`Temp/b99-core-patched-ship-backup.js` 放回 vendor 路径）⇒ 红在长度钉 `Expected: 927798 / Actual: 927195`（`ElViewerGrowTests.cs:2608`，`Temp/b100-guard-ofd-flip-b99core-1.log`），还原 `cp` + `md5sum -c`（`OK`）+ `cmp` 三重；另外直接数过：三条新锚点在批次99 版命中 **0** 次、现档各 **1** 次，长度钉被绕过也还有锚点钉。
  - **证据三层，每层都带翻面档**：① **bUnit**（判据统一为「挂载调用发生的那一刻，挂载点在不在渲染树里」，`Markup` 口径）—— 本组件 **184/184 绿**（`Temp/b100-bunit-full-2.log`），新增 3 例：`Viewer_OfdImperativeLoadAfterFailure_MountsOnlyWhileTheHostIsInTheDocument`（`:2182`，命令式 `LoadBytesAsync` 换文档；**翻面 M2**＝去掉 (a) 的清空 ⇒ 红）、`Viewer_OfdMount_WaitsForTheHostWhenTheFrameHasNoCanvas`（`:2245`）、`Viewer_OfdRemountAfterMaximizeToggle_NeverMountsIntoAFrameWithoutTheHost`（`:2282`，来路 b + 重试有界 + 真原因 `boom` 不被「容器为空」顶掉；**翻面 M1**＝把 (b) 写成 `if (false && …)` ⇒ `Assert.All() Failure: 1 out of 2 items… [1]: Item: False`，`Temp/b100-flip-M1-witness-1.log`）。② **一次性浏览器台账**（`Temp/b100-nullhost2.cjs`，用 `context.route` 给 `dom.js` 响应体追加插桩包 `elementOfdViewerMount`/`Dispose`，每次调用记 `containerNull/containerConnected/hostsInDocument/file/result`，落 sessionStorage；interop 不在调用点重读 `window.*`，所以插桩只能搭在脚本字节上 —— **常驻守卫不依赖它**）：修好后 **12/12、`nullOnMount=0`**、坏文件之后那份当场画 3 页（`Temp/b100-nullhost2-after-2.log`）；同档去掉 (b) ⇒ **3 条 FAIL、`nullOnMount=1`**，页面文案在 `parse failed` 之后紧跟 `container is null`＝用户那行栈一字不差复现（`Temp/b100-nullhost2-flipM1-1.log`）；报障时那版真实挂载里就有 `containerNull=true`（`Temp/b100-nullhost-before-1.log`）。③ **常驻探针 `demo/tools/probes/verify-ofd.cjs` 46 → 61 项**（新增 §12 注释层与 §13 两条序列；§13 **不插桩**，只看用户可见事实）：修后 **61/61 绿**（`Temp/b100-verify-ofd-after-2.log`），M1 档 **60/61**，唯一红「失败态改宽度重挂时没有把 null 容器交给 JS」的 detail 自带证人 `hasNull=true hasParseFailed=false pagesShown=false tail="…dotnet.native.rw4kynp763.wasm:wasm-function[256]"`（`Temp/b100-verify-ofd-flipM1-1/2.log`）。注释层判据：`[data-ofd-annot]` 恰好 1、`type/id/尺寸` = `Path / 11 / 182x86`、`title` 同含 `王芳（质控医师）`+`2026-10-23`+`病历质量审核…`、印章红 `rgb(183, 28, 28)` 命中 >0 **且带同色注入的有牙证人**（`red=1 → redWithWitness=2`）；读数全部来自现档样例离线复核 `Temp/b100-fixture-annot-1.log`（3 页 / 1392 字 / 88 path / 173 svg / annotCount 1 —— 与批次99 的 1367/87/169 差在注释层，属补丁应有的变化，先判性质再改判据）。
  - **全量归因**：**失败 1 / 通过 2814 / 总计 2815**（`Temp/b100-bunit-full-3.log`），唯一红仍是既有基线 `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows`（批次99 `Temp/b99-full-bunit-1.log` 与 2026-10-21 `Temp/b103_test_full.log` 里同样红着这一条）；对比批次99 的 2812 总计 **+3** ＝本批三条新挂载判据，零新增红。
  - **出库（0.10.29 → 0.10.30，NuGet + 发布包重发）**：**先说版本号为什么不是 28**——并发会话在 2026-10-23 15:04 / 16:01 已打出 `Element-Plus.0.10.28/0.10.29.nupkg`，`Element.csproj` 现值 `0.10.29`；拆开实测这两份的 OFD 核心**已是** `b596eb75…`（静态资源按磁盘现档收，vendor 补丁当时已落盘），但 `Element.dll` md5（`18e0bd37…` / `e2998e5b…`）≠ 本批 Release 产物（`1c739256…`）⇒ **里面没有本批两处 C# 挂载修复**，所以占 0.10.30、不动别人的号。**发包只从磁盘现档构建**：`build -c Release --no-incremental` **0 警 0 错**（`Temp/b100-release-build-1.log`）→ `pack -c Release -o nuget --no-build` RC=0 → `Element-Plus.0.10.30.nupkg` **1,795,465 B** + `.snupkg` 683,959 B。核验 `Temp/b100-nupkg-verify.py` → `-1.log`（RESULT OK / 0 fail）：nuspec `Element-Plus` / `0.10.30` / `Smart`；包内核心 927,798 / `b596eb75…` **与磁盘逐字节相同**、九条锚点各 1 次；`lib/net10.0/Element.dll` 3,046,400 B md5 `1c73925650fa2b7b2613639392672995` **== Release 磁盘产物**；UTF-16LE 版本串 `0.10.30` 3 处、`0.10.29` / `0.10.28` / `0.10.27` **各 0 处**。诚实记一笔：这颗 dll 里有批次98 的 `SuppressFormWriteBack` ⇒ **包版本＝整棵工作树**，并发会话未提交的改动一并进了包（本仓一贯口径，但包版本不等于单批次）。发布包：旧 zip 改名留证（`Temp/b100-release-old-package.zip`，md5 `7bd3cfa5…`＝批次99 交付那份，`cmp` 与 `artifacts/` 里那份逐字节相同才删），确认 5199 / 5203 / 5204 都没握着该目录后**整份删** `artifacts/demo-release` 再 `publish -c Release -o`（批次㉗ 的坑）；新档 390 个文件、`_framework` 四颗程序集**各只有一个**内容哈希（`Element` = `mb0hko8yoh`）⇒ 无过期程序集残留。三份同源（`Temp/b100-release-verify.py` + `.cjs` 两条都 RESULT OK）：未压缩 / `.gz` 解回 / `.br` 解回**全是 `b596eb75…`**，HTTP 侧 `curl` 与 `curl --compressed -H 'Accept-Encoding: gzip, br'` 落盘同档。**静态托管复验（5203）＝ OFD 61/61、启动自检 10/10、站内 vendor 无公网 CDN 23/23、Viewer 交互 20/20，控制台 0 错误**，与开发站逐条同读数（3 页 / 1392 字 / 88 path / 173 svg / 1 插图 / 注释层 `count=1 id=11 182x86` / `redHits=1` 且同色证人 `1→2`），§13 两条序列在发布包里全部 `hasNull=false`（垃圾文件那两帧 `hosts=0` 仍落 `parse failed`，最大化后也是）。交付物 `artifacts/Element.Blazor.Demo-release.zip` **390 文件 / 23,300,433 B / md5 `903a0297ced129b7158a624a79856e41`**；⚠️ 这里差点出厂一次**形状漂移**：第一版只打了 `wwwroot/` 子树，而上一批的包是**整个 publish 目录**（少掉 IIS 要的 `web.config` / `dotnet.js`）——重打包要先拆上一批的包看布局，别按「应该长什么样」打；纠正后与旧包文件名集合差异 24 条，**全部**是 `_framework` 内容哈希改名（样例与根级文件一条不多一条不少）。尺子自身的两条坑：本机 **Python `zlib` 没有 `brotli_decompress`**（AttributeError，`Temp/b100-release-verify-1a-python-traceback.log` 留原样），压缩档这一腿要交给 Node 的 `brotliDecompressSync`；`console.log('%-20s', x)` 在 Node 里**不是**格式化占位，会原样打出 `%-20s`（看着像没生效，其实压根没格式化）。详见 `docs/viewer-alignment.md` §12.6。
  - 缺陷链、来路表、补丁锚点表、字节指纹表与三层证据见 `docs/viewer-alignment.md` §12；「上一帧就被移除的元素」这类只在 JS 侧可见的缺陷怎么加判据见 `docs/testing-guards.md` §3.36 与 §4 第 108–111 条。

- 🟢 **批次99 · 真身缺陷（改了库内 vendor）：`ElViewerGrow` 打不开 GB/T 48666—2026 口径的 OFD-H 病历文件（用户原文件 `住院病案首页_OFD-H.ofd`）—— 站内渲染核心 ofd.js 0.2.6 读不了三种规范允许的写法，本地补丁 P1–P4 补上容错（随包 0.10.27）**（2026-10-23，#164）。
  - **报障与定位**：「ElViewerGrow 打不开这个 ofd 文件…这个文件是根据这个标准创建的」。三条偏差全部**实测**在解包 XML 与核心代码上（不是推理）：① `<ofd:Signatures Loc="Doc_0/Signs/Signatures.xml" />` 把清单路径写在**属性**里，`getSignature` 拿对象调 `indexOf` ⇒ 整份文档 `TypeError: t.indexOf is not a function`，一页都出不来；② 签名层/注释层的 XML 用**默认命名空间**（元素没有 `ofd:` 前缀），而核心按字面键 `json["ofd:Signatures"]` 取值 ⇒ 取不到（另一份样例 `modern-info-card.ofd` 因此在 `OFD.xml` 就崩：`Cannot read properties of undefined (reading 'ofd:DocBody')`）；③ 页面里 **87 个 `PathObject` 没有 `Boundary`**（坐标本身是绝对页面坐标），`renderPathObject` 无条件 `converterBox(parseStBox(null))` 抛错。第四条顺带挖出：`SignedValue` 的包内相对路径被 `s.toString().replace("/", "")` 削掉**第一个**斜杠（JS 字符串 `replace` 不锚定行首），zip 查不到 ⇒ 整个签名层丢失。**标准侧口径**：GB/T 48666—2026 §6.2 对元素前缀用「宜」、§9.2 a) 把版式还原判给阅读器 ⇒ 这是 viewer 容错不足，不是文件坏了。
  - **修法（vendor 文件打补丁，可从上游原字节一键重放）**：`Temp/b99-vendor-patch.py` 五对锚点各断言「恰好命中 1 次」，上游一旦覆盖回来会当场炸在脚本里而不是静默退回旧病。P1a 从 `@_Loc`/`@_BaseLoc` 取路径；P1b/P1c 给签名层整段 `try/catch`——**签名读不出来不许否决版面还原**，只 warn；P2 解析后递归给非属性、非 `#text`、不含 `:` 的键补 `ofd:` 前缀（带前缀的文档逐字节不变）；P3 无 `Boundary` 时退化成页面原点零号盒（`<svg>` 是 `overflow:visible`，路径按自身绝对坐标照画）；P4 只削**前导**斜杠（正则锚定行首）。文件头加 P0 注释声明「上游 + 本地补丁」并重放路径。上游原版 926,580 B / `bced7f3fc396769578b22a173d48705d` ⇒ 补丁版 **927,195 B / `8b9ea235b23500bec6ff664ed1166649`**（LF 69 / CR 0，`node --check` 通过）。
  - **检出层**：`.gitattributes` 给这个文件单独标 `-text`（批次㉙ 同款理由：本机 `core.autocrlf=true` + `* text=auto` 会在检出时把 69 个 LF 换成 CRLF，全新克隆里 MD5 判据会假红）。`VendoredBytesStayExactOnCheckout` 加它的 `InlineData`，`GuardedVendoredFileActuallyHasLfEndings` 由 `Fact` 换 `Theory` 同时给 hls 与这份核心当**前提证人**；`修法` 提示文案原来硬编码 demo vendor 目录，改成引用 `relative` 自身。
  - **守卫**：新增 `ElViewerGrowTests.Guard_OfdCore_CarriesTheLocalPatchAndIsBytePinned`（长度 + MD5 + 六条锚点 + 三条锚点各恰好 1 次）。**翻面实测**：把上游原版 `Temp/b99-shipped-core-pristine.js` 放回 vendor 路径 ⇒ **失败 1 / 通过 8**（红在长度钉 `ElViewerGrowTests.cs:2449`，`Temp/b99-guard-ofd-flip-pristine-1.log`）；换回补丁版 ⇒ **9/9 绿**（`Temp/b99-guard-ofd-run-2.log`）。还原用 `cp` + `md5sum -c`（`OK`）+ `cmp` 三重，未用 `git checkout --`。
  - **离线 A/B（同一台无头浏览器，两个核心 × 7 份样例，`Temp/b99-ofd-parity-3-ship.log`）**：5 份样例两核心**逐字相同**（`out` 1 页 / `font-styles` 2 页 / `MultiSpanStyles` 1 页 / `multi_demo` 3 页 / `hcview` 1 页·155 字·2448 path·2512 svg）＝补丁对已能开的文档零影响；`modern-info-card` 失败→1 页·25 svg·12 path；用户的 `住院病案首页_OFD-H.ofd`（11,838,554 B）失败→**3 页 / 1367 字 / 87 path / 169 svg / 1 插图**，且**没有** `signature layer skipped`（P1b 的 `catch` 没被走到）。
  - **在线验收（真实浏览器，demo 起在 5199，常驻探针 `demo/tools/probes/verify-ofd.cjs` 25 → 46 项）**：**改后 46/46、控制台 0 错误**（`Temp/b99-verify-ofd-after-3.log`）；**改前 35/46**（`Temp/b99-verify-ofd-before-2.log`），11 条红全部落在 OFD-H 两段，页面上直接读到 `渲染失败：elementOfdViewer: parse failed - TypeError: t.indexOf is not a function`；基线段 1–8 与护理记录单档**两档同数全绿**＝红是被测文档特有，不是会话状态。新判据一律量**几何与对象数**（页数 / 非空白字数 / path·svg·img 计数 / 页幅比例 210:297 与 297:210 / 内容包围盒覆盖大半页且不出页），不只判「没抛异常」；负向判据（`signature layer skipped` 不出现）前先注入 `ofd: probe-ruler-witness` 证明消息台账看得见同类前缀。演示站新增两份样例：`/ofd/ofd-h-case-page.ofd.bin`（用户那份 11.8 MB 的**等观感副本** 90,720 B，两份 10.5 MB 仿宋字稿各截 4096 字节头、内容为虚构演示数据；削完三种偏差一个没少，离线读数与原文件逐字相同，`Temp/b99-slim-parity-1.log`）与 `/ofd/hcview.ofd.bin`（16,793 B，用户那份原样）。
  - **探针自身三处尺子错（改前档才暴露，全部记档）**：① 样例路径写成仓库里从来不存在的 `probes/Element.Blazor.Demo/...` ⇒ `existsSync` 一判就跳过，那六条判据**一次也没跑过**（§4 第 104 条：分支条件不是判据）；② 分批建的 `/grow` 上用一次性 `count()` 当判据 ⇒ `FAIL …入口 | inputs=1` 这种自相矛盾的假红（§4 第 105 条：同一个数只取一次，判据与 detail 同源）；③ `page.evaluate` 里把 `getBoundingClientRect()` 当元素用 + 失败态没有 `.el-viewer-grow__ofd` 时 `scrollIntoViewIfNeeded()` 30 秒超时把探针崩成 FATAL，后面对照段一起没跑（§4 第 106 条：改前档要假设被测 DOM 整块不在，缺失时打可见的 SKIP）。
  - **生成器侧（记档，未动对端仓库）**：**NetOfd**（`E:\电子病历\netofd\src\NetOfd.Sign\SignatureModels.cs:211` 写 `DocBody/Signatures@Loc`）三条偏差建议改规范写法；它的 `SignedValue.dat`（917 B）实测是 ASN.1 SEQUENCE + OID `1.2.156.10197.6.1.4.2.2`、全包 0 个 `PK` 偏移 ⇒ GB/T 35275 的 SM2 签名报文，**没有内嵌 SES 印章图**（`SignatureMethod` 1.2.156.10197.1.501，Provider `NetOfd.Sign`）⇒ 「电子印章」在数据层就无物可画，不是 viewer 漏画。**HCView 6.8**：每个 XML 带 UTF-8 BOM、命名空间写成 `http://www.ofdspec.org`（缺 `/2016`）、机读附件 `Attachs/emr_xml.xml` 是 **0 字节**、无 `Attachments.xml`、无签名层、声明宋体未内嵌字稿——版面本批前后都能开（1 页 / 155 字，155 与从它自己 XML 数出的字数相同），但机读附件为空对 OFD-H 是实质缺陷。用户给的公网在线阅读地址（ofdreader.net）只当参照实现：演示站与病历数据都不走它（内网 http 部署 ⇒ 第三方库必须落站内 vendor；患者资料不外传）。
  - **全量归因**：**失败 1 / 通过 2811 / 总计 2812**（`Temp/b99-full-bunit-1.log`），唯一红仍是 testing-guards §1 记档的既有基线 `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows`；对比批次98 的 2809 总计 **+3** ＝本批新判据 1 + 判据④ 新 `InlineData` 1 + LF 证人换 `Theory` 多 1，零新增红。
  - **出库（0.10.26 → 0.10.27）**：`dotnet build -c Release --no-incremental` **0 警 0 错**（`Temp/b99-release-build-1.log`）→ `dotnet pack -o nuget` RC=0 → `Element-Plus.0.10.27.nupkg`（1,794,877 B）+ `.snupkg`（683,873 B）。核验（`Temp/b99-nupkg-verify.py` → `-1.log`）：nuspec id/version/authors = `Element-Plus` / `0.10.27` / `Smart`；包内 `staticwebassets/js/ofd/ofd.umd.min.js` **927,195 B / `8b9ea235…` 与磁盘逐字节相同**，六条补丁标记各 1 处；`lib/net10.0/Element.dll` 3,046,400 B md5 `8839531cf256cfd6e7110ba73bdcbf06` == Release 产物，UTF-16LE 版本串 `0.10.27` 3 处 / `0.10.26` 0 处。
  - **发布包重打 + 浏览器复验（真静态托管那一层，用户实际部署的形态）**：先把 `artifacts/demo-release` **整份删掉**再
    `dotnet publish -c Release -o`（`publish -o` 不清空是批次㉗ 记过的坑），旧 zip 改名留证
    （`Temp/b99-release-old-package.zip`，md5 `aff83b3e6883905526807ee10de0dd55`）。新包 **390 个文件**（= 旧包 388 + 两份新样例），
    `_framework` 里 demo 程序集只剩一份 hash `cm2jtyky76` ⇒ 无过期程序集残留；包内核心未压缩 927,195 / `.gz` 252,967 / `.br` 199,676，
    **两份压缩档解回原始字节后 md5 同为 `8b9ea235…`**（`Temp/b99-release-verify-1.log`），HTTP 侧 `curl` 与 `curl --compressed` 同档。
    **翻面档＝重发前那份包**（里面还是上游原版 `bced7f3f…`）：同一支探针 **29/41**（12 红，`Temp/b99-release-probe-before-1.log`）
    ⇒ 新包 **46/46、控制台 0 错误**（`Temp/b99-release-probe-after-1.log`），且与开发站**逐条同读数**；
    归因：改前那 12 条 = 开发站改前的 11 条 + 1 条「旧包压根没有护理记录单样例」（它下游 4 条整段没跑，总数才 46→41），
    **样例缺失与产品缺陷分开记账**。同包回归：启动自检 **10/10**、站内 vendor 无公网 CDN **23/23**、Viewer 交互 **20/20**。
    交付物 `artifacts/Element.Blazor.Demo-release.zip` **23,081,228 B** / md5 `7bd3cfa58519d16a893a9d3fa6332160`。
  - 补丁表、A/B 读数表与标准口径见 `docs/viewer-alignment.md` §11；守卫与尺子课见 `docs/testing-guards.md` §3.35 与 §4 第 104–107 条。

- 🟢 **批次98 · 真身缺陷（消费方裁定「改库根治」）：`TValue=string` 的 `ElSelect` 放进 `ElFormItem<string>` 时，把选项**文本**当字段值写进业务模型 —— 新增 `ElInput.SuppressFormWriteBack` 把「显示框」与「字段写回」彻底分离（随包 0.10.26）**（2026-10-22）。
  - **缺陷链（浏览器 C# 堆栈实抓，非推理）**：`ElSelect.razor:54` 的显示区是一颗 `ElInput TValue="string" Value="@DisplayLabel"`，而它就在 `ElFormItem.razor:102` 的 `<CascadingValue Value="this">` 里；`TValue=string` 时内外两层撞成同一个 `ElFormItem<string>`，于是 `ElInput.OnParametersSet` 的「父级改了我的 Value」分支把 **选项汉字** 赋给 `FormItem.Value` → `NotifyValueChanged` → `ElForm.NotifyFieldValueChanged` → `FormModelPath.TrySetValue` 反射写进模型。实测：选完「正常」250ms 后 `Status` 由 `"0"` 变成 `"正常"`，请求线格式 `status=%E6%AD%A3%E5%B8%B8` ⇒ 服务端按 int 解析 ⇒ 0 行；int/枚举下拉不发病是因为级联类型不匹配（只有 `string` 对 `string` 会撞上）。**最狠的一档**：编辑弹窗首帧没人碰过它，`edit.Sex` 已经从 `"0"` 被写成 `"男"`（17:22:18.717 `SEX<- [男]`），保存即 POST 坏数据。证据：`E:\电子病历\netetl\logs\m5-select-label-writeback-before.log`（改前，含调用栈）与 `m5-select-label-writeback-after.log`（改后）。
  - **修法**：`ElInput<TValue>` 新增 `[Parameter] bool SuppressFormWriteBack`（默认 false ⇒ 直接用 `ElInput` 的页面行为逐字节不变）。置 true 时：`OnParametersSet` 在 `FormItem == null` 早退之后整段早退（**两个方向都跳** —— 反向采纳 `FormItem.Value` 会让框里显示裸键 `"0"` 而不是「正常」，`_lastParamValue` 照常跟住）；`ElementFieldComponentBase.SetFieldValue` 改 `protected virtual`，`ElInput` 覆盖为抑制时直接 return（键入 / 失焦 / 清空三条写回路径一起拦住）；`ClearOnClick` 里的 `FormItem.Value = Value` 加 `&& !SuppressFormWriteBack`。`FormItem.Form.RegisterInput(...)` 原样保留 ⇒ `label for` 与 ScrollToField 不塌。字段同步交回 `ElSelect.SelectValueAsync` 唯一一条路。
  - **落点**：`ElSelect.razor:68`、`ElSelectV2.razor:58` 各加 `SuppressFormWriteBack="true"`（两颗都是「显示框喂文本」）。**同款潜在缺陷留档未动**：`ElCascader.razor:40`、`ElTimeSelect.razor:16`、`ElAutocomplete.razor:11` 同样把显示文本喂进 `ElInput TValue="string"`，等各自消费方撞上再改（口径见 `docs/select-alignment.md`）。
  - **守卫**：`test/Element.ComponentTests/ElSelectFormItemWriteBackTests.cs` 4 例 —— 选完两次重渲染后模型仍是 `"0"` **且**触发框仍显示「正常」；预置 `"0"` 首帧不被自己的文本顶掉；可搜索档键入的筛选词不进模型；第 4 例是对照组（同一颗 `ElFormItem<string>` 里直用的 `ElInput` 必须继续写模型）。改前 **失败 3 / 通过 1**（`Temp/b98-writeback-before.log`，三条判据红在 `Expected: "0" Actual: "正常"`），改后 **4/4 绿**（`Temp/b98-writeback-after.log`）。踩过的坑：判据必须用 `.Input(...)` 不能用 `.Change(...)` —— `ElInput` 在 `@oninput` 就写回，`.Change()` 只走 `@onchange`→`OnNativeChangeAsync`，而它仅在 `ValidateEvent=true` 时写（`ElSelect` 传的是 false），于是对照组一度在改前档就绿＝无牙。
  - **全量归因**：**失败 1 / 通过 2808 / 总计 2809**（`Temp/b98-writeback-fullsuite.log`），唯一红仍是 testing-guards §1 记档的既有基线 `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows`；对比批次97 的「失败 1 / 通过 2804 / 总计 2805」＝总计 +4、通过 +4，本批零新增红。
  - **出库（0.10.25 → 0.10.26）**：`build -c Release --no-incremental` **0 警 0 错**（`Temp/b98-release-build.log`）→ `pack -o nuget` RC=0 → `Element-Plus.0.10.26.nupkg`（1,794,570 B，md5 `41d4b1c9685ae2543e1dde2667a28429`）。核验 `Temp/b98-nupkg-verify.py` → `.log`：nuspec version 0.10.26；包内 `lib/net10.0/Element.dll` 3,046,400 B md5 `568df26c71f76f455a846245a00dc13c` 与磁盘产物 same=True；#Strings `SuppressFormWriteBack` 3 / `get_` 1 / `set_` 1；dll 内 `0.10.26` 2 处、`0.10.25` 0 处。消费侧换包（`Element-Plus` 0.10.23→0.10.26，clean+build）后**服务侧字节验**：`/_framework/Element.102ma75rap.wasm` HTTP 200 与 `obj/webcil` 同为 3,046,169 B、md5 `3dd465dd64036979dde2faac7c3aba34`、marker 3 处 ⇒ 浏览器加载的确实是打了补丁那一份。
  - **消费方实测**（netetl `/sys/user`，真实点击）：选「正常」⇒ 请求 `...&status=0`、10 行；切「停用」⇒ `...&status=1`、0 行 + 空态；再切回「正常」⇒ 10 行（切换双向都对，不是只有首侧对）；编辑弹窗首帧 `Sex` 保持 `"0"`、框里仍是「男」；对照组（同一颗 FormItem 里直用的账号框）键入 "wang" 仍能过滤出 2 行。

- 🟢 **批次97 · 新组件 `ElSplitButton` / `ElSplitButtonItem`：RadzenSplitButton 全功能面 × Element 渲染（用户指令「通过 E:\电子病历\radzen-blazor-master\Radzen.Blazor 中的 RadzenSplitButton，新建一个组件命名为 ElSplitButton，Element风格渲染，实现全功能，添加到demo里面的button里面，并打一下nuget包」）**（2026-10-22，随包 0.10.25）。
  - **功能映射**：主按钮 `Click(null)`、菜单项回传 `ElSplitButtonItem`（Text/Value/Icon/IconColor/Disabled）、`AlwaysOpenPopup`（主按钮只切菜单不触发 Click）、`IsBusy`/`BusyText`（`IsDisabled = Disabled || IsBusy`，演示页 1.5s 定时恢复）、程序化 `ShowDropDownAsync()`/`Close()`/`CloseAsync()`、全套键盘（箭头 ↓↑ 展开焦点落首/末启用项；菜单内 ↓↑ 跳禁用+clamp、Home/End、Enter/Space 激活、Esc/Tab 收起回焦箭头、首字母定位）；渲染＝`el-button-group` 两颗 ElButton + 箭头挂 `el-dropdown__caret-button`（默认类型补 `el-button--default` 出灰分隔线、`border-left:0`），菜单走 `ElPopup` 宿主统一渲染（`role=menu`、`bottom-start`、36px 行高、禁用项 `pointer-events:none`、点外 document 捕获收起）。映射表在 **`docs/split-button-alignment.md`**。
  - **真身缺陷（本批修）**：`ElPopup.CloseDropDownAsync` 把 `IsShow` 在关闭动画一开始就置 false 而 option 要等 210ms 尾巴才清——toggle 判据 `dropDownOption != null` 在窗口期吞点击（探针检查 21「Esc 后 Space 开不出」确定性红），`OnClosed` 不按 option 识别会清掉重开后的新 option（检查 28 菜单残留）。修法：`IsOpen => dropDownOption != null && dropDownOption.IsShow` ＋ 开启前 `DiscardClosingOption()` ＋ option-aware `OnClosed`；修后 **28/28 两连绿、JS 异常 0**（`Temp/b97-split-verify-5/6.log`，修前 26/28 与取证档 `Temp/b97-split-verify-4.log` 同在 Temp/）；bUnit 回归锁 `CaretClickDuringClosingWindowReopens`（吞点击与清新 option 修反任意一处都红）。
  - **验证**：常驻探针 `demo/tools/probes/verify-split-button.cjs` **28 项**（主目录探针份数 **256 → 257**）；bUnit `ElSplitButtonTests` **27 例全绿**（`Temp/b97-bunit-split-2.log`；宿主渲染 ElPopup ⇒ 必须 stub `getBoundingClientRect`，Loose 模式 null 会在 `ElPopup.razor.cs:853` NRE）；全量 **失败 1 / 通过 2804 / 总计 2805**（`Temp/b97-bunit-full.log`，唯一红仍是 testing-guards §1 记档的既有基线，本批零新增红）。
  - **演示**：`/button`「ElSplitButton 分裂按钮」六段——基础、类型/朴素/圆角、图标（含图片与 IconColor）、禁用/忙碌（含全部项禁用）、AlwaysOpenPopup、程序收起（`@ref` + `Close()`）。
  - **出库（0.10.24 → 0.10.25）**：`build -c Release --no-incremental` 0 警 0 错（`Temp/b97-release-build.log`）→ `pack -o nuget` RC=0 → `Element-Plus.0.10.25.nupkg`（1,793,986 B，md5 `488699f72e0e67696c5ee580ef1c3610`）+ `.snupkg`。核验（`Temp/b97-nupkg-verify.py` → `.log`）：dll（3,045,888 B）UTF-16LE `_content/Element-Plus` **4 处**==源码、旧前缀 **0 处**、`ElSplitButton` ×17（ASCII 元数据堆）；`el-dropdown__caret-button` ×6 / `el-button-group` ×49；css/dom.js 包内 md5 == 源码。尺子教训：**类型名查 ASCII #Strings、字面量查 UTF-16LE 字节模式**——整档 `decode('utf-16-le')` 只对齐偶偏移，首轮把 4 处数成 1、类型名数成 0（§4 第 101 条）。
- 🟢 **批次96 · `ElDropDownTableGrow` 面板**装回**一根可拖的横向滚条：静止 6px、悬停 12px、移出收回、按住拖动全程 12px；纵轴仍是 0（消费方在批次94 收尾把这一档单独挂出来问，回答一个字「需要」）**（2026-10-21，随包 0.10.24；本批零业务 C# 改动，交付面是组件 `<style>` ＋ 演示对象 ＋ 守卫/探针/文档）。
  - **这是对批次⑧「面板滚道 4px → 0（能去就去）」那次用户批准例外的局部反转，反转面只到横轴**：Chromium 的 `::-webkit-scrollbar` 里 `height` 只管横向那条、吃的是 `clientHeight`，不碰 `clientWidth` ⇒ 加高不移动任何列边界；`width` 只管纵向那条、吃的是 `clientWidth`，表体是滚动容器而表头是 `overflow:hidden`，两侧槽宽一旦不等列就从右往左越偏越多（批次⑧ 的取证档 `Temp/diag-batch8-scrollbar-zero2.log` 钉过这条依赖）。所以横轴按库口径装回，纵轴**一个像素都不给**。
  - **三条规则都写在组件自己的 `<style>` 里**（`src/Components/ElDropDownTableGrow.razor:47-63`）：表体 `{ width:0; height:var(--el-sb-h, 6px); }`、表头 `{ width:0; height:0; }`、表体悬停 `{ height:var(--el-sb-h-hover, 12px); }`。第三条最容易漏：组件静止档的选择器特异性 (0,2,0) 且样式表比 `index.css` 更晚，压得住全局 `*:hover::-webkit-scrollbar`（0,1,0）—— 压住全局加高的同时，也把全站统一的 12px 挡在了外面，不在这里补一条 hover 档，面板横条会永远停在 6px。**凡是自己声明静止档的宿主，都要自带悬停档。**
  - **表头为什么是 0×0 而不是 0×6**：`index.css` 里实测到两处 `.el-table__header-wrapper{overflow:hidden}`，它没有可横向滚的内容，6px 只会 ① 画一条拖不动的条、② 从表头底部白吃 6px 高度、③ 让表头/表体的可视高度不相等。探针 ⑬h/⑭h 把三点量成读数：表头静止 `{hBar:0, cssH:"0px"}`，表体悬停时表头仍 `0×0` 且 `hover:false`（加高不外溢）。
  - **演示对象**：该面板原本没有横向溢出（`sw==cw==338`），「装回横条」在那台上量不到任何东西。`/grow` 因此补了一台六列 660px 塞进 360px 面板的下拉（`demo/Element.Blazor.Demo/Pages/GrowDemo.razor`，在 `@if (builtSections > 9)` 那段之内，不新增 `.demo-section` ⇒ `GrowSectionGateGuardTests` 的章节闸门数仍然对得上），横条从此有可拖的对象。
  - **常驻探针 17 → 30 项**：`demo/tools/probes/verify-scrollbar-hover-grow.cjs` 新增 ⑫–⑳ /grow 面板段，**30/30 PASS、JS 异常 0、RC=0**（`Temp/b56-hover-grow-2.log`）。关键读数：⑫ 前提证人 `{n:12, found:true, idx:5, panel:{sw:660, cw:342, cols:7}}`（不成立则⑬–⑳ 全部作废）；⑬ 静止 `{hBar:6, cssH:"6px", vSlot:0, cssW:"0px"}`；⑭ 悬停 `{hBar:12, cssH:"12px", cssW:"0px", vSlot:0, hover:true}`（正好一倍，纵轴没动）；⑯ 移出 `{hBar:6, cssH:"6px", hover:false}`；⑰ 按住拖动 `{press:12, mid:12, end:12}`；⑱ 真推动内容 `scrollLeft 0->318 Δ=318`；⑮r/⑮h 墨带 `4 → 11` 行（严格增，不是两个死数字）；⑬c/⑭c `th`/`td` 逐列左边界差与相对静止档位移全 0；⑳ 无溢出对照组 `{sw:338, cw:338, hBar:0, vSlot:0, cssH:"6px"}`（声明 6px 但零 DOM 变化）＋「找不到这种面板＝这一档没跑」的防空转证人。
  - **邻档回归（同一台 dev server）**：`verify-scrollbar-wide-h.cjs` **8/8**（`Temp/b56-verify-scrollbar-wide-h-1.log`，`{vSlot:4, hBar:6}`、九列逐列全等）、`verify-scrollbar-thumb.cjs` **7/7**（`Temp/b56-verify-scrollbar-thumb-1.log`）、`verify-scrollbar-fade-scroll-race.cjs` **8/8**（`Temp/b56-verify-scrollbar-fade-scroll-race-1.log`，`repaint` 那档 `held:true`）。
  - **浏览器层两档单点变异（每档只改一处，从当前好档 `262b5e24…` 出发，每档重新构建重启）**：**MB1 删掉组件自己的悬停档**（md5→`ad132555…`）⇒ **26/30**、④ 条红（⑭ 悬停仍 `hBar:6`、⑮h `bandAbove:4` 不再严格增、⑰ `press/mid/end=6`、⑱ `Δ0`），而 `verify-batch8` 仍 **17/17**（证明删横条加高不碰纵轴）；**MB2 把表体 `width:0` 改成 `4px`**（md5→`d6ee56ce…`）⇒ **27/30**、③ 条红（⑬ `vSlot:4 cssW:"4px" cw:342→338`、⑭ `vSlot:4`、⑳ 对照组 `vSlot:4`），`verify-batch8` **14/17、G1/G2/G3 全红**（G1 表体/表头槽宽 `4/0`、G2 逐列差 `0.9/2.9/4`、G3 `restoreSlot:[4,0]`）。还原用 `cp Temp/b56-grow-razor-good.razor` ＋ `md5sum -c Temp/b56-good.md5` → **4/4 OK**（未用 `git checkout --`），重建重启后 **30/30** ＋ **17/17**（`Temp/b56-verify-scrollbar-hover-grow-RESTORE-1.log`、`Temp/b56-verify-batch8-RESTORE-1.log`）。
  - **守卫**：`ScrollbarCaliberGuardTests` 6 → **7** 例，新增 `Guard_GrowDropDownPanel_HorizontalBarUsesLibraryCaliber_VerticalSlotStaysZero`（`test/Element.ComponentTests/ScrollbarCaliberGuardTests.cs:193`），钉四件事：① 表体/表头 `width` 都必须是 0（谁改回 4px 就红）；② 静止/悬停值必须与 `index.css` 全局同数（不是各写一份字面数字）＋ `Assert.Equal(2 * restH, hoverH)` 钉住「一倍」；③ 两边挂同两个钩子 `--el-sb-h` / `--el-sb-h-hover`；④ 悬停档不许出现 `width`、表头档 `height` 必须是 0。**目标守卫 7/7 绿**（`Temp/b56-guard-mut-after.log`）；S1–S6 六档单点变异每档 `失败 1 / 通过 6`（逐档见 `docs/testing-guards.md` §3.33）。
  - **另一支守卫换代（改尺子，不是放宽断言）**：`ElDropDownTableGrowTests.Guard_PanelScrollbarGutterIsOneSharedEqualWidthRule` 原来钉「表头/表体共用一条同时列两个选择器的规则」，本批的竖切拆成两条规则后旧断言必然红；但它要守的语义（两侧槽宽都 0 且相等）没变 ⇒ 改成逐选择器采集：各自**恰好声明一次** `width`、值都为 0、两边相等，同时不限定规则条数。改后 **失败 0 / 通过 38 / 总计 38**（`Temp/b56-growtests-fixed-1.log`）。
  - **全量归因**：**失败 1 / 通过 2777 / 总计 2778**（`Temp/b96-bunit-full-1.log`），唯一红是既有基线 `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows`；对比批次95 的「失败 2 / 2775 / 2777」：那条 gutter 尺子债本批还清，故红 −1、通过 +2、总计 +1。`dotnet test` 重建 Debug 产物之后再跑一次常驻探针仍 **30/30 PASS、JS 异常 0**（`Temp/b56-post-test-1.log`）。文档改完后三支守卫独立复跑（RC 均为 0）：编码守卫 **4/4**（`Temp/b96-enc-only.log`）、滚动条守卫 **7/7**（`Temp/b96-guard-only.log`）、grow **38/38**（`Temp/b96-docsync-guards.log`）。
  - **出库校验**：`nuget/Element-Plus.0.10.24.nupkg` 内层 `lib/net10.0/Element.dll`（UTF-16LE 字符串）里能逐字读到本批三条规则 —— `.el-table__body-wrapper::-webkit-scrollbar { width: 0; height: var(--el-sb-h, 6px);` 与 `:hover::-webkit-scrollbar { height: var(--el-sb-h-hover, 12px);`、表头 `height: 0` ⇒ 横条加高**已随 0.10.24 在包里**。
  - 口径反转留痕、实测表、三条读数陷阱（表头零矩形假列、静止墨带必须在悬停前截、Node 常量不会自己进页面）与变异表见 `docs/caret-scrollbar-focus-ring-alignment.md` §11；守卫与尺子课见 `docs/testing-guards.md` §3.33。

- 🟢 **批次95 · `Visible` 从「只有 ElButton 认」铺到 `ElRow` / `ElSpace` / `ElProgress` / `ElTabPane`：宿主按状态收整块版式时不必再自己包 `@if`，包漏了也不会留一块常驻空白（消费方原话「组件不完善的话，就修改完善组件 E:\电子病历\Element-Blazor-master」）**（2026-10-22，随包 0.10.24）。
  - **根因（读库源码实抓）**：`ElementComponentBase` 有 `[Parameter(CaptureUnmatchedValues = true)] Attributes`，未声明的属性**不报错**、被当普通 HTML 属性吐到 DOM。0.10.23 及以前全仓只有 `ElButton` / `ElDialog` / `ElDialogGrow` / `ElDrawer` / `ElDropDownTableGrow` / `ElWaitingPopupGrow` 声明过 `Visible`（`ElBacktop` 那个是只读派生值）。于是消费方 `/business/docremake` 的 `<ElRow Visible="@(showstyle==N)">` 三块一直同时渲染（用户看到「整个页面乱了」），`<ElTabPane Visible="@(!u_new)">` 的「归还」页签也一直挂着 —— 编译期零信号，只有实抓 DOM 才看得见。
  - **改法（口径照抄 `ElButton.Visible`）**：四个组件各加 `[Parameter] public bool Visible { get; set; } = true;`，并把整块 markup 包进 `@if (Visible)` ⇒ 语义是**此刻不存在**（连壳带子树都不进渲染树），不是 `display:none`，更不是 `Disabled`。默认 `true` ⇒ 加参数前的用法逐字节不变。
  - **为什么不能用 `display:none`**：① `ElTabPane` 的表头测量（`AcitveTabOnRenderCompletedAsync` 量 `clientWidth`/`offsetLeft`）在隐藏容器里恒得 0，`MaxMeasureRetries = 3` 那道刹车就是批次88 宿主 DocManage 整页卡死留下的，CSS 隐藏等于把死循环的燃料留着；② 收起一块版式的意图本来就不留占位与外边距；③ 与 `Disabled`（还在条上、只是点不动）必须分家，单测里当两条独立判据钉。
  - **页签多做的那半件事**：内容区画的是 `ElTabs` 里的 `ActiveTab?.ChildContent`（`src/Components/ElTabs.razor:22`），页签自己藏掉带不走内容区 ⇒ `ElTabPane.OnParametersSet` 调 `SyncRegistration()`，可见走 `ElTabs.ShowTab`、收起走 `HideTab`；`OnInitializedAsync` 里初始即收起的页签不入集合；`HideTab` 若顶掉的正是激活项，走与 `CloseHardcodedTab` 同一条收尾 `ReactivateAfterRemoval`（顺延下一个可用页签、同步 `ModelValue`、`Refresh()` 它、并让容器重渲染）。两个坑：摘掉的页签要 `DeActivate()`，否则脏 `IsActive` 会在翻回可见、重新注册时被误选成当前页签；`ShowTab` 对 `IsTabClosed` 的名字直接拒绝复活（`closed` 是用户点 X 关掉的硬编码页签，与 `Visible` 两件事）。`AcitveTabOnRenderCompletedAsync` 开头加收起即返回 —— 没收起的 DOM 可量，也不该对外报「渲染完成」。
  - **单测**：`test/Element.ComponentTests/ElVisibleParameterTests.cs` 12 例全绿（`Temp/b103_test_new.log`：失败 0 / 通过 12 / 总计 12）。头一条是反射守卫（五个组件的 `Visible` 必须是 `bool` + `[Parameter]` + 默认 `true`，防以后有人删参数却留下吞属性的用法）；其余正负成对：默认可见画得出 ⇄ 收起后壳与子树都不留、翻回可见能恢复；页签那组另钉「收起激活页签会顺延」「翻回可见不抢激活」「只剩一个可见时它就是激活项」「收起 ≠ 禁用」。
  - **中间一版假红（尺子自己的错）**：`HiddenPaneIsNotTheSameAsDisabledPane` 曾断 `Assert.Equal("true", aria-disabled)` ⇒ 红。真身是 Blazor 的 bool 值是**条件属性**：`true` 吐 `aria-disabled=""`、`false` 整个属性不写。改成 `HasAttribute` 有无判断，并顺手把禁用那颗补成正向对照（`Temp/b103_test_single.log`）。
  - **全量归因（2 红，本批零新增）**：`Temp/b103_test_full.log` 失败 2 / 通过 2775 / 总计 2777；两条是 `ElDropDownTableGrowTests.Guard_PanelScrollbarGutterIsOneSharedEqualWidthRule` 与 `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows`。**归因不是断言**：`Temp/b103_test_baseline.log` 把本批 9 个文件回退 + 移除新测试后跑，同样只这 2 条红、编译 0 错 ⇒ 先前已存在。回退档用 `cp` 还原并逐个 md5 核对（禁 `git checkout --`，`core.autocrlf=true` 会把未提交改动按 EOL 吞掉）；本批行数一律用 `git diff --ignore-cr-at-eol --stat` 判。
  - **出库校验**：`Temp/b103_rebuild.log`（0 警告 0 错误）→ `Temp/b103_pack.log`；解包内层 `lib/net10.0/Element.dll` 3,017,216 B、md5 `52502d1b7ccaa752fe11d7608f2fa6d7`，与构建产物逐字节相同。消费方服务侧 `/_framework/Element.wasm` 3,016,985 B、md5 `f5701fbd2740ebb0a1220a900abfece2` == `bin/Debug/net10.0/wwwroot/_framework/Element.honcw1ofvm.wasm`（改前那一档是 3,014,425 B）—— 换包后解字节验，不信时间戳。
  - **配套的消费方改动（不属库，记此以免日后看不懂）**：`emrelecrecordplusgroup.csproj:15` `Element-Plus` 0.10.23→**0.10.24**；批次101/102 写的 `@if` 兜法全部退回原始 `Visible="@(…)"` 口径（`DocRemake` 5 处、`DocBorrow`/`DocLost`/`DocSeale` 各 1 处），业务分支/方法名/SQL/文案一个字没动。顺带救活两页**从没改过业务标记**的：`DocPrint.razor`（4 处 `ElRow.Visible`）与 `DocView.razor`（3 处）—— 探针 `_tools/b103_printview_probe.mjs` 改前 红 7 / 共 13（卡片+表格+上传三块同时在场），改后 全绿 13 / 13。另 `_tools/b101_docremake_probe.mjs` 全绿 9 / 9、`_tools/b102_tabprobe.mjs` 全绿 12 / 12（改前 红 3 / 共 12）、`_tools/b103_fixture_probe.mjs` 全绿 9 / 9 —— 最后这支是给「读数为 0 / []」那几条补同尺子的正向对照（默认 `Visible` 的 `ElProgress` 必须画出轨道、可见 `ElRow` 的子节点必须跟着在、夹具页六块仍在场），真实页那档先把前提写成判据（编辑面板确有 2 个页签头 + 29 个输入框）再断言 `.el-progress` 为 0。真实入口冒烟 `_tools/b103_dialogsmoke.mjs`（红 0 / 共 4，截图同名 .png）：DocManage 行菜单『借阅登记』⇒ `dialogService.OpenAsync<DocBorrow>` 传 u_new=true ⇒ 弹窗标题「病案借阅」、3 个输入框（前提判据先亮），页签实读 `["借阅"]`；『借阅归还』那一档（u_new=false，正向对照）前 10 行菜单里都没出现（状态驱动，借出中的记录本轮没有），显式记 SKIP 不算红 —— 该正向对照由夹具档 `_tools/b102_tabprobe_after.out` 的 `p-borrow-old` / `p-lost-old` / `p-seale-old` 三块承担（夹具页验完即删，留档 `_tools/B102Probe.razor.b103final`）。横查（`_tools/b103_scan_visible.py`，剥注释后按标签取参）：消费方现在活的 `Visible` 只有 ElRow×11 + ElTabPane×3 + ElProgress×1 + ElButton×2，`ElSpace` 0 处 —— 新参数不会波及其他页面。
  - **文档**：`docs/visible-parameter-row-space-progress-tabpane.md`（含「为什么 `@if` 不是 `display:none`」、页签五处时机表、两个坑、已知边界：`Visible` 仍是本仓 Grow 口径、EP 官方用父级 `v-if`；收起会前移位置型选择器；`ElCol` 没单独加 `Visible`）。

- 🟢 **批次94 · 横向滚条「悬停加高一倍」：静止 6px、鼠标进容器 12px、移出立刻收回、按住拖动全程 12px（消费方原话「eltable eltdropdownablegrow以及相关下方的这个滚动条，鼠标不移动上去是这个默认的高度，鼠标移动上去后，高度变大1倍，便于拖曳，鼠标移走，恢复原来默认高度（拖曳过程中不要改变高度就是默认高度放大1倍的高度）」）**（2026-10-21，随包 0.10.23；本批零 C# 改动，交付面是守卫＋探针＋文档）。
  - **机制（`src/Components/wwwroot/css/index.css` 三条，成对才不漏宿主）**：① `*:hover::-webkit-scrollbar { height: var(--el-sb-h-hover, 12px); }`（`:4978`）—— Chromium 里 **`height` 只管横条、`width` 只管纵条**，所以加高天生只碰用户要的那一轴，列对齐（纵轴）一个字没动；② 退出钩子 `.el-scrollbar__wrap--hidden-default, .el-pro-tabs-grow__scroll { --el-sb-h-hover: 0px; }`（`:4983`）—— 被**用户批准归零**的滚道不许被全局加高复活，走变量而不是再打一场特异性战争；③ 伴生规则 `.el-thin-scrollbar:hover::-webkit-scrollbar, .el-tree-v2__body:hover::-webkit-scrollbar { height: var(--el-sb-h-hover, 12px); }`（`:10023`）—— 那段自声明两轴、特异性 (0,1,1) 与全局那条打平且位置更靠后，不补它就永远只有 6px（M3 实测删掉当场红）。
  - **「移出收回」「拖曳期间不缩」为什么不需要 JS 状态**：都是 `:hover` 自带语义 —— 指针离开容器 ⇒ 伪元素回落 `--el-sb-h`（6px）；按住拖动时指针本来就在容器内 ⇒ `:hover` 全程成立。实测把它量成了数字：⑧ 三采样点 `press/mid/end` 全 `12` 且 `hover:true`，⑦ 移出后 `hBar:6` ＋ 条带 `bandAbove:0`，⑨ 这一下真推动了内容（`scrollLeft 691→227`，Δ=464）⇒ 加高不是为了好看而拖不动。与 §9.2 那条「拇指显形必须走 JS 挂 class」不矛盾：**几何**跟 `:hover` 立即变，`::-webkit-scrollbar-thumb` 的**背景绘制**在真实 OS 光标路径上不重绘（`Temp/b55-native-height-arm-9.log`、`Temp/b55-paint-control-1.log`：`SetCursorPos` 那一路几何照样 6→12，拇指只画旧帧）⇒ 本探针统一用 CDP 合成鼠标。
  - **批次⑫ 的读数口径同步**：那节写死的 `width:4px / height:6px` 现在走钩子（`width: var(--el-sb-w, 4px); height: var(--el-sb-h, 6px)`），**回退值＝原字面值，口径没变**，换 `var()` 只是给退出钩子留挂点。
  - **常驻探针**：`demo/tools/probes/verify-scrollbar-hover-grow.cjs`（新，主目录探针份数 **255 → 256**，**17 项**判据，`/table` + `/grow`）⇒ **17/17 PASS、JS 异常 0**（`Temp/b55-hover-grow-8.log`）。要点：① 裸 div 前提证人（`hBar:6/vSlot:4`，为 0 ＝ 覆盖式滚条则整支作废）；④ 悬停 `hBar:12`、`cssH:"12px"`、**`cssW` 仍 `"4px"`**、9 列 `th-td` 差全 0、`th`/`td` 相对静止位移全 0；⑤ 悬停墨带 `bandAbove:11`（＝厚度−1）＋最底行 RGB `[235,237,242]` 对上 EP 的 `#EBEEF5`；⑥ 无覆盖线的裸 div 当完整厚度的画证（墨带**正好 12 行**）且 `vSlot` 恒 4；⑩ **翻面**：页内把钩子压回 6px ⇒ 同一落点 `hBar:6, bandAbove:5` ⇒ 12 由那条规则驱动，不是环境自己长高；⑪ `hidden-default` 悬停仍 `0×0`；⑫⑬ grow 面板静止/悬停都 `0×0`（`cssH:"0px"`）；⑤w/⑥w/⑦w/⑩w 四条「这一帧不是空帧」证人读数 `frameNonWhite = 0.2376 / 0.2051 / 0.2061 / 0.2394`。
  - **一次 10/13 的假红，按「红先分是谁坏了」处理**（本批最贵的一条尺子课）：收尾复跑红的正好是三条像素档（⑤⑥⑩：`rows` 全 0、⑤ 最底行 RGB `255,255,255` —— 连 EP 那条浅灰底线都不在帧里），而③④⑦⑧⑨⑪⑫⑬ 这些 DOM 档全绿、`getComputedStyle` 当场读到 `cssH:"12px"`（`Temp/b55-hover-grow-6.log`）。同步步序取证（`Temp/b55-band-frame-2.log`）与**未改动脚本的复跑**（`Temp/b55-hover-grow-7.log` 13/13）都画出 12 行 ⇒ 坏的是那一帧，不是 CSS；**触发条件未复现 ⇒ 原因照记「未定位」，不写猜测**。牙补在尺子上：加四条整帧墨量证人（13 → 17 项），并用白幕翻面证明证人有牙（`Temp/b55-frame-witness-flip-1.log`，RC=0）：盖 `pointer-events:none` 全屏白幕后 DOM 仍 `{hover:true, hBar:12, cssH:"12px"}`，而条带 `rows` 全 0、`minL 214→255`、`frameNonWhite 0.2376→0`，撤幕后条带帧指纹回到与正常档**同一个 md5**（`8eba0ae6ff03`；白幕档 `0802dbdb91e8`）。为什么必须钉在⑦：⑦ 的判据就是「无墨」，空帧会让它**恒真**（空转绿），⑤⑥⑩ 则是假红。
  - **另一条被否证实验纠正的读数**：悬停那一拍纵向滚条槽 `vSlot 0→4` **不是加高造成的** —— `window.elementBindFadeScrollbar`（`src/Components/wwwroot/js/dom.js:4597`）里的 `repaint()` 会把 `scrollbar-gutter` 钉成 `stable`（`:4649-4653`，元素 inline style 当场看得见）。否证档 `Temp/b55-colshift-2.log` E 段：把 `--el-sb-h-hover` 压回 6px（条子完全不加高），`vSlot` 照样 `0→4`，而三档 `th`/`td` 边界逐列相同（`311.6/371.6/521.6/…`）⇒ ④ 钉的是**列边界**，不拿 `vSlot` 相等当判据（那是错前提）。
  - **守卫**：`ScrollbarCaliberGuardTests` 5 → **6** 例，新增 `Guard_HorizontalBarDoublesOnHover_WithoutTouchingTheVerticalAxis`（`test/Element.ComponentTests/ScrollbarCaliberGuardTests.cs:113`）：钉静止 6 / 悬停 12 ＋ **`Assert.Equal(2 * rest, hover)`**（写死数字看不出「一倍」这个口径）、悬停档**不许声明 `width`**、伴生 hover 与全局同值、退出钩子里必须同时列出两处归零宿主。**四档单点翻面各红自己那条**（每档 `1 失败 / 5 通过`）：M1 `12px→6px` 取消加高（`Temp/b55-guard-mutation.log`，`Assert.Equal() Values differ`）；M2 悬停档补 `width:8px` ⇒ 消息 `*:hover::-webkit-scrollbar 里出现了 width 声明：悬停会把纵向 4px 那一轴一起改掉（列对齐依赖它）`，落点 `:132`（`Temp/b55-guard-m2-raw.log`、消息复跑留证 `Temp/b55-guard-m2-assert.log`）；M3 删伴生规则 ⇒ `index.css 中未找到规则：.el-tree-v2__body:hover::-webkit-scrollbar {`（`Temp/b55-guard-m3-raw.log`）；M4 删退出钩子 ⇒ `退出钩子 --el-sb-h-hover: 0px 整条不见了：悬停加高会复活刻意归零的滚道`（`Temp/b55-guard-m4-raw.log`）。还原一律 `cp Temp/b55-index-css-good.css` ＋ `md5sum -c Temp/b55-index-css-good.md5` → `OK`，**未用 `git checkout --`**；还原后守卫 **6/6 绿**（`Temp/b55-guard-after-3.log`、`Temp/b55-guard-after-4.log`）。
  - **两条守卫自己的病（先修尺子，不偷偷放宽断言）**：① `Decl()` 原来只认字面 `width:4px`，读不了 `var(--x, 4px)` 的回退值 ⇒ 批次93 记的那 2 条假红（`Guard_GlobalScrollbarIsThinVertical_WideHorizontal`、`Guard_ThinScrollbarAndTreeV2Body_UseTheSameCaliber`）本轮先以 `Temp/b55-guard-before.log` 复现、改成同时读回退值后转绿。② 静态扫描**必须先剥注释**（`StripComments()`，`:50`）：index.css 的口径说明里字面写着 `--el-sb-h-hover: 0px`，`IndexOf` 命中注释后再往回找选择器就落到别的规则上 —— 新守卫首跑就红在 `Assert.Contains(".el-scrollbar__wrap--hidden-default")`（`Temp/b55-guard-new-1.log`，`ScrollbarCaliberGuardTests.cs:147`）；选择器串一律带尾随 `" {"` 才算钉住规则本体。
  - **同批修好的另一把尺子**：`verify-markdown-scrollbar.cjs` 首轮 **19/22**（`Temp/b55-markdown-regress-1.log`），三条红全在「悬停显形」档，真身是**OS 光标停在栏上**：`dom.js:39-71` 的挂 class 逻辑有 `if (same) return;` 早退，真实光标早已把 `md-bar-hover` 挂上 ⇒ 后续合成移动不再造成 class 变更 ⇒ 伪元素不重绘（静止档证人 `barClass:true`）。否证档 `Temp/b55-md-nudge-1.log`：G1（加高在位）与 G2（压回 6px）读数**完全相同**（编辑栏 0.865/214、预览栏 0.982/214）⇒ 杠杆不是 #154。改尺子不改产品：给每栏补两条前提证人（静止时 `md-bar-hover` 没挂着 ＋ 悬停时该栏 `:hover` 真成立），22 → **26 项**，被污染时红在前提证人上并写明「本轮被污染」；改后 **26/26**（`Temp/b55-markdown-regress-3.log`）。
  - **邻居回归（同一批跑，证明加高没碰别的轴）**：`verify-scrollbar-wide-h.cjs` **8/8**（`Temp/b55-wide-h-regress-1.log`，`{vSlot:4, hBar:6}`、逐列边界 `[87,147,…,1127]` 表头/表体全等、pro-tabs 仍 `0×0`）；`verify-scrollbar-thumb.cjs` **7/7**、`verify-scrollbar-fade-scroll-race.cjs` **8/8**（`Temp/b55-verify-scrollbar-thumb-regress-1.log`、`…-fade-scroll-race-regress-1.log`）。
  - **全量归因**：**失败 1 / 通过 2764 / 总计 2765**（`Temp/b55-full-suite.log`），唯一红是已归档基线 `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows`（`docs/testing-guards.md:113`）⇒ 本批零新增红。对比批次93 的「3 / 2761 / 2764」：**红 −2（那两条是 `Decl()` 的债，本批修的是尺子）、用例 +1、通过 +3**。**文档改完后再复跑一份（数字同源）**：`SourceEncodingGuardTests`（连 `docs/` 与根 `CHANGELOG.md` 一起扫）＋ `ScrollbarCaliberGuardTests` 合并 **10/10 绿、RC=0**（`Temp/b55-docsync-guards-1.log`），全量同为 **失败 1 / 通过 2764 / 总计 2765**（`Temp/b55-full-suite-docsync.log`，同名同断言），服务侧 CSS 重取仍是 `ffd70203…` / 568,706 B（`Temp/b55-served-docsync.css`），常驻探针在 `dotnet test` 重建产物之后再跑一次仍 **17/17 PASS、JS 异常 0**（`Temp/b55-hover-grow-9.log`，第二次独立全绿）。
  - **出库校验**：`nuget/Element-Plus.0.10.23.nupkg`（17:04 那只）内层 `staticwebassets/css/index.css` md5 `ffd70203b03cdad5cb36aa7c97077bb4`（568706 字节）**== 源码**、`el-sb-h-hover` 命中 5 处 ⇒ 悬停加高**已随这只包在库里**，本批不需要新的版本号；内层 `lib/net10.0/Element.dll` md5 `9f68e94002d7e7d3a4dcd4a5abebf73f` == `src/Components/bin/Release/net10.0/Element.dll`（本批零 C# 改动）。服务侧同源：`/_content/Element-Plus/css/index.css` 落地份（`Temp/b55-served.css`、`Temp/b55-served-index.css`）md5 同为 `ffd70203…`。
  - **待用户裁定的一条（本批不擅自反转已批准口径）**：`ElDropDownTableGrow` 面板里的表体滚道是 **0×0** —— 组件自带 `<style>`（`src/Components/ElDropDownTableGrow.razor:37-42`）按批次⑧ 用户批准的「能去就去」把两轴归零，特异性 (0,2,0) ＋ 更晚的样式表压得住全局那条 (0,1,0) 的加高（⑫⑬ 守住静止/悬停都 `0×0`）。且该面板现场**没有横向溢出**（`sw==cw==338`）⇒「便于拖曳」在这里没有对象可拖。要不要给它重新装回一根 6→12 的可拖横条属另一档口径，探针不擅自改。
  - 机制、三处成对、实测表、四条读数陷阱（含 EP `.el-table::before` 把墨带吃掉一行、墨阈值必须落在 238 与 214 之间、空帧假红/假绿、`vSlot` 归因）与变异表见 `docs/caret-scrollbar-focus-ring-alignment.md` §10；守卫与尺子课见 `docs/testing-guards.md` §3.32 与 §4 第 94–96 条。

- 🟢 **批次93 · `ElWaitingPopupGrow.ShowDelay`：短于一档延迟的请求「整块一个节点都不画」，于是 200ms 出头的换页不再闪一枚刚出场就收的胶囊（消费方原话「200ms 出头的浮层配 0.2s 进场动画，等于刚闪出来就收；要不要给等待浮层加个『短于 ~300ms 不显示』的阈值」⇒「好的」）**（2026-10-21，随包 0.10.23）。
  - **根因（实抓，不是想象）**：宿主 `/dictionary/docelement` 换页时 `QueryData()` 的 `Show()`→`Hide()` 真实跨 **518 / 550 / 824ms**，而浮层的 DOM 出场还要再晚 **~0.26–0.31s**（Blazor 渲染派发本身的延时，不是组件慢），所以旧观感是胶囊在 300ms 处刚冒头、200–500ms 后被收掉 —— 配 `index.css:5266` 那条 `ewpg-zoom-in 0.2s` 进场动画，动画还没跑完就开始消失。同一时刻 `ElTable` 自挂的表格罩**全程在场**（罩存活 264.2 / 471.2 / 329.9ms，与浮层同起同落）， ⇒ 藏掉浮层不会少一个信号。独立尺子 `_tools/waiting_lifetime_probe_l2now.out` 亦给浮层存活 [223,226,150]ms。
  - **为什么不是 300ms（用户点头的数字被实测推翻，这是本批最重要的一条）**：阈值判的是「这次请求会不会长寿」，可比的量是**请求自身跨度**，不是浮层可见窗口的起点。场上三档请求都 ≥518ms ⇒ **任何 <825ms 的阈值都挡不住其中至少一档**，300 更是几乎零作用（照旧每次浮出、照旧闪）。故取 **1000ms**：落在实测分布之上，且和「>1s 才该给进度指示」的通例对齐。**代价记清**：`ShowDelay` 计时期间浮层零节点，连遮罩 `.el-overlay`（那层挡点击的薄纱）也不出现 ⇒ 1s 内的等待期间点得到表格；这是宿主单实例的选择，改档只改 `ComFunction.razor:36` 一个属性，不必重打包。
  - **改法**：把「有没有人请求显示」(`_visible`) 与「场上到底画没画」(`_painted`) 拆成两个状态（`ElWaitingPopupGrow.razor.cs:40-43`），新参数 `ShowDelay`（`:176`，默认 0 ⇒ 与加参数前逐字节同行为）、只读 `IsPainted`（`:182`，与 `IsVisible` 并列供宿主与测试观察）、`Show()`/`OnParametersSetAsync()` 两条路径统一走 `StartPaint()`（`:281`，`≤0` 时同步置位、否则挂计时），`Hide()` 与 `Dispose(bool)`（`:247`）走 `CancelPendingPaint()`（`:323`）。**关键不是计时器而是"节点"**：`ElWaitingPopupGrow.razor:7` 的门从 `@if (Visible)` 改成 `@if (Visible && _painted)` ⇒ 延迟期间遮罩、卡片、进场动画一个都不生成，而不是生成后 `opacity:0`。每次请求一把**独立的 `CancellationTokenSource`**、回调里 `await InvokeAsync(StateHasChanged)` 且不递归 `Show/Hide`（本仓 `blazor-auto-close-pattern` 口径）；`TaskCanceledException` 与 `ObjectDisposedException` 都吞（`Cancel→Dispose` 与 `Task.Delay` 内部定时器之间有已知竞态）。**与 `ElViewerGrow.MinLoadingMilliseconds`（`:87`）不是同一件事**，别混：那根是"至少让人看满 N ms"（防消失闪），这根是"到点才让人看"（防出场闪），方向相反。
  - **单测**：`test/Element.ComponentTests/ElWaitingPopupGrowTests.cs` 新增 region **7 例**（不设参数⇒同步画 / 到点才画 / 未到点就 `Hide`⇒永不画 / 隐藏后重开用新计时器不留旧账 / 画完之后 `Hide` 立即收 / 声明式 `Visible` 也吃阈值 / `ShowDelay=0` 声明式当场画）。该文件 **30/30 绿**（`Temp/b93_popup_tests.log`）。**翻面验牙**（`Temp/b93_teeth.out`，基线 0 红）：`StartPaint` 的 `if (ShowDelay <= 0)` 改成恒真 ⇒ **3 红**；删掉 `Hide()` 里的 `CancelPendingPaint(); _painted = false;` ⇒ **1 红**（`ShowDelay_HideAfterPaint_IsImmediate`）；razor 门退回 `@if (Visible)` ⇒ **3 红**；声明式路径直接 `_painted = true` 不走 `StartPaint()` ⇒ **1 红**。红名从 `[FAIL]` 行抽取（`Temp/b93_teeth_names.out`）—— .NET 日志按 CP936 落盘、中文行乱码，用 ASCII 锚点才取得到。每档跑完 `cp Temp/b93/*.good` 复原 + `md5sum -c` 逐字节命中，**未用 `git checkout --`**。
  - **量测踩到的坑（本批改了两版尺子才对）**：跨 CDP 往返采样会把浮层存活读成 **2425 / 2717 / 2457ms**（`_tools/waiting_threshold_probe_t1_before.out`、`_t2`），跟独立尺子的 223ms 差一个数量级。根因不是页面，是尺子：**Blazor WASM 的渲染批次只在 JS 派发轮到手时才 flush，而 `Runtime.evaluate` 本身就是那样一个轮** ⇒ 探针每往返一次就替应用推一帧，消失时刻由探针决定。改成「一次点击 + 整段 10ms 轮询」封在同一个页面内 async evaluate、同一个 `performance.now()` 时钟上量，读数立刻与独立尺子同量级（存活 [263.3,259.2,404.8]ms）。另两条尺子规矩：计数器只统计**插入**（document-start `MutationObserver`，另有一条合成节点自检证明观察者有牙，`_t3` 就是自检没过当轮作废的）；请求证人用 `fetch`/`XMLHttpRequest` 挂钩，与轮询路径互相印证（判据1b「两条路径一致性」5/5）。
  - **浏览器实测（同一把尺子，改前/改后各一份落盘）**：改前 `_tools/waiting_threshold_probe_t5_before_req.out` ⇒ 快请求 3/3 照样浮出（出场延迟 [307.1,292.1,267.6]、存活 [289.9,531.8,256.8]）；改后 `_u1_after.out` / `_u2_after_1000.out` RC=0 ⇒ **快请求整块不画（0/3，计数器插入 0，表格罩 3/3 照旧在场，场上不缺信号）**，而 `Network.emulateNetworkConditions` 只加 600ms 往返延迟的慢请求 **2/2 到点才画（出场延迟 1026.7 / 1033.8 / 1037.9 / 1044.4ms）**。慢档是正向对照 —— 它证明 0/3 不是"浮层坏了"。截图 `_tools/waiting_threshold_probe_{t5_before_req,u2_after_1000}.png`。
  - **出库校验**：先 `dotnet build --no-incremental`（`Temp/b93_rebuild.log` 有 `Element -> ...Element.dll` 那行）再 `dotnet pack`（`Temp/b93_pack.log`），解包内层 `lib/net10.0/Element.dll`（3014656 字节，md5 `9f68e94002d7e7d3a4dcd4a5abebf73f`）**逐字节 ==** 磁盘 `src/Components/bin/Release/net10.0/Element.dll`；新成员按 **ASCII 在 `#Strings`** 里找 ⇒ `ShowDelay`=3、`PaintAfterDelayAsync`=3、`IsPainted`=1，版本串按 **UTF-16 在 `#US`** 找 ⇒ `0.10.22`=0、`0.10.23`=3（本批第三次用这条判据，0.10.21/0.10.22 都抓到过静默发旧版）。`staticwebassets/css/index.css` md5 `ffd70203b03cdad5cb36aa7c97077bb4`（568706 字节）== 源码（本批没动 CSS）。宿主侧再验服务字节：`/_framework/Element.wasm`（`_tools/served_Element_93.wasm`，3014425 字节）含 `ShowDelay`=3、`IsPainted`=1。
  - **全量归因（3 红，本批零新增）**：失败 3 / 通过 2761 / 总计 2764（`Temp/b93_full.log`）。① `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows` = 批次92 同款既有基线（`docs/testing-guards.md:113`）。②③ 两支 `ScrollbarCaliberGuardTests` 不是本批带来的，用**只换 `index.css` 一档**的实验坐实（`_tools/b93_css_attrib.out`）：工作区现状⇒红 2，把该文件临时换成 `git show HEAD:`（其余一律不动）⇒红 0，`cp` 复原并逐字节对账后⇒红 2。根因是工作区里那条未提交的 `width: var(--el-sb-w, 4px)`，守卫的读取正则 `width\s*:\s*(\d+)(?:px)?` 读不了 `var()`。**这条守卫的短板照记不偷偷补**：它只认字面 px，任何走变量的口径都会让它假红 —— 属滚条口径批次要处理的题，不混进本批。
  - **配套的消费方改动（不属库，记此以免日后看不懂）**：`emrelecrecordplusgroup.csproj:15` `Element-Plus` 0.10.22→**0.10.23**；`Pages/Common/ComFunction.razor:36` 全站唯一那枚浮层挂 `ShowDelay="1000"`，`_waitingPopup` 上方注释留了完整取数理由（三档跨度、~0.3s 派发延时、300 为什么是惰的、1s 内不画意味着点得到表格、改档只改一个属性）。业务逻辑一字未动（`QueryData()` 的 `Show/Hide` 调用点、方法名、SQL、文案全保留）。

- 🟢 **批次92 · `ElementIconName`：把「图标名 → 字形名」的归一从 `ElIcon` 私有实现提成公开件，`ElMenuItem`/`ElSubMenu` 一起用上（消费方原话「左侧菜单的 icon，根据返回的 u_icon 来判断，不为空就用 u_icon 渲染」）**（2026-10-21，随包 0.10.22）。
  - **触发点**：宿主 `emrelecrecordplusgroup` 的 `/baseinfo/menuinfo` 里把「人员维护」的图标填成 `user`，左侧导航那格却还是通用九宫格。根因两条，一条在库一条在宿主：宿主 `IconMap.ToElement` 认不得 `user` 就退到 `el-icon-menu`；而**即便它透传，库也只认 `<ElIcon Name>` 这一条路**。
  - **库侧的病（改前实抓，不是推断）**：`ElIcon` 里有一份完整的归一（trim → 去 `el-icon-` 前缀 → PascalCase→kebab → 查字体别名），但 `ElMenuItem`/`ElSubMenu` 各自只做了 `icon.StartsWith("el-icon-") ? icon : "el-icon-" + icon`。⇒ 同一个字符串在两个组件里结果不同：`<ElIcon Name="avatar"/>` 有图（别名退到 `user`），`<ElMenuItem Icon="avatar"/>` 是**空白格**（类名写出去但 CSS 里没有 `.el-icon-avatar:before`）。
  - **改法**：新增 `src/Components/ElementIconName.cs`（public static，`Normalize`/`ToClass`/`IsKnown`/`FromEnum`/`GlyphNames`），两份清单一起搬进来：`FontAliases`（59 条，枚举里有、字体里没有的名字退到最近的字形）与 `FontGlyphs`（280 条，从 `wwwroot/css/index.css` 的 `.el-icon-x:before` 抽出的真实字形名）。`ElIcon.razor.cs` 220→86 行（私有实现全删，改调本件）；`ElMenuItem.razor.cs:350`、`ElSubMenu.razor.cs:771` 的 `NormalizeIcon` 改成 `ElementIconName.ToClass(icon)`。**宿主因此能实现「空→默认、认得→照它渲染、画不出来→默认」而不必复制这 59+280 条清单**（`IsKnown` 就是给消费方挡空白格用的）。
  - **两份清单防烂**：`GlyphNames_AreExactlyTheFontRulesInIndexCss` 现场正则扫 CSS 与清单逐名对账（另有一条 `inCss.Count > 200` 自检，防的是"扫不到东西所以两边都空"的假绿）；`EveryEnumIcon_ResolvesToAGlyphInTheFont` 要求 `Icon` 枚举每个名字归一后都落在字形清单里。⇒ 以后改 CSS 或加枚举，测试当场翻脸，不靠人记。
  - **单测**：新增 `test/Element.ComponentTests/ElementIconNameTests.cs` **29 例**（归一五种写法、别名、`IsKnown` 三档、菜单两支同口径、`null` 图标不渲染 `<i>`、`ElIcon` 抽取后行为不变）。**翻面验牙**（`Temp/b92_teeth.out`，未变异基线 0 红）：菜单两支退回"只补前缀"⇒ **3 红**（正是 `avatar`/`UserFilled`/SubMenu 那三例）；摘掉 `["avatar"]="user"` 一条别名 ⇒ **5 红**；从字形清单删掉 `zoom-out` ⇒ **1 红**（对账那条）。每档跑完 `cp Temp/b92/post/*.good` 复原 + `md5sum` 逐字节对账，**未用 `git checkout --`**。全量 **失败 2 / 通过 2755 / 总计 2757**（`Temp/b92_full.log`）：两条红都是既有基线 —— `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows` 是 `docs/testing-guards.md:113` 记档的那 1 红基线，`ElViewerGrowTests.LoadingGate_MinDuration_*` 是 `docs/viewer-alignment.md:500` 记档的负载偶发（单跑 184 例只剩前者）。**归因不是靠"看着不像我改的"**：`Temp/b92_attrib.out` 把批次92 整体退回改前 ⇒ 仍红，再把 `ElTable` 两支退回 HEAD（批次91 一起摘）⇒ 仍红。
  - **出库校验（本批抓到一次静默发错版，第二次踩同一坑）**：第一次 `dotnet pack` RC=0、包也生成，但内层 `lib/net10.0/Element.dll`（`c85d389e…`）里 `ElementIconName` 命中 **0**、私有 `NormalizeIconName` 还在，连批次91 的 `TrailingFiller` 都没有 ⇒ 是**比批次91 还旧的字节**，`dotnet test` 的增量判定没被 `cp` 复原触发。判据先校准再使用：拿已知含新字面量的 `Element-Plus.0.10.21.nupkg`（`b74e4aed…`，`TrailingFiller` ascii=5）证明 grep 法有效，且**元数据名按 ASCII 找、字符串字面量按 UTF-16 找**（`el-icon-` ascii=0 / utf16=102，拿 ASCII 去 grep 字面量会一片 0 命中、误判成"没打进去"）。`dotnet build --no-incremental` 后重打 ⇒ 包内 dll == 磁盘 `bin/Release/net10.0/Element.dll`（`d5152c4f…`，3013120 字节），`ElementIconName`/`ToClass`/`GlyphNames` 各命中 1、`NormalizeIconName`/`IconToCssName` 归零；`staticwebassets/css/index.css` md5 `e8827923…` == 源码（本批没动 CSS）。宿主侧再验服务字节：`/_framework/Element.ocjkayn1jr.wasm`（文件名哈希随内容变）md5 `46641b90…` == 磁盘，且同样含 `ElementIconName`。
  - **配套的消费方改动（不属库，记此以免日后看不懂）**：`Common/IconMap.cs` 的 `ToElement` 从"字典没命中就兜底"改成四档 —— 空 → `el-icon-menu`；`el-icon-*` 前缀 → 过 `IsKnown` 才透传（画不出来的仍回默认）；遗留 Material/bootstrap 名 → 75 条字典；其余 → `Element.ElementIconName.IsKnown` 认得就 `ToClass` 原样渲染，不认得才兜底。字典 47 个目标名逐条查过字形清单（0 个画不出来），别名键与字形清单 0 重叠 ⇒ 这一改不会把原先画得出来的图标改画成别的。实测见宿主 `_tools/nav_icon_probe_n4.out`（改前：32 项，29 项落兜底，空白格 0）/ `_nav_icon_probe_n5.out`（改后：28 项落兜底，「人员维护」= `el-icon-user` U+E6A3 有字形）/ `_tools/uicon_source_probe_s5.out`（31 行逐条与导航配对，名字对不上 0、实际类名≠期望 0；其中 24 行 `u_icon` 真为空、4 行是字体没字形的 Material 名 ⇒ 按设计回默认）。

- 🟢 **批次91 · `ElTable.TrailingFiller`：把「表宽 − Σ声明」的富余交给一根没有内容的尾部填充列，于是每一列都精确等于声明值（消费方原话「/dictionary/docelement 太宽了」，截图圈住「编辑时间」）**（2026-10-21，随包 0.10.21）。
  - **触发点**：宿主 `emrelecrecordplusgroup` 的 `/dictionary/docelement`，「编辑时间」列实测 **634px**，而内容（`2026-01-12`）自然宽 71.6 + 内衬 20 = 需要 91.6 ⇒ 倍率 **6.92**，542px 死白；同时「名称」列被钉成 120 反而放不下 126 的自然宽，折成两行（`_tools/docelement_probe_de_before.out`，1600×900、20 行真数据）。
  - **根因（不是这一页写错，是库只给了一个不好用的杠杆）**：`table-layout:fixed` 只有两条分配规则 —— ① 每列都带宽度且 Σ<表宽 ⇒ 富余**按声明比例摊回每一列**；② 有列不带宽度 ⇒ 带宽度的列钉死、剩余**均分给不带宽度的列**。批次㊸（`docs/table-auto-column-width.md`）选的解法是「留一列 auto 吸富余」，代价是**必须由宿主挑那一列**：挑错就是本页（末列是日期 ⇒ 日期吃下全部富余）。宿主追问「不能一列都不留 auto 吗」，实测答：更糟 —— 9 列全钉（Σ=773，容器 1307）整体 **×1.69**，操作 128→216.4、日期 100→169.1，写多少都不算数（`_tools/docelement_allpinned_ap1.out`）。
  - **改法**（`ElTable.razor.cs` 新增 `TrailingFiller`（默认 `false`）+ `UsesTrailingFiller`，`ElTable.razor` 6 处插入，`wwwroot/css/index.css` 1 条归零规则）：① 表头/表体各多渲染一根**不带 width 的 `<col>`** 与一个空格子 ⇒ 规则 ② 生效、富余全归尾列、数据列逐列 = 声明；② 开了尾列后不写 `Width` 的列**按记账钉死**（`80 + 图标盒`，与 `ColumnsTotalWidth` 同一个数）—— 否则场上两根无宽 col 会变成「尾列和它均分」；③ 整表一列都没声明宽度时**不开**尾列（那种表插尾列只会把均分 N 份变成 N+1 份）；④ 尾格内衬/右边框归零，且选择器写到 `(0,4,1)` 才压得住 vendor 老主题 —— 归零是为了窄容器（Σ>容器）时它能干净地塌成 0 宽，不凭空撑出一条滚不动的滚动条。尾列**不进 `Headers`** ⇒ 排序/筛选/span-method/编辑取列/合计下标一律不受影响，表宽记账与关着时同一个数。
  - **本批发出库前被像素级探针抓到的第二个症状**：首版尾格 `th` 只带 `el-table__cell el-table-column--filler`，表头那条 1px 下划线**在 1125 处断掉**、右边 454px 没有底线（`_tools/filler_line_f1.out`：尾 `th` `bb=0px rgb(144,147,153)` vs 左邻 `1px rgb(235,238,245)`，而尾 `td` 是对的）。根因是本仓口径：表头下划线**只挂在 `th.is-leaf`** 上（`td.el-table__cell` 自带一条），而顶层非分组列本来就带 `is-leaf`（`GetHeaderCellClass`）⇒ 正解是给单行/分组两支尾格补 `is-leaf`，不是再写一条 CSS 特例。
  - **浏览器实测（同一把尺子，改前/改后各一份）**：改后 `_tools/docelement_probe_de_tfafter.out` ⇒ 逐列 **55/128/120/200/50/50/50/100/100**（全部 = 声明值）+ 尾部 454px 填充，无表头折行、无正文折行、无表头正文错位，`EXIT=0` 且金丝雀亮（把「编辑时间」`<col>` 钉到 40 ⇒ 该列表头 1→4 行、倍率 1.09→0.44、复原回 100）。尾格横线 `_tools/filler_line_f2.out` ⇒ 尾 `th`/尾 `td` 与各自左邻**同档**（`1px rgb(235,238,245)`）、尾 `th` 右缘 1579 == 表右缘；判据用「与左邻同档」而非「>0」，否则粗细或颜色降一档的半修好会漏网。
  - **列宽档对照（回答「多宽的尾巴算好看」）**：`_tools/filler_width_ab_ab1.out` 在同一张真实表上把「名称」列 `<col>` 依次钉成 200/320/460（跑完复原并核对 200）⇒ 尾部空白 454/334/194，三档都无折行、尾格都有横线。**采用 200**：名称钉到 460 时行内出现一段比列宽还长的空白，眼睛要从名称的值跨过 300 多像素才够到「病历」勾选框，比把空白留在表格右端更难看（截图 `filler_width_ab_ab1_{200,320,460}.png`）。空白落在哪一列是宿主的排版决定，库只保证「声明多少就是多少」。
  - **单测**：新增 `test/Element.ComponentTests/ElTableTrailingFillerTests.cs` **10 例**（默认关无尾列 / 两根尾 col 都不带宽度且顺序在数据列之后 / auto 列钉 80、带排序钉 104 且关着时必须读到 `null` / 表头一格表体每行一格且无内容 / 尾格带 `is-leaf`（含分组 `rowspan=2` 那一支）/ 开关两版 `max(100%,Σ)` 同数 / 全 auto 表不开尾列 / MinWidth 也算声明 / CSS 源码侧同源约定）。**翻面验牙**：摘掉两处 `is-leaf` ⇒ 2 红 8 绿（`Temp/b91tf_mut_isleaf.log`）；`UsesTrailingFiller => false` ⇒ 6 红 4 绿（`Temp/b91tf_mut_usesfiller.log`，那 4 个绿的正是「关着时不该有尾列」，方向对得上）。还原用 `cp Temp/b91tf_ElTable.razor.good` + `md5sum -c` 命中，**未用 `git checkout --`**，`touch` 后复跑 10/10。全量 **失败 1 / 通过 2727 / 总计 2728**（`Temp/b91tf_full_01021.log`）＝ 批次90 基线 2718 ＋ 本批 10，唯一红仍是既有 `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows`（`docs/testing-guards.md:113` 的 1 红基线），本批零新增红。
  - **版本号说明（本批为什么是 0.10.21 而不是重打 0.10.20）**：`0.10.20` 已被宿主 restore 过（`emrelecrecordplusgroup.csproj` 当时钉的就是它），NuGet 对已安装版本目录不刷新 ⇒ 同版本号重打宿主永远拿不到 `is-leaf` 那一修，遂进 `0.10.21`。**顺带抓到一次会静默发错版的坑**：第一次 `dotnet pack` RC=0、包也生成了，但内层 `Element.dll` 是**改前字节**（新字面量命中 0、dll mtime 仍是改前的 11:12，日志里没有 `Element -> ...dll` 那行）—— 正是 `docs/testing-guards.md:193` 那条「改了却没生效、编译仍 0 错误」；判据必须是「解包 grep 新字面量 + 比 dll mtime」，光看 pack 退出码会直接发错版。
  - **出库校验**：`nuget/Element-Plus.0.10.21.nupkg` 内层 `lib/net10.0/Element.dll` md5 `b74e4aed310a1af8cb4fc4f80e6594d3` == `bin/Release/net10.0/Element.dll`；`staticwebassets/css/index.css` md5 `e882792302c9f6e1386c3fa88674726c`（567151 字节）== 源码 == 服务侧 `/_content/Element-Plus/css/index.css` 逐字节；服务侧 `/_framework/Element.vitch2jwf9.wasm` md5 == 磁盘 == 且含 `el-table__cell el-table-column--filler is-leaf`（#US 里两处同名串合并成 1 份，命中 1 即两处）。
  - **真实像素复核**（computed style 只是 CSS 侧的说法，出库前再用自写无依赖 PNG 解码器 `_tools/png_row_scan.mjs` 逐行扫截图本身，`_tools/png_scan_control.txt`）：同一张截图取「数据列 x=1060」当对照、「尾部填充列 x=1400」当判据，看 y=249（表头下沿）有无横线 ⇒ 改前 0.10.20：对照列**有**（248[d6]、249[d11]）、填充列**没有**（最近的 259/260 是水印笔画）；改后 0.10.21：两列都有，填充列那条是精确 `249[d0]`。两档横线数只差这一条 —— 补上的正是那根线。**这一轮先假红过**：页面有斜排水印，水印压住横线时像素被混色（数据列的表头下沿读成 `rgb(241,241,241)`/`rgb(225,228,234)` 而非精确 `rgb(235,238,245)`），**精确色匹配在有内容的那侧不成立**，一度把对照档当成「尺子坏了」；改为「逐通道最大差 ≤ T」后 T=6/12/20 三档复跑，四个格子判据一字未变（`_tools/png_scan_tolerance.txt`）⇒ 结论不依赖我挑的容差，且容差没弄丢牙齿：填充列改前那格 T=20 仍 false（空白是纯白、水印又落在 249±1 之外）。
  - **配套的消费方改动（不属库，记此以免日后看不懂）**：`Pages/Dictionary/DocElement.razor` 表格挂 `TrailingFiller="true"`、名称列 `Width="200"`（内容自然宽 126 + 内衬 20 ⇒ 146 下限）、编辑时间 `Width="100"`（需要 91.6）。
  - **机制、四档试用与未做**（含「编辑行没补尾格」「合计行补了尾格」「已扫到三张仍是 auto 口径的日期列页」）见 `docs/table-trailing-filler.md`。

- 🟢 **批次90 · 勾选列表头那 2px：老主题把 `th > .cell` 的左内衬钉成 10px，表体是 12px ⇒ 表头方框和表体方框上下错开（消费方原话「表头的选择框和表体里面的选择框，不需要对齐吗？」）**（2026-10-20，随包 0.10.19）。
  - **触发点**：宿主 `emrelecrecordplusgroup` 的 `/dictionary/docelement`，最左勾选列表头那枚方框比下面每一行的方框靠左。
  - **根因（级联实抓，非推断）**：`wwwroot/css/index.css` 第 1 行 vendor 进来的 element-ui 老主题里有 `.el-table th>.cell{padding-left:10px;padding-right:10px}`，特异度 (0,2,1) **高于** EP 的 `.el-table .cell{padding-left:var(--el-table-cell-padding-x)}` (0,2,0)，而该变量是 12px（`--small` 档 8px）⇒ 全站**每一列**的表头内容都比同列表体内容靠左 2px，只是勾选列里两枚 14px 的方框并排时最扎眼。改前实测（1600×900，`_tools/docelement_v1.out`）：`th .cell` pl/pr=`10px`、`td .cell`=`12px`，勾选框左缘 **th 282 / td 284**。
  - **改法**（两处，成对才生效）：
    1. `src/Components/ElTable.razor:113`（单行表头）、`:181`（分组表头 `rowspan=2`）、`:347`（表体行）三处选择列 `th`/`td` 补上 EP 的官方类名 `el-table-column--selection`（原先库里根本没有这个类，CSS 侧无路可钉）；
    2. `src/Components/wwwroot/css/index.css:1823-1835` 新增 `.el-table th.el-table__cell.el-table-column--selection > .cell { padding-left/right: var(--el-table-cell-padding-x); }`，特异度 (0,4,1) 稳压老主题那条；用变量而非 12px 字面量，`--small` 档跟着一起收。
  - **为什么只钉选择列、不做全表口径统一（这是本批发过又被推翻的一档）**：先按全表口径出了 **0.10.18**（`.el-table th.el-table__cell > .cell`），横切扫描立刻看到回归——50px 宽的勾选列表头（病历/打印/停用）内衬从 10→12 后可用宽只剩 26px，放不下 28px 的两个汉字，`nat` 从 28 掉到 14＝**一行一个字**折成两行（`_tools/docelement_check_v2scan.out`）。⇒ 0.10.19 把选择器收窄到 `el-table-column--selection`，其余列一律不动（老 10px 原样保留，探针里有一条专门验这个：`编码列表头 pl=10px`）。
  - **浏览器实测（同一把尺子，改前/改后各一份）**：改后 `_tools/docelement_check.mjs TAG=v6` ⇒ **PASS=18 FAIL=0**，含 `③ 表头/表体选择框左边缘一致 th=284 td=284`、`③ 三行表体选择框同一条竖线 [284,284,284]`、`④ 本表没有一列表头被裁或被折行（8 个有文字表头）`。**变异验牙**：现场 `insertRule` 注回 `.el-table th.el-table__cell > .cell{padding-left:10px !important}` ⇒ 同一帧读出 `thL 282 / tdL 284`，`deleteRule` 撤除后回到 `284/284`。
  - **全站横切**（`_tools/sel_align_all.mjs TAG=a2`，RC=0）：30 个含表格的路由、15 张带勾选列的表；其中**表头有全选框且表体有行**的 7 张（`docborrowinfo`/`docprintviewinfo`/`packageinfo`/`cw/reportinfo`/`dict/settlementinfo`/`dictionary/docelement`/`third/thirdview`）**逐张 `delta=0`，逐张牙口 `before 0 → 注回 10px 后 -2 → 撤除后 0`**；另外 8 张在 testuser 下整页 0 行（无表体框可比），只验到规则确实落到表头（`thCellPl=12px`，8/8）。无牙自检：可比表 < 6 张直接 `exit 2`，不允许拿「0 未对齐」当结论。
  - **顺带记下的既有问题（本批未修）**：横切扫描发现 13 个路由共 49 处表头折行/裁切，**全部 `isSel false`**（不落在这条新规则上），根因是页面 razor 里窄列配长表头（如 `employeeinfo` 身份证号/联系电话 `Width="70"` 配 4 字表头＝56px 文字 + 20px 老内衬放不下，`roleinfo` 创建日期同理，`docmanage` 21 处、`readhispatient` 10 处）。属于消费方列宽口径，要改得单独开批。
  - **版本号说明**：`0.10.17` 只动了 `<Version>`、CSS 与 0.10.16 逐字节相同（内层 `staticwebassets/css/index.css` md5 同为 `1e840ffa…`），宿主从未消费过它；NuGet 对已安装版本目录不刷新 ⇒ 不能拿它发新版，遂 `0.10.18`（全表口径，已废）→ `0.10.19`（本批）。
  - **单测**：2717 通过 / 1 失败 / 2718，本批两次出库各跑一遍（`_tools/b96_libtest.log`、`_tools/b96b_libtest.log`），与批次89 基线同数；唯一红仍是既有 `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows`（`docs/testing-guards.md:113` 写明的 1 红基线），本批零新增红。
  - **出库校验**：本批同时动 `wwwroot/css` 与 `ElTable.razor` ⇒ 两头都要对：源码 `index.css` md5 `c8141979…` == nupkg 内层 `staticwebassets/css/index.css` == 服务侧 `http://localhost:5084/_content/Element-Plus/css/index.css` 逐字节；nupkg 内层 `lib/net10.0/Element.dll` md5 `48399c16…` == `bin/Release/net10.0/Element.dll`。
  - **配套的消费方改动（不属库，记此以免日后看不懂这两处 razor）**：`Pages/Dictionary/DocElement.razor` 编辑弹窗去掉 `Height="650px"`（表单内容实测只有 293px 高，钉死留一整块死白）、宽度 650→560；`Pages/Dictionary/DocElementEdit.razor` 三个勾选字段栏 `Lg="3"`→`Lg="6"`（78px 栏的标签文本盒只有 46px，装不下 48px 的「病历部分」⇒ 用户看到的「病…/允…」）。


  - **触发点**：宿主 `emrelecrecordplusgroup` 的 `/baseinfo/employeeinfo` 操作列四颗文本按钮（修改/停用/重置密码/仓库分配），鼠标一移上去字就没了。
  - **根因（CSSOM 实抓，非推断）**：库里 `.el-button--X.is-plain, .el-button--X.is-text` 这条**合并块**把 is-plain 专用的 `--el-button-hover-text-color: var(--el-color-white)` + `--el-button-hover-bg-color: var(--el-color-X)` 一并塞给了 is-text；但 is-text 的 hover 底色随后又被自己的规则 `.el-button.is-text:not(.is-disabled):hover { background-color: var(--el-fill-color-light); }` 拉回浅灰。EP 2.14.0 原实现里 is-text 的 `:hover` **只改底色不碰字色**（`button.scss:175-212` 的 `@include when(text)` 段），per-type 循环只调 `button-variant($type)`，不存在这条白字。⇒ 变量 `--el-button-hover-text-color` 仍是 `#fff`、底色却是 `rgb(245, 247, 250)`，白字落浅灰。
  - **改法**（`src/Components/wwwroot/css/index.css:649-689`，5 个类型各两条）：`.el-button--X.is-text, .el-button--X.is-link` 块内补 `--el-button-hover-text-color: var(--el-color-X);` 与 `--el-button-active-text-color: var(--el-color-X);`，底色一律不动（保持与 EP 同口径）。primary/success/warning/danger/info 全覆盖，注释里写明 EP 出处与本批实测读数。
  - **浏览器实测（同一把尺子，改前/改后各一份）**：改前 0.10.15「重置密码」hover 前 字 `rgb(230, 162, 60)` 底 `rgba(0,0,0,0)` → hover 后 字 `rgb(255, 255, 255)` 底 `rgb(245, 247, 250)`（`_tools/gridbtn_hover_v1.out`）；改后 0.10.16 同一颗 hover 后字色不变仍 `rgb(230, 162, 60)`、底色照旧浅灰（`_tools/gridbtn_hover_v2.out`）。
  - **四类型逐颗扫 + 三条对照**（`_tools/gridbtn_hover_all.mjs` → `gridbtn_hover_all_v2.out`，RC=0）：info/danger/warning/success 逐颗悬停，每颗都要「底色确实变了（证明 hover 真生效，不是压根没碰到）」且「字色等于该类型色且不是白」⇒ **PASS=4 FAIL=0**；换页 `/baseinfo/deptinfo`（修改/删除）同尺子 **PASS=2 FAIL=0**，说明不是这一页的偶然。**变异档**：现场 `insertRule` 注入 `.el-button--warning.is-text{--el-button-hover-text-color:var(--el-color-white)}` 复原旧行为 ⇒ 同一颗立刻读出 `rgb(255,255,255)`，撤除后回到 `rgb(230,162,60)` ⇒ 判据有牙。**反向对照**：同页实心「新增」（success、无 is-text）hover 后仍是白字 `rgb(255,255,255)` @ `rgb(149,212,117)` ⇒ 钉色只落在 is-text/is-link 这一类，没把实心按钮一起改坏。
    首轮 v1 全 4 条 FAIL 是**探针自己的错**：期望表按完整类名 `el-button--info` 建键，而正则取的是捕获组 `info` ⇒ `want` 恒 undefined。已改为裸名并加「类型不在期望表 ⇒ exit 2」的无牙自检。
  - **全量测试**：2717 通过 / 1 失败 / 2718（`_tools/b91_libtest.log`），与批次88 基线同数；唯一红仍是既有 `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows`（`src/Components/ElTable.razor.cs:187` 的 `|| column.ChildContent != null` 让带模板的散装列不再抛，测试仍钉旧契约；`docs/testing-guards.md:113` 已写明「这是一条有 1 个红的基线」，b52/b86/b87/b88 四条历史日志同名同红线，本批零新增红）。本批改的是 `wwwroot/css`，bUnit 侧不加载样式表，与这条红无因果关系。
  - **出库校验（本批是纯 CSS 批，流程要换比对对象）**：dll 与 0.10.15 同尺寸 3002880、nupkg 内层 `lib/net10.0/Element.dll` md5 `399aabbb…` == `bin/Release/net10.0/Element.dll`；真正要盯的是 `staticwebassets/css/index.css`——nupkg 内 md5 `1e840ffa…`（563540→564822 字节）== 源码 == 服务侧 `http://localhost:5084/_content/Element-Plus/css/index.css` 逐字节（`_tools/b91_served_v2.css`）。⇒ 只改 CSS 时「内层 dll md5 一致」不能当发版证据，必须把 `staticwebassets` 里那条 css 也 md5 对齐。


- 🟢 **批次88 · 隐藏容器里的页签表头测量收口：`barWidth < 0` 不再无条件 `Refresh()`（DocManage 整页卡死的真根因）**（2026-10-19，随包 0.10.15）。
  - **触发点**：宿主 `emrelecrecordplusgroup` 的 `/business/docmanage` 打开即整页假死——CDP `Runtime.evaluate` 60 秒 OK=0/blocked=8、同窗口**一次后端都没问**（`_tools/docmanage_net_dm1.out`；这句话当初没有证据，见本条末尾「断言复核」）。
  - **定位链**：razor 逐段二分（切掉 391-898 整块 ⇒ 活；只切 pane0 ⇒ 仍死；只切 pane1 ⇒ 仍死；只留 ElTabs 骨架 ⇒ 仍死；去掉 `ValueChanged`/`BeforeLeave` ⇒ 仍死；**只把外层 `ElRow` 的 `display:none` 去掉 ⇒ 活**）⇒ 死循环在 ElTabs/ElTabPane 的测量路径里，与业务代码无关。
  - **决定性读数**：库内投 3 条 `System.Console.WriteLine("B88DBG …")` 插桩（打包 0.10.13），浏览器侧从 `Runtime.consoleAPICalled` 抓（renderer 主线程被占死时浏览器进程事件仍照常流，此路此前已用 `Debugging hotkey` 验过）：
    `pane=0 w=0 pad=20 ol=0 barW=-20 hdrUpdated=False` **重复 12492 次/25 秒**（`_tools/b88dbg_dm_v13.out`），而同页 `tabsAfterRender` 只出现 8 次 ⇒ 循环不在 ElTabs，而在 `ElTabPane.AcitveTabOnRenderCompletedAsync`。
  - **根因**：页签被放在 `display:none` 的容器里时 `clientWidth` 恒为 0（`dom.js` 里 `return el.clientWidth || 0`），`barWidth = 0 - padding` 永远是负数；旧实现把「量到负宽」当成「布局还没就绪，下一轮再量」，无条件 `Refresh()` 自喂渲染 ⇒ 每轮渲染再量 4 个 JS 互操作再 `Refresh()`，主线程被吃满。EP 侧只在 `nextTick` 量一次、量不到不重渲染（`tabs.tsx:222-223`），不存在这条路。
  - **改法**（`ElTabPane.razor.cs:19-20,156-172`）：重试封顶 `MaxMeasureRetries = 3`，量成功即归零；容器由隐藏转可见时子树照常重渲染、当轮就量出真实宽度，下划线不丢。
  - **单测**（`ElTabsTests.cs`，两条同前提配对）：`HiddenActivePaneGivesUpMeasuringAfterBoundedRetries` 用 bUnit 把 `getClientWidth` 钉 0、左右 padding 各 10（`barWidth=-20` 恒负），断言 `JSInterop.Invocations["getClientWidth"].Count` 落在 `[1,8]`（下界保证测量这条路真被走到，不是哑弹）；正向对照 `VisibleActivePaneMeasuresAndPositionsTheActiveBar` 钉 100/10/10/5 ⇒ `.el-tabs__active-bar` 必须出现 `width: 80px` + `translateX(15px)`。
    **变异验牙**：把 `if (measureRetries < MaxMeasureRetries)` 改成 `if (true || …)` 后跑同一批 ⇒ 测试主机 `Stack overflow`（`Renderer.ProcessRenderQueue` 无限递归）直接中止（`_tools/b88_mut_v15_unbounded.log`），正是 DocManage 那个卡死的确定性复现。恢复用 `cp` 备份 + `md5sum -c`（`3e08520e…`），禁 `git checkout --`。
  - **浏览器复验（同尺子改前/改后各一份）**：改前 0.10.13 `/business/docmanage` evaluate `OK=0 blocked=4`、插桩 12496 行；改后 0.10.15 `OK=4 blocked=0`、`.el-table__body tr` 从 0 涨到 7、正文 374 字（`_tools/docmanage_net_v15.out`）。页签几何回归扫 6 个带 ElTabs 的路由（`_tools/tabs_visible_*.out`）：syssetinfo 下划线 x295/w56 → 点第二个页签 x391/w28、docprint 272→368、Login 1160→1256 全部照走；docremake 点第二个页签不切＝`OnSkipCanChange` 空模型校验拦住（既有业务分支，非本批回归）；docremakeaudit 页签在 `@if (showstyle == 2)` 里，查询态压根不出树。
  - **同批顺带**：`ElTabs.SetActivateTabAsync` 补上 EP `setCurrentName` 的同名短路（目标名==当前激活项直接返回，不重测表头、不重发 `update:modelValue`），配套 `ActivatingTheAlreadyActiveTabDoesNotRefireValueChanged`（含「真切换必须照发」正向对照）。这一条**单独并不足以**救 DocManage——它治的是回调自循环，DocManage 死在测量自循环；两条都要。
  - **包版本事故（记下别再踩）**：`0.10.11` 与 `0.10.12` 的 nupkg 内层 `Element.dll` 是**同一份 06:15 的陈旧 Release 产物**（都是 3002880 字节、md5 `3fad3e76…`）——`dotnet pack` 不带显式 `dotnet build -c Release` 时不会重编，`--no-build` 又把旧产物原样打包。⇒ 那两轮的「浏览器复验无效」结论**测的其实是旧版本**，作废。本批改流程：改库 → `dotnet build -c Release` → `pack --no-build` → **解开 nupkg 比对内层 dll md5 + 关键字节**，再动消费方。0.10.13（插桩）/0.10.14（插桩+修复，md5 `9c3b050f…`）/0.10.15（净版，md5 `93977ba1…`）三档均按此校验。
  - **全量测试**：2717 通过 / 1 失败 / 2718（`_tools/b88_libtest_full_v15.log`），唯一红是既有 `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows`，本批零新增红。
  - **消费方侧同批一项（不在本库）**：`Layout/NavMenu.razor` 的菜单补载改成最多问 3 次——原来「数据为空就再问」让每轮渲染都发一次后端，实测 25 秒 x274 POST + x277 `appurlinfo.json`；收口后同窗口 x4 POST + x5 `appurlinfo.json`（`_tools/req_tally_d2.out` vs `_tools/req_tally_navfix.out`）。
  - **断言复核（同日补做，结论：原话成立，但当初的探针没资格这么说）**：「一次后端都没问」出自 `_tools/docmanage_net_dm1.out`，而那个探针的请求过滤是 `u.startsWith("http://localhost:5084")`——后端在 `127.0.0.1:5282`（`/appurlinfo/appurlinfo.json` 的 `backurl`），**跨来源，一条都进不了统计**。⇒ 当时这句是「没看见」当成「没有」。补做：把重试上限去掉、每次测量投一行 `REPRO1` 到控制台，打包 `0.10.16-repro1`（纯复现档，测完即从本地源里取走，存档 `_tools/repro1_pkg/`），用 `_tools/repro1_tally.mjs` 按 host 分源重测 25 秒窗口：
    | 档 | evaluate | `REPRO1` 测量 | `127.0.0.1:5282` 请求 |
    |---|---|---|---|
    | 复现档 `/business/docmanage`（去掉上限） | OK=0 blocked=4 | **10754**（采样点 x200@4.4s 起，尾 x10754@32.0s；循环约 3.7s 起跑） | **x0**（`_tools/repro1_tally_dm.out`） |
    | 复现档 `/baseinfo/deptinfo`（牙口对照：同一把尺子量健康页） | OK=5 blocked=0 | 0 | x7，3.5–3.8s（`_tools/repro1_tally_canary.out`） |
    | 净包 0.10.15 `/business/docmanage`（恢复后） | OK=5 blocked=0 | 0 | x10，3.8–5.8s（`_tools/repro1_tally_dm_after.out`） |
    两档对照 ⇒ 负向断言有正向同窗支撑：卡死的 28 秒里页面连**首次**后端查询都没发出去（健康页在 3.8–5.8s 就问 10 次），也就是说不止「不问」，而是整个异步流水线被渲染自循环饿死在起跑线。恢复三证：源码 md5 回到 `3e08520e…`（`cp` 备份 + 逐字节核对，禁 `git checkout --`）、服务产物回到 `_framework/Element.qmiv91lc3r.wasm` md5 `a997a07b…`（与换包前基线一字不差，见 `_tools/wasm_baseline_v1.txt`）、全量测试 2717/1/2718 与基线同构（`_tools/repro1_libtest_full.log`）。
    **本轮新坑（值得记）**：换回 0.10.15 后 `dotnet build` 连着两次**仍然发复现档的 wasm**——`obj/Debug/net10.0/webcil/Element.wasm` 是「dll→wasm」的增量缓存，比的是时间戳不是内容，而复现档产物比全局包里的净 dll **更新**，于是拷贝/转换整步被跳过。删掉 `obj/Debug/net10.0/{webcil,compressed}` 与 `bin/.../wwwroot/_framework` 再构建才回正。⇒ 换包复验不能只看 `bin` 里的 dll（它确实是净的 `93977ba1…`），必须解服务侧的 `.wasm` 字节验内容（`'REPRO1'.encode('utf-16-le') in bytes`，C# 字面量在元数据 #US 堆里是 UTF-16LE，ASCII grep 抓不到）。
    **只改注释会不会动 dll？** 会动哈希、没动行为：本批改完注释后重编 Release，dll 与已出库的 0.10.15 内层**同尺寸 3002880 字节，只差 72 个字节，聚成 5 小段**（偏移 136–139、2817616–2817631、2995992–2995995、2996076–2996091、2996188–2996219；看位置和长度像 PE 头/元数据尾的标识区，未逐字段拆开证实）。同一份源码跑全量测试 2717/1/2718 与基线同构、浏览器同尺子复量 OK=5/blocked=0 且后端 x10 与改前一致 ⇒ 行为没变。结论：**整档 md5 不能单独当「源码==已发布包」的判据**（`<Deterministic>` 的哈希会随注释这类源码内容变），要配行为证据一起看。


- 🟢 **批次87 · `MaskedPassword` 改出生 markup：掩码场出生即 `type=text` + 内联 `-webkit-text-security`，堵死离开路径的挂锁图标（批次86 没堵住的那条）**（2026-10-19，随包 0.10.10）。
  - **触发点**：批次86 出库后真实页复验发现「打密码后不提交就离开」地址栏钥匙图标**照出**——掩码档在浏览器眼里仍是密码框。用户追问根因后按实验链定位并返工。
  - **根因（七轮实验链，全部落盘 `_tmp_probe/b86_axis1-7.log` + 图）**：Chrome 对密码格的**分类发生在场元素首次入树的时刻，并终身记牢；事后改属性不撤销**。
    ① 原地改 `type` / 摘类 / 去 `autocomplete` / 去 `placeholder` / 删 label 节点 / 换内联样式，13+ 臂**全部照出**（axis1-5）；
    ② 整节点 `replaceWith` 换成干净节点、以及出生就是 `text+sec` 的合成页 ⇒ **不出现**（axis3、`b155_mech`）；
    ③ MutationObserver 在插入后第一个微任务改造（0 信号锚点臂）**仍出现** ⇒ 分类同步于入树，JS 微任务追不上（axis7 `m-D`）；
    ④ 批次86 的探测是**异步**的 ⇒ WASM 首帧 `type=password`（渲染#1），掩码类渲染#2 才补上 ⇒ **出生即被分类**，翻面再快也是事后（axis6 选择器 `input.el-input__masked` 全部 hits=0 恰好钉死这个时序）。
  - **改法（四处）**：
    ① `ElInput.razor.cs` 新增 `OnInitialized`：WASM 下投 `IJSInProcessRuntime` **同步**探测 `elementSupportsTextSecurity()`，首帧前就定档 ⇒ 掩码场**出生** `type=text`，浏览器从头到尾看不到 password 瞬间；非 InProcess 运行时（Server / bUnit）投不动 ⇒ 保持 false 走 `OnAfterRenderAsync` 异步探测，出生仍原生 `password`（不泄露明文，回落语义与批次86 一致）；
    ② 掩码从 `.el-input__masked` 类改为 `inputStyle` **内联** `-webkit-text-security:disc`（affix interop 重写 style 时随 builder 一并重写，不会被抹掉），类与 `index.css` 的批次86 规则一并移除（类本身是分类信号票之一）；
    ③ 掩码档不再发 `autocomplete="new-password"`（同为信号票；`data-lpignore` / `data-1p-ignore` 保留；原生 `password` 档（Firefox 等不支持掩码的浏览器）仍发）；
    ④ 眼睛切换改走内联样式有无，语义不变（`passwordVisible` → builder 条件）。
  - **Firefox 无回归**：同步探测在 Firefox 里返回不支持 ⇒ 出生即原生 `password`，**没有明文闪帧**（乐观渲染才会有的坑，同步探测绕开了）。
  - **验证（zyweb 真页 + 三态尺子）**：`b87_ab.mjs`，尺子 = REF 无图标参照 + native 金丝雀（就地翻回 `type=password` 再填，必须出现）+ nofill 负对照（不填必须未出现）+ b87-real 主档（出生态勘察 → 眼睛切换往返 → 填三格 → 离开）。
    出生态勘察（`BORN` 行）：`types=["text","text","text","text"]`、`styleAttr="-webkit-text-security:disc"`、`cls2="el-input__inner"`（掩码类已不存在）、`ac="off"`、`sec2="disc"`。
    离开判 result（`_tmp_probe/b87_ab2.log`，全程日志）：`REF noIconRef=d2cc4e8dc1c0a81d557d5ae453f973a1`；**b87-nofill 未出现**（T==noIconRef，负对照干净）；**b87-native 出现**（ICON-REF=`4db0332c5b7e730a4dfbed9fdb5b47e2`，金丝雀活着＝尺子有牙）；**b87-real 未出现**（T==noIconRef）＝批次87 堵住。眼睛切换往返：`EYE-OPEN sec2=none（inlineSec 无）` → `EYE-CLOSED sec2=disc（inlineSec=disc）`，明文↔掩码双向都在。
    另两站出生态察（`_tmp_probe/b87_born2.log`，document-start 挂 MO 盯 `input[type=password]` 插入，种 `userfcode` 过守卫直达）：`/Common/changepwd/admin` 3 颗（含 `show_oldpwd=false` 时藏在 `.hidden` 壳里的「输入原密码」）与 `/Common/LockScreen/admin` 1 颗，**全部** `type=text` + 内联 `-webkit-text-security:disc` + `data-lpignore=true` + `autocomplete=off`，两页 **MO pwHits=0**＝从头到尾不存在 password 瞬间。三个站点共 5 颗密码框至此全部出生态核验。
  - **全量测试**：2713 通过 / 2 失败 / 2715（`_tmp_b87_test_full.log`），与批次86 基线同构（`PlainColumnWithoutPropertyThrows` 既有红 + `TrailingDebounce` 负载偶发家族），本批零新增红。
  - **截图通道换代（本轮踩坑，记下别再踩）**：OS 定点截屏（固定屏幕坐标 + AppActivate 聚焦）在探针窗口几何漂移 / 被其它窗口遮挡时抓到**别人的窗口内容**（REF 档抓到了 IDE 文字，md5 全新 ⇒ 三态判据作废）。换 `b87_grabhwnd.ps1`：按标题前缀找窗口（探针标题 `B87*` 独占，**永不匹配用户窗口**）→ `SetWindowPos` 钉 (100,100)+Topmost → `GetWindowRect` → `CopyFromScreen` → **窗口相对坐标**裁条（omnibox 右端 ★/头像/钥匙槽 + 下方气泡区）→ 撤 Topmost。
    **PS 进程必须先 `SetProcessDPIAware()`**：不声明时 `GetWindowRect` 给逻辑坐标、`CopyFromScreen` 抓物理像素，右侧 1/3 被裁（★/头像/⋮ 全没了，DPI 125% 实测）。
  - **消费侧**：zyweb 包引用 `Element-Plus` 0.10.9 → **0.10.10**，三处页面参数不动（`MaskedPassword="true"` 原样）。

- 🟢 **批次86 · `ElInput.MaskedPassword`：密码框改走 CSS 掩码，浏览器的密码管理器看不见这一格（新增「保存密码」抑制档，默认 `false`）**（2026-10-18，随包 0.10.9）。
  - **触发点**：用户原话「**登录的时候，修改密码的时候，锁屏的时候，不要让浏览器提示是否保存密码**」。给的两条路里选了「**改库：加掩码档（推荐）**」，版本「**递增到 0.10.8（推荐）**」——但 0.10.8 已被批次85 占用（当天 19:32 出库），故本批落 **0.10.9**，同一裁定、只是目标位后移。
  - **既有信号不够（这是本批存在的理由）**：`PreventPasswordSave` 给的是 `autocomplete="new-password"` + `data-lpignore` + `data-1p-ignore` 这一套**提示**。Chrome 155 实测对**登录用途**的密码框直接忽略这些提示（MDN 同口径），同一页原生 `type=password` 照弹。提示词这条路在这里没有牙，只剩「让这一格在密码表单分析里不存在」一条。
  - **两条触发路径先分清（都是实测，不是猜）**：
    ① **真提交且发生导航** ⇒ 「要保存密码吗？」气泡；
    ② 在 `type=password` 里**打字后不提交就离开页面**（刷新 / 关闭 / 跳转）⇒ 地址栏**钥匙图标**（挂起待保存）。
    ② 是本批真正要堵的那条——**锁屏页根本没有 `<form>`**，只有 ② 能解释它。
  - **改法**：`MaskedPassword="true"` ⇒ 原生 `type` 恒为 `text`，打点交给 `wwwroot/css/index.css` 新增的 `.el-input .el-input__inner.el-input__masked { -webkit-text-security: disc; }`。
    眼睛按钮切换不再在两种原生 `type` 之间来回切，改成摘 / 挂这一个类；绑定、校验、`ShowPassword` / `Type="InputType.Password"` 语义一律不动（`IsPasswordField` 仍是唯一判据）。
  - **回落是硬要求**：`dom.js` 新增 `window.elementSupportsTextSecurity()`，用 `CSS.supports("-webkit-text-security","disc")` 探测；不支持或探测抛（bUnit、服务端预渲染）⇒ 走原生 `password`——**宁可没有抑制效果，也不能把密码显示成明文**。
    探测结果刻意做成**实例字段**而非 `static`：静态会被无 JS 环境那一次失败永久污染，真实浏览器里的后续实例就再也开不了掩码档。
    渲染决策留在 C#，JS 只回答「支持与否」，不写任何样式（Blazor 单一真源）。
  - **一条反直觉的坑，记下来别再踩**：不能用 `getComputedStyle(el).webkitTextSecurity` 当探测器——实测原生 `type=password` 的格子也回 `"disc"`，那条读数分不开「浏览器实现了这个属性」和「这一格是密码框」两件事。
  - **验证（机制层，自建页 + 系统级截屏）**：`127.0.0.1:9343` 起最小页面，四档 `native | mask | plaintext | offform` × 两条触发（提交 / 离开），**每档各开一个新标签**（第一版没分标签，native 的气泡活过了 `Page.navigate`，把后面几档的「改前」基线全污染成假绿）。
    浏览器 UI 只能整屏抓（CDP 截不到工具条、也点不到气泡里的「保存」），裁条用 `_tmp_probe/b155_crop.ps1`（System.Drawing），图与脚本落 `Temp/b155/`；判据 = 裁条 md5 与改前基线 `f128b493449a6bfc1e15626dc2c28ad7` 比对。
    结果：**native 提交 ⇒ `native_t2.png` 气泡**、**native 离开 ⇒ `native_leave_t2.png` 钥匙图标**；**mask 与 plaintext 两档、两条触发，md5 与基线逐字节相同**（无气泡、无钥匙图标）。
    `plaintext`（不掩码的 `type=text`）是同名前提的正向对照，它同样不弹 ⇒ 证明起作用的是「**不是 `type=password`**」这一条，`-webkit-text-security` 只负责补回打点。
    整轮日志 `_tmp_probe/b155_mech_all.log`、`_tmp_probe/b155_mech_leave.log`。
  - **zyweb 登录页实测「不弹」，本批不是修一个已复现的弹窗**：Blazor `EditForm` 把 submit `preventDefault` 了，SPA 路由变化不算导航 ⇒ 点击与回车两条触发都没有气泡（`Temp/b155/ab_native_t3.png` 是登录成功后的首页，无气泡）。留档是为了不把「抑制失败」误判成「成功」——真正的暴露面是 ②，而 ② 在三个站点上都成立。
  - **全量测试（先立 baseline 再谈本批）**：`dotnet build` RC=0、0 警告；全量 **2713 通过 / 2 失败 / 总计 2715**（`_tmp_b86_test_full.log`）。两条红都做了归因，**没有一条是本批引入的**：
    在 `Temp/b86base`（`src`+`test` 副本）里把本批的两处行为改动还原（`UseMaskedPassword => false`、`OnAfterRenderAsync` 去掉探测调用）复跑 ⇒
    ① `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows` **还原后依旧红**＝既有红，`docs/testing-guards.md` 批次83 一节早写明「这是一条有 1 个红的基线」（`ElTable.razor.cs:187` 的 `|| column.ChildContent != null` 让带模板的散装列不再抛，测试仍钉旧契约）；
    ② `ElSegmentedTests.ValidateEventTriggersFormValidationOnSelect` 在副本里转绿，但**同一份 dll** 连跑两次同一条 2-test filter 得到「2 红」和「1 红」（`_tmp_b86_two_r1.log` / `_tmp_b86_two_r2.log`）、单独跑整个类 **5/5 绿**（`_tmp_b86_seg_class.log`）、第二次全量红的换成了 `ElButtonDebounceTests.TrailingDebounce_CoalescesRapidClicksIntoOne`（`_tmp_b86_test_full2.log`）⇒ 判**负载偶发**（`UnknownEventHandlerIdException`＝取节点与派发之间重渲染，handler id 失效），不是本批的行为差。
  - **消费侧**：zyweb 三个站点共 **5 颗**密码框开 `MaskedPassword="true"`（`Pages/Common/Login.razor:208`、`Pages/Common/ChangePwd.razor:38/52/66`、`Pages/Common/LockScreen.razor:232`），包引用 `Element-Plus` 0.10.6 → **0.10.9**。

- 🟡 **批次83 · 包身份改名：`Element` / `Element.X` / `Element.Markdown` → `Element-Plus` / `Element-Plus.X` / `Element-Plus.Markdown`，作者 `Smart`（`_content` 基路径随包名迁移＝消费侧破坏）**（2026-10-18）。
  - **触发点**：用户原话「**作者改成Smart，Element-Blazor改成Element-Plus-Blazor**」＋「**和另外一个nuget包要完全区分**」，
    再给 VS NuGet 包列表截图并在 `Element` 那一行批注「**改成Element Plus**」。两次裁定定稿：
    **包 id = `Element-Plus`**（X → `Element-Plus.X`、Markdown → `Element-Plus.Markdown`）、
    **`nuget/` 里改名前的旧 id 包移出 feed（保留备份）**。
  - **为什么不是改字符串**：Razor SDK 的静态资源 `<BasePath>` 是**派生值** = `_content/{PackageId}`，`ProjectReference` 消费者也一样跟着 PackageId 走。
    库内有四处硬编码消费点必须同步，否则改名后运行时 404：`ElementLang.LangPathPrefix`（词条 JSON）、
    `ElTableV2`/`ElTreeV2` 的动态 `import("./…/js/dom.js")`、`ElVideoPlayerGrow` 的 `elvideoplayergrow.js` import、
    `wwwroot/js/dom.js` 的 `CORE_FALLBACK` + `ElProTabsGrow`。JS 动态 import 在 bUnit 层完全不可见 ⇒ 复验只能在浏览器层做。
  - **改名面**（两轮 sweep ＋ fixture ＋ feed）：第一轮 25 个文件 / 56 处、第二轮 30 个文件 / 130 处
    （按行保护 GitHub URL **17 行**，扫完未保护的旧品牌串 leftover **0**；全仓改名面盘点 113 个文件 / 3294 处）、
    消费侧夹具同步 4 个文件 / 23 处、旧 id 与中间档包文件移出 feed **24 个**（落 `Temp/b53-feed-removed/`）。
    整仓散文改名（LOGO 字标、`.sln`）另立 Layer B，本批未动。
  - **身份核验**（`Temp/b53-final-identity.log`、`Temp/b53-nuspec-dump.log`、`Temp/b53-basepath-readouts.log`）：
    三包 id/title 全为新名、authors `Smart`、version 0.10.6、projectUrl `https://github.com/Element-Plus-Blazor/`，
    `<repository type="git" url="…/Element-Plus-Blazor/" />` 三处齐；依赖边 `Element-Plus.X → Element-Plus 0.10.6`、
    `Element-Plus.Markdown → Element-Plus 0.10.6 + Markdig 0.30.4`，主包只剩第三方。
    BasePath 三包各一个互异值（props 条目 8 / 2 / 10）；`src` obj 静态资源 **16/16 全 `_content/Element-Plus`**、
    `demo` obj **195 = 75 + 23 + 97**；`lib/net10.0/Element.dll`（2,999,296 B）内 UTF-16 字面量 `_content/Element-Plus` **4**、
    旧前缀 **0**、中间档前缀 **0**；包内 `staticwebassets/js/dom.js` 新前缀 2、旧 0。
    feed 里已无旧 id 产物 ⇒ 与 nuget.org 上那只 `Element` 在 id、基路径、作者三个面上完全区分。
  - **产物**（`nuget/` 最终 6 个文件，用户随后要求**整份清空重打**，故这里记的是重打那一只）：`Element-Plus.0.10.6.nupkg` 1,770,031 B md5 `f65d5e81dbf140f7f6257631cdb850bd`；
    `Element-Plus.X.0.10.6.nupkg` 93,814 B md5 `3923b1e1380e0eb57783db71b0a1f1b4`；
    `Element-Plus.Markdown.0.10.6.nupkg` 268,766 B md5 `9d1f9bff9ec789acddb2963e5ae5f2e0` ＋ 三份 `.snupkg`。
    全局 NuGet 缓存里三份的 md5 与 feed **逐字节一致**（缓存只按 id+version 取内容、不校验，这条才是「消费侧拿到的就是这只包」的证人）。
  - **清空重打实测到的一条口径**（`Temp/b53-repack-*.log`）：`cp` 备份 ＋ md5 清单 → `rm nuget/*` → 三包 `build --no-incremental` ＋ `pack`（RC=0）⇒
    **尺寸逐一只一致、六只 md5 全部变**；逐条目比对证明**包内 22/16/24 个条目内容 0 处不一致**，只有每包 11 个条目的 zip 时间戳变了
    ⇒ **nupkg 的 md5 是「这只工件」的指纹、不是「这份内容」的指纹**，抄 md5 必须连"哪一轮打的"一起抄。
    第二宗：重打后缓存里仍是旧字节（cache≠feed），删掉 `element-plus[.x|.markdown]/0.10.6/` 三个版本目录 + `restore --force` 才重新对齐，
    对齐后消费侧端到端复跑 **12/12、RC=0**（`Temp/b53-repack-consumer-verify.log`）。
  - **演示站发布包同步改名重打**（先整份删 `artifacts/demo-release` 再 `dotnet publish`）：改前 `_content/` 下还是 `Element`/`Element.X`/`Element.Markdown` 三个旧目录、
    `index.html` 旧前缀 2 处（部署站不改名就 404）；改后 388 文件、`_content/` 只剩新三目录、`index.html` 新前缀 12 处、**整份 wwwroot 带旧前缀的文件 0 个**、86 份 wasm 无过期残留。
    裸静态托管（5202）浏览器复验：启动自检 **10/10**、基路径守卫 **14/14**、弹层滚轮 **26/26**、日期写值 **30/30**、日期面板 **16/16**、站内 MSE 库 **23/23**。
    首轮启动自检 **7/10** 的三条红全是尺子：脚本自己 `fetch` 写死的旧路径 → 那条 404 连带把 Z1/Z2 判红；先 grep 证明产物里旧前缀 0 命中，再改脚本路径、判据不动。
    交付 zip 重做：388 条目＝磁盘 388 文件、22,953,677 B、md5 `aff83b3e6883905526807ee10de0dd55`（`Temp/b53-zip-verify.log`）。
  - **新增常驻守卫** `demo/tools/probes/verify-package-asset-paths.cjs`（14 项）：改前 **12/13、RC=1**
    （`Temp/b53-asset-path-probe.log`，红在 A5a——探针自己去取基路径清单，把运行时请求快照污染成「旧前缀也请过」），
    修的是尺子（先取快照再自取，脚本 39–42 行留了原因注释），**断言一条没放宽** ⇒ 改后 **14/14、RC=0**（`Temp/b53-asset-path-probe2.log`）。
  - **消费侧端到端**：`Temp/b31-app` 夹具只用本地 `nuget/` feed，restore/build/run 后浏览器 **12/12、RC=0**
    （`Temp/b53-consumer-verify.log`：按钮文字「来自 Element-Plus 0.10.6 包」＋主色 `rgb(64, 158, 255)`、`.X` 气泡、`.Markdown` 标题 id、
    六个 `_content/Element-Plus*` 资源 200 且字节数非零、旧前缀断档 404 正向对照）。
  - **回归（六个常驻探针，覆盖上面四处硬编码消费点）**：lang-i18n 21/21、lang-picker-family 34/34、tablev2-expand 9/9、
    treev2-scrollbar 7/7、tour-hollow-mask 57/57、datetime-close-on-date-pick 30/30（各 `Temp/b53-reg-<probe>.log`）。
    `verify-vendored-hls-lib` 改前 **RC=2 FATAL**（`page.goto` 30s 卡在 `networkidle`，`Temp/b53-reg-verify-vendored-hls-lib.log`）。
    归因探针 `Temp/b53-diag-videopage.log` 证真身不是改名：示例视频流仍走外部 `media.w3.org`/`test-streams.mux.dev`，
    流式请求让 `networkidle` 永不成立（0 个非 200、新前缀资源全部加载成功）。改的是等待条件——换成显式的「站内库已注入并赋给 `window.Hls`」，
    并把它写成前置证人 **A0**（今后真坏会报「接入没发生」而不是导航超时），判断断言全保留 ⇒ 改后 **23/23、RC=0**
    （`Temp/b53-reg-verify-vendored-hls-lib2.log`）；该常驻探针项数 22 → **23**，批次㉗ 的 22/22 与 13/22 是当时读数、不回写。
  - **全量 bUnit（数字与日志同源）**：`Temp/b53-tests-full.log` **2709 通过 / 1 失败 / 0 跳过 / 总计 2710**（51 s）。
    唯一红 `ElTablePropertylessColumnTests.PlainColumnWithoutPropertyThrows`
    （`Assert.IsType() Failure: Expected typeof(Element.ElementException), Actual null`，测试文件 110 行）
    与**改名前基线** `Temp/b52-tests-full-1.log`（2707 通过 / 3 失败 / 总计 2710）同名同断言，属用户未提交的表格改动、非本批引入；
    基线里另外两红（`ElFormGeneratedControlTests.GeneratedMentionSelectsOptionAndUpdatesModel`、`ElInputNumberTests.ClickAfterHoldIsSwallowed`）本轮皆绿。
  - **待裁定（未擅自处理）**：① `github.com/Element-Plus-Blazor` 实测 **404**——包里的项目链接指向还不存在的仓库
    （`github.com/Element-Plus` 是 Vue 官方 org，不可占用）；② `LICENSE` 仍是 `Copyright (c) 2019-present Element-Blazor contributors`，
    上游 MIT 署名不能单方摘除；③ 旧 id 的全局缓存目录（`element`/`element.x`/`element.markdown`）保持不动——对手包 2.14.1 就住在同一个 `element` 目录里；
    ④ 移出 feed 的旧 id 包要不要 `git rm`。
  - 详见 `docs/package-identity-alignment.md`（新建，可部署契约口径）；`docs/testing-guards.md` 新增 **§3.31** 与 **§4 第 89–91 条**。

- 🟢 **批次82 · `ElTable` 选择列宽度可调：新参数 `SelectionColumnWidth`（默认 55 不变）**（2026-10-17）。
  - **触发点**：宿主 `zyweb` 生产订单新增对话框「生产订单数量」页签，用户原话「**这个列太宽了，最多6个汉字宽就够了，
    前面选择的列的宽度也调整小一点**」。选择列宽 55 原先是硬编码，宿主一颗单选勾选框也压不下去。
  - **改法**：`ElTable.razor.cs` 新增 `[Parameter] int SelectionColumnWidth { get; set; } = 55`，
    三处硬编码 55 全部换用该参数——表头 colgroup（`ElTable.razor` 84 行）、表体 colgroup（300 行）、
    `ColumnsTotalWidth` 横向滚动记账（`ElTable.razor.cs` 1266 行，负值按 0 记）。
    默认值不动，所有既有表格零变化。
  - **消费侧用法**：`SelectionColumnWidth="40"`；本表同时挂 `TableLayout="fixed"` + 数量列不写 Width——
    fixed 布局下不带 width 的 `<col>` 均分剩余宽度，钉死列（选择列/名称列）才真正钉得住
    （auto 布局下全钉死会把富余按比例摊给每一列，见批次㛸 注释里部门维护表 305/305/381/114 的实测）。
  - **验证**：`_tmp_probe/b85_probe1_run.log`（详见该档）。详见 `docs/eltable-selection-column-width.md`。

- 🟢 **批次81 · `ElTabs` 键盘导航的作用范围收窄到页签头（pane 内容里的输入框不再被吃方向键）**（2026-10-17）。
  - **触发点**：宿主 `zyweb` 款号管理新增对话框，用户原话「**eltabs按左右键跳转tab页是不是有属性设置，
    能不能把，http://localhost:5171/dictionary/styleinfo中点击新增时候，不要按左右键跳转tab页**」。
    「预算成本/销售价格」pane 里的 DecNumberInput 按 `←/→/Home/End` 本该移动光标，页签却被切走。
  - **第一个问题的答案：没有属性**。EP 的 tablist 键盘导航是语义内建行为（官方 props 里没有 `keyboard`
    开关），本库也没有——这不是参数问题，是**作用范围**缺陷。
  - **真身**：`@onkeydown` 挂在 ElTabs **根 div** 上，Blazor 事件委托把 pane 内容里一切可聚焦元素冒泡上来的
    `ArrowLeft`/`ArrowRight`/`Home`/`End` 全部吃掉（horizontal 下 Up/Down 在 handler 里早退）。
    改前实测 `_tmp_probe/b82_probe2b_run.log`（8/0）：pane 内四键真把页签切走；同对话框页签条**上方**
    表单区的输入框不触发（它们是 ElTabs 的兄弟节点，冒泡不经过根 div）——顺带证明委托范围就是根 div。
  - **改法**：`@onkeydown="OnKeyDownAsync"` 从根 div 挪到 `role="tablist"` 的 `.el-tabs__nav` div 上
    （`ElTabs.razor`）；`OnKeyDownAsync`（`ElTabs.razor.cs` 615 行）逻辑一字未动。键事件只有焦点位于
    页签头才冒泡进 tablist（页签项是它的子节点），pane 内容里的按键不再可达。
  - **EP parity 保留**：页签项 roving tabindex（激活 0 / 其余 −1）⇒ `Tab` 落到页签头后 `←/→` 逐个移动、
    `Home`/`End` 到首尾、`Disabled` 跳过，全部照旧；`ElProTabsGrow` 不受影响（keydown 本就挂在页签项上）。
  - **验证**：单测 `ElNavigationTests.TabsSupportClosableEditableStretchBeforeLeaveAndKeyboard` 派发目标
    `.el-tabs → .el-tabs__nav`（断言语义不变），全套 **2707/0**；浏览器改后
    `_tmp_probe/b82_probe3_run.log` **13 PASS / 0 FAIL**（pane 内四键全不切 + 页签头焦点
    ArrowRight/Home/End 三条正向对照 + console/异常 0）。详见 `docs/tabs-keyboard-scope.md`。
  - **消费侧连带（`zyweb`）**：无消费端改动，库侧一处修平全仓多页签编辑对话框（款号/物料等）。

- 🟢 **批次㊾ · `ElTable` 末行回车的落点：由「换列但留在当前行」改成「换列并落在该列第一行」**（2026-10-16）。
  - **触发点**：宿主 `zyweb` 款号编辑「尺寸表」页签，用户原话「**http://localhost:5171/dictionary/styleinfo，
    把部位名称初始化，限定宽度，另外在编辑状态下如果是最后一行，右边还有列的话，回车跑到下一列第一行**」。
    这条把批次㊻ 刚定的「末行 → 右边一列、留在当前行」翻面成「末行 → 右边一列、回到第一行」，
    即按列逐格录完一列后，回车直接开下一列的第一格，不用再把光标停在该列的末行。
  - **两条路一起改（文档本来就写着两者口径一致）**：
    ① `wwwroot/js/dom.js` 的 `elementTableEnterNextEditor(shift)`：末行分支由
    `focusEditor(row.children[colIndex + 1])` 改成**双层循环**——外层向右扫列、内层从 `body.children[0]` 起逐行找，
    于是落点天然是新列的首行，且右邻整列都没有可聚焦编辑器时会自动跳到再右边一列（仍从首行起）；
    ② `ElTable.razor.cs` 的 `MoveEditingCellSameColumnAsync`（`IsEditable=true` 那条路）：末行时
    `FindVisibleRowIndex(-1, 1)` 取第一条可见行，行号变了才 `SwitchEditingRow(firstRowIndex)`，再
    `SetActiveEditingCell(columns[nextColumnIndex], nextColumnIndex, true)`；已是最后一列仍返回 `false` 不动。
    `zyweb` 全仓 0 处 `IsEditable`，② 属零消费风险的口径对齐。
  - **边界没跟着动**：`Shift + Enter` 到首行仍不换列（批次㊻ 锁的既有语义，用户只裁正向）；末行末列仍停住；
    正向对照「非末行仍同列下行」。
  - **语义代价（写进文档「已知限制」）**：换列即换编辑行 ⇒ 上一行尚未提交的编辑值按 `SwitchEditingRow(int)` 的口径丢弃。
    批次㊻ 的「留在当前行、值还在」不再成立，这是用户裁定的取舍。
  - **验证**：单测 `test/Element.ComponentTests/ElTableEnterCellNavigationTests.cs` 24/24
    （`SameColumnMode_Enter_OnLastRow_...` 翻面断言 `_r0_c1` 且编辑行回到 `people[0]`；
    新增单行表正向对照 `SameColumnMode_Enter_OnSingleRow_MovesToNextColumnSameRow`，`RenderTable` 加 `rowCount` 参数）；
    浏览器侧宿主 `zywebGroup/_tmp_probe/b60_verify_run.log`（最终包复跑 `b60_verify_run_final.log`，**29 PASS / 0 FAIL**）
    用真函数跑 7 档合成表
    （末行→下一列首行 / 右邻空列继续往右 / 非末行同列下行 / 末行末列 false / 首行 Shift false / Shift 上行 / 单行表），
    控制台 0 错误。详见 `docs/table-enter-cell-navigation.md`。
  - **消费侧连带（`zyweb`，非库改动）**：款号编辑「尺寸表」的动态首列（部位名称，`isreadonly`）原先不写 `Width`，
    按批次㊸ 是「auto 列吃掉全部余宽」——真表实测 3 列档它独占余宽摊到 542px（邻列 100）。
    现改为 `Width="@(column.isreadonly ? "80px" : column.width)"`（库里 `DefaultColumnWidth` 的兜底值，
    也是四字表头单行的下界 56+20=76），实测 col 属性成 `80px`、表头 `th` 高 32 不折行、余宽按比例摊（212/265/265，比例锁 80:100）。
    如实记着：同档对照下「不写宽度」也量到 80（库在 `ColumnsTotalWidth` 里就把 auto 列按 80 记账），
    所以钉宽动的是「独占余宽」那一侧，不是下限；先前探针手写的 8 列 60px／12 列 52px 是无单位 col 宽度的产物，本批不引用。

- 🟢 **批次㊽ · `ElFormFieldGrow` 内层控件「去壳」不再连带切掉 Element 的 30px 图标槽**（2026-10-16）。
  - **触发点**：宿主 `zyweb` 员工编辑「基本信息」页签，用户原话「**http://localhost:5171/baseinfo/employeeinfo，点击编辑进去，2026下面是不是还有东西，如果有的话，调整一下，需要改组件库，要对组件库同步更新**」。
    「2026 下面还有东西」= 「入厂日期 `2026-10-16`」的日期文本上压着 ElDatePicker 的日历前缀图标（字形整个盖在「2026」的 0 上）。
  - **真身（特异度打架，不是图标定位错）**：`wwwroot/css/index.css` 的去壳块为了「字段壳内控件贴边」写了
    `.el-form-field:not(.el-form-field--keep-control) > .el-form-field__content .el-input__inner { padding: 0 }`，**特异度 (0,4,0)**；
    而库自己给图标让位的两条只有 **(0,2,0)**（本文件 997 行：`.el-input--prefix .el-input__inner{padding-left:30px}` /
    `.el-input--suffix .el-input__inner{padding-right:30px}`）。`.el-input__prefix{left:5px}`、`.el-input__suffix{right:5px}`
    是绝对定位浮在 `input` 之上、字形 25px 宽，**30px 槽就是为了让开它**；槽被打掉后文本从 x=0 起画，图标就压在字上。
    改前实测（`_tmp_probe/b57_gutter_pre_run.log`）：**37 个被测控件里 17 个「文本起止点压在图标上」**，
    日期格 `padL/padR=0/0`、`overL/overR=-30/-30`，下拉格右余量清一色 `-30`。
  - **改法**：紧跟去壳块补两条 **(0,5,0)** 规则（写在后面、特异度更高，稳赢那条 `padding:0`），
    只对挂了 `--prefix` / `--suffix` 的控件按 Element 原口径把槽位还原：
    `.el-form-field:not(.el-form-field--keep-control) > .el-form-field__content .el-input--prefix > .el-input__inner { padding-left: 30px }`、
    `.el-input--suffix > .el-input__inner { padding-right: 30px }`。
    纯文本格（无这两个修饰类）仍然左右零槽——**去壳的本意没被破坏**（实测身份证/手机号仍 `0/0`）。
  - **验证（`_tmp_probe/b57_gutter_post2_run.log`，10 页 9 PASS）**：入厂日期 `padL/padR 0/0 → 30/30`、
    `overL/overR −30 → 0/0`、文本区 154.7 → 94.7（10 位日期实测宽 71.6，仍放得下）；
    **18 个下拉格右余量 −30 → 0，压图标的格子从 17 个归零**；
    改前基线 `_tmp_probe/b57_gutter_pre_run.log` 另配「去壳一刀切成 0」的正向对照，
    浏览器侧自证拿到的是新包（`len 532295, marker true, batch true`）；
    裁图对比 `Temp/b57/date_val_clip.png`（改前）与 `Temp/b57/date_post_clip.png`（改后，md5 不同）。
    守卫测试 `test/Element.ComponentTests/FormFieldIconGutterTests.cs` 5/5：钉住去壳那条仍为 0、
    补槽两条值=30px 且特异度严格大于并写在后面、两条必须挂在 `--prefix/--suffix` 与 `:not(--keep-control) > __content` 之下、
    库自己那两条 (0,2,0) 原样还在。勾选框等 Start 槽原生控件没有自有 `.el-input`，探针按 `holder === el-form-field__content` 排除，
    并断言「被跳过的行值只可能是 `""`/`on`」作为正向对照。
  - **消费侧连带（`zyweb` 生产订单查询条）**：留槽后日期格文本区变窄，320px 字段里的日期格只剩 119.1px、
    文本区 59.1px **装不下 71.6 的日期**（`_tmp_probe/b57_gutter_post_run.log` 就是这条 FAIL）。
    扫描器（`_tmp_probe/b57_sweep_s3_run.log`）逐档量出：**内联 `Style="width:…"` 不是杠杆**——字段壳是 flex，
    120/132/136/140 四档量出的格宽一模一样；真正的杠杆是字段的 `Width`，每 +10px 字段宽 = 每格 +5px。
    可行解只有 350/360px（370 起查询条从 2 排变 3 排），落点取 **360px**（格 139 / 文本区 79），日期控件内联宽度保持 120px 不动；
    复测 7 PASS（`_tmp_probe/b57_preorder_post2_run.log`：2 排不变、不越面板内边、无横向滚动、回退 320px 又掉回 59.1）。
  - **口径**：见 `docs/form-field-alignment.md` 第 9 节。本批**无新参数**，纯 CSS 口径修复。

- 🟢 **批次㊼ · `ElUpload` 真能直连业务后端：`ResponseParser` + `RawResponse` + 同源才发 `x-requested-with` + 粘贴登记句柄化**（2026-10-16）。
  - **触发点**：宿主 `zyweb` 款号编辑「图片页签」要换成 `ElUpload`，用户原话「**图片页签上传 ，选1**」——
    即在先前「不改库」与「改库」两个选项里明确改选**改库**（推翻他自己先前的裁定）。此前判断「ElUpload 在 WASM 里拿不到字节、
    直连后端不成立」是错的，真正拦路的是响应处理那一环，三处各自独立致命，下面逐条有实测。
    详见 `docs/upload-response-parser.md`、`docs/upload-paste-alignment.md`。
  - **① 响应键写死 `code`，本后端返 `result` ⇒ 不是报错，是永久挂起**：旧 `_uploadFile` 在 `onreadystatechange` 里
    `response.code.toString()`，键不存在就抛 `TypeError`，而**那个异常没人 `resolve` Promise**，C# 侧 `await` 永悬——
    界面只表现为一直转圈，`OnSuccess`/`OnError` 一个都不触发，控制台也不留可见痕迹。
    **改法**：新增 `[Parameter] Func<string, UploadRequestResult> ResponseParser`（原始响应体交宿主判成败与取键，
    业务键再多也不用回来加参数），`UploadRequestResult` 补 `RawResponse`（宿主在 `OnSuccess` 里二次解析）；
    不传映射时行为与旧版一致（`{ code, message, id, url }`，库演示页零改动），但**非 JSON / 缺 `code` / 状态非 2xx 一律立刻判失败并带上原文**。
    另：跨源时不再自动加 `x-requested-with`（这个头让 multipart POST 不再是「简单请求」，后端要先答一次 OPTIONS 预检；
    旧 .NET `HttpClient` 那条路不发它，所以它反而能传——这正是「换组件就传不上」的真因之一），同源仍发。
  - **② 粘贴上传的六个缺陷一并收**：旧实现是 `uploader/uploadUrl/uploadOptions` 三个模块级全局——
    后注册的顶掉前注册的（同页只落到最后注册那台）、`unRegisterPasteUpload()` 无句柄会把**整条** document 监听摘掉
    （一台 Dispose 全页粘贴失效）、url/options 冻结在注册那一帧（对话框里后填的地址永远用不上）、
    `previewFiles` 返回**紧凑数组**与 `files` 下标错位（多文件粘贴结果挂错行）、只取最后一张且无视
    `Multiple`/`AutoUpload`/`Disabled`（查看态的上传框粘贴照样外发）、`OnAfterRenderAsync` 要求 `Url` 非空才注册
    （地址后填的宿主永远注册不上）。
    **改法**：表项 `pasteUploadEntries[{upload, el, handle}]` 一实例一条、按句柄配对移除、
    每次粘贴现问 `[JSInvokable("getPasteUploadOptions")]`、id 数组改为与 `files` **同长同序**（被挡的那格空串占位）、
    尊重 `Multiple`/`AutoUpload`/`Disabled`，多台可见时按「焦点落点 → 注册顺序」定档；
    `Document` 侧用 `Dictionary<int, DotNetObjectReference<ElUpload>>` 与 `Dispose` 配对，不漏引用。
  - **验证（本机 mock 上传服务 + 临时探针页 `/probe-upload` 七台上传器，14 条断言全 PASS，`_tmp_probe/b53_upload_run5.log`）**：
    - **有牙的改前对照**：把 HEAD 版那两行响应处理原样贴进页面跑，
      `{"settled":false,"outcome":"TIMEOUT","err":"Uncaught TypeError: Cannot read properties of undefined (reading 'toString')"}`——挂死本体与原因同时抓到；
    - A 传 `ResponseParser` 读 `result` 键：选 2 张 ⇒ 2 条 `A-success`（`id=mock-19`/`mock-20`，`raw=178`）；
      B 不传映射读 EP 那一族键（向后兼容正向对照）：`id=ep-21`；
      C 指向返回 `not json at all` 的地址：4 秒内落 `C-error … msg=Upload response is not valid JSON.`，
      列表项 class `el-upload-list__item is-fail`（改前这一台就是无限转圈）；
    - 请求头用 CDP `Network.requestWillBeSent` 直接看，不靠服务端推断：
      **跨源 9 发全 `POST`、`x-requested-with: null`、预检 0 次**（mock 侧同步计数 `UPLOAD 条数=35 PREFLIGHT 条数=0`），
      **同源那一发仍带 `x-requested-with: XMLHttpRequest`**——这条同源断言是跨源负向断言的正向对照，
      缺了它「跨源没带头」与「这段代码压根没跑」在日志里长得一样；
    - multipart 形状未变（后端不用改）：`parts=[{name:"jparams",bytes:17},{name:"file",filename:"oldrepro.png",bytes:70}]`；
    - 粘贴：P1（`Multiple=true`）粘 2 张收 2 条、P2（`Multiple=false`）粘 2 张只收 1 条、
      P3（`Disabled=true`）粘 2 张日志行数 `7→7` 不动；路由用例「P1/P2 同屏可见、焦点在 P2」⇒ 新事件只进 P2
      （`P2 1→2`、`P1 2→2`），**旧实现在这条必失败**（只会落到最后注册的 P3，而那台是禁用的）；
      每步先断言「页面上可见的 `.el-upload` 恰好等于目标那台」，否则「粘贴没串台」与「盒子根本没藏好」在日志里同形。
  - **验收覆盖边界（诚实记录）**：验收会话 `unitsurl` 为空且不手动塞业务地址，所以真页面（款号编辑 · 图片页签）只验到
    「地址为空时整块不渲染、不再抛 `Upload action is required.`、页面不崩」；真后端那一次往返没跑过，机制层由 mock 全覆盖。
    探针页验完删除，仓库不留路由。`OnProgress` 仍是 0→100 两档（XHR 层未接 `upload.onprogress`，本批未改，与旧版一致）。


- 🟢 **批次㊻ · `ElSelect` 根节点补 `style="@Style"` + `ElTable` 末行回车换列 + `EnterNavigatesEditors`**（2026-10-16）。
  - **触发点**：宿主 `zyweb` 款号编辑（`/dictionary/styleinfo` → 新增）用户一句话带三个诉求，附 6 张截图：
    「**标红的列太宽，调整小一点**」「这个 table 用 small 模式」「**按回车键，如果有下一行跳到下行同列，也是编辑状态，
    没有下一行就跳到下一列（如果有的话），也是编辑状态**」。前两条的消费侧部分（列宽钉死 + 全表 `Size="Small"`）
    已在批次㊸ 的 auto 列口径下落地并实测；这一批处理剩下两件 + 顺带挖出来的那颗真雷。
  - **① `ElSelect` 吞 `Style`（缺陷，非宽度调参问题）**：宿主写了 `Style="width:130px;"`（工序分类）与
    `Style="width:250px;"`（条码生成方式），浏览器实测**完全无效**——那颗框 794px。定位过程（`_tmp_probe/b51m_probe.mjs`）：
    `flex-grow:0` ⇒ 仍 794px（不是 flex 拉的），`max-width:130px` ⇒ 130px（能压住但只是绕过）；
    根因是 `src/Components/ElSelect.razor` 的根 `<div>` **没有** `style="@Style"`——全仓 42 个 `.razor` 都写了这一行，
    下拉是漏网的那个，所以 `ElementComponentBase.Style` 赋给 `ElSelect` 后静默丢失，
    而 EP 的 `.el-select` 宽度取自 `--el-select-width`（默认 100%），没内联样式盖它就是整行宽。
    **改法**：根 `<div class="@warpperClsBuilder…">` 补 `style="@Style"`，与 `ElCascader.razor:4` 同形。
    详见 `docs/select-inline-style-width.md`。
  - **② `SameColumnNextRow` 模式末行回车改为换列**：原语是「首/末行停住」（`MoveEditingCellSameColumnAsync` 的
    `targetRowIndex < 0` 直接 `return false`），实际录完一列必须先手工加行才接得上下一列。
    按用户裁定「**下一列＋留在当前行**」：末行回车 → `columnIndex + 1`、`editingRow` 不变、`SetActiveEditingCell(..., selectText: true)`
    继续聚焦；已经是最后一列才真正停住。**反向不跟着改**：首行 `Shift + Enter` 仍停住（`SameColumnMode_ShiftEnter_OnFirstRow_Stays`
    锁的就是这条既有语义，用户只裁了正向）。
  - **③ `EnterNavigatesEditors`（新属性，默认 `false`）**：宿主要跳的那三张表（尺寸表 / BOM / 条码 / 生产指导书）
    **不走** `IsEditable` 模型——模板列把输入框常驻画在每个单元格里，`GetEditingColumns()` 要求
    `CanEdit(header) && header.EditorRender != null`，这类列一条都不满足，所以 ① ② 那条 C# 路在这里**一步都跑不到**。
    实现：`dom.js` 新增 `elementTableEnterNextEditor(shift)`，从 `document.activeElement.closest('td')` 按 DOM 顺序取行列
    （向下同列 → 到底换右列并跳过空格 → 到最后一列停住；`shift` 反向只走行），
    C# 侧在 `OnEditingRowKeyDownAsync` 的 `!IsEditable` 分支调它（`<tr>` 上的 `onkeydown` 历来就挂着，输入框事件会冒泡到行）。
    「算编辑器」的判据：`input/textarea/select` 非 hidden 非 disabled；`readonly` 的 input 默认不算，
    但 EP 把下拉/日期/数字步进做成 readonly input，所以 `.el-select` / `.el-date-editor` / `.el-time-picker` / `.el-input-number`
    里的 readonly **算**。目标格没有编辑器时返回 `false` 且焦点不掉出表格。详见 `docs/table-enter-cell-navigation.md`。
  - **验证（新增 6 例：本类 23 例）**：`ElTableEnterCellNavigationTests` 把 `SameColumnMode_Enter_OnLastRow_Stays`
    换成 `..._MovesToNextColumnSameRow`（断言活动单元格 `_r1_c1` 且 `EditingRow` 引用不变）+
    `..._OnLastRowLastColumn_Stays`；再加 `AlwaysOnEditors_Enter_CallsDomNavigation`、
    `AlwaysOnEditors_ShiftEnter_CallsDomNavigationReversed`（断言传给 JS 的 `shift` 实参）、
    **正向对照的反面** `AlwaysOnEditors_WithoutFlag_DoNotCallDomNavigation`（不开属性一个 JS 调用都不发）、
    `EditableTable_IgnoresAlwaysOnEditorNavigation`（编辑表格不去问 DOM，仍走活动单元格）；
    `ElSelectTests.InlineStyleLandsOnRoot` 断言 `.el-select` 的 `style` 属性逐字相等（bUnit 量不到计算宽度，几何交给浏览器）。
  - **翻面验牙（三个变异一次打，红的正是该红的）**：`MoveEditingCellSameColumnAsync` 的换列分支塞
    `return false;`（MUTATION-B51A）、`MoveFocusToNextEditorAsync` 首行 `if (true) return;`（MUTATION-B51N）、
    `ElSelect.razor` 的 `style="@Style"` 换成写死串（MUTATION-B51O）⇒ **失败 4 / 通过 20（共 24）**，
    红的恰好是 `SameColumnMode_Enter_OnLastRow_MovesToNextColumnSameRow`、
    `AlwaysOnEditors_Enter_CallsDomNavigation`、`AlwaysOnEditors_ShiftEnter_CallsDomNavigationReversed`、
    `InlineStyleLandsOnRoot`，而三条「停住/不发调用」的对照用例全绿（日志 `_tmp_b51_mut.log`）。
    还原用 `cp Temp/b51-good/{ElTable.razor.cs,ElSelect.razor}` + `md5sum -c` = OK
    （`c688157936eee2b18d86869c7c32e134`、`f2b421fde6c37f03a1e1435529b7faaf`），`grep -c MUTATION-B51` 两个文件都 0，
    **未用 `git checkout --`**。全量复跑 **2678 通过 / 0 失败**（改前基线 2672，日志 `_tmp_b51_full2.log`；
    中途那次 2674 的半程全量在 `_tmp_b51_full.log`，是新增 4 例 `AlwaysOnEditors_*` 之前的数）。
  - **宿主浏览器实跑（`_tmp_probe/b51n_probe.mjs`，日志 `_tmp_probe/b51n.log`，最终包复跑 `_tmp_probe/b51n_final.log`，截图 `Temp/b51/n_select_style.png`）**：
    `typeof window.elementTableEnterNextEditor === function`（新包真的上线了）；
    工序分类 `root=130x32 inline="width:130px;"`、条码生成方式 `root=250x32 inline="width:250px;"`（改前 794px）；
    合成表格（挂在真实 `.el-dialog-grow` 下）七档全对：
    `3x3 竖走+末行换列 Ey:r1c1 | Ey:r2c1 | Ey:r2c2 | Ey:r2c3 | En:r2c3`、
    `shift 反向+首行停住 Sy:r1c0 | Sy:r0c0 | Sn:r0c0`、`下行同列无编辑器 En:r0c1`（不掉焦）、
    `末行跳空列 Ey:r2c2`、`换列跳过裸 readonly Ey:sel-r0c2`、`EP readonly 可跳入 Ey:sel-r0c1 | Ey:num-r0c2 | Ey:r0c3`。
    ⚠️ 本验收会话 `unitsurl` 为空，尺寸表/BOM/条码表都是 0 行，真数据页面跑不了端到端，
    所以 **Blazor→JS 那条链用临时静态探针页补了证**（`zyweb/Pages/ProbeEnterNav.razor`，两张同构的常驻编辑器表格：
    第一张开 `EnterNavigatesEditors`、第二张不开作负向对照；探针 `zywebGroup/_tmp_probe/b51o_e2e.mjs`、
    `b51p_lastcell.mjs`，日志 `_tmp_probe/b51o.log`，截图 `Temp/b51/o_e2e_enter.png`）。
    真实浏览器里对 `<input>` 派发 `keydown(key=Enter)`（冒泡到 `<tr>` → Blazor 处理器 → JS），1.2s 后读 `document.activeElement`：
    `r0c0→r1c0`、`r1c0→r2c0`（同列下行）、`r2c0→r2c1`（**末行换列、留在当前行**）、`r2c1→` 下拉列的 readonly input
    （`readonly:true, row2,col2`）、`r1c1 -(shift)→ r0c1`、`r0c1 -(shift)→ 仍 r0c1`（首行停住）、
    末行末编辑器列回车 → `row2,col2` 原地不动；负向对照两张表 `o0c0`、`o1c0` 回车后**焦点一格都没动**（`tableIndex=1` 不变）。
    探针页已删除（删后重建 **1138 个警告 / 0 个错误**，回到加页前的警告数即证该页不在产物里；
    页面源码留档在 `zywebGroup/Temp/b51/ProbeEnterNav.razor.probe`）。
  - **改动文件**：`src/Components/ElSelect.razor`、`src/Components/ElTable.razor.cs`、`src/Components/wwwroot/js/dom.js`、
    `test/Element.ComponentTests/ElTableEnterCellNavigationTests.cs`、`test/Element.ComponentTests/ElSelectTests.cs`、
    `docs/table-enter-cell-navigation.md`、`docs/select-inline-style-width.md`（新建）、`docs/组件使用说明.md`、本文件。

- 🟢 **批次㊺ · `ElProTabsGrow` 标签级刷新：`ShowRefresh` + 项级 `Refreshable` + `OnRefreshTab`**（2026-10-16）。
  - **触发点**：宿主 `zyweb` 首页面包屑行里有一颗手写的蓝色方形「刷新」按钮（`ElButton Icon="el-icon-refresh"`，
    点击走 `RefreshUrl()` = 整页重载）。用户一句
    「这个首页，这个刷新按钮去掉，能不能加到tabs上的标签上面，**只刷新当前的页签内容**，如果需要改组件，就提交组件修改」。
    那颗按钮刷的是整个 WASM 应用（所有页签一起丢状态、重等启动屏），而用户要的是「这一页重来一遍」——
    组件里没有任何按页签触发的入口，`OnClose` 语义是关闭不是重建，所以必须加参数。
  - **改法**：`ElProTabsGrow.ShowRefresh`（`bool`，默认 `false` ⇒ 不加参数的既有宿主渲染逐字节不变）
    + `ElProTabsGrowItem.Refreshable`（`bool`，默认 `true`）。判定收在一处：
    `IsRefreshable(item) => ShowRefresh && item != null && item.Refreshable && !item.Disabled`
    —— 与 `Closable` **相互独立**：固定页（首页那种不可关闭的）照样要能刷新；`Disabled` 项不给图标。
    渲染位置在标签文字与关闭按钮之间：
    `<span class="el-pro-tabs-grow__refresh" role="button" tabindex="-1" aria-label="刷新 {Title}" title="刷新"
    @onclick="() => RefreshAsync(item)" @onclick:stopPropagation="true">` + 内联 12×12 SVG（放进度环 + 箭头，
    不引图标字体）。`stopPropagation` 是这条改动的关键：**点刷新不许顺手把该页签选中**（宿主可能正停在别的页签上刷后台页）。
    `RefreshAsync(item)` 先 `CloseMoreAsync()` + `CloseContextAsync()` 收起浮层，再 `OnRefreshTab.InvokeAsync(item)`；
    抛的是**同一个事件**，所以标签图标与右键菜单「刷新」在宿主侧天然同语义。
  - **口径裁定（一处与关闭按钮刻意不同）**：刷新图标**常驻可见**，不像 `__close` 那样 hover 才出现。
    理由：用户是把一颗**本来就常显**的页面按钮搬到页签上，改成悬停才见等于把功能藏起来；
    想要 hover 语义只差 CSS 一行（`opacity:0;visibility:hidden` + `:hover` 那条组），已按「常驻」写死并在 CSS 用例里锁住。
  - **验证（新增 8 例，本类共 52 例）**：`test/Element.ComponentTests/ElProTabsGrowTests.cs` 新增「标签刷新图标（ShowRefresh）」区
    —— `ShowRefreshOffByDefaultAndOnRendersOneIconPerTab`（关时 0 颗、开时**每页签一颗**）、
    `ItemRefreshableFalseExitsOnlyItsOwnIcon`、`PinnedTabGetsRefreshIconEvenWithoutCloseButton`（不可关闭 ≠ 不可刷新）、
    `DisabledItemGetsNoRefreshIcon`、`ClickingRefreshIconRaisesOnRefreshTabOnly`（刷新回调 1 次**且选中回调 0 次**，
    正向对照：随后点标签本体 `selectCount===1`）、`RefreshIconCarriesAccessibleLabelAndIsNotATabStop`
    （`aria-label` 以「刷新」开头 + `role=button` + `tabindex=-1`，不进 Tab 序列）、
    `RefreshIconClosesOpenMorePanel`；外加 CSS 守卫 `CssSection_KeepsRefreshIconVisibleAtRest`
    （规则体内**不得**出现 `visibility` / `opacity:0`，16px 盒 + `margin-left:5px`，hover/`.is-active` 变色规则在，
    `__refresh-icon` 落在 `pointer-events:none` 那组）与把 `__refresh` 补进 `CssSection_IsScopedAndProvidesExpectedClasses`。
    全量 **2672 通过 / 0 失败**（改前基线 2664）。
  - **翻面验牙（两轮变异，各自只打红该打的）**：
    ① `ElProTabsGrow.razor` 的 `@if (canRefresh)` → `@if (false && canRefresh)` ⇒ 本类 **失败 7 / 通过 45（共 52）**，
    红的正是渲染/交互那 7 例，CSS 守卫与「默认关」档不动；
    ② `index.css` 的 `.el-pro-tabs-grow__refresh` 规则体内塞 `visibility: hidden;` ⇒ **失败 1 / 通过 51（共 52）**，
    红的只有 `CssSection_KeepsRefreshIconVisibleAtRest`。
    两次还原都用 `cp Temp/b40-tabs-refresh-good/{ElProTabsGrow.razor,index.css}` + `md5sum -c` = OK
    （razor `41f344871a32848b14dce929ae9ea792`、css `2b3a2261e93d478a23fe0d0e8ca12a07`），**未用 `git checkout --`**。
  - **重打包**：`nuget/Element.0.10.0.nupkg`（1,761,332 字节），构建 0 警告 0 错误（`_tmp_b40_build.log`），
    全量测试日志 `_tmp_b40_full.log`。详见 `docs/pro-tabs-alignment.md` §13。

- 🟢 **批次㊹ · `ElPopover` 加 `FixedPopper`：面板钉到视口，逃出 `overflow:hidden` 祖先**（2026-10-16）。
  - **触发点**：宿主 `zyweb` 人员编辑对话框里「用户权限等级」旁的 `?` 按钮，点开「操作说明」面板被裁，
    用户一句「点击这个弹出的内容被遮挡了」并附截图（面板右半部文字拦腰断）。
  - **真身（不是 z-index、不是事件）**：面板内联渲染在 `.el-popover__reference-wrapper` 里，
    `PopoverStyleValue` 写死 `position:absolute` ⇒ 裁剪来自「最近的非 visible overflow 祖先」那条链。
    祖先链实抓（`_tmp_probe/b41_diag.log`，真点开对话框、真点 `?`）：
    `span.el-popover__reference-wrapper(visible) → .el-col-2(visible) → .el-row(visible)
    → .el-tabs__content(hidden ✂) → .el-tabs(visible) → form(visible) → .el-dialog__body(auto ✂)
    → .el-dialog(visible) → .el-overlay-dialog(auto ✂) → body(hidden ✂)`，
    面板矩形 `[936,108,1282,260]` 对上裁剪层上缘 `253` ⇒ **152px 高只剩 7px**，
    `elementFromPoint(面板中心)` 命中的是 `div.el-dialog__header` 而不是面板。
  - **改法**：`[Parameter] public bool FixedPopper { get; set; }`（默认 `false`，absolute，
    行为与加参数前逐字节一致）。打开前用 `TextClip.MeasureRectAsync` 量参考元素
    （`.el-popover__reference`，新增 `@ref="referenceRef"`）的视口矩形，
    再用批次㊶ 落地的 `TextClip.BuildFixedPopperStyle(state, Placement, out fixedPlacement, Offset)`
    写成 `position:fixed;left:…;top:…;right:auto;bottom:auto;transform:…`。
    三处与批次㊶ 同口径：① `BuildPopoverStyle()` 的 fixed 分支不再拼 `position:absolute` 与
    `top:100%/bottom:100%/left:50%` 那套相对锚点；② 翻面结果落到 `x-placement`
    （razor 从 `@Placement` 改读 `EffectivePlacement`，未开 fixed 时两者恒等），箭头跟着翻；
    ③ 间距用 `Offset` 不是写死的 8。量不到（bUnit 未打桩、元素在点击与渲染之间被移除、JS 抛）
    时静默退回 absolute。`width:{Width}px` 与 `z-index` 两条挪进公共段，fixed 分支不丢。
  - **验证（新增 7 例）**：`test/Element.ComponentTests/ElPopoverFixedPopperTests.cs` ——
    `FixedPopper_Top_AnchorsToMeasuredViewportRect`（`position:fixed` + `left:1116px` 中心 +
    `top:265px`，且**没有** `position:absolute` / `bottom:100%`）、`FlipsBelowWhenNoRoomAbove`、
    `UsesOffsetAsGap`、`KeepsWidthAndZIndex`、`ClearsStyleOnHide`（关一次再开要重量，不能拿上次坐标）、
    **正向对照** `FixedPopperOff_KeepsAbsoluteAnchors`、`WithoutJsReading_FallsBackToAbsolute`。
    全量 **2664 通过 / 0 失败**。翻面验牙：把 `UpdateFixedPopperAsync` 首行改成无条件 `return`
    ⇒ **失败 4 / 通过 3**，红的正是四条 fixed 用例，两条 absolute 对照与 z-index 守卫仍绿
    （`_tmp_b41_full.log` 为改回后的全量）；还原用 `cp Temp/b41-ElPopover.razor.cs.good2` +
    `md5sum -c` = OK（`42ad930a0bd603f2e50e7bc249dbb6e9`），**未用 `git checkout --`**。
  - **消费侧浏览器实跑（`_tmp_probe/b41_after.mjs` 26/26，`b41_regress.mjs` 11/11）**：
    A 档（开开关）面板 `position:fixed`、`346×152` 完整、中心/上缘/右缘/右上角四个
    `elementFromPoint` 全部命中面板自身、文字 111 字读到结尾「等级来约束。」；
    B 档（同容器同栅格、不开开关）仍是 absolute，右缘 6px（1440 视口）/ 52px（1100 视口）被
    `.el-tabs__content` 吃掉，那两个探针点命中的是 `div.el-dialog__body` —— 截图
    `b41_B_narrow.png` 复现的就是用户那张「文字被拦腰裁掉」的症状。窄档 A 仍完整（`346×152`）。
    详见 `docs/popover-fixed-popper.md`。

- 🟢 **批次㊸ · `ElTable` 列宽第三档改成 auto（不写 `width` 的列吸收富余），表宽求和认 `px` 后缀**（2026-10-15）。
  - **触发点**：宿主 `zyweb` 部门维护页 `/baseinfo/deptinfo` 用户一句「这个操作列太宽」并附截图 —— 操作列写了
    `Width="160px"`，浏览器里实测 **305px**，两颗按钮右边一大片死区。先做算术再动手：这张表四列声明和
    `160+160+200+60=580`，容器 1106px，`table-layout:fixed` 在「每列都有宽度且总和 < 表宽」时把富余
    **按声明宽度比例**摊回去（`160/580×1106 = 305.2`，实测 `305/305/381/114`；改动上线后用
    `_tmp_probe/b39_before.mjs` 在真实 DOM 上把四列宽度按改前写法钉回去复现，三视口读数
    `305/305/381/114`、`211/211/263/79`、`437/437/546/164` —— 死区随视口放大，见 `b39_before.log`）⇒ 消费侧把 160 改成 140
    只会得到 `140/580…→267`，**数字怎么调都调不窄**，这是库侧缺陷不是宿主写错了数。
  - **缺口 A（真身：兜底宽度把 auto 档吃掉了）**：`ResolveBaseColumnWidth` 原来第三档返回
    `DefaultColumnWidth`（80）并真写进 `<col width="80">`，于是本仓**不存在 auto 列**，每张表都落进
    「全列有宽 ⇒ 按比例摊富余」那条路。Element Plus 的口径是：不写 `width` 的列吃掉剩余空间，写了的列钉死。
  - **改法 A**：`Width` > `MinWidth` > **auto**，auto 时 `ResolveColumnWidth` 返回 `null` ⇒ Blazor 省略属性，
    `<col>` 不带 `width`，由 `table-layout:fixed` 把剩余宽度**均分给 auto 列**（`ElTable.razor.cs:1119-1175`）。
    auto 列**不吃表头图标盒加量**（批次⑦ 那 24/18/42 是给钉死宽度补的，auto 列没有可补的数），
    可读性下限改由 `ColumnsTotalWidth` 记账兜：`ResolveColumnWidthAsInt` 对 auto 返回 `80 + 加量`，
    所以 `width:max(100%,Σ)` 仍按每列 ≥80px 撑开，窄容器继续横向滚动 ——
    批次⑦ 那条「十个无宽列挤成 25px、`.cell{word-break:break-all}` 把 `Category` 逐字母竖排」的回归不会回来。
  - **缺口 B（顺带查出的第二个缺陷）**：`ColumnsTotalWidth` 用 `int.TryParse(ResolveColumnWidth(header))`
    折像素，而本仓宿主普遍写 `Width="160px"`（全仓 32 个含列声明的 .razor，后缀写法是主流）⇒
    `TryParse("160px")` 失败，**带 px 的声明一律按 80 记账**：部门维护表真实 580px 被记成 320px，
    页面实测 `style=width:100%;width:max(100%,320px)`（改前读数，正是缺口 A 的失真版本）。
    新增 `ParseColumnWidthPixels`（`:1178`）剥掉 `px` 后缀再解析，非法值才回落 80；
    `<col width>` 本身保持宿主写的字面原样，不动它。
  - **缺口 C（改 A 之后才会露出来的连带回归）**：`CellOverflowStyle`（EP `render-helper.ts:195` 的
    `.cell` 内联 `width = realWidth − 1`）走的是同一个折像素函数，auto 列折出 80 ⇒ 会给摊到 744px 的
    列内联钉一条 `width:79px`，文字提前打点。补判据 `ResolveColumnWidth(header) == null → return null`
    （`ElTable.razor.cs:3167-3184`）。百分比列此前已被「折不出像素返回 0 + `width > 1`」挡掉，同一口径。
  - **验证（新增 5 例 + 重写 1 例）**：`ElTableHeaderCellTests` —— `ColumnWithoutWidthEmitsAutoCol`
    （表头与表体的 `<col>` 都**没有** width 属性）、`AutoColumnStillCountsFloorInTableWidth`
    （auto 列仍进求和：`max(100%,80px)` / 带排序 `104px` / 带漏斗 `98px`）、`PxSuffixWidthCountsItsRealPixels`
    （`Width="160px"` ⇒ col 原样 `160px`、表宽按 `160` 记）；把原来断言「无宽列回落 104px」的
    `MinWidthAndDefaultWidthAlsoGetAllowance` 改成只测 MinWidth 一档的 `MinWidthColumnGetsAllowance`
    （全仓仅这一处依赖旧兜底）。`ElTableOverflowTooltipTests.AutoColumnSkipsInlineWidth` 钉缺口 C，
    同类的 `ColumnFlagSetsEpCellWidthOnePixelNarrower`（170→`width:169px`）留作正向对照。
    命名 8 例 **8/8**（`_tmp_b39_named.log`）、两个整类过滤 **26/26**（`_tmp_probe/b39_test1.log`）、
    全量 **2657 通过 / 0 失败**（`_tmp_b39_full2.log`；首次全量曾 1 红
    `ElNotificationTests.PauseOnHover_KeepsNotificationAlive_AndResumesAfterLeaveAsync`，
    bUnit 自述「timeout is too short… highly utilized hardware」，单跑即绿，判为负载敏感的偶发而非本批回归）。
  - **翻面验牙**：变异 A（把 `ResolveBaseColumnWidth` 的第三档还原成 `80`）⇒ **3 红 / 4 绿**
    （`_tmp_probe/b39_mutA.log`），读数就是三种旧症状：`Actual: "width:79px"`、
    `Actual: "80"`、`String: "width:100%;width:max(100%,80px)" Not found: "max(100%,160px)"`；
    变异 B（让 auto 列记账为 0）⇒ **1 红 / 3 绿**（`b39_mutB.log`，
    `String: "width:100%" Not found: "max(100%,80px)"`）。还原用 `cp Temp/b39/ElTable.razor.cs.good` 口径的
    当前好版本 + `md5sum -c`（`e54480bf687fff2c750bd9d0c63a76a5`），**未用 `git checkout --`**。
  - **出库**：`dotnet build src/Components/Element.csproj -c Release --no-incremental` **0 警告 / 0 错误** →
    `dotnet pack -c Release -o nuget`（`nuget/Element.0.10.0.nupkg` **1 758 829 字节 / 21:48**）→
    删 `~/.nuget/packages/element/0.10.0` 重装 → zyweb `dotnet restore` + 重建 **0 错误**（1138 警告，均为既有）→
    dev server 重启（只 `taskkill` 了 `netstat`+`Win32_Process.CommandLine` 双重证明是我自己那条
    `dotnet run --no-build` 的 PID 5612 及其 devserver 子进程 23600）+ `Network.setCacheDisabled` 刷新，
    量之前先自证样式挂上（`table-layout:fixed`、小按钮 `11px/11px`+`20px` 圆角、`--el-color-primary:#409eff`）。
  - **消费侧（`zyweb/Pages/BaseInfo/DeptInfo.razor`）**：操作列 `Width="160px"` → **`140px`**
    （两颗 small 朴素圆角钮口径 `n*48+(n-1)*12+24 = 132`，取 140）；「名称」列**去掉** `Width="200px"`
    改成 auto，让它吸收富余 —— 注释里写明了为什么必须有一列不写宽度。
  - **浏览器实跑**（`_tmp_probe/b39_accept.mjs` **11/11** + `b39_regress.mjs` / `b39_regress2.mjs`）：
    `colAttrs=["140px","160px",null,"60px"]`、`nameColHasWidthAttr=false`、
    `style=width:100%;width:max(100%,440px)`（`140+160+60+auto记账80`，缺口 A/B 同时落地的证据）；
    表头实测 **1440 视口 `[140,160,744,60]`（表宽 1104）／1920 视口 `[140,160,1224,60]`／
    1100 视口 `[140,160,404,60]` 且 `scrollWidth==clientWidth` 不出横向滚动** —— 改前是 `[305,305,381,114]`；
    操作列内容 `need=108 td=140 scrollW=140 clientW=140 左留白=10 右留白=22`，按钮完整且不裁切；
    11 页回归（员工/生产订单/报表/物料类别/款号/基本字典/供应商/仓库/物料列表…）全部**无 CLIP / 无 SHRUNK / 无 0 宽列**、
    `errs=[]`；三张此前调过列宽的物料页 `auto0`（全列有宽 ⇒ 本批不动它们）。截图 `b39_dept_head.png`、
    合成行版 `b39_dept_synth.png` / `b39_dept_opcol.png`、干净冷启动版 `b39_dept_final.png`；
    按「验收要真点」的口径另跑 `b39_click.mjs` **5/5**（点「新增」出「部门编辑」对话框 → 关掉后表头仍
    `[140,160,1224,60]`、`errs=[]`；该条断言里的 `fields:0` 是探针选择器没命中浮标式字段，
    截图 `b39_dept_dialog.png` 显示六个字段与「放弃/确定」都在，属探针口径而非页面缺陷）。详见 `docs/table-column-width-and-paging.md` §1–2、
    新增 `docs/table-auto-column-width.md`。
  - **如实记**：① 本批**只改「不写宽度的列」这一档**，全列都写了宽度的表仍按 EP 之外的那条 `fixed` 规则
    比例摊富余（物料类别 `180→184` 这类小偏差就是它，本批不动，要不要给某列去宽度是消费侧决定）；
    ② `<col width="50%">` 这种**百分比写在属性里浏览器不认**（四档试用实测 C≡B、D 塌成等宽 276/191/396，
    `_tmp_probe/b39_widthtrial.log`），而库侧把百分比记账为 0 ⇒ 连 80px 下限都没了，
    所以「写 `Width="100%"`」不是消费侧的合法杠杆 —— 想要吸收富余就是不写宽度；
    ③ 本环境无业务接口（`unitsurl` 为空），报表中心的分组表头与款号管理左表都渲染成 **0 列**，
    `ElTableColumnGroup` 那条 colgroup 路径只有源码级证据，没有浏览器证据；
    ④ 全仓 32 个含列声明的 .razor 里，auto 列只有 DeptInfo 这一处（本批新增），
    「有宽列 + 无宽列混在同一张稀疏表里」这个用法此前在本仓不存在，故改动影响面就是这一页 + 那一处旧兜底断言；
    ⑤ 截图顶部那条红条 `ArgumentNull_Generic Arg_ParamName_Name, uriString` **不是本批引入**：
    宿主 `Pages/Common/ComFunction.razor:233` 的 `new Uri(unitsurl)` 在本环境（`unitsurl` 键不存在）抛
    `ArgumentNullException`，被 `hishttppost` 的 catch 折成 `result:"1"` 回给页面，页面再 `ShowNotification` 出来 ——
    每个冷启动的数据驱动页都有一条，属既有健壮性问题 + 环境缺接口，只汇报不动（既有 bug 只汇报的口径）。

- 🟢 **批次㊷ · `ElButton.Visible`（按钮组内单颗可摘掉）＋ `ElSelect` 多选首屏回显选项文本**（2026-10-15）。
  - **触发点**：宿主 `zyweb`「生产订单」查询条要换成 `ElButtonGroup`，用户当场问「按钮组是不是有属性设置某个
    按钮 可以点击 或隐藏的属性，没有的话，修改组件」。核查结论：**可点击有**（`ElButton.Disabled`，且
    `ElButtonGroup` 会级联到组内每一颗），**隐藏没有** —— 组内单颗按钮此前只能靠消费侧 `@if` 包起来。
    同一次反馈里还有一条「生产状态显示汉字，并且完整」：多选下拉的胶囊首屏显示成 `0 × 1 × 2 ×`。
  - **缺口 A（`ElButton.Visible`）**：新增 `[Parameter] public bool Visible { get; set; } = true;`
    （`ElButton.razor.cs:107`），`ElButton.razor` 整段正文包进 `@if (Visible)`。口径写死在注释里：
    `false` 是「此刻不存在」（连 DOM 都不留，供权限/状态驱动的工具条），与 `Disabled`「看得见但点不动」
    是两件事；默认 `true` ⇒ 加参数前的行为逐字节不变。
  - **缺口 B（多选首屏不回显）真身**：`ElOption` 挂在弹层里，弹层内容只在**第一次点开**时才交给宿主
    （`ToggleDropDownAsync` 才把 `ChildContent` 塞进 `DropDownOption`），所以首屏 `Options` 是空的。
    单选此前已用「只读扫帧」补上（`ResolveLabelFromUnmountedOptions`），但多走的
    `SyncSelectedOptionsFromValues` 那条路没补 ⇒ 拿不到选项文本时只能回落 `Convert.ToString(value)`，
    数字字典（`int` 值 + 汉字文本）就显示成 `0 / 1 / 2`。Element Plus 的下拉是 `v-show`，选项一挂载就注册，
    所以原生没有这个问题。
  - **改法**：把原来那份单选扫描器抽成共用的遍历器 —— `WalkOptionFragment`（把片段只读 build 进临时
    `RenderTreeBuilder`）＋ `WalkFrames`（Region/Element 在数组内递归；遇到 `ElOption` 组件取
    `(Value, Text ?? Convert.ToString(Value))`；非选项组件从其 `ChildContent` 属性再钻进去），
    深度上限 `OptionScanMaxDepth = 4`。两条消费路径：单选 `ResolveLabelFromUnmountedOptions`（前提判断一字未改，
    仍在 `:348`）与新增的 `CollectUnmountedOptionTexts`（`:372`，扫成「值 → 文本」表）。
    `SyncSelectedOptionsFromValues`（`:1214+`）的取值优先级为 **已注册选项 → `dict` → 扫描结果 → 原始值字符串**，
    且整个循环里最多扫一次（`unmountedTextsResolved` 门闩）；`Options` 非空时扫描函数直接返回 `null`，
    点开过下拉的热路径不付这笔钱。**不做缓存**：扫描结果的生命周期与「候选项未注册」这个前提一样短，
    缓存反而会在父级换了 `@foreach` 数据源之后继续吐旧文本。整段 build 包在 `try/catch` 里，
    子项本身不可 build（畸形 `ChildContent`）时退化成改动前的原始值回落，不让一次首屏渲染崩掉整页。
  - **验证（新增 6 例 + 重写 6 例）**：`test/Element.ComponentTests/ElButtonVisibleTests.cs` —— 默认 `true`
    的兼容、`Visible=false` ⇒ `Assert.Empty(cut.Nodes)`、隐藏那颗点不动（`onClick` 不触发）、
    true→false→true 往返渲染树仍可 diff 且按钮仍可点、组内中间那颗隐藏后剩两颗且组壳完好、
    `Visible=false` 与 `Disabled` 的区别（后者仍有 `disabled`/`is-disabled`，前者无节点）。
    `ElSelectInitialLabelTests.cs` 多选 6 例：首屏胶囊是选项文本（且触发框不串显）、未知值**逐值**回落而非整组、
    选项嵌在分组/`@foreach` 区域里也找得到、不可 build 的 `ChildContent` 不崩、已注册选项优先于扫描（只判
    组件态 `Label`/`SelectedOptions`，不赌重渲染）、无候选项时保持原始值回落（这一组的前提对照）。
    过滤跑 **20 通过 / 0 失败**，全量 **2653 通过 / 0 失败 / 0 跳过**（基线 2642 + 11，`_tmp_b37_full.log`）。
  - **翻面验牙**：同时把 `@if (Visible)` 拆掉、让 `CollectUnmountedOptionTexts` 恒返回 `null` ⇒
    **失败 8 / 通过 12**（`_tmp_b37_mut.log`），红的正是 5 颗按钮用例 + 3 条多选文本用例；
    三条对照（不可 build 不崩、已注册优先、无候选项回落原始值）与 `ElButtonVisibleTests` 默认值用例仍绿。
    多选那两条的失败读数就是改动前的症状本身：`String: "beta" / Not found: "Beta"`。
    还原用 `cp Temp/b37/good/*` + `md5sum -c` 五份全 OK（`362f1b49…` `d7d71bb0…` `2ae17b30…`
    `169cea73…` `5b9ae81b…`），**未用 `git checkout --`**。
  - **出库**：`dotnet build src/Components/Element.csproj -c Release --no-incremental` **0 警告 / 0 错误** →
    `dotnet pack -c Release -o nuget`（`nuget/Element.0.10.0.nupkg` **1 758 178 字节 / 20:02**）→
    删 `~/.nuget/packages/element/0.10.0` 重装（缓存内 dll 反查得到 `set_Visible` 与
    `CollectUnmountedOptionTexts`）→ zyweb 重建 **0 错误** → dev server 重启 + `setCacheDisabled` 刷新。
  - **消费侧浏览器实跑**（zyweb 5171 / 真页 `/dzf/preorderinfo`，`_tmp_probe/b37_accept.mjs` **16/16**）：
    六颗按钮 `组内=6 组外=0`；9 个 `ElFormFieldGrow` 在场、`margin-left:-50px` 叠按钮的旧写法残留 0；
    订单日期/交货日期的勾选框落在字段 `Start` 槽（`307,206` / `307,306`）且自带文字已去掉；
    款号/客户的清除＋选择两颗按钮在 `End` 槽且完整落在字段盒内（`945..1029` ⊂ `791..1041`）；
    **生产状态胶囊 = 领料计划@w82 / 开裁@w58 / 生产中@w70，三颗同一行 `y=254`、无裁切**；
    点「增加订单」弹出「订单编辑」对话框、关掉后 9 个字段仍在；全程无黄条、无未处理异常。
    该表在本环境 0 行（无 `unitsurl`），那一档只作结构证据。详见 `docs/button-visible-and-select-multi-label.md`。
  - **如实记**：① `Visible` 是本仓 Grow 口径，Element Plus 没有同名属性（官方写父级 `v-if`），
    所以本库的 `ElButtonGroup` 也没有对应的「组内单颗隐藏」概念；
    ② 扫描器只认声明式 `ElOption` 子项，靠 `Options` 集合/`dict` 喂候选项的用法走原有路径，
    用 `ElSelectTreeGrow`/`ElSelectV2` 那种非 `ElOption` 结构的仍然回落到原始值；
    ③ 深度上限 4 层，嵌得更深的候选项扫不到（同样是回落，不是崩）；
    ④ **本批没修的那半**：多选胶囊换行时 `.el-select__tags` 是绝对定位的 `flex-wrap` 层，
    两行胶囊（60px）会在 32px 的选择框里上下各溢出约 14px —— 那是弹层几何问题，本批只在消费侧把字段
    定宽到 320px 让三颗胶囊一行放下来规避，库侧的溢出容器口径留给下一批。

- 🟢 **批次㊶ · `ElTooltip` 新增 `FixedPopper`：挂在表格单元格里的浮层不再被 `overflow` 裁掉**（2026-10-15）。
  - **触发点**：宿主 `zyweb` 把「物料类别」表的操作列改成四颗图标钮，名称交给悬停 tooltip。第一版
    `<ElTooltip><ElButton/></ElTooltip>` 在浏览器里**什么都不出来**——用户提的方案没错，是本库的浮层出不来。
  - **缺口是真身**：本库 `ElTooltip` 把浮层渲染在触发 span **内部**（EP 是 teleport 挂 body，本仓没有传送门），
    `position:absolute` 的裁剪来自「最近的非 visible overflow 祖先」那条链：
    `.cell(hidden) → .el-table__body-wrapper(auto) → .el-table(hidden)`。单元格 24px 高，浮层 32px 一露头就被吃掉。
    已有的 `ElOverflowTooltip` 救不了这一类：它的浮层只在 `scrollWidth > clientWidth`（文字真被裁）时才弹，
    图标按钮没被裁 ⇒ 判据恒假。
  - **面**：`[Parameter] public bool FixedPopper { get; set; }`（默认 `false`，absolute，与加参数前逐字节一致）。
    打开后悬停时走新增的 `TextClip.MeasureRectAsync`（同一个 JS 函数 `elementTextOverflowState`，
    **只取矩形、不做截断判据），用现成的 `TextClip.BuildFixedPopperStyle` 把浮层钉成 position:fixed +
    视口坐标。三处配套：`BuildPopperStyle` 的 fixed 分支不再拼 absolute 的 `top:100%/left:50%` 锚点
    （fixed 下会把浮层算到屏幕外）；`x-placement` 改读 `EffectivePlacement`，上方放不下翻到下方时箭头跟着翻；
    `BuildFixedPopperStyle` 多一个可选 `gap` 形参（默认仍是 `TextClip.PopperGap`，`ElOverflowTooltip` 一字没改），
    `FixedPopper` 传的是 `ElTooltip.Offset`——否则用户调了 `Offset` 浮层纹丝不动。
    量不到矩形（bUnit 未打桩、元素在悬停与渲染之间被移除）时静默退回 absolute，不让一次 hover 崩掉整页。
  - **验证（新增 6 例）**：`test/Element.ComponentTests/ElTooltipFixedPopperTests.cs` —— 全量
    **2642 通过 / 0 失败 / 0 跳过**（基线 2636 + 6，`Temp/b36-tests-full.log`）。四例钉坐标
    （`left:224px` 中心、`top:492px`＝500−8、`Offset=20` ⇒ `top:480px`、right 档贴右侧、上方只 10px 时翻到
    `top:50px` 且 `x-placement="bottom"`），两条对照：`FixedPopperOff_KeepsAbsoluteAnchors`（不开开关仍是
    `position:absolute` + `bottom:100%` + `translate(-50%, -8px)`）、
    `FixedPopper_WithoutJsReading_FallsBackToAbsolute`。
  - **翻面验牙**：把 `UpdateFixedPopperAsync` 首行改成无条件 `return`（等价于"参数没生效"）⇒
    **失败 4 / 通过 2**，红的正是四条 fixed 用例、两条对照仍绿（`Temp/b36-tests-mut.log`）——断言绑的是新行为。
    还原 `cp Temp/b36-ElTooltip.razor.cs.good` + `md5sum` 一致（`1720ca3b…`），**未用 `git checkout --`**。
  - **出库**：`dotnet build src/Components/Element.csproj -c Release --no-incremental` →
    `dotnet pack -c Release -o nuget`（`nuget/Element.0.10.0.nupkg` 1 756 823 字节 / 18:11）→
    删 `~/.nuget/packages/element/0.10.0` 重装（缓存内 `Element.dll` 18:05，`grep -c FixedPopper` = 2）→
    zyweb 重建 **0 错误** → dev server 重启（`netstat` 指认 5171 监听 PID + `Win32_Process.CommandLine`
    证明是自己的 `dotnet run --launch-profile http` 才停，不动用户浏览器与另外两台 5000/5003 服务）+ `setCacheDisabled` 刷新。
  - **消费侧浏览器实跑**（zyweb 5171 / 1440×900 / 真 `Input.dispatchMouseEvent`）：逐颗悬停四颗图标钮，
    浮层文本分别等于「新增同级/新增下级/编辑/删除」、computed `position:fixed`、
    **`document.elementFromPoint(浮层中心)` 命中的就是浮层自身**（这条才是"没被裁"的正证），移开后残留 0 个；
    操作列实际宽 **184px**（改前 477），四颗 36×24 同一行、缝 4px、`aria-label` 齐 ⇒ `_tmp_probe/h95.mjs` **21/21**。
    宿主环境该表 0 行（无 `unitsurl`），那一轮是临时在源码里塞一行假数据让真实组件树画出按钮与 tooltip，
    量完即摘；摘除重建后 `_tmp_probe/h96.mjs` **9/9**（列宽未回弹、无横向溢出、无黄条、无异常）。
    **截图订正**：先前留档的 `h95.png` 与 `h95-hover.png` md5 相同（`shot95.mjs` 的悬停分支要第三个参数才走，当时没传）
    ⇒ 那两张不算悬停证据；改在临时路由 `/probe-h97`（内存三行、列与操作列逐字抄自真页，不动交付文件）重拍，
    `h97-plain/h97-hover/h97-hover-crop` 三张互不相同，删临时页重建后 `_tmp_probe/h98.mjs` 复验该路由已 404、真页仍是交付态。
    详见 `docs/tooltip-fixed-popper.md`。
  - **如实记**：① 浮层坐标是**悬停那一刻**测的，之后不跟随（行滚动、表格重排、窗口 resize 都不重算，
    移开再移回来才会）——EP 有 popper.js 的 update 循环，本仓这条按"hover 一次量一次"够用；
    ② 祖先带 `transform/filter/contain` 时 fixed 仍会被裁（包含块不再是视口），对话框/抽屉那种容器里要用要另验；
    ③ `ElPopover` 有独立的定位代码路径，本批**没有**给它加同名开关；
    ④ `docs/组件使用说明.md` 的 ElTooltip 参数表是早先自动生成的，本来就漏了 `Enterable`/`AutoClose`，
    本批只补 `FixedPopper` 一行，整表重生成不在本批范围内。

- 🟢 **批次㊳ · 新增 `ElImage.Preview` 开关：嵌在触发器里的图不再"任何一张都可点放大"**（2026-10-14）。
  - **触发点**：宿主 `zyweb` 顶栏头像截图反馈「点击这个图片不要放大，如果组件没有这个属性，加上；」。头像挂在
    `ElDropdown TriggerMode="Click"` 的 `Trigger` 里，点开的是账号菜单，图片查看器一盖就把菜单挡住了。
  - **缺口是真身**：`ElImage` 原本没有关闭预览的入口——`PreviewSrcList` 不传时 `PreviewList` 回落到 `Src`，
    `ShowViewer => showViewer && PreviewList.Any()` 因此恒真，等于「凡是带 `Src` 的图都能点开放大」。
  - **面**：`[Parameter] public bool Preview { get; set; } = true;`（默认 `true`，既有页面行为一字不变）。
    开关只落在**一处**：`PreviewList` 在 `!Preview` 时于源头返回空列表，`ShowViewer`、查看器渲染、
    `OpenPreviewAsync` 的早退全部以它为准——避免出现「一个开关三处判断」而三处口径漂移。关掉只影响查看器，
    `OnClick` 照常回调（头像那种「点了要走别的事」的宿主不受影响）。
  - **验证**：`test/Element.ComponentTests/ElImageTests.cs` 本批**新增 4 例**（该类由 1 例 → 5 例），基线 **2626 → 2630**。先红后绿：红轮 **失败 2 / 通过 3**
    （`Assert.Empty() Failure: Collection was not empty`，两条 `PreviewFalse_*` 都拿到真实渲染出的查看器 `div`），
    补上源头置空后 **5/5 绿**；全量 **2630/0/0**。含两条必要对照：`PreviewDefault_StillOpensViewerFromSrcFallback`
    （不传 `Preview` 时回落 `Src` 仍能打开，证明默认值没被我的改动带偏）、`PreviewFalse_WinsOverExplicitPreviewSrcList`
    （显式给了 `PreviewSrcList` 也被开关压住）。日志 `Temp/verify_batch34_image_red.log`、`_green.log`、`_fullsuite.log`。
  - **消费侧浏览器实跑**（zyweb 5171 / headless chromium 1280×720 / CDP 真实 `Input.dispatchMouseEvent`，同一页面同一环境做正反对照）：
    负——点 `.zy-avatar img` 后 `viewer:0`，而 `.el-dropdown-menu` 同时开着且条目是「密码修改 / 安全退出」，
    说明点击确实被触发器接住了、不是落空；正——点品牌 logo（默认 `Preview`）得到 `viewer:1 / mask:1 / src images/72.png`，
    点关闭后 `viewer:0`。证据 `_tmp_probe/batch34v4.out`、`batch34v4-A-avatar-click.png`、`batch34v4-B-logo-click.png`。
  - **红过一次的是探针，不是产品**：上一版同一断言报 `viewer:0`（正向对照组也"失败"）。查清是我早前探针留下的
    `sessionStorage['tmp_lockscreen']=true`，`MainLayout.OnAfterRenderAsync` 每次载入据此自动弹「锁定屏幕」，
    全屏 `.el-overlay-dialog` 吃掉了点击（`elementFromPoint(69,28)` 返回的就是遮罩本身）。改为种 `false` 并在点击前
    断言可见遮罩数为 0，正向对照立刻变绿。
  - **出库**：`dotnet build src/Components/Element.csproj -c Release --no-incremental` → `dotnet pack -c Release -o nuget`（不带 `-p:Version`）
    出 `nuget/Element.0.10.0.nupkg`（15:41，1 754 749 字节）→ 删 `~/.nuget/packages/element/0.10.0` 重装（缓存内 `Element.dll` 时间戳 15:41）
    → zyweb 重建 **0 错误**、dev server 重启 + ignoreCache 刷新。zyweb `MainLayout.razor` 带着 `Preview="false"` 编译通过，
    这条本身即「刷新后的包确实带上了新属性」的证据。
  - **文档**：`docs/image-preview-switch.md`（新增，含「为什么真相只放一处」和消费侧口径）；`docs/组件使用说明.md` 的 ElImage 参数表补 `Preview` 行 + 一条示例。
  - **如实记**：① `docs/组件使用说明.md` ElImage 示例行写的 `ShowViewer="true"` 引用的不是公共 `[Parameter]`（`ShowViewer` 是受保护的计算属性），
    属既有文档缺陷，本批**只报告、不顺手改**，避免把没验证过的东西写进示例；② zyweb 品牌 logo 那张 `ElImage` 仍带预览（用户没提，未擅自改），
    已作为选项向消费方提出。
- 🟢 **批次㉛ · 新增 `FullScreenService`（浏览器全屏服务），补 zyweb 迁移缺的服务面**（2026-10-13）。
  - **触发点**：宿主 `zyweb`（Blazor WASM，net10.0）把 Radzen.Blazor + BootstrapBlazor 换成本库时，
    顶栏全屏按钮写的是 `@inject FullScreenService` + `fullScreenService.Toggle()`，本库没有同名服务，
    编译期 `CS0246`。这是消费侧提出的真实缺口，不是 speculative 扩展。
  - **面**：`src/Components/FullScreenService.cs`（Scoped，`AddElementServices()` 注册）
    + `wwwroot/js/dom.js` 追加 `elementIsFullscreen` / `elementFullscreenEnter` / `elementFullscreenExit` /
    `elementFullscreenToggle` 四个全局函数。方法一律返回**切换后的布尔状态**，`ToggleAsync` 单次往返完成读-判-做。
  - **三条口径决定**（详见 `docs/full-screen-service-alignment.md`）：① 无 target 时全屏 `document.documentElement`
    而不是 `body`（body 全屏出黑边）；② `requestFullscreen` 的 Promise 被拒绝（非手势、权限、目标已移除）时
    `.then/.catch` 双双回落成 `elementIsFullscreen()`，绝不让 JS 异常冒到 C#——WASM 侧表现为按钮"没反应"的未捕获 `JSException`；
    ③ 前缀回落 `webkit/moz/ms`，取不到方法返回当前状态而非抛错。ESC 退出不会回灌 Blazor 事件（不做长驻 `DotNetObjectReference` 回调），
    需要跟随状态的宿主自己 `IsFullScreenAsync()` 查询——这条写进文档的「已知边界」，不当缺陷藏起来。
  - **验证**：`test/Element.ComponentTests/FullScreenServiceTests.cs` **9/9 绿**（解析得到服务、四个方法命中各自标识符与
    参数个数、`ElementReference` 重载真把引用传下去、JS 返回值透传、以及「C# 调用名 ↔ `dom.js` 里 `window.*` 定义」同名守卫 +
    ES5/`['catch']`/webkit 前缀守卫）。全量 **2594/0/0**，见 `Temp/fullsuite-rerun2.log`、`rerun3.log`、`rerun4~6.log`。
  - **如实记两点**：① **真实浏览器全屏探针本批没做**——headless 要额外授权且手势语义受限，并到消费方 zyweb 顶栏按钮的
    浏览器验收里补，在此之前「webkit 分支实际生效」只有源码级证据；② 本批第一次全量跑有 **1 条红**
    （`ElFormGeneratedControlTests.GeneratedMentionSelectsOptionAndUpdatesModel`，与本批无关的时序敏感用例），
    **该轮日志只留了 `tail -12`，断言原文丢了**；随后 5 次全量 + 单例 + 整类都绿。当时唯一的环境差异是那一轮与一次
    `dotnet build`（65 个 razor 文件的重编译）并发跑，**"并发负载导致防抖用例落败"只是候选解释，未做否证实验，故真身记为未定位**，
    不写成"已解释"。

### Changed

- 🟡 **批次㊴ · 服务化 Confirm/Alert 的按钮回到官方 `.el-dialog__footer`：靠右、次按钮在前、主按钮吃 Enter**（2026-10-15）。
  - **触发点**：宿主 `zyweb`「安全退出」截图对比官方页反馈「这个对话怎么不如官方的好看呢，https://element-plus.org/zh-CN/component/dialog，你看看官方的，是不是没用 eldialog 这个组件」。
    追问改到哪一层，消费方选定**只改确认/警告框**（普通业务对话框一律不动）。
  - **先否证提问的前提**：框**确实**走 `ElDialogGrow`——DOM 上 `.el-dialog` / `.el-dialog__header` / `.el-dialog__title` / `.el-dialog__body` 一个不缺（`ElDialogGrow.razor:65-91`）。
    差的不是组件，是服务面：`ConfirmAsync` / `AlertAsync` 把按钮当**正文的一部分**内联渲染（自建 `.el-dialog-grow__confirm-buttons` 塞在 `<p>` 后面），
    官方 `.el-dialog__footer` 节点因此压根没生成，观感上三个偏差全来自这一条——按钮**居中**而非靠右、**主按钮在前**、间距被自写成 24px。
  - **面**：`ElDialogGrowService` 新增私有 `BuildConfirmFooter(dlg, okText, cancelText)`，`Confirm`/`Alert` 统一挂 `options.FooterContent`（`Alert` 传 `cancelText=null` ⇒ 只出主按钮），
    正文只剩那一行 `<p class="el-dialog-grow__confirm-message">`。按钮序按官方口径 **次按钮在前、主按钮在最后**，主按钮带 `Type=Primary` + `Autofocus=true`。
  - **换序会把 Enter 交给「放弃」——这是本批唯一的真实行为风险，不是纯 CSS**：`AutoFocusFirstElement` 默认 `true`，`elementDialogFocusFirst` 原实现是「焦点给第一个可见元素」，
    而对话框只绑了 Escape。按钮一换序，默认焦点就落在取消位上。改法在 `wwwroot/js/dom.js:4621` 的 `elementDialogFocusFirst` 里加一层**显式 `autofocus` 优先**
    （遍历 `visibleFocusables(root)` 先认 `hasAttribute('autofocus')`，没有才回落到第一个）。这样视觉序照官方，Enter 语义照旧＝确定。
  - **CSS**（`wwwroot/css/index.css`）：`.el-dialog-grow__confirm-buttons` 从 `justify-content:center` 改 `display:flex;justify-content:flex-end`，
    **并删掉自写的 24px 按钮间距**——EP 基础规则 `.el-button + .el-button{margin-left:12px}` 本来就是 12px，24px 是两个口径叠错的产物；
    批次㉞ 的 `.el-dialog__body.el-dialog-grow__confirm-body` 内边距 `0 20px 14px` → `0 20px`（底部余量现在由 `.el-dialog__footer` 自己的 `padding:10px 20px 20px` 给，正文不该再留一条）。
    类名 `el-dialog-grow__confirm-buttons` **保留**（只换父级），宿主写过的覆写仍命中。
  - **验证**：`test/Element.ComponentTests/ElDialogGrowServiceTests.cs` 本批**新增 2 例**
    （`ConfirmAsync_ButtonsLiveInFooterWithPrimaryLast`：footer 内含按钮条、文字序 取消→确定、index 1 带 `el-button--primary` 与 `autofocus`、
    并 `Assert.Empty(.el-dialog__body button)` 证明按钮真的搬走了；`AlertAsync_SingleOkButtonLivesInFooter`）。
    原有三例的点击辅助 `cutInvokeConfirm` 索引随之翻转（`ok ? 1 : 0`）——**这是断言跟着口径改，不是放宽**。全量 **2632 通过 / 0 失败**。
  - **消费侧浏览器实测**（zyweb 5171 / headless chromium 1280×900 / CDP，同一入口取改前改后；改前截图 `_tmp_probe/dlg60-confirm-before.png`，
    出库件复测截图 `dlg61-confirm-final.png`，读数日志 `dlg60.out` / `dlg61.out` / `dlg61b.out`）：

    | 读数 | 改前 | 改后 |
    | --- | --- | --- |
    | `.el-dialog__footer` 是否存在 | **false** | **true**（`padding:10px 20px 20px`，`text-align:right`） |
    | 按钮条父级 | `el-dialog__body` | `el-dialog__footer` |
    | `justify-content` / `text-align` | `center` | **`flex-end` / `right`**（按钮条右边缘与 footer 的 20px 内边距**齐平，偏移 0**） |
    | DOM 序 | `确定[primary]` → `放弃` | **`放弃` → `确定[primary]`** |
    | 次/主按钮 `margin-left` | 24px | **12px**（EP 基础值） |
    | 默认焦点 | `放弃`（第一个） | **`确定`**（`hasAutofocus=true`，实测 `{tag:BUTTON,text:确定,cls:el-button el-button--primary}`） |
    | 对话框尺寸 | 420×134 | 420×150 |
    | 正文区 `padding` / 正文内按钮数 | `0 20px 14px` / 2 | **`0 20px` / 0** |
    | 「提示底 → 按钮顶」实测间距 | 未单测（按钮在正文内、消息 `mb 12px` 之后） | **22px**（消息 `mb 12` + footer 上内边距 `10`，与 EP MessageBox 同档） |
    | 页面 JS 异常 | — | `[]` |

  - **正向对照（证明「只改确认/警告框」真的只改了这两类）**：同一轮浏览器里打开业务对话框「密码修改」读同一把尺——
    正文 `padding:10px 20px 30px`（EP 原样 + 批次㉝ 的 `padding-top:10px`）、无 `confirm-body` 类、`有footer节点=false`、
    按钮仍在正文里、默认焦点仍是**第一个**按钮（`放弃`，`hasAutofocus=false`）。即 `[autofocus]` 优先那层对没标 `autofocus`
    的既有对话框**零影响**，回落路径实测可用。
  - **如实记**：① 高度 +16px 是 footer 内边距换来的，正文不再自留 14px 底边距，总高略增但上下留白归到组件本来的口径；
    ② Enter=确定 依赖 `[autofocus]` 优先这一层，若宿主将来给别的按钮也标 `autofocus`，DOM 里第一个命中的赢——
    文档写明，不藏成"组件会自动找主按钮"；③ 上面那条业务对话框对照顺带读到它的按钮里有一个 `class="btn btn-secondary"`
    的裸 Bootstrap 按钮（真身在消费侧 `zyweb/Pages/Common/WaitingPopup.razor:10`，属宿主迁移遗留、非本库缺陷），
    按口径**只报告不顺手改**，归入消费方的 Bootstrap 遗留清单。
  - **出库**：`dotnet build src/Components/Element.csproj -c Release --no-incremental`（**0 警告 / 0 错误**）→ `dotnet pack -c Release -o nuget`（不带 `-p:Version`）
    → `nuget/Element.0.10.0.nupkg` 1 755 199 字节（06:29）→ 删 `~/.nuget/packages/element/0.10.0` 重装 →
    校验**装机的** `staticwebassets/css/index.css` 确实是新版（`.el-dialog-grow__confirm-buttons{display:flex;justify-content:flex-end}`、
    `confirm-body{padding:0 20px}`）→ zyweb 重建 **0 错误**（1146 个警告，全是既有的）→ dev server 重启 + ignoreCache 刷新，
    上表全部读数取自这最后一份出库件。
  - **文档**：`docs/dialog-alignment.md` 新增「服务化 Confirm/Alert 的页脚归位（批次㊴）」一节，含改前/改后表与 `[autofocus]` 优先的边界。

- 🟠 **批次㊲ · 定高对话框的正文这回真的能滚了（flex 列链），并补出面：标题栏动作条 `.el-dialog-grow__header-actions`**（2026-10-14）。
  - **触发点**：宿主 `zyweb` `/baseinfo/employeeinfo` 三张截图反馈「这个排版怎么全乱了，新增的时候，没显示全」「能不能把放弃和确定的按钮和标题栏放在一起，放到右边不影响关闭图标」「解除锁定这个按钮要和标题栏放在一起」。
  - **真身①：`没显示全` 不是没写 overflow，是那条规则是假的**。旧 CSS
    `.el-dialog-grow[style*="height"] > .el-dialog__body{overflow:auto}` 只声明滚动，没给正文盒子高度上限——块级布局下正文高度等于内容高度，滚动条永不出现。
    浏览器实测（1280×720，部门编辑 `height:500px`）：卡片 `108..608`，正文框 `162..678`（比卡片底**低 70px**），
    `clientHeight 516 == scrollHeight 516` ⇒ 没有可滚量，多出来的 70px 直接垂到卡片外面。
    改法是**列 flex 链**：卡片 `display:flex;flex-direction:column`、header/footer `flex:0 0 auto`、
    正文 `flex:1 1 auto;min-height:0;overflow:auto`。`min-height:0` 是这条链的命门——flex 子项默认 `min-height:auto`，
    会被内容顶回原高度，`overflow:auto` 依旧不生效。打标条件 `!Fullscreen && EffectiveHeight` 有值，修饰类 `el-dialog-grow--fixed-height`。
  - **真身②：标题栏动作条的每个像素都是量出来的**。header 高 54（`padding:20px 20px 10px`），标题/关闭图标所在带在卡片内
    `20..44`，其中关闭图标 24×24（视口 `128..152`，即卡片内 `20..44`），中线 **140**；`ElButton` 高 32 ⇒ `top:16px` 时动作条中线同为 140。
    右让位 `right:52px` = 内缩 20 + 图标宽 24 + 间隙 8；改造前的动作条右缘 945 与关闭图标右缘 945 **完全重合**，这就是「压到关闭图标」的实测形态。
  - **为什么是 CSS 类而不是插槽**：`ElDialogGrowOptions` 只有 `TitleContent/FooterContent/ChildContent`，
    而业务上**按钮的 `OnClick` 归子组件、options 归父页面**（`DialogService.OpenAsync<T>` 的固有分工），把按钮提到 TitleContent 就要把处理函数往下传一层；
    绝对定位类 `.el-dialog-grow__header-actions` 的包含块是 `.el-dialog`（`position:relative`），于是动作条**天生不随正文滚动、也不受正文 overflow 裁剪**，宿主一句 `class=` 即可，库面无需新插槽。
  - **口径：关闭图标的显隐只有组件知道，但宿主要用它决定让位多少** ⇒ 组件在 `!ShowClose` 时输出修饰类 `el-dialog-grow--no-close`，
    CSS 里 `--no-close` 下动作条改 `right:20px`。**这里刻意没用 `:has()`**：本仓 `index.css` 有浏览器下限守卫
    （`ElFormFieldGrowTests.Css_FormFieldSection_AvoidsUnsupportedCssFeatures`、`ElMarkdownGrowTests.CssUsesOnlyWidelySupportedFeatures`
    各自从本段标记读到文件末尾，`:has(`/`:is(`/`gap:`/多参 `:not()` 全在禁列，理由是「一旦引入即在老浏览器（Chromium 75 / Chrome 90）上整条规则失效」）。
    如实记：第一版我就是写了 `:has()`，全量 **2624/2** 的两条红正是这两条守卫报的；**没有削弱守卫**，改成修饰类 +
    `.el-button + .el-button{margin-left:8px}`（原本要用 `gap:8px`，同样在禁列）。
  - **验证**：新增 `test/Element.ComponentTests/DialogFixedHeightHeaderActionsTests.cs` **11/11 绿**，两条正向对照是必须的——
    ① 未定高（服务 `Confirm`/`Alert` 的 `Height` 就是 null）不打链，批次㉞ 的正文上贴 padding 不受影响；② 全屏 + 定高不打链。
    全量 **2626/0/0**。重打包 `Element.0.10.0.nupkg`（14:59，1 754 220 字节）→ 清 `~/.nuget/packages/element/0.10.0` →
    zyweb 重建 0 错 → dev server 重启 + ignoreCache，真实浏览器读数：
    部门编辑 `spillPx 0`、正文 `clientH 446 / scrollH 489 / 请求 60 实到 43`、`headerPinned true`、
    动作条中线 140 == 标题 140 == 关闭图标 140、`rightInsetToDialog 52`、离关闭图标 8px、
    放弃 `bg rgb(255,255,255) color rgb(96,98,102)`、确定 `el-button--primary bg rgb(64,158,255)`；
    锁定屏幕 / 密码修改（`ShowClose=false`）自动带 `--no-close`、让位 20px、无溢出无假滚动条。
  - **悬案已结：有界正文会不会裁掉下拉浮层——不会，但原因和我原先的判断相反**。本批一度按「浮层内联在组件子树里」推导出风险
    （人员编辑末行字段底 422 + 菜单 ~270 = 692 > 正文底 608 ⇒ 会被裁且没得滚）。实测推翻它：
    款号编辑正文 `clientH 666 / scrollH 1127` 真能滚，用 CDP 真实鼠标事件点开最靠下的 `.el-select` 后，
    浮层 `.el-select-dropdown.el-popper` 的祖先链是 `DIV.（应用根弹层）→ BODY`，`dialog.contains(popper) === false` ——
    下拉是经 `PopupService.SelectDropDownOptionsCore` 由**应用根的 `ElPopup` 实例**渲染的，不在对话框子树内，任何正文 overflow 都裁不到它
    （源码侧对证：`ElSelect.razor` 整份 markup 里没有 `ElPopup`）。探针 `zywebGroup/_tmp_probe/menu32.mjs`、`menu32b.mjs`。
  - **同一次实测暴露的既有边界（本批未改，汇报）**：浮层 left/top 是打开瞬间按 document body 算死的
    （`ElPopup.razor.cs:850-853` `GetTopRelativeBodyAsync/GetLeftRelativeBodyAsync`），滚动对话框正文时浮层**不跟字段走**。
    造不出真实证据：本环境选项列表为空（浮层壳高 14px、`items 0`），而口径禁止手动塞 `unitsurl` 造数据，故记为「结构推断，未实测」。
  - **另记**：款号编辑（`width/height:100%` + `margin-top:15vh`）卡片 `108..828` 超出 720 视口，但
    `.el-overlay-dialog`（`position:fixed;overflow:auto`，`scrollH 878 / clientH 720` ⇒ 可滚 158）滚到底后卡片变 `-50..670`，完整可达——
    「定高 100%」这类参数不会再有内容永久不可达，代价是正文和遮罩要滚两次。

- 🟠 **批次㊱ · 空态占位的几何补齐：树空态不再压在搜索框上、表空态不再被裁半截**（2026-10-14）。
  - **触发点**：宿主 `zyweb` 员工维护页截图反馈「无数据显示在这里对吗」——部门树过滤到 0 个节点时，「无数据」三个字贴在树搜索框的下边框上。
  - **真身（浏览器 `getBoundingClientRect` 实测，1280×720，不是估的）**：`.el-tree__empty-text` 是
    `position:absolute; top:50%; left:50%; transform:translate(-50%,-50%)`，居中基准必须是带 `min-height:60px` 的
    `.el-tree__empty-block`；本仓 `ElTree.razor` 把 text **直接挂在 `.el-tree` 下面**（两处分支：无数据源、过滤到 0 节点），
    于是基准退化成 `.el-tree` 自己——而树空时它高度为 **0**（实测 `.el-tree` rect `h:0`，文字 rect 顶 y=307 比树容器顶 317 还高 10px
    ⇒ 上半截盖在搜索框上）。
  - **同屏另一条（表）**：`.el-table__empty-block` 的内联样式此前写死 `height:60px`（`EmptyBlockStyle` 的「无 Height/MaxHeight」分支），
    宿主给自定义 `<Empty>` 片段（zyweb 用 133px 的 `ElEmpty`）时被裁掉半截——实测表格框底 392、空态文字在 y=395，已在框外。
    EP 那条规则本来就是 `min-height`，写死 `height` 是本仓自己加的。
  - **改法**：`ElTree.razor` 两处空态各包一层 `<div class="el-tree__empty-block">`——**CSS 一行都没加**，
    vendored EP 规则里 `.el-tree__empty-block{position:relative;min-height:60px;...}` 早就在，只是没有代码渲染这个节点；
    `ElTable.razor.cs` 的 `EmptyBlockStyle` 拆两支：无定高 ⇒ `min-height:60px`（让内容自己撑开），
    有 `Height`/`MaxHeight` ⇒ 仍按 `max(60, 高度-表头高)` 给死值（定高表格的空态要落在可见正文区中间，这条顺手改成 min-height 就会退化）。
  - **验证**：新增 `test/Element.ComponentTests/EmptyStateGeometryTests.cs` 5 条。改前 **4 红 1 绿**
    （3 条树：`Assert.Single() Failure: The collection was empty`；1 条表：内联 style 里没有 min-height；
    绿的那条是**定高正向对照**，钉住「有 Height 时仍给死值」，防止把两支一起改坏）。
    改后 5/5 绿，全量 **2617/0/0**（基线 2612 + 本批 5），见 `Temp/verify_batch36_empty_red.log`、`…_green2.log`、`…_fullsuite.log`。
    重打包 `Element.0.10.0.nupkg`（14:00，1 753 176 字节）→ 清 `~/.nuget/packages/element/0.10.0` → zyweb 重建 **0 错误**。
    浏览器复测（1280×720）：树空态块 `h:60`、文字中线 314 已在搜索框底 284 之下（`treeOverlapSearch:false`）；
    表格空态块 style 为 `min-height:60px`、`el-empty` 底 499 ≤ 表格框底 505（`tableEmptyClipped:false`），无未捕获异常。
  - **如实记三点**：① 本批只补**几何**，空态**文字**口径一字未动——树仍回落「无数据」（接了 `el.tree.emptyText` 词条才是 EP 的「暂无数据」），
    表是「暂无数据」；两者不一致属既有状态，只汇报不擅改。② 树空态现在是在搜索框下方那条 60px 带里居中（EP 原样），
    **不是**在整个左栏（实测 434px 高）里垂直居中；要不要填满左栏属宿主布局决定，库侧没替宿主做主。
    ③ 消费侧顺带量到本页在**受控**视口 1280/1024/900 下 `documentElement` 横向溢出均为 **0**，
    而此前一次冒烟在**未受控**的浏览器窗口尺寸下读到 28→295 —— 两次前提不同不可比，这条**记为未定位**，
    不写成「已解释」，也不归因给那排按钮。

- 🟠 **批次㉟ · `ElTree` 当前节点高亮收敛为唯一：逐个点一遍不再"全部选中"**（2026-10-14）。
  - **触发点**：宿主 `zyweb` 的 `/baseinfo/employeeinfo` 部门树截图反馈——「都变成选中了，如果逐个点击一遍」，
    图上 `0001 / 23232 / 3232323` 三个节点同时带蓝底。这是 ㉝ 那条"树当前节点可见"改样的后续：底色变明显后，
    **高亮叠加**这个既有行为缺陷才第一次肉眼可见。
  - **真身（源码两行，不是样式问题）**：① `ElTree.Features.cs:231` 的 `is-current` 判定原本分叉——
    勾选模式按 `ReferenceEquals(_currentNode, node)`（天然唯一），独立树却按 `selectedNodes.Contains(node)`（集合可含 N 个）；
    ② `SelectFeatureNodeAsync`（原 `:409`）往 `selectedNodes` 里**只增不减**（`if (!Contains) Add`）。
    两条叠起来 ⇒ 独立树每点一个节点就往选中集追加一个，点过的全都命中 `Contains`。
    顺带：宿主 `EmployeeInfo.razor:265 OnTreeSelectedAsync` 取的是 `nodes[nodes.Count - 1].Id`，
    靠"最后一个是当前"**侥幸**得到正确部门码，**过滤逻辑因此没出过错**——出错的是视觉和事件语义（订阅方拿到的不是"当前选中"而是"历史点击序列"）。
  - **口径**：EP 的 `el-tree` 只有一个 `current-node-key`（`setCurrentKey` 语义即唯一当前节点），`is-current` 至多命中一个；
    多选靠 checkbox，不靠高亮堆叠。本库勾选模式 `ApplyTreeSelectCheckAsync`（`:831-836`）已经是 `Clear()` + 只装一个，
    所以改法是**把独立树对齐到勾选模式已有的那条口径**，不新增开关、不加参数。
  - **改法**：`ElTree.Features.cs:231` 统一为 `.AddIf(ReferenceEquals(_currentNode, node), "is-current")`；
    `SelectFeatureNodeAsync` 内改为 `selectedNodes.Clear(); selectedNodes.Add(treeNode);`。
    `_currentNode` 的赋值点（`:430-432`）一字未动，所以"当前节点"的来源仍是最后一次点击。
  - **验证**：新增回归 `test/Element.ComponentTests/ElTreeTests.cs::ClickingSeveralNodesHighlightsOnlyTheLastOne`
    （点两次 ⇒ `is-current` 只 1 个，且 `SelectedNodesChanged` 收到 `[2]`；中间的单点断言作正向对照）。
    **改前先红**：`Assert.Single() Failure: The collection contained 2 items`（见 `Temp/verify_batch35_tree_red.log`）；
    **改后绿** 1/1（`…_green.log`），全量 **2612/0/0**（基线 2611 + 本批 1 条，`…_fullsuite.log`）。
    重打包 `Element.0.10.0.nupkg`（12:19，1 753 167 字节）→ 清 `~/.nuget/packages/element/0.10.0` 重装 → zyweb 重建 **0 错误** → 重启 5171。
    浏览器实测（`Runtime.evaluate` 读 class 与 `getComputedStyle`，见 `…_browser.out`）：静态四节点树逐个点完，
    `currentCount` **始终为 1**、高亮文本跟着点击走（`0001→23232→3232323→另一个部门`），
    宿主标记每次只收到 1 个节点（`last=N/1`），底色 `rgb(236, 245, 255)`＝㉝ 定的 `--el-color-primary-light-9`。
  - **如实记四点**：① **本批改的是行为面不是纯样式**——订阅 `SelectedNodesChanged` 且依赖"累积历史"的写法会变；
    zyweb 三个 `ElTree` 消费点里只有 `EmployeeInfo` 订阅该事件（取最后一条，不受影响），
    `MaterielInfo.razor:42` / `MaterielSelect.razor:29` 只用 `OnNodeClick`。② 验收环境部门树 **0 节点**（`unitsurl` 为空、无 SM2 后端，
    按约定不手动造数据），所以上面的点击证据出自**临时静态探针页**，该页已删除（删后重建 0 错误，`grep ZzProbe` 干净）；
    **真实部门数据下"选部门→过滤员工表"的链路仍未验**，等后端可达补。③ `ElTree.razor.cs:183 SelectNodeAsync` 是同一累积写法的
    **死代码**（全仓零调用者，已 grep 证实），按"既有 bug 只汇报不擅改"本批未动。
    ④ 探针第一版只点到 4 个里的 2 个——`ExpandOnClickNode` 默认 `true`，点父节点会先折叠掉子树；
    加 `ExpandOnClickNode="false"` 才跑满四个，这条属于测试脚手架口径，不是库的缺陷。

- 🟡 **批次㉞ · 服务化 Confirm/Alert 按 EP MessageBox 口径收窄：宽度 420px、正文贴标题**（2026-10-14）。
  - **触发点**：宿主 `zyweb` 拿「安全退出」确认框截图反馈两条——「`是否退出系统?` 要靠上一点，所有对话框 body 里面的内容都不要垂直居中」「这个对话框的宽度太宽了，没有一点美感」。
  - **真身（浏览器实测 `getComputedStyle`，638×570 视口，不是估的）**：确认框 DOM 是 `.el-dialog__body` 里直接放
    `p.el-dialog-grow__confirm-message`（高 22px）+ `div.el-dialog-grow__confirm-buttons`（高 32px），body 内边距
    `10px 20px 30px`（上边距已被 ㉝ 收紧）——**下边距是上边距的三倍，一行提示被夹在中间，观感就是"垂直居中"**；
    宽度走 `ElDialogGrowHostItem` 的默认 `50%`，1500px 屏上就是 740px，一行字拉成横幅。
    注意：全程**没有**任何 flex 居中，`align-items` 只出现在 `.el-dialog-grow__wrapper.is-align-center`（那是把**对话框**在视口里竖直居中，EP `align-center` 口径，未动）。
  - **口径**：EP MessageBox 的宽度是 `--el-messagebox-width: 420px`，确认/警告框按它走；正文区上内边距归零
    （标题栏本身有 10px 下边距）、左右 20、下 14，消息下外边距 16→12，垂直余量只留在底部。
  - **改法**：`ElDialogGrowService` 新增私有 `ApplyConfirmLayout(options)`，`ConfirmAsync` / `AlertAsync` 各调一次——
    `Width` 为空才填 `420px`（**调用方显式给过就不覆盖**），并往 `ContentCssClass` **追加**（不是替换）`el-dialog-grow__confirm-body`；
    `index.css` 末尾加 `.el-dialog__body.el-dialog-grow__confirm-body{padding:0 20px 14px}` 与
    `.el-dialog-grow__confirm-body .el-dialog-grow__confirm-message{margin:0 0 12px}`。
    类名由服务打标、样式仍留在 `index.css`（不建独立 CSS 文件），业务侧一行代码都不用改。
    **顺序是硬要求**：`confirm-body` 那条必须排在 ㉝ 的 `.el-dialog__header + .el-dialog__body{padding-top:10px}` **之后**——
    两者特异性同为两个类，写在前面的会被后者把 `padding-top` 顶回 10px。
  - **验证**：`ElDialogGrowServiceTests` 加 2 条（默认 420px + 挂了 `confirm-body`；显式 `Width="30%"` 不被覆盖但仍挂类），
    全量 **2611/0/0**（基线 2609 + 本批 2 条），见 `Temp/test_batch34_confirm.log`。
  - **如实记三点**：① 业务侧「所有对话框 body 内容不要垂直居中」这句是**泛指**，本批只改了服务化的 Confirm/Alert 两类框；
    普通 `OpenAsync` 对话框的 body 仍是 `10px 20px 30px`（上下不对称已经偏上贴），要不要再收紧等 zyweb 逐框反馈，不预先全局改——
    那会连带动到既有对话框的高度/滚动断言。② 测试是 bUnit 的**标记级**断言（style 字符串 + class 命中），
    真实渲染几何靠浏览器复测补——**本批重打包后已复测**（见 `docs/dialog-alignment.md`「重打包后的消费侧实测」）：
    `.el-dialog.el-dialog-grow` 宽 **420px**、`.el-dialog__body` 的 class 含 `el-dialog-grow__confirm-body`、
    计算样式 `padding:0px 20px 14px`、正文 `p` 相对 body 顶偏移 **0px**、body 高 110px→**80px**；
    「放弃」关闭后 `.el-dialog-grow` / `.el-overlay-dialog` 均归零，没有残留层。
    但**截图没拿到**（in-app browser 无可见表面，`take_screenshot` 返回 `NATIVE_BROWSER_VIEWPORT_UNAVAILABLE`），
    所以以上仍是 DOM 计算样式读数，不是视觉验收结论。③ `test/Element.Test.csproj`（旧的 `SetupTest.cs` 那一套）
    编译不过：`CS0234 命名空间"Element"中不存在类型或命名空间名"Admin"`——**与本批无关的既有状态**（本批没碰 Admin 相关任何文件），
    2609/2611 的基线一直出自 `Element.ComponentTests`，此处只记录不修。

- 🟡 **批次㉝ · 三处纯视觉口径收紧：表单字段边框去叠影、树当前节点可见、对话框标题下空白带收窄**（2026-10-14）。
  - **触发点**：宿主 `zyweb` 迁移到本库后在同一屏里给出三条反馈——「`ElFormFieldGrow` 边框太粗」「当前选中（树节点）没有选中显示」「红线处留白太多」。
    三条都是**样式面**，组件的 C# 行为、参数、判定逻辑一律未动。
  - **① `ElFormFieldGrow` / `.el-form-field` 边框**（`wwwroot/css/index.css`）。真身：内容盒原本 `border:1px solid` **之上又叠**了一层
    `:focus-within` 的 `box-shadow: inset 0 0 0 1px`，静止态 1px、聚焦态视觉上 **2px**——这就是"边框太粗"的来源。
    改法是**拆掉叠加层而不是把 1px 改成 0.5px**：新增根变量 `--el-form-field-border-width: 1px`（默认值与原来完全一致，消费侧改这一处即可整体加细/加粗，
    `--flat` 的下划线走同一变量），并**删除** `.el-form-field:focus-within > .el-form-field__content` 的 inset 描边及其 `--text/--flat` 的 `box-shadow:none` 例外
    （例外随主体一起消失，不留"为不存在的效果做豁免"的死规则）；聚焦反馈从此只由 `border-color` 变色承担。
  - **② `ElTree` 当前节点高亮**。真身：EP 原样只有一条 `.el-tree--highlight-current .el-tree-node.is-current>.el-tree-node__content{background-color:#f0f7ff}`，
    白底上 3% 的蓝且不动文字，100% 缩放实际看不出点了哪个。底色改为 `--el-color-primary-light-9`（`#ecf5ff`）——**与 `ElTable` 的
    `tr.current-row>td{#ecf5ff}` 同值，从此树和表的"选中"是同一个颜色**；文字加主色 + `font-weight:500`。
    `is-current` 的判定（独立 `ElTree` 按 `selectedNodes` 累积）一字未改，所以行为断言面为零。
  - **③ `ElDialog` / `ElDialogGrow` 有标题时的正文上边距**。真身用浏览器 `getComputedStyle` 打真值（不是估的）：
    `.el-dialog__header{padding:20px 20px 10px}`（整条 54px 高）+ `.el-dialog__body{padding:30px 20px}` ⇒ 标题文字底到正文第一行 **40px 纯白**；
    业务对话框正文第一行通常就是工具条按钮，看起来像"标题和内容断开了"。EP **没有**覆盖该内边距的 API，只能在库侧收。
    改为 `.el-dialog__header + .el-dialog__body { padding-top: 10px }`。**相邻兄弟选择器**是刻意的：`ShowTitle=false` 时不渲染 header，正文因此保持原 30px。
    **两条口径分开写清**，别混着用——本条 CSS 注释说的是"header 底 + body 顶"这一档：**40px → 20px**；
    `docs/dialog-alignment.md` 的探针口径是"标题底 → 正文顶（含正文自身外边距）"：默认 `30 + 14px(p 的 1em) = 44px` ⇒ 收紧后 `10 + 14 = 24px`。
    左右 20px、底部 30px 一律不动，避免连带改到既有宽高断言。
  - **文档同步**：`docs/form-field-alignment.md` §2 的聚焦态片段改为无描边版并记录新变量；`docs/tree-alignment.md` §2「已知差异」增补高亮偏离 EP 一条；
    `docs/dialog-alignment.md` 正文节记录批次㉝ 收紧到 10px，并把旧探针读数标注为改动前值。
  - **验证**：`dotnet build src/Components/Element.csproj -c Release --no-incremental` **0 警告 0 错误**；
    全量 `dotnet test test/Element.ComponentTests -c Release` **2609 通过 / 0 失败 / 0 跳过**、RC=0（整份输出 `Temp/b33-full-test.log`）。
    **基线对照**：批次㉜ 全量为 2599，本批 **CSS-only、未新增用例**，+10 全部来自㉜/㉛ 已入库的
    `ElTableReloadDataTests` / `ElTableGroupedHeaderTests` / `FullScreenServiceTests`（`git status` 里那批未提交文件），**不是本批带来的漂移**。
    反向确认过没有测试断言被删掉的聚焦描边或对话框 `padding-top`，`ce_build/probe_dialog_content_pos.cjs` 也已不存在，无探针需要跟着改。
  - **如实记**：① 本批 ③ 的 40px/54px 是往真实站点注入 `.el-dialog` DOM 后读 `getComputedStyle` 得到的，用的是发布中的 CSS，数值可信但**不是业务对话框实盘的截图**；
    ② ②/③ 的浏览器验收（点树节点、开物料编辑对话框）**排在重打包 + zyweb 重建之后**，本轮尚未做，做完才敢称"用户看到的那一条修好了"。

- 🟡 **批次㉚ · 包版本线由 `2.14.0-alpha.1` 改为 `0.10.0` 并落死在 csproj，三个包一起出库到 `nuget/`**（2026-10-13）。
  - **口径**：用户指定「`<Version>2.14.0-alpha.1</Version>` 改成 0.10.0，`Element.X`、`Element.Markdown` 也按 0.10 一起出，`nuget/` 提交而不是忽略」。
    口头说的 `0.10` 被 NuGet 规范化成 **`0.10.0`**（nuspec 与文件名都是它），csproj 里直接写三位数以免歧义。
    **两条线分开表述**：包版本线＝`0.10.0`，对齐的 Element Plus 基线仍是 **`2.14.0`**——文档里凡出现版本号的地方都成对写清，避免被读成"降级到 EP 0.10"。
  - **改动面（先数清再动手）**：`src/Components/Element.csproj`、`src/X/Element.X.csproj`、`src/Markdown/Element.Markdown.csproj` 各 1 行；
    随包发布的 `PACKAGE_README.md` / `PACKAGE_README.X.md` / `PACKAGE_README.Markdown.md` 必须在**打包前**改（README 是包内文件）；
    外层文档 `README.md`（含 `git tag v0.10.0` 示例）、`README.en.md`、`ROADMAP.md`、`docs/element-plus-alignment.md`、`docs/getting-started.md`、
    `docs/p0-project-baseline.md`、`docs/release-checklist.md`。
    **没有任何测试断言版本号**——`grep -rn '2\.14\.0-alpha'` 排除 `_backup_before*` 后只剩本 CHANGELOG 的旧标题与 `demo/**/obj/*.json`（gitignore 掉的还原产物，下次 restore 自刷），所以不存在"改版本号把守卫改红"。
  - **出库（`nuget/`，6 个文件）**：`Element.0.10.0.nupkg` 1,743,833 B / MD5 `a3094ed81b11055601800a3a4319d5b8`（22 条）、
    `Element.X.0.10.0.nupkg` 93,531 B / `c4404b7fcc93bc22a211721cfddf8a29`（16 条）、
    `Element.Markdown.0.10.0.nupkg` 268,436 B / `cca3ff53268f866ab41e9a8572efea49`（24 条）；三个 `.snupkg` 分别 665,910 / 56,813 / 27,579 B。
    依赖边随包核对：`Element.X → Element 0.10.0`、`Element.Markdown → Element 0.10.0 + Markdig 0.30.4`（`ProjectReference` 的版本会流成包依赖，改版本号后这条边也一起从 `2.14.0-alpha.1` 变成 `0.10.0`）。
    包内三个 `PACKAGE_README*` 读回来都是 `- Version line: \`0.10.0\` (aligned with Element Plus \`2.14.0\`)`（`Temp/b31-readme-in-package.log`）。
  - **本批第一个坑：`dotnet pack` 复用"已是最新"的构建产物** ⇒ 上一轮用 `dotnet pack -p:Version=0.10` 打出的包，**nuspec 是 0.10.0、`lib/net10.0/Element.dll` 的程序集版本还是 2.14.0.0**
    （`[Reflection.AssemblyName]::GetAssemblyName` 实测）。把 `-p:Version` 去掉、改由 csproj 供版本，并**先 `dotnet build -c Release --no-incremental` 再 pack**——`--no-incremental` 直接给 `dotnet pack` 会报 `MSB1001`。
    本轮三包 dll 复验全部 **`0.10.0.0`**（`Temp/b31-assemblyversion-all.log`）。
  - **第二个坑：同一个版本号换过内容后，NuGet 全局缓存不重新解包**。消费侧 `dotnet restore` 命中的是缓存里上一轮那份
    `~/.nuget/packages/element/0.10.0/element.0.10.0.nupkg`（MD5 `c71b4bae…`，旧内容），与 `nuget/` 里的 `a3094ed8…` 不同。
    处理：删掉缓存中**自己的三个包 id** 目录（`element`/`element.x`/`element.markdown`，可由本地 feed 重新解包，不动别的包），重新 restore 后
    三个缓存 `.nupkg` 的 MD5 与 feed 逐一对齐（`Temp/b31-app-restore-3pkgs.log`）。这条已写进 `docs/getting-started.md` §1.3 与 `docs/release-checklist.md`。
  - **消费侧端到端**：`Temp/b31-app`（Blazor WASM，feed 只指 `nuget/`）三个 `PackageReference` 全 `0.10.0`，页面同挂
    `ElButton` / `ElXBubble` / `ElMarkdown`，`index.html` 引 `_content/Element[.X|.Markdown]/...` 的 css+js。
    `dotnet build` **0 警告 0 错误**；一次性探针 `Temp/b31-consumer-verify.cjs` 对跑起来的 5211 端口 **12/12 全绿、RC=0**（`Temp/b31-consumer-verify-2.log`）：
    三包各渲各的（`el-button--primary` 计算样式 `rgb(64,158,255)`、`el-x-bubble el-x-bubble--assistant` 带正文、`.markdown-body` 出 `<h1>`+2 个 `<li>`），
    六条包内静态资源全 200 且非空（13,300 / 5,553 / 2,060 / 2,198 B 等），**反向对照**同路径下一个不存在的文件返回 404（证明那六个 200 不是"什么路径都回 200"的空转）。
  - **本批自己的两条尺子错（如实记）**：① 第一版把 Markdown 字面量写在 Razor 属性里（`Text="# …&#10;&#10;- 列表项一"`），
    渲染成单个 `<h1>` 把列表吞进标题 ⇒ 首轮 **9 通过 / 2 失败**（`Temp/b31-consumer-verify.log`，红的两条是这里的 A3 与下面的 A6）。改法是换成 `@code` 里的 C# 字符串（真空行），组件与包都没动、红就没了；
    这是夹具口径没立住，**Razor 对属性里 `&#10;` 的解码细节我没有继续挖**（不在本批交付面内）。
    ② "无控制台错误"这条把**反向对照自己去 fetch 那个不存在路径**产生的 404 也算了进来（首轮另一条红）；
    改法是按 URL 过滤（只排 `__nope__`）并**如实报排除条数**（新增 A6b：`excluded=1 total=1`），不是把整条断言放宽。
  - **bUnit**：本批**没有新增用例**，基线仍 **2585**；因为三个程序集版本从 `2.14.0.0` 变 `0.10.0.0`，跑了全量：**2585 通过 / 0 失败 / 0 跳过**、RC=0（整份输出 `Temp/b31-tests-full-1.log`）。

### Fixed

- 🔴 **批次85 · 饼/环图整图一色：扇区改为逐块取调色板，图例逐块列类别，另补 `CenterText` 圆心文字**（2026-10-18，随包 0.10.8）。
  - **触发点**：宿主 `emrelecrecordplusgroup` 首页 `/`，用户原话「**男女的颜色不能区分吗，还有病案状态**」＋截图。
    改前探针 `_tools/chartcolor_before_1.log`（`_tools/chartcolor_before.png`）量到性别饼图三块
    `fill` 全是 `#5470c6`（`女:1 / 女:3 / 男:3`），病案状态环图两块也全 `#5470c6`（`未打包入库:9 / 正常:7`）。
  - **真身不是宿主配色参数**：`Palette` 只在 `ElChartGrow.razor.cs` 的 `BuildModel` 里按**系列**分配
    （`palette[idx % palette.Count]`，首页每张图只有一个系列 ⇒ 永远拿到第 0 色），
    引擎侧 `ComputeRadial` 把 `radial.Color` 原样写进每块扇区、`RenderRadial` 四处 `fill` 全用 `s.Color`。
    图例同病：`ComputeLegend` 只列 `m.Series` 的 `(Title, Color)`，饼图开图例只会多出一条「性别」，色块与类别名照样对不上。
  - **改法**：① `ElChartSeriesModel` 加 `ColorExplicit`（`BuildModel` 按 `!string.IsNullOrEmpty(s.Color)` 置位），
    扇区色 = 显式给过就单色、否则 `PaletteColor(m, i)` 逐块取（新增私有助手，调色板空时退 `#409eff`，不画隐形扇区）；
    `Slices` 元组尾追加 `string Color`，`RenderRadial` 四处 `fill`/`stroke` 与 tooltip 命中 `ElChartHit.Color` 全部改用它。
    ② `LegendSliceSeries(m)` 只在**整图仅一个饼/环系列**时启用逐块图例（`LegendItems` 列类别名＋该块色），
    混合图（饼＋柱）仍按系列列，不吃掉柱子的条目；`ComputeLayout` 的图例版面预留行数同步按块数算，
    否则四块环图只留一行高、图例挤出画布。③ 新增 `ElChartGrow.CenterText` → `ElChartModel.CenterText` →
    `ElChartLayout.CenterText`，`RenderRadial` 在扇区之上按几何圆心画 `<text class="el-chart-grow__center">`。
  - **为什么把圆心文字收进库**：宿主原来用绝对定位的 HTML 叠层冒充（`.home-chart-center`）。改前探针就量到它偏了
    —— 环心 x=1381.5、叠层中心 1408.8（差 27.3px，因为 svg 只有 250px 而容器 304.5px，叠层按容器居中）；
    开图例后圆心还会再左移 60px，叠层更追不上。SVG 内画 ⇒ 跟着 `slice.Cx/Cy` 走，宿主那条 CSS 一并删掉。
  - **证据**：`test/Element.ComponentTests/ElChartGrowTests.cs` 新增 4 只 ——
    `PieChart_SlicesTakePaletteColors`（四块四色、与 `Palette.Take(4)` 逐位等、`fill` 全出现在 SVG）、
    `PieChart_ExplicitSeriesColor_KeepsSingleColorSlices`（显式 `#123456` 不被调色板覆盖，正向对照）、
    `Legend_PieListsSliceCategoriesWithSliceColors`（图例 4 条 = `Q1..Q4` 且色块逐条等于扇区色）、
    `DonutChart_CenterText_DrawsAtSliceCenter`（圆心文字坐标 = `Fmt(slice.Cx)`/`Fmt(slice.Cy+4)`）。
    既有的 `Legend_ItemsRenderedForEachSeries`（两系列 → 两条）继续绿，是「非径向图例没被动过」的对照。
    单文件 49/49 绿（`_tools/b86_chart_test_1.log`）。
  - **发布与复验**：宿主升 `Element-Plus 0.10.8`。改后探针 `_tools/chartcolor_after_3.log`：
    性别饼图 `女 #5470c6 / 男 #91cc75`、图例两条「女/男」；病案状态环图 `未打包入库 #5470c6 / 正常 #91cc75`、
    图例两条，`一览` 落在 `x=65,y=64` = 圆心（改前叠层偏 27.3px）。截图 `_tools/chartcolor_after.png`。
  - **打包踩到的既有口径，再记一次**（批次83 第 39 行写过「cache≠feed」）：第一次 `dotnet pack` 出的 0.10.8
    里 `Element.dll` 仍是 17:10 的旧 Release 产物（`--no-incremental` 之前 Release 侧没重编），
    浏览器第一轮复验因此照旧全蓝（`_tools/chartcolor_stalepkg_1.log` 留作反例）。
    修法是 `build -c Release --no-incremental` → 重 `pack` → **删全局缓存 `element-plus/0.10.8/`** → 重 build 宿主，
    证人：宿主 `bin/Debug/net10.0/Element.dll` 2,999,296 → 3,000,832 B、`_framework` 哈希
    `Element.vflsjznck1.wasm` → `Element.yqlt324407.wasm`。

- 🔴 **批次84 · `ElUpload.OnInitialized` 的「Upload action is required.」把宿主整页打死：校验搬到真正要发请求的 `UploadFilesAsync`**（2026-10-18，随包 0.10.7）。
  - **触发点**：宿主 `emrelecrecordplusgroup` 的 `/baseinfo/employeeinfo` 点「修改」进编辑页，控制台
    `crit: WebAssemblyRenderer[100] Unhandled exception rendering component: Upload action is required.
    Element.ElementException: Upload action is required. at Element.ElUpload.OnInitialized()` —— 整页崩，不是上传失败。
  - **为什么是位置错不是值错**：`Url` 允许在 `OnChange` 里现问后端拿带签名的上传地址再 `SubmitAsync()`
    （宿主 `EmployeeInfoEdit.razor` 的 `OnImageChange` 就是这个写法：先赋 `Url` 再提交）。这类用法在
    `OnInitialized` 那一刻本来就没有 `Url`，在这里抛 ⇒ 组件根本没机会活到提交。抛在初始化里还额外吃掉一种
    合法调用：`SubmitAsync()` 在没有待传文件时不该要求地址。
  - **改法**：`OnInitialized` 只留一行说明、不再校验；`UploadFilesAsync` 先取
    `pending = files.Where(UnStart).ToList()`，`pending.Count > 0 && string.IsNullOrWhiteSpace(Url) && HttpRequest == null`
    才抛 —— 报错时机与「真的要发请求」对齐，文案不变（`Upload action is required.`），`HttpRequest` 委托档依旧豁免。
  - **证据**：`test/Element.ComponentTests/ElUploadTests.cs` 新增 `MissingUrlDoesNotThrowAtInitializationButThrowsOnSubmit`
    —— 正向对照（无 `Url` 渲染后 `.el-upload` 存在）＋ 翻面断言（`PasteUploadFiles` 后 `SubmitAsync()` 必须抛 `ElementException`）。
    牙口验过：把守卫搬回 `OnInitialized` 后该测试**红**，改回本修法后**绿**
    （`_tmp_b84_teeth.log` / `_tmp_b84_upload_test_restored.log`，构建与测试日志 `_tmp_b84_upload_build.log` / `_tmp_b84_upload_test.log`）。
  - **发布与复验**：`dotnet pack src/Components/Element.csproj -c Release -o nuget` → `nuget/Element-Plus.0.10.7.nupkg`
    （三处 csproj 版本 0.10.6→0.10.7），宿主引用 0.10.7 后构建 0 错；浏览器实测进「其他信息」页签
    `uploadCount: 1`、`Runtime.exceptionThrown` **0**（宿主日志 `emrelecrecordplusgroup/_tools/upload_edit_after_2.log`）。

- 🔴 **批次51 · 字段组件在 standalone `ElFormItem` 里仍然崩：`FormItem.Form` 的 24 处裸解引用补空守卫**（2026-10-17，随包 0.10.4）。
  - **触发点**：批次㊿ 修完 `ElFormItem` 自身，宿主 `emrelecrecordplusgroup` 的对话框面板（锁屏 / 改密 / 验证码）里
    `ElFormItem` 不再炸，但**它包的字段一渲染就 HTTP 500**。探针 `/probe/f/*`（一个 standalone
    `ElFormItem` 里放一个字段）实测：包 0.10.3 时 `input` / `number` / `switch` / `date` / `select` 五条 **500**，
    只有 `checkbox` **200**（`_probe_formitem/probe_result_0103.log`）。
  - **缺口是真身**：字段组件普遍从级联的 `FormItem` 再往下一跳取宿主表单 —— `FormItem.Form.Values.Any()`、
    `FormItem.Form.StatusIcon`。㊿ 之后 standalone `ElFormItem` 的 `Form` 合法地为 null，于是 `FormItem != null`
    守卫成立、`FormItem.Form` 却炸。全仓扫出 **23 个文件 / 24 处**（清单 `emrelecrecordplusgroup/_guard_formitem_diff.log`，
    94 行 diff）。`ElCheckbox.razor.cs:106` 本来就不碰 `Form`，这就是它在探针里独活 200 的原因 —— 对照组不用另建。
  - **改法**：`FormItem.Form.X` → `FormItem.Form?.X ?? 库默认值`（布尔 `?? false`、集合判空用
    `?.Values.Any() ?? false`）。**5 处 `FormItem.Form.RegisterInput(...)`（`ElInput:876`、`ElInputNumber:240`、
    `ElMention:163`、`ElSlider:235`、`ElSwitch:224`）已经在 `if (FormItem?.Form != null)` 里，故意不动** ——
    它们是「有宿主表单才注册」的正确语义，加 `?.` 反而会把注册路径变成静默半执行。
    唯一一处嵌在布尔表达式里的站点（`ElInput.razor.cs:994` `HasValidationStatusIcon`）加显式括号：
    裸替换会写出 `… && FormItem.Form?.StatusIcon ?? false && …`，`??` 与 `&&` 的优先级把整条判据改了味。
  - **验证（新增 11 例，两文件）**：`ElFieldInStandaloneFormItemTests`（7 例：`ElInput` / `ElInputNumber` /
    `ElSwitch` / `ElDatePicker` / `ElSelect` 各自 standalone 渲染 + 二次 `cut.Render()` 走 diff 帧、
    `ElCheckbox` 对照、`ElInput` 在 `ElForm` 内对照）+ `ElTablePropertylessColumnTests`（4 例，见下条）。
    单独跑 **失败 0 / 通过 11**（`_lib_test_0104_new2.log`）。首轮 6 红是**夹具口径**：给 `EventCallback<T>` 参数传
    `Action<T>` 委托，bUnit 在 `ComponentProperties.SetProperties` 就抛 `InvalidCastException`；改法是闭泛型
    `typeof(ElInput<string>)` 且不传 `ValueChanged`。
  - **变异自证（有牙，但不全覆盖）**：把 `ElInput` 与 `ElSelect` 的守卫反回裸解引用重跑 → **失败 2 / 通过 9**
    （`_lib_test_0104_mutation.log`，两条红都是 `NullReferenceException`）。**如实记录：`ElInputNumber` 的变异没有翻红**
    ——它的这条路径只被服务端探针（`/probe/f/number` 500→200）覆盖，bUnit 那例走不到；不把它算作单测的功劳。
    字节级还原（`cp` 备份 + `md5sum` 对 `_probe_formitem/lib_backup_0104_good/md5_good.txt` 一致）后复跑 11 绿。
    全量 **2703 通过 / 0 失败 / 0 跳过**、RC=0（`_lib_test_full_0104.log`）。
  - **验证（A/B 同前提，同一探针应用换包）**：0.10.3 → 0.10.4，`/probe/f/input` 500→**200 (6743B)**、
    `/probe/f/number` 500→**200 (7392B)**、`/probe/f/switch` 500→**200 (6903B)**、`/probe/f/date` 500→**200 (7269B)**、
    `/probe/f/select` 500→**200 (7222B)**；对照组 `/probe/withform` **6694B**、`/probe/noforum` **6532B**、
    `/probe/f/checkbox` **6918B** 三档字节**一字节没变**，说明在架 `ElForm` 路径与既有 standalone 排版没被动。
    三份日志齐：`probe_result_0103.log` / `probe_result_0104.log` / `probe_result_0104b.log`。
  - **顺带结案 1（`ElCard` 有没有 `ChildContent`）**：有 —— `ElCard.Features.cs:11`
    把 `ChildContent` 声明成 `Body` 的别名（`get => Body; set => Body = value;`），所以
    `<ElCard>正文</ElCard>` 与 `<ElCard HeaderText="标题">正文</ElCard>` 都编译且渲染；探针 `/probe/card/bare`
    **200 (6572B)**，两个哨兵文案都在 HTML 里。此前「ElCard 没有 ChildContent、必须用 `<Body>`」的说法作废。
  - **顺带结案 2（`ElTable` 无 `Property` 的列运行期抛：不是库缺陷，不改库）**：
    `ElTable.AddColumnInternal`（`ElTable.razor.cs:163-189`）对 `Property == null` 且不是
    `ElTableTemplateColumn` / `Type="index"` / `Type="selection"` 的列**按设计抛**
    `Element.ElementException: 列 操作 没有设置 Property 属性`（探针服务端日志 `probe_server_0104b.log:11` 原样抓到）。
    编译器看不出、只在渲染时炸，所以把契约写成 4 例测试钉住：无 `Property` 的散装列抛（断言异常类型与中文文案）、
    同名内容改挂 `ElTableTemplateColumn` 后模板照常渲染 2 行 2 格、有 `Property` 的列不抛、`Type="index"` 不抛。
    A/B 探针两档：`/probe/t/bad` 两版包都 **500**、`/probe/t/good` 都 **200 (6473B)** —— 差别只在标签名，
    证明宿主侧改名（`audit_propertyless_columns.py --fix`，全仓现存 0 处裸列 / 54 处安全）就是正解。
  - **顺带记录（宿主侧，非库缺陷）**：`ElTable.razor.cs:472` 有 `[Inject] private HttpClient httpClient`，
    DI 没注册 `HttpClient` 的程序集只要出现一张表就 500；探针因此补了
    `builder.Services.AddScoped(sp => new HttpClient …)`，第一次 A/B 两档同为 500 就是这个原因（已把那份无效日志留作
    `probe_result_0103_staleattempt.log` 并改名保存，不是守卫失效）。

- 🔴 **批次㊿ · `ElFormItem` 脱离 `ElForm` 就崩：级联 `Form` 的裸解引用站点全部补空守卫**（2026-10-17）。
  - **触发点**：宿主 `emrelecrecordplusgroup`（Radzen→Element 迁移）把锁屏/改密/验证码这些对话框面板写成
    「`EditForm` + `ElFormItem`」，没有 `<ElForm>` 祖先。构建 0 错，一打开就 HTTP 500。
  - **缺口是真身**：`ElFormItemObject.cs:124` 的 `[CascadingParameter] public ElForm Form` 是可选级联，
    脱离 ElForm 时为 null；而 `ElFormItem.razor.cs:38` 的 `if (!Form.Values.Any())` 和 `ElFormItem.razor`
    渲染体 11 处（`Form.HideRequiredAsterisk` / `RequireAsteriskPosition` / `EffectiveSize` / `LabelPosition` /
    `ResolveLabelWidth` / `Inline` / `LabelSuffix` / `ResolveInputId` / `ShowMessage` / 两处 `InlineMessage`）
    全是裸解引用，`OnParametersSet` 先炸。同库的 `ResolveRules`（`ElFormItemObject.cs:166`）与
    `ElementFieldComponentBase.SetFieldValue`（:18）早就写了 `Form == null` / `FormItem == null` 早退 ——
    说明「没有宿主表单」本就是设计内场景，这 13 处是漏改，不是刻意的使用约束。
  - **改法**：`OnParametersSet` 顶部加 `if (Form == null) return;`；`ValidateCore` 两处
    `Form.OnValidate.HasDelegate` → `Form?.OnValidate.HasDelegate == true`；渲染体改 `Form?.` 并配与库默认值
    一致的兜底（`ShowMessage ?? true`，其余布尔 `?? false`；`ResolveLabelWidth`/`ResolveInputId` 返回 null 时
    走 standalone 的 group 分支，不出 `for=`）。standalone 的 `ElFormItem` 只承担排版（label、必填星号），
    校验仍归宿主 `EditForm` + `DataAnnotationsValidator`。
  - **验证（新增 2 例）**：`test/Element.ComponentTests/ElFormItemWithoutFormTests.cs` ——
    `StandaloneFormItemRendersWithoutFormAncestor`（脱离 ElForm 渲染 + 连打两次 `cut.Render()` 走 diff 帧，
    断言 `is-required`、label 文案、label 不带空 `for`）、`FormItemInsideFormStillRenders`（对照：在 `ElForm` 里照常）。
    变异自证：把 `OnParametersSet` 的 `if (Form == null) return;` 反成 `if (Form != null && false)` 重跑，
    结果 **1 失败 / 1 通过**（失败那条正是 `System.NullReferenceException`），说明用例真有牙；
    字节级还原（`cp` 备份 + `md5sum` 一致）后复跑 2 绿。全量 **2692 通过 / 0 失败 / 0 跳过**、RC=0。
  - **验证（A/B 同前提对照，探针是独立 net10 Blazor Server 应用 + `AddElementServices()`，静态预渲染两个页面）**：
    改前包 0.10.2 —— 对照页 `/probe/withform` 200（HTML 内 3 个 `el-form-item`），被测页 `/probe/noforum`
    **500**，服务端日志栈顶 `System.NullReferenceException at Element.ElFormItem\`1.OnParametersSet()`；
    改后包 0.10.3 —— `/probe/noforum` **200** 且同样是 3 个 `el-form-item`、日志 0 条 NRE，
    `/probe/withform` 返回 6694 字节与改前一致，说明 `ElForm` 内的老路径没被动。
  - **顺带记录（还原陷阱，非代码缺陷）**：nuget.org 上存在同名无关包 `Element` 2.14.1。请求 `[0.10.3,)` 时，
    若 `nuget.org` 在源列表里排在本地 feed 之前，restore 会抓那个假包（`project.assets.json` 记 `Element/2.14.1`，
    其依赖表里连 `Microsoft.AspNetCore.Components` 都没有）。本地 feed 已经打出 0.10.3，把 `element-local`
    排到第一位后解析回 `Element/0.10.3`。

- 🟠 **批次㊵ · `ElTable` 的 `@key` 不再拿列标题当身份：同名列不再让整张表在第二次渲染时崩掉**（2026-10-15）。
  - **触发点**：宿主 `zyweb` 验收「物料条码」页签的按钮底色/图标改动时，一点这个页签就弹「发生了未经处理的错误」黄条。
    控制台抓到真身：`InvalidOperationException: More than one sibling of element 'col' has the same key value, '颜色'`，
    栈顶是 `RenderTreeDiffBuilder.ThrowExceptionForDuplicateKey`。该表的 `u_barcode` 与 `u_color` 两列标题都写「颜色」（既有笔误）。
  - **缺口是真身**：`ElTable.razor` 三处按显示文本记账 —— 两处 `<col>`（表头表、表体表）、单行表头的 `<th>`、
    表体的 `<td>` 全是 `@key="header.Text"`。标题是给人看的文本、不是身份：改一个字、两列重名都会改变或撞掉键。
    分组表头那两条分支本来就用 `@key="c{i}"`，同一个组件里两套口径并存，这次统一到序号那套
    （`col{i}` / `th{i}` / `td{j}`；`<col name="el-table_1_column_@i">` 的 `name` 一直就是按序号编码的）。
  - **为什么以前没暴露**：Blazor 的重复键检查只发生在**有旧帧可比的更新 diff** 里，首帧不查 —— 所以 demo 与多数页面
    「打开就在这一页」看不出问题，翻页/刷新数据/切页签回来才炸。这条同时决定了测试写法：**只 `Render()` 一次是假绿**
    （本批第一版用例就是这样全绿，加上一次 `cut.Render()` 立刻 2 红）。
  - **验证（新增 4 例）**：`test/Element.ComponentTests/ElTableDuplicateColumnTitleTests.cs` ——
    两例「首帧 + 重渲染不抛」（两列同名、三列同名），一例表头/表体内容各归各列（防换键后串位），
    一例不同标题的对照（证明不是把键判据放宽到永不冲突）。先红：改前 **2 红 / 2 绿**，红在两条重渲染用例、
    消息与浏览器里那条一字不差；改后 **4/4 绿**，`--filter ~ElTable` **136/136**（`Temp/b16-tests-restored-eltable.log`），
    全量 **2636 通过 / 0 失败 / 0 跳过**（`Temp/b16-tests-full.log`）。
  - **三处键各自翻面验牙**：逐处退回 `header.Text`（其余保持修好）—— 退回两处 `<col>` ⇒ 2 红，消息
    `More than one sibling of element 'col'`；退回单行表头 `<th>` ⇒ 2 红，`element 'th'`；退回两处 `<td>` ⇒ 2 红，
    但消息是 `ArgumentException: Attempting to return wrong pooled instance. Get/Return calls must form a stack.`
    （同行重复 td 键先把 diff 引擎的键栈踩坏，没走到判重那步 —— 根因同一个、消息不同，日志
    `Temp/b16-mut-{col,th,td}.log`）。还原一律 `cp Temp/b16-mut/ElTable.razor.good` + `md5sum -c`（OK），**未用 `git checkout --`**。
  - **文档**：`docs/table-column-key-uniqueness.md`（新增，含「首帧为什么不炸」「三处变异各自报什么」两张表）。
  - **出库**：`dotnet build src/Components/Element.csproj -c Release --no-incremental` → `dotnet pack -c Release -o nuget`
    （`nuget/Element.0.10.0.nupkg` 1755242 字节 / 15:54）→ 删 `~/.nuget/packages/element/0.10.0` 让宿主从本地源重装
    （缓存目录 15:57 重建）→ zyweb 重建 **0 错误**，`bin/Debug/net10.0/Element.dll` 时间戳 15:54（自证吃的是新包、不是旧缓存）
    → dev server 重启（先 `netstat` 指认 5171 的监听 PID，再用 `Win32_Process.CommandLine` 证明它是自己的 `dotnet run`
    才停，不动用户浏览器）+ `Network.setCacheDisabled` 刷新。
    消费侧浏览器复验由我自己驱动，跑三轮：带临时探针页的逐项复量 **32/32**（`zyweb/_tmp_probe/h80v.json`）；
    重名列压力档 —— 在「物料条码 ⇄ 基本信息」之间来回 3 轮（更新 diff 才是判重的现场，单点一次不算压到）后
    `#blazor-error-ui` 仍 `display:none`、真实异常 0、表头 th 数 = 表头列槽数 **9/9**（`_tmp_probe/h84.json`）；
    删掉探针页再重建后的交付状态 **15/15**（`_tmp_probe/hship.json`，含「`/tmp/probe80` 不再渲染表格」与同样 3 轮切换）。
    顺带量清一条别被误判成串位的口径：表头 `colgroup` 末尾固定多一根 `<col name="gutter" width="0">`（`ElTable.razor:104`），
    表体那根没有 ⇒ 表头 8 槽 / 表体 7 槽是设计如此，不是键改动造成的错位。
  - **仍在本批之外（只报告）**：库兜住的是「同名就崩」，不是「同名是对的」。`zyweb` 三张表仍是重复标题：
    物料条码表「颜色」×2（`u_barcode`/`u_color`）、款号编辑 BOM 主料表与辅料表各一处「成分」×2
    （两列绑的都是 `mix_component`）。第二列该显示什么属业务文案，等宿主定。

- 🟠 **批次㉜ · 本地分页的分页器不再依赖宿主传 `Total`：新增 `EffectiveTotal` 回落当前行数**（2026-10-13）。
  - **触发点**：宿主 `zyweb` 迁移 `Pages/Common/Select_Comm.razor` 等一批 `RadzenDataGrid`（数据整批在手上、
    `LoadData` 只做本地翻页）时改写成 `ElTable EnablePagination="true" PageSize="20"`，读屏发现**分页器整条不出现**。
    源码核出的因果链：表体切片按 `rows.Count` 走（`ElTable.razor.cs` 的 `PageRows`），分页器却只读宿主 `Total`
    （`ElTable.razor` 分页块 `Math.Ceiling((float)Total / PageSize) > 1 || !NoPaginationOnSinglePage`），
    而 `Total` 默认 0 且**全库没有任何地方按数据源回填**（`grep -n 'Total' ElTable.razor.cs` 只剩声明处与 `ColumnsTotalWidth`）
    ⇒ `Total=0` 时条件恒假、`NoPaginationOnSinglePage` 又默认 `true`，12 行 / PageSize=3 的表格**只渲染头 3 行且无法翻页**。
    消费侧的 `TableDemo.razor:195` 恰好一路显式传 `Total`，所以库里从没暴露过。
  - **修法（最小面）**：`ElTable.razor.cs` 新增 `protected int EffectiveTotal` —— 宿主给了 `Total` 就用它（远程分页语义原样），
    否则取**已筛选排序后**的 `rows.Count`，树形数据取顶层节点数（与 `PageRows` 同一基数，否则页码与能翻到的页不一致）；
    `ElTable.razor` 分页块两处 `Total` → `EffectiveTotal`。**没有**翻 `HasSelectionColumn`/`NoPaginationOnSinglePage` 的默认值，
    也没动 `PageRows` 的切片逻辑 —— 只补读数口径，宿主已显式传 `Total` 的字节行为不变。
  - **文档**：`docs/table-column-width-and-paging.md` 新增 §4b「分页器总数回落本地行数」，
    写清回落基数跟着筛后行数走、以及 `NoPaginationOnSinglePage=true` 时不足一页整条隐藏是 EP `hide-on-single-page` 口径不是坏了。
  - **验证（新增 5 例，基线 2594 → 2599）**：`test/Element.ComponentTests/ElTablePaginationTotalTests.cs`
    覆盖未传 Total 出 4 页、翻页事件发到宿主后回写 `CurrentPage` 表体换行（`@bind-CurrentPage` 的等价动作）、
    显式 `Total` 优先于行数、不足一页整条隐藏（同前提的正向对照是第一条）、树形按顶层节点数。
    修复后全量 **2599 通过 / 0 失败 / 0 跳过**、RC=0，整份输出 `Temp/b32-tests-full.log`。
  - **三次翻面，各落在自己的判据上**：**M1** 分页块退回 `Total` ⇒ **3 红 / 2 绿**（红的正是三条回落用例，`Temp/b32-mut-m1.log`；
    此轮同时给出一条旧行为的直接读数——页码条 `FindAll(".el-pager li")` 为空，即用户看到的"分页器消失"）；
    **M2** `EffectiveTotal` 去掉树形分支只回 `rows.Count` ⇒ **1 红 / 4 绿**（红在树形用例，`Temp/b32-mut-m2.log`，证明树形那条不是空转）；
    **M3** 删掉 `Total > 0` 优先段 ⇒ **1 红 / 4 绿**（红在宿主优先用例，`Temp/b32-mut-m3.log`，钉住"向后兼容"这句不是口头保证）。
    还原一律 `cp Temp/b32-mut/*.good` ＋ `md5sum -c`（两份均 OK），**未用 `git checkout --`**；还原后复跑 **5/5**（`Temp/b32-tests-restored.log`）。
  - **仍在本批之外**：远程取数（`OnLoadDataSource` 声明了但全库无调用）没借这次改动接线，宿主仍要自己切 `DataSource` 并传服务端 `count`。
  - **出库读数刷新**：`nuget/Element.0.10.0.nupkg` 现为 **1,746,485 字节 / MD5 `a4b890ef8138afae379d2ab6803b0821`**
    （批次㉛ 记的 `a3094ed8…` / 1,743,833 B 是那一版出件的时点值，保留不动）。消费侧按老规矩
    `rm -rf ~/.nuget/packages/element/0.10.0` → `dotnet restore`，否则装的还是旧 DLL。

- 🟠 **批次㉙ · #138 提交批次㉗/㉘ 那句 `git add` 警告查出的雷：vendor「逐字节一致」在全新克隆上不成立**（2026-10-12）。
  - **触发点**：`git add` 时报 `warning: in the working copy of '…/wwwroot/js/vendor/hls.min.js', LF will be replaced by CRLF the next time Git touches it`。
    本机口径 `core.autocrlf=true` ＋ `.gitattributes` 默认规则 `* text=auto` ⇒ **库里存的字节是对的**
    （`git show HEAD:…hls.min.js | md5sum` ＝ `69991b6ecc5d57cf73bad5b3a1296058`，与上游一致），但**下一次检出会改写**。
    量出来这条改写的代价：`hls.min.js` 实测 **LF=2 / CR=0**（541,870 字节）⇒ 检出后变 **541,872 字节**，
    批次㉗ 的判据②（长度＋MD5）会在全新克隆上**当场假红** —— 坏的是尺子，不是产品。
  - **修法**：`.gitattributes` 末尾一行 `demo/Element.Blazor.Demo/wwwroot/js/vendor/** -text`（注释写明理由）。
    生效与"没写宽"都用 git 自己的读数确认，不是只看文件文本：`git check-attr text -- …/vendor/hls.min.js` → `text: unset`，
    `git check-attr text -- src/…/ElDateTimePicker.razor.cs` → `text: auto`；且 `git add --renormalize …/vendor/` **零改动**
    ⇒ 加规则不需要重写已有 blob，库里那份字节从头到尾没动。
    **有意没扩大**：没把整站 `wwwroot` 或 `**/*.{js,map}` 标 `-text` —— 只有 vendor 带"逐字节等于上游"的契约，
    其余源码仍走仓库既有 `text=auto`（改全站检出形态＝改 git 配置口径，不在授权范围）。
  - **守卫（判据④ ＋ 前提证人，5 例）**：`VendoredBytesStayExactOnCheckout`（Theory×4）按 git 口径解析仓库根 `.gitattributes`
    （逐行、**后命中覆盖前命中**、`**` 跨目录、`*`/`?` 不跨 `/`）再读 `text` 档：两行 vendor 要落 `binary`，
    两行**反向对照**（`VideoPlayerGrowDemo.razor`、`ElDateTimePicker.razor.cs`）要仍是 `auto`；
    `GuardedVendoredFileActuallyHasLfEndings` 钉住"非空转"——被守护的 `hls.min.js` 确实有 LF 且没有 CR。
  - **四次翻面，各落在自己的判据上**：**M1** 注释掉那行规则 ⇒ **2 红 / 7 绿**（红的正是两行 vendor，`Temp/b29-mut-M1.log`；`git check-attr` 同时变回 `text: auto`）；
    **M2** 写成过宽的 `** -text` ⇒ **2 红 / 7 绿**（红的是两行**反向对照**，`Temp/b29-mut-M2.log`；`git check-attr` 读数 `src/….razor.cs: text: unset`）；
    **M3** 把解析器改成"先命中优先" ⇒ **2 红 / 7 绿**（`Temp/b29-mut-M3.log`，证明判据真按 last-match-wins 解析，不是拿字符串去撞那一行）；
    **M4** 把前提证人改指 `hls.min.js.map`（实测 **LF=0**，单行 2,383,712 字节 ⇒ 换行转换根本动不了它）⇒ **1 红 / 8 绿**（`Temp/b29-mut-M4.log`，红字点名「没有 LF…判据④ 是空转」）。
  - **本批自己的一条尺子错（如实记）**：判据④ 第一版把"有 LF"写成 Theory 每行的现场证人，于是 `.map` 被判红
    （**1 红 / 7 绿 / 总计 8**，`Temp/b29-tests-newguard.log`）——那是**尺子前提错**不是产品坏：单行文件不可能被换行转换改写。
    改法是分层——规则档由 4 行 Theory 判，"非空转"只由有 LF 的那一份判（M4 即这条新判据的翻面）。
    另一条流程错：`cp` 备份取在**修好之前的那一版**上，还原时把两处未提交的修复一起退掉了；
    信号其实早就有（`md5sum -c` 对测试文件报 `FAILED`），当时先归因成"版本不同"而没有立刻处理。重做后刷新基线备份，复跑 **9/9**（`Temp/b29-tests-restored.log`）。
  - **bUnit**：本批**新增 5 例**，基线 **2580 → 2585**。四组变异区间内**没跑全量**（批次㉘ 的教训：全量与变异重建撞窗会编出假红），
    全部 `cp` 还原 ＋ `md5sum -c Temp/b29-backup/md5.txt` 两份 OK 后才跑全量：**2585 通过 / 0 失败 / 0 跳过**、RC=0（整份输出 `Temp/b29-tests-full-clean.log`；
    `Temp/b29-tests-full.log` 是同一轮 `tail -40` 的截断件，读数一致 2585/0/0）。**未用 `git checkout --`**。
  - **文档**：`docs/vendored-library-deployment-alignment.md` 新增 §9（警告原文与 autocrlf 口径、541,870→541,872 的量、一行修法与 `check-attr`/`--renormalize` 两边读数、判据④ 的解析口径、四次翻面、两条自己的错），
    §5.1 标题的判据份数同步（4 例 → 9 例）、§8 复跑口径改成"批次㉗ 2570 / ㉘ 2580 / ㉙ 2585，各以当批日志为准"；`docs/testing-guards.md` 新增 §3.29 与 §4 第 84–86 条。
    文档改完后复跑：判据类 **9/9**（`Temp/b29-tests-docs-sync.log`）、全量 **2585/0/0**、RC=0（`Temp/b29-tests-docs-sync-full.log`）——本批各处写下的数字与这两份同源。


- 🟠 **批次㉘ · #133/#134 用户拿 `/table` ① 可编辑表格「上架时间」列问「这个是不是有属性直接点日期就可以选中，并且关闭下拉面板」**（2026-10-12）。
  - **取证分两半，结论不一样**：① EP 2.14.0 **没有**这个属性 —— 公开属性表整份看过：`Temp/b28-ep-dp-props.ts` 的 `datePickerProps`
    ＝ `...timePickerDefaultProps` ＋ 一个 `type`，`Temp/b28-ep-tp-common-props.ts`（316 行 `timePickerDefaultProps`）里连一个跟"点日期后开合"沾边的开关都没有；
    面板侧唯一的开合来源是 `handleDatePick` 传给 `onPick` 的第二个实参。（同批抓的 `Temp/b28-ep-datepicker-props.ts`、`Temp/b28-ep-tp-shared-props.ts` 落下来是 `404: Not Found` 空文件，**没当证据用**。）
    ② 但**改前的本库比 EP 更严，这一条是缺陷**：日历点一天，输入框什么都不写、面板也不关，必须再点「确定」。
    EP 的口径（`Temp/b28-ep-use-common-picker.ts:121`）是 `onPick(date, visible = false)` 里直接 `emitInput(result)`，
    而 `Temp/b28-ep-panel-date-pick.vue:361` 的 `handleDatePick` 末尾 `emit(newDate, showTime.value || keepOpen)` —— datetime 面板 `showTime` 为真
    ⇒ **点日期立刻写值、面板保持打开**继续拨时间；`change` 晚一拍，`Temp/b28-ep-picker.vue` 的 `watch(pickerVisible)` 关闭时才 `emitChange`，
    且要求 `!valueEquals(val, valueOnOpen.value)` ⇒「开一下不选就关掉」不发 `change`。
  - **第一步（#133，对齐官方）**：`OnPanelPickAsync` 单值分支（`src/Components/ElDateTimePicker.razor.cs:672-689`）改为即写值 + `RefreshPendingAsync`（只重渲染弹层不关面板）；
    新增 `valueOnOpen`（:35、打开时 :421）与关闭时的 EP 同判据补发（:468-474 `!IsRange && OnChange.HasDelegate && !Equals(Value, valueOnOpen)`）。
  - **第二步（#134，本库扩展）**：`CloseOnDatePick`（:182）＝点日期即提交并收起、`change` 恰好一次（:680-683 三个开关 `validate/notifyChange/close`），
    **只对单值档生效**、范围档忽略。时间口径两档同源：现值时分秒 → `DefaultTime` → `00:00:00`（:677 兜底链）。
    表格侧透传链 `ElTableColumn.EditorCloseOnDatePick` → `TableHeader`（`ElTable.razor.cs:164`）→ `RenderConfig.CloseOnDatePick`（:1663）→ `EditorMap.cs` → `DateTimePickerRender`；
    `/table`「上架时间」列已开（单元格场景没有比"点一下"更自然的确认动作，「确定」在那里要多点一次）。
  - **真身在接线层，不在日期组件**：`DateTimePickerRender` 原把 `ValueChanged` 接收者写成 `config.Page`（＝宿主页面），点日期一开始写值就变成**每次选值重渲染整页**；
    改成 `RenderBase.CreateTwoWayBinding` 的同一条口径 `config.Owner ?? config.Page`。途中撞出一条硬约束：接收者必须是**渲染过至少一次**的组件，
    测试里那个从未渲染的假 `ElementDialogBase` 在 `EventCallback.Factory.Create` 上抛 `The render handle is not yet assigned`（`IHandleEvent.HandleEventAsync` → `StateHasChanged`）。
  - **守卫（两层，各带翻面）**：
    ① 新常驻 bUnit `test/Element.ComponentTests/DateTimePickerCloseOnDatePickTests.cs` **10 条**，每条负向断言都配了同前提的正向对照
    （`ClosingPanelWithoutPickingFiresNoChange` 配 `ChangeOnCloseAfterDatePick`；change 计数恒 1 的写法过不了 `…FiresChangeOnlyOnce` 与累计到 2 那条）。
    单独跑 **10/10**（`Temp/b28-tests-newguards-1.log`、`-2.log`）。七次变异逐条落在自己的判据上：
    M1 点日期回到改前行为 ⇒ **7 红 / 44 绿**（`Temp/b28-mut-M1.log`）、M2 `notifyChange` 恒 true ⇒ **4 / 47**、M3 点日期一律关面板 ⇒ **7 / 44**、
    M4 去掉 `DefaultTime` 兜底 ⇒ **1 / 50**、M5 关闭时无条件补发 change（丢 `valueOnOpen` 判据）⇒ **3 / 48**、
    M6 表格不透传 ⇒ **1 / 50**、M7 单元格编辑器不下发参数 ⇒ **1 / 50**。
    ② 新常驻浏览器探针 `demo/tools/probes/verify-datetime-close-on-date-pick.cjs` **30 项**：A 组 `/datetimepicker` 两档并排（默认档写值＋面板仍开＋「确定」仍在＋Escape 关闭后收起；
    参数档收起＋change 由 0→1→仍 1（不选就关）→2（再选一天）），B 组 `/table` 分「新增行空初值起步」（写值、时间落 `00:00:00`、收起）与「数据行有原值」（写值、**只换日期不换 09:30:00**、收起、保存后该列显示所选那天）；
    **30/30** 绿、RC=0（`Temp/b28-probe-run6.log`，翻面还原后复跑 `Temp/b28-probe-restored.log` 同为 30/30）。
    探针层三次翻面：**改前档 M1** ⇒ **21/30**（9 条红全部落在"点日期写值/提交/change 次数"这几条，`Temp/b28-probe-flip-M1.log`）；
    **只撤收起**（`close: false`，值仍写）⇒ 「参数档：点日期后面板当场收起」红为 `panels=1`，其后下游点击 30s 超时（面板没关 ⇒ 尺子被产品行为挡住，正是这条判据要防的事，`Temp/b28-probe-flip-P2.log`）；
    **撤表格透传 M7** ⇒ 表格段收起判据红 `panels=1`、`/datetimepicker` 段保持绿（`Temp/b28-probe-flip-M7.log`）。
    还原全部 `cp Temp/b28-backup/*.cs` ＋ `md5sum -c Temp/b28-backup/md5.txt` 三份 OK，**未用 `git checkout --`**。
  - **bUnit**：本批**新增 10 例**，基线 **2570 → 2580**。全量 **2580 通过 / 0 失败 / 0 跳过**、RC=0（`Temp/b28-tests-clean.log`）；本批文档改完后复跑：同一支探针 **30/30**（`Temp/b28-probe-docs-sync.log`，之后再复跑一份 `Temp/b28-probe-docs-sync-2.log` 同绿）、全量 **2580/0/0**（`Temp/b28-tests-docs-sync.log` 与 `Temp/b28-tests-docs-sync-2.log` 两份同数）——上面这些数字与这几份同源。
    ⚠️ 另有一份 `Temp/b28-tests-full-2.log`（2 失败）**不作数**：那次 `dotnet test` 与变异重建撞在同一时间窗、编的是被改坏的源码，本批文档里的数字只取 clean 那份。
  - **文档**：`docs/datetime-picker-alignment.md` 新增「点日期这一步：即写值（对齐 EP）与 CloseOnDatePick（本库扩展）」（EP 三处源码口径、两步实现、接线层真身、**仍与 EP 有意不同的一处：时间滚轮仍是草稿语义**，
    EP `handleTimePick` 是 `emit(innerDate, true)` 每拨一下也 `emitInput`，本批未动），`docs/testing-guards.md` 新增 §3.28 与 §4 第 79–83 条、主目录探针份数 **253 → 254**。


- 🟠 **批次㉗ · #132 用户报障「发布的包里怎么有这个信息：Access to script at 'https://cdn.jsdelivr.net/npm/hls.js@1/dist/hls.min.js' from origin 'http://175.178.5.253:8049' has been blocked by CORS policy: The request client is not a secure context and the resource is in more-private address space `loopback`」**（2026-10-12）。
  - **真身不在组件库**：`ElVideoPlayerGrow` 零第三方播放器依赖 —— `HlsLibUrl` / `DashLibUrl` / `FlvLibUrl` 三个参数**无默认值**
    （`src/Components/ElVideoPlayerGrow.razor.cs:116/119/122`），流媒体类型与地址**都非空**才接入（`:238`、`:293-298`），
    发请求那一句在 `src/Components/wwwroot/js/elvideoplayergrow.js:197-232`（`s.src = libUrl` → append 到 head，`onerror` 才回 `MSE 库加载失败`）。
    组件目录里 `grep -rn "cdn\|jsdelivr\|unpkg" src/Components/ElVideoPlayerGrow*`＝**0 命中** ⇒ 那条 CDN 地址是**演示页第 7 节的示例参数**
    （`demo/Element.Blazor.Demo/Pages/VideoPlayerGrowDemo.razor:114` 改前写作 `HlsLibUrl="https://cdn.jsdelivr.net/npm/hls.js@1/dist/hls.min.js"`）。
  - **为什么只在内网炸**：Chromium 的 Private Network Access —— 页面是 `http://`（非安全上下文）而目标解析到 `127.0.0.1`（更私有的地址空间）时**整个脚本请求被拒**，
    报的就是用户贴的那句 `more-private address space \`loopback\``。本机取证 `Temp/b27-cdn-dns.log`：`nslookup cdn.jsdelivr.net` 给 `151.101.x.x` 假 IP 池，
    而 `curl` 的 **`remote_ip=127.0.0.1` `remote_port=443`**、返回 200 后 20s 超时截在 327680 字节。
    **如实记下没能复现的那一半**：拿**改前的那份发布包**在本机跑浏览器探针，CDN 脚本是**加载成功**的（`window.Hls` 可用、`new Hls()`/`loadSource`/`attachMedia` 全绿、
    C1「无 CORS 红字」也绿）⇒ 拦截这一步在本机没触发（本机走代理、用户环境走内网 DNS），所以本批的验收**不押在"报错消失"上**，
    押在两版包当场分得出方向的三条：加载期间 **0 个 CDN 请求** ＋ 站内脚本 **200 / text-javascript / 541870 字节** ＋ `window.Hls.version` **钉死 1.6.15**。
    顺带量到这条外链的第二宗罪：`hls.js@1` 随 npm `latest` 漂 —— 改前的包本机实际读到 **`1.7.3`**。
  - **修法**：库进演示站自己的静态目录 `demo/Element.Blazor.Demo/wwwroot/js/vendor/hls.min.js`（**hls.js 1.6.15**，541,870 字节，
    MD5 `69991b6ecc5d57cf73bad5b3a1296058`，与 npm 官方 tarball `hls.js-1.6.15.tgz` 里的 `package/dist/hls.min.js` **逐字节一致、未改动**；
    同版本 `.map` ＋ 上游 Apache-2.0 `LICENSE-hls.js.txt` ＋ 记档取处/复核命令的 `README.md` 一并落该目录），
    示例改 `HlsLibUrl="js/vendor/hls.min.js"`（相对 `<base href="/" />` 解析 ⇒ 子路径部署同样可用），正文同时写明病因与「内网请把 `Src` 换成你们自己的 `.m3u8`」。
  - **守卫（两层，各带翻面）**：
    ① 新常驻 bUnit 源码守卫 `test/Element.ComponentTests/VendoredLibraryGuardTests.cs` **4 条**：判据① `src/`+`demo/` 的 `*.razor` 里不许出现
    `="http(s)://<公共 CDN>/…"`（只匹配引号里的地址 ⇒ 演示页正文用 `<code>cdn.jsdelivr.net</code>` 解释病因不误伤；`SkippedDirectories` 含 `vendor` ⇒ 不审自己引进来的文件）、
    判据② 站内文件 MD5＋长度指纹 Theory×2、判据③ **正向接线证人**（没有它判据① 会因"示例整段被删"而恒绿空转）。单独跑 **4/4**（`Temp/b27-vendored-guard.log`）。
    翻面三次：**A** 示例地址换回 CDN ⇒ **2 红 / 2 绿**（红字点名 `VideoPlayerGrowDemo.razor:114 ="https://cdn.jsdelivr.net`，`Temp/b27-flipA-cdnurl.log`）、
    **B** 往 `hls.min.js` 尾部追加一个空格 ⇒ **1 红 / 3 绿**（先被长度判据抓住，`Temp/b27-flipB-bytemod.log`）、
    **C** 移走 `hls.min.js.map` ⇒ **1 红 / 3 绿**（红字「站内第三方库文件不见了」，`Temp/b27-flipC-filemissing.log`）；
    全部 `cp Temp/b27-razor.bak` / `cp Temp/b27-hls.min.js.bak` / `mv` 还原 ＋ `md5sum -c Temp/b27-flip-backups.md5` 四条 OK，**未用 `git checkout --`**。
    ② 新常驻浏览器探针 `demo/tools/probes/verify-vendored-hls-lib.cjs` **22 项 5 组**（A 前提证人 4 ＋ B 修复生效 10 ＋ C 用户那条报错 3 ＋ D 牙 3 ＋ E 范围外 2）。
    **22/22**、RC=0 两份：开发站 5199（`Temp/b27-hls-probe-dev-final.log`）与干净发布包 5200（`Temp/b27-hls-probe-CLEANRELEASE.log`）；
    翻面**直接拿用户部署的那份旧包**跑同一份探针 ⇒ **13/22**（`Temp/b27-hls-probe-FLIP-oldrelease.log`，9 条红全部落在「外链地址」与「站内文件」两轴，
    而 A1/A2 前提证人与 B4/B6–B9「库真被 new 出来并接入该节 `<video>`」两版都绿 ⇒ 归因在地址来源，不在行为链）。
    首跑 19/21 的两条红是**尺子自己坏**（探针自身 `fetch` 污染了请求计数、页内包 `window.Hls` 时没搬静态成员），加 D1 的缓存击穿参数又消掉一条假红，见 `docs/testing-guards.md` §4 第 75–77 条。
  - **bUnit**：本批**新增 4 例**，基线 **2566 → 2570**。全量 **2570 通过 / 0 失败 / 0 跳过**、RC=0（`Temp/b27-full-suite.log`）；
    本批文档改完后复跑：同一支探针 **22/22**（`Temp/b27-hls-probe-docs-sync.log`）、全量 **2570/0/0**（`Temp/b27-full-suite-docs-sync.log`）——上面这些数字与这两份同源。
  - **发布包重打**（顺带记一条会骗人的坑）：`dotnet publish -o artifacts/demo-release` **不清空输出目录**，只在旧目录上覆盖时 `_framework/` 里会同时留着改前的
    `Element.Blazor.Demo.su0uqc08hw.wasm`（站内路径 0 命中）与改后的 `16dwaxmlky.wasm`（2 命中），那份**过期程序集**跟着进 zip；
    删目录重发时若后台静态服务器还占着该目录，`rm -rf` 报 `Device or resource busy` 并**只删掉一半**（`_framework` 被掏空、`wwwroot` 还在）⇒ **先停服务再删**。
    干净重发核验（`Temp/b27-zip-verify.log`）：目录 `E:\电子病历\Element-Blazor-master\artifacts\demo-release`（388 文件）、
    zip `E:\电子病历\Element-Blazor-master\artifacts\Element.Blazor.Demo-release.zip`（22,904,044 字节 / 392 条目 / `md5 0e344a2dc9272caacd3344a83bc0720c`）、
    包内 `hls.min.js` 指纹与源目录一致。新包回归三份：启动自检 **10/10**（`Temp/b27-release-boot-check-CLEAN.log`）、
    批次㉖ 滚轮穿透 **26/26**（`Temp/b27-wheel-guard-on-cleanrelease.log`）、日期面板 **16/16**（`Temp/b27-dp-guard-on-cleanrelease.log`）。
  - **诚实记下范围外未动的一处**：这条守卫只管「由组件加载的脚本/库地址」。演示数据里的**图片/视频**外链仍在，本批**没动**
    （`.razor` 字面量清点 `Temp/b27-external-hosts.log`：`media.w3.org` ×11、`fuss10.elemecdn.com` ×10、`test-streams.mux.dev` ×2、`cube.elemecdn.com` ×1；
    另 `element-plus.org` ×10 / `www.w3.org` ×9 是文档链接与 XML 命名空间，不发请求）。它们只影响内网能不能显示那一条示例，不会造出 CORS 拦截，
    各演示页正文已写明「实际项目请换成内网可访问的地址」，是否也一并本地化**等用户定**。
  - **文档**：新建 `docs/vendored-library-deployment-alignment.md`（归因表 / PNA 病因与本机未复现那半 / 修法与指纹 / 两层守卫与三次翻面 / 重打发布包的坑 / 剩余外部地址表），
    `docs/testing-guards.md` 新增 §3.27 与 §4 第 75–78 条、主目录探针份数 **252 → 253**（`find . -maxdepth 1 -name '*.cjs' | wc -l` 复数）。


- 🟠 **批次㉖ · #130 用户报障「/datepicker 弹出下拉面板后，滚轮移到面板外部时页面滚不动」（2026-10-12，官方对照 `element-plus.org/zh-CN/component/datetime-picker`）**。
  - **真身**：不在日期组件，在所有弹层共用的「点外部关闭」遮罩层 —— `ElPopup.razor:526` 渲染的 `.v-modal.el-select-dropdown__outside`
    是 `position:fixed` 全屏、内联 `opacity:0` 的一层。滚轮的原生滚动链从命中元素往上找**可滚祖先**，这层遮罩自己不可滚、祖先链上 0 个可滚容器，
    于是链子被改写成「遮罩 → 视口」，而演示站滚的是内层 `.demo-main`、视口本身不可滚 ⇒ 指针落在弹层外（= 整屏都被遮罩盖着）页面一动不动。
    取证是**同一坐标、同一容器，只切弹层开关**：关着 `main 0->300`，开着 `main 0->0`（`Temp/b26-dp-wheel-probe.log`）。
    EP 2.14.0 根本没有这一层（它在 document 上挂 `ClickOutside` 指令），所以官方没这症状。
  - **修法**：`src/Components/wwwroot/js/dom.js` 在 document 捕获阶段把落在这张**透明**遮罩上的滚轮「穿」到遮罩底下那一层
    （`elementsFromPoint` 取栈 → 跳过遮罩/`el-overlay` → 从命中元素逐级往上按原生语义找可滚容器 → 找不到就放行给视口），
    转发时 `preventDefault` 并把 `deltaMode` 换算成像素。六条规则写在 `docs/popup-shadow-wheel-alignment.md` §3；
    **真遮罩（dialog / drawer 的 `.v-modal.el-overlay`，`opacity:0.5`）不在名单里 —— 模态就该锁住后面**（D3/D5 实测），
    指针落在原生可滚容器内时也不抢（D1：列表照旧滚、页面不动）。遮罩本身保留（大量 bUnit 用例与探针靠点击它关弹层，删了会全局改点击语义）。
  - **守卫**：新常驻探针 `verify-popup-wheel-passthrough.cjs` **26 项**，实测 **26/26**（`Temp/b26-wheel-probe-RESTORED.log`）。
    分组：A 组 4 条前提证人（`.demo-main` 可滚/视口不可滚/遮罩是最上层/祖先无可滚）+ B1–B4 行为本体 + **B5/B6 机制证人（`prevented=true` 且 `Δmain==e.deltaY`）**
    + B3 两个额外点位（防「只对某个坐标生效」）+ B4 指针在侧栏时滚的是侧栏（滚动链逐级找容器）+ C1/D1/D1-pre/D4/E1 反向对照 +
    D2/D2-pre/D3/D5 边界对照 + **F 组 7 个弹层家族逐一复跑**（select / cascader / timepicker / treeselect / autocomplete / colorpicker / dropdown）。
    翻面（`cp Temp/b26-dom.js.bak` 换回修复前 `dom.js`，**这份文件由源站直接服务、无需重新构建**）：**13 通过 / 13 失败**（`Temp/b26-wheel-probe-FLIP2.log`），
    红的正是 6 条 B + 7 条 F，而 A / C1 / D 全组仍绿 ⇒ 只有「谁滚的」这一环被绑住。还原以 `md5sum` 校验
    （修复后 `084493229fdac7b700377afe16137f2b`、修复前 `de198096835f5f63eaf3e062506183c8`），**未用 `git checkout --`**。
    同页回归 `verify-datepicker-panel-sidebar.cjs` **16/16**（`Temp/b26-dp-sidebar-regression.log`）。
  - **bUnit**：本批**新增 0 例**——滚动链/几何类判据在无布局引擎的 bUnit 侧量不到（`docs/testing-guards.md` §4 第 11 条的老账），牙只能长在浏览器层。
    全量 **2566 通过 / 0 失败 / 0 跳过**、RC=0（`Temp/b26-full-suite.log`），基线与批次㉕ 一致未动；
    本批文档改完后复跑，探针 **26/26**、RC=0（`Temp/b26-wheel-probe-docs-sync.log`）、全量 **2566/0/0**（`Temp/b26-full-suite-docs-sync.log`），上面这些数字与这两份同源。
  - **诚实记下相邻未修的一处**：指针落在面板**内部**时滚轮仍不滚列表（time spinner 列的 `elementTimeSpinnerAttach` 语义 + popper 层），
    本批**没动**，见 `docs/popup-shadow-wheel-alignment.md` §4。
  - **同批演示站小改**（上一条提交 `8fb5216`）：`MainLayout.razor:11` 顶栏标题「组件演示中心 v2.14」改为「组件演示中心 v0.1」（用户指定；
    `MainLayout.razor.cs:64` 那条 mock 通知「检验科报告模板 v2.14 已更新」与版本号无关，未动）。浏览器实抓读数已复验。
  - **文档**：新建 `docs/popup-shadow-wheel-alignment.md`（结论 / 改前证据表 / EP 对照 / 六条转发规则 / 未修的相邻偏差 / 验证表 / 三条读数口径），
    `docs/date-picker-alignment.md` 新增「Popup 外的滚轮被透明遮罩吃掉（批次㉖ #130）」并交叉引用，
    `docs/testing-guards.md` 新增 §3.26 与 §4 第 72–74 条、探针份数 251→252。
- 🔴 **批次㉕ · #124 用户报障「/datepicker 点开后显示的有问题」：带快捷列的弹层没给侧栏让位 ⇒ 面板底部出横向滚动条、日历末列被裁**（2026-10-12）。
  - **真身**：不在面板 CSS，在 popper 宽度。库里有 `.el-date-picker__dropdown .el-picker-panel{max-width:100%}`（`src/Components/wwwroot/css/index.css:8698`）
    把面板宽度钳在弹层上，而 `DropDownWidth` 只给了日历本体的宽度，带 `Shortcuts` 时侧栏那 110px 无处安放。
    改前现场读数（`Temp/b25-dp-sidebar-probe-C-FLIP.log`）：popper `w=322`，面板 `__body cw=210` 却装着 `sw=307` 的内容 ⇒ 面板内 4 个横向溢出盒子，
    末列「六」`right=729` 越出 `panelRight=632` —— 与用户截图同形（底部一条横滚条 + 右边被切）。
  - **修法**：`ElDatePicker.cs` 的 `DropDownWidth` 带快捷列时单面板 `+116`、双面板 `+110`（`ElDateTimePicker.razor.cs:1224` 的同源属性一并改），
    对上 EP 2.14.0 的 `.el-date-picker{322px}` / `.el-date-picker.has-sidebar{438px}` / `.el-date-range-picker.has-sidebar{756px}`（`Temp/ep2140-full.css`）。
    改后 **438 / 756**、面板内 0 溢出、末列 `right=729 ≤ panelRight=748`，无快捷列的正向对照仍 **322px** 且没有侧栏（C5）。
  - **守卫**：常驻探针 `verify-datepicker-panel-sidebar.cjs` 从 10 项扩到 **16 项**（A 内嵌面板 / B 弹层几何 / C 本轮宽度组 / Z 0 JS 异常），
    实测 **16/16**（`Temp/b25-dp-sidebar-probe-final.log`）；撤掉侧栏那段宽度即翻面，**12 通过 / 4 失败**，红的正是 C1–C4。
    演示补「范围 + 快捷选项」（`DatePickerDemo.razor`）与「快捷选项 (Shortcuts)」（`DateTimePickerDemo.razor`）两段。
- 🟠 **批次㉕ · #125/#128/#129 按官方 Statistic 页建 /statistic 演示页，顺带抓到三处真对齐缺陷并修在库层**（2026-10-12）。
  用户口径是「如果没有 elstatistic 这个组件，创建一个，加到 demo 里面」；实查 `ElStatistic` / `ElCountdown` 早已存在，缺的是演示覆盖，
  而建页过程里逐项对照官方，才暴露下面三条库层缺陷。EP 侧证据：`Temp/ep2140-full.css`、`Temp/b25-ep-statistic-docs-zh.md`（官方 zh-CN 文档 404，取 en-US 那份、小节名按 en 列表对应）、
  `statistic/src/statistic.vue`、`Temp/b25-ep-countdown-utils.ts`。
  - **#129 `displayValue` 算法**：旧写法走 .NET 的 `N`/`F` 标准数值格式，与 EP 的字符串算法有两处不等价——
    ① `precision` 在 EP 是 `slice` **截断**不是四舍五入（`12345.6789` 取 2 位是 `12,345.67`，`1234.9` 取 0 位是 `1,234`）；
    ② 千分位只作用整数段、小数点只作用两段接缝，所以 `ThousandsSeparator="."` + `DecimalSeparator=","` 得 `1.234.567,89`，
    而旧的「整串 Replace」会把千分位一起换掉。移植在 `ElStatistic.razor.cs` 的 `EpDisplayValue`，NaN/Infinity 走 EP 的前置短路原样输出。
  - **#128 `ElCountdown` 的 `formatTime`**：旧写法每个 token 各取「总时长」，与 EP 的**逐级吃余数**不同（同写 `DD` 与 `HH` 时 `HH` 应是去掉整天后的小时；
    没有日级 token 时 `HH` 才是总小时，可以大于 24）；补零口径是按 token 连续长度；`[ ]` 字面量既不参与替换、最后还要剥掉方括号。
    另补 EP 有而本库缺的 `ValueStyle`（透传到 `.el-statistic__number`）。
  - **#125 CSS 变量**：EP 把 6 个 `--el-statistic-*` 变量声明在 **`.el-statistic` 根节点**、由 `__head`/`__content` 消费，
    本库原先把字号写死在子规则、根节点一个都没声明 ⇒ 官方卡片示例的 `--el-statistic-content-font-size:28px` 覆盖完全不生效；`__head` 也缺 `line-height:20px`。
  - **演示页**：`Pages/StatisticDemo.razor`（`/statistic`，侧栏「统计数值 Statistic」+ `el-icon-data-line`，按英文名升序排在 /steps 与 /switch 之间）五段：
    基本用法（含官方 basic 的 1500ms 数值过渡，本库用 50ms 定时线性插值实现 + 重放按钮）/ 数字风格（属性表逐项）/ 倒计时（三段含 `DD [天] HH:mm:ss`）/
    卡片用法（自写卡片 DOM + 28px 覆盖）/ 趋势与附加内容（本库扩展）。
  - **bUnit**：新增 12 个用例（`ElStatisticTests.cs` 16 + `ElDataDisplayTests.cs` 15 的过滤跑 **31/31**，`Temp/b25-statistic-tests.log`；
    12 个新用例逐个点名见 `Temp/b25-statistic-new-tests.log`），`ElDataDisplayTests` 那条旧期望同步改成截断口径。翻面三次：
    Flip A（countdown 不剥方括号）**1 红 / 7 绿**（`Temp/b25-flipA-countdown.log`，红的正是 `CountdownStripsBracketLiterals`）；
    Flip B（不插千分位）**6 红 / 25 绿**（`Temp/b25-flipB-statistic.log`，正向对照 `PrecisionPadsMissingDecimalsWithZero` 仍绿）；
    Flip C（不截断小数）**4 红 / 27 绿**（`Temp/b25-flipC-statistic.log`，红字全是「尾巴多出来」形状：`1,234.567` 对 `1,234.5`）。
    三次都 `cp` 备份 + `md5sum -c` 还原（`84368312439b646d7134035aa0f37e7b` / `aebb878f1a5a98ed098808fec01cf3fc`），未用 `git checkout`。
  - **浏览器守卫**：新常驻探针 `verify-statistic-demo.cjs` **30 项**（S CSS 口径 / P displayValue 读数 / M 数值过渡 / C formatTime 三条 / K 卡片 28px 与基本段 20px 正反对照 / E 扩展 / Z 0 JS 异常），
    全绿见 `Temp/b25-statistic-probe-RESTORED.log`；把 `.el-statistic__content` 的 `font-size` 改回写死 20px 即翻面，**29 绿 / 1 红**（`Temp/b25-statistic-probe-FLIP.log`），
    红的正是 K1，现场 `largeVar="28px"` 而 `largeSize="20px"`＝覆盖读不到；诚实记一句：S4/K2 在变异下仍绿（写死值恰好等于默认 20px），能分辨这项修复的只有 K1。
    CSS 还原后 `md5sum -c` 校验 `c2a54223dd1b84ef15951896efac1968` OK，再跑 30/30。
  - **全量 bUnit**：**2566 通过 / 0 失败 / 0 跳过**（`Temp/b25-full-suite-restored.log`）＝ 批次㉔ 基线 2554 + 本批 12 个新用例；本文件与三份对齐文档、`docs/testing-guards.md` 改完复跑同为 **2566/0/0**（`Temp/b25-full-suite-docs-sync.log`，文档改动只落在被 `SourceEncodingGuardTests` 扫的 `docs/` 里，编码与数都当场复核过）。
  - **文档**：`docs/date-picker-alignment.md` 新增「Popup Width With Shortcuts（批次㉕ #124）」；`docs/date-picker-panel-alignment.md`、
    `docs/datetime-picker-alignment.md` 补同一条的交叉引用与 16 项口径；`docs/statistic-alignment.md` 整篇重写（旧版 §2/§3/§4/§5 描述的是被本轮推翻的
    `ApplyDecimalSeparator` 整串替换、`N`/`F` 优先级与「演示已覆盖」的旧说法）。

- 🟠 **批次㉔ · #120 复测 #100「verify-markdown-scrollbar 批跑 18/20」：同一批 24 份串行重放＋页内证人，20/20，真身继续挂「未定位」**（2026-10-12）。
  - **做法**：`Temp/b24-batch-replay.sh` 按归档 `Temp/grow-probes-k5/_summary.log` 的同一批、同一顺序串行重放，
    只把目标探针换成证人副本 `Temp/b24-md-scroll-witness.cjs` —— **判据一字未改**（仍是「悬停 700ms 后取条带像素、`ratio>0.3` 才算显形」），
    只在同一次悬停后多开三枪，并同场报 `matches(':hover')`、命中链、条带/栏几何相对取点时刻的漂移、帧中位、长任务、`visibilityState`。
    同一份 build 的**驻留版串行单跑**也 20/20（`Temp/b24-md-scroll-serial.log`）。
  - **读数**：`Temp/b24-batch/verify-markdown-scrollbar.log` **20/20**，两条 hover 判据现场 `hover=true`、`ratio=0.8649 / 0.9811`、
    不依赖 hover 的静态变异对照同批成立（注入 vendor 规则 → `ratio=1 / minL=160`）⇒ 归档那轮的 18/20 形状**没复现**，按规矩写「未定位」，不写「偶发/环境问题」，也不结案。
  - **证人顺手抓到一件新东西**：预览栏那一臂 `rafMed=1000ms`、最长帧 1008ms、长任务 `0/0ms`、`vis=visible`、同臂四枪 ratio 一字不差
    —— 批次㉓ 那个「中位帧 1008.2ms」的降频**第一次有现场**。它只污染时长读数、不动像素/DOM 判据；
    **这条现场正出自一份 20/20 PASS 的日志 ⇒ 它与本轮那两条 hover 红没有因果关系**，不再当作 #100 的候选原因去追；
    且带全套反遮挡旗的 `b24-treeselect-latency.cjs` 与只带 `--no-sandbox` 的证人脚本都出现过同读数 ⇒ 旗子不是分水岭，**触发条件仍未定位**（`docs/testing-guards.md` 第 67 条、§3.24 第 4 行）。
  - **整批点名（不只盯自己那颗钉子）**：24 份里 **23 绿 1 红**，红的是 `verify-gap-margin-equivalence`（rc=1，唯一 `DIFF` 项 `dtg-tags`），
    与归档那轮**逐字一致**（两份日志第 100~110 行 `diff` 为空）⇒ 确定性红、非本批引入，而且此前 `docs`/`CHANGELOG` 里 `grep "dtg-tags"` **0 命中**＝从来没记过档。
    现场 `host h 24→28`、`span.el-tag x 694→696 / ml 0px→2px`；同探针用 `>*+*` 的 `dtg-tag-inner` 判 EQUAL，
    差别正是 `> *{margin:2px}` 给首个子项也补了 2px 前导、纵向 2px×2 把容器撑高，而 `gap` 两者都不产生 ⇒ 指向**替换 CSS 的选择器写法**而非库缺陷。
    **本批没动它**（改探针还是改库口径属另行决定），只点名记档，见 `docs/testing-guards.md` 第 68 条。
- 🟠 **批次㉔ · #121/#123 复测 #95 的「/treeselect 点击→面板 0.5~1s」：点开那一半在新口径下只剩百毫秒级，选择那一半抓到真身并修掉**（2026-10-12）。
  用户口径是「这 2 个再测试一下吧」，本轮所有读数都是新抓的日志，批次⑯ 那张表只作历史快照引用、不并入本轮数字。
  - **点开（click→面板）**：同一次点击挂两把尺子交替跑（每次重新 goto，各 3 遍，`Temp/b24-open-matrix2.log`）——
    页内 `MutationObserver` 打戳 /treeselect 125 / 251 / 129ms、/select 64 / 125 / 112ms；rAF 轮询尺 199 / 341 / 210 与 91 / 214 / 197ms。
    窗口置顶＋每臂 rAF 健康门（`Temp/b24-treeselect-latency3.log`）面板首现 **104 / 95 / 51ms**；
    变更批明细（`Temp/b24-treeselect-open-why.log`）冷开首个变更 80ms、弹层出现 103ms，复开 17 / 28ms。
    **批次⑯ 的 536~582ms 量级在本构建不复现**；且同一页自身的波动（64→125→112）比两页之差还大 ⇒ 「非树选择独有」这半条仍然成立。
  - **一把尺子的坑（已写进 `docs/testing-guards.md` §3.24）**：rAF 轮询命中≈该臂长任务的尾巴——六臂中位帧全部 8.3ms（无降频），
    而轮询命中与长任务合计逐条对上（/select 91↔90、214↔213、197↔196；/treeselect 199↔197、341↔340、210↔209），
    DOM 其实写在长任务中途。所以「点开慢」剩下的那一截是**一次 Blazor 渲染长任务**，与批次⑬ #83、批次㉓ #98 同族，不是事件往返也不是定时器。
  - **没做成的两件事照实记**：① 批次㉓ 收尾那轮的「中位帧 1008.2ms（rAF 掉到 1Hz）」条件本轮三次运行都没复现，**触发条件未定位**；
    ② 我做的三种「制造遮挡/后台」的操控**全都没过操控检查**，只算没做成对照的实验，既不能证明也不能否证「遮挡导致降频」，不当负向结果用：
    另开 Edge 窗口置顶（遮挡相 `focus=true`、中位帧 8.4ms）、Win32 `SW_MINIMIZE`（`iconicNow=True` 而页面仍 `vis=visible focus=true`、中位 8.3ms）、
    置顶无边框窗压住被测窗口（`Temp/b24-raf-cover-2.log`：覆盖窗自证人 `SHOWN=false ticks=0` ⇒ 覆盖窗根本没证实显示出来）。
    同轮 A/B 拿掉 `--disable-backgrounding-occluded-windows` 等三面反遮挡旗（`ignoreDefaultArgs` 传数组；整组传 `true` 会让 Edge 直接退出，见 `b24-raf-cover-1.log`）
    也没造出降频：盖住时中位帧 默认=8.3/8.3ms vs 无旗=8.3/8.3ms。
  - **选择（click→回显）的真身＝写值排在收起动画之后**：`SelectValueAsync` 原先 `await CloseDropDownAsync`（内部 `Task.Delay(10)`＋`Task.Delay(200)`，`src/Components/ElPopup.razor.cs:908-931`）
    再写 `SelectedOption/SetFieldValue/Value/Label` 和发 `OnChange` ⇒ 单选点完选项必然空等 ~210ms，**与主线程阻塞无关**（该臂长任务 0）。
    EP 2.14.0 的顺序相反（`Temp/ep2140-useSelect.ts:607-612`，按 `2.14.0` tag 取的源码）：`emit(update:modelValue)` → `emitChange` → `expanded.value = false`，即**先写值再收起**。
    改法＝把关闭整块挪到值写入与 `OnChange` 之后（写值块 `src/Components/ElSelect.razor.cs:655-673`，被挪到其后的关闭块 `:674-679`），其余一概不动；
    凡走单选 `CloseDropDownOnSelect` 的 ElSelect 家族都被这 210ms 压着，不只 ElTreeSelect。
  - **改前/改后同一把尺（`Temp/b24-pick-timeline.log` vs `-after.log`，rep0/rep1）**：回显 **276 / 256ms → 18 / 21ms**；
    面板节点从 DOM 移除 271 / 251 → 232 / 232ms（收起动画没被删，仍在末尾）；首个变更 20 / 16 → 15 / 19ms；两臂长任务恒为 0。
  - **牙口**：常驻探针 `demo/tools/probes/verify-treeselect-b19.cjs` 加段9 三条判据（顺序判据 + `removed≥150ms` 正向对照 + 回显文本），
    44→**47** 项，新字节 **47/47**（`Temp/b24-verify-treeselect-after.log`，段9 `echo=35ms removed=243ms`）；
    翻面（把顺序改回去、重建）**46/47**，唯一红的正是段9 那条顺序判据，现场读数 `echo=238ms removed=238ms`（同帧）⇒ 判据有牙（`Temp/b24-verify-treeselect-flip.log`）；
    `ElSelect`／`ElTree`／`DropDownTable` 过滤组 bUnit RC=0、`Failed: 0, Passed: 89`（`Temp/b24-select-reorder-tests.log`）。
    口径与逐条归因见 **`docs/tree-select-alignment.md` 批次㉔**、守卫记录见 **`docs/testing-guards.md` §3.24**。
- 🟡 **批次㉔ · /table 供应商列显示态改出名称（用户口径「出名称」）—— 只动演示页模板，值域、编辑器、保存、排序一概不变**（2026-10-12）。
  批次㉓ 把 `probe-table-editors` 那条红定性为「演示口径问题」（列里存的是编码 `S03`，编辑态触发器按 `EditorTextField` 显示名称，
  退出编辑态就跳回编码），本轮由用户拍板显示态也出名称。
  - **改法**：`demo/Element.Blazor.Demo/Pages/TableDemo.razor` 该列加显示模板 `<ChildContent Context="cellRow">@SupplierDisplayText(cellRow)</ChildContent>`
    ＋ 一个 code→name 的映射方法（找不到编码就退回显示原值）。**必须写成显式 `<ChildContent>`**：同列已有具名片段 `<EditorColumns>` 时
    隐式子内容实测编译报 `RZ9996`。库侧零改动；`CanEdit` 判据是 `IsEditable && Eval != null`（`ElTable.razor.cs:1952`），`Eval`/`EvalRaw` 都由 `Property` 生成，
    加模板既不影响编辑态也不影响排序/筛选（它们读的是编码域）。
  - **读数**：`probe-table-editors` 由 **18/19 → 19/19 全绿**（`Temp/b24-probe-table-editors-run1.log`），
    翻面基线就是改前那份（`Temp/recheck-b23-probe-table-editors.log:19` 同一格读数为 `S03` 而判红）。
    两份日志逐行 diff：**前 17 行逐字一致**，只有第 18/19 两行变 —— 19 行由 FAIL 转 PASS，
    18 行「⑤ 行内类别」判据不变、只是它把整行文本当读数带出来，那格里供应商从 `S03` 变成「星澜医药站」。
    显示态逐行取证 `Temp/b24-supplier-display.log`：6 行全部出名称、`supplierCellsShowingCode = 0`，同页其余列读数不变。
    本页仅 1 处 `Property="Supplier"`，且所有表格都是 `AutoGenerateColumns="false"` ⇒ 没有别处还在按编码显示供应商。
  - **顺带记一笔库侧空白（本批没动）**：`ElTableColumn.Formatter` 在 EP 2.14.0 是有效契约，本库声明了、也复制进了 `TableHeader`，
    但 `src` 内**无任何消费者**＝死参数。要不要接属库侧语义决定，不在演示口径改动里顺手扩范围。
- 🟠 **批次㉓ · #98 ④「已开页数会放大开页成本」实测不成立 —— keep-alive 保持原设计（产品代码零改动），并作废一把坏尺子、修好一条常驻探针**（2026-10-12）。
  原判据只有 `Temp/diag-lag3-1.log` 一条出过这个形状（TableV2 145→752ms、Grow 2160→3979ms），而那一轮帧节奏塌过（同轮心跳 135/帧 2），
  又属跨窗口数字，按 `docs/grow-progressive-section-build.md` 第 4 节第 1 条不作数 ⇒ 本批用**同 boot 配对阶梯**重测（口径见该文档第 8 节）。
  - **方法**：一次 boot 内把同一页放在货架第 k 位与第 14−k 位各测一次（轮 A 正序 /grow 第 13 档、轮 B 反序 /grow 第 1 档、轮 C 复跑），
    页面自身内容成本自动抵消，配对差才是档位差。三臂 39 个读数点全部判「计时可信」（中位帧一律 8.3ms），JS 异常 0；冷启 14501/13102/12299ms、整机 CPU 70.9/63.5/62.2%（`Temp/diag-shelf4-1.log`）。
  - **读数**：12 组轻页配对 Δ安静 均值 **−4.8ms**、范围 −459~+562ms、符号 6 负/5 正/1 零；Δ长任务 均值 −4.3ms。
    同轮阶梯也是平的（轻页首变三轮分别 207~463 / 227~308 / 192~351ms，货架 1→13、整页 DOM 4302→7246 都没跟着涨）；
    /grow 方向还反了（第 13 位 9146/6418ms 安静，均值 7782ms，比第 1 位 10163ms **快**）⇒ 只能归给整机负载漂移。
    **结论：把 `MaxCache` 从 30 调小买不到开页成本**，只会让还开着的页签丢状态（页签关闭已走 `KeepAliveRouteView.Evict` 真 Dispose）。
  - **反馈时刻（页内 MutationObserver 打时间戳，`Temp/diag-growfeel-1.log`）**：/grow 点击→首变 49/56/62ms、→侧栏高亮**与**本页容器挂占位 227/298/371ms（6/6 轮同一毫秒 ⇒ 两阶段挂载按设计生效，不存在「点了没反应」那一段）；
    Badge 正向对照 41/50/58ms、187/188/223ms。/grow 剩余重活全在 0~9s 分批建里（长任务 12~18 个 / 3962~5603ms / 最大单个 833~918ms，本轮 58~76% CPU，与第 3 节同窗口 408~475ms 跨窗口不可比）。
  - **常驻噪音点名（不修）**：直开 `/badge`、`/grow` 静置 30s **0 变更 0 长任务**（`Temp/diag-churn-1.log`），但按用户路径（先开 /table 再点走）静置 45s 后，
    9s 之后还在写 DOM 的全是 `attributes @ div.el-carousel__item(.is-active)` / `li.el-carousel__indicator(.is-active)`（`Temp/diag-churn2-1.log`，Badge 臂没开过 /grow 也照样有 ⇒ 属**已 `display:none` 的 /table 页**的自动播放轮播）；
    这两桶 **0 个长任务** ⇒ 不掉帧、不是「不流畅」成因，代价只是常驻 CPU/功耗。两种修法（演示外壳按激活 key 暂停 vs 库侧 `ElCarousel` 「不可见即停表」）都动既有语义/EP 之外的扩展，待用户点头。
  - **顺手把 `docs/testing-guards.md` §3.7 B 组「疑似组件缺陷」逐份重跑定性**（10-07 的表是快照，不作当前读数；日志 `Temp/recheck-b23-*.log`）：
    `probe-button-group-checks` **15/15 绿**（尺寸级联那条已不复现）；
    `verify-verifycode-demo` 从 7/8 修到 **8/8** —— 真身是**探针按 `nth(4)` 取旋钮的序号漂移**（演示区在「字号」与「输入框宽」之间插过「占位符字号」），
    `Temp/diag-evcg.cjs` 双向见证：写 nth(4) 只改到 `--evcg-placeholder-font-size:200px`、输入框仍 194px，按 `<label>` 文本定位到 nth(5) ⇒ 内联 `width:200px`、实测精确 **200**，
    **`ElVerifyCodeGrow.InputWidth` 接线无错**；探针改为按 label 文本取序号（判据数仍 8 条）。
    `probe-table-editors` 18/19：唯一红的那条不是「没更新」，编辑态触发器已变「星澜医药站」，而该列存的是**编码**（`Supplier="S01"…`），退出编辑态按原值显示 `S03` ⇒ 演示口径（要不要显示态也映射成名称属演示侧决定）。
    `probe-nav-collapse` 4/7：展开 235/239 对的是现外壳 `--demo-aside-w:256px`（探针常量还是旧外壳的 220），库侧 `.el-menu--collapse{width:64px}` 在样式表实测存在；
    折叠态菜单 48 与 64 的差额来源（侧栏滚条占位 vs 弹性拉伸）**未定位**——JS 直接切 `is-collapsed` 的假路径没走组件，不算证据。
  - **作废的量具**：`Temp/diag-growfirst.cjs` 用 Node 侧 50ms `page.evaluate` 轮询定「变更时刻」，铁证是三组读数里 `T-menu` 与 `T-page` 一模一样（1714/1440/1456ms 两臂同时相等）——
    `evaluate` 也要主线程空出来才跑，量到的是「主线程下一次可运行」。该口径整条作废，改页内观测器；已补进 `docs/testing-guards.md` §3.23（含「负载在场时跨 boot 档位对比要换同 boot 配对阶梯」「直连 URL 静置 0 变更 ≠ 真实路径无噪音」两条）。

- 🟠 **批次㉒ · #117 弹层关闭负载偶发红 —— 真身是 bUnit 2.7.2 `Nodes` 缓存投毒，判据换 markup 口径，库代码零改动**（2026-10-12）。
  十五档猎取 122 轮 / 13 次红攒的排除链，最后只剩一问：「渲染器的树已算成这层不存在，bUnit 挂在 fragment 上的那棵树为什么还带着它」（`docs/message-box-alignment.md` 第 10 条）。
  - **机制**：`Nodes => latestRenderNodes ??= Parse(Markup)`（check-then-act，无锁）对上 `UpdateMarkup` 的
    **清缓存 → 慢 `GetHtml` → 才写 markup** 序列：窗口内读 `Nodes` 的线程把**旧 markup 的解析树**写回缓存，非空永不重解析
    ⇒ `Markup`（`Volatile.Read`，永远新鲜）与 `FindAll`/`Nodes`（走缓存）永久分叉；只有**产生真实 DOM 变化的渲染**能清它。
  - **确定性放大见证**（临时件验完已删，读数 `Temp/b22-117-witness.log`，运行 `Temp/b22-117-witness-run.log` 按设计 1 红）：
    抓到取消按钮后放后台线程死循环读 `popup.Nodes`（每轮 5.8M–10.9M 次）覆盖关闭渲染的投毒窗 ⇒ **6/8 轮**
    `markup.wrapper=0 dom.wrapper=1 队列=0 RenderCount=6/11/16/21/26/36`——与历史红现场同形；2/8 轮错过窗口＝净（与无放大 ~10% 命中率同源）。
    见证判据全程用 `Markup` 字符串绕开被测缓存；泵 602 轮不恢复／强制渲染（diff 0 编辑）不恢复／再开一层反而归零，逐条对上历史红读数。
  - **产品侧无病**：`Temp/b17-threadprobe.log` 那条「Remove 事件 4/5 次落在别的池线程」是事实但非元凶（投毒与渲染线程无关；
    生产端渲染器同步上下文串行）。`ElPopup.razor.cs:418-423` 的裸 `StateHasChanged()` 不动。
  - **修法（测试侧换尺子，非放宽断言）**：三条 Prompt 关闭测试的 `WaitUntilAsync` 判据从 `FindAll(".el-message-box__wrapper").Count == 0`
    改为 markup 口径 `CountMarkupWrappers(popup) == 0`；`CloseScene` 常驻并打 `markup.wrapper`/`dom.wrapper`，分叉自己点名 bUnit 基础设施。
  - **翻面实测**：注释掉 `DialogAnimationEndAsync` 的 `Dialogs.Remove(option)` ⇒ 三条**全红**在「等待超时」（`Temp/b22-117-mut-remove.log` 3/3 红）；
    `cp` 还原 md5 逐位回基线 `e5f9ed791278dd2e43ee25ab0e53ffe3`、3/3 绿（`Temp/b22-117-after-restore.log`）。
    `ElFeedbackTests` 10/10（`Temp/b22-117-markup-caliber-check.log`）、全量 **2554 / 0 失败**（`Temp/b22-full-suite-3.log`）。
    负载轮如实记：r1–r3 各 3/3 绿、跑在 24-spinner 窗内（r3 后当场实测 26 个 python），未按轮前后自证 ⇒ 仅参考；
    r4–r6 `python_before=0`（窗口过期）＝空转轮。判据不再读那块缓存，负载命中率与结果无关；升级 bUnit 能否消掉竞态本批未验证。

- 🟠 **批次㉒ · #115 `ElViewerGrow` 加载门控负载偶发红 —— 「入口同步发一拍」契约 + 门控归属守卫 + `loadGatePrevious` 污染守卫**（2026-10-12）。
  报障：`LoadingGate_MinDuration_RaisesLoadingThenReady` 负载跑批偶发红，宿主收不到 `Status=Loading` 一拍（历史红样 `Temp/b17-hunt4.log`：3 拍 `PreviousStatus` 全 `Loading`）。
  机制三段全部环境门控探针逐行实证（`Temp/b22-115-trace-idle.log`，口径见 `docs/viewer-alignment.md` §10）：
  - **污染**：首帧参数驱动的门控窗口还在计时，`TryBeginLoadGate` 把 `loadGatePrevious` 抓成 `Loading`，终态上报全被污染（=红样形态）。
  - **双路收门**：门控单字段两路驱动，`TryBeginLoadGate` 派发的渲染让 `OnAfterRenderAsync` 也走进收门，trace 实测渲染路 `EndDrop` 在命令式半途落地（elapsed 141）；
    放门时 `current` 未赋则状态当场算成 `Empty`，命令式终态一拍被踩掉。
  - **续体丢拍**：旧实现把宿主 `Loading` 一拍排在 `Task.Yield` 后的续体里（原 ：1310），超额订阅下续体排队超窗、门已落下时 `status` 早已不是 `Loading`，整拍丢失。
  - **修法（用户拍板「入口同步发一拍」）**：`LoadAsync` 入口在首个 `await` 之前同步发 `OnStatusChanged(Loading)`，:1310 删除；
    新字段 `loadGateImperative` 让 `OnAfterRenderAsync` 不收命令式举的门；`TryBeginLoadGate` 已处 `Loading` 时不改写 `loadGatePrevious`；
    测试加强终态拍 `PreviousStatus != Loading` 断言。
  - **翻面实测**：M-a 删入口一拍 **恰 1 红**（`Temp/b22-115-mut-ma.log`）、M-c 删污染守卫 **1 红在新断言**（`Temp/b22-115-mut-mc.log`，失败行 ：2665）、
    M-b 删归属守卫 **幸存**（idle 180/180；24 spinner 真载 2 轮开局 26 进程仍绿，`Temp/b22-115-mut-mb-load1/2.log`）——
    该竞态窗口 bUnit 过滤器进不去，有界结论、机制判定靠 trace（批次㉑ M4 同款处理）。每条跑完 `cp` 还原并 `md5sum -c` 逐位校验。
    修复后 `ElViewerGrowTests` **180/180**（`Temp/b22-115-after-fix.log`）、全量 **2554 / 0 失败**（`Temp/b22-full-suite-2.log`）；
    负载复核 24 spinner 下 26 进程开局收尾 **180/180 绿**（`Temp/b22-115-load-verify1.log`；弱载/跨窗轮全绿但仅作参考，全列见对齐文档）。

- 🟠 **批次㉒ · #116 `ElAutocompleteTests.DebouncesRemoteSuggestions` 负载偶发红 —— 被取代查询不再出网（组件预检守卫）+ 键入合入同一派发回合**（2026-10-12）。
  24 spinner 超额订阅下偶发红（b17-hunt4 run5）。临时证人 `Temp116Witness.cs`（验完已删，日志 `Temp/b22-w116-witness.log`、`Temp/b22-w116-after.log`）三段实测：
  - **真身**：`Task.Delay(Debounce)` 到点与后一键 `CancelPendingQuery` 之间无墙钟保证——定时器先跳完时 Delay 已 RanToCompletion，`catch (TaskCanceledException)` 抓不到，
    旧代码照常调 `FetchSuggestionsAsync`；事后 `IsCurrentQuery` 只丢**结果**、收不回**请求**。键间真实间隙被负载拉到 > Debounce 时「a」必发一次远端，`Assert.Empty(calls)` 红。
  - **派发模型实测**：bUnit 派发回合串行（回合内 spin 3 倍防抖，续跑挤不进来）；W2 免负载复现（强制 150ms > 75ms ⇒ 必含「a」，属 EP 同款「真超窗各发一次」语义，非 bug）。
  - **修法**：组件在防抖到点后、发远端前加 `IsCurrentQuery` 预检（对齐 EP lodash debounce cancel 即不派发）；测试两次 `Input` 合入同一 `cut.InvokeAsync` 回合 + 回合内 spin 60ms（Debounce 20），断言改终态 `calls == ["ab"]`（终态蕴含中间态，不赌两回合之间的墙钟）。
  - **翻面实测**：M1 删预检 **2 红**（`DebouncesRemoteSuggestions` + 证人 W3，其余 7 绿，`Temp/b22-mut-m1.log`）；M2 整窗跳过防抖 **2 红**（同名两例，`Temp/b22-mut-m2.log`）；
    M3 删查询起点 `CancelPendingQuery()` **9/9 绿（幸存）**——版本计数独立判旧，两防线互为双保险，记为有界结论（`Temp/b22-mut-m3.log`）。每条跑完 `cp` 还原并 `md5sum -c` 逐位校验。
    全量 **2554 / 0 失败**（`Temp/b22-full-suite-1.log`）；负载复核 24 spinner（跑测时实测 26 python、墙钟 2m55s，负载自证生效）下 `ElAutocompleteTests` **6/6 绿**
    （`Temp/b22-load116-verify2.log`；首跑 `Temp/b22-load116-verify.log` 因负载未自证作废）。口径详见 `docs/autocomplete-alignment.md` 批次㉒记录。

- 🟠 **批次㉑ · #106 `/cascader`：勾选项字体不一致 + 多选标签没显示全 —— 节点类名/标签 DOM 对齐 EP 2.14.0 + 面板换代 key**（2026-10-12）。
  报障是「勾选的『一致性』『反馈』字体和其他项不一样」「多选标签『指南 / discipline / 一...』没显示全」。四项根因全部实测定位（口径与逐行出处见 `docs/cascader-alignment.md` 批次㉑记录）：
  - **`is-active` 没绑勾选态**：EP node.vue:12 `is-active` 绑 `node.checked`，多选勾中的叶子同样蓝粗体；本库只给活动路径末梢打，勾完第二项第一项就掉蓝粗体。改为 `IsSelected || IsChecked`。
  - **`is-selectable` 多打在叶子上**：EP node.vue:9 只在 checkStrictly 打；本库所有叶子都打，而旧 CSS 的
    `.el-cascader-node.is-selectable.in-active-path{color:#606266}`（0,3,0）压过 `.is-active` 蓝粗体（0,2,0），
    键盘光标所在的勾中叶子被压回常规灰——正是截图里「一致性」蓝而「反馈」灰的分裂。改为仅 `CheckStrictly`。
  - **`in-checked-path` 缺失**：EP node.vue:15 checkStrictly 勾中节点与其祖先要打；补 `CheckStrictly && IsOnCheckedPath`。
  - **标签被 120px 顶截断**：本库标签借用了 `el-select__tags-text`（index.css:2240 `max-width:120px`），「指南 / discipline / 一致性」被截成「一...」。
    类名对齐 EP cascader.vue:72/110（`el-cascader__tags`/`el-cascader__tags-text`，`cascader-tag-normal` mixin 无固定 px 顶），旧 CSS 里 EP 规则已在，零新增 CSS。
  - **顺带修掉一个 bUnit 抓不到的渲染损坏**：浏览器实测（`Temp/b21-cascader-diag3.cjs` outerHTML）叶子勾选、其 `li` 尾插 `el-icon-check` postfix 的那次
    ElPopup 增量 diff 会丢同 `li` 的 `class`/`role` 属性帧（面板出现裸 `<li aria-checked="">`）。`RefreshDropDown()` 里 `panelGeneration++` +
    面板外层 div `SetKey` 强制整棵子树重建，结构上免疫。
  - **翻面/牙口实测**：bUnit `ElCascaderTests` **12/12**（新增 4 条：PanelHost 挂宿主复验属性保留、双叶子 `is-active`、checkStrictly
    `in-checked-path`、标签 EP DOM；`Temp/b21-cascader-tests-2/3.log`），全量 **2554 / 0 失败**（基线 2550 + 4，`Temp/b21-full-suite-1/2.log`）；
    五条变异逐条归因——M1 去 `IsChecked→is-active` **1 红**、M2 去 `in-checked-path` **1 红**、M3 标签类名回退 **1 红**、M5 `is-selectable` 回退 **1 红**
    （bUnit 层，每条跑完还原并 `md5sum -c` 校验逐位一致）；M4 去换代 key 对 bUnit **不可见**（12/12 仍绿，`Temp/b21-mut-m4.log`）——损坏只活在真浏览器
    增量 diff 里，判据由常驻探针 A1 段承担。
    常驻探针 `demo/tools/probes/verify-cascader-panel.cjs`（新，5 项）**5/5**
    （`Temp/b21-cascader-probe-resident.log`；改前取证 `Temp/b21-cascader-diag-1.json`、`Temp/b21-cascader-diag3.cjs`，截图 `demo/tools/probes/shots/b21-cascader-verify.png`）。
  - **已知有界差异**：演示页选项 Label `" discipline"` 带前导空格（标签文本「指南 /  discipline / …」双空格），属演示数据非库；过滤建议项仍硬编码
    `is-selectable` 作 DOM 钩子，EP 走同一 node.vue，无用户报障。

- 🟡 **批次㉑ · #110 `/popover`：操作面板「确认/取消」——批次⑰ 已修复，本批复核留绿基线**（2026-10-12）。
  报障是「两个按钮点了不起作用」。排查实锤：两个按钮的 `OnClick` 在批次⑰ `ced0a51` 已接线（演示页注释原文「原来一个 OnClick 都没挂」即此事），
  当前构建浏览器实测全链路正常——点「确认」回显变「确认（第 1 次）」+ 面板收起、重开后点「取消」同样、0 控制台/页面错误
  （`Temp/b21-popover-diag.log`、`Temp/b21-popover-verify.log`）。若本地仍复现，先强刷（Ctrl+F5）排除旧 WASM 缓存。
  常驻探针 `demo/tools/probes/verify-popover-actions.cjs`（新，4 项）**4/4**（`Temp/b21-popover-probe-resident.log`）把该修法钉成回归尺子。

- 🟠 **批次⑳ · #108/#118 `/colorpickerpanel`：饱和度面板小圆点不好拖动 —— 指针拖动通道按 EP 2.14.0 重建**（2026-10-11）。
  报障是「这个不好拖动，这个小点不好拖动」。五条根因全部实测定位（口径与逐行出处见 `docs/color-picker-panel-alignment.md`）：
  - **两张透明 range 吃掉指针**：SV 面板里两张 `opacity:0` 的 280×180 range 以 `pointer-events:auto` 铺满面板，把二维拖动重解释成
    `clientX` 驱动的一维亮度拖动（改前事件流实测 `Temp/b20-drag-diag-1/2.log`）；改为 `pointer-events:none`，
    range 保留在 Tab 序列里给键盘/读屏（方向键判据留在探针 E 段）。
  - **拖动通道根本不存在**：本库只有 click 通道；EP `useDraggable` 在元素上绑 mousedown/touchstart + document 级 move/up
    （`Temp/ep2140-cp-draggable.mjs:7-38`）、`handleDrag` 用 rect 钳制后写 `saturation/value`（`Temp/ep2140-cp-use-sv-panel.mjs:23-39`）。
    按本仓口径（一次 WASM 往返 ≠ 同进程 `resetSize()`）每次手势只量一次矩形，move 途中即跟随、不再等松手。
  - **hue/alpha 的值映射不是 EP 的**：EP 的滑条公式扣 thumb 宽（`(x−thumbW/2)/(width−thumbW)·max`，`Temp/ep2140-cp-use-slider.mjs:19-36`，
    hue max=360、alpha max=100），改前是裸百分比，thumb 中心与指针错位 hue −6.47…+6.53px、alpha 最高 +7.59px（`Temp/b20-slider-follow-1.log`），
    改后值与 EP 公式**零容差**相等、thumb 中心残差 ≤3.55px（`Temp/b20-slider-follow-2.log`）。
  - **click 兜底硬编码 280/180** → 保留为无几何时的兜底（bUnit / 无宿主 JS 那条路），有几何时由指针通道接管；
    EP `handleClick` 的 `focus()` 放在 **click** 而不是按下——按下时浏览器原生默认会把焦点抢回 body（实测翻面 `Temp/b20-cp-probe-1.log` B2 红）。
  - **`@bind-Value` 字符串回环丢通道**：黑 `#000000` 解析回来饱和度塌成 0，圆点当场从右下角甩到左下角（`Temp/b20-drag-diag-3.log` 实测）；
    `OnParametersSet` 加「同色不重导」闸（`ColorEquals`），EP 面板自持 Color 对象、不从自己发布的字符串重导。
  - **翻面/牙口实测**：bUnit `filter FullyQualifiedName~ElColorPickerPanel` **24/24**（`Temp/b20-cp-tests-2.log`，本批新增 14 条），
    全量 **2550 / 0 失败**（`Temp/b20-full-suite-1.log`，基线 2536 + 14）；六条变异全部有牙 ——
    M1 range 退回 `pointer-events:auto` **1 红**、M2 去掉 SV pointermove **3 红**、M3 去掉同色不重导 **1 红**、
    M4 去掉 thumb 宽修正 **5 红**、M6 去掉 pointer capture **2 红**（bUnit 层，`Temp/b20-mut-m1/2/3/4/6.log`，每条跑完还原并校验 md5 相同）；
    M5 去掉 click 聚焦对 bUnit **不可见**（24/24 仍绿，`Temp/b20-mut-m5-bunit.log`）、浏览器探针 B7 红 **42/43**
    （`Temp/b20-mut-m5-probe.log`）—— 这条判据只活在真浏览器里。
    常驻探针 `demo/tools/probes/verify-colorpicker-drag.cjs`（新，43 项 / 7 段）**43/43**
    （`Temp/b20-cp-probe-2.log`；变异电池后复跑仍 43/43，`Temp/b20-cp-probe-3.log`）。
  - **已知有界差异**：视觉圆点仍用 `left:{v/max·100}%`（EP 用 px 定位），远端圆点中心与指针差 ≤4px（EP ≤2px）；
    保留 % 是因为 `HueSliderStyle` 可覆盖条宽。

- 🟡 **批次⑳ · #109/#105 演示页两件：/icon 全量图标目录 ＋ /carousel 真实图片**（2026-10-11）。
  - **#109 `/icon`**：页面末尾新增**全量图标目录**，按 `Icon` 枚举的注释分类（12 类）列出全部 **160 枚**图标
    （通用 27 / 编辑 12 / 数据 7 / 媒体 14 / 方向·导航 19 / 操作 14 / 安全·设置 13 / 删除·危险 3 / 时钟·日历 5 / 通讯 12 / 办公 17 / 其他 17），
    每格显示字形与 kebab 名（`title` 给出 `el-icon-*` 类名）；目录由枚举源脚本化生成（`Temp/b20-icon-catalog.cs.txt`），
    与 `ElIcon.razor.Icon.cs` 一一对应（`None` 除外）。浏览器实测 **9/9**（160 格 / 12 类标题 / 每个字形渲染宽度 >8px +
    `edit-pen`、`d-arrow-left`、`circle-check-filled` 三枚别名兜底抽查，0 控制台错误），见 `Temp/b20-icon-check-1.log`。
  - **#105 `/carousel`**：报障「找一些图片，这样可以显示出效果来」——原演示全是色块数字。全部行为段改用 **4 张本地真实图片**
    （`wwwroot/images/carousel/`，1200×675 JPEG，107–250 KB，离线可用、不引外部 URL）；指示器带文本段保留 Label 文案、
    自动高度段保留文本块（这两段演示各自 API）。浏览器实测 **9/9**（48 张图全部 `object-fit:cover` 且解码完成、12 个轮播实例、
    可见项全部量出实际尺寸、0 个 4xx、autoplay 3s 后激活项 0→1、0 控制台错误），见 `Temp/b20-carousel-check-2.log`。
    两页截图在 `demo/tools/probes/shots/b20-icon-catalog.png`、`b20-carousel-images.png`。

- 🟠 **批次⑲ · #111 `/treeselect`：勾选状态与输入框回显不一致 —— 勾选集合改为按 `Select.Value` 反推**（2026-10-11）。
  报障是「带复选框的树选择器里勾了两项，面板挂着两个勾、输入框只回显一条路径」。EP 2.14.0 源码取证后确认**两条主因**
  （口径与逐行出处见 `docs/tree-select-alignment.md` 的对照表）：
  - **勾选集合只增不减、且从不按值回收**。改前只往 `node.Checked` 打勾，没有任何一处把 `Select.Value` 下发成面板状态；
    EP 在 `watch([modelValue, tree])` 里 `setCheckedKeys(toValidArray(modelValue))`，`onCheck` 更新完值之后
    `nextTick` 再下发一次收口（`Temp/ep2140-treeselect-tree.mjs:11-19`、`:121-123`）—— 本库两条都没有。
  - **点节点文字无条件写值**。EP 的 `onNodeClick` 在 `showCheckbox` 为真时**不**调 `handleOptionSelect`（同文件 `:93-101`），
    只设当前节点、按 `expandOnClickNode` 展开；改前「点文字」与「勾框」是两条各写各的路，值能写到任意层级而勾选永不跟回。
  - 修法：新增「树选择器 + 复选框」模式（`TreeSelectCheckMode`），面板三态全部由值反推 —— 值节点自身 + 其整棵子树（非严格），
    祖先按 EP `reInitChecked` 显**半选**（`Temp/ep2140-tree-model-node.mjs:41-43`），唯一子节点链上的父节点按 `all ⇒ checked`
    转为选中（`:37-40`、`:45-46`）；勾父节点折算成「该子树第一个可用叶子」，折算出的叶子已是当前值或当前值就在这棵子树里则取消选中
    （`tree.mjs:115-119`）；严格模式取父节点自身 Id（`:111`）。**这一模式不再写 `node.Checked`**，因为 `DataSource` 是宿主对象、
    本页五个框共用同一棵 `treeData`，往共享节点打勾会把状态串到别的实例上。`GetCheckedNodes/Keys/HalfCheckedNodes` 同步改为按值反推。
  - **附带两条 EP 口径补齐**：`ElTreeSelect.ExpandOnClickNode` 默认 `false` → **`true`**（`Temp/ep2140-tree-props.mjs:18-21`，
    且下发给内嵌树的是 `!checkStrictly && props.expandOnClickNode`，`tree.mjs:66-68`）—— 改前勾选模式下点父节点文字既不写值又不展开，父节点形同死键；
    勾选模式下选完值**不再收起面板**（`ElSelect.CloseDropDownOnSelect` 新增虚钩子，EP 的 `onCheck` 不调 `handleOptionSelect`）。
  - **翻面/牙口实测**：bUnit `filter FullyQualifiedName~ElTree` **23/23**（`Temp/b19-treeselect-tests-4.log`，本批新增 8 条勾选用例）；
    七条变异全部有牙 —— M-a 勾选退回写 `node.Checked` **6 红**、M-b 勾父写父 Id **2 红**、M-c 取消半选 **3 红**、
    M-d 点文字直接写值 **2 红**、M-e 去掉独子折算 **1 红**、M-f `ExpandOnClickNode` 退回 false **1 红**、
    M-g 去掉 `!checkStrictly` 门 **1 红**（`Temp/b19-mutation-summary-2.log` + `Temp/b19-mut-M-*.log`，每条跑完从快照还原并校验 md5 相同）。
    M-g 初版 **0 红** —— 那条严格模式用例原本只点框、不碰折叠，补「先点父节点文字再断言子树没收」后复跑才拿到 1 红。
    浏览器常驻探针 `demo/tools/probes/verify-treeselect-b19.cjs`（新，44 项 / 8 段）**44/44**（`Temp/b19-probe-final2.log`），
    全量 **2536 / 0 失败**（`Temp/b19-full-suite-2.log`，基线 2528 + 8），`Element.csproj` 全量重建 **0 Warning**（`Temp/b19-warn-census.log`）。
  - **一处自我订正（写下来免得又被当成结论引用）**：原先把「点框的 click 冒泡到节点文字那条 onclick（EP 有 `@click.stop`，
    `Temp/ep2140-tree-tree-node.mjs:65`）」的症状写成「同一项被翻两次 ⇒ 点了没反应」。**删掉那层拦截重建后实测不是这样**：
    点 A-1 的框最终仍是 `checked=[二级节点 A-1]`、点 B 的框仍折成 B-1 —— content 那一遍的折叠与重复勾选，被 label 联动 `input` 的
    **第二次**合成 click 又翻回去了，`rowCount`/`checked` 净读数不变；唯一留得住的差异是多一个当前节点高亮（`is-current`）。
    所以拦截层按 EP 保留、探针改抓 `is-current`（翻面 **41/44**，红的正是三条该判据，`Temp/b19-probe-flip2.log`；
    同轮两条「点文字确实点亮 `is-current`」的正向对照仍绿 ⇒ 判据不恒真），但它**不是**用户报障的症状，主因是上面前两条。
  - **撤回我自己写的「已知有界差异」**：本批曾记「预置 `Value` 是禁用叶子、父节点唯一子节点是它时，EP `getChildState` 跳过 disabled ⇒
    父节点不显选中/半选，本库算成选中」。按行号复读 EP 2.14.0 后证伪：`allWithoutDisable` 全源码只出现 3 次且都在
    `getChildState` 内部（声明/赋值/返回），**无消费者**；`_setCheckedKeys`（`tree-store.mjs:190`）对命中 key 的叶子不看 disabled
    直接 `setChecked(true)`，`reInitChecked` 见 `all` ⇒ 父节点转为选中（`node.mjs:37-40`）—— EP 与本库在这条数据上**表现一致，不是差异**。
    交互侧同样不分叉（EP `some(n => !n.isEffectivelyChecked)` ⇔ 本库 `FirstEnabledLeafId` 跳过禁用节点，父节点都不勾）。
    取证与逐条行号见 `docs/tree-select-alignment.md`。
  - **注释里的 EP 符号名订正**：`ElTree.Features.cs:647`、`ElTreeSelectTests.cs:219`、`:347` 原写着
    `handleParentNodes`，2.14.0 缓存源码里**没有这个符号**（`grep -rn handleParentNodes Temp/ep2140-*.mjs` → 0 命中；
    这名字是我凭记忆写的，出处不明，不再猜），
    实际承担该职责的是 `reInitChecked`（`Temp/ep2140-tree-model-node.mjs:37-40`、`:45-46`），三处注释已改用真名并补行号。
    订正后在新字节上复跑：`Temp/b19-treeselect-tests-5.log` **23/23**、`Temp/b19-full-suite-3.log` **2536/0**、
    探针 `Temp/b19-probe-final3.log` **44/44**（`ElTree.Features.cs` md5 `2434dca22e29b9917d22bf1b1d6b4fb3`；
    浏览器加载的是重新寻址后的 `Element.lyx3603o83.wasm`，旧 `Element.nwcvrbn9ji.wasm` 已从 `_framework` 目录消失）。
    七条变异与探针翻面（41/44）跑在订正**之前**的字节上 —— 本轮只动注释，不改行为。
  - 演示页 `TreeSelectDemo.razor` 的「显示复选框」段补 EP 口径说明，并新增一段 `CheckStrictly="true"`（绑 `value3`，与 `value1` 解耦），
    半选折算与严格模式在浏览器里各有可见面，探针才量得到。

- 🟠 **批次⑱ · #59 `/timepicker`：「至」被裁、滚轮面板选中项不居中、三列滚道不等高 —— 复验出两处真身，都在 `dom.js`**（2026-10-11）。
  用户三条同屏报障（附带「偶发出现滚动条」）。「至」与「滚条」批次⑨ 已治（`index.css:8585` 那段 `flex:1` 注释即现场取证），
  本批把三条全部**量回绿**、给尺子**加牙**，并在量的过程中挖出**选中项不居中的两处真身**：
  - **真身一：收敛目标写成了「列的中心」，EP 的目标是「选中带的中心」**。EP 的带画在
    `.el-time-panel__content::before/::after`（`top:50%; margin-top:-15px; height:32px`）⇒ **带中心 = content 中心 + 1px**；
    而 `ul.el-time-spinner__list::before/after` 各占 **80px** ≠ `(190-32)/2 = 79`，所以 EP 的落点公式 `scrollTop = 索引 × 行高`
    与带中心数值重合、与列中心差那 1px（`Temp/ep214_timepicker_basic-time-spinner.txt:237-240`）。改前实测整列高 1px：
    `Δ=-1px wrapper中心=376 active中心=376 带中心=377 scrollTop=257/961/1441 对 EP=256/960/1440`。
    修法是 `dom.js` 新增 `bandCenter(wrap, wr)`：向上找 `.el-time-panel__content`，偏移从声明里算
    （`getComputedStyle(node,'::before')` 的 `marginTop + height/2`），读不到伪元素才退回列中心；`center()` 与 `nearest()`（滚轮落项）都改用它。
  - **真身二：弹层 `scaleY` 展开动画中途的读数会把 1~2px 残差「焊死」**。`getBoundingClientRect()` 是视觉尺寸（吃 `transform`）、
    `offsetHeight` 是布局尺寸（不吃）。`center()` 的提前收手条件 `|delta| < 1` 用的正是视觉 delta，动画里它被压扁 ⇒ 提前收手、
    `stabilize()` 数够两帧停表、动画一停真实 Δ 变 1~2px 且**再没人纠正**。时间序列实测（连开 5 次、每 100ms 采一帧，
    `Temp/b18-settle-series.log`）最终 `scrollTop` = **384 / 382 / 383 / 383 / 383**（EP=384，5 次里 4 次带残差）；
    加一道"动画停了没"的门（`|wr.height − wrap.offsetHeight| > 0.5` ⇒ 返回 `-1` pending，继续逐帧等），
    同法复跑 **5/5 全部 `scrollTop=384`、`Δ=0`**（`Temp/b18-settle-series-after-animfix.log`），纠正发生在动画停那一刻、无回跳。
  - **「三列等高」根本不是 CSS 给的**：来源是 `TimeSpinnerRenderer.cs:152` 给每列写的 **inline style**
    （`flex:1 1 0; max-height:190px; overflow:auto`）。后果是 M2 变异第一次不带 `!important` 时浏览器算出来仍是
    `max-height:190px`、探针 **42/42 假绿** —— 那不是"改动没影响用户"，而是**变异压根没生效**
    （`Temp/probe-m2-inline.cjs`：`inlineStyle=…max-height:190px…`，CSSOM 里"命中且写 max-height 的规则"为空）。
    加 `!important` 才复现现场。**教训：改几何之前先问"这个尺寸是谁给的"，inline / 属性 / 规则三者胜负次序不同。**
  - **阈值粒度必须比缺陷量级更细**：第一版 b18b 用 `|Δ| ≤ 1px`，把 M3 变异（`center()` 换回列中心）**跑成 42/42 全绿**
    （`Temp/b18-mutation-M3-container-centre.log`）—— 它要抓的正是"整列差 1px"，`≤1` 等于把凶手放走；
    收到 `<1px` 后同一变异立刻 **30 通过 / 12 失败**（`Temp/b18-mutation-M3-tighter-threshold.log`，读数带出
    `content中心=376 带偏移=1(mt-15+h32/2) 带中心=377`）。绿路的亚像素抖动远小于 1px，`<1` 不误伤。
  - **翻面 / 否证实验（每条跑在当轮那份尺子上，跑完逐字节还原）**：M1 把批次⑨ 那条分隔符规则改成命中不到 ⇒
    A2–A5 **4 条红**（`overflow=2 client=17 scroll=19`、字形被相邻输入框盖住 `2.49px`、`contentWidth=7 < 字宽14`、现场读回 `flex=0 1 auto`；
    `Temp/b18-mutation-M1-separator-v3.log`，尺子逐版加牙 v1 报 15/2、v2 报 15/3）；
    M2 只压时列滚道（`!important`）⇒ **4 条等高红** `heights=150/190/190`、范围框 `150/190/190/150/190/190`
    （`Temp/b18-mutation-M2-hour-150px-v3.log`，旧版尺子那份 `11 通过／13 失败` 还额外报 `Δ=-21px`、`centers=356/376/376` ——
    真身一修之前，列中心收敛会把不等高**放大成不居中**）；M3 见上一条；否证项=只撤动画门（见真身二的 5 轮表）。
    两份变异源均从备份覆盖还原：`index.css` 还原后 `md5sum = 91a41a0e72ded97c787cbbabe01b928c` 与
    `git show HEAD:…` 同字节、`git diff --quiet` 干净；`dom.js` 还原后与备份同 md5、服务器不再发 `M3-mutation`。
  - **回归**：新常驻探针 `demo/tools/probes/verify-timepicker-b18.cjs` **18/18**、
    `verify-timepicker-b18b.cjs` **42/42** × 4 轮（`Temp/b18-green-b18b-tight-run1/2/3.log`、
    M2 还原后 `Temp/b18-green-b18b-after-M2-revert.log`；真身一刚修完那份在 `Temp/b18-b18b-after-bandcenter-fix.log`）；
    既有滚轮回归 `probe-time-spinner.cjs` **25/25** —— 1px 落点改动没碰坏"一格一项 / 收敛到带 / 遮罩不压当前项"；
    顺手把该探针写死的**相对**截图路径 `shots/` 改成 `demo/tools/probes/shots/`（从仓根跑会每次造一个仓根 `shots/` 垃圾目录），
    改后复跑仍 **25/25**（`Temp/b18-reg-probe-time-spinner-pathfix.log`）。
    **两条红不是本批缺陷、已如实标注不弱化**：`verify-datetime.cjs` **31/33** 断言的是内置表兜底文案「确认」，
    现场早已接 zh 词条「确定」（`wwwroot/lang/zh-CN.json` 的 `el.datepicker.confirm`，与 EP 同形），
    拿 **HEAD 版 `dom.js`** 复跑同样是这 2 条红（`Temp/b18-reg-verify-datetime-HEADdomjs.log`）⇒ 与本批几何改动无关；
    `probe-time-scrollbar.cjs` **4/15** 整份断言批次⑨ 之前的旧口径（`scrollbar-width:auto` + 已在批次⑧ 折进 `index.css` 的 `fix.css`，
    跑它自带一条 404），两条均在文件头标注"过期、别再当回归用"、断言原样留作历史取证，现行牙口由 b18b 的 H1–H3 承担。
  - 全量 bUnit **2528 通过 / 0 失败 / 0 跳过**（`Temp/b18-full-suite-after-animfix.log`，RC=0）。警告按**去重集合**比对
    （行数在 125／126／127 间浮动，取决于哪些工程被重编）：基线 38 → 本批 39，多出来的是 `ElTree.Features.cs:73 CS1587`
    （`///` 写在 `[Parameter]` 之后），批次⑨ 的构建日志里就有 ⇒ **非本批引入**，本批没碰 `ElTree`，留给 #111 顺手收拾。
  - 文档：`docs/datetime-picker-alignment.md` 新增「/timepicker 三条报障复验…（批次⑱ · #59）」全貌段（含两处真身的 EP 取证算术、
    5 轮 A/B 表、变异表与回归数字），并把「Time-spinner scrollbars（2026-09-22）」一节标 **Superseded on 2026-10-08**；
    `docs/testing-guards.md` 新增第 **64**（阈值粒度要细于缺陷量级、"变异跑绿"必须回头核对原始读数）、
    **65**（视觉 vs 布局几何：任何逐帧收敛循环都要有一道"几何还在动吗"的谓词）、
    **66**（inline style 压过非 `!important` 规则 —— 先证变异真到了浏览器；以及 python 改写 `index.css` 会把整份转成 CRLF 的坑）三条。

- 🟠 **批次⑰（进行中）· #102 `/feedback`：「点击取消的时候，也必须要输入病历号」= Prompt 的输入校验没分 action**（2026-10-11）。
  用户原话：「**/feedback，点击取消的时候，也必须要输入病历号**」—— 这句是现象，不是需求：
  取消本来就该直接关，"必须先输入病历号"是它被校验拦住了。
  - **真身**：`MessageBox.CloseAsync` 调 `InputValidator` **不分 action**，
    改前是 `if (messageBoxOption.ShowInput && messageBoxOption.InputValidator != null)` ⇒
    Prompt 的「取消」「×」「点遮罩」全都走一遍校验，空值时 `return` 掉就是不关。
    EP 2.14.0 只在 `action === "confirm"` 时校验（`Temp/ep2140_messagebox_script.mjs:181`），
    `×` 走 `handleAction("close")`（同文件 `:218`）⇒ 本库只加一个 `action == MessageBoxAction.Confirm &&`，
    确定侧的拦截语义没动（`BeforeClose` 仍在校验之后，`:152`）。
  - **为什么旧的守卫没接住**：`MessageBoxPromptCreatesInputAndBeforeCloseCanBlock` 钉的是 `BeforeClose`，
    那是另一道门，与这条缺陷不同源 ⇒ 本批补 4 条（取消侧 2 条 + 确定侧 2 条**同前提正向对照**，
    因为"把校验整个删掉"也能让取消侧绿），另加 6 个私有 helper。
  - **翻面变异实测**：去掉那个 `action` 条件 ⇒ **红 2 通过 8**，红的恰是取消侧两条，
    失败文案就是用户报的现象（「空值点「取消」被 InputValidator 拦住了 —— 弹层没关 …等 20014ms 结果 Task 仍未完成」，
    `Temp/b17-final-mutant.log`）；还原后 `MessageBox.cs` 逐字节相同（`bytes=8075 sha256=cc2a6bf083b115cd…`），
    过滤 **10/10 × 3**（`Temp/b17-final-feedback.log`、`Temp/b17-final-restored.log`）、全量 **2528 全绿**
    （`Temp/b17-final-full.log`，跑前实测 0 个忙循环）。口径与全部证据见 `docs/message-box-alignment.md`。
  - **收尾复验（本批最后一环之外全部落地）**：临时探针件 `PopupCloseThreadProbeTests.cs` 已删，删后重建跑全量
    **2528 全绿**（`Temp/b17-final-noprobe-1011-150841.log`，125 个警告＝基线）；
    demo 用 `--no-incremental` 整份重建后（WASM 载荷文件名哈希 `Element.p6v29c4ywx` 与撤桩前那份同代 ⇒ 跑的就是撤桩代码）
    再跑常驻浏览器探针 `demo/tools/probes/verify-feedback-prompt-validator.cjs` ⇒ **22/22**
    （`Temp/b17-feedback-probe-noprobe-151727.log`：F10 取消、F12 ×、F13 遮罩、F17「格式报错后再取消」四条关闭路径
    全 PASS，F15/F16 确定侧仍然拦住，`CONSOLE_ERRORS=[]`）。
    **MessageBox 按 ESC 整条路没接**（`ElPopup.razor` 的键盘处理器只挂 dialog 层 `:180`，message-box 层 `:240` 没有，
    `CloseOnPressEscape` 默认 `true` 却无从触发；三形态实测按 ESC 无反应，`Temp/b17-messagebox-escape-diag.log`）
    —— 这属"新增一处行为"，不在 #102 范围内，**等确认再动**。
  - **顺带挖到并修掉的一处测试脚手架缺陷（跨类负载偶发红的真身）**：`JSInterop.SetupVoid(...)` 注册了但没
    `.SetVoidResult()` ⇒ 这次 interop 永远悬着，bUnit ~1s 后把它置成异常并在**下一次派发/等待**里重抛，
    把"断言其实已经成立"盖成红 —— 这就是 `ElRadioTests` 两条轮换红、`ElViewerGrowTests.LoadingGate_*` 偶发红的来源，
    组件本身没错（`ElRadioGroup.razor.cs:198-201`、`:214-217` 是先赋好值、之后才 `await FocusAsync()`）。
    全站补齐后 `SetupVoid` 30 处注册悬空 0 处（14 处缺结果的那批分布在 8 个文件），
    并新增常驻守卫 `VoidInteropGuardTests`（5 条：扫描器 + ≥25 处正向对照 + 三档 fixture）。配方见 `docs/testing-guards.md` 第 51 条。
  - **一条全量套件下偶发的红，本批只定位到倒数第二环（如实记录，没加绕过手段）**：
    `ElFeedbackTests` 的「结果已回传但弹层没从 DOM 移除」.**先前写的「要 CPU 超额订阅才出」本批被自己的实测推翻**：
    完全不烧自造忙循环（`Temp/b17-idle-repeat.log` 第 1 轮、抬头 `python忙循环=0 系统CPU负载=46%`）
    与 `Temp/b17-idle-hunt.log` 第 4 轮（`python进程数=0`）都照样红 ⇒ 它是**全量频率的偶发**，不是负载伪影。
    九次猎取合计 **51 轮全量、这条红抓到 8 次**（每轮都先量"实际在烧几个忙循环"再跑，逐轮日志与红原文见
    `docs/message-box-alignment.md` 的「偶发红」段表）。
  - **本批把自己先前写下的四条解释全部否掉了**（记档，免得下次再引用最像的那版）：
    ① "移除后没渲染"的签名：按宿主分组重算 315 事件插桩 trace，32 次移除里 16 次移除后整场再无该宿主事件、
    而那 16 条所属用例全绿 ⇒ 50% 出现率没有区分力，作废；
    ② "合流批没退到 0"：常驻读数实测 `Renderer._isBatchInProgress=False`、`_hasPendingQueuedRender=False` ⇒ 不是"攒着不派发"
    （但它同样兼容"请求没发出"，所以这一支没被否掉）；
    ③ "第二个宿主接管队列"：红现场 `popupHostCount=1`（该计数只 ++ 不 --，1 才是干净的）⇒ 排除；
    ④ **"⇒ 那一批渲染没被派发"这句（也是本批先前留在 CHANGELOG 里的结论）同样被否掉**：反射强行
    `ComponentBase.StateHasChanged` 再泵 40 轮，**`强制渲染后 host.DialogOptions=0 dom.wrapper=1 nodes.count=5`**
    ⇒ 主动要求的渲染也渲不动它；同时 `生命周期 componentId=2 Renderer._componentStateById=[3] 含2=是`
    且 `hostShadowCount=0`（`OnAfterRenderAsync:755` 按当前模型算，绿路基线开层时=1）
    ⇒ 「组件已不在渲染器生命周期里」这一支也排除，而「有一次渲染对着空模型跑完」成立。
  - **本批还否掉了我按记忆写的两把"尺子"**：`INode.Parent` 走到树根要求 `IDocument`（bUnit 这棵树的根是**游离的
    `HtmlBodyElement`，`Parent` 为 null，且没有 `Owner`/`Document`/`Context` 属性）与"被摘掉的元素 `Parent` 变 null"
    （绿路实测关闭后 `Parent` 仍是 `HtmlBodyElement` ⇒ AngleSharp 不随移除清 `Parent`，这条读数没有判别力，已从超时现场删掉）。
    教训写在 `docs/testing-guards.md` 第 55 条：**每条读数都要先在成功路径上验它真的会变**。
    同一条分叉改由 `popup.Nodes` 承担并验了绿路基线：关闭后根节点序列整个变空（`nodes.count=0`），
    打开时是 `Text | div.el-message-box__wrapper | Text | div.v-modal el-overlay | Text` 5 个
    ⇒ 红时的 `nodes.count=5` 是**DOM 里真挂着这一层整对**，不是尺子陈旧。
  - **超时消息现在带的判别读数已经抓到红了（累计十五次猎取 / 122 轮全量 / 这条红 13 次；第 12 次 ＋6 轮／＋1 次，第 13、14 次 ＋20 轮／＋2 次，第 15 次 ＋24 轮／＋0 次）**：第 10 次猎取第 2 轮（0 忙循环）
    抓到的现场是 `同一实例=否（残留是后来新生成的） 开层样本Id=88668 残留Id=45347075`、`开层时ShadowCount=1 → 红时 0`
    （`Temp/b17-idle-hunt-1011-151805.log`，`PromptCancelClosesEvenWhenInputValidatorRejects`，泵 472 轮）
    ⇒ **开层那一份确实被摘掉过，之后又有一次渲染按非空模型把这一层整对重建出来**，"DOM 里挂着的一直是刚开的那一层"这个默认图景是错的。
    （**这句推断本轮已被自己的否证实验推翻**：健康宿主正常加一层时，"是不是同一实例 / 旧那份还在不在树里"这两条**同样翻面**，
    所以它们判不了"被摘掉又重建"，详见下面第 12 次猎取那条与 `docs/testing-guards.md` 第 60 条。）
    **同时把本批刚写下的另一条结论否掉**：那句「第二层也渲不出来 ⇒ 这个宿主的渲染整体不再落到 DOM」是只数 DOM 就把
    ②"渲染不落地"与③"宿主根本没接到这层（队列侧）"压成一支 —— 两者修法完全不同。已把 `PipelineAliveSceneAsync`
    改成同时打 `模型侧 host.DialogOptions=前→后 service.Dialogs= queueId=` 的三分岔，`CloseScene` 再补
    `残留在本宿主Nodes里=` / `开层样本在本宿主Nodes里=` 两个成员判定。三条新读数都按第 55 条做了绿路正反对照
    （自己那份判「是」、**第二个宿主的同类元素判「否」**＝尺子不恒真、关闭后 `nodes.count=0`、健康宿主走第一分支），
    对照件 `PopupTreeProbeTests.cs` 验完已删（`Temp/b17-treeprobe.log`、`Temp/b17-treeprobe-run-152605.log`；
    那份日志**第一段整份只有** `总共 1 个测试文件与指定模式相匹配` + `没有测试匹配`＝用例没跑成但 RC=0，第二段才是 3/3，
    数字只引用第二段；当时的 `error CS` 只在构建 stdout 里、**没有落盘，所以不引用它的条数**）。
    这条口径连同"只采 DOM 一侧就分不开②③"的教训一起写进 `docs/testing-guards.md` 第 59 条。
  - **第 11 次猎取（0 忙循环，第 19 轮；`Temp/b17-idle-hunt-1011-153400.log`，`PromptHeaderCloseButtonIsNotBlockedByInputValidator`，
    前 18 轮逐轮 2528 全绿）三分读数第一次跑红，③被当场排除**：
    `再开一层实验：… 模型侧 host.DialogOptions=0→1 service.Dialogs=1 queueId=42178652`（与超时现场的 `queueId` 同一个数）
    ⇒ **宿主确实收到了实验新加的那一层**，③"它订阅的队列不是服务上那一份"不成立；`残留在本宿主Nodes里=是` 同时坐实尺子没坏。
    新的事实：残留 wrapper 带的是**开层那一项的** `aria-label=提示 z-index: 2199`（新层标题是「诊断用第二层」），而模型侧装的已是新层。
    **但读数印出的"⇒ 渲染不落地"仍比证据多说一步**（同第 59 条的形状：它还是只数了个数）：
    "个数没涨"同时兼容 ②a 新层的编辑没应用 与 ②b 新层被**就地写进那份旧 wrapper**——message-box 分支的渲染是按下标复用的
    （`ElPopup.razor:158` 是 `for (i<DialogOptions.Count)`，`:240` 的 wrapper **没有** `@key`，只有 dialog 分支 `:180` 有）。
    下一步补两条读数（都先按第 55 条验绿路）：实验之后再打一次残留的 label 与实例 id；以及反射 `Renderer._componentStateById`
    里这个 componentId 的 `_renderFrames`，把"渲染器记的 frame"与"DOM 挂的"逐格对照。**这两条尺子本轮都已造好并在绿路两面验过；
    跟着红落地的：帧尺子＝第 12 次（下面那条），`RenderCount`＝第 13 次，`父链帧表`＝第 14 次（再下面两条）。
    最后一环**仍未定位**，但范围已经缩小：③"宿主与队列脱钩"由上面那条读数排除，剩下的在 ②a（新层的编辑没应用）
    与 ②b（新层被就地写进旧 wrapper，因为这一支没有 `@key`）之间；
    追查用的库内插桩已撤干净并重核：`ElPopup.razor.cs` `bytes=50835 sha256=f3974dd3f4961340…` 逐字节相同、`src` 内插桩标记 0 处，
    撤桩后过滤 **10/10**（`Temp/b17-restored-feedback.log`）、全量 **2528 全绿**（`Temp/b17-restored-full.log`）。
    **没有**为它加超时、并行锁、skip 或降断言来按住它。
  - **第 12 次猎取（0 忙循环，第 6 轮；`Temp/b17-idle-hunt-1011-162538.log`，`PromptCancelClosesEvenWhenInputValidatorRejects`，
    前 5 轮逐轮 2528 全绿；红现场整段另存 `Temp/b17-red-round6-raw.txt`）：帧尺子第一次跟着红落地**（`实际等待 21296ms 泵了 620 轮`）：
    `｜帧 componentId=2 帧来源=状态.CurrentRenderTree._entries._items 已用格=0/0 Element帧=0 div帧=0 帧里的wrapper=0 帧里的v-modal=0 帧里的aria-label=[]`
    而同一句里 `dom.wrapper=1 nodes.count=5 残留在本宿主Nodes里=是`。
    **两侧第一次对上**：移除之后渲染器记的树与 DOM 挂的**确实不同形**（绿路同一次构建两面都取过——开着一层
    `已用格=72/128 帧里的wrapper=1 aria-label=[对照层 | Close this dialog]`、正常关干净 `0/0 … wrapper=0`，
    `Temp/b17-frameprobe.log` 第 13、14 行）。**这条尺子当场点不了 ②a/②b 的名**，因为 `0/0` 的第二个 0 是**容量**：
    池化数组归还后容量就是 0 ⇒ "树真的空"与"读到的是归还后的空壳"当时同形。
    **这个歧义随后被实测消掉了**（对照件 `FrameProbe2RenderCountAndPoolStateTakeGreenBaseline`，验完已删，
    `Temp/b17-frameprobe2-run2.log` `Passed: 1, Total: 1`，读数原文 `Temp/b17-frameprobe2.log`）：
    绿路开一层 `池已归还=否 池最小容量=32 已用格=72/128` ＋ `RenderCount=2`，关干净
    `池已归还=否 池最小容量=32 已用格=0/0` ＋ `RenderCount=6` ⇒ 归还标志两态都是"否"、最小容量恒为 32，
    所以 `已用格=0` 是**活表报 0 格**，不是空壳（教训落成 `docs/testing-guards.md` 第 61 条）。
    **这次红排除的是"落地能力整体丧失"**：`再开一层实验：加之前=1 加之后=1 … 实验前label=[请输入病历号] 实验后label=[诊断用第二层]
    实验前那份现在在Nodes里=不在（这份已被摘掉）` ⇒ 新层**落进 DOM 了**，落的方式是把残留那份**换掉**（个数不变、对象换），
    所以"这个宿主丧失了落地能力"这一支排除。（先前在这里写下的"剩下的是「那一次移除的 DOM 编辑没执行」"**当时说得过头**：
    它和帧侧读数直接矛盾——帧尺子既然报"当前树 0 格"，同一轮加层实验又报"diff 的基准树里还有那一份"，
    那么**渲染器算 diff 用的基准树与我读的 `CurrentRenderTree` 是不是同一份**就是下一问，尚未定位。
    读数印的 `⇒ 第二层渲得出来` 来自 verdict 里的 `|| newLayerInDom`，
    它把"就地换掉残留"也并进"渲得出来"，判据比措辞细。）
    **同时把本批先前的一条结论推翻**：`同一实例=否` ＋ `旧那份不在 Nodes 里` 曾被写成"开层那份被摘掉过、又被谁按非空模型重建回来"，
    而这轮的健康对照实测（**没有任何毛病**的宿主做一次普通加层，`Temp/b17-frameprobe.log` 第 15 行，运行 `Temp/b17-frameprobe-run7.log` `Passed: 2, Total: 2`）
    同样给出 `同一实例=否（换了实例）` ＋ `不在（这份已被摘掉）` ⇒ 这两条身份读数**没有判别力**（原因：这一支按下标复用且
    message-box 分支没有 `@key`，任何动到该位置的渲染都会换掉那份元素），已退回未定位；
    教训落成 `docs/testing-guards.md` **第 60 条**（身份/成员类读数要配一条"预期它就会翻面"的健康对照，对照里也翻面的只当旁证）
    与 **第 61 条**（反射读池化缓冲区时数组长度是容量不是计数：第一版打的 `已用格=128/128` 加了 `_itemsInUse` 之后是 `72/128`，
    多读的 56 格全是上一轮残帧；且 `> 0 ? 截断 : 用容量` 会把合法的"0 格"误当成整池残帧）。
    **`.NET 10` + bUnit 2.7.2 的帧位置实测**（`Temp/b17-renderer-shape.log`，四处坑都记在尺子的注释里）：
    `ComponentState` 上**已经没有 `_renderFrames`**，当前树在 `CurrentRenderTree._entries._items`（实测开一层时 `[128]` 格 / 在用 72）；
    `_componentStateById` 的值是 bUnit 自己的 `RenderedComponent<T>`（**继承** `ComponentState`，按型名相等筛会全筛掉）；
    私有字段不在派生类 `GetFields` 结果里（要沿继承链走）；运行时 `is RenderTreeFrame[]` 为**假**（只按元素型名认）；
    `RenderTreeFrame` 公开面**没有** `Attributes`（属性是其后的 `Attribute` 帧，第一版在此编译报 CS1061）。
    两条绿路对照件（`FrameProbeGreenBaselineWrapperFramesOpenAndClose`、`FrameProbeGreenBaselineSecondLayerLabelsAndIdentity`）
    **验完已删**，删后重建全量 **2528 全绿 / 125 个警告＝基线**（`Temp/b17-full-suite-after-probe-drop.log`：`Passed: 2528, Total: 2528`）。
    上面那件 `池已归还`/`RenderCount` 对照也**验完已删**；它落地时全量一度是 **2528 全绿 / 127 个警告**
    （`Temp/b17-full-suite-after-rendercount.log`，多出的是两个自造的空 `const BindingFlags flags` ⇒ CS0219），
    删干净后重建回到 **2528 全绿 / 125 个警告**（`Temp/b17-full-suite-final-2528.log`）。
    最后一环**仍未定位**，收窄到的形状与剩下的那对矛盾读数写在 `docs/message-box-alignment.md` 上面第 9 条。
  - **第 13、14 次猎取（都是 0 忙循环）把这条红第一次对上了「同操作的绿路基线」，并且把上一条留下的那对矛盾拆掉了**：
    第 13 次 `Temp/b17-idle-hunt-1011-163846.log`（19 轮抓到 1 红，前 18 轮逐轮 `失败: 0，通过: 2528`，
    `PromptHeaderCloseButtonIsNotBlockedByInputValidator`，`实际等待 21338ms 泵了 605 轮`，原文另存 `Temp/b17-red-13th-round19-raw.txt`）
    带着新的 `RenderCount`：`RenderCount=6 开层时RenderCount=2` ＋ `〔池已归还=否 池最小容量=32〕 已用格=0/0 帧里的wrapper=0`
    ＋ `hostShadowCount=0` ＋ `Renderer._componentStateById=[3] 含2=是` ＋ `没有未处理异常`，
    而 `dom.wrapper=1 nodes.count=5 残留在本宿主Nodes里=是`。
    **我先前拿它跟的绿值是「点取消」那条对照（件 `FrameProbe2…`）——不同操作路径的 6 对 6 不能算基准**，
    所以补了同操作（点 ×）的绿路对照 `FrameProbe3HeaderCloseGreenBaselineRenderCountAndFrames`（**验完已删**，
    `Temp/b17-frameprobe3-run5.log`／`-run6`／`-run7` 三次各 `Passed: 1, Total: 1`，读数原文 `Temp/b17-frameprobe3.log`）：
    点 × 的绿路同样 `RenderCount=2 → 6`、`已用格=72/128 → 0/0`、`帧里的wrapper=1 → 0`
    ⇒ **红这一刻渲染器一侧的每一项读数都与"正常关干净"逐项同形，能分开的只有 DOM 一侧**（教训落成 `docs/testing-guards.md` 第 63 条）。
    第 14 次 `Temp/b17-idle-hunt-1011-165436.log`（**第 1 轮就抓到**，`PromptCancelClosesEvenWhenInputValidatorRejects`，
    `实际等待 21325ms 泵了 586 轮`，原文另存 `Temp/b17-red-14th-raw.txt`）带着为下一问新加的**「父链帧表」**：
    `状态^0.CurrentRenderTree 已用=0/0 wrapper=0`、`状态^0._nextRenderTree 0/0`、父链两层 `1/32`、`2/32` 且 `wrapper=0`
    ⇒ **整条链上没有一份帧表还带着这一层**，上一条留下的"diff 的基准树里明明还有那一份"这一支被自己的新读数**否掉**；
    于是"就地换掉残留"那次改动不来自帧树侧，焦点换到 **DOM 落地那一侧自己那份节点记录**（为什么它与帧表不同形）—— **未定位**。
    这节尺子第一版也栽了：通用递归把深度给到 6，绿路"关干净"那一面整行退化成
    `｜帧：没读成（ElementRemovedFromDomException，该项不作结论）`，**连主读数一起没了**（`Temp/b17-frameprobe3-failmsg.txt`）
    ⇒ 改成沿 `ComponentState.ParentComponentState` 一跳一跳走、每人 depth 3，并给这一节单独包 try（副读数不许拖垮主读数），
    对照里同时断言"这一节真扫出了 Current 与 Next 两份"（只断言"这一节存在"会放过空列表）。教训落成 **第 62 条**。
    删掉第三批对照、常驻诊断留在原地重建的全量 **2528 全绿 / 125 个警告＝基线**（`Temp/b17-full-suite-with-chain-ruler.log`）。
  - **第 15 次猎取（0 忙循环，`Temp/b17-idle-hunt-1011-165941.log`）跑满 24 轮、一条红都没抓到**：
    24 轮逐轮 `失败: 0，通过: 2528`，24 个抬头全 `python进程数=0`（系统 CPU 0~90% 区间波动，收尾 48%）
    ⇒ 这 24 轮只补绿样本（累计 **122 轮 / 13 次**），**「父链帧表」这条新尺子到目前仍只有第 14 次那 1 次红现场可引用**；
    最后一环的形状相对上一条没有变化，仍记为**未定位**。
  - **本批还纠正了我自己的实验方法**：先前一轮标着"24 spinner"的负载实测是 38 个忙循环 / 12 逻辑核
    （上一批 spawner 的存活时长没到期就叠加了下一批），那 10 轮里 3 条红只能在 3.2 倍超额订阅下引用，
    不能当常规机器的偶发率。之后每次跑负载都先测"实际在烧几个"再写进日志抬头。

- 🟠 **批次⑰（进行中）· #101 ＋ #112 `/layout` 两个下拉改 ElSelect，顺带挖出库级缺口「预置值首屏不回显」**（2026-10-11）。
  用户原话：「**/layout，这2个下拉框，用 element 的下拉框，渲染一下**」。
  - **换控件**：`LayoutDemo.razor:78-93` 的「箭头模式」「图片填充」由原生 `<select>` 换成 `ElSelect`+`ElOption`，
    槽位 id（`#carousel-arrow-mode` / `#carousel-img-fit`）没动、外层给 `width:190px`。
    选项值用**枚举名**，新增 `carouselArrowText` / `carouselImageFitText` 两个属性做 `enum ⇄ string`：
    getter 读**真正生效的那个枚举**的 `ToString()`（显示与状态同源），setter 用 `Enum.Parse` 而**不用**
    原来那句 `if (Enum.TryParse(...))` 静默忽略 —— 解析不了就是选项值写错了，要当场炸，
    不能让它表现为「下拉框点了没反应」（那是最难查的一类假故障）。
  - **换成 ElSelect 立刻暴露一个库级缺口**（记为 #112）：本库弹层是**首次点开才把 `ChildContent` 交给宿主**
    （`ElSelect.razor.cs:707` 的 `ToggleDropDownAsync`，`:729` 才 `new DropDownOption{OptionContent=ChildContent}`），
    所以首屏 `Options` 是空的，两条「拿 Options 算文本」的回填（`OnParametersSet` / `OnAfterRenderAsync`）全部落空
    ⇒ **预置了 `Value` 的下拉首屏空白、顶着占位符，点开一次才补上**。EP 2.14.0 没这件事：面板是 `vShow`
    （`Temp/ep2140_select_select2.mjs:339`）、选项在 **setup 体内**就 `onOptionCreate`（`option_script.mjs:30`
    → `useSelect.mjs:369-372` 写 `options`/`cachedOptions`），`setSelected`→`getOption`（`useSelect.mjs:209/222`）
    首屏就查得到 `currentLabel`。
  - **改法**：不改成首屏挂弹层（那会让全站每个下拉都提前建出一整份候选项 DOM），而是把 `ChildContent` 的
    渲染帧**只读**扫一遍取匹配那条的 `Text`（同 `ElMarkdownGrow.ExtractMarkdownFromChildContent` 的手法）；
    认识 Region（`@foreach`）/ Element（div 包一层）/ ElOptionGroup 的嵌套 `ChildContent`，深度上限 4；
    没写 `Text` 按 `ElOption.OnInitialized` 口径回落 `Value.ToString()`；片段脱离真实上下文构造不出来时
    `catch` 退回改动前行为（不把整页带崩）。
  - **刻意不跟 EP 的一处**：EP 查不到候选项时兜底显示**裸值**（`useSelect.mjs:241-245`），本库留空不编造；
    要改成 EP 口径只需补一句回落，**等确认再动**。多选（`Multiple`）完全不介入扫描；显式 `Label` 优先；
    点开后以真注册的 `Options` 为准。多选预置值的 tag 文本仍显示裸值（同源另一处待办，本批不顺手改，已用例外置成可见待办）。
  - **浏览器 A/B 三档**（同两条常驻探针，OFF＝把扫描注掉）：`/select` **11/12 → 12/12**，红的恰是 P3
    「四台预置单选首屏全空」（`{"p":"请选择","v":""}`×4）；`/layout` **23/26 → 26/26**，红的恰是 L10/L11/L12
    首屏回显三条，而几何/点选生效/往返可逆（L13~L26）在两档都绿 ⇒ 换控件与修回显是两件独立的事。
    日志 `Temp/b15-select-echo-mutant.log`、`Temp/b15-layout-elselect-mutant.log`、
    `Temp/b15-select-echo-restored.log`、`Temp/b15-layout-elselect-restored.log`、`Temp/b15-layout-elselect.log`。
  - **验证**：新建 `ElSelectInitialLabelTests` **9 条**（每条同时钉「文本对」与「`.el-select-dropdown__item` 仍为 0」，
    防止把「顺手挂载弹层」当成修好了）**9/9**（`Temp/b15-select-label-tests.log`）；
    **变异翻面 M1~M7**（脚本 `Temp/reverse_validate_b13_select_label.py`，含「扫描整个关掉」「取 Value.ToString()」
    「不钻 Element」「不钻 Region」「深度 4→0」「匹配失效抓第一条」「异常兜底收窄」）⇒ **7 条全部被指定用例抓到**，
    还原后 `rc=0` 无红、源文件 `sha256=eacd908064d31715` 与变异前逐字节一致、`MUTATION-TEMP` 残留 **0** 行
    （`Temp/b15-select-label-mutation-rerun.log` 今日复跑，首跑 `Temp/reverse-validate-b13-select-label.log`）。
    常驻探针 `demo/tools/probes/verify-layout-elselect-controls.cjs`（26 项，由批次⑰ 临时脚本提升，
    新增「首屏候选项未挂载」前提与「回显不是裸枚举名」负向对照）与新建 `verify-select-preset-echo.cjs`（12 项）。
    全量 bUnit **2519 / 0 失败**（`Temp/b15-fullsuite-after-select.log`；总数与上一条记录一致 ——
    `ElSelectInitialLabelTests` 早在全量口径内）。口径、EP 出处与偏差见新建的 `docs/select-alignment.md`，
    `docs/carousel-alignment.md`、`docs/组件使用说明.md`、`docs/testing-guards.md` 同步。

- 🟠 **批次⑰（进行中）· #107 `/colorpicker`：「清空不起作用 + 按钮大小和旁边的不一样 + 用 element 渲染」= 三条各自独立的真身**（2026-10-11）。
  用户原话：「**/colorpicker，清空不起作用，按钮大小和旁边的按钮大小一样，用 element 渲染**」。
  - **「清空不起作用」①：默认值和 EP 不一样**。EP 是 `clearable: { type: Boolean, default: true }`
    （`Temp/ep2140-color-picker.ts:196`），本库 `Clearable` 以前默认 `false` ⇒ 绝大多数色板的面板页脚**根本没有「清空」按钮**，
    改前探针实测 `清空 buttons found: 0`、`footer: missing`（`Temp/b13-colorpicker-clear.log`、`Temp/b13-cp-before.log` 整轮 5/12）。默认改 `true`。
  - **「清空不起作用」②：触发器图标吃掉了首点**。图标上挂着 `@onclick + @onclick:stopPropagation`，遗留 CSS 又把它拉成
    整块色板都是命中区。实测连续 6 次首点色块 **6/6 都变成「清空」而不是开面板**
    （`Temp/b13-cp-openrate.log`：命中元素 `I.el-color-picker__icon`，点后色值转空）。EP 口径里这两个图标只是 `v-show` 的显示态
    （`Temp/ep2140-color-picker.vue:82-93`，模板里没有任何 `@click`），清空只由页脚承担 ⇒ 图标去点击语义、遗留尺寸规则从 `index.css` 删除。
    **这是一处「不属 EP 的本库扩展被移除」，没事先征得同意**：要保留这个快捷入口可以加一个默认关闭的参数（如 `HoverClear`）重新提供，等确认。
  - **「按钮大小和旁边的不一样」的量化**：页脚两个裸 `<button>` 只吃 element-ui 遗留的 `.el-color-dropdown__btn{line-height:24px}` /
    `__link-btn{padding:15px}`。用翻面工具（CP3+CP5）把遗留形态注回去实测 **「清空」h=51 对「确定」h=24**
    （`Temp/b14-cp-legacy-geometry.log`；全裸状态的「确定」是 h=26 / w=66，`Temp/b13-cp-before.log`）。
    EP 的 `color-picker.scss` 里没有任何页脚尺寸规则，尺寸来自 `<el-button text/plain size="small">`（模板第 33–53 行）
    ⇒ 页脚两个按钮改用 `ElButton`（`Text` / `Plain` + `ButtonSize.Small`），旧类名只当 DOM 钩子保留。
  - **「清空」的 `disabled` 按 EP 收掉 `!HasColor` 那半**：EP 两个页脚按钮连 `:disabled` 都没有（禁用只拦在触发器），
    没色值时点清空是幂等空操作，不该灰成「坏了」的样子。
  - **演示页**新增「清空（页脚按钮）」段：一台不写参数（正向对照，默认就该有「清空」）+ 一台显式 `Clearable="false"`（负向对照）。
  - **验证**：`ElColorPickerTests` 由 5 条增至 **10 条**（新增 5 条：默认有清空 / 显式关闭不出清空 / 页脚走 `.el-button` 且遗留规则已删 /
    点图标开面板而非清空 / 无值时「清空」仍不禁），**10/10**（`Temp/b14-cp-tests-clean2.log`）。
    **翻面变异 6 条**（脚本 `Temp/mut-cp107-guard.cjs`，支持按 id 单点注入）：单点归因 CP1 红 3 / CP2 红 1 / CP3 红 3 / CP4 红 1 /
    CP5 红 1 / CP6 红 1，6 条齐上 **5 红 5 绿**（`Temp/b14-cp-mutation.log`、`Temp/b14-cp-mutation-perid.log`、`Temp/b14-cp-mutation2.log`），
    回滚后 `MUTATION-TEMP` 残留 **0**，三个源文件回滚后 `git diff` 行数与变异前逐条一致。
    浏览器探针 **12/12**（`Temp/b13-cp-after.log`；干净重建后复跑 `Temp/b14-cp-after-clean.log`，并核对**服务产物 = 磁盘**：
    `index.css` 524670 字节逐字节相同，`Temp/served-css-b14.css`）。全量 bUnit **2519 / 0 失败**（`Temp/b14-fullsuite-after-cp.log`，
    文档同步后复跑同为 2519/0：`Temp/b14-fullsuite-docs-sync.log`）。口径与已知偏差见 `docs/color-picker-alignment.md`。

- 🟠 **批次⑰（进行中）· #103 / #113 `/anchor`：「点过的都变激活色」+「红色的线离字太近」= 三条各自独立的真身**（2026-10-11）。
  用户原话：「**http://localhost:5199/#u-3，章节A B C都点击了，都变成了激活色（整个页面都这样），这样对吗，红色的线离字太近了**」。
  - **marker 从不跟随激活项**：官方 `theme-chalk/src/anchor.scss` 的 marker 基态是 `opacity: 0`，位置由 JS
    `updateMarkerStyle` 按命中的那条链接算行内 style。本库 CSS 只有一条写死的 `top: 8px`、JS 从不写 style
    ⇒ 蓝线永远停在第一条链接旁（激活的却是章节 C）。新增 `elementAnchorUpdateMarker(root, href)`：竖向
    `top = linkRect.top - anchorRect.top + (linkRect.height - markerRect.height)/2`，横向 `left`/`width`，无命中时
    清空行内 style 回基态；量几何放 `requestAnimationFrame`（官方 `nextTick`，要等 `is-active` 刷完，underline 的
    `border-bottom: 2px` 会让链接高 2px）。点击、`ScrollToAsync`、滚动看门狗三处都驱动它。CSS 三条规则改成 EP 口径
    （基态 `opacity:0` + `border-radius:4px`；竖向 `4×14 / top:8 / left:0 / transition top`；横向 `20×2 / bottom:0 / transition left,width`）。
  - **「点过的都亮」的真身不是 `is-active` 叠加**（组件状态任意时刻只有一条命中），而是点击先激活、随后**自己发起的平滑滚动
    把它算回上一条** —— 缺官方 `isScrolling` 那道闸。容器滚到底时目标被 max 夹住就永远算不回来（实测 `/anchor` 的 `#c-2`：
    容器 `scrollTop 0→162` 即上限，目标只到 82px，而阈值 `offset+bound=75`；日志 `Temp/b13-anchor-cursor-active.log`）。
    补 `elementAnchorBeginSuppressScroll`（`scrollend` 精确解除 + `duration+100ms` 定时器兜底，没滚起来时不能永久挡住计算），
    `elementAnchorInit.update()` 入口先查闸。
  - **「红色的线离字太近」**：EP 的口径是竖向根列表缩进 `--el-anchor-padding-indent`（14px）、指示条自己贴容器左边
    （`left: 0`）。本库以前是 marker `left:-12px` 且列表不缩进 —— 默认 marker 勉强还有 10px 净空，而 `cursor` 型指示条
    （`.el-anchor__cursor` 就是 `left:0`）与文字零间距。
  - **顺带两条同源问题**：① 删掉 `history.replaceState(null, "", href)` —— 在 Blazor `<base href="/">` 下 `#c-1` 会被解析到站根，
    实测点完 URL 从 `/anchor` 变成 `http://127.0.0.1:5199/#c-1`（刷新掉回首页），而官方 `anchor.vue` 只在 `onMounted` 读一次
    `location.hash`、从不写地址栏；② `dom.js` 里 `window.elementSetScrollTop` 曾被定义两次，ElTreeV2 的两参版本覆盖了
    带 `behavior` 的三参版本 ⇒ ElAnchor 的 `Duration` 与 ElBacktop 的 smooth **静默退化成瞬间跳转**（ElTable 传的是 `auto`，
    两种实现结果一样所以只有它没受影响），已把 ElTreeV2 那份改名 `elementSetElementScrollTop`。
  - **订正旧文档**：`docs/anchor-alignment.md` 里「`cursor` 插槽（2.10.5）」的说法没有依据 —— EP 2.14.0 的
    `anchor.vue.d.ts` 中 `__VLS_Slots` 只有 `default` 一项，`theme-chalk/el-anchor.css` 里 `cursor` 出现 **0** 次。
    `CursorTemplate` / `.el-anchor__cursor` 是本库扩展，文档已改口径。演示页 `AnchorDemo.razor` 的编程式跳转段
    同时改成「三台 anchor 各点各的 ref」（`scrollTo` 必须打在拥有那段内容的 anchor 上）。
  - **验证**：新增常驻守卫 `AnchorMarkerCaliberGuardTests` **16 条**（CSS 口径 5 / JS 公式与契约 7 / .NET 双侧驱动 1 /
    DOM 结构契约 3），与 `ElAnchorTests` 8 条同跑 **24/24**（`Temp/b13-anchor-guard-clean2.log`），加导航族 **33/33**
    （`Temp/b13-anchor-guard-clean3.log`）。**两轮翻面变异**（脚本 `Temp/mut-anchor-guard.cjs`，逐条 clean↔mutant 字面对调、
    非唯一即 ABORT、落盘后复核形态）：第一轮 12 处 ⇒ **失败 14 / 通过 10**（`Temp/b13-anchor-guard-mutation.log`；多出的 2 条红是
    `ElAnchorTests` 的结构用例被 R1「marker 挪进 list」连带打死，属预期）；第二轮专打第一轮仍全绿的 4 条绑定点
    （调用点 `behavior`、抑制闸调用、TreeV2 改名、`data-anchor-href`）⇒ **恰 4 红 / 20 绿**（`Temp/b13-anchor-guard-mutation2.log`）；
    另用 S1 单点翻面证明新加的 `JSInterop.VerifyInvoke("elementAnchorUpdateMarker")` 正向对照真的绑住了调用。
    两轮回滚后 `MUTATION-TEMP` 残留 **0** 行。全量 bUnit **2518 / 0 失败**（`Temp/b13-fullsuite-final.log`）；
    加守卫后、修导航用例前是 **2517 / 1**（`Temp/b13-fullsuite-after-anchor.log`），那 1 红是真回归：
    `ElNavigationTests.AnchorRendersLinksAndRaisesClickChange` 用严格 JSInterop，组件多了一次 JS 调用就必须配它。
    浏览器几何探针 **15/15**（`Temp/b13-anchor-marker-final.log`：marker 行内 top 跟随章节 A/C/B、`gapMarkerToText 10`、
    cursor 插槽的红游标跟到 `#c-2`、点完 `pathname` 仍是 `/anchor`、smooth 帧数 31），并核对**服务产物 = 磁盘**
    （`dom.js` 200639 字节、`index.css` 552377 字节逐字节相同）以排除 dev server 缓存旧静态资源。
  - **明确没做**（记进 `docs/anchor-alignment.md` 第 6 节）：`is-active` 仍落在 `.el-anchor__item`（官方在 `<a class="el-anchor__link">`）；
    underline+竖向的 marker 官方收成 2px 且不圆角、underline 的 `::before` 灰轨 —— 与本批反馈无关，不顺手改。

- 🟠 **「操作整个界面感觉不流畅」定位收口：外壳无辜，真身是 /grow 冷开一次性建完 20 个章节**（2026-10-10）。
  用户原话：「**操作整个界面感觉不流畅**」。先证外壳不是瓶颈（`Temp/diag-shelf2-1.log`，货架 1 页 vs 8 页两档 A/B）：
  划过 12 个菜单项长任务 **0 个 / 0ms**（两档都是）、静置 3s **0 个 / 0ms**、搜索框打 10 字 **134ms → 349ms**，
  rAF 中位帧 8ms。再定位唯一离群页（`Temp/coldopen-debug-lowload.log`）：Avatar 645ms/0 长任务、Carousel 638ms/71ms、
  TableV2 646ms/145ms，而 **/grow 2642ms / 13 个长任务合计 2160ms / 最大单个 831ms**
  （Release 同口径 2771ms / 最大 1619ms ⇒ 换 Release/AOT 不是杠杆）。
  改法：`GrowDemo.razor` 20 个 `.demo-section` 各加 `@if (builtSections > k)` 闸门（放在 `<h3>` 之后 ⇒ 骨架/锚点/滚动目标一进来就在），
  `OnAfterRenderAsync` 起泵，每 60ms 建 `GrowSectionChunk = 5` 节，独立 `CancellationTokenSource` + `Dispose` 取消。
  判据是**同窗口三臂各 4 轮**（2026-10-10 19:12~19:21，整机负载 26~51%，每轮仪器自证「计时可信」，详见
  `docs/grow-progressive-section-build.md` 第 3 节）：最长一次冻结 **958/1033/967/1008ms → 459/408/442/475ms**（4 轮全赢、两组区间不重叠），
  长任务合计 2386~2595ms → 2580~2805ms（+8%，区间重叠＝持平），落定 5.6~5.9s → 5.6~6.2s，
  **三臂落定 DOM 完全一致**（8553 / 本子树 4243 / svg 228 / 章节 20 / 图表 34，JS 异常 0）⇒ 内容中性。
  另测「一节一帧」（18 次渲染）：冻结与一批 5 节区间重叠分不出高下，但长任务合计 **+70~90%**、落定多花约 3s，故不采用；
  该臂在三臂序列头尾各测一次数字一致（`grow-after-runB.log` / `grow-after-k1-runC.log`）⇒ 排除位置漂移。
  **作废留档**：跨窗口对比一律作废（同一份改前代码两次实测冻结差 **2.8 倍**：1317~2875ms vs 958~1033ms）；
  `grow-after-k5-samewindow.log` 第 4 轮作废（最大帧间隔 38 分钟却无对应长任务＝页面被挂起，仪器原判据只看中位帧被蒙混过关，
  已补「最大帧间隔超出最大长任务 500ms ⇒ 计时不可信」并在该日志尾部追加更正）；
  「20 节全建齐」读数不可信（量到 351ms，而泵物理上至少 1080ms），真身**未定位**，结论不依赖它。
  **明确没做**：没动 keep-alive 货架语义（会反转用户先前批准的设计）、没做真·视口按需渲染（落定 DOM 就不再等于改前，
  24 个打到 /grow 的常驻探针会因深层元素不存在而红）、没改库侧组件。
  - **验证**：全量 bUnit **2489 通过 / 0 失败 / 0 跳过**、RC=0（日志 `Temp/bunit-full-k5.log`；改前基线 **2484**
    取自 `Temp/b16-fullsuite2.log`，差的 5 例全部来自本批新增守卫 `GrowSectionGateGuardTests`）。
    该守卫按五步配方做了**反向验证**：五条各打穿一条判据（批大小 5→1、删一道闸门、两道闸门共用序号、闸门挪到 `<h3>` 前、
    Dispose 里去掉取消），还原后 `GrowDemo.razor` 字节数 93131 与 sha256 逐字节一致、复跑 5/5 全绿（`Temp/guard-growsection-reverse.log`）。
  - **24 个打到 /grow 的常驻探针串行批跑：20 份全绿，4 份红——逐条定性，没有一条算在本批账上**（`Temp/grow-probes-k5/_summary.log`）：
    `verify-gap-margin-equivalence`（EQUAL=12/DIFF=1/ABSENT=1）与 `verify-verifycode-demo`（`InputWidth=200` 实测 267）
    就是本篇 A/B 两组里**已存档的既有红**，逐项同数；`verify-ofd` 18/19 且失败项与仓库里那份 `ofd-result.json` 同名同数。
    第 4 条 `verify-markdown-scrollbar` 是唯一新红（今天 15:14/16:02 两份日志都还是 20/20），但**同一份 build 串行单跑 20/20、
    与另外 3 份 headful 探针 4 路并跑 2 轮各 20/20**，批跑那一次未复现，真身**未定位**。
    三条候选机制已做否证实验并推翻：① 分批建把编辑器推离旧落点（实测 Δy=0、ΔscrollY=0、落点仍在栏内、`matches(':hover')=true`）；
    ② 多浏览器并跑争抢（2 轮 20/20）；③ 窗口被遮挡成隐藏页（故意开全屏遮挡窗置顶，目标页 `visibilityState` 仍 `visible`、`:hover` 仍成立、条带 ratio 仍 0.865）。
    批跑红的那两条**依赖 `:hover`**（纯白 ratio=0/minL=255），紧随其后**不依赖 hover** 的变异对照却 ratio=1 —— 说明截图通道与条带位置都对，
    只有"悬停"这一件事当时没发生；能这样断言是因为该探针自己带了正向对照。诊断件 `Temp/diag-mdhover.cjs` / `Temp/diag-mdhover-occluded.cjs`。

- 🟠 **批次⑯「反馈组件」11 项：10 项修好并实抓，1 项（/treeselect 停滞感）量下来不是树选择独有**（2026-10-10）。
  用户原话（一次报 11 项、每项带截图）：「**http://localhost:5199/datepickerpanel，这个是什么，去掉；…/loading，这个是全屏loading了；
  …/notification，点击这2个没有效果，另外没有primary的吗？…/popover，这个dark没有效果；…/select-v2，这个里面不显示选中的值，并且下拉框不关闭；
  …/space，没对齐；…/tabs，这个+按钮去掉，单独放到外面一个按钮来进行添加…；…/tooltip，这个点击才出现tooltip；
  …/transfer，选中一个，点击右键，全部跑右边去了，选中候选，点击左键，不跑向左边；…/treeselect，点击下拉框有停滞感，选择也有停滞感；
  …/upload，加一个粘贴上传的示例，如果没有这个粘贴的功能，请添加上**」。
  判据是新增常驻探针 **`verify-batch16.cjs`（41 项：L0–L5 / N1–N4 / P1–P6b / S0–S5 / SP1–SP2 / T1–T2 / TT1–TT4 / TR0–TR2 / U1–U3 + Z0/Z0b）**
  ⇒ 终局 **41/41**（`Temp/b16-verify-run9.log`），全量 bUnit **2484 / 0 失败**（`Temp/b16-fullsuite2.log`，基线 2484 未涨），
  七条常驻守卫 **82 / 0**（`Temp/b16-guards.log`），构建 0 错误（`Temp/b16-build5.log`）。逐项如下：
  - **#86 /datepickerpanel**：截图红框是那条 110px 宽、整页高的白带 —— 侧栏在面板内部被逃出来渲染。按
    `.el-picker-panel.has-sidebar:not(.el-popper *)` 收口（`index.css`），页面本身保留（用户要的是去掉那条带，不是删页）。
  - **#87 /loading「指定区域加载」变全屏**：真身在库侧 —— `.el-loading-mask` 是 `position:absolute`（EP 原样），
    而演示目标 `position:static`，绝对定位一路向上相对初始包含块铺开。按 EP `loading/src/service.mjs#addClassList`
    补 `el-loading-parent--relative`（新 JS 助手 `elementLoadingParentRelative` 只在 computed position 不是
    absolute/fixed/sticky 时才加，撤遮罩时按队列里同目标遮罩计数决定是否摘类）。L1 遮罩矩形＝目标矩形（±2px）、
    L3 `maskPos:absolute, h:19 vs vpH:1000`、L4 关后类不残留、**L5 正对照：整屏 loading 仍 fixed 铺满 1500×1000**。
  - **#88 /notification**：定位切换原先不生效，现 N1 右下 / N2 右上 / N3 左下三项分别实测 `right` 贴视口边 + 上下半区不同；
    primary 类型按 EP `TypeComponentsMap`（primary→`InfoFilled`）取证后接通，N4 断言 `el-notification--primary`。
  - **#89 /popover「dark 没效果」挖出四条库侧真错**（详见 `docs/popover-alignment.md` 批次⑯第二轮）：
    ① 默认 `Trigger` 是 Click，EP 的默认是 **hover**（`tooltip/src/trigger2.mjs`）；② hover 事件绑在 `@onmouseover/@onmouseout`
    上，**会冒泡**，参考元素与内联面板之间的每次过渡都各触发一次 show/hide，鼠标早离开面板还挂着；
    ③ `@onclick/@oncontextmenu` 绑在外层 wrapper，而本库面板就内联在 wrapper 里 ⇒ 点面板内部等于再点一次触发器，
    实测「点面板里的文字 → 面板自己关掉」（P5 红，run6/7/8 确定性复现）；④ 没有点外关闭（EP `content2.mjs:112-121`
    的 `onClickOutside` 只在 `trigger !== hover` 时收）。修法：`mouseenter/mouseleave` 挂 wrapper、click/contextmenu 下移到
    `.el-popover__reference`、默认改 hover、按 EP 口径用 `elementPickerOutsideAttach` 绑点外关闭。
    判据：P1 dark 背景 `rgb(48, 49, 51)`、P2 文字翻白、**P3 对照 light 仍白底**（证明 P1 不恒真，且 `closedBefore:0`）、
    P6 只悬停就开 `{"hoverOpened":1}`、P6b 移开即收 `{"stillVisible":0}`、P4 点外收 `{"opened":1,"inside":1,"afterOutside":0}`、
    **P5 同前提对照：点面板内部不收**（证明 P4 不是「点哪都关」）。
  - **#90 /select-v2 不回显 + 下拉不关**：回显的真身是 `SyncSelectedOptions()` 无条件 `selectedOptions.Clear()` 再从
    `Values ?? ExtractValues(Value)` 重算，而非受控多选只绑得到一个标量 `Value` ⇒ 每选一项都把自己的真相抹掉。
    改为「`Values == null` 且算不出值时原样保留 `selectedOptions`」（`Values` 绑了且为空集合才是受控清空），
    表单重置那条路径显式清；不关闭则补 EP `useSelect.mjs#handleClickOutside` 的等价物（本库下拉内联在根节点、不走全局弹层宿主，
    原先没有任何机制替它收面板）。S1 展开列出 6 项、S2 连选两项回显 2 个标签（**修复前这里是 0**）、
    S3 EP 口径多选点选项不收面板、S4 点外收且标签不受损、**S4b 对照：控件内 mousedown 不收**、S5 单选对照点选项收起+回显。
  - **#91 /space 没对齐**：纵向子项被 EP 的兄弟外边距规则（`* + *` → `margin-left:12px`）推右 12px。
    SP1 三项左边缘同为 311px 且子项 margin 全 `0px`，**SP2 同前提对照：space 外相邻按钮的 `margin-left` 仍是真 12px**
    （证明不是把整页外边距都清掉了）。
  - **#92 /tabs**：去掉内置「+」（`.el-tabs__new-tab` 计 0，T1），演示改为外部按钮由开发者代码新增（T2：2→3，`labels` 含「新标签 3」）。
  - **#93 /tooltip「只有点击才出现」**：与 #89② 同根因（`mouseover/mouseout` 冒泡）＋ 一条 `GetPlacementStyle()`
    里少写 `$` 的插值 bug（输出非法 `translate(-50%, -{Offset}px)`，浏览器把**整条 transform 声明丢掉**，弹层贴回按钮脸上）。
    TT1 只悬停即出现、TT2 `transform` 矩阵 `ty:-8` 证明 `Offset` 真被插值、TT2b 与按钮间隙 13px、TT3 移开即消、
    **TT4 对照：click 触发的 tooltip 仍按点击出现**（hover 修复没把 click 路径带坏）。
  - **#94 /transfer「勾 1 项点右箭头全跑右边、点左键不回来」**：`ResetList2` 的左右分区只在 `List1` 上重算，
    已经在右侧的项要求左移时既进不了新 `List2` 又被兜底塞回 `List1` ⇒ 面板内容与 `Value` 脱钩。
    改成在 `List1 ∪ List2` 上按 `Id ∈ value` 做并集分区。TR0 先确认勾选真生效（否则"计数没变"是假绿）、
    TR1 左侧 3→2、右侧 2→3 且**逐标签比对**、TR2 右移回来复原。
  - **#96 /upload 粘贴上传**：能力原先不存在，按 EP 口径在 `document` 上挂 `paste` 监听、只把文件交给当前粘贴区块，
    并新增演示区块。U1 区块在场、U2 合成粘贴事件后 `paste-demo.txt` 进列表、U3 **只进粘贴区块**（区块外列表仍为空）。
    已知局限（写进 `docs/upload-paste-alignment.md`，未修）：监听挂在 document 级，组件 `Dispose` 后共享监听器的归属仍有边界。
  - **#95 /treeselect 停滞感：量下来不是树选择独有**。对照组实抓（`Temp/b16-select-control2.cjs` → `b16-control2.log`）：
    /select 的长任务总量与峰值（**973ms / max 759ms**）等于或差于 /treeselect（**331ms / max 279ms**），
    两页「选择」环节的阻塞都是 **0**，而点击到面板出现的墙钟延迟两边都约 1s。
    ⇒ 先前记在树选择头上的 735–756ms 是**归因错位的读数，已在 `docs/tree-select-alignment.md` 明确作废**；
    这约 0.5–1s 的「点击 → 面板」延迟真身**至今未定位**（按规矩不靠调大超时或加串行锁抹平，留作待复观项）。
  - **S5 的两次 FATAL 不复现，按未解释记录**：run7/run8 探针在 S5 处 `Cannot read properties of null` 崩，
    两份隔离脚本（`Temp/b16-s5-repro.cjs`：变体 A 直连、变体 B 先跑 /popover 的 P4/P5 再过来）都显示单选正常，run9 亦全绿。
    没有把它锁成 workaround，而是让探针在该处**打印状态**（`itemCount` / `hasDropdownIn` / `dropdownsAll` / `rootCls` / `roots`）而不是抛异常，
    真回归时会出数字。
  - **测试侧的两处必要同步**：`ElFeedbackTests` 的 tooltip 用例改 `MouseEnter()/MouseLeave()`、点击型 popover 显式给
    `Trigger="PopperTrigger.Click"` 并点 `.el-popover__reference`（`Temp/b16-test-feedback.log` 1 红 → `…-feedback2.log` 6/6）；
    `ElSelectV2Tests` 构造器补 `JSRuntimeMode.Loose` —— 点外关闭绑的是 document 级 DOM JS，bUnit 没有 DOM，
    这一层只能空转（改前全量 **14 例** 红在 `Bunit.JSRuntimeUnhandledInvocationException … elementPickerOutsideAttach`，非 14 例而是 14 例，
    见 `Temp/b16-fullsuite.log`；还原后 2484/0）。点外关闭的真实行为由常驻探针 S4/S4b 覆盖。
  - **口径证据**：EP 2.14.7 源码已抓进 `Temp/`（`ep216_tooltip_trigger.mjs.txt` / `…trigger2…` / `…content…` / `…content2…` / `…tooltip…`×2），
    popover 默认 trigger、trigger 元素事件绑定、`onClickOutside` 三条口径都按文件:行引用。
  文档同步：`docs/popover-alignment.md`（第二轮四条真错）、`docs/tooltip-alignment.md`、`docs/loading-alignment.md`、
  `docs/notification-alignment.md`、`docs/space-alignment.md`、`docs/tabs-alignment.md`、`docs/date-picker-panel-alignment.md`，
  新建 `docs/select-v2-alignment.md`、`docs/transfer-alignment.md`、`docs/upload-paste-alignment.md`、`docs/tree-select-alignment.md`。
  探针编目与测量层教训（冒泡事件 vs mouseenter、click 不能挂在含内联面板的 wrapper 上、EP 默认 trigger 是 hover 且点外关闭只对
  非 hover 生效、bUnit 对 document 级 DOM JS 要 `Loose`、探针里的 rAF 轮询必须有硬截止否则挂死并误归因长任务）
  记在 `docs/testing-guards.md` §3 编目 **241** 与 §4 第 30–35 条。
- 🟠 **/checkbox「半选状态（Indeterminate）」那一格从来没进过半选（2026-10-10 批次⑮）**。
  用户原话：「**http://localhost:5199/checkbox，点击半选，这个是不是显示不对**」（截图里「全选」是蓝底白勾，不是横线）。
  真身**不在库的渲染上**：`@bind-Indeterminate="indeterminate"` 里 **`ElCheckbox` 根本没有 `IndeterminateChanged` 参数**
  （EP 的 `indeterminate` 本来就是单向属性），而基类 `ElementComponentBase` 挂着
  `[Parameter(CaptureUnmatchedValues = true)] Attributes` —— 多余的那个参数被**静默收进字典，不抛异常、不进 DOM、也不生效**，
  `indeterminate` 恒为初始 `false`，于是「半选」演示从头到尾只是个普通复选框。
  浏览器改前实抓把这条与"样式坏了"分开：`is-indeterminate` 手工贴到别的框上横线立刻长出（正对照 PASS）、
  没贴的框没有横线（负对照 PASS），而那一格 `inputClass="el-checkbox__input   "` 且**整节只有 1 个复选框**
  （`Temp/b15-probe-before.log` 7 通过 / 9 失败）。
  修法按 EP 2.14.7 的 `docs/examples/checkbox/intermediate.vue`（已抓进 `Temp/ep214/`）把演示页改成宿主拥有状态：
  单向 `Indeterminate="@isIndeterminate"` + 4 个子选项的 `ElCheckboxGroup`，初始 4 选 2 ⇒ **一进页面就是横线**，
  点它→4/4 打勾、再点→全空、子项增减→宿主重算（3/4 回横线、4/4 翻成勾）。**库侧零改动**
  （`ElCheckbox.razor.cs` 改前改后 `git diff` 为空），两处既有配合点因此第一次真被用到：
  `Indeterminate` 的 setter（`false` 且当前半选 ⇒ 退回未选，不会自己变打勾）与
  `OnParametersSet` 里 `Status != Status.Indeterminate` 那道守卫（拆掉它 ⇒ 4 例 bUnit 红）。
  判据：新增常驻探针 `verify-checkbox-indeterminate.cjs` **16 项**（读 `::before`/`::after` 的实际几何而不是类名，
  自带 CSS 通路正负对照 + 勾判据自证）⇒ 改后 **16/16**、另连跑两次各 16/16，
  浏览器翻面（初始改 4/4）**9 项红**（`Temp/b15-probe-{after,after-r1,after-r2,mutation}.log`）；
  新增 `ElCheckboxIndeterminateTests.cs` **9 例**（含一个与演示页等价的 `SelectAllHost` 五步闭环，
  以及钉住上面那个陷阱本身的 `BindIndeterminate_IsNotAParameter_…`），两个库侧变异各 **4 例红**
  （`Temp/b15-mut-{M1,M2}.log`，红的不是同一批用例），还原后 9/9。
  全量 bUnit **2484 / 0 失败**（基线 2475 ＋9，`Temp/b15-fullsuite-r1.log`）、七条常驻守卫 **82 / 0**（`Temp/b15-guards.log`）。
  两条测量层教训写进 `docs/testing-guards.md` §4 第 28、29 条：
  「`@bind-X` 少了 `XChanged` 不报错」与「读带 `transition` 的伪元素终值必须等 `getAnimations()` 清空」——
  本探针第一版就是拿固定 250ms 死等读出了「类名已 `is-checked` 但勾还是 `scaleY(0)`」的假红
  （`Temp/b15-diag-anim.log` 实测 6 条 transition 在 250ms 内清空）。口径全文见 `docs/checkbox-alignment.md` §3。
- 🟠 **保活货架的「每开一页，全部已缓存页整页重画」：改成子组件自带的 `ShouldRender` 闸门（2026-10-10 批次⑭）**。
  用户原话：「**鼠标移动到标签页的时候，有种停滞感**」（截图红箭头指着页签条里的「步骤条 Steps」）。
  取证先把嫌疑洗清：hover 本身没占主线程 —— 8 页签来回 3 轮 546 次真实 `mouse.move`、265 次 `pointerover`、
  每个页签都进出过 ≥3 轮，**0 个长任务**，同轮 rAF 中位帧 8ms（`Temp/b14-probe-gate-env.log`）。
  真身在 `demo/Element.Blazor.Demo/KeepAliveRouteView.cs`：货架原先把每个缓存页包成 `RenderFragment` 委托
  再 `AddContent`，而委托片段**每次父级重渲染都必然重新执行**，于是二阶段挂载的 `version++` 一触发，
  货架上每个已缓存页的整棵组件树都被重跑，成本是「页数 × 轮数」。
  **本批第一版修法（只把参数换成引用可比的 `RouteData` + `bool`、页面搬进子组件）实测无效并被否证**：
  `ComponentBase.SetParametersAsync` 从不比对参数值，宿主与页内组件仍旧每轮各跑一次（6 页 × 4 轮各 24 次）。
  现在 `CachedPage` 自己覆写 `ShouldRender()`，且闸门取值在 `BuildRenderTree` 里播种（首帧不走 `ShouldRender`，
  漏播种会让第 2 轮白渲染一次）。判据：新增常驻用例 `KeepAliveShelfRenderFanOutTests` **5 例**
  （旧委托 页数×4 / 只换引用参数 仍旧 4 / 带闸门 1 / 翻面控制项只重渲染被翻的那一页 / 规模口径 3→6 页），
  牙口：把闸门改成恒 `true` ⇒ **3 例红**（1×N→4×N、翻面 1→2、规模 3→12），还原并 `touch` 后 5/5
  （`Temp/b14-shelf-fanout-{tests6,MUTATION-nogate,restored}.log`）。全量 bUnit **2475 / 0 失败**（连跑两次皆尔，
  `Temp/b14-fullsuite-r3.log`、`…-r4.log`；第一次跑红过 1 例与本批无关的既有偶发
  `ElButtonDebounceTests.TrailingDebounce_NextWindowFiresAgain`，单跑 6/6 绿，按未解决原样记录在
  `docs/button-debounce.md`，没有靠调大超时或加串行锁把它抹平）。
  浏览器侧的翻面**没判别成功**（6 个重页在架时再开一个轻页：委托 55ms → 闸门 0ms，两点都远在 90ms 门槛下），
  所以 V2/V5 只是护栏；另外本批为一次「一趟 91s + 一个 1004ms 长任务」的污染读数先后写过三个解释
  （残留 Playwright 实例 / 整机 CPU 打满 / 诊断脚本自己的 CDP Tracing），**后两个都被受控实验否证**
  （16 个忙等进程吃掉 38.6/60 CPU 秒时探针仍 8~10ms 中位帧；同一脚本 `trace`/`notrace` A/B 两档都干净），
  污染真身**至今未定位**（要确定性复现得真去锁屏或断会话，没做危险操作）。为此常驻探针加了第 9 项
  **ENV 环境体检**（测量窗口内逐帧记 rAF 间隔，帧数 <30 或中位 >60ms ⇒ 长任务判据不作结论并整体非 0 退出；
  阈值两侧都是实抓：污染 1008ms vs 健康 8~10ms）。详见 `docs/pro-tabs-alignment.md` §12、
  `docs/testing-guards.md` §3.6 第 10/12 条与 §4 第 26/27 条。刻意没动：隐藏页里 `ElCarousel` 每 4s 空转
  （实测每 12s 改 78 处 DOM 但 0 个长任务，属「该不该顺手管」的开放问题），以及新页首次挂载自身
  那 1.3~1.6s 的 `FireAnimationFrame`（页面组件树体量，不在本批范围）。

- 🟠 **`ElTable` 树形列的「点了没反应」死箭头：判据对齐 EP 2.14.0（2026-10-10 批次⑬）**。
  用户原话：「**http://localhost:5199/table；点击这个不能折叠或展开子内容**」（截图红箭头指着 ⑥ 节
  「心血管内科」「外科」两行的展开箭头）。真身是两层：
  ① 库侧 `ElTable.razor` 旧写法 `treeRow.HasChildren || 真有孩子` 把**宿主字段排在真实父子链前面**，
  误标一个 `true` 的叶子就长出一个箭头，点下去 `ToggleTreeAsync` 翻了它的 `Expanded`、
  `SyncTreeHiddenRows` 推导出的可见集合里却没有它的任何子孙 ⇒ 行数一动不动。
  EP 的判据是 `config.ts:249` 的 `isBoolean(treeNode.expanded) && !treeNode.noLazyChildren`，
  而 `treeNode` 只在 `store/tree.ts:52-67 normalize()` 走到「`children` 是非空数组」时才建。
  ② 演示数据 `TableDemo.BuildDeptTree()` 正好给四个叶子节点（心血管内科 / 普外科 / 产科 / 新生儿科）
  手写了 `true`，而它们的孩子实际挂在祖父节点上。
  改法：库侧只认 `ParentId` 链、字段仅在**懒加载**（设了 `Url`）时生效（等价
  `render-helper.ts:234` 的 `noLazyChildren`）；演示侧的 `HasChildren` 全部由 `ParentId` 反推，
  并把数据改成真的三层链，让「分页 × 多层展开」有样本。
  判据：`verify-table-tree-caret.cjs` **17/17**（含「呼吸内科无箭头 + 缩进差恰 16px」「心血管内科
  **第一击** 5→6 行」「展开全部 12 行且箭头只长在 6 个真有孩子的行上」「第 2 页 新生儿科▸ / 普儿病区·」
  「④ 节 10 叶子无箭头 + 9 分支有箭头」）；`ElTableTreeExpansionTests` 7 → **10** 例；
  全量 bUnit **2470 / 0 失败**。牙口：整批还原成改前跑常驻探针 **13/17**（红的正是
  `T6 6→6 新增=空`、`T7`、`T8`、`T11 12→12` 四条）；只把判据改回字段优先 ⇒ bUnit **恰好 1 红**；
  只去掉懒加载那半句 ⇒ 另 **1 红**。刻意没动：箭头类名与字形的组合方式、`ExpandChange`
  不由树形箭头触发（故工具栏「本页可见 N 行」点箭头后是旧值）、`ElTree` / `ElTreeV2` / `ElTreeSelect`
  的同形状「字段优先」。详见 `docs/table-tree-expansion-alignment.md` §7。

- 🟢 **演示站侧栏「反馈组件」之后 80 条改按菜单名后面的英文首字母升序（2026-10-10 批次⑬）**。
  用户原话：「**反馈组件之后的，按菜单名后面的英文首字母升序排序**」（截图红箭头指着「指示灯 **Light**」）。
  真源只有 `DemoNavigation.MockSection.Entries` 的数组序一处（`SidebarMenu.razor` 逐条渲染不排序，
  搜索与面包屑读 `DemoNavigation.All`）；排序键取标题里最后一段英文，故「指示灯 Light」按 **L**
  落在 `inputtag` 与 `link` 之间，没有英文名的两条（导航组件 / 水印整页）排在尾段最前并保持源序，
  前面 8 条分类聚合页与 `Fixed` 三条一位没挪。判据：新常驻探针 `verify-demo-menu-order.cjs` **10/10**
  （含三点跳转抽查：`/light` `/segmented` `/upload` 与活动页签标题、`data-path` 三者对齐）；
  牙口：还原成改前再跑 **7/10**（`M3` 报 16 处倒退、`M5` Light 掉到最前、`M6` 两条无英文不再在前，
  其余全绿 ⇒ 红的确实只是顺序）。详见 `docs/pro-tabs-alignment.md` §11。


- 🟠 **全站横向滚条 8px → 6px，与 /grow 的 Markdown 编辑器「又粗又常驻」那条竖条（2026-10-10 批次⑫）**。
  用户原话：「**eltable（所有组件的，包括grow）的横向滚动 改为6px吧（不影响grow的对齐）；http://localhost:5199/grow，
  这个竖向滚动条，改为4px，鼠标移动上去才显示，若隐若现**」，并附截图用红箭头指着编辑器两栏之间那条常驻灰条。
  - **横轴回到 6px**：`index.css` 两处口径点（全局 `*::-webkit-scrollbar` 与 `.el-thin-scrollbar`/`.el-tree-v2__body` 段）
    的 `height` 由 8 改 6，`width:4px` 一个字没碰（grow 的表头/表体列对齐吃的是纵轴槽宽，批次⑧ 的 G1/G3 就是这条实测）。
    批次⑩ 的 8px 是把 EP `scrollbar.scss` 的 `height:6px` 再叠 `bottom:2px` 内缩后「拇指实际能压到的宽」；
    用户这次直接指定 6px，就回到 **EP 声明的字面值**，两条理由都记在 CSS 注释里而不是抹掉。
    headful 复验 `verify-scrollbar-wide-h.cjs` **8/8 PASS**（`Temp/b12-scrollbar-6px.log`）：
    `{vSlot:4, hBar:6}`、按住横条真拖 200px **Δ=297**、`th`/`td` 逐列边界逐个相同、批准归零的 `.el-pro-tabs-grow__scroll` 仍 `0×0`。
  - **/grow 那条竖条的真身分两层，只挖到第一层会留下一份假红的探针**：
    ① vendor `editormd.min.css` 的四条 `.editormd ::-webkit-scrollbar…`（`width:7px`/`height:10px`/
    轨道 `background:rgba(0,0,0,.1)` **常显**/拇指 `rgba(0,0,0,.3)` 常显）带一个 class ⇒ 特异性 (0,1,x)
    高于全局基线 `*::-webkit-scrollbar` (0,0,x)，把全站「4px + 平时全透明」在编辑器上**整条顶掉**
    （改前 headful 实测 `.CodeMirror-scroll vSlot:6 hSlot:10`、`.editormd-preview vSlot:6`）。
    复位写在 `Element.Markdown` 自己的 `editor.css` 里，用**与 vendor 完全相同的选择器**
    （`index.html` 里它排在 `editormd.min.css` 之后 → 同特异性后来者赢），不改第三方压缩包。
    ② 复位之后**厚度量对了但还差一步**：headful 几何表（`Temp/b12-editor-geom-1.log`）显示
    `.CodeMirror-scroll` 右缘在 **x=850**，而外层 `.CodeMirror{overflow:hidden}` 只到 **x=801** ——
    它的原生 4px 条整个被裁掉了，用户看得见、拖得动的是 CodeMirror 的**替身** `.CodeMirror-vscrollbar`
    （实测 x 795..800、`z-index:6`、`visibility:visible`）。替身不是被悬停的那个滚动容器本身，
    所以 `:hover::-webkit-scrollbar-thumb` 只在指针正好压在 4px 条上才成立，「移到编辑器上就显形」
    要另补一条按栏点亮的规则（`.CodeMirror:hover .CodeMirror-vscrollbar::-webkit-scrollbar-thumb`）。
  - **验证**（全部 `headless:false`，日志整份落盘）：`verify-markdown-scrollbar.cjs` **20/20 PASS**
    （`Temp/b12-markdown-sb-5.log`）—— 两栏 `vSlot=4`、`.CodeMirror-scroll` 横 `hSlot=6`；
    静止条带 `ratio=0 / minL=255`（截图取像素，UA 伪元素 `getComputedStyle` 读不到）→ 指针落在**栏中间**
    （不是压到条上）后 `ratio=0.86 / 0.98`、`minL=214`；按住 4px 替身竖条拖 150px → 替身 `0→62`（量程 63）
    **且真容器 `.CodeMirror-scroll.scrollTop` 同步 0→62**；预览栏量程仅 11px，判据相应写成「拖到底 `Δ=10`」。
    视觉留证三张 `Temp/b12-md-idle.png` / `-hover-editor.png` / `-hover-preview.png`。
  - **两组变异**：浏览器侧往页面重新注入 vendor 那四条 → 替身 `4→5`、预览栏 `4→6`、静止条带 `ratio=1 / minL=160`
    （常显灰条回来），删掉注入回到 `4px` + `ratio=0`；源码侧从 `editor.css` 删掉按栏点亮那条 →
    `Guard_MarkdownEditorScrollbarFollowsLibraryCaliber` 当场红（`Sub-string not found`），还原后逐字节一致。
  - **踩过的两条自己的坑**（写进 `docs/testing-guards.md` §4 第 23、24 条）：第一版探针在 `.CodeMirror-scroll`
    右缘取条带，量到的其实是**隔壁预览栏的列表文字**（`elementsFromPoint` 命中 `UL`、`minL=51` 而非滚条的 ≈211），
    于是 4 条假红；以及「拖 150px 至少动 40px」这种**不看对象真实量程**的阈值，在只有 11px 可滚范围的预览栏上必然假红。
  - 口径与两层真身见 **`docs/caret-scrollbar-focus-ring-alignment.md` §9**（§8 的 8px 记为批次⑩ 时点值），
    探针与守卫读数见 **`docs/testing-guards.md` §3.16**；`ScrollbarCaliberGuardTests` 4 → **5** 例，
    全量 bUnit **2467 通过 / 0 失败 / 0 跳过**、RC=0（`Temp/b12-fullsuite-2.log`）。

- 🟠 **`/tour` 引导「还是不行、没效果」：遮罩没有洞、面板不贴目标、任意按键就退出、第二次引导不从第一步开始（2026-10-10 批次⑪）**。
  用户第三次反馈「http://localhost:5199/tour，还是不行，没效果，请参考官方继续优化完善」。
  这轮把官方 `2.14.0` 的 tour 目录整包重抓（`tour.vue`/`mask.vue`/`content.vue`/`step.vue`/`helper.ts`/`types.ts`
  与 `theme-chalk/src/tour.scss`），对着源码逐条接线。四个各自独立的真身：
  - **遮罩是一片死的暗罩，没有洞**：`mask.vue` 的暗色不是背景，而是一条 `<path>` ——
    「视口外框 + **反向绕**的目标圆角矩形」在非零环绕规则下中间才没有填充。本库以前只画了外框，
    所以目标既看不见也点不着，`target-area-clickable` 这个参数等于不存在。
    现在 `dom.js` 逐字符抄那段 `d` 的生成（内框起点是 `M(left+r, top)`，**x 减 r、y 不减**；
    洞矩形 = 目标矩形四周各外扩 `gap.offset`，它同时是浮层的虚拟 reference），
    `pointer-events` 的分工也照官方摆正：`.el-tour__mask` 默认 `none`、`<path>` 自带 `auto`、
    只有「无目标或宿主关掉可点击」时 `MaskStyle` 才写内联 `auto`。
    实测洞右边界 `1451` = 目标 `right 1445 + gap.offset 6`，洞中心命中 `div.demo-tour-box` 本身、
    视口角落命中 `path.el-tour__hollow`；第三步洞中心是 `button.el-button`，真点下去演示计数 **0 → 1**。
  - **面板被压成 520×51**：旧的 `.is-bottom{top:auto;bottom:24px}` 与 JS 写的内联 `top` 一起
    把 `position:fixed` 面板的**高度**夹成「视口高 − top − 24」，而 flip 又要读这个高度 ⇒ 高度随坐标变、
    坐标随高度算的反馈环。四条 `is-*` 几何规则删掉（类名保留、只不再带几何）。
    变异对照实测：视口压扁到 400 后 flip 正常翻到上方（`side:"top"`、贴边 16），此时把 `bottom:24px` 写回去，
    `offsetHeight` 立刻从 **126** 变 **376**（内容高度仍 126）；清掉后回 126 且连续 6 帧坐标/高度不变。
  - **点上面的按钮引导就没了**：`OnKeyDownAsync` 以前**不判键名**，面板内任何一次 keydown ——
    连按 Tab 换焦点、在按钮上按空格 —— 都会关掉整条引导。改成照官方只认 `Escape`/`ArrowLeft`/`ArrowRight`
    （ESC 归 `close-on-press-escape` 管，左右键在最后一步＝完成），并在 `dom.js` 补一个 `document` 转发器
    （只认 `isTrusted`、排除 contenteditable 与输入控件、方向键 `preventDefault`），
    因为 Blazor 的 `@onkeydown` 只在冒泡经过面板时才收到。
    同时官方 `mergedShowArrow = !!currentTarget && …` 与 `watch(modelValue)` 两条以前完全没接：
    无目标的那一步连箭头一起撤掉（否则它以「无 `data-side`」的姿态钉在面板左上角），
    关闭时 `current` 归零（否则复用同一个 `ElTour` 再开就停在旧步，看着就是「第二遍引导没有第一步」）。
  - **指示器是自造的 `1 / N` 计数器**：官方 `step.vue` 里根本没有 counter，默认是
    `.el-tour-indicators` / `.el-tour-indicator` 的 N 颗 6×6 圆点、当前步 `is-active`。换成官方同构，
    箭头的沿边坐标由 `placeArrow()` 按洞中心算、`-5px` 贴边交给 CSS 的 `[data-side^=…]` 四条规则。
    实测外接框 `14.14`（10×10 转 45°）、底色 === 面板底色、`pointer-events:none`，
    `bottom`/`right`/`top` 三步分别 `top:-5px` / `left:-5px` / `bottom:-5px` 且中心压在面板边上。
  - **顺带修掉两处演示页的真缺陷**：`#tour-target-2` 原本是整宽块，520px 面板在它右侧放不下、
    flip 恒翻到左边 ⇒ `Placement="right"` 这个演示被自己的翻面逻辑吃掉了（探针实测发现，改成
    `inline-block; width:280px`）；演示补第四步「不设 `Target`」，专门验官方无 reference 分支
    （遮罩整块、无洞、无箭头、面板 `50% + translate3d(-50%,-50%,0)` 居中、不写 `data-side`）。
  - 测试与验证：`ElTourTests` 从 7 例扩到 **31** 例（新增 24：镂空 path、箭头渲染门、`pointer-events`
    两种取值、遮罩点击不关闭（两向）、只认三个键、两条关闭路径都复位、`Gap`/`Offset`/`ZIndex` 落到
    `data-*` 与 style、style 里不许有臆造几何、`elementScrollIntoView` 的调用与跳过条件）。
    本类改用 `JSRuntimeMode.Loose` —— 目标步现在真会调 JS 滚动，bUnit 严格模式抛的异常类型不在
    `ScrollActiveTargetIntoView` 有意吞掉的两种之内；没有把断言写松，而是补两条用例把「调了、参数是什么、
    什么条件下不调」钉住。全量 bUnit **2466 / 0 失败 / 0 跳过**、RC=0，日志 `Temp/b11-fullsuite-4.log`（改前基线 2442）。
    浏览器新增 `verify-tour-hollow-mask.cjs` **57 / 57 PASS**（0 个 JS 异常），
    回归 `verify-tour-upload-i18n.cjs` **27 / 27 PASS**（贴边口径改成 `gap.offset + offset = 16`）。
    **牙口按两组 + 两处浏览器翻面实测**：A 组改坏键盘与复位语义 ⇒ **7 例红**；
    B 组改坏镂空/箭头/`gap` 接线 ⇒ **4 例红**；还原后全绿。
  - 另一条与 tour 无关但同轮暴露的卫生问题：新增的诊断探针 `diag-batch11-i18n-hole.cjs` 把演示站地址写死，
    被常驻守卫 `ProbeHygieneGuardTests.Guard_EveryActiveProbeCanOverrideBaseUrl` 抓红（**这条守卫是对的**），
    按同目录惯例补 `BASE = process.argv[2] || …` 并让 `page.goto` 真正用它。
  - 两个写探针时的坑记档：`.el-tour__hollow` 有官方那条 `transition:all .3s ease`，而 `d` 在 Chromium 里可动画，
    命中测试不停掉就是在动画中途量洞边（实测读到 1461 / 1453 这类中间值）；`MutationObserver` 的属性列表
    不能含 `style`，也不能含 `data-side` —— `position()` 自己写这两样，加了就自激。
  全貌见 `docs/tour-alignment.md` §6-§10。

- 🟠 **`/table` 树形箭头「有时点击 2 次才行」，以及全站横向滚条细到抓不住（2026-10-10 批次⑩）**。
  用户两条反馈（附截图，红框圈住「总院（本院区）/内科系统/心血管内科/呼吸内科」那一列的展开箭头）：
  「http://localhost:5199/table，这种**有时候点击 2 次才行**，另外 eltable（所有组件的吧）的**横向的滚动条稍微宽一点，
  要不不容易拖动**」。两件机制互不相干。
  - **树形那半的真身是「隐藏集」和「展开标志」两本账各写各的**：`ToggleTreeAsync` 的本地分支以前展开走
    `LocalExpand` ⇒ `hiddenRows.RemoveAll(GetAllChildren(node))`，而 `GetAllChildren` 是**递归**取整棵子树的 ——
    点一下「总院」把孙子曾孙全摊开（实测一次冒出 **17 行**，EP 只该出 5 行），
    可被摊开的里层节点自己 `Expanded` 仍是 `false`；再点它的箭头第一下走「展开」分支、行本来就在 ⇒
    **DOM 零变化**，第二下才轮到折叠 ⇒ 这就是「有时要点两次」。差分探针实测改前 **12 次单击有 7 次是空击**。
  - **改成 EP 2.14.0 口径**（实抓 commit `4a0706b`：`store/tree.ts:175-191 toggleTreeExpansion` 只翻被点节点自己的
    `expanded`、子孙一律不动；`table-body/render-helper.ts:249` 的 `display: parent.display && parent.expanded`
    与 `:267` 的 `cur.display = !!(cur.expanded && …)` ⇒ **一行可见 ⇔ 每层祖先都展开**）：
    `ToggleTreeAsync` 本地分支只翻自身标志，随后交给新增的 `SyncTreeHiddenRows()`（`ElTable.razor.cs:2443`）
    沿 `ParentId` 整表重算隐藏集（祖先不在行集时按顶层处理、`visited` 兼作环保护）；
    `CollapseAllTree` 也只置标志、隐藏集交给推导，不再两本账各写。附带得到 EP 的「收起再展开回到原下钻」——
    子孙的 `Expanded` 没被抹掉。`Url` / 懒加载分支与 `LocalExpand`（`ExpandAsync` 仍在用）没动。
  - **横轴单独加宽到 8px，纵轴一个字没碰**：Chromium 的 `::-webkit-scrollbar` **只有 `width` 管纵向条厚度、
    `height` 管横向条厚度**，所以「横条加宽、竖条不动」用一条 `height` 就能改完；批次⑧依赖的
    「grow 面板表头/表体槽宽相等」是纵轴，不受影响。取值参照 EP `theme-chalk/src/scrollbar.scss`：
    `.el-scrollbar__bar.is-horizontal{height:6px}` 再加 `bottom:2px` 内缩，实际可压到的就是 8px。
    改动落在 `index.css:4688`（`*::-webkit-scrollbar`）与 `:9685`（`.el-thin-scrollbar` / `.el-tree-v2__body`）；
    用户批准归零的滚道（grow 面板、时间面板、`.el-pro-tabs-grow__scroll`）**保持 0×0**。
    全站口径据此从「一律 4px」改为「**纵向 4px / 横向 8px**」。
  - **量厚度这件事不能用 headless**：headless Chromium 走**覆盖式滚条**，`offsetHeight-clientHeight` 与
    `offsetWidth-clientWidth` **恒为 0**，改动会被读成「没生效」。改 `headless:false` 之外还把这件事写成判据 ——
    探针第 ① 项用一个溢出裸 `div` 断两轴槽宽非 0，为 0 就宣告整份探针作废（否则哪天环境变回覆盖式，
    全绿的探针其实是静默失明）。第一次还踩了「取点在视口外」的老坑：`y=863` 而视口只有 700 高，
    鼠标按下落在页面空白上，加 `scrollIntoView({block:'center'})` + 视口内断言后拖动才成立。
  - 测试与验证：新增 bUnit `ElTableTreeExpansionTests` **7** 例（含逐节点复算「可见 ⇔ 每层祖先都展开」）与
    `ScrollbarCaliberGuardTests` **4** 例（CSS 文本守卫）；全量 bUnit **2442 通过 / 0 失败 / 0 跳过**
    （改前基线 2431）。浏览器探针 `verify-table-tree-onclick.cjs` **8/8 PASS**（0 个 JS 异常）、
    `verify-scrollbar-wide-h.cjs` **8/8 PASS**（拖动 Δ=298），回归 `verify-tree-table.cjs` `ALL PASS`、
    `verify-tree-pagination.cjs` `15 PASS / 0 FAIL`。**牙口按四处翻面实测**：MUT-1 还原旧展开写法 ⇒ 树形用例
    **5/7 红**（含「第一击即展开」）；MUT-2 把祖先链退化成「只有收起的根才入账」⇒ **5/7 红**（折叠中间层那支赖着不走）；
    CSS 把 `height` 改回 `4px` ⇒ 守卫 `Expected: 8 / Actual: 4` 红、探针 **5/8 FAIL**（厚度与拖动 Δ=0）；还原后全绿。
    全貌见 `docs/table-tree-expansion-alignment.md` 与 `docs/caret-scrollbar-focus-ring-alignment.md` §8。

- 🟠 **引导面板不贴目标、点上面按钮不关闭、切语言按钮文字冻住，以及 `/i18n` 演示页把整页渲染批打崩（2026-10-10 批次⑨）**。
  用户反馈「ElUpload ElTour 接通词条；tour 还是不行、没啥效果，参照官方的试试；点上面的按钮不关闭对话框」，
  随后又报「不能运行，你来修复一下，一直是用的 .net 10，不要降级」。四件事各自的真身与实测证据：
  - **`/i18n` 的表格空态写错了参数名**：`<ElTable Data="emptyData">` + `<ElTableColumn Prop="name">`，
    而真参数是 `DataSource` / `Property`。基类有 `[Parameter(CaptureUnmatchedValues = true)]`，
    两个值被**静默 splat 收走**、`DataSource` 仍是 `null`，列注册时 `ElTable.AddColumn` 抛
    `风格 ElTable 没有设置 DataSource 属性或该属性为空` ⇒ 整批渲染失败，浏览器一条
    `crit: WebAssemblyRenderer[100]`（探针的「0 个 JS 异常」就是被这条顶红的）。
    改成 `DataSource` / `Property` / `Text`，行类型从 `List<Dictionary<string,object>>` 换成强类型小类
    （`AddColumn` 要反射属性名，Dictionary 拿不到 `Name`）。全仓 grep 该笔误现 **0 处**，`.NET 10` 全程没降级、没换 SDK。
  - **面板定位补齐**：官方靠 `@floating-ui/dom` 的 `computePosition` + `autoUpdate`，本库以前只把 `placement`
    映射成 CSS 对齐类、面板恒居中 ——「点了开始引导啥也没发生」和「面板差一大截」是同一个缺口。
    现在 `wwwroot/js/dom.js` 的 IIFE 自己算 12 个 placement + 视口边界钳制，写 `position:fixed` 的 `left/top`
    并**清掉 `transform`**（CSS 的 `left:50% + translateX(-50%)` 会把算好的坐标再挪半个面板宽），
    由 MutationObserver 在面板出现/换步时启动，只在值真变时写回（否则会自激）。实测 `gapY:10`、居中误差 ≤2px、
    `transform:"none"`、`position:"fixed"`。面板仍在 `.el-tour` 内不 teleport（搬走会打断 Blazor 事件绑定，
    正是「点上面的按钮不关闭」的另一半），点「结束导览」后面板与遮罩一起 detach（`mask=true` 已撤）。
  - **跟随要两层，rAF 单独用不够**：只挂 `scroll`/`resize` 收不到**纯重排**（切语言整页重渲染、目标被兄弟节点顶开）；
    而 `requestAnimationFrame` 在隐藏标签页一帧都不发（实测 `visibilityState:hidden` 时 1 秒 0 帧），面板冻在旧坐标。
    故 rAF 链 + `setInterval(tick, 100)` 双保险，面板被摘掉时两路一起停。探针据此验三种扰动：
    换步 / 插 260px 兄弟节点的纯重排 / `.demo-main` 滚 ±120 双向，全部跟回 `gapY:10`。
  - **词条接在 EP 的真实消费点上，不再自造契约**：实抓 EP 2.14.0 —— `upload-content.vue` / `upload.vue`
    **没有任何 `t()` 调用**，所以上一轮把 `el.upload.preview` / `el.upload.continue` 接到触发按钮与拖拽区文案是错的
    （那两句属宿主插槽内容，改语言不动也就是「没啥效果」的一部分）。现在接的是 `upload-list.vue` 真正取词的两处：
    删除图标 `el.upload.delete`（并补 `role="button"` + `tabindex="0"`，与 EP 一致）与悬停提示 `el.upload.deleteTip`
    （`Disabled` 时整条文字不渲染），picture-card 的删除图标同样取名称；`preview` / `continue` 两条键**删除**（无消费者）。
    反向一条：`el.tour.close` 上一轮按「没有消费者」被删，其实面板关闭按钮的 `aria-label` 里写着硬编码 `关闭`
    —— 是**消费者没接线**，补回（zh `关闭` / en `Close`，fallback 沿用改动前 DOM 值，没词条时零变化）。
  - **`OnInitialized` 里 `??= Lang.T(...)` 是假接通**：文字被冻在首次语言上，浏览器实测「开着引导切 English，
    整页都换了、三颗按钮仍写着上一步/下一步」。改成渲染期 `Effective*` 只读属性。
  - 词条规模：zh / en 各 **117 → 123**（引导 4 + 上传文件项 2，`node` 摊平 `el.*` 实抓、键集互等）。
- **一条长期红的常驻守卫顺手清掉**：`TimingGuardTests.Guard_EveryWaitCallSpecifiesTimeout` 抓到
  `ElVideoPlayerGrowDisposeTests.cs:38` 的 `WaitForState` 没写 `timeout`（默认只等 1 秒，并行跑全量时会假失败），
  补 `timeout: TestWait.Timeout`。此前"全量有 1 条已知失败"的说法到此作废。
- 测试与验证：`LangTourUploadTextTests` 新增 **13** 条（引导三态 / 关闭名称三态 / 上传三态 / `Disabled` 不渲染提示 /
  面板开着切语言）；全量 bUnit **2431 通过 / 0 失败 / 0 跳过**（RC=0，日志 `Temp/bunit-full-2.log`）；
  浏览器探针 `demo/tools/probes/verify-tour-upload-i18n.cjs` **26 / 26 PASS**、0 个 JS 异常（日志 `Temp/probe-tour-upload-4.log`）。
  **牙口按两条翻面实验实测**：① 把 `ElTour.razor` 的 `aria-label` 改回硬编码 `关闭` ⇒ 该类恰好 2 条红，还原后 13/13；
  ② 在浏览器里把遮罩 `pointer-events:none` ⇒ 遮罩命中测试从 `allMask=true` 翻成 `false`（命中退回页面元素）。
  另有两条探针自己的假红已修：`elementFromPoint` 拿**视口外**坐标恒返回 `null`（被读成"遮罩没盖住"），
  以及两条恒真的 `check(name, true, …)` 空壳断言 —— 现在都绑定实测值。
  全貌见 `docs/tour-alignment.md` §2/§6/§7/§8 与 `docs/lang-i18n.md` §6（第九批）/§8/§11。

- 🟠 **面板滚条槽比表头宽 4px 把列拽歪、开关外面那圈浏览器原生黑环、排序上下箭头横向错开还把表头撑高（2026-10-09 批次⑧）**。
  用户四条实测反馈（附 3 张截图）：「这个滚动条太粗了，如果能方便去掉就去掉吧，不能去掉就调整小一点；另外这个外边的黑框不是
  element 格式的吧，调整一下；另外 sort 的上下箭头垂直对齐，要显的专业一点；Pages/SelectDemo.razor:114 的城市下拉仍有真实城市名
  （含"青岛"），虚构化；」三件事机制互不相干，全貌见 `docs/caret-scrollbar-focus-ring-alignment.md`：
  - **箭头的"不专业"是两处遗留叠出来的，且不止是审美**：`index.css` 的 `.el-table .caret-wrapper` 段落以前只重写颜色与
    `display`，几何仍由打包进来的 element-ui 压缩段决定（盒 **24×34**、`ascending top:5px`、`descending bottom:7px`），
    再叠本段自己写的 `margin-left:4px` / `-5px` ⇒ 两个三角形**横向错开实测 4.5px**，而 34px 的盒把可排序列的 `.cell` 撑到 34px、
    **整行表头从 40px 涨到 51px**（同页其他行仍 40，`Temp/_b8_carets.txt` 的 `section 11` 直接量到）。
    按 EP 2.14.0 `table.scss:515-544` 原样改写（盒 `24×14`、两三角形 `position:absolute + left:7px`、`top:-5px`/`bottom:-3px`、
    **删掉两条 `margin-left`**）。改后：`24×14`、上下左边线 `1002.9/1002.9`（错开 0）、箭头组 vs 文字垂直中心 **Δ0.4px**、
    可排序行 `40 == 40`；翻面注入旧几何立刻复现 `wrapH 34 / stagger 4.5 / thH 51`，撤掉回 `14 / 0 / 40`。
    顺带把批次⑦探针 `verify-batch7.cjs` 的 H2 口径从 24×34 改成 24×14（注释写明批次），两份既有文档里 24×34 的说法同步
    （历史抓取原文按"不改写"留着并加了勘误）。
  - **滚条"太粗"的真身是两侧不等宽，不只是粗细**：面板为了让表头/表体可视宽度一致，两个容器都强制
    `overflow-y:scroll` + `scrollbar-gutter:stable`，而组件里只给**表体**写了 `::-webkit-scrollbar{width:8px}`、表头只被全局
    `*::-webkit-scrollbar{width:4px}` 命中 ⇒ 槽宽 **8 vs 4**，列边界按位置越往右偏越多（实测 `0 / 0.9 / 2.9 / 4px`，
    `headClientW 312` vs `bodyClientW 308`）。改成**一条规则同时命中两个 wrapper、宽度取 4**（相等由同一处保证，不靠两处巧合）。
    改后两侧槽都 `4`、`clientW 312/312`、三列边界差**全 0**；翻面只给表体注入 8px → 槽 `[8,4]`、边界差**逐列复现 0.9/2.9/4**。
    ⚠️ 踩到一条 Chromium 特性：**动态追加的 `::-webkit-scrollbar` 规则不会自己重排滚动条**，注入/撤掉后必须逼一次布局失效才量得到
    （第一次写翻面时槽宽纹丝不动，看着像"判据没牙"，实为探针缺重排）。
  - **「能不能去掉」按用户的要求实测了，不是猜的**：`diag-batch8-scrollbar-zero.cjs` 在同一面板上量三种写法 ——
    现状 4px、`::-webkit-scrollbar{width:0}`、标准 `scrollbar-width:none`，**三种都列边界差全 0、滚轮都能滚**（`inner 0→43`），
    所以"去掉"在几何上是可行的。仍保留 4px 的两条代价写在账上：归零后 thumb 一起消失、面板没有"这里还能滚"的视觉提示
    （`scrollRange 43` 确有溢出内容），且本仓有「所有组件一律 4px 细滚条」的硬口径，做成无滚条会是全库唯一例外。
    **要换成 0 只需把那一条规则的 `width/height` 改成 `0`**（探针 B/C 段就是这条改动的预演）。
  - **开关外那圈黑框是浏览器画的，不是本库任何一条规则**：`ElSwitch` 根节点是 `div[tabindex=0]`，里面 0×0 的透明复选框
    **原本也在 Tab 序里**，真键盘 Tab 常住的是那个 input —— 取证 computed `outline: rgb(16, 16, 16) auto 1px`、
    `outline-offset 2px`、`:focus-visible=true`（根节点反而 `none`）。这就是"框套框"的黑环。改法两件事成对做：
    ① input `tabindex="-1"`（根节点成唯一停靠点，键盘处理本来就挂在根节点）；② 压掉两处原生环 + 在 `.el-switch__core` 上画
    EP 的环 `outline: 2px solid var(--el-switch-on-color); outline-offset: 1px`。**只写 `outline:none` 是把可达性提示删掉**，
    必须与替代环成对（本仓 `ElDropDownTableGrow` 早就是这个写法）。实测：根节点 computed `none`、core
    `rgb(64, 158, 255) solid 2px` / offset `1px`；整页 tabbable `265`、开关根节点在 `index 110`、input `index -1`；
    三处翻面各有牙（注入 `core{outline:none}` 环消失、注回原生环 computed 变回 `rgb(16,16,16) auto 1px`、input 的
    `tabindex` 改回 `0` 就重新出现在 Tab 序 `111`／总数 `266`）。
    ⚠️ 这一段起初追加在 `index.css` 文件末尾，被 Markdown 组件那条"只用广泛支持特性"的 CSS 守卫判红两次：
    该守卫的取段范围是「段标记 → **文件末尾**」，且标记词按**第一次字面命中**算 —— 段落含 `:focus-visible` 必须往前挪，
    连注释里都不能出现那个组件的类名前缀。两条都写进了 `docs/testing-guards.md` §1 的告诫里。
  - **演示数据**：`SelectDemo.razor:112-115` 的十个真实城市（含"青岛"）换成虚构名（云州/岚川/星澜/青梧/澜溪/沐野/栖迟/海若/
    望舒/榕溪），`grep` 旧城市名 + `山东` 在 `demo/Element.Blazor.Demo/{Pages,Models}` 命中 **0**；
    批次⑦文档把这条记成"未确认边界"，本批按用户明确要求做完并同步那篇 §5。
  - 配 **5 条 bUnit**（开关两个 Tab 停靠点契约 2 条 + 三条 CSS 文本守卫：焦点环成对性、箭头盒 EP 几何 7 正向 + 1 负向、
    滚条槽"两侧同一条规则且 width 只有一个值"），全量 **2381/2381**（批次⑦基线 2376 ＋5）。
    变异实测：四处锚点各改一处 → **失败 5 / 通过 45 / 总计 50**（`tabindex` 那处一次打红两条），还原 + `touch` 复跑 **50/50**、
    基线 **50/50**。⚠️ 第一次变异跑差点把"没跑过测试"当成绿：包装脚本已拼好 `dotnet test --nologo`，我又把 `"test"`
    当子参数传进去 ⇒ `MSB1009 项目文件不存在。开关: test`，三个阶段**一条用例都没跑**而外层 python 退出码照样 0；
    改成"退出码从日志首行 `EXIT n` 读 + 断言日志必须含 `总计:`"之后才拿到上面那组数（`Temp/bunit-batch8-mutation-*.log`）。
  - 探针与复跑：新 `verify-batch8.cjs` **17/17 PASS**（C/G/F 三段，含 4 处翻面与 F1 正向对照）；
    `verify-batch7` **22/22**、`verify-batch5` **14 项 0 FAIL**、`verify-batch6` **15/15**，`CONSOLE_ERRORS=[]` 全空。
    ⚠️ 两处判据写法被本批纠正：`G2/G3` 起初用后代选择器 `.el-table__body tbody tr` 取面板的行，**取到的是面板自己的表头行**——
    面板是渲染在主表 `tbody>tr>td` 里的（不是 teleport），祖先链会污染后代选择器，一律改 `:scope >` 逐层走；
    `F2` 起初按"尺寸 > 0"过滤 tabbable，而 0×0 的 input 无论 `tabindex` 是什么都会被滤掉 ⇒ 那条负向断言恒绿空转，
    改成只按 `display:none / visibility:hidden / disabled / tabindex=-1` 过滤并补 F7 当牙齿检查。

- 🟠 **表头文字被挤进箭头盒、列宽没给图标留位置、grow 面板"只有右侧窄条滚得动"、样式字典被 `ToString()` 写进 DOM（2026-10-09）**。
  用户三条实测反馈（附 3 张截图）＋自查出的第 4 条，四件事机制互不相干，全貌见 `docs/table-header-cell-and-column-width.md`：
  - **表头「没撑开」的真身是列宽口径，不是行高**（用户两次纠正后定的口径：「不是行高没撑开，这种的话，应该宽度自适应」
    「如果设定了列宽…再设定了排序或过滤，这 2 个是单独算的」）：本仓列宽由 `<col width>` 钉死、没有 EP 的 realWidth 布局引擎，
    图标盒若算在声明宽度**里面**，`Width="110"` 的「单价(元)」带排序时留给文字只剩 66px，必被 `.cell{overflow:hidden}` 裁。
    改成 **`<col width>` = 声明宽(`Width > MinWidth > 80`) + 表头图标盒加量**（`.caret-wrapper` 24、漏斗 18 = 图标 12 + `margin-left:6`，
    都有 42；百分比列不加、原样透传），加量同时进 `ColumnsTotalWidth` → `width:max(100%,{总宽}px)` 的求和
    —— **宁可横向滚动也不压列**。表头渲染漏斗的 `@if` 与列宽判据共用同一个 `HasFilterTrigger(header)`，避免"渲染了却没留宽度"。
    实测（800×700 视口，`tableW 842 == Σcol 842`）：`70+0→70`、`220+18→238`、`160+18→178`、`110+24→134`、`180+42→222`，
    逐列 `th 实测 == col`、文字净可用 `49/199/139/89/160` 全 ≥ 需要的 `48/198/138/88/158`；
    **翻面**（收回加量并钉住表宽）四列逐列翻红 `181<198 / 121<138 / 65<88 / 118<158`，还原后数字与改前逐列相同。
  - **表头 `.cell` 的结构对齐 EP 2.14.0**（`table-header/index.ts:260-295`：先是 `column.label` 直接文本，再是只装两个
    `sort-caret` 的 `caret-wrapper`）：旧写法把 label 放进了 `caret-wrapper`，而库 CSS 给这个盒的是 `width:24px / flex-direction:column`
    （实测命中盒 **24×34**），窄列文字被挤成一字一行再被裁。改后 `caret-wrapper` 内文字实测 `""`、`.cell` 无裁（`sh34/h34`）；
    翻面变异（把文字塞回去）立刻 `sh138/h34` 红。
  - **grow 面板内滚轮失效 ＝ 内层那条 `overscroll-behavior: contain`**：本组件的 `.el-table__body-wrapper` 为预留滚条槽
    被强制 `overflow-y:scroll`，**即使数据没溢出它也是滚动容器**，再挂 `contain` 就把滚轮吞在这里 —— 实测「只有落在面板右侧
    那条 ~8px padding 上才滚得动，落在行上一动不动（`panel.scrollTop` 恒 0）」。删掉这条规则（边界防穿透由面板自己的 `contain` 承担）后
    `inner 0→43` → `panel 0→16` → `.demo-main 115→115` 三段接力恢复；加回 `contain` 的翻面复现出原症状。
  - **自查出的第 4 条：`style="@dict"` 被 Blazor 渲染成 `Dictionary.ToString()`**。浏览器把整条声明当非法 CSS 丢掉，
    于是 `RowStyle` / `HeaderRowStyle` / `HeaderCellStyle` / `CellStyle` 按字典传的全部不生效，还把垃圾串留在属性里。
    新增 `ElTable.razor.cs` 的 `StyleToString`（`HtmlPropertyBuilder.CreateCssStyleBuilder()`，空字典返回 null 即不渲染属性），
    `ElTable.razor` 的 5 处 `style` 统一包一层。实测 `box-shadow:inset 0 -3px 0 #409eff` 的 computed 真变出阴影、
    整页含 `System.Collections` 的元素数 **0**；换成旧垃圾串的翻面让 computed 回到 `none`。
  - **演示数据去真实化**（用户要求「整个 demo 里面的都这样改」）：5 家供应商换成虚构名（云溪医药控股 / 北屿医药供应链 /
    星澜医药站 / 青梧医药物流 / 澜溪器械耗材），联系人连同 `Patients` 里复用的四个中文名换成英文（Kevin Carter 等），
    共 11 个文件（`MockData.cs` + 10 个 `Pages/*.razor`）；全仓 `grep` 旧名命中 **0**，探针 `/table` 与 `/basic` 全文查旧名
    `命中=[]`（配了正向对照 D1，证明页面确实有数据可查）。⚠️ `SelectDemo.razor` 的城市选项仍含真实城市名，那是通用下拉演示、
    不在"供应商/联系人"之列，边界记在文档里待确认。
  - 配 **11 条 bUnit**（`ElTableHeaderCellTests.cs`：结构 2 + 字典 3 + 宽度 6）；宽度逻辑变异体（去掉加量）实测
    **失败 4 / 通过 7**，4 条正是四条宽度契约，还原 + `touch` 后 **11/11**；bUnit 全量 **2376/2376**（下一批基线）。
    `verify-batch7.cjs` 22 项 **22 PASS / 0 FAIL / 0 SKIP**、`CONSOLE_ERRORS=[]`（日志 `Temp/verify-batch7.log`）。
  - 顺手拆掉一个**假红**（不是产品缺陷）：`verify-batch6.cjs` 的 C 段（面板内滚轮）取的点位 `panel.top + 40` 落在搜索框上，
    实测命中 `el-input__inner` —— 它不是滚动容器，滚不动是**正确行为**。点位改到内层第一行中心后 **15/15 PASS 连续 6 次**
    （3 次热跑 + 冷重启演示服务后 3 次）。另有一条历史读数（F 段 1274ms）本批 7 次复跑只量到 63~94ms，**未复现、未定位机制**，
    所以没有放宽阈值（仍 400ms），如实记在文档 §7.4。

- 🟠 **`ElDropDownTableGrow` 向上弹钉在视口顶、开着面板滚轮失效、⑪ 列头菜单 1.2 秒（2026-10-09）**。
  三条用户实测反馈（附 3 张截图）+ 修的过程中自查出的第 4 条，机制互不相干，全貌见 `docs/dropdown-table-grow-placement.md`：
  - **向上弹位置不对 ＝ 把 `max-height` 当高度用**：`dom.js` 的 `place()` 旧写法两分支都算 `top`，而向上弹时
    `maxH` 恰等于「上方全部余量」，代入得 `top ≡ margin` ⇒ 面板钉到视口顶，与触发器间空出**实测 176px**
    缝隙（面板真实高只 298px）。改成向上时锚 `bottom`、`top:auto`（fixed 盒 `top`/`bottom` 同时给值会按两边拉伸，
    另一条必须显式 `auto`）。实测 `top:auto; bottom:83px`、`gapUp=4`；**翻面变异**（改回旧公式）立刻红且
    复现出 `top:8px / gapUp:176` 的原始形状。
  - **滚轮失效 ＝ 全屏 `position:fixed` 遮罩把滚动链改道到「视口」**：`.el-dropdown-table-grow__backdrop`
    命中滚轮后沿链找不到可滚祖先，链头是视口，而本站真正在滚的是 `.demo-main`（实测 `scrollHeight 14351 / clientHeight 336`）
    ⇒ 开着面板整页滚不动。删掉 backdrop（标记 + CSS），「点外面收起」改用组件库现成的
    `elementPickerOutsideAttach`（document 捕获阶段 `mousedown`，与 `ElDateTimePicker`/`ElTimePicker` 同一套）。
    实测开着面板 `mainTop 0→240→480`、`/grow` 上 `8355→8595→8835`；加回 backdrop 的翻面变异复现出
    `[0,0,0]` 与 `[8355,8355,8355]` 的"滚不动"形状。**行为变化（有意接受，与 EP 一致）**：点外面现在收起面板
    *并且*落到底层元素上（实测单击一行＝选中 + 关闭一次完成），旧遮罩会把这一下吃掉。
  - **⑪ 列头菜单慢 ＝ Blazor 渲染扇出，不是那次 JS 往返**：菜单状态挂在 `/table` 页面字段上，点一次列头连带
    重渲染整页 12 段。同尺对照实测（口径：点击 → 首个 DOM mutation）：页面级 `StateHasChanged` 本身值 473~557ms、组件级 176~214ms，
    而 ⑪ 改前 **1155 / 1271ms**；`demoGetLastHeaderClick` 只占 0ms JS + 13~40ms 派发，是被冤枉的。整段搬进独立组件
    `Components/HeaderMenuDemo.razor` 后实测 **107 / 82ms**（同口径）；验收探针 F 四次独立运行 79 / 107 / 143 / 153ms，阈值 400ms。
    菜单节点改为常驻、收起只 `display:none`
    —— `ElementReference` 必须稳定，否则 detach 解不到正确的元素。
  - **自查出的第 4 条**：⑪ 菜单自己的 `.demo-colmenu-backdrop` 复刻同一滚轮缺陷（实测 `5642→5642`），同法删除后
    `5642→5882→5402`。
  - 取证中还拆掉一个**假红**：C3 一度报 `8355→8355`，逐点位 dump 证明探网点 `(731,28)` 落在吸顶的 `.demo-header`
    上 —— 它不在 `.demo-main` 内，那里滚滚轮本就不该动页面。改为 `pickPagePoint()` 用 `elementFromPoint`
    校验「在 `.demo-main` 内 ∧ 不在 `.demo-header` 内 ∧ 不在面板内」，并按当前滚动位置选滚轮方向（贴底还往下滚也是假红）。
  - 配 4 条 bUnit（`OpenPanel_UsesOutsideHandlerInsteadOfBackdrop` 等，负向断言都配正向对照）；
    新 bUnit 全量 **2365/2365**；`verify-batch6.cjs` 15 项 **14 PASS / 1 SKIP / 0 FAIL**
    （SKIP 是面板内容不足、自滚前提不成立，真验得住的位置由 `/grow` 的 C2 覆盖：`panelTop 0→56`，`sh 288 > ch 232`）。

- 🟠 **表头漏斗「点了没反应」、侧栏 tooltip「没显示全」、高亮第 1 行「不起作用」、滚轮「不滚」四件事同源不同因（2026-10-09）**。
  四条都来自用户实测反馈（附 5 张截图），归因各走各的，**没有一条是顺着直觉修的**：
  - **筛选点不开 ＝ 裁剪链**：面板原本写在漏斗触发器里面，而 `.cell` 是 `overflow:hidden` 的单行框（**实测 23px 高**，
    漏斗本身 12px），absolute 的包含块与裁剪来自同一条链 `.cell(hidden) → th → .el-table__header-wrapper(hidden) → .el-table(hidden)`
    ⇒ 面板刚冒头就整条被裁。改成**面板由表格在表体后渲染、打开时用一次 JS 往返（`window.elementViewportRect`）量出漏斗的
    视口矩形钉成 `position:fixed`**（`TableFilterPopper.BuildPanelStyle`，`Gap=4 / EdgeMargin=8 / MinRoomBelow=140 /
    OptionPanelWidth=150 / AdvancedPanelWidth=320`；`max-height` **只给选项列表**，条件面板给了就会裁掉自己那颗运算符下拉）。
    配 `.el-table-filter__backdrop` 遮罩（点空白收起）+ 面板上 `@onwheel` 收起（一次定标，一滚就错位），每次重开 `@key` 自增使草稿重取。
    实测「类别」列：`position:fixed;left:848.41px;top:407.38px;…;max-height:484.63px`，面板 `120×156 @409..565` 四边都在视口内，
    中心 `elementFromPoint` 命中 `el-checkbox__label`；条件面板 `left:544.91px;…;width:320px`，输入「阿莫」点「应用」8 行→1 行。
  - **tooltip 没显示全 ＝ 层叠上下文，不是裁剪**（同一个浮层族，成因完全不同）。实测浮层矩形**完整**
    （`243..406 × 462..494`，侧栏右缘只有 256 ⇒ 冲出 150px 一个像素没少），逐层 dump 的
    `transform/filter/contain/will-change/backdrop-filter/clip-path` **全是 `none`** ⇒ fixed 不受 ancestor overflow 裁剪。
    真身：`.demo-aside` 是 `position:sticky`，**sticky 无条件创建层叠上下文**，浮层那个 `z-index:2000` 出不了侧栏这一层，
    而正文里 `position:relative` 的 `.el-menu` 与侧栏同处「定位但 z-index:auto」这一步、DOM 更靠后 ⇒ 盖在上面
    （`elementFromPoint(中心)` 读到 `.el-menu-item-group__title`，左缘 13px 仍读到浮层 —— 正是"露出一小条"的截图形状）。
    定罪用**只动一个属性的翻面变异**（`diag-menu-popper-stack.cjs`：仅把 aside 的 `zIndex` 设 60 → 同一坐标命中从正文变回浮层，
    两次矩形逐像素相同）；`/menu` 两条 `flipped:true`、`/tabs` 两条 `flipped:false` —— **变异两侧都要跑**，否则会把"环境恰好没压住"当成机制普适。
    修法是宿主一行（`.demo-aside { z-index: 25 }`，高于正文、低于顶栏 30）；库侧不改是因为 `ElTooltip` **没有 teleport**
    （grep 过 `Teleport/appendTo/PopupService`，零落点），EP 靠 `teleported`+`:append-to` 挂 body 才天然避开 ⇒ 记为**库的已知限制**。
  - **高亮第 1 行「不起作用」＝ 取错了行**：演示按钮原先按数据源下标取 `epRows[0]`，而那张表开了 `DefaultSortProp="Stock"`
    ⇒ 它实际排在第 31 行，在 260px 定高表体外面，点了确实看不见。改成 `VisibleRows[0]`（**显示**的第 1 行）。
    实测 60 行数据下 `.current-row` **恰好 1 个**（EP 同口径），行矩形 `442..482` 落在可见表体 `442..660` 内。
  - **滚轮不滚**：`/table` 两张定高表实测滚轮本来就有效（⑫ `scrollTop 0→112`，`scrollHeight 319 / clientHeight 207`，
    且 `pageScrollY 0→0` 整页没被带着滚；⑨ `0→240`，`2399 / 218`），坏的是**下拉表格 `ElDropDownTableGrow` 的弹层**
    —— 容器用 `Height` 定高会把 199px 的内容摊在 271px 的盒子里留白，改成 **`MaxHeight`**（贴内容、到顶才滚）。
    /grow 弹层实测 `h 290 @517..807`、表体 `scrollHeight 120 == clientHeight 120`、3 行 ⇒ `needsScroll:false`；
    **这条只证"贴内容、在视口内"，滚轮本身的证据是 /table 那两组读数**，文档里按这个口径写着。
  - **顺手抓到两处自己写坏的**：① `@for` 体里输出过标记之后 Razor 已在标记上下文，裸 `if (NeedsValue(condition)) { … }`
    被当正文**整句印进 DOM**（`outerHTML` 实测躺着那句 C#），那段匿名文字成了 flex 容器的第三个 item，
    把右值框从 `182px` 挤到 **`11.375px`** ⇒ 必须写 `@if`；② `ElOption` 只在**弹层被渲染时**才挂载注册
    （`ElOption.razor.cs:42-51` 的 `OnInitialized → RegisterOption`，`ElSelect.razor.cs:829` 注释「下拉第一次打开时
    Options 还是空的」），而 `RegisterOption` 里那条显示文本补算（`:816-826`）只在 `Label` 为空时才生效 ⇒
    受控 `Value` 在点开之前是**空白**的，运算符与关系两颗下拉必须显式给 `Label`。
    两条各有一条 bUnit 用例与一次变异钉着（M1 红在 L292、M2 红在 L295，均 **失败 1 / 通过 13**）。
  - **两条常驻守卫各抓走过一条真红**：`LangResourceGuard`（`el.table.*` 缺键 + 拼接键整族报成 `el:table:operator:`）、
    `TimingGuard`（新用例两处 `WaitForElement` 没写 `timeout:`）。第一次全量读数 **失败 2 / 通过 2358 / 2360、RC=1**
    （`Temp/full_suite_batch5.log`）—— 摘要里曾把它记成"exit 0"，按"证据不是记忆"重读日志才发现。
  - **探针自己的红也要归因**：`verify-batch5.cjs` 第一版 **11 项红 4**、第二版 **14 项红 1**、第三版 **14/14 绿**
    （整份读数落盘 `Temp/verify_batch5_run1.log` / `_run2.log` / `_run3.log`）。四条红里三条是探针自己的前提坏了：七组共用一条
    circuit，而演示站用 `KeepAliveRouteView` 不卸载已访问的页面 ⇒ 上一条留下的「类别」筛选只剩 1 行，后面的滚轮
    （读数就是"该段没有可竖滚的表体容器"）/排序/条件组全失去前提；另一条 A2 是硬编码 `checkboxCount === 2`（演示实际 4 个类别选项）。
    剩下的 T9 `/menu` 浮层是**真红**：那次运行里 aside 的 `z-index: 25` 还没生效，同一脚本 `/tabs` 侧 `hitInside:true`、
    `/menu` 侧 `false` —— 与上面翻面变异的 `flipped:true / false` 完全对得上。改成**每组各自 reload `/table`**、
    断言放宽为 `>=2` 并补「未勾选时『筛选』禁用」，第三版全绿。
    **收尾复跑**（三处变异全部还原之后）：`dotnet build` 演示站 **0 警告 0 错误**（`Temp/build_batch5_final.log`）→
    重启 5199 → 再跑一次 **14/14 PASS、0 console error**（`Temp/verify_batch5_final.log`），且输出与被引用的
    `demo/tools/probes/verify-batch5.log` **逐字节相同**（`diff` 无输出）—— 读数可复现，而不是"最后一次碰巧绿"。
  - **文档同步**：新增 `docs/table-filter-alignment.md`（EP 两态口径、定标算术、条件面板、列 sort 属性完整清单、
    M1/M2/M3 读数、有界清单）；`docs/menu-alignment.md` 加 §10（sticky 层叠上下文的完整取证）；
    `docs/text-ellipsis-tooltip.md` 加 §3.5（fixed 只挡裁剪、不挡层叠）；`docs/testing-guards.md` 加 §4 第 15/16 条，
    探针主目录 183 → **192** 份；`docs/组件使用说明.md` 的 `ElTableColumn` 参数表补 `FilterMultiple` / `AdvancedFilter`
    并给 `SortNo` 标注「列的显示次序，不是数据排序」。
  - **两处引证自查**：① 上面 ② 那条原先写的是「`ElSelect.razor:7` 那道门」——去读那行，它只往箭头图标上加 `is-reverse` 类，
    跟候选项挂载无关；真身在 `ElOption.razor.cs:42-51` 与 `ElSelect.razor.cs:816-829`，文档与这条都已改成实测口径。
    ② 编码守卫扫描范围上一批记的是 1560，恢复会话时的摘要里又写成 1597（demo 576）——两个都不是本批读过的数，
    按"逼 `files.Count` 进失败消息"的手法实测为 **1595**（src 762 / demo 574 / test 152 / docs 94 / 仓库根顶层 13，
    `Temp/scope_probe2.log:14`），注释与 `docs/source-encoding.md`、`docs/testing-guards.md` 同步；
    `SourceEncodingGuard + LangResourceGuard + TimingGuard` 三族过滤集 **16 例 0 失败**（`Temp/guards_batch5.log`）。

- 🟠 **滚条淡入机制的「钉回旧值」写回会盖掉真实滚动，把页面弹回上一个位置（2026-10-08）**。
  触发点不是用户报的，是本批新探针自己**10 次红 3 次**：`verify-ellipsis-tooltip.cjs` 的「上方放不下时翻到下方」
  那条每次读数都是「把 `#eltext-truncated` 滚到视口 top=20 之后，它又回到 798」，也就是**滚完 250ms 内被弹回 0**。
  - **真身**：`window.elementBindFadeScrollbar` 的 `repaint()` 在 pointerover/out 换容器时把 `overflow` 在
    auto↔scroll 之间互换两帧强制 Blink 重建滚条，并在两帧之后把 `scrollTop/scrollLeft` **写回「进入时」读到的旧值**。
    定位手法：① 孤立复现（只藏 chrome + 滚）不红 ⇒ 需要前序动作；② 逐句镜像探针（同样的 hover/`waitForFunction`/
    `addStyleTag` 顺序）5 次红 1 次，采到 `scrollTop` 被**第二次赋值为 0**、而 `scrollHeight` 不变（＝不是内容塌了导致的
    钳位，是有人主动写）；③ 在那个滚动元素实例上 `Object.defineProperty(el,'scrollTop',{set(v){push(stack)}})` 劫持 setter
    打调用栈 ⇒ 直接指向 `wwwroot/js/dom.js:4182`。修完连跑 6 次 `resets=0`。
  - **那条写回是死重**：`diag-fade-writeback.cjs` 把互换照抄一份、只去掉写回，逐帧采样 `.demo-main`（maxTop 2973/5956）、
    `.el-scrollbar__wrap`（204）、`.el-table__body-wrapper`（0）三个宿主，**两轴偏移全是 0**、`midTop` 与进入前一致。
    它是 `overflow:hidden` 年代的遗留（hidden 会丢位置），换成 auto↔scroll 之后两边都可滚、client 尺寸不变，位置不会丢；
    反过来却会盖掉窗口里的任何真实滚动（滚轮、`scrollIntoView`、程序化赋值都是这个形状）。
  - **同一把刀仓库早就记过案，只是当时只豁免了虚拟滚动容器**：`repaint()` 上方 `isVirtualScroller` 那段注释写的
    「跨帧 overflow 切换 + 事后钉回旧值 ⇒ 滚动位置回跳/闪烁」就是这个缺陷，普通滚动容器身上的那份一直没处理。
  - **验证**：新探针 `demo/tools/probes/verify-scrollbar-fade-scroll-race.cjs` **8/8 PASS**。它带**正向对照**
    （窗口内 `computed overflowY==='scroll'` 且 `scrollbarGutter==='stable'`，证明机制真的跑了 —— 否则「位置没被改回去」
    会因为什么都没发生而空转通过）。变异法：把旧写回原样塞回去 → **3 条红**（该竞态、假绿守卫、重叠触发），
    还原后 8/8 绿、文件 `restored identical bytes: True`。滚条淡入原有的视觉回归 `probe-scrollbar-final.cjs`
    的 `.demo-main`「交互后拇指显形 dark=67」照旧绿。
  - 📌 同一次归因顺手记一条**先于本批存在**的红：`probe-scrollbar-final.cjs` 的
    `.demo-aside-scroll 交互后拇指若隐若现显形`（`dark=0 min=255`，12/13）。写回放回/还原各跑一次都是**同一条红、3/3 稳定**，
    且 aside 实测 `scrollHeight 4600 > clientHeight 1344`、`overflowY:auto` —— 与本批改动无关，**没有**顺手改，
    取证与判据见 `docs/testing-guards.md` §4 第 14 条。

- 🔴 **`ElMenu` 的 `ellipsis` 从来没生效过：默认值与 EP 相反、`SyncOptions` 漏字段、子菜单标题是半条功能（2026-10-08）**。
  触发点是用户报「`http://localhost:5199/menu`，ellipsis 没起作用」并附截图（红框在「一个很长很长很长的菜单项用于验证
  ellipsis 截断」上，文字整个冲出菜单右边框）。**截断 CSS 本身一直是好的** —— 手工给那条文字 `span` 加上
  `.el-menu-item__ellipsis-text` 后立刻从 302px 收到 170px、`li` 的 `scrollWidth` 从 351 回到 239；
  坏的是"那个类从来没被输出"。旧构建实测：`span` 的 `class` 是**空串**、`overflow:visible`、`text-overflow:clip`。
  - **① 默认值方向反了**：`ElMenu.Ellipsis` 写死 `= false`，XML 注释还声称「官方默认 false」——
    EP 2.14.0 `menu/src/menu.ts:124` 的 `ellipsis` 默认值是 **`true`**，而且本仓 `docs/menu-alignment.md` §1 的表
    早就写了 `true`：**只有代码和注释是错的，表和文档没同步到代码**。演示页那条菜单也没显式开属性，于是类不输出。
  - **② `SyncOptions()` 漏字段**：`Options` 在 `OnInitialized` 里由 `BuildOptions()` 建好，之后每次 `OnParametersSet`
    只走 `SyncOptions`；那里逐字段复制了 18 个，**唯独漏了 `Ellipsis`** → 渲染后再改这个参数永久停在初始值。
    这类"漏一个字段"没有编译错误、没有运行时异常，只是那条参数从此失灵。
  - **③ 只修菜单项会留下半条功能**：子菜单标题 `.el-sub-menu__title > span` 从不带截断类。给它同样的超长文字后实测
    `li scrollWidth 401 / clientWidth 239`、`span` 宽 358px —— 只截断 `ElMenuItem` 的话，同级子菜单标题照样溢出。
  - **口径差异写进注释与文档（别把本库行为当成 EP 行为）**：EP 的 `ellipsis` 注释是 "available only in horizontal
    mode"（`menu.ts:122`），做的是 `useResizeObserver` + 把溢出项折叠进「更多」弹层（`menu.ts:412`、`menu.ts:508`），
    `menu.scss` 通篇没有 `text-overflow` —— **EP 的竖向菜单本来就会溢出**。本库实现的是文字级 CSS 截断，
    vertical / horizontal 都生效，属本库扩展；`Ellipsis="false"` 可退回旧行为（DOM 与本属性引入前逐字节一致）。
  - **折叠态与弹层没有被顺手改坏（实测）**：EP 的 `.el-menu--collapse ... > span{width:0;height:0;visibility:hidden}`
    优先级高于单个类，加类后折叠 span 仍是 `0×0 / hidden`；弹层是内容宽，子项 `span` 的
    `scrollWidth == clientWidth == 42`，没被裁。横向深色菜单三项 `56/70/56` 全部文字完整。
  - **双侧夹具**：演示页新增「超长文字截断」一节，两条同形状菜单（`max-width:180px`）——
    开：`li scrollWidth 179 == clientWidth 179`、`span 302→139`；`Ellipsis="false"`：`li 322 > 179`、`span 302` 不裁。
    没有右边这条对照，左边所有"被裁"断言都可能只是"永远裁"造成的假绿。
  - **验证**：`ElMenuTests` 9 → **13 例**；`ElNavigationTests` 那条精确串 `<span>Home</span>` 改成带截断类
    （它正是默认值翻转的机器可观测面，**red-before** 就是它先红）。变异法：默认改回 `false` → **失败 3 / 通过 50**；
    删掉 `SyncOptions` 那行 → **失败 1**；撤掉子菜单标题的类 → **失败 1**；三处还原后 53/53。
    新探针 `demo/tools/probes/verify-menu-ellipsis.cjs` **15/15 PASS**、0 个 JS 异常（**red-before**：旧构建第 ①②③ 条全红）。
    全量 bUnit **2301 通过 / 0 失败 / 0 跳过**（基线 2297 ＋4）；编码守卫扫描范围 1558 → **1559**
    （src 755 / demo 552 / test 148 / docs 91 / 仓库根顶层 13），`bad=0`；细节见 `docs/menu-alignment.md` §8。
  - 🟡 一条工具教训：用 `shutil.copy` + `shutil.move` 还原 `.bak` 会**保留原 mtime**，MSBuild 据此判定"没变更"，
    于是"还原后"的那次测试跑的是变异体的旧 DLL（本批实测还原后仍报 `失败 1`，`touch` 之后才 53/53）。
    `docs/testing-guards.md` §2 那条「改完先 touch 再 build/test」因此补上了这个前提：**还原文件也算改动**，
    变异法的完整闭环是"改 → 跑 → 还原 → **touch** → 再跑一遍确认绿"。

- 🔴 **`ElTabs`：点 X 不删页签、`before-leave` 形同虚设、页签文字没法对齐（2026-10-08）**。触发点是用户报
  「`http://localhost:5199/tabs` 里面的文字应该有属性可以设置居中 居左 居右；点击 X 的时候没有真正移除；
  不打对勾一样可以切换到页签二，没有真正拦截」并附三张截图。三件事全部先在浏览器里复现成立，最后共定位到 **5 件事**
  （两件顺带抓出）。EP 侧口径来自 2.14.0 源码直取（`tabs/src/tabs.tsx`、`tab-nav.tsx`、`tab-pane.ts`）。
  - **① 硬编码 `<ElTabPane>` 点 X 只发事件不删除**：`ResetActiveTab` 第一行 `if (DataSource == null) return null;`。
    修前实测卡片段 3 个页签，点第 2 个的 X 之后仍是 3 个，只有日志变成 `edit: remove c2`。
    EP 的 `tabs.tsx:182` `handleTabRemove` 本来就只发 `edit(name,'remove')` + `tab-remove(name)`、由使用方 splice 数组，
    但 Blazor 里写死的子标记使用方删不掉（父组件一重渲染就又出来），所以分两种模式：`DataSource` 模式沿用"删数据"，
    硬编码模式由组件把页签摘出 `tabPanels` 并记名进 `closedTabNames`（`ElTabPane` 命中即整项不渲染、`AddTab` 拒绝再登记）。
    关掉当前激活页签时补位到邻居，且**刻意不再过 `before-leave`** —— 被关的页签已经不存在，拦下来只会让
    `ActiveTab` 停在一个已摘掉的页签上、内容区空白。
  - **② `ElTabs.IsRemovable` 漏标 `[Parameter]`**：只有别名 `Closable` 带标注，而演示页写的正是 `IsRemovable="true"`
    → 赋值根本不发生。实测 3 个页签只有 1 个 `.el-icon-close`（显式 `IsClosable` 的那个）；同族的
    `IsAddable` / `IsEditable` / `IsStretch` 都有标注，是漏的。补上后实测每个页签各 1 个图标。
  - **③ `before-leave` 演示页参数序反了**：组件传的是 `BeforeLeave(tab.Name, oldName)`，与 EP `tabs.tsx:147`
    的 `beforeLeave(value, currentName.value)` 一致（**第一个参数才是要切去的页签**），演示页却写成
    `(from, to)` 并拿 `to` 判据 → `to != "g2"` 恒真、永远放行。实测：不勾选点「页签二」真的切过去了。
    组件侧无错，修演示＋用 bUnit/探针把参数序钉住。
  - **④ 初始自动激活不该走 `before-leave`（bUnit 顺带抓出）**：回调被调两次 `[("p1", null), ("p2", "p1")]`。
    EP 的 `currentName` 是直接初始化的、不查 `beforeLeave`。危害不是多调一次，而是**一个"没有当前页签就不放行"
    的回调会让整条页签栏一个激活项都没有、内容区空白**。现在 `ActiveTab == null` 视为初始化，跳过
    `BeforeLeave` / `BeforeLeaveSync`。另记一个 Blazor 特有的坑：**拦截成功时组件什么都不改、也就不会重渲染使用方那一层**，
    日志看着像回调没跑（实测仍停在 `(未触发)`）——回调里自己 `StateHasChanged()` 才会刷新。
  - **⑤ 新增 `TabTextAlign`（本库扩展：EP `tabsProps` 里没有任何对齐属性）**：`TabTextAlign?`，
    `Left` / `Center` / `Right`，**默认 null＝零 DOM 变化**。落地踩到四条量出来的 CSS 事实：
    `.el-tabs__nav` 被后面的规则改成 `display:flex` 但 `float:left` 没人复位 → 栏宽收缩到内容宽
    （1034px 容器里只有 220px，没有剩余空间可分配），故横向时补 `min-width:100%`；`.el-tabs__item` computed 是 `flex`
    （源码 `inline-flex` 被块化）→ **`text-align` 对它无效**，对齐只能走 `justify-content`；纵向时 nav 是
    `flex-direction:column` → `justify-content` 会变成上下分布、`min-width` 会撑破栏宽，所以纵向只给页签项；
    EP 自己 `.el-tabs--top .el-tabs__item.is-top:nth-child(2){padding-left:0}`（`active-bar` 占了第 1 个子元素位）
    → 首项文字贴项左沿是设计值，探针按"指示条盖住文字矩形"判。
  - 新增 `ElTabsTests` **12 例**（9 → 21）：关激活／关非激活、重渲染不回弹、`edit`+`OnTabClose` 仍发、
    `IsRemovable` 级联出图标、对齐输出（含"不设时不输出"的负向断言 + `transform: translateX(0px)` 正向对照、
    纵向只给项不给栏）、`before-leave` 参数序与拦下／放行两态、初始化不参与。**能抓缺陷是变异法验的**：
    把三处修复分别改回旧行为 → **8 例红**，恢复后全绿。
  - 新增探针 **`demo/tools/probes/verify-tabs-text-align-close-beforeleave.cjs`** 18 项：三件事各配正向对照
    （勾选后必须真切换、默认段栏宽必须仍是收缩的 220/1034、默认段 bar 同样盖住文字）。
    **red-before**：旧构建实测 3→3 不删、不勾选也能切、`justify-content:normal`；修后 **18/18**，0 个 JS 异常。
  - 🟡 顺带纠正一处**文档谎报**：演示页原话「`TabClass` 是本轮新增的 EP 标准属性」不成立 ——
    EP 2.14.0 的 `tabsProps` 只有 type / closable / addable / modelValue / defaultValue / editable / tabPosition /
    beforeLeave / stretch / tabindex，`tab-pane.ts` 只有 label / name / closable / disabled / lazy。已改标为本库扩展。
  - **验证**：全量 bUnit **2297 通过 / 0 失败 / 0 跳过**（基线 2285 ＋12）；编码守卫扫描范围 1556 → **1558**
    （src 755 / demo 551 / test 148 / docs 91 / 仓库根顶层 13），`bad=0`；细节见 `docs/tabs-alignment.md` §8。
- 🔴 **`ElMenu` 深色主题悬浮时"白底白字"：一个写死的 `HoverColor` 默认值压过了主题类（2026-10-08）**。触发点是用户报「`http://localhost:5199/menu` 看不到下面的文字了」并附截图（红框在「我的工作台」上）—— 悬浮那条菜单标题时文字整个消失。根因不在主题类，在一行自基线提交 `157dde6` 就在的默认值（**不是本仓任何一批引入的回归**）：
  `public string HoverColor { get; set; } = "#ecf5ff";` 且无条件内联输出 `--el-menu-hover-bg-color:#ecf5ff`，
  内联压过 `.el-menu--dark` 的 `#343842`，而悬浮文字色是 `#fff` → 近白底配白字。**实测旧态**：
  `background rgb(236,245,255)` / `color rgb(255,255,255)` → 对比度 **1.1:1**（WCAG 正文要求 4.5:1）。
  - **EP 2.14.0 的口径（源码直取）**：`menu/src/use-menu-color.ts` ＝ `hover-bg-color = backgroundColor ? TinyColor(bg).shade(20) : ''`；
    `use-menu-css-var.ts` ＝ `hover-text-color` 跟随 `text-color`，且四个变量各自只在给了值时才输出（不给就留给类级默认）。
    本库照此实现：显式 `HoverColor` 优先 → 否则有 `BackgroundColor` 时输出
    `color-mix(in srgb, {bg} 80%, var(--el-color-black))` → 给了 `TextColor` 时同时输出 `--el-menu-hover-text-color` → 两者都没给则**一条 hover 变量都不输出**。
  - **`shade(20)` 的真实语义值得记**：`@ctrl/tinycolor` v4 的 `shade(n)` 是 `mix('black', n)`（线性通道混合），**不是** HSL 亮度减法，
    所以 `#545c64`(84,92,100) → ×0.8 → (67.2,73.6,80) → `#434a50`。`color-mix(in srgb, … 80%, black)` 与之等价，
    实测内核 computed 值是 `color(srgb 0.263529 0.288627 0.313726)`（分量 ×255 正是 67.2/73.6/80），修后悬浮对比度 **9:1**。
  - 新增 `ElMenuTests` **4 例**（配色 CSS 变量口径区）：深色＋自定义配色派生出 `color-mix` 与 hover 文字色、
    一条配色都不给时内联不得出现任何 hover 变量（负向断言配 `--el-menu-level:0` 正向对照）、显式 `HoverColor` 压过派生值、
    只给 `TextColor` 时只跟文字色。**red-before**：把默认值改回 `#ecf5ff` → **失败 3 / 通过 37**。
  - 新增探针 **`demo/tools/probes/verify-menu-dark-hover.cjs`** 13 项（bUnit 只能断言内联写了什么，
    "算出什么颜色、跟文字撞不撞"是渲染层的事）：常态白字/激活项黄字可读、悬浮底色＝EP 的 (67,74,80)、悬浮文字仍白、
    悬浮对比度 ≥4.5、**对照组把旧缺陷写死**（白字配 `#ecf5ff` 实测 1.1:1，证明这探针抓得住）、内联不再出现 `#ecf5ff`、
    弹层项可读、浅色菜单（没给配色）仍走类级默认。**red-before**：旧构建 **9/13**（悬浮那项实测 `ratio:1.1`）；修后 **13/13**，连跑两次一致。
  - 🟡 **探针写出来踩了三个"读数会假绿/假红"的坑（都记进探针注释）**：① `.el-sub-menu__title` 带 `background-color` 过渡，
    悬浮后立刻量会拿到**过渡中间值**，内核把它序列化成 `oklab(0.404… / 0.989)` → 必须先注入 `transition:none`；
    ② `color-mix` 的**终值**也不是 `rgb(...)` 而是 `color(srgb 0.2635 0.2886 0.3137)`，解析只认 `rgb()` 就会拿到 `null`、
    断言空转成"看起来通过" → 现在解析不出颜色时 `contrast()` 返回 -1 让断言红得明白；
    ③ 弹层项自身 `background-color: transparent`，白底在外层 `.el-popper.is-light` 上 → 对比度必须按**祖先链合成**后的有效底色算，
    直接读自身会得到 1.61:1 的假 FAIL。
  - 🟡 **一条不属于本批的观测**：EP **自己的**浅色默认悬浮是 primary 字（`rgb(64,158,255)`）压 primary-light-9 底
    （`rgb(236,245,255)`）＝ **2.53:1**，未达 WCAG AA。这是 EP 既有设计口径，本批按"不改变默认观感"处理，探针只要求 >1.5 不撞色并打出真实读数。
  - **明确没做**：弹层配色仍由 `PopperEffect` 的 `.el-menu--popup-container.el-popper.is-light/.is-dark` 那组规则决定，
    没有像 EP `sub-menu.ts:217` 那样把同一份 cssVar 打在子菜单 `<ul>` 上（改了会同时动到弹层的圆角/阴影/箭头口径）——
    实测深条 + 白底弹层是 13.02:1 可读，差异记在 `docs/menu-alignment.md` §6。
  - **验证**：全量 bUnit **2285 通过 / 0 失败 / 0 跳过**（基线 2281 ＋4）；编码守卫口径随新探针 `.cjs` 入扫
    1555 → **1556**（src 754／demo 550／test 148／docs 91／仓库根顶层 13），`bad=0`。
    全过程与三处口径写在 **`docs/menu-alignment.md` §7**，`docs/testing-guards.md`、`docs/lang-i18n.md`（基线与轨迹）、
    `docs/table-edit-issues.md`、`docs/source-encoding.md` §4 同步。

- 🔴 **全仓文本文件清成 UTF-8：30 个 GBK 存盘的文件、12 个被写成 `?` 的汉字、9 行已塌成 U+FFFD 的注释（2026-10-08）**。触发点是用户指出 `src/Components/ControlRenders/TableRender.cs` 是 GBK 存盘 —— 一查不是个例：**`git ls-files` 1489 个跟踪文件里，剔掉 97 个二进制后 1392 个文本文件有 31 个不能用严格 UTF-8 解码**，而且它们全都"编译 0 错误、运行也正常"。
  - **为什么能潜伏这么久（这条是本批真正的收获）**：Roslyn 读**无 BOM** 源文件时先按 UTF-8 解，解不动就**退回系统 ANSI 代码页**（本机 ACP=936/GBK）再解一遍。所以 GBK 存盘的中文与字符串字面量在中文 Windows 上完全正常 —— 实测重新构建后的 `Element.dll` 里是正确 UTF-16 字面量（`确认删除吗` 命中 1 次、`要置为 Loading 状态的组件尚未渲染完成` 命中 2 次）。坏只坏在换代码页的机器、Git 托管站网页、以及任何按 UTF-8 读文件的工具（grep / 编辑器 / `DocumentationFile` 生成的注释 XML）。**"编译过了"在这一层不是验收依据**，与浮层归属那两条同属静默缺陷。
  - 损坏分**三类**，只有前两类可无损救回：**A 纯 GBK 存盘 30 个**（`src/Components` 15、`src/Markdown` 14、`test/TabTest.cs`）→ `bytes.decode('gbk')` 后按 UTF-8（无 BOM）写回，转换前逐份断言 `text.encode('gbk') == 原字节`，不等就中止；**B UTF-8 但汉字第三字节被写成 `?`（0x3F）1 个文件 12 个字**（`src/Components/Element.xml`）→ 按 UTF-8 序列补回（`始`×3 / `交` / `值`×3 / `身` / `素` / `态`×2 / `纹`）；**C UTF-8 里嵌 U+FFFD 3 个代码文件 9 行**（`PopupOption.cs` 6、`ElDynamicComponent.cs` 2、`Extension.cs` 1）→ 字符已经丢了，转换救不回来，只能照上下文重写。
  - C 类的成因值得记：**拿编辑工具直接改一份 GBK 文件**，工具按 UTF-8 读、把非法字节替换成 U+FFFD 再写回，中文就永久丢了。所以规则是"先整份转 UTF-8，再改内容"（写进 `docs/testing-guards.md` §5 检查单）。上一轮记的"`Extension.cs` 是 GBK、必须按字节补"当时就已过期：它那时是合法 UTF-8 ＋ 3 行 U+FFFD。
  - 补字时踩到一次**靠短语通顺猜字**的错误：`弹出层元素本?` 我先补成"本体"，但坏字节是 `E8 BA 3F`，码点区间 U+8E80–U+8EBF，`体`(U+4F53) 不在其中，正确字是 **身**(U+8EAB)。判据是"补完整份文件必须严格 UTF-8 通过"，`PopupOption.cs` 的那行注释也据此从"本体"改回"本身"。
  - 新增第七常驻守卫 `test/Element.ComponentTests/SourceEncodingGuardTests.cs` **4 例**：① `src/ demo/ test/ docs/` 下全部文本文件必须被 `new UTF8Encoding(false, true)` 解出来；② `src/**/*.cs`、`*.razor` 不得含 `EF BF BD`（U+FFFD 本身是合法 UTF-8，①查不到，必须单开一条）；③ **双侧夹具** —— "确认"的 GBK 字节 `{BF C6 C8 CF}` 必须抛 `DecoderFallbackException`、同词 UTF-8 必须解出，并断言**宽松解码不抛只产生替换符**（这就是 §1 那条静默的机器版本）；④ 覆盖断言 ≥700（实测 **1555**：src 754／demo 549／test 148／docs 91／仓库根顶层 13）且必须包含 `TableRender.cs`、`Extension.cs`、守卫自己、以及根层的 `CHANGELOG.md`（根层那条通道要真的通）。**red-before**：临时放两份夹具（一份 GBK、一份含 U+FFFD）→ **失败 2**，报错带文件与字节偏移（`_TempGbkProbe.cs @byte 65：Unable to translate bytes [C8] at index 65…`），撤掉即复原。
  - 🟡 **顺带抓到一条属于"扫描类守卫自己"的缺陷（工具坏了和仓库坏了长得一样红）**：把仓库根顶层纳入扫描范围后，两条用例**每次必红**，报的却是
    `System.IO.IOException : The process cannot access the file … because it is being used by another process`。
    真身是 `File.OpenRead` 的共享模式 = `FileShare.Read`（＝"别人也只许读"），于是任何一个正被别的进程**开着写**的工作树文件
    （本仓常态：探针写 `demo/tools/probes/*-result.txt`、脚本开着输出文件）都会让它抛异常 —— 不是偶发，是确定性：同一场景连跑 **6 次 6 次 `失败数: 2`**。
    改法是按只读校验器真正的意图开文件（`FileShare.ReadWrite`，测试里收成 `OpenShared` / `ReadAllBytesShared` 两个助手），
    同一场景改后连跑 **6 次全绿**。这条写成 `docs/testing-guards.md` §1 配方的**第 5 步**，并记在 `docs/source-encoding.md` §4.1：
    **先分异常类型再动判据** —— `IOException`＝工具坏了，`DecoderFallbackException`/断言失败＝仓库真坏了。
  - **验证**：全量 bUnit **2281 通过 / 0 失败 / 0 跳过**（上一基线 2277，+4 条守卫；收尾连跑三次读数一致），库重新构建 RC 0，运行时字符串按上表在 DLL 里复核。收尾复扫：跟踪文件 **0 个**非严格 UTF-8，`src` 代码文件 **0 个**含 U+FFFD。
  - 🟡 **顺手抓到一条测试层的假绿**：`dotnet test --no-build --filter <匹配不到用例>` **返回 0**，而 `--no-build` 用的是上一次成功构建的 DLL —— 本批出现过"编译其实失败（error CS1061）但测试 RC=0"。判绿必须读"通过/失败/总计"那一行并核对总数等于基线，不能只看退出码（写进 `docs/testing-guards.md` §2）。
  - 🗑 **两份历史遗留件按用户指示处置（删除）**：`src/Components/Element.xml` —— 它不是构建产物（`Element.csproj:38` 的 `DocumentationFile` 指向 `obj\Debug\net10.0\Element.xml`，实测 1 045 159 字节），而是基线提交 `157dde6` 带进来的一份 4295 字节／25 个 `<member>` 的**过期注释快照**（`BFormBase.Validations`、`Select.OptionModel`1` 这些成员在当前源码里已不存在，留着只会给出错的注释），本批先把 12 个第三字节被写成 `?` 的汉字补正、再整份删除；`test/Element.ComponentTests/baseline.log`（225 KB、2006 处替换符）是"GBK 控制台输出被按 UTF-8 抓下来"的日志快照，源头字符已经塌成 `?`/U+FFFD、不可逆，内容随时能由 `dotnet test` 重新产生。两份的原件都在基线提交里：`git show 157dde6:src/Components/Element.xml`、`git show 157dde6:test/Element.ComponentTests/baseline.log`。删除后守卫口径 src 755 → **754**（总数 1555）。
  - **明确没做**（`docs/source-encoding.md` §6 逐条）：没重新生成删掉的 `Element.xml` 注释快照（它已与源码脱节）；没统一 BOM 口径（转换统一写无 BOM，依据是既有分布 `src` 的 734 个 `.cs`/`.razor` 里 111 带 BOM／623 不带，守卫不查 BOM）；没动 vendor 的 `wwwroot/js/ofd/ofd.umd.min.js`（1 处 U+FFFD，属第三方压缩产物）；没碰 `.gitattributes`/`core.autocrlf`（改 git 配置不在本批授权范围）。
  - 全过程、三类判据与数字集中写在新文档 **`docs/source-encoding.md`**；`docs/testing-guards.md`（§1 第七守卫、§2 假绿口径、§4 第 10 条取证顺序、§5 检查单）、`docs/getting-started.md`、`docs/lang-i18n.md`（基线与轨迹）、`docs/table-edit-issues.md` 同步。

- 🔴 **`LoadingService` 不再注册成 Singleton，并把"浮层宿主只能挂一个"变成可观测契约（2026-10-08）**。上一条（`DialogService.Dialogs` 去 static）留的那句"相邻但没改的一处 —— `LoadingService` 注册的是 `AddSingleton`…留作单独一批"就是这一条；这一批把它改掉，并且**挖到了真实复现**：缺陷不只存在于"将来可能的 Blazor Server"，演示站自己就在违反契约。
  - 共同机制：`ElPopup.OnInitialized`（`src/Components/ElPopup.razor.cs:184-247`）对**每一类**浮层队列做的都是同一个三步「解订阅自己的处理器 → 把集合整个换成新实例 → 再订阅」，共 10 个集合 + 2 个事件 —— 也就是说**"宿主初始化"这个动作本身等于夺取队列所有权**。于是三条必须同时成立：① 持队列的服务 `AddScoped`；② 队列是实例成员不是 `static`；③ 一个作用域内只有一个宿主。破掉任何一条，前一个宿主就守着没人写入的旧集合，症状全是**静默**的（不抛异常、0 个 JS 错误，只是"调用了却什么都不出现"或"遮罩关不掉"）。
  - **真实事故**：`demo/.../Layout/LoginLayout.razor` 里挂着第二个 `<ElPopup />`。站内访问过 `/login`（`Login.razor` 带 `@layout LoginLayout`）之后队列所有权交给它，离开时 `LayoutView` 换回 `MainLayout`、那棵宿主被拆掉，而 `App.razor` 的宿主在 Router 之外不会再初始化、`ElPopup.Dispose` 也不退订 → 接管变成**永久**：**此后整站的整屏 Loading 与 MessageBox 全部不再出现**（实测 `inDocument:0`），控制台 0 个 JS 异常（当初先怀疑的是 CSS 和 z-index，方向完全错）。
  - 修法分四层：注册生命周期（`AddSingleton<LoadingService>` → `AddScoped`，理由写成注释钉在改动下一行）、队列的契约（`LoadingService.LoadingOptions` 的 XML 文档写明"必须实例成员 + 按作用域注册"）、运行时可观测（`PopupService.PopupHostCount` 计数，第 2 个宿主初始化时打一条带文档指向的告警 —— 计数放在 Scoped 服务上，不会跨作用域互相干扰）、演示站自身契约（删掉 `LoginLayout` 里那个宿主并留注释）。
  - 新增常驻守卫 `PopupHostScopeTests` **5 例**（`docs/testing-guards.md` §1 的第 **6** 条守卫）：7 个持队列服务必须全 `Scoped`；**同一个 `ServiceProvider` + 两个 `CreateScope()`** 断言服务与 `LoadingOptions` 都 `NotSame`，并带正向对照 `Assert.Same`（否则那条 `NotSame` 可能只是"每次都新建"，没证明"每连接一份、连接内共享"）；第二个宿主必须被数出来；挂载点扫描器的双侧夹具（注释 / `<code>` 里的 `<ElPopup />` 不算，真挂载必须算 —— 没这条，扫描器写坏就"什么都不匹配"→ 假绿）；`demo/**/*.razor` 里宿主只能出现在 `App.razor`。**red-before**：临时改回 `AddSingleton` → `失败 2 / 通过 3`。
  - 新增浏览器探针 `demo/tools/probes/verify-popup-host-single-mount.cjs`：修复前 **6/9 PASS（3 条 FAIL，读数 `inDocument:0 / visible:0 / host:null`）**，修复后 **9/9 PASS**，全程 0 JS 异常。踩到的四个坑（都写进脚本注释与 §3.11）：① `page.goto` 是整页重载＝新 DI 作用域，两个宿主永不共存 → 假 PASS，必须注入 `<a>` 点击做**站内跳转**并 `waitForURL` 确认真的换了页；② `position:fixed` 的元素 `offsetParent` 恒为 `null`，拿它判可见会假 FAIL；③ 服务式与组件式遮罩必须按**祖先**（`.el-loading-parent`）分开数，混数会把"没生成"读成"生成了但不可见"；④ "能关掉"这类负向断言在缺陷态下**空转 PASS**，必须与正向断言成对。
  - **验证**：四类弹层用例（`ElFeedbackTests|LangOverlayTextTests|PopupHostIsolationTests|PopupHostScopeTests`）定向连跑 **20 次 0 红**（每次 23 例），全量 **2277 通过 / 0 失败 / 0 跳过**（上一基线 2272，+5 新守卫；并发连跑一致）。回归 `verify-feedback-messagebox` **39/39**、`verify-dialogdata` **19/19**、`verify-lang-i18n` **21/21**、`verify-lang-picker-family` **34/34**（跑探针前杀掉 5199 旧实例、重新 build 并重启 devserver，判据看 `_framework/*.wasm` 时间戳）。
  - ⚠️ **宿主侧唯一可能的破坏性变化**：把 `LoadingService` 注入进**自己的 Singleton 服务**的宿主，在 Blazor Server 上现在会抛 `Cannot consume scoped service ... from singleton ...`（WASM 整站一个作用域，行为不变）。那种写法本身就在跨连接共享可变队列，正是要修的形状；本仓无此类消费点。另外库里拦不住宿主在 `AddElementServices()` 之后再自行 `AddSingleton<LoadingService>()`（DI 以最后一条为准），守卫只看库的注册项。
  - **明确没做**（`docs/popup-host-isolation.md` §6 逐条说明）：`ElPopup.Dispose()` 仍不退订集合处理器（只 dispose 了词条订阅）；没有"合法多宿主共存"的设计（要做需宿主注册表 / portal，是设计改动不是补丁）；服务式遮罩仍缺 EP 的 `is-fullscreen` 类（属另一条对齐项）。
  - 机制、事故链路、判据与未做的事集中写在新文档 **`docs/popup-host-isolation.md`**；`docs/table-edit-issues.md`（第 5 层那段"留作单独一批"已改成 ✅ 已修）、`docs/testing-guards.md`（§1 第六守卫、§3.11 探针口径、§4 第 9 条取证顺序）、`docs/getting-started.md` 同步。

- 🔴 **`DialogService.Dialogs` 不再是 `static`：弹层队列按 DI 作用域存放（2026-10-08）**。这一条同时是两件事的根因：① 那条被记了很久的"弹层类并行才红"的老 flaky 的第 5 层（上一轮只做到"锁进同一个 `[Collection("ElPopupHost")]`、根因标注未定位"）；② 一个对宿主有实际影响的产品缺陷 —— **Blazor Server 上后打开的 circuit 会把前一个 circuit 的待显示对话框队列换走**。
  - 机制：`ElPopup.OnInitialized` 对每一类弹层集合做的都是同一个三步「解订阅自己的处理器 → **把集合整个换成新实例** → 再订阅」。其它集合（`MessageService.Messages` / `NotificationService.Notifications` / `LoadingService.LoadingOptions` / `PopupService.*OptionsCore`）都是 `AddScoped` 服务上的实例成员，各作用域互不相干；唯独 `DialogService.Dialogs` 声明成了 `internal static`。于是宿主 B 初始化后，宿主 A 的处理器挂在一个再也没人写入的旧集合上，A 调 `MessageBox.PromptAsync` 写进的是 B 的队列 → **A 的弹层永远不出现**，症状与那条 flaky 的第一层一模一样（结果 Task 不完成、`FindAll` 计数为 0），所以先前被误判成"并行争抢抖动 / 超时不够长"。
  - 修法只有一行可见改动：去掉 `static`（`AddScoped<DialogService>` 本来就是按作用域注册的，队列本该跟着作用域走）。三处读写（`DialogService.ShowDialog` / `CloseDialogAsync` / `MessageBox.ShowAsync`）原本就通过注入的 `DialogService` 实例访问，调用点一行没动。
  - 新增 `PopupHostIsolationTests` 把隔离契约钉住：两个 `BunitContext` 各起一个 `ElPopup` 宿主，再让 A 弹 MessageBox —— **修复前 20s 等待超时红，修复后 314ms 绿**；同一条用例反向断言 A 的弹层不许漏进 B 的宿主。
  - **并行锁已解除**：`ElFeedbackTests` 与 `LangOverlayTextTests` 恢复并行（两个类上的 `[Collection]` 与"干扰源尚未定位"的注释一并换成真身说明）。稳定性口径：两类 + 新用例定向连跑 **20 次 0 红**（每次 18 例），全量并发连跑 **3 次 2272 通过 / 0 失败**（上一基线 2271，+1 隔离用例）。
  - **浏览器侧无退红（重新 build 演示站 + 重启 5199 后实跑）**：`verify-feedback-messagebox` **39/39**（Alert / Confirm / Prompt 出框、输入校验拦下不关、× 与遮罩关闭的返回值区分 —— 全走被改动的这条对话框队列）、`verify-dialogdata` **19/19**（`ShowDialogAsync` 的开框/关框）、`verify-lang-i18n` **21/21**、`verify-lang-picker-family` **34/34**、`verify-table-edit-typing` **7/7**、`diag-typing-cost3` 表格单元格 ≈19ms/字（表格外 ≈13ms/字，与上一轮记的 23/11 同级噪声）、`diag-dt-table-probe` 五步全对，全程 0 JS 异常。这一列**只证"没改坏"**：演示站是 WebAssembly、整站一条 circuit＝一个 DI 作用域＝只有一个 `ElPopup` 宿主，缺陷需要第二个宿主才现形，正向证据是上面那条双 `BunitContext` 用例。明细与口径见 `docs/testing-guards.md` §3.10。
  - ✅ **相邻的那一处已在同一天下一批修掉**：`LoadingService` 当时注册的是 `AddSingleton`，而 `ElPopup` 同样会在初始化时替换它那份 `LoadingOptions` —— 多 circuit / 多宿主下同样会互相清掉遮罩。当时记的"本仓没有已知复现、且改 Singleton 生命周期会牵动 `ElLoading` 指令那条链"两处都被推翻：**复现就是演示站自己**（`LoginLayout.razor` 挂了第二个宿主），而且没有消费者受影响。见本篇顶部第一条与 `docs/popup-host-isolation.md`。
  - 排查过程与判据写进 `docs/table-edit-issues.md`（那条 flaky 的五层清单第 5 层已补全）和 `docs/testing-guards.md` §4 第 8 条：**"单跑必绿、并行才红"先枚举跨作用域的 `static` 可变状态，别先怀疑时间预算，更不要先加并行锁把它按住** —— 加了锁就把真缺陷藏起来了。


### Added

- 🟢 **表头筛选面板补齐 EP 两态，并新增可开关的条件式筛选 `ElTableColumn.AdvancedFilter`（2026-10-09，混合口径）**。
  触发点是用户报「`/table` 筛选点不开，这个最好有属性开关，最好类似这种，用 element 渲染」+ 一张
  DevExtreme 风格的「运算符 + 值」条件弹窗截图。**EP 侧先取证再动手**（tag `2.14.0`，HTTP 200）：
  - `table/src/filter-panel.vue:202-207` 的 `multiple` 取 `column.filterMultiple`，默认值在
    `table-column/defaults.ts:226-229` 是 **`true`**；`:21-55` 是勾选组 +「筛选 / 重置」，`:56-75` 是
    `role="radiogroup"` 的单选列表、**首项文案是 `clearFilter`**；`:214-222` 的 `handleConfirm/handleReset`
    才 `confirmFilter(...)` 提交给 store，`:223-232` 的单选点一项即生效并 `hidden()`。
    ⇒ **勾选只改草稿是 EP 的设计**，演示里因此把「草稿期行数」也量出来（勾选后仍 40 行，点「筛选」才 5 行）。
  - 新增两个列参数：`FilterMultiple`（**EP 同名属性**，默认 `true`）与 `AdvancedFilter`（**本库扩展**，默认 `false`）。
    条件面板全程用 Element 组件渲染（运算符 `ElSelect` + 右值 `ElInput`，第二条上方多一颗且/或 `ElSelect`，
    底部「应用 / 清除」两颗 `button`），最多两条、`Relation` 左折叠、运算符候选按**列属性类型**给
    （文本 8 条 / `IComparable` 8 条），右值永远按字符串录入、比较时才换算类型，换算不动退回字符串比较**不抛**。
  - 词条 **19 条**（zh/en 两份同时加，拍平后各 **117** 条）：`el.table.confirmFilter` 筛选/Confirm、
    `.resetFilter` 重置/Reset、`.clearFilter` **全部/All**（值取自 `locale/lang/zh-cn.ts:152-154`、`en.ts:155-157`，
    第一版按直觉写成「清除」，实读源码后改回）、`.applyFilter` / `.clearAdvancedFilter` / `.filterRelationAnd|Or`（本库扩展键）
    + `el.table.operator.*` 12 条。**踩回 `LangResourceGuard` 的老坑**：`Lang.T("el.table.operator." + 名称)` 它只看到前缀，
    整族被报成一条 `el:table:operator:` 缺键 ⇒ 枚举族取词一律展开成完整字面量（`LabelOf` 的 `switch`）。
  - **口径差异记账**：EP 有 `sort-orders` / `sort-method` / `sort-by` / `sortable="custom"` / `filter-placement` /
    `filter-class-name` 与面板的 `role="dialog"` + 方向键导航，本仓**都没有**；条件式面板 EP 根本没有形态，
    `FilterChange` 在条件路径上抛的是各生效条件的右值串（运算符仍以面板状态为准），属本库口径。
  - **验证**：`ElTableFilterPanelTests` **14 例**（两态、草稿→提交、重置、单选「全部」、条件两条规则/清除/且或链、
    `ClearFilterAsync` 双清、定标算术两侧夹具）；新探针 `demo/tools/probes/verify-batch5.cjs` **14/14 PASS**、0 console error。
    全量 bUnit **2361 / 0 失败 / 0 跳过**（基线 2341 ＋20）。三处语义变异（M1 `@if`→裸 `if`、M2 删 `ElSelect` 的 `Label=`、
    M3 `position:fixed`→`absolute`）**各自 失败 1 / 通过 13、RC=1**，还原后回到 14/14 绿。口径见 `docs/table-filter-alignment.md`。

- 🟢 **截断文字悬停浮出 `elTooltip` 看全文，从 `ElMenu` 扩到 `ElText` / `ElTable` / `ElTableV2` / `*TableGrow`（2026-10-08，混合口径）**。
  触发点是用户报「`http://localhost:5199/basic`，鼠标移动到有 Ellipsis 的时，能不能用 elTooltip 完整显示一下内容，
  包括 elable elablev2 和 eltablegrow 有 Ellipsis 的时,也这样处理」。**四个宿主的 EP 口径不一样，所以开关默认值也不一样**
  （源码直取，HTTP 200，tag `2.14.0` 无 `v` 前缀）：
  - `ElText`：EP `text/src/text.vue:42-71` 用 `useResizeObserver` 量 `scrollWidth>clientWidth`（开 `line-clamp` 改量高度），
    真的被裁才 `setAttribute('title', …)` —— **EP 没有 tooltip**。本库新增 `ElText.EllipsisTooltip`，默认 **`true`**（扩展），
    `="false"` 退回 EP 原生 `title`（`window.elementSetEllipsisTitle`，`dom.js:803`）。
  - `ElTable`：`show-overflow-tooltip` 是**真契约**（`table-column/render-helper.ts:193-200`：加 `el-tooltip` 类 +
    内联 `width:(realWidth||width)-1px`；`table.scss:197-199`：那个类把 `white-space:normal` 换成 `nowrap`），
    但本仓**两条都没实现**，于是 `.cell` 永远换行显示全、「被裁」这件事根本不成立。本批补齐类与定宽，
    开关默认 **`false`**（同 EP）。`*TableGrow` 的列定义原样透传，不新增第三条开关。
  - `ElTableV2`：EP `components/cell.tsx:14` **只给原生 `title`**，table-v2 连 `show-overflow-tooltip` 都没有 ⇒
    `ElTableV2.EllipsisTooltip` / `TableV2Column.EllipsisTooltip` 是纯扩展，默认 **`false`**，列级覆盖表级。
  - `ElLink` 与标题族：EP 的 link/heading 里没有任何截断浮层机制，**本批不动**（没有可对齐的面）。
  - **新共用件 `ElOverflowTooltip`**（`ElText`/`ElTable`/`ElTableV2` 都走它），测算逻辑从 `MenuEllipsisTooltip` 改名成
    `TextClip`（`src/Components/TextClip.cs`）。三个参数形状是被实测逼出来的：`ClipStyle` 不叫 `Style`
    （基类已有同名参数，会撞「参数名大小写不敏感且必须唯一」→ 运行时 `InvalidOperationException`）、
    `Trigger="Manual"`+`ModelValue`（可见性只有一个来源）、`ContentOnlyWhenDisabled`（关掉时连被裁的盒子都不输出，
    这样「关着＝零 DOM 变化」才守得住）。判据与矩形一次 JS 往返（`elementTextOverflowState` 本批补
    `scrollHeight/clientHeight` 与 `rectTop/Left/Width/Height`）。
  - 🔴 **途中抓到一条真缺陷：absolute 浮层被表格的滚动容器整条裁掉**。旧实现浏览器实测 row 0 浮层矩形
    `x536 y1127 w271 h32` 对上 `.el-table__body-wrapper` 上缘 `1156` ⇒ 中心命中测试拿到的是 `DIV.cell`（浮层看不见），
    而同一列 row 1（`y1167`，放得下）命中的是浮层自己 —— 只有首行坏，所以很像"偶发"。原因是祖先链
    `trigger(relative) → td(relative) → … → body-wrapper(relative, overflow:auto) → .el-table(overflow:hidden)`
    既是 absolute 的包含块又是裁剪来源。修法：`BuildFixedPopperStyle` 用悬停时实测的视口矩形把浮层钉成
    `position:fixed`（fixed 的包含块是视口，只要祖先没有 `transform/filter/contain` 就不受 overflow 裁剪；
    实测 `td` 与 `.el-table-v2__row` 都没有），并显式写 `right:auto;bottom:auto` 让位（`PopperStyle` 是 `ElTooltip`
    最后拼上去的，同名属性后声明的赢）。EP 不遇到这个问题：它给整张表只渲染**一个挂在滚动容器外的虚拟 tooltip**
    （`use-tooltip/index.ts`）。另加「上方放不下（`rectTop < 40`）就翻到下方」，`placement` 与 `x-placement` 一起翻，
    箭头不会朝反。
  - **`ElText` 触发层那条 `max-width:100%` 不是可选的**（bUnit 全绿也能整条坏掉，见 `docs/testing-guards.md` §4 第 11 条）：
    EP 的 `.el-text.is-truncated` 靠自己 `max-width:100%` 被宿主钳住，插入一层 auto 宽 `inline-block` 触发 span 后
    shrink-to-fit 下限＝不换行文字的 min-content。computed 等价变异实测：现状 `trigger 180 / .el-text 378>180 被裁`，
    退回 EP 默认（去掉 `min-width:0;max-width:100%`）`trigger 378、378/378` ⇒ **冲出 180px 宿主、截断整个失效**。
  - **DOM 形状（写断言前认清多出来的是哪几层）**：`ElText` 是 `span.el-tooltip__trigger` **包住** `.el-text`
    （浮层不能放进 `.el-text`，那是裁剪盒）；`ElTable` 的 `<div class="cell">` 换成 `display:block` 的触发层、
    类名仍是 `cell` / `cell el-tooltip`；`ElTableV2` 开着时 `.el-table-v2__cell.is-ellipsis-tooltip{overflow:visible}`
    + 内层 `.el-table-v2__cell-text`（类名与三条规则来自 EP `table-v2.scss:221-226`，本仓父盒是块级所以补 `display:block`），
    关着时逐字节回到改动前（`hasInnerSpan:false / hasTrigger:false / openClass:false`，原生 `title` 照旧）。
  - 🟡 顺带修一处真缺陷：`ElTable.razor` 把单元格内容交给延迟 `RenderFragment` 后，`@for` 的计数器在求值时已是
    最后一行的下标 ⇒ `Type="index"` 列**三行全渲染成 `"4"`**（实测）。循环体内复制 `var cellRowIndex = i` 修掉。
  - **与 EP 的一处真实差异（记下来别当 bug 报）**：EP 的 `realWidth` 来自它自己的布局引擎，本仓用与 `<colgroup>`
    同源的列宽代替；**百分比列宽折算不出像素时干脆不写内联宽度**，交给 `table-layout:fixed`
    （bUnit `PercentageColumnSkipsInlineWidth`）。
  - **验证**：新测试文件 `ElOverflowTooltipTests`（4 例，钉 `position:fixed` 与坐标算术、翻面、`right` 分支、未裁不挂）
    + `ElTableOverflowTooltipTests`（10 例），改动的 `ElTextTests`/`ElTableV2Tests`/`ElMenuTests` 再 +17 例；
    **全量 bUnit 2341 通过 / 0 失败 / 0 跳过**（上一条记录 2310 ＋31）。新探针 `verify-ellipsis-tooltip.cjs`
    **54/54 PASS、连跑 8 次读数一致**（四个宿主页 + 每页正反对照 + 0 个 JS 异常）；菜单路径没迁件，
    `verify-menu-ellipsis.cjs` 同期 **26/26 PASS**。**变异（改 → 跑 → 还原 → touch → 再跑）**：
    M1 `fixed`→`absolute` → 探针 **9 条红**；M2 永不翻面 / M3 间距丢 / M4 不居中 / M5 不写 `right/bottom:auto` /
    M6 `right` 分支坐标错 → **各 1 条 bUnit 红**；还原后 `same bytes as backup: True`、四宿主重新全绿。
    编码守卫扫描范围 1560 → **1579**（含本批两份新文档、共用件、两份新测试与 5 份新探针），`bad=0`。
  - **有界没覆盖的**：不做浮层的视口左右钳位（窄列极靠右时理论上溢出右缘）、`top-start/-end` 当居中处理、
    悬停期间滚动会留下错位浮层（坐标在悬停那一刻冻结）、`PopperMinRoomAbove=40` 是单行浮层的估算、
    `BindTitle` 只量首帧之后一次（EP 每次更新重算）、`ElTableV2` 虚滚动每帧重建这些实例（行数多请按需开）。
    另记一条**本批没修**的缺陷：`ElTooltip.razor.cs:245-256` 与 `ElPopover.razor.cs:234-245` 把 `Offset` 拼成
    `"…translate(-50%, -{Offset}px)"`（**少了 `$`**，插值没发生），浏览器把整条 `transform` 丢掉 ⇒ 走默认 absolute 路径的
    浮层不居中、`Offset` 被忽略；`ElPopconfirm.razor.cs:256-261` 是同一族里唯一写对的。细节见
    `docs/text-ellipsis-tooltip.md` §8。

- 🟢 **`ElMenu` 的 ellipsis 截断行悬停浮出 `elTooltip` 显示完整文字（2026-10-08，本库扩展）**。触发点是用户提
  「另外 `ElMenu.Ellipsis`，当鼠标移动到有 Ellipsis 的菜单项时，能不能把该菜单用 `elTooltip` 完整显示一下」——
  上一批把溢出修成 CSS 截断之后，超长项只剩「一个很长很长很长的菜单…」，完整内容看不到。
  - **口径先说清：EP 没有这件事的非折叠版本。** EP 2.14.0 `menu/src/menu-item.vue:12-31`（源码直取，HTTP 200）里
    tooltip 的条件是 `parentMenu.type.name === 'ElMenu' && rootMenu.props.collapse && $slots.title`，
    **只有折叠态才挂**，`placement="right"`（`menu-item.vue:20`），`popper-class` / `popper-style` / effect 从 `rootMenu.props` 继承。
    本库把它扩到"非折叠 + 真的被截断"，折叠态仍走原生 `title`（`ElMenuItem.EffectiveTitle`），两套路互不重叠。
  - **判据是量出来的，不是猜的**：新增 `window.elementTextOverflowState(el)`（`wwwroot/js/dom.js:769`）与共用逻辑
    `MenuEllipsisTooltip.MeasureClippedTextAsync`，`scrollWidth > clientWidth + 1` 才算真被截断 ——
    **未截断的行悬停什么都不浮**（否则每个菜单项悬停都会盖一层浮层）。bUnit 里没有布局引擎，只能打桩；
    未打桩时 Loose 模式返回 `null` ＝ "量不到" ＝ 不弹也不抛（`EllipsisTooltipToleratesMissingJsState` 钉住）。
  - **复用真的 `ElTooltip`，两个坑是实测逼出来的**（不是设计出来的）：
    ① 浮层渲染在 `ElTooltip` **自己的触发 span 内部**，所以只能**包住**文字 span，绝不能放进
      `.el-menu-item__ellipsis-text` —— 那层 `overflow:hidden` 会把浮层一起裁掉；
    ② 触发 span 是 `.el-menu-item`（`display:flex`）的 flex item，而 flex item 的**自动最小尺寸＝min-content**
      （＝整行不折行的文字宽），必须 `Style="min-width:0"` 才允许收缩。同一 DOM 只改 computed `min-width`：
      `0` → `li 179/179` 不溢出、`span 302 > 139` 被裁、文字右缘 `470` ≤ 菜单右缘 `491`；
      `auto` → `li 322 > 179` **重新冲出边框**、`span 302 == 302` 不裁、文字右缘 `633`（超出菜单 142px）。
  - **挂载范围是收敛过的**：`Ellipsis == true && !Collapse`；子菜单标题再排除 `Mode == Horizontal`
    （横向标题悬停本身要开弹层，叠上去＝两层浮层互相盖）。`Ellipsis="false"` 与折叠态**一个触发器都不挂**，
    DOM 与上一批完全一致（探针 `offTriggers 0 / collapseTriggers 0 / hSubTriggers 0 / hItemTriggers 2`）。
  - **DOM 形状变更（宿主侧要知道）**：`li > span.el-menu-item__ellipsis-text` 变成
    `li > span.el-tooltip__trigger > span.el-menu-item__ellipsis-text`；按 `:scope > span` 写的选择器会落空，
    本仓两条既有 bUnit 断言因此改成后代选择器。竖向菜单项与子菜单标题浮层 `right`，横向菜单项 `bottom`；
    effect 跟随 `PopperEffect`（未指定＝`light`），`PopperClass` / `PopperStyle` 一并继承。
    实测：`.el-tooltip__popper.is-light` **285×32**、`x-placement="right"`、`left 482` 贴在文字右缘 `490` 旁、
    `elementFromPoint` 命中测试在最上层、自身未被裁。
  - **验证**：`ElMenuTests` 44 → **53 例**（新增 region「ellipsis 截断悬停 tooltip（本库扩展）」9 例）；
    探针 `demo/tools/probes/verify-menu-ellipsis.cjs` **15 → 26 项 PASS**、0 个 JS 异常。
    变异法（改 → 跑 → 还原 → **touch** → 再跑确认绿）6 处真实缺陷里 **bUnit 抓到 5 处**：阈值写坏 **失败 1 / 通过 61**、
    折叠态漏排除 **失败 2**（连带 `ElNavigationTests` 一条）、横向例外漏掉 **失败 1**、标题测算没接线 **失败 1**、
    `mouseenter` 没绑 **失败 4**；**而 `min-width:0` 撤掉后 bUnit 失败 0 / 通过 62 ＝ 纯盲区**，只有浏览器看得见
    （上面那组 179 ↔ 322 读数）。这是"bUnit 全绿不等于功能没坏"的又一例，也是本批坚持要探针的原因。
    顺带一条变异写法教训：把 ` Style="min-width:0">` 整段删掉得到的是 `error RZ1035 / RZ1024`（标记不闭合），
    那属于**语法破坏**而不是语义变异，必须改成 `min-width:auto` 才测得到布局回归。
    全量 bUnit **2310 通过 / 0 失败 / 0 跳过**（基线 2301 ＋9）；编码守卫扫描范围 1559 → **1560**
    （src 756 / demo 552 / test 148 / docs 91 / 仓库根顶层 13，新增那份是 `src/Components/MenuEllipsisTooltip.cs`），`bad=0`。
    设计与逐条实测见 `docs/menu-alignment.md` §9。
- 🧹 **`.gitignore` 增加 `_devserver.log`**（同批的用户「加一条」）：演示站后台起服务时重定向的日志每次都会追加，
  此前会被 `git status` 当成脏文件。`git check-ignore -v _devserver.log` → `.gitignore:38:_devserver.log`。

- **词条第四批：给运行时加上插值能力，并接分页 / 输入框字数 / 走马灯指示器（2026-10-07）**：前三批只能取整串文字，EP 有 17 个键带 <c>{...}</c> 占位符（分页的 <c>{total}/{pager}</c>、输入框字数 <c>{count}/{max}</c>、走马灯 <c>{index}</c>、表格 <c>{column}</c>、颜色面板 5 条等），不插值就只能继续手写 <c>Replace</c>。新增 <c>LocaleFormat.Interpolate</c> 与 <c>ILang.T(key, fallback, args…)</c> / <c>ILang.Template(key, 宿主模板, 默认模板, args…)</c>，语义逐条照 EP <c>use-locale/index.ts</c> 里的 <c>translate</c>（curl 实抓）：<b>没有对应实参时原样保留 <c>{name}</c></b>、只认 <c>{\w+}</c>（<c>{}</c>、<c>{a.b}</c> 不算）、数字按不变文化转（<c>1234</c> 不会变 <c>1,234</c>）、null 实参转空串。新增 9 个键（值取 EP 权威 locale）：<c>el.pagination.total/pagesize/goto/page/currentPage/prev/next</c>、<c>el.input.characters</c>、<c>el.carousel.indicator</c>；两份语言包现在各 36 键。
  - **`Template` 与 `Text` 必须分开**：<c>ElPagination.TotalText</c> 这类参数宿主编的是<b>模板</b>（含占位符），    走 <c>Text()</c>（"显式参数即最终文字、不再加工"）会让宿主自定义模板里的 <c>{total}</c> 原样显示出来；    而 <c>ConfirmButtonText</c> 这类是字面文字，反过来不该再插值。这条区分写进了接口注释与测试。
  - 顺手替掉了 <c>ElPagination</c> 里两处手写替换（<c>TotalText.Replace("{total}", …)</c> 与 <c>$"{size} 条/页"</c>），    并补了两个既有无障碍缺口：**分页上/下一页按钮**与**走马灯指示器**此前没有任何可访问名称    （EP分别用 <c>el.pagination.prev/next</c> 与 <c>el.carousel.indicator</c>），跳页输入框的 aria-label 接词条后    变成带真实页码的「第 {pager} 页」。**指示器索引按 EP 一样是 0 基**，注释里点明，避免误"修正"成 1 基。
  - 守卫加两条并配两侧夹具（<c>LangResourceGuardTests</c> 5 → **7 例**）：    ① 同一个键在两份语言包里的<b>占位符集合必须相同</b>（不同＝切语言时某一边露出 <c>{pager}</c>）；    ② 引用了带占位符的词条<b>却没传对应参数</b>就报（按"键 → 该键各处调用片段"扫描，实参写在下一行的跨行调用也算数，    有专门的"不误报"夹具）。覆盖断言同时抬高：引用点 ≥12、词条数 ≥30、被引用键里至少 3 条带占位符。
  - **补上表格两个可访问名称**（同一批的尾巴）：排序箭头与筛选漏斗此前只有图标/`aria-sort`，读屏说不出"按哪一列"；现接 `el.table.sortLabel` / `el.table.filterLabel`（EP 同名键，带 <c>{column}</c>），列名取 <c>header.Text</c>，**列名为空时整个 aria-label 不渲染**（不给读屏一个空名称）。无词条时用 EP 英文值兜底 → 属纯新增属性。新增 `LangInterpolationTests` 2 例 → 全量 **2240 / 0 失败**。
  - 🔧 **把上一批"老 flaky 已修完"的结论补全**：第四批加负载后那条弹层用例又红一次，原来还有一半 ——     <c>.Wait(timeout)</c> 阻塞测试线程等渲染器会与调度互等（表现为整 20 秒超时），而事件回调排进队列的重渲染    <c>WaitForAssertion</c> 只会被"已发生的渲染"唤醒。改成 <c>await InvokeAsync(...)</c> + 切语言后主动     <c>Render()</c> 之后连跑三次全量 **2238 / 0 失败**。教训写进 <c>docs/testing-guards.md</c> §3.9：    **同一件事可能有两半，修一半不算修完，要连跑多次回归。**
  - **验证**：新增 <c>LangInterpolationTests</c> **12 例**（插值语义 5 条边界、<c>T/Template/Text</c> 分工、    分页三态含宿主模板插值、每页条数/跳页前后缀/跳页与上下页 aria-label、输入框字数两种语言、指示器 0 基索引）；    全量 **2238 通过 / 0 失败**（上一基线 2224，+12 新用例 +2 守卫用例，连跑三次一致）；    探针 <c>verify-lang-i18n.cjs</c> 从 17 项扩到 **21 项全 PASS**（新增分页总数两种语言、输入框字数后缀、    指示器逐张索引，全部实测到数字被真实代入）。
- **词条第三批：弹层族接入（2026-10-07，接上两批）**：`ElDialog` / `ElDrawer` / `ElPopconfirm` / `MessageBox`（含 `MessageBoxOption` 与 `ElPopup`）/ `ElDialogGrowService` 的关闭按钮名称、确认/取消按钮、弹层标题全部改为三级取值（显式参数 > 词条 > 改动前字面量），新增 8 个键：`el.dialog.close`、`el.drawer.close`、`el.messagebox.title/confirm/cancel/close`、`el.popconfirm.confirmButtonText/cancelButtonText`，值全部取 EP 权威 locale（curl 实抓 `dialog-content.vue`、`drawer.vue`、`message-box/index.vue`、`popconfirm.vue` 的 `t()` 调用点与 `zh-cn.ts`/`en.ts` 的值）。键值细节里有一条值得记：**EP 的 popconfirm 在 en-US 里是 Yes / No，不是 OK / Cancel**，所以走词条的宿主拿到的措辞和硬编码时不同。设计、清单与限制见 `docs/lang-i18n.md` §2 / §6。
  - 🔧 **顺带补了一个既有无障碍缺口**：`ElDialog` 与 `MessageBox` 的关闭按钮此前<b>只有图标、没有任何可访问名称</b>（EP 用 `t('el.dialog.close')` / `t('el.messagebox.close')`）。这次连 fallback 都给了 EP 的英文值 —— 属于纯新增属性、不改任何可见文字，但确实新增了两个 DOM 属性，写在这里以免被当成"词条改动顺手加的私有属性"。`ElDrawer` 原本硬编码英文 `"Close"`，按规则保持为 fallback，接 zh 词条后才变成「关闭此对话框」。
  - **服务 + DTO 的接入形状（和组件族不一样，必须说清）**：`MessageBoxOption` 是 DTO、`MessageBox` / `ElDialogGrowService` 是服务，都没有渲染生命周期。做法是 **DTO 默认值留 `null`，由渲染处取词** —— 标题走 `ElPopup.ResolveTitle()`，按钮走 `CreateButtonRenderer` 里的 `RenderFragment` 闭包，所以**弹层开着的时候切语言，按钮文字与关闭名称会当场跟着换**（在服务里提前算成字符串就做不到）；只有标题在打开时定型（它要写进 `DialogOption`）。`ElPopup` 不是 `ElementComponentBase` 的后代（应用根上的弹层宿主），所以它自己 `[Inject] ILang` + 订阅 + `Dispose` 退订。
  - ⚠️ **一个容易做错的点已被测试钉住**：`MessageBoxOption.Title` 默认值从「提示」改成 `null` 之后，`ElPopup` 渲染 header 的条件是「标题非空」——解析顺序稍有差错，整块标题就会凭空消失。`MessageBoxKeepsHeaderAndChineseTextsWithoutEntries` 专门断言 header 仍在且文案不变。同理 `ConfirmAsync`/`AlertAsync` 的 `title` 默认参数改成 `null` 后必须在服务内回落到词条，否则标题为空。
  - **刻意没接的**：`ElDialogGrow` 的「知道了」（EP locale 里没有这个词，挂到 `el.messagebox.confirm` 上会把措辞悄悄改成「确定/OK」）；`el.messagebox.error`（本仓 Prompt 的校验提示由宿主 `InputValidator` 给字符串，库里没有那句"输入的数据不合法!"，无消费者的键不进语言包）。
  - **验证**：新增 `LangOverlayTextTests` **11 例**（Dialog/Drawer 关闭名称的三态、Popconfirm 的 EP 措辞、MessageBox 的标题不为空 + 按钮取词 + **开着切语言当场更新** + 显式压制词条、Prompt 标题经服务不再写死）→ 全量 **2224 通过 / 0 失败**（2213 + 11；旧断言里"取消/提示/确定"的用例一条没退红，说明 fallback 链成立）。探针 `verify-lang-i18n.cjs` 从 15 项扩到 **17 项全 PASS**（新增"对话框关闭按钮取 `el.dialog.close`"与"开着对话框切语言名称当场换"）。
  - 🔧 **顺带定位并修掉一个"老 flaky"**：`ElFeedbackTests.MessageBoxPromptCreatesInputAndBeforeCloseCanBlock` 长期被记成"并行争抢抖动"（`TestWait.cs` 还点名过它）。真因是测试写法：`popup.InvokeAsync(() => messageBox.PromptAsync(option))` 里那个 Task 要点按钮才完成，不能 await，于是调用方也没等这次调度+渲染批落地，下一行直接 `Find()` 就偶发拿到空 markup。改成「`InvokeAsync` 里只发起 + 等 `InvokeAsync` 自己的调度完成 + 首帧断言包进 `WaitForAssertion`」，并把新用例里"切语言"也放进渲染器调度队列执行 → 连跑三次全量 **2224 / 0 失败**（改前是两次里红一次）。判据写进 `docs/testing-guards.md` §3.9：**只在并行全量里红、单跑必绿 → 先查有没有等调度，别先去加大超时**。\n  - 🔧 **两条探针自身的教训（产品没错，先怀疑脚本）**：① 对话框开着时遮罩会拦掉 Playwright 的可点击性检查（`element intercepts pointer events` 一路重试到超时），改用 `page.evaluate` 做 DOM 层点击；② 有一节按 zh 断言却报假 FAIL，原因是上一节把语言留在了 en-US —— **多状态脚本要在每节结尾把状态归位**。另外本会话第一次跑这批测试时把测试线程挂死（`ConfirmAsync` 返回的 Task 要点按钮才完成，绝不能 `Wait()`/`await`），改成"发起但不等待 + 收尾点掉按钮"后正常，这条已经写进测试文件注释。
- **词条铺到「下拉与空态族」（2026-10-07 第二批，接上一条）**：`ElSelect` / `ElSelectV2` / `ElCascader` / `ElEmpty` / `ElTable` / `ElTableV2` / `ElTree` 的占位、加载中、无数据、无匹配、空态文案全部改为 **`Lang.Text(key, 显式参数, fallback)`** 三级取值，共 9 处消费点、10 个新键（`el.select.*`、`el.cascader.*`、`el.table.emptyText`、`el.tree.emptyText`）。键名与优先级形状逐个对照 EP 权威源码（`useSelect.ts` 里就是 `props.loadingText || t('el.select.loading')`；EP 的 `ElEmpty` 回落的是 `t('el.table.emptyText')`，本仓跟随同键）。**明确不接**：`ElAutocomplete`（EP 的 autocomplete.vue 里没有任何 locale 调用，自造 `el.autocomplete.*` 就是伪装 EP 契约）、`ElTableV2.LoadingText`（EP 无 `el.table.loading`）、`ElCarousel` 预览层「关闭」与 `title`、`ElTreeSelect.EmptyText`（它显式传值给 `ElTree`）；`PickerLocale.cs` 的月份/星期 45 个键属日历本地化，另开一轮。
  - **取值规则收敛成一条**：词条值一律用 EP 权威 locale，fallback 一律用"本仓改动前那个字面量"。上一条给 `el.carousel.leftArrow/rightArrow` 的 zh 值填了本仓原文「上一张 / 下一张」作为有意差异，这一批判断改掉了：词条本身是 opt-in 的，"怕改掉别人在用的读屏文本"不成立（不接词条就改不到），**留一个组件例外只会让规则变成两条**。已把这两个值统一成 EP 的「上一张幻灯片 / 下一张幻灯片」，并同步改了探针断言。
  - 🔧 **订阅上提到 `ElementComponentBase`**：基类 `[Inject] public ILang Lang` + `OnInitialized` 里 `SubscribeLocaleChanged(() => InvokeAsync(StateHasChanged))` + `Dispose(bool)` 里退订。第一批两个组件各自订阅（6 行一处）尚可，第二批一上来 7 个组件 14 处就不划算了，而且"复制配对代码漏一半＝切一次语言泄漏一批组件"。放基类不给宿主增加任何新要求（`ElementComponentBase` 本来就注入 MessageBox / DialogService / MessageService / LoadingService，少 `AddElementServices()` 一个组件都渲染不出来）。相应删掉 `ElInputNumber` / `ElCarousel` 里的 `@inject ILang Lang` 与本地订阅字段（派生类再生成同名属性会遮蔽基类的）。
  - ⚠️ **兼容代价**：`ElSelect.Placeholder`、`ElSelectV2.NoDataText/NoMatchText/LoadingText`、`ElCascader.Placeholder`、`ElTableV2.EmptyText`、`ElTree.EmptyText` 等参数默认值由中文常量改成 `null`（`null` 才能表达"宿主没指定"，词条才有机会生效）。**渲染结果一字不变**（fallback 就是原字符串；旧测试里断言这些中文的用例全绿），但直接读这些属性拿默认值的宿主代码会拿到 `null`。`ILang` 同时新增 `Text(key, explicitValue, fallback)` 接口成员（不是扩展方法，组件里免 `using`）。
  - **验证**：新增 `LangDropdownTextTests` **12 例**（三态：不取词零变化 / 词条生效 / 显式参数压制；Empty 与 Tree 空态；"只改服务不碰参数"的重渲染；Dispose 后切语言不抛）+ `TestLang` 替身（喂真 JSON 走 `ElementLang` 的解析路径，不 mock 接口，键名写错当场就红）；`LangResourceGuardTests` 3 → **5 例**（补 `MaskComments` 两侧夹具 —— 基类文档注释里举例写的 `el.xxx.yyy` 曾被当成真引用误报过）→ 全量 **2213 通过 / 0 失败**（连跑两次一致，上一基线 2199）。新探针断言下拉占位在 `请选择`/`Select` 之间随语言来回，`verify-lang-i18n.cjs` 从 10 项扩到 **15 项全 PASS**（含"宿主显式写的 `多选` 不被词条覆盖"这条反向优先级断言，以及实测切语言在几百组件的 `/basic` 页耗时 560ms/484ms）；演示站 `/basic` 新增 `#c4-lang-select-row`。
- **库级词条（i18n）运行时接通（2026-10-07）**：此前本仓**没有**可用的 i18n —— `Lang/ElementLang.cs` 与 `Lang/ILang.cs` 是从未接通的死代码（`ElementLang` 未实现 `ILang`、`ILang.InitElementLangBaseAsync()` 是 `void` 根本无法 await、`AddElementServices` 的 23 个注册项里没有它、全仓零消费方、`T()` 只是 `configuration?[key]` 没有 fallback 通道、语言包只有 4 个演示键）。触发点是 ElInputNumber 的 P2-3（EP 那边 `aria-label` 走 `t('el.inputNumber.decrease')`）。设计、坑与实测见新增的 **`docs/lang-i18n.md`**。
  - 运行时：`ElementLang : ILang`；新增 `T(key, fallback)`（`el.a.b` 与 `el:a:b` 两种键写法归一、只认字符串、null/空白一律算缺失）；内置加载器读库自身的静态资源 `_content/Element/lang/{locale}.json`；注册为 **Scoped**（注册成 Singleton 会消费不到宿主那个 Scoped `HttpClient` 而直接抛 `InvalidOperationException`），工厂用 `GetService<HttpClient>()`，取不到就整体退化为"一切取词走 fallback"—— **组件渲染不依赖词条可用性**；词条字典发布后只读、切语言时整体替换 volatile 引用；404/断网/坏 JSON 一律不抛，保留上一次成功载入的词并记 `LastLoadError`；`lang` 入参走白名单校验（只放字母数字连字符，`../../appsettings` 被拒且**不发请求**）；老的 4 参构造与"宿主 `IConfiguration` 优先"语义原样保留可用。
  - 词条值**取 EP 权威 locale**（curl 实抓 `packages/locale/lang/zh-cn.ts`、`en.ts`）：`el.inputNumber.decrease/increase` = 「减少数值 / 增加数值」、「decrease number / increase number」。`el.carousel.leftArrow/rightArrow` 的 en 值同 EP，zh 值**刻意保留本仓原有的「上一张 / 下一张」**而不是 EP 的「上一张幻灯片」—— 那两个字符串此前对所有中文用户可见，对齐不该顺手改掉别人已经在用的无障碍文本。
  - 首批消费方：`ElInputNumber` 步进按钮（优先级 **显式参数 > 词条 > 原英文默认**，宿主不取词时 DOM 一字不变）、`ElCarousel` 左右箭头含预览层共 4 处（fallback 就是原来硬编码的中文字）。演示站 `/basic` 新增 `#c4-num-lang-target` + `#c4-num-lang-state` 与「中文标签 / English labels」切换按钮，`Program.cs` 在首帧之前 `InitElementLangBaseAsync()`。
  - 🐛 **接通过程中由浏览器探针先抓到的真缺陷**：Blazor 在父组件重渲染时会跳过**参数没变**的子组件，所以宿主 `await SetLangAsync(...)` + 自己 `StateHasChanged()` 只刷得动页面自身文本，深层组件的 `aria-label` 停在旧语言上。探针第一版 **8/10**，两条 FAIL 正是这个（状态行已是 `decrease number`，按钮标签仍是「减少数值」）。修法：`ILang.LocaleChanged` 事件 + 新增 `LangSubscriptionExtensions.SubscribeLocaleChanged`（返回 `IDisposable`，组件在 `OnInitialized` 存句柄、`Dispose` 里释放；复制加减号配对漏一半就是切一次语言泄漏一批组件）。改完探针 **10/10**。
  - **验证**：新增 `ElementLangTests` **20 例**（取词与回退、键名归一、数字布尔不算文案、404/坏 JSON 保留旧词、切语言整体替换而非合并、非法 locale 零请求、无 HttpClient 退化、老 ctor 与宿主配置优先、DI 注册可解析、订阅句柄退订与重复 Dispose、两个组件"参数没变也要跟着换语言"的重渲染护栏）+ 新增第 5 条常驻守卫 `LangResourceGuardTests` **3 例**（中英键集互等、源码引用的 `el.*` 必须两边都在且非空白、带覆盖断言与两侧夹具）→ 全量 **2199 通过 / 0 失败**（本轮前基线 2176，+23 全为新增，无一项退红）；新探针 `verify-lang-i18n.cjs` 支持 `argv[2]` BASE，`docs/testing-guards.md` §1 已收录新守卫并把基线数字更新到 2199。
  - ⚠️ **接口破坏性说明**：`ILang.InitElementLangBaseAsync()` 由 `void` 改为 `Task`（原来是 `void` 就永远等不到加载结果），接口另新增 `LocaleChanged` / `IsLoaded` / `LastLoadError` / `SetLangAsync` 两个重载与 `T(key, fallback)`。该接口在本次改动前**无任何实现方与消费方**（上面已证），所以只对"第三方自己实现了 `ILang`"这一种情况是破坏性的；`El*` 组件的公开参数与 DOM 契约没有破坏。
- **新增 `docs/testing-guards.md`（验证层手册，2026-10-07）**：把本会话攒下的守门工具集中到一处，下一次会话/下一个人不必再考古日志。内容：三条常驻守卫（渲染树序列号漂移 R1 / 捕获后加属性 R2 / 等待必须显式超时）的规则、判据与各自主挡住的真实事故；**写守卫的四步配方**（掩码注释与字符串 → 两侧夹具 → 覆盖断言 → 反向注入验证，并说明"过度剥离＝假绿"）；`dotnet` env 全串、`dotnet test` 必须用 Python 捕获、`touch` 再 build、demo 5199 重启与 `--noproxy` 等跑法；**探针资产盘点**；**探针脚本自身的六个坑**；"是组件错还是测试错"的六步取证顺序；加东西时的检查单。`docs/getting-started.md` §7 已加指路条。
  - 探针盘点（全量 195 份 `.cjs`，非抽样）：**0 份语法损坏**（逐个 `node --check`）；**194/195 的依赖 class 仍在**（比对基准＝源码 class 词表 ∪ 演示站 26 条路由真实渲染出的 class）；**1 处过时**：`probe-table-editors.cjs` 查 `.el-picker__popper`，库内已无此类名（现契约 `.el-date-picker__dropdown` / `.el-picker-panel`）；**101 份被 CHANGELOG/docs 点名为回归依据**，其余按一次性诊断存档看待；**133 份硬编码端口**（71 份同时从未被点名，62 份被点名但换端口需改一行）；5 份是访问 element-plus.org 的**外部基准**探针，不算本地回归。
  - 为此新增采集器 `collect-rendered-classes.cjs`：一个浏览器会话内用 `pushState + 手动派发 popstate` 让 Blazor 路由切换（**不重载 WASM**，26 条路由几十秒扫完，对比逐页 `goto` 每页 45~90s 冷启动），并说明了 KeepAlive 导致 class 集合累积这一解读边界。
  - 自查修正：本文数字全部换成可用 shell 命令复现的口径（原先"109 份硬编码 / 62 份未点名"用的是自写正则，复现口径实为 **133 / 71**），"46 份 verify-*"更正为 195 份总数下的 44 份 `verify-*`。


### Changed

- **`ElInputNumber` P2-3 落地：步进按钮 `aria-label` 可本地化（2026-10-07），并更正上一条里我自己写错的"正确做法"**。上一条把待办记成"先补中英两份词条再接 `T(key, fallback)`，属独立小改动"。全仓实测推翻了它的前提 —— 本仓 `Lang` 子系统**从未被接通**：`ElementLang` 并未实现 `ILang`（接口方法 `InitElementLangBaseAsync()` 无实现）、`Extension.cs` 的 `AddElementServices` 共 23 个注册项里没有任何 lang 服务、全仓（排除 `bin`/`obj`/`node_modules`/`attic`）搜注入与调用**命中 0**、`ElementLang.T()` 只是 `configuration?[sections]`，**没有 fallback 重载**，漏配即返回 null。照那条施工＝为一个组件的标签先造库级 i18n 运行时，而且可能把无障碍标签从英文变成空白，属假接入。
  - 本轮做法（组件级、零 DOM 回归）：新增 `DecreaseAriaLabel` / `IncreaseAriaLabel`，不传时回落原来的 `decrease` / `increase`（回落常量与原字面量同值，现有页面 DOM 一字不变）；传 null 或空白同样回落 —— 空的 `aria-label` 是无障碍反模式，不能让按钮变成无名元素。**标注为本仓扩展**：EP 的 props 里没有这两个键，不挂到 EP 名下（EP 侧靠它自己的 locale provider）。
  - **验证**：`ElInputNumberTests` 新增 6 例（默认值不变 / 覆盖生效 / `controls-position="right"` 是另一套模板分支必须同样生效 / 空白回落 `Theory` ×3）→ 本组件 **42/42**，全量 **2176 通过 / 0 失败**（基线 2170 + 本次 6，逐一对得上）。**本轮不跑浏览器探针**并记下理由：演示站当前服务的是改动前的构建，拿旧二进制去验"默认标签未变"必然变绿＝假绿。库级 i18n 运行时改判为**独立提案**，工作项清单（`ElementLang : ILang` + 实现 `InitElementLangBaseAsync` + `T(key, fallback)` 重载 + DI 注册 + 补词条 + 首个消费方含 `ElCarousel` 的硬编码中文标签）写入 `docs/input-number-alignment.md` §3b P2-3。
- **`ElInputNumber` P2 三项定案（2026-10-07，全部实测后再决定，不凭记忆）**：一项判为"保持现状的有意差异"、一项判为"不该改"、一项确认为真缺口但需配套改动故未动。
  - **`type=number` vs `text`：保持 `text`（有意差异，已登记）**。在真实浏览器里运行时切 `type` 做对照（新脚本 `demo/tools/probes/measure-inputnumber-type.cjs`），实测 `number` 会破坏组件三条既有语义：① 滚轮在聚焦输入框上会**原生改值**（12 → 13，用户想滚页面却改了数）；② 输入非法文本 `12ab` 被浏览器**静默吞成 `12`**，绕过组件自己的非法输入策略（`text` 下我们能看到 `12ab` 并自行判定）；③ `ArrowUp` 变成**双步进**（原生 + 组件各一步，12 → 14）。EP 用 number 是图移动端数字键盘与原生步进 —— 前者本仓已有 `Inputmode` 参数覆盖，后者我们本就自实现了一整套 spinbutton 语义（`role` + `aria-value*` + 上下键/PageUp/Home/End）。换成 number 是净损失，故不改。
  - **字体图标 vs `<el-icon>` SVG：不改**（仓级约定，Win7/Chromium 75 级内核下可控性更好），且本轮新增的 `DecreaseIcon` / `IncreaseIcon` 插槽正是宿主自行换 SVG 的正规入口，需求已可满足。
  - **`aria-label` 走 i18n：确认为真缺口，但本轮不做**。实测两个语言包里**没有任何 `inputNumber` 词条**（grep 命中 0），此时照 EP 直接写 `t('el.inputNumber.decrease')` 只会拿到空串 —— 属于「看起来对齐了其实读不到文案」的假接入。正确做法是先补中英两份 `inputNumber.decrease / increase` 再接 `T(key, fallback)`，已作为独立小改动记入文档待办。
- **`ElInputNumber` P1：补 Exposes `FocusAsync` / `BlurAsync`（2026-10-07）**。EP 实抓 `defineExpose({ focus, blur })` —— InputNumber 公开的就只有这两个方法，本仓此前一个都没暴露，宿主拿不到焦点控制。
  - 实现：`<input>` 加 `@ref`，方法复用仓内既有 `elementReference.Dom(JSRuntime)` 通道（与 `ElTreeSelect` 同款），不新增 JS。验证 2 例：`ExposesFocusAndBlurLikeElementPlus` 断言**真的发出** execFocus/execBlur 调用（而不是「有个同名方法」）、`RefDoesNotDisturbExistingDomContract` 锁住加 @ref 后 id/role/value/`is-right` 一字不变 → `ElInputNumberTests` **36/36**。
  - 🔧 **同时撤回我自己写的两个假缺口**：P1 清单里的 `EmptyValue`/`InitialValue` 与 `Label`/`Autofocus` 经核对 **都不是 EP InputNumber 的 props** —— dev 分支 `input-number.ts` 运行时 props 实测 16 个键（align / controls / controlsPosition / disabled / formatter / id / inputmode / max / min / modelValue / parser / precision / step / tabindex / validateEvent / valueOnClear），全文没有这两个词。它们是我凭别的组件的记忆写进去的。**原则写进文档**：不实现权威源码里不存在的 API，否则等于自造一套与 EP 不兼容的参数面；确有业务需要就明确标成「本仓扩展」，别挂 EP 名下。
- **`ElInputNumber` P1：补 `Formatter` / `Parser`（2026-10-07）**。EP 实抓签名是 `formatter?: (value: string) => string`、`parser?: (value: string) => string` —— **字符串进出**，不是我最初在清单里写的 `decimal?→string`；照 EP 落地。
  - 三个接入点：① 显示出口统一走新的 `DisplayTextFor(decimal?)`（`inputText ?? Formatter(FormatValue(Value))`，5 处赋值点全部改道，含清空路径）；② 解析入口在 `TryParseInput` 开头过 `Parser`，非法结果沿用既有失败路径（保持上一次显示与值，等价 EP 的 NaN 跳过）；③ 两个回调都加异常护栏 —— **与 EP 的有意差异**：EP 不保护宿主回调，我们保护，避免宿主一行 `throw` 把整页渲染干掉。输入过程中的原文不做二次格式化（否则光标与内容错乱）。
  - **验证**：bUnit 5 例（Formatter 生效、Parser 往返 2,500 %→2500、非法解析保持原值、Formatter 抛异常退回纯数字、不传时零变化）→ `ElInputNumberTests` **34/34**；演示站 `/basic` 加 `#c4-num-formatter` 示例；新探针 `verify-inputnumber-formatter.cjs` **5/5 PASS**。该探针第一版 2 条假 FAIL：`Control+a` + `keyboard.type` 的按键没落进输入框，换 `fill()` + `Tab` 就通过 —— 又一次「先怀疑探针」。
- **`ElInputNumber` P1：补 `DecreaseIcon` / `IncreaseIcon` 插槽（对齐 EP `decrease-icon` / `increase-icon`，2026-10-07）**。EP 模板里两处控制按钮的图标都是 `<slot name="decrease-icon"><el-icon>…</el-icon></slot>` 形态 —— 插槽给出即整体替换默认图标。
  - 实现：两个 `[Parameter] RenderFragment`，四处图标位（`controls` 左右两种布局各自）统一改为「有插槽用插槽、没有则回落默认 `el-icon-minus` / `el-icon-plus`（`controls-position="right"` 时是 `arrow-down` / `arrow-up`）」。**不传插槽的实例 DOM 一字不变**，不会牵动现有页面。演示站 `/basic` 新增 `#c4-num-icons` 示例（左右箭头图标）。
  - **验证**：bUnit 新增 2 例（`IconSlotsReplaceDefaultIcons` 断自定义图标在位且默认图标不再出现、`DefaultIconsRenderWhenSlotsAreNotSet` 锁默认路径零变化）→ `ElInputNumberTests` **29/29**；新增浏览器探针 `verify-inputnumber-icons.cjs` **5/6→5/5 PASS**（顺带确认换了图标后点击步进仍正常、0 JS 异常）。
- **`ElInputNumber` P0-4：步进按钮"按住连发"，对齐 EP `v-repeat-click`（2026-10-07）——P0 四项全部完成**。EP 权威实抓 `directives/repeat-click/index.ts`：`REPEAT_DELAY=600`、`REPEAT_INTERVAL=100`，mousedown 立即一步、按住 600ms 后每 100ms 连发、document 级 mouseup 停止。
  - 实现选**纯 C#、不引 JS 通道**：`CancellationTokenSource` + `Task.Delay` 循环，停止走 `@onmouseup` / `@onmouseleave`，`Dispose()`（基类是 virtual）里取消并 `base.Dispose()`；`repeatEngaged` 吞掉连发后随之派生的那次 click，避免末尾多走一步。**与 EP 的一处有意差异**已记入对齐文档：EP 在 mousedown 即出第一步且没有 click 监听，本组件保留 `@onclick` 作为快速点击通路（Blazor 监听不到 document 级 mouseup，靠 click 更稳），因此那一步延后到 mouseup —— 观感差约 80ms，净步数一致。
  - **验证**：bUnit 新增 3 例（按住连发 ≥2 步且松开即停 / 快速点击净一步 / 连发后的 click 被吞）→ `ElInputNumberTests` **27/27**；新增浏览器探针 `verify-inputnumber-repeat.cjs` **6/6 PASS**（真实 `mouse.down()` 按住 950ms 实测走 3 步、拖出按钮即停、0 JS 异常）。该探针第一版**假 FAIL** 值得记：按钮 `boundingBox().y=2923` 在视口外，而 `page.mouse.move()` 用的是视口坐标，按下根本没落在按钮上；加 `scrollIntoViewIfNeeded()` 后才正确 —— 量真实鼠标行为前必须先把目标滚进视口。
  - 下一步进 P1：`decrease-icon` / `increase-icon` 插槽、`Formatter`/`Parser`、`EmptyValue`/`InitialValue`、Exposes（focus/blur）核对、`Label`/`Autofocus`。
- **`ElInputNumber` P0-3：补 `Align` 与 EP 的 `is-{align}` 契约（2026-10-07）**。EP 2.14 有 `align: left|right|center`（默认 `center`），根节点输出 `is-{align}`，`theme-chalk/input-number.scss` 用 `@each $align in (left,right,center)` 给 `.el-input__inner` 设 `text-align`；本仓此前既无参数也无这三条规则。
  - **零视觉回归的关键判断**：本仓基底 CSS 本来就有 `.el-input-number .el-input__inner{text-align:center}`，所以按 EP 取默认 `center` 不改变任何现有页面观感（若基底是左对齐就得反向权衡，已写进 `docs/input-number-alignment.md`）。新规则进 `index.css` 末尾 `/* ==== ElInputNumber ==== */` 段三条并列，特异度 (0,3,0) 压过基底 (0,2,0)；不涉及 `gap`；非法值归一到 `center`。
  - **回归**：`ElInputNumberTests` **24/24**（新增默认 `is-center`、`left`/`right` 生效且互斥、非法值退回 4 例）；演示站补 `#c4-num-align-left`/`#c4-num-align-right` 示例；新增浏览器探针 `verify-inputnumber-align.cjs` **6/6 PASS**（顺带覆盖 P0-1 常驻清除与 P0-2 value-on-clear 示例）。P0 仅剩 **第 4 项：控制按钮"按住连发"**（EP `v-repeat-click`）。
- **`ElInputNumber` 对齐 EP 第一批（P0-1 清除图标语义 + P0-2 `ValueOnClear`），演示站补常驻清除与 value-on-clear 示例（2026-10-07）**：旧实现 `ShouldShowClearIcon => Clearable && … && (ShowClearIcon || isHovering)` 让 `ShowClearIcon` 单独设置完全无效。
  - 修法是把两条通路拆开：`ShowClearIcon` ⇒ 常驻显示；`Clearable` ⇒ 悬停显示（EP `el-input` 惯例，本组件 `value-on-clear` 亦按 EP 语义）。新增 `[Parameter] string ValueOnClear`（`null` 默认 / `min` / `max` / 数字，`ResolveClearedValue()` 解析，越界由既有 `Normalize` 夹回），`ClearAsync` 统一走它。演示站 `/basic` 补 `#c4-num-always-clear`（常驻）与 `#c4-num-onclear`（`Min=5` + `ValueOnClear=min`，点清除落到 5 而不是空）两个示例。
  - **P0 立项理由经过两次自我推翻，最终结论第三种才是对的**：① 初判「两份独立探针同报 ⇒ 组件缺陷」；② 我只查了 `/input-number` 就宣布「演示站没配开关、探针在断言不存在的东西」—— 这个推翻也是错的；③ 真相是开关配在 **`/basic` 的 `#c4-num`**（`Clearable=true`，页面文案即「悬停才出现」），而**探针未悬停就找图标**。所以三处一起改：组件语义、演示覆盖、探针先 `hover()` 再量几何。
  - **回归**：`probe-inputnumber` **8/8**、`probe-c4` **15/15**（此前分别 6/8、13/15）；bUnit `ElInputNumberTests` **20/20**，含修复前必失败的新护栏 `ShowClearIconAloneRendersClearIcon` 与 `ValueOnClear*` 三条。体检与剩余缺口见 `docs/input-number-alignment.md`（P0-3 `Align`/`is-{align}`、P0-4 控制按钮按住连发未做）。改前专项备份 `E:\电子病历\_backup_before_ElInputNumberEP_20261007`。
- **94 份「被 CHANGELOG/docs 点名的 `verify-*`/`probe-*`」现役探针全量实跑，结果分组写进手册 §3.7（2026-10-07）**：3 通道并跑 + 失败串行复跑，每份解析自身 PASS/FAIL 输出。**71 份全绿**（含 9 份纯取证 dump 型），**23 份有 FAIL 或未跑通**，按性质分四组：
  - **A 断言腐化（组件无错）**：`probe-time-scrollbar.cjs` 7 条断言在查 `fix.css` 的规则 —— 该文件 2026-09-26 已合并进 `index.css` 并删除；`verify-selectv2-navscroll.cjs` 前置断言「页面本身可滚动」拿到 `scrollY=0`；`verify-gap-margin-equivalence.cjs` 是改 gap 前的预演工具，使命已完成。另 `probe-date-range-panel.cjs` 2 条英文月份断言把「当月」写死（期望 2026 September 得 October），属日期敏感断言，按腐化处理。
  - **B 疑似组件缺陷（下一步开专项）**：`ElInputNumber` 清除图标 `.el-input__clear` **不渲染** —— `probe-c4` 与 `probe-inputnumber` **两份独立探针报同一现象**（最可信）；`verify-verifycode-demo` 的 `InputWidth=200` 未生效（实测 w=267）；`probe-button-group-checks` 按钮组 Large/Small 实测高都 32px（尺寸级联疑未透传）；`probe-table-editors` 行内「供应商」选择后单元格未更新；`probe-nav-collapse` 展开/折叠宽度不符。
  - **C 时序/视口敏感待定性**（勿轻率判缺陷）：`probe-carousel-image`（箭头 opacity 采到过渡值 0.525）、`probe-selectv2-clear`（叉与箭头中心 x 差 4px）、`probe-scrollbar-final`、`probe-tablev2-thumb`、`probe-carousel-preview`、`probe-keepalive-perf` 各 1~2 条，须单份串行复跑定性。
  - 🔧 **本轮我自己弄坏 12 份并已修复**：上一轮批量加 `process.argv[2]` 的脚本产出了 `const BASE = process.argv[2] || BASE;`（自引用 → `ReferenceError: Cannot access 'BASE' before initialization`）。从 pristine 备份复原后改用「声明必早于使用」的顺序自检重写，复跑证据 `probe-demo-shell` 33/33、`probe-tablev2` 20/20、`probe-keepalive` 7/7、`probe-preview-arrow-center` 8/8 恢复全绿。教训固化：**`node --check` 只查语法，抓不到 TDZ 这类运行时错误 —— 批量改代码后必须实跑受影响文件**；已把该条写进技能与手册 §3.7 尾注。
  - 全量 bUnit 仍是 **2150 通过 / 0 失败**（本轮未动源码，只动探针）。
- **新增第四条全库守卫 `ProbeHygieneGuardTests`（7 例）：把 attic 治理变成机器规则（2026-10-07）**。手工归档必然缓慢回退，所以把判据交给测试守着。三条硬规则（现状 0 违规）：**H1** 连本地演示站的探针必须可覆盖地址（`process.argv[2]` 或 `process.env.*BASE`，**只写在注释里不算** —— 判据跑在 `TestJsScanner.StripComments` 之后）；**H2** `attic/` 里的脚本不得被 `CHANGELOG.md`/`docs/*.md` 点名（被引用即说明它已复活成回归资产，该移回主目录）；**H3** 探针不得拿 `#app` 当就绪判据（静态 HTML 里它一直在，等它等于没等 Blazor）。
  - **判据作用域收敛**：首版把 H1 无差别套到全部 148 份，立刻抓出 5 份违规 —— 但它们其实是**不该管**的：3 份访问 `element-plus.org` 的外部基准探针、2 份用 `setContent` 喂内联 HTML 根本不连服务器，都没有端口可覆盖。改成"仅当代码里出现 `://localhost|127.0.0.1` 才要求可覆盖"，并加 `local >= 120` 的覆盖断言防"判据收紧到什么都不查"式假绿。
  - **两侧夹具**（判据抽成纯函数便于直接喂样本）：注释里写 argv 必须仍算未实现；`process.env.EB_BASE` 与 `process.argv[2]` 同等合格；外部基准与内联 HTML 探针不许误报；H2/H3 各有正反样本。另把字符串语义写清：`TestJsScanner` **有意保留字符串内容**（兼容性守卫要在剥离后仍能 `Contains("\"inserttable\"")`），所以"argv 只写在字符串里"不在打击面内 —— 这是设计边界而非漏洞，已用断言固化。
  - **反向验证**：临时放入一份写死 `http://127.0.0.1:5199` 的 `zz-teeth-check.cjs` → 守卫立刻失败并点名该文件；删除后复原（探针目录已核对无残留）。**全量 bUnit：2150 通过 / 0 失败**（新增 7 例守卫）。手册 §1 表格、§3.5、§5 检查单同步。
- **48 份一次性诊断探针移入 `demo/tools/probes/attic/`，主目录 148 份实现「全部可换端口」（2026-10-07）**：上一轮盘点后仍有 53 份「未被文档点名 + 端口硬编码」的老脚本混在现行探针里，跑回归时难分辨该信谁。移入 `attic/`（**内容一字未改，只换位置**，`attic/README.md` 写明判据、运行方式与还原方法）；其中 5 份名字是 `verify-*` 的**没有**进 attic —— 按仓库命名约定它们是回归资产，改为就地补 `process.argv[2]` 覆盖能力。
  - 判据三条（全部可复现）：端口硬编码（既无 `process.argv[2]` 也无 `process.env.*BASE`）、**未被 CHANGELOG/docs 任何一处点名**、未被其它探针或 runner 引用（第三条件避免断链）。核对结果：主目录 148 + attic 48 = 196 守恒，**主目录硬编码降到 0**，attic 内被文档点名者 0。
  - 改这 5 份时按「BASE 自带路径」形状写成 `const URL = (process.argv[2] || 'http://127.0.0.1:5199') + '/form2';`，逐份 `node --check` 通过；并做 **A/B 复核**：备份原文件与改后文件在同一台演示站各跑一遍，结果一致（都 10/11）才提交。顺带暴露 `verify-selectv2-navscroll.cjs` 的「页面本身可滚动」前置断言已随布局变化过时（`scrollY=0/maxScroll=0`）——与本次改动无关，属未点名老探针的自然腐化，重用前先修前置条件。
  - 手册 `docs/testing-guards.md` §3 标题/表格/§3.5 同步为新口径（148 现行 + 48 存档），并把「换端口」命令与「不删只挪窝」的理由写清。
- **195→196 份浏览器探针全量盘点，并把「被文档点名却硬编码端口」的 63 份统一加上 BASE 覆盖（2026-10-07）**：演示站端口一旦不是 5199，这些回归/取证脚本就得逐个改文件才能跑。
  - 盘点口径全部可复现：逐个 `node --check`（**0 份语法损坏**）；class 有效性用新采集器 `demo/tools/probes/collect-rendered-classes.cjs` 拿**演示站 26 条路由真实渲染出的 class** 与源码词表求并集比对（**194/195 命中**，唯一过时项 `probe-table-editors.cjs` 的 `.el-picker__popper`，现契约 `.el-date-picker__dropdown`）；`/component` 路由的 3 份其实是访问 element-plus.org 的**外部基准**探针，不算失效。
  - 改造：`process.argv[2] || 'http://127.0.0.1:5199'`；三种既有写法分别处理 —— ① `const BASE='…:5199'` → 加 `argv` 兜底；② `const BASE='…:5199/table'`（BASE 自带路径）→ `(process.argv[2] || '…:5199') + '/table'`；③ 内联字面量 `'…:5199/x'` / 模板串 → 插入 BASE 声明并替换为 `BASE + '/x'` / `` `${BASE}/x` ``。共改 **76 份**，其余 120 份一字未动。
  - 安全：改前整目录备份；**每份改完立即 `node --check`，不过就当场还原** —— 第一轮正是这道门禁抓出 14 份会因"插入第二个 BASE 声明 → 重复声明语法错"（那些文件的 BASE 字面量里带路径），第二轮按形状②修正后 0 失败。
  - 结果：`被文档点名 && 无 BASE 覆盖` 从 **63 → 0**；真·硬编码从 133 份降到 **53 份**，且这 53 份全部是未被文档点名的一次性诊断脚本（按 §3.5 规则保留不动）。实跑验证：`verify-slider.cjs`（形状②改造）默认端口 **22/22 PASS**、显式传 `http://localhost:5199`（换 host 拼写，证明参数真被使用）仍 **22/22 PASS**；`verify-descriptions-border.cjs` 显式传 BASE **7/7 PASS**。手册 §3.1/§3.5 数字同步更新（含"表格单元格里放不下含竖线的 grep 命令，挪到 §3.5"这类细节）。

- **全量 bUnit 不再靠运气：68 处等待调用统一显式超时 + 「等待必须写 timeout」守卫（2026-10-07）**。bUnit 的 `WaitForAssertion` / `WaitForState` 不写 `timeout:` 时默认只等 **1 秒**，而全量套件是并行跑的 —— CPU 争抢时 1 秒不够，表现就是「单类复跑必过、全量偶发红」，本仓为此白记过一轮"flaky 12↔13 属抖动"的噪音，真缺陷反倒被淹掉。
  - 新增 `test/Element.ComponentTests/TestWait.cs`：`Timeout = 20s` + `IntervalMilliseconds = 25`，注释写明来由。
  - 脚本化补齐 **68 处调用 / 18 个测试文件**（`.WaitForAssertion(` / `.WaitForState(` / `.WaitForElement(` 一律追加 `timeout: TestWait.Timeout`）。脚本按**字符串/字符/逐字串/注释感知**做括号配平（实参里有 `"外部引用改的标题"` 这类中文文本），并逐处复核多行 lambda 的收尾位置正确；补齐后全仓「缺 timeout」计数归 0。
  - `ElAutocompleteTests.DebouncesRemoteSuggestions` 改掉一条真正不稳定的断言：原来 `Input → await Task.Delay(25) → Assert.Empty(calls)` 是**拿墙钟赌调度**（一次停顿超过剩余 50ms，防抖定时器就已跑完 → 假失败）。改成输入语句之后**立刻**断言（防抖是挂定时器，同步路径绝不会 fetch），零时间赌注；「防抖坏掉＝每个按键都发请求」这类回归仍被后面的 `calls == ["ab"]` 精确抓住。`ElFormTests` 里的 `Task.Delay(1)` 是异步校验桩、`ElButtonDebounceTests` 的 200ms 是刻意的静默期观察，均非计时断言，保持不动。
  - 新增 `TimingGuardTests`（**5 例**）守住这条规则：实参未写 `timeout` 的 `WaitFor*` 调用一律判违规；夹具两侧都锁（裸调用必须抓到 / 写了超时的不许报 / **注释里存档的 `WaitFor` 不许报**）；另有 `Guard_ScannerSeesTheWaitCalls` 要求全工程识别到 ≥60 处 WaitFor 调用，防"扫描器退化 → 0 命中假绿"。
  - 🔧 过程失误照记：`TimingGuard` 第一版直接在**原文**上跑正则，`/* cut.WaitForAssertion(...); */` 这种注释存档会被误报 —— 是夹具当场抓出来的（本仓早些时候三条兼容性守卫误报就是同一个病根）。补了「注释与字符串内容抹成等长空格、偏移与行号不变」的掩码步骤后修正。
  - **回归**：全量套件**连跑三次** `2143 通过 / 0 失败`（run1/run2/run3 全部 rc=0），总数从 2138 增至 2143（+5 例守卫）。等待抖动源清零后，基线出现的任何红都是真缺陷。


### Added

- **全库「手写渲染树序列号」守卫测试（2026-10-07）**：`test/Element.ComponentTests/RenderTreeSequenceScanner.cs` + `RenderTreeSequenceGuardTests`（8 例），把本轮 `ElButton` / `ElTreeSelect` 两个真实事故固化成机器规则，防止后人再写一遍。
  - **R1 序列漂移**：某段手写渲染树用 `seq++` 连续编号，且**引用捕获帧之前存在条件帧**（写在 `if`/`else`/`switch` 里的帧发射）→ 条件帧增减会让捕获帧序列漂移 → `NotImplementedException: Unexpected frame type during RemoveOldFrame: ElementReferenceCapture`。
  - **R2 捕获后加属性**：同一元素内属性帧出现在引用捕获帧之后 → `InvalidOperationException: Attributes may only be added immediately after frames of type Element or Component`。
  - 扫描器**先剥注释与字符串/字符/逐字串内容**再判定（本仓刚因为「守卫拿原文断言」误报过三条兼容性守卫）；区域按花括号深度切方法体，元素嵌套用栈，所以「捕获之后另开子元素再写属性」这种**合法**写法（`ElDateTimePicker.razor.cs` 就是）不会误报；`OpenComponent<T>(...)` 带泛型实参的调用**必须**识别成开元素，否则元素栈为空、R2 永远抓不到（原型阶段真漏过一次）。
  - 夹具两侧都锁：R1/R2 各有「必须抓到」与「不许误报」样本，外加「整段坏代码被注释存档 / 写在字符串里」不许触发；另有一条 `Guard_ScannerActuallySeesBuilderCode`（要求全仓 >400 个源文件、≥10 个含 `builder.` 发射的文件被识别）—— 防止扫描器自身退化导致「0 违规」变成假绿。
  - **现状**：全仓 562 个 `src/**/*.cs` 扫描结果 **0 违规**（`ElButton`/`ElTreeSelect` 修复后自证干净；原型阶段用修复前的备份文件验过 R1/R2 均能命中）。`.razor` 由 Razor 编译器生成固定序列常量，天然安全，不在扫描范围内。
  - 全量 bUnit：**2138 通过 / 0 失败（总 2138）**。


### Fixed

- **生成式 `ElMention` 选不中候选：又是测试竞态，组件无错（2026-10-07）** —— 全量套件最后一次清零。`ElFormGeneratedControlTests.GeneratedMentionSelectsOptionAndUpdatesModel` 原先报 `ElementNotFoundException: .el-select-dropdown__item`。
  - 诊断方式：临时在同一个测试类里插一个反射探针（打印 `dropdownVisible` / `activeMentionStart` / `activeMentionEnd` / 实例数 / 各步 items 数），拿到硬证据而不是猜。结论：`instances=1 textareas=1`（排除"两个实例被搞混"）；`Input("@a")` 之后 **items=0**，等到防抖到期后 items=1 且状态完全正确（`start=0 end=2 prefix=@`）；点击确实生效，`model=@alice`。⇒ 原测试只是**在防抖刷新之前就点了**。
  - 还有一个更阴的坑（同一轮里被我自己踩到）：防抖到点会**再渲染一次**，跨批次拿到的元素其 `onclick` handler id 可能已被回收 —— 用缓存的 `items[0].Click()` 直接抛 `UnknownEventHandlerIdException`，而"重新 Find 再 Click"在两次运行里一次成功一次静默无效。稳定写法是把 Find 与 Click 放进**同一个** `cut.InvokeAsync(() => cut.Find(...).Click())` 批次，并用 `WaitForState` 轮询而不是固定 `Task.Delay`。
  - 配套补齐一个真正的**表面缺失**：`ElMention.Debounce` 是公开参数，但 `MentionAttribute`（生成式表单的属性层）没有暴露它，宿主无法关掉防抖。新增 `MentionAttribute.Debounce`（默认 300，与组件一致 ⇒ 现有行为一字不变）并在 `FormControlRender.RenderMention` 里透传；新增用例 `GeneratedMentionDebounceZeroRendersOptionsSynchronously`（`Debounce = 0` 时候选**同步**出现，刻意不用 WaitFor，将来谁把 0 也吞进计时器就会立刻红）。
  - 🔧 **一次自我纠正（记下来免得重演）**：中途我以为挖到一个真缺陷 —— `DebouncedRefreshSearchAsync` 的 `Task.Delay` 续跑在事件处理器之外，只改状态不请求重渲染，于是在 `RefreshSearchAsync()` 之后补了 `await InvokeAsync(StateHasChanged)`。做了**反证实验**（把那行注掉重跑诊断）：候选照样在防抖到期后出现、点击照样回写模型 ⇒ 那行属于多余改动，**已回退**，`src/Components/ElMention.razor.cs` 与今日基线备份逐字节一致（`filecmp` 全仓核对确认）。原则：没有"去掉它就失败"的证据，就不进源码。
  - **回归**：全量 bUnit **2130 通过 / 0 失败 / 0 跳过（总 2130）** —— 本会话起点是 2105 通过 / 12 失败，首次清零。今日源码侧改动共 7 个文件（`ElButton.razor.cs`、`ElTreeSelect.cs`、`MentionAttribute.cs`、`FormControlRender.cs`、`wwwroot/css/index.css`、`ElXMentionSender.razor(.cs)`），另有 3 个新测试文件与 6 个测试文件调整，全部与 `E:\电子病历\_backup_before_repoCleanup_20261007` 对比核实，无越界改动。

### Added

- **`test/Element.ComponentTests/TestJsScanner.cs`：引号感知的注释剥离器，收拢三处重复实现（2026-10-07）**。仓库里已有两份一模一样的 `StripComments`（`ElHtmlEditorGrowTests` / `ElViewerGrowTests`），但它们是**按行找第一个 `//` 就截断**的朴素实现：一旦字符串里出现 `"https://…"`，那一行之后的**真代码会被整段吞掉** —— 守卫不是误报，而是**假绿**（真违规看不见）。新实现按状态机走：单/双引号字符串**原样保留**（正向断言如 `Contains("\"inserttable\"")` 仍然成立）并屏蔽其中的 `//`、`/*`；反引号**本身保留**（模板字符串正是要抓的东西）但丢掉其内容；块注释整段剥掉但补回换行以维持行号；整行 `///` 文档注释照旧丢弃。`ElHtmlEditorGrowTests` / `ElViewerGrowTests` 的私有 `StripComments` 现在只是一层转发。
- **`TestJsScannerTests`（6 例）：给"守卫的守卫"上锁（2026-10-07）**。剥注释这件事本身如果被改坏，会让所有兼容性守卫静默失效，所以断言分两侧：该剥的必须剥掉（注释里的 `=>`、反引号、`Radzen` 字样、`scrollHeight` 说明），该留的必须留下（代码里的 `=>` 仍被抓到、反引号本身不被吞、字符串与 URL 完整、`document.execCommand` / `window.getSelection` / `XMLHttpRequest` / `"inserttable"` 在真实 `dom.js` 剥离后全部还在、剥离后长度 > 2000 防"整段剥没"），并额外断言行数不变（行号不漂）。

### Fixed

- **`ElTableV2` 展开箭头：又是测试拿错契约类名（2026-10-07，组件无错）**：`ElDataDisplayRemainingTests.TableV2VirtualizesExpandsAndSummarizesRows` 报 `ElementNotFoundException: .el-table__expand-icon`。取证：Element Plus `theme-chalk/src/table-v2.scss` 里展开图标是
  `.el-table-v2 .el-table-v2__row .el-table-v2__expand-icon` 与 `.el-table-v2__expand-icon.is-expanded svg{transform:rotate(90deg)}`（`el-table-v2__` 前缀），
  `el-table__expand-icon` / `el-table__expand-icon--expanded` 是**旧版 `el-table`** 的契约，两代 Table 的类名不可混用；本仓 `index.css`（`.el-table-v2 .el-table-v2__expand-icon` + `.is-expanded{transform:rotate(90deg)}`）与 `docs/table-v2-alignment.md` 的 DOM 树（`span.el-table-v2__expand-icon(.is-expanded)`）也都是 v2 契约 ⇒ 组件与 CSS 一直正确，测试写歪。
  - 修法：断言改到 `.el-table-v2__expand-icon` + `is-expanded`，并**补上"再点一次必须收回去"的反向断言**（原用例只测展开、不测折叠，展开态不可逆这种回归根本抓不住）。
  - **回归**：`ElDataDisplayRemainingTests` **5/5 通过**；新增浏览器探针 `demo/tools/probes/verify-tablev2-expand.cjs`（`/tablev2`「展开行」节）→ **9/9 PASS、0 JS 异常**：10 个箭头全部用 v2 契约类名且**旧版类名 0 命中**、点击后 `is-expanded` + 展开行插入（实测文本「药品-001」详情：库存 7…）、箭头计算后 `transform` 由 `none` 变 `matrix(0, 1, -1, 0, 0, 0)`（即 rotate(90deg)）、再点收回、树形节同样用 v2 契约。

- **`ElDescriptions` 的 `is-bordered` 断言挂错元素（2026-10-07，测试侧缺陷，组件无错）**：`ElDataDisplayTests.DescriptionsRegistersItemsAndRendersBorderedRows` 长期红，报 `Collection: ["el-descriptions"], Not found: "is-bordered"`。取证结论是**测试写错、组件一直是对的**：
  - 权威依据：Element Plus `descriptions/src/description.vue` 里根节点是 `:class="[ns.b(), ns.m(size)]"`，`is-bordered` 挂在表格上 —— `<table :class="[ns.e('table'), ns.is('bordered', border)]">`；`theme-chalk` 对应规则同样是 `.el-descriptions__table.is-bordered .el-descriptions__cell{…}`。
  - 本仓一致：`ElDescriptions.razor.cs` 把类加在 `TableClass` 上，统一的 `index.css` 里四条边框规则也都是 `.el-descriptions__table.is-bordered …`；专用用例 `ElDescriptionsTests.BorderRendersBorderedTableCells` 早就是按表格断言并且一直绿 —— 说明红的那条是重复且写歪的冒烟用例。
  - 修法：断言改到 `.el-descriptions__table`，并**补一条反向断言**锁死契约 —— 根节点 `.el-descriptions` **不得**出现 `is-bordered`（防止后人"顺手"把类加到根上，那会让 EP 的 CSS 完全不命中、边框静默消失）。断言处留了权威依据注释。
  - **回归**：`ElDataDisplayTests` + `ElDescriptionsTests` **25/25 通过**；新增浏览器探针 `demo/tools/probes/verify-descriptions-border.cjs`（`/descriptions` 14 张表，按 水平·无边框 / 水平·有边框 / 垂直 三类分别断言）→ **7/7 PASS、0 JS 异常**：有 Border 的表格单元格四边计算后各 `1px`、无 Border 的全 `0`、水平无边框是 `td.el-descriptions__cell` 内含 label/content span 且无 `<th>`、垂直模式 `th.is-vertical-label` 与 `td.is-vertical-content` 数量严格一一对应。
  - 全量套件：**2126 通过 / 3 失败（总 2129）**；其中 `ElFeedbackTests.MessageBoxPromptCreatesInputAndBeforeCloseCanBlock` 单类复跑 **6/6 通过**（并行下 bUnit `WaitFor` 默认 1s 超时，既有 flaky）⇒ 真实待修只剩 `ElTableV2` 树展开图标与生成式 `ElMention` 弹层 2 例。

- **三条兼容性守卫长期误报，把真缺陷埋在噪音里（2026-10-07）**：`ElHtmlEditorGrowTests.JsChannel_IsWrittenInEs5Syntax`、`ElHtmlEditorGrowTests.JsChannel_UsesOnlyBrowserBuiltIns`、`ElViewerGrowTests.Guard_OfdViewerChannel_IsEs5Safe` 自建立以来一直是红的，原因都不是代码违规，而是**拿原文做断言**：通道段的中文注释习惯用反引号包裹标识号（`` `scrollHeight-clientHeight` ``）→ 被当成模板字符串；注释里写「对齐 Radzen 的 AutoFocusFirst…」→ 被当成引用第三方库。三例现在统一走 `StripComments` 只查代码。
  - 顺带修掉更危险的反方向问题：旧剥离器会把 `"https://…"` 之后的真代码当成注释吃掉（守卫假绿），见上方 `TestJsScanner` 条目。
  - **反向验证守卫仍有牙齿**（避免"改到永远绿的守卫"这种假修复）：往 `dom.js` 的该通道段真插入一行 ES6 代码 `var x = function (a) { return (b) => b + a; };` → `JsChannel_IsWrittenInEs5Syntax` **立刻变红**；删除后复原（`dom.js` 字节数回到 167020、探针标记 0 命中），守卫重新全绿。守卫的两种失败模式（误报 / 假绿）因此都被关闭。
  - **回归**：`ElHtmlEditorGrowTests` + `ElViewerGrowTests` + `TestJsScannerTests` 三类 **403/403 全绿**；全量套件 **2125 通过 / 4 失败（总 2129）**，其中 `ElAutocompleteTests.DebouncesRemoteSuggestions` 单类复跑 **6/6 通过**（xUnit 并行下 bUnit `WaitFor` 默认 1s 超时，属既有 flaky，非本次引入）⇒ 真实待修只剩 3 例：`ElTableV2` 展开图标、`ElDescriptions` 的 `is-bordered`、生成式 `ElMention` 弹层条目。


- **🔥 `ElButton` 进入 loading 态会让整页渲染崩掉（2026-10-07，库级严重缺陷）**：任何宿主里只要把一个 `ElButton` 的 `Loading`（或 `Icon` / `IconContent` / `IconPosition`）在运行期改变，Blazor diff 就抛 `System.NotImplementedException: Unexpected frame type during RemoveOldFrame: ElementReferenceCapture`，该组件所在渲染批次整体失败。
  - 根因：`ElButton.ButtonElement` / `BuildButtonContent` / `RenderIcon` 是**手写渲染树**，却用 `var seq = 0; seq++` **连续**分配序列号，而其中有多枚**条件帧**（`aria-busy`、`autofocus`、`tabindex`、`<a>` 的 href/target/rel、图标 `<i>`、Loading 内容、文字 span）。任一条件帧增减都会把它之后所有帧的序列号整体平移，于是末帧 `AddElementReferenceCapture` 与旧树里同序列的「异类型帧」相撞 —— `RenderTreeDiffBuilder.RemoveOldFrame` 不处理 `ElementReferenceCapture`，直接 `NotImplementedException`。
  - 定性过程：用最小复现矩阵逐个隔离（`Loading` 变化 / `Escape` / 同值重渲染 / `Disabled` 变化 / `Icon` 出现 / 裸 `ElButton`）→ **裸 `ElButton` 只切一次 `Loading` 就必崩**，与 `ElXSender` 无关；只改 `Disabled`（不增减帧数）不崩 → 证实根因是帧数漂移而非别的状态耦合。
  - 修复：给每个帧位分配**固定槽位常量**（`SeqRoot`…`SeqOnClick` 0–16、左侧图标 20–27、文字 40–43、右侧图标 60–67、`SeqRefCapture` 90），可选帧跳过时不再占用后续编号；`RenderIcon` 改收 `baseSeq`，左右图标用互不重叠的区间，所以连 `IconPosition` 左右切换也不撞序列。
  - **回归**：新增 `test/Element.ComponentTests/ElButtonRenderTreeDiffTests.cs` **6 例**（loading 切换、图标出现、图标位置互换、仅改 Disabled、`ButtonReference` 在切换后仍有效、`ElXSender` 原始报障现场），除首例外其余在修复前均无法通过；浏览器端 `demo/tools/probes/verify-button-loading-diff.cjs`（Playwright + msedge，打 `/x`）实测 **8/8 PASS、0 JS 异常** —— loading 窗口内 `is-loading` + 停止按钮 + `aria-busy` 齐备，600 ms 后帧数回落、气泡列表继续增长、连续多轮进出 loading 电路仍存活。
  - 附带：`ElXComponentTests` 另有 2 例（`MentionSenderAppliesSelectedCommandAndSubmitsShortcut` / `MentionSenderClosesCommandLoopAndSupportsStopClearEscape`）报 NRE，真因是 `ElMention` 的候选刷新默认 `Debounce = 300`，测试敲完关键字立刻按 Enter 时候选还没刷新，与产品行为无关（仓库内 `ElMentionTests` 一直显式传 `Debounce, 0` 正是这个约定）。见下方 `ElXMentionSender.Debounce` 条目。

### Added

- **`ElXMentionSender.Debounce`：把内层 `ElMention` 的候选刷新防抖透出来（2026-10-07）**。此前 `ElXMentionSender` 只固定吃 `ElMention` 的默认 300 ms，宿主与测试都无法关掉：输入 `/s` 后立刻按 Enter 会因候选未刷新而选不中命令（表现为"斜杠命令按键无反应"）。现在 `[Parameter] int Debounce { get; set; } = 300;` 原样透传给 `ElMention`，默认行为一字不变，即时联想/测试场景传 `0` 即同步刷新（`ElXComponentTests` 的 2 例已按此补齐）。

### Fixed

- **`ElTreeSelect` 一用声明式树内容（`TreeContent`）就抛异常（2026-10-07）**：`<ElTreeSelect TreeContent="...">` 打开下拉即抛 `System.InvalidOperationException: Attributes may only be added immediately after frames of type Element or Component`；而 `Data` 绑定的用法完全正常 —— 只有声明式路径必崩。
  - 根因（`src/Components/ElTreeSelect.cs:144`）：`BuildTreeContent` 把 `ChildContent` 的 `AddAttribute` 写在了 `AddComponentReferenceCapture` **之后**。`RenderTreeBuilder` 的帧序约束是「`OpenComponent` 之后只能紧接一串 `AddAttribute`」，一旦插入 `ComponentReferenceCapture` 帧就再不能追加属性。而那句属性下发恰好被 `if (childContent != null)` 包着，只有传了 `TreeContent` 才会执行，所以故障面精确等于「声明式树内容」。
  - 修复：把 `ChildContent` 属性移到引用捕获**之前**（全部属性 → 再 capture），并在原地写下这条帧序约束的注释。顺手全仓扫同类写法：`ElDateTimePicker.razor.cs:622` 的 `AddElementReferenceCapture` 之后紧跟的是新 `OpenElement` 的属性，合法，不改。
  - **回归**：bUnit `ElTreeSelectTests` 单类 **7/7 通过**（原 6/7，唯一红的就是 `TreeSelectSupportsDeclarativeTreeContent`）；全量套件由基线 **失败 12 / 通过 2105** 变成 **失败 9 / 通过 2108**（总 2117 不变，减少的 3 例 = 本条 1 例 + 下条 2 例 CSS 守卫）。

- **`index.css` 全站清掉 flex `gap`，改为 margin / padding 等效写法（2026-10-07）**：`gap` 需要 Chrome 84+，与本仓兼容性守卫声明的下限（Chromium 75 级内核）冲突，共 13 处全部改写：`.el-input-tag`、`.el-input-otp`、`.el-slider--with-input`、`.el-select.is-multiple .el-select__tags`、`.el-segmented`（含 `is-vertical` 纵轴变体）、`.el-tour__footer`、`.el-tour__actions`、`.el-anchor--horizontal > .el-anchor__list`、`.el-page-header`、`.demo-feedback__row`、`.el-dropdown-table-grow .el-select__tags` 及其内层 `.el-tag`、`.el-dialog-grow__confirm-buttons`。
  - 手法分三类：① 不换行的容器 → `> * + * { margin-left: N }`；② 会换行的标签容器 → 「子项半间距 `margin: N/2` + 容器 `padding`/负 `margin` 吸收」，外沿尺寸与首项落点都不变；③ 只有行间距的 `.el-page-header`（`gap:12px 0`）→ 给「整行块（`__main` / `__breadcrumb`）之后的兄弟」补 `margin-top`，单行时零影响，与 `row-gap` 语义一致。
  - 🔥 两个必踩的坑（写测试/改样式都要记住）：**子项自带 margin 时替代值必须写合计** —— EP 的 `.el-button + .el-button{margin-left:12px}` 与 gap 是叠加的，所以 `ElTour` 动作区要写 `20px`（8+12）、`ElDialogGrow` 确认按钮区要写 `24px`（12+12），照抄 gap 值会少一截；**同特异度且更靠后的复位规则会赢** —— 下拉表格标签的关闭图标有 `.el-dropdown-table-grow .el-select__tags > .el-tag .el-tag__close.el-icon-close{margin:0}`（5 个类、位置在后），替代规则必须凑到 6 个类（`… > .el-select__tags-text + .el-tag__close.el-icon-close`）才压得住。
  - **回归（逐像素）**：新增 `demo/tools/probes/gap-snapshot.cjs`（Playwright + msedge，12 个容器全部走真实交互态：多选选中 8 项逼出换行、打开引导弹层、服务化确认框、下拉表格勾选多选标签），改造前后对比子项相对坐标与尺寸 → **12/12 完全一致**；只有两个「无背景、无边框、`pointer-events:none`」的标签容器自身盒子尺寸差 ±4px（子项位置不变，肉眼无差异）。另用 `verify-gap-margin-equivalence.cjs` 在同一页面会话内「注入等价规则 + gap 置 normal」预演过一遍，`.el-slider--with-input` 的 `padding-left/right:16 + margin-left:16` 写法就是据此确定的。
  - **兼容性守卫转绿**：`ElFormFieldGrowTests.Css_FormFieldSection_AvoidsUnsupportedCssFeatures`、`ElMarkdownGrowTests.CssUsesOnlyWidelySupportedFeatures` 两条 `(?<![-\w])gap\s*:` 断言由红转绿（此前失败原因是 `.el-dialog-grow__confirm-buttons` 的压缩行里真有 `gap:12px`，被「取段到文件末尾」的切片一并算进别人家段落）。

### Changed

- **演示站：机构名「青岛市即墨区第三人民医院」全部替换为「某某市人民医院」（2026-10-06）**。真实机构名不该出现在演示站/示例文档里，统一改为脱敏名称。
  - **`wwwroot/samples/office-sample.pptx`（二进制样本，也就是 `/grow` 里 PPT 预览那页显示的机构名）**：直接改写包内 `ppt/slides/slide1.xml`，`青岛市即墨区第三人民医院　内科门诊` → `某某市人民医院　内科门诊`；改写后重新校验包内已无旧名、其余条目字节不变。
  - `demo/tools/make-office-samples.py`：生成该 pptx 的脚本同步改名，避免下次重新生成样本又带出真实名称。
  - demo 页面统一为「某某市人民医院」：`Models/MockData.cs`（ElTree 组织树根节点）、`Pages/BasicDemo.razor`（水印 `Content` / `ContentList`）、`Pages/GrowDemo.razor`（公告跑马灯）、`Pages/WatermarkDemo.razor`（页头标题 + 水印文本/多行）。
  - 配套断言与产物同步：`demo/verify-detail.cjs`、`demo/verify-watermark.cjs` 的期望字符串，以及 `demo/detail-result.json`、`demo/office-result.json`、`demo/pickers-tree-result.json` 三个运行产物里残留的旧写法。
  - 组件测试里也带真实全称，一并脱敏：`ElQRCodeGrowTests.cs`（3 处）、`ElQRCodeGrowEncoderTests.cs`（1 处）。
  - **回归**：`ce_build/probe_hospital_rename.cjs`（Playwright + msedge 打真实站）**18/18 PASS、0 JS 异常** —— `/grow` 点「PPT 演示」后预览文本为 `门诊病历汇报 某某市人民医院 内科门诊 …`、`/watermark` 与 `/basic` 的水印 SVG data-URI 解码后含新名、`/data` 组织树根节点为新名、三个页面均无旧短名（`某某人民医院`）残留；静态样本经 HTTP 提供时字节数与磁盘一致（30801，已减小）。bUnit 二维码系列 **93/93 全绿**。
  - 全仓逐字节扫描（排除 `bin/` `obj/`，遍历所有文件类型、UTF-8 与 GBK 双编码尝试）：旧全称 **0 命中**、旧短名 **0 命中**。

### Added

- **`ElDrawerRef`：抽屉的级联引用（对齐 `ElDialogGrowRef`，2026-10-06）**。`ElDrawer` 通过 `CascadingValue` 把自己的引用下发给正文里的任意组件，让**抽屉里打开的表单可以反向操作这个抽屉**，补齐了「四个能力」里唯一缺的一项：
  ```razor
  @* 抽屉里的表单组件 *@
  [CascadingParameter] public ElDrawerRef Drawer { get; set; }
  Drawer.Title = $"编辑员工信息（第 {n} 次改标题）";   // 运行时改当前抽屉标题
  await Drawer.CloseAsync();                          // 走 before-close 校验后关闭
  ```
  - `ElDrawer` 新增 `[Parameter] ElDrawerRef DrawerRef`：调用方可自带实例，组件负责把「改标题 / 关闭」能力接到它上面（否则内容组件拿到的是一个什么都不做的空引用）。
  - 标题优先级：`internal EffectiveTitle => mutableTitle ?? Title`；级联引用改的是 `MutableTitle`（不覆盖 `[Parameter] Title`，避免父组件重渲染把标题打回去），**抽屉每次重新打开会把 `mutableTitle` 重置为 null**，回到 `Title` 参数值。
  - **另外三个能力本来就有**：`ShowClose`（默认 true，控制右上角 ×）、`CloseOnClickModal`（默认 true）、`CloseOnPressEscape`（默认 true）、`WithHeader`（默认 true，连标题栏一起去掉）、动态 `Title`（改参数即可）。本轮补的是 demo 覆盖与自动化用例。
  - **回归**：`ce_build/probe_drawer_splitter.cjs`（Playwright + msedge 打真实站）**22/22 PASS、0 JS 异常**；bUnit 新增 `ElDrawerTests`（此前仓库根本没有抽屉测试）**14 例全绿**，dialog 家族 **76/76**。

- **`ElSplitter` 分隔条对齐 Element Plus 官方渲染（2026-10-06）**：原来是一条 6px 实心底色的带子（`#ebeef5`），官方是「bar 不占布局宽度 + 2px 细线 + 16px 不可见拖拽热区」。
  - `index.css`：`.el-splitter__bar` 去掉 `background-color`、`flex: 0 0 6px` → `flex: 0 0 0`；新增 `.el-splitter__bar-dragger`（绝对定位 16px 热区，横 `ew-resize` / 纵 `ns-resize`），可见线是它的 `::before`（2px、`--el-border-color-light`）；`::after` 是 lazy 模式的半透明预览线，靠 `--el-splitter-bar-offset` 位移。
  - 折叠按钮改为官方 `opacity:0`，`.el-splitter__bar:hover` 才显形，扁平 `border-radius:2px` 无阴影。
  - `ElSplitterPanel` 补 `data-index` 与 `is-lazy` class；`ElSplitter` 在 `Lazy` 时把 `--el-splitter-bar-offset` 写到根元素内联 style。

- **`ElSelect` / `ElSelectV2` 下拉面板 `HeaderContent` / `FooterContent` 插槽（对齐 EP 2.5.2+ 的 `#header` / `#footer`，2026-10-07）**：内容固定在面板上下两端、不随选项列表滚动。`ElPopup` 在 `.el-scrollbar` 的兄弟节点渲染 `option.HeaderContent` / `option.FooterContent`；`ElSelect` / `ElSelectV2` 把同名 `[Parameter]` 透传到 `DropDownOption`。`ElTreeSelect` 新增 `[Parameter] Func<TreeItemBase,string,bool> FilterNodeMethod`（对齐官方 filter-node-method，null 时按 `Text` 不区分大小写包含匹配）。
  - demo：`SelectDemo` 新增「下拉面板头尾插槽」一节（header 放说明、footer 显示选项总数）；`TreeSelectDemo` 新增「可搜索（Filterable）」一节。

### Fixed

- **`ElSplitter` 拖拽只生效一部分（2026-10-06，实测拖 60px 面板只走 37px）**：两层原因叠加。
  1. **只改了当前面板**：相邻面板仍按原百分比（`65%`）占着不动，两块 `flex-basis` 之和超过容器 → 被 `flex-shrink` 按比例拉回（433.8 被压回 410.8，正好差 23px）。改为**同时改相邻两块**（当前 `+delta`、下一块 `-delta`，并按两块各自的 `min/max` 夹取），实测拖 60px 就是 60.2px。
     - 相应地 `JsSplitterMove` / `JsSplitterEnd` 增加 `nextSize` 参数，`.NET` 侧用 `ApplyNeighborSizeAsync` 只同步尺寸（不派 resize 事件、不进激活态），否则下一次 Blazor 重渲染会把相邻面板弹回原百分比。
  2. `dom.js` 的 `elementSplitterInit` 里 `JsSplitterStart` 传了 2 个参数而方法只接受 1 个 → `JsonException`（`Unexpected JSON token Number`）。`send` 改为变参，按回调实际签名传参。

### Fixed

- **`ElDialogGrow` 缩放到一半松手会把整个对话框关掉（2026-10-06）**：`elementDialogEnableResize` 少了「松手吞掉那次 click」的处理（同一文件里的 `elementDialogEnableDrag` 有，缩放分支漏了）。缩放时指针常拖到对话框之外、在遮罩上松手，此时 `mousedown`（手柄）与 `mouseup`（遮罩）的最近公共祖先是 `.el-dialog__wrapper`，浏览器会把这次 click 派发到 wrapper → 命中「点击遮罩关闭」→ 对话框被误关。
  - `wwwroot/js/dom.js`：`elementDialogEnableResize` 重写为与拖拽同一套状态机（1px 死区 `moved` 标记 + `swallowClick` 在**捕获阶段**吞掉紧随松手的那一次 click，0ms 后自动解绑），并让 `elementDialogDisableResize` 一并清理 document 上的残留监听（缩放中途销毁不再留野指针）。缩放过程改为**直接写 DOM 的 width/height 跟手**、约 80ms 节流回传 `JsResize`。
- **`ElDialogGrow` 的缩放尺寸会在父组件重渲染后被打回原值（2026-10-06）**：`JsResize` 之前直接覆盖 `[Parameter] Width` / `Height`，渲染器把它当成「父组件未更新」；此后任何引起父组件重渲染的操作（典型：内容组件改标题 → 服务化宿主刷新、或选项变更走 `NotifyChanged`）都会用参数值把它重新赋回去 → 缩放白做。改为写 internal 可变字段 `MutableWidth` / `MutableHeight`（`EffectiveWidth => mutableWidth ?? Width`，优先级高于参数），缩放结果可稳定保留到对话框关闭。
  - 同时 `NotifyOpeningAsync` 里重置 `mutableWidth/Height` 与拖拽位移 —— 同一个组件实例第二次打开不再带着上一次缩放 / 拖拽的结果。
  - **回归**：`ce_build/probe_grow_close_resize.cjs`（Playwright + msedge 打真实站）**16/16 PASS、0 JS 异常** —— `ShowClose=false` 不渲染 ×、`CloseOnClickModal=false` 点遮罩不关、声明式改 `Title` 立即生效、**服务化 form 内 `Dialog.Title = …` 生效**、缩放到对话框外松手后对话框仍在（`stillOpen=1`）、尺寸 520×260 → 1004×434、**缩放后再改标题尺寸不被重置**。
  - bUnit `ElDialogGrowEnhancedTests` 新增 1 例（`ResizedSizeSurvivesLaterRerender`：缩放后父组件重渲染、参数仍是旧值，尺寸保持），dialog / drawer 系列 **62/62 全绿**（原 61 + 1）。

- **`ElSelect` `Filterable` 触发搜索框打字后选项不收缩（2026-10-07）**：下拉选项由 `ElPopup` 宿主渲染、其入参在关键字变化时不变，Blazor 会跳过重渲染，导致 `IsOptionVisible`（`display:none`）刷不到 DOM、输入关键字选项不收缩。新增 `FilterChanged` 事件，`ElOption` 订阅后在关键字变化时自行 `StateHasChanged`；`UpdateValueAsync` 设关键字后额外 `dropDownOption.RequestRender?.Invoke()` 让弹层（含「无匹配数据」空态）重渲染。修复前输入「西」仍显示全部 5 项，修复后仅 1 项可见且下拉保持打开。
  - **回归**：`ce_build/probe_select_video.cjs`（Playwright + msedge 打真实站）选择器相关 **8/8 PASS、0 JS 异常**（video 控制条自动隐藏因沙箱无法加载外网示例视频环境跳过，逻辑已代码核对）；bUnit 新增 `ElSelectTests.FilterableSelectHidesNonMatchingItemsInDom` / `DropdownCarriesHeaderAndFooterContent`、`ElTreeSelectTests.TreeSelectFilterableFiltersTreeNodes` 全绿。

- **`ElVideoPlayerGrow` 全屏按钮点了不最大化（2026-10-07）**：全屏时容器仍带着 `Width` / `Height` 内联样式，`CSS .is-fullscreen{width:100vw;height:100vh}` 被覆盖 → 视频高度不变。改为 `ContainerStyle` / `BodyStyle` 在 `_isFullscreen` 或参数为空时返回 null，交给 CSS 接管。新增 `[Parameter] bool AutoHideControls`（默认 false）+ `int ControlsHideDelay`（ms，默认 3000）：`elvideoplayergrow.js` 的 `autoHide` 用纯前端 class 切换（`is-controls-hidden` + `cursor:none`），鼠标移动即显示、静止超时隐藏、暂停时保持可见、速率菜单打开时阻止隐藏。
  - **回归**：`ce_build/probe_select_video.cjs` 全屏一节 **PASS**（点击前 910×280 → 点击后 1280×800，`is-fullscreen=true`）。

- **`ElInput` 表单字段键入后，父组件重渲染会把值回写为初始值（2026-10-07，已修）**：抽屉/对话框里放 `ElForm` + `ElInput`，键入后只要发生一次父级重渲染（抽屉里点「改标题」、或提交触发页面重渲染），输入框与模型都会变回初始值。实测 `/drawer` 与 `/dialog`（未改动的对话框版同样复现）。
  - 根因：`ElInput.OnParametersSet` 在 `FormItem.OriginValueHasRendered` 为 **true** 的**后续重渲染**分支里写了 `Value = Value ?? FormItem.OriginValue` —— 把当前值错填成初始值（CHANGELOG 原先记的「在 false 分支」描述不准确，实际在 true 分支）。其余约 30 个字段组件（ElSelect / ElSwitch / ElCheckbox / ElDatePicker …）的同名分支只是 `return` 早返回、**从不**用 `OriginValue` 回填，因此只有 `ElInput` 有此 bug。
  - 修复：`OnParametersSet` 后续重渲染分支新增 `_lastParamValue` 记住「上一次实际收到的父级 `Value` 参数」——只有 `Value` 相对上一次参数**确实发生变化**（真正的外部赋值 / 重置）时才把它同步进 `FormItem.Value`；其余情况（本组件/表单内部自行触发的重渲染，父级 `Value` 尚未经 `@bind-Value` 的 `ValueChanged` 追上、仍是旧值）一律保留 `FormItem.Value`（当前已输入的值）。经浏览器端控制台埋点验证，真正的触发链是：`OnChangeEventArgs` 末尾 `StateHasChanged()` 在 `ValueChanged` 把「李四」写回父模型**之前**就触发了一次重渲染，这次重渲染拿到的父级 `Value` 仍是旧值「张三」，旧分支据此把 `FormItem.Value` 与模型覆盖回「张三」；新分支靠 `_lastParamValue` 识别出这是陈旧滞后、不再覆盖。改动只在 `ElInput.razor.cs`，不碰 `ElementFieldComponentBase` 基类，风险面最小。
  - **回归**：bUnit 新增 `ElFormTests.FieldRecreatedAfterParentRerenderKeepsCurrentValueNotOrigin`（键入「李四」→ 父级重渲染重建字段实例 → 重建后输入框显示当前值「李四」、不回滚初始值「张三」；无修复时必失败，是有效护栏，已 **PASS**）；浏览器端到端 `probe_form_realinput.cjs`（真实键盘逐字输入，非 `fill` 假象）`A/B/C` 三项 **3/3 PASS**（键入「李四」后点「改抽屉标题」做父级重渲染，输入框保持「李四」不再回退成「张三」），`probe_form_model2.cjs` 提交后模型值 = `李四`；`ElFormTests` 类 **10/10 PASS**；全量 bUnit 套件 2104 通过，失败数与基线 12 个既有失败持平（均为 ES5/CSS/JS 语法与若干无关组件测试，浮动 12↔13 属 flaky，无一例与表单值同步链路相关）。

### Removed

- **演示站：移除侧栏「基础组件 / 表单组件 / 数据展示组件 / 导航组件 / 反馈组件 / 自研 · Grow 组件 / AI · X 组件」7 个自动生成分组，并删除配套的 `/c/{组件名}`、`/cat/{分类}` 两条自动画廊路由**。这 7 个分组由 `ComponentCatalog` 反射扫描 `Element` / `Element.X` 程序集后按分类自动生成，指向 `AllComponents.razor` 的 `@page "/c/{Name}"`，与手写的「分类演示（Mock 数据）」重复、入口价值低。
  - `Shared/DemoNavigation.cs`：删除 `Categories` / `CategoryIcons` / `CategoryOrder` / `BuildCategories()` / `CategoryRank()`；`All` 只保留 `Fixed + MockSection.Entries`（搜索候选随之只剩真实示例页）；`TitleOf` 去掉已失效的 `/c/` 前缀特判。
  - `Components/SidebarMenu.razor`：删除遍历 `DemoNavigation.Categories` 的 `<ElSubMenu>` 循环。
  - `Pages/AllComponents.razor`：移除 `@page "/cat/{Category}"` 与 `@page "/c/{Name}"` 及对应的 `Category` / `Name` 参数与分支，页面简化为纯「全部组件」画廊；**`/all` 保留**（首页「浏览全部组件」入口仍在）。
  - `Pages/Home.razor`：快速说明删掉「可点击左侧任一组件名进入单组件页」。
  - **回归**：`ce_build/probe_demo_nav_clean.cjs`（Playwright + msedge 打真实站）**7/7 PASS、RESULT_OK=true、0 JS 异常**——侧栏子菜单只剩「分类演示（Mock 数据）」、`.el-menu-item` 实测 90 条（3 固定 + 87 分类演示，一项未丢）、`/c/ElButton` 与 `/cat/基础组件` 均落到「页面未找到」、`/all`（9 张卡片）与 `/button` 示例页正常。

### Added

- **`ElDialog` / `ElDialogGrow` 新增 `ContentClass` / `ContentStyle`：正文（ChildContent）相对标题的位置与间距可自定义（2026-10-06）**。正文与标题之间的距离完全来自 EP 原生 `.el-dialog__body{padding:30px 20px}`（上下各 30px）与 `p` 自身 `1em` 外边距，实测标题底到正文顶 **44px**（居中对话框体高 161px = header 54 + body 107）。EP 未提供覆盖入口，此前只能改全局 CSS（违反「CSS 单一真源」），现两个组件各补透传参数：
  - `ContentClass`：追加到 `.el-dialog__body` 的类名（`ElDialogGrow` 原有，`ElDialog` 新增）。
  - `ContentStyle`：正文容器内联样式。`ContentStyle="padding:0 20px 20px"` → 上内边距归零、正文紧贴标题；`ContentStyle="min-height:220px;display:flex;align-items:center;justify-content:center"` → 正文在正文区内垂直居中。两者均为**增强**（非 EP API），默认行为零变化。
  - 未设置 `ContentStyle` 时**不渲染 `style` 属性**，保持 DOM 干净、便于排查内联样式。
  - **回归**：`ce_build/probe_dialog_content_pos.cjs`（Playwright + msedge 打真实站）**10/10 PASS、0 JS 异常**——默认 `padding-top:30px` / 间距 44px → 紧凑版实测 `0px` / **14px**（子元素紧贴标题）→ 居中版正文区实测高 **220px**、正文中心与正文区中心重合（477 == 477）。bUnit `ElDialogTests` 新增 2 例（透传断言 + 未设置时不渲染 style），**56/56 全绿**（原 54 + 2）。

- **新增自渲染组件 `ElVideoPlayerGrow`（零第三方播放器依赖，按 Element Plus 观感渲染）**：参照 `teek-design-vue3` 的 `video-player` 思路，但**不用 xgplayer**，全部用原生 `<video>` + 自研控制条实现。能力：① 网络地址（`Src` 单源 / `Sources` 多源回退）；② 流媒体（HLS `.m3u8` 走浏览器原生播放，Safari / Chrome 134+ 直接播；DASH `.mpd` / FLV `.flv` 需外部 MSE 库，本组件零依赖仅做能力探测与友好提示）；③ 多格式（mp4 / webm / ogg / mov / m4v / avi / wmv / ts / mkv / mpd 等，按扩展名自动推断 MIME）；④ 自定义控制条（播放/暂停、可拖拽进度、时间、音量、倍速菜单、画中画、全屏）、错误/加载覆盖层、播放/暂停/结束/时间更新/错误事件回调。交互逻辑与媒体状态全部经 `wwwroot/js/elvideoplayergrow.js` 完成；样式追加到 `wwwroot/css/index.css`（`/* ==== ElVideoPlayerGrow ==== */` 段）。演示页 `/video-player-grow`（`Pages/VideoPlayerGrowDemo.razor`，含网络地址 / 多格式 / HLS 流 / 事件日志四个示例），并在 `Shared/DemoNavigation.cs` 增加入口。

- **`ElVideoPlayerGrow` 增强：循环播放按钮 + 外部 MSE 库接入分支**：① 控制条新增 **循环播放切换按钮**（♺，`ShowLoop` 默认 true，运行时可切换 `Loop`，并补 `elVideoPlayerGrowSetLoop`）；② **静音开关**原本已支持（`Muted` 参数 + 🎤/🔇 切换按钮 + `elVideoPlayerGrowSetMuted`）；③ 新增可选 `HlsLibUrl` / `DashLibUrl` / `FlvLibUrl`——显式提供时组件**动态加载** hls.js / dash.js / flv.js 并接管视频元素（默认仍保持零依赖原生播放）；MSE 路径下 `<video>` 不再渲染 `<source>`（由库设置 src），库加载/接入失败经 `VideoPlayerGrowOnError` → `OnError` 回调与错误覆盖层提示。回归：`ce_build/serve_and_probe.py`（Python 起站 + Playwright 探针）实测 **7 播放器 / 6 控制条 / 循环按钮 6 / 静音按钮 6 / 点击·循环切换·静音切换均 null / MSE 播放器无 `<source>`·原生播放器有 `<source>` / consoleErrors[]·pageErrors[]**。


- **`/table` 演示新增「列头点击弹出菜单 + 删除当前列」（`ElTableColumn.OnHeaderClick` 实战）**：给演示表 6 个列头都挂 `OnHeaderClick`，点击列头即在点击处弹出菜单——`删除此列` 调 `ElTable.RemoveColumn(Func<TableHeader,bool>)` 即时从运行时列集合移除该列并重绘；`复制列名` 把列标题写入剪贴板（安全上下文 `navigator.clipboard`，否则 `execCommand` 兜底）。菜单打开后给表格传 `@@key` 换新值即可「重置演示」恢复全部列。
  - **组件库侧无需改动**：`OnHeaderClick`（组件把该列 `TableHeader` 实例原样回传，可 `ReferenceEquals` 定位）、`RemoveColumn(predicate)` / `RemoveColumn(text)`、`ApplyColumnChanges()`（从 `Headers` 移除后触发 `StateHasChanged`，且**不动** `headerRendered`，故声明式列不会被 ChildContent 重新注册回来）均为既有公开能力。
  - 弹层**不用** `ElDropdown`（其浮层走全局 popup 宿主，难以定位到任意列头坐标），改用 demo 专用 `wwwroot/js/table-header-menu.js` 记录最近一次 `.el-table__header th` 点击坐标（`clientX/Y`，已做视口夹取）供 C# 读取定位，样式 `.demo-colmenu*` 写在 `wwwroot/css/app.css`（**不进组件库 `index.css`**）。
  - 注：`TableHeader.Ignore` 的 setter 是 `internal`，demo 程序集不可写，故未提供「隐藏本列」，仅保留「删除此列 / 复制列名」。
  - **回归**：`ce_build/probe_table_header_menu.cjs`（Playwright + msedge 打真实站）**10/10 PASS、RESULT_OK=true、0 JS 异常**——初始 6 列 → 点「名称」列头弹菜单（2 项、标题「列：名称」、定位在视口内）→ 删除此列 6→5 且表头消失、状态文案更新 → 点「ID」列头「复制列名」状态更新 → 「重置演示」列数恢复 6、「名称」「类别」列重现。

- **演示站：每个示例块新增「复制代码 / 查看源代码」操作条（对齐官方文档每个示例块右下角的那两个小按钮，如 https://element-plus.org/zh-CN/component/border）**：89 个示例页、486 个 `<div class="demo-section">` **零页面改动**即全部生效——`复制代码` 把该段示例的 Razor 源码写入剪贴板（成功后按钮变「✓ 已复制」，1.4s 复原）；`查看源代码` **在示例块内就地展开**代码面板（与官方一致，不是弹窗）。
  - **代码面板（就地展开，对齐官方）**：块被撑开、代码铺在块底部，操作条让出底部圆角与边距交给面板，两者无缝拼成一块。面板工具条左侧是 `本段示例 / 整页源码` 分段切换（对应官方的 TS / JS 位置），中间显示文件名（如 `BorderDemo.razor · 边框样式（border-style）`），右侧是 `复制` 与 `<> 收起` 两个图标按钮。展开时操作条按钮变「收起代码」并高亮，`Esc` 亦可收起。
  - **代码折叠**：每行左侧带折叠箭头（对齐官方代码区的 `˅`），点一下收起来缩进更深的子行（箭头转 `˃`、行尾显示 `⋯`），再点恢复；支持嵌套折叠（折叠集合取并集重算可见行，展开外层不误伤内层状态）。折叠区间按**缩进**计算（不做标签配对——Razor 里 `@if`/`@foreach` 的标签配不平，缩进法天然正确），全量校验 486 个块共 **1702 个区间 / 1216 个嵌套、10218 条断言全过**。
  - **性能**：面板外壳（工具条 + 空代码区）注入时就建好，但**代码首次展开才逐行渲染**，两个页签各自懒渲染——486 个块不会一次性把源码铺进 DOM。
  - **源码下发**：Blazor WASM 在浏览器里读不到磁盘上的 `.razor`，故在编译期把 demo 页源码以 `EmbeddedResource`（`DemoSource.<文件名>.razor`）打进程序集；`Shared/DemoSourceStore.cs` 按页面类型全名读回，`Components/DemoCodeHost.razor` 挂在 `App.razor` 的 `<Found>` 内随导航推给 JS。页面身份直接复用 keep-alive 外壳的 `data-keep-key`（= 页面类型全名），**不需要额外维护路由表**。
  - **时序**：keep-alive 是「两阶段挂载」（先占位、下一帧才挂页面组件），源码常常早于 DOM 到达；JS 侧用 `MutationObserver`（`childList + subtree`，180ms 去抖）持续补齐，两种先后顺序都能注入，且插入后自然收敛（不会自激循环）。
  - **片段切分**：按 `<div>` 嵌套深度配平（自闭合 `<div/>` 不加深）；只认「标签起始位置」的匹配，避免命中 C# 字符串里手写的 `<div class="demo-section">`；与 DOM 示例块**按块内首个标题（h1~h6）文本精确匹配**、缺失或重名时回退按出现顺序（`TimelineDemo` 用 `<h4>` 的页面同样命中）。
  - **操作条视觉**：贴在示例块底部横向铺满（左右/下负外边距在 JS 里按容器实际 `padding` 计算，底圆角随容器），平时淡灰、容器悬停或键盘聚焦时提亮；纯键盘可达（`focus-visible` 有描边）。demo 侧样式写在 `wwwroot/css/app.css`，**不进组件库 `index.css`**。
  - **回归**：`ce_build/test_demo_code_parse.cjs`（node，全量切分 486/486 块）+ `ce_build/test_demo_code_folds.cjs`（node，折叠区间 10218 条断言全过）+ `ce_build/probe_demo_code_app.cjs`（Playwright + msedge **打真实站 38 项断言全过**：就地展开/无弹层/块高度 157→357px/圆角交接/两页签/折叠收起恢复/面板内复制==源码且行数一致/**「整页源码」与磁盘 `.razor` 逐字符一致**（4708=4708）/Esc 收起/keep-alive 切回不重复不丢失/0 JS 异常）+ `ce_build/test_demo_code_browser.cjs`（本地 harness 预检）。详见 `docs/demo-code-actions.md`。
  - 注：改 `wwwroot` 下的 `js` / `css` **不需要重新编译**——`dotnet run` 的 DevServer 直接从项目 `wwwroot` 提供静态文件，浏览器 `CTRL+F5` 即可。

- **`ElButton` 按官方 Button 文档全面对齐（https://element-plus.org/zh-CN/component/button），既有扩展功能（Image/HoverImage/ClickImage、Debounce/DebounceLeading、IconPosition、Content/Label、Href/Target/Rel/Tag 等）全部保留**：补齐 EP Exposes —— `ShouldAddSpace` 公开化（对应官方 `shouldAddSpace`），新增 `ResolvedType` / `ResolvedSize` / `ResolvedDisabled`（分别对应官方 `type` / `size` / `disabled`，返回解析后生效值，含按钮组级联与全局 `ButtonConfigContext` 回落）；demo `/button` 新增「Exposes 读取」演示，bUnit 新增 **5 个测试方法（1 个 Theory 含 5 用例 + 4 个 Fact）**回归（ElButtonTests **8 → 17 通过 / 0 失败**）。详见 `docs/button-alignment.md`。

- **`ElSlider` 按官方 Slider 文档全面对齐（https://element-plus.org/zh-CN/component/slider —— 属性 / 事件 / DOM / 竖向布局），既有扩展（`@bind-RangeValue`、`int Height`、`ValidateEvent`、`OnInput`/`OnChange` 等）全部保留**：详见 `docs/slider-alignment.md`。
  - **新增属性**：`Size`（官方 2.x 的 `size`，包装器尺寸 large`/`default`/`small`）、`StepToMark`（官方 2.13.6 的 `step="mark"`，取值限制为 `Marks` 刻度）、`Persistent`（官方 2.9.5，非活动态即销毁 tooltip）、`TooltipClass`、`RangeStartLabel`、`RangeEndLabel`、`FormatValueText`、`Placement`（原 `TooltipPlacement` 降为其兼容别名）。官方已标记 deprecated 的 `label` **按约定不接入**，请用 `AriaLabel`。
  - **`input-size` 联动**：未显式赋值时自动继承 `Size`（对齐官方 `inputSize || sliderWrapperSize`）。
  - **`show-input` 生效条件改为官方口径**：`ShowInput && !Range && !StepToMark`（此前 range 下也会渲染输入框）。
  - **DOM 对齐官方 `slider.vue`**：runway 补 `show-input` / `is-disabled` 修饰类；marks 包进 `el-slider__marks` 容器并为每个标记补 `el-slider__marks-stop`；range 时根容器加 `role="group"` + `aria-label`/`aria-labelledby`；两端补 `aria-orientation`；range 两个把手的 `aria-valuemin`/`aria-valuemax` 改为**相互约束**（官方同名语义）。
  - **stops 改用官方 `useStops` 算法**：`ceil((max-min)/step)` 计数，并**只渲染当前已选区间之外的断点**；`step="mark"` 下不渲染断点。
  - **`Vertical` 竖向布局正式实现**（此前仅有 `Height` 作用于 runway，滑块方向仍是横向）：bar 改 `bottom + height` 定位，握把 / 断点 / 标记改 `bottom` 定位，原生 range input 以 `writing-mode: vertical-lr; direction: rtl` 竖排（同时保留 `-webkit-appearance: slider-vertical` 兼容旧内核）。浏览器实测几何与 `Height` 严格吻合。
  - 回归：新增/调整 **13 例** bUnit（`ElSliderTests` **25 通过 / 0 失败**）；浏览器探针 `demo/verify-slider.cjs` **22/22 通过**、控制台 0 错误；demo `/slider` 重写为官方章节顺序。

- **#148 EXT-UNRELATED 专项 · 第一批低风险 14 项全部接线**：`ElTimePicker.Tabindex`、`ElSegmented.ValidateEvent`、`ElTransfer.PanelHeight` / `ShowAll`、`ElCheckbox.ValidateEvent`、`ElInputTag.Effect`、`ElRadio.BorderColor`、`ElRate.Readonly`、`ElImage.InitialIndex`、`ElTreeSelect.EmptyText`、`ElSlider.Height`、`ElCascader.Debounce`、`ElMention.Debounce`、`ElSelectV2.DefaultFirstOption`。原先均「声明了 `[Parameter]` 但组件内零引用」，现已真实消费。
  - **ElSegmented / ElCheckbox**：`ValidateEvent` 接入 `SetFieldValue(value, validate)`，仅 `true` 时触发表单重校验 + 刷新错误态；`false` 只更新值。
  - **ElCascader / ElMention**：新增搜索/候选防抖 `Debounce`（`CancellationTokenSource` + `Task.Delay`，默认 `300`ms，新输入取消上一轮）；`Debounce=0` 即时刷新。⚠️ 行为变更：默认由「边打边出」变为「停手 300ms 出」，调用方可显式传 `0` 恢复即时。
  - **ElTransfer**：`PanelHeight` 作用于两侧列表体（出现滚动条）；`ShowAll` 控制标题栏全选复选框的显隐。
  - **ElInputTag**：`Effect` 输出 `el-tag--light` / `el-tag--dark` / `el-tag--plain` 主题类。
  - **ElImage**：`InitialIndex` 决定点击后预览的起始图，越界或负数回落 `0`。
  - **ElSelectV2**：`DefaultFirstOption` 打开面板即高亮首项，输入框直接回车选中。
  - **ElRate**：`Readonly=true` 时点击/键盘不再改值，仍显示当前分值且不灰显（区别于 `Disabled`）。
  - **ElRadio.BorderColor**、**ElTreeSelect.EmptyText**、**ElTimePicker.Tabindex**（根 input）、**ElSlider.Height**（`Vertical` 时作用于 runway 高度）均为直接透传。
  - 回归：新增/调整 **15 例** bUnit 用例（含新建 `ElImageTests.cs`）；针对性回归 **166 通过 / 0 失败 / 3 跳过**。
  - demo：13 个页面补演示段（`/timepicker` `/segmented` `/transfer` `/checkbox` `/input-tag` `/radio` `/rate` `/image` `/treeselect` `/slider` `/cascader` `/mention` `/select-v2`）。
  - 注：`ElSlider.Vertical` 的竖向布局当时标注为「尚未实现」，已在本 `[Unreleased]` 上方的 `ElSlider` 条目中正式补齐，不再作为待接线项。

- **#148 EXT-UNRELATED 专项 · 第二批（弹层基础设施 5 项 + `ElCascader.ValueKey` + `ElDatePickerPanel` 2 项 = 8 项）**：补齐底层 `PopupOption`/`DropDownOption` 的 `Persistent`/`AppendTo` 字段并真正消费，解决「声明了 `[Parameter]` 却零引用」的弹层缺口；`ElCascader.ValueKey`、`ElDatePickerPanel.Editable`/`Shortcuts` 由声明落地为真实行为。详见 `EXT-UNRELATED-AUDIT.md`（行 1/4/5/6/7/8/9/32 状态改为「已修」）。
  - **弹层基础设施 5 项**（`ElColorPicker.Persistent`/`AppendTo`、`ElDateTimePicker.Persistent`/`AppendTo`、`ElTour.AppendTo`）：`PopupOption` 新增 `Persistent`(bool) 与 `AppendTo`(string) 字段；`ElPopup` 在 select-dropdown / dropdown-menu 两个浮层的**外部点击遮罩（v-modal）**上加 `if (!option.Persistent)` 守卫（`Persistent=true` 时点击遮罩不关闭），并在浮层根节点输出 `data-append-to="@option.AppendTo"` 供 API 对齐与调试；`ElColorPicker`/`ElDateTimePicker` 在 `DropDownOption` 构造处把本组件 `Persistent`/`AppendTo` 透传下去，`ElTour` 根节点直接输出 `data-append-to="@AppendTo"`。
  - **`ElCascader.ValueKey`**：`RootOptions` 在 `ValueKey` 非空且非默认 `"value"` 时，递归遍历 `Options`（含 `Children`），从每个 `CascaderOption.Data` 上按 `ValueKey` 反射抽取字段值回写 `option.Value`（支持字典 `IDictionary` 与 POCO 属性，含大小写不敏感回退）；未携带 `Data` 的选项保持原 `Value` 不变，向后兼容。
  - **`ElDatePickerPanel.Editable` / `Shortcuts`**：`Editable=false` 时所有日期/月份/年份选择入口直接短路返回（只读展示面板，对齐官方语义）；`Shortcuts`（`IList<DatePickerShortcut>`）非空时在面板左侧渲染 `el-picker-panel__sidebar` 快捷项列表（`Text` 按钮），点击经 `ResolveValue()` / `ResolveRangeValue()` 提交对应日期；面板根 `has-sidebar` 类 + `index.css` 追加 `.el-picker-panel__sidebar+.el-picker-panel__body-wrapper{margin-left:110px}` 与 `.el-date-picker-panel.has-sidebar{width:438px}` 布局修正。注：宿主 `ElDatePicker` 自身维护快捷侧栏且**不转发** `Shortcuts` 给面板，故不存在双侧栏。
  - **`Persistent` 默认值修正**：`ElColorPicker`/`ElDateTimePicker` 的 `Persistent` 由 `true` 改为 `false`——该参数此前声明未消费、运行时已按「点击外部关闭」行为，`false` 使声明默认值与真实行为及官方一致（默认点击外部关闭浮层）。
  - 回归：`Element.csproj` 全量 `dotnet build` **0 警告 / 0 错误**；bUnit 新增 **4 例**（`ElCascaderTests.ValueKeyMapsDataProperty`、`ElTourTests.RendersAppendToAttribute`、`ElDatePickerPanelTests.EditableFalseSuppressesSelection`、`ElDatePickerPanelTests.ShortcutsRenderSidebarAndCommit`），经 `run_ext_unrelated_tests.py` 全绿。

- **#148 EXT-UNRELATED 专项 · 收口（原「剩余 9 项」实为审计快照漏判）**：对 `ElMention`/`ElRate`/`ElSelectV2` 三组件当前代码逐文件复核，确认 `ElMention.Position`/`PopperClass`/`Editable`、`ElRate.TextTemplate`/`OnFocus`/`OnBlur`、`ElSelectV2.ClearAfterSelect`/`AllowCreate`/`OnCreateOption` **均已在 `.razor` 标记或 `.razor.cs` 中被真实消费**（与 #29 `ElTreeSelect.ExpandOnClickNode` 同类，属 2026-10-01 审计静态快照滞后导致的 false negative，非真正未接线）。`Element.csproj` 全量 `dotnet build` **0 警告 / 0 错误**；bUnit 新增 **9 例**回归全部通过（见下），`EXT-UNRELATED-AUDIT.md` 35 项状态列全部「已修」，专项收口。
  - `ElMention.Position`：下拉 `el-mention-dropdown--top/bottom` 类 + 上下方位渲染；`PopperClass`：附加到 dropdown class；`Editable=false`：textarea `readonly`。回归：`ElMentionTests.PositionTopRendersDropdownAboveTextarea` / `.PopperClassAppliedToDropdown` / `.EditableFalseMakesTextareaReadonly`。
  - `ElRate.TextTemplate`：`DisplayText` 中以 `{value}` 替换为当前分值的自定义描述模板（与 `ShowText` 配合）；`OnFocus`/`OnBlur`：接 `@onfocus`/`@onblur` 事件回调。回归：`ElRateTests.TextTemplateRendersCustomScoreText` / `.OnFocusFiresCallback` / `.OnBlurFiresCallback`。
  - `ElSelectV2.ClearAfterSelect`：多选分支选中后清空 `filterText`（单选分支本就清空）；`AllowCreate`：驱动 `ShowCreateOption` 渲染「创建 X」哨兵行 + `SelectOptionAsync` 哨兵分支回调 `OnCreateOption`。回归：`ElSelectV2Tests.ClearAfterSelectClearsFilterInMultipleMode` / `.WithoutClearAfterSelectFilterPersistsInMultipleMode`（对照）/ `.AllowCreateShowsCreateRowAndInvokesCallback`。

- **`ElMenu.PopperEffect` 三层接线（对齐官方 `popper-effect`，2.2.26+，官方未标记废弃）**：链路 `MenuOptions.PopperEffect` → `SubMenuOption.PopperEffect`（新增字段）→ `ElSubMenu` 取值下发（`PopperEffect ?? Options?.PopperEffect`）→ `ElPopup` 在 `el-menu--popup-container` 上输出 `el-popper.is-{theme}` 类；`index.css` 补 `.el-popper.is-light` / `.is-dark` 两套主题（含箭头底色）。
  - **与官方的已知偏差**：官方语义为「菜单折叠时 tooltip 的主题」，默认 `dark`；本库弹层长期为浅色，若按默认 `dark` 会造成全量视觉回归，故**默认值取 `light`**（保持既有外观），调用方显式传 `dark` 才切深色。已在 docs 标注，后续可 eval whether 需要改为官方默认值并同步 UI 规范。

- **`ElAffix` / `ElAnchor` / `ElBacktop` / `ElBreadcrumb` / `ElDropdown` / `ElMenu` / `ElPageHeader` / `ElSteps` / `ElTabs` 对齐 Element Plus（官方文档）**：按各自官方文档补齐能力面，既有扩展功能全部保留；并新增 9 个独立 demo 页面（`/affix` `/anchor` `/backtop` `/breadcrumb` `/dropdown` `/menu` `/page-header` `/steps` `/tabs`）。详见 `docs/{affix,anchor,backtop,breadcrumb,dropdown,menu,page-header,steps,tabs}-alignment.md`。
  - **ElAffix**：新增只读 `IsFixed`（官方无 exposes，便于调用方联动动画/样式）。`Offset`/`OffsetTop`/`OffsetBottom`/`Position`/`Target`/`ZIndex` 与 `change`/`scroll` 事件原本已对齐。
  - **ElAnchor**：新增 `SelectScrollTop`（官方 2.10.5）、`CursorTemplate`（官方 cursor 插槽）、`Id`/`AriaLabel`、只读 `ActiveHref` 与 `ScrollToAsync(href)`；`ElAnchorLink` 新增 `Target`（原生 a 的 target）与单条 `Offset`（优先级高于 `ElAnchor.Offset`）。`dom.js` 的 `elementAnchorScrollTo` 增加第 5 个参数 `selectScrollTop`（忽略 offset、目标直接顶到容器顶部）。
  - **ElBacktop**：新增 `Id`/`AriaLabel`、只读 `Visible` 与 `ScrollToTopAsync()`；点击与编程式调用统一走 `ScrollToTopAsync()`。
  - **ElBreadcrumb**：新增 `SeparatorTemplate`（优先级高于 `SeparatorIcon`/`Separator`）、`Id`/`AriaLabel`（未设置回落官方默认 `Breadcrumb`）。
  - **ElDropdown**：新增 `Placement`（默认 `bottom-start`）、`MaxHeight`、`Tabindex`、`Role`（默认 `menu`）、`Id`/`AriaLabel`，并把既有 `PopperClass`/`PopperStyle` 真正下发到弹层；新增 `OnVisibleChange`/`OnOpen`/`OnClose` 事件；`ShowTimeout` 默认值由 150 对齐官方 250。
  - **ElMenu**：新增 `Ellipsis`（官方 ellipsis 的「文字省略号截断」部分，未实现溢出折叠进「更多」弹层）、`Id`/`AriaLabel`；`MenuOptions` 同步新增 `Ellipsis`。
  - **ElPageHeader**：新增 `Id`/`AriaLabel`（官方 icon/title/content 属性与 icon/title/content/extra/breadcrumb/default 插槽、back 事件原本已完整对齐）。
  - **ElSteps / ElStep**：`ElStep` 新增官方 `Space`（单步骤宽度，覆盖 `ElSteps.Space`）。
  - **ElTabs / ElTabPane**：新增官方 `tab-click` 事件（`OnTabClick`，点击即触发、与 `tab-change` 语义区分）、`edit` 事件（`OnEdit`，Action=`add`/`remove`）、exposes `CurrentName`；`ElTabPane` 新增 `Label` 别名、`TitleTemplate`（官方 label 插槽）、`Lazy`（官方 lazy，回落 `ElTabs.Lazy`）。

- **`ElAlert` / `ElDivider` / `ElTooltip` / `ElPopover` / `ElPopconfirm` / `ElDialog` / `ElDrawer` / `ElLoading` / `ElMessage` / `ElMessageBox` / `ElNotification` / `ElWatermark` 对齐 Element Plus（官方文档）**：按各自官方文档补齐能力面，既有扩展功能全部保留；新增 11 个独立 demo 页面（`/alert` `/divider` `/tooltip` `/popover` `/popconfirm` `/dialog` `/drawer` `/loading` `/message` `/message-box` `/notification`，`/watermark` 此前已存在）。详见 `docs/{alert,divider,tooltip,popover,popconfirm,dialog,drawer,loading,message,message-box,notification,watermark}-alignment.md`。
  - **ElAlert**：补齐 `Title`/`Description` 字符串属性与 `TitleContent`/`DescriptionContent`/`IconContent` 插槽、`ChildContent` 描述正文、`Effect`(light/dark)、`Center`、`OnClose` 事件。
  - **ElDivider**：`Direction`(horizontal/vertical)、`ContentPosition`(left/right/center)、`BorderStyle` 自定义边框样式。
  - **ElTooltip**：`Enterable`（鼠标可进入浮层）、`AutoClose`、`Persistent`、`Effect`(dark/light)、`PopperClass`/`PopperStyle`、`ContentTemplate` 富文本。
  - **ElPopover**：`Effect`、`TitleContent`/`ContentTemplate`/`Footer`/`Reference` 插槽、`ShowArrow`、`PopperClass`/`PopperStyle`。
  - **ElPopconfirm**：`Type`(PopconfirmType)、`Content`/`ContentTemplate`、`ActionsContent` 自定义操作区、`ConfirmButtonText`/`CancelButtonText`/`ConfirmButtonType`/`CancelButtonType`、`Icon`/`IconColor`/`HideIcon`/`ShowClose`、`OnConfirm`/`OnCancel`。
  - **ElDialog**：`AlignCenter`、`HeaderAriaLevel`、`ModalClass`，完整 `Open`/`Opened`/`Close`/`Closed`/`VisibleChange` 事件与 `Header` 插槽。
  - **ElDrawer**：`BodyClass`/`ModalClass`/`DrawerClassParam`/`HeaderClass`/`FooterClass`、`OpenDelay`/`CloseDelay`、`ModalPenetrable`、`HeaderAriaLevel`、`Resizable` 及 `OnResizeStart`/`OnResize`/`OnResizeEnd`、`OpenAutoFocus`/`CloseAutoFocus`、`BeforeClose`/`OnBeforeClose`。
  - **ElLoading**：组件式 `<ElLoading>`（遮罩/文本/背景/全屏/锁滚动/自定义 class）与服务式 `LoadingService.Show/ShowFullscreen`（整屏或指定 `Target` 区域）。
  - **ElMessage**：`MessageService` 四类便捷方法 + `Show(MessageInfo{Message,Type,Duration,ShowClose,Center,Grouping,Plain,Offset,DangerouslyUseHTMLString})`、`CloseAll`、`Max`。
  - **ElMessageBox**：`MessageBox.AlertAsync/ConfirmAsync/PromptAsync(MessageBoxOption{Title,Message,Type,ConfirmButtonText,ShowInput,...})`，返回 `MessageBoxResult`/`MessageBoxPromptResult`。
  - **ElNotification / ElWatermark**：能力面此前已对齐（见上），本轮补齐 demo 与 `docs` 说明。

- **`ElSegmented` / `ElStatistic` / `ElTour` / `ElTree` / `ElTreeV2` 对齐 Element Plus（官方文档，2026-09-30）**：按各自官方文档补齐能力面，既有扩展功能（Segmented 的 `ButtonType`/`Items`、Statistic 的 `Trend`/`Extra`、Tour 的 `Header` 插槽与 `OnNext`/`OnPrev`/`OnEscPress`/`OnMaskClick` 事件、Tree/TreeV2 的 `Url`/`LoadFullTree`/`DataSource` 远程加载与虚拟滚动）全部保留。详见 `docs/{segmented,statistic,tour,tree,tree-v2}-alignment.md`。
  - **ElSegmented**：新增 `Direction`（`Horizontal`/`Vertical`，2.8.7）、`ItemTemplate`（官方默认插槽，作用域为 `SegmentedOption`）、`Name`/`ValidateEvent`/`Id`/`AriaLabel`；`ButtonType="primary"` 整项高亮主题色。CSS 单一真源追加 `.el-segmented.is-vertical` 与 `--primary` 段。
  - **ElStatistic**：新增 `DecimalSeparator`（官方 `decimal-separator`，替换区域小数点符号）、`DisplayValueText`（官方 `exposes: displayValue`）。
  - **ElTour**：新增 `Type`（`default`/`primary`，对应官方 step type）、`ContentStyle` + `TourStep.ContentStyle`、`Gap`（`TourGap`）、`CloseIcon`、`TargetAreaClickable`、`ScrollIntoView`（JS `elementScrollIntoView`）、`Indicators` 插槽、`AppendTo`；`TourStep` 新增 `Type`/`ContentStyle`/`StepHeader`/`NextButtonProps`/`PrevButtonProps`；`ZIndex` 默认值对齐官方 `2001`。
  - **ElTree / ElTreeV2**：新增 `CheckOnClickNode`/`CheckOnClickLeaf`、`CurrentNodeKey`/`CurrentNodeKeyChanged`、`Accordion`、`AllowDrag`/`AllowDrop`、`OnNodeContextMenu`/`OnCheck`/`OnCheckChange`/`OnCurrentChange`、拖拽过程事件 `OnNodeDragStart/Enter/Leave/Over/End`；暴露方法 `Filter`/`SetCheckedKeys`/`GetCheckedKeys`/`SetChecked`/`SetCheckedNodes`/`GetHalfCheckedNodes(Keys)`/`GetCurrentKey(Node)`/`SetCurrentKey`/`GetNode`/`Append`/`Remove`/`InsertBefore`/`InsertAfter`。
  - **ElTreeV2 专属**：`ScrollbarAlwaysOn`（2.10.4）、`FilterMethod`（接入虚拟拍平的可见性计算）、`SetExpandedKeys`/`ExpandNode`/`CollapseNode`/`SetData`/`ScrollTo`/`ScrollToNode`（2.8.0，JS `elementSetScrollTop`）；CSS 追加 `.is-scrollbar-always-on` 常显滚动条。
  - **测试**：`test/Element.ComponentTests/` 下新增/扩展 `ElSegmentedTests`/`ElStatisticTests`/`ElTourTests`/`ElTreeTests`/`ElTreeV2Tests`。

- **`ElTimeline` / `ElTimelineItem` 对齐 Element Plus Timeline（官方文档，2026-09-30）**：按 EP `timeline` / `timeline-item` 补齐能力面，扩展功能（Pending / Horizontal / OnClick / HideTail / TimestampContent）全部保留：
  - **`ElTimeline` 新增**：`Reverse`（逆序，2.11.9，CSS 列反转实现，保持 ChildContent 声明顺序）、`Mode`（2.13.1：`Start` / `End` / `Alternate` / `AlternateReverse`，轴线与内容相对位置，多列交替）；新增枚举 `TimelineMode`。
  - **`ElTimelineItem` 新增**：`Center`（垂直居中，2.x）、`HideTimestamp`（隐藏时间戳）；`Size` 默认由空串改为 `normal`，**修复默认尺寸下节点 0×0 不可见**的潜在缺陷；`Dot` 插槽改为独立 `.el-timeline-item__dot` 元素（与 EP 一致，原实现把 dot 塞进 node 内）。
  - **CSS（`index.css` 单一真源，追加块，无 guard 违禁写法）**：`reverse` 列反转 + 末首项 tail 反向隐藏；`end` 轴线镜像到右侧；`alternate`/`alternate-reverse` 轴线居中、内容左右半区交替；`center` 垂直居中（`display:flex; align-items:center` + 首尾 tail 半线修正）；`hollow` 空心节点；`large` 节点尺寸对齐 EP（22px）；`dot` 定位。
  - **演示**：`/timeline`（`demo/Element.Blazor.Demo/Pages/TimelineDemo.razor`）覆盖基础 / 类型色 / 颜色 / 尺寸 / 空心 / 图标 / 自定义 dot / 时间戳 top / center / reverse / 四态 mode / 扩展项；`demo/diag-timeline.cjs` Playwright 探针 9 段全绿、控制台零错误。

- **`ElButton` / `ElColorPicker` / `ElContainer` / `ElIcon` / `ElRow·ElCol` / `ElLink` / `ElText` / `ElScrollbar` / `ElSpace` / `ElSplitter` / `ElConfigProvider` 对照 Element Plus 官方文档继续完善（2026-09-30）**：逐组件核对 EP 官方文档能力面，既有扩展功能全部保留；新增 / 补齐 10 个独立 demo 页面与 11 份 `docs/*-alignment.md`：
  - **ElButton**：`Type`/`Plain`/`Round`/`Circle`/`Text`/`Bg`/`Link`/`Dashed`/`Color`+`Dark`/`Loading`/`LoadingIcon`/`Disabled`/`Icon`+`IconPosition`(左/右)/`AutoInsertSpace`/`NativeType`/`Tag`/`Href`/`Target`/`Rel`/`Tabindex`/`Debounce` 等全量对齐；`ButtonConfigContext`（type/plain/text/round/dashed/autoInsertSpace 全局默认）通过 `SetParametersAsync` 记录「是否显式赋值」区分 EP 的 `undefined` 与 `false`；扩展：`IconPosition`、`Debounce`(防抖)、`Image`/`HoverImage`/`ClickImage`、`Content`/`Label`、`FocusAsync()` exposes。
  - **ElColorPicker**（demo `/colorpicker`，此前已对齐）：`@bind-Value`/`ShowAlpha`/`ColorFormat`/`Predefine`/`Clearable`/`Disabled`/`Size`；本轮修掉 `ColorPickerDemo.razor` 说明文案里把 `@bind-Value` 当成代码导致 **CS0103 编译错误** 的隐患（改为转义文本）。
  - **ElContainer**（demo `/container`）：`ElContainer`+`ElHeader`/`ElAside`/`ElMain`/`ElFooter`，`Direction`(auto/horizontal/vertical) 与 `ElAside.Width`(默认 300px) 对齐。
  - **ElIcon**（demo `/icon`）：`Name`(字符串，三种写法归一化)/`Icon`(枚举)/`Size`/`Color`/`Spin`/`OnClick`/`Title`/`AriaLabel`；内置 `FontAliases` 把字体缺失字形兜底到语义最接近已有字形。
  - **ElRow·ElCol**（demo `/grid`）：`Gutter`/`Justify`(start/center/end/space-*)/`Align`(top/middle/bottom)、`Span`/`Offset`/`Push`/`Pull`、响应式 `Xs~Xl`、`Tag`；24 列、列宽类经 `NormalizeSpan` 限幅 0~24。
  - **ElLink**（demo `/link`）：`Type`(default/primary/success/warning/danger/info)/`Disabled`/`Underline`(hover/always/never)/`Href`/`Target`/`Rel`(target=_blank 自动安全 rel)/`Icon`+`IconContent`/`OnClick`；纯名称 `Icon` 自动补 `el-icon-` 前缀。
  - **ElText**（demo `/text`）：`Type`/`Size`(default/large/small)/`Truncated`/`LineClamp`(int?)/`Tag`；经 `ElementTagRenderer` 渲染并合并未匹配 `class`/`style` 特性。
  - **ElScrollbar**（demo `/scrollbar`）：`Height`/`MaxHeight`/`Native`/`Noresize`/`Always`/`MinSize`/`Tag`/`WrapStyle`/`ViewStyle`/`OnScroll`(ScrollEventArgs)/`EndReached`(ScrollEndEventArgs)/`SetScrollTopAsync`/`SetScrollLeftAsync`/`ScrollToAsync`/`UpdateAsync`；a11y 增强 `Id`/`Role`/`AriaLabel`/`AriaOrientation`/`Tabindex`。
  - **ElSpace**（demo `/space`）：`Direction`/`Alignment`/`Size`/`Spacer`/`Wrap`/`Fill`/`FillRatio`/`Separator`(RenderFragment)/`PrefixCls`；`gap` 由 Spacer 优先、Size 次之。
  - **ElSplitter**（demo `/splitter`）：`Direction`/`Lazy`/`OnResizeStart`/`OnResize`/`OnResizeEnd`/`OnCollapse`；`ElSplitterPanel.Size`/`Min`/`Max`/`Resizable`(默认 true)/`Collapsible`(双击分隔条折叠)/`SizeChanged`；面板经 `CascadingParameter` 注册，首帧 `elementSplitterInit` 初始化拖拽。
  - **ElConfigProvider**（demo `/config-provider`）：`Size`(级联下发)/`Locale`/`ZIndex`/`Namespace`(变化重写根命名空间)/`Config`/`Button`/`Link`/`Dialog`/`Message`/`Table` 各 ConfigContext 合并；局部作用域（仅影响内部子组件）。

- **`ElTypography` / `ElTypographyTitle` 全新组件（对齐 Element Plus Typography，2026-09-30）**：此前仓库**缺失**该组件，本轮自研补齐、零外部依赖。
  - `ElTypography`(根容器，`Tag` 默认 div) + `ElTypographyTitle`(`Level` 1~5→h1~h5、`Type`(default/primary/success/warning/danger/info)、`Content`/`ChildContent`、`Copyable`(一键复制)、`Truncated`、`LineClamp`(int?))。
  - 经 `ElementTagRenderer` 渲染；复制按钮 `.el-typography-title__copy` 调新增 JS 辅助 `window.elementCopyText`（dom.js，ES5 安全，剪贴板不可用时静默失败）；样式集中在 `index.css` 的 `/* ==== ElTypography / ElTypographyTitle ==== */` 段（h1~h5 字号 32/26/20/16/14px、语义色映射 `--el-color-*`、截断走 `-webkit-box`）。demo `/typography`。

- **`Border` 对照 Element Plus 官方 border（CSS 工具类，2026-09-30）**：EP `border` **不是组件**而是统一规范一组 CSS 变量（`--el-border` / `--el-border-color*` / `--el-border-radius-*` / `--el-box-shadow*`）。仓库 `index.css` 的 `:root` 已固化全部变量；新增 `BorderDemo.razor`(`/border`) 用这些变量做可视化参考（边框样式 / 语义色 / 深浅梯度 / 圆角 / 阴影）。明确：**不存在也不应有 `ElBorder` 组件**（与官方一致）。

- **`ElButton` / `ElColorPicker` / `ElContainer` / `ElIcon` / `ElRow·ElCol` / `ElLink` / `ElText` / `ElScrollbar` / `ElSpace` / `ElSplitter` / `ElConfigProvider` / `ElTypography` / `Border` 对齐 Element Plus（官方文档，2026-09-30）**：新增 / 补齐 10 个独立 demo 页面（`/icon` `/link` `/text` `/scrollbar` `/space` `/splitter` `/config-provider` `/typography` `/border` `/grid`，其中 `/colorpicker` `/container` 此前已存在但未登记导航），并在 `DemoNavigation.MockSection` 注册入口；新增 11 份 `docs/*-alignment.md`（`border`/`container`/`icon`/`layout`/`link`/`text`/`scrollbar`/`space`/`splitter`/`typography`/`config-provider`，`button`/`color-picker` 此前已存在）。`Element.Blazor.Demo.csproj`（`Microsoft.NET.Sdk.BlazorWebAssembly`，net10.0）构建 **0 错误**（修复 `ColorPickerDemo.razor` 文案 `@bind-Value` 编译错误后）。

- **`ElCollapse` / `ElAutocomplete` 按 EP 官方文档补齐 exposes 与事件钩子（#148 Tier B，2026-10-01）**：
  - **ElCollapse**：新增 `BeforeCollapse`（`Func<IList<string>, Task<bool>>`，对齐官方 `before-collapse`，2.9.11+）。折叠状态更改前调用，返回 `false` 或被 reject 的 `Task` 即取消本次切换；入参为「切换后即将生效的激活面板名集合」（手风琴模式下收起时为空集合）。既有 `Value`/`Values`/`OnChange`/`Accordion` 行为不变。
  - **ElAutocomplete**：新增 `CloseAsync()`（对齐官方 `close()` exposes），折叠/收起建议列表；未打开时为空操作，可安全重复调用。已有 `FocusAsync`/`BlurAsync`/`ClearAsync` 保持不变。
  - **测试 / 演示**：新增 `ElCollapseTests`(3) + `ElAutocompleteTests.CloseAsyncCollapsesOpenDropdown`(1)，全绿；demo `/collapse`、`/autocomplete` 新增对应演示段；组件库与 demo 构建 0 错误。

- **`ElAutocomplete` / `ElDatePicker` 按 EP 官方文档补齐 exposes 与透传参数（#148 Tier B，2026-10-01）**：
  - **ElAutocomplete**：新增 `ShowAsync()`（对齐官方 `open()` exposes），根据 `Suggestions`/`FetchSuggestions`/`FetchSuggestionsAsync` 重新拉取候选并弹出面板；已禁用 / 只读时为无操作。与既有 `CloseAsync()` 成对。
  - **ElDatePicker**：将面板已支持但对齐缺失的 EP 参数**透传**至 `ElDatePickerPanel` —— `ShowWeekNumber`（周数列，仅 date 类型生效）、`Footer`（底部插槽，优先级高于 `ShowConfirm` 默认「确定」按钮）、`ShowConfirm`（daterange 等场景的底部「确定」按钮）；新增 `ValueOnClear`（点击清除时回写的值，默认 null，对齐官方 `value-on-clear`，仅单选 date 类型生效，范围选择始终清空为 `[null,null]`）。既有 `ShowAsync`/`HideAsync`/`ClearAsync` 行为不变。
  - **测试 / 演示**：新增 `ElAutocompleteTests.ShowAsyncOpensDropdown`(1) + `ElDatePickerTests` 4 例（ShowWeekNumber/ShowConfirm/Footer/ValueOnClear），共 16 例全绿（ElAutocomplete 6 + ElDatePicker 10）；demo `/autocomplete` 补 `ShowAsync` 按钮、`/datepicker` 补 ShowWeekNumber/Footer+ShowConfirm/ValueOnClear 三段；组件库与 demo 构建 0 错误。

- **`ElDateTimePicker` 按 EP 官方文档补齐透传参数与事件（#148 Tier B，2026-10-01）**：
  - **OnPanelChange**：类型由 `string` 更正为 `EventCallback<DateTime?>`（对齐官方 `onPanel-change`，2.2.0），并真正接线到 `ElDatePickerPanel.OnPanelChange`（面板翻月/翻年/选月/选年时携带切换后显示日期上抛；此前声明了参数却从未触发，纯 `string` 类型也无法承载日期）。新增桥接 `OnInnerPanelChangeAsync`。
  - **ValueOnClear**：类型由 `string` 更正为 `DateTime?`（对齐官方 `value-on-clear`，2.10.3），并在清除分支接入——单选清除时回写该值（默认 null），范围选择仍清空为 `[null,null]`（与 `ElDatePicker` 同构）。
  - **未做项（明确留档）**：`Persistent`/`AppendTo` 两个参数虽已声明，但底层 `PopupOption`/`DropDownOption` 目前**无对应字段**（连 `ElDatePicker` 也未接线），属弹层基础设施缺口，非本组件可独立透传，留待后续弹层专项；其余 `DefaultTime`/`DefaultStartTime`/`DefaultEndTime`/`Shortcuts`/`CellClassName`/`DisabledDate`/`TimeLabelText` 等已具备或属扩展能力，保持现状。
  - **测试 / 演示**：新增 `ElDateTimePickerTests` 2 例（OnPanelChange_FiresWhenNavigatingMonth / ValueOnClear_WritesCustomValueOnClear），整类 38 例全绿 0 失败；demo `/datetimepicker` 补 OnPanelChange、ValueOnClear 两段；组件库与 demo 构建 0 错误。

- **`ElTour` 按 EP 官方文档补齐组件级 `Placement` 兜底（#148 Tier B，2026-10-01）**：
  - **`ElTour` 新增组件级 `Placement` 参数**（默认 `bottom`，对齐官方 `placement`，2.4.2+），作为所有 step 的默认弹出位置；面板 class 改用 `EffectivePlacement`（单个 `TourStep.Placement` 优先，否则用组件级兜底）。
  - **关键修正**：`TourStep.Placement` 默认值由 `"bottom"` 改为 `null`（留空才回落组件级兜底）——原默认值导致组件级 `Placement` 永远被 step 的非空默认值覆盖、兜底形同虚设；默认渲染结果 `is-bottom` 不变（组件 `Placement` 默认 `bottom`），无视觉回归。
  - **测试 / 演示**：新增 `ElTourTests` 2 例（PlacementFallsBackToComponentDefault / StepPlacementOverridesComponentDefault），整类 6 例全绿 0 失败；demo `/tour` 补组件级 `Placement="left"` 演示段；组件库与 demo 构建 0 错误。

- **`ElDatePicker` 按 EP 官方文档补齐 `unlink-panels`（#148 Tier B，2026-10-01）**：
  - **`ElDatePicker` 新增 `UnlinkPanels` 参数**（默认 `false`，对齐官方 `unlink-panels`，2.5.0+）：范围选择时左右两个日期面板是否取消联动。默认 `false`（联动：翻动任一侧，另一侧自动保持相邻一个月）；为 `true` 时两侧独立翻页、互不影响。
  - **实现要点**：新增独立右面板锚点 `rangeDisplayMonthRight`；`OnRangePanelChangeAsync` 在 unlink 模式下只更新被翻那一侧的锚点、不碰对侧；`RangeRightMonth` 属性在 unlink 且右面板已独立翻过时返回固定右锚点，否则返回「左月 + 1 月（年范围 +10 年）」；`OpenDropDownAsync` 打开弹层时固化右面板初始月份（左月 + 偏移），使每次打开重新相邻、翻页后独立。默认行为（linked）零变化，无回归。
  - **测试 / 演示**：新增 `ElDatePickerTests` 2 例（LinkedPanelsKeepLeftFollowingRightNavigation 联动回归 / UnlinkPanelsKeepsLeftPanelFixedWhenNavigatingRight 独立），整类 12 例全绿 0 失败；demo `/datepicker` 补 `UnlinkPanels="true"` 范围选择演示段；组件库与 demo 构建 0 错误。

### EXT-UNRELATED 专项排查（2026-10-01）

- 对 #147 审计出的 `EXT-UNRELATED` 类「声明但未接线」参数做全量复核（脚本交叉比对 + 全仓同名排查），产出 `EXT-UNRELATED-AUDIT.md`。原始 35 项，结论：
  - **已修复 2 项**（`ElDateTimePicker.ValueOnClear` / `OnPanelChange`，本会话 #148 Tier B5 已接线）。
  - **弹层基础设施缺口 5 项**（`ElColorPicker.Persistent`/`AppendTo`、`ElDateTimePicker.Persistent`/`AppendTo`、`ElTour.AppendTo`）：底层 `PopupOption`/`DropDownOption` 无对应字段，单组件无法独立透传，留待弹层专项（补字段 or 删参数）。
  - **本组件可接线真缺陷 28 项**：其中低风险简单透传/开关 14 项（如 `ElTimePicker.TabIndex`、`ElSegmented.ValidateEvent`、`ElTransfer.PanelHeight`/`ShowAll`、`ElCheckbox.ValidateEvent`、`ElInputTag.Effect`、`ElRadio.BorderColor`、`ElRate.Readonly`、`ElImage.InitialIndex`、`ElTreeSelect.EmptyText`、`ElSlider.Height`、`ElCascader.Debounce`、`ElMention.Debounce`、`ElSelectV2.DefaultFirstOption`）建议首批；中/高风险 14 项（`ElCascader.ValueKey`、`ElDatePickerPanel.Editable`/`Shortcuts`、`ElMention.Position`/`PopperClass`/`Editable`、`ElRate.TextTemplate`/`OnFocus`/`OnBlur`、`ElSelectV2.ClearAfterSelect`/`AllowCreate`/`OnCreateOption`、`ElSlider.Vertical`、`ElTreeSelect.ExpandOnClickNode`）建议评估后分批，其中 `ElSelectV2.AllowCreate` 为高风险（需重写选项生成与提交链路）。
  - 全部 33 项待修项经核验「本组件前缀文件内除声明外零引用、razor 未消费」，`other_files` 命中的同名均为巧合（不同组件的同名参数），确系真未接线，无误报。

### Fixed

- **`ElDialogGrowService.Close()` 编程式关闭「点了没反应、关不掉」根因修复（2026-10-06）**：症状——demo `/dialog` 的「CanClose 拦截（关不掉）」示例里，点「强制关闭」按钮对话框赖在屏幕上不消失。根因：服务化对话框由 `ElDialogGrowHost` 按内部 `requests` 列表渲染，每个 `ElDialogGrowHostItem` 的 `<ElDialogGrow Visible="true" …>` 是**写死**的——它只能靠宿主把该项从列表摘除才消失，而宿主只在「对话框自己走完关闭流程（`OnCloseWithResult`）」时才摘除。`ElDialogGrowService.Close()` 却是一条完全独立的路径：它只从服务内部 `dialogs` 里移除请求并 `Complete(result)`，**从未通知宿主**，于是宿主照旧渲染那一项 → 视觉上关不掉（`await OpenAsync` 其实已经返回了，只是用户看到框还在）。修法：① `ElDialogGrow` 新增 `ForceCloseAsync(result)`（`CloseCoreAsync(result, skipCloseGuard: true)`），**绕过 `BeforeClose`/`OnBeforeClose` 拦截**但保留过渡、解锁 body 滚动与回传结果；② `ElDialogGrowRequest` 新增 `internal event Action<object> ForceCloseRequested`（+ `HasForceCloseHandler` / `RequestForceClose`）；③ `ElDialogGrowService.Close()` 改为 `CloseRequest()`：有宿主项监听时发 `RequestForceClose`，让该项走对话框自身的关闭管线并回报宿主摘除（无宿主时回退为直接完成任务，兼容单测）；④ `ElDialogGrowHostItem` 订阅该事件（`@implements IDisposable` + `OnInitialized`/`Dispose` 成对挂摘），收到后 `dialog.ForceCloseAsync(result)`，并由既有 `HandleClosedAsync → 宿主 HandleClosed` 收尾。
  - **为什么不能简单「先摘除再完成」**：直接 `requests.Remove` 会把 `ElDialogGrow` 一起销毁，`NotifyClosingAsync` 不再执行 → `elementLockBodyScroll(false)` 不跑，**页面滚动被永久锁死**；走 `ForceCloseAsync` 才能保证解锁。
  - **实测**（Playwright + msedge 打真实站，`ce_build/probe_dialoggrow_move.cjs`）：`canCloseBlocksEscape=true`（`CanClose=false` 时 × / Esc 确实关不掉，语义未被破坏）→ 点「强制关闭」后 `forceCloseWorked=true`、`document.body.style.overflow` 回到未锁定态、**0 pageerror**。
  - **bUnit 回归**：`ce_build/run_dialog_tests.py` 的 `ElDialogTests|ElDialogGrowTests|ElDialogGrowEnhancedTests|ElDialogGrowServiceTests|ElDrawerTests` **54/54 全绿**。

- **演示站：把「ElDialogGrow 声明式对话框」从 `/grow` 移到 `/dialog`（2026-10-06）**：`/grow` 的「ElViewerGrow 文本预览 + ElDialogGrow 对话框」一节语义混杂（文本预览属 viewer 家族、对话框属 feedback 家族），现拆开——`/grow` 只留「ElViewerGrow 文本预览」（并去掉页面里已无用的 `dlgVisible` 字段），ElDialogGrow 那个「诊断结论」对话框移到 `/dialog` 新增的「ElDialogGrow 声明式对话框（标题 + 正文 + Footer）」一节。
  - **实测**：`movedDialogOpened=true` / `movedDialogClosed=true`（点「关闭」后消失）、`growTextPreviewKept=true`、`growOldDialogRemoved=true`（`/grow` 已无「打开对话框」按钮）。

- **`ElTransfer` 中间操作按钮「移除 / 加入」垂直错位 5px 修复（2026-10-06）**：症状——demo `/misc` 穿梭框中间那两个横向并排的操作按钮一高一低，肉眼可见错位（「移除」高于「加入」）。根因：本库手写的 `.el-button{display:inline-flex}`（位于基底之后）覆盖了 EP 基底的 `.el-transfer__button{display:block}`，两个按钮因此变成横向并排；但 EP 遗留的 `.el-transfer__button:first-child{margin-bottom:10px}` 仍在生效 —— `.el-transfer__buttons` 是 `inline-block`、两个按钮 `vertical-align:middle`，这 10px 底边距使第一个按钮（移除）的**外边距盒**比边框盒高 10px、居中时整体上移 **5px（= 10px 的一半）**；第二个按钮（加入）则只拿到本库 `.el-button + .el-button{margin-left:12px}` 的横向间距（EP 的 `:nth-child(2){margin:0}` 被等特异性、更靠后的 `.el-button+.el-button` 覆盖）。修法：在 `index.css` 末尾 fix 段追加 `.el-transfer__buttons .el-transfer__button{margin-bottom:0}` —— 本项目穿梭框按钮为**横向**布局，该纵向底边距无意义；选择器等特异性（0,2,0 对 0,2,0）且位于文件末尾，按「同特异性后写者胜」覆盖 EP 原规则。
  - **实测**（Playwright + msedge 打真实站，`ce_build/probe_transfer_align.cjs` / `probe_transfer_regress.cjs`）：修复前 `移除 y=512 / 加入 y=517`（差 5px）、容器高 42（= 按钮 32 + 余量 10）；修复后两按钮**同为 y=517**、容器高回落 32，`/misc` 与 `/transfer` 两页共 5 组穿梭框的按钮组**垂直 delta 全为 0**、宽度与横向间距不变。
  - **交互回归**（`ce_build/probe_transfer_reverse.cjs`）：勾选左栏 → 「加入」由禁用变可用 → 点击后 `[5,0] → [4,1]`、文案变 `已选（右侧）：d1`；再勾右栏 → 「移除」可用 → 点击后回到 `[5,0]`。**0 控制台错误 / 0 pageerror**。

- **`ElDropdown` 悬停触发「弹出即消失 / 反复闪烁」根因修复（2026-10-06）**：症状——demo `/dropdown` 的「悬停触发（TriggerMode=Hover）」段，鼠标移上按钮菜单刚闪出来就没了，来回抖动。根因：每层下拉菜单都会在 `ElPopup` 宿主里额外交付一个「点击外部关闭」用的全屏透明遮罩 `div.v-modal`（`position:fixed; left/top:0; width/height:100%; opacity:0` —— `opacity` 不解除命中），其 `z-index` 高于触发器而触发器自身是 `z-index:auto`：菜单一出现，遮罩就压在仍停在触发器上的指针之下，Chrome 重算命中后给触发器补发 `mouseout`（`relatedTarget` 正是 `div.v-modal`），hover 分支据此排 `HideTimeout` 关闭 → 菜单消失 → 遮罩移除 → 指针又落回触发器 → `mouseover` 重新打开，形成「弹出即消失」的循环。click / contextmenu 不暴露该问题（`OnTriggerMouseOutAsync` 对非 hover 模式直接返回）。修法：`DropDownOption` 新增 `ShowShadow`（默认 `true`），`ElDropdown.ShowDropDownAsync` 在 `TriggerMode == Hover` 时不铺该遮罩（hover 的关闭语义本就由「触发器/面板 `mouseout` + `HideTimeout`」承担，无需遮罩），`ElPopup.razor` 的 `v-modal` 改为按该开关条件渲染。
  - **取证**（Playwright + msedge 打真实站）：修复前事件时间线 `over(t=88) → out(t=170, relatedTarget=div.v-modal) → over(t=337)`，2 秒内 42 个 50ms 采样点里菜单仅可见 **8 个**（≈400ms，即「闪一下」），指针下方元素在 `span ↔ div.v-modal` 间来回切；修复后 `ce_build/probe_dropdown2.cjs` 实测 **20/20 采样全程可见**、**`.v-modal` 出现 0 次**、指针移入菜单保持打开（`vis=1`）、移开后按 `HideTimeout` 正常关闭（`vis=0`）；同动作对照——click 触发的菜单**仍渲染 1 个 `.v-modal`** 且点击页面空白处仍能关闭（无回归）。
  - **bUnit 回归**：新增 `HoverTriggerDoesNotRenderOutsideClickShadow`（hover ⇒ `.v-modal` 数 0）/ `ClickTriggerKeepsOutsideClickShadow`（click ⇒ 恰好 1 个），对照只差 `TriggerMode`；`ElDropdownTests` **10/10 全绿**。

- **`ElDropdown` 悬停菜单「弹两次才正常 / 内部微动即收起」根因修复（2026-10-06）**：上一轮为修「弹出即消失」移除了 hover 遮罩，hover 菜单又出现「第一次 hover 后稍微动一下就收起、要再 hover 一次才稳定」的现象。根因：触发器根 `.el-dropdown` 与弹层 `ul` 绑定的都是 `mouseover`/`mouseout`（**会冒泡、且进入内部子元素时重新触发**）。`ElButton` 内部有 `span`/`i` 图标等子节点，鼠标在按钮内部从文字移到图标、或真人手抖轻微越过边界，都会先触发 `mouseout`（冒泡到 `.el-dropdown`）→ hover 分支排 `HideTimeout` 关闭计时；虽紧接着的 `mouseover` 会因 `hoverCloseVersion` 自增而取消该计时，但存在竞态窗口（尤其重渲染期间），表现为「弹一下就没、要弹两次才正常」。修法：触发器与弹层一律改用 `mouseenter`/`mouseleave`（**不冒泡、进入子元素不重触发**，只在真正进出整个元素时触发一次），彻底消除内部移动导致的误关闭。`ElDropdown.razor` 的 `@onmouseover/@onmouseout` → `@onmouseenter/@onmouseleave`；`ElPopup.razor` 弹层 `ul` 的 `@onmouseover/@onmouseout` → `@onmouseenter/@onmouseleave`（仅事件名替换，关闭/保持逻辑不变）。
  - **回归**：`ce_build/probe_hover_jitter2.cjs`（Playwright + msedge 打真实站）在按钮内部做 ±2/±5/±8px 抖动，**菜单全程保持（`dropped=false`）**，按钮尺寸 107×32（半高 16px，±8 仍在按钮内）；`ce_build/probe_hover_final.cjs` 三次连续 hover（含抖动 + 移入菜单 + 移开）全绿：首次即正常展开、抖动后不关、移入菜单保持、`afterLeave=false`（移开按 `HideTimeout` 关闭）、二/三次 hover 一次即开，**0 控制台错误**。

- **`ElDropdown` 分裂按钮（SplitButton）两半不紧贴修复（2026-10-06）**：症状——「分裂按钮」段的 `主要操作` 与 `▾` 之间是一条 **16px** 空隙，看起来像两个互不相干的按钮。根因：EP 把两半放进 `.el-button-group`（组内 `.el-button+.el-button{margin-left:-1px}` 叠边、仅外圈圆角、caret 侧 `border-left:none` 由 `::before` 画 1px 分隔线），本库 `ElDropdown.razor` 却把两个 `ElButton` 平铺在 `.el-dropdown` 下 —— 于是叠加了 `.el-button + .el-button{margin-left:12px}`，再叠加 Razor 保留的标签间换行空白（≈4px 词空格，因 `.el-dropdown .el-button-group .el-button{float:none}` 是行内盒布局；Vue 模板会自动压掉这段空白，Razor 不会）。修法：复用既有 `ElButtonGroup` 组件包裹两半（其 `Type/Size/Disabled` 级联仅在按钮自身为 default 时生效，而此处三者均显式传入 ⇒ 行为不变），并把两个 `ElButton` 写到同一标记行内消除词空格。
  - **实测**：修复前内间距 16px、根节点下直接挂 2 个 `button`、无 `.el-button-group`；修复后内间距 **-1px（叠边）**、`.el-button-group` 存在、两半不再直接挂在根节点下，两个示例之间仍是 demo-row 的 8px。
  - **bUnit 回归**：新增 `SplitButtonRendersAttachedButtonGroup`（组内恰好 2 个 `button`、含 `.el-dropdown__caret-button`、根节点下无裸 `button`）。
  - 顺带为 `ElDropdownTests.cs` 补 `using System.Threading.Tasks;`：既有用例靠 `InvokeAsync(Action)` 重载恰好不需要，新增的 `async Task` 用例需要。

- **`ElSteps` 子步骤状态「冻结在首帧」根因修复（2026-10-06）**：症状——demo `/steps` 点「下一步」时 `Active` 确实在 0→1→2→3 变化（`.demo-note` 文案跟着变），但四步的 `is-finish / is-process / is-wait` 一个都不动，步骤条整体没反应。根因：`ElStep` 的外观完全由父级 `ElSteps.Active` 在渲染时推导（`GetStepStatus`），而**它自己的参数恒不变化**——`Title` / `Description` 是字面量、四个 `RenderFragment` 参数全为 `null`，没有任何「每次渲染都会新建的 lambda / 片段」参与参数比较，于是渲染器判定该子组件参数未变、**跳过** `SetParametersAsync`，父级再渲染也拉不动它。对照实验证实这条边界：`ElCollapse` / `ElCarousel` 的子项参数里都带 `ChildContent` 片段（片段体每次执行都会新建委托 → 参数必然「变化」）→ 一直被重渲染，所以从未暴露该问题（实测手风琴同级互斥关闭正常、轮播箭头切换正常）。修法：`ElSteps` 经级联下发 `RenderToken`（每次渲染 `new` 一个实例，级联值变化走独立通知通道，可绕过「参数未变即跳过」），渲染树里补一层 `<CascadingValue>`；`ElStep` 用同名 `[CascadingParameter]` 接收，于是父级 `Active` / `Direction` / `Simple` / `AlignCenter` / `FinishStatus` / `ProcessStatus` 任一变化都能带动全部子步骤重算状态。
  - **回归**：bUnit `ElStepsTests.RecomputesStatusWhenActiveChanges`（修复前失败 → 修复后通过）+ 新增 `LastStepIsProcessWhenActiveIsLastIndex`（4 步、`Active=3` ⇒ 前三步 `is-finish`、末步 `is-process` 且图标为数字 `4`）/ `AllStepsFinishWhenActiveEqualsStepCount`（`Active=4` ⇒ 四步全 `is-finish` + 4 个 `is-status` 对勾）/ `CascadingTokenChangeForcesChildRerender`（钉住「参数未变」的子组件仅靠级联令牌变化就会重渲染这一机制），整类 **10/10 全绿**；`ce_build/probe_steps_audit.cjs`（Playwright + msedge 打真实站）逐档实测 `Active` 0→1→2→3 与回退到 0 的每一步 class 全部跟随、0 JS 异常。
  - **demo `/steps` 顺带把边界语义讲清楚**：`Active` 是「当前步骤下标」，`Active=3`（最后一步）再点「下一步」按定义不会有任何变化——这不是 bug，而是原先被 `Math.Min(3, …)` 静默夹住、按钮又不禁用，才被误读成「点了没反应」。现在「上一步 / 下一步」在边界自动置灰，并新增「全部完成（Active = 4）」「重置」两个按钮，示例块下方补一段语义说明：`Active` 为 0~3 时第 `Active+1` 步是 `is-process`（**深色**数字圆点 + 深色加粗标题，与蓝色 `is-finish` 的 ✓ 观感差异明显，容易被误读为「状态没变」），`Active` 等于步骤总数 4 时表示**全部完成**（四步全 ✓）。

- **`ElCarousel` 自动播放完全不生效根因修复（2026-10-06）**：症状——`/carousel` 全部 12 个轮播（含 `Interval="5000"` 那一段）**不会自动切换**，只有点箭头 / 指示器才会动。根因：定时器只在 `OnInitialized()` 与 `OnParametersSet()` 里经 `EnsureTimer()` 起停，而这两处都跑在**子项注册之前**（`Items.Count` 还是 0，`EnsureTimer` 里 `Autoplay && Items.Count > 1` 不成立 → 定时器建好但从不 `Start`），此后除非父级参数再变（页面重渲染）否则无人再调用 `EnsureTimer` → 自动播放永远不启动。修法：在 `AddItem` / `RemoveItem`（子项注册 / 注销完成之后）补调 `EnsureTimer()`——增删子项是唯一能让启动条件从「不成立」变「成立」的时机；子项减到 1 个时同一函数会 `Stop`，边界自动收口。
  - **回归**：`ce_build/probe_carousel_autoplay.cjs` 修复前实测 **12 个轮播在 12 秒内零次切换**（每档 `.is-active` 恒停在第一项 `A...`）；统一入口 `ce_build/serve_probe_audit.py`（Python 起站 → 轮询 200 → 跑探针 → 关站）可一次跑完多支探针。

- **编译期 warning 大扫除（2026-10-05）：`dotnet run` / `dotnet build` 时满屏 warning 已全部清零（0 警告、0 错误）**。分类修复如下：
  - **组件库真实代码修复（9 处）**：① `ElLink.razor.cs` XML 注释里裸写 `<i class="edit">` 导致 CS1570（转义为 `&lt;i ...&gt;`）；② `ElTree.Features.cs` 的 `Refresh()` 隐藏基类 `ElementComponentBase.Refresh()`（public virtual）触发 CS0114 → 改 `public override void Refresh()`；③ `ElTag.Features.cs` 的 `OnClose` 访问器转发到已过时的 `OnAfteClose` 触发 CS0618 → 访问器内加 `#pragma warning disable CS0618`（razor.cs 内部调用处早已有同类 pragma）；④ `ElTour` / `ElTreeV2` 在无可空上下文里用了 `InvokeAsync<object?>` 触发 CS8632 → 改 `InvokeAsync<object>`；⑤ `ElTable.razor` 的 `@onclick` 里 `SetCurrentRowAsync(row)` 未 await 触发 CS4014 → 改 `async e => { await ...; }`；⑥ `ElHtmlEditorGrow` 删未使用字段 `attachedPanelOwner`（CS0169）与只赋值不读的 `firstRenderDone`（CS0414）；⑦ `ElRate` 删未使用字段 `lastHoverValue`（CS0169）；⑧ `ElTreeV2` XML 注释里 `<see cref="AutoExpandAll"/>` 无法解析触发 CS1574 → 改纯文本；⑨ `ElMarkdownGrow` 的 `#pragma warning restore BL0006` 位置太早（包在 `GetFrames()` 后），导致后续 `frames` 读取仍冒 BL0006 → 把 restore 移到 `frames` 全部用完之后。
  - **SourceLink / Git 警告消除（3 个项目）**：`Element` / `Element.Markdown` / `Element.X` 三个 csproj 引用了 `Microsoft.SourceLink.GitHub`，但仓库不在 git 下，编译时刷 `Microsoft.Build.Tasks.Git` / `Microsoft.SourceLink.Common` 的「找不到存储库 / 源代码管理信息不可用」警告。在三个 csproj 的 PropertyGroup 加 `<EnableSourceLink>false</EnableSourceLink>` + `<EnableSourceControlManagerQueries>false</EnableSourceControlManagerQueries>` 消除（本地发布到 `../local-nuget` 不需要源链接）。
  - **demo 项目 nullable 噪声抑制**：demo csproj 已启用 `<Nullable>enable</Nullable>`，满屏 CS86xx（CS8618 字段未初始化 / CS8600 / CS8603 / CS8622 / CS8625 / CS8601 / CS8602 / CS8604 / CS8619 等，多为 Blazor 框架注入字段）。这些是示例项目噪声、非发布物，在 demo csproj 的 `NoWarn` 里补齐整组 `CS86xx` 可空性警告族（CS8600;CS8601;CS8602;CS8603;CS8604;CS8605;CS8610;CS8616;CS8618;CS8619;CS8620;CS8621;CS8622;CS8624;CS8625）抑制。
  - **验证**：增量 `dotnet build demo -c Debug -p:BuildAcceleration=false` → `BUILD_EXIT=0`、`0 个警告`、`0 个错误`；组件库 3 个项目均无 warning。

- **`ElNotification`「多次点击后整个页面卡死」根因修复（2026-10-04）**：症状——`/feedback` 的「ElNotification 通知栏」段连点若干次（默认 `Max=4` 时第 5 次）后整个页面无响应：Blazor WASM 的 UI 线程被占满，按钮点不动，只能刷新。根因：`NotificationService.Show` 超出 `Max` 时用 `while (Notifications.Count >= Max) { CloseCore(Notifications[0]); }` 淘汰最早一条，但 `CloseCore` **只调 `OnCloseSync` / `OnClose` 回调、并不从集合移除**，`Notifications.Count` 恒不变 → `while` 条件恒成立 → **同步死循环**。（同文件 `CloseAll` 因循环后紧跟 `Notifications.Clear()` 才幸免；`MessageService.Show` 早已写对，源码里还留着「必须真的从集合里 Remove」的警告注释。）修法：改为与 `MessageService.Show` 一致的实现——循环内 `Notifications.RemoveAt(0)` 真正移除，再依次触发 `OnCloseSync` / `OnClose`（`_ = oldest.OnClose()`）。移除会经 `ObservableCollection.CollectionChanged` 通知 `ElPopup` 宿主 `RepositionNotifications()` + 重渲染，剩余通知自动上移，语义与 EP 一致。
  - **回归**：`test/Element.ComponentTests/ElNotificationTests.cs` 新增 `Max_Eviction_RemovesOldest_AndDoesNotHang`（后台线程 + 3s 超时，把「死循环」转成可读失败；并断言被淘汰项触发 `OnClose`、集合长度为 2、最新项在列），连同既有 `Max_LimitsSimultaneousNotifications`（旧实现在该用例上会直接挂死）共 **ElNotificationTests 18/18 全绿**。
  - **浏览器实测**：`ce_build/probe_notif_freeze.cjs`（Playwright + msedge 打真实站）——连点「不自动关闭（duration=0）」**10 次**后页面仍即时响应、通知稳定淘汰为 **4** 条，再点 3 次仍稳定为 4 条；依次点 6 个不同类型通知后只剩最后 4 条（Primary/Success 已被淘汰、「基础用法」仍在）；`全部关闭` 归零；**0 JS 异常**。
  - 顺带修正同一测试文件内 `UpdateOffsets_RepositionsOnlyThatDirection` 的**过期断言**：原断言 `UpdateOffsets(TopRight)` 会把 BottomRight 的通知也挪到 80px，与用例名和实现都矛盾。查 EP 源码 `packages/components/notification/src/notify.ts` 的 `updateOffsets(position = 'top-right')` 确认其**只遍历 `notifications[position]`**（仅重排该方向）→ 实现无误、断言过期；已改为「另一方向保持 `bottom:0px`，再显式调一次 `UpdateOffsets(BottomRight)` 才变 80px」。

- **`ElTableV2` / `ElTreeV2` 虚拟滚动「首次滚动到底部多次闪烁/回跳」根因修复（2026-10-04）**：症状——`/misc` 的 ElTableV2（200 行）第一次把鼠标移到表格上滚动时，滚动位置反复跳动闪烁。根因：`MainLayout` 在文档级捕获 `pointerover` 挂了 `elementBindFadeScrollbar`（细滚动条「悬停才显形」），它对**任意真实滚动容器**做 `overflow:auto → hidden → auto` 的跨帧重建来强制 Blink 重绘 `::-webkit-scrollbar`。但 ElTableV2 的滚动容器 `.el-table-v2` 正是真实滚动容器，且虚拟滚动靠 `padding` 撑开总高度、渲染窗口随 `scrollTop` 平移——`overflow:hidden` 那两帧里 Blazor 仍按最新 `scrollTop` 重渲染（窗口已变），2 帧后 `repaint` 把 `scrollTop` **钉回切换前的旧值**，于是「视觉锚点」与「渲染窗口」错位 → 滚动位置回跳/闪烁（真实浏览器里配合惯性滚动会连续多次发生，即用户看到的「多次闪烁」）。实证：`ce_build/diag_tablev2_fade.cjs` 让表格滚动中派发 `pointerover` 触发该逻辑，修复前 `scrollTop` 被从 3600 钉回 3000（`YANK=true`），修复后保持 3600（`YANK=false`）。修法：`repaint` 新增 `isVirtualScroller` 守卫——命中 `.el-table-v2` / `.el-tree-v2__body` / `.el-tree-v2` 时**只钉 `scrollbar-gutter:stable`（避免悬停时宽度抖动），不做 `overflow` 切换重建**，把滚动位置完全交给虚拟列表自身管理。另在 `.el-table-v2` 与 `.el-tree-v2__body` 加 `overflow-anchor:none`（虚拟滚动标准防护：禁用浏览器滚动锚定，避免其自动微调 `scrollTop` 与组件渲染窗口打架）。虚拟容器滚动条改为常显（不再走 hover 重建），对数据网格是更稳妥的默认。回归：`ce_build/diag_tablev2_bottom.cjs` 滚到底部正确渲染 190–200 行（末行 id=200）、`pageerror`/console 错误 **0**；`diag_tablev2_fade.cjs` **YANK=false**。

- **`ElInputNumber` 清除图标改为「悬停才显示」（2026-10-03）**：组件逻辑本身已是 EP 交互 —— `ShouldShowClearIcon => Clearable && !IsDisabled && !Readonly && Value.HasValue && (ShowClearIcon || isHovering)`，即默认**悬停（`isHovering`）才出现**，`ShowClearIcon` 才是「常显」开关（bUnit `ClearableWithoutShowClearIconRendersOnlyOnHover` 早已锁定该语义）。问题出在 demo：`/basic` 的用例写了 `ShowClearIcon="true"`，导致圆圈叉**常驻**，与 `+` 按钮同屏挤成「两个按钮」的观感。修法：demo 去掉 `ShowClearIcon`（保留 `Clearable="true"`）→ 平时不显示、鼠标移入才出现、移出即消失；并补一个 `Readonly="true"` 实例作为只读能力的活样例。
  - 同步澄清：**`ElInputNumber` 有只读属性** `Readonly`（`bool`，默认 `false`，对齐 EP 官方 `readonly`（2.2.16，原生 readonly 语义））。为 `true` 时：input 打上 `readonly` + `aria-readonly="true"`，± 步进按钮加 `is-disabled`，键盘步进（`OnKeyDownAsync`）/ 点击步进（`StepValueAsync`）/ 清除（`ClearAsync`）全部拦截，且 `Clearable` 也不再渲染清除图标。
  - 取证：`demo/probe-inputnumber-clear-hover.cjs` **11/11 通过** —— 静止 `/basic` 的 `#c4-num` 内 `.el-input__clear` 数量 `0`；悬停后 `1` 且可见；**悬停 1.2s 期间图标稳定不闪烁**（防抖生效，原实现会疯狂抖动）；移开再回 `0`；悬停点击叉后值 `"5" → ""` 且图标消失；只读实例 `readonly` 属性存在、`aria-readonly="true"`、± 按钮均 `is-disabled`、`ArrowUp` + 键入 `99` 后值仍为 `33`、悬停也不出清除图标；JS 异常 0。

- **`ElInputNumber` 悬停清除图标「疯狂闪烁、点不中」根因修复（2026-10-03）**：上一轮探针实测暴露真正的 bug——鼠标移入输入框后，清除图标时隐时现、`element was detached from the DOM`、`html intercepts pointer events`，根本点不到叉。根因：`ElInputNumber` 沿用了**朴素 `onmouseover`/`onmouseout`**，而 Blazor 只有会冒泡的 `mouseover`/`mouseout`（没有 `mouseenter`/`mouseleave`）。鼠标从输入区移向清除图标时，会先冒泡 `mouseout`（离开输入区 → 置 `isHovering=false` → 图标卸载）再冒泡 `mouseover`（进入图标 → 又置 `true` → 图标重建），一个像素的抖动就触发反复卸载/重建，表现为「图标闪烁 + 点不中」。修法：把 `ElInput` 已验证的同款**防抖式悬停离开**搬过来——`OnWrapperMouseOutAsync` 不再立即置 false，而是起一个 60ms 的 `CancellationTokenSource` 延迟；若这 60ms 内又移入（`OnWrapperMouseOverAsync` 或再次移入图标）就 `Cancel` 掉这次离开，等价于 `mouseleave` 语义。字段：`isHovering` / `hoverLeaveCts` / `const HoverLeaveDelayMs = 60` / `CancelPendingMouseLeave()`，接线改为 `@onmouseover="OnWrapperMouseOverAsync" @onmouseout="OnWrapperMouseOutAsync"`。组件库构建 0 错误，探针 11/11 验证悬停稳定。

- **`docs/组件使用说明.md` 8 处 `bool` 被写成退格控制符（2026-10-03）**：ElInput 参数表里 `Disabled`/`Readonly`/`Clearable`/`EnableClearButton`/`ShowPassword`/`ShowWordLimit`/`Autofocus`/`ValidateEvent` 的类型列实际字节是 `\x08ool`（`\x08` = 退格），即某次生成时字符串字面量 `\b` 被解析成了真·退格字节，丢掉 `bool` 的首字母 —— 渲染出来是 `ool`。已按字节级替换 `\x08ool` → `bool`（纯 ASCII，不动中文，保留原 UTF-8），全仓 `.md/.razor/.cs/.json` 复查后 `\x08` 计数归零。

- **`ElInputNumber` 清除图标压在步进按钮上（「看起来是两个按钮」）+ 步进按钮边框归一（2026-10-02）**：两处根因。
  1. **图标重叠**：清除图标 `.el-input__clear` 定位 `right:10px`，而 increase/decrease 按钮是 `right/left:0; width:32px` 的绝对定位兄弟 —— 实测命中测试显示 `x ∈ [右边缘-24, -12]` 命中的是 `i.el-input__clear.el-icon-circle-close`，即**圆圈叉压在 + 按钮上**，两个图标叠成一个「⊕」被误看成「两个按钮」。
  2. **按钮边框**：按钮原用 `border: var(--el-border)` 四边全画 + `top:0; height:100%`，与 `.el-input__inner` 自身的边框完全重合，视觉上像「外挂在输入框边上的独立按钮」。
  修法（对齐 EP）：① 按钮改为 `top:1px; bottom:1px; left/right:1px` 嵌进输入框边框内，只画**内侧分隔线**（decrease 用 `border-right`、increase 用 `border-left`；`is-controls-right` 时 increase 追加 `border-bottom` 与下按钮分隔）；② 清除图标让位到按钮左侧 `right:42px`（= 按钮 32px + 10px 间隙）、`z-index:2`，无步进按钮（`is-without-controls`）时回退 EP 的 `right:10px`；③ 补 EP 的悬停联动：悬停步进按钮时输入框边框同步变 `--el-color-primary`。同类问题一并修掉分页跳页框：`.el-pagination__jump .el-input-number__increase/decrease` 原写死 `border: 1px solid`（四边全画）→ 与容器边框叠成「框中有框」，现交由通用 `is-controls-right` 段提供左/下分隔线。
  取证：`demo/probe-inputnumber.cjs` **8/8 通过** —— `inc.right=490.00 = root.right-1`、`inc.h=30.00 = 32-2`、increase 边框 `t/r/b/l = 0/0/0/1px`、decrease `0/1/0/0px`、clear 与 increase 重叠 `-9px`（不再相交）、clear 中心点命中 `el-input__clear`；复用场景实测按钮几何：Slider 带输入框 `32×30` 仅 `border-right`、右侧竖直步进 `32×16` 为 `border-bottom+left`、分页跳页框 `20×14` 为 `border-left+bottom`；bUnit `~SelectV2|~InputNumber` **28/28 通过**。

- **`ElSelectV2` 后缀图标对齐 EP「同位互斥」（2026-10-02）**：清除图标原为 `position:absolute; right:30px` 且**常显**，与后缀箭头（`right:5px`，宽 25px）并排且**间距为 0** ⇒ 实测 `clear 1330..1355` / `caret 1355..1380`，两个图标严丝合缝贴在一起，观感同「两个按钮」。EP 官方语义（`showClose = clearable && !disabled && inputHovering && hasModelValue`，见 select.vue）是**同一个 `.el-select__caret` 位置上的互斥切换**：仅 hover + 有值时出现圆圈叉，且此时箭头不渲染（EP 社区把「叉与箭头并存」明确视为图标重复 bug）。修法用**纯 CSS**实现同一交互（DOM 不变，bUnit 断言不受影响）：`clear` 移到与箭头同位的 `right:5px`、默认 `display:none`，`.el-select-v2:hover / :focus-within` 时显示 clear 并隐藏箭头（`loading` 图标 `:not(.el-icon-loading)` 不参与切换）；`:focus-within` 比 EP 多覆盖了键盘用户。
  取证：`demo/probe-selectv2-clear.cjs` **6/6 通过** —— 静止时 `clear.display=none` 且箭头 `block`；hover 时 `clear.display=block`、箭头 `none`；**同位实测** hover 时 `clear.cx=1367.5` 与静止时 `caret.cx=1367.5` 完全一致；hover 后点击叉可清空（`"香蕉 Banana" → ""`）。

- **全站「浮层图标重叠」体检（2026-10-02）**：新增 `demo/probe-icon-overlap.cjs`，遍历 28 个 demo 页面，对 `.el-input__clear` / `.el-input-number__increase|decrease` / `.el-input__password` / `.el-select__caret` / `.el-input__count` 等浮层图标做两两矩形相交判定，并附带「步进按钮是否多画边框」检查。修复前命中 `/select-v2`、`/form2` 各 1 处（`el-select-v2__clear` ∩ `el-input__suffix-inner` 8×32px），修复后 **0/28 页面有问题**（其余如 `/input` `/form` `/pagination` `/cascader` `/date-picker` `/slider` 等均为 OK，JS 异常 0）。

- **`ElScrollbar` thumb 尺寸/位移修复：滚动条永远到不了底部（2026-10-02）**：`thumb` 用 `transform: translateY(ratio%)` 定位，而 **`translate` 的百分比是相对 thumb 自身尺寸、不是相对轨道** ⇒ 滚到底时 thumb 只挪动了自身高度（如 20%），底边永远停在轨道中部上方。另外 thumb 高度被写死 `MinSize`（默认 20%），没有按「视口 / 内容」比测算。两处一起修：`thumb 长度 = max(minSize, clientHeight / scrollHeight × 100)`，`translate 位移 = ratio × (100 - size) / size`（`MovePercent`），使 ratio=100 时 thumb 底边恰好贴到轨道底部。首帧由 `OnAfterRenderAsync` 调 `elementScrollbarGetState` 测算，未取得状态时退回 `MinSize`（不破坏 `MinSize_ThumbWidthStyle` 等既有断言）。
  取证：Playwright 探针 `demo/verify-scrollbar-thumb.cjs` **7/7 通过** —— `clientH=140 / scrollH=344` ⇒ thumb 高 `55.3px`（= `140/344 × 136`，而非旧的写死 20%）、滚到底 `scrollTop 204/204` 时 `thumb 下边 = 轨道下边 = 2032.0px`（贴合）、中间位置 `thumb 上边 = 轨道上边 + 0.5×(轨道-thumb)`；bUnit `ElScrollbarTests` **34/34 通过**。

- **`ElScrollbar` 滚轮穿透修复（2026-10-02）**：自定义滚动条 `.el-scrollbar__bar` / `__thumb` 是 `.el-scrollbar__wrap` 的**兄弟节点**（浮在 wrap 之上，不参与 wrap 的滚动），鼠标停在滚动条上滚轮时事件目标是 thumb → wrap 收不到 `wheel` → 事件沿 `.el-scrollbar` 一路冒泡到外层滚动容器（demo 的 `.demo-main`）⇒ **滚动条明明停在中间、内层纹丝不动，整个页面却跟着滚**。新增 JS `elementScrollbarBindWheel(root, wrap)`：在根容器接管「落在 `.el-scrollbar__bar` 上」的滚轮，按 `deltaMode` 归一化后手动写入 wrap 的 `scrollTop` / `scrollLeft` 并 `preventDefault()`；落在 wrap / view 上的滚轮不进该分支，仍走浏览器原生滚动（行为不变）。绑定幂等（`__elScrollbarWheelBound` 标记 + `__elScrollbarWheelHandler` 引用，便于测试解绑对照）；`ElScrollbar.razor` 根节点新增 `@ref="RootElement"`，由 `OnAfterRenderAsync` 调用（异常静默，兼容 bUnit / 预渲染）。
  取证：Playwright 探针 `demo/verify-scrollbar-wheel.cjs`（15 断言，**A/B 对照**）——接管开启时「滚动条上滚轮」→ wrap `scrollTop 0→100`、外层 `1324→1324`（不动）；**临时解绑**后同一坐标 → wrap `100→100`（不动）、外层 `1324→1424`（**根因复现**）；`reload` 走真实绑定路径复测 `0→90` / 外层不动 ✓；组件外区域滚轮仍正常滚外层 ✓。bUnit `ElScrollbarTests` **34/34 通过**。

- **`ElTypographyTitle` 渲染崩溃修复（2026-09-30）**：原 `TitleStyle` 的 `AddIf(LineClamp.HasValue && LineClamp.Value > 0, $"--el-text-line-clamp:{LineClamp.Value}")` 中插值串 `$"{LineClamp.Value}"` 在 `AddIf` 求值前被**急切求值**，一旦 `LineClamp` 为 `null`（即绝大多数标题的默认状态——未设置行截断），便抛 `System.InvalidOperationException: Nullable object must have a value`，使任何未设 `LineClamp` 的 `ElTypographyTitle` 在渲染时直接崩溃。改为仅在 `LineClamp` 非空前计算样式串：
  ```csharp
  var clampStyle = LineClamp.HasValue && LineClamp.Value > 0
      ? $"--el-text-line-clamp:{LineClamp.Value}"
      : null;
  return HtmlPropertyBuilder.CreateCssStyleBuilder()
      .AddIf(clampStyle != null, clampStyle).Add(Style).ToString();
  ```
  由新增 `test/Element.ComponentTests/ElTypographyTests.cs`（11 个 bUnit 用例）覆盖：修复前因该崩溃 9 例失败，修复后 **11/11 全绿**；`Element.Blazor.Demo.csproj` 与 `Element.ComponentTests.csproj` 均 0 错误。

- **`ElNotification` 对齐 Element Plus Notification（官方文档 + 源码级对齐，2026-09-29）**：通知由 `NotificationService`（`ElNotification` 是其薄壳）+ `ElPopup` 宿主渲染，本次按 EP `notification.vue` / `notification.scss` 补齐能力面与 DOM：
  - **新增选项**：`onClick`（点击回调）、`pauseOnHover`（悬停暂停计时，默认 true）、`progress` + `progressColor`（底部进度条，2.14.4）、`closeIcon`（自定义关闭图标，2.9.8）、公开的 `zIndex`（0 = 自动分配）、`Id`；新增服务方法 `Primary()`（EP 2.9.11 第五种类型）与 `UpdateOffsets(position)`（对应 EP 2.10.5 的 `ElNotification.updateOffsets`）。
  - **修正默认值与语义**：`type` 改为**可空** `MessageType?`（EP 默认是空串 ⇒ **默认通知不显示图标、不带类型色**，原先默认 `Info` 会凭空多一个 info 图标）；`offset` 默认由 16 改为 0（EP 默认 0）。
  - **DOM 对齐**：类型修饰类 `el-notification--{type}` 从根节点**移到图标元素**上（EP 是 `[ns.e('icon'), typeClass]`，挂在根上时 EP 的「用修饰类给图标上色」规则永远命中不了）；正文用 `<p>` 包裹；无标题时正文 `margin:0`（EP 的 `!!title ? undefined : { margin: 0 }`）；水平定位 `right/left:16px` 交回 CSS（原先写死内联会压制主题样式）。
  - **修掉真实缺陷**：`DangerouslyUseHTMLString` 是**死参数**（声明了但渲染端从不读，HTML 被转义成文本），现已真正走 `MarkupString`。
  - **计时器重写**：100ms 一拍，`pauseOnHover` 悬停时把已走时长累加、移开按剩余时长续跑；进度条百分比 = 剩余/总时长（EP 同公式），未开进度条时不触发重渲染（避免每 100ms 重绘弹层宿主）。
  - **CSS**：补 `.el-notification__progress*`、primary 图标色、`.el-notification__group{flex:1;min-width:0}`（缺 `min-width:0` 时长英文单词会撑破 330px 的框）。
  - **演示**：`/feedback` 新增「ElNotification 通知栏」段（基础 / 五种类型 / 四角位置 / offset / HTML / 进度条+悬停暂停 / 隐藏关闭 / 点击回调 / 分组 / 全部关闭）。
  - 验证：`test/Element.ComponentTests/ElNotificationTests.cs`（bUnit）+ `demo/probe-notification.cjs`（端到端 26 项，含真实 hover 暂停与进度条随时间收缩）；文档 `docs/notification-alignment.md`。

- **`ElCard` 继续对齐 Element Plus Card（官方文档，2026-09-29）**：在保留既有扩展（分隔线 / 分段高度 / 底色 / `HeaderCls`/`BodyCls`/`FooterCls`）基础上，补齐 EP 的 `header` / `footer` **字符串属性**：
  - 新增 `HeaderText`（`header`）/ `FooterText`（`footer`）：与 `Header` / `Footer` 插槽并存时**插槽优先**（与 EP `<slot name="header">{{ header }}</slot>` 行为一致）；header/footer 仅当「插槽非空 或 字符串非空」才渲染。
  - `Shadow`（Always/Hover/Never）、`BodyStyle`（EP `body-style`）、`HeaderCls`/`BodyCls`/`FooterCls`（对应 EP `header-class`/`body-class`/`footer-class`）保持并确认一致。
  - **演示**：`/card` 新增 `CardDemo.razor`（基础用法 / Header+Footer 插槽 / 简单卡片 / 图片卡片 BodyStyle / 三种阴影 / 自定义 Class / 新增能力自检）。
  - 验证：`demo/probe-card.cjs` 端到端 **10/10**（多卡片、三阴影类、HeaderText/FooterText 渲染、图片 data URI、自定义 Class 落到对应段、插槽内嵌 button、默认分隔线、简单卡片无 header、零 pageerror）。

- **`ElCarousel` 继续对齐 Element Plus Carousel（官方文档 + `carousel.vue`/`carousel.scss` 源码级对齐，2026-09-29）**：在保留既有扩展（`ImageFit`、`IndicatorClass`/`ArrowClass`、内置图片预览 `PreviewOnClick`/`PreviewLoop`/`PreviewImages`/`PreviewImagesChanged`/`OnPreviewChanged`）基础上，补齐 EP 全量能力面：
  - **新增属性**：`Loop`（默认 true，是否循环）、`Type`（`CarouselType.Card` 卡片模式，含二级卡缩放 `CardScale` 默认 0.83）、`Trigger`（`CarouselTrigger.Hover`/`Click`，指示器触发方式，默认 Hover）、`MotionBlur`（2.6.0 动态模糊）、`Name`/`Label`（挂在 `ElCarouselItem` 上：指示器的文本）。
  - **暴露方法对齐 EP Exposes**：`ActiveIndex`（public，`el-carousel` 暴露的 `activeIndex` 2.7.8）、`SetActiveItem(下标或 Name)`（对应 EP `setActiveItem`）、`Prev()`/`Next()`（对应 EP `prev`/`next`）。
  - **卡片模式（Type=Card）**：容器 `overflow:visible`，各卡 `width:50%`、`left:50%`，按与激活项的偏移 `translateX(calc(-50% + N%)) scale(...)` 排布（非激活缩放 `CardScale`），**点击两侧卡片直接切换**（EP 卡片模式交互核心）；`.el-carousel__item` 不再 `display:none`，侧卡常驻可见。
  - **Loop=false 行为**：到边界停住，边界箭头（`LeftArrowVisible`/`RightArrowVisible`）自动隐藏。
  - **auto-height**：`Height="auto"`（或留空）时加 `el-carousel--auto-height`，容器高度跟随当前激活页（EP 同名特性）。
  - **Indicator 文本**：`ElCarouselItem.Label` 非空时，指示器按钮内渲染 `<span>` 文本（`el-carousel__indicators--labels`）。
  - **动态模糊（MotionBlur）**：渲染 `elCarouselHorizontal`/`elCarouselVertical` SVG 滤镜，`@ontransitionstart/end` 在切换过程中为容器加 `filter:url(#elCarousel…)`。
  - **修饰类**：`el-carousel--card` / `--motion-blur` / `--auto-height` / `--vertical-outside`（垂直 + outside 指示器走 flex 布局）。
  - **演示**：`/carousel` 新增 `CarouselDemo.razor`（基础 / click 触发 / outside / arrow-always / vertical / card / motion-blur / auto-height / label / loop=false / 编程式切换 / 图片预览）。
  - 验证：`demo/probe-carousel.cjs` 端到端 **22/22**（11+ 走马灯、基础 4 项、卡片模式侧卡可见且可点击切换、vertical/outside/arrow-always/motion-blur/auto-height/label/loop=false 类与 DOM、编程式 Prev/Next/SetActiveItem("c")、图片预览打开、零 pageerror）。

  - **回归修复（图片预览滚轮缩放丢失）**：上轮重写 `ElCarousel.razor` 时把内置预览 lightbox 的**滚轮缩放**能力丢了（原预览容器有 `@onwheel` 缩放，重写后只剩点击放大/箭头/关闭）。现补回，且与 `ElImage` 内置 viewer 行为对齐：**`@onwheel` 滚轮缩放（向上放大/向下缩小，0.2x~7x，步长 0.15）**、**拖动平移**（scale>1 时按住拖动）、**双击在 1x/2x 切换**、`transform` 居中原点、`overscroll-behavior:contain` 防页面滚动；打开/切换图片时重置缩放。新增 `previewScale`/`previewOffsetX/Y` 等字段与 `OnPreviewWheelAsync`/`OnPreviewDoubleClickAsync`/`OnPreviewPointerDown/Move/UpAsync`/`ResetPreviewView`/`PreviewImageStyle`。验证 `demo/probe-carousel-wheel.cjs` **7/7**（初始 scale(1) → 上滚 1.75 → 拖动偏移(8,4) → 下滚回 ~0.85 → 双击 2x → ESC 关闭 → 零 pageerror）。

- **`ElDescriptions` 对齐 Element Plus Descriptions（官方文档 + `description.vue`/`descriptions-cell.ts` 源码级对齐，2026-09-29）**：原实现非边框模式用 grid `div`+`gap`（违反 CSS 单一真源铁律且不符 EP DOM），且缺 `RowSpan`/`Align`/`LabelAlign`/`ClassName`/`LabelClassName`/`LabelWidth`/`Column` 自动折行。本次按 EP 全量对齐：
  - **DOM 真源对齐 EP**：无论边框与否始终渲染 `<table class="el-descriptions__table [is-bordered]">`（去掉原 grid div）；垂直方向（`Direction=Vertical`）无论有无边框都渲染**两个 `<tr>`**（先 `th` label 行、后 `td` content 行），水平方向一个 `<tr>`（边框时 `th` label + `td` content，非边框时单个 `td.el-descriptions__cell` 内含 label `span` + content `span`）。
  - **跨列/跨行算法对齐 EP `getRows()`**：`RowSpan` 通过 `rowspanTemp` 占用后续行的列位；最后一项自动填满本行剩余列位（`lastSpan = column - (totalSpan % column)`）；`colspan`：水平边框 content = `span*2-1`、非边框 = `span`；垂直 content `rowspan = RowSpan*2-1`。
  - **新增参数**：`ElDescriptions` 增 `Column`（默认 3、超量自动折行）、`Size`（large/default/small）、`Colon`（默认 **false**，对齐 EP 官方无冒号）、`LabelWidth`（2.8.8 label-width）；`ElDescriptionsItem` 增 `RowSpan`（2.8.1）、`Align`/`LabelAlign`（left/center/right，`LabelAlign` 未设回落 `Align`）、`ClassName`/`LabelClassName`（EP class-name/label-class-name）、`LabelWidth`（优先级高于父级）、`Order`（排序）。
  - **类名/样式**：`is-bordered-label`/`is-bordered-content`/`is-vertical-label`/`is-vertical-content` 及对齐修饰 `is-center`/`is-right`；`LabelWidthResolved` 优先级为 本项 LabelWidth > Descriptions.LabelWidth > 本项 Width；标签宽度/最小宽度内联 style 经 `ElementCssUtility.NormalizeCssSize` 统一。
  - **CSS 单一真源**：`index.css` 末尾新增 `/* ==== ElDescriptions（对齐 Element Plus Descriptions）==== */` 段（header 16px/700、body 背景、`table-layout:fixed`、单元格 padding、非边框 label `margin-right:8px`、垂直 label `display:block;margin-bottom:8px`、边框 label 背景 `#fafafa` 等，**无 `gap:`/`is(`**）。
  - **演示**：新增 `demo/.../Pages/DescriptionsDemo.razor`（`@page "/descriptions"`，含基础/三尺寸边框/垂直带&不带边框/Column=2·4/Span=2 跨列/RowSpan=2 跨行/Align·LabelAlign 对齐/LabelWidth·Width·MinWidth/TitleContent·Extra·LabelContent 插槽/Colon 开关共 10 段），并在 `DemoNavigation.MockSection` 加 `/descriptions` 入口（`el-icon-tickets`）。
  - 验证：`test/Element.ComponentTests/ElDescriptionsTests.cs`（bUnit **10/10**）+ `demo/probe-descriptions.cjs`（端到端 **11/11**：标题、is-bordered、is-vertical-label≥3、Span→colspan=3、RowSpan→rowspan=2、is-center、LabelWidth=120px、冒号、TitleContent 插槽、无边框 .el-descriptions__cell、零 pageerror）。

- **`ElEmpty` 对齐 Element Plus Empty（官方文档 + `empty.vue`/`img-empty.vue` 源码级对齐，2026-09-29）**：原实现默认插画为手写简版 SVG（硬编码配色），且 `Description` 默认 `"No Data"`、`ImageSize` 为字符串，不符 EP。本次按 EP 全量对齐：
  - **DOM 真源对齐 EP**：`<div class="el-empty">` → `el-empty__image`（style=`width:{ImageSize}px`）内 `img`（设 `Image` 时优先）/ `ImageContent` 插槽 / 默认插画；`el-empty__description` 内 `DescriptionContent` 插槽或 `<p>`；`el-empty__bottom` 内 `ChildContent` 默认插槽。优先级对齐 EP：`Image` 显式 URL > `ImageContent` 插槽 > 默认插画。
  - **默认插画改为 EP `img-empty` 同款 SVG**：用 `var(--el-empty-fill-color-0..9)` 变量驱动配色（共 10 档），并在 `index.css` 的 `.el-empty` 上补默认变量值，从而支持官方文档所述的「整体换色」——用户只需 `:root { --el-empty-fill-color-0: red; … }` 即可换色（原硬编码配色不支持）。
  - **参数对齐 EP**：`Image`（`string` URL，默认 `""`）、`ImageSize` 改为 `int?`（单位 px，对齐 EP `image-size: number`）、`Description` 默认 `""` 并回落到本地化默认「暂无数据」（对齐 EP `t('el.table.emptyText')` 语义，与本项目 `ElTable` 空态文案一致）；保留 `ImageContent`/`DescriptionContent`/`ChildContent` 三插槽（映射 EP 同名校验插槽）。
  - **演示**：新增 `demo/.../Pages/EmptyDemo.razor`（`@page "/empty"`，含基础/自定义描述/自定义图片 URL/ImageSize=200/底部插槽/ImageContent 插槽/DescriptionContent 插槽/自定义 `--el-empty-fill-color-*` 配色/**ElTable 空状态内嵌 ElEmpty** 共 9 段），并在 `DemoNavigation.MockSection` 加 `/empty` 入口（`el-icon-box`）。
  - **ElTable 空状态能力确认**：`ElTable` 本就支持空状态——`EmptyText`/`EmptyTextOverride` 设纯文本，或用 `Empty` 插槽放任意内容；demo 第 ⑨ 段即把 `<ElEmpty>` 放进 `<ElTable><Empty>` 作为空数据占位。
  - 验证：`test/Element.ComponentTests/ElEmptyTests.cs`（bUnit **8/8**）+ `demo/probe-empty.cjs`（端到端 **12/12**：描述回落「暂无数据」、`--el-empty-fill-color-*` 变量引用、自定义 Description、图片 URL `<img>`、ImageSize→`width:200px`、底部插槽、ImageContent 自定义 svg、DescriptionContent 插槽、自定义配色区块、ElTable 空态内嵌 ElEmpty、ElTable 自定义文案、零 pageerror）。

- **`ElInfiniteScroll` 对齐 Element Plus Infinite Scroll（官方 directive 文档，2026-09-29）**：原组件能力已覆盖 EP `v-infinite-scroll` 指令的五个选项（`OnLoad`→`v-infinite-scroll`、`Disabled`→`infinite-scroll-disabled`、`Delay`→`infinite-scroll-delay`（默认 200）、`Distance`→`infinite-scroll-distance`、`Immediate`→`infinite-scroll-immediate`），本次按官方语义收口并增强：
  - **`Distance` 默认值 `50` → `0`**，对齐 EP 官方 `infinite-scroll-distance` 默认 `0`（滚动到最底部才触发）。
  - **加载态可视化**：`Loading=true` 时底部渲染 `.el-infinite-scroll__loading`，默认内容从纯文本升级为「转圈 spinner（CSS `el-infinite-rotate` 动画）+ 加载中文案」；新增 `aria-busy`/`aria-disabled` 无障碍属性；`LoadingContent`/`DisabledContent`/`LoadingText`/`DisabledText` 插槽与文案保留。
  - **CSS 单一真源**：`index.css` 新增 `/* ==== ElInfiniteScroll ==== */` 段（spinner + keyframe + loading 居中对齐），无 `gap:`/`:is(`。
  - **演示**：新增 `demo/.../Pages/InfiniteScrollDemo.razor`（`@page "/infinite-scroll"`，含基础用法/自定义加载·结束内容/Distance 阈值/关闭 Immediate/禁用状态 共 5 段），并在 `DemoNavigation.MockSection` 加 `/infinite-scroll` 入口（`el-icon-loading`）；`MiscDemo` 原有内联样例保留。
  - 验证：`test/Element.ComponentTests/ElInfiniteScrollTests.cs`（bUnit **7/7**，覆盖渲染/Loading 转圈/Disabled 文案/Immediate=false 不自动加载/Disabled 阻止滚动加载/到底触发加载/Height 样式）+ `demo/probe-infinite-scroll.cjs`（端到端 **13/13**：Immediate 首屏加载、滚动追加到上限、结束文案、转圈出现、自定义加载/结束文案、Distance 加载、Immediate=false 滚动触发、禁用态文案、零 pageerror）；既有 `ElDataDisplayRemainingTests` 全绿（5/5，未破坏）。

- **`ElPagination` 对齐 Element Plus Pagination（官方文档 + `pagination.vue`/`pagination.scss` 源码级对齐，2026-09-29）**：保留既有扩展（快速跳页越界收口、5–21 收口的 `PagerCount`、`Change(Page,Size)` 复合事件、按钮禁用态）基础上，按 EP 全量补齐分页布局与语义：
  - **layout 布局语法对齐 EP**：`Layout` 解析支持 `total` / `sizes` / `prev` / `pager` / `next` / `jumper` / `->`（右对齐分隔符，EP 用 `->`）/ `slot`（具名插槽占位，渲染 `ChildContent`）。新增 `MainParts` / `RightParts` 两个只读属性：按 `RightWrapper` 索引把布局拆成「左主区」与「右对齐区」。
  - **右对齐真实嵌套修复（关键 bug）**：原先用 Razor `@:` 字面量发射 `<div class="el-pagination__rightwrapper">` 包后续 part，但 Blazor 运行时只把 `@:` 当作**文本行**，浏览器里该 div 渲染为空 `<div class="el-pagination__rightwrapper">↵</div>`（total/sizes/jumper 实际落到 div 外）；**bUnit 因文本比较竟假阳性通过**。改为把每个布局元素抽成 `RenderFragment<PaginationLayoutPart> PartTemplate`，正文对 `RightParts` 用真实 `<div class="el-pagination__rightwrapper">@foreach (…){ @PartTemplate(part) }</div>` 包裹 —— DOM dump 证实 total/sizes/jumper 现在真在 rightwrapper 内。
  - **新增/修正语义**：`PageCount` 优先于由 `Total` 推算（EP `page-count` 存在即忽略 `total`）；`PagerCount` 收口为奇数且 5–21（EP `pager-count`，默认 7）；`HideOnSinglePage`（单页时整组件隐藏，EP `hide-on-single-page`）；`PrevText`/`NextText`（替换默认箭头图标，EP `prev-text`/`next-text`）；`PrevClick`/`NextClick` 事件（EP `prev-click`/`next-click`），与既有 `Change` 并存。
  - **演示**：`demo/.../Pages/PaginationDemo.razor`（`@page "/pagination"`，`total=1000`，含带背景色+总条数/其后所有元素右对齐/`layout` 含 `->`/`slot`/自定义 total/PageCount=12/PagerCount=9/只有一页时隐藏/事件 多段），`DemoNavigation.MockSection` 加 `/pagination` 入口（`el-icon-arrow-right`）。
  - 验证：`test/Element.ComponentTests/ElPaginationTests.cs`（bUnit **10/10**：含 `LayoutArrowPushesSubsequentPartsToRightWrapper`、`SlotTokenRendersSlotContent`、`PageCountTakesPriorityOverTotal`、`HideOnSinglePage_*`、`PrevTextReplacesArrowIcon`、`NextClickFiresEvents` 等）+ `demo/probe-pagination.cjs`（端到端 **20/20**：rightwrapper 内真含 `.el-pgress__total`/sizes/jumper、隐含 next 按钮 last 直点、零 pageerror）；既有 `PaginationSupportsLayoutSizesAndJumper` 未破坏。

- **`ElProgress` 对齐 Element Plus Progress（官方文档 + `progress.vue`/`progress.scss` 源码级对齐，2026-09-29）**：保留既有扩展（`TrackColor` 轨道色、`Content` 插槽（入参当前百分比）、`Format` 字符串、`Id`/`AriaLabel`、Line/Circle/Dashboard 三型 + `Status` 三态）基础上，按 EP 2.x 补齐全部能力面：
  - **新增参数**：`Striped`（条纹进度条，2.3.4）/ `StripedFlow`（条纹流动，须与 `Striped` 同真）/ `Indeterminate`（不确定模式，持续循环动画；内联宽度改 40% + `animation-duration` 由 `Duration` 控制）/ `Duration`（动画/流动速度秒，默认 3）/ `ColorFunction`（`Func<double,string>`，颜色随百分比变化，优先级高于 `Color` 字符串）/ `ColorStops`（`IEnumerable<ProgressColorStop>`，EP `color` 数组形态：直线型生成 `linear-gradient`、环形/仪表盘按当前百分比取对应停靠色）/ `FormatFunc`（`Func<double,string>` 文字格式化，优先级高于 `Format` 串）。
  - **颜色优先级统一**：`ColorStops`（最高）> `ColorFunction` > `Color` 字符串 > `Status` 状态色 > 默认主色（`#409eff`）；环形/仪表盘描边走 `ResolvedStroke`（数组时按 `ColorAtPercentage` 取当前停靠色，与状态色/默认色衔接）。
  - **DOM 坑修正**：`el-progress-circle` 类挂在**包裹 div**（`.el-progress-circle`），不是 `<svg>`；svg 本身仅 `path.el-progress-circle__track` / `path.el-progress-circle__path`，与 EP 完全一致（bUnit 选择器须用 `.el-progress-circle svg`，否则假阴性）。
  - **CSS 单一真源**：`index.css` 大 blob 已含全部 `.el-progress*` 基础样式（含 `is-striped`/`is-striped-flow`/`is-indeterminate`），本次不重复新增段，仅复用；`InnerModifierClass` 输出 `is-striped`/`is-striped-flow`/`is-indeterminate` 由 CSS 的 keyframe 消费。
  - **演示**：`demo/.../Pages/ProgressDemo.razor`（`@page "/progress"`，14 段：基础 0/50/100、TextInside+StrokeWidth、`Color` 串、`ColorFunction`、`ProgressColorStop` 渐变、Circle 0/25/75、Dashboard 10/50、`Content` 插槽、FormatFunc、Indeterminate+Duration、Striped/StripedFlow、`Status` 三态、交互 range 拖动），`DemoNavigation.MockSection` 加 `/progress` 入口（`el-icon-loading`）。
  - 验证：`test/Element.ComponentTests/ElProgressTests.cs`（bUnit **16/16**：含 `CircleRendersSvgAndClass`(`.el-progress-circle svg`)、`StripedAddsIsStriped`、`StripedFlowAddsIsStripedFlow`、`IndeterminateAddsIsIndeterminate`、`ColorStopsProducesGradient`、`ColorStopsPicksColorAtPercentageForCircle`、`FormatFuncOverridesText`、`ContentSlotRendered`、`PercentageClampedToRange`）+ `demo/probe-progress.cjs`（端到端 **19/19**：width:50%、circle/dashboard 类、`svg` 节点、status 三态、text-inside、is-striped/flow、is-indeterminate、颜色函数含 `#`、`linear-gradient`、`format` 含 `/100`、拖动 range 后 inner 变 `width:30%`、零 pageerror）。

- **`ElResult` 对齐 Element Plus Result（官方文档，2026-09-29）**：原实现已覆盖 EP 基础 DOM（`el-result` > `__icon`/`__title`/`__subtitle`/`__extra` + `icon`/`title`/`sub-title`/`extra` 插槽），本次补齐 **`primary` 图标类型（2.9.11 新增）** 并固化验证闭环：
  - **API 对齐**：`Icon`（`ResultIcon` 枚举：Success/Warning/Info/Error + 新增 `Primary`）、`Title`/`SubTitle`（字符串属性 + `TitleContent`/`SubTitleContent` 插槽优先）、`IconContent`（覆盖默认 `<i>` 图标）、`Extra`（额外操作区）、`Id`/`AriaLabel`（无障碍，根节点 `role="alert"` + `aria-label`）。
  - **primary 类型**：字形复用 InfoFilled（本库 `el-icon-info`），配色取主题主色 `var(--el-color-primary, #409eff)`；`IconClass` 映射 `primary → el-result-icon--primary el-icon-info`，`index.css` 补 `.el-result-icon--primary { color: var(--el-color-primary, #409eff); }`（无 `gap:`/`:is(`/`:has(`）。
  - **图标字形/配色映射**：Success→`el-icon-success`/`--success`、Warning→`el-icon-warning`/`--warning`、Info→`el-icon-info`/`--info`、Error→`el-icon-error`/`--error`、Primary→`el-icon-info`/`--primary`（与 EP `result.ts` 的 `IconMap`/`IconComponentMap` 完全对应）。
  - **演示**：`demo/.../Pages/ResultDemo.razor`（`@page "/result"`，含基础 5 型 / 自定义 icon 插槽(404 SVG 示例) / 自定义 title·sub-title 插槽 / extra 操作区 四段），`DemoNavigation.MockSection` 加 `/result` 入口（`el-icon-success`）；demo `app.css` 补 `.demo-result-grid`（5 型横向排列）。
  - 验证：`test/Element.ComponentTests/ElResultTests.cs`（bUnit **13/13**：含 `PrimaryRendersPrimaryClassAndInfoGlyph`、`primary 图标主色`、`IconContentSlotOverridesDefaultIcon`、各插槽、`AriaLabelIsOutput`、`RootHasAlertRole`）+ `demo/probe-result.cjs`（端到端 **19/19**：5 型图标类齐全、primary 主色 `rgb(64,158,255)`、404 示例 svg 与标题、插槽渲染、extra 按钮、控制台零错误）。文档 `docs/result-alignment.md`。

- **`ElTableV2` 对齐 Element Plus TableV2（虚拟化表格，2026-09-29）**：保留全部原有扩展（`StartIndex`/`ItemCount` 显式虚拟窗口、汇总行、Stripe/Border/当前行、`ExpandContent` 展开行、`AutoGenerateColumns`、Loading/Empty 插槽、`OnRowClick`/`OnScroll`），补齐 EP API 并修 4 个真实缺陷：
  - **EP API 补齐**：`Data`/`Cache`/`RowClass` 别名（对齐 EP `data`/`cache`/`row-class` 命名）；**排序**（`SortBy`+`SortByChanged` 受控、`SortState` 多列状态、`ColumnSort` 事件——只发事件不排序数据，对齐 EP 语义；列级 `Sortable` + 纯 CSS 双三角排序指示，不依赖图标字体）；**树形数据**（`ExpandColumnKey` 展开箭头进单元格、`TreeChildrenField` 默认 children（反射大小写不敏感回退）、`IndentSize` 缩进占位、`IconSize`、`DefaultExpandRowKeys`/`DefaultExpandAllRows`/`ExpandedRowsChange`，扁平化深度上限 32 防环）；`FixedData` 粘性行（`position:sticky` 钉在表头下）；列级 `Sortable`/`MinWidth`/`MaxWidth`/`FlexGrow`/`FlexShrink`；`HeaderClass`/`HeaderClassGetter`/`RowClassGetter`/`RowProps`；`FooterHeight`+`FooterContent`/`OverlayContent` 插槽；`UseIsScrolling`；行双击/右键/移入移出事件；exposes `ScrollToAsync`/`ScrollToTopAsync`/`ScrollToLeftAsync`/`ScrollToRowAsync`（dom.js ES5 通道 `elementTableV2ScrollTop/ScrollTo`）；默认值对齐 `row-height`/`header-height`=50、`row-key` 回落 `id`。
  - **修复 ① 列宽失效**：`.el-table-v2__cell{flex:1 0 120px}` 的 flex-basis 吞掉内联 `width`，所有列被压成等宽 120px → 显式 `Width` 时输出 `flex:0 0 {w}`（`FlexGrow` 时 `flex:{grow} {shrink} {w}`）。**修复 ② 虚拟滚动事件挂错容器**：`onscroll` 原挂内层 body（永不滚动），根容器才是滚动者 → 统一移到根 + `elementTableV2ScrollTop(root)` 读取。**修复 ③ Razor 结构错误**：占位 padding `<div>` 未闭合 + 多余 `}`（多余 `}` 作为文本渲染进表体、padding-bottom 把汇总行/空态推出数千 px）。**修复 ④ `Height="320px"` 解析失败回落 500**：剥离 px 后再取整；另将 `Rows` 物化缓存到 `OnParametersSet`（原先每次访问重物化整个数据源）。
  - **演示**：`demo/.../Pages/TableV2Demo.razor`（`@page "/tablev2"`，8 段：2000 行虚拟滚动/排序/树形/展开行/粘性行/固定列/汇总+页脚/编程滚动+空态），`DemoNavigation` 加 `/tablev2` 入口；样式按铁律追加到 `index.css` 末尾 `/* ==== ElTableV2 ==== */` 段（无 `gap:`/`:is(`/`:has(`）。
  - 验证：`test/Element.ComponentTests/ElTableV2Tests.cs`（bUnit **28/28**）+ `demo/probe-tablev2.cjs`（端到端：虚拟窗口行数远小于 2000、列宽 flex-basis、排序双击循环、树形展开行数增加、展开行、粘性行 sticky、固定列 computed position、页脚、scrollToRow/Top 真实 scrollTop、空态×2+overlay、控制台零错误）。文档 `docs/table-v2-alignment.md`。

- **滚动条「继承污染」根除：标准属性关进 `@supports`，细滚动条抽成可复用工具段**（2026-09-29）：
  - **根因（干净基线对照实验 `demo/diag-scrollbar-pollution.cjs`，headful，7 组）**：真凶是 **`scrollbar-color`（继承属性）**，不是 `scrollbar-width`（MDN：不继承）。Chromium ≥121 只要元素上有非 initial 的 `scrollbar-color`，就**忽略该元素的 `::-webkit-scrollbar`**、回落平台粗条（实测 **15px**）；祖先设 `scrollbar-width:thin` 对后代无效（后代 computed 仍 `auto`），但**写在 `html` 上会作用于 `html` 自己**，页面根滚条因此是 10px 而非 4px。另：`thin` 在 Chromium 下实测同样 15px，**精确 4px 只能靠 `::-webkit-scrollbar`**。
  - **宿主侧根除（优于逐组件打补丁）**：demo `app.css` 中 `html, body, #app` 的 `scrollbar-width` / `scrollbar-color` **整体移进 `@supports not selector(::-webkit-scrollbar){ … }`** —— Chromium/Safari 下该块被跳过（不设 → 不继承 → 全站组件自动免疫），Firefox 下生效作回落。
  - **库侧升级为可复用工具段**：`index.css` 细滚动条段选择器改为 `.el-thin-scrollbar, .el-tree-v2__body`，任何滚动容器挂 **`.el-thin-scrollbar`** 即获得同一套 4px（拇指基色极淡灰、悬停加深、track 透明），不必再复制 8 条规则；段内 `scrollbar-width/color: auto` 复位保留作**库侧免疫**（防御宿主仍在祖先上乱设）。
  - 验证 `demo/probe-treev2-scrollbar.cjs` **7/7**（改前 15px → 改后 4px、悬停不展开、页面根滚条 4px、主内容区 4px）；keep-alive **7/7**、切标签性能 **6/6**（avg 33.8ms）无回归。

- **新增 `ElLightGrow` 状态指示灯组件（参照 BootstrapBlazor `Light`，按 Element Plus 风格自渲染，2026-09-29）**：
  - 零第三方 UI 依赖，所有渲染与闪烁动画均为本仓库内部实现。API：`Color`（库内统一 `MessageType` 五态，默认 `Success`）、`IsFlash`（呼吸闪烁）、`IsFlat`（平面无外发光）、`Size`（直径 px，默认 12，<8 回退默认）、`Text`（右侧文本标签）、`ChildContent`（自定义内容）；继承 `ElementComponentBase` 的 `Cls`/`Style`/`Attributes` 透传。
  - DOM：外层 `span.el-light-grow` 内嵌 `i.el-light-grow__dot` + 可选 `span.el-light-grow__label`，`inline-flex` 居中、文本 `margin-inline-start`（规避 `gap:` 禁用写法）。
  - CSS：`index.css` 末尾 `/* ==== ElLightGrow ==== */` 段——用 EP 五态配色（`--el-light-color`，primary/success/warning/error/info），圆点带 `box-shadow` 外发光、`is-flat` 去掉、`is-flash` 走 `el-light-grow-flash` 呼吸动画（flat 走 `el-light-grow-flash-flat` 仅透明度）。
  - **demo 接入**：新增 `demo/.../Pages/LightDemo.razor`（`@page "/light"`，含基础/文本/闪烁/平面/尺寸/交互切换六段示例），并在 `DemoNavigation.MockSection` 加 `/light` 入口（自动归入「基础组件」目录与「全部组件」画廊，由反射扫描收录 `ElLightGrow`）。
  - 验证：`demo/probe-light.cjs` 端到端 **17/17**（五态色值、文本标签、闪烁类+动画、平面无 glow、尺寸 8/12/18/28、点击切换 success→warning 与文本「告警」、开关闪烁 is-flash）。

- **`ElCalendar` 对齐 Element Plus Calendar（官方文档 + 源码级对齐，2026-09-29）**：原实现仅有基础月历与 `RangeMode` 布尔简化模式，本次按 EP `calendar.vue` / `use-calendar.ts` / `use-date-table.ts` / `select-controller.vue` 补齐能力面：
  - **`v-model`/`Value`**：双向绑定选中日；`OnPick` 保留为本库扩展回调（传原始点击日期）。
  - **`Range`（替换旧 `RangeMode`）**：对齐 EP `range`（长度 2 的 `DateTime[]`），跨度不超过两个月、起止自动按周对齐、按月分段并排渲染；单元格补 `is-in-range`/`is-range-start`/`is-range-end`。
  - **`ControllerType`（2.13.1）**：`Button`（默认，上/下月+今天按钮组）或 `Select`（年/月下拉 + 今天按钮）；`Select` 配 `Formatter(value, type)` 自定义下拉标签。
  - **`DateCell` 插槽**：上下文字段对齐 EP `date-cell` 的 `data` —— `Type`（`prev-month`/`current-month`/`next-month`）、`IsSelected`、`Day`（`yyyy-MM-dd`）、`Date`；默认单元格渲染日数字。
  - **`Header` 插槽**：整体替换默认头部，参数为当前视图参照日期 `DateTime`（EP 传格式化串，本库传 `DateTime` 更灵活）。
  - **暴露方法（对齐 EP Exposes）**：`SelectedDay`、`PickDay(DateTime)`、`SelectDate(string)`（`prev-month`/`next-month`/`prev-year`/`next-year`/`today`）、`CalculateValidatedRange(start,end)`；demo 用 `@ref` 调 `SelectDate` 实现自定义头部导航。
  - **CSS**：`index.css` 末尾 `/* ==== ElCalendar ==== */` 段（头部 flex 布局、select 控制器间距、range 区间高亮），沿用基底 EP 主题类，未引入 `gap:`/`:is()` 等禁用写法。
  - **demo 接入**：新增 `demo/.../Pages/CalendarDemo.razor`（`@page "/calendar"`，含基础/自定义单元格/范围/自定义头部/控制器类型五段），`DemoNavigation.MockSection` 加 `/calendar` 入口。
  - 验证：`demo/probe-calendar.cjs` 端到端 **16/16**（5 个日历渲染、标题含年月、42 格、is-today、点击选中、date-cell 的 M-D 与 ✔、range 的 is-range/起终点、header 按钮齐全且下一月/上一年导航生效、select 控制器含两个下拉、运行期零 pageerror）。

- **demo 切标签卡顿修复：侧栏菜单抽成 `SidebarMenu` + `ShouldRender` 守卫**（2026-09-28）：keep-alive 去掉页面重建后，切页签仍有明显停滞感（实测**平均 559ms**）。根因＝`MainLayout.OnLocationChanged` 每次导航都 `InvokeAsync(StateHasChanged)`，触发含 **100+ 菜单项**的侧栏 `ElMenu` 整体重 diff（菜单 `ChildContent` 是每次新建的委托，无法跳过）。方案：把侧栏 `ElMenu` 抽成 demo 外壳本地组件 `demo/Element.Blazor.Demo/Components/SidebarMenu.razor`（**演示站专属，不放进组件库**——它依赖 `DemoNavigation` 与搜索结果），内部 `ShouldRender()` 仅当 `Collapse` 变化或 `SearchResults` **引用**变化时才返回 true，纯路由切换直接跳过；激活高亮改由 `MainLayout` 调 `sidebarMenuRef.SetActive(path)` → `ElMenu.UpdateActiveIndex`（JS 同步，不走渲染）。实测 `demo/probe-keepalive-perf.cjs` **avg 559ms → 28.1ms（max 41ms）**，正确性回归 `demo/probe-keepalive.cjs` **7/7** 不变。

- **`ElCarousel` 预览层 ‹ › 箭头居中修复**（2026-09-28）：用户截图反馈灯箱内左右箭头在圆钮里偏心。根因＝用文本字形 `‹`/`›`，`button` 默认 padding 未清零 + 字形基线度量随字体漂移（`line-height:44px` 也顶偏）。改为**内联 SVG 雪佛龙**（`stroke:currentColor`，与箭头色联动）＋按钮 `padding:0; display:inline-flex; align-items:center; justify-content:center; line-height:1`；新增 `.el-carousel__preview-arrow-icon{width:20px;height:20px;display:block}`。像素级验证 `demo/probe-preview-arrow-center.cjs` **8/8**（左右箭头 dx≤1.5px、dy≤0.5px，44px 圆形/尺寸不变，零控制台错误）；旧回归 `demo/probe-carousel-preview.cjs` **14/14** 不破坏。

- **demo 页签切回已打开页面不重挂载（keep-alive）**（2026-09-28）：用户反馈「点击已打开的页签切过去，内容会刷新一下」。根因＝点击非活动页签走 `NavigateTo`，Blazor Router 重建页面组件（旧实例销毁、OnInitialized 重跑）。新增 `demo/Element.Blazor.Demo/KeepAliveRouteView.cs`（替代 `RouteView`）：已访问页面按「页面类型+路由参数」缓存保活（非活动页 `display:none`），切回直接复用旧实例；页面自带 `@layout` 优先（对齐 RouteView 语义）、缓存上限 30 淘汰最旧未激活页；右键「刷新」走 forceLoad 整页重载不受影响。`App.razor` 换用该组件。回归 `demo/probe-keepalive.cjs` **7/7**（打标记→侧栏切走→旧实例隐藏保活→点页签切回→标记仍在＝未重挂载→单实例无重复→点已激活页签无动作→零控制台错误）。

- **`ElProTabsGrow` 右键菜单 + 拖拽排序（对齐 teek pro-tabs 剩余能力）**（2026-09-28）：
  - 右键任意页签弹出菜单：**刷新 / 关闭 / 关闭左侧 / 关闭右侧 / 关闭其他 / 关闭全部**，按被右键页签的位置自动禁用不可用项（仅置钉项时全禁用）；「关闭其他」以被右键页签为锚而非当前激活页签；复用既有 `elementProTabsMoreOpen/Close` 外部点击通道（**零新增 JS**）。新参数 `ShowContextMenu`（默认 true），新事件 `OnRefreshTab` / `OnCloseLeft` / `OnCloseRight`。
  - 拖拽排序：`Sortable="true"` 开启（HTML5 原生 drag & drop，无第三方库），置钉项（`Closable=false`）作为顺序锚点不可拖；落定后抛 `OnReorder`（排序后 Path 数组，宿主重排 `Items`）；`is-dragging` 半透明 + `is-drop-target` 描边。
  - **连带修掉组件真 bug**：`draggable="@bool"` 在 Blazor 里 `true` 渲染成空值属性（HTML 语义 = auto，div 拖不动）、`false` 整个省略 —— 必须写 `draggable="@(cond ? "true" : null)"`。
  - demo 外壳接线：页签开启右键菜单与拖拽；刷新=整页重载（页签已持久化）；关闭左/右侧时激活项被关自动切换锚点；重排序后首页兜底钉回最左。
  - 测试 `ElProTabsGrowTests` **44/44**（新增 10 例）；端到端 `probe-demo-shell.cjs` **33/33**。

- **`ElProTabsGrow` 新增页签导航组件 + 演示站外壳改造（ElMenu 侧栏 / 页头 / 多标签页，强刷持久化）**（2026-09-28）：
  - **新增组件 `ElProTabsGrow`**（ProTabs 风格，Element Plus 自渲染版，零外部依赖）：圆点（或图标）+ 标题 + 关闭按钮的胶囊页签；溢出时出现左右滚动箭头，右侧「更多」列出全部标签并提供「关闭其他 / 关闭全部」。
  - **关闭按钮两种显示时机**：`CloseButtonDisplay`（`Hover` 默认 → 鼠标移到该页签才显示；`Always` → 常显），默认 `Hover` 用 `visibility:hidden` 真隐藏（避免看不见却能点的幽灵按钮），`:hover`/`:focus-within` 时 `visible`；`Closable=false` 项（如首页）根本不渲染 X。
  - **当前激活页签字体色两个等价入口**（对齐需求「属性或方便的方法修改」）：属性 `ActiveTextColor`（下发 `--eptg-active-color`），或运行时 `SetActiveTextColor(色)` / `ResetActiveTextColor()`（撤销覆盖回到属性值）；圆点色 `ActiveDotColor`。
  - **设计取向**：组件只管展示与抛事件（`OnSelect` / `OnClose` / `OnCloseOthers` / `OnCloseAll`），打开/关闭/持久化由宿主（demo `MainLayout`）维护；滚动测量与「更多」外部点击关闭由 `dom.js` 的 `elementProTabs*` 通道完成（随库引入）。
  - **演示站外壳改造**（`demo/Element.Blazor.Demo`）：`MainLayout` 重写为 `ElMenu` 侧栏（子菜单 + 分组 + 折叠 + 搜索过滤）+ 页头（折叠触发 / 刷新 / 全屏 `requestFullscreen` 并回推图标 / Ctrl+K 搜索 / 通知 / 头像）+ `ElProTabsGrow` 标签栏；侧栏折叠偏好与**已打开的多标签页均经 `demo-shell.js` 的 `demoStore*` 持久化到 localStorage，强刷浏览器后原样恢复**。激活色、圆点色、关闭按钮时机均可经 `MainLayout`/`NavDemo` 现场调。
  - **本次修掉两个真实 bug**：① `DemoNavigation` 静态字段初始化顺序错误——`Categories = BuildCategories()` 声明在 `CategoryOrder` 之前，导致 `BuildCategories()` 调 `CategoryRank` 时 `CategoryOrder` 仍为 null，`Array.IndexOf` 抛 `ArgumentNullException`、整张侧栏导航数据初始化失败（页签数恒为 0）；已将 `CategoryIcons`/`CategoryOrder` 声明前移。② `MainLayout.OnInitializedAsync` 在 `EnsureTab` 后漏调 `SaveTabsAsync`，导致整页导航（不触发 `OnLocationChanged`）时 localStorage 始终为空、历史页签不累积也不持久化；已补持久化。
  - 测试：新建 `test/Element.ComponentTests/ElProTabsGrowTests.cs` **34 例全过**（渲染/激活态/关闭按钮两时机/激活色属性+运行时方法/点击关闭/更多面板/键盘/JS 通道/CSS·JS 守卫）；新增探针 `demo/probe-demo-shell.cjs` **16/16 全过、控制台零错误**——实测侧栏折叠生效、激活色变量 `#395ae3`、路由驱动页签累积（/data→2、/table→3）、当前激活、X 默认 `visibility:hidden` 且 hover 后 `visible`、点 X 关闭、刷新后页签持久化、全屏按钮存在。

- **`ElCarousel` 箭头 Hover 自动隐藏 + 图片轮播（`ImageFit`）+ 表格列内嵌入示例**（2026-09-28）：
  - **箭头 Hover 显隐**：`Arrow` 一直有 `Hover` / `Always` / `Never` 三档，但 EP 官方 CSS 里真正让它生效的两条规则
    （`.el-carousel__arrow{opacity:0}` + `.el-carousel:hover .el-carousel__arrow{opacity:1}`）**在本项目的 index.css 中缺失**，
    所以三档表现完全一样、箭头永远显示。已在 `index.css` 末尾新增 `/* ==== ElCarousel ==== */` 段补齐（含 `el-carousel--arrow-always` 常显）。
    - **刻意用 `opacity` 而非 `visibility`/`display`**：后者会把按钮移出 Tab 序列，键盘用户就再也 Tab 不到箭头；这里保留可聚焦性，
      另加 `:focus` 规则让 Tab 聚焦时立即显现。
    - **隐藏态加 `pointer-events:none`**：`opacity:0` 的元素仍可被鼠标点到（看不见却能点的幽灵按钮），`:hover`/`:focus` 时恢复 `auto`。
    - ⚠️ 守卫禁用 `:focus-visible`，不要用其替代 `:focus`。
  - **图片轮播**：`ElCarouselItem` 内容本就是任意 `RenderFragment`，图片照放即可；本次补一条规则让 `<img>` 自动铺满该项
    （`.el-carousel__item > img { display:block; width:100%; height:100%; object-fit: var(--el-carousel-img-fit, cover) }`），
    并新增参数 **`ImageFit`**（新增枚举 `CarouselImageFit`：`Cover` 默认 / `Contain` 完整显示不裁切 / `Fill` 拉伸 / `None` 原始尺寸）。
    C# 只在非默认值时下发 `--el-carousel-img-fit`，默认零内联变量。
  - **表格列内嵌入**：`ElTableTemplateColumn` 里直接放 `ElCarousel` 即可，**每个单元格是独立实例**（各自 Timer，互不干扰）。
    经验值见 `docs/carousel-alignment.md`：小尺寸务必给 `Height`、`Arrow="Hover"` 让一堆小轮播不至于吵闹、`ImageFit="Contain"` 避免影像被裁。
  - demo：`/layout` 新增 `#sec-carousel-images`（4 张本机 SVG 图 + 「箭头模式」「图片填充」下拉，**刻意不动原 `#sec-carousel` 段**，避免破坏 `verify-detail.cjs` 的既有断言）；
    `/table` 新增 `#sec-table-carousel`（⑦ 表格列内轮播 + 「查看清单」）；新增 4 张离线示例图 `demo/.../wwwroot/images/demo-img-*.svg`（CT / DR / 检验报告单 / 心电图）。
  - 测试：新建 `test/Element.ComponentTests/ElCarouselTests.cs` **6 例全过**（渲染与指示器、仅首项可见、三档箭头与 class、`ImageFit` 零侵入与 contain/fill、`SetActiveIndexAsync` 环形回绕与 `OnChange`、`IndicatorPosition=None` 与单项不渲染箭头）；
    新增探针 `demo/probe-carousel-image.cjs` **31/31 全过、控制台零错误**（实测：图片 `naturalWidth>0` 真实加载、默认 `object-fit:cover` 且宽高与轮播项完全一致；箭头默认 `opacity=0`+`pointer-events:none`+`0.3s` 过渡，hover 后 `1`+`auto`，移出再隐藏，点右键头确实换图，`Always` 常显 / `Never` 不渲染；表格内 3 行各一个实例、图片全部加载成功、悬停某行**只有该行**箭头显示、其它行仍 `opacity:0`）。

- **`ElCarousel` 图片单击放大预览（内置 lightbox）+ 开发者编程放大（2026-09-28）**：
  - 新增参数 **`PreviewOnClick`**（默认 false）：开启后轮播项内含 `<img>` 则单击该项图片即弹出内置 lightbox 放大；**非图片内容（文本/组件）单击无任何反应**（按 `collectedImages` 对应项是否为空图地址判定；图片地址可为普通 URL 或 base64 data URI，由 `dom.js` 的 `elementCarouselCollectImages` 逐项 `querySelector('img')` 收集并解析绝对地址，base64 原样保留）。
  - 开发者可经属性/事件动态选择要放大的图片：① `PreviewImages`（`IEnumerable<string>`）显式指定图集，非空时优先于自动收集；② `PreviewImagesChanged` 事件回调 `CollectedImages`（组件自动收集到的全部图片地址，已解析绝对地址）；③ 编程方法 `OpenPreviewAsync(IEnumerable<string>, int)` / `OpenPreviewAsync(int)` 直接控制打开哪几张、从第几张起；④ `ClosePreviewAsync()` / `PrevPreviewAsync()` / `NextPreviewAsync()` 控制切换；`PreviewLoop`（默认 true）控制上下张到边界是否循环；`OnPreviewChanged` 回调当前张下标。
  - lightbox：`.el-carousel__preview` 遮罩 + 居中 `.el-carousel__preview-img`；多图时左右箭头（带 `@onclick:stopPropagation`）+ 关闭按钮 + `1 / N` 角标；点遮罩或右上角 X 关闭；**ESC 关闭 / ← → 方向键切换**由 `dom.js` 的 `elementCarouselBindPreviewKeys` 绑定（document keydown，`dotNetRef.invokeMethodAsync` 回调 `[JSInvokable]` 的 `ClosePreviewAsync` / `PrevPreviewAsync` / `NextPreviewAsync`）。
  - **关键修复**：`ClosePreviewAsync` 等回调方法原先**未标 `[JSInvokable]`**，导致 `invokeMethodAsync('ClosePreviewAsync')` 静默失败、ESC 无法关闭——已补齐特性。
  - demo `/table` 新增预览演示区块（图像/文本混合项 + `PreviewOnClick` + `PreviewImagesChanged` 绑定 + 「只放大第 2、4 张」编程放大按钮）。
  - 测试：`ElCarouselTests` 扩至 **12/12 全过**（bUnit 仅验非图片无反应——图片收集依赖真实 JS，图片单击路径交 Playwright）；端到端 `demo/probe-carousel-preview.cjs` **14/14 全过、控制台零错误**（实测：图片单击弹层且显示被点图片、下一张/角标变化、键盘左右切换、ESC 关闭、遮罩关闭、非图片项无反应、编程放大指定 2 张）。

- **`ElCard` 分隔线可控 + Header/Body/Footer 分段高度与底色**（2026-09-27）：卡片里 Header 下的那条线和 Footer 上的那条线，现在**可关、可改色、可改粗细**；三段还各能设**最小高度**与**背景色**。
  - 新增 `ShowHeaderDivider` / `ShowFooterDivider`（`bool`，默认 `true`；置 `false` 等价于线粗 0、**不占位**）、`HeaderDividerColor` / `FooterDividerColor`（任意 CSS 颜色，不设回落 `--el-border-color-lighter`）、`HeaderDividerWidth` / `FooterDividerWidth`（`int?` 像素**粗细**，默认 1，填 `0` 即隐藏）。
  - 新增 `HeaderHeight` / `BodyHeight` / `FooterHeight`（`string`，纯数字自动补 `px`，也接受 `"4rem"`；语义是 **`min-height`**，内容更高时照常长高不被裁）、`HeaderBackgroundColor` / `BodyBackgroundColor` / `FooterBackgroundColor`（不设则透明，继承卡片底色），并补上 `FooterStyle` / `FooterCls` 使三段对齐。
  - **实现仍守「CSS 单一真源」**：`CardStyle` 只把值当 CSS 自定义变量（`--el-card-header-divider-width` / `-color`、`--el-card-header-height` / `-bg`，body / footer 同理）内联下发到卡片根，由 `index.css` 新增的 `/* ==== ElCard ==== */` 段里的 `.el-card__header` / `__body` / `__footer` 三条规则消费（`var()` 带 `1px` / `var(--el-border-color-lighter,#ebeef5)` / `auto` / `transparent` 回落）。**一个新参数都不设时不产生任何变量，与改造前完全一致（零侵入）**。
  - **踩到并修掉一个真坑**：`AddIf(cond, $"…{HeaderDividerWidth.Value}px")` 即使在 `cond=false` 时也会**先求值**插值参数 → `Nullable object must have a value` 让 5 个既有用例全红。必须先把 `int?` 解成局部变量（`ShowHeaderDivider ? HeaderDividerWidth ?? 1 : 0`）再拼字符串。已在 `docs/card-alignment.md` 记录。
  - demo `/basic` 的 ElCard 段新增实时控件（两条线的开关 + 取色器 + 粗细数字框、三段高度文本框、三段底色开关 + 取色器）与可调卡片 `#c4-card-tunable`。
  - 测试：`ElDataDisplayTests` 新增 4 例（**零侵入** `CardWithNoAppearanceParameters_EmitsNoExtraVariables`、`CardDividerCanBeHiddenAndCustomized`、`CardSectionHeightAndBackgroundAreConfigurable`、`CardFooterStyleAndClsAreApplied`），该过滤组 **15/15 全过**；新增探针 `demo/probe-card-divider.cjs` **23/23 全过、控制台零错误**——实测默认两线 1px、关 Header 线后 `borderBottomWidth=0px` 且 Footer 线不受影响、线粗 4px/2px 与色值 `rgb(245,108,108)`/`rgb(103,194,58)` 生效、填 0 消失、三段 `min-height` 60px/120px/48px 且清空回未限制、三段底色独立生效与取消后回透明。

- **`ElWaitingPopupGrow` 取消按钮全面可配置**（2026-09-27）：该按钮**本来就是一个 `<ElButton>`**（不是自拼样式），本次把它的能力完整透传出来——新增 `CancelButtonType`（语义配色 Default/Primary/Success/Warning/Danger/Info）、`CancelButtonSize`（Default/Large/Small）、`CancelButtonColor`（**自定义颜色**，任意 CSS 颜色）+ `CancelButtonDark`（false = 浅底彩字 / true = 实色底白字）、`CancelButtonPlain`、`CancelButtonRound`、`CancelButtonPosition`（`ElWaitingPopupGrowActionAlign`：Center 默认 / Left / Right）；文字沿用既有 `CancelText`，显隐沿用 `ShowCancelButton`。
  - 位置实现沿用本组件的既有范式：`ActionsStyle` 只把对齐值作为 CSS 变量 `--ewpg-actions-align` 内联下发到操作区（`Center` 时不输出任何东西），`index.css` 里 `.el-waiting-popup-grow__actions { text-align: var(--ewpg-actions-align, center); }` 消费 —— **C# 只给值，样式真源仍在 index.css**；默认居中与改动前完全一致。
  - demo `/grow` 等待浮层段新增第二行控件：按钮文字 / 类型 / 尺寸 / 位置 / 朴素 / 圆角 / 自定义色（开关 + 取色器）/ 实心，两个浮层实例共用同一组配置。
  - 测试：`ElWaitingPopupGrowTests` 17 → **23 例全过**（新增 `CancelButton_IsAnElButtonWithDefaults`、`CancelButtonType_And_Size_MapToButtonClasses`、`CancelButtonPlain_And_Round_MapToButtonClasses`、`CancelButtonColor_FlowsThroughToElButtonCssVariables`、`CancelButtonPosition_EmitsAlignVariable`、`CancelButton_Text_IsConfigurableAndClickStillCancels`）；新增探针 `demo/probe-wp-cancel-button.cjs` **20/20 全过、控制台零错误**（实测：Primary/Danger = 实色底 `rgb(64,158,255)`/`rgb(245,108,108)` + 白字，Small=`el-button--small` 且高 24px < Default 32px < Large 40px，自定义色 `#e6a23c` 浅底彩字 / 勾「实心」底色即 `rgb(230,162,60)`，左/中/右实测按钮中心 636 / 700 / 764，朴素 `is-plain`、圆角 `is-round` 且 20px 全圆）。
  - **顺带修掉 demo 的绑定写法坑**：`CancelButtonColor="flag ? color : null"` 会被 Razor 当作**字面字符串**原样传给 string 参数（非 string 参数如 `ButtonType` / `int?` 才会按 C# 表达式求值），导致 ElButton 输出非法 CSS 变量、颜色静默失效；已统一改为 `CancelButtonColor="@(flag ? color : null)"`，并在 `docs/waiting-popup-alignment.md` 标注该陷阱。

- **`ElVerifyCodeGrow` 定时刷新倒计时进度条**（2026-09-27）：开启 `AutoRefreshInterval` 后，验证码图片底部贴一条**宽度随时间递减**的进度条，每次刷新（自动到点 / 点图 / 按 Enter-Space）都**重置为满格**，让用户看出「这张验证码还剩多久失效」。
  - **四个新参数**：`ProgressColor`（默认 `#409eff`，即 EP `--el-color-primary`；常量 `ElVerifyCodeGrow.DefaultProgressColor`，传空值也回落至此）、`ProgressWidth`（`int?`，不设则跟随图片宽度）、`ProgressHeight`（默认 3px）、`ShowProgressBar`（总开关，默认 true；仅 `AutoRefreshInterval > 0` 时可见）。
  - **两层结构**：外层 `__progress` 是透明「轨道」（绝对定位贴左下，宽 = `ProgressWidth ?? CaptchaWidth`、高 = `ProgressHeight`），内层 `__progress-bar` 才是着色条（`background` + 宽度动画）。**为什么不直接给一条元素动画宽度**——`@keyframes` 里的 `width:100%` 相对**包含块**（整个验证码块）解析，会把 `ProgressWidth` 覆盖掉；套一层定宽轨道后，内部的 100% 才是相对 `ProgressWidth` 解析。
  - **重置靠 `@key` 而非 JS**：`.el-verify-code-grow__progress` 挂 `@key="CountdownKey"`（`CountdownKey = CaptchaId + "|" + AutoRefreshInterval`），换验证码即换键 → Blazor 元素重建 → CSS 动画从头播放。间隔也进键，改值即时重播。动画 `forwards` 保证走到 0 后停在空。**零计时器、零 JS**。
  - **手动刷新顺带重新计时**：点图 / 按键改走新增的 `ManualRefreshAsync()`——换完验证码后再 `RestartTimer`，让下一次自动刷新从当下重新计时；否则定时器相位不变，进度条虽已重置却会在走到一半时被旧的到点时间截断。定时器自己到点时仍走 `RefreshAsync()`（不重建 Timer）。
  - 样式仍在唯一真源 `index.css` 的 `/* ==== ElVerifyCodeGrow ==== */` 段（`__captcha` 加 `position:relative; overflow:hidden`，新增 `__progress` / `__progress-bar` 与 `@keyframes el-verify-code-grow-countdown`），未新建 CSS 文件。
  - demo `/grow` 验证码段新增「定时刷新」开关 + 「刷新间隔」毫秒数（原先写死 5000ms，现改为 `AutoRefreshInterval="vcAutoRefresh ? vcInterval : 0"`）+ 「倒计时进度条」开关 + 「进度条色」取色器 + 「进度条宽」数字框（0 = 同图片宽）+ 「进度条高」数字框。
  - 测试：`ElVerifyCodeGrowTests` 17 → **23 例全过**（新增 6 例覆盖显隐条件、默认值、三项可配置、宽度跟随图宽、刷新即换 `CountdownKey`）；新增 Playwright 探针 `demo/probe-vc-countdown.cjs` **25/25 全过、控制台零错误**——实测宽度 1.4s 内 143.0px → 85.0px、点击刷新后 85.0px → 191.0px（轨道 200px）、`#e6a23c` / 120px / 6px 实时生效、自动刷新仍按时换图、改间隔即时同步动画时长（5000ms↔2000ms）并按新间隔刷新、间隔置 0 等价于关闭定时刷新。

- **验证码题面动态居中 + 占位符字号分离；等待浮层可设圆角与背景色**（2026-09-27）：
  - **`ElVerifyCodeGrow` 题面居中修复**：`VerifyCodeService.GenerateDistortedText` 原先用硬编码 `x=32` / `baseY=45` 定位题面——当 `CaptchaHeight` 小于 60（如 demo 中的 50）时题面被压到图片底部（「靠下」），宽度变化时也不水平居中。现改为**按 SVG 宽高动态居中**：先累加各字符实际占位宽度（含字间与运算符额外间距）求出整行总宽，令其中点落在水平中线；再以 `height/2 + 0.35em(最大字号)` 计算基线，令数字/大写字母的视觉中心落在垂直中线。**不论宽高如何调整都稳定居中、不再靠下**。
  - **`ElVerifyCodeGrow` 占位符字号独立**：新增 `PlaceholderFontSize`（像素，可不设）——通过 CSS 自定义属性 `--evcg-placeholder-font-size` 驱动 `::placeholder` 字号，与 `FontSize`（输入框字符大小）**互相独立**；不设置时占位符沿用输入字号。
  - **`ElWaitingPopupGrow` 圆角与背景色**：新增 `Round`（默认 true，4px，对齐 `--el-border-radius-base`）/ `RoundRadius`（自定义像素，优先于 `Round`）/ `BackgroundColor`（卡片背景色），由 `ContentStyle` 统一下发内联样式。
  - **`ElWaitingPopupGrow` 标题 / 提示 / 转圈三色 + 取消按钮显隐**（同日追加）：新增 `TitleColor`（标题文字色）/ `MessageColor`（提示文字色）/ `SpinnerColor`（加载圈描边色）——三者以 **CSS 自定义变量**（`--ewpg-title-color` / `--ewpg-message-color` / `--ewpg-spinner-color`）内联下发到卡片，由 `index.css` 的三条规则消费（`color` / `stroke` 均带 EP 主题变量回落），因此**样式真源仍在 `index.css`**，C# 只提供变量值；都不设置时行为与旧版完全一致。取消按钮显隐沿用既有 `ShowCancelButton`（`false` 时 `.el-waiting-popup-grow__actions` 整段不渲染），本轮把它接进 demo 实时切换。
  - demo `/grow` 等待浮层段控件扩充：「取消按钮」开关（`ShowCancelButton`）、「标题色」/「提示文字色」/「转圈颜色」三个取色器（连同既有「背景色」共 4 个 `type=color`），配合「圆角」开关与「圆角半径」数字框，可把卡片调成深色底 + 浅色字 + 自定义描边的组合。
  - 测试追加：`ElWaitingPopupGrowTests` **13 → 17 例**（新增 `TitleColor_MessageColor_SpinnerColor_EmitCssVariables`、`ColorParameters_Unset_EmitNoCssVariables`、`ShowCancelButton_ToggledByParameter`）；`demo/probe-grow-tweaks.cjs` 扩到 **24 项**（新增标题色 `rgb(255,255,255)` / 提示色 `rgb(230,240,255)` / 转圈色 `rgb(255,214,102)` 实测、取消按钮隐藏与恢复显示），**24/24 全过、控制台零错误**。
  - demo `/grow` 新增对应控件：验证码段加「占位符字号」数字框；等待浮层段加「圆角」开关、「圆角半径」数字框、「背景色」取色器（命令式与 `@bind-Visible` 两个浮层实例共用同一组外观配置）。
  - 测试：`ElVerifyCodeGrowTests` 11 → **15 例**（`CaptchaText_IsCenteredForAnySize` 对 140×50 / 200×60 / 300×80 / 400×100 四种尺寸断言题面水平与垂直中心贴合中线、`PlaceholderFontSize_EmitsCssVariable`、`FontSize_And_PlaceholderFontSize_AreIndependent`）；新增 `ElWaitingPopupGrowTests` **13 例**（默认隐藏 / 可见渲染 / `Show`-`Hide` 与事件 / `MaskClosable` / 取消按钮 / 圆角 / 背景色 / z-index / `ChildContent` / 自定义与空文案）；`--filter` 两组共 **31 例全过**。
  - Playwright(msedge)：新增 `demo/probe-grow-tweaks.cjs` —— 占位符字号独立生效与置 0 回落、题面在 140×50 与 320×90 下水平/垂直居中（实测 hc 70.7/159.6、vc 26.1/46.7，均贴合中线）、浮层圆角 4px/16px/0px 与背景色 `#123456` 即时生效。

- **`ElInput` 对齐 Element Plus 官方 Input 并新增密码防保存**（2026-09-26）：按 https://element-plus.org/zh-CN/component/input 的 Attributes / Events / Slots / Exposes 四张表补齐（详见 `docs/input-alignment.md`）。
  - **密码框**：官方语义是「使用 `show-password` 属性即可得到一个可切换显示隐藏的密码框」，故 `ShowPassword="true"` 即为密码框（不必再写 `Type="InputType.Password"`），原生 type 在 `password` / `text` 间切换；图标出现时机对齐官方 `!disabled && !readonly && (有值 || 已聚焦)`（此前空值聚焦时不出现）；内置内联 SVG「睁眼 / 闭眼」随状态切换，可用 `PasswordIcon`（对应官方 `password-icon` 插槽，2.13.6）覆盖；新增 `PasswordVisible` / `SetPasswordVisibleAsync(bool)` / `PasswordVisibleChanged`（对应官方 expose `passwordVisible`）。
  - **不弹「记住 / 保存密码」对话框**：新增 `PreventPasswordSave`（默认 `true`，仅密码框生效）——把原生 `autocomplete` 置为 `new-password`（浏览器对 `off` 已普遍忽略，但对 `new-password` 按新密码处理，不再自动填充、不再弹保存气泡），并附加 `data-lpignore` / `data-1p-ignore` 供 LastPass / 1Password 忽略；**调用方显式设置 `Autocomplete` 时以显式值为准**（`SetParametersAsync` 记录是否显式赋值），想保留记忆能力则传 `PreventPasswordSave="false"`。
  - 其余补齐：`ClearIcon`（官方 `clear-icon`，2.11.0）、`Max` / `Min` / `Step` 原生属性、`AutosizeMinRows` / `AutosizeMaxRows`（对应官方 `autosize: { minRows, maxRows }`）、`CountGraphemes`（2.13.7，设置后绕开原生 maxlength/minlength）、`Label`（官方 deprecated 的 a11y 别名）、`OnMouseEnter` / `OnMouseLeave`、公开方法 `ResizeTextareaAsync()`、只读属性 `IsComposing`。
  - **输入法组合事件真正接线**：`OnCompositionStart/Update/End` 此前只是声明、从未触发（Blazor 不派发 `composition*` 原生事件），现由 `dom.js` 新增的 `elementInputBindComposition` 绑定监听后回调 `[JSInvokable]` 方法；没有任何订阅者时完全不碰 JS。
  - `WordLimitPosition="outside"` 渲染修正：字数统计不再塞进绝对定位的 suffix，改为根容器直接子节点，位置真正落在输入框外侧。
  - demo `/form` 新增 5 段演示（基础/清空/自定义清除图标、密码框含防保存开关对比与 password-icon 插槽、图标/插槽/复合型、尺寸/字数统计 inside 与 outside、文本域/自适应/min-max-step/鼠标事件/组合事件）。
  - 测试：新增探针 `demo/verify-input-ep.cjs` **24 项全过、控制台零错误**；`ElInputTests` 3 → **15 例全过**；既有 `probe-input-checks.cjs` 9/9、`verify-fixes.cjs` 46/46；全量 bUnit **1666/1666**。

- **新增 `ElMarqueeGrow` 滚动字幕组件**（2026-09-26）：能力参照 BootstrapBlazor 的 `Marquee`（`Text` / `Color` / `BackgroundColor` / `FontSize` / `Duration` / `Direction`），但**不使用其任何代码或样式**——动画由统一 `index.css` 的 `@keyframes el-marquee-grow-*` 加组件内联样式驱动，**零 JS、零第三方依赖**，按 Element Plus 观感渲染。
  - 四个方向 `ElMarqueeDirection`：`RightToLeft`（默认）/ `LeftToRight` / `BottomToTop` / `TopToBottom`；内容既可用 `Text`，也可用任意 `ChildContent`。
  - **圆角可控**：`Round` 预设（`None` 0 / `Small` 4px / `Large` 8px / `Pill` 999px）与 `RoundRadius` 自定义像素（优先级更高，负数按 0），配合 `Bordered` / `BorderColor` / `BorderWidth` 使用。
  - **无缝滚动**：内容元素带「等于容器尺寸」的 padding 偏移（横向 `padding-left:100%`；纵向按容器**内容高度**取 px，并扣除 `box-sizing:border-box` 下的上下边框），动画位移恰为其自身尺寸的 100%，内容「一侧进入、另一侧退出」，循环点严格衔接——**无需 JS 测量内容宽度，任意长度内容都不留空档**。实测中曾用「多份副本 + 位移 1/N」方案，因单份宽度小于容器宽度会产生约 533px 空档与到点跳变，故改为本方案。
  - 另有 `Repeat`（一次循环内的内容份数，默认 1）、`Spacing`（份间距，默认 32）、`PauseOnHover`（悬停暂停，默认 true）、`role="marquee"` + `aria-label` 无障碍（`Repeat > 1` 时仅首份内容对读屏可见）。
  - 样式写入统一 `index.css`（`/* ==== ElMarqueeGrow ==== */` 段），**未新建任何 `fix.css` / `theme.css`**；demo `/grow` 新增演示段（方向/圆角/像素/时长/份数/边框交互控件 + 四档圆角肉眼对比 + 纵向 `ChildContent`）。

- **新增 `ElWaitingPopupGrow` 等待 / 处理中浮层组件**（2026-09-27）：全屏半透明遮罩（`el-overlay`）+ 居中卡片，按 Element Plus 观感渲染——SVG 旋转加载圈（`ewpg-rotate` / `ewpg-dash` 动画，取 `--el-color-primary`）、标题、提示文字，可带「取消」按钮；零第三方依赖、零 JS（仅 CSS 动画）。
  - **两种显隐用法**：`Show()` / `Hide()` 命令式（`@ref`），或 `Visible` + `@bind-Visible` 声明式；二者都会广播 `OnVisibleChange`。
  - **参数**：`Title`（默认「处理中...」）、`Message`（默认「请稍候」）、`ShowCancelButton`（默认 true）、`CancelText`、`MaskClosable`（默认 false，开启后点遮罩等同取消）、`SpinnerSize`（默认 42）、`ZIndex`（默认 2000，遮罩用本值、卡片用 +1）、`Width`、`ChildContent`（额外插槽）。
  - **取消路径**：点击取消按钮（或 `MaskClosable` 时点遮罩）→ 先触发 `OnCancel` 再关闭。
  - 样式写入统一 `index.css`（`/* ==== ElWaitingPopupGrow ==== */` 段），**未新建任何 `fix.css` / `theme.css`**；demo `/grow` 新增演示段（命令式 3 秒自动关闭 / `@bind-Visible` / `MaskClosable` 三种用法）。
  - 验证：Playwright(msedge) 探针 `demo/probe-waiting-popup.cjs` **13/13 全过、控制台零错误**（遮罩铺满视口 / 卡片居中 / 加载圈动画生效 / 标题提示文案 / 取消关闭 / 默认遮罩不关 / `MaskClosable` 遮罩关）。
  - 测试：`ElMarqueeGrowTests` 23 例全过；Playwright(msedge) 实测 20 项全过、控制台零错误。

- **新增 `ElVerifyCodeGrow` 验证码输入组件**（2026-09-26）：左图右框（`Position` 可切换为图右框左）——左侧为程序生成的算术验证码 SVG（加/减/乘/除中文题面，逐字符随机变形 + 多类干扰线/点/几何图形），点击图片或聚焦后按 `Enter`/`Space` 即可刷新；右侧为可双向绑定的输入框（`Value` / `ValueChanged`，支持 `@bind-Value`）。**零 JS、零第三方依赖**，按 Element Plus 观感渲染。
  - 主要参数：`NoiseComplexity` 噪点复杂度（默认 5，0 = 无噪点，各干扰元素数量按比例缩放）、`AutoRefreshInterval` 定时器自动刷新毫秒数（≤0 不刷新，默认 0，由 `OnParametersSetAsync` 统一启停，运行时勾选/改值即时生效）、`ShowBorder` 边框显隐（默认 true）、`Round`/`RoundRadius` 圆角（4px 预设 / 自定义像素）、`CaptchaWidth`/`CaptchaHeight`（长宽可调）、`InputWidth`（输入框宽度，不设自适应）、`FontSize`（输入框字号，默认 14）、`Placeholder`（可自定义）、`Disabled`。
  - 校验 API：`ValidateAsync(userInput)` 把输入与当前验证码答案比对（忽略大小写与首尾空白），**校验即销毁（一次性）**；每次刷新通过 `OnRefresh`（`VerifyCodeGeneratedEventArgs`：CaptchaId/ImageData/Question/Answer）对外暴露新验证码，`CaptchaId` 可交服务端校验。
  - 服务：内置自包含 `VerifyCodeService`（实现 `ICaptchaService`，答案存进程内字典）仅适合演示与前端自校验；生产应把生成与校验下沉服务端（组件 XML 注释已标注安全提示）。
  - 样式写入统一 `index.css`（`/* ==== ElVerifyCodeGrow ==== */` 段），**未新建任何 CSS 文件**；demo `/grow` 新增演示段（位置切换/噪点复杂度/定时刷新 5s/边框/圆角交互控件 + 校验按钮显示通过/错误 + 题面与绑定值实时回显）。
  - 测试：`ElVerifyCodeGrowTests` 11 例全过（渲染/边框/反转/圆角/噪点 0 与 8 的 SVG 内容与数量对比/@bind-Value/点击刷新换 Id 且触发 OnRefresh/ValidateAsync 命中与一次性销毁/错误答案/禁用）；Playwright(msedge) 实测 `/grow` **8 项全过、控制台零错误**（定时刷新 5s 内生效、`FontSize`/`CaptchaWidth`/`InputWidth`/`Placeholder` 实时生效、图与输入框垂直对齐中心偏差 1px、点击刷新；截图 `demo/verify-verifycode-final.png`）。
  - 修复：**勾选「定时刷新」后不自动刷新**——原定时器仅在 `OnAfterRenderAsync(firstRender)` 启动一次，参数变化不会重启；改为 `OnParametersSetAsync` 对比 `_activeInterval` 后 `RestartTimer`。
  - 修复：验证码图与输入框**垂直未对齐**——根容器与字段块改用 flex 布局（`align-items:stretch` + 字段内 `align-items:center`），实测中心偏差 1px。
  - 修复：`ElInput` 复合型输入框（prepend/append）`display:inline-table` 在混入非单元格子元素时错位——`.el-input-group` 由 `inline-table` 改为 `inline-flex`（`el-input__inner` `flex:1 1 auto`、`__prepend`/`__append` `flex:0 0 auto`），单一真源 `index.css`。
  - 修复（2026-09-27）：`ElInput` 前后缀插槽 / 复合型输入框三处对齐问题（demo `/form` 实测）：
    1. **插槽内容不再偏上**：`.el-input__prefix-inner` / `.el-input__suffix-inner` 此前是行内元素，内容按行盒顶部排（比输入框高 7px）；改为 `display:inline-flex` + `align-items:center` + `height:100%`，并把 `.el-input__prefix/__suffix` 也设为 flex 容器，插槽文本与输入框严格垂直居中、底边对齐。
    2. **插槽内容变宽不再压住输入文字**：官方只用固定 `padding-left/right: 30px`，插槽写成 `Http://`（37px）就会压到文字上。新增 `dom.js#elementInputMeasureAffix`（量出前缀最右 / 后缀最左边界，跳过清空 / 密码 / 字数等动态图标）+ `ElInput.SyncAffixPaddingAsync` 把像素回写成 input 内联 `padding-left/right`（Blazor 仍是样式唯一真源；bUnit 等无 JS 环境静默降级为官方 30px）。后缀预留宽度取「插槽实测 / 密码 60 / 字数 60」的最大值。
    3. **复合型前后缀宽度自适应**：根因是文件顶部老版压缩样式残留 `width:1px`（覆盖不掉）+ 末尾兼容段 `width:100px`、`padding:0 !important` → prepend 被钉成 100px 且文本溢出；现显式声明 `width:auto`（压掉 `width:1px`）、`height:var(--el-input-height)`，前后缀宽度随文本自适应、与输入框等高且底边对齐；内嵌按钮 / 选择器改 `margin:0 -20px` + `align-self:stretch`（左右铺满、高度撑满），纯文本前后缀保留 20px 内边距。
    - 实测：`demo/probe-form-input.cjs` **14/14 全过**（居中 / 等高 / 底边对齐 / 前缀不压文字 / prepend 宽 = 文本宽 + 40 / append 按钮铺满 / 控制台零错误），`demo/smoke-pages.cjs` 六个页面 6/6 零错误；bUnit `ElInput*` **56/56** 全过。
- **`ElAvatar` 支持自定义颜色**：新增 `Color`（背景色）/ `TextColor`（文字色）两个参数，取值是**任意 CSS 颜色**——十六进制 `#7b61ff`、`rgb(...)`、主题变量 `var(--el-color-success)` 都直接可用；两者渲染为内联样式（无需为每种颜色在 CSS 里加规则），不传时沿用 EP 官方默认灰底 `#c0c4cc` / 白字 `#fff`，对既有用法零影响。`Color` 会渲染在 `Style` 之后，因此同时传两者时 `Color` 生效。演示页 `/data` 的「卡片 / 头像 / 角标 / 图片」小节新增一整行示例：默认灰 + primary/success/warning/danger/info 五个主题色 + 自定义紫 `#7b61ff` + 浅底深字（`Color` + `TextColor`）+ 图标头像 + 方形（`Shape`）+ 大/小号（`Size`）+ `Style` 渐变。覆盖：`ElDataDisplayTests.AvatarSupportsCustomColorAndTextColor`（断言内联样式含 `background-color:` / `color:`，且不传时两者都不出现）。
- **`ElXBubble` / `ElXBubbleList` 支持时间显示**：`ElXBubble` 新增 `Time` 参数、`XMessageItem` 新增 `Time` 属性，`ElXBubbleList` 的默认渲染与 `ItemTemplate` 两条路径都会透传。渲染为作者名右侧的 `<time class="el-x-bubble__time">`（用户侧同样显示在名字右侧，与作者名基线对齐）；`Time` 为空则不渲染、不占位，没有作者名只有时间时标题行也照常显示。演示页 `/x` 的 4 条 Mock 消息补齐时间，发送后新增的消息取当前 `HH:mm`。覆盖：`ElXComponentTests.BubbleRendersTimeNextToHeader`。

- **`ElTable` 经典版对齐 Element Plus 官方能力补齐（2026-09-30）**：在保留全部既有扩展（动态列、整表批量编辑、树形、选择列、固定表头、流体高度、合并 class/style 钩子等）基础上，补齐下列官方能力，全部位于 `src/Components/ElTable.razor(.cs)`：
  - **合计行**：`ShowSummary` + `SumText`（默认「合计」）+ `SummaryMethod`（自定义合计行内容）此前已声明但模板从未渲染 `<tfoot>`；新增 `<tfoot>` 渲染，数值列自动求和、首列显示 `SumText`，并支持 `SummaryMethod` 回调返回 `{ columns, data }` 逐格结果（对齐 EP `summary-method`）。
  - **单元格合并 `SpanMethod`**：新增 `SpanMethod` 回调（签名 `(row, column, rowIndex, columnIndex) → { rowspan, colspan } | null`），在表体 `<td>` 渲染时跳过被合并覆盖的单元格并应用 `rowspan`/`colspan`（此前 `MergeCellClass`/`MergeCellStyle` 仅合并 class，不是真正的行列合并）；报表类「同字段每 N 行纵向合并」即可纯声明式实现。
  - **默认排序 `DefaultSortProp` / `DefaultSortOrder`**：首次加载即按指定属性升/降序排列（走既有 `ApplyTableQuery` 排序路径，不引入第二套排序状态）。
  - **实例方法（编程式控制）**：补齐 EP 的 `setCurrentRow` / `clearSelection` / `toggleRowSelection` / `clearFilter` / `doLayout` / `setScrollTop` 对应实现 —— `SetCurrentRowAsync(row)`（配合 `HighlightCurrentRow` 高亮当前行，已加 `InvokeAsync(StateHasChanged)` 确保立即刷新）、`ClearSelectionAsync()`、`ToggleRowSelectionAsync(row)`、`ClearFilterAsync()`、`DoLayoutAsync()`、`SetScrollTopAsync(int)`（经 `ElementHelper.GetScrollTop/SetScrollTop` 操作 `body-wrapper`）；全部为 `public` 可直接经 `@ref` 调用。
  - **演示**：`demo/…/Pages/TableDemo.razor` 新增 **⑩ EP 进阶能力** 段（合计行 + 单元格合并 + 默认排序 + 三个实例方法按钮），数据用唯一 Id 副本避免行 key 冲突。
  - 验证：`demo/diag-table-epfeatures.cjs` 端到端 **ALL_PASS=true**（合计行文本/列数、合并后 `rowspan=2` 且次行少一格、默认排序非递减、`ClearSelection` 清空勾选、`SetCurrentRow` 高亮生效、`SetScrollTop(0)` 归零、控制台零错误）；横向滚动头/体同步沿用纯 JS `elementTableSyncScroll`（不触发 Blazor 重渲染）。经典 `ElTable` 非虚拟化 → 无 ElTableV2 式滚动空白停滞缺陷。

### Changed
- **`ElDrawer` 对齐 Element Plus 官方 API 并纳入 demo**（2026-09-26）：按官方文档 https://element-plus.org/zh-CN/component/drawer 补齐/落地：
  - 新增参数 `BodyClass`（官方 2.9.3）、`OpenDelay`、`CloseDelay`、`ModalPenetrable`（2.11.7）、`HeaderAriaLevel`（a11y，默认 `"2"`）、`Resizable`（2.11.0）、事件 `OnOpenAutoFocus` / `OnCloseAutoFocus` / `OnResizeStart` / `OnResize` / `OnResizeEnd`（2.11.8）。
  - `LockScroll`（复用 `elementLockBodyScroll`）与 `AppendToBody`（复用 `documentAppendChild`）此前**声明了却未实现**，现已真正生效；新增公开方法 `HandleCloseAsync()`（对应官方暴露的 `handleClose`）。
  - 对齐官方「内容懒渲染」语义：首次打开前不再渲染抽屉内部内容（此前只看 `DestroyOnClose` 会提前渲染）。
  - `dom.js` 新增 `elementDrawerFocusContent` / `elementDrawerEnableResize` / `elementDrawerDisableResize`（严格 ES5 语法）。
  - 修复：`.el-drawer__container` 为全屏元素且原先承载 `@onclick:stopPropagation`，吞掉空白区点击导致**点遮罩无法关闭**；已将 `stopPropagation` 移至真正的抽屉面板 `.el-drawer`。
  - 样式走统一 `index.css`（新增以 `/* ==== ElDrawer ==== */` 开头的 `.el-drawer__resizer` 段），未新建任何 `fix.css` / `theme.css`。
  - demo `/feedback` 新增「Drawer 抽屉」与「Drawer 承载另一个 Razor 页面」两段，后者以 `<ChildContent><FormDemo /></ChildContent>` **组件嵌套**（非 iframe）把 `/form` 页面放进抽屉，并演示 `Resizable` 拖拽调宽与 `DestroyOnClose`。

- **`theme.css` + `fix.css` 并入 `index.css`，三文件统一为单一 `index.css`**（2026-09-26）：原独立文件 `wwwroot/css/theme.css`（Element Plus 主题 token 层）与 `wwwroot/css/fix.css`（自渲染组件与兼容补丁层，4673 行）**逐字节原样**拼入 `wwwroot/css/index.css`，顺序严格保持原加载序 **基底 index → theme → fix**——theme 段由 `/* ==== THEME_CSS_START ==== */` / `/* ==== THEME_CSS_END ==== */` 标记包裹，fix 补丁段在文件末尾（段头「自渲染补丁层」注释 + 合并日期）。级联语义与旧「`index.css → theme.css → fix.css` 三文件依次加载」**100% 等价**，不存在「theme 压过 fix」的翻转风险（早期曾试过「index+fix 合并、theme 独立后加载」，实测 4 套 e2e 级联回归失败，正是 theme 覆盖 fix 所致，已否弃该方案）。两份独立文件删除、`Element.csproj` 不再打包它们；宿主页面现在**只需加载一个 `index.css`**（`theme.css` / `fix.css` 的 `<link>` 直接删掉即可，旧写法会 404）。同步更新：demo `index.html`（仅留 `index.css`）、README / PACKAGE_README 安装片段、`docs/getting-started.md`（顺序改写为只加载 `index.css`）/ `docs/p1-style-inventory.md`（重写加载顺序与文件决策表，含 THEME_CSS_START/END 标记与级联等价说明）/ `docs/p1-theme-variables.md`（token 层已折叠进 index.css），及 9 篇 `docs/*-alignment.md` 的引用；bUnit 守卫测试读取路径改为统一后的 `index.css`（`ElMenuTests` / `ElDateTimePickerTests` / `ElFormFieldGrowTests` / `ElHtmlEditorGrowTests` / `ElMarkdownGrowTests` / `ElPivotDataGridGrowTests` / `ElViewerGrowTests` 等——按组件 marker 截段逻辑不变，兼容黑名单断言照常生效；`ElMenuTests` 的 menu 段守卫锚定到 THEME_CSS_START 之后，只截 theme 块内的 `.el-menu` 段，避免把 theme 合法的 `gap: 4px` 纳入断言）。备份存于 `src/Components/wwwroot/css/_pre_unify_backup/`。
- **`ElXBubble` 气泡排版收紧**（用户实测「对话框不太紧凑」）。**根因级修复**：`.el-x-bubble__content` 原本是 `white-space: pre-wrap`，而 Razor 缩进会在 `@Content` 前后各写进一段「换行 + 缩进」空白文本，`pre-wrap` 把它们当成真实换行 → 每个气泡顶部白多出一整行空行，单行消息的内容区被撑到 **60px**（8px 上内边距 + 空行 21px + 正文 21px + 8px 下内边距 + 2px 边框），这才是「不紧凑」的主因。修复：内容区改 `white-space: normal`（吸收缩进空白），正文包一层 `<span class="el-x-bubble__text">` 并由它保留 `pre-wrap` —— 用户正文里的 `\n` 照旧换行，调用方 `ChildContent` 模板也不再被缩进污染。修复后单行消息内容区 **60px → 39px**，整行 80.8px → 59.8px。同时按紧凑方向微调：内容区内边距 `12px 14px → 8px 12px`、行高 `1.6 → 1.5`、`min-width 120px → 96px`；标题行下边距 `6px → 4px`；头像与气泡间距 `12px → 10px`；列表行间距 `16px → 12px`；附件区间距与上边距 `8px → 6px`；footer 上边距 `6px → 4px`。会话头像同步从 EP 默认 `40px` 收到 **32px**（规则 `.el-x-bubble__avatar .el-avatar`，字号 13px），与紧凑气泡高度相当，整行更贴合。`.el-x-bubble__header` 改为 `display:flex; align-items:baseline; gap:8px`，作者名与时间同一行基线对齐。演示页 `/x` 第 4 条 Mock 消息改成含 `\n` 的两行文本，用来锁住「换行仍生效」。覆盖：`demo/verify-avatar-bubble.cjs` **38/38**（含 white-space=normal / 正文 pre-wrap / 无前导换行 / 单行 39px / 含 `\n` 消息 2 行 60px）。

- **demo 细滚动条统一改 4px：thumb 默认透明、悬停/滚动才若隐若现显形**（2026-09-28）：用户截图指出侧栏滚动条偏粗（原全局 10px、部分 6px）。`demo/Element.Blazor.Demo/wwwroot/css/app.css` 的「细滚动条」段重写：
  - **统一 4px**：`.demo-aside-scroll`（侧栏）/ `.demo-main`（主内容）/ `.el-menu--popup-container`（折叠弹层）三处 `::-webkit-scrollbar` 宽高均 `4px`；并复位 `scrollbar-width/scrollbar-color: auto`，避免 Chromium≥121 因非 initial 标准属性而忽略 webkit 伪元素、回落平台滚条（那样改不动宽度）。
  - **根因修复（覆盖式滚条透明即不可见）**：覆盖式(overlay)滚条的拇指颜色取自 `::-webkit-scrollbar-thumb` **基色**，且只在「激活」（悬停/滚动）时绘制。若基色为 `transparent`，激活时也透明 → 永远不显形（侧栏实测即此：指针压上去也不出条）。故基色改为极淡灰（`rgba(0,0,0,.12)`/弹层 `.14`），并保留 `:hover` 加浓（`.20`/`.28`）兼容经典滚条常绘场景。**静止时覆盖式不绘制＝默认透明；激活才画淡灰＝若隐若现**——正是用户要的「默认透明、悬停才显形」。
  - 覆盖 `index.css` 全局 `*::-webkit-scrollbar-thumb{box-shadow:inset 0 0 0 10px rgba(0,0,0,.3)}` 的深色内阴影（`box-shadow:none`），否则拇指恒为深灰、悬停无效。
  - Firefox 等无 webkit 伪元素的引擎走 `@supports not selector(::-webkit-scrollbar)` 回落 `thin + transparent`、`:hover` 才 `rgba(0,0,0,.18)`。
  - 端到端回归 `demo/probe-scrollbar-final.cjs` **13/13**（4px 宽 + 静止极淡 + 交互显形 + 移开复淡 + 零控制台错误）；探针为模式无关稳健写法（classic/overlay 皆过），并作为细滚动条回归基线保留。

### Fixed

- **`/table` ③ 段（动态列 + 整表批量编辑）输入/选择迟滞 + 多标签切换后整页滚动条/滚轮失效（2026-09-30）**：
  - **迟滞根因（两层叠加）**：① ③ 段每次 `SetCell` 都调 `StateHasChanged()`，重渲染**整个 TableDemo 页面（10 个 ElTable + 轮播）**，每次按键都全页重画 ⇒ 实测连续键入触发 **28 个 140~470ms 长任务**。② 即便去掉 `SetCell` 里的 `StateHasChanged`，Blazor 的 `EventCallback` 行为仍会让 `ValueChanged="@(v => SetCell(...))` 这个 lambda 的**宿主组件（TableDemo）自动重渲染**（链式渲染），迟滞不消失。
  - **修法（两层隔离）**：A. 把 ③ 段整体抽成独立组件 `DynEditTable`（`demo/Element.Blazor.Demo/Components/DynEditTable.razor`），`SetCell` 的 `ValueChanged` 链式渲染**只落在这一个 ElTable + 它的输入上**，不再连累同级 9 个段；`DynRow/DynCol` 类与全部 ③ 方法一并迁入。B. 在 `DynEditTable` 重写 `ShouldRender()` + `_renderDirty` 门控：**输入过程中（oninput 逐字符）不重渲染本组件**——编辑中的输入框由 `ElInput` 内部 `rawInputText` 维护键入显示、数据已即时落库；**仅在失焦 / 下拉选定（ElSelect 离散一次）/ 结构变化（进出编辑态、增删列、表头点击提示）时置 `_renderDirty=true` 触发一次重渲染**刷新终值。`LiveNote` 组件承载「保存结果」提示，只刷自身、不牵动父级。
  - **多标签切换后滚动失效根因**：`MainLayout` 在 `OnAfterRenderAsync` 调 `elementBindFadeScrollbar` 重建内容区滚条；其 `repaint()` 用 `prevY = el.style.overflowY; …; el.style.overflowY = 'hidden'; …; restore(prevY)` 存在竞态——切标签 / 鼠标在内容区移动时两次 `repaint` 重叠，会把 `.demo-main` 的 `overflow` **永久钉在 `hidden`** ⇒ 整页滚不动、滚轮也失效（KeepAlive 路由使页面不销毁、只 `display:none`，放大此竞态）。
  - **修法**：`repaint()` 改用**状态机**——进入时设 `el.dataset.__fadeRepainting='1'` 并跳过重叠调用，退出时统一从 `getComputedStyle` 读真实 `overflow`（不再读会被自己污染的 `style.overflowY`），`requestAnimationFrame` 内重设时若已处于 `hidden` 先恢复到 `auto`/`visible` 再短暂关两帧、最终**强制 `el.style.overflowY = ''`**（回到样式表值，杜绝残留 `hidden`）；`.demo-main` 在 `MainLayout` 里显式兜底 `overflow-y:auto`。`__fadeRepaintDepth` 计数防嵌套。
  - 验证：Playwright 探针 `probe-table-final.cjs` 模拟 8 次跨标签切换 + 内容区鼠标扫动 → `.demo-main` 仍 `overflowY:auto`、赋值滚动/滚轮**均生效**（`pass:true`）；③ 段进入编辑 → 键入 → 失焦 → 退出/再进入，**值保留、提示更新、下拉选择触发保存**（全部 `pass`，零控制台错误）。**性能**：连续键入 90 字符的 `PerformanceObserver('longtask')` 实测 **`longtaskCount:0`（长任务归零，此前 140~470ms/键）**。

- **`ElTableV2` 表头错位 + 滚动空白停滞 + 滚动条过粗（用户反馈三连，2026-09-30）**：
  - **表头错位（真 bug，非样式偏好）**：`.el-table-v2__header-row` 这个类**从来没有 `display:flex`**，于是表头单元格退化成块级盒 —— headful 实测 5 个表头格的 `x` **全部等于 312**、各自宽度正确却**完全重叠竖排**，表头被撑到 293px 压住表体（用户截图即「编号 / 名称 / 类别 竖着叠在数据行上」）。
    - 修法：`.el-table-v2__header-row{display:flex;min-width:max-content;height:100%}`；表头单元格与表体单元格**共用 `.el-table-v2__cell` 盒模型**（同 padding / box-sizing / flex-basis / border）；列对齐映射为 flex 容器的 `justify-content`（表头是 flex，`text-align` 管不了横向对齐），表头标题抽进 `.el-table-v2__header-cell-text`（`min-width:0` + 省略号，与表体一致）。
    - 实测：`maxDeltaX=0 / maxDeltaWidth=0`，左/中/右三类列的文字对齐偏差 ≤1px，表头高回到 `HeaderHeight=50`，表头/表体计算盒模型逐项相同。
  - **滚动时空白停滞**：headful 逐帧 rAF + `PerformanceObserver('longtask')` 实测 —— 滚动一次触发一段 **80~107ms 主线程长任务**，连 scroll 事件本身都被推迟到长任务结束才派发 ⇒ 表现为「先空白、过一会儿才出内容」。对照实验先排除 CSS（去掉 `min-width:max-content` / `position:sticky` 后耗时不变），再锁定两处叠加：① 组件每帧把「数据源 → 扁平行」**全量重算**、逐行反射两次（`rowKey` 属性 + `children` 字段）；② 绑定 `OnScroll` 后**整页重渲染**，把页面里另外 8 张表也一起渲染（同类表无 `OnScroll` 只需 27ms，有则 62~71ms）。
    - **三层缓存**：扁平化结果（按 数据源引用 / 行数 / 树字段 / rowKey / 展开态 判定失效）、数据源拷贝、属性反射（按「类型 + 属性名」永久缓存，含「精确 → 忽略大小写」回退，`TableV2Column.GetValue` 共用）。
    - **新增 `ShouldRender` 抑制**：`SetParametersAsync` 逐个参数与当前值比较，**参数一个都没变且组件自身状态（滚动/展开/排序）没变时不重渲染** —— 父组件重渲染不再波及本表；列上挂了 `Template`/`HeaderTemplate`/`CellClassName`/`HeaderCellClassName` 时自动放弃抑制（这类值可能引用不变而输出变）；首帧无条件渲染（`<ElTableV2 />` 一个参数都不传也要出画面）。
    - **新增 `ScrollThrottleMs`（默认 0 = 不节流，保持 EP 语义）**：`EventCallback` 调用后必然令**接收方**（通常是页面）`StateHasChanged`，高频回调会把整页重渲染拖成卡顿；设 ~150ms 即「滚动日志不丢、帧不掉」。demo 基础表示范 `ScrollThrottleMs="150"`。
    - 实测：单次滚动首帧 **80~107ms → 中位数 22~23ms**；`longtask` **55~112ms → `[]`**；逐帧可视区覆盖率恒 100%（`blankFrames=0`）。
  - **滚动条 4px 全局化**：`*::-webkit-scrollbar` 由 `10px + 深色内阴影` 改为 **4px + 静止全透明**，`*:hover::-webkit-scrollbar-thumb` 才浮出 `rgba(0,0,0,.16)`、压到条上 `.32`，指针移出即消失；并用 `*{scrollbar-width:auto;scrollbar-color:auto}` 切断祖先 `scrollbar-color` 继承（Chromium ≥121 只要见到非 initial 值就忽略 `::-webkit-scrollbar`，这正是此前「宽度改不动」的原因）。既有细滚动条工具段 `.el-thin-scrollbar` / `.el-tree-v2__body` 的拇指基色同步由「`.12` 常显」改为「transparent → hover `.16` → 压条 `.32`」，全库行为统一。
    - 实测：表格自身滚动条 **9px → 4px**（`overflow:hidden` 前后 `clientWidth` 差值精确量法），`probe-treev2-scrollbar.cjs` 仍 **7/7**（4px / 悬停不展开 / 页面根与主内容区均 4px）。
    - **demo 外壳同步收敛**（`demo/…/wwwroot/css/app.css`）：页面根 `html/body/#app`、`.demo-aside-scroll`、`.demo-main`、`.el-menu--popup-container` 的拇指基色由「`.10` / `.12` / `.14` 常显淡灰」改为 **静止 `transparent` + `:hover` `.16` + 压条 `.32`**，与库侧全局规则完全一致；Firefox `@supports` 回落同步为「静止 `transparent transparent` → hover `.18`」。此前 `.demo-main` 静止态仍能截到 min=198 的条纹（探针阈值 200 → 判 FAIL），正是这条常显淡灰造成的。
    - 实测：`probe-scrollbar-final.cjs` **12/13 → 13/13**（`.demo-aside-scroll` 静止 `dark=0/min=255`、`.demo-main` 静止 `dark=0/min=247`，交互后显形 `min=173/207`，指针移开复归 `min=244~255`）。
  - 验证：`ElTableV2Tests` bUnit **28/28**；`probe-tablev2.cjs` **20/20**；`probe-tablev2-scroll.cjs`（几何 + 滚动延迟 + 滚动条三项断言）；`probe-tablev2-thumb.cjs` **6/6**（新增）；`probe-scrollbar-final.cjs` **13/13**；`probe-treev2-scrollbar.cjs` **7/7**；定位用诊断脚本 `demo/diag-tablev2-scroll.cjs`（把「设置 scrollTop → 窗口更新」拆成 scroll 事件 / DOM 改写 / 首帧三段计时）保留备用。
  - **滚动条取证的三个方法学陷阱（花了一轮才查清，已写进探针注释）**：
    ① **改完 `::-webkit-scrollbar` 样式后，滚动不足以让滚条重绘** —— 注入纯红拇指再滚一次、截图仍纹丝不动；必须「`overflow` 开关一次」**强制重建滚动条**才能看到新样式。最初的「注入红拇指自校准」因此得出「滚动条像素列为空」的假结论。
    ② **元素级截图与页面截图行为不同**；且 `--hide-scrollbars` 只在 **headless** 下注入（Playwright 源码 `_innerDefaultArgs` 里该参数只出现在 `options.headless` 分支），headful 才拍得到滚条 —— 这也解释了为什么 `.demo-main` 能测到、表格早期测不到。
    ③ **静止态不能用绝对亮度阈值**：同一脚本两次跑出 `min=238/dark=0` 与 `min=214/dark=119`，条带里本身可能压着内容像素。改为**差分判定**：把拇指显式压成 `transparent` 再采样，前后「几乎无变化」⇒ 静止态没有东西是拇指画的 ⇒ 静止透明成立（旧的 `.12` 常显基色会被这条准确抓出来）。
    - 最终表格侧取证结果：右缘 4px `elementFromPoint` 命中 `.el-table-v2` 自身（未被覆盖）；静止态差分 `dMin=0 / dDark=0`；注入红拇指+强制重建后 `min=76 / reddish=116`（确为该滚条且样式可控）；精确宽度 4px。

- **`ElTableV2` 快速甩动/惯性滚动仍空白停滞（深度修复，2026-09-30）**：上一轮把每帧成本从 80~107ms 压到 ~22ms、并加了 `ShouldRender` 抑制与 `ScrollThrottleMs`，但**快速滚动**仍偶发空白。根因在滚动机制本身：原生 `@onscroll` 每次事件都走一次 `JSRuntime.InvokeAsync<int>` 回读 `scrollTop` + `StateHasChanged`，高频时「JS→C# 往返」与「渲染排队」叠加，使 `_scrollTop` 落后真实位置超过 `overscan×RowHeight`(5×50=250px)，渲染窗口覆盖不到视口 ⇒ 空白/停滞。
  - **改法（DOM 层 rAF 节流 + 直接下发位置）**：新增 `elementTableV2BindScroll` / `elementTableV2UnbindScroll`（`dom.js`，ES5）—— scroll 事件只记录最新 `scrollTop/Left` 并 `requestAnimationFrame`，**每帧最多回调一次** C#（用该帧最新位置作参数，`[JSInvokable] OnTableV2Scroll(top,left)`），彻底去掉异步回读与逐事件 `StateHasChanged`；渲染频率被钉在刷新率（≤60fps），且始终用最新位置。
  - **动态缓冲**：新增 `ScrollingOverscanBoost`（默认 10 行 ≈ 500px）—— 滚动中（`_isScrolling`）在基础 `OverscanCount` 之外追加预渲染行，覆盖极端快速滚动的子帧位移；静止后回落到基础值，不增加常态渲染成本。`ComputeStartIndex`/`ComputeEndIndex`/`EffectiveStartIndex` 改用 `EffectiveOverscan`。
  - **生命周期**：`OnAfterRenderAsync(firstRender)` 用 `DotNetObjectReference.Create(this)` 绑定；`DisposeAsync` 解绑并释放引用（避免悬空回调/泄漏）。移除旧的 `@onscroll="OnRootScrollAsync"` 绑定与回读方法。
  - 扩展功能**全部保留**（树形/FixedData/展开/排序/汇总/StartIndex-ItemCount/编程滚动/固定列/is-scrolling 样式类）。
  - 验证：`ElTableV2Tests` bUnit **28/28**；`probe-tablev2.cjs` 对基础表（400 行）做瞬时大跳（0/1500/4000/8000/9500…）+ 连续快滚 40 帧 + 接近底部，逐像素扫描视口覆盖 —— **`blanks=0`、零控制台错误、PASS**；滚动中子项渲染由 17 升至 37（动态缓冲生效）。

- **`ElTableV2` 滚动仍有空白 —— 真凶是 interop 参数小数被 `int` 反序列化失败（2026-09-30，用户带截图复报）**：上一轮把滚动改到 DOM 层 rAF 节流后，空白仍在。抓 `pageerror` 拿到决定性证据：**56 次 `System.Text.Json.JsonException: DeserializeUnableToConvertValue, System.Int32 / FormatException: FormatInt32`，全部来自 `DotNetDispatcher.ParseArguments`**，数量与「scrollTop 为小数」的帧数**完全一致**（56/361）。
  - **机理**：非整数 `devicePixelRatio` / 页面缩放（用户环境实测 `dpr=1.0000000149 / zoom=1.0125`）下 `element.scrollTop` 不是整数（实测 `391.20001220703125`）。`elementTableV2BindScroll` 原样把 `root.scrollTop` 传给 `[JSInvokable] OnTableV2Scroll(int top, int left)`，参数 JSON 序列化成 `[391.20001220703125,0]`，C# 侧反序列化成 `Int32` 即抛异常（报错里的 `BytePositionInLine: 18` 正是该小数在第 18 个字符处）⇒ **整次回调失败**：`_scrollTop` 不更新、`StateHasChanged` 不执行 ⇒ DOM 停在旧窗口 ⇒ **空白**（实测 `st=391.2/521.6/652` 时 `padTop` 仍是 0，渲染窗口一步都没动，最大空隙 **269px ≈ 5.4 行**）。小数帧成串出现，所以一丢就是 200+px，肉眼即「空白行」。
  - **修法（双保险）**：① `elementTableV2BindScroll` 下发前 `Math.round(root.scrollTop/scrollLeft)`（与既有的 `elementTableSyncScroll` 用 `Math.round` 同一套路），并给 `invokeMethodAsync` 挂 `.catch` 防未捕获 rejection；② `OnTableV2Scroll` 签名改 **`double top, double left`** 再内部 `(int)Math.Round(...)`（`NaN`/`Infinity` 保持原值），**彻底免疫**任何未来调用方传小数。
  - 排查确认 `limit(size)`/`valueAt()` 两个几何量入口本身就返回整数（`Math.round`/`parseInt`），全库仅 `OnTableV2Scroll` 漏了取整。
  - 验证：`ElTableV2Tests` bUnit **28/28**；逐帧覆盖并集扫描（非「首行位置」近似）在 **DPR=1 与 DPR=1.25 两种缩放下均为 `blankFrames=0 / maxGap=0px / pageerror=0`**（修复前 DPR=1 为 `blankFrames=6 / maxGap=269.2px / pageerror=56`），而 `fracTops=56` 表明小数 scrollTop 的触发条件依旧存在 —— 证明是修复生效而非环境改变。回归脚本已固化为 `demo/probe-tablev2-blank.cjs`（`node demo/probe-tablev2-blank.cjs 1` 与 `... 1.25` 分别验证两种缩放）。
  - **教训（写进探针铁律）**：只监听 `console.error` 会**漏掉 Blazor interop 抛出的未捕获异常**（它们走 `pageerror`/`unhandledrejection`，实测 56 个 error 在上一轮「零控制台错误」的结论下被完全漏检）；空白检测要用**覆盖并集**（所有行 rect 的并集 vs 可视区），只看「首行 top」会把「窗口整体落在视口外」误判为无空白。

- **细滚动条「鼠标移入才显形」必须靠 JS 触发重绘（重要结论，2026-09-30）**：CSS 侧早已写好「静止 `transparent` / `:hover` 才 `.16`」，但实测**在 Blink 上根本不会生效** —— 因为 **Blink 不因 `:hover` 状态变化重绘 `::-webkit-scrollbar` 伪元素**。四种纯 CSS 写法（`*:hover::`、`div:hover::`、`[class]:hover::`、CSS 变量 `*:hover{--sb}`）在「静止 / 悬停容器 / 指针移开」三态下像素完全相同（headful msedge + 页面截图 + 4px 条带像素分析）。
  - **唯一有效手段＝重建滚动条**：把 `overflow` 关掉再还原（必须**跨帧**，同帧开关会被合并）。A/B 对照（摘掉 JS 前后）：静止 `238/dark=0` → 仅悬停 `238/dark=0`（不显形）→ 悬停+重建 `214/dark=119`（显形，214 ≈ `rgba(0,0,0,.16)` 叠白底）。也就是**纯 CSS 停在第 2 步**。
  - **落地**：新增 `window.elementBindFadeScrollbar(root)`（`src/Components/wwwroot/js/dom.js`）—— 在指针进入/离开滚动容器时（`pointerover`/`pointerout` 捕获委托，只在该容器真正变化时动作）做一次跨帧 overflow 开关；上色仍由 index.css 既有的 `:hover` 规则负责，**刻意不加专门的显形类**（Blazor 重渲染会重写 class 属性把 JS 加的类抹掉 —— 实测出现过「类被抹后再也不恢复」，与 ElAffix 那条教训同源）。重建期间先用内联 `scrollbar-gutter: stable` 钉住槽位，保证**内容不左右抖 4px**（实测悬停前后表格 `x/w` 完全相同）。demo 在 `MainLayout.OnAfterRenderAsync(firstRender)` 里调用一次，全站生效；宿主页面同样可自行调用。
  - 验证：`demo/probe-tablev2-thumb.cjs` **11/11**（精确 4px + `::-webkit-scrollbar` 计算宽 + 静止态差分无拇指 + 红拇指链路自检 + **悬停显形 `min 238→214`/移开复归 `238`** + 重建无位移 + 零控制台错误）；`probe-scrollbar-final.cjs` **13/13**（外壳侧不变）。

- **`ElTable` ③ 动态列批量编辑「输入即回退原值」+ 横向滚动纯 JS 镜像（用户反馈，2026-09-30）**：
  - **③ 编辑态输入回退原值（真 bug）**：进入编辑后文本/数字格用 `ElInput` 单向 `Value=` + `OnChange`（失焦才写回），键入过程中若父级（ElTable）任意一次重渲染把「尚未保存的旧值」作为 `Value` 回传，`ElInput.OnParametersSet` 原本只要 `Value != rawInputValue` 就清空 `rawInputText`，于是刚键入的字符被旧值瞬间覆盖 —— 表现即「输入完马上变回原来的东西」。
    - 修法：`ElInput.OnParametersSet` 改为**聚焦期间绝不因外部 `Value` 与已键入内容不一致而丢弃原文**（`!IsFocus` 才允许清）；失焦（`OnBlurEventAsync`）时统一丢弃并由 `OnChange` 写回数据源。清除按钮 `ClearOnClick` 同步置 `rawInputText=null`，避免聚焦态下清不掉。
    - 验证：`demo/diag-table-sec3.cjs` 端到端 —— 8 个文本/数字格键入「Z测N」后值全部保留（`reverted=false`），控制台零错误；`ElSelect` 下拉格（分类列）本就不接受键入，行为不变。
  - **横向滚动头/体同步改为纯 JS（顺带消除滚动卡顿隐患）**：原先表头/表体各自 `@onscroll` 绑 Blazor 事件，每滚一次都要整表重渲染一遍；现 `ElTable.SyncScrollBindingAsync` 改为调用 `window.elementTableSyncScroll(header, body)`（`dom.js`），用 `requestAnimationFrame` + 锁在 DOM 层直接镜像 `scrollLeft`，**滚动期间完全不进 Blazor 渲染管线**。
    - 验证：`demo/diag-table-scroll.cjs` —— `Height=260` 固定表头表演示表 60 行，滚动后行数恒 60、`firstRowVisible=true`、控制台零错误；经典 `ElTable` 非虚拟化，不存在 ElTableV2 那种「滚动空白停滞」缺陷。

- **`ElTable` ③「输入完马上变回原来」根因补全（上一条只修了聚焦期显示，没修落库，2026-09-30 二次复测确认仍回弹）**：
  - **残留根因**：上一条把 `ElInput.OnParametersSet` 改成「聚焦期保留 `rawInputText`」，确实让**键入过程显示**不再被旧值覆盖（`afterType` 正确），但**落库仍失败** —— 每次父级重渲染 `SetParametersAsync` 会用父级参数（尚未保存的旧值）覆盖 ElInput 内部 `Value`；失焦时 `OnBlurEventAsync` 清空 `rawInputText`，显示与 `OnChange`（仅 `onchange`/失焦触发）拿到的都是被还原的内部 `Value`=旧值 ⇒ `SetCell` 写回旧值，表现为「输入完马上变回原来的东西」。实测 `SetCell` 被调用、但入参即原值，确认是 **`OnChange` 在失焦瞬间读到的是已被父级参数覆盖的 `Value`**。
  - **真正修法（改 demo 绑定，零侵入核心库）**：③ 三处 `ElInput`（text/int/decimal）把 `OnChange=` 改为 `ValueChanged=`。`ValueChanged` 在 `oninput` 即触发 → `SetCell` 在键入时就把新值写回 `dynRows` → 父级 `Value` 参数变成新值 → 内部 `Value` 不再被还原 → 失焦/切换编辑态都拿到正确值。`ElSelect` 本就用 `ValueChanged`，所以下拉列从未有此问题。`SetCell` 内 `dynTable?.Refresh()` 冗余（页面 `StateHasChanged` 已驱动表格重渲染且 `tr`/`td` 用 `@key` 保焦点），一并移除。
  - **验证**：`demo/diag-editbug.cjs`（Playwright + msedge）端到端 —— 进入编辑 → Name 格键入「测试EDIT123」→ 失焦后值 `测试EDIT123`（**不再回弹**）→ 退出/再进入编辑仍 `测试EDIT123`（证明真正落库 `dynRows`）→ Stock 数字格键入 `777` 同样保留；note 显示「自动保存 #1 · 库存 = 777」；**控制台零错误**。`RESULT=PASS`。

- **`ElTag` / `ElCheckTag` 对齐 Element Plus 官方 API（2026-09-30）**：经逐项比对官方 https://element-plus.org/zh-CN/component/tag，组件能力已完整覆盖 —— Tag 属性 `type`/`closable`/`disable-transitions`/`hit`/`color`/`size`/`effect`(映射到 `Theme`)/`round`，事件 `click`(`OnClick`)/`close`(`OnClose`+`OnBeforeClose` 拦截)，插槽 `default`；独立 `ElCheckTag` 组件含 `Checked`(`v-model:checked`)/`CheckedChanged`/`Type`/`Disabled` 及点击切换。修复两处与官方不符的默认值：`Type` 默认由枚举首值 `Success` 改为 `TagType.Primary`、`Theme`(effect) 默认由 `Dark` 改为 `TagTheme.Light`（EP 文档默认 primary / light）。验证：`ElTagTests` **6/6 通过，0 失败**。

- **`ElTable` 非编辑态行键盘处理器缺失导致 `NonEditableTable_EnterIsIgnored` bUnit 测试失败（回归修复，2026-09-30）**：
  - **根因**：`GetRowKeyboardAttributes(row)` 在 `!IsEditable` 时直接返回空字典，使非编辑态 `<tr>` 根本不挂 `onkeydown`/`onkeyup`。测试 `NonEditableTable_EnterIsIgnored` 用 bUnit 的 `Rows()[i].KeyDown(args)` 模拟按回车，该方法要求元素必须已挂载对应事件处理器，否则抛 `MissingEventHandlerException` → 测试失败。**两个键盘处理方法本身（`OnEditingRowKeyDownAsync`/`OnRowKeyUpAsync`）早已对非编辑态 early-return（no-op），缺的只是「挂载」这一步**。
  - **修法**：去掉 `GetRowKeyboardAttributes` 里的 `!IsEditable` 提前返回，让处理器始终挂载；非编辑态回车由方法内部 no-op 安全忽略。编辑态行为完全不变。
  - **验证**：`ElTableEnterCellNavigationTests` 18/18 全绿；完整 `ElTable*` 套件 **57/57 通过，0 失败**（此前为 56/57）。

- **演示站多标签页会跟着内容一起滚走（用户反馈：「某个标签页内容较多时向下滚动，多标签页不会跟着滚动」）**（2026-09-28）：
  - **对齐参考站 teek 的滚动模型**：teek 是 `el-container` 满高 + 页头/页签在**不滚动的顶部区**、只有 `el-main` 内容区滚（`height:100%` + `overflow:auto`）。本项目 demo 外壳原来是「整页滚动 + 页头/侧栏 sticky」，内容一长页签栏就被顶出视口。现改为固定外壳：`.demo-shell` 定高 `100vh`、`.demo-body` 高度 100% 且 `min-height:0`、**`.demo-main` 成为唯一滚动容器**（`overflow-y:auto` + `overscroll-behavior:contain`），标签栏以 `flex:0 0 auto` 固定成一行，因此**内容再长、页签栏也不会滚动**（页头同样固定）。
  - **滚动层与固定层的视觉反馈**：`demo-shell.js` 新增 `demoBindContentScroll`（滚过 0 就给外壳打 `.is-content-scrolled`，CSS 据此给标签栏加一道淡投影）、`demoScrollContentTop`（切页签时把内容区滚回顶部，`MainLayout.AfterNavigateAsync` 调用）。
  - **顺带修掉 `ElAffix` 两个真 bug**（因滚动模型改动而暴露，`/layout` 演示区原先「看起来能用」只是恰好滚不到位）：
    - ① **Blazor 重写 `style` 属性把 JS 写的定位抹掉 → 固钉永久静默失效**：`AffixContentStyle` 原本随 `isFixed` 取值（空串 → `z-index:100`）。JS 把 `position/left/top/width` 直接写在 `.el-affix__content` 的内联样式上，而 `SetFixed` 触发 `StateHasChanged` 重渲染时，Blazor「值变了才写 DOM」于是**整体重写该元素的 style 属性**，把刚写上的定位清空。现该属性恒定输出 `z-index:{ZIndex}`（z-index 对 static 元素无副作用），Blazor 不再重写，JS 内联定位得以保留。**排查要点：JS 直接操作内联样式的元素，其 Blazor `style`/`class` 属性取值不得随状态变化，否则必被重渲染清掉。**
    - ② **`Target` 指向页面内滚动容器时定位错位**：旧实现把 `top` 写成 `offset + "px"`（视口坐标），容器不是 window 时固钉会飞到视口 `0px`、盖住页头与标签栏。现改为容器相对定位：`top = containerRect.top + offset`，底部同理 `bottom = (innerHeight - containerRect.bottom) + offset`；容器为 window 时 `containerRect` 恰为 `{top:0,bottom:innerHeight}`，结果与旧实现**逐像素一致**（向后兼容）。
  - demo：`/layout` 的固钉 / 回到顶部示例改为**自带滚动容器**（`#affix-scroll-box` + `Target="#affix-scroll-box"`，与同页 ElAnchor 的写法一致），不再依赖页面长度与窗口高度。
  - 测试：`demo/probe-demo-shell.cjs` 扩到 **30/30 全过、控制台零错误**；新增断言——`window.scrollY=0`（窗口不滚）、页面主体不出滚动条、内容区确实滚动（scrollTop>0）、内容区高度=外壳剩余高度、**滚动前后页头与标签栏位置不变（★ 本次需求）**、标签栏紧贴页头下方且不被内容遮挡、滚动后标签栏出现投影态、切页签后内容区回到顶部、固钉进入 `fixed` 态且**吸附在容器顶部**（`contentTop === boxTop`）、固钉未落在视口 0（不盖页头/标签栏）、回到顶部按钮出现。

- **`ElHtmlEditorGrow` 源码视图下工具栏变成一排「没有图标的空按钮」**（用户截图反馈，2026-09-27）：截图里红框圈出的那排小方块并不是按钮，而是 **8 个连在一起的分隔符**。根因是 `VisibleTools` 把 `ElHtmlEditorGrowToolDescriptor.EnabledModes` 当成了**过滤条件**（不在 `Mode` 内的工具整块不渲染），于是切到 `Source` 后设计类工具（含撤销/加粗/对齐/下拉/表格命令等 42 项）全被抽走，只剩分隔符与「源码 / 设计视图」；而 `Separator` 的 `EnabledModes = All`，8 个分隔符全部留下、首尾相接成一堵「竖线墙」。
  - **对齐参考实现语义**：`EnabledModes` 与 Radzen 的 `EditorButton.EnabledModes` 一致，决定的是**可点 / 置灰**而非**渲染 / 不渲染**（`IsDisabled = Editor.Disabled || Disabled || !EnabledModes.HasFlag(mode)`）。现 `VisibleTools` **不再按模式过滤**，源码视图下工具栏完整渲染整 45 项；设计类命令由 `IsToolEnabled` 判定为 `disabled` + `is-disabled`，「源码 / 设计视图」保持可点且 `is-active`。**图标一个不少、工具栏高度与设计视图一致，切换模式不再跳动**。
  - **悬空分隔符收敛**（`CollapseSeparators`）：行首、行尾以及连续出现的分隔符一律不渲染。既覆盖 `HiddenTools` 隐藏一整段工具后两侧分隔符贴在一起的场景，也保证任何模式下都不会再出现「只剩一排分隔符」的工具栏。
  - 要让某个工具彻底不出现，请继续用 `HiddenTools`（语义与行为不变）；`Tools` 自定义工具表的 `EnabledModes` 同样改为「置灰不隐藏」。
  - 测试：`ElHtmlEditorGrowTests` 新增 `SourceMode_KeepsFullToolbarWithIconsAndDisablesDesignTools`（源码视图下 34 按钮 / 8 分隔符 / 3 下拉齐备、**每个按钮都断言带 `svg.el-html-editor-grow__icon`**、加粗 `disabled` + `is-disabled`、源码按钮不灰且 `is-active`）、`HiddenTools_CollapseTheSeparatorsTheyLeaveBehind`（隐藏「加粗…删除线」整段后分隔符 8 → 7、按钮 34 → 30）、`VisibleTools_NeverDangleSeparators`（设计 / 源码 / 全部三种模式下首尾与相邻分隔符均不存在）；原 `SourceOnlyTool_IsHiddenInSourceMode` 按新语义改写为 `DesignOnlyTool_IsRenderedButDisabledInSourceMode`（渲染保留、置灰）。
  - Playwright(msedge)：新增 `demo/probe-htmleditor-toolbar.cjs`，实测设计视图与源码视图的工具栏结构（按钮数 / 分隔符数 / 每个按钮带图标 / 设计类命令 disabled / 源码按钮 `is-active`）与来回切换不塌陷。

- **`ElWatermark` 水印「显示不全 / 被裁掉一角」**（用户截图反馈）：旧实现把「拼贴 tile」直接当成**一个未旋转的水印框**来画（`background-size = (Width+gapX) × (Height+gapY)`，且只放一份副本），而水印框是**旋转**的（默认 -22°），旋转后的实际占位是它的**外接矩形**——更宽更高，于是旋转文字超出框被 tile 边缘裁掉一角，行间还对不齐、有缝隙。现按 element-plus 2.11.8 `useClips` / `getMarkSize` **逐行对齐重写几何算法**（详见 `docs/watermark-alignment.md`）：
  - **旋转补偿**：`space = ceil(|sin(rotate)| × 框高 / 2)`，未显式指定的 `Width` 会加上 `space`，保证旋转后文字仍完整落在框内（对齐官方 `getMarkSize`）。
  - **旋转包围盒**：把「水印框 / 实际墨迹」内容矩形绕中心旋转，取四角外接矩形作为真正的拼贴单元 `cutWidth × cutHeight`。
  - **双份横向 + 纵向错半格**：`tileWidth = ceil((cutWidth + gapX) * 2)`，tile 内画三份 copy（`(0,0)`、`(cutW+gapX, -(cutH+gapY)/2)`、`(cutW+gapX, +(cutH+gapY)/2)`），形成官方那种**菱形交错**排布，任意角度都不缺角、不漏缝。
  - **偏移修正**：`background-position = Offset - gap/2`；为正时输出 `left/top` + `calc(100% - Npx)` 同步收窄，避免右侧/底部露出无水印白边（对齐官方 `getMarkStyle`）。
  - 内置内联 SVG 绘制（`<g transform="translate(...) rotate(...)">` + `<text>` / `<image>`），`MutationObserver` 防篡改（`Protect`，默认开启）保留；文字宽度用字宽估算法（CJK 全角按 1em 精确、拉丁按字宽表 + 安全余量）。
  - 新增 demo 页 **`/watermark`「ElWatermark 整页水印 · MainLayout 示例」**：一个真实 MainLayout 风格布局（顶栏 + 左侧导航 + 内容卡片 + 页脚）被水印整页覆盖，顶部提供「文本 / 旋转角度 / 间距 / 字号 / 颜色」实时控件，任意角度都完整、不漏白；并附「多行 `ContentList` 水印 + 无水印对比」。导航栏新增「水印整页」入口。
  - 覆盖：新增 `demo/verify-watermark.cjs` **12/12**（mask 铺满整页宽高 / tile 带旋转补偿 / 旋转 60° 后仍铺满且偏移非负 / 多行渲染 / 两张截图 / 控制台零错误）；`ElWatermarkTests` 2 例断言更新为对齐后的尺寸与偏移公式后全过；全量 bUnit **1666/1666**。样式仍只写统一 `index.css`，未新建任何 `fix.css` / `theme.css`。

- **`ElInput` 清除图标闪烁 / 点不中**：鼠标停在清除图标上时图标被反复增删（探针 12 次采样 `001001001010`），真实鼠标点击经常落空、值清不掉。根因是 Blazor 只有**会冒泡**的 `mouseover` / `mouseout`：鼠标从输入框移进内部图标时先冒泡 `mouseout`（判定离开）再冒泡 `mouseover`（判定进入），组件据此卸载又重建图标，光标下元素变化再触发下一轮（官方用的是不冒泡的 `mouseenter` / `mouseleave`）。修法：`mouseout` 后延迟 60ms（`HoverLeaveDelayMs`）再判定离开，期间收到 `mouseover` 就取消，等价于官方语义；鼠标移出后图标消失延后 60ms，肉眼不可感知。修复后采样为 `111111111111`，真实鼠标点击稳定生效（`demo/verify-input-ep.cjs` C1b / C2）。
- **`ElButton` 对照 Element Plus 官方实测样式对齐**（基准值取自 2026-09-24 用 Playwright 实测 `https://element-plus.org/zh-CN/component/button` 上真实按钮的 `computedStyle`，含 hover 态，而非凭文档记忆）：
  - **根因级修正：`is-text` 与 `is-link` 被合并成一条 CSS 规则**，导致 `is-text` 错拿了 `is-link` 的 `padding:0 / height:auto`；且两者都被刷成 primary 蓝，而官方是「继承基础/类型文字色」。现已拆成两条独立规则：
    - `is-text`（`Text` 布尔属性）：`border:0 solid transparent` + 透明底 + **保留 `8px 15px` 内边距与 32px 高**，文字色继承（无类型时 `#606266`，有类型时为该类型色），hover 为 `fill-color-light #f5f7fa`；`is-has-bg` 常显浅底、hover `fill-color`、active `fill-color-dark`；禁用态底色 `transparent !important`。
    - `is-link`：**`padding:2px` + `height:auto`**，无类型时文字 `#606266`（官方非蓝），类型色链接 hover 走 **light-5**（实测 primary `#a0cfff`）；补齐两个官方 CSS 变量 `--el-button-hover-link-text-color`（base = `--el-text-color-secondary`）/ `--el-button-active-color`（= `--el-text-color-primary`）。
  - **`type="text"` 与 `text` 布尔属性此前被当成同一件事**：官方这是两个独立开关（`type="text"` → `.el-button--text`，蓝色 + 左右内边距 0；`text` → `.is-text`）。现在 `TypeCssValue` 对 `Text` 也输出类名 → 渲染 `el-button--text`，并补齐 `.el-button--text` 全套官方规则（含 hover `light-3` / active `dark-2` / 禁用透明）；`is-text` 只由 `Text` 布尔属性触发。
  - **类型色文字/链接按钮底色**：官方 `is-text` / `is-link` 一律透明底（此前沿用 `is-plain` 的 light-9 浅底），已按 5 个类型分别覆盖。
  - **尺寸 × 形状的内边距被 `is-round` 压回**：`.el-button.is-round{padding:8px 15px}`（0,2,0）一直盖过 `.el-button--large{padding:12px 19px}`（0,1,0），导致大号圆角按钮内边距错成 8px 15px。补齐 `.el-button--large.is-round` / `.el-button--small.is-round`；并补齐官方 **圆形按钮随尺寸变 padding**（large 12px / small 5px）与 **图标+文字间距随尺寸变**（large 8px / small 4px）。
  - `is-dashed` 补齐官方 hover 变量（蓝字 + 白底 + 蓝虚线边，实测一致）。
  - **两字自动加宽改为官方做法**：此前是往文本里插一个真空格，官方是给文本 span 加 `.el-button__text--expand`（`letter-spacing:.3em; margin-right:-.3em`），已按此实现（新增同名 CSS 规则）。
  - **`ButtonConfigContext` 全局配置此前只消费了 `AutoInsertSpace`**：EP 里 `plain/text/round/dashed/type` 的默认值是 `undefined`（组件没显式传才回落全局配置），而 Blazor 的 `bool` 参数区分不出「没传」和「传了 false」。现在 `ElButton` 重写 `SetParametersAsync` 记录 `Type/Plain/Round/Dashed/Text` 是否真的被赋值，未赋值时才回落 `ElConfigProvider.Button` 的对应配置（`ButtonConfigContext.Type` 为字符串，按名解析）。
  - 覆盖：新增 `demo/verify-button-ep.cjs` **33/33**（逐项比对官方实测基准值）；既有 `probe-button-checks.cjs` 11/11、`probe-button-group-checks.cjs` 15/15、`verify-feedback-messagebox.cjs` 39/39、`verify-dialogdata.cjs` 19/19、`verify-message-stacking.cjs` 7/7、`verify-message-hover.cjs` 5/5、`verify-message-types.cjs` 27/27、bUnit `ElButton*` 7/7 全绿。样式改动集中在 `theme.css` 的 `/* ==== ElButton 对齐 Element Plus 官方 ==== */` 块。

- **`ElMessage` 类型底色被基类覆盖回归**（用户实测「官方比本地颜色深」的直接原因）：上一轮 EP 对齐块把 `background-color:#f4f4f5`（info 底色）写进了文件末尾的 `.el-message` 基类——与前面 `.el-message--success/--warning/--error` 类型规则**同特异性、但更靠后**，级联把整摞提示的类型底色全压成了 info 灰，只有文字/图标还是类型色，观感即「比官方浅一截」。修复：基类移除 `background-color`，info 底色改由显式 `.el-message--info{background-color:#f4f4f5}` 提供（success/warning/error 恢复各自的 light-9 底色）。实测（canvas 逐像素采样官方截图 vs 本地渲染）：两者文字色本就同为 `#67c23a`，差异全部来自底色；修复后 5 种类型 bg/文字/图标全部与 EP 2.9/2.11 官方值逐项一致。

- **`ElMessage` 补齐第 5 种类型 `Primary`**（EP 2.9+ 官方文档类型选择器即 Primary/Success/Warning/Info/Error 五个）：`MessageType` 枚举新增 `Primary`（追加在末尾，不动既有整数值）；`MessageService.Primary(string)` 便捷方法；`ElPopup` 图标映射——primary 复用 `el-icon-info` 实心图标（EP 同款行为），CSS 新增 `.el-message--primary`（bg=`#ecf5ff` primary-light-9、border=`#d9ecff` light-8、文字/图标=`#409eff`，图标规则用 `.el-message--primary .el-message__icon.el-icon-info` 压过通用灰）。演示页 `/feedback` 增加「主要」按钮。覆盖：新增 `demo/verify-message-types.cjs` **27/27**（5 类型 × 类名渲染/背景/文字色/图标色/图标类逐项断言 + 全家福截图 + 无控制台错误）；`verify-message-stacking.cjs` 7/7、`verify-message-hover.cjs` 5/5、`verify-feedback-messagebox.cjs` 39/39 仍全绿。

- **`ElMessage` 文字折行回归**：上一轮对齐 EP 去掉 `min-width:380px` 时给 `.el-message` 加了 `max-width:calc(100% - 32px)`——`calc()` 里混百分比在「anchor `width:fit-content` → 消息 `width:fit-content`」的嵌套内在尺寸计算链中会被 Chrome 当 0 参与钳制，消息盒被压到 min-content 宽（实测「处方保存成功」6 个汉字只剩 108px 宽、折成两行）。修复：视口钳制挪到外层 `.el-message-anchor`（`max-width:calc(100vw - 32px)`，vw 是确定值不参与内在尺寸链），`.el-message` 只留纯百分比 `max-width:100%`（内在尺寸计算期按 none 处理）。实测修复后盒宽 140px、内容单行 14px 高。

- **`ElMessage` 悬停暂停自动关闭**（对齐 EP 官方行为：鼠标放到提示上时关闭定时暂停，移开后继续倒计时）：`ElTransition` 的 path 延时从裸 `Task.Delay` 换成可暂停的 `DelayPausableAsync`（`PauseTiming()` 打断等待并记住剩余时间，`ResumeTiming()` 按剩余时间继续，暂停期间不计入，总时长不变），并新增公开方法 `PauseTiming()` / `ResumeTiming()`（不处于延时等待期时为空操作，不影响 Dialog 既有的 `Pause`/`Resume` 通道）。`ElPopup` 给每条提示的 anchor 挂 `@onmouseenter`/`@onmouseleave`，经 `MessageInfo.TransitionRef`（`@ref` 回填）转发暂停/恢复；`Duration=0` 本就不自动关，直接跳过。覆盖：新增 `demo/verify-message-hover.cjs` **5/5**（6 字消息单行 140px 宽；`Duration=3000` 的提示悬停 3.5s 超出寿命仍在；移开鼠标后按剩余时间关闭，实测 ~2.8s；全程无 JS 控制台错误）。`verify-message-stacking.cjs` 7/7、`verify-feedback-messagebox.cjs` 39/39、`ElFeedbackTests` 6/6 仍全绿。

- **`ElCheckbox` 独立复选框（不在 `ElFormItem` 内）`@bind-Value` 初始态与绑定值相反、首点翻不过来**：`OnParametersSet` 原仅在有 `FormItem` / `CheckBoxGroup` 时才把 `Status` 同步自 `Value`，独立使用时 `Status` 停留初始 `UnChecked`，导致勾选框渲染态与 `Value`（如默认 `true`）相反，且首次点击 `ChangeStatus` 在 `Status` 与 `Value` 脱钩下等于空操作、绑定的宿主状态永远翻不过来。现改为：独立且 `@bind-Value` 真正接线（`ValueChanged.HasDelegate`）时由 `Value` 同步 `Status`（保留 `Indeterminate` 不受影响），未绑定的纯展示复选框行为不变。demo `/grow` 的「圆角」「MaskClosable」开关现已正确反映并切换 `waitingRound` / `waitingMaskClosable`；新增 `ElCheckboxTests.StandaloneBoolCheckbox_ToggleEmitsTrueThenFalse` 锁住该行为。

- `ElMessage` 的 **「越弹越朝下」堆叠漂移**：位置在消息**加入时一次性写死**——新消息永远堆在「当前最后一条」下面（`prevMessage.EndTop + 60`），而某条到期消失后剩余提示**不回填坑位**，于是连发时整摞越漂越低、永不复位。三处根治：① 堆叠改为**按队列序号算槽位**（`EndTop = Offset + index × 60`），新提示总是落在固定槽位；② 垂直定位从 `ElTransition` 的内联样式（挂载后不随重渲染更新）搬到外层 `.el-message-anchor` 容器（`position:fixed` + `transition:top .4s`），任意一条被移除后只需重算剩余条的 `EndTop`，CSS 自动**平滑上移收拢**——零 JS 依赖；③ 消息循环补 `@key="message.InternalId"`，消除移除头部消息后 Blazor 按索引复用 `ElTransition` 实例导致的动画定时器串位/消息僵尸。`MessageInfo.Offset` 默认值同步 EP 官方改为 **16**。覆盖：新增 `demo/verify-message-stacking.cjs` **7/7**（连发 3 条量得 16/76/136 整齐堆叠；首条到期后剩余两条收拢到 16/76；补发后整摞仍从顶端对齐；全程无 JS 控制台错误）。

- **`ElMessage` 样式对照 Element Plus 2.9.x 官方编译 CSS 对齐**（取自 jsDelivr `element-plus/dist/index.css` 逐条比对）：① padding `15px 15px` → **`11px 15px`**；② 固定 `min-width:380px`（Element UI 遗产）→ **`width:fit-content` + `max-width:calc(100% - 32px)`**（EP 按内容自适应、长文本不撑破视口）；③ info 背景色 Element UI 旧值 `#edf2fc` → EP **`#f4f4f5`**（`--el-color-info-light-9`）；④ 关闭按钮基色 `#C0C4CC` → EP `--el-text-color-placeholder` **`#a8abb2`**；⑤ 补齐官方 `is-plain` 规则（**白底白边框 + `0 0 12px rgba(0,0,0,.12)` 浅阴影**，此前 `Plain` 参数只挂了 class、CSS 无对应规则等于无效）；⑥ 补齐 grouping 徽标 `.el-message__badge` 的 EP 定位（`right:-8px; top:-8px`）与 `el-badge--danger` 外观（红底白字圆角），并放开 `.el-message` 的 `overflow:hidden` 以免徽标被裁掉。其余（边框 `#ebeef5`、圆角 4px、success/warning/error 三色系、文字色、closeBtn hover）与官方一致，未动。行为面（`duration=0` 不自动关、`showClose`、`grouping` 合并计数、`center`、`dangerouslyUseHTMLString`、`max`）此前已对齐。覆盖：`verify-feedback-messagebox.cjs` **39/39**、`verify-message-stacking.cjs` **7/7** 仍全绿。样式改动集中在 `index.css` 的新 `/* ==== ElMessage 对齐 Element Plus ==== */` 作用域块，未触碰其他组件。
- `ElMessage` 的 **`Offset`（距顶偏移）此前是个空参数**：`MessageInfo.Offset` 声明了默认值 20、注释齐全，但渲染端从未读它 —— `ElPopup.Messages_CollectionChanged` 把首条提示的静止位置硬编码成局部变量 `movingDistance`（30px），后续提示按「上一条 `EndTop` + 30」依次堆叠，所以无论 `Offset` 设成多少都毫无效果。现在首条提示停在 `Offset` 处（`BeginTop` 仍为 0，从顶端滑入；`EndTop = message.Offset`，默认 20，与 `.el-message` 基础样式里的 `top:20px` 完全吻合），后续提示在该基准上继续向下堆叠，整摞提示的位置随 `Offset` 上移/下移。`Center`（内容居中）本就生效（`is-center`），两者合起来构成官方 `offset` / `center` 的完整对齐。覆盖：`demo/verify-feedback-messagebox.cjs` 扩到 **39/39**（新增 4 条实测 —— `Offset=140` 时提示的实际渲染位置恰为距顶 140px、`Offset=60` 时为 60px、勾选 `Center` 带 `is-center`、不勾选则不带），`ElFeedbackTests` 6/6 通过。

- Fixed five defects that made `ElTable`'s built-in editing (`IsEditable="true"`) unusable. ① **Numeric columns rendered blank**: `DisplayRenderFactory` only mapped `string` / `int` / `int?` / `DateTime` / `DateTime?` / `bool` / enums to a display renderer, so `decimal`, `double`, `float`, `long` and `short` (plus their nullable forms) got `Eval == null` — the cell showed **nothing** and the column could not take part in editing; they now map to `GenericRender`, which is type-agnostic (`value?.ToString()`). ② **`NullReferenceException` on entering edit mode**: `ElTable.razor` dereferenced `header.EditorRenderConfig` for every column with `IsEditable == true` but no null check, so any editable column lacking an editor (exactly the numeric types above) crashed the whole table on double-click; the condition is now null-guarded. ③ **`InvalidCastException` when saving the add-row**: `SaveDataAsync` called `Convert.ChangeType(EditingValue, type)` directly, so a value-type column the user never touched (`null`) threw `InvalidCast_CannotCastNullToValueType` — because the number/switch editors start empty, *every* add-row save crashed; an empty `EditingValue` now falls back to the type's default value (still `null` for `Nullable<T>`). ④ **`NullReferenceException` in the auto-generated 「操作」 column**: `btnUpdate` / `btnDelete` are a single pair of references shared by **all** rows (captured by the one closure `CreateOperationColumn` builds), so rows whose reference was never captured held `null` and clicking their button crashed; method calls now use `?.` and property assignments are explicitly null-checked (C# does not allow `x?.Prop = v`). ⑤ **Restored six mojibake (U+FFFD) literals** in that operation column — 操作 / 保存 / 更新 / 删除 / 确认要删除吗？ / 删除成功 — recovered verbatim from the clean baseline under `E:\电子病历\_backup_before_ExternalComponents` rather than guessed; note the non-editing button reads **更新**, not 「编辑」. (The file still carries ~1 900 further U+FFFD occurrences in comments and other messages — left for a dedicated pass.) Coverage: `demo/probe-table-demo.cjs`, 19/19 browser assertions.

- Refined the `ElPagination` jumper (`Layout` containing `jumper`): the `ElInputNumber` now shows its **± step buttons** stacked on the right (`ControlsPosition="right"` → `is-controls-right`) instead of `Controls="false"`, and the target page is **hard-clamped** inside `Jump(int)` (`page < 1 → 1`, `page > pageCount → pageCount`) so typing `999` snaps back to the last page — the `Min`/`Max` pair alone only disables the buttons and does not stop raw typing, because `ElInputNumber.OnInput` reports the unclamped parsed value on every keystroke. The step buttons were also **narrowed from the theme's effective 35.5px to 20px** (setting both `width` and `min-width`, since a flex item's default `min-width: auto` refuses to shrink — that is exactly why an earlier `width: 24px` had no effect) and their icons are now dead-centre (the button becomes `inline-flex` with `align-items`/`justify-content: center` and the `i` becomes `display: block; line-height: 1` — measured offset `0/0`). The box grew 54 → 64px with `.is-controls-right .el-input__inner { padding: 0 24px 0 5px }` (24px = 20px button + 4px gap) so the digits never overlap the buttons and stay centred in the text area (`textRight=870 < btnLeft=874`).

- `ElPagination` 的跳页框（`Layout` 含 `jumper`）已从**手写裸 `<input>`** 换成真正的 **`ElInputNumber`**（`Controls="false"`，即 `is-without-controls`，不带 `+/-` 步进按钮），与 Element Plus 的 `el-input-number` 结构一致：`.el-input-number.el-pagination__editor > .el-input > input.el-input__inner`（`role="spinbutton"`）。`Min=1` / `Max=pageCount` 让输入自动夹在合法页码区间内（超范围输入由 `Normalize` clamp 到边界页），跳页由 `ValueChanged → OnJumperNumberChangedAsync(decimal?)` 驱动（旧的 `OnJumperChangeAsync(ChangeEventArgs)` 已删除）。`fix.css` 中 `el-pagination__editor` 的样式改为作用域 `.el-pagination__jump .el-input-number.el-pagination__editor`：外层盒子 54×28 且**清零 padding/border/background**（EP 的 `.el-input-number` 自带 `padding:2px` + 1px 边框，会把内容区从 54px 挤成 48px 并叠出双层边框，已复位），内层 `input.el-input__inner` 负责 54×28 居中、1px `var(--el-border-color)` 边框、圆角 3px、hover `#c0c4cc`、focus 主色、`appearance:none` + 去掉 `::-webkit-*-spin-button`。覆盖：`demo/probe-jumper.cjs` 扩到 **21 条浏览器断言**（组件为 `el-input-number`、`is-without-controls`、步进按钮数为 0、外层 54×28、内层 input 54×28 + `type=text` + `role=spinbutton`、边框/居中/外观、输入 3 后激活页为 3），`ElDataDisplayExistingTests` 改取内层 `input` 提交并锁定组件类名与结构。

- `ElPagination` 的跳页框（`Layout` 含 `jumper`）之前直接渲染裸的 `<input type="number" class="el-pagination__editor">`，没走 Element Plus 的 `.el-input` 结构也没配套样式，于是露出浏览器原生控件外观：数字上下箭头、默认边框、左对齐、继承 `.el-pagination` 的粗体深色字。现在输入框改为 `type="text" inputmode="numeric"`（从根上不再产生原生 spinner，取值仍由 `int.TryParse` 处理），并在 `fix.css` 里补齐作用域到分页器的样式：`.el-pagination__jump`（inline-flex 居中、13px/400/`#606266`、左间距 16px）与 `.el-pagination__editor`（54×28、文字居中、1px `var(--el-border-color)` 边框、圆角 3px、hover `#c0c4cc`、focus 主色，并显式 `-webkit-appearance:none` + 去掉 `::-webkit-*-spin-button`）。效果对齐 EP 的「前往 [n] 页」。覆盖：`demo/probe-jumper.cjs`（11 条浏览器断言——输入框为 text、边框 `rgb(220,223,230)`、尺寸 54×28、居中、appearance none、跳页文字常规字重灰色、输入 3 后激活页变为 3、- `ElPagination` 的「每页条数」下拉同样是裸的 `<select class="el-pagination__sizes-select">`，露的是系统下拉外观（Chromium 聚焦时还会画出黑粗圆角焦点环，正是截图里的那一圈）。现在 `fix.css` 里把它对齐 EP 的尺寸选择器：`appearance:none` 去掉系统外观与焦点环、`height:28px`、13px/400/`#606266`、1px `var(--el-border-color)` 边框 + 圆角 3px、hover `#c0c4cc`、focus 主色、`cursor:pointer`，右侧箭头用**纯 CSS 双层 `linear-gradient` 拼出**（不引图标字体、不引外部 SVG，兼容 Win7 旧内核），并给 `padding-right:24px` 让文字与箭头不打架；`.el-pagination__sizes` 补 `margin-right:12px`。

### Added

- **`ElTable` 树形表格（`ElTableTreeColumn`）一键展开 / 折叠**：新增 public 方法 `ExpandAllTree()` / `CollapseAllTree()`，仅对**本地树形数据**（实体继承 `Element.TreeItemBase`，靠 `Id` / `ParentId` 串成树、未设置 `Url` 异步加载）生效；通过操作内部 `hiddenRows` 实现「展开全部节点」与「只显示根节点（ParentId==0）」。`CollapseAllTree()` 返回 `bool`（`true`=已执行折叠，`false`=表格行尚未就绪），便于调用方在 `OnAfterRender` 里重试直到表格数据装载完成。异步加载（`Url`）场景仍走既有的 `ExpandAsync` / `RefreshRootNodeAsync`。演示见 `demo/Element.Blazor.Demo/Pages/TableDemo.razor` 的「④ 树形表格」：模型 `OrgNode : TreeItemBase`（名称/类别/负责人/床位或编制/状态），19 节点的「院区 → 学科系统 → 科室 → 病区/医疗组」四级扁平数据（双根 总院/东院区），首帧默认折叠、并提供「展开全部 / 折叠全部」按钮。覆盖：新增 `demo/verify-tree-table.cjs` **8/8**（初始 2 根 → 展开全部 19 → 折叠 2 → 单节点展开 17 且子节点可见、缩进占位存在，全程无 JS 控制台错误）。

### Added

- Added a dedicated **ElTable 进阶** demo page (`/table`, sidebar entry 「表格进阶」) with two sections. ① **Editable table** — `IsEditable="true"` plus `OnSave`: double-click a row (or use 更新 / 保存 in the auto-generated 「操作」 column) to edit in place, fill the trailing add-row and press 保存 to append, and 删除 which asks for confirmation first. The page handles `TableSaveEventArgs.Action` (`Create` / `Update` / `Delete`), assigns ids on create, and calls `ElTable.Refresh()` afterwards because the component never mutates `DataSource` itself. Columns exercise every editor that currently has a renderer — string / int / decimal → `ElInput<T>`, bool → `ElSwitch<bool>`; `ID` is `IsEditable="false"` so it is carried over as the row identity on update. ② **Custom template columns** — the same `ElTable` decorated with `ChildContent` (`@context` is the row) and `ElTableTemplateColumn` (no `Property` required): a two-line composite cell (bold name + muted subtitle), right-aligned `¥0.00` currency, a threshold-coloured stock bar (red < 100, amber < 500, green ≥ 500), conditional 在售 / 下架 text, and a 详情 button living inside a template cell. Coverage: `demo/probe-table-demo.cjs`, 19/19 browser assertions — including a real double-click → keyboard edit → save → row-reload round trip, an add-row create, and zero console errors.

- Realigned the `ElDateTimePicker` / `ElTimePicker` time spinner with Element Plus's own `el-time-panel` (EP's default is **scroll-to-select**): the DOM is now EP's three-level layout — `div.el-time-spinner__wrapper` (the only scroll container) > `ul.el-time-spinner__list` (80px spacers from `::before/::after`) > `li.el-time-spinner__item.active` — nested in `.el-time-panel__content.has-seconds`, so the middle selection band (drawn by pseudo-elements on the **non-scrolling** parent) stays put and every column's selected value stops on the same horizontal line. Previously the `ul` was the wrapper with a hand-rolled `padding: 80px 0`: the band scrolled along with the content and nothing fed the scrolled value back, so a `14:00:00` value could display `14 / 02 / 02`. `dom.js` gained shared helpers `elementTimeSpinnerAttach` / `elementTimeSpinnerAlign` / `elementTimeSpinnerDispose` (scroll debounce ~100 ms like EP, snap to the nearest item, report back through `OnTimeSpinnerSelect(panel, part, value)`) and `elementPickerOutsideAttach` / `elementPickerOutsideDetach` (click outside closes the popover — and closing now also collapses the spinner, so reopening never leaves the time panel hanging open). Selected items render `#000` + `font-weight: 700` on a light band. `ElTimePicker` now reuses `TimeSpinnerRenderer` instead of its own column builder, so both pickers keep one DOM contract. Coverage: `TimePanel_UsesElementPlusSpinnerDom` and `Guard_ActiveSpinnerItemIsBlackAndBold` unit cases (full suite 1567/1567) plus `demo/probe-time-spinner.cjs` (15 assertions — per-column selected value equals the field value, all three centred within 2px, black/bold, outside-click closes, reopen collapsed, scroll changes the value). See `docs/datetime-picker-alignment.md`.

- Fixed the time spinner's **wheel step** (one mouse-wheel notch used to jump ~3 values) and the **dead spot on the highlighted row**. ① A native wheel notch is ~100px ≈ 3 rows at 32px, so minute `00` jumped straight to `03`. The notch is now consumed at the **panel** level, the hit column is advanced by exactly one row and the value is reported to .NET immediately: the row height is measured from the items (never hard-coded), `deltaMode` LINE/PAGE is normalised, a trackpad's small deltas accumulate to a half row so they step one at a time too, and the first/last row simply stops. The listener sits on the panel rather than on each column because the selection band is drawn by `.el-time-panel__content`'s `::before/::after`, whose host **is** the content box — with the mouse resting on the highlighted middle row the event target is the content, so a column-level listener never fired and the wheel did nothing there at all. ② The same root cause made `:hover` and click dead on the highlighted row: `.el-time-panel__content` is now `pointer-events: none` with its direct children back to `auto`, so the hit chain returns to the real list items (measured: `elementFromPoint` on a column centre went from `DIV.el-time-panel__content has-seconds` to `LI.el-time-spinner__item active`). `elementTimeSpinnerAttach` also became idempotent per column, so a Blazor re-render that replaces the column elements re-binds them automatically. `demo/probe-time-spinner.cjs` grew five real-mouse assertions (one notch = ±1 row on hour/minute/second, three consecutive notches = +3, reverse = −1, columns still on the band afterwards, and the values visible inside a column read as consecutive +1 so a visually skipping column cannot pass either) — 22/22, with `verify-datetime.cjs` 33/33, `verify-pickers.cjs` 29/29 and the full unit suite green. See `docs/datetime-picker-alignment.md`.

- Fixed the time spinner's **fade-out masks ghosting the selected row**. The top/bottom white gradients that dim the off-band rows were pseudo-elements on the **scroll container** (`.el-time-spinner__wrapper`), and inside a scroll container an absolutely positioned pseudo-element is part of the scrolled content — the masks travelled with the list instead of staying pinned. Because a column stops at `scrollTop = 1 + 32 × value` while the lower mask sits at `containerHeight − maskHeight`, the mask slid up into the middle selection band for the low values (`00`–`03`) and washed out the top half of the highlighted row, which is exactly why it read as "only `03` is obscured". Both masks now hang on the non-scrolling `.el-time-spinner` (`position: relative`) and are sized `calc((100% - 34px) / 2)`, i.e. 1px clear of the 32px band. Root cause proven by dyeing the two masks opaque red/blue and screenshotting every value (at `02`/`03` the blue mask covered the band; after the fix it never reaches it). `demo/probe-time-spinner.cjs` grew 3 assertions that lock this down: the scroll container carries **no** mask, the mask lives on `.el-time-spinner` with `position: absolute`, and for every value in `0..5 / 12 / 23` the selected row lies completely outside both masks — 25/25, with `verify-datetime.cjs` 33/33, `verify-pickers.cjs` 29/29 and the full unit suite green. See `docs/datetime-picker-alignment.md`.

- Fixed `ElTimePicker` **opening on `00` instead of the current time**. The field read `08:30:00` but every column stayed at `scrollTop = 0`, so the band showed `00 00 00` and never refreshed. The spinner was aligned exactly once, from `OnAfterRenderAsync`, and the popover is inserted **inside its `scaleY()` open animation** — measured panel height at that instant: 10px (final: 228px). With the geometry squashed, `center()` computed `delta ≈ 0`, took its `|delta| < 1` early-return and silently did nothing; nothing re-ran it later, so the columns stayed at `0`. The one-shot align is now a **converge loop**: `center()` reports `-1` (not laid out yet) / `0` (already on the band) / `1` (moved) and `stabilize()` re-runs on `requestAnimationFrame` until two consecutive frames need no movement (2s deadline). It yields immediately to the user — a real wheel notch, a scrollbar drag or any genuine `scroll` cancels it — and to tell "our own align scroll" from "the user scrolled", each column remembers the `scrollTop` it last wrote (`ctl.target`) and ignores scroll events landing within 1px of it; without that, the scroll event produced by the align itself would run `settle()` → `commit()` and push a bogus value into .NET mid-animation. Measured after the fix: band values `08 / 30 / 00` within 1px at `t = 500ms`. New regression `demo/probe-time-picker-open.cjs` — 21 assertions (band value == field value on first open / reopen / after a value change, align does not mutate the field, binding intact, a real wheel notch still steps one row right after opening, confirm commits, `CONSOLE ERRORS: []`) — green, with `probe-time-spinner.cjs`, `verify-datetime.cjs` and `verify-pickers.cjs` still green. See `docs/datetime-picker-alignment.md`.

- Rebuilt `ElDateTimePicker`'s popup time area as an Element Plus style spinner editor so the pending time is actually readable and editable. The footer now renders a read-only `HH:mm:ss` field (`.el-date-time-picker__time-input`) with a leading label and a time icon, and clicking it opens a three-column hour / minute / second spinner popover **above** the field (`.el-date-time-picker__time-panel`, 24 / 60 / 60 rows) with `取消` / `确认` buttons; range mode gets one field per side (`data-time-panel-host="start" / "end"`) with the `开始` / `结束` labels. Picking a column writes straight through, `确认` collapses the panel keeping the value and `取消` rolls back to the snapshot taken on open. The spinner markup comes from the shared `TimeSpinnerRenderer`, so `ElTimePicker` and `ElDateTimePicker` keep one DOM contract (`li[data-time-part='hour'][data-time-value='9']`). All four texts are localized — `TimeLabelText`, `StartTimeLabelText` / `EndTimeLabelText`, `ConfirmText`, `CancelText` — resolving through `ElConfigProvider Locale` (zh-CN `时间` / `开始` / `结束` / `确认` / `取消`, en-US `Time` / `Start` / `End` / `OK` / `Cancel`, zh-TW `時間` / `開始` / `結束` / `確認` / `取消`) with per-component overrides taking priority. See `docs/datetime-picker-alignment.md`.

- Added `demo/verify-datetime.cjs`, a 33-assertion real-browser regression for `ElDateTimePicker`'s time area (field visibility and font size, panel geometry, spinner columns and current-value highlighting, select / confirm / cancel round-trips, host binding update through the calendar, en-US texts, console-error check), plus 2 `ElDateTimePickerTests` cases pinning the ARIA output.

- Added a data loading / reading API to `ElAccountTableGrow` so hosts no longer have to build `ElAccountTableGrowRow` lists by hand: `LoadRows(IEnumerable<T>)` maps business entities by **property-name convention** — `摘要` / `Abstracts`, `会计科目` / `AccountSubject`, `借方金额` / `AmountIn` (plus `DebitAmount`), `贷方金额` / `AmountOut` (plus `CreditAmount`) — and a `LoadRows(IEnumerable<T>, Func<T, ElAccountTableGrowRow>)` overload takes an explicit selector when the names differ; both pad the grid back up to `DefaultRowCount` so trailing empty input rows are preserved. Reading back is symmetric: `Rows` is the live list, `GetData()` returns only the filled rows (`GetData(true)` keeps the blank ones, and `GetData(row => dto)` projects straight into the host's own type), so a 10-row grid holding 4 vouchers saves 4. `Clear()` leaves one blank row and `ResetRows()` rebuilds the empty grid. See `docs/account-table-alignment.md`.

- Added three dropdown-panel controls to `ElDropDownTableGrow`: `Size` (`InputSize.Normal` default / `Small` compact / `Large`), which is forwarded to the inner `ElTable` (`el-table--small`) and additionally tightens panel padding, search-box height and cell gutters; `PanelWidth` / `PanelMinWidth` / `PanelMaxWidth`, which decouple the **panel** width from the trigger width (`Width`) so a 240px trigger can open a 640px panel, with `PanelMinWidth` acting as a floor and `PanelMaxWidth` (e.g. `90vw`) as a viewport guard; and field-scoped search via the previously undocumented `SearchProperties` — supply one or more property names and any single hit counts as a match, leave it empty to scan every public property. See `docs/dropdown-table-grow-options.md`.

- Added `ElPivotDataGridGrow`, an Element Plus-style pivot / cross-tab grid (self-rendered, **zero Radzen dependency and zero JavaScript**) — the Element Plus self-rendered counterpart of Radzen's `RadzenPivotDataGrid`. Fields are declared with three registration-only child components that render nothing and register into the parent grid via a cascading parameter: `ElPivotColumnGrow<TItem>` (extra params `Width`, `MaxGroups`, `OthersLabel`, `HeaderTemplate`), `ElPivotRowGrow<TItem>` (`MaxGroups`, `OthersLabel`) and `ElPivotAggregateGrow<TItem>` (`Aggregate`, `TextAlign`, `FormatString`, `FormatProvider`, `Template`, `ColumnTotalTemplate`, `RowTotalTemplate`, `HeaderTemplate`); all three share `ElPivotFieldGrow<TItem>` (`Property`, `Title`, `Type`, `SortOrder`, `SortOrderSequence`, `Sortable`, `Selected`, `Filterable`, `FilterValue`, `SecondFilterValue`, `FilterOperator`, `SecondFilterOperator`, `LogicalFilterOperator`, `FilterTemplate`, `FilterValueTemplate`, `SecondFilterValueTemplate`). The whole computation lives in a **pure-C# engine decoupled from Blazor** (`ElPivotEngine.Build<TItem>(data, rows, columns, aggregates, options)` → `ElPivotLayout<TItem>`; see `ElPivotDataGridGrowModels.cs`), so grouping, aggregates, totals, sorting, drill-down, `MaxGroups`/others-bucketing and filtering are all unit-testable without a browser and the layout is the single source of truth shared by rendering and string export. Property access **replaces Radzen's `PropertyAccess` + `System.Linq.Dynamic.Core`** with a self-written reflection accessor (`ElPivotPropertyAccess`: nested paths `A.B.C`, `IDictionary<string, object>`, typed `Dictionary<K,V>` via non-generic `IDictionary`, string indexers, case-insensitive fallback, null safety, `CompareValues`/`ToSortKey`/`GroupKeyComparer`) — note that `GroupKeyComparer` deliberately treats `bool` as non-numeric while treating numeric strings as equal to numbers, so `Equals` and `GetHashCode` stay consistent. Aggregates (`ElPivotDataGridGrowAggregator`): `Sum`, `Average`, `Count`, `CountDistinct`, `Min`, `Max`, `First`, `Last` (`ElPivotAggregateFunctions.Available(type)` restricts numeric-only functions to numeric properties); empty buckets yield `null` rather than `0`. Filters (`ElPivotDataGridGrowFilter`): 17 operators (`Equals`, `NotEquals`, `<`, `<=`, `>`, `>=`, `Contains`, `DoesNotContain`, `StartsWith`, `EndsWith`, `In`, `NotIn`, `IsNull`, `IsNotNull`, `IsEmpty`, `IsNotEmpty`, `Custom`) with two-condition logical combination per field plus `And`/`Or` across fields, and string comparisons honouring `CaseSensitivity` through `StringComparison.Ordinal`/`OrdinalIgnoreCase` (never culture-dependent); an aggregate-level filter restricts the records entering the pivot, matching the reference behaviour. Two **real bugs found and fixed while writing the tests**: text operators `Contains`/`DoesNotContain`/`StartsWith`/`EndsWith` previously fell through and silently passed every record, and string comparison ignored the `CaseSensitive` flag. Main-grid parameters: `Data`, `Columns`, `Rows`, `Aggregates`, `AllowFieldsPicking`, `FieldsPickerHeaderText`, `FieldsPickerHeaderTemplate`, `FieldsPickerExpanded`, `SelectedRows`/`SelectedColumns`/`SelectedAggregates` (+ `…Changed`), `FieldSelectionChanged`, `AllowSorting`, `AllowFiltering`, `AllowDrillDown`, `ExpandGroupsByDefault`, `FilterCaseSensitivity`, `LogicalFilterOperator`, `Filter`, `ShowRowsTotals`, `ShowColumnsTotals`, `ShowAggregateHeaderRow`, `AllowAlternatingRows`, `GridLines`, `MaxHeight`, `IsLoading` (overlay only — content is preserved), `AllowPaging`, `PageSize`, `PageIndex`, `PageIndexChanged`, `PageSizeChanged`, `PagerPosition`, `PageSizeOptions`, `ShowPagerSummary`, `PageNumbersCount`, `Strings`, `EmptyText`, `EmptyTemplate`, `LoadingTemplate`, `CellClicked`, `SortChanged`, `FilterChanged`, `DrillDownToggled`, `GroupStateChanged`. Members: `GetLayout()`, `BuildEngineOptions()`, `Invalidate()`, `ReloadAsync()`, `ColumnsCollection`/`RowsCollection`/`AggregatesCollection`, `CandidateFields`, `TotalRows`/`PageCount`/`CurrentPage`/`EffectivePageSize`, `GetPagedRows()`, `GetRenderRows()`, `GetCellValue`/`GetRowTotal`/`GetColumnTotal`/`GetGrandTotal`, `ToggleSortAsync`/`GetSortOrder`/`GetSortIcon`/`GetSortTitle`, `OpenFilterAsync`/`ApplyFilterAsync`/`ClearFilterAsync`/`CloseFilterPanelAsync`/`SetFilterOperatorAsync`/`SetFilterValueAsync`/`SetSecondFilterOperatorAsync`/`SetSecondFilterValueAsync`/`SetLogicalFilterOperatorAsync`/`FilterContext`/`IsFilterPanelOpen`, `ToggleDrillDownAsync`/`IsGroupCollapsed`/`SetAllGroupsAsync`, `GoToPageAsync`/`ChangePageSizeAsync`/`GetPageNumbers()`/`GetPagerSummary()`, `ToggleFieldsPickerAsync`/`IsRowSelected`/`IsColumnSelected`/`IsAggregateSelected`/`ToggleFieldSelectionAsync`/`GetAggregateFunction`/`GetAvailableAggregates`/`ChangeAggregateFunctionAsync`, `GetAlignClass`, `DescribePath` (+ `EmptyPathSegmentText` for null group values), `IsActivationKey`, and the event-value parsers `ToBool`/`ToInt`/`ParseAggregateFunction`/`ParseFilterOperator`/`ParseLogicalOperator`/`IsEnumType`/`GetEnumOptions`. A **registration-after-layout timing trap** is handled explicitly: the layout is computed at the top of the render tree while field components register during the diff phase, so the first frame would always render an empty "no fields" state — derived (non-controlled) field selections are discarded on every register/unregister (`ResetDerivedSelection`) and a `fieldsDirty` flag triggers a re-render from `OnAfterRender`, while explicitly bound `SelectedRows`/`SelectedColumns`/`SelectedAggregates` are never discarded. Multiple value fields are supported side by side (value cell index = `leafIndex * Aggregates.Count + aggregateIndex`); row headers get computed `rowspan`/placeholder cells in `ElPivotDataGridGrowRenderModels.cs`; icons are hand-drawn inline SVG (`ElPivotDataGridGrowIcons`, no icon font/package, no `<text>` element) and every user-visible string is overridable through `ElPivotDataGridGrowStrings`. The shipped CSS section in `wwwroot/css/fix.css` deliberately avoids `:has()`/`:is()`/`:where()`/CSS nesting/logical properties/`clamp()`/flex `gap`/`!important`; four guard tests scan the real sources and stylesheet to enforce zero third-party dependency, zero JavaScript, browser-compatible CSS and full style coverage. Documented in `docs/pivot-data-grid-alignment.md` and covered by `test/Element.ComponentTests/ElPivotDataGridGrowTests.cs` (163 cases, all green).

- Added `ElViewerGrow`, an Element Plus-style **document viewer** (self-rendered, zero third-party dependency and **zero custom JavaScript**) — the Element Plus self-rendered counterpart of the `jit-viewer` Blazor wrapper in `jit-viewer-sdk-main/blazor`. The reference implementation is a thin shell: 154 lines of Razor plus 159 lines of a service whose eight `InvokeVoidAsync` calls forward everything to a ~10 MB bundled JS SDK (`jit-viewer.min.js`), so nothing about the preview is observable from C#. `ElViewerGrow` pulls the capability back into C#: type detection, URL safety, filename inference, size formatting, the zoom ladder, rotation normalisation, inline data URIs and external-viewer URL templates live in a **pure-C# engine** (`ElViewerGrowEngine`), while `ElViewerGrowRenderer` builds document markup as strings — plain text/JSON into `<pre>`, a hand-written Markdown subset (ATX headings, fenced code, horizontal rules, block quotes, ordered/unordered lists, inline code/bold/italic/links; no inline HTML), CSV/TSV parsed per RFC 4180 with delimiter auto-detection into a table, images/video/audio into native elements, and PDF into an `<iframe>` backed by the browser's own PDF viewer. Office/OFD/CAD/3D are **not** silently faked: they surface an explicit `Unsupported` state unless `ExternalViewerUrl` (with `{url}`/`{name}`/`{ext}`/`{kind}` placeholders) is configured, in which case the document is embedded from that service. Toolbar is a **data-driven descriptor table** (`ElViewerGrowToolbar.Default`, 19 entries incl. 5 separators; `Minimal`, `Build`, `Without`) so shape and ordering can be asserted directly and overridden wholesale via `Toolbar` or trimmed via `HiddenTools`; icons are 17 hand-drawn 24×24 inline SVGs (`ElViewerGrowIcons`). Everything the reference needed JavaScript for is done declaratively: zoom/rotation are CSS `transform` on the canvas (ladder `0.25…4`, buttons auto-disable at the limits), maximize is `position: fixed` inside the page (button swaps to a restore icon and label), download is a native `<a download href>` fed by the URL or a base64 data URI, printing is a `@media print` block that hides the chrome and neutralises the transform, and file picking uses the framework's `InputFile` with `MaxFileSize` enforcement. State is a real enum — `ElViewerStatus` (`Empty`/`Loading`/`Ready`/`Error`/`Unsupported`) — surfaced through `OnStatusChanged` / `OnError` instead of a concatenated string; `Theme`, `Locale` and a fully overridable `ElViewerGrowStrings` (zh-CN + en built in) replace the reference's hard-coded Chinese. Security: `IsSafeUrl` allow-lists http/https/blob plus whitelisted `data:` prefixes and relative addresses, **decodes HTML entities and strips control characters before reading the scheme** so `&#106;avascript:` / `javascript&colon;` / tab-split schemes cannot slip into an attribute; `data:text/html` is rejected; all document text passes `EscapeHtml` before it becomes markup; Markdown links only emit `href` when the target passes the same check; data URIs are refused past 8 MB and uploads past `MaxFileSize`. Parameters (41): `Document`/`DocumentChanged`, `Src`, `FileName`, `ContentType`, `Content`, `Text`, `AutoLoad`, `Loading`, `Theme`, `Locale`, `Strings`, `Width`, `Height`, `ShowToolbar`, `ShowStatusBar`, `Toolbar`, `HiddenTools`, `Bordered`, `Zoom`/`ZoomChanged`, `Rotation`/`RotationChanged`, `MinZoom`, `MaxZoom`, `PageNumber`/`PageNumberChanged`, `PageCount`, `Maximized`/`MaximizedChanged`, `AllowFileOpen`, `Accept`, `MaxFileSize`, `ExternalViewerUrl`, `PreferExternalViewer`, `ContentTemplate`, `EmptyTemplate`, `UnsupportedTemplate`, `OnDocumentChanged`, `OnStatusChanged`, `OnError`; commands `LoadAsync`/`LoadUrlAsync`/`LoadTextAsync`/`LoadBytesAsync`/`CloseAsync`/`ReloadAsync`/`SubmitUrlAsync`/`ZoomInAsync`/`ZoomOutAsync`/`SetZoomAsync`/`ResetZoomAsync`/`RotateLeftAsync`/`RotateRightAsync`/`SetRotationAsync`/`ResetViewAsync`/`ToggleMaximizeAsync`/`GoToPageAsync`/`PrevPageAsync`/`NextPageAsync`/`ExecuteAsync`/`ToggleUrlBarAsync`. Also fixed in the reference's own design while porting: `EnsureViewerDestroyedAsync` destroyed the container it was about to create (leaking the previous instance), `FixOfdSignatureTransparencyAsync` swallowed its exception, `FullscreenAsync`/`GotoPageAsync` were never called, theme/locale were not exposed, and the `accept` list advertised `.dxf`/`.glb`/`.stl` with no Blazor-side support — `DefaultAccept` now lists only what the built-in renderer actually handles. The shipped CSS section in `wwwroot/css/fix.css` (466 lines) avoids `:has()`/`:is()`/`:where()`/`:focus-visible`/`accent-color`/`clamp()`/logical properties/`gap`/`grid-template`/`aspect-ratio`/`backdrop-filter`/CSS nesting/`!important`, keeps every selector scoped under `.el-viewer-grow` and gives every `var(--el-*)` a static fallback. Documented in `docs/viewer-alignment.md` and covered by `test/Element.ComponentTests/ElViewerGrowTests.cs` (145 cases, all green). OFD is additionally openable without breaking the zero-dependency rule: `ElViewerGrowRenderer` exposes a `RegisterRenderer(kind, Func<byte[], string>)` / `UnregisterRenderer` / `HasRenderer` plug-in point plus a `ContentLoader` parameter (`Func<ElViewerDocument, Task<byte[]>>`) for documents that only have a URL, so a host can wire in its own OFD→SVG library (e.g. netofd's `OfdSvgMaker`) and the same component renders OFD as inline SVG; unregistered types still surface `Unsupported` rather than faking support, a renderer that returns empty or throws falls back to the built-in path instead of blanking the view, injected markup is run through `SanitizeMarkup` (strips `script`/`iframe`/`object`/`embed` blocks, every `on*` handler and `javascript:`/`vbscript:`/`data:text/html` URIs) before being inlined, and `ResolvedAccept` only advertises `.ofd` once a renderer is actually registered.
- Added built-in **Office preview** to `ElViewerGrow` for `docx` / `xlsx` / `pptx` (gated by `EnableOfficePreview`, default `true`): the ZIP+XML payload is parsed **entirely in pure C#** via `System.IO.Compression.ZipArchive` + `System.Xml.Linq.XDocument` — **no third-party library and no script download**, unlike the ~10 MB `jit-viewer.min.js` that the reference forwards everything to. Four new files (`ElViewerGrowOffice.cs` shared OOXML helpers, `ElViewerGrowOfficeWord.cs` / `ElViewerGrowOfficeExcel.cs` / `ElViewerGrowOfficePresentation.cs`) render Word (headings/paragraphs/bold/italic/underline/strikethrough/tables/images/hyperlinks), Excel (sharedStrings + inlineStr, merged cells, multiple sheets, date-by-style detection, `MaxRows=2000` cap) and PowerPoint (slide → layout → master placeholder inheritance for position & font size, percentage absolute positioning with `cqw` units that scale with preview width). Rendering is the single source of truth shared by the component and unit tests; every office document's `RenderedOfficeHtml` is **cached** so zoom/rotate re-renders don't re-unzip the whole package. Legacy binary `.doc`/`.xls`/`.ppt` (OLE compound files) are detected and shown an explicit "save as docx/xlsx/pptx" hint rather than a vague "unsupported". Covered by 6 new `ElViewerGrowTests` cases (real sample files embedded as resources) plus a 66-case isolated `ooxml_probe` parser harness; demo route `/grow` adds a live Office section driven by `HttpClient` byte fetches.
- Added viewer **UX round** to `ElViewerGrow`: ① a **Print** toolbar action (`ElViewerToolbarAction.Print`, printer icon, localisable title) with a `PrintAsync()` command — rendered content (text/Markdown/Office/OFD/image) prints via the component's existing `@media print` rules through a bare `window.print()`, while iframe-shaped documents (PDF / external viewer) hand their source URL to a new ES5 bridge `elementPrintDocument(url)` in `dom.js`, which mounts a screen-off hidden iframe, calls the browser PDF viewer's own `print()` and falls back to `window.open` on cross-origin restrictions (page-level `window.print()` is unreliable for iframe PDF plugins). ② a **loading overlay gate**: the spinner overlay already existed but synchronous loads (open file / URL / parameter swap) computed a terminal state within one frame so it never painted; a load gate (`MinLoadingMilliseconds`, default `0` = exact legacy synchronous contract, demo uses `400`) now holds `Loading` across `LoadAsync`, `OnInitialized` (first open incl. `@key` rebuilds) and parameter-driven swaps, pre-warms the lazy OOXML parse inside the gate and dispatches every `StateHasChanged` through `InvokeAsync` so post-`Task.Yield` continuations stay dispatcher-safe. ③ a **zoom scrollbar fix**: CSS `transform` does not participate in layout, so zoomed overflow was unreachable (center origin also flings half of it above/left where scrolling cannot go); the canvas now pins `transform-origin: 0 0` inline when zoomed without rotation (overflow only grows right/down → reachable scrollbars) while rotation keeps the centered origin, and the PDF iframe form (`.is-embed`) ditches `transform` for zoom entirely in favour of a real layout width `calc(100% * s)` so the browser PDF viewer renders crisp and scrolls natively. Covered by 5 new `ElViewerGrowTests` cases (print bridge invocation for rendered vs embed documents, loading-gate event sequence, default-zero synchronous contract, transform-origin scoping) and a new browser E2E `demo/verify-viewer-ux.cjs` (9 assertions: print button present & click-safe, loading overlay actually painted, scroll region grows 1072×618 → 3216×1854 and is scrollable to the borders, zoom reset restores, console clean).

- Fixed `ElViewerGrow` **print isolation**: the content-form print path used to call a bare `window.print()`, which queued the *entire host page* into the print job (the `/grow` demo produced a 94-page printout where the file content appeared only after pages of page chrome). Following the reference `printWindow` approach, `elementPrintDocument(null, root)` now serialises the viewer canvas plus the page's `<style>`/`<link>` stylesheets into a screen-off independent iframe (A4-like 794px width) and prints that document, so the paper contains **only the file itself**, paginating naturally by paper width. The `@media print` block that gets cloned along neutralises the zoom-time inline `min-width`/`min-height`/layout width with `!important` (the only place the CSS guard allows it), so zoomed canvases print at natural size. ② Made the **loading overlay actually noticeable**: the gate worked but a 400 ms flash over a faint mask was easy to miss — the overlay is now a centred card (36 px spinner + shadowed caption) over a stronger mask, and the demo holds it for 900 ms. Covered by 3 new browser assertions (independent print document created; contains the Excel canvas; contains no demo-page content) in `demo/verify-viewer-ux.cjs` (12/12) plus full-suite green (1441/1441).
- Fixed `ElViewerGrow` **PDF print blank page**: local PDFs are inlined as `data:application/pdf` URIs, which are opaque origins — the hidden print iframe could not reach `contentWindow.print()` (cross-origin), the fallback `window.open(data:...)` was blocked by Chrome's top-frame data-URL navigation, and the user just saw an empty popup. `elementPrintDocument` now fetches the source into a **blob URL** (same-origin) in the page context first and prints through that, keeping direct-iframe and `window.open` as escalation fallbacks; the print iframe no longer uses `visibility: hidden` (Chrome's PDF plugin ignores print commands inside hidden frames). ② Fixed **PDF zoom had no vertical scrollbar**: the embed canvas grew only `width: calc(100% * s)` while the layout box height stayed at viewport 100%, so the outer viewport never produced a vertical scrollbar; zoom now grows **both** width and height. Covered by a new `Zoom_EmbedDocument_GrowsBothWidthAndHeight` unit test plus browser assertions (PDF zoom grows scroll area in both axes and actually reaches the bottom) in `demo/verify-viewer-ux.cjs` (20/20).
- Fixed `ElCheckbox<TValue>` **standalone bool binding never emitting `true`**: without `TrueValue`/`FalseValue` and outside a `ElCheckboxGroup`, toggling on emitted the *current* `Value` (still `false`) instead of `true`, so `@bind-Value="someBool"` could never flip its target and host components never re-rendered. The standalone-bool path now emits `true` on check and `false` on uncheck; covered by a new `StandaloneBoolCheckbox_ToggleEmitsTrueThenFalse` test.
- Added `ElHtmlEditorGrow`, an Element Plus-style rich-text editor (self-rendered, zero Radzen dependency) — the Element Plus self-rendered counterpart of Radzen's `RadzenHtmlEditor`. The reference implementation's tool set is replaced by a **data-driven descriptor table** (`ElHtmlEditorGrowToolbar.Default`, 45 entries: 34 buttons, 3 selects, 2 colours, 8 separators) so the toolbar shape, ordering, icons, shortcuts and per-mode availability can be asserted directly in unit tests, and can be overridden wholesale via `Tools` / `HiddenTools` / `ChildContent`. Aliased commands: `insertHtml` → native `insertHTML`, plus `insertHorizontalRule`; table commands (`insertTable`, `updateTable`, `addRowBefore/After`, `addColumnBefore/After`, `deleteRow/Column`, `deleteTable`, `mergeCellRight`, `mergeCellDown`, `splitCell`, `copyCells`, `pasteCells`) deliberately bypass `execCommand` and go through the self-written table channel. **All markup is built in pure C#** (`ElHtmlEditorGrowHtmlBuilder`: `BuildTable` / `BuildImage` / `BuildLink` / `BuildHorizontalRule` / `BuildCellStyle` / `ResolveBorder` / `ResolveCellPadding` / `NormalizeLength`, one source of truth shared by rendering and tests) — JS only inserts the finished string at the caret and manages the selection. Browser state comes back as a JSON string parsed by `ElHtmlEditorGrowState` / `ElHtmlEditorGrowTableSelection` / `ElHtmlEditorGrowTableArgs`, so status display, table selection and table-command payloads are all unit-testable without a browser. Table editing rebuilds a **logical grid** from `colspan`/`rowspan` before every row/column operation, so merged cells no longer shift rows or columns; merges require matching spans, and failures return localisable codes (`table.action.requiresTable` / `table.merge.invalidSelection` / `table.paste.blocked`) translated by `ElHtmlEditorGrowTableStrings.Resolve`. Paste sanitiser (`ElHtmlEditorGrowSanitizer`, toggleable via `SanitizePaste`): tag + attribute whitelists (with `data-*` opt-out), dangerous containers (`script`/`style`/`iframe`/`object`/…) removed **together with their content**, `on*` handlers always dropped, dangerous URI schemes (`javascript:`/`vbscript:`/`data:text/html`, including entity-obfuscated and whitespace-padded forms) and `style` expressions blocked, sharing one security core with `ElMarkdownGrowSanitizer`. Icons are ~40 hand-drawn 24×24 inline SVGs (`ElHtmlEditorGrowIcons`) — no icon font, no icon package, and no `<text>` element (a pre-existing Razor/SVG pitfall in this repo). Parameters: `Value`/`ValueChanged`, `Change`, `Input`, `Mode`/`ModeChanged`, `ShowToolbar`, `EnableDefaultToolbar`, `ChildContent`, `Tools`, `HiddenTools`, `ReadOnly`, `Disabled`, `Placeholder`, `Height`, `MinHeight`, `AutoFocus`, `SanitizePaste`, `SanitizeOptions`, `Paste`, `Execute`, `UploadUrl`, `UploadHeaders`, `UploadFieldName`, `Accept`, `UploadComplete`, `UploadError`, `TableStrings`, `ToolbarCls`, `DefaultTableRows`, `DefaultTableColumns`, `DefaultTableWidth`. The self-written JS channel in `wwwroot/js/dom.js` (`elementHtmlEditorAttach`/`Detach`/`PickFile`/`GetHtml`/`SetHtml`/`Exec`/`QueryState`/`SaveSelection`/`RestoreSelection`/`SelectionText`/`GetTableSelection`/`TableCommand`) is **ES5 only** (no arrow functions, template literals, `let`/`const` or optional chaining) and uses nothing beyond `Selection`/`Range`, `document.execCommand` and `XMLHttpRequest`; every call is guarded so the component still renders and binds in bUnit / SSR / prerendering. Browser compatibility of the shipped CSS is enforced by guard tests scanning the `.el-html-editor-grow*` section (no `:has()`/`:is()`/`:where()`/`:focus-visible`/`accent-color`/`clamp()`/logical properties/flex `gap`/`!important`, every selector scoped, every `var(--el-*)` with a fallback). Documented in `docs/html-editor-alignment.md` and covered by `test/Element.ComponentTests/ElHtmlEditorGrowTests.cs`.
- Added `ElQRCodeGrow`, an Element Plus-style QR Code component that renders inline SVG.
- Added `ElQRCodeGrow`, an Element Plus-style QR Code component that renders inline SVG. Full ISO/IEC 18004 support for versions 1–40 and error-correction levels L/M/Q/H, with automatic **multi-segment optimisation** across numeric / alphanumeric / byte (UTF-8) modes, automatic mask selection by the four penalty rules, and block-wise Reed-Solomon error correction. Parameters: `Value`, `Ecc`, `Size`, `Foreground`, `Background`, `QuietZone`, `ModuleShape`, `EyeShape` (+ per-corner `EyeShapeTopLeft`/`TopRight`/`BottomLeft`), `EyeColor` (+ per-corner overrides), `Logo`, `LogoSizePercent`, `LogoPadding`, `LogoCornerRadius`, `LogoBackground`, `LogoBackgroundOpacity`, `MergeModules`, `OptimizeSegments`, `UseEci`, `MinVersion`, `MaxVersion`, `ShowError`, `ErrorText`, `ErrorColor`. Members: `ToSvg()` (standalone SVG string, no JS required), `DownloadPngAsync()`, `ToPngDataUrlAsync()`, `ResolveExportSize()`, `Matrix`, `Layout`, `Version`, `SymbolSize`, `MaskPattern`, `HasError`/`ErrorMessage`. The encoder (`ElQRCodeGrowEncoder`) is implemented entirely inside this library — no third-party QR package, no canvas API — so it works in server-side rendering, unit tests and offline environments. Documented in `docs/qrcode-alignment.md` and covered by `ElQRCodeGrowEncoderTests.cs`, `QRCodeGrowTestDecoder.cs` (an independent standards-compliant decoder used for round-trip proof) and `ElQRCodeGrowTests.cs`. Verified against two independent third-party decoders (zxing-cpp and OpenCV): 21/21 fixture symbols decode correctly.
- Added `ElDialogGrow`, an Element Plus-style modal dialog component (self-rendered, zero Radzen dependency) that mirrors `ElDialog` as a parallel implementation reusing the existing `el-dialog` global styles. Supports `ModelValue`/`Visible` two-way binding, `Title`, `Width` (default `50%`), `Fullscreen`, `Top` (default `15vh`), `Modal`/`ModalClass` overlay, `CloseOnClickModal`, `CloseOnPressEscape`, `ShowClose`, `Center`, `AlignCenter` (vertical centering), `Draggable` (pure-Blazor `transform` drag, no JS), `DestroyOnClose`, `LockScroll` (via `elementLockBodyScroll` in `dom.js`), `AppendToBody`, `ZIndex`, and `BeforeClose` / `OnBeforeClose` cancellation. Slots: `Header`, `ChildContent`, `Footer`; events: `OnOpen`, `OnOpened`, `OnClose`, `OnClosed`, `OnVisibleChange`. Methods `ShowAsync()` / `CloseAsync()` for programmatic control, with open/close side-effects (scroll lock and events) shared between parameter-driven and programmatic paths so both fire consistently. Covered by `test/Element.ComponentTests/ElDialogGrowTests.cs` (24 cases).
- Added `ElDropDownTableGrow`, an Element Plus-style dropdown table picker (self-rendered, zero Radzen dependency) that wraps the existing `ElTable` / `ElTableColumn` inside a popup panel — the Element Plus self-rendered counterpart of Radzen's `RadzenDropDownDataGrid` (where Radzen embeds a full `RadzenDataGrid`, this component embeds `ElTable` so paging / column sorting / multi-select are handled by `ElTable` itself). Supports a data grid for row selection, a top search box (text filter across row properties or a restricted `SearchProperties` set), single-select (row click) and multi-select (checkbox column, `Type="selection"`), chips for multi-selected items, clearable, disabled, custom `Width` or auto-expand-by-columns. Parameters: `Items`, `ValueField`, `TextField`, `Value`/`Values`, `Multiple`, `Placeholder`, `Clearable`, `AllowSearch`, `SearchProperties`, `PageSize`, `ShowPagination`, `Stripe`, `Border`, `Height`, `Width`, `EmptyText`, `AutoCloseOnSelect`, `ChildContent` (column definitions), `OnChange`, `OnClear`; methods `ShowAsync()` / `CloseAsync()` / `ToggleAsync()`. Covered by `test/Element.ComponentTests/ElDropDownTableGrowTests.cs` (11 cases).
- Added `ElChartGrow`, an Element Plus-style chart component (self-rendered, zero Radzen dependency) — the Element Plus self-rendered counterpart of Radzen's `RadzenChart`. All geometry and SVG markup are computed in pure C# (`ElChartGrowEngine`, no JS, no third-party chart library) so it works in server-side rendering, unit tests and offline packaging; on-screen rendering and `RenderSvg()` export share the same layout source of truth. Child components `ElChartSeries` (declares one data series) and `ElChartAxis` (declares a category/value axis) register into the parent. Supported chart types via `ElChartType`: `Line`, `Area`, `Column`, `Bar`, `Scatter`, `Bubble`, `Pie`, `Donut`, `Radar`, `StackedColumn`, `StackedBar`, `StackedArea`, `StackedLine`. Features: title, legend (`LegendPosition` Top/Bottom/Left/Right), category + value axes with nice-number auto ticks and optional `Min`/`Max`/`Step`/`Formatter`, a numeric X axis for `Scatter`/`Bubble` (enabled by `XValueField`), grid lines, per-series color (palette or explicit `Color`), line style (Solid/Dashed/Dotted), markers (Circle/Square/Diamond), data labels, radar chart with concentric polygon grid + spokes + axis/value labels and multi-series polygon overlay, and native SVG `<title>` hover tooltips. Parameters: `Width` (600), `Height` (360), `Title`, `ShowLegend`, `LegendPosition`, `ShowCategoryAxis`, `ShowValueAxis`, `ShowGridLines`, `Palette`, `EmptyText`, `ChildContent`; series params `Items`, `ChartType`, `CategoryField`, `ValueField`, `XValueField` (numeric X for scatter/bubble), `SizeField` (bubble radius), `Title`, `Color`, `ShowDataLabels`, `LineStyle`, `MarkerType`. Members: `BuildModel()`, `ComputeLayout()` (structured geometry for test assertions), `GetSvg()`. Covered by `test/Element.ComponentTests/ElChartGrowTests.cs` (18 cases).
- Added `ElMarkdownGrow`, an Element Plus-style Markdown renderer (self-rendered, zero Radzen dependency, and no Markdig or any other third-party parser) — the Element Plus self-rendered counterpart of Radzen's `RadzenMarkdown`. Parsing (`ElMarkdownGrowParser`), HTML sanitising (`ElMarkdownGrowSanitizer`) and rendering (`ElMarkdownGrowHtmlRenderer`) are all hand-written pure C# with **no JavaScript interop**, so it works in server-side rendering, unit tests and offline packaging; the on-screen markup and `RenderHtml()` share one output. Block syntax: ATX/setext headings, fenced (``` / ~~~, with info string → `language-*` class) and indented code blocks, blockquotes (nested), ordered/unordered/nested lists (tight & loose), GFM task lists, GFM pipe tables with per-column alignment, thematic breaks, HTML blocks and paragraphs, plus link reference definitions. Inline syntax: emphasis/strong/strikethrough (correct intraword `_` handling), code spans (backtick runs), escape sequences, inline/reference/shortcut links, images, `<https://…>` and `<e@mail>` autolinks, bare-URL autolinking, hard breaks (two spaces or trailing `\`) and emoji shortcodes. Security: HTML pass-through is opt-in-safe — tag whitelist, attribute whitelist, dangerous URI schemes (`javascript:`/`vbscript:`/`data:text/html`, incl. entity-obfuscated and whitespace-padded forms), `style` expression filtering, event-handler attributes always dropped, and dangerous containers (`script`/`style`/`iframe`/`object`/… ) removed together with their content. Headings can get automatic anchors (`AutoLinkHeadingDepth`) using the reference implementation's slug algorithm. Input via `Text` **or** `ChildContent` — when Markdown is written directly inside the tag, embedded HTML elements/Blazor components are preserved in place through outlet segments instead of being parsed as text. Parameters: `Text`, `ChildContent`, `AllowHtml` (true), `AllowedHtmlTags`, `AllowedHtmlAttributes`, `AutoLinkHeadingDepth`, `SoftBreakAsHard`, `EnableEmoji`, `EnableAutoLinkUrl`, `EmptyText`, `HideEmptyText`. Members: `Document`, `BuildOptions()`, `RenderHtml()`, `InnerMarkup`, `Segments`, `HasOutlets`, `TryGetOutlet()`. Browser compatibility: the emitted HTML uses only elements/attributes available since Chromium 75 and the shipped CSS only uses features available since Chrome 49 (no `:has()`/`:is()`/`:where()`/logical properties/`gap`/`clamp()`/CSS nesting/`accent-color`), enforced by guard tests that scan the stylesheet. Documented in `docs/markdown-alignment.md` and covered by `test/Element.ComponentTests/ElMarkdownGrowTests.cs`.
- Added `ElHeatmapGrow`, an Element Plus-style heatmap component (self-rendered, zero Radzen dependency) — the Element Plus self-rendered counterpart of Radzen's `RadzenHeatmap` / `RadzenHeatmapSeries`. All geometry, colour mapping and SVG markup are computed in pure C# (`ElHeatmapGrowEngine` + `ElHeatmapGrowColorScale`, no JS, no third-party chart library), so it works in server-side rendering, unit tests and offline packaging; on-screen rendering and `GetSvg()` export share the same layout source of truth. Two modes are supported: a **category grid** (`NumericAxes` false, the default) that evenly tiles the plot area like `RadzenHeatmap`, and a **numeric grid** (`NumericAxes` true) that places every cell by its axis-unit coordinate like `RadzenHeatmapSeries`, with the cell size inferred from the smallest gap between unique coordinates (`CellWidth` / `CellHeight` override it) plus half-cell expansion so the outermost cells align exactly with the plot edges. Colour is either a two-endpoint linear interpolation (`MinColor` → `MaxColor`, defaults `#f0f0f0` → `#1a9641`) or a list of `ElHeatmapColorRange` buckets (`Min` / `Max` / `Color`, ±∞ supported) that take precedence and fall back to `Fill` (transparent when unset). Features: title, X/Y axis titles, category or numeric tick labels, in-cell value labels (drawn only when the cell exceeds 20×14 px, with automatic light text on dark fills), a right-side colour-scale legend (one entry per `ColorRanges` bucket, otherwise a 5-step gradient), cell stroke, and native SVG `<title>` hover tooltips. Parameters: `Data`, `XProperty`, `YProperty`, `ValueProperty`, `Width` (600), `Height` (360), `Title`, `NumericAxes`, `CellWidth`, `CellHeight`, `CellPadding` (2), `MinColor`, `MaxColor`, `ColorRanges`, `Fill`, `Stroke`, `StrokeWidth`, `ShowValues`, `FormatString`, `ShowXLabels`, `ShowYLabels`, `ShowColorScale`, `XTitle`, `YTitle`, `EmptyText`; members `BuildModel()`, `ResolveCells()`, `ComputeLayout()` (structured geometry for test assertions), `GetSvg()`. Covered by `test/Element.ComponentTests/ElHeatmapGrowTests.cs` (33 cases).
- Added `ElFormFieldGrow`, an Element Plus-style form-field container (self-rendered, zero Radzen dependency, **zero JS**) — the Element Plus self-rendered counterpart of Radzen's `RadzenFormField`. Wraps any input control and adds a (optionally floating) label, leading/trailing slots, helper text and validation feedback. Unlike the reference implementation it needs **no JS interop at all**: Radzen's `Radzen.createFormField` attaches `focusin`/`focusout` listeners to toggle an `rz-state-focused` class, whereas this component derives its whole focus state from the CSS `:focus-within` pseudo-class, so it also works in static SSR, prerendering and unit tests without `JSInterop`. Floats are driven by three mutually exclusive modifier classes (`el-form-field--float-auto` / `--float-always` / `--float-never`, via `ElFormFieldFloat`) so the "never float" case simply emits no float rules and no specificity battle (and no `:has()`, no `!important`) is required. Parameters: `ChildContent`, `Start`, `End`, `Helper`, `TextTemplate`, `Text`, `Label` (alias), `Component` (`for` target), `Required`, `AllowFloatingLabel`, `Float`, `HasValue` (explicit "has value" flag — the reliable cross-browser path for wrapped controls), `Variant` (`ElFormFieldVariant`: Outlined/Filled/Flat/Text), `Size` (`ElFormFieldSize`: Small/Default/Large), `Inline`, `Disabled`, `ReadOnly`, `Invalid`, `Width` (bare numbers get `px`), `KeepControlChrome`, plus inherited `Cls`/`Style`/`Attributes`. `HelperText` / `ErrorText` cover the plain-text case while `Helper` takes precedence; `ErrorText` only renders (in red, with `role="alert"`) when `Invalid` is true. Inner Element Plus inputs are "de-chromed" by overriding their CSS custom properties from within the field scope (no `!important`) unless `KeepControlChrome` is set. `IElFormFieldContext` is cascaded to children (mirroring the reference's `IFormFieldContext`) so nested controls can report their disabled state back via `DisabledChanged`. Members: `BuildCssClass()`, `BuildStyle()`, `BuildHelperClass()`, `NormalizeLength()`, `EffectiveFloat`, `IsDisabled`, `HelperId`. **Browser compatibility is enforced by tests**: the stylesheet section deliberately avoids `:has()` (Chrome 105+), `:is()`/`:where()` and multi-argument `:not()` (Chrome 88+), CSS nesting (Chrome 112+), logical properties (`inset-block-*`/`margin-block`/`padding-inline`, Chrome 87+), flex `gap` (Chrome 84+) and `clamp()` (Chrome 79+), using physical properties and `margin` spacing instead; four guard tests in `ElFormFieldGrowTests.cs` scan the CSS section (comments stripped) and the component sources to keep it that way. Documented in `docs/form-field-alignment.md` and covered by `test/Element.ComponentTests/ElFormFieldGrowTests.cs`.
- Added shared `elementSvgRasterize` / `elementSvgExportBackground` / `elementSvgDownloadPng` / `elementSvgToPngDataUrl` helpers to `wwwroot/js/dom.js`, used by both `ElBarcodeGrow` and `ElQRCodeGrow` for SVG→PNG export.
- Added `ElBarcodeGrow`, an Element Plus-style 1D barcode component that renders inline SVG. Supports 14 symbologies (Code 128, Code 39, EAN-13, EAN-8, UPC-A, ITF, POSTNET, RM4SCC, Codabar, Pharmacode, ISBN, ISSN, MSI, Telepen) through `ElBarcodeGrowType`, with parameters `Value`, `Type`, `Width`, `Height`, `Color`, `Background`, `ErrorColor`, `QuietZone`, `BarHeight`, `ShowValue`, `ShowChecksum`, `ShowError`, `FontSize`, `TextMarginTop`, `ValueStyle` and `ErrorText`; members `ToSvg()` (standalone SVG string, no JS required), `DownloadPngAsync()`, `Layout`, `HasError` / `ErrorMessage`, and the static `HasChecksum`. The encoder (`ElBarcodeGrowEncoder`) is implemented entirely inside this library — no third-party barcode package, no canvas API — so it works in server-side rendering, unit tests and offline environments. Documented in `docs/barcode-alignment.md` and covered by `test/Element.ComponentTests/ElBarcodeGrowTests.cs`.
- Added `elementBarcodeDownloadPng` helper to `wwwroot/js/dom.js` for rasterizing a barcode SVG to PNG.
- Added `EnterNavigationMode` (`TableEnterNavigationMode`) for editable `ElTable`: `NextCell` (default, move to the next editable cell in the same row) or `SameColumnNextRow` (move to the same column of the next row, `Shift+Enter` for the previous row) for column-by-column data entry. Documented in `docs/table-enter-cell-navigation.md` and covered by 8 new cases in `test/Element.ComponentTests/ElTableEnterCellNavigationTests.cs`.
- Added Enter-key cell navigation for editable `ElTable`: in edit mode `Enter` moves to the next editable cell (and focuses it), `Shift+Enter` moves back, with `EnterMovesToNextCell` / `EnterWrapsToNextRow` on the table and a per-column `EnterMovesToNextCell` opt-out. New members: `EditingRow`, `ActiveEditingColumn`, `ActiveEditingCellIndex`, `EditableColumns`, `MoveEditingCellAsync`. Documented in `docs/table-enter-cell-navigation.md` and covered by `test/Element.ComponentTests/ElTableEnterCellNavigationTests.cs`.
- Added `elementTableFocusCell` and `elementTableColumnIndexAt` helpers to `wwwroot/js/dom.js`, plus the `.el-table__cell--editing` highlight style in `wwwroot/css/fix.css`.
- Added dynamic column support for `ElTable`: `Columns` snapshot, `AddColumn` (by `TableHeader` / property name / eval delegate / template), `AddColumns`, `RemoveColumn`, `ClearColumns`, and `ReloadColumns`. New columns take effect immediately on an already populated table. Covered by `test/Element.ComponentTests/ElTableDynamicColumnTests.cs` and documented in `docs/table-dynamic-columns.md`.
- Added click debounce for `ElButton` (`Debounce` / `DebounceLeading` parameters) with trailing debounce and leading throttle modes, plus cancellation on dispose. Covered by `test/Element.ComponentTests/ElButtonDebounceTests.cs` and documented in `docs/button-debounce.md`.
- Added release stabilization checklist in `docs/release-checklist.md`.
- Added NuGet package readme files for `Element`, `Element.Markdown`, and `Element.X`.
- Added Source Link and `.snupkg` symbol package settings for packable projects.
- Added independent `Element.X` package for optional AI and conversation components.
- Added `ElementXStreamService`, `ElementXRequestService`, and `ElementXRecordService` with offline stream, cancellation, browser recording fallback, and service tests.
- Added `ElMenuItemGroup` (aligned with Element Plus `el-menu-item-group`): renders as `li.el-menu-item-group` with an `el-menu-item-group__title` header (`Title` / `TitleTemplate`) and a plain `ul` body, inheriting the cascading `--el-menu-level` so grouped items indent one level deeper; the Nav demo now mirrors the official example (子菜单 + 分组 + 折叠 + 手风琴 + `Open`/`Close` API).
- Reworked `ElMenu` / `ElSubMenu` / `ElMenuItem` to the Element Plus contract: renamed `el-submenu` DOM classes to `el-sub-menu`, `DefaultOpeneds` / `UniqueOpened` / `Collapse` / `DefaultActive` per EP semantics (`watch(collapse)` closes all sub-menus, `UniqueOpened` keeps only ancestors of the active path), `OnSelect` / `OnOpen` / `OnClose` events with `MenuSelectEventArgs` / `MenuOpenEventArgs` (`index` + `indexPath`), `OpenAsync(index)` / `CloseAsync(index)` exposed methods, `--el-menu-level` per-level indentation, horizontal active underline, popup sub-menu dark effect, and theme-chalk token map (`--el-menu-item-height` / `sub-item-height` / `horizontal-height` / level).
- Added the **date range (daterange) double panel** to `ElDatePicker` / `ElDatePickerPanel`, aligned with Element Plus. `ElDatePickerPanel` gains `RangeRole` (`DatePickerRangeRole.None` / `Start` / `End`), `RangeStart` / `RangeEnd` (highlight only) and `Locale`; a range panel renders just its own side of the header — `« ‹  2026 年 9 月` on the left, `2026 年 10 月  › »` on the right — with the label absolutely centred across the whole panel and the arrow groups anchored to the outer edges, and it draws EP's `in-range` / `start-date` / `end-date` highlight. `ElDatePicker` renders the two halves as `.el-date-range-picker__content.is-left` / `.is-right` sharing one display month (`left = M`, `right = M + 1`) and re-anchors the pair on any paging, so `›` on the right half advances **both**. The calendar is now localizable end to end — `PickerLocale` gained the week headers (`日…六` / `Sun…Sat`), the month names (`一月` / `January`), the year / month labels (`2026 年` / `2026 September`) and `FormatYearMonth`, all driven by `ElConfigProvider Locale` (Simplified Chinese by default). New demo section **ElDatePicker 日期范围（双面板并排）** on `/form2`: a Chinese example pre-filled with `2026-09-12 ~ 2026-09-25` beside an `en-US` one, matching EP's own daterange screenshot. See `docs/date-picker-panel-alignment.md` and `docs/datetime-picker-alignment.md`.

- Added a **`Compact` mode** to the date pickers. `ElDatePicker` and `ElDateTimePicker` gain a `Compact` parameter (default `false` = the existing normal size) that, when set to `true`, narrows the popup — the `Width` is derived from `(IsRange && !SinglePanel, Compact)` so a single panel drops **322 → 256px** and a range panel **646 → 514px** — and is forwarded to the shared `ElDatePickerPanel` via a new `Compact` parameter that toggles the `is-compact` CSS class on `.el-picker-panel` (the same flag also drives `ElDatePicker`'s `DropDownPanelClass`). `wwwroot/css/fix.css` adds a `.el-date-picker-panel.is-compact …` ruleset that shrinks the content area **292 → 224px** (= 7 × 32), the date cells **41.7 → 32px** tall (24 → 20px round day cells, tighter padding), the header margins (12 → 8px, label 16 → 14px, icon-btn margin-top 8 → 6px), the month table (row 48 → 34px, 36 → 26px pills) and the year table (padding 20 → 12px, 32 → 26px cells); the range panel still flex-distributes so `.is-compact` only re-widths a `width:auto` content area. Net effect: the same calendar at ~78% of the footprint. The `/form2` demo gains a **日期面板紧凑模式（Compact）** section with compact `ElDatePicker` / `ElDateTimePicker` / daterange examples, and `demo/probe-compact-panel.cjs` (30 assertions: popper width, content width, cell size, range two-sided geometry, console-error check) passes, alongside the full unit suite (1575/1575).

- Added a working **index (序号) column** to `ElTable`. `ElTableColumn.Type="index"` and `IndexMethod` were previously declared as Element Plus compatibility parameters but never consumed by the renderer, so `<ElTableColumn Type="index" />` produced a blank cell. The cell loop in `ElTable.razor` now renders the row number: by default the **page-local** index (1..N per page, resetting on page change — matching EP's `type="index"`), or, when `IndexMethod` is supplied, the value it returns for the page-local 0-based row index (e.g. `i => i + (CurrentPage-1)*PageSize + 1` for a continuous number across pages). `ElTable.AddColumn` no longer demands a `Property` for `Type="index"` / `Type="selection"` columns. Covered by `ElTableIndexColumnTests` (3 cases: page-local numbers, reset on page change, continuous via `IndexMethod`) and a new `/data` demo section.

- Made two `ElInput` parameters that were declared but never consumed actually work. **`ShowClearIcon`** (documented as "always show the clear icon when clearable") had no read site at all, so the icon only ever appeared on hover/focus; the clear-icon condition now honours it — `Clearable="true" ShowClearIcon="true"` pins the icon visible, while the default stays focus/hover-only. **`Autosize`** was threaded through `ControlConfigs.InputAttribute` and `InputRender` yet unimplemented for textareas: a new `dom.js` helper `elementTextareaAutoHeight(el, minRows, maxRows)` measures the height the content needs (restoring the inline height after measuring), and `ElInput` writes the result into the textarea's own `style` — keeping Blazor the single source of truth for the style attribute instead of letting JS mutate it and lose it on the next diff. `Rows` acts as the minimum height and the height springs back down when text is deleted. No-JS hosts (bUnit) keep the previous `rows` behaviour. Verified with `demo/probe-input-checks.cjs` (9 assertions: clear icon visible without hover, absent by default, appearing on hover, clearing on click; textarea 75 → 138px on growth, 60px unchanged without `Autosize`, shrinking back to 75px and never below `Rows`), and a new **ElInput 属性自检** demo section on `/form`.

- Fixed three parameters of `ElButton` that were declared but did not do what they say. **`Dark`** was a no-op: the style builder emitted `--el-button-text-color:{(Dark ? "var(--el-color-white)" : "var(--el-color-white)")}` — both branches identical — so a custom `Color` always rendered as a solid fill with white text regardless of `Dark`. The builder now splits into two rules following Element Plus semantics: `Dark="true"` keeps the solid colour fill with white text, while the default (`Dark=false`) uses a light `color-mix(in srgb, <Color> 12%, var(--el-fill-color-blank))` fill with `<Color>` as the text colour and only goes solid on hover. Each custom property is written twice — a plain fallback first, then the `color-mix()` version — so old cores that cannot parse `color-mix` silently keep the fallback instead of losing the background. **`Href`** was silently dropped unless the caller also set `Tag="a"`; `Tag` now defaults to `null` and resolves to `<a>` when `Href` is given (and `<button>` otherwise), which also brings the existing `role` / `tabindex` / `keydown` / `target` / `rel` branch into play. **`Tabindex`** is now emitted for native `<button>` tags too (it was previously only written on non-native tags). Verified with `demo/probe-button-checks.cjs` (11 assertions: computed colour pair differing between `Dark` and default, `A` tag with `href` / `target` / `role`, `tabindex=3` on a native button, plain buttons still `BUTTON`) and an **ElButton 属性自检** demo section on `/form`.

- Audited `ElIcon` and fixed the two real defects it had. **The `Icon` enum had no `None` member**, so its zero value was `PlatformEleme` and `<ElIcon />` — or any component leaving an `Icon` parameter unset — silently rendered the eleme logo; the enum now starts with `None = 0` (`PlatformEleme` and everything after it shift by one) and `IconToCssName` returns an empty name for it, so nothing is emitted. **59 of the 160 enum members map to glyphs the bundled `element-icons` font does not contain** (the font is the Element UI set of 280, while the enum names follow Element Plus — `UserFilled`, `Avatar`, `Loading2`, `*Filled`, `*Bold`, `Notebook`, `Clock`, `Calendar`, `DArrowUp/Down`, …); those names produced a class with no `::before` content, i.e. an invisible 1em box. A new `FontAliases` map now redirects them to the closest existing glyph (`user-filled → user-solid`, `*-filled → *-solid` / base name, `loading2 → loading`, `d-arrow-up → top`, `clock → time`, `calendar → date`, `notebook(s) → notebook-1/-2`, `shopping-cart → shopping-cart-2`, `keyboard → key`, `no-data → sold-out`, `expand → full-screen`, …), and it is applied to both the enum path and the string `Name` path. `fix.css` also gained `.el-icon > svg, .el-icon svg { height:1em; width:1em; fill:currentColor }` so an SVG placed in the default slot — which `ChildContent` explicitly advertises — follows `Size` and inherits `Color` instead of rendering at its intrinsic size. `Size` / `Color` / `Spin` / `OnClick` / `AriaLabel` / `Title` were already consumed correctly. Verified with `demo/probe-icon-checks.cjs` (15 assertions: default `<ElIcon/>` carrying no `el-icon-*` class and no `::before` content, `Icon.Close` producing a real glyph, `Size="28"` → 28px, `Color` → `rgb(103,194,58)`, `Spin` → `is-loading` + `rotating` animation, `Name="el-icon-user-filled"` landing on `el-icon-user-solid` with a visible glyph, SVG slot 32×32px, click counter incrementing) and an **ElIcon 属性自检** demo section on `/form`.

- Audited `ElLink` and fixed three defects. **The default `Type` rendered in the primary colour**: `LinkClass` deliberately skips the modifier for `LinkType.Default` (there is a unit test locking "no redundant `el-link--default`"), but `theme.css`'s base rule is `color: var(--el-link-text-color, var(--el-color-primary))`, so a plain `<ElLink>` came out blue instead of Element Plus's `--el-text-color-regular`. `fix.css` now sets `--el-link-text-color` on `.el-link:not([class*="el-link--"])` — scoped so it cannot override `primary` / `success` / `danger` / `warning` / `info`. **Icon–text spacing used `gap: 4px`**, which old cores (the project targets Win7 / Chromium 75) do not support, making the icon touch the text; replaced with Element Plus's own `.el-link [class*="el-icon-"] + span { margin-left: 5px }` (`gap: 0` keeps the flex behaviour honest). **`Icon` advertised accepting a bare name** ("图标 CSS 类名（如 "el-icon-edit"）或纯名称") but the markup emitted `Icon` verbatim, so `Icon="close"` produced `<i class="close">` — an unstyled empty element; a new `EffectiveIconClass` normalises single-word values to `el-icon-<name>` while leaving hyphenated / already-prefixed values such as `"fa fa-search"` untouched. Verified with `demo/probe-link-checks.cjs` (20 assertions: default link `rgb(96,98,102)` vs primary `rgb(64,158,255)` vs danger `rgb(245,108,108)`, no `el-link--` class emitted for default, disabled → `is-disabled` + `aria-disabled` + `tabindex=-1` + `href` dropped + `OnClick` suppressed, `Underline=Always` drawing an `::after` border while `Never` sets `content:none`, `Icon="close"` → `el-icon-close` with a real glyph, `span` `margin-left:5px` with `gap:0`, `target=_blank` picking up `rel="noreferrer noopener"`) and an **ElLink 属性自检** demo section on `/form`. Two `ElLinkTests` cases were updated for the new icon contract plus two added (third-party class preserved, no double prefix).

- Audited `ElButtonGroup` end to end and found **no component defect**: all four parameters (`ChildContent` / `Size` / `Type` / `Direction`) are consumed, and `theme.css` covers `el-button-group`, `.is-vertical`, the `> .el-button + .el-button` seam and the first/last-child corner radii. What looked like a bug (switching `Direction` to `vertical` did not add `is-vertical`) turned out to be **usage**: in Razor, `Attr="foo"` on a **string** parameter is a literal — it must be `Attr="@foo"` to bind a field — whereas `Size="groupSize"` on an enum is evaluated as an expression, which is why only the string parameter appeared broken. Note this before suspecting a re-render problem: if the bound value visibly updates (a text indicator next to it changes) but the element's class does not, the caller's syntax is the first place to look. Added `demo/probe-button-group-checks.cjs` (15 assertions: Size cascade 32 → 40 → 24px, `Type` cascade, `Direction` flipping to `column` with `is-vertical`, a child's own `Size` taking priority over the group's, a static group combining `Size=Large` + `Type=Danger`, first/last-child right-angle corners, console-error check) and an **ElButtonGroup 级联自检** section on `/form`. The component itself is unchanged.

- Audited `ElText` and fixed the defects it had. **`Tag` was not accepted as a render parameter**: `ElText.razor.cs` only reported `ChildContent` / `Type` / `Size` / `Cls` / `Style` as parameters, so `<ElText Tag="p">` / `<ElText Tag="strong">` were silently ignored and the element always rendered as a `<span>`; `Tag` is now a real `[Parameter]` and the effective tag resolves to it (default `span`). **The base CSS rule was missing**, so the component relied on inherited colour/size instead of Element Plus's `el-text` contract — `theme.css` now defines `.el-text` (`color: var(--el-text-color-regular)`, `font-size: var(--el-font-size-base)`, `line-height: var(--el-font-line-height-primary)`, `margin:0`, `overflow-wrap: break-word`, plus a `<p>`/`<div>` reset to `padding:0;margin:0` so the default tag is not shifted), and the `Type` / `Size` modifier classes now map exactly to EP's `--primary/-success/-info/-warning/-danger` and `--large/-default/-small` (`Default` and `Normal` intentionally emit no modifier, matching EP). **`Truncated` and `LineClamp` were never wired to CSS**: `Truncated` now emits `is-truncated` with `display:inline-block;overflow:hidden;text-overflow:ellipsis;white-space:nowrap` (and the content is actually clipped), while `LineClamp` emits `is-line-clamp` with the EP `-webkit-box / -webkit-box-orient:vertical / -webkit-line-clamp: var(--el-text-line-clamp)` contract and the `--el-text-line-clamp` variable is written from the parameter; the two can now be combined (`is-truncated is-line-clamp` with `white-space:normal`), which EP does not support. `Cls` / `Style` / `Type` / `Size` are all consumed and merge correctly. Verified with `demo/probe-eltext.cjs` (31 assertions: `el-text` class + `Type=Danger`/`Size=Large|Small` modifiers, default colour `rgb(96,98,102)`, `Truncated` clipping + `ellipsis`, `Tag=p|strong` rendering the right element, `LineClamp=2` mapping to `-webkit-box` layout clipped to 2 lines, combined mode, custom-class merge, `padding/margin:0`, no console errors) plus 3 `ElTextTests` cases (full suite 1585/1585), and an **ElText 属性自检** demo section on `/basic`.

### Changed

- Fixed `ElMenu` ignoring the collapsed width when the caller also passes an inline width. Setting `Collapse="true"` did add `el-menu--collapse` (whose `.el-menu--collapse { width: calc(var(--el-menu-icon-width) + var(--el-menu-base-level-padding) * 2) }` rule resolves to EP's 64px), but an inline `Style="width:220px"` — the pattern used by the `/nav` demo — has higher priority than any class rule, so the sidebar kept its full width after collapsing. `ElMenu`'s style builder now appends `width: calc(var(--el-menu-icon-width) + var(--el-menu-base-level-padding) * 2)` **after** `Style` when `Collapse` is true, so the collapsed width wins while the menu keeps honouring a custom width when expanded. Verified with `demo/probe-nav-collapse.cjs` (7 assertions: 220 → 64 → 220, `el-menu--collapse` toggling, sub-menu title width 0 while collapsed).

- Fixed `ElImage`'s preview viewer (`el-image-viewer`) so it actually sits on top and supports mouse zoom / pan. The wrapper already used `position: fixed` but Element Plus' base stylesheet gives it no `z-index`, and later siblings such as `.el-step` are `position: relative` with `z-index: auto` — same positioned layer, later in the DOM — so the following sections painted **over** the mask (the "下面的界面还露着" symptom). `wwwroot/css/fix.css` now sets `.el-image-viewer__wrapper { z-index: 2400 }` (above the 2000/2200 layers already used in that file) and adds `overflow: hidden`, `cursor: grab` / `.is-dragging { cursor: grabbing }`, `-webkit-user-drag: none` and a `transform` transition that is switched off while dragging. `ElImage` itself gains the viewer interactions: **wheel to zoom** (`OnPreviewWheelAsync`, clamped to 0.2x–7x in 0.15 steps; zooming back to ≤1x recentres), **drag to pan** once zoomed (`OnPreviewPointerDown/Move/UpAsync`, with `is-dragging` driving the cursor), and **double-click to toggle 1x / 2x**. The zoom + offset are applied through a new `PreviewImageStyle` (`translate3d(...) scale(...)`), reset on open, close and prev/next. Covered by `demo/probe-image-viewer.cjs` (9 assertions incl. a hit-test proving the topmost element over the steps section is the viewer mask).

- Made `ElTable`'s existing `Border` / `IsBordered` parameter actually work. `ElTable.razor` already emitted the `el-table--border` class when `Border="true"`, but `wwwroot/css/fix.css` contained no rule for it, so `Border="true"` rendered identically to the default. `fix.css` now adds the missing ruleset: the table gets a 1px outer frame (`--el-table-border-color`, falling back to `--el-border-color-lighter` / `#ebeef5`), every `th.el-table__cell` / `td.el-table__cell` gets a 1px right divider (last column excluded so it does not double up with the frame), the header row gets a same-colour bottom line, and the last body row's bottom border is dropped in favour of the frame. Default behaviour is unchanged (`Border=false` = horizontal row lines only, no column dividers). New `/data` demo section **ElTable 列分隔线（Border 属性，默认不显示）** shows a bordered table with an index column.

- Reworked `ElAccountTableGrow` into the accounting-voucher shape used on real 记账凭证: the `日期` (date), `凭证号` (voucher no.) and `操作` (action) columns are gone, leaving 摘要 → 会计科目 → 借方金额(亿…分) → 贷方金额(亿…分); every amount cell is now only as wide as its own header label (`亿` / `千` / `百` …) so the digits sit under the label instead of floating in a 90px box, and 摘要 / 会计科目 are widened and independently controllable through the new `AbstractWidth` (default `220px`) and `AccountWidth` (default `200px`) parameters. The trailing 合计 row is now a single non-editable row (never editable, even in edit mode) whose 借方 / 贷方 totals are computed automatically from `Rows`; it prints `合　计` normally and switches to `合计:柒万肆仟陆佰捌拾元整` only when the two totals are **equal and non-zero**, which also retires the old stand-alone uppercase-amount row. A new `ReadOnly` parameter (default `false`) switches the whole grid between edit and read-only — in read-only mode the inputs are `readonly`, the hover ⊕/⊗ row buttons stay hidden and Enter no longer navigates. Enter navigation is now scoped to the amount cells only: Enter walks 借方 → 贷方 within a row, and Enter in the **last** amount cell jumps to the 摘要 column of the next row and focuses it (auto-advance, no mouse); nothing happens on the last row, on any non-Enter key, or whenever `ReadOnly` is `true`. See `docs/account-table-alignment.md`.

- Updated the Element-Blazor 2.14 package metadata for NuGet publishing.
- Updated package license metadata to use the MIT license expression.
- Updated package documentation file output paths to the `net10.0` target framework.
- Replaced the unreadable changelog contents with a release-ready changelog baseline.
- Moved X components and `x.css` out of the core `Element` package.

### Added

- **`ElChartGrow` now covers the full `RadzenChart` series surface** (26 of 32 `Radzen*Series` components) plus the three structural capabilities it was missing. Compared against `E:\电子病历\radzen-blazor-master\Radzen.Blazor` (`RadzenChart.razor` is only a 91-line host; chart kinds live in the 32 `Radzen*Series.razor` children), the gap list was 19 missing series + 3 structural features. All of them are now implemented:
  - **19 new `ElChartType` values** — `FullStackedColumn` / `FullStackedBar` / `FullStackedArea` / `FullStackedLine` (per-category normalisation to 100, axis fixed at 0–100), `RangeColumn` / `RangeBar` / `RangeArea` (low↔high band, vertical / horizontal / area), `Candlestick` / `Ohlc` / `HighLow` (financial family, `OpenField`/`HighField`/`LowField`/`CloseField`), `BoxPlot` (`MinField`/`Q1Field`/`MedianField`/`Q3Field`/`MaxField` with whiskers, caps and median line), `Waterfall` / `HorizontalWaterfall` (running cumulative + `IsTotalField` subtotal rows + connector segments), `Bullet` (`ValueField` vs `TargetField`, three-zone background + target tick), `Funnel` / `Pyramid` (centred trapezoid bands, descending vs ascending), `Heatmap` / `Contour` (`CategoryField` = X column, `GroupField` = Y row, `ValueField` = value; independent layout with row/column labels and a colour-scale legend), `SpiderColumn` (columns radiating from the radar centre).
  - A generic five-slot value model (`ValueField2`…`ValueField5`) with per-type semantic aliases, so no caller has to remember slot numbers.
  - `ElChartType.RisingColor` / `FallingColor` default to the Chinese convention (**up = red `#f56c6c`, down = green `#67c23a`**) and can be overridden per series.
- **Trend lines and reference elements** (structural gap 1): `ElChartSeries.TrendLine` supports `Linear` (least squares), `MovingAverage` (`TrendLinePeriod`), `Polynomial` (quadratic fit), `Mean`, `Median` and `Mode`, with `TrendLineStyle` / `TrendLineColor`. Chart-level `ReferenceLines` (threshold / target lines on either the value or the category axis), `ReferenceBands` (interval shading) and `Annotations` (marker + text at a category or numeric-X coordinate) are rendered as `el-chart-grow__trendline` / `__refline` / `__refband` / `__annotation`.
- **Named secondary value axes** (structural gap 2): `ElChartAxis.Name` matches `ElChartSeries.ValueAxisName`; each matched series is scaled against its own axis, which is drawn on the right side (`__axis--secondary` / `__tick--secondary`). The layout exposes every scale through `ElChartLayout.ValueScales`. This was previously impossible — `ElChartAxis` had no `Name` and `ElChartSeries` had no `ValueAxisName`.
- **Interactive tooltip and hit testing** (structural gap 3): `ElChartGrow.ShowTooltip` (default `true`) collects an `ElChartHit` per data point, bar, slice, cell or spoke; `ElChartGrowEngine.FindNearest` resolves the closest target under the cursor; the component renders a crosshair + highlight marker overlay and an HTML tooltip (series title, category, formatted value). `ShowTooltipCrosshair` and `TooltipTolerance` tune it. Turning `ShowTooltip` off skips hit collection entirely (`ElChartModel.CollectHits`).
- `ElChartSeries.ValueFormat` formats values for data labels and tooltips (e.g. `N2`, `0.0%`).
- 25 new tests in `ElChartGrowAdvancedTests` cover every new series type (geometry + markup), the trend-line family, reference lines/bands, annotations, the secondary axis, `FindNearest`, and the hover show/clear cycle. Full suite: **1627/1627**.
- The `/grow` demo page now exercises the whole surface in one place: 21 live charts (basic column, K-line, OHLC, box plot, range area/column/bar, waterfall + horizontal waterfall, bullet, high-low, 100% stacked, funnel, pyramid, spider column, heatmap, contour) plus a second section for the structural capabilities (linear trend + target reference line + outlier annotations, named secondary axis, moving average, quadratic fit + reference band + hover tooltip), driven by 8 new mock data sets in `MockData` (`PriceCandles`, `StayBoxes`, `TempRanges`, `RevenueWaterfall`, `DeptKpi`, `RevenuePoints`, `SalesPointsB`, `SalesReferenceLines`).
- `demo/probe-chart.cjs` — a 46-assertion real-browser regression for the whole chart surface. It counts geometry inside the rendered SVG (candle bodies and wicks, OHLC open/close ticks, box whiskers/caps/median, range caps, waterfall connectors, bullet bands/targets, funnel and pyramid polygons, spider columns, 21 heatmap cells, contour boundary lines, trend lines, reference lines/bands, annotations, secondary axis ticks), reads computed styles to prove up/down colours are really red/green, and drives the mouse to verify the tooltip appears with the right category + value, that the crosshair lands on the bar centre, and that it clears on mouse-out. `demo/shots-chart.cjs` captures the sections as PNGs for eyeballing. Two traps worth remembering: the first `<rect>` in every chart SVG is the full-canvas background板 and legend swatches are also `<rect>` (so assertions must filter to rects carrying a `<title>`), and a K-line axis that starts at 0 shrinks the candles to a sliver — which is what the zero-baseline fix below is about.

### Fixed

- **`ElChartGrow` no longer forces a zero baseline for value-hugging series.** `Consider` seeded its range accumulators with `0`, so the value axis always included zero regardless of the data, and `NiceScale(0, max)` then stretched it further: a K-line series ranging 41.2–45.6 was plotted on a `0–50` axis (bodies ~12% of the plot height — the "K线被压成顶部一条缝" bug), and box plots / range bands were similarly flattened. The range accumulators now seed from the first considered value, and a new `ElChartGrowEngine.NeedsZeroBaseline(ElChartType)` decides per family: bar-like series (`Column`, `Bar`, stacked and 100% stacked, `Waterfall`, `HorizontalWaterfall`, `Bullet`) keep the mandatory zero baseline — bar height must stay proportional to value — while value-hugging series (`Candlestick`, `Ohlc`, `HighLow`, `BoxPlot`, `RangeColumn`, `RangeBar`, `RangeArea`) scale to their own data domain, matching Radzen (`CartesianSeries.TransformValueScale` only does `scale.Input.MergeWidth(...)`, it never pulls the domain to zero). Locked by two new tests: `Candlestick_AxisHugsDataInsteadOfZeroBaseline` (41.2–45.6 → ticks 41…46, candle body > 30% of the plot height) and `DataHuggingFamiliesSkipZeroBaseline_WhileColumnKeepsIt` (range/box/high-low families skip zero, column still starts at 0).

- Audited thirty-eight layout / navigation / feedback components (`ElCarousel`, `ElTimeline`, `ElAlert`, `ElLoading`, `ElPopconfirm`, `ElTooltip`, `ElPopover`, `ElMenu`, `ElSubMenu`, `ElMenuItemGroup`, `ElTabs`, `ElBreadcrumb`, `ElSteps`, `ElAnchor`, `ElAnchorLink`, `ElTree`, `ElTreeV2`, `ElFooter`, `ElMain`, `ElAside`, `ElCol`, `ElCard`, `ElCollapse`, `ElSplitter`, `ElSpace`, `ElScrollbar`, `ElDialog`, `ElDrawer`, `ElDropdown`, `ElPopup`, `ElUpload`, `ElColorPicker`, `ElForm`, `ElBacktop`, `ElWatermark`, `ElTour`, `ElInfiniteScroll`, `ElCheckTag`) with the same scripted "declared vs consumed" pass, this time with a **two-stage cross-check** that first separates cascading consumers (a child component reading its parent's parameter) from unrelated same-named parameters. Two real defects were found and fixed:
  - `ElColorPicker.AriaLabel` was declared but never emitted — the root `role="combobox"` element now writes `aria-label`, matching the other pickers.
  - `ElDropdown.HideOnClick` (default `true`) was declared but never read: `ElDropdownItem.InternalOnClickAsync` unconditionally closed the menu, so a caller setting `HideOnClick="false"` could not keep it open. The item now resolves its owner dropdown and only closes when `HideOnClick` is `true`.
- Confirmed **not** defects (so the next audit can skip them): `ElMenu.CanCollapse` / `ElMenu.Match` (read by `ElSubMenu` / `ElMenuItem` as `TopMenu.*`), `ElBreadcrumb.Separator` / `SeparatorIcon` (`Breadcrumb?.*`), `ElSteps.AlignCenter` (`Steps?.AlignCenter`), `ElCollapse.NoAnimation`, `ElSpace.Separator` (`ParentSpace?.Separator`), `ElForm.OnValidate` / `LabelSuffix` / `RequireAsteriskPosition` / `ShowMessage` / `InlineMessage` / `HideRequiredAsterisk` / `StatusIcon` (all consumed through `Form.*` / `FormItem.Form.*`), and `ElScrollbar.Noresize`, which is deliberately an inert hint (documented by `ElScrollbarTests`). `ElPopup` has no public parameters by design (exactly 0 `[Parameter]` in `ElPopup.razor*`; `ElPopupLayer` owns the 3 that exist).
- Recorded (not yet implemented) the remaining declared-but-unconsumed parameters: `ElSubMenu.Model`, `ElTabs.Lazy` / `TabClass`, `ElTree.AutoExpandParent` / `OnNodeDblClick`, `ElCollapse.NoAnimation` (the item carries its own copy but never reads it either), `ElDialog.AppendToBody`, `ElDrawer.AppendToBody` / `LockScroll`, `ElDropdown.PopperClass` / `PopperStyle` / `ShowTimeout` / `Teleported`, `ElUpload.UploadClass`, `ElColorPicker.Persistent` / `AppendTo`, `ElForm.ValidateAfterFirstBlur` / `LabelRequiredIcon` / `IsCreate`. Most of these need real behaviour or a portal layer (teleport / append-to), not a class name, so they are intentionally left for a dedicated round.

- Audited seventeen display / data components (`ElSlider`, `ElRate`, `ElTransfer`, `ElSegmented`, `ElMention`, `ElCalendar`, `ElCountdown`, `ElTable`, `ElTableV2`, `ElDescriptions`, `ElDescriptionsItem`, `ElAvatar`, `ElBadge`, `ElProgress`, `ElSkeleton`, `ElSkeletonItem`, `ElEmpty`, `ElResult`, `ElStatistic`) with a scripted "declared vs consumed" pass instead of reading every `.razor`, and fixed three **declared-but-unconsumed accessibility parameters**. ① `ElSlider.AriaLabel` was never written to the DOM even though the single and range thumbs both carry `role="slider"` — both `<input class="el-slider__input">` branches now emit `aria-label`. ② `ElSlider.AriaLabelledBy` was equally dead — the same elements now emit `aria-labelledby`, so a slider can be named by an external label element. ③ `ElRate.AriaLabel` was declared but absent from the markup — the root `<div role="slider">` now emits `aria-label`. ④ `ElDescriptionsItem.AriaLabel` was declared but dropped, because the row markup is produced by the parent: `ElDescriptions.razor` now forwards it as `aria-label` onto the item's content cell in all three layout branches (bordered `<td>` ×2 and the non-bordered `<div class="el-descriptions__cell">`). Everything else held up: `ModelValue` / `IsDisabled` on `ElSlider` / `ElRate`, `Filterable` / `EmptyText` / `IsStripe` / `IsBordered` on `ElTransfer` / `ElTable` and `Dot` on `ElBadge` are intentional aliases, `ElDescriptions.Colon` is consumed through `ElDescriptionsItem.ShowColon`, and `ElSegmented`, `ElCountdown`, `ElAvatar`, `ElEmpty` and `ElResult` have no unconsumed parameters at all. Full unit suite green.
- Recorded (not yet implemented) the remaining declared-but-unconsumed parameters found by the same audit: `ElSlider.Vertical` / `Height` (no `el-slider.is-vertical` rules exist in the theme, so a vertical slider needs both the layout CSS and the click geometry), `ElRate.Readonly` / `TextTemplate` / `OnFocus` / `OnBlur`, `ElTransfer.PanelHeight` / `LeftDefaultChecked` / `RightDefaultChecked` / `ShowAll`, `ElMention.MentionClass` / `Position` / `PopperClass` / `Debounce` / `Editable`, `ElCalendar.RangeMode`, `ElSkeleton.Throttle` / `Round` / `Bordered`, `ElSkeletonItem.Round`, `ElStatistic.ShowTrendSign`, `ElTableV2.RowKey` and the `ElTable` batch (`HideHeader`, `TableLayout`, `HeaderRowClassName` / `HeaderRowStyle` / `HeaderCellStyle`, `CellClick` / `HeaderCellClick`, `FilterChange` / `ExpandChange`, `Load` / `OnLoadDataSource`, `ShowSummary` / `SumText` / `SummaryMethod`, `RowKey`, `ExpandContent` / `ExpandedRows` / `DefaultExpandAllRows`, `Virtualized` / `VirtualStartIndex` / `VirtualItemCount`, `ShowPageCountChanged`).

- Audited ten form controls (`ElInputOtp`, `ElSelect`, `ElSelectV2`, `ElCascader`, `ElAutocomplete`, `ElCheckbox`, `ElCheckboxGroup`, `ElRadio`, `ElRadioGroup`, `ElSwitch`) against the "declared vs actually consumed" rule and fixed three **declared-but-unconsumed** parameters in the radio family. ① `ElRadioGroup.Tag` (default `"div"`) was never read — the root element was hardcoded `<div role="radiogroup">`, so a caller could not switch the wrapper tag; the group now renders through the shared `ElementTagRenderer` (`Tag="@Tag"`, default still `div`, class preserved via its `Class` parameter), so `<ElRadioGroup Tag="section">` emits a `<section role="radiogroup" class="el-radio-group">`. ② The same wrapper was also missing its accessible name: `ElRadioGroup.AriaLabel` was declared but never written to the DOM — it now emits `aria-label` on the group element. ③ `ElRadio.AriaLabel` was likewise declared but absent from the markup — it is now emitted on the inner `<label role="radio">`. Everything else held up: the `Border` / `IsBordered` / `Bordered` aliases on `ElCheckbox` / `ElRadio` / `ElRadioGroup` are intentional (all three map to one backing field and reach `is-bordered`), the `IsClearable` / `Clearable` and `ModelValue` / `Value` aliases on `ElSelect` / `ElSelectV2` are wired, `ElSwitch` wires `Width` / `Height` / `ActiveIcon` / `InactiveIcon` / `HideIcon` / `InlinePrompt`, and `ElInputOtp` wires `Separator` / `AriaDescribedby` / `InputStyle`. Full unit suite green (**1599/1599**; the two timing-sensitive cases `ElAutocompleteTests.DebouncesRemoteSuggestions` and `ElFeedbackTests.MessageBoxPromptCreatesInputAndBeforeCloseCanBlock` are debounce-timeout flaky and pass on re-run).

- Fixed `ElDialog` / `ElDialogGrow` missing the **`is-center` centering rule**. Both components emit `el-dialog.is-center` when `Center` is set, but `theme.css` had no rule for it, so the header, body and footer text stayed left-aligned and did not match Element Plus (`text-align: center` on `__header` / `__body` / `__footer`). `fix.css` now adds the scoped `.el-dialog.is-center .el-dialog__header, .el-dialog.is-center .el-dialog__body, .el-dialog.is-center .el-dialog__footer { text-align: center }` ruleset (no global side effects). Verified with `demo/probe-c4.cjs` — 15 assertions across four components: `ElInputNumber` clear icon rendered and click wipes the value; `ElTag` close glyph / dark theme / small size / icon; `ElCard` `is-always-shadow` + header/body/footer regions; `ElDialog` overlay opened, `Center` adds `is-center` and centres the body text (`text-align: center`), `LockScroll` sets `body` `overflow: hidden`, and close collapses to `display:none` — plus a console-error check (all clean). Added 14 bUnit cases (`ElInputNumberTests` clear-icon ×N, new `ElTagTests`, new `ElDialogTests` Center/LockScroll); full unit suite green (**1599/1599**, up from 1585).

- Fixed the **time-spinner scrollbar being too thick and too dark** in both `ElTimePicker` and `ElDateTimePicker`, and stopped the two panels from disagreeing about it. Root cause: since Chromium 121 an element carrying a non-`initial` `scrollbar-width` / `scrollbar-color` **ignores `::-webkit-scrollbar`** and falls back to the platform track (15px wide on Windows, painting a ~5.5px thumb). Both panels were caught — the demo sheet lets `html, body, #app` inherit `scrollbar-color: rgba(0,0,0,.1) transparent`, while `index.css` pinned `.el-picker-panel .el-time-spinner__wrapper` to `scrollbar-width: none` and only revealed a `thin` bar on `:hover`. `fix.css` now owns one section for both: the standard properties are reset to `auto` (including their `:hover` variants, at a specificity that beats the base sheet) so the `::-webkit` rules actually apply, the track is `4px` and the thumb `rgba(0,0,0,.1)` (= `#e6e6e6`, the requested grey), with `border: 1px solid transparent` + `background-clip: content-box` so **the painted thumb is only 2px** inside a 4px drag target; the popup shell's EP-drawn scrollbar is hidden (its 20px thumb used to poke out of the popper edge on hover), and Firefox gets `thin` + the same colour through `@supports not selector(::-webkit-scrollbar)`. Measured drawn thumb: `5.5px → 2px`. `index.css` no longer writes any hide / hover-reveal rule for the time-spinner wrapper (its comment points at the new single source). Coverage: `demo/probe-time-scrollbar.cjs` — 15 assertions (standard properties really reset, fake bars hidden, rule text present, no leftover hide rule, console clean) plus `verify-datetime.cjs` 33/33, `verify-pickers.cjs` 34/34, `probe-time-spinner.cjs` 25/25, `probe-date-range-panel.cjs` 35/35 and the full unit suite green.

- Fixed the **blank band at the top and bottom of the `ElDateTimePicker` / `ElDatePicker` popup**. Inside the popper the panel is wrapped twice (`ul.el-select-dropdown__list > li > div.el-picker-panel > div.el-picker-panel__body-wrapper > div.el-picker-panel`); the base sheet gives every `.el-picker-panel` a `1px` border plus `margin: 5px 0` while the popup shell adds `padding: 6px 0`, so the inner panel painted a **second rounded rectangle inside the popper** and the top edge read as a 19px strip (bottom: 13px). `Border="false"` now really removes the frame (`is-borderless` had a class name but no rule), the popup's inner panel shell drops border / vertical margin / shadow, and the list's vertical padding goes to `0`, leaving the popper as the only thing drawing a frame — measured top gap `31px → 13px` (the header's own EP `margin: 12px` plus the popup border), bottom `13px → 1px`. Scoped to `.el-date-picker__dropdown`, so an in-page `ElDatePickerPanel` with `Border="true"` keeps its frame.
- Fixed the date / time popup drawing a **white arrow diamond on its top edge** (plus its drop shadow). EP's date and time dropdowns use `show-arrow: false`, as `el-select` does; the arrow is now hidden for `.el-date-picker__dropdown` only, so Select / Tooltip keep theirs.
- Fixed a picker wrapped in `ElConfigProvider Locale="en-US"` still rendering a **Chinese calendar**. The popup body is rendered by the `ElPopup` host, not by the picker, so the panels inside it inherit the cascading `ElementConfiguration` at the *popup's* position — i.e. the app default. `ElDatePicker` now forwards its resolved locale to each `ElDatePickerPanel` through the new `Locale` parameter (empty ⇒ fall back to the cascade, so standalone in-page panels are unaffected).
- Fixed a **range selection's first click giving no visual feedback**, and **paging one range panel leaving the other behind**. The first pick assigned `RangeValue = { picked, null }` — a two-way-bound parameter the next parent render overwrote — and, more importantly, the popup host was never asked to re-render, so the DOM kept the old `12 / 25` highlight (the same missing call meant only the *clicked* panel followed a page change). The in-progress start now lives in `pendingRangeStart`, which drives the highlight through `RangeHighlightStart` / `RangeHighlightEnd` and is out of the parent's reach, and both paths now refresh the popup via `dropDownOption.RequestRender` — the mechanism `ElAutocomplete` / `ElCascader` / `ElColorPicker` / `ElDateTimePicker` already used and `ElDatePicker` was the only consumer missing.
- Fixed `ElDateTimePicker`'s **time being completely invisible** in the popup footer. The footer inherits Element Plus's `.el-picker-panel__footer { font-size: 0 }` (that declaration collapses the whitespace between inline-block children), and the time area was a native `<input type="time">` that inherited the zero font size, so it painted nothing at all. The native control is gone, replaced by the spinner editor described above with an explicit `font-size`, and a guard test (`Guard_TimeInputDeclaresExplicitFontSize`) asserts the CSS declares it so the regression cannot come back silently.

- Fixed `ElDateTimePicker`'s **confirm button being clipped at the right edge** of the popup. The panel box (322px) was wider than the 320px scroll content box, and `.el-scrollbar { overflow: hidden }` simply cut off whatever overflowed — which was the `确认` button. The panel is now `box-sizing: border-box; max-width: 100%` and the scroll wrapper hides horizontal overflow, so the footer's `margin-left: auto` right-aligned button stays inside the panel.

- Fixed `aria-expanded` / `aria-selected` / `aria-disabled` being rendered as **valueless attributes** (`aria-expanded=""`) on the picker DOM. Passing a `bool` to an `aria-*` attribute makes Blazor emit the attribute with no value, which assistive technology cannot interpret as a state. These now emit explicit `"true"` / `"false"`, matching the convention already used by `ElButton`, `ElInputTag`, `ElSelect` and others — fixed on `ElDateTimePicker` (footer field and both trigger variants), on the shared `TimeSpinnerRenderer` and on `ElDatePickerPanel` (both also improve `ElTimePicker` and `ElDatePicker`). `ElDateTimePickerTests` pins the output.

- Removed stray leaked escape tokens (`` `r`n ``) that an earlier scripted edit had written into the range branch of `ElDateTimePicker.razor` and `ElDatePicker.razor`, where they sat inside the `<input>` tag as junk attribute text.

- Fixed `ElMenu` **horizontal popup sub-menus never opening**, and reworked sub-menu placement to match Element Plus's `currentPlacement`. Root cause: `ElSubMenu.ShowPopupCoreAsync` waited for `PopupService.SubMenuOptionsCore` to drain (`while (…Any()) await Task.Delay(50)`) *before* registering its own popup — in horizontal mode the parent popup is always alive, so a nested (or any subsequent) sub-menu could never reach `Add` and its popup never appeared; the blocking wait is gone. Placement now comes from `ElPopup.ResolveSubMenuPlacement` (EP `currentPlacement`): **only a first-level sub-menu in a horizontal menu opens `bottom-start`** (directly under its trigger), while every other case (nested level, vertical + collapse) opens `right-start` and flips to `left-start` when the right edge would overflow the viewport — previously everything was positioned `bottom-start`, so nested sub-menus dropped *below* their trigger instead of beside it. Popups are body-level siblings, **not DOM children**, so moving the pointer from a parent popup into its child left the parent container and started its close timer; the popup container now calls `KeepSelfAndAncestorsOpen()` on `mouseover` to cancel the hide timer **level by level** (EP's `mouseInChild`) and leaves on the non-bubbling `mouseleave`. `CloseSubMenuAsync` cascades to descendant popups by **index-path prefix** (component instances are rebuilt on re-render, so reference comparison is unreliable) and hides after the inherited `SubMenuOption.HideTimeoutMs`. `SelectItemAsync` also closes every popup explicitly after clearing `OpenedMenus` in horizontal / collapse mode, since visibility there is derived from `openedMenus` in EP but is an independent object here. Covered by 3 new `ElMenuTests` cases and the real-browser probe `demo/probe-menu-horizontal-nested.cjs` (11/11).

- Fixed `ElMenu` vertical sidebar sub-menus **collapsing the instant they opened**, leaving the child items unclickable. `ElSubMenu`'s title listened to `@onmouseout`, which **bubbles**, so moving the pointer between the title's icon, label and arrow fired it repeatedly and `OnOutAsync` flipped the inline `ul`'s `IsOpened` to false, hiding the children with `display:none`. The title now uses the non-bubbling `mouseenter` / `mouseleave` pair (matching EP's `utils/menu-item.ts` / `submenu.ts`); `OnOutAsync` returns early for vertical non-popup menus instead of collapsing the inline sub-menu, and the stray `:stopPropagation` on the icon / arrow is gone. A new `Refresh()` pass re-derives each instance's `IsOpened` from the root menu's `OpenedMenus` (the single source of truth, **only ever opening, never force-closing**), which removes the dual-source drift where `DefaultOpeneds` and the active item's ancestor path were written to the root copy only. Covered by 2 new `ElMenuTests` cases and `demo/probe-menu-vertical.cjs`.

- Fixed `ElPivotDataGridGrow` **shifting a whole row's data one column to the left** as soon as any row group was expanded, so a value rendered under the wrong column header (expand `保健品` and `耗材 / 华南 4,200.00` moves from the `华南` column into the `华北` column). Root cause: the row-header (frozen leading) column count is `layout.RowHeaderDepth` — the *maximum* row-header cell count across all body rows — and both the header and the footer occupy that many columns, but a partially drilled row tree only gives the **expanded** branch deeper cells. The engine pads the shallower rows with placeholder cells (`PadHeaderCells`), yet placeholders are deliberately **not rendered** (`ElPivotRenderCell.Skip`), so every un-expanded sibling row emitted one `<td>` fewer than the header and all of its value cells — plus its row total — slipped left by one column. The render model now gives each row's **last actually rendered** row-header cell a horizontal span of `RowHeaderDepth - its column index`, so it absorbs the trailing placeholder columns and the data area re-aligns with the header; detail rows already fill the row header width so their span stays `1` (idempotent), and the fully collapsed case is unchanged. Covered by `Renders_ExpandedRowGroup_SiblingRowsKeepColumnAlignment`, which uses a new `SimulateBodyLayout` helper (a `rowspan`+`colspan`-aware column cursor mirroring the browser) to assert every body row occupies the header's column count, each row's row-header right edge lands exactly on the data start column, and every row's value-cell column indices are identical. `ElPivotDataGridGrowTests` 168/168 green, full suite 1448/1448; new real-browser regression `demo/verify-pivot-row-align.cjs` (3/3 across collapsed / single-row-group-expanded / row+column expanded) alongside the existing `verify-pivot-header.cjs`.

- Fixed `ElPivotDataGridGrow` rendering a few **empty trailing columns** whose header showed only the value-field name (`金额(求和)`) while every data cell under them was blank. Root cause: a column-header cell's `RowSpan` was computed from the **number of column fields** (`maxLevel - (depth - 1)`) rather than from the header rows that actually get rendered. With two column fields (e.g. `区域` + `年份`) at their default collapsed state the deeper level produces no cells at all, so that trailing header row is trimmed away and `HeaderRowCount` (1) becomes smaller than the field depth (2) — the surviving `区域` leaf cell still carried `rowspan="2"`, so the browser let it keep occupying the **value-field-name row** (`ShowAggregateHeaderRow`, on by default) and pushed those cells past the last data column, producing columns that had a header but no data. The engine now clamps every header cell's `RowSpan` to `HeaderRowCount - rowIndex` right after trimming the empty rows: collapsed groups let the value-field name drop into the same column right below the group name (matching the single-column-field behaviour), expanded groups keep their original spans (the clamp is idempotent), and mixed expand/collapse states stay column-aligned. Covered by 2 new engine cases (`Engine_CollapsedColumnGroup_RowSpanNeverExceedsRenderedHeaderRows`, `Engine_ExpandedColumnGroup_KeepsRowSpanAcrossHeaderRows`) and 2 new rendering cases that **simulate the browser's table layout** (a `rowspan`-aware column cursor) and assert every header row occupies the same number of columns — `Renders_CollapsedColumnGroup_NoPhantomColumnsAndValueHeaderUnderItsColumn` and `Renders_ExpandedColumnGroup_ValueHeaderRowAlignsWithLeaves`; the obsolete `Engine_CollapsedColumnGroup_BecomesSingleLeafWithRowSpan` expectation was updated from `RowSpan == 2` to `RowSpan == 1` with the rationale in a comment. `ElPivotDataGridGrowTests` 167/167 green.

- Fixed `ElDropDownTableGrow`'s multi-select tags rendering with the label and the ✕ icon misaligned and without their pill background. `index.css` ships `.el-select .el-select__tags>span{display:contents}` (an Element Plus 2.x rule for the native select), and because this component's trigger deliberately reuses the `el-select` class the rule matched our `<span class="el-tag">` and **collapsed its box entirely** — the label and the close icon were promoted to flex items of `.el-select__tags`, so the tag's background, border, padding and height were all discarded, and `.el-tag .el-icon-close{position:relative;right:-5px;top:-1px}` (plus `.el-tag--small .el-icon-close{transform:scale(.8)}` and `.el-tag__close{margin-top:-2px}`) then pushed the icon out of line. The tag is now restored to an `inline-flex` pill and the close icon's relative offsets / transform are zeroed, both scoped to `.el-dropdown-table-grow`; `.el-select__tags` also returns to `position:static` (it was `absolute; top:50%; translateY(-50%)`) so a wrapped tag list grows the trigger instead of escaping it.

- Fixed the `ElDropDownTableGrow` search box rendering its placeholder in the same dark colour as typed text. `::placeholder` had no rule anywhere in the shipped CSS, so Chromium inherited the input's own colour (`rgb(48,49,51)`); it now uses `var(--el-text-color-placeholder, #a8abb2)` with `opacity:1` (Firefox would otherwise dim it further).

- Fixed `ElButton` silently **dropping** `Cls` when `AppendCustomCls="false"` was set, which it implemented as "ignore the custom class" instead of `ButtonBase`'s documented "replace the default classes with `Cls`". Every selector panel therefore rendered as empty boxes: `<ElButton Cls="el-picker-panel__icon-btn el-date-picker__prev-btn el-icon-d-arrow-left" AppendCustomCls="false" />` came out as `class="el-button"`, so the Element icon-font classes never reached the element and the glyph (`::before` content of `.el-icon-*`) was never drawn. `ElButton` now matches `ButtonBase`: with `AppendCustomCls="false"` and a non-empty `Cls` it emits **only** `Cls` (no `el-button` / `el-button--*` / `is-*`) and returns early. This restores the prev/next year-month arrows and the clear (×) button in the `ElDatePicker` / `ElDateTimePicker` panels, and the `el-dialog__headerbtn` / `el-message-box__headerbtn` positioning classes on the dialog and message-box close buttons.
- Fixed `ElTable` merging **reflection auto-generated columns with explicitly declared `<ElTableColumn />` children**, which produced two headers per field. `AutoGenerateColumns` defaults to `true` and `InitilizeHeaders()` runs from `SetParametersAsync` — i.e. *before* child components register — so `Headers` was still empty when the auto-generation pass ran, and the declared columns were appended afterwards on top of it. On `/data` this rendered `Id / Name / Price / Stock / Category / OnSale / CreatedAt` **plus** `名称 / 类别 / 单价(元) / 库存` (11 columns for a 4-column declaration); inside `ElDropDownTableGrow` the same table produced 10 columns where 3 were declared. Generated headers are now flagged (`TableHeader.IsAutoGenerated`) and discarded as soon as the first explicit column registers, so declared columns win. `Ignore`-flagged auto entries and the editable-table operation column are handled by the same pass.
- Fixed `ElTable` collapsing columns to ~25px and making every cell wrap one character per line. `ElTable.razor` emitted `width="auto"` for columns without an explicit `Width`, but `.el-table__header` is `table-layout: fixed`, where an invalid `auto` means "no width specified" — the remaining space was split **equally** instead of by content, and `.el-table .cell { word-break: break-all }` then broke words mid-character. In the `ElDropDownTableGrow` panel this turned `Category / Name / Price / Stock` into diagonal single-letter columns (10 columns × 25px) that read as garbage. Column width now resolves as `Width` → `MinWidth` (px-parsed) → **80px**, matching Element Plus's default `min-width: 80`, and the table width is emitted as `width: 100%; width: max(100%, <sum>px)` (`min-width` variant when `Fit="false"`) so a table whose columns need more room scrolls horizontally instead of squeezing below readability. The extra declaration keeps browsers without `max()` on the old behaviour.
- Fixed `ElTable` pagination being decorative: `EnablePagination` rendered an `ElPagination` but the body loop always iterated every row, so `PageSize`/`CurrentPage` had no effect (`ElDropDownTableGrow` with `PageSize="3"` showed all 6 rows while the pager claimed 2 pages). The body now iterates `PageRows`, which slices `rows` by the current page whenever `EnablePagination` is on and the row count exceeds `PageSize` (current page is clamped to the last page). Tables that fit in one page are untouched.
- Fixed `ElDropDownTableGrow` rendering a stray blank column for multi-select: it declared `<ElTableColumn Type="selection" />`, but `Type="selection"` is not implemented anywhere in `ElTable` — the checkbox column is rendered from `ElTable.HasSelectionColumn`, which defaults to `true` and so was *also* on for single-select. The component now passes `HasSelectionColumn="@Multiple"` and no longer declares a selection column, giving single-select no checkbox column, multi-select exactly one, and no phantom column in either. The demo additionally pins column widths and `Height="0"` (no fixed scroll area) so the panel hugs three rows plus the pager.
- Fixed `ElInputOtp` cell `aria-label` containing a leading space (`" OTP 1"`) when `AriaLabel` was unset. The Razor interpolated `$"{AriaLabel} OTP {index + 1}"` directly; it now calls a `CellAriaLabel(index)` helper that falls back to the Element Plus form `OTP character n of N` (the previously unused `GetCellAriaLabel`) and only prefixes `AriaLabel` when one is supplied.
- Fixed three stale test expectations that could not compile or no longer matched the shipped components, so `test/Element.ComponentTests` now builds and runs end to end (**1418/1418 green**, previously the project did not compile at all due to `ElInputOtpTests` passing `bool` to the string-typed `ElInputOtp.Mask`): `Mask` is asserted through its current string API and the cell-editing tests render with masking explicitly disabled (`Mask = null` → plaintext); the lazy-load assertion in `ElDataDisplayRemainingTests` now locates the tree node by its **own** `.el-tree-node__label` instead of `TextContent`, because children are now correctly nested inside the parent node (the old lookup matched the root and clicked the wrong arrow); and the infinite-scroll test registers a `JSInterop` stub for `Element.getScrollDistance` returning `1`, without which the near-bottom probe always failed inside bUnit and only the `Immediate` load ever fired.

- Fixed `ElTour`'s panel being completely unclickable: the mask `.el-tour__mask` is `inset:0` with `z-index: ZIndex-1` while `.el-tour__content` had no `z-index` at all, so the mask painted **above** the panel and swallowed every pointer event — `上一步 / 下一步 / 完成 / 关闭` could not be pressed, and clicking anywhere (including on a button) fell through to the mask handler and closed the tour. The panel now renders with an explicit `z-index: ZIndex` via a new `PanelStyle`, which also carries the previous inline `padding` so behaviour is otherwise unchanged.
- Fixed `ElTree.AutoExpandAll` being ignored when the tree is driven purely by `DataSource` (no `Url`): auto-expansion was only triggered from inside `ExpandAsync` / `AddChild`, so a data-bound tree never expanded any node even though `AutoExpandAll` defaults to `true`. `OnParametersSet` now expands the whole data source **once per data-source instance** (tracked by `autoExpandedSource`), which keeps a user's manual collapse from being undone on later parameter updates.
- Fixed `ElTree` throwing `NullReferenceException` when `DefaultExpandedKeys` / `DefaultCheckedKeys` were set together with a plain `DataSource`: `OnParametersSet` walked `items` for those keys *before* assigning `items = DataSource`. The assignment now happens first and the method returns early only when `items` is genuinely still null (e.g. `Url`-driven trees whose data has not arrived yet).
- Fixed `ElTree` overflowing the stack on malformed tree data: with the default `DataFormat=List`, `FindChildren` returns `items.Where(x => x.ParentId == x.Id)`, so duplicated ids (or ids equal to `ParentId`, e.g. everything left at the default `0`) made every node a child of every node. `Initilize` and the render path (`BuildTreeNode`) now carry a visited set and stop on nulls/cycles instead of recursing forever.

- Fixed `ElTable` pressing `Enter` inside an editing row: it no longer leaks into the save path and creates an unintended row.
- Fixed `ElTable` running the save path when `Enter` is pressed on a read-only table (`IsEditable=false`), which could throw when a column had no editor initialized.
- Fixed `ElTable` keeping the previous row's editor values when the editing row changed (double-clicking another row).
- Restored four text literals in `ElTable.razor` that had been destroyed by an earlier encoding conversion (filter panel clear button, add-row save button, empty text, `ElTableTreeColumn` exception message).

- Fixed the malformed `Element.Markdown.csproj` package description XML.
- Removed stale .NET Framework-era package references from `Element.csproj`.
- Updated ASP.NET Core and Microsoft.Extensions package references used by `Element.csproj` to the .NET 10 package line.

- Fixed `ElTreeV2` declaring a duplicate `[Parameter] public bool AutoExpandAll` that already exists on its base `ElTree` — Blazor rejected the component with *"declares more than one parameter matching the name 'autoexpandall'"*. Removed the redeclaration in `ElTreeV2` and set the virtual tree's default back to `false` (base `ElTree` defaults to `true`) via the `ElTreeV2` constructor, so callers who don't set the parameter keep the original virtual-tree behaviour while the parameter still binds from the base class.
- Exposed previously-`internal` observation/registration members as public API so the matching component test suites compile and run: `PopupService` collections (`SelectDropDownOptions`, `DropDownMenuOptions`, `SubMenuOptions`, `PopupLayerOptions`, `DropDownTreeOptions`, `DateTimePickerOptions`), `DropDownOption` layout fields (`AutoWidth`, `DropDownId`, `Placement`, `TransformOrigin`, `MinWidth`, `WidthPx`, `ScrollElement`), `ElSelect<TValue>.Options` / `IsOptionVisible` / `OnInternalSelectAsync`, `ElAutocomplete<T>.CurrentSuggestions` / `IsInternalLoading`, `ElUpload.Files`, `ElDropdown.ShowDropDownAsync`, and `ElTableV2` `StartIndex`/`EndIndex`. These are read-only/observer surfaces and do not change input behaviour.
- Resolved the `ElInputOtp` mask type conflict between the component and the `ControlConfigs` form generator: `ElInputOtp.Mask` stays a `string` (mask character, default `•`), while the form-generator attribute `InputOtpAttribute` now exposes a separate `bool Mask` (on/off switch) plus `string MaskChar`, bridged by `FormControlRender.ResolveMaskChar` — eliminating the `InvalidCastException` that occurred when a bool config value was coerced onto the string component parameter.

- Added a **built-in, lazily loaded OFD channel** to `ElViewerGrow`, so `.ofd` documents no longer fall back to `Unsupported`. `EnableOfdPreview` (new `[Parameter]`, default `true`) routes bytes *or* a URL to `dom.js`'s new `elementOfdViewer*` bridge (`Available` / `Mount` / `PageCount` / `ScrollToPage` / `Dispose`), which fetches `wwwroot/js/ofd/ofd.umd.min.js` (~0.9 MB) **only when an OFD is actually opened**; the pages it paints are real DOM with a text layer, so they are selectable, searchable and printable. The bridge takes precedence order `ContentTemplate` → `ExternalViewerUrl` → host-registered renderer → built-in channel → `Unsupported`, and `accept` only advertises `.ofd` when some path can actually open it. Hosts without `dom.js` still get `Unsupported` (capability missing), while a document that the channel *does* reach but fails to parse now reports `Error` and raises `OnError` instead of silently degrading. The channel is ES5-only (no arrow functions / template literals / `let` / `const` / optional chaining) and the component markup never contains a `<script>`. The `ElViewerGrow` test suite grew from 147 to **167 cases** — 16 of them cover this channel (default on, the precedence table, the bridge payload for bytes vs URL, page-count backfill, mount-once-per-document with re-mount on maximise, release on switch-off and on close, `GoToPageAsync` routing, failure vs capability-missing, and guards for the JS boundary) — and the JS guards were rewritten from "no JS at all" to "JS only through the `elementOfdViewer*` channel", including ES5 compliance, lazy loading via a dynamic `<script>`, and no channel call on the empty state or on a PDF.

- Fixed `ElViewerGrow` freezing the OFD page count across documents. The channel's parsed page count is now exposed as `OfdPageCount` and consumed through a new `EffectivePageCount` (`PageCount > 0 ? PageCount : OfdPageCount`); the component **no longer writes back** to the host's `PageCount` parameter. Previously, opening a 1-page OFD and then a 2-page one left the status bar stuck at `1 / 1` (the same bug showed up when uploading through the file picker instead of clicking a sample button, since the picker does not swap the `@key`).

- Fixed `ElViewerGrow` reporting `Ready` with a blank canvas when the built-in OFD channel could not mount. The channel's early-exit paths (no `JSRuntime`, bridge unavailable) used `return` **inside** the `try` block, which returns from the whole method and skipped the trailing `status = ResolveStatus()` — leaving the status from the previous render. The mount body was extracted into `MountOfdCoreAsync` so the tail (recompute status, raise `OnStatusChanged`) always runs, and the failed-mount message is kept in a dedicated field so a real failure is no longer indistinguishable from "this type is not supported".

- Fixed `ElAccountTableGrow` **silently discarding every credit-side (贷方) keystroke** — typing into any 贷方 digit appeared to do nothing while 借方 worked. Root cause: the credit `<td>` loop declared `var column = AmountColumnsPerSide + c;` but the `@oninput` lambda still closed over the shared loop variable `c`, which Blazor captures by reference; by the time the event fired `c` was already `AmountColumnsPerSide` (11), so `OnDigitInput` was called with `index == 11` against an 11-element digit array and bailed out on the bounds check. The credit loop now closes over a per-iteration local (mirroring the debit loop) so the index is the cell's own position.

- Fixed `ElAccountTableGrow` writing 借方 角/分 into the wrong side. `OnDigitInput` inferred the side from `index < MaxIntegerDigits`, but the index it received was the **per-side** column (`0…10`), so the 角 and 分 cells (index 9 and 10 on a side whose integer part is 9 digits) were classified as the opposite side and cleared the value the user was editing. `isDebit` is now passed explicitly by the markup. Both fixes are covered by `DigitInput_CommitsAndKeepsDebitCreditExclusive` (元位 → 借方, 借方角位 stays 借方, 贷方分位 → 贷方 and clears 借方).
- Enforced the **one-digit-per-cell** contract of `ElAccountTableGrow` amount cells end to end: every 金额 cell now carries `maxlength="1"` again (the browser itself rejects any real insertion past one character, paste included) **plus** a `keydown` fast path that writes the pressed digit straight into the row model before the browser's blocked insertion, so typing over an occupied cell **overwrites in place** instead of being swallowed — the original reason `maxlength` was removed. Backspace / Delete clear the cell, letters / `-` / function keys never touch the model (the `input` event fallback still sanitizes paste leftovers), and `ReadOnly` suppresses all of it. Covered by 5 new bUnit cases (`DigitKeyDown_OverwritesExistingDigitInPlace`, `DigitKeyDown_NonDigitOrControlKey_DoesNotTouchModel`, `BackspaceKey_ClearsTheDigit`, `DigitKeyDown_DoesNothingInReadOnly`, `DigitKeyDown_CreditSide_WritesCreditAndClearsDebit`) and 2 browser assertions (字母拦截、同格原位覆盖).
- Fixed `ElTable` **crashing with `IndexOutOfRangeException` for non-generic data sources** (`object[]`, `ArrayList`, `IEnumerable`): `InitlizeDataSourceAsync` unconditionally read `DataSource.GetType().GetGenericArguments()[0]`, which throws for array types. The engine now falls back to the first row's runtime type when the source type has no generic arguments. Also hardened `ResetSelectAllStatus` against a `null` `SelectedRows` (single-select callers pass `null`), which surfaced as a follow-up `NullReferenceException` in the same flow.
- Fixed the `ElAccountTableGrow` **会计科目 dropdown never being visible**: the component's own `.el-account-table-grow td { overflow: hidden }` rule clipped the `ElDropDownTableGrow` popup panel to the table cell, so the panel existed in the DOM (search + data worked) but was invisible and unclickable. `td.account-cell` now sets `overflow: visible` so the popup escapes the cell while the amount/abstract cells keep their clipping.
- Enriched the `ElAccountTableGrow` demo 会计科目 mock data to a hospital chart of accounts: 29 subjects across 资产 / 负债 / 净资产 / 收入 / 费用 five categories and 4 机构 (总院 / 东院区 / 西院区), shown in the dropdown as 科目编码 / 科目名称 / 科目类别 / 所属机构 columns with keyword search (e.g. searching 医保 filters to the 医保专户 / 医保结算 rows).

- Fixed `ElViewerGrow` leaving stale state behind the built-in OFD channel when state changed through the public API instead of a toolbar click. `LoadAsync` / `CloseAsync` / `ToggleMaximizeAsync` now request a re-render (`await InvokeAsync(StateHasChanged)`) because the channel's mount/re-mount/dispose runs *after* rendering — without it, a host that does not bind `Document` / `Maximized` would keep the old pages (wrong width after maximising, or a blank canvas when re-opening the same `ElViewerDocument` instance after `CloseAsync`).

### Fixed

- **折叠态左侧菜单弹层「抖动 / 撑高页面 / 无滚动条」**（2026-09-28）：子菜单弹层 `.el-menu--popup-container` 此前**没有 `max-height` 也没有 `overflow`**——分类组件多时弹层比视口还高，绝对定位的弹层把文档可滚动区撑大，于是鼠标滚轮会带动下方 `.demo-main` 滚动、并因文档回流产生抖动，且内容被裁掉看不到。修复：`ElPopup.razor` 给弹层加 `max-height:calc(100vh - 16px); overflow-y:auto; overscroll-behavior:contain`，内容多时**弹层自身内部滚动**（不再撑高页面、不再联动滚动下方的页面）；`demo/app.css` 补 `.el-menu--popup-container` 细滚动条（6px、thumb 透明、`:hover` 才显形，若隐若现）。端到端 `probe-collapsed-popup.cjs` 验证：49 项弹层 `maxHeight=784px`、`scrollHeight 1157 > clientHeight 452`（滚动条生效）、`top` 12 次采样恒定、`document` 滚动量恒为 0、滚轮后 `.demo-main` 滚动量不变、控制台零错误。

- **ElTable 内嵌入 ElCarousel 双击/单击图片打不开预览（2026-09-29）**：`/table` 第⑦段「表格列内嵌入 ElCarousel」的轮播此前**未开 `PreviewOnClick`**，所以单击或双击图片都毫无反应；且 `ElTable` 的 `<tr @ondblclick="BeginEditAsync">` 会把双击冒泡吃掉——嵌在**可编辑表格**里时双击图片会误触发行进入编辑态。修复：`ElCarouselItem.razor` 的轮播项加 `@ondblclick:stopPropagation`（双击被限制在轮播项内部，不再冒泡到表格行）；`TableDemo.razor` 第⑦段给内嵌轮播补 `PreviewOnClick="true" PreviewLoop="true"`。端到端 `probe-table-carousel-preview.cjs` 验证 5/5：单击图片弹 lightbox、双击也能弹、双击未触发行编辑、`ESC` 关闭、零 pageerror。

- **`ElRibbon` 功能区导航（带 Menu 弹出菜单）两个 bug 修复（2026-10-06）**：
  - **① 点击「开始」弹出的菜单位置不对**：`ElRibbon.razor.cs` 的 `ShowMenuAsync` 把浮层定位点算成 `new PointF(rect.X, rect.Height)`——第二个分量用的是**标签自身的高度**而非**标签底边在视口中的 Y 坐标**，于是浮层被推到视口顶部（`top`≈40）而非标签正下方（`top`≈标签 `Y+Height`≈456）。修复：`rect.Y + rect.Height`。
  - **② 浮层点位分支内层 `position:static`，`left/top` 失效 + 整屏覆盖**：`ElPopup.razor` 的 else 分支（非对话框点位浮层）内层 `div` 在 `Point` 非空时**没有定位类**（只有 `class="el-message-box"` 走居中那条），`position:static` 下 `left/top` 被忽略；外层 `.el-message-box__wrapper` 是 `position:fixed` 铺满整屏 → 浮层被顶到视口顶端、且整屏遮罩会吞掉下面的页签点击。修复：点位分支内层 `div` 直接加 `position:fixed;left:{Point.X}px;top:{Point.Y}px`，按点位的视口坐标精确定位。
  - **③ 点击其它页签（门诊/住院/首页）状态没变化**：`ElRibbon.razor` 内部 `ElTabs` 用单向 `ModelValue="@ModelValue"`（父级 `NavDemo` 固定传 `"home"`），每次 `OnParametersSet` 都因入参 `"home"` ≠ 当前激活页签而 `SetActivateTabAsync("home")` 把激活**复位回首页**，于是点什么都跳回首页。修复：改成 `@bind-Value="ModelValue"`（`ElTabs` 已支持 `ModelValueChanged`，且菜单页签在 `PreventMenuSwitchAsync` 里 `DisallowChange=true` 不会写入绑定值，故「开始」仍走菜单、其它页签正常双向绑定切换）。
  - **端到端验证**（`ce_build/probe_nav_verify.cjs`，Playwright + msedge 打真实站）：点击「开始」→ 菜单出现且 `top` 与标签底边吻合（dy<60）、`x` 与标签左缘吻合（dx<60）；点击「门诊」→ 激活页签变「门诊」且**停留 1.2s 不复位**；点击「住院」→ 激活切「住院」；点击「首页」→ 可切回——0 控制台错误。

- **`ElCarousel` 自动播放/项注销时 `ObjectDisposedException`（Timer 已释放仍 `Start`）修复（2026-10-06）**：`ElCarouselItem.Dispose()` → `Carousel.RemoveItem` → `EnsureTimer()` → `timer.Start()`，而 `ElCarousel.Dispose()` 已先把 `timer` 释放——Blazor 子组件先于父组件释放的时序下，`Start()` 抛 `System.ObjectDisposedException: Timer`，该异常落在渲染释放管线（`ComponentState.DisposeInBatchAsync`）里，会打断当批渲染、造成整页（含 `/table` 里内嵌轮播所在表格的滚动容器）渲染抖动。修复：`ElCarousel` 加 `_disposed` 标志，`EnsureTimer()` 入口 `if (_disposed) return;` 直接短路；`Dispose()` 先置 `_disposed=true` 再释放 `timer` 并置 `null`（顺带让 `PauseOnHover` 的 `timer?.Stop()/Start()` 在释放后变 null 安全）。端到端 `ce_build/probe_table_console.cjs` 在 `/table` 静置 9s + hover + 滚轮后，`Timer`/`ObjectDisposed`/`ElCarousel` 相关控制台/页面错误归零。

- **细滚动条「悬停才显形」跨帧重建导致滚动条闪烁修复（2026-10-06）**：`elementBindFadeScrollbar` 的 `repaint` 对真实滚动容器做 `overflow:auto/scroll → hidden → auto` 的跨两帧重建以强制 Blink 重绘 `::-webkit-scrollbar`。但 `overflow:hidden` 那两帧会**让滚动条彻底消失**（`offsetWidth - clientWidth` 从 4 变 0），当鼠标在多个滚动容器（如 `/table` 一页有 6 个 `.el-table__body-wrapper` + 页面 `.demo-main`）之间移动、或在进入/离开容器边界时，就会看到滚动条一闪一闪；原实现还会因重叠触发把 `overflowY` 永久钉死成 `hidden`（整页滚动条失效）。修复：`repaint` 改为 `auto ↔ scroll` 互换——两种取值在「内容溢出」时滚动条**始终可见**，仍借 overflow 变化触发 Blink 重绘（让 `:hover` 拇指显形），但滚动条不再消失 → 消除闪烁；同时因永不写 `hidden`，「钉死成 hidden」的回归也被一并根除。

- **`ElDialog` 的 `AlignCenter` 对话框常显且永远关不掉（2026-10-06）**：`ElDialog.razor` 的容器样式把 `display:none`（关闭态）与 `display:flex/align-items:center/justify-content:center`（`AlignCenter`）**无条件先后追加到同一个元素**上——后者写在后面，于是 `AlignCenter` 的对话框一进页面就是 `display:flex` 常显，且因为 `ModelValue` 本就是 `false`，点右上角 X / 点遮罩 / 按 Esc 全都被 `CloseAsync` 开头的 `if (!ModelValue) return;` 直接短路，表现为「关闭不掉」。修复：弹性布局改为 `ModelValue && AlignCenter` 才输出，与关闭态互斥（关闭=只有 `display:none`；打开且 align-center=只有 flex 三项）。

- **对话框/抽屉的 z-index 与 MessageBox 各自计数，导致 `before-close` 确认框被对话框盖住点不到（2026-10-06）**：`ElDialog` / `ElDialogGrow` / `ElDrawer` **各自**维护一个从 2000 起步、每次 +2 的静态计数器，而 MessageBox / Notification / Message 走的是 `ElPopup` 里另一个从 2000 起步、每次 +1 的计数器——两边互不知情。实测「先开一个 `ElDialog`（容器 2002）→ 关它时触发 `before-close` 弹 MessageBox（容器 2001）」时，确认框落在对话框**之下**，`elementFromPoint` 命中的是对话框容器，用户点不到「确定」。修复：`ElPopup` 新增 `internal static int AllocateOverlayZIndex()`（`Interlocked.Add(ref ZIndex, 2) - 2`，返回遮罩层槽位，容器层取 +1），三个整屏浮层组件改为统一从它取号，与 MessageBox 共用同一计数器，后开的层永远在上。

- **`ElDialog` 全屏（`Fullscreen`）时仍输出内联 `width` / `margin-top`，只占半屏（2026-10-06）**：`Width` 默认 `50%`、`Top` 默认 `15vh`，此前无论是否 `Fullscreen` 都写进内联样式——内联 `width:50%` 会**盖掉** `.el-dialog.is-fullscreen { width:100% }`，全屏对话框只占半屏且被 `margin-top:15vh` 往下推。对齐 Element Plus 的 `style` 计算（`if (!fullscreen)` 才写 width/margin-top，全屏时 margin 归零）：改为 `!Fullscreen && …` 才输出 `width` / `margin-top`，`Fullscreen` 时补 `margin:0`（与 `ElDialogGrow` 已有写法一致）。

- **`/dialog`、`/drawer` 两页端到端与单测验证（2026-10-06）**：`ce_build/probe_dialog_drawer.cjs`（Playwright + msedge 打真实站）19/19 全绿、0 浏览器错误——初始 6 个对话框全部 `display:none`；基础对话框 X / 遮罩 / Esc 三种关法均可关；`align-center` 打开后垂直居中（中心 y=450/900）且 X 可关；全屏对话框铺满 1280×900 且可关；`before-close` 确认框在对话框**之上**且确认后对话框关闭；header 插槽可开关；`/drawer` 四个方向按钮 rtl(x=896)/ltr(x=0)/ttb(y=0,h=270)/btt(y=630,h=270) 分别从对应边滑出（尺寸 30%），可拖拽抽屉渲染 resizer，`Modal=false` 抽屉可开。单测 `ElDialogTests` + `ElDialogGrowTests` + `ElDrawerTests` 28/28 通过。
- **`/drawer` 页「四个方向」四个按钮点了没反应（2026-10-06）**：`DrawerDemo.razor` 的回调只写 `Dir = DrawerDirection.Xxx`，而抽屉的 `@bind-ModelValue="Open"` 的 `Open` **始终是 false**（页面上没有任何地方把它置 true），所以点按钮只改了一个不可见的内部状态（抽屉还没渲染过）→ 看起来「按键没有效果」。修复：新增 `OpenWith(dir)` 一次同时设置 `Dir` 与 `Open = true`，点击即按该方向滑出（对齐 Element Plus 文档里「选方向 + 打开」的示例行为）。

- **`ElDialogGrow` 对齐 Radzen `DialogService` / `RadzenDialog`，新增服务化对话框（2026-10-06）**：此前 `ElDialogGrow` 只有「声明式内联」一种用法——必须在页面里写死 `<ElDialogGrow>` 标签、靠 `@bind-Visible` 开关、**无法打开任意组件、拿不到任何返回值**，与 Radzen 官方 Dialog 示例（`DialogService.OpenAsync` 打开组件/`Close(result)` 回传/`Confirm`/`Alert`/`CanClose`/动态改属性）差距很大。本次按「抄能力不抄代码」原则补齐，实现全部自研、零 Radzen 依赖：
  - **`ElDialogGrowService`**（对应 `DialogService`）：`OpenAsync<TComponent,TResult>(title, parameters, options)` 打开任意组件并 `await` 拿回结果；`OpenAsync<TResult>(title, RenderFragment<ElDialogGrowRef>, options)` 内联内容；`Open` / `Close` / `TryCloseAsync` / `Refresh` / `ConfirmAsync` / `AlertAsync`；`Count` 给当前栈深。已由 `AddElementServices()` 注册为 Scoped。
  - **`ElDialogGrowOptions`**（对应 `DialogOptions` / `ConfirmOptions`）：`Width` / `Height` / `Top` / `ShowTitle` / `ShowClose` / `CloseOnClickModal` / `CloseOnPressEscape` / `Modal` / `AlignCenter` / `Center` / `Draggable` / `Resizable` / `DestroyOnClose` / `LockScroll` / `AutoFocusFirstElement` / `FocusTrap` / `CanClose` / `Role` / `AriaLabel` / `CloseAriaLabel` / `CloseTabIndex` / `CssClass` / `WrapperCssClass` / `ContentCssClass` / `ModalClass` / `ContentComponent` / `Parameters` / `TitleContent` / `ChildContent` / `FooterContent`。所有可变属性 `set` 时触发内部 `Changed` 事件 → 宿主重渲染，等价于官方「Update dialog properties」示例。
  - **`ElDialogGrowHost`**（对应 `RadzenDialog`）+ **`ElDialogGrowHostItem`**：订阅服务事件，把每个请求渲染成一个自渲染的 `ElDialogGrow`（`el-dialog` 系列标签），支持栈式多开、关闭后从 DOM 摘除再完成 `Task`（否则调用方续体里还能看到已关闭的对话框）。
  - **`ElDialogGrowRef`**（对应官方示例里的 `Dialog` 级联值）：通过 `CascadingValue` 下发给正文任意组件，可 `CloseAsync(result)` / 改 `Title` `Width` `Height` / `Refresh()`。
  - **`ElDialogGrow` 组件增强**：`Height`、`ShowTitle`、`Resizable` + `ResizeMinWidth/ResizeMinHeight` + `OnResize`、`AutoFocusFirstElement`、`FocusTrap`、`Role` / `AriaLabel` / `CloseAriaLabel` / `CloseTabIndex`、`WrapperClass` / `ContentClass`，以及 `CloseAsync(object result)` / `CloseAsync<T>(T result)` + `OnCloseWithResult`（声明式用法也能回传结果）。`OnCloseWithResult` 排在 `OnClosed` 之后触发，宿主据此先摘 DOM 再完成 `Task`。
  - **JS（`wwwroot/js/dom.js` 末尾新增独立 IIFE）**：`elementDialogFocusFirst`（聚焦首个可聚焦控件，**跳过右上角关闭按钮**）、`elementDialogBindFocusTrap` / `elementDialogUnbindFocusTrap`（Tab / Shift+Tab 在框内循环 + 焦点被外部抢走时拉回；对话框从 DOM 摘除时自动解绑，不在 document 上留残监听）、`elementDialogEnableResize` / `elementDialogDisableResize`（右下角缩放手柄）。
  - **CSS（`index.css` 末尾新增 `/* ==== ElDialogGrow 服务化对话框 ==== */` 段）**：缩放手柄斜纹 + hover 高亮、`Confirm/Alert` 的正文与按钮排版、给了 `Height` 时正文区滚动。
  - **demo**：`/dialog` 页新增「ElDialogGrow 组件增强」段（可缩放 + 自动聚焦 + 焦点陷阱 + `ShowTitle=false` + `OnCloseWithResult` 回传）与「ElDialogGrow 服务化对话框」段（打开表单组件拿返回值 / 内联内容 + 级联 ref / `CanClose` 拦截 / `ConfirmAsync` / `AlertAsync`）；新增内容组件 `Components/EmployeeFormDialog.razor`（真实 `ElForm` + `FormFieldValidation` 校验规则，点「确定」校验通过才 `CloseAsync(model)` 回传）；`App.razor` 挂 `<ElDialogGrowHost />`。
  - **单测**：`ElDialogGrowEnhancedTests`（13 例）+ `ElDialogGrowServiceTests`（13 例），与历史 `ElDialogGrowTests`（24 例）合计 50 例全绿。

## [0.10.0] - Unreleased（原条目标题 `2.14.0-alpha.1`，2026-10-13 随包版本线改名；对齐的 Element Plus 基线仍是 `2.14.0`）

### Added

- Added `ROADMAP.md` for the Element Plus alignment plan.
- Added Element Plus 2.14 theme variable layer at `/_content/Element/css/theme.css`.
- Added documentation for the Element Plus alignment and component entry points.

### Changed

- Moved the main component library to the `.NET 10` baseline.
- Aligned public Razor component names with Element Plus-style `El*` names.
- Removed legacy public component names and compatibility aliases from the active mainline.

## [0.0.7.2] - 2023-02-14

### Changed

- Historical text and repository metadata updates.

## [0.0.7.1] - Historical

### Notes

- Earlier historical changes were not fully curated into this repository changelog.
