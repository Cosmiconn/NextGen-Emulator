namespace NextGen.World
{
    using System;
    using NextGen.Util;
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
        public string WorldMysqlServer { get; set; }
        public int WorldMysqlPort { get; set; }
        public string WorldMysqlUser { get; set; }
        public string WorldMysqlPassword { get; set; }
        public string WorldMysqlDatabase { get; set; }
        public string zoneMysqlServer { get; set; }
        public int zoneMysqlPort { get; set; }
        public string zoneMysqlUser { get; set; }
        public string zoneMysqlPassword { get; set; }
        public string zoneMysqlDatabase { get; set; }
        public bool ShowEquips { get; set; }
        public string ConnString { get; set; }
        public string DataConnString { get; set; }
        public static Settings Instance { get; set; }
        public uint WorldDBMinPoolSize { get; set; }
        public uint WorldDBMaxPoolSize { get; set; }
        public int OverloadFlags { get; set; }
        public int QuerCachePerClient { get; set; }
        public ulong TicksToSleep { get; set; }
        public int SleepTime { get; set; }
        public static bool Load()
        {
            try{
                Settings obj = new Settings()
                {
                    Port = (ushort)NextGen.InterLib.Settings.GetInt32("World.Port"),
                    ZoneBasePort = (ushort)NextGen.InterLib.Settings.GetInt32("World.ZoneBase.Port"),
                    ZoneCount = (ushort)NextGen.InterLib.Settings.GetInt32("World.ZoneCount"),
                    IP = NextGen.InterLib.Settings.GetString("World.IP"),
                    Debug = NextGen.InterLib.Settings.GetBool("World.Debug"),
                    InterServerPort = (ushort)NextGen.InterLib.Settings.GetInt32("World.InterServerPort"),//zone lisner port
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
                    WorldMysqlServer = NextGen.InterLib.Settings.GetString("World.Mysql.Server"),
                    WorldMysqlPort = NextGen.InterLib.Settings.GetInt32("World.Mysql.Port"),
                    WorldMysqlUser = NextGen.InterLib.Settings.GetString("World.Mysql.User"),
                    WorldMysqlPassword = NextGen.InterLib.Settings.GetString("World.Mysql.Password"),
                    WorldMysqlDatabase = NextGen.InterLib.Settings.GetString("World.Mysql.Database"),
                    zoneMysqlServer = NextGen.InterLib.Settings.GetString("Data.Mysql.Server"),
                    zoneMysqlPort = NextGen.InterLib.Settings.GetInt32("Data.Mysql.Port"),
                    zoneMysqlUser = NextGen.InterLib.Settings.GetString("Data.Mysql.User"),
                    zoneMysqlPassword = NextGen.InterLib.Settings.GetString("Data.Mysql.Password"),
                    zoneMysqlDatabase = NextGen.InterLib.Settings.GetString("Data.Mysql.Database"),
                    WorldDBMinPoolSize = NextGen.InterLib.Settings.GetUInt32("World.Mysql.MinPool"),
                    WorldDBMaxPoolSize = NextGen.InterLib.Settings.GetUInt32("World.Mysql.MaxPool"),
                    QuerCachePerClient = NextGen.InterLib.Settings.GetInt32("World.Mysql.QuerCachePerClient"),
                    OverloadFlags = NextGen.InterLib.Settings.GetInt32("World.Mysql.OverloadFlags"),
                    TicksToSleep = NextGen.InterLib.Settings.GetUInt32("World.TicksToSleep"),
                    SleepTime = NextGen.InterLib.Settings.GetInt32("World.SleepTime"),
                };
                obj.ConnString = " User ID=" + obj.WorldMysqlUser + ";Password=" + obj.WorldMysqlPassword + ";Host=" + obj.WorldMysqlServer + ";Port=" + obj.WorldMysqlPort + ";Database=" + obj.WorldMysqlDatabase + ";Protocol=TCP;Compress=false;Pooling=true;Min Pool Size=0;Max Pool Size=2000;Connection Lifetime=0;";
                obj.DataConnString = " User ID=" + obj.zoneMysqlUser + ";Password=" + obj.zoneMysqlPassword + ";Host=" + obj.zoneMysqlServer + ";Port=" + obj.zoneMysqlPort + ";Database=" + obj.zoneMysqlDatabase + ";Protocol=TCP;Compress=false;Pooling=true;Min Pool Size=0;Max Pool Size=2000;Connection Lifetime=0;";
                Settings.Instance = obj;
                return true;
            }
            catch (Exception ex)
            {
                // Vorher: bare "catch { return false; }", das jede Ursache
                // (typischerweise ein fehlender Key in Config.cfg) verschluckte.
                // Settings.Instance blieb dann null, und Program.Load() stuerzte
                // zwei Zeilen spaeter mit einer nichtssagenden
                // NullReferenceException ab, statt den echten Grund zu nennen.
                Log.WriteLine(LogLevel.Exception,
                    "Fehler beim Laden der World-Settings aus Config.cfg: {0}. " +
                    "Pruefe, ob alle World.*-Keys vorhanden sind (siehe SETUP.md).", ex);
                return false;
            }

        }
    }
}
