using System;
using System.Collections.Generic;
using System.Linq;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Thread-safe source-owned catalog of client-visible KQ definitions.
    ///
    /// This registry does not allocate handles, calculate schedules, choose
    /// maps, or change status. A source-backed scheduler supplies complete
    /// native PROTO_KQ_INFO_CLIENT values and may replace them as state changes.
    /// </summary>
    public static class KingdomQuestDefinitionRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<uint, KingdomQuestClientInfo> ByHandle =
            new Dictionary<uint, KingdomQuestClientInfo>();

        public static void Upsert(KingdomQuestClientInfo info)
        {
            if (info == null) throw new ArgumentNullException("info");
            lock (Sync)
                ByHandle[info.Handle] = Clone(info);
        }

        public static bool TryGet(uint handle, out KingdomQuestClientInfo info)
        {
            lock (Sync)
            {
                KingdomQuestClientInfo current;
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

        public static IReadOnlyList<KingdomQuestClientInfo> Snapshot()
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

        private static KingdomQuestClientInfo Clone(KingdomQuestClientInfo source)
        {
            return new KingdomQuestClientInfo
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
            };
        }

        private static KingdomQuestNativeTime CloneTime(KingdomQuestNativeTime source)
        {
            if (source == null) return new KingdomQuestNativeTime();
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
