using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElementPlus
{
    public class TreeLazyLoadContext
    {
        public TreeItemBase Node { get; set; }

        public Task<IEnumerable<TreeItemBase>> ChildrenTask { get; set; }
    }
}
