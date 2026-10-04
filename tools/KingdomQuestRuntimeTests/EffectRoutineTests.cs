using System.Text;
using NextGen.Zone.Data;

static class EffectRoutineTests
{
    static void Check(bool ok, string message)
    { if (!ok) throw new Exception("effect routine: " + message); }
    static KingdomQuestPineEffectMapIdentity Map(string name = "KQAlias", uint width = 100, uint height = 200)
    {
        var bytes = new byte[12]; Encoding.ASCII.GetBytes(name).CopyTo(bytes, 0);
        return new KingdomQuestPineEffectMapIdentity(bytes, width, height);
    }
    static KingdomQuestPineEffectLocation At(int x = 0, int y = 0, uint mode = 0,
        KingdomQuestPineEffectMapIdentity map = null) => new(map ?? Map(), mode, x, y, 136);

    public static void Run()
    {
        var routine = new KingdomQuestPineEffectRoutine(0, 0, 3600000);
        var owner = new Owner { Parent = At(11), Tick = 50 };
        Check(routine.TryStep(owner) && routine.NextFollowTick == 50 && owner.Moves == 0, "follow waits at exact deadline");
        owner.Events.Clear(); owner.Tick = 51;
        Check(routine.TryStep(owner) && routine.NextFollowTick == 100 && owner.Moves == 1, "strict follow after deadline");
        Check(owner.Events.SequenceEqual(new[] { "clock", "parent", "move:11:0:5", "clock", "script-check" }), "follow order and native MoveTo argument");
        Check(owner.Current.X == 11 && routine.Deadline == 36000, "movement does not reset expiry");

        foreach (var (x, y, expected) in new[] { (10, 0, 0), (6, 8, 0), (10, 1, 1), (-11, 0, 1) })
        {
            owner = new Owner { Parent = At(x, y), Tick = 51 };
            routine = new KingdomQuestPineEffectRoutine(0, 0, 3600000);
            Check(routine.TryStep(owner) && owner.Moves == expected, "squared distance >100: " + x + "," + y);
        }
        owner = new Owner { Parent = null, Tick = 1000 };
        routine = new KingdomQuestPineEffectRoutine(0, 0, 3600000);
        Check(routine.TryStep(owner) && routine.NextFollowTick == 100 && owner.Moves == 0, "late timer advances once even without parent");
        Check(routine.TryStep(owner) && routine.NextFollowTick == 150, "next pass advances old deadline again, no catch-up loop");
        routine = new KingdomQuestPineEffectRoutine(0, uint.MaxValue - 25, 3600000);
        owner = new Owner { Tick = 24 };
        Check(routine.NextFollowTick == 24 && routine.TryStep(owner) && routine.NextFollowTick == 24, "follow timer wraps and waits at equality");
        owner.Tick = 25;
        Check(routine.TryStep(owner) && routine.NextFollowTick == 74, "follow resumes after wrapped deadline");
        var twoClocks = new KingdomQuestPineEffectRoutine(10, 11, 1000);
        Check(twoClocks.Deadline == 20 && twoClocks.NextFollowTick == 61, "separate native initialization clock reads");

        // Vanish does not remove immediately; next routine still follows FIRST.
        routine = new KingdomQuestPineEffectRoutine(0, 0, 3600000);
        owner = new Owner { Parent = At(20), Tick = 51, Script = true };
        routine.RetreatFromMap();
        Check(routine.Deadline == 0 && owner.Events.Count == 0, "retreat has no owner side effects");
        Check(routine.TryStep(owner), "retreated routine dispatch");
        Check(owner.Events.SequenceEqual(new[] { "clock", "parent", "move:20:0:5", "clock",
            "unmark:0:1:3", "free:19388:0:5", "script-check", "script:19388:KQAlias" }), "follow/unmark/free/Lua order after vanish");
        Check(owner.Frees == 1 && owner.FreeResult == false, "ignored native Free return does not stop the routine tail");

        foreach (var (x, y, invalid) in new[] { (99, 199, false), (100, 0, true), (0, 200, true), (-1, 0, true), (0, -1, true) })
        {
            routine = new KingdomQuestPineEffectRoutine(0, 0, 3600000);
            owner = new Owner { Current = At(x, y), Tick = 0 };
            Check(routine.TryStep(owner) && (owner.Frees == 1) == invalid, "unsigned bounds: " + x + "," + y);
            Check(!invalid || owner.Events.First() == "bounds", "bounds checked before timers");
        }
        owner = new Owner { Current = At(100), Parent = At(20), Tick = 51 };
        routine = new KingdomQuestPineEffectRoutine(0, 0, 3600000);
        Check(routine.TryStep(owner) && owner.Current.X == 20 && owner.Frees == 1 &&
            owner.Events.IndexOf("bounds") < owner.Events.IndexOf("move:20:0:5"), "follow back inside cannot undo bounds expiry");
        owner = new Owner { Current = new KingdomQuestPineEffectLocation(null, 0, -1, -1, 0) };
        routine = new KingdomQuestPineEffectRoutine(0, 0, 3600000);
        Check(routine.TryStep(owner) && owner.Frees == 0 && !owner.Events.Contains("bounds"), "null map skips bounds, not forced expiry");

        owner = new Owner { Tick = 0, LaterTick = 1 };
        routine = new KingdomQuestPineEffectRoutine(0, 0, 100);
        Check(routine.TryStep(owner) && owner.Frees == 1, "expiry re-reads clock and expires at equality");
        owner = new Owner { Tick = 1, Script = true };
        owner.OnUnmark = () => owner.WireHandle = 123;
        owner.OnFree = () => { owner.WireHandle = 456; owner.Current = At(map: Map("OtherMap")); };
        routine = new KingdomQuestPineEffectRoutine(0, 0, 100);
        Check(routine.TryStep(owner) && owner.Events.Contains("free:123:0:5") &&
            owner.Events.Last() == "script:456:OtherMap", "handle/map read after unmark and after free");
        owner = new Owner { Tick = 1, Script = true };
        owner.OnFree = () => owner.Script = false;
        routine = new KingdomQuestPineEffectRoutine(0, 0, 100);
        Check(routine.TryStep(owner) && owner.Events.Last() == "script-check" &&
            !owner.Events.Any(e => e.StartsWith("script:")), "Free can clear the script before tail check");
        owner = new Owner { Available = false, Tick = 1000 };
        routine = new KingdomQuestPineEffectRoutine(0, 0, 100);
        Check(!routine.TryStep(owner) && owner.Events.Count == 0 && routine.NextFollowTick == 50 &&
            routine.Deadline == 1 && !routine.TryStep(null), "unavailable owner never dispatches or mutates timer");

        var identityBytes = Encoding.ASCII.GetBytes("ABCDEFGHIJKL");
        var mapIdentity = new KingdomQuestPineEffectMapIdentity(identityBytes, 100, 200);
        identityBytes[0] = 0; var snapshot = mapIdentity.SnapshotName12(); snapshot[1] = 0;
        Check(Encoding.ASCII.GetString(mapIdentity.SnapshotName12()) == "ABCDEFGHIJKL", "raw map identity isolated");
        Check(KingdomQuestPineEffectRoutine.DistanceSquared(At(1, 2), At(4, 6)) == 25, "native integer square");
        Check(KingdomQuestPineEffectRoutine.DistanceSquared(At(0), At(65536)) == 0, "distance multiplication wraps at 32 bits");
        Check(KingdomQuestPineEffectRoutine.DistanceSquared(At(int.MinValue), At(int.MaxValue)) == 1, "distance subtraction wraps too");
        Check(KingdomQuestPineEffectRoutine.DistanceSquared(At(), At(46341, 46341)) == 9266, "distance sum wraps");
        foreach (var other in new[] { At(map: Map("Different")), At(mode: 1),
            new KingdomQuestPineEffectLocation(null, 0, 0, 0, 0), null })
            Check(KingdomQuestPineEffectRoutine.DistanceSquared(At(), other) == 999999999, "native different-space sentinel");
        var padded = new byte[12]; padded[0] = (byte)'A';
        var mapA = new KingdomQuestPineEffectMapIdentity(padded, 10, 10);
        padded[11] = 1;
        var mapB = new KingdomQuestPineEffectMapIdentity(padded, 10, 10);
        Check(KingdomQuestPineEffectRoutine.DistanceSquared(At(map: mapA), At(map: mapB)) == 999999999,
            "distance compares all Name3 bytes, including after NUL");
        Check(KingdomQuestPineEffectRoutine.DistanceSquared(At(map: Map(width: 100)), At(map: Map(width: 500))) == 0,
            "distance uses name/mode, not map pointer or limits");

        byte[] name32 = new byte[32]; Encoding.ASCII.GetBytes("KQ_SlimeGate").CopyTo(name32, 0);
        var wire = KingdomQuestPineEffectBrief.CreateNativeWire(0x4BBC, name32, 9860, 6094, 136, 0x509E, 1000, 0xA4);
        Check(wire.Length == 50 && Convert.ToHexString(wire) ==
            "111CBC4B4B515F536C696D6547617465000000000000000000000000000000000000000084260000CE170000889E50E803A4",
            "literal original 0x1C11 effect brief wire");
        name32[0] = 0;
        Check(wire[4] == 'K', "wire detached from name buffer");
        wire = KingdomQuestPineEffectBrief.CreateNativeWire(65535, name32, -1, int.MinValue, 255, 65535, 65537, 255);
        Check(BitConverter.ToInt32(wire, 36) == -1 && BitConverter.ToInt32(wire, 40) == int.MinValue &&
            wire[44] == 255 && BitConverter.ToUInt16(wire, 47) == 1 && wire[49] == 255, "signed coordinates, raw direction and u16 scale");
        foreach (var (index, handle) in new[] { (0, 0x4BBC), (999, 0x4FA3), (1000, 65535), (65535, 65535) })
            Check(KingdomQuestPineEffectBrief.RemakeNativeHandle((ushort)index) == handle, "effect handle pool boundary " + index);
        foreach (var (old, arg, result) in new[] { (255, 0, 254), (254, 1, 255), (0, -1, 1), (165, 2, 164) })
            Check(KingdomQuestPineEffectBrief.ApplyBlastFlag((byte)old, arg) == result, "only bit zero changes");
        Console.WriteLine("PASS: native effect follow/expiry/bounds/Lua order, clock and distance overflow, original 50-byte brief and handle range");
    }
    sealed class Owner : IKingdomQuestPineEffectRoutineOwner
    {
        public bool Available = true, Script, FreeResult = false;
        public uint Tick;
        public uint? LaterTick;
        int clockReads;
        public ushort WireHandle = 0x4BBC;
        public KingdomQuestPineEffectLocation Current = At(), Parent;
        public int Moves, Frees;
        public Action OnUnmark, OnFree;
        public readonly List<string> Events = new();
        public bool IsAvailable => Available;
        public uint CurrentNativeTick { get { Events.Add("clock"); return clockReads++ == 0 ? Tick : LaterTick ?? Tick; } }
        public ushort Handle => WireHandle;
        public KingdomQuestPineEffectLocation Location => Current;
        public KingdomQuestPineEffectLocation ParentLocation { get { Events.Add("parent"); return Parent; } }
        public void ReportOutOfBounds() { Events.Add("bounds"); }
        // Test owner records the call and supplies the post-movement snapshot;
        // it is not a production implementation of map/axis relinking.
        public void MoveTo(int x, int y, int argument)
        { Events.Add($"move:{x}:{y}:{argument}"); Moves++; Current = new(Current.Map, Current.Mode, x, y, Current.Direction); }
        public void Unmark(int a, int b, int reason) { Events.Add($"unmark:{a}:{b}:{reason}"); OnUnmark?.Invoke(); }
        public bool Free(ushort handle, int when, int reason)
        { Events.Add($"free:{handle}:{when}:{reason}"); Frees++; OnFree?.Invoke(); return FreeResult; }
        public bool HasRoutineScript { get { Events.Add("script-check"); return Script; } }
        public void RunRoutineScript(ushort handle, KingdomQuestPineEffectMapIdentity map)
        { Events.Add($"script:{handle}:" + (map == null ? "null" : Encoding.ASCII.GetString(map.SnapshotName12()).TrimEnd('\0'))); }
    }
}
