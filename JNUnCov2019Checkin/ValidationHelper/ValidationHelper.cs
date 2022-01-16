using System;
using System.Threading.Tasks;
namespace JNUnCov2019Checkin.ValidationHelper
{
    public interface IValidation
    {
        Task<string> GetValidationAsync();
    }
}
