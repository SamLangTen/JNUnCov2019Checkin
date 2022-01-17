using System;
namespace JNUnCov2019Checkin.ValidationHelper
{
    public class ValidationHelperExeception : Exception
    {
        public ValidationHelperExeception(string message) :base(message)
        {
        }

        public ValidationHelperExeception()
        {
        }
    }
}
