using System.IO.Compression;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using NextGen.FiestaLib.Data;
using NextGen.Zone.Data;
using NextGen.Zone.Game;

static class CollisionTests
{
    static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception("collision: " + name);
    }
    static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    static void Set(object o, string p, object v) => o.GetType().GetProperty(p).SetValue(o, v);
    static byte[] Bitmap(int width, int height, params byte[] bytes)
    {
        using var m = new MemoryStream();
        using var w = new BinaryWriter(m);
        w.Write(width); w.Write(height); w.Write(bytes);
        return m.ToArray();
    }
    static byte[] DoorSource(byte[] name)
    {
        using var m = new MemoryStream();
        using var w = new BinaryWriter(m);
        w.Write(1); w.Write(name);
        foreach (int n in new[] { 0, 0, 7, 1, 2, 0 }) w.Write(n);
        w.Write(new byte[] { 4, 8, 1, 16 }); // closed masks, then open row bytes
        return m.ToArray();
    }

    public static void Run()
    {
        byte[] name = new byte[32]; name[0] = (byte)'G';
        byte[] shbd = Bitmap(2, 2, 1, 128, 2, 64);
        byte[] sbi = DoorSource(name);
        Check(KingdomQuestMapCollision.TryLoad("fixture", shbd, null, sbi, out var map), "fixture parse");
        Check(map.DoorCount == 1 && map.RowBytes == 2 && map.Rows == 2, "header geometry");
        Check(!map.CanWalk(6, 0) && map.CanWalk(7, 0), "6.25-unit cell boundary");
        Check(map.CanWalk(13, 0), "door cell initially open");
        map.CloseAllDoors();
        Check(map.SnapshotBitmap().SequenceEqual(new byte[] { 5, 128, 10, 64 }),
            "close ORs masks and preserves adjacent row bytes");
        Check(!map.CanWalk(13, 0), "closed door blocks movement lookup");
        map.CloseAllDoors();
        Check(map.SnapshotBitmap().SequenceEqual(new byte[] { 5, 128, 10, 64 }), "close idempotent");
        Check(map.TryDoorAction(name, true) && map.CanWalk(13, 0), "open restores traversability");
        Check(map.SnapshotBitmap().SequenceEqual(new byte[] { 1, 128, 16, 64 }),
            "open copies saved rows, not AND/OR mask and not base bitmap");
        var changedName = (byte[])name.Clone(); changedName[31] = 1;
        Check(!map.TryDoorAction(changedName, false), "all 32 name bytes significant");
        changedName = (byte[])name.Clone(); changedName[0] = (byte)'g';
        Check(!map.TryDoorAction(changedName, false), "case-sensitive native identity");
        Check(!map.TryDoorAction(null, false) && !map.TryDoorAction(new byte[31], false), "name boundaries");
        Check(!map.CanWalk(-1, 0) && !map.CanWalk(100, 0) && !map.CanWalk(0, 13), "out of grid");
        shbd[8] = 255; sbi[^1] = 255;
        Check(map.SnapshotBitmap().SequenceEqual(new byte[] { 1, 128, 16, 64 }), "source buffers isolated");
        var snapshot = map.SnapshotBitmap(); snapshot[0] = 0;
        Check(map.SnapshotBitmap()[0] == 1, "snapshot isolated");

        Check(KingdomQuestMapCollision.TryLoad("overlay", Bitmap(1, 1, 1), Bitmap(1, 1, 4),
            null, out var overlay) && overlay.SnapshotBitmap()[0] == 5, "SHAB merge is OR");
        Check(KingdomQuestMapCollision.TryLoad("absent", Bitmap(1, 1, 1), null, null, out var empty),
            "verified absent optional door file");
        empty.CloseAllDoors();
        Check(empty.DoorCount == 0 && empty.SnapshotBitmap()[0] == 1, "no-door close is empty native loop");
        foreach (byte[] invalid in new[] { Array.Empty<byte>(), Bitmap(0, 1), Bitmap(-1, 1),
            Bitmap(int.MaxValue, 2), Bitmap(2, 2, 0), Bitmap(1, 1, 0, 0) })
            Check(!KingdomQuestMapCollision.TryLoad("bad", invalid, null, null, out _), "invalid bitmap rejected");
        Check(!KingdomQuestMapCollision.TryLoad("bad", Bitmap(1, 1, 0), Bitmap(2, 1, 0, 0),
            null, out _), "overlay dimensions must match");
        foreach (var (position, value) in new[] { (0, 32), (36, -1), (40, -1), (44, 8),
            (48, 2), (52, 3), (56, int.MaxValue) })
        {
            var bad = DoorSource(name);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(position), value);
            Check(!KingdomQuestMapCollision.TryLoad("bad", Bitmap(2, 2, 0, 0, 0, 0),
                null, bad, out _), "bad SBI field " + position);
        }
        Check(!KingdomQuestMapCollision.TryLoad("bad", Bitmap(2, 2, 0, 0, 0, 0),
            null, DoorSource(name)[..^1], out _), "truncated door payload rejected");

        var sources = new Dictionary<string, byte[]>();
        using (var stream = typeof(KingdomQuestMapCollisionSource).Assembly.GetManifestResourceStream(
            KingdomQuestMapCollisionSource.ResourceName))
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
            foreach (var e in zip.Entries)
                using (var input = e.Open())
                using (var output = new MemoryStream())
                { input.CopyTo(output); sources[e.FullName] = output.ToArray(); }
        var bases = sources.Keys.Where(k => k.EndsWith(".shbd")).Select(k => k[..^5]).ToArray();
        Check(bases.Length == 23 && sources.Count == 38, "complete source corpus");
        int doors = 0;
        foreach (string baseName in bases)
        {
            Check(KingdomQuestMapCollisionSource.TryCreate(baseName, out var current), "source parse " + baseName);
            Check(KingdomQuestMapCollisionSource.TryCreate(baseName, out var other), "second instance " + baseName);
            doors += current.DoorCount;
            byte[] before = other.SnapshotBitmap();
            current.CloseAllDoors();
            Check(other.SnapshotBitmap().SequenceEqual(before), "instance isolation " + baseName);
            sources.TryGetValue(baseName + ".sbi", out var originalDoors);
            if (originalDoors != null)
                for (int i = 0; i < current.DoorCount; i++)
                    Check(current.TryDoorAction(originalDoors.Skip(4 + i * 56).Take(32).ToArray(), true),
                        "every original door opens " + baseName + "/" + i);
        }
        Check(doors == 31, "complete native door count");
        foreach (var (baseName, expected) in new[] {
            ("KDHoneying", "603dca29091db31c9434403dc06076ed90f3864e39c9d46dd528d1304e1cd8aa"),
            ("KDEnMaze", "2b3ed271c4bc7f490d023951399a5a30f9926d316836754db7223fde385e2893"),
            ("KDUnHall", "a95329209d6b0bd9024811f0c2923b8bcd1823636b3eed1a572c5480296623e9") })
        {
            Check(KingdomQuestMapCollisionSource.TryCreate(baseName, out var current), "golden source");
            var liveMap = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            Set(liveMap, "MapInfo", new MapInfo(126, baseName, baseName, 0, 0, 1, 200));
            Set(liveMap, "KingdomQuestCollision", current);
            Check(liveMap.HasCollision && KingdomQuestPineMapStartOwner.Instance.TryCloseAllDoors(liveMap),
                "real map start owner " + baseName);
            Check(Hash(current.SnapshotBitmap()) == expected, "source-backed closed bitmap " + baseName);
            Check(liveMap.CanWalk(100, 100) == current.CanWalk(100, 100), "Map movement reads instance bitmap");
            Set(liveMap, "MapInfo", new MapInfo(126, "wrong", "wrong", 0, 0, 1, 200));
            Check(!KingdomQuestPineMapStartOwner.Instance.TryCloseAllDoors(liveMap), "owner map identity gate");
        }
        Check(!KingdomQuestMapCollisionSource.TryCreate("Unknown", out _) &&
            !KingdomQuestPineMapStartOwner.Instance.TryCloseAllDoors(null), "unknown owner fails closed");
        Console.WriteLine("PASS: native collision/door bytes, all 23 maps/31 doors, instance isolation and real START door owner");
    }
}
