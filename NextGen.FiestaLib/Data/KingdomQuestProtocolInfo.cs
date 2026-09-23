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
    }
}
