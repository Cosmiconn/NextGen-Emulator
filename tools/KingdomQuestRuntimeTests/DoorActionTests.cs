using NextGen.Zone.Data;

static class DoorActionTests
{
    static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception("door action: " + name);
    }
    public static void Run()
    {
        var variables = new KingdomQuestPineVariableStack();
        for (int i = 1; i <= 3; i++)
            Check(variables.TryPush("Door" + i, out var token) &&
                token.TrySetAscii("opaque-door-" + i), "opaque source token");
        foreach (var (line, door, open) in new[] {
            (15, 1, false), (16, 2, false), (17, 3, false),
            (61, 1, true), (97, 2, true), (130, 3, true) })
        {
            string command = $"{(open ? "dooropen" : "doorclose")} Door{door} \"CloseGate0{door}\".";
            Check(KingdomQuestHoneyingExternalPlanBuilder.TryBuild("KQ/Honeying", line, command,
                out var source), "source site");
            Check(KingdomQuestHoneyingDoorNativePlan.TryBuild(source, variables, out var plan), "native plan");
            Check(plan.CanonicalLine == line && plan.ObjectIdentifier == "Door" + door &&
                plan.CollisionName == "CloseGate0" + door && plan.Action == (open ? 1 : 0), "exact action identity");
            variables.TryFind("Door" + door, out var token);
            Check(plan.SnapshotSourceObjectToken().SequenceEqual(token.SnapshotNativeBytes()), "opaque token retained");
            var name = plan.SnapshotCollisionName32();
            Check(name.Length == 32 && name[11..].All(b => b == 0), "native strncpy name padding");
            name[0] = 0;
            Check(plan.SnapshotCollisionName32()[0] == 'C', "name immutable");
            Check(!KingdomQuestHoneyingDoorNativePlan.TryBuild(source,
                new KingdomQuestPineVariableStack(), out _), "missing variable is unresolved");
            var expectedWire = new byte[] { 9, 108, 52, 18, (byte)(open ? 1 : 0) };
            Check(plan.CreateNativeWire(0x1234).SequenceEqual(expectedWire), "five native wire bytes");
            Check(plan.CreateNativeWire(0xFFFF).Skip(2).Take(2).SequenceEqual(new byte[] { 255, 255 }), "full wire handle");

            Check(KingdomQuestMapCollisionSource.TryCreate("KDHoneying", out var collision), "source collision");
            collision.CloseAllDoors();
            KingdomQuestMapCollisionSource.TryCreate("KDHoneying", out var expected);
            expected.CloseAllDoors();
            expected.TryDoorAction(plan.SnapshotCollisionName32(), open);
            byte[] before = collision.SnapshotBitmap();
            int writes = 0, sends = 0;
            var live = new KingdomQuestPineDoorActionOwner(collision, 0x1234,
                (action, nativeName) => {
                    Check(sends == 0 && collision.SnapshotBitmap().SequenceEqual(before), "state precedes collision/send");
                    Check(action == plan.Action && nativeName.SequenceEqual(plan.SnapshotCollisionName32()), "object state/name");
                    nativeName[0] = 0; // callbacks cannot corrupt subsequent collision arguments
                    writes++;
                }, wire => {
                    Check(writes == 1 && collision.SnapshotBitmap().SequenceEqual(expected.SnapshotBitmap()), "collision precedes send");
                    Check(wire.SequenceEqual(expectedWire), "actual emitted wire"); sends++;
                });
            var resolver = new Resolver { Object = live };
            Check(KingdomQuestPineDoorActionRuntime.TryStep(plan, resolver, out bool done) && done &&
                writes == 1 && sends == 1 && resolver.Errors == 0, "real source mutation and packet");
            Check(resolver.Token.SequenceEqual(plan.SnapshotSourceObjectToken()), "resolver receives complete token");
            resolver.Available = false;
            Check(!KingdomQuestPineDoorActionRuntime.TryStep(plan, resolver, out done) && !done && writes == 1,
                "unavailable resolver cannot report native no-op");
            resolver.Available = true; resolver.Object = null;
            Check(KingdomQuestPineDoorActionRuntime.TryStep(plan, resolver, out done) && done && resolver.Errors == 1,
                "native null object logs then completes");
            var wrong = new WrongObject(); resolver.Object = wrong;
            Check(KingdomQuestPineDoorActionRuntime.TryStep(plan, resolver, out done) && done && resolver.Errors == 2 &&
                wrong.Calls == 0, "native non-door logs then completes without action");
            resolver.Object = new KingdomQuestPineDoorActionOwner(collision, 1, null, _ => sends++);
            Check(!KingdomQuestPineDoorActionRuntime.TryStep(plan, resolver, out done) && !done && sends == 1,
                "missing execution dependency fails before side effects");

            // This original map has no SBI. Native so_door_DoorAction still
            // updates the object and broadcasts even though lookup returns 0.
            KingdomQuestMapCollisionSource.TryCreate("KDUnHall", out var noDoors);
            byte[] unchanged = noDoors.SnapshotBitmap();
            int phase = 0;
            resolver.Object = new KingdomQuestPineDoorActionOwner(noDoors, 0x1234,
                (_, _) => { Check(phase == 0, "missing name state first"); phase = 1; },
                wire => { Check(phase == 1 && wire.SequenceEqual(expectedWire), "missing name still sends"); phase = 2; });
            Check(KingdomQuestPineDoorActionRuntime.TryStep(plan, resolver, out done) && done && phase == 2 &&
                noDoors.SnapshotBitmap().SequenceEqual(unchanged), "native collision miss ignored");
        }
        Console.WriteLine("PASS: all six Honeying door actions, native no-op boundaries, real collision-before-wire order");
    }
    sealed class Resolver : IKingdomQuestPineDoorObjectResolver
    {
        public bool Available = true;
        public IKingdomQuestPineDoorObject Object;
        public byte[] Token;
        public int Errors;
        public bool TryResolve(byte[] token, out IKingdomQuestPineDoorObject obj)
        { Token = token; obj = Object; return Available; }
        public void ReportInvalidDoorObject() => Errors++;
    }
    sealed class WrongObject : IKingdomQuestPineDoorObject
    {
        public byte NativeObjectType => 2;
        public int Calls;
        public bool TryApplyDoorAction(KingdomQuestHoneyingDoorNativePlan plan) { Calls++; return true; }
    }
}
