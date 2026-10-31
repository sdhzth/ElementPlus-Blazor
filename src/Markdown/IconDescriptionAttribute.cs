using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus.Markdown
{
    public class IconDescriptionAttribute : Attribute
    {
        public string IconCls { get; set; }
        public Type Handler { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
    }
}
