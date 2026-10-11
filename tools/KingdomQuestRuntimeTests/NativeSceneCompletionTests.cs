using System.Text;
using NextGen.Zone.Data;

static class NativeSceneCompletionTests
{
    static void Check(bool ok, string name) { if (!ok) throw new Exception("native scene completion: " + name); }
    static KingdomQuestPineNativeAiState Ai()
    {
        return new(new object(), Enumerable.Range(0, 8).Select(i =>
            new KingdomQuestPineNativeAiString(Enumerable.Repeat((byte)(0xA0 + i), i % 2 == 0 ? 16 : 64).ToArray(),
                i + 1, (uint)(0x12340000 + i))).ToArray());
    }
    static void ClearedAi(KingdomQuestPineNativeAiState ai)
    {
        Check(ai.Script == null, "script pointer reset");
        for (int i = 0; i < 8; i++)
        {
            var slot = ai.Slot(i); var bytes = slot.SnapshotStorage();
            Check(slot.Length == 0 && slot.Capacity == (i % 2 == 0 ? 15 : 63) && bytes[0] == 0 &&
                bytes[1..].All(b => b == 0xA0 + i) && slot.Field18 == 0x12340000 + i,
                "AI inline/heap capacity, tail bytes and adjacent DWORD preserved at slot " + i);
        }
    }
    public static void Run()
    {
        SightAndDispatch(); CompletionOrder(); Movement(); BuildBlastIntegration();
        Console.WriteLine("PASS: native door/effect completion, persistent AI reset, DWORD sight, explicit map dispatch, partial failures, movement state 3 and concrete Build/Blast integration");
    }
    static void SightAndDispatch()
    {
        var f = new Fixture(); var door = f.Door();
        Check(door.NativeSightState == null && door.NativeMobileMode == null && door.NativeByte1F0C == null &&
            door.NativeMovement == null && door.NativeFlags1E78 == 0, "constructor does not invent unwritten source state");
        Check(f.Completion.TryFieldSightSquared(door, out uint sight) && sight == 0 &&
            f.Completion.TryVisitMap(door, out bool result) && !result, "null map is known zero sight/native false");
        door.SetNativeMap(f.Map.Identity);
        Check(!f.Completion.TryFieldSightSquared(door, out _), "uninitialized sight remains unresolved");
        f.Binding.HasMapData = false;
        Check(f.Completion.TryFieldSightSquared(door, out sight) && sight == 0, "missing data guard precedes unknown object sight");
        f.Binding.HasMapData = true; door.NativeSightState = 0x101;
        Check(f.Completion.TryFieldSightSquared(door, out sight) && sight == 10000, "sight is DWORD, low byte one is NOT unlimited");
        door.NativeSightState = 1;
        Check(f.Completion.TryFieldSightSquared(door, out sight) && sight == uint.MaxValue, "exact object one is unlimited");
        door.NativeSightState = 0; f.Binding.MapSightState = 1;
        Check(f.Completion.TryFieldSightSquared(door, out sight) && sight == uint.MaxValue, "map exact one is unlimited");
        f.Binding.MapSightState = 0x101; f.Binding.SightDistance = 65537;
        Check(f.Completion.TryFieldSightSquared(door, out sight) && sight == 131073, "map is also DWORD, square wraps uint32");
        Check(!f.Completion.TryBindMap(f.Binding), "duplicate identity cannot silently replace binding");
        door.SetNativeMap(new(f.Map.Identity.SnapshotName12(), 20000, 40000));
        Check(!f.Completion.TryFieldSightSquared(door, out _) && !f.Completion.TryVisitMap(door, out _),
            "equal map name is not actual pointer identity");
        door.SetNativeMap(f.Map.Identity); f.Binding.MemberFunctionAddress = 0; f.Binding.ThisAdjustment = 99;
        Check(f.Completion.TryVisitMap(door, out result) && !result, "null member pointer returns false before this adjustment");
        f.Binding.MemberFunctionAddress = 0x0054BE00;
        Check(!f.Completion.TryVisitMap(door, out _), "nonzero member this-adjustment unresolved");
        f.Binding.ThisAdjustment = 0; f.Binding.MemberFunctionAddress++;
        Check(!f.Completion.TryVisitMap(door, out _), "unknown member target never guessed from map dimensions");
        f.Binding.MemberFunctionAddress = 0x0054BE00;
        Check(f.Completion.TryVisitMap(door, out result) && !result && f.Diagnostics.Single() == "SaveFieldInformation",
            "normal-map detached object is known native false, not unresolved");
    }
    static void CompletionOrder()
    {
        var f = new Fixture(); var door = f.Door(); var effect = f.Effect(); var player = new Actor(f.Map.Identity);
        door.NativeMobileMode = 1; door.NativeMovement = new(new byte[64]);
        Check(f.Exchange.TryVisit(door, player, 0) && f.Exchange.TrySendDoors(player) &&
            f.Exchange.TryVisit(effect, player, 0) && f.Exchange.TrySendEffects(player), "seed both accumulators and prior lengths");
        f.Exchange.Reciprocal.TryExchangeCharacter(player, player);
        f.Exchange.Reciprocal.TryExchangeAbstates(false, player, player);
        Check(f.Exchange.Reciprocal.CharacterCount == 1 && f.Exchange.Reciprocal.CharacterAbstateObjectCount == 1, "seed unrelated buffers");
        door.SetNativeMode(78, 5); door.NativeWord168 = 456; door.NativeFlags1E78 = 0xFF;
        Check(!f.Completion.TryDoorBuildComplete(door, 0) && door.Mode == 78 && door.NativeWord168 == 456 &&
            f.Exchange.DoorCount == 1, "unbound AI rejected before any completion writes");
        door.NativeAi = Ai(); door.NativeSightState = 0x101; door.NativeMobileMode = 0; door.NativeByte1F0C = 211;
        door.SetNativePosition(5000, 7000); f.Attach(door); f.Attach(player);
        int sent = f.Packets.Count;
        f.OnSend = () =>
        {
            ClearedAi(door.NativeAi);
            Check(door.Mode == 0 && door.ModeFlags == 0 && door.NativeWord168 == 0 && door.NativeMobileMode == 1 &&
                door.NativeByte1F0C == 0 && door.NativeFlags1E78 == 0xFF && f.Exchange.SourceSightSquared == 10000 &&
                f.Exchange.DoorCount == 0 && f.Exchange.EffectCount == 1 &&
                f.Exchange.Reciprocal.CharacterCount == 1 && f.Exchange.Reciprocal.CharacterAbstateObjectCount == 1,
                "AI/fields/own buffer/global sight precede visitor; no other buffer resets");
            door.NativeWord168 = 999; door.NativeFlags1E78 = 0xAF;
            // Shared source sight is NOT restored after a nested completion.
            var nested = f.Effect(); nested.NativeAi = Ai();
            Check(f.Completion.TryEffectBlastComplete(nested, ushort.MaxValue), "nested completion with null map");
        };
        Check(f.Completion.TryDoorBuildComplete(door, ushort.MaxValue) && door.NativeWord168 == 0 && door.NativeFlags1E78 == 0xAD &&
            f.Packets.Count == sent + 1 && f.Packets[^1].Wire.SequenceEqual(door.SnapshotBriefWire()) &&
            ReferenceEquals(f.Packets[^1].Recipient, player) && f.Exchange.SourceSightSquared == 0,
            "actual normal-map reciprocal door send, ignored handle, post-visit tail and shared nested sight");
        f.OnSend = null;
        Check(f.Exchange.DoorPacketLength == 51 && f.Exchange.EffectPacketLength == 51 && f.Exchange.EffectCount == 0,
            "both Init operations retain previous packet length and never auto-flush");
        // A known false traversal still executes the completion tail.
        var detached = f.Door(); detached.NativeAi = Ai(); detached.NativeFlags1E78 = 0xFF;
        detached.SetNativeMap(f.Map.Identity); detached.NativeSightState = 0;
        Check(f.Completion.TryDoorBuildComplete(detached, 3) && detached.NativeFlags1E78 == 0xFD, "native false is deliberately ignored");
        // Unsupported dispatch preserves the prefix and does not run the tail.
        f.Binding.MemberFunctionAddress = 123; door.NativeAi = Ai(); door.NativeFlags1E78 = 0xFF; door.SetNativeMode(71, 5);
        Check(!f.Completion.TryDoorBuildComplete(door, 1) && door.Mode == 0 && door.NativeFlags1E78 == 0xFF &&
            door.NativeAi.Script == null && f.Exchange.SourceSightSquared == 10000, "unresolved dispatch retains earlier writes only");
        f.Binding.MemberFunctionAddress = 0x0054BE00;
        f.OnSend = () => { door.NativeWord168 = 321; throw new InvalidOperationException("completion callback"); };
        bool threw = false;
        try { f.Completion.TryDoorBuildComplete(door, 0); } catch (InvalidOperationException) { threw = true; }
        Check(threw && door.NativeWord168 == 321 && door.NativeFlags1E78 == 0xFF && f.State.Depth == -1,
            "exception preserves callback writes, skips tail and unwinds traversal depth");
        f.OnSend = null;
        // Effect completion clears only its own accumulator; source type 9
        // sends individual 0x1C11 through the character branch, not a batch.
        f.Exchange.TryVisit(door, player, 0); f.Exchange.TryVisit(effect, player, 0);
        effect.NativeAi = Ai(); effect.NativeSightState = 0; effect.SetNativeMode(123, 5);
        effect.SetNativePosition(5000, 8000); f.Attach(effect); sent = f.Packets.Count;
        Check(f.Completion.TryEffectBlastComplete(effect, 0) && effect.Mode == 0 && effect.ModeFlags == 0 &&
            f.Exchange.DoorCount == 1 && f.Exchange.EffectCount == 0 && f.Packets.Count == sent + 1 &&
            f.Packets[^1].Wire.SequenceEqual(effect.SnapshotBriefWire()), "effect completion actual individual visibility, independent buffer");
        ClearedAi(effect.NativeAi);
        f.Binding.HasMapData = false; f.Binding.MemberFunctionAddress = 0; effect.NativeSightState = null;
        Check(f.Completion.TryEffectBlastComplete(effect, 0) && f.Exchange.SourceSightSquared == 0,
            "no map data and null dispatch require no invented sight state");
    }
    static void Movement()
    {
        var f = new Fixture(); var door = f.Door();
        Check(!KingdomQuestPineNativeSceneCompletion.TryFinishDoorBuildMovement(door), "unbound MoveManager stays unresolved");
        byte[] seed = Enumerable.Range(0, 64).Select(i => (byte)(i + 100)).ToArray();
        door.NativeMovement = new(seed); seed[63] = 0; door.NativeMovement.Write(0, 3);
        var before = door.NativeMovement.Snapshot(); door.SetNativePosition(-7, int.MinValue);
        Check(KingdomQuestPineNativeSceneCompletion.TryFinishDoorBuildMovement(door) && before.SequenceEqual(door.NativeMovement.Snapshot()),
            "state 3 retains all 64 bytes, including prior XY and owner pointer");
        door.NativeMovement.Write(0, 0x10000003); before = door.NativeMovement.Snapshot();
        Check(KingdomQuestPineNativeSceneCompletion.TryFinishDoorBuildMovement(door), "only exact DWORD 3 bypasses reset");
        for (int offset = 0; offset < 64; offset += 4)
        {
            uint expected = offset == 0 || offset == 8 || (offset >= 0x1C && offset <= 0x2C) ? 0 :
                offset == 0x14 ? unchecked((uint)-7) : offset == 0x18 ? 0x80000000 : BitConverter.ToUInt32(before, offset);
            Check(door.NativeMovement.Read(offset) == expected, "exact MoveManager DWORD write mask at " + offset);
        }
        door.NativeAi = Ai(); door.NativeMobileMode = 128; door.NativeByte1F0C = 99; door.NativeSightState = 77;
        door.NativeMovement.Write(0, 3); var ai = door.NativeAi; before = door.NativeMovement.Snapshot();
        door.InitializeForAllocation();
        Check(ReferenceEquals(door.NativeAi, ai) && ai.Script != null && door.NativeMobileMode == 128 && door.NativeByte1F0C == 99 &&
            door.NativeSightState == 77 && before.SequenceEqual(door.NativeMovement.Snapshot()), "pool Init preserves newly modeled persistent fields");
        var player = new Actor(f.Map.Identity); f.Events.Clear();
        Check(f.Exchange.TryVisit(door, player, 0) && f.Events.SequenceEqual(new[] { "queue", "trace" }),
            "brief dispatcher reads concrete mobile and movement state without fallback services");
        door.NativeMobileMode = 0; f.Events.Clear();
        Check(f.Exchange.TryVisit(door, player, 0) && f.Events.Count == 0, "concrete mobile zero filters before movement");
    }
    static void BuildBlastIntegration()
    {
        var f = new Fixture(); var door = f.Door(); var effect = f.Effect(); var bridge = new Bridge(f, door, effect);
        door.NativeCompletion = effect.NativeCompletion = f.Completion;
        door.NativeAi = Ai(); effect.NativeAi = Ai(); door.NativeSightState = effect.NativeSightState = 0;
        door.NativeMovement = new(Enumerable.Repeat((byte)0xB5, 64).ToArray());
        var player = new Actor(f.Map.Identity); f.Attach(player);
        var vars = new KingdomQuestPineVariableStack(); vars.TryPush("Door1", out _); vars.TryPush("EffDoor1", out _);
        Check(KingdomQuestHoneyingExternalPlanBuilder.TryBuild("KQ/Honeying", 12,
            "doorbuild Door1 \"KQ_SlimeGate\" 9860 6094  272 1000 \"Normal\".", out var site) &&
            KingdomQuestHoneyingDoorBuildNativePlan.TryBuild(site, out _), "original door site");
        KingdomQuestHoneyingDoorBuildNativePlan.TryBuild(site, out var plan);
        f.OnSend = () =>
        {
            Check(door.IsMarked && door.NativeMovement.State == 0xB5B5B5B5 && door.NativeAi.Script == null,
                "Build sends after marking and AI reset, but before movement tail");
            Check(f.Registry.TryMoveTo(door, 11000, 8000, 5), "callback moves through real map axes");
        };
        Check(KingdomQuestPineDoorBuildRuntime.TryStep(plan, vars, bridge, out bool done) && done &&
            door.NativeMovement.State == 0 && door.NativeMovement.Read(0x14) == 11000 && door.NativeMovement.Read(0x18) == 8000 &&
            BitConverter.ToInt32(door.SnapshotLoginLocation21(), 12) == 9860 && door.NativeWord168 == 0,
            "source command -> concrete Build -> completion -> post-callback current XY, not login XY");
        ClearedAi(door.NativeAi); f.OnSend = null;
        Check(KingdomQuestHoneyingExternalPlanBuilder.TryBuild("KQ/Honeying", 18,
            "effectobj EffDoor1 Door1 \"KQ_SlimeGate\" 3600000 1000.", out site), "original effect site");
        KingdomQuestHoneyingEffectNativePlan.TryBuild(site, vars, out var blast);
        Check(KingdomQuestPineEffectRuntime.TryStep(blast, vars, bridge, out done) && done && effect.IsMarked &&
            effect.Parent == door && effect.Location.X == 11000 && effect.Routine.Deadline == 36010 &&
            f.Packets[^1].Wire.SequenceEqual(effect.SnapshotBriefWire()), "source command -> concrete Blast -> real completion/visibility");
        ClearedAi(effect.NativeAi);
        // Direct lifecycle callers can distinguish unresolved from native error
        // zero. No legacy callback is used after selecting the concrete owner.
        f.Binding.MemberFunctionAddress = 123; door.NativeMovement.Write(0, 9); door.NativeFlags1E78 = 0xFF;
        f.Registry.FreeFromLists(door);
        Check(!door.TryBuild(bridge.LastDoor, out int error) && error == 0 && door.IsMarked &&
            door.NativeMovement.State == 9 && door.NativeFlags1E78 == 0xFF,
            "Build propagates unresolved completion without movement tail or false native failure code");
        f.Registry.FreeFromLists(effect);
        Check(!effect.TryBlast(bridge.LastEffect, out error) && error == 0 && effect.IsMarked,
            "Blast propagates unresolved completion retaining prior source mutations");
        f.Binding.MemberFunctionAddress = 0; door.NativeMovement = null;
        f.Registry.FreeFromLists(door);
        Check(!door.TryBuild(bridge.LastDoor, out _) && door.NativeFlags1E78 == 0xFD,
            "missing movement binding fails after successful completion and its tail");
    }
    sealed class Actor : IKingdomQuestPineNativeAxialObject
    {
        public Actor(KingdomQuestPineEffectMapIdentity map)
        { Location = new(map, 0, 5000, 5000, 0); Axes = new(this, true, () => Location.X, () => Location.Y); }
        public KingdomQuestPineEffectLocation Location { get; }
        public KingdomQuestPineNativeAxes Axes { get; }
        public object RangeObject => this;
        public ulong LayerRegistrationNumber => 0;
        public byte LayerObjectViewType => 0;
        public bool HasNativeAxisCounters => true;
        public KingdomQuestPineNativeAxisCounters AxisCounters { get; private set; }
        public void SetNativeAxisCounters(KingdomQuestPineNativeAxisCounters value) => AxisCounters = value;
    }
    // Real production marking/movement/axes, dispatcher, reciprocal packets,
    // completion and Build/Blast; explicit synthetic player and transport.
    sealed class Fixture : IKingdomQuestPineNativeBriefServices, IKingdomQuestPineNativeReciprocalServices,
        IKingdomQuestPineNativeDoorServices, IKingdomQuestPineNativeEffectServices
    {
        public readonly KingdomQuestPineNativeListCheckState State = new(new(0, 0, 0, 0));
        public readonly KingdomQuestPineNativeFieldMapAxes Map;
        public readonly KingdomQuestPineNativeMapRegistry Registry;
        public readonly KingdomQuestPineNativeCompletionMap Binding;
        public readonly KingdomQuestPineNativeSceneCompletion Completion;
        public readonly KingdomQuestPineNativeBriefExchange Exchange;
        public readonly List<string> Events = new(), Diagnostics = new();
        public readonly List<(IKingdomQuestPineNativeAxialObject Recipient, byte[] Wire)> Packets = new();
        readonly KingdomQuestPineNativeAxialFlag[] flags;
        readonly HashSet<IKingdomQuestPineNativeAxialObject> linked = new();
        public Action OnSend;
        uint tick = 10;
        public Fixture()
        {
            flags = Enumerable.Range(0, 7).Select(i => new KingdomQuestPineNativeAxialFlag((ushort)i, (_, _) => { })).ToArray();
            foreach (var flag in flags) flag.SetNativeAxisCounters(State.SnapshotCounters().Previous());
            Map = new(new(new byte[12], 20000, 40000), flags, _ => { }, (_, _) => { });
            Registry = new(new[] { Map }, () => State.SnapshotCounters(),
                (e, _) => throw new Exception("unexpected axis diagnostic: " + e));
            var traversal = new KingdomQuestPineNativeMapTraversal(State, (e, _) => Diagnostics.Add(e.ToString()),
                (_, _) => throw new Exception("unexpected relink"));
            Check(KingdomQuestPineNativeSubLayer.TryParse(new StringReader(File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "Fixtures", "SubLayerInteract.txt"), Encoding.Latin1)), out var layers), "source matrix");
            Exchange = new(this, this, layers); Completion = new(Exchange, traversal);
            Binding = new(Map.Identity, true, 0, 100, 0x0054BE00, 0);
            Check(Completion.TryBindMap(Binding), "bind original normal-map member target");
        }
        public void Attach(IKingdomQuestPineNativeAxialObject obj)
        {
            if (obj is KingdomQuestPineNativeSceneObject scene) scene.SetNativeMap(Map.Identity);
            if (!linked.Add(obj)) return;
            obj.SetNativeAxisCounters(State.SnapshotCounters().Previous());
            Check(obj.Axes.X.TryAppend(flags[0].Axes.X, (_, _) => { }) && obj.Axes.Y.TryAppend(flags[0].Axes.Y, (_, _) => { }), "attach axes");
        }
        public KingdomQuestPineNativeDoorObject Door() => new(0, new byte[48], this);
        public KingdomQuestPineNativeEffectObject Effect() => new(0, new byte[48], this, new(_ => { }, (_, _) => { }));
        public bool IsAvailable => true;
        public byte ObjectType(IKingdomQuestPineNativeAxialObject obj) => obj is Actor ? (byte)2 : throw new Exception("unknown type");
        public int ViewKind(IKingdomQuestPineNativeAxialObject obj) => 1;
        public byte MobileMode(IKingdomQuestPineNativeAxialObject obj) => obj is Actor ? (byte)1 : throw new Exception("unbound native mobile");
        public int StateA18(IKingdomQuestPineNativeAxialObject obj) => 0;
        public int MovementState(IKingdomQuestPineNativeAxialObject obj) => obj is Actor ? 0 : throw new Exception("unbound native movement");
        public uint FieldSightSquared(IKingdomQuestPineNativeAxialObject obj)
        { Check(Completion.TryFieldSightSquared((KingdomQuestPineNativeSceneObject)obj, out uint sight), "bound candidate sight"); return sight; }
        public void QueueMovement(IKingdomQuestPineNativeAxialObject obj) => Events.Add("queue");
        public void TraceDoorSourceName(IKingdomQuestPineNativeAxialObject obj) => Events.Add("trace");
        public void Report(KingdomQuestPineBriefEvent reason, IKingdomQuestPineNativeAxialObject obj) => throw new Exception(reason.ToString());
        public bool TryExchangeOther(KingdomQuestPineBriefFamily family, IKingdomQuestPineNativeAxialObject source,
            IKingdomQuestPineNativeAxialObject candidate) => throw new NotSupportedException();
        public void SendPacket(IKingdomQuestPineNativeAxialObject recipient, byte[] wire)
        { Packets.Add((recipient, wire)); OnSend?.Invoke(); }
        public byte ModeFlags(IKingdomQuestPineNativeAxialObject obj) => 0;
        public ushort NativeHandle(IKingdomQuestPineNativeAxialObject obj) => 100;
        public bool TrySendMyBrief(IKingdomQuestPineNativeAxialObject source, IKingdomQuestPineNativeAxialObject recipient) => true;
        public bool TryFillCharacterBrief235(IKingdomQuestPineNativeAxialObject obj, Span<byte> target) { target.Clear(); return true; }
        public bool TryFillAbstates(IKingdomQuestPineNativeAxialObject obj, Span<byte> entries, ref byte count) { count = 0; return true; }
        public void Report(KingdomQuestPineReciprocalEvent reason, IKingdomQuestPineNativeAxialObject obj) => throw new Exception(reason.ToString());
        public int Mark(KingdomQuestPineNativeSceneObject obj, byte ignore)
        { Check(Registry.TryMark(obj, ignore, out int error), "source marking"); return error; }
        public void FreeFromLists(KingdomQuestPineNativeSceneObject obj) => throw new NotSupportedException();
        public KingdomQuestPineNativeDoorMobData FindMobData(ushort id) => new(0xDEADBEEF);
        public void ReportMissingMobData() => throw new NotSupportedException();
        public void BuildComplete(KingdomQuestPineNativeDoorObject obj, ushort handle) => throw new Exception("legacy completion fallback forbidden");
        public void FinishBuildMovement(KingdomQuestPineNativeDoorObject obj) => throw new Exception("legacy movement fallback forbidden");
        public KingdomQuestMapCollision GetCollision(KingdomQuestPineNativeDoorObject obj) => throw new NotSupportedException();
        public void BroadcastDoorAction(KingdomQuestPineNativeDoorObject obj, byte[] wire) => throw new NotSupportedException();
        public uint CurrentNativeTick => tick++;
        public void BlastComplete(KingdomQuestPineNativeEffectObject obj, ushort handle) => throw new Exception("legacy blast fallback forbidden");
        public void ReportOutOfBounds(KingdomQuestPineNativeEffectObject obj) => throw new NotSupportedException();
        public void MoveTo(KingdomQuestPineNativeEffectObject obj, int x, int y, int argument) => throw new NotSupportedException();
        public void Unmark(KingdomQuestPineNativeEffectObject obj, int when, int broadcast, int reason) => throw new NotSupportedException();
        public bool HasRoutineScript(KingdomQuestPineNativeEffectObject obj) => throw new NotSupportedException();
        public void RunRoutineScript(KingdomQuestPineNativeEffectObject obj, ushort handle, KingdomQuestPineEffectMapIdentity map) => throw new NotSupportedException();
    }
    sealed class Bridge(Fixture fixture, KingdomQuestPineNativeDoorObject door, KingdomQuestPineNativeEffectObject effect)
        : IKingdomQuestPineDoorBuildOwner, IKingdomQuestPineEffectOwner
    {
        public KingdomQuestPineDoorBuildRequest LastDoor;
        public KingdomQuestPineEffectBlastRequest LastEffect;
        public bool IsAvailable => true;
        public object Allocate(byte type, out ushort handle)
        { var obj = type == 7 ? (KingdomQuestPineNativeSceneObject)door : effect; handle = obj.NativeHandle; return obj; }
        public KingdomQuestPineTokenValue ResolveDestination(KingdomQuestPineVariableStack vars, string id)
        { vars.TryFind(id, out var token); return token; }
        public ushort FindMobId(string name) => 1091;
        public byte[] GetCurrentMapName12() => fixture.Map.Identity.SnapshotName12();
        public int Build(object obj, KingdomQuestPineDoorBuildRequest request)
        { LastDoor = request; Check(door.TryBuild(request, out int error), "Build resolved"); return error; }
        public object ResolveParent(byte[] token) => door;
        public bool ParentHasMap(object obj) => door.Map != null;
        public int Blast(object obj, KingdomQuestPineEffectBlastRequest request)
        { LastEffect = request; Check(effect.TryBlast(request, out int error), "Blast resolved"); return error; }
        public bool Free(ushort handle, int when, int reason) => throw new Exception("unexpected free");
        public void ReportFailure(KingdomQuestPineDoorBuildFailure failure, int error) => throw new Exception(failure.ToString());
        public void ReportAllocationFailure() => throw new Exception("unexpected allocation failure");
        public void ReportDestinationFailure() => throw new Exception("unexpected destination failure");
    }
}
