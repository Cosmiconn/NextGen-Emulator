using System.Text;
using NextGen.Zone.Data;

static class NativeSceneLogoutTests
{
    static void Check(bool ok, string name) { if (!ok) throw new Exception("native scene logout: " + name); }
    public static void Run()
    {
        RecipientFilters(); IteratorWalk(); LogoutAndUnmark(); RetryAndSharedPacket(); PooledEffectExpiry();
        Console.WriteLine("PASS: native axial packet iterator, directional logout recipients, booth ordering, one retry, shared packet reentrancy and pooled effect unmark/send/free lifecycle");
    }
    static void RecipientFilters()
    {
        var f = new Fixture(); var source = new Actor(f.Map.Identity) { Type = 7 }; var target = new Actor(f.Map.Identity);
        f.Packet.Write(0, new byte[] { 5, 24, 34, 12 });
        var iterator = new KingdomQuestPineNativeAxialPacketBroadcast(f.Packet, 4, f.Layers, f);
        bool Work() { Check(iterator.TryWork(source, target, uint.MaxValue, out bool result), "resolved iterator"); return result; }
        target.Type = 8;
        Check(Work() && f.Events.SequenceEqual(new[] { "type" }), "only candidate type 2, not character alias 8");
        target.Type = 2; target.Mobile = 0; f.Events.Clear();
        Check(Work() && f.Events.SequenceEqual(new[] { "type", "mobile" }), "mobile byte gates before modes/flags");
        target.Mobile = 128; target.Mode = 1; source.Flags = 255; f.Events.Clear();
        Check(Work() && f.Events.SequenceEqual(new[] { "type", "mobile" }), "mode mismatch precedes invalid sublayer flags");
        source.Mode = uint.MaxValue; f.Events.Clear();
        Check(!iterator.TryWork(source, target, 0, out bool done) && !done && f.Events[^1] == "InvalidModeFlags", "wildcard does not hide invalid matrix address");
        source.Mode = 1; source.Flags = 0; target.Mode = uint.MaxValue; target.Flags = 1; f.Events.Clear();
        Check(Work() && f.Events.SequenceEqual(new[] { "type", "mobile", "flags", "flags", "empty", "solo", "send" }) &&
            f.Packets.Count == 1, "either wildcard accepted; source Base sends to RangerStealth; distance ignored");
        source.Flags = 1; target.Flags = 0; f.Events.Clear();
        Check(Work() && f.Events.SequenceEqual(new[] { "type", "mobile", "flags", "flags" }) && f.Packets.Count == 1,
            "reverse matrix direction blocks before empty/solo/transport");
        source.Flags = 0; target.Empty = 2; f.Events.Clear();
        Check(Work() && f.Events[^1] == "empty" && f.Packets.Count == 1, "any nonzero empty byte suppresses send");
        target.Empty = 0; target.Solo = 128; f.Events.Clear();
        Check(Work() && f.Events[^1] == "type" && f.Packets.Count == 1, "solo player does not receive other ordinary source");
        source.Type = 11; f.OwnerReads.Enqueue(uint.MaxValue); f.Events.Clear();
        Check(Work() && f.Events.Count(e => e == "owner") == 1 && f.Packets.Count == 1, "mover owner sentinel skips second read");
        f.OwnerReads.Enqueue(12); f.OwnerReads.Enqueue(target.Number); f.Events.Clear();
        Check(Work() && f.Events.TakeLast(4).SequenceEqual(new[] { "owner", "owner", "number", "send" }) && f.Packets.Count == 2,
            "mover compares second live owner read to recipient character number");
        f.OwnerReads.Enqueue(12); f.OwnerReads.Enqueue(13); f.Events.Clear();
        Check(Work() && f.Events[^1] == "number" && f.Packets.Count == 2, "different mover owner suppressed");
        source = target; f.Events.Clear();
        Check(Work() && f.Events[^1] == "send" && f.Packets.Count == 3, "ordinary solo source may send to itself");
        target.Solo = 0; f.SendAvailable = false;
        Check(!iterator.TryWork(source, target, 0, out done) && !done, "unknown stream/send remains unresolved");
        f.SendAvailable = true; f.Packet.Write(4, new byte[] { 0xCA, 0xFE }); f.Packet.TrySetLength(6);
        Check(Work() && f.Packets[^1].Wire.Length == 6 && iterator.RequestedLength == 4, "iterator gate does not restore shared length before send");
        var rejected = new KingdomQuestPineNativeAxialPacketBroadcast(f.Packet, 8192, f.Layers, f);
        f.Available = false; f.Events.Clear();
        Check(rejected.RequestedLength == 0 && f.Packet.Length == 6 && rejected.TryWork(null, null, 0, out done) && !done &&
            f.Events.Count == 0, "capacity equality rejected; old packet length retained; zero iterator length stops before services");
        var zero = new KingdomQuestPineNativeAxialPacketBroadcast(f.Packet, 0, f.Layers, f);
        Check(f.Packet.Length == 0 && zero.TryWork(null, null, 0, out done) && !done, "accepted zero packet length still returns native false");
    }
    static void IteratorWalk()
    {
        var f = new Fixture(); var source = f.BuildDoor(); f.Registry.TryMoveTo(source, 5000, 10000, 5);
        var a = f.Player(5000, 9000); var b = f.Player(5000, 7000); var c = f.Player(5000, 12000);
        var seen = new List<IKingdomQuestPineNativeAxialObject>();
        var iterator = new Iterator((origin, candidate, distance) =>
        {
            Check(ReferenceEquals(origin, source), "iterator first argument is initiating source");
            Check(distance == KingdomQuestPineEffectRoutine.DistanceSquared(source.Location, candidate.Location), "iterator distance source");
            seen.Add(candidate); return (true, true);
        });
        Check(f.Traversal.TryAllInMapNormalIterator(source, 255, iterator, out bool result) && result &&
            seen.OfType<Actor>().SequenceEqual(new[] { a, b, c }) && seen[^1] == source && f.State.Depth == -1,
            "iterator Y predecessor/successor order and final self, including real flags");
        source.SetNativeLayer(0x100000001, 0); a.Layer = 1; b.Layer = c.Layer = 0x100000001; seen.Clear();
        Check(f.Traversal.TryAllInMapNormalIterator(source, 0, iterator, out result) && result &&
            seen.SequenceEqual(new[] { b, c }), "iterator applies full 64-bit layer filter before callback");
        source.SetNativeLayer(0, 128);
        iterator.Work = (_, _, _) => (true, false);
        Check(f.Traversal.TryAllInMapNormalIterator(source, 1, iterator, out result) && !result && f.State.Depth == -1,
            "native iterator false distinguished from missing implementation");
        iterator.Work = (_, _, _) => (false, false);
        Check(!f.Traversal.TryAllInMapNormalIterator(source, 1, iterator, out result) && !result && f.State.Depth == -1,
            "unresolved iterator unwinds without invented native success");
        f.Binding.IteratorFunctionAddress = 0; f.Binding.IteratorThisAdjustment = 100;
        Check(f.Logout.TryVisitMap(source, iterator, out result) && !result, "null iterator member pointer before adjustment");
        f.Binding.IteratorFunctionAddress = 0x54BE00; f.Binding.IteratorThisAdjustment = 0;
        Check(!f.Logout.TryVisitMap(source, iterator, out _), "member-callback target is NOT iterator target");
        f.Binding.IteratorFunctionAddress = 0x54C370; f.Binding.IteratorThisAdjustment = 1;
        Check(!f.Logout.TryVisitMap(source, iterator, out _), "nonzero iterator this-adjustment unresolved");
    }
    static void LogoutAndUnmark()
    {
        var f = new Fixture(); var door = f.BuildDoor(); var player = f.Player(9000, 5000);
        var brief = door.SnapshotBriefWire(); var login = door.SnapshotLoginLocation21();
        f.Open = 1; f.CloseResult = false; f.Events.Clear();
        f.OnSend = _ => Check(door.IsMarked && !door.Axes.X.IsDetached && !door.Axes.Y.IsDetached,
            "logout send observes marked object and both lists still linked");
        Check(f.Registry.TryUnmark(door, 87, 255, 93, _ => throw new Exception("legacy callback forbidden")) &&
            !door.IsMarked && door.Axes.X.IsDetached && door.Axes.Y.IsDetached && door.Map == f.Map.Identity &&
            door.SnapshotBriefWire().SequenceEqual(brief) && door.SnapshotLoginLocation21().SequenceEqual(login),
            "concrete logout before X/Y unlink; when/reason unused; retained map/login/brief");
        Check(f.Events.Take(3).SequenceEqual(new[] { "store-check", "store-close", "StoreCloseFailed" }) &&
            f.Packets.Single().Recipient == player && f.Packets[0].Wire.SequenceEqual(new byte[] { 5, 24, 158, 80 }),
            "close failure logs but still sends exact 0x1805 + native door WORD handle");
        f.Events.Clear(); f.OnSend = null;
        Check(f.Logout.TryBroadcastLogout(door) && f.Events.Count == 0, "detached X skips booth and packet");
        Check(f.Logout.TryLogoutPacketBroadcast(door) && f.Diagnostics.TakeLast(2).SequenceEqual(new[] { "SaveFieldInformation", "SaveFieldInformation" }),
            "direct packet broadcast retries detached traversal once and ignores second false");
        door.SetNativeMap(null); f.Events.Clear(); f.Packet.Write(0, new byte[] { 7, 7 });
        Check(f.Logout.TryBroadcastLogout(door) && f.Events.Count == 0 && f.Packet.Snapshot()[0] == 7, "null map guard before any packet write");
        Check(!f.Logout.TryLogoutPacketBroadcast(door) && f.Packet.Snapshot()[0] == 5 && f.Events.Single() == "NullPacketMap",
            "direct packet null-map fault unresolved after earlier packet writes");

        f = new Fixture(); door = f.BuildDoor(); f.Player(9000, 5000); f.Binding.Field0C = 0; f.Events.Clear();
        uint counter = f.State.SnapshotCounters().A;
        Check(f.Logout.TryBroadcastLogout(door) && f.Packet.Length == 4 && f.Packets.Count == 0 &&
            f.Events.SequenceEqual(new[] { "store-check" }) && f.State.SnapshotCounters().A == counter,
            "zero map +0C still checks booth/writes packet but does not traverse");
        f.Binding.Field0C = 1; f.Binding.Booths = null; f.Packet.Write(0, new byte[] { 9 });
        Check(!f.Registry.TryUnmark(door, 0, 1, 3) && door.IsMarked && !door.Axes.X.IsDetached && f.Packet.Snapshot()[0] == 9,
            "unknown booth state stops before packet/unlink");
        f.Binding.Booths = f; f.Open = 1; f.CloseAvailable = false; f.Events.Clear();
        Check(!f.Registry.TryUnmark(door, 0, 1, 3) && door.IsMarked && f.Packet.Snapshot()[0] == 9 &&
            f.Events.SequenceEqual(new[] { "store-check", "store-close" }), "unresolved close stops before packet, unlike known native false close");
        f.CloseAvailable = true; f.Open = 0; f.Binding.IteratorFunctionAddress = 123; f.Events.Clear();
        Check(!f.Registry.TryUnmark(door, 0, 1, 3) && door.IsMarked && !door.Axes.Y.IsDetached &&
            f.Events.SequenceEqual(new[] { "store-check" }) && f.Packet.Snapshot()[0] == 5,
            "unknown iterator dispatch retains packet writes, no retry/fallback/unlink");
        f.Events.Clear();
        Check(f.Registry.TryUnmark(door, 0, 0, 3) && f.Events.Count == 0 && !door.IsMarked,
            "broadcast zero requires neither booth nor dispatch");

        f = new Fixture(); door = f.BuildDoor(); f.Player(9000, 5000); f.Open = 2; f.Events.Clear();
        Check(f.Logout.TryBroadcastLogout(door) && !f.Events.Contains("store-close"), "only exact open byte one closes booth");
        f.OnSend = _ => throw new InvalidOperationException("transport test"); bool threw = false;
        try { f.Registry.TryUnmark(door, 0, 1, 3); } catch (InvalidOperationException) { threw = true; }
        Check(threw && door.IsMarked && !door.Axes.X.IsDetached && f.State.Depth == -1 && f.Packet.Length == 4,
            "transport exception retains packet/object state, skips unlink and releases depth");
        f.OnSend = null; f.Open = 1; f.OnStoreCheck = () => { door.SetNativeMap(null); f.Binding.Booths = null; }; f.Events.Clear();
        Check(!f.Logout.TryBroadcastLogout(door) && f.Events.Take(2).SequenceEqual(new[] { "store-check", "store-close" }) &&
            f.Events[^1] == "NullPacketMap", "old booth pointer retained across callback; packet rereads changed map");
    }
    static void RetryAndSharedPacket()
    {
        var f = new Fixture(); var door = f.BuildDoor(); f.Registry.TryMoveTo(door, 5000, 10000, 5);
        var a = f.Player(5000, 9000); var b = f.Player(5000, 7000); uint before = f.State.SnapshotCounters().A;
        f.OnSend = who =>
        {
            Check(who == a, "first recipient"); f.OnSend = null;
            b.SetNativeAxisCounters(f.State.SnapshotCounters()); f.Binding.Field0C = 0;
        };
        Check(f.Logout.TryBroadcastLogout(door) && f.Packets.Select(p => p.Recipient).SequenceEqual(new[] { a, a, b }) &&
            f.State.SnapshotCounters().A == before + 2 && f.Diagnostics.SequenceEqual(new[] { "RepeatedObject" }),
            "known false retries once without rechecking +0C or deduplicating earlier sends");
        f.Binding.Field0C = 1; f.Packets.Clear(); f.Diagnostics.Clear(); before = f.State.SnapshotCounters().A;
        f.OnSend = _ => b.SetNativeAxisCounters(f.State.SnapshotCounters());
        Check(f.Registry.TryUnmark(door, 0, 1, 3) && !door.IsMarked && f.Packets.Count == 2 &&
            f.State.SnapshotCounters().A == before + 2 && f.Diagnostics.SequenceEqual(new[] { "RepeatedObject", "RepeatedObject" }),
            "second native false ignored, no third pass; Unmark continues");

        f = new Fixture(); door = f.BuildDoor(); f.Player(9000, 5000); f.SendAvailable = false;
        before = f.State.SnapshotCounters().A;
        Check(!f.Registry.TryUnmark(door, 0, 1, 3) && door.IsMarked && f.State.SnapshotCounters().A == before + 1 &&
            f.Events.Count(e => e == "send") == 1, "unresolved transport does not retry or unlink");

        f = new Fixture(); door = f.BuildDoor(); var other = f.BuildDoor();
        f.Registry.TryMoveTo(door, 5000, 10000, 5); f.Registry.TryMoveTo(other, 7000, 12000, 5);
        a = f.Player(5000, 9000); b = f.Player(5000, 7000);
        f.OnSend = _ => { f.OnSend = null; Check(f.Logout.TryLogoutPacketBroadcast(other), "nested packet broadcast"); };
        Check(f.Logout.TryLogoutPacketBroadcast(door) && f.Packets.Count == 4 &&
            BitConverter.ToUInt16(f.Packets[0].Wire, 2) == door.NativeHandle &&
            f.Packets.Skip(1).All(p => BitConverter.ToUInt16(p.Wire, 2) == other.NativeHandle) &&
            f.Packets.Select(p => p.Recipient).SequenceEqual(new[] { a, a, b, b }) && f.State.Depth == -1,
            "nested broadcast overwrites shared packet; outer later recipient sees nested handle, not restored original");
    }
    static void PooledEffectExpiry()
    {
        var f = new Fixture(); var parent = f.BuildDoor(); var effect = f.Blast(parent); var player = f.Player(9000, 5000);
        var login = effect.SnapshotLoginLocation21(); f.Registry.TryMoveTo(parent, 11000, 7000, 5);
        f.Clock = 100; effect.RetreatFromMap(); f.Events.Clear();
        f.OnSend = _ => Check(effect.IsMarked && !effect.Axes.X.IsDetached &&
            f.Manager.TryGetObject(effect.NativeHandle, out var obj) && ReferenceEquals(obj, effect), "expiry sends BEFORE unlink and pool removal");
        Check(effect.TryRunRoutine() && f.Packets.Single().Recipient == player &&
            f.Packets[0].Wire.SequenceEqual(new byte[] { 5, 24, 188, 75 }) &&
            f.Events.Where(e => e is "clock" or "move" or "unmark" or "store-check" or "send" or "free-lists" or "script-check" or "lua")
                .SequenceEqual(new[] { "clock", "move", "clock", "unmark", "store-check", "send", "free-lists", "script-check", "lua" }),
            "real effect follow -> expiry -> logout -> detach -> pool free -> script order and exact native handle packet");
        Check(!effect.IsMarked && effect.Axes.X.IsDetached && effect.Axes.Y.IsDetached &&
            f.Manager.TryGetObject(effect.NativeHandle, out var gone) && gone == null &&
            f.Manager.TryGetObjectAbsolute(effect.NativeHandle, out var retained) && ReferenceEquals(retained, effect) &&
            effect.Parent == parent && effect.Map == f.Map.Identity && effect.Location.X == 11000 && effect.SnapshotLoginLocation21().SequenceEqual(login),
            "retained pool slot/current XY/map/parent/login after completed removal");
    }
    sealed class Iterator(Func<IKingdomQuestPineNativeAxialObject, IKingdomQuestPineNativeAxialObject, uint, (bool Resolved, bool Native)> work)
        : IKingdomQuestPineNativeAxialIterator
    {
        public Func<IKingdomQuestPineNativeAxialObject, IKingdomQuestPineNativeAxialObject, uint, (bool Resolved, bool Native)> Work = work;
        public bool TryWork(IKingdomQuestPineNativeAxialObject source, IKingdomQuestPineNativeAxialObject candidate, uint distance, out bool nativeResult)
        { var result = Work(source, candidate, distance); nativeResult = result.Native; return result.Resolved; }
    }
    sealed class Actor(KingdomQuestPineEffectMapIdentity map) : IKingdomQuestPineNativeAxialObject
    {
        public byte Type = 2, Mobile = 1, Flags, Empty, Solo;
        public uint Mode, Number = 1234;
        public int X = 5000, Y = 5000;
        public ulong Layer;
        public KingdomQuestPineEffectLocation Location => new(map, Mode, X, Y, 0);
        public KingdomQuestPineNativeAxes Axes { get; private set; }
        public void InitializeAxes() => Axes = new(this, true, () => X, () => Y);
        public object RangeObject => this;
        public ulong LayerRegistrationNumber => Layer;
        public byte LayerObjectViewType => 0;
        public bool HasNativeAxisCounters => true;
        public KingdomQuestPineNativeAxisCounters AxisCounters { get; private set; }
        public void SetNativeAxisCounters(KingdomQuestPineNativeAxisCounters value) => AxisCounters = value;
    }
    // Production pools, Build/Blast completion, axes, logout and effect routine.
    // Player state, BoothManager and packet transport remain explicit fixtures.
    sealed class Fixture : IKingdomQuestPineNativeBroadcastServices, IKingdomQuestPineNativeLogoutBooths,
        IKingdomQuestPineNativeDoorServices, IKingdomQuestPineNativeEffectServices,
        IKingdomQuestPineDoorBuildOwner, IKingdomQuestPineEffectOwner, IKingdomQuestPineNativeBriefServices
    {
        public readonly KingdomQuestPineNativeObjectManager Manager;
        public readonly KingdomQuestPineNativeListCheckState State = new(new(0, 0, 0, 0));
        public readonly KingdomQuestPineNativeFieldMapAxes Map;
        public readonly KingdomQuestPineNativeMapRegistry Registry;
        public readonly KingdomQuestPineNativeMapTraversal Traversal;
        public readonly KingdomQuestPineNativeSubLayer Layers;
        public readonly KingdomQuestPineNativeSharedPacket Packet = new();
        public readonly KingdomQuestPineNativeSceneLogout Logout;
        public readonly KingdomQuestPineNativeLogoutMap Binding;
        public readonly KingdomQuestPineNativeSceneCompletion Completion;
        public readonly List<string> Events = new(), Diagnostics = new();
        public readonly List<(IKingdomQuestPineNativeAxialObject Recipient, byte[] Wire)> Packets = new();
        public readonly Queue<uint> OwnerReads = new();
        public bool Available = true, SendAvailable = true, CloseAvailable = true, CloseResult = true;
        public byte Open;
        public uint Clock = 10;
        public Action<IKingdomQuestPineNativeAxialObject> OnSend;
        public Action OnStoreCheck;
        readonly KingdomQuestPineNativeAxialFlag[] flags;
        readonly KingdomQuestPineVariableStack variables = new();
        KingdomQuestPineNativeDoorObject parent, lastDoor;
        KingdomQuestPineNativeEffectObject lastEffect;
        static byte[] Name() { var value = new byte[12]; Encoding.ASCII.GetBytes("KDHoneying").CopyTo(value, 0); return value; }
        static KingdomQuestPineNativeAiState Ai() => new(null, Enumerable.Range(0, 8).Select(_ => new KingdomQuestPineNativeAiString(new byte[16], 0, 0)).ToArray());
        public Fixture()
        {
            Manager = new(_ => throw new Exception("unexpected pool"), (_, _) => throw new Exception("unexpected free miss"));
            flags = Enumerable.Range(0, 7).Select(i => new KingdomQuestPineNativeAxialFlag((ushort)i, (_, _) => { })).ToArray();
            foreach (var flag in flags) flag.SetNativeAxisCounters(State.SnapshotCounters().Previous());
            Map = new(new(Name(), 20000, 40000), flags, _ => { }, (e, _) => throw new Exception(e.ToString()));
            Registry = new(new[] { Map }, () => State.SnapshotCounters(), (e, _) => throw new Exception(e.ToString()));
            Traversal = new(State, (e, _) => Diagnostics.Add(e.ToString()), (_, _) => throw new Exception("unexpected relink"));
            Check(KingdomQuestPineNativeSubLayer.TryParse(new StringReader(File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "Fixtures", "SubLayerInteract.txt"), Encoding.Latin1)), out var layers), "source matrix"); Layers = layers;
            Logout = new(Traversal, Layers, this, Packet, (e, _) => Events.Add(e.ToString()));
            Binding = new(Map.Identity, 1, 0x54C370, 0, this); Check(Logout.TryBindMap(Binding), "logout map binding"); Registry.NativeLogout = Logout;
            Completion = new(new(this), Traversal); Check(Completion.TryBindMap(new(Map.Identity, true, 0, 20000, 0x54BE00, 0)), "completion binding");
            var doors = Enumerable.Range(0, 1000).Select(i => new KingdomQuestPineNativeDoorObject((ushort)i, new byte[48], this)
                { NativeCompletion = Completion, NativeAi = Ai(), NativeSightState = 0, NativeMovement = new(new byte[64]) }).ToArray();
            var effects = Enumerable.Range(0, 1000).Select(i => new KingdomQuestPineNativeEffectObject((ushort)i, new byte[48], this, Manager)
                { NativeCompletion = Completion, NativeAi = Ai(), NativeSightState = 0 }).ToArray();
            Check(Manager.TryBindPool(7, doors) && Manager.TryBindPool(9, effects), "native-size persistent pools");
            variables.TryPush("Door1", out _); variables.TryPush("EffDoor1", out _);
        }
        public Actor Player(int x, int y)
        {
            var actor = new Actor(Map.Identity) { X = x, Y = y }; actor.InitializeAxes(); actor.SetNativeAxisCounters(State.SnapshotCounters().Previous());
            Check(actor.Axes.X.TryAppend(flags[0].Axes.X, (_, _) => { }) && actor.Axes.Y.TryAppend(flags[0].Axes.Y, (_, _) => { }), "player test axes"); return actor;
        }
        public KingdomQuestPineNativeDoorObject BuildDoor()
        {
            KingdomQuestHoneyingExternalPlanBuilder.TryBuild("KQ/Honeying", 12, "doorbuild Door1 \"KQ_SlimeGate\" 9860 6094  272 1000 \"Normal\".", out var site);
            KingdomQuestHoneyingDoorBuildNativePlan.TryBuild(site, out var plan);
            Check(KingdomQuestPineDoorBuildRuntime.TryStep(plan, variables, this, out bool done) && done, "source door build"); return lastDoor;
        }
        public KingdomQuestPineNativeEffectObject Blast(KingdomQuestPineNativeDoorObject obj)
        {
            parent = obj;
            KingdomQuestHoneyingExternalPlanBuilder.TryBuild("KQ/Honeying", 18, "effectobj EffDoor1 Door1 \"KQ_SlimeGate\" 3600000 1000.", out var site);
            KingdomQuestHoneyingEffectNativePlan.TryBuild(site, variables, out var plan);
            Check(KingdomQuestPineEffectRuntime.TryStep(plan, variables, this, out bool done) && done, "source effect blast"); return lastEffect;
        }
        public bool IsAvailable => Available;
        public byte ObjectType(IKingdomQuestPineNativeAxialObject obj) { Events.Add("type"); return ((Actor)obj).Type; }
        public byte MobileMode(IKingdomQuestPineNativeAxialObject obj) { Events.Add("mobile"); return ((Actor)obj).Mobile; }
        public byte ModeFlags(IKingdomQuestPineNativeAxialObject obj) { Events.Add("flags"); return ((Actor)obj).Flags; }
        public byte IsEmpty(IKingdomQuestPineNativeAxialObject obj) { Events.Add("empty"); return ((Actor)obj).Empty; }
        public byte SoloingMode(IKingdomQuestPineNativeAxialObject obj) { Events.Add("solo"); return ((Actor)obj).Solo; }
        public uint MoverOwnerCharacterNumber(IKingdomQuestPineNativeAxialObject obj) { Events.Add("owner"); return OwnerReads.Dequeue(); }
        public uint CharacterNumber(IKingdomQuestPineNativeAxialObject obj) { Events.Add("number"); return ((Actor)obj).Number; }
        public bool TrySendPacket(IKingdomQuestPineNativeAxialObject obj, KingdomQuestPineNativeSharedPacket packet)
        { Events.Add("send"); if (!SendAvailable) return false; Packets.Add((obj, packet.Snapshot())); OnSend?.Invoke(obj); return true; }
        public void Report(KingdomQuestPineBroadcastEvent reason, IKingdomQuestPineNativeAxialObject obj) => Events.Add(reason.ToString());
        public bool TryCheckStoreIsOpening(ushort handle, out byte result) { Events.Add("store-check"); OnStoreCheck?.Invoke(); result = Open; return true; }
        public bool TryCloseStore(ushort handle, out bool result) { Events.Add("store-close"); result = CloseResult; return CloseAvailable; }
        public int ViewKind(IKingdomQuestPineNativeAxialObject obj) => throw new NotSupportedException();
        public int StateA18(IKingdomQuestPineNativeAxialObject obj) => throw new NotSupportedException();
        public int MovementState(IKingdomQuestPineNativeAxialObject obj) => throw new NotSupportedException();
        public uint FieldSightSquared(IKingdomQuestPineNativeAxialObject obj) { Check(Completion.TryFieldSightSquared((KingdomQuestPineNativeSceneObject)obj, out uint value), "candidate sight"); return value; }
        public void QueueMovement(IKingdomQuestPineNativeAxialObject obj) => throw new NotSupportedException();
        public void TraceDoorSourceName(IKingdomQuestPineNativeAxialObject obj) => throw new NotSupportedException();
        public void Report(KingdomQuestPineBriefEvent reason, IKingdomQuestPineNativeAxialObject obj) => throw new Exception(reason.ToString());
        public bool TryExchangeOther(KingdomQuestPineBriefFamily family, IKingdomQuestPineNativeAxialObject source, IKingdomQuestPineNativeAxialObject candidate) => throw new NotSupportedException();
        public void SendPacket(IKingdomQuestPineNativeAxialObject obj, byte[] wire) => throw new NotSupportedException();
        public int Mark(KingdomQuestPineNativeSceneObject obj, byte ignore) { Check(Registry.TryMark(obj, ignore, out int error), "native marking"); return error; }
        public void FreeFromLists(KingdomQuestPineNativeSceneObject obj) { Events.Add("free-lists"); Registry.FreeFromLists(obj); }
        public KingdomQuestPineNativeDoorMobData FindMobData(ushort id) => new(1);
        public void ReportMissingMobData() => throw new NotSupportedException();
        public void BuildComplete(KingdomQuestPineNativeDoorObject obj, ushort handle) => throw new Exception("legacy completion");
        public void FinishBuildMovement(KingdomQuestPineNativeDoorObject obj) => throw new Exception("legacy movement");
        public KingdomQuestMapCollision GetCollision(KingdomQuestPineNativeDoorObject obj) => throw new NotSupportedException();
        public void BroadcastDoorAction(KingdomQuestPineNativeDoorObject obj, byte[] wire) => throw new NotSupportedException();
        public uint CurrentNativeTick { get { Events.Add("clock"); return Clock++; } }
        public void BlastComplete(KingdomQuestPineNativeEffectObject obj, ushort handle) => throw new Exception("legacy completion");
        public void ReportOutOfBounds(KingdomQuestPineNativeEffectObject obj) => throw new NotSupportedException();
        public void MoveTo(KingdomQuestPineNativeEffectObject obj, int x, int y, int argument) { Events.Add("move"); Check(Registry.TryMoveTo(obj, x, y, argument), "native move"); }
        public void Unmark(KingdomQuestPineNativeEffectObject obj, int when, int broadcast, int reason)
        { Events.Add("unmark"); Check(Registry.TryUnmark(obj, when, (byte)broadcast, (byte)reason), "native logout/unmark"); }
        public bool HasRoutineScript(KingdomQuestPineNativeEffectObject obj) { Events.Add("script-check"); return true; }
        public void RunRoutineScript(KingdomQuestPineNativeEffectObject obj, ushort handle, KingdomQuestPineEffectMapIdentity map)
        { Check(Manager.TryGetObject(handle, out var active) && active == null && obj.Axes.X.IsDetached, "Lua after map/pool removal"); Events.Add("lua"); }
        public object Allocate(byte type, out ushort handle) { handle = 0; Check(Manager.TryAllocate(type, ref handle, out var obj), "allocate"); return obj; }
        public KingdomQuestPineTokenValue ResolveDestination(KingdomQuestPineVariableStack vars, string id) { vars.TryFind(id, out var value); return value; }
        public ushort FindMobId(string name) => 1091;
        public byte[] GetCurrentMapName12() => Name();
        public int Build(object obj, KingdomQuestPineDoorBuildRequest request)
        { lastDoor = (KingdomQuestPineNativeDoorObject)obj; Check(lastDoor.TryBuild(request, out int error), "concrete Build"); return error; }
        public object ResolveParent(byte[] token) => parent;
        public bool ParentHasMap(object obj) => parent.Map != null;
        public int Blast(object obj, KingdomQuestPineEffectBlastRequest request)
        { lastEffect = (KingdomQuestPineNativeEffectObject)obj; Check(lastEffect.TryBlast(request, out int error), "concrete Blast"); return error; }
        public bool Free(ushort handle, int when, int reason) { Check(Manager.TryFree(handle, when, reason, out bool result), "native free"); return result; }
        public void ReportFailure(KingdomQuestPineDoorBuildFailure failure, int error) => throw new Exception(failure.ToString());
        public void ReportAllocationFailure() => throw new NotSupportedException();
        public void ReportDestinationFailure() => throw new NotSupportedException();
    }
}
