var markdownEditors = {};
window.initilizeEditor = function (el, mdEditor, height, value, enableSyncScroll) {
    var editor = CodeMirror.fromTextArea(el, {
        lineNumbers: true,
        mode: "markdown",
        highlightFormatting: true,
        foldGutter: true,
        lineWrapping: true,
        matchBrackets: true,
        styleActiveLine: true,
        styleSelectedText: true,
        showTrailingSpace: true,
        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"]
    });
    editor.setValue(value);
    editor.display.wrapper.style.marginTop = "43px";
    editor.display.wrapper.style.height = (height - 42) + "px";
    editor.on("change", function () {
        mdEditor.invokeMethodAsync("refreshPreview", editor.getValue());
    });
    markdownEditors[el] = editor;
    window.initMarkdownBarReveal(editor.display.wrapper.closest(".editormd"));
    if (!enableSyncScroll) {
        return;
    }
    editor.on("scroll", function () {
        var scrollInfo = editor.getScrollInfo();
        mdEditor.invokeMethodAsync("scrollPreview", scrollInfo.top);
    });
};
/**
 * 「鼠标移进某一栏，该栏的竖向滚条才显形」用 class 驱动，不靠 :hover。
 * 原因见 css/editor.css 同段注释：Chromium 155 的真实鼠标移动不会重取「祖先 :hover + 后代滚条伪元素」
 * 的样式（原生光标已在栏内仍读不到墨），合成事件却会 —— 只写 :hover 就是探针绿、用户看不见。
 * mouseover/mouseleave 在两条路径上都会派发，所以显形对真实光标和探针一致。
 * class 必须挂在**滚动元素自身**（编辑栏是 .CodeMirror-vscrollbar/.CodeMirror-hscrollbar 替身，
 * 预览栏是 .editormd-preview）：挂到祖先 .CodeMirror 时伪元素计算值仍是 transparent。
 */
window.initMarkdownBarReveal = function (root) {
    if (!root || root.__mdBarReveal) {
        return;
    }
    root.__mdBarReveal = true;
    var current = [];
    var barsOf = function (node) {
        if (!node || !node.closest) {
            return [];
        }
        var editor = node.closest(".CodeMirror");
        if (editor) {
            return Array.prototype.slice.call(
                editor.querySelectorAll(".CodeMirror-vscrollbar, .CodeMirror-hscrollbar"));
        }
        var preview = node.closest(".editormd-preview");
        return preview ? [preview] : [];
    };
    var moveTo = function (node) {
        var bars = barsOf(node);
        var same = bars.length === current.length && bars.every(function (bar, i) {
            return bar === current[i];
        });
        if (same) {
            return;
        }
        current.forEach(function (bar) { bar.classList.remove("md-bar-hover"); });
        current = bars;
        bars.forEach(function (bar) { bar.classList.add("md-bar-hover"); });
    };
    root.addEventListener("mouseover", function (e) { moveTo(e.target); });
    root.addEventListener("mouseleave", function () { moveTo(null); });
};
window.scrollPreview = function (preview, scrollTop) {
    preview.scrollTop = scrollTop;
};
/**
 * 宿主侧改值后回写编辑器（@bind-Value 的值被 C# 改动时调用）。
 * 正在输入时不动内容，否则 setValue 会把光标重置到最后，打断输入。
 * @returns {boolean} 是否真的写进去了
 */
window.setMarkdownValue = function (el, value) {
    var editor = markdownEditors[el];
    if (!editor) {
        return false;
    }
    if (typeof editor.hasFocus === 'function' && editor.hasFocus()) {
        return false;
    }
    editor.setValue(value == null ? '' : value);
    return true;
};
window.wrapSelection = function (el, prefix, suffix) {
    var editor = this.markdownEditors[el];
    var selection = editor.getSelection();
    if (!selection) {
        selection = "内容";
    }
    editor.replaceSelection(prefix + selection + suffix);
};
window.replaceSelection = function (el, content) {
    var editor = this.markdownEditors[el];
    editor.replaceSelection(content);
};
window.append = function (el, content) {
    var editor = this.markdownEditors[el];
    editor.replaceSelection(content);
};
window.getSelection = function (el) {
    var editor = this.markdownEditors[el];
    var selection = editor.getSelection();
    return selection;
};