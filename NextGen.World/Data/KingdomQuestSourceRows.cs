using System;
using System.Collections.Generic;
using System.Data;
using NextGen.Database.DataStore;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// One exact row from the supplied KingdomQuest.shn source table.
    /// Names intentionally follow the SHN, including its historical spelling.
    /// No runtime Handle/Status/Schedule fields are synthesized here.
    /// </summary>
    public sealed class KingdomQuestSourceDefinition
    {
        public uint SourceRow { get; private set; }
        public short ID { get; private set; }
        public string Title { get; private set; }
        public ushort LimitTime { get; private set; }
        public byte ST_Year { get; private set; }
        public byte ST_Month { get; private set; }
        public byte ST_Day { get; private set; }
        public byte ST_Hour { get; private set; }
        public byte ST_Minute { get; private set; }
        public byte ST_Second { get; private set; }
        public ushort StartWaitTime { get; private set; }
        public byte NextStartMode { get; private set; }
        public ushort NextStartDeleyMin { get; private set; }
        public byte RepeatMode { get; private set; }
        public ushort RepeatCount { get; private set; }
        public byte MinLevel { get; private set; }
        public byte MaxLevel { get; private set; }
        public ushort MinPlayers { get; private set; }
        public ushort MaxPlayers { get; private set; }
        public byte PlayerRepeatMode { get; private set; }
        public ushort PlayerRepeatCount { get; private set; }
        public byte PlayerRevivalMode { get; private set; }
        public byte PlayerRevivalCount { get; private set; }
        public ushort DemandQuest { get; private set; }
        public ushort DemandItem { get; private set; }
        public byte DemandMobKill { get; private set; }
        public uint RewardIndex { get; private set; }
        public IReadOnlyList<short> MapLinkColumns { get; private set; }
        public string ScriptLanguage { get; private set; }
        public string InitValue { get; private set; }
        public uint UseClass { get; private set; }
        public sbyte DemandGender { get; private set; }
        public sbyte Undefined3 { get; private set; }

        public static KingdomQuestSourceDefinition Load(DataRow row)
        {
            if (row == null) throw new ArgumentNullException("row");
            return new KingdomQuestSourceDefinition
            {
                SourceRow = GetDataTypes.GetUint(row["__SourceRow"]),
                ID = GetDataTypes.Getshort(row["ID"]),
                Title = Convert.ToString(row["Title"]),
                LimitTime = GetDataTypes.GetUshort(row["LimitTime"]),
                ST_Year = GetDataTypes.GetByte(row["ST_Year"]),
                ST_Month = GetDataTypes.GetByte(row["ST_Month"]),
                ST_Day = GetDataTypes.GetByte(row["ST_Day"]),
                ST_Hour = GetDataTypes.GetByte(row["ST_Hour"]),
                ST_Minute = GetDataTypes.GetByte(row["ST_Minute"]),
                ST_Second = GetDataTypes.GetByte(row["ST_Second"]),
                StartWaitTime = GetDataTypes.GetUshort(row["StartWaitTime"]),
                NextStartMode = GetDataTypes.GetByte(row["NextStartMode"]),
                NextStartDeleyMin = GetDataTypes.GetUshort(row["NextStartDeleyMin"]),
                RepeatMode = GetDataTypes.GetByte(row["RepeatMode"]),
                RepeatCount = GetDataTypes.GetUshort(row["RepeatCount"]),
                MinLevel = GetDataTypes.GetByte(row["MinLevel"]),
                MaxLevel = GetDataTypes.GetByte(row["MaxLevel"]),
                MinPlayers = GetDataTypes.GetUshort(row["MinPlayers"]),
                MaxPlayers = GetDataTypes.GetUshort(row["MaxPlayers"]),
                PlayerRepeatMode = GetDataTypes.GetByte(row["PlayerRepeatMode"]),
                PlayerRepeatCount = GetDataTypes.GetUshort(row["PlayerRepeatCount"]),
                PlayerRevivalMode = GetDataTypes.GetByte(row["PlayerRevivalMode"]),
                PlayerRevivalCount = GetDataTypes.GetByte(row["PlayerRevivalCount"]),
                DemandQuest = GetDataTypes.GetUshort(row["DemandQuest"]),
                DemandItem = GetDataTypes.GetUshort(row["DemandItem"]),
                DemandMobKill = GetDataTypes.GetByte(row["DemandMobKill"]),
                RewardIndex = GetDataTypes.GetUint(row["RewardIndex"]),
                MapLinkColumns = Array.AsReadOnly(new[]
                {
                    GetDataTypes.Getshort(row["MapLink"]),
                    GetDataTypes.Getshort(row["Undefined 0"]),
                    GetDataTypes.Getshort(row["Undefined 1"]),
                    GetDataTypes.Getshort(row["Undefined 2"]),
                }),
                ScriptLanguage = Convert.ToString(row["ScriptLanguage"]),
                InitValue = Convert.ToString(row["InitValue"]),
                UseClass = GetDataTypes.GetUint(row["UseClass"]),
                DemandGender = GetDataTypes.GetSByte(row["DemandGender"]),
                Undefined3 = GetDataTypes.GetSByte(row["Undefined 3"]),
            };
        }
    }

    /// <summary>One exact source row from KingdomQuestMap.shn.</summary>
    public sealed class KingdomQuestMapSourceRow
    {
        public uint SourceRow { get; private set; }
        public byte NumOfMap { get; private set; }
        public string BaseMap { get; private set; }
        public IReadOnlyList<string> MapColumns { get; private set; }
        public IReadOnlyList<sbyte> ClearColumns { get; private set; }

        public static KingdomQuestMapSourceRow Load(DataRow row)
        {
            if (row == null) throw new ArgumentNullException("row");
            return new KingdomQuestMapSourceRow
            {
                SourceRow = GetDataTypes.GetUint(row["__SourceRow"]),
                NumOfMap = GetDataTypes.GetByte(row["NumOfMap"]),
                BaseMap = Convert.ToString(row["BaseMap"]),
                MapColumns = Array.AsReadOnly(new[]
                {
                    Convert.ToString(row["Map"]),
                    Convert.ToString(row["Undefined 0"]),
                    Convert.ToString(row["Undefined 1"]),
                    Convert.ToString(row["Undefined 2"]),
                    Convert.ToString(row["Undefined 3"]),
                    Convert.ToString(row["Undefined 4"]),
                    Convert.ToString(row["Undefined 5"]),
                    Convert.ToString(row["Undefined 6"]),
                    Convert.ToString(row["Undefined 7"]),
                    Convert.ToString(row["Undefined 8"]),
                }),
                ClearColumns = Array.AsReadOnly(new[]
                {
                    GetDataTypes.GetSByte(row["Clear"]),
                    GetDataTypes.GetSByte(row["Undefined 9"]),
                    GetDataTypes.GetSByte(row["Undefined 10"]),
                    GetDataTypes.GetSByte(row["Undefined 11"]),
                    GetDataTypes.GetSByte(row["Undefined 12"]),
                    GetDataTypes.GetSByte(row["Undefined 13"]),
                    GetDataTypes.GetSByte(row["Undefined 14"]),
                    GetDataTypes.GetSByte(row["Undefined 15"]),
                    GetDataTypes.GetSByte(row["Undefined 16"]),
                    GetDataTypes.GetSByte(row["Undefined 17"]),
                }),
            };
        }
    }

    /// <summary>
    /// One exact source row from KingdomQuestRew.shn. The two 15-value blocks
    /// are preserved by source position; their gameplay selection rules are not
    /// interpreted here.
    /// </summary>
    public sealed class KingdomQuestRewardSourceRow
    {
        public uint SourceRow { get; private set; }
        public uint ID { get; private set; }
        public string IndexString { get; private set; }
        public string KQBoxItemIDX { get; private set; }
        public IReadOnlyList<short> RewardColumns { get; private set; }
        public IReadOnlyList<short> RewardRateColumns { get; private set; }

        public bool TryProjectNative(out KingdomQuestNativeRewardInfo value)
        {
            return KingdomQuestNativeRewardInfo.TryCreate(
                KQBoxItemIDX, RewardColumns, RewardRateColumns, out value);
        }

        public static KingdomQuestRewardSourceRow Load(DataRow row)
        {
            if (row == null) throw new ArgumentNullException("row");
            var rewards = new short[15];
            var rates = new short[15];
            rewards[0] = GetDataTypes.Getshort(row["Reward"]);
            rates[0] = GetDataTypes.Getshort(row["RewardRate"]);
            for (int i = 1; i < 15; i++)
            {
                rewards[i] = GetDataTypes.Getshort(row["Undefined " + (i - 1)]);
                rates[i] = GetDataTypes.Getshort(row["Undefined " + (i + 13)]);
            }

            return new KingdomQuestRewardSourceRow
            {
                SourceRow = GetDataTypes.GetUint(row["__SourceRow"]),
                ID = GetDataTypes.GetUint(row["ID"]),
                IndexString = Convert.ToString(row["IndexString"]),
                KQBoxItemIDX = Convert.ToString(row["KQBoxItemIDX"]),
                RewardColumns = Array.AsReadOnly(rewards),
                RewardRateColumns = Array.AsReadOnly(rates),
            };
        }
    }

    /// <summary>One exact source row from KQItem.shn.</summary>
    public sealed class KingdomQuestItemSourceRow
    {
        public uint SourceRow { get; private set; }
        public string ItemIndex { get; private set; }
        public ushort MoveSpdRate { get; private set; }
        public ushort AbsoluteAttack { get; private set; }
        public ushort PickupLimit { get; private set; }

        public static KingdomQuestItemSourceRow Load(DataRow row)
        {
            if (row == null) throw new ArgumentNullException("row");
            return new KingdomQuestItemSourceRow
            {
                SourceRow = GetDataTypes.GetUint(row["__SourceRow"]),
                ItemIndex = Convert.ToString(row["ItemIndex"]),
                MoveSpdRate = GetDataTypes.GetUshort(row["MoveSpdRate"]),
                AbsoluteAttack = GetDataTypes.GetUshort(row["AbsoluteAttack"]),
                PickupLimit = GetDataTypes.GetUshort(row["PickupLimit"]),
            };
        }
    }
    /// <summary>
    /// Exact row from the original shared UseClassTypeInfo.shn table used by
    /// WorldManager's CharClassDataBox::ccdb_UseClassTypeToBit.
    /// </summary>
    public sealed class KingdomQuestUseClassSourceRow
    {
        private static readonly string[] FlagColumns =
        {
            "Fig", "Cfig", "War", "Gla", "Kni", "Cle", "Hcle", "Pal", "Hol",
            "Gua", "Arc", "Harc", "Sco", "Sha", "Ran", "Mag", "Wmag", "Enc",
            "Warl", "Wiz", "Jok", "Chs", "Cru", "Cls", "Ass", "Sen", "Sav",
        };

        public uint SourceRow { get; private set; }
        public uint UseClass { get; private set; }
        public IReadOnlyList<byte> ClassFlags { get; private set; }

        public static KingdomQuestUseClassSourceRow Load(DataRow row)
        {
            if (row == null) throw new ArgumentNullException("row");
            var flags = new byte[FlagColumns.Length];
            for (int i = 0; i < flags.Length; i++)
                flags[i] = GetDataTypes.GetByte(row[FlagColumns[i]]);

            return new KingdomQuestUseClassSourceRow
            {
                SourceRow = GetDataTypes.GetUint(row["__SourceRow"]),
                UseClass = GetDataTypes.GetUint(row["UseClass"]),
                ClassFlags = Array.AsReadOnly(flags),
            };
        }

        public long ToDemandClassMask()
        {
            // ccdb_UseClassTypeToBit reads Sav..Fig, repeatedly shifts left
            // and adds the raw byte, then performs one final left shift.
            // This places Fig at bit 1 through Sav at bit 27.
            unchecked
            {
                ulong value = 0;
                for (int i = ClassFlags.Count - 1; i >= 0; i--)
                    value = (value << 1) + ClassFlags[i];
                value <<= 1;
                return (long)value;
            }
        }
    }
}
