using ElementPlus;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace ElementPlus.X
{
    public partial class ElXThoughtChain : ElementComponentBase
    {
        [Parameter]
        public IEnumerable<XThoughtItem> Items { get; set; }

        protected string ThoughtChainClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-x-thought-chain", Cls)
            .ToString();
    }
}
