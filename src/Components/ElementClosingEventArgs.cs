using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public class ElementClosingEventArgs<T>
    {
        public T Target { get; set; }
        public bool Cancel { get; set; }
    }
}
