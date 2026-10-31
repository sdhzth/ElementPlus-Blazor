using System.Threading.Tasks;

namespace ElementPlus
{
    public interface IAsyncValidationRule : IValidationRule
    {
        Task<bool> ValidateAsync(object value);
    }
}
