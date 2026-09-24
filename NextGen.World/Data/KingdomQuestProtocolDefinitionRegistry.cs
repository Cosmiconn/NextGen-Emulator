using System;
using System.Collections.Generic;
using System.Linq;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Source-owned full native PROTO_KQ_INFO catalog (377 bytes per entry).
    ///
    /// Unlike KingdomQuestDefinitionRegistry, this keeps the server/session
    /// suffix required by the original World -> Zone KQ lifecycle.
    /// </summary>
    public static class KingdomQuestProtocolDefinitionRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<uint, KingdomQuestProtocolInfo> ByHandle =
            new Dictionary<uint, KingdomQuestProtocolInfo>();

        public static void Upsert(KingdomQuestProtocolInfo info)
        {
            if (info == null) throw new ArgumentNullException("info");
            KingdomQuestProtocolInfo copy = Clone(info);
            lock (Sync)
                ByHandle[copy.Handle] = copy;
        }

        public static bool TryGet(uint handle, out KingdomQuestProtocolInfo info)
        {
            lock (Sync)
            {
                KingdomQuestProtocolInfo current;
                if (!ByHandle.TryGetValue(handle, out current))
                {
                    info = null;
                    return false;
                }

                info = Clone(current);
                return true;
            }
        }

        public static bool Remove(uint handle)
        {
            lock (Sync)
                return ByHandle.Remove(handle);
        }

        public static IReadOnlyList<KingdomQuestProtocolInfo> Snapshot()
        {
            lock (Sync)
                return ByHandle.Values
                    .OrderBy(v => v.Handle)
                    .Select(Clone)
                    .ToList()
                    .AsReadOnly();
        }

        public static void Clear()
        {
            lock (Sync)
                ByHandle.Clear();
        }

        private static KingdomQuestProtocolInfo Clone(KingdomQuestProtocolInfo source)
        {
            if (source.StartTm == null || source.ScheduleTm == null)
                throw new ArgumentException("KQ native tm fields must be present.", "source");
            if (source.MapLink == null || source.MapLink.Length != 4 ||
                source.MapLink.Any(v => v == null))
                throw new ArgumentException("KQ PROTO_KQ_INFO requires four map links.", "source");
            if (source.TeamRegenXY == null || source.TeamRegenXY.Length != 2 ||
                source.TeamRegenXY.Any(v => v == null))
                throw new ArgumentException("KQ PROTO_KQ_INFO requires two team regen coordinates.", "source");

            var copy = new KingdomQuestProtocolInfo
            {
                Handle = source.Handle,
                Status = source.Status,
                NumOfJoiner = source.NumOfJoiner,
                ID = source.ID,
                Title = source.Title,
                LimitTime = source.LimitTime,
                StartTime = source.StartTime,
                StartTm = CloneTime(source.StartTm),
                StartWaitTime = source.StartWaitTime,
                MinLevel = source.MinLevel,
                MaxLevel = source.MaxLevel,
                MinPlayers = source.MinPlayers,
                MaxPlayers = source.MaxPlayers,
                PlayerRepeatMode = source.PlayerRepeatMode,
                PlayerRepeatCount = source.PlayerRepeatCount,
                PlayerRevivalMode = source.PlayerRevivalMode,
                PlayerRevivalCount = source.PlayerRevivalCount,
                DemandQuest = source.DemandQuest,
                DemandItem = source.DemandItem,
                DemandClass = source.DemandClass,
                DemandGender = source.DemandGender,
                NextStartMode = source.NextStartMode,
                NextStartDelayMin = source.NextStartDelayMin,
                RepeatMode = source.RepeatMode,
                RepeatCount = source.RepeatCount,
                RewardIndex = source.RewardIndex,
                DemandMobKill = source.DemandMobKill,
                ScheduleTime = source.ScheduleTime,
                ScheduleTm = CloneTime(source.ScheduleTm),
                RunCounter = source.RunCounter,
                ScriptLanguage = source.ScriptLanguage,
                ScriptInitValue = source.ScriptInitValue,
                IsTeamPvp = source.IsTeamPvp,
                MapLink = new KingdomQuestMapProtocolInfo[4],
                TeamRegenXY = new KingdomQuestXY[2],
            };

            for (int i = 0; i < 4; i++)
            {
                copy.MapLink[i] = new KingdomQuestMapProtocolInfo
                {
                    MapIndex = source.MapLink[i].MapIndex,
                    MapBase = source.MapLink[i].MapBase,
                    MapName = source.MapLink[i].MapName,
                    MapClear = source.MapLink[i].MapClear,
                };
            }

            for (int i = 0; i < 2; i++)
            {
                copy.TeamRegenXY[i] = new KingdomQuestXY
                {
                    X = source.TeamRegenXY[i].X,
                    Y = source.TeamRegenXY[i].Y,
                };
            }

            return copy;
        }

        private static KingdomQuestNativeTime CloneTime(KingdomQuestNativeTime source)
        {
            return new KingdomQuestNativeTime
            {
                Second = source.Second,
                Minute = source.Minute,
                Hour = source.Hour,
                Day = source.Day,
                MonthFromZero = source.MonthFromZero,
                YearFrom1900 = source.YearFrom1900,
                DayOfWeek = source.DayOfWeek,
                DayOfYearFromZero = source.DayOfYearFromZero,
                IsDaylightSaving = source.IsDaylightSaving,
            };
        }
    }
}
