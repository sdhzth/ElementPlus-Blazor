using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElImage : ElementComponentBase
    {
        private bool loadFailed;
        private bool showViewer;
        private int previewIndex;
        private ElementReference _viewerWrapper;
        private bool _teleported;

        /* ---- 预览缩放 / 平移状态（对应 Element Plus image-viewer 的 zoom / drag 能力） ---- */
        private const double MinPreviewScale = 0.2;
        private const double MaxPreviewScale = 7;
        private const double PreviewScaleStep = 0.15;

        private double previewScale = 1;
        private double previewOffsetX;
        private double previewOffsetY;
        private bool previewDragging;
        private double dragStartX;
        private double dragStartY;
        private double dragOriginX;
        private double dragOriginY;

        [Parameter]
        public string Src { get; set; }

        [Parameter]
        public string Alt { get; set; }

        [Parameter]
        public string Fit { get; set; }

        [Parameter]
        public bool Lazy { get; set; }

        [Parameter]
        public string Width { get; set; }

        [Parameter]
        public string Height { get; set; }

        [Parameter]
        public RenderFragment Placeholder { get; set; }

        [Parameter]
        public RenderFragment ErrorContent { get; set; }

        [Parameter]
        public string ErrorText { get; set; } = "加载失败";

        [Parameter]
        public IList<string> PreviewSrcList { get; set; }

        /// <summary>
        /// 能否点击放大预览，默认 <c>true</c>。本库在不传 <see cref="PreviewSrcList"/> 时会回落到预览
        /// <see cref="Src"/>，所以「任何一张图都可点放大」；头像这类嵌在按钮/下拉触发器里、点了不该盖住菜单的图要显式置 <c>false</c>。
        /// 关掉只影响查看器，<see cref="OnClick"/> 照常回调。
        /// </summary>
        [Parameter]
        public bool Preview { get; set; } = true;

        [Parameter]
        public bool PreviewTeleported { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        [Parameter]
        public EventCallback OnError { get; set; }

        /// <summary>初始预览索引</summary>
        [Parameter]
        public int InitialIndex { get; set; }

        /// <summary>点击遮罩是否关闭预览（默认 true）</summary>
        [Parameter]
        public bool HideOnClickModal { get; set; } = true;

        /// <summary>关闭预览事件</summary>
        [Parameter]
        public EventCallback OnPreviewClose { get; set; }

        [Parameter]
        public string ReferrerPolicy { get; set; }

        [Parameter]
        public string SrcSet { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        protected bool ShowViewer => showViewer && PreviewList.Any();

        /// <summary>
        /// 预览图源。<see cref="Preview"/> 为 false 时在源头返回空列表——<see cref="ShowViewer"/>、
        /// 查看器渲染、<c>OpenPreviewAsync</c> 的早退都以它为准，这样开关只有一处真相。
        /// </summary>
        protected IReadOnlyList<string> PreviewList => !Preview
            ? new List<string>()
            : PreviewSrcList != null && PreviewSrcList.Any()
                ? PreviewSrcList.Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
                : string.IsNullOrWhiteSpace(Src) ? new List<string>() : new List<string> { Src };

        protected string CurrentPreviewSrc => PreviewList.Count == 0 ? Src : PreviewList[previewIndex];

        protected string LoadingMode => Lazy ? "lazy" : null;

        protected string ImageClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-image", Cls)
            .ToString();

        protected string WrapperStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(!string.IsNullOrWhiteSpace(Width), $"width:{ElementCssUtility.NormalizeCssSize(Width)}")
            .AddIf(!string.IsNullOrWhiteSpace(Height), $"height:{ElementCssUtility.NormalizeCssSize(Height)}")
            .Add(Style)
            .ToString();

        protected string ImageStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .AddIf(!string.IsNullOrWhiteSpace(Fit), $"object-fit:{Fit}")
            .ToString();

        /// <summary>预览大图的变换：先平移再缩放（滚轮缩放 / 拖动平移共用）</summary>
        protected string PreviewImageStyle => HtmlPropertyBuilder.CreateCssStyleBuilder()
            .Add($"transform:translate3d({previewOffsetX:0.##}px,{previewOffsetY:0.##}px,0) scale({previewScale:0.###})")
            .ToString();

        private void ResetPreviewView()
        {
            previewScale = 1;
            previewOffsetX = 0;
            previewOffsetY = 0;
            previewDragging = false;
        }

        /// <summary>滚轮缩放（向上放大 / 向下缩小），范围 0.2x ~ 7x</summary>
        protected Task OnPreviewWheelAsync(WheelEventArgs e)
        {
            if (!ShowViewer)
            {
                return Task.CompletedTask;
            }

            var next = Math.Round(previewScale + (e.DeltaY < 0 ? PreviewScaleStep : -PreviewScaleStep), 3);
            previewScale = Math.Clamp(next, MinPreviewScale, MaxPreviewScale);
            if (previewScale <= 1)
            {
                previewOffsetX = 0;
                previewOffsetY = 0;
            }

            return Task.CompletedTask;
        }

        /// <summary>双击在 1x / 2x 之间切换</summary>
        protected Task OnPreviewDoubleClickAsync(MouseEventArgs e)
        {
            if (previewScale > 1)
            {
                previewScale = 1;
                previewOffsetX = 0;
                previewOffsetY = 0;
            }
            else
            {
                previewScale = 2;
            }

            return Task.CompletedTask;
        }

        protected Task OnPreviewPointerDownAsync(PointerEventArgs e)
        {
            if (!ShowViewer || previewScale <= 1)
            {
                return Task.CompletedTask;
            }

            previewDragging = true;
            dragStartX = e.ClientX;
            dragStartY = e.ClientY;
            dragOriginX = previewOffsetX;
            dragOriginY = previewOffsetY;
            return Task.CompletedTask;
        }

        protected Task OnPreviewPointerMoveAsync(PointerEventArgs e)
        {
            if (!previewDragging)
            {
                return Task.CompletedTask;
            }

            previewOffsetX = dragOriginX + (e.ClientX - dragStartX);
            previewOffsetY = dragOriginY + (e.ClientY - dragStartY);
            return Task.CompletedTask;
        }

        protected Task OnPreviewPointerUpAsync(PointerEventArgs e)
        {
            previewDragging = false;
            return Task.CompletedTask;
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            previewIndex = InitialIndex >= 0 && InitialIndex < PreviewList.Count ? InitialIndex : 0;
            if (!string.IsNullOrWhiteSpace(Src))
            {
                loadFailed = false;
            }
        }

        protected async Task OnErrorAsync()
        {
            loadFailed = true;
            if (OnError.HasDelegate)
            {
                await OnError.InvokeAsync(null);
            }
        }

        protected async Task OpenPreviewAsync(MouseEventArgs e)
        {
            if (OnClick.HasDelegate)
            {
                await OnClick.InvokeAsync(e);
            }

            if (!PreviewList.Any())
            {
                return;
            }

            ResetPreviewView();
            showViewer = true;
            if (PreviewTeleported)
            {
                // 先让 Blazor 把预览层渲染进组件子树，再整体 teleport 到 body，
                // 避免被祖先 transform/overflow 裁剪（对齐 EP preview-teleported）。
                await InvokeAsync(StateHasChanged);
                await JSRuntime.InvokeVoidAsync("elementTeleportToBody", _viewerWrapper);
                _teleported = true;
            }
        }

        protected async Task ClosePreviewAsync()
        {
            if (_teleported)
            {
                // 关闭前先归位，否则 Blazor 会在错误的父节点上执行 removeChild 而抛错。
                await JSRuntime.InvokeVoidAsync("elementTeleportRestore", _viewerWrapper);
                _teleported = false;
            }
            ResetPreviewView();
            showViewer = false;
            if (OnPreviewClose.HasDelegate)
            {
                await OnPreviewClose.InvokeAsync();
            }
        }

        protected Task ShowPreviousAsync()
        {
            if (PreviewList.Count > 0)
            {
                previewIndex = (previewIndex - 1 + PreviewList.Count) % PreviewList.Count;
            }

            ResetPreviewView();
            return Task.CompletedTask;
        }

        protected Task ShowNextAsync()
        {
            if (PreviewList.Count > 0)
            {
                previewIndex = (previewIndex + 1) % PreviewList.Count;
            }

            ResetPreviewView();
            return Task.CompletedTask;
        }
    }
}
