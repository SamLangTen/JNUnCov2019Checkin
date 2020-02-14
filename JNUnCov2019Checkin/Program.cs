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
using JNUnCov2019Checkin.JNUModule.StuHealth;

namespace JNUnCov2019Checkin
{
    class Program
    {
        static List<Config> Configs { get; set; }
        static CancellationTokenSource tokenSource { get; set; }

        static async Task Checkin(Config config)
        {

            var cookies = new CookieContainer();

            var stuhealth = new StuHealthModule();
            stuhealth.Cookies = cookies;

            try
            {
                if (config.IsManualMode)
                {
                    await stuhealth.Login(config.Username, config.EncryptedPassword);
                    Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{config.Username} has logined in to StuHealth");

                    if (stuhealth.State == CheckinState.Finished)
                        Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{config.Username} found today's check-in, bot will not check-in again");
                    else if (stuhealth.State == CheckinState.Unfinished)
                    {
                        await stuhealth.Checkin(config.EncryptedUsername, config.PostMainTable);
                        Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{config.Username} has finished today check-in");
                    }

                }
                else
                {
                    await stuhealth.Login(config.Username, config.Password, config.Key);
                    Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{config.Username} has logined in to StuHealth");

                    if (stuhealth.State == CheckinState.Finished)
                        Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{config.Username} found today's check-in, bot will not check-in again");
                    else if (stuhealth.State == CheckinState.Unfinished)
                    {
                        await stuhealth.Checkin(config.Username, config.Key, config.PostMainTable);
                        Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{config.Username} has finished today check-in");
                    }
                }
            }
            catch (StuHealthLoginException)
            {
                Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{config.Username} has failed to login to StuHealth, exiting");
            }
            catch (StuHealthCheckinException)
            {
                Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{config.Username} has failed to check-in, exiting");
            }
            catch (Exception)
            {
                throw;
            }

        }


        static void Main(string[] args)
        {
            string configPath = "./config.json";
            if (File.Exists(configPath))
                Configs = Config.LoadConfigs(configPath);
            else
                Configs = new List<Config>();
            Console.WriteLine("Load config successfully");

            //Automatic mode
            if (args.Length == 1 && args[0] == "-a")
            {
                if (Configs == null || Configs.Where(c => c.Enabled).Count() == 0) return;

                Task.WaitAll(Configs.Where(c => c.Enabled).Select(c => Task.Run(() => Checkin(c))).ToArray());
                return;
            }

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

                    Console.Write("Use Manual Mode(y/N):");
                    if (Console.ReadLine().ToLower() == "y")
                        config.IsManualMode = true;
                    else
                        config.IsManualMode = false;

                    if (config.IsManualMode)
                    {
                        Console.Write("Encrypted Password:");
                        config.EncryptedPassword = Console.ReadLine();
                        Console.Write("Encrypted Username:");
                        config.EncryptedUsername = Console.ReadLine();
                    }
                    else
                    {
                        Console.Write("Password:");
                        config.Password = Console.ReadLine();
                        Console.Write("Key:");
                        config.Key = Console.ReadLine();
                    }


                    Console.Write("MainTable Json File Path");
                    config.PostMainTable = File.ReadAllText(Console.ReadLine());

                    Console.Write("Enable this bot?(Y/n):");
                    config.Enabled = Console.ReadLine().ToLower() == "n" ? false : true;
                    Configs.Add(config);
                    Config.SaveConfigs(Configs, configPath);
                }
            }
        }
    }
}
