namespace NextGen.Zone
{
    using System;
    using NextGen.Util;
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
        public string zoneMysqlServer { get; set; }
        public int zoneMysqlPort { get; set; }
        public string zoneMysqlUser { get; set; }
        public string zoneMysqlPassword { get; set; }
        public string zoneMysqlDatabase { get; set; }
        public string WorldMysqlServer { get; set; }
        public int WorldMysqlPort { get; set; }
        public string WorldMysqlUser { get; set; }
        public string WorldMysqlPassword { get; set; }
        public string WorldMysqlDatabase { get; set; }
        public uint WorldDBMinPoolSizeZoneWorld { get; set; }
        public uint WorldDBMaxPoolSizeZoneWorld { get; set; }
        public static Settings Instance { get; set; }
        public string ConnString { get; set; }
        public string WorldConnString { get; set; }
        public uint ZoneDBMinPoolSize { get; set; }
        public uint ZoneDBMaxPoolSize { get; set; }
        public int OverloadFlags { get; set; }
        public int QuerCachePerClient { get; set; }
        public int OverloadFlagsZoneWorld { get; set; }
        public int QuerCachePerClientZoneWorld { get; set; }
        public ulong TicksToSleep { get; set; }
        public int SleepTime { get; set; }
        // Optional, siehe Buff.cs (PeriodicInterval) - Default 1000ms falls
        // nicht gesetzt.
        public int PeriodicBuffTickMs { get; set; }

        public static bool Load()
        {
            try
            {
                Settings obj = new Settings()
                {
                    // V.1
                    WorldServerIP = NextGen.InterLib.Settings.GetString("Zone.WorldServerIP"),
                    WorldServerPort = (ushort)NextGen.InterLib.Settings.GetInt32("Zone.WorldServerPort"),
                    IP = NextGen.InterLib.Settings.GetString("Zone.IP"),
                    Debug = NextGen.InterLib.Settings.GetBool("Zone.Debug"),

                    WorkInterval = NextGen.InterLib.Settings.GetInt32("Zone.WorkInterval"),
                    TransferTimeout = NextGen.InterLib.Settings.GetInt32("Zone.TransferTimeout"),

                    WorldServiceUri = NextGen.InterLib.Settings.GetString("Zone.WorldServiceURI"),
                    InterPassword = NextGen.InterLib.Settings.GetString("Zone.Password"),
                    zoneMysqlServer = NextGen.InterLib.Settings.GetString("Data.Mysql.Server"),
                    zoneMysqlPort = NextGen.InterLib.Settings.GetInt32("Data.Mysql.Port"),
                    zoneMysqlUser = NextGen.InterLib.Settings.GetString("Data.Mysql.User"),
                    zoneMysqlPassword = NextGen.InterLib.Settings.GetString("Data.Mysql.Password"),
                    zoneMysqlDatabase = NextGen.InterLib.Settings.GetString("Data.Mysql.Database"),
                    WorldMysqlServer = NextGen.InterLib.Settings.GetString("World.Mysql.Server"),
                    ZoneDBMinPoolSize = (uint)NextGen.InterLib.Settings.GetInt32("Data.Mysql.MinPool"),
                    ZoneDBMaxPoolSize = (uint)NextGen.InterLib.Settings.GetInt32("Data.Mysql.MaxPool"),
                    WorldMysqlPort = NextGen.InterLib.Settings.GetInt32("World.Mysql.Port"),
                    WorldMysqlUser = NextGen.InterLib.Settings.GetString("World.Mysql.User"),
                    WorldMysqlPassword = NextGen.InterLib.Settings.GetString("World.Mysql.Password"),
                    WorldMysqlDatabase = NextGen.InterLib.Settings.GetString("World.Mysql.Database"),
                    QuerCachePerClientZoneWorld = NextGen.InterLib.Settings.GetInt32("ZoneWorld.Mysql.QuerCachePerClient"),
                    OverloadFlagsZoneWorld = NextGen.InterLib.Settings.GetInt32("ZoneWorld.Mysql.OverloadFlags"),
                    QuerCachePerClient = NextGen.InterLib.Settings.GetInt32("Data.Mysql.QuerCachePerClient"),
                    OverloadFlags = NextGen.InterLib.Settings.GetInt32("Data.Mysql.OverloadFlags"),
                    WorldDBMinPoolSizeZoneWorld = (uint)NextGen.InterLib.Settings.GetInt32("ZoneWorld.Mysql.MinPool"),
                    WorldDBMaxPoolSizeZoneWorld = (uint)NextGen.InterLib.Settings.GetInt32("ZoneWorld.Mysql.MaxPool"),
                    TicksToSleep = NextGen.InterLib.Settings.GetUInt32("Zone.TicksToSleep"),
                    SleepTime = NextGen.InterLib.Settings.GetInt32("Zone.SleepTime"),
                };
                {
                    string raw;
                    obj.PeriodicBuffTickMs = (NextGen.InterLib.Settings.TryGetString("Zone.PeriodicBuffTickMs", out raw) && int.TryParse(raw, out var ms) && ms > 0)
                        ? ms : 1000;
                }
                obj.WorldConnString = " User ID=" + obj.WorldMysqlUser + ";Password=" + obj.WorldMysqlPassword + ";Host=" + obj.WorldMysqlServer + ";Port=" + obj.WorldMysqlPort + ";Database=" + obj.WorldMysqlDatabase + ";Protocol=TCP;Compress=false;Pooling=true;Min Pool Size=0;Max Pool Size=2000;Connection Lifetime=0;";
                obj.ConnString = " User ID=" + obj.zoneMysqlUser + ";Password=" + obj.zoneMysqlPassword + ";Host=" + obj.zoneMysqlServer + ";Port=" + obj.zoneMysqlPort + ";Database=" + obj.zoneMysqlDatabase + ";Protocol=TCP;Compress=false;Pooling=true;Min Pool Size=0;Max Pool Size=2000;Connection Lifetime=0;";
                Settings.Instance = obj;
                return true;
            }
            catch (Exception ex)
            {
                // Siehe Kommentar im World-Pendant: vorher "catch { return false; }",
                // das die eigentliche Ursache (i.d.R. ein fehlender Key in
                // Config.cfg) verschluckte statt sie zu loggen.
                Log.WriteLine(LogLevel.Exception,
                    "Fehler beim Laden der Zone-Settings aus Config.cfg: {0}. " +
                    "Pruefe, ob alle Zone.*-Keys vorhanden sind (siehe SETUP.md).", ex);
                return false;
            }

        }
    }
}
