using System;

namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestUnderHall2RewardNativePlan
    {
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public KingdomQuestPineKqRewardCommandPlan RewardCommand
        {
            get;
            private set;
        }

        internal KingdomQuestUnderHall2RewardNativePlan(
            int canonicalLine,
            string topLevelBlock,
            KingdomQuestPineKqRewardCommandPlan rewardCommand)
        {
            CanonicalLine = canonicalLine;
            TopLevelBlock = topLevelBlock ?? string.Empty;
            RewardCommand = rewardCommand;
        }
    }

    public sealed class KingdomQuestUnderHall2QuestMobKillNativePlan
    {
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public KingdomQuestPineUsedQuestMobKillNativePlan NativeCommand
        {
            get;
            private set;
        }

        internal KingdomQuestUnderHall2QuestMobKillNativePlan(
            int canonicalLine,
            string topLevelBlock,
            KingdomQuestPineUsedQuestMobKillNativePlan nativeCommand)
        {
            CanonicalLine = canonicalLine;
            TopLevelBlock = topLevelBlock ?? string.Empty;
            NativeCommand = nativeCommand;
        }
    }

    /// <summary>
    /// Reuses the already recovered native Pine reward and questmobkill
    /// primitives for the exact UnderHall2 source sites.
    ///
    /// Source ownership remains script-specific; command semantics are shared.
    /// No map traversal, player enumeration, CQuestZone mutation, reward
    /// mutation, GameDB write or packet send occurs here.
    /// </summary>
    public static class KingdomQuestUnderHall2CommonNative
    {
        public const int RewardOccurrenceCount = 2;
        public const int QuestMobKillOccurrenceCount = 2;

        public static bool TryBuildReward(
            KingdomQuestUnderHall2ExternalPlan source,
            uint currentKingdomQuestHandle,
            out KingdomQuestUnderHall2RewardNativePlan plan)
        {
            plan = null;
            if (source == null ||
                source.Kind != KingdomQuestUnderHall2ExternalKind.Reward ||
                !string.Equals(
                    source.SourceToken,
                    "KingdomQuest",
                    StringComparison.Ordinal) ||
                !IsRewardSite(source.CanonicalLine, source.TopLevelBlock))
                return false;

            KingdomQuestPineKqRewardCommandPlan rewardCommand;
            if (!KingdomQuestPineKqRewardCommandNative.TryBuild(
                    currentKingdomQuestHandle,
                    out rewardCommand) ||
                rewardCommand == null ||
                rewardCommand.Handle != currentKingdomQuestHandle)
                return false;

            plan = new KingdomQuestUnderHall2RewardNativePlan(
                source.CanonicalLine,
                source.TopLevelBlock,
                rewardCommand);
            return true;
        }

        public static bool TryBuildQuestMobKill(
            KingdomQuestUnderHall2ExternalPlan source,
            DataProvider data,
            out KingdomQuestUnderHall2QuestMobKillNativePlan plan)
        {
            plan = null;
            if (source == null ||
                source.Kind != KingdomQuestUnderHall2ExternalKind.QuestMobKill ||
                !IsQuestMobKillSite(
                    source.CanonicalLine, source.TopLevelBlock))
                return false;

            KingdomQuestPineUsedQuestMobKillNativePlan nativeCommand;
            if (!KingdomQuestPineUsedQuestMobKillNative.TryBuild(
                    source.RawNumeric1,
                    source.SourceToken,
                    source.Count,
                    data,
                    out nativeCommand) ||
                nativeCommand == null)
                return false;

            plan = new KingdomQuestUnderHall2QuestMobKillNativePlan(
                source.CanonicalLine,
                source.TopLevelBlock,
                nativeCommand);
            return true;
        }

        private static bool IsRewardSite(int line, string block)
        {
            return
                (line == 543 &&
                    string.Equals(
                        block, "QuestSuc", StringComparison.Ordinal)) ||
                (line == 559 &&
                    string.Equals(
                        block, "QuestSuc2", StringComparison.Ordinal));
        }

        private static bool IsQuestMobKillSite(int line, string block)
        {
            return
                (line == 544 &&
                    string.Equals(
                        block, "QuestSuc", StringComparison.Ordinal)) ||
                (line == 560 &&
                    string.Equals(
                        block, "QuestSuc2", StringComparison.Ordinal));
        }
    }
}
