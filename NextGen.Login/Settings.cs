namespace NextGen.Login
{
    using System;
    using System.Collections.Generic;
    using NextGen.Util;
    public sealed class Settings
    {
        public const int SettingsVersion = 2;

        public int? Version { get; set; }
        public ushort Port { get; set; }
        public bool Debug { get; set; }
        public int WorkInterval { get; set; }
        public string LoginMysqlServer { get; set; }
        public int LoginMysqlPort { get; set; }
        public string LoginMysqlUser { get; set; }
        public string LoginMysqlPassword { get; set; }
        public string LoginMysqlDatabase { get; set; }
        public uint LoginDBMinPoolSize { get; set; }
        public uint LoginDBMaxPoolSize { get; set; }
        public string LoginServiceUri { get; set; }
        public int OverloadFlags { get; set; }
        public int QuerCachePerClient { get; set; }
        public string InterPassword { get; set; }
        public ushort InterServerPort { get; set; }
        public static Settings Instance { get; set; }
        public string ConnString { get; set; }

        // Leere Liste = jede Client-Version wird akzeptiert (Default,
        // rueckwaertskompatibel mit dem bisherigen Verhalten). Befuellt aus
        // Login.SupportedClientVersions ("Jahr:Version" kommagetrennt, z.B.
        // "2016:2,2017:3"). Siehe DOCUMENTATION.md.
        public List<(ushort Year, ushort Version)> SupportedClientVersions { get; set; }

        public static bool Load()
        {
      try
      {
            Settings obj = new Settings()
            {
                InterServerPort = (ushort)NextGen.InterLib.Settings.GetInt32("Login.InterServerPort"),
                Port = (ushort)NextGen.InterLib.Settings.GetInt32("Login.Port"),
                Debug = NextGen.InterLib.Settings.GetBool("Login.Debug"),
                WorkInterval = NextGen.InterLib.Settings.GetInt32("Login.WorkInterVal"),
                LoginServiceUri = NextGen.InterLib.Settings.GetString("Login.LoginServiceURI"),
                InterPassword =  NextGen.InterLib.Settings.GetString("Login.InterPassword"),
                LoginMysqlServer = NextGen.InterLib.Settings.GetString("Login.Mysql.Server"),
                LoginMysqlPort = NextGen.InterLib.Settings.GetInt32("Login.Mysql.Port"),
                LoginMysqlUser = NextGen.InterLib.Settings.GetString("Login.Mysql.User"),
                LoginMysqlPassword = NextGen.InterLib.Settings.GetString("Login.Mysql.Password"),
                LoginMysqlDatabase = NextGen.InterLib.Settings.GetString("Login.Mysql.Database"),
                LoginDBMinPoolSize = NextGen.InterLib.Settings.GetUInt32("Login.Mysql.MinPool"),
                LoginDBMaxPoolSize = NextGen.InterLib.Settings.GetUInt32("Login.Mysql.MaxPool"),
                QuerCachePerClient = NextGen.InterLib.Settings.GetInt32("Login.Mysql.QuerCachePerClient"),
                OverloadFlags = NextGen.InterLib.Settings.GetInt32("Login.Mysql.OverloadFlags"),
               
                Version = SettingsVersion,
            };
                obj.ConnString =  " User ID="+obj.LoginMysqlUser+";Password="+obj.LoginMysqlPassword+";Host="+obj.LoginMysqlServer+";Port="+obj.LoginMysqlPort+";Database="+obj.LoginMysqlDatabase+";Protocol=TCP;Compress=false;Pooling=true;Min Pool Size="+obj.LoginDBMinPoolSize+";Max Pool Size="+obj.LoginDBMaxPoolSize+";Connection Lifetime=0;";

                obj.SupportedClientVersions = new List<(ushort, ushort)>();
                string raw;
                if (NextGen.InterLib.Settings.TryGetString("Login.SupportedClientVersions", out raw) && !string.IsNullOrWhiteSpace(raw))
                {
                    foreach (var pair in raw.Split(','))
                    {
                        var parts = pair.Trim().Split(':');
                        ushort year, version;
                        if (parts.Length == 2 && ushort.TryParse(parts[0], out year) && ushort.TryParse(parts[1], out version))
                        {
                            obj.SupportedClientVersions.Add((year, version));
                        }
                        else
                        {
                            Log.WriteLine(LogLevel.Warn, "Ungueltiger Eintrag in Login.SupportedClientVersions ignoriert: '{0}'", pair);
                        }
                    }
                }

                Settings.Instance = obj;
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception,
                    "Fehler beim Laden der Login-Settings aus Config.cfg: {0}. " +
                    "Pruefe, ob alle Login.*-Keys vorhanden sind (siehe SETUP.md).", ex);
                return false;
            }

        }
    }
}
