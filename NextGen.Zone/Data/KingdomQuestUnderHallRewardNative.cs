using System;

namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestUnderHallRewardNativePlan
    {
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public KingdomQuestPineKqRewardCommandPlan RewardCommand
        {
            get;
            private set;
        }

        internal KingdomQuestUnderHallRewardNativePlan(
            int canonicalLine,
            string topLevelBlock,
            KingdomQuestPineKqRewardCommandPlan rewardCommand)
        {
            CanonicalLine = canonicalLine;
            TopLevelBlock = topLevelBlock ?? string.Empty;
            RewardCommand = rewardCommand;
        }
    }

    /// <summary>
    /// UnderHall-specific source/instance binding for the sole
    /// "reward KingdomQuest" occurrence.
    ///
    /// KingdomQuestPineKqRewardCommandNative already owns the recovered
    /// KQElement Handle/RewardIndex/DemandMobKill gate. This wrapper adds the
    /// exact UnderHall source ownership: canonical QuestSuc line 378 and the
    /// literal KingdomQuest operand. Missing current KQ state remains
    /// fail-closed.
    ///
    /// No map traversal, contribution lookup, reward mutation or packet send
    /// occurs here.
    /// </summary>
    public static class KingdomQuestUnderHallRewardNative
    {
        public const int UnderHallCanonicalLine = 378;
        public const string NativeRewardToken = "KingdomQuest";

        public static bool TryBuild(
            KingdomQuestUnderHallExternalPlan source,
            KingdomQuestUnderHallExternalSourceSite sourceSite,
            uint currentKingdomQuestHandle,
            out KingdomQuestUnderHallRewardNativePlan plan)
        {
            plan = null;
            if (source == null ||
                sourceSite == null ||
                source.Kind != KingdomQuestUnderHallExternalPlanKind.Reward ||
                sourceSite.Kind != KingdomQuestUnderHallExternalPlanKind.Reward ||
                sourceSite.CanonicalLine != UnderHallCanonicalLine ||
                !string.Equals(
                    sourceSite.TopLevelBlock,
                    KingdomQuestUnderHallSourceFlow.SuccessBlock,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    source.SourceToken,
                    NativeRewardToken,
                    StringComparison.Ordinal))
                return false;

            KingdomQuestPineKqRewardCommandPlan rewardCommand;
            if (!KingdomQuestPineKqRewardCommandNative.TryBuild(
                    currentKingdomQuestHandle,
                    out rewardCommand) ||
                rewardCommand == null ||
                rewardCommand.Handle != currentKingdomQuestHandle)
                return false;

            plan = new KingdomQuestUnderHallRewardNativePlan(
                sourceSite.CanonicalLine,
                sourceSite.TopLevelBlock,
                rewardCommand);
            return true;
        }
    }
}
