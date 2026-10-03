using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using NextGen.FiestaLib;
using NextGen.Zone.Data;
using NextGen.Zone.Game;

static class WaitLoginTests
{
    static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception("waitlogin: " + name);
    }

    static T Empty<T>()
    {
        var value = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        GC.SuppressFinalize(value);
        return value;
    }

    public static void Run()
    {
        var clock = new Clock { Tick = 100 };
        var presence = new Presence();
        var source = new KingdomQuestPineNativeWaitLogin(clock, presence);
        var token = new KingdomQuestPineTokenValue();
        token.TrySetAscii("untouched");
        int state = 0;
        Check(source.TryStep("Wait", token, ref state, out bool done) &&
            !done && state == 1 && token.Text == "untouched", "initialize and wait");
        clock.Tick = 2500;
        Check(source.TryStep("Wait", token, ref state, out done) && !done &&
            token.Text == "untouched", "equality does not expire or write token");
        clock.Tick++;
        Check(source.TryStep("Wait", token, ref state, out done) && done &&
            token.Text == "0", "strict deadline expires");
        state = 0;
        clock.Tick = 5000;
        Check(source.TryStep("Wait", token, ref state, out done) && !done,
            "reentry creates fresh deadline");
        clock.Tick = 7400;
        Check(source.TryStep("Wait", token, ref state, out done) && !done, "reentry equality");
        clock.Tick = 9000;
        presence.Present = true;
        Check(source.TryStep("Wait", token, ref state, out done) && done && token.Text == "1",
            "player wins after deadline");
        state = 0;
        Check(source.TryStep("Wait", token, ref state, out done) && done,
            "presence completes on initialization call");
        presence.Present = false;
        clock.Tick = uint.MaxValue - 10;
        state = 0;
        Check(source.TryStep("Wait", token, ref state, out done) && done && token.Text == "0",
            "native unsigned wrapped deadline expires immediately");
        state = 0;
        clock.Tick = uint.MaxValue - 2400;
        Check(source.TryStep("Wait", token, ref state, out done) && !done,
            "maximum unwrapped deadline");
        clock.Tick = 0;
        Check(source.TryStep("Wait", token, ref state, out done) && !done,
            "native comparison across tick wrap retained");
        var second = new KingdomQuestPineNativeWaitLogin(clock, presence);
        Check(!second.TryStep("Wait", token, ref state, out done) && !done,
            "cannot reuse another process stack's state");
        state = 0;
        presence.Available = false;
        Check(!second.TryStep("Wait", token, ref state, out done) && !done && state == 0,
            "unavailable map is not empty map");
        presence.Available = true;
        clock.Available = false;
        Check(!second.TryStep("Wait", token, ref state, out done) && state == 0,
            "clock failure does not initialize");

        var liveClock = new KingdomQuestPineLiveClock(uint.MaxValue - 49);
        liveClock.Advance(49); // 99 ms across timeGetTime wrap
        Check(liveClock.TryGetCurrentTick(out uint tick) && tick == 0, "sub-tick truncation");
        liveClock.Advance(50);
        Check(liveClock.TryGetCurrentTick(out tick) && tick == 1, "100 ms is one tick");
        liveClock.Advance(50);
        Check(liveClock.TryGetCurrentTick(out tick) && tick == 1, "same sample no drift");
        liveClock.Advance(239950);
        Check(liveClock.TryGetCurrentTick(out tick) && tick == 2400, "240 seconds = 2400 ticks");

        var map = Empty<Map>();
        var objects = new ConcurrentDictionary<ushort, MapObject>();
        typeof(Map).GetProperty("Objects").SetValue(map, objects);
        var livePresence = new KingdomQuestPineMapPlayerPresence(map);
        Check(livePresence.TryHasPlayer(out bool found) && !found, "empty live map");
        var npc = Empty<Npc>();
        npc.Map = map;
        objects[1] = npc;
        Check(livePresence.TryHasPlayer(out found) && !found, "NPC excluded");
        var player = Empty<ZoneCharacter>();
        player.Map = map;
        objects[2] = player;
        for (int mode = 0; mode < 256; mode++)
        {
            player.State = (PlayerState)mode;
            bool expected = mode is 1 or 2 or 4 or 6;
            Check(livePresence.TryHasPlayer(out found) && found == expected, "mode " + mode);
        }
        player.State = PlayerState.Normal;
        player.Map = Empty<Map>();
        Check(livePresence.TryHasPlayer(out found) && !found, "transferred player excluded");
        player.Map = map;
        objects.TryRemove(2, out _);
        Check(livePresence.TryHasPlayer(out found) && !found, "departure observed immediately");
        Check(!new KingdomQuestPineMapPlayerPresence(null).TryHasPlayer(out found),
            "missing map rejected");

        foreach (string script in new[] { "KQ/UnderHall", "KQ/UnderHall2" })
        {
            clock = new Clock { Tick = 50 };
            presence = new Presence();
            source = new KingdomQuestPineNativeWaitLogin(clock, presence);
            var context = new KingdomQuestPineUsedCommandContext(clock, null,
                new KingdomQuestPineLocalCommandState(), null, null, source);
            Check(KingdomQuestPineScriptSource.TryGet(script, out var document), "source " + script);
            Check(KingdomQuestPineControlRuntime.TryCreate(document, new RejectHost(),
                null, context, "main", out var runtime), "runtime " + script);
            for (int i = 0; i < 100 && presence.Reads == 0; i++) runtime.Step();
            Check(presence.Reads == 1 && runtime.Status == KingdomQuestPineRuntimeStatus.Running,
                "original main reached live waitlogin: " + script + " " + runtime.Fault);
            int frames = runtime.FrameCount;
            runtime.Step();
            Check(runtime.FrameCount == frames, "waiting keeps Pine frame");
            presence.Present = true;
            runtime.Step();
            Check(runtime.FrameCount == frames - 1 && runtime.Variables.TryFind("Wait", out var value)
                && value.Text == "1", "login pops exactly one frame and sets source variable");
        }
        Console.WriteLine("PASS: native waitlogin, clock units/wrap, live map modes, original UnderHall control flow");
    }

    sealed class Clock : IKingdomQuestPineNativeTickSource
    {
        public uint Tick;
        public bool Available = true;
        public bool TryGetCurrentTick(out uint tick) { tick = Tick; return Available; }
    }
    sealed class Presence : IKingdomQuestPinePlayerPresence
    {
        public bool Present;
        public bool Available = true;
        public int Reads;
        public bool TryHasPlayer(out bool present) { Reads++; present = Present; return Available; }
    }
    sealed class RejectHost : IKingdomQuestPineRuntimeHost
    {
        public bool TryEvaluateCondition(string e, out int v) { v = 0; return false; }
        public bool TryResolveIdentifier(string e, out string v) { v = null; return false; }
        public bool TryCalculateExpression(string e, KingdomQuestPineTokenValue v) => false;
        public bool TryStepCommand(string c, int l, ref int s, out bool done) { done = false; return false; }
    }
}
