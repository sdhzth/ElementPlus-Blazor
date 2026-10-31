using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public enum MessageType
    {
        Info,
        Success,
        Warning,
        Error,
        /// <summary>主要（EP 2.9+ 新增的第五种类型，蓝色系）</summary>
        Primary
    }
}
