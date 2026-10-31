using Microsoft.AspNetCore.Components;

namespace ElementPlus
{
    public partial class MessageBoxContent
    {
        [Parameter]
        public MessageBoxOption Option { get; set; }

        /// <summary>执行 InputValidator 校验</summary>
        public bool ValidateInput()
        {
            if (Option?.InputValidator == null) return true;
            var err = Option.InputValidator(Option.InputValue ?? "");
            Option.InputErrorMessage = err;
            return string.IsNullOrEmpty(err);
        }
    }
}