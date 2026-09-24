using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Networking;

namespace NextGen.FiestaLib.Data
{
    /// <summary>
    /// Native C tm wire representation used by the KQ protocol.
    /// Exactly nine signed 32-bit fields / 36 bytes.
    /// </summary>
    public sealed class KingdomQuestNativeTime
    {
        public int Second { get; set; }
        public int Minute { get; set; }
        public int Hour { get; set; }
        public int Day { get; set; }
        public int MonthFromZero { get; set; }
        public int YearFrom1900 { get; set; }
        public int DayOfWeek { get; set; }
        public int DayOfYearFromZero { get; set; }
        public int IsDaylightSaving { get; set; }

        public static KingdomQuestNativeTime FromLocalDateTime(DateTime local)
        {
            return new KingdomQuestNativeTime
            {
                Second = local.Second,
                Minute = local.Minute,
                Hour = local.Hour,
                Day = local.Day,
                MonthFromZero = local.Month - 1,
                YearFrom1900 = local.Year - 1900,
                DayOfWeek = (int)local.DayOfWeek,
                DayOfYearFromZero = local.DayOfYear - 1,
                IsDaylightSaving = TimeZoneInfo.Local.IsDaylightSavingTime(local) ? 1 : 0,
            };
        }

        public void Write(Packet packet)
        {
            packet.WriteInt(Second);
            packet.WriteInt(Minute);
            packet.WriteInt(Hour);
            packet.WriteInt(Day);
            packet.WriteInt(MonthFromZero);
            packet.WriteInt(YearFrom1900);
            packet.WriteInt(DayOfWeek);
            packet.WriteInt(DayOfYearFromZero);
            packet.WriteInt(IsDaylightSaving);
        }

        public static bool TryRead(Packet packet, out KingdomQuestNativeTime value)
        {
            value = null;
            if (packet == null) return false;

            int second, minute, hour, day, month, year, weekday, yearday, daylight;
            if (!packet.TryReadInt(out second) ||
                !packet.TryReadInt(out minute) ||
                !packet.TryReadInt(out hour) ||
                !packet.TryReadInt(out day) ||
                !packet.TryReadInt(out month) ||
                !packet.TryReadInt(out year) ||
                !packet.TryReadInt(out weekday) ||
                !packet.TryReadInt(out yearday) ||
                !packet.TryReadInt(out daylight))
                return false;

            value = new KingdomQuestNativeTime
            {
                Second = second,
                Minute = minute,
                Hour = hour,
                Day = day,
                MonthFromZero = month,
                YearFrom1900 = year,
                DayOfWeek = weekday,
                DayOfYearFromZero = yearday,
                IsDaylightSaving = daylight,
            };
            return true;
        }
    }

    /// <summary>
    /// Native PROTO_KQ_INFO_CLIENT. Wire size is exactly 141 bytes.
    /// This class models protocol fields only; it does not assign scheduler
    /// semantics or source-table columns beyond the original field names.
    /// </summary>
    public class KingdomQuestClientInfo
    {
        public const int WireSize = 141;

        public uint Handle { get; set; }
        public byte Status { get; set; }
        public ushort NumOfJoiner { get; set; }
        public ushort ID { get; set; }
        public string Title { get; set; } = string.Empty;
        public ushort LimitTime { get; set; }
        public int StartTime { get; set; }
        public KingdomQuestNativeTime StartTm { get; set; } = new KingdomQuestNativeTime();
        public ushort StartWaitTime { get; set; }
        public byte MinLevel { get; set; }
        public byte MaxLevel { get; set; }
        public ushort MinPlayers { get; set; }
        public ushort MaxPlayers { get; set; }
        public byte PlayerRepeatMode { get; set; }
        public ushort PlayerRepeatCount { get; set; }
        public byte PlayerRevivalMode { get; set; }
        public byte PlayerRevivalCount { get; set; }
        public ushort DemandQuest { get; set; }
        public ushort DemandItem { get; set; }
        public long DemandClass { get; set; }
        public byte DemandGender { get; set; }

        public virtual void Write(Packet packet)
        {
            packet.WriteUInt(Handle);
            packet.WriteByte(Status);
            packet.WriteUShort(NumOfJoiner);
            packet.WriteUShort(ID);
            packet.WriteString(Title ?? string.Empty, 64);
            packet.WriteUShort(LimitTime);
            packet.WriteInt(StartTime);
            StartTm.Write(packet);
            packet.WriteUShort(StartWaitTime);
            packet.WriteByte(MinLevel);
            packet.WriteByte(MaxLevel);
            packet.WriteUShort(MinPlayers);
            packet.WriteUShort(MaxPlayers);
            packet.WriteByte(PlayerRepeatMode);
            packet.WriteUShort(PlayerRepeatCount);
            packet.WriteByte(PlayerRevivalMode);
            packet.WriteByte(PlayerRevivalCount);
            packet.WriteUShort(DemandQuest);
            packet.WriteUShort(DemandItem);
            packet.WriteLong(DemandClass);
            packet.WriteByte(DemandGender);
        }

        public static bool TryRead(Packet packet, out KingdomQuestClientInfo value)
        {
            value = null;
            if (packet == null || packet.Remaining < WireSize) return false;

            var result = new KingdomQuestClientInfo();
            if (!TryReadInto(packet, result))
                return false;
            value = result;
            return true;
        }

        internal static bool TryReadInto(Packet packet, KingdomQuestClientInfo value)
        {
            if (packet == null || value == null) return false;

            uint handle;
            byte status, minLevel, maxLevel, repeatMode, revivalMode, revivalCount, demandGender;
            ushort numJoiner, id, limitTime, startWaitTime, minPlayers, maxPlayers,
                repeatCount, demandQuest, demandItem;
            int startTime;
            long demandClass;
            string title;
            KingdomQuestNativeTime startTm;

            if (!packet.TryReadUInt(out handle) ||
                !packet.TryReadByte(out status) ||
                !packet.TryReadUShort(out numJoiner) ||
                !packet.TryReadUShort(out id) ||
                !packet.TryReadString(out title, 64) ||
                !packet.TryReadUShort(out limitTime) ||
                !packet.TryReadInt(out startTime) ||
                !KingdomQuestNativeTime.TryRead(packet, out startTm) ||
                !packet.TryReadUShort(out startWaitTime) ||
                !packet.TryReadByte(out minLevel) ||
                !packet.TryReadByte(out maxLevel) ||
                !packet.TryReadUShort(out minPlayers) ||
                !packet.TryReadUShort(out maxPlayers) ||
                !packet.TryReadByte(out repeatMode) ||
                !packet.TryReadUShort(out repeatCount) ||
                !packet.TryReadByte(out revivalMode) ||
                !packet.TryReadByte(out revivalCount) ||
                !packet.TryReadUShort(out demandQuest) ||
                !packet.TryReadUShort(out demandItem) ||
                !packet.TryReadLong(out demandClass) ||
                !packet.TryReadByte(out demandGender))
                return false;

            value.Handle = handle;
            value.Status = status;
            value.NumOfJoiner = numJoiner;
            value.ID = id;
            value.Title = title;
            value.LimitTime = limitTime;
            value.StartTime = startTime;
            value.StartTm = startTm;
            value.StartWaitTime = startWaitTime;
            value.MinLevel = minLevel;
            value.MaxLevel = maxLevel;
            value.MinPlayers = minPlayers;
            value.MaxPlayers = maxPlayers;
            value.PlayerRepeatMode = repeatMode;
            value.PlayerRepeatCount = repeatCount;
            value.PlayerRevivalMode = revivalMode;
            value.PlayerRevivalCount = revivalCount;
            value.DemandQuest = demandQuest;
            value.DemandItem = demandItem;
            value.DemandClass = demandClass;
            value.DemandGender = demandGender;
            return true;
        }
    }

    /// <summary>Native PROTO_KQ_MAP_INFO, exactly 26 bytes.</summary>
    public sealed class KingdomQuestMapProtocolInfo
    {
        public const int WireSize = 26;

        public byte MapIndex { get; set; }
        public string MapBase { get; set; } = string.Empty;
        public string MapName { get; set; } = string.Empty;
        public byte MapClear { get; set; }

        public void Write(Packet packet)
        {
            packet.WriteByte(MapIndex);
            packet.WriteString(MapBase ?? string.Empty, 12);
            packet.WriteString(MapName ?? string.Empty, 12);
            packet.WriteByte(MapClear);
        }

        public static bool TryRead(Packet packet, out KingdomQuestMapProtocolInfo value)
        {
            value = null;
            byte index, clear;
            string mapBase, mapName;
            if (packet == null || packet.Remaining < WireSize ||
                !packet.TryReadByte(out index) ||
                !packet.TryReadString(out mapBase, 12) ||
                !packet.TryReadString(out mapName, 12) ||
                !packet.TryReadByte(out clear))
                return false;

            value = new KingdomQuestMapProtocolInfo
            {
                MapIndex = index,
                MapBase = mapBase,
                MapName = mapName,
                MapClear = clear,
            };
            return true;
        }
    }

    /// <summary>
    /// Native KQ_JOIN_CHAR_INFO carried by NC_KQ_JOIN_LIST_ACK.
    /// Layout: u8 level, u8 class, Name5[20], u8 team = 23 bytes.
    /// </summary>
    public sealed class KingdomQuestJoinCharacterInfo
    {
        public const int WireSize = 23;

        public byte Level { get; set; }
        public byte Class { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte Team { get; set; }

        public void Write(Packet packet)
        {
            packet.WriteByte(Level);
            packet.WriteByte(Class);
            packet.WriteString(Name ?? string.Empty, 20);
            packet.WriteByte(Team);
        }

        public static bool TryRead(Packet packet, out KingdomQuestJoinCharacterInfo value)
        {
            value = null;
            byte level, cls, team;
            string name;
            if (packet == null || packet.Remaining < WireSize ||
                !packet.TryReadByte(out level) ||
                !packet.TryReadByte(out cls) ||
                !packet.TryReadString(out name, 20) ||
                !packet.TryReadByte(out team))
                return false;

            value = new KingdomQuestJoinCharacterInfo
            {
                Level = level,
                Class = cls,
                Name = name,
                Team = team,
            };
            return true;
        }
    }

    /// <summary>
    /// Native PROTO_NC_KQ_JOINER used by NC_KQ_W2Z_START_CMD.
    /// Layout: u32 chrregnum + u8 TeamType = 5 bytes.
    /// </summary>
    public sealed class KingdomQuestZoneJoinerInfo
    {
        public const int WireSize = 5;

        public uint CharacterNumber { get; set; }
        public byte TeamType { get; set; }

        public void Write(Packet packet)
        {
            packet.WriteUInt(CharacterNumber);
            packet.WriteByte(TeamType);
        }

        public static bool TryRead(Packet packet, out KingdomQuestZoneJoinerInfo value)
        {
            value = null;
            uint characterNumber;
            byte teamType;
            if (packet == null || packet.Remaining < WireSize ||
                !packet.TryReadUInt(out characterNumber) ||
                !packet.TryReadByte(out teamType))
                return false;

            value = new KingdomQuestZoneJoinerInfo
            {
                CharacterNumber = characterNumber,
                TeamType = teamType,
            };
            return true;
        }
    }

    /// <summary>Native SHINE_XY_TYPE, exactly 8 bytes.</summary>
    public sealed class KingdomQuestXY
    {
        public uint X { get; set; }
        public uint Y { get; set; }

        public void Write(Packet packet)
        {
            packet.WriteUInt(X);
            packet.WriteUInt(Y);
        }

        public static bool TryRead(Packet packet, out KingdomQuestXY value)
        {
            value = null;
            uint x, y;
            if (packet == null || packet.Remaining < 8 ||
                !packet.TryReadUInt(out x) ||
                !packet.TryReadUInt(out y))
                return false;

            value = new KingdomQuestXY { X = x, Y = y };
            return true;
        }
    }

    /// <summary>
    /// Native PROTO_KQ_INFO, exactly 377 bytes: the 141-byte client prefix
    /// followed by server/session fields.
    /// </summary>
    public sealed class KingdomQuestProtocolInfo : KingdomQuestClientInfo
    {
        public new const int WireSize = 377;

        public byte NextStartMode { get; set; }
        public ushort NextStartDelayMin { get; set; }
        public byte RepeatMode { get; set; }
        public ushort RepeatCount { get; set; }
        public ushort RewardIndex { get; set; }
        public ushort DemandMobKill { get; set; }
        public int ScheduleTime { get; set; }
        public KingdomQuestNativeTime ScheduleTm { get; set; } = new KingdomQuestNativeTime();
        public byte RunCounter { get; set; }
        public KingdomQuestMapProtocolInfo[] MapLink { get; set; } =
            new KingdomQuestMapProtocolInfo[4];
        public string ScriptLanguage { get; set; } = string.Empty;
        public string ScriptInitValue { get; set; } = string.Empty;
        public byte IsTeamPvp { get; set; }
        public KingdomQuestXY[] TeamRegenXY { get; set; } =
            new KingdomQuestXY[2];

        public override void Write(Packet packet)
        {
            base.Write(packet);
            packet.WriteByte(NextStartMode);
            packet.WriteUShort(NextStartDelayMin);
            packet.WriteByte(RepeatMode);
            packet.WriteUShort(RepeatCount);
            packet.WriteUShort(RewardIndex);
            packet.WriteUShort(DemandMobKill);
            packet.WriteInt(ScheduleTime);
            ScheduleTm.Write(packet);
            packet.WriteByte(RunCounter);

            if (MapLink == null || MapLink.Length != 4)
                throw new InvalidOperationException("PROTO_KQ_INFO requires exactly four MapLink entries.");
            for (int i = 0; i < 4; i++)
            {
                if (MapLink[i] == null) MapLink[i] = new KingdomQuestMapProtocolInfo();
                MapLink[i].Write(packet);
            }

            packet.WriteString(ScriptLanguage ?? string.Empty, 32);
            packet.WriteString(ScriptInitValue ?? string.Empty, 32);
            packet.WriteByte(IsTeamPvp);

            if (TeamRegenXY == null || TeamRegenXY.Length != 2)
                throw new InvalidOperationException("PROTO_KQ_INFO requires exactly two TeamRegenXY entries.");
            for (int i = 0; i < 2; i++)
            {
                if (TeamRegenXY[i] == null) TeamRegenXY[i] = new KingdomQuestXY();
                TeamRegenXY[i].Write(packet);
            }
        }

        public static bool TryRead(Packet packet, out KingdomQuestProtocolInfo value)
        {
            value = null;
            if (packet == null || packet.Remaining < WireSize) return false;

            var result = new KingdomQuestProtocolInfo();
            if (!KingdomQuestClientInfo.TryReadInto(packet, result))
                return false;

            byte nextStartMode, repeatMode, runCounter, isTeamPvp;
            ushort nextStartDelay, repeatCount, rewardIndex, demandMobKill;
            int scheduleTime;
            KingdomQuestNativeTime scheduleTm;
            if (!packet.TryReadByte(out nextStartMode) ||
                !packet.TryReadUShort(out nextStartDelay) ||
                !packet.TryReadByte(out repeatMode) ||
                !packet.TryReadUShort(out repeatCount) ||
                !packet.TryReadUShort(out rewardIndex) ||
                !packet.TryReadUShort(out demandMobKill) ||
                !packet.TryReadInt(out scheduleTime) ||
                !KingdomQuestNativeTime.TryRead(packet, out scheduleTm) ||
                !packet.TryReadByte(out runCounter))
                return false;

            var mapLinks = new KingdomQuestMapProtocolInfo[4];
            for (int i = 0; i < mapLinks.Length; i++)
            {
                if (!KingdomQuestMapProtocolInfo.TryRead(packet, out mapLinks[i]))
                    return false;
            }

            string scriptLanguage, scriptInitValue;
            if (!packet.TryReadString(out scriptLanguage, 32) ||
                !packet.TryReadString(out scriptInitValue, 32) ||
                !packet.TryReadByte(out isTeamPvp))
                return false;

            var teamRegen = new KingdomQuestXY[2];
            for (int i = 0; i < teamRegen.Length; i++)
            {
                if (!KingdomQuestXY.TryRead(packet, out teamRegen[i]))
                    return false;
            }

            result.NextStartMode = nextStartMode;
            result.NextStartDelayMin = nextStartDelay;
            result.RepeatMode = repeatMode;
            result.RepeatCount = repeatCount;
            result.RewardIndex = rewardIndex;
            result.DemandMobKill = demandMobKill;
            result.ScheduleTime = scheduleTime;
            result.ScheduleTm = scheduleTm;
            result.RunCounter = runCounter;
            result.MapLink = mapLinks;
            result.ScriptLanguage = scriptLanguage;
            result.ScriptInitValue = scriptInitValue;
            result.IsTeamPvp = isTeamPvp;
            result.TeamRegenXY = teamRegen;
            value = result;
            return true;
        }
    }

    /// <summary>
    /// Numeric KQ runtime values recovered from the original NA2016
    /// WorldManager executable. These are protocol/runtime constants, not
    /// emulator policy.
    /// </summary>
    public static class KingdomQuestNativeConstants
    {
        public const ushort MakeAckSuccess = 0x0981;

        public const ushort JoinSuccess = 0x0991;
        public const ushort JoinInvalidHandle = 0x0992;
        public const ushort JoinCapacityReached = 0x0993;
        public const ushort JoinWrongStatus = 0x0994;
        public const ushort JoinLevelRejected = 0x0995;
        public const ushort JoinClassRejected = 0x0996;
        public const ushort JoinGenderRejected = 0x0997;
        public const ushort JoinUnexpectedResult = 0x0998;
        public const ushort JoinPrisonRestricted = 0x0999;
        public const ushort JoinAlreadyInRequestedKq = 0x099A;

        public const ushort JoinCancelSuccess = 0x09A1;
        public const ushort JoinCancelNotJoined = 0x09A2;

        public const ushort JoinListSuccess = 0x3118;
        public const ushort JoinListInvalidHandle = 0x3119;
        public const ushort JoinListCooldown = 0x311A;
        public const int JoinListCooldownSeconds = 5;

        public const int JoinHardCapacity = 100;

        public const byte StatusScheduled = 0;
        public const byte StatusMakeRequested = 1;
        public const byte StatusJoining = 2;
        public const byte StatusStartCountdown = 3;
        public const byte StatusRunning = 4;
        public const byte StatusDone = 5;
        public const byte StatusDoneSkip = 6;
        public const byte StatusNoMap = 8;
        public const byte StatusDelete = 11;

        public const int StartCountdownSeconds = 10;
        public const byte DoneSkipReasonNotReady = 2;
        public const byte DoneSkipReasonTeamGap = 3;

        // Original PDB KQ_TEAM_DIVIDE_TYPE names + executable branches:
        // 1 = KQTD_RANDOM, 2 = KQTD_USERSELECT.
        public const ushort RandomTeamDivideType = 1;
        public const ushort UserSelectTeamDivideType = 2;
        public const byte NeutralTeamType = 2;
    }

}
