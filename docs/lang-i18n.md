# 库级词条（i18n）运行时

> 目的：把「组件里那句 `t('el.inputNumber.decrease')` 到底取不取得到文案」这件事，从假设变成实测。
> 结论先行：接通前本仓**没有**可用的 i18n；现在有了，五批消费方已落地：
> ① `ElInputNumber` + `ElCarousel`；② 下拉与空态族（`ElSelect` / `ElSelectV2` / `ElCascader` / `ElEmpty` / `ElTable` / `ElTableV2` / `ElTree`）；
> ③ 弹层族（`ElDialog` / `ElDrawer` / `ElPopconfirm` / `MessageBox` / `ElDialogGrowService`）；④ **插值能力** + 分页 / 输入框字数 / 走马灯指示器；
> ⑤ **日历族**（`PickerLocale` + 日期面板 + `ElCalendar`，50 条 `el.datepicker.*`）与颜色 / 滑块 / 穿梭框三族；
> ⑥ **表头筛选面板族**（`ElTableFilterPanel`，19 条：3 条 EP 逐字 + 4 条本库面板文案 + 12 条运算符）。
> 基线：`ElementLangTests` 20 + `LangDropdownTextTests` 12 + `LangOverlayTextTests` 11 + `LangInterpolationTests` 14 + `LangResourceGuardTests` 7 + `LangPickerTextTests` 23，
> 全量 bUnit **2361 / 0 失败**，浏览器探针 `verify-lang-i18n.cjs` **21/21** + `verify-lang-picker-family.cjs` **34/34**（2026-10-08 弹层宿主两批改完后重新 build 演示站复跑，仍全绿，见 `docs/testing-guards.md` §3.10 / §3.11；随后浮层归属守卫 `PopupHostScopeTests` +5＝2277、源码编码守卫 `SourceEncodingGuardTests` +4＝2281、`ElMenu` 配色 CSS 变量口径 +4＝2285、`ElTabs` 删除/拦截/对齐口径 +12＝2297、`ElMenu` ellipsis 截断口径 +4＝2301、`ElMenu` 截断行悬停 tooltip（本库扩展）+9＝2310、四个宿主的截断悬停 tooltip +31＝2341（`docs/text-ellipsis-tooltip.md` §6）、表头筛选面板 +20＝**2361**（`docs/table-filter-alignment.md` §8），这几批都不是词条批次，口径分别见 `docs/source-encoding.md`、`docs/menu-alignment.md` §7 / §8 / §9 / §10、`docs/tabs-alignment.md` §8 与 `docs/table-filter-alignment.md`）。

## 1. 接通前是什么状态（死代码，口径可复现）

| 检查项 | 接通前实测 | 复现 |
| --- | --- | --- |
| lang 服务的消费方（注入或调用） | **0 处** | `grep -rn --binary-files=text "ElementLang\|ILang" --include=*.razor --include=*.cs --include=*.js src demo test \| grep -v "/bin/\|/obj/"` → 6 行，其中 4 行是 `Lang/` 自身定义、2 行是注释 |
| DI 注册 | 23 项，无 lang | `awk 'NR>=25 && NR<=56' src/Components/Extension.cs \| grep -c "services\.Add"`（接入后 **24**） |
| `ElementLang` 是否实现 `ILang` | 否；且 `ILang.InitElementLangBaseAsync()` 是 `void`，调用方**无法 await** | 读 `src/Components/Lang/` |
| `T()` 取不到时 | `configuration?[sections]` → `null`，没有 fallback 通道 | 同上 |
| 语言包内容 | 只有 `Message` / `Snowman` / `SetEnLang` / `SetCnLang` 四个演示键，无 `el.*` | `src/Components/wwwroot/lang/*.json` |

所以 EP 那种 `:aria-label="t('el.inputNumber.decrease')"` **照抄进来只会拿到空串** —— 无障碍标签从英文退化成空白，
比不对齐更糟。这就是 `docs/input-number-alignment.md` §3b P2-3 当时判"不做"的理由；本轮改为**先把运行时接通，再接组件**。

## 2. 权威依据与取值策略（curl 实抓，不凭记忆）

`element-plus` dev 分支 `packages/locale/lang/zh-cn.ts`、`en.ts`：

| 键 | zh-CN（本仓采用 = EP 值） | en-US（本仓采用 = EP 值） |
| --- | --- | --- |
| `el.inputNumber.decrease` | 减少数值 | decrease number |
| `el.inputNumber.increase` | 增加数值 | increase number |
| `el.carousel.leftArrow` | 上一张幻灯片 | Carousel arrow left |
| `el.carousel.rightArrow` | 下一张幻灯片 | Carousel arrow right |
| `el.select.placeholder` / `loading` / `noData` / `noMatch` | 请选择 / 加载中 / 无数据 / 无匹配数据 | Select / Loading / No data / No matching data |
| `el.cascader.placeholder` / `loading` / `noData` / `noMatch` | 请选择 / 加载中 / 暂无数据 / 无匹配数据 | Select / Loading / No data / No matching data |
| `el.table.emptyText`（EP 的 `ElEmpty` 也回落到它） | 暂无数据 | No Data |
| `el.table.confirmFilter` / `resetFilter` / `clearFilter` | 筛选 / 重置 / **全部** | Confirm / Reset / **All** |
| `el.table.sumText` | 合计 | Sum |
| `el.tree.emptyText` | 暂无数据 | No Data |
| `el.dialog.close` | 关闭此对话框 | Close this dialog |
| `el.drawer.close` | 关闭此对话框 | Close this dialog |
| `el.messagebox.title` | 提示 | Message |
| `el.messagebox.confirm` | 确定 | OK |
| `el.messagebox.cancel` | 取消 | Cancel |
| `el.messagebox.close` | 关闭此对话框 | Close this dialog |
| `el.popconfirm.confirmButtonText` | 确定 | Yes |
| `el.popconfirm.cancelButtonText` | 取消 | No |

第五批另有一张"挂法"依据表（值取自 tag `2.14.0` 的 `packages/locale/lang/{zh-cn,en}.ts`，
挂法取自 `packages/components/color-picker-panel/src/components/*.vue`、
`date-picker-panel/src/date-picker-com/basic-{date,month,year}-table.vue`、`calendar/src/calendar.vue`）：

| EP 侧形态 | 本仓落点 |
| --- | --- |
| `…Label` → `aria-label`，`…Description` → `aria-valuetext`（color-picker-panel 三条滑杆） | `ElColorPickerPanel.razor` 同名属性，`ElColorPicker` trigger 用 `defaultLabel` + `description` |
| 日期表 `<th>`：名称 `weeksFull.{sun..sat}`，可见文字 `weeks.{sun..sat}` | `PickerLocale.WeekHeaderFullName` / `WeekHeaders`（本仓周标题行在 `tbody` 内，没有 `thead`） |
| 月表 `<td>`：名称 `month{1..12}`，可见文字 `months.{jan..dec}` | `MonthNumberEntry`（只取词条、不回落表）+ `MonthName` |
| 年表 `<td>`：名称就是年份数字 | 未建键（不是 locale 词条），保持原样 |
| calendar 标题 = `{year}` + `el.datepicker.year` + `el.datepicker.month{N}` | `ElCalendar.Title` 经 `FormatYear`（`{year}` 模板）+ `FormatMonth` |
| 三张表根节点 `role="grid"` + `aria-label=…TablePrompt` | `ElDatePickerPanel.razor.cs` 的表格 builder |

抓 EP 源码时两个环境坑：tag 名是 `2.14.0`（**没有** `v` 前缀，`v2.14.0` 会 404），locale 文件在 `packages/locale/lang/` 而不是 `packages/element-plus/`；
node 直接 `fetch` GitHub raw 会 `UNABLE_TO_VERIFY_LEAF_SIGNATURE`，改用 `curl … | node -e`（stdin 解析），且临时文件要写在仓库内（Windows 侧看不到 `/tmp`）。

**一条统一规则**（第二批时把第一批的例外也并进来了）：

- **词条值 = EP 权威 locale 的值**；**组件里的 fallback = 本仓改动前那个字面量**。
- 于是三条路都清楚：宿主不接词条 → DOM 与改动前一字不变；宿主接 zh-CN → 拿到 EP 中文措辞；接 en-US → 拿到 EP 英文措辞。
- 第一批曾给 `el.carousel.*` 的 zh 值填了本仓原字面量「上一张 / 下一张」作为"有意差异"。第二条批做下拉族时发现：
  既然词条本身是 opt-in 的，"怕改掉别人正在用的读屏文本"这个理由不成立 —— 不接词条就改不到，接了就是想要 EP 措辞。
  **留一个组件例外只会让规则变成两条**，所以已把 carousel 的 zh 值统一成 EP 的「上一张幻灯片 / 下一张幻灯片」，
  fallback 仍是「上一张 / 下一张」。改动同时更新了探针断言。

## 3. 接口

`src/Components/Lang/ILang.cs`：`CurrentLang` / `IsLoaded` / `LastLoadError` / `LocaleChanged` 事件 /
`InitElementLangBaseAsync()` / `SetLangAsync(lang)` / `SetLangAsync(lang, httpClient)` /
`T(key)`（旧语义，缺失返回 null）/ `T(key, fallback)`（缺失或空白返回 fallback）/
`Text(key, 显式参数, fallback)`（组件里最常用，三级取值）/
`T(key, fallback, args…)` 与 `Template(key, 宿主模板, 默认模板, args…)`（插值，见 §9）。

`Lang/ElementLang.cs` 要点：

| 设计 | 为什么 |
| --- | --- |
| 词条来源 `_content/Element-Plus/lang/{locale}.json` | 库自身静态资源，路径由 `PackageId` 决定（2026-10-18 包改名前是 `Element`，改名后跟着变成 `Element-Plus`），实跑验证 HTTP 200 |
| 键名 `el.a.b` 与 `el:a:b` 都收（内部归一成 `:`） | EP 用点、`IConfiguration` 用冒号；代价是**词条键里不许有点**，两份文件都遵守 |
| 只认 JSON 字符串 | 数字/布尔拍平成 `"3"`/`"True"` 当无障碍标签，比缺失更难查 |
| 载入失败**保留旧词**并记 `LastLoadError` | 404/断网时清空会让全部标签瞬间退化成 fallback，比"还是旧语言"更糟 |
| 切语言是整体替换 `volatile` 引用 | 字典发布后只读，渲染线程不会看到半截表，也不需要锁 |
| `lang` 入参白名单（字母/数字/连字符，≤35，不以 `-` 开头结尾） | 它直接拼进请求 URL，`../../appsettings` 必须被拒且**不发请求**（有测试锁） |
| 保留老的 4 参 ctor，宿主 `IConfiguration` 优先 | 已有宿主代码继续可用；宿主明确声明的来源优先级更高 |

**破坏性修正说明**：`ILang.InitElementLangBaseAsync()` 由 `void` 改为 `Task`，并新增 `LocaleChanged` 事件与
两个 `T` 语义之外的成员。该接口在本次改动前**无任何实现方与消费方**（§1 已证），因此这是可接受的必要修正；
对外发布的组件 API（`El*` 组件的参数与 DOM）没有任何破坏。

## 4. 注册与宿主接入

`AddElementServices()` 里新增：`services.AddScoped<ILang>(sp => new ElementLang(sp.GetService<HttpClient>()));`

- **Scoped 而不是 Singleton**：宿主普遍把 `HttpClient` 注册为 Scoped（本仓 demo 就是），
  Singleton 消费它会直接抛 `InvalidOperationException`。
- **工厂里用 `GetService<HttpClient>()` 而不是构造注入**：没有 `HttpClient` 的宿主
  （SSR、命名客户端、精简 Program.cs）也必须能解析出 `ILang` —— 组件的 `@inject ILang` 不能因为宿主少一行就崩。
  取不到 HTTP 客户端时 `ElementLang` 退化为「一切取词走 fallback」。

```csharp
// 1) 原有那一行即可拿到 ILang
builder.Services.AddElementServices();

// 2) 首帧之前取词，避免标签先渲染成 fallback 再跳一次（可选，但推荐）
var app = builder.Build();
await app.Services.GetRequiredService<ILang>().InitElementLangBaseAsync();
await app.RunAsync();

// 3) 运行时切换（命名 HttpClient 的宿主用 SetLangAsync(lang, myClient) 重载）
await Lang.SetLangAsync("en-US");
```

## 5. 关键坑：切语言后子组件默认不重渲染（浏览器先抓到，不是推演）

探针第一版 **8/10**，两条 FAIL 长这样：页面状态行已经是 `en-US：decrease number / increase number`，
但 `ElInputNumber` 的 `aria-label` 仍是「减少数值 / 增加数值」。

根因：词条是应用级可变状态，而 **Blazor 在父组件重渲染时会跳过参数没变的子组件**。宿主的
`StateHasChanged()` 只刷得动页面自身的 `@Lang.T(...)`，深层组件要等自己的参数变了才更新。

修法（现在是**基类统一处理**，组件不用各自接事件）：`ILang.LocaleChanged` +
`ElementComponentBase.OnInitialized` 里订阅、`Dispose(bool)` 里退订 ——

```csharp
// ElementComponentBase
[Inject] public ILang Lang { get; set; }

protected override void OnInitialized()
{
    base.OnInitialized();
    langSubscription = Lang.SubscribeLocaleChanged(() => InvokeAsync(StateHasChanged));
}

protected virtual void Dispose(bool disposing)
{
    if (disposing) { langSubscription?.Dispose(); langSubscription = null; }
}
```

为什么放基类：第一批只有 2 个消费方时各自订阅成本更低；第二批一上来就是 7 个组件 14 处文案，
"每个组件复制 6 行、漏一半就是内存泄漏"的写法不再划算。放基类也不给宿主增加新要求
（`ElementComponentBase` 本来就 `@Inject` 了 MessageBox / DialogService / MessageService 等，
少 `AddElementServices()` 一个组件都渲染不出来）。

两个写法细节：① 组件里取词用 `Lang`（基类的 public 注入属性），不要再 `@inject ILang Lang` ——
派生类里再生成一个同名属性会遮蔽基类的；② 回调用 `InvokeAsync(StateHasChanged)` 而不是裸调，
SSR/预渲染上下文才不会踩错同步帧。护栏：`LangDropdownTextTests.PlaceholderRefreshesWhenLocaleChangesWithoutParameterChange`
（只改服务、不碰参数）+ `SelectExplicitPlaceholderWinsOverEntries`（显式参数压过词条）+ `LanguageSwitchAfterDisposeDoesNotThrow`。

## 6. 消费方清单

| 组件 | 位置 | 词条键 | fallback（=改动前的值） |
| --- | --- | --- | --- |
| `ElInputNumber` | 递减 / 递增按钮 | `el.inputNumber.decrease` / `.increase` | `decrease` / `increase` |
| `ElCarousel` | 左右箭头（含预览层，4 处） | `el.carousel.leftArrow` / `.rightArrow` | `上一张` / `下一张` |
| `ElSelect` | 占位 / 加载 / 空态 / 无匹配 | `el.select.placeholder` / `.loading` / `.noData` / `.noMatch` | 请选择 / 加载中 / 无数据 / 无匹配数据 |
| `ElSelectV2` | 同上（与 Select 共用一族键，同 EP） | 同上 | 同上 |
| `ElCascader` | 占位 / 空态 / 无匹配 | `el.cascader.placeholder` / `.noData` / `.noMatch` | 请选择 / 无数据 / 无匹配数据 |
| `ElEmpty` | 描述文字 | `el.table.emptyText`（EP 的 empty 就是回落它） | 暂无数据 |
| `ElTable` | 空态 | `el.table.emptyText` | 暂无数据 |
| `ElTableV2` | 空态 | `el.table.emptyText` | 暂无数据 |
| `ElTree` | 空态 | `el.tree.emptyText` | 无数据 |

组件侧统一写法是 **`Lang.Text(key, 显式参数, fallback)`**（= 显式参数 > 词条 > 改动前字面量），
优先级形状直接照 EP：`useSelect.ts` 里就是 `props.loadingText || t('el.select.loading')`。

**第四批（插值 + 分页 / 字数 / 指示器）**：

| 位置 | 词条键 | fallback | 说明 |
| --- | --- | --- | --- |
| `ElPagination` 总数 | `el.pagination.total` | `共 {total} 条` | 原先是 `TotalText.Replace("{total}", …)` 手写替换，现在统一走运行时 |
| `ElPagination` 每页条数 | `el.pagination.pagesize` | `条/页` | 拼成 `15 条/页`，en 下是 `15 /page`（EP 同款拼接） |
| `ElPagination` 跳页前后缀 | `el.pagination.goto` / `.page` | 前往 / 页 | |
| `ElPagination` 跳页输入框 aria-label | `el.pagination.currentPage` | `页码` | 接词条后是 `第 {pager} 页`，带真实页码 |
| `ElPagination` 上/下页按钮 aria-label | `el.pagination.prev` / `.next` | EP 英文值 | **此前没有 aria-label**，属补无障碍缺口 |
| `ElInput` 字数统计 | `el.input.characters` | `{count} / {max}` | 不取词时仍是 `7 / 7`；取 zh 词条才变成 EP 的「7 / 7 个字符」 |
| `ElCarousel` 指示器 aria-label | `el.carousel.indicator` | EP 英文值 | **此前没有该属性**；索引跟 EP 一样是 0 基 |
| `ElTable` 排序箭头 / 筛选漏斗 aria-label | `el.table.sortLabel` / `.filterLabel` | EP 英文值（`Sort by {column}` / `Filter by {column}`） | 列名取 `header.Text`；列名为空时**整个属性不渲染**（不给读屏一个空的名称） |

**第五批（日历族 + 颜色 / 滑块 / 穿梭框）**：

日历族与前四批不同形：前四批是「取词 or 字面量」的二选一，日历族背后还有一套**内置 C# 数据表**
（`PickerLocale.cs` 的周名 / 月名 / PickerText 表）。按定下的口径 **组件参数 > `el.datepicker.*` 词条 > 内置表**，
所以没有词条文件的语言（zh-TW、未识别语言）行为与改动前**逐字一致**。

| 位置 | 词条键 | 内置兜底 | 说明 |
| --- | --- | --- | --- |
| 日期 / 月 / 年表格 | `el.datepicker.dateTablePrompt` / `monthTablePrompt` / `yearTablePrompt` | 不渲染 | 顺带给三张表补了 `role="grid"`（此前是结构缺口） |
| 周标题 `<th>` | `el.datepicker.weeksFull.*`（可见文字用 `weeks.*`） | 不渲染 | 全称只进 `aria-label`，短名仍是可见文字，与 EP 挂法一致 |
| 月格 `<td>` | `el.datepicker.month1..12`（可见文字用 `months.jan..dec`） | 不渲染 | 同一个格子上有两套词形（`1 月` 进名称、`一月` 是可见文字），探针分别断言，挂法写反了才会红 |
| 年份导航 / 清除按钮 | `el.datepicker.prevYear` / `nextYear` / `prevMonth` / `nextMonth` / `clear` | 不渲染 | 属性挂在 `ElButton` 上，经基类未匹配属性 splat 落到 DOM |
| 面板年月标签、日历标题 | `el.datepicker.year`（`{year}` 模板）+ `monthN` | `2026 年` / 英文裸年份 | EP 的 `el.datepicker.year` 中文是后缀「年」、**英文是空串**；本仓守卫不允许空白词条，故两份都存成 `{year}` 模板 —— 渲染结果与 EP 一致，形态是**已记录的偏离** |
| 面板底部按钮、`ElDateTimePicker` 确认 / 取消 | `el.datepicker.confirm` / `cancel` | `确认` / `取消`（内置表原值） | zh 词条是「确定」，与内置表「确认」不同形，正好当接线证据；`el.datepicker.now` / `selectDate` / `selectTime` 本仓无落点，**不建无消费者的键** |
| `ElCalendar` 按钮组与表头 | `el.datepicker.prevMonth` / `today` / `nextMonth` + `weeks.*` | 上个月 / 今天 / 下个月 | `WeekDays` 参数默认值从静态数组改成 `null`，否则宿主没指定时也永远"有显式值"，词条成死代码 |
| `ElColorPicker` trigger | `el.colorpicker.defaultLabel` + `.description`（`{color}`） | `color picker` / 不渲染 | `aria-description` 是新增属性 |
| `ElColorPickerPanel` 三条滑杆 | `svLabel` / `hueLabel` / `alphaLabel` + `svDescription` / `hueDescription` / `alphaDescription` | 名称保留改动前英文字面量、`aria-valuetext` 不渲染 | 面板上 `saturation` / `value` / `alpha` 那几个**既有**硬编码输入框标签刻意不动（有旧用例按原文断言，且不属于 EP locale 契约） |
| 预设色块 | `el.colorpicker.predefineDescription`（`{value}`） | `select color {value}` | 属性改动前就有，所以 fallback 用改动前的英文字面量 |
| `ElSlider` 单滑块 / 范围组根节点 | `el.slider.defaultLabel`（`{min}` `{max}`） | 不渲染 | 本仓（同 EP）把 `role="slider"` 挂在隐藏的 `<input type="range">` 上 |
| `ElSlider` 范围两端 | `defaultRangeStartLabel` / `defaultRangeEndLabel` | 不渲染 | 宿主写了 `RangeStartLabel` 时仍以宿主为准 |
| `ElTransfer` 筛选占位 / 空态 | `el.transfer.filterPlaceholder` / `noData` / `noMatch` | `Enter keyword` / `No data` / `No matches` | 改动前这三处**只有英文**，接词条后中文宿主才显示中文 |

三条与"怎么判定接对"直接相关的口径：

- **两条语言开关的分工**：`ElConfigProvider.Locale`（→ `ElementConfig.Locale`）决定 picker 说哪套语言，
  `ILang.CurrentLang` 决定载入哪份词条文件。`PickerLocale.Entry` 先比这两者（`Normalize` 后），**不一致时 Locale 赢**：
  面板的 `aria-label` / `aria-valuetext` 整组不渲染，而不是把另一种语言的词条漏进宿主明确指定了语言的组件里。
  `verify-lang-picker-family.cjs` 末尾两条断言就是钉这件事的（切到 en-US 后日历仍是「2026 年 10 月」，面板可访问名称变 `null`）。
- **「没词条＝零 DOM 变化」的落法**：改动前**已存在**的属性，fallback 一律填改动前那个字面量；
  本轮**新增**的属性（`aria-valuetext`、三张表的 `aria-label`、`th`/`td` 名称、`aria-description`）fallback 一律 `null`，属性直接不渲染。
- **优先级不能用 `T()`**：`SliderRangeStartLabelParameterWinsOverEntry` 抓到一处真缺陷 —— 原来写成
  `Lang.T(key, explicitLabel)`，词条会压过宿主参数；凡"显式参数优先"的落点必须用 `Lang.Text(key, 显式值, fallback)`。
  同形问题在四族里逐个审过，现在没有残留。


**第六批（表头筛选面板，2026-10-09）**：

| 位置 | 词条键 | fallback（=改动前的值） | 说明 |
| --- | --- | --- | --- |
| 勾选组底部两颗 | `el.table.confirmFilter` / `.resetFilter` | 筛选 / 重置 | 值＝EP 逐字（zh-cn.ts:152-153、en.ts:155-156） |
| 单选列表首项 | `el.table.clearFilter` | **全部** | EP 的值是「全部 / All」（zh-cn.ts:154、en.ts:157），**不是「清除」** —— 第一版按直觉写成「清除」，实读源码后改回 |
| 条件面板两颗 | `el.table.applyFilter` / `.clearAdvancedFilter` | 应用 / 清除 | 本库扩展键：EP 只有选项列表，没有运算符表达式面板 |
| 且 / 或关系下拉 | `el.table.filterRelationAnd` / `.filterRelationOr` | 且 / 或 | 本库扩展键 |
| 运算符下拉 12 项 | `el.table.operator.{contains,notContains,startsWith,endsWith,equal,notEqual,greater,greaterOrEqual,less,lessOrEqual,empty,notEmpty}` | 包含…不为空 | **必须写成 12 条完整字面量**：守卫只扫引号里的整串，`"el.table.operator." + name` 会被报成一条缺键 `el:table:operator:`（§7 第一条的又一次触发） |

消费方是 `ElTableFilterPanel.razor.cs`（`ConfirmLabel` / `ResetLabel` / `ClearLabel` / `ApplyLabel` /
`AdvancedClearLabel` / `RelationAndLabel` / `RelationOrLabel` / `LabelOf`），全部走 `Lang.T(key, 中文兜底)`；
宿主不接词条时 DOM 与不接时一字不变。细节与实测见 `docs/table-filter-alignment.md` §1、§3、§7。

**第三批（弹层族）**：

| 位置 | 词条键 | fallback | 求值时机 |
| --- | --- | --- | --- |
| `ElDialog` 关闭按钮 aria-label | `el.dialog.close` | `Close this dialog` | 渲染期（开着切语言会跟着换） |
| `ElDrawer` 关闭按钮 aria-label | `el.drawer.close` | `Close`（改动前就是这个英文） | 渲染期 |
| `ElPopconfirm` 两颗按钮 | `el.popconfirm.confirmButtonText` / `.cancelButtonText` | 确定 / 取消 | 渲染期 |
| `MessageBox` 标题 / 按钮 / 关闭 | `el.messagebox.title` / `.confirm` / `.cancel` / `.close` | 提示 / 确定 / 取消 / Close this dialog | 标题打开时定型，按钮与关闭渲染期 |
| `ElDialogGrowService.ConfirmAsync` | 同上 title / confirm / cancel | 提示 / 确定 / 取消 | 同上 |

弹层族与组件族在**接入形状**上有三点必须说清的差别：

- `MessageBoxOption` 是 DTO、`MessageBox` / `ElDialogGrowService` 是服务，都没有渲染生命周期。做法是 DTO 默认值留 `null`，
  由渲染处（`ElPopup` 的标题、`MessageBox.CreateButtonRenderer` 的 `RenderFragment` 闭包）在**渲染那一刻**取词；
  在服务里提前把字符串算死，弹层开着时切语言就不会更新 —— 有专门的测试盯这件事。
- `ElPopup` 不是 `ElementComponentBase` 的后代（它是应用根上的弹层宿主），拿不到基类订阅，
  所以自己 `[Inject] ILang` + 订阅 + `Dispose` 退订。
- 一个真实回归风险已被测试钉住：`MessageBoxOption.Title` 默认值从「提示」改成 `null` 后，
  `ElPopup` 的 header 渲染条件是「标题非空」，**解析顺序稍有差错整块标题就会消失**。
- 顺带补了一个既有无障碍缺口：`ElDialog` / `MessageBox` 的关闭按钮此前**只有图标、没有任何可访问名称**，
  现在连 fallback 都给了 EP 的英文值（纯新增属性，不改可见文字）。

⚠️ **参数默认值的兼容代价**：`ElSelect.Placeholder`、`ElSelectV2.NoDataText`、`ElTableV2.EmptyText`、`ElTree.EmptyText`
等参数的默认值从中文常量改成了 `null`（`null` 才表示"宿主没指定"，让词条有机会生效）。
**渲染结果不变**（fallback 就是原来那个字符串，全量套件里断言这些文字的旧用例一条没退红），
但如果有宿主代码直接读这些属性拿默认值，会拿到 `null` —— 这种读法本来也不该有。

**刻意没接**：`ElAutocomplete` 的 `LoadingText` / `NoDataText`（EP 的 autocomplete.vue 里**没有任何** locale 调用，
自造 `el.autocomplete.*` 就是伪装 EP 契约）；`ElTableV2.LoadingText`（EP 无 `el.table.loading`）；
`ElCarousel` 预览层的「关闭」与 `title="滚轮缩放…"`；`ElTreeSelect.EmptyText`（它显式把值传给 `ElTree`，
要接词条的宿主直接给 `ElTree` 留空即可）。`PickerLocale.cs` 的月份 / 星期表在**第五批已接**（见上表），
但只接「本仓真有落点」的键：`el.datepicker.now` / `selectDate` / `selectTime` 与 `PickerText.Time` / `Start` / `End`
仍不建键 —— 前者面板上没有对应元素，后者 EP 用的是 `startTime` / `endDate` 这类合成词、与本仓面板元素形态不同，
**给它们造键就是伪装 EP 契约**。

**第九批（引导 + 上传文件项，2026-10-10）**：

| 位置 | 词条键 | fallback（=改动前的值） | 说明 |
| --- | --- | --- | --- |
| `ElTour` 面板「上一步 / 下一步 / 结束导览」 | `el.tour.previous` / `.next` / `.finish` | `Previous` / `Next` / `Finish` | 三个按钮的文字**每次渲染现取**（见下方"渲染期取词"），宿主给 `PrevText` / `NextText` / `FinishText` 时仍以参数为准 |
| `ElTour` 关闭按钮的可访问名称 | `el.tour.close` | `关闭`（改动前 DOM 里就是这个硬编码中文） | 本库给关闭按钮补了 `aria-label`，所以它必须取词，否则切英文后读屏仍念中文 |
| `ElUpload` 文件项删除图标 | `el.upload.delete` | `Delete` | 同时补 `role="button"` + `tabindex="0"`，与 EP `upload-list.vue` 的可聚焦删除一致 |
| `ElUpload` 文件项悬停提示 | `el.upload.deleteTip` | `Press Delete to remove` | EP 是 `<i v-if="!disabled" class="el-icon-close-tip">{{ t(...) }}</i>`；`Disabled` 时**整条文字不渲染**，不留空壳 |
| `ElUpload` 图片卡片删除图标 | `el.upload.delete` | `Delete` | picture-card 形态的 `<i class="el-icon-delete">` 同样取名称 |

这一批的最大教训是**先找 EP 的真实消费点，再决定键要不要存在**：

- 第一版把 `el.upload.preview` / `el.upload.continue` 接在了触发按钮与拖拽区的文案上（`Drop file here or click to upload` / `Upload`）。
  实抓 EP 2.14.0 的 `upload-content.vue` 与 `upload.vue`：**这两个文件里没有任何 `t()` 调用** —— 那两句是宿主插槽内容，不是库输出。
  接上去等于自造 EP 契约，而且演示页里"看不见效果"（改语言那两处不动）。现在已回退成 EP 的字面量，
  `upload.preview` / `upload.continue` 两条键**一并删除**（EP locale 里有它们，但本仓没有消费者 —— 留着一个取不到的键，
  下一个人只会以为接线漏了）。`upload-content` 侧的演示位改成用 `AutoUpload="false"` 直接把文件放进列表，验的是列表项那两处。
- 同一条规则把 `el.tour.close` 判成"该留"：上一轮按"没有消费者"把它删了，但面板关闭按钮的 `aria-label` 明明白白写着硬编码 `关闭`
  —— 不是没有消费者，是**消费者没接线**。这一轮把它接上。

**渲染期取词（这一批又踩实一次）**：`ElTour` 最初在 `OnInitialized` 里 `NextText ??= Lang.T(...)`，
把文字冻在了首次取到的语言上 —— 浏览器实测「开着引导切英文，整页都换了、三个按钮仍写着上一步/下一步」。
改成 `Effective*` 只读属性在渲染时取词，并用一条 bUnit 用例（面板开着时 `SetLangAsync`）把缓存写法钉死：
该写法在这条上必红（见 §11 的变异记录）。

## 7. 词条资源守卫（第 5 条常驻守卫）

`test/Element.ComponentTests/LangResourceGuardTests.cs`，跑在仓库真实文件上：

1. 中英两份拍平后键集必须互为相等（单边键＝切语言时静默退化）。
2. 源码里出现的每个 `"el.*"` 字面量键，必须在**两份**文件里都存在且值非空白（假接入的直接形态）。
3. 覆盖断言：引用点 ≥4、词条数 ≥8、且显式断言 `el:inputNumber:decrease` 与 `el:carousel:leftArrow` 被扫到
   —— 否则"0 违规"只是正则失效的同义词。
4. 判据是纯函数 `Audit(referencedKeys, zh, en)`，所以能按五步配方两侧夹验：
   `AuditCatchesEveryRealViolationShape`（单边键/空白值/引用缺键共 6 条违规必须全被抓到）与
   `AuditDoesNotFlagAWellFormedFixture`（点写法引用对冒号写法键集不得误报）。
5. 占位符相关的两条判据（键集互等之外的"同一键两份占位符必须一致"与"引用带占位符的键必须传实参"）见 §9。

第五批在这三条上踩出了新形状，记在这里省得下次重踩：

- **键名必须写成完整字面量**。守卫的正则只认 `"el.xxx.yyy"` 整串，`"el.datepicker.weeks." + name` 这种拼接
  会被当成一个不存在的键直接报违规。所以 `PickerLocale` 里周名 / 星期 / 月份 / monthN 四族各写了一张**完整键名字面量数组**。
  第六批在同一个坑上又红一次：`Lang.T("el.table.operator." + 名称)` 被报成一条 `el:table:operator:` 缺键，
  改成 `switch` 十二条字面量（`LabelOf`）才通过 —— **凡"一族运算符/星期/月份"这种按枚举取词的，一律展开成字面量**。
- **不允许空白词条 = EP 的空串值不能照抄**。EP 的 `el.datepicker.year` 英文就是 `""`，本仓守卫把"值为空白"判违规，
  于是中英两份都改存 `{year}` 模板（zh `"{year} 年"`、en `"{year}"`）。渲染结果与 EP 逐字一致，**形态是偏离**，
  已在 §6 上表里标注。
- 由此带出一个真回归：`ElCalendar.Title` 里曾写 `Entry(..., "el.datepicker.year", null) != null` 这种"探测式"取值，
  引用了带 `{year}` 的键却没传实参 —— 守卫当场报红。修法不是关掉守卫，而是把 `FormatYear` 补一个可选
  `IFormatProvider`，无条件组装（顺带让 `ElCalendar.Culture` 这个公共参数不再是死代码）。

当前规模：zh / en 各 **123** 条（第五批后 98，第六批表头筛选面板 +19 到 117，第九批引导 +4、上传文件项 +2 到 123；
键数用 `node -e` 摊平 `el.*` 实抓，不手数），键集互等、无空白值、占位符互等。
`el.upload.preview` / `el.upload.continue` 在本批被删掉（EP locale 里有，但本仓无消费者 —— 见 §6 第九批）。

## 8. 验证

| 项 | 结果 |
| --- | --- |
| `ElementLangTests` | **20 / 20**：两种键写法、缺失与空白都回退、数字/布尔不算文案、404 与坏 JSON 保留旧词、切语言整体替换而非合并、非法 locale 零请求、无 HttpClient 退化、老 ctor 与宿主配置优先、DI 可解析、订阅句柄退订与重复 Dispose、两个组件的"参数没变也要跟着换"护栏 |
| `LangDropdownTextTests` | **12 / 12**：Select / SelectV2 / Cascader 占位与空态的三态（不取词零变化、词条生效、显式参数压制）、Empty 与 Tree 的空态、切语言无参数变化的重渲染、Dispose 后切语言不抛。`TestLang.Loaded(json)` 造真 `ElementLang` 替身（走真实 JSON 解析路径，不 mock 接口，键名写错当场就红） |
| `LangInterpolationTests` | **14 / 14**：插值语义 5 条（无实参保留字面量、多参数无序、非 `\w+` 不算占位符、无花括号零处理、不变文化与 null 容忍）、`T`/`Template`/`Text` 分工、分页总数与 `TotalText` 宿主模板插值、每页条数/跳页前后缀/跳页 aria-label/上下页 aria-label、输入框字数两种语言、走马灯指示器 0 基索引 |
| `LangPickerTextTests` | **23 / 23**（本轮新增）：颜色 trigger 与面板（含"没词条时 `aria-valuetext` 整组不渲染"这一侧）、滑块三态（无词条零 DOM / 词条生效 / `AriaLabel` 与 `RangeStartLabel` 压制词条）、穿梭框三态、日期面板（`role=grid` + 提示语 + `th` 全称 + 导航名称 + 底部按钮「确定ZH」vs 兜底「确定」）、`PanelFollowsLocaleInsteadOfLangFileWhenTheyDiffer`（Locale=en 而词条是 zh 时面板说英文且不漏中文）、`PickerLocaleResolvesParameterThenEntryThenTable` 三级优先、日历（标题 / 按钮组 / 表头三态 + `WeekDays` 参数压制）。夹具值统一带 `ZH` 后缀，所以"读到 ZH"就等于"读到词条"而不是兜底值 |
| `LangResourceGuardTests` | **7 / 7**：真数据体检（中英键集互等 + 源码引用键两份都在且非空 + 覆盖断言）、`Audit` 两侧夹具、注释掩码两侧夹具（`.cs` 与 `@* *@` 各一例，且断言掩码不改行数） |
| `LangTourUploadTextTests` | **13 / 13**（第九批新增）：引导三颗按钮三态（无词条仍是 `Previous/Next/Finish`、zh 词条生效且最后一步出「结束导览」、宿主 `NextText/PrevText` 压制词条）、**面板开着切语言**（`TourButtonsFollowLanguageSwitchWhilePanelOpen`，钉死 `OnInitialized ??=` 缓存写法）、关闭按钮 `aria-label` 三态（无词条仍是改动前的 `关闭`、切英文变 `Close`、`CloseAriaLabel` 压制）、上传删除图标 `aria-label` + `role/tabindex` + close-tip 三态、`Disabled` 时 close-tip 文字不渲染、picture-card 的删除图标同样取名称、上传提示随语言换。夹具走 `TestLang.Loaded(两种语言)`，用 `PasteUploadFiles` 把文件放进列表（不联网、不依赖上传端点） |
| 全量 bUnit | **2484 通过 / 0 失败 / 0 跳过**（2026-10-10 批次⑮ 后整份复跑，日志 `Temp/b15-fullsuite-r1.log`，RC=0）。增长轨迹：2199 →（+12 下拉族、+1 掩码夹具）2213 →（+11 弹层族）2224 →（+12 插值、+2 守卫插值判据）2238 →（表格无障碍等 +9）2247 →（+23 日历 / 颜色 / 滑块 / 穿梭框族）2270 →（表格按格刷新取证 −1 有界用例 +2 计数口径用例）2271 →（弹层宿主去 static 后的隔离回归用例 +1）2272 →（浮层队列归属守卫 `PopupHostScopeTests` +5，非词条批次）2277 →（源码编码守卫 `SourceEncodingGuardTests` +4，非词条批次）2281 →（`ElMenu` 配色 CSS 变量口径 +4，非词条批次，见 `docs/menu-alignment.md` §7）2285 →（`ElTabs` 删除/拦截/对齐口径 +12，非词条批次，见 `docs/tabs-alignment.md` §8）**2297** →（`ElMenu` ellipsis 截断口径 +4，非词条批次，见 `docs/menu-alignment.md` §8）**2301** →（四个宿主的截断悬停 tooltip +31，见 `docs/text-ellipsis-tooltip.md` §6）**2341** →（表头筛选面板与条件筛选 +20，非词条批次，见 `docs/table-filter-alignment.md` §8）**2361** →（批次⑦⑧的 grow / 滚动条 / 焦点环与 IP 组件等，非词条批次）**2418** →（+13 `LangTourUploadTextTests`，第九批）**2431** →（批次⑩ 树形展开与滚条口径，非词条批次 ＋11）**2442** →（批次⑪ `ElTour` 对齐官方镂空/键盘/复位，非词条批次 ＋24）**2466** →（批次⑫ 全站横条 8px→6px 与 Markdown 编辑器滚条复位，非词条批次 ＋1 例 CSS 守卫）**2467** →（批次⑬ 树形列箭头判据对齐 EP，非词条批次 ＋3 例 `ElTableTreeExpansionTests`）**2470** →（批次⑭ 新增 `KeepAliveShelfRenderFanOutTests` 钉住保活货架的三种渲染扇出口径，非词条批次 ＋5 例）**2475** →（批次⑮ /checkbox 半选显示判据，非词条批次 ＋9 例 `ElCheckboxIndeterminateTests`）**2484**，每一步都零退红（2026-10-10 批次⑮ 后整份复跑，日志 `Temp/b15-fullsuite-r1.log` **2484 / 0 失败**；批次⑭ 那次是 `Temp/b14-fullsuite-r3.log` 与 `…-r4.log` **两次都是 2475 / 0 失败**；批次⑬ 那次是 `Temp/b13-fullsuite-component.log`，RC=0；批次⑫ 那次是 `Temp/b12-fullsuite-2.log`）。⚠️ 全量口径只指 `Element.ComponentTests`：`test/Element.Test.csproj` 引用的 `template/` 工程在本仓不存在，`dotnet build` 直接 `CS0234`（早于词条批次，细节见 `docs/table-tree-expansion-alignment.md` §7.4 第 4 条）。⚠️ 本轮顺手清掉一条**长期红**的常驻守卫：`TimingGuardTests.Guard_EveryWaitCallSpecifiesTimeout` 抓到 `ElVideoPlayerGrowDisposeTests.cs` L38 的 `WaitForState` 没写 `timeout`（默认只等 1 秒，并行跑全量时会假失败）—— 补 `timeout: TestWait.Timeout` 后 2431 全绿，之前"1 条已知失败"的说法到此作废 |
| 浏览器探针（前四批） | `demo/tools/probes/verify-lang-i18n.cjs` **21 / 21**（第五批后复跑，无退红）：静态资源 200 含两批键、启动即中文标签、页内其它实例同样生效、切 en-US 后 `aria-label` 跟着变、换回不留脏值、走马灯取 EP 措辞、下拉占位在 `请选择`/`Select` 之间来回、占位与空态两族同步更新、**同一页里宿主显式写的 `"多选"` 不被词条覆盖**（反向验优先级，只断言"全页变英文"测不出优先级写反）、0 JS 异常。实测一次语言切换在几百个组件的 `/basic` 页上耗时 591ms（切出）/ 520ms（切回）；第三批加入弹层断言：对话框关闭按钮 aria-label 取到 `el.dialog.close`，且**开着对话框切语言时名称当场跟着换**（渲染期取词） |
| 浏览器探针（第五批） | `demo/tools/probes/verify-lang-picker-family.cjs` **34 / 34**：颜色 trigger 的 `aria-label` + `aria-description`（`{color}` 已插值成 `#409eff`）、SV / 色相 / 透明度三条 `aria-valuetext`（占位符全填，透明度那条读回的是 `rgba(64, 158, 255, 0.5)`）、预设色块 `{value}`、既有硬编码 `saturation` 标签未被改动、滑块单 / 双 / 范围根节点三形 + 「最小值」压制、穿梭框占位与 `noData` / `noMatch`（输入不匹配关键词后现场变成「无匹配数据」）、日期面板 `role=grid` + 提示语 + 7 条 `weeksFull` 全称配 7 条短名可见文字 + `prevYear`/`nextYear`/`prevMonth` + 「2026 年 / 10 月」标签、弹层底部「确定」（按小节标题定位，DateRange 的宿主 `Placeholder` 不落 `input`）、月表格 `td` 名称用 `monthN` 而可见文字用 `months.*`、年表格提示语、日历标题与按钮组表头；en-US 侧三条反向证据：滑块 / 穿梭框随 `ILang` 换英文，**日历与面板随 `Locale` 保持中文、可访问名称整组消失**（门控生效，不是漏接）|
| 浏览器探针（第六批，表头筛选面板） | `demo/tools/probes/verify-batch5.cjs` **14 / 14 PASS**、0 条 console error（整份日志 `demo/tools/probes/verify-batch5.log`）。取词侧四条证据：勾选组底部两颗按钮读出「筛选 / 重置」（且未勾选时「筛选」`disabled:true`）、单选首项是「全部」、条件面板两颗运算符下拉**没点开就显示「包含」**、关系下拉显示「且」。其余是定标（`position:fixed;left:848.41px;top:407.38px;…`）与命中、草稿→提交（勾选后 40 行不变、点筛选才 5 行）、条件面板输入「阿莫」点应用 8→1 行、排序 `aria-sort none→ascending`、滚轮 `0→112` / `0→240`、高亮 `current-row` 恰 1 行、侧栏浮层两点命中、/grow 弹层贴内容。**七组之间各自 reload `/table`**：串在同一条 circuit 里跑时，上一条留下的筛选会让后面的滚轮/排序/条件三组失去前提（实测报过 3 条假红） |
| 浏览器探针（第九批，引导定位 + 上传/引导取词） | `demo/tools/probes/verify-tour-upload-i18n.cjs` **26 / 26 PASS**、0 条 console error / 0 个 JS 异常（整份日志 `Temp/probe-tour-upload-4.log`，RC=0）。几何侧：第一步 `gapY:10` 且水平居中、`position:fixed` + `transform:none`（CSS 的 `left:50%;translateX(-50%)` 已清）、面板中心命中自己（遮罩不盖面板）、`__tourTimer` 在（跟进循环活着）、换步后贴第二个目标仍是 `gapY:10`（不是首次坐标残留）、**纯重排**（往目标上方插 260px 兄弟节点，不滚不动窗口）后仍跟回、`.demo-main` 滚 ±120 双向都跟、切语言引起重排后几何仍贴。**遮罩侧**：视口内 5 点取点全部命中 `DIV.el-tour__mask`。取词侧：zh `删除` / `按 Delete 键可删除` / `上一步·下一步·结束导览` / 关闭按钮 `关闭` → 切 en 后 `Delete` / `Press Delete to remove` / `Previous·Finish` / `Close` → 换回 zh 又回来（两向都验）。**牙口（变异实测）**：① 把 `ElTour.razor` 的 `aria-label` 改回硬编码 `关闭` → 该类**恰好 2 条红**（切换那条 + 显式参数压制那条），还原后 13/13；② 在浏览器里把遮罩 `pointer-events:none` → 命中测试从 `allMask=true` 翻成 `false`（命中退回 `HEADER.demo-header` / `MAIN.demo-main` 等页面元素），见 `Temp/diag-mask-teeth.log` |
| 演示站 | `/basic` 的 `#c4-num-lang-target` / `#c4-lang-select-row` + `#c4-lang-select-state` / `#c4-num-lang-state`，配「中文标签 / English labels」切换按钮；`/i18n` 有引导（`#i18n-tour-target-1/2`）与上传（`#i18n-upload-row`）两节，第九批的 26 条断言全跑在这页上 |

跑法：

```bash
cd demo/tools/probes
NODE_PATH="C:/Users/Administrator/.workbuddy/ce_verify/node_modules" node verify-lang-i18n.cjs        # 可选 BASE
NODE_PATH="C:/Users/Administrator/.workbuddy/ce_verify/node_modules" node verify-lang-picker-family.cjs # 可选 BASE，默认 5199
NODE_PATH="C:/Users/Administrator/.workbuddy/ce_verify/node_modules" node verify-batch5.cjs             # 第六批：表头筛选 / 排序 / 滚轮 / 高亮 / 侧栏浮层
NODE_PATH="C:/Users/Administrator/.workbuddy/ce_verify/node_modules" node verify-tour-upload-i18n.cjs    # 第九批：引导定位与跟随 + 引导/上传取词（跑在 /i18n）
```

两份探针**分开发跑**，不要同一条 circuit 里串起来：演示站用 `KeepAliveRouteView`，同一条 circuit 访问过的页面不会卸载，
攒到第八第九页时新页首屏会慢到 60s 等不到（本轮实测：`.el-transfer` 60s 不出）。各自跑各自的，两边都稳。

## 9. 插值：与 EP 的 `translate` 逐条对齐

EP 的实现只有一行（`packages/hooks/use-locale/index.ts`）：

```ts
(get(locale, path, path)).replace(/\{(\w+)\}/g, (_, key) => `${option?.[key] ?? `{${key}}`}`)
```

本仓 `LocaleFormat.Interpolate` 就是照它做的，四条语义都有测试钉住：

| 情形 | 结果 | 为什么这样 |
| --- | --- | --- |
| 正常占位符 | `共 {total} 条` + `("total",128)` → `共 128 条` | — |
| **没有对应实参** | **原样保留 `{total}`**，不吞不抛 | EP 同款；漏参数时界面露出花括号，比少一段文字更容易被发现 |
| `{}`、`{a.b}`、`{ }` | 不算占位符，原样保留 | EP 也只认 `\w+` |
| 数字/浮点/null | 按 **不变文化** 转（`1234` 不会变 `1,234`），null → 空串 | 避免区域设置把 `1.5` 变成 `1,5` |
| 重名参数 | 取第一个 | 与 EP 的对象取值等价 |

两个方法的分工容易踩错，注释里也写了：

- `T(key, fallback, args…)` —— 插值**最终取到的那段文字**（词条命中插词条，没命中插 fallback）。纯标签用它。
- `Template(key, 宿主模板, 默认模板, args…)` —— 宿主给的是**模板**时必须用它。`ElPagination.TotalText` 这类参数
  本来就是 `共 {total} 条` 形状，若走 `Text()`（"显式参数即最终文字"），宿主自定义模板里的 `{total}` 会原样显示出来。
- 反过来，`ConfirmButtonText` 这种是字面文字，就该用 `Text()`，不该再动它。

守卫侧同步加了两条（`LangResourceGuardTests`）：

1. **同一个键在两份语言包里的占位符集合必须相同** —— 不同就意味着切语言时某一边会露出 `{pager}`。
2. **引用了带占位符的词条就必须传对应参数**：按"键 → 该键在源码里的各处调用片段"扫描，
   只要有一处调用缺参数就报（同键在不同页面漏传，比全漏更难复现）。片段切到语句结束的 `;`，
   所以实参写在下一行的跨行调用也能认（有专门的"不误报"夹具）。

覆盖断言也加了一条：被引用的键里**至少 3 条带占位符**，否则这两条规则等于零输入空转。

## 10. 剩余待办（精确到键与位置，下次可直接开工）

**已接完的部分**：EP 带占位符的键里，除了下面第一条那对，本仓有落点的全部接通了
（pagination 的 total/currentPage + input.characters + carousel.indicator + table 的 sortLabel/filterLabel
+ colorpicker 的 description/svDescription/hueDescription/alphaDescription/predefineDescription
+ slider.defaultLabel + datepicker.year），无占位符那批同步接了 colorpicker 一族名称、slider 一族、transfer 三条、datepicker 一族。
`PickerLocale` 与日期面板、日历的接线细节见 §6 第五批表。

剩下的按性价比排序：

| 待接键 | 值（zh / en） | 消费点 | 备注 |
| --- | --- | --- | --- |
| `el.transfer.noCheckedFormat` / `.hasCheckedFormat` | 共 {total} 项 / 已选 {checked}/{total} 项 | 无默认消费者 | EP 里这两条只在宿主给了 `Format` 时才走，**本仓面板脚注没有默认计数文字**。将来若要补"共 N 项"这种默认可见文案，再接这两条并走 `Template()` |
| `el.datepicker.now` / `selectDate` / `selectTime` | 此刻 / 选择日期 / 选择时间 | 无落点 | 本仓此刻按钮不存在，输入框占位符是「请选择日期」这类合成词，不是这三个键。**不建无消费者的键** |
| `PickerText.Time` / `Start` / `End` | — | 范围面板标签 | EP 用的是 `startTime` / `endDate` 这类合成词，与本仓面板元素形态不同；给它们造 `el.datepicker.*` 键就是伪装 EP 契约。当前仍走内置表 |
| `ElCalendar` 的 select 控制器下拉 | — | 年/月候选项标签 | 兜底标签现在是裸数字（locale 中性），EP 在该处用 `{year}` / `monthN`。要接得先决定"下拉里要不要写 2026 年"，且宿主已有 `Formatter` 参数可定制，收益低 |
| 日历范围模式的「上一年 / 下一月」等按钮 | — | **不是库输出** | 实测这几句只出现在 `demo/…/CalendarDemo.razor` 的 `Header` 插槽里，是宿主自己写的字面量；EP 的 calendar 只暴露 prevMonth / today / nextMonth。不动它 |
| `el.colorpicker` 面板上 `saturation` / `value` / `alpha` / `"color value"` 四个硬编码标签 | — | `ElColorPickerPanel` 的隐藏输入框 | EP locale **没有**这些键（它们不是 locale 契约），且有旧用例按原文断言（`ElColorPickerPanelTests`）。刻意保持原样，探针里也有一条专门盯"不被词条改动" |
| `el.messagebox.error` | 输入的数据不合法! / Illegal input | 无消费者 | 本仓 Prompt 的校验提示由宿主 `InputValidator` 给字符串；**不建无消费者的键** |
| `el.inputOTP.*` | — | 本仓无该组件 | 跳过 |
| `el.cascader.loading`（**2026-10-10 批次⑬ 清点时新发现的唯一一条**） | 加载中 / Loading | 无消费者 | 两份词条里都躺着这一条，但 `ElCascader.razor.cs:1290-1292` 的 `ISelectDropDownContext` 是 `Loading => false` + `LoadingText => string.Empty`：级联的下拉**不显示整表加载文案**，只在节点右侧挂一个 `el-icon-loading` 图标（:1081-1084，无文字）。要么删掉这条键（与「不建无消费者的键」一致），要么给级联补一条默认可见的加载文案再接 —— 属决定，不擅自改。清点口径：`Temp/b13-lang-keys.txt`（zh 展平 128 键）∩`Temp/b13-lang-refs.txt`（`src/Components` 里出现的 `"el.*"` 字面量 125 条），差集另四条是 zh-CN.json 的历史遗留演示段（`Message` / `SetCnLang` / `SetEnLang` / `Snowman.*`），不参与组件取词 |

与词条无关但同批攒下的：

- `TestWait.Timeout` 分档（20s 一刀切会让"真挂掉"和"争抢"都表现为等满 20 秒；§3.9 那两次假红就是被它放大的）。
- ~~本仓无版本控制，每轮手工建 `_backup_before_*` 目录~~ —— **这条已经解决**：现在仓里有 git（`git log` 有提交、`git ls-files` 1483 个文件），
  改动可以用 `git diff` 复核、备份目录可以按批清理。剩下的收尾是把 `bin/`、`obj/`、`_backup_before_*`、`_artifacts/`
  这类目录正式忽略掉（当前 `.gitignore` 是 VS 自动生成的那份，规则不全）。

## 11. 环境提醒（本轮新踩）

- **Razor 属性里带字符串字面量必须用单引号包属性**：`OnClick="() => SwitchLangAsync("zh-CN")"` 会打出
  一串 `CS1026 应输入 )` + `CS7036 未提供参数`。正确：`OnClick='() => SwitchLangAsync("zh-CN")'`。
- **改的是库源码，就必须重发布演示站再验**：先 `curl --noproxy 127.0.0.1 .../_content/Element-Plus/lang/zh-CN.json`
  确认 200 且含新键，再看组件行为 —— 拿旧构建的静态资源测新代码，得到的"缺陷"是假的。
- **bUnit 里切语言后要"主动推一次渲染"**：`LocaleChanged` 的回调是 `InvokeAsync(StateHasChanged)`，
  而 `WaitForAssertion` 只会被**已发生的渲染**唤醒；高压并行跑全量时偶发 20s 超时（三次里红过一次）。
  测试里在切换之后补一句 `popup.Render()` 即可，真实浏览器没这个问题（渲染器自己会批处理，探针 21/21 实测）。
  同理：**不要用 `.Wait(timeout)` 阻塞测试线程等渲染器**，改成 `async Task` + `await`。
- **别把 Python 的 `u'…'` 写进 JS**：用脚本生成探针代码时，`u'中文标签'` 会原样落进 `.cjs` 变成语法错误
  （`node --check` 当场抓住，别等实跑）。
- **弹层类断言要绕开模态遮罩**：对话框开着时遮罩会拦掉 Playwright 的可点击性检查
  （`element intercepts pointer events`，然后一路重试到超时）。用 `page.evaluate` 走 DOM 层点击即可，
  走的仍是同一个事件回调，别为了测试去改产品交互。
- **多状态脚本要在每节结尾把状态归位**：第三批的对话框断言先报了一条假 FAIL，原因是上一节把语言留在了
  en-US 而断言按 zh 写。**"上一节结束时世界处于什么状态"要显式收尾**，否则报红的是脚本自己的顺序。
- **批量脚本里 `replace(old, new, 1)` 命中的是第一处相同文本**：本会话用它删探针末尾一处点击，
  结果删掉了前一节同文本的「切回中文」点击，探针立刻报 3 条**假 FAIL**（产品全程是对的）。
  判据：**探针连红、bUnit 全绿 → 先怀疑探针自己**，别去改组件；补丁叠到第三层就整份重写。
  另外：切语言是「全库活动组件重渲染」，实测约 0.5s，断言前要等条件（`waitForFunction`）而不是定长 sleep。
- **本仓源码编码混杂**：`Lang/ElementLang.cs` 原是 GBK 存盘（grep 不加 `--binary-files=text` 会整份跳过），
  `Extension.cs` 的中文注释里有历史遗留的 `U+FFFD`（早于本轮，不是本轮写坏的）。本轮重写/新增的 `Lang/` 三个文件
  都是 UTF-8：`ILang.cs`、`ElementLang.cs` 带 BOM，`LangSubscriptionExtensions.cs` 不带 —— 实测无 BOM 也正常编译
  （Roslyn 默认按 UTF-8 读，本仓大量无 BOM 中文文件），所以**不用为了这个专门补 BOM**，但 GBK 老文件必须认得出来。
- 数字口径更新：`AddElementServices` 注册项 **23 → 24**；探针主目录 **148 → 154**（`docs/testing-guards.md` §3.1 那次盘点后又加了 6 份：ElInputNumber 对齐的 5 份 + 本轮的 `verify-lang-i18n.cjs`）。第五批再加 1 份 `verify-lang-picker-family.cjs`，`.cjs` 实测 **166** 份。

第五批（日历 / 颜色 / 滑块 / 穿梭框）新踩的：

- **`KeepAliveRouteView` 会让访问过的页面留在 DOM 里**，探针的 `pushState` 换页因此有两个坑：
  ① 裸选择器（`div[role="combobox"]`、`table.el-date-table th[aria-label]`）会命中上一页遗留的节点或跨面板累加
  （实测把 7 条周标题读成 21 条，以及拿到 `null` 报出一条**假 FAIL**）—— 查询必须限定在自己的组件类名上、并锁定单张表；
  ② 攒到第七八页时新页首屏会慢到 60s 等不到（`.el-transfer` 超时），所以**按主题拆成独立探针、各起一条 circuit**。
- **`ElButton` 在两个 CJK 字符之间插空格**（EP 的 `autoInsertSpace`）：面板底部那颗按钮的 `textContent` 是「确 定」，
  `trim()` 不管用，比对前必须 `replace(/\s+/g, '')`。这条在 bUnit 里不会暴露（断言的是渲染树文本）。
- **DateRange 型 picker 的宿主 `Placeholder` 不落进 `input`**：那一族两个输入框的占位符是「开始日期 / 结束日期」，
  所以"按占位符找示例"会找不到 —— 按所属小节的 `h3` 标题定位更稳。
- **词条文件只有 `zh-CN.json` / `en-US.json` 两份**（`ls src/Components/wwwroot/lang`），
  守卫比的就是这两份的键集，所以"加一族键"必须是**两份同时加**，单边加一条就当场红。
- **别用 Python 往 GBK 控制台打中文**：`UnicodeEncodeError` 之前写进 stdout 的字节已经是 mojibake，
  之后 `iconv` 也救不回来。读语言包统一用 `node -e`（按 UTF-8 读文件，输出字节原样经 bash 管道，中文不乱）。
- **后台命令重定向到不存在的目录 = 静默 exit 1，测试根本没跑**：本仓根目录没有 `_artifacts/`，
  `dotnet test > _artifacts/xxx.log` 只会留下「Exit code 1 / No such file or directory」。
  重定向前先确认目录存在，或写到 `/tmp`（bash 侧可见即可，不必进 Windows 文件系统）。

第九批（引导定位 + 引导/上传取词）新踩的：

- **Razor 里写错参数名不会报错，会把值 splat 进未匹配属性**：`/i18n` 的表格空态写的是
  `<ElTable Data="…">` + `<ElTableColumn Prop="…">`，而真参数是 `DataSource` / `Property`。
  基类 `ElementComponentBase` 上有 `[Parameter(CaptureUnmatchedValues = true)] Attributes`，
  于是两个值被静默收下、`DataSource` 仍是 `null`，列注册时 `ElTable.AddColumn` 抛
  `风格 ElTable 没有设置 DataSource 属性或该属性为空` —— 整个渲染批失败，浏览器里是一条
  `crit: WebAssemblyRenderer[100]`。**编译器一声不响，只有控制台会告**。
  判据：见到 `WebAssemblyRenderer[100]` 先看栈里的组件方法与参数名，别急着怀疑组件；
  写演示页时把参数名对照 `.razor.cs` 里的 `[Parameter]`，`grep -rn "ElTable[^>]*\bData=\""` 这种笔误一条 grep 就能扫干净（本仓现已 0 处）。
- **`elementFromPoint` 对视口外坐标恒返回 `null`**：遮罩命中测试第一版拿「English 按钮的中心」去测，
  而那颗按钮在 `.demo-main` 里已被滚出视口（rect 是有效负 y），返回 `null` 被读成"遮罩没盖住"→ **假 FAIL**。
  改成只在 `0 ≤ x < innerWidth`、`0 ≤ y < innerHeight` 且落在面板之外取点（本轮取 5 个视口内点），
  并且**把命中元素一起打出来**，红的时候能立刻分清是"坐标无效"还是"遮挡失效"。
- **被遮罩盖住的元素不要真点击**：Playwright 的可点击性重试会自己 `scrollIntoView`，
  把 `.demo-main` 拽回顶部 ⇒ 目标滚出视口 ⇒ 面板按边界钳制贴在视口底边，`gapY` 变成 −357，
  看着像"不跟随"，其实是测量前提被点击动作改掉了。语言切换这类跨遮罩动作用 DOM 层 `dispatchEvent`，
  遮罩本身改用命中测试验。同理：几何断言要 `targetInView` 门控 —— 目标不在视口内时贴边是**正确行为**，不能当对齐判据。
- **`requestAnimationFrame` 在隐藏标签页一帧都不发**：实测 `visibilityState:hidden` 时 1 秒内 0 帧。
  只做 rAF 的跟随实现（EP `autoUpdate` 的等价写法）在无头 / 后台 / 省电节流场景会冻在旧坐标，
  所以额外挂一层 `setInterval(tick, 100)` 兜底，`tick` 里仍是"只在值变化时写样式"。
  探针据此断言 `panel.__tourTimer` 存在（跟进循环活着），并把这条兜底写进 `docs/tour-alignment.md` §6。
- **演示站是纯 WASM 静态托管，没有后端**：`ElUpload` 默认 `AutoUpload=true`，选完文件真发 POST 到 `/api/upload`
  只会得到 405，刷一条 console error 把"0 JS 异常"那条断言顶红。演示"列表项取词"这类需求时给 `AutoUpload="false"`
  —— 文件进列表就够了，不需要一个不存在的端点。（这不是产品缺陷：EP 的上传示例同样要宿主自备接收端。）
- **断言不能写死 `check(name, true, detail)`**：本轮两条（切英文按钮文字、切回中文 aria-label）实际由前面的
  `waitForFunction` 把关，`check` 自己恒真 —— 一旦上一节的等待条件被改动，那条 PASS 就是空壳。
  统一改成把测到的值再比一遍（`sEn.buttons[0] === 'Previous' && …`），并且**给每条正向断言配一个翻面实验**：
  本批两条翻面（硬编码回 `关闭` → 恰好 2 条红；遮罩 `pointer-events:none` → `allMask` 翻 false）都按预期翻了。
- **`OnInitialized` 里 `??= Lang.T(...)` 是一种"看起来接了线"的假接通**：值被冻在首次语言上，
  切语言后整页都换了、那三个按钮不换。必须渲染期现取；bUnit 里用"面板开着时切语言"钉死（`WaitForAssertion` 只会被**已发生的渲染**唤醒，
  所以夹具里切完要 `Render()` 推一次 —— 见本节第五批那条）。
