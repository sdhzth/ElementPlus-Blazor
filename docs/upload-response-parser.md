# ElUpload · `ResponseParser`：让上传组件真能直连业务后端（批次㊼）

宿主 `zyweb` 的原话是「**图片页签上传**」，选定方案 ①「改库让 ElUpload 真能直连后端」。
在此之前那条路走不通，原因不在 WASM 拿不到文件字节（XHR 本来就能直接把 `File` 塞进 `FormData`），
而在响应处理这一环——三处各自独立致命，实测都在同一台浏览器上复现过。

## ① 响应键写死 `code`，本后端返回 `result` ⇒ Promise 永不 resolve

旧 `dom.js` 的 `_uploadFile` 在 `onreadystatechange` 里做的是：

```js
let response = JSON.parse(this.responseText);
callback([response.code.toString(), response.message, response.id, response.url]);
```

后端返回 `{ "result": "0", "message": …, "fcode": …, "u_imgurl": … }` 时 `response.code` 是 `undefined`，
`.toString()` 抛 `TypeError`。这个异常落在 `onreadystatechange` 里，**没有人 `resolve` 那个 Promise**，
于是 C# 侧的 `await input.UploadFileAsync(...)` 永久挂起：

- 界面表现：文件项一直停在「上传中」转圈，既不变红也不变绿；
- 控制台表现：什么都没留（异常被 `onreadystatechange` 吃掉，不冒泡到 `window.onerror` 的可见路径）；
- .NET 表现：`OnSuccess` / `OnError` 一个都不触发，宿主无从补救。

**实测证据**（`_tmp_probe/b53_upload.mjs`，把 HEAD 版那两行响应处理原样贴进页面跑一次）：

```
改前对照（HEAD 版响应处理原样跑）=
  {"settled":false,"outcome":"TIMEOUT",
   "err":"Uncaught TypeError: Cannot read properties of undefined (reading 'toString')","cb":null}
```

`settled:false` + 6 秒 `TIMEOUT` 就是「挂死」本体，`err` 就是它的原因。这一条是**有牙的改前对照**：
同一段代码、同一个 mock、同一个浏览器，改前挂死、改后立刻出结果。

## 改法：把「怎么算成功」交给宿主

```csharp
/// <summary>接口返回的原始响应体（JSON 文本）→ 成/败 + id/url/message。</summary>
[Parameter]
public Func<string, UploadRequestResult> ResponseParser { get; set; }
```

- 不传 = 行为与旧版一致（内置 `{ code, message, id, url }` 映射），**向后兼容**，库自己的演示页不受影响；
- 传了 = 原始响应体整个交给宿主，业务键再多也不用回来加参数；
- 无论走哪条，`UploadRequestResult.RawResponse` 都带着原始响应体，宿主可以在 `OnSuccess` 里二次解析。

同时补齐的失败面（旧版只在「有 `code` 键」时才给出结果）：

| 响应 | 旧行为 | 新行为 |
|------|--------|--------|
| 非 JSON（网关返回 HTML / 纯文本） | `JSON.parse` 抛 ⇒ 同样挂死 | 判失败，`message = "Upload response is not valid JSON."`，`RawResponse` 保留原文 |
| 是 JSON 但没有 `code` 键 | `.toString()` 抛 ⇒ 挂死 | 判失败，`message = 'Upload response has no "code" field.'` |
| HTTP 状态非 2xx | 走 `onerror`/状态分支 | 判失败，`message = statusText` |

**实测证据**（同一台浏览器，`_tmp_probe/b53_upload_run5.log` 全 14 条 PASS）：

```
PASS A：两个文件各自上传成功、id 对得上（result 键由宿主映射） :: A-success 行数=2
    A-success file=f1.png id=mock-19 url=/images/empty.svg code=0 msg=mock：图片已接收 raw=178
PASS B：内置 code 键映射照旧成功（向后兼容对照） :: B-success … id=ep-21 … raw=66
PASS C：响应不是 JSON 时立刻判失败，不再一直转圈 :: C-error … msg=Upload response is not valid JSON. raw=15
PASS C：列表项落在 is-fail 状态 :: el-upload-list__item is-fail
```

A 用 `ResponseParser` 读 `result` 键（和款号编辑里同一份写法），B 不传映射读 EP 那一族键（正向对照，证明没改坏默认行为），
C 指向返回 `not json at all` 的地址——**改前这一台就是无限转圈，改后 4 秒内落 `is-fail`**。

## ② `x-requested-with` 无条件加 ⇒ 跨源 multipart POST 触发预检

旧实现无条件 `xhr.setRequestHeader('x-requested-with','XMLHttpRequest')`。这个头不属于「简单请求」白名单，
一旦加上，浏览器就要先对后端发一次 OPTIONS；后端没放通就直接上传失败。
而旧版那条 .NET `HttpClient` 的路不发这个头，所以它反而能传——这正是「换组件就传不上」的真实原因之一。

改法：只在**与页面同源**时才发（`isSameOriginUrl()` 比 `protocol + host`），跨源不发。
同源那半保留原样，是因为这类头通常给服务端的防伪过滤器用，跨源用不上它。

**实测证据**（CDP `Network.requestWillBeSent` 直接看发出的请求头，非服务端推断）：

```
PASS 跨源：全部是简单请求（无 OPTIONS 预检、POST 不带 x-requested-with） :: 跨源发数=9 预检=0 带头的 POST=0
PASS 同源正向对照：仍然带 x-requested-with，且请求发得出去
    [{"method":"POST","port":"5171","xrw":"XMLHttpRequest","u":"http://localhost:5171/probe-upload?elupload-same-origin=noop"}]
```

mock 服务侧同步计数：`UPLOAD 条数=35  PREFLIGHT 条数=0`。
跨源 9 发全是 `POST`、`x-requested-with: null`；同源那一发 `x-requested-with: XMLHttpRequest` 仍在——
**这条同源断言是负向断言的正向对照**，缺了它「跨源没带头」与「这段代码压根没跑」在日志里长得一样。

## ③ multipart 形状没变

送法与原 `HttpClient` 那条一致，后端不用改。mock 侧记到的分片：

```
样例分片: {"path":"/upload/zyweb","contentType":"multipart/form-data","xRequestedWith":null,"cookie":"none",
           "parts":[{"name":"jparams","filename":null,"bytes":17},
                    {"name":"file","filename":"oldrepro.png","bytes":70}]}
所有分片形状: ["/upload/zyweb|jparams|value","/upload/zyweb|file|file","/upload/ep|file|file","/upload/badjson|file|file"]
```

`Data` 里的每一项都是普通文本分片（`jparams`），文件分片带 `filename`，字段名取自 `FileFieldName`。

## 验收覆盖边界（诚实记录）

- 这一批的运行时验收全部在本机 mock 上传服务（`_tmp_probe/mockupload.mjs`，127.0.0.1:5399）+ 临时探针页
  `/probe-upload` 上完成。验收会话的 `unitsurl` 为空（不手动塞业务地址），
  所以**真页面（款号编辑 · 图片页签）只验到「地址为空时整块不渲染、不抛 `Upload action is required.`、页面不崩」**，
  真后端那一次往返没跑过——机制层已由 mock 覆盖，缺的是宿主接线本身。
- 探针页验收后已删除，不在仓库里留路由。
- 未覆盖：`OnProgress` 的真实百分比（XHR 这层没接 `upload.onprogress`，仍是 0 → 100 两档，本批未改，行为与旧版一致）。
