using System.Security.Cryptography;
using System.Text;
using NextGen.Zone.Data;

static class NativeReciprocalBriefTests
{
    static void Check(bool ok, string name) { if (!ok) throw new Exception("native reciprocal brief: " + name); }
    static KingdomQuestPineNativeSubLayer OriginalTable()
    {
        byte[] raw = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "SubLayerInteract.txt"));
        Check(Convert.ToHexString(SHA256.HashData(raw)).ToLowerInvariant() == KingdomQuestPineNativeSubLayer.OriginalSourceSha256,
            "lossless original Server.zip fixture fingerprint");
        Check(KingdomQuestPineNativeSubLayer.TryParse(new StringReader(Encoding.Latin1.GetString(raw)), out var table), "original matrix source parsed");
        return table;
    }
    public static void Run()
    {
        SourceMatrix(); DirectionAndCharacters(); AbstatePackets(); ActualSceneTraversal();
        Console.WriteLine("PASS: original directional sublayers, reciprocal character/scene briefs, 235-byte batching, abstate ordering/reentrancy and explicit native capacity faults");
    }
    static void SourceMatrix()
    {
        var table = OriginalTable();
        byte[][] send = [ [1,1,1,1,0,1], [0,0,0,1,0,1], [0,0,0,1,1,1], [0,0,0,0,1,1], [0,0,0,0,0,0], [1,0,0,0,1,1] ];
        byte[][] attack = [ [1,0,0,0,0,1], [1,0,0,0,0,1], [0,0,0,0,0,0], [0,0,0,0,0,0], [0,0,0,0,0,0], [1,0,0,0,0,0] ];
        for (byte row = 0; row < 6; row++) for (byte col = 0; col < 6; col++)
        {
            Check(table.TrySendMyBrief(row, col, out byte value) && value == send[row][col], "all 36 source send cells in native row/column orientation");
            Check(table.TryCanAttack(row, col, false, out value) && value == attack[row][col], "all 36 attack cells");
        }
        Check(!table.TrySendMyBrief(6, 0, out _) && !table.TrySendMyBrief(0, 255, out _) &&
            !table.TryCanAttack(255, 255, false, out _) && table.TryCanAttack(255, 255, true, out byte blocked) && blocked == 0,
            "invalid matrix addresses unresolved, but abstate C7 blocks before reading indices");
        var input = Enumerable.Repeat((byte)129, 36).ToArray(); table = new(input, input); input[0] = 0;
        Check(table.TrySendMyBrief(0, 0, out byte original) && original == 129, "table snapshots preserve raw nonzero bytes");
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "SubLayerInteract.txt"), Encoding.Latin1);
        Check(!KingdomQuestPineNativeSubLayer.TryParse(new StringReader(text.Replace("#Record\t1\tRangerStealth", "#Record\t2\tRangerStealth")), out _), "checksum mismatch rejected");
        Check(!KingdomQuestPineNativeSubLayer.TryParse(new StringReader(text.Replace("#ColumnType\tBYTE\tIndex", "#ColumnType\tWORD\tIndex")), out _), "unsupported physical record layout not reinterpreted");
        Check(!KingdomQuestPineNativeSubLayer.TryParse(new StringReader(text[..text.IndexOf("#Table\tCanAttack")]), out _), "both original tables mandatory");
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++) if (lines[i].StartsWith("#Record\t0\tBase"))
        {
            var fields = lines[i].Split('\t'); fields[1] = "77"; fields[9] = "77"; lines[i] = string.Join('\t', fields);
        }
        Check(KingdomQuestPineNativeSubLayer.TryParse(new StringReader(string.Join('\n', lines)), out table) &&
            table.TrySendMyBrief(0, 1, out original) && original == 1, "checksums compare to each other, not to row index; physical order retained");
    }
    static void DirectionAndCharacters()
    {
        var f = new Fixture(); var r = f.Exchange.Reciprocal;
        var source = new Actor(2) { Handle = 0x1234 }; var candidate = new Actor(2) { Handle = 0xA123, Flags = 1 };
        Check(r.TryExchangeCharacter(source, candidate) && f.Events.SequenceEqual(new[] { "direct" }) && r.CharacterCount == 0,
            "Base sends to RangerStealth, reverse table cell suppresses collection");
        source.Flags = 1; candidate.Flags = 0; f.Events.Clear();
        Check(r.TryExchangeCharacter(source, candidate) && f.Events.SequenceEqual(new[] { "fill-char" }) && r.CharacterCount == 1,
            "reverse orientation collects without immediate send");
        source.Mode = 17; candidate.Mode = 18; source.Flags = 255; f.Events.Clear();
        Check(r.TryExchangeCharacter(source, candidate) && f.Events.Count == 0, "mode mismatch exits before invalid flags");
        source.Mode = uint.MaxValue;
        Check(!r.TryExchangeCharacter(source, candidate) && f.Events.SequenceEqual(new[] { "InvalidModeFlags" }), "either wildcard mode bypasses equality, but not table bounds");
        source.Flags = 0; candidate.Mode = uint.MaxValue; source.Mode = 17; f.Events.Clear();
        Check(r.TryExchangeCharacter(source, candidate) && f.Events.SequenceEqual(new[] { "direct", "fill-char" }), "candidate wildcard also permits exchange");
        source.Mode = candidate.Mode = 0; r.InitializeCharacters(); f.Events.Clear();
        f.OnDirect = () => source.Flags = 4;
        Check(r.TryExchangeCharacter(source, candidate) && r.CharacterCount == 0 && f.Events.SequenceEqual(new[] { "direct" }),
            "reverse flags re-read after immediate send mutation");
        source.Flags = 0; f.OnDirect = () => source.Mode = 99; f.Events.Clear();
        Check(r.TryExchangeCharacter(source, candidate) && r.CharacterCount == 1, "mode equality is not rerun after callback");
        source.Mode = 0; f.OnDirect = null; r.InitializeCharacters(); f.Events.Clear(); f.Packets.Clear();
        for (int i = 0; i < 28; i++) { candidate.Seed = (byte)i; Check(r.TryExchangeCharacter(source, candidate), "character fill"); }
        Check(r.CharacterCount == 28 && r.CharacterPacketLength == 0 && f.Packets.Count == 0, "28 character entries wait for next append");
        candidate.Seed = 199; f.Events.Clear();
        Check(r.TryExchangeCharacter(source, candidate) && r.CharacterCount == 1 && r.CharacterPacketLength == 6583 &&
            f.Events.SequenceEqual(new[] { "direct", "send:1C07", "fill-char" }), "entry 29 sends old 28 after direct send, before new fill");
        var wire = f.Packets.Single().Wire;
        Check(wire.Length == 6583 && wire[0] == 7 && wire[1] == 0x1C && wire[2] == 28 && wire[3] == 0 && wire[3 + 27 * 235] == 27,
            "0x1C07 packet uses original 235-byte stride/order");
        Check(r.TrySendCharacters(source) && r.CharacterCount == 1 && r.CharacterPacketLength == 238 &&
            f.Packets[^1].Wire[3..].SequenceEqual(candidate.CharacterPayload()), "explicit Send retains count and full character payload");
        r.InitializeCharacters(); source.Mobile = 0; f.Packets.Clear();
        for (int i = 0; i < 29; i++) Check(r.TryExchangeCharacter(source, candidate), "invisible character append");
        Check(r.CharacterCount == 1 && f.Packets.Count == 0 && r.CharacterPacketLength == 238, "skipped overflow Send discards prior entries but retains last length");
        source.Mobile = 1; source.Type = 8; f.Events.Clear();
        Check(r.TryExchangeCharacter(source, candidate) && r.CharacterCount == 1 && f.Events.SequenceEqual(new[] { "direct" }), "type 8 can send directly but cannot collect type-2 batch");
        source.Type = 2; f.FillCharacterAvailable = false; f.Events.Clear();
        Check(!r.TryExchangeCharacter(source, candidate) && r.CharacterCount == 1 && f.Events.SequenceEqual(new[] { "direct", "fill-char" }), "unknown payload retains earlier send, does not increment count");
        f.FillCharacterAvailable = true; r.InitializeCharacters();
        for (int i = 0; i < 27; i++) r.TryExchangeCharacter(source, candidate);
        f.OnFillCharacter = () => { f.OnFillCharacter = null; Check(r.TryExchangeCharacter(source, candidate), "nested character fill"); };
        Check(r.TryExchangeCharacter(source, candidate) && r.CharacterCount == 29, "nested character fill increments shared byte after each callback");
        int sends = f.Packets.Count; int length = r.CharacterPacketLength;
        Check(r.TrySendCharacters(source) && f.Packets.Count == sends && r.CharacterPacketLength == length &&
            r.CharacterCount == 29 && f.Events[^1] == "CharacterPacketLengthRejected", "oversized reentrant character batch logs and skips Send without changing old length");
        Check(r.TryExchangeCharacter(source, candidate) && r.CharacterCount == 1 && f.Packets.Count == sends,
            "next character overflow resets after rejected length, then fills normally");
    }
    static void AbstatePackets()
    {
        var f = new Fixture(); var r = f.Exchange.Reciprocal;
        var source = new Actor(2) { Flags = 1, Handle = 0x1234 }; var candidate = new Actor(2) { Handle = 0xCAFE, Abstates = 1, Seed = 23 };
        // Reverse visible, forward invisible: queue only, no immediate packet.
        Check(r.TryExchangeAbstates(false, source, candidate) && r.CharacterAbstateObjectCount == 1 && f.Packets.Count == 0, "asymmetric reverse visibility gates collection");
        r.InitializeAbstates(); source.Mobile = 0;
        Check(r.TrySendAbstates(false, source) && r.CharacterAbstateObjectCount == 1, "native Init is no-op for initialized current pointer; invisible Send retains queue");
        source.Mobile = 128;
        Check(r.TrySendAbstates(false, source) && r.CharacterAbstateObjectCount == 0 && r.CharacterAbstateCurrentIndex == 0, "abstate Send clears object queue unlike brief Send");
        var wire = f.Packets.Single().Wire;
        Check(wire.Length == 17 && wire[0] == 0x19 && wire[1] == 0x1C && wire[2] == 0xFE && wire[3] == 0xCA && wire[4] == 1 &&
            wire[5..].SequenceEqual(Enumerable.Repeat((byte)23, 12)), "abstate opcode, WORD native handle, byte count, 12-byte raw records");
        source.Flags = 0; candidate.Flags = 1; f.Events.Clear();
        Check(r.TryExchangeAbstates(false, source, candidate) && f.Events.Count == 0, "reverse zero blocks even otherwise allowed immediate source send");
        candidate.Flags = 0; source.Abstates = 1; f.Events.Clear(); f.Packets.Clear();
        Check(r.TryExchangeAbstates(false, source, candidate) && f.Events.SequenceEqual(new[] { "fill-state", "send:1C19", "fill-state" }) &&
            f.Packets[0].Recipient == candidate && BitConverter.ToUInt16(f.Packets[0].Wire, 2) == source.Handle,
            "source status is sent to candidate before candidate status is queued for source");
        Check(r.TrySendAbstates(false, source), "drain");
        source.Flags = 1; f.Packets.Clear();
        candidate.Abstates = 0;
        Check(r.TryExchangeAbstates(false, source, candidate) && r.CharacterAbstateObjectCount == 1 && r.TrySendAbstates(false, source) &&
            f.Packets.Count == 0 && r.CharacterAbstateObjectCount == 0, "empty status still occupies an object slot, skipped on Send then cleared");
        candidate.Abstates = 28; f.Events.Clear();
        Check(r.TryExchangeAbstates(false, source, candidate) && r.TrySendAbstates(false, source) && f.Packets.Count == 0 &&
            f.Events.SequenceEqual(new[] { "fill-state", "AbstatePacketLengthRejected" }) && r.CharacterAbstateObjectCount == 0,
            "28 status entries request length 341 equal to capacity: native Send logs, skips and clears queue");
        candidate.Abstates = 1;
        for (int i = 0; i < 28; i++) { candidate.Handle = (ushort)i; Check(r.TryExchangeAbstates(false, source, candidate), "28 object slots"); }
        Check(r.CharacterAbstateObjectCount == 28 && !r.TryExchangeAbstates(false, source, candidate) &&
            r.CharacterAbstateObjectCount == 28 && f.Events[^1] == "AbstateObjectCapacity", "29th object hits original >28 bug; no invented out-of-bounds packet or early flush");
        Check(r.TryExchangeAbstates(true, source, candidate) && r.MobAbstateObjectCount == 1 && r.CharacterAbstateObjectCount == 28, "mob and character queues independent");
        Check(r.TrySendAbstates(false, source) && f.Packets.Count == 28 &&
            f.Packets.Select(p => BitConverter.ToUInt16(p.Wire, 2)).SequenceEqual(Enumerable.Range(0, 28).Select(i => (ushort)i)), "all 28 queued objects sent in order with captured handles");
        Check(r.TrySendAbstates(true, source) && r.MobAbstateObjectCount == 0, "mob queue uses same native sender");

        // Shared immediate packet: pp_SetPacketLen failure is ignored here.
        var door = f.Door(); f.DoorAbstates = 1; f.Packets.Clear();
        Check(r.TryExchangeAbstates(false, door, candidate) && f.Packets.Single().Wire.Length == 17 && r.ImmediateAbstatePacketLength == 17,
            "door uses real mobile abstate dependency and native pool handle");
        Check(BitConverter.ToUInt16(f.Packets[0].Wire, 2) == door.NativeHandle, "door handle is not an emulator ID");
        f.DoorAbstates = 28;
        Check(r.TryExchangeAbstates(false, door, candidate) && f.Packets[^1].Wire.Length == 17 && f.Packets[^1].Wire[4] == 28 &&
            r.ImmediateAbstatePacketLength == 17, "immediate packet keeps previous length on exact-capacity rejection");
        var empty = new Fixture(); empty.DoorAbstates = 28;
        Check(empty.Exchange.Reciprocal.TryExchangeAbstates(false, empty.Door(), candidate) && empty.Packets.Single().Wire.Length == 0,
            "first failed immediate length retains original zero rather than repairing source behavior");
        int prior = f.Packets.Count; f.Events.Clear();
        Check(r.TryExchangeAbstates(false, f.Effect(), candidate) && f.Packets.Count == prior && f.Events.Count == 0 &&
            r.ImmediateAbstatePacketLength == 5, "effect's literal no-op virtual yields empty status without mobile dependency");
        ReentrantAbstates();
    }
    static void ReentrantAbstates()
    {
        var f = new Fixture(); var r = f.Exchange.Reciprocal; var source = new Actor(2) { Flags = 1 }; var candidate = new Actor(2) { Abstates = 1 };
        for (int i = 0; i < 3; i++) Check(r.TryExchangeAbstates(false, source, candidate), "seed queue");
        f.OnSend = () => { f.OnSend = null; Check(r.TryExchangeAbstates(false, source, candidate), "append during send"); };
        Check(r.TrySendAbstates(false, source) && f.Packets.Count == 4 && r.CharacterAbstateObjectCount == 0,
            "Send re-reads shared object count and visits callback-added entry");
        r.TryExchangeAbstates(false, source, candidate); r.TryExchangeAbstates(false, source, candidate);
        f.OnSend = () => throw new InvalidOperationException("send test"); bool threw = false;
        try { r.TrySendAbstates(false, source); } catch (InvalidOperationException) { threw = true; }
        Check(threw && r.CharacterAbstateObjectCount == 2 && r.CharacterAbstateCurrentIndex == 0, "exception retains queued count and current pointer");
        f.OnSend = null; r.TrySendAbstates(false, source);
        for (int i = 0; i < 27; i++) r.TryExchangeAbstates(false, source, candidate);
        f.OnFillState = () => { f.OnFillState = null; Check(r.TryExchangeAbstates(false, source, candidate), "nested fill of shared last slot"); };
        Check(r.TryExchangeAbstates(false, source, candidate) && r.CharacterAbstateObjectCount == 29,
            "nested fill increments shared count twice after writing same slot, matching native late increment");
        int packets = f.Packets.Count;
        Check(r.TrySendAbstates(false, source) && r.CharacterAbstateObjectCount == 29 && f.Packets.Count == packets,
            "Send rejects count above 28 without clearing");
        Check(r.TryExchangeAbstates(false, source, candidate) && r.CharacterAbstateObjectCount == 1 && f.Packets.Count == packets,
            "next exchange's >28 branch skips Send then resets and fills slot zero");
    }
    static void ActualSceneTraversal()
    {
        var f = new Fixture(); var state = new KingdomQuestPineNativeListCheckState(new(0,0,0,0));
        var flags = Enumerable.Range(0,7).Select(i => new KingdomQuestPineNativeAxialFlag((ushort)i, (_,_) => { })).ToArray();
        foreach (var flag in flags) flag.SetNativeAxisCounters(state.SnapshotCounters().Previous());
        var map = new KingdomQuestPineNativeFieldMapAxes(new(new byte[12],20000,40000), flags, _ => { }, (_,_) => { });
        var traversal = new KingdomQuestPineNativeMapTraversal(state, (_,_) => throw new Exception("traversal fault"), (_,_) => throw new Exception("relink"));
        var player = new Actor(2) { View = 3, X = 5000, Y = 6000, Map = map.Identity };
        var door = f.Door(); door.SetNativeMap(map.Identity); door.SetNativePosition(5000,5000);
        var effect = f.Effect(); effect.SetNativeMap(map.Identity); effect.SetNativePosition(5000,7000);
        foreach (var obj in new IKingdomQuestPineNativeAxialObject[] { player, door, effect })
        {
            obj.SetNativeAxisCounters(state.SnapshotCounters().Previous());
            Check(obj.Axes.X.TryAppend(flags[0].Axes.X, (_,_) => { }) && obj.Axes.Y.TryAppend(flags[0].Axes.Y, (_,_) => { }), "link real scene nodes");
        }
        f.Exchange.SourceSightSquared = uint.MaxValue; f.DoorAbstates = 2;
        Check(f.Exchange.TryVisitNormalMap(traversal, door, 0, out bool done) && done && f.Packets.Count == 2 &&
            f.Packets[0].Recipient == player && f.Packets[0].Wire.SequenceEqual(door.SnapshotBriefWire()) &&
            f.Packets[1].Wire.Length == 29 && f.Exchange.Reciprocal.CharacterCount == 0, "door-origin map traversal sends individual door and source statuses to visited player");
        f.Packets.Clear();
        Check(f.Exchange.TryVisitNormalMap(traversal, effect, 0, out done) && done && f.Packets.Count == 1 &&
            f.Packets[0].Recipient == player && f.Packets[0].Wire.SequenceEqual(effect.SnapshotBriefWire()) && state.Depth == -1,
            "effect-origin traversal sends individual effect, no fabricated status packet");
        door.SetNativeMode(0,4); player.Flags = 5; f.Packets.Clear();
        Check(f.Exchange.TryVisitNormalMap(traversal, door, 0, out done) && done && f.Packets.Count == 0,
            "original GMDoor row suppresses door->GMPlayer briefs without conflating traversal layer");
        f.Available = false;
        Check(!f.Exchange.Reciprocal.TryExchangeCharacter(door,player), "unbound native dependencies remain unresolved");
    }
    sealed class Actor : IKingdomQuestPineNativeAxialObject
    {
        public byte Type, Flags, Mobile = 1, Abstates, Seed; public ushort Handle; public int View = 1, X, Y; public uint Mode;
        public KingdomQuestPineEffectMapIdentity Map;
        public Actor(byte type) { Type = type; Axes = new(this,true,()=>X,()=>Y); }
        public byte[] CharacterPayload() => Enumerable.Range(0,235).Select(i => unchecked((byte)(Seed+i))).ToArray();
        public KingdomQuestPineEffectLocation Location => new(Map,Mode,X,Y,0);
        public KingdomQuestPineNativeAxes Axes { get; }
        public object RangeObject => this;
        public ulong LayerRegistrationNumber => 0;
        public byte LayerObjectViewType => 0;
        public bool HasNativeAxisCounters => true;
        public KingdomQuestPineNativeAxisCounters AxisCounters { get; private set; }
        public void SetNativeAxisCounters(KingdomQuestPineNativeAxisCounters value) => AxisCounters = value;
    }
    sealed class Fixture : IKingdomQuestPineNativeBriefServices, IKingdomQuestPineNativeReciprocalServices,
        IKingdomQuestPineNativeDoorServices, IKingdomQuestPineNativeEffectServices
    {
        public readonly KingdomQuestPineNativeBriefExchange Exchange;
        public readonly List<string> Events = new();
        public readonly List<(IKingdomQuestPineNativeAxialObject Recipient,byte[] Wire)> Packets = new();
        public bool Available = true, FillCharacterAvailable = true; public byte DoorAbstates;
        public Action OnDirect, OnSend, OnFillState, OnFillCharacter;
        public Fixture() { Exchange = new(this,this,OriginalTable()); }
        public bool IsAvailable => Available;
        public byte ObjectType(IKingdomQuestPineNativeAxialObject obj) => ((Actor)obj).Type;
        public int ViewKind(IKingdomQuestPineNativeAxialObject obj) => ((Actor)obj).View;
        public byte MobileMode(IKingdomQuestPineNativeAxialObject obj) => obj is KingdomQuestPineNativeDoorObject ? (byte)1 : ((Actor)obj).Mobile;
        public int StateA18(IKingdomQuestPineNativeAxialObject obj) => 0;
        public int MovementState(IKingdomQuestPineNativeAxialObject obj) => 0;
        public uint FieldSightSquared(IKingdomQuestPineNativeAxialObject obj) => uint.MaxValue;
        public void QueueMovement(IKingdomQuestPineNativeAxialObject obj) => Events.Add("movement");
        public void TraceDoorSourceName(IKingdomQuestPineNativeAxialObject obj) => Events.Add("trace");
        public void SendPacket(IKingdomQuestPineNativeAxialObject source, byte[] wire)
        { Events.Add("send:"+(wire.Length>=2 ? BitConverter.ToUInt16(wire).ToString("X4") : "empty")); Packets.Add((source,wire)); OnSend?.Invoke(); }
        public bool TryExchangeOther(KingdomQuestPineBriefFamily family, IKingdomQuestPineNativeAxialObject source, IKingdomQuestPineNativeAxialObject candidate)
            => throw new Exception("unexpected delegated family "+family);
        public void Report(KingdomQuestPineBriefEvent reason, IKingdomQuestPineNativeAxialObject obj) => Events.Add(reason.ToString());
        public void Report(KingdomQuestPineReciprocalEvent reason, IKingdomQuestPineNativeAxialObject obj) => Events.Add(reason.ToString());
        public byte ModeFlags(IKingdomQuestPineNativeAxialObject obj) => ((Actor)obj).Flags;
        public ushort NativeHandle(IKingdomQuestPineNativeAxialObject obj) => ((Actor)obj).Handle;
        public bool TrySendMyBrief(IKingdomQuestPineNativeAxialObject source, IKingdomQuestPineNativeAxialObject recipient)
        { Events.Add("direct"); OnDirect?.Invoke(); return true; }
        public bool TryFillCharacterBrief235(IKingdomQuestPineNativeAxialObject obj, Span<byte> destination)
        { Events.Add("fill-char"); if (!FillCharacterAvailable) return false; ((Actor)obj).CharacterPayload().CopyTo(destination); OnFillCharacter?.Invoke(); return true; }
        public bool TryFillAbstates(IKingdomQuestPineNativeAxialObject obj, Span<byte> entries, ref byte count)
        {
            Events.Add("fill-state");
            count = obj is KingdomQuestPineNativeDoorObject ? DoorAbstates : ((Actor)obj).Abstates;
            entries[..(count*12)].Fill(obj is KingdomQuestPineNativeDoorObject ? (byte)71 : ((Actor)obj).Seed);
            OnFillState?.Invoke(); return true;
        }
        public KingdomQuestPineNativeDoorObject Door() => new(0, Enumerable.Repeat((byte)91,48).ToArray(),this);
        public KingdomQuestPineNativeEffectObject Effect() => new(0, Enumerable.Repeat((byte)173,48).ToArray(),this,new(_=>{},(_,_)=>{}));
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
