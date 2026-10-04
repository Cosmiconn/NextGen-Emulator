using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// ShineObjectHandleUnion::sohu_HandleSplit 0x00633650 and constructors
    /// 0x00548E00..0x00549062. Native handles address Zone-wide type pools.
    /// </summary>
    public static class KingdomQuestPineNativeObjectHandles
    {
        // Indexed by the original so_ObjectType, NOT by handle range order.
        private static readonly ushort[] starts = {
            0x34BC, 0x2904, 0x1F40, 0x251C, 0x42BC, 0x0000, 0x4FA4,
            0x509E, 0x43BC, 0x4BBC, 0x5486, 0x567A, 0x5C56 };
        private static readonly ushort[] counts = {
            3584, 3000, 1500, 1000, 256, 8000, 250, 1000, 2048, 1000, 500, 1000, 1500 };
        public static bool TryGetRange(ushort type, out ushort first, out ushort count)
        {
            first = ushort.MaxValue; count = 0;
            if (type >= starts.Length) return false;
            first = starts[type]; count = counts[type]; return true;
        }
        public static ushort Encode(ushort type, ushort index)
        {
            ushort first, count;
            return TryGetRange(type, out first, out count) && index < count
                ? (ushort)(first + index) : ushort.MaxValue;
        }
        public static bool TrySplit(ushort handle, out KingdomQuestPineNativeObjectType type, out ushort index)
        {
            for (int i = 0; i < starts.Length; i++)
                if (handle >= starts[i] && handle < starts[i] + counts[i])
                { type = (KingdomQuestPineNativeObjectType)i; index = (ushort)(handle - starts[i]); return true; }
            type = (KingdomQuestPineNativeObjectType)byte.MaxValue;
            index = ushort.MaxValue;
            return false;
        }
    }

    public interface IKingdomQuestPineNativePoolObject
    {
        // Set once during native-equivalent pool construction, not allocation.
        KingdomQuestPineNativeObjectType NativeObjectType { get; }
        ushort NativeHandle { get; }
        void InitializeForAllocation(); // vtable +0xE78, while slot is occupied
        void FreeFromLists();           // vtable +0x2C, before clearing occupancy
        // vtable +0x2F0, with the native shared argument (0x132815E8).
        // The concrete object owns that dependency; do not substitute Dispose.
        void InvokeManagerFreeCallback();
    }

    /// <summary>
    /// Native pool identity/occupancy, allocation and immediate free. Bind the
    /// complete preconstructed pools once per Zone, shared by ALL maps. No
    /// emulator MapObjectID allocation, map marking or visibility is implied.
    /// Calls and object callbacks require the native owning thread's serialization.
    /// </summary>
    public sealed class KingdomQuestPineNativeObjectManager
    {
        private readonly Pool[] pools = new Pool[13];
        private readonly HashSet<uint> reportedMissing = new HashSet<uint>();
        private readonly Action<ushort> reportInvalidType;
        private readonly Action<ushort, int> reportMissing;

        public KingdomQuestPineNativeObjectManager(Action<ushort> reportInvalidType,
            Action<ushort, int> reportMissing)
        {
            this.reportInvalidType = reportInvalidType ?? throw new ArgumentNullException(nameof(reportInvalidType));
            this.reportMissing = reportMissing ?? throw new ArgumentNullException(nameof(reportMissing));
        }

        // som_Initialize preconstructs every object and assigns its permanent
        // handle. Missing/incomplete pools are unresolved, never native exhaustion.
        public bool TryBindPool(ushort type, IReadOnlyList<IKingdomQuestPineNativePoolObject> objects)
        {
            ushort first, count;
            if (!KingdomQuestPineNativeObjectHandles.TryGetRange(type, out first, out count) ||
                pools[type] != null || objects == null || objects.Count != count) return false;
            var copy = new IKingdomQuestPineNativePoolObject[count];
            for (int i = 0; i < count; i++)
            {
                var obj = objects[i];
                if (obj == null || (ushort)obj.NativeObjectType != type || obj.NativeHandle != first + i) return false;
                copy[i] = obj;
            }
            pools[type] = new Pool(copy);
            return true;
        }

        // Bool is availability; object null is the original allocation failure.
        // ref preserves the untouched output on the native invalid-type branch.
        public bool TryAllocate(ushort type, ref ushort handle, out IKingdomQuestPineNativePoolObject obj)
        {
            obj = null;
            if (type >= pools.Length) { reportInvalidType(type); return true; }
            Pool pool = pools[type];
            if (pool == null) return false;
            // Native reuses the type argument's storage as l_AllocationZ's
            // output. Exhaustion leaves it unchanged, then STILL encodes it.
            ushort index = type;
            obj = pool.Allocate(ref index);
            handle = KingdomQuestPineNativeObjectHandles.Encode(type, index);
            if (obj != null) obj.InitializeForAllocation();
            return true;
        }

        public bool TryGetObject(ushort handle, out IKingdomQuestPineNativePoolObject obj) =>
            TryGet(handle, false, out obj);
        public bool TryGetObjectAbsolute(ushort handle, out IKingdomQuestPineNativePoolObject obj) =>
            TryGet(handle, true, out obj);
        private bool TryGet(ushort handle, bool absolute, out IKingdomQuestPineNativePoolObject obj)
        {
            obj = null;
            KingdomQuestPineNativeObjectType type; ushort index;
            if (!KingdomQuestPineNativeObjectHandles.TrySplit(handle, out type, out index)) return true;
            var pool = pools[(int)type];
            if (pool == null) return false;
            obj = pool.Get(index, absolute);
            return true;
        }

        public bool TryFree(ushort handle, int removeWhen, int reason, out bool nativeResult)
        {
            nativeResult = false;
            // 0x00557DDE -> 0x00558011: value 1 immediately returns true;
            // it does not look up, enqueue, detach or free anything here.
            if (removeWhen == 1) { nativeResult = true; return true; }
            IKingdomQuestPineNativePoolObject obj;
            if (!TryGetObject(handle, out obj)) return false;
            if (obj == null)
            {
                uint key = unchecked(((uint)reason << 16) | handle);
                if (!reportedMissing.Contains(key))
                {
                    reportMissing(handle, reason);
                    reportedMissing.Add(key);
                }
                return true;
            }
            obj.FreeFromLists();
            obj.InvokeManagerFreeCallback();
            KingdomQuestPineNativeObjectType type; ushort index;
            KingdomQuestPineNativeObjectHandles.TrySplit(handle, out type, out index);
            nativeResult = pools[(int)type].Free(index);
            return true;
        }

        public bool TryGetAllocatedCount(ushort type, out int count)
        {
            count = 0;
            if (type >= pools.Length || pools[type] == null) return false;
            count = pools[type].Count; return true;
        }

        // l_4AllInList 0x004026E0 caches the successor BEFORE its callback. A
        // callback can free the current slot; a freed successor stops the loop.
        public bool TryVisitAllocated(ushort type,
            Func<IKingdomQuestPineNativePoolObject, ushort, bool> visit, out bool nativeResult)
        {
            nativeResult = false;
            if (type >= pools.Length || pools[type] == null || visit == null) return false;
            nativeResult = pools[type].Visit(visit); return true;
        }

        // l_MakeList 0x005546F0, AllocZ 0x005D08C0, Free 0x004B1300:
        // circular free list + occupied list with a permanent sentinel.
        // Released slots append to the free tail; objects themselves survive.
        private sealed class Pool
        {
            private readonly IKingdomQuestPineNativePoolObject[] objects;
            private readonly int[] next, previous;
            private readonly bool[] used;
            private readonly int sentinel;
            private int freeHead;
            public int Count { get; private set; }
            public Pool(IKingdomQuestPineNativePoolObject[] objects)
            {
                this.objects = objects;
                sentinel = objects.Length;
                next = new int[sentinel + 1]; previous = new int[sentinel + 1]; used = new bool[sentinel];
                for (int i = 0; i < sentinel; i++)
                { next[i] = (i + 1) % sentinel; previous[i] = (i + sentinel - 1) % sentinel; }
                next[sentinel] = previous[sentinel] = sentinel;
                freeHead = 0;
            }
            public IKingdomQuestPineNativePoolObject Get(ushort index, bool absolute) =>
                absolute || used[index] ? objects[index] : null;
            public IKingdomQuestPineNativePoolObject Allocate(ref ushort index)
            {
                if (freeHead < 0) return null;
                int slot = freeHead;
                index = (ushort)slot;
                if (next[slot] == slot) freeHead = -1;
                else { freeHead = next[slot]; Unlink(slot); }
                InsertBefore(slot, sentinel);
                used[slot] = true; Count++;
                return objects[slot];
            }
            public bool Free(ushort slot)
            {
                if (!used[slot]) return false;
                Unlink(slot);
                if (freeHead < 0) { freeHead = slot; next[slot] = previous[slot] = slot; }
                else InsertBefore(slot, freeHead);
                used[slot] = false; Count--;
                return true;
            }
            private void Unlink(int slot)
            { next[previous[slot]] = next[slot]; previous[next[slot]] = previous[slot]; }
            private void InsertBefore(int slot, int before)
            {
                next[slot] = before; previous[slot] = previous[before];
                next[previous[before]] = slot; previous[before] = slot;
            }
            public bool Visit(Func<IKingdomQuestPineNativePoolObject, ushort, bool> visit)
            {
                int slot = next[sentinel];
                while (slot < sentinel)
                {
                    if (!used[slot]) return false;
                    int successor = next[slot];
                    if (!visit(objects[slot], (ushort)slot)) return false;
                    slot = successor;
                }
                return true;
            }
        }
    }
}
