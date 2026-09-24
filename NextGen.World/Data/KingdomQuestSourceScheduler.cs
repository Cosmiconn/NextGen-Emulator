using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Data;

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
}
