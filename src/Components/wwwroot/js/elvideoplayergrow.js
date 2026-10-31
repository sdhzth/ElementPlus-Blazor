// ElVideoPlayerGrow 交互脚本（零第三方依赖）。
// 负责：媒体事件监听并回传 .NET、播放/暂停/跳转/音量/倍速/全屏/画中画、进度与音量拖拽、HLS 原生支持探测。
(function () {
    "use strict";

    var players = {};

    function getEl(id) { return typeof document !== "undefined" ? document.getElementById(id) : null; }
    function getVideo(id) { return getEl(id + "-video"); }

    function snapshot(video) {
        var buffered = 0;
        try {
            if (video.buffered && video.buffered.length) {
                for (var i = 0; i < video.buffered.length; i++) {
                    if (video.buffered.start(i) <= video.currentTime && video.currentTime <= video.buffered.end(i)) {
                        buffered = video.buffered.end(i);
                    }
                }
                if (!buffered && video.buffered.length) {
                    buffered = video.buffered.end(video.buffered.length - 1);
                }
            }
        } catch (e) { /* ignore */ }

        var error = null;
        if (video.error) {
            error = "播放错误（code " + video.error.code +
                (video.error.message ? "：" + video.error.message : "") + "）";
        }

        return {
            currentTime: isFinite(video.currentTime) ? video.currentTime : 0,
            duration: isFinite(video.duration) ? video.duration : 0,
            buffered: buffered,
            volume: isFinite(video.volume) ? video.volume : 1,
            muted: !!video.muted,
            paused: !!video.paused,
            playbackRate: isFinite(video.playbackRate) ? video.playbackRate : 1,
            readyState: video.readyState,
            waiting: false,
            ended: !!video.ended,
            error: error,
            fullscreen: !!document.fullscreenElement,
            pip: !!document.pictureInPictureElement && document.pictureInPictureElement === video
        };
    }

    function emit(rec, type) {
        if (!rec || !rec.dotNet) return;
        try {
            rec.dotNet.invokeMethodAsync("VideoPlayerGrowOnEvent", rec.id, type, JSON.stringify(snapshot(rec.video)));
        } catch (e) { /* ignore */ }
    }

    function attach(id, dotNetRef, isHls) {
        var video = getVideo(id);
        if (!video) return;
        players[id] = { id: id, video: video, dotNet: dotNetRef, listeners: [] };

        // HLS 原生支持探测
        if (isHls) {
            var can = video.canPlayType("application/vnd.apple.mpegurl") ||
                video.canPlayType("application/x-mpegURL");
            if (!can && dotNetRef) {
                try { dotNetRef.invokeMethodAsync("VideoPlayerGrowOnHlsUnsupported", id); } catch (e) { /* ignore */ }
            }
        }

        var listeners = [
            { ev: "loadedmetadata", t: "loadedmetadata" },
            { ev: "durationchange", t: "durationchange" },
            { ev: "timeupdate", t: "timeupdate" },
            { ev: "play", t: "play" },
            { ev: "pause", t: "pause" },
            { ev: "ended", t: "ended" },
            { ev: "volumechange", t: "volumechange" },
            { ev: "waiting", t: "waiting" },
            { ev: "playing", t: "playing" },
            { ev: "canplay", t: "canplay" },
            { ev: "progress", t: "progress" },
            { ev: "error", t: "error" }
        ];
        listeners.forEach(function (item) {
            var fn = function () { emit(players[id], item.t); };
            video.addEventListener(item.ev, fn);
            players[id].listeners.push({ ev: item.ev, fn: fn });
        });

        players[id].onFs = function () {
            var rec = players[id];
            if (rec) emit(rec, "fullscreenchange");
        };
        document.addEventListener("fullscreenchange", players[id].onFs);

        players[id].onPipEnter = function () {
            var rec = players[id]; if (rec) emit(rec, "pip");
        };
        players[id].onPipLeave = function () {
            var rec = players[id]; if (rec) emit(rec, "pip");
        };
        video.addEventListener("enterpictureinpicture", players[id].onPipEnter);
        video.addEventListener("leavepictureinpicture", players[id].onPipLeave);

        // 初始快照
        players[id].initTimer = setTimeout(function () { var rec = players[id]; if (rec) emit(rec, "init"); }, 0);
    }

    /* 释放实例（关页签 / 组件销毁时由 .NET 调用）。
       改前 .NET 侧只 Dispose 掉 DotNetObjectReference：players 注册表、document 上的 fullscreenchange
       监听、init 定时器与已下载的媒体数据全部留着 —— 实测关闭页签后视频仍在播放（声音还在响）。
       顺序有讲究：先摘监听并断掉 dotNet 回调，再 pause + 断源 + load()（浏览器据此释放网络与解码器），
       最后删注册表项；反过来先 pause 会把 pause 事件回传给已经卸载的 .NET 对象。 */
    function detach(id) {
        var rec = players[id];
        if (!rec) return false;
        var video = rec.video;

        rec.dotNet = null;
        if (rec.listeners) {
            rec.listeners.forEach(function (l) { if (video) video.removeEventListener(l.ev, l.fn); });
            rec.listeners = null;
        }
        if (rec.onFs) { document.removeEventListener("fullscreenchange", rec.onFs); rec.onFs = null; }
        if (video) {
            if (rec.onPipEnter) video.removeEventListener("enterpictureinpicture", rec.onPipEnter);
            if (rec.onPipLeave) video.removeEventListener("leavepictureinpicture", rec.onPipLeave);
        }
        if (rec.initTimer) { clearTimeout(rec.initTimer); rec.initTimer = null; }
        if (rec.detachAutoHide) { try { rec.detachAutoHide(); } catch (e) { /* ignore */ } rec.detachAutoHide = null; }
        try {
            if (rec.hls && rec.hls.destroy) rec.hls.destroy();
            if (rec.flv && rec.flv.destroy) rec.flv.destroy();
            if (rec.dash && rec.dash.destroy) rec.dash.destroy();
            if (video) {
                video.pause();
                video.removeAttribute("src");
                video.load();
            }
        } catch (e) { /* 源已不可用时 load() 会抛，不影响释放 */ }

        delete players[id];
        return true;
    }

    function play(id) { var v = getVideo(id); if (v) { var p = v.play(); if (p && p.catch) p.catch(function () { }); } }
    function pause(id) { var v = getVideo(id); if (v) v.pause(); }
    function seek(id, t) { var v = getVideo(id); if (v) v.currentTime = t; }
    function setVolume(id, vol) { var v = getVideo(id); if (v) { v.volume = vol; v.muted = false; } }
    function setMuted(id, muted) { var v = getVideo(id); if (v) v.muted = muted; }
    function setRate(id, r) { var v = getVideo(id); if (v) v.playbackRate = r; }
    function setLoop(id, v) { var video = getVideo(id); if (video) video.loop = !!v; }

    // 运行时切换视频源（本地 blob / 网络地址 / 流均可）：直接驱动 <video>.src 并 load/play，
    // 同时清理可能挂载的 MSE 实例与 razor 渲染的旧 <source> 子元素，避免旧源干扰。
    function setSrc(id, url, autoPlay) {
        var rec = players[id];
        var v = getVideo(id);
        if (!v || !url) return;
        if (rec) {
            if (rec.hls) { try { rec.hls.destroy(); } catch (e) {} rec.hls = null; }
            if (rec.flv) { try { rec.flv.destroy(); } catch (e) {} rec.flv = null; }
        }
        try {
            var srcs = v.querySelectorAll("source");
            for (var i = 0; i < srcs.length; i++) v.removeChild(srcs[i]);
        } catch (e) {}
        v.removeAttribute("src");
        v.src = url;
        try { v.load(); } catch (e) {}
        if (autoPlay) {
            var p = v.play();
            if (p && p.catch) p.catch(function () { });
        }
    }

    // 弹出系统文件选择框，将选中的本地视频转为 blob URL 回传（不依赖任何服务器）。
    function pickLocalVideo() {
        return new Promise(function (resolve) {
            var input = document.createElement("input");
            input.type = "file";
            input.accept = "video/*";
            input.style.display = "none";
            input.onchange = function () {
                var f = input.files && input.files[0];
                try { document.body.removeChild(input); } catch (e) {}
                if (!f) { resolve(null); return; }
                try { resolve({ url: URL.createObjectURL(f), name: f.name }); }
                catch (e) { resolve(null); }
            };
            document.body.appendChild(input);
            input.click();
        });
    }

    // 外部 MSE 库（hls.js / dash.js / flv.js）接入分支：仅当调用方显式提供库地址时加载，默认零依赖。
    function loadMse(id, kind, srcUrl, libUrl) {
        var video = getVideo(id);
        if (!video || !libUrl) return;
        function start() {
            try {
                if (kind === "hls" && window.Hls) {
                    var hls = new window.Hls();
                    hls.loadSource(srcUrl);
                    hls.attachMedia(video);
                    if (players[id]) players[id].hls = hls;
                } else if (kind === "dash" && window.dashjs) {
                    var mp = window.dashjs.MediaPlayer().create();
                    mp.initialize(video, srcUrl, false);
                    // 改前 dash 实例只活在 start() 的局部变量里，detach 拿不到 → 卸载后仍在拉流
                    if (players[id]) players[id].dash = mp;
                } else if (kind === "flv" && window.flvjs) {
                    var p = window.flvjs.createPlayer({ type: "flv", url: srcUrl });
                    p.attachMediaElement(video);
                    p.load();
                    if (players[id]) players[id].flv = p;
                }
            } catch (e) {
                var rec = players[id];
                if (rec && rec.dotNet) rec.dotNet.invokeMethodAsync("VideoPlayerGrowOnError", id, "MSE 接入失败：" + (e && e.message ? e.message : e));
            }
        }
        if (window.Hls || window.dashjs || window.flvjs) { start(); return; }
        var s = document.createElement("script");
        s.src = libUrl;
        s.onload = start;
        s.onerror = function () {
            var rec = players[id];
            if (rec && rec.dotNet) rec.dotNet.invokeMethodAsync("VideoPlayerGrowOnError", id, "MSE 库加载失败：" + libUrl);
        };
        document.head.appendChild(s);
    }

    function toggleFullscreen(id) {
        var el = getEl(id); // 全屏目标为容器，使控制条一并进入全屏
        if (!el) return;
        if (document.fullscreenElement) {
            if (document.exitFullscreen) document.exitFullscreen();
        } else if (el.requestFullscreen) {
            var p = el.requestFullscreen();
            if (p && p.catch) p.catch(function () { });
        }
    }

    function togglePiP(id) {
        var v = getVideo(id);
        if (!v) return;
        if (document.pictureInPictureElement) {
            if (document.exitPictureInPicture) document.exitPictureInPicture().catch(function () { });
        } else if (v.requestPictureInPicture) {
            v.requestPictureInPicture().catch(function () { });
        }
    }

    // 「平时隐藏控制条、鼠标移上去才显示」。
    // 纯前端行为（只切一个 class），不走 .NET 往返 —— 每次鼠标移动都回 C# 会让视频播放明显掉帧。
    // 幂等：重复调用会先解绑上一次的监听，因此 AutoHideControls / ControlsHideDelay 可以在运行时改。
    function autoHide(id, enabled, delay) {
        var el = getEl(id);
        if (!el) return;

        var rec = players[id];
        if (rec && rec.detachAutoHide) {
            rec.detachAutoHide();
            rec.detachAutoHide = null;
        }
        el.classList.remove("is-controls-hidden");

        if (!enabled) return;

        var wait = (typeof delay === "number" && delay > 0) ? delay : 3000;
        var timer = null;

        function video() {
            var r = players[id];
            return (r && r.video) ? r.video : getVideo(id);
        }

        function show() {
            el.classList.remove("is-controls-hidden");
            schedule();
        }

        function hide() {
            var v = video();
            // 暂停时始终可见：用户停下来就是要找按钮，藏起来只会让人困惑。
            if (v && v.paused) return;
            // 倍速菜单展开中不隐藏，否则菜单会连按钮一起淡出。
            if (el.querySelector(".el-video-player-grow__rate-menu")) return;
            el.classList.add("is-controls-hidden");
        }

        function schedule() {
            if (timer) clearTimeout(timer);
            timer = setTimeout(hide, wait);
        }

        function onLeave() {
            if (timer) clearTimeout(timer);
            hide();
        }

        function onPlayState() {
            var v = video();
            if (!v) return;
            if (v.paused) {
                if (timer) clearTimeout(timer);
                el.classList.remove("is-controls-hidden");
            } else {
                schedule();
            }
        }

        el.addEventListener("mousemove", show);
        el.addEventListener("mouseenter", show);
        el.addEventListener("mouseleave", onLeave);
        el.addEventListener("pointerdown", show);
        var v0 = video();
        if (v0) {
            v0.addEventListener("play", onPlayState);
            v0.addEventListener("pause", onPlayState);
        }

        if (!players[id]) players[id] = { id: id, video: v0 };
        players[id].detachAutoHide = function () {
            if (timer) clearTimeout(timer);
            el.removeEventListener("mousemove", show);
            el.removeEventListener("mouseenter", show);
            el.removeEventListener("mouseleave", onLeave);
            el.removeEventListener("pointerdown", show);
            if (v0) {
                v0.removeEventListener("play", onPlayState);
                v0.removeEventListener("pause", onPlayState);
            }
            el.classList.remove("is-controls-hidden");
        };

        onPlayState();
    }

    function makeScrub(getTrack, getVideo, apply) {
        return function (id, clientX) {
            var track = getTrack(id);
            var v = getVideo(id);
            if (!track || !v) return;
            function move(e) {
                var pt = (e.touches && e.touches[0]) || e;
                var rect = track.getBoundingClientRect();
                var x = (pt.clientX != null ? pt.clientX : 0) - rect.left;
                var frac = rect.width ? x / rect.width : 0;
                frac = Math.max(0, Math.min(1, frac));
                apply(v, frac);
            }
            function up() {
                document.removeEventListener("pointermove", move);
                document.removeEventListener("pointerup", up);
                document.removeEventListener("touchmove", move);
                document.removeEventListener("touchend", up);
            }
            document.addEventListener("pointermove", move);
            document.addEventListener("pointerup", up);
            document.addEventListener("touchmove", move, { passive: true });
            document.addEventListener("touchend", up);
            if (clientX != null) move({ clientX: clientX });
        };
    }

    var beginScrub = makeScrub(
        function (id) { return getEl(id + "-progress"); },
        function (id) { var rec = players[id]; return rec ? rec.video : getEl(id); },
        function (v, frac) { if (isFinite(v.duration) && v.duration > 0) v.currentTime = frac * v.duration; }
    );

    var beginVolume = makeScrub(
        function (id) { return getEl(id + "-volume"); },
        function (id) { var rec = players[id]; return rec ? rec.video : getEl(id); },
        function (v, frac) { v.volume = frac; v.muted = false; }
    );

    window.elVideoPlayerGrowAttach = attach;
    window.elVideoPlayerGrowDetach = detach;
    window.elVideoPlayerGrowPlay = play;
    window.elVideoPlayerGrowPause = pause;
    window.elVideoPlayerGrowSeek = seek;
    window.elVideoPlayerGrowSetVolume = setVolume;
    window.elVideoPlayerGrowSetMuted = setMuted;
    window.elVideoPlayerGrowSetRate = setRate;
    window.elVideoPlayerGrowSetLoop = setLoop;
    window.elVideoPlayerGrowSetSrc = setSrc;
    window.elVideoPlayerGrowPickLocalVideo = pickLocalVideo;
    window.elVideoPlayerGrowLoadMse = loadMse;
    window.elVideoPlayerGrowToggleFullscreen = toggleFullscreen;
    window.elVideoPlayerGrowTogglePiP = togglePiP;
    window.elVideoPlayerGrowAutoHide = autoHide;
    window.elVideoPlayerGrowBeginScrub = beginScrub;
    window.elVideoPlayerGrowBeginVolume = beginVolume;
})();
