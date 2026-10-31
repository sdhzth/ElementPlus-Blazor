/* ==========================================================================
   Element-Blazor 演示中心 —— 每个示例块的「复制代码 / 查看源代码」
   （demo 专用，不随组件库发布）

   对齐 https://element-plus.org/zh-CN/component/xxx 官方文档：
   - 每个示例块右下角一排小按钮：复制代码 / 查看源代码；
   - 点「查看源代码」不是弹窗，而是把代码**在示例块内就地展开**，顶部一条工具条
     （左侧 本段示例 / 整页源码 分段切换，右侧 复制、收起 两个图标）；
   - 代码左侧带折叠箭头，可逐层展开/收起（对齐官方代码区的 ˅）。

   实现要点（为什么不改 100 个 demo 页）：
   - 示例块在 89 个页面里手写成 <div class="demo-section">，共 486 个；
     一页页改既不现实也会破坏既有结构，所以这里走「浏览器侧增强」：
     C# 侧只把当前页面整份 .razor 源码推给 JS（demoCodeSetSource），
     JS 自己在每个 .demo-section 末尾追加操作条 + 代码面板，并从源码切片段。
   - 页面与源码的对应关系不用额外维护路由表：keep-alive 外壳给每个缓存页
     打的是 data-keep-key="页面类型全名|路由参数"，类型全名的末段就是文件名
     （Element.Blazor.Demo.Pages.BorderDemo → BorderDemo.razor）。
   - keep-alive 页面是「两阶段挂载」（先占位、下一帧才真正挂载），所以源码
     可能比 DOM 先到；这里用 MutationObserver + 去抖扫描兜住任意先后顺序。
   - 代码面板的内容**首次展开才渲染**（486 个块不可能一次性铺满 DOM）。
   ========================================================================== */
(function () {
    'use strict';

    /** 页面类型全名 → 整份 .razor 源码 */
    var sources = Object.create(null);
    /** 页面类型全名 → 文件名（BorderDemo.razor） */
    var fileNames = Object.create(null);

    var observer = null;
    var scanTimer = 0;
    var scanQueued = false;

    // ---------- 图标（内联 SVG，不依赖图标字体，颜色跟随 currentColor） ----------

    function svg(cls, inner) {
        return '<svg class="' + cls + '" viewBox="0 0 24 24" fill="none" stroke="currentColor"'
            + ' stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">'
            + inner + '</svg>';
    }

    var ICON_COPY = svg('demo-code-icon',
        '<rect x="9" y="9" width="11" height="11" rx="2"></rect><path d="M5 15V6a2 2 0 0 1 2-2h9"></path>');
    var ICON_CHECK = svg('demo-code-icon', '<path d="M20 6 9 17l-5-5"></path>');
    var ICON_CODE = svg('demo-code-icon', '<path d="M9 6 4 12l5 6"></path><path d="M15 6l5 6-5 6"></path>');
    var ICON_CHEVRON = svg('dcl-chevron', '<path d="m6 9 6 6 6-6"></path>');

    // ---------- 纯函数区（不碰 DOM，可在 node 里单测） ----------

    function escapeHtml(text) {
        return String(text)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    }

    function unescapeHtml(text) {
        return String(text)
            .replace(/&lt;/g, '<')
            .replace(/&gt;/g, '>')
            .replace(/&quot;/g, '"')
            .replace(/&#39;/g, "'")
            .replace(/&nbsp;/g, ' ')
            .replace(/&amp;/g, '&');
    }

    /**
     * 判断 index 处是否处于「标签起始位置」：往前跳过空白后应当是行首或 '>'。
     * 用来排除 C# 字符串里手写的 "<div class=\"demo-section\">" 之类的假命中。
     */
    function atTagStart(text, index) {
        var i = index - 1;

        while (i >= 0 && (text.charAt(i) === ' ' || text.charAt(i) === '\t')) {
            i--;
        }

        return i < 0 || text.charAt(i) === '\n' || text.charAt(i) === '>';
    }

    /** 从 from 处开始按 <div> 嵌套深度找出该 div 的收尾位置（不含收尾标签之后）。 */
    function findDivEnd(text, from) {
        var depth = 1;
        var re = /<(\/?)div\b[^>]*?(\/?)>/gi;
        var m;

        re.lastIndex = from;

        while ((m = re.exec(text))) {
            if (m[1] === '/') {
                depth--;

                if (depth === 0) {
                    return m.index + m[0].length;
                }
            } else if (m[2] !== '/') {
                depth++;
            }
        }

        return text.length;
    }

    /** 去掉整块代码的公共缩进与首尾空行，让复制出来的代码贴左。 */
    function deindent(code) {
        var lines = String(code).replace(/\r\n/g, '\n').split('\n');

        while (lines.length && !lines[0].trim()) {
            lines.shift();
        }

        while (lines.length && !lines[lines.length - 1].trim()) {
            lines.pop();
        }

        var min = -1;

        for (var i = 0; i < lines.length; i++) {
            if (!lines[i].trim()) {
                continue;
            }

            var indent = lines[i].length - lines[i].replace(/^[ \t]+/, '').length;

            if (min < 0 || indent < min) {
                min = indent;
            }
        }

        if (min > 0) {
            for (var j = 0; j < lines.length; j++) {
                lines[j] = lines[j].slice(min);
            }
        }

        return lines.join('\n');
    }

    /** 取块内第一个标题（h1~h6）的纯文本，用于把 DOM 里的示例块和源码片段对上号。 */
    function sectionTitle(code) {
        var m = /<h[1-6][^>]*>([\s\S]*?)<\/h[1-6]>/i.exec(code);

        if (!m) {
            return '';
        }

        return unescapeHtml(m[1].replace(/<[^>]*>/g, '')).replace(/\s+/g, ' ').trim();
    }

    /** 取出页面里所有 <div class="demo-section"> 块（按出现顺序）。 */
    function extractSections(text) {
        var src = String(text).replace(/\r\n/g, '\n');
        var out = [];
        var re = /<div\b[^>]*class="[^"]*\bdemo-section\b[^"]*"[^>]*>/g;
        var m;

        while ((m = re.exec(src))) {
            if (!atTagStart(src, m.index)) {
                continue;
            }

            var tag = m[0];
            var end = tag.charAt(tag.length - 2) === '/'
                ? m.index + tag.length
                : findDivEnd(src, m.index + tag.length);
            var code = deindent(src.slice(m.index, end));

            out.push({ title: sectionTitle(code), code: code });
            re.lastIndex = Math.max(end, m.index + tag.length);
        }

        return out;
    }

    /**
     * 轻量 Razor / HTML 高亮（单遍替换，天然不会嵌套出错的 span）。
     * 注释 → 灰、@ 指令与表达式 → 紫、标签名 → 蓝、属性名 → 橙、字符串 → 绿。
     */
    function highlight(code) {
        return escapeHtml(code).replace(
            /(&lt;!--[\s\S]*?--&gt;)|(@@[A-Za-z_][\w.]*|@[A-Za-z_][\w.]*|@\(|@@)|(&lt;\/?)([A-Za-z][\w.:-]*)|([A-Za-z_][\w.:-]*)(?==)|("(?:[^"\\\n]|\\.)*")/g,
            function (all, comment, razor, tagLead, tagName, attrName, str) {
                if (comment) {
                    return '<span class="dc-comment">' + comment + '</span>';
                }

                if (razor) {
                    return '<span class="dc-razor">' + razor + '</span>';
                }

                if (tagName) {
                    return '<span class="dc-tag">' + tagLead + tagName + '</span>';
                }

                if (attrName) {
                    return '<span class="dc-attr">' + attrName + '</span>';
                }

                if (str) {
                    return '<span class="dc-str">' + str + '</span>';
                }

                return all;
            });
    }

    /** 行首缩进宽度（tab 折 4 空格），折叠判定用。 */
    function indentOf(line) {
        var m = /^[ \t]*/.exec(line)[0];
        var n = 0;

        for (var i = 0; i < m.length; i++) {
            n += m.charAt(i) === '\t' ? 4 : 1;
        }

        return n;
    }

    /**
     * 按缩进算出可折叠区间（行号 0-based，闭区间 {start, end}）。
     * 与官方代码区一致：某行的下一个非空行缩进更深 → 该行可折叠，
     * 范围一直到「最后一个缩进仍更深的行」。纯缩进法天然不会产生交叉区间，
     * 嵌套折叠只是包含关系，展开/收起互不干扰。
     */
    function computeFolds(lines) {
        var out = [];
        var n = lines.length;

        for (var i = 0; i < n; i++) {
            if (!lines[i].trim()) {
                continue;
            }

            var base = indentOf(lines[i]);
            var j = i + 1;

            while (j < n && !lines[j].trim()) {
                j++;
            }

            if (j >= n || indentOf(lines[j]) <= base) {
                continue;
            }

            var end = -1;

            for (var k = j; k < n; k++) {
                if (!lines[k].trim()) {
                    continue;
                }

                if (indentOf(lines[k]) > base) {
                    end = k;
                } else {
                    break;
                }
            }

            if (end > i) {
                out.push({ start: i, end: end });
            }
        }

        return out;
    }

    /** 从 keep-alive 的 data-keep-key 里取出页面类型全名。 */
    function typeNameOfKey(key) {
        var raw = String(key || '');
        var bar = raw.indexOf('|');

        return bar >= 0 ? raw.slice(0, bar) : raw;
    }

    // node（无 DOM）下只导出纯函数，供解析/折叠/高亮回归测试直接调用；浏览器里继续走下面的 DOM 逻辑。
    if (typeof window === 'undefined' && typeof document === 'undefined') {
        if (typeof module !== 'undefined' && module.exports) {
            module.exports = {
                extractSections: extractSections,
                highlight: highlight,
                computeFolds: computeFolds,
                indentOf: indentOf,
                deindent: deindent,
                sectionTitle: sectionTitle,
                escapeHtml: escapeHtml,
                typeNameOfKey: typeNameOfKey
            };
        }

        return;
    }

    // ---------- 剪贴板 ----------

    /** 非安全上下文（局域网 IP 访问）下 navigator.clipboard 不可用，退回 textarea + execCommand。 */
    function legacyCopy(text) {
        try {
            var area = document.createElement('textarea');

            area.value = text;
            area.setAttribute('readonly', 'readonly');
            area.style.position = 'fixed';
            area.style.top = '-1000px';
            area.style.opacity = '0';
            document.body.appendChild(area);
            area.select();
            area.setSelectionRange(0, area.value.length);

            var ok = document.execCommand('copy');

            document.body.removeChild(area);

            return ok;
        } catch (e) {
            return false;
        }
    }

    function copyText(text, onSuccess, onFailure) {
        if (navigator.clipboard && window.isSecureContext) {
            navigator.clipboard.writeText(text).then(function () {
                onSuccess();
            }, function () {
                if (legacyCopy(text)) {
                    onSuccess();
                } else {
                    onFailure();
                }
            });

            return;
        }

        if (legacyCopy(text)) {
            onSuccess();
        } else {
            onFailure();
        }
    }

    // ---------- 小提示（复制失败才用，成功走按钮自身状态） ----------

    function toast(message) {
        var el = document.createElement('div');

        el.className = 'demo-code-toast';
        el.textContent = message;
        document.body.appendChild(el);

        window.requestAnimationFrame(function () {
            el.classList.add('is-show');
        });

        window.setTimeout(function () {
            el.classList.remove('is-show');
            window.setTimeout(function () {
                if (el.parentNode) {
                    el.parentNode.removeChild(el);
                }
            }, 240);
        }, 2000);
    }

    // ---------- 操作条 ----------

    function barButton(act, icon, label, title) {
        return '<button type="button" class="demo-code-btn" data-code-act="' + act + '"'
            + ' title="' + title + '" aria-label="' + title + '">'
            + icon + '<span class="demo-code-label">' + label + '</span></button>';
    }

    /** 复制成功：图标变对勾、文案变「已复制」，1.4 秒后复原（图标按钮没有文案就只换图标）。 */
    function flashCopied(btn) {
        if (btn.getAttribute('data-code-busy') === '1') {
            return;
        }

        var icon = btn.querySelector('.demo-code-icon');

        if (!icon) {
            return;
        }

        var label = btn.querySelector('.demo-code-label');
        var oldIcon = icon.outerHTML;
        var oldLabel = label ? label.textContent : '';

        btn.setAttribute('data-code-busy', '1');
        btn.classList.add('is-copied');
        icon.outerHTML = ICON_CHECK;

        if (label) {
            label.textContent = '已复制';
        }

        window.setTimeout(function () {
            var check = btn.querySelector('.demo-code-icon');

            if (check) {
                check.outerHTML = oldIcon;
            }

            if (label) {
                label.textContent = oldLabel;
            }

            btn.classList.remove('is-copied');
            btn.removeAttribute('data-code-busy');
        }, 1400);
    }

    /** 量出示例块的 padding / 底圆角，好让操作条与代码面板抵到块的左右下边缘。 */
    function measureEdges(section) {
        try {
            var cs = window.getComputedStyle(section);

            return {
                pl: parseFloat(cs.paddingLeft) || 0,
                pr: parseFloat(cs.paddingRight) || 0,
                pb: parseFloat(cs.paddingBottom) || 0,
                bl: Math.max((parseFloat(cs.borderBottomLeftRadius) || 0) - 1, 0),
                br: Math.max((parseFloat(cs.borderBottomRightRadius) || 0) - 1, 0)
            };
        } catch (e) {
            return { pl: 22, pr: 22, pb: 20, bl: 7, br: 7 };
        }
    }

    /**
     * 操作条 / 代码面板贴着示例块铺满：
     * - 面板收起时，操作条独自吃下底部圆角；
     * - 面板展开时，操作条底圆角归零，圆角与底部负外边距交给面板。
     */
    function layoutEdges(section, bar, panel, open) {
        var m = measureEdges(section);

        if (bar) {
            bar.style.marginTop = (m.pb > 0 ? 16 : 8) + 'px';
            bar.style.marginLeft = (-m.pl) + 'px';
            bar.style.marginRight = (-m.pr) + 'px';
            bar.style.marginBottom = open ? '0' : (-m.pb) + 'px';
            bar.style.borderRadius = open ? '0' : ('0 0 ' + m.bl + 'px ' + m.br + 'px');
        }

        if (panel) {
            panel.style.marginLeft = (-m.pl) + 'px';
            panel.style.marginRight = (-m.pr) + 'px';
            panel.style.marginBottom = (-m.pb) + 'px';
            panel.style.borderRadius = '0 0 ' + m.bl + 'px ' + m.br + 'px';
        }
    }

    // ---------- 代码渲染（逐行 + 折叠箭头） ----------

    function renderCode(body, code) {
        var lines = String(code || '').replace(/\r\n/g, '\n').split('\n');
        var folds = computeFolds(lines);
        var foldAt = Object.create(null);
        var html = '';
        var i;

        for (i = 0; i < folds.length; i++) {
            foldAt[folds[i].start] = true;
        }

        for (i = 0; i < lines.length; i++) {
            var foldable = foldAt[i] === true;

            html += '<div class="dcl' + (foldable ? ' is-foldable' : '') + '" data-line="' + i + '">'
                + '<span class="dcl-gutter">'
                + (foldable
                    ? '<button type="button" class="dcl-fold" data-fold="' + i + '" aria-expanded="true"'
                        + ' aria-label="折叠或展开第 ' + (i + 1) + ' 行">' + ICON_CHEVRON + '</button>'
                    : '')
                + '</span>'
                + '<code class="dcl-code">' + highlight(lines[i]) + '</code>'
                + (foldable ? '<span class="dcl-more" aria-hidden="true">⋯</span>' : '')
                + '</div>';
        }

        body.innerHTML = html;
        body.__folds = folds;
        body.__collapsed = Object.create(null);
        body.__rendered = true;
    }

    /** 按已折叠集合重算可见行：嵌套折叠只是区间包含，取并集即可，展开/收起互不干扰。 */
    function applyFolds(body) {
        var folds = body.__folds || [];
        var collapsed = body.__collapsed || Object.create(null);
        var hideSet = Object.create(null);
        var i;
        var k;

        for (i = 0; i < folds.length; i++) {
            if (collapsed[folds[i].start]) {
                for (k = folds[i].start + 1; k <= folds[i].end; k++) {
                    hideSet[k] = true;
                }
            }
        }

        var rows = body.querySelectorAll('.dcl');

        for (i = 0; i < rows.length; i++) {
            rows[i].classList.toggle('is-hidden', hideSet[parseInt(rows[i].getAttribute('data-line'), 10)] === true);
        }

        for (i = 0; i < folds.length; i++) {
            var row = body.querySelector('.dcl[data-line="' + folds[i].start + '"]');

            if (!row) {
                continue;
            }

            var isCollapsed = collapsed[folds[i].start] === true;

            row.classList.toggle('is-collapsed', isCollapsed);

            var btn = row.querySelector('.dcl-fold');

            if (btn) {
                btn.setAttribute('aria-expanded', isCollapsed ? 'false' : 'true');
            }
        }
    }

    // ---------- 代码面板（就地展开，不弹窗） ----------

    function switchPane(panel, which) {
        panel.__active = which;

        var tabs = panel.querySelectorAll('[data-code-pane]');
        var i;

        for (i = 0; i < tabs.length; i++) {
            tabs[i].classList.toggle('is-active', tabs[i].getAttribute('data-code-pane') === which);
        }

        var bodies = panel.querySelectorAll('[data-code-body]');

        for (i = 0; i < bodies.length; i++) {
            bodies[i].classList.toggle('is-active', bodies[i].getAttribute('data-code-body') === which);
        }

        var body = panel.querySelector('[data-code-body="' + which + '"]');

        if (body && !body.__rendered) {
            renderCode(body, which === 'page' ? panel.__page : panel.__block);
        }
    }

    function buildPanel(block, page, fileName) {
        var panel = document.createElement('div');

        panel.className = 'demo-code-panel';
        panel.setAttribute('data-demo-code-panel', '1');
        panel.__block = block && block.code ? block.code : '';
        panel.__page = typeof page === 'string' ? page : '';
        panel.__active = 'block';

        var title = block && block.title ? block.title : '示例';

        panel.innerHTML = '<div class="demo-code-panel-head">'
            + '<div class="demo-code-tabs" role="tablist">'
            + '<button type="button" class="demo-code-tab is-active" role="tab" data-code-pane="block">本段示例</button>'
            + '<button type="button" class="demo-code-tab" role="tab" data-code-pane="page">整页源码</button>'
            + '</div>'
            + '<span class="demo-code-file"></span>'
            + '<div class="demo-code-tools">'
            + '<button type="button" class="demo-code-tool" data-code-act="panel-copy" title="复制代码"'
            + ' aria-label="复制代码">' + ICON_COPY + '</button>'
            + '<button type="button" class="demo-code-tool" data-code-act="panel-collapse" title="收起代码"'
            + ' aria-label="收起代码">' + ICON_CODE + '</button>'
            + '</div>'
            + '</div>'
            + '<div class="demo-code-code is-active" data-code-body="block"></div>'
            + '<div class="demo-code-code" data-code-body="page"></div>';

        panel.querySelector('.demo-code-file').textContent = (fileName ? fileName + ' · ' : '') + title;

        var tabs = panel.querySelectorAll('[data-code-pane]');

        for (var i = 0; i < tabs.length; i++) {
            tabs[i].addEventListener('click', function () {
                switchPane(panel, this.getAttribute('data-code-pane'));
            });
        }

        panel.querySelector('[data-code-act="panel-copy"]').addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            var which = panel.__active || 'block';
            var text = which === 'page' ? panel.__page : panel.__block;
            var btn = this;

            if (!text) {
                toast('没有可复制的代码');

                return;
            }

            copyText(text, function () {
                flashCopied(btn);
            }, function () {
                toast('复制失败，请手动选择代码复制');
            });
        });

        panel.querySelector('[data-code-act="panel-collapse"]').addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            var section = panel.closest('.demo-section');

            if (section) {
                setPanelOpen(section, false);
            }
        });

        // 折叠箭头走事件委托，一次绑定（行数多，逐行绑定没必要）
        panel.addEventListener('click', function (e) {
            var t = e.target;
            var btn = t && t.closest ? t.closest('.dcl-fold') : null;

            if (!btn) {
                return;
            }

            e.preventDefault();
            e.stopPropagation();

            var body = btn.closest('[data-code-body]');

            if (!body) {
                return;
            }

            var line = parseInt(btn.getAttribute('data-fold'), 10);

            if (body.__collapsed[line]) {
                delete body.__collapsed[line];
            } else {
                body.__collapsed[line] = true;
            }

            applyFolds(body);
        });

        return panel;
    }

    /** 展开 / 收起某个示例块的代码面板。 */
    function setPanelOpen(section, open) {
        var panel = section.querySelector('[data-demo-code-panel]');
        var bar = section.querySelector('.demo-code-bar');

        if (!panel) {
            return false;
        }

        panel.classList.toggle('is-open', !!open);
        section.classList.toggle('demo-code-has-panel', !!open);

        if (open) {
            switchPane(panel, panel.__active || 'block');   // 首次展开才真正渲染代码
        }

        var viewBtn = bar ? bar.querySelector('[data-code-act="view"]') : null;

        if (viewBtn) {
            var label = viewBtn.querySelector('.demo-code-label');

            viewBtn.classList.toggle('is-active', !!open);

            if (label) {
                label.textContent = open ? '收起代码' : '查看源代码';
            }

            viewBtn.setAttribute('title', open ? '收起代码' : '查看源代码');
            viewBtn.setAttribute('aria-expanded', open ? 'true' : 'false');
        }

        layoutEdges(section, bar, panel, !!open);

        return !!open;
    }

    function addActions(section, block, page, fileName) {
        var bar = document.createElement('div');

        bar.className = 'demo-code-bar';
        bar.setAttribute('data-demo-code-bar', '1');
        bar.innerHTML = barButton('copy', ICON_COPY, '复制代码', '复制本段示例代码')
            + barButton('view', ICON_CODE, '查看源代码', '查看源代码');

        var panel = buildPanel(block, page, fileName);
        var code = block && block.code ? block.code : '';

        bar.querySelector('[data-code-act="copy"]').addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            var btn = this;

            if (!code) {
                toast('本段没有可复制的代码');

                return;
            }

            copyText(code, function () {
                flashCopied(btn);
            }, function () {
                toast('复制失败，请在「查看源代码」里手动选择复制');
            });
        });

        bar.querySelector('[data-code-act="view"]').addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            setPanelOpen(section, !section.classList.contains('demo-code-has-panel'));
        });

        section.appendChild(bar);
        section.appendChild(panel);
        section.setAttribute('data-demo-code', '1');

        layoutEdges(section, bar, panel, false);
    }

    /** 一个示例块是否已经有操作条（Blazor 重渲染可能把它顶掉，所以每次都实测）。 */
    function hasBar(section) {
        for (var i = 0; i < section.children.length; i++) {
            if (section.children[i].className === 'demo-code-bar') {
                return true;
            }
        }

        return false;
    }

    /** 重渲染可能把块上的 demo-code-has-panel 抹掉，这里按面板实际状态补回（幂等，不产生 DOM 改动）。 */
    function syncPanelState(section) {
        var panel = section.querySelector('[data-demo-code-panel]');

        if (!panel) {
            return;
        }

        var open = panel.classList.contains('is-open');

        if (section.classList.contains('demo-code-has-panel') !== open) {
            section.classList.toggle('demo-code-has-panel', open);
        }
    }

    /** 增强某个 keep-alive 页面容器里的全部示例块。返回是否真的插入过节点。 */
    function enhancePage(host, typeName) {
        var page = sources[typeName];

        if (typeof page !== 'string') {
            return false;
        }

        var sections = host.querySelectorAll('.demo-section');
        var pending = [];
        var i;

        for (i = 0; i < sections.length; i++) {
            if (hasBar(sections[i])) {
                syncPanelState(sections[i]);
            } else {
                pending.push({ el: sections[i], index: i });
            }
        }

        if (!pending.length) {
            return false;
        }

        var blocks = extractSections(page);
        var used = [];

        for (i = 0; i < pending.length; i++) {
            addActions(
                pending[i].el,
                pickBlock(blocks, used, pending[i].el, pending[i].index),
                page,
                fileNames[typeName] || '');
        }

        return true;
    }

    /**
     * 给 DOM 里的示例块挑源码片段：先按标题对齐（最稳），标题缺失/重名时退回按顺序。
     */
    function pickBlock(blocks, used, section, index) {
        if (!blocks.length) {
            return null;
        }

        var title = '';
        var heading = section.querySelector('h1,h2,h3,h4,h5,h6');

        if (heading) {
            title = (heading.textContent || '').replace(/\s+/g, ' ').trim();
        }

        var i;

        if (title) {
            for (i = 0; i < blocks.length; i++) {
                if (used.indexOf(i) < 0 && blocks[i].title === title) {
                    used.push(i);

                    return blocks[i];
                }
            }
        }

        if (index < blocks.length && used.indexOf(index) < 0) {
            used.push(index);

            return blocks[index];
        }

        for (i = 0; i < blocks.length; i++) {
            if (used.indexOf(i) < 0) {
                used.push(i);

                return blocks[i];
            }
        }

        return blocks[blocks.length - 1];
    }

    // ---------- 扫描（keep-alive 双阶段挂载 → 用 MutationObserver + 去抖兜住时序） ----------

    function scan() {
        scanQueued = false;

        var hosts = document.querySelectorAll('[data-keep-key]');
        var changed = false;

        for (var i = 0; i < hosts.length; i++) {
            var typeName = typeNameOfKey(hosts[i].getAttribute('data-keep-key'));

            if (sources[typeName] === undefined) {
                continue;
            }

            if (enhancePage(hosts[i], typeName)) {
                changed = true;
            }
        }

        // 插入的操作条本身会再触发一次 MutationObserver 回调，这一轮不会再改 DOM，自然收敛。
        return changed;
    }

    function scheduleScan() {
        if (scanQueued) {
            return;
        }

        scanQueued = true;

        if (scanTimer) {
            window.clearTimeout(scanTimer);
        }

        scanTimer = window.setTimeout(function () {
            scanTimer = 0;
            window.requestAnimationFrame(scan);
        }, 180);
    }

    function initObserver() {
        if (observer || !document.body) {
            return;
        }

        observer = new MutationObserver(function () {
            scheduleScan();
        });

        observer.observe(document.body, { childList: true, subtree: true });
    }

    // ---------- 全局：Esc 收起所有已展开的代码面板 ----------

    function initEscape() {
        document.addEventListener('keydown', function (e) {
            if (e.key !== 'Escape' || e.defaultPrevented) {
                return;
            }

            var open = document.querySelectorAll('.demo-code-panel.is-open');
            var closed = 0;

            for (var i = 0; i < open.length; i++) {
                var section = open[i].closest('.demo-section');

                if (section) {
                    setPanelOpen(section, false);
                    closed++;
                }
            }

            if (closed) {
                e.preventDefault();
            }
        }, true);
    }

    // ---------- 对外接口 ----------

    /**
     * C# 侧下发某个演示页的整份源码（App.razor 里的 DemoCodeHost 调用）。
     * typeName 为页面类型全名，fileName 为 .razor 文件名。
     */
    window.demoCodeSetSource = function (typeName, fileName, text) {
        if (!typeName || typeof text !== 'string') {
            return;
        }

        var changed = sources[typeName] === undefined;

        sources[typeName] = text;
        fileNames[typeName] = fileName || typeName.split('.').pop() + '.razor';
        initObserver();
        scheduleScan();

        return changed;
    };

    /** 供探针/调试手动触发一次扫描。 */
    window.demoCodeScan = function () {
        return scan();
    };

    /** 收起当前所有已展开的代码面板，返回收起的数量。 */
    window.demoCodeClose = function () {
        var open = document.querySelectorAll('.demo-code-panel.is-open');
        var closed = 0;

        for (var i = 0; i < open.length; i++) {
            var section = open[i].closest('.demo-section');

            if (section) {
                setPanelOpen(section, false);
                closed++;
            }
        }

        return closed;
    };

    initEscape();

    // 首次进入也要扫一遍（源码可能先于 DOM 到达，反之亦然）
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            initObserver();
            scheduleScan();
        });
    } else {
        initObserver();
        scheduleScan();
    }
})();
