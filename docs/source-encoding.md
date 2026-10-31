# 源码编码：仓库只允许 UTF-8，不允许 GBK

> 触发：2026-10-08 用户指出 `src/Components/ControlRenders/TableRender.cs` 是 GBK 存盘，要求"查看所有的文件，都不要 GBK 存盘"。
> 本页记录：为什么这件事能潜伏这么久（编译器不报错）、全仓实测有多少文件坏了、三类损坏各自的修法、以及新加的第七常驻守卫。
> 结果：**30 个纯 GBK 文件无损转成 UTF-8（无 BOM）＋ 1 个文档注释快照补回 12 个坏字 ＋ 9 行已损坏注释重写**，
> 全仓文本文件现在 **0 个** 不是严格 UTF-8；判据由 `SourceEncodingGuardTests`（4 例）钉住。
> 基线：**全量 bUnit 2281 通过 / 0 失败 / 0 跳过**（2277 + 本批 4 条守卫；收尾连跑三次读数一致）。
> 守卫自己也被抓到一次工具层缺陷（只读扫描用 `File.OpenRead` 撞上"别人正在写"），见 §4.1。

## 0. 一句话契约

**仓库里任何文本文件都必须以 UTF-8 存盘。** 中文 Windows 上"能编译、运行也正常"不能当验收依据 ——
缺陷在这一层是完全静默的（见 §1）。新文件用编辑器写中文前先确认右下角是 `UTF-8`。

## 1. 为什么以前没人发现：Roslyn 的 ANSI 兜底

C# 编译器读**没有 BOM** 的源文件时：

1. 先按 UTF-8 解；
2. 解不动 → **退回系统 ANSI 代码页**（本机 ACP = 936 / GBK）再解一遍。

所以 GBK 存盘的中文注释、中文字符串字面量，在中文 Windows 上**编译 0 错误**，运行时的字符串也是对的。
实测硬证据（转换后重新构建，比对 `src/Components/bin/Debug/net10.0/Element.dll` 里的 UTF-16 字面量）：

| 字符串（UTF-16LE 检索） | DLL 命中次数 |
| --- | --- |
| `确认删除吗` | 1 |
| `要置为 Loading 状态的组件尚未渲染完成` | 2 |

也就是说：**GBK 存盘在这台机器上不是"坏"，而是"只在别的机器上坏"** ——
换代码页的机器、Git 托管站的网页视图、任何按 UTF-8 读文件的工具（grep、编辑器、`DocumentationFile` 生成的注释 XML）
都会看到乱码。这跟浮层归属那两件事是同一类缺陷：**没有异常、没有日志，只能靠机器判据**。

顺带一条同类静默：`dotnet test --no-build --filter <匹配不到任何用例>` **返回 0**（只在 stdout 写"总共 1 个测试文件与指定模式相匹配"）。
本批就差点被它糊过去 —— 编译失败的那次运行 RC 也是 0。跑测试必须读"通过/失败/总计"那一行，不能只看退出码。

## 2. 全仓实测：损坏分三类

扫描口径：`git ls-files` 全部 1489 个跟踪文件，先按 NUL 字节剔掉 97 个二进制，剩 1392 个文本文件按严格 UTF-8 解码。
**失败 31 个**。另有 5 个文件"是合法 UTF-8 但内含 U+FFFD"（C 类，见 §4 为什么要单独一条判据）：
3 个代码文件（下表）、`src/Components/wwwroot/js/ofd/ofd.umd.min.js`（1 处，vendor，见 §5）、
`test/Element.ComponentTests/baseline.log`（**2006 处** —— 那是把 GBK 控制台输出当 UTF-8 抓下来的运行日志快照，
不是源码）。这两份都不是源码，收尾按用户指示**已删除**（取回命令见 §6）。

| 类别 | 判据 | 数量 | 能不能无损救 | 修法 |
| --- | --- | --- | --- | --- |
| **A. 纯 GBK 存盘** | 严格 UTF-8 解码失败，且整份文件能按 GBK 解码 | **30** | ✅ 能 | `bytes.decode('gbk').encode('utf-8')`，写回前断言 `text.encode('gbk') == 原字节`（不等就中止，防有损） |
| **B. UTF-8 但汉字第三字节被写成 `?`（0x3F）** | 严格 UTF-8 解码失败在 `[E0-EF][80-BF]3F` 形态 | **1 个文件 12 个字**（`src/Components/Element.xml`） | ✅ 能（每个都能从短语唯一确定） | 按 UTF-8 序列补回第三字节（补完之后整份过期快照照 §3 删除） |
| **C. UTF-8 里嵌 U+FFFD** | 严格 UTF-8 能解，但出现 `EF BF BD` | **3 个代码文件 9 行** ＋ 1 个 vendor JS | ❌ 不能（字符已经丢了） | 照上下文重写注释 |

A 类分布：`src/Components` 16 个（含后来单独处理的 `Element.xml`）、`src/Markdown` 14 个、`test/TabTest.cs` 1 个。
逐份字节增量见转换日志（例：`ButtonBase.cs 4712 → 4767`，中文从 2 字节变 3 字节，符合预期）。

**为什么 A 类清单里不能只看"UTF-8 解码失败"**：一个 GBK 文件恰好也是合法 UTF-8 的概率极低，但不是零；
而 C 类（U+FFFD）**本来就是合法 UTF-8**，所以守卫必须是两条判据（§4），只有一条会漏掉已经损坏的那批。

### B 类补回的 12 个字

`始`（动画开～／从1开～，3 处）、`交`（触发浏览器提～）、`值`（获取表单输入～／自己的～／选择的～，3 处）、
`身`（弹出层元素本～）、`素`（遮罩层元～）、`态`（隐藏／显示动画状～，2 处）、`纹`（启用斑马～）。

> 这里踩过一次判断错误：`弹出层元素本?` 我一开始按"本体"补，但坏字节是 `E8 BA 3F` → 码点区间 U+8E80–U+8EBF，
> `体`(U+4F53) 根本不在这个区间，正确字是 **身**(U+8EAB)。补完必须"严格 UTF-8 通过"才算证完，
> 光看短语通顺会补错。`src/Components/PopupOption.cs` 的那行注释也据此从"本体"改回"本身"。

### C 类重写的 9 行

- `src/Components/PopupOption.cs`（6 行）：关联到的 ElPopup 组件实例／弹出层元素本身／遮罩层元素／要将弹出层显示到哪个元素下方／隐藏动画状态／显示动画状态
- `src/Components/ElDynamicComponent.cs`（2 行）：提供动态组件渲染的功能／渲染配置
- `src/Components/Extension.cs`（1 行）：注册 Element 相关服务

C 类的来源是"用 Edit 工具改一份 GBK 文件"：工具按 UTF-8 读、把非法字节替换成 U+FFFD 再写回，
中文就永久丢了。**所以改 GBK 存盘的文件不能走 Edit，必须先整份转 UTF-8**（本批之前 `Extension.cs` 就被这样坏过一次，
旧文档里"Extension.cs 是 GBK，要按字节补"的记载其实已经过期：它那时是合法 UTF-8 ＋ 3 行 U+FFFD）。

## 3. `src/Components/Element.xml` 是什么

它**不是**构建产物：`Element.csproj:38` 的 `DocumentationFile` 指向 `obj\Debug\net10.0\Element.xml`（实测 1 045 159 字节），
而仓库根的这份只有 4295 字节、25 个 `<member>`，是基线提交（`157dde6`）里带进来的一份**过期快照**
（其中 `BFormBase.Validations`、`Select.OptionModel`1` 等成员在当前源码里已经不存在）。
本批先只把它的编码补成严格 UTF-8（守卫要扫它），**没动内容**；删除跟踪文件属于不可逆动作，所以等到用户确认之后才整份删掉 —— 它与源码已经脱节，留着只会给出错的注释。

```bash
# 取回原件（基线提交里那份；Element.xml 这份是补回 12 个字之前的原始字节）
git show 157dde6:src/Components/Element.xml > src/Components/Element.xml
git show 157dde6:test/Element.ComponentTests/baseline.log > test/Element.ComponentTests/baseline.log
```

## 4. 第七常驻守卫：`SourceEncodingGuardTests`（4 例）

| 用例 | 判据 | 为什么 |
| --- | --- | --- |
| `EveryTextFileDecodesAsStrictUtf8` | `src/ demo/ test/ docs/`（递归）＋ **仓库根顶层**（`CHANGELOG.md`、`README*.md` 这些每批都要写中文的地方）里所有文本扩展名文件（跳过 `bin/obj/publish/node_modules`、跳过含 NUL 的文件）必须被 `new UTF8Encoding(false, true)` 解出来 | 挡 A 类：GBK 存盘 |
| `CodeFilesContainNoReplacementCharacter` | `src/**/*.cs`、`*.razor` 不得出现 `EF BF BD` | 挡 C 类：U+FFFD 本身合法，上一条查不到 |
| `StrictDecoderRejectsGbkButAcceptsUtf8` | 双侧夹具：`{BF C6 C8 CF}`（"确认"的 GBK 字节）必须抛 `DecoderFallbackException`；同词 UTF-8 字节必须解出"确认"；**并断言宽松解码不抛、只产生替换符** | 掩码/判据自身失效就会假绿；最后半句就是 §1 那条静默的机器版本 |
| `ScanCoversTheWholeSourceTree` | 扫描数 ≥700（2026-10-09 实测 **1595**：src 762／demo 574／test 152／docs 94／仓库根顶层 13。取数手法：把下限临时抬到 999999 跑一次，失败消息里的 `files.Count` 才是真身，读完还原）且必须包含 `src/Components/ControlRenders/TableRender.cs`、`src/Components/Extension.cs`、`src/Components/Element.csproj`、`demo/Element.Blazor.Demo/App.razor`、守卫自己、以及根层的 `CHANGELOG.md`（根层那条通道要真的通） | 覆盖断言："0 违规"不能来自"什么都没扫" |

### 4.1 写这类"扫工作树"的守卫会踩到的一条坑：只读打开必须容许别人在写

第一次把仓库根顶层纳入扫描范围后，两条用例**每次都红**，报的还不是编码问题：

```
System.IO.IOException : The process cannot access the file
'E:\电子病历\Element-Blazor-master\_x_loop.txt' because it is being used by another process.
```

真身不是文件被删、也不是权限：`File.OpenRead` 的共享模式是 **`FileShare.Read`** ——
它声明的是"别人只许读"，所以任何一份**正被别的进程开着写**的文件都会让它抛异常（Windows 的共享检查是双向的：
新开的 share 必须容得下已存在句柄的 access）。而跑测试时"另有一个进程正在往工作树里写报告"是常态
（本仓的探针会把结果写进 `demo/tools/probes/*-result.txt`，脚本也常开着输出文件），
所以这不是偶发，是**确定性**的：同一场景连跑 6 次 → 6 次 `失败数: 2`。

修法是把只读扫描器按它真正的意图打开文件（`OpenShared` / `ReadAllBytesShared` 两个小助手，本文件同目录的测试里）：

```csharp
new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
```

同一场景改完连跑 **6 次 RC=0**，全量 **2281 通过 / 0 失败**（×3 次读数一致）。
和 §1 那两条静默同理：**"扫描器写坏"和"扫描器抓到违规"长得一样（都是红）**，
所以先确认异常类型再改判据 —— 这次是 `IOException` 而不是 `DecoderFallbackException`，一眼就能分清是工具坏了还是仓库坏了。

**red-before（实测）**：临时往 `src/Components/` 放两份夹具文件 ——
一份按 GBK 存"确认删除吗"、一份含两个 U+FFFD —— 跑本类立刻 **失败数 2**，报错信息带文件名与字节偏移：

```
src/Components/_TempGbkProbe.cs @byte 65：Unable to translate bytes [C8] at index 65 from specified code page to Unicode.
src\Components\_TempFffdProbe.cs @byte 64
```

撤掉两份夹具后 4 例全绿。反向验证照 §1 配方第 4 步做，夹具文件用完即删。

### BOM 口径

转换时统一写成 **UTF-8 无 BOM**，依据是当时实测的仓库既有分布：`src/` 的 `.cs`/`.razor` 共 **734** 个，
带 BOM **111** 个、不带 **623** 个 → 无 BOM 是多数。守卫**不检查 BOM**（两种都合法，Roslyn 都能正确读），
避免把 153 个既有 BOM 文件拖进无谓的改动。

## 5. vendor 文件例外说明

`src/Components/wwwroot/js/ofd/ofd.umd.min.js`（第三方 OFD viewer 压缩产物）含 1 个 U+FFFD 与一段 latin-1 痕迹，
但**是合法 UTF-8**，且 A/B 两类判据里 U+FFFD 那条只扫 `src/**` 的 `.cs`/`.razor`，所以它不会被守卫拦下。
理由：改动 vendor 产物会让"能不能重新拉一份上游文件"这件事变得不可能核对。

## 6. 明确没做的事

- （已办）`src/Components/Element.xml`（§3 的过期注释快照）与 `test/Element.ComponentTests/baseline.log`（225 KB、2006 处替换符的
  控制台日志快照，中文在源头已经塌成 `?`/U+FFFD，不可逆，且内容随时能由 `dotnet test` 重新产生）两份历史遗留件已按用户指示删除；
  没有重新生成 `Element.xml` 那份注释快照。取回命令见 §3。
- 没统一去 BOM／加 BOM（§4 BOM 口径）。
- 没动 vendor JS（§5）。
- 没加 `.gitattributes`（`* text=auto working-dir-encoding` 之类）—— 改 git 配置/属性文件属于会影响全局的动作，
  且当前 `core.autocrlf` 已经在产生 LF→CRLF 警告，先只在守卫层拦编码，不碰换行。
- 只扫了 `src/ demo/ test/ docs/`（递归）＋ **仓库根顶层文件**（`CHANGELOG.md`、`README*.md` 等，`TopDirectoryOnly`，
  实测 13 个）；根目录的<em>子目录</em>里不属于这四个根的散落文件（例如 `_backup_*`、IDE 生成的临时目录）不在常驻口径内，
  本批由 §2 那次 `git ls-files` 全量核对过：1392 个文本文件现在 **0 个**非严格 UTF-8。
