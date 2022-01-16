using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using JNUnCov2019Checkin.JNUModule.StuHealth;
using JNUnCov2019Checkin.Config;
using System.Threading;
using JNUnCov2019Checkin.ValidationHelper;

namespace JNUnCov2019Checkin
{
    class Program
    {

        static async Task<bool> Checkin(SubConfig config, string encryptionKey, ValidationHelper.ValidationHelper validation)
        {
            var stuhealth = new StuHealthModule();
            stuhealth.UserAgent = config.UserAgent;
            //Get bot name
            string botName = config.Username;

            try
            {
                string encryptedUsername = null;

                if (config.Password != null)
                {
                    //Login mode
                    var encryptedPassword = StuHealthModule.EncryptPassword(config.Password, encryptionKey);

                    encryptedUsername = await stuhealth.Login(config.Username, encryptedPassword, await validation.GetValidationAsync());
                    Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{botName} has logined in to StuHealth");
                }
                else
                {
                    //Direct Mode
                    encryptedUsername = config.EncryptedUsername;
                }

                //Get last data table
                var dataTable = await stuhealth.GetLastCheckin(encryptedUsername);

                //Check if checkin today
                if (stuhealth.State == CheckinState.Finished)
                {
                    Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{botName} found today's check-in, bot will not do check-in again");
                    return true;
                }

                //Do checkin now
                await stuhealth.Checkin(encryptedUsername, dataTable);
                Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{botName} has finished today check-in");

                return true;
            }
            catch (StuHealthLoginException ex)
            {
                Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{botName} has failed to login to StuHealth, reason: {ex.Message}");
            }
            catch (StuHealthCheckinException ex)
            {
                Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{botName} has failed to do check-in, reason: {ex.Message}");
            }
            catch (StuHealthLastCheckinNotFoundException)
            {
                Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{botName} can not find last check-in record, please do check-in manually at least once");
            }
            catch (StuHealthCheckinLessThanSixHourException)
            {
                Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{botName} can not do check-in because it has been done in 6 hours, please do check-in again after 6 hours");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{botName} got a unhandled exception, reason: {ex.Message}");
            }
            return false;

        }

        static void PrintHelp()
        {
            Console.WriteLine("Usage: JNUnCov2019Checkin [-a] [-c <config_file>]");
            Console.WriteLine("Options:");
            Console.WriteLine("  -a\tDo check-in without asking.");
            Console.WriteLine("  -c <config_file>\tLoad specified config file");
            Console.WriteLine("  -h\tDisplay this help.");
        }

        static bool CheckinAll(GlobalConfig configs)
        {
            if (configs == null || configs.Configs == null || configs.Configs.Where(c => c.Enabled).Count() == 0) return true;
            try
            {
                var validation = ValidationHelper.ValidationHelper.GetValidationHelper(configs.ValidationHelper, configs);

                var encryptionKey = "";
                Task.WaitAll(Task.Run(async () =>
                {
                    encryptionKey = await StuHealthModule.GetEncryptionKey(configs.GlobalUserAgent);
                }));
                Console.WriteLine($"[{DateTime.Now.ToString()}] JNUnCov2019Checkin program has fetched the key");

                var parsedConfigs = configs.Configs.Where(c => c.Enabled).Select(t => new SubConfig()
                {
                    EncryptedUsername = t.EncryptedUsername,
                    Username = t.Username,
                    UserAgent = t.UserAgent ?? configs.GlobalUserAgent,
                    Password = t.Password
                }).ToList();
                //Do checkin for each user

                var taskResults = parsedConfigs.Select(c =>
                {
                    for (int i = 0; i < configs.RetryTimes + 1; i++)
                    {

                        var task = Checkin(c, encryptionKey, validation);
                        task.Wait();
                        Thread.Sleep(configs.CheckinInterval);
                        if (task.Result)
                        {
                            break;
                        }

                        //Reach maximum retry times but still failed
                        if (i == configs.RetryTimes)
                        {
                            return false;
                        }
                    }
                    return true;
                });

                var successTaskCount = taskResults.Where(r => r == true).Count();
                if (successTaskCount != parsedConfigs.Count)
                    return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now.ToString()}] JNUnCov2019Checkin program got exception while fetching key, reason:{ex.Message}");
                return false;
            }
            return true;
        }

        static GlobalConfig ConstructConfigs(string configPath)
        {
            GlobalConfig config = null;
            //Load config from file
            if (File.Exists(configPath))
                config = GlobalConfig.LoadConfigs(configPath);
            else
                config = new GlobalConfig()
                {
                    CheckinInterval = 0,
                    GlobalUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4324.146 Safari/537.36",
                    RetryTimes = 0,
                    Configs = new List<SubConfig>()
                };

            //Load config from environment variable
            var envInterval = Environment.GetEnvironmentVariable("JNUCHECKIN_INTERVAL");
            var envUseragent = Environment.GetEnvironmentVariable("JNUCHECKIN_USERAGENT");
            var envRetry = Environment.GetEnvironmentVariable("JNUCHECKIN_RETRY");
            if (envInterval != null) config.CheckinInterval = int.Parse(envInterval);
            if (envRetry != null) config.RetryTimes = int.Parse(envRetry);
            if (envUseragent != null) config.GlobalUserAgent = envUseragent;
            int envIndex = 1;
            while (true)
            {
                var envConfig = new SubConfig
                {
                    Username = Environment.GetEnvironmentVariable($"JNUCHECKIN{envIndex}_USERNAME"),
                    Password = Environment.GetEnvironmentVariable($"JNUCHECKIN{envIndex}_PASSWORD"),
                    EncryptedUsername = Environment.GetEnvironmentVariable($"JNUCHECKIN{envIndex}_ENCRYPTED"),
                    UserAgent = Environment.GetEnvironmentVariable($"JNUCHECKIN{envIndex}_USERAGENT"),
                    Enabled = true
                };
                if ((envConfig.Username != null && envConfig.Password != null) || envConfig.EncryptedUsername != null)
                    config.Configs.Add(envConfig);
                else
                    break;

                envIndex++;
            }


            Console.WriteLine("Load config successfully");
            return config;
        }

        static void InteractiveLoop(GlobalConfig config)
        {
            var validation = ValidationHelper.ValidationHelper.GetValidationHelper(config.ValidationHelper, config);

            string encryptionKey = "";
            while (true)
            {
                Console.Write(">");
                var option = Console.ReadLine();
                if (option == "add")
                {
                    var sub = new SubConfig();

                    Console.Write("Username:");
                    sub.Username = Console.ReadLine();

                    Console.Write("Password:");
                    sub.Password = Console.ReadLine();

                    Console.Write("Enable this bot?(Y/n):");
                    sub.Enabled = Console.ReadLine().ToLower() == "n" ? false : true;
                    config.Configs.Add(sub);
                }
                else if (option.StartsWith("checkin"))
                {
                    if (option == "checkin-all")
                    {
                        CheckinAll(config);
                    }
                    else
                    {
                        if (encryptionKey == "")
                        {
                            var task = StuHealthModule.GetEncryptionKey();
                            Task.WaitAll(task);
                            encryptionKey = task.Result;
                            Console.WriteLine($"[{DateTime.Now.ToString()}] JNUnCov2019Checkin program has fetched the key");
                        }

                        var subOptions = option.Split(" ");
                        if (subOptions.Length == 2)
                        {
                            var sub = config.Configs.FirstOrDefault(c => c.Username == subOptions[1]);
                            if (config != null)
                            {
                                Task.WaitAll(Checkin(sub, encryptionKey, validation));
                            }
                            else
                            {
                                Console.WriteLine($"Bot #{subOptions[1]} not found");
                            }
                        }
                    }
                }
                else if (option == "exit")
                {
                    return;
                }
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to JNU nCov2019 Check-in!");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine($"Version: {typeof(Program).Assembly.GetName().Version}");
            Console.WriteLine("");


            string configPath = "./config.json";
            bool enabledAuto = false;
            bool enabledHelp = false;

            //Simple "DFA"
            string currentState = "";
            foreach (var arg in args)
            {
                if (currentState == "" && arg == "-h")
                {
                    enabledHelp = true;
                    currentState = "printedHelp";
                }
                else if (currentState == "" && arg == "-a")
                {
                    enabledAuto = true;
                }
                else if (currentState == "" && arg == "-c")
                {
                    currentState = "waitConfig";
                }
                else if (currentState == "waitConfig")
                {
                    configPath = arg;
                    currentState = "";
                }
            }
            if (currentState != "printedHelp" && currentState != "") //Incorrect final state
            {
                PrintHelp();
                return;
            }


            //Display help
            if (enabledHelp)
            {
                PrintHelp();
                return;
            }

            //Construct configs
            var configs = ConstructConfigs(configPath);

            //Do checkin
            if (enabledAuto)
            {
                var checkinState = CheckinAll(configs);
                if (checkinState == true)
                {
                    return;
                }
                else
                {
                    Environment.Exit(1);
                }
            }

            //Interactive mode
            InteractiveLoop(configs);
            GlobalConfig.SaveConfigs(configs, configPath);
        }
    }
}
