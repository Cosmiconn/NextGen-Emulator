using System;
using System.Globalization;
using System.Text;

namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestHoneyingEffectNativePlan
    {
        public const uint NativeStepAddress = 0x004F2570;
        public const byte ObjectType = 9;
        public const int BlastVtableOffset = 0x6E4;
        public const string EffectName = "KQ_SlimeGate";
        public const uint DurationMilliseconds = 3600000;
        public const int Scale = 1000;
        public const int TrailingArgument = 0;
        public int CanonicalLine { get; }
        public string DestinationIdentifier { get; }
        private readonly byte[] parentToken;

        private KingdomQuestHoneyingEffectNativePlan(int line, byte[] parentToken)
        {
            CanonicalLine = line;
            DestinationIdentifier = "EffDoor" + (line - 17);
            this.parentToken = (byte[])parentToken.Clone();
        }
        public byte[] SnapshotParentToken() => (byte[])parentToken.Clone();
        public byte[] CreateEffectName32()
        {
            var result = new byte[32];
            Encoding.ASCII.GetBytes(EffectName).CopyTo(result, 0);
            return result;
        }
        public static bool TryBuild(KingdomQuestHoneyingExternalPlan source,
            KingdomQuestPineVariableStack variables, out KingdomQuestHoneyingEffectNativePlan plan)
        {
            plan = null;
            if (source == null || variables == null || source.Kind != KingdomQuestHoneyingExternalKind.EffectObject ||
                source.TopLevelBlock != "main" || source.CanonicalLine < 18 || source.CanonicalLine > 20) return false;
            KingdomQuestHoneyingExternalSourceSite original;
            KingdomQuestPineTokenValue parent;
            if (!KingdomQuestHoneyingSourceFlow.TryResolve("KQ/Honeying", source.CanonicalLine, source.CommandText, out original) ||
                !variables.TryFind("Door" + (source.CanonicalLine - 17), out parent) || parent == null) return false;
            plan = new KingdomQuestHoneyingEffectNativePlan(source.CanonicalLine, parent.SnapshotNativeBytes());
            return true;
        }
    }

    public sealed class KingdomQuestPineEffectBlastRequest
    {
        public KingdomQuestHoneyingEffectNativePlan Source { get; }
        public ushort Handle { get; }
        public object Parent { get; }
        private readonly byte[] mapName12;
        public byte[] SnapshotMapName12() => mapName12 == null ? null : (byte[])mapName12.Clone();
        // Native EffectBlast obtains these values from the resolved parent;
        // neither the opaque Pine token nor the theater alias supplies them.
        public byte[] CreateInitialBriefWire(ushort parentHandle, int parentX,
            int parentY, byte parentDirection, byte previousFlags) =>
            KingdomQuestPineEffectBrief.CreateNativeWire(Handle, Source.CreateEffectName32(),
                parentX, parentY, parentDirection, parentHandle,
                KingdomQuestHoneyingEffectNativePlan.Scale,
                KingdomQuestPineEffectBrief.ApplyBlastFlag(previousFlags,
                    KingdomQuestHoneyingEffectNativePlan.TrailingArgument));
        public KingdomQuestPineEffectRoutine CreateRoutine(uint expiryClock, uint followClock) =>
            new KingdomQuestPineEffectRoutine(expiryClock, followClock,
                KingdomQuestHoneyingEffectNativePlan.DurationMilliseconds);
        internal KingdomQuestPineEffectBlastRequest(KingdomQuestHoneyingEffectNativePlan source,
            ushort handle, object parent, byte[] mapName)
        {
            Source = source; Handle = handle; Parent = parent;
            mapName12 = mapName == null ? null : (byte[])mapName.Clone();
        }
    }

    public interface IKingdomQuestPineEffectOwner
    {
        bool IsAvailable { get; }
        KingdomQuestPineTokenValue ResolveDestination(KingdomQuestPineVariableStack variables, string identifier);
        // Evaluate the parent token with native pst_GetNumber(0), then use
        // som_GetObject. Null is a native miss, not an unavailable service.
        object ResolveParent(byte[] nativeToken);
        object Allocate(byte nativeObjectType, out ushort handle);
        bool ParentHasMap(object parent);
        byte[] GetCurrentMapName12();
        int Blast(object allocated, KingdomQuestPineEffectBlastRequest request);
        bool Free(ushort handle, int removeWhen, int reason);
        void ReportAllocationFailure();
        void ReportDestinationFailure();
    }

    public static class KingdomQuestPineEffectRuntime
    {
        public static bool TryStep(KingdomQuestHoneyingEffectNativePlan plan,
            KingdomQuestPineVariableStack variables, IKingdomQuestPineEffectOwner owner, out bool completed)
        {
            completed = false;
            if (plan == null || variables == null || owner == null || !owner.IsAvailable) return false;
            var destination = owner.ResolveDestination(variables, plan.DestinationIdentifier);
            if (destination == null) owner.ReportDestinationFailure();
            else
            {
                object parent = owner.ResolveParent(plan.SnapshotParentToken());
                if (parent != null)
                {
                    ushort handle;
                    object allocated = owner.Allocate(KingdomQuestHoneyingEffectNativePlan.ObjectType, out handle);
                    if (allocated == null) owner.ReportAllocationFailure();
                    else if (owner.ParentHasMap(parent))
                    {
                        byte[] map = owner.GetCurrentMapName12();
                        // Native forwards a null MapNameServer result too.
                        if (map != null && map.Length != 12) return false;
                        int result = owner.Blast(allocated, new KingdomQuestPineEffectBlastRequest(plan, handle, parent, map));
                        // Unlike doorbuild, this write PRECEDES the result check.
                        if (!destination.TrySetAscii(handle.ToString(CultureInfo.InvariantCulture))) return false;
                        if (result != 0) owner.Free(handle, 0, 31);
                    }
                    // Native parent-map miss follows allocation, pops and does
                    // NOT free or write a handle. Preserve this observed branch.
                }
            }
            completed = true;
            return true;
        }
    }

    public sealed class KingdomQuestHoneyingVanishNativePlan
    {
        public const uint NativeStepAddress = 0x004EDCF0;
        public const int RetreatVtableOffset = 0x3F4;
        public int CanonicalLine { get; }
        private readonly byte[] token;
        private KingdomQuestHoneyingVanishNativePlan(int line, byte[] token)
        { CanonicalLine = line; this.token = (byte[])token.Clone(); }
        public byte[] SnapshotObjectToken() => (byte[])token.Clone();
        public static bool TryBuild(KingdomQuestHoneyingExternalPlan source,
            KingdomQuestPineVariableStack variables, out KingdomQuestHoneyingVanishNativePlan plan)
        {
            plan = null;
            if (source == null || variables == null || source.Kind != KingdomQuestHoneyingExternalKind.Vanish) return false;
            int door;
            switch (source.CanonicalLine) { case 62: door = 1; break; case 98: door = 2; break; case 131: door = 3; break; default: return false; }
            KingdomQuestHoneyingExternalSourceSite original;
            KingdomQuestPineTokenValue value;
            if (!KingdomQuestHoneyingSourceFlow.TryResolve("KQ/Honeying", source.CanonicalLine, source.CommandText, out original) ||
                !variables.TryFind("EffDoor" + door, out value) || value == null) return false;
            plan = new KingdomQuestHoneyingVanishNativePlan(source.CanonicalLine, value.SnapshotNativeBytes());
            return true;
        }
    }

    public interface IKingdomQuestPineRetreatObject { void RetreatFromMap(); }
    public interface IKingdomQuestPineVanishOwner
    {
        // This is the identifier branch of os_ShineObject, NOT vanish all,
        // vanish kind, or vanish handle. No object-type filter is applied.
        bool TryResolve(byte[] nativeToken, out IKingdomQuestPineRetreatObject obj);
        void ReportMissingObject();
    }
    public static class KingdomQuestPineVanishRuntime
    {
        public static bool TryStep(KingdomQuestHoneyingVanishNativePlan plan,
            IKingdomQuestPineVanishOwner owner, out bool completed)
        {
            completed = false;
            IKingdomQuestPineRetreatObject obj;
            if (plan == null || owner == null || !owner.TryResolve(plan.SnapshotObjectToken(), out obj)) return false;
            if (obj == null) owner.ReportMissingObject();
            else obj.RetreatFromMap();
            completed = true;
            return true;
        }
    }

    /// <summary>
    /// EffectBlast 0x004B1540 sets the unsigned expiry. Effect retreat
    /// 0x00495CC0 sets it to zero; it does NOT send/free immediately.
    /// Routine 0x00569FD0 expires at equality. KingdomQuestPineEffectRoutine
    /// owns its call order; actual map/pool operations remain live dependencies.
    /// </summary>
    public sealed class KingdomQuestPineEffectLifetime : IKingdomQuestPineRetreatObject
    {
        public uint Deadline { get; private set; }
        public KingdomQuestPineEffectLifetime(uint now, uint milliseconds)
        { Deadline = unchecked(now + unchecked(milliseconds * 10u) / 1000u); }
        public void RetreatFromMap() { Deadline = 0; }
        public bool IsExpired(uint now) => now >= Deadline;
    }
}
