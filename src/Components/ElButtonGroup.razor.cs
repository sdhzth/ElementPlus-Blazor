using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElButtonGroup
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public ButtonSize Size { get; set; }

        internal ButtonSize EffectiveSize => ResolveButtonSize(Size);

        [Parameter]
        public ButtonType Type { get; set; } = ButtonType.Default;

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public string Direction { get; set; } = "horizontal";

        internal bool EffectiveDisabled => Disabled;

        protected string GroupClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-button-group", Cls)
            .AddIf(string.Equals(Direction, "vertical", StringComparison.OrdinalIgnoreCase), "is-vertical")
            .ToString();
    }
}
