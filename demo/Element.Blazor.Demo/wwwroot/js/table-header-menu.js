// demo 专用：列头点击弹出菜单的辅助逻辑（不随组件库发布）
// 1) 记录最近一次点击的 .el-table__header th 坐标，供 C# OnHeaderClick 弹菜单时定位
// 2) 提供 clipboard 复制列名的纯 JS 实现（安全上下文走 navigator.clipboard，否则 execCommand 兜底）
(function () {
    // 最近一次有效列头点击坐标；菜单弹出前由 C# 读取
    window.__demoLastThClick = { x: window.innerWidth / 2, y: 80 };

    function isTableHeader(node) {
        var el = node;
        while (el && el.nodeType === 1) {
            if (el.tagName === 'TH' && el.closest && el.closest('.el-table__header')) {
                return true;
            }
            el = el.parentElement;
        }
        return false;
    }

    document.addEventListener('click', function (e) {
        // 排序箭头 / 筛选项 等带 @onclick:stopPropagation 的元素会阻止冒泡，因此不会污染到这里
        if (isTableHeader(e.target)) {
            window.__demoLastThClick = { x: e.clientX, y: e.clientY };
        }
    }, false);

    // 返回最近一次列头点击坐标（已做视口内夹取，避免菜单溢出屏幕）
    window.demoGetLastHeaderClick = function () {
        var x = window.__demoLastThClick ? window.__demoLastThClick.x : window.innerWidth / 2;
        var y = window.__demoLastThClick ? window.__demoLastThClick.y : 80;
        var maxX = window.innerWidth - 200;
        var maxY = window.innerHeight - 220;
        if (x > maxX) x = maxX;
        if (y > maxY) y = maxY;
        if (x < 8) x = 8;
        if (y < 8) y = 8;
        return { x: x, y: y };
    };

    function fallbackCopy(text) {
        var ta = document.createElement('textarea');
        ta.value = text;
        ta.style.position = 'fixed';
        ta.style.top = '-1000px';
        ta.style.opacity = '0';
        document.body.appendChild(ta);
        ta.focus();
        ta.select();
        var ok = false;
        try { ok = document.execCommand('copy'); } catch (e) { ok = false; }
        document.body.removeChild(ta);
        return ok;
    }

    // 复制文本：安全上下文用 navigator.clipboard，否则 textarea + execCommand 兜底
    window.demoCopyText = function (text) {
        try {
            if (navigator.clipboard && window.isSecureContext) {
                return navigator.clipboard.writeText(text == null ? '' : String(text))
                    .then(function () { return true; })
                    .catch(function () { return fallbackCopy(text); });
            }
        } catch (e) { /* 忽略，走兜底 */ }
        return Promise.resolve(fallbackCopy(text));
    };
})();
