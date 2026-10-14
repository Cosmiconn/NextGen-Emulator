using System.Buffers.Binary;
using NextGen.Zone.Data;
using Op = NextGen.Zone.Data.KingdomQuestPineBoothOperation;
using Error = NextGen.Zone.Data.KingdomQuestPineBoothError;

static class NativeBoothManagerTests
{
    static void Check(bool ok, string name) { if (!ok) throw new Exception("native booths: " + name); }
    public static void Run()
    {
        StoresAndHandles(); ItemCopiesAndLookups(); AddRollback(); RemovalAndClose();
        Console.WriteLine("PASS: native BoothManager lists, duplicate stores/keys, 118-byte item copies, handle wrap, two-stage lookup, rollback return values, partial removal and logout ownership");
    }
    internal static byte[] Item(ushort id = 123)
    {
        var bytes = Enumerable.Range(0, 103).Select(i => (byte)(i * 37)).ToArray();
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, id); return bytes;
    }
    internal sealed class Source : IKingdomQuestPineNativeBoothItemSource
    {
        public int Calls, Class = 13;
        public bool Available = true;
        public Func<int, (bool Available, KingdomQuestPineNativeBoothItemData Entry)> OnLookup;
        public bool TryLookup(ushort id, out KingdomQuestPineNativeBoothItemData entry)
        {
            Calls++;
            var result = OnLookup == null ? (Available, new KingdomQuestPineNativeBoothItemData(Class)) : OnLookup(Calls);
            entry = result.Item2; return result.Item1;
        }
    }
    static void StoresAndHandles()
    {
        var events = new List<(Op, Error)>(); var m = new KingdomQuestPineNativeBoothManager((o, e) => events.Add((o, e)));
        Check(m.Stores.Count == 0 && m.Items.Count == 0 && m.LastItemHandle == uint.MaxValue, "native constructor owns two empty lists and sentinel counter");
        Check(!m.OpenStore(ushort.MaxValue, 0, 0) && m.TryCheckStoreIsOpening(ushort.MaxValue, out byte open) && open == 0 &&
            m.TryCloseStore(ushort.MaxValue, out bool closed) && !closed && m.GetStorePosition(ushort.MaxValue) == null && events.Count == 0,
            "wrapper sentinel short-circuits without inner assertions");
        Check(m.OpenStore(3, -17, int.MinValue) && m.OpenStore(3, 45, 67) && m.OpenStore(4, 89, 90), "duplicates and raw signed positions accepted");
        Check(m.TryCheckStoreIsOpening(3, out open) && open == 1 && m.TryCheckStoreIsOpening(5, out open) && open == 0 &&
            m.GetStorePosition(3).X == -17 && m.GetStorePosition(3).Y == int.MinValue, "first store lookup and existence");
        var first = m.Stores[0]; var second = m.Stores[1];
        Check(m.TryCloseStore(3, out closed) && closed && first.Handle == ushort.MaxValue &&
            ReferenceEquals(m.GetStorePosition(3), second) && m.Stores.Select(s => s.Handle).SequenceEqual(new ushort[] { 3, 4 }),
            "close invalidates/removes first duplicate only");
        Check(m.GenerateNewItemHandle() == 0 && m.GenerateNewItemHandle() == 1, "fresh handles start at zero");
        foreach (var (seed, expected) in new[] { (0xFFFFFFFCu, 0xFFFFFFFDu), (0xFFFFFFFDu, 0u), (0xFFFFFFFEu, 0u), (0xFFFFFFFFu, 0u) })
        { var sequence = new KingdomQuestPineNativeBoothManager((_, _) => { }, lastItemHandle: seed); Check(sequence.GenerateNewItemHandle() == expected, "unsigned wrap " + seed); }
        byte[] sizes = { 0,2,4,2,62,89,37,37,37,36,8,1,1,101,1,65,1,1,4,1,1,2,1,19,4,1,4,4,1,12,9,5,0,1,1,2,26,4,37 };
        Check(sizes.Select((n, i) => KingdomQuestPineNativeBoothManager.AttributeSize(i) == n).All(b => b) &&
            KingdomQuestPineNativeBoothManager.AttributeSize(-1) == 0 && KingdomQuestPineNativeBoothManager.AttributeSize(39) == 0 &&
            KingdomQuestPineNativeBoothManager.AttributeSize(int.MinValue) == 0, "all native table cases and unsigned range gate including unreachable class zero");
    }
    static void ItemCopiesAndLookups()
    {
        var events = new List<(Op, Error)>(); var source = new Source();
        var m = new KingdomQuestPineNativeBoothManager((o, e) => events.Add((o, e)), source);
        byte[] input = Item(), original = (byte[])input.Clone();
        source.OnLookup = call => { input[30] = 9; return (true, new(call == 1 ? null : 5)); };
        Check(m.TryAddItemRecord(42, 0xFEDCBA9876543210, input, out uint handle) && handle == 0 && source.Calls == 2,
            "first entry only checked for existence; second base pointer supplies class");
        var item = m.GetItem(handle); var bytes = item.Snapshot();
        Check(bytes.Length == 118 && bytes[0] == 105 && BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(1)) == 0 &&
            BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(5)) == 42 && BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(7)) == 0xFEDCBA9876543210 &&
            bytes.AsSpan(15).SequenceEqual(original) && item.Price == 0xFEDCBA9876543210 && item.ItemId == 123,
            "exact packed offsets, full opaque attributes and by-value copy before callbacks");
        bytes[117] ^= 255; Check(item.Snapshot()[117] == original[102], "snapshot cannot mutate owned record");
        Check(m.Stores.Count == 0, "item manager does not require a store record");
        source.OnLookup = _ => (true, null); source.Calls = 0;
        Check(m.TryAddItemRecord(42, 0, original, out handle) && handle == uint.MaxValue && source.Calls == 1 && m.LastItemHandle == 0 &&
            events.Last() == (Op.AddItemRecord, Error.MissingItemIndex), "known absent index before generation");
        source.OnLookup = call => (true, call == 1 ? new(13) : new(null)); source.Calls = 0;
        Check(m.TryAddItemRecord(42, 0, original, out handle) && handle == uint.MaxValue && source.Calls == 2 &&
            events.Last() == (Op.AddItemRecord, Error.MissingBaseData), "known null base pointer");
        source.OnLookup = call => (true, call == 1 ? new(13) : null); source.Calls = 0;
        Check(!m.TryAddItemRecord(42, 0, original, out handle) && handle == uint.MaxValue && m.LastItemHandle == 0,
            "null repeated lookup would dereference null: unresolved, not native rejection");
        source.OnLookup = call => (call != 2, new(13)); source.Calls = 0;
        Check(!m.TryAddItem(42, 2, 3, original, out bool result) && !result && m.Items.Count == 1 && m.LastItemHandle == 0,
            "unresolved repeated lookup does not enter wrapper rollback or consume handle");
        source.Calls = 0; int reports = events.Count;
        Check(m.TryAddItemRecord(42, 0, Item(ushort.MaxValue), out handle) && handle == uint.MaxValue && source.Calls == 0 && events.Count == reports,
            "FFFF item ID returns sentinel without lookup or inner assertion");
        Check(m.TryAddItem(42, 0, 0, Item(ushort.MaxValue), out result) && !result && events.Last() == (Op.AddItem, Error.AddItemFailed),
            "wrapper logs item failure separately");
        var unknown = new KingdomQuestPineNativeBoothManager((_, _) => { });
        Check(!unknown.TryAddItem(42, 0, 0, original, out result) && !result && unknown.Items.Count == 0 &&
            unknown.TryAddItem(ushort.MaxValue, 0, 0, ReadOnlySpan<byte>.Empty, out result) && !result,
            "missing data binding is unresolved; invalid store guard precedes payload/data access");
    }
    static void AddRollback()
    {
        var events = new List<(Op, Error)>(); var source = new Source();
        var m = new KingdomQuestPineNativeBoothManager((o, e) => events.Add((o, e)), source);
        Check(m.TryAddItem(8, ulong.MaxValue, 9, Item(), out bool result) && result && m.Items.Count == 0 && m.LastItemHandle == 0 &&
            events.SequenceEqual(new[] { (Op.AddStoreItem, Error.MissingStore), (Op.AddItem, Error.AddStoreItemFailed) }),
            "missing store rolls item back yet returns native success and retains consumed handle");
        Check(m.OpenStore(8, 0, 0) && m.TryAddItem(8, ulong.MaxValue, 9, Item(), out result) && result &&
            m.GetStoreItem(8, ulong.MaxValue).Value.ItemHandle == 1, "no guessed invalid-registration guard");
        Check(m.TryAddItem(8, ulong.MaxValue, 10, Item(), out result) && result && m.Stores[0].Items.Count == 2 &&
            m.GetStoreItem(8, ulong.MaxValue).Value.ItemHandle == 1, "duplicate registration keys append; lookup selects first");
        KingdomQuestPineNativeBoothManager interrupted = null;
        interrupted = new((o, e) =>
        {
            if (o == Op.AddItem && e == Error.AddStoreItemFailed) throw new InvalidOperationException("assert sink");
        }, source);
        bool threw = false;
        try { interrupted.TryAddItem(8, 3, 4, Item(), out _); } catch (InvalidOperationException) { threw = true; }
        Check(threw && interrupted.Items.Count == 1 && interrupted.LastItemHandle == 0, "assertion exception keeps item before rollback");
        KingdomQuestPineNativeBoothManager changed = null; events.Clear();
        changed = new((o, e) =>
        {
            events.Add((o, e));
            if (o == Op.AddItem && e == Error.AddStoreItemFailed) changed.RemoveItemRecord(changed.Items.Single().Handle);
        }, source);
        Check(changed.TryAddItem(8, 1, 2, Item(), out result) && !result && changed.Items.Count == 0 &&
            events.TakeLast(2).SequenceEqual(new[] { (Op.AddItem, Error.RollbackFailed), (Op.AddItem, Error.RollbackStateInvalid) }),
            "failed rollback produces both assertions then native false");
    }
    static void RemovalAndClose()
    {
        var events = new List<(Op, Error)>(); var m = new KingdomQuestPineNativeBoothManager((o, e) => events.Add((o, e)), new Source());
        m.OpenStore(7, 1, 2); m.OpenStore(8, 3, 4);
        Check(m.TryAddItem(7, 0x100000001, 50, Item(), out bool result) && result && m.TryAddItem(7, 1, 60, Item(), out result) && result,
            "two registrations differing only in upper DWORD");
        Check(m.GetStoreItem(7, 0x100000001).Value.ItemHandle == 0 && m.GetStoreItem(7, 1).Value.ItemHandle == 1 &&
            m.RemoveItem(7, 1) && m.GetItem(1) == null && m.GetItem(0) != null, "full registration comparison, exact removal");
        Check(m.ReconfirmExisting(ushort.MaxValue, uint.MaxValue) == 0x31D1 && m.ReconfirmExisting(7, uint.MaxValue) == 0x31D2 &&
            m.ReconfirmExisting(9, 0) == 0x31D3 && m.ReconfirmExisting(8, 0) == 0x31D4 && m.ReconfirmExisting(7, 0) == 0x31C8,
            "reconfirm error priority and success codes");
        Check(m.AddStoreItem(8, 1, 0) && m.ReconfirmExisting(8, 0) == 0x31C8, "reconfirm does not compare item owner with requested store");
        Check(m.RemoveItemRecord(0) && m.ReconfirmExisting(8, 0) == 0x31D4 && m.RemoveItem(7, 0x100000001) &&
            m.Stores[0].Items.Count == 0 && events.Last() == (Op.RemoveItem, Error.RemoveItemFailed),
            "reference removed before missing-item assertion, wrapper still succeeds");
        Check(!m.RemoveItem(7, 1) && events.TakeLast(2).SequenceEqual(new[] { (Op.RemoveStoreItem, Error.MissingRegistration), (Op.RemoveItem, Error.RemoveStoreItemFailed) }),
            "missing key reports inner and outer errors and returns false");
        m.TryAddItemRecord(7, 0, Item(), out uint a); m.TryAddItemRecord(8, 0, Item(), out uint b); m.TryAddItemRecord(7, 0, Item(), out uint c);
        m.AddStoreItem(7, 9, b); // closing must use record owner, not these reference handles
        var store = m.GetStorePosition(7);
        Check(m.TryCloseStore(7, out result) && result && m.Items.Select(x => x.Handle).SequenceEqual(new[] { b }) &&
            store.Items.Count == 0 && store.Handle == ushort.MaxValue && m.Stores.Single().Handle == 8,
            "close removes all matching records, even unreferenced, and preserves other owner's item despite reference");
        m.TryAddItemRecord(12, 0, Item(), out uint orphan); events.Clear();
        Check(m.TryCloseStore(12, out result) && !result && m.GetItem(orphan) == null && m.Items.Single().Handle == b &&
            events.Single() == (Op.CloseStore, Error.MissingStore), "missing store close keeps earlier orphan cleanup and reports false");
        Check(m.RemoveItemsOfOneStore(12), "zero matching records still success");
        events.Clear();
        Check(!m.RemoveItemsOfOneStore(ushort.MaxValue) && !m.RemoveItemRecord(uint.MaxValue) &&
            !m.AddStoreItem(8, 9, uint.MaxValue) && events.Count == 3, "low-level invalid sentinels retain distinct assertions");
    }
}
