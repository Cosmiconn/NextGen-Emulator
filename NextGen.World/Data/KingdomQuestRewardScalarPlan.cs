using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Mutation-free projection of the scalar branches recovered from
    /// ShinePlayer::sp_KQReward.
    ///
    /// Zone.exe proves three 32-bit local accumulators. Selected
    /// ShineReward.Quantity u32 values are added with native x86 DWORD adds,
    /// therefore EXP / MONEY / HONOR wrap modulo 2^32. MONEY is zero-extended
    /// to the u64 cen field only when NC_KQ_REWARD_REQ is constructed.
    /// </summary>
    public sealed class KingdomQuestRewardScalarPlan
    {
        public IReadOnlyList<KingdomQuestResolvedRewardEntry> ExperienceEntries { get; private set; }
        public IReadOnlyList<KingdomQuestResolvedRewardEntry> MoneyEntries { get; private set; }
        public IReadOnlyList<KingdomQuestResolvedRewardEntry> HonorEntries { get; private set; }

        public uint ExperienceQuantity { get; private set; }
        public uint MoneyQuantity { get; private set; }
        public uint HonorQuantity { get; private set; }

        /// <summary>
        /// Exact packet representation used by sp_KQReward: the native u32
        /// MONEY accumulator is written as the low DWORD of u64 cen while the
        /// high DWORD is explicitly zero.
        /// </summary>
        public ulong MoneyPacketCen
        {
            get { return (ulong)MoneyQuantity; }
        }

        public int SelectedScalarCount
        {
            get
            {
                return ExperienceEntries.Count +
                    MoneyEntries.Count +
                    HonorEntries.Count;
            }
        }

        private KingdomQuestRewardScalarPlan(
            List<KingdomQuestResolvedRewardEntry> experience,
            List<KingdomQuestResolvedRewardEntry> money,
            List<KingdomQuestResolvedRewardEntry> honor,
            uint experienceQuantity,
            uint moneyQuantity,
            uint honorQuantity)
        {
            ExperienceEntries = experience.AsReadOnly();
            MoneyEntries = money.AsReadOnly();
            HonorEntries = honor.AsReadOnly();
            ExperienceQuantity = experienceQuantity;
            MoneyQuantity = moneyQuantity;
            HonorQuantity = honorQuantity;
        }

        public static bool TryBuild(
            KingdomQuestRewardSelectionPlan selection,
            out KingdomQuestRewardScalarPlan plan)
        {
            plan = null;
            if (selection == null)
                return false;

            var experience = new List<KingdomQuestResolvedRewardEntry>(
                selection.Experience);
            var money = new List<KingdomQuestResolvedRewardEntry>(
                selection.Money);
            var honor = new List<KingdomQuestResolvedRewardEntry>(
                selection.Honor);

            uint experienceQuantity;
            uint moneyQuantity;
            uint honorQuantity;
            if (!TryAccumulate(
                    experience, ShineRewardType.Experience,
                    out experienceQuantity) ||
                !TryAccumulate(
                    money, ShineRewardType.Money,
                    out moneyQuantity) ||
                !TryAccumulate(
                    honor, ShineRewardType.Honor,
                    out honorQuantity))
                return false;

            if (experience.Count + money.Count + honor.Count >
                KingdomQuestNativeRewardInfo.EntryCount)
                return false;

            plan = new KingdomQuestRewardScalarPlan(
                experience, money, honor,
                experienceQuantity, moneyQuantity, honorQuantity);
            return true;
        }

        private static bool TryAccumulate(
            IReadOnlyList<KingdomQuestResolvedRewardEntry> entries,
            ShineRewardType expectedType,
            out uint quantity)
        {
            quantity = 0;
            if (entries == null)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                KingdomQuestResolvedRewardEntry entry = entries[i];
                if (entry == null ||
                    entry.Reward == null ||
                    entry.Reward.RewardType != expectedType)
                    return false;

                // Native sp_KQReward uses a DWORD local and an x86 add.
                quantity = unchecked(quantity + entry.Reward.Quantity);
            }

            return true;
        }
    }
}
