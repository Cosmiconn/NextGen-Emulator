using System.Text;
using NextGen.Zone.Data;

static class NativeMapAxesTests
{
    static void Check(bool ok, string message)
    { if (!ok) throw new Exception("native map axes: " + message); }
    static byte[] Name(string value)
    { var result = new byte[12]; Encoding.ASCII.GetBytes(value).CopyTo(result, 0); return result; }
    static KingdomQuestHoneyingExternalPlan Site(int line, string text)
    {
        Check(KingdomQuestHoneyingExternalPlanBuilder.TryBuild("KQ/Honeying", line, text, out var plan), "source site");
        return plan;
    }
    static object[] Objects(KingdomQuestPineNativeFieldMapAxes map, bool y = false) =>
        map.SnapshotAxis(y).Where(o => o is KingdomQuestPineNativeSceneObject).ToArray();

    public static void Run()
    {
        Nodes();
        var f = new Fixture();
        Check(!f.Doors[0].HasNativeAxisCounters, "pre-marking counters are unavailable, not invented zeroes");
        var map = f.AddMap(Name("KDHoneying"), 20000, 40000);
        var other = f.AddMap(Name("OTHER"), 20000, 40000);
        f.Bind(map, other);
        var flags = map.SnapshotAxis(false).Cast<KingdomQuestPineNativeAxialFlag>().ToArray();
        Check(flags.Select(o => o.X).SequenceEqual(new[] { 0, 2000, 6000, 10000, 14000, 18000, 0xFFFFFF }) &&
            flags.Select(o => o.Y).SequenceEqual(new[] { 0, 4000, 8000, 12000, 16000, 20000, 0xFFFFFF }),
            "rectangular maps increment BOTH axes by XLimit/5 after separate half-step starts");
        Check(map.IsReady && flags[0].RangeObject == null && flags[^1].RangeObject == null &&
            flags[1..^1].All(o => ReferenceEquals(o.RangeObject, o) && o.Map == map.Identity && o.Direction == 0),
            "endpoint and intermediate range-object state");
        Check(flags.All(o => f.Manager.TryGetObject(o.NativeHandle, out var active) && ReferenceEquals(active, o)),
            "seven actual occupied native type-zero slots per map");

        var door = f.BuildDoor();
        Check(door.Map == map.Identity && door.IsMarked && Objects(map).SequenceEqual(new[] { door }) &&
            Objects(map, true).SequenceEqual(new[] { door }) && Objects(other).Length == 0, "source Build inserts both axes of selected map only");
        Check(f.CounterReads == 2 && door.AxisCounters.A == uint.MaxValue && door.AxisCounters.B == 1 &&
            door.AxisCounters.C == 9 && door.AxisCounters.D == uint.MaxValue - 1, "two separate counter snapshots, each DWORD decremented with wrap");
        var effect = f.Blast(door);
        Check(effect.Map == map.Identity && effect.IsMarked && Objects(map).SequenceEqual(new object[] { door, effect }) &&
            Objects(map, true).SequenceEqual(new object[] { effect, door }),
            "equal-coordinate order depends on search anchor: X searches left to door, Y stops with door as successor");

        var login = effect.SnapshotLoginLocation21(); byte direction = effect.Location.Direction;
        effect.SetNativeMode(7, 0xA0);
        Check(f.Registry.TryMoveTo(effect, effect.Location.X, effect.Location.Y, 123) &&
            Objects(map).SequenceEqual(new object[] { door, effect }) && Objects(map, true).SequenceEqual(new object[] { door, effect }),
            "MoveTo equality reinserts: X retains order while Y moves after cached equal successor");
        Check(f.Registry.TryMoveTo(effect, 100, 39000, 0) &&
            Objects(map).SequenceEqual(new object[] { effect, door }) && Objects(map, true).SequenceEqual(new object[] { door, effect }) &&
            effect.Mode == 7 && effect.ModeFlags == 0xA0 && effect.Location.Direction == direction &&
            effect.SnapshotLoginLocation21().SequenceEqual(login), "movement independently reorders X/Y and preserves login/direction/mode");
        foreach (var (x, y) in new[] { (-1, 1), (1, -1), (20000, 1), (1, 40000) })
            Check(f.Registry.TryMoveTo(effect, x, y, 5) && effect.Location.X == 100 && effect.Location.Y == 39000,
                "signed exclusive MoveTo bounds");
        Check(!f.Registry.TryUnmark(effect, 0, 1, 3, null) && effect.IsMarked && !effect.Axes.X.IsDetached,
            "unavailable requested logout callback rejects before mutations");
        bool broadcast = false;
        Check(f.Registry.TryUnmark(effect, 77, 2, 254, o => {
            Check(o.IsMarked && Objects(map).Contains(o) && Objects(map, true).Contains(o), "logout sees marked object in both lists");
            broadcast = true;
        }) && broadcast && !effect.IsMarked && effect.Axes.X.IsDetached && effect.Axes.Y.IsDetached &&
            effect.Map == map.Identity && effect.Mode == 7 && effect.Parent == door && effect.SnapshotLoginLocation21().SequenceEqual(login),
            "Unmark broadcasts before detach, clears only marked state and ignores RemoveWhen/reason");
        Check(f.Registry.TryUnmark(effect, 1, 0, 99, null), "already detached unmark without broadcast");

        // A detached object still retains its map. Failed relocation updates XY
        // and logs for each axis; the effect's relink virtual is a no-op.
        f.Errors.Clear();
        Check(f.Registry.TryMoveTo(effect, 800, 900, 5) && effect.Location.X == 800 && effect.Location.Y == 900 &&
            f.Errors.SequenceEqual(new[] { KingdomQuestPineAxisError.RelocateDetachFailure, KingdomQuestPineAxisError.SaveFieldInformation,
                KingdomQuestPineAxisError.DumpMap, KingdomQuestPineAxisError.RelocateDetachFailure,
                KingdomQuestPineAxisError.SaveFieldInformation, KingdomQuestPineAxisError.DumpMap }), "detached effect relocation failure sequence");
        Check(f.Registry.TryMark(effect, 1, out int err) && err == 0, "remark detached effect using retained login map");
        effect.SetNativeMode(0, 0);
        Check(f.Registry.TryMoveTo(door, 12000, 15000, 5), "move actual parent in actual lists");
        f.Clock = 100; f.Script = true; effect.RetreatFromMap(); f.Events.Clear();
        Check(effect.TryRunRoutine() && f.Events.SequenceEqual(new[] { "move", "logout", "free-lists", "script-check", "lua" }),
            "source effect routine follows, broadcasts logout, unlinks, frees, then invokes script");
        Check(effect.Location.X == 12000 && effect.Location.Y == 15000 && !effect.IsMarked &&
            Objects(map).SequenceEqual(new object[] { door }) && Objects(map, true).SequenceEqual(new object[] { door }) &&
            f.Manager.TryGetObject(effect.NativeHandle, out var absent) && absent == null &&
            f.Manager.TryGetObjectAbsolute(effect.NativeHandle, out var retained) && ReferenceEquals(retained, effect),
            "vanish removes actual pool occupancy and axis nodes while preserving slot identity");

        MarkingEdges();
        Console.WriteLine("PASS: native two-axis lists/flags, exact map lookup, marking error mutations, movement/reinsertion, logout ordering and real pooled effect cleanup");
    }

    static void Nodes()
    {
        var errors = new List<KingdomQuestPineAxisError>();
        void Report(KingdomQuestPineAxisError e, object _) => errors.Add(e);
        KingdomQuestPineNativeAxisNode Node(int n, bool real = true, byte axis = 0x58) => new(new object(), axis, real, () => n);
        var low = Node(0, false); var high = Node(0xFFFFFF, false);
        Check(!KingdomQuestPineNativeAxisNode.TryLinkBoundaries(low, Node(100, false)) && low.IsDetached &&
            KingdomQuestPineNativeAxisNode.TryLinkBoundaries(low, high), "boundary preconditions without partial mutation");
        var a = Node(20); var b = Node(10); var c = Node(20);
        Check(a.TryAppend(low, Report) && b.TryAppend(high, Report) && c.TryAppend(a, Report) &&
            low.Next == b && b.Next == a && a.Next == c && c.Next == high, "bidirectional search and equal anchor order");
        Check(!a.TryAppend(low, Report) && errors[^1] == KingdomQuestPineAxisError.AlreadyLinked &&
            !Node(5, true, 0x59).TryAppend(low, Report) && errors[^1] == KingdomQuestPineAxisError.AxisType &&
            !Node(5).TryAppend(Node(6), Report) && errors[^1] == KingdomQuestPineAxisError.DetachedAnchor,
            "duplicate link, mixed axis and detached anchor rejection");
        Check(b.Unlink(Report) && b.IsDetached && low.Next == a && !b.Unlink(Report), "unlink reciprocal neighbours and repeated removal");
        var zeroFlag = Node(0, false);
        Check(!zeroFlag.TryAppend(low, Report) && !zeroFlag.IsDetached && low.Next == zeroFlag &&
            errors[^1] == KingdomQuestPineAxisError.InvalidBoundary, "failed post-validation leaves native insertion writes intact");
        zeroFlag.Unlink(Report);
        var pair = new KingdomQuestPineNativeAxes(new object(), true, () => 15, () => 15);
        Check(pair.X.TryAppend(low, Report), "partial pair setup"); errors.Clear();
        pair.FreeFromLists(Report);
        Check(pair.X.IsDetached && pair.Y.IsDetached && errors.SequenceEqual(new[] { KingdomQuestPineAxisError.UnequalAxisRemoval }),
            "FreeFromList detects X/Y removal mismatch after unlinking");
        errors.Clear(); pair.FreeFromLists(Report); Check(errors.Count == 0, "already detached pair is a no-op");

        // Native search permits 10,000 advances, then returns false without
        // diagnostics or insertion. Nearby anchors still permit that object.
        low = Node(0, false); high = Node(0xFFFFFF, false);
        Check(KingdomQuestPineNativeAxisNode.TryLinkBoundaries(low, high), "long-list endpoints");
        var last = low;
        for (int i = 1; i <= 10001; i++) { var next = Node(i); Check(next.TryAppend(last, Report), "populate long list"); last = next; }
        var far = Node(10002); errors.Clear();
        Check(!far.TryAppend(low, Report) && far.IsDetached && errors.Count == 0 && far.TryAppend(last, Report), "native bounded search");
    }

    static void MarkingEdges()
    {
        var f = new Fixture();
        var pollutedName = Name("KDHoneying"); pollutedName[11] = 7;
        var wrong = f.AddMap(pollutedName, 20000, 40000);
        var first = f.AddMap(Name("KDHoneying"), 20000, 40000);
        var duplicate = f.AddMap(Name("KDHoneying"), 30000, 50000);
        f.Bind(wrong, first, duplicate);
        var door = f.BuildDoor();
        Check(door.Map == first.Identity && Objects(wrong).Length == 0 && Objects(duplicate).Length == 0,
            "exact twelve bytes including bytes after NUL; first matching map in native list order");
        f.Registry.TryUnmark(door, 0, 0, 0, null);
        var before = door.SnapshotBriefWire(); var previousCounters = door.AxisCounters;
        f.Bind(wrong); f.CounterReads = 0;
        Check(f.Registry.TryMark(door, 1, out int err) && err == 1 && door.Map == first.Identity &&
            f.CounterReads == 0 && !door.IsMarked && door.AxisCounters.B == previousCounters.B && before.SequenceEqual(door.SnapshotBriefWire()),
            "missing exact map returns native error one without mutation");
        Check(!f.Registry.TryMark(door, 0, out _) && !f.Registry.TryMark(f.Doors[1], 1, out _) &&
            f.CounterReads == 0, "collision-aware marking and uninitialized login remain unresolved");
        f.Bind(first); door.SetNativeMap(null); door.SetNativePosition(-1, 0); f.CounterReads = 0; f.Errors.Clear();
        Check(f.Registry.TryMark(door, 1, out err) && err == 3 && door.Map == first.Identity &&
            f.CounterReads == 1 && door.AxisCounters.B == 0 && !door.IsMarked && door.Axes.X.IsDetached &&
            f.Errors.SequenceEqual(new[] { KingdomQuestPineAxisError.CoordinatesOutsideMap }), "unsigned bounds failure retains first map/counter writes");
        door.SetNativePosition(20000, 40000); f.CounterReads = 0;
        Check(f.Registry.TryMark(door, 1, out err) && err == 0 && door.IsMarked && f.CounterReads == 2 &&
            !door.Axes.X.IsDetached && !door.Axes.Y.IsDetached, "marking accepts equality at BOTH map limits");
        Check(f.Registry.TryMoveTo(door, 20000, 1, 5) && door.Location.Y == 40000, "MoveTo rejects same inclusive marking limit");

        f.Errors.Clear();
        var previousY = door.Axes.Y.Previous;
        Check(f.Registry.TryMark(door, 1, out err) && err == 0 && door.Location.X == 10000 && door.Location.Y == 20000 &&
            door.IsMarked && door.Axes.Y.Previous == previousY && f.Errors.SequenceEqual(new[] {
                KingdomQuestPineAxisError.AlreadyLinked, KingdomQuestPineAxisError.SaveFieldInformation, KingdomQuestPineAxisError.DumpMap }),
            "failed X append skips Y; door relinks to center, logs and still marks successfully");
        f.Registry.TryUnmark(door, 0, 0, 0, null); door.SetNativePosition(9000, 9000);
        var anchor = ((KingdomQuestPineNativeAxialFlag)first.SnapshotAxis(true)[1]).Axes.Y;
        Check(door.Axes.Y.TryAppend(anchor, f.Report), "prepare failed Y append"); f.Errors.Clear();
        Check(f.Registry.TryMark(door, 1, out err) && err == 0 && !door.Axes.X.IsDetached && !door.Axes.Y.IsDetached &&
            door.Location.X == 10000 && f.Errors.SequenceEqual(new[] { KingdomQuestPineAxisError.AlreadyLinked,
                KingdomQuestPineAxisError.SaveFieldInformation, KingdomQuestPineAxisError.DumpMap }),
            "failed Y append keeps newly inserted X and performs no rollback");
        f.Registry.TryUnmark(door, 0, 0, 0, null); f.Errors.Clear();
        Check(f.Registry.TryMoveTo(door, 100, 200, 5) && door.Location.X == 10000 && door.Location.Y == 20000 &&
            f.Errors.Count(e => e == KingdomQuestPineAxisError.RelocateDetachFailure) == 2,
            "door relocation failure recenters before proceeding with second axis");

        var big = f.AddMap(Name("KDHoneying"), 0x1000000, 0x1000000); f.Bind(big);
        door.SetNativePosition(0xFFFFFF, 3); f.CounterReads = 0; f.Errors.Clear();
        Check(f.Registry.TryMark(door, 1, out err) && err == 3 && door.Map == big.Identity && f.CounterReads == 2 &&
            f.Errors.SequenceEqual(new[] { KingdomQuestPineAxisError.ReservedCoordinate }) && door.Axes.X.IsDetached,
            "reserved 24-bit coordinate rejection occurs AFTER second map/counter writes");
        door.SetNativeMap(first.Identity); before = door.SnapshotBriefWire();
        Check(!f.Registry.TryMoveTo(door, 1, 2, 5) && before.SequenceEqual(door.SnapshotBriefWire()), "unbound map pointer is unresolved");
        door.SetNativeMap(null);
        Check(f.Registry.TryMoveTo(door, 1, 2, 5) && before.SequenceEqual(door.SnapshotBriefWire()), "null map movement is native no-op");
    }

    // Actual pool, objects, flags, axes, marking, movement and cleanup. These
    // callbacks deliberately stand in for still-unbound visibility/AI/Lua owners.
    sealed class Fixture : IKingdomQuestPineNativeDoorServices, IKingdomQuestPineNativeEffectServices,
        IKingdomQuestPineDoorBuildOwner, IKingdomQuestPineEffectOwner
    {
        public readonly List<KingdomQuestPineAxisError> Errors = new();
        public readonly List<string> Events = new();
        public readonly KingdomQuestPineNativeObjectManager Manager;
        public readonly KingdomQuestPineNativeDoorObject[] Doors;
        public KingdomQuestPineNativeMapRegistry Registry;
        readonly KingdomQuestPineVariableStack variables = new();
        KingdomQuestPineNativeDoorObject parent;
        KingdomQuestPineNativeSceneObject marking;
        KingdomQuestPineNativeEffectObject lastEffect;
        int nextDoor;
        public int CounterReads;
        public uint Clock;
        public bool Script;
        public Fixture()
        {
            Manager = new(_ => throw new Exception("unexpected type"), (_, _) => throw new Exception("unexpected free miss"));
            var seed = new byte[48];
            Doors = Enumerable.Range(0, 1000).Select(i => new KingdomQuestPineNativeDoorObject((ushort)i, seed, this)).ToArray();
            var effects = Enumerable.Range(0, 1000).Select(i => new KingdomQuestPineNativeEffectObject((ushort)i, seed, this, Manager)).ToArray();
            var flags = Enumerable.Range(0, 3584).Select(i => new KingdomQuestPineNativeAxialFlag((ushort)i, Report)).ToArray();
            Check(Manager.TryBindPool(0, flags) && Manager.TryBindPool(7, Doors) && Manager.TryBindPool(9, effects), "bind all three native-size pools");
            variables.TryPush("Door1", out _); variables.TryPush("EffDoor1", out _);
        }
        public void Report(KingdomQuestPineAxisError error, object _) => Errors.Add(error);
        public KingdomQuestPineNativeFieldMapAxes AddMap(byte[] name, uint x, uint y)
        {
            var flags = new KingdomQuestPineNativeAxialFlag[7];
            for (int i = 0; i < 7; i++) { ushort h = 0; Check(Manager.TryAllocate(0, ref h, out var o), "flag allocation"); flags[i] = (KingdomQuestPineNativeAxialFlag)o; }
            var identity = new KingdomQuestPineEffectMapIdentity(name, x, y);
            var map = new KingdomQuestPineNativeFieldMapAxes(identity, flags, low => {
                Check(low == flags[0] && low.X == 0 && low.Y == 0 && low.Map == identity &&
                    flags[1].X == 0xFFFFFF && flags[1].Map == identity &&
                    low.Axes.X.IsDetached && low.Axes.Y.IsDetached && flags[1].Axes.X.IsDetached,
                    "title-zone object setter runs after endpoint configure and BEFORE list linking");
            }, Report);
            Check(map.IsReady, "source axes ready"); return map;
        }
        public void Bind(params KingdomQuestPineNativeFieldMapAxes[] maps) => Registry = new(maps, () => {
            if (marking != null) Check(marking.Map != null, "map written BEFORE counter read");
            return new(0, (uint)++CounterReads, 10, uint.MaxValue);
        }, Report);
        public KingdomQuestPineNativeDoorObject BuildDoor()
        {
            Check(KingdomQuestHoneyingDoorBuildNativePlan.TryBuild(Site(12,
                "doorbuild Door1 \"KQ_SlimeGate\" 9860 6094  272 1000 \"Normal\"."), out var plan), "door source plan");
            Check(KingdomQuestPineDoorBuildRuntime.TryStep(plan, variables, this, out bool done) && done, "source door command");
            return Doors[nextDoor++];
        }
        public KingdomQuestPineNativeEffectObject Blast(KingdomQuestPineNativeDoorObject door)
        {
            parent = door;
            Check(KingdomQuestHoneyingEffectNativePlan.TryBuild(Site(18,
                "effectobj EffDoor1 Door1 \"KQ_SlimeGate\" 3600000 1000."), variables, out var plan), "effect source plan");
            Check(KingdomQuestPineEffectRuntime.TryStep(plan, variables, this, out bool done) && done, "source effect command");
            return lastEffect;
        }
        public bool IsAvailable => true;
        public uint CurrentNativeTick => Clock++;
        public int Mark(KingdomQuestPineNativeSceneObject obj, byte ignore)
        { marking = obj; Check(Registry.TryMark(obj, ignore, out int error), "available native marking"); marking = null; return error; }
        public void FreeFromLists(KingdomQuestPineNativeSceneObject obj)
        { Events.Add("free-lists"); Registry.FreeFromLists(obj); }
        public KingdomQuestPineNativeDoorMobData FindMobData(ushort id) => new(1);
        public void ReportMissingMobData() => throw new Exception("unexpected missing mob");
        public void BuildComplete(KingdomQuestPineNativeDoorObject obj, ushort handle) { }
        public void FinishBuildMovement(KingdomQuestPineNativeDoorObject obj) { }
        public KingdomQuestMapCollision GetCollision(KingdomQuestPineNativeDoorObject obj) => null;
        public void BroadcastDoorAction(KingdomQuestPineNativeDoorObject obj, byte[] wire) => throw new Exception("unexpected action");
        public void BlastComplete(KingdomQuestPineNativeEffectObject obj, ushort handle) { }
        public void ReportOutOfBounds(KingdomQuestPineNativeEffectObject obj) => throw new Exception("unexpected routine bounds");
        public void MoveTo(KingdomQuestPineNativeEffectObject obj, int x, int y, int arg)
        { Events.Add("move"); Check(Registry.TryMoveTo(obj, x, y, arg), "real effect movement"); }
        public void Unmark(KingdomQuestPineNativeEffectObject obj, int when, int broadcast, int reason)
        { Check(Registry.TryUnmark(obj, when, (byte)broadcast, (byte)reason, o => {
            Check(o.IsMarked && !o.Axes.X.IsDetached && !o.Axes.Y.IsDetached, "routine logout before detach"); Events.Add("logout");
        }), "real unmark"); }
        public bool HasRoutineScript(KingdomQuestPineNativeEffectObject obj) { Events.Add("script-check"); return Script; }
        public void RunRoutineScript(KingdomQuestPineNativeEffectObject obj, ushort handle, KingdomQuestPineEffectMapIdentity map)
        { Check(Manager.TryGetObject(handle, out var active) && active == null && obj.Axes.X.IsDetached && obj.Axes.Y.IsDetached,
            "script after removal from pool and map lists"); Events.Add("lua"); }
        public object Allocate(byte type, out ushort handle)
        { handle = 0; Check(Manager.TryAllocate(type, ref handle, out var obj), "scene allocation"); return obj; }
        public KingdomQuestPineTokenValue ResolveDestination(KingdomQuestPineVariableStack vars, string id)
        { vars.TryFind(id, out var value); return value; }
        public ushort FindMobId(string name) => 1091;
        public byte[] GetCurrentMapName12() => Name("KDHoneying");
        public int Build(object obj, KingdomQuestPineDoorBuildRequest request)
        { Check(((KingdomQuestPineNativeDoorObject)obj).TryBuild(request, out int error), "Build available"); return error; }
        public bool Free(ushort handle, int when, int reason)
        { Check(Manager.TryFree(handle, when, reason, out bool result), "Free available"); return result; }
        public void ReportFailure(KingdomQuestPineDoorBuildFailure failure, int error) => throw new Exception("unexpected door error");
        public object ResolveParent(byte[] token) => parent;
        public bool ParentHasMap(object obj) => ((KingdomQuestPineNativeDoorObject)obj).Map != null;
        public int Blast(object obj, KingdomQuestPineEffectBlastRequest request)
        { lastEffect = (KingdomQuestPineNativeEffectObject)obj; Check(lastEffect.TryBlast(request, out int error), "Blast available"); return error; }
        public void ReportAllocationFailure() => throw new Exception("unexpected allocation failure");
        public void ReportDestinationFailure() => throw new Exception("unexpected destination failure");
    }
}
