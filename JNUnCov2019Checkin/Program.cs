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
using Newtonsoft.Json.Linq;

namespace JNUnCov2019Checkin
{
    class Program
    {
        static List<Config> Configs { get; set; }

        static async Task Checkin(Config config, string encryptionKey)
        {

            var stuhealth = new StuHealthModule();

            try
            {
                var encryptedPassword = StuHealthModule.EncryptPassword(config.Password, encryptionKey);

                var encryptedUsername = await stuhealth.Login(config.Username, encryptedPassword);
                Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{config.Username} has logined in to StuHealth");

                if (stuhealth.State == CheckinState.Finished)
                    Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{config.Username} found today's check-in, bot will not check-in again");
                else if (stuhealth.State == CheckinState.Unfinished)
                {
                    var mainTable = await stuhealth.GetLastCheckin(encryptedUsername);

                    await stuhealth.Checkin(encryptedUsername, mainTable);
                    Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{config.Username} has finished today check-in");
                }

            }
            catch (StuHealthLoginException ex)
            {
                Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{config.Username} has failed to login to StuHealth, reason: {ex.Message}");
            }
            catch (StuHealthCheckinException ex)
            {
                Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{config.Username} has failed to do check-in, reason: {ex.Message}");
            }
            catch (StuHealthLastCheckinNotFoundException)
            {
                Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{config.Username} can not find last check-in record, please do check-in manually at least once");
            }
            catch (StuHealthCheckinLessThanSixHourException)
            {
                Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{config.Username} can not do check-in because it has been done in 6 hours, please do check-in again after 6 hours");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{config.Username} got a unhandled exception, reason: {ex.Message}");
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
                try
                {
                    var encryptionKey = "";
                    Task.WaitAll(Task.Run(async () =>
                    {
                        encryptionKey = await StuHealthModule.GetEncryptionKey();
                    }));
                    Console.WriteLine($"[{DateTime.Now.ToString()}] JNUnCov2019Checkin program has fetched the key");

                    Task.WaitAll(Configs.Where(c => c.Enabled).Select(c => Task.Run(() => Checkin(c, encryptionKey))).ToArray());
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now.ToString()}] JNUnCov2019Checkin program got exception while fetching key, reason:{ex.Message}");
                }
            }

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

                    Console.Write("Enable this bot?(Y/n):");
                    config.Enabled = Console.ReadLine().ToLower() == "n" ? false : true;
                    Configs.Add(config);
                    Config.SaveConfigs(Configs, configPath);
                }
            }
        }
    }
}
