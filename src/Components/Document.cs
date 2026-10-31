using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    internal class Document
    {
        private readonly IJSRuntime jSRuntime;

        public Document(IJSRuntime jSRuntime)
        {
            this.jSRuntime = jSRuntime;
        }

        public async Task AppendAsync(ElementReference content)
        {
            await jSRuntime.InvokeAsync<object>("documentAppendChild", content);
        }

        public async Task DisableXScrollAsync()
        {
            await jSRuntime.InvokeAsync<object>("disableXScroll");
        }

        public async Task DisableYScrollAsync()
        {
            await jSRuntime.InvokeAsync<object>("disableYScroll");
        }
        public async Task EnableYScrollAsync()
        {
            await jSRuntime.InvokeAsync<object>("enableYScroll");
        }

        public async Task DisableScrollAsync()
        {
            await DisableXScrollAsync();
            await DisableYScrollAsync();
        }

        public async Task RemoveAsync(ElementReference content)
        {
            await jSRuntime.InvokeAsync<object>("remove", content);
        }

        public async Task<int> GetClientWidthAsync()
        {
            return await jSRuntime.InvokeAsync<int>("getClientWidth");
        }
        public async Task<int> GetClientHeightAsync()
        {
            return await jSRuntime.InvokeAsync<int>("getClientHeight");
        }

        // 粘贴上传的实例登记表：句柄 → DotNetObjectReference。
        // 旧写法是每次注册 Create 一个引用却从不 Dispose（引用会一直挂在 JS 侧，页面反复开对话框就反复漏），
        // 注销又是整条 document 监听一起摘（同页后一个 ElUpload 的 Dispose 把前一个也停了）。
        // 现在按句柄配对注册/注销，最后一个实例走掉时才摘监听（摘监听在 JS 侧做）。
        private readonly Dictionary<int, DotNetObjectReference<ElUpload>> _pasteUploadRefs = new Dictionary<int, DotNetObjectReference<ElUpload>>();

        internal async ValueTask RegisterPasteUploadAsync(ElUpload upload, ElementReference element, int handle)
        {
            var reference = DotNetObjectReference.Create(upload);
            _pasteUploadRefs[handle] = reference;
            await jSRuntime.InvokeVoidAsync("registerPasteUpload", reference, element, handle);
        }

        internal async ValueTask UnRegisterPasteUploadAsync(int handle)
        {
            await jSRuntime.InvokeVoidAsync("unRegisterPasteUpload", handle);
            if (_pasteUploadRefs.TryGetValue(handle, out var reference))
            {
                _pasteUploadRefs.Remove(handle);
                reference.Dispose();
            }
        }
    }
}
