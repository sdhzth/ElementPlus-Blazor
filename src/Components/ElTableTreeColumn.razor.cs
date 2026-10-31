using System;
using System.Collections.Generic;
using System.Text;

namespace ElementPlus
{
    public partial class ElTableTreeColumn : ElTableColumn
    {
        internal override bool IsTree { get; set; } = true;
    }
}
