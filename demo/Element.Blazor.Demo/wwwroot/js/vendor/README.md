# 站内第三方库（vendor）

演示站把「接入外部 MSE 库」这一节示例的库地址从 CDN 换成了本目录，目的是让**内网 / 非 HTTPS 部署也能跑通**：
`cdn.jsdelivr.net` 在不少内网环境里会被解析到 `127.0.0.1`，页面又是 `http://`（非安全上下文），
Chromium 的 Private Network Access 会直接拦掉，控制台报
`... has been blocked by CORS policy: The request client is not a secure context and the resource is in more-private address space loopback`。

## hls.min.js

| 项 | 值 |
| --- | --- |
| 包 | `hls.js` |
| 版本 | **1.6.15**（固定版本；原来示例写的 `hls.js@1` 会随 npm `latest` 漂，现已漂到 1.7.3） |
| 许可 | Apache License 2.0（上游 `LICENSE` 原文随文件放在本目录 `LICENSE-hls.js.txt`） |
| 取处 | `https://registry.npmjs.org/hls.js/-/hls.js-1.6.15.tgz` 里的 `package/dist/hls.min.js` |
| 字节数 | 541,870 |
| MD5 | `69991b6ecc5d57cf73bad5b3a1296058`（**与官方 tarball 内那份逐字节一致，未做任何改动**） |
| 源映射 | `hls.min.js.map`，2,383,712 字节，MD5 `2579fb128c905163dd84662c7b48c8a5`（同样逐字节一致；只有 DevTools 打开时才会被请求） |

复核命令（在本目录）：

```
curl -sL -o /tmp/hls.tgz https://registry.npmjs.org/hls.js/-/hls.js-1.6.15.tgz
tar -xzf /tmp/hls.tgz package/dist/hls.min.js -O | md5sum   # 应为 69991b6ecc5d57cf73bad5b3a1296058
```

## 谁在用

`Pages/VideoPlayerGrowDemo.razor` 第 7 节：`<ElVideoPlayerGrow ... HlsLibUrl="js/vendor/hls.min.js" />`。
路径相对 `<base href="/" />` 解析，因此子路径部署同样可用。组件本身**零第三方播放器依赖**，
只有在这种显式给出 `HlsLibUrl` / `DashLibUrl` / `FlvLibUrl` 的示例里才会加载外部脚本。
