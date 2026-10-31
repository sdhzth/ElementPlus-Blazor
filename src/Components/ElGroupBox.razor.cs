using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace ElementPlus
{
    public partial class ElGroupBox
    {
        /// <summary>
        /// 标题
        /// </summary>
        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}
