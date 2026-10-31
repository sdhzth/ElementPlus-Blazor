using Bunit;
using ElementPlus;
using ElementPlus.ControlConfigs;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElUploadTests : BunitContext
    {
        public ElUploadTests()
        {
            Services.AddElementServices();
        }

        [Fact]
        public async Task PictureCardListRendersActionsAndTemplateContent()
        {
            RenderFragment<UploadModel> template = file => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-file");
                builder.AddContent(2, $"{file.FileName}:{file.Status}");
                builder.CloseElement();
            };
            var cut = Render<ElUpload>(parameters => parameters
                .Add(x => x.Url, "/upload")
                .Add(x => x.EnablePasteUpload, false)
                .Add(x => x.ListType, UploadListType.PictureCard)
                .Add(x => x.AutoUpload, false)
                .Add(x => x.FileTemplate, template));

            await cut.InvokeAsync(() => cut.Instance.PasteUploadFiles(new[]
            {
                new[] { "avatar.png", "1024", "64", "64", "/avatar.png" }
            }));

            Assert.Contains("el-upload--picture-card", cut.Find(".el-upload").ClassList);
            Assert.Contains("el-upload-list--picture-card", cut.Find(".el-upload-list").ClassList);
            Assert.Equal("avatar.png:UnStart", cut.Find(".custom-file").TextContent);
        }

        [Fact]
        public async Task MissingUrlDoesNotThrowAtInitializationButThrowsOnSubmit()
        {
            // 消费侧有「OnChange 里现问后端拿带签名的上传地址，再 SubmitAsync」的用法：
            // 初始化那一刻本来就没有 Url，所以这条校验只能在真要发请求时做。
            // 旧写法在 OnInitialized 抛 ⇒ 整页渲染被打死（WebAssemblyRenderer[100] Unhandled exception）。
            var cut = Render<ElUpload>(parameters => parameters
                .Add(x => x.EnablePasteUpload, false)
                .Add(x => x.AutoUpload, false));

            // 正向对照：组件确实渲染出来了（不是被静默跳过）
            Assert.NotNull(cut.Find(".el-upload"));

            cut.InvokeAsync(() => cut.Instance.PasteUploadFiles(new[]
            {
                new[] { "avatar.png", "1024", "64", "64", "/avatar.png" }
            }));
            // 确实有待传文件、又没有任何上传出口时仍要报错，不能变成静默丢弃
            await Assert.ThrowsAsync<ElementException>(() => cut.InvokeAsync(() => cut.Instance.SubmitAsync()));
        }

        [Fact]
        public void DragUploadRendersDraggerAndAcceptsConfiguredExtensions()
        {
            var cut = Render<ElUpload>(parameters => parameters
                .Add(x => x.Url, "/upload")
                .Add(x => x.EnablePasteUpload, false)
                .Add(x => x.Drag, true)
                .Add(x => x.AllowExtensions, new[] { ".png", ".jpg" }));

            var root = cut.Find(".el-upload");
            var input = cut.Find("input[type=file]");

            Assert.Contains("el-upload--drag", root.ClassList);
            Assert.NotNull(cut.Find(".el-upload-dragger"));
            Assert.Equal(".png,.jpg", input.GetAttribute("accept"));
        }

        [Fact]
        public async Task LimitInvokesExceedAndKeepsExistingFiles()
        {
            UploadExceedEventArgs exceed = null;
            var cut = Render<ElUpload>(parameters => parameters
                .Add(x => x.Url, "/upload")
                .Add(x => x.EnablePasteUpload, false)
                .Add(x => x.AutoUpload, false)
                .Add(x => x.Limit, 1)
                .Add(x => x.OnExceed, args => exceed = args));

            await cut.InvokeAsync(() => cut.Instance.PasteUploadFiles(new[]
            {
                new[] { "one.txt", "100" }
            }));
            await cut.InvokeAsync(() => cut.Instance.PasteUploadFiles(new[]
            {
                new[] { "two.txt", "100" }
            }));

            Assert.NotNull(exceed);
            Assert.Equal("two.txt", exceed.Files.Single().FileName);
            Assert.Single(cut.Instance.Files);
            Assert.Equal("true", cut.Find(".el-upload").GetAttribute("aria-disabled"));
        }

        [Fact]
        public async Task BeforeUploadPreviewAndBeforeRemoveHooksRun()
        {
            var changed = new List<string>();
            IFileModel previewed = null;
            var deleteCount = 0;
            var cut = Render<ElUpload>(parameters => parameters
                .Add(x => x.Url, "/upload")
                .Add(x => x.EnablePasteUpload, false)
                .Add(x => x.AutoUpload, false)
                .Add(x => x.BeforeUpload, file => Task.FromResult(file.FileName != "blocked.txt"))
                .Add(x => x.BeforeRemove, _ => Task.FromResult(false))
                .Add(x => x.OnPreview, file => previewed = file)
                .Add(x => x.OnDeleteFile, _ => deleteCount++)
                .Add(x => x.OnChange, args => changed.Add(args.File.FileName)));

            await cut.InvokeAsync(() => cut.Instance.PasteUploadFiles(new[]
            {
                new[] { "blocked.txt", "100" },
                new[] { "accepted.txt", "100" }
            }));

            cut.Find(".el-upload-list__item-name").Click();
            cut.Find(".el-icon-close").Click();

            Assert.Equal(new[] { "accepted.txt" }, changed);
            Assert.Equal("accepted.txt", previewed.FileName);
            Assert.Equal(0, deleteCount);
            Assert.Single(cut.Instance.Files);
        }

        [Fact]
        public async Task CustomRequestControlsUploadResultAndProgress()
        {
            JSInterop.SetupVoid("clear", _ => true).SetVoidResult();
            var progress = new List<double>();
            UploadRequestContext request = null;
            UploadRequestEventArgs success = null;
            IFileModel[] uploadedList = null;
            var cut = Render<ElUpload>(parameters => parameters
                .Add(x => x.Url, "/custom-upload")
                .Add(x => x.EnablePasteUpload, false)
                .Add(x => x.AutoUpload, false)
                .Add(x => x.Method, "put")
                .Add(x => x.FileFieldName, "asset")
                .Add(x => x.HttpRequest, async context =>
                {
                    request = context;
                    await context.ReportProgress(42);
                    return UploadRequestResult.Success("server-id", "/files/report.txt", "ok");
                })
                .Add(x => x.OnProgress, args => progress.Add(args.Percent))
                .Add(x => x.OnSuccess, args => success = args)
                .Add(x => x.OnFileListUpload, files => uploadedList = files));

            await cut.InvokeAsync(() => cut.Instance.PasteUploadFiles(new[]
            {
                new[] { "report.txt", "2048" }
            }));
            await cut.InvokeAsync(() => cut.Instance.SubmitAsync());

            var file = cut.Instance.Files.Cast<UploadModel>().Single();
            Assert.Equal("report.txt", request.File.FileName);
            Assert.Equal("/custom-upload", request.Url);
            Assert.Equal("put", request.Method);
            Assert.Equal("asset", request.FileFieldName);
            Assert.Equal(new[] { 0d, 42d, 100d }, progress);
            Assert.NotNull(success);
            Assert.Equal("server-id", file.Id);
            Assert.Equal("/files/report.txt", file.Url);
            Assert.Equal(UploadStatus.Success, file.Status);
            Assert.Single(uploadedList);
            Assert.Contains("is-success", cut.Find(".el-upload-list__item").ClassList);
        }

        [Fact]
        public void GeneratedUploadAppliesAttributeConfiguration()
        {
            var cut = Render<ElForm>(parameters => parameters
                .Add(x => x.EntityType, typeof(GeneratedUploadModel))
                .Add(x => x.Value, new GeneratedUploadModel()));

            var root = cut.Find(".el-upload");
            var input = cut.Find("input[type=file]");

            Assert.Contains("el-upload--picture-card", root.ClassList);
            Assert.Contains("is-disabled", root.ClassList);
            Assert.Equal("true", root.GetAttribute("aria-disabled"));
            Assert.Equal(".png", input.GetAttribute("accept"));
            Assert.Equal("asset", input.GetAttribute("name"));
            Assert.False(input.HasAttribute("multiple"));
            Assert.Empty(cut.FindAll(".el-upload-list"));
        }

        private class GeneratedUploadModel
        {
            [Upload(
                Url = "/upload",
                ListType = UploadListType.PictureCard,
                Limit = 1,
                Multiple = false,
                AutoUpload = false,
                ShowFileList = false,
                Disabled = true,
                Accept = ".png",
                FileFieldName = "asset",
                EnablePasteUpload = false)]
            public IFileModel[] Files { get; set; }
        }

        // ---- 批次㊼：响应键交宿主判、原始响应体进 RawResponse、粘贴 id 与槽位对齐、选项不再冻结 ----

        /// <summary>宿主后端返的是 <c>result</c> 而不是 <c>code</c>。JS 只负责把原始响应体整份递上来，
        /// 成败与取键全归 <see cref="ElUpload.ResponseParser"/>；锁的是「原始文本确实进了解析器、
        /// 解析器的返回值确实驱动了 Id/Url/状态」这一条契约（浏览器侧的真实往返由 mock 上传服务那轮覆盖）。</summary>
        [Fact]
        public async Task ResponseParserReceivesRawBodyAndDrivesResult()
        {
            const string raw = "{\"result\":\"0\",\"message\":\"图片已接收\",\"fcode\":\"srv-7\",\"u_imgurl\":\"/img/srv-7.png\"}";
            var seenRaw = new List<string>();
            UploadRequestEventArgs success = null;
            var cut = Render<ElUpload>(parameters => parameters
                .Add(x => x.Url, "/upload")
                .Add(x => x.EnablePasteUpload, false)
                .Add(x => x.AutoUpload, false)
                .Add(x => x.ResponseParser, text =>
                {
                    seenRaw.Add(text);
                    return text != null && text.Contains("\"result\":\"0\"")
                        ? UploadRequestResult.Success("srv-7", "/img/srv-7.png", "图片已接收")
                        : UploadRequestResult.Failure("上传失败");
                })
                .Add(x => x.OnSuccess, args => success = args));

            var ids = await cut.InvokeAsync(() => cut.Instance.PasteUploadFiles(new[]
            {
                new[] { "shot.png", "100" }
            }));
            await cut.InvokeAsync(() => cut.Instance.FileUploadedFromJsAsync(new[] { "0", "图片已接收", "srv-7", "/img/srv-7.png", raw }, ids.Single()));

            Assert.Equal(new[] { raw }, seenRaw);
            var file = cut.Instance.Files.Cast<UploadModel>().Single();
            Assert.Equal("srv-7", file.Id);
            Assert.Equal("/img/srv-7.png", file.Url);
            Assert.Equal(UploadStatus.Success, file.Status);
            Assert.NotNull(success);
            Assert.Equal(raw, success.Response.RawResponse);
            Assert.Contains("is-success", cut.Find(".el-upload-list__item").ClassList);
        }

        /// <summary>不传解析器时仍是旧的 <c>{ code, message, id, url }</c> 四元映射（向后兼容正向对照）。</summary>
        [Fact]
        public async Task BuiltinMappingStillWorksWithoutParser()
        {
            var cut = Render<ElUpload>(parameters => parameters
                .Add(x => x.Url, "/upload")
                .Add(x => x.EnablePasteUpload, false)
                .Add(x => x.AutoUpload, false));

            var ids = await cut.InvokeAsync(() => cut.Instance.PasteUploadFiles(new[]
            {
                new[] { "legacy.txt", "100" }
            }));
            await cut.InvokeAsync(() => cut.Instance.FileUploadedFromJsAsync(new[] { "0", "ok", "ep-3", "/files/ep-3" }, ids.Single()));

            var file = cut.Instance.Files.Cast<UploadModel>().Single();
            Assert.Equal("ep-3", file.Id);
            Assert.Equal(UploadStatus.Success, file.Status);
            Assert.Equal("ok", file.Message);
        }

        /// <summary>响应体不是 JSON / 没有 code 键时，JS 侧给出的是「失败 + 原文」，
        /// C# 这一侧必须把它落成 Failure 状态并把原文留给宿主——旧版在这里是 Promise 永悬，界面一直转圈。</summary>
        [Fact]
        public async Task FailureCarryingRawBodyLandsInFailState()
        {
            UploadRequestEventArgs error = null;
            var cut = Render<ElUpload>(parameters => parameters
                .Add(x => x.Url, "/upload")
                .Add(x => x.EnablePasteUpload, false)
                .Add(x => x.AutoUpload, false)
                .Add(x => x.OnError, args => error = args));

            var ids = await cut.InvokeAsync(() => cut.Instance.PasteUploadFiles(new[]
            {
                new[] { "gateway.html", "100" }
            }));
            await cut.InvokeAsync(() => cut.Instance.FileUploadedFromJsAsync(new[] { "1", "Upload response is not valid JSON.", "", "", "<html>502</html>" }, ids.Single()));

            var file = cut.Instance.Files.Cast<UploadModel>().Single();
            Assert.Equal(UploadStatus.Failure, file.Status);
            Assert.Equal("Upload response is not valid JSON.", file.Message);
            Assert.NotNull(error);
            Assert.Equal("<html>502</html>", error.Response.RawResponse);
            Assert.Contains("is-fail", cut.Find(".el-upload-list__item").ClassList);
        }

        /// <summary>粘贴返回的 id 必须与文件槽位**同长同序**：被 BeforeUpload 挡掉的那格是空串。
        /// 旧版返回「已接受」的紧凑数组，JS 拿 <c>files[j]</c> 去配 <c>ids[j]</c>，多文件粘贴会把结果挂到错的那一行。</summary>
        [Fact]
        public async Task PasteIdsAlignWithFileSlotsWhenOneRejected()
        {
            var cut = Render<ElUpload>(parameters => parameters
                .Add(x => x.Url, "/upload")
                .Add(x => x.EnablePasteUpload, false)
                .Add(x => x.AutoUpload, false)
                .Add(x => x.BeforeUpload, file => Task.FromResult(file.FileName != "blocked.png")));

            var ids = await cut.InvokeAsync(() => cut.Instance.PasteUploadFiles(new[]
            {
                new[] { "blocked.png", "100" },
                new[] { "kept.png", "100" }
            }));

            Assert.Equal(2, ids.Length);
            Assert.Equal(string.Empty, ids[0]);
            Assert.NotEqual(string.Empty, ids[1]);
            Assert.Equal("kept.png", cut.Instance.Files.Cast<UploadModel>().Single().FileName);
        }

        /// <summary>粘贴选项每次现取（旧版冻结在注册那一帧，对话框里后填的 Url 永远用不上）。</summary>
        [Fact]
        public void PasteOptionsReflectCurrentParameters()
        {
            var cut = Render<ElUpload>(parameters => parameters
                .Add(x => x.Url, "/first")
                .Add(x => x.EnablePasteUpload, false)
                .Add(x => x.Multiple, false)
                .Add(x => x.AutoUpload, false)
                .Add(x => x.Method, "put")
                .Add(x => x.FileFieldName, "file"));

            Assert.Equal("/first", Value(cut.Instance.GetPasteUploadOptions(), "url"));
            Assert.Equal(false, Value(cut.Instance.GetPasteUploadOptions(), "multiple"));
            Assert.Equal(false, Value(cut.Instance.GetPasteUploadOptions(), "autoUpload"));
            Assert.Equal("put", Value(cut.Instance.GetPasteUploadOptions(), "method"));
            Assert.Equal("file", Value(cut.Instance.GetPasteUploadOptions(), "fileFieldName"));

            // 后填的地址要能被现取到（bUnit 这版 Render 要整批重给参数）
            cut.Render(parameters => parameters
                .Add(x => x.Url, "/later")
                .Add(x => x.EnablePasteUpload, false)
                .Add(x => x.Multiple, false)
                .Add(x => x.AutoUpload, false)
                .Add(x => x.Method, "put")
                .Add(x => x.FileFieldName, "file"));

            Assert.Equal("/later", Value(cut.Instance.GetPasteUploadOptions(), "url"));
            Assert.Equal("put", Value(cut.Instance.GetPasteUploadOptions(), "method"));
        }

        /// <summary>
        /// 粘贴登记按实例配句柄：两台各注册一次、句柄不同，摘除时只摘自己那条。
        /// 旧写法是整条 document 监听一起摘（后一架的 Dispose 把前一架也停了），
        /// 且 DotNetObjectReference 从不释放。
        /// </summary>
        [Fact]
        public void PasteRegistrationIsPerHandleAndUnregistersOnlyItself()
        {
            JSInterop.SetupVoid("registerPasteUpload", _ => true).SetVoidResult();
            JSInterop.SetupVoid("unRegisterPasteUpload", _ => true).SetVoidResult();

            var first = Render<ElUpload>(parameters => parameters.Add(x => x.Url, "/one"));
            var second = Render<ElUpload>(parameters => parameters.Add(x => x.Url, "/two"));

            var handles = JSInterop.Invocations
                .Where(x => x.Identifier == "registerPasteUpload")
                .Select(x => Assert.IsType<int>(x.Arguments[2]))
                .ToList();
            Assert.Equal(2, handles.Count);
            Assert.Equal(2, handles.Distinct().Count());

            first.Instance.Dispose();

            var unregisters = JSInterop.Invocations
                .Where(x => x.Identifier == "unRegisterPasteUpload")
                .ToList();
            Assert.Single(unregisters);
            Assert.Equal(handles[0], Assert.IsType<int>(unregisters[0].Arguments[0]));
            // 另一架还活着：它的登记不能被同伴的 Dispose 顺带摘掉
            Assert.DoesNotContain(unregisters, x => Equals(Assert.IsType<int>(x.Arguments[0]), handles[1]));
            Assert.NotNull(second.Instance);
        }

        private static object Value(object options, string key)
        {
            return options.GetType().GetProperty(key).GetValue(options);
        }
    }
}
