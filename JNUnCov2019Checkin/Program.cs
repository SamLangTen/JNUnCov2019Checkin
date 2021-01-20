using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using JNUnCov2019Checkin.JNUModule.StuHealth;

namespace JNUnCov2019Checkin
{
    class Program
    {

        static async Task<bool> Checkin(Config config, string encryptionKey)
        {

            var stuhealth = new StuHealthModule();

            //Get bot name
            string botName = config.Username;

            try
            {
                string encryptedUsername = null;

                if (config.Password != null)
                {
                    //Login mode
                    var encryptedPassword = StuHealthModule.EncryptPassword(config.Password, encryptionKey);

                    encryptedUsername = await stuhealth.Login(config.Username, encryptedPassword);
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

        static bool CheckinAll(List<Config> configs)
        {
            if (configs == null || configs.Where(c => c.Enabled).Count() == 0) return true;
            try
            {
                var encryptionKey = "";
                Task.WaitAll(Task.Run(async () =>
                {
                    encryptionKey = await StuHealthModule.GetEncryptionKey();
                }));
                Console.WriteLine($"[{DateTime.Now.ToString()}] JNUnCov2019Checkin program has fetched the key");

                var checkTasks = configs.Where(c => c.Enabled).Select(c => Task.Run(() => Checkin(c, encryptionKey))).ToArray();
                Task.WaitAll(checkTasks);
                var successTaskCount = (from r in checkTasks where r.Result == true select r.Result).Count();

                if (successTaskCount != checkTasks.Length)
                    return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now.ToString()}] JNUnCov2019Checkin program got exception while fetching key, reason:{ex.Message}");
                return false;
            }
            return true;
        }

        static List<Config> ConstructConfigs(string configPath)
        {
            List<Config> configs = null;
            //Load config from file
            if (File.Exists(configPath))
                configs = Config.LoadConfigs(configPath);
            else
                configs = new List<Config>();

            //Load config from environment variable
            int envIndex = 1;
            while (true)
            {
                var envConfig = new Config
                {
                    Username = Environment.GetEnvironmentVariable($"JNUCHECKIN{envIndex}_USERNAME"),
                    Password = Environment.GetEnvironmentVariable($"JNUCHECKIN{envIndex}_PASSWORD"),
                    EncryptedUsername = Environment.GetEnvironmentVariable($"JNUCHECKIN{envIndex}_ENCRYPTED"),
                    Enabled = true
                };
                if ((envConfig.Username != null && envConfig.Password != null) || envConfig.EncryptedUsername != null)
                    configs.Add(envConfig);
                else
                    break;

                envIndex++;
            }
            Console.WriteLine("Load config successfully");
            return configs;
        }

        static void InteractiveLoop(List<Config> configs)
        {
            string encryptionKey = "";
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
                    configs.Add(config);
                }
                else if (option.StartsWith("checkin"))
                {
                    if (option == "checkin-all")
                    {
                        CheckinAll(configs);
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
                            var config = configs.FirstOrDefault(c => c.Username == subOptions[1]);
                            if (config != null)
                            {
                                Task.WaitAll(Checkin(config, encryptionKey));
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
            Config.SaveConfigs(configs, configPath);
        }
    }
}
