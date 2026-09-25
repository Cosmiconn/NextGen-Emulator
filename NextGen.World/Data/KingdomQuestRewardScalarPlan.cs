using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Mutation-free projection of the scalar branches recovered from
    /// ShinePlayer::sp_KQReward.
    ///
    /// The native switch adds selected ShineReward.Quantity values to the
    /// EXP / MONEY / HONOR accumulators. This model deliberately sums into
    /// UInt64 so it does not invent the native local-variable overflow policy.
    /// The supplied NA2016 corpus never approaches UInt32 overflow, so this
    /// wider representation is numerically identical for every source-backed
    /// reward row while keeping mutation/transaction timing separate.
    /// </summary>
    public sealed class KingdomQuestRewardScalarPlan
    {
        public IReadOnlyList<KingdomQuestResolvedRewardEntry> ExperienceEntries { get; private set; }
        public IReadOnlyList<KingdomQuestResolvedRewardEntry> MoneyEntries { get; private set; }
        public IReadOnlyList<KingdomQuestResolvedRewardEntry> HonorEntries { get; private set; }

        public ulong ExperienceQuantity { get; private set; }
        public ulong MoneyQuantity { get; private set; }
        public ulong HonorQuantity { get; private set; }

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
            ulong experienceQuantity,
            ulong moneyQuantity,
            ulong honorQuantity)
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

            ulong experienceQuantity;
            ulong moneyQuantity;
            ulong honorQuantity;
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
            out ulong quantity)
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

                // There are at most 15 native reward slots and Quantity is u32,
                // so the source-defined sum cannot overflow UInt64.
                quantity += entry.Reward.Quantity;
            }

            return true;
        }
    }
}
