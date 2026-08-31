using System;
using System.Linq;
using System.IO;
using Microsoft.Extensions.Configuration;
using NextGen.Database;
using NextGen.Util;

namespace NextGen.Login
{
    class Program
    {
        internal static DatabaseManager DatabaseManager { get; set; }
        public static IConfiguration Configuration { get; private set; }

        static void Main(string[] args)
        {
            Log.Initialize("NextGen.Login");
            
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
            
            Console.Title = "NextGen.Login";
            if (Load())
            {
                Log.IsDebug = Settings.Instance.Debug;
                while (true)
                    Console.ReadLine();
            }
            else
            {
                Log.WriteLine(LogLevel.Error, "Could not start server. Press RETURN to exit.");
                Console.ReadLine();
            }
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Log.WriteLine(LogLevel.Exception, "Unhandled Exception : " + e.ToString());
            Console.ReadKey(true);
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
                Settings.Instance.Database.Server,
                (uint)Settings.Instance.Database.Port,
                Settings.Instance.Database.User,
                Settings.Instance.Database.Password,
                Settings.Instance.Database.Database,
                Settings.Instance.Database.MinPoolSize,
                Settings.Instance.Database.MaxPoolSize,
                Settings.Instance.Database.QueryCachePerClient,
                Settings.Instance.Database.OverloadFlags);
            
            DatabaseManager.GetClient();
            Log.IsDebug = Settings.Instance.Debug;

            if (Reflector.GetInitializerMethods().Any(method => !method.Invoke()))
            {
                Log.WriteLine(LogLevel.Error, "Server could not be started. Errors occured.");
                return false;
            }
            return true;
        }
    }
}
