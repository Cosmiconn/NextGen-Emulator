using System.Text;
using NextGen.Zone.Data;

static class NativeObjectManagerTests
{
    // Independent literal ranges from HandleSplit, ordered by native type.
    static readonly (ushort First, int Count)[] Ranges = {
        (0x34BC,3584), (0x2904,3000), (0x1F40,1500), (0x251C,1000),
        (0x42BC,256), (0,8000), (0x4FA4,250), (0x509E,1000),
        (0x43BC,2048), (0x4BBC,1000), (0x5486,500), (0x567A,1000), (0x5C56,1500) };
    static void Check(bool ok, string message)
    { if (!ok) throw new Exception("native pool: " + message); }
    static KingdomQuestPineNativeObjectManager Manager(List<string> events) =>
        new(t => events.Add("invalid:" + t), (h,r) => events.Add($"missing:{h}:{r}"));
    static NativeObject[] Bind(KingdomQuestPineNativeObjectManager manager, ushort type, List<string> events)
    {
        var range = Ranges[type];
        var objects = Enumerable.Range(0, range.Count).Select(i => new NativeObject(manager, type, (ushort)(range.First + i), events)).ToArray();
        Check(manager.TryBindPool(type, objects), "bind full original pool " + type);
        return objects;
    }
    static NativeObject Allocate(KingdomQuestPineNativeObjectManager manager, ushort type, ushort expected)
    {
        ushort handle = 0xBEEF;
        Check(manager.TryAllocate(type, ref handle, out var obj) && obj != null && handle == expected, "allocation " + expected);
        return (NativeObject)obj;
    }
    public static void Run()
    {
        int valid = 0;
        for (int handle = 0; handle <= ushort.MaxValue; handle++)
        {
            int expected = Array.FindIndex(Ranges, r => handle >= r.First && handle < r.First + r.Count);
            bool found = KingdomQuestPineNativeObjectHandles.TrySplit((ushort)handle, out var type, out ushort index);
            Check(found == (expected >= 0), "complete u16 decode domain " + handle);
            if (found)
            {
                valid++;
                Check((int)type == expected && index == handle - Ranges[expected].First &&
                    KingdomQuestPineNativeObjectHandles.Encode((ushort)type, index) == handle, "source range and inverse " + handle);
            }
            else Check((byte)type == 255 && index == 65535, "native invalid sentinels");
        }
        Check(valid == 24638, "all thirteen ranges including real gaps");
        Check(!KingdomQuestPineNativeObjectHandles.TryGetRange(13, out _, out _) &&
            KingdomQuestPineNativeObjectHandles.Encode(65535, 0) == 65535, "invalid type");
        for (ushort type = 0; type < Ranges.Length; type++)
        {
            var range = Ranges[type];
            Check(KingdomQuestPineNativeObjectHandles.TryGetRange(type, out var start, out var capacity) &&
                start == range.First && capacity == range.Count, "original pool capacity");
            Check(KingdomQuestPineNativeObjectHandles.Encode(type, capacity) == 65535, "exclusive index bound");
            var events = new List<string>(); var manager = Manager(events); var objects = Bind(manager, type, events);
            Check(manager.TryGetObject(range.First, out var inactive) && inactive == null &&
                manager.TryGetObjectAbsolute(range.First, out var absolute) && ReferenceEquals(absolute, objects[0]), "absolute lookup before allocation");
            for (int i = 0; i < capacity; i++)
            {
                var obj = Allocate(manager, type, (ushort)(range.First + i));
                Check(ReferenceEquals(obj, objects[i]) && obj.InitCount == 1, "stable preconstructed slot");
            }
            ushort output = 0xBEEF;
            int eventsBefore = events.Count;
            Check(manager.TryAllocate(type, ref output, out var exhausted) && exhausted == null &&
                output == range.First + type && events.Count == eventsBefore, "native exhaustion still encodes unchanged type argument");
            Check(manager.TryGetAllocatedCount(type, out int count) && count == capacity, "full count unchanged by failure");
            objects[0].Payload = 123;
            Check(manager.TryFree(range.First, 0, 22, out bool freed) && freed &&
                manager.TryGetObject(range.First, out inactive) && inactive == null &&
                manager.TryGetObjectAbsolute(range.First, out absolute) && ReferenceEquals(absolute, objects[0]) &&
                objects[0].Payload == 123, "free occupancy without disposing/resetting slot");
            Check(manager.TryFree((ushort)(range.First + capacity - 1), 0, 31, out freed) && freed, "release a second full-pool slot");
            Check(ReferenceEquals(Allocate(manager, type, range.First), objects[0]) &&
                ReferenceEquals(Allocate(manager, type, (ushort)(range.First + capacity - 1)), objects[^1]), "full-pool FIFO reuse");
        }

        var trace = new List<string>(); var pool = Manager(trace);
        ushort reported = 0xBEEF;
        Check(!pool.TryAllocate(7, ref reported, out _) && reported == 0xBEEF &&
            !pool.TryGetObject(0x509E, out _) && !pool.TryGetObjectAbsolute(0x509E, out _) &&
            !pool.TryFree(0x509E, 0, 22, out _) && trace.Count == 0, "unbound pool is unresolved, not exhausted or missing");
        Check(pool.TryAllocate(13, ref reported, out var invalid) && invalid == null &&
            reported == 0xBEEF && trace.SequenceEqual(new[] { "invalid:13" }), "invalid-type branch leaves output untouched");
        Check(pool.TryGetObject(0x5A62, out invalid) && invalid == null &&
            pool.TryGetObjectAbsolute(65535, out invalid) && invalid == null, "invalid handle is a native miss even with unbound pools");
        trace.Clear();
        Check(pool.TryFree(65535, 1, 99, out bool nativeResult) && nativeResult && trace.Count == 0,
            "removeWhen one returns true without lookup or diagnostics");
        var doors = Bind(pool, 7, trace);
        Check(!pool.TryBindPool(7, doors), "cannot replace a Zone-wide bound pool");
        ushort actual = 0;
        doors[0].BeforeInitialize = () => Check(actual == 0x509E && pool.TryGetObject(actual, out var obj) &&
            ReferenceEquals(obj, doors[0]), "output and occupancy set BEFORE virtual Init");
        Check(pool.TryAllocate(7, ref actual, out var first) && ReferenceEquals(first, doors[0]), "real first door");
        doors[0].BeforeDetach = () => Check(pool.TryGetObject(0x509E, out var obj) &&
            ReferenceEquals(obj, first), "occupied during FreeFromLists");
        doors[0].BeforeFreeCallback = () => Check(pool.TryGetObject(0x509E, out var obj) &&
            ReferenceEquals(obj, first), "occupied during second virtual callback");
        trace.Clear();
        Check(pool.TryFree(actual, 1, 23, out nativeResult) && nativeResult && trace.Count == 0 &&
            pool.TryGetObject(actual, out var stillLive) && ReferenceEquals(stillLive, first), "removeWhen one leaves active object intact");
        Check(pool.TryFree(actual, -1, 22, out nativeResult) && nativeResult &&
            trace.SequenceEqual(new[] { "detach:20638", "callback:20638" }), "every removeWhen except one takes ordered immediate path");
        Check(pool.TryFree(actual, 0, 22, out nativeResult) && !nativeResult &&
            pool.TryFree(actual, 0, 22, out nativeResult) && !nativeResult &&
            pool.TryFree(actual, 0, 65558, out nativeResult) && !nativeResult &&
            pool.TryFree(actual, 0, 23, out nativeResult) && !nativeResult, "repeated native missing frees");
        Check(trace.Count(e => e == "missing:20638:22") == 1 && trace.Count(e => e == "missing:20638:23") == 1 &&
            trace.All(e => !e.EndsWith(":65558")), "dedup key truncates reason to high u16");
        Check(Allocate(pool, 7, 0x509F).NativeHandle == 0x509F, "freed slot goes behind still-unused slots");
        var saved = doors[10]; doors[10] = doors[11];
        Check(pool.TryGetObjectAbsolute(0x50A8, out var retained) && ReferenceEquals(retained, saved), "binding copies caller collection");
        var bad = Manager(new());
        Check(!bad.TryBindPool(7, new IKingdomQuestPineNativePoolObject[999]) &&
            !bad.TryBindPool(7, doors), "incomplete/misidentified pool rejected before binding");
        doors[10] = saved;
        Check(bad.TryBindPool(7, doors), "failed binding does not leave partial registration");

        TestTraversal();
        TestSourceLifecycle();
        Console.WriteLine("PASS: complete native handle space, thirteen pools/exhaustion, absolute/live lookup, FIFO reuse, free callbacks, traversal and Honeying source lifecycle");
    }

    static void TestTraversal()
    {
        var trace = new List<string>(); var manager = Manager(trace); Bind(manager, 9, trace);
        for (int i = 0; i < 3; i++) Allocate(manager, 9, (ushort)(0x4BBC + i));
        var visits = new List<ushort>();
        Check(manager.TryVisitAllocated(9, (obj, index) => {
            visits.Add(index); Check(manager.TryFree(obj.NativeHandle, 0, 5, out var freed) && freed, "free current in visit"); return true;
        }, out bool result) && result && visits.SequenceEqual(new ushort[] { 0,1,2 }) &&
            manager.TryGetAllocatedCount(9, out int count) && count == 0, "successor cached before removal of current slot");
        manager = Manager(trace); Bind(manager, 9, trace);
        for (int i = 0; i < 3; i++) Allocate(manager, 9, (ushort)(0x4BBC + i));
        visits.Clear();
        Check(manager.TryVisitAllocated(9, (obj, index) => {
            visits.Add(index); manager.TryFree(0x4BBD, 0, 5, out _); return true;
        }, out result) && !result && visits.SequenceEqual(new ushort[] { 0 }), "freed cached successor stops native loop");
        visits.Clear();
        Check(manager.TryVisitAllocated(9, (obj, index) => { visits.Add(index); return false; }, out result) &&
            !result && visits.SequenceEqual(new ushort[] { 0 }), "callback false stops iteration");
        visits.Clear();
        Check(manager.TryVisitAllocated(9, (obj, index) => {
            visits.Add(index); if (index == 0) Allocate(manager, 9, 0x4BBF); return true;
        }, out result) && result && visits.SequenceEqual(new ushort[] { 0,2,3 }), "new tail visible if predecessor has not been visited");
        visits.Clear();
        Check(manager.TryVisitAllocated(9, (obj, index) => {
            visits.Add(index); if (index == 3) Allocate(manager, 9, 0x4BC0); return true;
        }, out result) && result && visits.SequenceEqual(new ushort[] { 0,2,3 }), "append after cached sentinel waits until next visit");
    }

    static void TestSourceLifecycle()
    {
        var trace = new List<string>(); var manager = Manager(trace); Bind(manager, 7, trace); Bind(manager, 9, trace);
        var variables = new KingdomQuestPineVariableStack(); variables.TryPush("Door1", out var doorToken);
        variables.TryPush("EffDoor1", out var effectToken);
        Check(KingdomQuestHoneyingExternalPlanBuilder.TryBuild("KQ/Honeying", 12,
            "doorbuild Door1 \"KQ_SlimeGate\" 9860 6094  272 1000 \"Normal\".", out var doorSite) &&
            KingdomQuestHoneyingDoorBuildNativePlan.TryBuild(doorSite, out _), "door source");
        KingdomQuestHoneyingDoorBuildNativePlan.TryBuild(doorSite, out var doorPlan);
        var doorOwner = new DoorOwner(manager);
        Check(KingdomQuestPineDoorBuildRuntime.TryStep(doorPlan, variables, doorOwner, out bool done) && done &&
            doorToken.Text == "20638", "source door build writes real native pool handle");
        Check(KingdomQuestHoneyingExternalPlanBuilder.TryBuild("KQ/Honeying", 18,
            "effectobj EffDoor1 Door1 \"KQ_SlimeGate\" 3600000 1000.", out var effectSite) &&
            KingdomQuestHoneyingEffectNativePlan.TryBuild(effectSite, variables, out _), "effect source");
        KingdomQuestHoneyingEffectNativePlan.TryBuild(effectSite, variables, out var effectPlan);
        var effects = new EffectOwner(manager);
        Check(KingdomQuestPineEffectRuntime.TryStep(effectPlan, variables, effects, out done) && done &&
            effectToken.Text == "19388", "source effect resolves native parent and allocated handle");
        Check(manager.TryGetObject(0x4BBC, out var live), "live effect lookup");
        Check(KingdomQuestHoneyingExternalPlanBuilder.TryBuild("KQ/Honeying", 62, "vanish EffDoor1.", out var vanishSite) &&
            KingdomQuestHoneyingVanishNativePlan.TryBuild(vanishSite, variables, out _), "vanish source");
        KingdomQuestHoneyingVanishNativePlan.TryBuild(vanishSite, variables, out var vanishPlan);
        trace.Clear();
        Check(KingdomQuestPineVanishRuntime.TryStep(vanishPlan, new VanishOwner(manager), out done) && done &&
            manager.TryGetAllocatedCount(9, out int count) && count == 1 && trace.Count == 0, "source vanish leaves slot occupied until routine");
        Check(manager.TryVisitAllocated(9, (obj, _) => ((NativeObject)obj).Routine.TryStep((NativeObject)obj), out bool loop) && loop &&
            manager.TryGetAllocatedCount(9, out count) && count == 0, "real routine frees current pool slot during iteration");
        Check(trace.SequenceEqual(new[] { "unmark:0:1:3", "detach:19388", "callback:19388" }) &&
            manager.TryGetObject(0x4BBC, out var missing) && missing == null &&
            manager.TryGetObjectAbsolute(0x4BBC, out var absolute) && ReferenceEquals(absolute, live) &&
            manager.TryGetAllocatedCount(7, out count) && count == 1, "native delayed cleanup retains absolute identity and parent door");
        // A failed door marking calls Free twice: only the first can detach.
        doorOwner.FailMarking = true; doorToken.TrySetAscii("unchanged"); trace.Clear();
        Check(KingdomQuestPineDoorBuildRuntime.TryStep(doorPlan, variables, doorOwner, out done) && done &&
            doorToken.Text == "unchanged" && trace.SequenceEqual(new[] {
                "init:20639", "detach:20639", "callback:20639", "missing:20639:23" }), "door failure's second free is lookup miss, not double disposal");
    }

    sealed class NativeObject : IKingdomQuestPineNativePoolObject, IKingdomQuestPineRetreatObject, IKingdomQuestPineEffectRoutineOwner
    {
        readonly KingdomQuestPineNativeObjectManager manager;
        readonly List<string> trace;
        public KingdomQuestPineNativeObjectType NativeObjectType { get; }
        public ushort NativeHandle { get; }
        public int InitCount, Payload;
        public Action BeforeInitialize, BeforeDetach, BeforeFreeCallback;
        public KingdomQuestPineEffectRoutine Routine;
        public NativeObject(KingdomQuestPineNativeObjectManager manager, ushort type, ushort handle, List<string> trace)
        { this.manager = manager; NativeObjectType = (KingdomQuestPineNativeObjectType)type; NativeHandle = handle; this.trace = trace; }
        public void InitializeForAllocation() { trace.Add("init:" + NativeHandle); InitCount++; BeforeInitialize?.Invoke(); }
        public void FreeFromLists() { trace.Add("detach:" + NativeHandle); BeforeDetach?.Invoke(); }
        public void InvokeManagerFreeCallback() { trace.Add("callback:" + NativeHandle); BeforeFreeCallback?.Invoke(); }
        public void RetreatFromMap() { Routine.RetreatFromMap(); }
        public bool IsAvailable => true;
        public uint CurrentNativeTick => 0;
        public ushort Handle => NativeHandle;
        public KingdomQuestPineEffectLocation Location => new(null, 0, 0, 0, 0);
        public KingdomQuestPineEffectLocation ParentLocation => null;
        public void ReportOutOfBounds() => throw new Exception("unexpected bounds");
        public void MoveTo(int x, int y, int argument) => throw new Exception("unexpected move");
        public void Unmark(int a, int b, int reason) { trace.Add($"unmark:{a}:{b}:{reason}"); }
        public bool Free(ushort handle, int when, int reason)
        { Check(manager.TryFree(handle, when, reason, out bool result), "bound free service"); return result; }
        public bool HasRoutineScript => false;
        public void RunRoutineScript(ushort handle, KingdomQuestPineEffectMapIdentity map) => throw new Exception("unexpected Lua");
    }
    // Test map/marking services are deliberately explicit; these adapters
    // connect production command/routine/pool code without inventing live maps.
    sealed class DoorOwner(KingdomQuestPineNativeObjectManager manager) : IKingdomQuestPineDoorBuildOwner
    {
        public bool FailMarking;
        public bool IsAvailable => true;
        public object Allocate(byte type, out ushort handle)
        { handle = 65535; Check(manager.TryAllocate(type, ref handle, out var obj), "bound door pool"); return obj; }
        public KingdomQuestPineTokenValue ResolveDestination(KingdomQuestPineVariableStack v, string name) { v.TryFind(name, out var token); return token; }
        public ushort FindMobId(string index) => 1091;
        public byte[] GetCurrentMapName12() => new byte[12];
        public int Build(object obj, KingdomQuestPineDoorBuildRequest request) => FailMarking ? 3 : 0;
        public bool Free(ushort handle, int when, int reason)
        { Check(manager.TryFree(handle, when, reason, out bool result), "bound door free"); return result; }
        public void ReportFailure(KingdomQuestPineDoorBuildFailure failure, int nativeError) { }
    }
    sealed class EffectOwner(KingdomQuestPineNativeObjectManager manager) : IKingdomQuestPineEffectOwner
    {
        public bool IsAvailable => true;
        public KingdomQuestPineTokenValue ResolveDestination(KingdomQuestPineVariableStack v, string name) { v.TryFind(name, out var token); return token; }
        public object ResolveParent(byte[] token)
        { Check(Encoding.ASCII.GetString(token).TrimEnd('\0') == "20638", "actual source parent token"); manager.TryGetObject(0x509E, out var obj); return obj; }
        public object Allocate(byte type, out ushort handle)
        { handle = 65535; Check(manager.TryAllocate(type, ref handle, out var obj), "bound effect pool"); return obj; }
        public bool ParentHasMap(object parent) => true;
        public byte[] GetCurrentMapName12() => new byte[12];
        public int Blast(object obj, KingdomQuestPineEffectBlastRequest request) { ((NativeObject)obj).Routine = request.CreateRoutine(0, 0); return 0; }
        public bool Free(ushort handle, int when, int reason)
        { Check(manager.TryFree(handle, when, reason, out bool result), "bound effect free"); return result; }
        public void ReportAllocationFailure() => throw new Exception("unexpected exhaustion");
        public void ReportDestinationFailure() => throw new Exception("unexpected variable miss");
    }
    sealed class VanishOwner(KingdomQuestPineNativeObjectManager manager) : IKingdomQuestPineVanishOwner
    {
        public bool TryResolve(byte[] token, out IKingdomQuestPineRetreatObject obj)
        { Check(Encoding.ASCII.GetString(token).TrimEnd('\0') == "19388", "actual source effect token"); var resolved = manager.TryGetObjectAbsolute(0x4BBC, out var native); obj = (IKingdomQuestPineRetreatObject)native; return resolved; }
        public void ReportMissingObject() => throw new Exception("unexpected vanish miss");
    }
}
