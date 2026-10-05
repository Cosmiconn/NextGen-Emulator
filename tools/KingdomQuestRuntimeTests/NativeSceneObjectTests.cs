using System.Text;
using NextGen.Zone.Data;

static class NativeSceneObjectTests
{
    static void Check(bool ok, string message)
    { if (!ok) throw new Exception("native scene objects: " + message); }
    static byte[] Name(string value)
    { var bytes = new byte[12]; Encoding.ASCII.GetBytes(value).CopyTo(bytes, 0); return bytes; }
    static KingdomQuestHoneyingExternalPlan Site(int line, string command)
    {
        Check(KingdomQuestHoneyingExternalPlanBuilder.TryBuild("KQ/Honeying", line, command, out var plan), "source site");
        return plan;
    }
    public static void Run()
    {
        var events = new List<string>();
        var manager = new KingdomQuestPineNativeObjectManager(t => throw new Exception("unexpected type"),
            (h,r) => events.Add($"missing:{h}:{r}"));
        var services = new Services(events, manager);
        var seed = Enumerable.Repeat((byte)0xA5, 48).ToArray();
        var doors = Enumerable.Range(0, 1000).Select(i => new KingdomQuestPineNativeDoorObject((ushort)i, seed, services)).ToArray();
        var effects = Enumerable.Range(0, 1000).Select(i => new KingdomQuestPineNativeEffectObject((ushort)i, seed, services, manager)).ToArray();
        Check(manager.TryBindPool(7, doors) && manager.TryBindPool(9, effects), "bind concrete original-size pools");
        seed[0] = 0;
        Check(doors[0].SnapshotBriefWire()[2] == 0xA5 && effects[0].SnapshotBriefWire()[2] == 0xA5 &&
            BitConverter.ToUInt16(doors[0].SnapshotBriefWire(), 48) == 1000 &&
            BitConverter.ToUInt16(effects[0].SnapshotBriefWire(), 47) == 1000,
            "constructors copy explicit seed and set ONLY scale in payload");
        Check(doors[0].Map == null && doors[0].Mode == 0 && !doors[0].IsMarked &&
            doors[0].SnapshotLoginLocation21() == null && !effects[0].TryRunRoutine(), "uninitialized login/routine unavailable");
        var vars = new KingdomQuestPineVariableStack();
        for (int i = 1; i <= 3; i++) { vars.TryPush("Door" + i, out _); vars.TryPush("EffDoor" + i, out _); }
        var bridge = new Bridge(manager, services);
        int[][] coords = { new[] {9860,6094,272}, new[] {6692,3944,6}, new[] {5894,6098,88} };
        for (int i = 0; i < 3; i++)
        {
            bridge.Parent = null;
            int door = i + 1; var xyz = coords[i];
            var site = Site(12+i, $"doorbuild Door{door} \"KQ_SlimeGate\" {xyz[0]} {xyz[1]}{new string(' ', new[] {2,4,3}[i])}{xyz[2]} 1000 \"Normal\".");
            Check(KingdomQuestHoneyingDoorBuildNativePlan.TryBuild(site, out var build), "door plan");
            doors[i].SetStaticWalkSpeed(1, 777); doors[i].SetStaticRunSpeed(1, 888);
            services.OnMark = obj => {
                var wire = obj.SnapshotBriefWire();
                Check(wire[2] == 0xA5 && wire[4] == 0xA5 && wire[15] == 0 && wire[16..48].All(b => b == 0),
                    "door marking sees previous handle/mob but new action/name");
                Check(doors[i].StaticWalkEnabled == 0 && doors[i].StaticWalkSpeed == 0 &&
                    doors[i].StaticRunEnabled == 0 && doors[i].StaticRunSpeed == 0, "native allocation resets only static speeds");
                Check(obj.Location.X == xyz[0] && obj.Location.Y == xyz[1] &&
                    obj.SnapshotLoginLocation21()[20] == xyz[2]/2, "door coordinates before marking");
            };
            events.Clear();
            Check(KingdomQuestPineDoorBuildRuntime.TryStep(build, vars, bridge, out bool done) && done &&
                events.SequenceEqual(new[] { "mob:1091", "mark:7:1", "build-complete", "movement" }), "native door build order");
            Check(doors[i].NativeMobField46 == 0xDEADBEEF && doors[i].NativeWord168 == 0 &&
                doors[i].RegistrationNumber == 0 && doors[i].IsMarked, "successful door fields");
            Check(doors[i].SnapshotBriefWire().SequenceEqual(bridge.LastDoor.CreateInitialBriefWire()), "door wire from persistent object");
            var oldLogin = doors[i].SnapshotLoginLocation21(); oldLogin[0] = 0;
            Check(doors[i].SnapshotLoginLocation21()[0] == 'L', "login snapshot isolation");

            // The current map/mode differ from login; effect Blast copies login
            // name but uses CURRENT parent coordinates and raw direction byte.
            doors[i].SetNativeMap(services.ParentMap); doors[i].SetNativeMode(7, 0xAA);
            doors[i].SetNativePosition(xyz[0]+5, xyz[1]+6); doors[i].SetNativeDirection(253);
            bridge.Parent = doors[i]; services.Clock = 10; services.ClockReads = 0;
            services.OnClock = n => {
                Check(ReferenceEquals(effects[i].Parent, doors[i]), "parent stored before first clock");
                if (n == 2) Check(effects[i].Routine.Deadline == 36010, "expiry stored before follow-clock read");
            };
            services.OnMark = obj => {
                var effect = (KingdomQuestPineNativeEffectObject)obj;
                Check(effect.Routine.Deadline == 36010 && effect.Routine.NextFollowTick == 61 &&
                    ReferenceEquals(effect.Parent, doors[i]), "effect state before marking");
                var login = effect.SnapshotLoginLocation21();
                Check(login[..12].SequenceEqual(Name("LOGIN_MAP")) && BitConverter.ToInt32(login,12) == xyz[0]+5 &&
                    BitConverter.ToInt32(login,16) == xyz[1]+6 && login[20] == 253, "login name/current parent coordinate split");
                Check(effect.SnapshotBriefWire().SequenceEqual(bridge.LastEffect.CreateInitialBriefWire(
                    doors[i].NativeHandle, xyz[0]+5, xyz[1]+6, 253, 0xA5)), "effect brief fully updated BEFORE marking");
            };
            Check(KingdomQuestHoneyingEffectNativePlan.TryBuild(Site(18+i,
                $"effectobj EffDoor{door} Door{door} \"KQ_SlimeGate\" 3600000 1000."), vars, out var blast), "effect plan");
            events.Clear();
            Check(KingdomQuestPineEffectRuntime.TryStep(blast, vars, bridge, out done) && done &&
                events.SequenceEqual(new[] { "clock", "clock", "mark:9:1", "blast-complete" }), "native effect blast order");
            Check(effects[i].Map == services.TargetMap && effects[i].Mode == 0 && effects[i].IsMarked &&
                effects[i].SnapshotBriefWire()[49] == 0xA4, "explicit map owner and retained high flag bits");
            services.OnClock = null;
        }

        // Real native object state feeds the existing collision/action executor.
        Check(KingdomQuestHoneyingDoorNativePlan.TryBuild(Site(61, "dooropen Door1 \"CloseGate01\"."), vars, out var action), "action plan");
        KingdomQuestMapCollisionSource.TryCreate("KDHoneying", out var expected); expected.CloseAllDoors();
        expected.TryDoorAction(action.SnapshotCollisionName32(), true);
        services.BeforeBroadcast = obj => Check(obj.SnapshotBriefWire()[15] == 1 &&
            obj.SnapshotBriefWire()[16..48].SequenceEqual(action.SnapshotCollisionName32()) &&
            services.Collision.SnapshotBitmap().SequenceEqual(expected.SnapshotBitmap()), "stored action and real collision before broadcast");
        Check(doors[0].TryApplyDoorAction(action), "concrete door action");
        services.BeforeBroadcast = null;
        Check(services.LastAction.SequenceEqual(new byte[] {9,108,158,80,1}), "native door action handle");

        // Failed marking leaves a prepared effect and writes its script handle
        // before free. Door failure keeps the old wire handle/mob and old token.
        services.OnMark = null; services.MarkError = 3;
        vars.TryFind("Door1", out var target); target.TrySetAscii("unchanged");
        events.Clear();
        KingdomQuestHoneyingDoorBuildNativePlan.TryBuild(Site(12,
            "doorbuild Door1 \"KQ_SlimeGate\" 9860 6094  272 1000 \"Normal\"."), out var firstBuild);
        Check(KingdomQuestPineDoorBuildRuntime.TryStep(firstBuild, vars, bridge, out _) &&
            target.Text == "unchanged" && doors[3].SnapshotBriefWire()[2] == 0xA5 &&
            doors[3].Map == services.TargetMap && !doors[3].IsMarked &&
            events.SequenceEqual(new[] { "mob:1091", "mark:7:1", "door-error:Marking:3", "detach:20641", "missing:20641:23" }),
            "door failure retains source mutations and manager observes second free miss");
        bridge.Parent = doors[0]; target.TrySetAscii("20638");
        vars.TryFind("EffDoor1", out var effectToken); effectToken.TrySetAscii("unchanged");
        KingdomQuestHoneyingEffectNativePlan.TryBuild(Site(18,
            "effectobj EffDoor1 Door1 \"KQ_SlimeGate\" 3600000 1000."), vars, out var firstBlast);
        services.OnDetach = obj => Check(obj.NativeHandle != 19391 || effectToken.Text == "19391", "handle written BEFORE failed effect free");
        Check(KingdomQuestPineEffectRuntime.TryStep(firstBlast, vars, bridge, out _) && effectToken.Text == "19391" &&
            BitConverter.ToUInt16(effects[3].SnapshotBriefWire(),2) == 19391 && !effects[3].IsMarked,
            "effect marking failure retains new brief and script token");
        services.OnDetach = null;

        // Mob data miss occurs before map marking, even though XY/action/name
        // have already changed. Missing services cause no mutation at all.
        services.MissingMob = true; events.Clear();
        Check(KingdomQuestPineDoorBuildRuntime.TryStep(firstBuild, vars, bridge, out _) &&
            events.SequenceEqual(new[] { "mob:1091", "mob-miss", "door-error:Marking:3", "detach:20642", "missing:20642:23" }), "native MobDataBox miss");
        services.MissingMob = false; services.MarkError = 0; services.Available = false;
        var before = doors[4].SnapshotBriefWire(); events.Clear();
        Check(!doors[4].TryBuild(bridge.LastDoor, out _) && before.SequenceEqual(doors[4].SnapshotBriefWire()) && events.Count == 0,
            "unavailable lifecycle rejected before writing fields");
        services.Available = true;

        // Expired effect follows its retained parent reference first, then
        // unmarks/frees itself, and finally reads Lua after removal from pool.
        services.Clock = 100; services.Script = true;
        doors[0].SetNativePosition(10200, 6500);
        effects[0].RetreatFromMap(); events.Clear();
        Check(effects[0].TryRunRoutine() && events.SequenceEqual(new[] {
            "clock", "move:10200:6500:5", "clock", "unmark:0:1:3", "detach:19388", "script-check", "lua:19388" }),
            "concrete effect follow/vanish/free/Lua sequence");
        Check(manager.TryGetObject(19388, out var absent) && absent == null &&
            manager.TryGetObjectAbsolute(19388, out var retained) && ReferenceEquals(retained,effects[0]) &&
            effects[0].Map == services.TargetMap && effects[0].Parent == doors[0] && !effects[0].IsMarked &&
            effects[0].Location.Direction == 253 && BitConverter.ToInt32(effects[0].SnapshotLoginLocation21(),12) == 9865,
            "free retains object/map/parent; movement leaves login and direction unchanged");

        // Consume unused slots, then observe FIFO reuse of actual object classes.
        for (int i = 4; i < 1000; i++) { ushort h = 0; Check(manager.TryAllocate(9,ref h,out _) && h == 19388+i, "fill effect pool"); }
        var savedBrief = effects[3].SnapshotBriefWire(); var savedParent = effects[3].Parent; var savedDeadline = effects[3].Routine.Deadline;
        ushort reused = 0;
        Check(manager.TryAllocate(9,ref reused,out var same) && reused == 19391 && ReferenceEquals(same,effects[3]) &&
            effects[3].SnapshotBriefWire().SequenceEqual(savedBrief) && effects[3].Parent == savedParent &&
            effects[3].Routine.Deadline == savedDeadline && effects[3].Map == services.TargetMap,
            "effect allocation init preserves ALL existing fields on FIFO reuse");
        var doorBefore = doors[0].SnapshotBriefWire(); doors[0].SetStaticRunSpeed(1,999); doors[0].SetStaticWalkSpeed(1,777);
        doors[0].InitializeForAllocation();
        Check(doors[0].StaticRunSpeed == 0 && doors[0].StaticWalkSpeed == 0 && doors[0].SnapshotBriefWire().SequenceEqual(doorBefore) &&
            doors[0].Map == services.ParentMap && doors[0].Mode == 7, "door Init preserves payload and map/mode");
        // Re-blast the reused object resets timers and parent, without replacing
        // its identity or using the theater's different map name.
        services.Clock = 200; services.Script = false;
        Check(effects[3].TryBlast(bridge.LastEffect, out int err) && err == 0 &&
            effects[3].Routine.Deadline == 36200 && effects[3].Routine.NextFollowTick == 251,
            "persistent object re-blast refreshes both timers");
        Console.WriteLine("PASS: concrete native door/effect pools, ordered Build/Blast fields, login/current location split, original Init/reuse, collision, failure cleanup and deferred effect lifecycle");
    }

    // Map/axis/visibility/Lua services are deliberate test owners. The slot
    // objects, pool manager, source commands, collision and effect routine are
    // production implementations; no automatic live-world readiness is claimed.
    sealed class Services : IKingdomQuestPineNativeDoorServices, IKingdomQuestPineNativeEffectServices
    {
        readonly List<string> events; readonly KingdomQuestPineNativeObjectManager manager;
        public bool Available = true, MissingMob, Script;
        public int MarkError, ClockReads;
        public uint Clock;
        public Action<KingdomQuestPineNativeSceneObject> OnMark, OnDetach;
        public Action<KingdomQuestPineNativeDoorObject> BeforeBroadcast;
        public Action<int> OnClock;
        public byte[] LastAction;
        public KingdomQuestMapCollision Collision;
        public readonly KingdomQuestPineEffectMapIdentity TargetMap = new(Name("LOGIN_MAP"), 30000, 30000);
        public readonly KingdomQuestPineEffectMapIdentity ParentMap = new(Name("CURRENT_MAP"), 30000, 30000);
        public Services(List<string> events, KingdomQuestPineNativeObjectManager manager)
        { this.events=events; this.manager=manager; KingdomQuestMapCollisionSource.TryCreate("KDHoneying",out Collision); Collision.CloseAllDoors(); }
        public bool IsAvailable => Available;
        public void Record(string message) => events.Add(message);
        public uint CurrentNativeTick { get { events.Add("clock"); OnClock?.Invoke(++ClockReads); return Clock++; } }
        public int Mark(KingdomQuestPineNativeSceneObject obj, byte ignore)
        { events.Add($"mark:{(byte)obj.NativeObjectType}:{ignore}"); OnMark?.Invoke(obj); obj.SetNativeMap(TargetMap); return MarkError; }
        public void FreeFromLists(KingdomQuestPineNativeSceneObject obj)
        { events.Add("detach:"+obj.NativeHandle); OnDetach?.Invoke(obj); }
        public KingdomQuestPineNativeDoorMobData FindMobData(ushort id)
        { events.Add("mob:"+id); return MissingMob ? null : new(0xDEADBEEF); }
        public void ReportMissingMobData() => events.Add("mob-miss");
        public void BuildComplete(KingdomQuestPineNativeDoorObject obj, ushort handle)
        { Check(obj.IsMarked && BitConverter.ToUInt16(obj.SnapshotBriefWire(),2)==handle, "door complete after brief/mark"); events.Add("build-complete"); obj.NativeWord168=123; }
        public void FinishBuildMovement(KingdomQuestPineNativeDoorObject obj)
        { Check(obj.NativeWord168==123,"completion before movement tail"); events.Add("movement"); }
        public KingdomQuestMapCollision GetCollision(KingdomQuestPineNativeDoorObject obj) => Collision;
        public void BroadcastDoorAction(KingdomQuestPineNativeDoorObject obj, byte[] wire)
        { BeforeBroadcast?.Invoke(obj); LastAction=wire; }
        public void BlastComplete(KingdomQuestPineNativeEffectObject obj, ushort handle)
        { Check(obj.IsMarked,"effect complete after mark"); events.Add("blast-complete"); obj.SetNativeMode(0,0); }
        public void ReportOutOfBounds(KingdomQuestPineNativeEffectObject obj) => throw new Exception("unexpected bounds");
        public void MoveTo(KingdomQuestPineNativeEffectObject obj,int x,int y,int argument)
        { events.Add($"move:{x}:{y}:{argument}"); obj.SetNativePosition(x,y); }
        public void Unmark(KingdomQuestPineNativeEffectObject obj,int when,int broadcast,int reason)
        { events.Add($"unmark:{when}:{broadcast}:{reason}"); obj.SetNativeMarked(false); }
        public bool HasRoutineScript(KingdomQuestPineNativeEffectObject obj) { events.Add("script-check"); return Script; }
        public void RunRoutineScript(KingdomQuestPineNativeEffectObject obj,ushort handle,KingdomQuestPineEffectMapIdentity map)
        { Check(manager.TryGetObject(handle,out var live) && live==null && map==TargetMap,"Lua reads persistent object AFTER free"); events.Add("lua:"+handle); }
    }
    sealed class Bridge(KingdomQuestPineNativeObjectManager manager, Services services) : IKingdomQuestPineDoorBuildOwner, IKingdomQuestPineEffectOwner
    {
        public KingdomQuestPineNativeDoorObject Parent;
        public KingdomQuestPineDoorBuildRequest LastDoor;
        public KingdomQuestPineEffectBlastRequest LastEffect;
        public bool IsAvailable => services.Available;
        public object Allocate(byte type,out ushort handle)
        { handle=65535; Check(manager.TryAllocate(type,ref handle,out var obj),"bound pool"); return obj; }
        public KingdomQuestPineTokenValue ResolveDestination(KingdomQuestPineVariableStack vars,string id)
        { vars.TryFind(id,out var value); return value; }
        public ushort FindMobId(string name) => 1091;
        public byte[] GetCurrentMapName12() => Parent==null ? Name("LOGIN_MAP") : Name("THEATER_MAP");
        public int Build(object obj,KingdomQuestPineDoorBuildRequest request)
        { LastDoor=request; Check(((KingdomQuestPineNativeDoorObject)obj).TryBuild(request,out var error),"door lifecycle available"); return error; }
        public bool Free(ushort handle,int when,int reason)
        { Check(manager.TryFree(handle,when,reason,out var result),"free bound pool"); return result; }
        public void ReportFailure(KingdomQuestPineDoorBuildFailure failure,int error) => services.Record($"door-error:{failure}:{error}");
        public object ResolveParent(byte[] token) => Parent;
        public bool ParentHasMap(object obj) => ((KingdomQuestPineNativeDoorObject)obj).Map!=null;
        public int Blast(object obj,KingdomQuestPineEffectBlastRequest request)
        { LastEffect=request; Check(((KingdomQuestPineNativeEffectObject)obj).TryBlast(request,out var error),"effect lifecycle available"); return error; }
        public void ReportAllocationFailure() => throw new Exception("unexpected allocation miss");
        public void ReportDestinationFailure() => throw new Exception("unexpected destination miss");
    }
}
