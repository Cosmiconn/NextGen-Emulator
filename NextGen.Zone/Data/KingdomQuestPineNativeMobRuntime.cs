using System;

namespace NextGen.Zone.Data
{
    public delegate bool KingdomQuestPineNativeDwordReader(out uint value);

    // Each Try bool is dependency availability; native outcomes have separate
    // outputs. Bind the real virtual implementations, never an emulator spawn.
    public interface IKingdomQuestPineNativeBreederMob : IKingdomQuestPineNativePoolObject
    {
        bool TryGetMode(out byte mode);                // vtable +484
        bool TryAlreadyDead(out byte dead);            // +28
        bool TryGetHp(out uint hp);                    // +4E8, no parameters
        bool TryKilledBy(IKingdomQuestPineNativePoolObject killer, int hp, int argument1000, uint argument1); // +5BC
        bool TryRegenLocation(out int x, out int y);    // +614, copied XY
        bool TryRegenerate(KingdomQuestPineNativeMobRegeneration request, out int markingError); // +6E0
        bool TrySetRoamingPath(IKingdomQuestPineNativeMobRoamingPath path); // +A14
        bool TrySetLifeTime(uint lifeTime);             // +ABC
    }
    public interface IKingdomQuestPineNativeMobRoamingPath
    { bool TryRegenLocation(ref int x, ref int y); }
    public interface IKingdomQuestPineNativeMobRegenLocation
    { bool TryRegenLocation(out int x, out int y, out int direction, out byte nativeResult); }
    public interface IKingdomQuestPineNativeMobLifeTimeSource
    {
        // Known missing MobDataBox index differs from index +24 == 0.
        bool TryLookup(ushort mobId, out bool indexPresent, out uint lifeTime);
    }
    public interface IKingdomQuestPineNativeMobDiagnostics
    {
        void AllocationFailed();
        // Original assertions followed by diagnostic Name3 ring write, before
        // Free(reason 0x24). This binding must own that shared diagnostic state.
        bool TryInvalidCoordinates(KingdomQuestPineNativeMobGroupBinding group, int x, int y);
    }

    /// <summary>
    /// Explicit fields/services used by mb_regen; NOT the full 4096-group
    /// hatchery or its constructor. Geometry and roaming bodies remain bound.
    /// </summary>
    public sealed class KingdomQuestPineNativeMobGroupBinding
    {
        private readonly byte[] mapName;
        public byte[] SnapshotMapName() => (byte[])mapName.Clone();
        public IKingdomQuestPineNativeMobRegenLocation Location { get; set; }
        public IKingdomQuestPineNativeMobRoamingPath RoamingPath { get; set; }
        public KingdomQuestPineNativeMobGroupBinding(byte[] nativeMapName12,
            IKingdomQuestPineNativeMobRegenLocation location, IKingdomQuestPineNativeMobRoamingPath roamingPath)
        {
            if (nativeMapName12 == null || nativeMapName12.Length != 12) throw new ArgumentException("Native Name3 required.");
            mapName = (byte[])nativeMapName12.Clone(); Location = location; RoamingPath = roamingPath;
        }
    }

    /// <summary>Exact arguments of so_mob_Regenerate in mb_regen 0x4B2F09.</summary>
    public sealed class KingdomQuestPineNativeMobRegeneration
    {
        // Keep group reference: native passes the address of its Name3.
        public KingdomQuestPineNativeMobGroupBinding Group { get; }
        public int X { get; }
        public int Y { get; }
        public int Direction { get; }
        public ushort Handle { get; }
        public ushort MobId { get; }
        public object LoginCharacterBrief { get; }
        public uint GlobalArgument { get; }
        public IKingdomQuestPineNativePoolObject Leader { get; }
        public KingdomQuestPineNativeMobBreeder Breeder { get; }
        public int Field08 { get; }
        public byte Argument1 => 1;
        public ulong Argument0 => 0;
        internal KingdomQuestPineNativeMobRegeneration(KingdomQuestPineNativeMobBreeder breeder,
            int x, int y, int direction, ushort handle, uint globalArgument, IKingdomQuestPineNativePoolObject leader)
        {
            Group = breeder.Group; X = x; Y = y; Direction = direction; Handle = handle;
            MobId = breeder.MobId; LoginCharacterBrief = breeder.LoginCharacterBrief;
            GlobalArgument = globalArgument; Leader = leader; Breeder = breeder; Field08 = breeder.Field08;
        }
    }

    /// <summary>
    /// Shared native WORD ring: 14FBA310 index, 14FBA314 mask, 14FB2310 data.
    /// Explicit native seed required; this is NOT a new random-number generator.
    /// Every consumer on the owning Zone thread must share this same instance.
    /// </summary>
    public sealed class KingdomQuestPineNativeMobRandomWords
    {
        private readonly ushort[] words;
        public uint Index { get; set; }
        public uint Mask { get; set; }
        public KingdomQuestPineNativeMobRandomWords(ushort[] nativeWords, uint index, uint mask)
        {
            words = (ushort[])(nativeWords ?? throw new ArgumentNullException(nameof(nativeWords))).Clone();
            Index = index; Mask = mask;
        }
        public bool TryNext(out ushort word)
        {
            word = 0;
            Index = unchecked(Index + 1) & Mask; // store precedes native table dereference
            if (Index >= words.Length) return false;
            word = words[Index]; return true;
        }
    }

    /// <summary>
    /// mb_regen 0x4B2CF0. Uses the concrete native object manager; virtual mob,
    /// map, data and clock owners must be explicitly bound. Unresolved calls
    /// preserve allocation/RNG/earlier writes, with no invented rollback.
    /// </summary>
    public sealed class KingdomQuestPineNativeMobRuntime
    {
        public KingdomQuestPineNativeObjectManager Objects { get; }
        public KingdomQuestPineNativeMobFamilies Families { get; }
        private readonly KingdomQuestPineNativeDwordReader clock, regenerationArgument;
        private readonly KingdomQuestPineNativeMobRandomWords random;
        private readonly IKingdomQuestPineNativeMobLifeTimeSource lifeTimes;
        private readonly IKingdomQuestPineNativeMobDiagnostics diagnostics;
        public KingdomQuestPineNativeMobRuntime(KingdomQuestPineNativeObjectManager objects,
            KingdomQuestPineNativeMobFamilies families, KingdomQuestPineNativeDwordReader clock,
            KingdomQuestPineNativeDwordReader regenerationArgument, KingdomQuestPineNativeMobRandomWords random,
            IKingdomQuestPineNativeMobLifeTimeSource lifeTimes, IKingdomQuestPineNativeMobDiagnostics diagnostics)
        {
            Objects = objects ?? throw new ArgumentNullException(nameof(objects));
            Families = families ?? throw new ArgumentNullException(nameof(families));
            this.clock = clock; this.regenerationArgument = regenerationArgument; this.random = random;
            this.lifeTimes = lifeTimes; this.diagnostics = diagnostics;
        }
        internal bool TryClock(out uint value) { value = 0; return clock != null && clock(out value); }
        public bool TryRegenerate(KingdomQuestPineNativeMobBreeder breeder,
            KingdomQuestPineNativeMobBreeder leader, out ushort result)
        {
            result = ushort.MaxValue;
            if (breeder == null) return false;
            ushort handle = ushort.MaxValue;
            if (!Objects.TryAllocate((ushort)(breeder.LoginCharacterBrief == null ? 5 : 8), ref handle, out var obj)) return false;
            if (obj == null)
            {
                if (diagnostics == null) return false;
                diagnostics.AllocationFailed(); return true;
            }
            IKingdomQuestPineNativePoolObject leaderObject = null;
            int x, y;
            if (!ReferenceEquals(breeder, leader))
            {
                if (leader == null || !Objects.TryGetObject(leader.Handle, out leaderObject)) return false;
                if (leaderObject == null) return true; // ORIGINAL LEAK: allocated object is not freed
                if (!(leaderObject is IKingdomQuestPineNativeBreederMob parent) || !parent.TryRegenLocation(out x, out y)) return false;
            }
            else
            {
                var group = breeder.Group;
                if (group?.Location == null || !group.Location.TryRegenLocation(out x, out y, out _, out byte ok)) return false;
                if (ok == 0) return Objects.TryFree(handle, 0, 0x23, out _);
            }
            if (random == null || !random.TryNext(out ushort word)) return false;
            int direction = word * 5; // overwrites direction from group geometry
            if (breeder.Group == null) return false;
            var roaming = breeder.Group.RoamingPath;
            if (roaming != null && !roaming.TryRegenLocation(ref x, ref y)) return false;
            if (unchecked((uint)x) >= 0xFFFFFF || unchecked((uint)y) >= 0xFFFFFF)
            {
                if (diagnostics == null || !diagnostics.TryInvalidCoordinates(breeder.Group, x, y)) return false;
                return Objects.TryFree(handle, 0, 0x24, out _);
            }
            if (!(obj is IKingdomQuestPineNativeBreederMob mob) || regenerationArgument == null ||
                !regenerationArgument(out uint argument)) return false;
            var request = new KingdomQuestPineNativeMobRegeneration(breeder, x, y, direction, handle, argument, leaderObject);
            if (!mob.TryRegenerate(request, out int error)) return false;
            if (error != 0) return Objects.TryFree(handle, 0, 0x25, out _);
            // Group/path and MobId are reread after callbacks, as in the EXE.
            if (breeder.Group == null || !mob.TrySetRoamingPath(breeder.Group.RoamingPath)) return false;
            if (lifeTimes == null || !lifeTimes.TryLookup(breeder.MobId, out bool present, out uint lifeTime)) return false;
            if (present && !mob.TrySetLifeTime(lifeTime)) return false;
            result = handle; return true;
        }
    }
}
