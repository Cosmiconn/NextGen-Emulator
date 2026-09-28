using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestUnderHallExternalSourceSite
    {
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public KingdomQuestUnderHallExternalPlanKind Kind { get; private set; }

        internal KingdomQuestUnderHallExternalSourceSite(
            int canonicalLine,
            string topLevelBlock,
            KingdomQuestUnderHallExternalPlanKind kind)
        {
            CanonicalLine = canonicalLine;
            TopLevelBlock = topLevelBlock ?? string.Empty;
            Kind = kind;
        }
    }

    /// <summary>
    /// Exact top-level block ownership of all 25 external KQ/UnderHall command
    /// occurrences in the canonical hash-locked Pine source.
    ///
    /// This is source routing only. It does not assign native semantics to a
    /// command. In particular it proves that the sole Reward occurrence belongs
    /// to source block QuestSuc and that QuestFail has no Reward occurrence.
    /// </summary>
    public static class KingdomQuestUnderHallSourceFlow
    {
        public const string MainBlock = "main";
        public const string SuccessBlock = "QuestSuc";
        public const string FailureBlock = "QuestFail";
        public const int ExternalOccurrenceCount = 25;
        public const int SuccessRewardOccurrenceCount = 1;
        public const int FailureRewardOccurrenceCount = 0;

        private static readonly Dictionary<int, KingdomQuestUnderHallExternalSourceSite>
            Sites = new Dictionary<int, KingdomQuestUnderHallExternalSourceSite>
        {
            { 339, Site(339, "Nineteenth", KingdomQuestUnderHallExternalPlanKind.MobRegen) },

            { 355, Site(355, "Summon1", KingdomQuestUnderHallExternalPlanKind.SummonMob) },
            { 356, Site(356, "Summon1", KingdomQuestUnderHallExternalPlanKind.SummonMob) },
            { 359, Site(359, "Summon2", KingdomQuestUnderHallExternalPlanKind.SummonMob) },
            { 362, Site(362, "Summon3", KingdomQuestUnderHallExternalPlanKind.SummonMob) },
            { 363, Site(363, "Summon3", KingdomQuestUnderHallExternalPlanKind.SummonMob) },
            { 364, Site(364, "Summon3", KingdomQuestUnderHallExternalPlanKind.SummonMob) },
            { 367, Site(367, "Summon4", KingdomQuestUnderHallExternalPlanKind.SummonMob) },
            { 368, Site(368, "Summon4", KingdomQuestUnderHallExternalPlanKind.SummonMob) },
            { 369, Site(369, "Summon4", KingdomQuestUnderHallExternalPlanKind.SummonMob) },
            { 372, Site(372, "Summon5", KingdomQuestUnderHallExternalPlanKind.SummonMob) },
            { 373, Site(373, "Summon5", KingdomQuestUnderHallExternalPlanKind.SummonMob) },
            { 374, Site(374, "Summon5", KingdomQuestUnderHallExternalPlanKind.SummonMob) },

            { 378, Site(378, SuccessBlock, KingdomQuestUnderHallExternalPlanKind.Reward) },
            { 379, Site(379, SuccessBlock, KingdomQuestUnderHallExternalPlanKind.QuestMobKill) },
            { 380, Site(380, SuccessBlock, KingdomQuestUnderHallExternalPlanKind.Broadcast) },
            { 382, Site(382, SuccessBlock, KingdomQuestUnderHallExternalPlanKind.Broadcast) },
            { 384, Site(384, SuccessBlock, KingdomQuestUnderHallExternalPlanKind.Broadcast) },
            { 386, Site(386, SuccessBlock, KingdomQuestUnderHallExternalPlanKind.Broadcast) },
            { 388, Site(388, SuccessBlock, KingdomQuestUnderHallExternalPlanKind.LinkTo) },

            { 394, Site(394, FailureBlock, KingdomQuestUnderHallExternalPlanKind.Broadcast) },
            { 396, Site(396, FailureBlock, KingdomQuestUnderHallExternalPlanKind.Broadcast) },
            { 398, Site(398, FailureBlock, KingdomQuestUnderHallExternalPlanKind.Broadcast) },
            { 400, Site(400, FailureBlock, KingdomQuestUnderHallExternalPlanKind.Broadcast) },
            { 402, Site(402, FailureBlock, KingdomQuestUnderHallExternalPlanKind.LinkTo) },
        };

        public static bool TryResolve(
            int canonicalLine,
            KingdomQuestUnderHallExternalPlanKind kind,
            out KingdomQuestUnderHallExternalSourceSite site)
        {
            site = null;
            KingdomQuestUnderHallExternalSourceSite candidate;
            if (!Sites.TryGetValue(canonicalLine, out candidate) ||
                candidate == null ||
                candidate.Kind != kind)
                return false;

            site = candidate;
            return true;
        }

        public static IReadOnlyDictionary<int, KingdomQuestUnderHallExternalSourceSite>
            Snapshot()
        {
            return new Dictionary<int, KingdomQuestUnderHallExternalSourceSite>(Sites);
        }

        private static KingdomQuestUnderHallExternalSourceSite Site(
            int canonicalLine,
            string block,
            KingdomQuestUnderHallExternalPlanKind kind)
        {
            return new KingdomQuestUnderHallExternalSourceSite(
                canonicalLine,
                block,
                kind);
        }
    }
}
