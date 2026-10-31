using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace ElementPlus
{
    public partial class ElRibbonItem
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}
