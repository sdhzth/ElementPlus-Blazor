/* ==========================================================================
   Element-Blazor 演示中心 —— 布局外壳（demo 专用，不随组件库发布）

   只做三件浏览器原生的事：
   1. 全屏开关（Fullscreen API）并把状态回推给 Blazor，用于切换图标；
   2. localStorage 读写，供「页签持久化」使用（强刷后恢复已打开的多标签页）；
   3. Ctrl+K 聚焦页头搜索框。
   ========================================================================== */
(function () {
    var FS_KEY = '__demoFullscreen';
    var SHORTCUT_KEY = '__demoShortcut';

    // ---------- 全屏 ----------

    window.demoIsFullscreen = function () {
        return !!(document.fullscreenElement || document.webkitFullscreenElement);
    };

    window.demoToggleFullscreen = function () {
        var el = document.documentElement;
        var isFull = window.demoIsFullscreen();

        try {
            if (isFull) {
                if (document.exitFullscreen) {
                    document.exitFullscreen();
                } else if (document.webkitExitFullscreen) {
                    document.webkitExitFullscreen();
                }
            } else if (el.requestFullscreen) {
                var p = el.requestFullscreen();
                if (p && typeof p.catch === 'function') { p.catch(function () {}); }
            } else if (el.webkitRequestFullscreen) {
                var wp = el.webkitRequestFullscreen();
                if (wp && typeof wp.catch === 'function') { wp.catch(function () {}); }
            }
        } catch (e) {
            // 用户手势之外调用或浏览器策略拒绝：静默失败，别把 demo 弄崩
        }
    };

    window.demoBindFullscreen = function (dotnet) {
        if (window[FS_KEY]) {
            return;
        }

        var handler = function () {
            if (dotnet) {
                dotnet.invokeMethodAsync('OnDemoFullscreenChanged', window.demoIsFullscreen());
            }
        };

        window[FS_KEY] = { dotnet: dotnet, handler: handler };
        document.addEventListener('fullscreenchange', handler);
        document.addEventListener('webkitfullscreenchange', handler);
    };

    window.demoUnbindFullscreen = function () {
        var st = window[FS_KEY];

        if (!st) {
            return;
        }

        document.removeEventListener('fullscreenchange', st.handler);
        document.removeEventListener('webkitfullscreenchange', st.handler);
        window[FS_KEY] = null;
    };

    // ---------- localStorage（页签持久化） ----------

    window.demoStoreGet = function (key) {
        try {
            return window.localStorage.getItem(key);
        } catch (e) {
            return null;
        }
    };

    window.demoStoreSet = function (key, value) {
        try {
            window.localStorage.setItem(key, value);
        } catch (e) {
            // 隐私模式 / 配额满：忽略，最多丢持久化
        }
    };

    window.demoStoreRemove = function (key) {
        try {
            window.localStorage.removeItem(key);
        } catch (e) {
        }
    };

    // ---------- 内容区滚动（标签栏固定，只有 .demo-main 滚） ----------

    var CONTENT_KEY = '__demoContentScroll';

    /** 内容区滚到顶（切换标签页时调用，避免新页面停在上一页的滚动位置）。 */
    window.demoScrollContentTop = function () {
        var main = document.querySelector('.demo-main');

        // 已经在顶部就别再赋一次值，省掉一次无谓的滚动/样式失效
        if (main && main.scrollTop !== 0) {
            main.scrollTop = 0;
        }
    };

    /** 监听内容区滚动：滚过 0 就在外壳上打 is-content-scrolled，供 CSS 给标签栏加投影。 */
    window.demoBindContentScroll = function () {
        if (window[CONTENT_KEY]) {
            return;
        }

        var main = document.querySelector('.demo-main');
        var shell = document.querySelector('.demo-shell');

        if (!main || !shell) {
            return;
        }

        var handler = function () {
            if (main.scrollTop > 0) {
                shell.classList.add('is-content-scrolled');
            } else {
                shell.classList.remove('is-content-scrolled');
            }
        };

        main.addEventListener('scroll', handler, { passive: true });
        window[CONTENT_KEY] = { main: main, handler: handler };
        handler();
    };

    window.demoUnbindContentScroll = function () {
        var st = window[CONTENT_KEY];

        if (!st) {
            return;
        }

        st.main.removeEventListener('scroll', st.handler);
        window[CONTENT_KEY] = null;
    };

    // ---------- keep-alive 路由：缓存页显隐 ----------

    /**
     * 只显示 key 匹配的缓存页（其余 display:none）。
     * 由 KeepAliveRouteView 在激活 key 变化时调用。用命名函数替代 eval：
     * 免去每次切页签都编译一段动态脚本，并且只在该元素显隐确实需要改变时才写 style。
     */
    window.demoKeepAliveShow = function (key) {
        var els = document.querySelectorAll('[data-keep-key]');

        for (var i = 0; i < els.length; i++) {
            var el = els[i];

            if (el.getAttribute('data-keep-key') === key) {
                if (el.style.display !== 'contents') {
                    el.style.display = 'contents';
                }
            } else if (el.style.display !== 'none') {
                el.style.display = 'none';
            }
        }
    };

    /**
     * 等浏览器真正画完一帧后 resolve。
     * 连等两帧：第一帧的 rAF 回调触发时该帧还没 paint，第二帧回调触发时上一帧已经画完。
     * 用于「先让反馈落屏，再做会阻塞主线程的重挂载」。
     */
    window.demoNextFrame = function () {
        return new Promise(function (resolve) {
            var done = false;

            function finish() {
                if (!done) {
                    done = true;
                    resolve();
                }
            }

            requestAnimationFrame(function () {
                requestAnimationFrame(finish);
            });

            // 兜底：后台标签页里 rAF 会被暂停，这里保证挂载不会无限期等待
            window.setTimeout(finish, 120);
        });
    };

    // ---------- 其它 ----------

    window.demoFocusElement = function (el) {
        if (el && typeof el.focus === 'function') {
            el.focus();
        }
    };

    window.demoBindShortcut = function (dotnet) {
        if (window[SHORTCUT_KEY]) {
            return;
        }

        var handler = function (e) {
            // Ctrl+K / ⌘K → 聚焦搜索
            if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) {
                e.preventDefault();
                if (dotnet) {
                    dotnet.invokeMethodAsync('OnDemoShortcut', 'search');
                }
            }
        };

        window[SHORTCUT_KEY] = { dotnet: dotnet, handler: handler };
        document.addEventListener('keydown', handler, true);
    };

    window.demoUnbindShortcut = function () {
        var st = window[SHORTCUT_KEY];

        if (!st) {
            return;
        }

        document.removeEventListener('keydown', st.handler, true);
        window[SHORTCUT_KEY] = null;
    };
})();
