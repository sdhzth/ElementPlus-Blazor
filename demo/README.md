# Element-Blazor 组件演示中心（Blazor WebAssembly）

本目录是一个**独立的 Blazor WebAssembly 应用**，把 `Element` 与 `Element.X` 两个组件库中的
**所有组件**集中到一个可运行的程序里演示。凡是需要数据的组件，全部使用本地 **Mock 数据**驱动。

- 组件总数：**147**（`Element` 135 个 `.razor` 组件 + `Element.X` 12 个）
- 分类演示页已手工编写真实用法：**10 个页面**
- 全量自动画廊：`/all` 用反射把两个程序集里的每一个组件都实例化一遍

## 目录结构

```
demo/
├─ Element.Blazor.Demo/
│  ├─ Element.Blazor.Demo.csproj      # net10.0 Blazor WASM 项目，引用 Element / Element.X
│  ├─ Program.cs                      # 注册 AddElementPlusServices() / AddElementPlusX() / HttpClient
│  ├─ App.razor / _Imports.razor      # 路由与应用入口
│  ├─ Layout/MainLayout.razor         # 侧边栏导航（分类 + 搜索 + 当前路由高亮）
│  ├─ Shared/ComponentCatalog.cs      # 反射扫描两个程序集，产出组件目录 / 安全参数
│  ├─ Models/MockData.cs              # 统一 Mock 数据（产品 / 患者 / 组织树 / 订单 / 销量 / 200 行大数据集）
│  ├─ Pages/
│  │  ├─ Home.razor                   # /          首页
│  │  ├─ AllComponents.razor          # /all /cat/{分类} /c/{组件名}：自动画廊
│  │  ├─ BasicDemo.razor              # /basic     基础组件
│  │  ├─ FormDemo.razor               # /form      表单组件
│  │  ├─ Form2Demo.razor              # /form2     表单进阶
│  │  ├─ DataDemo.razor               # /data      数据展示
│  │  ├─ LayoutDemo.razor             # /layout    布局与容器
│  │  ├─ MiscDemo.razor               # /misc      交互与虚拟化
│  │  ├─ FeedbackDemo.razor           # /feedback  反馈组件
│  │  ├─ NavDemo.razor                # /nav       导航组件
│  │  ├─ GrowDemo.razor               # /grow      自研 / Grow 组件
│  │  └─ XDemo.razor                  # /x         AI / X 组件
│  └─ wwwroot/
│     ├─ index.html                   # 引用 _content/Element-Plus 与 _content/Element-Plus.X 静态资源
│     └─ css/app.css                  # 演示壳样式
├─ browser-test.cjs                   # Playwright 路由级端到端自测（12 条路由）
├─ verify-detail.cjs                  # Playwright 明细级断言（DOM 结构 + 交互结果，55 项）
└─ shots/                             # 自测截图
```

## 运行

```bash
cd demo/Element.Blazor.Demo
dotnet run --urls "http://127.0.0.1:5199"
```

浏览器打开 <http://127.0.0.1:5199> 即可。

> 注意：`index.html` 中的 `blazor.webassembly.js` **必须作为普通脚本引入**
> （`<script src="_framework/blazor.webassembly.js"></script>`），
> 不能加 `type="module"`；否则加载器取不到 `document.currentScript`，应用不会挂载。

## 页面说明

| 路由 | 内容 |
|---|---|
| `/` | 首页：项目简介与快捷入口 |
| `/all` | **全部组件**：反射自动发现 `Element` / `Element.X` 中每个组件，用 `DynamicComponent` + `ErrorBoundary` 逐个实例化展示；分块渐进渲染，避免一次性渲染上百组件卡死 |
| `/cat/{分类}` | 某分类下的自动画廊 |
| `/c/{组件名}` | 单个组件的自动渲染 |
| `/basic` | 基础：Button / ButtonGroup / Link / Text / Divider / Space / Row+Col / Layout 容器族 / Icon / Scrollbar / Watermark … |
| `/form` | 表单：Input / InputNumber / Select / Checkbox / Switch / Rate / Slider / FormItem + JSON 快照 |
| `/form2` | 表单进阶：RadioGroup / RadioButton / CheckboxGroup / DatePicker / DateTimePicker / Cascader / InputTag / InputOtp / ColorPicker / Autocomplete / TimePicker / Mention … |
| `/data` | 数据展示：Table / Tree / Tag / Progress / Card / Badge / Avatar / Steps / Timeline / Descriptions |
| `/layout` | 布局与容器：PageHeader / Collapse / Segmented / CheckTag / Carousel / Calendar / Countdown / GroupBox / Splitter / Anchor / Affix / Backtop / ConfigProvider |
| `/misc` | 交互与虚拟化：TableV2 / TreeV2 / TimeSelect / Transfer / Upload / Drawer / Popconfirm / Tour / InfiniteScroll |
| `/feedback` | 反馈：Alert / Tooltip / Popover / Loading / Skeleton / Result / Empty / Dialog |
| `/nav` | 导航：Menu / Tabs / Dropdown / Breadcrumb / Pagination |
| `/grow` | 自研：PivotDataGrid / Chart / Barcode / QRCode / Markdown / Viewer / Dialog |
| `/x` | AI：Welcome / Prompts / Conversations / BubbleList / Bubble / Sender / MentionSender / Thinking / ThoughtChain / Attachments / FilesCard / Typewriter（12 个全含） |

## 组件用法约束（写演示页时踩到的真实坑）

这些是组件库当前的**实际约定**，不是 bug，写 Demo 前先看这里可以少走弯路：

1. **`ElTree` 默认 `DataFormat=List`**：`DataSource` 需要「扁平节点 + 唯一 `Id` + `ParentId`（根为 0）」。
   `Id` 若重复（例如都用默认值 0）会与 `ParentId==0` 的根判定互相牵连，使每个节点把全部节点当成子节点
   而形成环。组件侧已加防环保护（不再栈溢出），但数据仍应保证 `Id` 唯一。
   已提供 `MockData.FlatOrgTree()` 作为标准示例。
   `AutoExpandAll` 默认 `true`，对纯 `DataSource` 绑定同样生效（每个数据源实例只自动展开一次）。
2. **`ElSwitch` 没有 `ChildContent`**：标签用 `ActiveText` / `InactiveText`。
3. **`ElCard` 没有 `ChildContent`**：内容放 `<Body>`，标题放 `<Header>`。
4. **`ElResult` 没有 `ChildContent`**：操作按钮放 `<Extra>`。
5. **`ElDropdown` 的触发器要放 `<Trigger>`**，不是直接塞子元素。
6. **`ElDialog` / `ElDialogGrow` / `ElDrawer` 混用「松散内容 + 命名片段」时要显式写 `<ChildContent>`**，
   否则报 `RZ9996: Unrecognized child content`。
7. **属性值是否按字面量解析取决于参数类型**：
   - `string` 类型可直接写 `Height="320px"`；
   - `int` 类型必须写数值，例如 `ElTreeV2 Height="240"`（**不是** `"240px"`）、
     `ElTableV2 RowHeight="40"`、`ElBacktop VisibilityHeight="200"`。
   - 写错会得到 `CS1003 / CS0103: 不存在名称“px”`。
8. **`ElButton.Size` 是 `ButtonSize`**（Default/Large/Small），与 `ElTag` 的 `TagSize`、
   `ElSegmented` 的 `ElementSize` 不是同一个枚举。
9. **`ElTable` 是非泛型组件**：`<ElTable DataSource="...">` + `<ElTableColumn Property="Name" Text="名称" />`。
10. **`ElTable` / `ElTree` 会注入 `HttpClient`**，WASM 宿主必须注册 `HttpClient`
    （`builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })`）。
11. **`ElTabs` 早期版本在 `@bind-Value` 下会因 `OnParametersSet` 自我触发 `ModelValueChanged` 导致无限渲染**，
    已修复为「仅当值真正变化时才同步激活页」。
12. **`ElTour` 的面板必须带高于遮罩的 `z-index`**，否则遮罩 `inset:0` 会盖住面板导致按钮点不动 ——
    已修复（面板 `PanelStyle` 使用 `z-index: ZIndex`，遮罩为 `ZIndex-1`）。
    另外 `ElTour` **不做 JS 锚定**：面板按 `TourStep.Placement`（top/bottom/left/right）由 CSS 居中或贴边定位，
    不会跟随目标元素的实际屏幕坐标。这是刻意的零 JS 设计取舍。
13. **`ElConfigProvider` 本身不渲染 DOM 包装元素**（仅当需要重写命名空间时才输出 `<span style="display:contents">`），
    它通过 `CascadingValue<ElementConfig>` 生效。断言/选型时不要去找 `.el-config-provider` 容器，
    而应比较子组件尺寸（例如 `Size="ElementSize.Small"` 会让 `ElButton` 高度 32px → 24px）。
14. **`ElAnchorLink` 的链接类名是 `a.el-anchor__link`**，`ElGroupBox` 带标题时输出 `fieldset.blazor-fieldset > legend`，
    `ElCheckTag` 的选中类为 `is-checked` / `is-active`。
15. **`ElTransfer` / `ElUpload` 的默认文案是英文**，需要中文请显式设置
    `EmptyText` / `NoMatchText` / `List1SearchPlaceHolder` / `List2SearchPlaceHolder` / `ToLeftText` / `ToRightText`，
    上传触发器则用 `ChildContent` 覆盖（提供 `ChildContent` 会替换默认 "Upload" 按钮）。
16. **`ElCheckBoxGrow` 已从库中删除**，复选框请用标准 `ElCheckbox` / `ElCheckboxGroup` / `ElCheckboxButton`。

## 端到端自测

```bash
# 1) 启动站点（后台）
cd demo/Element.Blazor.Demo && dotnet run --urls "http://127.0.0.1:5199" &

# 2) 路由级断言：12 条路由逐个加载，等待 h1.demo-page-title 出现即判定渲染成功
NODE_PATH="C:/Users/Administrator/.workbuddy/binaries/node/workspace/node_modules" \
  node demo/browser-test.cjs "http://127.0.0.1:5199" "demo/shots"

# 3) 明细级断言：组件真实的 DOM 结构 + 交互结果（折叠展开、分段二绑、锚点点击、
#    ConfigProvider 级联尺寸、Tour 点「下一步」是否真的翻页、Popconfirm 确认回调、
#    Drawer 开关可见性、虚拟表格行数……）
NODE_PATH="C:/Users/Administrator/.workbuddy/binaries/node/workspace/node_modules" \
  node demo/verify-detail.cjs "http://127.0.0.1:5199" "demo/shots"
```

`browser-test.cjs` 收集控制台错误、截图到 `shots/`（失败页存为 `*_FAIL.png`），结果写入 `test-result.json`。
`verify-detail.cjs` 结果写入 `detail-result.json`。两个脚本都**只依赖本机 Edge**（`--no-sandbox` headless），无需下载 Chromium。

当前结果：

| 脚本 | 结果 |
|---|---|
| `browser-test.cjs` | **12 / 12 路由渲染成功，控制台错误 0** |
| `verify-detail.cjs` | **55 / 55 明细断言通过，控制台错误 0** |

> 注意：`verify-detail.cjs` 里 Tour / Popconfirm / Drawer 三段交互各自使用**全新的页面加载**。
> 因为抽屉打开后遮罩会覆盖整页，若在同一页面里继续点别的按钮，会因遮罩拦截而误判成「组件坏了」——
> 这是自测脚本自身的陷阱，不是组件问题。

### 通过自测发现并修复的组件缺陷

| 组件 | 缺陷 | 修复 |
|---|---|---|
| `ElTour` | 遮罩 `.el-tour__mask` 为 `inset:0` 且 `z-index:ZIndex-1`，面板 `.el-tour__content` 无 `z-index`，遮罩盖住面板 → **所有按钮点不动**，点击还会穿透到遮罩把引导关掉 | 面板新增 `PanelStyle`，显式 `z-index: ZIndex`（同时接管原内联 `padding`） |
| `ElTree` | `AutoExpandAll`（默认 `true`）只在 `ExpandAsync` / `AddChild` 内触发，**纯 `DataSource` 绑定不展开任何节点** | `OnParametersSet` 中按数据源实例只自动展开一次（`autoExpandedSource` 去重） |
| `ElTree` | 同时设置 `DefaultExpandedKeys` / `DefaultCheckedKeys` 与 `DataSource` 时，遍历 `items` 发生在 `items = DataSource` 之前 → **`NullReferenceException`** | 先赋值 `items`，仅在确实无数据时提前返回 |
| `ElTree` | `DataFormat=List` 且节点 `Id` 重复（或等于 `ParentId`）时节点互为子节点成环 → **`StackOverflowException` 整页崩溃** | `Initilize` 与渲染路径 `BuildTreeNode` 增加 visited 防环保护 |
| `ElTabs` | `@bind-Value` 下 `OnParametersSet` 自我触发 `ModelValueChanged` → **无限渲染卡死** | 仅在值真正变化时同步激活页 |
