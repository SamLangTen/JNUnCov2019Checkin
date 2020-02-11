using System;
using JNUnCov2019Checkin.JNUModule.ICAS;

namespace JNUnCov2019Checkin
{
    class Program
    {
        static void Main(string[] args)
        {
            var icas = new ICASModule();
            icas.Login(Console.ReadLine(), Console.ReadLine()).Wait();
            Console.WriteLine("Login successfully");
        }
    }
}
