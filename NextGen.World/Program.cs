using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.IO;
using Microsoft.Extensions.Configuration;
using NextGen.Database;
using NextGen.Util;
using NextGen.World.InterServer;

namespace NextGen.World
{
    class Program
    {
        public static bool Maintenance { get; set; }
        private static bool HandleCommands = true;
        public static Database.DatabaseManager DatabaseManager { get; set; }
        public static DateTime CurrentTime { get; set; }
        public static ConcurrentDictionary<byte, ZoneConnection> Zones { get; private set; }
        public static IConfiguration Configuration { get; private set; }

        static void Main(string[] args)
        {
            Log.Initialize("NextGen.World");

            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
            Console.Title = "NextGen.World";
#if DEBUG
            Thread.Sleep(980);
#endif
            if (Load())
            {
                Log.IsDebug = Settings.Instance.Debug;
                Zones = new ConcurrentDictionary<byte, ZoneConnection>();

                while (HandleCommands)
                {
                    string line = Console.ReadLine();
                    try
                    {
                        HandleCommand(line);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(LogLevel.Exception, "Could not parse: {0}; Error: {1}", line, ex.ToString());
                    }
                }
                Log.WriteLine(LogLevel.Warn, "Shutting down the server..");
                CleanUp();
                Log.WriteLine(LogLevel.Info, "Server has been cleaned up. Program will now exit.");
            }
            else
            {
                Log.WriteLine(LogLevel.Error, "Errors occured starting server. Press RETURN to exit.");
                Console.ReadLine();
            }
        }

        private static void CleanUp()
        {
            foreach (var method in Reflector.GetCleanupMethods())
            {
                method();
            }
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Log.WriteLine(LogLevel.Exception, "Unhandled Exception : " + e);
            Console.ReadKey(true);
        }

        public static ZoneConnection GetZoneByMap(int id)
        {
            try
            {
                return Zones.Values.First(z => z.Maps.Count(m => m.ID == id) > 0);
            }
            catch
            {
                Log.WriteLine(LogLevel.Exception, "No zones are active at the moment.");
                return null;
            }
        }

        public static ZoneConnection GetZoneByMapShortName(string Name)
        {
            try
            {
                return Zones.Values.First(z => z.Maps.Count(m => m.ShortName == Name) > 0);
            }
            catch
            {
                Log.WriteLine(LogLevel.Exception, "No zones are active at the moment.");
                return null;
            }
        }

        public static void HandleCommand(string line)
        {
            string[] command = line.Split(' ');
            switch (command[0].ToLower())
            {
                case "maintenance":
                    if (command.Length >= 2)
                    {
                        Maintenance = bool.Parse(command[1]);
                    }
                    break;
                case "shutdown":
                case "exit":
                case "quit":
                    HandleCommands = false;
                    break;
                default:
                    Console.WriteLine("Command not recognized.");
                    break;
            }
        }

        public static bool Load()
        {
            if (!Settings.Load(Configuration))
            {
                Log.WriteLine(LogLevel.Error, "Failed to load settings from appsettings.json. Falling back to Config.cfg...");
                if (!Settings.Load(null))
                    return false;
            }

            DatabaseManager = new DatabaseManager(
                Settings.Instance.WorldDatabase.Server,
                (uint)Settings.Instance.WorldDatabase.Port,
                Settings.Instance.WorldDatabase.User,
                Settings.Instance.WorldDatabase.Password,
                Settings.Instance.WorldDatabase.Database,
                Settings.Instance.WorldDatabase.MinPoolSize,
                Settings.Instance.WorldDatabase.MaxPoolSize,
                Settings.Instance.WorldDatabase.QueryCachePerClient,
                Settings.Instance.WorldDatabase.OverloadFlags);

            try
            {
                if (Reflector.GetInitializerMethods().Any(method => !method.Invoke()))
                {
                    Log.WriteLine(LogLevel.Error, "Server could not be started. Errors occured.");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "Fatal exception while load: {0}:{1}", ex.ToString(), ex.StackTrace);
                return false;
            }
        }

        public static byte GetFreeZoneID()
        {
            for (byte i = 0; i < 3; i++)
            {
                if (Zones.ContainsKey(i)) continue;
                return i;
            }
            return 255;
        }
    }
}
