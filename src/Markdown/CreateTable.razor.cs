using ElementPlus;

using ElementPlus.Markdown.IconHandlers;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace ElementPlus.Markdown
{
    public partial class CreateTable
    {
        internal ElForm form;

        internal void Submit()
        {
            if (!form.IsValid())
            {
                return;
            }

            var model = form.GetValue<CreateTableModel>();
            _ = DialogService.CloseDialogAsync(this, model);
        }
    }
}
