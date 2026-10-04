using System.Text;
using NextGen.Zone.Data;

static class EffectTests
{
    static void Check(bool ok, string message)
    { if (!ok) throw new Exception("effect/vanish: " + message); }
    static KingdomQuestHoneyingExternalPlan Site(int line, string command)
    {
        Check(KingdomQuestHoneyingExternalPlanBuilder.TryBuild("KQ/Honeying", line, command, out var site), "source site");
        return site;
    }
    public static void Run()
    {
        KingdomQuestHoneyingEffectNativePlan first = null;
        foreach (int door in new[] { 1, 2, 3 })
        {
            var variables = new KingdomQuestPineVariableStack();
            variables.TryPush("Door" + door, out var parent); parent.TrySetAscii("opaque-parent-" + door);
            variables.TryPush("EffDoor" + door, out var shadowed); shadowed.TrySetAscii("shadowed");
            variables.TryPush("EffDoor" + door, out var destination); destination.TrySetAscii("unchanged");
            var site = Site(17 + door, $"effectobj EffDoor{door} Door{door} \"KQ_SlimeGate\" 3600000 1000.");
            Check(KingdomQuestHoneyingEffectNativePlan.TryBuild(site, variables, out var plan), "effect plan");
            first ??= plan;
            Check(plan.CanonicalLine == 17 + door && plan.DestinationIdentifier == "EffDoor" + door, "exact destination");
            byte[] token = plan.SnapshotParentToken();
            Check(token.SequenceEqual(parent.SnapshotNativeBytes()), "opaque parent preserved");
            token[0] = 0;
            Check(plan.SnapshotParentToken()[0] == 'o', "parent snapshot isolation");
            var name = plan.CreateEffectName32();
            Check(name.Length == 32 && Encoding.ASCII.GetString(name).TrimEnd('\0') == "KQ_SlimeGate", "native padded effect name");
            name[0] = 0;
            Check(plan.CreateEffectName32()[0] == 'K', "effect name isolation");
            var owner = new EffectOwner { BeforeBlast = () => Check(destination.Text == "unchanged", "write follows Blast") };
            var sink = new Sink { Effect = owner };
            int state = 47;
            Check(new KingdomQuestHoneyingCommandState(sink).TryStep(site, variables, null, ref state, out bool completed) &&
                completed && state == 47, "typed effect dispatch and unchanged native state");
            Check(owner.Events.SequenceEqual(new[] { "destination:EffDoor" + door, "parent", "allocate:9", "parent-map", "map", "blast" }), "success call order");
            Check(destination.Text == "4660" && shadowed.Text == "shadowed", "decimal handle writes newest variable");
            Check(owner.ParentToken.SequenceEqual(parent.SnapshotNativeBytes()) &&
                ReferenceEquals(owner.Request.Parent, owner.Parent) && owner.Request.Handle == 0x1234, "native parent and allocated handle");
            Check(owner.Request.Source.CanonicalLine == 17 + door &&
                KingdomQuestHoneyingEffectNativePlan.DurationMilliseconds == 3600000 &&
                KingdomQuestHoneyingEffectNativePlan.Scale == 1000 &&
                KingdomQuestHoneyingEffectNativePlan.TrailingArgument == 0, "original Blast arguments");
            var map = owner.Request.SnapshotMapName12();
            Check(Encoding.ASCII.GetString(map).TrimEnd('\0') == "KQAlias", "theater map alias");
            map[0] = 0; owner.MapName[0] = 0;
            Check(owner.Request.SnapshotMapName12()[0] == 'K', "request map isolation");
            parent.TrySetAscii("changed");
            Check(plan.SnapshotParentToken()[0] == 'o', "plan detached from mutable variable");
            Check(!KingdomQuestHoneyingEffectNativePlan.TryBuild(site, new KingdomQuestPineVariableStack(), out _), "missing runtime token unresolved");
            int noOwnerState = 12;
            Check(!new KingdomQuestHoneyingCommandState().TryStep(site, variables, null, ref noOwnerState, out completed) &&
                !completed && noOwnerState == 12, "missing dispatcher owner cannot complete");
        }

        var stack = new KingdomQuestPineVariableStack();
        stack.TryPush("EffDoor1", out var target);
        foreach (string failure in new[] { "destination", "parent", "allocation", "parent-map", "marking" })
        {
            target.TrySetAscii("unchanged");
            var owner = new EffectOwner { Failure = failure,
                BeforeFree = () => Check(target.Text == "4660", "marking error writes handle BEFORE Free") };
            Check(KingdomQuestPineEffectRuntime.TryStep(first, stack, owner, out bool completed) && completed, "native failure pops: " + failure);
            string[] expected = failure switch {
                "destination" => new[] { "destination:EffDoor1", "error:destination" },
                "parent" => new[] { "destination:EffDoor1", "parent" },
                "allocation" => new[] { "destination:EffDoor1", "parent", "allocate:9", "error:allocation" },
                "parent-map" => new[] { "destination:EffDoor1", "parent", "allocate:9", "parent-map" },
                _ => new[] { "destination:EffDoor1", "parent", "allocate:9", "parent-map", "map", "blast", "free:4660:0:31" }
            };
            Check(owner.Events.SequenceEqual(expected), "failure precedence and cleanup: " + failure);
            Check(target.Text == (failure == "marking" ? "4660" : "unchanged"), "native destination result: " + failure);
        }
        var unavailable = new EffectOwner { IsAvailable = false };
        Check(!KingdomQuestPineEffectRuntime.TryStep(first, stack, unavailable, out bool done) && !done &&
            unavailable.Events.Count == 0, "unavailable service produces no side effects");
        Check(!KingdomQuestPineEffectRuntime.TryStep(first, stack, null, out done) && !done, "null effect owner");
        var nullMap = new EffectOwner { MapName = null };
        Check(KingdomQuestPineEffectRuntime.TryStep(first, stack, nullMap, out done) && done &&
            nullMap.Request.SnapshotMapName12() == null, "native null map forwarded to parent Blast overload");
        target.TrySetAscii("unchanged");
        var badMap = new EffectOwner { MapName = new byte[11] };
        Check(!KingdomQuestPineEffectRuntime.TryStep(first, stack, badMap, out done) && !done &&
            badMap.Events.Last() == "map" && !badMap.Events.Contains("blast") && target.Text == "unchanged", "invalid owner width unresolved without invented cleanup");
        var fallback = new EffectOwner();
        Check(KingdomQuestPineEffectRuntime.TryStep(first, new KingdomQuestPineVariableStack(), fallback, out done) &&
            done && fallback.Fallback.Text == "4660", "explicit native fallback destination");

        foreach (var (line, door) in new[] { (62, 1), (98, 2), (131, 3) })
        {
            var variables = new KingdomQuestPineVariableStack();
            variables.TryPush("EffDoor" + door, out var value); value.TrySetAscii("opaque-effect-" + door);
            var site = Site(line, $"vanish EffDoor{door}.");
            Check(KingdomQuestHoneyingVanishNativePlan.TryBuild(site, variables, out var plan), "vanish plan");
            var token = plan.SnapshotObjectToken();
            Check(token.SequenceEqual(value.SnapshotNativeBytes()), "opaque vanish token");
            token[0] = 0; value.TrySetAscii("changed");
            Check(plan.SnapshotObjectToken()[0] == 'o', "vanish snapshot isolation");
            value.TrySetAscii("opaque-effect-" + door);
            var lifetime = new KingdomQuestPineEffectLifetime(100, 3600000);
            var owner = new VanishOwner { Object = lifetime };
            int state = 47;
            Check(new KingdomQuestHoneyingCommandState(new Sink { Vanish = owner }).TryStep(site, variables, null,
                ref state, out done) && done && state == 47, "typed vanish dispatch");
            Check(owner.Events.SequenceEqual(new[] { "resolve" }) && lifetime.Deadline == 0 &&
                lifetime.IsExpired(0) && owner.Token.SequenceEqual(value.SnapshotNativeBytes()), "vanish sets expiry only; timer observes it later");
            var otherObject = new RetreatObject();
            owner = new VanishOwner { Object = otherObject };
            Check(KingdomQuestPineVanishRuntime.TryStep(plan, owner, out done) && done && otherObject.Calls == 1, "identifier vanish has no type filter");
            var missing = new VanishOwner();
            Check(KingdomQuestPineVanishRuntime.TryStep(plan, missing, out done) && done &&
                missing.Events.SequenceEqual(new[] { "resolve", "error:missing" }), "native missing object logs and completes");
            var absent = new VanishOwner { Available = false };
            Check(!KingdomQuestPineVanishRuntime.TryStep(plan, absent, out done) && !done &&
                absent.Events.SequenceEqual(new[] { "resolve" }), "unavailable resolution stays unresolved");
            Check(!KingdomQuestPineVanishRuntime.TryStep(plan, null, out done) && !done, "null vanish owner");
            Check(!KingdomQuestHoneyingVanishNativePlan.TryBuild(site, new KingdomQuestPineVariableStack(), out _), "missing vanish variable unresolved");
        }
        var expiry = new KingdomQuestPineEffectLifetime(123, 3600000);
        Check(expiry.Deadline == 36123 && !expiry.IsExpired(36122) && expiry.IsExpired(36123), "native 10Hz expiry includes equality");
        Check(new KingdomQuestPineEffectLifetime(0, 99).Deadline == 0 &&
            new KingdomQuestPineEffectLifetime(0, 100).Deadline == 1, "millisecond truncation");
        Check(new KingdomQuestPineEffectLifetime(0, uint.MaxValue).Deadline == 4294967, "multiply wraps BEFORE division");
        var wrap = new KingdomQuestPineEffectLifetime(uint.MaxValue - 5, 1000);
        Check(wrap.Deadline == 4 && wrap.IsExpired(uint.MaxValue - 5) && !wrap.IsExpired(3) && wrap.IsExpired(4), "native unsigned absolute comparison at wrap");
        Check(!KingdomQuestHoneyingExternalPlanBuilder.TryBuild("KQ/Honeying", 18,
            "effectobj EffDoor1 Door1 \"InventedEffect\" 3600000 1000.", out _), "altered effect source rejected");
        Check(!KingdomQuestHoneyingExternalPlanBuilder.TryBuild("KQ/Honeying", 62, "vanish EffDoor2.", out _), "altered vanish source rejected");
        Console.WriteLine("PASS: all six original Honeying effect/vanish sites, typed dispatch, failure order, handle-before-free and delayed unsigned expiry");
    }

    sealed class EffectOwner : IKingdomQuestPineEffectOwner
    {
        public bool IsAvailable { get; set; } = true;
        public readonly List<string> Events = new();
        public readonly object Parent = new();
        readonly object allocated = new();
        public string Failure;
        public Action BeforeBlast, BeforeFree;
        public byte[] ParentToken, MapName = Encoding.ASCII.GetBytes("KQAlias\0\0\0\0\0");
        public KingdomQuestPineTokenValue Fallback = new();
        public KingdomQuestPineEffectBlastRequest Request;
        public KingdomQuestPineTokenValue ResolveDestination(KingdomQuestPineVariableStack variables, string name)
        { Events.Add("destination:" + name); return Failure == "destination" ? null : variables.TryFind(name, out var token) ? token : Fallback; }
        public object ResolveParent(byte[] token)
        { Events.Add("parent"); ParentToken = token; return Failure == "parent" ? null : Parent; }
        public object Allocate(byte type, out ushort handle)
        { Events.Add("allocate:" + type); handle = 0x1234; return Failure == "allocation" ? null : allocated; }
        public bool ParentHasMap(object parent)
        { Check(ReferenceEquals(parent, Parent), "parent map identity"); Events.Add("parent-map"); return Failure != "parent-map"; }
        public byte[] GetCurrentMapName12() { Events.Add("map"); return MapName; }
        public int Blast(object obj, KingdomQuestPineEffectBlastRequest request)
        { Check(ReferenceEquals(obj, allocated), "allocated effect identity"); Events.Add("blast"); BeforeBlast?.Invoke(); Request = request; return Failure == "marking" ? 3 : 0; }
        public bool Free(ushort handle, int when, int reason)
        { Events.Add($"free:{handle}:{when}:{reason}"); BeforeFree?.Invoke(); return false; }
        public void ReportAllocationFailure() { Events.Add("error:allocation"); }
        public void ReportDestinationFailure() { Events.Add("error:destination"); }
    }
    sealed class VanishOwner : IKingdomQuestPineVanishOwner
    {
        public bool Available = true;
        public IKingdomQuestPineRetreatObject Object;
        public byte[] Token;
        public readonly List<string> Events = new();
        public bool TryResolve(byte[] token, out IKingdomQuestPineRetreatObject obj)
        { Events.Add("resolve"); Token = token; obj = Object; return Available; }
        public void ReportMissingObject() { Events.Add("error:missing"); }
    }
    sealed class RetreatObject : IKingdomQuestPineRetreatObject
    { public int Calls; public void RetreatFromMap() { Calls++; } }
    sealed class Sink : IKingdomQuestHoneyingExternalCommandSink
    {
        public EffectOwner Effect;
        public VanishOwner Vanish;
        public bool TryEffectObject(KingdomQuestHoneyingEffectNativePlan p, KingdomQuestPineVariableStack v, ref int s, out bool c) => KingdomQuestPineEffectRuntime.TryStep(p, v, Effect, out c);
        public bool TryVanish(KingdomQuestHoneyingVanishNativePlan p, ref int s, out bool c) => KingdomQuestPineVanishRuntime.TryStep(p, Vanish, out c);
        public bool TryBroadcast(KingdomQuestHoneyingBroadcastNativePlan p, ref int s, out bool c) => throw new NotSupportedException();
        public bool TryChatWindow(KingdomQuestHoneyingChatWindowNativePlan p, ref int s, out bool c) => throw new NotSupportedException();
        public bool TryQuestMobKill(KingdomQuestHoneyingQuestMobKillNativePlan p, KingdomQuestPineVariableStack v, ref int s, out bool c) => throw new NotSupportedException();
        public bool TryReward(KingdomQuestHoneyingRewardNativePlan p, KingdomQuestPineVariableStack v, ref int s, out bool c) => throw new NotSupportedException();
        public bool TryNpcShout(KingdomQuestHoneyingNpcShoutNativePlan p, ref int s, out bool c) => throw new NotSupportedException();
        public bool TryDoorBuild(KingdomQuestHoneyingDoorBuildNativePlan p, KingdomQuestPineVariableStack v, ref int s, out bool c) => throw new NotSupportedException();
        public bool TryDoorAction(KingdomQuestHoneyingDoorNativePlan p, ref int s, out bool c) => throw new NotSupportedException();
        public bool TryUnresolved(KingdomQuestHoneyingExternalPlan p, KingdomQuestPineVariableStack v, uint? h, ref int s, out bool c) => throw new NotSupportedException();
        public bool TryLinkTo(KingdomQuestPineLinkToOwnerPlan p, KingdomQuestPineVariableStack v, ref int s, out bool c) => throw new NotSupportedException();
        public bool TryMobRegen(KingdomQuestPineMobRegenOwnerPlan p, ref int s, out bool c) => throw new NotSupportedException();
        public bool TrySummonMob(KingdomQuestPineSummonMobOwnerPlan p, ref int s, out bool c) => throw new NotSupportedException();
    }
}
