using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 自渲染视频播放器（零第三方播放器依赖，按 Element Plus 观感渲染）。
    /// <para>
    /// 能力覆盖：① 网络地址（<see cref="Src"/> 或 <see cref="Sources"/>）；
    /// ② 流媒体（HLS <c>.m3u8</c> 走浏览器原生 HLS，Safari / Chrome 134+ 直接播放；
    /// DASH <c>.mpd</c> / FLV <c>.flv</c> 可通过 <see cref="HlsLibUrl"/> / <see cref="DashLibUrl"/> / <see cref="FlvLibUrl"/> 显式接入 hls.js / dash.js / flv.js）；
    /// ③ 多种格式（mp4 / webm / ogg / mov / m4v / avi / wmv / ts / mkv 等，按扩展名自动推断 MIME，并支持多 &lt;source&gt; 回退）；
    /// ④ 自定义控制条（播放/暂停、<see cref="Loop"/> 循环切换、时间、可拖拽进度、静音开关、音量、倍速、画中画、全屏）。
    /// 未提供外部库时本组件保持零第三方依赖。
    /// </para>
    /// <para>
    /// 所有 DOM 行为通过 <c>wwwroot/js/elvideoplayergrow.js</c> 完成；媒体状态由 JS 监听后回传 .NET，
    /// 控制动作（播放/跳转/音量/全屏/PiP）也由 JS 通道下发，逻辑均自研、不引用任何第三方 UI 组件库。
    /// </para>
    /// </summary>
    public partial class ElVideoPlayerGrow : ElementComponentBase, IDisposable, IAsyncDisposable
    {
        /// <summary>单个视频源（URL + 可选 MIME 类型 + 展示名）。</summary>
        public sealed class VideoSource
        {
            /// <summary>视频地址（网络 URL 或相对路径）。</summary>
            public string Url { get; set; }

            /// <summary>MIME 类型；为空时按扩展名自动推断。</summary>
            public string Type { get; set; }

            /// <summary>展示名（可选，多源切换时用于提示）。</summary>
            public string Label { get; set; }
        }

        /// <summary>流/封装类型提示（仅用于能力探测与提示，不改变渲染逻辑；渲染始终走原生 &lt;video&gt;）。</summary>
        public enum ElVideoStreamType
        {
            /// <summary>自动识别（按扩展名/MIME）。</summary>
            Auto,

            /// <summary>MP4（H.264）。</summary>
            Mp4,

            /// <summary>WebM（VP8/VP9）。</summary>
            Webm,

            /// <summary>Ogg/Theora。</summary>
            Ogg,

            /// <summary>HLS 直播/点播（.m3u8，浏览器原生支持）。</summary>
            Hls,

            /// <summary>FLV（需要 flv.js 等 MSE 库，本组件零依赖未内置）。</summary>
            Flv,

            /// <summary>DASH（.mpd，需要 dash.js 等 MSE 库，本组件零依赖未内置）。</summary>
            Dash,

            /// <summary>QuickTime（.mov）。</summary>
            Mov
        }

        /// <summary>单一视频地址；与 <see cref="Sources"/> 二选一，二者都给时以 <see cref="Sources"/> 优先。</summary>
        [Parameter] public string Src { get; set; }

        /// <summary>多视频源（用于不同格式回退）。为空时回退到 <see cref="Src"/>。</summary>
        [Parameter] public IEnumerable<VideoSource> Sources { get; set; }

        /// <summary>封面图 URL。</summary>
        [Parameter] public string Poster { get; set; }

        /// <summary>自动播放（浏览器通常要求同时 <see cref="Muted"/> 才会真正自动播放）。</summary>
        [Parameter] public bool AutoPlay { get; set; }

        /// <summary>循环播放。</summary>
        [Parameter] public bool Loop { get; set; }

        /// <summary>静音。</summary>
        [Parameter] public bool Muted { get; set; }

        /// <summary>是否显示自定义控制条（默认 true）。</summary>
        [Parameter] public bool Controls { get; set; } = true;

        /// <summary>
        /// 是否「平时隐藏控制条、鼠标移上去才显示」（默认 false，保持既有行为）。
        /// 打开后：播放中鼠标静止超过 <see cref="ControlsHideDelay"/> 毫秒，控制条淡出、
        /// 鼠标指针一并隐藏，画面更干净；鼠标移入播放器或产生移动 / 点击立即恢复显示。
        /// 暂停时始终保持可见，避免用户找不到播放按钮。
        /// </summary>
        [Parameter] public bool AutoHideControls { get; set; }

        /// <summary>自动隐藏控制条前的静止时长（毫秒，默认 3000），仅在 <see cref="AutoHideControls"/> 为 true 时生效。</summary>
        [Parameter] public int ControlsHideDelay { get; set; } = 3000;

        /// <summary>容器宽度（CSS 值，默认 100%）。</summary>
        [Parameter] public string Width { get; set; } = "100%";

        /// <summary>播放区高度（CSS 值，默认 360px）。</summary>
        [Parameter] public string Height { get; set; } = "360px";

        /// <summary>流/封装类型提示（仅影响能力探测与提示文案）。</summary>
        [Parameter] public ElVideoStreamType StreamType { get; set; } = ElVideoStreamType.Auto;

        /// <summary>
        /// 外部 HLS 库（如 hls.js）地址。当显式设置 <see cref="StreamType"/>=<see cref="ElVideoStreamType.Hls"/>
        /// 且提供本地址时，组件将改用该库（而非依赖浏览器原生 HLS）接入 .m3u8，便于适配内网/老旧浏览器。
        /// 留空则维持零依赖原生播放（Safari / Chrome 134+）。
        /// </summary>
        [Parameter] public string HlsLibUrl { get; set; }

        /// <summary>外部 DASH 库（如 dash.js）地址；配合 <see cref="StreamType"/>=<see cref="ElVideoStreamType.Dash"/> 使用。</summary>
        [Parameter] public string DashLibUrl { get; set; }

        /// <summary>外部 FLV 库（如 flv.js）地址；配合 <see cref="StreamType"/>=<see cref="ElVideoStreamType.Flv"/> 使用。</summary>
        [Parameter] public string FlvLibUrl { get; set; }

        /// <summary>可选倍速列表（默认 0.5/1/1.25/1.5/2）。</summary>
        [Parameter] public double[] PlaybackRates { get; set; } = { 0.5, 1.0, 1.25, 1.5, 2.0 };

        /// <summary>是否显示倍速菜单（默认 true）。</summary>
        [Parameter] public bool ShowPlaybackRate { get; set; } = true;

        /// <summary>是否显示音量控制（默认 true）。</summary>
        [Parameter] public bool ShowVolume { get; set; } = true;

        /// <summary>是否显示全屏按钮（默认 true）。</summary>
        [Parameter] public bool ShowFullscreen { get; set; } = true;

        /// <summary>是否显示画中画按钮（默认 true）。</summary>
        [Parameter] public bool ShowPiP { get; set; } = true;

        /// <summary>是否显示循环播放按钮（默认 true）。</summary>
        [Parameter] public bool ShowLoop { get; set; } = true;

        /// <summary>自定义错误提示；为空时由播放器自动生成。</summary>
        [Parameter] public string ErrorText { get; set; }

        [Parameter] public EventCallback OnPlay { get; set; }
        [Parameter] public EventCallback OnPause { get; set; }
        [Parameter] public EventCallback OnEnded { get; set; }
        [Parameter] public EventCallback<double> OnTimeUpdate { get; set; }
        [Parameter] public EventCallback OnLoadedMetadata { get; set; }
        [Parameter] public EventCallback<string> OnError { get; set; }

        private string _uid;
        private List<VideoSource> _resolvedSources = new List<VideoSource>();
        private bool _isHls;
        private bool _attached;
        private bool _loadedFired;
        private bool _rateMenuOpen;
        private IJSObjectReference _jsModule;
        private DotNetObjectReference<ElVideoPlayerGrow> _dotNetRef;
        private bool _released;

        private ElementReference _videoRef;

        // 实时状态（由 JS 回传）
        private bool _isPlaying;
        private double _currentTime;
        private double _duration;
        private double _buffered;
        private double _volume = 1;
        private bool _isMuted;
        private double _playbackRate = 1;
        private bool _isFullscreen;
        private bool _isPiP;
        private bool _isWaiting;
        private string _errorMessage = string.Empty;
        private bool _hlsUnsupported;
        private bool _loop;
        private bool _useMseLib;
        private bool _manualSrc;

        /// <summary>已下发给 JS 的自动隐藏配置（null 表示还没下发过），用于幂等同步。</summary>
        private bool? _autoHideApplied;
        private int? _hideDelayApplied;

        private bool IsPlaying => _isPlaying;
        private bool IsMuted => _isMuted;
        private bool IsFullscreen => _isFullscreen;
        private bool IsPiP => _isPiP;
        private bool IsWaiting => _isWaiting;
        private double Volume => _volume;
        private double PlaybackRate => _playbackRate;
        private double CurrentTime => _currentTime;
        private double Duration => _duration;
        private double Buffered => _buffered;
        private string ErrorMessage => string.IsNullOrEmpty(ErrorText) ? _errorMessage : ErrorText;
        private bool HlsUnsupported => _hlsUnsupported;

        private double PlayedPercent => _duration > 0 ? Math.Min(100, _currentTime / _duration * 100) : 0;
        private double BufferedPercent => _duration > 0 ? Math.Min(100, _buffered / _duration * 100) : 0;
        private double VolumePercent => Math.Round(_volume * 100);

        private string ContainerClass => "el-video-player-grow" + (_isFullscreen ? " is-fullscreen" : "");

        /// <summary>
        /// 容器内联尺寸。
        /// <para>
        /// ⚠️ 全屏时必须**不输出**内联 width —— 内联样式优先级高于任何选择器，
        /// 一旦无条件写上 <c>width:800px</c>，就会盖掉 <c>.el-video-player-grow.is-fullscreen{width:100vw}</c>，
        /// 全屏后播放器还是原来那么宽。
        /// </para>
        /// </summary>
        private string ContainerStyle => _isFullscreen || string.IsNullOrWhiteSpace(Width) ? null : $"width:{Width};";

        /// <summary>
        /// 播放区内联高度。同理：全屏时不能输出内联 height，否则
        /// <c>.el-video-player-grow.is-fullscreen .el-video-player-grow__body{height:100%}</c> 会被内联
        /// <c>height:360px</c> 盖掉 —— 这正是「点了全屏按钮，画面并没有变大」的根因。
        /// </summary>
        private string BodyStyle => _isFullscreen || string.IsNullOrWhiteSpace(Height) ? null : $"height:{Height};";

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();
            _uid = "elvpg-" + Guid.NewGuid().ToString("N");

            if (Sources != null && Sources.Any())
                _resolvedSources = Sources.Select(NormalizeSource).ToList();
            else if (!string.IsNullOrEmpty(Src))
                _resolvedSources = new List<VideoSource> { NormalizeSource(new VideoSource { Url = Src }) };

            _isHls = _resolvedSources.Any(s =>
                string.Equals(s.Type, "application/x-mpegURL", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s.Type, "application/vnd.apple.mpegurl", StringComparison.OrdinalIgnoreCase) ||
                (s.Url != null && s.Url.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)));

            _loop = Loop;
            _useMseLib = !string.IsNullOrEmpty(ResolveMseKind()) && !string.IsNullOrEmpty(ResolveMseLibUrl());
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            if (Loop != _loop)
                _loop = Loop;
        }

        private VideoSource NormalizeSource(VideoSource src)
        {
            var url = src?.Url;
            var type = src?.Type;
            if (string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(url))
                type = InferMime(url);
            var label = src?.Label;
            if (string.IsNullOrEmpty(label))
                label = url;
            return new VideoSource { Url = url, Type = type, Label = label };
        }

        private static string InferMime(string url)
        {
            var lower = url.Split('?')[0].ToLowerInvariant();
            if (lower.EndsWith(".mp4") || lower.EndsWith(".m4v")) return "video/mp4";
            if (lower.EndsWith(".webm")) return "video/webm";
            if (lower.EndsWith(".ogg") || lower.EndsWith(".ogv")) return "video/ogg";
            if (lower.EndsWith(".mov")) return "video/quicktime";
            if (lower.EndsWith(".m3u8")) return "application/x-mpegURL";
            if (lower.EndsWith(".mpd")) return "application/dash+xml";
            if (lower.EndsWith(".flv")) return "video/x-flv";
            if (lower.EndsWith(".ts")) return "video/mp2t";
            if (lower.EndsWith(".mkv")) return "video/x-matroska";
            if (lower.EndsWith(".avi")) return "video/x-msvideo";
            if (lower.EndsWith(".wmv")) return "video/x-ms-wmv";
            if (lower.EndsWith(".3gp")) return "video/3gpp";
            return string.Empty;
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender && !_attached)
            {
                try
                {
                    _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                        "import", "./_content/Element-Plus/js/elvideoplayergrow.js");
                    _dotNetRef = DotNetObjectReference.Create(this);
                    await JSRuntime.InvokeVoidAsync("elVideoPlayerGrowAttach", _uid, _dotNetRef, _isHls && !_useMseLib);
                    _attached = true;

                    var libUrl = ResolveMseLibUrl();
                    if (!string.IsNullOrEmpty(libUrl))
                    {
                        var firstSrc = _resolvedSources.FirstOrDefault()?.Url;
                        if (!string.IsNullOrEmpty(firstSrc))
                            await JSRuntime.InvokeVoidAsync("elVideoPlayerGrowLoadMse", _uid, ResolveMseKind(), firstSrc, libUrl);
                    }
                }
                catch (Exception ex)
                {
                    _errorMessage = "播放器脚本加载失败：" + ex.Message;
                    StateHasChanged();
                }
            }

            // 自动隐藏控制条的配置同步给 JS（首次与后续参数变更都走这里，幂等重绑）。
            if (_attached && (_autoHideApplied != AutoHideControls || _hideDelayApplied != ControlsHideDelay))
            {
                _autoHideApplied = AutoHideControls;
                _hideDelayApplied = ControlsHideDelay;
                try
                {
                    await JSRuntime.InvokeVoidAsync("elVideoPlayerGrowAutoHide", _uid, AutoHideControls && Controls, ControlsHideDelay);
                }
                catch (Exception)
                {
                    // 组件已销毁 / 脚本未就绪：忽略，不阻断渲染
                }
            }
        }

        // ---- 控制动作（下发到 JS） ----
        private ValueTask JsVoid(string fn, params object[] args) => JSRuntime.InvokeVoidAsync(fn, args);

        private async Task TogglePlay()
        {
            if (_isPlaying) await JsVoid("elVideoPlayerGrowPause", _uid);
            else await JsVoid("elVideoPlayerGrowPlay", _uid);
        }

        private Task Play() => JsVoid("elVideoPlayerGrowPlay", _uid).AsTask();
        private Task Pause() => JsVoid("elVideoPlayerGrowPause", _uid).AsTask();

        private async Task OnProgressPointerDown(PointerEventArgs e)
        {
            await JsVoid("elVideoPlayerGrowBeginScrub", _uid, e.ClientX);
        }

        private async Task OnVolumePointerDown(PointerEventArgs e)
        {
            await JsVoid("elVideoPlayerGrowBeginVolume", _uid, e.ClientX);
        }

        private Task ToggleMute() => JsVoid("elVideoPlayerGrowSetMuted", _uid, !_isMuted).AsTask();

        private async Task ToggleLoop()
        {
            _loop = !_loop;
            await JsVoid("elVideoPlayerGrowSetLoop", _uid, _loop);
            StateHasChanged();
        }

        private Task SetLoop(bool value)
        {
            _loop = value;
            return JsVoid("elVideoPlayerGrowSetLoop", _uid, _loop).AsTask();
        }

        private Task SetVolume(double v) => JsVoid("elVideoPlayerGrowSetVolume", _uid, Math.Max(0, Math.Min(1, v))).AsTask();

        private Task SetRate(double r)
        {
            _playbackRate = r;
            _rateMenuOpen = false;
            StateHasChanged();
            return JsVoid("elVideoPlayerGrowSetRate", _uid, r).AsTask();
        }

        private Task ToggleFullscreen() => JsVoid("elVideoPlayerGrowToggleFullscreen", _uid).AsTask();

        private Task TogglePiP() => JsVoid("elVideoPlayerGrowTogglePiP", _uid).AsTask();

        /// <summary>
        /// 运行时切换到指定视频源（网络地址 / blob 地址 / 流地址均可），并可选自动播放。
        /// 用于“打开本地文件”等需要动态切换源的场景：直接驱动 &lt;video&gt;.src 并 load/play，
        /// 同时绕过 razor 渲染的 &lt;source&gt; 子元素（由 JS 接管）。
        /// </summary>
        /// <param name="url">视频地址。</param>
        /// <param name="autoPlay">是否在设置后自动播放（默认 true；浏览器通常要求静音才会允许自动播放）。</param>
        public async Task LoadSource(string url, bool autoPlay = true)
        {
            if (string.IsNullOrEmpty(url)) return;
            _manualSrc = true;
            _resolvedSources = new List<VideoSource> { NormalizeSource(new VideoSource { Url = url }) };
            _isHls = _resolvedSources.Any(s =>
                string.Equals(s.Type, "application/x-mpegURL", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s.Type, "application/vnd.apple.mpegurl", StringComparison.OrdinalIgnoreCase) ||
                (s.Url != null && s.Url.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)));
            _loadedFired = false;
            _currentTime = 0;
            _duration = 0;
            _buffered = 0;
            _errorMessage = string.Empty;
            _hlsUnsupported = false;
            StateHasChanged();
            if (_attached)
                await JsVoid("elVideoPlayerGrowSetSrc", _uid, url, autoPlay);
        }

        private void ToggleRateMenu() => _rateMenuOpen = !_rateMenuOpen;

        // ---- JS 回传 ----
        [JSInvokable]
        public void VideoPlayerGrowOnEvent(string id, string type, string json)
        {
            if (id != _uid) return;
            try
            {
                using var doc = JsonDocument.Parse(json ?? "{}");
                var r = doc.RootElement;
                ApplySnapshot(r);

                switch (type)
                {
                    case "play":
                        _isPlaying = true;
                        _isWaiting = false;
                        _ = OnPlay.InvokeAsync();
                        break;
                    case "pause":
                        _isPlaying = false;
                        _ = OnPause.InvokeAsync();
                        break;
                    case "ended":
                        _isPlaying = false;
                        _ = OnEnded.InvokeAsync();
                        break;
                    case "waiting":
                        _isWaiting = true;
                        break;
                    case "playing":
                    case "canplay":
                        _isWaiting = false;
                        break;
                    case "volumechange":
                        break;
                    case "loadedmetadata":
                    case "durationchange":
                        if (!_loadedFired)
                        {
                            _loadedFired = true;
                            _ = OnLoadedMetadata.InvokeAsync();
                        }
                        break;
                    case "timeupdate":
                        if (OnTimeUpdate.HasDelegate)
                            _ = OnTimeUpdate.InvokeAsync(_currentTime);
                        break;
                    case "error":
                        if (OnError.HasDelegate)
                            _ = OnError.InvokeAsync(ErrorMessage);
                        break;
                    case "fullscreenchange":
                    case "pip":
                    case "progress":
                    case "init":
                    default:
                        break;
                }

                StateHasChanged();
            }
            catch
            {
                // 忽略单个回传解析异常，避免阻断 UI
            }
        }

        [JSInvokable]
        public void VideoPlayerGrowOnHlsUnsupported(string id)
        {
            if (id != _uid) return;
            _hlsUnsupported = true;
            if (string.IsNullOrEmpty(_errorMessage))
                _errorMessage = "当前浏览器不支持原生 HLS（.m3u8）播放，请使用 Safari 或较新版本 Chrome，或接入 hls.js。";
            StateHasChanged();
        }

        [JSInvokable]
        public void VideoPlayerGrowOnError(string id, string msg)
        {
            if (id != _uid) return;
            _errorMessage = msg;
            StateHasChanged();
            if (OnError.HasDelegate)
                _ = OnError.InvokeAsync(msg);
        }

        private void ApplySnapshot(JsonElement r)
        {
            if (r.TryGetProperty("currentTime", out var ct) && ct.ValueKind == JsonValueKind.Number) _currentTime = ct.GetDouble();
            if (r.TryGetProperty("duration", out var d) && d.ValueKind == JsonValueKind.Number) _duration = d.GetDouble();
            if (r.TryGetProperty("buffered", out var b) && b.ValueKind == JsonValueKind.Number) _buffered = b.GetDouble();
            if (r.TryGetProperty("volume", out var v) && v.ValueKind == JsonValueKind.Number) _volume = v.GetDouble();
            if (r.TryGetProperty("muted", out var m) && (m.ValueKind == JsonValueKind.True || m.ValueKind == JsonValueKind.False)) _isMuted = m.GetBoolean();
            if (r.TryGetProperty("playbackRate", out var pr) && pr.ValueKind == JsonValueKind.Number) _playbackRate = pr.GetDouble();
            if (r.TryGetProperty("paused", out var p) && (p.ValueKind == JsonValueKind.True || p.ValueKind == JsonValueKind.False)) _isPlaying = !p.GetBoolean();
            if (r.TryGetProperty("fullscreen", out var fs) && (fs.ValueKind == JsonValueKind.True || fs.ValueKind == JsonValueKind.False)) _isFullscreen = fs.GetBoolean();
            if (r.TryGetProperty("pip", out var pip) && (pip.ValueKind == JsonValueKind.True || pip.ValueKind == JsonValueKind.False)) _isPiP = pip.GetBoolean();
            if (r.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.String) _errorMessage = err.GetString();
        }

        private string FormatTime(double seconds)
        {
            if (!double.IsFinite(seconds) || seconds < 0) seconds = 0;
            if (_duration <= 0 && seconds == 0) return "--:--";
            var t = TimeSpan.FromSeconds(seconds);
            return t.TotalHours >= 1
                ? $"{(int)t.TotalHours:d2}:{t.Minutes:d2}:{t.Seconds:d2}"
                : $"{t.Minutes:d2}:{t.Seconds:d2}";
        }

        private string DurationText => _duration > 0 ? FormatTime(_duration) : (_isHls ? "LIVE" : "--:--");

        private bool IsLoop => _loop;

        private string ResolveMseKind()
        {
            switch (StreamType)
            {
                case ElVideoStreamType.Hls: return "hls";
                case ElVideoStreamType.Dash: return "dash";
                case ElVideoStreamType.Flv: return "flv";
                default: return null;
            }
        }

        private string ResolveMseLibUrl()
        {
            return ResolveMseKind() switch
            {
                "hls" => HlsLibUrl,
                "dash" => DashLibUrl,
                "flv" => FlvLibUrl,
                _ => null
            };
        }

        /// <summary>
        /// 异步销毁通道（Blazor 见到 <see cref="IAsyncDisposable"/> 就只走这条，不会再调同步 Dispose）。
        /// 必须在 JS 侧真的执行 detach（摘监听 + pause + 断源）之后再释放 <c>DotNetObjectReference</c>：
        /// 改前只 Dispose 引用就不管了，players 注册表 / document 上的 fullscreenchange / init 定时器与
        /// 已下载的媒体数据全部留着，实测关掉页签（keep-alive 只是 display:none，实例还活着）后声音照响。
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_released) return;
            _released = true;

            await DetachJavaScriptAsync();

            try { _dotNetRef?.Dispose(); } catch { }
            _dotNetRef = null;

            if (_jsModule != null)
            {
                try { await _jsModule.DisposeAsync(); } catch { }
                _jsModule = null;
            }

            Dispose();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_released)
            {
                // 兜底：宿主只走同步通道时无法 await，把 detach 调用发出去即返回（微任务里落地，
                // 媒体照样会被 pause）。绝不在同步上下文里 GetResult() 等 JS —— 那是死锁风险。
                _released = true;
                _ = DetachJavaScriptAsync();
                try { _dotNetRef?.Dispose(); } catch { }
                _dotNetRef = null;
                _jsModule = null;
            }

            base.Dispose(disposing);
        }

        /// <summary>把 <c>players[_uid]</c> 从 JS 注册表里摘掉（未 attach 成功过就是 no-op）。</summary>
        private async Task DetachJavaScriptAsync()
        {
            if (!_attached) return;
            _attached = false;
            try { await JSRuntime.InvokeVoidAsync("elVideoPlayerGrowDetach", _uid); }
            catch { /* 脚本未就绪 / JS 通道已断：不影响其余清理 */ }
        }
    }
}
