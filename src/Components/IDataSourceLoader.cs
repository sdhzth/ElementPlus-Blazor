using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ElementPlus
{
    public interface IDataSourceLoader
    {
        Task<object> LoadAsync();
    }
}
