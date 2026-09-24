using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Data;
using NextGen.Util;

namespace NextGen.World.Data
{
    /// <summary>
    /// Pure source-to-schedule primitives recovered from the original NA2016
    /// WorldManager CKQServer::DoSchedule/GetNextScheduleTime/AddNewScheduleList
    /// paths. This class does not own handles, publish list entries or advance
    /// KQ gameplay states.
    /// </summary>
    public static class KingdomQuestSourceScheduler
    {
        public const int ScheduleWindowSize = 2;

        public static IReadOnlyList<DateTime> GetNextScheduleTimes(
            KingdomQuestSourceDefinition source, DateTime localNow)
        {
            if (source == null) throw new ArgumentNullException("source");

            DateTime currentMinute = NormalizeLocal(localNow);
            currentMinute = new DateTime(
                currentMinute.Year, currentMinute.Month, currentMinute.Day,
                currentMinute.Hour, currentMinute.Minute, 0,
                DateTimeKind.Local);

            // CKQServer::DoSchedule copies ST_Day/ST_Hour/ST_Minute into a tm,
            // but takes tm_mon/tm_year from the current local tm. ST_Year,
            // ST_Month and ST_Second are not read on this path.
            DateTime next = new DateTime(
                currentMinute.Year, currentMinute.Month, 1, 0, 0, 0,
                DateTimeKind.Local)
                .AddDays(source.ST_Day - 1)
                .AddHours(source.ST_Hour)
                .AddMinutes(source.ST_Minute);

            int stepMinutes = source.NextStartDeleyMin;
            if (stepMinutes == 0 && next < currentMinute)
                throw new InvalidOperationException(
                    "Original GetNextScheduleTime would not advance a past KQ with a zero minute step.");

            while (next < currentMinute)
                next = next.AddMinutes(stepMinutes);

            var result = new DateTime[ScheduleWindowSize];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = next;
                next = next.AddMinutes(stepMinutes);
            }
            return Array.AsReadOnly(result);
        }

        public static KingdomQuestProtocolInfo CreateScheduledDefinition(
            KingdomQuestSourceDefinition source,
            uint handle,
            DateTime scheduledLocalTime,
            IReadOnlyDictionary<uint, long> demandClassMasks,
            KingdomQuestTeamInfo team)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (demandClassMasks == null) throw new ArgumentNullException("demandClassMasks");

            long demandClass;
            if (!demandClassMasks.TryGetValue(source.UseClass, out demandClass))
                throw new InvalidOperationException(
                    "No source-backed UseClassTypeInfo row for KQ UseClass " + source.UseClass + ".");

            DateTime local = NormalizeLocal(scheduledLocalTime);
            int time32 = ToNativeTime32(local);

            var result = new KingdomQuestProtocolInfo
            {
                Handle = handle,
                Status = 0,
                NumOfJoiner = 0,
                StartTime = time32,
                StartTm = KingdomQuestNativeTime.FromLocalDateTime(local),
                ScheduleTime = time32,
                ScheduleTm = KingdomQuestNativeTime.FromLocalDateTime(local),
                DemandClass = demandClass,
            };

            for (int i = 0; i < result.MapLink.Length; i++)
                result.MapLink[i] = new KingdomQuestMapProtocolInfo();
            for (int i = 0; i < result.TeamRegenXY.Length; i++)
                result.TeamRegenXY[i] = new KingdomQuestXY();

            KingdomQuestSourceProjection.ApplyProvenStaticFields(source, result);

            // CKQServer::GetKQTeamData is called with KINGDOM_QUEST::ID.
            // AddNewScheduleList copies only IsTeamPVP and the two regen XYs;
            // if no KQTeam row exists it writes zeroes.
            if (team != null)
            {
                if (team.ID != result.ID)
                    throw new InvalidOperationException(
                        "KQTeam source row does not match KINGDOM_QUEST ID.");

                result.IsTeamPvp = team.IsTeamPvp ? (byte)1 : (byte)0;
                result.TeamRegenXY[0].X = team.RegenXRed;
                result.TeamRegenXY[0].Y = team.RegenYRed;
                result.TeamRegenXY[1].X = team.RegenXBlue;
                result.TeamRegenXY[1].Y = team.RegenYBlue;
            }

            return result;
        }

        private static DateTime NormalizeLocal(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
                return value.ToLocalTime();
            if (value.Kind == DateTimeKind.Unspecified)
                return DateTime.SpecifyKind(value, DateTimeKind.Local);
            return value;
        }

        private static int ToNativeTime32(DateTime local)
        {
            long seconds = new DateTimeOffset(local).ToUnixTimeSeconds();
            if (seconds < int.MinValue || seconds > int.MaxValue)
                throw new ArgumentOutOfRangeException("local",
                    "KQ native scheduler uses signed 32-bit time_t.");
            return (int)seconds;
        }
    }

    /// <summary>
    /// Atomically publishes a pre-map native scheduler entry to the World
    /// registries. This mirrors AddNewScheduleList ownership only: it does not
    /// allocate a map target or advance the KQ out of Status 0.
    /// </summary>
    public static class KingdomQuestScheduledDefinitionCoordinator
    {
        private static readonly object Sync = new object();

        public static bool TryPublish(KingdomQuestProtocolInfo definition)
        {
            if (definition == null ||
                definition.Status != KingdomQuestNativeConstants.StatusScheduled ||
                definition.NumOfJoiner != 0)
                return false;

            lock (Sync)
            {
                KingdomQuestProtocolInfo protocolExisting;
                KingdomQuestClientInfo clientExisting;
                KingdomQuestInstanceWireState stateExisting;
                if (KingdomQuestProtocolDefinitionRegistry.TryGet(
                        definition.Handle, out protocolExisting) ||
                    KingdomQuestDefinitionRegistry.TryGet(
                        definition.Handle, out clientExisting) ||
                    KingdomQuestInstanceRegistry.TryGet(
                        definition.Handle, out stateExisting))
                    return false;

                try
                {
                    KingdomQuestProtocolDefinitionRegistry.Upsert(definition);
                    KingdomQuestDefinitionRegistry.Upsert(definition);
                    KingdomQuestInstanceRegistry.Upsert(
                        definition.Handle,
                        definition.Status,
                        definition.ID,
                        definition.MinLevel,
                        definition.MaxLevel);
                    KingdomQuestParticipantRegistry.Set(
                        definition.Handle,
                        new KingdomQuestJoinCharacterInfo[0]);
                    return true;
                }
                catch
                {
                    KingdomQuestProtocolDefinitionRegistry.Remove(definition.Handle);
                    KingdomQuestDefinitionRegistry.Remove(definition.Handle);
                    KingdomQuestInstanceRegistry.Remove(definition.Handle);
                    KingdomQuestParticipantRegistry.Remove(definition.Handle);
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Live owner for the exact WorldManager DoSchedule/AddNewScheduleList
    /// behavior that is already recovered from the supplied NA2016 binary.
    ///
    /// It owns only the scheduler array and its monotonically increasing
    /// process-local Handle counter. Map allocation and later status changes
    /// remain separate native lifecycle work.
    /// </summary>
    [ServerModule(InitializationStage.Worker)]
    public sealed class KingdomQuestScheduleRuntime
    {
        public const int NativeScheduleCapacity = 300;
        private const double TickMilliseconds = 1000.0;

        private static KingdomQuestScheduleRuntime Instance;
        private readonly object sync = new object();
        private readonly System.Timers.Timer timer;
        private uint nextHandle;
        private DateTime? lastScheduleMinute;
        private bool enabled;

        private KingdomQuestScheduleRuntime()
        {
            // CKQServer::CKQServer initializes the native next-Handle member
            // to zero. It is incremented only after AddNewScheduleList
            // successfully appends a new (ID, ScheduleTime) entry.
            nextHandle = 0;
            timer = new System.Timers.Timer(TickMilliseconds);
            timer.AutoReset = true;
            timer.Elapsed += OnElapsed;
        }

        [InitializerMethod]
        public static bool Load()
        {
            Instance = new KingdomQuestScheduleRuntime();
            return Instance.Start();
        }

        private bool Start()
        {
            DataProvider provider = DataProvider.Instance;
            if (provider == null)
                return false;

            if (!provider.HasCompleteKingdomQuestMainSource ||
                !provider.HasKingdomQuestUseClassSource)
            {
                Log.WriteLine(LogLevel.Warn,
                    "KQ scheduler disabled: exact main/UseClass source corpus is incomplete.");
                return true;
            }

            if (KingdomQuestProtocolDefinitionRegistry.Snapshot().Count != 0 ||
                KingdomQuestDefinitionRegistry.Snapshot().Count != 0 ||
                KingdomQuestInstanceRegistry.Snapshot().Count != 0)
            {
                Log.WriteLine(LogLevel.Error,
                    "KQ scheduler requires empty runtime registries so the native Handle counter can start at zero.");
                return false;
            }

            enabled = true;
            RunSchedule(DateTime.Now);
            timer.Start();
            Log.WriteLine(LogLevel.Info,
                "KQ native scheduler enabled from exact source rows; next Handle={0}.",
                nextHandle);
            return true;
        }

        private void OnElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                RunSchedule(DateTime.Now);
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception,
                    "KQ scheduler tick failed: {0}", ex);
            }
        }

        internal void RunSchedule(DateTime localNow)
        {
            if (!enabled)
                return;

            DateTime local = localNow.Kind == DateTimeKind.Utc
                ? localNow.ToLocalTime()
                : (localNow.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(localNow, DateTimeKind.Local)
                    : localNow);
            DateTime minute = new DateTime(
                local.Year, local.Month, local.Day,
                local.Hour, local.Minute, 0, DateTimeKind.Local);

            lock (sync)
            {
                if (lastScheduleMinute.HasValue &&
                    lastScheduleMinute.Value == minute)
                    return;

                PublishScheduleWindow(local);
                lastScheduleMinute = minute;
            }
        }

        private void PublishScheduleWindow(DateTime localNow)
        {
            DataProvider provider = DataProvider.Instance;
            IReadOnlyList<KingdomQuestProtocolInfo> existing =
                KingdomQuestProtocolDefinitionRegistry.Snapshot();

            if (existing.Count > NativeScheduleCapacity)
                throw new InvalidOperationException(
                    "KQ scheduler registry exceeds the native 300-entry capacity.");

            var keys = new HashSet<Tuple<ushort, int>>();
            for (int i = 0; i < existing.Count; i++)
                keys.Add(Tuple.Create(existing[i].ID, existing[i].ScheduleTime));

            int count = existing.Count;
            for (int sourceIndex = 0;
                sourceIndex < provider.KingdomQuestSourceDefinitions.Count;
                sourceIndex++)
            {
                KingdomQuestSourceDefinition source =
                    provider.KingdomQuestSourceDefinitions[sourceIndex];
                IReadOnlyList<DateTime> times =
                    KingdomQuestSourceScheduler.GetNextScheduleTimes(source, localNow);

                for (int timeIndex = 0; timeIndex < times.Count; timeIndex++)
                {
                    if (count >= NativeScheduleCapacity)
                        return;

                    KingdomQuestTeamInfo team = null;
                    if (source.ID >= 0)
                        provider.KingdomQuestTeams.TryGetValue(
                            (ushort)source.ID, out team);

                    KingdomQuestProtocolInfo definition =
                        KingdomQuestSourceScheduler.CreateScheduledDefinition(
                            source,
                            nextHandle,
                            times[timeIndex],
                            provider.KingdomQuestDemandClassMasks,
                            team);

                    Tuple<ushort, int> key =
                        Tuple.Create(definition.ID, definition.ScheduleTime);
                    if (keys.Contains(key))
                        continue;

                    if (!KingdomQuestScheduledDefinitionCoordinator.TryPublish(
                            definition))
                        throw new InvalidOperationException(
                            "KQ scheduler could not publish native Handle " +
                            nextHandle + ".");

                    keys.Add(key);
                    count++;
                    nextHandle = unchecked(nextHandle + 1);
                }
            }
        }

        [CleanUpMethod]
        public static void CleanUp()
        {
            KingdomQuestScheduleRuntime current = Instance;
            Instance = null;
            if (current == null)
                return;

            current.enabled = false;
            current.timer.Stop();
            current.timer.Dispose();
        }
    }

}
