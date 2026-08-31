using Microsoft.Extensions.Configuration;
using NextGen.Util;

namespace NextGen.Zone
{
    public sealed class Settings
    {
        public const int SettingsVersion = 2;
        public int? Version { get; set; }
        public string IP { get; set; }
        public bool Debug { get; set; }
        public int WorkInterval { get; set; }
        public int TransferTimeout { get; set; }
        public string WorldServiceUri { get; set; }
        public string InterPassword { get; set; }
        public string WorldServerIP { get; set; }
        public ushort WorldServerPort { get; set; }
        public ushort InterServerPort { get; set; }
        public ulong TicksToSleep { get; set; }
        public int SleepTime { get; set; }
        public DatabaseConfig ZoneDatabase { get; set; }
        public DatabaseConfig WorldDatabase { get; set; }
        public string ConnString { get; set; }
        public string WorldConnString { get; set; }
        public string zoneMysqlDatabase { get { return ZoneDatabase?.Database; } }
        public static Settings Instance { get; set; }

        public static bool Load(IConfiguration config = null)
        {
            try
            {
                if (config != null)
                {
                    var section = config.GetSection("ZoneServer");
                    Instance = new Settings
                    {
                        IP = section.GetValue<string>("IP", "127.0.0.1"),
                        Debug = section.GetValue<bool>("Debug"),
                        WorkInterval = section.GetValue<int>("WorkInterval", 1),
                        TransferTimeout = section.GetValue<int>("TransferTimeout", 10),
                        InterPassword = section.GetValue<string>("InterPassword"),
                        WorldServerIP = section.GetValue<string>("WorldServerIP"),
                        WorldServerPort = (ushort)section.GetValue<int>("WorldServerPort"),
                        WorldServiceUri = section.GetValue<string>("WorldServiceUri"),
                        TicksToSleep = section.GetValue<ulong>("TicksToSleep", 10),
                        SleepTime = section.GetValue<int>("SleepTime", 1),
                        ZoneDatabase = section.GetSection("ZoneDatabase").Get<DatabaseConfig>(),
                        WorldDatabase = section.GetSection("WorldDatabase").Get<DatabaseConfig>(),
                        Version = SettingsVersion
                    };
                }
                else
                {
                    NextGen.InterLib.Settings.Initialize();
                    Instance = new Settings
                    {
                        WorldServerIP = NextGen.InterLib.Settings.GetString("Zone.WorldServerIP"),
                        WorldServerPort = (ushort)NextGen.InterLib.Settings.GetInt32("Zone.WorldServerPort"),
                        IP = NextGen.InterLib.Settings.GetString("Zone.IP"),
                        Debug = NextGen.InterLib.Settings.GetBool("Zone.Debug"),
                        WorkInterval = NextGen.InterLib.Settings.GetInt32("Zone.WorkInterval"),
                        TransferTimeout = NextGen.InterLib.Settings.GetInt32("Zone.TransferTimeout"),
                        WorldServiceUri = NextGen.InterLib.Settings.GetString("Zone.WorldServiceURI"),
                        InterPassword = NextGen.InterLib.Settings.GetString("Zone.Password"),
                        TicksToSleep = NextGen.InterLib.Settings.GetUInt32("Zone.TicksToSleep"),
                        SleepTime = NextGen.InterLib.Settings.GetInt32("Zone.SleepTime"),
                        ZoneDatabase = new DatabaseConfig
                        {
                            Server = NextGen.InterLib.Settings.GetString("Data.Mysql.Server"),
                            Port = NextGen.InterLib.Settings.GetInt32("Data.Mysql.Port"),
                            User = NextGen.InterLib.Settings.GetString("Data.Mysql.User"),
                            Password = NextGen.InterLib.Settings.GetString("Data.Mysql.Password"),
                            Database = NextGen.InterLib.Settings.GetString("Data.Mysql.Database"),
                            MinPoolSize = (uint)NextGen.InterLib.Settings.GetInt32("Data.Mysql.MinPool"),
                            MaxPoolSize = (uint)NextGen.InterLib.Settings.GetInt32("Data.Mysql.MaxPool"),
                            QueryCachePerClient = NextGen.InterLib.Settings.GetInt32("Data.Mysql.QuerCachePerClient"),
                            OverloadFlags = NextGen.InterLib.Settings.GetInt32("Data.Mysql.OverloadFlags")
                        },
                        WorldDatabase = new DatabaseConfig
                        {
                            Server = NextGen.InterLib.Settings.GetString("World.Mysql.Server"),
                            Port = NextGen.InterLib.Settings.GetInt32("World.Mysql.Port"),
                            User = NextGen.InterLib.Settings.GetString("World.Mysql.User"),
                            Password = NextGen.InterLib.Settings.GetString("World.Mysql.Password"),
                            Database = NextGen.InterLib.Settings.GetString("World.Mysql.Database"),
                            MinPoolSize = (uint)NextGen.InterLib.Settings.GetInt32("ZoneWorld.Mysql.MinPool"),
                            MaxPoolSize = (uint)NextGen.InterLib.Settings.GetInt32("ZoneWorld.Mysql.MaxPool"),
                            QueryCachePerClient = NextGen.InterLib.Settings.GetInt32("ZoneWorld.Mysql.QuerCachePerClient"),
                            OverloadFlags = NextGen.InterLib.Settings.GetInt32("ZoneWorld.Mysql.OverloadFlags")
                        },
                        Version = SettingsVersion
                    };
                }
                if (Instance.ZoneDatabase == null || Instance.WorldDatabase == null) return false;
                Instance.ConnString = Instance.ZoneDatabase.BuildConnectionString();
                Instance.WorldConnString = Instance.WorldDatabase.BuildConnectionString();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
