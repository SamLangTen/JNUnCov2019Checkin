using System;
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
            var cookies = new CookieContainer();
            var icas = new ICASModule();
            icas.Cookies = cookies;
            icas.Login(Console.ReadLine(), Console.ReadLine()).Wait();
            Console.WriteLine("Login to ICAS successfully");
            var ehall = new EhallModule();
            ehall.Cookies = cookies;
            ehall.Login(Console.ReadLine()).Wait();
            Console.WriteLine("Login to EHall successfully");
        }
    }
}
