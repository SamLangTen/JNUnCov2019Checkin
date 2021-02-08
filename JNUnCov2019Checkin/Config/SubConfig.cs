using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace JNUnCov2019Checkin.Config
{
    class SubConfig
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string EncryptedUsername { get; set; }
        public string UserAgent { get; set; }
        public bool Enabled { get; set; }

    }
}
