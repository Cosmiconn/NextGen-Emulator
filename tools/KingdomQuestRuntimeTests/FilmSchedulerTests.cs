using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using NextGen.FiestaLib;
using NextGen.FiestaLib.Data;
using NextGen.Zone;
using NextGen.Zone.Data;
using NextGen.Zone.Game;

static class FilmSchedulerTests
{
    static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception("film scheduler: " + name);
    }
    static T Empty<T>()
    {
        var value = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        GC.SuppressFinalize(value);
        return value;
    }
    static void Set(object o, string name, object value) => o.GetType().GetProperty(name).SetValue(o, value);
    static void StaticSet(Type t, object value) => t.GetProperty("Instance").SetValue(null, value);

    public static void Run()
    {
        var oldData = DataProvider.Instance;
        var oldMaps = MapManager.Instance;
        var data = Empty<DataProvider>();
        var info = Empty<MapInfo>();
        Set(info, "ID", (ushort)126);
        Set(info, "ShortName", "KDUnHall");
        var map = Empty<Map>();
        Set(map, "MapInfo", info);
        Set(map, "InstanceID", (short)0);
        Set(map, "Objects", new ConcurrentDictionary<ushort, MapObject>());
        var map2 = Empty<Map>();
        Set(map2, "MapInfo", info);
        Set(map2, "InstanceID", (short)1);
        Set(map2, "Objects", new ConcurrentDictionary<ushort, MapObject>());
        Set(data, "MapsByID", new Dictionary<ushort, MapInfo> { [126] = info });
        Set(data, "Blocks", new Dictionary<ushort, BlockInfo>());
        var maps = new MapManager();
        maps.Maps[info] = new List<Map> { map, map2 };
        StaticSet(typeof(DataProvider), data);
        StaticSet(typeof(MapManager), maps);
        KingdomQuestZoneRuntimeRegistry.Clear();
        try
        {
            var definition = Definition(101);
            var definition2 = Definition(102);
            Check(KingdomQuestZoneRuntimeRegistry.TryMake(definition, 126, 0) ==
                KingdomQuestZoneMakeResult.Success, "MAKE first map");
            Check(KingdomQuestZoneRuntimeRegistry.TryMake(definition2, 126, 1) ==
                KingdomQuestZoneMakeResult.Success, "MAKE second instance");
            var scheduler = new KingdomQuestZonePineFilmScheduler(0);
            var door = new Doors(scheduler);
            var host = new RejectHost();
            var context = new KingdomQuestPineUsedCommandContext(null, null,
                new KingdomQuestPineLocalCommandState(), null);
            Check(!scheduler.TryStartStartedSession(map, 101, door, host, null, context, out _)
                && door.Calls == 0, "MADE does not play");
            Check(KingdomQuestZoneRuntimeRegistry.TryStart(definition,
                Array.Empty<KingdomQuestZoneJoinerInfo>()), "START");
            Check(!scheduler.TryStartStartedSession(map2, 101, door, host, null, context, out _)
                && door.Calls == 0, "map instance mismatch rejected before effects");
            Check(!scheduler.TryStartStartedSession(map, 101, null, host, null, context, out _),
                "missing door implementation cannot start");
            door.Succeeds = false;
            Check(!scheduler.TryStartStartedSession(map, 101, door, host, null, context, out _)
                && scheduler.Count == 0, "failed close creates no film");
            door.Succeeds = true;
            Check(scheduler.TryStartStartedSession(map, 101, door, host, null, context, out var film),
                "attach started film");
            Check(door.CountDuringClose == 0 && film.Runtime.FrameCount == 1,
                "close precedes play; no eager execution");
            scheduler.Step(0);
            Check(film.Runtime.FrameCount == 2 && !film.Runtime.Variables.TryFind("Wait", out _),
                "one structural step per worker iteration");
            for (int i = 0; i < 12; i++) scheduler.Step(0);
            Check(film.Runtime.Variables.TryFind("Wait", out var wait) && wait.Text == "" &&
                film.Status == KingdomQuestPineRuntimeStatus.Running, "live wait reached");
            var player = Empty<ZoneCharacter>();
            player.Map = map2;
            player.State = PlayerState.Normal;
            map2.Objects[1] = player;
            scheduler.Step(0);
            Check(wait.Text == "", "player in another instance does not release wait");
            player.Map = map;
            map2.Objects.TryRemove(1, out _);
            map.Objects[1] = player;
            scheduler.Step(0);
            Check(wait.Text == "1", "live player releases bound film");
            Check(KingdomQuestZoneRuntimeRegistry.TryDisjoin(101, 77), "roster update");
            scheduler.Step(0);
            Check(scheduler.Count == 1, "roster update preserves film identity");

            Check(KingdomQuestZoneRuntimeRegistry.TryStart(definition,
                Array.Empty<KingdomQuestZoneJoinerInfo>()), "repeat START identity");
            int oldFrames = film.Runtime.FrameCount;
            scheduler.Step(0);
            Check(scheduler.Count == 0 && film.Runtime.FrameCount == oldFrames,
                "stale generation never steps after restart");
            Check(scheduler.TryStartStartedSession(map, 101, door, host, null, context, out film),
                "restart attaches fresh process stack");
            var previous = film;
            door.Succeeds = false;
            Check(!scheduler.TryStartStartedSession(map, 101, door, host, null, context, out _)
                && scheduler.Count == 0 && door.CountDuringClose == 0,
                "native drop occurs before failing close");
            door.Succeeds = true;
            Check(scheduler.TryStartStartedSession(map, 101, door, host, null, context, out film),
                "new film after failed close");
            Check(!ReferenceEquals(previous.Runtime, film.Runtime), "no process-stack reuse");
            Check(KingdomQuestZoneRuntimeRegistry.TryStart(definition2,
                Array.Empty<KingdomQuestZoneJoinerInfo>()) &&
                scheduler.TryStartStartedSession(map2, 102, door, host, null, context, out var film2),
                "second instance plays independently");
            // First film will fault at its first missing gameplay owner. The
            // second stays in waitlogin (no player), and must still be stepped.
            for (uint i = 0; i < 80; i++) scheduler.Step(i * 500);
            Check(film.Status == KingdomQuestPineRuntimeStatus.Faulted && scheduler.Count == 1,
                "unresolved gameplay faults and removes only affected film");
            Check(KingdomQuestZoneRuntimeRegistry.TryGet(101, out var live) &&
                live.State == KingdomQuestZoneLifecycleState.Started,
                "film fault never synthesizes KQ success or END");
            Check(KingdomQuestZoneRuntimeRegistry.Remove(102), "DESTROY registry entry");
            scheduler.Step(40000);
            Check(scheduler.Count == 0, "destroyed film removed on next worker pass");
        }
        finally
        {
            KingdomQuestZoneRuntimeRegistry.Clear();
            StaticSet(typeof(DataProvider), oldData);
            StaticSet(typeof(MapManager), oldMaps);
        }
        Console.WriteLine("PASS: worker film cadence, live map binding, START order, restart/destroy cleanup, fail-closed owners");
    }

    static KingdomQuestProtocolInfo Definition(uint handle) => new()
    {
        Handle = handle, ScriptLanguage = "KQ/UnderHall", ScriptInitValue = "0",
        MapLink = new[] {
            new KingdomQuestMapProtocolInfo { MapBase = "KDUnHall", MapName = "KQTest" },
            new KingdomQuestMapProtocolInfo(), new KingdomQuestMapProtocolInfo(),
            new KingdomQuestMapProtocolInfo() }
    };

    sealed class Doors(KingdomQuestZonePineFilmScheduler scheduler) : IKingdomQuestPineMapStartOwner
    {
        public int Calls, CountDuringClose;
        public bool Succeeds = true;
        public bool TryCloseAllDoors(Map map)
        {
            Calls++;
            CountDuringClose = scheduler.Count;
            return Succeeds;
        }
    }
    sealed class RejectHost : IKingdomQuestPineRuntimeHost
    {
        public bool TryEvaluateCondition(string e, out int v) { v = 0; return false; }
        public bool TryResolveIdentifier(string e, out string v) { v = null; return false; }
        public bool TryCalculateExpression(string e, KingdomQuestPineTokenValue v) => false;
        public bool TryStepCommand(string c, int l, ref int s, out bool done) { done = false; return false; }
    }
}
