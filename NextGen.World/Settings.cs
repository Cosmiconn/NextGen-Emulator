using Microsoft.Extensions.Configuration;
using NextGen.Util;

namespace NextGen.World
{
    public sealed class Settings
    {
        public const int SettingsVersion = 2;
        public int? Version { get; set; }
        public string WorldName { get; set; }
        public byte ID { get; set; }
        public string IP { get; set; }
        public ushort Port { get; set; }
        public ushort ZoneBasePort { get; set; }
        public ushort ZoneCount { get; set; }
        public int TransferTimeout { get; set; }
        public bool Debug { get; set; }
        public int WorkInterval { get; set; }
        public string LoginServiceUri { get; set; }
        public string WorldServiceUri { get; set; }
        public string GameServiceUri { get; set; }
        public string InterPassword { get; set; }
        public string LoginServerIP { get; set; }
        public ushort LoginServerPort { get; set; }
        public ushort InterServerPort { get; set; }
        public bool ShowEquips { get; set; }
        public ulong TicksToSleep { get; set; }
        public int SleepTime { get; set; }
        public DatabaseConfig WorldDatabase { get; set; }
        public DatabaseConfig DataDatabase { get; set; }
        public string ConnString { get; set; }
        public string DataConnString { get; set; }
        public string zoneMysqlDatabase { get { return DataDatabase?.Database; } }
        public string WorldMysqlDatabase { get { return WorldDatabase?.Database; } }
        public static Settings Instance { get; set; }

        public static bool Load(IConfiguration config = null)
        {
            try
            {
                if (config != null)
                {
                    var section = config.GetSection("WorldServer");
                    Instance = new Settings
                    {
                        WorldName = section.GetValue<string>("Name", "NextGen"),
                        ID = section.GetValue<byte>("ID", 0),
                        IP = section.GetValue<string>("IP", "127.0.0.1"),
                        Port = (ushort)section.GetValue<int>("Port"),
                        ZoneBasePort = (ushort)section.GetValue<int>("ZoneBasePort"),
                        ZoneCount = (ushort)section.GetValue<int>("ZoneCount"),
                        Debug = section.GetValue<bool>("Debug"),
                        WorkInterval = section.GetValue<int>("WorkInterval", 1),
                        TransferTimeout = section.GetValue<int>("TransferTimeout", 10),
                        ShowEquips = section.GetValue<bool>("ShowEquips", true),
                        InterPassword = section.GetValue<string>("InterPassword"),
                        InterServerPort = (ushort)section.GetValue<int>("InterServerPort"),
                        LoginServerIP = section.GetValue<string>("LoginServerIP"),
                        LoginServerPort = (ushort)section.GetValue<int>("LoginServerPort"),
                        LoginServiceUri = section.GetValue<string>("LoginServiceUri"),
                        WorldServiceUri = section.GetValue<string>("WorldServiceUri"),
                        GameServiceUri = section.GetValue<string>("GameServiceUri"),
                        TicksToSleep = section.GetValue<ulong>("TicksToSleep", 10),
                        SleepTime = section.GetValue<int>("SleepTime", 1),
                        WorldDatabase = section.GetSection("WorldDatabase").Get<DatabaseConfig>(),
                        DataDatabase = section.GetSection("DataDatabase").Get<DatabaseConfig>(),
                        Version = SettingsVersion
                    };
                }
                else
                {
                    NextGen.InterLib.Settings.Initialize();
                    Instance = new Settings
                    {
                        Port = (ushort)NextGen.InterLib.Settings.GetInt32("World.Port"),
                        ZoneBasePort = (ushort)NextGen.InterLib.Settings.GetInt32("World.ZoneBase.Port"),
                        ZoneCount = (ushort)NextGen.InterLib.Settings.GetInt32("World.ZoneCount"),
                        IP = NextGen.InterLib.Settings.GetString("World.IP"),
                        Debug = NextGen.InterLib.Settings.GetBool("World.Debug"),
                        InterServerPort = (ushort)NextGen.InterLib.Settings.GetInt32("World.InterServerPort"),
                        WorkInterval = NextGen.InterLib.Settings.GetInt32("World.WorkInterval"),
                        TransferTimeout = NextGen.InterLib.Settings.GetInt32("World.TranferTimeout"),
                        LoginServerIP = NextGen.InterLib.Settings.GetString("World.LoginServer.IP"),
                        LoginServerPort = (ushort)NextGen.InterLib.Settings.GetInt32("World.LoginServer.Port"),
                        WorldName = NextGen.InterLib.Settings.GetString("World.Name"),
                        ID = NextGen.InterLib.Settings.GetByte("World.ID"),
                        ShowEquips = true,
                        LoginServiceUri = NextGen.InterLib.Settings.GetString("World.LoginServiceURI"),
                        WorldServiceUri = NextGen.InterLib.Settings.GetString("World.WorldServiceURI"),
                        GameServiceUri = NextGen.InterLib.Settings.GetString("World.GameServiceURI"),
                        InterPassword = NextGen.InterLib.Settings.GetString("World.InterPassword"),
                        TicksToSleep = NextGen.InterLib.Settings.GetUInt32("World.TicksToSleep"),
                        SleepTime = NextGen.InterLib.Settings.GetInt32("World.SleepTime"),
                        WorldDatabase = new DatabaseConfig
                        {
                            Server = NextGen.InterLib.Settings.GetString("World.Mysql.Server"),
                            Port = NextGen.InterLib.Settings.GetInt32("World.Mysql.Port"),
                            User = NextGen.InterLib.Settings.GetString("World.Mysql.User"),
                            Password = NextGen.InterLib.Settings.GetString("World.Mysql.Password"),
                            Database = NextGen.InterLib.Settings.GetString("World.Mysql.Database"),
                            MinPoolSize = NextGen.InterLib.Settings.GetUInt32("World.Mysql.MinPool"),
                            MaxPoolSize = NextGen.InterLib.Settings.GetUInt32("World.Mysql.MaxPool"),
                            QueryCachePerClient = NextGen.InterLib.Settings.GetInt32("World.Mysql.QuerCachePerClient"),
                            OverloadFlags = NextGen.InterLib.Settings.GetInt32("World.Mysql.OverloadFlags")
                        },
                        DataDatabase = new DatabaseConfig
                        {
                            Server = NextGen.InterLib.Settings.GetString("Data.Mysql.Server"),
                            Port = NextGen.InterLib.Settings.GetInt32("Data.Mysql.Port"),
                            User = NextGen.InterLib.Settings.GetString("Data.Mysql.User"),
                            Password = NextGen.InterLib.Settings.GetString("Data.Mysql.Password"),
                            Database = NextGen.InterLib.Settings.GetString("Data.Mysql.Database")
                        },
                        Version = SettingsVersion
                    };
                }
                if (Instance.WorldDatabase == null || Instance.DataDatabase == null) return false;
                Instance.ConnString = Instance.WorldDatabase.BuildConnectionString();
                Instance.DataConnString = Instance.DataDatabase.BuildConnectionString();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
