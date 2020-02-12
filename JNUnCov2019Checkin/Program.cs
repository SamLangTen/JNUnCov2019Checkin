using System;
using System.IO;
using System.Net;
using System.Net.Http;
using JNUnCov2019Checkin.JNUModule.Ehall;
using JNUnCov2019Checkin.JNUModule.ICAS;

namespace JNUnCov2019Checkin
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 5) return;
            var cookies = new CookieContainer();
            var icas = new ICASModule();
            icas.Cookies = cookies;
            icas.Login(args[0], args[1]).Wait();
            Console.WriteLine("Login to ICAS successfully");

            var ehall = new EhallModule();
            ehall.Cookies = cookies;
            ehall.Login(args[2]).Wait();
            Console.WriteLine("Login to EHall successfully");

            var formData = File.ReadAllText(args[3]);
            var boundsField = File.ReadAllText(args[4]);

            ehall.StudentnCov2019Checkin(boundsField, formData).Wait();
            Console.WriteLine("nCov2019Checkin successfully");
        }
    }
}
