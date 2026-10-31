window.getChild = function (el, idx) {
    if (!el) {
        return;
    }
    return el.children[idx];
};
window.getClientHeight = function (el) {
    if (!el) {
        return this.document.body.clientHeight;
    }
    return el.clientHeight || 0;
};
window.submitForm = function (el, url) {
    if (!el) {
        return;
    }

    el.method = "post";
    el.action = url;
    el.submit();
};
window.execFocus = function (el) {
    if (!el) {
        return;
    }
    el.focus();
}
window.execBlur = function (el) {
    if (!el) {
        return;
    }
    el.blur();
}
window.execSelect = function (el) {
    if (!el || !el.select) {
        return;
    }
    el.select();
}
/**
 * 让选择器在键盘导航时不要滚动页面（对齐 Element Plus：上下键只在下拉里移动高亮项）。
 *
 * 原生方向键 / PageUp / PageDown / Home / End 的默认行为是滚动最近的可滚动祖先，
 * 在页面或表格里表现为「按上下键页面跟着动了」；Enter 在表单内会提交表单。
 * Blazor 侧无法按键动态 preventDefault（@onkeydown:preventDefault 是渲染期静态值，
 * 一旦开启会连 Tab 一起吞掉，破坏焦点流转），所以在这里按 key 精确拦截。
 *
 * 只拦截导航键 + Enter，放行 Tab（必须保留原生焦点移动）与字符键（筛选输入）。
 * 事件继续向上冒泡，组件自身的键盘处理（高亮移动 / 回车选中）不受影响。
 *
 * @param {HTMLElement} element ElSelect 根节点（keydown 从内层 input 冒泡到它）
 */
window.elementSelectLockNavScroll = function (element) {
    if (!element || element.__elSelectNavScrollLocked) {
        return;
    }
    element.__elSelectNavScrollLocked = true;
    element.addEventListener('keydown', function (e) {
        var key = e.key;
        if (key !== 'ArrowUp' && key !== 'ArrowDown'
            // ←/→ 供 ElCascader 在多列之间左右移动（默认行为是横向滚动）
            && key !== 'ArrowLeft' && key !== 'ArrowRight'
            && key !== 'PageUp' && key !== 'PageDown'
            && key !== 'Home' && key !== 'End' && key !== 'Enter') {
            return;
        }
        if (e.cancelable) {
            e.preventDefault();
        }
    });
};
/**
 * 分裂按钮的箭头键不滚页：焦点在右侧箭头按钮上时，↑/↓ 只用于展开菜单并移动高亮，
 * Home/End/PageUp/PageDown 也不该把整页带走。Enter/空格/Tab 一律放行
 * （前者要触发按钮点击，后者要保留原生焦点流转）。
 * @param {HTMLElement} element 箭头按钮
 */
window.elementSplitButtonLockNavScroll = function (element) {
    if (!element || element.__elSplitButtonNavScrollLocked) {
        return;
    }
    element.__elSplitButtonNavScrollLocked = true;
    element.addEventListener('keydown', function (e) {
        var key = e.key;
        if (key !== 'ArrowUp' && key !== 'ArrowDown' && key !== 'Home' && key !== 'End'
            && key !== 'PageUp' && key !== 'PageDown') {
            return;
        }
        if (e.cancelable) {
            e.preventDefault();
        }
    });
};
/**
 * 把焦点移到表格编辑单元格内的编辑器上（ElTable 编辑状态回车跳转用）。
 * @param {string} cellId 单元格 DOM id
 * @param {boolean} selectText 是否全选文本，默认 true
 */
window.elementTableFocusCell = function (cellId, selectText) {
    if (!cellId) {
        return false;
    }
    var cell = document.getElementById(cellId);
    if (!cell) {
        return false;
    }
    var el = cell.querySelector('input:not([type=hidden]):not([disabled]), textarea:not([disabled]), select:not([disabled]), [contenteditable="true"]');
    if (!el) {
        el = cell.querySelector('[tabindex], button');
    }
    if (!el) {
        cell.setAttribute('tabindex', '-1');
        el = cell;
    }
    el.focus();
    if (selectText !== false && el.tagName && (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA') && typeof el.select === 'function') {
        try {
            el.select();
        } catch (e) {
        }
    }
    if (el.scrollIntoView) {
        try {
            el.scrollIntoView({ block: 'nearest', inline: 'nearest' });
        } catch (e) {
        }
    }
    return document.activeElement === el;
}

/**
 * 常驻编辑器表格（ElTable 的 IsEditable=false，模板列把输入框直接画在每个单元格里）里按回车跳格。
 * 先沿当前列往下走一行；已经到底就换到右边一列、落在这**一列的第一行**（批次㊾ 之前是留在当前行）。
 * 右边第一列若整列都没有可聚焦编辑器，就继续往右找下一列，仍然从它的首行开始。
 * Shift + 回车反向走一行，到顶不再往左换列（与 C# 侧 MoveEditingCellSameColumnAsync 的边界口径一致）。
 * @param {boolean} shift 是否反向
 * @param {boolean} carryValue 批次 80：只在「换列」那一跳（末行回车 → 右边一列的首行）把来源格的内容带进目标格。
 *   同列往下走一行**不**带值：那是逐行录入的常态，带值会把下一行已有的数据覆盖掉。
 * @returns {boolean} 是否发生了跳转
 */
window.elementTableEnterNextEditor = function (shift, carryValue) {
    var current = document.activeElement;
    if (!current || !current.closest) {
        return false;
    }
    var cell = current.closest('td');
    var row = cell && cell.parentElement;
    var body = row && row.parentElement;
    if (!body || !body.children) {
        return false;
    }
    var rowIndex = Array.prototype.indexOf.call(body.children, row);
    var colIndex = Array.prototype.indexOf.call(row.children, cell);
    if (rowIndex < 0 || colIndex < 0) {
        return false;
    }
    // 来源格只取「用户真能打字」的框：下拉/日期那类 readonly 触发框里是显示文本，带过去不是合法选项。
    // 空值不带 —— 带过去等于把目标格里已有的数据抹掉，那不是「带过来」的意思。
    var carried = carryValue && current.readOnly !== true && typeof current.value === 'string' && current.value !== ''
        ? current.value
        : null;
    var editorSelector = 'input:not([type=hidden]):not([disabled]), textarea:not([disabled]), select:not([disabled])';
    var focusEditor = function (td, carry) {
        var candidates = td && td.querySelectorAll ? td.querySelectorAll(editorSelector) : [];
        var el = null;
        for (var i = 0; i < candidates.length; i++) {
            var candidate = candidates[i];
            // EP 的下拉/日期/数字步进是把选择框做成 readonly input，这类要能跳进去；
            // 其余 readonly input（纯展示）不算编辑器，跳过。
            if (candidate.readOnly && candidate.tagName === 'INPUT'
                && !(candidate.closest && candidate.closest('.el-select, .el-date-editor, .el-time-picker, .el-input-number'))) {
                continue;
            }
            el = candidate;
            break;
        }
        if (!el) {
            return false;
        }
        el.focus();
        var writable = !!carry
            && carried !== null
            && (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA')
            && el.readOnly !== true && el.disabled !== true;
        if (writable) {
            // 直接改 DOM 不回写 .NET 的话，下一次渲染就会把旧值刷回来，所以补发 input 走 @oninput 链路
            // （同本文件 ElIpAddressGrow 的 write()：见「值模型的唯一权威在 C# 侧」那段）。
            el.value = carried;
            try {
                el.dispatchEvent(new Event('input', { bubbles: true }));
            } catch (e) {
            }
        }
        if (typeof el.select === 'function') {
            try {
                // 没带值时保持既有口径：进格全选，第一个键就把原内容替换掉。
                // 带值时不选：内容是给人接着改的，全选会让第一个键把刚带过来的值打掉。
                if (!writable) {
                    el.select();
                } else {
                    var end = el.value.length;
                    el.setSelectionRange(end, end);
                }
            } catch (e) {
            }
        }
        if (el.scrollIntoView) {
            try {
                el.scrollIntoView({ block: 'nearest', inline: 'nearest' });
            } catch (e) {
            }
        }
        return document.activeElement === el;
    };
    var targetRowIndex = rowIndex + (shift ? -1 : 1);
    if (targetRowIndex >= 0 && targetRowIndex < body.children.length) {
        return focusEditor(body.children[targetRowIndex].children[colIndex], false);
    }
    if (shift) {
        return false;
    }
    for (var colIndex2 = colIndex + 1; colIndex2 < row.children.length; colIndex2++) {
        for (var firstRowIndex = 0; firstRowIndex < body.children.length; firstRowIndex++) {
            if (focusEditor(body.children[firstRowIndex].children[colIndex2], true)) {
                return true;
            }
        }
    }
    return false;
}

/**
 * 判断表格编辑行失焦时是否应触发「离开即保存」。
 * @param {Element} container 表格根容器
 * @returns {boolean} true 表示焦点已离开当前编辑行、应自动保存
 */
window.elementShouldAutoSaveOnBlur = function (container) {
    // 记录「最近一次 mousedown 的落点与时刻」。
    // 点击下拉箭头 / 日期图标等**不可聚焦**元素时，浏览器会把 activeElement 变成 body，
    // 此时只看 activeElement 会误判成「焦点离开了当前行」→ 编辑器刚被点开就被自动保存并关掉
    // （现象：点 ElSelect 下拉不弹选择框，反而退出了编辑态）。用「刚按下的位置」补这一票。
    if (container && !container.__elAutoSaveBlurHooked) {
        container.__elAutoSaveBlurHooked = true;
        container.addEventListener('mousedown', function (ev) {
            container.__elLastMouseDown = ev.target;
            container.__elLastMouseDownAt = Date.now();
        }, true);
    }

    var active = document.activeElement;
    if (!active || !active.closest) {
        // 焦点彻底离开页面（如切换到地址栏）→ 视为离开当前行，保存
        return true;
    }
    if (active.closest('.el-popper, .el-select-dropdown, .el-picker__panel, .el-dropdown-menu, .el-cascader__dropdown, .el-table__dropdown')) {
        // 焦点进入了下拉/日期选择等弹出层 → 不保存，避免打断编辑器
        return false;
    }
    // 当前正在编辑的单元格带 .el-table__cell--editing 标记，取其所在行。
    var editingCell = document.querySelector('.el-table__cell--editing');
    var editingRowEl = editingCell ? editingCell.closest('tr') : null;

    // 刚（同一次交互内）在「当前编辑行」上按过鼠标 —— 用户是在行内操作（点下拉箭头、换格），
    // 不是真的离开，不保存；把编辑态交给编辑器自己处理（例如展开下拉面板）。
    // 限定 800ms 是为了不误伤「用 Tab / 鼠标点别处」这类真正的离开（此时没有新的 mousedown）。
    var downAt = container ? container.__elLastMouseDownAt : 0;
    var down = container ? container.__elLastMouseDown : null;
    if (editingRowEl && down && editingRowEl.contains(down) && (Date.now() - downAt) < 800) {
        return false;
    }
    // 焦点仍在「当前编辑行」内部（例如同行动格间切换）→ 不保存，继续编辑。
    if (editingRowEl && editingRowEl.contains(active)) {
        return false;
    }
    // 其余：切到别的行、或落到表格外的控件（工具栏按钮等）→ 离开当前行，自动保存
    return true;
};
/**
 * 根据鼠标坐标取得所在单元格在行内的 DOM 序号（ElTable 双击定位起始编辑单元格用）。
 * @param {number} x 鼠标的 clientX
 * @param {number} y 鼠标的 clientY
 */
window.elementTableColumnIndexAt = function (x, y) {
    if (typeof x !== 'number' || typeof y !== 'number') {
        return -1;
    }
    if (typeof document.elementFromPoint !== 'function') {
        return -1;
    }
    var el = document.elementFromPoint(x, y);
    if (!el || !el.closest) {
        return -1;
    }
    var cell = el.closest('td');
    if (!cell || !cell.parentElement) {
        return -1;
    }
    return Array.prototype.indexOf.call(cell.parentElement.children, cell);
}
window.elementMentionGetSelection = function (el) {
    if (!el) {
        return [0, 0];
    }
    return [el.selectionStart || 0, el.selectionEnd || 0];
}
window.elementMentionSetSelection = function (el, start, end) {
    if (!el || !el.setSelectionRange) {
        return;
    }
    el.setSelectionRange(start || 0, end || 0);
}
window.scrollElementIntoViewById = function (id, options) {
    if (!id) {
        return;
    }
    var el = document.getElementById(id);
    if (!el) {
        return;
    }
    if (options) {
        try {
            el.scrollIntoView(JSON.parse(options));
            return;
        } catch (e) {
        }
    }
    el.scrollIntoView({ block: "nearest", inline: "nearest" });
}
window.elementConfigApplyNamespace = function (root, namespace, defaultNamespace) {
    if (!root || !namespace || !defaultNamespace || namespace === defaultNamespace) {
        return;
    }

    var previousNamespace = root.dataset.elementNamespace || defaultNamespace;
    root.dataset.elementNamespace = namespace;

    var rewriteElement = function (el) {
        if (!el || !el.classList || root.__elementApplyingNamespace) {
            return;
        }

        root.__elementApplyingNamespace = true;
        try {
            Array.from(el.classList).forEach(function (className) {
                var prefix = null;
                if (className.indexOf(defaultNamespace + "-") === 0) {
                    prefix = defaultNamespace + "-";
                } else if (previousNamespace && className.indexOf(previousNamespace + "-") === 0) {
                    prefix = previousNamespace + "-";
                }

                if (!prefix) {
                    return;
                }

                el.classList.remove(className);
                el.classList.add(namespace + "-" + className.substring(prefix.length));
            });
        } finally {
            root.__elementApplyingNamespace = false;
        }
    };

    var rewriteTree = function (el) {
        rewriteElement(el);
        if (!el || !el.querySelectorAll) {
            return;
        }
        el.querySelectorAll("[class]").forEach(rewriteElement);
    };

    rewriteTree(root);
    if (root.__elementNamespaceObserver) {
        root.__elementNamespaceObserver.disconnect();
    }

    root.__elementNamespaceObserver = new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            if (mutation.type === "attributes") {
                rewriteElement(mutation.target);
                return;
            }

            mutation.addedNodes.forEach(function (node) {
                if (node.nodeType === 1) {
                    rewriteTree(node);
                }
            });
        });
    });
    root.__elementNamespaceObserver.observe(root, {
        subtree: true,
        childList: true,
        attributes: true,
        attributeFilter: ["class"]
    });
};
window.upload = function (el) {
    if (!el) {
        return;
    }
    var input = el.querySelector && el.querySelector('input[type="file"]');
    if (!input || input.disabled) {
        return;
    }
    return input.click();
};

window.uploadDrop = function (event, el) {
    if (event && event.preventDefault) {
        event.preventDefault();
    }
    if (!el || !event || !event.dataTransfer) {
        return;
    }
    var input = el.querySelector && el.querySelector('input[type="file"]');
    if (!input || input.disabled) {
        return;
    }
    input.files = event.dataTransfer.files;
    input.dispatchEvent(new Event("change", { bubbles: true }));
};

// 粘贴上传的登记表：一条 document 级监听 + 每个 ElUpload 实例一条表项。
// 旧实现是 uploader/uploadUrl/uploadOptions 三个全局变量，后注册的直接把前一个盖掉
//（同页两个上传组件时粘贴只会落到最后注册的那个），而且 url/options 在注册那一帧就冻结了
//（对话框里 Url/Data 后填或改动，粘贴还在用旧值）。
// 现在表项只存 DotNetObjectReference + 触发区元素 + 句柄，url/options 每次粘贴现问 .NET。
var pasteUploadEntries = [];
var pasteUploadListening = false;
window.clear = function (el) {
    if (!el) {
        return;
    }
    el.value = null;
};

function pasteUploadHost(entry) {
    var el = entry && entry.el;
    if (!el || !el.isConnected) {
        return null;
    }
    return (el.closest && el.closest('.el-upload')) || el;
}

function pickPasteUploadEntry() {
    var visible = [];
    for (var i = 0; i < pasteUploadEntries.length; i++) {
        var host = pasteUploadHost(pasteUploadEntries[i]);
        // 禁用的那台不收粘贴：点不了的文件框，粘贴却能绕过 Disabled 直接发上传（查看态的对话框就是这样被穿过去的）
        if (host && host.getClientRects().length > 0 && !pasteUploadEntries[i].el.disabled) {
            visible.push(pasteUploadEntries[i]);
        }
    }
    if (visible.length <= 1) {
        return visible[0] || null;
    }
    // 同页多个可见上传区：焦点落在哪个就用哪个，都不含焦点时按注册顺序取第一个（口径固定，便于复现）
    var active = document.activeElement;
    if (active) {
        for (var j = 0; j < visible.length; j++) {
            var box = pasteUploadHost(visible[j]);
            if (box === active || box.contains(active)) {
                return visible[j];
            }
        }
    }
    return visible[0];
}

async function executePasteUpload(event) {
    var items = event.clipboardData && event.clipboardData.items;
    if (!items || !items.length) {
        return;
    }
    var files = [];
    for (var i = 0; i < items.length; i++) {
        if (items[i].kind != "file") {
            continue;
        }
        var file = items[i].getAsFile();
        if (file) {
            files.push(file);
        }
    }
    if (!files.length) {
        return;
    }

    var entry = pickPasteUploadEntry();
    if (!entry) {
        return;
    }

    var options = await entry.upload.invokeMethodAsync("getPasteUploadOptions") || {};
    if (!options.url) {
        return;
    }
    event.preventDefault();
    if (!options.multiple && files.length > 1) {
        files = files.slice(0, 1);
    }

    // previewFiles 返回的 id 与 files 同长同序：被扩展名/尺寸/数量上限挡掉的那一格是空串，跳过即可
    //（旧实现返回的是「已接受」的紧凑数组，和 files 下标对不上，多文件粘贴会把结果挂到错的那一行）
    var ids = await convertFiles(files).then(async fs => {
        return await entry.upload.invokeMethodAsync("previewFiles", fs);
    });
    for (var j = 0; j < files.length; j++) {
        var id = ids && ids[j];
        if (!id || options.autoUpload === false) {
            continue;
        }
        var result = await new Promise(resolver => {
            _uploadFile(options.url, files[j], resolver, options);
        });
        await entry.upload.invokeMethodAsync("fileUploaded", result, id);
    }
    if (options.autoUpload !== false) {
        await entry.upload.invokeMethodAsync("filesUploaded");
    }
};
window.registerPasteUpload = function (upload, el, handle) {
    if (!upload || !el) {
        return;
    }
    window.unRegisterPasteUpload(handle);
    pasteUploadEntries.push({ upload: upload, el: el, handle: handle });
    if (!pasteUploadListening) {
        document.addEventListener('paste', executePasteUpload);
        pasteUploadListening = true;
    }
};
window.unRegisterPasteUpload = function (handle) {
    pasteUploadEntries = pasteUploadEntries.filter(function (entry) {
        return entry.handle !== handle;
    });
    if (!pasteUploadEntries.length && pasteUploadListening) {
        document.removeEventListener('paste', executePasteUpload);
        pasteUploadListening = false;
    }
};
function convertFiles(files) {
    let scanFile = function (file) {
        return new Promise((resolver) => {
            let infos = [file.name, file.size.toString()];
            if (file.type.indexOf("image") != -1) {
                let img = new Image();
                img.onload = function () {
                    infos.push(this.width.toString());
                    infos.push(this.height.toString());
                    infos.push(img.src);
                    resolver(infos);
                };
                img.src = URL.createObjectURL(file);
                return;
            }
            resolver(infos);
        });
    };
    let filePromises = [];
    for (var i = 0; i < files.length; i++) {
        let file = files.item ? files.item(i) : files[i];

        filePromises.push(new Promise((resolver) => {
            scanFile(file).then(infos => {
                resolver(infos);
            });
        }));
    }
    return Promise.all(filePromises);
};
window.scanFiles = function (el) {
    if (!el) {
        return [];
    }

    let files = this.convertFiles(el.files);
    return files;
};
function isSameOriginUrl(url) {
    try {
        var a = document.createElement('a');
        a.href = url;
        return a.protocol === window.location.protocol && a.host === window.location.host;
    } catch (e) {
        return false;
    }
}

async function _uploadFile(url, file, callback, options) {
    options = options || {};
    let xhr = new XMLHttpRequest();
    xhr.open((options.method || "POST").toUpperCase(), url);
    xhr.onreadystatechange = function () {
        if (this.readyState != 4) {
            return;
        }
        let raw = this.responseText || "";
        if (this.status < 200 || this.status >= 300) {
            callback(["1", this.statusText || "Upload failed.", "", "", raw]);
            return;
        }
        // 后端返回键不是 {code,message,id,url} 这一族时，旧实现直接 response.code.toString() 抛 TypeError：
        // 异常在 onreadystatechange 里没人接，Promise 永不 resolve，.NET 侧那道 await 就永远挂着
        //（表现为选完文件转圈不结束，控制台一句错都不留）。现在一律兜成失败，并把原始响应体带给 .NET，
        // 键名不同的宿主用 ElUpload.ResponseParser 自己映射。
        let response = null;
        try {
            response = JSON.parse(raw);
        } catch (e) {
            response = null;
        }
        if (!response) {
            callback(["1", "Upload response is not valid JSON.", "", "", raw]);
            return;
        }
        if (response.code === undefined || response.code === null) {
            callback(["1", "Upload response has no \"code\" field.", "", "", raw]);
            return;
        }
        callback([String(response.code), response.message || "",
            response.id === undefined || response.id === null ? "" : String(response.id),
            response.url === undefined || response.url === null ? "" : String(response.url),
            raw]);
    };
    xhr.onerror = function () {
        callback(["1", "Upload failed.", "", "", ""]);
    };
    xhr.withCredentials = options.withCredentials !== false;
    // x-requested-with 只在同源时加：跨源带上这个自定义头就不是「简单请求」了，浏览器会先打一次
    // OPTIONS 预检，宿主后端没放通这个请求头整条上传直接失败；而宿主自己的 HttpClient 那条路不发这个头，
    // 所以同一个后端用 HttpClient 能传、用本组件传不上。宿主通过 Headers 显式要求的头照发。
    if (isSameOriginUrl(url)) {
        xhr.setRequestHeader("x-requested-with", "XMLHttpRequest");
    }
    if (options.headers) {
        Object.keys(options.headers).forEach(function (key) {
            xhr.setRequestHeader(key, options.headers[key]);
        });
    }
    let formData = new FormData();
    if (options.data) {
        Object.keys(options.data).forEach(function (key) {
            formData.append(key, options.data[key]);
        });
    }
    formData.append(options.fileFieldName || "fileContent", file);
    xhr.send(formData);
};
window.uploadFile = function (el, fileName, url, options) {
    return new Promise((resolver, reject) => {
        let file = null;
        for (var i = 0; i < el.files.length; i++) {
            file = el.files[i];
            if (file.name == fileName) {
                break;
            }
        }
        _uploadFile(url, file, resolver, options);
    });
    //const temporaryFileReader = new FileReader();
    //return new Promise((resolve, reject) => {
    //    temporaryFileReader.onerror = () => {
    //        temporaryFileReader.abort();
    //        reject(new DOMException("Problem parsing input file."));
    //    };
    //    temporaryFileReader.addEventListener("load", function () {
    //        var data = [el.value, temporaryFileReader.result.split(',')[1]];
    //        resolve(data);
    //    }, false);
    //    temporaryFileReader.readAsDataURL(el.files[0]);
    //});
};
window.trigger = function (el, eventName) {
    var eventClass = "";

    // Different events have different event classes.
    // If this switch statement can't map an eventName to an eventClass,
    // the event firing is going to fail.
    switch (eventName) {
        case "click": // Dispatching of 'click' appears to not work correctly in Safari. Use 'mousedown' or 'mouseup' instead.
        case "mousedown":
        case "mouseup":
            eventClass = "MouseEvents";
            break;

        case "focus":
        case "change":
        case "blur":
        case "select":
            eventClass = "HTMLEvents";
            break;

        default:
            throw "fireEvent: Couldn't find an event class for event '" + eventName + "'.";
            break;
    }
    let event = this.document.createEvent(eventClass);
    event.initEvent(eventName);
    el.dispatchEvent(event);
};
window.getClientWidth = function (el) {
    if (!el) {
        return this.document.body.clientWidth;
    }
    return el.clientWidth || 0;
};
window.getOffsetLeft = function (el) {
    if (!el) {
        return 0;
    }
    return el.offsetLeft || 0;
};
window.setDisabled = function (el, disabled) {
    if (!el) {
        return;
    }
    el.disabled = disabled;
};
window.getOffsetTop = function (el) {
    if (!el) {
        return 0;
    }
    return el.offsetTop || 0;
};
window.getPaddingLeft = function (el) {
    if (!el) {
        return 0;
    }
    return parseInt(getComputedStyle(el, null)["padding-left"]);
};
window.getPaddingRight = function (el) {
    if (!el) {
        return 0;
    }
    return parseInt(getComputedStyle(el, null)["padding-right"]);
};
window.disableXScroll = function () {
    this.document.body.style.overflowX = "hidden";
};
window.disableYScroll = function () {
    this.document.body.style.overflowY = "hidden";
};
window.enableXScroll = function () {
    this.document.body.style.overflowX = "auto";
};
window.enableYScroll = function () {
    this.document.body.style.overflowY = "auto";
};
window.removeSelf = function (el) {
    if (!el) {
        return;
    }
    el.parentNode.removeChild(el);
};
window.documentAppendChild = function (el) {
    if (!el) {
        return;
    }
    document.body.append(el);
};
// 预览层 teleport：把元素挂到 body（脱离组件 DOM 子树，避免被祖先的 transform/overflow 裁剪）。
// 先记录原始父节点与相邻兄弟，restore 时精确归位，Blazor 才能在正确的父节点上做 diff/移除。
window.elementTeleportToBody = function (el) {
    if (!el || el.__portalParent) {
        return;
    }
    el.__portalParent = el.parentNode;
    el.__portalNext = el.nextSibling;
    document.body.append(el);
};
window.elementTeleportRestore = function (el) {
    if (!el || !el.__portalParent) {
        return;
    }
    el.__portalParent.insertBefore(el, el.__portalNext);
    el.__portalParent = null;
    el.__portalNext = null;
};
window.elementAppendChild = function (parent, child) {
    if (!parent || !child) {
        return 0;
    }
    parent.append(child);
};
// EP service.mjs addClassList 口径：遮罩 .el-loading-mask 是 position:absolute，
// 定位上下文由「被挂遮罩的那个元素」提供；EP 只在它的 computed position 属于
// absolute/fixed/sticky 之外的值（即 static/relative）时补 el-loading-parent--relative，
// 已经是那三种之一就不动它（否则会把人家原本的定位方式改写成 relative）。
window.elementLoadingParentRelative = function (el, on) {
    if (!el || !el.classList) {
        return;
    }
    if (!on) {
        el.classList.remove("el-loading-parent--relative");
        return;
    }
    var position = window.getComputedStyle(el).position;
    if (position === "absolute" || position === "fixed" || position === "sticky") {
        el.classList.remove("el-loading-parent--relative");
    } else {
        el.classList.add("el-loading-parent--relative");
    }
};
window.getTopRelativeBody = function (el) {
    if (!el) {
        return 0;
    }
    var bodyRect = document.body.getBoundingClientRect(),
        elemRect = el.getBoundingClientRect(),
        offset = elemRect.top - bodyRect.top;
    return this.parseFloat(offset);
};
window.getLeftRelativeBody = function (el) {
    if (!el) {
        return 0;
    }
    var bodyRect = document.body.getBoundingClientRect(),
        elemRect = el.getBoundingClientRect(),
        offset = elemRect.left - bodyRect.left;
    return this.parseFloat(offset);
};
window.isScrollAtEnd = function (el) {
    if (!el) {
        return false;
    }
    return el.scrollTop + el.clientHeight >= el.scrollHeight - 1;
};
window.getScrollLeft = function (el) {
    if (!el) {
        return 0;
    }
    return el.scrollLeft || 0;
};
window.setScrollLeft = function (el, value) {
    if (!el) {
        return;
    }
    el.scrollLeft = value || 0;
};
window.Remove = function (el) {
    if (!el) {
        return;
    }
    el.remove();
};
window.getLeft = function (el) {
    if (!el) {
        return 0;
    }
    return parseInt(getComputedStyle(el, null)["left"]);
};
window.getTop = function (el) {
    if (!el) {
        return 0;
    }
    return parseInt(getComputedStyle(el, null)["top"]);
};
window.getBoundingClientRect = function (el) {
    if (!el) {
        return {};
    }
    return el.getBoundingClientRect();
};
window.setTransform = function (el, value) {
    if (!el) {
        return;
    }
    el.style.transform = value;
};
window.setStyle = function (el, key, value) {
    if (!el) {
        return;
    }
    el.style[key] = value;
};
window.getMarginTop = function (el) {
    if (!el) {
        return "";
    }
    return el.style["margin-top"];
};
window.clearStyle = function (el, key) {
    if (!el) {
        return;
    }
    el.style[key] = "";
};
window.setTransitionAsync = function (el, value) {
    if (!el) {
        return;
    }
    el.style.transition = value;
};
window.getDomGuid = function (el) {
    for (var index in el.attributes) {
        var guid = el.attributes[index].name;
        if (!guid) {
            continue;
        }
        if (guid.indexOf("_bl_") == 0) {
            return guid;
        }
    }
};
window.onblazortransitionend = function (e) {
    var instance = window[getDomGuid(e.target)];
    if (!instance) {
        return;
    }
    console.log("end:");
    console.dir(instance)

    return instance.invokeMethodAsync('AnimationEnd', e.propertyName);
}
// Loading service: body scroll lock
window.elementLoadingLock = function () {
    document.body.style.overflow = 'hidden';
};
window.elementLoadingUnlock = function () {
    document.body.style.overflow = '';
};
window.RegisterAnimationBegin = function (transitionRef, el) {
    window[getDomGuid(el)] = transitionRef;
    el.addEventListener('transitionend', onblazortransitionend);
}
window.UnregisterAnimationBegin = function (el) {
    if (el) {
        el.removeEventListener('transitionend', onblazortransitionend);
        var guid = getDomGuid(el);
        if (window[guid]) { delete window[guid]; }
    }
}
/* ElScrollbar：自定义滚动条 .el-scrollbar__bar / __thumb 是 .el-scrollbar__wrap 的**兄弟节点**，
   浮在 wrap 之上（bar 1px 轨道，thumb 因 min-width/min-height:20px 实际命中区约 3px）。
   滚轮落在 bar 上时 wrap 收不到 wheel → 事件沿 .el-scrollbar 冒泡到页面 ⇒ 整页跟着滚，
   而 wrap 自身纹丝不动。这里在根元素接管「落在 bar 上的滚轮」，手动滚到 wrap 并 preventDefault。
   落在 wrap / view 上的滚轮不进此分支，仍走浏览器原生滚动。 */
window.elementScrollbarBindWheel = function (root, wrap) {
    if (!root || !wrap) {
        return;
    }
    // wrap 可能随重渲染被替换，每次都刷新引用
    root.__elScrollbarWrap = wrap;
    if (root.__elScrollbarWheelBound) {
        return;
    }
    root.__elScrollbarWheelBound = true;

    var onWheel = function (e) {
        var target = e.target;
        var bar = target && target.closest ? target.closest('.el-scrollbar__bar') : null;
        if (!bar || !root.contains(bar)) {
            return; // 鼠标在 wrap / view 上：交给原生滚动
        }
        if (e.ctrlKey) {
            return; // 捏合缩放放行
        }
        var w = root.__elScrollbarWrap;
        if (!w) {
            return;
        }
        var dx, dy;
        if (e.deltaMode === 1) {          // DOM_DELTA_LINE
            dx = e.deltaX * 16;
            dy = e.deltaY * 16;
        } else if (e.deltaMode === 2) {   // DOM_DELTA_PAGE
            dx = e.deltaX * w.clientWidth;
            dy = e.deltaY * w.clientHeight;
        } else {                          // DOM_DELTA_PIXEL
            dx = e.deltaX;
            dy = e.deltaY;
        }
        if (!dx && !dy) {
            return;
        }
        e.preventDefault();
        if (dy) { w.scrollTop = (w.scrollTop || 0) + dy; }
        if (dx) { w.scrollLeft = (w.scrollLeft || 0) + dx; }
    };
    root.__elScrollbarWheelHandler = onWheel;
    root.addEventListener('wheel', onWheel, { passive: false });
};
/* 批次㉖：弹层「点外部关闭」的那张全屏透明遮罩（.v-modal，opacity:0）会把滚轮的滚动链
   从页面滚动容器拐到视口 —— 遮罩自己不可滚、祖先也不可滚，于是指针只要落在弹层之外
   （= 整屏都被它盖着）页面就一动不动。官方 EP 根本没有这一层（它用 document 上的
   click-outside 指令），滚轮在弹层外照常滚页。这里在 document 上把滚轮「穿」到遮罩底下
   那一层，按原生滚动链的语义逐级找可滚容器；找不到就放行交给视口（宿主自己滚）。
   真遮罩（dialog / drawer 的 .v-modal.el-overlay，opacity≠0）不在此列 —— 模态就该锁住后面。 */
(function () {
    // 已经到底、能滚视口的那些层：不再往下找，交给浏览器原生
    var viewportHosts = [document.body, document.documentElement];

    function canScroll(el, axis, dir) {
        var cs = getComputedStyle(el);
        var ovf = axis === 'y' ? cs.overflowY : cs.overflowX;
        if (ovf !== 'auto' && ovf !== 'scroll') {
            return false;
        }
        if (axis === 'y') {
            return dir > 0 ? el.scrollTop + el.clientHeight < el.scrollHeight - 1 : el.scrollTop > 0;
        }
        return dir > 0 ? el.scrollLeft + el.clientWidth < el.scrollWidth - 1 : el.scrollLeft > 0;
    }

    // 逐级上溯 = 复刻原生「滚不动就交给父级」的滚动链；到边界就继续爬，别停在第一层
    function scrollHostUnder(el, axis, dir) {
        var node = el;
        while (node && node.nodeType === 1 && viewportHosts.indexOf(node) < 0) {
            if (canScroll(node, axis, dir)) {
                return node;
            }
            node = node.parentElement;
        }
        return null;
    }

    function toPixels(e, host) {
        if (e.deltaMode === 1) {            // DOM_DELTA_LINE
            return { x: e.deltaX * 16, y: e.deltaY * 16 };
        }
        if (e.deltaMode === 2) {            // DOM_DELTA_PAGE
            return { x: e.deltaX * host.clientWidth, y: e.deltaY * host.clientHeight };
        }
        return { x: e.deltaX, y: e.deltaY };
    }

    document.addEventListener('wheel', function (e) {
        var shadow = e.target;
        if (!shadow || !shadow.classList || !shadow.classList.contains('v-modal')) {
            return;
        }
        if (getComputedStyle(shadow).opacity !== '0') {
            return;
        }
        if (e.ctrlKey || (!e.deltaY && !e.deltaX)) {
            return;
        }
        var stack = document.elementsFromPoint(e.clientX, e.clientY);
        var under = null;
        for (var i = 0; i < stack.length; i++) {
            var el = stack[i];
            // 跳过遮罩本身、以及装着遮罩与浮层的那个 body 级宿主（它铺满视口，会被当成「底下的页面」）
            if (el === shadow || el.contains(shadow)) {
                continue;
            }
            if (el.classList && (el.classList.contains('v-modal') || el.classList.contains('el-overlay'))) {
                continue;
            }
            if (viewportHosts.indexOf(el) >= 0) {
                break;
            }
            under = el;
            break;
        }
        if (!under) {
            return;
        }
        var axis = e.deltaY ? 'y' : 'x';
        var dir = (e.deltaY || e.deltaX) > 0 ? 1 : -1;
        var host = scrollHostUnder(under, axis, dir);
        if (!host) {
            return; // 页面里没有嵌套滚动容器：视口自己能滚，别抢下来
        }
        if (e.cancelable) {
            e.preventDefault();
        }
        var px = toPixels(e, host);
        if (axis === 'y') {
            host.scrollTop = (host.scrollTop || 0) + px.y;
        } else {
            host.scrollLeft = (host.scrollLeft || 0) + px.x;
        }
    }, { passive: false });
})();
window.elementScrollbarScrollTo = function (el, top, left) {
    if (!el) {
        return;
    }
    el.scrollTo({ top: top || 0, left: left || 0 });
};
window.elementScrollbarSetScrollTop = function (el, top) {
    if (!el) {
        return;
    }
    el.scrollTop = top || 0;
};
window.elementScrollbarSetScrollLeft = function (el, left) {
    if (!el) {
        return;
    }
    el.scrollLeft = left || 0;
};
window.elementScrollbarGetState = function (el) {
    if (!el) {
        return {
            scrollTop: 0,
            scrollLeft: 0,
            scrollHeight: 0,
            scrollWidth: 0,
            clientHeight: 0,
            clientWidth: 0
        };
    }
    return {
        scrollTop: el.scrollTop || 0,
        scrollLeft: el.scrollLeft || 0,
        scrollHeight: el.scrollHeight || 0,
        scrollWidth: el.scrollWidth || 0,
        clientHeight: el.clientHeight || 0,
        clientWidth: el.clientWidth || 0
    };
};
// 文字被裁掉的判据。两个轴都要给：单行 ellipsis 看 scrollWidth/clientWidth，
// -webkit-line-clamp 多行截断看 scrollHeight/clientHeight（EP 的 text.vue:49-61 就是这么分的）。
// rect* 是那个被裁元素的视口矩形（getBoundingClientRect）：ElOverflowTooltip 要靠它把浮层钉成
// position:fixed —— 表格的 .el-table__body-wrapper 是 overflow:auto，absolute 浮层会被它裁掉，
// 只有 fixed（包含块＝视口）能逃出去。同一次悬停只走一趟 JS 往返，所以判据和矩形合在一个函数里。
window.elementTextOverflowState = function (el) {
    if (!el) {
        return {
            scrollWidth: 0,
            clientWidth: 0,
            scrollHeight: 0,
            clientHeight: 0,
            text: "",
            rectTop: 0,
            rectLeft: 0,
            rectWidth: 0,
            rectHeight: 0
        };
    }
    var rect = el.getBoundingClientRect();
    return {
        scrollWidth: el.scrollWidth || 0,
        clientWidth: el.clientWidth || 0,
        scrollHeight: el.scrollHeight || 0,
        clientHeight: el.clientHeight || 0,
        text: el.textContent || "",
        rectTop: rect.top,
        rectLeft: rect.left,
        rectWidth: rect.width,
        rectHeight: rect.height
    };
};
// EP 的 text.vue:42-67 口径：真的被裁才绑原生 title，宿主自己给了 title 就不动。
// 只在 ElText 关掉本库浮层扩展（EllipsisTooltip="false"）时走这里。
window.elementSetEllipsisTitle = function (el, text) {
    if (!el || !text) {
        return;
    }
    if (el.getAttribute("title")) {
        return;
    }
    el.setAttribute("title", text);
};
// 元素在视口中的矩形 + 视口尺寸，一次调用给齐（表头筛选面板要靠它把自己钉成 position:fixed）。
// 不能直接把 getBoundingClientRect() 的返回值交给 JSInterop：DOMRect 的属性挂在原型的 getter 上，
// System.Text.Json 只序列化自身可枚举属性，过去会拿到全 0 的矩形。这里手动摊平成普通对象。
window.elementViewportRect = function (el) {
    if (!el) {
        return null;
    }
    var rect = el.getBoundingClientRect();
    return {
        left: rect.left,
        top: rect.top,
        right: rect.right,
        bottom: rect.bottom,
        width: rect.width,
        height: rect.height,
        vw: window.innerWidth || document.documentElement.clientWidth || 0,
        vh: window.innerHeight || document.documentElement.clientHeight || 0
    };
};
// 滑块跑道用：把指针按住到跑道元素上。EP 是在 window 上挂 mousemove/mouseup
// （Temp/ep214_slider_use-slider-button.ts.txt:112-116），本仓的处理器挂在跑道元素自己上面，
// 而跑道只有 6px 高，鼠标稍微走偏就不再命中，拖动会中途断掉；指针捕获后事件会重定向到跑道，
// 一路冒泡到 document，Blazor 的委托监听因此还能收到 pointermove。
// 指针已不活跃时 setPointerCapture 会抛 NotFoundError（例如松手极快），这里吞掉并返回 false。
window.elementPointerCapture = function (el, pointerId, capture) {
    if (!el || typeof pointerId !== "number" || typeof el.hasPointerCapture !== "function") {
        return false;
    }
    try {
        if (capture) {
            if (!el.hasPointerCapture(pointerId)) {
                el.setPointerCapture(pointerId);
            }
        } else if (el.hasPointerCapture(pointerId)) {
            el.releasePointerCapture(pointerId);
        }
        return true;
    } catch (e) {
        return false;
    }
};
window.elementXBubbleListScrollToEnd = function (el, reverse, smooth, onlyWhenNearBottom, threshold) {
    if (!el) {
        return;
    }

    threshold = typeof threshold === "number" ? threshold : 80;
    var maxScrollTop = Math.max(0, (el.scrollHeight || 0) - (el.clientHeight || 0));
    var distance = reverse
        ? Math.abs(el.scrollTop || 0)
        : Math.abs(maxScrollTop - (el.scrollTop || 0));

    if (onlyWhenNearBottom && distance > threshold) {
        return;
    }

    if (el.scrollTo) {
        el.scrollTo({
            top: reverse ? 0 : maxScrollTop,
            behavior: smooth ? "smooth" : "auto"
        });
        return;
    }

    el.scrollTop = reverse ? 0 : maxScrollTop;
};
window.elementXBubbleListIsUserPinned = function (el, reverse, threshold) {
    if (!el) {
        return false;
    }

    threshold = typeof threshold === "number" ? threshold : 80;
    var maxScrollTop = Math.max(0, (el.scrollHeight || 0) - (el.clientHeight || 0));
    var distance = reverse
        ? Math.abs(el.scrollTop || 0)
        : Math.abs(maxScrollTop - (el.scrollTop || 0));

    return distance > threshold;
};
window.elementResolveScrollContainer = function (target) {
    if (!target) {
        return window;
    }
    if (typeof target === "string") {
        return document.querySelector(target) || window;
    }
    return target;
};
window.elementGetScrollTop = function (container) {
    if (!container || container === window || container === document || container === document.body || container === document.documentElement) {
        return window.pageYOffset || document.documentElement.scrollTop || document.body.scrollTop || 0;
    }
    return container.scrollTop || 0;
};
window.elementSetScrollTop = function (container, top, behavior) {
    top = top || 0;
    behavior = behavior || "smooth";
    if (!container || container === window || container === document || container === document.body || container === document.documentElement) {
        window.scrollTo({ top: top, behavior: behavior });
        return;
    }
    if (container.scrollTo) {
        container.scrollTo({ top: top, behavior: behavior });
        return;
    }
    container.scrollTop = top;
};
window.elementGetContainerRect = function (container) {
    if (!container || container === window || container === document || container === document.body || container === document.documentElement) {
        return {
            top: 0,
            bottom: window.innerHeight || document.documentElement.clientHeight || 0,
            height: window.innerHeight || document.documentElement.clientHeight || 0
        };
    }
    return container.getBoundingClientRect();
};
window.elementAffixInit = function (root, dotnet, options) {
    if (!root) {
        return;
    }

    options = options || {};
    if (root.__elementAffixCleanup) {
        root.__elementAffixCleanup();
    }

    var content = root.firstElementChild || root;
    var container = elementResolveScrollContainer(options.target);
    var offset = parseInt(options.offset || 0);
    var position = options.position || "top";
    var zIndex = parseInt(options.zIndex || 100);
    var placeholderHeight = 0;

    var update = function () {
        var scrollTop = elementGetScrollTop(container);
        var rootRect = root.getBoundingClientRect();
        var containerRect = elementGetContainerRect(container);
        var fixed = position === "bottom"
            ? rootRect.bottom >= containerRect.bottom - offset
            : rootRect.top <= containerRect.top + offset;

        if (fixed) {
            placeholderHeight = placeholderHeight || root.offsetHeight || content.offsetHeight || 0;
            root.style.height = placeholderHeight + "px";
            content.style.position = "fixed";
            content.style.zIndex = zIndex;
            content.style.width = rootRect.width + "px";
            content.style.left = rootRect.left + "px";
            // 吸附位置相对「滚动容器的边缘」而不是视口边缘：
            // 容器为 window 时 containerRect.top/bottom 正好是 0/innerHeight，结果与旧实现一致；
            // 容器是页面内某个可滚动元素时（如 .demo-main），必须补上它的视口偏移，
            // 否则固钉会飞到视口 0px 处、盖住页头与标签栏。
            if (position === "bottom") {
                var viewportHeight = window.innerHeight || document.documentElement.clientHeight || 0;
                content.style.bottom = Math.max(0, viewportHeight - containerRect.bottom) + offset + "px";
                content.style.top = "";
            } else {
                content.style.top = containerRect.top + offset + "px";
                content.style.bottom = "";
            }
        } else {
            root.style.height = "";
            content.style.position = "";
            content.style.zIndex = "";
            content.style.width = "";
            content.style.left = "";
            content.style.top = "";
            content.style.bottom = "";
        }

        if (dotnet) {
            dotnet.invokeMethodAsync("SetFixed", fixed, Math.round(scrollTop));
        }
    };

    var listenerTarget = container === window ? window : container;
    listenerTarget.addEventListener("scroll", update, { passive: true });
    window.addEventListener("resize", update);
    root.__elementAffixCleanup = function () {
        listenerTarget.removeEventListener("scroll", update);
        window.removeEventListener("resize", update);
        root.__elementAffixCleanup = null;
    };
    update();
};
window.elementAffixDispose = function (root) {
    if (root && root.__elementAffixCleanup) {
        root.__elementAffixCleanup();
    }
};
window.elementBacktopInit = function (root, dotnet, options) {
    options = options || {};
    var container = elementResolveScrollContainer(options.target);
    var visibilityHeight = parseInt(options.visibilityHeight || 200);
    if (root && root.__elementBacktopCleanup) {
        root.__elementBacktopCleanup();
    }

    var update = function () {
        var visible = elementGetScrollTop(container) >= visibilityHeight;
        if (dotnet) {
            dotnet.invokeMethodAsync("SetVisible", visible);
        }
    };

    var listenerTarget = container === window ? window : container;
    listenerTarget.addEventListener("scroll", update, { passive: true });
    if (root) {
        root.__elementBacktopCleanup = function () {
            listenerTarget.removeEventListener("scroll", update);
            root.__elementBacktopCleanup = null;
        };
    }
    update();
};
window.elementBacktopDispose = function (root) {
    if (root && root.__elementBacktopCleanup) {
        root.__elementBacktopCleanup();
    }
};
window.elementBacktopScrollTo = function (target) {
    elementSetScrollTop(elementResolveScrollContainer(target), 0, "smooth");
};
window.elementAnchorScrollTo = function (href, containerSelector, offset, duration, selectScrollTop) {
    if (!href || href.charAt(0) !== "#") {
        if (href) {
            location.href = href;
        }
        return;
    }

    var target = document.querySelector(href);
    if (!target) {
        return;
    }

    var container = elementResolveScrollContainer(containerSelector);
    var scrollTop = elementGetScrollTop(container);
    var containerRect = elementGetContainerRect(container);
    var targetRect = target.getBoundingClientRect();
    // selectScrollTop（官方 2.10.5）：忽略 offset，直接把目标顶到容器顶部
    var usedOffset = selectScrollTop ? 0 : (parseInt(offset || 0));
    var nextTop = scrollTop + targetRect.top - containerRect.top - usedOffset;
    var dur = parseInt(duration || 0, 10) || 0;
    /* 官方 anchor.vue 的 isScrolling：点击先按被点的那条激活，动画期间容器自身产生的 scroll 事件
       不参与「当前锚点」计算。本库以前没有这道闸，点完立刻被自己的程序滚动算回上一条 ——
       容器滚到底、目标被 max 夹住时永远算不回来（实测 /anchor 的 #c-2：目标只到 82px，
       而阈值是 offset+bound=75），表现为「点了 C，亮的还是 A」。 */
    elementAnchorBeginSuppressScroll(container, dur);
    elementSetScrollTop(container, nextTop, dur <= 0 ? "auto" : "smooth");
    /* 官方从不改地址栏（anchor.vue 只在 onMounted 读一次 location.hash）。这里以前会
       history.replaceState(null, "", href)，而 Blazor 的 <base href="/"> 把 "#c-1" 解析到站根，
       实测点完 URL 从 /anchor 变成 http://127.0.0.1:5199/#c-1 —— 刷新就掉回首页。 */
};
window.elementAnchorSuppressTarget = function (container) {
    if (!container || container === window || container === document || container === document.documentElement) {
        return window;
    }
    return container;
};
window.elementAnchorBeginSuppressScroll = function (container, duration) {
    var store = elementAnchorSuppressTarget(container);
    if (store.__elAnchorRelease) {
        store.__elAnchorRelease();
    }
    var delay = Math.max(0, parseInt(duration || 0, 10) || 0) + 100;
    store.__elAnchorSuppressUntil = Date.now() + delay;
    function release() {
        store.__elAnchorSuppressUntil = 0;
        store.__elAnchorRelease = null;
        store.removeEventListener("scrollend", release);
        clearTimeout(timer);
    }
    // 原生 smooth 滚动拿不到完成回调：scrollend 精确解除，定时器兜底（没滚起来时不能永久挡住计算）
    store.addEventListener("scrollend", release);
    var timer = setTimeout(release, delay);
    store.__elAnchorRelease = release;
};
window.elementAnchorScrollSuppressed = function (container) {
    var store = elementAnchorSuppressTarget(container);
    return !!store.__elAnchorSuppressUntil && Date.now() < store.__elAnchorSuppressUntil;
};
window.elementAnchorUpdateMarker = function (root, href) {
    if (!root) {
        return;
    }
    // 量几何要在 is-active 刷完之后（官方是在 nextTick 里量），underline 型的 border-bottom 会让链接高 2px
    requestAnimationFrame(function () {
        var marker = root.querySelector(":scope > .el-anchor__marker") || root.querySelector(":scope > .el-anchor__cursor");
        if (!marker) {
            return;
        }

        var link = null;
        if (href) {
            var links = root.querySelectorAll("[data-anchor-href]");
            for (var i = 0; i < links.length; i++) {
                if (links[i].getAttribute("data-anchor-href") === href) {
                    link = links[i];
                    break;
                }
            }
        }

        if (!link) {
            // 官方口径：markerStyle 置空对象，回到 CSS 基态（竖向/横向 marker 基态 opacity:0 即不显示）
            marker.style.top = "";
            marker.style.left = "";
            marker.style.width = "";
            marker.style.opacity = "";
            return;
        }

        var anchorRect = root.getBoundingClientRect();
        var linkRect = link.getBoundingClientRect();
        var markerRect = marker.getBoundingClientRect();
        if (root.classList.contains("el-anchor--horizontal")) {
            marker.style.left = (linkRect.left - anchorRect.left) + "px";
            marker.style.width = linkRect.width + "px";
            marker.style.opacity = "1";
        } else {
            marker.style.top = (linkRect.top - anchorRect.top + (linkRect.height - markerRect.height) / 2) + "px";
            marker.style.opacity = "1";
        }
    });
};
window.elementAnchorInit = function (root, dotnet, options) {
    if (!root) {
        return;
    }

    options = options || {};
    if (root.__elementAnchorCleanup) {
        root.__elementAnchorCleanup();
    }

    var container = elementResolveScrollContainer(options.container);
    var offset = parseInt(options.offset || 0);
    var bound = parseInt(options.bound || 15);
    var update = function () {
        if (elementAnchorScrollSuppressed(container)) {
            return;
        }
        var links = Array.prototype.slice.call(root.querySelectorAll("[data-anchor-href]"));
        var activeHref = "";
        var containerRect = elementGetContainerRect(container);
        links.forEach(function (link) {
            var href = link.getAttribute("data-anchor-href");
            if (!href || href.charAt(0) !== "#") {
                return;
            }
            var target = document.querySelector(href);
            if (!target) {
                return;
            }
            var top = target.getBoundingClientRect().top - containerRect.top;
            if (top <= offset + bound) {
                activeHref = href;
            }
        });
        if (dotnet && activeHref) {
            dotnet.invokeMethodAsync("SetActiveHref", activeHref);
            elementAnchorUpdateMarker(root, activeHref);
        }
    };

    var listenerTarget = container === window ? window : container;
    listenerTarget.addEventListener("scroll", update, { passive: true });
    window.addEventListener("resize", update);
    root.__elementAnchorCleanup = function () {
        listenerTarget.removeEventListener("scroll", update);
        window.removeEventListener("resize", update);
        root.__elementAnchorCleanup = null;
    };
    update();
};
window.elementAnchorDispose = function (root) {
    if (root && root.__elementAnchorCleanup) {
        root.__elementAnchorCleanup();
    }
};
// 分隔条拖拽。分隔条自身宽度为 0（对齐官方：DOM 上 bar 不占布局），所以拖拽事件**不能**绑在它身上
// —— 曾经绑在 6px 的 bar 上，指针一移出这条带子就收不到 mousemove（mouseleave 还会直接结束拖拽），
// 实测拖 60px 只生效 34px。现在 mousedown 后改为监听 document：直接写面板 DOM 保证跟手，
// 并节流回传 .NET 维护状态 / 派发 resize 事件。
// 幂等：每根分隔条只在没绑过时绑定，动态新增的分隔条由下一次 elementSplitterInit 自动补绑。
window.elementSplitterInit = function (root, dotNet) {
    if (!root || !dotNet) {
        return;
    }

    var isVertical = root.classList.contains("el-splitter--vertical");
    var bars = root.querySelectorAll(":scope > .el-splitter__bar");

    function bindBar(bar) {
        if (bar.__elSplitterDown) {
            return;
        }

        function writeSize(panel, size) {
            panel.style.flexBasis = size + "px";
            panel.style[isVertical ? "height" : "width"] = size + "px";
        }

        /* 读 CSS 已解析好的 min/max（px）。没配时 min=0、max=Infinity。 */
        function limits(el) {
            var cs = getComputedStyle(el);
            var minProp = isVertical ? cs.minHeight : cs.minWidth;
            var maxProp = isVertical ? cs.maxHeight : cs.maxWidth;
            var min = parseFloat(minProp);
            var max = parseFloat(maxProp);
            return {
                min: isNaN(min) ? 0 : min,
                max: isNaN(max) ? Infinity : max
            };
        }

        function swallowClick(event) {
            event.stopPropagation();
            event.preventDefault();
            document.removeEventListener("click", swallowClick, true);
        }

        function onDown(event) {
            if (!event || event.button !== 0) {
                return;
            }

            /* 折叠按钮上的按下不启动拖拽。 */
            var target = event.target;
            if (target && target.closest && target.closest(".el-splitter__button")) {
                return;
            }

            var panel = bar.previousElementSibling;
            if (!panel || !panel.classList.contains("el-splitter-panel")) {
                return;
            }

            /* 官方语义：拖拽是「把分隔条从相邻面板之间挪走」，所以两块面板要同时改
               （当前 +delta、下一块 -delta）。只改当前那块的话，两块 flex-basis 之和会
               超过容器，被 flex-shrink 按比例拉回 —— 实测拖 60px 只有 37px 生效。 */
            var nextPanel = bar.nextElementSibling;
            if (!nextPanel || !nextPanel.classList.contains("el-splitter-panel")) {
                nextPanel = null;
            }

            if (event.preventDefault) {
                event.preventDefault();
            }

            var index = bar.dataset.index != null ? parseInt(bar.dataset.index, 10) : 0;
            if (isNaN(index)) {
                index = 0;
            }

            var lazy = root.classList.contains("is-lazy");
            var start = isVertical ? event.clientY : event.clientX;
            var rect = panel.getBoundingClientRect();
            var startSize = isVertical ? rect.height : rect.width;
            var nextRect = nextPanel ? nextPanel.getBoundingClientRect() : null;
            var nextStartSize = nextRect ? (isVertical ? nextRect.height : nextRect.width) : 0;
            var lim = limits(panel);
            var nextLim = nextPanel ? limits(nextPanel) : { min: 0, max: Infinity };

            function applyDelta(delta) {
                /* 同时受两块面板的 min / max 约束：谁先撞墙就以谁为准。 */
                if (nextPanel) {
                    delta = Math.min(delta, nextStartSize - nextLim.min);
                    delta = Math.max(delta, -(nextLim.max - nextStartSize));
                }
                delta = Math.max(delta, -(startSize - lim.min));
                delta = Math.min(delta, lim.max - startSize);
                return delta;
            }

            var state = { size: startSize, delta: 0, moved: false, sentAt: 0 };

            /* 变参：不同回调的参数个数不同（Start 1 个、Move/End 3 个），
               多传一个 undefined 会让 JSON 反序列化报「参数个数不匹配」。 */
            function send(method) {
                var args = [method];
                for (var i = 1; i < arguments.length; i++) {
                    args.push(arguments[i]);
                }
                try {
                    dotNet.invokeMethodAsync.apply(dotNet, args);
                } catch (err) {
                    /* 组件已销毁：忽略 */
                }
            }

            function onMove(moveEvent) {
                var point = isVertical ? moveEvent.clientY : moveEvent.clientX;
                var raw = point - start;
                /* 2px 死区：分隔条上的轻微抖动不算拖拽。 */
                if (!state.moved && Math.abs(raw) < 2) {
                    return;
                }
                state.moved = true;
                state.delta = applyDelta(raw);
                var next = Math.max(0, startSize + state.delta);
                state.size = next;

                if (lazy) {
                    /* 官方 lazy：拖拽过程只挪预览线（--el-splitter-bar-offset），面板尺寸松手才变。 */
                    send("JsSplitterLazy", index, Math.round(state.delta));
                    return;
                }

                writeSize(panel, Math.round(next));
                if (nextPanel) {
                    writeSize(nextPanel, Math.round(nextStartSize - state.delta));
                }
                var now = Date.now();
                if (now - state.sentAt >= 80) {
                    state.sentAt = now;
                    send("JsSplitterMove", index, Math.round(next), nextPanel ? Math.round(nextStartSize - state.delta) : -1);
                }
            }

            function onUp() {
                document.removeEventListener("mousemove", onMove);
                document.removeEventListener("mouseup", onUp);
                document.body.style.userSelect = "";
                bar.classList.remove("is-active");
                if (!state.moved) {
                    return;
                }
                if (!lazy) {
                    writeSize(panel, Math.round(state.size));
                    if (nextPanel) {
                        writeSize(nextPanel, Math.round(nextStartSize - state.delta));
                    }
                }
                send("JsSplitterEnd", index, Math.round(state.size), nextPanel ? Math.round(nextStartSize - state.delta) : -1);
                document.addEventListener("click", swallowClick, true);
                window.setTimeout(function () {
                    document.removeEventListener("click", swallowClick, true);
                }, 0);
            }

            bar.__elSplitterMove = onMove;
            bar.__elSplitterUp = onUp;
            bar.classList.add("is-active");
            document.body.style.userSelect = "none";
            document.addEventListener("mousemove", onMove);
            document.addEventListener("mouseup", onUp);
            send("JsSplitterStart", index);
        }

        bar.__elSplitterDown = onDown;
        bar.addEventListener("mousedown", onDown);
    }

    for (var i = 0; i < bars.length; i++) {
        bindBar(bars[i]);
    }
};

window.elementSplitterDispose = function (root) {
    if (!root) {
        return;
    }

    var bars = root.querySelectorAll(":scope > .el-splitter__bar");
    for (var i = 0; i < bars.length; i++) {
        var bar = bars[i];
        if (bar.__elSplitterDown) {
            bar.removeEventListener("mousedown", bar.__elSplitterDown);
            bar.__elSplitterDown = null;
        }
        /* 拖拽进行中被销毁时，清掉 document 上残留的监听，避免野指针。 */
        if (bar.__elSplitterMove) {
            document.removeEventListener("mousemove", bar.__elSplitterMove);
            bar.__elSplitterMove = null;
        }
        if (bar.__elSplitterUp) {
            document.removeEventListener("mouseup", bar.__elSplitterUp);
            bar.__elSplitterUp = null;
        }
    }
};

// 条码：把 SVG 字符串栅格化为 PNG 并触发下载。
// width/height 省略时按 SVG 自身尺寸结合设备像素比换算，保证导出图清晰。
window.elementBarcodeDownloadPng = function (svgText, fileName, width, height) {
    if (!svgText) {
        return Promise.resolve(false);
    }

    return new Promise(function (resolve, reject) {
        var svgBlob = new Blob([svgText], { type: "image/svg+xml;charset=utf-8" });
        var svgUrl = URL.createObjectURL(svgBlob);
        var image = new Image();

        image.onload = function () {
            var ratio = window.devicePixelRatio || 1;
            var naturalWidth = image.naturalWidth || 300;
            var naturalHeight = image.naturalHeight || 100;
            var targetWidth = width || naturalWidth;
            var targetHeight = height || Math.round(targetWidth * naturalHeight / naturalWidth);

            var canvas = document.createElement("canvas");
            canvas.width = Math.max(1, Math.round(targetWidth * ratio));
            canvas.height = Math.max(1, Math.round(targetHeight * ratio));

            var context = canvas.getContext("2d");
            context.scale(ratio, ratio);

            // 先铺白底，避免透明背景在部分看图工具中显示为黑色。
            context.fillStyle = "#ffffff";
            context.fillRect(0, 0, targetWidth, targetHeight);
            context.drawImage(image, 0, 0, targetWidth, targetHeight);

            URL.revokeObjectURL(svgUrl);

            var link = document.createElement("a");
            link.href = canvas.toDataURL("image/png");
            link.download = fileName || "barcode.png";
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);

            resolve(true);
        };

        image.onerror = function () {
            URL.revokeObjectURL(svgUrl);
            reject(new Error("barcode svg could not be rasterized"));
        };

        image.src = svgUrl;
    });
};

// 通用：把 SVG 栅格化为 canvas。
// width/height 省略时按 SVG 自身尺寸结合设备像素比换算；background 为空或 transparent 时保留透明通道。
window.elementSvgRasterize = function (svgText, width, height, background) {
    if (!svgText) {
        return Promise.reject(new Error("empty svg"));
    }

    var fill = window.elementSvgExportBackground(background);

    return new Promise(function (resolve, reject) {
        var svgBlob = new Blob([svgText], { type: "image/svg+xml;charset=utf-8" });
        var svgUrl = URL.createObjectURL(svgBlob);
        var image = new Image();

        image.onload = function () {
            var ratio = window.devicePixelRatio || 1;
            var naturalWidth = image.naturalWidth || 300;
            var naturalHeight = image.naturalHeight || 300;
            var targetWidth = width || naturalWidth;
            var targetHeight = height || Math.round(targetWidth * naturalHeight / naturalWidth);

            var canvas = document.createElement("canvas");
            canvas.width = Math.max(1, Math.round(targetWidth * ratio));
            canvas.height = Math.max(1, Math.round(targetHeight * ratio));

            var context = canvas.getContext("2d");
            context.scale(ratio, ratio);

            if (fill) {
                context.fillStyle = fill;
                context.fillRect(0, 0, targetWidth, targetHeight);
            }

            context.drawImage(image, 0, 0, targetWidth, targetHeight);
            URL.revokeObjectURL(svgUrl);
            resolve(canvas);
        };

        image.onerror = function () {
            URL.revokeObjectURL(svgUrl);
            reject(new Error("svg could not be rasterized"));
        };

        image.src = svgUrl;
    });
};

// 解析导出用的底色：未指定时铺白底，显式透明时保留透明通道。
window.elementSvgExportBackground = function (background) {
    if (background === undefined || background === null) {
        return "#ffffff";
    }

    var value = String(background).trim();

    if (!value) {
        return "#ffffff";
    }

    var lower = value.toLowerCase();

    return (lower === "transparent" || lower === "none") ? null : value;
};

// 通用：把 SVG 字符串栅格化为 PNG 并触发下载。
window.elementSvgDownloadPng = function (svgText, fileName, width, height, background) {
    return window.elementSvgRasterize(svgText, width, height, background).then(function (canvas) {
        var link = document.createElement("a");
        link.href = canvas.toDataURL("image/png");
        link.download = fileName || "image.png";
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        return true;
    });
};

// 通用：把 SVG 字符串栅格化为 PNG 的 data URL，便于嵌入报表或打印模板。
window.elementSvgToPngDataUrl = function (svgText, width, height, background) {
    return window.elementSvgRasterize(svgText, width, height, background).then(function (canvas) {
        return canvas.toDataURL("image/png");
    });
};

// 对话框：显示时锁定 body 滚动，关闭时还原（ElDialogGrow 的 LockScroll 用）。
// 保存上一次 overflow，避免多次开关互相覆盖。
window.elementLockBodyScroll = function (lock) {
    if (!document || !document.body) {
        return;
    }
    if (lock) {
        if (document.body.__elPrevOverflow === undefined) {
            document.body.__elPrevOverflow = document.body.style.overflow;
        }
        document.body.style.overflow = "hidden";
    } else {
        document.body.style.overflow = document.body.__elPrevOverflow || "";
        document.body.__elPrevOverflow = undefined;
    }
};

/**
 * ============================================================================
 * ElHtmlEditorGrow：自写富文本编辑器浏览器通道（不含任何第三方依赖）
 * ----------------------------------------------------------------------------
 * 仅使用浏览器内建的 Selection / Range、document.execCommand 与 XMLHttpRequest。
 * 所有状态均以 JSON 字符串回传，由 C# 侧 ElHtmlEditorGrowState /
 * ElHtmlEditorGrowTableSelection 解析；因此在无浏览器环境下组件仍可渲染与绑定值。
 * ============================================================================
 */
(function () {
    var KEY = "__elHtmlEditorGrow";
    var cellClipboard = null;

    function context(el) {
        return el && el[KEY] ? el[KEY] : null;
    }

    function exec(command, value) {
        try {
            return document.execCommand(command, false, value === undefined ? null : value);
        } catch (e) {
            return false;
        }
    }

    function queryState(command) {
        try {
            return !!document.queryCommandState(command);
        } catch (e) {
            return false;
        }
    }

    function queryEnabled(command) {
        try {
            return !!document.queryCommandEnabled(command);
        } catch (e) {
            return false;
        }
    }

    function queryValue(command) {
        try {
            return document.queryCommandValue(command) || "";
        } catch (e) {
            return "";
        }
    }

    function activeRange(el) {
        var selection = window.getSelection ? window.getSelection() : null;
        if (!selection || selection.rangeCount === 0) {
            return null;
        }
        var range = selection.getRangeAt(0);
        var node = range.commonAncestorContainer;
        var host = node ? (node.nodeType === 1 ? node : node.parentNode) : null;
        if (!host || !el.contains(host)) {
            return null;
        }
        return range;
    }

    function ancestor(node, tags, root) {
        var el = node ? (node.nodeType === 1 ? node : node.parentNode) : null;
        while (el && el.nodeType === 1) {
            if (tags.indexOf(el.tagName.toLowerCase()) >= 0) {
                return el;
            }
            if (el === root || el === document.body) {
                break;
            }
            el = el.parentNode;
        }
        return null;
    }

    function currentCell(el) {
        var range = activeRange(el);
        if (!range) {
            return null;
        }
        return ancestor(range.startContainer, ["td", "th"], el);
    }

    function currentRow(cell) {
        var row = cell ? cell.parentNode : null;
        return row && row.tagName && row.tagName.toLowerCase() === "tr" ? row : null;
    }

    function tableOf(cell) {
        return ancestor(cell, ["table"], null);
    }

    function toHex(color) {
        if (!color) {
            return null;
        }
        var text = String(color).trim();
        if (text === "" || text === "transparent" || text === "rgba(0, 0, 0, 0)") {
            return null;
        }
        if (text.charAt(0) === "#") {
            return text.toLowerCase();
        }
        var match = text.match(/^rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)/i);
        if (!match) {
            return text;
        }
        var out = "#";
        for (var i = 1; i <= 3; i++) {
            var v = Math.max(0, Math.min(255, parseInt(match[i], 10))).toString(16);
            out += v.length === 1 ? "0" + v : v;
        }
        return out;
    }

    function toPixels(text) {
        if (!text) {
            return null;
        }
        var match = String(text).match(/(-?\d+(?:\.\d+)?)\s*px/i);
        if (!match) {
            return null;
        }
        var n = parseFloat(match[1]);
        return isNaN(n) ? null : Math.round(n);
    }

    /* ---------------------------------------------------------------- 状态 */

    function buildState(el) {
        var range = activeRange(el);
        var cell = currentCell(el);
        var link = range ? ancestor(range.startContainer, ["a"], el) : null;
        var block = String(queryValue("formatBlock") || "").replace(/[<>]/g, "").toLowerCase();

        return {
            Html: el.innerHTML,
            Undo: queryEnabled("undo"),
            Redo: queryEnabled("redo"),
            Bold: queryState("bold"),
            Italic: queryState("italic"),
            Underline: queryState("underline"),
            StrikeThrough: queryState("strikeThrough"),
            Subscript: queryState("subscript"),
            Superscript: queryState("superscript"),
            JustifyLeft: queryState("justifyLeft"),
            JustifyCenter: queryState("justifyCenter"),
            JustifyRight: queryState("justifyRight"),
            JustifyFull: queryState("justifyFull"),
            Unlink: !!link,
            InTable: !!cell,
            FontName: String(queryValue("fontName") || ""),
            FontSize: String(queryValue("fontSize") || ""),
            FormatBlock: block,
            Success: true,
            Message: null
        };
    }

    /* -------------------------------------------------------------- 表格模型 */

    function buildGrid(table) {
        var rows = Array.prototype.slice.call(table.rows);
        var grid = [];

        for (var r = 0; r < rows.length; r++) {
            if (!grid[r]) {
                grid[r] = [];
            }
            var column = 0;
            for (var c = 0; c < rows[r].children.length; c++) {
                var cell = rows[r].children[c];
                while (grid[r][column]) {
                    column++;
                }
                var colspan = cell.colSpan || 1;
                var rowspan = cell.rowSpan || 1;
                for (var dr = 0; dr < rowspan; dr++) {
                    if (!grid[r + dr]) {
                        grid[r + dr] = [];
                    }
                    for (var dc = 0; dc < colspan; dc++) {
                        grid[r + dr][column + dc] = {
                            cell: cell,
                            row: r,
                            column: column,
                            origin: dr === 0 && dc === 0
                        };
                    }
                }
                column += colspan;
            }
        }

        return { rows: rows, grid: grid };
    }

    function locate(model, cell) {
        for (var r = 0; r < model.grid.length; r++) {
            var row = model.grid[r];
            if (!row) {
                continue;
            }
            for (var c = 0; c < row.length; c++) {
                var entry = row[c];
                if (entry && entry.origin && entry.cell === cell) {
                    return entry;
                }
            }
        }
        return null;
    }

    function slotAt(model, row, column) {
        return model.grid[row] && model.grid[row][column] ? model.grid[row][column] : null;
    }

    function buildTableSelection(el) {
        var result = {
            InTable: false, InHeader: false, RowIndex: -1, ColumnIndex: -1,
            Rows: 0, Columns: 0, Width: null, Border: 0, HeaderRow: false,
            ColSpan: 1, RowSpan: 1,
            CanMergeRight: false, CanMergeDown: false, CanSplit: false,
            ColumnWidth: null, ColumnWidthPx: null,
            CellBackground: null, CellPadding: null, CellPaddingPx: null,
            CellTextAlign: null, CellVerticalAlign: null, CellBorder: null,
            BorderStyle: null, BorderWidthPx: null, BorderColor: null,
            BorderTop: false, BorderRight: false, BorderBottom: false, BorderLeft: false
        };

        var cell = currentCell(el);
        if (!cell) {
            return result;
        }

        var table = tableOf(cell);
        var row = currentRow(cell);
        if (!table || !row) {
            return result;
        }

        var model = buildGrid(table);
        var position = locate(model, cell);
        var computed = window.getComputedStyle ? window.getComputedStyle(cell) : null;
        var heads = table.getElementsByTagName("thead");
        var bodies = table.getElementsByTagName("tbody");
        var rows = bodies.length > 0 ? Array.prototype.slice.call(bodies[0].rows) : model.rows;

        result.InTable = true;
        result.InHeader = cell.tagName.toLowerCase() === "th";
        result.RowIndex = position ? position.row : -1;
        result.ColumnIndex = position ? position.column : -1;
        result.Rows = rows.length;
        result.Columns = model.grid.length > 0 ? Math.max.apply(null, model.grid.map(function (r) {
            return r ? r.length : 0;
        })) : 0;
        result.HeaderRow = heads.length > 0;
        result.Width = cell.parentNode.style.width || table.style.width || table.getAttribute("width") || null;
        result.Border = parseInt(table.getAttribute("border") || "0", 10) || 0;
        result.ColSpan = cell.colSpan || 1;
        result.RowSpan = cell.rowSpan || 1;
        result.CanSplit = result.ColSpan > 1 || result.RowSpan > 1;

        var right = position ? slotAt(model, position.row, position.column + (cell.colSpan || 1)) : null;
        result.CanMergeRight = !!(right && right.origin && right.cell !== cell && right.cell.rowSpan === cell.rowSpan);

        var below = position ? slotAt(model, position.row + (cell.rowSpan || 1), position.column) : null;
        result.CanMergeDown = !!(below && below.origin && below.cell !== cell && below.cell.colSpan === cell.colSpan);

        result.CellBackground = toHex(cell.style.backgroundColor || (computed ? computed.backgroundColor : null));
        result.CellPadding = cell.style.padding || (computed ? computed.padding : null);
        result.CellPaddingPx = toPixels(result.CellPadding);
        result.CellTextAlign = cell.style.textAlign || (computed ? computed.textAlign : null);
        result.CellVerticalAlign = cell.style.verticalAlign || (computed ? computed.verticalAlign : null);
        result.CellBorder = cell.style.border || null;

        var borderStyle = cell.style.borderStyle || (computed ? computed.borderTopStyle : null);
        result.BorderStyle = borderStyle && borderStyle !== "none" ? String(borderStyle).split(" ")[0] : null;
        result.BorderWidthPx = toPixels(cell.style.borderWidth || (computed ? computed.borderTopWidth : null));
        result.BorderColor = toHex(cell.style.borderColor || (computed ? computed.borderTopColor : null));

        if (computed) {
            result.BorderTop = computed.borderTopStyle !== "none" && toPixels(computed.borderTopWidth) > 0;
            result.BorderRight = computed.borderRightStyle !== "none" && toPixels(computed.borderRightWidth) > 0;
            result.BorderBottom = computed.borderBottomStyle !== "none" && toPixels(computed.borderBottomWidth) > 0;
            result.BorderLeft = computed.borderLeftStyle !== "none" && toPixels(computed.borderLeftWidth) > 0;
        }

        result.ColumnWidth = cell.style.width || null;
        result.ColumnWidthPx = toPixels(cell.style.width);

        return result;
    }

    /* -------------------------------------------------------------- 表格操作 */

    function removeTable(table) {
        if (table && table.parentNode) {
            table.parentNode.removeChild(table);
            return true;
        }
        return false;
    }

    function insertRow(table, cell, before) {
        var model = buildGrid(table);
        var position = locate(model, cell);
        if (!position) {
            return false;
        }

        var row = model.rows[position.row];
        var inHead = row.parentNode && row.parentNode.tagName.toLowerCase() === "thead";
        var created = document.createElement("tr");

        for (var i = 0; i < row.children.length; i++) {
            var source = row.children[i];
            var target = document.createElement(inHead ? "th" : "td");
            if (source.colSpan > 1) {
                target.colSpan = source.colSpan;
            }
            target.innerHTML = "&nbsp;";
            created.appendChild(target);
        }

        if (before) {
            row.parentNode.insertBefore(created, row);
        } else {
            row.parentNode.insertBefore(created, row.nextSibling);
        }

        return true;
    }

    function insertColumn(table, cell, before) {
        var model = buildGrid(table);
        var position = locate(model, cell);
        if (!position) {
            return false;
        }

        var target = position.column;

        for (var r = 0; r < model.rows.length; r++) {
            var row = model.rows[r];
            var slot = slotAt(model, r, target);
            if (!slot || !slot.origin || !slot.cell.parentNode) {
                continue;
            }
            var anchor = slot.cell;
            var created = document.createElement(anchor.tagName.toLowerCase() === "th" ? "th" : "td");
            created.innerHTML = "&nbsp;";
            if (before) {
                row.insertBefore(created, anchor);
            } else {
                row.insertBefore(created, anchor.nextSibling);
            }
        }

        return true;
    }

    function deleteRow(table, cell) {
        var model = buildGrid(table);
        var position = locate(model, cell);
        if (!position) {
            return false;
        }
        if (model.rows.length <= 1) {
            return removeTable(table);
        }
        var row = model.rows[position.row];
        row.parentNode.removeChild(row);
        return true;
    }

    function deleteColumn(table, cell) {
        var model = buildGrid(table);
        var position = locate(model, cell);
        if (!position) {
            return false;
        }

        var target = position.column;

        for (var r = 0; r < model.rows.length; r++) {
            var row = model.rows[r];
            var slot = slotAt(model, r, target);
            if (!slot || !slot.origin) {
                continue;
            }
            var victim = slot.cell;
            if (victim.colSpan > 1) {
                victim.colSpan = victim.colSpan - 1;
                continue;
            }
            if (row.children.length <= 1) {
                continue;
            }
            victim.parentNode.removeChild(victim);
        }

        return true;
    }

    function mergeRight(table, cell) {
        var model = buildGrid(table);
        var position = locate(model, cell);
        if (!position) {
            return false;
        }

        var slot = slotAt(model, position.row, position.column + (cell.colSpan || 1));
        if (!slot || !slot.origin || slot.cell === cell || slot.cell.rowSpan !== cell.rowSpan) {
            return false;
        }

        var other = slot.cell;
        cell.colSpan = (cell.colSpan || 1) + (other.colSpan || 1);
        cell.innerHTML = cell.innerHTML + other.innerHTML;
        other.parentNode.removeChild(other);
        return true;
    }

    function mergeDown(table, cell) {
        var model = buildGrid(table);
        var position = locate(model, cell);
        if (!position) {
            return false;
        }

        var slot = slotAt(model, position.row + (cell.rowSpan || 1), position.column);
        if (!slot || !slot.origin || slot.cell === cell || slot.cell.colSpan !== cell.colSpan) {
            return false;
        }

        var other = slot.cell;
        cell.rowSpan = (cell.rowSpan || 1) + (other.rowSpan || 1);
        cell.innerHTML = cell.innerHTML + other.innerHTML;
        other.parentNode.removeChild(other);
        return true;
    }

    function splitCell(table, cell) {
        var colspan = cell.colSpan || 1;
        var rowspan = cell.rowSpan || 1;

        if (colspan <= 1 && rowspan <= 1) {
            return false;
        }

        if (colspan > 1 && rowspan > 1) {
            // 同时跨行跨列的复合合并无法无损拆分，交由上层提示。
            return false;
        }

        var tag = cell.tagName.toLowerCase();
        var row = currentRow(cell);
        var content = cell.innerHTML;

        if (colspan > 1) {
            cell.colSpan = 1;
            var anchor = cell;
            for (var i = 1; i < colspan; i++) {
                var extra = document.createElement(tag);
                extra.innerHTML = "&nbsp;";
                row.insertBefore(extra, anchor.nextSibling);
                anchor = extra;
            }
            if (content) {
                cell.innerHTML = content;
            }
            return true;
        }

        var model = buildGrid(table);
        var position = locate(model, cell);
        if (!position) {
            return false;
        }

        // 先把插入锚点全部算好，再统一改 DOM，避免网格失效。
        var anchors = [];
        for (var k = 1; k < rowspan; k++) {
            var targetRow = model.rows[position.row + k];
            if (!targetRow) {
                break;
            }
            var slot = slotAt(model, position.row + k, position.column);
            anchors.push({
                row: targetRow,
                before: slot && slot.cell && slot.cell.parentNode === targetRow ? slot.cell : null
            });
        }

        cell.rowSpan = 1;
        if (content) {
            cell.innerHTML = content;
        }

        for (var j = 0; j < anchors.length; j++) {
            var created = document.createElement(tag);
            created.innerHTML = "&nbsp;";
            if (anchors[j].before) {
                anchors[j].row.insertBefore(created, anchors[j].before);
            } else {
                anchors[j].row.appendChild(created);
            }
        }

        return true;
    }

    function buildTableHtml(args) {
        var rows = Math.max(1, parseInt(args.Rows, 10) || 3);
        var columns = Math.max(1, parseInt(args.Columns, 10) || 3);
        var border = args.Border === undefined || args.Border === null ? 1 : args.Border;
        var width = args.Width || "100%";
        var html = '<table border="' + border + '" style="width:' + width + ';border-collapse:collapse;">';

        if (args.HeaderRow) {
            html += "<thead><tr>";
            for (var c = 1; c <= columns; c++) {
                var header = args.DefaultColumnHeader ? String(args.DefaultColumnHeader).replace("{0}", c) : String(c);
                html += "<th>&nbsp;" + header + "</th>";
            }
            html += "</tr></thead>";
        }

        html += "<tbody>";
        for (var r = 0; r < rows; r++) {
            html += "<tr>";
            for (var j = 0; j < columns; j++) {
                html += "<td>&nbsp;</td>";
            }
            html += "</tr>";
        }
        html += "</tbody></table>";

        return html;
    }

    function applyTableArgs(table, cell, args) {
        if (!table || !cell) {
            return;
        }

        if (args.Width) {
            table.style.width = args.Width;
        }

        if (args.Border !== undefined && args.Border !== null) {
            table.setAttribute("border", String(args.Border));
        }

        if (args.ColumnWidth) {
            var model = buildGrid(table);
            var position = locate(model, cell);
            if (position) {
                for (var r = 0; r < model.rows.length; r++) {
                    var slot = slotAt(model, r, position.column);
                    if (!slot) {
                        continue;
                    }
                    slot.cell.style.width = args.ColumnWidth === "auto" ? "" : args.ColumnWidth;
                }
            }
        }

        var border = null;
        if (args.CellBorder) {
            border = args.CellBorder;
        } else if (args.BorderStyle || args.BorderWidthPx || args.BorderColor) {
            border = (args.BorderWidthPx ? args.BorderWidthPx : 1) + "px "
                + (args.BorderStyle || "solid") + " "
                + (args.BorderColor || "#dcdfe6");
        }

        if (args.CellBackground) {
            cell.style.backgroundColor = args.CellBackground;
        }
        if (args.CellPadding) {
            cell.style.padding = args.CellPadding;
        }
        if (args.CellTextAlign) {
            cell.style.textAlign = args.CellTextAlign;
        }
        if (args.CellVerticalAlign) {
            cell.style.verticalAlign = args.CellVerticalAlign;
        }
        if (border) {
            cell.style.border = border;
        }
    }

    /* ---------------------------------------------------------------- 上传 */

    function uploadFiles(self, files) {
        if (!self.uploadUrl || !files || files.length === 0) {
            return;
        }

        var form = new FormData();
        form.append(self.fieldName || "file", files[0]);

        var headers = {};
        try {
            headers = self.headers ? JSON.parse(self.headers) : {};
        } catch (e) {
            headers = {};
        }

        var xhr = new XMLHttpRequest();
        xhr.open("POST", self.uploadUrl, true);

        for (var name in headers) {
            if (Object.prototype.hasOwnProperty.call(headers, name)) {
                try {
                    xhr.setRequestHeader(name, headers[name]);
                } catch (e) {
                    // 忽略非法请求头。
                }
            }
        }

        var notify = function (payload) {
            if (self.ref) {
                self.ref.invokeMethodAsync("OnUploadCompletedAsync", payload);
            }
        };

        xhr.onload = function () {
            if (xhr.status >= 200 && xhr.status < 300) {
                notify(xhr.responseText || "");
                return;
            }
            notify(JSON.stringify({ success: false, error: "HTTP " + xhr.status }));
        };

        xhr.onerror = function () {
            notify(JSON.stringify({ success: false, error: "network error" }));
        };

        xhr.send(form);
    }

    /* ------------------------------------------------------------ 对外接口 */

    window.elementHtmlEditorAttach = function (el, dotNetRef, fileInput, uploadUrl, headersJson, fieldName) {
        if (!el) {
            return;
        }

        window.elementHtmlEditorDetach(el);

        var self = {
            el: el,
            ref: dotNetRef,
            fileInput: fileInput || null,
            uploadUrl: uploadUrl || null,
            headers: headersJson || "{}",
            fieldName: fieldName || "file",
            saved: null,
            listeners: []
        };

        el[KEY] = self;

        var listen = function (target, name, handler) {
            if (!target || !target.addEventListener) {
                return;
            }
            target.addEventListener(name, handler);
            self.listeners.push([target, name, handler]);
        };

        var notifyInput = function () {
            if (self.ref) {
                self.ref.invokeMethodAsync("OnEditorInputAsync", el.innerHTML);
            }
        };

        var notifySelection = function () {
            if (self.ref) {
                self.ref.invokeMethodAsync("OnSelectionChangedAsync");
            }
        };

        listen(el, "input", function () {
            notifyInput();
        });

        listen(el, "keyup", function (e) {
            if (e && e.key && e.key.indexOf("Arrow") === 0) {
                return;
            }
            notifySelection();
        });

        listen(el, "mouseup", function () {
            notifySelection();
        });

        listen(el, "blur", function () {
            if (self.ref) {
                self.ref.invokeMethodAsync("OnEditorBlurAsync");
            }
        });

        listen(el, "paste", function (e) {
            if (!e || !e.clipboardData) {
                return;
            }

            if (self.uploadUrl && e.clipboardData.files && e.clipboardData.files.length > 0) {
                e.preventDefault();
                uploadFiles(self, e.clipboardData.files);
                return;
            }

            var html = typeof e.clipboardData.getData === "function"
                ? (e.clipboardData.getData("text/html") || "")
                : "";

            if (!html) {
                // 纯文本粘贴交给浏览器默认行为（会保留换行）。
                return;
            }

            e.preventDefault();

            var insert = function (markup) {
                el.focus();
                exec("insertHTML", markup || "");
                notifyInput();
            };

            if (!self.ref) {
                insert(html);
                return;
            }

            self.ref.invokeMethodAsync("OnPasteFilterAsync", html).then(insert, function () {
                insert(html);
            });
        });

        listen(el, "drop", function (e) {
            if (!self.uploadUrl || !e || !e.dataTransfer) {
                return;
            }
            var files = e.dataTransfer.files;
            if (files && files.length > 0) {
                e.preventDefault();
                uploadFiles(self, files);
            }
        });

        if (self.fileInput) {
            listen(self.fileInput, "change", function () {
                if (self.fileInput.files && self.fileInput.files.length > 0) {
                    uploadFiles(self, self.fileInput.files);
                }
                try {
                    self.fileInput.value = "";
                } catch (e) {
                    // 忽略：部分浏览器不允许脚本清空文件框。
                }
            });
        }
    };

    window.elementHtmlEditorDetach = function (el) {
        var self = context(el);
        if (!el || !self) {
            return;
        }

        for (var i = 0; i < self.listeners.length; i++) {
            var item = self.listeners[i];
            try {
                item[0].removeEventListener(item[1], item[2]);
            } catch (e) {
                // 忽略：节点可能已被移除。
            }
        }

        self.listeners = [];
        self.saved = null;

        try {
            delete el[KEY];
        } catch (e) {
            el[KEY] = null;
        }
    };

    window.elementHtmlEditorPickFile = function (el) {
        var self = context(el);
        if (self && self.fileInput && typeof self.fileInput.click === "function") {
            self.fileInput.click();
        }
    };

    window.elementHtmlEditorGetHtml = function (el) {
        return el ? el.innerHTML : null;
    };

    window.elementHtmlEditorSetHtml = function (el, html) {
        if (!el) {
            return;
        }
        var next = html || "";
        if (el.innerHTML !== next) {
            el.innerHTML = next;
        }
    };

    window.elementHtmlEditorExec = function (el, command, value) {
        if (!el || !command) {
            return null;
        }

        var self = context(el);

        if (self && self.saved) {
            window.elementHtmlEditorRestoreSelection(el);
        } else if (typeof el.focus === "function") {
            el.focus();
        }

        var key = String(command).toLowerCase();

        if (key === "inserthtml") {
            exec("insertHTML", value || "");
        } else if (key === "formatblock") {
            var tag = value || "p";
            if (tag.charAt(0) !== "<") {
                tag = "<" + tag + ">";
            }
            exec("formatBlock", tag);
        } else {
            exec(command, value);
        }

        return JSON.stringify(buildState(el));
    };

    window.elementHtmlEditorQueryState = function (el) {
        if (!el) {
            return null;
        }
        return JSON.stringify(buildState(el));
    };

    window.elementHtmlEditorSaveSelection = function (el) {
        var self = context(el);
        if (!el || !self) {
            return;
        }

        var range = activeRange(el);
        self.saved = range ? range.cloneRange() : null;
    };

    window.elementHtmlEditorRestoreSelection = function (el) {
        var self = context(el);
        if (!el || !self || !self.saved) {
            return;
        }

        if (typeof el.focus === "function") {
            el.focus();
        }

        var selection = window.getSelection ? window.getSelection() : null;
        if (!selection) {
            return;
        }

        try {
            selection.removeAllRanges();
            selection.addRange(self.saved);
        } catch (e) {
            // 忽略：文档结构变化后旧 range 可能已失效。
        }
    };

    window.elementHtmlEditorSelectionText = function (el) {
        if (!el) {
            return "";
        }
        var range = activeRange(el);
        if (!range) {
            return "";
        }
        return typeof range.toString === "function" ? range.toString() : "";
    };

    window.elementHtmlEditorGetTableSelection = function (el) {
        if (!el) {
            return null;
        }
        return JSON.stringify(buildTableSelection(el));
    };

    window.elementHtmlEditorTableCommand = function (el, name, argsJson) {
        if (!el || !name) {
            return null;
        }

        var args = {};
        try {
            args = argsJson ? JSON.parse(argsJson) : {};
        } catch (e) {
            args = {};
        }

        var self = context(el);

        if (self && self.saved) {
            window.elementHtmlEditorRestoreSelection(el);
        } else if (typeof el.focus === "function") {
            el.focus();
        }

        var key = String(name).toLowerCase();
        var success = true;
        var message = null;
        var cell = key === "inserttable" ? null : currentCell(el);
        var table = cell ? tableOf(cell) : null;

        if (key === "inserttable") {
            var html = args.Html;
            if (!html) {
                html = buildTableHtml(args);
            }
            exec("insertHTML", html);
        } else if (!cell || !table) {
            success = false;
            message = "table.action.requiresTable";
        } else {
            switch (key) {
                case "updatetable":
                    applyTableArgs(table, cell, args);
                    break;
                case "addrowbefore":
                    success = insertRow(table, cell, true);
                    break;
                case "addrowafter":
                    success = insertRow(table, cell, false);
                    break;
                case "addcolumnbefore":
                    success = insertColumn(table, cell, true);
                    break;
                case "addcolumnafter":
                    success = insertColumn(table, cell, false);
                    break;
                case "deleterow":
                    success = deleteRow(table, cell);
                    break;
                case "deletecolumn":
                    success = deleteColumn(table, cell);
                    break;
                case "deletetable":
                    success = removeTable(table);
                    break;
                case "mergecellright":
                    success = mergeRight(table, cell);
                    if (!success) {
                        message = "table.merge.invalidSelection";
                    }
                    break;
                case "mergecelldown":
                    success = mergeDown(table, cell);
                    if (!success) {
                        message = "table.merge.invalidSelection";
                    }
                    break;
                case "splitcell":
                    success = splitCell(table, cell);
                    if (!success) {
                        message = "table.merge.invalidSelection";
                    }
                    break;
                case "copycells":
                    cellClipboard = { html: cell.innerHTML, style: cell.getAttribute("style") || "" };
                    break;
                case "pastecells":
                    if (!cellClipboard) {
                        success = false;
                        message = "table.paste.blocked";
                    } else if (cell.colSpan > 1 || cell.rowSpan > 1) {
                        success = false;
                        message = "table.paste.blocked";
                    } else {
                        cell.innerHTML = cellClipboard.html;
                        if (cellClipboard.style) {
                            cell.setAttribute("style", cellClipboard.style);
                        }
                    }
                    break;
                default:
                    success = false;
                    message = "table.action.requiresTable";
                    break;
            }
        }

        var state = buildState(el);
        state.Success = success;
        state.Message = message;

        return JSON.stringify(state);
    };
})();

/**
 * ElInput 密码掩码档的能力探测：浏览器是否支持 -webkit-text-security。
 * 只回答"支持与否"，不写任何样式 —— 渲染决策仍归 C#（Blazor 单一真源）。
 * 注意不能用 getComputedStyle(el).webkitTextSecurity 当探测器：实测原生 type=password 的格子也回 "disc"，
 * 那条读数分不开"浏览器实现了这个属性"和"这是密码框"两件事。
 */
window.elementSupportsTextSecurity = function () {
    try {
        return !!(window.CSS && typeof window.CSS.supports === "function" && window.CSS.supports("-webkit-text-security", "disc"));
    } catch (e) {
        return false;
    }
};

/**
 * ElInput(textarea) Autosize：测量内容实际需要的高度（像素）。
 * 只负责「量」，不写 el.style —— 写样式仍交给 C# 的 style 属性，
 * 避免 Blazor 的下一次 diff 把 JS 塞的行内 height 冲掉。
 * minRows/maxRows 为 0 或负时表示不限制。
 */
window.elementTextareaAutoHeight = function (el, minRows, maxRows) {
    if (!el) {
        return 0;
    }

    var style = window.getComputedStyle ? window.getComputedStyle(el) : null;
    var line = style ? (parseFloat(style.lineHeight) || 0) : 0;

    if (!line) {
        line = style ? (parseFloat(style.fontSize) || 14) * 1.4 : 20;
    }

    var padding = style ? (parseFloat(style.paddingTop) || 0) + (parseFloat(style.paddingBottom) || 0) : 0;
    var border = style ? (parseFloat(style.borderTopWidth) || 0) + (parseFloat(style.borderBottomWidth) || 0) : 0;
    var chrome = padding + border;
    var previous = el.style.height;

    el.style.height = "auto";
    var content = (el.scrollHeight || 0) - chrome;
    var rows = Math.ceil((content - 1) / line);

    el.style.height = previous;

    minRows = minRows || 0;
    maxRows = maxRows || 0;

    if (minRows > 0 && rows < minRows) {
        rows = minRows;
    }
    if (maxRows > 0 && rows > maxRows) {
        rows = maxRows;
    }

    return Math.round(rows * line + chrome);
};

window.Element = window.Element || {};
Element.getScrollDistance = function (el, distance) {
    if (!el) return 0;
    try {
        var scrollTop = el.scrollTop || 0;
        var scrollHeight = el.scrollHeight || 0;
        var clientHeight = el.clientHeight || 0;
        var remaining = scrollHeight - scrollTop - clientHeight;
        if (remaining <= distance) {
            return 1;
        }
        return 0;
    } catch (e) {
        return 0;
    }
};

/**
 * ============================================================================
 * ElViewerGrow：OFD 预览通道
 * ----------------------------------------------------------------------------
 * 浏览器没有 OFD 原生能力，C# 侧也没有实现 OFD 版面解析，所以「打开 OFD」这一段
 * 交给一个可选的渲染核心：wwwroot/js/ofd/ofd.umd.min.js（约 0.9 MB）。
 *
 * 它**只在真的打开 OFD 时按需注入**：不打开 OFD 的页面永远不会下载这 0.9 MB，
 * 也不会有任何全局污染（核心只在加载后往 window.ofd 上挂一个对象）。
 *
 * 本通道只做「桥接与容器管理」，版面解析与逐页绘制全部由核心完成：
 *   - 从 dom.js 自身的 script src 推导核心地址（兼容 _content/Element-Plus/... 前缀）；
 *   - 把 C# 传过来的字节（base64）或文档地址喂给核心；
 *   - 把每页包成 .el-viewer-grow__ofd-page 塞进容器，滚动 / 缩放 / 旋转 / 打印
 *     全部复用组件已有的 CSS（所以这里不需要任何交互逻辑）；
 *   - 回传页数给工具栏，并提供跳页滚动与销毁；
 *   - 给注释盒右上角补一枚 (i) 角标（批次101），点击时由组件侧经
 *     elementOfdViewerAnnotationAt 取回注释内容开卡片。
 *
 * 与参照实现（jit-viewer 的 ofd-bridge.js / ofd-patch.js）刻意不同：
 *   - 不隐藏、不接管核心的内部节点，只给它一个容器；
 *   - 不额外叠一层 overlay canvas 去补文字 —— 核心本身就把文字渲染成可选中、
 *     可打印的 DOM 文本，再叠 canvas 反而会破坏选中与打印，还会和缩放打架。
 * ============================================================================
 */
(function () {
    "use strict";

    var CORE_RELATIVE = "js/ofd/ofd.umd.min.js";
    var CORE_FALLBACK = "_content/Element-Plus/" + CORE_RELATIVE;
    var STATE_KEY = "__elOfdViewer";
    var PAGE_CLASS = "el-viewer-grow__ofd-page";
    var WRAPPER_CLASS = "el-viewer-grow__ofd-pages";
    var ANNOT_SELECTOR = "[data-ofd-annot]";
    var MARKER_SELECTOR = "[data-ofd-annot-marker]";
    var MARKER_CLASS = "el-viewer-grow__ofd-annot-marker";
    var MARKER_DOT_CLASS = "el-viewer-grow__ofd-annot-marker-dot";

    var resolvedCoreUrl = null;
    var coreLoading = null;

    /** 从 dom.js 自己的 script src 推导核心地址，避免把 _content 前缀写死。 */
    function resolveCoreUrl() {
        if (resolvedCoreUrl) {
            return resolvedCoreUrl;
        }

        try {
            var scripts = document.getElementsByTagName("script");
            for (var i = 0; i < scripts.length; i++) {
                var src = scripts[i].src || "";
                var at = src.indexOf("/js/dom.js");
                if (at >= 0) {
                    resolvedCoreUrl = src.substring(0, at + 1) + CORE_RELATIVE;
                    break;
                }
            }
        } catch (e) {
            resolvedCoreUrl = null;
        }

        if (!resolvedCoreUrl) {
            resolvedCoreUrl = CORE_FALLBACK;
        }

        return resolvedCoreUrl;
    }

    /** 按需注入核心；并发调用共用同一个 Promise，失败后允许重试。 */
    function ensureCore() {
        if (window.ofd) {
            return Promise.resolve();
        }

        if (coreLoading) {
            return coreLoading;
        }

        coreLoading = new Promise(function (resolve, reject) {
            var url = resolveCoreUrl();
            var script = document.createElement("script");
            script.src = url;
            script.async = true;
            script.onload = function () {
                if (window.ofd) {
                    resolve();
                } else {
                    reject(new Error("elementOfdViewer: " + url + " loaded but window.ofd is missing"));
                }
            };
            script.onerror = function () {
                reject(new Error("elementOfdViewer: cannot load " + url));
            };
            (document.head || document.body).appendChild(script);
        });

        coreLoading.then(null, function () {
            coreLoading = null;
        });

        return coreLoading;
    }

    function stateOf(container) {
        return container ? container[STATE_KEY] || null : null;
    }

    /**
     * base64 → ArrayBuffer。
     * 必须回 ArrayBuffer 而不是 Uint8Array：渲染核心的 parseOfdDocument 只认
     * File / ArrayBuffer，其它值会走「当路径去取二进制」的分支 —— Uint8Array 被字符串化
     * 成 "80,75,3,4,..."（正是 PKZIP 头）后会被当相对地址 fetch，结果是 414 URI Too Long
     * 外加一句看不懂的 "Ajax error"。这里返回底层 buffer 就避开了整条弯路。
     */
    function base64ToArrayBuffer(base64) {
        var binary = window.atob(base64 || "");
        var length = binary.length;
        var bytes = new Uint8Array(length);
        for (var i = 0; i < length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        return bytes.buffer;
    }

    function fetchBytes(url) {
        return window.fetch(url, { credentials: "same-origin" }).then(function (response) {
            if (!response.ok) {
                throw new Error("elementOfdViewer: HTTP " + response.status + " for " + url);
            }
            return response.arrayBuffer();
        });
    }

    function loadBytes(payload) {
        if (payload.base64) {
            return Promise.resolve(base64ToArrayBuffer(payload.base64));
        }
        if (payload.url) {
            return fetchBytes(payload.url);
        }
        return Promise.reject(new Error("elementOfdViewer: neither content nor url was provided"));
    }

    function parseDocument(buffer) {
        return new Promise(function (resolve, reject) {
            try {
                window.ofd.parseOfdDocument({
                    ofd: buffer,
                    success: function (result) {
                        if (result && result[0]) {
                            resolve(result[0]);
                        } else {
                            reject(new Error("elementOfdViewer: the OFD contains no document"));
                        }
                    },
                    fail: function (error) {
                        reject(new Error("elementOfdViewer: parse failed - " + String(error)));
                    }
                });
            } catch (e) {
                reject(e);
            }
        });
    }

    /**
     * 核心在页面没有模板层时会用 console.error 打「ofd:Template not found!」——
     * 那是它自己的正常分支（内容常常直接挂在页面层上），不是我们的错误。
     * 渲染期间临时把它过滤掉，免得把宿主的错误监控弄脏；其余日志原样放行。
     */
    function renderQuietly(render) {
        var original = console.error;
        var patched = function () {
            var first = arguments[0];
            if (typeof first === "string" && first.indexOf("ofd:Template not found") >= 0) {
                return;
            }
            return original.apply(console, arguments);
        };

        try {
            console.error = patched;
            return render();
        } finally {
            console.error = original;
        }
    }

    function resolveRenderWidth(container, payload) {
        var available = 0;

        try {
            available = container.clientWidth || 0;
        } catch (e) {
            available = 0;
        }

        if (!available) {
            available = payload.maxWidth || 900;
        }

        // 核心拿 (screenWidth - 10) / 页宽(mm) 当比例，所以这里只做最小值保护，
        // 余下的 24px 留给滚动条与外边距。
        return Math.max(320, available - 24);
    }

    function paint(container, ofdObject, payload) {
        container.innerHTML = "";

        var width = resolveRenderWidth(container, payload);
        var pages = renderQuietly(function () {
            return window.ofd.renderOfd(width, ofdObject);
        });

        var wrapper = document.createElement("div");
        wrapper.className = WRAPPER_CLASS;

        for (var i = 0; i < pages.length; i++) {
            var page = pages[i];
            page.className = page.className ? page.className + " " + PAGE_CLASS : PAGE_CLASS;
            page.setAttribute("data-ofd-page-index", String(i));
            wrapper.appendChild(page);
        }

        container.appendChild(wrapper);
        decorateAnnotationMarkers(wrapper);

        container[STATE_KEY] = {
            ofd: ofdObject,
            pages: pages,
            pageCount: pages.length,
            width: width,
            fileName: payload.fileName || null
        };

        return pages.length;
    }

    /**
     * 核心（批次100/101 补丁）把注释的交互信息写在包装节点的 data 属性上；
     * 这里给每个注释盒的右上角补一枚 (i) 角标，样式由 index.css 负责，
     * 打印隐藏同样交给那份样式表的 @media print —— 核心不认识组件样式。
     * 角标用真 button：Enter / Space 原生产生 click，事件冒泡到组件宿主，
     * 由 Blazor 侧开注释卡片，本通道不需要任何回调注册。
     */
    function decorateAnnotationMarkers(scope) {
        var annots = scope.querySelectorAll(ANNOT_SELECTOR);

        for (var i = 0; i < annots.length; i++) {
            var annot = annots[i];

            if (annot.querySelector(MARKER_SELECTOR)) {
                continue;
            }

            var marker = document.createElement("button");
            marker.type = "button";
            marker.className = MARKER_CLASS;
            marker.setAttribute("data-ofd-annot-marker", "1");
            marker.setAttribute("aria-label",
                annot.getAttribute("data-ofd-annot-remark") || "annotation");

            var dot = document.createElement("span");
            dot.className = MARKER_DOT_CLASS;
            dot.textContent = "i";
            marker.appendChild(dot);

            annot.appendChild(marker);
        }

        return annots.length;
    }

    /** 桥接是否可用。dom.js 被宿主引入就一定有它；核心是否可下载由 mount 自己兜底。 */
    window.elementOfdViewerAvailable = function () {
        return true;
    };

    /**
     * 把一份 OFD 挂到容器里。
     * @param {HTMLElement} container 组件给的挂载点
     * @param {string} payloadJson {"base64"|"url","fileName","maxWidth"}
     * @returns {Promise<number>} 页数
     */
    window.elementOfdViewerMount = function (container, payloadJson) {
        if (!container) {
            return Promise.reject(new Error("elementOfdViewer: container is null"));
        }

        var payload = {};
        try {
            payload = payloadJson ? JSON.parse(payloadJson) : {};
        } catch (e) {
            payload = {};
        }

        return ensureCore()
            .then(function () {
                return loadBytes(payload);
            })
            .then(function (buffer) {
                return parseDocument(buffer);
            })
            .then(function (ofdObject) {
                return paint(container, ofdObject, payload);
            });
    };

    /** 已挂载文档的页数；未挂载时为 0。 */
    window.elementOfdViewerPageCount = function (container) {
        var state = stateOf(container);
        return state ? state.pageCount : 0;
    };

    /**
     * 视口坐标命中角标时回该注释的内容 JSON，未命中回 null。
     * 刻意不收容器参数：#166 的教训是离开 DOM 的 ElementReference 到 JS 就是 null，
     * 纯坐标读取没有这份状态，也就没有那份终态。
     */
    window.elementOfdViewerAnnotationAt = function (clientX, clientY) {
        var el = null;

        try {
            el = document.elementFromPoint(clientX, clientY);
        } catch (e) {
            el = null;
        }

        var marker = el && el.closest ? el.closest(MARKER_SELECTOR) : null;
        var annot = marker && marker.closest ? marker.closest(ANNOT_SELECTOR) : null;

        if (!marker || !annot) {
            return null;
        }

        return JSON.stringify({
            remark: annot.getAttribute("data-ofd-annot-remark") || "",
            creator: annot.getAttribute("data-ofd-annot-creator") || "",
            time: annot.getAttribute("data-ofd-annot-time") || "",
            type: annot.getAttribute("data-ofd-annot-type") || "",
            id: annot.getAttribute("data-ofd-annot-id") || ""
        });
    };

    /** 最近的可滚动祖先（预览区通常就是这个，而不是 window）。 */
    function findScrollParent(el) {
        var node = el ? el.parentNode : null;
        while (node && node.nodeType === 1) {
            var style = null;
            try {
                style = window.getComputedStyle(node);
            } catch (e) {
                style = null;
            }
            var oy = style ? style.overflowY : "";
            if ((oy === "auto" || oy === "scroll" || oy === "overlay")
                && node.scrollHeight > node.clientHeight) {
                return node;
            }
            node = node.parentNode;
        }
        return null;
    }

    /** 滚到第 index 页（0 起）。 */
    window.elementOfdViewerScrollToPage = function (container, index) {
        var state = stateOf(container);
        if (!container || !state || state.pageCount <= 0) {
            return;
        }

        var page = state.pages[index];
        if (!page) {
            return;
        }

        // 优先自己算 scrollTop 只滚预览区：scrollIntoView 会连带滚动外层页面，
        // 翻页时整个文档跟着跳一下，很打扰。
        var scroller = findScrollParent(page);
        if (scroller) {
            var delta = page.getBoundingClientRect().top - scroller.getBoundingClientRect().top;
            var top = scroller.scrollTop + delta;
            try {
                scroller.scrollTop = top;
            } catch (e) {
                /* 只读滚动容器，忽略 */
            }
            return;
        }

        if (typeof page.scrollIntoView === "function") {
            try {
                page.scrollIntoView({ block: "start", inline: "nearest" });
            } catch (e) {
                page.scrollIntoView();
            }
        }
    };

    /** 销毁容器上的挂载状态（切换文档或组件释放时调用）。 */
    window.elementOfdViewerDispose = function (container) {
        if (!container) {
            return;
        }

        try {
            delete container[STATE_KEY];
        } catch (e) {
            container[STATE_KEY] = null;
        }

        container.innerHTML = "";
    };

    /**
     * 屏幕外挂一个打印 iframe，加载完调它自己的 print()。
     * @param {string} src iframe 要加载的地址（data: 的调用方应先转成 blob:）。
     * @param {string} fallbackUrl 打印指令被拒（跨域）时新窗口打开的地址。
     * @param {boolean} revoke 是否在打完后回收 blob URL。
     */
    function mountPrintFrame(src, fallbackUrl, revoke) {
        var frame = document.createElement("iframe");
        var objectUrl = revoke ? src : null;

        frame.setAttribute("title", "print");
        frame.setAttribute("aria-hidden", "true");
        frame.style.position = "fixed";
        frame.style.right = "0";
        frame.style.bottom = "0";
        frame.style.width = "1px";
        frame.style.height = "1px";
        frame.style.border = "0";
        // 不能 visibility:hidden：Chrome 的 PDF 插件在隐藏 iframe 里收不到
        // 打印指令。opacity:0（仍参与渲染、只是透明）是能弹起打印对话框的下限。
        frame.style.opacity = "0";

        frame.onload = function () {
            // 插件查看器 onload 之后还要一小会才接受打印指令。
            setTimeout(function () {
                try {
                    frame.contentWindow.focus();
                    frame.contentWindow.print();
                } catch (e) {
                    // 跨域：contentWindow 受限，退回新窗口打开。
                    window.open(fallbackUrl, "_blank");
                }

                setTimeout(function () {
                    if (objectUrl) {
                        URL.revokeObjectURL(objectUrl);
                    }

                    if (frame.parentNode) {
                        frame.parentNode.removeChild(frame);
                    }
                }, 60000);
            }, 250);
        };

        frame.src = src;
        document.body.appendChild(frame);
    }

    /**
     * 打印当前文档。ElViewerGrow 的打印桥接。
     * @param {string|null} url iframe 形态（PDF / 外链预览）的源地址；其余形态传 null。
     * @param {Element|null} root 查看器根元素（内容形态用来找画布），iframe 形态可传 null。
     *
     * 传 url：塞一个屏幕外的 iframe 让浏览器内建查看器加载，然后调它自己的
     * print() —— 页面级 window.print() 对 iframe 里的 PDF 插件并不可靠
     * （多数内核会把插件区域打成空白）。注意 data: URI 不能直接挂 iframe：
     * 它是不透明源，print() 会当跨域被拦，退回 window.open 又被 Chrome 的
     * 顶层 data: 导航拦截 —— 用户看到的「弹出一个空白页」就是这么来的。
     * 所以先在当前页上下文里把地址取成 blob（blob: 是同源 URL），整条打印
     * 路径只走同源；fetch 不可用或失败（跨域且无 CORS）才退回直接挂 iframe
     * / 新窗口的旧路径。
     * 传 null：把查看器画布（只有文件内容）连同页面样式序列化进一个
     * 独立 iframe 再调它的 print()。直接 window.print() 会把宿主页面上
     * 查看器之外的所有内容都排进打印队列（演示页打出 94 张纸的教训），
     * 而 visibility 隔离又会被前面的兄弟节点顶出若干空白页，所以走
     * iframe 重建文档：纸张上只有文件本身，按纸宽自然分页。
     */
    window.elementPrintDocument = function (url, root) {
        if (!url) {
            printContentDocument(root);
            return;
        }

        if (typeof fetch === "function" && typeof URL.createObjectURL === "function") {
            fetch(url).then(function (response) {
                if (!response.ok) {
                    throw new Error("HTTP " + response.status);
                }

                return response.blob();
            }).then(function (blob) {
                mountPrintFrame(URL.createObjectURL(blob), url, true);
            }).catch(function () {
                mountPrintFrame(url, url, false);
            });
            return;
        }

        mountPrintFrame(url, url, false);
    };

    /**
     * 内容打印：把查看器画布连同页面样式序列化进一个屏幕外的独立 iframe，
     * 再调它自己的 print()。
     *
     * 画布外的一概不带 —— 工具栏、状态栏、页面上其它的卡片与文案都不会
     * 出现在纸张上；iframe 的 body 没有定高与裁剪，长文档按纸宽自然分页。
     * 样式直接克隆当前页面的 <style>/<link>，组件的 @media print 规则
     * 会随之生效（复位缩放与旋转），百分比布局按 A4 近似纸宽（794px）计算。
     */
    function printContentDocument(root) {
        var canvas = null;

        if (root && root.querySelector) {
            canvas = root.querySelector(".el-viewer-grow__canvas");
        }

        if (!canvas) {
            // 找不到画布（理论到不了）退回整页打印，至少不丢功能。
            window.print();
            return;
        }

        var styles = "";
        var styleNodes = document.querySelectorAll("style, link[rel=\"stylesheet\"]");

        for (var i = 0; i < styleNodes.length; i++) {
            styles += styleNodes[i].outerHTML;
        }

        var frame = document.createElement("iframe");

        frame.setAttribute("title", "print");
        frame.setAttribute("aria-hidden", "true");
        frame.style.position = "fixed";
        frame.style.left = "-10000px";
        frame.style.top = "0";
        frame.style.width = "794px";
        frame.style.height = "1123px";
        frame.style.border = "0";

        document.body.appendChild(frame);

        var doc = frame.contentDocument || frame.contentWindow.document;

        doc.open();
        doc.write("<!DOCTYPE html><html><head><meta charset=\"utf-8\">"
            + styles
            + "</head><body>"
            + canvas.outerHTML
            + "</body></html>");
        doc.close();

        // 给样式与图片（data URI 基本瞬时）一拍加载时间，再交给打印。
        setTimeout(function () {
            try {
                frame.contentWindow.focus();
                frame.contentWindow.print();
            } catch (e) {
                // 打印被拒（极少数环境）就静默收场，不让页面报错。
            }

            setTimeout(function () {
                if (frame.parentNode) {
                    frame.parentNode.removeChild(frame);
                }
            }, 60000);
        }, 300);
    }
})();

/* ============================================================================
   TimePicker 滚轮 —— ElDateTimePicker / ElTimePicker 共用，对齐 Element Plus
   EP 默认「滚动选中」：列容器 .el-time-spinner__wrapper 可滚动，
   .el-time-spinner__list 上下各 80px 占位，使首/末项也能滚到中间那条选中带。
   - elementTimeSpinnerAlign(panel)                    把当前项停在选中带（幂等）
   - elementTimeSpinnerAttach(panel, dotnet, panelName) 绑定滚动/滚轮 + 居中
   - elementTimeSpinnerDispose(panel)                  解绑
   滚轮单独接管：浏览器原生滚轮一格 deltaY≈100px（≈3 行），会把三格一起带过；
   这里一格只走一项，并立即回传 .NET，保证「滚动步长 = 1」。
   监听挂在面板根上（而不是每一列）：选中带的伪元素宿主是 content，
   鼠标正对中间那一项时事件目标是 content，挂在列上会漏掉这种最常见的操作。
   打开面板时必须「收敛到当前时间」：面板是在 scaleY 展开动画里被插入的，
   那一刻量到的几何是压扁的，单次对齐会被算成 delta≈0 而失效 ——
   align 内部用 rAF 逐帧校正到稳定（stabilize），用户一滚动就让位。
   - elementPickerOutsideAttach(root, dropdownSelector, dotnet, methodName) 点面板外关闭
   - elementPickerOutsideDetach(root)
   ============================================================================ */
(function () {
    function columns(panel) {
        return panel ? panel.querySelectorAll('.el-time-spinner__wrapper') : [];
    }

    function items(wrap) {
        return wrap.querySelectorAll('.el-time-spinner__item');
    }

    // 选中带的中心（EP 口径）。带是 `.el-time-panel__content::before/::after` 画的：
    // `top:50%; margin-top:-15px; height:32px` ⇒ 带中心 = content 中心 + (-15 + 32/2) = content 中心 + 1。
    // 偏移从声明里算出来（不是写死的 1），伪元素读不到时按 0 退回容器中心。
    // 为什么要较真这 1px：EP 的落点公式是 `scrollTop = 索引 × 行高`（Temp/ep214_timepicker_basic-time-spinner.txt:237-240），
    // 而 `__list::before/after` 各占 80px（不是 (190-32)/2 = 79），所以 EP 的当前项上沿正好压在带上沿；
    // 若按容器中心收敛，实际 scrollTop 变成 `索引 × 32 + 1`，整列比 EP 高 1px（实测每列 Δ=-1px）。
    function bandCenter(wrap, wr) {
        var node = wrap ? wrap.parentElement : null;
        while (node && node.nodeType === 1) {
            if (node.className && String(node.className).indexOf('el-time-panel__content') >= 0) {
                var cr = node.getBoundingClientRect();
                if (cr.height < 1) { return -1; } // 还没布局完：调用方这一帧不动
                var offset = 0;
                try {
                    var cs = window.getComputedStyle(node, '::before');
                    var mt = parseFloat(cs.marginTop);
                    var hh = parseFloat(cs.height);
                    if (isFinite(mt) && isFinite(hh) && hh > 0) { offset = mt + hh / 2; }
                } catch (e) { /* 量不到就用 0 */ }
                return cr.top + cr.height / 2 + offset;
            }
            node = node.parentElement;
        }
        return wr.top + wr.height / 2; // 结构里没有 content：退回容器中心
    }

    // 把第 index 项滚到选中带中心。
    // 返回值：-1 = 还没布局完（这一帧量不到尺寸，必须重试）
    //          0 = 已经正好在选中带
    //          1 = 这一帧滚了
    // 弹层打开时面板还在 scaleY 展开动画里，几何是压扁的（实测面板只有 10px 高，
    // delta 会被算成 ~0），所以「一次对齐」会静默失效，调用方必须能重试（见 stabilize）。
    function center(wrap, index) {
        var list = items(wrap);
        if (!list.length) { return -1; }
        var idx = Math.max(0, Math.min(list.length - 1, index));
        var it = list[idx];
        var wr = wrap.getBoundingClientRect();
        var ir = it.getBoundingClientRect();
        if (wr.height < 1 || ir.height < 1) { return -1; }
        // 弹层用 scaleY 展开：动画期间 getBoundingClientRect 是「被压扁的视觉尺寸」，
        // 而 offsetHeight 是布局尺寸、不受 transform 影响。两者不一致就说明动画还没停，
        // 这一帧量到的 delta 不可信 —— 拿它收敛会把残余误差焊死在列上：实测动画中途
        // |Δ|<1 提前收手 ⇒ stabilize 停表 ⇒ 动画幅度一停，真实 Δ 变成 1~2px 且再不纠正。
        // 所以这里返回 -1（pending），让 stabilize 继续逐帧等到几何真的是最终值。
        if (Math.abs(wr.height - wrap.offsetHeight) > 0.5) { return -1; }
        var mid = bandCenter(wrap, wr);
        if (mid < 0) { return -1; }
        var delta = (ir.top + ir.height / 2) - mid;
        if (Math.abs(delta) < 1) { return 0; }
        var ctl = wrap.__elSpinnerCtl;
        wrap.scrollTop = wrap.scrollTop + delta;
        // 记下我们自己滚到哪儿：scroll 事件里靠它区分「对齐引起的滚动」和「用户滚动」
        if (ctl) { ctl.target = wrap.scrollTop; }
        return 1;
    }

    function raf(fn) {
        if (window.requestAnimationFrame) { return window.requestAnimationFrame(fn); }
        return setTimeout(fn, 16);
    }

    // 距离选中带中心最近的那一项
    function nearest(wrap) {
        var list = items(wrap);
        var wr = wrap.getBoundingClientRect();
        if (wr.height < 1) { return 0; }
        var mid = bandCenter(wrap, wr);
        if (mid < 0 || !isFinite(mid)) { mid = wr.top + wr.height / 2; }
        var best = 0;
        var bestD = Infinity;
        for (var i = 0; i < list.length; i++) {
            var r = list[i].getBoundingClientRect();
            var d = Math.abs((r.top + r.height / 2) - mid);
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    function activeIndex(wrap) {
        var list = items(wrap);
        var act = wrap.querySelector('.el-time-spinner__item.active');
        for (var i = 0; i < list.length; i++) {
            if (list[i] === act) { return i; }
        }
        return 0;
    }

    function valueAt(wrap, index) {
        var list = items(wrap);
        var raw = list[index] ? list[index].getAttribute('data-time-value') : '0';
        var v = parseInt(raw, 10);
        return isNaN(v) ? 0 : v;
    }

    function state(panel) {
        if (!panel.__elTimeSpinner) {
            panel.__elTimeSpinner = {
                bound: false, entries: [], dotnet: null, panelName: null, onWheel: null,
                raf: 0,       // 开面板对齐循环的句柄（0 = 没在跑）
                stopAlign: false
            };
        }
        return panel.__elTimeSpinner;
    }

    // 一行的高度：优先量相邻两项的间距，退化到单项高度，最后兜底 32
    function rowHeight(wrap) {
        var list = items(wrap);
        if (list.length > 1) {
            var a = list[0].getBoundingClientRect();
            var b = list[1].getBoundingClientRect();
            var d = Math.abs(b.top - a.top);
            if (d > 1) { return d; }
        }
        return list.length ? list[0].getBoundingClientRect().height : 32;
    }

    // 事件落在哪一列：先看事件目标，再用鼠标坐标兜底。
    // 选中带（.el-time-panel__content 的 ::before/::after 伪元素）画在列的上层，
    // 鼠标正对中间那一项时事件目标是 content 而不是列 —— 只看 target 滚轮就会失灵。
    function columnAt(panel, e) {
        var t = e.target;
        if (t && t.nodeType === 3) { t = t.parentNode; }
        while (t && t !== panel) {
            if (t.classList && t.classList.contains('el-time-spinner__wrapper')) { return t; }
            t = t.parentNode;
        }
        var cols = columns(panel);
        for (var i = 0; i < cols.length; i++) {
            var r = cols[i].getBoundingClientRect();
            if (e.clientX >= r.left && e.clientX <= r.right && e.clientY >= r.top && e.clientY <= r.bottom) {
                return cols[i];
            }
        }
        return null;
    }

    // 当前应该停在选中带上的索引：以 DOM 的 .active 为准。
    // 值也可能被 .NET 侧改动（点某一项、外部赋值），此时 ctl.last 是旧的，
    // 用它对齐会把列拉回旧索引（实测过：scrollTop 卡在索引 15 不动）。
    function currentIndex(wrap) {
        var list = items(wrap);
        var act = wrap.querySelector('.el-time-spinner__item.active');
        if (act) {
            for (var i = 0; i < list.length; i++) {
                if (list[i] === act) { return i; }
            }
        }
        var ctl = wrap.__elSpinnerCtl;
        return ctl ? ctl.last : 0;
    }

    // 一帧对齐：把每列的当前项拉回选中带
    function alignOnce(panel) {
        var cols = columns(panel);
        var moved = false;
        var pending = 0; // 还没布局完的列数
        for (var i = 0; i < cols.length; i++) {
            var wrap = cols[i];
            var res = center(wrap, currentIndex(wrap));
            if (res < 0) { pending++; } else if (res > 0) { moved = true; }
        }
        return { moved: moved, pending: pending };
    }

    function cancelAlign(panel) {
        var st = panel && panel.__elTimeSpinner;
        if (!st) { return; }
        st.stopAlign = true;
        st.raf = 0;
    }

    // 打开弹层的那一刻，面板还在 scaleY 展开动画里 —— 几何被压扁，
    // 只做一次对齐会算出 delta≈0 而静默失效（面板就一直停在 00，字段却是 08:30）。
    // 所以这里逐帧校正，直到「连续两帧都不需要再动」；用户一旦自己滚动/滚轮就立刻让位。
    function stabilize(panel) {
        var st = state(panel);
        if (st.raf) { return; }
        st.stopAlign = false;
        var deadline = (new Date()).getTime() + 2000;
        var stable = 0;

        function tick() {
            st.raf = 0;
            if (st.stopAlign || !document.documentElement.contains(panel)) { return; }
            var r = alignOnce(panel);
            // 还没布局完（面板尚未上屏 / 动画刚起步）不算「已经稳定」，继续等
            stable = (r.moved || r.pending) ? 0 : stable + 1;
            if (stable >= 2 || (new Date()).getTime() > deadline) { return; }
            st.raf = raf(tick);
        }

        st.raf = raf(tick);
    }

    window.elementTimeSpinnerAlign = function (panel) {
        if (!panel) { return; }
        alignOnce(panel);
        stabilize(panel);
    };

    // 给一列接线（幂等）：Blazor 重渲染把列元素换掉后再 attach 会自动补绑
    function bindColumn(st, wrap) {
        if (wrap.__elSpinnerCtl) { return; }
        var ctl = {
            part: wrap.getAttribute('data-part') || 'hour',
            // 范围模式同一面板里有 start / end 两组列，靠 data-time-panel 区分
            name: wrap.getAttribute('data-time-panel') || st.panelName || 'single',
            timer: null,
            acc: 0, // 滚轮累积量：不足一格的部分留到下一个 wheel 事件继续累
            last: activeIndex(wrap)
        };
        wrap.__elSpinnerCtl = ctl;

        ctl.commit = function (idx) {
            if (idx === ctl.last) { return; }
            ctl.last = idx;
            if (st.dotnet) {
                st.dotnet.invokeMethodAsync('OnTimeSpinnerSelect', ctl.name, ctl.part, valueAt(wrap, idx));
            }
        };

        ctl.settle = function () {
            var idx = nearest(wrap);
            center(wrap, idx);
            ctl.commit(idx);
        };

        ctl.onScroll = function () {
            // 区分「对齐循环自己滚的」和「用户滚的」：
            // 对齐过程中 scrollTop 由我们自己设，绝不能走 settle → commit，
            // 否则展开动画里的瞬时错位会被当成用户选择回传给 .NET。
            var ours = typeof ctl.target === 'number' && Math.abs(wrap.scrollTop - ctl.target) < 1.5;
            if (ours) { return; }
            if (st.raf) { st.stopAlign = true; st.raf = 0; } // 用户自己滚了：让位，别再抢
            if (ctl.timer) { clearTimeout(ctl.timer); }
            ctl.timer = setTimeout(ctl.settle, 100);
        };

        ctl.stop = function () {
            if (ctl.timer) { clearTimeout(ctl.timer); ctl.timer = null; }
        };

        // 把当前项挪一格（dir=+1 向下 / -1 向上），落点仍然吸附在选中带
        ctl.step = function (dir) {
            var list = items(wrap);
            if (!list.length) { return; }
            var from = nearest(wrap);
            var idx = Math.max(0, Math.min(list.length - 1, from + dir));
            if (idx === from) { return; }
            center(wrap, idx);
            ctl.commit(idx);
        };

        // 一格滚轮 = 一格时间：浏览器原生一格 deltaY≈100px（≈3 行），
        // 会一次把三格带过，所以这里吃掉事件、自己走一步并立刻回传 .NET。
        ctl.wheel = function (deltaY, deltaMode) {
            var list = items(wrap);
            if (!list.length) { return; }
            var h = rowHeight(wrap);
            var d = deltaY;
            if (deltaMode === 1) { d = d * h; }                       // DOM_DELTA_LINE
            else if (deltaMode === 2) { d = d * wrap.clientHeight; }  // DOM_DELTA_PAGE
            ctl.acc += d;
            // 半行即算一格：鼠标滚轮一格（100px）立刻走一步；
            // 触摸板的小步 delta 先累积，够了再走一步，同样每次只走一格。
            if (Math.abs(ctl.acc) < h * 0.5) { return; }
            var dir = ctl.acc > 0 ? 1 : -1;
            ctl.acc = 0;
            ctl.stop();
            // 用户已经用滚轮操作：停掉开面板的对齐收敛，免得两边抢滚动
            if (st.raf) { st.stopAlign = true; st.raf = 0; }
            ctl.step(dir);
        };

        wrap.addEventListener('scroll', ctl.onScroll);
        st.entries.push({ el: wrap, ctl: ctl });
        center(wrap, ctl.last);
    }

    window.elementTimeSpinnerAttach = function (panel, dotnet, panelName) {
        if (!panel) { return; }
        var st = state(panel);
        st.dotnet = dotnet;
        st.panelName = panelName;

        var cols = columns(panel);
        for (var i = 0; i < cols.length; i++) {
            bindColumn(st, cols[i]);
        }

        // 滚轮挂在整个面板上、按坐标找列：选中带会挡住列中心的事件目标，
        // 挂在列上时「鼠标正对中间那一项」就滚不动了。
        if (!st.onWheel) {
            st.onWheel = function (e) {
                var wrap = columnAt(panel, e);
                if (!wrap || !wrap.__elSpinnerCtl) { return; } // 不在任何列上：放行，页面照常滚
                if (e.cancelable) { e.preventDefault(); }
                wrap.__elSpinnerCtl.wheel(e.deltaY, e.deltaMode);
            };
            panel.addEventListener('wheel', st.onWheel, { passive: false });
        }

        st.bound = true;
        window.elementTimeSpinnerAlign(panel);
    };

    window.elementTimeSpinnerDispose = function (panel) {
        if (!panel || !panel.__elTimeSpinner) { return; }
        var st = panel.__elTimeSpinner;
        st.stopAlign = true;
        st.raf = 0;
        for (var i = 0; i < st.entries.length; i++) {
            var entry = st.entries[i];
            if (entry.ctl) {
                entry.el.removeEventListener('scroll', entry.ctl.onScroll);
                entry.el.__elSpinnerCtl = null;
            } else if (entry.handler) {
                entry.el.removeEventListener('scroll', entry.handler);
            }
        }
        if (st.onWheel) { panel.removeEventListener('wheel', st.onWheel); }
        panel.__elTimeSpinner = null;
    };

    window.elementPickerOutsideAttach = function (root, dropdownSelector, dotnet, methodName) {
        if (!root || root.__elOutsideHandler) { return; }
        root.__elOutsideHandler = function (e) {
            var t = e.target;
            if (t && t.nodeType === 3) { t = t.parentNode; }
            if (!t) { return; }
            if (root === t || root.contains(t)) { return; }
            var dds = document.querySelectorAll(dropdownSelector);
            for (var i = 0; i < dds.length; i++) {
                if (dds[i] === t || dds[i].contains(t)) { return; }
            }
            if (dotnet) { dotnet.invokeMethodAsync(methodName); }
        };
        document.addEventListener('mousedown', root.__elOutsideHandler, true);
    };

    window.elementPickerOutsideDetach = function (root) {
        if (!root || !root.__elOutsideHandler) { return; }
        document.removeEventListener('mousedown', root.__elOutsideHandler, true);
        root.__elOutsideHandler = null;
    };
})();

/* ==========================================================================
   ElDropDownTableGrow 弹层定位（ES5，末尾独立 IIFE）
   面板以 position:fixed 渲染，逃逸表格滚动容器（.el-table__body-wrapper overflow:auto /
   .el-table overflow:hidden）的裁剪；这里按触发器的屏幕坐标写 top/left/max-height，
   并在任意祖先滚动 / 窗口缩放时重定位（scroll 用捕获阶段监听才收得到内部容器的滚动）。
   面板内的滚动边界由 CSS 的 overscroll-behavior:contain 兜底，滚轮不会外溢。
   ========================================================================== */
(function () {
    var registry = {};   // id -> { trigger, panel, onScroll, onResize }

    function viewport() {
        return {
            vw: window.innerWidth || document.documentElement.clientWidth || 0,
            vh: window.innerHeight || document.documentElement.clientHeight || 0
        };
    }

    // 量内层表格的「真实内容宽」：ElTable 把每列宽写在 <col width> 上，且列宽会随容器拉伸/压缩，
    // 直接量面板只会得到被钳制后的宽度。这里累加列宽之和（与容器无关，得到不挤压也不撑破的自然宽），
    // 再加上面板自身的边框 + 滚动条 + 左右内边距，得到面板应设的目标宽度。取不到（表格未渲染）返回 0。
    function measureContentWidth(p) {
        var header = p.querySelector('.el-table__header') || p.querySelector('.el-table__body');
        var colEls = header ? header.querySelectorAll('col') : p.querySelectorAll('.el-table col');
        var sum = 0, n = 0, i;
        for (i = 0; i < colEls.length; i++) {
            var w = parseFloat(colEls[i].getAttribute('width')) || parseFloat(colEls[i].style.width) || 0;
            if (w > 0) { sum += w; n++; }
        }
        // 部分结构下表头/表体各有一份 colgroup，n 会翻倍 —— 用表头 th 数量校正回单列集合计
        var thCount = p.querySelectorAll('.el-table__header th').length;
        if (n > 0 && thCount > 0 && n >= thCount * 2) { sum = sum / (n / thCount); }
        if (!sum) { return 0; }
        // 面板边框 + 可能的纵向滚动条 + 左右内边距都会占用内容区，需补进目标宽度
        var cs = getComputedStyle(p);
        var padX = (parseFloat(cs.paddingLeft) || 0) + (parseFloat(cs.paddingRight) || 0);
        var frame = (p.offsetWidth - p.clientWidth) + padX;
        return Math.max(sum + frame + 16, 0); // +16 吸收内层 scrollbar-gutter 等内边距，避免横向滚动条
    }

    function place(entry) {
        var t = entry.trigger, p = entry.panel;
        if (!t || !p || !t.getBoundingClientRect) { return; }
        var r = t.getBoundingClientRect();
        var vp = viewport();
        var gap = 4;
        var margin = 8;

        // 宽度：调用方显式设了 PanelWidth/Width 时（inline style.width 非空且非 auto）保持原值；
        // 否则按「内层表格的真实列宽」自适应，封顶视口、且不小于触发器宽度。
        // data-el-auto-width 标记区分「JS 自动算」与「调用方显式设」：自动宽度每次 reposition 都重测，
        // 因为内嵌表格是异步渲染的，首帧量到的宽度会偏小（列宽还没算好）。
        var autoW = p.getAttribute('data-el-auto-width') === '1';
        var hasExplicitW = !autoW && p.style.width && p.style.width !== 'auto' && p.style.width !== '';
        if (!hasExplicitW) {
            p.setAttribute('data-el-auto-width', '1');
            var natural = measureContentWidth(p);
            if (!natural || natural < r.width) { natural = r.width; }
            var cap = vp.vw - margin * 2;
            if (natural > cap) { natural = cap; }
            var cur = p.offsetWidth || 0;
            if (Math.abs(natural - cur) > 1) {
                p.style.minWidth = '0px';
                p.style.width = natural + 'px';
            }
        }
        var pw = p.offsetWidth || r.width;
        var left = r.left;
        if (left + pw > vp.vw - margin) { left = vp.vw - margin - pw; }
        if (left < margin) { left = margin; }

        // 默认向下展开；下方空间不足且上方更宽裕时改为向上展开
        var below = vp.vh - r.bottom - gap - margin;
        var above = r.top - gap - margin;
        var goUp = below < 160 && above > below;
        var maxH = goUp ? above : below;
        if (maxH < 120) { maxH = Math.max(above, below); }
        if (maxH < 0) { maxH = 0; }
        p.style.left = Math.round(left) + 'px';
        p.style.maxHeight = Math.round(maxH) + 'px';

        // 向上展开必须锚 bottom：max-height 只是「上限」，面板真实高度按内容算。
        // 原先写的是 top = r.top - gap - maxH，而 goUp 时 maxH 就等于「上方全部余量」(r.top - gap - margin)，
        // 代入得 top ≡ margin ⇒ 面板被钉到视口顶，与触发器之间空出一大段（实测 gap 176px、面板只占 298px 高）。
        // fixed 下 top/bottom 同时给值会把盒子按两条边拉伸，所以另一条必须显式 auto。
        if (goUp) {
            p.style.top = 'auto';
            var b = vp.vh - r.top + gap;
            if (b > vp.vh - margin) { b = vp.vh - margin; }
            p.style.bottom = Math.round(b) + 'px';
        } else {
            p.style.bottom = 'auto';
            var tTop = r.bottom + gap;
            if (tTop < margin) { tTop = margin; }
            p.style.top = Math.round(tTop) + 'px';
        }
        if (p.classList) { p.classList.add('is-positioned'); }
    }

    function attach(id, trigger, panel) {
        detach(id);
        if (!trigger || !panel) { return; }
        var entry = { trigger: trigger, panel: panel };
        entry.onScroll = function () { place(entry); };
        entry.onResize = function () { place(entry); };
        // 内层表格异步渲染（分页 / 列宽计算在 ElTable 自己的 OnAfterRenderAsync 之后才会落地），
        // 会改变面板内容尺寸。用 ResizeObserver 在其布局完成后重测宽度 + 定位，
        // 避免首帧量到的宽度偏窄把某一列挤掉。仅当新宽度与当前相差 >1px 才回写，防抖动死循环。
        if (typeof ResizeObserver !== 'undefined') {
            entry.ro = new ResizeObserver(function () { place(entry); });
            entry.ro.observe(panel);
        }
        registry[id] = entry;
        place(entry);
        // 首帧布局可能尚未完成（表格列宽未算好），下一动画帧再测一次兜底
        if (typeof requestAnimationFrame !== 'undefined') {
            requestAnimationFrame(function () { place(entry); });
        }
        window.addEventListener('scroll', entry.onScroll, true);
        window.addEventListener('resize', entry.onResize);
    }

    function detach(id) {
        var e = registry[id];
        if (!e) { return; }
        if (e.ro && e.ro.disconnect) { e.ro.disconnect(); }
        window.removeEventListener('scroll', e.onScroll, true);
        window.removeEventListener('resize', e.onResize);
        delete registry[id];
    }

    window.elementDropDownTablePanelAttach = function (id, trigger, panel) {
        attach(id, trigger, panel);
    };

    window.elementDropDownTablePanelReposition = function (id) {
        var e = registry[id];
        if (e) { place(e); }
    };

    window.elementDropDownTablePanelDetach = function (id) {
        detach(id);
    };
})();

// ===== ElDrawer: 焦点自动移入 + 拖拽调整大小（对齐 Element Plus 2.11 官方 API） =====
window.elementDrawerFocusContent = function (drawerEl) {
    if (!drawerEl) {
        return;
    }

    var selector = 'a[href],button:not([disabled]),input:not([disabled]),select:not([disabled]),textarea:not([disabled]),[tabindex]:not([tabindex="-1"])';
    var nodes = drawerEl.querySelectorAll(selector);
    for (var i = 0; i < nodes.length; i++) {
        var el = nodes[i];
        if (el.offsetWidth > 0 || el.offsetHeight > 0) {
            el.focus();
            return;
        }
    }
};

window.elementDrawerEnableResize = function (drawerEl, handlerEl, direction, dotNet) {
    if (!drawerEl || !handlerEl || !dotNet) {
        return;
    }

    function limit(size) {
        return size < 60 ? 60 : Math.round(size);
    }

    function calc(e) {
        if (direction === 'rtl') {
            return window.innerWidth - e.clientX;
        }
        if (direction === 'ltr') {
            return e.clientX;
        }
        if (direction === 'ttb') {
            return e.clientY;
        }
        return window.innerHeight - e.clientY;
    }

    function onMove(e) {
        if (!handlerEl._drawerDragging) {
            return;
        }
        dotNet.invokeMethodAsync('JsResize', limit(calc(e)));
    }

    function onUp(e) {
        if (!handlerEl._drawerDragging) {
            return;
        }
        handlerEl._drawerDragging = false;
        document.removeEventListener('mousemove', handlerEl._drawerMove);
        document.removeEventListener('mouseup', handlerEl._drawerUp);
        dotNet.invokeMethodAsync('JsResizeEnd', limit(calc(e)));
    }

    function onDown(e) {
        if (!e || e.button !== 0) {
            return;
        }
        handlerEl._drawerDragging = true;
        if (e.preventDefault) {
            e.preventDefault();
        }
        if (e.stopPropagation) {
            e.stopPropagation();
        }
        dotNet.invokeMethodAsync('JsResizeStart', limit(calc(e)));
        handlerEl._drawerMove = onMove;
        handlerEl._drawerUp = onUp;
        document.addEventListener('mousemove', onMove);
        document.addEventListener('mouseup', onUp);
    }

    handlerEl._drawerDown = onDown;
    handlerEl.addEventListener('mousedown', onDown);
};

window.elementDrawerDisableResize = function (handlerEl) {
    if (!handlerEl || !handlerEl._drawerDown) {
        return;
    }
    handlerEl.removeEventListener('mousedown', handlerEl._drawerDown);
    handlerEl._drawerDown = null;
};

/* 量出插槽前后缀（Prefix / Suffix RenderFragment）实际占用的宽度，
   交给 C# 写成 input 的内联 padding —— 否则绝对定位的插槽内容会压在输入文字上。
   返回值 [前缀需要的 padding-left, 后缀需要的 padding-right]，0 表示该侧无插槽内容。 */
window.elementInputMeasureAffix = function (el) {
    if (!el || !el.closest) {
        return [0, 0];
    }

    var root = el.closest('.el-input');

    if (!root) {
        return [0, 0];
    }

    var inputRect = el.getBoundingClientRect();

    if (!inputRect.width) {
        return [0, 0];
    }

    /* 动态图标（清空 / 密码 / 字数）有固定的预留宽度，不参与插槽测量，避免 hover 时文字跳动。 */
    var dynamicClasses = [
        'el-input__clear',
        'el-input__password',
        'el-input__count',
        'el-input__count-inner',
        'el-input__validateIcon'
    ];

    var isDynamic = function (node) {
        for (var i = 0; i < dynamicClasses.length; i++) {
            if (node.classList.contains(dynamicClasses[i])) {
                return true;
            }
        }

        return false;
    };

    var GAP = 8;

    var measure = function (selector, edge) {
        var inner = root.querySelector(selector);

        if (!inner) {
            return 0;
        }

        var reach = 0;

        Array.prototype.forEach.call(inner.children, function (node) {
            if (isDynamic(node)) {
                return;
            }

            var rect = node.getBoundingClientRect();

            if (!rect.width) {
                return;
            }

            /* edge = 'right'：前缀取最右边界；'left'：后缀取最左边界。 */
            reach = edge === 'right'
                ? Math.max(reach, rect.right - inputRect.left)
                : Math.max(reach, inputRect.right - rect.left);
        });

        return reach > 0 ? Math.ceil(reach) + GAP : 0;
    };

    return [measure('.el-input__prefix-inner', 'right'), measure('.el-input__suffix-inner', 'left')];
};

window.elementInputBindComposition = function (el, dotNet) {
    if (!el || !dotNet || el.__elCompositionBound) {
        return;
    }

    el.__elCompositionBound = true;

    var onStart = function () {
        dotNet.invokeMethodAsync('OnCompositionStartFromJs');
    };

    var onUpdate = function (e) {
        dotNet.invokeMethodAsync('OnCompositionUpdateFromJs', (e && e.data) ? e.data : '');
    };

    var onEnd = function (e) {
        dotNet.invokeMethodAsync('OnCompositionEndFromJs', (e && e.data) ? e.data : '');
    };

    el.addEventListener('compositionstart', onStart);
    el.addEventListener('compositionupdate', onUpdate);
    el.addEventListener('compositionend', onEnd);

    el.__elCompositionUnbind = function () {
        el.removeEventListener('compositionstart', onStart);
        el.removeEventListener('compositionupdate', onUpdate);
        el.removeEventListener('compositionend', onEnd);
        el.__elCompositionBound = false;
        el.__elCompositionUnbind = null;
    };
};

window.elementInputUnbindComposition = function (el) {
    if (el && el.__elCompositionUnbind) {
        el.__elCompositionUnbind();
    }
};

/* ==== ElWatermark 防篡改（对齐 element-plus 内部 MutationObserver 机制） ==== */

window.elementWatermarkProtect = function (el, dotNet) {
    if (!el || !dotNet || el.__elWmBound) {
        return;
    }

    el.__elWmBound = true;
    el.__elWmDotNet = dotNet;
    el.__elWmHost = el.parentNode;

    var notify = function () {
        if (el.__elWmMuted || !el.__elWmDotNet) {
            return;
        }

        try {
            el.__elWmDotNet.invokeMethodAsync('OnWatermarkTamperedFromJs');
        } catch (e) {
        }
    };

    var observer = new MutationObserver(function (mutations) {
        for (var i = 0; i < mutations.length; i++) {
            var mutation = mutations[i];

            if (mutation.type === 'attributes' && mutation.target === el) {
                notify();
                return;
            }

            if (mutation.removedNodes && mutation.removedNodes.length) {
                for (var j = 0; j < mutation.removedNodes.length; j++) {
                    if (mutation.removedNodes[j] === el) {
                        notify();
                        return;
                    }
                }
            }
        }
    });

    observer.observe(el, { attributes: true, attributeFilter: ['style'] });
    if (el.__elWmHost) {
        observer.observe(el.__elWmHost, { childList: true });
    }

    el.__elWmObserver = observer;
};

window.elementWatermarkRestore = function (el, style) {
    if (!el || typeof style !== 'string') {
        return;
    }

    el.__elWmMuted = true;

    try {
        if (!el.isConnected && el.__elWmHost && el.__elWmHost.isConnected) {
            el.__elWmHost.appendChild(el);
        }

        el.setAttribute('style', style);
    } catch (e) {
    }

    window.setTimeout(function () {
        el.__elWmMuted = false;
    }, 0);
};

window.elementWatermarkRelease = function (el) {
    if (!el) {
        return;
    }

    if (el.__elWmObserver) {
        try {
            el.__elWmObserver.disconnect();
        } catch (e) {
        }

        el.__elWmObserver = null;
    }

    el.__elWmBound = false;
    el.__elWmDotNet = null;
};

/* ==========================================================================
   ElProTabsGrow 页签导航：溢出测量 + 横向滚动 + 「更多」面板的点击外部关闭

   为什么需要 JS：CSS 无法知道内容是否溢出（因此无法决定左右滚动箭头是否出现），
   也无法在「点了组件外面」时通知 .NET 关掉「更多」下拉。这里把测量结果回推给
   ElProTabsGrow.OnProTabsScrollState，组件只负责按状态渲染。
   一律 ES5 写法（与 dom.js 其余部分保持一致，兼容项目里要在旧 Chromium 上跑的宿主）。
   ========================================================================== */
(function () {
    // 量一次并回推；状态没变就不打扰 .NET（避免滚动时高频重渲染）
    window.elementProTabsReport = function (root) {
        var st = root && root.__elProTabs;

        if (!st || !st.scroll) {
            return;
        }

        var el = st.scroll;
        var hasScroll = (el.scrollWidth - el.clientWidth) > 1;
        var atStart = el.scrollLeft <= 1;
        var atEnd = (el.scrollLeft + el.clientWidth) >= (el.scrollWidth - 1);

        if (st.last && st.last.h === hasScroll && st.last.s === atStart && st.last.e === atEnd) {
            return;
        }

        st.last = { h: hasScroll, s: atStart, e: atEnd };

        if (st.dotnet) {
            st.dotnet.invokeMethodAsync('OnProTabsScrollState', hasScroll, atStart, atEnd);
        }
    };

    // 挂载测量：scroll 事件 + ResizeObserver（容器/内容尺寸变化都要重算）
    window.elementProTabsAttach = function (root, scrollEl, dotnet) {
        if (!root) {
            return;
        }

        var scroll = scrollEl || root.querySelector('.el-pro-tabs-grow__scroll');

        if (!scroll) {
            return;
        }

        var st = root.__elProTabs;

        if (st && st.scroll === scroll && st.dotnet === dotnet) {
            window.elementProTabsReport(root);
            return;
        }

        window.elementProTabsDetach(root);

        st = { scroll: scroll, dotnet: dotnet, last: null, onScroll: null, observer: null };

        st.onScroll = function () {
            window.elementProTabsReport(root);
        };

        scroll.addEventListener('scroll', st.onScroll, { passive: true });

        if (typeof ResizeObserver === 'function') {
            st.observer = new ResizeObserver(function () {
                window.elementProTabsReport(root);
            });
            st.observer.observe(scroll);

            if (scroll.firstElementChild) {
                st.observer.observe(scroll.firstElementChild);
            }
        } else {
            window.addEventListener('resize', st.onScroll);
        }

        root.__elProTabs = st;

        // 等 Blazor 这一帧把 DOM 落稳（字体/图片可能改变宽度）再量
        if (typeof requestAnimationFrame === 'function') {
            requestAnimationFrame(function () {
                window.elementProTabsReport(root);
            });
        } else {
            window.setTimeout(function () {
                window.elementProTabsReport(root);
            }, 0);
        }
    };

    window.elementProTabsDetach = function (root) {
        var st = root && root.__elProTabs;

        if (!st) {
            return;
        }

        if (st.scroll && st.onScroll) {
            st.scroll.removeEventListener('scroll', st.onScroll);
        }

        if (st.observer) {
            st.observer.disconnect();
        } else if (st.onScroll) {
            window.removeEventListener('resize', st.onScroll);
        }

        root.__elProTabs = null;
    };

    window.elementProTabsScrollBy = function (root, delta) {
        var st = root && root.__elProTabs;

        if (!st || !st.scroll) {
            return;
        }

        try {
            st.scroll.scrollBy({ left: delta, behavior: 'smooth' });
        } catch (e) {
            st.scroll.scrollLeft += delta;
        }
    };

    // 把某个页签滚入可视区（用 getBoundingClientRect 算，不依赖 offsetParent 链）
    window.elementProTabsScrollToTab = function (root, path) {
        var st = root && root.__elProTabs;

        if (!st || !st.scroll || !path) {
            return;
        }

        var tabs = st.scroll.querySelectorAll('[data-path]');
        var tab = null;

        for (var i = 0; i < tabs.length; i++) {
            if (tabs[i].getAttribute('data-path') === path) {
                tab = tabs[i];
                break;
            }
        }

        if (!tab) {
            return;
        }

        var outer = st.scroll.getBoundingClientRect();
        var inner = tab.getBoundingClientRect();

        if (inner.left < outer.left) {
            st.scroll.scrollLeft += (inner.left - outer.left) - 4;
        } else if (inner.right > outer.right) {
            st.scroll.scrollLeft += (inner.right - outer.right) + 4;
        }
    };

    // 「更多」面板：点击组件外部时通知 .NET 收起
    window.elementProTabsMoreOpen = function (root, dotnet) {
        if (!root || root.__elProTabsDown) {
            return;
        }

        root.__elProTabsDown = function (e) {
            var t = e.target;

            if (t && t.nodeType === 3) {
                t = t.parentNode;
            }

            if (!t || root === t || root.contains(t)) {
                return;
            }

            if (dotnet) {
                dotnet.invokeMethodAsync('OnProTabsOutsideClick');
            }
        };

        document.addEventListener('mousedown', root.__elProTabsDown, true);
    };

    window.elementProTabsMoreClose = function (root) {
        if (!root || !root.__elProTabsDown) {
            return;
        }

        document.removeEventListener('mousedown', root.__elProTabsDown, true);
        root.__elProTabsDown = null;
    };
})();

// ===== ElCarousel 图片预览相关 =====

// 从各轮播项收集第一个 <img> 的地址（已解析绝对地址，含 base64 data URI）；无图的项对应位置为 null。
window.elementCarouselCollectImages = function (root) {
    if (!root) return [];
    var items = root.querySelectorAll('.el-carousel__item');
    var imgs = [];
    for (var i = 0; i < items.length; i++) {
        var img = items[i].querySelector('img');
        imgs.push(img ? (img.currentSrc || img.src) : null);
    }
    return imgs;
};

// 给尚未加载完成的 <img> 绑定一次性 load 事件，加载完后回调 .NET 重新收集。
window.elementCarouselBindImgLoad = function (root, dotNet, method) {
    if (!root || !dotNet) return;
    var imgs = root.querySelectorAll('.el-carousel__item img');
    for (var i = 0; i < imgs.length; i++) {
        (function (img) {
            if (!img.complete) {
                img.addEventListener('load', function () {
                    dotNet.invokeMethodAsync(method);
                }, { once: true });
            }
        })(imgs[i]);
    }
};

// 预览打开时绑定 ESC 关闭；再次调用（或关闭时）解绑，避免重复监听。
var __carouselPreviewKeyHandler = null;
window.elementCarouselBindPreviewKeys = function (dotNet, method) {
    window.elementCarouselUnbindPreviewKeys();
    __carouselPreviewKeyHandler = function (e) {
        if (e.key === 'Escape' || e.key === 'Esc') {
            dotNet.invokeMethodAsync(method);
        } else if (e.key === 'ArrowLeft') {
            dotNet.invokeMethodAsync('PrevPreviewAsync');
        } else if (e.key === 'ArrowRight') {
            dotNet.invokeMethodAsync('NextPreviewAsync');
        }
    };
    document.addEventListener('keydown', __carouselPreviewKeyHandler);
};
window.elementCarouselUnbindPreviewKeys = function () {
    if (__carouselPreviewKeyHandler) {
        document.removeEventListener('keydown', __carouselPreviewKeyHandler);
        __carouselPreviewKeyHandler = null;
    }
};

// ==== ElTableV2（虚拟滚动表格）：滚动位置读取 / 设置（ES5） ====
window.elementTableV2ScrollTop = function (root) {
    if (root && root.scrollTop) return root.scrollTop;
    return 0;
};
window.elementTableV2ScrollTo = function (root, top, left) {
    if (!root) return;
    if (typeof top === 'number' && !isNaN(top) && isFinite(top)) root.scrollTop = top;
    if (typeof left === 'number' && !isNaN(left) && isFinite(left)) root.scrollLeft = left;
};

// ==== ElTableV2 滚动性能优化（rAF 节流 + 直接下发 scrollTop，消除快速滚动空白/停滞） ====
// 背景：原先的 @onscroll 每次原生滚动事件都走一次 JSRuntime 回读 scrollTop + StateHasChanged，
// 高频滚动时「JS→C# 往返」与「渲染排队」叠加，使 _scrollTop 落后真实位置超过 overscan*RowHeight，
// 渲染窗口覆盖不到视口 ⇒ 出现空白/停滞（实测 400 行快速滚动即复现）。
// 改法：scroll 事件只记录最新位置并 requestAnimationFrame，每帧最多回调一次 C#，
// 且直接把该帧最新的 scrollTop/Left 作为参数传入（不再异步回读），
// ① 把渲染频率钉在刷新率上（≤60fps），消除排队；② 始终用最新位置，消除滞后。
//    再配合组件侧「滚动中动态加大 overscan 缓冲」，可覆盖极端快速滚动的子帧位移。
window.elementTableV2BindScroll = function (root, dotNetRef) {
    if (!root || root.__elTableV2Bound) return;
    root.__elTableV2Bound = true;
    // ⚠️ 必须取整再下发：非整数 devicePixelRatio / 页面缩放会让 scrollTop 变成小数
    //（实测 391.20001220703125），System.Text.Json 反序列化成 Int32 会抛
    // FormatException，整个 [JSInvokable] 调用失败 ⇒ _scrollTop 不更新、DOM 不重渲染 ⇒ 空白。
    var latestTop = Math.round(root.scrollTop) || 0;
    var latestLeft = Math.round(root.scrollLeft) || 0;
    var scheduled = false;
    root.__elTableV2OnScroll = function () {
        latestTop = Math.round(root.scrollTop) || 0;
        latestLeft = Math.round(root.scrollLeft) || 0;
        if (scheduled) return;
        scheduled = true;
        requestAnimationFrame(function () {
            scheduled = false;
            if (root.__elTableV2Disposed) return;
            var p = dotNetRef.invokeMethodAsync('OnTableV2Scroll', latestTop, latestLeft);
            if (p && p.catch) p.catch(function () { /* 组件已释放/间隔内失联，忽略 */ });
        });
    };
    root.addEventListener('scroll', root.__elTableV2OnScroll, { passive: true });
};
window.elementTableV2UnbindScroll = function (root) {
    if (!root) return;
    root.__elTableV2Disposed = true;
    if (root.__elTableV2OnScroll) {
        root.removeEventListener('scroll', root.__elTableV2OnScroll);
        root.__elTableV2OnScroll = null;
    }
    root.__elTableV2Bound = false;
};

// ==== 细滚动条「鼠标移入才显形」（ES5） ====
// 为什么必须用 JS：Blink **不会**因 :hover 状态变化去重绘 ::-webkit-scrollbar 伪元素。
// 实测四条纯 CSS 写法在「静止 / 悬停容器 / 指针移开」三态下像素完全相同（headful msedge，页面截图 + 4px 条带像素分析）：
//   ① *:hover::-webkit-scrollbar-thumb      ② div:hover::-webkit-scrollbar-thumb
//   ③ [class]:hover::-webkit-scrollbar-thumb ④ CSS 变量方案 *:hover{--sb}（还会因变量继承而静止态就显形）
// 唯一能让 Blink 用新样式重绘滚条的手段是「重建滚动条」—— 即把 overflow 关掉再还原（实测必须跨帧，同帧开关会被合并）。
// 故本函数在指针进入/离开滚动容器时做一次跨帧 overflow 开关，让滚条按**当前 :hover 状态**重建。
// A/B 实测（摘掉本函数后对比）：静止 238/dark=0 → 仅悬停 238/dark=0（不显形）→ 悬停+重建 214/dark=119（显形）；
// 有本函数时「仅悬停」即为 214 ⇒ 显形确实靠这次重建。
// ⚠️ **不要用「加 class」来上色**：Blazor 重渲染会整体重写元素的 class属性把 JS 加的类抹掉，
//    且「已缓存为 hot」的判断会让后续 pointerover 走短路不再补类（实测出现过 hot 类被抹后永不恢复）。
//    正确做法就是**只重建**——上色交给 CSS 里已存在的 :hover 规则（它在重建时能正确命中，
//    实测 214 ≈ rgba(0,0,0,.16) 叠在白底上）。
// 配合 scrollbar-gutter:stable 保证重建期间槽位不消失 ⇒ 内容不会左右抖 4px。
//
// ⚠️ **必须是「真滚动容器」才允许重建**（2026-10-03 修复闪烁的根因）：
//   旧判定只看内容溢出量 `scrollHeight-clientHeight>1 || scrollWidth-clientWidth>1`，
//   会把**普通布局容器**误判为滚动容器。触发条件很隐蔽：`position:absolute` 的浮层
//   （ElSelectV2 下拉、picker 面板…）本身就是所在容器的子孙，**会把祖先的 scrollHeight 撑大**，
//   于是「浮层一打开 → 祖先看起来可滚动 → 被重建 → 浮层被 overflow:hidden 裁掉两帧 → 位移/消失」
//   形成自馈振荡，鼠标一动就连闪。
//   实测（demo /form2，ElSelectV2 下拉，1062→1070px 抖动 + 命中测试）：
//     · `.demo-row`（computed overflow:visible，仅因弹层撑大 scrollHeight 179px 而被选中）
//       一旦被置 overflow:hidden，弹层底部（超出其 40px 盒子）整块消失，还原后恢复 ⇒ 即闪烁；
//     · 同批被误选的还有 `.demo-section` / `.el-input`（均 overflow:visible），
//       它们被写 scrollbar-gutter:stable 后各自多留 4px 槽 ⇒ 内容宽度 1070→1066→1062 来回跳。
//   修法：先看 computed overflowX/Y 是否为 auto/scroll/overlay（即**真的会出现原生滚动条**），
//   不是就直接排除 —— 这样既消除误判，又省掉对每个祖先强制 layout 的开销
//   （旧写法对每个祖先都读 scrollHeight，而它必须先完成布局计算）。
window.elementBindFadeScrollbar = function (root) {
    var host = root || document;

    if (host.__elFadeSbBound) {
        return;
    }

    host.__elFadeSbBound = true;

    var last = null;

    // 只有「内容真的溢出、且该轴向的 overflow 允许出现原生滚动条」才算滚动容器。
    // overflow:visible / clip / hidden 都不会渲染滚动条 ⇒ 重建对它们毫无意义，
    // 反而会因临时 overflow:hidden 裁剪掉容器内的绝对定位浮层（详见上方注释）。
    function isScrollContainer(el) {
        var cs = getComputedStyle(el);
        var ys = cs.overflowY === 'auto' || cs.overflowY === 'scroll' || cs.overflowY === 'overlay';
        var xs = cs.overflowX === 'auto' || cs.overflowX === 'scroll' || cs.overflowX === 'overlay';
        if (!ys && !xs) {
            return false;
        }
        return (ys && (el.scrollHeight - el.clientHeight > 1)) || (xs && (el.scrollWidth - el.clientWidth > 1));
    }

    function nearestScroller(node) {
        while (node && node.nodeType === 1 && node !== host && node !== document.body) {
            if (isScrollContainer(node)) {
                return node;
            }
            node = node.parentElement;
        }
        return null;
    }

    // 虚拟滚动容器（ElTableV2 / ElTreeV2）自行管理滚动位置与渲染窗口：
        // 跨帧 overflow 切换会让 Blazor 在 hidden 那两帧重渲染（窗口随最新 scrollTop 变），
        // 随后 repaint 把 scrollTop 钉回「切换前旧值」⇒ 滚动位置回跳/闪烁。
        // 故这类容器只钉住 scrollbar-gutter（避免悬停时宽度抖动），不做 overflow 切换重建。
        function isVirtualScroller(el) {
            if (!el || !el.classList) return false;
            return el.classList.contains('el-table-v2') ||
                   el.classList.contains('el-tree-v2__body') ||
                   el.classList.contains('el-tree-v2');
        }

    function repaint(el) {
        if (isVirtualScroller(el)) {
            if (el.style.scrollbarGutter !== 'stable') {
                try { el.style.scrollbarGutter = 'stable'; } catch (e) {}
            }
            return;
        }
        // 槽位恒定：先钉住 gutter，再跨两帧「互换 overflow 取值」强制 Blink 重建/重绘滚条。
        try {
            if (el.style.scrollbarGutter !== 'stable') {
                el.style.scrollbarGutter = 'stable';
            }
        } catch (e) {
        }

        // 只在「第一次」把元素真实的（非 hidden 的）overflow 记下来作为复原基准。
        // 之后无论 repaint 被重叠触发多少次，都只恢复这个值 —— 否则会出现：
        //   repaint(A) 先把 overflowY 写成 'hidden'，还没恢复时又来一次 repaint(A)，
        //   此时读到的 el.style.overflowY 已经是 'hidden'，restore 就把它永远钉死成 'hidden'，
        //   表现就是「切几个标签后整页滚动条失效 / 滚轮滑不动」。
        if (el.__elFadePrevY === undefined) {
            el.__elFadePrevY = (el.style.overflowY && el.style.overflowY !== 'hidden') ? el.style.overflowY : '';
            el.__elFadePrevX = (el.style.overflowX && el.style.overflowX !== 'hidden') ? el.style.overflowX : '';
        }

        // 关键修复：原先是 overflow:hidden 两帧 —— 这会让滚条在两帧内「彻底消失」（sbw 变 0），
        // 进入/离开滚动容器、或在多个滚动容器间移动鼠标时就会看到滚条一闪一闪。
        // 改为 auto<->scroll 互换：两种取值在「内容溢出」时滚条始终可见，
        // 仍借 overflow 变化触发 Blink 重绘滚条（让 :hover 拇指显形），但滚条不再消失 → 消除闪烁。
        var cY = getComputedStyle(el).overflowY;
        var cX = getComputedStyle(el).overflowX;
        var swapY = (cY === 'scroll') ? 'auto' : 'scroll';
        var swapX = (cX === 'scroll') ? 'auto' : 'scroll';
        // 非滚动取值（visible/clip 等，理论上不会走到这里）兜底成 auto，避免写出非法值
        if (cY !== 'scroll' && cY !== 'auto' && cY !== 'overlay') { swapY = 'auto'; }
        if (cX !== 'scroll' && cX !== 'auto' && cX !== 'overlay') { swapX = 'auto'; }

        // 这里曾有一对「把 scrollTop/scrollLeft 钉回切换前旧值」的写回（那是 overflow:hidden 年代的遗留：
        // hidden 会让浏览器丢掉滚动位置）。auto↔scroll 两边都可滚、client 尺寸不变，位置根本不会丢
        // （浏览器实测 .demo-main / .el-scrollbar__wrap / .el-table__body-wrapper 两轴偏移都是 0），
        // 反过来却会盖掉别人在这两帧里的滚动：读到的旧值是「进入时」的，写回发生在两帧之后，
        // 中间任何一次真实滚动（滚轮、scrollIntoView、程序化赋值）都被按旧值弹回去。
        // 上面 isVirtualScroller 那段注释记的是同一类回跳，只是当时只给虚拟滚动容器开了豁免。
        el.style.overflowY = swapY;
        el.style.overflowX = swapX;

        requestAnimationFrame(function () {
            requestAnimationFrame(function () {
                el.style.overflowY = el.__elFadePrevY;
                el.style.overflowX = el.__elFadePrevX;
            });
        });
    }

    function sync(target) {
        var next = nearestScroller(target);

        if (next === last) {
            return;
        }

        // 离开：此刻 :hover 已不在它身上 ⇒ 重建后按「透明」绘制，即消失
        if (last) {
            repaint(last);
        }

        last = next;

        // 进入：此刻 :hover 已在它身上 ⇒ 重建后按 .16 绘制，即若隐若现
        if (next) {
            repaint(next);
        }
    }

    host.addEventListener('pointerover', function (e) {
        sync(e.target);
    }, true);

    host.addEventListener('pointerout', function (e) {
        sync(e.relatedTarget);
    }, true);
};

// ElTable 表头 / 表体横向滚动同步。
// 之前用 Blazor 的 @onscroll 绑定：每滚一次就走一遍事件分发 + 无条件 StateHasChanged，
// 整表（含全部行）重渲染一次 —— 行多时表现为「滚动有一段空白停滞」。
// 这里改成纯 JS 镜像 scrollLeft，滚动期间完全不进 Blazor 渲染管线。
window.elementTableSyncScroll = function (headerEl, bodyEl) {
    if (!headerEl || !bodyEl) {
        return;
    }

    var key = '__elTableScrollSync';

    if (headerEl[key] && headerEl[key].body === bodyEl) {
        return;
    }

    var lock = false;

    function mirror(from, to) {
        if (lock) {
            return;
        }

        if (from.scrollLeft === to.scrollLeft) {
            return;
        }

        lock = true;
        to.scrollLeft = from.scrollLeft;
        // 目标压根不能横滚（scrollLeft 恒 0）时立刻解锁，避免锁死
        if (to.scrollLeft !== from.scrollLeft) {
            lock = false;
            return;
        }

        window.requestAnimationFrame(function () {
            lock = false;
        });
    }

    var onHeader = function () {
        mirror(headerEl, bodyEl);
    };

    var onBody = function () {
        mirror(bodyEl, headerEl);
    };

    headerEl.addEventListener('scroll', onHeader, { passive: true });
    bodyEl.addEventListener('scroll', onBody, { passive: true });

    headerEl[key] = {
        body: bodyEl,
        dispose: function () {
            headerEl.removeEventListener('scroll', onHeader);
            bodyEl.removeEventListener('scroll', onBody);
            delete headerEl[key];
        }
    };
};

// 将指定元素滚动进可视区（ElTour scroll-into-view-options 用）。
// selector 可为 CSS 选择器或 DOM 元素；optionsJson 为 ScrollIntoViewOptions 的 JSON 字符串。
window.elementScrollIntoView = function (selector, optionsJson) {
    if (!selector) return;
    var el = typeof selector === 'string' ? document.querySelector(selector) : selector;
    if (!el || !el.scrollIntoView) return;
    var opts = { block: 'center', inline: 'center' };
    if (typeof optionsJson === 'string' && optionsJson) {
        try { opts = JSON.parse(optionsJson); } catch (e) { }
    } else if (optionsJson && typeof optionsJson === 'object') {
        opts = optionsJson;
    }
    el.scrollIntoView(opts);
};

// 设置某个元素的垂直滚动位置（ElTreeV2 ScrollTo / ScrollToNode 用）。
// 以前这里叫 elementSetScrollTop，把上面 925 行那个带 behavior 参数的同名函数覆盖了：
// 站内两个传 "smooth" 的调用（elementAnchorScrollTo 的 Duration、elementBacktopScrollTo）
// 实际都退化成 el.scrollTop = top 的瞬间跳转，平滑滚动参数静默失效。
// ElTable 那处传的是 "auto"，两种实现结果一样，所以只有它没受影响。
window.elementSetElementScrollTop = function (el, top) {
    if (!el) return;
    if (typeof el === 'string') el = document.querySelector(el);
    if (el && el.scrollTop !== undefined) el.scrollTop = top;
};

// 复制文本到剪贴板（ElTypographyTitle Copyable 用）。ES5 兼容。
window.elementCopyText = function (text) {
    try {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text);
            return true;
        }
    } catch (e) { /* 剪贴板不可用时降级 */ }
    return false;
};

// ===== ElDialogGrow: 自动聚焦 + 焦点陷阱 + 右下角缩放（对齐 Radzen AutoFocusFirstElement / EP resizable） =====
(function () {
    var FOCUSABLE = 'a[href],button:not([disabled]),input:not([disabled]):not([type="hidden"]),select:not([disabled]),textarea:not([disabled]),[tabindex]:not([tabindex="-1"])';

    /* 存活的焦点陷阱，按绑定顺序排列，末尾为最上层对话框。 */
    var trapStack = [];
    /* 单次焦点流转内只允许一个陷阱动作，防止递归拉抢。 */
    var trapBusy = false;

    function visibleFocusables(root) {
        if (!root) {
            return [];
        }
        var nodes = root.querySelectorAll(FOCUSABLE);
        var list = [];
        for (var i = 0; i < nodes.length; i++) {
            var el = nodes[i];
            /* 跳过 offsetWidth/offsetHeight 都为 0 的节点：隐藏的表单项、被折叠的内容不算可聚焦。 */
            if (el.offsetWidth > 0 || el.offsetHeight > 0 || el === document.activeElement) {
                list.push(el);
            }
        }
        return list;
    }

    /* 打开后聚焦第一个可聚焦控件；找不到返回 false（调用方退回聚焦包装层）。 */
    window.elementDialogFocusFirst = function (dialogEl) {
        var list = visibleFocusables(dialogEl);
        if (!list.length) {
            return false;
        }
        /* 显式标了 autofocus 的元素优先：确认框的主按钮按官方口径排在最后（DOM 第二位），
           但 Enter 的默认动作必须还是「确定」，所以焦点要越过它前面的次按钮。 */
        for (var j = 0; j < list.length; j++) {
            if (list[j].hasAttribute('autofocus')) {
                list[j].focus();
                return true;
            }
        }
        /* 跳过右上角的关闭按钮——它排在 DOM 前面，但用户期望焦点落在第一个业务输入项上。 */
        for (var i = 0; i < list.length; i++) {
            if (!list[i].classList.contains('el-dialog__headerbtn')) {
                list[i].focus();
                return true;
            }
        }
        list[0].focus();
        return true;
    };

    /* 焦点陷阱：Tab / Shift+Tab 在对话框内循环，鼠标点击对话框外把焦点拉回来。 */
    window.elementDialogBindFocusTrap = function (dialogEl, dotNet) {
        if (!dialogEl) {
            return;
        }
        if (dialogEl.__elFocusTrap) {
            window.elementDialogUnbindFocusTrap(dialogEl);
        }

        function onKeyDown(e) {
            if (e.key !== 'Tab') {
                return;
            }
            var list = visibleFocusables(dialogEl);
            if (!list.length) {
                e.preventDefault();
                dialogEl.focus();
                return;
            }
            var first = list[0];
            var last = list[list.length - 1];
            var active = document.activeElement;
            if (!dialogEl.contains(active)) {
                e.preventDefault();
                (e.shiftKey ? last : first).focus();
                return;
            }
            if (e.shiftKey && active === first) {
                e.preventDefault();
                last.focus();
            } else if (!e.shiftKey && active === last) {
                e.preventDefault();
                first.focus();
            }
        }

        function onFocusIn(e) {
            if (!dialogEl.isConnected) {
                window.elementDialogUnbindFocusTrap(dialogEl);
                return;
            }
            /* 只有最上层对话框参与焦点管理：下层对话框一起抢焦点会形成无限递归。 */
            if (trapStack[trapStack.length - 1] !== dialogEl) {
                return;
            }
            if (trapBusy) {
                return;
            }
            if (!dialogEl.contains(e.target)) {
                /* 焦点被外部抢走（程序 focus 到别处）时拉回第一个控件。 */
                var list = visibleFocusables(dialogEl);
                if (list.length) {
                    trapBusy = true;
                    try {
                        list[0].focus();
                    } finally {
                        trapBusy = false;
                    }
                }
            }
        }

        /* onKeyTrap 包一层：对话框已从 DOM 摘除时自动解绑，避免文档上残留全局监听。 */
        function onKeyTrap(e) {
            if (!dialogEl.isConnected) {
                window.elementDialogUnbindFocusTrap(dialogEl);
                return;
            }
            onKeyDown(e);
        }

        dialogEl.__elFocusTrap = { key: onKeyTrap, focus: onFocusIn };
        document.addEventListener('keydown', onKeyTrap, true);
        document.addEventListener('focusin', onFocusIn);
        trapStack.push(dialogEl);
    };

    window.elementDialogUnbindFocusTrap = function (dialogEl) {
        if (!dialogEl || !dialogEl.__elFocusTrap) {
            return;
        }
        document.removeEventListener('keydown', dialogEl.__elFocusTrap.key, true);
        document.removeEventListener('focusin', dialogEl.__elFocusTrap.focus);
        dialogEl.__elFocusTrap = null;
        var i = trapStack.indexOf(dialogEl);
        if (i >= 0) {
            trapStack.splice(i, 1);
        }
    };

    /* 右下角缩放手柄：mousedown 后在 document 上跟mousemove，回调 .NET 写回像素宽高。 */
    /* 对话框缩放（Resizable）：按住右下角手柄拖动 .el-dialog。
       - 拖拽过程直接改 DOM 的 width/height，保证跟手；
       - 以约 80ms 节流把尺寸回传 .NET（JsResize），松手时强制回传一次；
       - 松手若发生过位移，吞掉紧随其后的那次 click —— 与拖拽同一处理。
         **这就是「缩放到一半松手，整个对话框被关掉」的根因**：指针常拖到对话框
         之外（遮罩上）才松手，此时 mousedown(手柄) 与 mouseup(遮罩) 的最近公共祖先是
         .el-dialog__wrapper，浏览器把 click 派发到 wrapper → 命中「点击遮罩关闭」。 */
    window.elementDialogEnableResize = function (dialogEl, handleEl, dotNet) {
        if (!dialogEl || !handleEl || !dotNet) {
            return;
        }

        var state = { dragging: false, moved: false, sentAt: 0, w: 0, h: 0 };

        function measure(e) {
            var rect = dialogEl.getBoundingClientRect();
            return {
                w: Math.max(120, Math.round(e.clientX - rect.left)),
                h: Math.max(80, Math.round(e.clientY - rect.top))
            };
        }

        function send(force) {
            var now = Date.now();
            if (!force && now - state.sentAt < 80) {
                return;
            }
            state.sentAt = now;
            try {
                dotNet.invokeMethodAsync('JsResize', state.w, state.h);
            } catch (err) {
                /* 组件已销毁：忽略 */
            }
        }

        function onMove(e) {
            if (!state.dragging) {
                return;
            }
            var m = measure(e);
            /* 1px 死区：手柄上的原地抖动不算缩放，避免白吞一次 click。 */
            if (!state.moved && m.w === state.w && m.h === state.h) {
                return;
            }
            state.moved = true;
            state.w = m.w;
            state.h = m.h;
            dialogEl.style.width = m.w + 'px';
            dialogEl.style.height = m.h + 'px';
            send(false);
        }

        function swallowClick(e) {
            e.stopPropagation();
            e.preventDefault();
            document.removeEventListener('click', swallowClick, true);
        }

        function onUp() {
            if (!state.dragging) {
                return;
            }
            state.dragging = false;
            document.removeEventListener('mousemove', onMove);
            document.removeEventListener('mouseup', onUp);
            if (!state.moved) {
                return;
            }
            send(true);
            document.addEventListener('click', swallowClick, true);
            window.setTimeout(function () {
                document.removeEventListener('click', swallowClick, true);
            }, 0);
        }

        function onDown(e) {
            if (!e || e.button !== 0) {
                return;
            }
            var rect = dialogEl.getBoundingClientRect();
            state.dragging = true;
            state.moved = false;
            state.w = Math.round(rect.width);
            state.h = Math.round(rect.height);
            state.sentAt = 0;
            if (e.preventDefault) { e.preventDefault(); }
            if (e.stopPropagation) { e.stopPropagation(); }
            document.addEventListener('mousemove', onMove);
            document.addEventListener('mouseup', onUp);
        }

        handleEl.__elDialogResize = { down: onDown, move: onMove, up: onUp, swallow: swallowClick };
        handleEl.addEventListener('mousedown', onDown);
    };

    window.elementDialogDisableResize = function (handleEl) {
        var bound = handleEl && handleEl.__elDialogResize;
        if (!bound) {
            return;
        }
        handleEl.removeEventListener('mousedown', bound.down);
        /* 缩放进行中被销毁时，清掉 document 上残留的监听，避免野指针。 */
        document.removeEventListener('mousemove', bound.move);
        document.removeEventListener('mouseup', bound.up);
        document.removeEventListener('click', bound.swallow, true);
        handleEl.__elDialogResize = null;
    };

    /* 对话框拖拽（Draggable）：按住标题栏拖动 .el-dialog。
       - 拖拽过程直接写 DOM 的 transform，保证跟手（不等 .NET 往返）；
       - 以约 80ms 节流把偏移回传 .NET，仅用于**保存**（.NET 侧不因它重渲染），
         这样对话框因其它原因重渲染时，style 里带上的同一份 transform 不会让位置跳回；
       - 松手若发生过位移，吞掉紧随其后的那次 click：否则鼠标停在遮罩上松开时，
         .el-dialog__wrapper 的「点击遮罩关闭」会把这次 click 当成遮罩点击而误关对话框。 */
    window.elementDialogEnableDrag = function (dialogEl, dotNet, handleEl) {
        if (!dialogEl || !dotNet) {
            return;
        }
        handleEl = handleEl || dialogEl.querySelector('.el-dialog__header');
        if (!handleEl) {
            return;
        }

        /* 拖拽起点从 DOM 当前 transform 读，不在 JS 里留跨会话状态：
           .NET 重置位置（重新打开对话框）后这里自然读到 0，无需额外的 reset 接口。 */
        function readOffset() {
            var m = /translate\(\s*(-?[\d.]+)px\s*,\s*(-?[\d.]+)px\s*\)/.exec(dialogEl.style.transform || '');
            return m ? { x: parseFloat(m[1]), y: parseFloat(m[2]) } : { x: 0, y: 0 };
        }

        var state = { sx: 0, sy: 0, baseX: 0, baseY: 0, dx: 0, dy: 0, dragging: false, moved: false, sentAt: 0 };

        function apply() {
            dialogEl.style.transform = 'translate(' + Math.round(state.dx) + 'px,' + Math.round(state.dy) + 'px)';
        }

        function send(force) {
            var now = Date.now();
            if (!force && now - state.sentAt < 80) {
                return;
            }
            state.sentAt = now;
            try {
                dotNet.invokeMethodAsync('JsDialogDrag', Math.round(state.dx), Math.round(state.dy));
            } catch (e) {
                /* 组件已销毁：忽略 */
            }
        }

        function onMove(e) {
            if (!state.dragging) {
                return;
            }
            var rawX = e.clientX - state.sx;
            var rawY = e.clientY - state.sy;
            /* 3px 死区：标题栏上的轻微抖动不算拖拽，避免影响 header 插槽内的点击。 */
            if (!state.moved && Math.abs(rawX) + Math.abs(rawY) < 3) {
                return;
            }
            state.moved = true;
            state.dx = state.baseX + rawX;
            state.dy = state.baseY + rawY;
            apply();
            send(false);
        }

        function swallowClick(e) {
            e.stopPropagation();
            e.preventDefault();
            document.removeEventListener('click', swallowClick, true);
        }

        function onUp() {
            if (!state.dragging) {
                return;
            }
            state.dragging = false;
            document.removeEventListener('mousemove', onMove);
            document.removeEventListener('mouseup', onUp);
            if (!state.moved) {
                return;
            }
            apply();
            send(true);
            document.addEventListener('click', swallowClick, true);
            window.setTimeout(function () {
                document.removeEventListener('click', swallowClick, true);
            }, 0);
        }

        function onDown(e) {
            if (!e || e.button !== 0) {
                return;
            }
            /* 标题栏里的交互控件（关闭按钮、header 插槽里的输入框等）不参与拖拽。 */
            var t = e.target;
            if (t && t.closest && t.closest('button,a,input,select,textarea,.el-dialog__headerbtn')) {
                return;
            }
            var cur = readOffset();
            state.dragging = true;
            state.moved = false;
            state.sx = e.clientX;
            state.sy = e.clientY;
            state.baseX = cur.x;
            state.baseY = cur.y;
            state.dx = cur.x;
            state.dy = cur.y;
            if (e.preventDefault) {
                e.preventDefault();
            }
            document.addEventListener('mousemove', onMove);
            document.addEventListener('mouseup', onUp);
        }

        dialogEl.__elDialogDrag = { down: onDown, move: onMove, up: onUp, swallow: swallowClick, handle: handleEl };
        handleEl.addEventListener('mousedown', onDown);
    };

    window.elementDialogDisableDrag = function (dialogEl) {
        var bound = dialogEl && dialogEl.__elDialogDrag;
        if (!bound) {
            return;
        }
        bound.handle.removeEventListener('mousedown', bound.down);
        /* 拖拽进行中被销毁时，清掉 document 上残留的监听，避免野指针。 */
        document.removeEventListener('mousemove', bound.move);
        document.removeEventListener('mouseup', bound.up);
        document.removeEventListener('click', bound.swallow, true);
        dialogEl.__elDialogDrag = null;
    };

    /* ==== ElIpAddressGrow（IPv4 四段输入）====
       这里只做「必须看得到光标与 DOM」的那半：进格全选、keydown 拦掉非法字符与越界数字、
       「.」/方向键/满 3 位的跨格、退格到空段回退一格、粘贴整串摊进四格、输入法合成期不落字。
       值模型（过滤/截断/钳位/合成点分串）的唯一权威在 C# 侧，所以凡是本函数改过 cell.value 的地方
       一律补发 input 事件，让 Blazor 的 @oninput 走同一条路（直接改 DOM 不回写，.NET 就会拿着旧值渲染）。 */
    window.elementIpAddressGrowAttach = function (el, dotnet) {
        if (!el || el.__elIpAddressGrow) { return; }
        var cells = el.querySelectorAll('.el-ip-address-grow__cell');
        if (!cells.length) { return; }

        var max = 255;
        var state = { entries: [], prevValues: [] };
        var ipRegex = /\b((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b/;

        var focusCell = function (index) {
            if (index < 0) { index = 0; }
            if (index > cells.length - 1) { index = cells.length - 1; }
            if (cells[index]) { cells[index].focus(); } // 全选在 focus 监听里做
        };

        var write = function (cell, value) {
            cell.value = value;
            cell.dispatchEvent(new Event('input', { bubbles: true }));
        };

        var isDigitKey = function (key) { return !!key && key.length === 1 && key >= '0' && key <= '9'; };

        for (var c = 0; c < cells.length; c++) {
            (function (cell, index) {
                state.prevValues[index] = cell.value;

                var add = function (type, fn) {
                    cell.addEventListener(type, fn);
                    state.entries.push({ cell: cell, type: type, fn: fn });
                };

                add('focus', function () { cell.select(); });

                add('keydown', function (e) {
                    if (e.ctrlKey || e.metaKey || e.altKey) { return; } // 组合键放行（含 Ctrl+V）
                    if (cell.readOnly || cell.disabled) { return; }
                    var key = e.key;

                    if (isDigitKey(key)) {
                        state.prevValues[index] = cell.value;
                        if (cell.value === '0') { cell.value = ''; } // 前导 0 给真正要打的数字让位
                        var start = cell.selectionStart;
                        var end = cell.selectionEnd;
                        var next = start === end
                            ? cell.value + key
                            : cell.value.substring(0, start) + key + cell.value.substring(end);
                        if (next.length > 3 || Number(next) > max) {
                            e.preventDefault();
                            cell.value = state.prevValues[index]; // 拦下来要把刚清的 0 放回去
                        }
                        return;
                    }
                    if (key === '.') { e.preventDefault(); focusCell(index + 1); return; }
                    if (key === 'Backspace') {
                        if (cell.value.length <= 1) {
                            e.preventDefault();
                            write(cell, '0');
                            focusCell(index - 1);
                        }
                        return; // 还有多位：默认删除 + @oninput
                    }
                    if (key === 'ArrowRight' || key === ' ') {
                        if (cell.selectionStart === cell.value.length && cell.selectionEnd === cell.value.length) {
                            e.preventDefault();
                            focusCell(index + 1);
                        }
                        return;
                    }
                    if (key === 'ArrowLeft') {
                        if (cell.selectionStart === 0 && cell.selectionEnd === 0) {
                            e.preventDefault();
                            focusCell(index - 1);
                        }
                        return;
                    }
                    if (key && (key.length > 1 || key === 'Tab' || key === 'Delete' || key === 'Escape'
                        || key === 'Enter' || key === 'Home' || key === 'End')) {
                        return; // 功能键/组合键名放行
                    }
                    if (e.isComposing) { return; }
                    e.preventDefault(); // 其余可打印字符（字母、符号）不进格
                });

                add('keyup', function (e) {
                    if (isDigitKey(e.key) && cell.value.length === 3) { focusCell(index + 1); }
                });

                add('compositionstart', function () { state.prevValues[index] = cell.value; });

                add('compositionend', function () {
                    if (!/^\d*$/.test(cell.value)) { cell.value = state.prevValues[index] || '0'; }
                });

                add('paste', function (e) {
                    e.preventDefault();
                    if (cell.readOnly || cell.disabled) { return; }
                    var clip = e.clipboardData || window.clipboardData;
                    var raw = clip ? clip.getData('text') : '';
                    if (!raw) { return; }
                    var matched = raw.match(ipRegex);
                    if (!matched) { return; }
                    var parts = matched[0].split('.');
                    var args = [];
                    for (var p = 0; p < parts.length && p < cells.length; p++) {
                        var num = parseInt(parts[p], 10);
                        if (isNaN(num) || num < 0) { num = 0; }
                        if (num > max) { num = max; }
                        args.push(num);
                        cells[p].value = String(num);
                        state.prevValues[p] = String(num);
                    }
                    while (args.length < cells.length) { args.push(0); }
                    if (dotnet) {
                        // 回写是异步的：断线/组件已卸载时 reject，不接住会在控制台留一条未处理拒绝。
                        var pushed = dotnet.invokeMethodAsync('TriggerUpdate', args[0], args[1], args[2], args[3]);
                        if (pushed && pushed.catch) { pushed.catch(function () { }); }
                    }
                });
            })(cells[c], c);
        }

        el.__elIpAddressGrow = state;
    };

    window.elementIpAddressGrowDetach = function (el) {
        var state = el && el.__elIpAddressGrow;
        if (!state) { return; }
        for (var i = 0; i < state.entries.length; i++) {
            state.entries[i].cell.removeEventListener(state.entries[i].type, state.entries[i].fn);
        }
        state.entries.length = 0;
        el.__elIpAddressGrow = null;
    };
})();

// ===== ElTour 引导层（对齐 EP 2.14.0 packages/components/tour）=====
// 官方这一层由三件事构成，本 IIFE 全部接管，因为它们都依赖真实布局，bUnit / C# 侧看不见：
//   ① mask.vue：把遮罩画成一条 <path>，「视口外框 + 反向绕的目标矩形」在非零环绕规则下
//      中间才有填充 —— 洞里没有像素，所以目标既看得见又点得穿（target-area-clickable）。
//   ② helper.ts useTarget：洞的矩形不是目标矩形本身，而是四周各外扩 gap.offset（默认 6）、
//      圆角 gap.radius（默认 2）之后的 mergedPosInfo；这个矩形同时是浮层的定位基准。
//   ③ helper.ts useFloating：offset(10) + flip + shift + arrow，箭头贴面板朝向目标的那条边。
// 只做一次性定位不够：切步会把目标 scrollIntoView，目标在定位之后才移动。这里挂
// scroll / resize / 属性三类自动更新（逐帧 + 定时器兜底），面板从 DOM 移除时全部摘掉。
(function () {
    if (window.__tourAutoUpdateAttached) return;
    window.__tourAutoUpdateAttached = true;

    // 用 Set 而不是普通对象：DOM 元素作对象键会被强转成同一个字符串而互相覆盖。
    var tracked = new Set();

    var ARROW_SIZE = 10;       // 官方 tour.scss：.el-tour__arrow 是 10×10 的方块
    var ARROW_PADDING = 5;     // 官方 floating-ui arrow({ element }) 的默认 padding

    function targetElOf(panelEl) {
        var selector = panelEl.getAttribute('data-target');
        if (!selector) return null;
        try {
            return document.querySelector(selector);
        } catch (e) {
            // 选择器写错（宿主传了非法表达式）时按「没有目标」处理：遮罩不留洞、面板居中
            return null;
        }
    }

    function intAttr(panelEl, name, fallback) {
        var v = parseInt(panelEl.getAttribute(name), 10);
        return isNaN(v) ? fallback : v;
    }

    function viewportSize() {
        return {
            width: window.innerWidth || document.documentElement.clientWidth,
            height: window.innerHeight || document.documentElement.clientHeight
        };
    }

    // 官方 useTarget 的 mergedPosInfo：目标矩形四周各外扩 gap.offset，radius 取 gap.radius
    function spotRectOf(targetEl, gapOffset, gapRadius) {
        var r = targetEl.getBoundingClientRect();
        return {
            left: r.left - gapOffset,
            top: r.top - gapOffset,
            width: r.width + gapOffset * 2,
            height: r.height + gapOffset * 2,
            radius: gapRadius
        };
    }

    // 逐字符抄官方 mask.vue 的 path computed：外框四点和内框四段圆角弧的绕向必须相反，
    // 否则非零环绕规则会把内框也算进填充，洞就消失了（看着就是「遮罩一片黑、没有高亮」）。
    function hollowPathData(vw, vh, spot) {
        var outer = 'M' + vw + ',0 L0,0 L0,' + vh + ' L' + vw + ',' + vh + ' L' + vw + ',0 Z';
        if (!spot) return outer;
        var w = spot.width;
        var h = spot.height;
        var r = Math.max(0, Math.min(spot.radius, w / 2, h / 2));
        if (r === 0) {
            return outer + ' M' + spot.left + ',' + spot.top + ' h' + w + ' v' + h + ' h' + (-w) + ' z';
        }
        var arc = ' a' + r + ',' + r + ' 0 0 1 ';
        return outer
            + ' M' + (spot.left + r) + ',' + spot.top
            + ' h' + (w - r * 2) + arc + r + ',' + r
            + ' v' + (h - r * 2) + arc + (-r) + ',' + r
            + ' h' + (-w + r * 2) + arc + (-r) + ',' + (-r)
            + ' v' + (-h + r * 2) + arc + r + ',' + (-r)
            + ' z';
    }

    function hollowPathEl(panelEl) {
        var root = panelEl.parentElement;
        if (!root || !root.querySelector) return null;
        if (root.__tourHollow && root.__tourHollow.isConnected) return root.__tourHollow;
        root.__tourHollow = root.querySelector('.el-tour__hollow');
        return root.__tourHollow;
    }

    // placement 的第一段（top/bottom/left/right）：官方 content.vue 把它写进 data-side，
    // 箭头的 -5px 贴边规则全靠它（.el-tour__content[data-side^='bottom'] .el-tour__arrow{top:-5px}）
    function sideOf(placement) {
        var dash = placement.indexOf('-');
        return dash > 0 ? placement.slice(0, dash) : placement;
    }

    var OPPOSITE = { top: 'bottom', bottom: 'top', left: 'right', right: 'left' };

    // 一次算出「面板放在 side 这一侧」的左上角（EP computePosition 的 mainAxis offset）
    function coordsFor(spot, panelRect, side, gap) {
        if (side === 'top') {
            return { x: spot.left + (spot.width - panelRect.width) / 2, y: spot.top - panelRect.height - gap };
        }
        if (side === 'left') {
            return { x: spot.left - panelRect.width - gap, y: spot.top + (spot.height - panelRect.height) / 2 };
        }
        if (side === 'right') {
            return { x: spot.left + spot.width + gap, y: spot.top + (spot.height - panelRect.height) / 2 };
        }
        return { x: spot.left + (spot.width - panelRect.width) / 2, y: spot.top + spot.height + gap };
    }

    // 官方对齐方式里 *-start / *-end 的两种变体：交叉轴贴目标的起边 / 终边
    function align(spot, panelRect, placement, x, y) {
        if (/(^|-)start$/.test(placement)) {
            if (placement.indexOf('top') === 0 || placement.indexOf('bottom') === 0) x = spot.left;
            else y = spot.top;
        } else if (/(^|-)end$/.test(placement)) {
            if (placement.indexOf('top') === 0 || placement.indexOf('bottom') === 0) x = spot.left + spot.width - panelRect.width;
            else y = spot.top + spot.height - panelRect.height;
        }
        return { x: x, y: y };
    }

    function overflowOf(coords, panelRect, vw, vh) {
        return Math.max(
            Math.max(0, -coords.x),
            Math.max(0, -coords.y),
            Math.max(0, coords.x + panelRect.width - vw),
            Math.max(0, coords.y + panelRect.height - vh)
        );
    }

    function writeStyle(el, prop, value) {
        // 只在值真的变了才写：写回同值同样会触发属性观察者，无守卫就自激成死循环。
        if (el.style[prop] !== value) el.style[prop] = value;
    }

    function placeArrow(panelEl, side, spot, panelRect) {
        // 必须 :scope >：面板重渲染时 Blazor 会把 arrow 作为直接子节点复用，
        // 但后代选择器会顺着子树再抓一遍，取到 slot 内容里同名 class 的东西。
        var arrow = panelEl.querySelector(':scope > .el-tour__arrow');
        if (!arrow) return;
        if (!spot) {
            writeStyle(arrow, 'left', '');
            writeStyle(arrow, 'top', '');
            return;
        }
        var horizontal = side === 'top' || side === 'bottom';
        var center = horizontal ? spot.left + spot.width / 2 : spot.top + spot.height / 2;
        var origin = horizontal ? panelRect.left : panelRect.top;
        var extent = horizontal ? panelRect.width : panelRect.height;
        var half = ARROW_SIZE / 2;
        var lower = origin + ARROW_PADDING + half;
        var upper = origin + Math.max(lower, extent - ARROW_PADDING - half);
        var along = Math.min(Math.max(center, lower), upper) - origin - half;
        if (horizontal) {
            writeStyle(arrow, 'left', along + 'px');
            writeStyle(arrow, 'top', '');
        } else {
            writeStyle(arrow, 'top', along + 'px');
            writeStyle(arrow, 'left', '');
        }
    }

    function position(panelEl) {
        var targetEl = targetElOf(panelEl);
        var spot = null;
        var size = viewportSize();
        var pathEl = hollowPathEl(panelEl);

        if (targetEl) {
            spot = spotRectOf(targetEl, intAttr(panelEl, 'data-gap-offset', 6), intAttr(panelEl, 'data-gap-radius', 2));
        }

        // ① 遮罩的洞：每次布局都重画，切步 / 滚动 / 缩放都由同一个入口跟进
        if (pathEl) {
            var d = hollowPathData(size.width, size.height, spot);
            if (pathEl.getAttribute('d') !== d) pathEl.setAttribute('d', d);
        }

        var panelRect = panelEl.getBoundingClientRect();
        // 面板还没渲染出尺寸，让调用方下一轮再量
        if (panelRect.width === 0 || panelRect.height === 0) return false;

        // ② 没有目标：官方 content.vue 是 position:fixed + 视口居中（不是我们旧的 top:20% 兜底）
        if (!spot) {
            writeStyle(panelEl, 'position', 'fixed');
            writeStyle(panelEl, 'left', '50%');
            writeStyle(panelEl, 'top', '50%');
            writeStyle(panelEl, 'transform', 'translate3d(-50%, -50%, 0)');
            writeStyle(panelEl, 'maxWidth', '100vw');
            panelEl.removeAttribute('data-side');
            placeArrow(panelEl, 'bottom', null, panelRect);
            return true;
        }

        var placement = panelEl.getAttribute('data-placement') || 'bottom';
        var gap = intAttr(panelEl, 'data-offset', 10);
        var wanted = sideOf(placement);
        var variant = placement.indexOf('-') > 0 ? '-' + placement.split('-')[1] : '';

        function coordsAt(side) {
            var base = coordsFor(spot, panelRect, side, gap);
            return align(spot, panelRect, side + variant, base.x, base.y);
        }

        // ③ flip()：首选边放不下、对侧更宽裕就翻面（官方 middleware 里的 flip）
        var side = wanted;
        var coords = coordsAt(side);
        var overflow = overflowOf(coords, panelRect, size.width, size.height);
        if (overflow > 0) {
            var opposite = OPPOSITE[wanted];
            var oppositeCoords = coordsAt(opposite);
            if (overflowOf(oppositeCoords, panelRect, size.width, size.height) < overflow) {
                side = opposite;
                coords = oppositeCoords;
            }
        }

        var x = coords.x;
        var y = coords.y;
        // ④ shift()：翻面也放不下就把交叉轴夹进视口（官方 middleware 里的 shift）
        if (x < 0) x = 0;
        if (y < 0) y = 0;
        if (x + panelRect.width > size.width) x = size.width - panelRect.width;
        if (y + panelRect.height > size.height) y = size.height - panelRect.height;

        writeStyle(panelEl, 'position', 'fixed');
        writeStyle(panelEl, 'left', x + 'px');
        writeStyle(panelEl, 'top', y + 'px');
        // CSS 里 .el-tour__content 用 left:50% + translateX(-50%) 做默认居中，
        // 不清掉就会把这里算出的 left 再往左平移半个面板宽。
        writeStyle(panelEl, 'transform', 'none');
        // ⑤ overflow 中间件：官方只在面板自身宽度装不进视口时才收缩 maxWidth
        var maxWidth = panelRect.width > size.width - 16 ? (size.width - 16) + 'px' : '';
        writeStyle(panelEl, 'maxWidth', maxWidth);

        if (panelEl.getAttribute('data-side') !== side) panelEl.setAttribute('data-side', side);
        placeArrow(panelEl, side, spot, panelRect);
        return true;
    }

    function focusPanel(panelEl) {
        // 官方 content.vue 是 focus-trap（loop + trapped），键盘事件因此能到达面板；
        // 本库不做焦点圈定（不拦 Tab 逃逸），但至少把焦点交给面板，ESC / 方向键才有效。
        var active = document.activeElement;
        if (active === panelEl || (panelEl.contains && active && panelEl.contains(active))) return;
        try {
            panelEl.focus({ preventScroll: true });
        } catch (e) {
            panelEl.focus();
        }
    }

    function start(panelEl) {
        if (tracked.has(panelEl)) return;
        tracked.add(panelEl);
        focusPanel(panelEl);

        var tick = function () {
            // 面板被 Blazor 摘掉（关引导、切宿主）时自停，不留野循环
            if (!document.body.contains(panelEl)) { stop(panelEl); return false; }
            position(panelEl);
            return true;
        };

        // EP 的 autoUpdate 是按帧观察布局的，只挂 scroll/resize 收不到「纯重排」：
        // 例如切语言让整页重新渲染、目标被兄弟节点顶开几百像素，面板若不等帧跟进就留在旧坐标。
        // 每帧只做两次 getBoundingClientRect，且只在值真的变化时写样式（见 position），成本可忽略。
        // 顺带取代了旧的重试循环：量不到尺寸时下一帧自然再试。
        (function follow() {
            if (!tick()) return;
            panelEl.__tourRaf = requestAnimationFrame(follow);
        })();

        // 但 rAF 不够：标签页隐藏或被遮挡时 Chrome 一帧都不发（实测 visibilityState=hidden 时
        // 1 秒内 0 帧），面板会冻在旧坐标；后台标签、无头渲染、被省电策略节流的地方全都是。
        // 兜一层定时器，让「跟进」不依赖渲染帧，100ms 一次同样只在值变时写。
        panelEl.__tourTimer = window.setInterval(tick, 100);
    }

    function stop(panelEl) {
        if (!tracked.has(panelEl)) return;
        tracked.delete(panelEl);
        if (panelEl.__tourRaf) cancelAnimationFrame(panelEl.__tourRaf);
        delete panelEl.__tourRaf;
        if (panelEl.__tourTimer) window.clearInterval(panelEl.__tourTimer);
        delete panelEl.__tourTimer;
    }

    function stopTree(rootEl) {
        if (rootEl.classList && rootEl.classList.contains('el-tour__content')) stop(rootEl);
        if (rootEl.querySelectorAll) {
            rootEl.querySelectorAll('.el-tour__content').forEach(stop);
        }
    }

    new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            if (mutation.type === 'attributes') {
                var panelEl = mutation.target;
                // 切步复用同一个面板元素、只换 data-target / data-placement / data-offset / gap
                if (panelEl.nodeType === 1 && panelEl.classList && panelEl.classList.contains('el-tour__content')) {
                    start(panelEl);
                    position(panelEl);
                }
                return;
            }
            mutation.addedNodes.forEach(function (node) {
                if (node.nodeType !== 1) return;
                if (node.classList && node.classList.contains('el-tour__content')) start(node);
                if (node.querySelectorAll) {
                    node.querySelectorAll('.el-tour__content').forEach(start);
                }
            });
            mutation.removedNodes.forEach(function (node) {
                if (node.nodeType !== 1) return;
                stopTree(node);
            });
        });
    }).observe(document.body, {
        childList: true,
        subtree: true,
        attributes: true,
        attributeFilter: ['data-target', 'data-placement', 'data-offset', 'data-gap-offset', 'data-gap-radius']
    });

    // ===== 键盘：对齐官方「ESC 关、左右方向键切步」，且只认这三个键 =====
    // 官方分两处：ESC 由 content.vue 的 focus-trap 发出 release-requested，左右键由 step.vue
    // 的 window keydown 处理。Blazor 的 @onkeydown 只在事件冒泡经过面板时才收到，
    // 所以这里在 document 上补一个转发器：焦点不在面板里（例如刚点过目标元素）时，
    // 把这三个键合成一次 keydown 派发到面板上，交给 Blazor 的事件处理。
    document.addEventListener('keydown', function (e) {
        // 只处理真实按键：自己派发的那次必须跳过，否则一次按键会被算两遍（连点两次才生效的老坑）
        if (!e.isTrusted) return;
        if (e.key !== 'Escape' && e.key !== 'ArrowLeft' && e.key !== 'ArrowRight') return;
        var panel = document.querySelector('.el-tour__content');
        if (!panel) return;
        var t = e.target;
        // 官方 step.vue：目标可编辑时整段跳过；本库再把输入控件一并排除，
        // 否则在镂空里的输入框敲方向键会被引导抢走。
        if (t && (t.isContentEditable || (t.closest && t.closest('input,textarea,select')))) return;
        if (e.key !== 'Escape') {
            // 官方对左右键调了 preventDefault，避免顺手把页面滚走
            e.preventDefault();
        }
        if (panel.contains(t)) return;
        panel.dispatchEvent(new KeyboardEvent('keydown', { key: e.key, code: e.code, bubbles: true, cancelable: true }));
    });
})();
// ===== 全屏：浏览器原生 Fullscreen API，供 FullScreenService（elementFullscreen*）使用 =====
// requestFullscreen 必须由用户手势触发；Promise 被拒绝（权限、非手势、目标已移除）时统一
// 回落成「当前是否全屏」的布尔值，让 C# 侧拿到 false 自己提示，而不是抛 JS 异常。
window.elementIsFullscreen = function () {
    return !!(document.fullscreenElement || document.webkitFullscreenElement
        || document.mozFullScreenElement || document.msFullscreenElement);
};

window.elementFullscreenEnter = function (target) {
    var el = target || document.documentElement;
    var request = el.requestFullscreen || el.webkitRequestFullscreen
        || el.mozRequestFullScreen || el.msRequestFullscreen;
    if (!request) {
        return window.elementIsFullscreen();
    }
    var result = request.call(el);
    if (result && typeof result.then === 'function') {
        return result.then(function () {
            return window.elementIsFullscreen();
        })['catch'](function () {
            return window.elementIsFullscreen();
        });
    }
    return window.elementIsFullscreen();
};

window.elementFullscreenExit = function () {
    var exit = document.exitFullscreen || document.webkitExitFullscreen
        || document.mozCancelFullScreen || document.msExitFullscreen;
    if (!exit) {
        return window.elementIsFullscreen();
    }
    var result = exit.call(document);
    if (result && typeof result.then === 'function') {
        return result.then(function () {
            return window.elementIsFullscreen();
        })['catch'](function () {
            return window.elementIsFullscreen();
        });
    }
    return window.elementIsFullscreen();
};

window.elementFullscreenToggle = function (target) {
    if (window.elementIsFullscreen()) {
        return window.elementFullscreenExit();
    }
    return window.elementFullscreenEnter(target);
};

// ElTable 列宽拖拽（本库扩展，消费方批次110uu）。
// 手柄 span 由 razor 渲染在可拖列的 th 里（.el-table__column-resize-handle），本函数只做两件事：
//   1) 按手柄幂等绑定 mousedown（__elResizeBound），Blazor 重渲染重复调用无副作用；
//   2) 拖拽全程纯 JS 改「表头 + 表体」两张 table 的同名 <col width> 与表宽 style，
//      不进 Blazor 渲染管线 —— 与 elementTableSyncScroll 同一条纪律。
// 松手才 invokeMethodAsync('JsTableColumnResizeEnd', colIndex, w, oldW)：C# 把覆盖宽记进
// TableHeader.ResizeWidthOverride 并重渲染（渲染树从此携带同一个值，排序/刷新不会打回去）。
// .cell 的内联宽（ShowOverflowTooltip 档）不在这里追改：落定后 Blazor 重渲染会按
// ResolveColumnWidth-1 重新下发，拖拽中那几百毫秒的旧宽只是过渡态。
window.elementTableColumnResizeInit = function (headerEl, bodyEl, dotNetRef) {
    if (!headerEl || !bodyEl || !dotNetRef) {
        return;
    }

    var handles = headerEl.querySelectorAll('th .el-table__column-resize-handle');
    for (var k = 0; k < handles.length; k++) {
        bindHandle(handles[k]);
    }

    function bindHandle(handle) {
        if (handle.__elResizeBound) {
            return;
        }
        handle.__elResizeBound = true;
        handle.addEventListener('mousedown', onMouseDown);
    }

    function onMouseDown(e) {
        if (e.button !== 0) {
            return;
        }
        var th = this.closest ? this.closest('th') : null;
        if (!th) {
            return;
        }
        var m = /el-table_1_column_(\d+)/.exec(th.className || '');
        if (!m) {
            return;
        }
        var colIndex = parseInt(m[1], 10);
        // 0 是选择列：宽度由 SelectionColumnWidth 钉死；razor 也不会在它上面渲染手柄，双保险
        if (!colIndex) {
            return;
        }
        var colName = 'el-table_1_column_' + colIndex;
        var hCol = headerEl.querySelector('col[name="' + colName + '"]');
        var bCol = bodyEl.querySelector('col[name="' + colName + '"]');
        if (!hCol || !bCol) {
            return;
        }
        var oldW = parseInt(hCol.getAttribute('width'), 10);
        if (isNaN(oldW) || oldW <= 0) {
            // auto 列（<col> 不带 width）按渲染实宽起步，拖完落到显式 width
            oldW = Math.round(th.getBoundingClientRect().width);
        }
        var startX = e.clientX;
        var lastW = oldW;
        var moved = false;
        e.preventDefault();

        var prevUserSelect = document.body.style.userSelect;
        var prevCursor = document.body.style.cursor;
        document.body.style.userSelect = 'none';
        document.body.style.cursor = 'col-resize';

        function applyTotals() {
            var tables = [
                headerEl.querySelector('table.el-table__header'),
                bodyEl.querySelector('table.el-table__body')
            ];
            for (var t = 0; t < tables.length; t++) {
                var tb = tables[t];
                if (!tb) {
                    continue;
                }
                var sum = 0;
                var cols = tb.querySelectorAll('colgroup > col');
                for (var c = 0; c < cols.length; c++) {
                    var w = parseInt(cols[c].getAttribute('width'), 10);
                    if (!isNaN(w)) {
                        sum += w;
                    }
                }
                // 表宽 style 是 Blazor 的（TableWidthStyle 的 max() 求和）：
                // 拖拽中同步抬它，否则加宽的那列会临时顶出横向滚动条、收窄则富余被均摊
                var st = tb.style;
                if ((st.width || '').indexOf('max(') >= 0) {
                    st.width = 'max(100%,' + sum + 'px)';
                } else if ((st.minWidth || '').indexOf('max(') >= 0) {
                    st.minWidth = 'max(100%,' + sum + 'px)';
                } else if (st.width) {
                    st.width = 'max(100%,' + sum + 'px)';
                } else if (st.minWidth) {
                    st.minWidth = 'max(100%,' + sum + 'px)';
                }
            }
        }

        function onMove(ev) {
            var w = Math.max(24, Math.round(oldW + ev.clientX - startX));
            if (w === lastW) {
                return;
            }
            lastW = w;
            moved = true;
            hCol.setAttribute('width', w);
            bCol.setAttribute('width', w);
            applyTotals();
        }

        function onUp() {
            document.removeEventListener('mousemove', onMove, true);
            document.removeEventListener('mouseup', onUp, true);
            document.body.style.userSelect = prevUserSelect;
            document.body.style.cursor = prevCursor;
            if (!moved) {
                // 点了一下没拖：吞掉随后的 click，别让表头点击（排序/筛选）替用户触发
                th.addEventListener('click', function (ev) {
                    ev.stopPropagation();
                    ev.preventDefault();
                }, { capture: true, once: true });
            }
            try {
                dotNetRef.invokeMethodAsync('JsTableColumnResizeEnd', colIndex, lastW, oldW);
            } catch (err) {
                // .NET 侧已销毁（页面切走）：拖拽成果留在 DOM 上，回调没有去处
            }
        }

        document.addEventListener('mousemove', onMove, true);
        document.addEventListener('mouseup', onUp, true);
    }
};
