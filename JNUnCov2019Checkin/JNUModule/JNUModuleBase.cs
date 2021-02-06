using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace JNUnCov2019Checkin.JNUModule
{
    public class JNUModuleBase
    {
        protected CookieContainer Cookies { get; set; }

        /// <summary>
        /// User-Agent of this module.
        /// </summary>
        public string UserAgent { get; set; }

    }
}
