using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus.Markdown
{
    public interface IIconHandler
    {
        Task HandleAsync(ElMarkdownEditor editor);
    }
}
