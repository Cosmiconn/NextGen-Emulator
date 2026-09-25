using System;
using System.Collections.Generic;
using System.Linq;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// One selected native KQ reward slot after the original dice predicate
    /// and RewardData::rd_FindHandle-equivalent handle lookup.
    ///
    /// This is a pure source/runtime projection. It does not grant an item,
    /// experience, money or honor and does not perform persistence.
    /// </summary>
    public sealed class KingdomQuestResolvedRewardEntry
    {
        public int Slot { get; private set; }
        public ushort RewardHandle { get; private set; }
        public ushort RewardRate { get; private set; }
        public ushort RandomSample { get; private set; }
        public ShineRewardNativeInfo Reward { get; private set; }

        internal KingdomQuestResolvedRewardEntry(
            KingdomQuestRewardDiceEntry dice,
            ShineRewardNativeInfo reward)
        {
            if (dice == null) throw new ArgumentNullException("dice");
            if (reward == null) throw new ArgumentNullException("reward");

            Slot = dice.Slot;
            RewardHandle = dice.RewardHandle;
            RewardRate = dice.RewardRate;
            RandomSample = dice.RandomSample;
            Reward = reward;
        }
    }

    /// <summary>
    /// Mutation-free projection of the recovered positive branches in
    /// ShinePlayer::sp_KQReward.
    ///
    /// The native function consumes all 15 dice slots, skips failed dice,
    /// skips RewardData handle misses, ignores NONE, and has positive branches
    /// for ITEM / EXP / MONEY / HONOR. Later SHINE_REWARD_TYPE values are kept
    /// separately instead of being promoted to KQ behavior.
    /// </summary>
    public sealed class KingdomQuestRewardSelectionPlan
    {
        public IReadOnlyList<KingdomQuestResolvedRewardEntry> Items { get; private set; }
        public IReadOnlyList<KingdomQuestResolvedRewardEntry> Experience { get; private set; }
        public IReadOnlyList<KingdomQuestResolvedRewardEntry> Money { get; private set; }
        public IReadOnlyList<KingdomQuestResolvedRewardEntry> Honor { get; private set; }
        public IReadOnlyList<KingdomQuestResolvedRewardEntry> LaterTypes { get; private set; }
        public IReadOnlyList<KingdomQuestRewardDiceEntry> MissingHandles { get; private set; }

        private KingdomQuestRewardSelectionPlan(
            List<KingdomQuestResolvedRewardEntry> items,
            List<KingdomQuestResolvedRewardEntry> experience,
            List<KingdomQuestResolvedRewardEntry> money,
            List<KingdomQuestResolvedRewardEntry> honor,
            List<KingdomQuestResolvedRewardEntry> laterTypes,
            List<KingdomQuestRewardDiceEntry> missingHandles)
        {
            Items = items.AsReadOnly();
            Experience = experience.AsReadOnly();
            Money = money.AsReadOnly();
            Honor = honor.AsReadOnly();
            LaterTypes = laterTypes.AsReadOnly();
            MissingHandles = missingHandles.AsReadOnly();
        }

        public static bool TryBuild(
            KingdomQuestRewardSourceRow rewardSource,
            IReadOnlyList<ShineRewardSourceRow> shineRewards,
            IReadOnlyList<ushort> randomSamples,
            out KingdomQuestRewardSelectionPlan plan)
        {
            plan = null;
            if (rewardSource == null || shineRewards == null ||
                randomSamples == null)
                return false;

            KingdomQuestNativeRewardInfo nativeReward;
            if (!rewardSource.TryProjectNative(out nativeReward))
                return false;

            IReadOnlyList<KingdomQuestRewardDiceEntry> dice;
            try
            {
                dice = nativeReward.EvaluateDice(randomSamples);
            }
            catch (ArgumentException)
            {
                return false;
            }

            var items = new List<KingdomQuestResolvedRewardEntry>();
            var experience = new List<KingdomQuestResolvedRewardEntry>();
            var money = new List<KingdomQuestResolvedRewardEntry>();
            var honor = new List<KingdomQuestResolvedRewardEntry>();
            var laterTypes = new List<KingdomQuestResolvedRewardEntry>();
            var missing = new List<KingdomQuestRewardDiceEntry>();

            for (int i = 0; i < dice.Count; i++)
            {
                KingdomQuestRewardDiceEntry selected = dice[i];
                if (!selected.Selected)
                    continue;

                ShineRewardSourceRow sourceReward;
                if (!KingdomQuestRewardSourceResolver.TryFindShineRewardByHandle(
                        shineRewards, selected.RewardHandle, out sourceReward))
                {
                    // Native sp_KQReward skips a selected slot when
                    // RewardData::rd_FindHandle returns null.
                    missing.Add(selected);
                    continue;
                }

                ShineRewardNativeInfo resolved = sourceReward.ToNativeInfo();
                if (resolved.RewardType == ShineRewardType.None)
                    continue;

                var entry = new KingdomQuestResolvedRewardEntry(
                    selected, resolved);
                switch (resolved.RewardType)
                {
                    case ShineRewardType.Item:
                        items.Add(entry);
                        break;
                    case ShineRewardType.Experience:
                        experience.Add(entry);
                        break;
                    case ShineRewardType.Money:
                        money.Add(entry);
                        break;
                    case ShineRewardType.Honor:
                        honor.Add(entry);
                        break;
                    default:
                        // The recovered KQ switch has no proven positive
                        // branch for reward types 5..10. Preserve them without
                        // inventing an effect.
                        laterTypes.Add(entry);
                        break;
                }
            }

            plan = new KingdomQuestRewardSelectionPlan(
                items, experience, money, honor, laterTypes, missing);
            return true;
        }
    }
}
