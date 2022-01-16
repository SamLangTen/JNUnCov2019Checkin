using System;
using System.Threading.Tasks;
using JNUnCov2019Checkin.Config;

namespace JNUnCov2019Checkin.ValidationHelper
{
    abstract class ValidationHelper
    {
        public abstract Task<string> GetValidationAsync();

        public static ValidationHelper GetValidationHelper(string validationHelper, GlobalConfig config)
        {
            return validationHelper switch
            {
                "AkarinValidationHelper" => new AKarinValidationHelper(config),
                _ => null,
            };
        }
    }
}
