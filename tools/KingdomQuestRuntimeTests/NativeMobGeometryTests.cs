using System.Text.Json;
using System.Buffers.Binary;
using System.Security.Cryptography;
using NextGen.FiestaLib.Data;
using NextGen.Zone.Data;

static class NativeMobGeometryTests
{
    static void Check(bool ok, string name) { if (!ok) throw new Exception("native mob geometry: " + name); }
    public static void Run()
    {
        OriginalX86Vectors(); OriginalStartupTable(); UnresolvedAndTableBoundaries();
        Console.WriteLine("PASS: 18 original-x86 mob geometry vectors, CRT/ring consumption, signed wrap/division faults, original startup table in eight FP paths, table lookup boundaries and unresolved-state ordering");
    }
    static void OriginalX86Vectors()
    {
        using var file = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "NativeMobGeometry.json")));
        var root = file.RootElement;
        Check(root.GetProperty("sourceSha256").GetString() == "db1cb42912556a4ea5cde5c18f15f2495b81465c70ca9c18ad5bc7e36611aff5", "original EXE identity");
        var table = new KingdomQuestPineNativeMobAngleTable(root.GetProperty("angleTable").EnumerateArray().Select(x => x.GetInt32()).ToArray());
        int cases = 0;
        foreach (var c in root.GetProperty("cases").EnumerateArray())
        {
            cases++; string name = c.GetProperty("name").GetString();
            int I(string key) => c.GetProperty(key).GetInt32();
            var rings = new[] { "selector", "radial", "angular" }.Select(key => new KingdomQuestPineNativeMobRandomWords(
                c.GetProperty(key).EnumerateArray().Select(x => x.GetUInt16()).ToArray(), 0, 3)).ToArray();
            var random = new MsvcCrtRand(c.GetProperty("crtSeed").GetUInt32());
            int assertions = 0;
            var geometry = new KingdomQuestPineNativeMobGeometry(I("centerX"), I("centerY"), c.GetProperty("regionHeight").GetUInt32(),
                I("halfWidth"), I("halfHeight"), I("rotationFirst"), I("rotationSecond"), I("radius"), I("direction"),
                random, rings[0], rings[1], rings[2], table, () => assertions++);
            bool available = geometry.TryRegenLocation(out int x, out int y, out int direction, out byte result);
            var expected = c.GetProperty("expected");
            Check(available == expected.GetProperty("resolved").GetBoolean() && direction == expected.GetProperty("direction").GetInt32() &&
                result == expected.GetProperty("nativeResult").GetByte(), name + " native outcome/direction");
            if (available) Check(x == expected.GetProperty("x").GetInt32() && y == expected.GetProperty("y").GetInt32(), name + " original XY");
            Check(assertions == expected.GetProperty("assertions").GetInt32() && random.State == expected.GetProperty("crtState").GetUInt32() &&
                rings.Select(r => r.Index).SequenceEqual(expected.GetProperty("indices").EnumerateArray().Select(v => v.GetUInt32())), name + " original state transitions");
        }
        Check(cases == 18, "complete original-x86 vector set");
    }
    static void OriginalStartupTable()
    {
        using var file = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "NativeMobGeometry.json")));
        var fixture = file.RootElement;
        const string expectedHash = "50aca2c9f617c45ebfdde052be10ff95095edb93fb79ad519aed77c2e7b28bad";
        Check(fixture.GetProperty("startupSha256").GetString() == expectedHash, "recorded original constructor fingerprint");
        var expected = fixture.GetProperty("originalStartupTable").EnumerateArray().Select(v => v.GetInt32()).ToArray();
        Check(expected.Length == 360, "complete recovered table");
        var profiles = new HashSet<(int, int, ushort, uint)>();
        foreach (var profile in fixture.GetProperty("startupProfiles").EnumerateArray())
        {
            ushort cw = profile.GetProperty("x87ControlWord").GetUInt16(); uint mxcsr = profile.GetProperty("mxcsr").GetUInt32();
            profiles.Add((profile.GetProperty("mathSse").GetInt32(), profile.GetProperty("conversionSse").GetInt32(), cw, mxcsr));
            Check(KingdomQuestPineNativeMobAngleTable.TryCreateOriginalStartup(cw, mxcsr, out var table), "verified native FP profile");
            var bytes = new byte[1440];
            for (int i = 0; i < 180; i++)
            {
                Check(table.TryFirst(i, out int first) && first == expected[i], "original first half " + i);
                Check(table.TrySecond(i, out int second) && second == expected[i + 180], "original second half " + i);
                BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * 4), first);
                BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan((i + 180) * 4), second);
            }
            Check(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() == expectedHash, "all 1440 native bytes");
            Check(table.TryFirst(45, out int peak) && peak == 1023 && table.TrySecond(90, out peak) && peak == -1023,
                "native approximate-pi/truncation peaks are not normalized to +/-1024");
        }
        var expectedProfiles = from m in new[] { 0, 1 } from c in new[] { 0, 1 } from cw in new ushort[] { 0x027F, 0x037F }
            select (m, c, cw, 0x1F80u);
        Check(profiles.SetEquals(expectedProfiles), "all eight independent CRT feature/control paths recorded");
        foreach (var cw in new ushort[] { 0, 0x007F, 0x067F, 0x0A7F, 0x0E7F, 0xFFFF })
            Check(!KingdomQuestPineNativeMobAngleTable.TryCreateOriginalStartup(cw, 0x1F80, out var unknown) && unknown == null,
                "unverified native x87 state stays unresolved");
        Check(!KingdomQuestPineNativeMobAngleTable.TryCreateOriginalStartup(0x027F, 0x1F81, out _) &&
            !KingdomQuestPineNativeMobAngleTable.TryCreateOriginalStartup(0x037F, 0x9F80, out _), "MXCSR is not silently normalized");
    }
    static void UnresolvedAndTableBoundaries()
    {
        var values = Enumerable.Range(0, 360).ToArray(); var table = new KingdomQuestPineNativeMobAngleTable(values);
        values[0] = 99;
        Check(table.TryFirst(0, out int value) && value == 0 && table.TryFirst(180, out value) && value == 0 &&
            table.TrySecond(360, out value) && value == 180, "owned table copy and native modulo 180");
        Check(!table.TryFirst(-1, out _) && table.TrySecond(-1, out value) && value == 179,
            "signed negative remainder can address first half from second lookup");
        var selector = new KingdomQuestPineNativeMobRandomWords(new ushort[] { 0, 3 }, 0, 1);
        var radial = new KingdomQuestPineNativeMobRandomWords(new ushort[] { 0, 7 }, 0, 1);
        var angular = new KingdomQuestPineNativeMobRandomWords(new ushort[] { 0, 33 }, 0, 1);
        var geometry = new KingdomQuestPineNativeMobGeometry(1, 2, 0, 3, 4, 5, 6, 7, 123,
            null, selector, radial, angular, table, null);
        Check(!geometry.TryRegenLocation(out _, out _, out int direction, out _) && direction == 123 && selector.Index == 1 && radial.Index == 0 && angular.Index == 0,
            "invalid selector requires native output pointer; stop unresolved after selector consumption");
        selector = new(new ushort[] { 0, 1 }, 0, 1);
        geometry = new(1, 2, 0, 3, 4, 5, 6, 0, 123, null, selector, radial, angular, null, null);
        Check(!geometry.TryRegenLocation(out _, out _, out _, out _) && selector.Index == 1 && radial.Index == 1 && angular.Index == 1,
            "missing table unresolved after both rings, even radius zero");
        var random = new MsvcCrtRand(123);
        geometry = new(1, 2, 1, -1, 0, 1024, 0, 7, 123, random, null, null, null, null, null);
        Check(!geometry.TryRegenLocation(out _, out _, out _, out _) && random.State == 123, "missing assertion binding doesn't invent success");
        geometry = new(1, 2, 1, -1, 0, 1024, 0, 7, 123, random, null, null, null, null, () => { throw new InvalidOperationException(); });
        try { geometry.TryRegenLocation(out _, out _, out _, out _); throw new Exception("expected assertion exception"); }
        catch (InvalidOperationException) { Check(random.State == 123, "exception before RNG"); }
        try
        {
            _ = new KingdomQuestPineNativeMobGeometry(0, 0, 0, 0, 0, 0, 0, 0, 0, null, selector, selector, angular, table, null);
            throw new Exception("expected distinct ring check");
        }
        catch (ArgumentException) { }
    }
}
