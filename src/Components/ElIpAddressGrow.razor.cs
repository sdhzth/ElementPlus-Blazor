using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// IPv4 地址输入（四段 + 圆点分隔，按 Element Plus 观感自渲染，零第三方依赖）。
    /// <para>
    /// <see cref="Value"/> 是点分十进制字符串（<c>@bind-Value</c>），四段各 0-255；段不足四位时按 <c>0</c> 补齐，
    /// 超过三位的输入截断后再钳到 255。键入行为（数字过滤、满 3 位自动进下一格、<c>.</c>/方向键跨格、
    /// 退格回退一格、粘贴整串 IPv4 自动摊到四格、输入法拦截）由 <c>elementIpAddressGrowAttach</c> 在浏览器侧完成，
    /// 因为它要看得到光标位置；值模型（过滤、截断、钳位、合成字符串）始终由本类的 C# 侧负责，
    /// 所以在没有 JS 的环境（bUnit、预渲染）里 <see cref="Value"/> 依然正确，只是少了跨格动作。
    /// </para>
    /// </summary>
    public partial class ElIpAddressGrow : ElementFieldComponentBase<string>, IAsyncDisposable
    {
        /// <summary>IPv4 固定四段。</summary>
        private const int CellCount = 4;

        /// <summary>每段上限（IPv4 八位组）。</summary>
        private const int MaxOctet = 255;

        private static readonly int[] CellIndexes = { 0, 1, 2, 3 };

        private readonly string[] _segments = { "0", "0", "0", "0" };
        private ElementReference _wrapper;
        private DotNetObjectReference<ElIpAddressGrow> dotNetReference;
        private string lastEmittedValue;
        private bool attached;
        private bool isFocused;
        private bool isDisposed;

        /// <summary>点分十进制地址（支持 <c>@bind-Value</c>），如 <c>192.168.1.10</c>。</summary>
        [Parameter]
        public string Value { get; set; }

        /// <summary><see cref="Value"/> 变更回调，供 <c>@bind-Value</c> 使用。</summary>
        [Parameter]
        public EventCallback<string> ValueChanged { get; set; }

        /// <summary>禁用（四段全部不可编辑，外观置灰）。</summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>只读：可读不可改，仍在 Tab 序里（与 <see cref="Disabled"/> 的区别）。</summary>
        [Parameter]
        public bool Readonly { get; set; }

        /// <summary>尺寸，与 ElInput 的 <c>Size</c> 同一套取值。默认 <see cref="InputSize.Normal"/>。</summary>
        [Parameter]
        public InputSize Size { get; set; } = InputSize.Normal;

        /// <summary>每段 <c>aria-label</c> 的前缀文案（无障碍）。</summary>
        [Parameter]
        public string Label { get; set; } = "IP 地址";

        private string WrapperClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-ip-address-grow")
            .AddIf(Size == InputSize.Large, "el-ip-address-grow--large")
            .AddIf(Size == InputSize.Small, "el-ip-address-grow--small")
            .AddIf(Disabled, "is-disabled")
            .AddIf(isFocused, "is-focus")
            .Add(Cls)
            .ToString();

        private string WrapperStyle => string.IsNullOrWhiteSpace(Style) ? null : Style;

        private string SegmentText(int index) => _segments[index];

        private string CellLabel(int index) => $"{Label} 第 {index + 1} 段（0-{MaxOctet}）";

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            // 只在父级真的换了 Value 时回填四段；@bind-Value 回写滞后时传回来的就是自己刚发出去的那个串，
            // 无条件回填会把用户正在编辑的半截输入按旧值打回（ElInput 的 rawInputText 同因）。
            if (!string.Equals(Value, lastEmittedValue, StringComparison.Ordinal))
            {
                ApplyValue(Value);
                lastEmittedValue = Value;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender || attached || isDisposed)
            {
                return;
            }

            try
            {
                dotNetReference ??= DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("elementIpAddressGrowAttach", _wrapper, dotNetReference);
                attached = true;
            }
            catch
            {
                // JS 不可用（单测宿主未注册 interop、模块加载失败）时静默降级：值模型仍由 C# 保证。
                attached = false;
            }
        }

        /// <summary>把点分字符串拆成四段并逐段规范化；不足四段时补 <c>0</c>（与拆分丢掉的空段同源，按位置对齐）。</summary>
        private void ApplyValue(string value)
        {
            var parts = (value ?? string.Empty)
                .Split('.', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < CellCount; i++)
            {
                _segments[i] = i < parts.Length ? Normalize(parts[i]) : "0";
            }
        }

        /// <summary>只留数字、最多三位、超过 255 钳到 255；空段保留为空（编辑中间态），合成时按 0 呈现。</summary>
        private static string Normalize(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return string.Empty;
            }

            var digits = new string(raw.Where(char.IsDigit).ToArray());
            if (digits.Length == 0)
            {
                return string.Empty;
            }

            if (digits.Length > 3)
            {
                digits = digits.Substring(0, 3);
            }

            if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                return string.Empty;
            }

            return Math.Min(number, MaxOctet).ToString(CultureInfo.InvariantCulture);
        }

        private string Compose()
            => string.Join(".", _segments.Select(s => s.Length == 0 ? "0" : s));

        private async Task OnCellInputAsync(int index, ChangeEventArgs args)
        {
            if (Disabled || Readonly)
            {
                return;
            }

            _segments[index] = Normalize(args.Value?.ToString());
            await EmitAsync();
        }

        /// <summary>粘贴整串 IPv4 后由 JS 回写四段（JS 侧已把 DOM 填好，这里同步值模型）。</summary>
        [JSInvokable]
        public async Task TriggerUpdate(int v1, int v2, int v3, int v4)
        {
            if (isDisposed)
            {
                return;
            }

            var values = new[] { v1, v2, v3, v4 };
            for (var i = 0; i < CellCount; i++)
            {
                _segments[i] = Math.Min(Math.Max(values[i], 0), MaxOctet).ToString(CultureInfo.InvariantCulture);
            }

            await EmitAsync();
            // JS 回调不保证在渲染线程上，直接 StateHasChanged 会抛「not associated with the Dispatcher」。
            await InvokeAsync(StateHasChanged);
        }

        private async Task EmitAsync()
        {
            var composed = Compose();
            lastEmittedValue = composed;
            Value = composed;
            SetFieldValue(composed, validate: true);
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(composed);
            }
        }

        private void OnCellFocus() => isFocused = true;

        private void OnCellBlur() => isFocused = false;

        /// <summary>表单重置（ElFormItem 的 OnReset）时按外层给回的初值重新拆段。</summary>
        protected override void FormItem_OnReset(object value, bool requireRerender)
            => ApplyValue(value as string ?? Value);

        public async ValueTask DisposeAsync()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            if (attached)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("elementIpAddressGrowDetach", _wrapper);
                }
                catch
                {
                    // 卸载竞态下 interop 可能已不可用，监听器随页面一起销毁。
                }

                attached = false;
            }

            dotNetReference?.Dispose();
            dotNetReference = null;
            base.Dispose();
        }
    }
}
