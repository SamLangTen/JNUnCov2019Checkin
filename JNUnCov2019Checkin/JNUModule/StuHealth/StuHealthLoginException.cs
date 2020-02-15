using System;
using System.Collections.Generic;
using System.Text;

namespace JNUnCov2019Checkin.JNUModule.StuHealth
{
    class StuHealthLoginException : Exception
    {
        public StuHealthLoginException(string message) : base(message)
        {
        }

        public StuHealthLoginException()
        {

        }
    }
}
