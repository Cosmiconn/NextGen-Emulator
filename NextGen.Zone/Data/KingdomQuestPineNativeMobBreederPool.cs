using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// The 64 persistent breeders in MobBreederGroup (0x4B3CE0), AllocZ
    /// (0x4B2180), Free (0x4B5C00), Routine (0x4B55A0) and Lua regen
    /// (0x4B30C0). This is not the outer MobHatchery group pool/ScriptBreed.
    /// </summary>
    public sealed class KingdomQuestPineNativeMobBreederPool
    {
        public const int Capacity = 64;
        private readonly KingdomQuestPineNativeMobBreeder[] slots;
        private readonly int[] next = new int[Capacity + 1], previous = new int[Capacity + 1];
        private readonly bool[] used = new bool[Capacity];
        private readonly KingdomQuestPineNativeMobFamilies families;
        private readonly Action allocationFailed;
        private readonly Action<ushort> invalidFree;
        private int freeHead;
        public int Count { get; private set; }
        public KingdomQuestPineNativeMobBreederPool(KingdomQuestPineNativeMobGroupBinding group,
            IReadOnlyList<KingdomQuestPineNativeMobBreeder> explicitSlots, KingdomQuestPineNativeMobFamilies families,
            Action allocationFailed, Action<ushort> invalidFree)
        {
            if (group == null || explicitSlots == null || explicitSlots.Count != Capacity) throw new ArgumentException("A group and 64 persistent seeded slots required.");
            this.families = families ?? throw new ArgumentNullException(nameof(families));
            this.allocationFailed = allocationFailed ?? throw new ArgumentNullException(nameof(allocationFailed));
            this.invalidFree = invalidFree ?? throw new ArgumentNullException(nameof(invalidFree));
            slots = new KingdomQuestPineNativeMobBreeder[Capacity];
            var unique = new HashSet<KingdomQuestPineNativeMobBreeder>();
            for (int i = 0; i < Capacity; i++)
            {
                if (explicitSlots[i] == null || !unique.Add(explicitSlots[i])) throw new ArgumentException("Distinct persistent slots required.");
                slots[i] = explicitSlots[i];
            }
            for (int i = 0; i < Capacity; i++)
            {
                slots[i].Group = group; slots[i].Pending = 1; slots[i].Deadline = 0;
                next[i] = (i + 1) % Capacity; previous[i] = (i + Capacity - 1) % Capacity;
            }
            next[Capacity] = previous[Capacity] = Capacity;
        }
        public KingdomQuestPineNativeMobBreeder Get(ushort index, bool absolute = false) =>
            index < Capacity && (absolute || used[index]) ? slots[index] : null;
        public KingdomQuestPineNativeMobBreeder Allocate(ref ushort index)
        {
            if (freeHead < 0) { allocationFailed(); return null; }
            int slot = freeHead; index = (ushort)slot;
            if (next[slot] == slot) freeHead = -1;
            else { freeHead = next[slot]; Unlink(slot); }
            InsertBefore(slot, Capacity); used[slot] = true; Count++;
            slots[slot].Next = null; slots[slot].Previous = null;
            return slots[slot];
        }
        public bool Free(ushort index)
        {
            var obj = Get(index, true);
            bool occupied = index < Capacity && used[index];
            if (!occupied) invalidFree(index);
            else { obj.Next = null; obj.Previous = null; } // do not repair neighbors
            families.Remove(obj); // even inactive/invalid index: absolute slot or null
            if (!occupied) return false;
            Unlink(index);
            if (freeHead < 0) { freeHead = index; next[index] = previous[index] = index; }
            else InsertBefore(index, freeHead);
            used[index] = false; Count--; return true;
        }
        public bool TryRoutine(KingdomQuestPineNativeMobRuntime runtime, Func<bool> tryFreeGroup)
        {
            if (Count == 0) return tryFreeGroup != null && tryFreeGroup();
            int slot = next[Capacity];
            while (slot < Capacity)
            {
                if (!used[slot]) return true; // native traversal false ignored by group
                int successor = next[slot];
                ushort current = (ushort)slot;
                if (!slots[slot].TryRoutine(runtime, () => { Free(current); return true; })) return false;
                slot = successor;
            }
            return true; // last breeder freed: group is freed on a later call
        }
        public bool TryRegenFromLua(KingdomQuestPineNativeMobRuntime runtime)
        {
            if (runtime == null) return false;
            int slot = next[Capacity];
            var seen = new HashSet<int>();
            while (slot < Capacity)
            {
                if (!seen.Add(slot)) return false; // freed-slot free-list cycles are unresolved
                int successor = next[slot];
                if (used[slot] && !runtime.TryRegenerate(slots[slot], slots[slot], out _)) return false;
                slot = successor; // inactive slots skipped; returned handle is NOT stored
            }
            return true;
        }
        private void Unlink(int slot)
        { next[previous[slot]] = next[slot]; previous[next[slot]] = previous[slot]; }
        private void InsertBefore(int slot, int before)
        {
            next[slot] = before; previous[slot] = previous[before];
            next[previous[before]] = slot; previous[before] = slot;
        }
    }
}
