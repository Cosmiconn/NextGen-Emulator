using Microsoft.Extensions.Configuration;
using NextGen.Util;

namespace NextGen.Login
{
    public sealed class Settings
    {
        public const int SettingsVersion = 2;
        public int? Version { get; set; }
        public ushort Port { get; set; }
        public bool Debug { get; set; }
        public int WorkInterval { get; set; }
        public string LoginServiceUri { get; set; }
        public string InterPassword { get; set; }
        public ushort InterServerPort { get; set; }
        public DatabaseConfig Database { get; set; }
        public string ConnString { get; set; }
        public static Settings Instance { get; set; }

        public static bool Load(IConfiguration config = null)
        {
            try
            {
                if (config != null)
                {
                    var section = config.GetSection("LoginServer");
                    Instance = new Settings
                    {
                        Port = (ushort)section.GetValue<int>("Port"),
                        Debug = section.GetValue<bool>("Debug"),
                        WorkInterval = section.GetValue<int>("WorkInterval", 1),
                        LoginServiceUri = section.GetValue<string>("LoginServiceUri"),
                        InterPassword = section.GetValue<string>("InterPassword"),
                        InterServerPort = (ushort)section.GetValue<int>("InterServerPort"),
                        Database = section.GetSection("Database").Get<DatabaseConfig>(),
                        Version = SettingsVersion
                    };
                }
                else
                {
                    NextGen.InterLib.Settings.Initialize();
                    Instance = new Settings
                    {
                        InterServerPort = (ushort)NextGen.InterLib.Settings.GetInt32("Login.InterServerPort"),
                        Port = (ushort)NextGen.InterLib.Settings.GetInt32("Login.Port"),
                        Debug = NextGen.InterLib.Settings.GetBool("Login.Debug"),
                        WorkInterval = NextGen.InterLib.Settings.GetInt32("Login.WorkInterVal"),
                        LoginServiceUri = NextGen.InterLib.Settings.GetString("Login.LoginServiceURI"),
                        InterPassword = NextGen.InterLib.Settings.GetString("Login.InterPassword"),
                        Database = new DatabaseConfig
                        {
                            Server = NextGen.InterLib.Settings.GetString("Login.Mysql.Server"),
                            Port = NextGen.InterLib.Settings.GetInt32("Login.Mysql.Port"),
                            User = NextGen.InterLib.Settings.GetString("Login.Mysql.User"),
                            Password = NextGen.InterLib.Settings.GetString("Login.Mysql.Password"),
                            Database = NextGen.InterLib.Settings.GetString("Login.Mysql.Database"),
                            MinPoolSize = NextGen.InterLib.Settings.GetUInt32("Login.Mysql.MinPool"),
                            MaxPoolSize = NextGen.InterLib.Settings.GetUInt32("Login.Mysql.MaxPool"),
                            QueryCachePerClient = NextGen.InterLib.Settings.GetInt32("Login.Mysql.QuerCachePerClient"),
                            OverloadFlags = NextGen.InterLib.Settings.GetInt32("Login.Mysql.OverloadFlags")
                        },
                        Version = SettingsVersion
                    };
                }
                if (Instance.Database == null) return false;
                Instance.ConnString = Instance.Database.BuildConnectionString();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
