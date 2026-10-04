using System;
using System.Collections.Generic;
using NextGen.Util;
using NextGen.Zone.Game;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// START must execute MapDoorArray::mda_CloseAllDoor (0x0049E1E0),
    /// including collision mutation. An absent owner must not acknowledge it.
    /// </summary>
    public interface IKingdomQuestPineMapStartOwner
    {
        bool TryCloseAllDoors(Map map);
    }

    /// <summary>
    /// Live worker owner for KQ Pine films. ShineAxialFlag::so_Routine
    /// (0x00569E00) visits the film list every mainthread pass; 0x00508E40
    /// calls Movie::m_Routine -> Theater::t_Routine -> ps_Step once and
    /// removes a finished film. There is no one-second movie timer.
    /// </summary>
    public sealed class KingdomQuestZonePineFilmScheduler
    {
        public static KingdomQuestZonePineFilmScheduler Instance { get; } =
            new KingdomQuestZonePineFilmScheduler(unchecked((uint)Environment.TickCount));

        private sealed class Entry
        {
            public Map Map;
            public KingdomQuestScenarioStartPlan StartIdentity;
            public KingdomQuestZonePineFilmSession Session;
        }

        private readonly object sync = new object();
        private readonly List<Entry> films = new List<Entry>();
        private readonly KingdomQuestPineLiveClock clock;
        private bool stepping;

        public KingdomQuestZonePineFilmScheduler(uint initialMilliseconds)
        {
            clock = new KingdomQuestPineLiveClock(initialMilliseconds);
        }

        public int Count { get { lock (sync) return films.Count; } }

        // Production entry point: closes the bound map's actual collision
        // doors. Gameplay dependencies must still be supplied for this film.
        public bool TryStartStartedSession(Map map, uint handle,
            IKingdomQuestPineRuntimeHost host,
            KingdomQuestPineUsedExpressionContext expressionContext,
            KingdomQuestPineUsedCommandContext commandContext,
            out KingdomQuestZonePineFilmSession session)
        {
            return TryStartStartedSession(map, handle, KingdomQuestPineMapStartOwner.Instance,
                host, expressionContext, commandContext, out session);
        }

        public bool TryStartStartedSession(Map map, uint handle,
            IKingdomQuestPineMapStartOwner startOwner,
            IKingdomQuestPineRuntimeHost host,
            KingdomQuestPineUsedExpressionContext expressionContext,
            KingdomQuestPineUsedCommandContext commandContext,
            out KingdomQuestZonePineFilmSession session)
        {
            session = null;
            if (map == null || map.MapInfo == null || map.Objects == null ||
                startOwner == null || host == null || commandContext == null ||
                (commandContext.CurrentKingdomQuestHandle.HasValue &&
                 commandContext.CurrentKingdomQuestHandle.Value != handle))
                return false;

            lock (sync)
            {
                // Reject reentrant starts from a gameplay callback while
                // its old stack is stepping.
                if (stepping)
                    return false;
                KingdomQuestZoneRuntimeState state;
                KingdomQuestPineScriptDocument document;
                if (!KingdomQuestZoneRuntimeRegistry.TryGet(handle, out state) ||
                    state.State != KingdomQuestZoneLifecycleState.Started ||
                    state.MapID != map.MapID || state.MapInstance != map.InstanceID ||
                    state.ScenarioStartPlan == null ||
                    !KingdomQuestPineScriptSource.TryGet(
                        state.ScenarioStartPlan.ScriptLanguage, out document))
                    return false;

                // The native START order is observable: drop old film,
                // close the actual doors, then create the replacement film.
                films.RemoveAll(e => e.Session.Handle == handle || ReferenceEquals(e.Map, map));
                if (!startOwner.TryCloseAllDoors(map))
                    return false;

                var liveContext = new KingdomQuestPineUsedCommandContext(
                    clock, commandContext.RegenResolver, commandContext.Sink,
                    commandContext.UnderHallSink, handle,
                    new KingdomQuestPineNativeWaitLogin(clock,
                        new KingdomQuestPineMapPlayerPresence(map)),
                    commandContext.WaitInterruptSource, commandContext.InterruptRegistry,
                    commandContext.UnderHall2Sink, commandContext.KQHBatSink,
                    commandContext.HoneyingSink);
                if (!KingdomQuestZonePineFilmBridge.TryCreateStartedSession(handle,
                        host, expressionContext, liveContext, out session))
                    return false;

                KingdomQuestZoneRuntimeState current;
                if (!KingdomQuestZoneRuntimeRegistry.TryGet(handle, out current) ||
                    !ReferenceEquals(current.ScenarioStartPlan, state.ScenarioStartPlan))
                {
                    session = null;
                    return false;
                }
                films.Add(new Entry { Map = map, StartIdentity = state.ScenarioStartPlan,
                    Session = session });
                return true;
            }
        }

        public void Step(uint milliseconds)
        {
            lock (sync)
            {
                if (stepping)
                    return;
                stepping = true;
                try
                {
                    clock.Advance(milliseconds);
                    for (int i = 0; i < films.Count;)
                    {
                        Entry entry = films[i];
                        if (!KingdomQuestZoneRuntimeRegistry.IsCurrentScenario(
                                entry.Session.Handle, entry.Map.MapID, entry.Map.InstanceID,
                                entry.StartIdentity))
                        {
                            films.RemoveAt(i);
                            continue;
                        }

                        KingdomQuestPineRuntimeStatus status;
                        try { status = entry.Session.Step(); }
                        catch (Exception ex)
                        {
                            entry.Session.Runtime.FailOwner("Pine live owner failed: " + ex.Message);
                            status = KingdomQuestPineRuntimeStatus.Faulted;
                        }
                        if (status != KingdomQuestPineRuntimeStatus.Running)
                        {
                            // A failed dependency remains visible on the
                            // session; it is never reported as a KQ success.
                            films.RemoveAt(i);
                            if (status == KingdomQuestPineRuntimeStatus.Faulted)
                                Log.WriteLine(LogLevel.Error,
                                    "KQ Pine film {0} on map {1}/{2} stopped: {3}",
                                    entry.Session.Handle, entry.Map.MapID, entry.Map.InstanceID,
                                    entry.Session.Fault);
                            continue;
                        }
                        i++;
                    }
                }
                finally { stepping = false; }
            }
        }
    }
}
