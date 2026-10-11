using NextGen.Zone.Data;

static class NativeBriefExchangeTests
{
    static void Check(bool ok, string name)
    { if (!ok) throw new Exception("native brief exchange: " + name); }
    public static void Run()
    {
        SightAndDispatch();
        PacketsAndReentrancy();
        ActualMapTraversal();
        Console.WriteLine("PASS: native brief sight/dispatch, concrete door/effect payloads, 28-entry packets, shared buffers, flush/reset order and normal-map integration");
    }

    static void SightAndDispatch()
    {
        Check(KingdomQuestPineNativeBriefExchange.FieldSightSquared(false, true, 1, 1, 100) == 0 &&
            KingdomQuestPineNativeBriefExchange.FieldSightSquared(true, false, 1, 1, 100) == 0,
            "map and data guard precedes unlimited flags");
        Check(KingdomQuestPineNativeBriefExchange.FieldSightSquared(true, true, 1, 0, 0) == uint.MaxValue &&
            KingdomQuestPineNativeBriefExchange.FieldSightSquared(true, true, 0, 1, 0) == uint.MaxValue &&
            KingdomQuestPineNativeBriefExchange.FieldSightSquared(true, true, 2, 2, 65537) == 131073,
            "exact-one flags, uint32 square wraps");
        var f = new Services(); var x = new KingdomQuestPineNativeBriefExchange(f); var source = new Participant(2);
        var effect = f.Effect(); var door = f.Door();
        f.Sight = 100;
        Check(x.TryVisit(effect, source, 101) && x.EffectCount == 0 &&
            x.TryVisit(effect, source, 100) && x.EffectCount == 1, "effect uses inclusive candidate sight boundary");
        x.SourceSightSquared = uint.MaxValue;
        Check(x.TryVisit(effect, source, uint.MaxValue) && x.EffectCount == 1,
            "source global sight does not replace effect's own sight");
        f.Sight = uint.MaxValue;
        Check(x.TryVisit(effect, source, uint.MaxValue) && x.EffectCount == 2, "sight comparisons are unsigned");
        f.Sight = 0;
        Check(x.TryVisit(door, source, uint.MaxValue) && x.DoorCount == 1, "door view 1 ignores distance and map sight");
        f.DoorMobile = 0;
        Check(x.TryVisit(door, source, 0) && x.DoorCount == 1, "door visibility byte filters before movement and exchange");
        f.DoorMobile = 128; f.DoorMovement = 3; f.Events.Clear();
        Check(x.TryVisit(door, source, 0) && f.Events.SequenceEqual(new[] { "queue", "trace" }),
            "nonzero door mobile byte accepted; move state is queued BEFORE exchange");
        f.DoorMovement = 0;
        var target = new Participant(1) { View = 3 };
        x.SourceSightSquared = 9; f.Events.Clear();
        Check(x.TryVisit(target, source, 10) && f.Events.Count == 0 &&
            x.TryVisit(target, source, 9) && f.Events.SequenceEqual(new[] { "Item" }), "view 3 uses shared source limit inclusively");
        target.View = 0; f.Events.Clear();
        Check(x.TryVisit(target, source, 0) && f.Events.Count == 0, "view zero skips all later virtual calls");
        target.View = -1; target.State = 1; target.Move = 2; f.Events.Clear(); f.MovementReads = 0;
        Check(x.TryVisit(target, source, 0) && f.Events.SequenceEqual(new[] { "InvalidViewKind", "queue", "Item" }) &&
            f.MovementReads == 0, "invalid view logs then proceeds; nonzero A18 short-circuits movement query");
        target.State = 0; f.Events.Clear();
        Check(x.TryVisit(target, source, 0) && f.MovementReads == 1, "second movement predicate read only when first zero");

        string[][] families = [ [], ["Item"], ["Character", "CharacterAbstate"], ["MiniHouse"],
            ["Mob", "MobAbstate"], ["Mob", "MobAbstate"], ["MagicField"], [],
            ["Character", "CharacterAbstate"], [], ["Mob", "MobAbstate"], ["Mover", "CharacterAbstate"], ["Pet"] ];
        for (byte type = 0; type <= 12; type++)
        {
            if (type == 7 || type == 9) continue;
            f.Events.Clear(); target = new Participant(type);
            Check(x.TryVisit(target, source, 0) && f.Events.SequenceEqual(families[type]), "original jump table type " + type);
        }
        f.Events.Clear(); target = new Participant(255) { State = 1 };
        Check(x.TryVisit(target, source, 0) && f.Events.SequenceEqual(new[] { "queue", "InvalidObjectType" }),
            "unknown type reports after queue and returns native true");
        f.Events.Clear(); f.OtherAvailable = false;
        Check(!x.TryVisit(new Participant(2), source, 0) && f.Events.SequenceEqual(new[] { "Character" }),
            "unbound family is unresolved; do not invent subsequent abstate success");
        f.Available = false; f.Events.Clear();
        Check(!x.TryVisit(door, source, 0) && !x.TrySendDoors(source) && f.Events.Count == 0,
            "unavailable services cannot silently complete");
    }

    static void PacketsAndReentrancy()
    {
        var f = new Services(); var x = new KingdomQuestPineNativeBriefExchange(f); var source = new Participant(2);
        var door = f.Door(); var effect = f.Effect();
        Check(x.DoorCount == 0 && x.EffectCount == 0 && x.DoorPacketLength == 0 && x.EffectPacketLength == 0,
            "original data initializes separate counts and protocol lengths to zero");
        Check(x.TrySendDoors(source) && f.Packets.Count == 0 && f.SourceMobileReads == 0, "empty Send skips recipient virtuals");
        for (int i = 0; i < 28; i++) { door.SetNativePosition(i * 17, -i); Check(x.TryVisit(door, source, 0), "append door"); }
        Check(x.DoorCount == 28 && f.Packets.Count == 0 && x.DoorPacketLength == 0, "entry 28 does not flush yet");
        var oldLast = door.SnapshotBriefWire()[2..];
        door.SetNativePosition(123456, -998);
        f.Events.Clear();
        Check(x.TryVisit(door, source, 0) && x.DoorCount == 1 && x.DoorPacketLength == 1347 &&
            f.Events.SequenceEqual(new[] { "trace", "send" }), "entry 29 flushes 28 then appends; source name trace comes first");
        var packet = f.Packets.Single().Wire;
        Check(packet.Length == 1347 && packet[0] == 0x10 && packet[1] == 0x1C && packet[2] == 28 &&
            packet[^48..].SequenceEqual(oldLast) && BitConverter.ToInt32(packet, 7) == 0 &&
            BitConverter.ToInt32(packet, 3 + 27 * 48 + 4) == 459, "door batch opcode, count and copied entry order");
        Check(x.TrySendDoors(source) && x.TrySendDoors(source) && x.DoorCount == 1 && x.DoorPacketLength == 51 &&
            f.Packets[^1].Wire[3..].SequenceEqual(door.SnapshotBriefWire()[2..]) &&
            f.Packets[^1].Wire.SequenceEqual(f.Packets[^2].Wire), "explicit Send retains count and can send same packet twice");
        f.Packets[^1].Wire[3] ^= 255;
        Check(x.TrySendDoors(source) && f.Packets[^1].Wire[3..].SequenceEqual(door.SnapshotBriefWire()[2..]),
            "transport receives its own byte array, not writable accumulator storage");
        x.InitializeDoors();
        Check(x.DoorCount == 0 && x.DoorPacketLength == 51 && x.EffectCount == 0, "Init clears count only, not last protocol length");
        f.Events.Clear();
        for (int i = 0; i < 29; i++) { effect.SetNativePosition(i, i + 1); Check(x.TryVisit(effect, source, 0), "append effect"); }
        Check(x.EffectCount == 1 && x.DoorCount == 0 && f.Events.SequenceEqual(new[] { "send" }) &&
            f.Packets[^1].Wire[0] == 0x12 && f.Packets[^1].Wire[2] == 28, "effect has independent batch/opcode and no door name trace");
        Check(x.TrySendEffects(source) && f.Packets[^1].Wire[3..].SequenceEqual(effect.SnapshotBriefWire()[2..]),
            "all 48 effect bytes including parent, scale and high flag bits copied exactly");
        int sends = f.Packets.Count; source.Mobile = 0; x.InitializeDoors();
        for (int i = 0; i < 29; i++) Check(x.TryVisit(door, source, 0), "invisible recipient collects");
        Check(x.DoorCount == 1 && f.Packets.Count == sends && x.DoorPacketLength == 51 &&
            x.TrySendDoors(source) && x.DoorCount == 1, "invisible source still collects; overflow skips send then discards old count");
        source.Type = 8; f.Events.Clear();
        Check(x.TryVisit(door, source, 0) && x.TryVisit(effect, source, 0) && x.DoorCount == 1 && x.EffectCount == 1 &&
            f.Events.Count == 0, "type 8 not accepted as initiating recipient despite character dispatch mapping");
        source.Type = 2; source.Mobile = 1;
        var otherSource = new Participant(2);
        Check(x.TrySendDoors(otherSource) && ReferenceEquals(f.Packets[^1].Source, otherSource) && x.DoorCount == 1,
            "accumulator is shared, not tied to the recipient that appended");

        x.InitializeDoors();
        for (int i = 0; i < 28; i++) x.TryVisit(door, source, 0);
        f.OnSend = () => throw new InvalidOperationException("transport test"); bool threw = false;
        try { x.TryVisit(door, source, 0); } catch (InvalidOperationException) { threw = true; }
        Check(threw && x.DoorCount == 28 && x.DoorPacketLength == 1347, "exception before return from Send retains count and written length");
        // The outer exchange must read the actual payload only AFTER returning
        // from Send; a nested exchange shares and can reset the same accumulator.
        f.OnSend = () => {
            f.OnSend = null; x.InitializeDoors(); door.SetNativePosition(7654, 321);
            Check(x.TryVisit(door, source, 0) && x.DoorCount == 1, "nested exchange shares count");
            door.SetNativePosition(9991, 99);
        };
        Check(x.TryVisit(door, source, 0) && x.DoorCount == 1 && x.TrySendDoors(source) &&
            BitConverter.ToInt32(f.Packets[^1].Wire, 7) == 9991,
            "outer reset discards nested append, then snapshots candidate's post-callback payload");
        f.Events.Clear();
        Check(!x.TryVisit(new Participant(7), source, 0) && f.Events.SequenceEqual(new[] { "trace" }) && x.DoorCount == 1,
            "unknown concrete door cannot substitute fabricated 48-byte brief");
    }

    static void ActualMapTraversal()
    {
        var f = new Services(); var exchange = new KingdomQuestPineNativeBriefExchange(f);
        var state = new KingdomQuestPineNativeListCheckState(new(0, 0, 0, 0));
        var flags = Enumerable.Range(0, 7).Select(i => new KingdomQuestPineNativeAxialFlag((ushort)i, (_, _) => { })).ToArray();
        foreach (var flag in flags) flag.SetNativeAxisCounters(state.SnapshotCounters().Previous());
        var map = new KingdomQuestPineNativeFieldMapAxes(new(new byte[12], 20000, 40000), flags, _ => { }, (_, _) => { });
        var traversal = new KingdomQuestPineNativeMapTraversal(state, (_, _) => throw new Exception("unexpected traversal diagnostic"),
            (_, _) => throw new Exception("unexpected relink"));
        var source = new Participant(2); source.Position = new(map.Identity, 0, 5000, 10000, 0);
        var near = f.Effect(); near.SetNativeMap(map.Identity); near.SetNativePosition(5000, 9000);
        var far = f.Effect(); far.SetNativeMap(map.Identity); far.SetNativePosition(5000, 13000);
        var door = f.Door(); door.SetNativeMap(map.Identity); door.SetNativePosition(5000, 35000);
        foreach (var obj in new IKingdomQuestPineNativeAxialObject[] { source, near, far, door })
        {
            obj.SetNativeAxisCounters(state.SnapshotCounters().Previous());
            Check(obj.Axes.X.TryAppend(flags[0].Axes.X, (_, _) => { }) && obj.Axes.Y.TryAppend(flags[0].Axes.Y, (_, _) => { }), "link actual nodes");
        }
        f.Sight = 1000000;
        Check(exchange.TryVisitNormalMap(traversal, source, 0, out bool result) && result &&
            exchange.EffectCount == 1 && exchange.DoorCount == 1 && state.Depth == -1 &&
            far.AxisCounters.A == 1 && f.Packets.Count == 0, "actual traversal stamps far effect, filters it in visitor, includes distant door, skips flags and does not auto-flush");
        Check(exchange.TrySendEffects(source) && f.Packets[^1].Wire[3..].SequenceEqual(near.SnapshotBriefWire()[2..]),
            "actual map packet contains only visible effect");
        exchange.InitializeDoors(); exchange.InitializeEffects(); f.OtherAvailable = false;
        Check(!exchange.TryVisitNormalMap(traversal, source, 1, out result) && !result && state.Depth == -1 &&
            exchange.DoorCount == 1 && exchange.EffectCount == 1, "unresolved self character stops traversal and retains prior batches/stamps");
    }

    sealed class Participant : IKingdomQuestPineNativeAxialObject
    {
        public byte Type, Mobile = 1; public int View = 1, State, Move;
        public Participant(byte type) { Type = type; Axes = new(this, true, () => Location.X, () => Location.Y); }
        public KingdomQuestPineEffectLocation Position = new(null, 0, 0, 0, 0);
        public KingdomQuestPineEffectLocation Location => Position;
        public KingdomQuestPineNativeAxes Axes { get; }
        public object RangeObject => this;
        public ulong LayerRegistrationNumber => 0;
        public byte LayerObjectViewType => 0;
        public bool HasNativeAxisCounters => true;
        public KingdomQuestPineNativeAxisCounters AxisCounters { get; private set; }
        public void SetNativeAxisCounters(KingdomQuestPineNativeAxisCounters value) => AxisCounters = value;
    }

    sealed class Services : IKingdomQuestPineNativeBriefServices, IKingdomQuestPineNativeDoorServices, IKingdomQuestPineNativeEffectServices
    {
        public bool Available = true, OtherAvailable = true;
        public byte DoorMobile = 1; public int DoorMovement, MovementReads, SourceMobileReads;
        public uint Sight = uint.MaxValue;
        public readonly List<string> Events = new();
        public readonly List<(IKingdomQuestPineNativeAxialObject Source, byte[] Wire)> Packets = new();
        public Action OnSend;
        public bool IsAvailable => Available;
        public byte ObjectType(IKingdomQuestPineNativeAxialObject obj) => ((Participant)obj).Type;
        public int ViewKind(IKingdomQuestPineNativeAxialObject obj) => ((Participant)obj).View;
        public byte MobileMode(IKingdomQuestPineNativeAxialObject obj)
        { if (obj is KingdomQuestPineNativeDoorObject) return DoorMobile; SourceMobileReads++; return ((Participant)obj).Mobile; }
        public int StateA18(IKingdomQuestPineNativeAxialObject obj) => ((Participant)obj).State;
        public int MovementState(IKingdomQuestPineNativeAxialObject obj)
        { MovementReads++; return obj is KingdomQuestPineNativeDoorObject ? DoorMovement : ((Participant)obj).Move; }
        public uint FieldSightSquared(IKingdomQuestPineNativeAxialObject obj) => Sight;
        public void QueueMovement(IKingdomQuestPineNativeAxialObject obj) => Events.Add("queue");
        public void TraceDoorSourceName(IKingdomQuestPineNativeAxialObject obj) => Events.Add("trace");
        public void Report(KingdomQuestPineBriefEvent reason, IKingdomQuestPineNativeAxialObject obj) => Events.Add(reason.ToString());
        public bool TryExchangeOther(KingdomQuestPineBriefFamily family, IKingdomQuestPineNativeAxialObject source, IKingdomQuestPineNativeAxialObject candidate)
        { Events.Add(family.ToString()); return OtherAvailable; }
        public void SendPacket(IKingdomQuestPineNativeAxialObject source, byte[] wire)
        { Events.Add("send"); Packets.Add((source, wire)); OnSend?.Invoke(); }
        public KingdomQuestPineNativeDoorObject Door() => new(0, Enumerable.Range(0, 48).Select(i => (byte)(i + 30)).ToArray(), this);
        public KingdomQuestPineNativeEffectObject Effect() => new(0, Enumerable.Range(0, 48).Select(i => (byte)(i + 70)).ToArray(), this, new(_ => { }, (_, _) => { }));
        // No Build/Blast/AI/transport owner is claimed by these packet fixtures.
        public int Mark(KingdomQuestPineNativeSceneObject obj, byte ignore) => throw new NotSupportedException();
        public void FreeFromLists(KingdomQuestPineNativeSceneObject obj) => throw new NotSupportedException();
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
