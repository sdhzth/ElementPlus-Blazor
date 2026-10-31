using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ElementPlus.DisplayRenders
{
    public interface IDisplayRender
    {
        Func<object,object> CreateRender(TableHeader tableHeader);
    }
}
