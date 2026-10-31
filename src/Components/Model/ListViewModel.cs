using System;
using System.Collections.Generic;
using System.Text;

namespace ElementPlus.Model
{
    public class ListViewModel<T>
    {
        [TableColumn(Text = "列表")]
        public T Item { get; set; }
    }
}
