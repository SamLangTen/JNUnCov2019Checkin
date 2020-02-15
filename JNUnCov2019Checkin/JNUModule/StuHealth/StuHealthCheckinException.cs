using System;
using System.Collections.Generic;
using System.Text;

namespace JNUnCov2019Checkin.JNUModule.StuHealth
{
    class StuHealthCheckinException : Exception
    {
        public StuHealthCheckinException(string message) : base(message)
        {
        }

        public StuHealthCheckinException()
        {
        }
    }
}
