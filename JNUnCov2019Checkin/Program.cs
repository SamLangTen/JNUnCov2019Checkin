using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using JNUnCov2019Checkin.JNUModule.Ehall;
using JNUnCov2019Checkin.JNUModule.ICAS;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;

namespace JNUnCov2019Checkin
{
    class Program
    {
        static List<Config> Configs { get; set; }
        static CancellationTokenSource tokenSource { get; set; }

        static async Task StartCheckin(Config config)
        {
            if (config.Enabled == false) return;
            if (tokenSource.IsCancellationRequested)
            {
                Console.WriteLine($"Check-in bot {config.Username} has exited");
                return;
            }

            DateTime? lastCheckinTime = null;

            while (true)
            {
                if (tokenSource.IsCancellationRequested)
                {
                    Console.WriteLine($"Check-in bot {config.Username} has exited");
                    return;
                }

                var todayCheckinTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, config.CheckinTime.Hour, config.CheckinTime.Minute, config.CheckinTime.Second);
                if (lastCheckinTime == null || (lastCheckinTime.Value.Day != DateTime.Now.Day && DateTime.Now > todayCheckinTime))
                {
                    var cookies = new CookieContainer();
                    var icas = new ICASModule();
                    icas.Cookies = cookies;
                    try
                    {
                        await icas.Login(config.Username, config.Password);
                        Console.WriteLine($"[{DateTime.Now}]Check-in bot #{config.Username} has logined in to ICAS");
                    }
                    catch (ICASLoginException)
                    {
                        Console.WriteLine($"[{DateTime.Now}]Check-in bot #{config.Username} has failed to login to ICAS, exiting");
                        return;
                    }

                    var ehall = new EhallModule();
                    ehall.Cookies = cookies;
                    try
                    {
                        await ehall.Login(config.Realname);
                        Console.WriteLine($"[{DateTime.Now}]Check-in bot #{config.Username} has logined to Ehall.");
                        lastCheckinTime = await ehall.GetLastEventTime("学生健康状况申报");
                        if (lastCheckinTime?.Day == DateTime.Now.Day)
                            Console.WriteLine($"[{DateTime.Now}]Check-in bot #{config.Username} found today's check-in, bot will not check-in again");
                        else
                        {
                            await ehall.StudentnCov2019Checkin(config.PostBoundsField, config.PostFormData);
                            Console.WriteLine($"[{DateTime.Now}]Check-in bot #{config.Username} has finished today check-in.");
                        }
                        
                    }
                    catch (EhallLoginException)
                    {
                        Console.WriteLine($"[{DateTime.Now}]Check-in bot #{config.Username} has failed to login to Ehall, exiting");
                        return;
                    }
                    catch (EhallStudentNCov2019CheckinException)
                    {
                        Console.WriteLine($"[{DateTime.Now}]Check-in bot #{config.Username} has failed to check-in, trying again");
                    }
                }
                Thread.Sleep(600000);
            }

        }

        static void Main(string[] args)
        {
            string configPath = "./config.json";
            if (args.Length != 0)
                configPath = args[0];
            if (File.Exists(configPath))
                Configs = Config.LoadConfigs(configPath);
            else
                Configs = new List<Config>();
            Console.WriteLine("Load config successfully");

            tokenSource = new CancellationTokenSource();

            var tasks = new ConcurrentBag<Task>();
            while (true)
            {
                Console.Write(">");
                var option = Console.ReadLine();
                if (option == "add")
                {
                    var config = new Config();
                    Console.Write("Username:");
                    config.Username = Console.ReadLine();
                    Console.Write("Password:");
                    config.Password = Console.ReadLine();
                    Console.Write("Realname:");
                    config.Realname = Console.ReadLine();
                    Console.Write("PostBoundsField Path:");
                    config.PostBoundsField = File.ReadAllText(Console.ReadLine());
                    Console.Write("PostFormData Path:");
                    config.PostFormData = File.ReadAllText(Console.ReadLine());
                    Console.Write("Check-in time(e.g. 05:44:22):");
                    config.CheckinTime = DateTime.Parse(Console.ReadLine());
                    Console.Write("Enable this bot?(Y/n):");
                    config.Enabled = Console.ReadLine().ToLower() == "n" ? false : true;
                    Configs.Add(config);
                    Config.SaveConfigs(Configs, configPath);
                }
                else if (option == "start")
                {
                    Configs.Select(c => Task.Run(() => StartCheckin(c))).ToList().ForEach(t => tasks.Add(t));
                }
                else if (option == "stop")
                {
                    Console.WriteLine("Stopping all Check-in bots...");
                    while (tasks.Where(t => t.Status == TaskStatus.Running).Count() > 0)
                        ;
                    tasks.Clear();
                    Console.WriteLine("All Check-in bots have stopped");
                }
            }
        }
    }
}
