using NextGen.Zone.Data;

static class NativeMapTraversalTests
{
    static void Check(bool ok, string name)
    { if (!ok) throw new Exception("native map traversal: " + name); }
    public static void Run()
    {
        var f = new Fixture();
        var source = f.Door(12000, 10000);
        var a = f.Effect(0, 5000); var b = f.Door(18000, 15000); var c = f.Effect(400, 500);
        var expected = new IKingdomQuestPineNativeAxialObject[] {
            f.Flags[3], a, f.Flags[2], c, f.Flags[4], b, f.Flags[5], f.Flags[6], source };
        var visited = new List<IKingdomQuestPineNativeAxialObject>();
        Check(f.Traversal.TryAllInMapNormal(source, 255, (candidate, origin, distance) => {
            Check(ReferenceEquals(origin, source), "callback receiver and initiating source are distinct arguments");
            int dx = source.Location.X - candidate.Location.X, dy = source.Location.Y - candidate.Location.Y;
            Check(distance == unchecked((uint)(dx * dx + dy * dy)), "unrestricted squared distance, not a guessed sight radius");
            visited.Add(candidate); return true;
        }, out bool done) && done && visited.SequenceEqual(expected), "Y predecessors then successors then optional self, including middle flags");
        Check(f.State.Depth == -1 && f.State.SnapshotCounters().A == 1 && source.AxisCounters.A == uint.MaxValue &&
            expected[..^1].All(o => o.AxisCounters.A == 1 && o.AxisCounters.B == 16) &&
            f.Flags[0].AxisCounters.A == uint.MaxValue && f.Flags[1].AxisCounters.A == uint.MaxValue,
            "single-depth stamp changes only candidates, not endpoints or final self callback");

        // Layer registration is a full 64-bit field, independent of mode.
        source.SetNativeLayer(0x100000007, 0); a.SetNativeLayer(7, 0);
        b.SetNativeLayer(0x100000007, 0); b.SetNativeMode(99, 0);
        c.SetNativeLayer(123, 2); visited.Clear();
        Check(f.Traversal.TryAllInMapNormal(source, 0, (candidate, origin, distance) => {
            visited.Add(candidate);
            if (ReferenceEquals(candidate, b)) Check(distance == 999999999, "same layer/different mode still calls visitor with native distance sentinel");
            return true;
        }, out done) && done && visited.SequenceEqual(new IKingdomQuestPineNativeAxialObject[] { c, b }) &&
            a.AxisCounters.A == 2 && f.Flags[3].AxisCounters.A == 2, "layer filter follows stamping; either nonzero view byte bypasses mismatched layer");
        source.SetNativeLayer(0x100000007, 128); visited.Clear();
        Check(f.Traversal.TryAllInMapNormal(source, 0, (o, _, _) => { visited.Add(o); return true; }, out done) && done &&
            visited.SequenceEqual(expected[..^1]), "source view byte also bypasses all layer mismatches");

        visited.Clear(); uint untouched = b.AxisCounters.A;
        Check(f.Traversal.TryAllInMapNormal(source, 1, (o, _, _) => { visited.Add(o); return false; }, out done) && !done &&
            visited.SequenceEqual(new[] { f.Flags[3] }) && b.AxisCounters.A == untouched && f.State.Depth == -1,
            "false callback stops immediately before forward pass/self and releases depth");

        // Missing flag state is unavailable, not an invented zero byte pattern.
        var unknown = new Fixture(false); var unknownSource = unknown.Door(100, 10000);
        Check(!unknown.Traversal.TryAllInMapNormal(unknownSource, 0, (_, _, _) => true, out done) && !done &&
            unknown.Errors.SequenceEqual(new[] { KingdomQuestPineTraversalEvent.UninitializedCounters }) &&
            unknown.State.Depth == -1 && unknown.State.SnapshotCounters().A == 1, "unknown initial flag counters surface an unresolved dependency");
        f = new Fixture(); source = f.Door(100, 10000);
        source.FreeFromLists();
        Check(f.Traversal.TryAllInMapNormal(source, 1, (_, _, _) => throw new Exception("detached callback"), out done) && !done &&
            f.State.SnapshotCounters().A == 0 && f.Errors.SequenceEqual(new[] { KingdomQuestPineTraversalEvent.SaveFieldInformation }),
            "detached source returns native false BEFORE incrementing visit stamp");

        MutationAndCorruption();
        NestedVisits();
        VisitLimit();
        Console.WriteLine("PASS: normal-map native traversal order, concrete flags/door/effect nodes, layer/mode separation, mutable callbacks, stamps/wrap, nesting and visit limits");
    }

    static void MutationAndCorruption()
    {
        var f = new Fixture(); var source = f.Door(100, 10000); var target = f.Door(200, 9000);
        target.Axes.X.Unlink((_, _) => { });
        Check(f.Traversal.TryAllInMapNormal(source, 1, (_, _, _) => throw new Exception("broken candidate callback"), out bool done) && !done &&
            target.AxisCounters.A == 1 && target.Location.X == 10000 && target.Location.Y == 20000 && target.Axes.X.IsDetached &&
            f.Events.SequenceEqual(new[] { "BrokenCandidate", "relink:1", "SaveFieldInformation", "DumpMap" }),
            "candidate is stamped before X-self check, then real door relink and ordered diagnostics without repair");

        f = new Fixture(); source = f.Door(100, 10000); target = f.Door(200, 9000); int calls = 0;
        Check(f.Traversal.TryAllInMapNormal(source, 0, (candidate, _, _) => {
            Check(ReferenceEquals(candidate, target), "mutation target first"); calls++; target.FreeFromLists(); return true;
        }, out done) && !done && calls == 1 && f.Errors.SequenceEqual(new[] { KingdomQuestPineTraversalEvent.RepeatedObject }),
            "removed current candidate is reread through self-link; duplicate stamp aborts before broken-link check");

        f = new Fixture(); source = f.Door(100, 10000);
        KingdomQuestPineNativeEffectObject inserted = null; var visited = new List<IKingdomQuestPineNativeAxialObject>();
        Check(f.Traversal.TryAllInMapNormal(source, 0, (candidate, _, _) => {
            visited.Add(candidate);
            if (candidate == f.Flags[3]) inserted = f.Effect(100, 7000);
            return true;
        }, out done) && done && visited[0] == f.Flags[3] && visited[1] == inserted,
            "post-callback neighbour read observes newly inserted object during active traversal");

        f = new Fixture(); source = f.Door(100, 10000); bool caught = false;
        try { f.Traversal.TryAllInMapNormal(source, 0, (_, _, _) => throw new InvalidOperationException("test"), out _); }
        catch (InvalidOperationException) { caught = true; }
        Check(caught && f.State.Depth == -1 && f.State.SnapshotCounters().A == 1, "unwinding restores depth but not already written counters");
    }

    static void NestedVisits()
    {
        var f = new Fixture(); var source = f.Door(100, 10000); var depths = new List<int>();
        bool Visit(IKingdomQuestPineNativeAxialObject candidate, IKingdomQuestPineNativeAxialObject origin, uint distance)
        {
            depths.Add(f.State.Depth);
            if (f.State.Depth < 3)
                Check(f.Traversal.TryAllInMapNormal(source, 0, Visit, out bool nested) && !nested, "nested visitor stop");
            return false;
        }
        Check(f.Traversal.TryAllInMapNormal(source, 0, Visit, out bool done) && !done &&
            depths.SequenceEqual(new[] { 0, 1, 2, 3 }) && f.State.Depth == -1 &&
            f.State.SnapshotCounters().A == 1 && f.State.SnapshotCounters().B == 18 &&
            f.State.SnapshotCounters().C == 24 && f.State.SnapshotCounters().D == 0 &&
            f.Flags[3].AxisCounters.D == 0 && f.Errors.Count == 0, "four independent recursion stamps including DWORD wrap");

        f = new Fixture(); source = f.Door(100, 10000); int rejectedDepth = -99;
        bool Overflow(IKingdomQuestPineNativeAxialObject candidate, IKingdomQuestPineNativeAxialObject origin, uint distance)
        {
            int before = f.State.Depth;
            Check(f.Traversal.TryAllInMapNormal(source, 0, Overflow, out bool nested) && !nested, "all nested calls return native false");
            if (before == 3) rejectedDepth = f.State.Depth;
            return false;
        }
        Check(f.Traversal.TryAllInMapNormal(source, 0, Overflow, out done) && !done && rejectedDepth == 2 && f.State.Depth == -1 &&
            f.Errors.SequenceEqual(new[] { KingdomQuestPineTraversalEvent.NestingLimit }),
            "fifth enter fails but its destructor STILL decrements depth; native behavior is not silently corrected");

        // The step counter is shared too. A nested complete visit leaves its
        // own forward-pass count visible in the outer callback.
        f = new Fixture(); source = f.Door(100, 10000); int nestedSteps = -1;
        Check(f.Traversal.TryAllInMapNormal(source, 0, (candidate, _, _) => {
            Check(f.State.Steps == 1, "outer first step");
            Check(f.Traversal.TryAllInMapNormal(source, 0, (_, _, _) => true, out bool nested) && nested, "complete nested traversal");
            nestedSteps = f.State.Steps; return false;
        }, out done) && !done && nestedSteps == 3 && f.State.Steps == 3, "nested calls do not restore global loop count");
    }

    static void VisitLimit()
    {
        var f = new Fixture(); var source = f.Door(1, 1);
        // Mixed native pools can exceed 10,000 entries. Synthetic participants
        // isolate the source walk bound without pretending to implement mobs.
        var lastX = source.Axes.X; var lastY = source.Axes.Y; Participant last = null;
        for (int i = 0; i < 10001; i++)
        {
            last = new Participant(f.Map.Identity, 2, 2, f.State.SnapshotCounters().Previous());
            Check(last.Axes.X.TryAppend(lastX, (_, _) => { }) && last.Axes.Y.TryAppend(lastY, (_, _) => { }), "populate limit fixture");
            lastX = last.Axes.X; lastY = last.Axes.Y;
        }
        int calls = 0;
        Check(f.Traversal.TryAllInMapNormal(source, 1, (_, _, _) => { calls++; return true; }, out bool done) && !done && calls == 10000 &&
            f.State.Steps == 10001 && last.AxisCounters.A == uint.MaxValue &&
            f.Errors.SequenceEqual(new[] { KingdomQuestPineTraversalEvent.VisitLimit }), "10,001st candidate rejected BEFORE stamp/callback");
    }

    sealed class Participant : IKingdomQuestPineNativeAxialObject
    {
        public KingdomQuestPineNativeAxes Axes { get; }
        public object RangeObject => this;
        public KingdomQuestPineEffectLocation Location { get; }
        public ulong LayerRegistrationNumber => 0;
        public byte LayerObjectViewType => 0;
        public bool HasNativeAxisCounters => true;
        public KingdomQuestPineNativeAxisCounters AxisCounters { get; private set; }
        public void SetNativeAxisCounters(KingdomQuestPineNativeAxisCounters value) { AxisCounters = value; }
        public Participant(KingdomQuestPineEffectMapIdentity map, int x, int y, KingdomQuestPineNativeAxisCounters counters)
        { Location = new(map, 0, x, y, 0); AxisCounters = counters; Axes = new(this, true, () => x, () => y); }
    }

    sealed class Fixture : IKingdomQuestPineNativeDoorServices, IKingdomQuestPineNativeEffectServices
    {
        public readonly KingdomQuestPineNativeListCheckState State = new(new(0, 17, 23, uint.MaxValue));
        public readonly List<KingdomQuestPineTraversalEvent> Errors = new();
        public readonly List<string> Events = new();
        public readonly KingdomQuestPineNativeAxialFlag[] Flags;
        public readonly KingdomQuestPineNativeFieldMapAxes Map;
        public readonly KingdomQuestPineNativeMapTraversal Traversal;
        readonly KingdomQuestPineNativeObjectManager manager = new(_ => { }, (_, _) => { });
        int doors, effects;
        public Fixture(bool seedFlags = true)
        {
            var pool = Enumerable.Range(0, 3584).Select(i => new KingdomQuestPineNativeAxialFlag((ushort)i, (_, _) => { })).ToArray();
            Check(manager.TryBindPool(0, pool), "bind flag pool"); Flags = new KingdomQuestPineNativeAxialFlag[7];
            for (int i = 0; i < 7; i++) { ushort h = 0; Check(manager.TryAllocate(0, ref h, out var flag), "flag allocation"); Flags[i] = (KingdomQuestPineNativeAxialFlag)flag;
                if (seedFlags) Flags[i].SetNativeAxisCounters(State.SnapshotCounters().Previous()); }
            Map = new(new(new byte[12], 20000, 40000), Flags, _ => { }, (_, _) => { });
            Traversal = new(State, (error, _) => { Errors.Add(error); Events.Add(error.ToString()); }, (obj, reason) => {
                Events.Add("relink:" + reason);
                if (obj is KingdomQuestPineNativeDoorObject door && door.Map != null)
                    door.SetNativePosition((int)(door.Map.XLimit / 2), (int)(door.Map.YLimit / 2));
                else if (obj is not KingdomQuestPineNativeEffectObject) throw new Exception("unbound test relink");
            });
        }
        T Add<T>(T obj, int x, int y) where T : KingdomQuestPineNativeSceneObject
        {
            obj.SetNativeMap(Map.Identity); obj.SetNativePosition(x, y); obj.SetNativeAxisCounters(State.SnapshotCounters().Previous());
            Check(obj.Axes.X.TryAppend(Flags[0].Axes.X, (_, _) => { }) && obj.Axes.Y.TryAppend(Flags[0].Axes.Y, (_, _) => { }), "actual scene axes");
            return obj;
        }
        public KingdomQuestPineNativeDoorObject Door(int x, int y) => Add(new KingdomQuestPineNativeDoorObject((ushort)doors++, new byte[48], this), x, y);
        public KingdomQuestPineNativeEffectObject Effect(int x, int y) => Add(new KingdomQuestPineNativeEffectObject((ushort)effects++, new byte[48], this, manager), x, y);
        public bool IsAvailable => true;
        public void FreeFromLists(KingdomQuestPineNativeSceneObject obj) => obj.Axes.FreeFromLists((_, _) => { });
        // These tests execute traversal, not Build/Blast, AI or packet batching.
        public int Mark(KingdomQuestPineNativeSceneObject obj, byte ignore) => throw new NotSupportedException();
        public KingdomQuestPineNativeDoorMobData FindMobData(ushort id) => throw new NotSupportedException();
        public void ReportMissingMobData() => throw new NotSupportedException();
        public void BuildComplete(KingdomQuestPineNativeDoorObject obj, ushort h) => throw new NotSupportedException();
        public void FinishBuildMovement(KingdomQuestPineNativeDoorObject obj) => throw new NotSupportedException();
        public KingdomQuestMapCollision GetCollision(KingdomQuestPineNativeDoorObject obj) => throw new NotSupportedException();
        public void BroadcastDoorAction(KingdomQuestPineNativeDoorObject obj, byte[] wire) => throw new NotSupportedException();
        public uint CurrentNativeTick => throw new NotSupportedException();
        public void BlastComplete(KingdomQuestPineNativeEffectObject obj, ushort h) => throw new NotSupportedException();
        public void ReportOutOfBounds(KingdomQuestPineNativeEffectObject obj) => throw new NotSupportedException();
        public void MoveTo(KingdomQuestPineNativeEffectObject obj, int x, int y, int arg) => throw new NotSupportedException();
        public void Unmark(KingdomQuestPineNativeEffectObject obj, int when, int broadcast, int reason) => throw new NotSupportedException();
        public bool HasRoutineScript(KingdomQuestPineNativeEffectObject obj) => throw new NotSupportedException();
        public void RunRoutineScript(KingdomQuestPineNativeEffectObject obj, ushort h, KingdomQuestPineEffectMapIdentity map) => throw new NotSupportedException();
    }
}
