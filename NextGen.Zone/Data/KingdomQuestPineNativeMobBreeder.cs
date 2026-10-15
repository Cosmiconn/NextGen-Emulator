using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    /// <summary>MobBreedSecond: the twelve DWORDs at MobBreeder +0x0C..+0x38.</summary>
    public sealed class KingdomQuestPineNativeBreedSeconds
    {
        private readonly uint[] words;
        public uint this[int index] => words[index];
        public KingdomQuestPineNativeBreedSeconds(uint[] nativeWords)
        {
            if (nativeWords == null || nativeWords.Length != 12) throw new ArgumentException("Twelve native DWORDs required.");
            words = (uint[])nativeWords.Clone();
        }
        public static KingdomQuestPineNativeBreedSeconds FromLayout(KingdomQuestPineRegenNativeLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            var data = new uint[12]; data[0] = layout.Standard; data[1] = layout.MinSeconds; data[2] = layout.MaxSeconds;
            for (int i = 0; i < 9; i++) data[i + 3] = unchecked((uint)layout.TimeDistribution[i]);
            return new KingdomQuestPineNativeBreedSeconds(data);
        }
        internal KingdomQuestPineNativeBreedSeconds Copy() => new KingdomQuestPineNativeBreedSeconds(words);
        internal static KingdomQuestPineNativeBreedSeconds Uniform(int seconds)
        {
            var data = new uint[12];
            for (int i = 0; i < 12; i++) data[i] = unchecked((uint)seconds);
            return new KingdomQuestPineNativeBreedSeconds(data);
        }
    }

    /// <summary>
    /// Persistent MobBreeder storage. Constructor 0x4B0E40 writes only five
    /// fields; every other used field requires an explicit seed. Null pointers
    /// are known native nulls, never automatically resolved live dependencies.
    /// </summary>
    public sealed class KingdomQuestPineNativeMobBreeder
    {
        public KingdomQuestPineNativeMobGroupBinding Group { get; set; } // +0
        public byte Enabled { get; set; } = 1;                         // +4
        public ushort MobId { get; set; }                             // +6
        public int Field08 { get; set; } = -1;
        public KingdomQuestPineNativeBreedSeconds Seconds { get; private set; }
        public ushort Handle { get; set; } = ushort.MaxValue;         // +3C
        public uint Deadline { get; set; }                            // +40
        public byte Pending { get; set; }                             // +44
        public uint CurrentInterval { get; set; }                     // +48
        public uint Tick4C { get; set; }
        public uint Tick50 { get; set; }
        public int Remaining { get; set; } = -1;                      // +54
        public KingdomQuestPineNativeMobBreeder Previous { get; set; } // +58
        public KingdomQuestPineNativeMobBreeder Next { get; set; }     // +5C
        public object LoginCharacterBrief { get; set; }               // +60, opaque native pointer

        public KingdomQuestPineNativeMobBreeder(KingdomQuestPineNativeMobGroupBinding group,
            KingdomQuestPineNativeBreedSeconds seconds, uint deadline, byte pending, uint currentInterval,
            uint tick4C, uint tick50, KingdomQuestPineNativeMobBreeder previous,
            KingdomQuestPineNativeMobBreeder next, object loginCharacterBrief)
        {
            Group = group; Seconds = (seconds ?? throw new ArgumentNullException(nameof(seconds))).Copy();
            Deadline = deadline; Pending = pending; CurrentInterval = currentInterval;
            Tick4C = tick4C; Tick50 = tick50; Previous = previous; Next = next; LoginCharacterBrief = loginCharacterBrief;
        }
        // mb_Set 0x4B3110: does NOT reset enabled, handle, timestamps, links or brief.
        public void Set(KingdomQuestPineNativeMobGroupBinding group, ushort mobId,
            KingdomQuestPineNativeBreedSeconds seconds, int remaining)
        {
            if (seconds == null) throw new ArgumentNullException(nameof(seconds));
            Group = group; MobId = mobId; Field08 = -1; Seconds = seconds.Copy(); Pending = 1;
            Deadline = unchecked(seconds[7] * 10); Remaining = remaining; CurrentInterval = Seconds[0];
        }
        // mb_Setregentime 0x4B1B90: all twelve words, including thresholds/deltas.
        public void SetRegenTime(int seconds)
        { Seconds = KingdomQuestPineNativeBreedSeconds.Uniform(seconds); CurrentInterval = Seconds[0]; }
        // mb_SetLeader 0x4B2400. Cyclic/corrupt chains are UNRESOLVED, not repaired.
        public bool TrySetLeader(KingdomQuestPineNativeMobBreeder leader)
        {
            if (leader == null) return false;
            var seen = new HashSet<KingdomQuestPineNativeMobBreeder>();
            while (leader.Next != null)
            { if (!seen.Add(leader)) return false; leader = leader.Next; }
            leader.Next = this; Previous = leader; return true; // own Next is untouched
        }

        /// <summary>
        /// mb_Routine 0x4B52C0. True means the entire native call was resolved
        /// (its native result is always 1). False retains all preceding writes.
        /// tryFreeSlot performs group Free for THIS occupied slot; native false
        /// is ignored by that binding. Do not retry partial calls transactionally.
        /// </summary>
        public bool TryRoutine(KingdomQuestPineNativeMobRuntime runtime, Func<bool> tryFreeSlot)
        {
            if (runtime == null) return false;
            if (Remaining == 0)
            {
                if (!runtime.Objects.TryGetObject(Handle, out var last)) return false;
                return last != null || (tryFreeSlot != null && tryFreeSlot());
            }
            if (Handle != ushort.MaxValue)
            {
                if (!runtime.Objects.TryGetObject(Handle, out var current)) return false;
                if (current == null)
                {
                    Handle = ushort.MaxValue;
                    var root = this;
                    var roots = new HashSet<KingdomQuestPineNativeMobBreeder>();
                    while (root.Previous != null)
                    { if (!roots.Add(root)) return false; root = root.Previous; }
                    if (runtime.Families.Dead(root, MobId))
                    {
                        var seen = new HashSet<KingdomQuestPineNativeMobBreeder>();
                        for (var member = root; member != null; member = member.Next)
                        {
                            if (!seen.Add(member) || !runtime.Objects.TryGetObject(member.Handle, out var obj)) return false;
                            if (obj == null) continue;
                            if (!(obj is IKingdomQuestPineNativeBreederMob mob) || !mob.TryGetMode(out byte mode)) return false;
                            if (mode == 3) continue;
                            if (!mob.TryAlreadyDead(out byte dead)) return false;
                            if (dead != 0) continue;
                            // +4E8 takes NO arguments: the earlier pushes 1,1000
                            // belong to +5BC KilledBy(self, HP, 1000, 1).
                            if (!mob.TryGetHp(out uint hp) || !mob.TryKilledBy(obj, unchecked((int)hp), 1000, 1)) return false;
                        }
                    }
                }
            }
            if (Previous != null || Enabled == 0) return true;
            if (Pending != 0)
            {
                if (!runtime.TryClock(out uint clock)) return false;
                if (clock <= Deadline) return true; // strict, unsigned; no wrap-aware replacement
                runtime.Families.Initialize(this);
                var seen = new HashSet<KingdomQuestPineNativeMobBreeder>();
                for (var member = this; member != null; member = member.Next)
                {
                    if (!seen.Add(member)) return false;
                    member.Pending = 0;
                    if (!runtime.TryRegenerate(member, this, out ushort handle)) return false;
                    member.Handle = handle;
                    if (member.Remaining > 0) member.Remaining--;
                }
                return true;
            }
            var visited = new HashSet<KingdomQuestPineNativeMobBreeder>();
            for (var member = this; member != null; member = member.Next)
            {
                if (!visited.Add(member) || !runtime.Objects.TryGetObject(member.Handle, out var obj)) return false;
                if (obj != null) return true;
            }
            int elapsed = unchecked((int)(unchecked(Tick50 - Tick4C) / 10u));
            Pending = 1; // before reading the global clock
            if (!runtime.TryClock(out uint now)) return false;
            uint delta = elapsed < unchecked((int)Seconds[4]) ? Seconds[3] :
                elapsed < unchecked((int)Seconds[6]) ? Seconds[5] :
                elapsed > unchecked((int)Seconds[10]) ? Seconds[11] :
                elapsed > unchecked((int)Seconds[8]) ? Seconds[9] : 0;
            uint candidate = unchecked(CurrentInterval + delta);
            CurrentInterval = candidate < Seconds[1] ? Seconds[1] : candidate > Seconds[2] ? Seconds[2] : candidate;
            Deadline = unchecked(now + CurrentInterval * 10);
            return true;
        }
    }

    /// <summary>FamilyMobManager 0x4B5050/5120/5A40/5CF0, Zone-wide owner.</summary>
    public sealed class KingdomQuestPineNativeMobFamilies
    {
        public sealed class Species
        {
            public ushort MobId { get; }
            public ushort Threshold { get; }
            public ushort Deaths { get; internal set; }
            internal Species(ushort mobId, ushort threshold) { MobId = mobId; Threshold = threshold; }
        }
        private sealed class Family
        {
            internal KingdomQuestPineNativeMobBreeder Root;
            internal readonly List<Species> Species = new List<Species>();
        }
        private readonly List<Family> families = new List<Family>();
        public int Count => families.Count;
        public Species Find(KingdomQuestPineNativeMobBreeder root, ushort mobId) =>
            families.Find(f => ReferenceEquals(f.Root, root))?.Species.Find(s => s.MobId == mobId);
        public void Register(KingdomQuestPineNativeMobBreeder root, ushort mobId, int killNumber)
        {
            var family = families.Find(f => ReferenceEquals(f.Root, root));
            if (family == null) { family = new Family { Root = root }; families.Add(family); }
            if (family.Species.Exists(s => s.MobId == mobId)) return;
            family.Species.Add(new Species(mobId, unchecked((ushort)(killNumber == 0 ? 60000 : killNumber))));
        }
        public void Initialize(KingdomQuestPineNativeMobBreeder root)
        {
            var family = families.Find(f => ReferenceEquals(f.Root, root));
            if (family != null) foreach (var species in family.Species) species.Deaths = 0;
        }
        public bool Dead(KingdomQuestPineNativeMobBreeder root, ushort mobId)
        {
            var species = Find(root, mobId);
            if (species == null) return false;
            species.Deaths = unchecked((ushort)(species.Deaths + 1));
            return species.Deaths >= species.Threshold;
        }
        public void Remove(KingdomQuestPineNativeMobBreeder root) => families.RemoveAll(f => ReferenceEquals(f.Root, root));
    }
}
