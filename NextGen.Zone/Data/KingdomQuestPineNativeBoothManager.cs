using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestPineBoothOperation
    { AddItem, AddItemRecord, AddStoreItem, RemoveItem, RemoveItemRecord, RemoveStoreItem, RemoveStoreItems, CloseStore, GetItem, GetStoreItem, GetStorePosition }
    public enum KingdomQuestPineBoothError
    { InvalidStoreHandle, InvalidItemHandle, MissingStore, MissingRegistration, MissingItemIndex, MissingBaseData,
      AddItemFailed, AddStoreItemFailed, RollbackFailed, RollbackStateInvalid, RemoveStoreItemFailed, RemoveItemFailed,
      NullSecondItemLookup }

    // Entry is the ItemDataBox index; a null ItemClass means its base-data pointer is null.
    public sealed class KingdomQuestPineNativeBoothItemData
    {
        public int? ItemClass { get; }
        public KingdomQuestPineNativeBoothItemData(int? itemClass) { ItemClass = itemClass; }
    }
    public interface IKingdomQuestPineNativeBoothItemSource
    {
        // false: UNRESOLVED. true/null: a known absent index entry.
        bool TryLookup(ushort itemId, out KingdomQuestPineNativeBoothItemData entry);
    }
    public sealed class KingdomQuestPineNativeBoothItem
    {
        private readonly byte[] bytes;
        public byte ValidSize => bytes[0];
        public uint Handle => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(1));
        public ushort StoreHandle => BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(5));
        public ulong Price => BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(7));
        public ushort ItemId => BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(15));
        internal KingdomQuestPineNativeBoothItem(byte validSize, uint handle, ushort store, ulong price, byte[] item)
        {
            bytes = new byte[118]; bytes[0] = validSize;
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(1), handle);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(5), store);
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(7), price);
            item.CopyTo(bytes, 15); // complete 103-byte SHINE_ITEM_STRUCT, including all 101 attribute bytes
        }
        public byte[] Snapshot() => (byte[])bytes.Clone();
    }
    public readonly struct KingdomQuestPineNativeBoothReference
    {
        public ulong Registration { get; }
        public uint ItemHandle { get; }
        public KingdomQuestPineNativeBoothReference(ulong registration, uint itemHandle)
        { Registration = registration; ItemHandle = itemHandle; }
    }
    public sealed class KingdomQuestPineNativeBoothStore
    {
        internal readonly LinkedList<KingdomQuestPineNativeBoothReference> References = new();
        public ushort Handle { get; internal set; }
        public int X { get; }
        public int Y { get; }
        public IReadOnlyList<KingdomQuestPineNativeBoothReference> Items => References.ToArray();
        internal KingdomQuestPineNativeBoothStore(ushort handle, int x, int y) { Handle = handle; X = x; Y = y; }
    }

    /// <summary>
    /// FieldMap+0x1F0 BoothManager: ordered stores and the separate item list at +0x0C.
    /// Constructor 0x0043B550, open/check/close and item-list operations from Zone.exe.
    /// This owns bookkeeping, not player StreetBooth, inventory transactions or item UpdateItem.
    /// </summary>
    public sealed class KingdomQuestPineNativeBoothManager : IKingdomQuestPineNativeLogoutBooths
    {
        private readonly LinkedList<KingdomQuestPineNativeBoothStore> stores = new();
        private readonly LinkedList<KingdomQuestPineNativeBoothItem> items = new();
        private readonly IKingdomQuestPineNativeBoothItemSource itemSource;
        private readonly Action<KingdomQuestPineBoothOperation, KingdomQuestPineBoothError> report;
        // Default is the native constructor's +0x18 write. Nondefault seeds are explicit restored state.
        public uint LastItemHandle { get; private set; }
        public IReadOnlyList<KingdomQuestPineNativeBoothStore> Stores => stores.ToArray();
        public IReadOnlyList<KingdomQuestPineNativeBoothItem> Items => items.ToArray();
        public KingdomQuestPineNativeBoothManager(
            Action<KingdomQuestPineBoothOperation, KingdomQuestPineBoothError> report,
            IKingdomQuestPineNativeBoothItemSource itemSource = null, uint lastItemHandle = uint.MaxValue)
        {
            this.report = report ?? throw new ArgumentNullException(nameof(report));
            this.itemSource = itemSource; LastItemHandle = lastItemHandle;
        }
        private LinkedListNode<KingdomQuestPineNativeBoothStore> FindStore(ushort handle)
        {
            for (var n = stores.First; n != null; n = n.Next) if (n.Value.Handle == handle) return n;
            return null;
        }
        private LinkedListNode<KingdomQuestPineNativeBoothItem> FindItem(uint handle)
        {
            for (var n = items.First; n != null; n = n.Next) if (n.Value.Handle == handle) return n;
            return null;
        }
        private bool Fail(KingdomQuestPineBoothOperation op, KingdomQuestPineBoothError error)
        { report(op, error); return false; }
        // 0x0043B820 -> 0x0043B6A0. Duplicates and signed coordinates are accepted.
        public bool OpenStore(ushort handle, int x, int y)
        {
            if (handle == ushort.MaxValue) return false; // wrapper suppresses inner assertion
            stores.AddLast(new KingdomQuestPineNativeBoothStore(handle, x, y)); return true;
        }
        public bool TryCheckStoreIsOpening(ushort handle, out byte result)
        { result = handle != ushort.MaxValue && FindStore(handle) != null ? (byte)1 : (byte)0; return true; }
        public KingdomQuestPineNativeBoothStore GetStorePosition(ushort handle)
        {
            if (handle == ushort.MaxValue) return null; // 0x00439C30 wrapper
            var n = FindStore(handle);
            if (n == null) report(KingdomQuestPineBoothOperation.GetStorePosition, KingdomQuestPineBoothError.MissingStore);
            return n?.Value;
        }
        // 0x0062DCA0 jump table at 0x0062DD1C. The class-0 table slot is unreachable.
        public static byte AttributeSize(int itemClass)
        {
            switch (itemClass)
            {
                case 1: case 3: case 21: case 35: return 2;
                case 2: case 18: case 24: case 26: case 27: case 37: return 4;
                case 4: return 62;
                case 5: return 89;
                case 6: case 7: case 8: case 38: return 37;
                case 9: return 36;
                case 10: return 8;
                case 13: return 101;
                case 15: return 65;
                case 23: return 19;
                case 29: return 12;
                case 30: return 9;
                case 31: return 5;
                case 36: return 26;
                default: return itemClass >= 1 && itemClass <= 38 && itemClass != 32 ? (byte)1 : (byte)0;
            }
        }
        // 0x00439760. No collision scan; 0xFFFFFFFE and 0xFFFFFFFF are skipped on increment.
        public uint GenerateNewItemHandle()
        {
            uint next = LastItemHandle == uint.MaxValue ? 0 : unchecked(LastItemHandle + 1);
            LastItemHandle = next >= uint.MaxValue - 1 ? 0 : next; return LastItemHandle;
        }
        // 0x0043A6C0. Native item add happens BEFORE the store-reference add.
        public bool TryAddItemRecord(ushort store, ulong price, ReadOnlySpan<byte> itemStruct, out uint handle)
        {
            handle = uint.MaxValue;
            if (itemStruct.Length != 103) throw new ArgumentException("Native SHINE_ITEM_STRUCT must contain 103 bytes.", nameof(itemStruct));
            byte[] item = itemStruct.ToArray(); // native by-value copy precedes lookup callbacks
            if (store == ushort.MaxValue) { Fail(KingdomQuestPineBoothOperation.AddItemRecord, KingdomQuestPineBoothError.InvalidStoreHandle); return true; }
            ushort id = BinaryPrimitives.ReadUInt16LittleEndian(item);
            if (id == ushort.MaxValue) return true;
            if (itemSource == null || !itemSource.TryLookup(id, out var index)) return false;
            if (index == null) { Fail(KingdomQuestPineBoothOperation.AddItemRecord, KingdomQuestPineBoothError.MissingItemIndex); return true; }
            // The original repeats ItemDataBox lookup and then dereferences the returned index.
            if (!itemSource.TryLookup(id, out index)) return false;
            if (index == null) { Fail(KingdomQuestPineBoothOperation.AddItemRecord, KingdomQuestPineBoothError.NullSecondItemLookup); return false; }
            if (!index.ItemClass.HasValue) { Fail(KingdomQuestPineBoothOperation.AddItemRecord, KingdomQuestPineBoothError.MissingBaseData); return true; }
            byte size = (byte)(16 + AttributeSize(index.ItemClass.Value)); // 0x0062DDC0
            uint generated = GenerateNewItemHandle();
            items.AddLast(new KingdomQuestPineNativeBoothItem(size, generated, store, price, item));
            handle = generated; return true;
        }
        // 0x0043AA30. Does not check whether the supplied item handle exists or is already referenced.
        public bool AddStoreItem(ushort store, ulong registration, uint itemHandle)
        {
            const KingdomQuestPineBoothOperation op = KingdomQuestPineBoothOperation.AddStoreItem;
            if (store == ushort.MaxValue) return Fail(op, KingdomQuestPineBoothError.InvalidStoreHandle);
            if (itemHandle == uint.MaxValue) return Fail(op, KingdomQuestPineBoothError.InvalidItemHandle);
            var n = FindStore(store);
            if (n == null) return Fail(op, KingdomQuestPineBoothError.MissingStore);
            n.Value.References.AddLast(new KingdomQuestPineNativeBoothReference(registration, itemHandle)); return true;
        }
        public bool TryAddItem(ushort store, ulong registration, ulong price, ReadOnlySpan<byte> itemStruct, out bool result)
        {
            result = false;
            const KingdomQuestPineBoothOperation op = KingdomQuestPineBoothOperation.AddItem;
            if (store == ushort.MaxValue) { Fail(op, KingdomQuestPineBoothError.InvalidStoreHandle); return true; }
            if (!TryAddItemRecord(store, price, itemStruct, out uint handle)) return false;
            if (handle == uint.MaxValue) { Fail(op, KingdomQuestPineBoothError.AddItemFailed); return true; }
            if (!AddStoreItem(store, registration, handle))
            {
                report(op, KingdomQuestPineBoothError.AddStoreItemFailed);
                if (!RemoveItemRecord(handle))
                {
                    report(op, KingdomQuestPineBoothError.RollbackFailed);
                    report(op, KingdomQuestPineBoothError.RollbackStateInvalid); return true;
                }
            }
            result = true; return true; // 0x0043AD0C: successful rollback ALSO returns one
        }
        public KingdomQuestPineNativeBoothItem GetItem(uint handle)
        {
            if (handle == uint.MaxValue) { Fail(KingdomQuestPineBoothOperation.GetItem, KingdomQuestPineBoothError.InvalidItemHandle); return null; }
            return FindItem(handle)?.Value;
        }
        public KingdomQuestPineNativeBoothReference? GetStoreItem(ushort store, ulong registration)
        {
            if (store == ushort.MaxValue) { Fail(KingdomQuestPineBoothOperation.GetStoreItem, KingdomQuestPineBoothError.InvalidStoreHandle); return null; }
            var n = FindStore(store);
            if (n != null) foreach (var r in n.Value.References) if (r.Registration == registration) return r;
            return null;
        }
        // 0x0043A420. Remove the FIRST full 64-bit key match and return its item handle.
        public uint RemoveStoreItem(ushort store, ulong registration)
        {
            const KingdomQuestPineBoothOperation op = KingdomQuestPineBoothOperation.RemoveStoreItem;
            if (store == ushort.MaxValue) { Fail(op, KingdomQuestPineBoothError.InvalidStoreHandle); return uint.MaxValue; }
            var n = FindStore(store);
            if (n == null) { Fail(op, KingdomQuestPineBoothError.MissingStore); return uint.MaxValue; }
            for (var r = n.Value.References.First; r != null; r = r.Next)
                if (r.Value.Registration == registration)
                { uint result = r.Value.ItemHandle; n.Value.References.Remove(r); return result; }
            Fail(op, KingdomQuestPineBoothError.MissingRegistration); return uint.MaxValue;
        }
        public bool RemoveItemRecord(uint handle)
        {
            if (handle == uint.MaxValue) return Fail(KingdomQuestPineBoothOperation.RemoveItemRecord, KingdomQuestPineBoothError.InvalidItemHandle);
            var n = FindItem(handle); if (n == null) return false;
            items.Remove(n); return true;
        }
        // 0x0043AD30. A missing item after reference removal is logged, but the native result stays true.
        public bool RemoveItem(ushort store, ulong registration)
        {
            const KingdomQuestPineBoothOperation op = KingdomQuestPineBoothOperation.RemoveItem;
            if (store == ushort.MaxValue) return Fail(op, KingdomQuestPineBoothError.InvalidStoreHandle);
            uint handle = RemoveStoreItem(store, registration);
            if (handle == uint.MaxValue) return Fail(op, KingdomQuestPineBoothError.RemoveStoreItemFailed);
            if (!RemoveItemRecord(handle)) report(op, KingdomQuestPineBoothError.RemoveItemFailed);
            return true;
        }
        // 0x0043A950: remove ALL records matching the WORD store handle, including unreferenced records.
        public bool RemoveItemsOfOneStore(ushort store)
        {
            if (store == ushort.MaxValue) return Fail(KingdomQuestPineBoothOperation.RemoveStoreItems, KingdomQuestPineBoothError.InvalidStoreHandle);
            var n = items.First;
            while (n != null) { var next = n.Next; if (n.Value.StoreHandle == store) items.Remove(n); n = next; }
            return true; // even if zero matches
        }
        public bool TryCloseStore(ushort handle, out bool result)
        {
            result = false;
            if (handle == ushort.MaxValue) return true; // outer wrapper, no assertion or mutations
            if (!RemoveItemsOfOneStore(handle)) return true;
            var n = FindStore(handle);
            if (n == null) { Fail(KingdomQuestPineBoothOperation.CloseStore, KingdomQuestPineBoothError.MissingStore); return true; }
            n.Value.References.Clear(); n.Value.Handle = ushort.MaxValue; stores.Remove(n);
            result = true; return true; // only first store removed; duplicates are not normalized
        }
        // 0x00439B70: validity/order and two independent list searches, no owner cross-check.
        public ushort ReconfirmExisting(ushort store, uint itemHandle)
        {
            if (store == ushort.MaxValue) return 0x31D1;
            if (itemHandle == uint.MaxValue) return 0x31D2;
            var n = FindStore(store); if (n == null) return 0x31D3;
            foreach (var r in n.Value.References)
                if (r.ItemHandle == itemHandle) return FindItem(itemHandle) != null ? (ushort)0x31C8 : (ushort)0x31D4;
            return 0x31D4;
        }
    }
}
