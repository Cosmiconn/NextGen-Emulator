using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.IO;
using Microsoft.Extensions.Configuration;
using NextGen.Database;
using NextGen.FiestaLib.Data;
using NextGen.Util;
using NextGen.Zone.InterServer;
using NextGen.Zone.Networking;

namespace NextGen.Zone
{
    class Program
    {
        public static ZoneData ServiceInfo { get { return Zones[0]; } set { Zones[0] = value; } }
        public static ConcurrentDictionary<byte, ZoneData> Zones { get; set; }
        public static Random Randomizer { get; set; }
        public static DateTime CurrentTime { get; set; }
        public static bool Shutdown { get; private set; }
        public static DatabaseManager DatabaseManager;
        public static DatabaseManager CharDBManager;
        public static IConfiguration Configuration { get; private set; }

        static void Main(string[] args)
        {
            Log.Initialize("NextGen.Zone");

            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += MyHandler;
            Console.Title = "NextGen.Zone[Registering]";
#if DEBUG
            Thread.Sleep(TimeSpan.FromSeconds(3));
#endif
            Zones = new ConcurrentDictionary<byte, ZoneData>();
            Zones.TryAdd(0, new ZoneData());
            if (Load())
            {
                Worker.Load();
                Worker.Instance.AddCallback(GroupManager.Instance.Update);
                while (true)
                {
                    string cmd = Console.ReadLine();
                    string[] arguments = cmd.Split(' ');
                    switch (arguments[0])
                    {
                        case "shutdown":
                            Shutdown = true;
                            Log.WriteLine(LogLevel.Info, "Disconnecting from world.");
                            WorldConnector.Instance.Disconnect();
                            Log.WriteLine(LogLevel.Info, "Stopping client acceptor");
                            ZoneAcceptor.Instance.Stop();
                            Log.WriteLine(LogLevel.Info, "Stopping worker thread");
                            Worker.Instance.Stop();
                            Log.WriteLine(LogLevel.Info, "Disconnecting all clients");
                            ClientManager.Instance.DisconnectAll();
                            Log.WriteLine(LogLevel.Info, "Saving everything a last time");
                            Log.WriteLine(LogLevel.Info, "Bay.");
                            Environment.Exit(1);
                            break;
                    }
                }
            }
            else
            {
                Console.WriteLine("There was an error during load. Please press RETURN to exit.");
                Console.ReadLine();
            }
        }

        private static bool Load()
        {
            if (!Settings.Load(Configuration))
            {
                Log.WriteLine(LogLevel.Error, "Failed to load settings from appsettings.json. Falling back to Config.cfg...");
                if (!Settings.Load(null))
                    return false;
            }

            DatabaseManager = new DatabaseManager(
                Settings.Instance.ZoneDatabase.Server,
                (uint)Settings.Instance.ZoneDatabase.Port,
                Settings.Instance.ZoneDatabase.User,
                Settings.Instance.ZoneDatabase.Password,
                Settings.Instance.ZoneDatabase.Database,
                Settings.Instance.ZoneDatabase.MinPoolSize,
                Settings.Instance.ZoneDatabase.MaxPoolSize,
                Settings.Instance.ZoneDatabase.QueryCachePerClient,
                Settings.Instance.ZoneDatabase.OverloadFlags);
            DatabaseManager.GetClient();

            CharDBManager = new DatabaseManager(
                Settings.Instance.WorldDatabase.Server,
                (uint)Settings.Instance.WorldDatabase.Port,
                Settings.Instance.WorldDatabase.User,
                Settings.Instance.WorldDatabase.Password,
                Settings.Instance.WorldDatabase.Database,
                Settings.Instance.WorldDatabase.MinPoolSize,
                Settings.Instance.WorldDatabase.MaxPoolSize,
                Settings.Instance.WorldDatabase.QueryCachePerClient,
                Settings.Instance.WorldDatabase.OverloadFlags);
            CharDBManager.GetClient();

            Randomizer = new Random();
            Log.IsDebug = Settings.Instance.Debug;

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
                Log.WriteLine(LogLevel.Exception, "Error loading Initializer methods: {0}", ex.ToString());
                return false;
            }
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Log.WriteLine(LogLevel.Exception, "Unhandled Exception : " + e.ToString());
            Console.ReadKey(true);
        }

        public static ZoneData GetZoneForMap(ushort mapid)
        {
            foreach (var v in Zones.Values)
            {
                if (v.MapsToLoad.Count(m => m.ID == mapid) > 0) return v;
            }
            return null;
        }

        public static MapInfo GetMapInfo(ushort mapid)
        {
            foreach (var v in Zones.Values)
            {
                MapInfo mi = v.MapsToLoad.Find(m => m.ID == mapid);
                if (mi != null) return mi;
            }
            return null;
        }

        public static bool IsLoaded(ushort mapid)
        {
            try
            {
                return ServiceInfo.MapsToLoad.Count(m => m.ID == mapid) > 0;
            }
            catch { return false; }
        }
    }
}
