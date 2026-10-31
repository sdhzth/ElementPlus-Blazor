using Microsoft.AspNetCore.Components;
using System;

namespace ElementPlus
{
    public partial class ElDescriptionsItem : ElementComponentBase
    {
        [CascadingParameter]
        public ElDescriptions Descriptions { get; set; }

        [Parameter]
        public string Label { get; set; }

        [Parameter]
        public RenderFragment LabelContent { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public int Span { get; set; } = 1;

        /// <summary>单元格跨越的行数（EP 2.8.1 rowspan）。</summary>
        [Parameter]
        public int RowSpan { get; set; } = 1;

        [Parameter]
        public int Order { get; set; }

        /// <summary>内容对齐方式（EP align：left / center / right）；null 时不加修饰类，对齐 EP 行为。</summary>
        [Parameter]
        public DescriptionsAlign? Align { get; set; }

        /// <summary>标签对齐方式（EP label-align）；未设置时回落到 Align。</summary>
        [Parameter]
        public DescriptionsAlign? LabelAlign { get; set; }

        /// <summary>列内容自定义类名（EP class-name）。</summary>
        [Parameter]
        public string ClassName { get; set; }

        /// <summary>列标签自定义类名（EP label-class-name）。</summary>
        [Parameter]
        public string LabelClassName { get; set; }

        [Parameter]
        public string Width { get; set; }

        [Parameter]
        public string MinWidth { get; set; }

        /// <summary>列标签宽度（EP 2.8.8 label-width），优先级高于 Descriptions.LabelWidth；如无 border，宽度包含标签与内容。</summary>
        [Parameter]
        public string LabelWidth { get; set; }

        /// <summary>单独控制冒号显示（null 继承父组件 Colon，默认不显示，对齐 EP 官方无冒号）。</summary>
        [Parameter]
        public bool? Colon { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        internal bool ShowColon => Colon ?? Descriptions?.Colon ?? false;

        internal int NormalizedSpan => Math.Max(1, Math.Min(Span, Descriptions?.EffectiveColumn ?? Span));

        /// <summary>标签宽度解析顺序对齐 EP：本项 LabelWidth &gt; Descriptions.LabelWidth &gt; 本项 Width。</summary>
        internal string LabelWidthResolved
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(LabelWidth)) return LabelWidth;
                if (Descriptions != null && !string.IsNullOrWhiteSpace(Descriptions.LabelWidth)) return Descriptions.LabelWidth;
                return string.IsNullOrWhiteSpace(Width) ? null : Width;
            }
        }

        internal RenderFragment RenderLabel() =>
            LabelContent ?? (builder => builder.AddContent(0, Label));

        internal string GetAlignClass(DescriptionsAlign? a) => a switch
        {
            DescriptionsAlign.Center => "is-center",
            DescriptionsAlign.Right => "is-right",
            _ => string.Empty
        };

        /// <summary>边框 / 垂直模式下的 label 单元格（&lt;th&gt;）类。</summary>
        internal string GetLabelClass()
        {
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-descriptions__label")
                .AddIf(Descriptions?.Border == true, "is-bordered-label")
                .AddIf(Descriptions?.Direction == DescriptionsDirection.Vertical, "is-vertical-label")
                .Add(GetAlignClass(LabelAlign ?? Align))
                .Add(LabelClassName)
                .ToString();
        }

        /// <summary>边框 / 垂直模式下的 content 单元格（&lt;td&gt;）类。</summary>
        internal string GetContentClass()
        {
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-descriptions__content")
                .AddIf(Descriptions?.Border == true, "is-bordered-content")
                .AddIf(Descriptions?.Direction == DescriptionsDirection.Vertical, "is-vertical-content")
                .Add(GetAlignClass(Align))
                .Add(ClassName)
                .ToString();
        }

        /// <summary>无边框模式下的整格（&lt;td class="el-descriptions__cell"&gt;）类。</summary>
        internal string GetCellClass()
        {
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-descriptions__cell")
                .Add(GetAlignClass(Align))
                .ToString();
        }

        /// <summary>无边框模式下 label / content 行内 span 的类。</summary>
        internal string GetLabelSpanClass() =>
            HtmlPropertyBuilder.CreateCssClassBuilder().Add("el-descriptions__label").Add(LabelClassName).ToString();
        internal string GetContentSpanClass() =>
            HtmlPropertyBuilder.CreateCssClassBuilder().Add("el-descriptions__content").Add(ClassName).ToString();

        /// <summary>边框模式下 label 单元格（&lt;th&gt;）内联样式：标签宽度 + 最小宽度。</summary>
        internal string GetLabelCellStyle()
        {
            var cb = HtmlPropertyBuilder.CreateCssStyleBuilder();
            var w = LabelWidthResolved;
            if (!string.IsNullOrWhiteSpace(w)) cb.Add($"width:{ElementCssUtility.NormalizeCssSize(w)}");
            if (!string.IsNullOrWhiteSpace(MinWidth)) cb.Add($"min-width:{ElementCssUtility.NormalizeCssSize(MinWidth)}");
            return cb.ToString();
        }

        /// <summary>内容单元格（&lt;td&gt;）内联样式：宽度 + 最小宽度。</summary>
        internal string GetContentStyle()
        {
            var cb = HtmlPropertyBuilder.CreateCssStyleBuilder();
            if (!string.IsNullOrWhiteSpace(Width)) cb.Add($"width:{ElementCssUtility.NormalizeCssSize(Width)}");
            if (!string.IsNullOrWhiteSpace(MinWidth)) cb.Add($"min-width:{ElementCssUtility.NormalizeCssSize(MinWidth)}");
            return cb.ToString();
        }

        /// <summary>无边框模式下 label span 的内联样式：设置标签宽度时转为 inline-block。</summary>
        internal string GetLabelSpanStyle()
        {
            var w = LabelWidthResolved;
            if (string.IsNullOrWhiteSpace(w)) return string.Empty;
            return $"width:{ElementCssUtility.NormalizeCssSize(w)};display:inline-block";
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Descriptions?.AddItem(this);
        }

        public override void Dispose()
        {
            Descriptions?.RemoveItem(this);
            base.Dispose();
        }
    }
}
