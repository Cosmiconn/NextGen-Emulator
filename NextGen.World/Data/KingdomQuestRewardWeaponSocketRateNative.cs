using System;
using System.Collections.Generic;
using System.Data;
using NextGen.Database.DataStore;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Exact row from the supplied original EnchantSocketRate.shn.
    /// </summary>
    public sealed class KingdomQuestEnchantSocketRateSourceRow
    {
        public uint SourceRow { get; private set; }
        public uint ItemGradeType { get; private set; }
        public ushort Socket0 { get; private set; }
        public ushort Socket1 { get; private set; }
        public ushort Socket2 { get; private set; }

        public uint TotalWeight
        {
            get { return (uint)Socket0 + Socket1 + Socket2; }
        }

        public static KingdomQuestEnchantSocketRateSourceRow Load(DataRow row)
        {
            if (row == null) throw new ArgumentNullException("row");
            return new KingdomQuestEnchantSocketRateSourceRow
            {
                SourceRow = GetDataTypes.GetUint(row["__SourceRow"]),
                ItemGradeType = GetDataTypes.GetUint(row["ItemGradeType"]),
                Socket0 = GetDataTypes.GetUshort(row["Socket0"]),
                Socket1 = GetDataTypes.GetUshort(row["Socket1"]),
                Socket2 = GetDataTypes.GetUshort(row["Socket2"]),
            };
        }
    }

    /// <summary>
    /// Exact source-backed projection of
    /// EnchantSocketRateTable::EnchantSocketRateDataChild::iod_GetSocketCount.
    ///
    /// Original Zone.exe:
    /// - source object install: 0x005C8070
    /// - data-child vfunc0:     0x005C7FD0
    /// - source getter:         0x0064C5C0
    /// - random boundary:       0x0063CCC0
    ///
    /// ItemData +0x76 is ItemInfo.ItemGradeType. The source row's three socket
    /// weights are summed and the native random result in [0,total) selects
    /// socket count 0/1/2 by cumulative weight.
    ///
    /// The KQ weapon reward path calls this vfunc once, and calls it a second
    /// time when the first result is less than 2. Thus a first result of 1
    /// consumes a second WELL512 sample; a first result of 2 terminates
    /// immediately. A missing ItemGradeType row returns 0 without RNG.
    ///
    /// The caller supplies already-generated WELL512 samples. This type never
    /// owns, seeds, or advances a random generator.
    /// </summary>
    public static class KingdomQuestRewardWeaponSocketRateNative
    {
        public const string SourceSha256 =
            "777b29ac1c42481cc8887f169f1b70bb79467f82506bbf7d670d5a698fc54605";
        public const int SourceRecordCount = 5;
        public const int SourceColumnCount = 4;

        public const uint DataChildFunctionAddress = 0x005C7FD0u;
        public const uint InstallAddress = 0x005C8070u;
        public const uint SourceGetterAddress = 0x0064C5C0u;
        public const uint RandomBoundaryAddress = 0x0063CCC0u;
        public const int NativeItemGradeTypeOffset = 0x76;
        public const int WeaponSocketCountOffset = 0x43;

        public static bool TryApplyWeaponReward(
            ItemInfo item,
            IReadOnlyList<KingdomQuestEnchantSocketRateSourceRow> rates,
            IReadOnlyList<ushort> well512Samples,
            ref int sampleIndex,
            byte[] itemTotalInformation)
        {
            if (item == null || rates == null || well512Samples == null ||
                itemTotalInformation == null ||
                itemTotalInformation.Length !=
                    KingdomQuestRewardTreasureChestNative.ItemTotalInformationBytes)
                return false;

            byte first;
            if (!TryRoll(
                    item.ItemGradeType, rates, well512Samples,
                    ref sampleIndex, out first))
                return false;

            byte finalValue;
            if (first >= 2)
            {
                finalValue = 2;
            }
            else
            {
                if (!TryRoll(
                        item.ItemGradeType, rates, well512Samples,
                        ref sampleIndex, out finalValue))
                    return false;
            }

            itemTotalInformation[WeaponSocketCountOffset] = finalValue;
            return true;
        }

        public static bool TryRoll(
            uint itemGradeType,
            IReadOnlyList<KingdomQuestEnchantSocketRateSourceRow> rates,
            IReadOnlyList<ushort> well512Samples,
            ref int sampleIndex,
            out byte socketCount)
        {
            socketCount = 0;
            if (rates == null || well512Samples == null ||
                sampleIndex < 0 || sampleIndex > well512Samples.Count)
                return false;

            KingdomQuestEnchantSocketRateSourceRow match = null;
            for (int i = 0; i < rates.Count; i++)
            {
                KingdomQuestEnchantSocketRateSourceRow current = rates[i];
                if (current == null)
                    return false;
                if (current.ItemGradeType != itemGradeType)
                    continue;
                if (match != null)
                    return false;
                match = current;
            }

            // Native logs the unknown grade type and returns AL=0 without
            // entering the random path.
            if (match == null)
                return true;

            uint total = match.TotalWeight;
            if (total == 0 || total > ushort.MaxValue)
                return false;
            if (sampleIndex >= well512Samples.Count)
                return false;

            uint sample = well512Samples[sampleIndex++];
            if (sample >= total)
                return false;

            if (sample < match.Socket0)
            {
                socketCount = 0;
                return true;
            }

            sample -= match.Socket0;
            if (sample < match.Socket1)
            {
                socketCount = 1;
                return true;
            }

            socketCount = 2;
            return true;
        }
    }
}
