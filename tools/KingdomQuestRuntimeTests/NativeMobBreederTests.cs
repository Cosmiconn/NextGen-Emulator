using NextGen.Zone.Data;
using Breeder = NextGen.Zone.Data.KingdomQuestPineNativeMobBreeder;
using Seconds = NextGen.Zone.Data.KingdomQuestPineNativeBreedSeconds;

static class NativeMobBreederTests
{
    static void Check(bool ok, string name) { if (!ok) throw new Exception("native mob breeder: " + name); }
    static Seconds Timings(uint standard = 20, uint min = 1, uint max = 100) =>
        new(new uint[] { standard, min, max, unchecked((uint)-5), 10, unchecked((uint)-2), 20, 7, 30, 3, 40, 9 });
    static Breeder New(KingdomQuestPineNativeMobGroupBinding group = null) =>
        new(group, Timings(), 456, 9, 123, 77, 88, null, null, null);
    public static void Run()
    {
        StorageAndFamilies(); Timers(); Regeneration(); Failures(); FamilyDeath(); PoolLifecycle();
        Console.WriteLine("PASS: native mob breeder storage, family counters, strict timers, regeneration/pool failure order, family death dispatch, persistent 64-slot groups and Lua handle semantics");
    }
    sealed class Fixture : IKingdomQuestPineNativeMobRegenLocation, IKingdomQuestPineNativeMobLifeTimeSource,
        IKingdomQuestPineNativeMobDiagnostics, IKingdomQuestPineNativeMobRoamingPath
    {
        public readonly List<string> Events = new();
        public readonly KingdomQuestPineNativeObjectManager Objects;
        public readonly KingdomQuestPineNativeMobFamilies Families = new();
        public readonly KingdomQuestPineNativeMobRandomWords Random = new(new ushort[] { 9, 13, 65535, 27 }, 0, 3);
        public readonly KingdomQuestPineNativeMobGroupBinding Group;
        public readonly KingdomQuestPineNativeMobRuntime Runtime;
        public readonly Mob[] Mobs;
        public uint Clock = 71, LifeTime = 222, Argument = 0xABCD0123;
        public int X = 25, Y = 35, Error, Allocations, RoamX = 40, RoamY = 50;
        public byte LocationResult = 1;
        public bool ClockAvailable = true, LocationAvailable = true, LifeAvailable = true, HasLifeIndex = true,
            RegenAvailable = true, RoamAvailable = true, DiagnosticsAvailable = true;
        public Action OnClock, OnInit;
        public Action<KingdomQuestPineNativeMobRegeneration> OnRegen;
        public Fixture(bool bind = true)
        {
            Objects = new(_ => throw new Exception("type"), (h, r) => Events.Add($"missing:{h}:{r}"));
            Group = new(new byte[12], this, null);
            Runtime = new(Objects, Families, ReadClock, ReadArgument, Random, this, this);
            Mobs = Enumerable.Range(0, 8000).Select(i => new Mob(this, (ushort)i, 5)).ToArray();
            if (bind) Check(Objects.TryBindPool(5, Mobs), "full native mob pool binding");
        }
        public bool ReadClock(out uint value) { Events.Add("clock"); OnClock?.Invoke(); value = Clock; return ClockAvailable; }
        public bool ReadArgument(out uint value) { Events.Add("argument"); value = Argument; return true; }
        public bool TryRegenLocation(out int x, out int y, out int direction, out byte result)
        { Events.Add("location"); x = X; y = Y; direction = 9999; result = LocationResult; return LocationAvailable; }
        public bool TryRegenLocation(ref int x, ref int y)
        { Events.Add("roam-position"); x = RoamX; y = RoamY; return RoamAvailable; }
        public bool TryLookup(ushort id, out bool present, out uint life)
        { Events.Add("lookup:" + id); present = HasLifeIndex; life = LifeTime; return LifeAvailable; }
        public void AllocationFailed() => Events.Add("allocation-failed");
        public bool TryInvalidCoordinates(KingdomQuestPineNativeMobGroupBinding group, int x, int y)
        { Events.Add($"invalid:{x}:{y}"); return DiagnosticsAvailable; }
        public Breeder Breeder(int remaining = -1)
        { var b = New(Group); b.Set(Group, 123, Timings(), remaining); return b; }
        public int Occupied { get { Check(Objects.TryGetAllocatedCount(5, out int n), "bound pool count"); return n; } }
    }
    sealed class Mob : IKingdomQuestPineNativeBreederMob
    {
        readonly Fixture f;
        public KingdomQuestPineNativeObjectType NativeObjectType { get; }
        public ushort NativeHandle { get; }
        public KingdomQuestPineNativeMobRegeneration Request;
        public IKingdomQuestPineNativeMobRoamingPath Path;
        public byte Mode, Dead;
        public uint Hp = uint.MaxValue;
        public Action OnKill;
        public Mob(Fixture fixture, ushort handle, ushort type) { f = fixture; NativeHandle = handle; NativeObjectType = (KingdomQuestPineNativeObjectType)type; }
        public void InitializeForAllocation() { f.Allocations++; f.Events.Add("init:" + NativeHandle); f.OnInit?.Invoke(); }
        public void FreeFromLists() => f.Events.Add("free-lists:" + NativeHandle);
        public void InvokeManagerFreeCallback() => f.Events.Add("free-callback:" + NativeHandle);
        public bool TryGetMode(out byte mode) { f.Events.Add("mode:" + NativeHandle); mode = Mode; return true; }
        public bool TryAlreadyDead(out byte dead) { f.Events.Add("dead:" + NativeHandle); dead = Dead; return true; }
        public bool TryGetHp(out uint hp) { f.Events.Add("hp:" + NativeHandle); hp = Hp; return true; }
        public bool TryKilledBy(IKingdomQuestPineNativePoolObject killer, int hp, int n, uint flag)
        {
            Check(ReferenceEquals(killer, this) && hp == unchecked((int)Hp) && n == 1000 && flag == 1, "native self-kill arguments including signed HP bits");
            f.Events.Add("kill:" + NativeHandle); OnKill?.Invoke(); return true;
        }
        public bool TryRegenLocation(out int x, out int y)
        { f.Events.Add("leader-position:" + NativeHandle); x = 101; y = 202; return true; }
        public bool TryRegenerate(KingdomQuestPineNativeMobRegeneration request, out int error)
        { f.Events.Add("regen:" + NativeHandle); Request = request; f.OnRegen?.Invoke(request); error = f.Error; return f.RegenAvailable; }
        public bool TrySetRoamingPath(IKingdomQuestPineNativeMobRoamingPath path)
        { f.Events.Add("path:" + NativeHandle); Path = path; return true; }
        public bool TrySetLifeTime(uint life) { f.Events.Add("life:" + life); return true; }
    }
    static void StorageAndFamilies()
    {
        var b = New(); var previous = New(); var next = New(); var brief = new object();
        Check(b.Enabled == 1 && b.MobId == 0 && b.Field08 == -1 && b.Remaining == -1 && b.Handle == 65535 &&
            b.Deadline == 456 && b.Pending == 9 && b.CurrentInterval == 123 && b.Tick4C == 77 && b.Tick50 == 88,
            "constructor writes only proven fields and retains explicit seed");
        b.Previous = previous; b.Next = next; b.LoginCharacterBrief = brief; b.Handle = 14; b.Enabled = 0;
        b.Set(null, 42, Timings(), 8);
        Check(b.Deadline == 70 && b.Pending == 1 && b.CurrentInterval == 20 && b.Remaining == 8 && b.Handle == 14 && b.Enabled == 0 &&
            b.Previous == previous && b.Next == next && b.LoginCharacterBrief == brief && b.Tick4C == 77 && b.Tick50 == 88,
            "Set uses delta2 * 10 as absolute initial deadline and preserves reuse fields");
        b.SetRegenTime(-2);
        Check(Enumerable.Range(0, 12).All(i => b.Seconds[i] == 0xFFFFFFFE) && b.CurrentInterval == 0xFFFFFFFE && b.Deadline == 70,
            "Setregentime writes every timing DWORD but not deadline");
        var root = New(); var child = New(); child.Next = next;
        Check(child.TrySetLeader(root) && root.Next == child && child.Previous == root && child.Next == next, "append preserves own successor");
        root.Next = root; Check(!New().TrySetLeader(root), "cyclic native traversal unresolved");
        var families = new KingdomQuestPineNativeMobFamilies();
        families.Register(root, 3, 2); families.Register(root, 3, 9);
        Check(!families.Dead(root, 3) && families.Dead(root, 3) && families.Find(root, 3).Threshold == 2, "duplicate registration does not reset or update");
        families.Register(root, 4, 0); families.Register(root, 5, 65536); families.Register(child, 3, -1);
        Check(families.Find(root, 4).Threshold == 60000 && families.Find(root, 5).Threshold == 0 && families.Find(child, 3).Threshold == 65535,
            "zero sentinel before WORD truncation");
        for (int i = 2; i < 65535; i++) families.Dead(root, 3);
        Check(!families.Dead(root, 3) && families.Find(root, 3).Deaths == 0, "WORD counter wraps and threshold comparison stays unsigned");
        families.Dead(root, 3); families.Initialize(root); Check(families.Find(root, 3).Deaths == 0, "family reset");
        families.Remove(root); Check(families.Count == 1 && !families.Dead(root, 3) && !families.Dead(child, 99), "remove and missing species");
    }
    static void Timers()
    {
        var f = new Fixture(); var b = f.Breeder();
        f.Clock = 70; Check(b.TryRoutine(f.Runtime, null) && f.Occupied == 0 && b.Pending == 1, "equal deadline does not regenerate");
        f.Clock = 71; Check(b.TryRoutine(f.Runtime, null) && f.Occupied == 1 && b.Handle == 0 && b.Pending == 0 && b.Remaining == -1, "strict deadline spawn");
        b.Enabled = 0; f.Events.Clear(); Check(b.TryRoutine(f.Runtime, null) && f.Events.Count == 0, "disabled root no clock read");
        b.Enabled = 1; b.Previous = New(); Check(b.TryRoutine(f.Runtime, null) && f.Events.Count == 0, "follower no scheduling"); b.Previous = null;
        Check(b.TryRoutine(f.Runtime, null) && f.Events.Count == 0 && b.Pending == 0, "live object prevents scheduling");
        f.Objects.TryFree(0, 0, 0, out _); b.Tick4C = 100; b.Tick50 = 190; f.Clock = 400;
        Check(b.TryRoutine(f.Runtime, null) && b.Handle == 65535 && b.Pending == 1 && b.CurrentInterval == 15 && b.Deadline == 550,
            "elapsed death duration chooses first delta and schedules from clock");
        foreach (var pair in new[] { (10u,18u), (19u,18u), (20u,20u), (30u,20u), (31u,23u), (40u,23u), (41u,29u) })
        {
            b = f.Breeder(); b.Pending = 0; b.Tick4C = 0; b.Tick50 = pair.Item1 * 10;
            Check(b.TryRoutine(f.Runtime, null) && b.CurrentInterval == pair.Item2, "all equality boundaries " + pair.Item1);
        }
        b = f.Breeder(); b.Pending = 0; b.Tick4C = uint.MaxValue - 8; b.Tick50 = 1; b.CurrentInterval = 2;
        Check(b.TryRoutine(f.Runtime, null) && b.CurrentInterval == 100, "wrapped elapsed subtraction and negative-delta unsigned clamp to MAX");
        b.Set(f.Group, 1, Timings(20, 50, 10), -1); b.Pending = 0; b.Tick4C = b.Tick50 = 0;
        f.Clock = uint.MaxValue - 9;
        Check(b.TryRoutine(f.Runtime, null) && b.CurrentInterval == 50 && b.Deadline == 490, "ordered min/max clamp and wrapping deadline");
        b = f.Breeder(); b.Pending = 0; f.ClockAvailable = false;
        f.OnClock = () => Check(b.Pending == 1, "pending write precedes clock read");
        Check(!b.TryRoutine(f.Runtime, null) && b.Pending == 1 && b.Deadline == 70 && b.CurrentInterval == 20, "unresolved clock preserves preceding write only");
    }
    static void Regeneration()
    {
        var f = new Fixture(); var root = f.Breeder(2); var child = f.Breeder(1); child.TrySetLeader(root);
        f.Families.Register(root, 123, 4); f.Families.Dead(root, 123);
        f.OnRegen = request => Check(f.Families.Find(root, 123).Deaths == 0 && request.Breeder.Pending == 0, "family reset and pending clear before regen");
        Check(root.TryRoutine(f.Runtime, null) && f.Occupied == 2 && root.Handle == 0 && child.Handle == 1 && root.Remaining == 1 && child.Remaining == 0,
            "whole family sequential spawn and attempts");
        var r = f.Mobs[0].Request; var c = f.Mobs[1].Request;
        Check(r.X == 25 && r.Y == 35 && r.Direction == 65 && r.Leader == null && r.Handle == 0 && r.MobId == 123 &&
            r.GlobalArgument == f.Argument && r.Field08 == -1 && r.Argument1 == 1 && r.Argument0 == 0 && r.Breeder == root,
            "complete leader regeneration arguments; random overrides geometry direction");
        Check(c.X == 101 && c.Y == 202 && c.Direction == 327675 && c.Leader == f.Mobs[0] && f.Random.Index == 2,
            "follower copies leader position and consumes next shared WORD");
        Check(f.Events.SequenceEqual(new[] { "clock", "init:0", "location", "argument", "regen:0", "path:0", "lookup:123", "life:222",
            "init:1", "leader-position:0", "argument", "regen:1", "path:1", "lookup:123", "life:222" }), "observable regeneration ordering");
        f = new Fixture(); root = f.Breeder(); f.Group.RoamingPath = f;
        f.OnRegen = request => { f.Group.RoamingPath = null; root.MobId = 77; };
        Check(f.Runtime.TryRegenerate(root, root, out ushort handle) && handle == 0 && f.Mobs[0].Request.X == 40 &&
            f.Mobs[0].Request.Y == 50 && f.Mobs[0].Path == null && f.Events.Contains("lookup:77") && root.Handle == 65535,
            "roaming modifies XY; path and MobId reread after regen; helper doesn't store handle");
        var ring = new KingdomQuestPineNativeMobRandomWords(new ushort[] { 11 }, uint.MaxValue, 0);
        Check(ring.TryNext(out ushort word) && word == 11 && ring.Index == 0, "ring increments before mask and wraps");
        ring.Mask = 7; Check(!ring.TryNext(out _) && ring.Index == 1, "out-of-bound seed unresolved after native index write");
        f = new Fixture(); root = f.Breeder(); child = f.Breeder();
        f.OnInit = () => root.Handle = 0;
        Check(f.Runtime.TryRegenerate(child, root, out handle) && f.Mobs[0].Request.Leader == f.Mobs[0],
            "leader handle is read after allocation initialization callback");
        f = new Fixture(); var actor = f.Breeder(); actor.LoginCharacterBrief = new object();
        Check(!f.Runtime.TryRegenerate(actor, actor, out _) && f.Occupied == 0 && f.Events.Count == 0, "non-null brief selects unbound actor pool without mob fallback");
        var actors = Enumerable.Range(0, 2048).Select(i => new Mob(f, (ushort)(0x43BC + i), 8)).ToArray();
        Check(f.Objects.TryBindPool(8, actors) && f.Runtime.TryRegenerate(actor, actor, out handle) && handle == 0x43BC &&
            ReferenceEquals(actors[0].Request.LoginCharacterBrief, actor.LoginCharacterBrief), "actor pool and brief identity");
    }
    static void Failures()
    {
        var f = new Fixture(false); var b = f.Breeder();
        Check(!b.TryRoutine(f.Runtime, null) && b.Pending == 0 && b.Remaining == -1 && b.Handle == 65535, "unbound allocation retains pending clear");
        f = new Fixture(); b = f.Breeder(3); f.LocationResult = 0;
        Check(b.TryRoutine(f.Runtime, null) && b.Remaining == 2 && b.Handle == 65535 && f.Occupied == 0 && f.Random.Index == 0 &&
            f.Events.TakeLast(2).SequenceEqual(new[] { "free-lists:0", "free-callback:0" }), "location rejection frees before RNG but consumes attempt");
        f = new Fixture(); b = f.Breeder();
        Check(f.Runtime.TryRegenerate(b, f.Breeder(), out ushort handle) && handle == 65535 && f.Occupied == 1 && f.Random.Index == 0 &&
            f.Events.SequenceEqual(new[] { "init:0" }), "missing leader intentionally leaves allocated slot occupied");
        foreach (var xy in new[] { (-1, 0), (0, 0xFFFFFF), (0xFFFFFF, 2) })
        {
            f = new Fixture(); b = f.Breeder(); f.X = xy.Item1; f.Y = xy.Item2;
            Check(f.Runtime.TryRegenerate(b, b, out handle) && handle == 65535 && f.Occupied == 0 && f.Random.Index == 1 &&
                f.Events[2] == $"invalid:{xy.Item1}:{xy.Item2}" && !f.Events.Contains("argument"), "unsigned coordinate rejection after random");
        }
        f = new Fixture(); b = f.Breeder(); f.Error = 123;
        Check(f.Runtime.TryRegenerate(b, b, out handle) && handle == 65535 && f.Occupied == 0 && !f.Events.Contains("path:0"), "marking error immediate free");
        f = new Fixture(); b = f.Breeder(); f.RegenAvailable = false;
        Check(!f.Runtime.TryRegenerate(b, b, out handle) && f.Occupied == 1 && f.Random.Index == 1 && handle == 65535, "unknown regeneration does not invent rollback");
        f = new Fixture(); b = f.Breeder(); f.LifeAvailable = false;
        Check(!f.Runtime.TryRegenerate(b, b, out handle) && f.Occupied == 1 && f.Events.Contains("path:0"), "late unresolved lookup retains allocation and path");
        f = new Fixture(); b = f.Breeder(); f.HasLifeIndex = false;
        Check(f.Runtime.TryRegenerate(b, b, out handle) && handle == 0 && !f.Events.Contains("life:222"), "known absent index skips lifetime");
        f = new Fixture(); b = f.Breeder(); f.OnRegen = _ => throw new InvalidOperationException("fixture");
        try { f.Runtime.TryRegenerate(b, b, out _); throw new Exception("expected callback exception"); }
        catch (InvalidOperationException) { Check(f.Occupied == 1 && f.Random.Index == 1, "exception preserves allocation and RNG"); }
        f = new Fixture(); b = f.Breeder(); ushort h = 65535;
        for (int i = 0; i < 8000; i++) Check(f.Objects.TryAllocate(5, ref h, out var obj) && obj != null, "fill mob pool");
        f.Events.Clear();
        Check(f.Runtime.TryRegenerate(b, b, out handle) && handle == 65535 && f.Events.SequenceEqual(new[] { "allocation-failed" }) && f.Random.Index == 0,
            "known pool exhaustion reports failure before location/RNG");
    }
    static void FamilyDeath()
    {
        var f = new Fixture(); var root = f.Breeder(); var child = f.Breeder(); var third = f.Breeder();
        child.TrySetLeader(root); third.TrySetLeader(root); root.TryRoutine(f.Runtime, null);
        f.Families.Register(root, 123, 1); f.Objects.TryFree(root.Handle, 0, 0, out _);
        f.Events.Clear(); f.Mobs[1].OnKill = () => { child.Next = null; };
        Check(root.TryRoutine(f.Runtime, null) && f.Families.Find(root, 123).Deaths == 1 && root.Handle == 65535 &&
            f.Events.SequenceEqual(new[] { "mode:1", "dead:1", "hp:1", "kill:1" }), "root death triggers family self-kill and reads successor after callback");
        // An extant mode-3/already-dead object still prevents the next wave.
        child.Next = third; f.Mobs[1].Mode = 3; f.Mobs[2].Dead = 1;
        root.Handle = 7999; f.Events.Clear();
        Check(root.TryRoutine(f.Runtime, null) && f.Events.SequenceEqual(new[] { "mode:1", "mode:2", "dead:2" }) && root.Pending == 0,
            "mode 3 skips dead/HP; already dead skips HP; allocated objects still block rebreed");
        child.Remaining = 0; bool freed = false; f.Events.Clear();
        Check(child.TryRoutine(f.Runtime, () => { freed = true; return true; }) && !freed && f.Events.Count == 0,
            "zero-count living follower returns before death/scheduling");
        f.Objects.TryFree(child.Handle, 0, 0, out _);
        Check(child.TryRoutine(f.Runtime, () => { freed = true; return true; }) && freed && child.Handle == 1,
            "zero-count dead follower frees slot without handle reset or family death increment");
    }
    static void PoolLifecycle()
    {
        var f = new Fixture(); int allocationReports = 0, invalidReports = 0, groupFrees = 0;
        var seeds = Enumerable.Range(0, 64).Select(_ => New()).ToArray();
        var pool = new KingdomQuestPineNativeMobBreederPool(f.Group, seeds, f.Families, () => allocationReports++, _ => invalidReports++);
        Check(seeds.All(b => b.Group == f.Group && b.Pending == 1 && b.Deadline == 0 && b.Tick4C == 77), "group ctor owns only three additional breeder fields");
        ushort index = 444; Breeder first = pool.Allocate(ref index); first.Set(f.Group, 123, Timings(), 1);
        Check(index == 0 && pool.Count == 1 && pool.Get(0) == first && pool.Get(1) == null && pool.Get(1, true) == seeds[1], "pool identity and occupancy");
        Check(pool.TryRoutine(f.Runtime, () => { groupFrees++; return true; }) && first.Handle == 0 && first.Remaining == 0, "pool dispatch creates mob");
        f.Objects.TryFree(first.Handle, 0, 0, out _);
        Check(pool.TryRoutine(f.Runtime, () => { groupFrees++; return true; }) && pool.Count == 0 && groupFrees == 0, "last breeder free doesn't free group in same call");
        Check(pool.TryRoutine(f.Runtime, () => { groupFrees++; return true; }) && groupFrees == 1, "empty group frees next call");
        for (int i = 1; i < 64; i++) { index = 500; Check(pool.Allocate(ref index) == seeds[i] && index == i, "FIFO before recycled slot"); }
        first.Enabled = 0; first.LoginCharacterBrief = new object(); first.Next = seeds[3]; first.Previous = seeds[2];
        Check(pool.Allocate(ref index) == first && index == 0 && first.Next == null && first.Previous == null && first.Enabled == 0 &&
            first.Handle == 0 && first.Remaining == 0 && first.LoginCharacterBrief != null, "reuse only resets links");
        index = 999; Check(pool.Allocate(ref index) == null && index == 999 && allocationReports == 1, "full pool preserves output index");
        seeds[2].Next = seeds[3]; seeds[3].Previous = seeds[2]; seeds[3].Next = seeds[4];
        f.Families.Register(seeds[3], 1, 1);
        Check(pool.Free(3) && seeds[2].Next == seeds[3] && seeds[3].Next == null && seeds[3].Previous == null && f.Families.Find(seeds[3], 1) == null,
            "Free clears own links/removes family but does not repair neighboring family links");
        f.Families.Register(seeds[3], 1, 1); f.Families.Register(null, 1, 1);
        Check(!pool.Free(3) && f.Families.Find(seeds[3], 1) == null && !pool.Free(65535) && f.Families.Find(null, 1) == null && invalidReports == 2,
            "inactive/invalid frees still remove absolute/null family key");
        f = new Fixture(); seeds = Enumerable.Range(0, 64).Select(_ => New()).ToArray();
        pool = new(f.Group, seeds, f.Families, () => { }, _ => { });
        index = 0; first = pool.Allocate(ref index); first.Set(f.Group, 3, Timings(), 8);
        Check(pool.TryRegenFromLua(f.Runtime) && f.Occupied == 1 && first.Handle == 65535 && first.Pending == 1 && first.Remaining == 8,
            "Lua regen ignores returned handle and preserves pending/count");
        var second = pool.Allocate(ref index); second.Set(f.Group, 4, Timings(), 8);
        f.OnRegen = _ => { pool.Free(1); };
        Check(pool.TryRoutine(f.Runtime, null) && second.Remaining == 8 && pool.Count == 1,
            "routine caches successor before callback and ignores native traversal false on freed successor");
    }
}
