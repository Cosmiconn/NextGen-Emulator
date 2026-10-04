using System.Text;
using NextGen.Zone.Data;

static class DoorBuildTests
{
    static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception("door build: " + message);
    }
    public static void Run()
    {
        KingdomQuestHoneyingDoorBuildNativePlan first = null;
        foreach (var (line, command, x, y, direction) in new[] {
            (12, "doorbuild Door1 \"KQ_SlimeGate\" 9860 6094  272 1000 \"Normal\".", 9860, 6094, 272),
            (13, "doorbuild Door2 \"KQ_SlimeGate\" 6692 3944    6 1000 \"Normal\".", 6692, 3944, 6),
            (14, "doorbuild Door3 \"KQ_SlimeGate\" 5894 6098   88 1000 \"Normal\".", 5894, 6098, 88) })
        {
            Check(KingdomQuestHoneyingExternalPlanBuilder.TryBuild("KQ/Honeying", line, command, out var site), "source site");
            Check(KingdomQuestHoneyingDoorBuildNativePlan.TryBuild(site, out var plan), "plan");
            first ??= plan;
            Check(plan.X == x && plan.Y == y && plan.DirectionDegrees == direction &&
                plan.DestinationIdentifier == "Door" + (line - 11), "source arguments");
            var variables = new KingdomQuestPineVariableStack();
            variables.TryPush(plan.DestinationIdentifier, out var old);
            old.TrySetAscii("old-shadowed");
            variables.TryPush(plan.DestinationIdentifier, out var current);
            current.TrySetAscii("stale-handle");
            var owner = new Owner { BeforeBuild = () => Check(current.Text == "stale-handle", "destination written after build") };
            Check(KingdomQuestPineDoorBuildRuntime.TryStep(plan, variables, owner, out bool done) && done,
                "successful build");
            Check(owner.Events.SequenceEqual(new[] { "allocate:7", "resolve:" + plan.DestinationIdentifier,
                "lookup:KQ_SlimeGate", "map", "build" }), "native success order");
            Check(current.Text == "20638" && old.Text == "old-shadowed", "decimal handle replaces newest destination only");
            var request = owner.Request;
            Check(request.Handle == 0x509E && request.MobId == 1091 && ReferenceEquals(request.Source, plan), "native request identity");
            var mapName = request.SnapshotMapName12();
            Check(Encoding.ASCII.GetString(mapName).TrimEnd('\0') == "KQAlias", "theater map alias retained");
            mapName[0] = 0; owner.MapName[0] = 0;
            Check(request.SnapshotMapName12()[0] == 'K', "map identity isolated from owner/caller mutation");
            byte[] wire = request.CreateInitialBriefWire();
            Check(wire.Length == 50 && wire[0] == 15 && wire[1] == 28, "native brief opcode/size");
            Check(BitConverter.ToUInt16(wire, 2) == 0x509E && BitConverter.ToUInt16(wire, 4) == 1091 &&
                BitConverter.ToInt32(wire, 6) == x && BitConverter.ToInt32(wire, 10) == y &&
                wire[14] == direction / 2 && wire[15] == 0 && wire[16..48].All(b => b == 0) &&
                BitConverter.ToUInt16(wire, 48) == 1000, "all original brief fields");
            if (line == 12)
                Check(Convert.ToHexString(wire) ==
                    "0F1C9E50430484260000CE17000088000000000000000000000000000000000000000000000000000000000000000000E803",
                    "full literal golden wire");
            wire[0] = 0;
            Check(request.CreateInitialBriefWire()[0] == 15, "wire snapshot isolation");
        }
        var stack = new KingdomQuestPineVariableStack();
        stack.TryPush("Door1", out var destination); destination.TrySetAscii("unchanged");
        foreach (var failure in new[] { "allocation", "destination", "index", "map", "marking" })
        {
            var owner = new Owner { Failure = failure };
            Check(KingdomQuestPineDoorBuildRuntime.TryStep(first, stack, owner, out bool done) && done,
                "native failure pops: " + failure);
            Check(destination.Text == "unchanged", "failed build preserves token: " + failure);
            string[] expected = failure switch {
                "allocation" => new[] { "allocate:7", "error:Allocation:0" },
                "destination" => new[] { "allocate:7", "resolve:Door1", "error:Destination:0", "free:20638:0:23" },
                "index" => new[] { "allocate:7", "resolve:Door1", "lookup:KQ_SlimeGate", "error:MobIndex:0", "free:20638:0:23" },
                "map" => new[] { "allocate:7", "resolve:Door1", "lookup:KQ_SlimeGate", "map", "error:MapName:0", "free:20638:0:23" },
                _ => new[] { "allocate:7", "resolve:Door1", "lookup:KQ_SlimeGate", "map", "build", "error:Marking:3", "free:20638:0:22", "free:20638:0:23" }
            };
            Check(owner.Events.SequenceEqual(expected), "failure cleanup order: " + failure);
        }
        var unavailable = new Owner { IsAvailable = false };
        Check(!KingdomQuestPineDoorBuildRuntime.TryStep(first, stack, unavailable, out bool completed) &&
            !completed && unavailable.Events.Count == 0, "missing owner has no allocation or fake completion");
        Check(!KingdomQuestPineDoorBuildRuntime.TryStep(first, stack, null, out completed) && !completed, "null owner");
        var malformed = new Owner { MapName = new byte[11] };
        Check(!KingdomQuestPineDoorBuildRuntime.TryStep(first, stack, malformed, out completed) && !completed &&
            malformed.Events.Last() == "free:20638:0:23" && !malformed.Events.Contains("build"), "invalid name width fails and frees allocation");
        var fallbackOwner = new Owner();
        Check(KingdomQuestPineDoorBuildRuntime.TryStep(first, new KingdomQuestPineVariableStack(),
            fallbackOwner, out completed) && completed && fallbackOwner.Fallback.Text == "20638", "native fallback token supplied by owner");
        foreach (var (degrees, expected) in new[] { (272,136), (6,3), (88,44), (-1,0), (-2,179),
            (-359,1), (-360,0), (-362,255), (720,104), (int.MinValue,180), (int.MaxValue,255) })
            Check(KingdomQuestPineDoorBrief.EncodeDirection(degrees) == expected, "native signed direction " + degrees);
        foreach (var (index, handle) in new[] { (0,0x509E), (999,0x5485), (1000,65535), (65535,65535) })
            Check(KingdomQuestPineDoorBrief.RemakeNativeHandle((ushort)index) == handle, "native handle range " + index);
        var name = Enumerable.Range(1,32).Select(i => (byte)i).ToArray();
        var update = KingdomQuestPineDoorBrief.CreateNativeWire(1, 2, -1, int.MinValue, -362, 1, name, 65537);
        Check(update[15] == 1 && update[16..48].SequenceEqual(name) && BitConverter.ToUInt16(update,48) == 1,
            "full action/name and native u16 scale truncation");
        Console.WriteLine("PASS: original Honeying door builds, native allocation/cleanup order, decimal handles and 50-byte brief wire");
    }
    sealed class Owner : IKingdomQuestPineDoorBuildOwner
    {
        public bool IsAvailable { get; set; } = true;
        public readonly List<string> Events = new();
        public string Failure;
        public Action BeforeBuild;
        public byte[] MapName = Encoding.ASCII.GetBytes("KQAlias\0\0\0\0\0");
        public KingdomQuestPineTokenValue Fallback = new();
        public KingdomQuestPineDoorBuildRequest Request;
        readonly object allocated = new();
        public object Allocate(byte type, out ushort handle)
        { Events.Add("allocate:" + type); handle = 0x509E; return Failure == "allocation" ? null : allocated; }
        public KingdomQuestPineTokenValue ResolveDestination(KingdomQuestPineVariableStack variables, string name)
        { Events.Add("resolve:" + name); return Failure == "destination" ? null : variables.TryFind(name, out var token) ? token : Fallback; }
        public ushort FindMobId(string index)
        { Events.Add("lookup:" + index); return Failure == "index" ? ushort.MaxValue : (ushort)1091; }
        public byte[] GetCurrentMapName12()
        { Events.Add("map"); return Failure == "map" ? null : MapName; }
        public int Build(object obj, KingdomQuestPineDoorBuildRequest request)
        { Check(ReferenceEquals(obj, allocated), "exact allocated object"); Events.Add("build"); BeforeBuild?.Invoke(); Request = request; return Failure == "marking" ? 3 : 0; }
        public bool Free(ushort handle, int when, int reason)
        { Events.Add($"free:{handle}:{when}:{reason}"); return false; } // native caller ignores it
        public void ReportFailure(KingdomQuestPineDoorBuildFailure failure, int nativeError)
        { Events.Add($"error:{failure}:{nativeError}"); }
    }
}
