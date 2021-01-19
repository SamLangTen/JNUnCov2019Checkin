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
        static List<Config> Configs { get; set; }

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
                    Console.WriteLine($"[{DateTime.Now.ToString()}] Check-in bot #{botName} found today's check-in, bot will not check-in again");
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


        static void Main(string[] args)
        {
            //Load config from file
            string configPath = "./config.json";
            if (File.Exists(configPath))
                Configs = Config.LoadConfigs(configPath);
            else
                Configs = new List<Config>();

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
                    Configs.Add(envConfig);
                else
                    break;

                envIndex++;
            }


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

                    var checkTasks = Configs.Where(c => c.Enabled).Select(c => Task.Run(() => Checkin(c, encryptionKey))).ToArray();
                    Task.WaitAll(checkTasks);
                    var successTaskCount = (from r in checkTasks where r.Result == true select r.Result).Count();

                    if (successTaskCount != checkTasks.Length)
                        Environment.Exit(1);

                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now.ToString()}] JNUnCov2019Checkin program got exception while fetching key, reason:{ex.Message}");
                    Environment.Exit(1);
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
