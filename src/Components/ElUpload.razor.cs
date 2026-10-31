using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ElementPlus.Lang;

namespace ElementPlus
{
    public partial class ElUpload : ElementFieldComponentBase<IFileModel[]>
    {
        [Inject]
        internal Document Document { get; set; }

        [Inject]
        private ILang Lang { get; set; }

        internal ElementReference hdnField;
        internal ElementReference Input { get; set; }
        /// <summary>
        /// 当前文件列表（已保存顺序）。元素类型为 <see cref="UploadModel"/>，
        /// 状态机为 UnStart → Uploading → Success/Fail。
        /// </summary>
        public IReadOnlyCollection<IFileModel> Files => _files;

        public Task ClearFilesAsync()
        {
            _files.Clear();
            StateHasChanged();
            return Task.CompletedTask;
        }
        internal HashSet<IFileModel> _files = new HashSet<IFileModel>();

        private bool eventRegistered;

        // 粘贴登记的实例句柄：JS 侧一条 document 监听服务多个实例，注销要按句柄摘自己那一条，
        // 不能整条监听一起摘（否则同页后销毁的那个上传组件会把还在页上的那个也停掉）。
        private static int pasteHandleSeed;
        private int _pasteHandle;

        [Parameter]
        public bool EnablePasteUpload { get; set; } = true;

        [Parameter]
        public string Url { get; set; }

        [Parameter]
        public string Action
        {
            get => Url;
            set => Url = value;
        }

        [Parameter]
        public string Method { get; set; } = "post";

        [Parameter]
        public RenderFragment Tip { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public RenderFragment<UploadModel> FileTemplate { get; set; }

        [Parameter]
        public EventCallback<IFileModel> OnDeleteFile { get; set; }

        [Parameter]
        public EventCallback<IFileModel> OnPreview { get; set; }

        [Parameter]
        public string[] AllowExtensions { get; set; } = Array.Empty<string>();

        [Parameter]
        public string Accept { get; set; }

        [Parameter]
        public string CrossOrigin { get; set; }

        [Parameter]
        public long MaxSize { get; set; }

        [Parameter]
        public float Width { get; set; }

        [Parameter]
        public float Height { get; set; }

        [Parameter]
        public EventCallback<IFileModel> OnFileUploadSuccess { get; set; }

        [Parameter]
        public EventCallback<UploadRequestEventArgs> OnSuccess { get; set; }

        [Parameter]
        public EventCallback<IFileModel> OnFileUploadFailure { get; set; }

        [Parameter]
        public EventCallback<UploadRequestEventArgs> OnError { get; set; }

        [Parameter]
        public EventCallback<IFileModel[]> OnFileListUpload { get; set; }

        [Parameter]
        public EventCallback<IFileModel> OnFileUploadStart { get; set; }

        [Parameter]
        public EventCallback<UploadProgressEventArgs> OnProgress { get; set; }

        [Parameter]
        public EventCallback<UploadChangeEventArgs> OnChange { get; set; }

        [Parameter]
        public EventCallback<UploadExceedEventArgs> OnExceed { get; set; }

        [Parameter]
        public Func<UploadModel, Task<bool>> BeforeUpload { get; set; }

        [Parameter]
        public Func<IFileModel, Task<bool>> BeforeRemove { get; set; }

        [Parameter]
        public Func<UploadRequestContext, Task<UploadRequestResult>> HttpRequest { get; set; }

        /// <summary>把上传接口的原始响应体（<see cref="UploadRequestResult.RawResponse"/>，JSON 文本）
        /// 映射成 <see cref="UploadRequestResult"/>，用于响应键不是 EP 那一族
        /// （<c>code/message/id/url</c>）的宿主，例如返回 <c>{ result:"0", message, fcode, u_imgurl }</c> 的后端。
        /// 不传则走内置映射：HTTP 2xx 且 JSON 里有 <c>code</c> 才算成功，缺 <c>code</c> 判失败（旧实现在这里
        /// 会对 undefined 调 toString 抛异常，Promise 永不 resolve，界面表现为一直转圈）。</summary>
        [Parameter]
        public Func<string, UploadRequestResult> ResponseParser { get; set; }

        [Parameter]
        public UploadType UploadType { get; set; }

        [Parameter]
        public UploadListType ListType { get; set; } = UploadListType.Text;

        [Parameter]
        public bool Drag { get; set; }

        [Parameter]
        public int Limit { get; set; }

        [Parameter]
        public bool Multiple { get; set; } = true;

        [Parameter]
        public bool AutoUpload { get; set; } = true;

        [Parameter]
        public bool ShowFileList { get; set; } = true;

        /// <summary>删除图标的可读名称（EP 的 <c>aria-label="t('el.upload.delete')"</c>）。
        /// 优先级：<b>本参数 &gt; 词条 <c>el.upload.delete</c> &gt; 默认 <c>Delete</c></b>。</summary>
        [Parameter]
        public string DeleteAriaLabel { get; set; }

        /// <summary>文件项上「按 Delete 键可删除」提示文字（EP 的 <c>.el-icon-close-tip</c> 内容）。
        /// 优先级：<b>本参数 &gt; 词条 <c>el.upload.deleteTip</c> &gt; 默认 <c>Press Delete to remove</c></b>。</summary>
        [Parameter]
        public string DeleteTipText { get; set; }

        // 触发按钮与拖拽区的文案在 EP 里不是词条（upload-content.vue / upload.vue 没有任何 t() 调用），
        // 属宿主插槽内容；本库只把 EP 真正取词的两处接上：删除图标 aria-label 与 close-tip 提示。
        private const string DeleteAriaLabelKey = "el.upload.delete";
        private const string DeleteTipTextKey = "el.upload.deleteTip";

        private string EffectiveDeleteAriaLabel => Lang.Text(DeleteAriaLabelKey, DeleteAriaLabel, "Delete");

        private string EffectiveDeleteTipText => Lang.Text(DeleteTipTextKey, DeleteTipText, "Press Delete to remove");

        /// <summary>自定义触发区域 CSS class</summary>
        [Parameter]
        public string UploadClass { get; set; }

        /// <summary>拖拽区域 CSS class</summary>
        [Parameter]
        public string DragClass { get; set; }

        /// <summary>Tip 文字 CSS class</summary>
        [Parameter]
        public string TipClass { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        [Parameter]
        public string AriaDescribedBy { get; set; }

        [Parameter]
        public bool IsDisabled { get; set; }

        [Parameter]
        public bool Disabled
        {
            get => IsDisabled;
            set => IsDisabled = value;
        }

        [Parameter]
        public string FileFieldName { get; set; } = "fileContent";

        [Parameter]
        public bool WithCredentials { get; set; } = true;

        [Parameter]
        public IDictionary<string, string> Headers { get; set; }

        [Parameter]
        public IDictionary<string, string> Data { get; set; }

        protected UploadListType EffectiveListType =>
            ListType == UploadListType.Text && UploadType == UploadType.Image
                ? UploadListType.Picture
                : ListType;

        protected bool UploadDisabled => IsDisabled || (Limit > 0 && _files.Count >= Limit) || (FormItem?.Form?.Disabled ?? false);

        protected string UploadDisabledText => UploadDisabled ? "true" : "false";

        protected string HiddenValue => string.Join(",", _files.Select(x => x.Id).Where(x => !string.IsNullOrWhiteSpace(x)));

        protected string ListTypeName => EffectiveListType switch
        {
            UploadListType.Picture => "picture",
            UploadListType.PictureCard => "picture-card",
            _ => "text"
        };

        protected string UploadTriggerClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-upload")
            .AddIf(EffectiveListType == UploadListType.PictureCard, "el-upload--picture-card")
            .AddIf(EffectiveListType != UploadListType.PictureCard, Drag ? "el-upload--drag" : "el-upload--text")
            .AddIf(UploadDisabled, "is-disabled")
            .Add(Cls)
            .AddIf(!string.IsNullOrEmpty(UploadClass), UploadClass)
            .ToString();

        protected override void FormItem_OnReset(object value, bool requireRerender)
        {
            RequireRender = true;
            if (value == null)
            {
                _files = new HashSet<IFileModel>();
            }
            else
            {
                InitilizeFiles(value as IFileModel[]);
            }
            StateHasChanged();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            // 这里刻意不校验 Url：见 UploadFilesAsync 里的说明，Url 可以是运行期现算的。
        }

        internal async Task DeleteFile(IFileModel file)
        {
            if (BeforeRemove != null && !await BeforeRemove(file))
            {
                return;
            }

            _files.Remove(file);
            RequireRender = true;
            SetFieldValue(_files.ToArray(), true);
            if (OnDeleteFile.HasDelegate)
            {
                await OnDeleteFile.InvokeAsync(file);
            }
            await NotifyChangeAsync(file);
        }

        internal async Task SelectFileAsync(MouseEventArgs args)
        {
            await Input.Dom(JSRuntime).ClickAsync();
        }

        /// <summary>把 JS 侧扫出的文件建成本地模型，并按扩展名/尺寸/数量上限过滤。
        /// <c>AlignedIds</c> 与入参 <c>files</c> 同长同序，被挡掉的那一格是空串：粘贴上传要靠下标把
        /// 上传结果送回对应那一行，返回「已接受」的紧凑数组会让结果挂到错的那一行（旧实现的形状）。</summary>
        private async Task<(List<UploadModel> Accepted, string[] AlignedIds)> ScanFilesAsync(string[][] files)
        {
            var accepted = new List<UploadModel>();
            if (files == null || files.Length == 0)
            {
                return (accepted, Array.Empty<string>());
            }

            var candidates = files.Select(CreateUploadModel).ToList();
            if (Limit > 0 && _files.Count + candidates.Count > Limit)
            {
                await NotifyExceedAsync(candidates);
                return (accepted, Enumerable.Repeat(string.Empty, candidates.Count).ToArray());
            }

            var aligned = new List<string>(candidates.Count);
            foreach (var file in candidates)
            {
                if (!ValidateFile(file))
                {
                    break;
                }
                if (BeforeUpload != null && !await BeforeUpload(file))
                {
                    aligned.Add(string.Empty);
                    continue;
                }

                _files.Add(file);
                accepted.Add(file);
                aligned.Add(file.Id);
                await NotifyChangeAsync(file);
            }

            // ValidateFile 没过就整批停（迁移前口径），剩下的格子补空串把长度对齐
            while (aligned.Count < candidates.Count)
            {
                aligned.Add(string.Empty);
            }

            return (accepted, aligned.ToArray());
        }

        private UploadModel CreateUploadModel(string[] item)
        {
            var model = new UploadModel
            {
                FileName = Path.GetFileName(item[0]),
                Status = UploadStatus.UnStart,
                Url = item.Length == 5 ? item[4] : string.Empty
            };

            if (item.Length > 1 && long.TryParse(item[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var size))
            {
                model.Size = size;
            }
            if (item.Length > 2 && int.TryParse(item[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var width))
            {
                model.Width = width;
            }
            if (item.Length > 3 && int.TryParse(item[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var height))
            {
                model.Height = height;
            }

            return model;
        }

        private bool ValidateFile(UploadModel file)
        {
            var ext = Path.GetExtension(file.FileName);
            if (AllowExtensions.Any() && !AllowExtensions.Contains(ext, StringComparer.CurrentCultureIgnoreCase))
            {
                Alert("Selected files include a file extension that is not allowed.");
                return false;
            }

            if (file.Size / 1000 > MaxSize && MaxSize > 0)
            {
                Alert("Selected files include a file larger than the allowed size.");
                return false;
            }

            if ((file.Width > Width && Width > 0) || (file.Height > Height && Height > 0))
            {
                Alert("Selected files include an image larger than the allowed dimensions.");
                return false;
            }

            return true;
        }

        internal async Task ScanFileAsync()
        {
            var input = Input.Dom(JSRuntime);
            var files = await input.ScanFilesAsync();
            var acceptedFiles = (await ScanFilesAsync(files)).Accepted;
            if (AutoUpload)
            {
                await UploadFilesAsync(input, acceptedFiles);
            }
            else
            {
                RequireRender = true;
                SetFieldValue(_files.ToArray(), true);
                StateHasChanged();
            }
        }

        public async Task SubmitAsync()
        {
            await UploadFilesAsync(Input.Dom(JSRuntime), _files.Cast<UploadModel>().Where(x => x.Status == UploadStatus.UnStart).ToList());
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            AllowExtensions = AllowExtensions?.Select(x => x?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray() ?? Array.Empty<string>();
            if (string.IsNullOrWhiteSpace(Accept) && AllowExtensions.Any())
            {
                Accept = string.Join(",", AllowExtensions);
            }
            if (FormItem == null || FormItem.OriginValueHasRendered)
            {
                return;
            }

            FormItem.OriginValueHasRendered = true;
            if (FormItem.Form?.Values.Any() ?? false)
            {
                InitilizeFiles(FormItem.OriginValue as IFileModel[]);
            }
        }

        private void InitilizeFiles(IFileModel[] fileModels)
        {
            _files = new HashSet<IFileModel>();
            if (fileModels == null)
            {
                return;
            }

            foreach (var item in fileModels)
            {
                _files.Add(new UploadModel
                {
                    Url = item.Url,
                    FileName = item.FileName,
                    Id = item.Id,
                    Status = UploadStatus.Success
                });
            }
        }

        private async Task UploadFilesAsync(ElementHelper input, IEnumerable<UploadModel> files)
        {
            var pending = files.Where(x => x.Status == UploadStatus.UnStart).ToList();
            // 「必须有上传地址」这条校验放在真正要发请求的时候，不放 OnInitialized：
            // Url 允许在 OnChange 里现问后端拿（带签名的上传地址），拿到后再 SubmitAsync，
            // 这类用法在初始化那一刻本来就没有 Url，若在 OnInitialized 抛会把整页渲染打死。
            // 空列表不校验：SubmitAsync() 在没有待传文件时是合法调用。
            if (pending.Count > 0 && string.IsNullOrWhiteSpace(Url) && HttpRequest == null)
            {
                throw new ElementException("Upload action is required.");
            }

            foreach (var model in pending)
            {
                model.Status = UploadStatus.Uploading;
                await NotifyProgressAsync(model, 0);
                if (OnFileUploadStart.HasDelegate)
                {
                    await OnFileUploadStart.InvokeAsync(model);
                }

                UploadRequestResult result;
                if (HttpRequest != null)
                {
                    result = await HttpRequest(CreateRequestContext(model, input));
                }
                else
                {
                    result = MapJsResult(await input.UploadFileAsync(
                        model.FileName,
                        Url,
                        Method,
                        FileFieldName,
                        WithCredentials,
                        Headers,
                        Data));
                }

                await FileUploadedAsync(model, result);
            }

            await input.ClearAsync();
            RequireRender = true;
            if (OnFileListUpload.HasDelegate)
            {
                await OnFileListUpload.InvokeAsync(_files.ToArray());
            }
            StateHasChanged();
        }

        private UploadRequestContext CreateRequestContext(UploadModel file, ElementHelper input)
        {
            return new UploadRequestContext
            {
                File = file,
                FileList = _files.ToArray(),
                Url = Url,
                Method = Method,
                FileFieldName = FileFieldName,
                WithCredentials = WithCredentials,
                Headers = Headers,
                Data = Data,
                Input = input,
                ReportProgress = value => NotifyProgressAsync(file, value)
            };
        }

        private async Task FileUploadedAsync(UploadModel model, UploadRequestResult result)
        {
            if (model == null)
            {
                return;
            }

            result ??= UploadRequestResult.Failure("Upload failed.");
            model.Message = result.Message;
            model.Id = string.IsNullOrWhiteSpace(result.Id) ? model.Id : result.Id;
            model.Url = string.IsNullOrWhiteSpace(result.Url) ? model.Url : result.Url;
            model.Status = result.Succeeded ? UploadStatus.Success : UploadStatus.Failure;

            await NotifyProgressAsync(model, result.Succeeded ? 100 : 0);
            var requestArgs = new UploadRequestEventArgs
            {
                File = model,
                FileList = _files.ToArray(),
                Response = result
            };

            if (result.Succeeded)
            {
                if (OnFileUploadSuccess.HasDelegate)
                {
                    await OnFileUploadSuccess.InvokeAsync(model);
                }
                if (OnSuccess.HasDelegate)
                {
                    await OnSuccess.InvokeAsync(requestArgs);
                }
            }
            else
            {
                if (OnFileUploadFailure.HasDelegate)
                {
                    await OnFileUploadFailure.InvokeAsync(model);
                }
                if (OnError.HasDelegate)
                {
                    await OnError.InvokeAsync(requestArgs);
                }
            }

            await NotifyChangeAsync(model);
        }

        private async Task NotifyProgressAsync(IFileModel file, double percent)
        {
            if (!OnProgress.HasDelegate)
            {
                return;
            }

            await OnProgress.InvokeAsync(new UploadProgressEventArgs
            {
                File = file,
                FileList = _files.ToArray(),
                Percent = percent
            });
        }

        private async Task NotifyChangeAsync(IFileModel file)
        {
            SetFieldValue(_files.ToArray(), true);
            if (!OnChange.HasDelegate)
            {
                return;
            }

            await OnChange.InvokeAsync(new UploadChangeEventArgs
            {
                File = file,
                FileList = _files.ToArray()
            });
        }

        private async Task NotifyExceedAsync(IEnumerable<UploadModel> files)
        {
            if (OnExceed.HasDelegate)
            {
                await OnExceed.InvokeAsync(new UploadExceedEventArgs
                {
                    Files = files.ToArray(),
                    FileList = _files.ToArray()
                });
                return;
            }

            Alert("The selected files exceed the upload limit.");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (EnablePasteUpload && !eventRegistered)
            {
                eventRegistered = true;
                _pasteHandle = ++pasteHandleSeed;
                await Document.RegisterPasteUploadAsync(this, Input, _pasteHandle);
            }
        }

        [JSInvokable("previewFiles")]
        public async Task<string[]> PasteUploadFiles(string[][] files)
        {
            var alignedIds = (await ScanFilesAsync(files)).AlignedIds;
            RequireRender = true;
            StateHasChanged();
            return alignedIds;
        }

        [JSInvokable("fileUploaded")]
        public async Task FileUploadedFromJsAsync(string[] file, string id)
        {
            await FileUploadedAsync(_files.OfType<UploadModel>().FirstOrDefault(x => x.Id == id), MapJsResult(file));
            RequireRender = true;
            StateHasChanged();
        }

        [JSInvokable("filesUploaded")]
        public async Task FilesUploaded()
        {
            SetFieldValue(_files.ToArray(), true);
            RequireRender = true;
            if (OnFileListUpload.HasDelegate)
            {
                await OnFileListUpload.InvokeAsync(_files.ToArray());
            }
            else
            {
                StateHasChanged();
            }
        }

        protected string GetFileItemClass(UploadModel file)
        {
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-upload-list__item")
                .AddIf(file.Status == UploadStatus.Success, "is-success")
                .AddIf(file.Status == UploadStatus.Uploading, "is-uploading")
                .AddIf(file.Status == UploadStatus.Failure, "is-fail")
                .ToString();
        }

        protected void BuildTextOrPictureItem(RenderTreeBuilder builder, UploadModel file, ref int seq)
        {
            if (EffectiveListType == UploadListType.Picture)
            {
                builder.OpenElement(seq++, "img");
                builder.AddAttribute(seq++, "src", file.Url);
                builder.AddAttribute(seq++, "alt", string.Empty);
                builder.AddAttribute(seq++, "class", "el-upload-list__item-thumbnail");
                builder.CloseElement();
            }

            builder.OpenElement(seq++, "a");
            builder.AddAttribute(seq++, "class", "el-upload-list__item-name");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, _ => PreviewFileAsync(file)));
            builder.OpenElement(seq++, "i");
            builder.AddAttribute(seq++, "class", "el-icon-document");
            builder.CloseElement();
            builder.AddContent(seq++, file.FileName);
            builder.CloseElement();

            BuildStatusLabel(builder, file, ref seq);
            BuildDeleteIcon(builder, file, ref seq, "el-icon-close");

            builder.OpenElement(seq++, "i");
            builder.AddAttribute(seq++, "class", "el-icon-close-tip");
            // EP：<i v-if="!disabled" class="el-icon-close-tip">{{ t('el.upload.deleteTip') }}</i>
            if (!UploadDisabled)
            {
                builder.AddContent(seq++, EffectiveDeleteTipText);
            }
            builder.CloseElement();
        }

        protected void BuildPictureCardItem(RenderTreeBuilder builder, UploadModel file, ref int seq)
        {
            builder.OpenElement(seq++, "img");
            builder.AddAttribute(seq++, "src", file.Url);
            builder.AddAttribute(seq++, "alt", file.FileName);
            builder.AddAttribute(seq++, "class", "el-upload-list__item-thumbnail");
            builder.CloseElement();

            BuildStatusLabel(builder, file, ref seq);

            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "class", "el-upload-list__item-actions");
            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "class", "el-upload-list__item-preview");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, _ => PreviewFileAsync(file)));
            builder.OpenElement(seq++, "i");
            builder.AddAttribute(seq++, "class", "el-icon-zoom-in");
            builder.CloseElement();
            builder.CloseElement();

            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "class", "el-upload-list__item-delete");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, _ => DeleteFile(file)));
            builder.OpenElement(seq++, "i");
            builder.AddAttribute(seq++, "class", "el-icon-delete");
            builder.AddAttribute(seq++, "aria-label", EffectiveDeleteAriaLabel);
            builder.CloseElement();
            builder.CloseElement();
            builder.CloseElement();
        }

        private void BuildStatusLabel(RenderTreeBuilder builder, UploadModel file, ref int seq)
        {
            if (file.Status != UploadStatus.Success)
            {
                return;
            }

            builder.OpenElement(seq++, "label");
            builder.AddAttribute(seq++, "class", "el-upload-list__item-status-label");
            builder.OpenElement(seq++, "i");
            builder.AddAttribute(seq++, "class", "el-icon-upload-success el-icon-circle-check");
            builder.CloseElement();
            builder.CloseElement();
        }

        private void BuildDeleteIcon(RenderTreeBuilder builder, UploadModel file, ref int seq, string cls)
        {
            builder.OpenElement(seq++, "i");
            builder.AddAttribute(seq++, "class", cls);
            // EP 在删除图标上取词：<el-icon class="el-icon-close" :aria-label="t('el.upload.delete')" role="button" tabindex="0">
            builder.AddAttribute(seq++, "aria-label", EffectiveDeleteAriaLabel);
            builder.AddAttribute(seq++, "role", "button");
            builder.AddAttribute(seq++, "tabindex", "0");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, _ => DeleteFile(file)));
            builder.CloseElement();
        }

        private UploadRequestResult MapJsResult(string[] results)
        {
            if (ResponseParser == null)
            {
                return UploadRequestResult.FromJsResult(results);
            }

            var raw = results != null && results.Length > 4 ? results[4] : null;
            var mapped = ResponseParser(raw);
            if (mapped == null)
            {
                return new UploadRequestResult { Succeeded = false, Code = "1", Message = "Upload response parser returned null.", RawResponse = raw };
            }

            mapped.RawResponse = raw;
            return mapped;
        }

        /// <summary>JS 粘贴上传时现取的一份选项（旧实现把这份选项冻结在注册那一帧，
        /// Url/Data 之后再改粘贴仍按旧值发）。JS 侧只读 url/method/fileFieldName/withCredentials/headers/data/multiple/autoUpload。</summary>
        [JSInvokable("getPasteUploadOptions")]
        public object GetPasteUploadOptions()
        {
            return new
            {
                url = Url,
                method = Method,
                fileFieldName = FileFieldName,
                withCredentials = WithCredentials,
                headers = Headers,
                data = Data,
                multiple = Multiple,
                autoUpload = AutoUpload
            };
        }

        private async Task PreviewFileAsync(IFileModel file)
        {
            if (OnPreview.HasDelegate)
            {
                await OnPreview.InvokeAsync(file);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (eventRegistered)
            {
                _ = Document.UnRegisterPasteUploadAsync(_pasteHandle);
            }
        }
    }
}
