using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.Text;
using NextGen.FiestaLib.Data;
using NextGen.Zone.Data;

static void Check(bool value, string name)
{
    if (!value) throw new Exception(name);
}

// Build only source lookup dependencies; no database/server initialization.
static T Empty<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
static void Set(object target, string property, object value) =>
    target.GetType().GetProperty(property).SetValue(target, value);

KingdomQuestHoneyingExternalPlan Site(int line, string command)
{
    Check(KingdomQuestHoneyingExternalPlanBuilder.TryBuild(
        "KQ/Honeying", line, command, out var plan), "source site " + line);
    return plan;
}

Check(KingdomQuestPineScriptFile.TryGetSource("KQHoneying", out var source), "source");
var fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "KQHoneying.txt"));
Check(Convert.ToHexString(SHA256.HashData(fixture)).ToLowerInvariant() ==
    "3438e2d0144619b52fd4f66d7ea3f9b6384a0d67c76dbf712641b61d6bfdcc30", "original script SHA256");
int recordCount = 0;
foreach (string line in Encoding.Latin1.GetString(fixture).Split('\n'))
{
    if (!line.StartsWith("#Record\t", StringComparison.Ordinal)) continue;
    var fields = line.TrimEnd('\r').Split('\t');
    Check(KingdomQuestHoneyingTextSource.TryResolve(source, fields[1], out var actual,
        out bool found) && found && actual == fields[2], "original record " + fields[1]);
    recordCount++;
}
Check(recordCount == 14, "complete original Script table");
Check(KingdomQuestHoneyingTextSource.TryResolve(source, "Honeying01", out var text,
    out var present) && present && text == "Where do you think you are!", "dialog 1");
Check(KingdomQuestHoneyingTextSource.TryResolve(source, "Honeying02", out text,
    out present) && present && text == "You want to get stung!!?", "dialog 2");
Check(KingdomQuestHoneyingTextSource.TryResolve(source, "KQ_H_GHoneyingDead", out text,
    out present) && !present && text == "", "native absent-record empty result");
Check(!KingdomQuestHoneyingTextSource.TryResolve(null, "Honeying01", out text,
    out present) && text == null, "unknown source is unresolved");
KingdomQuestPineScriptFile.TryGetSource("KQHBat1", out var otherSource);
Check(!KingdomQuestHoneyingTextSource.TryResolve(otherSource, "Honeying01", out _,
    out _), "wrong script cannot borrow Honeying text");

foreach (var (line, seconds) in new[] { (183, 30), (185, 20), (187, 10), (189, 5),
    (197, 30), (199, 20), (201, 10), (203, 5) })
{
    var site = Site(line, $"broadcast all \"KQReturn{seconds}\".");
    Check(KingdomQuestHoneyingCommonNative.TryBuildBroadcast(site, out var plan) &&
        plan.MessageTextResolved && plan.MessageText == $"Move to Elderine in {seconds} seconds.",
        "return notice " + line);
}

var data = Empty<DataProvider>();
var mob = Empty<MobInfo>();
Set(mob, "ID", (ushort)1129);
var serverMob = Empty<MobInfoServer>();
Set(serverMob, "ID", (uint)1129);
Set(data, "MobsByName", new Dictionary<string, MobInfo> { ["KQ_H_GHoneying"] = mob });
Set(data, "MobData", new Dictionary<string, MobInfoServer> { ["KQ_H_GHoneying"] = serverMob });
foreach (var (line, key, message) in new[] {
    (140, "Honeying01", "Where do you think you are!"),
    (142, "Honeying02", "You want to get stung!!?") })
{
    var site = Site(line, $"chatwin \"KQ_H_GHoneying\" \"{key}\".");
    Check(KingdomQuestHoneyingCommonNative.TryBuildChatWindow(site, data, out var plan), "chat");
    Check(plan.NativeLookup.MessageTextResolved && plan.NativeLookup.RecordPresent, "resolved chat");
    Check(plan.NativeLookup.TryCreateNativeWire(out var wire), "chat wire");
    var expected = new byte[] { 0x0C, 0x6C, 0x69, 0x04, (byte)message.Length }
        .Concat(Encoding.ASCII.GetBytes(message)).ToArray();
    Check(wire.SequenceEqual(expected), "byte-exact native chat envelope");
    wire[0] = 0;
    Check(plan.NativeLookup.TryCreateNativeWire(out var again) && again.SequenceEqual(expected),
        "wire snapshot isolation");
}
Set(serverMob, "ID", (uint)1130);
Check(!KingdomQuestHoneyingCommonNative.TryBuildChatWindow(
    Site(140, "chatwin \"KQ_H_GHoneying\" \"Honeying01\"."), data, out _), "NPC mismatch");

var variables = new KingdomQuestPineVariableStack();
Check(variables.TryPush("Boss", out var boss) && boss.TrySetAscii("opaque-native-handle"), "Boss");
foreach (int line in new[] { 160, 164, 168, 172, 177 })
{
    bool missing = line == 177;
    string key = missing ? "KQ_H_GHoneyingDead" : "Summon01";
    var site = Site(line, $"npcshout Boss \"{key}\".");
    Check(KingdomQuestHoneyingNpcShoutNativePlan.TryBuild(site, variables, out var plan), "shout");
    Check(plan.RecordPresent == !missing && plan.MessageText ==
        (missing ? "" : "Friends! Attack those worthless people!!"), "shout text");
    var snapshot = plan.SnapshotSourceObjectToken();
    Check(snapshot.SequenceEqual(boss.SnapshotNativeBytes()), "opaque handle preservation");
    snapshot[0] = 0;
    Check(plan.SnapshotSourceObjectToken()[0] != 0, "immutable shout handle");
    Check(!KingdomQuestHoneyingNpcShoutNativePlan.TryBuild(site,
        new KingdomQuestPineVariableStack(), out _), "missing Boss fails closed");
}
Check(!KingdomQuestHoneyingExternalPlanBuilder.TryBuild("KQ/Honeying", 177,
    "npcshout Boss \"InventedDeathLine\".", out _), "changed source rejected");
int state = 0;
Check(!new KingdomQuestHoneyingCommandState().TryStep(
    Site(160, "npcshout Boss \"Summon01\"."), variables, 123, ref state, out bool completed) &&
    !completed && state == 0, "missing live owner never pretends to complete");

Console.WriteLine("PASS: Honeying source text, native missing record, chat wire, shout plans and fail-closed boundaries");
WaitLoginTests.Run();
FilmSchedulerTests.Run();
CollisionTests.Run();
DoorActionTests.Run();
DoorBuildTests.Run();
EffectTests.Run();
EffectRoutineTests.Run();
